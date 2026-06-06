# ART_NEEDS.md — Auditoría de arte (placeholders IA)

> **Qué es este archivo:** doc vivo que recoge TODO lugar del juego que consume o
> debería consumir un sprite, su estado actual, y si el código ya lo soporta. Es el
> checklist de integración de arte para ambos devs. Generado en la auditoría de
> arte post-M3 (2026-06-05, `modo:diseno`).
>
> **No es arte final.** Todo lo marcado `placeholder-IA` se cubre con imágenes
> generadas por IA como puente. El arte definitivo llegará después; estos son
> stand-ins para ver las pantallas vestidas.
>
> **Flujo del plan:** Fase 1 (esta auditoría) → Fase 2 (estilo madre, abajo) →
> Fase 3 (prompts por asset, sesión siguiente) → Fase 4 (import + asignar a SOs).

---

## Cómo leer la tabla

- **Estado actual** — qué se ve hoy en pantalla sin arte nuevo.
- **¿Código ya lo soporta?** — la distinción clave del plan:
  - ✅ **Drop-in**: el gancho (campo de sprite en el SO o lista en el componente)
    ya existe. Generar el arte → asignar → listo. Sin tocar código.
  - 🔶 **Mixto**: falta código ANTES de poder usar el arte (campo nuevo en un SO,
    render nuevo en una vista). El arte por sí solo no entra.
  - 🎨 **Solo arte / solo código**: casos especiales anotados en la fila.
- **Prioridad** — P1 (impacto visual alto, barato) · P2 (medio) · P3 (pulido / post).
- **Marca** — `placeholder-IA` = se genera con IA en Fase 3.

---

## Hallazgo de la auditoría (leer primero)

El barrido reveló que **el patrón placeholder-first de 3C/3D ya dejó mucho arte
asignado**. El panorama real es más chico de lo que asumía el plan:

**Ya tiene placeholder asignado en su SO (✅ no requiere acción, salvo pulido):**

| Slot | Dónde | Confirmado |
|------|-------|------------|
| Fondo de combate parallax (5 capas × Mundo A/B = 10 sprites) | `CombatBackgroundTheme_Act1.asset` | sprites asignados en las 5 `LayerSprites` |
| Hoguera parallax (cielo / medio / fuego) | `CampfireConfig.asset` | `skySprite` / `midSprite` / `fireSprite` asignados |
| Tienda parallax (pared / estantería / mostrador) | `ShopConfig.asset` | `wallSprite` / `shelfSprite` / `counterSprite` asignados |
| Íconos de Retazos (23) | `RelicDefinition.Icon` | 23 PNG en `Assets/Art/Relics/Icons/` |
| Animación del héroe (Mundo A: idle + 14 frames de ataque) | `CombatUIController.heroIdleFrames` / `heroAttackFrames` | PNGs en `Assets/Art/Sprites/Hero/A/` |
| Avatar del Slime (Mundo A idle) | `EnemyDefinition.Avatar` (WeakSlime) | `Assets/Art/Sprites/Enemies/Slime/A/Idle/` |

> Lo de arriba **no se vuelve a generar** en Fase 3 a menos que Sebastián quiera
> subir la calidad. Lo que sigue es lo que **falta** o **requiere código**.

---

## Tabla de assets faltantes / pendientes

### Combate

