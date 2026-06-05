# Insights — RoguelikeCardBattler

> **Qué es este archivo:** observaciones, ideas y aprendizajes que surgen durante
> el desarrollo, playtesting y diseño. NO es un log técnico ni una lista de TODOs.
> Son chispas que pueden afectar decisiones de diseño, balance o arquitectura.
>
> **Cuándo se actualiza:** cuando algo te sorprende durante un playtest, cuando
> una idea de gameplay surge a mitad de implementación, cuando notas un patrón
> que vale la pena recordar.
>
> **Quién edita:** Sebastián escribe en lenguaje natural. Claude da formato sin
> perder la esencia. Una vez registrado, no se modifica salvo pedido explícito.

---

## Plantilla de insight

```markdown
## Insight [N] — [Título corto] — [Fecha YYYY-MM-DD]
**Contexto:** [qué estaba haciendo cuando surgió la idea — playtesting, implementando feature, leyendo el GDD, diseñando]
**Idea:** [texto libre, lo que se observó o se le ocurrió a Sebastián]
**Conexión con sistemas:** [a qué sistema del juego afecta — combate, mazo, mundos, mapa, economía, etc.]
**Potencial:** [cómo podría impactar el diseño/balance/arquitectura del juego]
**Estado:** [crudo | discutido | aplicado | descartado]
```

---

## Insights registrados

<!-- Los insights se agregan aquí en orden cronológico (más reciente arriba). -->
<!-- No editar insights pasados sin pedido explícito. -->

## Insight 7 — Filtrado de stock de tienda: tipo estricto + factor de sinergia con el mazo (futuro) — 2026-06-04

