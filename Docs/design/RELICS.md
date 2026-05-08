# RELICS — Catálogo de Retazos placeholder (M3 Sub-PR 3B)

> **Status:** pool cerrado + nombres/flavor placeholder aprobados (2026-05-08). Listo para `modo:implementacion` (Sub-PR 3B). Los nombres y flavor text son placeholder funcional — refinables en playtest cuando aparezca arte real.
> **Origen:** M3 Sub-PR 3B (`_roadmap.md`), Insight 3 (6 categorías de hook), GOLDEN_RULES §10, DD-017 cerrada (opción C).
> **Modo:** `modo:diseno`
> **Fecha:** 2026-05-08

---

## Origen

- **Roadmap:** Sub-PR 3B requiere ~23 Retazos placeholder distribuidos en:
  - 15 Neutros distribuidos por las 6 categorías de hook (Insight 3).
  - 5 de Elite (drop garantizado al ganar Elite).
  - 1 de Boss (drop tras vencer BossAct1, vinculado narrativamente).
  - 2–3 de Cambio (DD-017 opción C: demo de la categoría `Switch` en contenido base).
- **Infra disponible (Sub-PR 3A):** 9 hooks (`OnCombatStart`, `OnPlayerTurnStart`, `OnDamageDealt`, `OnDamageTaken`, `OnWorldSwitch`, `OnCombatEnd`, `OnCardPlayed`, `OnCampfireOptionsBuilt`, `OnShopStockBuilt`) + API `RelicHookContext` con 7 métodos: `GrantBlock`, `GrantHeal`, `GrantDrawCards`, `GrantEnergy`, `GrantStyleCharge`, `GrantBonusWorldSwitch`, `EnqueueExtraDamage`. Mutación directa de `RunState` permitida en `OnCombatEnd`.
- **GOLDEN_RULES §10:** un Retazo NUNCA debe resolver un combate por sí solo. Amplifica decisiones, no las sustituye.

---

## Schema común

Todos los Retazos comparten el mismo wrapper `RelicDefinition` SO con `Effect : IRelicEffect [SerializeReference]`. Los campos del SO:

```
DisplayName       : string                 ← Sebastián decide
Description       : string (TextArea)      ← texto mecánico, claro
FlavorText        : string (TextArea)      ← texto narrativo (handmade/cartón)
Icon              : Sprite                 ← placeholder en 3B
Category          : RelicCategory          ← Neutral | Switch | World
Hooks             : RelicHook[]            ← qué hooks escucha
Effect            : IRelicEffect           ← clase concreta con campos serializables
```

**Convenciones de este catálogo:**
- `[PROPUESTA]` valor numérico — abierto a ajuste de balance.
- `[ABIERTO]` decisión que Sebastián tiene que cerrar antes de implementar.
- Cada propuesta lista: **mecánica** + **hook(s) usado(s)** + **API/payload** + **valor numérico** + **rareza sugerida** (Común / No común / Raro / Elite / Boss).

---

## Advertencias técnicas que afectan el contenido (leer antes de elegir)

1. **Cartas neutras y `OnDamageDealt`** — Insight 5: hoy el dispatch de `OnDamageDealt` NO se dispara en el path `attackerType == ElementType.None`. Cualquier Retazo de "Modificador de daño ofensivo" basado en `OnDamageDealt` requiere agregar el 8º punto de inserción en `TurnManager` (ya marcado en roadmap 3B con `[REQUIERE APROBACIÓN]`). Decisión confirmada en Insight 5 = opción A (extender al path None). Sin ese cambio, los Retazos ofensivos serán contraintuitivos al jugar Strike neutro.
2. **`EnqueueExtraDamage` no re-dispara `OnDamageDealt`** — daño "raw" sin efectividad ni cargas de Estilo. Si querés que el daño extra se beneficie de WEAK/RESIST, mutá `data.Amount` en `OnDamageDealt`; no uses `EnqueueExtraDamage`.
3. **`GrantEnergy` solo aplica al player** ([IMPL 4] del spec 3A) — no-op silencioso para enemigos.
4. **`OnCombatEnd` muta `RunState` directamente** ([CERRADO 3]) — para +oro, heal post-combate, etc. NO encolar acciones.
5. **`Counters` los maneja el efecto** — el dispatcher no toca counters. Cada efecto resetea sus claves en el hook que tenga sentido (`OnPlayerTurnStart` para por-turno, `OnCombatEnd` para por-combate).
6. **Orden por `AcquisitionOrder` ascendente** — relevante para Retazos que se encadenan (ej: "+5 daño" antes que "x2 daño" da resultado distinto al inverso). Sebastián puede usar esto como vector de build.

---

