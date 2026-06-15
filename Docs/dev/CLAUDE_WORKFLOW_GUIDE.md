# CLAUDE_WORKFLOW_GUIDE.md — Trabajar bien con Claude Code

> Guía práctica para Sebastián (y el otro dev) sobre cómo sacarle el máximo a
> Claude Code en este proyecto: **prompting**, **ahorro de tokens** y, sobre todo,
> **persistencia de memoria**. No es teoría — son hábitos concretos para este repo.

Referencia estable. Si un hábito cambia, editá este archivo.

---

## 1. Persistencia de memoria (lo más importante)

Claude Code tiene una **memoria persistente entre sesiones** basada en archivos.
No vive en el chat (eso se borra); vive en disco y se vuelve a cargar cada sesión.

### Dónde vive

```
C:\Users\Asus\.claude\projects\c--Users-Asus-RoguelikeCardBattler\memory\
├── MEMORY.md                 ← índice: 1 línea por memoria, se carga SIEMPRE
├── feedback_*.md             ← una memoria = un archivo = un hecho
├── project_*.md
└── user_*.md
```

- **`MEMORY.md`** es el índice. Es lo único que se inyecta entero al inicio de
  cada sesión. Por eso es 1 línea por memoria, nunca contenido completo.
- Cada **archivo de memoria** guarda UN hecho, con frontmatter (`name`,
  `description`, `metadata.type`). El `description` es lo que Claude usa para
  decidir si esa memoria es relevante a lo que estás haciendo.

### Los 4 tipos de memoria

| Tipo | Qué guarda | Ejemplo en este repo |
|------|-----------|----------------------|
| `user` | Quién sos (rol, preferencias) | "Sebastián, sole dev, español/código inglés" |
| `feedback` | Cómo querés que Claude trabaje (correcciones, approaches confirmados) | "auto-crear GameObjects en runtime, nunca setup manual" |
| `project` | Trabajo en curso, metas, constraints NO derivables del código/git | "M3 hasta 3C mergeado, falta 3D Tienda" |
| `reference` | Punteros a recursos externos (URLs, tickets, dashboards) | link a un issue, a un doc del MCP |

`feedback` y `project` deben incluir **Why** y **How to apply**.

### Qué SÍ guardar

- Preferencias tuyas que se repiten ("siempre hacé X", "nunca Y").
- Decisiones de proceso/herramientas y su porqué (no derivables del código).
- Estado de trabajo entre sesiones ("voy por acá, falta esto").
- Correcciones: si me corregís algo y aplica a futuro, decímelo para guardarlo.

### Qué NO guardar (desperdicia y confunde)

- Lo que ya está en el código, git history, o `CLAUDE.md` (no duplicar).
- Detalles que solo importan a la conversación actual.
- Si pedís "acordate de este fix puntual" → mejor guardar **qué fue no-obvio**
  del fix, no el fix en sí (eso ya está en git).

### Cómo pedirme que recuerde / olvide

- **Guardar:** "acordate que prefiero X", "guardá esto en memoria", o simplemente
  corregime un patrón — yo propongo guardarlo. Convierto fechas relativas a
  absolutas ("la semana que viene" → fecha concreta).
- **Actualizar:** si un hecho cambió, edito el archivo existente en vez de crear
  duplicados. Decímelo: "actualizá el estado del roadmap en memoria".
- **Olvidar:** "esa memoria ya no aplica, borrala".

### Importante sobre memorias recordadas

Cuando una memoria reaparece en contexto, refleja lo que era cierto **cuando se
escribió**. Si nombra un archivo/flag/función, verifico que siga existiendo antes
de recomendarlo. Vos también: si ves una memoria desactualizada, decímelo.

### Higiene periódica

Cada tanto (o al cerrar un milestone) conviene pedir: "revisá las memorias del
proyecto, borrá las que ya no aplican y actualizá el estado". Mantiene el índice
liviano y la carga inicial barata.

---

## 2. Ahorro de tokens

El costo de Opus crece con el **historial de la conversación**, no solo con la
tarea. Hábitos que importan en este proyecto:

