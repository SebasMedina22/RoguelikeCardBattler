# Spec — Swap de avatar del enemigo por mundo (cierra C4)

> **ESTADO: PROPUESTA — CERRADA (lista para `modo:implementacion`).** Todas las
> decisiones de diseño están cerradas. Handoff al final.
> (Al implementarse, cambiar a `IMPLEMENTADO — PR #N (YYYY-MM-DD)`.)
>
> **Alcance cerrado (Sebastián, sesión de diseño):** este PR cubre **solo el avatar
> del ENEMIGO** (boss transdim → cierra deuda de arte **C4**). El héroe (**C5**) se
> diseña acá como **apéndice-extensión** pero **NO se implementa en este PR**; queda
> listo para un sub-PR posterior.

## Origen
- Deuda de arte **C4** (avatar del boss dual-mundo) del audit de arte
  ([[project-art-audit-plan]]): hoy el boss transdim muestra la misma cara en Mundo A
  y B.
- Roadmap M5, sección "Pendientes / swap de avatar por mundo".
- Arte ya producido: `personaje_costura_maldita.png` (Mundo A, ya asignado) y
  `personaje_unit_rb7.png` (Mundo B, en disco sin asignar).

## Objetivo
Que el avatar del enemigo transdimensional conmute su sprite al cambiar de mundo
(cara de Mundo A ↔ cara de Mundo B), en sincronía con la ficha de dos tipos que ya
conmuta hoy. Es **pulido de presentación puro**: cero cambios de reglas de juego,
cero archivos protegidos.

## Comportamiento esperado (perspectiva del jugador)
1. En un combate contra el boss transdim (Costura Maldita / UNIT-RB7), en **Mundo A**
   se ve la cara medieval; al cambiar a **Mundo B** el avatar pasa a la cara cyberpunk
   (y viceversa), igual que la ficha de tipos ya resalta/atenúa.
2. El swap es **instantáneo** (no fade — ver DC4).
3. Para cualquier enemigo **no transdimensional** (single-type, ancla, o transdim sin
   arte de Mundo B asignado) el avatar **no cambia nunca** → cero regresión.
4. Vale para cualquier origen del cambio de mundo (botón Change World hoy; y el
   Desfase Dimensional forzado por IA cuando llegue el Sub-PR C de M5, gratis — ver
   "Reuso / propiedad emergente").

---

## Sistemas afectados
- **UI de combate:** `CombatHudView` (que ya posee el avatar del enemigo y hace
  polling del mundo) resuelve y aplica el sprite del mundo activo.
- **Animación de avatar:** `SpriteFrameAnimatorUI` necesita poder actualizar su
  "sprite en reposo" para que el flash de impacto post-swap restaure la cara correcta.
- **Datos (SO):** `EnemyDefinition` gana un campo `avatarWorldB` + resolutor
  `ResolveAvatar(bool)`.
- **Setup editor:** `BossConfigSetup` asigna ambos avatares por menú (no manual).
- **Combate (TurnManager / ActionQueue / PlayerCombatActor):** **ninguno.** No se
  tocan protegidos.

---

## Arquitectura y punto de enganche

