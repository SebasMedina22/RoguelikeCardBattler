# Roadmap — RoguelikeCardBattler

> **Qué es este archivo:** fuente única de verdad sobre milestones del proyecto.
> Se actualiza al cerrar sub-tareas (checkbox `[x]`) y al cerrar milestones
> completos (mover de Activo → Completados).
>
> En `modo:implementacion` se lee al inicio para saber qué milestone está activo
> y qué sub-tareas quedan.
>
> **Fase actual:** Higiene técnica + preparación para GDD nuevo
> **Milestone activo:** M0 — Setup del sistema de modos
> **Última actualización:** 2026-04-27

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

---

## Activo

### M0 — Setup del sistema de modos
**Objetivo:** dejar instalado el sistema de modos especializados (gdd, diseño,
implementación, revisión) + documentación de contexto persistente, antes de
empezar a procesar el GDD nuevo.

**Por qué primero:** procesar el GDD sin un sistema de modos consistente nos haría
volver a improvisar cada conversación. El setup paga dividendos durante toda la
implementación del GDD nuevo.

**Sub-tareas:**

- [x] Crear `Docs/dev/_insights.md` con plantilla
- [x] Crear `Docs/design/_gdd.md` placeholder
- [x] Crear `Docs/dev/_tech_snapshot.md` capturando estado actual post-extracciones
- [x] Crear `Docs/dev/_roadmap.md` (este archivo) con deuda técnica conocida
- [x] Reescribir `CLAUDE.md` raíz con sistema de modos completo (gdd, diseño,
      implementación, revisión + conversacional)
- [x] Actualizar `Assets/Scripts/Gameplay/Combat/CLAUDE.md` para mencionar el
      sistema de modos y qué archivos lee `modo:implementacion` al tocar combate
- [ ] Validar el sistema con una conversación corta en cada modo (smoke test)
- [ ] Commit + PR + merge

**Dependencias:** ninguna. Todo es trabajo de documentación.
**Criterios de cierre:** los 4 modos están definidos y son activables. Los archivos
`_tech_snapshot.md` y `_roadmap.md` reflejan el estado real del proyecto.

---

## Pendientes

### M1 — Procesar GDD nuevo
**Objetivo:** activar `modo:gdd` para procesar el documento que va a traer
Sebastián. Generar el mapa de gaps (qué tenemos vs qué falta), llenar
`_gdd.md` con la estructura final, y crear los milestones M2+ que el GDD implique.

**Sub-tareas (desagregar al activar):**
- Lectura completa del GDD nuevo
- Identificar sistemas implementados / parciales / pendientes
- Identificar contradicciones entre GDD y `GOLDEN_RULES.md` o código actual
- Identificar decisiones de diseño que el GDD cierra (mover de
  `DESIGN_DECISIONS.md` a `GOLDEN_RULES.md`)
- Identificar decisiones nuevas que el GDD abre
- Proponer 3-5 milestones nuevos (M2, M3, M4...) ordenados por dependencias
- Sebastián aprueba la priorización
- Actualizar `_gdd.md` y crear los milestones nuevos en este roadmap

**Dependencias:** M0 cerrado.
**Cuándo se activa:** cuando Sebastián diga "tengo el GDD, procesalo".

---

### M-tech — Deuda técnica acumulada (paralelo, no bloqueante)
**Objetivo:** pagar la deuda técnica conocida antes de que el GDD nuevo la
amplifique. Cada sub-tarea es relativamente independiente — pueden ejecutarse en
ratos sueltos, no requieren un sprint dedicado.

**Sub-tareas:**

#### Extracción CombatUIController — Fase 3 (CombatHudView)
- [ ] Diseñar contrato de `CombatHudView` (qué campos recibe, qué eventos escucha)
- [ ] Crear `CombatHudView.cs` con: paneles HP, energía, momentum, mundo,
      block overlays, intents, switches counter
- [ ] Mover `UpdateInfoTexts()` y `UpdateAvatarHighlight()` a CombatHudView
- [ ] CombatUIController delega via `_hudView.Sync()` en su Update()
- [ ] Verificar en BattleScene: HUD se actualiza igual, zero console errors

#### Extracción CombatUIController — Fase 4 (CombatBackgroundView)
- [ ] Crear `CombatBackgroundView.cs` con: sky/ground, sprites A/B,
      colors A/B, CoverFill, transición en world change
- [ ] Mover `CreateBackgroundLayers()`, `UpdateWorldVisuals()`, `CoverFill()`
- [ ] CombatUIController delega world visuals al view extraído
- [ ] Verificar en BattleScene: cambio de mundo muestra fondos correctos

#### Bugs/gaps en TurnManager (REQUIEREN aprobación explícita por archivo protegido)
- [ ] **PhaseBased AI**: declarado en `EnemyEnums.AIPattern.PhaseBased` pero sin
      case en `SelectEnemyMove()`. Implementar selección por fases (HP thresholds
      o turn count) o eliminar del enum si no se va a usar.
- [ ] **EffectType.Heal**: no existe. BossAct1 Regenerate usa Block como
      workaround. Si el GDD nuevo introduce mecánicas de heal/recover, agregar
      `EffectType.Heal` + `HealAction` + case en `CreateAction()`.
- [ ] **CalculateIntentValue gaps**: solo maneja `Attack+Damage` y `Defend+Block`.
      Si se agregan combinaciones nuevas (ej: Defend+Heal, Buff+StatusEffect),
      ampliar el switch.

#### Tooling
- [ ] Instalar `gh` CLI para automatizar creación de PRs desde terminal
- [ ] Verificar que el otro developer también lo instala

**Dependencias:** ninguna específica. Cada item es independiente. Las extracciones
3-4 pueden ejecutarse antes o durante M1.
**Prioridad:** baja-media. No bloquea ninguna feature, pero pagar antes del GDD
nuevo evita acumulación.

---

## Completados

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

## Future work / Backlog (no milestone)

Ideas que pueden volverse milestones cuando el GDD las priorice:

- **CombatUIController fases 5+**: extracción completa quedaría con ~400 loc en
  CombatUIController (puro orquestador de BuildUI). Probablemente innecesario.
- **Tests de integración**: actualmente solo hay EditMode tests. Play-mode tests
  para flujo completo de combate serían útiles antes del GDD nuevo si se va a
  refactorear mucho.
- **Sistema de logging**: no hay infraestructura de logging. Cuando se debugee en
  builds (no editor), va a hacer falta. Actualmente se usan `Debug.Log` con
  `#if UNITY_EDITOR`.
- **Strategy pattern para IA enemiga**: la selección de movimientos está baked en
  TurnManager. Para bosses con comportamiento complejo, va a hacer falta
  abstraer. Posible milestone si el GDD pide bosses elaborados.
- **Translación de comentarios**: mezcla español/inglés sin criterio. Decisión
  pendiente: estandarizar a uno solo.
