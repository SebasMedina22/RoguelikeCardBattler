# Auditoría integral 2026-06 — Fase 2: Tests

> **Método:** 6 lectores paralelos (3 inventarios de la suite + 3 lectores de
> superficie de riesgo: run flow, Contador de Estilo, cruce con hallazgos F1)
> → consolidación a 13 agujeros → 13 verificadores adversariales livianos
> (grep dirigido + lectura puntual, default refutar).
> **Resultado:** 12 agujeros confirmados, 1 parcialmente cubierto.
> **Consumo:** ~433k tokens de subagentes (20 agentes, 308 tool uses; el
> fan-out inicial se recuperó cacheado del run interrumpido el 2026-06-11).

---

## Qué cubre DE VERDAD la suite hoy (17 archivos, 146 tests)

La suite es **100% sintética** — cero `.asset` reales, cero `AssetDatabase` —
y cubre bien la **lógica pura en aislamiento**:

- Matriz de efectividad 6×6 completa (39 casos) + multiplicadores 1.5x/0.75x
  aplicados en combate real (vía `CombatTestBase` + TurnManager).
- Block-antes-de-HP, draw con reshuffle, clamps de heal.
- Happy path del Contador de Estilo: +1 por SuperEficaz, decremento por hit
  enemigo, no-negativo, bonus a las 5 cargas, bonus no acumulable.
- Límites de world switch A→B (no el ciclo de vuelta B→A).
- Determinismo por seed: mapa, stock de tienda, draft de NewRun.
- Lógica de apply de Hoguera (heal %, upgrades, idempotencia) y Tienda
  (oro, guards, clonado, precio escalado del remove).
- Ordering y filtrado del `RelicHookDispatcher` (con stubs `RecordingEffect`).
- Helpers de color/arte (contratos de datos).

**Lo que NO cubre** (de ahí salen los agujeros):
1. **Integración entre capas** — ningún test junta TurnManager +
   BattleFlowController + RunState; ahí viven los bugs confirmados en F1
   (H1/H2 pasan verdes por esto).
2. **La mitad del catálogo de relics**: 10 efectos Grant-based testeados solo
   con `Assert.DoesNotThrow` y `TurnManager=null` — cero asserts de mutación.
3. **Boundaries y timing del Estilo**: carga pre-block/pre-hook, overshoot >5
   vía relics, resets entre combates.
4. **Contrato datos↔código**: los 23 `.asset` de Retazos jamás se cargan.
5. **Progresión del run**: transiciones de nodo, derrota→retry, round-trip de
   tipos elegidos en NewRun hasta el combate.

En síntesis: excelente red para regresiones de fórmulas y determinismo; casi
nula para flujo, integración y datos reales.

### Inventario por archivo (tests / qué asserta)