## Distribución sugerida del pool inicial (23 Retazos)

| Slot | Categoría hook | Cantidad | RelicCategory |
|------|----------------|----------|---------------|
| 1–3  | Apertura de combate | 3 | Neutral |
| 4–6  | Inicio de turno | 3 | Neutral |
| 7–9  | Modificador de daño | 3 | Neutral |
| 10–11 | Acumulador / trigger | 2 | Neutral |
| 12–14 | Economía / fin de combate | 3 | Neutral |
| 15   | Cambio de mundo (neutro) | 1 | Neutral |
| 16–18 | Cambio de mundo (DD-017) | 3 | **Switch** |
| 19–22 | Drops de Elite (más fuertes) | 4 | Neutral o Switch (Sebastián elige) |
| 23   | Drop de Boss (único, narrativo) | 1 | Neutral |

Total: 23. Las 30 propuestas que siguen son un menú — Sebastián elige cuáles entran al pool y cuáles quedan en backlog para sub-PRs futuras.

---

## CATÁLOGO

### Categoría 1 — Apertura de combate (`OnCombatStart`)

> **Hook:** `OnCombatStart`. **Payload:** `CombatStartHookData { EnemyDefinition Enemy, bool IsBoss, bool IsElite }`.
> **Tip de balance:** efectos de apertura son fuertes en combates cortos y débiles en largos. Valores chicos (3–6) suelen ser suficientes.

**R-OPEN-1 — "Bloque inicial chico"**
- Mecánica: empezás cada combate con N de bloque.
- Implementación: `ctx.GrantBlock(player, N)` en `OnCombatStart`.
- Valor: `[PROPUESTA] N = 4`.
- Rareza: Común. **Recomendado para pool inicial (slot 1).**

**R-OPEN-2 — "Mano inicial extendida"**
- Mecánica: empezás cada combate con +N cartas en la mano.
- Implementación: `ctx.GrantDrawCards(player, N)` en `OnCombatStart`.
- Valor: `[PROPUESTA] N = 1`.
- Rareza: No común. Puede colisionar con hand-limit del player — verificar que `DrawCards` respeta el cap o si el cap se levanta durante este draw extra. `[ABIERTO]` ¿Permitir overflow temporal o capear?
- **Recomendado para pool inicial (slot 2).**

**R-OPEN-3 — "Energía de apertura"**
- Mecánica: empezás cada combate con +N de energía adicional este turno.
- Implementación: `ctx.GrantEnergy(player, N)` en `OnCombatStart`.
- Valor: `[PROPUESTA] N = 1`.
- Rareza: Raro. La energía extra del primer turno permite combos tempranos fuertes — cuidar que no rompa combates cortos. **Recomendado para pool inicial (slot 3).**

**R-OPEN-4 — "Carga inicial de Estilo"**
- Mecánica: empezás cada combate con N cargas de Estilo.
- Implementación: `ctx.GrantStyleCharge(N)` en `OnCombatStart`.
- Valor: `[PROPUESTA] N = 2`.
- Rareza: No común. Requiere que el motor maneje el caso "cargar Estilo sin haber hecho SuperEficaz" — la API ya lo soporta (es delegación pura a la lógica del Contador). Backlog si Sebastián prefiere las 3 anteriores.

---

### Categoría 2 — Inicio de turno (`OnPlayerTurnStart`)

> **Hook:** `OnPlayerTurnStart`. **Payload:** `PlayerTurnStartHookData { WorldSide CurrentWorld }`.

**R-TURN-1 — "Robo extra"**
- Mecánica: robás +N cartas extra al inicio de cada turno.
- Implementación: `ctx.GrantDrawCards(player, N)` en `OnPlayerTurnStart`.
- Valor: `[PROPUESTA] N = 1`.
- Rareza: Común. **Recomendado para pool inicial (slot 4).**

**R-TURN-2 — "Bloque cíclico"**
- Mecánica: ganás N de bloque al inicio de cada turno.
- Implementación: `ctx.GrantBlock(player, N)` en `OnPlayerTurnStart`.
- Valor: `[PROPUESTA] N = 3`.
- Rareza: Común. Nota: el bloque se otorga DESPUÉS de `ClearBlock` (orden del hook) — el bloque persiste el turno. **Recomendado para pool inicial (slot 5).**

**R-TURN-3 — "Energía recurrente"**
- Mecánica: cada N turnos, +1 energía al inicio del turno siguiente.
- Implementación: `Counters["turnsSeen"]++` en `OnPlayerTurnStart`; cuando llega a N, `ctx.GrantEnergy(player, 1)` y resetear counter a 0.
- Valor: `[PROPUESTA] N = 3`.
- Rareza: No común. **Recomendado para pool inicial (slot 6).**

