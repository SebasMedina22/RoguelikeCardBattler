# Subagentes — playbook de delegación por modo

> **Qué es este archivo:** recurso compartido (igual que `_plantillas.md` y
> `_marcadores.md`). Detalla CÓMO usar subagentes en cada modo. El **criterio
> de cuándo delegar vs. hacerlo directo** vive en `CLAUDE.md` raíz
> ("Sobre subagentes y delegación") porque debe estar siempre cargado. Acá está
> el detalle operativo, que se carga on-demand para no pesar en cada sesión.
>
> **Cuándo leer este archivo:** al activar un modo y prever trabajo de fan-out o
> paralelizable, o ante una decisión de delegación no trivial.

---

## Recordatorio del gate (resumen del criterio de CLAUDE.md)

**Delegá** si: fan-out de lectura (≥3-4 archivos para una conclusión) ·
paralelismo (≥2 sub-tareas independientes) · verificación adversarial
independiente.

**Hacelo directo** si: lookup de un dato en archivo conocido · edición puntual
de 1-3 ubicaciones ya localizadas · tarea trivial/conversacional · el costo del
subagente supera el ahorro de contexto (overkill).

**Nunca** delegues la EDICIÓN de archivos protegidos (`TurnManager.cs`,
`ActionQueue.cs`, `PlayerCombatActor.cs`). Leer/explorar sí; editar lo hace el
hilo principal con aprobación de Sebastián.

---

## Tipos de subagente disponibles

| Tipo | Para qué | Edita archivos |
|------|----------|----------------|
| `Explore` | Búsqueda/lectura read-only a lo ancho de muchos archivos; devuelve la conclusión, no el volcado de archivos | No |
| `Plan` | Diseñar un plan de implementación / bocetar approaches; no ejecuta | No |
| `general-purpose` | Tarea multi-step completa (buscar + editar + verificar) | Sí |

> Regla práctica: empezá por el tipo más restringido que resuelva la tarea
> (`Explore` antes que `general-purpose`). Menos superficie, menos riesgo.

---

## Mapeo por modo

### `modo:gdd`
- **`Explore`** para el barrido "qué está implementado en código vs. qué dice el
  GDD" a lo ancho de muchos archivos. El ritual de este modo ya lee mucho; el
  barrido de gaps es exactamente fan-out → delegalo y traé solo el mapa de
  sistemas (✓/~/✗).

### `modo:diseno`
- **`Explore`** para reconocer cómo está hecho HOY el sistema que se va a tocar
  (contratos, call sites, acoplamiento existente) antes de escribir el spec.
- **`Plan`** para bocetar approaches alternativos cuando el diseño tiene más de
  una salida razonable; el resultado alimenta las secciones `[PROPUESTA]` /
  `[ALTERNATIVA]` del spec.

### `modo:implementacion`
- **`Explore`** para localizar call sites, firmas y puntos de inserción antes de
  editar.
- **`general-purpose`** para implementar piezas **aisladas e independientes** en
  paralelo (p. ej. varios SOs/effects sin dependencias cruzadas). Cada agente
  trabaja sobre su archivo; nada que toque protegidos.
  - **Caveat protegidos:** si la pieza toca `TurnManager`/`ActionQueue`/
    `PlayerCombatActor`, NO se delega la edición. El subagente puede mapear el
    punto exacto; el cambio lo hace el hilo principal con aprobación.
- **`/code-review`** (skill) al cerrar la sub-tarea, antes del PR.

### `modo:revision`
- Subagentes en **paralelo, uno por dimensión** del review:
  1. Correctitud lógica / gameplay
  2. Adherencia a `GOLDEN_RULES.md`
  3. Arquitectura / acoplamiento / fugas de estado a UI
- Cada uno devuelve su lista de issues priorizados; el hilo principal sintetiza
  el Reporte de Revisión con la plantilla de `_plantillas.md`. La paralelización
  acá da cobertura más amplia sin inflar el contexto del revisor.

---

## Cómo escribir un buen prompt de subagente

- **Da el objetivo y el formato de salida.** El subagente devuelve solo su
  mensaje final; pedile estructura (rutas + `file:line`, firmas, conclusión).
- **Acotá el alcance.** Decile qué archivos/carpetas mirar y qué NO hacer
  ("no modifiques nada, solo explorá y reportá").
- **Pedí evidencia, no opinión.** Citas de líneas exactas > resúmenes vagos.
- **Para paralelo:** lanzá los subagentes independientes en un mismo turno
  (varias tool calls) para que corran concurrentes.

---

## Skills propias (gaps de tooling)

Relacionado pero distinto de los subagentes (ver también CLAUDE.md raíz,
"Sobre skills propias"). Si en cualquier modo aparece una operación de editor
Unity **repetitiva** (mismo patrón manual ≥2-3 veces: generar SOs en lote,
poblar configs, asignar arte por SerializedObject, etc.):

1. **Proponer** —no crear— una skill propia con `unity-skill-create`,
   describiendo qué automatiza y qué ahorra (tokens / clicks manuales / errores).
2. Sebastián decide si vale la pena. Si sí, se crea y queda versionada en el repo.
3. Nunca se crea una skill sin su aprobación explícita.

> Patrón histórico que justificaría esto: los menús `Roguelike > Generate Relic
> Assets`, `Setup Shop Config`, `Setup New Run Config` de M3 se corrieron a mano.
> Si M4 trae un equivalente (p. ej. generar `EventDefinition` SOs en lote), es
> candidato a skill propia.
