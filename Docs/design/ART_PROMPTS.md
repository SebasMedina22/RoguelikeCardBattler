# ART_PROMPTS.md — Prompts de IA para placeholders (Fase 3)

> **Qué es este archivo:** prompts paste-ready (en inglés) para generar los
> placeholders de arte con una IA generadora de imágenes. Doc dedicado y separado de
> [ART_NEEDS.md](ART_NEEDS.md) porque estos prompts se copian/pegan repetido — el
> catálogo de slots y el estilo madre viven allá; acá vive el texto final listo para
> pegar.
>
> **Generado:** Fase 3 del plan de auditoría de arte, `modo:diseno` (2026-06-05).
> **Alcance acumulado de este doc:**
> - **Lote drop-in** (cero código): C1, C2, C3, C6 y C4 (con deuda anotada).
> - **C7 — Caras de carta** (24 PNGs): prompteado tras mergear el campo
>   `CardDefinition.Art` (PR #105). Ver la sección `## C7 — Caras de carta`. Esto
>   también cubre **N2** (caras del draft de NewRun), que es parte de las 24.
>
> Slots aún SIN promptear (necesitan código antes): C5 (héroe Mundo B), C8 (solo
> código, sin IA), M1, M2, S1, N1, H1, y la variante B del boss (UNIT-RB7).
>
> **No es arte final.** Todo lo generado acá es placeholder-IA: stand-in para vestir
> las pantallas hasta que llegue el arte definitivo.

---

## Cómo usar este doc

1. Copiá el bloque ` ``` ` completo de la sección del slot que vas a generar.
2. Pegalo tal cual en la IA generadora. Cada bloque ya trae: estilo madre + variante
   de mundo correcta + sujeto + restricciones técnicas (tamaño, transparencia).
3. Generá; elegí la mejor variante; exportá PNG con alpha.
4. Guardá el archivo con el **nombre y la ruta exactos** que indica el campo
   **`📁 Guardar el PNG como:`** de cada slot. **Las carpetas destino ya están
   creadas en el proyecto** (`Assets/Art/Sprites/Enemies/<Enemigo>/A/Idle/` y
   `.../Common/Hit/`) → solo tenés que copiar el PNG ahí.
5. En Unity: refrescá (el asset se importa), confirmá import como **Sprite (2D and UI
   / Single)**, y asigná según la **nota de asignación (Fase 4)** de cada slot.

**Contexto técnico común (verificado contra código):**
- `[INTERPRETACIÓN]` El avatar de enemigo se renderiza con `preserveAspect = true`
  dentro de un box cuadrado del HUD ([CombatUIController.cs:563-595](../../Assets/Scripts/Gameplay/Combat/CombatUIController.cs#L563-L595)).
  Por eso el formato correcto es **cuadrado 1:1** con el sujeto centrado y aire
  alrededor — un PNG cuadrado encaja sin deformarse. El Slime de referencia
  (`Enemies/Slime/A/Idle/personaje_slime.png`, escala 0.85) confirma que esto
  funciona.
- `[GDD]` (línea 57): Mundo A = **Medieval Oscuro**, Mundo B = **Cyberpunk/Futurista**.
  Goblin, SkeletonWarrior y DarkMage son sujetos de fantasía medieval y su único
  sprite vive en la convención `.../A/Idle/` igual que el Slime → **todos son Mundo A
  (Medieval Oscuro)** en su placeholder actual.
- `[INTERPRETACIÓN]` Los 4 enemigos tienen `elementType: 3` (= **Azul**, ver
  [ElementTypes.cs](../../Assets/Scripts/Gameplay/Combat/ElementTypes.cs)). Es un
  acento opcional: un detalle azul/cian de crayón puede tejerse en cada criatura sin
  romper la paleta medieval. No es obligatorio para el placeholder.

---

## C1 — Avatar enemigo: Goblin (Mundo A)

**Prompt paste-ready:**

```
STYLE: Childlike handmade arts-and-crafts aesthetic — as if drawn and built by two
kids from cardboard, wax crayons, cut-out paper, magazine clippings, school glue and
sticky tape. Visible crayon strokes, uneven hand-cut paper edges, cardboard texture,
slightly wonky proportions, warm imperfect lines. Flat 2D, storybook/diorama feel.
Everything looks like a physical scrap pinned into a kids' imaginary world.
NOT digital-clean, NOT vector-smooth, NOT photorealistic, NO gradients-heavy 3D.

WORLD A — DARK MEDIEVAL: gloomy fairy-tale medieval theme rendered in the same
crayon-and-cardboard craft style. Muted earthy palette — deep browns, charcoal,
dried-blood reds, candle-amber highlights. Castles, torn banners, rusty armor,
stitched cloth monsters, torchlight. Spooky but still handmade and toy-like, never
gory-realistic.

SUBJECT: a small mischievous GOBLIN — scrawny green creature with big pointy ears, a
crooked toothy grin, holding a tiny rusty dagger or jagged knife. Hunched, wiry,
cheeky-menacing but toy-like. A weak, common dungeon minion. Optional subtle
blue/cyan crayon accent woven into the goblin (it is an "Azul"-type enemy) without
overriding the medieval palette.

TECHNICAL: single character, full body head-to-toe, centered with generous empty
margin all around. Facing slightly LEFT (it faces the hero, who stands on the left of
the screen). TRANSPARENT background (PNG with alpha, no scene, no ground). Square 1:1
composition, target 512x512 px. Clean silhouette readable at small size.
```

**📁 Guardar el PNG como:** `Assets/Art/Sprites/Enemies/Goblin/A/Idle/personaje_goblin.png`
— la carpeta **ya está creada** en el proyecto.

**Nota de asignación (Fase 4):** importar como Sprite (2D and UI / Single, alpha
transparente). Asignar al campo
`avatar` de `Assets/ScriptableObjects/Enemies/Goblin.asset`
(`EnemyDefinition.Avatar`). Hoy `avatar: {fileID: 0}` (vacío). `avatarScale` actual =
1, `avatarOffset` = (0,0) — ajustar a ojo tras asignar si hace falta.

---

## C2 — Avatar enemigo: SkeletonWarrior (Mundo A)

**Prompt paste-ready:**

```
STYLE: Childlike handmade arts-and-crafts aesthetic — as if drawn and built by two
kids from cardboard, wax crayons, cut-out paper, magazine clippings, school glue and
sticky tape. Visible crayon strokes, uneven hand-cut paper edges, cardboard texture,
slightly wonky proportions, warm imperfect lines. Flat 2D, storybook/diorama feel.
Everything looks like a physical scrap pinned into a kids' imaginary world.
NOT digital-clean, NOT vector-smooth, NOT photorealistic, NO gradients-heavy 3D.

WORLD A — DARK MEDIEVAL: gloomy fairy-tale medieval theme rendered in the same
crayon-and-cardboard craft style. Muted earthy palette — deep browns, charcoal,
dried-blood reds, candle-amber highlights. Castles, torn banners, rusty armor,
stitched cloth monsters, torchlight. Spooky but still handmade and toy-like, never
gory-realistic.

SUBJECT: a SKELETON WARRIOR — a standing skeleton soldier made of cut-out white/bone
paper, wearing a dented rusty helmet and holding a chipped sword and a small battered
shield. Bones drawn as crayon-outlined paper strips, hollow eye sockets, a grim but
goofy toy-soldier vibe. Optional subtle blue/cyan crayon accent (it is an "Azul"-type
enemy) without overriding the medieval palette.

TECHNICAL: single character, full body head-to-toe, centered with generous empty
margin all around. Facing slightly LEFT (it faces the hero, who stands on the left of
the screen). TRANSPARENT background (PNG with alpha, no scene, no ground). Square 1:1
composition, target 512x512 px. Clean silhouette readable at small size.
```

**📁 Guardar el PNG como:** `Assets/Art/Sprites/Enemies/SkeletonWarrior/A/Idle/personaje_skeletonwarrior.png`
— la carpeta **ya está creada** en el proyecto.

**Nota de asignación (Fase 4):** importar como Sprite. Asignar al campo `avatar` de
`Assets/ScriptableObjects/Enemies/SkeletonWarrior.asset`. Hoy vacío.

---

## C3 — Avatar enemigo: DarkMage (Mundo A)

**Prompt paste-ready:**

```
STYLE: Childlike handmade arts-and-crafts aesthetic — as if drawn and built by two
kids from cardboard, wax crayons, cut-out paper, magazine clippings, school glue and
sticky tape. Visible crayon strokes, uneven hand-cut paper edges, cardboard texture,
slightly wonky proportions, warm imperfect lines. Flat 2D, storybook/diorama feel.
Everything looks like a physical scrap pinned into a kids' imaginary world.
NOT digital-clean, NOT vector-smooth, NOT photorealistic, NO gradients-heavy 3D.

WORLD A — DARK MEDIEVAL: gloomy fairy-tale medieval theme rendered in the same
crayon-and-cardboard craft style. Muted earthy palette — deep browns, charcoal,
dried-blood reds, candle-amber highlights. Castles, torn banners, rusty armor,
stitched cloth monsters, torchlight. Spooky but still handmade and toy-like, never
gory-realistic.

SUBJECT: a DARK MAGE — a hooded robed sorcerer cut from dark purple and charcoal
paper, face hidden in shadow under the hood, raising one hand that conjures a small
crayon-scribbled magic spark. Long tattered robe, crooked wooden staff optional.
Sinister but storybook, toy-like. The conjured magic spark should read as a
blue/cyan crayon glow (it is an "Azul"-type enemy) — that accent is welcome here.

TECHNICAL: single character, full body head-to-toe, centered with generous empty
margin all around. Facing slightly LEFT (it faces the hero, who stands on the left of
the screen). TRANSPARENT background (PNG with alpha, no scene, no ground). Square 1:1
composition, target 512x512 px. Clean silhouette readable at small size.
```

**📁 Guardar el PNG como:** `Assets/Art/Sprites/Enemies/DarkMage/A/Idle/personaje_darkmage.png`
— la carpeta **ya está creada** en el proyecto.

**Nota de asignación (Fase 4):** importar como Sprite. Asignar al campo `avatar` de
`Assets/ScriptableObjects/Enemies/DarkMage.asset`. Hoy vacío.

---

## C4 — Avatar boss: BossAct1 (Costura maldita — Mundo A) · ⚠ con deuda conocida

`[GDD]` (línea 57) — el boss es **dual-mundo**:
- **Mundo A (Medieval): "Costura maldita"** — peluche de paja en forma de conejo,
  poseído por una maldición; de su pecho sale una mano oscura con la que ataca
  (inspiración Mimikyu).
- **Mundo B (Futurista): "UNIT-RB7"** — robot de juguete infectado por un virus de
  una IA mutada.

`[INTERPRETACIÓN]` `EnemyDefinition.Avatar` es **un solo `Sprite`** — no hay forma de
guardar A + B distintos sin código. **Decisión ya tomada (no es decisión abierta):**
generamos **una** versión placeholder ahora — la de **Mundo A (Costura maldita)**,
para mantener coherencia con el roster del Acto 1 (todos Mundo A) — y dejamos la
variante B (UNIT-RB7) como **deuda de código** (ver abajo).

**Prompt paste-ready (Mundo A — Costura maldita):**

```
STYLE: Childlike handmade arts-and-crafts aesthetic — as if drawn and built by two
kids from cardboard, wax crayons, cut-out paper, magazine clippings, school glue and
sticky tape. Visible crayon strokes, uneven hand-cut paper edges, cardboard texture,
slightly wonky proportions, warm imperfect lines. Flat 2D, storybook/diorama feel.
Everything looks like a physical scrap pinned into a kids' imaginary world.
NOT digital-clean, NOT vector-smooth, NOT photorealistic, NO gradients-heavy 3D.

WORLD A — DARK MEDIEVAL: gloomy fairy-tale medieval theme rendered in the same
crayon-and-cardboard craft style. Muted earthy palette — deep browns, charcoal,
dried-blood reds, candle-amber highlights. Castles, torn banners, rusty armor,
stitched cloth monsters, torchlight. Spooky but still handmade and toy-like, never
gory-realistic.

SUBJECT — ACT 1 BOSS "CURSED STITCH" ("Costura maldita"): a cursed handmade
straw-stuffed RABBIT PLUSHIE, sewn from patchwork burlap and frayed cloth with big
uneven stitches and loose straw poking out. It looks like a child's toy gone wrong —
button eyes, a crooked stitched mouth. From a torn seam in its CHEST emerges a
shadowy dark spectral HAND reaching out to attack (Mimikyu-like menace). Imposing,
boss-scale, eerie but still toy/craft-made, never gory-realistic. Subtle blue/cyan
crayon accent allowed (it can roll an "Azul" type) without overriding the medieval
palette.

TECHNICAL: single character, full body head-to-toe, centered with generous empty
margin all around. Facing slightly LEFT (it faces the hero, who stands on the left of
the screen). TRANSPARENT background (PNG with alpha, no scene, no ground). Square 1:1
composition, target 768x768 px (larger than regular enemies — it is a boss). Clean
silhouette readable at small size.
```

**📁 Guardar el PNG como:** `Assets/Art/Sprites/Enemies/BossAct1/A/Idle/personaje_bossact1.png`
— la carpeta **ya está creada** en el proyecto.

**Nota de asignación (Fase 4):** importar como Sprite. Asignar al campo `avatar` de
`Assets/ScriptableObjects/Enemies/BossAct1.asset`. Hoy vacío. `avatarScale` actual =
1.2, `avatarOffset` = (0,-30).

**🔧 Deuda de código registrada (NO es decisión abierta):** para que el boss muestre
**A (Costura maldita) y B (UNIT-RB7)** distintos según el mundo activo, hace falta un
PR de código: agregar un segundo campo de avatar (p. ej. `avatarWorldB`) a
`EnemyDefinition` y conmutarlo en el world-switch (análogo a C5 héroe Mundo B). El
prompt del UNIT-RB7 (Mundo B / Cyberpunk) se genera **recién después** de ese campo,
para no producir un PNG sin gancho donde asignarlo. Por ahora: solo el placeholder A.

---

## C6 — Frames de impacto del ENEMIGO (flash de golpe) · genérico, compartido

⚠ **Corrección de slot (verificado en código):** el pedido original decía "hit-frames
del héroe", pero el único slot de hit vacío es `CombatUIController.enemyHitFrames`
([CombatUIController.cs:730](../../Assets/Scripts/Gameplay/Combat/CombatUIController.cs#L730)):
son los frames de impacto del **enemigo**. El héroe ya tiene sus frames
(`heroIdleFrames`/`heroAttackFrames`, Mundo A asignado) y **no existe** un campo
`heroHitFrames`. → Este slot se genera para el enemigo.

`[INTERPRETACIÓN]` `enemyHitFrames` es una **lista única en el componente**, NO por
`EnemyDefinition`. El animador la reproduce sobre el sprite del avatar enemigo al
recibir un golpe. Como es compartida entre TODOS los enemigos, **debe ser
agnóstica**: no puede ser una pose de retroceso de un enemigo concreto (reemplazaría
al Goblin/Skeleton/etc.). Por eso se diseña como un **efecto de impacto que se
superpone**: un destello/estrellido de crayón tipo cómic, transparente, mismo
encuadre cuadrado y mismo pivote/escala en todos los frames — solo cambia la fase del
estallido. Estética handmade, sin variante de mundo (es un efecto, no un personaje).

**Prompt paste-ready (secuencia de 4 frames):**

```
STYLE: Childlike handmade arts-and-crafts aesthetic — wax crayons, cut-out paper,
cardboard texture, visible crayon strokes, uneven hand-cut edges, warm imperfect
lines. Flat 2D, storybook feel. NOT digital-clean, NOT vector-smooth, NOT
photorealistic.

SUBJECT: a generic comic-book "HIT" / impact effect — a hand-drawn crayon IMPACT
BURST: a spiky star-burst / "POW" splash of jagged crayon lines and torn-paper shards
radiating from the center, in bright white with red and yellow crayon edges. Reads as
the moment a character gets struck. No creature, no letters/text, just the impact
flash.

SEQUENCE: 4 separate frames of the SAME burst animating: frame 1 = small bright core
sparking; frame 2 = burst fully expanded and brightest; frame 3 = burst breaking into
scattered shards; frame 4 = faint fading remnants. Identical center pivot, identical
canvas scale and framing across all 4 frames — ONLY the burst pose/size changes, so
they overlay cleanly on the same spot.

TECHNICAL: TRANSPARENT background (PNG with alpha) on every frame. Centered burst with
margin. Square 1:1 composition, target 512x512 px per frame (matches the enemy avatar
frame so it overlays correctly). Deliver as 4 separate transparent PNGs.
```

**📁 Guardar los 4 PNGs como:** en `Assets/Art/Sprites/Enemies/Common/Hit/`
(carpeta **ya creada**), nombrados **en orden**: `hit_1.png`, `hit_2.png`,
`hit_3.png`, `hit_4.png` (frame 1 = chispa → frame 4 = remanente).

**Nota de asignación (Fase 4):** importar los 4 PNGs como Sprites. Asignarlos **en
orden** a la lista `enemyHitFrames` del componente `CombatUIController` (lista
`[SerializeField]`, misma forma en que hoy se asignan los frames del héroe — no es
setup nuevo, es el patrón de frames existente). `enemyHitFps` actual = 16.

---

## C7 — Caras de carta (ilustraciones)

> **Generado:** Fase 3 del plan de auditoría de arte, `modo:diseno` (2026-06-05).
> Anexo del slot **C7** ahora que el campo `CardDefinition.Art` ya está en main
> (PR #105). Render: `CardHandView` (combate) + `NewRunController` (draft de NewRun =
> slot **N2**), con fallback a texto. Spec de código: `Docs/dev/specs/art_c7_card_art_spec.md`.
>
> **Este bloque es la PLANTILLA ESCALABLE.** Cada cara de carta es UN bloque
> paste-ready. Para una carta futura: copiá un bloque del grupo correcto (combate o
> draft), cambiá sujeto + color + el `<id>` de la ruta, y listo. No hace falta tocar
> código: el campo `Art` es opcional → fallback automático mientras no haya PNG.

### Diferencias con el lote drop-in (leer antes de generar)

- **Restricción técnica C7 (vale para TODAS las caras):** ilustración **vertical
  512×768 px (2:3)**, PNG con alpha, **un solo sujeto centrado pensado para una
  ventana vertical dentro del marco de la carta** — NO una escena de página completa,
  NO landscape. `preserveAspect` letterboxea el vertical dentro de la región superior
  del recuadro (aceptable para placeholder). Difiere del lote drop-in (avatares
  cuadrados 1:1).
- **Ruta/nombre (decisión D8 del spec):** `Assets/Art/Sprites/Cards/carta_<id>.png`,
  donde `<id>` = el `Id` de la `CardDefinition`. **La carpeta `Assets/Art/Sprites/Cards/`
  NO existe todavía** — la crea quien importe el arte (Fase 4), no esta sesión.
- **Asignación (Fase 4, igual para todas):** importar como **Sprite (2D and UI /
  Single)** con alpha; asignar el sprite al campo **`Art`** del `CardDefinition`
  correspondiente. Sin scale/offset por carta (decisión D7).

### Inventario real (verificado contra los SOs)

**Total: 24 PNGs** = **6 caras de combate** (lados fijos A/B de los duales del mazo
inicial) + **18 caras del draft** de NewRun.

`[INTERPRETACIÓN]` Verificado contra los `.asset`:

- El **mazo inicial** (`RunCombatConfig_Act1`) son 3 `DualCardDefinition`:
  `StrikeDual`, `DefendDual`, `BattleFocusDual` (4× / 2× / 1×). **Las duales NO tienen
  campo `Art`** (es de `CardDefinition`); el arte vive en cada **lado**, que SÍ es una
  `CardDefinition` con su propio `id` → su propio PNG. Por eso son **6 caras
  world-FIJAS**: lado A = Mundo A (Medieval), lado B = Mundo B (Cyberpunk). El SO de
  la dual misma **no recibe PNG**.
- Las **18 caras del draft** (`NewRunConfig.draftFaces`, 6 tipos × 3) son
  **world-AGNÓSTICAS**: `StarterDraft.PickFacesForType` las filtra por **tipo
  elemental** y el MISMO pool alimenta la columna del Mundo A y la del Mundo B — el
  jugador decide qué tipo va a cada mundo. Una misma cara puede caer en el lado A en
  una run y en el lado B en otra. Como cada cara es UNA `CardDefinition` con UN solo
  campo `Art`, **no puede tener arte medieval y cyberpunk a la vez** → su ilustración
  es **neutral al mundo**, anclada al COLOR/tipo, no a un mundo.

**Decisión explícita por carta (lo pedido):**

- Caras de **combate** (lados de duales del starter) → **variante de mundo fija**
  (A Medieval / B Cyberpunk): el SO fija el lado.
- Caras del **draft** → **world-neutral, color-keyed** (sin bloque de mundo en el
  prompt; paleta monocromática del tipo). No se inventa un lado que el SO no fija.

`[ALTERNATIVA]` Sesgar cada color del draft hacia un mundo "por defecto" → descartada:
la cara flota entre ambos mundos según la elección del jugador, así que un sesgo se
vería incoherente la mitad de las veces. Si en el futuro se quiere arte por-mundo en
las caras del draft, requeriría código (2º campo de arte + conmutación), igual que la
deuda B del boss C4. Para placeholder: world-neutral.

| `<id>` | Carta (efecto) | Single / Lado | Tipo | Mundo del prompt |
|--------|----------------|---------------|------|------------------|
| `strike_basic`        | Strike — 6 dmg     | lado A de `StrikeDual`      | Rojo  | **A Medieval**  |
| `strike_side_b`       | Strike (B) — 9 dmg | lado B de `StrikeDual`      | Negro | **B Cyberpunk** |
| `defend_basic`        | Defend — 5 block   | lado A de `DefendDual`      | None  | **A Medieval**  |
| `defend_side_b`       | Defend (B) — 8 blk | lado B de `DefendDual`      | None  | **B Cyberpunk** |
| `battle_focus`        | Battle Focus — draw 2 | lado A de `BattleFocusDual` | None | **A Medieval**  |
| `battle_focus_side_b` | Battle Focus (B) — draw 3 | lado B de `BattleFocusDual` | None | **B Cyberpunk** |
| `face_rojo_1/2/3`     | Golpe 6 / Guardia 5 / Golpe 8 | draft (lado flexible) | Rojo     | **Neutral** |
| `face_amarillo_1/2/3` | Golpe 6 / Guardia 5 / Golpe 8 | draft | Amarillo | **Neutral** |
| `face_azul_1/2/3`     | Golpe 6 / Guardia 5 / Golpe 8 | draft | Azul     | **Neutral** |
| `face_morado_1/2/3`   | Golpe 6 / Guardia 5 / Golpe 8 | draft | Morado   | **Neutral** |
| `face_negro_1/2/3`    | Golpe 6 / Guardia 5 / Golpe 8 | draft | Negro    | **Neutral** |
| `face_blanco_1/2/3`   | Golpe 6 / Guardia 5 / Golpe 8 | draft | Blanco   | **Neutral** |

---

### Grupo A — Caras de COMBATE (lados de los duales del mazo inicial)

#### C7-01 — `strike_basic` · Strike (Rojo · Mundo A Medieval)

**Prompt paste-ready:**

```
STYLE: Childlike handmade arts-and-crafts aesthetic — as if drawn and built by two
kids from cardboard, wax crayons, cut-out paper, magazine clippings, school glue and
sticky tape. Visible crayon strokes, uneven hand-cut paper edges, cardboard texture,
slightly wonky proportions, warm imperfect lines. Flat 2D, storybook/diorama feel.
Everything looks like a physical scrap pinned into a kids' imaginary world.
NOT digital-clean, NOT vector-smooth, NOT photorealistic, NO gradients-heavy 3D.

WORLD A — DARK MEDIEVAL: gloomy fairy-tale medieval theme rendered in the same
crayon-and-cardboard craft style. Muted earthy palette — deep browns, charcoal,
dried-blood reds, candle-amber highlights. Castles, torn banners, rusty armor,
stitched cloth monsters, torchlight. Spooky but still handmade and toy-like, never
gory-realistic.

SUBJECT: a MEDIEVAL STRIKE — a worn iron longsword (or jagged notched blade) caught
mid downward SLASH, with bold scarlet crayon impact slash-lines trailing behind it.
A fast, basic attack. Dried-blood red is the dominant accent (this is a "Rojo"-type
card). No full character needed — the weapon plus the slash IS the subject. Cardboard-
and-crayon, toy-like.

TECHNICAL: single subject, centered, composed for a TALL VERTICAL WINDOW inside a card
frame — NOT a full-page scene, NOT landscape. Generous empty margin around the subject.
TRANSPARENT background (PNG with alpha, no ground, no floor, no scenery). Vertical 2:3
composition, target 512x768 px. Clean silhouette, readable as a small card illustration.
```

**📁 Guardar el PNG como:** `Assets/Art/Sprites/Cards/carta_strike_basic.png`
— la carpeta `Assets/Art/Sprites/Cards/` se crea al importar (Fase 4), no existe aún.

**Nota de asignación (Fase 4):** importar como Sprite (2D and UI / Single, alpha).
Asignar al campo `Art` de `Assets/ScriptableObjects/Cards/StrikeBasic.asset`
(`CardDefinition.Art`). Es el lado A de `StrikeDual` → se ve cuando el Mundo A está activo.

---

#### C7-02 — `strike_side_b` · Strike (B) (Negro · Mundo B Cyberpunk)

**Prompt paste-ready:**

```
STYLE: Childlike handmade arts-and-crafts aesthetic — as if drawn and built by two
kids from cardboard, wax crayons, cut-out paper, magazine clippings, school glue and
sticky tape. Visible crayon strokes, uneven hand-cut paper edges, cardboard texture,
slightly wonky proportions, warm imperfect lines. Flat 2D, storybook/diorama feel.
Everything looks like a physical scrap pinned into a kids' imaginary world.
NOT digital-clean, NOT vector-smooth, NOT photorealistic, NO gradients-heavy 3D.

WORLD B — CYBERPUNK: neon-soaked futuristic theme rendered in the SAME crayon-and-
cardboard craft style. Palette — electric cyan, magenta, violet, acid green over
dark base; "neon" drawn as glowing crayon scribbles, circuits as taped foil and
marker lines, robots built from cardboard boxes and bottle caps. High-tech subjects
made of craft materials, never sleek digital chrome.

SUBJECT: a CYBERPUNK STRIKE — a glowing vibro-blade / energy knife (or a cyber-fist)
mid-swing, cutting with sharp neon slash-lines. Built from cardboard, taped foil and
marker circuitry; the blade "glows" as a black-and-violet crayon scribble edged with
cold neon. Black / dark charcoal is the dominant accent (this is a "Negro"-type card),
lit by faint violet-cyan neon so it still reads. The weapon plus the neon slash IS the
subject. Craft-made, never sleek chrome.

TECHNICAL: single subject, centered, composed for a TALL VERTICAL WINDOW inside a card
frame — NOT a full-page scene, NOT landscape. Generous empty margin around the subject.
TRANSPARENT background (PNG with alpha, no ground, no floor, no scenery). Vertical 2:3
composition, target 512x768 px. Clean silhouette, readable as a small card illustration.
```

**📁 Guardar el PNG como:** `Assets/Art/Sprites/Cards/carta_strike_side_b.png`

**Nota de asignación (Fase 4):** importar como Sprite. Asignar al campo `Art` de
`Assets/ScriptableObjects/Cards/StrikeSideB.asset`. Lado B de `StrikeDual` → se ve
cuando el Mundo B está activo.

---

#### C7-03 — `defend_basic` · Defend (None · Mundo A Medieval)

**Prompt paste-ready:**

```
STYLE: Childlike handmade arts-and-crafts aesthetic — as if drawn and built by two
kids from cardboard, wax crayons, cut-out paper, magazine clippings, school glue and
sticky tape. Visible crayon strokes, uneven hand-cut paper edges, cardboard texture,
slightly wonky proportions, warm imperfect lines. Flat 2D, storybook/diorama feel.
Everything looks like a physical scrap pinned into a kids' imaginary world.
NOT digital-clean, NOT vector-smooth, NOT photorealistic, NO gradients-heavy 3D.

WORLD A — DARK MEDIEVAL: gloomy fairy-tale medieval theme rendered in the same
crayon-and-cardboard craft style. Muted earthy palette — deep browns, charcoal,
dried-blood reds, candle-amber highlights. Castles, torn banners, rusty armor,
stitched cloth monsters, torchlight. Spooky but still handmade and toy-like, never
gory-realistic.

SUBJECT: a MEDIEVAL DEFENSE — a battered round wooden SHIELD bound with a rusty iron
rim and a cross brace, planted and bracing like a barrier. This is a neutral (None)
card: keep the earthy medieval palette — browns, charcoal, candle-amber — with NO
strong element-color tint. Sturdy, toy-like, cardboard-and-crayon. The shield is the
single subject.

TECHNICAL: single subject, centered, composed for a TALL VERTICAL WINDOW inside a card
frame — NOT a full-page scene, NOT landscape. Generous empty margin around the subject.
TRANSPARENT background (PNG with alpha, no ground, no floor, no scenery). Vertical 2:3
composition, target 512x768 px. Clean silhouette, readable as a small card illustration.
```

**📁 Guardar el PNG como:** `Assets/Art/Sprites/Cards/carta_defend_basic.png`

**Nota de asignación (Fase 4):** importar como Sprite. Asignar al campo `Art` de
`Assets/ScriptableObjects/Cards/DefendBasic.asset`. Lado A de `DefendDual`.

---

#### C7-04 — `defend_side_b` · Defend (B) (None · Mundo B Cyberpunk)

**Prompt paste-ready:**

```
STYLE: Childlike handmade arts-and-crafts aesthetic — as if drawn and built by two
kids from cardboard, wax crayons, cut-out paper, magazine clippings, school glue and
sticky tape. Visible crayon strokes, uneven hand-cut paper edges, cardboard texture,
slightly wonky proportions, warm imperfect lines. Flat 2D, storybook/diorama feel.
Everything looks like a physical scrap pinned into a kids' imaginary world.
NOT digital-clean, NOT vector-smooth, NOT photorealistic, NO gradients-heavy 3D.

WORLD B — CYBERPUNK: neon-soaked futuristic theme rendered in the SAME crayon-and-
cardboard craft style. Palette — electric cyan, magenta, violet, acid green over
dark base; "neon" drawn as glowing crayon scribbles, circuits as taped foil and
marker lines, robots built from cardboard boxes and bottle caps. High-tech subjects
made of craft materials, never sleek digital chrome.

SUBJECT: a CYBERPUNK DEFENSE — a hexagonal holographic FORCE-FIELD / energy barrier,
drawn as a shield-shaped grid of taped foil strips and glowing marker lines hovering
in front. This is a neutral (None) card: use a cool neutral neon (cyan-white) glow
WITHOUT a strong single element color. Built from craft scraps, never sleek digital.
The energy shield is the single subject.

TECHNICAL: single subject, centered, composed for a TALL VERTICAL WINDOW inside a card
frame — NOT a full-page scene, NOT landscape. Generous empty margin around the subject.
TRANSPARENT background (PNG with alpha, no ground, no floor, no scenery). Vertical 2:3
composition, target 512x768 px. Clean silhouette, readable as a small card illustration.
```

**📁 Guardar el PNG como:** `Assets/Art/Sprites/Cards/carta_defend_side_b.png`

**Nota de asignación (Fase 4):** importar como Sprite. Asignar al campo `Art` de
`Assets/ScriptableObjects/Cards/DefendSideB.asset`. Lado B de `DefendDual`.

---

#### C7-05 — `battle_focus` · Battle Focus (None · Mundo A Medieval)

**Prompt paste-ready:**

```
STYLE: Childlike handmade arts-and-crafts aesthetic — as if drawn and built by two
kids from cardboard, wax crayons, cut-out paper, magazine clippings, school glue and
sticky tape. Visible crayon strokes, uneven hand-cut paper edges, cardboard texture,
slightly wonky proportions, warm imperfect lines. Flat 2D, storybook/diorama feel.
Everything looks like a physical scrap pinned into a kids' imaginary world.
NOT digital-clean, NOT vector-smooth, NOT photorealistic, NO gradients-heavy 3D.

WORLD A — DARK MEDIEVAL: gloomy fairy-tale medieval theme rendered in the same
crayon-and-cardboard craft style. Muted earthy palette — deep browns, charcoal,
dried-blood reds, candle-amber highlights. Castles, torn banners, rusty armor,
stitched cloth monsters, torchlight. Spooky but still handmade and toy-like, never
gory-realistic.

SUBJECT: MEDIEVAL FOCUS / INSIGHT (a "draw more cards" card) — an open hand-stitched
spellbook or an unrolled parchment scroll, with a small glowing candle (or a single
watchful eye) above it and a couple of parchment cards being fanned/drawn out. This is
a neutral (None) card: earthy medieval palette with a warm candle-amber glow, no strong
element color. Handmade paper-and-crayon. The book plus the drawn cards is the subject.

TECHNICAL: single subject, centered, composed for a TALL VERTICAL WINDOW inside a card
frame — NOT a full-page scene, NOT landscape. Generous empty margin around the subject.
TRANSPARENT background (PNG with alpha, no ground, no floor, no scenery). Vertical 2:3
composition, target 512x768 px. Clean silhouette, readable as a small card illustration.
```

**📁 Guardar el PNG como:** `Assets/Art/Sprites/Cards/carta_battle_focus.png`

**Nota de asignación (Fase 4):** importar como Sprite. Asignar al campo `Art` de
`Assets/ScriptableObjects/Cards/BattleFocus.asset`. Lado A de `BattleFocusDual`.

---

#### C7-06 — `battle_focus_side_b` · Battle Focus (B) (None · Mundo B Cyberpunk)

**Prompt paste-ready:**

```
STYLE: Childlike handmade arts-and-crafts aesthetic — as if drawn and built by two
kids from cardboard, wax crayons, cut-out paper, magazine clippings, school glue and
sticky tape. Visible crayon strokes, uneven hand-cut paper edges, cardboard texture,
slightly wonky proportions, warm imperfect lines. Flat 2D, storybook/diorama feel.
Everything looks like a physical scrap pinned into a kids' imaginary world.
NOT digital-clean, NOT vector-smooth, NOT photorealistic, NO gradients-heavy 3D.

WORLD B — CYBERPUNK: neon-soaked futuristic theme rendered in the SAME crayon-and-
cardboard craft style. Palette — electric cyan, magenta, violet, acid green over
dark base; "neon" drawn as glowing crayon scribbles, circuits as taped foil and
marker lines, robots built from cardboard boxes and bottle caps. High-tech subjects
made of craft materials, never sleek digital chrome.

SUBJECT: CYBERPUNK FOCUS (a "draw more cards" card) — a pair of cardboard VR goggles /
a cyber visor with a glowing scan-reticle, and a few holographic data-cards streaming
and fanning out of it. This is a neutral (None) card: cool neutral neon (cyan) glow,
no strong single element color. Built from cardboard, foil and marker lines, never
sleek chrome. The visor plus the streaming cards is the subject.

TECHNICAL: single subject, centered, composed for a TALL VERTICAL WINDOW inside a card
frame — NOT a full-page scene, NOT landscape. Generous empty margin around the subject.
TRANSPARENT background (PNG with alpha, no ground, no floor, no scenery). Vertical 2:3
composition, target 512x768 px. Clean silhouette, readable as a small card illustration.
```

**📁 Guardar el PNG como:** `Assets/Art/Sprites/Cards/carta_battle_focus_side_b.png`

**Nota de asignación (Fase 4):** importar como Sprite. Asignar al campo `Art` de
`Assets/ScriptableObjects/Cards/BattleFocusSideB.asset`. Lado B de `BattleFocusDual`.

---

### Grupo B — Caras del DRAFT de NewRun (world-neutral, color-keyed)

> **Por qué no llevan bloque de Mundo A/B:** estas 18 caras se filtran por tipo y se
> reutilizan en CUALQUIER lado del dual compuesto (el jugador asigna el tipo al Mundo A
> o al B). Con un solo campo `Art` no pueden ser medieval y cyberpunk a la vez → se
> generan **neutrales al mundo**, en paleta monocromática del color del tipo. Los tres
> sujetos por color son siempre los mismos (Golpe / Guardia / Golpe fuerte); lo único
> que cambia entre colores es la **paleta**. Eso hace el grupo trivial de escalar.
>
> Color por tipo (fuente: enum `ElementType` + helper `ElementTypeColors`):
> Rojo = scarlet/fire red · Amarillo = golden yellow · Azul = blue/cyan ·
> Morado = violet/purple · Negro = black/charcoal (con tenues realces gris-cian para
> que la silueta lea) · Blanco = white/cream/pale chalk (con contornos gris claro para
> que la forma lea).

#### C7-07 — `face_rojo_1` · Golpe Rojo (6 dmg · Neutral)

**Prompt paste-ready:**

```
STYLE: Childlike handmade arts-and-crafts aesthetic — as if drawn and built by two
kids from cardboard, wax crayons, cut-out paper, magazine clippings, school glue and
sticky tape. Visible crayon strokes, uneven hand-cut paper edges, cardboard texture,
slightly wonky proportions, warm imperfect lines. Flat 2D, storybook/diorama feel.
NOT digital-clean, NOT vector-smooth, NOT photorealistic, NO gradients-heavy 3D.

TYPE COLOR — RED ("Rojo"): render the WHOLE illustration in a monochromatic scarlet /
fire-red crayon palette — red is the card's identity. WORLD-NEUTRAL: do NOT theme it
medieval or cyberpunk (this card is reused on either world side); keep it a plain
handmade icon-like illustration, just the motif drawn in red crayon on transparent.

SUBJECT: a basic ATTACK motif — a single crayon slash / a small struck blow, e.g. a
short dagger or a fist with a few sharp impact lines. Light, basic hit (this is the
6-damage strike).

TECHNICAL: single subject, centered, composed for a TALL VERTICAL WINDOW inside a card
frame — NOT a full-page scene, NOT landscape. Generous empty margin. TRANSPARENT
background (PNG with alpha, no ground, no scenery). Vertical 2:3, target 512x768 px.
Clean silhouette, readable as a small card illustration.
```

**📁 Guardar el PNG como:** `Assets/Art/Sprites/Cards/carta_face_rojo_1.png`

**Nota de asignación (Fase 4):** Sprite → campo `Art` de
`Assets/ScriptableObjects/Cards/NewRunFaces/Face_Rojo_1.asset`.

---

#### C7-08 — `face_rojo_2` · Guardia Rojo (block 5 · Neutral)

**Prompt paste-ready:**

```
STYLE: Childlike handmade arts-and-crafts aesthetic — as if drawn and built by two
kids from cardboard, wax crayons, cut-out paper, magazine clippings, school glue and
sticky tape. Visible crayon strokes, uneven hand-cut paper edges, cardboard texture,
slightly wonky proportions, warm imperfect lines. Flat 2D, storybook/diorama feel.
NOT digital-clean, NOT vector-smooth, NOT photorealistic, NO gradients-heavy 3D.

TYPE COLOR — RED ("Rojo"): render the WHOLE illustration in a monochromatic scarlet /
fire-red crayon palette — red is the card's identity. WORLD-NEUTRAL: do NOT theme it
medieval or cyberpunk (this card is reused on either world side); keep it a plain
handmade icon-like illustration, just the motif drawn in red crayon on transparent.

SUBJECT: a basic GUARD / BLOCK motif — a simple hand-drawn shield outline bracing,
with a couple of "block" arc lines around it. Defensive (this is the 5-block guard).

TECHNICAL: single subject, centered, composed for a TALL VERTICAL WINDOW inside a card
frame — NOT a full-page scene, NOT landscape. Generous empty margin. TRANSPARENT
background (PNG with alpha, no ground, no scenery). Vertical 2:3, target 512x768 px.
Clean silhouette, readable as a small card illustration.
```

**📁 Guardar el PNG como:** `Assets/Art/Sprites/Cards/carta_face_rojo_2.png`

**Nota de asignación (Fase 4):** Sprite → campo `Art` de `.../NewRunFaces/Face_Rojo_2.asset`.

---

#### C7-09 — `face_rojo_3` · Golpe Rojo (8 dmg · Neutral)

**Prompt paste-ready:**

```
STYLE: Childlike handmade arts-and-crafts aesthetic — as if drawn and built by two
kids from cardboard, wax crayons, cut-out paper, magazine clippings, school glue and
sticky tape. Visible crayon strokes, uneven hand-cut paper edges, cardboard texture,
slightly wonky proportions, warm imperfect lines. Flat 2D, storybook/diorama feel.
NOT digital-clean, NOT vector-smooth, NOT photorealistic, NO gradients-heavy 3D.

TYPE COLOR — RED ("Rojo"): render the WHOLE illustration in a monochromatic scarlet /
fire-red crayon palette — red is the card's identity. WORLD-NEUTRAL: do NOT theme it
medieval or cyberpunk (this card is reused on either world side); keep it a plain
handmade icon-like illustration, just the motif drawn in red crayon on transparent.

SUBJECT: a STRONGER ATTACK motif — a heavier double-slash or a bigger two-handed weapon
strike, with more, bolder impact lines. Reads as a harder hit than the basic strike
(this is the 8-damage strike).

TECHNICAL: single subject, centered, composed for a TALL VERTICAL WINDOW inside a card
frame — NOT a full-page scene, NOT landscape. Generous empty margin. TRANSPARENT
background (PNG with alpha, no ground, no scenery). Vertical 2:3, target 512x768 px.
Clean silhouette, readable as a small card illustration.
```

**📁 Guardar el PNG como:** `Assets/Art/Sprites/Cards/carta_face_rojo_3.png`

**Nota de asignación (Fase 4):** Sprite → campo `Art` de `.../NewRunFaces/Face_Rojo_3.asset`.

---

#### C7-10 — `face_amarillo_1` · Golpe Amarillo (6 dmg · Neutral)

**Prompt paste-ready:**

```
STYLE: Childlike handmade arts-and-crafts aesthetic — as if drawn and built by two
kids from cardboard, wax crayons, cut-out paper, magazine clippings, school glue and
sticky tape. Visible crayon strokes, uneven hand-cut paper edges, cardboard texture,
slightly wonky proportions, warm imperfect lines. Flat 2D, storybook/diorama feel.
NOT digital-clean, NOT vector-smooth, NOT photorealistic, NO gradients-heavy 3D.

TYPE COLOR — YELLOW ("Amarillo"): render the WHOLE illustration in a monochromatic
golden-yellow crayon palette — yellow is the card's identity. WORLD-NEUTRAL: do NOT
theme it medieval or cyberpunk (reused on either world side); keep it a plain handmade
icon-like illustration, just the motif drawn in yellow crayon on transparent.

SUBJECT: a basic ATTACK motif — a single crayon slash / a small struck blow, e.g. a
short dagger or a fist with a few sharp impact lines. Light, basic hit (6-damage strike).

TECHNICAL: single subject, centered, composed for a TALL VERTICAL WINDOW inside a card
frame — NOT a full-page scene, NOT landscape. Generous empty margin. TRANSPARENT
background (PNG with alpha, no ground, no scenery). Vertical 2:3, target 512x768 px.
Clean silhouette, readable as a small card illustration.
```

**📁 Guardar el PNG como:** `Assets/Art/Sprites/Cards/carta_face_amarillo_1.png`

**Nota de asignación (Fase 4):** Sprite → campo `Art` de `.../NewRunFaces/Face_Amarillo_1.asset`.

---

#### C7-11 — `face_amarillo_2` · Guardia Amarillo (block 5 · Neutral)

**Prompt paste-ready:**

```
STYLE: Childlike handmade arts-and-crafts aesthetic — as if drawn and built by two
kids from cardboard, wax crayons, cut-out paper, magazine clippings, school glue and
sticky tape. Visible crayon strokes, uneven hand-cut paper edges, cardboard texture,
slightly wonky proportions, warm imperfect lines. Flat 2D, storybook/diorama feel.
NOT digital-clean, NOT vector-smooth, NOT photorealistic, NO gradients-heavy 3D.

TYPE COLOR — YELLOW ("Amarillo"): render the WHOLE illustration in a monochromatic
golden-yellow crayon palette — yellow is the card's identity. WORLD-NEUTRAL: do NOT
theme it medieval or cyberpunk (reused on either world side); keep it a plain handmade
icon-like illustration, just the motif drawn in yellow crayon on transparent.

SUBJECT: a basic GUARD / BLOCK motif — a simple hand-drawn shield outline bracing,
with a couple of "block" arc lines around it. Defensive (5-block guard).

TECHNICAL: single subject, centered, composed for a TALL VERTICAL WINDOW inside a card
frame — NOT a full-page scene, NOT landscape. Generous empty margin. TRANSPARENT
background (PNG with alpha, no ground, no scenery). Vertical 2:3, target 512x768 px.
Clean silhouette, readable as a small card illustration.
```

**📁 Guardar el PNG como:** `Assets/Art/Sprites/Cards/carta_face_amarillo_2.png`

**Nota de asignación (Fase 4):** Sprite → campo `Art` de `.../NewRunFaces/Face_Amarillo_2.asset`.

---

#### C7-12 — `face_amarillo_3` · Golpe Amarillo (8 dmg · Neutral)

**Prompt paste-ready:**

```
STYLE: Childlike handmade arts-and-crafts aesthetic — as if drawn and built by two
kids from cardboard, wax crayons, cut-out paper, magazine clippings, school glue and
sticky tape. Visible crayon strokes, uneven hand-cut paper edges, cardboard texture,
slightly wonky proportions, warm imperfect lines. Flat 2D, storybook/diorama feel.
NOT digital-clean, NOT vector-smooth, NOT photorealistic, NO gradients-heavy 3D.

TYPE COLOR — YELLOW ("Amarillo"): render the WHOLE illustration in a monochromatic
golden-yellow crayon palette — yellow is the card's identity. WORLD-NEUTRAL: do NOT
theme it medieval or cyberpunk (reused on either world side); keep it a plain handmade
icon-like illustration, just the motif drawn in yellow crayon on transparent.

SUBJECT: a STRONGER ATTACK motif — a heavier double-slash or a bigger two-handed weapon
strike, with more, bolder impact lines. Reads as a harder hit (8-damage strike).

TECHNICAL: single subject, centered, composed for a TALL VERTICAL WINDOW inside a card
frame — NOT a full-page scene, NOT landscape. Generous empty margin. TRANSPARENT
background (PNG with alpha, no ground, no scenery). Vertical 2:3, target 512x768 px.
Clean silhouette, readable as a small card illustration.
```

**📁 Guardar el PNG como:** `Assets/Art/Sprites/Cards/carta_face_amarillo_3.png`

**Nota de asignación (Fase 4):** Sprite → campo `Art` de `.../NewRunFaces/Face_Amarillo_3.asset`.

---

#### C7-13 — `face_azul_1` · Golpe Azul (6 dmg · Neutral)

**Prompt paste-ready:**

```
STYLE: Childlike handmade arts-and-crafts aesthetic — as if drawn and built by two
kids from cardboard, wax crayons, cut-out paper, magazine clippings, school glue and
sticky tape. Visible crayon strokes, uneven hand-cut paper edges, cardboard texture,
slightly wonky proportions, warm imperfect lines. Flat 2D, storybook/diorama feel.
NOT digital-clean, NOT vector-smooth, NOT photorealistic, NO gradients-heavy 3D.

TYPE COLOR — BLUE ("Azul"): render the WHOLE illustration in a monochromatic blue /
cyan crayon palette — blue is the card's identity. WORLD-NEUTRAL: do NOT theme it
medieval or cyberpunk (reused on either world side); keep it a plain handmade
icon-like illustration, just the motif drawn in blue crayon on transparent.

SUBJECT: a basic ATTACK motif — a single crayon slash / a small struck blow, e.g. a
short dagger or a fist with a few sharp impact lines. Light, basic hit (6-damage strike).

TECHNICAL: single subject, centered, composed for a TALL VERTICAL WINDOW inside a card
frame — NOT a full-page scene, NOT landscape. Generous empty margin. TRANSPARENT
background (PNG with alpha, no ground, no scenery). Vertical 2:3, target 512x768 px.
Clean silhouette, readable as a small card illustration.
```

**📁 Guardar el PNG como:** `Assets/Art/Sprites/Cards/carta_face_azul_1.png`

**Nota de asignación (Fase 4):** Sprite → campo `Art` de `.../NewRunFaces/Face_Azul_1.asset`.

---

#### C7-14 — `face_azul_2` · Guardia Azul (block 5 · Neutral)

**Prompt paste-ready:**

```
STYLE: Childlike handmade arts-and-crafts aesthetic — as if drawn and built by two
kids from cardboard, wax crayons, cut-out paper, magazine clippings, school glue and
sticky tape. Visible crayon strokes, uneven hand-cut paper edges, cardboard texture,
slightly wonky proportions, warm imperfect lines. Flat 2D, storybook/diorama feel.
NOT digital-clean, NOT vector-smooth, NOT photorealistic, NO gradients-heavy 3D.

TYPE COLOR — BLUE ("Azul"): render the WHOLE illustration in a monochromatic blue /
cyan crayon palette — blue is the card's identity. WORLD-NEUTRAL: do NOT theme it
medieval or cyberpunk (reused on either world side); keep it a plain handmade
icon-like illustration, just the motif drawn in blue crayon on transparent.

SUBJECT: a basic GUARD / BLOCK motif — a simple hand-drawn shield outline bracing,
with a couple of "block" arc lines around it. Defensive (5-block guard).

TECHNICAL: single subject, centered, composed for a TALL VERTICAL WINDOW inside a card
frame — NOT a full-page scene, NOT landscape. Generous empty margin. TRANSPARENT
background (PNG with alpha, no ground, no scenery). Vertical 2:3, target 512x768 px.
Clean silhouette, readable as a small card illustration.
```

**📁 Guardar el PNG como:** `Assets/Art/Sprites/Cards/carta_face_azul_2.png`

**Nota de asignación (Fase 4):** Sprite → campo `Art` de `.../NewRunFaces/Face_Azul_2.asset`.

---

#### C7-15 — `face_azul_3` · Golpe Azul (8 dmg · Neutral)

**Prompt paste-ready:**

```
STYLE: Childlike handmade arts-and-crafts aesthetic — as if drawn and built by two
kids from cardboard, wax crayons, cut-out paper, magazine clippings, school glue and
sticky tape. Visible crayon strokes, uneven hand-cut paper edges, cardboard texture,
slightly wonky proportions, warm imperfect lines. Flat 2D, storybook/diorama feel.
NOT digital-clean, NOT vector-smooth, NOT photorealistic, NO gradients-heavy 3D.

TYPE COLOR — BLUE ("Azul"): render the WHOLE illustration in a monochromatic blue /
cyan crayon palette — blue is the card's identity. WORLD-NEUTRAL: do NOT theme it
medieval or cyberpunk (reused on either world side); keep it a plain handmade
icon-like illustration, just the motif drawn in blue crayon on transparent.

SUBJECT: a STRONGER ATTACK motif — a heavier double-slash or a bigger two-handed weapon
strike, with more, bolder impact lines. Reads as a harder hit (8-damage strike).

TECHNICAL: single subject, centered, composed for a TALL VERTICAL WINDOW inside a card
frame — NOT a full-page scene, NOT landscape. Generous empty margin. TRANSPARENT
background (PNG with alpha, no ground, no scenery). Vertical 2:3, target 512x768 px.
Clean silhouette, readable as a small card illustration.
```

**📁 Guardar el PNG como:** `Assets/Art/Sprites/Cards/carta_face_azul_3.png`

**Nota de asignación (Fase 4):** Sprite → campo `Art` de `.../NewRunFaces/Face_Azul_3.asset`.

---

#### C7-16 — `face_morado_1` · Golpe Morado (6 dmg · Neutral)

**Prompt paste-ready:**

```
STYLE: Childlike handmade arts-and-crafts aesthetic — as if drawn and built by two
kids from cardboard, wax crayons, cut-out paper, magazine clippings, school glue and
sticky tape. Visible crayon strokes, uneven hand-cut paper edges, cardboard texture,
slightly wonky proportions, warm imperfect lines. Flat 2D, storybook/diorama feel.
NOT digital-clean, NOT vector-smooth, NOT photorealistic, NO gradients-heavy 3D.

TYPE COLOR — PURPLE ("Morado"): render the WHOLE illustration in a monochromatic
violet / purple crayon palette — purple is the card's identity. WORLD-NEUTRAL: do NOT
theme it medieval or cyberpunk (reused on either world side); keep it a plain handmade
icon-like illustration, just the motif drawn in purple crayon on transparent.

SUBJECT: a basic ATTACK motif — a single crayon slash / a small struck blow, e.g. a
short dagger or a fist with a few sharp impact lines. Light, basic hit (6-damage strike).

TECHNICAL: single subject, centered, composed for a TALL VERTICAL WINDOW inside a card
frame — NOT a full-page scene, NOT landscape. Generous empty margin. TRANSPARENT
background (PNG with alpha, no ground, no scenery). Vertical 2:3, target 512x768 px.
Clean silhouette, readable as a small card illustration.
```

**📁 Guardar el PNG como:** `Assets/Art/Sprites/Cards/carta_face_morado_1.png`

**Nota de asignación (Fase 4):** Sprite → campo `Art` de `.../NewRunFaces/Face_Morado_1.asset`.

---

#### C7-17 — `face_morado_2` · Guardia Morado (block 5 · Neutral)

**Prompt paste-ready:**

```
STYLE: Childlike handmade arts-and-crafts aesthetic — as if drawn and built by two
kids from cardboard, wax crayons, cut-out paper, magazine clippings, school glue and
sticky tape. Visible crayon strokes, uneven hand-cut paper edges, cardboard texture,
slightly wonky proportions, warm imperfect lines. Flat 2D, storybook/diorama feel.
NOT digital-clean, NOT vector-smooth, NOT photorealistic, NO gradients-heavy 3D.

TYPE COLOR — PURPLE ("Morado"): render the WHOLE illustration in a monochromatic
violet / purple crayon palette — purple is the card's identity. WORLD-NEUTRAL: do NOT
theme it medieval or cyberpunk (reused on either world side); keep it a plain handmade
icon-like illustration, just the motif drawn in purple crayon on transparent.

SUBJECT: a basic GUARD / BLOCK motif — a simple hand-drawn shield outline bracing,
with a couple of "block" arc lines around it. Defensive (5-block guard).

TECHNICAL: single subject, centered, composed for a TALL VERTICAL WINDOW inside a card
frame — NOT a full-page scene, NOT landscape. Generous empty margin. TRANSPARENT
background (PNG with alpha, no ground, no scenery). Vertical 2:3, target 512x768 px.
Clean silhouette, readable as a small card illustration.
```

**📁 Guardar el PNG como:** `Assets/Art/Sprites/Cards/carta_face_morado_2.png`

**Nota de asignación (Fase 4):** Sprite → campo `Art` de `.../NewRunFaces/Face_Morado_2.asset`.

---

#### C7-18 — `face_morado_3` · Golpe Morado (8 dmg · Neutral)

**Prompt paste-ready:**

```
STYLE: Childlike handmade arts-and-crafts aesthetic — as if drawn and built by two
kids from cardboard, wax crayons, cut-out paper, magazine clippings, school glue and
sticky tape. Visible crayon strokes, uneven hand-cut paper edges, cardboard texture,
slightly wonky proportions, warm imperfect lines. Flat 2D, storybook/diorama feel.
NOT digital-clean, NOT vector-smooth, NOT photorealistic, NO gradients-heavy 3D.

TYPE COLOR — PURPLE ("Morado"): render the WHOLE illustration in a monochromatic
violet / purple crayon palette — purple is the card's identity. WORLD-NEUTRAL: do NOT
theme it medieval or cyberpunk (reused on either world side); keep it a plain handmade
icon-like illustration, just the motif drawn in purple crayon on transparent.

SUBJECT: a STRONGER ATTACK motif — a heavier double-slash or a bigger two-handed weapon
strike, with more, bolder impact lines. Reads as a harder hit (8-damage strike).

TECHNICAL: single subject, centered, composed for a TALL VERTICAL WINDOW inside a card
frame — NOT a full-page scene, NOT landscape. Generous empty margin. TRANSPARENT
background (PNG with alpha, no ground, no scenery). Vertical 2:3, target 512x768 px.
Clean silhouette, readable as a small card illustration.
```

**📁 Guardar el PNG como:** `Assets/Art/Sprites/Cards/carta_face_morado_3.png`

**Nota de asignación (Fase 4):** Sprite → campo `Art` de `.../NewRunFaces/Face_Morado_3.asset`.

---

#### C7-19 — `face_negro_1` · Golpe Negro (6 dmg · Neutral)

**Prompt paste-ready:**

```
STYLE: Childlike handmade arts-and-crafts aesthetic — as if drawn and built by two
kids from cardboard, wax crayons, cut-out paper, magazine clippings, school glue and
sticky tape. Visible crayon strokes, uneven hand-cut paper edges, cardboard texture,
slightly wonky proportions, warm imperfect lines. Flat 2D, storybook/diorama feel.
NOT digital-clean, NOT vector-smooth, NOT photorealistic, NO gradients-heavy 3D.

TYPE COLOR — BLACK ("Negro"): render the WHOLE illustration in a monochromatic black /
charcoal crayon palette — black is the card's identity. Add faint cold grey-cyan
highlights so the dark silhouette still reads. WORLD-NEUTRAL: do NOT theme it medieval
or cyberpunk (reused on either world side); keep it a plain handmade icon-like
illustration, just the motif drawn in black crayon on transparent.

SUBJECT: a basic ATTACK motif — a single crayon slash / a small struck blow, e.g. a
short dagger or a fist with a few sharp impact lines. Light, basic hit (6-damage strike).

TECHNICAL: single subject, centered, composed for a TALL VERTICAL WINDOW inside a card
frame — NOT a full-page scene, NOT landscape. Generous empty margin. TRANSPARENT
background (PNG with alpha, no ground, no scenery). Vertical 2:3, target 512x768 px.
Clean silhouette, readable as a small card illustration.
```

**📁 Guardar el PNG como:** `Assets/Art/Sprites/Cards/carta_face_negro_1.png`

**Nota de asignación (Fase 4):** Sprite → campo `Art` de `.../NewRunFaces/Face_Negro_1.asset`.

---

#### C7-20 — `face_negro_2` · Guardia Negro (block 5 · Neutral)

**Prompt paste-ready:**

```
STYLE: Childlike handmade arts-and-crafts aesthetic — as if drawn and built by two
kids from cardboard, wax crayons, cut-out paper, magazine clippings, school glue and
sticky tape. Visible crayon strokes, uneven hand-cut paper edges, cardboard texture,
slightly wonky proportions, warm imperfect lines. Flat 2D, storybook/diorama feel.
NOT digital-clean, NOT vector-smooth, NOT photorealistic, NO gradients-heavy 3D.

TYPE COLOR — BLACK ("Negro"): render the WHOLE illustration in a monochromatic black /
charcoal crayon palette — black is the card's identity. Add faint cold grey-cyan
highlights so the dark silhouette still reads. WORLD-NEUTRAL: do NOT theme it medieval
or cyberpunk (reused on either world side); keep it a plain handmade icon-like
illustration, just the motif drawn in black crayon on transparent.

SUBJECT: a basic GUARD / BLOCK motif — a simple hand-drawn shield outline bracing,
with a couple of "block" arc lines around it. Defensive (5-block guard).

TECHNICAL: single subject, centered, composed for a TALL VERTICAL WINDOW inside a card
frame — NOT a full-page scene, NOT landscape. Generous empty margin. TRANSPARENT
background (PNG with alpha, no ground, no scenery). Vertical 2:3, target 512x768 px.
Clean silhouette, readable as a small card illustration.
```

**📁 Guardar el PNG como:** `Assets/Art/Sprites/Cards/carta_face_negro_2.png`

**Nota de asignación (Fase 4):** Sprite → campo `Art` de `.../NewRunFaces/Face_Negro_2.asset`.

---

#### C7-21 — `face_negro_3` · Golpe Negro (8 dmg · Neutral)

**Prompt paste-ready:**

```
STYLE: Childlike handmade arts-and-crafts aesthetic — as if drawn and built by two
kids from cardboard, wax crayons, cut-out paper, magazine clippings, school glue and
sticky tape. Visible crayon strokes, uneven hand-cut paper edges, cardboard texture,
slightly wonky proportions, warm imperfect lines. Flat 2D, storybook/diorama feel.
NOT digital-clean, NOT vector-smooth, NOT photorealistic, NO gradients-heavy 3D.

TYPE COLOR — BLACK ("Negro"): render the WHOLE illustration in a monochromatic black /
charcoal crayon palette — black is the card's identity. Add faint cold grey-cyan
highlights so the dark silhouette still reads. WORLD-NEUTRAL: do NOT theme it medieval
or cyberpunk (reused on either world side); keep it a plain handmade icon-like
illustration, just the motif drawn in black crayon on transparent.

SUBJECT: a STRONGER ATTACK motif — a heavier double-slash or a bigger two-handed weapon
strike, with more, bolder impact lines. Reads as a harder hit (8-damage strike).

TECHNICAL: single subject, centered, composed for a TALL VERTICAL WINDOW inside a card
frame — NOT a full-page scene, NOT landscape. Generous empty margin. TRANSPARENT
background (PNG with alpha, no ground, no scenery). Vertical 2:3, target 512x768 px.
Clean silhouette, readable as a small card illustration.
```

**📁 Guardar el PNG como:** `Assets/Art/Sprites/Cards/carta_face_negro_3.png`

**Nota de asignación (Fase 4):** Sprite → campo `Art` de `.../NewRunFaces/Face_Negro_3.asset`.

---

#### C7-22 — `face_blanco_1` · Golpe Blanco (6 dmg · Neutral)

**Prompt paste-ready:**

```
STYLE: Childlike handmade arts-and-crafts aesthetic — as if drawn and built by two
kids from cardboard, wax crayons, cut-out paper, magazine clippings, school glue and
sticky tape. Visible crayon strokes, uneven hand-cut paper edges, cardboard texture,
slightly wonky proportions, warm imperfect lines. Flat 2D, storybook/diorama feel.
NOT digital-clean, NOT vector-smooth, NOT photorealistic, NO gradients-heavy 3D.

TYPE COLOR — WHITE ("Blanco"): render the WHOLE illustration in a monochromatic white /
cream / pale-chalk palette — white is the card's identity. Use light grey crayon
outlines so the white shape reads even on a light background. WORLD-NEUTRAL: do NOT
theme it medieval or cyberpunk (reused on either world side); keep it a plain handmade
icon-like illustration, just the motif drawn in white/chalk on transparent.

SUBJECT: a basic ATTACK motif — a single crayon slash / a small struck blow, e.g. a
short dagger or a fist with a few sharp impact lines. Light, basic hit (6-damage strike).

TECHNICAL: single subject, centered, composed for a TALL VERTICAL WINDOW inside a card
frame — NOT a full-page scene, NOT landscape. Generous empty margin. TRANSPARENT
background (PNG with alpha, no ground, no scenery). Vertical 2:3, target 512x768 px.
Clean silhouette, readable as a small card illustration.
```

**📁 Guardar el PNG como:** `Assets/Art/Sprites/Cards/carta_face_blanco_1.png`

**Nota de asignación (Fase 4):** Sprite → campo `Art` de `.../NewRunFaces/Face_Blanco_1.asset`.

---

#### C7-23 — `face_blanco_2` · Guardia Blanco (block 5 · Neutral)

**Prompt paste-ready:**

```
STYLE: Childlike handmade arts-and-crafts aesthetic — as if drawn and built by two
kids from cardboard, wax crayons, cut-out paper, magazine clippings, school glue and
sticky tape. Visible crayon strokes, uneven hand-cut paper edges, cardboard texture,
slightly wonky proportions, warm imperfect lines. Flat 2D, storybook/diorama feel.
NOT digital-clean, NOT vector-smooth, NOT photorealistic, NO gradients-heavy 3D.

TYPE COLOR — WHITE ("Blanco"): render the WHOLE illustration in a monochromatic white /
cream / pale-chalk palette — white is the card's identity. Use light grey crayon
outlines so the white shape reads even on a light background. WORLD-NEUTRAL: do NOT
theme it medieval or cyberpunk (reused on either world side); keep it a plain handmade
icon-like illustration, just the motif drawn in white/chalk on transparent.

SUBJECT: a basic GUARD / BLOCK motif — a simple hand-drawn shield outline bracing,
with a couple of "block" arc lines around it. Defensive (5-block guard).

TECHNICAL: single subject, centered, composed for a TALL VERTICAL WINDOW inside a card
frame — NOT a full-page scene, NOT landscape. Generous empty margin. TRANSPARENT
background (PNG with alpha, no ground, no scenery). Vertical 2:3, target 512x768 px.
Clean silhouette, readable as a small card illustration.
```

**📁 Guardar el PNG como:** `Assets/Art/Sprites/Cards/carta_face_blanco_2.png`

**Nota de asignación (Fase 4):** Sprite → campo `Art` de `.../NewRunFaces/Face_Blanco_2.asset`.

---

#### C7-24 — `face_blanco_3` · Golpe Blanco (8 dmg · Neutral)

**Prompt paste-ready:**

```
STYLE: Childlike handmade arts-and-crafts aesthetic — as if drawn and built by two
kids from cardboard, wax crayons, cut-out paper, magazine clippings, school glue and
sticky tape. Visible crayon strokes, uneven hand-cut paper edges, cardboard texture,
slightly wonky proportions, warm imperfect lines. Flat 2D, storybook/diorama feel.
NOT digital-clean, NOT vector-smooth, NOT photorealistic, NO gradients-heavy 3D.

TYPE COLOR — WHITE ("Blanco"): render the WHOLE illustration in a monochromatic white /
cream / pale-chalk palette — white is the card's identity. Use light grey crayon
outlines so the white shape reads even on a light background. WORLD-NEUTRAL: do NOT
theme it medieval or cyberpunk (reused on either world side); keep it a plain handmade
icon-like illustration, just the motif drawn in white/chalk on transparent.

SUBJECT: a STRONGER ATTACK motif — a heavier double-slash or a bigger two-handed weapon
strike, with more, bolder impact lines. Reads as a harder hit (8-damage strike).

TECHNICAL: single subject, centered, composed for a TALL VERTICAL WINDOW inside a card
frame — NOT a full-page scene, NOT landscape. Generous empty margin. TRANSPARENT
background (PNG with alpha, no ground, no scenery). Vertical 2:3, target 512x768 px.
Clean silhouette, readable as a small card illustration.
```

**📁 Guardar el PNG como:** `Assets/Art/Sprites/Cards/carta_face_blanco_3.png`

**Nota de asignación (Fase 4):** Sprite → campo `Art` de `.../NewRunFaces/Face_Blanco_3.asset`.

---

## Cierre — orden sugerido de las próximas fases

`[PROPUESTA]` Después de generar e importar este lote drop-in (C1–C4, C6), el orden
que ordena el resto de la integración de arte es:

1. **C8 — Tinte por tipo elemental (quick win de CÓDIGO, sin IA).** Es la ganancia
   más barata: los 6 tipos SON colores (Rojo/Amarillo/Azul/Morado/Negro/Blanco).
   Helper `ElementType → Color` + tintar borde/fondo del botón de carta en
   `CardHandView` (hoy solo prefijo de texto `[Rojo]`). No necesita prompt; va a
   `modo:implementacion`. Buen candidato para el primer PR de arte/código.
2. **C7 — Cara de carta · ✔ HECHO.** El código (campo `CardDefinition.Art` + render
   en `CardHandView`/`NewRunController`) está en main (PR #105) y los **24 prompts**
   ya están en la sección `## C7 — Caras de carta` de este doc (6 caras de combate +
   18 del draft de NewRun = N2). Próximo paso de C7 = **Fase 4**: generar los PNGs,
   importarlos a `Assets/Art/Sprites/Cards/` y asignarlos al campo `Art` de cada SO.
3. Luego: M1/M2 (mapa), S1 (vendedor), N1 (fondo NewRun) — prompts + PRs de código
   chicos. Pulido al final: C5 (héroe Mundo B), H1 (íconos HUD), y la **variante B del
   boss (UNIT-RB7)** una vez exista el segundo campo de avatar.

> **Próxima sesión recomendada:** **Fase 4 de C7** (generar/importar/asignar los 24
> PNGs de carta), o `modo:implementacion` para el quick win de **C8** (tinte por tipo,
> solo código), o `modo:diseno` para el siguiente slot mixto (M1/M2/S1/N1).
