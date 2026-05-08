# DESIGN DECISIONS — Decisiones de Diseno

> Este documento recoge las decisiones de diseno que estan EN DISCUSION o CERRADAS.
> Cuando una decision se cierra y se cristaliza, su regla resultante se mueve a GOLDEN_RULES.md.
> Formato: pregunta -> opciones -> resolucion / estado.

---

## Estado actual

GDD v2 (procesado el 2026-04-28) cerro **DD-001 a DD-016**. Las reglas resultantes viven en `GOLDEN_RULES.md`. En la revision post-GDD se abrieron **DD-017 a DD-021** y se cerraron todas excepto DD-017. **DD-017 fue cerrada el 2026-05-07** durante el diseño de M3.

**Hoy no quedan decisiones de diseño abiertas.** Solo queda `DD-015` (narrativa) en estado `postponed`.

---

## Cerradas por GDD v2 (2026-04-28)

| DD  | Tema | Resolucion (resumen) | Donde vive |
|-----|------|----------------------|------------|
| DD-001 | Cambio de mundo y sistema de cargas | Momentum se elimina. Sistema unico: Contador de Estilo. +1 carga por SuperEficaz hecho, -1 por SuperEficaz recibido, 5 cargas = 1 cambio extra (no acumulable). 1 cambio gratis base por combate; cambios adicionales por cargas, cartas o items | GOLDEN_RULES §2, §4 |
| DD-002 | Sistema de tipos | El jugador elige 2 tipos al inicio (uno por mundo). El tipo activo del jugador = tipo del mundo activo. El pool de cartas se filtra por los 2 tipos elegidos. Mazo inicial: 10 cartas exactas | GOLDEN_RULES §3, §5 |
| DD-003 | Sistema de tipos | Redundante: cubierta por DD-002 | — |
| DD-004 | Diseno de bosses | 2 tipos por boss (1 SuperEficaz contra el jugador, 1 debilidad). Mecanicas unicas que interactuan con el cambio de mundo. Fase 2 obligatoria al 50% HP. Pueden aplicar debuffs unicos vinculados al tema (Sangrado en Medieval, Virus en Cyberpunk) | GOLDEN_RULES §6 |
| DD-005 | Sistema de nodos | 6 tipos: Combat, Elite, Boss, Shop, Campfire, Event. Mapa scroll lateral generado por seed. Eventos multidimensionales permiten elegir mundo | GOLDEN_RULES §7 |
| DD-006 | Progresion roguelite | XP entre runs. Desbloqueos amplian opciones (cartas, Retazos), nunca poder | GOLDEN_RULES §11 |
| DD-007 | Atributos del heroe | Cerrada via DD-002. El "atributo del heroe" es el tipo activo, derivado del mundo en que esta. No hay sistema separado | GOLDEN_RULES §3 |
| DD-008 | Pool de cartas | Cerrada via DD-002. El pool se filtra por los 2 tipos elementales elegidos al inicio del run, no por par narrativo de mundos | GOLDEN_RULES §5 |
| DD-009 | Economia | Combate 10-20, Elite 30-50, Boss 100. No persiste entre runs. Usos: comprar cartas/Retazos/consumibles, eliminar cartas | GOLDEN_RULES §8 |
| DD-010 | Tamano del mazo | Inicio 10 (exactas), Acto 1 objetivo 15-20, fin de run objetivo 20-25. Sin limite duro | GOLDEN_RULES §5 |
| DD-011 | Dificultad y actos | HP base 70. 3 actos. Acto 1: 1 tipo / patrones simples. Acto 2: 2 tipos / fases / transdimensionales. Acto 3: multifase / ancla / bosses que interactuan con cambio | GOLDEN_RULES §9 |
| DD-012 | Retazos | Pasivos persistentes. 3 categorias: neutros, de cambio, de mundo. Sin limite. Obtencion: Elite (garantizado), Boss (unico), Tienda, Eventos. (Retazos de cambio en contenido base: ver DD-017) | GOLDEN_RULES §10 |
| DD-013 | Mejora de cartas | 1 mejora por carta por run. En cartas duales mejora ambos lados. Hoguera (heal vs upgrade) y eventos especiales | GOLDEN_RULES §5 |
| DD-014 | Enemigos transdimensionales | 2 categorias: estandar (cambian tipo activo con el mundo) y ancla (tipo fijo, no reaccionan). La frase "tres categorias" del GDD era ruido de transcripcion | GOLDEN_RULES §6 |
| DD-016 | Meta-progresion | XP por progreso. Desbloqueos: cartas duales nuevas, Retazos nuevos, vinetas narrativas. Curva: rapido al inicio, lento despues con mayor impacto | GOLDEN_RULES §11 |

---

## Cerradas en revision post-GDD (2026-04-28)

| DD  | Tema | Resolucion |
|-----|------|------------|
| DD-018 | Multiplicador de dano enemigo SuperEficaz contra el tipo activo del jugador | **Si**: el dano se multiplica x1.5 (igual que dano del jugador). El multiplicador es configurable (constante en codigo, ajustable sin cambiar logica). Esto reabre la regla anterior de "efectividad solo al dano jugador->enemigo": ahora aplica en ambas direcciones | GOLDEN_RULES §3 |
| DD-019 | Sangrado y Virus: genericos o exclusivos del boss | **Exclusivos del Boss Acto 1** (Costura Maldita / UNIT-RB7). No son debuffs genericos del juego. Si en futuros bosses se necesita el mismo concepto, se reevalua entonces | GOLDEN_RULES §6 |
| DD-020 | Carta especial dual inicial: filtrada o pool fijo | **Filtrada** por los 2 tipos elementales que el jugador elige al inicio del run | GOLDEN_RULES §5 |
| DD-021 | Representacion del MCguffin de quests | **Diferida** al diseno de M4 (Eventos). No es bloqueante para diseno actual. Se decide cuando se ataque el sistema de eventos | — |

---

## Cerradas en diseño de M3 (2026-05-07)

| DD  | Tema | Resolucion |
|-----|------|------------|
| DD-017 | Retazos de cambio en contenido base o post-launch | **Opción C — Híbrido**: 2-3 Retazos de cambio se incluyen en el contenido base como demo de la categoría. Razón: la categoría existe (refuerza la mecánica diferenciadora del cambio de mundo desde el primer run) sin la carga de diseñar y balancear 10-15 Retazos. Si en playtest la categoría es divertida, M4/M5 amplían el pool; si no, no se invirtió tiempo de más. Aplicado en Sub-PR 3B de M3 |

---

## Postponed

### DD-015 — Narrativa y vinetas
Tema: vinetas narrativas, dialogo de los hermanos, lore. **Postponed** hasta que se aborde la capa de narrativa explicitamente. No bloqueante para gameplay base.

---

> Ultima actualizacion: 2026-05-07 (DD-017 cerrada en diseño de M3)
> Cuando una decision se cierra, su regla resultante se mueve a GOLDEN_RULES.md y se marca su entrada aqui con la referencia a la seccion correspondiente.