**Contexto:** diseñando M3 Sub-PR 3D (Tienda). Al cerrar el filtrado de cartas del stock (decisión abierta #6 del spec), Sebastián confirmó filtrado estricto por los 2 tipos del run para la base, pero pidió que más adelante exista un factor que incline qué cartas aparecen hacia las que combinan bien con el mazo actual.

**Idea:** el filtrado de cartas de la tienda en 3D es **estricto por tipo**: solo aparecen cartas cuyo `ElementType` ∈ {tipo A del run, tipo B del run, None}. Sebastián quiere, además y más adelante, un **factor de sinergia**: que la probabilidad de que aparezca una carta se incline hacia las que combinan bien con el mazo que el jugador ya tiene (no solo por tipo elemental). No entra en 3D — la base es aleatorio estricto-por-tipo. Se difiere a una sub-PR posterior.

**Conexión con sistemas:** economía/tienda (M3 3D), generación de stock por seed, sistema de mazo (`RunState.Deck`), futura heurística de "puntuación de sinergia".

**Potencial:** requiere una heurística de sinergia para puntuar cartas candidatas. Opciones a evaluar cuando se aborde: peso por tags compartidos con cartas del mazo, por arquetipo, por curva de coste, o por afinidad con los Retazos activos. Define si la tienda se siente como herramienta dirigida de construcción de mazo vs aleatoriedad pura. Riesgo de balance: muy dirigida → mata variabilidad y vuelve obvias las builds; nula → las tiendas se sienten irrelevantes. Empezar simple (peso por tags compartidos con el mazo) cuando se implemente.

**Estado:** crudo — diferido post-3D. La base de 3D usa filtrado estricto por tipo sin ponderación de sinergia.

---

## Insight 6 — La descripción de la carta es texto fijo en el SO, no se computa desde efectos — 2026-05-10

**Contexto:** validando M3 Sub-PR 3C (Hoguera) en Unity, al mejorar una carta el nombre se actualizaba a "Strike (B)+" pero el cuerpo de texto seguía mostrando "Deal 9 damage" cuando el efecto real ya era 12. La primera versión de `CardUpgradeSetup.cs` solo seteaba `upgradedEffects` (los datos de gameplay). El daño al jugar la carta era correcto — pero el HUD mostraba el número viejo porque toma la string `description` del SO directamente.

**Idea:** `CardDefinition.description` es un campo `string` que el HUD muestra tal cual (`CardHandView.BuildCardLabel` → `activeCard.Description`). Los números visibles ("Deal 6 damage", "Gain 5 Block") son texto pre-renderizado en el SO, no se calculan a partir de `Effects[i].value`. Cualquier sistema que mute los efectos (upgrades, Retazos modificadores permanentes, eventos de mejora futuros) tiene que mutar también la descripción si quiere que el HUD muestre el número nuevo — o se rompe la coherencia entre lo que la carta dice que hace y lo que hace.

**Conexión con sistemas:** mejora de cartas (M3 Sub-PR 3C, M4), Retazos que modifican daño/bloqueo permanentemente (M3 3B+), eventos de transformación de carta (M4), HUD de combate (`CardHandView`).

**Potencial:** dos caminos posibles a evaluar cuando aparezca el caso:
- **A.** Mantener `description` como string fijo, todo sistema mutador debe actualizarla (lo que hace `CardUpgradeSetup` hoy). Pro: tradúce a otros idiomas trivialmente; el diseñador escribe la copy del cuerpo libremente. Contra: cada nueva mecánica que toca números tiene que recordar actualizar el texto.
- **B.** Computar la descripción desde efectos (`description` es un template tipo "Deal {Damage} damage."). Pro: imposible desincronizar números y texto. Contra: pierde flexibilidad de copy editorial; lokalización requiere templating; cartas con efectos complejos (multi-hit, condicionales) se vuelven raras.

Hoy el placeholder usa A. Si en M4 (mejora de cartas con UI dedicada, no placeholder) los upgrades empiezan a ser muchos, conviene re-evaluar B o un híbrido (template default con override manual).

**Estado:** aplicado — `CardUpgradeSetup` ahora setea `upgradedDescription` además de `upgradedEffects`. Lección queda registrada para que la próxima mecánica que mute efectos no cometa el mismo error.

---

## Insight 5 — Scope estricto en archivos protegidos puede dejar comportamiento no obvio en paths secundarios — 2026-05-08

**Contexto:** durante implementación de M3 Sub-PR 3A, el agente respetó "7 puntos exactos de inserción en TurnManager" y no agregó el hook `OnDamageDealt` en el branch de cartas neutras (`ElementType.None`) dentro de `ApplyPlayerToEnemyEffectiveness` (líneas 588-630). Eso hubiera sido un 8º punto fuera del alcance aprobado del spec. La decisión es conservadora y correcta para el alcance — pero deja un comportamiento que para el jugador será contraintuitivo cuando lleguen los primeros Retazos en 3B.

**Idea:** un Retazo de la categoría "Modificador de daño" (ej: "+5 daño al primer ataque") hoy se dispararía en cartas con afinidad de tipo (Strike Rojo, Strike Amarillo, etc.) pero **NO** en cartas neutras. El jugador no tiene forma de saberlo sin leer el código.

La lección: **scope estricto sobre archivos protegidos protege contra scope creep, pero puede dejar comportamiento no obvio en paths secundarios del mismo flujo**. La implementación queda correcta según el spec aprobado, pero la sub-PR siguiente que la consuma se va a chocar con el comportamiento al diseñar contenido. Si en la sesión de diseño se anticipa la necesidad, conviene incluir explícitamente todos los paths del flujo (no solo el "principal") en la lista aprobada — o documentar el límite del scope con la consecuencia de gameplay para que la siguiente sesión de diseño lo tenga en cuenta sin descubrirlo en testeo.

**Conexión con sistemas:** sistema de Retazos (M3 Sub-PR 3B), TurnManager (archivo protegido), efectividad de cartas neutras (DD-002, GOLDEN_RULES §3 y §5).

**Potencial:** dos opciones se evaluaron al diseñar Retazos placeholder en 3B:
- **Opción A (ELEGIDA por Sebastián, 2026-05-08):** los Retazos de daño afectan también cartas neutras. Implica agregar el dispatch en el path `attackerType == ElementType.None` como 8º punto de inserción en TurnManager con aprobación explícita en 3B. Razón: para el jugador es contraintuitivo que un Retazo "+5 al primer ataque" no se active con Strike neutro. Cartas neutras siguen aplicando 90% del daño base (DD-002, GOLDEN_RULES §5) — el modificador del Retazo se suma sobre ese 90%.
- **Opción B (descartada):** Retazos de daño solo afectan cartas con afinidad. Hubiera mantenido cartas neutras como "estabilidad pura sin combos" pero generaba ambigüedad en el texto del Retazo.

**Estado:** discutido — Opción A confirmada. Implementación pendiente: en Sub-PR 3B, agregar el `Dispatch(RelicHook.OnDamageDealt, ...)` también en el branch de cartas None de `ApplyPlayerToEnemyEffectiveness` con aprobación de archivo protegido. El 8º punto de inserción se justifica porque la sub-PR 3B es la primera que va a tener Retazos concretos consumiendo el hook.

---

## Insight 4 — Payloads de hooks de Retazos: 5 campos diferidos por falta de cableado — 2026-05-08

**Contexto:** durante la implementación de M3 Sub-PR 3A (foundations de hooks + dispatcher), tres rondas de revisión técnica del spec detectaron campos en los payloads cuyo dato fuente no existe todavía en la arquitectura del juego o cuya semántica es ambigua en el contexto actual. Se decidió quitarlos de 3A en lugar de cablear infraestructura nueva sin un Retazo concreto que la consuma.

**Idea:** los 5 campos diferidos, cada uno con su causa raíz y qué desbloquea agregarlo. Cuando un Retazo concreto los pida, agregar el campo va junto con el cableado correspondiente — no antes.

| Campo removido | Payload | Causa de diferimiento | Cableado pendiente para reactivarlo |
|---|---|---|---|
| `int TurnNumber` | `PlayerTurnStartHookData` | TurnManager no tiene contador de turno; agregarlo era cambio fuera de los 7 puntos aprobados del archivo protegido | Agregar `_turnNumber++` en `BeginPlayerTurn` (con aprobación de archivo protegido) y exponer vía `TurnManager.TurnNumber`. Workaround: `RelicInstance.Counters` |
| `int TurnsTaken` | `CombatEndHookData` | Mismo: requiere contador de turno en TurnManager | Mismo cableado que `TurnNumber`. Workaround: `RelicInstance.Counters` |
| `bool WasFreeSwitch` | `WorldSwitchHookData` | Semánticamente ambiguo: en M3 todos los switches son "gratis" porque el pool base+bonus es fungible en `TryChangeWorld`. El campo no distingue nada significativo | Si el diseño futuro introduce switches con coste no fungible (ej: oro, HP), entonces el flag tiene sentido — y ese mismo cambio define qué significa "free" |
| `bool IsBoss` | `CombatStartHookData` | Boss/Elite vive en `NodeType` del MapNode, no en `EnemyDefinition`; TurnManager hoy no recibe la referencia del nodo activo | Extender `RunCombatConfig`/`ConfigureCombat` para inyectar el `NodeType` (o un par de bools) del nodo que abrió el combate. Lo conoce `BattleFlowController` |
| `bool IsElite` | `CombatStartHookData` | Mismo que IsBoss | Mismo cableado que IsBoss |

**Conexión con sistemas:** sistema de hooks/dispatcher de Retazos (M3 Sub-PR 3A), TurnManager (archivo protegido), RunCombatConfig/BattleFlowController (cableado de combate desde el run), diseño de Retazos placeholder (M3 Sub-PR 3B).

**Potencial:** la lección de proceso es que los specs de infraestructura tienden a sobre-especificar payloads anticipando todos los Retazos imaginables. Mejor regla: el payload solo lleva campos que (a) ya están disponibles en el call site sin cableado nuevo, o (b) tienen un Retazo concreto que los justifique y cuyo cableado entra en la misma sub-PR. Para reacomodar el roadmap, considerar: ¿alguno de los 5 campos es requisito del pool inicial de Retazos placeholder de 3B? Si sí, su cableado entra como sub-tarea de 3B (no de 3A); si no, queda diferido a la sub-PR donde aparezca el Retazo que lo necesite.

**Estado:** crudo — los 5 campos quedan removidos en 3A, listos para reincorporar cuando 3B (o posterior) defina los Retazos que los necesitan.

---

## Insight 3 — Retazos: diseñar por categoría de hook, no por idea suelta — 2026-05-07

**Contexto:** durante el diseño de M3, al pensar cómo abordar los Retazos placeholder iniciales (DD-012). Sin contenido base heredado, había que diseñar desde cero el primer pool de Retazos del juego.

**Idea:** diseñar Retazos partiendo de "qué evento del combate disparan" (categoría de hook) en lugar de partir de "qué efecto se nos ocurre". 6 categorías cubren toda la superficie del combate:

| Categoría | Cuándo se dispara | Ejemplo placeholder |
|---|---|---|
| Apertura de combate | Empieza el combate | "Empezás cada combate con 4 de bloqueo" |
| Inicio de turno | Empieza tu turno | "Robás 1 carta extra al inicio del turno" |
| Modificador de daño | Hacés/recibís daño | "Tu primer ataque del combate hace +5 daño" |
| Acumulador / trigger | Después de N acciones | "Cada 3 cartas Skill jugadas en un turno → +1 energía siguiente turno" |
| Economía / fin de combate | Termina combate o muere enemigo | "+5 oro al final de cada combate" |
| Cambio de mundo | Ejecutás un switch | "Al cambiar de mundo, ganás 5 de bloqueo" |

**Conexión con sistemas:** Retazos (M3 Sub-PR 3B), sistema de hooks/dispatcher (M3 Sub-PR 3A), eventos del combate (M4 eventos, M5 bosses con fases).

**Potencial:** garantiza que el pool de Retazos del lanzamiento cubra toda la superficie del combate sin diseñar 23 efectos redundantes. Las 6 categorías alimentan directamente el spec de hooks: cada categoría → un hook específico. El diseño de Retazos y el diseño de hooks se construyen sobre la misma base. Aplica también a opciones extensibles de Hoguera/Tienda (`OnCampfireOptionsBuilt`, `OnShopStockBuilt`) — el mismo principio: el sistema expone hooks, las features que vienen después se cuelgan ahí.

**Estado:** crudo — pendiente de aplicar al spec formal de hooks (Sub-PR 3A) y al diseño de Retazos placeholder (Sub-PR 3B).

---

## Insight 2 — Ampliar contrato de interfaz requiere grep por implementadores, no por nombre del miembro — 2026-05-07

**Contexto:** durante Sub-PR D de M2 se agregó `Heal(int)` al contrato de `ICombatActor`. Se actualizaron las dos implementaciones de producción (`PlayerCombatActor`, `EnemyCombatActor`) pero quedó sin actualizar el mock privado `TestActor` dentro de `ActionQueueTests.cs`. El error CS0535 recién apareció al abrir Unity post-commit; el commit ya estaba pusheado y el PR #91 abierto. Bloqueó compilación entera (Safe Mode).

**Idea:** cuando se amplía el contrato de una interfaz, el grep que captura todos los puntos a tocar NO es por el nombre del miembro nuevo (`Heal`) — porque los lugares que faltan implementarlo todavía no lo mencionan. El grep correcto es por la declaración de implementación (`: ICombatActor`, `: IInterface\b`). Esto encuentra mocks de test, clases stub, y cualquier implementador que el code review por nombre del miembro nuevo no captura.

**Conexión con sistemas:** proceso de desarrollo / validación. Aplica a cualquier cambio de contrato (interfaces, clases abstractas, virtuals nuevos).

**Potencial:** sumarlo al checklist de `modo:implementacion` cuando se toca una interfaz: antes de commitear, hacer `grep ": NombreInterfaz"` y verificar que todos los implementadores tengan el miembro nuevo. Especialmente importante en proyectos con tests que usan mocks privados — esos no aparecen en búsquedas por nombre del archivo de la interfaz. Es una versión específica del Insight 1 (abrir Unity siempre antes de cerrar): si el grep por implementadores se hubiera hecho, no hubiera hecho falta llegar a Unity para descubrirlo.

**Estado:** aplicado — fix puntual de `TestActor.Heal()` commiteado encima de Sub-PR D.

---

## Insight 1 — Validación de fase de extracción debe abrir Unity, no solo revisar el diff — 2026-05-01

**Contexto:** durante la validación de la Fase 4 (extracción de CombatBackgroundView), al abrir Unity por primera vez post-merge me salió el editor en Safe Mode con 6 errores CS0246/CS0103 en CombatHudView.cs. El error venía latente desde Fase 3: `BuildEnemyIntentLabel()` se movió de CombatUIController a CombatHudView pero el `using RoguelikeCardBattler.Gameplay.Enemies;` no se replicó. Como el code review de Fase 3 fue por inspección de diff y no abriendo Unity, el error pasó. Recién apareció en Fase 4 cuando volví a abrir el editor.

**Idea:** la validación de una fase de refactor que mueve código entre archivos NO es completa hasta que Unity compile sin errores. El code review del diff puede pasar y aún así el proyecto no compilar (typically por usings faltantes, asmdefs mal referenciados, o tipos del namespace destino que no existen en el origen).

**Conexión con sistemas:** proceso de desarrollo / validación. No afecta gameplay directamente, pero afecta cómo cerramos sub-tareas en `_roadmap.md`.

**Potencial:** agregar al protocolo de extracción de combate (y refactors en general) un checkbox explícito: "Abrir Unity y verificar zero compilation errors antes de marcar como validada". Esto es independiente y previo a "validar comportamiento en BattleScene". Para M2 (que va a tocar mucho más código y reescribir tests), este chequeo es no-negociable porque la cantidad de errores potenciales se multiplica.

**Estado:** aplicado — la regla queda registrada acá para que se aplique en M2 y siguientes. El protocolo de futuros specs de refactor debe incluirla en la sección de validación.
