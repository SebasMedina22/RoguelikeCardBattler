# CARDS – Esquema y ejemplos (MVP)

## Esquema de datos de `Card`

Campos propuestos para cada carta:

- `id` (string, único)
- `name` (string)
- `description` (string)
- `cost` (int)
- `type` (enum): Attack, Skill, Power, Curse, Status
- `rarity` (enum): Common, Uncommon, Rare, Legendary
- `target` (enum): Self, SingleEnemy, AllEnemies, None
- `tags` (lista de strings): palabras clave para sinergias (`"Basic"`, `"Physical"`, `"Defense"`, etc.)
- `effects` (lista de `EffectRef`)

### Esquema de `EffectRef` (referencia a un efecto)

- `effectType` (enum): Damage, Block, DrawCards, GainEnergy, ApplyStatus, Heal
- `value` (int): valor base (daño, bloque, cartas a robar, etc.)
- `target` (enum): Self, SingleEnemy, AllEnemies
- `statusType` (opcional, enum): Poison, Weak, Vulnerable, Custom
- `extraParams` (opcional, mapa key→value): para cosas más avanzadas.

---

## Cartas ejemplo (MVP inicial)

### 1. Strike (ataque básico)

- `id`: `strike_basic`
- `name`: `Golpe`
- `description`: `Inflige 6 de daño a un enemigo.`
- `cost`: 1
- `type`: `Attack`
- `rarity`: `Common`
- `target`: `SingleEnemy`
- `tags`: [`Basic`, `Attack`]
- `effects`:
  - `effectType`: `Damage`
  - `value`: 6
  - `target`: `SingleEnemy`

### 2. Defend (defensa básica)

- `id`: `defend_basic`
- `name`: `Defensa`
- `description`: `Gana 5 de bloque.`
- `cost`: 1
- `type`: `Skill`
- `rarity`: `Common`
- `target`: `Self`
- `tags`: [`Basic`, `Defense`]
- `effects`:
  - `effectType`: `Block`
  - `value`: 5
  - `target`: `Self`

### 3. Battle Focus (buff simple)

- `id`: `battle_focus`
- `name`: `Foco de Batalla`
- `description`: `Roba 2 cartas adicionales en este turno.`
- `cost`: 1
- `type`: `Skill`
- `rarity`: `Uncommon`
- `target`: `Self`
- `tags`: [`Buff`, `CardDraw`]
- `effects`:
  - `effectType`: `DrawCards`
  - `value`: 2
  - `target`: `Self`


