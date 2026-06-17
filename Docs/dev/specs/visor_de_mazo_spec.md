# Spec — Visor de mazo ("librito" estilo Slay the Spire)

> **ESTADO: IMPLEMENTADO + fixes de revisión (2026-06-16).** Branch `feat/deck-viewer`
> (PR #123). Suite EditMode 166 → 181/181. Revisión aplicada: helpers de label
> canonizados en `CardDisplay` (M1), Test 7 des-tautologizado (M3), +3 tests de
> `BuildTooltip` (M2), badge reubicado a esquina sup. izquierda. E2E visual pendiente
> de confirmación por Sebastián.

## Origen
- Roadmap `_roadmap.md` § "Pulido pre-M4 — Visor de mazo". Pieza de pulido
  independiente (formato como el #104/#107), previa a M4.
- `[INTERPRETACIÓN]` Es la herramienta de playtesting de los bloques 4a y 4b:
  sin un visor del mazo fuera de combate, los eventos que dan/quitan/mejoran
  cartas son invisibles para verificar qué pasó con el mazo.

## Objetivo
UI consultable, de solo lectura, que lista el mazo completo del run. Permite al
jugador (y a quien playtestea) revisar en cualquier momento qué cartas tiene,
sus tipos, costes, descripciones y la mejora disponible de cada una.

## Comportamiento esperado

**Punto de entrada (siempre visible, incluido combate).** En la esquina superior
del HUD hay un botón fijo `Mazo (N)` (N = cantidad de cartas). Está disponible:
- En el mapa del run (RunScene).
- Dentro de los nodos que tapan el mapa (Tienda, Hoguera, Resolve, Recompensa):
  el botón flota por encima de esos paneles opacos.
- En combate (BattleScene).

**Al pulsar el botón** se abre un panel modal (overlay) sobre la escena actual:
- Fondo oscuro semi-opaco que bloquea los clics de lo que hay detrás (no pausa
  nada: el combate es por turnos, no en tiempo real).
- Título `Mazo (N)`.
- Lista vertical scrolleable con una fila por carta del mazo, **ordenada por
  tipo de carta → coste → nombre**.
- Botón `Cerrar` (y el botón `Mazo (N)` actúa como toggle).

**Cada fila** muestra, en una línea de texto clara y **coloreada por tipo
elemental** (reusa `ElementTypeColors.TypePrefix`):
- Carta simple: `[Tipo] Nombre · {coste}⚡`.
- Carta dual: **ambos lados** → `[TipoA] NombreA / [TipoB] NombreB · {coste}⚡`
  (colapsa a un solo token solo si nombre Y tipo coinciden en los dos lados —
  mismo criterio que `ShopNodeController.BuildCardSelectLabel`).
- Marcador de mejora al final de la fila:
  - `★` si la carta ya está mejorada (`entry.IsUpgraded`).
  - `+` si la carta tiene una mejora disponible y aún no aplicada
    (`entry.CanUpgrade()`).
  - sin marcador si no tiene mejora configurada.

**Al pasar el mouse / tocar una fila** aparece un tooltip (mismo mecanismo que
`RelicInventoryView`: FadeIn/FadeOut vía `UIAnimationHelper`) con el detalle
completo:
- Por cada lado (A y B en duales, único en simples): `[Tipo] Nombre · coste` +
  descripción completa.
- Bloque de **preview de mejora** si aplica:
  - Si ya está mejorada: línea `★ Mejorada`.
  - Si tiene mejora disponible: bloque `Mejora ▸` con el nombre/coste/descripción
    que tendría al mejorarse (delta legible: coste nuevo si lo cambia, descripción
    mejorada).
  - Si no tiene mejora configurada: nada.

**Datos = solo lectura.** El visor nunca muta el mazo ni ningún estado. Solo
consulta `RunState.Deck`.

## Sistemas afectados
- **UI:** clase de presentación nueva `DeckViewerView` (molde: `RelicInventoryView`).
- **Run:** `RunFlowController` instancia el visor en RunScene.
- **Combate:** `CombatUIController` instancia el visor en BattleScene.
- **RunState:** solo lectura de `RunState.Deck` (vía `GetDeckSnapshot()`). Sin
  cambios.
- **ScriptableObjects:** ninguno nuevo. Lee `CardDefinition` / `DualCardDefinition`
  / `CardUpgradeDef` ya existentes.
- **Combate (TurnManager / protegidos):** **NINGUNO.** Ver "Archivos protegidos".

## Archivos a crear
- `Assets/Scripts/Gameplay/Cards/UI/DeckViewerView.cs`
  — namespace `RoguelikeCardBattler.Gameplay.Cards.UI`. Clase de presentación
  pura (NO MonoBehaviour), molde de `RelicInventoryView`. Vive en el módulo
  `Cards` porque es la capa compartida más baja que **tanto Run como Combat ya
  referencian**. `[CÓDIGO ACTUAL]` todo el runtime compila en un **único asmdef**
  (`Assets/Scripts/RoguelikeCardBattler.asmdef`, `references: []`) → no hay riesgo
  de dependencia circular a nivel asmdef en NINGUNA ubicación, y Combat→Run ya
  existe (`CombatUIController.cs:8` `using RoguelikeCardBattler.Run;` +
  `RunSession.Instance`). Se elige `Cards` para no profundizar el acoplamiento de
  una vista de cartas a `Run`, no porque evite un ciclo (no lo hay).
- `Assets/Tests/EditMode/DeckViewerTests.cs`
  — cubre los helpers static puros (orden, label, tooltip, preview de mejora).

## Archivos a modificar
- `Assets/Scripts/Run/RunFlowController.cs`
  — campo `DeckViewerView _deckViewer`; instanciarlo en `BuildUI()`; `Refresh`
  con `_state.GetDeckSnapshot()` en `BuildUI` y `ShowMap`; `Cleanup()` junto a
  `_relicView?.Cleanup()` en las transiciones de escena
  (`StartCombatForNode`, retry de derrota, `ReturnToMainMenu`).
- `Assets/Scripts/Gameplay/Combat/CombatUIController.cs`
  — campo `DeckViewerView _deckViewer`; instanciarlo en `BuildUI()` (junto al
  `_relicView`); `Refresh` con `RunSession.Instance?.State?.GetDeckSnapshot()`.
  `[CÓDIGO ACTUAL]` `CombatUIController` **NO es archivo protegido** (~752 LOC,
  ya se modifica libremente; los protegidos son solo TurnManager / ActionQueue /
  PlayerCombatActor).

## Archivos protegidos involucrados
- [x] **Ninguno.** El visor no toca combate. La resolución del lado activo de las
  duales NO depende de `TurnManager` porque el visor muestra **ambos lados
  siempre** (es agnóstico al mundo actual) → no necesita `GetActiveCardDefinition`
  ni el `WorldSide` vigente.

## Contratos

### Datos
No introduce estructuras de datos nuevas ni campos en `RunState`/SOs. Consume:
- `RunState.Deck : List<CardDeckEntry>` (vía `GetDeckSnapshot()`). `[CÓDIGO
  ACTUAL]` `GetDeckSnapshot()` es una **copia SHALLOW** (`new List<>(Deck)`):
  lista nueva pero comparte las MISMAS instancias `CardDeckEntry` que el mazo
  vivo. Inocuo porque el visor es de solo lectura — refuerza el contrato: nunca
  llamar mutadores sobre las entries (el visor solo lee
  `SingleCard`/`DualCard`/`IsUpgraded`/`CanUpgrade()`).
- `CardDeckEntry`: `SingleCard`, `DualCard`, `IsUpgraded`, `CanUpgrade()`.
  `[CÓDIGO ACTUAL]` **Semántica dual de `CanUpgrade()` = CUALQUIER lado**
  (`CardDeckEntry.cs:38-42`: `aHas || bHas`). El marcador `+` y el gate del
  preview se rigen por esto: una dual con upgrade en UN solo lado ya es mejorable.
- `CardDefinition`: `CardName`, `Type` (CardType), `Cost`, `Description`,
  `ElementType`, `Upgrade` (`CardUpgradeDef`).
- `DualCardDefinition`: `DisplayName`, `SideA`, `SideB`. `[CÓDIGO ACTUAL]`
  **NO expone `Cost`** — cada lado es un `CardDefinition` con su propio `Cost`. El
  "comparten coste" es solo convención de autoría (comentario de la clase), NO
  está enforced. → el coste de la fila se lee del lado representante
  (`RepresentativeCard.Cost`, espejo de `ShopNodeController.RepresentativeCard`).
- `CardUpgradeDef`: `HasUpgrade`, `OverrideCost`, `UpgradedCost`, `UpgradedName`,
  `UpgradedDescription`.

### APIs públicas (de `DeckViewerView`)

**Instancia (presentación):**
- `DeckViewerView(Canvas hostCanvas, Font font)` — crea, bajo `hostCanvas`, un
  **sub-Canvas overlay propio** (`overrideSorting = true`, `sortingOrder` alto,
  p. ej. 100) que contiene el botón `Mazo (N)` + el panel modal + el tooltip. El
  sub-Canvas garantiza que el visor flote por encima de paneles opacos
  (ShopPanel, CampfirePanel) y del HUD de combate sin coordinar con ellos.
  `[PROPUESTA]` **Divergencia intencional del molde:** `RelicInventoryView` toma
  `(RectTransform parent, Font)` y monta el tooltip vía
  `parent.GetComponentInParent<Canvas>()`. Acá el ctor toma un `Canvas` y el
  tooltip se parenta bajo el **sub-Canvas nuevo** (NO copiar el patrón
  parent-RectTransform del molde para el tooltip).
- `void Refresh(IReadOnlyList<CardDeckEntry> deck)` — actualiza el contador del
  badge y, si el modal está abierto, reconstruye la lista. Cachea la última lista
  para reconstruir al abrir. Lee la lista que le pasa el owner (no referencia
  `RunState`). **Null-safe:** `deck == null` se trata como vacío → badge
  `Mazo (0)`, lista vacía, sin excepción. (En BattleScene standalone sin
  `RunSession`, `RunSession.Instance?.State?.GetDeckSnapshot()` es `null`.)
- `void Open()` / `void Close()` / `void Toggle()` — control del modal. `Open`
  reconstruye la lista desde la última recibida en `Refresh`.
- `void Cleanup()` — destruye los GameObjects creados (sub-Canvas + tooltip),
  espejo de `RelicInventoryView.Cleanup()`. La llaman los owners en transiciones
  de escena.

**Helpers static puros (testeables sin UI — patrón `ShopNodeController.BuildStock`
/ `StarterDraft`):**
- `static List<CardDeckEntry> SortForDisplay(IReadOnlyList<CardDeckEntry> deck)`
  — devuelve una **lista nueva** ordenada por
  `(CardType del representante, Cost del representante, Nombre, Id)`. No muta la
  entrada. **Orden ESTABLE obligatorio:** usar `OrderBy/ThenBy` (LINQ, estable) o
  un tie-break final por `Id` — NO `List<T>.Sort` (no es estable) → así dos
  cartas con la misma `(tipo,coste,nombre)` conservan su orden de entrada de forma
  determinista (evita tests flaky).
  - Representante = `SingleCard ?? DualCard.SideA` (espejo de
    `ShopNodeController.RepresentativeCard`). Entradas inválidas (`!IsValid`) se
    filtran. **Edge case (`SideA` null):** una dual con `DualCard != null` pero
    `SideA == null` ES `IsValid` (IsValid solo mira que `dualCard != null`), así
    que NO la filtra el guard `!IsValid`; el representante caería en null →
    `RepresentativeCard` debe hacer fallback `SideA ?? SideB` y, si ambos son
    null, la entrada se omite. Sin esto hay riesgo de `NullReferenceException` al
    leer `rep.Cost`/`rep.Type`.
- `static string BuildRowLabel(CardDeckEntry entry)` — la línea de la fila
  (rich-text con `TypePrefix`, ambos lados en duales, coste, marcador ★/+). Los
  lados null se renderizan como `?` (mismo guard que `CardToken`); nunca crashea.
- `static string BuildTooltip(CardDeckEntry entry)` — el cuerpo del tooltip
  (por lado: tipo/nombre/coste/descripción + bloque de preview de mejora).
- `static string BuildUpgradePreview(CardDeckEntry entry)` — `""` si no hay
  mejora configurada; `★ Mejorada` si `IsUpgraded`; si no, el bloque `Mejora ▸`
  con lo que la carta SE CONVERTIRÍA al mejorarse. Lee los campos de
  `CardUpgradeDef` **sin** llamar `CreateUpgradedClone()` (no instancia SOs para
  un preview): nombre vacío ⇒ `Nombre+`, descripción vacía ⇒ la base, coste =
  `UpgradedCost` si `OverrideCost` else el base.
  - **CRÍTICO — preview PER-LADO en duales.** `CardDeckEntry.CanUpgrade()` en
    duales devuelve true si **cualquiera** de los dos lados tiene upgrade
    (`aHas || bHas`), y `DualCardDefinition.CreateUpgradedClone()` mejora **solo
    los lados con `HasUpgrade`** (el otro queda igual) y renombra el dual a
    `displayName + "+"`. Un preview de un solo nombre/coste/descripción NO puede
    representar "lado A mejorado, lado B sin tocar". Por eso el preview de una
    dual se construye **por lado** (espejo de la estructura per-lado del tooltip):
    para cada lado, si `SideX.Upgrade.HasUpgrade` muestra sus valores nuevos, si
    no muestra el lado sin cambios. Así el preview COINCIDE con lo que produce un
    upgrade real (el jugador no ve un preview falso).

### Eventos
Ninguno. El visor no se suscribe a eventos de `TurnManager` ni dispara hooks.

## Reuso
- **Molde `RelicInventoryView`** (`Gameplay/Relics/UI/`): clase pura no-MonoBehaviour,
  `Refresh`/`Cleanup`, tooltip único al root con `EventTrigger`
  PointerEnter/PointerExit + FadeIn/FadeOut. Copiar esa estructura.
- **`ElementTypeColors.TypePrefix(ElementType)`** — token `[Tipo]` coloreado
  (fuente única de verdad; `None` ⇒ string vacío).
- **Patrón de label de carta dual** — `ShopNodeController.BuildCardSelectLabel` /
  `CardToken` / `RepresentativeCard`. `[CÓDIGO ACTUAL]` estos 3 son `private
  static` y **ya están duplicados** en `ShopNodeController` y
  `CampfireNodeController` (lo nota `_tech_snapshot.md`). Para NO crear una 3ª
  copia que arrastre el drift: el visor define sus versiones como **`public
  static` en el módulo `Cards`** (p. ej. un `CardDisplay` helper), que quedan como
  el **hogar canónico** de esta lógica. Migrar Shop/Campfire a ese helper queda
  como **follow-up fuera de scope** de este pulido (anotado abajo en Estimación)
  para no inflar la complejidad ni tocar tests de la Tienda/Hoguera. Criterio de
  colapso a replicar: colapsa a un token solo si nombre Y tipo coinciden en
  ambos lados, con lados null renderizados como `?` (nunca colapsan).
- **`UIAnimationHelper`** — `FadeIn`/`FadeOut` (tooltip), `ScaleIn` (apertura del
  modal, opcional).
- **`RunState.GetDeckSnapshot()`** — copia del mazo ya existente.
- **ScrollRect vertical estándar** (Viewport + Content con `VerticalLayoutGroup`
  + `ContentSizeFitter`). `[ALTERNATIVA]` reusar el `CreateScrollView` de
  `RunMapView` — descartado: ese está cableado para scroll **horizontal** y
  matemática de mapa; un ScrollRect vertical vanilla es más simple y sin
  acoplar `Cards` → `Run.Map`.
- **`CardDeckEntry.IsUpgraded` / `CanUpgrade()`** y **`CardDefinition.Upgrade`**
  (`CardUpgradeDef`) — datos de mejora ya existentes (Sub-PR 3C).

## Casos de prueba (EditMode) — `DeckViewerTests.cs`
1. **Orden tipo→coste→nombre:** dado un mazo con tipos/costes/nombres variados,
   `SortForDisplay` los devuelve agrupados por `CardType` (Attack→Skill→Power→
   Curse→Status), dentro por coste asc, luego nombre.
2. **Sort estable en claves iguales:** dos entries con misma `(tipo,coste,nombre)`
   conservan su orden de entrada relativo (valida `OrderBy/ThenBy` o tie-break por
   `Id`, no `List.Sort`).
3. **Sort no muta la entrada** y devuelve lista nueva (la original conserva su
   orden y conteo).
4. **Null / vacío:** `SortForDisplay(null)` y `SortForDisplay(empty)` ⇒ lista
   vacía, sin excepción. (Es la base de `Refresh(null)` → badge `Mazo (0)`.)
5. **Dual con `SideA` null no crashea:** entry dual con `SideA == null` (sigue
   siendo `IsValid`) ⇒ `SortForDisplay`/`BuildRowLabel` no lanzan
   `NullReferenceException` (fallback a `SideB` o se omite; lados null → `?`).
6. **Label simple:** incluye el prefijo de tipo coloreado + nombre + coste.
7. **Label/tooltip dual muestra AMBOS lados:** nombre y tipo de A y de B
   presentes; colapsa a uno solo cuando nombre Y tipo coinciden en ambos lados
   **no-null** (no asertar colapso con lados null — esos van como `?`).
8. **Tipo neutro (None):** `TypePrefix(None)` vacío ⇒ la fila no inserta token de
   color para esa carta (sin corchetes colgando).
9. **Preview de mejora disponible (single):** entry single con `CardUpgradeDef`
   configurado y `!IsUpgraded` ⇒ `BuildUpgradePreview` no vacío y refleja el
   coste/nombre/descripción mejorados (incluido `OverrideCost`).
10. **Preview de mejora dual PER-LADO:** entry dual con upgrade SOLO en `SideA`
    (SideB sin upgrade) ⇒ `CanUpgrade() == true`, `BuildRowLabel` termina en `+`,
    y el preview muestra los valores nuevos de A **y B sin cambios** — coincide
    con lo que produciría `ApplyUpgrade()`/`CreateUpgradedClone()`.
11. **Sin mejora configurada:** `HasUpgrade == false` en todos los lados ⇒
    `BuildUpgradePreview == ""` y la fila no lleva marcador `+`.
12. **Ya mejorada:** `entry.IsUpgraded == true` ⇒ marcador `★` y el tooltip dice
    `Mejorada` (no muestra el "se convertiría en…").

> Construcción de fixtures (molde `NewRunTests`/`ShopTests`/`CampfireTests`):
> `CardDefinition` vía `ScriptableObject.CreateInstance` + `SetDebugData`
> (**siempre público, sin `#if`**). La mejora se adjunta sobre la carta ya creada:
> `cardDef.Upgrade.SetTestData(...)` (`Upgrade` devuelve la instancia default
> no-null; `SetTestData` SÍ está bajo `UNITY_EDITOR || UNITY_INCLUDE_TESTS`, igual
> que `CardDeckEntry.SetSingleCard`/`SetDualCard`). El asmdef de EditMode define
> `UNITY_INCLUDE_TESTS`, así que esos guards están disponibles en los tests.

## Validación manual (RunScene + BattleScene)
1. **RunScene:** entrar a una run; en el mapa, el botón `Mazo (N)` muestra el
   conteo correcto (10 al inicio). Abrir → lista ordenada, colores por tipo,
   duales con ambos lados, scroll si hay muchas. Cerrar.
2. **Persistencia del conteo:** comprar/eliminar una carta en la Tienda, volver al
   mapa → el badge y la lista reflejan el cambio. Mejorar una carta en la Hoguera
   → la fila pasa a `★` y el tooltip dice `Mejorada`.
3. **Sobre paneles opacos:** abrir Tienda/Hoguera → el botón `Mazo (N)` sigue
   visible encima del panel; abrir el visor encima de la Tienda funciona.
4. **Preview de mejora:** una carta del starter con upgrade configurado muestra
   `+` y, en el tooltip, el bloque `Mejora ▸` con los números nuevos.
5. **BattleScene:** entrar en combate; el botón `Mazo (N)` está en el HUD; abrir
   el visor muestra el mazo completo del run; el dimmer bloquea clics sobre las
   cartas de la mano; cerrar y seguir jugando sin errores.
6. **Zero console errors** en todos los pasos.

## Decisiones cerradas
1. **Presentación = filas de texto coloreadas** (no caras con arte), porque el
   arte de cartas no es final. La clase queda **diseñada para escalar a arte**
   después (separar `BuildRowLabel` de la construcción de la fila para poder
   reemplazar la fila por una cara C7 sin tocar la lógica de orden/datos).
2. **Mostrar ambos lados (A y B) de las duales**, siempre — refuerza la dualidad
   del juego y hace el visor agnóstico al mundo (no necesita estado de combate).
3. **Preview de mejora** por carta (★ mejorada / + disponible + bloque en
   tooltip), leyendo `CardUpgradeDef` sin clonar SOs.
4. **Orden:** CardType → coste → nombre.
5. **Alcance: visible en todo momento, incluido combate.** Montado por
   `RunFlowController` (RunScene) y `CombatUIController` (BattleScene). El
   sub-Canvas overlay propio lo hace flotar también sobre Tienda/Hoguera/Resolve/
   Recompensa con un solo punto de montaje por escena.
6. **Modal overlay** (no escena nueva): respeta el molde y evita transición de
   escena. Fondo que bloquea raycasts; no pausa (combate por turnos).
7. **Scroll vertical, sin paginación.** Mazos de 10–25 cartas (GOLDEN_RULES §5)
   caben en scroll cómodo.
8. **Solo lectura.** El visor nunca muta `RunState`. (Eliminar/mejorar siguen
   siendo de Tienda/Hoguera.)
9. **Fuente de datos = `RunState.Deck`** en ambas escenas (en combate es el mazo
   maestro del run; `TurnManager` trabaja sobre sus propias pilas runtime y no lo
   toca). El visor muestra el mazo completo, no la pila de robo/descarte.

## Decisiones abiertas (REQUIEREN cierre antes de implementar)
- Ninguna. Spec cerrado.

## Alternativas consideradas
- **Caras de carta con arte (cuadrícula, "librito" StS literal)** — descartada
  por ahora: el arte de cartas no es final y subía la complejidad a baja-media.
  Se deja la puerta abierta (decisión cerrada #1).
- **Pantalla/escena dedicada** — descartada: rompe el molde `RelicInventoryView`
  y añade transición de escena para una vista de solo lectura.
- **Botón en `_mapPanel` (sin sub-Canvas)** — descartada: los paneles de
  Tienda/Hoguera son opacos y tapan el `_mapPanel`; un botón ahí no estaría
  "visible en todo momento". El sub-Canvas con `sortingOrder` alto resuelve esto
  sin tocar los controllers de cada nodo.
- **Refrescar el badge cada frame desde `Update()` del owner** (como
  `SyncRelicInventory`) — descartada: genera asignación de strings por frame. En
  su lugar, `Refresh` se llama en puntos de cambio (build, `ShowMap`, y al abrir
  el modal se relee). `[CÓDIGO ACTUAL]` el badge puede quedar momentáneamente
  desfasado mientras se está DENTRO de un panel opaco de nodo sin abrir el visor;
  es cosmético y la lista siempre se construye fresca al abrir.

## Estimación
- **Complejidad:** baja (se desvía levemente del molde por el sub-Canvas overlay
  y los 2 puntos de montaje, pero la lógica es de solo lectura y los datos ya
  existen).
- **Sub-tareas:**
  1. `DeckViewerView` (helpers static puros + UI: badge, modal, scroll, tooltip).
  2. `DeckViewerTests.cs` (12 casos).
  3. Montaje en `RunFlowController` (build + Refresh + Cleanup).
  4. Montaje en `CombatUIController` (build + Refresh).
  5. Validación en Unity (RunScene + BattleScene).
- **Follow-up FUERA de scope (no hacer en este PR):** migrar
  `ShopNodeController`/`CampfireNodeController` a los helpers `public static`
  canónicos de label/representante del módulo `Cards` (hoy son 3 copias privadas).
  Es deuda de duplicación pre-existente; consolidarla acá inflaría el scope y
  tocaría tests de Tienda/Hoguera. Anotar en `_roadmap.md` Future work.
- **Riesgo:** bajo. Único punto fino: el sub-Canvas overlay debe quedar por
  encima de los paneles opacos (verificar `overrideSorting`/`sortingOrder` en
  Unity). Sin impacto en combate ni en archivos protegidos.

## Prompt de handoff para `modo:implementacion`

```markdown
modo:implementacion

Implementá el "Visor de mazo" (pulido pre-M4). El spec cerrado está en
`Docs/dev/specs/visor_de_mazo_spec.md` — leelo completo antes de tocar código;
todas las decisiones de diseño ya están cerradas, no abras ninguna.

Setup de branch (no depende de ningún PR previo):
  git fetch --all --prune
  git checkout -b feat/deck-viewer origin/main

Qué construir (resumen; el detalle y los contratos están en el spec):
- CREAR `Assets/Scripts/Gameplay/Cards/UI/DeckViewerView.cs` — clase de
  presentación pura (molde `RelicInventoryView`): sub-Canvas overlay propio con
  badge `Mazo (N)` + modal scrolleable + tooltip. Helpers static puros
  `SortForDisplay` / `BuildRowLabel` / `BuildTooltip` / `BuildUpgradePreview`.
- CREAR `Assets/Tests/EditMode/DeckViewerTests.cs` — 12 casos (orden tipo→coste→
  nombre + estabilidad, no-mutación, null/vacío, dual con SideA null sin crash,
  label simple/dual ambos lados, tipo None, preview mejora single, preview mejora
  dual PER-LADO, sin mejora, ya-mejorada).
- MODIFICAR `Assets/Scripts/Run/RunFlowController.cs` — montar `_deckViewer` en
  `BuildUI()`, `Refresh(_state.GetDeckSnapshot())` en `BuildUI`/`ShowMap`,
  `Cleanup()` en las transiciones de escena (junto a `_relicView?.Cleanup()`).
- MODIFICAR `Assets/Scripts/Gameplay/Combat/CombatUIController.cs` — montar
  `_deckViewer` en `BuildUI()` (junto al `_relicView`) y `Refresh` con
  `RunSession.Instance?.State?.GetDeckSnapshot()`.

Reglas no negociables:
- NO tocar archivos protegidos (TurnManager, ActionQueue, PlayerCombatActor).
  El visor NO los necesita (muestra ambos lados de las duales → agnóstico al
  mundo, sin GetActiveCardDefinition).
- No manual editor setup: el visor se auto-crea en runtime, montado por los
  scene controllers existentes. Sin SOs nuevos.
- Solo lectura: el visor NUNCA muta RunState. Sin eventos de TurnManager.
- Reusar `ElementTypeColors.TypePrefix`, el criterio de label dual de
  `ShopNodeController.BuildCardSelectLabel`, `UIAnimationHelper` (FadeIn/Out),
  `RunState.GetDeckSnapshot()`. No duplicar helpers.
- El preview de mejora lee `CardUpgradeDef` directo (sin `CreateUpgradedClone`) y
  es PER-LADO en duales (mejora solo el lado con `HasUpgrade`; el otro va sin
  cambios) — debe coincidir con lo que produce `ApplyUpgrade`. `CanUpgrade()` en
  duales es CUALQUIER lado.
- `Refresh(null)` es null-safe (→ `Mazo (0)`, lista vacía). `SortForDisplay` usa
  orden ESTABLE (`OrderBy/ThenBy` o tie-break por `Id`) y tolera `SideA` null
  (fallback `SideB` / omitir; lados null → `?`).
- Label/representante: definir los helpers como `public static` en el módulo
  `Cards` (hogar canónico). NO migrar Shop/Campfire en este PR (follow-up).
- CS0104: `UnityEngine.Object` explícito donde se use `System` + `UnityEngine`.

Validación obligatoria antes de cerrar:
- Compilación limpia (zero console errors).
- `DeckViewerTests` en verde + suite EditMode completa sin regresiones
  (Unity-MCP `tests-run` o Test Runner EditMode).
- Flujo end-to-end: RunScene (mapa: badge, lista ordenada, duales, scroll;
  Tienda/Hoguera: botón visible encima del panel opaco; mejora → ★) y
  BattleScene (botón en HUD, dimmer bloquea clics de la mano, cerrar y seguir).
- Validar en Unity con Unity-MCP (abrir escena, Play, screenshot del modal).

Al cerrar: actualizá `_roadmap.md` (checkbox del visor pre-M4 + mover la flecha
"próximo paso" a M4/4a) y `_tech_snapshot.md` (nuevo `DeckViewerView` + montaje
en RunFlowController/CombatUIController). Commit + push + PR a main.
```
