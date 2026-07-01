# Spec — M5: Boss del Acto 1 (Costura Maldita / UNIT-RB7)

> **ESTADO: PROPUESTA — CERRADA (lista para `modo:implementacion`).** Todas las
> decisiones de diseño están cerradas (ver "Decisiones cerradas"). Handoff al final.
> (Al implementarse, cambiar a `IMPLEMENTADO — PR #N (YYYY-MM-DD)`.)
>
> Decisiones cerradas por Sebastián (sesión 2026-06-29): D1=behavior object + seams;
> D2=el Desfase dispara `OnWorldSwitch` (sin gastar presupuesto del jugador);
> D3=debuffs duran N turnos y expiran (N=3 default tuneable); D5=fase latcheada.

## Origen
- GDD `_gdd.md` líneas 53-77 (DD-004) + `DESIGN_DECISIONS.md` DD-004 (boss, fases,
  debuffs) y DD-019 (Sangrado/Virus exclusivos del boss).
- `GOLDEN_RULES.md` §6 (bosses, categorías dimensionales), §2 (turno/mundo), §3
  (efectividad), §4 (Estilo).
- `_roadmap.md` líneas 301-330 (M5 desagregado en 7 sub-tareas).

## Objetivo
Primer boss real del juego: un enemigo transdimensional de dos tipos con **fases
por HP**, una mecánica única (**Desfase Dimensional**: la IA fuerza el cambio de
mundo para romper combos) y dos **debuffs temáticos** (Sangrado / Virus). Establece
el patrón técnico de boss que reusarán M6+.

---

## ⚠️ Correcciones al enunciado de partida (seguir el GDD, no el prompt)

El prompt de arranque describió dos mecánicas de forma distinta a como las define el
GDD. **El spec sigue el GDD** (autoridad de diseño):

| Mecánica | Prompt decía | [GDD] dice (línea) | Spec sigue |
|----------|--------------|--------------------|------------|
| **Sangrado** | "daño al inicio del turno del afectado" | "Te quita vida **cada vez que lanzas un ataque**" (69) + roadmap "pérdida de HP **al jugar ataque**" (311) | Pérdida de HP del jugador **al jugar una carta `CardType.Attack`** |
| **Virus** | "reduce el daño del afectado" | "Tus **cartas de defensa bajan su efectividad a un 80%**" (73) | Reduce el **bloqueo** que generan las cartas del jugador a 80% |

Si alguna de estas dos lecturas no es lo que querés, es una decisión de diseño que
hay que reabrir **antes** de implementar (ver "Decisiones abiertas").

---

## Comportamiento esperado (perspectiva del jugador)

1. **Encuentro de boss.** El nodo de boss inicia un combate marcado `isBoss=true`
   (canal ya cableado en `ConfigureCombat`). El enemigo es **Costura Maldita** en
   Mundo A (medieval) y **UNIT-RB7** en Mundo B (cyberpunk): un solo
   `EnemyDefinition` transdimensional con dos tipos (uno por mundo). La ficha de dos
   tipos del HUD (reuso 4c) muestra ambos; el activo a color pleno, el inactivo
   atenuado.
2. **Fase 1 (HP > 50%).** El boss ataca, se defiende, y en algún momento ejecuta un
   movimiento que **carga el Desfase Dimensional** (intent telegrafiado 1 turno
   antes). Una vez cargado, durante los siguientes turnos: **cada 3 cartas que el
   jugador juega en un mismo turno, el mundo cambia automáticamente** — flip del
   tipo activo del jugador y del boss, y flip del lado activo de las cartas duales,
   reventando la cadena de combo. Un **contador visible** avisa cuándo se disparará.
3. **Fase 2 (HP ≤ 50%, obligatoria).** El umbral del Desfase baja: **cambia de mundo
   cada 2 cartas** en vez de 3. El resto del kit del boss se mantiene o se endurece.
4. **Debuffs temáticos.** En Mundo A el boss usa "Costura Viva" → aplica **Sangrado**
   (el jugador pierde HP cada vez que juega un ataque). En Mundo B usa "Código
   Reescrito" → aplica **Virus** (las cartas que dan bloqueo rinden al 80%).
