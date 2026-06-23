# ART_PROMPTS.md — Plantillas de formato de prompt (por tipo de asset)

> **Qué es este archivo:** las **plantillas de formato** paste-ready, una por TIPO de
> asset (avatar/personaje, cara de carta, fondo de panel/escena). Sirven como molde de
> estructura para el skill `/art-prompt` y para escribir un prompt a mano.
>
> **Qué NO es:** ya **no** guarda los prompts concretos de cada slot (los 24 de C1–C7,
> los fondos de evento, etc.). Esos eran de un solo uso — se **regeneran** con el skill
> `/art-prompt <slot>` cuando hagan falta. El **inventario de qué arte está hecho/falta**
> vive en [ART_NEEDS.md](ART_NEEDS.md) (tabla de slots); el **estilo madre** (bloque
> `STYLE:` + variantes de mundo) vive en [ART_NEEDS.md §Fase 2](ART_NEEDS.md) y es la
> fuente única de verdad.
>
> **Decisión 2026-06-23:** se adelgazó este doc (de ~1200 líneas de prompts one-off a
> estas plantillas) porque las reglas de prompteo viven en el skill y los prompts
> concretos no aportan guardarse. Histórico completo en git si se necesita arqueología.

---

## Cómo usar

1. Identificá el **tipo** de asset (avatar 1:1 / cara de carta 2:3 / fondo 16:9) → copiá
   la plantilla de abajo.
2. Pegá **textual** el bloque `STYLE:` y la variante de mundo (`WORLD A` / `WORLD B`) o
   el `TYPE COLOR` correspondiente **desde [ART_NEEDS.md §Fase 2](ART_NEEDS.md)** — no de
   memoria. Esa es la regla anti-drift.
3. Reemplazá `<SUBJECT ...>` por el sujeto concreto del slot.
4. Dejá el bloque `TECHNICAL:` de la plantilla (ya trae tamaño/aspect/transparencia del
   tipo).
5. Guardá el PNG en la ruta de la convención del slot (ver ART_NEEDS) y asigná al campo
   del SO correspondiente.

> Lo de arriba lo automatiza el skill **`/art-prompt <slot>`**, que relee ART_NEEDS +
> estas plantillas y emite el bloque final. Preferí el skill; estas plantillas son el
> molde que consume.

**Estructura canónica de todo prompt (orden de bloques):**
`STYLE:` (siempre) → variante de mundo o `TYPE COLOR` → `SUBJECT:` → `TECHNICAL:` →
`📁 Guardar como:` → `Nota de asignación (Fase 4)`.

El bloque base (pegar SIEMPRE, textual desde ART_NEEDS §Fase 2):

```
STYLE: Childlike handmade arts-and-crafts aesthetic — as if drawn and built by two
kids from cardboard, wax crayons, cut-out paper, magazine clippings, school glue and
sticky tape. Visible crayon strokes, uneven hand-cut paper edges, cardboard texture,
slightly wonky proportions, warm imperfect lines. Flat 2D, storybook/diorama feel.
Everything looks like a physical scrap pinned into a kids' imaginary world.
NOT digital-clean, NOT vector-smooth, NOT photorealistic, NO gradients-heavy 3D.
```

---

## Plantilla A — Avatar / personaje (enemigo, vendedor, boss)

Aspecto **cuadrado 1:1**, sujeto centrado, **fondo transparente** (PNG con alpha). El
avatar se renderiza con `preserveAspect` en un box cuadrado del HUD.

```
<<STYLE base — pegar textual desde ART_NEEDS §Fase 2>>

<<variante de mundo — WORLD A (medieval) o WORLD B (cyberpunk), textual desde ART_NEEDS §Fase 2>>

SUBJECT: <un solo personaje, cuerpo completo de la cabeza a los pies; describir criatura,
pose, props; tono toy-like handmade>. Optional subtle <type-color> crayon accent without
overriding the world palette.

TECHNICAL: single character, full body head-to-toe, centered with generous empty margin
all around. Facing slightly LEFT (it faces the hero on the left of the screen).
TRANSPARENT background (PNG with alpha, no scene, no ground). Square 1:1 composition,
target 512x512 px (bosses 768x768). Clean silhouette readable at small size.
```