**R-TURN-4 — "Heal lento"**
- Mecánica: heal N HP al inicio de cada turno.
- Implementación: `ctx.GrantHeal(player, N)` en `OnPlayerTurnStart`.
- Valor: `[PROPUESTA] N = 1`.
- Rareza: No común. Riesgo: en combates muy largos puede estallar — capear con HP máximo lo limita naturalmente. Backlog.

**R-TURN-5 — "Energía si la mano está vacía"**
- Mecánica: si terminaste el turno anterior con 0 cartas en mano, +1 energía este turno.
- Implementación: requiere snapshot de hand size al final del turno (no hay hook `OnPlayerTurnEnd` — `[ABIERTO]` ¿agregar 10º hook o usar workaround?). Workaround: leer `_player.Hand.Count` en `OnPlayerTurnStart` previo a `DrawCards` no funciona — el draw ya ocurrió en `BeginPlayerTurn`. Requiere infra adicional.
- Rareza: Raro. **Backlog hasta que aparezca `OnPlayerTurnEnd` o equivalente.** No incluir en 3B.

---

### Categoría 3 — Modificador de daño (`OnDamageDealt` / `OnDamageTaken`)

> **Hooks:** `OnDamageDealt` (mutable) y `OnDamageTaken` (mutable). Payloads tienen `int Amount` mutable + `Effectiveness`, `ElementType`, `ICombatActor`.
> **Advertencia:** ver §Advertencias punto 1 — sin el 8º punto de inserción aprobado, ninguno de estos efectos aplica a cartas neutras.

**R-DMG-1 — "Boost universal de ataque"**
- Mecánica: tus cartas de ataque hacen +N daño.
- Implementación: en `OnDamageDealt`, `data.Amount += N`.
- Valor: `[PROPUESTA] N = 2`.
- Rareza: Común. **Recomendado para pool inicial (slot 7).**

**R-DMG-2 — "Primer ataque del combate"**
- Mecánica: tu primer ataque de cada combate hace +N daño.
- Implementación: `Counters["firstHitDone"]` (default 0). En `OnDamageDealt`: si counter == 0, `data.Amount += N` y setear counter a 1. Resetear en `OnCombatStart` (o aprovechar que `RelicInstance.Counters` es per-combate gracias a que el dispatcher no resetea — `[ABIERTO]` ¿reset explícito en `OnCombatEnd` o en `OnCombatStart`? Recomiendo `OnCombatStart` para que la primera vez quede consistente).
- Valor: `[PROPUESTA] N = 5`.
- Rareza: No común. **Recomendado para pool inicial (slot 8).**

**R-DMG-3 — "Resistencia genérica"**
- Mecánica: el daño que recibís se reduce en N (mínimo 0).
- Implementación: en `OnDamageTaken`, `data.Amount = Mathf.Max(0, data.Amount - N)`.
- Valor: `[PROPUESTA] N = 1`.
- Rareza: Común. **Recomendado para pool inicial (slot 9).**

**R-DMG-4 — "Combo de cargas"**
- Mecánica: si tenés ≥3 cargas de Estilo, tus ataques hacen +N daño.
- Implementación: en `OnDamageDealt`, leer `ctx.TurnManager.StyleCharges` (verificar que la propiedad sea pública o exponerla — `[ABIERTO]`). Si ≥3, `data.Amount += N`.
- Valor: `[PROPUESTA] N = 3`.
- Rareza: No común. Sinergia con build de cambio de mundo. Backlog si Sebastián prefiere los 3 anteriores.

**R-DMG-5 — "Vampírico ligero"**
- Mecánica: cuando hacés daño SuperEficaz, heal N HP.
- Implementación: en `OnDamageDealt`, si `data.Effectiveness == SuperEficaz`, `ctx.GrantHeal(player, N)`.
- Valor: `[PROPUESTA] N = 1`.
- Rareza: Raro. Backlog candidato fuerte para Elite.

**R-DMG-6 — "Espinas"**
- Mecánica: cuando recibís daño, devolvés N daño al enemigo.
- Implementación: en `OnDamageTaken`, `ctx.EnqueueExtraDamage(data.Source, N, ElementType.None)`. Recordar: NO re-dispara `OnDamageDealt` ni aplica efectividad.
- Valor: `[PROPUESTA] N = 2`.
- Rareza: No común. Backlog candidato para Elite.

---

### Categoría 4 — Acumulador / trigger (`OnCardPlayed` + counters)

> **Hook:** `OnCardPlayed`. **Payload:** `CardPlayedHookData { CardDefinition Card, WorldSide PlayedInWorld, int EnergySpent }`.