### [CÓDIGO ACTUAL] Cómo se muestra hoy el avatar del enemigo
- El sprite del enemigo se asigna **una sola vez** en
  [CombatUIController.cs:303](Assets/Scripts/Gameplay/Combat/CombatUIController.cs#L303)
  (`finalEnemySprite = turnManager.CurrentEnemyDefinition.Avatar`) y se pinta dentro de
  `CreateAvatar(...)` en el hijo `EnemyAvatar_Sprite`
  ([CombatUIController.cs:598](Assets/Scripts/Gameplay/Combat/CombatUIController.cs#L598)).
  **Nunca se re-asigna al cambiar de mundo.**
- Ese hijo `EnemyAvatar_Sprite` (`_enemyAvatarSpriteImage`) tiene montado un
  `SpriteFrameAnimatorUI` (`_enemyAnimator`) con **idle vacío** y `enemyHitFrames` para
  el flash de impacto
  ([CombatUIController.cs:738](Assets/Scripts/Gameplay/Combat/CombatUIController.cs#L738)).
- **Detalle crítico (fix #103):** como el idle está vacío, tras un ataque el animator
  restaura `_restingSprite` — el sprite capturado en `Configure()`
  ([SpriteFrameAnimatorUI.cs:33](Assets/Scripts/Gameplay/Combat/SpriteFrameAnimatorUI.cs#L33),
  restaurado en [:111](Assets/Scripts/Gameplay/Combat/SpriteFrameAnimatorUI.cs#L111)).
  → Un swap de mundo debe actualizar **ese `_restingSprite`**, no solo el `.sprite` del
  Image; si no, el próximo flash de impacto revierte a la cara vieja.

### [CÓDIGO ACTUAL] No hay evento de cambio de mundo — las vistas hacen polling
`TurnManager.TryChangeWorld()` muta `currentWorld` y dispara **solo** el relic hook
`OnWorldSwitch` (dirigido a Retazos, no a vistas)
([TurnManager.cs:785-815](Assets/Scripts/Gameplay/Combat/TurnManager.cs#L785)). Las
vistas reaccionan por **polling cada frame** leyendo `TurnManager.CurrentWorld`:
- `CombatHudView.Sync()` redibuja la ficha de tipos y el label de mundo cada frame
  ([CombatHudView.cs:169-196](Assets/Scripts/Gameplay/Combat/CombatHudView.cs#L169)).
- `CombatBackgroundView` ya hace **swap de sprite por mundo** con un centinela
  `_lastKnownWorld` + `image.sprite = target`
  ([CombatBackgroundView.cs:97-121](Assets/Scripts/Gameplay/Combat/CombatBackgroundView.cs#L97))
  → **este es el patrón de referencia exacto** a replicar para el avatar.

> Corrección al enunciado de partida: el prompt decía "CombatHudView se suscribe al
> cambio de mundo". [CÓDIGO ACTUAL] **no se suscribe: hace polling** (ver docstring
> [CombatHudView.cs:17-20](Assets/Scripts/Gameplay/Combat/CombatHudView.cs#L17)). El
> swap se engancha al **mismo polling**, no a un evento.

### [PROPUESTA] Dónde vive el swap
En **`CombatHudView`** (no protegido): ya posee la referencia al avatar del enemigo,
ya hace polling de `CurrentWorld` y `CurrentEnemyDefinition`, y ya manipula el avatar
(dim/highlight por turno). Se le inyecta **una referencia nueva** —el
`SpriteFrameAnimatorUI` del enemigo— y en `Sync()` resuelve el avatar por mundo con un
centinela, llamando a `SetRestingSprite(...)` solo cuando el mundo cambió.

[ALTERNATIVA] *Nuevo MonoBehaviour `EnemyAvatarWorldView`* (hermano, espejo de
`CombatBackgroundView`, polling autónomo). Más desacoplado, pero es un archivo + wiring
nuevo para conmutar **un** sprite; `CombatHudView` ya es el dueño natural del avatar del
enemigo. Descartada por sobre-ingeniería para un swap de un sprite.

[ALTERNATIVA] *Hacer el swap en `CombatUIController.Update()`* (donde viven
`_enemyAnimator` y el sentinel sería trivial). Descartada: la regla del directorio
([Combat/CLAUDE.md]) pide **NO agregar métodos/responsabilidades a CombatUIController**;
solo se permite wiring en `InitializeExtractedViews()`.

---

## Archivos a crear
- `Assets/Tests/EditMode/EnemyAvatarByWorldTests.cs` — tests de `ResolveAvatar(bool)`
  (plantilla `EnemyTransdimTests`). NO protegido.

## Archivos a modificar (todos NO protegidos)
- `Assets/Scripts/Gameplay/Enemies/EnemyDefinition.cs` — campo `avatarWorldB` + getter
  `AvatarWorldB` + método `ResolveAvatar(bool)` + seam de test para inyectar sprites.
- `Assets/Scripts/Gameplay/Combat/SpriteFrameAnimatorUI.cs` — método público
  `SetRestingSprite(Sprite)`.
- `Assets/Scripts/Gameplay/Combat/CombatHudView.cs` — ref inyectada al animator del
  enemigo + swap por mundo (centinela) en `Sync()`.
- `Assets/Scripts/Gameplay/Combat/CombatUIController.cs` — **solo wiring**: pasar
  `_enemyAnimator` a `hudView.Initialize(...)` en `InitializeExtractedViews()`. Sin
  métodos ni responsabilidades nuevas.
- `Assets/Editor/BossConfigSetup.cs` — el MenuItem existente también asigna `avatar`
  (A) + `avatarWorldB` (B) al `.asset`, idempotente (no manual editor setup).
- `Assets/Tests/EditMode/CombatTestBase.cs` — helper `CreateDummySprite()` y/o seam
  para poblar avatares en el factory de `EnemyDefinition` (si hace falta).

## Archivos protegidos involucrados
- [x] **Ninguno.** No se toca `TurnManager.cs`, `ActionQueue.cs` ni `PlayerCombatActor.cs`.
- No se modifican los contratos 4c (`typeWorldB` / `isAnchor` / `IsTransdimensional`
  ni `TurnManager.EnemyElementType`): el swap los **consume** tal cual (`IsTransdimensional`).

---

## Contratos

### Datos — `EnemyDefinition` (NO protegido)
[CÓDIGO ACTUAL] `avatar` es `[SerializeField] private Sprite avatar` con getter
`Avatar` ([EnemyDefinition.cs:28](Assets/Scripts/Gameplay/Enemies/EnemyDefinition.cs#L28),
[:47](Assets/Scripts/Gameplay/Enemies/EnemyDefinition.cs#L47)); `IsTransdimensional`
es derivada (`!isAnchor && typeWorldB != None`,
[:46](Assets/Scripts/Gameplay/Enemies/EnemyDefinition.cs#L46)).

[PROPUESTA] agregar (análogo a `typeWorldB`, junto al campo `avatar`):
```csharp
// Avatar de Mundo B. Solo significativo si el enemigo es transdimensional; para
// single-type/ancla queda null y se usa el avatar base.
[SerializeField] private Sprite avatarWorldB = null;
public Sprite AvatarWorldB => avatarWorldB;

/// <summary>
/// Avatar activo según el mundo. Conmuta SOLO para transdimensionales con arte de
/// Mundo B asignado; single-type / ancla / B sin asignar → avatar base (cero
/// regresión). El caller pasa un bool (no WorldSide) para no acoplar este SO al
/// enum de combate.
/// </summary>
public Sprite ResolveAvatar(bool worldBActive)
    => (IsTransdimensional && worldBActive && avatarWorldB != null) ? avatarWorldB : avatar;
```

**Fallback (DC2):** cualquier caso que no sea "transdim + Mundo B + `avatarWorldB != null`"
devuelve `avatar`. Esto cubre: single-type, ancla, transdim sin B, y Mundo A. Cero
regresión para todos los enemigos actuales.

**Seam de test:** [CÓDIGO ACTUAL] `SetDebugData(...)` **no** setea `avatar` (no lo
acepta), así que los tests no pueden poblar el sprite hoy. [PROPUESTA] agregar un seam
gated con el mismo `#if UNITY_EDITOR || UNITY_INCLUDE_TESTS` que ya usa `SetDebugData`:
```csharp
#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
public void SetAvatarsForTest(Sprite avatarA, Sprite avatarB)
{
    avatar = avatarA;
    avatarWorldB = avatarB;
}
#endif
```
[ALTERNATIVA] extender `SetDebugData` con dos params opcionales `Sprite avatar = null,
Sprite avatarWorldB = null` (backward-compatible). Descartada como recomendación:
sobrecarga la firma de un seam ya usado por dos config-setups; el setter dedicado es
más honesto y localizado.

### API — `SpriteFrameAnimatorUI` (NO protegido)
[PROPUESTA]:
```csharp
/// <summary>
/// Actualiza el sprite "en reposo" (p.ej. al conmutar el avatar del enemigo por
/// mundo). Lo aplica de inmediato al Image y queda como el sprite a restaurar tras
/// el próximo flash de ataque. Si un flash está en curso, sigue reproduciéndose y al
/// terminar restaura este nuevo resting sprite.
/// </summary>
public void SetRestingSprite(Sprite sprite)
{
    _restingSprite = sprite;
    if (targetImage != null)
    {
        targetImage.sprite = sprite;
    }
}
```
- **Efecto:** cubre las dos rutas — el display inmediato y la restauración post-flash
  (`AttackRoutine` restaura `_restingSprite`,
  [SpriteFrameAnimatorUI.cs:111](Assets/Scripts/Gameplay/Combat/SpriteFrameAnimatorUI.cs#L111)).
- **Idle:** para el enemigo el idle está vacío (no hay `IdleLoop`), así que setear
  `targetImage.sprite` no pelea con ninguna corrutina de idle.

### UI — `CombatHudView` (NO protegido)
[PROPUESTA] inyectar el animator del enemigo y conmutar por polling con centinela:
```csharp
// Nuevos campos
private SpriteFrameAnimatorUI _enemyAnimator;
private bool? _lastEnemyWorldBActive;   // centinela: null → resuelve en el primer Sync

// Initialize(...): un parámetro nuevo al final del bloque de Imágenes/refs cruzadas
//   SpriteFrameAnimatorUI enemyAnimator
_enemyAnimator = enemyAnimator;

// En Sync(), reutilizando el `enemyDef` que ya se lee en la línea ~170:
if (_enemyAnimator != null && enemyDef != null && enemyDef.IsTransdimensional)
{
    bool worldBActive = _turnManager.CurrentWorld == TurnManager.WorldSide.B;
    if (_lastEnemyWorldBActive != worldBActive)
    {
        _lastEnemyWorldBActive = worldBActive;
        Sprite worldAvatar = enemyDef.ResolveAvatar(worldBActive);
        if (worldAvatar != null)
        {
            _enemyAnimator.SetRestingSprite(worldAvatar);
        }
    }
}
```
- **Gating:** solo para `IsTransdimensional` → enemigos comunes/ancla no entran nunca
  (cero regresión, cero llamadas al animator).
- **Centinela:** `_lastEnemyWorldBActive` evita reasignar cada frame; `null` inicial
  fuerza una resolución en el primer `Sync()` (robusto ante cualquier mundo de arranque,
  aunque hoy el combate arranca en A).
- **`_enemyAnimator` puede ser null** si el enemigo no tiene sprite (no se crea el hijo
  `_Sprite` → `_enemyAnimator` queda null,
  [CombatUIController.cs:714-723](Assets/Scripts/Gameplay/Combat/CombatUIController.cs#L714)).
  El guard `!= null` lo cubre.

### Wiring — `CombatUIController` (NO protegido, solo wiring)
Pasar el campo ya existente `_enemyAnimator` a `hudView.Initialize(...)` en
`InitializeExtractedViews()` (~[CombatUIController.cs:485-520](Assets/Scripts/Gameplay/Combat/CombatUIController.cs#L485)).
`_enemyAnimator` ya está inicializado antes (BuildUI → `InitializeEnemyAnimator()` en
[:321](Assets/Scripts/Gameplay/Combat/CombatUIController.cs#L321), previo a
`InitializeExtractedViews`). **No se agrega ningún método** — es un argumento más en una
llamada existente, en el punto de integración permitido por [Combat/CLAUDE.md].

### Setup de arte — `BossConfigSetup` (NO protegido)
[PROPUESTA] el MenuItem existente `Roguelike/Setup Boss Act 1 (M5 A)`, tras autorar el
`.asset`, asigna ambos avatares idempotentemente (patrón `EventConfigSetup.FindBackground`
+ `SerializedObject`, ya usado en el proyecto):
```
Rutas:
  A: Assets/Art/Sprites/Enemies/CosturaMaldita/A/Idle/personaje_costura_maldita.png
  B: Assets/Art/Sprites/Enemies/CosturaMaldita/B/Idle/personaje_unit_rb7.png

Pasos (editor):
  1) Asegurar que ambos PNG estén como Sprite Single (TextureImporter.spriteImportMode
     = Single + ImportAsset/Refresh si difiere) → LoadAssetAtPath<Sprite> los resuelve.
  2) var so = new SerializedObject(bossDef);
     - so.FindProperty("avatarWorldB").objectReferenceValue = LoadAssetAtPath<Sprite>(B);
     - si so.FindProperty("avatar").objectReferenceValue == null:
         so.FindProperty("avatar").objectReferenceValue = LoadAssetAtPath<Sprite>(A);
     - so.ApplyModifiedProperties();
  3) EditorUtility.SetDirty(bossDef); AssetDatabase.SaveAssets();
```
- **Idempotente:** si `avatar` (A) ya está asignado (hoy lo está), no lo pisa; siempre
  (re)asigna `avatarWorldB` (B). Correr el menú N veces converge al mismo estado.
- **No manual editor setup:** import mode + asignación las hace el menú, no el inspector.

> **[Residual R1 — confirmar en implementación, no bloquea]** `personaje_unit_rb7.png`
> hoy está como Sprite **Multiple** (2 sub-sprites). [PROPUESTA] forzarlo a **Single**
> (es un único personaje idle, no un atlas), igual que el fix de fondos de evento
> ([[project-art-audit-plan]], "PNG entra como Multiple → forzar Single"). Si al hacer
> eyeball el Single recorta mal (canvas con padding), [ALTERNATIVA] mantener Multiple y
> referenciar el sub-sprite correcto por nombre (`LoadAllAssetsAtPath(...).OfType<Sprite>()`),
> tal como se asignó el avatar A. Decidir por lo que se vea bien en BattleScene.

---

## Reuso (no reinventar)
- **`CombatBackgroundView`** — molde exacto de "swap de sprite por mundo por polling con
  centinela". El swap del avatar es el mismo patrón, aplicado al animator del enemigo.
- **`SpriteFrameAnimatorUI._restingSprite`** — mecanismo ya existente (fix #103); solo se
  expone un setter.
- **Contrato transdim 4c** — `IsTransdimensional` (gating) + `CurrentWorld` (polling),
  consumidos tal cual.
- **`EventConfigSetup.FindBackground` + `SerializedObject`** — patrón editor de
  "cargar sprite por path y asignarlo a un SO".
- **`EnemyTransdimTests`** — plantilla de tests de resolución por mundo.

### Propiedad emergente (de regalo para M5 Sub-PR C)
Como el swap es por **polling** (no atado al handler del botón Change World), cuando el
**Desfase Dimensional** (M5 Sub-PR C) fuerce el cambio de mundo desde la IA, el avatar
conmutará **automáticamente**, sin trabajo extra ni acoplamiento con esa mecánica. Es la
misma razón por la que `CombatBackgroundView` es polling (ver su docstring).

---

## Casos de prueba (EditMode) — `EnemyAvatarByWorldTests`
Unidad bajo prueba: `EnemyDefinition.ResolveAvatar(bool)`. Sprites dummy vía
`Sprite.Create(new Texture2D(2,2), new Rect(0,0,2,2), new Vector2(0.5f,0.5f))`.

1. **Transdim con B asignado, Mundo A:** enemigo `IsTransdimensional`, `avatar=A`,
   `avatarWorldB=B` → `ResolveAvatar(false) == A`.
2. **Transdim con B asignado, Mundo B:** mismo enemigo → `ResolveAvatar(true) == B`.
3. **Transdim SIN B (fallback):** `avatarWorldB=null` → `ResolveAvatar(true) == A`.
4. **Single-type (no transdim):** `typeWorldB=None` → `ResolveAvatar(true) == A`
   (no conmuta aunque el flag de mundo diga B).
5. **Ancla:** `isAnchor=true` (con `typeWorldB` cualquiera) → `IsTransdimensional=false`
   → `ResolveAvatar(true) == A`.

**Regresión:** la suite EditMode completa (parte de **235**) debe subir sin
regresiones. `SpriteFrameAnimatorUI.SetRestingSprite` no lleva test unitario dedicado
(requeriría montar un MonoBehaviour+Image en EditMode por un setter trivial); su efecto
se valida por eyeball (paso 4 de validación manual).

## Validación manual (BattleScene)
1. Correr el MenuItem `Roguelike/Setup Boss Act 1 (M5 A)`; verificar que el `.asset` del
   boss quede con `avatar` (A) y `avatarWorldB` (B) poblados y sin nulls/morado.
2. Lanzar el combate de boss.
3. En **Mundo A**: se ve la cara medieval (`personaje_costura_maldita`).
4. Pulsar **Change World** → el avatar pasa **instantáneamente** a la cara cyberpunk
   (`personaje_unit_rb7`), en sincronía con la ficha de tipos (activo pleno / inactivo
   atenuado). Volver a A → vuelve la cara medieval.
5. **Post-swap + golpe:** cambiar a B, recibir/observar el flash de impacto del enemigo
   y confirmar que al terminar el flash **queda la cara B** (no revierte a A) — valida
   `SetRestingSprite`.
6. **No regresión:** un enemigo común (p.ej. Slime/Goblin) mantiene su avatar al cambiar
   de mundo (no conmuta).

---

## Decisiones cerradas (no se rediscuten)
- **DC1 — Alcance.** Este PR cubre **solo el avatar del enemigo** (cierra C4). El héroe
  (C5) es apéndice-extensión, NO se implementa acá.
- **DC2 — Contrato de datos + fallback.** `avatarWorldB` en `EnemyDefinition` (análogo a
  `typeWorldB`). `ResolveAvatar(bool)` devuelve `avatar` en todo caso que no sea
  "transdim + Mundo B + `avatarWorldB != null`" → cero regresión.
- **DC3 — Dónde vive el swap.** En `CombatHudView` (dueño del avatar, ya hace polling),
  vía `SpriteFrameAnimatorUI.SetRestingSprite`. NO toca protegidos; `CombatUIController`
  solo recibe wiring.
- **DC4 — Transición instantánea (no fade).** Regla "animación cosmética
  fire-and-forget"; coherente con el swap instantáneo de fondos (`CombatBackgroundView`).
  Un fade exigiría crossfade de dos imágenes o pelear con el `_restingSprite`/hit-flash;
  sobra para un swap de placeholder. [ALTERNATIVA] juice opcional (punch de escala
  fire-and-forget al conmutar) queda como pulido futuro fuera de scope.
- **DC5 — Gating por `IsTransdimensional`.** El swap solo corre para transdim; single/
  ancla quedan 100% intactos.
- **DC6 — Setup por menú (no manual).** `BossConfigSetup` asigna ambos avatares,
  idempotente.
- **DC7 — Cobertura de test.** Unit de `ResolveAvatar` (5 casos, patrón
  `EnemyTransdimTests`); el swap visual y el resting-sprite se validan por eyeball.

## Estimación
- **Complejidad:** baja. Aditivo, sin protegidos, patrón ya existente
  (`CombatBackgroundView`).
- **Sub-tareas:** (1) campo+resolutor+seam en `EnemyDefinition`; (2) `SetRestingSprite`;
  (3) swap+wiring en HUD/UIController; (4) `BossConfigSetup` asigna avatares; (5) tests.
- **Riesgo:** bajo. Único punto delicado = import mode del PNG B (Residual R1),
  confirmable por eyeball.

---

## Apéndice — C5 (héroe por mundo): diseño listo, FUERA de este PR

> No se implementa en este PR (DC1). Se documenta para que un sub-PR posterior lo tome
> sin rediseñar. Cuando se aborde, hacerlo con su propio `modo:diseno`/handoff.

**[CÓDIGO ACTUAL]** El héroe usa dos listas planas `heroIdleFrames`/`heroAttackFrames`
([CombatUIController.cs:37-38](Assets/Scripts/Gameplay/Combat/CombatUIController.cs#L37)),
configuradas **una vez** en `InitializePlayerAnimator()`
([:708](Assets/Scripts/Gameplay/Combat/CombatUIController.cs#L708)). No hay swap por
mundo. Arte en disco: `Hero/A/Idle` + `Hero/A/Attack` (14) y `Hero/B/Idle` (1) +
`Hero/B/Attack` (16), sin cablear.

**Por qué es más caro que el enemigo:**
- El héroe no es un sprite estático sino **dos secuencias** (idle+attack) por mundo →
  el swap debe **reconfigurar** el `SpriteFrameAnimatorUI` del jugador (cambiar las
  listas), no solo un resting sprite.
- Reconfigurar en vivo debe respetar el estado de animación (no cortar un ataque en
  curso feo; reanudar idle tras el swap).
- El único lugar con las listas + el animator del jugador es `CombatUIController`, que
  la regla del directorio pide **NO engordar** → probablemente exige extraer un
  `HeroAnimationView` (MonoBehaviour hermano) que posea las 4 listas (A/B × idle/attack)
  y conmute por polling — más superficie que el enemigo.

**Boceto [PROPUESTA] para el sub-PR futuro:**
- Nuevas listas `heroIdleFramesB`/`heroAttackFramesB` (dónde: en el view nuevo o en
  CombatUIController como datos, TBD en su diseño).
- Un `HeroAnimationView` que, por polling con centinela (igual patrón), al cambiar de
  mundo llame a `_playerAnimator.Configure(target, idleSet, attackSet, fps)` con el set
  del mundo activo + `PlayIdleLoop()`.
- Fallback: si falta el set B, usar el set A.
- Tests: resolución de set por mundo (A→setA, B→setB, B-faltante→setA).

---

## Prompt de handoff para `modo:implementacion`

```markdown
modo:implementacion

Implementá el "swap de avatar del enemigo por mundo" (cierra la deuda de arte C4). El
spec cerrado está en `Docs/dev/specs/art_c4_avatar_swap_por_mundo_spec.md` — leelo
completo antes de tocar código; todas las decisiones ya están cerradas (DC1-DC7), no
abras ninguna. Alcance: SOLO el avatar del enemigo; el héroe (C5) NO va en este PR (es
el apéndice del spec, para un sub-PR futuro).

Setup de branch (M5 Sub-PR A ya está en main = PR #132, commit 79f57b8):
  git fetch --all --prune
  git checkout -b feat/avatar-swap-por-mundo origin/main
  # NO ramificar desde feat/m5-boss-a-so-fases (ya mergeada).

Qué construir (el detalle y los contratos exactos están en el spec):
- EnemyDefinition.cs — campo `avatarWorldB` + getter `AvatarWorldB` + método
  `ResolveAvatar(bool worldBActive)` (fallback a `avatar` si no es transdim / sin B) +
  seam de test `SetAvatarsForTest(Sprite, Sprite)` gated `#if UNITY_EDITOR || UNITY_INCLUDE_TESTS`.
- SpriteFrameAnimatorUI.cs — método público `SetRestingSprite(Sprite)` (setea
  `_restingSprite` + `targetImage.sprite`).
- CombatHudView.cs — inyectar el `SpriteFrameAnimatorUI` del enemigo en `Initialize(...)`;
  en `Sync()`, con centinela `_lastEnemyWorldBActive`, resolver `enemyDef.ResolveAvatar(...)`
  y llamar `SetRestingSprite` solo al cambiar de mundo (gated en `IsTransdimensional`).
- CombatUIController.cs — SOLO wiring: pasar `_enemyAnimator` a `hudView.Initialize(...)`
  en `InitializeExtractedViews()`. NO agregar métodos.
- BossConfigSetup.cs — el MenuItem también asigna `avatar` (A) + `avatarWorldB` (B) por
  path (SerializedObject), idempotente; forzar los PNG a Sprite Single si hace falta.
- EnemyAvatarByWorldTests.cs — 5 casos de `ResolveAvatar` (spec §Casos de prueba).
- CombatTestBase.cs — helper `CreateDummySprite()` / poblar avatares si el factory lo
  necesita.

Reglas no negociables:
- NO tocar archivos protegidos: TurnManager, ActionQueue, PlayerCombatActor. Este PR no
  los necesita (pulido de presentación puro).
- NO modificar los contratos 4c: EnemyDefinition.typeWorldB/isAnchor/IsTransdimensional
  ni TurnManager.EnemyElementType. Consumirlos tal cual.
- NO agregar métodos/responsabilidades a CombatUIController (regla de Combat/CLAUDE.md):
  solo wiring en InitializeExtractedViews.
- No manual editor setup: los avatares se asignan por el menú de BossConfigSetup.
- Swap instantáneo (no fade). El héroe (C5) queda fuera de scope.

Validación obligatoria antes de cerrar:
- Compilación limpia (zero console errors).
- Correr el MenuItem de BossConfigSetup → el .asset del boss queda con avatar (A) y
  avatarWorldB (B) poblados.
- Tests EditMode nuevos (EnemyAvatarByWorldTests) en verde + suite completa (≥235) sin
  regresiones, validado con Unity-MCP (tests-run EditMode).
- Eyeball en BattleScene: en Mundo A la cara medieval, Change World → cara cyberpunk
  instantánea (en sync con la ficha de tipos), volver a A revierte; tras el flash de
  impacto en Mundo B queda la cara B (no revierte a A); un enemigo común no conmuta.
- Residual R1: confirmar visualmente que el PNG de Mundo B (Single) no queda recortado;
  si sí, referenciar el sub-sprite correcto (ver spec).

Al cerrar: actualizá `_roadmap.md` (marca el pendiente "swap de avatar por mundo" de M5
+ deuda C4 del audit de arte) y `_tech_snapshot.md` (campo `avatarWorldB` +
`ResolveAvatar`, `SetRestingSprite`, swap en CombatHudView), commit + push + PR a main.
Dejá anotado en el PR que C5 (héroe por mundo) sigue como extensión pendiente.
```
