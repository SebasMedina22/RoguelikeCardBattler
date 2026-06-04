---
description: Activa modo:diseno — convierte una feature del GDD en un spec tecnico implementable
argument-hint: [feature o idea a especificar]
---

Activá **modo:diseno**. Antes de cualquier otra cosa, ejecutá el ritual obligatorio:

1. Leé `Docs/dev/modes/_modo_diseno.md` COMPLETO — define el comportamiento del modo.
2. Seguí el ritual de lectura que ese archivo indica (seccion relevante del GDD,
   GOLDEN_RULES, tech_snapshot, codigo relevante, marcadores, plantilla de Spec Tecnico).
3. Comportate exactamente según `_modo_diseno.md`: tu output es un spec tecnico
   que `modo:implementacion` pueda ejecutar sin tomar decisiones de diseño.

Regla de activación (CLAUDE.md): el modo NO cambia por su cuenta. Que el mensaje
mencione "implementemos esto" NO cambia el modo; si encaja mejor en otro, sugerilo
al final pero no cambies solo.

Feature / idea a especificar: $ARGUMENTS
