## Glossary (dev)

Formato: término, definición breve, y si aplica “Dónde se ve” (UI) / “Dónde vive” (archivo).

- ActionQueue: cola FIFO que ejecuta acciones de combate en orden. Vive en `Assets/Scripts/Gameplay/Combat/ActionQueue.cs`.
- asmdef (runtime): define el ensamblado de scripts del juego (`Assets/Scripts/RoguelikeCardBattler.asmdef`).
- asmdef (tests): define el ensamblado de pruebas EditMode (`Assets/Tests/EditMode/EditModeTests.asmdef`); referencia al runtime.
- CardDeckEntry: entrada de mazo/mano que puede contener carta simple o dual. Vive en `Assets/Scripts/Gameplay/Cards/CardDeckEntry.cs`.
- CardDefinition: ScriptableObject con datos de una carta (coste, efectos, tipo elemental). Vive en `Assets/Scripts/Gameplay/Cards/CardDefinition.cs`. Se ve en UI de mano.
- Change World: acción que alterna WorldSide A/B. Limitada a 1 por combate salvo debug ilimitado. Botón en HUD.
- CombatUIController: construye HUD en runtime (energía, momentum, mundo, switches, popups WEAK/RESIST). Vive en `Assets/Scripts/Gameplay/Combat/CombatUIController.cs`.
- DualCardDefinition: carta con dos lados (A/B) dependiente del WorldSide actual. Vive en `Assets/Scripts/Gameplay/Cards/DualCardDefinition.cs`.
- Effectiveness: resultado de comparar ElementType atacante vs defensor (SuperEficaz, Neutro, PocoEficaz). Lógica en `Assets/Scripts/Gameplay/Combat/ElementTypes.cs`. UI muestra popups WEAK/RESIST.
- ElementType: enum placeholder por color (Rojo, Amarillo, Azul, Morado, Negro, Blanco, None). Vive en `Assets/Scripts/Gameplay/Combat/ElementTypes.cs`.
- EnemyDefinition: ScriptableObject de enemigo (HP, patrón AI, moves, tipo elemental). Vive en `Assets/Scripts/Gameplay/Enemies/EnemyDefinition.cs`.
- EnemyMove: entrada de IA con efectos, peso/orden y tipo de intent. Vive en `Assets/Scripts/Gameplay/Enemies/EnemyMove.cs`. Se refleja en intent UI.
- FreePlays: contador interno de Momentum; al consumirlo, la siguiente carta no gasta energía. Variable interna en `TurnManager`.
- IGameAction: interfaz mínima para acciones en la ActionQueue (daño, block, draw). Vive en `Assets/Scripts/Gameplay/Combat/IGameAction.cs`.
- Momentum: nombre visible del recurso de “free play” que se gana al golpear debilidad (SuperEficaz). HUD muestra “Momentum: X”; popup “MOMENTUM +1”.
- Placeholder: marcadores temporales (p. ej. nombres de tipos por color) que se reemplazarán más adelante; se debe documentar cuando se use.
- Test Runner / EditMode tests: ventana Unity `Window > General > Test Runner`, pestaña EditMode; pruebas viven en `Assets/Tests/EditMode/`.
- TurnManager: orquesta el flujo de combate (turnos, jugar cartas, cambio de mundo, momentum, efectividad). Vive en `Assets/Scripts/Gameplay/Combat/TurnManager.cs`.
- WEAK / RESIST: popups de feedback de efectividad; WEAK cuando es SuperEficaz, RESIST cuando es PocoEficaz. Mostrados por `CombatUIController` a partir del evento de hit.
- World / WorldSide (A/B): estado binario del combate (mundo actual). Afecta cartas duales; cambio limitado según reglas. Guardado en `TurnManager`.
- maxWorldSwitchesPerCombat / worldSwitchesUsed / debugUnlimitedWorldSwitches: config y contadores para Change World (límite por combate, usos actuales, flag de debug ilimitado). Viven en `TurnManager` y se muestran en HUD (Switches).

