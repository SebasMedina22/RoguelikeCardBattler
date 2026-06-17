# Spec — M4 Bloque 4a: Integridad del sistema de cartas

> **ESTADO: IMPLEMENTADO** — branch `feat/m4-4a-card-integrity` (2026-06-17), PR a
> `main`. Suite EditMode 191/191; CERO cambios en archivos protegidos (la afinidad se
> monta sobre el mecanismo dual). `/cierre-sesion` puede fijar el número de PR exacto.

## Origen
- Roadmap `_roadmap.md` §M4 Bloque 4a (reordenado 2026-06-10, `modo:gdd`).
- `DESIGN_DECISIONS.md` **DD-022** (afinidad de cartas → opción A) y **DD-023**
  (mejora autorada por carta + test guard), ambas cerradas 2026-06-10.
- `GOLDEN_RULES.md` §5 (Mazo inicial ⏳, Mejora de cartas ⏳) y §3 (tipo activo,
  cartas neutras 90%).

## Objetivo
Cerrar la "integridad" del sistema de cartas del Acto 1 pegado al GDD §5: (1) que
**toda** carta del juego sea mejorable (datos de upgrade autorados en cada SO,
con un test que lo garantice) y (2) implementar la **afinidad de tipo**: las
cartas afines del mazo inicial adoptan en runtime el tipo del mundo activo, y el
mazo inicial se recompone a la fórmula exacta del GDD (5 Strike: 3 afines + 2
neutras; 4 Defend: 2 afines + 2 neutras; 1 dual drafteada = 10 cartas).

## Comportamiento esperado

### Sub-tarea 1 — Cobertura de mejoras (DD-013 re-scopeada / DD-023)
- Sin cambios visibles de gameplay nuevos: el mecanismo de mejora ya existe
  (Hoguera, Sub-PR 3C). El cambio es de **datos**: cada `CardDefinition` del
  proyecto define su mejora en su ficha.
- Concretamente: las **18 caras de `NewRunFaces/`** (hoy con `_upgrade` vacío)
  pasan a tener mejora autorada. Consecuencia: la **carta dual drafteada** (que
  se compone de 2 caras) se vuelve mejorable en la Hoguera (sus dos lados tienen
  upgrade → `CardDeckEntry.CanUpgrade()` devuelve `true`).
- Garantía permanente: un **test guard EditMode** recorre todos los
  `CardDefinition` SOs del proyecto y falla si alguno queda sin
  `Upgrade.HasUpgrade`. La Hoguera no vuelve a "verse rota" en silencio.

### Sub-tarea 2 — Afinidad de cartas (DD-022 opción A)
Desde la perspectiva del jugador:
- Las cartas **afines** (3 de las 5 Strike, 2 de las 4 Defend del mazo inicial)
  NO tienen tipo fijo: en el Mundo A muestran y usan el tipo elegido para A, en
  el Mundo B el tipo elegido para B. Al cambiar de mundo en combate, su tipo
  (y su tinte en HUD/visor) **conmuta en vivo**, igual que ya hacen las cartas
  duales.
- Las cartas **neutras** (2 Strike, 2 Defend) son tipo `None`: 90 % del daño base
  fijo, sin generar ni quitar cargas de Estilo (DD-002, ya implementado).
- El mazo inicial pasa de su composición placeholder actual
  (`[CÓDIGO ACTUAL]` 4 StrikeDual + 2 DefendDual + 1 BattleFocusDual = 7) a la
  del GDD: **5 Strike (3 afín + 2 neutra) + 4 Defend (2 afín + 2 neutra) + 1 dual
  drafteada = 10**. BattleFocus sale del starter (queda como SO disponible para
  recompensas/Tienda; no se borra).

