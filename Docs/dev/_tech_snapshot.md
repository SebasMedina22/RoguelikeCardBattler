# Technical Snapshot — RoguelikeCardBattler

> **Qué es este archivo:** foto point-in-time de la arquitectura técnica concreta
> del proyecto. Se actualiza cuando hay cambios estructurales (nuevo subsistema,
> nuevo módulo, cambio de stack, refactor importante).
>
> En `modo:implementacion` se lee OBLIGATORIAMENTE antes de cualquier cambio que
> afecte arquitectura o componentes críticos.
>
> **Última actualización:** 2026-06-16 — **SUB-PR 2 auditoría: red de tests pre-cirugía
> + cap de Estilo** (branch `feat/test-net-pre-surgery`). 4 archivos de test EditMode
> nuevos (suite **148 → 166**, archivos **18 → 22**): `StyleChargeTests` (T3 pre-block /
> T4 reset de estado + sangrado de counters R-6 / T7 cap a 5), `RelicAssetTests` (T5:
> los 23 `.asset` de Retazos — cada hook declarado es despachable sin error),
> `RelicGrantEffectsOnTurnManagerTests` (T6: 10 casos de Grant* sobre TurnManager real),
> `NeutralCardDamageTests` (T9: carta None = 90% de daño, DD-002). **Fix D-B en el
> protegido `TurnManager.IncrementStyleCharges`** (+8 líneas): clamp `else if
> (_styleCharges > 5) _styleCharges = 5;` — antes solo `SetStyleChargesForTest`
> clampaba; el path de producción acumulaba >5 vía Retazos con un switch bonus pendiente
> (`TurnManager.cs` ahora **~1107 LOC**). Validado: compilación limpia, EditMode **166/166**.
>
> **Última actualización previa:** 2026-06-16 — **SUB-PR 5 auditoría: números y estado**
> (branch `docs/numbers-and-state`). Refresh mecánico de cifras desviadas, sin
> cambios de código: **108** scripts C# en `Assets/Scripts` (no 53); `TurnManager.cs`
> **~1098 LOC** (no 735/~960 contradictorios); LOC de vistas de combate corridos
> (`CombatUIController` 752, `CardHandView` 501, `CombatHudView` 344,
> `PlayerCombatActor` 246); suite EditMode **148/148** (18 archivos, +`CombatEndSyncTests`);
> `gh` CLI **presente** (2.92.0) — PRs vía `gh pr create`; **CI parqueado** (GameCI
> bloqueado por licencias Entitlement de Unity 6; `tests.yml` commiteado pero
> DESACTIVADO, `workflow_dispatch`); purgada la sección "Restricciones conocidas"
> de los 3 gaps de TurnManager (PhaseBased / EffectType.Heal / CalculateIntentValue)
> **resueltos en M2 Sub-PR D**. HP base = 60 (D-C).
>
> **Última actualización previa:** 2026-06-14 — **SUB-PR 1 auditoría: fix de sincronización
> fin-de-combate + retry/reset** (branch `feat/fix-combat-end-sync`, spec
> `Docs/dev/specs/fix_combat_end_hp_sync_spec.md`, Opción B). Raíz del cluster H1-H4:
> RunState tenía HP stale cuando corrían los hooks OnCombatEnd. **Fix:**
> `TurnManager.DispatchCombatEnd` sincroniza `RunState.PlayerCurrentHP/MaxHP =
> _player.*` ANTES del dispatch (+9 líneas aditivas en el protegido, aprobadas y
> acotadas) → RunState autoritativo; los Retazos mutan/leen HP fresco (H1/H2).
> `BattleFlowController` deja de re-pisar HP: se extrajo el seam público
> `ApplyCombatResult(session, victory)` (flags/ActoCompleted/drops, sin HP ni
> `LoadScene`) y `ReportOutcome` = `ApplyCombatResult` + tail de transición.
> `RunState.PrepareForRetry()` (HP a full, limpia flags) recablea el botón Reintentar
> (H3). Botones Derrota/Acto → `ReturnToMainMenu()` (carga `MainMenuScene` con guard),
> eliminado el reset in-place + TODO `:701` (H4). Tests `CombatEndSyncTests.cs` (T1
> no-pisada de heal vía seam; T2 retry restaura HP jugable).
> **Validado:** compilación limpia, EditMode **148/148** (146 previos + T1/T2),
> H1-H4 verificados en Play.
>
> **Última actualización previa:** 2026-06-09 — **Pulido pre-M4 tinte tipo en
> selectores (PR #107; #104 fue el PR de docs del backlog)**. `ElementTypeColors` gana `TypePrefix(ElementType)` — token
> rich-text `<color=#hex>[Tipo]</color>` sin espacio final, fuente única de
> verdad del patrón que antes vivía inline en `CardHandView`. Aplicado en 3
> sitios: `CampfireNodeController.BuildCardSelectLabel`, `ShopNodeController.
> BuildCardSelectLabel` y `ShopNodeController.CreateItemButton` (render del
> stock). Refactor behavior-preserving en `CardHandView.BuildCardLabel` (consume
> `TypePrefix` en vez del inline). `ElementTypeColorsTests.cs` ampliado: **146/146**
> (142 previos + 4 nuevos: TypePrefix None/token/hex/all-types). `ShopTests` y
> `CampfireTests` siguen intactos (no se tocó `BuildStock`).
> **Validado:** compilación limpia, EditMode **146/146** (142 previos + 4 nuevos).
>
> **Última actualización previa:** 2026-06-05 — **Auditoría de arte, slot C7 (cara de carta,
> habilitación de CÓDIGO)** en branch `feat/c7-card-art` (spec
> `Docs/dev/specs/art_c7_card_art_spec.md`). `CardDefinition` gana el campo
> `[SerializeField] Sprite art` + getter `Art` (paralelo a `EnemyDefinition.Avatar`),
> propagado en `SetDebugData(... , newArt = null)` y en `CreateUpgradedClone()` (el clon
> de upgrade conserva el arte base — D3). `DualCardDefinition`/`CardDeckEntry` heredan
> el campo gratis (cada lado/cara ES un `CardDefinition`). Render: `CardHandView` crea
> un `Image` hijo "Art" en la región superior del botón (anclas `0.06,0.42`–`0.94,0.96`,
> `preserveAspect`, sin raycast) y un helper idempotente `ApplyCardArtLayout` baja el
> texto a la franja inferior cuando hay arte y lo devuelve a full-card cuando no
> (fallback = look actual, cero regresión); el sprite del lado activo se sincroniza
> por-frame en `SyncHandButtons` vía `GetActiveCardDefinition` → **conmuta en vivo** al
> cambiar de mundo sin reconstruir la mano. `NewRunController.BuildDraftColumn` cablea
> el mismo render en las caras del draft (cierra **N2**; sin arte = texto como hoy). C8
> no se toca: el arte convive con el tinte por tipo. Nuevo
> `Assets/Tests/EditMode/CardArtTests.cs` (5 casos: default null, set/omit en
> `SetDebugData`, persistencia en el clon de upgrade, herencia dual por mundo, resolución
> vía `CardDeckEntry`). **Validado:** compilación limpia (zero console errors), EditMode
> **142/142** (137 previos + 5 nuevos). Cubre C7 de `ART_NEEDS.md` y desbloquea N2.
> **Fase 4 (integración de arte) hecha en la misma rama:** 24 PNGs placeholder en
> `Assets/Art/Sprites/Cards/` (`carta_<id>.png`, Sprite/Single) asignados al campo
> `Art` de los 6 SOs de combate + 18 caras de draft (`NewRunFaces/`), vía script
> editor. Ajuste de layout C7: carta más alta (`HandCardHeightBase` 130→175, min
> 96→125) y franja de arte ampliada (anclas `0.06,0.36`–`0.94,0.98`) para que la
> ilustración vertical (2:3) se vea más grande. Validado en Play (BattleScene:
> conmutación A/B del arte; NewRunScene: los 6 tipos muestran sus 3 caras).
>
> **Última actualización previa:** 2026-06-05 — **Auditoría de arte, slot C8 (tinte por tipo
> elemental, solo código, sin IA)** en branch `feat/element-type-color-tint`. Nuevo
> `Assets/Scripts/Gameplay/Combat/ElementTypeColors.cs`: fuente única de verdad
> `ElementType→Color` (los 6 tipos son placeholders por color). API: `For(type)`
> (color canónico), `ReadableOnDark(type)` (levanta colores oscuros como Negro para
> texto legible sobre fondos oscuros), `ReadableTextOn(bg)` (tinta negra/blanca por
> luminancia), `Dim(color,factor)` (atenúa preservando alpha). Aplicado en 3 sitios
> de presentación (sin tocar lógica): `NewRunController.BuildTypeColumn` (fondo de los
> botones de tipo tintado por color + texto contrastado; dorado se reserva para
> "elegido", deshabilitado = color atenuado), `CombatHudView.Sync` (labels de tipo del
> jugador y del enemigo tintados), `CardHandView.BuildCardLabel` (prefijo `[Tipo]` de
> la carta coloreado vía rich text). Nuevo `Assets/Tests/EditMode/ElementTypeColorsTests.cs`
> (6 casos: opacidad, colores distintos por tipo, contraste de texto, levante de Negro
> sobre oscuro, brillantes intactos, Dim preserva alpha). **Validado:** compilación
> limpia, EditMode **137/137** (131 previos + 6 nuevos). Cubre C8 de `ART_NEEDS.md`.
>
> **Última actualización previa:** 2026-06-05 — M3 Sub-PR 3F (Mapa horizontal) **implementado
> y validado en Unity** en branch `feat/m3-sub-f-horizontal-map`. **Cierra M3.**
> Refactor de UX puro del mapa del run: scroll vertical → **scroll horizontal
> izquierda→derecha** (DD-005, start a la izquierda / boss a la derecha). El eje vive
> ENTERAMENTE en `RunMapView`: `GetNodePosition` ahora mapea `depth→X`
> (`x = LeftPadding + depth*ColumnSpacing`) e `índice→Y`
> (`y = -(index-(count-1)/2)*NodeRowSpacing`, apilado vertical centrado); el content
> se dimensiona por ANCHO (`contentWidth`, `sizeDelta=(W,0)`); `CreateScrollView`
> setea `ScrollRect.horizontal=true/vertical=false`, content pivot `(0,0.5)` anclado
> a la izquierda, y baja el techo del scroll a `anchorMax.y=0.72`. Constantes
> renombradas: `HorizontalSpacing→ColumnSpacing(200)`, `RowSpacing→NodeRowSpacing(120)`,
> `TopPadding→LeftPadding(80)`, `BottomPadding→RightPadding(80)`. `RunMapNodeView` y
> `RunMapEdgeView` sólo cambian su anchor `(0.5,1)→(0,0.5)` (1 línea c/u; animaciones
> y matemática de aristas son agnósticas al eje). **`RunMapGenerator` NO se tocó**
> (es agnóstico al eje — sólo asigna tipos/aristas; decisión cerrada del spec).
> `RunFlowController` ahora instancia un `RelicInventoryView` en una banda dedicada
> del `_mapPanel` (anchors 0.72–0.79, arriba del scroll y bajo el status) → fila de
> Retazos en el HUD del mapa, consistente con el HUD de combate; `Refresh` en
> `ShowMap()` y `Cleanup()` junto al de `_mapView` en las transiciones de escena.
> Nuevo `Assets/Tests/EditMode/RunMapGeneratorTests.cs` (5 casos: determinismo de
> topología, determinismo de enemigos, divergencia por seed, DAG válido, start/end
> forzados) — blinda "misma seed = mismo mapa" (no existía test del sistema de mapa).
> **Validado:** compilación limpia, EditMode **131/131** (126 previos + 5 nuevos),
> Play en RunScene sin errores/excepciones; diagnóstico de código confirma ScrollRect
> horizontal, content `pivot(0,0.5) sizeDelta(1160,0)`, start en x=80 / boss en x=1080,
> ramas del mismo depth apiladas y centradas (±60), RelicBar en su banda.
>
> **Última actualización previa:** 2026-06-04 — M3 Sub-PR 3E (NewRunScene) **implementado
> y validado en Unity** en branch `feat/m3-sub-e-newrun`. Nuevos:
> `Assets/Scripts/Run/NewRun/{NewRunController, NewRunConfig, StarterDraft}.cs`,
> `Assets/Editor/NewRunConfigSetup.cs` (menú `Roguelike > Setup New Run Config`),
> `Assets/Tests/EditMode/NewRunTests.cs`, `Assets/Scenes/NewRunScene.unity`.
> Pantalla de arranque de run entre MainMenu y RunScene: máquina de 3 pasos
> (elegir 2 tipos distintos uno por mundo → draftear carta dual componiendo 1 cara
> A + 1 cara B filtradas por tipo → confirmar). `NewRunController` es scene-owned,
> construye su UI en runtime (espejo estructural de `MainMenuController`); la lógica
> del draft vive en helpers static puros `StarterDraft.BuildDraftOptions(config,
> typeA, typeB, seed)` / `ComposeDualCard(sideA, sideB)` / `TypesValid` /
> `ApplySelectionToState` (testeables sin UI, espejo de `ShopNodeController.BuildStock`).
> El dual se compone en runtime vía `DualCardDefinition.InitRuntimeSides` (mismo
> mecanismo que el upgrade de la Hoguera). `RunState` gana `CardDeckEntry
> PendingStarterCard` (runtime-only, reset a null en `Reset()`): `NewRunController.
> ApplySelection` la escribe al confirmar y `InitializeDeck` la inyecta clonada
> tras el starter base (la "10ª carta" GDD §5, sin romper el guard `Deck.Count == 0`).
> `MainMenuController.OnPlayClicked` ahora carga `NewRunScene` (sigue llamando
> `ResetForNewRun` antes) y la validación de Build Settings la incluye.
> `NewRunConfig` SO: pool `draftFaces` (caras single tipadas, ≥3 por tipo cubriendo
> los 6), `selectableTypes`, `optionsPerWorld`, `confirmClip`. El menú editor crea
> el asset + 18 caras placeholder en `Assets/ScriptableObjects/Cards/NewRunFaces/`.
> **Importante:** `RunFlowController` ya **NO** llama `State.Reset` (cambió en el
> fix fin-de-combate, 2026-06-14): los botones Derrota ("Volver al mapa") y Acto
> completado ("Volver al Menú") delegan en `ReturnToMainMenu()` → cargan
> `MainMenuScene` con guard `IsSceneInBuild`. El reset de run nuevo lo posee
> exclusivamente `RunSession.ResetForNewRun` (MainMenu→Play→NewRunScene), así no
> quedan caminos de reset divergentes ni una run degradada in-place.
> **Validado:** compilación limpia, EditMode 126/126, NewRunScene en Build Settings,
> `newRunConfig` asignado en la escena, E2E por código (mazo+1, filtro Tienda correcto).
>
> **Última actualización previa:** 2026-06-04 — M3 Sub-PR 3D (Tienda) **implementado,
> validación Unity pendiente** en branch `feat/m3-sub-d-tienda`. Nuevos:
> `Assets/Scripts/Run/Shop/{ShopNodeController, ShopConfig, ShopItem}.cs`,
> `Assets/Scripts/Gameplay/Relics/Hooks/ShopStockBuiltHookData.cs`,
> `Assets/Tests/EditMode/ShopTests.cs`, `Assets/Editor/ShopConfigSetup.cs`
> (menú `Roguelike > Setup Shop Config` — idempotente, sólo puebla pools).
> Espejo de la Hoguera: `ShopNodeController` es un panel runtime sobre RunScene
> auto-creado por `RunFlowController.BuildShopController()`; `EnterNode` ramifica
> en `NodeType.Shop` → `ShowShopPanel`; `OnShopComplete` incrementa
> `RunState.ShopsCompleted` + `CompleteNode`. La lógica de stock/compra vive en
> helpers static puros `ShopNodeController.BuildStock(config, state, seed)` y
> `TryPurchase(state, item, removalTarget=null)` (testeables sin UI). Stock = 3
> cartas (filtro ESTRICTO por tipo: ElementType ∈ {WorldA, WorldB, None}) + 2
> Retazos (excluye los ya poseídos) + 1 servicio "Eliminar carta" (precio
> escalante `RemoveCardPriceFor(ShopsCompleted)` = 75 + 5×previas). `ShopConfig`
> SO tiene pools dedicados (decisión: NO reusar `RunCombatConfig.RewardPool`).
> `RunState`: nuevos `int ShopsCompleted` (reset en `Reset()`) y
> `bool RemoveCardFromDeck(CardDeckEntry)` (remove por referencia). Hook
> `OnShopStockBuilt` con payload `ShopStockBuiltHookData` (Stock mutable,
> TurnManager null) disparado tras BuildStock y antes de dibujar. Parallax 3
> capas (wall/shelf/counter) con fallback de color si los sprites son null.
> **Pendiente:** abrir Unity → compilar → `ShopTests` 12/12 → flujo end-to-end +
> correr el menú `Setup Shop Config` para crear `ShopConfig.asset`.
>
> **Última actualización previa:** 2026-05-10 — M3 Sub-PR 3C **validado en Unity** en
> branch `feat/m3-sub-c-campfire` (rebaseada sobre `origin/main`). Nuevos:
> `Assets/Scripts/Run/Campfire/{CampfireNodeController, CampfireConfig,
> CampfireOption}.cs`, `CardUpgradeDef.cs`,
> `Assets/Scripts/Gameplay/Relics/Hooks/CampfireOptionsBuiltHookData.cs`,
> `Assets/Tests/EditMode/CampfireTests.cs`,
> `Assets/Editor/CardUpgradeSetup.cs` (menú `Roguelike > Setup Placeholder
> Card Upgrades` — sobreescritor, no idempotente — que pobla `upgradedEffects`
> + `upgradedDescription` en los 6 SOs del starter deck).
> Mejora de cartas "bake-in": `CardUpgradeDef` embebido en `CardDefinition` con
> override de cost / effects / name / description; `CreateUpgradedClone()` en
> single y dual; `CardDeckEntry` gana `IsUpgraded`/`CanUpgrade()`/`ApplyUpgrade()`;
> el clon es una nueva instancia SO runtime (TurnManager no necesita cambios).
> **Importante:** la descripción de la carta es un string fijo en el SO
> (`description`), no se computa desde `effects` — los upgrades DEBEN actualizar
> también la descripción para que el HUD refleje los números nuevos.
> `AudioManager.CampfireAmbientClip` (clip programático loop-friendly).
> `RunFlowController` instancia el `CampfireNodeController` en `BuildUI()` y
> ramifica `EnterNode` en `NodeType.Campfire` antes del fallback ResolvePanel.
> Hook `OnCampfireOptionsBuilt` con payload `CampfireOptionsBuiltHookData`
> (Options mutable, TurnManager null por estar fuera de combate).
> Sprites de parallax en `Assets/Art/Campfire/`, asignados al
> `CampfireConfig.asset` (en `Assets/ScriptableObjects/`).
>
> **Última actualización previa:** 2026-05-08 — M3 Sub-PR 3B cerrado:
> 23 efectos concretos en `Gameplay/Relics/Effects/`, `RelicInventoryView` en
> HUD de combate, drops Elite/Boss en `BattleFlowController`, editor script
> `Assets/Editor/RelicSoGenerator.cs`. Cableado de `IsElite`/`IsBoss` en
> payloads via `ConfigureCombat`; 8º dispatch de `OnDamageDealt` en path
> de cartas neutras de `TurnManager`. 23 iconos PNG en `Assets/Art/Relics/Icons/`.
> `RelicDebugOverlay` (`Assets/Scripts/Debug/`, `#if UNITY_EDITOR`) — overlay
> IMGUI auto-instanciado vía `RuntimeInitializeOnLoadMethod` para agregar/quitar
> Retazos en runtime (toggle ` o F2).

---

## Stack

### Engine
- **Unity 6.2** (versión exacta: 6000.2.14f1)
- Render Pipeline: **Built-in URP / 2D** (no es HDRP)
- Lenguaje: **C#**
- Active Input Handling: por defecto (Both)

### UI
- **Unity UI legacy (UnityEngine.UI)** — NO TextMeshPro
- Canvas construido en runtime (no prefabs de Canvas)
- Render mode: ScreenSpaceOverlay
- CanvasScaler: ScaleWithScreenSize, ref 1920x1080, match 0.5

### Animaciones
- **DOTween** (Demigiant) para todas las animaciones UI y transiciones
- Helper centralizado: `Assets/Scripts/Core/UI/UIAnimationHelper.cs` (8 métodos)
- Frame animation custom: `SpriteFrameAnimatorUI` para sprites de combate

### Audio
- **AudioManager singleton** con clips programáticos (placeholders)
- Bootstrap: `AudioManagerBootstrap` (auto-instancia en runtime)

### Persistencia
- **JSON file** vía `LocalFileSaveService` en `Application.persistentDataPath`
- Interfaz: `ISaveService`

### Tests
- **Unity Test Framework** (NUnit) en EditMode
- 22 archivos de test en `Assets/Tests/EditMode/` (suite 166/166)
- Helper compartido: `CombatTestBase.cs`

---

## Arquitectura del proyecto

### Principio rector: Scene-owned controllers

Cada escena tiene su controller principal. NO hay managers globales flotantes
salvo `RunSession`. Esto evita el anti-patrón de Unity de tener 15 singletons
DontDestroyOnLoad.

```
MainMenu Scene  →  MainMenuController
RunScene        →  RunFlowController + RunSession (DDOL)
BattleScene     →  BattleFlowController + RunSession (DDOL) + CombatUIController
                                            (con CombatFeedbackView, CardHandView, CombatHudView)
```

### Únicos DontDestroyOnLoad
- `RunSession` (gameplay) — owner del `RelicHookDispatcher` (instanciado en
  `Awake`, persiste con la run, lee `RunState.Relics` por referencia → sobrevive
  a `State.Reset`)
- `AudioManager` (audio bootstrap)
- `SceneTransitionManager` (fade entre escenas)

### Separación de responsabilidades (no negociable)

| Capa | Qué es | Ejemplos |
|------|--------|----------|
| **Datos** | RunState, ScriptableObjects, structs puros | `RunState.cs`, `CardDefinition.cs`, `EnemyDefinition.cs` |
| **Lógica** | Controllers de flujo, manejo de turnos, IA | `TurnManager.cs`, `RunFlowController.cs`, `BattleFlowController.cs` |
| **Presentación** | UI, VFX, animaciones, audio | `CombatUIController.cs`, `CombatFeedbackView.cs`, `CardHandView.cs` |

**Regla**: la lógica nunca vive en UI. La UI nunca muta gameplay state.
Comunicación UI → lógica vía métodos públicos del TurnManager. Comunicación
lógica → UI vía eventos (`Action<T>`) en TurnManager.

### Comunicación entre escenas

Exclusivamente vía `RunSession.State` (= `RunState`). Flags principales:

- `PendingReturnFromBattle` — combate terminó, RunScene debe procesar resultado
- `LastNodeOutcome` — Victoria / Derrota / None
- `CurrentNodeId` — nodo donde estamos en el mapa
- `RunFailed` — la run completa falló (game over)
- `ActoCompleted` — el boss del acto fue derrotado

---

## Pipeline de combate

```
[BattleFlowController.Awake]
    └─ TryConfigureCombat() lee RunSession + nodo actual
    └─ Determina enemigo (specific del nodo o default del config)
    └─ Detecta si es boss
    └─ TurnManager.ConfigureCombat(deck, enemy, hp, maxHp)

[CombatUIController.Start]
    └─ BuildUI() construye canvas en runtime (HUD, paneles, fondos)
    └─ InitializeExtractedViews() crea CombatFeedbackView, CardHandView y CombatHudView
    └─ Cada view se suscribe a TurnManager events de forma independiente

[Loop de combate]
    Player turn:
      - TurnManager.BeginPlayerTurn() → reset energía, draw cards
      - CardHandView muestra mano, click → TryPrepareCardPlay → ResolvePreparedCardPlay
      - Effects encolados en ActionQueue
      - ActionQueue.ProcessAll() resuelve secuencialmente (determinista)
      - Eventos disparan: PlayerHitEffectiveness, EnemyTookDamage
      - CombatFeedbackView muestra popups WEAK/RESIST/MOMENTUM, shake, etc.
      - Player.EndTurn() → enemigo

    Enemy turn:
      - TurnManager.ExecuteEnemyTurn() → ClearBlock + SelectEnemyMove + QueueEffects
      - ActionQueue.ProcessAll()
      - PlanNextEnemyMove() para mostrar intent en próximo turno

[Fin de combate]
    └─ TurnManager.CheckCombatEndConditions() detecta victoria/derrota
    └─ TurnManager.DispatchCombatEnd() SINCRONIZA RunState.PlayerCurrentHP/MaxHP
       = _player.* ANTES de disparar los hooks OnCombatEnd (Opción B, fix 2026-06-14)
       → RunState autoritativo; los Retazos OnCombatEnd mutan/leen HP fresco
    └─ BattleFlowController.Update() detecta cambio de fase
    └─ ReportOutcome() = ApplyCombatResult(session, victory) [flags/ActoCompleted/
       drops, SIN re-pisar HP] + carga RunScene
```

---

## Estructura de archivos

### Scripts (`Assets/Scripts/`) — 108 archivos C#

```
Core/
  Audio/
    AudioManager.cs              ← singleton, clips programáticos
    AudioManagerBootstrap.cs     ← auto-instancia
  UI/
    UIAnimationHelper.cs         ← 8 métodos DOTween (FadeIn, ScaleIn, Punch, etc.)
  VFX/
    AmbientParticleController.cs
  SceneTransitionManager.cs      ← fade entre escenas

Menu/
  MainMenuController.cs

Run/
  RunSession.cs                  ← ÚNICO DontDestroyOnLoad de gameplay
  RunState.cs                    ← datos del run (deck, gold, HP, nodos)
  RunCombatConfig.cs
  RunFlowController.cs           ← flujo en RunScene
  BattleFlowController.cs        ← flujo en BattleScene
  RunAmbientParticles.cs
  NewRun/                        ← Sub-PR 3E — pantalla de arranque de run
    NewRunController.cs          ← scene-owned, máquina de 3 pasos, UI runtime
    NewRunConfig.cs              ← SO: pool de caras, tipos, optionsPerWorld, confirmClip
    StarterDraft.cs              ← helpers static puros (draft + composición dual)
  Shop/                          ← Sub-PR 3D — Tienda
    ShopNodeController.cs, ShopConfig.cs, ShopItem.cs
  Campfire/                      ← Sub-PR 3C — Hoguera
    CampfireNodeController.cs, CampfireConfig.cs, CampfireOption.cs
  Map/
    ActMap.cs, MapNode.cs, NodeState.cs, NodeType.cs
    RunMapGenerator.cs
    Act1MapConfig.cs, EnemyPoolConfig.cs
    UI/
      RunMapView.cs                ← 3F: scroll HORIZONTAL (depth→X, índice→Y); el eje
                                     vive entero acá (GetNodePosition + CreateScrollView)
      RunMapNodeView.cs, RunMapEdgeView.cs  ← 3F: anchor (0,0.5)

Gameplay/
  Cards/
    CardDefinition.cs            ← SO base de carta
    DualCardDefinition.cs        ← SO con sideA/sideB
    CardDeckEntry.cs             ← entrada de mazo (puede ser simple o dual)
    CardEnums.cs                 ← CardType, EffectType, EffectTarget
    EffectRef.cs                 ← referencia a efecto en SO
  Combat/
    TurnManager.cs               ← ~1107 loc — PROTEGIDO
    ActionQueue.cs               ← 85 loc — PROTEGIDO
    PlayerCombatActor.cs         ← 246 loc — PROTEGIDO
    EnemyCombatActor.cs
    ICombatActor.cs, IGameAction.cs, ActionContext.cs
    ElementTypes.cs              ← 7 tipos + matriz de efectividad
    ElementTypeColors.cs         ← C8: fuente única de verdad ElementType→Color (tinte UI)
    SpriteFrameAnimatorUI.cs
    CombatUIController.cs        ← 752 loc (era 1473)
    CombatFeedbackView.cs        ← 357 loc (extraído en Fase 1)
    CardHandView.cs              ← 501 loc (extraído en Fase 2)
    CombatHudView.cs             ← 344 loc (extraído en Fase 3)
    CombatBackgroundView.cs      ← 182 loc (extraído en Fase 4)
    CombatAmbientParticles.cs
    Actions/
      DamageAction.cs, BlockAction.cs, DrawCardsAction.cs
    Backgrounds/
      CombatBackgroundController.cs, CombatBackgroundTheme.cs
  Enemies/
    EnemyDefinition.cs           ← SO de enemigo
    EnemyMove.cs                 ← struct de movimiento + IntentType enum
    EnemyEnums.cs                ← AIPattern (RandomWeighted, Sequence, PhaseBased)
  Relics/                        ← Sub-PR 3A — sistema de Retazos
    RelicCategory.cs             ← enum (Neutral, Switch, World)
    RelicHook.cs                 ← enum de los 9 eventos expuestos
    IRelicEffect.cs              ← contrato ejecutable
    RelicDefinition.cs           ← SO con [SerializeReference] sobre IRelicEffect
    RelicInstance.cs             ← runtime wrapper (AcquisitionOrder + Counters)
    RelicHookDispatcher.cs       ← bus que TurnManager invoca; flag LogDispatches
    Hooks/
      RelicHookContext.cs        ← payload base + API limitada de 7 métodos
      CombatStartHookData.cs, PlayerTurnStartHookData.cs
      DamageDealtHookData.cs, DamageTakenHookData.cs   ← Amount mutable
      WorldSwitchHookData.cs, CombatEndHookData.cs, CardPlayedHookData.cs
      [Campfire/Shop hook data: diferidos a 3C/3D junto con sus tipos]
    Effects/                     ← Sub-PR 3B — 23 IRelicEffect concretos
      RelicOpenBlockEffect.cs, RelicOpenDrawEffect.cs, RelicOpenEnergyEffect.cs
      RelicTurnDrawEffect.cs, RelicTurnBlockEffect.cs, RelicTurnEnergyEveryNEffect.cs
      RelicDmgFlatBoostEffect.cs, RelicDmgFirstHitEffect.cs, RelicDmgReduceEffect.cs
      RelicAccSkillStackerEffect.cs, RelicAccEveryNthAttackEffect.cs
      RelicEndGoldEffect.cs, RelicEndHealEffect.cs, RelicEndEliteGoldEffect.cs
      RelicSwitchHealEffect.cs, RelicSwitchBlockEffect.cs
      RelicSwitchStyleChargeEffect.cs, RelicSwitchDamageEffect.cs
      RelicEliteVampiricEffect.cs, RelicEliteSpinesEffect.cs
      RelicElitePuristEffect.cs, RelicEliteChargeBoostEffect.cs
      RelicBossLastStitchEffect.cs
    UI/
      RelicInventoryView.cs      ← fila de iconos en HUD combate Y mapa (3F: instanciada
                                   también por RunFlowController en banda del _mapPanel)

Save/
  ISaveService.cs
  LocalFileSaveService.cs
  SaveService.cs
  SaveData.cs
  SaveSmokeTest.cs
```

### Editor scripts (`Assets/Editor/`) — Sub-PR 3B

```
RoguelikeCardBattler.Editor.asmdef   ← editor-only, references runtime asmdef
RelicSoGenerator.cs                   ← MenuItem "Roguelike/Generate Relic Assets":
                                        crea los 23 .asset en Assets/ScriptableObjects/Relics/
                                        (idempotente — saltea archivos existentes)
```

### Tests (`Assets/Tests/EditMode/`) — 22 archivos (suite EditMode 166/166)

```
CombatTestBase.cs                ← helper compartido
ActionQueueTests.cs
TurnManagerTests.cs
DamageEffectivenessTests.cs
ElementEffectivenessTests.cs
PlayerCombatActorTests.cs
DefinitionTypeDefaultsTests.cs
WorldSwitchLimitTests.cs
HealActionTests.cs               ← M2 Sub-PR D
RelicHookDispatcherTests.cs      ← M3 Sub-PR 3A (8 casos de plomería)
RelicEffectsTests.cs             ← M3 Sub-PR 3B (efectos concretos: mutación,
                                   counters, RunState directo, hook filtering,
                                   guards con TurnManager null)
CampfireTests.cs                 ← M3 Sub-PR 3C (8 casos: heal/upgrade/hook)
ShopTests.cs                     ← M3 Sub-PR 3D (13 casos: oro/stock/filtro/hook)
NewRunTests.cs                   ← M3 Sub-PR 3E (8 casos: draft/dual/tipos)
RunMapGeneratorTests.cs          ← M3 Sub-PR 3F (5 casos: determinismo topología/
                                   enemigos, divergencia por seed, DAG válido,
                                   start/end forzados — blinda el refactor horizontal)
ElementTypeColorsTests.cs        ← C8 (6 casos) + #104 (4 casos TypePrefix: None vacío,
                                   token con corchetes, hex==ReadableOnDark, all-types
                                   no vacíos) = 10 casos totales
CardArtTests.cs                  ← Auditoría de arte C7 (5 casos: campo Art default
                                   null, set/omit en SetDebugData, persistencia en el
                                   clon de upgrade, herencia dual por mundo, resolución
                                   vía CardDeckEntry)
CombatEndSyncTests.cs            ← Auditoría SUB-PR 1 (T1: el seam ApplyCombatResult
                                   no pisa el heal de Retazos OnCombatEnd; T2: el retry
                                   tras derrota con 0 HP restaura HP jugable)
StyleChargeTests.cs              ← Auditoría SUB-PR 2 (T3 pre-block: golpe SuperEficaz
                                   bloqueado al 100% igual da carga; T4 reset de estado
                                   de Estilo en re-init + sangrado de counters R-6;
                                   T7 cap a 5 / overshoot — D-B)
RelicAssetTests.cs               ← Auditoría SUB-PR 2 (T5: carga los 23 .asset reales y
                                   valida que cada hook declarado es despachable)
RelicGrantEffectsOnTurnManagerTests.cs ← Auditoría SUB-PR 2 (T6: 10 casos de Grant*
                                   —Block/StyleCharge/Heal/Energy/DrawCards— sobre un
                                   TurnManager real, dispatch manual del hook)
NeutralCardDamageTests.cs        ← Auditoría SUB-PR 2 (T9: carta None aplica 90% del
                                   daño base, DD-002, contra cualquier tipo)
```

### ScriptableObjects (`Assets/ScriptableObjects/`)

```
Cards/
  StrikeBasic.asset, DefendBasic.asset, BattleFocus.asset
  [+ duales]
Enemies/
  WeakSlime.asset, Goblin.asset, SkeletonWarrior.asset, DarkMage.asset
  BossAct1.asset
Configs/
  [run config, enemy pools, etc.]
```

### Asmdefs

- Runtime: `Assets/Scripts/RoguelikeCardBattler.asmdef`
- Tests: `Assets/Tests/EditMode/EditModeTests.asmdef`

---

## Archivos PROTEGIDOS (no modificar sin aprobación explícita)

Estos archivos contienen el core del combate. Cualquier modificación requiere
aprobación explícita de Sebastián antes de proceder:

| Archivo | LOC | Por qué está protegido |
|---------|-----|------------------------|
| `TurnManager.cs` | ~1107 | Flujo de turnos, efectividad, energía, Contador de Estilo, IA enemiga, world switch, hooks de Retazos (3A) + API delegada |
| `ActionQueue.cs` | 85 | Orden determinista de ejecución (FIFO) |
| `PlayerCombatActor.cs` | 236 | Mecánicas del jugador en combate |

**Si una tarea requiere cambios aquí**: PARAR y preguntar al usuario antes
de tocar.

---

## Configuraciones críticas a verificar

### Build Settings
- BattleScene y RunScene deben estar en Build Settings o `BattleFlowController`
  loggea error y se queda colgado.

### Player Settings
- Run In Background → debe estar activado para WebSocket si se agrega backend
  (no aplica hoy).

### Asmdef references
- EditModeTests.asmdef → debe referenciar RoguelikeCardBattler.asmdef para
  que los tests vean el código de gameplay.

### Escena de combate
- `Assets/Scenes/BattleScene.unity` es la escena principal de pruebas.
- Debe tener un GameObject con `TurnManager` + `CombatUIController` (mismo GO).
- BattleFlowController vive en otro GameObject de la misma escena.

---

## Convenciones de código

### Naming
- Tipos en inglés (TurnManager, CardDefinition, BlockAction)
- Comentarios y docstrings: español preferido para onboarding, mezcla aceptable
- Variables privadas con prefijo `_` (ej: `_canvas`, `_handCache`)
- Constantes en PascalCase (`HandCardWidthBase`, `HudFlashColor`)

### Reglas obligatorias
- **CS0104**: si un archivo usa `System` y `UnityEngine`, escribir `UnityEngine.Object`
  explícito (nunca `Object` ambiguo).
- **Zero console errors** = criterio de aceptación base de todo cambio.
- **No manual editor setup**: features nuevas deben auto-crear GameObjects en runtime
  o ser instanciadas por un scene controller existente. Nunca requerir attach manual
  en inspector para probar.
- **Animaciones fire-and-forget**: la lógica de gameplay NUNCA espera ni vive dentro
  de callbacks de animación cosmética. Excepción única: `PlayAttackOnce()` con
  callback de resolución de carta.
- **Comentarios de onboarding**: todo código nuevo lleva comentarios explicando el
  porqué, no el qué.
- **No duplicar lógica**: verificar `UIAnimationHelper.cs` y `AudioManager.cs` antes
  de crear helpers nuevos.

---

## Restricciones conocidas

- **CI parqueado.** GameCI quedó bloqueado por el modelo de licencias Entitlement
  de Unity 6. El workflow `tests.yml` está commiteado pero DESACTIVADO
  (`workflow_dispatch` only); los tests se corren localmente vía Unity Test
  Runner / Unity-MCP (`tests-run`). Detalle en `_roadmap.md` Future work.
- **`gh` CLI presente** (2.92.0). Los PRs se crean con `gh pr create`.
- **Comentarios mezclados español/inglés** sin criterio fijo (deuda menor, ver roadmap).
- **CombatUIController** (752 loc post-Fase 4). Descomposición completa — es ahora
  un orquestador puro de BuildUI y lifecycle.

> **Nota:** los 3 gaps de `TurnManager` que listaba esta sección (PhaseBased AI,
> `EffectType.Heal`, `CalculateIntentValue`) **se resolvieron en M2 Sub-PR D
> (2026-05-07)** — ya no son restricciones. Ver `Assets/Scripts/Gameplay/Combat/
> CLAUDE.md §"Deuda técnica resuelta en Sub-PR D"`.

---

## Cómo se actualiza este archivo

- En `modo:implementacion`: cuando un cambio modifica estructura (nuevo módulo,
  nuevo subsistema, refactor que mueve responsabilidades, cambio de stack), Claude
  actualiza este archivo al terminar la tarea.
- Cambios menores (un método nuevo en archivo existente) NO requieren actualización.
- Si dudás si un cambio es "estructural", preguntá.
- La fecha "Última actualización" en el header se actualiza con cada edit.
