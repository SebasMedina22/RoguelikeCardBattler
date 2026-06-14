# Auditoría integral 2026-06 — Fase 1: Código y arquitectura

> **Método:** 6 lectores paralelos por subsistema (~108 scripts) → 56 hallazgos
> brutos → dedup a 24 (4 "críticos" + 20 mayores) → 9 verificadores
> adversariales (cada cadena causal re-leída en código, chequeo de "ya
> registrado" contra _roadmap/_tech_snapshot/_insights/Combat-CLAUDE.md).
> **Resultado:** 0 críticos, **10 mayores confirmados**, ~14 menores, 3
> oportunidades, 3 sub-claims descartados.
> **Consumo:** ~1.05M tokens de subagentes (find ~500k + verify ~530k).

---

## Resumen ejecutivo

El proyecto está estructuralmente sano: capas respetadas, ActionQueue
determinista, generador de mapa sin RNG no seedeado, presentación disciplinada.
Los problemas reales se concentran en **tres clusters**:

1. **Sincronización RunState↔combate al cierre** — dos Retazos rotos HOY en el
   flujo real (H1, H2) + el retry de derrota inutilizable (H3) + resets
   divergentes (H4). Todos comparten la raíz: `BattleFlowController.ReportOutcome`
   sincroniza tarde y los resets in-place duplican lógica de `RunSession`.
2. **Cuatro supuestos del core que M5/M6 rompen de frente** (H6-H9): switch
   solo-jugador sin veto/initiator, tipo de enemigo único y estático, cero
   sustrato de status/debuff, acto único como bool. Todos viven en archivos
   protegidos → **conviene UNA cirugía diseñada y aprobada de TurnManager antes
   de M5, no cuatro aperturas incrementales.**
3. **Semántica encolar-vs-ejecutar** (H5): efectividad/Estilo/hooks corren al
   encolar, el daño al ejecutar. Es diseño documentado (m3_hooks_spec), pero
   tiene consecuencias no documentadas (enemigo muerto que ataca, Victory
   evaluada antes que Defeat, carga de Estilo con daño 100% bloqueado, Spines
   resuelve antes que el golpe que la causa). Hay que DECIDIR la semántica, no
   solo parcharla.

---

## Hallazgos MAYORES confirmados (10)

### Cluster A — Sincronización de fin de combate / resets de run

**H1. `RelicEndHealEffect` (R-END-2 "Curita con Estampa") es un no-op en el
flujo real** — `BattleFlowController.cs:117`
El efecto escribe `RunState.PlayerCurrentHP` durante `DispatchCombatEnd`
(sincrónico, dentro de `CheckCombatEndConditions`), pero `ReportOutcome`
(polling posterior) pisa incondicionalmente ese valor con
`_turnManager.PlayerHP`, que nunca recibió el heal. Los tests pasan porque no
cubren el flujo con `BattleFlowController`. **Origen: el propio spec
`RELICS.md:228` prescribe esta implementación.**
*Fix sugerido:* sincronizar RunState antes de `DispatchCombatEnd`, o que el
ctx lea/escriba el HP del actor. Esfuerzo S.

**H2. `RelicElitePuristEffect` ("Caja Intacta") evalúa HP stale** —
`RelicElitePuristEffect.cs:17`
Lee `RunState.PlayerCurrentHP` en OnCombatEnd, que durante el combate conserva
el valor PRE-combate (verificado por grep exhaustivo: nada sincroniza antes).
Paga si ENTRASTE con vida llena, no si TERMINASTE con vida llena — condición
invertida. Agravante: si R-END-2 corre antes en la cadena, puede inflar el
valor stale. Mismo origen que H1 (`RELICS.md:240`) — **arreglarlos juntos**.
Esfuerzo S (con H1).

**H3. Reintentar tras derrota arranca el combate con 1 HP** —
`RunFlowController.cs:~651`
La derrota guarda `PlayerCurrentHP=0`; el botón Reintentar limpia flags pero no
restaura HP; `HasPlayerHPInitialized` sigue true así que el 0 pasa como
override y `TurnManager.InitializeCombat` lo clampa a 1. El botón es inútil en
la práctica (loop de derrota casi garantizado). Bug player-facing verificado
eslabón por eslabón. Esfuerzo S.

**H4. Dos paths de reset de run divergentes: los botones de defeat/acto
resetean in-place y pierden la identidad del run** — `RunFlowController.cs:666,693`
`RunState.Reset()` devuelve los tipos a Rojo/Amarillo y anula
`PendingStarterCard`; los botones resetean sin pasar por NewRunScene → "nueva
run" con identidad default silenciosa, mazo de 9 cartas (sin la dual drafteada)
y el MISMO mapa (no regenera, a diferencia de `RunSession.ResetForNewRun`).
Relacionado: el TODO stale de `RunFlowController.cs:701` ("cuando exista
MainMenuScene" — existe desde hace meses); cargar MainMenuScene vía
`ResetForNewRun` resolvería ambos. Esfuerzo S-M.

### Cluster B — Readiness M5/M6 (los cuatro supuestos del core)

> Los cuatro viven en TurnManager/actors (protegidos). Recomendación operativa
> del verify: **diseñar una cirugía única aprobada pre-M5** que resuelva el
> paquete completo, en vez de abrir el protegido cuatro veces.

**H5. Semántica encolar-vs-ejecutar sin decidir + daño de Retazos sin
ProcessAll** — `TurnManager.cs:1065, 634, 856`
Verificado: `RelicEnqueueExtraDamage` solo encola (el drain ocurre en el
próximo ProcessAll del flujo natural — diseño documentado en
`m3_hooks_spec.md:391`); `DamageAction.Execute` no chequea source vivo →
enemigo muerto por daño de Retazo igual ejecuta su ataque ya encolado;
`CheckCombatEndConditions` evalúa Victory con return ANTES que Defeat →
victoria posible con jugador en 0 HP. Además: +1 Estilo se otorga pre-block
(SuperEficaz 100% absorbido da carga — la redacción de GOLDEN_RULES §4 no lo
prohíbe explícitamente, pero la lectura natural de "hace daño" sí); Spines
encola la represalia ANTES del golpe que la causa (inversión FIFO); y el
comentario de `:856` promete una detección de victoria que el código no cumple.
Hoy el escenario letal es un edge angosto; con más Retazos/bosses M5 deja de
serlo. *Fix sugerido:* decidir y documentar la semántica (¿drain post-hook?
¿Defeat prioritario sobre Victory? ¿Estilo post-block?) como parte de la
cirugía única. Esfuerzo M (decisión + cambios acotados).

**H6. Cambio de mundo: un solo path, presupuestado al jugador, sin initiator
ni veto** — `TurnManager.cs:763-786`, `WorldSwitchHookData.cs:14`
Verificado por grep: `TryChangeWorld` es el único mutador de `currentWorld` en
producción; siempre chequea el cap del jugador, siempre incrementa
`_worldSwitchesUsed`, siempre dispatchea OnWorldSwitch DESPUÉS de mutar (sin
punto de veto). El hook no lleva Initiator → el Desfase Dimensional de M5
dispararía los 4 Retazos Switch del jugador en cambios hostiles, y los bosses
M6 que bloquean el cambio no tienen dónde vetar. No existe contador de cartas
jugadas por turno (cero hits). Esfuerzo M (diseño de firma + payload).

**H7. Tipo de enemigo único, estático, leído del SO; sin TypeWorldB ni
IsAnchor** — `TurnManager.cs:624,416,112`, `EnemyDefinition.cs:22`
El jugador tiene la indirección correcta (`PlayerActiveType` deriva del mundo);
el enemigo no — 3 call sites leen crudo del SO y `EnemyCombatActor` no tiene
noción de tipo. Bloquea 4c (transdim+ancla), el boss M5 (un tipo por mundo) y
M6. **Es exactamente el bloque 4c del roadmap** — este hallazgo confirma su
alcance y que la extensión es limpia y acotada. Esfuerzo M (ya agendado).

**H8. Cero sustrato de status/debuff + lógica de actors duplicada textualmente** —
`ICombatActor.cs`, `PlayerCombatActor.cs:97-110` vs `EnemyCombatActor.cs:35-48`
`EffectType.ApplyStatus` existe en el enum y cae al default LogWarning (única
referencia = la declaración). Ningún actor tiene contenedor de statuses;
TakeDamage/GainBlock/LoseBlock/Heal son copias idénticas en ambos actors —
Virus (block al 80%) obligaría a tocar DOS clases sin punto de intercepción, y
Sangrado necesita tick/expiración que el turn flow no contempla. Statuses son
núcleo de M5. Esfuerzo L (diseño nuevo dentro de la cirugía).

**H9. Multi-acto sin representación: `ActoCompleted` bool, config single-act** —
`RunState.cs:32`, `RunCombatConfig.cs`
Cero hits de `CurrentAct`; un solo `defaultEnemy`/`bossRelicDrop`; HP enemigo
fijo sin escalado por acto. M6 lo fuerza todo a la vez; introducir `CurrentAct`
ya en M5 evita migrar el bool después. Esfuerzo M.

### Cluster C — Fragilidad latente de inicialización

**H10. Carrera de Awake en run nueva: `TryInheritBossFrom` regenera el mapa
que `RunFlowController` ya pudo haber capturado** — `RunSession.cs:147`,
`RunFlowController.cs:71`
La cadena es real (MainMenu crea RunSession DDOL sin configs → mapa fallback →
RunScene regenera al heredar configs). HOY funciona porque el GameObject
RunSession precede a RunFlowController en el YAML de la escena y el orden de
facto lo respeta — **sin contrato** (no hay DefaultExecutionOrder; reordenar la
jerarquía de RunScene rompería el mapa en silencio). Bonus verificado:
`RunState.Reset` en MainMenu queda atado al mapa fallback y funciona por
coincidencia de StartNodeId. *Fix sugerido:* inicialización explícita (lazy
getter o DefaultExecutionOrder + comentario). Esfuerzo S.

---

## Menores confirmados (al backlog, sin urgencia)

- **Docs stale sobre gaps resueltos** (reportado por 4 lectores independientes):
  PhaseBased AI, `EffectType.Heal`+`HealAction` y `CalculateIntentValue`
  (Defend+Heal) **SÍ están implementados** (Sub-PR D, 2026-05-07; Combat/CLAUDE.md
  ya lo registra) pero `_tech_snapshot` "Restricciones conocidas" y menciones del
  roadmap los siguen listando como pendientes. → insumo directo para Fase 3.
- `RelicTurnEnergyEveryN` (Reloj de Cocina): counter sangra entre combates (sin
  reset OnCombatStart, a diferencia de sus siblings R-ACC-2/R-DMG-2). Resolver
  como DECISIÓN de diseño (el wording del asset lo hace defendible) + fix simétrico.
- `RelicAccSkillStacker`: pending de energía cruza combates (mismo fix).
- Sin validación SO↔efecto: el reset de counters depende del array Hooks del
  asset; fix útil = test EditMode que cargue los `.asset` reales de Relics y
  valide hooks declarados vs consumidos (hoy ningún test carga assets reales).
- Orden de hooks T1 (OnPlayerTurnStart antes que OnCombatStart): decisión
  documentada (evitar ClearBlock), pero footgun real para Retazos futuros que
  combinen ambos hooks — documentar en RELICS.md como regla de autoría.
- `SaveSmokeTest` vivo en BattleScene (sin `#if UNITY_EDITOR`, corre en builds,
  sobreescribe `save_v1.json` en cada combate). Hoy inocuo (cero consumidores
  del save); se vuelve mayor el día que meta-progresión (M6c) lea ese archivo.
  Borrarlo es gratis.
- Topologías hardcode 8 nodos vs `totalNodes` editable sin clamp/OnValidate:
  foot-gun latente de inspector (grafo roto en silencio). Validación de 5 líneas.
- `SceneTransitionManager`: `_isTransitioning` sin recuperación — soft-lock no
  alcanzable hoy (verificado: no hay KillAll, SetUpdate(true), safe mode on),
  pero sin hardening un KillAll futuro lo rompe sin síntoma.
- Comentario engañoso `TurnManager.cs:856` (promete detección de victoria
  post-hook que no ocurre) — trampa para autores de Retazos futuros.
- `currentWorld` no se resetea en `InitializeCombat` (hoy lo salva la recarga
  de escena; muerde si M6 encadena combates sin recarga).
- Residuo de M17: End Turn no chequea `IsPlayingAttack` → clic durante la
  animación de ataque difiere la resolución de la carta al turno enemigo.
- `RelicDebugOverlay` muta `RunState.Relics` directo → AcquisitionOrder
  duplicado en sesiones de playtesting (editor-only; molesto para lo que el
  tool quiere probar). Fix: `RemoveRelic()` en RunState que normalice.
- Recompensa post-combate NO randomizada (siempre las primeras N del
  RewardPool — sin TODO que lo marque); oro +10 hardcodeado en nodos
  placeholder; reward distorsiona playtests de economía.
- `RunFlowController` god-object (885 LOC, ~20 métodos Build*/Show*/flow,
  verificado método por método) y duplicación espejo Shop↔Campfire (~150-180
  LOC/archivo de helpers idénticos; `GetWhiteSprite` ×4). Ambos sin riesgo
  funcional hoy — pero **extraer ANTES de 4b** (EventNodeController sería la
  3ª copia).

## Oportunidades

- Migrar fin-de-combate de polling a evento (`OnCombatFinished`) — nota de
  diseño pre-M5, sin bug actual.
- `UIFactory` central en Core/UI (patrón create-text/button/panel forkeado ×4+).
- `EnemyIntentType` extensible + íconos (el switch del HUD cubre todos los
  valores actuales; el gap es del enum, no de la view).
- `EnsureEventSystem` ×3 divergentes → helper único en Core.
- Base `RelicEffectBase<THookData>` antes de la ola de efectos M5/M6.
- `MaxStyleCharges` expuesto por TurnManager (HUD hardcodea "/5").

## Descartados por el verify (y por qué)

- **Efectividad stale si el mundo cambia entre prepare y resolve:** falso — la
  efectividad jugador→enemigo depende del elemento de la carta (fijado al
  preparar) y del tipo del enemigo (inmutable en combate); `currentWorld` no
  entra al cálculo.
- **Softlock por cola pendiente:** el guard de Update solo bloquea el AUTO
  end-turn; el botón End Turn drena la cola vía ExecuteEnemyTurn.
- **Muerte del callback de PlayAttackOnce (energía perdida/carta en limbo):**
  los modos de muerte afirmados no son alcanzables — guard `_isPlayingAttack` +
  botones deshabilitados + animator del jugador sin otros callers (queda solo
  el residuo End-Turn-durante-animación, listado en menores).
- **PhaseBased "sin estado" como mayor:** factualmente exacto pero ya
  trackeado en Future work (strategy pattern IA + play-mode tests pre-M5).

## Insumos para fases siguientes

- **Fase 2 (tests):** los tests de Relics no cubren el flujo con
  BattleFlowController (por eso H1/H2 pasan verdes); ningún test carga los
  `.asset` reales; el flujo derrota→retry no tiene test; candidatos directos.
- **Fase 3 (docs):** gaps resueltos listados como pendientes en _tech_snapshot
  y roadmap; `RELICS.md:228,240` prescribe el patrón que causa H1/H2;
  GOLDEN_RULES §4 ambiguo en "daño real" (¿post-block?); _tech_snapshot con
  LOC/conteos viejos (53 vs 108 scripts).

---
*Fase 1 cerrada 2026-06-11. Verify adversarial: 9 grupos, 24 hallazgos, 152
lecturas de verificación. Veredictos: 10 confirmados-mayor, 14 menores
(confirmados o degradados con núcleo real), 3 oportunidades, 3 descartados.*
