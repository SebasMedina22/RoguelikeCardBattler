# Technical Snapshot — RoguelikeCardBattler

> **Qué es este archivo:** foto point-in-time de la arquitectura técnica concreta
> del proyecto. Se actualiza cuando hay cambios estructurales (nuevo subsistema,
> nuevo módulo, cambio de stack, refactor importante).
>
> En `modo:implementacion` se lee OBLIGATORIAMENTE antes de cualquier cambio que
> afecte arquitectura o componentes críticos.
>
> **Última actualización:** 2026-05-10 — M3 Sub-PR 3C **validado en Unity** en
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
- 8 archivos de test en `Assets/Tests/EditMode/`
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
    └─ BattleFlowController.Update() detecta cambio de fase
    └─ ReportOutcome() actualiza RunSession + carga RunScene
```

---

## Estructura de archivos

### Scripts (`Assets/Scripts/`) — 53 archivos C#

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
  Map/
    ActMap.cs, MapNode.cs, NodeState.cs, NodeType.cs
    RunMapGenerator.cs
    Act1MapConfig.cs, EnemyPoolConfig.cs
    UI/
      RunMapView.cs, RunMapNodeView.cs, RunMapEdgeView.cs

Gameplay/
  Cards/
    CardDefinition.cs            ← SO base de carta
    DualCardDefinition.cs        ← SO con sideA/sideB
    CardDeckEntry.cs             ← entrada de mazo (puede ser simple o dual)
    CardEnums.cs                 ← CardType, EffectType, EffectTarget
    EffectRef.cs                 ← referencia a efecto en SO
  Combat/
    TurnManager.cs               ← 735 loc — PROTEGIDO
    ActionQueue.cs               ← 85 loc — PROTEGIDO
    PlayerCombatActor.cs         ← 236 loc — PROTEGIDO
    EnemyCombatActor.cs
    ICombatActor.cs, IGameAction.cs, ActionContext.cs
    ElementTypes.cs              ← 7 tipos + matriz de efectividad
    SpriteFrameAnimatorUI.cs
    CombatUIController.cs        ← 710 loc (era 1473)
    CombatFeedbackView.cs        ← 357 loc (extraído en Fase 1)
    CardHandView.cs              ← 400 loc (extraído en Fase 2)
    CombatHudView.cs             ← 327 loc (extraído en Fase 3)
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
      RelicInventoryView.cs      ← fila de iconos en HUD combate (3F: RunMapView)

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

### Tests (`Assets/Tests/EditMode/`) — 11 archivos

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
| `TurnManager.cs` | ~960 | Flujo de turnos, efectividad, energía, Contador de Estilo, IA enemiga, world switch, hooks de Retazos (3A) + API delegada |
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

- **No CI/CD** configurado. Tests se corren localmente vía Unity Test Runner.
- **No gh CLI** instalado. PRs se crean manualmente desde la URL que devuelve `git push`.
- **Comentarios mezclados español/inglés** sin criterio fijo (deuda menor, ver roadmap).
- **CombatUIController** (710 loc post-Fase 4). Descomposición completa — es ahora
  un orquestador puro de BuildUI y lifecycle. Listo para recibir features de M2.
- **TurnManager tiene gaps conocidos** que requieren tocar el archivo protegido:
  - PhaseBased AI declarado en enum pero sin implementación en `SelectEnemyMove()`
  - `EffectType.Heal` no existe (BossAct1 Regenerate usa Block como workaround)
  - `CalculateIntentValue()` solo maneja Attack+Damage y Defend+Block

---

## Cómo se actualiza este archivo

- En `modo:implementacion`: cuando un cambio modifica estructura (nuevo módulo,
  nuevo subsistema, refactor que mueve responsabilidades, cambio de stack), Claude
  actualiza este archivo al terminar la tarea.
- Cambios menores (un método nuevo en archivo existente) NO requieren actualización.
- Si dudás si un cambio es "estructural", preguntá.
- La fecha "Última actualización" en el header se actualiza con cada edit.