## Sistemas afectados
- **Combate:** **ninguno** (confirmado abajo — ver "Confirmación: TurnManager NO
  cambia"). La afinidad se monta sobre el mecanismo dual existente.
- **Datos (capa de cartas):** `CardDefinition` gana un flag `affinity` y un método
  de clonado por-tipo. Nuevo helper puro de resolución del mazo.
- **Run:** `RunSession.ConfigureCombat` resuelve afinidad antes de poblar el mazo.
  `RunState` no cambia (sigue clonando lo que recibe).
- **ScriptableObjects:** 4 SOs de cuerpo del starter (Strike/Defend × afín/neutra)
  + recomposición del `starterDeck` de `RunCombatConfig_Act1`; upgrades poblados
  en las 18 caras.
- **UI:** ninguna línea nueva — el visor de mazo y `CardHandView` renderizan las
  cartas afines como duales automáticamente (consecuencia gratis, ver validación).
- **Editor:** menú de setup (autorado idempotente de SOs + starter deck + upgrades).

## Archivos a crear
- `Assets/Scripts/Gameplay/Cards/AffinityResolver.cs` — helper **static puro**
  (espejo de `StarterDraft`): convierte una entrada de mazo afín en una dual
  runtime tipada por mundo; deja pasar neutras/duales sin tocar. Testeable sin UI.
- `Assets/Editor/StarterDeckSetup.cs` — menú `Roguelike > Setup Starter Deck (4a)`:
  crea/asegura los 4 SOs de cuerpo, autora sus upgrades + las 18 caras, y reescribe
  `RunCombatConfig_Act1.starterDeck` a la composición GDD. Idempotente.
- `Assets/Tests/EditMode/AffinityTests.cs` — casos de afinidad (resolución,
  preservación de cuerpo/upgrade, integración con `CardDeckEntry` y con un
  `TurnManager` real).
- `Assets/Tests/EditMode/CardUpgradeCoverageTests.cs` — el **test guard** DD-023.

## Archivos a modificar
- `Assets/Scripts/Gameplay/Cards/CardDefinition.cs` — añade el flag de afinidad,
  su propagación en `SetDebugData`/`CreateUpgradedClone`, y `CreateAffinityVariant`.
- `Assets/Scripts/Run/RunSession.cs` — `ConfigureCombat` resuelve afinidad
  (detrás del guard `State.Deck.Count == 0`) y pasa el mazo resuelto a
  `InitializeDeck`.
- `Assets/Scripts/Run/RunCombatConfig.cs` — método editor-gated
  `EditorPopulateStarterDeck(List<CardDeckEntry>)` (patrón de
  `NewRunConfig.EditorPopulateFaces` / `ShopConfig.EditorPopulatePools`).
- `Assets/Editor/CardUpgradeSetup.cs` — extender para autorar las 18 caras (o
  delegar en `StarterDeckSetup`; ver "Decisiones cerradas").
- SOs de datos (vía el menú, no a mano):
  `Assets/ScriptableObjects/Cards/` (4 cuerpos nuevos) +
  `NewRunFaces/*` (18 upgrades) + `Run/RunCombatConfig_Act1.asset` (starterDeck).

## Archivos protegidos involucrados
- [x] **Ninguno.** `TurnManager.cs`, `ActionQueue.cs`, `PlayerCombatActor.cs` NO
  se tocan. Confirmación explícita abajo.

### Confirmación: TurnManager NO cambia (la pregunta abierta del roadmap)
El roadmap pedía "confirmar en el spec si TurnManager necesita cambios (intentar
que no)". **No necesita.** Verificado leyendo el código hoy:

1. Al jugar una carta, el tipo que entra a la tabla de efectividad es
   `activeCard.ElementType`, donde `activeCard = GetActiveCardDefinition(entry)`
   ([TurnManager.cs:826,842](../../../Assets/Scripts/Gameplay/Combat/TurnManager.cs#L826)).
2. `GetActiveCardDefinition(entry)` = `entry.GetActiveCard(CurrentWorld)`
   ([TurnManager.cs:803-806](../../../Assets/Scripts/Gameplay/Combat/TurnManager.cs#L803)).
3. Para una carta dual, `GetActiveCard(world)` devuelve el lado del mundo activo
   (`dualCard.GetSide(world)`), y cada lado es un `CardDefinition` con su propio
   `ElementType` ([CardDeckEntry.cs:67-75](../../../Assets/Scripts/Gameplay/Cards/CardDeckEntry.cs#L67)).

→ Las duales **ya conmutan tipo por mundo sin que TurnManager sepa nada**. La
afinidad se implementa representando cada carta afín como una **dual runtime**
cuyos lados son el mismo cuerpo con el tipo del Mundo A (lado A) y del Mundo B
(lado B). El daño neutro (tipo `None`, 90 %) ya está cubierto por
`ApplyPlayerToEnemyEffectiveness` ([TurnManager.cs:611](../../../Assets/Scripts/Gameplay/Combat/TurnManager.cs#L611)).
Cero líneas nuevas en archivos protegidos.

## Contratos

### Datos

**`CardDefinition` (modificado)** — campo nuevo + getter:
```csharp
[SerializeField] private bool affinity = false;
public bool Affinity => affinity;
```
- `affinity == true`  → carta afín: en runtime adopta el tipo del mundo activo.
  Su `elementType` autorado en el SO es irrelevante (se autora `None`).
- `affinity == false` → comportamiento actual: usa su `elementType` fijo
  (incluido `None` = neutra 90 %).

**`AffinityResolver` (nuevo, static puro)**:
```csharp
// Resuelve UNA entrada. Afín-single → dual runtime tipada por mundo.
// Neutra-single o ya-dual → Clone() sin cambios.
public static CardDeckEntry Resolve(CardDeckEntry authored, ElementType typeA, ElementType typeB);

// Resuelve la lista entera (mapea Resolve sobre cada entrada válida).
public static List<CardDeckEntry> ResolveDeck(IReadOnlyList<CardDeckEntry> deck, ElementType typeA, ElementType typeB);
```
Construcción de la dual afín (reusa lo existente):
```csharp
var dual = ScriptableObject.CreateInstance<DualCardDefinition>();
dual.InitRuntimeSides(
    $"affine_{single.Id}", single.CardName,
    single.CreateAffinityVariant(typeA),   // lado A
    single.CreateAffinityVariant(typeB));  // lado B
return CardDeckEntry.CreateDual(dual);
```

### APIs públicas

**`CardDefinition.CreateAffinityVariant(ElementType worldType)`** (nuevo):
- Devuelve una **instancia runtime** idéntica al cuerpo (effects, cost, name,
  description, type, rarity, target, tags, art) pero con `elementType = worldType`
  y `affinity = false` (ya resuelto).
- **Preserva el payload de upgrade** (`_upgrade`) — a diferencia de
  `CreateUpgradedClone`, que no lo copia — para que la dual resuelta siga siendo
  mejorable en la Hoguera. Como el método vive en `CardDefinition`, asigna
  `clone._upgrade = _upgrade;` directo (el `CardUpgradeDef` es read-only en
  runtime; compartir la referencia es seguro).
- Side effects: crea un `ScriptableObject` runtime (igual que
  `CreateUpgradedClone`/`ComposeDualCard`). NO toca el SO de origen.

**`SetDebugData(...)`** (modificado): añade parámetro opcional
`bool newAffinity = false` al final y lo asigna. Compatibilidad: los call sites
existentes (tests, `CardUpgradeSetup`, `CreateUpgradedClone`) siguen compilando.

**`CreateUpgradedClone()`** (modificado, 1 línea): pasa `affinity` a `SetDebugData`
para que un afín mejorado conserve el flag (caso defensivo; en el flujo normal el
afín ya es dual antes de mejorarse).

**`RunCombatConfig.EditorPopulateStarterDeck(List<CardDeckEntry>)`** (nuevo,
editor-gated `#if UNITY_EDITOR`): setter para que el menú reescriba el starter
deck sin edición manual del inspector.

### Eventos
- Ninguno nuevo. No se tocan eventos de TurnManager ni hooks de Retazos.

### Punto de inyección de la resolución
`RunSession.ConfigureCombat` ([RunSession.cs:152-161](../../../Assets/Scripts/Run/RunSession.cs#L152)):
```csharp
public void ConfigureCombat(RunCombatConfig config)
{
    if (config == null) return;
    CombatConfig = config;

    // 4a: resolver afinidad SOLO al poblar el mazo por primera vez. El guard
    // Deck.Count==0 evita re-crear (y filtrar) ScriptableObjects runtime en cada
    // combate — InitializeDeck ya no-opea tras el primero, así que sin guard
    // resolveríamos y descartaríamos SOs cada vez (leak de instancias).
    if (State.Deck.Count == 0)
    {
        var resolved = AffinityResolver.ResolveDeck(
            config.StarterDeck, State.PlayerWorldAType, State.PlayerWorldBType);
        State.InitializeDeck(resolved);
    }
}
```
- Orden garantizado: `NewRunScene` escribe `PlayerWorldAType/BType` en `RunState`
  (`StarterDraft.ApplySelectionToState`) **antes** de que cargue RunScene; la
  primera llamada a `ConfigureCombat` (desde `BattleFlowController.TryConfigureCombat`
  o `RunFlowController`) ya ve los tipos. Si se entra directo a BattleScene sin
  pasar por NewRunScene, los tipos caen a los defaults de `RunState` (Rojo/Amarillo)
  — no crashea.
- `RunState.InitializeDeck` **no cambia**: sigue recibiendo una lista y clonándola
  (+ inyectando `PendingStarterCard`). La dual drafteada **no** pasa por el
  resolver (sus lados ya tienen el tipo de cada mundo, porque el draft las filtra
  por los tipos elegidos).

### Composición del starter deck (autorada por el menú)
`RunCombatConfig_Act1.starterDeck` queda con **9 entradas single** (la 10ª es la
dual drafteada, inyectada por `InitializeDeck` vía `PendingStarterCard`):

| Cuerpo SO        | `affinity` | `elementType` | upgrade            | × |
|------------------|:---------:|:-------------:|--------------------|:-:|
| `Strike_Affine`  | true      | None          | +3 daño (6→9)      | 3 |
| `Strike_Neutral` | false     | None          | +3 daño (6→9)      | 2 |
| `Defend_Affine`  | true      | None          | +5 bloqueo (5→10)  | 2 |
| `Defend_Neutral` | false     | None          | +5 bloqueo (5→10)  | 2 |

- Cuerpo Strike = `Attack`, coste 1, `Damage 6`. Cuerpo Defend = `Skill`, coste 1,
  `Block 5` (espejo de los `StrikeBasic`/`DefendBasic` actuales).
- Los upgrades reusan los valores placeholder ya probados en `CardUpgradeSetup`
  (Strike 6→9, Defend 5→10).

## Reuso
- **Mecanismo dual completo**: `DualCardDefinition.InitRuntimeSides`,
  `GetSide`, `CardDeckEntry.CreateDual/GetActiveCard`, `CreateUpgradedClone` (dual
  mejora ambos lados). La afinidad NO inventa un camino nuevo; usa el de las duales.
- **Camino neutro 90 %**: `ApplyPlayerToEnemyEffectiveness` (tipo `None`), ya en
  producción y cubierto por `NeutralCardDamageTests`.
- **Patrón de helper puro testeable**: `AffinityResolver` espeja `StarterDraft` /
  `ShopNodeController.BuildStock`.
- **Patrón de menú editor idempotente + `EditorPopulate*`**: `NewRunConfigSetup`,
  `ShopConfigSetup`, `CardUpgradeSetup`.
- **Render automático**: `CardDisplay.EntryToken` / `DeckViewerView` /
  `CardHandView` ya saben mostrar duales y conmutar tinte/arte por mundo. Las
  cartas afines (= duales) se renderizan sin código nuevo: el visor muestra
  `[TipoA] Strike / [TipoB] Strike` (no colapsa porque los tipos difieren).

## Casos de prueba (EditMode)

### Afinidad — `AffinityTests.cs`
1. **Resolución afín→dual:** `AffinityResolver.Resolve` sobre una entrada single
   con `Affinity=true` devuelve una entrada **dual** cuyo `SideA.ElementType ==
   typeA` y `SideB.ElementType == typeB`.
2. **Preservación de cuerpo:** la variante afín conserva effects, cost, name,
   `Type`, target, art idénticos al cuerpo; solo difiere `ElementType` (y
   `Affinity=false`).
3. **Preservación de upgrade:** la dual resuelta tiene `CanUpgrade()==true`; tras
   `ApplyUpgrade()` ambos lados quedan mejorados (daño/bloqueo nuevo).
4. **Neutra pasa sin tocar:** entrada single con `Affinity=false` y
   `elementType=None` vuelve como single con `None` (no se convierte en dual).
5. **Integración con `CardDeckEntry`:** sobre la dual resuelta,
   `GetActiveCard(WorldSide.A).ElementType == typeA` y `(...B) == typeB`.
6. **Composición de mazo completo:** `ResolveDeck` sobre el starter autorado
   (3 afín + 2 neutra Strike, 2 afín + 2 neutra Defend) → 9 entradas: 5 con nombre
   "Strike" (3 dual + 2 single) y 4 "Defend" (2 dual + 2 single).
7. **Integración con `TurnManager` real (prueba que el protegido no cambia):**
   con un `TurnManager` configurado (estilo `RelicGrantEffectsOnTurnManagerTests`),
   jugar una Strike **afín** en Mundo A contra un enemigo de tipo SuperEficaz por
   el tipo A → daño = base × 1.5 y `StyleCharges` sube; jugar una Strike **neutra**
   → daño = 90 % y `StyleCharges` no cambia.
8. **Flag round-trip:** `CreateUpgradedClone` de un afín single preserva
   `Affinity`; `CreateAffinityVariant` produce `Affinity=false`.

### Cobertura de mejoras — `CardUpgradeCoverageTests.cs` (el guard DD-023)
9. **Guard global:** `AssetDatabase.FindAssets("t:CardDefinition")` → cargar cada
   SO → `Assert` que `card.Upgrade.HasUpgrade == true`. El fallo lista por nombre
   los SOs sin upgrade (mensaje accionable). *Nota de implementación:* es un test
   editor-only (usa `UnityEditor.AssetDatabase`); confirmar que
   `EditModeTests.asmdef` corre en plataforma Editor (lo hace) — envolver el
   `using UnityEditor;` en `#if UNITY_EDITOR` por higiene.
10. **Caras drafteables:** una `DualCardDefinition` compuesta de 2 caras (vía
    `StarterDraft.ComposeDualCard`) tiene `CanUpgrade()==true` (consecuencia de
    poblar las 18 caras).

## Validación manual (BattleScene + NewRunScene)
1. Correr `Roguelike > Setup Starter Deck (4a)` en Unity → 0 errores; los 4 SOs de
   cuerpo existen, `RunCombatConfig_Act1.starterDeck` tiene 9 entradas, las 18
   caras y los 4 cuerpos muestran upgrade en el inspector.
2. NewRunScene: elegir A=Rojo, B=Azul, draftear una dual. Entrar a combate.
3. Abrir el **visor de mazo**: 10 cartas. Las 3 Strike afines se ven
   `[Rojo] Strike / [Azul] Strike`; las 2 neutras se ven `Strike` sin tinte; la
   dual drafteada con sus dos caras.
4. En Mundo A, jugar una Strike afín contra enemigo: el daño refleja la
   efectividad Rojo→tipo-enemigo; el HUD de la mano la tinta de Rojo. **Cambiar de
   mundo** → la misma carta en mano pasa a tinte Azul y su daño usa Azul.
5. Jugar una Strike **neutra**: daño = 90 % fijo, sin popup WEAK/RESIST y sin
   mover el Contador de Estilo.
6. En la Hoguera, mejorar la dual drafteada (antes no se podía) y una Strike afín:
   ambas reflejan los números mejorados en el HUD, en ambos mundos.
7. **Resultado esperado:** sin errores de consola; afinidad conmuta en vivo;
   neutras al 90 %; toda carta mejorable.

## Decisiones cerradas
- **DD-022 (opción A):** afinidad implementada tal cual el GDD. Flag en
  `CardDefinition`. (Cerrada 2026-06-10.)
- **DD-023:** mejora autorada por carta (sin fallback genérico runtime) + test
  guard. (Cerrada 2026-06-10.)
- **Mecanismo de afinidad = dual runtime.** Decisión técnica de este spec:
  la carta afín se resuelve a una `DualCardDefinition` runtime por-mundo. Es el
  único camino que conmuta tipo por mundo sin tocar TurnManager, y reusa el
  mecanismo dual ya probado.
- **BattleFocus sale del mazo inicial.** El GDD §5 fija exactamente 5 Strike +
  4 Defend + 1 dual. BattleFocus no se borra (queda como SO para recompensas).
- **Flag de afinidad por `CardDefinition`, no por `CardDeckEntry`** (lo pide
  DD-022). Implica 2 SOs por carta de cuerpo (afín / neutra).
- **Resolución detrás del guard `Deck.Count==0`** en `ConfigureCombat` para no
  filtrar SOs runtime cada combate.
- **`RunState` y `InitializeDeck` no cambian de contrato** (siguen siendo capa de
  datos: reciben y clonan). La lógica de resolución vive en `RunSession` (capa de
  flujo) + helper puro.
- **Autorado vía menú editor**, no edición manual de inspector (consistente con
  el patrón `*Setup.cs` y con "no manual editor setup").

## Decisiones abiertas (REQUIEREN cierre antes de implementar)
- Ninguna. (Observación no bloqueante, NO decisión: la afinidad en cartas
  **Defend** es estructural pero hoy mecánicamente inerte — el bloqueo no pasa por
  la tabla de efectividad y las cartas no cambian `PlayerActiveType`. Importa para
  el tinte en UI y para futuros Retazos que lean `ActiveCard.ElementType`. Se
  implementa igual el 2+2 del GDD; queda anotado para no sorprender en playtest.)

## Alternativas consideradas
- **Resolver el tipo dentro de TurnManager** (flag afín + override de tipo en
  `GetActiveCardDefinition`): descartada — toca archivo protegido y exige inyectar
  los tipos del jugador en una ruta que hoy lee un SO inmutable. El camino dual no
  toca nada protegido.
- **Campo de tipo runtime mutado en cambio de mundo:** descartada — el cambio de
  mundo vive en TurnManager (protegido) y mutar un SO compartido tiene efectos
  laterales globales.
- **Flag de afinidad por `CardDeckEntry`** (permitiría 1 solo SO Strike usado como
  afín o neutra): más económico en SOs, pero contradice DD-022 ("flag en
  `CardDefinition`") y mezcla la afinidad con la semántica de 90 % que hoy depende
  de `elementType==None` en la carta resuelta. Descartada por DD-022.
- **Reutilizar `StrikeBasic`/`StrikeSideB`/`DefendBasic`/`DefendSideB` como los 4
  cuerpos:** posible (ya tienen upgrade), pero el naming ("Basic" = afín?) es
  confuso y esos SOs son los lados de los duales de recompensa (`StrikeDual`).
  Descartada a favor de 4 SOs nuevos con nombre explícito; los viejos quedan
  intactos para el `rewardPool`.

## Estimación
- **Complejidad: baja-media** (coincide con el roadmap). El código nuevo es chico:
  ~1 campo + 1 método en `CardDefinition`, 1 helper puro (~40 loc), 1 guard en
  `ConfigureCombat`. El grueso es **autorado de SOs** (4 cuerpos + 18 upgrades +
  starter deck) vía menú, y los tests.
- **Sub-tareas:**
  1. `CardDefinition`: flag + `CreateAffinityVariant` + propagación en
     `SetDebugData`/`CreateUpgradedClone`.
  2. `AffinityResolver` + guard en `RunSession.ConfigureCombat`.
  3. `RunCombatConfig.EditorPopulateStarterDeck` + menú `StarterDeckSetup`
     (cuerpos, upgrades de las 18 caras, recomposición del starter).
  4. Tests: `AffinityTests` (8) + `CardUpgradeCoverageTests` (2).
  5. Validación en Unity (menú + E2E NewRunScene→combate→Hoguera).
- **Riesgos:**
  - **Leak de `ScriptableObject` runtime** si la resolución no queda detrás del
    guard `Deck.Count==0` (mitigado en el contrato).
  - El guard global de upgrades fuerza a que **todo** `CardDefinition` del proyecto
    tenga upgrade — al correr el menú la primera vez puede destapar SOs viejos sin
    upgrade; el test lista cuáles (accionable).
  - Acceso a `UnityEditor.AssetDatabase` desde el test (editor-only; envolver en
    `#if UNITY_EDITOR`).

## Prompt de handoff para `modo:implementacion`

```text
modo:implementacion

Implementá M4 Bloque 4a (Integridad del sistema de cartas). El spec cerrado está
en `Docs/dev/specs/m4_4a_card_integrity_spec.md` — leelo completo antes de tocar
código; todas las decisiones de diseño ya están cerradas (DD-022 afinidad opción A,
DD-023 mejora autorada + guard), no abras ninguna.

Setup de branch (depende de PR #123 "visor de mazo": confirmá que está MERGED a
main antes de arrancar; si no, avisá):
  git fetch --all --prune
  git checkout -b feat/m4-4a-card-integrity origin/main

Qué construir (el detalle y los contratos están en el spec):
Crear:
- Assets/Scripts/Gameplay/Cards/AffinityResolver.cs (static puro: Resolve/ResolveDeck)
- Assets/Editor/StarterDeckSetup.cs (menú "Roguelike > Setup Starter Deck (4a)")
- Assets/Tests/EditMode/AffinityTests.cs (8 casos)
- Assets/Tests/EditMode/CardUpgradeCoverageTests.cs (guard DD-023, 2 casos)
Modificar:
- Assets/Scripts/Gameplay/Cards/CardDefinition.cs (flag affinity + getter,
  CreateAffinityVariant, propagar affinity en SetDebugData y CreateUpgradedClone)
- Assets/Scripts/Run/RunSession.cs (ConfigureCombat: resolver afinidad detrás del
  guard State.Deck.Count==0, pasar mazo resuelto a InitializeDeck)
- Assets/Scripts/Run/RunCombatConfig.cs (EditorPopulateStarterDeck, #if UNITY_EDITOR)
Autorar (vía el menú, NO a mano en inspector):
- 4 SOs de cuerpo (Strike_Affine/Strike_Neutral/Defend_Affine/Defend_Neutral) con
  upgrade (Strike 6→9, Defend 5→10)
- upgrades en las 18 caras de Assets/ScriptableObjects/Cards/NewRunFaces/
- RunCombatConfig_Act1.starterDeck = 3 Strike_Affine + 2 Strike_Neutral +
  2 Defend_Affine + 2 Defend_Neutral (9 entradas; la 10ª es la dual drafteada)

Reglas no negociables:
- NO tocar archivos protegidos (TurnManager, ActionQueue, PlayerCombatActor). El
  spec confirma que NO hacen falta cambios ahí — si creés que sí, PARÁ y avisá.
- No manual editor setup: SOs y starter deck se autoran por el menú editor.
- BattleFocus sale del mazo inicial pero NO se borra (queda para recompensas).
- Afinidad = dual runtime tipada por mundo (reusar InitRuntimeSides/GetSide). La
  variante afín preserva el payload de upgrade (a diferencia de CreateUpgradedClone).
- Resolución detrás del guard Deck.Count==0 para no filtrar ScriptableObjects.

Validación obligatoria antes de cerrar:
- Compilación limpia (zero console errors).
- Tests EditMode nuevos en verde (AffinityTests + CardUpgradeCoverageTests) +
  suite completa sin regresiones (parte de 181).
- Correr el menú "Setup Starter Deck (4a)" en Unity (crea SOs + starter deck +
  upgrades).
- Flujo E2E: NewRunScene (elegir 2 tipos + draft) → combate → visor muestra 10
  cartas (afines como [A]/[B], neutras sin tinte) → afín conmuta tipo/tinte al
  cambiar de mundo → neutra al 90 % sin mover Estilo → Hoguera mejora dual
  drafteada y afín.
- Validar en Unity-MCP (tests-run EditMode + screenshot del visor si aplica).

Al cerrar: actualizá `_roadmap.md` (checkboxes de los 2 bullets de 4a) y
`_tech_snapshot.md` (campo affinity en CardDefinition, AffinityResolver, guard de
upgrades, recomposición del starter), commit + push + PR a main.
```
