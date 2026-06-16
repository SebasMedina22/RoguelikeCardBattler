# Spec — Sub-PR 3E: NewRunScene (selección de tipos + draft de carta dual + arranque de run)

> **ESTADO: IMPLEMENTADO — PR #97 (2026-06-04).**

> Generado en `modo:diseno` el 2026-06-04. Todas las decisiones cerradas — spec
> implementable sin tocar archivos protegidos. El prompt de handoff para
> `modo:implementacion` está al final.

## Origen
GDD §5 (Mazo inicial), GOLDEN_RULES §3/§5, DD-002, DD-020. Roadmap M3 Sub-PR 3E.
Penúltima sub-tarea de M3 (cierra 3F después).

## Objetivo
Insertar una escena dedicada entre MainMenu y RunScene donde el jugador arma su
build inicial: elige 2 tipos elementales (uno por mundo), draftea su carta
especial dual filtrada por esos tipos, y confirma para arrancar la run. Esto
convierte `RunState.PlayerWorldAType/BType` de hardcode (Rojo/Amarillo) en
elección real, que alimenta la efectividad defensiva del combate y el filtro de
stock de la Tienda.

## Comportamiento esperado (flujo del jugador)
1. MainMenu → **Play** → carga **NewRunScene** (hoy carga RunScene directo).
2. **Paso 1 — Tipos.** Dos columnas (Mundo A / Medieval, Mundo B / Cyberpunk). El
   jugador elige un tipo elemental para cada mundo. El tipo elegido en A se
   deshabilita en la columna de B (deben ser distintos). **Continuar** se habilita
   con dos tipos distintos. Botón **Volver al menú**.
