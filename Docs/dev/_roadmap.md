# Roadmap — RoguelikeCardBattler

> **Qué es este archivo:** fuente única de verdad sobre milestones del proyecto.
> Se actualiza al cerrar sub-tareas (checkbox `[x]`) y al cerrar milestones
> completos (mover de Activo → Completados).
>
> En `modo:implementacion` se lee al inicio para saber qué milestone está activo
> y qué sub-tareas quedan.
>
> **Fase actual:** M2 cerrado completo (motor v2 estable). Diseño de M3 cerrado.
> **Milestone activo:** M3 — Personalización del run (Retazos, Tienda, Hoguera, NewRunScene, mapa horizontal)
> **Próximo bloque:** M4 — Resto del Acto 1 (mejora cartas con UI, eventos, transdimensionales, ancla)
> **Última actualización:** 2026-05-07 (M2 cerrado; M3 activado con 6 sub-PRs acordados)

---

## Cómo se usa este archivo

**Al activar `modo:implementacion`:**
1. Claude lee este archivo para saber qué milestone está activo.
2. Lee las sub-tareas pendientes.
3. Si la tarea pedida no corresponde a una sub-tarea activa, pregunta a Sebastián
   si registrar como nueva en el roadmap o como nota fuera de roadmap.

**Al cerrar una sub-tarea:**
- Marcar checkbox `[x]`
- Agregar fecha de cierre si es significativo
- Mover a la siguiente sub-tarea

**Al cerrar un milestone completo:**
1. Mover de "Activo" a "Completados" con fecha y resumen.
2. Activar el siguiente milestone pendiente.
3. Si hay aprendizajes, registrar en `_insights.md`.

**Reglas:**
- NO agregar tareas fuera de milestones aquí.
- Las dependencias son duras: no activar milestones sin terminar los previos.
- Si una tarea técnica surge a mitad de implementación y no encaja en el milestone
  activo, registrarla como sub-tarea del milestone pertinente o crear un milestone
  nuevo.
- Mantener este archivo al día CONFORME se avanza, no en lotes.

---

## Activo

### M3 — Personalización del run

**Objetivo:** entregar el ecosistema completo que convierte una secuencia de combates
en un run con identidad: Retazos como sistema base de pasivos, Tienda y Hoguera
funcionales como nodos del mapa, NewRunScene como flujo inicial (elección de tipos +
draft de carta especial dual), y mapa con scroll horizontal según GDD.

**Por qué bundle:** los 4 sistemas comparten una capa de infraestructura nueva
(sistema de hooks/dispatcher de Retazos). Hacer cada sistema en milestones separados
implicaría diseñar la infra varias veces o duplicar código. M3 instala la base + 4
features que la usan + 1 refactor de UX (mapa horizontal).

**Decisiones cerradas durante diseño (2026-05-07):**
- DD-017 → opción C: 2-3 Retazos de cambio en contenido base como demo de la categoría.
- Tienda + Hoguera = paneles sobre RunScene (no escenas nuevas) con parallax 2D.
- Hoguera con opciones extensibles vía hooks (Retazos pueden agregar opciones tipo
  "excavar para encontrar Retazo común").
- Tienda con stock extensible vía hooks (Retazos pueden modificar stock/precios).
- Consumibles diferidos: no hay sistema base, se decide en futura sesión si entran
  a M3 o M4.
- NewRunScene = escena dedicada (no canvas en MainMenu) entre MainMenu y RunScene.
  Razón: cambio de contexto (música, fondo, atmósfera) refuerza el "estoy empezando
  algo"; mantiene MainMenu liviano para meta-progresión futura.
- Mapa horizontal incluido en M3 como refactor del scroll vertical actual (DD-005).
- Inventario de Retazos = fila de iconos en HUD (estilo StS), visible siempre en
  combate y mapa para reforzar identidad de build.

**Diseño de Retazos:** las 6 categorías de hook definidas en Insight 3 de
`_insights.md` son la base. Se diseñan partiendo de "qué evento del combate
disparan", no de "qué efecto se nos ocurre".

