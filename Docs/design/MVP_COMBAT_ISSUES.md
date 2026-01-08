# MVP – Combate Básico (Milestone)

Objetivo: tener una escena de combate jugable con:
- 1 enemigo (`weak_slime`)
- 3 cartas básicas (`strike_basic`, `defend_basic`, `battle_focus`)
- Sistema de turnos y cola de acciones funcionales.

## Issues propuestos

### 1) `feat: inicializar proyecto y estructura de carpetas`

- Crear proyecto 2D en Unity.
- Añadir estructura de carpetas:
  - `Assets/Scripts/Core`
  - `Assets/Scripts/Gameplay/Cards`
  - `Assets/Scripts/Gameplay/Combat`
  - `Assets/Scripts/Gameplay/Enemies`
  - `Assets/Scenes`
- Añadir carpeta `Docs/` en la raíz del repo y colocar dentro `DESIGN.md`, `CARDS.md`, `ENEMIES.md` y este archivo.

### 2) `feat: modelo de datos de cartas y efectos`

- Implementar clases/estructuras para:
  - `Card`
  - `EffectRef`
- Asegurar que se pueden definir las 3 cartas del MVP según `CARDS.md`.

### 3) `feat: modelo de datos de enemigos y movimientos`

- Implementar clases/estructuras para:
  - `Enemy`
  - `EnemyMove`
- Asegurar que se puede definir el enemigo `weak_slime` según `ENEMIES.md`.

### 4) `feat: sistema de Action Queue`

- Definir interfaz/base para acciones (`GameAction` o similar).
- Implementar acciones mínimas:
  - `DamageAction`
  - `BlockAction`
  - `DrawCardsAction`
- Implementar `ActionQueue` que procese acciones en orden.

### 5) `feat: sistema de turnos (TurnManager)`

- Manejar fases de turno:
  - Turno del jugador (puede jugar cartas mientras tenga energía).
  - Turno del enemigo (elige y ejecuta un `EnemyMove`).
- Integrar `ActionQueue` con el flujo de turnos.

### 6) `feat: escena de combate y UI mínima`

- Crear una escena `BattleScene`.
- Mostrar:
  - HP del jugador.
  - HP del enemigo.
  - Energía del jugador.
  - Mano de cartas del jugador (con botones o elementos clicables).
- Permitir:
  - Jugar carta objetivo enemigo (al menos para `strike_basic`).
  - Terminar turno del jugador manualmente.

### 7) `test: pruebas básicas de lógica de combate`

- Tests unitarios o de integración mínima para:
  - Daño no negativo (no bajar de 0 HP).
  - Bloque aplicado antes de daño.
  - Secuencia básica de turno (jugador → enemigo → jugador).