**R-ACC-1 — "Skill stacker"**
- Mecánica: cada N cartas Skill jugadas en el mismo turno → +1 energía el turno siguiente.
- Implementación: en `OnCardPlayed`, si `Card.Type == Skill`, `Counters["skillsThisTurn"]++`. Si llega a N, `Counters["energyPending"]++` y resetear `skillsThisTurn`. En `OnPlayerTurnStart`: si `energyPending > 0`, `ctx.GrantEnergy(player, energyPending)` y resetear. También resetear `skillsThisTurn` al inicio de turno.
- Valor: `[PROPUESTA] N = 3`.
- Rareza: No común. **Recomendado para pool inicial (slot 10).**
- `[ABIERTO]` ¿Existe `CardType.Skill` en el código? Confirmar nombre del enum.

**R-ACC-2 — "Tercer ataque"**
- Mecánica: cada 3er ataque del combate hace +N daño.
- Implementación: en `OnDamageDealt`, `Counters["attacks"]++`. Si `attacks % 3 == 0`, `data.Amount += N`. Resetear en `OnCombatStart`.
- Valor: `[PROPUESTA] N = 4`.
- Rareza: No común. **Recomendado para pool inicial (slot 11).**

**R-ACC-3 — "Bloque por cartas jugadas"**
- Mecánica: cada N cartas jugadas en un turno → +M de bloque al final del turno siguiente. **Problema:** sin hook `OnPlayerTurnEnd`, el "al final" no es disparable directo. Workaround: aplicar el bloque al inicio del turno siguiente (`OnPlayerTurnStart`).
- Implementación: counter `cardsThisTurn` en `OnCardPlayed`. En `OnPlayerTurnStart` siguiente: `floor(cardsThisTurn / N) * M` de bloque, resetear contador.
- Valor: `[PROPUESTA] N = 4, M = 3`.
- Rareza: Raro. Backlog.

**R-ACC-4 — "Rompecadenas"**
- Mecánica: si jugás N cartas del mismo tipo elemental en un turno, el siguiente ataque hace +M daño.
- Implementación: counters `lastType`, `streak`. En `OnCardPlayed`: si `Card.Element == lastType` → `streak++`; sino → `lastType = Card.Element; streak = 1`. Si `streak >= N`, set flag `nextAttackBoost = M`. En siguiente `OnDamageDealt`, consumir flag.
- Valor: `[PROPUESTA] N = 3, M = 6`.
- Rareza: Raro. Backlog candidato Elite.

---

### Categoría 5 — Economía / fin de combate (`OnCombatEnd`)

> **Hook:** `OnCombatEnd`. **Payload:** `CombatEndHookData { bool Victory, EnemyDefinition Enemy }`.
> **Acceso a `RunState` directo** ([CERRADO 3] del spec 3A). Mutación de `Gold`, `PlayerCurrentHP` permitida.

**R-END-1 — "Botín extra"**
- Mecánica: +N oro al ganar cada combate.
- Implementación: en `OnCombatEnd`, si `data.Victory`, `ctx.RunState.Gold += N`.
- Valor: `[PROPUESTA] N = 5`.
- Rareza: Común. **Recomendado para pool inicial (slot 12).**

**R-END-2 — "Heal post-combate"**
- Mecánica: heal N HP al ganar cada combate.
- Implementación: en `OnCombatEnd`, si `data.Victory`, mutación directa `ctx.RunState.PlayerCurrentHP = Mathf.Min(maxHp, current + N)` o usar `ctx.GrantHeal(player, N)` (verificar que el actor siga referenciable post-Victory — `[ABIERTO]`; si no, mutación directa de RunState es la vía segura).
- Valor: `[PROPUESTA] N = 4`.
- Rareza: Común. **Recomendado para pool inicial (slot 13).**

**R-END-3 — "Bono de elite"**
- Mecánica: +N oro extra al ganar combates de Elite.
- Implementación: en `OnCombatEnd`, si `data.Victory && data.IsElite` (campo diferido en payload — `[ABIERTO]` ¿se cablea ya en 3B junto con el Retazo, o el call site del combate todavía no inyecta IsElite?). Si no llega aún, fallback: detectar por `EnemyDefinition` (flag custom) — más frágil.
- Valor: `[PROPUESTA] N = 10`.
- Rareza: No común. **Recomendado para pool inicial (slot 14) SI se cabla `IsElite` en 3B.** Si no, postergar.

**R-END-4 — "Recompensa de purista"**
- Mecánica: si terminaste el combate con HP completo, +N oro.
- Implementación: en `OnCombatEnd`, si `data.Victory && ctx.RunState.PlayerCurrentHP == maxHp`, `ctx.RunState.Gold += N`.
- Valor: `[PROPUESTA] N = 8`.
- Rareza: Raro. Backlog candidato Elite.

