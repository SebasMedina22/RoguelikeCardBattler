# Spec — M4 bloque 4b: Eventos + Quests (DD-005, DD-021)

> **ESTADO: IMPLEMENTADO — 4b-1 PR #125 (2026-06-18) · 4b-2 PR #126 (2026-06-19).**
>
> Spec cerrado el 2026-06-18 en sesión `modo:diseno`. Las 4 decisiones abiertas
> quedaron cerradas por Sebastián (ver §Decisiones cerradas). Dividido en **2
> sub-PRs**: **4b-1** (motor de eventos + eventos simples) y **4b-2** (eventos
> multidimensionales + quest/MCguffin).

## Origen
- GDD §DD-005 (sistema de nodos — tipo Event; eventos multidimensionales que
  dejan elegir mundo) y §DD-021 (representación del MCguffin de quests,
  explícitamente diferida "al diseño de M4 Eventos" → se cierra aquí).
- GOLDEN_RULES §7 (Event: encuentros con decisiones, algunos multidimensionales),
  §8 (eventos otorgan oro variable), §10 (Retazos por evento).
- `_roadmap.md` → M4 bloque 4b.

## Objetivo
Convertir el nodo Event (hoy stub: panel genérico "Contenido placeholder" en
`RunFlowController.ShowResolvePanel`) en un sistema de encuentros con decisiones
y consecuencias data-driven, incluyendo eventos multidimensionales (elegir
mundo) y el quest con MCguffin (objeto = un Retazo que da un pasivo + un nodo
destino a alcanzar en el mapa que otorga recompensa final).

## Comportamiento esperado (perspectiva del jugador)
1. Click en un nodo Event → se abre un **panel de evento** (espejo estructural de
   Hoguera/Tienda: panel runtime sobre RunScene, fondo opaco, fallback de color
   sin arte).
2. **Evento simple:** título + texto narrativo + N botones de decisión. Cada
   decisión aplica consecuencias (dar/quitar carta, ±oro, ±HP, dar Retazo) y
   muestra un texto de resultado → botón "Continuar" cierra el nodo.
3. **Evento multidimensional:** primero una pantalla de **elección de mundo**
   (A medieval / B futurista). El enunciado y las recompensas cambian según el
   mundo elegido; la estructura de decisiones es la misma.
4. **Quest (MCguffin):** una decisión "Aceptar" entrega el objeto (un **Retazo**
   con pasivo según el mundo) y marca un **nodo destino resaltado** en el mapa.
   Completar ese nodo otorga la recompensa final (oro). La decisión alternativa
   ("Robar") da +100 oro inmediato y destruye el objeto (no hay quest).

## Sistemas afectados
- **ScriptableObjects:** nuevo `EventDefinition` + `EventPoolConfig`.
- **Run/Flujo:** `RunFlowController` ramifica `NodeType.Event` a un nuevo
  `EventNodeController` (hoy cae al `ShowResolvePanel` genérico).
- **RunState:** estado del quest activo (`QuestState`) + helpers de mutación.
- **Mapa:** `RunMapView` resalta el nodo destino del quest; `MapNode` porta el
  `EventDefinition` asignado (determinismo por seed); `RunMapGenerator.AssignEvents`.
