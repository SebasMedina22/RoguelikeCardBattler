# Spec — Sub-PR 3F: Mapa horizontal (refactor scroll lateral)

> **ESTADO: IMPLEMENTADO — PR #98 (2026-06-05).** Cierra M3.

> Estado: **CERRADO** (sin decisiones abiertas). Último sub-PR de M3 — su cierre
> cierra el milestone. Diseñado 2026-06-05 en `modo:diseno`.

## Origen

- `[GDD]` §"DD-005 / Sistema de nodos": *"El jugador atravesará un mapa de
  **scroll lateral (izquierda a derecha)** que se creará a través de un sistema
  de semillas"*.
- `GOLDEN_RULES.md` §7 → "Estructura del mapa: **Scroll lateral (izquierda a
  derecha)**".
- `_roadmap.md` → M3 Sub-PR 3F (checklist de 5 puntos).
- Es un **refactor de UX puro**, no una feature nueva: el mapa hoy scrollea
  vertical (filas por profundidad). Hay que invertirlo a horizontal.

## Objetivo

Que el mapa del run se presente como un scroll horizontal izquierda→derecha
(start a la izquierda, boss a la derecha), coherente con el GDD y la estética de
roguelike de progresión lateral. El refactor es **puramente visual**: no puede
alterar la topología generada, los tipos de nodo ni la asignación de enemigos
(determinismo por seed intacto). Además integra el inventario de Retazos al HUD
del mapa (consistencia con el HUD de combate, pendiente declarado desde 3B).

## Comportamiento esperado

Desde la perspectiva del jugador:

- Entra a RunScene y ve el mapa con el **nodo inicial a la izquierda** y el
  **boss a la derecha**. Las columnas avanzan por profundidad hacia la derecha.
- Los nodos del mismo depth (las ramas paralelas de una bifurcación) se apilan
  **verticalmente, centrados** en su columna.
- El mapa **scrollea horizontalmente** (drag / rueda) cuando no entra completo
  en pantalla. No scrollea en vertical.
- La animación de entrada sigue siendo escalonada por profundidad: aparece
  primero el start (izquierda), por último el boss (derecha).
- Una **fila de íconos de Retazos** es visible siempre en la parte superior del
  HUD del mapa (igual que en combate), reflejando la build actual.
- Todo el resto del flujo (click en nodo disponible → entra; nodos
  completados/bloqueados; highlight de aristas al desbloquear) se comporta
  idéntico a hoy.

## Sistemas afectados

- **UI (Run/Map/UI):** `RunMapView` (refactor de ejes + ScrollRect),
  `RunMapNodeView` y `RunMapEdgeView` (ajuste de anchor), `RunFlowController`
  (instancia el `RelicInventoryView` en el HUD del mapa).
