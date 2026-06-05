# modo:diseno — Diseño de specs técnicos

> **Para qué se carga este archivo:** se lee al inicio cuando Sebastián activa
> `modo:diseno` con la palabra clave al inicio del mensaje. Define el comportamiento
> completo del modo. Para la regla general de activación, ver `CLAUDE.md` raíz.

**Activación:** `modo:diseno` al inicio del mensaje
**Propósito:** convertir UNA feature del GDD (o una idea concreta) en un **spec
técnico implementable**. Es game design + arquitectura aplicada.

## Ritual obligatorio al activar

1. Leer la sección relevante de `Docs/design/_gdd.md`
2. Leer `Docs/dev/GOLDEN_RULES.md` (autoridad sobre lo probado)
3. Leer `Docs/dev/_tech_snapshot.md` para conocer arquitectura existente
4. Leer código relevante al sistema afectado (sin proponer cambios todavía)
5. Leer `Docs/dev/modes/_marcadores.md` para conocer los marcadores explícitos
6. Leer `Docs/dev/modes/_plantillas.md` para conocer la plantilla de Spec Técnico

## Comportamiento

- Eres pragmático y orientado a contratos. Tu output es un **spec técnico**
  que el `modo:implementacion` puede ejecutar sin tener que tomar decisiones de diseño.
- Diferencias SIEMPRE entre (ver `_marcadores.md` para la tabla completa):
  - `[GDD]`: lo que el GDD pide
  - `[INTERPRETACIÓN]`: cómo lo traduces a sistemas
  - `[PROPUESTA]`: tu sugerencia técnica concreta
  - `[ALTERNATIVA]`: opciones que descartaste y por qué
  - `[ABIERTO]`: decisiones que Sebastián tiene que cerrar antes de implementar
- NO escribes código en este modo. Solo specs y diagramas.
- Si una feature requiere tocar archivos protegidos (TurnManager, ActionQueue,
  PlayerCombatActor), lo marcas explícitamente y pides aprobación de Sebastián
  antes de continuar.
- Si una feature contradice una golden rule, te detienes y discutes.
- Si una feature requiere una decisión de diseño abierta, paras y preguntas.
- Output formal: usa la **plantilla de spec técnico** (ver `_plantillas.md`).

## Skills de diseño

- Pensar en contratos: qué entradas, qué salidas, qué efectos secundarios.
- Pensar en estados: qué fases tiene la feature, qué transiciones permitidas.
- Reuso: identificar si la feature puede usar `IGameAction`, `EffectRef`, SOs
  existentes, helpers (`UIAnimationHelper`, `AudioManager`) antes de proponer abstracciones nuevas.
- Acoplamiento: minimizar referencias entre sistemas. Preferir eventos a
  llamadas directas cuando UI ↔ gameplay.
- Tests: identificar qué casos de prueba EditMode validarían la feature.
- Balance: si el GDD da números, traducirlos a campos de SO. Si no, marcar como
  decisión pendiente, no inventar.

## Reglas de archivos

- Puedes proponer cambios a `_gdd.md` (cuando descubres algo durante el diseño).
- NO modificas código en este modo.
- El output va al chat. Sebastián decide si lo guarda en algún issue/doc.

## Output esperado

Plantilla de Spec Técnico en `Docs/dev/modes/_plantillas.md` (sección "Spec Técnico").

## Cierre obligatorio: prompt de handoff para `modo:implementacion`

Cuando un spec queda cerrado (sin decisiones abiertas), SIEMPRE generás un
**prompt de handoff** que Sebastián pueda copiar/pegar como mensaje nuevo para
arrancar `modo:implementacion` sobre ese spec. Es la última sección del output.

Reglas del prompt de handoff:
- Empieza con la línea `modo:implementacion`.
- Apunta al archivo de spec persistido (`Docs/dev/specs/<nombre>.md`) e indica
  leerlo completo antes de codear.
- Incluye el setup de branch concreto (con `git fetch --all --prune` y el
  `checkout -b feat/...` desde `origin/main`, confirmando antes el estado de
  cualquier PR previo del que dependa).
- Resume QUÉ construir (archivos a crear/modificar) sin re-explicar el diseño —
  el spec ya lo tiene.
- Lista las reglas no negociables relevantes (archivos protegidos, no manual
  editor setup, lo que quedó fuera de scope por decisión cerrada).
- Lista la validación obligatoria antes de cerrar (compilación, tests, flujo
  end-to-end, herramienta de validación del proyecto).
- Cierra indicando qué docs actualizar (`_roadmap.md`, `_tech_snapshot.md`) y el
  flujo de commit/PR.

Si el spec todavía tiene decisiones abiertas (`[ABIERTO]`), NO generás el prompt
de handoff: primero se cierran las decisiones. La plantilla del prompt vive en
`_plantillas.md` (sección "Prompt de handoff a implementación").
