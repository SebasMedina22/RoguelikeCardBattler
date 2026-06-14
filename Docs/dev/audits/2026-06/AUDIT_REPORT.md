# Auditoría integral 2026-06 — Informe consolidado (Fase 5)

> **Sesión de cierre:** 2026-06-12 · Síntesis inline, sin fan-out.
> **Insumos:** `fase1_codigo_arquitectura.md`, `fase2_tests.md`, `fase3_docs.md`,
> `fase4_workflow_tooling.md`. Este informe consolida **solo** hallazgos que
> pasaron verificación adversarial en sus fases. Nada se re-derivó: cada ítem
> mantiene su `archivo:línea` y su veredicto original.
> **Plan de remediación:** `PLAN_PRE_M4.md` (secuencia, sub-PRs, qué entra a M4 /
> qué a la cirugía pre-M5 / qué se descarta).

---

## 0. Veredicto global

El proyecto está **estructuralmente sano**: capas respetadas, `ActionQueue`
determinista, generador de mapa sin RNG no seedeado, presentación disciplinada,
suite de 146 tests sólida para fórmulas y determinismo. **Cero hallazgos
críticos** sobrevivieron el verify en las 4 fases.

El riesgo real se concentra en cuatro frentes, ninguno urgente pero todos
convenientes de cerrar **antes de M4/M5**:

1. **Sincronización RunState↔combate al cierre** — 2 Retazos rotos HOY en el
   flujo real + retry de derrota inutilizable. Bugs player-facing, fix barato.
2. **Cuatro supuestos del core que M5/M6 rompen de frente** — todos en archivos
   protegidos → conviene **una cirugía única diseñada y aprobada de TurnManager
   pre-M5**, no cuatro aperturas incrementales.
3. **La documentación describe el juego de hace dos milestones** — Momentum
   (eliminado en M2) vigente en 5 docs; specs de Retazos prescriben el patrón
   que CAUSA los bugs del frente 1. Envenena el contexto auto-cargado y el
   onboarding.
4. **Tooling con un bypass real de la protección de archivos** + carga muerta
   de skills MCP + ruido de permisos.

**Total consolidado:** 0 críticos · **13 hallazgos mayores** (10 código + 3
tooling) + **14 mayores documentales** consolidados (18 hallazgos originales
pre-consolidación, ver F3) · ~36 menores · **8 oportunidades** · ~9 descartados
por verify.

---

## 1. Hallazgos de CÓDIGO (Fase 1)

### Cluster A — Sincronización fin de combate / resets de run

| ID | Severidad | Esfuerzo | Hallazgo | Ubicación | Destino |
|---|---|---|---|---|---|
| **H1** | Mayor | S | `RelicEndHealEffect` (R-END-2 "Curita con Estampa") es un **no-op**: escribe `RunState.PlayerCurrentHP` en `DispatchCombatEnd`, pero `ReportOutcome` (polling posterior) lo pisa con `_turnManager.PlayerHP`. Tests verdes porque no cubren el flujo con `BattleFlowController`. Origen: `RELICS.md:228` prescribe esta implementación. | `BattleFlowController.cs:117` | **Pre-M4** (fix + test T1) |
| **H2** | Mayor | S (con H1) | `RelicElitePuristEffect` ("Caja Intacta") evalúa **HP stale**: lee `RunState.PlayerCurrentHP` en OnCombatEnd, que conserva el valor PRE-combate. Paga si ENTRASTE con vida llena, no si TERMINASTE — condición invertida. Mismo origen que H1 (`RELICS.md:240`). | `RelicElitePuristEffect.cs:17` | **Pre-M4** (con H1) |
| **H3** | Mayor | S | Reintentar tras derrota arranca el combate con **1 HP**: la derrota guarda `PlayerCurrentHP=0`, el botón Reintentar limpia flags pero no restaura HP, `HasPlayerHPInitialized` sigue true → el 0 pasa como override y se clampa a 1. Loop de derrota casi garantizado. | `RunFlowController.cs:~651` | **Pre-M4** (fix + test T2) |
| **H4** | Mayor | S-M | Dos paths de reset de run **divergentes**: los botones defeat/acto resetean in-place y pierden identidad del run (tipos a Rojo/Amarillo, `PendingStarterCard` anulada, mismo mapa). `RunSession.ResetForNewRun` sí regenera. TODO stale en `:701` ("cuando exista MainMenuScene" — existe hace meses). | `RunFlowController.cs:666,693` | **Pre-M4** |

