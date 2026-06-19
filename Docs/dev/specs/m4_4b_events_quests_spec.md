# Spec — M4 bloque 4b: Eventos + Quests (DD-005, DD-021)

> **ESTADO: 4b-1 IMPLEMENTADO — PR #125 (2026-06-18). 4b-2 PROPUESTA.**
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
ser **alcanzable** desde el nodo del quest en el DAG. Regla `[PROPUESTA]`:
BFS forward desde el nodo del quest y elegir un nodo a profundidad +2/+3 sobre un
camino alcanzable; fallback robusto = el nodo de convergencia pre-boss (siempre
alcanzable porque todo converge al end). Validar con `RunMapGeneratorTests`-style
que el destino elegido es alcanzable.

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
- Riesgo principal: selección del nodo destino debe garantizar alcanzabilidad (BFS
  forward o convergencia pre-boss). Blindar con test.

---

## Prompt de handoff para `modo:implementacion` — Sub-PR 4b-1

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

## Prompt de handoff — Sub-PR 4b-2

> Se genera/afina al cerrar 4b-1 (depende de su motor mergeado). Resumen de scope
> para esa sesión: agregar `IsMultidimensional` + variantes A/B a `EventDefinition`,
> pantalla de elección de mundo en `EventNodeController`, `QuestState` en RunState
> (`StartQuest`/`CompleteQuestIfDestination`), resolución del nodo destino con
> alcanzabilidad + resaltado en `RunMapView`, `RelicCardPlayedBlockEffect`
> (`OnCardPlayed`), el evento quest multidimensional placeholder en `EventConfigSetup`,
> casos 8-14 en `QuestTests.cs`, y la transcripción de DD-021 a `DESIGN_DECISIONS.md`.
> Branch: `feat/m4-4b2-events-quests` desde `origin/main` con 4b-1 ya mergeado.
