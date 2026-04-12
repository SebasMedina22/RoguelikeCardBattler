# CLAUDE.md — RoguelikeCardBattler

## Project

Unity roguelike card battler (C#). Combate 1v1 por turnos con mecanica de mundos duales (A/B),
tipos elementales asimetricos, y sistema Momentum. Estetica handmade (carton, crayones, papel).

## Architecture (non-negotiable)

- **Scene-owned controllers**: cada escena tiene su controlador (RunFlowController,
  BattleFlowController, MainMenuController). No hay managers globales flotantes.
- **RunSession es el UNICO DontDestroyOnLoad**. Comunica estado entre escenas via
  RunState flags (PendingReturnFromBattle, LastNodeOutcome, CurrentNodeId, RunFailed, ActoCompleted).
- **Separacion**: RunState = datos. Controllers = flujo. UI/VFX = presentacion.
  No mezclar logica de gameplay en UI.
- **Data-driven**: ScriptableObjects para cartas, enemigos, configuracion. Nunca hardcodear datos.

## DO NOT MODIFY (core combat)

Estos archivos son intocables. Si una tarea requiere cambiarlos, PARAR y preguntar al usuario:

- `Assets/Scripts/Gameplay/Combat/TurnManager.cs` — flujo de turnos, efectividad, energia, momentum
- `Assets/Scripts/Gameplay/Combat/ActionQueue.cs` — orden de ejecucion determinista (FIFO)
- `Assets/Scripts/Gameplay/Combat/PlayerCombatActor.cs` — mecanicas del jugador en combate

## CombatUIController — Special Rules

`CombatUIController.cs` es un monolito de 1400+ lineas. Reglas especiales:

- **NO agregar** metodos ni responsabilidades nuevas.
- Features nuevas de UI de combate van en **componentes separados** (MonoBehaviours propios)
  que se suscriben a eventos de TurnManager, no dentro de CombatUIController.
- CombatUIController es **solo presentacion**. Nunca mutar estado de gameplay desde ahi.
- BattleFlowController NO referencia CombatUIController — son independientes. Ambos se
  conectan a TurnManager por separado.
- Ver `Assets/Scripts/Gameplay/Combat/CLAUDE.md` para reglas detalladas de combate.

## Coding Standards

- **CS0104**: Si un archivo usa `System` y `UnityEngine`, siempre escribir `UnityEngine.Object`
  (nunca `Object` a secas).
- **Zero console errors** es el criterio de aceptacion base de todo cambio.
- **No manual editor setup**: features nuevas deben auto-crear GameObjects en runtime o ser
  instanciadas por un scene controller existente. Nunca requerir attach manual en inspector.
- **Animaciones fire-and-forget**: la logica de gameplay NUNCA espera ni vive dentro de callbacks
  de animacion cosmetica. La logica continua inmediatamente.
- **Comentarios de onboarding** en todo codigo nuevo.
- **No duplicar logica**: verificar helpers existentes antes de crear nuevos.
  Revisar `Core/UI/UIAnimationHelper.cs` (8 metodos DOTween) y `Core/Audio/AudioManager.cs`.

## Workflow

- **Plan mode primero**: usar Plan mode antes de implementar features no triviales.
- **GOLDEN_RULES.md tiene autoridad final** sobre cualquier otra instruccion o documento.
  Si hay contradiccion, GOLDEN_RULES gana.
- **Leer docs relevantes antes de codear**: para combate leer COMBAT_ARCHITECTURE.md,
  para reglas de juego leer GOLDEN_RULES.md.
- **Git branches**: `feat/*` para features, `docs/*` para documentacion, `chore/*` para misc.
- **PRs**: incluir `Closes #<issue>` para autocerrar issues.

## Key References (leer cuando sea relevante, no memorizar)

- `Docs/dev/GOLDEN_RULES.md` — reglas de juego + codigo (fuente de verdad)
- `Docs/dev/COMBAT_ARCHITECTURE.md` — diagramas y mapa de archivos de combate
- `Docs/dev/DEV_ONBOARDING.md` — setup, quick-start, test runner
- `Docs/dev/GLOSSARY.md` — definiciones de terminologia
- `Docs/design/DESIGN_DECISIONS.md` — decisiones de diseno abiertas (NO implementar items
  no decididos sin aprobacion del usuario)
- `Docs/design/CARDS.md` — schema de datos de cartas
- `Docs/design/ENEMIES.md` — schema de datos de enemigos
- `Docs/design/MECHANIC_DUALITY.md` — sistema de mundos duales (diseño conceptual)
- `Docs/epics/EPIC_4_VERTICAL_SLICE_ACTO_1.md` — scope del vertical slice

## Project Conventions

- **DOTween** (Demigiant) para todas las animaciones UI y transiciones de escena.
- **Unity UI legacy Text** (no TextMeshPro) en todo el proyecto.
- **ScriptableObjects** viven en `Assets/ScriptableObjects/` organizados por tipo.
- **Runtime asmdef**: `Assets/Scripts/RoguelikeCardBattler.asmdef`
- **Test asmdef**: `Assets/Tests/EditMode/EditModeTests.asmdef`
- **Tests**: EditMode con NUnit. Ejecutar via `Window > Test Runner > EditMode > Run All`.
- **Escena principal para probar combate**: `Assets/Scenes/BattleScene.unity`