**Sub-PRs (orden):**

#### Sub-PR 3A — Foundations: hooks + dispatcher de Retazos
- [x] **PASO 1 (sin código):** sesión `modo:diseno` dedicada al spec de hooks
      (output: `Docs/dev/specs/m3_hooks_spec.md`). Define cada hook, signature,
      datos que pasa, orden de ejecución, interacción con ActionQueue. **NO codear
      hasta que el spec esté cerrado.**
- [x] `RelicDefinition` SO + `IRelicEffect` interface + `RelicHook` enum
- [x] `RelicHookDispatcher`: bus que TurnManager invoca en eventos clave
- [x] Hooks expuestos (mínimo): OnCombatStart, OnPlayerTurnStart, OnDamageDealt,
      OnDamageTaken, OnWorldSwitch, OnCombatEnd, OnCardPlayed,
      OnCampfireOptionsBuilt, OnShopStockBuilt — los 7 de combate con payload;
      los 2 de nodos solo declarados (call sites en 3C/3D, payloads diferidos)
- [x] `RunState.Relics: List<RelicInstance>` + reset
- [x] `RunSession.RelicDispatcher`: instanciado en Awake, persiste con la run
- [x] `RelicHookContext` + API limitada (7 métodos: GrantBlock, GrantHeal,
      GrantDrawCards, GrantEnergy, GrantStyleCharge, GrantBonusWorldSwitch,
      EnqueueExtraDamage) con guards Victory/Defeat
- [x] TurnManager: 7 invocaciones + 7 métodos `internal Relic*` + extracción
      `IncrementStyleCharges` (golden rule §4 con una sola fuente de verdad)
- [x] Tests EditMode: dispatcher invoca hooks en orden correcto, sin Retazos no
      rompe nada, suscripciones múltiples se ejecutan en orden de adquisición
      (8 casos en `RelicHookDispatcherTests.cs`)
- **Toca archivos protegidos:** **SÍ** — TurnManager. **APROBACIÓN DADA** (7 puntos
  + extracción `IncrementStyleCharges` aprobados explícitamente por Sebastián).
- **5 campos de payload diferidos** (ver Insight 4): `TurnNumber`, `TurnsTaken`,
  `WasFreeSwitch`, `IsBoss`, `IsElite`. Se reincorporan en la sub-PR donde un
  Retazo concreto los justifique + cableen el dato fuente.

#### Sub-PR 3B — Retazos: contenido placeholder + UI inventario
- [ ] Diseño formal de ~23 Retazos placeholder (sesión `modo:diseno` específica)
  - 15 neutros distribuidos por las 6 categorías de Insight 3
  - 5 de Elite (drop garantizado al ganar Elite)
  - 1 de Boss (drop tras vencer BossAct1, vinculado narrativamente)
  - 2-3 de cambio (DD-017 opción C)
- [ ] `RelicInventoryView`: fila de iconos en HUD de combate y de RunMapView
      (mismo lugar en ambas vistas)
- [ ] Tooltip al hover/tap con nombre + descripción + texto narrativo
- [ ] Pulse + brillo cuando se obtiene uno nuevo (game feel barato, alto impacto)
- [ ] Hook de drop al ganar Elite/Boss
- [ ] Tests EditMode: cada Retazo aplica su efecto en el hook correcto

#### Sub-PR 3C — Hoguera (Campfire)
- [ ] `CampfireNodeController` + `CampfireView` (panel sobre RunScene)
- [ ] Opciones base: **Descansar** (heal X HP) | **Mejorar carta** (1 carta del mazo)
- [ ] Mejora de cartas: toggle `IsUpgraded` en `CardDeckEntry` + campos upgraded
      en `CardDefinition` (DD-013: cartas duales upgrade ambos lados)
- [ ] Hook `OnCampfireOptionsBuilt` para que Retazos puedan agregar opciones
- [ ] Parallax 2-3 capas placeholder (cielo nocturno + montañas + tronco con fuego)
- [ ] Música ambiente distinta a combate
- [ ] Tests: heal aplica HP correcto; upgrade marca flag y respeta dualidad;
      opciones extra de Retazos aparecen en la lista

