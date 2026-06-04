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

### Tooling Claude Code + Unity

El proyecto está configurado para trabajar con Claude Code conectado a Unity.
Esto es lo mínimo que necesitas saber para que funcione; los **hábitos** de uso
(prompting, ahorro de tokens, memoria persistente) viven en
`Docs/dev/CLAUDE_WORKFLOW_GUIDE.md`.

- **MCP de Unity** — servidor `ai-game-developer` (IvanMurzak), en modo **local**
  (sin login ni token). Da ~73 tools para leer la consola, inspeccionar
  escenas/GameObjects, correr tests NUnit y tomar screenshots desde el Editor.
  Requiere Unity abierto con el plugin conectado (Connected verde). Si al reabrir
  Unity cambia el puerto, actualiza la URL en `.mcp.json` (por defecto
  `http://localhost:24348`).
- **Permisos** — `.claude/settings.json` es **compartido** (commiteado, lo lee el
  otro dev); `.claude/settings.local.json` es **personal** (gitignored, para tus
  rutas absolutas y reglas propias). Precedencia: `deny > ask > allow`.
- **Hook protect-files** — `.claude/hooks/protect-files.js` bloquea Edit/Write
  sobre los 6 archivos protegidos (los 3 de combate: `TurnManager.cs`,
  `ActionQueue.cs`, `PlayerCombatActor.cs`, más `GOLDEN_RULES.md`, `_gdd.md` y
  `DESIGN_DECISIONS.md`). Para una edición autorizada, comenta el hook en
  `settings.json` o edita a mano.
- **Slash commands `/modo-*`** — `.claude/commands/modo-*.md` activan los 4 modos
  (`/modo-gdd`, `/modo-diseno`, `/modo-implementacion`, `/modo-revision`). Cada
  uno carga solo el ritual de lectura de ese modo, no todo `Docs/`.

**Para hábitos de prompting, ahorro de tokens y memoria persistente →
`Docs/dev/CLAUDE_WORKFLOW_GUIDE.md`.**

