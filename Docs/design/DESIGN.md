# DESIGN – Roguelike por turnos tipo Slay the Spire (MVP)

## Visión

Juego de combate por turnos inspirado en Slay the Spire, pero con contenido original.  
Objetivo inicial: construir un prototipo donde el jugador pueda luchar contra un enemigo usando un pequeño mazo de cartas.

En fases posteriores se añadirán:
- Mapa por nodos (eventos, combates, tiendas, jefe).
- Sistema de reliquias (pasivas).
- Historia y universo propios.

## Tecnologías (propuesta)

- Lenguaje: C#
- Motor recomendado: Unity 2D
- Control de versiones: Git + GitHub
- Gestión de tareas: GitHub Issues + Milestones

> Nota: el diseño de sistemas es agnóstico de motor y podría implementarse también en libGDX/Java u otros engines.

## Sistemas principales (MVP)

1. **Sistema de Cartas**
   - Cartas como datos (`Card`) que referencian efectos reutilizables (`EffectRef`).
   - Cada carta define coste, tipo, objetivo y una lista de efectos.

2. **Sistema de Efectos**
   - Efectos atómicos como "hacer daño", "ganar bloque", "robar cartas", "aplicar estado".

3. **Sistema de Cola de Acciones (Action Queue)**
   - Cuando se juega una carta, se generan una o más acciones.
   - La cola procesa acciones en orden, aplicando efectos sobre jugador y enemigo.

4. **Sistema de Turnos**
   - Turno del jugador: puede jugar cartas hasta quedarse sin energía.
   - Turno del enemigo: ejecuta uno de sus movimientos definidos.
   - Cambio de turno controlado por un `TurnManager`.

5. **Enemigos**
   - Cada enemigo tiene vida máxima, intents/movimientos y un patrón simple (aleatorio ponderado o secuencial).

6. **UI mínima de combate**
   - Mostrar HP del jugador y del enemigo.
   - Mostrar energía disponible.
   - Mostrar mano de cartas del jugador.
   - Permitir jugar carta sobre enemigo.

## Alcance del MVP

- 1 escena de combate.
- 1 enemigo con 2 movimientos distintos.
- 3 cartas para el jugador (ataque, defensa, mejora).

El foco es tener **un ciclo de combate jugable de inicio a fin**, no el mapa ni la historia todavía.


