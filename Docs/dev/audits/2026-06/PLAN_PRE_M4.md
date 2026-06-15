# Plan de remediación pre-M4 — Auditoría integral 2026-06

> **Sesión:** 2026-06-12 (Fase 5). **Estado: PROPUESTA.** Sebastián aprueba qué
> entra al roadmap, en qué orden y qué se difiere/descarta.
> **Insumo:** `AUDIT_REPORT.md` (este plan no re-deriva hallazgos, los secuencia).
> **Filosofía:** sub-PRs chicos estilo M3 (un tema por PR, mergeable solo,
> reversible), ordenados por dependencia. Nada toca archivos protegidos salvo la
> cirugía pre-M5, que va aparte con diseño aprobado.

---

## 0. Mapa de decisiones que bloquean trabajo

**D-A, D-B y D-C resueltas por Sebastián el 2026-06-12** (abajo, con sus
consecuencias). **D-D sigue abierta** (se decide al actualizar el roadmap de M4).

| Decisión | Resolución (2026-06-12) | Consecuencia operativa |
|---|---|---|
| **D-A. Semántica del Estilo: ¿pre o post-block?** (D4/H5) | **PRE-block (ratificado).** Todo golpe SuperEficaz otorga carga aunque el enemigo lo bloquee al 100%. = comportamiento actual del código. | **SIN cambio de lógica.** ① El test T3 **se INVIERTE**: pasa a `StyleCharge_SuperEffectiveHitFullyBlocked_StillGrantsCharge` (asserta que SÍ otorga). ② GOLDEN_RULES §4 (Sebastián): "+1 carga cuando el ataque es SuperEficaz; el bloqueo del enemigo NO anula la carga". ③ Corregir el comentario "daño real" de `TurnManager.cs:634` para aclarar que el pre-block es intencional (edit chico, protegido → Sebastián aprueba; va con el paquete docs/comentarios). |
| **D-B. Contador de Estilo: ¿acumula >5 o capea a 5?** (A12) | **Capea a 5.** | **ES UN BUG a arreglar.** El código hoy acumula >5 vía relics (`TurnManager.cs:992-996`; solo `SetStyleChargesForTest` clampa). Fix pequeño en TurnManager (protegido → Sebastián aprueba) que **acompaña al test T7** (`GrantStyleCharge_Overshoot_ResetsAndGrantsSingleBonus`). El test hoy fallaría → documenta el bug. |
| **D-C. HP base: ¿60 o 70?** | **60.** | Código y escena ya están en 60 → **sin cambio de código**. Corregir GOLDEN_RULES §9 (Sebastián) de 70 → 60. |
| **D-D. Huérfanos: ¿adoptar o diferir?** cartas WorldSwitch + tiers DD-009 | **ABIERTA** | Se decide al actualizar el roadmap de M4. |

> **Impacto de D-A en la cirugía pre-M5:** como el Estilo queda pre-block por
> diseño, ese punto **sale** del alcance de la cirugía (era un "si es post-block"
> condicional). La cirugía pre-M5 se concentra en H5 (resto de la semántica
> encolar-vs-ejecutar: Defeat vs Victory, actor muerto que ataca), H6, H8, H9.
> **Impacto de D-B:** el cap a 5 es un fix chico y aislado en TurnManager — se
> puede hacer ya (con aprobación de edición del protegido), no necesita esperar
> a la cirugía.

---

## 1. Secuencia por dependencias

