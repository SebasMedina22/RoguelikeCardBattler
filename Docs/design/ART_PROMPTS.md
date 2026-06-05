# ART_PROMPTS.md — Prompts de IA para placeholders (Fase 3)

> **Qué es este archivo:** prompts paste-ready (en inglés) para generar los
> placeholders de arte con una IA generadora de imágenes. Doc dedicado y separado de
> [ART_NEEDS.md](ART_NEEDS.md) porque estos prompts se copian/pegan repetido — el
> catálogo de slots y el estilo madre viven allá; acá vive el texto final listo para
> pegar.
>
> **Generado:** Fase 3 del plan de auditoría de arte, `modo:diseno` (2026-06-05).
> **Alcance de esta sesión:** SOLO el lote **drop-in** (cero código): C1, C2, C3, C6
> y C4 (con deuda anotada). Los slots mixtos (C5, C7, C8, M1, M2, S1, N1, N2, H1) NO
> se prompean acá todavía — necesitan código antes.
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

## Cierre — orden sugerido de las próximas fases

`[PROPUESTA]` Después de generar e importar este lote drop-in (C1–C4, C6), el orden
que ordena el resto de la integración de arte es:

1. **C8 — Tinte por tipo elemental (quick win de CÓDIGO, sin IA).** Es la ganancia
   más barata: los 6 tipos SON colores (Rojo/Amarillo/Azul/Morado/Negro/Blanco).
   Helper `ElementType → Color` + tintar borde/fondo del botón de carta en
   `CardHandView` (hoy solo prefijo de texto `[Rojo]`). No necesita prompt; va a
   `modo:implementacion`. Buen candidato para el primer PR de arte/código.
2. **C7 — Cara de carta (la pieza grande, requiere spec de código ANTES del prompt).**
   Hay que diseñar primero el PR que agrega un campo `Sprite` a `CardDefinition` y el
   render en `CardHandView`. **Recién con ese campo cerrado se diseña el prompt** de
   "ilustración de carta". C7 también desbloquea N2 (caras del draft de NewRun).
3. Luego: M1/M2 (mapa), S1 (vendedor), N1 (fondo NewRun) — prompts + PRs de código
   chicos. Pulido al final: C5 (héroe Mundo B), H1 (íconos HUD), y la **variante B del
   boss (UNIT-RB7)** una vez exista el segundo campo de avatar.

> **Próxima sesión recomendada:** `modo:diseno` para el **spec de código de C7**
> (campo sprite en `CardDefinition` + render en `CardHandView`), o bien
> `modo:implementacion` para el quick win de **C8**. C7 es la dependencia crítica que
> destraba más arte.
