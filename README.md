## Roguelike Card Battler (Unity)

Deckbuilder roguelite 1v1 con **mundos duales** (Medieval Oscuro / Cyberpunk),
**tipos elementales asimétricos** y cambio de mundo como mecánica diferenciadora.
Estética handmade (cartón, recortes, papel). Estado: prototipo en desarrollo activo.

### Estado actual

- **Combate 1v1 funcional**: turnos, energía, bloqueo, mano de cartas, `ActionQueue` determinista.
- **Sistema de tipos asimétrico**: 6 tipos + None, matriz de efectividad (1.5x / 1.0x / 0.75x).
- **Mundos duales**: cartas duales (sideA/sideB), cambio de mundo limitado por combate.
- **Vertical Slice Acto 1**: mapa de nodos generado con seed, recompensas post-combate, boss del acto.
- **UI de combate modular**: `CombatUIController` + 4 views especializados (`CombatFeedbackView`, `CardHandView`, `CombatHudView`, `CombatBackgroundView`).
- **Tests EditMode** en verde.

### Quickstart

- Abrir Unity 6.2 (6000.2.14f1) y cargar `Assets/Scenes/BattleScene.unity`.
- Tests EditMode: `Window > General > Test Runner` → EditMode → Run All.

### Docs

- **Identidad y reglas inmutables**: [`Docs/dev/GOLDEN_RULES.md`](Docs/dev/GOLDEN_RULES.md)
- **GDD vigente**: [`Docs/design/_gdd.md`](Docs/design/_gdd.md)
- **Decisiones de diseño abiertas**: [`Docs/design/DESIGN_DECISIONS.md`](Docs/design/DESIGN_DECISIONS.md)
- **Roadmap activo**: [`Docs/dev/_roadmap.md`](Docs/dev/_roadmap.md)
- **Snapshot técnico**: [`Docs/dev/_tech_snapshot.md`](Docs/dev/_tech_snapshot.md)
- **Onboarding dev**: [`Docs/dev/DEV_ONBOARDING.md`](Docs/dev/DEV_ONBOARDING.md)
- **Índice general de docs**: [`Docs/README.md`](Docs/README.md)

### Workflow

- Ramas: `feat/*`, `docs/*`, `chore/*`, `refactor/*`.
- PRs: usar `Closes #<issue>` para autocerrar el issue.
- Sistema de modos para colaboración con asistente de IA: ver [`CLAUDE.md`](CLAUDE.md) en la raíz.