| # | Asset | Dónde se usa (archivo/SO) | Estado actual | Proporción / formato | ¿Código lo soporta? | Prioridad | Marca |
|---|-------|---------------------------|---------------|----------------------|---------------------|-----------|-------|
| C1 | Avatar enemigo — Goblin | `EnemyDefinition.Avatar` (Goblin.asset) | sin sprite → no se ve avatar | ~512×512, PNG transparente | ✅ Drop-in (campo `avatar`) | P1 | `placeholder-IA` |
| C2 | Avatar enemigo — SkeletonWarrior | `EnemyDefinition.Avatar` (SkeletonWarrior.asset) | sin sprite | ~512×512, PNG transparente | ✅ Drop-in | P1 | `placeholder-IA` |
| C3 | Avatar enemigo — DarkMage | `EnemyDefinition.Avatar` (DarkMage.asset) | sin sprite | ~512×512, PNG transparente | ✅ Drop-in | P1 | `placeholder-IA` |
| C4 | Avatar boss — BossAct1 (UNIT-RB7 / Costura maldita) | `EnemyDefinition.Avatar` (BossAct1.asset) | sin sprite | ~768×768, PNG transparente | ✅ Drop-in **pero** campo es 1 sprite → el boss es dual-mundo (GDD §boss) y hoy NO puede tener avatar A + B distintos sin código. Generar **una** versión por ahora; marcar como deuda. | P1 | `placeholder-IA` |
| C5 | Animación del héroe — Mundo B (idle + ataque) | `CombatUIController` (listas de frames) | **arte ya existe** en `Hero/B/`, pero el código usa una sola lista de frames (no conmuta héroe por mundo) | frames PNG transparentes, mismo tamaño que A | 🔶 Mixto: arte listo; falta código para swap de frames del héroe en world switch | P2 | (arte existe) |
| C6 | Frames de "hit" del enemigo (flash de golpe) | `CombatUIController.enemyHitFrames` | lista vacía; sin animación de impacto | frames PNG transparentes | ✅ Drop-in (lista opcional) | P3 | `placeholder-IA` |
| C7 | Cara de carta (ilustración) | `CardDefinition.Art` + `CardHandView` | campo `Art` en `CardDefinition` + render en `CardHandView` (región superior, `preserveAspect`); fallback a texto sin arte | 512×768 vertical (2:3), PNG con alpha; ruta `Assets/Art/Sprites/Cards/carta_<id>.png` | ✅ **Drop-in** (código hecho, spec `art_c7_card_art_spec.md`): asignar el sprite al campo `Art` → se renderiza solo. Conmuta el lado activo de las duales en vivo. **✔ CERRADO** (Fase 4 hecha): 24 PNGs en `Assets/Art/Sprites/Cards/` asignados al campo `Art` de los 6 SOs de combate + 18 caras de draft; validado en Play (conmutación A/B). Layout de carta ampliado para el arte vertical. | P1 | ✔ hecho |
| C8 | Tinte por tipo elemental | `CardHandView` (prefijo `[Tipo]`) + HUD (labels de tipo) + NewRunScene (botones de tipo) | ✔ **HECHO** (branch `feat/element-type-color-tint`): helper `ElementTypeColors` (fuente única de verdad) tinta los botones de tipo de NewRunScene, los labels de tipo del HUD (jugador/enemigo) y el prefijo `[Tipo]` de las cartas | — (no es arte) | ✅ **Cerrado** — solo código, sin IA | P1 | (sin arte) |

### Mapa del run

| # | Asset | Dónde se usa | Estado actual | Proporción / formato | ¿Código lo soporta? | Prioridad | Marca |
|---|-------|--------------|---------------|----------------------|---------------------|-----------|-------|
| M1 | Íconos de nodo por tipo (Combate / Élite / Tienda / Hoguera / Boss) | `RunMapNodeView` | nodos = recuadro de color + texto; `_bgImage` usa whiteSprite | íconos cuadrados ~128×128, PNG transparente | 🔶 Mixto: agregar sprite-por-tipo en `RunMapNodeView` (hoy sólo tinta color) | P2 | `placeholder-IA` |
| M2 | Fondo del mapa (horizontal) | `RunFlowController._mapPanel` | panel transparente (color 0,0,0,0); fondo vacío | banda ancha horizontal (scroll izq→der), p.ej. 3000×1080, tileable horizontal | 🔶 Mixto: agregar Image de fondo al `_mapPanel` (+ posible campo de config) | P2 | `placeholder-IA` |

### Tienda

