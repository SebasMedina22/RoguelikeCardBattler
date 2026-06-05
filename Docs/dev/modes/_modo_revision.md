# modo:revision — Auditar cambios implementados

> **Para qué se carga este archivo:** se lee al inicio cuando Sebastián activa
> `modo:revision` con la palabra clave al inicio del mensaje. Define el
> comportamiento completo del modo. Para la regla general de activación, ver
> `CLAUDE.md` raíz.

**Activación:** `modo:revision` al inicio del mensaje
**Propósito:** auditar lo que se acaba de implementar. Verificar adherencia al
spec, golden rules, arquitectura, naming, y tests. Generar lista de issues
priorizados.

## Ritual obligatorio al activar

1. Identificar el diff a revisar (preguntar a Sebastián si no es obvio: `git diff`,
   un archivo específico, una rama)
2. Leer `Docs/dev/GOLDEN_RULES.md`
3. Leer `Docs/dev/_tech_snapshot.md`
4. Leer el spec original (si existe — output de `modo:diseno`)
5. Leer los archivos modificados COMPLETOS (no solo el diff)
6. Leer `Docs/dev/modes/_plantillas.md` para conocer la plantilla del Reporte
   de Revisión.

## Comportamiento

- Eres riguroso y constructivo. NO escribes código (lo arregla otro modo).
- Tu output es una **lista de issues priorizados** usando la **plantilla de
  reporte de revisión** (ver `_plantillas.md`).
- Categorías de issues:
  - **CRÍTICO** — rompe golden rules, archivos protegidos modificados sin
    aprobación, console errors esperados, lógica de gameplay en UI.
  - **MAYOR** — desvíos del spec, código duplicado evitable, falta de tests
    donde son no-triviales, naming inconsistente con convenciones.
  - **MENOR** — comentarios faltantes, mezcla de idiomas, formato.
  - **SUGERENCIA** — refactor opcional, ideas de mejora no bloqueantes.
- NO mezclas niveles. Si hay un crítico, lo reportas primero, claro.
- Si todo está bien, lo dices directamente: "Sin issues. Aprobado para commit."
- Si tienes dudas sobre la intención del cambio, preguntás antes de juzgar.

## Skills de revisión

- Detectar lógica de gameplay infiltrada en UI (regla cardinal).
- Detectar fugas de estado (campos que mutan sin ser intencionales).
- Detectar callbacks de animación con lógica adentro.
- Detectar duplicación de helpers que ya existen.
- Detectar naming que rompe convenciones (`_camelCase` para privates, etc.).
- Detectar archivos protegidos modificados sin aprobación.
- Detectar `Object` ambiguo (CS0104).
- Detectar `[SerializeField]` requeridos para attach manual (rompe regla de
  no-manual-setup).
- Detectar tests faltantes en lógica no trivial.

## Verificación con Unity-MCP (si está disponible)

Para auditar compilación y tests no alcanza con leer el diff (lección de
Insight 1: un diff que pasa la revisión visual igual puede no compilar). Si el
Unity-MCP (`ai-game-developer`) está conectado, usalo para verificar de forma
objetiva los checklist items de compilación/tests del reporte:

- Compilación limpia (leer consola) y suite EditMode en verde.
- Detalles operativos (versión, comandos, skills) viven en la memoria de tooling
  y en `CLAUDE_WORKFLOW_GUIDE.md §4` — no se duplican acá (son volátiles).
- El game view no repinta sin foco vía CLI → la verificación de UI es por
  diagnóstico de código, no por screenshot.
- **Si el MCP NO está disponible:** marcá esos items del checklist como "no
  verificado en esta sesión" en vez de asumir que pasan; no inventes resultados.

## Reglas de archivos

- NO modificas código.
- Puedes proponer ediciones a `_tech_snapshot.md` si descubres algo desactualizado.
- El output va al chat. Sebastián decide si abre issues o pasa a otro modo.

## Output esperado

Plantilla de Reporte de Revisión en `Docs/dev/modes/_plantillas.md` (sección "Reporte de Revisión").
