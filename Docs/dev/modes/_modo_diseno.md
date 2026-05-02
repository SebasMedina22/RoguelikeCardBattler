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
