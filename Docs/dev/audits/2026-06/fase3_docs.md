# Auditoría integral 2026-06 — Fase 3: Coherencia documental

> **Método:** 4 finders en paralelo (barrido ⏳ de GOLDEN_RULES + 3 cruces:
> GDD↔GOLDEN_RULES↔DESIGN_DECISIONS, docs técnicos↔código, specs↔implementación)
> → 16 ítems ⏳ + 38 hallazgos de cruce → verify adversarial por lotes (9
> verificadores: cada cita releída en el doc, cada afirmación de código
> reproducida con grep/lectura propia).
> **Resultado:** 37 hallazgos confirmados (18 mayores + 19 menores, 1 de ellos
> verificación positiva), 1 descartado, 9/9 filas del barrido confirmadas
> (varias con correcciones de precisión que se integran abajo).
> **Consumo:** ~594k tokens de subagentes en el run final + el gasto parcial del
> run interrumpido por usage limit el 2026-06-12 (el resume por
> `resumeFromRunId` recuperó el fan-out de caché).

---

## Resumen ejecutivo

La documentación tiene **un problema dominante y sistémico: describe el juego
de hace dos milestones.** Los hallazgos se agrupan en cinco clusters:

1. **Momentum/One More como sistema "vigente" en 5 docs** — COMBAT_ARCHITECTURE,
   GLOSSARY y DEV_ONBOARDING describen con diagramas y entradas de glosario una
   mecánica eliminada en M2 (PR #89); el GDD y MECHANIC_DUALITY arrastran
   "One More" como nombre de la mecánica central. Quien onboardee con esos docs
   aprende un juego que no existe.
2. **GOLDEN_RULES congelado en 2026-04-28** — 3 ⏳ completamente vencidos
   (PhaseBased, Shop, Campfire), 1 referencia a DD-017 "pendiente" (cerrada
   hace un mes), la ambigüedad §4 pre/post-block confirmada, y 2 reglas donde
   el código diverge de la letra (contador >5 con bonus pendiente; "transdimensional"
   usado con 3 significados distintos).
3. **Los specs/docs de Retazos prescriben el patrón que causa H1/H2** —
   RELICS.md y m3_hooks_spec [CERRADO 3] llaman "vía segura" exactamente al
   patrón roto (escribir `RunState.PlayerCurrentHP` en OnCombatEnd, que
   `ReportOutcome` pisa). Mientras no se corrijan, **cada Retazo OnCombatEnd
   nuevo de M5/M6 nacerá con el mismo bug.**
4. **_tech_snapshot y Combat/CLAUDE.md con números/gaps fantasma** — 53 vs 108
   scripts, TurnManager en 3 valores (735/~960/real 1088), sección
   "Restricciones conocidas" entera vencida (los 3 gaps resueltos en Sub-PR D),
   y Combat/CLAUDE.md negando DD-018 (efectividad bidireccional) dos líneas
   antes de afirmarla.
5. **Huérfanos del barrido ⏳** — 2 reglas cerradas por diseño sin dueño en
   ningún milestone (cartas con efecto WorldSwitch; tiers de oro DD-009, donde
   además la economía vigente se calibró contradiciendo los tiers del doc).

**Lectura transversal:** ningún hallazgo es bug de código nuevo (eso fue Fase 1);
el riesgo es de PROCESO — docs canónicos que inducen errores a futuro (cluster 3
es el más peligroso) y onboarding/contexto de Claude que describe sistemas
muertos (clusters 1-2-4 envenenan el contexto auto-cargado).

---

## 1. Barrido ⏳ de GOLDEN_RULES.md (16 ítems, exhaustivo)

Última actualización del doc: **2026-04-28** (línea 333) — pre-cierre de M2.

| § | Ítem ⏳ | Estado real | Dueño | Nota |
|---|---|---|---|---|
| §2 | Cartas con efecto WorldSwitch + Items/Retazos (:62) | **parcial** | Retazos: M3 ✓ · cartas: **HUÉRFANO** | Mitad Retazos VENCIDA (R-SW-1..4 + GrantBonusWorldSwitch). `EffectType` sin WorldSwitch (CardEnums.cs:36-44); ningún bloque del roadmap lo agenda. |
| §2 | Cambio afecta tipo de transdimensionales | no | M4-4c | Cubierto explícito (_roadmap.md:117-118). |
| §2 | Cambio NO afecta anclas | no | M4-4c | Cubierto (_roadmap.md:119-120). |
| §5 | Mazo inicial DD-002/008/020 (tipos+pool+composición+afinidad) | **parcial** | M4-4a | Selección de tipos + draft dual + 10ª carta ✓ (3E). Afinidad: 0 hits en código. Starter actual = **7 entries base + 1 drafteada = 8 cartas** (corrección del verify; no 10). |
| §5 | Tamaño del mazo DD-010 (10 exactas → 20-25) | **parcial** | inicio-en-10: 4a · rangos: **HUÉRFANO** | Crecimiento sin límite ✓. Los rangos 15-20/20-25 no figuran como tarea de balance en ningún milestone (objetivo de diseño, no feature — huérfano tolerable). |
| §5 | Mejora de cartas DD-013 | **parcial** | mecanismo: M3-3C ✓ · datos+guard: 4a · eventos: 4b | Mecanismo completo desde 3C. Faltan: upgrades en las 18 caras NewRunFaces, test guard DD-023 (0 tests cargan SOs reales), eventos como lugar de mejora. Corrección del verify: `CanUpgrade` dual usa **OR** (aHas \|\| bHas), no "ambos" como dice _roadmap.md:93-94 — el roadmap exagera. |
| §6 | "PhaseBased: declarado, no implementado aun" (:188) | **implementado** | M2-D ✓ (PR #91) | **⏳ VENCIDO** — TurnManager.cs:464-480 con filtro HP% y fallback. Debe pasar a ✓. |
| §6 | Categorías dimensionales DD-014 (estándar/ancla) | no | M4-4c | Cubierto (transdim+ancla+ficha doble). |
| §6 | Bosses DD-004 (2 tipos, fases, Sangrado/Virus) | no | M5 | M5 lo desglosa completo; depende de 4c. Building block: PhaseBased ya filtra por HP%. |
| §7 | Shop (:221) | **implementado** | M3-3D ✓ (PR #96) | **⏳ VENCIDO** salvo consumibles (sin sistema; decisión diferida RASTREADA en _roadmap.md:337, no huérfano puro). ShopTests = **14** casos hoy (roadmap dice 13). |
| §7 | Campfire (:222) | **implementado** | M3-3C ✓ (PR #94) | **⏳ VENCIDO** — descansar + mejorar + hook, 8 tests. |
| §7 | Event (:223) | no | M4-4b | Único ⏳ de §7 legítimo (placeholder en RunFlowController.cs:377-402). |
| §8 | Economía DD-009 (tiers 10-20/30-50/100, cartas de oro) | **parcial** | **HUÉRFANO** | Oro+gasto+Retazos de oro ✓. Tiers NO: un solo `goldReward=10` (RunCombatConfig.cs:12), +10 plano en nodos (RunFlowController.cs:356,402); cartas de oro no existen. Nadie lo agenda. **Agravante: el balance de tienda 2026-06-04 se calibró asumiendo ~10 oro/nodo — la economía vigente contradice los tiers del doc** (_roadmap.md:329-331). |
| §9 | Actos DD-011 (3 actos, escalada) | no | M6 (a/b) | **Discrepancia código↔doc detectada de paso: `playerMaxHP = 60` (TurnManager.cs:26) vs "HP base 70" (§9)** — no pasó por verify adversarial (fila 'no'); confirmar si la escena serializa 70 antes de reportarlo en el informe final. Micro-huérfano: "elite con mecánica única" en Acto 1. |
| §10 | Retazos DD-012 | **parcial** | M3 ✓ · eventos: 4b · "De mundo": diferida (huérfana aceptable) | ~80% VENCIDO (sistema entero desde M3). Sub-pendiente "ver DD-017" también VENCIDO (cerrada opción C 2026-05-07). Correcciones del verify: son **4** Retazos de cambio (R-SW-1..4, DD-017 decía 2-3), y TurnManager despacha **7 hooks en 8 call sites** (los otros 2 del enum los despachan Campfire/Shop). |
| §11 | Meta-progresión DD-006/016 | no | M6c | Cubierto. |

**Huérfanos netos del barrido:** cartas con efecto WorldSwitch (§2) y tiers de
economía DD-009 (§8) — mismo patrón que DD-022 (regla cerrada por diseño sin
sub-tarea dueña). Huérfanos tolerables/diferidos: rangos de DD-010 (balance),
categoría "De mundo" (fuera de contenido base por diseño), consumibles
(rastreado como decisión), elite con mecánica única (micro).

---

## 2. Hallazgos MAYORES confirmados (18)

### Cluster 1 — Momentum/One More: mecánica muerta documentada como vigente

**D1 (=A7+B10+B12+B14+B16).** `COMBAT_ARCHITECTURE.md`, `GLOSSARY.md` y
`DEV_ONBOARDING.md` describen Momentum/FreePlays como sistema vigente: big
picture (:8), diagrama PlayCard entero ("Check energy or Momentum" → "Consume
Momentum free play"), entradas de glosario FreePlays/Momentum (:17,19) y "HUD
con Energy, Momentum" (DEV_ONBOARDING:10). Realidad: grep de Momentum/FreePlay
en Assets/Scripts da UN match — el comentario `TurnManager.cs:592` que afirma su
ausencia. HUD real: `"Estilo: X/5"` (CombatHudView.cs:157); popup real
`"WEAK!\n+ESTILO"` (CombatFeedbackView.cs:139). Además el diagrama PlayCard no
refleja la API real Prepare/Resolve que consume CardHandView (B12), y los
popups ya no viven en CombatUIController sino en CombatFeedbackView (B16).
*Fix:* reescritura de los 3 docs (Estilo + flujo Prepare/Resolve). Esfuerzo M.

**D2 (=B13).** `COMBAT_ARCHITECTURE.md` no menciona NINGUNO de los subsistemas
de M2/M3 que hoy son centrales al combate: hooks de Retazos (9 hooks, 8
dispatches), Contador de Estilo, HealAction, PhaseBased, ni el pipeline de fin
de combate (DispatchCombatEnd → ReportOutcome → RunState). Estructuralmente
vencido, no solo desactualizado. *Fix:* junto con D1. Esfuerzo M (mismo PR).

**D3 (=B15).** `GLOSSARY.md` congelado pre-M2: 21 entradas solo-M1, cero
términos de M2/M3 (Estilo, Retazo, hooks, RunState, Hoguera, Tienda, NewRun,
RunMapGenerator…) y mantiene 2 entradas de sistema muerto. Esfuerzo S.

### Cluster 2 — GOLDEN_RULES.md vencido o ambiguo (archivo de Sebastián)

**D4 (=A1).** §4:119 "+1 carga cuando un ataque hace daño SuperEficaz" no
especifica pre/post-block; el código otorga PRE-block (`finalAmount > 0` antes
de encolar el DamageAction — TurnManager.cs:634-637; el block se consume
después en TakeDamage). Un SuperEficaz 100% bloqueado da carga. El comentario
del código dice "daño real", sugiriendo que el autor tampoco vio la sutileza.
Confirmado por test T3 de Fase 2. **Es DECISIÓN de diseño, no solo redacción**
(¿pre-block intencional o bug?) — ligada a H5 de Fase 1. *Fix:* Sebastián
decide y precisa §4; si la respuesta es post-block, entra a la cirugía pre-M5.

**D5 (=A2+A3, + barrido §6/§7).** Tres ⏳ completamente vencidos: PhaseBased
(:188, implementado en M2-D), Shop (:221) y Campfire (:222, ambos M3). El ✓ del
header de §6 agrava la inconsistencia interna. *Fix:* texto propuesto en
sección 5 para que Sebastián pegue.

**D6 (=A4).** §10:268 "Inclusión en contenido base: pendiente, ver DD-017" —
DD-017 cerrada 2026-05-07 (opción C) y aplicada en 3B; se shipearon 4 Retazos
de cambio (DD decía 2-3). *Fix:* misma pasada que D5.

### Cluster 3 — Docs de Retazos prescriben el patrón roto (raíz documental de H1/H2)

**D7 (=B17).** `RELICS.md:217-218,228,240` declara "mutación de
`PlayerCurrentHP` permitida" y llama a la mutación directa "la vía segura".
`RelicEndHealEffect.cs:22` implementa la receta LITERAL (su comentario cita al
doc) y el resultado es H1: `ReportOutcome` pisa el valor después
(BattleFlowController.cs:117). Para R-END-4 la condición lee HP stale
pre-combate (H2). **Ironía verificada: el doc menciona GrantHeal como
alternativa y lo descarta — es exactamente al revés** (GrantHeal sobre el actor
sobrevive porque ReportOutcome copia `_turnManager.PlayerHP`). Esfuerzo S
(nota de advertencia + regla de autoría), coordinado con el fix de H1/H2.

**D8 (=C3).** `m3_hooks_spec.md:364-368` ([CERRADO 3]) prescribe el mismo
patrón, con snippet literal de `PlayerCurrentHP`. El spec es además
internamente inconsistente: su tabla :436 usa GrantHeal (correcto) mientras
[CERRADO 3] prescribe la escritura directa. Esfuerzo S (junto con D7).

**D9 (=C2).** El spec de hooks documenta la semántica encolar-vs-ejecutar pero
NO sus dos consecuencias conocidas: (a) Estilo pre-block (cero menciones de
block en relación al otorgamiento), (b) acciones encoladas se ejecutan aunque
el actor haya muerto antes en la cola (ActionQueue.cs:50-80 sin chequeo de
muerte; cero menciones en el spec). Son los componentes de H5. *Fix:* subsección
"Consecuencias de la semántica" en el spec, tras la decisión de D4/H5.

**D10 (=C1).** `m3_hooks_spec.md:607-614` ([IMPL 1]) afirma que el path de
cartas neutras NO dispatchea OnDamageDealt y "Reabrir en Sub-PR 3B" — la
reapertura ocurrió, se implementó y aprobó (8º dispatch, TurnManager.cs:608-621
con comentario explícito), pero el spec nunca registró el cierre y sigue
afirmando lo contrario al código (también habla de "7 puntos" cuando son 8).
Esfuerzo S.

### Cluster 4 — Docs técnicos con números/gaps fantasma

**D11 (=B1+B2+B4).** `_tech_snapshot.md`: "53 archivos C#" (:299, real 108);
TurnManager "735 loc" (:347) y "~960" (:472) contradiciéndose entre sí (real
1088); y la sección "Restricciones conocidas" (:534-537) lista 3 gaps de
TurnManager **resueltos hace un mes** (PhaseBased/Heal/CalculateIntentValue,
Sub-PR D) — cuarta fuente independiente que lo confirma. Esfuerzo S.

**D12 (=B8).** `Combat/CLAUDE.md:13-22` "Estado actual": 4 LOC vencidos
(TurnManager 735 vs 1088, +48%) y el bloque omite CombatHudView y
CombatBackgroundView que el propio doc marca extraídos más abajo. Es contexto
auto-cargado para todo trabajo en Combat/. Esfuerzo S.

**D13 (=A14).** `Combat/CLAUDE.md:85-86` — "Efectividad: SOLO aplica a daño de
carta del jugador... NUNCA a daño del enemigo" **contradice DD-018** (cerrada:
bidireccional), GOLDEN_RULES §3:99 y el código (ApplyEnemyToPlayerEffectiveness,
TurnManager.cs:597,655-667). El bullet siguiente del MISMO doc dice "-1 al
recibir SuperEficaz del enemigo" — inconsistencia interna a 2 líneas de
distancia. Peligroso: es la regla que un implementador lee antes de tocar
combate. Esfuerzo S.

### Cluster 5 — Specs sin cerrar

**D14 (=C7).** Los 4 specs restantes (3E, 3F, tint, C7-arte) están **100%
implementados promesa por promesa** (verificado ítem a ítem, incluyendo conteos
de tests: 8/5/10/5) pero se presentan como "cerrado en diseño / listo para
implementación" con prompts de handoff vigentes — riesgo de que alguien tome un
handoff como tarea pendiente. *Fix:* header de estado "IMPLEMENTADO — PR #X
MERGED" o mover a `specs/_archive/`. Esfuerzo S.

---

## 3. Menores confirmados (al backlog de la pasada de docs)

- **GDD (archivo de Sebastián):** "One More" residual en :6 y :115 (A6);
  DD-010 con "10–12 cartas" y DOS bloques duplicados de tamaño final con rangos
  contradictorios 20-25 vs 20-30/30-40 (A9); "tres categorías" dimensionales
  donde DD-014 cerró 2 (A10); Mundo B "Futurista" en secciones tempranas
  (:57,70,86,87) vs "Cyberpunk" en sus propias DD-012/013 y en GOLDEN_RULES
  (A11 — inconsistencia interna del GDD además del cruce).
- **MECHANIC_DUALITY.md** se autodenomina "biblia de identidad jugable" activa
  con sección entera "One More" (A8). Fix: nota de cabecera o archivar. OJO:
  su §6.2 contiene la ÚNICA definición buena de "transdimensional" (ver
  siguiente).
- **"Transdimensional" con 3 usos inconsistentes** (A13, corregido por verify):
  GOLDEN_RULES §2:66 referencia "(ver §6)" pero §6 no usa el término; §9/GDD lo
  presentan como novedad de Acto 2; DD-014 lo usa como paraguas de
  estándar+ancla. SÍ está definido en MECHANIC_DUALITY.md:143-149 y la relación
  con 4c ya está clarificada en _roadmap.md:122-125 — el fix es alinear
  GOLDEN_RULES §6 y GLOSSARY con esa definición existente, no crear una nueva.
- **GOLDEN_RULES §4:122-125** (A12, matizado por verify): el doc SÍ cubre que
  no se genera segundo bonus; lo no especificado es el destino del CONTADOR con
  bonus pendiente — el código acumula >5 sin reset ni clamp
  (TurnManager.cs:992-996; el clamp 0-5 solo existe en SetStyleChargesForTest).
  Decidir: ¿acumula o capea?
- **_tech_snapshot** menores: "suite 142/142" en :209 (header del mismo archivo
  ya dice 146) (B3); "No gh CLI" (:530) — gh 2.92.0 instalado y usado en
  #96-#108 (B5); "No CI/CD configurado" (:529) — impreciso, tests.yml existe
  parqueado (B6); LOC menores corridos (PlayerCombatActor 246, UIController
  752, CardHandView 501, HudView 344) (B7).
- **Combat/CLAUDE.md:37-39** lista como "cambios futuros que requieren
  aprobación" dos ya implementados (Heal, PhaseBased); el tercero
  (CalculateIntentValue) sigue válido (B9, matizado).
- **COMBAT_ARCHITECTURE:75-91** "monolito de 1400+ líneas... se descompone
  incrementalmente" — descomposición completa desde 2026-05-01 (B11; absorbido
  por D1/D2).
- **m3_hooks_spec**: 5 referencias de línea a TurnManager corridas (mejor citar
  métodos, no líneas) (C4); naming de payloads divergió sin nota
  (`CampfireOptionsBuiltHookData` vs `CampfireOptionsHookData` del spec) (C5).
- **Verificación positiva (A5):** DD-022 y DD-023 SÍ están pegados en
  DESIGN_DECISIONS.md (:61-62, footer 2026-06-10) — micro-pendiente del reorden
  M4 cerrado; el cambio está en disco sin commitear.

## 4. Descartados por el verify (1)

- **C6 (DispatchCombatEnd como "divergencia" del spec):** lectura forzada — el
  spec prescribe punto y momento de invocación y el código lo cumple
  literalmente (TurnManager.cs:721-731 dentro de CheckCombatEndConditions); la
  extracción del helper es refactor interno no contradictorio.

## 5. Propuesta de texto para GOLDEN_RULES (Sebastián pega; hook bloquea a Claude)

1. §6:188 → `✓ PhaseBased: filtra moves por rango de HP (MinHpPercent/MaxHpPercent), fallback a todos los moves si ningún rango cubre (M2 Sub-PR D).`
2. §7:221 → `✓ Shop (Tienda): comprar cartas, Retazos y eliminar cartas (M3 Sub-PR 3D). Consumibles: diferidos, sin sistema aún (decisión pendiente, roadmap).`
3. §7:222 → `✓ Campfire (Hoguera): descansar (heal % de MaxHP) o mejorar 1 carta (M3 Sub-PR 3C).`
4. §10:268 → `**De cambio**: incluidos en contenido base como demo de la categoría — 4 Retazos (R-SW-1..4). DD-017 cerrada (opción C, 2026-05-07).`
5. §4:119 → precisar pre/post-block **después de decidir D4** (si pre-block se
   ratifica: `+1 carga cuando el ataque es SuperEficaz y el daño calculado pre-block es > 0 (el bloqueo del enemigo NO anula la carga)`).
6. §4:122-125 → precisar destino del contador con bonus pendiente (tras decidir A12).
7. §2:62 → desglosar: Retazos ✓ (M3) / cartas WorldSwitch ⏳ **+ asignar dueño**.

## 6. Insumos para fases siguientes

- **Fase 4 (workflow/tooling):** DEV_ONBOARDING.md describe Momentum en el HUD
  (parte de D1) — auditarlo completo en F4 que ya lo toca; los docs de modos
  remiten a plantillas que asumen specs con marca de estado (D14 sugiere
  agregar al ritual de `modo:implementacion` el paso "marcar spec como
  IMPLEMENTADO al mergear"; la regla de `/cierre-sesion` ya cubre roadmap pero
  NO specs). MEMORY.md: el micro-pendiente DD-022/023 ya se cerró (A5).
- **Fase 5 (síntesis / PLAN_PRE_M4):** los fixes documentales se agrupan
  naturalmente en 3 paquetes: (i) pasada Momentum→Estilo (D1-D3, B11, mismo
  PR docs); (ii) verdad de Retazos (D7-D10 + decisión D4/A12, coordinado con el
  fix H1/H2 y la cirugía pre-M5); (iii) números/estado (D11-D14, mecánico).
  GOLDEN_RULES y GDD van aparte (Sebastián). Huérfanos a adoptar o declarar
  diferidos: cartas WorldSwitch, tiers DD-009 (con su contradicción de
  balance), rangos DD-010, elite única.
- **Pendiente de confirmación puntual (no pasó verify):** HP base 60
  (TurnManager.cs:26) vs 70 (GOLDEN_RULES §9) — chequear el valor serializado
  en BattleScene antes del informe final.

---
*Fase 3 cerrada 2026-06-12. Verify adversarial: 9 lotes, 47 ítems verificados
(38 cruces + 9 filas de barrido con claim de código), 37+9 confirmados (varios
con correcciones de precisión integradas), 1 descartado (C6).*
