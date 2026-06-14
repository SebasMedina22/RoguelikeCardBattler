# Auditoría integral 2026-06 — Fase 0: Scoping

> **Sesión:** 2026-06-10 · **Modo:** auditoría dedicada (solo lectura, sin fixes)
> **Regla de la sesión:** hallazgos → ítems de roadmap/pulidos con aprobación de
> Sebastián. Verificación adversarial antes de entrar al reporte final.
> **Output final:** `AUDIT_REPORT.md` en esta carpeta (Fase 5).

---

## 1. Inventario real (medido en disco, no según docs)

| Superficie | Medido | Lo que dicen los docs |
|---|---|---|
| Scripts runtime (`Assets/Scripts/**/*.cs`) | **108** | `_tech_snapshot` dice "53 archivos C#" |
| Scripts editor (`Assets/Editor/`) | 4 | snapshot solo lista 1 (RelicSoGenerator) en su sección Editor |
| Archivos de test (`Assets/Tests/EditMode/`) | 17 | ✓ coincide (suite 146/146) |
| `TurnManager.cs` LOC | **1088** | snapshot dice 735 (árbol) y ~960 (tabla protegidos) — inconsistente consigo mismo |
| `RunFlowController.cs` LOC | 885 | no reportado — hoy es el 2º archivo más grande |
| `CombatUIController.cs` LOC | 752 | snapshot dice 710 |
| Docs activos (`Docs/**/*.md`, sin `_archive`) | ~29 | — |
| Tooling `.claude/` | settings.json + settings.local.json, 1 hook (`protect-files.js`), 5 commands, ~85 skills Unity-MCP | — |

**Nota:** el delta 53→108 no es alarmante en sí (los 23 relic effects + ~10 hook
data + M3 explican la mayoría), pero confirma que `_tech_snapshot` arrastra
números de antes de M3 en varias secciones. Va al barrido de Fase 3.

## 2. Señales detectadas durante el scoping (a verificar en su fase)

Sin verificar todavía — solo lo que saltó leyendo los 4 archivos base:

- **[→F3]** `GOLDEN_RULES.md` no se actualiza desde 2026-04-28 (pre-M2-cierre).
  Estados ⏳ visiblemente vencidos: §7 Shop/Campfire ⏳ (implementados en 3C/3D),
  §6 "PhaseBased pendiente en M2" (M2 Sub-PR D lo implementó). El barrido
  completo de ⏳ (¿implementado? ¿dueño en milestone? ¿huérfano?) es el corazón
  de Fase 3.
- **[→F3]** `_tech_snapshot` con drift interno (LOC de TurnManager en 3 valores
  distintos, conteo de scripts, sección Editor incompleta, "suite 142/142" en
  sección Tests vs 146 en header).
- **[→F1]** `RunFlowController` (885 LOC) creció hasta ser el 2º archivo del
  proyecto sin estar en ninguna lista de vigilancia — instancia Campfire, Shop,
  mapa, RelicInventoryView y panel de resolve. Candidato a revisión de
  acoplamiento antes de que 4b (eventos) le sume otro controller.
- **[→F2]** Los gaps conocidos del snapshot ("Restricciones conocidas") dicen
  que PhaseBased no está implementado, pero el roadmap M2-D dice que sí, con
  tests. Hay que ver cuál miente y si el test realmente cubre rangos de HP.

## 3. Plan de fan-out por fase

Presupuesto relativo estimado (sobre el gasto total de la auditoría):

| Fase | Tema | Mecánica | Agentes est. | Costo rel. |
|---|---|---|---|---|
| 1 | Código y arquitectura | Workflow multi-agente | ~10-15 | ~35-40% |
| 2 | Tests | Workflow chico | ~5-7 | ~15% |
| 3 | Coherencia documental | Workflow chico | ~6-8 | ~20% |
| 4 | Workflow Claude + tooling | Mayormente inline + 1-2 agentes | ~2-3 | ~10-15% |
| 5 | Síntesis | Inline (consolidación) | 0-1 | ~10% |

### Fase 1 — Código y arquitectura → `fase1_codigo_arquitectura.md`
- **Find (paralelo, 5 lectores por subsistema):**
  1. Combat core (TurnManager, ActionQueue, actors, Actions/) — solo lectura
  2. Relics (dispatcher, hooks, 23 effects, RelicInstance)
  3. Run layer (RunFlowController, BattleFlowController, RunState/Session, Shop/Campfire/NewRun)
  4. Map + datos (RunMapGenerator, configs, Cards/, Enemies/, Save/)
  5. Presentación (CombatUIController + 4 views, RunMapView, RelicInventoryView, Core/UI)
  Cada lector devuelve JSON: violaciones GOLDEN_RULES §12 (capas), acoplamientos,
  duplicación, deuda escondida (TODOs, workarounds, números mágicos).
