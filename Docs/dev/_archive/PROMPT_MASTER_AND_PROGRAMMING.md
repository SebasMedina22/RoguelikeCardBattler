> **DEPRECATED**: Este workflow dual-agent ha sido reemplazado por un solo agente usando
> Claude Code Plan mode + `CLAUDE.md` como guardrails. Se mantiene como referencia histórica.
> Ver `CLAUDE.md` en la raíz del proyecto para el workflow actual.

# Prompt Master y Programming Master — Workflow de desarrollo (HISTÓRICO)

Este documento describe el proceso de desarrollo original del proyecto: la arquitectura de software que seguimos, la separación de roles entre **Prompt Master** y **Programming Master**, y las reglas que garantizan consistencia y calidad.

---

## 1. Arquitectura de software

### Principios generales

El proyecto sigue una arquitectura clara para mantener el código mantenible, escalable y fácil de onboardear:

| Principio | Descripción |
|-----------|-------------|
| **Scene-owned controllers** | Cada escena tiene su controlador principal (RunFlowController, BattleFlowController, MainMenuController). No hay "managers" globales flotantes salvo RunSession (DontDestroyOnLoad para estado entre escenas). |
| **Data-driven** | Tipos, enemigos, cartas y configuración viven en ScriptableObjects. La lógica lee datos, no los hardcodea. |
| **Separación de responsabilidades** | **RunState** = datos. **Controllers** = flujo y decisiones. **UI/VFX** = presentación que se suscribe a eventos o lee estado. No mezclar lógica de gameplay en prefabs o UI. |
| **Persistence mínima** | Solo RunSession persiste entre escenas. El save/load meta (stats, desbloqueos) usa ISaveService con JSON en `Application.persistentDataPath`. |
| **Comunicación entre escenas** | Exclusivamente vía RunSession.State: flags como `PendingReturnFromBattle`, `LastNodeOutcome`, `CurrentNodeId`, `RunFailed`, `ActoCompleted`. |

### Flujo de datos (Run)

```text
MainMenu → RunScene (mapa) → BattleScene (combate) → RunScene (resultado) → ...
                ↓                      ↓
          RunState (nodos, gold,      RunState (HP, outcome)
          deck, available/completed)
```

### Archivos y responsabilidades

- **No tocar core de combate**: TurnManager, ActionQueue, PlayerCombatActor manejan la mecánica. Los controllers orquestan, no implementan.
- **Helpers reutilizables**: Métodos en `Core/UI/UIAnimationHelper.cs` y `Core/Audio/AudioManager.cs` deben usarse en vez de duplicar lógica.

---

## 2. Prompt Master y Programming Master

### ¿Qué es cada uno?

| Rol | Responsabilidad | Herramienta típica |
|-----|-----------------|--------------------|
| **Prompt Master** | Escribir prompts detallados, definir issues, revisar arquitectura, dar criterios de aceptación, validar que los cambios respeten las reglas. | Cursor (este chat), GitHub Issues |
| **Programming Master** | Implementar el código según el prompt: crear/modificar archivos, mantener la arquitectura, entregar diffs revisables. | Codex u otro agente de código |

El **Prompt Master** no escribe código directamente; el **Programming Master** no toma decisiones de diseño. Cada uno tiene un rol claro.

### Ventajas de esta separación

1. **Consistencia**: Los prompts incluyen restricciones explícitas (qué archivos tocar, qué no tocar), evitando desvíos.
2. **Onboarding**: Cualquier desarrollador puede ser Prompt Master con solo leer este doc y los issues; el Programming Master recibe instrucciones autocontenidas.
3. **Calidad**: El Prompt Master valida que el resultado cumpla criterios de aceptación antes de cerrar el issue.
4. **Reproducibilidad**: Prompts reutilizables para fixes o features similares.

---

## 3. Formato de prompts para el Programming Master

Todo prompt debe incluir:

### 3.1 Contexto existente
- Descripción breve de lo que ya existe y no debe romperse.
- Referencias a archivos clave y flujos de datos.

### 3.2 Restricciones (no negociables)
- **NO tocar**: lista explícita de archivos/carpetas que no se deben modificar.
- Reglas de arquitectura: scene-owned, data-driven, no lógica en UI.

### 3.3 Objetivo claro
- Una o más oraciones que expliquen qué debe hacer el código.

### 3.4 Alcance (archivos a crear/modificar)
- Lista concreta: crear X, modificar Y, opcionalmente Z.

### 3.5 Regla CS0104 (ambiguidad Object)
- Si el archivo usa tanto `using System;` como `using UnityEngine;`, **siempre** usar `UnityEngine.Object` (no solo `Object`) para evitar el error de referencia ambigua.

### 3.6 Criterios de aceptación
- Lista de comprobaciones que el Prompt Master usará para validar.
- Incluir "Cero errores en consola".

### 3.7 Formato de entrega
- Indicar que el diff debe ser revisable, con comentarios de onboarding.
- No incluir "Closes #X" en el primer commit si el issue se cierra después.

---

## 4. Flujo típico de un issue

```text
1. Prompt Master crea issue en GitHub (objetivo, alcance, criterios).
2. Prompt Master escribe prompt completo siguiendo la plantilla.
3. Programming Master implementa según prompt.
4. Prompt Master revisa: criterios de aceptación, arquitectura, reglas.
5. Si hay errores: Prompt Master devuelve feedback específico (qué falló, cómo corregirlo).
6. Si todo OK: commit con summary/description, merge, cierre del issue.
```

---

## 5. Checklist para el Prompt Master antes de cerrar un issue

- [ ] ¿Los archivos tocados respetan las restricciones (no se tocó lo prohibido)?
- [ ] ¿Se mantiene la separación datos / lógica / presentación?
- [ ] ¿Se reutilizaron helpers existentes cuando aplica?
- [ ] ¿Los criterios de aceptación se cumplen?
- [ ] ¿Hay comentarios de onboarding en código nuevo?
- [ ] ¿Cero errores en consola durante el flujo de prueba?

---

## 6. Referencias

- Arquitectura de combate: `Docs/dev/COMBAT_ARCHITECTURE.md`
- Onboarding: `Docs/dev/DEV_ONBOARDING.md`
- Glosario: `Docs/dev/GLOSSARY.md`