- **Retazos:** el MCguffin reusa el sistema de Retazos (consecuencia "dar
  Retazo"); 1 efecto nuevo (`OnCardPlayed`) para el pasivo Mundo B.
- **Combate:** **NINGUNO** — cero cambios en archivos protegidos. Los pasivos del
  MCguffin corren por hooks de Retazos ya existentes (`OnCombatEnd`, `OnCardPlayed`).

---

## División en sub-PRs

### Sub-PR 4b-1 — Motor de eventos + eventos simples
Construye toda la infraestructura del nodo Event y los eventos NO
multidimensionales. Entregable jugable: nodos Event abren un panel con
decisiones que dan/quitan cartas, oro, HP y Retazos.

**Archivos a crear:**
- `Assets/Scripts/Run/Events/EventDefinition.cs`
- `Assets/Scripts/Run/Events/EventChoice.cs`
- `Assets/Scripts/Run/Events/EventConsequence.cs`
- `Assets/Scripts/Run/Events/EventResolver.cs`
- `Assets/Scripts/Run/Events/EventNodeController.cs`
- `Assets/Scripts/Run/Events/EventPoolConfig.cs`
- `Assets/Editor/EventConfigSetup.cs`
- `Assets/Tests/EditMode/EventTests.cs`

**Archivos a modificar:**
- `RunFlowController.cs` — `BuildEventController()` en `BuildUI()`;
  `EnterNode` ramifica `NodeType.Event` → `ShowEventPanel`; `OnEventComplete`;
  `_eventController` field; cleanup en transiciones de escena.
- `MapNode.cs` — `EventDefinition AssignedEvent { get; set; }` (paralelo a
  `SpecificEnemy`).
- `RunMapGenerator.cs` — `AssignEvents(map, pool, seed)` (espejo de `AssignEnemies`).
- `RunSession.cs` — pasar/asignar el `EventPoolConfig` (donde hoy resuelve
  enemigos/config; verificar el punto exacto en implementación).

### Sub-PR 4b-2 — Eventos multidimensionales + quest/MCguffin
Agrega la elección de mundo y el sistema de quests sobre el motor de 4b-1.

**Archivos a crear:**
- `Assets/Scripts/Run/Quests/QuestState.cs`
- `Assets/Scripts/Gameplay/Relics/Effects/RelicCardPlayedBlockEffect.cs`
  (pasivo MCguffin Mundo B — efecto `OnCardPlayed`)
- `Assets/Tests/EditMode/QuestTests.cs`

**Archivos a modificar:**
- `EventDefinition.cs` — flag `IsMultidimensional` + variantes `WorldA`/`WorldB`.
- `EventConsequence.cs` — tipo `StartQuest` + payload.
- `EventNodeController.cs` — pantalla de elección de mundo previa; render de la
  variante elegida.
- `EventResolver.cs` — `ResolveVariant(def, world)`.
- `RunState.cs` — `QuestState ActiveQuest` + `StartQuest` / `CompleteQuestIfDestination`.
- `RunFlowController.cs` — al completar cualquier nodo, chequear destino del quest.
- `RunMapView.cs` — resaltar el nodo destino.
- `EventConfigSetup.cs` — sumar el evento quest multidimensional placeholder.
- `Docs/design/DESIGN_DECISIONS.md` — cerrar DD-021 (transcripción, autoridad de
  Sebastián; ya confirmada la representación).

**Dependencia interna dura:** 4b-2 requiere 4b-1 cerrado (mergeado).

---

## Archivos protegidos involucrados
- [x] **Ninguno.** El MCguffin corre sobre hooks de Retazos ya expuestos
  (`OnCombatEnd`, `OnCardPlayed`). El pasivo Mundo B es un `IRelicEffect` nuevo
  sobre `OnCardPlayed` (hook existente) — NO toca TurnManager/ActionQueue/PlayerCombatActor.

## Contratos

### Datos

```csharp
enum ConsequenceType {
    GiveCard, RemoveCard, GiveGold, LoseGold, ModifyHP, GiveRelic, StartQuest
}

// EventConsequence — datos puros (serializable)
ConsequenceType Type;
int Amount;                      // oro / HP delta (signo según Type)
CardDefinition Card;             // GiveCard
RelicDefinition Relic;           // GiveRelic / MCguffin (pasivo)
QuestData Quest;                 // StartQuest

// EventChoice — serializable
string Label;
string ResultText;               // texto mostrado tras elegir
List<EventConsequence> Consequences;
int MinGoldRequired;             // 0 = sin condición; botón gris si no alcanza

// EventVariant — usado solo en multidimensionales (4b-2)
string Body;                     // enunciado del mundo
List<EventChoice> Choices;

// EventDefinition (SO)
string Id, Title, Body;
bool IsMultidimensional;         // 4b-2
EventVariant WorldA, WorldB;     // 4b-2; usados si IsMultidimensional
List<EventChoice> Choices;       // usados si NO es multidimensional
// (tags/pesos de filtrado si EventPoolConfig los necesita)

// QuestData — payload de StartQuest (en EventConsequence)
RelicDefinition CarriedRelic;    // el "objeto"/pasivo entregado al aceptar
int FinalRewardGold;             // recompensa al llegar al destino (~75)
// (destino se RESUELVE en runtime al aceptar, no se autora; ver Nota de destino)

// QuestState — runtime en RunState (4b-2)
bool Active;
int DestinationNodeId;           // nodo resaltado a alcanzar
int FinalRewardGold;
string SourceWorldLabel;         // "A"/"B" para flavor del resaltado
```

### APIs públicas
- `EventConsequence.Apply(RunState state, RelicHookDispatcher dispatcher, EventConsequence c)`
  — **static, sin UI.** Muta RunState. Reusa `state.AddCardToDeck`,
  `state.RemoveCardFromDeck`, `state.AddRelic`, `state.StartQuest`. Clamp: oro ≥0,
  HP en `[0, PlayerMaxHP]`.
- `EventResolver.SelectEvent(EventPoolConfig pool, int nodeId, int seed)` →
  `EventDefinition` determinista (mismo seed/nodo = mismo evento).
- `EventResolver.ResolveVariant(EventDefinition def, int world)` → `List<EventChoice>`
  (4b-2; `world` 0=A / 1=B).
- `RunState.StartQuest(QuestState quest)` — activa el quest.
- `RunState.CompleteQuestIfDestination(int nodeId)` → `bool` — si `nodeId` es el
  destino activo, otorga `FinalRewardGold`, desactiva el quest y devuelve true.
- `EventNodeController.Initialize(Canvas, RunState, RelicHookDispatcher, EventPoolConfig, Action<int> onComplete)`
  + `Show(int nodeId)` — firma espejo de `CampfireNodeController`.
  - **Ajuste de firma (implementado en 4b-1, autoritativo para 4b-2):** la firma real
    es `Show(int nodeId, EventDefinition definition)`. El evento autoritativo vive en
    `MapNode.AssignedEvent` (fijado por seed al generar el mapa) y el controller no
    tiene la seed ni referencia al mapa; re-resolverlo internamente arriesgaría divergir
    del evento ya fijado. Por eso `RunFlowController` le pasa la definición. La analogía
    con `CampfireNodeController` no transfiere en este punto porque la Hoguera no tiene
    contenido por-nodo que pasar. 4b-2 hereda `Show(int nodeId, EventDefinition)` (la
    pantalla de elección de mundo opera sobre esa misma definición).

### Eventos / Hooks
- **Sin hooks nuevos en TurnManager.** El MCguffin Mundo A reusa
  `RelicEndGoldEffect` (Amount=2). El Mundo B usa el nuevo
  `RelicCardPlayedBlockEffect` sobre `OnCardPlayed` (suma bloqueo al jugador
  cuando la carta jugada es de tipo Block — vía `RelicHookContext.GrantBlock`).
- **Sin hook de nodo de evento** (a diferencia de Hoguera/Tienda): en esta
  versión los Retazos no mutan los eventos. Diferido (Insight 4) hasta que un
  Retazo concreto lo justifique.

### Nota de destino del quest (alcanzabilidad)
El nodo destino se resuelve en runtime al aceptar (no se autora en el SO). Debe
ser **alcanzable** por BFS forward desde el nodo del quest en el DAG.

> ⚠️ **La regla `[PROPUESTA]` original ("+2/+3 con fallback a convergencia
> pre-boss") quedó SUPERSEDED.** Validada contra el código real (2026-06-18) tiene
> tres huecos: (1) para Events en penúltima capa no existe nodo a +2/+3; (2) la
> "convergencia pre-boss única" no existe en una de las 3 topologías (el Boss tiene
> 2 predecesores); (3) donde el predecesor del Boss es único, coincide con el propio
> nodo del Event → destino degenerado. La regla corregida y blindada está en
> **§Resolución del nodo destino del quest (regla corregida)** dentro de
> §Contenido de Sub-PR 4b-2.

## Reuso
- Patrón node-controller: `CampfireNodeController` (panel runtime, `BuildPanel`/
  `Show`/`CompleteX`/`CreateButtonRaw`, fallback de color sin arte, `ClearSpawnedButtons`).
- Helpers de label de carta: `CardDisplay` (`public static`) para mostrar cartas
  que se dan/quitan.
- Mazo/economía: `RunState.AddCardToDeck/RemoveCardFromDeck/AddRelic`, `state.Gold`,
  `PlayerCurrentHP/MaxHP`.
- MCguffin pasivo Mundo A: `RelicEndGoldEffect` (ya existe).
- `RelicHookContext.GrantBlock` para el efecto Mundo B (ya existe en la API limitada).
- Editor setup idempotente: `ShopConfigSetup` / `NewRunConfigSetup`.
- Determinismo por seed: `RunMapGenerator.AssignEnemies` como plantilla de `AssignEvents`.

## Casos de prueba (EditMode)

### 4b-1 (`EventTests.cs`)
1. `Apply(GiveGold)` suma oro; `Apply(LoseGold)` resta con clamp ≥0.
2. `Apply(ModifyHP)` positivo respeta cap `PlayerMaxHP`; negativo no baja de 0.
3. `Apply(GiveCard)` clona la carta al mazo; `Apply(RemoveCard)` quita por referencia.
4. `Apply(GiveRelic)` agrega Retazo con `AcquisitionOrder` correcto (orden de lista).
5. `EventResolver.SelectEvent` determinista por seed (misma seed = mismo evento por nodo;
   distinta seed diverge).
6. Choice con `MinGoldRequired` > oro actual → no disponible.
7. `AssignEvents` asigna `EventDefinition` solo a nodos `NodeType.Event`, determinista.

### 4b-2 (`QuestTests.cs`)
8. `ResolveVariant` devuelve choices de A vs B distintas en un evento multidimensional.
9. Quest aceptar → `ActiveQuest.Active` + `DestinationNodeId` seteado + Retazo entregado;
   destino alcanzable desde el nodo del quest.
10. `CompleteQuestIfDestination(destino)` → otorga `FinalRewardGold`, `Active=false`, true.
11. `CompleteQuestIfDestination(otroNodo)` → false, no otorga, quest sigue activo.
12. Quest "Robar" → +100 oro, sin quest activo, Retazo NO entregado.
13. `RunState.Reset()` limpia `ActiveQuest`.
14. `RelicCardPlayedBlockEffect` sobre `OnCardPlayed` con carta Block → suma bloqueo;
    con carta no-Block → no-op.

## Validación manual (RunScene)
1. Correr `Roguelike > Setup Event Config` → `EventPoolConfig.asset` + EventDefinitions creados.
2. **(4b-1)** Entrar a un nodo Event simple → decisiones aplican (verificar oro/HP/mazo
   en el visor de mazo y el HUD).
3. **(4b-2)** Entrar a un evento multidimensional → elegir A vs B muestra enunciados y
   recompensas distintos.
4. **(4b-2)** Aceptar el quest → nodo destino resaltado en el mapa; el pasivo aplica en
   combate (oro extra A / bloqueo extra B); llegar al destino otorga la recompensa final.
   Robar → +100 oro y sin quest.
5. Zero console errors; suite EditMode completa en verde.

## Decisiones cerradas (no se discuten)
- **División:** 2 sub-PRs (4b-1 motor + simples; 4b-2 multidim + quest). *(Q1)*
- **DD-021 / MCguffin:** el "objeto" ES un **Retazo** (RelicDefinition) entregado al
  Aceptar → reusa el sistema de Retazos completo (pasivo, inventario, hooks). El
  "llevarlo a destino" es tracking en RunState (`QuestState` con `DestinationNodeId`).
  Cero infra de items nueva. *(Q2 — transcribir a DESIGN_DECISIONS.md en 4b-2.)*
- **Recompensa de quest:** llegar al destino otorga oro (~75, afinable en balance). Si
  no se llega (muerte / ruta alterna), el pasivo ya se disfrutó durante el trayecto y
  NO hay recompensa final (riesgo/decisión real). *(Q3)*
- **Pasivo Mundo B:** se autora `RelicCardPlayedBlockEffect` (nuevo `IRelicEffect` sobre
  `OnCardPlayed`, hook existente, NO toca protegidos): +1 bloqueo al jugar una carta de
  escudo. *(Q4)*
- Event node = panel runtime sobre RunScene (no escena nueva), espejo Hoguera/Tienda.
- Consecuencias data-driven por enum (no subclases por consecuencia).
- El "mundo" del evento es elección **local** del encuentro (flavor + recompensa); NO
  existe ni se introduce estado de mundo persistente fuera de combate.
- Determinismo por seed: los eventos por nodo se fijan al generar el mapa.

## Alternativas consideradas
- **Mundo persistente en el run** (que el evento fije el mundo del próximo combate) —
  descartado: el mundo es mecánica de combate (`TurnManager.CurrentWorld`); el GDD dice
  que el evento solo cambia "el enunciado y la recompensa". Expandiría scope a combate
  (protegido).
- **MCguffin como carta-maldición en el mazo** — descartado: el GDD lo describe como
  objeto pasivo ("te da X"), que es exactamente la semántica de Retazo.
- **Ítem de quest dedicado (clase QuestItem)** — descartado (Q2): duplica la infra que ya
  da el sistema de Retazos.
- **Hook `OnEventResolved` para Retazos** — diferido (Insight 4: ningún Retazo lo justifica aún).
- **Destino del quest autorado en el SO** — descartado: el destino depende del mapa
  generado por seed; debe resolverse en runtime garantizando alcanzabilidad.

## Estimación
- Complejidad: **alta** (lo grande de M4). 4b-1 media · 4b-2 alta (tracking + marcado en
  mapa + recompensa diferida + alcanzabilidad en DAG).
- Riesgo principal: selección del nodo destino debe garantizar alcanzabilidad. Resuelto
  con BFS forward + fallback al Boss (único nodo siempre alcanzable). Blindado con test
  exhaustivo por topología. Ver §Contenido de Sub-PR 4b-2.

---

# Contenido de Sub-PR 4b-2 (cerrado en sesión `modo:diseno` 2026-06-18)

> Esta parte fija el **contenido placeholder** que `EventConfigSetup` autora en 4b-2 y
> **corrige la regla de resolución del nodo destino** del quest (la nota `[PROPUESTA]`
> previa tenía huecos). Todo lo de aquí está verificado contra el código real de 4b-1
> y listo para implementación: no quedan decisiones de diseño abiertas. Los números de
> oro/HP son placeholders **afinables en balance** (marcados ⚖️).

## Realidad del mapa (verificada en código 2026-06-18) `[CÓDIGO ACTUAL]`

- El DAG **no** es procedural: `RunMapGenerator.Topologies` (`int[][][]`) son **3
  plantillas fijas** de aristas, elegidas por `rng.Next(3)`. `Act1MapConfig.TotalNodes = 8`
  (ids 0..7). Nodo **0 = start** (Combat forzado, sin entrantes), nodo **7 = end = Boss**
  (forzado, sin salientes). `MapNode.Connections` = lista de **sucesores** (adyacencia
  forward); `MapNode.Id` es el índice; `ActMap.GetNode(id)` / `ActMap.Nodes`.
- Un `NodeType.Event` puede caer en **cualquier** índice intermedio **1..6** (peso 20, sin
  mínimo ni restricción por profundidad). El Boss queda excluido del pool de pesos.
- Las 3 topologías y profundidades forward desde el start:

  | Topo | Aristas | Profundidades (d) |
  |------|---------|-------------------|
  | **A** doble diamante | 0→(1,2)→3→(4,5)→6→7 | d1{1,2} d2{3} d3{4,5} d4{6} d5{7} |
  | **B** split tardío | 0→1→(2,3)→4→(5,6)→7 | d1{1} d2{2,3} d3{4} d4{5,6} d5{7} |
  | **C** triple temprano | 1→4, 2→4, 3→5, 4→6, 5→6, 6→7 (0→1,2,3) | d1{1,2,3} d2{4,5} d3{6} d4{7} |

- **Invariante clave:** desde cualquier Event (1..6), el Boss/end (nodo 7) **siempre** es
  alcanzable forward — todo camino converge al end. No existe ningún otro nodo con esa
  garantía universal (la "convergencia pre-boss única" **no existe**: en B el Boss tiene
  dos predecesores, 5 y 6).

## Eventos multidimensionales placeholder

Se autoran **3 eventos multidimensionales** (`IsMultidimensional = true`): **2 simples**
(no-quest, para ejercitar la pantalla de elección de mundo con consecuencias normales) +
**1 quest/MCguffin**. Estructura por GDD DD-005: *"la quest es exactamente la misma, solo
cambia el enunciado y la recompensa"* → cada variante A/B comparte el **número y los tipos
de consecuencia** de sus choices; difieren el flavor (Body/Label/ResultText) y los valores
de recompensa. `Title` es único por `EventDefinition`; el `Body` y los `Choices` viven en
`WorldA` / `WorldB` (`EventVariant`).

> Modelo de datos que 4b-2 agrega (ya previsto en §Contratos): `EventVariant { string Body;
> List<EventChoice> Choices; }`, `EventDefinition.IsMultidimensional` + `WorldA` + `WorldB`,
> y `ConsequenceType.StartQuest` **al final del enum** (para no renumerar valores ya
> serializados en 4b-1). `EventResolver.ResolveVariant(def, world)` devuelve `WorldA.Choices`
> (world 0=A) o `WorldB.Choices` (world 1=B).

### Evento simple 1 — `evt_md_forge` ("El artesano y su yunque")

Tema: pagar por una carta o curarse. Cubre `LoseGold` + `GiveCard` + `ModifyHP`.

**Variante A (medieval)** — Body: *"Un herrero ermitaño aviva las brasas de su fragua.
'Dame algo de oro y tu acero saldrá más afilado', murmura sin mirarte."*

| Label | minGold | Consecuencias | ResultText |
|-------|---------|---------------|-----------|
| Pagar al herrero | 25 | `LoseGold 25` + `GiveCard(afín)` | "El herrero te entrega una hoja recién templada." |
| Curar tus heridas | 0 | `ModifyHP +10` ⚖️ | "El calor de la fragua reconforta tu cuerpo (+10 HP)." |
| Dejar la fragua | 0 | — | "Te alejas del calor de las brasas." |

**Variante B (futurista)** — Body: *"Un técnico de chatarra calibra su soldadora de
plasma. 'Unos créditos y recalibro tu equipo', zumba su vocoder."*

| Label | minGold | Consecuencias | ResultText |
|-------|---------|---------------|-----------|
| Pagar al técnico | 25 | `LoseGold 25` + `GiveCard(afín)` | "El técnico imprime un módulo y lo acopla a tu equipo." |
| Pedir un parche | 0 | `ModifyHP +14` ⚖️ | "El nanogel sella tus heridas (+14 HP)." |
| Salir del taller | 0 | — | "Las compuertas se cierran tras de ti." |

*(La recompensa cambia por mundo: B cura +14 vs A +10. La carta dada se resuelve por
`FindAnyCard()` placeholder y se rutea por `AffinityResolver` igual que cualquier
recompensa — la elección de mundo del evento es flavor local, no altera el ruteo.)*

### Evento simple 2 — `evt_md_relic` ("El relicario sellado")

Tema: arriesgar HP por un Retazo, o saquear oro. Cubre `GiveRelic` + `ModifyHP` + `GiveGold`.

**Variante A (medieval)** — Body: *"Un relicario de hierro descansa sobre un altar
agrietado. Un susurro promete poder a quien se atreva a abrirlo."*

| Label | minGold | Consecuencias | ResultText |
|-------|---------|---------------|-----------|
| Romper el sello | 0 | `GiveRelic(afín)` + `ModifyHP -8` ⚖️ | "Arrancas la reliquia; las púas del relicario te hieren (-8 HP)." |
| Llevarte las ofrendas | 0 | `GiveGold 30` ⚖️ | "Recoges 30 de oro de entre las ofrendas." |
| No arriesgarte | 0 | — | "Retrocedes sin tocar el altar." |

**Variante B (futurista)** — Body: *"Una cápsula de contención parpadea en rojo. Una voz
sintética ofrece su carga a quien acepte el riesgo."*

| Label | minGold | Consecuencias | ResultText |
|-------|---------|---------------|-----------|
| Forzar la cápsula | 0 | `GiveRelic(afín)` + `ModifyHP -6` ⚖️ | "Extraes el núcleo; una descarga te recorre (-6 HP)." |
| Vaciar la reserva | 0 | `GiveGold 35` ⚖️ | "Transfieres 35 créditos de la reserva." |
| No arriesgarte | 0 | — | "Te apartas de la cápsula parpadeante." |

*(El Retazo dado aquí es un Retazo de pool normal vía `FindAnyRelic()` — **no** un MCguffin.
Recompensa cambia por mundo: B menos daño (-6) y más oro (35).)*

### Evento quest/MCguffin — `evt_md_quest_courier` ("Un encargo en el camino")

Texto **canónico del GDD** (DD-005) `[GDD]`. **Solo 2 choices por variante (Aceptar /
Robar)** — el GDD no contempla una opción de salida, y es intencional (el quest es un
compromiso). Usa el nuevo `ConsequenceType.StartQuest`.

**Variante A (medieval)** — Body: *"Un hombre moribundo, con las ropas raídas de la legión
de magos, te aferra el brazo. Te ruega llevar un objeto místico hasta un punto lejano del
mapa; si lo logras, promete recompensarte."*

| Label | Consecuencias | ResultText |
|-------|---------------|-----------|
| Aceptar el encargo | `StartQuest{ CarriedRelic = R-MCG-A, FinalRewardGold = 75 ⚖️ }` | "El mago te confía un cáliz lacrado. Un punto del mapa queda resaltado." |
| Robar al moribundo | `GiveGold 100` `[GDD]` | "Le arrebatas la bolsa y huyes. El objeto se hace añicos. +100 de oro." |

**Variante B (futurista)** — Body: *"Un robot con las piezas rotas chisporrotea en el
suelo. Dice pertenecer a una facción revolucionaria y te pide llevar un disco duro con
información vital hasta un punto lejano del mapa."*

| Label | Consecuencias | ResultText |
|-------|---------------|-----------|
| Aceptar el encargo | `StartQuest{ CarriedRelic = R-MCG-B, FinalRewardGold = 75 ⚖️ }` | "El robot te transfiere un disco sellado. Un punto del mapa queda resaltado." |
| Robar al robot | `GiveGold 100` `[GDD]` | "Le arrancas el disco y sus créditos. El disco se desintegra. +100 de oro." |

> **Tensión de diseño (intencional, cerrada por Q3):** "Robar" da **+100 oro inmediato**;
> "Aceptar" da el **pasivo durante el trayecto** + **75 oro al llegar** (si llegás). Robar
> rinde más oro neto y sin riesgo; Aceptar apuesta por el pasivo + economía a futuro. Si no
> se llega al destino (muerte o ruta alterna), el pasivo ya se disfrutó y no hay recompensa
> final. Riesgo/decisión real. `FinalRewardGold = 75` está entre Elite (30-50) y Boss (100).

## Los dos Retazos MCguffin (stats)

Se autoran como `RelicDefinition` `.asset` en una **subcarpeta dedicada**
`Assets/ScriptableObjects/Relics/Quest/` para dejar explícito que **NO entran al pool de
drops** (no se agregan a `RelicSoGenerator.BuildSpecs` ni al reward pool de
`RunCombatConfig`; solo los referencia el quest vía `CarriedRelic`). Los crea
`EventConfigSetup` (helper `CreateOrUpdateMcguffinRelic`, espejo del patrón `RelicSpec` de
`RelicSoGenerator`) para mantener el contenido del quest autocontenido.

| Campo | R-MCG-A (Mundo A) | R-MCG-B (Mundo B) |
|-------|-------------------|-------------------|
| Asset | `R-MCG-A_CalizDelMensajero.asset` | `R-MCG-B_DiscoDeLaResistencia.asset` |
| DisplayName | "Cáliz del mensajero" | "Disco de la resistencia" |
| Description | "+2 oro al ganar cada combate." | "+1 escudo al jugar una carta de escudo." |
| FlavorText | "Lacrado con cera de otra época. Pesa como una promesa." | "Late con datos que alguien murió por proteger." |
| Category | `RelicCategory.World` | `RelicCategory.World` |
| Hooks | `{ OnCombatEnd }` (byte `05000000`) | `{ OnCardPlayed }` (byte `06000000`) |
| Effect | `RelicEndGoldEffect { Amount = 2 }` (reusa el existente; default es 5, setear 2) | `RelicCardPlayedBlockEffect { Amount = 1 }` (**clase nueva**, ver abajo) |

### `RelicCardPlayedBlockEffect` (clase nueva — Mundo B) `[PROPUESTA]`

Archivo nuevo `Assets/Scripts/Gameplay/Relics/Effects/RelicCardPlayedBlockEffect.cs`
(molde: `RelicAccSkillStackerEffect` para el cast de `CardPlayedHookData` + `RelicTurnBlockEffect`
para el `GrantBlock`). Cero archivos protegidos: corre sobre el hook `OnCardPlayed` ya
existente vía `RelicHookContext.GrantBlock` (ya existe en la API limitada).

```
[Serializable] class RelicCardPlayedBlockEffect : IRelicEffect
  public int Amount = 1;
  OnHook(hook, ctx):
    if (hook != RelicHook.OnCardPlayed || ctx.TurnManager == null) return;
    var cp = ctx as CardPlayedHookData;
    if (cp?.Card == null) return;
    bool isBlockCard = cp.Card.Effects.Any(e => e.effectType == EffectType.Block);
    if (!isBlockCard) return;
    ctx.GrantBlock(ctx.TurnManager.Player, Amount);
```

- **Detección de "carta de escudo":** NO existe `CardType.Block`. La convención del codebase
  (`StarterDeckSetup.cs`, `CardUpgradeSetup.cs`) es inspeccionar `Card.Effects` y buscar un
  `EffectRef` con `effectType == EffectType.Block`. El efecto usa esa misma convención.
- **Pasivo incondicional respecto al mundo:** el pasivo aplica al jugar cualquier carta de
  escudo en **cualquier** combate/mundo. **No** se filtra por `CardPlayedHookData.PlayedInWorld`
  — el GDD describe el efecto sin condición de mundo, y el "mundo" del evento es elección
  local del encuentro, no estado persistente (decisión cerrada). El campo `PlayedInWorld`
  existe pero no se usa aquí.

## Resolución del nodo destino del quest (regla corregida) `[PROPUESTA]`

Reemplaza la nota `[PROPUESTA]` original (superseded; ver §Nota de destino). El destino se
resuelve **al aceptar**, no se autora. Garantía exigida: el `DestinationNodeId` debe ser
**alcanzable por BFS forward** desde el nodo del quest, distinto del propio nodo y no
ya completado.

**Algoritmo** (static puro, testeable sin UI):

1. **BFS forward** desde el nodo del quest (`questNodeId`, = `RunState.CurrentPositionNodeId`
   al aceptar) usando `MapNode.Connections` como adyacencia (mismo patrón que
   `RunMapGenerator.ComputeNodeDepths`, pero arrancando `depth = 0` en `questNodeId`).
   Produce `Dictionary<int,int> forwardDepth` con todos los nodos alcanzables y su distancia.
2. **Candidatos** = nodos alcanzables EXCLUYENDO: `questNodeId`, los de `RunState.CompletedNodes`,
   y el **Boss** (`node.Type == NodeType.Boss`).
3. **Selección preferida:** de los candidatos, elegir el de **mayor `forwardDepth`** (el
   destino más lejano = más "trayecto" = mejor para un MCguffin que se "lleva por el mapa");
   desempate por **menor `Id`**. Determinista sin RNG (el mapa ya es determinista por seed).
4. **Fallback** (candidatos vacíos — p. ej. Event en penúltimo nodo, donde el único forward
   es el Boss): usar el **Boss/end** como destino (siempre alcanzable por construcción; es el
   único nodo con garantía universal). El "punto resaltado" coincide entonces con el combate
   final — caso raro y aceptable.

> Por qué "más lejano" y no "+2/+3": en topologías A y C el más lejano no-Boss suele ser el
> nodo de convergencia pre-boss (todos los caminos pasan por él → solo se falla por muerte);
> en B a veces cae en un nodo de rama (→ se puede fallar tomando la otra ruta). Esa mezcla
> de "forzado salvo muerte" vs "evitable por ruta" es exactamente el espacio de riesgo que
> pide Q3, y un destino lejano maximiza el trayecto del MCguffin.

**Ubicación (no en RunState):** la BFS es lógica de flujo → va en un helper static nuevo
`Assets/Scripts/Run/Quests/QuestDestinationResolver.cs`:

```
static int SelectDestination(ActMap map, int questNodeId, ISet<int> completedNodes)
```

Golden Rule §7: `RunState = solo datos`. `RunState.StartQuest(QuestState)` recibe el
`DestinationNodeId` **ya resuelto**; no calcula la BFS.

### Wiring de `StartQuest` (flujo)

La consecuencia `StartQuest` necesita el mapa (para resolver el destino), que la firma static
`EventConsequence.Apply(state, dispatcher, c)` **no** tiene. Por eso:

- `EventConsequence.Apply` mantiene su firma y maneja los **6 tipos simples**. **No** maneja
  `StartQuest` (resolver un grafo no es mutación pura de datos). *(Corrige el hint de la línea
  ~158 que sugería que `Apply` reusa `state.StartQuest`: la resolución del destino vive en el
  flujo, no en `Apply`.)*
- El **controller de flujo** (`EventNodeController` / `RunFlowController`, que tienen el
  `ActMap`) detecta `Type == StartQuest` en la choice elegida y ejecuta:
  1. `int dest = QuestDestinationResolver.SelectDestination(map, currentNodeId, state.CompletedNodes);`
  2. `state.AddRelic(c.Quest.CarriedRelic);`
  3. `state.StartQuest(new QuestState { Active = true, DestinationNodeId = dest, FinalRewardGold = c.Quest.FinalRewardGold, SourceWorldLabel = chosenWorld });`  *(SourceWorldLabel = "A"/"B" según la variante elegida)*
- `RunMapView` resalta `state.ActiveQuest.DestinationNodeId` (persistente entre redibujos
  hasta completar el quest).
- En `RunFlowController.CompleteNode` (donde hoy se avanza el DAG): tras completar cualquier
  nodo, `state.CompleteQuestIfDestination(node.Id)` → si coincide con el destino activo,
  otorga `FinalRewardGold`, `Active = false`, devuelve true.

### `[ALTERNATIVA]` descartada
- **Restringir el placement del quest event** (que nunca caiga en penúltima capa, para
  garantizar un destino no-Boss): requiere lógica especial en `RunMapGenerator` (que no sabe
  cuál Event es el quest) o un swap post-generación. Más invasivo y toca generación de mapa.
  El fallback al Boss lo resuelve sin tocar el generador.
- **Destino autorado en el SO:** descartado ya en el spec base (el destino depende del mapa
  por seed).

## Casos de prueba adicionales (4b-2, sobre `QuestTests.cs`)

Refinan/extienden los casos 8-14 del spec base:

- **Caso 9 (expandido):** `QuestDestinationResolver.SelectDestination` es **alcanzable** y
  válido para **toda topología × toda posición de Event (1..6)**. Iterar las 3 topologías
  (forzar cada plantilla por seed, o exponer `Topologies` para test) y cada nodo intermedio
  como quest: assert que el destino resuelto está en el BFS forward del nodo del quest, es
  `!= questNodeId`, y `∉ CompletedNodes`.
- **Caso 15 (fallback al Boss):** Event en penúltima capa (A@6 / B@5 / B@6 / C@6) → el destino
  resuelto es el Boss (`Type == NodeType.Boss`), y es alcanzable.
- **Caso 16 (determinismo):** misma `(map, questNodeId)` → mismo `DestinationNodeId` en
  llamadas repetidas (sin RNG).

## Checklist de autoría para `EventConfigSetup` (4b-2)

Lo que el menú `Roguelike > Setup Event Config` debe crear/actualizar (idempotente, patrón
`CreateOrUpdateEvent` existente):

1. **2 Retazos MCguffin** en `Assets/ScriptableObjects/Relics/Quest/` vía
   `CreateOrUpdateMcguffinRelic` (R-MCG-A con `RelicEndGoldEffect{Amount=2}`; R-MCG-B con
   `RelicCardPlayedBlockEffect{Amount=1}`). **No** agregarlos a ningún drop pool.
2. **3 EventDefinitions multidimensionales** (`IsMultidimensional = true`, con `WorldA`/`WorldB`):
   `evt_md_forge`, `evt_md_relic`, `evt_md_quest_courier` (este último referencia los 2
   MCguffin en sus `StartQuest`).
3. Agregar los 3 al pool (`EventPoolConfig`) junto a los 3 simples de 4b-1 → 6 eventos.
4. Backgrounds opcionales `Assets/Art/Events/fondo_<id>.png` (cadena de fallback def→pool→color
   ya existe; sin arte = color).

```
modo:implementacion

Implementá el Sub-PR 4b-1 (motor de eventos + eventos simples) de M4 bloque 4b.
El spec cerrado está en `Docs/dev/specs/m4_4b_events_quests_spec.md` — leelo
completo antes de tocar código; todas las decisiones de diseño ya están cerradas,
no abras ninguna. Implementás SOLO el alcance de 4b-1 (eventos NO
multidimensionales y sin quests; eso es 4b-2).

Setup de branch (4b-1 no depende de PRs abiertos; verificá el estado de main):
  git fetch --all --prune
  git checkout -b feat/m4-4b1-events-engine origin/main

Qué construir (resumen; los contratos están en el spec §Sub-PR 4b-1):
- Crear: Assets/Scripts/Run/Events/{EventDefinition, EventChoice, EventConsequence,
  EventResolver, EventNodeController, EventPoolConfig}.cs
- Crear: Assets/Editor/EventConfigSetup.cs (menú `Roguelike > Setup Event Config`,
  idempotente — patrón ShopConfigSetup; crea el pool + EventDefinitions simples placeholder)
- Crear: Assets/Tests/EditMode/EventTests.cs (casos 1-7 del spec)
- Modificar: RunFlowController.cs (BuildEventController + EnterNode ramifica
  NodeType.Event → ShowEventPanel + OnEventComplete + cleanup), MapNode.cs
  (AssignedEvent), RunMapGenerator.cs (AssignEvents), RunSession.cs (cablear EventPoolConfig)

Reglas no negociables:
- NO tocar archivos protegidos (TurnManager, ActionQueue, PlayerCombatActor). 4b-1 NO los toca.
- No manual editor setup: todo se auto-crea en runtime / por el menú editor.
- Eventos multidimensionales y quest/MCguffin quedan FUERA de 4b-1 (son 4b-2).
- EventConsequence.Apply y EventResolver.SelectEvent son static puros y testeables sin UI
  (espejo de CampfireNodeController.ApplyRest / ShopNodeController.BuildStock).
- EventNodeController es panel runtime sobre RunScene, espejo de CampfireNodeController
  (fallback de color sin arte).
- Reusar CardDisplay (helpers de label), RunState.AddCardToDeck/RemoveCardFromDeck/AddRelic.

Validación obligatoria antes de cerrar:
- Compilación limpia (zero console errors).
- EventTests (casos 1-7) en verde + suite EditMode completa sin regresiones (hoy 193/193).
- Correr el menú `Roguelike > Setup Event Config` en Unity (crea los assets).
- Flujo end-to-end en RunScene: entrar a un nodo Event → decisión da/quita carta, ±oro,
  ±HP, Retazo → verificar en el visor de mazo y HUD.
- Validación de datos en Unity-MCP donde aplique.

Al cerrar: actualizá `_roadmap.md` (checkbox de 4b-1) y `_tech_snapshot.md` (nuevo
subsistema Events), commit + push + PR a main.
```

## Prompt de handoff para `modo:implementacion` — Sub-PR 4b-2

```
modo:implementacion

Implementá el Sub-PR 4b-2 (eventos multidimensionales + quest/MCguffin) de M4 bloque 4b.
El spec cerrado está en `Docs/dev/specs/m4_4b_events_quests_spec.md` — leelo COMPLETO antes
de tocar código, en especial la §"Contenido de Sub-PR 4b-2" (eventos, Retazos MCguffin,
regla de destino corregida, wiring de StartQuest, tests). Todas las decisiones de diseño
están cerradas; no abras ninguna. El contenido (textos, choices, stats) ya está autorado en
el spec listo para EventConfigSetup.

Setup de branch (4b-2 DEPENDE de 4b-1 mergeado — confirmá primero que PR #125 está en main):
  git fetch --all --prune
  git checkout -b feat/m4-4b2-events-quests origin/main

Qué construir (contratos completos en el spec):
- Crear: Assets/Scripts/Run/Quests/{QuestState, QuestDestinationResolver}.cs
- Crear: Assets/Scripts/Gameplay/Relics/Effects/RelicCardPlayedBlockEffect.cs
  (IRelicEffect sobre OnCardPlayed; detecta carta de escudo por Card.Effects con
  EffectType.Block; ctx.GrantBlock(ctx.TurnManager.Player, Amount); incondicional al mundo)
- Crear: Assets/Tests/EditMode/QuestTests.cs (casos 8-16 del spec)
- Modificar: EventDefinition.cs (IsMultidimensional + WorldA/WorldB:EventVariant),
  EventConsequence.cs (ConsequenceType.StartQuest AL FINAL del enum + payload QuestData),
  EventResolver.cs (ResolveVariant(def, world)), EventNodeController.cs (pantalla de elección
  de mundo previa, sobre Show(int, EventDefinition)), RunState.cs (QuestState ActiveQuest +
  StartQuest + CompleteQuestIfDestination + Reset limpia ActiveQuest),
  RunFlowController.cs (resolver destino vía QuestDestinationResolver al elegir StartQuest;
  CompleteQuestIfDestination en CompleteNode), RunMapView.cs (resaltar DestinationNodeId),
  EventConfigSetup.cs (2 Retazos MCguffin + 3 eventos multidim; ver Checklist de autoría)
- Transcribir: Docs/design/DESIGN_DECISIONS.md → cerrar DD-021 (autoridad de Sebastián;
  representación ya confirmada: MCguffin = Retazo + QuestState. Pedir OK antes de escribir.)

Reglas no negociables:
- NO tocar archivos protegidos (TurnManager, ActionQueue, PlayerCombatActor). 4b-2 NO los
  toca: el pasivo Mundo B corre por el hook OnCardPlayed ya existente vía GrantBlock.
- No manual editor setup: todo se auto-crea por el menú Roguelike > Setup Event Config.
- StartQuest NO se maneja en EventConsequence.Apply (necesita el mapa) — la resolución del
  destino vive en el flujo (controller) vía QuestDestinationResolver static. RunState NO
  calcula la BFS (Golden Rule §7: RunState = solo datos).
- Los 2 Retazos MCguffin NO entran a ningún drop pool (subcarpeta Relics/Quest/).
- El evento quest tiene SOLO 2 choices (Aceptar/Robar), por GDD.

Validación obligatoria antes de cerrar:
- Compilación limpia (zero console errors).
- QuestTests (casos 8-16) en verde + suite EditMode completa sin regresiones (hoy 212/212).
- Correr el menú Roguelike > Setup Event Config (crea MCguffin + eventos multidim).
- Flujo E2E en RunScene: evento multidim → elegir A vs B muestra enunciados/recompensas
  distintos; Aceptar el quest → nodo destino resaltado + pasivo aplica en combate (oro extra
  A / bloqueo extra B); llegar al destino otorga FinalRewardGold; Robar → +100 oro sin quest.
- Validación de datos en Unity-MCP (asset de MCguffin: Hooks/Effect/Amount; serialización
  de StartQuest en el evento quest).

Al cerrar: actualizá `_roadmap.md` (checkbox de 4b-2) y `_tech_snapshot.md` (subsistema
Quests + eventos multidim), commit + push + PR a main. Con esto cierra M4 bloque 4b completo.
```
