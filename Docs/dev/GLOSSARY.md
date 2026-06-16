## Glossary (dev)

> **Última actualización:** 2026-06-15 (SUB-PR 3 auditoría pre-M4 — paquete docs i).
> Purga de entradas de Momentum/FreePlays (eliminados en M2, PR #89).
> Agregados términos M2/M3.

Formato: término, definición breve, y si aplica "Dónde se ve" (UI) / "Dónde vive" (archivo).

---

- **ActionQueue:** cola FIFO que ejecuta acciones de combate en orden determinista. `ProcessAll()` es síncrono. Vive en `Assets/Scripts/Gameplay/Combat/ActionQueue.cs`.

- **AIPattern:** enum de patrones de IA enemiga: `RandomWeighted` (peso aleatorio), `Sequence` (orden fijo), `PhaseBased` (filtra moves por rango de HP `MinHpPercent`/`MaxHpPercent`). Configurado en `EnemyDefinition`. Vive en `Assets/Scripts/Gameplay/Enemies/EnemyEnums.cs`.

- **asmdef (runtime):** define el ensamblado de scripts del juego (`Assets/Scripts/RoguelikeCardBattler.asmdef`).

- **asmdef (tests):** define el ensamblado de pruebas EditMode (`Assets/Tests/EditMode/EditModeTests.asmdef`); referencia al runtime.

- **BattleFlowController:** orquesta el flujo de BattleScene: inicializa el combate desde `RunSession`, detecta victoria/derrota, aplica drops de Retazos y navega a RunScene. Vive en `Assets/Scripts/Run/BattleFlowController.cs`.

- **CardDeckEntry:** entrada de mazo/mano que puede contener carta simple o dual. Expone `GetActiveCardDefinition(world)`. Vive en `Assets/Scripts/Gameplay/Cards/CardDeckEntry.cs`.

- **CardDefinition:** ScriptableObject con datos de una carta (coste, efectos, tipo elemental, arte, upgrade). Vive en `Assets/Scripts/Gameplay/Cards/CardDefinition.cs`. Se ve en UI de mano.

- **CardUpgradeDef:** struct embebido en `CardDefinition` con overrides de coste/efectos/nombre/descripción para la versión mejorada. `CanUpgrade()`/`ApplyUpgrade()` en `CardDeckEntry`; `CreateUpgradedClone()` produce un SO runtime. Vive embebido en `CardDefinition`.

- **Change World:** acción que alterna WorldSide A/B. Limitada por combate (configurable, default 1; debug ilimitado). El Contador de Estilo puede otorgar 1 uso extra. Botón "Cambiar Mundo" en el HUD.

- **CombatHudView:** componente UI extraído de `CombatUIController` (Fase 3). Muestra energía, Contador de Estilo, HP/bloqueo jugador+enemigo, intent del enemigo, tipo elemental, mundo, switches. No se suscribe a eventos — hace polling vía `Sync()` cada frame. Vive en `Assets/Scripts/Gameplay/Combat/CombatHudView.cs`.

- **CombatUIController:** orquesta BuildUI + lifecycle del canvas de combate en runtime. Wirear las 4 views extraídas vía `InitializeExtractedViews()`. ~710 LOC. Vive en `Assets/Scripts/Gameplay/Combat/CombatUIController.cs`.

- **Contador de Estilo:** sistema que reemplaza a Momentum (eliminado en M2, PR #89). +1 carga al hacer un golpe SuperEficaz (pre-block); −1 al recibir SuperEficaz del enemigo. 5 cargas → 1 switch de mundo extra (no acumulable), reset de cargas. UI muestra "Estilo: X/5". Popup "+ESTILO" en `CombatFeedbackView`. Variable `_styleCharges` en `TurnManager`.

- **DualCardDefinition:** ScriptableObject con dos `CardDefinition` (sideA/sideB) correspondientes a los mundos A y B. La cara activa depende del WorldSide actual; `GetActiveCardDefinition(world)` via `CardDeckEntry`. Vive en `Assets/Scripts/Gameplay/Cards/DualCardDefinition.cs`.

- **Efectividad (Effectiveness):** resultado de comparar ElementType atacante vs tipo activo del defensor. Tres valores: `SuperEficaz` (×1.5), `Neutro` (×1.0), `PocoEficaz` (×0.75). **Bidireccional desde M2 (DD-018):** aplica al daño del jugador sobre el enemigo Y al daño del enemigo sobre el jugador. Cartas sin tipo (`None`) aplican 90% del daño base sin popup. Lógica en `Assets/Scripts/Gameplay/Combat/ElementTypes.cs`. UI: popups WEAK/RESIST/+ESTILO.

- **ElementType:** enum placeholder por color (Rojo, Amarillo, Azul, Morado, Negro, Blanco, None). Vive en `Assets/Scripts/Gameplay/Combat/ElementTypes.cs`.

- **ElementTypeColors:** clase estática con la fuente única de verdad `ElementType→Color`. API: `For(type)`, `ReadableOnDark(type)`, `ReadableTextOn(bg)`, `Dim(color,factor)`, `TypePrefix(type)` (token rich-text `[Tipo]` para UI). Vive en `Assets/Scripts/Gameplay/Combat/ElementTypeColors.cs`.

- **EnemyDefinition:** ScriptableObject de enemigo (HP, AIPattern, moves, tipo elemental, avatar sprite). Vive en `Assets/Scripts/Gameplay/Enemies/EnemyDefinition.cs`.

- **EnemyMove:** entrada de IA con efectos, peso/orden, tipo de intent y rango de HP (para PhaseBased). Vive en `Assets/Scripts/Gameplay/Enemies/EnemyMove.cs`. Se refleja en el intent UI.

- **HealAction:** acción (`IGameAction`) que cura a un actor. Producida por `EffectType.Heal` en `TurnManager.CreateAction()`. Funciona en jugador y enemigo. Implementada en M2 Sub-PR D. Vive en `Assets/Scripts/Gameplay/Combat/Actions/`.

- **IGameAction:** interfaz mínima para acciones en la ActionQueue (DamageAction, BlockAction, DrawCardsAction, HealAction). Vive en `Assets/Scripts/Gameplay/Combat/IGameAction.cs`.

- **OnCombatEnd / OnCombatStart / OnDamageDealt / … :** hooks del `RelicHook` enum. Invocan a los `IRelicEffect` de los Retazos activos a través del `RelicHookDispatcher`. Ver tabla completa en `COMBAT_ARCHITECTURE.md §Sistema de Retazos`.

- **Placeholder:** marcadores temporales (p. ej. nombres de tipos por color, sprites de arte) que se reemplazarán más adelante; documentar cuando se use.

- **RelicHookDispatcher:** bus pub/sub que `TurnManager` invoca en 9 eventos clave. Instanciado en `RunSession.Awake`, persiste con la run. Vive en `Assets/Scripts/Gameplay/Relics/RelicHookDispatcher.cs`.

- **RelicHookContext:** payload de contexto para cada dispatch. Expone 7 métodos seguros: `GrantBlock`, `GrantHeal`, `GrantDrawCards`, `GrantEnergy`, `GrantStyleCharge`, `GrantBonusWorldSwitch`, `EnqueueExtraDamage`. Guards de `IsCombatFinished` protegen los que encolan acciones. Vive en `Assets/Scripts/Gameplay/Relics/Hooks/RelicHookContext.cs`.

- **RelicInstance:** wrapper runtime de un Retazo (AcquisitionOrder + Counters). No es el SO — es la instancia viva durante el run. Vive en `Assets/Scripts/Gameplay/Relics/RelicInstance.cs`.

- **Retazo / Retazos:** items persistentes del run (equiv. a Relics en Slay the Spire). Adquiridos en combates Elite/Boss o en la Tienda. Efectos cableados a hooks del combate y nodos. Inventario visible en HUD de combate y mapa. `RunState.Relics: List<RelicInstance>`.

- **RunFlowController:** orquesta RunScene: mapa, nodos, transiciones (Hoguera/Tienda/Combate/Evento), instancia `RelicInventoryView` en el HUD del mapa. 885 LOC (god-object pendiente de desguace antes de M4-4b). Vive en `Assets/Scripts/Run/RunFlowController.cs`.

- **RunSession:** único DontDestroyOnLoad de gameplay. Owner del `RelicHookDispatcher` (instanciado en Awake; persiste con la run, sobrevive a `State.Reset`). Comunica datos entre escenas vía `RunState`. Vive en `Assets/Scripts/Run/RunSession.cs`.

- **RunState:** datos del run (mazo, HP, gold, Retazos, nodos, flags de flujo). Fuente única de verdad entre escenas. Vive en `Assets/Scripts/Run/RunState.cs`.

- **Test Runner / EditMode tests:** ventana Unity `Window > General > Test Runner`, pestaña EditMode. Tests en `Assets/Tests/EditMode/`. Suite: 148/148 (al 2026-06-14).

- **TurnManager:** orquesta el flujo de combate (turnos, jugar cartas, cambio de mundo, Contador de Estilo, efectividad, IA enemiga, hooks de Retazos). Archivo protegido. Vive en `Assets/Scripts/Gameplay/Combat/TurnManager.cs`.

- **WEAK / RESIST / +ESTILO:** popups de feedback de efectividad y Estilo. WEAK = golpe SuperEficaz; RESIST = golpe PocoEficaz; +ESTILO = carga de Estilo ganada (se combina con WEAK: "WEAK!\n+ESTILO"). Mostrados por `CombatFeedbackView` a partir del evento `PlayerHitEffectiveness`.

- **World / WorldSide (A/B):** estado binario del combate (mundo actual). Afecta cartas duales y el tipo elemental activo del jugador. Cambio limitado por combate según reglas. Guardado en `TurnManager.currentWorld`.

- **WorldSwitch count / Estilo bonus:** `_worldSwitchesUsed` (usos totales) y `_bonusWorldSwitches` (extra por Contador de Estilo, máx 1 sin acumular). Ambos en `TurnManager`. HUD muestra los switches restantes.