5. **Victoria.** Al derrotarlo: 100 de oro (ya cableado) + el Retazo único de boss
   **R-BOSS-1 "Hilo de Costura Maldita"** (ya existe, ver Reuso).

---

## Sistemas afectados

- **Combate (TurnManager — PROTEGIDO):** seams nuevos para (a) forzar cambio de
  mundo desde la IA sin gastar el presupuesto del jugador; (b) contador de cartas
  jugadas en el turno; (c) interceptar el bloqueo del jugador (Virus) y el juego de
  ataques (Sangrado); (d) orquestar el estado del Desfase y las fases.
- **IA de enemigos:** el boss usa `EnemyAIPattern.PhaseBased` (ya implementado) para
  el pool de movimientos por HP%. La lógica de fase/Desfase/debuff vive en un
  **objeto de comportamiento de boss** nuevo (ver Arquitectura), no inline si se
  elige la opción recomendada.
- **Datos (ScriptableObjects):** un `EnemyDefinition` nuevo (boss) + sus `EnemyMove`;
  campos nuevos mínimos para parametrizar Desfase/debuffs (en SO, no hardcode).
- **UI (CombatHudView / view nueva):** contador del Desfase, indicador de
  Sangrado/Virus activos, e intent de "cargando Desfase". Refresco de mano al forzar
  el cambio de mundo.
- **RunState / Retazos:** confirmación del drop del Retazo de boss (infra ya existe).

---

## Arquitectura propuesta

[PROPUESTA] **Objeto de comportamiento de boss delegado** (`DimensionalBossBehavior`,
archivo nuevo NO protegido en `Assets/Scripts/Gameplay/Enemies/` o
`.../Combat/Bosses/`) que **posee todo el estado específico del boss**:

- `Phase` (derivada o latcheada de HP%, ver decisión D5)
- estado del Desfase: `{ Idle, Charging, Active }` + `turnosRestantesActivo`
- `cartasJugadasEsteTurno`
- estado de debuffs sobre el jugador: `sangradoActivo`, `virusActivo` (+ duración
  según D3)

`TurnManager` (PROTEGIDO) expone **seams mínimos** que delegan en este objeto:

- al iniciar el turno del jugador → resetear contador de cartas + tick de fase/Desfase
- al resolver una carta del jugador → notificar al behavior (incrementa contador;
  si llega al umbral y Desfase activo → fuerza cambio de mundo; si la carta es
  `Attack` y Sangrado activo → aplica daño de Sangrado al jugador)
- al construir un `BlockAction` de fuente jugador → si Virus activo, escalar el valor
- en el turno del enemigo → si el move del boss lo indica, activar Desfase / aplicar
  debuff

[ALTERNATIVA] *Inline en TurnManager*: todo el estado y la lógica dentro del
protegido. Descartada como recomendación porque infla el archivo más crítico y
agranda el diff a revisar; pero es una opción válida (decisión D1).

[ALTERNATIVA] *Sustrato genérico de StatusEffect por-actor* (lista de estados en
`ICombatActor` + tick genérico). Descartada como recomendación porque DD-019 acota
Sangrado/Virus al boss (no genéricos), y tocaría `ICombatActor` +
`PlayerCombatActor` (más archivos protegidos) para algo que hoy usa un solo enemigo.
Reconsiderable en M6+ si varios bosses comparten debuffs (decisión D1).

### Por qué NO se toca `PlayerCombatActor`
Sangrado aplica daño vía `_player.TakeDamage(...)` (API ya existente). Virus escala
el `value` del efecto `Block` **antes** de crear el `BlockAction`, en el punto de
construcción de la acción. Ninguno requiere estado nuevo dentro de
`PlayerCombatActor`. → Se respeta la restricción de no tocar ese protegido.

### `ActionQueue`: probablemente NO se toca
[CÓDIGO ACTUAL] `ActionQueue` ya soporta encolar cualquier `IGameAction` (incluida
una eventual `WorldChangeAction`) sin modificarse, y el guard `_isProcessing` evita
reentradas. El Desfase se dispara **entre cartas** (en la resolución de carta del
jugador), en un límite seguro, vía llamada directa al seam — no necesita un "hook
post-ProcessAll". [ABIERTO menor D6] confirmar en implementación que `ActionQueue`
queda intacto; si se confirma, **sale del set de archivos protegidos de M5**.

