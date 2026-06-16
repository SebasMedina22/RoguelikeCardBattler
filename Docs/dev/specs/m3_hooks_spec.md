# Spec — Sistema de hooks + dispatcher de Retazos (M3 Sub-PR 3A)

> **Status:** cerrado en diseño (2026-05-07), refinado tras 3 rondas de revisión
> técnica. Ronda 1 cerró 8 hallazgos de feedback (1 crítico, 4 mayores, 3 menores)
> incluyendo semántica de `EnqueueExtraDamage`, lifecycle de `Counters`, guards
> en `Grant*`, herencia de payloads, lógica completa de `GrantStyleCharge`, fix
> de inserción de `OnCombatStart` post-`ClearBlock` y rename
> `RelicEffectHook` → `RelicHook`. Ronda 2 cerró 3 inconsistencias residuales
> (cleanup de `#if UNITY_EDITOR` en validación manual, encoding fix en tabla
> de inserción, instanciación explícita del dispatcher en `RunSession.Awake`).
> Ronda 3 (durante implementación, 2026-05-08) cerró 4 inconsistencias detectadas
> al codear: payloads de nodos diferidos a 3C/3D, y campos `TurnNumber`/
> `TurnsTaken`/`WasFreeSwitch` removidos por requerir cambios fuera de los 7
> puntos aprobados de `TurnManager`. Listo para `modo:implementacion` con
> aprobación explícita ya dada por Sebastián a los 7 puntos de inserción en
> `TurnManager`.
> **Fecha:** 2026-05-08
> **Modo:** `modo:diseno`

---

## Origen

- **Roadmap:** M3 Sub-PR 3A (`Docs/dev/_roadmap.md`).
- **Insight base:** Insight 3 — diseñar Retazos por categoría de hook (`Docs/dev/_insights.md`).
- **Decisiones cerradas hoy (2026-05-07):**
  - DD-017 — opción C (2-3 Retazos de cambio en contenido base).
  - Hoguera y Tienda extensibles vía hooks (`OnCampfireOptionsBuilt`, `OnShopStockBuilt`).
  - Inventario de Retazos = fila de iconos en HUD (estilo StS).
- **GOLDEN_RULES §10:** Retazos como pasivos persistentes, sin límite, 3 categorías
  (neutros, de cambio, de mundo).

---

## Objetivo

Instalar la infraestructura mínima para que **Retazos** (pasivos persistentes
del run) puedan reaccionar a eventos del combate, del mapa y de los nodos
(Hoguera, Tienda) **sin acoplarse al sistema que los dispara**. El `TurnManager`,
`RunFlowController`, `CampfireNodeController` y `ShopNodeController` solo emiten
eventos a un dispatcher; los Retazos individuales se cuelgan ahí.

Esta sub-PR **NO incluye contenido de Retazos** (eso es Sub-PR 3B). Solo el
contrato + la plomería + tests de plomería.

---

## Comportamiento esperado

Desde la perspectiva del jugador no hay nada visible en Sub-PR 3A: es
infraestructura. La validación es que sin Retazos en `RunState.Relics` el combate
y los nodos se comporten **exactamente igual que antes** (los hooks se invocan,
nadie escucha, nada cambia).

Desde la perspectiva del developer:

1. Crear un `RelicDefinition` SO declarando 1+ hooks que escucha.
2. Implementar el efecto en una clase `IRelicEffect` (uno por Retazo).
3. Al ganar un Retazo, se agrega un `RelicInstance` a `RunState.Relics`.
4. `RelicHookDispatcher` (singleton del run) suscribe automáticamente los
   efectos según los hooks declarados.
5. Cuando `TurnManager` dispara `OnPlayerTurnStart`, el dispatcher recorre los
   suscriptores en orden de adquisición y ejecuta cada efecto pasándole un
   payload tipado.

---

## Sistemas afectados

- **Combate:** `TurnManager` debe emitir 7 hooks de combate en puntos
  específicos del flujo. **REQUIERE APROBACIÓN** (archivo protegido).
- **Run:** `RunState` agrega `List<RelicInstance> Relics`.
- **RunSession:** instancia el `RelicHookDispatcher` en `Awake` con referencia
  al `RunState` actual. El dispatcher persiste con `RunSession` (cruza escenas).
  Al resetear el run via `RunState.Reset(map)`, el dispatcher persiste pero
  `Relics` queda vacío y `GetSubscribers` devuelve 0 hasta que se gane uno.
- **RunFlow:** `RunFlowController` no se toca en 3A (los hooks de Tienda/Hoguera
  los invocan los controllers de esos nodos en sub-PRs 3C/3D).
- **UI:** ningún cambio en 3A. El inventario visual es 3B.
- **ScriptableObjects:** nuevo tipo `RelicDefinition`.
- **Tests:** nuevo archivo `RelicHookDispatcherTests.cs`.

---

## Archivos a crear

