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
