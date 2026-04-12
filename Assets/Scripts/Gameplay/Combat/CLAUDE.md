# Combat Directory Rules

## Protected Files (DO NOT MODIFY without explicit user approval)

- `TurnManager.cs` (~735 loc) — orquesta turnos, juego de cartas, cambio de mundo, momentum, calculo de efectividad
- `ActionQueue.cs` (~85 loc) — cola de acciones determinista FIFO
- `PlayerCombatActor.cs` (~236 loc) — HP, bloqueo, aplicacion de dano del jugador

Si una tarea requiere modificar estos archivos, PARAR y pedir aprobacion explicita.

## CombatUIController.cs — Architectural Debt

Este archivo tiene 1400+ lineas y 47 metodos. Es un monolito conocido. Reglas:

1. **NO agregar metodos ni responsabilidades nuevas.**
2. **UI nueva** = MonoBehaviours separados conectados via BattleFlowController, NO dentro de CombatUIController.
3. Si se modifica un metodo existente: cambiar SOLO ese metodo. No reorganizar ni refactorizar codigo circundante a menos que se pida explicitamente.
4. Todo el codigo en este archivo es **solo presentacion**. Nunca mutar estado de gameplay.
5. **Plan de extraccion en progreso** (ver `Docs/dev/COMBAT_ARCHITECTURE.md`):
   - `CombatFeedbackView` — popups WEAK/RESIST/MOMENTUM, shake de dano **(Fase 1 — en progreso)**
   - `CardHandView` — mano de cartas, seleccion, animaciones de juego (Fase 2)
   - `CombatHudView` — paneles de HP, energia, momentum, mundo (Fase 3)
   - `CombatBackgroundView` — fondos de mundo A/B, parallax (Fase 4)

## Turn Flow (NO alterar orden)

1. Turno jugador: robar cartas → jugar cartas → fin de turno
2. Turno enemigo: limpiar bloqueo → ejecutar movimiento → planear siguiente → verificar fin
3. Efectos se resuelven via `ActionQueue.ProcessAll()` (deterministico, secuencial)

## Key Patterns

- **Efectividad**: SOLO aplica a dano de carta del jugador vs tipo del enemigo. NUNCA a dano del enemigo, bloqueo, robo ni otro efecto.
- **Momentum**: se otorga en hit SuperEficaz con dano > 0. Se consume al jugar la siguiente carta (gratis). Nunca al seleccionar.
- **Cambio de mundo**: limitado por combate (configurable). Afecta lado activo de cartas duales.
- **Acciones**: `DamageAction`, `BlockAction`, `DrawCardsAction` implementan `IGameAction`. Nuevos tipos siguen este patron.

## Adding New Combat Features

1. Leer `Docs/dev/COMBAT_ARCHITECTURE.md` para diagramas y flujo de datos.
2. **Nuevas acciones**: crear en `Actions/` implementando `IGameAction`.
3. **Nueva UI de combate**: crear MonoBehaviour separado. NO extender CombatUIController.
4. **Nuevos datos**: agregar campos en ScriptableObjects. Nunca hardcodear valores.
5. **Nuevos enemigos/patrones**: definir en `EnemyDefinition` SO. IA en `EnemyEnums.AiPattern`.
