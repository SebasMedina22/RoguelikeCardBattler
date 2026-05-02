# Plantillas Estructuradas

> **Para qué se carga este archivo:** los modos `gdd`, `diseno`, `implementacion`
> y `revision` lo leen como parte de su ritual cuando van a producir un output
> formal. Cada plantilla es el output esperado de su modo respectivo.

Estas plantillas son los outputs formales de los modos especializados. Usalas
literalmente — la consistencia entre ejecuciones es lo que hace el sistema útil.

---

## Plantilla: Análisis de GDD (output de `modo:gdd`)

```markdown
# Análisis del GDD — [Fecha YYYY-MM-DD]

## Resumen ejecutivo
[3-5 líneas: qué es el juego según el GDD nuevo, qué cambia respecto a lo actual,
qué prioridades sugiere]

## Identidad y pilares (del GDD)
- Visión central: ...
- Pilares: ...
- Tono y estética: ...

## Mapa de sistemas

### Sistemas implementados (✓)
- [Sistema X] — `[archivos clave]` — completo según GDD

### Sistemas parciales (~)
- [Sistema Y] — `[archivos clave]` — falta: ...

### Sistemas pendientes (✗)
- [Sistema Z] — no existe en código — complejidad: alta/media/baja —
  archivos a crear: ...

### Sistemas que el GDD descarta
- [Sistema W] — actualmente en código pero el GDD no lo menciona o lo elimina —
  acción sugerida: deprecar / mantener / discutir

## Decisiones que el GDD cierra
- DD-001 — [tema] — el GDD lo resuelve como: ...
- DD-002 — [tema] — el GDD lo resuelve como: ...

## Decisiones que el GDD abre o deja pendientes
- DD-009 (nueva) — [tema] — opciones: ...
- DD-010 (nueva) — [tema] — opciones: ...

## Contradicciones detectadas
- [GDD] dice X — [GOLDEN_RULES] dice Y — sugerencia: discutir cuál gana
- [Código actual] hace Z — [GDD] pide W — sugerencia: ...

## Milestones propuestos (orden por dependencias)

### M[N] — [Nombre]
**Objetivo:** ...
**Sistemas afectados:** ...
**Toca archivos protegidos:** sí/no
**Dependencias:** ...
**Sub-tareas estimadas:** ...
**Complejidad:** alta/media/baja

[Repetir por cada milestone]

## Recomendación de priorización
[Orden sugerido de M2, M3, M4... con razones]
```

---

## Plantilla: Spec Técnico (output de `modo:diseno`)

```markdown
# Spec — [Nombre de la feature]

## Origen
- GDD section / DESIGN_DECISIONS / pedido directo de Sebastián / surgió en playtest

## Objetivo
[1-2 líneas: qué resuelve esta feature en términos de gameplay]

## Comportamiento esperado
[Descripción del flujo desde la perspectiva del jugador. Qué ve, qué pasa,
qué inputs hay]

## Sistemas afectados
- Combate: ...
- UI: ...
- RunState: ...
- ScriptableObjects: ...

## Archivos a crear
- `path/Foo.cs` — propósito: ...

## Archivos a modificar
- `path/Bar.cs` — qué cambia: ...

## Archivos protegidos involucrados
- [ ] Ninguno
- [ ] `TurnManager.cs` — necesita: ... — REQUIERE APROBACIÓN

## Contratos
### Datos
[Estructuras nuevas: campos, tipos, valores por defecto]

### APIs públicas
[Métodos públicos nuevos: signatura, qué hace, side effects]

### Eventos
[Eventos nuevos en TurnManager o subsistemas]

## Reuso
- Helpers existentes que aplican: `UIAnimationHelper.X`, ...
- Patrones existentes a seguir: `IGameAction`, ...

## Casos de prueba (EditMode)
1. Caso 1: ...
2. Caso 2: ...

## Validación manual (BattleScene)
1. Paso 1: ...
2. Paso 2: ...
3. Resultado esperado: ...

## Decisiones cerradas
[Cosas que ya están decididas y no se discuten más]

## Decisiones abiertas (REQUIEREN cierre antes de implementar)
- [ ABIERTO ] ¿X o Y? Necesito que decidas.

## Alternativas consideradas
- Alt 1: [propuesta] — descartada porque ...

## Estimación
- Complejidad: alta/media/baja
- Sub-tareas: ...
- Riesgo: ...
```

---

## Plantilla: Reporte de Revisión (output de `modo:revision`)

```markdown
# Revisión — [Identificador del cambio: rama, archivo, PR]

## Resumen
[1-2 líneas: qué se revisó, veredicto general]

## Issues CRÍTICOS (bloquean merge)
1. **[archivo:línea]** — [descripción del problema] — sugerencia: ...

(o "Sin críticos.")

## Issues MAYORES (deberían arreglarse antes de merge)
1. **[archivo:línea]** — [descripción] — sugerencia: ...

## Issues MENORES (no bloquean, pero limpiar pronto)
1. **[archivo:línea]** — [descripción]

## Sugerencias (opcionales, no bloqueantes)
- ...

## Adherencia a checklist
- [x/✗] Zero console errors esperados
- [x/✗] No toca archivos protegidos sin aprobación
- [x/✗] CS0104: `UnityEngine.Object` explícito donde aplica
- [x/✗] No hay lógica de gameplay en UI
- [x/✗] No hay callbacks de animación con lógica
- [x/✗] Helpers existentes reutilizados
- [x/✗] No hay manual editor setup requerido
- [x/✗] Tests cubren la lógica no trivial (o N/A)
- [x/✗] Comentarios de onboarding presentes

## Veredicto
- [ ] Aprobado para commit
- [ ] Aprobado tras fixes menores
- [ ] Bloqueado hasta resolver críticos
```