```
Assets/Scripts/Gameplay/Relics/
  RelicDefinition.cs            ← SO base de un Retazo (datos + referencia al efecto)
  RelicCategory.cs              ← enum: Neutral, Switch, World (alineado con GOLDEN_RULES §10)
  RelicHook.cs            ← enum con los 9 hooks expuestos
  IRelicEffect.cs               ← interface ejecutable: void OnHook(RelicHookContext ctx)
  RelicInstance.cs              ← runtime wrapper: RelicDefinition + estado mutable (counters por turno, etc.)
  RelicHookDispatcher.cs        ← bus que TurnManager/Nodes invocan
  Hooks/
    RelicHookContext.cs         ← payload base (RunState, dispatcher, hook ejecutándose)
    CombatStartHookData.cs      ← payload de OnCombatStart
    PlayerTurnStartHookData.cs
    DamageDealtHookData.cs      ← payload mutable (ver §Contratos)
    DamageTakenHookData.cs      ← payload mutable
    WorldSwitchHookData.cs
    CombatEndHookData.cs
    CardPlayedHookData.cs

Assets/Tests/EditMode/
  RelicHookDispatcherTests.cs
```

**Payloads diferidos a sub-PRs futuras:**
`CampfireOptionsHookData.cs` y `ShopStockHookData.cs` **NO se crean en 3A**.
Se crean junto con el call site correspondiente (3C crea `CampfireOptionsHookData`
acompañando a `CampfireOption`; 3D crea `ShopStockHookData` acompañando a
`ShopItem`). Razón: los tipos `CampfireOption` y `ShopItem` son responsabilidad
de 3C/3D y crear sus payloads ahora forzaría decidir su shape antes de tiempo.
El enum `RelicHook` mantiene `OnCampfireOptionsBuilt` y `OnShopStockBuilt`
declarados en 3A; el dispatcher es genérico
(`Dispatch<TData> where TData : RelicHookContext`) y acepta cualquier payload
que herede de `RelicHookContext`, así que no necesita conocer los tipos
concretos hasta que 3C/3D los introduzcan junto con sus call sites.

## Archivos a modificar

- `Assets/Scripts/Run/RunState.cs` — agregar `List<RelicInstance> Relics` +
  reset en `Reset(map)`.
- `Assets/Scripts/Run/RunSession.cs` — exponer/owner del `RelicHookDispatcher`
  (vive en `RunSession` para que cruce escenas con la run).
- `Assets/Scripts/Gameplay/Combat/TurnManager.cs` — **PROTEGIDO** — invocar
  hooks en 7 puntos del flujo. Detalle en §Contratos.

## Archivos protegidos involucrados

- [x] `TurnManager.cs` — necesita: 7 invocaciones a `RelicHookDispatcher` en
      puntos exactos. **REQUIERE APROBACIÓN ANTES DE IMPLEMENTAR.**
- [ ] `ActionQueue.cs` — no se toca. Los hooks viven **fuera** de `ProcessAll()`
      (ver §Interacción con ActionQueue).
- [ ] `PlayerCombatActor.cs` — no se toca.

---

## Contratos

### Datos

#### `RelicCategory` (enum)

```csharp
public enum RelicCategory
{
    Neutral,   // efectos genéricos no atados al cambio de mundo
    Switch,    // se activan en/modifican cambios de mundo (DD-017)
    World      // ligados a un mundo específico (A o B)
}
```

#### `RelicHook` (enum)

Lista cerrada para Sub-PR 3A. Ampliar requiere nueva sub-PR + tests + actualización
de este spec.

```csharp
public enum RelicHook
{
    OnCombatStart,           // dentro de InitializeCombat, DESPUÉS del primer BeginPlayerTurn (para no ser borrado por ClearBlock)
    OnPlayerTurnStart,       // dentro de BeginPlayerTurn, después de ResetEnergy + ClearBlock, antes de DrawCards
    OnDamageDealt,           // jugador → enemigo, después de calcular efectividad, antes de encolar DamageAction (mutable)
    OnDamageTaken,           // enemigo → jugador, después de calcular efectividad, antes de encolar DamageAction (mutable)
    OnWorldSwitch,           // dentro de TryChangeWorld, después de mutar currentWorld, antes de incrementar _worldSwitchesUsed
    OnCombatEnd,             // dentro de CheckCombatEndConditions, una vez detectada Victory/Defeat
    OnCardPlayed,            // dentro de ResolvePreparedCardPlay, después de ProcessAll y DiscardCard, antes de CheckCombatEndConditions
    OnCampfireOptionsBuilt,  // antes de mostrar UI de Hoguera (Sub-PR 3C la invoca)
    OnShopStockBuilt         // antes de mostrar UI de Tienda (Sub-PR 3D la invoca)
}
```

Mapeo Insight 3 ↔ hooks:

| Categoría Insight 3 | Hook(s) |
|---------------------|---------|
| Apertura de combate | `OnCombatStart` |
| Inicio de turno | `OnPlayerTurnStart` |
| Modificador de daño | `OnDamageDealt`, `OnDamageTaken` |
| Acumulador / trigger | `OnCardPlayed` (counter en `RelicInstance`) + `OnPlayerTurnStart` (reset/check) |
| Economía / fin de combate | `OnCombatEnd` |
| Cambio de mundo | `OnWorldSwitch` |

#### `RelicDefinition` (SO)

```csharp
[CreateAssetMenu(menuName = "Roguelike/Relic")]
public class RelicDefinition : ScriptableObject
{
    public string DisplayName;
    [TextArea] public string Description;
    [TextArea] public string FlavorText;     // texto narrativo
    public Sprite Icon;
    public RelicCategory Category;
    public RelicHook[] Hooks;          // hooks que escucha (subset del enum)
    [SerializeReference] public IRelicEffect Effect;  // ver [CERRADO 1] abajo
}
```