| Archivo | Tests | Cubre de verdad | Blind spot principal |
|---|---|---|---|
| ElementEffectivenessTests | 39 | matriz 6×6 (solo enum, no multiplicadores) | sin contexto de DamageAction |
| RelicEffectsTests | 17 | mutación de Amount, counters, gold, heal con cap | 10 Grant-based solo DoesNotThrow; nunca con BattleFlowController |
| ShopTests | 12 | oro, guards, clonado, remove escalado, filtro por tipo | hooks de compra; pools edge (None,None) |
| DamageEffectivenessTests | 10 | 1.5x/0.75x ambas direcciones, happy path Estilo | Estilo con block enemigo; carta None 90%; mutación por hook |
| CampfireTests | 8 | heal %, upgrades, idempotencia, hook de opciones | persistencia post-combate del heal |
| NewRunTests | 8 | filtro/determinismo de draft, inyección al deck, tipos | round-trip hasta el combate; idempotencia de InitializeDeck |
| RelicHookDispatcherTests | 8 | filtrado, AcquisitionOrder, payloads mutables | solo stubs, TurnManager=null en todos |
| ElementTypeColorsTests | 8 | opacidad, distinguibilidad, luminancia, prefijos | — (menor) |
| RunMapGeneratorTests | 5 | determinismo topología/enemigos, invariantes básicos | aciclicidad solo por rango; sin boss; un solo DepthPool |
| CardArtTests | 5 | contrato de datos del arte (defaults, clones, dual) | — (render se valida en Play, correcto) |
| TurnManagerTests | 4 | init, end turn, Victory/Defeat por overkill | orden Victory-vs-Defeat; fallback de tipos None |
| WorldSwitchLimitTests | 4 | límite A→B, unlimited debug, bonus suma | ciclo B→A; gate sin init; counter en modo debug |
| HealActionTests | 4 | heal con cap, no-ops, intent value | RelicGrantHeal |
| PlayerCombatActorTests | 3 | block antes de HP, reshuffle | maxHandSize, GainEnergy cap |
| ActionQueueTests | 2 | FIFO, filtrado de nulls | guard de re-entrada, eventos |
| DefinitionTypeDefaultsTests | 2 | defaults de ElementType | — (menor) |
| CombatTestBase | — | base compartida (GameObject + TurnManager en EditMode) | — |

---

## Agujeros confirmados por el verify (12)

> Todos pasaron verificación adversarial (default refutar): se buscó
> activamente un test existente que cubriera el claim y se validó la
> factibilidad EditMode del test propuesto.

### TOP — los que más riesgo eliminan por esfuerzo

**T1. Flujo OnCombatEnd → ReportOutcome jamás testeado en conjunto** *(M)*
Ningún test instancia TurnManager + BattleFlowController juntos. Es
exactamente el agujero por el que H1 (heal de R-END-2 pisado por
`BattleFlowController.cs:115-119`) y H2 (Purist lee HP stale) pasan verdes.
`RelicEffectsTests.cs:150-157` testea el efecto aislado con `TurnManager=null`.
*Test:* `ReportOutcome_AfterCombatEndHook_PreservesRelicHeal` — hoy FALLA
(documenta el bug ejecutable). Obstáculo único: mockear/extraer el
`SceneTransitionManager.LoadScene` de `ReportOutcome:131`.

**T2. Derrota → Reintentar arranca con HP=0: cero tests con "Retry"** *(S)*
Confirmado eslabón por eslabón: el handler (`RunFlowController.cs:650-658`)
solo toca flags (contraste: el botón Exit en :664 SÍ llama `Reset()`);
`EnsurePlayerHpInitialized` (`RunState.cs:148-150`) hace early-return con HP
ya inicializado → el 0 persiste. Es H3 de F1 hecho test.
*Test:* `RunState_AfterDefeatWithZeroHp_RetryPathRestoresPlayableHp` — puro
RunState, sin MonoBehaviour. Hoy FALLA.

**T3. Estilo otorga carga por "daño fantasma"** *(S)*
`TurnManager.cs:634-637`: la carga se decide con `finalAmount` PRE-block y
PRE-hook. Un hit SuperEficaz 100% absorbido por block enemigo igual carga.
Ningún test de Estilo usa enemigo con block (verificado: el más cercano,
`SuperEffectiveHit_GrantsStyleCharge`, usa block=0 implícito).
*Test:* `StyleCharge_SuperEffectiveHitFullyBlocked_DoesNotGrantCharge` +
gemelo con relic OnDamageDealt que mutea a 0. Sirve también para forzar la
DECISIÓN de diseño de H5 (¿pre o post block? GOLDEN_RULES §4 es ambiguo).

**T4. Reset entre combates** *(S — parcialmente cubierto, núcleo real)*
El verify refutó la mitad: el reset de Estilo en `InitializeCombat:282-284`
existe y los counters SÍ se testean *dentro* de un combate. Lo NO cubierto:
ningún test llama `InitializeCombat()` dos veces, y
`RelicTurnEnergyEveryNEffect` NO tiene reset en OnCombatStart (sus siblings
sí) mientras el dispatcher persiste entre combates (`RunSession`).
*Test:* `InitializeCombat_SecondCall_ResetsStyleStateAndRelicCounters` —
congela el contrato y documenta el counter que sangra (menor de F1).