---

### Categoría 6 — Cambio de mundo (`OnWorldSwitch`)

> **Hook:** `OnWorldSwitch`. **Payload:** `WorldSwitchHookData { WorldSide From, WorldSide To }`.
> **DD-017 cerrada — opción C:** 2–3 Retazos `Switch` entran al pool base.

**R-SW-1 — "Bloque al cambiar"** (RelicCategory.Switch)
- Mecánica: al cambiar de mundo, +N bloque.
- Implementación: en `OnWorldSwitch`, `ctx.GrantBlock(player, N)`.
- Valor: `[PROPUESTA] N = 5`.
- Rareza: Común (Switch). **Recomendado para pool inicial DD-017 (slot 16).**

**R-SW-2 — "Carga al cambiar"** (RelicCategory.Switch)
- Mecánica: al cambiar de mundo, +1 carga de Estilo.
- Implementación: en `OnWorldSwitch`, `ctx.GrantStyleCharge(1)`. La lógica de "5 cargas → bonus switch" se ejecuta sola dentro de la API (ver [CERRADO 4] del spec 3A).
- Rareza: No común (Switch). Sinergia explícita con build de switch-spam. **Recomendado para pool inicial DD-017 (slot 17).**
- `[ABIERTO]` ¿Riesgo de loop? `GrantStyleCharge` puede desencadenar un bonus switch automático cuando llega a 5. Pero el bonus switch NO se "ejecuta" desde la API — solo queda disponible. No hay reentrada.

**R-SW-3 — "Daño de transición"** (RelicCategory.Switch)
- Mecánica: al cambiar de mundo, encolá N daño extra al enemigo.
- Implementación: en `OnWorldSwitch`, `ctx.EnqueueExtraDamage(enemy, N, ElementType.None)`. Recordar: daño raw, no aplica efectividad ni cargas.
- Valor: `[PROPUESTA] N = 5`.
- Rareza: No común (Switch). **Recomendado para pool inicial DD-017 (slot 18).**

**R-SW-4 — "Heal al cambiar"** (Neutral, hook `OnWorldSwitch` pero no es de la categoría Switch — solo escucha el hook; semánticamente diferente, lo discutimos)
- Mecánica: al cambiar de mundo, heal N HP.
- Implementación: en `OnWorldSwitch`, `ctx.GrantHeal(player, N)`.
- Valor: `[PROPUESTA] N = 2`.
- Rareza: No común. `[ABIERTO]` ¿Esto cuenta como Neutral o Switch? **Sugerencia:** la categoría es organizativa para tooltips/lore, no técnica. Yo lo pondría en `Neutral` para que el slot 15 (cambio de mundo neutro) tenga un candidato y los 3 slots de DD-017 queden con los efectos más distintivos.
- **Recomendado para pool inicial (slot 15).**

**R-SW-5 — "Primer cambio del combate"**
- Mecánica: el primer cambio de mundo de cada combate hace +N daño extra.
- Implementación: counter `firstSwitchDone`. En `OnWorldSwitch`: si `firstSwitchDone == 0`, `ctx.EnqueueExtraDamage(enemy, N, ElementType.None)` y setear flag. Resetear en `OnCombatStart`.
- Valor: `[PROPUESTA] N = 8`.
- Rareza: Raro. Backlog candidato Elite.

---

### Drops de Elite (5 propuestas, slots 19–22 — Sebastián elige 4)

> Filosofía: efectos más fuertes o con sinergia explícita. Drop garantizado por Elite (GOLDEN_RULES §8). Pueden ser Neutral o Switch a criterio de Sebastián.

**R-ELITE-1 — "Vampírico" (mejorado de R-DMG-5)**
- Mecánica: al hacer daño SuperEficaz, heal N HP.
- Valor: `[PROPUESTA] N = 2`.
- Rareza: Elite.

**R-ELITE-2 — "Espinas reforzadas" (mejorado de R-DMG-6)**
- Mecánica: al recibir daño, devolvés N raw al atacante.
- Valor: `[PROPUESTA] N = 3`.
- Rareza: Elite.

**R-ELITE-3 — "Recompensa de purista" (R-END-4)**
- Mecánica: terminar combate con HP lleno → +N oro.
- Valor: `[PROPUESTA] N = 10`.
- Rareza: Elite.

**R-ELITE-4 — "Combo cargado" (mejorado de R-DMG-4)**
- Mecánica: con ≥3 cargas de Estilo, tus ataques hacen +N daño.
- Valor: `[PROPUESTA] N = 4`.
- Rareza: Elite.