```
                    ┌─────────────────────────────────────────────┐
   DECISIONES  →    │ D-A pre/post-block · D-B cap Estilo · D-C HP │
                    └─────────────────────────────────────────────┘
                                      │
        ┌─────────────────────────────┼─────────────────────────────┐
        ▼                             ▼                             ▼
  ┌───────────┐               ┌───────────────┐            ┌──────────────┐
  │ SUB-PR 1  │  bugs sync    │  SUB-PR 6     │ seguridad  │ SUB-PRs 3-5  │ docs
  │ H1-H4     │ ◄── tests ──► │  tooling T1   │ tooling    │ paquetes i/  │
  │ +T1,T2    │   T1,T2        │  T3,T4,T9,T10│            │ ii/iii       │
  └───────────┘               └───────────────┘            └──────────────┘
        │  (T1/T2 dependen del fix; los demás tests son independientes)
        ▼
  ┌───────────┐
  │ SUB-PR 2  │  red de tests pre-cirugía (T4-T9)
  └───────────┘
        │
        ▼  (todo lo anterior cerrado = base estable)
  ┌─────────────────────────────────────────────┐
  │  M4 bloque 4a/4b/4c (roadmap existente)      │  ← H7 = 4c
  │  + desguace RunFlowController ANTES de 4b    │
  └─────────────────────────────────────────────┘
        │
        ▼  (con la red de tests como contrato congelado)
  ┌─────────────────────────────────────────────┐
  │  CIRUGÍA ÚNICA DE TURNMANAGER (pre-M5)        │  H5,H6,H8,H9 + T3,T12 + D-A
  │  diseño aprobado · archivos protegidos       │
  └─────────────────────────────────────────────┘
```

Los sub-PRs 1, 3-5 y 6 son **independientes entre sí** → pueden ir en paralelo o
en cualquier orden. El sub-PR 2 (tests T1/T2) depende del sub-PR 1. La cirugía
pre-M5 depende de que la red de tests esté completa.

---

## 2. Sub-PRs pre-M4 (detalle)

### SUB-PR 1 — Fix de sincronización fin-de-combate + retry `feat/fix-combat-end-sync` ✅ CERRADO 2026-06-14

**Cierra:** H1, H2, H3, H4 (+ tests T1, T2 que los documentan).
**Esfuerzo:** M (el cluster comparte raíz; un solo PR coherente).
**Toca 1 protegido (aprobado y acotado):** TurnManager — H1 lo exigía (ver spec
`fix_combat_end_hp_sync_spec.md`; el "No toca protegidos" original era falso para
H1). BattleFlowController y RunFlowController no son protegidos.

- [x] **Opción B (cerrada):** TurnManager sincroniza `RunState.PlayerCurrentHP/MaxHP
      = _player.*` **antes** de `DispatchCombatEnd` (+9 líneas aditivas, solo eso del
      protegido). Los Retazos mutan/leen RunState fresco → arregla H1 y H2 de raíz.
- [x] BattleFlowController: eliminado el re-sync tardío de HP (`ReportOutcome`);
      extraído seam público `ApplyCombatResult(session, victory)` (mutaciones de
      RunState sin HP ni `LoadScene`) → habilita T1 sin mockear `SceneTransitionManager`.
- [x] Botón Reintentar → `RunState.PrepareForRetry()` (HP a full, limpia flags) → H3.
- [x] Botones defeat/acto → `ReturnToMainMenu()` (carga `MainMenuScene` con guard
      `IsSceneInBuild`); eliminado el reset in-place + `InitializeDeck` + el TODO de
      `:701` → H4.
- [x] **Tests (T1 fail-first → verde, T2):**
  - `ReportOutcome_AfterCombatEndHook_PreservesRelicHeal` (T1)
  - `RunState_AfterDefeatWithZeroHp_RetryPathRestoresPlayableHp` (T2)
- [x] Validación: compilación limpia, suite EditMode **148/148** (146 + T1/T2),
      H1–H4 verificados en Play (2026-06-14).
- **Coordina con:** SUB-PR 4 (RELICS.md / D7/D8) — el fix define la regla de autoría
  OnCombatEnd real: bajo Opción B la **mutación directa de `PlayerCurrentHP` en
  OnCombatEnd es CORRECTA** (TurnManager sincroniza antes del dispatch), NO el patrón
  roto que asumía la "vía segura" vía `GrantHeal` (no-op por `IsCombatFinished`).

### SUB-PR 2 — Red de tests pre-cirugía + cap de Estilo `feat/test-net-pre-surgery`

**Cierra:** T3, T4, T5, T6, T7, T9 (los 6 tests pendientes del top 8) + el fix
del cap a 5 (D-B) + suplentes opcionales.
**Esfuerzo:** S-M (todos EditMode puros, verificados factibles uno a uno) + un
fix chico en TurnManager.
**Depende de:** D-A y D-B (ambas ya resueltas → desbloqueado).
**Incluye 1 edición de archivo protegido** (cap a 5 en TurnManager) → Sebastián
aprueba la edición en el hilo principal.

