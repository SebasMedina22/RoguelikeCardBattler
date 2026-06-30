# Spec — M4 Bloque 4c — Enemigos Transdimensionales + Ancla

> **ESTADO: IMPLEMENTADO — branch `feat/m4-4c-transdim-ancla` (2026-06-23).**
> Suite EditMode 230/230, validado Unity-MCP. Cierra M4. Pendiente confirmación
> de Sebastián en GOLDEN_RULES §2 + §6.

## Origen
- **GDD** `DD-014` (enemigos transdimensionales + ancla), `DD-004` (boss Acto 1 —
  2 tipos, uno por mundo — la mecánica que M5 necesita), `DD-011` (escalado por
  acto: transdim en Acto 2, ancla en Acto 3).
- **GOLDEN_RULES** §6 (categorías dimensionales: Estándar / Ancla; "la ficha
  muestra ambos tipos cuando aplica") y §2 (efecto del cambio de mundo, hoy
  marcado `⏳ pendiente` para transdim y ancla).
- **Roadmap** M4 bloque 4c (último bloque de M4; único que toca un archivo
  protegido).

## Objetivo
Implementar la maquinaria de **tipo activo por mundo** en enemigos: los
transdimensionales conmutan su tipo con el cambio de mundo, los ancla lo ignoran,
y la ficha del enemigo muestra ambos tipos cuando el enemigo es transdimensional.
Es el patrón de "tipos múltiples por mundo" que el boss del Acto 1 (M5) reutiliza.

## Comportamiento esperado
Desde la perspectiva del jugador:

- **Enemigo de tipo único** (los 5 actuales, Acto 1): sin cambios. Un tipo en
  ambos mundos; la ficha muestra un tipo.
- **Enemigo transdimensional:** la ficha muestra **dos tipos** (`[Rojo] / [Azul]`).
  El tipo del mundo activo se ve a color pleno; el otro, atenuado. Al cambiar de
  mundo, el tipo activo conmuta **instantáneamente**: la efectividad del daño
  (jugador→enemigo y enemigo→jugador) se recalcula contra el nuevo tipo, y el
  resaltado de la ficha se invierte. El jugador ve ambos tipos **antes** de gastar
  el cambio, para anticipar el efecto.
- **Enemigo ancla:** un solo tipo, **fijo e invariable**. Cambiar de mundo no lo
  altera. La ficha muestra un solo tipo.

## Sistemas afectados
- **ScriptableObjects:** `EnemyDefinition` gana `typeWorldB` + `isAnchor` + la
  propiedad derivada `IsTransdimensional`.
- **Combate (PROTEGIDO):** `TurnManager` resuelve el tipo activo del enemigo según
  el mundo actual / ancla. **[REQUIERE APROBACIÓN]**
- **UI:** `CombatHudView` renderiza ambos tipos cuando el enemigo es transdim.
- **Tests:** `CombatTestBase` + `EnemyDefinition.SetDebugData` extendidos; nuevo
  `EnemyTransdimTests`.
- **Editor:** nuevo `EnemyConfigSetup` (genera SOs de prueba para E2E).

## Archivos a crear
- `Assets/Editor/EnemyConfigSetup.cs` — generador idempotente (molde
  `EventConfigSetup`) con `MenuItem "Roguelike/Setup Enemy Test Data (4c)"`. Crea
  **2 SOs**: `TransdimTestEnemy` (transdim) + `AnchorTestEnemy` (ancla). NO toca
  los 5 SOs existentes.
- `Assets/Tests/EditMode/EnemyTransdimTests.cs` — tests de resolución de tipo por
  mundo, defaults de los campos nuevos y `IsTransdimensional`.

## Archivos a modificar
- `Assets/Scripts/Gameplay/Enemies/EnemyDefinition.cs` (NO protegido) — agregar
  campos `typeWorldB` + `isAnchor`, propiedades getter, propiedad derivada
  `IsTransdimensional`, y extender `SetDebugData`.
