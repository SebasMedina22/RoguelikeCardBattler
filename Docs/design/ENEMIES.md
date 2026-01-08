# ENEMIES – Esquema y ejemplos (MVP)

## Esquema de datos de `Enemy`

Campos propuestos:

- `id` (string, único)
- `name` (string)
- `maxHP` (int)
- `baseBlock` (int, opcional)
- `moves` (lista de `EnemyMove`)
- `aiPattern` (enum): RandomWeighted, Sequence, PhaseBased
- `tags` (lista de strings): para clasificación (`"Slime"`, `"Basic"`, etc.)

### Esquema de `EnemyMove`

- `id` (string)
- `name` (string)
- `description` (string)
- `effects` (lista de `EffectRef`) – mismo esquema que en las cartas
- `weight` (int, opcional): usado si `aiPattern` = `RandomWeighted`
- `sequenceIndex` (int, opcional): usado si `aiPattern` = `Sequence`

---

## Enemigo ejemplo (MVP inicial)

### Slime Débil

- `id`: `weak_slime`
- `name`: `Slime Débil`
- `maxHP`: 30
- `tags`: [`Basic`, `Slime`]
- `aiPattern`: `RandomWeighted`

#### Moves

1. **Golpe Baboso**
   - `id`: `slime_tackle`
   - `name`: `Golpe Baboso`
   - `description`: `Inflige 7 de daño.`
   - `effects`:
     - `effectType`: `Damage`
     - `value`: 7
     - `target`: `SingleEnemy` (el jugador)
   - `weight`: 60

2. **Reforzar Babas**
   - `id`: `slime_harden`
   - `name`: `Reforzar Babas`
   - `description`: `Gana 6 de bloque.`
   - `effects`:
     - `effectType`: `Block`
     - `value`: 6
     - `target`: `Self`
   - `weight`: 40