- `RelicAssets_AllDeclaredHooksAreDispatchable` (T5) — única validación datos↔código; carga los 23 `.asset` reales.
- `RelicGrantEffects_OnRealTurnManager_MutateState` ×10 param. (T6) — media tabla de relics gana asserts de conducta.
- `StyleCharge_SuperEffectiveHitFullyBlocked_StillGrantsCharge` (T3, **invertido por D-A**) — ratifica que el pre-block otorga carga aunque el golpe se bloquee al 100%. Hoy PASA (congela comportamiento).
- `GrantStyleCharge_Overshoot_ResetsAndGrantsSingleBonus` (T7) — invariante de D-B (cap a 5). **Hoy FALLA** → documenta el bug; se arregla en este mismo PR con el cap en `TurnManager.cs:992-996`.
- `InitializeCombat_SecondCall_ResetsStyleStateAndRelicCounters` (T4) — congela el contrato; documenta el counter que sangra (R-6).
- `NeutralCard_Applies90PercentDamage` (T9) — regla de balance viva.
- **Suplentes si hay apetito:** T8 (`InitializeDeck` ×2), T10 (round-trip de tipos).
- **Propósito:** estos tests **congelan el comportamiento que la cirugía pre-M5 no debe romper.** Por eso van antes.

### SUB-PR 3 — Docs paquete (i): Momentum → Estilo `docs/momentum-to-style`

**Cierra:** D1, D2, D3, B11 + **T2 y T6 de F4** (mismo PR, todos son docs de onboarding/arquitectura).
**Esfuerzo:** M (reescritura de COMBAT_ARCHITECTURE; S para los demás).

- `COMBAT_ARCHITECTURE.md`: reescribir con Estilo + flujo Prepare/Resolve + subsistemas M2/M3 (hooks, HealAction, PhaseBased, pipeline fin-de-combate).
- `GLOSSARY.md`: purgar entradas muertas, agregar términos M2/M3.
- `DEV_ONBOARDING.md`: HUD real, popup real, 5 commands, "Dónde mirar primero" con el Run layer (T2).
- Unificar conteo de skills a "~75+, ver `unity-tool-list`" (T6); agregar `/cierre-sesion` a WORKFLOW_GUIDE §4.

### SUB-PR 4 — Docs paquete (ii): verdad de Retazos `docs/relics-truth`

**Cierra:** D7, D8, D9, D10.
**Esfuerzo:** S.
**Coordina con:** SUB-PR 1 ✅ CERRADO — el fix YA fijó la regla de autoría real (ver abajo).
**Depende de:** decisión D-A (D9 documenta las consecuencias de la semántica).

