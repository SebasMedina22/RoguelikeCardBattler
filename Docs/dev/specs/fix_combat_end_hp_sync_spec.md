# Spec — Fix de sincronización de HP al cerrar combate + retry/reset (SUB-PR 1)

> **ESTADO: CERRADO — Opción B ratificada + edición aditiva de TurnManager APROBADA por Sebastián (2026-06-14). Listo para `modo:implementacion`.**
> Branch destino: `feat/fix-combat-end-sync`. Cluster A de `AUDIT_REPORT.md`.
> Plan: `PLAN_PRE_M4.md §SUB-PR 1`.

## Origen
- Auditoría integral 2026-06 — `AUDIT_REPORT.md` Cluster A (H1, H2, H3, H4) +
  Fase 2 (tests T1, T2). Plan de remediación `PLAN_PRE_M4.md §SUB-PR 1`.

## Objetivo
Que el HP del jugador y las recompensas de fin de combate sean correctas al
cruzar el borde combate→run, y que los caminos de retry/abandono de run dejen el
estado en una condición jugable y coherente. Hoy: 2 Retazos rotos en el flujo
real, retry inutilizable (arranca a 1 HP), y dos resets de run divergentes.

## Comportamiento esperado (perspectiva del jugador)
- **Curita con Estampa (R-END-2):** al ganar un combate, el +HP de victoria se
  conserva al volver al mapa (hoy se pierde).
- **Caja Intacta (R-ELITE-3):** paga oro si **terminás** el combate con HP lleno
  (hoy paga si **entraste** con HP lleno — condición invertida).
- **Reintentar (tras derrota):** el combate reinicia con HP jugable, no a 1 HP.
- **Abandonar run (botón de derrota / acto completado):** vuelve al menú
  principal, desde donde "Play" arranca una run nueva legítima (regenera mapa,
  re-elige tipos y draftea en NewRunScene). Hoy resetea in-place y deja la run en
  un estado degradado (tipos a default, sin carta drafteada, mismo mapa).

---

## Diagnóstico verificado `[CÓDIGO ACTUAL]`