**R-ELITE-5 — "Reciclador"**
- Mecánica: al cambiar de mundo, robás 1 carta.
- Implementación: `ctx.GrantDrawCards(player, 1)` en `OnWorldSwitch`.
- Rareza: Elite (Switch o Neutral según Sebastián).

---

### Drop de Boss (1 propuesta — narrativo)

> GOLDEN_RULES §10: vinculado narrativamente al boss derrotado, mayor impacto que comunes, único.

**R-BOSS-1 — "Boss Acto 1: Costura Maldita"**
- Mecánica candidata A: tus cargas de Estilo se acumulan al doble (cada SuperEficaz da +2 cargas en lugar de +1).
- Mecánica candidata B: el primer cambio de mundo de cada combate es gratis (no consume del pool).
- Mecánica candidata C: al ganar un combate, el siguiente combate empezás con +1 energía y +5 bloque.
- `[ABIERTO]` Sebastián decide cuál es la mecánica del boss + nombre + flavor narrativo.
- Implementación A: hook `OnPlayerTurnStart` u `OnDamageDealt` — pero requiere modificar el `IncrementStyleCharges` desde fuera de TurnManager. Más complejo. **No es la mejor opción para 3B sin más cableado.**
- Implementación B: counter `combatsSwitches`. En `OnWorldSwitch`, si counter==0, `ctx.GrantBonusWorldSwitch()` y setear counter a 1. Resetear en `OnCombatStart`. Esto efectivamente "regala" un switch.
  - Limitación: `GrantBonusWorldSwitch` solo aplica si `_bonusWorldSwitches == 0`; verificar que el combo "Carga al cambiar (R-SW-2)" + este Retazo no se canibalicen — `[ABIERTO]`.
- Implementación C: `Counters["wonLastCombat"]` set en `OnCombatEnd` con Victory; en `OnCombatStart` consumir y aplicar `GrantEnergy(player, 1)` + `GrantBlock(player, 5)`. **Más simple y limpia.**
- **Recomendación:** opción C como base mecánica para 3B (limpia, sin reabrir cableado de TurnManager). Sebastián cierra texto + nombre.

---

## Resumen del pool sugerido (23 Retazos)

| Slot | Código | DisplayName | Mecánica corta | Hook | RelicCategory | Fuente de obtención |
|------|--------|-------------|----------------|------|---------------|---------------------|
| 1 | R-OPEN-1 | Tapa de Caja de Galletas | +4 bloque al iniciar combate | OnCombatStart | Neutral | Pool general |
| 2 | R-OPEN-2 | Bolsillo Roto | +1 carta en mano inicial | OnCombatStart | Neutral | Pool general |
| 3 | R-OPEN-3 | Sorbo Robado | +1 energía en T1 | OnCombatStart | Neutral | Pool general |
| 4 | R-TURN-1 | Mano de Más | +1 carta cada turno | OnPlayerTurnStart | Neutral | Pool general |
| 5 | R-TURN-2 | Almohadón Reforzado | +3 bloque cada turno | OnPlayerTurnStart | Neutral | Pool general |
| 6 | R-TURN-3 | Reloj de Cocina | Cada 3 turnos +1 energía | OnPlayerTurnStart | Neutral | Pool general |
| 7 | R-DMG-1 | Punta de Lápiz | +2 daño a tus ataques | OnDamageDealt | Neutral | Pool general |
| 8 | R-DMG-2 | Sorpresa Guardada | +5 al primer ataque del combate | OnDamageDealt | Neutral | Pool general |
| 9 | R-DMG-3 | Cinta de Embalar | -1 al daño recibido | OnDamageTaken | Neutral | Pool general |
| 10 | R-ACC-1 | Cuaderno Cuadriculado | Cada 3 Skills → +1 energía siguiente turno | OnCardPlayed + OnPlayerTurnStart | Neutral | Pool general |
| 11 | R-ACC-2 | Tres en Raya | Cada 3er ataque +4 daño | OnDamageDealt | Neutral | Pool general |
| 12 | R-END-1 | Mochila de Botín | +5 oro por victoria | OnCombatEnd | Neutral | Pool general |
| 13 | R-END-2 | Curita con Estampa | +4 HP por victoria | OnCombatEnd | Neutral | Pool general |
| 14 | R-END-3 | Frasco de Confianza | +10 oro por Elite | OnCombatEnd | Neutral | Pool general |
| 15 | R-SW-4 | Aliento Entre Mundos | +2 HP al cambiar de mundo | OnWorldSwitch | Neutral | Pool general |
| 16 | R-SW-1 | Escudo de Costuras | +5 bloque al cambiar | OnWorldSwitch | **Switch** | Pool general |
| 17 | R-SW-2 | Estilo Doble | +1 carga de Estilo al cambiar | OnWorldSwitch | **Switch** | Pool general |
| 18 | R-SW-3 | Onda Dimensional | +5 daño raw al cambiar | OnWorldSwitch | **Switch** | Pool general |
| 19 | R-ELITE-1 | Diente Afilado | Vampírico SuperEficaz | OnDamageDealt | Neutral | Drop Elite |
| 20 | R-ELITE-2 | Erizo de Cartón | Espinas | OnDamageTaken | Neutral | Drop Elite |
| 21 | R-ELITE-3 | Caja Intacta | Purista (HP lleno → +10 oro) | OnCombatEnd | Neutral | Drop Elite |
| 22 | R-ELITE-4 | Ritmo Encendido | ≥3 cargas → +4 daño | OnDamageDealt | Neutral | Drop Elite |
| 23 | R-BOSS-1 | Hilo de Costura Maldita | Boss Acto 1 (post-victoria → siguiente combate +1 energía +5 bloque) | OnCombatEnd + OnCombatStart | Neutral | Drop Boss |

