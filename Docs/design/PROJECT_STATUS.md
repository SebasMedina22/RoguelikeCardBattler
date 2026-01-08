## Project Status (Epic 1 cerrado)

### TL;DR
- Prototipo Unity de deckbuilder roguelite 1v1 con tipos elementales placeholder, efectividad de daño, Momentum (free plays al golpear debilidad) y cambio de mundo limitado.
- Epic 1 (Combat Core) está completo: tipos, efectividad, Momentum, limitación de Change World, feedback básico y tests EditMode en verde.
- Inmediato: Epic 2 (docs & onboarding) en curso (issues #5–#9).
- Próximo después de Epic 2: extender gameplay (mapa/recompensas/reliquias básicas) sin fechas cerradas.

### Estado actual — Epic 1 (Combat Core)
- 1.1 Tipos + matriz: `ElementType` placeholder (colores) y `ElementEffectiveness.GetEffectiveness`.
- 1.2 Tipos en cartas/enemigos + UI: `CardDefinition` y `EnemyDefinition` incluyen `ElementType`; UI muestra tipo.
- 1.3 Multiplicador por efectividad: daño de cartas del jugador aplica x1.5 (SuperEficaz) / x0.75 (PocoEficaz) / x1.0 (Neutro).
- 1.4 Momentum (free play): golpes SuperEficaz otorgan +1 free play; el siguiente PlayCard no gasta energía si hay Momentum.
- 1.5 Change World limitado: 1 cambio por combate; debug permite ilimitado; botón en UI.
- 1.6 Feedback UI: popups WEAK/RESIST/MOMENTUM +1; nombre público “Momentum” (antes “One More”).

### Decisiones de diseño
- Cerradas:
  - ElementType por color es placeholder; se renombrará a tipos finales más adelante.
  - “Momentum” es el nombre visible; internamente existe contador de free plays.
  - Change World sigue como botón placeholder; trigger final se discutirá (propietario: Julio).
- Pendientes:
  - Definir trigger “real” de cambio de mundo (por Momentum/carta/combo/fin de turno). Verificar con Julio.
  - Definir tipos finales (nombres, lore, iconografía).
  - Pipeline de animación (frame-by-frame vs rig) y cuándo integrarlo.

### Roadmap corto
- Epic 2 (en curso): docs & onboarding (#5–#9).
- Luego (siguiente razonable de gameplay, sin fechas):
  - Mapa/nodos P2 y flujo de progresión.
  - Recompensas post-combate y selección de cartas.
  - Reliquias/efectos básicos que interactúen con Momentum/efectividad.
  - Más feedback/animaciones cuando se defina pipeline.

### Cómo validar rápido
- Abrir escena: `Assets/Scenes/BattleScene.unity`.
- Tests EditMode: `Window > General > Test Runner` → EditMode → Run All.
- Config rápida de tipos: `Assets/ScriptableObjects/Cards/StrikeBasic.asset` y `Assets/ScriptableObjects/Enemies/WeakSlime.asset`; asigna tipos para ver WEAK/RESIST/Momentum en HUD/popups.