3. **Paso 2 — Draft (Componer).** Se muestran 3 caras filtradas por el tipo del
   Mundo A y 3 caras filtradas por el tipo del Mundo B. El jugador elige una cara
   de cada columna; se compone el dual en runtime. Cartas entran con fade/slide
   desde su lado de mundo (A izquierda, B derecha). Texto de peso ("Esta carta te
   acompañará todo el run"). Botón **Volver** (Paso 1) y **Volver al menú**.
4. **Paso 3 — Confirmar.** Resumen (tipos + carta compuesta). Sonido al confirmar.
   **Comenzar run** → escribe la selección en `RunState` y carga RunScene. Botón
   **Volver** (Paso 2).
5. RunScene arranca: el mazo inicial incluye la carta drafteada; combates aplican
   efectividad defensiva según los tipos; la Tienda filtra su stock por ellos.

## Sistemas afectados
- **Escenas / flujo:** nueva NewRunScene entre MainMenu y RunScene. Cambia el
  destino de `MainMenuController.OnPlayClicked`.
- **RunState (datos):** los tipos ya existen como campos; se agrega el transporte
  de la carta drafteada al mazo inicial (`PendingStarterCard`).
- **Mazo inicial:** la carta drafteada se inyecta en `InitializeDeck`.
- **UI/feel:** controller scene-owned nuevo, construido en runtime (sin setup
  manual), patrón = `MainMenuController` (intro sequence) + `UIAnimationHelper` /
  `AudioManager`.
- **Combate / Tienda:** NO se tocan; solo consumen los tipos ya elegidos.
- **Archivos protegidos:** **Ninguno.** Todo fuera de combate.

## Archivos a crear
- `Assets/Scenes/NewRunScene.unity` — escena con un GameObject `NewRunController`
  (+ agregar a Build Settings).
- `Assets/Scripts/Run/NewRun/NewRunController.cs` — controller scene-owned. Máquina
  de 3 pasos, UI en runtime, feel. Espejo estructural de `MainMenuController`.
- `Assets/Scripts/Run/NewRun/NewRunConfig.cs` — SO: pool de caras para el draft,
  tipos seleccionables, parámetros de feel. Espejo de `ShopConfig`.
- `Assets/Scripts/Run/NewRun/StarterDraft.cs` — helpers static puros
  (`BuildDraftOptions`, `ComposeDualCard`), testeables sin UI. Espejo de
  `ShopNodeController.BuildStock`.
- `Assets/Editor/NewRunConfigSetup.cs` — menú `Roguelike > Setup New Run Config`:
  crea `NewRunConfig.asset` idempotente + caras placeholder cubriendo los 6 tipos
  (≥3 por tipo). Espejo de `ShopConfigSetup`.
- `Assets/Tests/EditMode/NewRunTests.cs` — tests EditMode.

## Archivos a modificar
- `Assets/Scripts/Menu/MainMenuController.cs` — `OnPlayClicked` carga
  `"NewRunScene"` en vez de `"RunScene"` (sigue llamando `session.ResetForNewRun()`
  antes). Agregar `NewRunScene` a la validación de Build Settings.
- `Assets/Scripts/Run/RunState.cs` — agregar `CardDeckEntry PendingStarterCard`
  (runtime-only, no serializado) + reset a null en `Reset()`. En `InitializeDeck`,
  tras copiar `starterDeck`, si `PendingStarterCard != null` agregarla clonada al
  mazo (la "10ª carta" del GDD §5) sin romper el guard `Deck.Count == 0`.
- **Build Settings** — incluir `NewRunScene` (si falta, MainMenu deshabilita Play,
  igual que hoy con RunScene/BattleScene).

## Contratos

### Datos
**`NewRunConfig` (SO):**
- `List<CardDefinition> draftFaces` — caras single tipadas. El pool placeholder
  debe cubrir los 6 tipos con **≥3 caras por tipo** para que cualquier elección
  rinda 3 opciones por columna.
- `List<ElementType> selectableTypes` — default: los 6 no-None.
- `int optionsPerWorld = 3`.
- `(opcional) AudioClip confirmClip`.

**`RunState`:**
- `CardDeckEntry PendingStarterCard { get; set; }` — runtime-only, reset a null en
  `Reset()`. La escribe `NewRunController` al confirmar; la consume
  `InitializeDeck`.

### APIs públicas (static puro, testeable sin UI)
- `StarterDraft.BuildDraftOptions(NewRunConfig config, ElementType typeA, ElementType typeB, int seed)`
  → `{ List<CardDefinition> worldAOptions, List<CardDefinition> worldBOptions }`.
  Filtra `draftFaces` por `ElementType == typeA` / `== typeB`, baraja Fisher-Yates
  con RNG por seed, toma `optionsPerWorld` de cada uno. **Si un tipo tiene menos
  caras que `optionsPerWorld` → devuelve las disponibles + `log`** (política
  "no silent caps").
- `StarterDraft.ComposeDualCard(CardDefinition sideA, CardDefinition sideB)` →
  `DualCardDefinition` runtime vía `InitRuntimeSides`.

### Mutación de estado
- `NewRunController.ApplySelection()` — única vía que muta `RunState`. Se llama
  SOLO al confirmar: escribe `PlayerWorldAType/BType` + `PendingStarterCard =
  CardDeckEntry.CreateDual(composedDual)`. Garantiza "cancelar no deja estado
  sucio".

### Eventos
Ninguno nuevo en TurnManager. NewRunScene vive fuera de combate.

## Reuso
- `UIAnimationHelper` (FadeIn, ScaleIn, SlideIn, Punch) — feel de entrada de cartas
  y botones (igual que la intro de MainMenu).
- `AudioManager.Instance.PlaySFX(ClickSFX)` para clicks; clip de confirmación vía
  `NewRunConfig.confirmClip` (o reusar uno existente como placeholder).
- `DualCardDefinition.InitRuntimeSides` — compone el dual en runtime sin
  SerializedObject (ya usado por el flujo de upgrade de la Hoguera).
- `MainMenuController` (BuildUI / EnsureEventSystem / CreateCanvas / CreateButton)
  como patrón de construcción de UI runtime.
- `ShopNodeController.BuildStock` + `ShopConfigSetup` + `ShopTests` como patrón de
  helpers static puros + editor setup idempotente + tests sin UI.
- `EntryMatchesPlayerTypes` / `TypeAllowed` (filtro por tipo de la Tienda) como
  referencia del filtrado.

## Casos de prueba (EditMode)
1. `BuildDraftOptions`: `worldAOptions` (3) todas con `ElementType == typeA`;
   `worldBOptions` (3) todas con `== typeB`.
2. Determinismo: misma seed → mismas opciones; seeds distintas → (probable)
   distintas.
3. `ComposeDualCard` → `GetSide(WorldSide.A) == caraA`, `GetSide(WorldSide.B) ==
   caraB`.
4. Carta compuesta entra al mazo: tras `ApplySelection` + `InitializeDeck`, el
   `Deck` contiene la carta drafteada (count = base + 1).
5. `ApplySelection` escribe `PlayerWorldAType/BType` con los tipos elegidos.
6. Restricción distinta: la UI no permite B == A (Continuar deshabilitado hasta
   dos tipos distintos).
7. "Sin estado sucio": construir el controller y NO confirmar → `RunState`
   conserva tipos default y `PendingStarterCard == null`.
8. Guard de pool insuficiente: tipo con menos caras que `optionsPerWorld` →
   devuelve las disponibles sin crashear (+ log).

## Validación manual (Unity)
1. MainMenu → Play → carga NewRunScene (no RunScene directo).
2. Paso 1: elegir tipos por mundo; el tipo de A se deshabilita en B; Continuar se
   habilita con ambos distintos.
3. Paso 2: opciones filtradas correctas; cartas entran con fade desde su lado;
   texto de peso visible.
4. Paso 3: sonido al confirmar; Comenzar run → RunScene.
5. En RunScene: combate muestra el dual drafteado en mano; abrir Tienda → stock
   filtra por los tipos elegidos.
6. Volver al menú en cualquier paso → re-Play arranca limpio.
7. Zero console errors. Verificación de UI por diagnóstico de código (el game view
   no repinta sin foco vía CLI).

## Decisiones cerradas (2026-06-04)
1. **Modelo del draft = Componer**: 3 caras filtradas por tipo A + 3 por tipo B; el
   jugador elige 1 de cada → se compone el dual en runtime con `InitRuntimeSides`.
2. **Re-tipado starter = solo tipos de mundo.** 3E setea `PlayerWorldAType/BType`
   (defensa + filtro Tienda + filtro draft) + carta drafteada. El re-tipado de las
   cartas afín del starter (mazo inicial completo GDD §5) se **difiere**.
3. **Restricción de tipos = distintos, 6 disponibles.** El tipo de B no puede
   igualar al de A.
4. **Contenido del pool = editor menu placeholder** (`NewRunConfigSetup.cs`, espejo
   `ShopConfigSetup`).
5. Escena dedicada (no canvas en MainMenu) — roadmap M3.
6. DD-020: carta especial dual **filtrada** por los 2 tipos elegidos.
7. La carta drafteada se inyecta vía `RunState.PendingStarterCard` consumido en
   `InitializeDeck`.
8. Botón "Volver al menú" en cada paso; `RunState` solo muta en confirmar.

## Alternativas consideradas
- **Inyectar la carta drafteada poblando `RunState.Deck` directo en NewRunScene** —
  descartada: `InitializeDeck` guarda en `Deck.Count == 0`, así que poblar antes
  haría que el starter base no se cargue. De ahí el transporte vía
  `PendingStarterCard`.
- **Modelo Pick-1-de-6 duales prearmados** — descartado a favor de Componer
  (encaja con "elige una combinación entre 6 opciones, 3 por mundo" del GDD y liga
  el dual a ambos tipos).
- **NewRunScene como canvas dentro de MainMenu** — descartada por diseño (cambio de
  contexto/atmósfera).

## Estimación
- **Complejidad:** media. Sin archivos protegidos, patrón muy establecido (espejo
  3C/3D/MainMenu).
- **Sub-tareas:** (1) NewRunConfig + editor setup + contenido placeholder;
  (2) StarterDraft helpers + tests; (3) NewRunController UI + 3 pasos + feel;
  (4) wiring MainMenu→NewRun→Run + inyección de mazo; (5) validación Unity.
- **Riesgos:** (a) Build Settings — olvidar agregar NewRunScene rompe el Play;
  (b) `ResetForNewRun` corre en MainMenu ANTES de NewRunScene → verificar que ningún
  otro reset pise los tipos elegidos entre NewRunScene y RunScene; (c) el pool
  placeholder debe cubrir los 6 tipos con ≥3 caras o el draft queda corto.

---

## Prompt de handoff para `modo:implementacion`

> Copiar y pegar el bloque siguiente como mensaje nuevo (en una conversación
> fresca, por el costo de Opus con historial largo) para arrancar la implementación.

```
modo:implementacion

Implementá el Sub-PR 3E (NewRunScene) de M3. El spec cerrado está en
`Docs/dev/specs/m3_3e_newrun_spec.md` — leelo completo antes de tocar código;
todas las decisiones de diseño ya están cerradas, no abras ninguna.

Setup de branch (PR #96 / 3D ya mergeado a main):
  git fetch --all --prune
  git checkout -b feat/m3-sub-e-newrun origin/main

Qué construir (resumen; el detalle y los contratos están en el spec):
- Escena `NewRunScene` entre MainMenu y RunScene con `NewRunController` (3 pasos:
  elegir 2 tipos distintos uno por mundo → draftear carta dual componiendo 1 cara
  A + 1 cara B filtradas por tipo → confirmar y cargar RunScene).
- `NewRunConfig` SO + `NewRunConfigSetup.cs` (menú idempotente con caras
  placeholder cubriendo los 6 tipos, ≥3 por tipo) — espejo de ShopConfig/Setup.
- `StarterDraft.cs` helpers static puros (`BuildDraftOptions`, `ComposeDualCard`)
  — espejo de ShopNodeController.BuildStock.
- `RunState.PendingStarterCard` + inyección en `InitializeDeck`.
- `MainMenuController.OnPlayClicked` → carga "NewRunScene"; agregarla a Build
  Settings y a la validación.
- `NewRunTests.cs` EditMode (8 casos en el spec).

Reglas no negociables:
- NO tocar archivos protegidos (TurnManager, ActionQueue, PlayerCombatActor). 3E
  no los necesita.
- No manual editor setup: todo se auto-crea en runtime / por el menú editor.
- Re-tipado de cartas afín del starter está FUERA de scope (decisión cerrada #2).
- Feel: cartas entran con fade/slide desde su lado de mundo (UIAnimationHelper),
  texto de peso, sonido al confirmar (AudioManager / NewRunConfig.confirmClip).
- RunState solo muta al confirmar (ApplySelection) — cancelar no deja estado sucio.

Validación obligatoria antes de cerrar (vía Unity-MCP ai-game-developer, modo
local v0.77.3 — NO actualizar a 0.79.0; `npx unity-mcp-cli run-tool`):
- Compilación limpia (zero console errors).
- `NewRunTests` EditMode en verde + suite completa sin regresiones.
- Correr el menú `Roguelike > Setup New Run Config` para crear el .asset y
  asignar `newRunConfig` en NewRunController/escena.
- Flujo end-to-end MainMenu → NewRunScene → RunScene; carta drafteada en mano en
  combate; Tienda filtra por los tipos elegidos.
- Verificar UI por diagnóstico de código, NO por screenshot del game view (no
  repinta sin foco vía CLI).

Al cerrar: actualizá `_roadmap.md` (checkboxes de 3E) y `_tech_snapshot.md`
(nuevos archivos/subsistema), commit + push + PR a main. Después de 3E queda solo
3F (mapa horizontal) para cerrar M3.
```
