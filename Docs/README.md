# Docs — Índice de navegación

> **Convención de archivos:**
> - `_<nombre>.md` con guión bajo = documento "vivo" (se actualiza durante el flujo)
> - `<NOMBRE>.md` en mayúsculas = referencia estable
> - `_archive/` = histórico (no editar salvo arqueología)

---

## Punto de entrada para el agente

- `CLAUDE.md` (raíz del proyecto) — instrucciones, sistema de modos, plantillas

## Documentación técnica activa — `Docs/dev/`

| Archivo | Propósito |
|---------|-----------|
| `GOLDEN_RULES.md` | Reglas del juego + código probadas y funcionando. **Autoridad final**. |
| `_tech_snapshot.md` | Foto técnica actual (stack, arquitectura, archivos críticos) |
| `_roadmap.md` | Milestones activos, sub-tareas, dependencias |
| `_insights.md` | Observaciones de gameplay/playtesting |
| `COMBAT_ARCHITECTURE.md` | Diagramas + flujo de combate |
| `DEV_ONBOARDING.md` | Setup inicial, quickstart, test runner |
| `GLOSSARY.md` | Terminología del proyecto |

## Documentación de diseño activa — `Docs/design/`

| Archivo | Propósito |
|---------|-----------|
| `_gdd.md` | GDD vigente. **Solo Sebastián edita.** |
| `DESIGN_DECISIONS.md` | Decisiones de diseño abiertas. **Solo Sebastián cierra.** |
| `MECHANIC_DUALITY.md` | Sistema de mundos duales (conceptual) |
| `CARDS.md` | Schema de datos de cartas |
| `ENEMIES.md` | Schema de datos de enemigos |

## Histórico — `_archive/`

Documentos de fases anteriores del proyecto. No se editan.

- `Docs/dev/_archive/PROMPT_MASTER_AND_PROGRAMMING.md` — workflow dual-agent (deprecado)
- `Docs/design/_archive/DESIGN.md` — visión MVP inicial
- `Docs/design/_archive/PROJECT_STATUS.md` — reemplazado por `_roadmap.md`
- `Docs/design/_archive/MVP_COMBAT_ISSUES.md` — issues iniciales del MVP
- `Docs/design/_archive/EPIC_4_VERTICAL_SLICE_ACTO_1.md` — Epic completado
- `Docs/design/_archive/Proyecto C.md` — GDD original v1 (markdown)
- `Docs/design/_archive/Proyecto C.pdf` — GDD original v1 (PDF)
- `Docs/design/_archive/GDD_v2_source.pdf` — GDD nuevo (PDF de referencia)
- `Docs/design/_archive/Tabla de tipos simetrica.png` — idea descartada (la tabla del juego es asimétrica)

---

## Quickstart por rol

**Si vienes a programar:** `Docs/dev/DEV_ONBOARDING.md` → `Docs/dev/_tech_snapshot.md` → `Docs/dev/_roadmap.md`

**Si vienes a diseñar features:** `Docs/design/_gdd.md` → `Docs/dev/GOLDEN_RULES.md` → `Docs/design/DESIGN_DECISIONS.md`

**Si vienes a entender el combate:** `Docs/dev/COMBAT_ARCHITECTURE.md` → `Docs/dev/GLOSSARY.md` → código en `Assets/Scripts/Gameplay/Combat/`

**Si vienes a usar Claude Code/IA en el proyecto:** `CLAUDE.md` (raíz) — el sistema de modos está documentado ahí.
