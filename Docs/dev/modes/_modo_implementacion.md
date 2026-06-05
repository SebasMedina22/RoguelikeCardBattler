# modo:implementacion — Escribir y modificar código

> **Para qué se carga este archivo:** se lee al inicio cuando Sebastián activa
> `modo:implementacion` con la palabra clave al inicio del mensaje. Define el
> comportamiento completo del modo. Para la regla general de activación, ver
> `CLAUDE.md` raíz.

**Activación:** `modo:implementacion` al inicio del mensaje
**Propósito:** escribir, modificar, debuggear código del juego siguiendo un spec
de `modo:diseno` o una tarea concreta del roadmap activo.

## Ritual obligatorio al activar

1. Leer `Docs/dev/_tech_snapshot.md` ANTES de proponer cambios técnicos
2. Leer `Docs/dev/_roadmap.md` para saber qué milestone está activo
3. Leer `Docs/dev/GOLDEN_RULES.md` para no romper reglas
4. Si la tarea toca combate: leer `Assets/Scripts/Gameplay/Combat/CLAUDE.md`
5. Si la tarea no corresponde a una sub-tarea del milestone activo, preguntar
   a Sebastián si registrarla en el roadmap o tratarla como ad-hoc.
6. Leer `Docs/dev/modes/_marcadores.md` para conocer los marcadores explícitos.

## Comportamiento

- Eres pragmático, conciso y verificable. Priorizas correctness sobre elegancia.
- Diferencias SIEMPRE entre (ver `_marcadores.md`):
  - `[CÓDIGO ACTUAL]`: lo que realmente existe hoy (verificado, leído del repo)
  - `[PROPUESTA]`: lo que estás sugiriendo implementar
  - `[HIPÓTESIS]`: explicación de un bug o comportamiento que no verificaste
- Cambios sin confirmación previa: archivos que Sebastián mencionó en el mensaje
  o que están listados en el spec activo.
- Cambios QUE REQUIEREN CONFIRMACIÓN previa:
  - Tocar archivos protegidos: `TurnManager.cs`, `ActionQueue.cs`,
    `PlayerCombatActor.cs`
  - Crear archivos nuevos no mencionados en el spec
  - Modificar `GOLDEN_RULES.md`, `_gdd.md`, `DESIGN_DECISIONS.md`
  - Cambios de arquitectura (nuevos módulos, nuevos asmdefs, cambio de stack)
  - Modificar tests existentes (puedes agregar tests nuevos sin pedir)

## Reglas de preservación de contexto (críticas)

- ANTES de proponer cambios en arquitectura, VERIFICA `_tech_snapshot.md`.
- Si `_tech_snapshot.md` no aclara algo que necesitas saber, PREGUNTA a Sebastián.
  No asumas. (Ej: el bug del 2D/3D híbrido en otro proyecto pasó por asumir.)
- Si implementas algo que cambia estructuralmente el proyecto (nuevo subsistema,
  refactor que mueve responsabilidades, dependencia nueva), ACTUALIZA
  `_tech_snapshot.md` al terminar.

## Skills de implementación

- Debuggear causas raíz, no síntomas. Si un bug se repite, entender por qué
  antes de parchar.
- Priorizar legibilidad. Sebastián es senior pero el código lo va a leer otro
  developer también.
- Reuso: usar `UIAnimationHelper`, `AudioManager`, patrones existentes (eventos
  de TurnManager, `IGameAction`, SOs) antes de crear nuevos.
- Tests: agregar tests EditMode cuando la lógica sea no-trivial. Si la feature
  es 100% UI/Inspector-driven, validación manual en BattleScene es suficiente.
- Al terminar una sub-tarea del roadmap, marcar checkbox `[x]`.

## Reglas obligatorias del proyecto

- **CS0104**: si un archivo usa `System` y `UnityEngine`, escribir
  `UnityEngine.Object` explícito (nunca `Object` ambiguo).
- **Zero console errors** = criterio de aceptación base de todo cambio.
- **No manual editor setup**: features nuevas auto-crean GameObjects en runtime
  o las instancia un scene controller. Nunca requerir attach manual.
- **Animaciones fire-and-forget**: gameplay logic NUNCA espera callbacks de
  animación cosmética. Excepción única: `PlayAttackOnce()` en CardHandView.
- **Comentarios de onboarding**: explican el porqué, no el qué.
- **No duplicar lógica**: verificar `Core/UI/UIAnimationHelper.cs` (8 métodos
  DOTween) y `Core/Audio/AudioManager.cs` antes de crear helpers nuevos.

## Reglas de archivos

- Modificas: archivos del spec, tests nuevos, `_tech_snapshot.md` si hay cambio
  estructural, `_roadmap.md` para marcar sub-tareas cerradas.
- NO modificas (sin aprobación): archivos protegidos, `GOLDEN_RULES.md`,
  `_gdd.md`, `DESIGN_DECISIONS.md`.
- Cuando termines una sub-tarea sustantiva, sugiere activar `modo:revision`
  antes del commit.

## Validación con Unity-MCP (si está disponible)

El proyecto tiene un Unity-MCP (`ai-game-developer`) conectado en la mayoría de
las sesiones interactivas. Es el **mecanismo** para cumplir los criterios de
aceptación que este modo ya exige (zero console errors, tests EditMode verdes),
en vez de pedirle a Sebastián que abra Unity a mano.

- Detalles operativos (servidor, versión a usar/evitar, comandos, skills
  disponibles) viven en la memoria de tooling y en `CLAUDE_WORKFLOW_GUIDE.md §4`
  — NO se duplican acá porque son volátiles. Consultá esa fuente al usarlo.
- Skills típicas de validación: correr tests EditMode, leer la consola, ejecutar
  código de diagnóstico, inspeccionar escena/GameObjects.
- **Limitación conocida:** el game view no repinta sin foco vía CLI → verificá
  UI por diagnóstico de código (estado de componentes, jerarquía), no por
  screenshot del game view.
- **Si el MCP NO está disponible** (corrida headless/cron, Unity cerrado): no
  inventes resultados de validación. Dejá explícito qué quedó sin validar y pedí
  a Sebastián la verificación manual en Unity.

### Cierre de una sub-tarea sustantiva

Antes de marcar `[x]` o sugerir `modo:revision`/commit, si el MCP está
disponible, verificá: (1) compilación limpia (zero console errors), (2) tests
EditMode nuevos + suite completa en verde, (3) el flujo jugable end-to-end de la
feature en su escena. Reportá el resultado real — si algo falla, decilo con el
output, no lo maquilles.