> Raíz compartida del cluster: `BattleFlowController.ReportOutcome` sincroniza
> tarde y los resets in-place duplican lógica de `RunSession`. Los 4 se arreglan
> en el mismo sub-PR coordinado con los tests T1/T2 de F2.

### Cluster B — Readiness M5/M6 (los cuatro supuestos del core)

> Los cuatro viven en TurnManager/actors (**protegidos**). Recomendación del
> verify: **una cirugía única aprobada pre-M5** que resuelva el paquete completo.

| ID | Severidad | Esfuerzo | Hallazgo | Ubicación | Destino |
|---|---|---|---|---|---|
| **H5** | Mayor | M (decisión) | Semántica **encolar-vs-ejecutar** sin decidir: efectividad/Estilo/hooks corren al encolar, el daño al ejecutar. Consecuencias no documentadas — enemigo muerto que igual ataca (`DamageAction.Execute` no chequea source vivo), Victory evaluada antes que Defeat (jugador en 0 HP puede ganar), +1 Estilo otorgado pre-block (SuperEficaz 100% bloqueado da carga), Spines encola represalia antes del golpe. Edge angosto hoy; con más Retazos/bosses M5 deja de serlo. | `TurnManager.cs:1065,634,856` | **Cirugía pre-M5** (decidir semántica) |
| **H6** | Mayor | M | Cambio de mundo: un solo path, **presupuestado al jugador, sin initiator ni veto**. `TryChangeWorld` siempre chequea cap del jugador, siempre incrementa el contador, dispatchea OnWorldSwitch DESPUÉS de mutar (sin punto de veto). Sin Initiator → el Desfase Dimensional M5 dispararía los 4 Retazos Switch del jugador en cambios hostiles; los bosses M6 que bloquean el cambio no tienen dónde vetar. | `TurnManager.cs:763-786`, `WorldSwitchHookData.cs:14` | **Cirugía pre-M5** |
| **H7** | Mayor | M | Tipo de enemigo **único, estático, leído del SO**; sin `TypeWorldB` ni `IsAnchor`. El jugador tiene la indirección correcta (`PlayerActiveType` deriva del mundo); el enemigo no — 3 call sites leen crudo del SO. **Es exactamente el bloque 4c del roadmap** — la extensión es limpia y acotada. | `TurnManager.cs:624,416,112`, `EnemyDefinition.cs:22` | **M4 bloque 4c** |
| **H8** | Mayor | L | **Cero sustrato de status/debuff** + lógica de actors duplicada textualmente. `EffectType.ApplyStatus` cae al default LogWarning; ningún actor tiene contenedor de statuses; TakeDamage/GainBlock/LoseBlock/Heal son copias idénticas en ambos actors. Virus (block al 80%) y Sangrado (tick/expiración) son núcleo de M5. | `ICombatActor.cs`, `PlayerCombatActor.cs:97-110` vs `EnemyCombatActor.cs:35-48` | **Cirugía pre-M5** |
| **H9** | Mayor | M | **Multi-acto sin representación**: `ActoCompleted` es bool, config single-act (un solo `defaultEnemy`/`bossRelicDrop`, HP enemigo fijo sin escalado). Introducir `CurrentAct` ya en M5 evita migrar el bool después. | `RunState.cs:32`, `RunCombatConfig.cs` | **Cirugía pre-M5** (anticipar) |

### Cluster C — Fragilidad latente de inicialización

| ID | Severidad | Esfuerzo | Hallazgo | Ubicación | Destino |
|---|---|---|---|---|---|
| **H10** | Mayor | S | **Carrera de Awake** en run nueva: `TryInheritBossFrom` regenera el mapa que `RunFlowController` ya pudo capturar. HOY funciona por orden de facto en el YAML de la escena — **sin contrato** (no hay DefaultExecutionOrder; reordenar la jerarquía rompe el mapa en silencio). | `RunSession.cs:147`, `RunFlowController.cs:71` | **Pre-M4** (lazy getter o DefaultExecutionOrder + comentario) |

### Menores de código (al backlog, sin urgencia)