**Ruta/asignación:** `Assets/Art/Sprites/Enemies/<Nombre>/A/Idle/personaje_<nombre>.png`
→ campo `avatar` del `EnemyDefinition`.

---

## Plantilla B — Cara de carta (ilustración)

Ilustración **vertical 2:3 (512×768)**, **fondo transparente**, un solo sujeto pensado
para una ventana vertical dentro del marco de la carta. Caras de **combate** (lados de
duales) llevan variante de mundo fija; caras de **draft** van **world-neutral
color-keyed** (sin bloque de mundo; paleta monocromática del tipo).

```
<<STYLE base — pegar textual desde ART_NEEDS §Fase 2>>

<<para combate: variante de mundo (WORLD A/B). Para draft: bloque TYPE COLOR del tipo
(p.ej. "TYPE COLOR — RED (Rojo): render the WHOLE illustration in a monochromatic
scarlet crayon palette … WORLD-NEUTRAL: do NOT theme it medieval or cyberpunk …")>>

SUBJECT: <motivo de la carta — un arma en slash para ataque, un escudo para guardia,
un libro/visor para robar cartas; un solo sujeto, sin escena>.

TECHNICAL: single subject, centered, composed for a TALL VERTICAL WINDOW inside a card
frame — NOT a full-page scene, NOT landscape. Generous empty margin around the subject.
TRANSPARENT background (PNG with alpha, no ground, no scenery). Vertical 2:3 composition,
target 512x768 px. Clean silhouette, readable as a small card illustration.
```

**Ruta/asignación:** `Assets/Art/Sprites/Cards/carta_<id>.png` → campo `Art` del
`CardDefinition` (el `<id>` = `Id` de la carta).

---

## Plantilla C — Fondo de panel / escena (evento, tienda, hoguera, mapa, NewRun)

**16:9, 1920×1080, SIN alpha** (fondo opaco), estirado a full-panel. Lleva UI encima en
casi toda la superficie → **centro y zonas de texto calmas y oscuras, interés visual en
los bordes/esquinas**, vignette suave para que el texto claro lea. Si el fondo se reusa
entre mundos (caso de eventos multidim: un solo sprite para A y B) → **dual-world
neutral** (fundir ambas paletas), sin comprometerse con un mundo.

```
<<STYLE base — pegar textual desde ART_NEEDS §Fase 2>>

<<si el panel es de un mundo fijo: variante WORLD A/B textual desde ART_NEEDS §Fase 2.
Si se muestra en ambos mundos (evento multidim): bloque DUAL-WORLD NEUTRAL fundiendo
las dos paletas canónicas — earthy medieval (browns/charcoal/dried-blood/candle-amber)
fading into cyberpunk neon (cyan/magenta/violet/acid-green), mismo estilo crayon-and-
cardboard; do NOT commit to one world>>

SUBJECT: <el escenario del panel — fragua/banco de trabajo, relicario sobre pedestal,
mensajero en una encrucijada, mostrador de tienda, etc.; sin personaje protagónico si no
hace falta>.

TECHNICAL: scene background, NO transparency (full opaque). Horizontal 16:9, target
1920x1080 px. COMPOSITION FOR UI OVERLAY: keep the CENTER and upper-third calm, dark and
low-detail (title + body text sit there) and the lower half quiet (buttons sit there);
push the visual interest toward the lower corners and edges. Soft vignette darkening so
light UI text stays readable.
```

**Ruta/asignación (eventos):** `Assets/Art/Events/fondo_<id>.png` → campo
`backgroundSprite` del `EventDefinition` (el menú `Roguelike > Setup Event Config` lo
asigna por convención de nombre). Otros paneles: ver su SO en ART_NEEDS.

---

## Mantenimiento

- Si aparece un **tipo de asset nuevo** (no avatar/carta/fondo), agregá una plantilla acá.
- Los prompts **concretos** no se guardan: se regeneran con `/art-prompt <slot>`.
- El **estado de cada slot** (hecho/falta) se trackea en [ART_NEEDS.md](ART_NEEDS.md), no acá.
