---
description: Ritual de cierre y persistencia de sesión (cualquier modo)
model: sonnet
---

# /cierre-sesion — Ritual de cierre y persistencia

Ejecutá el ritual de cierre de sesión. Aplica al final de CUALQUIER sesión de
trabajo, sin importar el modo (implementación, revisión, diseño, gdd o
conversacional): el cierre pertenece a la SESIÓN, no a un modo. Si una sesión
de `modo:revision` detectó problemas e implicó correcciones, esas correcciones
también se persisten con este mismo ritual.

Checklist (en orden; reportá qué se actualizó y qué no aplicaba):

1. **Roadmap — lo hecho:** marcar checkboxes `[x]` de sub-tareas cerradas en
   `Docs/dev/_roadmap.md` y actualizar el header (Fase actual / Última
   actualización).
2. **Roadmap — lo futuro (anti-desfase):** releer las sub-tareas de los
   milestones Activo y Pendientes. Si el trabajo de esta sesión absorbió,
   invalidó o dejó vieja alguna sub-tarea FUTURA, corregir su texto AHORA.
   (Origen de la regla: reorden de M4 del 2026-06-10 — el Sub-PR 3C implementó
   el mecanismo de mejora de cartas y la sub-tarea de M4 quedó 6 semanas
   pidiendo construir algo que ya existía.)
3. **Tech snapshot:** si hubo cambios estructurales (módulo/subsistema nuevo,
   refactor que mueve responsabilidades), actualizar `Docs/dev/_tech_snapshot.md`.
4. **Memoria:** actualizar las memorias de proyecto afectadas (estado, próximo
   paso) y su línea en `MEMORY.md`. Editar archivos existentes — no crear
   duplicados.
5. **Solo si se cerró un MILESTONE completo:** seguir los pasos de "Al cerrar
   un milestone completo" de `_roadmap.md`, que incluyen la higiene de memoria
   y el barrido de ⏳ de GOLDEN_RULES (verificar que cada regla
   cerrada-por-diseño tenga dueño en algún milestone; huérfanas → reportar).
6. **Git:** revisar el diff antes de proponer commit — de-scopear
   `.claude/settings.json` y `RoguelikeCardBattler.slnx` si se auto-modificaron.
   Proponer branch/commit para docs si aplica (`docs/*`).
7. **Cierre:** mini-resumen de qué quedó persistido + siguiente paso sugerido
   del proyecto.

Límites: este ritual NO edita `GOLDEN_RULES.md`, `_gdd.md` ni
`DESIGN_DECISIONS.md` (archivos de Sebastián, protegidos por hook). Si algo de
ahí quedó desactualizado, proponer el texto exacto en el chat para que
Sebastián lo pegue.
