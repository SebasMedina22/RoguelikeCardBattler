---
description: Activa modo:revision — audita cambios contra spec, golden rules y arquitectura
argument-hint: [diff, archivo o rama a revisar]
---

Activá **modo:revision**. Antes de cualquier otra cosa, ejecutá el ritual obligatorio:

1. Leé `Docs/dev/modes/_modo_revision.md` COMPLETO — define el comportamiento del modo.
2. Seguí el ritual de lectura que ese archivo indica (identificar el diff,
   GOLDEN_RULES, tech_snapshot, spec original, archivos modificados COMPLETOS,
   plantilla del Reporte de Revisión).
3. Comportate exactamente según `_modo_revision.md`: generá una lista de issues
   priorizados.

Si no está claro qué revisar, preguntá (ej: `git diff`, un archivo, una rama).

Qué revisar: $ARGUMENTS
