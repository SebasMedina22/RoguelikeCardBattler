# Spec — C7: Cara de carta (arte de cartas) · habilitación de CÓDIGO

> **Generado en `modo:diseno`, 2026-06-05.** Es el spec de la pieza de CÓDIGO del
> slot **C7** del plan de auditoría de arte (`Docs/design/ART_NEEDS.md`). C7 es
> **mixto**: primero el código (este spec), DESPUÉS el prompt de IA.
>
> **⚠ El prompt de arte NO se genera en este spec.** Igual que el patrón de los
> slots mixtos: el prompt de "ilustración de carta" se diseña recién cuando exista
> el campo `Art` en `CardDefinition` y haya un gancho real donde asignar el PNG.
> Esto evita producir un placeholder sin lugar donde colgarlo. Ver la sección
> "Lo que NO entra en este spec" al final.

## Origen

- `Docs/design/ART_NEEDS.md` (slot **C7**) y `Docs/design/ART_PROMPTS.md` (cierre,
  ítem 2): C7 es la **dependencia crítica** que más destraba la integración de arte.
- Hoy las cartas son **botones de TEXTO** y `CardDefinition` **no tiene campo de
  sprite** → ninguna carta puede mostrar ilustración, ni en combate ni en el draft
  de NewRun (slot **N2**, bloqueado explícitamente por C7).

## Objetivo

Dar a cada carta un campo de **ilustración (sprite)** en su SO y renderizarlo en la
mano de combate (y, gratis, en la preview del draft de NewRun), con **fallback al
look de texto actual** cuando no hay arte. No cambia ninguna regla de gameplay: es
puramente presentación + un campo de datos.

## Comportamiento esperado

Desde la perspectiva del jugador:

- En combate, cada carta de la mano muestra su **ilustración** en una región
  superior del recuadro y el **texto** (nombre + `[Tipo]` tintado por C8 + coste +
  descripción) debajo.
- Si una carta **no tiene arte asignado**, se ve **exactamente como hoy**: el
  recuadro con todo el texto centrado (cero regresión visual).
- **Cartas duales:** la carta muestra el arte del **lado activo según el mundo**.
  Al cambiar de mundo (A↔B), el arte conmuta junto con el prefijo `[A]`/`[B]` y el
  texto, en vivo, sin reconstruir la mano.
- **Cartas mejoradas (upgrade):** muestran el **mismo arte** que su versión base
  (placeholder; ver decisión cerrada D3).
- En **NewRunScene**, las caras del draft muestran su arte con el mismo criterio
  (fallback a texto si no hay sprite). Esto cierra el código de **N2**.

## Sistemas afectados

- **ScriptableObjects:** `CardDefinition` gana un campo `Sprite art`.
- **Combate / UI:** `CardHandView` renderiza el arte del lado activo + fallback.
- **NewRun / UI:** `NewRunController` muestra el arte en los botones del draft.
- **Combate (datos):** `DualCardDefinition` y `CardDeckEntry` — **sin cambios**: cada
  lado de una dual ya ES un `CardDefinition`, así que heredan el campo gratis.
- **Combate (protegidos):** `TurnManager` / `ActionQueue` / `PlayerCombatActor` —
  **NO se tocan** (ver más abajo).

## Archivos a crear

- `Assets/Tests/EditMode/CardArtTests.cs` — propósito: validar el contrato de datos
  del campo `Art` (default, herencia dual por mundo, persistencia en el clon de
  upgrade). Lógica pura de SO, sin UI.

## Archivos a modificar

- `Assets/Scripts/Gameplay/Cards/CardDefinition.cs` — agrega el campo `art`, su
  getter `Art`, lo incorpora a `SetDebugData(...)` (param opcional al final) y lo
  propaga en `CreateUpgradedClone()`.
- `Assets/Scripts/Gameplay/Combat/CardHandView.cs` — render del arte en la región
  superior de la carta + sincronización en vivo del sprite del lado activo +
  fallback a texto full-card.
- `Assets/Scripts/Run/NewRun/NewRunController.cs` — render del arte en los botones
  del draft (`BuildDraftColumn`), con el mismo fallback.

## Archivos protegidos involucrados