- `Assets/Scripts/Gameplay/Combat/TurnManager.cs` — **PROTEGIDO — [REQUIERE APROBACIÓN]**
  — 3 ediciones quirúrgicas (ver Contratos).
- `Assets/Scripts/Gameplay/Combat/CombatHudView.cs` (NO protegido) — `BuildEnemyTypeLabel()`
  + tinte del label para el caso de dos tipos.
- `Assets/Scripts/Gameplay/Combat/ElementTypeColors.cs` (NO protegido) — overload
  `TypePrefix(type, brightness)` para el tipo atenuado (reuso, ver Contratos).
- `Assets/Tests/EditMode/CombatTestBase.cs` (test helper) — `CreateEnemyDefinition`
  acepta `typeWorldB` + `isAnchor` opcionales.

## Archivos protegidos involucrados
- [ ] Ninguno
- [x] `TurnManager.cs` — **REQUIERE APROBACIÓN**. Tres ediciones, todas
  centralizadas en una sola propiedad:
  1. **Getter `EnemyElementType` ([TurnManager.cs:112](../../../Assets/Scripts/Gameplay/Combat/TurnManager.cs#L112))** — hoy
     `=> enemyDefinition != null ? enemyDefinition.ElementType : ElementType.None;`.
     Pasa a resolver por mundo/ancla (pseudocódigo en Contratos).
  2. **Línea [TurnManager.cs:416](../../../Assets/Scripts/Gameplay/Combat/TurnManager.cs#L416)** —
     `QueueEffects(move.Effects, _enemy, _player, enemyDefinition?.ElementType ?? ElementType.None);`
     → el 4º argumento pasa a `EnemyElementType` (enemigo como **atacante**).
  3. **Línea [TurnManager.cs:624](../../../Assets/Scripts/Gameplay/Combat/TurnManager.cs#L624)** —
     `ElementType defenderType = enemyDefinition != null ? enemyDefinition.ElementType : ElementType.None;`
     → `ElementType defenderType = EnemyElementType;` (enemigo como **defensor**).

  > Hoy esos tres sitios leen `enemyDefinition.ElementType` directo. Tras el
  > cambio, **todos** consumen el tipo resuelto a través de un único punto de
  > verdad (el getter). No se agregan campos, eventos ni métodos públicos nuevos
  > a TurnManager; sólo cambia la **semántica** del getter (firma idéntica).

## Contratos

### Datos — `EnemyDefinition`
Agregar junto a `elementType` (insertar tras la línea 22, antes de `avatar`):

| Campo | Tipo | Default | Significado |
|-------|------|---------|-------------|
| `elementType` (existente) | `ElementType` | `None` | Tipo en **Mundo A** / tipo **fijo** (para tipo-único y ancla) |
| `typeWorldB` (nuevo) | `ElementType` | `None` | Tipo en **Mundo B**. Sólo significativo si el enemigo es transdim |
| `isAnchor` (nuevo) | `bool` | `false` | Si `true`, el tipo NO reacciona al cambio de mundo |

Propiedades públicas (patrón lambda read-only existente, tras la línea 34):
```csharp
public ElementType TypeWorldB => typeWorldB;
public bool IsAnchor => isAnchor;

// Derivada: un enemigo es transdimensional si tiene un tipo de Mundo B
// distinto y NO es ancla. No es un flag serializado (evita superficie de
// autoría redundante) — se computa de los otros dos campos.
public bool IsTransdimensional => !isAnchor && typeWorldB != ElementType.None;
```

**Backward-compat:** los 5 SOs existentes quedan con `typeWorldB = None` e
`isAnchor = false` por defecto → `IsTransdimensional == false` →
`EnemyElementType` devuelve `elementType` en ambos mundos. Comportamiento
idéntico al actual. (Cubierto por test de regresión.)

### Resolución — `TurnManager.EnemyElementType` (getter, PROTEGIDO)
```
si enemyDefinition == null            → ElementType.None
si enemyDefinition.IsAnchor           → enemyDefinition.ElementType      // ancla: ignora el mundo
si currentWorld == B && TypeWorldB != None → enemyDefinition.TypeWorldB  // transdim, lado B
en otro caso                          → enemyDefinition.ElementType      // Mundo A o tipo único
```
- **Precedencia:** `isAnchor` gana sobre `typeWorldB` (un ancla con `typeWorldB`
  seteado por error igual ignora el mundo).
- Es el **único** lugar de resolución; las 3 lecturas (112/416/624) pasan por él.

### APIs públicas
- `EnemyDefinition.TypeWorldB`, `.IsAnchor`, `.IsTransdimensional` (getters nuevos).
- `TurnManager.EnemyElementType` — **firma sin cambios**; cambia la semántica
  (ahora resuelto por mundo/ancla). `CurrentEnemyDefinition`
  ([TurnManager.cs:113](../../../Assets/Scripts/Gameplay/Combat/TurnManager.cs#L113))
  ya expone el SO → el HUD lee de ahí los dos tipos crudos, **no se agrega API
  nueva a TurnManager para la ficha**.
- `ElementTypeColors.TypePrefix(ElementType type, float brightness = 1f)` —
  overload nuevo (NO protegido): con `brightness < 1f` aplica
  `Dim(ReadableOnDark(type), brightness)` al color del rich-text. El overload
  sin parámetro mantiene el comportamiento actual.

### Eventos
- Ninguno nuevo. La ficha se refresca por el polling existente de
  `CombatHudView.Sync()` (corre cada frame), así que el resaltado conmuta solo al
  cambiar de mundo sin necesidad de eventos.

### UI — `CombatHudView.BuildEnemyTypeLabel()` ([CombatHudView.cs:265-274](../../../Assets/Scripts/Gameplay/Combat/CombatHudView.cs#L265))
```
def = _turnManager.CurrentEnemyDefinition
si def == null → "Type: —"

si !def.IsTransdimensional:                       // tipo único o ancla → 1 tipo
    type = _turnManager.EnemyElementType          // resuelto
    return type == None ? "Type: —" : $"Type: {type}"

si def.IsTransdimensional:                         // 2 tipos
    activoEsA = _turnManager.CurrentWorld == WorldSide.A
    prefijoA  = activoEsA ? TypePrefix(def.ElementType) : TypePrefix(def.ElementType, DIM)
    prefijoB  = activoEsA ? TypePrefix(def.TypeWorldB, DIM) : TypePrefix(def.TypeWorldB)
    return $"Type: {prefijoA} / {prefijoB}"
```
- `DIM` = factor de atenuación (constante local, p. ej. `0.45f`).
- **Tinte del label** ([CombatHudView.cs:170-173](../../../Assets/Scripts/Gameplay/Combat/CombatHudView.cs#L170)):
  cuando el enemigo es transdim, poner `_enemyTypeLabel.color = Color.white` (el
  color lo aporta el rich-text inline por tipo; un único `Text` no puede tener
  dos `.color`). Para tipo único / ancla, mantener el `ReadableOnDark` actual.

## Reuso
- `ElementTypeColors.TypePrefix(type)` (rich-text `<color=#hex>[Tipo]</color>`),
  `.Dim(color, factor)`, `.ReadableOnDark(type)` — ya existen y están testeados
  (`ElementTypeColorsTests`).
- `TurnManager.CurrentWorld`, `WorldSide`, `SetCurrentWorldForTest`,
  `CurrentEnemyDefinition` — ya existen.
- `CombatTestBase.CreateEnemyDefinition` + `EnemyDefinition.SetDebugData` —
  patrón existente de construcción de SOs en memoria; sólo se extienden con
  parámetros opcionales con default (no rompen llamadas existentes).
- `EventConfigSetup.cs` — molde del generador idempotente (`EnsureFolder`,
  `AssetDatabase.CreateAsset`, chequeo de existencia previa).
- Patrón `[SerializeField] private` + getter lambda de `EnemyDefinition`.

## Casos de prueba (EditMode)
Todos construyen enemigos **en memoria** (no requieren SOs en disco):

1. **Defaults (regresión / guard):** `EnemyDefinition` recién creado tiene
   `TypeWorldB == None`, `IsAnchor == false`, `IsTransdimensional == false`.
   (Extiende o acompaña a `DefinitionTypeDefaultsTests`.)
2. **`IsTransdimensional`:** `true` sólo con `typeWorldB != None && !isAnchor`;
   `false` si es ancla; `false` si `typeWorldB == None`.
3. **Resolución transdim sigue el mundo:** enemigo con `elementType=Rojo`,
   `typeWorldB=Azul`, `isAnchor=false` → `EnemyElementType == Rojo` en Mundo A,
   `== Azul` en Mundo B (vía `SetCurrentWorldForTest`).
4. **Resolución ancla ignora el mundo:** enemigo con `elementType=Morado`,
   `isAnchor=true` (aun con `typeWorldB=Azul` seteado) → `EnemyElementType ==
   Morado` en ambos mundos.
5. **Resolución tipo único (backward-compat):** `typeWorldB=None`,
   `isAnchor=false` → `EnemyElementType == elementType` en ambos mundos.
6. **Combate orgánico (la resolución fluye al daño):** un transdim cuyo tipo de
   Mundo A es SuperEficaz-recibido y el de Mundo B no, según la tabla **del
   código** (`ElementEffectiveness.GetEffectiveness`). Jugar una carta tipada en
   Mundo A y verificar el daño/multiplicador; cambiar a Mundo B y verificar que
   cambia. *No re-asierta los valores de la tabla de efectividad* (eso es de
   `ElementEffectivenessTests`); asierta que **el tipo activo usado** cambió con
   el mundo. Reusa el patrón de `DamageEffectivenessTests`.
7. **Regresión:** `DamageEffectivenessTests` y `AffinityTests` siguen en verde
   sin cambios de aserción (sólo recompilan por la firma extendida de
   `CreateEnemyDefinition` con defaults).

## Validación manual (BattleScene)
1. Ejecutar menú **`Roguelike/Setup Enemy Test Data (4c)`** → genera
   `TransdimTestEnemy.asset` y `AnchorTestEnemy.asset`.
2. Asignar `TransdimTestEnemy` al combate (vía nodo/config) e iniciar BattleScene.
3. **Resultado esperado:** la ficha muestra `Type: [Rojo] / [Azul]`, con el tipo
   del mundo activo (A=Rojo) a color pleno y Azul atenuado.
4. Cambiar de mundo → el resaltado se invierte (Azul pleno, Rojo atenuado) y el
   daño de una carta tipada cambia de efectividad contra el enemigo.
5. Repetir con `AnchorTestEnemy` → un solo tipo en la ficha; cambiar de mundo NO
   lo altera.
6. Validar con Unity-MCP (compilación limpia, cero errores en consola).

## Decisiones cerradas
- Mecanismo dimensional **DD-014** cerrado por diseño (Estándar / Ancla).
- **Sin tercer flag serializado:** "transdimensional" se **deriva**
  (`typeWorldB != None && !isAnchor`).
- `elementType` = tipo **Mundo A** / tipo **fijo**; `typeWorldB` = tipo **Mundo B**.
- **`isAnchor` precede a `typeWorldB`** en la resolución.
- **Backward-compat por defaults** (`None` / `false`) → los SOs existentes no
  cambian de comportamiento.
- **Ficha:** un solo `Text`, rich-text, ambos tipos, inactivo atenuado con `Dim`
  *(decisión de Sebastián, 2026-06-23)*.
- **SOs de prueba:** editor generator nuevo (`EnemyConfigSetup`), 2 SOs, sin tocar
  los existentes; tests EditMode in-memory *(decisión de Sebastián, 2026-06-23)*.
- **TurnManager:** 3 ediciones (getter + 2 read-sites), todo canalizado por una
  única propiedad. Cambio de **semántica**, no de firma.
- La tabla de efectividad sigue resolviéndose en `ElementEffectiveness` sin
  cambios (4c sólo decide **qué tipo** entra, no los multiplicadores).

## Decisiones abiertas (REQUIEREN cierre antes de implementar)
Ninguna que bloquee 4c. Dos notas **fuera de scope** (no tocar en este Sub-PR):

- **[CONTRADICCIÓN — fuera de scope]** La tabla de efectividad de GOLDEN_RULES §3
  difiere del código (`ElementEffectiveness.GetEffectiveness`) en
  Rojo↔Amarillo/Azul y Amarillo→Rojo. **No afecta a 4c**: 4c resuelve *qué tipo*
  está activo, no los multiplicadores, y los tests de 4c asertan resolución (no
  valores de la tabla). Decidir fuente de verdad (corregir doc o código) es un
  ítem separado, abierto para Sebastián.
- **[Nota — stale]** GOLDEN_RULES §6 dice "PhaseBased: declarado, no implementado
  aún"; ya se implementó en Sub-PR D (2026-05-07). No es parte de 4c; corregir el
  doc aparte.

## Alternativas consideradas
- **Resolver el tipo del enemigo fuera de TurnManager (como afinidad en 4a):**
  descartada. 4a no tocó TurnManager porque ya leía `GetActiveCard(CurrentWorld).ElementType`
  y el `AffinityResolver` producía una dual runtime tipada por mundo. El enemigo
  **no tiene** una abstracción de "lado activo": hay un único campo
  `enemyDefinition` y la resolución necesita `currentWorld`, que vive dentro de
  TurnManager. No hay forma de evitar el toque al protegido.
- **Flag `IsTransdimensional` serializado:** descartada — redundante y derivable;
  agrega superficie de error de autoría (un SO con `typeWorldB` seteado pero el
  flag olvidado).
- **`EnemyDefinition.ResolveActiveType(WorldSide)`:** descartada — acoplaría el
  namespace `Enemies` a `TurnManager.WorldSide` (enum anidado en Combat). Mejor
  mantener el SO como **data pura** y dejar la resolución por mundo en TurnManager.
- **Ficha con dos labels apilados / marcador `▸` en el activo:** descartadas por
  decisión (ver Decisiones cerradas).

## Estimación
- **Complejidad:** media (coincide con el roadmap).
- **Sub-tareas:**
  1. `EnemyDefinition`: campos + getters + `IsTransdimensional` + `SetDebugData`.
  2. `TurnManager`: 3 ediciones **[REQUIERE APROBACIÓN]**.
  3. `ElementTypeColors`: overload `TypePrefix(type, brightness)` (+ su test).
  4. `CombatHudView`: render de dos tipos + tinte.
  5. `CombatTestBase` + `EnemyTransdimTests`.
  6. `EnemyConfigSetup` (editor generator).
- **Riesgo:** bajo-medio. El único riesgo real es el toque al protegido, acotado
  a 3 líneas y centralizado en un getter. Backward-compat cubierto por defaults +
  test de regresión. La discrepancia de la tabla §3 NO entra al camino crítico.

## Prompt de handoff para `modo:implementacion`

```text
modo:implementacion

Implementá el Sub-PR M4 4c — Enemigos Transdimensionales + Ancla. El spec cerrado
está en Docs/dev/specs/m4_4c_transdim_ancla_spec.md — leelo completo antes de
tocar código; todas las decisiones de diseño ya están cerradas, no abras ninguna.

Setup de branch (dependencias previas — todas MERGED: 4b PRs #125/#126/#127;
main en ff0d1cd):
  git fetch --all --prune
  git checkout -b feat/m4-4c-transdim-ancla origin/main

Qué construir (resumen; el detalle y los contratos están en el spec):
- Modificar Assets/Scripts/Gameplay/Enemies/EnemyDefinition.cs (NO protegido):
  +typeWorldB (ElementType=None), +isAnchor (bool=false), getters, propiedad
  derivada IsTransdimensional, y extender SetDebugData con ambos params opcionales.
- Modificar Assets/Scripts/Gameplay/Combat/TurnManager.cs (PROTEGIDO): getter
  EnemyElementType resuelve por mundo/ancla (pseudocódigo en el spec); líneas 416
  y 624 leen por la propiedad EnemyElementType. Cambio de semántica, no de firma.
- Modificar Assets/Scripts/Gameplay/Combat/ElementTypeColors.cs (NO protegido):
  overload TypePrefix(type, brightness=1f) que atenúa con Dim cuando brightness<1.
- Modificar Assets/Scripts/Gameplay/Combat/CombatHudView.cs (NO protegido):
  BuildEnemyTypeLabel renderiza dos tipos cuando IsTransdimensional (activo pleno,
  inactivo atenuado) + tinte del label a Color.white en ese caso.
- Modificar Assets/Tests/EditMode/CombatTestBase.cs: CreateEnemyDefinition acepta
  typeWorldB + isAnchor opcionales (default None/false).
- Crear Assets/Tests/EditMode/EnemyTransdimTests.cs: casos 1-6 del spec (+ correr
  la suite completa para el caso 7 de regresión).
- Crear Assets/Editor/EnemyConfigSetup.cs: MenuItem "Roguelike/Setup Enemy Test
  Data (4c)" idempotente (molde EventConfigSetup) → TransdimTestEnemy + AnchorTestEnemy.

Reglas no negociables:
- TurnManager.cs es PROTEGIDO: al editarlo, el hook protect-files pedirá allow/deny.
  PARAR y confirmar con Sebastián antes de aplicar las 3 ediciones. NO delegar la
  edición de TurnManager a un subagente — se hace en el hilo principal.
- NO tocar ActionQueue.cs ni PlayerCombatActor.cs.
- No manual editor setup: los SOs de prueba los genera el MenuItem, no a mano.
- FUERA de scope: la discrepancia de la tabla de efectividad §3 vs código, y el
  comportamiento dimensional del BOSS (eso es M5). 4c sólo resuelve el tipo activo
  de enemigos normales + ficha.
- No agregar un flag IsTransdimensional serializado — es derivado.

Validación obligatoria antes de cerrar:
- Compilación limpia (cero errores en consola).
- Tests EditMode: EnemyTransdimTests nuevos en verde + suite completa sin
  regresiones (hoy 221; quedan ~227-228).
- Correr el menú Roguelike/Setup Enemy Test Data (4c) y verificar que crea los 2 SOs.
- Flujo end-to-end en BattleScene: ficha de TransdimTestEnemy muestra dos tipos,
  conmuta el resaltado al cambiar de mundo y cambia la efectividad del daño; el
  AnchorTestEnemy no cambia al cambiar de mundo.
- Validar con Unity-MCP (compilación + cero errores).

Al cerrar: actualizá _roadmap.md (marcar los 3 checkboxes de 4c [x]; 4c es el
último bloque de M4 → marcar M4 completo), _tech_snapshot.md (EnemyDefinition con
typeWorldB/isAnchor/IsTransdimensional, getter resuelto de TurnManager,
CombatHudView con dos tipos, EnemyConfigSetup). Proponer a Sebastián marcar en
GOLDEN_RULES §2 (tipo activo transdim ✓ / ancla ✓) y §6 (categorías dimensionales)
como implementados — esos docs los confirma él. Commit + push + PR a main
(Closes #<issue 4c> si existe).
```