- **Docs stale sobre gaps resueltos** (PhaseBased AI, `EffectType.Heal`+`HealAction`, `CalculateIntentValue`): SÍ implementados (Sub-PR D, 2026-05-07) pero listados como pendientes → **insumo directo de F3** (D11). *(menor, S)*
- `RelicTurnEnergyEveryN` (Reloj de Cocina): counter **sangra entre combates** (sin reset OnCombatStart, a diferencia de siblings). Resolver como decisión de diseño + fix simétrico. *(menor, S)*
- `RelicAccSkillStacker`: pending de energía cruza combates (mismo fix). *(menor, S)*
- Sin validación SO↔efecto: el reset de counters depende del array Hooks del asset → test que cargue `.asset` reales (= T5 de F2). *(menor, S)*
- Orden de hooks T1 (OnPlayerTurnStart antes que OnCombatStart): decisión documentada, footgun para Retazos futuros — documentar como regla de autoría en RELICS.md. *(menor, S)*
- `SaveSmokeTest` vivo en BattleScene (sin `#if UNITY_EDITOR`, corre en builds, sobreescribe `save_v1.json` cada combate). Inocuo hoy; mayor cuando M6c lea ese archivo. **Borrarlo es gratis.** *(menor, S)*
- Topologías hardcode 8 nodos vs `totalNodes` editable sin clamp/OnValidate. Validación de 5 líneas. *(menor, S)*
- `SceneTransitionManager`: `_isTransitioning` sin recuperación — soft-lock no alcanzable hoy, pero sin hardening un KillAll futuro lo rompe sin síntoma. *(menor, S)*
- Comentario engañoso `TurnManager.cs:856` (promete detección de victoria post-hook que no ocurre) — trampa para autores futuros. *(menor, S — absorbido por la cirugía H5)*
- `currentWorld` no se resetea en `InitializeCombat` (lo salva la recarga de escena; muerde si M6 encadena combates sin recarga). *(menor, S)*
- Residuo de M17: End Turn no chequea `IsPlayingAttack` → clic durante animación difiere la resolución de la carta al turno enemigo. *(menor, S)*
- `RelicDebugOverlay` muta `RunState.Relics` directo → AcquisitionOrder duplicado en playtesting (editor-only). Fix: `RemoveRelic()` que normalice. *(menor, S)*
- Recompensa post-combate **NO randomizada** (siempre las primeras N del RewardPool, sin TODO); oro +10 hardcodeado. Distorsiona playtests de economía. *(menor, S-M)*
- `RunFlowController` **god-object** (885 LOC) + duplicación espejo Shop↔Campfire (~150-180 LOC/archivo; `GetWhiteSprite` ×4). Sin riesgo funcional hoy — **extraer ANTES de 4b** (EventNodeController sería la 3ª copia). *(menor, M)*

### Oportunidades de código

- Migrar fin-de-combate de polling a evento (`OnCombatFinished`) — nota de diseño pre-M5, sin bug actual.
- `UIFactory` central en Core/UI (patrón create-text/button/panel forkeado ×4+).
- `EnemyIntentType` extensible + íconos (gap del enum, no de la view).
- `EnsureEventSystem` ×3 divergentes → helper único en Core.
- Base `RelicEffectBase<THookData>` antes de la ola de efectos M5/M6.
- `MaxStyleCharges` expuesto por TurnManager (HUD hardcodea "/5").

---

## 2. Hallazgos de TESTS (Fase 2)

La suite (146 tests, 17 archivos) es **100% sintética** — cero `.asset` reales,
cero `AssetDatabase`. Excelente red para fórmulas y determinismo; **casi nula
para integración, flujo y datos reales**. Los agujeros se concentran donde viven
los bugs de F1.

### Top 8 tests (orden = riesgo eliminado / esfuerzo)

Los **3 primeros documentan bugs reales de F1 como tests que HOY FALLAN** —
ejecutables, no narrativa. Van **antes** de la cirugía: los que pasan congelan
el comportamiento que la cirugía no debe romper; los que fallan son la spec
ejecutable de los fixes.