| # | Asset | Dónde se usa | Estado actual | Proporción / formato | ¿Código lo soporta? | Prioridad | Marca |
|---|-------|--------------|---------------|----------------------|---------------------|-----------|-------|
| S1 | Vendedor / personaje de la tienda | `ShopConfig` (no existe campo) | sin vendedor; sólo parallax de fondo | personaje, ~512×768, PNG transparente | 🔶 Mixto: agregar `vendorSprite` a `ShopConfig` + render en `ShopNodeController` | P2 | `placeholder-IA` |

### NewRunScene (arranque de run)

| # | Asset | Dónde se usa | Estado actual | Proporción / formato | ¿Código lo soporta? | Prioridad | Marca |
|---|-------|--------------|---------------|----------------------|---------------------|-----------|-------|
| N1 | Fondo de la pantalla de arranque | `NewRunConfig` (no existe campo) / `NewRunController` | UI runtime sobre fondo liso | 16:9, 1920×1080, PNG/JPG | 🔶 Mixto: agregar campo bg a `NewRunConfig` + render (o reusar un fondo genérico) | P2 | `placeholder-IA` |
| N2 | Caras de carta del draft | `NewRunController.BuildDraftColumn` (usa `CardDefinition` de `draftFaces`) | render de arte cableado (mismo criterio que C7); fallback a texto sin arte | igual que C7 (512×768 vertical, PNG con alpha) | ✅ **Drop-in** — **desbloqueado por C7** (campo `Art` en `CardDefinition`): asignar el sprite a la cara → el botón del draft muestra la imagen. **✔ CERRADO** (Fase 4 hecha): las 18 caras world-neutral asignadas y validadas en NewRunScene (los 6 tipos muestran sus 3 caras con arte). | P2 | ✔ hecho |

### HUD (combate y mapa)

| # | Asset | Dónde se usa | Estado actual | Proporción / formato | ¿Código lo soporta? | Prioridad | Marca |
|---|-------|--------------|---------------|----------------------|---------------------|-----------|-------|
| H1 | Íconos de HUD (energía / oro / bloqueo / intent) | `CombatHudView` / `CardHandView` / `RunFlowController` (status) | todo texto | ~64×64, PNG transparente | 🔶 Mixto: render de Image junto a cada label | P3 | `placeholder-IA` |

---

## Resumen: drop-in vs dependiente de código

**Total de slots pendientes catalogados: 13** (C1–C8, M1–M2, S1, N1–N2, H1).

- ✅ **Drop-in puro (genera → asigna, sin código): 4**
  → C1, C2, C3 (avatares Goblin/Skeleton/DarkMage), C6 (hit frames enemigo).
  → C4 (avatar boss) es drop-in técnicamente pero con la deuda de "1 solo sprite
    para un boss dual-mundo".
- 🔶 **Mixto (requiere código antes del arte): 8**
  → C5 (héroe Mundo B — arte ya existe, falta swap), C7 (cara de carta — la grande),
    M1 (íconos de nodo), M2 (fondo de mapa), S1 (vendedor), N1 (fondo NewRun),
    N2 (caras de draft, bloqueado por C7), H1 (íconos de HUD).
- 🔶 **Solo código, sin arte: 1**
  → C8 (tinte por tipo elemental) — ganancia barata, no necesita IA.

**Dependencia crítica (la que ordena todo):** **C7** (sprite en `CardDefinition` +
render en `CardHandView`). Desbloquea las cartas de combate Y las caras de draft de
NewRun (N2). Es la pieza de código más grande de toda la integración de arte.

---

## Propuesta de prioridad para la sesión de prompts (Fase 3)

Ordenada por **impacto visual / costo**:

1. **Avatares de enemigos (C1–C4)** — drop-in puro, 4 prompts, llenan el combate
   de golpe. Empezar acá: máximo retorno, cero código.
2. **Tinte por tipo (C8)** — no necesita prompt; es un mini-tarea de código que
   puede ir en el primer PR de arte. Anotar para `modo:implementacion`.
