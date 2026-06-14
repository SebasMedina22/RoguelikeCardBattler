# Auditoría integral 2026-06 — Fase 4: Workflow Claude + tooling

> **Sesión:** 2026-06-12 · Solo lectura. Plan: `fase0_scoping.md` §Fase 4.
> Mecánica: inline (settings, hook, commands, modos, MEMORY.md, guías) +
> 2 agentes (skills Unity-MCP, tips de Claude Code). Todo hallazgo verificado
> contra disco/git antes de entrar acá; lo descartado va en §5.

---

## 0. Inventario verificado

| Superficie | Medido |
|---|---|
| `.claude/` trackeado en git | `settings.json`, `hooks/protect-files.js`, 5 commands (verificado con `git ls-files`) |
| `.claude/settings.local.json` | gitignored (regla `.claude/*` + excepciones), 1 sola regla personal — sin drift |
| Skills Unity-MCP | **77 carpetas** (no ~85 como decía el scoping, no ~73/~70 como dicen DEV_ONBOARDING/WORKFLOW_GUIDE) |
| Hook | 1 (PreToolUse `protect-files.js`), cubre los **6** protegidos (3 código + 3 docs) |
| Memoria persistente | MEMORY.md (~10 KB) + 15 archivos; **2 huérfanos no indexados** |

---

## 1. Hallazgos MAYORES

### T1 — El hook protect-files tiene un bypass real vía Bash/MCP (MAYOR, fix S)

- **[CÓDIGO ACTUAL]** El hook solo se engancha a `Edit|Write|MultiEdit|NotebookEdit`
  (`settings.json:93`). Cualquier escritura que NO pase por esas tools no se
  intercepta.
- **[CÓDIGO ACTUAL]** `settings.json:79` auto-aprueba `Bash(npx unity-mcp-cli *)`
  (y `:43` `Bash(unity-mcp-cli run-tool:*)`). Por esa vía se puede invocar
  `script-update-or-create` / `script-delete` / `assets-modify` del plugin
  Unity-MCP, que **sobreescriben archivos `.cs` en disco** — incluido
  `TurnManager.cs` — sin prompt de permiso y sin pasar por el hook.
- Vector secundario, menor: `git checkout:*` / `git restore:*` auto-aprobados
  pueden revertir un protegido en silencio (riesgo bajo: revierte, no inventa).
- **Escenario realista de accidente:** una sesión de `modo:implementacion` con
  MCP conectado elige `script-update-or-create` en vez de Edit para tocar un
  protegido → la protección entera queda en la disciplina del modelo.
- **Propuesta (no aplicada, solo lectura):** dos capas complementarias —
  1. Agregar al hook un matcher `Bash` que bloquee si el comando contiene
     `script-update-or-create|script-delete` junto a un nombre protegido
     (cambio chico en `protect-files.js` + 1 entrada en `settings.json`).
  2. Deshabilitar en el plugin las tools de mutación de scripts que no se usan
     (ver T9 — cierra el mismo agujero desde el otro lado).

### T2 — DEV_ONBOARDING.md describe el juego pre-M2 (MAYOR, cluster D1 de F3)

Auditado completo (insumo de Fase 3). Confirmado contra código:

| Línea | Dice | Realidad |
|---|---|---|
| 10 | HUD con "Energy, **Momentum**, World y Switches" | Momentum eliminado en M2 (PR #89). Única ocurrencia en `Assets/Scripts`: un comentario histórico en `TurnManager.cs:592` que dice "ese sistema desaparece" |
| 14 | popup "**MOMENTUM +1**" al pegar WEAK | el feedback hoy es del Contador de Estilo |
| 29 | TurnManager "orquesta turnos, ..., **momentum**" | ídem |
| 12-13 | prueba con `StrikeBasic.asset` / `WeakSlime.asset` | los assets SÍ existen (verificado) — la receta sigue siendo usable, solo el resultado descrito está viejo |
| 45 | MCP "da **~73 tools**" | 77 skills en disco; WORKFLOW_GUIDE:130 dice "~70" — 3 números distintos en 3 docs |
| 57-58 | "activan los **4 modos**" | falta `/cierre-sesion` (5º command, agregado en #108) |
| 28-34 | "Dónde mirar primero" | 100% combate; ignora todo el Run layer post-M3 (`RunFlowController` 885 LOC, NewRunScene, mapa) y las otras escenas |

Destino sugerido: entra al paquete (i) "pasada Momentum→Estilo" de Fase 5
(D1-D3, B11) + una pasada de actualización M3 (esfuerzo S, mismo PR docs).

### T3 — D14: el paso "marcar spec como IMPLEMENTADO" no tiene dueño (MAYOR de proceso)

Evaluado dónde encaja (insumo de F3). Confirmado el triple gap:

- La plantilla de Spec Técnico (`_plantillas.md:74-142`) **no tiene campo
  Estado** — un spec no puede "cerrarse" porque no hay dónde.
- El checklist de `/cierre-sesion` (`cierre-sesion.md:9-33`) cubre roadmap
  (pasos 1-2), snapshot (3), memoria (4), milestone (5), git (6) — **specs: nada**.
- "Cierre de una sub-tarea sustantiva" de `_modo_implementacion.md:128-134`
  verifica compilación/tests/flujo — tampoco menciona el spec.

**Propuesta de encaje** (el merge ocurre a nivel sesión, no dentro del modo —
el dueño natural es el ritual de cierre):

1. **`/cierre-sesion`** — nuevo paso entre el 1 y el 2: *"Specs: si esta sesión
   mergeó un PR que implementa un spec de `Docs/dev/specs/`, agregar al header
   del spec `> **ESTADO: IMPLEMENTADO** — PR #N (YYYY-MM-DD)`"*.
2. **`_plantillas.md`** — la plantilla de Spec Técnico gana una línea de header
   `> **ESTADO: BORRADOR | CERRADO | IMPLEMENTADO (PR #N)**`.
3. Opcional (refuerzo): 1 línea en el cierre de `_modo_implementacion.md`.

Los 4 specs ya mergeados sin marca (D14) se marcan retroactivamente en el
mismo PR de docs de Fase 5.

---

## 2. Hallazgos MENORES

### T4 — Ruido y huecos en `settings.json`

- **Redundancia:** `:42` `npx --yes unity-mcp-cli@latest:*`, `:43`
  `unity-mcp-cli run-tool:*` y `:79` `npx unity-mcp-cli *` se superponen (la
  `:79` subsume a las otras en la práctica). Consolidar en una.
- **Resto de otra plataforma:** `:78` `Read(//tmp/**)` no aplica en Windows.
- **Deny incompleto para PowerShell:** el shell por defecto ES PowerShell y el
  deny solo cubre `rm:*` / `rm -rf:*` (`:82-83`, la segunda redundante con la
  primera). `Remove-Item`, `del`, `rd /s` no están. Mismo espíritu, otra sintaxis.
- `settings.local.json`: 1 regla, coherente con su `$comment`. **Sin drift** —
  la sospecha del plan no se confirmó.

### T5 — Higiene de memoria persistente

- **2 huérfanos no indexados en MEMORY.md** (existen en disco, nunca se cargan):
  `project_next_phase.md` (2026-05-07, "M3 entrando en diseño" — vencidísima) y
  `project_newrun_3e_spec.md` (3E cerrado, PR #97). → borrar; lo vigente ya
  vive en otras memorias. La primera además tiene el frontmatter mal formado
  (`type:` fuera de `metadata:`).
- **Indexadas pero cerradas** (candidatas a borrar/condensar en la higiene de
  cierre de auditoría): `project_shop_3d_spec` (PR #96 merged),
  `project_m4_reorder_pending` (cerrado salvo el commit pendiente),
  `project_dev_tooling_mcp` ("las 4 fases hechas (cerrado)"),
  `project_art_audit_plan` (11.7 KB para un tail opcional).
- **MEMORY.md (~10 KB) viola su propia regla** ("1 línea por memoria, nunca
  contenido" — CLAUDE_WORKFLOW_GUIDE:25-30): la sección "Estado actual" son
  párrafos largos que duplican las memorias de proyecto. Es carga inicial cara
  en CADA sesión — exactamente lo que la guía §2 dice evitar.
- **DD-022/023:** la memoria es EXACTA, no vencida — `git status` confirma
  `M Docs/design/DESIGN_DECISIONS.md` aún sin commitear al 2026-06-12. (El
  insumo de F3 cerró el micro-pendiente de *disco*; el commit sigue pendiente.)

### T6 — Inconsistencias menores en guías

- Conteo de tools: 77 reales vs "~70" (WORKFLOW_GUIDE:130) vs "~73"
  (DEV_ONBOARDING:45). Unificar o decir "~75+, ver `unity-tool-list`".
- WORKFLOW_GUIDE §4 lista los commands `modo-*` pero no `/cierre-sesion`
  (que §2.6 sí explica). Coherencia interna a medias.
- `cierre-sesion.md` es el único command **sin frontmatter**
  (description/argument-hint) — los otros 4 lo tienen.

### Sanos (auditados, sin hallazgo)

- `protect-files.js`: cubre los 6 protegidos, normaliza separadores,
  case-insensitive, fail-open razonable ante JSON inválido. El único agujero
  es el de superficie (T1), no de lógica interna.
- Los 7 archivos de `Docs/dev/modes/`: sin drift Momentum, sin referencias
  muertas, consistentes entre sí y con CLAUDE.md raíz. La política de testing
  de `_modo_implementacion` y el playbook de `_subagentes.md` están al día.
- Los 4 commands `modo-*`: consistentes con la regla de activación.
- `.gitignore` de `.claude/`: correcto (skills excluidas = regenerables;
  settings/hooks/commands re-incluidos; `settings.local.json` ignorado como
  prometen los docs).

---

## 3. Oportunidades (agentes)

### T9 — Skills Unity-MCP: ~36 de 77 son carga muerta desactivable (OPORTUNIDAD, esfuerzo S)

Agente de análisis de uso (evidencia en docs/specs/commits/settings):

- **Con evidencia de uso o pre-aprobadas en settings (~22):** tests-run,
  console-get-logs, scene-*, editor-application-get-state, assets-find/get-data,
  gameobject-find/component-get, screenshot-*, profiler-get-* (solo lectura),
  script-execute, ping, unity-tool-list y sistema. → mantener.
- **Carga muerta probable (~36):** profiler de control (start/stop/save/load/
  module ×8 — overkill para un juego de turnos 1v1), package-* ×4,
  reflection-* ×2, mutación de assets/prefabs (copy/delete/move/modify/
  material/prefab-* ×11), mutación de gameobjects ×7, editor-*-set ×2,
  script-read/delete/update-or-create ×3, type-get-json-schema,
  screenshot-isolated, console-clear-logs. Cero referencias en specs, modos,
  roadmap o commits.
- **Mecanismo correcto:** `tool-set-enabled-state` (persiste en el plugin).
  **NO borrar carpetas** — `unity-skill-generate` las regeneraría todas.
  Reversible en 1 llamada si M4 las necesita (p. ej. re-habilitar assets-* si
  se aprueba una skill propia de generación de SOs).
- **Beneficio:** lista de skills por sesión 77→~41 (ahorro de contexto real,
  aunque la cifra exacta de tokens del agente es estimativa), menos superficie
  de mutación accidental, y **cierra parcialmente el bypass T1** (deshabilitar
  `script-update-or-create`/`script-delete` mata el vector principal).

### T10 — Tips de Claude Code aplicables (filtrados adversarialmente)

Del segundo agente sobrevivieron al verify **3 de 10** propuestas (el resto
citaba features inexistentes — ver §5):

1. **`model: sonnet` en el frontmatter de `cierre-sesion.md`** (esfuerzo: 1
   línea). Los commands soportan override de modelo por frontmatter; hoy el
   "/model sonnet para abaratar" es un hábito manual documentado en memoria.
   Automatizarlo elimina el paso y el riesgo de olvidarlo. De paso resuelve T6
   (frontmatter faltante).
2. **`/fewer-permission-prompts`** (skill built-in, 1 corrida): escanea
   transcripts reales y propone el allowlist — insumo objetivo para la
   limpieza de T4 en vez de curarla a mano.
3. **De-scope automático de `settings.json`/`.slnx`:** el dolor es real (paso 6
   de /cierre-sesion existe por eso), pero la propuesta del agente (hook
   PostToolUse) NO funciona — la auto-modificación la hace el harness, no una
   tool Edit/Write, así que el hook nunca dispararía. Alternativa que sí
   funciona si se quiere automatizar: **git pre-commit hook** que rechace
   commits que incluyan esos 2 archivos salvo flag explícito. Veredicto: nice
   to have; el paso manual del ritual ya lo cubre. Decidir en F5.

---

## 4. Resumen para Fase 5

| ID | Severidad | Tema | Esfuerzo | Destino sugerido |
|---|---|---|---|---|
| T1 | MAYOR | Bypass de protect-files vía Bash/MCP | S | Fix inmediato (hook + T9) |
| T2 | MAYOR | DEV_ONBOARDING pre-M2/M3 | S | Paquete docs (i) Momentum→Estilo |
| T3 | MAYOR (proceso) | Specs sin marca de cierre (D14) | S | Editar cierre-sesion + plantillas + marcar 4 specs retro |
| T4 | MENOR | Ruido/huecos en settings.json | S | PR de tooling |
| T5 | MENOR | Higiene de memoria (2 huérfanas, índice pesado) | S | Higiene al cierre de auditoría |
| T6 | MENOR | Conteos de tools y omisiones en guías | S | Mismo PR docs que T2 |
| T9 | OPORTUNIDAD | Deshabilitar ~36 skills muertas | S | 1 llamada tool-set-enabled-state (decisión de Sebastián) |
| T10 | OPORTUNIDAD | model:sonnet en cierre-sesion + /fewer-permission-prompts | S | PR de tooling |

## 5. Descartados y por qué

- **Drift settings.json ↔ settings.local.json** (sospecha del plan): no existe —
  el local tiene 1 regla coherente.
- **7 de 10 tips del agente de Claude Code:** output styles con JSON de reglas,
  frontmatter `paths:` en commands, `promptCacheConfig`/TTL en settings,
  comando `/batch` para worktrees, hook `PermissionRequest` como lo describió —
  **features inexistentes o mal descritas** (alucinadas); SessionStart con
  assets-refresh (latencia en cada sesión para cubrir un caso que la regla
  "no switchear con Unity abierto" ya previene); PreCompact snapshot de memoria
  (valor marginal).
- **Hook PostToolUse para de-scope** tal como lo propuso el agente: mecanismo
  incorrecto (ver T10.3).
- **"Suite 146/146" y conteos de skills de agentes** (78 vs 77): corregidos a
  los valores medidos en disco.

---
*Consumo Fase 4: 2 subagentes (~48k + ~72k tokens de subagente) + inline
(~25 lecturas, 4 greps/globs, 3 comandos git/fs de verificación). Verify: cada
hallazgo de §1-§3 confirmado contra disco/git; lo no confirmable, descartado (§5).*