---

## Archivos a crear

- `Assets/Scripts/Gameplay/Enemies/DimensionalBossBehavior.cs` (o `Combat/Bosses/`)
  — estado + lógica del boss (fase, Desfase, debuffs). NO protegido.
- `Assets/Scripts/Gameplay/Combat/Actions/WorldChangeAction.cs` *(opcional, solo si
  se modela el Desfase como acción encolada en vez de llamada directa)* — `IGameAction`.
- `Assets/Editor/BossConfigSetup.cs` — `MenuItem` que autoría el `EnemyDefinition`
  del boss + sus moves (análogo a `EnemyConfigSetup` de 4c). Cumple "no manual editor
  setup": el boss se genera por menú, no a mano en el inspector.
- `Assets/ScriptableObjects/Enemies/BossAct1_CosturaMaldita.asset` — el SO del boss
  (generado por el menú anterior). *(Existe un `BossAct1.asset` vestigial de M2/M3;
  ver "Decisiones abiertas D7": reusarlo o reemplazarlo.)*
- Tests EditMode nuevos (ver "Casos de prueba").

## Archivos a modificar

- `Assets/Scripts/Gameplay/Enemies/EnemyDefinition.cs` — campos nuevos del boss
  (p.ej. flags/parámetros de Desfase y de debuffs por move; ver Contratos). NO protegido.
- `Assets/Scripts/Gameplay/Enemies/EnemyMove.cs` — metadata para que un move dispare
  el Desfase / aplique un debuff (campo nuevo, p.ej. `bossAction` o un `EffectType`
  nuevo). NO protegido.
- `Assets/Scripts/Gameplay/Cards/CardEnums.cs` — `StatusType` (+`Sangrado`, `Virus`)
  y/o `EffectType` (+`ShiftWorld` si se modela como efecto). NO protegido.
- `Assets/Scripts/Gameplay/Combat/CombatHudView.cs` — contador del Desfase, badges
  de Sangrado/Virus, refresco de mano ante cambio de mundo forzado. NO protegido.
- `Assets/Tests/EditMode/CombatTestBase.cs` — extender factories
  (`CreateEnemyDefinition`, `CreateEnemyMove`) con los campos del boss. NO protegido.

## Archivos protegidos involucrados

- [x] `TurnManager.cs` — **REQUIERE APROBACIÓN.** Necesita: (a) método de cambio de
  mundo forzado por IA (`ForceWorldChange(initiator)` o ampliar `TryChangeWorld` con
  `Initiator`+veto, audit H6) que NO consuma `_worldSwitchesUsed`; (b) contador de
  cartas jugadas por turno + reset al inicio de turno del jugador; (c) interceptar el
  `value` de `Block` de fuente jugador en `CreateAction` (Virus); (d) gancho al
  resolver carta del jugador para Sangrado; (e) `case` para el/los `EffectType`/move
  nuevos del boss; (f) seams de test. **Cambio de semántica/superficie, NO de los
  contratos 4c.**
- [ ] `ActionQueue.cs` — **probablemente NO** (ver arriba). Si se confirma necesario,
  REQUIERE APROBACIÓN.
- [ ] `PlayerCombatActor.cs` — **NO se toca** (por diseño).

> **NO TOCAR (contrato 4c):** `EnemyDefinition.typeWorldB` / `isAnchor` /
> `IsTransdimensional`, ni el getter `TurnManager.EnemyElementType`. M5 los **consume**
> sin modificarlos. El boss es un transdimensional estándar; su tipo activo lo
> resuelve `EnemyElementType` tal cual.

---

## Contratos

### Datos (SO)

[PROPUESTA] Campos nuevos, todos en SO (data-driven), con defaults tuneables:

**`EnemyDefinition` (boss):**
- `int desfaseChargeTurns = 1` — turnos de carga antes de activarse.
- `int desfaseActiveTurns = 3` — duración del estado activo.
- `int desfaseThresholdPhase1 = 3` — cartas para forzar cambio (fase 1).
- `int desfaseThresholdPhase2 = 2` — cartas para forzar cambio (fase 2).
- `int phase2HpPercent = 50` — umbral de fase 2.
- `int sangradoDamagePerAttack = 4` — HP que pierde el jugador por ataque jugado
  *(tuneable, ver D4)*.
- `int sangradoTurns = 3` — turnos que dura Sangrado antes de expirar (D3, tuneable).
- `float virusBlockMultiplier = 0.8f` — factor de bloqueo bajo Virus (GDD fija 0.8).
- `int virusTurns = 3` — turnos que dura Virus antes de expirar (D3, tuneable).
- `int maxHP` — *(tuneable, HP del boss; default propuesto ~140, ver D4)*.

> **Lifecycle (D3 cerrada — N turnos):** al aplicar Sangrado/Virus, el behavior
> object guarda `turnosRestantes = sangradoTurns/virusTurns`. Se decrementa **al
> inicio de cada turno del jugador** (en el tick que ya resetea el contador de
> cartas); al llegar a 0, el debuff se desactiva. Re-aplicar **refresca** la duración
> al máximo (no acumula stacks — D3 opción B, no C). N por defecto = 3 para ambos.

**`EnemyMove` (boss):** un campo que marque la acción especial del move, p.ej.
`BossMoveTag { None, ChargeDesfase, ApplySangrado, ApplyVirus }` o, alternativamente,
nuevos `EffectType` (`ShiftWorld`, etc.). [PROPUESTA] usar un enum de tag en el move
(más localizado al boss; evita inflar `EffectType` global con efectos que solo el
boss usa, en línea con DD-019). [ALTERNATIVA] `EffectType.ShiftWorld` + `ApplyStatus`
genéricos — más reutilizable pero expone los debuffs del boss al sistema general.

**`StatusType`:** agregar `Sangrado`, `Virus` (o usar `Custom` + tag). [PROPUESTA]
valores explícitos para legibilidad/tests.

### APIs (TurnManager — PROTEGIDO)

- `void ForceWorldChange(WorldChangeInitiator initiator)` *(o `TryChangeWorld(initiator)`)*
  — muta `currentWorld` A↔B **sin** validar/consumir `_worldSwitchesUsed`; dispara (o
  no) el hook `OnWorldSwitch` según decisión D2; reusa internamente la misma ruta de
  flip que ya existe. **No altera `EnemyElementType` ni `PlayerActiveType`** (esos
  derivan de `currentWorld` automáticamente — el flip los actualiza solo).
- `int CardsPlayedThisTurn { get; }` — contador, reset en `BeginPlayerTurn`.
- Seams de test análogos a los de 4c: `SetBossStateForTest(...)`,
  `SetCardsPlayedThisTurnForTest(int)`, etc.

### Eventos