**[CERRADO 1] Opción C — `[SerializeReference]` sobre `IRelicEffect`** dentro del SO.
Cada Retazo declara su efecto inline en el asset. Type-safe, sin reflexión, y
Unity 6.2 lo soporta de forma nativa. Cada implementación de `IRelicEffect`
debe llevar `[Serializable]`. Cuando un efecto necesite parámetros (ej:
"+N daño al primer ataque"), se serializan como campos de la clase del efecto.

```csharp
[CreateAssetMenu(menuName = "Roguelike/Relic")]
public class RelicDefinition : ScriptableObject
{
    public string DisplayName;
    [TextArea] public string Description;
    [TextArea] public string FlavorText;
    public Sprite Icon;
    public RelicCategory Category;
    public RelicHook[] Hooks;
    [SerializeReference] public IRelicEffect Effect;
}
```

#### `RelicInstance` (runtime, no SO)

```csharp
public class RelicInstance
{
    public RelicDefinition Definition { get; }
    public IRelicEffect Effect => Definition.Effect;  // viene del SO via [SerializeReference]
    public int AcquisitionOrder { get; }              // determina orden de ejecución
    public Dictionary<string, int> Counters { get; }  // estado mutable per-instance
                                                       // ej: "cardsPlayedThisTurn"
}
```

**Lifecycle de `Counters`:** son responsabilidad del propio Retazo. Cada
`IRelicEffect` decide cuándo resetearlos suscribiéndose a los hooks
correspondientes (ej: clear en `OnPlayerTurnStart` para counters por turno;
en `OnCombatEnd` para counters por combate). El dispatcher **NO** toca
counters — no resetea, no inicializa, no inspecciona. Esto evita que dos
Retazos colisionen por usar el mismo nombre de counter con asunciones
distintas: cada efecto es responsable del namespacing y del ciclo de vida de
sus propias claves.

Vive en `RunState.Relics`. No se serializa a disco en M3 (no hay save/load
de runs en curso).

#### `RunState.Relics`

```csharp
public List<RelicInstance> Relics { get; } = new List<RelicInstance>();
```

Reset incluido en `RunState.Reset(map)`.

#### `IRelicEffect`

```csharp
public interface IRelicEffect
{
    void OnHook(RelicHook hook, RelicHookContext ctx);
}
```

Cada Retazo recibe **el hook que se está disparando** (un mismo efecto puede
escuchar varios hooks declarados en `Definition.Hooks`). El método se llama
una sola vez por Retazo por evento.

#### Payloads (`*HookData`)

Distinguir **observables** vs **mutables**:

| Hook | Payload | Mutable? | Campos clave |
|------|---------|----------|--------------|
| `OnCombatStart` | `CombatStartHookData` | No | `EnemyDefinition Enemy`, `bool IsBoss`, `bool IsElite` |
| `OnPlayerTurnStart` | `PlayerTurnStartHookData` | No | `WorldSide CurrentWorld` |
| `OnDamageDealt` | `DamageDealtHookData` | **Sí** | `int Amount` (mutable), `Effectiveness Eff`, `ElementType AttackerType`, `ICombatActor Target` |
| `OnDamageTaken` | `DamageTakenHookData` | **Sí** | `int Amount` (mutable), `Effectiveness Eff`, `ElementType AttackerType`, `ICombatActor Source` |
| `OnWorldSwitch` | `WorldSwitchHookData` | No | `WorldSide From`, `WorldSide To` |
| `OnCombatEnd` | `CombatEndHookData` | No | `bool Victory`, `EnemyDefinition Enemy` |
| `OnCardPlayed` | `CardPlayedHookData` | No | `CardDefinition Card`, `WorldSide PlayedInWorld`, `int EnergySpent` |
| `OnCampfireOptionsBuilt` | (creado en 3C) | **Sí** | payload se crea junto con `CampfireOption` en Sub-PR 3C |
| `OnShopStockBuilt` | (creado en 3D) | **Sí** | payload se crea junto con `ShopItem` en Sub-PR 3D |

**Campos removidos en ronda 3 de revisión (2026-05-08):**
- `int TurnNumber` (estaba en `PlayerTurnStartHookData`): TurnManager no tiene
  contador de turno y agregarlo requiere cambios fuera de los 7 puntos
  aprobados. Retazos que necesiten "primer turno" o "cada N turnos" usan
  `RelicInstance.Counters` (incrementar en `OnPlayerTurnStart`, leer/resetear
  según necesidad — el lifecycle de Counters ya documenta esto).
- `int TurnsTaken` (estaba en `CombatEndHookData`): mismo argumento que
  `TurnNumber`. Retazos que necesiten "ganaste en menos de N turnos" usan
  Counters (incrementar en `OnPlayerTurnStart`, leer en `OnCombatEnd`).
- `bool WasFreeSwitch` (estaba en `WorldSwitchHookData`): semánticamente
  ambiguo dado que en M3 todos los switches son "gratis" (no cuestan recursos)
  y el pool de switches base+bonus es fungible en `TryChangeWorld`. Retazos
  como "primer cambio del combate" usan Counters. Si en sub-PRs futuras se
  requiere diferenciar base vs bonus, se reabre con cambio dedicado a
  `TryChangeWorld`.

**Todos los payloads heredan de `RelicHookContext`.** Cada `*HookData` extiende
`RelicHookContext` y agrega los campos específicos del evento. La signature de
`Dispatch<TData>(...) where TData : RelicHookContext` lo hace explícito a nivel
de tipos. No se usa composición (no hay `Ctx` adentro del payload).