| # | Test propuesto | Agujero | Esfuerzo | Estado hoy | Destino |
|---|---|---|---|---|---|
| 1 | `ReportOutcome_AfterCombatEndHook_PreservesRelicHeal` | T1 (H1/H2) | M | **FALLA** = bug confirmado | **Pre-M4**, con fix H1/H2 |
| 2 | `RunState_AfterDefeatWithZeroHp_RetryPathRestoresPlayableHp` | T2 (H3) | S | **FALLA**; puro RunState | **Pre-M4**, con fix H3 |
| 3 | `StyleCharge_SuperEffectiveHitFullyBlocked_DoesNotGrantCharge` | T3 (H5) | S | depende de decisión | **Cirugía pre-M5** (fuerza decisión pre/post-block) |
| 4 | `RelicAssets_AllDeclaredHooksAreDispatchable` | T5 | S | falta | **Pre-M4** (única validación datos↔código) |
| 5 | `RelicGrantEffects_OnRealTurnManager_MutateState` (×10 param.) | T6 | M | falta | **Pre-M4** (media tabla de relics gana asserts) |
| 6 | `GrantStyleCharge_Overshoot_ResetsAndGrantsSingleBonus` | T7 | S | falta | **Pre-M4** (invariante GOLDEN_RULES §4) |
| 7 | `InitializeCombat_SecondCall_ResetsStyleStateAndRelicCounters` | T4 | S | falta | **Pre-M4** (congela contrato pre-cirugía) |
| 8 | `NeutralCard_Applies90PercentDamage` | T9 | S | falta | **Pre-M4** (regla de balance viva) |

Suplentes con apetito: **T8** `InitializeDeck_CalledTwice_DoesNotDuplicateStarterCard` (S), **T10** round-trip de tipos NewRun→combate (M).

### Backlog de tests (con dueño)

- **T11** RunMapGenerator: aciclicidad solo por rango, `AssignBossAct1` sin test, DepthPools múltiples nunca ejercitados. *(M)*
- **T12** `TryChangeWorld`: ciclo B→A sin test, gate `_initialized==false` sin test, assert del modo debug que no valida nada. → **junto a la cirugía H6 pre-M5**. *(S)*
- **T13** Transiciones de nodo post-victoria + ciclo `LastNodeOutcome`. Requiere **extraer la lógica a método estático puro primero** → junto al desguace de RunFlowController antes de 4b. *(L con refactor; S después)*

---

## 3. Hallazgos DOCUMENTALES (Fase 3)

Problema dominante y sistémico: **la documentación describe el juego de hace dos
milestones.** Ningún hallazgo es bug de código nuevo; el riesgo es de **proceso**
— docs canónicos que inducen errores a futuro y contexto auto-cargado que
describe sistemas muertos.

> **GOLDEN_RULES.md y _gdd.md son archivos de Sebastián** (el hook bloquea a
> Claude). El informe los marca pero **Sebastián los edita**; el texto propuesto
> ya está en `fase3_docs.md §5`.

### Cluster 1 — Momentum/One More: mecánica muerta documentada como vigente

| ID | Sev | Esf | Hallazgo | Destino |
|---|---|---|---|---|
| **D1** | Mayor | M | `COMBAT_ARCHITECTURE`, `GLOSSARY`, `DEV_ONBOARDING` describen Momentum/FreePlays como sistema vigente (diagrama PlayCard, entradas de glosario, "HUD con Momentum"). Realidad: grep da 1 match — el comentario que afirma su ausencia. HUD real `"Estilo: X/5"`. | **Paquete docs (i)** |
| **D2** | Mayor | M | `COMBAT_ARCHITECTURE` no menciona NINGÚN subsistema M2/M3 (hooks de Retazos, Contador de Estilo, HealAction, PhaseBased, pipeline de fin de combate). Estructuralmente vencido. | **Paquete docs (i)** (mismo PR) |
| **D3** | Mayor | S | `GLOSSARY` congelado pre-M2: 21 entradas solo-M1, cero términos M2/M3, mantiene 2 entradas de sistema muerto. | **Paquete docs (i)** |

### Cluster 2 — GOLDEN_RULES vencido o ambiguo (Sebastián edita)

| ID | Sev | Esf | Hallazgo | Destino |
|---|---|---|---|---|
| **D4** | Mayor | — | §4:119 "+1 carga cuando hace daño SuperEficaz" no especifica pre/post-block; el código otorga **PRE-block**. **Es DECISIÓN de diseño** (ligada a H5). | **Decisión de Sebastián** → si post-block, entra a cirugía pre-M5 |
| **D5** | Mayor | S | Tres ⏳ completamente vencidos: PhaseBased (M2-D), Shop (M3-3D), Campfire (M3-3C). | **Sebastián** (texto en F3 §5) |
| **D6** | Mayor | S | §10:268 "pendiente, ver DD-017" — DD-017 cerrada 2026-05-07 (opción C); se shipearon 4 Retazos de cambio. | **Sebastián** (F3 §5) |