### Flavor Text (placeholder funcional)

Texto narrativo que aparece en el tooltip junto a la descripción mecánica. Estética: handmade / cartón / crayón / dos hermanos que mezclan medieval oscuro y cyberpunk con objetos cotidianos del imaginario infantil.

| Código | DisplayName | Flavor Text |
|--------|-------------|-------------|
| R-OPEN-1 | Tapa de Caja de Galletas | "La levantás cada vez que va a empezar la pelea. Sirve, casi siempre." |
| R-OPEN-2 | Bolsillo Roto | "Siempre cae una carta de más. No sabés de cuál mundo viene." |
| R-OPEN-3 | Sorbo Robado | "La gaseosa de tu hermano. Menos mal que no se dio cuenta." |
| R-TURN-1 | Mano de Más | "Te dibujaste una tercera mano en el codo. Funciona." |
| R-TURN-2 | Almohadón Reforzado | "Tres almohadones contra la pared. Inexpugnable." |
| R-TURN-3 | Reloj de Cocina | "Tic. Tic. Tic. Cada tres tics, vas más rápido." |
| R-DMG-1 | Punta de Lápiz | "Afilada hace cinco minutos. Todavía duele si toca." |
| R-DMG-2 | Sorpresa Guardada | "El primer golpe siempre lo planeás. No es trampa, es preparación." |
| R-DMG-3 | Cinta de Embalar | "Tres vueltas alrededor del brazo. Improvisado, pero protege." |
| R-ACC-1 | Cuaderno Cuadriculado | "Cada tres planes anotados, te dan ganas de hacer uno más." |
| R-ACC-2 | Tres en Raya | "Uno, dos, tres — y caen. Siempre fue tu juego favorito." |
| R-END-1 | Mochila de Botín | "Vacía cuando empezás. Pesada cuando ganás." |
| R-END-2 | Curita con Estampa | "De los dinosaurios. Funciona después de cada pelea." |
| R-END-3 | Frasco de Confianza | "Lo abrís solo cuando ganás los grandes. Te da más cosas adentro." |
| R-SW-4 | Aliento Entre Mundos | "Cuando saltás de un lado a otro, un poco te recomponés." |
| R-SW-1 | Escudo de Costuras | "Mitad armadura medieval, mitad placa de circuito. Siempre te tapa." |
| R-SW-2 | Estilo Doble | "Cada salto entre mundos te queda guardado en la cabeza." |
| R-SW-3 | Onda Dimensional | "El cambio no es silencioso. Algo se rompe del otro lado." |
| R-ELITE-1 | Diente Afilado | "Cuando muerde donde duele, te dejás un poco para vos." |
| R-ELITE-2 | Erizo de Cartón | "Lo pegaste con espinas de plástico. Si te tocan, también las sienten." |
| R-ELITE-3 | Caja Intacta | "Si no la abriste, vale más. Llegás entero, te llevás el premio." |
| R-ELITE-4 | Ritmo Encendido | "Cuando el ritmo es tuyo, todo lo que tirás pega más fuerte." |
| R-BOSS-1 | Hilo de Costura Maldita | "Lo arrancaste de su pecho. Sigue moviéndose. Te ayuda en el próximo combate, no sabés bien por qué." |

**Nota de obtención:** "Pool general" = disponible en Tienda (compra, sub-PR 3D) y Eventos (variable, M4). En 3B solo se crean los assets y la lógica; el cableado de drop se completa cuando 3C/3D/M4 implementan sus respectivos nodos. Los drops de Elite y Boss se completan en 3B (hook de drop al ganar Elite/Boss, roadmap §3B).

