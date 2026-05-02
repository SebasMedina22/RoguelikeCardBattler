# Roadmap — RoguelikeCardBattler

> **Qué es este archivo:** fuente única de verdad sobre milestones del proyecto.
> Se actualiza al cerrar sub-tareas (checkbox `[x]`) y al cerrar milestones
> completos (mover de Activo → Completados).
>
> En `modo:implementacion` se lee al inicio para saber qué milestone está activo
> y qué sub-tareas quedan.
>
> **Fase actual:** entrando a M2 — reescritura de la mecánica core sobre TurnManager.
> **Milestone activo:** M2 — TurnManager v2 (mecánica core nueva + bundle de deuda)
> **Próximo bloque:** M3 — Personalización del run (Retazos, Tienda, Hoguera)
> **Última actualización:** 2026-05-01 (post-cierre M-tech, gh CLI instalado)

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

### M2 — TurnManager v2: mecánica core nueva + deuda técnica

**Objetivo:** instalar el sistema de cargas (Contador de Estilo) y todas las
mecánicas que el GDD v2 introduce sobre el motor de combate, en un único refactor
coordinado de los archivos protegidos. Bundle gigante intencional.

**Por qué bundle:** todas las features tocan `TurnManager.cs` (PROTEGIDO).
Hacerlas en PRs separados obliga a reabrir el archivo cada vez, reescribir tests
varias veces y reconciliar cambios. Una sola serie de sub-PRs cohesivos es más
limpio.

**Features incluidas:**

#### Mecánica core nueva (cierra DDs del GDD v2)
- [ ] Eliminar Momentum (todas sus referencias en TurnManager + UI)
- [ ] Implementar Contador de Estilo (cargas: +1 por SuperEficaz hecho, -1 por
      SuperEficaz recibido, 5 cargas → 1 cambio extra no acumulable, reset cada
      combate)
- [ ] Cambios múltiples de mundo por combate (cap dinámico: 1 base + extras por
      cargas / cartas / Retazos)
- [x] Tipo activo del jugador (campo derivado del mundo, viene de los 2 tipos
      elegidos al inicio del run) *(Sub-PR A — PR #86)*
- [x] Daño enemigo SuperEficaz contra el jugador: aplicar multiplicador x1.5
      (DD-018) — multiplicador configurable como constante *(Sub-PR B)*
- [x] Hacer configurables los multiplicadores de efectividad (1.5 / 1.0 / 0.75)
      como constantes en código *(Sub-PR A — PR #86)*
- [ ] Cartas neutras al 90% del daño base (DD-002)

#### Deuda técnica que se cierra aquí (bundle de M-tech)
- [ ] PhaseBased AI: implementar case en `SelectEnemyMove()` (HP thresholds o
      turn count)
- [ ] `EffectType.Heal` + `HealAction` + case en `CreateAction()`
- [ ] `CalculateIntentValue` ampliado para combinaciones nuevas (Defend+Heal,
      Buff+StatusEffect, etc.)

#### UI de M2
- [ ] HUD: cambiar "Momentum: N" por "Cargas: N/5" (modificar CombatHudView ya
      extraído)
- [ ] HUD: indicador de tipo activo del jugador (label nuevo)
- [ ] HUD: contador de switches dinámico (ya no es "X/1", es "X/?" según cargas)

**Sistemas afectados:** TurnManager, PlayerCombatActor, RunState, ElementTypes,
CombatHudView, tests existentes (WorldSwitchLimitTests, DamageEffectivenessTests,
PlayerCombatActorTests deberán reescribirse).

**Toca archivos protegidos:** **SÍ** — TurnManager + PlayerCombatActor.
**REQUIERE APROBACIÓN EXPLÍCITA** antes de empezar implementación.

**Dependencias:** M-tech cerrada ✅ (CombatHudView ya extraído facilita la
modificación del HUD para cargas; CombatBackgroundView libera el monolito
para tocar HUD/state sin pisar fondos; gh CLI listo para PRs ágiles).

**Estrategia de PRs:** subdividir en 4 sub-PRs cohesivos:
- Sub-PR A: tipo activo del jugador + multiplicadores configurables (sin tocar
  comportamiento todavía)
- Sub-PR B: efectividad bidireccional + daño x1.5 al jugador
- Sub-PR C: Contador de Estilo + cambios múltiples (reemplaza Momentum)
- Sub-PR D: PhaseBased AI + Heal + CalculateIntentValue (deuda técnica)

**Validación obligatoria por sub-PR (lección de Fase 3-4, ver `_insights.md`):**
- Compilación de Unity sin errores antes de marcar como mergeable.
- Tests EditMode en verde.
- BattleScene jugable y manualmente verificada.

**Complejidad:** alta. Es el cambio que más riesgo de regresión introduce en el
motor de combate.

**Criterios de cierre:** todos los tests pasan + comportamiento verificado en
BattleScene + Momentum completamente eliminado del código + UI del HUD refleja
Cargas y tipo activo del jugador.

---

## Pendientes

### M3 — Personalización del run

**Objetivo:** instalar el ecosistema de personalización del run según GDD v2:
Retazos como sistema base, Tienda funcional, Hoguera con heal/upgrade, y draft
de carta especial dual al inicio.

**Sub-tareas (desagregar al activar):**
- Sistema de Retazos: `RelicDefinition` SO, hooks (`RelicEffectHook`) en eventos
  clave (init combate, draw, daño hecho/recibido, switch de mundo, fin combate),
  `RunState.Relics`, UI
- Tienda (Shop): controller, view, scene/canvas, lógica de selección por mundo,
  compra/eliminar cartas
- Hoguera (Campfire): controller, view, decisión heal vs upgrade
- Carta especial dual inicial: draft de 6 opciones filtradas por los 2 tipos
  elegidos (DD-020), UI panel
- **Cerrar DD-017** (Retazos de cambio en contenido base) durante el diseño

**Toca archivos protegidos:** parcial (TurnManager para hooks de Retazos en
eventos del combate).

**Dependencias:** M2 cerrado (Retazos de cambio dependen de cargas estables;
hoguera depende de `EffectType.Heal`).

**Complejidad:** alta. Retazos solos son alta complejidad por la cantidad de
hooks y categorías.

---

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

- **DD-017** — Retazos de cambio en contenido base: se cierra al diseñar M3.
  No bloquea M2.

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
