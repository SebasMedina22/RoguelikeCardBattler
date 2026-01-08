## Onboarding dev

### Requisitos y setup
- Ver versión de Unity: abre `ProjectSettings/ProjectVersion.txt` (línea `m_EditorVersion`).
- Abrir el proyecto en Unity (hub o abrir carpeta raíz).
- Paquetes: Test Framework ya está en `Packages/manifest.json` (com.unity.test-framework).

### Correr el juego rápido
- Escena: `Assets/Scenes/BattleScene.unity`.
- En Play deberías ver HUD con Energy, Momentum, World y Switches; mano de cartas y un enemigo.
- Prueba rápida de tipos:
  - Carta: `Assets/ScriptableObjects/Cards/StrikeBasic.asset` → asigna `Element Type` (p. ej. Rojo).
  - Enemigo: `Assets/ScriptableObjects/Enemies/WeakSlime.asset` → asigna `Element Type` (p. ej. Azul).
  - En combate: Rojo vs Azul muestra “WEAK!” y “MOMENTUM +1”; Azul vs Rojo muestra “RESIST”.

### Correr tests
- Unity: `Window > General > Test Runner` → pestaña `EditMode` → `Run All`.
- CLI (opcional): `unity -runTests -projectPath . -testPlatform editmode -testResults results.xml`.
- Asmdefs:
  - Runtime: `Assets/Scripts/RoguelikeCardBattler.asmdef`.
  - Tests: `Assets/Tests/EditMode/EditModeTests.asmdef` (referencia al runtime).
  - Separación para no arrastrar dependencias de tests al runtime.

### Workflow Git
- Ramas: `feat/*` para features, `docs/*` para documentación, `chore/*` para tareas misceláneas.
- PRs: incluir `Closes #<issue>` para autocerrar el issue.

### Dónde mirar primero (archivos clave)
- `Assets/Scripts/Gameplay/Combat/TurnManager.cs`: orquesta turnos, jugar cartas, cambiar mundo, momentum.
- `Assets/Scripts/Gameplay/Combat/CombatUIController.cs`: HUD, botones, popups WEAK/RESIST/MOMENTUM.
- `Assets/Scripts/Gameplay/Combat/ActionQueue.cs`: cola FIFO de acciones.
- `Assets/Scripts/Gameplay/Combat/ElementTypes.cs`: enum de tipos y matriz de efectividad.
- `Assets/Scripts/Gameplay/Cards/CardDefinition.cs`: datos de cartas, tipo incluido.
- `Assets/Scripts/Gameplay/Enemies/EnemyDefinition.cs`: datos de enemigo, tipo incluido.

