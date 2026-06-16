## Onboarding dev

> **Última actualización:** 2026-06-15 (SUB-PR 3 auditoría pre-M4 — paquete docs i).
> Actualizado con HUD real (Estilo, no Momentum), 5 slash commands, y Run layer post-M3.

---

### Requisitos y setup

- Ver versión de Unity: abrí `ProjectSettings/ProjectVersion.txt` (línea `m_EditorVersion`). Versión actual: **6000.2.14f1** (Unity 6.2).
- Abrí el proyecto en Unity Hub o desde la carpeta raíz.
- Paquetes: Test Framework ya está en `Packages/manifest.json` (`com.unity.test-framework`).

---

### Correr el juego rápido

**Escena de combate:** `Assets/Scenes/BattleScene.unity`.

En Play deberías ver:
- HUD con **Energía: X**, **Estilo: X/5** (Contador de Estilo), **Mundo: A/B**, **Switches: X** (cambios disponibles).
- Mano de cartas (hasta 5), botones **End Turn** y **Cambiar Mundo**.
- Avatar y HP/bloqueo del jugador y del enemigo.
- Intent del enemigo (qué va a hacer en el próximo turno).

**Prueba rápida de tipos:**
- Carta: `Assets/ScriptableObjects/Cards/StrikeBasic.asset` → asigná `Element Type` (p. ej. Rojo).
- Enemigo: `Assets/ScriptableObjects/Enemies/WeakSlime.asset` → asigná `Element Type` (p. ej. Azul).
- En combate: Rojo vs Azul → popup `WEAK!` (SuperEficaz, daño ×1.5) + si se ganó carga de Estilo: `WEAK!\n+ESTILO`. Azul vs Rojo → `RESIST` (PocoEficaz).
- Cartas sin tipo (`None`) → daño al 90% sin popup.

**Escena del run completo:** `Assets/Scenes/RunScene.unity`.

La run completa va: `MainMenuScene` → `NewRunScene` (elegir 2 tipos, draftear carta dual) → `RunScene` (mapa horizontal con nodos: combate, hoguera, tienda, evento) → `BattleScene` → de vuelta a `RunScene`.

---

### Correr tests

- Unity: `Window > General > Test Runner` → pestaña `EditMode` → `Run All`.
- CLI (opcional): `unity -runTests -projectPath . -testPlatform editmode -testResults results.xml`.
- Suite actual: **148/148** (al 2026-06-14, 17 archivos de test en `Assets/Tests/EditMode/`).
- Asmdefs:
  - Runtime: `Assets/Scripts/RoguelikeCardBattler.asmdef`.
  - Tests: `Assets/Tests/EditMode/EditModeTests.asmdef` (referencia al runtime).

---

### Workflow Git

- Ramas: `feat/*` (features), `docs/*` (documentación), `chore/*` (misc), `refactor/*` (refactor).
- PRs: incluir `Closes #<issue>` para autocerrar el issue.

---

### Dónde mirar primero

#### Combate (BattleScene)

| Qué | Dónde |
|-----|-------|
| Orquestación de turnos, Contador de Estilo, efectividad, IA | `Assets/Scripts/Gameplay/Combat/TurnManager.cs` |
| Flujo de BattleScene (inicializa, detecta victoria/derrota, drops) | `Assets/Scripts/Run/BattleFlowController.cs` |
| HUD: Estilo, HP, bloqueo, intent, tipo, mundo | `Assets/Scripts/Gameplay/Combat/CombatHudView.cs` |
| Popups WEAK/RESIST/+ESTILO, shake, toast de mano llena | `Assets/Scripts/Gameplay/Combat/CombatFeedbackView.cs` |
| Mano de cartas, click → jugar | `Assets/Scripts/Gameplay/Combat/CardHandView.cs` |
| Cola FIFO de acciones (determinista) | `Assets/Scripts/Gameplay/Combat/ActionQueue.cs` |
| Tipos elementales y tabla de efectividad | `Assets/Scripts/Gameplay/Combat/ElementTypes.cs` |

#### Run layer (RunScene + estado entre escenas)