#### Sub-PR 3D — Tienda (Shop)
- [ ] `ShopNodeController` + `ShopView` (panel sobre RunScene)
- [ ] Stock generado por seed: cartas, Retazos, opción "Eliminar carta del mazo"
- [ ] Filtrado de cartas por los 2 tipos elegidos al inicio del run
- [ ] Hook `OnShopStockBuilt` para que Retazos puedan modificar stock/precios
- [ ] Economía: precios desde SO config; gold del RunState
- [ ] Parallax 2-3 capas placeholder (pared del local + estanterías + mostrador)
- [ ] Vendedor con personalidad por mundo (placeholder ahora, arte después)
- [ ] Consumibles **diferidos** (decidir en sesión futura si entran o se van a M4)
- [ ] Tests: stock determinista por seed; compra resta gold y agrega item;
      remove carta funciona; precios modificados por Retazos respetan tope mínimo

#### Sub-PR 3E — NewRunScene
- [ ] Escena dedicada `Assets/Scenes/NewRunScene.unity` con `NewRunController.cs`
- [ ] **Paso 1:** Selección de 2 tipos elementales (uno por mundo)
- [ ] **Paso 2:** Draft de carta especial dual (6 opciones filtradas: 3 Mundo A
      + 3 Mundo B según tipos elegidos en Paso 1, DD-020)