**Backlog (no entran al pool inicial, candidatos para sub-PRs futuras):**
R-OPEN-4 (cargas de Estilo iniciales), R-TURN-4 (heal lento), R-TURN-5 (energía mano vacía), R-DMG-4 (combo cargas), R-DMG-5 (vampírico básico), R-DMG-6 (espinas básico), R-ACC-3 (bloque por cartas), R-ACC-4 (rompecadenas), R-END-4 (purista común), R-SW-5 (primer cambio +daño), R-ELITE-5 (reciclador).

---

## Decisiones cerradas (2026-05-08)

1. **8º punto de inserción en TurnManager (cartas neutras + `OnDamageDealt`)** — **APROBADO**. Sub-tarea de 3B. Agregar dispatch en el path `attackerType == ElementType.None` de `ApplyPlayerToEnemyEffectiveness`. Mismo archivo protegido que 3A — la aprobación dada aquí equivale a la requerida por el roadmap §3B.
2. **`IsElite` / `IsBoss` en payloads** — **CABLEAR en 3B** como sub-tarea. Opción A: agregar los campos a `CombatStartHookData` y `CombatEndHookData` cableando `NodeType` desde `RunCombatConfig`/`ConfigureCombat`. R-END-3 (slot 14) entra al pool.
3. **`StyleCharges` público en `TurnManager`** — ya expuesto en `TurnManager:109`. Sin cambios requeridos.
4. **Mecánica del Boss R-BOSS-1** — **opción C**: post-victoria → siguiente combate arranca con +1 energía y +5 bloque. Implementación: `Counters["wonLastCombat"]` set en `OnCombatEnd` Victory; en `OnCombatStart` consumir y aplicar. Nombre y flavor text: Sebastián define en 3B.
5. **Reset de `Counters` per-combate** — **en `OnCombatStart`**. Cada efecto que use counters per-combate los resetea en `OnCombatStart`. Esto garantiza que el primer turno del combate lea estado limpio y que los counters "vivos" al terminar un combate (ej: en derrota rápida) no contaminen el siguiente.
6. **R-OPEN-2 y hand-limit** — **respetar el cap**. `GrantDrawCards` con el cap activo — sin bypass. Si el player ya tiene mano llena al iniciar combate (caso improbable pero posible con otros Retazos), la carta extra se descarta silenciosamente o no se roba (según comportamiento actual de `DrawCards`).
7. **Nombre del enum `CardType`** — **`CardType.Skill` confirmado** en código. R-ACC-1 puede implementarse sin cambios de infra.
8. **Convención de naming para `Counters` keys** — **prefijo `"r-xxx-n:"` + clave descriptiva**. Ejemplo: `"r-acc-1:skillsThisTurn"`, `"r-dmg-2:firstHitDone"`. El número es el slot del pool. Esto evita colisiones entre Retazos que usen nombres genéricos como `"count"` o `"first"`.

---

## Próximos pasos

**Estado actual:** todo listo para `modo:implementacion`.

1. ✅ Pool de 23 cerrado (tabla del Resumen).
2. ✅ Decisiones técnicas cerradas (8/8).
3. ✅ Nombres y flavor text placeholder aprobados (tablas arriba). Refinables en playtest.
4. **[OPCIONAL — Sebastián]** Ajustar valores numéricos `[PROPUESTA]` si alguno no convence. Si no, se dejan así y se tunean post-implementación en playtesting.

**`modo:implementacion` (Sub-PR 3B) puede arrancar con este checklist:**
- 8º punto de inserción en TurnManager (path `attackerType == ElementType.None` de `ApplyPlayerToEnemyEffectiveness`) — aprobado.
- Cablear `IsElite`/`IsBoss` en `CombatStartHookData` y `CombatEndHookData` (extender `RunCombatConfig`/`ConfigureCombat` para inyectar `NodeType` desde `BattleFlowController`).
- 23 `RelicDefinition` SOs (con `DisplayName` y `FlavorText` de las tablas arriba) + 23 clases `IRelicEffect`.
- `RelicInventoryView` (fila de iconos en HUD de combate + RunMapView, estilo StS, ver roadmap §3B).
- Hook de drop al ganar Elite/Boss (drops de R-ELITE-1..4 y R-BOSS-1 garantizados; el resto entra al Pool general que consume Tienda en 3D).
- Tests EditMode por Retazo (cada Retazo aplica su efecto en el hook correcto, sin Retazos no rompe nada).

Convención de naming de `Counters` keys: `"r-xxx-n:clave"` (ej: `"r-acc-1:skillsThisTurn"`, `"r-dmg-2:firstHitDone"`, `"r-boss-1:wonLastCombat"`).