```csharp
// Definición completa en [CERRADO 4] más abajo.
public class RelicHookContext
{
    public RunState RunState { get; }
    public TurnManager TurnManager { get; }
    public RelicHookDispatcher Dispatcher { get; }
    public RelicInstance CurrentRelic { get; }
    // + API limitada de mutaciones — ver [CERRADO 4]
}
```

Para `OnDamageDealt`/`OnDamageTaken`, los Retazos pueden mutar `Amount` antes
de que se encole el `DamageAction`. La cadena de mutaciones es secuencial en
orden de adquisición (`AcquisitionOrder` ascendente). El valor final es el que
se pasa a `new DamageAction(...)`.

### APIs públicas

#### `RelicHookDispatcher`

```csharp
public class RelicHookDispatcher
{
    public RelicHookDispatcher(RunState runState);

    // Llamado por TurnManager y por NodeControllers
    public void Dispatch<TData>(RelicHook hook, TData data) where TData : RelicHookContext;

    // Para tests / debug
    public IReadOnlyList<RelicInstance> GetSubscribers(RelicHook hook);
}
```

`Dispatch`:
1. Filtra `_runState.Relics` donde `Definition.Hooks.Contains(hook)`.
2. Ordena por `AcquisitionOrder` ascendente.
3. Por cada `RelicInstance` ejecuta `instance.Effect.OnHook(hook, data)`.
4. Si el payload es mutable, las mutaciones se acumulan entre Retazos.

#### Cambios en `TurnManager.cs` (PROTEGIDO — REQUIERE APROBACIÓN)

7 invocaciones, en este orden de aparición en el archivo:

| Punto | Hook | Justificación |
|-------|------|---------------|
| `InitializeCombat()`, **después de** `BeginPlayerTurn(useStartingHand: true)` (último statement del método) | `OnCombatStart` | Crítico: se inserta DESPUÉS de `BeginPlayerTurn` para evitar que `ClearBlock(_player)` (que corre dentro de `BeginPlayerTurn`) borre el bloque que un Retazo "+4 bloque al iniciar combate" haya otorgado. En el primer turno `ClearBlock` es no-op (block = 0), así que invocar el hook después es semánticamente correcto: el combate ya está inicializado, el primer turno comenzó, y el GrantBlock del Retazo aplica sobre estado limpio. |
| `BeginPlayerTurn()`, después de `ClearBlock(_player)` y **antes de** `_player.DrawCards(...)` | `OnPlayerTurnStart` | Permite Retazos que modifican draw size, energía extra, etc. |
| `ApplyPlayerToEnemyEffectiveness()`, después de calcular `finalAmount` y **antes de** `PlayerHitEffectiveness?.Invoke(...)` | `OnDamageDealt` | El daño post-efectividad es el que el jugador "siente", y los Retazos deben poder mutarlo antes de la cola. |
| `ApplyEnemyToPlayerEffectiveness()`, después de calcular `finalAmount` y antes de `EnemyHitEffectiveness?.Invoke(...)` | `OnDamageTaken` | Permite Retazos defensivos ("daño recibido -1"). |
| `TryChangeWorld()`, después de mutar `currentWorld` y **antes de** `_worldSwitchesUsed++` | `OnWorldSwitch` | El payload conoce el mundo destino y si fue gratis. |
| `ResolvePreparedCardPlay()`, después de `_actionQueue.ProcessAll()` y `_player.DiscardCard(...)`, antes de `CheckCombatEndConditions()` | `OnCardPlayed` | La carta ya se resolvió; el Retazo puede contar/encolar más acciones. |
| `CheckCombatEndConditions()`, **inmediatamente después de** setear `_phase = Victory` o `_phase = Defeat` | `OnCombatEnd` | Permite drops, +oro, heal post-combate. |

**[CERRADO 2] `OnCardPlayed` se dispara ANTES** de `CheckCombatEndConditions()`.
Razón: un Retazo "+5 oro por carta jugada" debe contar la última carta del
combate también. Si un Retazo encola daño extra que mata al enemigo, el
`CheckCombatEndConditions` posterior detecta la victoria normalmente.

**[CERRADO 3] `OnCombatEnd` muta `RunState` directamente.** Nada de encolar
acciones post-combate. El efecto del Retazo escribe directo:
`ctx.RunState.Gold += 5`, `ctx.RunState.PlayerCurrentHP = Mathf.Min(...)`. La
`ActionQueue` ya no se procesa una vez detectado Victory/Defeat, por lo que
encolar ahí sería frágil.

