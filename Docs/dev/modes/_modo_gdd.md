# modo:gdd — Procesamiento del GDD

> **Para qué se carga este archivo:** se lee al inicio cuando Sebastián activa
> `modo:gdd` con la palabra clave al inicio del mensaje. Define el comportamiento
> completo del modo. Para la regla general de activación, ver `CLAUDE.md` raíz.

**Activación:** `modo:gdd` al inicio del mensaje
**Propósito:** procesar el documento GDD entero, mapear gaps vs estado actual del
código, identificar decisiones que el GDD cierra/abre, y generar milestones.

## Ritual obligatorio al activar

1. Leer `Docs/design/_gdd.md` (placeholder o versión actual)
2. Leer `Docs/dev/GOLDEN_RULES.md` para saber qué está probado
3. Leer `Docs/design/DESIGN_DECISIONS.md` para decisiones abiertas
4. Leer `Docs/dev/_tech_snapshot.md` para saber qué arquitectura existe
5. Leer `Docs/dev/_roadmap.md` para milestones actuales
6. Leer `Docs/dev/modes/_marcadores.md` para conocer los marcadores explícitos
7. Leer `Docs/dev/modes/_plantillas.md` para conocer la plantilla de análisis de GDD

## Comportamiento

- Eres analítico y exhaustivo. Lees el GDD COMPLETO antes de empezar a procesar.
- Diferencias SIEMPRE entre lo que dice el GDD y nuestra interpretación. Usa
  marcadores (ver `_marcadores.md` para la tabla completa):
  - `[GDD]`: lo que el documento afirma textualmente
  - `[INTERPRETACIÓN]`: cómo entendemos eso para el proyecto
  - `[GAP]`: algo que el GDD pide y no existe en código
  - `[CONTRADICCIÓN]`: algo que el GDD contradice respecto a `GOLDEN_RULES.md`
  - `[NO CLARO]`: algo ambiguo o que el GDD no especifica
- Si el GDD contradice algo de `GOLDEN_RULES.md` (que es código probado), NO asumes
  cuál gana — preguntas a Sebastián.
- Identificas qué decisiones de `DESIGN_DECISIONS.md` el GDD cierra y propones
  moverlas a `GOLDEN_RULES.md` (Sebastián confirma).
- Identificas decisiones nuevas que el GDD abre y propones agregarlas a
  `DESIGN_DECISIONS.md`.
- Generas un mapa estructurado de gaps usando la **plantilla de análisis de GDD**
  (ver `Docs/dev/modes/_plantillas.md`).
- Propones milestones nuevos (M2, M3, ...) con dependencias claras.
- Sebastián aprueba la priorización antes de actualizar `_roadmap.md`.

## Skills de procesamiento de GDD

- Distinguir entre la **dirección** del GDD (qué quiere ser el juego) y los
  **detalles** (números, nombres, etc.). La dirección es más importante para
  arquitectura; los detalles para implementación específica.
- Detectar incoherencias internas en el GDD (sistemas que se contradicen entre sí).
- Estimar complejidad relativa de implementación (alta/media/baja) por sistema.
- Identificar dependencias entre sistemas (no se puede hacer X sin Y).
- Detectar features que requieren tocar archivos protegidos (TurnManager, etc.)
  y marcarlas explícitamente.

## Reglas de archivos

- Puedes proponer cambios a `_gdd.md`, `DESIGN_DECISIONS.md`, `_roadmap.md`.
- Sebastián aprueba antes de aplicar cualquier cambio.
- NO modificas `GOLDEN_RULES.md` en este modo (solo Sebastián cierra reglas ahí).

## Output esperado

Plantilla de Análisis de GDD en `Docs/dev/modes/_plantillas.md` (sección "Análisis de GDD").
