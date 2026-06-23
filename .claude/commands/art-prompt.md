---
description: Genera un prompt de IA paste-ready para un slot de arte, heredando el estilo madre y las reglas canónicas (anti-drift)
argument-hint: [slot o sujeto a promptear, ej. "C1 goblin" o "fondo evento altar"]
---

Generá un prompt de arte placeholder-IA para: **$ARGUMENTS**

Esto es una operación de **generación de prompts**, NO de implementación ni de
diseño de gameplay. Tu único output es un bloque de texto paste-ready (en inglés)
que respeta el estilo madre del proyecto. El objetivo del skill es que **las reglas
de arte no se pierdan nunca**: por eso el primer paso es SIEMPRE releer las fuentes
canónicas, no responder de memoria.

## Ritual obligatorio (releer las fuentes canónicas)

1. Leé `Docs/design/ART_NEEDS.md` — **fuente única de verdad**:
   - **§Fase 2 — Estilo madre**: el bloque base `STYLE:` (pegar SIEMPRE), la variante
     `WORLD A — DARK MEDIEVAL`, la variante `WORLD B — CYBERPUNK`, y las
     restricciones técnicas. Copiá estos bloques **textuales** desde ahí — no los
     reescribas de memoria ni los parafrasees.
   - **§Tabla de assets**: si el slot pedido ya está catalogado (C1–C8, M1–M2, S1,
     N1–N2, H1, …), usá su columna "Proporción / formato" y su gancho de código.
2. Leé `Docs/design/ART_PROMPTS.md` — el formato paste-ready ya establecido y los
   ejemplos por tipo de asset (avatar 1:1, cara de carta 2:3, fondo 16:9, frames de
   efecto, etc.). Tu bloque debe calcar ese formato.

## Checklist (corré los 6 pasos, en orden)

1. **Identificá el tipo de asset** (avatar / boss / cara de carta / fondo de escena /
   ícono de nodo o HUD / secuencia de frames). De ahí salen tamaño, aspect ratio y
   transparencia.
2. **Verificá contra el código/SO si el formato o el gancho está en duda** (campo de
   sprite, `preserveAspect`, tamaño esperado, ruta). Usá Unity-MCP o búsqueda — NO
   asumas. Si el slot es nuevo y NO está en `ART_NEEDS.md`, derivá el formato del
   análogo más cercano y marcá el supuesto con `[INTERPRETACIÓN]`.
3. **Elegí la variante de mundo** y justificá en una línea:
   - El SO fija el lado/mundo (ej. lado A/B de un dual) → variante de ESE mundo.
   - El asset se reusa entre mundos (ej. caras de draft) o no tiene split → **neutral
     color-keyed** (sin bloque de mundo; paleta del tipo elemental).
   - Asset de Acto 1 sin split de mundo → puede ir **Mundo A (medieval)** por
     coherencia con el roster (mismo criterio que el boss C4). Decilo explícito.
4. **Emití el bloque paste-ready** con esta estructura exacta (en inglés, dentro de un
   bloque de código):
   - `STYLE:` — el bloque base, textual desde ART_NEEDS §Fase 2.
   - `WORLD A` / `WORLD B` / `TYPE COLOR` — la variante elegida, textual.
   - `SUBJECT:` — el sujeto concreto del slot.
   - `TECHNICAL:` — tamaño, aspect ratio, transparencia y, si hay UI encima del asset
     (fondos de panel: evento/tienda/hoguera/mapa), la regla de composición "centro
     calmo/oscuro para que el texto lea".
5. **Cerrá con los dos campos del proyecto**: `📁 Guardar el PNG como:` (ruta exacta
   según convención) y `Nota de asignación (Fase 4):` (a qué campo de qué SO va).
6. **Persistencia (anti-drift):** si es un slot nuevo y recurrente, ofrecé anexarlo a
   `ART_PROMPTS.md` y catalogarlo en `ART_NEEDS.md` (ambos NO protegidos, pero pedí OK
   a Sebastián antes de escribir). Si es one-off, devolvé el bloque sólo en el chat.

## Reglas

- **No inventes estilo.** Si el estilo madre o una restricción no está en
  `ART_NEEDS.md`, decilo y preguntá — no lo completes de memoria.
- Marcadores del proyecto (`[GDD]`, `[INTERPRETACIÓN]`, `[PROPUESTA]`) donde apliquen.
- Este skill NO cambia de modo. Si el pedido en realidad requiere código (un gancho de
  sprite que aún no existe, ej. slot 🔶 Mixto), generá igual el prompt pero avisá que
  el arte no entra hasta cerrar el PR de código, y sugerí `modo:implementacion`.
- Múltiples slots en un pedido (ej. "los 3 fondos de evento"): un bloque por slot,
  mismo estilo madre heredado en todos.