**Aclaración (fix de fin-de-combate, SUB-PR 1 / 2026-06-14, Opción B).** Los
métodos `Grant*` del contexto (`GrantHeal`, `GrantBlock`, `GrantStyleCharge`, …)
son **no-op en `OnCombatEnd`**. `CheckCombatEndConditions` setea
`_phase = Victory/Defeat` ANTES de llamar a `DispatchCombatEnd`, así que durante
el dispatch `IsCombatFinished == true` y cada `RelicGrant*` hace early-return
(el guard de §Guards comunes). Por eso heal/escudo post-combate van **sí o sí
por mutación directa de `RunState`**, no por la API de combate. Y es seguro
porque `DispatchCombatEnd` sincroniza `RunState.PlayerCurrentHP/MaxHP =
_player.*` ANTES del dispatch → el `RunState` es autoritativo en este hook. Esto
**invierte** la guía vieja D7/D8 (que trataba la mutación directa como "patrón
roto" y `GrantHeal` como "vía correcta"): el fix demostró lo contrario.

### Eventos

Sub-PR 3A no introduce nuevos `event Action<>` en `TurnManager`. El dispatcher
es el único bus para Retazos. Los eventos existentes
(`PlayerHitEffectiveness`, `EnemyTookDamage`, etc.) siguen sirviendo a la UI
sin cambios.

---

## Interacción con ActionQueue

**Regla:** los hooks **NO** se ejecutan dentro de `ActionQueue.ProcessAll()`.
Se invocan en `TurnManager` antes de `Enqueue` (para hooks de daño) o entre
`ProcessAll` y la siguiente fase (para `OnCardPlayed`, `OnCombatEnd`).

Razones:
1. `ActionQueue` es determinista FIFO y no debe tener side-effects de Retazos
   que reentren a `Enqueue` desde adentro de un `Execute`.
2. Mantiene `ActionQueue.cs` sin cambios (sigue protegido y simple).
3. Si un Retazo necesita encolar una acción (ej: "al recibir daño, +1 bloque"),
   lo hace mutando el `Amount` del payload o llamando un método de la API
   limitada (`ctx.GrantBlock(...)`, `ctx.EnqueueExtraDamage(...)`). La
   encolación entra en el siguiente `ProcessAll()` del flujo natural.

**`EnqueueExtraDamage` no pasa por efectividad ni re-dispara hooks.** El daño
encolado por este método es daño "raw": (a) no aplica multiplicador WEAK/RESIST,
(b) no otorga ni resta cargas de Estilo, y (c) **no re-dispara `OnDamageDealt`**.
Esto último es deliberado y previene loops infinitos cuando dos Retazos se
modifican mutuamente. Para daño extra que SÍ se beneficie de la efectividad,
mutar `data.Amount` en `OnDamageDealt` en vez de usar `EnqueueExtraDamage`.

**[CERRADO 4] API limitada en `RelicHookContext`.** Los Retazos NO reciben el
`TurnManager` entero. Reciben un contexto con métodos puntuales que internamente
delegan a `TurnManager`. Lista cerrada para Sub-PR 3A — ampliar requiere nueva
sub-PR + actualización de este spec.

```csharp
public class RelicHookContext
{
    public RunState RunState { get; }
    public TurnManager TurnManager { get; }       // null fuera de combate
    public RelicHookDispatcher Dispatcher { get; }
    public RelicInstance CurrentRelic { get; }

    // API limitada de mutaciones permitidas a Retazos:
    public void GrantBlock(ICombatActor actor, int amount);
    public void GrantHeal(ICombatActor actor, int amount);
    public void GrantDrawCards(ICombatActor actor, int amount);
    public void GrantEnergy(ICombatActor actor, int amount);
    public void GrantStyleCharge(int amount);
    public void GrantBonusWorldSwitch();
    public void EnqueueExtraDamage(ICombatActor target, int amount, ElementType type);
}
```

Cobertura por categoría del Insight 3:

| Categoría / ejemplo | Mecanismo |
|---|---|
| "+4 bloque al empezar combate" | `GrantBlock(player, 4)` en `OnCombatStart` |
| "Robás 1 carta extra" | `GrantDrawCards(player, 1)` en `OnPlayerTurnStart` |
| "+1 energía al inicio del turno" | `GrantEnergy(player, 1)` en `OnPlayerTurnStart` |
| "+5 al primer ataque del combate" | mutar `DamageDealtHookData.Amount` + counter |
| "Cada 3 cartas Skill → +1 energía siguiente turno" | counter en `OnCardPlayed` + `GrantEnergy` en `OnPlayerTurnStart` siguiente |
| "+5 oro al final de combate" | `ctx.RunState.Gold += 5` (mutación directa, [CERRADO 3]) |
| "Al cambiar de mundo, +5 bloque" | `GrantBlock(player, 5)` en `OnWorldSwitch` |
| "Heal 3 al matar enemigo" | mutación directa `ctx.RunState.PlayerCurrentHP = Mathf.Min(RunState.PlayerMaxHP, RunState.PlayerCurrentHP + 3)` en `OnCombatEnd` (Victory). **NO** `GrantHeal`: es no-op tras Victory/Defeat ([CERRADO 3]) |
| "+1 carga de Estilo al cambiar de mundo" | `GrantStyleCharge(1)` en `OnWorldSwitch` |
| "Tu primer cambio de mundo encola 5 daño extra" | `EnqueueExtraDamage(enemy, 5, type)` en `OnWorldSwitch` |

Implementación en `TurnManager`: cada método del contexto delega a un método
`internal` o `public` del `TurnManager` que muta el estado de forma controlada.

**Semántica precisa de cada método:**

- **`GrantStyleCharge(amount)`** delega en el método `IncrementStyleCharges`
  de `TurnManager` — la **misma fuente de verdad** que usa el path orgánico de
  `ApplyPlayerToEnemyEffectiveness` cuando un SuperEficaz genera +1 carga:
  incrementa `_styleCharges`; si llega a 5 con `_bonusWorldSwitches == 0`,
  otorga el bonus switch y resetea las cargas a 0. Esto preserva la golden
  rule §4 (Contador de Estilo). NO basta con clampear a 5 — sin el reset +
  bonus el jugador queda con cargas estancadas.
- **`GrantBonusWorldSwitch()`** setea `_bonusWorldSwitches = 1` solo si está
  en 0 (respeta el "no acumulable").
- **`GrantBlock` / `GrantHeal` / `GrantDrawCards` / `GrantEnergy`** delegan
  directo a `actor.GainBlock(n)` / `actor.Heal(n)` / `actor.DrawCards(n)` /
  `_player.GainEnergy(n)` (este último puede requerir nuevo método en
  `PlayerCombatActor` — confirmar en revisión de código; si ya existe,
  reusar).
- **`EnqueueExtraDamage(target, amount, type)`** encola un `DamageAction`
  directo en `_actionQueue`. **Importante:** este `DamageAction` **NO** pasa
  por `ApplyPlayerToEnemyEffectiveness` (es daño "raw"), por lo tanto:
  - No aplica multiplicador de efectividad (WEAK/RESIST).
  - No otorga ni resta cargas de Estilo.
  - **No re-dispara `OnDamageDealt`** (esto es deliberado: previene loops
    infinitos cuando dos Retazos se modifican mutuamente).
  - Si un Retazo quiere daño extra que **sí** se beneficie de la efectividad,
    debe mutar `data.Amount` en el hook `OnDamageDealt` en vez de usar
    `EnqueueExtraDamage`.

**Guards comunes (todos los métodos `Grant*` y `EnqueueExtraDamage`):**
early return si `_phase == Victory || _phase == Defeat`. La API es defensiva:
si un Retazo intenta otorgar algo después de que el combate terminó (caso
borde de un hook que se dispara tarde), no rompe — simplemente no hace nada.
`GrantHeal`/`GrantBlock`/etc también validan que `actor != null`.

---

## Orden de ejecución y casos borde

1. **Múltiples Retazos en mismo hook:** ejecución secuencial por
   `AcquisitionOrder` ascendente. El primer Retazo adquirido corre primero.
2. **Mutación de daño con varios Retazos:** se encadena. Si Retazo A es
   "+5 daño" y Retazo B es "x2 daño", el resultado depende del orden de
   adquisición. Esto es **intencional** (el jugador construye su build) y debe
   ser observable en tooltip ("este Retazo modifica el daño después de los
   anteriores").
3. **Sin Retazos:** `Dispatch` filtra y obtiene 0 suscriptores. Cero overhead
   funcional. Comportamiento idéntico al pre-3A.
4. **Reentrada:** un efecto que dispara otro hook (ej: `OnCardPlayed` que
   provoca un `OnDamageDealt`). El dispatcher debe permitirlo (no lockear).
   Opción más simple: no proteger contra reentrada — los hooks no son recursivos
   de naturaleza (cada uno se invoca desde un punto único de TurnManager).
5. **Retazo adquirido a mitad de combate (drop de Elite no aplica en M3
   porque drops son post-combate, pero por tienda en M3 podrían):** se agrega
   a `RunState.Relics` con el siguiente `AcquisitionOrder` libre. Se activa
   inmediatamente para hooks futuros.

---

## Reuso

- Patrón `IGameAction` + `ActionQueue` se mantiene inalterado. Los Retazos no
  son acciones de combate, son "modificadores" que viven una capa arriba.
- Patrón de eventos de `TurnManager` (eventos UI) se mantiene. El dispatcher
  es paralelo a los eventos UI, no los reemplaza.
- ScriptableObject pattern (igual que `CardDefinition`, `EnemyDefinition`).
- `RunSession` como dueño del dispatcher (mismo patrón que el ownership de
  `RunState`).

---

## Casos de prueba (EditMode)

Archivo: `RelicHookDispatcherTests.cs`. Mínimo:

1. **Sin Retazos, dispatch no rompe:** `Dispatch(OnPlayerTurnStart, data)` con
   `RunState.Relics` vacío → no excepción, no efectos.
2. **Suscripción única:** Retazo con hook `OnCombatStart` → al disparar, su
   `OnHook` se invoca exactamente 1 vez con el hook correcto.
3. **Múltiples Retazos en orden de adquisición:** 3 Retazos con `OnPlayerTurnStart`,
   adquiridos en orden A → B → C. Al disparar, llamadas registradas en ese orden.
4. **Hook no relevante no dispara:** Retazo con `OnDamageDealt` no se llama al
   disparar `OnPlayerTurnStart`.
5. **Mutación de payload encadenada:** 2 Retazos en `OnDamageDealt`, ambos
   suman +1 al `Amount`. Damage base 10 — final 12.
6. **Retazo con múltiples hooks declarados:** un Retazo declara
   `[OnCombatStart, OnCombatEnd]` y se llama en ambos eventos.
7. **Counter persistente entre invocaciones:** una instance de `RelicInstance`
   incrementa un counter (ej: `Counters["x"]++`) en una llamada a `OnHook`,
   y en una segunda llamada al mismo instance el valor sigue siendo el
   incrementado. Verifica que el dispatcher NO toca/resetea counters entre
   invocaciones — el ciclo de vida queda en manos del Retazo.
8. **`GetSubscribers` filtra correctamente:** con 5 Retazos mixtos, devuelve
   solo los que escuchan el hook pedido.

Tests de integración con `TurnManager` se cubren en Sub-PR 3B (donde hay Retazos
reales).

---

## Validación manual (BattleScene)

1. Abrir `BattleScene` con `RunState.Relics` vacío (default).
2. Jugar un combate completo (atacar, recibir daño, cambiar mundo, ganar).
3. **Resultado esperado:** combate idéntico a pre-3A. Zero console errors.
   Logs de dispatch silenciosos por default (`LogDispatches = false`, ver
   [CERRADO 5]).

**[CERRADO 5] Logging implementado, default OFF.** El `RelicHookDispatcher`
incluye un `bool LogDispatches = false` configurable. Cuando se prende, cada
dispatch loggea:
```
[Relics] OnPlayerTurnStart → 2 subscribers (StrikeBoost, DrawExtra)
[Relics] OnDamageDealt — Amount mutado 10 → 12 por StrikeBoost
[Relics] GrantBlock(player, 4) por OpeningRelic
```
Cuando está apagado el dispatcher no llama a `Debug.Log` en absoluto (zero
overhead, zero ruido). Los logs son parte de la implementación de 3A — no
detrás de `#if UNITY_EDITOR` — para que también funcionen en builds de
desarrollo si Sebastián quiere debuggear más tarde.

---

## Decisiones cerradas

**Bases (cerradas al iniciar diseño):**

- 9 hooks definidos en `RelicHook` enum (lista cerrada para 3A).
- Dispatcher vive en `RunSession` (cruza escenas con la run).
- Hooks se invocan **fuera** de `ActionQueue.ProcessAll`.
- Orden de ejecución = `AcquisitionOrder` ascendente.
- Sin Retazos = cero cambio de comportamiento.
- Sin contenido de Retazos en 3A (eso es 3B).
- `RelicCategory` enum alineado con GOLDEN_RULES §10 (Neutral, Switch, World).
- `OnCampfireOptionsBuilt` y `OnShopStockBuilt` son hooks fuera de combate, los
  invocan los controllers de nodos en sub-PRs 3C/3D (en 3A solo se declara la
  signature, no el call site).

**Cerradas en sesión de diseño (2026-05-07):**

- **[CERRADO 1]** `[SerializeReference]` sobre `IRelicEffect` directo en el SO
  (Opción C). Type-safe, sin reflexión.
- **[CERRADO 2]** `OnCardPlayed` se dispara **antes** de `CheckCombatEndConditions()`.
- **[CERRADO 3]** `OnCombatEnd` muta `RunState` directamente (no encola
  acciones).
- **[CERRADO 4]** API limitada en `RelicHookContext` con 7 métodos:
  `GrantBlock`, `GrantHeal`, `GrantDrawCards`, `GrantEnergy`, `GrantStyleCharge`,
  `GrantBonusWorldSwitch`, `EnqueueExtraDamage`. Lista cerrada para 3A;
  ampliarla requiere nueva sub-PR.
- **[CERRADO 5]** Logging implementado en el dispatcher con flag
  `LogDispatches` (default `false`). Logs son parte de la implementación, no
  detrás de `#if UNITY_EDITOR`.

## Decisiones abiertas

Ninguna. Spec listo para `modo:implementacion` tras aprobación explícita de
Sebastián a los 7 puntos de inserción en `TurnManager.cs` (ver §Cambios en
`TurnManager.cs`).

---

## Decisiones tomadas durante implementación (2026-05-08)

Durante la implementación de Sub-PR 3A, el agente tomó 5 decisiones que no
estaban explícitas en el spec original. Quedan registradas acá para
trazabilidad y porque varias tienen impacto en sub-PRs futuras.

**[IMPL 1] `OnDamageDealt` en el path de cartas neutras (`ElementType.None`) —
CERRADO en Sub-PR 3B.** En diseño 3A el branch de cartas None retornaba temprano
sin disparar el hook (el 8º punto de dispatch quedaba fuera de los 7 aprobados).
**3B cerró esto con aprobación explícita** (ver `RELICS.md §Decisiones cerradas`,
ítem 1 — opción A del Insight 5): el branch `attackerType == ElementType.None`
de `ApplyPlayerToEnemyEffectiveness` ahora dispara `OnDamageDealt` con el mismo
contrato mutable que el path tipado (los Retazos mutan `data.Amount` sobre el
daño neutro al 90% de DD-002). Consecuencia: los Retazos de modificador de daño
**sí** afectan cartas neutras hoy. El `TurnManager` quedó con **8** puntos de
dispatch, no 7.

**[IMPL 2] Try/catch alrededor de `effect.OnHook(hook, data)` en el
dispatcher.** No estaba en el spec; el agente lo agregó como defensa: un
Retazo defectuoso (asset SO mal serializado, NullRef en su efecto, etc.) no
debe abortar la cadena de Retazos restantes ni el flujo de combate. Excepción
queda como `Debug.LogError`. **Trade-off:** enmascara errores — si un Retazo
deja de funcionar, solo se entera por consola. Cubrir con un test "un Retazo
que tira excepción no rompe a los demás" en 3B.

**[IMPL 3] El parámetro `type` en `EnqueueExtraDamage(target, amount, type)`
no se usa en el cálculo de 3A.** Se acepta y se ignora. Razón: aplicarlo
implicaría duplicar la efectividad orgánica (el daño raw quedaría con
multiplicador, lo cual contradice [CERRADO 3] de la doc de
`EnqueueExtraDamage`). El parámetro se conserva en la signature como metadata
para futuros usos (animaciones diferenciadas por tipo en UI de 3B, posibles
Retazos en M5/M6 que sí lo lean). Comentario explícito en el método
`RelicEnqueueExtraDamage` de `TurnManager`.

**[IMPL 4] `RelicGrantEnergy` es no-op silencioso para non-player actors.**
Razón: la energía es concepto del jugador (`PlayerCombatActor.GainEnergy`);
`ICombatActor` no expone `GainEnergy` y el enemigo no tiene pool de energía.
Si un Retazo intenta `GrantEnergy(enemy, 1)`, el método valida y retorna sin
loggear. Documentado en el método `RelicGrantEnergy` de `TurnManager`.

**[IMPL 5] Logger del dispatcher no spammea cuando hay 0 subscribers.**
Aún con `LogDispatches = true`, si el filtrado del hook devuelve 0
subscribers, el dispatcher hace early-return sin loggear nada. Razón:
evitar inundar consola en combates sin Retazos (el caso default en M3 hasta
que 3B aterrice contenido). Si en debug querés ver "el dispatch se invocó
aunque no hubo subscribers", sumar un flag `LogEmptyDispatches` aparte —
no es prioridad ahora.

Ninguna de estas decisiones rompe el spec ni reabre las 5 [CERRADO]s. Todas
son refinamientos de implementación dentro del alcance acordado.

## Consecuencias de la semántica del Contador de Estilo (D-A, 2026-06-12)

Sebastián ratificó el Contador de Estilo como **PRE-block** (D-A, ver
`Docs/dev/audits/2026-06/PLAN_PRE_M4.md §0`): un golpe SuperEficaz otorga +1
carga aunque el enemigo lo bloquee al 100%. Esto = comportamiento actual del
código y tiene dos consecuencias para los Retazos:

1. **La carga se otorga ANTES del hook `OnDamageDealt`.** En
   `ApplyPlayerToEnemyEffectiveness` el orden es: calcular `finalAmount`
   post-efectividad → `IncrementStyleCharges(1)` si fue SuperEficaz con
   `finalAmount > 0` → recién entonces `Dispatch(OnDamageDealt)`. La carga
   depende del daño post-efectividad **pre-block** (el bloqueo del enemigo se
   aplica más tarde, en `DamageAction.Execute`). Por lo tanto, un Retazo que en
   `OnDamageDealt` baje `data.Amount` a 0 **no** revierte la carga ya otorgada
   — la semántica pre-block es independiente del valor final encolado.

2. **Retazos que leen/otorgan cargas operan sobre el conteo pre-block.**
   `GrantStyleCharge` (R-SW-2 "Estilo Doble") y los Retazos que condicionan por
   `StyleCharges` (R-DMG-4 "Combo de cargas") ven el contador tal cual lo dejó
   el pre-block, sin descuento por bloqueos enemigos. Es intencional y es el
   contrato que el test `StyleCharge_SuperEffectiveHitFullyBlocked_StillGrantsCharge`
   (SUB-PR 2) congela.

## Alternativas consideradas

- **[ALTERNATIVA] Eventos `Action<>` en TurnManager por cada hook (como
  `PlayerHitEffectiveness`):** descartada porque obligaría a cada Retazo a
  suscribirse/desuscribirse manualmente y la lógica de filtrado por hook
  declarado en SO se duplicaría. El dispatcher centraliza esto.

- **[ALTERNATIVA] Sistema de aspectos / componentes ECS:** descartada por
  overkill. El proyecto no usa ECS y meterlo solo para Retazos no se justifica.

- **[ALTERNATIVA] Hooks como métodos virtuales en `RelicDefinition` (sin enum,
  sin dispatcher):** descartada porque cada Retazo tendría que filtrar "soy
  para este evento o no" en runtime y el dispatcher no podría optimizar el
  filtrado.

- **[ALTERNATIVA] Invocar hooks desde dentro de `ActionQueue.ProcessAll`:**
  descartada — riesgo de reentrada y rompe la garantía de "FIFO determinista
  sin side effects externos" de `ActionQueue`.

---

## Estimación

- **Complejidad:** alta (toca archivo protegido, infraestructura nueva, payloads
  mutables, decide la forma del API que las sub-PRs 3B/3C/3D consumen).
- **Riesgo:** alto si las decisiones abiertas no se cierran antes de codear —
  rehacer el contrato después de 3B implica refactor de cada Retazo.
- **Sub-tareas (post-cierre de abiertos):**
  1. Crear estructura de archivos (`Relics/` + `Hooks/`).
  2. Definir enums y SO.
  3. Implementar `RelicHookDispatcher` + `RelicHookContext`.
  4. Modificar `RunState` + `RunSession`.
  5. **[REQUIERE APROBACIÓN]** Modificar `TurnManager` con 7 invocaciones.
  6. Tests EditMode (8 casos arriba).
  7. Validación manual sin Retazos (zero diff de comportamiento).
  8. Actualizar `_tech_snapshot.md` (nuevo subsistema `Gameplay/Relics/`).
  9. Marcar checkbox de Sub-PR 3A en `_roadmap.md`.

- **Bloqueo principal:** aprobación de Sebastián para modificar `TurnManager.cs`
  (los 7 puntos descritos en §Cambios en TurnManager).

---

## Próximo paso

Aprobar los 7 puntos de inserción en `TurnManager.cs` (archivo protegido).
Con eso, el spec pasa a `modo:implementacion` como input directo.