- **Lente M5/M6 (paralelo con lo anterior):** 1 agente dedicado leyendo
  TurnManager + EnemyCombatActor + EnemyMove contra los requisitos concretos de
  M5 (fases al 50%, Desfase Dimensional, debuffs Sangrado/Virus, 2 tipos por
  boss) y M6 (transdim, ancla, bloqueo de cambio): qué va a doler y dónde.
- **Barrier:** dedup + triage de hallazgos (descartar cosméticos).
- **Verify (paralelo):** 1 verificador adversarial por hallazgo mayor:
  ¿es real? ¿es relevante para M5/M6? ¿ya está resuelto/registrado en roadmap?
- Riesgo de desproporción: BAJO-MEDIO. Si el find devuelve >25 hallazgos
  mayores, freno antes del verify y propongo recorte.

### Fase 2 — Tests → `fase2_tests.md`
- 3 lectores en paralelo: (a) inventario real de qué ASSERTA cada uno de los 17
  archivos (no qué dice cubrir), (b) superficie de riesgo sin tocar: flujo de
  run completo (NewRun→mapa→combate→outcome→reward), transiciones de RunState,
  BattleFlowController, edge cases del Contador de Estilo (cap, -1 en 0,
  no-acumulable, interacción con relics de switch), (c) cruce con los gaps
  que Fase 1 haya marcado como riesgo M5/M6.
- Verify liviano: confirmar que cada "agujero" reportado de verdad no está
  cubierto (grep dirigido sobre los tests).
- Cierre: top 5-10 tests propuestos, ordenados por riesgo eliminado/esfuerzo.

### Fase 3 — Coherencia documental → `fase3_docs.md`
- **Barrido ⏳ (1 agente):** tabla exhaustiva de TODOS los ⏳ de GOLDEN_RULES →
  estado real en código (implementado/parcial/no) + dueño en roadmap (bloque
  concreto) o **HUÉRFANO**. Precedente DD-022: 6 semanas sin dueño.
- **Cruces (3 agentes en paralelo):** (a) GDD ↔ GOLDEN_RULES ↔ DESIGN_DECISIONS
  (decisiones cerradas no reflejadas, contradicciones), (b) docs técnicos ↔
  código (números viejos, promesas falsas, secciones vencidas de _tech_snapshot,
  COMBAT_ARCHITECTURE, GLOSSARY, Combat/CLAUDE.md), (c) specs en `Docs/dev/specs/`
  ↔ implementación mergeada (¿algún spec promete algo que no llegó?).
- Verify: confirmación puntual contra código de cada "violación" reportada.

### Fase 4 — Workflow Claude + tooling → `fase4_workflow_tooling.md`
- Inline (yo, sin fan-out): `settings.json` + `settings.local.json` (drift entre
  ambos, permisos), `hooks/protect-files.js` (¿cubre los 3 protegidos? ¿bypasses?),
  5 commands, archivos de modos, MEMORY.md (memorias vencidas), CLAUDE_WORKFLOW_GUIDE.
- 1-2 agentes: costo/beneficio de las ~85 skills Unity-MCP (cuáles se usan según
  historial de docs/PRs) + tips de Claude Code no aplicados a esta escala de
  proyecto.

### Fase 5 — Síntesis → `AUDIT_REPORT.md`
- Inline: consolidar SOLO hallazgos que pasaron verify. Formato: severidad
  (crítico/mayor/menor/oportunidad), esfuerzo estimado (S/M/L), propuesta
  de destino (roadmap M4/M5/pulido/descartar + por qué).

## 4. Criterios transversales

- Todo hallazgo entra con referencia `archivo:línea` verificable.
- "Prefiero 10 sólidos que 40 especulativos": el verify mata especulación;
  los descartados quedan listados en una sección corta "descartados y por qué".
- Cada fase cierra con: archivo escrito + resumen corto en chat + consumo
  aproximado + GO/NO-GO de Sebastián.

---
*Consumo Fase 0: mínimo (~4 lecturas de docs + 3 globs + 1 conteo; sin subagentes).*