- [PROPUESTA] `event Action OnWorldChanged` (o reusar el hook existente) que la
  **view** consume para hacer `SyncHandButtons(forceRebuild:true)` cuando el cambio
  lo inicia la IA — hoy el rebuild solo lo dispara el handler del botón del jugador
  ([CombatHudView.cs:349](Assets/Scripts/Gameplay/Combat/CombatHudView.cs#L349)).
  **Requisito duro:** un Desfase debe refrescar la mano igual que el cambio manual,
  o las cartas duales quedarían mostrando el lado equivocado.

---

## Reuso (no reinventar)

- **Patrón transdim 4c (consumir tal cual):** `EnemyElementType` getter resuelve el
  tipo activo por mundo; `CombatHudView.BuildEnemyTypeLabel` ficha de dos tipos;
  `ElementTypeColors.TypePrefix(type, brightness)` para resaltar/atenuar.
- **`EnemyAIPattern.PhaseBased`** (ya implementado): el pool de moves del boss se
  filtra por `MinHpPercent`/`MaxHpPercent`. Fase 1 = moves con rango [51,100], Fase 2
  = moves con rango [0,50]. **No hay que escribir el selector**, solo etiquetar moves.
- **`ConfigureCombat(..., isBoss:true)`** y `_isCurrentCombatBoss` — canal de "este
  combate es boss" ya cableado.
- **Retazo de boss ya existe:** `RelicBossLastStitchEffect` (R-BOSS-1 "Hilo de Costura
  Maldita", +1 energía / +5 bloque el próximo combate) y el drop de Retazo de boss
  (`BossRelicDrop` en `RunCombatConfig`). M5 **confirma el cableado**, no diseña
  contenido nuevo.
- **`IGameAction` / `DamageAction` / `BlockAction`** — patrón para cualquier acción
  nueva. `DamageAction` es el molde para el tick de Sangrado.
- **`CombatTestBase`** — factories + TearDown automático; extender, no recrear.
- **Detección de carta de ataque:** `card.Type == CardType.Attack` (Sangrado).
- **Detección de "carta de defensa":** [INTERPRETACIÓN] no existe `CardType.Defense`
  (el enum es `Attack/Skill/Power/Curse/Status`). Virus intercepta el **efecto
  `Block`** de cartas de fuente jugador. (Confirmar interpretación en D-Virus.)

---

## Casos de prueba (EditMode)

**Fases / PhaseBased (reuso, plantilla `EnemyTransdimTests`):**
1. Boss con moves etiquetados [51,100] y [0,50]: con HP > 50% solo elige moves de
   fase 1; con HP ≤ 50% solo de fase 2.
2. El umbral del Desfase es 3 con HP > 50% y 2 con HP ≤ 50%.

**Desfase Dimensional (plantilla `WorldSwitchLimitTests`):**
3. Con Desfase activo (fase 1), jugar 3 cartas en un turno fuerza un cambio de mundo.
4. El cambio forzado **NO** incrementa `WorldSwitchesUsed` del jugador.
5. El cambio forzado flipea `EnemyElementType` (boss transdim) y `PlayerActiveType`.
6. El contador `CardsPlayedThisTurn` se resetea al iniciar el turno del jugador.
7. (Fase 2) jugar 2 cartas fuerza el cambio.
8. El Desfase respeta carga (1 turno) y expira tras `desfaseActiveTurns`.
9. El cambio forzado **dispara** el hook `OnWorldSwitch` (D2): un Retazo de switch
   (p.ej. `RelicSwitchBlockEffect`) se activa con el Desfase.

**Debuffs (plantilla `HealActionTests`: test directo de Action + vía TurnManager):**
10. Con Sangrado activo, jugar una carta `Attack` resta `sangradoDamagePerAttack` al
    jugador; jugar una carta no-Attack no resta.
11. Sangrado es raw (no lo modifica la tabla de efectividad ni mueve cargas de Estilo)
    *(según D-Sangrado)*.
12. Con Virus activo, una carta del jugador que da `Block N` produce
    `round(N * 0.8)` de bloqueo; sin Virus produce `N`.
13. Virus no afecta el bloqueo de fuente enemiga (boss).
14. Lifecycle (D3): Sangrado/Virus expiran tras N turnos del jugador; re-aplicar
    refresca la duración (no acumula). Tras expirar, jugar un ataque ya no sangra y
    el bloqueo vuelve a 100%.
15. Los debuffs solo los aplica el boss (no expuestos a cartas/enemigos comunes).

**Regresión (guard de protegidos):** la suite `ActionQueueTests` y `TurnManagerTests`
debe seguir 100% verde. Suite total parte de **230** y debe subir, sin regresiones.

## Validación manual (BattleScene)

1. Lanzar el combate de boss (vía menú de setup o nodo de boss).
2. Verificar ficha de dos tipos: el activo a color pleno, el inactivo atenuado;
   conmuta al cambiar de mundo (reuso 4c, eyeball).
3. Cargar el Desfase (esperar el move del boss; ver el intent telegrafiado).
4. Jugar cartas y confirmar: el contador avanza y al llegar al umbral el mundo cambia
   solo, la mano se re-renderiza (cartas duales flipean), el combo se interrumpe.
5. Bajar el boss a ≤ 50% HP → confirmar que el umbral pasa a 2.
6. Recibir Sangrado y jugar un ataque → ver la pérdida de HP. Recibir Virus y jugar
   una carta de bloqueo → ver el bloqueo reducido (~80%).
7. Derrotar al boss → 100 de oro + Retazo R-BOSS-1 ofrecido.

---

## Decisiones cerradas (por GDD / DESIGN_DECISIONS, no se rediscuten)

- **DC1.** El boss es UN `EnemyDefinition` transdimensional (no un tipo nuevo). (DD-014/DD-004)
- **DC2.** Dos tipos, uno por mundo, elegidos para que 1 sea SuperEficaz contra el
  jugador y 1 sea su debilidad. La **fuente de verdad de efectividad es el CÓDIGO**
  (`ElementEffectiveness`), no la tabla §3 del doc (desfasada, issue 4a). (DD-018)
- **DC3.** Fase 2 **obligatoria al 50% HP**. (DD-004 / GOLDEN_RULES §6)
- **DC4.** Números fijos del Desfase: carga 1 turno, activo 3 turnos, umbral 3 (fase
  1) / 2 (fase 2). (GDD 58-77)
- **DC5.** Virus = bloqueo al **80%**. (GDD 73)
- **DC6.** Sangrado y Virus son **exclusivos del boss**, no debuffs genéricos. (DD-019)
  → la implementación NO debe exponerlos a cartas/enemigos comunes.
- **DC7.** Mecánicas "bloquear cambio de mundo" e "invertir debilidades" son ejemplos
  para **bosses futuros (M6+)**, FUERA de scope de M5. La única mecánica única del
  boss del Acto 1 es el **Desfase Dimensional**. (GDD 53-56)
- **DC8.** El Retazo de boss es el ya existente R-BOSS-1; M5 lo cablea, no lo rediseña.
- **DC9 (D1).** El estado y la lógica del boss viven en un **behavior object**
  (`DimensionalBossBehavior`, no protegido); `TurnManager` solo expone **seams
  mínimos**. No se construye sustrato genérico de status; no se toca `ICombatActor`
  ni `PlayerCombatActor`.
- **DC10 (D2).** El cambio de mundo forzado por IA **NO consume** el presupuesto de
  cambios del jugador y **SÍ dispara** el hook `OnWorldSwitch` (Retazos de switch
  reaccionan al Desfase). [INTERPRETACIÓN] cadencia: el contador se resetea al inicio
  del turno del jugador y el Desfase se dispara **cada N cartas dentro del turno**
  (3ª, 6ª… en fase 1 / 2ª, 4ª… en fase 2), pudiendo gatillar varias veces por turno.
- **DC11 (D3).** Sangrado y Virus **duran N turnos y expiran** (N=3 default tuneable,
  campos `sangradoTurns`/`virusTurns`). Re-aplicar **refresca** la duración; **no**
  acumulan stacks. Decremento al inicio del turno del jugador.
- **DC12 (D5).** La fase es **latcheada**: al cruzar 50% HP por primera vez queda en
  Fase 2 de forma irreversible (aunque el boss recupere HP). "Fase 2 obligatoria al
  50%" = sticky.
- **DC13 (D-Sangrado).** El daño de Sangrado es **raw**: auto-daño del jugador al
  atacar, no un golpe tipado del boss → NO pasa por la tabla de efectividad ni mueve
  cargas de Estilo. Se aplica vía `_player.TakeDamage(sangradoDamagePerAttack)`.
- **DC14 (D-Virus).** Virus reduce el **efecto `Block`** de cualquier carta de fuente
  jugador a `virusBlockMultiplier` (0.8) — no existe `CardType.Defense`, así que "cartas
  de defensa" = "cartas que generan bloqueo". Redondeo `Mathf.RoundToInt(value*0.8f)`.
  No afecta el bloqueo de fuente enemiga.
- **DC15 (D7).** Se crea un SO nuevo `BossAct1_CosturaMaldita.asset`. El vestigio
  `BossAct1.asset` (M2/M3) **no se borra** (no es dato que creó M5); se deja y Sebastián
  decide su limpieza por separado.

---

## Residuales (no bloquean; se resuelven/confirman en implementación)

- **[D4 — tuneable]** HP del boss (default ~140 sobre HP de jugador 60-70) y
  `sangradoDamagePerAttack` (default ~4). Son campos de SO; se ajustan en playtest sin
  recompilar lógica. Arrancar con los defaults.
- **[D6 — verificar]** `ActionQueue` **no** debería tocarse (la cola ya soporta
  encolar acciones sin modificarse). Confirmar durante la implementación; si resultara
  necesario tocarla, **parar y pedir OK** a Sebastián antes.
- **[DC12 — pendiente Sub-PR C]** La **fase latcheada/sticky** (al cruzar 50% HP queda
  irreversible en Fase 2 aunque el boss recupere HP) **NO está implementada tras Sub-PR
  A**: el selector `PhaseBased` de `TurnManager` es *stateless* (filtra por HP%
  instantáneo). En A no es observable (el kit del boss no tiene move de curación → el HP
  solo baja) y el latch pertenece al behavior object de B/C (toca `TurnManager`). Al
  construir ese objeto en B/C: agregar el latch de fase + un **test de regresión** (boss
  recupera HP > 50% → sigue en Fase 2).

> **[CONTRADICCIÓN — informativa, no bloquea]** GOLDEN_RULES §9 ata "bosses interactúan
> con el cambio de mundo" al **Acto 3**, pero DD-004/GDD describen al boss del **Acto 1**
> con Desfase Dimensional. Se sigue DD-004/GDD (más específico). Sugerencia: ajustar la
> redacción de §9 para no implicar que la interacción con el mundo recién aparece en Acto 3.
>
> **[CONTRADICCIÓN — informativa, fuera de scope]** HP del jugador: GDD dice 70 (L113),
> GOLDEN_RULES §9 dice 60 (L248). No es del boss; conviene reconciliar (issue aparte).

---

## Sub-PRs sugeridos (cada uno mergeable sin romper lo anterior)

- **Sub-PR A — Boss SO + fases (PhaseBased) + ficha dos tipos.** Autoría del
  `EnemyDefinition` del boss (dos tipos, moves de ataque/defensa etiquetados por HP%
  para fase 1 / fase 2) + `BossConfigSetup` (menú editor) + extensión de
  `CombatTestBase` + tests de selección de move por fase. **NO toca protegidos.**
  Resultado: boss transdim jugable con fases, usando solo sistemas existentes. Derisk.
- **Sub-PR B — Debuffs Sangrado & Virus.** Estado boss-scoped + intercepción de
  Sangrado (daño al jugar `Attack`) y Virus (bloqueo ×0.8) + moves "Costura Viva" /
  "Código Reescrito" + badges en HUD. **Toca `TurnManager` (PROTEGIDO).** Depende de A.
- **Sub-PR C — Desfase Dimensional.** Seam de cambio de mundo forzado por IA (H6),
  contador de cartas, estado carga/activo, umbral 3→2 por fase, move "Cargar Desfase"
  + telegrafía, contador en HUD, refresco de mano al forzar el switch. **Toca
  `TurnManager` (PROTEGIDO).** Depende de A (independiente de B; B y C pueden
  intercambiarse — quien entre primero crea el scaffold del behavior object).
  **Incluye el latch de fase (DC12/D5):** el selector `PhaseBased` es stateless, así que
  la fase sticky (Fase 2 irreversible tras cruzar 50% HP) se implementa en el behavior
  object aquí, con test de regresión (boss recupera HP > 50% → sigue en Fase 2).
- **Sub-PR D — Retazo de boss + pulido/feedback.** Confirmar/cablear el drop de
  R-BOSS-1, label de intent "cargando Desfase", juice de telegrafía. Contenido +
  feel. Depende de C.

## Estimación

- **Complejidad:** alta (toca el protegido más crítico; introduce 3 mecánicas nuevas).
- **Riesgo:** cambio de mundo forzado a mitad de turno (orden de resolución, refresco
  de mano, interacción con Estilo y Retazos de switch). Mitigado por sub-PR A sin
  cirugía y por la red de tests existente (230) como guard.
- **Sub-tareas:** 4 sub-PRs (A derisk → B/C cirugía → D contenido).

## Prompt de handoff para `modo:implementacion` (Sub-PR A)

> M5 son 4 sub-PRs. Este handoff arranca **Sub-PR A** (boss SO + fases + ficha dos
> tipos), que NO toca archivos protegidos — es el arranque de menor riesgo. **B**
> (debuffs) y **C** (Desfase) tocan `TurnManager` y llevan su propio handoff por
> sesión (pedir el OK de protegidos en el momento). **D** es contenido/pulido.
>
> **Dependencia previa:** M5 reusa el patrón transdim de 4c (**PR #130**). Confirmar
> que **#130 está mergeado a `main`** antes de ramificar; si sigue abierto, mergearlo
> primero (o, excepcionalmente, ramificar desde `feat/m4-4c-transdim-ancla`).

```markdown
modo:implementacion

Implementá el Sub-PR A de M5 (Boss del Acto 1): boss SO + fases (PhaseBased) + ficha
de dos tipos. El spec cerrado está en `Docs/dev/specs/m5_boss_acto1_spec.md` — leelo
completo antes de tocar código; todas las decisiones de diseño ya están cerradas
(DC1-DC15), no abras ninguna.

Setup de branch (requiere PR #130 / M4 4c mergeado a main):
  git fetch --all --prune
  git checkout main && git pull       # confirmar que #130 está en main
  git checkout -b feat/m5-boss-a-so-fases origin/main

Qué construir (el detalle y los contratos están en el spec):
- `Assets/Editor/BossConfigSetup.cs` — MenuItem (estilo EnemyConfigSetup de 4c) que
  autoría el EnemyDefinition del boss + sus EnemyMove. NADA de setup manual en el
  inspector.
- `Assets/ScriptableObjects/Enemies/BossAct1_CosturaMaldita.asset` — boss generado por
  ese menú: aiPattern=PhaseBased; dos tipos (elementType = SuperEficaz contra el
  jugador, typeWorldB = debilidad del jugador, elegidos según la matriz de
  ElementEffectiveness en CÓDIGO, no la tabla §3); isAnchor=false; moves de
  ataque/defensa etiquetados por HP% → fase 1 en [51,100], fase 2 en [0,50].
- `Assets/Tests/EditMode/CombatTestBase.cs` — extender CreateEnemyDefinition /
  CreateEnemyMove si hace falta para los nuevos campos del boss.
- `Assets/Tests/EditMode/BossAct1Tests.cs` — tests de selección de move por fase
  (plantilla EnemyTransdimTests): casos 1-2 del spec.
- (Si se agregan campos al SO ya en A) `EnemyDefinition.cs` / `EnemyMove.cs` —
  campos nuevos de parámetros del boss (NO protegidos). Si preferís, dejá los campos
  de Desfase/debuff para B/C y en A solo usá lo existente (PhaseBased + dos tipos).

Reglas no negociables:
- NO tocar archivos protegidos en Sub-PR A: TurnManager, ActionQueue, PlayerCombatActor.
  (B y C sí tocan TurnManager, con OK explícito en el momento.)
- NO modificar los contratos 4c: EnemyDefinition.typeWorldB/isAnchor/IsTransdimensional
  ni el getter TurnManager.EnemyElementType. Consumirlos tal cual.
- No manual editor setup: el boss se genera por el menú, no a mano.
- Fuera de scope de A: Desfase Dimensional, Sangrado/Virus, contador de cartas,
  cambio de mundo por IA (son B y C). A entrega un boss transdim jugable con fases.

Validación obligatoria antes de cerrar:
- Compilación limpia (zero console errors).
- Correr el MenuItem de BossConfigSetup y verificar que el .asset se crea bien.
- Tests EditMode nuevos (BossAct1Tests) en verde + suite completa (≥230) sin
  regresiones, validado con Unity-MCP (tests-run EditMode).
- Eyeball en BattleScene: el boss aparece con ficha de dos tipos (activo pleno /
  inactivo atenuado) y elige moves distintos sobre/bajo 50% HP.

Al cerrar: actualizá `_roadmap.md` (checkbox de la sub-tarea de fases + 2-tipos de
M5) y `_tech_snapshot.md` (nuevo SO de boss + BossConfigSetup), commit + push + PR a
main. Dejá anotado en el PR que B/C/D siguen.
```