3. **Cara de carta (C7)** — definir el PR de código primero (campo + render); el
   prompt de "ilustración de carta" se diseña en paralelo pero no se asigna hasta
   tener el campo.
4. **Mapa (M1 íconos de nodo, M2 fondo)** y **Tienda (S1 vendedor)** — prompts +
   PRs de código chicos.
5. **NewRun (N1 fondo, N2 caras)** — N1 independiente; N2 espera a C7.
6. **Pulido (C5 héroe Mundo B, C6 hit frames, H1 íconos HUD)** — P3, al final.

> Nota: lo ya asignado (fondos de combate, Hoguera, Tienda parallax, Retazos, héroe
> A, Slime) se puede re-generar en Fase 3 SÓLO si se quiere subir calidad — no es
> bloqueante para vestir las pantallas que hoy están desnudas.

---

## Fase 2 — Estilo madre (bloque reutilizable para todos los prompts)

> Este bloque se **pega al inicio de cada prompt** de Fase 3 para garantizar
> coherencia visual entre todos los assets. Define la estética base (handmade de los
> dos hermanos) y las dos variantes de mundo. Abajo, en inglés (los generadores de
> imágenes responden mejor), con notas en español.

### Bloque base (pegar siempre)

```
STYLE: Childlike handmade arts-and-crafts aesthetic — as if drawn and built by two
kids from cardboard, wax crayons, cut-out paper, magazine clippings, school glue and
sticky tape. Visible crayon strokes, uneven hand-cut paper edges, cardboard texture,
slightly wonky proportions, warm imperfect lines. Flat 2D, storybook/diorama feel.
Everything looks like a physical scrap pinned into a kids' imaginary world.
NOT digital-clean, NOT vector-smooth, NOT photorealistic, NO gradients-heavy 3D.
```

### Variante Mundo A — Medieval Oscuro (pegar para assets de Mundo A)

```
WORLD A — DARK MEDIEVAL: gloomy fairy-tale medieval theme rendered in the same
crayon-and-cardboard craft style. Muted earthy palette — deep browns, charcoal,
dried-blood reds, candle-amber highlights. Castles, torn banners, rusty armor,
stitched cloth monsters, torchlight. Spooky but still handmade and toy-like, never
gory-realistic.
```

### Variante Mundo B — Cyberpunk / Futurista (pegar para assets de Mundo B)

```
WORLD B — CYBERPUNK: neon-soaked futuristic theme rendered in the SAME crayon-and-
cardboard craft style. Palette — electric cyan, magenta, violet, acid green over
dark base; "neon" drawn as glowing crayon scribbles, circuits as taped foil and
marker lines, robots built from cardboard boxes and bottle caps. High-tech subjects
made of craft materials, never sleek digital chrome.
```

### Restricciones técnicas (anexar según el slot en Fase 3)

- **Transparencia:** avatares, vendedor, íconos, caras de personaje → fondo
  transparente (PNG con alpha). Fondos de escena → sin alpha.
- **Aspect ratio del slot:** respetar la columna "Proporción / formato" de la tabla.
- **Parallax / fondos de mapa:** tileable en horizontal cuando aplica (mapa, capas).
- **Íconos:** legibles a tamaño chico, silueta clara, un solo sujeto centrado.
- **Cartas (C7):** ilustración pensada para una ventana vertical dentro del marco de
  la carta, no una escena de página completa.

---

## Mantenimiento

- Cuando un slot se cubre (arte generado + asignado al SO), marcarlo aquí (✔) o
  moverlo a la tabla de "ya tiene placeholder".
- Cuando se cierra una tarea de código mixta (ej. C7), actualizar la columna
  "¿Código lo soporta?" a ✅ Drop-in.
- Las decisiones de diseño cerradas que afecten arte (paleta, estilo) viven acá;
  cambios de fondo se discuten con Sebastián.