**T5. Cero tests cargan los 23 `.asset` reales de Retazos** *(S)*
Grep: 0 hits de `AssetDatabase|LoadAsset` en toda la suite. Un hook mal
tipeado en un `.asset` pasa los 146 tests. Whitelist real de hooks
despachados: 7 en TurnManager + `OnCampfireOptionsBuilt` + `OnShopStockBuilt`.
*Test:* `RelicAssets_AllDeclaredHooksAreDispatchable` — FindAssets
`t:RelicDefinition`, valida Effect≠null, Hooks⊆whitelist. Extensible a
Cards/Enemies. Es el candidato directo de F1 (validación SO↔efecto).

**T6. 10 efectos Grant-based solo con DoesNotThrow** *(M)*
`RelicEffectsTests.cs:260-288` (`GrantBasedEffects_TurnManagerNull_DoNotThrow`)
es el ÚNICO test de OpenBlock/OpenDraw/OpenEnergy/TurnDraw/TurnBlock/
SwitchHeal/SwitchBlock/SwitchStyleCharge/SwitchDamage/EliteSpines — con
TurnManager=null, o sea bypass total de la lógica. TurnManager expone
PlayerBlock/PlayerEnergy/PlayerHP/PlayerHandCount para los asserts.
*Test:* `RelicGrantEffects_OnRealTurnManager_MutateState` parametrizado
(TestCaseSource) — ~media tabla del catálogo gana verificación de conducta.

**T7. Sin clamp superior de StyleCharges vía relics** *(S)*
`RelicGrantStyleCharge(3)` estando en 4 deja `_styleCharges=7` sin reset ni
bonus (la condición de `IncrementStyleCharges:989-997` exige el paso exacto
por >=5 con bonus en 0... y el path de relics nunca se testeó con TurnManager
real). Viola GOLDEN_RULES §4 (Estilo nunca >5). Boundary extra sin test:
decremento enemigo desde charges==1.
*Test:* `GrantStyleCharge_Overshoot_ResetsAndGrantsSingleBonus` (vía
reflection o wrapper de test — patrón ya usado en RunMapGeneratorTests).

**T8. `PendingStarterCard` sin idempotencia ni consumo** *(S)*
`InitializeDeck` tiene guard anti-duplicado (`Deck.Count>0` early-return)
pero NADA lo testea, y `PendingStarterCard` nunca se anula tras inyectarse
(el comentario de `RunState.cs:49` dice "consumed" — el código no lo hace).
Retry no la resetea, Reset sí → divergencia H4.
*Test:* `InitializeDeck_CalledTwice_DoesNotDuplicateStarterCard` — puro C#.

**T9. Regla "carta None = 90% daño" sin un solo test** *(S)*
`TurnManager.cs:611-622` activo en producción; DamageEffectivenessTests solo
usa cartas tipadas (las None del fixture tienen cost:99, injugables).
*Test:* `NeutralCard_Applies90PercentDamage` — trivial, asegura una regla de
balance viva + el redondeo de `Mathf.RoundToInt`.

**T10. Round-trip de tipos NewRun → combate jamás cerrado** *(M)*
La escritura está testeada (`ApplySelection_WritesChosenTypes`); la lectura
no: 0 hits de `ConfigureCombat(` en la suite — los tests usan
`SetPlayerTypesForTest` que lo BYPASSEA. El fallback silencioso a defaults
(`TurnManager.cs:257-258`) enmascararía exactamente el bug que esto cazaría.
*Test:* `InitializeCombat_WithRunStateTypes_PlayerActiveTypeMatchesSelection`
+ caracterización del fallback con tipos None.