### Raíz común de H1/H2 — RunState tiene HP stale cuando corren los hooks OnCombatEnd
Orden real de eventos al ganar/perder:
1. `TurnManager.CheckCombatEndConditions` ([TurnManager.cs:717-733](../../../Assets/Scripts/Gameplay/Combat/TurnManager.cs#L717-L733))
   setea `_phase = Victory/Defeat` **y luego** llama `DispatchCombatEnd`.
2. `DispatchCombatEnd` ([:738-747](../../../Assets/Scripts/Gameplay/Combat/TurnManager.cs#L738-L747))
   dispara los hooks `OnCombatEnd`. En este punto **`RunState.PlayerCurrentHP`
   sigue siendo el valor PRE-combate** (nunca se sincronizó durante el combate;
   la verdad vive en el actor `_player.CurrentHP`).
3. Mucho después, `BattleFlowController.Update` detecta la fase y llama
   `ReportOutcome`, que en [BattleFlowController.cs:115-119](../../../Assets/Scripts/Run/BattleFlowController.cs#L115-L119)
   hace `session.State.PlayerCurrentHP = _turnManager.PlayerHP` — **pisa** lo que
   el Retazo hubiera escrito en RunState.

- **H1** — `RelicEndHealEffect` ([RelicEndHealEffect.cs:15-23](../../../Assets/Scripts/Gameplay/Relics/Effects/RelicEndHealEffect.cs#L15-L23))
  escribe `RunState.PlayerCurrentHP += Amount` sobre la base stale; el paso 3 lo
  pisa → **la curación se pierde** (además computaría sobre la base equivocada).
- **H2** — `RelicElitePuristEffect` ([RelicElitePuristEffect.cs:12-19](../../../Assets/Scripts/Gameplay/Relics/Effects/RelicElitePuristEffect.cs#L12-L19))
  lee `RunState.PlayerCurrentHP` (pre-combate) → **condición invertida.**

> **Corrección a la auditoría (D7) y al plan:** el plan afirma que SUB-PR 1
> "No toca protegidos". Es **falso para H1**. La "vía correcta" que sugiere D7
> (`GrantHeal` sobre el actor) **sería un no-op**: `RelicGrantHeal`
> ([:1005-1009](../../../Assets/Scripts/Gameplay/Combat/TurnManager.cs#L1005-L1009))
> hace `if (IsCombatFinished ...) return`, e `IsCombatFinished` ([:96](../../../Assets/Scripts/Gameplay/Combat/TurnManager.cs#L96))
> ya es `true` cuando corre OnCombatEnd (la fase se setea antes del dispatch). El
> dispatch de OnCombatEnd lo posee TurnManager y ningún componente externo puede
> sincronizar RunState entre "fase seteada" y "hooks". **Por lo tanto H1 exige
> un cambio mínimo en TurnManager** (H2/H3/H4 no). Esto alimenta el SUB-PR 4
> (docs RELICS.md): la regla de autoría real depende del fix elegido aquí.

### H3 — Retry arranca a 1 HP
El botón Reintentar ([RunFlowController.cs:650-658](../../../Assets/Scripts/Run/RunFlowController.cs#L650-L658))
limpia flags pero **no restaura HP**: tras la derrota `PlayerCurrentHP = 0` y
`HasPlayerHPInitialized = true`. Al recargar BattleScene, `EnsurePlayerHpInitialized`
([RunState.cs:146-156](../../../Assets/Scripts/Run/RunState.cs#L146-L156)) hace
early-return (ya inicializado), pasa `currentHp = 0` como override, y
`InitializeCombat` ([TurnManager.cs:264-272](../../../Assets/Scripts/Gameplay/Combat/TurnManager.cs#L264-L272))
clampa `Clamp(0, 1, MaxHP) = 1`. → loop de derrota.

### H4 — Dos resets de run divergentes + TODO stale
- Botón "Volver al mapa" (panel de derrota) [RunFlowController.cs:664-672](../../../Assets/Scripts/Run/RunFlowController.cs#L664-L672)
  y "Volver al Menú" (panel acto completado) [:691-703](../../../Assets/Scripts/Run/RunFlowController.cs#L691-L703)
  hacen `_state.Reset(_map)` + `InitializeDeck` **in-place**, con el `_map`
  cacheado viejo (no regenera topología), reseteando `PlayerWorldAType/BType` a
  Rojo/Amarillo y anulando `PendingStarterCard` — y **saltándose NewRunScene**
  (donde el jugador elige tipos y draftea). El camino canónico es
  `MainMenuController.OnPlayClicked` → `RunSession.ResetForNewRun()` →
  `NewRunScene` ([MainMenuController.cs:77-89](../../../Assets/Scripts/Menu/MainMenuController.cs#L77-L89)).
- TODO stale en [:701](../../../Assets/Scripts/Run/RunFlowController.cs#L701)
  ("cuando exista MainMenuScene") — `MainMenuScene.unity` existe hace meses.

---

## Decisión central `[ABIERTO]` — ¿dónde vive la verdad del HP al cerrar combate?

Ambas opciones tocan TurnManager (protegido) para H1. Difieren en alcance.

### `[PROPUESTA]` Opción B (RECOMENDADA) — RunState es la verdad, sync pre-dispatch
TurnManager sincroniza `RunState.PlayerCurrentHP = _player.CurrentHP` **justo
antes** de disparar OnCombatEnd. Entonces los Retazos siguen mutando RunState
directo (ya sobre base fresca) y `BattleFlowController` deja de pisar.

- **TurnManager `[REQUIERE APROBACIÓN]`** — cambio **puramente aditivo**, ~6
  líneas, dentro del `if (TryGetRelicContext(...))` ya existente en
  `DispatchCombatEnd`. NO toca fases, orden, ni ningún guard:
  ```csharp
  private void DispatchCombatEnd(bool victory)
  {
      if (TryGetRelicContext(out RunState ceRs, out RelicHookDispatcher ceDisp))
      {
          // Fix H1/H2: el HP del run = HP del actor al cerrar combate, ANTES de
          // que los Retazos OnCombatEnd lean/muten RunState. Sin esto, ceRs
          // conserva el HP PRE-combate y BattleFlowController lo sincronizaba
          // tarde (pisando curaciones de fin de combate).
          if (_player != null)
          {
              ceRs.PlayerCurrentHP = _player.CurrentHP;
              ceRs.PlayerMaxHP = _player.MaxHP;
          }

          CombatEndHookData ceData = new CombatEndHookData(
              ceRs, this, ceDisp, victory, enemyDefinition,
              isBoss: _isCurrentCombatBoss, isElite: _isCurrentCombatElite);
          ceDisp.Dispatch(RelicHook.OnCombatEnd, ceData);
      }
  }
  ```
- **`RelicEndHealEffect` y `RelicElitePuristEffect`: SIN cambio de lógica** (la
  mutación/lectura directa de RunState pasa a ser correcta). Solo se actualizan
  sus comentarios engañosos.
- **BattleFlowController:** se **elimina** el bloque de sync tardío ([:115-119](../../../Assets/Scripts/Run/BattleFlowController.cs#L115-L119))
  porque RunState ya es autoritativo post-dispatch (si quedara, re-pisaría).

**Por qué B:** mínima superficie en protegido (aditivo, sin cambiar reglas de
combate); cero churn en los 23 Retazos; coherente con GOLDEN_RULES §7
("RunState = datos; el HP persiste entre combates"); resuelve H1 **y** H2 juntos;
y **disuelve el obstáculo de T1** (el fix sube a TurnManager, así que el test no
necesita mockear `SceneTransitionManager`).

### `[ALTERNATIVA]` Opción A — el actor es la verdad, Retazos vía API
`RelicEndHeal` usaría `ctx.GrantHeal(ctx.TurnManager.Player, Amount)` y
`RelicElitePurist` leería `ctx.TurnManager.PlayerHP`; `BattleFlowController:117`
queda (sincroniza actor→RunState).
- Requiere **relajar el guard `IsCombatFinished` de `RelicGrantHeal`** en
  TurnManager — un cambio **semántico** a una regla de combate protegida (radio
  de impacto mayor; es justo lo que la cirugía pre-M5 quiere tratar con cuidado).
- Cambia 2 archivos de Retazos. Alinea con la letra de D7 pero por un camino más
  invasivo en el protegido.
- **Descartada** frente a B por mayor riesgo en el archivo protegido y más churn.
  *(Sub-variante rechazada: `ctx.TurnManager.Player.Heal()` directo saltándose el
  guard — funciona sin editar TurnManager pero rompe deliberadamente el contrato
  de la API de Retazos; mal precedente.)*

> **CERRADO (2026-06-14):** Sebastián ratificó **Opción B** y **aprobó** la edición
> aditiva de TurnManager (solo el sync en `DispatchCombatEnd`). El prompt de
> handoff queda desbloqueado.

---

## Sistemas afectados
- **Combate (TurnManager, protegido):** sync de HP actor→RunState pre-dispatch (B).
- **Run flow:** `BattleFlowController` (sync tardío), `RunFlowController`
  (botones retry/derrota/acto), `RunState` (helper de retry).
- **Retazos:** `RelicEndHealEffect`, `RelicElitePuristEffect` (comentarios; lógica
  solo si se elige A).
- **UI:** sin cambios estructurales (los botones ya existen).
- **ScriptableObjects:** ninguno.

## Archivos a crear
- `Assets/Tests/EditMode/CombatEndSyncTests.cs` — aloja T1 y T2.

## Archivos a modificar
- `Assets/Scripts/Gameplay/Combat/TurnManager.cs` — **PROTEGIDO**, ver §protegidos.
- `Assets/Scripts/Run/BattleFlowController.cs` — eliminar el sync de HP de
  `ReportOutcome` (:115-119) y extraer la mutación de RunState a un método
  testeable sin carga de escena (seam para T1). El tail de `LoadScene` queda fuera.
- `Assets/Scripts/Run/RunFlowController.cs` — botón Reintentar → `_state.PrepareForRetry()`;
  botones "Volver al mapa" (derrota) y "Volver al Menú" (acto) → cargar
  `MainMenuScene` (con guard `IsSceneInBuild`), eliminando el `Reset` in-place +
  `InitializeDeck` + el TODO de :701.
- `Assets/Scripts/Run/RunState.cs` — agregar `PrepareForRetry()`.
- `Assets/Scripts/Gameplay/Relics/Effects/RelicEndHealEffect.cs` — actualizar
  comentario (bajo B, sin cambio de lógica).
- `Assets/Scripts/Gameplay/Relics/Hooks/CombatEndHookData.cs` — actualizar
  comentario de cabecera (la mutación directa es correcta porque TurnManager
  sincroniza antes del dispatch).

## Archivos protegidos involucrados
- [x] `TurnManager.cs` — **REQUIERE APROBACIÓN.** Bajo Opción B: +6 líneas
  aditivas en `DispatchCombatEnd` (sync actor→RunState antes del `Dispatch`). No
  altera fases, orden de turno, ni guards existentes. Bajo Opción A: relajar el
  guard `IsCombatFinished` en `RelicGrantHeal` (cambio semántico — mayor riesgo).
- [ ] `ActionQueue.cs` — no se toca.
- [ ] `PlayerCombatActor.cs` — no se toca (se usa su API pública `CurrentHP`/`MaxHP`/`Heal`).

---

## Contratos

### Datos
Sin estructuras nuevas. Se reusa `RunState.{PlayerCurrentHP, PlayerMaxHP,
RunFailed, PendingReturnFromBattle, LastNodeOutcome}`.

### APIs nuevas
- `RunState.PrepareForRetry()` — restaura el estado a "combate jugable de nuevo"
  tras una derrota. Efecto:
  ```csharp
  public void PrepareForRetry()
  {
      RunFailed = false;
      PendingReturnFromBattle = false;
      LastNodeOutcome = NodeOutcome.None;
      if (PlayerMaxHP > 0) PlayerCurrentHP = PlayerMaxHP;   // HP jugable = full
  }
  ```
  No toca `HasPlayerHPInitialized` (queda `true`; `PlayerCurrentHP = PlayerMaxHP`
  pasa el clamp de `InitializeCombat`). No toca `CurrentNodeId` (retry = mismo nodo).
- `BattleFlowController.ApplyCombatResult(RunSession session, bool victory)`
  (seam de testeo) — contiene las mutaciones de RunState de `ReportOutcome`
  (flags, `ActoCompleted`, `TryDropRelics`) **sin** sync de HP y **sin**
  `LoadScene`. `ReportOutcome` = `ApplyCombatResult(...)` + tail de transición de
  escena. Visibilidad mínima para que el test la alcance (ver §Casos de prueba).

### Eventos
Ninguno nuevo. (La migración fin-de-combate polling→evento queda fuera de scope;
es oportunidad pre-M5 en `AUDIT_REPORT.md §1`.)

## Reuso
- Patrón de unificación de reset: el reset de run nuevo ya lo posee
  `MainMenuController.OnPlayClicked` → `RunSession.ResetForNewRun()`. Los botones
  de H4 **delegan** en ese camino (cargan `MainMenuScene`) en vez de duplicarlo.
- Guard de escena en Build Settings: reusar el patrón `IsSceneInBuild(...)` ya
  presente en `RunFlowController.StartCombatForNode` ([:360-375](../../../Assets/Scripts/Run/RunFlowController.cs#L360-L375)).
- `SceneTransitionManager.LoadScene(...)` para la transición (ya usado en todo el flujo).

---

## Casos de prueba (EditMode) — `CombatEndSyncTests.cs`

> Política de testing del proyecto (`_modo_implementacion`): lógica pura/
> determinista → EditMode obligatorio; integración que cruza el borde de escena/
> RunSession → validación en Play. Se respeta abajo con un límite de cobertura
> explícito.

### T1 — `ReportOutcome_AfterCombatEndHook_PreservesRelicHeal`
Documenta H1. Bajo Opción B, el HP correcto lo establece el sync de TurnManager
(Play-validado) y la **no-pisada** la garantiza `ApplyCombatResult` (sin sync de HP).
- **Seam:** primero extraer `ApplyCombatResult` de `ReportOutcome`
  (behavior-preserving). El test ejercita esa lógica sin cargar escena.
- **Arrange:** `RunState` con `PlayerMaxHP=60`, `PlayerCurrentHP=34` (simula la
  base post-combate ya sincronizada 30 + heal 4 aplicado en el dispatch).
- **Act:** invocar el camino de outcome de victoria (vía `ApplyCombatResult`).
- **Assert:** `PlayerCurrentHP == 34` (la curación sobrevive; el outcome no pisa).
- **Fail-first:** correr contra el código actual (con el sync inline `PlayerCurrentHP
  = _turnManager.PlayerHP` activo en el seam) → falla (devuelve 30). Tras el fix
  (sync removido del seam + sync agregado a TurnManager) → pasa (34).
- **Límite de cobertura (explícito):** el test bloquea el contrato "el paso de
  outcome NO pisa el HP". La otra mitad — TurnManager sincronizando RunState=actor
  antes del dispatch — cruza el borde de RunSession (necesita sesión viva /
  PlayMode) → se valida en BattleScene (§Validación manual).

### T2 — `RunState_AfterDefeatWithZeroHp_RetryPathRestoresPlayableHp`
Documenta H3. Puro RunState, sin Unity.
- **Arrange:** `PlayerMaxHP=60`, `PlayerCurrentHP=0`, `HasPlayerHPInitialized=true`,
  `RunFailed=true`, `PendingReturnFromBattle=true`.
- **Act:** `state.PrepareForRetry()`.
- **Assert:** `PlayerCurrentHP == 60` (y `> 1`), `RunFailed == false`,
  `PendingReturnFromBattle == false`, `LastNodeOutcome == None`.
- **Fail-first:** contra el código actual no existe `PrepareForRetry`; el camino
  de retry deja `PlayerCurrentHP == 0`. Tras el fix → pasa.

### Regresión
Suite EditMode completa en verde (hoy 146/146). En particular sin regresión en
`RelicEffectsTests` (`RelicEndHeal_VictoryRespectsMaxHp` y demás OnCombatEnd
siguen pasando — la lógica del Retazo no cambia bajo B).

## Validación manual (BattleScene + RunScene)
1. **H1:** equipar Curita con Estampa, entrar a combate con HP < max, ganar
   recibiendo daño → en RunScene el HP debe ser (HP post-combate + Amount), no el
   HP post-combate pelado ni el HP pre-combate.
2. **H2:** equipar Caja Intacta, ganar un Elite **con HP lleno al terminar** →
   +oro; ganar **dañado** → sin oro. Verificar también el caso inverso (entrar
   lleno, terminar dañado) → sin oro.
3. **H3:** perder un combate → Reintentar → el combate arranca con HP full.
4. **H4:** perder → "Volver al mapa" → carga MainMenuScene; "Play" arranca run
   nueva (NewRunScene: re-elegir tipos + draft; mapa regenerado). Igual desde el
   panel de acto completado ("Volver al Menú").
5. Cero errores en consola en todo el flujo.

## Decisiones cerradas
- "HP jugable" del retry = **HP full** (`PlayerMaxHP`). Una variante de balance
  (ej. 50%) es ajuste futuro, no bloquea.
- H4 unifica vía **cargar MainMenuScene** (no replicar `ResetForNewRun` en los
  botones): el reset de run nuevo lo posee MainMenu→Play. Resuelve el TODO de :701.
- HP base del jugador = **60** (D-C ya resuelta; código y escena en 60). El retry
  a full usa `PlayerMaxHP`, agnóstico al número.
- Interacción heal+purist en el mismo combate: si un heal de fin de combate topea
  a max, purist (si corre después por AcquisitionOrder) paga — consistente con
  "terminar con HP lleno". Comportamiento aceptado, no se fuerza orden.
- Fuera de scope (decisión cerrada): semántica del Estilo pre/post-block (D-A, va
  en la cirugía pre-M5 / otro sub-PR); migración fin-de-combate a evento.

## Decisiones abiertas
Ninguna. **Cerradas el 2026-06-14:**
- Opción A vs B → **B** (RunState es la verdad; sync pre-dispatch). Ratificada por Sebastián.
- Edición de `TurnManager.cs` → **APROBADA**: solo el sync aditivo (+6 líneas) en
  `DispatchCombatEnd`. Ningún otro cambio del protegido permitido en este PR.

## Alternativas consideradas
- **Opción A** (actor = verdad): descartada — ver §Decisión central.
- **Sin tocar TurnManager** (honrar el plan literal): **imposible** para H1
  correcto — demostrado en §Diagnóstico (el dispatch lo posee TurnManager y
  `GrantHeal` está bloqueado por `IsCombatFinished`).
- **Mockear `SceneTransitionManager` para testear `ReportOutcome` end-to-end:**
  innecesario bajo B — el fix sube a TurnManager y el seam `ApplyCombatResult`
  evita la carga de escena.

## Estimación
- **Complejidad:** media (cluster comparte raíz; un PR coherente).
- **Sub-tareas:** (1) sync en TurnManager [aprobado]; (2) quitar sync tardío +
  extraer seam en BattleFlowController; (3) `PrepareForRetry` + recablear botón
  retry; (4) recablear botones derrota/acto a MainMenuScene + guard; (5) comentarios
  de Retazos/CombatEndHookData; (6) T1+T2 + suite.
- **Riesgo:** bajo-medio. El único riesgo real es el protegido (mitigado: cambio
  aditivo, suite completa + validación Play). Coordinar con **SUB-PR 4** (RELICS.md
  / m3_hooks_spec): el fix define la regla de autoría OnCombatEnd real.

---

## Prompt de handoff para `modo:implementacion`
> ✅ **Desbloqueado (2026-06-14):** Opción B ratificada y edición de TurnManager
> aprobada por Sebastián. Copiá/pegá el bloque para arrancar `modo:implementacion`.

```
modo:implementacion

Implementá el SUB-PR 1 (fix de sincronización fin-de-combate + retry/reset). El
spec cerrado está en `Docs/dev/specs/fix_combat_end_hp_sync_spec.md` — leelo
completo antes de tocar código. Decisión cerrada: Opción B (RunState es la verdad,
sync pre-dispatch en TurnManager). Edición de TurnManager APROBADA (solo el sync
aditivo en DispatchCombatEnd; nada más del protegido).

Setup de branch:
  git fetch --all --prune
  git checkout -b feat/fix-combat-end-sync origin/main

Qué construir (detalle y contratos en el spec):
- TurnManager.cs (PROTEGIDO, cambio APROBADO y ACOTADO): en DispatchCombatEnd,
  antes del Dispatch, sincronizar ceRs.PlayerCurrentHP/PlayerMaxHP desde _player.
  NADA MÁS de TurnManager.
- BattleFlowController.cs: eliminar el sync de HP de ReportOutcome (:115-119);
  extraer ApplyCombatResult(session, victory) (mutaciones de RunState sin LoadScene)
  como seam de testeo.
- RunState.cs: agregar PrepareForRetry().
- RunFlowController.cs: botón Reintentar → _state.PrepareForRetry(); botones
  "Volver al mapa"/"Volver al Menú" → cargar MainMenuScene con guard IsSceneInBuild,
  eliminando el Reset in-place + InitializeDeck + el TODO de :701.
- RelicEndHealEffect.cs y CombatEndHookData.cs: actualizar comentarios (sin cambio
  de lógica).
- Tests: Assets/Tests/EditMode/CombatEndSyncTests.cs con T1 y T2.

Reglas no negociables:
- TurnManager: SOLO el sync aprobado. Cualquier otra necesidad en el protegido →
  PARAR y pedir aprobación.
- No tocar la semántica del Estilo (D-A, otro sub-PR) ni migrar fin-de-combate a
  evento (pre-M5).
- No manual editor setup: todo se prueba en runtime / por flujo de escena.
- MainMenuScene debe estar en Build Settings (verificar; ya debería estar).

Validación obligatoria antes de cerrar:
- Compilación limpia (zero console errors).
- T1 y T2 en verde + suite EditMode completa sin regresiones (hoy 146/146).
- Flujo end-to-end en Play (BattleScene→RunScene): los 5 pasos de "Validación
  manual" del spec (H1 heal persiste, H2 purist correcto ambos sentidos, H3 retry
  full HP, H4 ambos botones → MainMenuScene → Play arranca run nueva).
- Validación de UI por diagnóstico de código si el game view no repinta vía CLI.

Al cerrar: actualizá `_roadmap.md` (checkboxes del SUB-PR 1) y `_tech_snapshot.md`
(nota del fix + seam ApplyCombatResult), commit + push + PR a main. Coordinar con
SUB-PR 4 (RELICS.md): este fix define la regla de autoría OnCombatEnd real.
```
