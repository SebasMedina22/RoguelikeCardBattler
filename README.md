## Roguelike Card Battler (Unity)
Prototipo de deckbuilder roguelite 1v1 con tipos elementales, Momentum (free play al golpear debilidad) y cambio de mundo limitado por combate.

### What’s implemented (Epic 1)
- ElementType placeholder por color + matriz de efectividad.
- Daño modificado por efectividad (WEAK/RESIST popups).
- Momentum: golpes SuperEficaz otorgan free plays (HUD “Momentum: X”).
- Cambio de mundo limitado a 1 por combate (override debug).
- Tests EditMode corriendo en verde.

### Quickstart
- Abrir Unity y cargar `Assets/Scenes/BattleScene.unity`.
- Tests EditMode: `Window > General > Test Runner` → EditMode → Run All.

### Docs
- Onboarding dev: `Docs/dev/DEV_ONBOARDING.md`
- Índice de docs: `Docs/README.md`
- Diseño base (Proyecto C): `Docs/design/Proyecto C.pdf`

### Workflow
- Ramas: `feat/*`, `docs/*`, `chore/*`.
- PRs: usar `Closes #<issue>` para autocerrar el issue.