### Cluster 3 — Docs de Retazos prescriben el patrón roto (raíz documental de H1/H2)

| ID | Sev | Esf | Hallazgo | Destino |
|---|---|---|---|---|
| **D7** | Mayor | S | `RELICS.md:217-218,228,240` llama "vía segura" a la mutación directa de `PlayerCurrentHP` en OnCombatEnd = la receta literal de H1. Ironía: menciona GrantHeal como alternativa y la descarta — es exactamente al revés. | **Paquete docs (ii)**, con fix H1/H2 |
| **D8** | Mayor | S | `m3_hooks_spec.md:364-368` [CERRADO 3] prescribe el mismo patrón; internamente inconsistente (su tabla :436 usa GrantHeal correcto). | **Paquete docs (ii)** |
| **D9** | Mayor | S | El spec documenta la semántica encolar-vs-ejecutar pero NO sus dos consecuencias (Estilo pre-block; acciones encoladas se ejecutan aunque el actor muera). Componentes de H5. | **Paquete docs (ii)**, tras decisión D4/H5 |
| **D10** | Mayor | S | `m3_hooks_spec.md:607-614` [IMPL 1] afirma que las cartas neutras NO dispatchean OnDamageDealt y "Reabrir en 3B" — la reapertura ocurrió y se implementó (8º dispatch); el spec nunca registró el cierre. | **Paquete docs (ii)** |

### Cluster 4 — Docs técnicos con números/gaps fantasma

| ID | Sev | Esf | Hallazgo | Destino |
|---|---|---|---|---|
| **D11** | Mayor | S | `_tech_snapshot`: "53 archivos C#" (real 108); TurnManager "735 loc" y "~960" contradiciéndose (real 1088); "Restricciones conocidas" lista 3 gaps resueltos hace un mes. | **Paquete docs (iii)** |
| **D12** | Mayor | S | `Combat/CLAUDE.md:13-22`: 4 LOC vencidos (TurnManager 735 vs 1088) y omite CombatHudView/CombatBackgroundView. Contexto auto-cargado. | **Paquete docs (iii)** |
| **D13** | Mayor | S | `Combat/CLAUDE.md:85-86` "Efectividad SOLO al daño del jugador... NUNCA al enemigo" **contradice DD-018** (bidireccional), GOLDEN_RULES §3 y el código. Inconsistencia interna a 2 líneas. Peligroso: es lo que un implementador lee antes de tocar combate. | **Paquete docs (iii)** (prioridad alta del paquete) |

### Cluster 5 — Specs sin cerrar

| ID | Sev | Esf | Hallazgo | Destino |
|---|---|---|---|---|
| **D14** | Mayor | S | Los 4 specs restantes (3E, 3F, tint, C7-arte) están **100% implementados** pero se presentan como "listo para implementación" con handoffs vigentes — riesgo de tomar un handoff como tarea pendiente. | **Paquete docs (iii)** + arreglo de proceso T3/F4 |

### Barrido ⏳ — huérfanos netos (regla cerrada por diseño sin sub-tarea dueña)

- **Cartas con efecto WorldSwitch (§2):** Retazos ✓ (M3) pero las cartas no; `EffectType` sin WorldSwitch; ningún bloque del roadmap lo agenda. → **adoptar o declarar diferido**.
- **Tiers de economía DD-009 (§8):** un solo `goldReward=10`; cartas de oro no existen. **Agravante: el balance de tienda 2026-06-04 se calibró asumiendo ~10 oro/nodo — la economía vigente contradice los tiers del doc.** → **adoptar o declarar diferido (con la contradicción explícita)**.
- Huérfanos tolerables/diferidos: rangos DD-010 (balance), categoría "De mundo", consumibles (rastreado), elite con mecánica única (micro).

### 🆕 HP base: discrepancia confirmada (cabo suelto de F3, resuelto en F5)

| Sev | Esf | Hallazgo | Destino |
|---|---|---|---|
| Menor | S | **GOLDEN_RULES §9 dice "HP base 70"; el juego corre con 60.** Verificado: `BattleScene.unity:434` serializa `playerMaxHP: 60`, coincidiendo con el default de `TurnManager.cs:26`. No es drift de un solo lado — código y escena concuerdan en 60; **el doc es el que está vencido, o hubo una decisión de balance no registrada.** | **Sebastián decide** la verdad (subir escena+código a 70, o corregir §9 a 60). Si es 60 deliberado → nota en GOLDEN_RULES |