- [ ] **Paso 3:** Confirmación + carga de RunScene
- [ ] Feel: cards entrando con fade desde sus mundos, texto de peso ("esta carta
      te acompañará todo el run"), sonido al confirmar
- [ ] La carta seleccionada se inyecta en el mazo inicial del run
- [ ] Botón volver a MainMenu en cada paso
- [ ] Tests: draft genera 6 opciones válidas filtradas por tipos; carta
      seleccionada entra al deck; cancelar vuelve a MainMenu sin estado sucio

#### Sub-PR 3F — Mapa horizontal (refactor scroll)
- [ ] Refactor `RunMapView` para scroll lateral (izquierda → derecha) por DD-005
- [ ] Adaptar generación de `RunMapGenerator` a layout horizontal
- [ ] Adaptar UI de nodos y aristas (`RunMapNodeView`, `RunMapEdgeView`) al
      nuevo eje
- [ ] `RelicInventoryView` integrado en HUD del mapa (consistencia con HUD de
      combate)
- [ ] Tests: misma seed = mismo mapa (no romper determinismo en el refactor)

**Sistemas afectados:** TurnManager (hooks), RunState (Relics), RunFlowController
(NewRunScene flow + Tienda/Hoguera nodes), RunMapView/Generator (horizontal),
CombatUIController (RelicInventoryView). Nuevos: RelicHookDispatcher,
RelicDefinition, IRelicEffect, NewRunController, CampfireNodeController,
ShopNodeController, RelicInventoryView.

**Toca archivos protegidos:** **SÍ** — TurnManager (hooks en eventos clave).
**REQUIERE APROBACIÓN EXPLÍCITA** antes de empezar implementación de Sub-PR 3A.

**Dependencias:** M2 cerrado ✅ (motor de combate v2 estable; hooks pueden
colgarse en eventos confiables como Contador de Estilo, daño bidireccional,
HealAction, PhaseBased AI).

**Validación obligatoria por sub-PR:**
- Compilación de Unity sin errores antes de marcar como mergeable.
- Tests EditMode en verde.
- Validación visual en la escena correspondiente (combate / hoguera / tienda /
  new run / mapa).

**Complejidad:** alta. Sub-PR 3A es la más crítica arquitectónicamente; el resto
depende de que su contrato esté bien definido (lección reforzada por Insight 3).

**Criterios de cierre M3:** todos los sub-PRs mergeados + run completo jugable
de inicio a fin (NewRunScene → mapa horizontal → combates con Retazos visibles
y activos → Tienda y Hoguera funcionales → Boss con drop de Retazo único +
zero console errors + tests EditMode al 100%.

---

## Pendientes

### M4 — Resto del Acto 1 según GDD v2

**Objetivo:** cerrar el contenido del Acto 1: mejora de cartas, eventos básicos
y multidimensionales, enemigos transdimensionales y enemigos ancla.

**Sub-tareas (desagregar al activar):**
- Mejora de cartas (DD-013): toggle `IsUpgraded` en `CardDeckEntry`, campos
  upgraded en `CardDefinition`, mejora ambos lados de cartas duales, UI
- Eventos (DD-005): `EventDefinition` SO, controller, choice UI, sistema de
  decisiones, eventos multidimensionales (elegir mundo)
- Quests con MCguffin (DD-021 se cierra aquí): tracking en RunState, marca de
  nodo destino en mapa, eventos de aceptar/robar
- Enemigos transdimensionales (DD-014): campo `TypeWorldB` en `EnemyDefinition`,
  resolución de tipo activo según `RunState.CurrentWorld`
- Enemigos ancla (DD-014): flag `IsAnchor` en `EnemyDefinition`, bypass del
  cambio de mundo en lógica

**Toca archivos protegidos:** sí (TurnManager para tipo activo de transdim y
ancla).

**Dependencias:** M3 cerrado (eventos pueden otorgar Retazos / mejoras y
necesitan el sistema funcionando).

**Complejidad:** alta. El sistema de eventos con quests es trabajo significativo.

---

### M5 — Bosses con fases + Boss Acto 1 según GDD v2

**Objetivo:** rediseñar el Boss del Acto 1 (Costura Maldita / UNIT-RB7) según
DD-004 con fases, debuffs únicos y mecánica "Desfase Dimensional". Establecer
el patrón para futuros bosses.

**Sub-tareas (desagregar al activar):**
- Sistema de fases en bosses (Fase 2 obligatoria al 50% HP)
- Mecánica "Desfase Dimensional": contador de cartas jugadas en turno, cambio
  automático cada 3 cartas (2 en Fase 2)
- Debuff Sangrado (DD-019, exclusivo del boss medieval): pérdida de HP al jugar
  ataque
- Debuff Virus (DD-019, exclusivo del boss cyberpunk): bloqueo al 80%
  efectividad
- 2 tipos por boss (1 SuperEficaz contra el jugador, 1 debilidad)
- Retazo único de boss vinculado narrativamente

**Toca archivos protegidos:** sí (TurnManager + ActionQueue para hook
post-ProcessAll y mutación de mundo desde IA).

**Dependencias:** M2 cerrado (cambio de mundo robusto), M4 cerrado (transdim/
ancla establecen el patrón de tipos múltiples).

**Complejidad:** alta.

---

### M6 — Acto 2, Acto 3, Meta-progresión

**Objetivo:** llevar el juego a su shape final: 3 actos, dificultad escalada
según DD-011, XP entre runs, desbloqueos según DD-016.

**Probable subdivisión:**
- M6a — Acto 2 (HP enemigo, 2 tipos simultáneos, fases simples por umbral,
  primer transdimensional, elites con 2 mecánicas)
- M6b — Acto 3 (multifase, ancla, bosses que bloquean/fuerzan cambio,
  restricciones de cambio)
- M6c — Meta-progresión (XP, persistencia, desbloqueos, viñetas narrativas
  iniciales — DD-015 entra aquí si está listo)

**Toca archivos protegidos:** parcial (Acto 3 con bosses que bloquean cambio
toca TurnManager).

**Dependencias:** M5 cerrado (Boss Acto 1 funcional como plantilla).

**Complejidad:** muy alta.

---

## Completados

### M2 — TurnManager v2: mecánica core nueva + deuda técnica
**Fecha cierre:** 2026-05-07
**Resumen:**
- **Sub-PR A (PR #86):** tipo activo del jugador derivado del mundo, multiplicadores
  de efectividad como constantes configurables (1.5/1.0/0.75).
- **Sub-PR B (PR #88):** efectividad bidireccional. Daño enemigo SuperEficaz contra
  el tipo activo del jugador aplica multiplicador x1.5 (DD-018). Cargas de Estilo
  enchufadas al lado defensivo.
- **Sub-PR C (PR #89):** Contador de Estilo reemplaza Momentum por completo.
  +1 carga por SuperEficaz hecho, -1 por SuperEficaz recibido, 5 cargas → 1 cambio
  extra (no acumulable). Cap dinámico de switches via `TotalAvailableWorldSwitches`.
  Cartas neutras al 90% del daño base. UI HUD actualizada (Estilo: N/5).
- **UI auxiliar (PR #90):** label de tipo activo del jugador en WorldPanel del HUD.
- **Sub-PR D (PR #91 + hotfix):** PhaseBased AI implementado en `SelectEnemyMove()`
  con rangos `MinHpPercent`/`MaxHpPercent` en `EnemyMove`. `EffectType.Heal` +
  `HealAction` end-to-end (interfaz, ambos actors, action, case en CreateAction,
  tests). `CalculateIntentValue` cubre Defend+Heal. BossAct1 elementType asignado.
  Hotfix de `ActionQueueTests.TestActor.Heal()` post-merge para desbloquear Safe Mode
  → registrado como Insight 2 (grep por implementadores al ampliar interfaces).
- **Bonus de cleanup (Sub-PR D):** referencias a Momentum en comentarios eliminadas
  de TurnManager, CombatHudView, CombatUIController y `Combat/CLAUDE.md`. Doc del
  combate al día con el motor v2.

**Insights generados durante M2:** Insight 2 (interfaces) e Insight 3 (Retazos por
categoría de hook) registrados en `_insights.md`.

### M-tech — Deuda técnica acumulada
**Fecha cierre:** 2026-05-01
**Resumen:**
- **Fase 3 — `CombatHudView`** (327 loc): textos del HUD, botones End Turn /
  Change World, highlight de avatares por turno. Mergeada en PR previo a #85.
- **Fase 4 — `CombatBackgroundView`** (182 loc): sky/ground, sprites/colores
  A/B, CoverFill, polling autónomo de `CurrentWorld` (decisión Opción B —
  pensada para que cartas/Retazos futuros que cambien mundo refresquen fondos
  sin coordinación). Mergeada en PR #85.
- **CombatUIController**: 1473 → **710 loc** (-52% acumulado). Quedó como puro
  orquestador (BuildUI + helpers de creación) + InitializeExtractedViews.
- **`_owner` eliminado de CombatHudView**: andamio temporal de Fase 3 cerrado.
  HudView ya no conoce a CombatUIController.
- **Bug catcheado y fixeado**: CombatHudView no tenía `using
  RoguelikeCardBattler.Gameplay.Enemies;` (latente desde Fase 3, surfaced al
  abrir Unity post-Fase 4). 6 errores CS0246/CS0103 resueltos con la línea
  faltante. Insight 1 registrado en `_insights.md` para que validación de
  refactors abra Unity como criterio de cierre.
- **`gh` CLI instalado y autenticado**: `winget install --id GitHub.cli`.
  Habilita creación de PRs desde terminal sin copy-paste manual.
- **Deudas técnicas BUNDLEADAS con M2** (no se cerraron aquí porque tocan
  TurnManager protegido): PhaseBased AI, `EffectType.Heal`, `CalculateIntentValue`
  gaps. Se resuelven dentro del refactor coordinado de M2.

### M0 — Setup del sistema de modos
**Fecha cierre:** 2026-04-28
**Resumen:**
- CLAUDE.md raíz reescrito con sistema de 4 modos (gdd, diseno, implementacion,
  revision) + conversacional por defecto.
- `_insights.md`, `_gdd.md` placeholder, `_tech_snapshot.md`, `_roadmap.md`
  creados con plantillas y estado real del proyecto.
- `Assets/Scripts/Gameplay/Combat/CLAUDE.md` actualizado para mencionar el
  sistema de modos y restricciones específicas de combate.
- Smoke test pendiente fue absorbido por uso real durante M1.

### M1 — Procesar GDD v2
**Fecha cierre:** 2026-04-28
**Resumen:**
- GDD v2 procesado por completo en sesión `modo:gdd`.
- DDs cerradas en `DESIGN_DECISIONS.md`: DD-001 a DD-016 (todas excepto DD-015
  postponed por narrativa). Reglas movidas a `GOLDEN_RULES.md`.
- 5 DDs nuevas detectadas y resueltas en revisión post-GDD: DD-018 (daño enemigo
  x1.5 con multiplicador configurable), DD-019 (Sangrado/Virus exclusivos del
  boss), DD-020 (carta especial filtrada por tipos elegidos), DD-021 (MCguffin
  diferido a M4). DD-017 (Retazos de cambio en contenido base) queda como única
  abierta — se cierra al diseñar M3.
- `GOLDEN_RULES.md` ampliado de 7 a 12 secciones: Contador de Estilo (reemplaza
  Momentum), Mazo inicial, Mejora de cartas, Categorías dimensionales de
  enemigos, Bosses, 6 tipos de nodos, Economía, Actos y Dificultad, Retazos,
  Meta-progresión. Marcadores ✓ (implementado) / ⏳ (cerrado por diseño).
- Roadmap reorganizado con M2-M6 nuevos.

### Higiene previa (pre-roadmap)
**Fecha cierre:** 2026-04-27
**Resumen:**
- Consolidación de contexto: CLAUDE.md raíz creado + Combat/CLAUDE.md +
  MEMORY.md adelgazado + PROMPT_MASTER deprecado + texto residual de blockchain
  eliminado de docs.
- Extracción CombatUIController Fase 1: `CombatFeedbackView` (357 loc) — popups
  WEAK/RESIST/MOMENTUM, enemy shake, victory/defeat text, panel flash, hand
  limit toast.
- Extracción CombatUIController Fase 2: `CardHandView` (400 loc) — card buttons,
  click handler, attack animation, layout adaptivo, fade-in escalonado.
- CombatUIController reducido de 1473 → 980 loc (-33%).
- BossAct1 Regenerate fix: cambiado de DrawCards (no-op para enemigos) a
  Block 10.
- PRs mergeados: #83.

---

## Decisiones abiertas que afectan el roadmap

- Ninguna activa al 2026-05-07. **DD-017 cerrada** durante diseño de M3 → opción C
  (2-3 Retazos de cambio en contenido base como demo de la categoría). Detalle en
  `DESIGN_DECISIONS.md`.

---

## Future work / Backlog (no milestone)

Ideas que pueden volverse milestones cuando el GDD las priorice o cuando se
cierren los milestones actuales:

- **Refactor cross-cutting de los `Initialize()` de las views**: introducir un
  struct `ViewRefs` compartido en lugar de 25+ parámetros. Tiene sentido HACER
  después de Fase 4 cerrada (cuando las 3 views estén estables). ~30 min.
- **Extracción CombatUIController Fase 5+**: extracción completa quedaría con
  ~400 loc en CombatUIController (puro orquestador de BuildUI). Probablemente
  innecesario — evaluar si después de Fase 4 hay algo que extraer aún.
- **Tests de integración (Play-mode)**: actualmente solo hay EditMode tests.
  Play-mode tests para flujo completo de combate serían útiles antes de M5
  (bosses con fases tienen lógica difícil de unit-testear).
- **Sistema de logging**: no hay infraestructura de logging. Cuando se debugee
  en builds (no editor), va a hacer falta. Actualmente se usan `Debug.Log` con
  `#if UNITY_EDITOR`.
- **Strategy pattern para IA enemiga**: la selección de movimientos está baked
  en TurnManager. Para bosses con comportamiento complejo (M5) probablemente
  haga falta abstraer.
- **Translación de comentarios**: mezcla español/inglés sin criterio. Decisión
  pendiente: estandarizar a uno solo.
- **DD-015 Narrativa**: viñetas, diálogo de hermanos, lore. Postponed hasta que
  se aborde la capa de narrativa explícitamente. Probable que entre como parte
  de M6c.