### Backlog (confirmados, menor prioridad)

**T11.** RunMapGenerator: aciclicidad solo por rango de índices (un back-edge
en rango pasaría), `AssignBossAct1` (`RunSession.cs:102-117`) sin test alguno,
DepthPools múltiples nunca ejercitados (el fixture usa un pool 0-99). *(M)*

**T12.** `TryChangeWorld`: ciclo B→A sin test (el toggle solo se ejercita en
una dirección), gate `_initialized==false` sin test, y el assert del modo
debug (`GreaterOrEqual(WorldSwitchesUsed, 0)`) no valida nada — siempre pasa.
*(S — natural hacerlo junto con la cirugía H6 pre-M5)*

**T13.** Transiciones de nodo post-victoria (`CompleteNode`,
`RunFlowController.cs:453-482`: AvailableNodes/CompletedNodes/posición) y el
ciclo `LastNodeOutcome` — toda la progresión por el mapa sin red. Requiere
**extraer la lógica a método estático puro primero** (refactor S, encaja con
el desguace del god-object M12 de F1). *(L con el refactor; S después)*

## Refutados / degradados por el verify

- **T4 (mitad):** el reset de Estilo/Bonus en `InitializeCombat` SÍ existe y
  los counters SÍ tienen test intra-combate (`RelicEffectsTests.cs:215-227`);
  lo que queda sin red es el cross-combate (queda en T4, degradado a
  caracterización + documentación del counter de R-6).

---

## Propuesta de cierre: los 8 tests que más riesgo eliminan

Orden = riesgo eliminado / esfuerzo. Los tres primeros **documentan bugs
reales de F1 como tests que hoy fallan** (ejecutables, no narrativa):

| # | Test | Agujero | Esfuerzo | Nota |
|---|---|---|---|---|
| 1 | `ReportOutcome_AfterCombatEndHook_PreservesRelicHeal` | T1 (H1/H2) | M | falla hoy = bug H1 confirmado ejecutable |
| 2 | `RunState_AfterDefeatWithZeroHp_RetryPathRestoresPlayableHp` | T2 (H3) | S | falla hoy; puro RunState |
| 3 | `StyleCharge_SuperEffectiveHitFullyBlocked_DoesNotGrantCharge` | T3 (H5) | S | fuerza la decisión de diseño pre/post-block |
| 4 | `RelicAssets_AllDeclaredHooksAreDispatchable` | T5 | S | única validación datos↔código; barata |
| 5 | `RelicGrantEffects_OnRealTurnManager_MutateState` (×10 param.) | T6 | M | media tabla de relics gana asserts reales |
| 6 | `GrantStyleCharge_Overshoot_ResetsAndGrantsSingleBonus` | T7 | S | invariante GOLDEN_RULES §4 |
| 7 | `InitializeCombat_SecondCall_ResetsStyleStateAndRelicCounters` | T4 | S | congela contrato pre-cirugía M5 |
| 8 | `NeutralCard_Applies90PercentDamage` | T9 | S | regla de balance viva, test trivial |

Suplentes si hay apetito: T8 (`InitializeDeck` ×2, S), T10 (round-trip de
tipos, M). T11-T13 al backlog con dueño (T12 → cirugía pre-M5; T13 → junto
al desguace de RunFlowController antes de 4b).

**Nota de timing:** los 8 son EditMode puros (verificado uno a uno por el
verify) y conviene escribirlos ANTES de la cirugía de TurnManager pre-M5 —
los que pasan congelan el comportamiento que la cirugía no debe romper; los
que fallan son la spec ejecutable de los fixes de F1.

---
*Fase 2 cerrada 2026-06-12. Verify adversarial: 13 verificadores, 12
confirmados, 1 degradado, 0 falsos. Consumo F2: ~433k tokens de subagentes
(20 agentes; fan-out inicial recuperado de caché del run interrumpido).*