- [x] **Ninguno.** `TurnManager.GetActiveCardDefinition(entry)` **ya existe**
  ([TurnManager.cs:793](../../../Assets/Scripts/Gameplay/Combat/TurnManager.cs#L793))
  y resuelve el lado activo por `CurrentWorld`. `CardHandView` ya lo usa por-frame
  para el label, así que el arte de cartas duales conmuta por el mismo camino sin
  tocar nada protegido.

---

## Contratos

### Datos — `CardDefinition`

`[CÓDIGO ACTUAL]` Hoy `CardDefinition` tiene id/nombre/coste/tipo/efectos/upgrade,
**sin sprite** ([CardDefinition.cs:13-23](../../../Assets/Scripts/Gameplay/Cards/CardDefinition.cs#L13-L23)).

`[PROPUESTA]` Campo nuevo, nombrado en paralelo a `EnemyDefinition.avatar`/`Avatar`
([EnemyDefinition.cs:23,35](../../../Assets/Scripts/Gameplay/Enemies/EnemyDefinition.cs#L23)):

```csharp
[SerializeField] private Sprite art = null;   // ilustración de la carta (C7). null = fallback a texto.
public Sprite Art => art;
```

`SetDebugData(...)` suma un parámetro **opcional al final** (no rompe callers
existentes; mismo criterio que `elementType` ya añadido así):

```csharp
public void SetDebugData(
    string newId, string newName, string newDescription, int newCost,
    CardType newType, CardRarity newRarity, CardTarget newTarget,
    List<string> newTags, List<EffectRef> newEffects,
    ElementType newElementType = ElementType.None,
    Sprite newArt = null)        // ← nuevo, al final
{
    ...
    art = newArt;
}
```

`CreateUpgradedClone()` propaga el arte base (decisión D3: mismo arte en el clon):

```csharp
clone.SetDebugData(
    id + "_upgraded", newName, newDescription, newCost,
    type, rarity, target, new List<string>(tags), newEffects,
    elementType,
    art);                        // ← el clon de upgrade conserva la ilustración base
```

> **Contrato crítico:** sin la última línea, toda carta mejorada perdería su arte
> (porque `CreateUpgradedClone` reconstruye vía `SetDebugData`). El caso de prueba
> 3 lo cubre.

### Datos — `DualCardDefinition`, `CardDeckEntry`

`[CÓDIGO ACTUAL]` `DualCardDefinition.GetSide(worldSide)` devuelve la `CardDefinition`
del lado ([DualCardDefinition.cs:22](../../../Assets/Scripts/Gameplay/Cards/DualCardDefinition.cs#L22)),
y `CardDeckEntry.GetActiveCard(worldSide)` la resuelve
([CardDeckEntry.cs:67](../../../Assets/Scripts/Gameplay/Cards/CardDeckEntry.cs#L67)).
**Sin cambios:** como cada lado es un `CardDefinition`, `sideA.Art` y `sideB.Art`
existen automáticamente y se pueden asignar por separado en el inspector. El draft
compuesto de NewRun (`StarterDraft.ComposeDualCard`) también hereda el arte de cada
cara elegida sin tocar nada.

### Render — `CardHandView`

`[CÓDIGO ACTUAL]` `CreateCardButton` arma un `Button` con un único `Text` que ocupa
toda la carta ([CardHandView.cs:225-252](../../../Assets/Scripts/Gameplay/Combat/CardHandView.cs#L225-L252)).
`SyncHandButtons` refresca por-frame el `Label.text` (incluye prefijo `[A]/[B]` y el
`[Tipo]` tintado de C8) y la interactividad, **sin reconstruir** salvo que cambie la
composición de la mano ([CardHandView.cs:93-120](../../../Assets/Scripts/Gameplay/Combat/CardHandView.cs#L93-L120)).

`[PROPUESTA]` Patrón de render (espeja `CombatUIController.CreateAvatar`
[CombatUIController.cs:579-596](../../../Assets/Scripts/Gameplay/Combat/CombatUIController.cs#L579-L596):
`Image` hijo, `preserveAspect = true`, `raycastTarget = false`):

1. **`CardButtonBinding`** gana dos miembros:
   - `Image Art;` — el `Image` hijo de la ilustración (creado siempre, una vez).
   - `bool ArtShown;` — última visibilidad aplicada (guard anti-thrash de layout).

2. **`CreateCardButton`** crea SIEMPRE un `Image` hijo "Art" como primer hijo del
   recuadro, **anclado a la región superior** (p. ej. `anchorMin (0.06, 0.42)` /
   `anchorMax (0.94, 0.96)`), con `preserveAspect = true`, `raycastTarget = false`,
   `color = Color.white`. El sprite se setea en el sync (paso 4). El `Label` de texto
   se crea como hoy (anclado full-card) — su región la ajusta el sync.

3. **Helper `ApplyCardArtLayout(binding, bool hasArt)`** (privado, idempotente):
   - `hasArt == true`: `binding.Art.enabled = true`; el `Label` se reancla a la
     **mitad inferior** (`anchorMin (0,0)` / `anchorMax (1, 0.40)`), alineación
     `UpperCenter`, fontSize algo menor (ver D6).
   - `hasArt == false`: `binding.Art.enabled = false`; el `Label` vuelve a **full-card**
     (`anchorMin (0,0)` / `anchorMax (1,1)`), alineación `MiddleCenter` (look actual).
   - Solo se ejecuta cuando `hasArt != binding.ArtShown` (evita re-anclar cada frame);
     actualiza `binding.ArtShown` al final.

4. **`SyncHandButtons`** — dentro del foreach existente, por cada binding:
   - `CardDefinition activeCard = _turnManager.GetActiveCardDefinition(binding.CardEntry);`
   - `Sprite art = activeCard != null ? activeCard.Art : null;`
   - `binding.Art.sprite = art;`
   - `ApplyCardArtLayout(binding, art != null);`
   - El resto (label text, color, interactabilidad, tinte de fondo) queda igual.

   > Esto hace que una **carta dual** cuyo lado A tiene arte y lado B no (o sprites
   > distintos) **conmute en vivo** al cambiar de mundo, por el mismo mecanismo que
   > ya conmuta el label — sin forzar rebuild.

5. **`RebuildHandButtons`** guarda la referencia `Art` (y deja `ArtShown = false`
   para que el primer sync aplique el layout correcto) en el `CardButtonBinding`.

`[INTERPRETACIÓN]` Convivencia con **C8**: el tinte por tipo vive en el **texto** del
label (prefijo `[Tipo]` con `<color>` vía `ElementTypeColors.ReadableOnDark`) y en el
**color de fondo** del recuadro. El arte es un `Image` hijo encima del fondo y debajo
del texto. **No se pisan**: C8 sigue tal cual; el arte solo ocupa la región superior.

### Render — `NewRunController` (cierra N2)

`[CÓDIGO ACTUAL]` `BuildDraftColumn` crea un botón por cara con
`CreateButtonAnchored(...)` y `BuildFaceLabel(face)` (texto)
([NewRunController.cs:317-350](../../../Assets/Scripts/Run/NewRun/NewRunController.cs#L317-L350)).

`[PROPUESTA]` Tras crear el botón de cada cara, si `face.Art != null` agregar un
`Image` hijo en la región superior del botón (mismo criterio: `preserveAspect`,
`raycastTarget = false`) y dejar el `Label` en la franja inferior; si es `null`,
queda el texto como hoy. Como hoy el campo estará vacío hasta que exista el arte, el
efecto visual inmediato es nulo (fallback) — pero el gancho queda cableado y N2 deja
de estar bloqueado. **Decisión D4: se incluye en este PR** (es trivial y es el punto
de "destrabar N2").

> `[INTERPRETACIÓN]` No se extrae un helper compartido entre `CardHandView` y
> `NewRunController` en este PR: viven en asmdef/escenas distintas y construyen UI con
> patrones locales propios (ya hoy cada uno tiene su `CreateText`). Duplicar ~6
> líneas de "Image hijo con preserveAspect" es más barato y menos acoplado que
> introducir una utilidad compartida ahora. Ver [ALTERNATIVA] A2.

### Eventos

Ninguno nuevo. No se agregan eventos a `TurnManager` ni subsistemas.

---

## Reuso

- **Patrón de `Image` con `preserveAspect`** ya probado en `CombatUIController.CreateAvatar`
  (avatares de enemigo, slot drop-in validado en #102/#103). El arte de carta lo
  reaplica.
- **`ElementTypeColors`** (C8) — sin cambios; el arte convive con su tinte.
- **`GetActiveCardDefinition`** — ya resuelve el lado activo por mundo; reusar tal cual.
- **`CardButtonBinding`** — estructura existente; se le suman 2 miembros.

---

## Casos de prueba (EditMode) — `CardArtTests.cs`

Todos sobre el contrato de datos (sin UI):

1. **Default null:** un `CardDefinition` recién creado tiene `Art == null` (garantiza
   el fallback de texto sin asignación).
2. **`SetDebugData` setea el arte:** tras `SetDebugData(..., newArt: sprite)`,
   `Art == sprite`; y llamado sin el parámetro, `Art == null` (compatibilidad de
   callers viejos).
3. **El clon de upgrade conserva el arte:** `CardDefinition` con `art = sprite` y
   upgrade definido → `CreateUpgradedClone().Art == sprite` (cubre el contrato
   crítico de `CreateUpgradedClone`).
4. **Herencia dual por mundo:** `DualCardDefinition` con `sideA.Art = spriteA` y
   `sideB.Art = spriteB` → `GetSide(WorldSide.A).Art == spriteA` y
   `GetSide(WorldSide.B).Art == spriteB`. (Reusa `SetDebugData` para poblar los lados;
   crea sprites dummy vía `Sprite.Create(Texture2D.whiteTexture, ...)`).
5. **`CardDeckEntry` resuelve el arte activo:** `CardDeckEntry.CreateDual(dual)` →
   `GetActiveCard(WorldSide.A).Art == spriteA`, idem B.

> El render en `CardHandView`/`NewRunController` **no es unit-testeable** sin escena;
> se valida a mano (abajo).

## Validación manual (BattleScene + NewRunScene)

1. **Compilación limpia** (zero console errors) tras agregar el campo.
2. **Fallback (sin arte):** abrir BattleScene y jugar — las cartas se ven **igual que
   hoy** (texto centrado, tinte `[Tipo]` intacto, layout adaptivo igual).
3. **Con arte placeholder de prueba:** asignar un sprite cualquiera al campo `Art` de
   un `CardDefinition` del starter deck (vía inspector o un sprite temporal) → la
   carta muestra la imagen arriba y el texto abajo, sin deformar (preserveAspect).
4. **Carta dual + cambio de mundo:** con una dual cuyos lados A/B tengan sprites
   distintos, cambiar de mundo en combate → el arte conmuta junto con el `[A]/[B]` y
   el texto, **sin reconstruir la mano** (no hay flicker de fade-in de toda la mano).
5. **Upgrade (Hoguera):** mejorar una carta con arte → la carta mejorada conserva la
   ilustración.
6. **Draft NewRun:** abrir NewRunScene; con caras sin arte se ve como hoy; con una
   cara con sprite asignado, el botón del draft muestra la imagen.

---

## Decisiones cerradas

- **D1 — El campo vive en `CardDefinition`** (no en `DualCardDefinition` ni en
  `CardDeckEntry`). Razón: cada lado de una dual y cada cara de draft de NewRun **ya
  son** `CardDefinition`, así que heredan el arte gratis y se asignan por separado.
  Confirmado contra el código.
- **D2 — Nombre del campo: `art` / `Art`** (paralelo a `EnemyDefinition.avatar`/`Avatar`).
- **D3 — Upgrade usa el MISMO arte placeholder** que la carta base. No se agrega
  override de arte a `CardUpgradeDef`. El clon de upgrade propaga `art`.
- **D4 — N2 (draft de NewRun) entra en el MISMO PR.** Es trivial (el campo ya existe)
  y es el objetivo declarado de "destrabar N2". Sin arte asignado el efecto es nulo
  (fallback), pero el gancho queda cableado.
- **D5 — Cartas duales muestran el arte del LADO ACTIVO** vía
  `GetActiveCardDefinition`, refrescado en `SyncHandButtons` (mismo camino que el
  label). No se fuerza rebuild en world-switch.
- **D6 — Layout:** el arte ocupa la **región superior** (~`0.42`–`0.96` vertical) y el
  texto la **franja inferior** (~`0.0`–`0.40`) cuando hay arte; **full-card** cuando no.
  Se mantienen las constantes de tamaño/escala adaptiva existentes
  (`HandCardWidthBase` etc.). El fontSize del label baja levemente solo en modo
  "con arte" para que entre en la franja inferior (ajuste a ojo en implementación;
  arrancar en ~16). `preserveAspect` absorbe que el arte fuente sea vertical (512×768)
  dentro de una región más ancha (se letterboxea; aceptable para placeholder).
- **D7 — Sin per-card scale/offset de arte en v1** (a diferencia de
  `EnemyDefinition.avatarScale/Offset`). El placeholder se ve bien con `preserveAspect`
  centrado; agregar scale/offset es deuda menor si el arte final lo pide.
- **D8 — Formato y carpeta del PNG (convención, para cuando llegue el arte):**
  - Ruta: `Assets/Art/Sprites/Cards/carta_<id>.png` (espeja la convención de enemigos
    `Assets/Art/Sprites/Enemies/<Enemy>/A/Idle/personaje_*.png`). `<id>` = el `Id` de
    la `CardDefinition` (cada lado de una dual es un SO con su propio id → su propio
    PNG).
  - Tamaño: **512×768 px, vertical (2:3)**, PNG con alpha (la ilustración pensada para
    ventana vertical dentro del marco, según ART_NEEDS).
  - Import: **Sprite (2D and UI / Single)**.
  - **La carpeta `Assets/Art/Sprites/Cards/` se crea cuando se genere el arte**, no en
    este PR de código (el PR de código no necesita PNGs).

## Decisiones abiertas (REQUIEREN cierre antes de implementar)

- **Ninguna.** El spec está cerrado.

## Alternativas consideradas

- **A1 — Campo en `DualCardDefinition` (un arte por carta dual):** descartada. Rompe
  la simetría con las cartas simples y con las caras de draft (que son `CardDefinition`),
  y no permitiría arte distinto por lado (el GDD trata cada mundo como estética
  propia). Ponerlo en `CardDefinition` cubre los tres casos con un solo campo.
- **A2 — Helper de render compartido `CardArtView` entre combate y NewRun:**
  descartada para v1. Acopla dos escenas/flows con patrones de UI locales propios por
  ~6 líneas duplicadas. Si más vistas necesitan render de carta (p. ej. selectores de
  Hoguera/Tienda del backlog #104), reconsiderar extraer entonces.
- **A3 — Forzar rebuild de la mano en world-switch para refrescar el arte:**
  descartada. El sync por-frame ya refresca label y tinte; refrescar el sprite ahí es
  más barato y evita el flicker del fade-in escalonado de toda la mano.
- **A4 — Override de arte en `CardUpgradeDef` (arte distinto para la mejorada):**
  descartada para v1 (placeholder). Es contenido/arte, no código; se puede sumar
  después sin migración (campo opcional).

## Estimación

- **Complejidad:** media. El campo de datos + tests es chico; el render con conmutación
  en vivo y el guard de layout es la parte con más cuidado.
- **Sub-tareas:**
  1. Campo `art` + getter + `SetDebugData` + propagación en `CreateUpgradedClone`.
  2. `CardArtTests.cs` (5 casos).
  3. Render + sync + `ApplyCardArtLayout` en `CardHandView`.
  4. Render de arte en `BuildDraftColumn` de `NewRunController` (N2).
  5. Validación manual + actualización de `ART_NEEDS.md` (C7 → ✅ Drop-in) y docs.
- **Riesgo:** bajo. No toca gameplay ni archivos protegidos. El único punto fino es
  el guard anti-thrash del re-anclado de texto (caso 3-4 de validación).

---

## Lo que NO entra en este spec (siguiente fase)

- **El prompt de IA de "ilustración de carta"** se diseña en una sesión posterior
  (`modo:diseno`, Fase 3 del plan de arte), **recién con el campo `Art` ya en main**.
  Recién ahí se decide el sujeto por carta, la variante de mundo por lado de las
  duales, y se anexan las restricciones técnicas (512×768, alpha, vertical) al estilo
  madre de `ART_NEEDS.md`. Igual que se hizo con los slots drop-in en `ART_PROMPTS.md`.
- **El arte final.** Todo lo de C7 es placeholder-IA cuando llegue.
- **Backlog #104** (legibilidad de tipo/color en selectores Hoguera/Tienda) es una
  tarea hermana pero independiente; no se mezcla acá.

---

## Prompt de handoff para `modo:implementacion`

```text
modo:implementacion

Implementá C7 — cara de carta (arte de cartas), pieza de CÓDIGO. El spec cerrado
está en `Docs/dev/specs/art_c7_card_art_spec.md` — leelo completo antes de tocar
código; todas las decisiones de diseño ya están cerradas (D1–D8), no abras ninguna.

Setup de branch (no hay PR previo del que dependa; main al día, 0 PRs abiertos):
  git fetch --all --prune
  git checkout -b feat/c7-card-art origin/main

Qué construir (el detalle y los contratos están en el spec):
- Modificar `Assets/Scripts/Gameplay/Cards/CardDefinition.cs`: campo `Sprite art` +
  getter `Art`, param opcional `newArt` al final de `SetDebugData`, y propagarlo en
  `CreateUpgradedClone()` (contrato crítico: sin esto la carta mejorada pierde el arte).
- Modificar `Assets/Scripts/Gameplay/Combat/CardHandView.cs`: `Image Art` + `bool
  ArtShown` en `CardButtonBinding`; crear el Image hijo en la región superior en
  `CreateCardButton`; helper idempotente `ApplyCardArtLayout`; en `SyncHandButtons`
  setear `binding.Art.sprite = GetActiveCardDefinition(entry)?.Art` y aplicar layout
  (con guard anti-thrash). Fallback sin arte = texto full-card (look actual). Conmuta
  en vivo el arte del lado activo de las duales (mismo camino que el label).
- Modificar `Assets/Scripts/Run/NewRun/NewRunController.cs`: en `BuildDraftColumn`,
  si `face.Art != null` renderizar Image en la región superior del botón (cierra N2).
- Crear `Assets/Tests/EditMode/CardArtTests.cs`: 5 casos (default null; SetDebugData
  setea/omite; el clon de upgrade conserva el arte; herencia dual por mundo; CardDeckEntry
  resuelve el arte activo). Sprites dummy vía Sprite.Create(Texture2D.whiteTexture,...).

Reglas no negociables:
- NO tocar archivos protegidos (TurnManager, ActionQueue, PlayerCombatActor). No hace
  falta: `GetActiveCardDefinition` ya existe y resuelve el lado activo por mundo.
- NO engordar `CombatUIController` — el render de carta va en `CardHandView` (vista
  extraída), según `Combat/CLAUDE.md`.
- No manual editor setup: el campo es opcional y `null` → fallback automático. No se
  requiere asignar sprites para que el juego corra.
- C8 (tinte por tipo / `ElementTypeColors`) NO se toca: el arte convive con él.
- NO generar el prompt de arte ni crear la carpeta `Assets/Art/Sprites/Cards/` —
  eso es la fase siguiente, con el campo ya en main.
- Revisar el diff de `.claude/settings.json` y `*.slnx` antes de commitear (se
  auto-modifican) — de-scopearlos con `git checkout --` si aparecen.

Validación obligatoria antes de cerrar:
- Compilación limpia (zero console errors).
- `CardArtTests` (5) en verde + suite EditMode completa sin regresiones
  (`tests-run` por Unity-MCP, o Test Runner > EditMode > Run All).
- BattleScene: cartas sin arte se ven igual que hoy (fallback); con un sprite de
  prueba asignado a una carta, se ve arte arriba + texto abajo; dual A/B con sprites
  distintos conmuta en vivo al cambiar de mundo sin reconstruir la mano; carta
  mejorada conserva el arte.
- NewRunScene: draft sin arte = look actual; con sprite asignado, el botón muestra
  la imagen.
- Nota: el game view no repinta vía CLI sin foco → validar layout por código/lógica y
  el render a ojo en el editor con foco.

Al cerrar: actualizá `Docs/design/ART_NEEDS.md` (slot C7 → columna "¿Código lo
soporta?" a ✅ Drop-in; N2 → desbloqueado), `_roadmap.md` (marcar C7 hecho en el
bloque de arte pre-M4) y `_tech_snapshot.md` (campo `Art` en CardDefinition + nuevo
test). Commit + push + PR a main.
```