### Menores documentales (al backlog de la pasada de docs)

- **GDD (Sebastián):** "One More" residual (:6, :115); DD-010 con rangos contradictorios duplicados; "tres categorías" donde DD-014 cerró 2; Mundo B "Futurista" vs "Cyberpunk" (inconsistencia interna).
- **MECHANIC_DUALITY.md** se autodenomina "biblia activa" con sección "One More". OJO: su §6.2 tiene la ÚNICA definición buena de "transdimensional".
- **"Transdimensional" con 3 usos inconsistentes** — alinear GOLDEN_RULES §6 y GLOSSARY con la definición existente de MECHANIC_DUALITY:143-149 (no crear una nueva).
- **GOLDEN_RULES §4:122-125** — no especifica el destino del contador con bonus pendiente; el código acumula >5 sin reset ni clamp. Decidir: ¿acumula o capea?
- **_tech_snapshot menores:** "suite 142/142" (header dice 146); "No gh CLI" (gh 2.92.0 usado en #96-#108); "No CI/CD" (impreciso, tests.yml parqueado); LOC menores corridos.
- **Combat/CLAUDE.md:37-39** lista como "futuros que requieren aprobación" dos ya implementados (Heal, PhaseBased).
- **m3_hooks_spec:** 5 referencias de línea corridas (citar métodos, no líneas); naming de payloads divergió.
- **Verificación positiva:** DD-022/023 SÍ están pegados en DESIGN_DECISIONS (en disco, **sin commitear**).

---

## 4. Hallazgos de WORKFLOW / TOOLING (Fase 4)

| ID | Sev | Esf | Hallazgo | Destino |
|---|---|---|---|---|
| **T1** | Mayor | S | **Bypass real del hook protect-files.** El hook solo engancha `Edit\|Write\|MultiEdit\|NotebookEdit`; `Bash(npx unity-mcp-cli *)` está auto-aprobado y permite `script-update-or-create`/`script-delete` del plugin MCP, que **sobreescriben `.cs` en disco — incluido TurnManager.cs — sin prompt ni hook.** | **Pre-M4** (matcher Bash en el hook + T9 cierra el otro lado) |
| **T2** | Mayor | S | `DEV_ONBOARDING.md` describe el juego pre-M2/M3: Momentum en el HUD, popup "MOMENTUM +1", solo 4 commands (falta `/cierre-sesion`), "Dónde mirar primero" 100% combate (ignora el Run layer post-M3). | **Paquete docs (i)** (mismo PR que D1-D3) |
| **T3** | Mayor (proceso) | S | El paso "marcar spec como IMPLEMENTADO" (D14) **no tiene dueño**: ni la plantilla de Spec Técnico tiene campo Estado, ni `/cierre-sesion` ni `_modo_implementacion` lo mencionan. | **Pre-M4** (editar cierre-sesion + plantilla + marcar 4 specs retro) |
| **T4** | Menor | S | Ruido en `settings.json`: 3 permisos MCP redundantes superpuestos, `Read(//tmp/**)` de otra plataforma, deny solo cubre `rm` (falta `Remove-Item`/`del`/`rd` de PowerShell). | **PR de tooling** |
| **T5** | Menor | S | Higiene de memoria: 2 huérfanas no indexadas (`project_next_phase.md`, `project_newrun_3e_spec.md`); MEMORY.md (~10 KB) viola su regla "1 línea por memoria" (la sección "Estado actual" son párrafos largos). | **Higiene al cierre de auditoría** |
| **T6** | Menor | S | Conteos de tools inconsistentes (77 reales vs "~70" WORKFLOW_GUIDE vs "~73" DEV_ONBOARDING); `/cierre-sesion` omitido en WORKFLOW_GUIDE §4; `cierre-sesion.md` sin frontmatter. | **Paquete docs (i)** (mismo PR que T2) |
| **T9** | Oportunidad | S | **~36 de 77 skills Unity-MCP son carga muerta desactivable** vía `tool-set-enabled-state` (profiler de control, package-*, mutación de assets/prefabs/gameobjects, script-read/delete/update-or-create…). NO borrar carpetas (se regeneran). **Cierra parcialmente T1** (deshabilitar script-update-or-create mata el vector principal). | **Decisión de Sebastián** (1 llamada, reversible) |
| **T10** | Oportunidad | S | `model: sonnet` en frontmatter de `cierre-sesion.md` (automatiza el hábito manual + resuelve T6); `/fewer-permission-prompts` para curar el allowlist objetivamente (insumo de T4). | **PR de tooling** |

### Sanos auditados (sin hallazgo)

`protect-files.js` (lógica interna correcta; el único agujero es la superficie
T1); los 7 archivos de `Docs/dev/modes/`; los 4 commands `modo-*`; el
`.gitignore` de `.claude/`; `settings.local.json` (1 regla coherente, **sin drift**).

---

## 5. Descartados por el verify (consolidado)

### De Fase 1 (código)
- **Efectividad stale si el mundo cambia entre prepare y resolve:** falso — la efectividad depende del elemento de la carta (fijado al preparar) y del tipo del enemigo (inmutable en combate); `currentWorld` no entra al cálculo.
- **Softlock por cola pendiente:** el guard de Update solo bloquea el AUTO end-turn; el botón End Turn drena la cola.
- **Muerte del callback de PlayAttackOnce (energía perdida/carta en limbo):** modos de muerte no alcanzables (guard `_isPlayingAttack` + botones deshabilitados + animator sin otros callers).
- **PhaseBased "sin estado" como mayor:** factualmente exacto pero ya trackeado en Future work.

### De Fase 2 (tests)
- **T4 (mitad):** el reset de Estilo/Bonus en `InitializeCombat` SÍ existe y los counters SÍ tienen test intra-combate; lo que queda sin red es el cross-combate (degradado a caracterización + documentación del counter de R-6).

### De Fase 3 (docs)
- **C6 (DispatchCombatEnd como "divergencia" del spec):** lectura forzada — el spec prescribe punto y momento de invocación y el código lo cumple literalmente; la extracción del helper es refactor interno no contradictorio.

### De Fase 4 (tooling)
- **Drift settings.json ↔ settings.local.json** (sospecha del plan): no existe — el local tiene 1 regla coherente.
- **7 de 10 tips del agente de Claude Code:** features inexistentes o mal descritas (output styles con JSON de reglas, frontmatter `paths:`, `promptCacheConfig`, `/batch`, hook `PermissionRequest` como lo describió), o de valor marginal (SessionStart con assets-refresh, PreCompact snapshot).
- **Hook PostToolUse para de-scope** de settings/.slnx: mecanismo incorrecto — la auto-modificación la hace el harness, no una tool Edit/Write, así que el hook nunca dispararía. Alternativa viable si se quiere automatizar: git pre-commit hook. Veredicto: nice-to-have; el paso manual del ritual ya lo cubre.

---

## 6. Tabla maestra de prioridad

| Frente | Items | Severidad | Esfuerzo total | Cuándo |
|---|---|---|---|---|
| **Bugs player-facing** | H1, H2, H3, H4 + tests T1, T2 | Mayor | S-M | **Pre-M4** (sub-PR 1) |
| **Red de tests pre-cirugía** | T4-T9 (tests 4-8) | — | S-M | **Pre-M4** (sub-PR 2) |
| **Verdad documental** | D1-D3, D7-D14, T2, T6, HP | Mayor (proceso) | M | **Pre-M4** (sub-PRs 3-5, paquetes i/ii/iii) |
| **Seguridad del tooling** | T1, T3, T4, T9, T10 | Mayor | S | **Pre-M4** (sub-PR 6 + decisión T9) |
| **Bloque 4c del roadmap** | H7 | Mayor | M | **M4 (ya agendado)** |
| **Cirugía única de TurnManager** | H5, H6, H8, H9 + tests T3, T12 + decisión D4 | Mayor | M-L | **Pre-M5 (diseño aprobado)** |
| **Desguace de RunFlowController** | god-object + dup Shop↔Campfire + T13 | Menor | M | **Antes de 4b** |
| **Backlog de menores** | ~20 ítems S | Menor | S c/u | Oportunista |

Detalle de secuencia, dependencias y sub-PRs en **`PLAN_PRE_M4.md`**.

---

*Fase 5 cerrada 2026-06-12. Síntesis inline (sin fan-out). Verificación del
cabo suelto: `BattleScene.unity:434` = `playerMaxHP: 60`. Consumo F5: ~6
lecturas (4 checkpoints + 2 memorias) + 2 greps + escritura de los 2
entregables; cero subagentes.*