| Qué | Dónde |
|-----|-------|
| Estado del run (mazo, HP, gold, Retazos, flags) | `Assets/Scripts/Run/RunState.cs` |
| DontDestroyOnLoad de gameplay, owner del dispatcher de Retazos | `Assets/Scripts/Run/RunSession.cs` |
| Mapa horizontal, nodos, tienda, hoguera, transiciones | `Assets/Scripts/Run/RunFlowController.cs` |
| Pantalla de arranque de run (elegir tipos + draftear carta dual) | `Assets/Scripts/Run/NewRun/NewRunController.cs` |
| Stock de la Tienda (lógica pura testeable) | `Assets/Scripts/Run/Shop/ShopNodeController.cs` |
| Opciones de la Hoguera (heal/upgrade) | `Assets/Scripts/Run/Campfire/CampfireNodeController.cs` |
| Generador de mapa del run | `Assets/Scripts/Run/Map/RunMapGenerator.cs` |

#### Cartas y Retazos

| Qué | Dónde |
|-----|-------|
| Datos de carta (coste, efectos, tipo, arte, upgrade) | `Assets/Scripts/Gameplay/Cards/CardDefinition.cs` |
| Carta dual (cara A/B según mundo) | `Assets/Scripts/Gameplay/Cards/DualCardDefinition.cs` |
| Sistema de Retazos (hooks, bus, contexto) | `Assets/Scripts/Gameplay/Relics/` |

---

### Tooling Claude Code + Unity

El proyecto está configurado para trabajar con Claude Code conectado a Unity.
Los **hábitos** de uso (prompting, ahorro de tokens, memoria persistente) viven en
`Docs/dev/CLAUDE_WORKFLOW_GUIDE.md`.

- **MCP de Unity** — servidor `ai-game-developer` (IvanMurzak), en modo **local**
  (sin login ni token). Da ~75+ tools para leer la consola, inspeccionar
  escenas/GameObjects, correr tests NUnit y tomar screenshots desde el Editor.
  Usá `unity-tool-list` para ver el listado completo actualizado.
  Requiere Unity abierto con el plugin conectado (Connected verde). Si al reabrir
  Unity cambia el puerto, actualizá la URL en `.mcp.json` (por defecto
  `http://localhost:24348`).
- **Permisos** — `.claude/settings.json` es **compartido** (commiteado, lo lee el
  otro dev); `.claude/settings.local.json` es **personal** (gitignored, para tus
  rutas absolutas y reglas propias). Precedencia: `deny > ask > allow`.
- **Hook protect-files** — `.claude/hooks/protect-files.js` bloquea ediciones sobre
  los archivos protegidos por dos superficies: (1) `Edit/Write/MultiEdit/NotebookEdit`
  por `file_path`, y (2) `Bash` con los comandos del plugin Unity-MCP
  (`script-update-or-create`/`script-delete`) que editan `.cs` en disco.
  Protege 7 archivos: 3 de combate (`TurnManager.cs`, `ActionQueue.cs`,
  `PlayerCombatActor.cs`) + `GOLDEN_RULES.md` + `_gdd.md` + `DESIGN_DECISIONS.md`
  + el propio hook. Para una edición autorizada, comentá el hook en `settings.json`
  o editá a mano.
- **Slash commands** — invocados con `/nombre` en Claude Code:
  - `/modo-gdd` — procesa el GDD, mapea gaps vs código, genera milestones.
  - `/modo-diseno` — convierte una feature en spec técnico implementable.
  - `/modo-implementacion` — escribe, modifica y debuggea código.
  - `/modo-revision` — audita cambios contra spec, golden rules y arquitectura.
  - `/cierre-sesion` — checklist completo de persistencia: checkboxes de sub-tareas,
    anti-desfase de milestones futuros, tech_snapshot, memoria, higiene de milestone.
    Invocarlo al terminar cualquier bloque de trabajo para que la próxima sesión
    arranque con contexto fresco.

**Para hábitos de prompting, ahorro de tokens y memoria persistente →
`Docs/dev/CLAUDE_WORKFLOW_GUIDE.md`.**
