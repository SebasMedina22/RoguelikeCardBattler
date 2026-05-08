# Combat Directory Rules

> **Cuándo se carga este archivo:** automáticamente cuando Claude accede a
> cualquier archivo en `Assets/Scripts/Gameplay/Combat/`. Complementa al
> `CLAUDE.md` raíz con guardrails específicos del directorio.
>
> **Para qué modos aplica:** principalmente `modo:implementacion` y `modo:revision`
> cuando tocan combate. También `modo:diseno` para conocer las restricciones
> antes de proponer specs.

---

## Estado actual (post-extracciones, abril 2026)

```
CombatUIController.cs   → 980 loc  (era 1473) — orquesta BuildUI + HUD + lifecycle
CombatFeedbackView.cs   → 357 loc  (Fase 1) — popups, shake, victory/defeat, flash
CardHandView.cs         → 400 loc  (Fase 2) — mano de cartas, click, layout
TurnManager.cs          → 735 loc  PROTEGIDO
ActionQueue.cs          →  85 loc  PROTEGIDO
PlayerCombatActor.cs    → 236 loc  PROTEGIDO
```

---

## Archivos protegidos (NO MODIFICAR sin aprobación explícita)

- `TurnManager.cs` — orquesta turnos, juego de cartas, cambio de mundo,
  Contador de Estilo, cálculo de efectividad, IA enemiga (`SelectEnemyMove`,
  `CalculateIntentValue`).
- `ActionQueue.cs` — cola de acciones determinista FIFO.
- `PlayerCombatActor.cs` — HP, bloqueo, aplicación de daño del jugador.

Si una tarea requiere modificar estos archivos, **PARAR y pedir aprobación
explícita a Sebastián antes de continuar.** Esto aplica a:

- Implementar `EffectType.Heal` (case nuevo en `CreateAction()`)
- Implementar `PhaseBased` AI (case nuevo en `SelectEnemyMove()`)
- Ampliar `CalculateIntentValue()` para combinaciones nuevas
- Cualquier otro cambio estructural en estos archivos

---

## CombatUIController.cs — Reglas activas

1. **NO agregar métodos ni responsabilidades nuevas.** El plan es seguir adelgazando
   este archivo, no engordarlo más.
2. **UI nueva** = MonoBehaviour separado, agregado como componente hermano y
   wireado en `InitializeExtractedViews()`. NO dentro de CombatUIController.
3. **Solo presentación.** Nunca mutar gameplay state desde aquí.
4. **No es referenciado por BattleFlowController** — son independientes. Ambos
   se conectan a TurnManager por separado.
5. **Plan de extracción** (ver `Docs/dev/COMBAT_ARCHITECTURE.md`):
   - ✓ `CombatFeedbackView` (Fase 1 — completo)
   - ✓ `CardHandView` (Fase 2 — completo)
   - ✓ `CombatHudView` (Fase 3 — completo, M-tech cerrado 2026-05-01)
   - ✓ `CombatBackgroundView` (Fase 4 — completo, M-tech cerrado 2026-05-01)

---

## Patrón de extracción (para Fases 3-4 futuras)

Cada componente extraído sigue este patrón (ver `CombatFeedbackView` y
`CardHandView` como referencia):

1. Vive en el mismo GameObject que CombatUIController (componente hermano).
2. Recibe referencias de UI vía método `Initialize()` después de `BuildUI()`.
3. Se suscribe a eventos de TurnManager en `OnEnable`/`OnDisable` de forma
   independiente (no necesita que CombatUIController le pase los eventos).
4. Es **solo presentación** — nunca muta gameplay state.
5. Limpia sus propios tweens/coroutines en `OnDestroy`.

---

## Turn Flow (NO alterar orden)

1. **Turno jugador**: robar cartas → jugar cartas → fin de turno
2. **Turno enemigo**: limpiar bloqueo → ejecutar movimiento → planear siguiente → verificar fin
3. Efectos se resuelven vía `ActionQueue.ProcessAll()` (determinista, secuencial)

---

## Key Patterns

- **Efectividad**: SOLO aplica a daño de carta del jugador vs tipo del enemigo.
  NUNCA a daño del enemigo, bloqueo, robo de cartas ni ningún otro efecto.
- **Contador de Estilo**: +1 carga al hacer SuperEficaz con daño real; -1 al
  recibir SuperEficaz del enemigo. 5 cargas → 1 switch de mundo extra (no
  acumulable), reset de cargas. Se lee vía `TurnManager.StyleCharges`.
- **Cambio de mundo**: limitado por combate (configurable, default 1, debug
  ilimitado). Afecta lado activo de cartas duales.
- **Acciones**: `DamageAction`, `BlockAction`, `DrawCardsAction` implementan
  `IGameAction`. Nuevos tipos siguen este patrón.

---

## Deuda técnica resuelta en Sub-PR D (2026-05-07)

- **PhaseBased AI**: implementado en `SelectEnemyMove()`. Filtra moves por
  `MinHpPercent`/`MaxHpPercent`; fallback a todos los moves si ningún rango cubre.
- **EffectType.Heal + HealAction**: `ICombatActor.Heal()` implementado en Player
  y Enemy; `HealAction` resuelve vía ActionQueue; `CreateAction()` tiene su case.
- **CalculateIntentValue**: cubre `Defend+Heal` además de `Attack+Damage` y
  `Defend+Block`.

---

## Adding New Combat Features (en `modo:implementacion`)

1. Leer `Docs/dev/COMBAT_ARCHITECTURE.md` para diagramas y flujo de datos.
2. Leer `Docs/dev/_tech_snapshot.md` para conocer arquitectura.
3. **Nuevas acciones**: crear en `Actions/` implementando `IGameAction`.
   Probable: requerir agregar case en `TurnManager.CreateAction()` (PROTEGIDO,
   pedir aprobación).
4. **Nueva UI de combate**: crear MonoBehaviour separado. NO extender
   CombatUIController. Wirear en `InitializeExtractedViews()`.
5. **Nuevos datos**: agregar campos en ScriptableObjects. Nunca hardcodear.
6. **Nuevos enemigos/patrones**: definir en `EnemyDefinition` SO.
   `AiPattern.PhaseBased` está implementado — configurar `MinHpPercent`/
   `MaxHpPercent` en cada `EnemyMove` del SO para definir fases.