- `RELICS.md`: corregir §217-218/228/240 con la regla de autoría **que el SUB-PR 1
  fijó (Opción B, 2026-06-14)**: bajo este diseño la **mutación directa de
  `PlayerCurrentHP` en OnCombatEnd ES CORRECTA** — `TurnManager.DispatchCombatEnd`
  sincroniza `RunState.PlayerCurrentHP/MaxHP = _player.*` ANTES de disparar los hooks,
  así RunState es autoritativo y `BattleFlowController` ya no pisa. `GrantHeal` sobre
  el actor en OnCombatEnd **NO es viable** (sería no-op: `IsCombatFinished` ya es `true`).
  Esto INVIERTE la guía vieja de D7/D8 ("mutación directa = patrón roto, GrantHeal =
  vía correcta"), que el fix refutó. Agregar la regla de orden de hooks
  (OnPlayerTurnStart/OnCombatStart).
- `m3_hooks_spec.md`: corregir [CERRADO 3] y la inconsistencia con su tabla :436; registrar el cierre de [IMPL 1] (8º dispatch de cartas neutras); subsección "Consecuencias de la semántica" tras D-A; arreglar referencias de línea corridas (citar métodos).

### SUB-PR 5 — Docs paquete (iii): números y estado `docs/numbers-and-state`

**Cierra:** D11, D12, D13, D14 + el arreglo de proceso T3 + HP (tras D-C).
**Esfuerzo:** S (mecánico).

- `_tech_snapshot.md`: 108 scripts, TurnManager 1088 LOC, purgar "Restricciones conocidas" (3 gaps resueltos), suite 146, gh CLI presente, CI parqueado, LOC corridos.
- `Combat/CLAUDE.md`: LOC actuales, agregar CombatHudView/CombatBackgroundView, **corregir D13** (efectividad bidireccional — DD-018; es lo más peligroso del paquete), sacar Heal/PhaseBased de "futuros que requieren aprobación".
- **D14 + T3 (proceso):** marcar los 4 specs (3E/3F/tint/C7) con header `> **ESTADO: IMPLEMENTADO — PR #N (fecha)**`; agregar campo Estado a la plantilla de Spec Técnico; agregar paso "marcar spec IMPLEMENTADO" a `/cierre-sesion`.
- **HP (tras D-C):** si la verdad es 70 → subir `BattleScene.unity:434` + `TurnManager.cs:26`; si es 60 → corregir GOLDEN_RULES §9 (lo toca Sebastián) + nota de decisión.

### SUB-PR 6 — Tooling: cerrar el bypass + limpieza `chore/tooling-hardening` ✅ CERRADO 2026-06-14

**Cierra:** T1, T3 (parte de proceso ya en sub-PR 5), T4, T10.
**Esfuerzo:** S.

- [x] **T1 (mayor):** `protect-files.js` reescrito con DOS superficies — (1) la de
      siempre (`Edit/Write/MultiEdit/NotebookEdit` por `file_path`) y (2) un matcher
      `Bash` que bloquea (exit 2) si el comando contiene `script-update-or-create` o
      `script-delete` junto a un path/basename protegido. Matcher de `settings.json`
      ampliado a `…|Bash`. Validado: bloquea el vector MCP sobre los 3 protegidos
      (path completo y basename, incl. backslashes) y NO rompe `tests-run`/`ping`/
      `assets-refresh` ni `script-update-or-create` sobre archivos NO protegidos.
- [x] **T1 residual (decisión de Sebastián 2026-06-14):** el propio hook
      `.claude/hooks/protect-files.js` se sumó a `PROTECTED` → cierra la auto-edición
      silenciosa del hook. Riesgo residual aceptado y documentado en el header del
      hook: un bypass exige editar `settings.json` para desenganchar (acción
      deliberada y visible en el diff). `settings.json` NO se protege (lo necesita
      el flujo `update-config`).
- [x] **T4:** 3 permisos MCP redundantes consolidados en `Bash(npx unity-mcp-cli *)`;
      `Read(//tmp/**)` eliminado; `Remove-Item`/`del`/`rd /s` agregados al deny
      (equivalentes PowerShell de `rm`).
- [x] **T10:** `model: sonnet` + `description` en frontmatter de `cierre-sesion.md`
      (resuelve también el frontmatter faltante de T6). `/fewer-permission-prompts`
      corrido sobre las 50 sesiones recientes → **sin entradas nuevas**: todo lo
      observado ya estaba allowlisted, es auto-allowed, o se excluye por regla
      (código arbitrario / mutación). El allowlist ya estaba afinado.

---

## 3. Decisiones de Sebastián fuera de PR (archivos protegidos)

Estos NO son sub-PRs de Claude — Sebastián los edita (el hook lo bloquea):

- **GOLDEN_RULES.md:** D5, D6 (texto listo en `fase3_docs.md §5`), D4/§4 (tras D-A), §4:122-125 (tras D-B), §9 HP (tras D-C), "transdimensional" (alinear con MECHANIC_DUALITY:143-149), §2 cartas WorldSwitch (asignar dueño o diferir).
- **_gdd.md:** "One More" residual, DD-010 rangos duplicados, "tres categorías", Mundo B Futurista/Cyberpunk.
- **MECHANIC_DUALITY.md:** nota de cabecera o archivar la sección "One More" (preservando §6.2).

---

## 4. Cirugía única de TurnManager (pre-M5) — NO pre-M4

> **Va aparte, con diseño aprobado, en `modo:diseno` dedicado.** Toca los 3
> archivos protegidos. La red de tests del sub-PR 2 es su contrato congelado.

**Paquete a resolver de una sola apertura** (en vez de 4 incrementales):

- **H5** — decidir y documentar el resto de la semántica encolar-vs-ejecutar:
  drain post-hook, Defeat prioritario sobre Victory, `DamageAction.Execute` sin
  chequeo de source vivo (enemigo muerto que ataca), comentario engañoso de
  `:856`. **El sub-punto del Estilo pre/post-block YA NO entra** (D-A lo cerró
  como pre-block; el cap a 5 de D-B se hace antes en el sub-PR 2).
- **H6** — firma de `TryChangeWorld` con Initiator + punto de veto (Desfase Dimensional M5, bosses M6 que bloquean el cambio).
- **H8** — sustrato de status/debuff + extraer la lógica duplicada de actors a un punto de intercepción único (Virus block 80%, Sangrado tick/expiración). **Esfuerzo L — el más grande del paquete.**
- **H9** — introducir `CurrentAct` (evita migrar el bool `ActoCompleted` después).
- **Tests asociados:** T12 (ciclo B→A, gate, modo debug). (T3 ya se cubre en el sub-PR 2.)
- **Oportunidades a absorber:** base `RelicEffectBase<THookData>`, `MaxStyleCharges` expuesto, migrar fin-de-combate a evento, `currentWorld` reset en `InitializeCombat`.

**Esfuerzo total:** M-L. **Riesgo:** alto (archivos protegidos) → de ahí la
exigencia de red de tests completa + diseño aprobado antes de abrir.

---

## 5. Dentro de M4 (roadmap existente, no remediación)

- **H7** = bloque **4c** (transdim+ancla): tipo de enemigo con `TypeWorldB`/`IsAnchor`, indirección como la del jugador. El verify confirmó que la extensión es limpia y acotada. Ya agendado.
- **Desguace de `RunFlowController`** (god-object 885 LOC + duplicación Shop↔Campfire): **antes de 4b** (EventNodeController sería la 3ª copia). Habilita el test T13 (extraer transiciones de nodo a método estático puro). Esfuerzo M.

---

## 6. Diferido / descartado

**Diferido al backlog (oportunista, sin urgencia):**
- Menores de código S sueltos: `SaveSmokeTest` (borrar gratis), clamp de `totalNodes`, hardening de `SceneTransitionManager`, residuo M17 End-Turn-durante-animación, `RelicDebugOverlay.RemoveRelic()`, counters que sangran entre combates (R-6/skill-stacker, decisión + fix simétrico).
- Recompensa post-combate no randomizada (distorsiona playtests; S-M).
- Oportunidades de refactor: `UIFactory`, `EnsureEventSystem` helper único, `EnemyIntentType` extensible.
- T11 (RunMapGenerator), T13 (transiciones de nodo) → backlog con dueño.

**Diferido por diseño (huérfanos tolerables):** rangos DD-010 (balance), categoría "De mundo", consumibles (rastreado), elite con mecánica única.

**A decidir si se adoptan o se declaran diferidos (D-D):** cartas WorldSwitch, tiers DD-009 (con su contradicción de balance explícita en el roadmap).

**Descartado (ver `AUDIT_REPORT.md §5`):** efectividad stale, softlock por cola, muerte de PlayAttackOnce, PhaseBased "sin estado", drift settings↔local, 7/10 tips de Claude Code, hook PostToolUse para de-scope, C6.

---

## 7. Orden recomendado de ejecución

1. **Pasada de decisiones** (D-A, D-B, D-C; D-D al cerrar roadmap) — corta, conversacional o `modo:diseno`.
2. **SUB-PR 1** (bugs sync + tests T1/T2) — lo más valioso, bugs player-facing.
3. **SUB-PR 6** (tooling T1) — cierra el agujero de seguridad; barato e independiente.
4. **SUB-PRs 3-5** (docs) — en paralelo a lo anterior; sanea el contexto auto-cargado antes de seguir codeando.
5. **SUB-PR 2** (resto de la red de tests) — completa el contrato.
6. **Higiene de memoria** (T5) — al cerrar la auditoría (ver nota de cierre).
7. **M4:** desguace de RunFlowController → 4a → 4b → 4c (H7).
8. **Cirugía única de TurnManager** (pre-M5) — con la red de tests como contrato.

---

*Plan propuesto 2026-06-12. Es propuesta: Sebastián aprueba alcance, orden y qué
entra al roadmap. Detalle de cada hallazgo en `AUDIT_REPORT.md`.*
