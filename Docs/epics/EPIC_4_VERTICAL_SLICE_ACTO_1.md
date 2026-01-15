## Epic 4 - Vertical Slice Acto 1 (8 nodos + boss)

### Objetivo
Construir una **run jugable end-to-end** (con placeholders permitidos) que permita:
- Entrar al **Mapa (Acto 1)**.
- Recorrer un acto corto de **8 nodos** con **rutas ramificadas**.
- Jugar combates con nuestras mecánicas base (tipos, efectividad, Momentum, cambio de mundo limitado).
- Recibir recompensas, tomar decisiones (tienda/evento/fogata) y llegar a un **Boss**.
- Terminar la run (victoria/derrota) y volver al flujo de run.

Este epic busca un **vertical slice** estable: no “mucho contenido”, sino **loop completo + feeling correcto**.

---

### Alcance (Scope)

#### 1) Mapa / Navegación de Run
- Estructura de mapa de Acto 1 con **8 nodos**.
- Al menos **2 rutas** (branching) con decisiones reales.
- Tipos de nodos mínimos:
  - Combate (común)
  - Evento (simple)
  - Tienda (mínima)
  - Fogata (mínima)
  - (Opcional) Elite
  - Boss final de acto
- Persistencia de “dónde voy” durante la run (aunque sea solo en memoria al inicio; guardado persistente es opcional).

#### 2) Loop de juego “end-to-end”
- Flujo mínimo:
  - Mapa → Nodo → (Combate/Tienda/Evento/Fogata) → Recompensa/Resultado → Mapa → … → Boss → Fin de Acto
- Pantallas/transiciones mínimas claras (sin pantallazos raros).

#### 3) Combate (usar lo que ya existe, sin reescribir)
- Mantener el sistema actual (TurnManager, ActionQueue, UI) como base.
- Verificaciones:
  - Tipos y efectividad se sienten consistentes.
  - Momentum funciona y es visible.
  - Cambio de mundo limitado por combate.
  - Feedback básico (hit reaction, etc.).

#### 4) Recompensas / Economía mínima
- Post-combate:
  - Otorgar oro.
  - Ofrecer elección de cartas (mínimo 1–3 opciones).
- Tienda:
  - Comprar algo simple (carta/reliquia placeholder).
- Fogata:
  - 2–3 opciones con tradeoff (ej. mejorar carta / curar / riesgo).
- Evento:
  - 1–2 eventos simples con elección (A/B) y resultado.

#### 5) Boss Acto 1
- Boss con:
  - 2–3 patrones/movimientos (secuencia o random simple).
  - 1 gimmick simple relacionado a mundos o debilidades (aunque sea placeholder).
- Definición de final:
  - Victoria: boss derrotado → pantalla/estado de fin.
  - Derrota: jugador muere → fin de run.

#### 6) Arte / Presentación mínima (placeholder aceptado)
- Backgrounds del Acto 1 (World A / World B) en formato que soporte el pipeline.
- Pulido básico del HUD:
  - Mejor layout, tamaños, espaciados y legibilidad.
  - Mantenerlo funcional y claro en PC.

#### 7) Pipeline “pro” de backgrounds (Plan seguro)
- Fase 1 (dentro de este epic):
  - Migrar backgrounds a **GameObjects en escena** con **SpriteRenderer** y parallax básico.
  - Mantener personajes en UI temporalmente si reduce riesgo.
- Fase 2 (epic futuro):
  - Migrar personajes a escena (SpriteRenderer) + animación más pro.

---

### Fuera de Alcance (Non-goals por ahora)
- Balance fino y definitivo.
- Gran cantidad de cartas/reliquias/eventos.
- Meta-progresión completa y guardados robustos.
- Port a consolas / TRCs.
- Arte final (aquí valen placeholders, pero con pipeline ordenado).

---

### Criterios de Aceptación (Definition of Done)
Este epic se considera “Hecho” cuando:
- Se puede jugar una run completa del Acto 1:
  - Mapa con 8 nodos y rutas.
  - Combates funcionales.
  - Al menos 1 tienda, 1 evento, 1 fogata.
  - Boss final jugable.
- No hay errores de compilación.
- Tests EditMode relevantes pasan (si aplican).
- El HUD es legible y no se rompe en resoluciones comunes (ej. 16:9).
- Backgrounds cargan para World A/B y el cambio de mundo no rompe el fondo.
- Todo el trabajo del epic está trazado a issues del milestone y (si aplica) PRs conectados.

---

### Estructura sugerida del Acto 1 (ejemplo)
- Nodo 1: Combate
- Nodo 2: Branch (Evento / Tienda)
- Nodo 3: Combate
- Nodo 4: Fogata
- Nodo 5: Branch (Combate / Elite opcional)
- Nodo 6: Combate
- Nodo 7: Recompensa/Evento corto (opcional)
- Nodo 8: Boss

(La estructura final la define Diseño, pero debe mantener 8 nodos y branching.)

---

### Convenciones de trabajo (para mantener orden)
- Cada issue debe tener:
  - Descripción clara
  - Checklist de entregables
  - Criterios de aceptación
  - Label de disciplina y área
- Para cerrar issues automáticamente:
  - PR con `Closes #<issue>` y merge a `main`.
- El Project Board refleja el estado real:
  - Por hacer → En progreso → En revisión → Hecho