- **Generación (Run/Map):** `[CÓDIGO ACTUAL]` `RunMapGenerator` es **agnóstico
  al eje** → **NO se toca** (ver Decisiones cerradas #1).
- **RunState / ScriptableObjects:** sin cambios.
- **Archivos protegidos (TurnManager/ActionQueue/PlayerCombatActor):** **NINGUNO**.

## Archivos a crear

- `Assets/Tests/EditMode/RunMapGeneratorTests.cs` — tests de determinismo que
  blindan la generación (ver "Casos de prueba"). **No existe hoy ningún test del
  sistema de mapa** → este archivo es la red de seguridad del refactor.

## Archivos a modificar

- `Assets/Scripts/Run/Map/UI/RunMapView.cs` — el grueso del refactor: invertir
  el mapeo de ejes, el `ScrollRect` y el dimensionado del content.
- `Assets/Scripts/Run/Map/UI/RunMapNodeView.cs` — cambiar el anchor del nodo de
  `(0.5, 1)` a `(0, 0.5)` para alinear con el nuevo pivot del content.
- `Assets/Scripts/Run/Map/UI/RunMapEdgeView.cs` — mismo cambio de anchor
  `(0.5, 1)` → `(0, 0.5)`. La matemática de la arista (atan2/distancia) es
  agnóstica al eje y **no cambia**.
- `Assets/Scripts/Run/RunFlowController.cs` — instanciar `RelicInventoryView` en
  el HUD del mapa y refrescarlo en `ShowMap()`.

## Archivos protegidos involucrados

- [x] **Ninguno.** Todo vive en `Run/Map/UI/`, `Run/RunFlowController.cs` y
  `Tests/EditMode/`. `RunMapGenerator` (no protegido) tampoco se toca.

---

## Contratos

### 1. Refactor de ejes en `RunMapView.cs`

`[PROPUESTA]` Reemplazar las constantes y el mapeo. Hoy (vertical):

```
depth  → Y  (filas, hacia abajo)   via  y = -(TopPadding + depth * RowSpacing)
índice → X  (columnas, centrado)   via  x = (index - (count-1)/2) * HorizontalSpacing
```

Nuevo (horizontal):

```
depth  → X  (columnas, hacia la derecha)   x = LeftPadding + depth * ColumnSpacing
índice → Y  (apilado vertical, centrado)   y = -(index - (count-1)/2) * NodeRowSpacing
```

**Constantes nuevas del eje** (renombrar las actuales, no agregar duplicados):

| Constante | Valor `[PROPUESTA]` | Rol |
|-----------|---------------------|-----|
| `ColumnSpacing` | `200f` | separación horizontal entre columnas de depth (hoy el valor vivía en `HorizontalSpacing`, ahora gobierna el eje de avance) |
| `NodeRowSpacing` | `120f` | separación vertical entre nodos del mismo depth (hoy era `RowSpacing=130`; se baja un poco porque ahora compite con alto de pantalla, no de scroll infinito) |
| `LeftPadding` | `80f` | padding antes de la primera columna (reemplaza `TopPadding`) |
| `RightPadding` | `80f` | padding tras la última columna (reemplaza `BottomPadding`) |
| `NodeWidth` | `130f` | sin cambio |
| `NodeHeight` | `50f` | sin cambio |
| `EdgeThickness` | `3f` | sin cambio |
| `EntranceDelayPerDepth` | `0.08f` | sin cambio (sigue escalonando por depth, ahora izq→der) |

**Centrado vertical de cada columna:** el signo del término de índice se mantiene
negativo (`-(index - (count-1)/2) * NodeRowSpacing`) para que el orden de apilado
sea estable respecto al actual; con el content pivoteado en el centro vertical
(ver abajo) `y=0` es el centro de la pantalla y la columna se reparte simétrica
arriba/abajo. Una columna de 1 nodo queda en `y=0` (centrada).

**`GetNodePosition` nuevo** (firma idéntica, sólo cambia el cuerpo):

```
int depth = depthMap[nodeId];
List<int> bucket = depthBuckets[depth];
int index = bucket.IndexOf(nodeId);
int count = bucket.Count;

float x = LeftPadding + depth * ColumnSpacing;
float y = -(index - (count - 1) / 2f) * NodeRowSpacing;
return new Vector2(x, y);
```

**Dimensionado del content (por ANCHO):**

```
float contentWidth = LeftPadding + maxDepth * ColumnSpacing + RightPadding;
content.sizeDelta = new Vector2(contentWidth, 0f);
```

(hoy es `contentHeight` sobre el eje Y con `sizeDelta = (0, contentHeight)`).

### 2. `CreateScrollView` — invertir ScrollRect y pivot del content

`[PROPUESTA]` Cambios dentro de `CreateScrollView`:

- `scroll.horizontal = true;` / `scroll.vertical = false;` (hoy es al revés).
- `movementType = Clamped` se mantiene.
- **Content anchoring/pivot** — hoy stretch horizontal anclado arriba:
  ```
  contentRect.anchorMin = new Vector2(0f, 1f);
  contentRect.anchorMax = new Vector2(1f, 1f);
  contentRect.pivot     = new Vector2(0.5f, 1f);
  ```
  Nuevo: stretch vertical anclado a la izquierda, pivot en centro-izquierda:
  ```
  contentRect.anchorMin = new Vector2(0f, 0f);
  contentRect.anchorMax = new Vector2(0f, 1f);
  contentRect.pivot     = new Vector2(0f, 0.5f);
  contentRect.anchoredPosition = Vector2.zero;
  contentRect.sizeDelta = new Vector2(800f, 0f);  // placeholder, lo pisa el dimensionado por ancho
  ```
- El rect del ScrollView (`scrollRect.anchorMin/Max`) **baja su techo** para dejar
  una franja superior limpia para el HUD de Retazos (ver #4): cambiar el
  `anchorMax` de `(0.98, 0.78)` a `(0.98, 0.72)`. El `anchorMin` `(0.02, 0.02)`
  se mantiene.

### 3. `RunMapNodeView` y `RunMapEdgeView` — anchor

`[PROPUESTA]` Ambos hardcodean hoy `anchorMin/Max = (0.5, 1)` y `pivot` para
alinear con el content pivot anterior. Con el content pivoteado en `(0, 0.5)`,
ambos deben usar:

```
Rect.anchorMin = new Vector2(0f, 0.5f);
Rect.anchorMax = new Vector2(0f, 0.5f);
// pivot del nodo se mantiene (0.5, 0.5); pivot de la arista se mantiene (0.5, 0.5)
```

`[CÓDIGO ACTUAL]` Confirmado: ningún otro cambio. La animación de entrada
(`PlayEntrance`, scale+fade), el pulse de nodos disponibles, el punch al
completar y el `AnimateHighlight` de aristas son **agnósticos al eje** — sólo
operan sobre `localScale`/`alpha`/`color`, nunca sobre posición. Reciben las
posiciones ya calculadas por `RunMapView`. La matemática de la arista
(`Atan2(diff.y, diff.x)`, magnitud) funciona con cualquier par de posiciones.

### 4. `RelicInventoryView` en el HUD del mapa (`RunFlowController`)

`[CÓDIGO ACTUAL]` `RunFlowController.BuildUI()` ya tiene `uiFont`,
`GetWhiteSprite()`, `_state` y `_root` (RectTransform del RunCanvas). El estado
expone `_state.Relics` (la misma lista que lee el dispatcher). Todos los caminos
de retorno al mapa (combate, tienda, hoguera, evento, derrota cancelada) pasan
por `ShowMap()`.

`[PROPUESTA]` Espejo del patrón de combate (`CombatUIController` crea un panel
`RelicBar` y mete dentro un `RelicInventoryView`):

- Nuevo campo `private RelicInventoryView _relicView;`.
- En `BuildUI()`, tras crear `_mapPanel` y antes/después de `_mapView`, crear una
  franja horizontal **dentro de `_mapPanel`** que no pise el scroll:
  ```
  // Franja de Retazos: arriba del scroll del mapa (techo del scroll = 0.72),
  // debajo del status (0.80). Banda dedicada → no colisiona con el área scrolleable.
  RectTransform relicBar = CreatePanel("RelicBar", _mapPanel,
      <anchorMin (0.02, 0.72)>, <anchorMax (0.98, 0.79)>, new Color(0,0,0,0));
  _relicView = new RelicInventoryView(relicBar, uiFont);
  ```
  (usar el helper `CreatePanel` existente; los valores exactos de anchor son
  `[PROPUESTA]` — la banda 0.72–0.79 queda libre tras bajar el techo del scroll a
  0.72 en #2, y bajo el status 0.80–0.88).
- En `ShowMap()`, tras `_mapView?.Refresh(_state)`, agregar
  `_relicView?.Refresh(_state.Relics);`. Refrescar en `ShowMap` es suficiente:
  los Retazos sólo cambian fuera del mapa (combate/elite/boss/tienda) y todo
  retorno al mapa pasa por `ShowMap`.
- `RelicInventoryView` ya es presentación pura no-MonoBehaviour, ya construye su
  propio tooltip colgado del Canvas raíz y ya tiene `Cleanup()`. **Llamar
  `_relicView?.Cleanup()`** donde hoy se hace `_mapView?.Cleanup()` (transición
  de escena) para no dejar el tooltip huérfano.

`[INTERPRETACIÓN]` No se necesita `AddRelic` (pulse-on-acquire) en el mapa: la
adquisición de Retazos ocurre en combate/tienda con su propio feedback; en el
mapa basta con que el ícono esté presente. `Refresh` crea los íconos faltantes.

---

## Reuso

- `RelicInventoryView` (3B) — se reusa tal cual, sin tocar su código.
- `CreatePanel` / `GetWhiteSprite` / `uiFont` de `RunFlowController` — helpers
  existentes.
- `UIAnimationHelper`, DOTween — ya usados por los node/edge views; sin cambios.
- `RunMapGenerator.Generate` / `AssignEnemies` — se reusan intactos; los tests
  nuevos los ejercitan.

## Casos de prueba (EditMode) — `RunMapGeneratorTests.cs`

El refactor es visual; el blindaje prueba que la **generación** (lógica pura) no
cambió. `RunMapGenerator` es `static` y no necesita Unity runtime → testeable en
EditMode. Se construye un `Act1MapConfig` vía `ScriptableObject.CreateInstance`
(mismo patrón que `GenerateAct1`).

1. **Determinismo de topología:** `Generate(config, seed=12345)` dos veces →
   mismo `StartNodeId`, misma cantidad de nodos, mismos `Type` por Id y mismas
   `Connections` por Id (comparación profunda). Es el corazón del blindaje:
   "misma seed = mismo mapa".
2. **Determinismo de enemigos:** `AssignEnemies(map, pool, seed=12345)` sobre dos
   mapas idénticos → mismo `SpecificEnemy` por nodo Combat/Elite. "misma seed =
   mismos enemigos".
3. **Seeds distintas pueden divergir:** `Generate(config, seed=1)` vs `seed=2`
   producen al menos una diferencia (topología o tipos). Guard suave: si por
   azar coincidieran, el test no debe ser frágil → afirmar sobre el log de
   template index o sobre el arreglo de tipos con dos seeds elegidas que se sabe
   difieren (elegir seeds tras una corrida exploratoria; documentar en comentario).
4. **DAG válido invariante:** el mapa generado mantiene nodo 0 sin entrantes,
   nodo `TotalNodes-1` sin salientes, y todos los `connId` ∈ rango válido
   (sanity del contrato de `Topologies`, independiente del eje).
5. **Boss/end forzado:** `types[total-1] == ForcedEndType` y
   `types[0] == ForcedStartType` (la asignación de tipos no se ve afectada por el
   refactor visual — pin defensivo).

`[INTERPRETACIÓN]` No se testea `RunMapView.GetNodePosition` directamente: es
`private static` sobre UI y exponerlo (InternalsVisibleTo) agrega acoplamiento de
test por un cálculo trivial. El contrato que importa blindar es el de generación;
el layout se valida en la pasada visual en escena.

## Validación manual (RunScene)

1. Abrir RunScene en Play (o entrar vía MainMenu → NewRunScene → RunScene).
2. **Resultado esperado:** el mapa se ve con el start a la **izquierda**, el boss
   a la **derecha**, columnas avanzando hacia la derecha; nodos del mismo depth
   apilados verticalmente y centrados. Scroll horizontal funciona (drag/rueda);
   no hay scroll vertical.
3. La animación de entrada aparece izq→der (start primero, boss último).
4. La **fila de íconos de Retazos** está visible arriba del mapa y no se solapa
   con el área scrolleable ni con title/status. Tooltip al hover funciona.
5. Completar un nodo → highlight dorado de aristas hacia los nodos desbloqueados;
   los nuevos nodos disponibles pulsan. Avanzar hasta el boss.
6. Volver de tienda/elite con un Retazo nuevo → al re-mostrar el mapa el ícono
   aparece en la fila.
7. **Zero console errors** durante todo el recorrido. Sin warnings de
   "destroyed RectTransform" al cambiar de escena (Cleanup correcto).

## Decisiones cerradas

1. **`RunMapGenerator` NO se toca.** `[CÓDIGO ACTUAL]` confirmado leyendo el
   archivo: sólo asigna tipos (peso) y aristas (templates DAG); nunca calcula
   posiciones ni asume eje. `AssignEnemies` usa BFS depth, agnóstico al layout.
   El eje vive **enteramente** en `RunMapView.GetNodePosition` +
   `CreateScrollView`. Declarado para no inventar trabajo (punto 2 del checklist).
2. **`RunMapNodeView`/`RunMapEdgeView` sólo cambian el anchor** (1 línea cada
   uno). Su lógica de animación y la matemática de aristas son agnósticas al eje
   y reciben posiciones ya calculadas (punto 3 del checklist).
3. **HUD de Retazos en banda dedicada superior** (0.72–0.79), bajando el techo
   del scroll a 0.72. No se reusa un layout flotante sobre el scroll para evitar
   solapamiento de raycasts con los nodos.
4. **Refresh en `ShowMap`, sin pulse-on-acquire.** Suficiente porque los Retazos
   sólo cambian fuera del mapa y todo retorno pasa por `ShowMap`.
5. **El blindaje de determinismo apunta a `RunMapGenerator`** (lógica pura), no a
   `RunMapView` (UI). Es donde vive el contrato "misma seed = mismo mapa".
6. **Estética handmade:** sin cambios de paleta ni sprites en este sub-PR; el
   mapa usa los mismos colores/whiteSprite actuales. El arte del mapa entra en el
   plan de auditoría de arte post-M3, no aquí.

## Alternativas consideradas

- **`[ALTERNATIVA]` Usar `HorizontalLayoutGroup` para las columnas** — descartada:
  el layout es un DAG con ramas de ancho variable y aristas diagonales; un layout
  group no modela posiciones absolutas con bifurcaciones. El posicionamiento
  manual actual (anchoredPosition) ya resuelve esto y sólo hay que invertir ejes.
- **`[ALTERNATIVA]` Parametrizar el eje (enum Vertical/Horizontal) en `RunMapView`**
  — descartada: nadie necesita el modo vertical nunca más (el GDD pide horizontal
  definitivo). Mantener ambos modos agrega ramas muertas. Se hace el flip directo.
- **`[ALTERNATIVA]` Reutilizar el `relicBar` de combate moviéndolo a un helper
  compartido** — descartada por ahora: el panel de combate vive en
  `CombatUIController` (otra escena/controller) y extraer un helper compartido es
  scope creep. Cada HUD instancia su propio `RelicInventoryView` (la view ya es
  reutilizable; sólo cambia el parent). Posible cleanup futuro si aparece un 3er
  consumidor.

## Estimación

- **Complejidad: baja-media.** El refactor de ejes es mecánico y acotado a 1
  archivo grande (`RunMapView`) + 2 cambios de 1 línea + integración del HUD +
  tests nuevos.
- **Sub-tareas:**
  1. Flip de ejes + ScrollRect + dimensionado en `RunMapView`.
  2. Anchor en `RunMapNodeView` y `RunMapEdgeView`.
  3. `RelicInventoryView` en `RunFlowController` (build + Refresh + Cleanup).
  4. `RunMapGeneratorTests.cs` (5 casos).
  5. Validación visual en escena + suite EditMode.
- **Riesgo:** bajo. El único riesgo real es que el flip de pivot/anchor descoloque
  visualmente nodos o aristas → se mitiga con la pasada visual obligatoria. El
  determinismo está cubierto por código intacto + tests nuevos. Sin tocar
  protegidos.

---

## Prompt de handoff para `modo:implementacion`

```text
modo:implementacion

Implementá el Sub-PR 3F — Mapa horizontal (refactor scroll lateral), el último
sub-PR de M3 (su cierre). El spec cerrado está en
`Docs/dev/specs/m3_3f_horizontal_map_spec.md` — leelo completo antes de tocar
código; todas las decisiones de diseño ya están cerradas, no abras ninguna.

Setup de branch (dependencia previa: PR #97 / 3E ya MERGED a main, 6d171a5):
  git fetch --all --prune
  git checkout -b feat/m3-sub-f-horizontal-map origin/main

Qué construir (el detalle y los contratos están en el spec):
Modificar:
- `Assets/Scripts/Run/Map/UI/RunMapView.cs` — invertir ejes (depth→X / índice→Y),
  ScrollRect horizontal=true/vertical=false, content pivot (0,0.5) anclado
  izquierda, dimensionar por ANCHO (contentWidth), nuevas constantes
  (ColumnSpacing/NodeRowSpacing/LeftPadding/RightPadding), bajar techo del scroll
  a anchorMax.y=0.72.
- `Assets/Scripts/Run/Map/UI/RunMapNodeView.cs` — anchor (0.5,1) → (0,0.5).
- `Assets/Scripts/Run/Map/UI/RunMapEdgeView.cs` — anchor (0.5,1) → (0,0.5).
- `Assets/Scripts/Run/RunFlowController.cs` — instanciar RelicInventoryView en
  banda superior del _mapPanel (0.72–0.79), Refresh en ShowMap, Cleanup en la
  transición de escena.
Crear:
- `Assets/Tests/EditMode/RunMapGeneratorTests.cs` — 5 casos de determinismo
  (topología, enemigos, divergencia por seed, DAG válido, start/end forzados).

Reglas no negociables:
- NO tocar archivos protegidos (TurnManager, ActionQueue, PlayerCombatActor) —
  este PR no los necesita.
- NO tocar `RunMapGenerator.cs`: es agnóstico al eje (decisión cerrada #1). Si
  durante la implementación parece que hace falta tocarlo, PARAR — algo se
  entendió mal respecto al spec.
- No manual editor setup: el mapa se construye en runtime por RunFlowController;
  nada nuevo que asignar en inspector.
- Reusar RelicInventoryView tal cual (no editar su código); sólo cambia el parent.
- Estética handmade: sin cambios de paleta/sprites en este PR.

Validación obligatoria antes de cerrar:
- Compilación limpia (zero console errors).
- `RunMapGeneratorTests` 5/5 verde + suite EditMode completa sin regresiones
  (vía `tests-run` EditMode o Test Runner).
- Flujo end-to-end en escena (Unity-MCP / Play): mapa horizontal start-izq /
  boss-der, scroll lateral, entrada izq→der, fila de Retazos visible sin pisar el
  scroll, recorrido completo hasta el boss sin errores ni warnings de
  RectTransform destruido.
- OJO (Insight de 3E): no dejar que `.claude/settings.json` viaje en el PR de
  feature con permisos auto-agregados — revisar el diff antes de commitear.

Al cerrar (cierra M3):
- `_roadmap.md`: marcar los 5 checkboxes de Sub-PR 3F `[x]`; mover M3 completo de
  "Activo" a "Completados" con fecha y resumen; activar M4 como milestone activo;
  registrar aprendizajes en `_insights.md` si los hubo.
- `_tech_snapshot.md`: nota del refactor horizontal de RunMapView + RelicInventory
  en HUD del mapa + nuevo RunMapGeneratorTests.cs (subir conteo de tests).
- Commit + push + PR a main (Closes #<issue de 3F si existe>).
- Tras el merge: arranca el plan de auditoría de arte (memoria
  project-art-audit-plan).
```