1. **Conversación nueva por bloque de trabajo.** Ya es tu regla (memoria
   `feedback_fresh_conversation_per_session`). Una sesión = un objetivo. No
   arrastres el historial de la feature anterior a la siguiente.
2. **Usá los modos.** `/modo-diseno`, `/modo-implementacion`, etc. cargan SOLO
   los archivos que ese modo necesita (ritual acotado), no todo `Docs/`. Evita
   leer de más.
3. **Acotá el scope del pedido.** "Implementá la sub-tarea 3D-1" lee menos que
   "seguí con la tienda". Cuanto más concreto el objetivo, menos exploración.
4. **No me hagas releer.** Si en la misma sesión ya leí un archivo, no pidas
   "leé de nuevo X" salvo que haya cambiado.
5. **Dejá que delegue en subagentes** para barridos grandes (buscar en todo el
   repo, auditorías). Los subagentes leen en paralelo y me devuelven solo la
   conclusión, no el volcado de archivos — eso ahorra contexto. No es un comando;
   lo aplico solo cuando conviene.
6. **Cerrá la sesión cuando el bloque terminó.** Invocá **`/cierre-sesion`**:
   ejecuta el checklist completo de persistencia (checkboxes + anti-desfase de
   milestones futuros + tech_snapshot + memoria + higiene si cerró milestone).
   Aplica a cualquier modo — el cierre pertenece a la sesión, no al modo. Así
   la próxima sesión arranca con contexto fresco y barato.

---

## 3. Prompting efectivo en este proyecto

Tu estilo de colaboración ya está definido: **vos diseñás, Claude implementa**
(memoria `user_collaboration_style`). El prompting eficiente se apoya en eso.

- **Invertí en el spec, no en el chat.** Un buen prompt en `modo:diseno` produce
  un spec que `modo:implementacion` ejecuta casi sin preguntas. Tiempo en el spec
  = menos ida y vuelta después.
- **Un objetivo por mensaje** cuando la tarea es concreta. Si tenés varias cosas,
  enumeralas y decime el orden.
- **Dame el criterio de "listo".** "Funciona cuando el test X pasa y se ve Y en
  BattleScene" me deja verificar solo, sin preguntarte.
- **Decí el modo explícito** (`/modo-...`) cuando querés ese comportamiento. En
  conversacional hago análisis/implementación ligera; para trabajo formal, modo.
- **Corregí temprano.** Si voy por mal camino, frename ya — no al final. Y si la
  corrección aplica a futuro, la guardo en memoria.
- **Pedí honestidad.** Si una idea tuya tiene un problema, lo digo; si no sé algo,
  lo digo. No invento. Si una búsqueda no da resultados, lo reporto.

---

## 4. Tooling de este repo (resumen)

- **Unity MCP** (`ai-game-developer`, IvanMurzak, local): ~70 tools para leer
  consola, escenas, GameObjects, correr tests NUnit, screenshots. Las de
  solo-lectura están pre-aprobadas en `.claude/settings.json`; las que mutan
  piden confirmación.
- **Permisos** — `.claude/settings.json` (compartido, commiteado) +
  `.claude/settings.local.json` (personal, gitignored). Precedencia:
  `deny > ask > allow`.
- **Hook de protección** — `.claude/hooks/protect-files.js` (PreToolUse) bloquea
  la escritura sobre los archivos protegidos por DOS superficies: (1)
  `Edit/Write/MultiEdit/NotebookEdit` por `file_path`, y (2) `Bash` con los
  comandos del plugin Unity-MCP (`script-update-or-create`/`script-delete`) que
  antes editaban `.cs` en disco sin pasar por el hook. Protege 7 archivos: 3 de
  combate + GOLDEN_RULES + GDD + DESIGN_DECISIONS + **el propio hook** (evita la
  auto-edición silenciosa). `settings.json` NO se protege (lo necesita
  `update-config`). Para una edición autorizada, comentá el hook en `settings.json`
  o editá a mano.
- **Modos como comandos** — `.claude/commands/modo-*.md`. Invocás con
  `/modo-diseno`, etc.

---

_Mantené este archivo al día si cambia un hábito o el tooling. Lo lee el otro dev._
