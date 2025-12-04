# MECHANIC – DUALIDAD DIMENSIONAL, TIPOS Y ONE MORE

Este documento resume la evolución del brainstorming de Fase 2 (ideas propias + propuestas de Gepeto) y la consolida en una mecánica central coherente. Es la “biblia” de identidad jugable sobre la que construiremos los siguientes sistemas.

---

## 1. Fantasía central

Un único protagonista viaja entre **dos mundos superpuestos** (Mundo A y Mundo B).  
En mitad del combate puede **romper el velo dimensional** y cambiar de mundo:

- El cambio transforma TODO su mazo al lado alterno (cartas duales A/B).
- Cambian tipos, efectos y sinergias.
- El cambio de mundo está fuertemente ligado a un sistema de **tipos y debilidades**.
- Golpear la debilidad del enemigo otorga un **“One More”** (acción extra) al estilo Persona.

En la práctica, el jugador siente que está **armando dos mazos a la vez** (uno por mundo), pero juega con un único conjunto de cartas que tiene dos caras.

---

## 2. Cambio de mundo (regla base)

### 2.1 Regla conceptual

- Por defecto: **1 cambio de mundo por combate**.
- Al cambiar de mundo:
  - Todas las cartas duales giran a su lado alterno (A ↔ B).
  - El fondo, el HUD y algunos efectos visuales comunican el nuevo mundo.
  - (En la Fase 2 temprana, el cambio será ilimitado para probar visual y técnicamente el sistema. Más adelante se aplicarán límites y costes).

### 2.2 Extensiones en la run (a futuro)

Reliquias / objetos podrán:

- Permitir **un segundo cambio** por combate a cambio de:
  - Pagar vida.
  - Perder energía.
  - Aplicarse un debuff.
- **Amortiguar penalizaciones** (ej.: recibir menos daño al cambiar).
- **Premiar el cambio** (ej.: robar +2 cartas, ganar +1 Fuerza, limpiar debuffs).

La regla central sigue siendo simple (1 cambio por defecto), pero el metajuego puede girar en torno a construir alrededor de cuándo y cómo cambias.

---

## 3. Cartas duales (draft inteligente)

En lugar de elegir una sola carta nueva, el jugador elige **una combinación A/B**:

| Opción | Lado A (Mundo A / Neon‑City) | Lado B (Mundo B / Ocean Frontier) |
| :-- | :-- | :-- |
| 1 | Ataque Tecno | Tajo Sangriento |
| 2 | Hack Psi | Maldición Bestial |
| 3 | Bio‑Shot | Fuego del Eclipse |

El jugador elige **una** de estas parejas. Eso define qué carta dual entra a su colección: una entidad con dos caras (A y B).

Beneficios:

- Cada run es más variada: las parejas posibles A/B explotan combinaciones.
- Puedes inclinar tu mazo hacia un mundo (más “cyber” o más “berserk”) según qué parejas elijas.
- Sientes que preparas **dos configuraciones de mazo** que se alternan con el cambio de mundo, sin obligarte a gestionar dos mazos separados.

> Fase 2 – Paso actual:  
> Para el primer prototipo vamos a usar **parejas A/B predefinidas** (cada carta conoce ya su otro lado).  
> Más adelante, durante el diseño del sistema de recompensas, implementaremos el draft real donde el jugador decide cómo se “arma” el lado B de cada carta.

---

## 4. Tipos y debilidades (versión inicial)

### 4.1 Tipos iniciales

Partimos de **6 tipos base**, organizados en dos tríadas (una por mundo). Los nombres exactos pueden cambiar más adelante, pero para el primer prototipo usaremos:

- Mundo A – **Neon‑City (Sci‑Fi / Cyber)**:
  - `Tech` – daño tecnológico directo (disparos, láser).
  - `Psi` – ataques mentales, control, debuffs.
  - `Bio` – toxinas, virus, efectos a lo largo del tiempo.

- Mundo B – **Ocean Frontier (aventura marina / fantasía)**:
  - `Tide` – agua, flujo, control de ritmo (robo, manipulación de mazo).
  - `Storm` – rayos, viento, golpes rápidos (crits, chain).
  - `Depth` – magia abisal / maldiciones (debuffs pesados, corrupción).

Cada carta y cada enemigo tiene al menos **un tipo principal**.

### 4.2 Matriz de debilidades (versión simple)

Buscamos algo tan claro como Agua/Fuego/Planta en Pokémon, pero adaptado. Una primera idea de triángulos (provisional):

- `Tech` fuerte contra `Bio`.
- `Bio` fuerte contra `Storm`.
- `Storm` fuerte contra `Tech`.

Y otro eje:

- `Psi` fuerte contra `Depth`.
- `Depth` fuerte contra `Tide`.
- `Tide` fuerte contra `Psi`.

> Fase 2 – Paso actual:  
> En el primer prototipo solo necesitaremos:
> - Definir el `enum ElementType` con estos 6 tipos.  
> - Asignar un tipo básico a algunas cartas y a `WeakSlime` como ejemplo.  
> - Implementar una función `IsWeakness(attackerType, defenderType)` que devuelva `true/false`, usando esta matriz simple.  
> La recompensa tipo “One More” (acción extra) podrá esperar a una sub‑fase posterior.

---

## 5. One More (acción extra)

Inspirado en Persona:

- Si golpeas al enemigo con un ataque de un tipo al que es **débil**, obtienes una recompensa inmediata:
  - +1 acción (puedes jugar otra carta).
  - O +1 energía.
  - O robar 1 carta.

Esto permite **encadenar** golpes eficaces, especialmente si combinas el cambio de mundo con tipos diferentes:

Ejemplo narrativo:

1. Estás en Mundo A (Cyber). El enemigo es débil a `Psi`.
2. Juegas una carta `Psi` → debilidad → `One More` (ganas +1 acción).
3. Usas esa acción extra para activar `SwitchWorld()` a Mundo B.
4. En Mundo B, usas una carta `Fire` que es débilidad del nuevo tipo del enemigo → `One More` de nuevo.

> Fase 2 – Paso actual:  
> En la primera iteración solo necesitaremos detectar debilidades y, como mucho, dar una recompensa muy simple (ej. +1 energía). El sistema completo de encadenar varias acciones quedará para una sub‑fase posterior.

---

## 6. Enemigos y su relación con los mundos

Cada enemigo se clasifica en tres categorías:

### 6.1 Enemigo Ancla

- No cambia de tipo aunque cambies de mundo.
- Sirve para que el jugador practique el sistema de tipos y One More.

### 6.2 Enemigo Transdimensional

- Cuando el jugador cambia de mundo, el enemigo también cambia a su “lado B”.
- Ejemplo:
  - En Mundo A es `Bio`.
  - En Mundo B se vuelve `Fire` o `Void`.
- Resultado: cambiar de mundo no es siempre bueno; puede exponer nuevas debilidades, pero también nuevas resistencias o patrones de ataque.

### 6.3 Jefes Multiforma

- Cambian de tipo por fases, no solo cuando cambias de mundo.
- Algunos ataques solo existen en un lado (A o B).
- Pueden **bloquear** tu cambio de mundo una vez por combate (mecánica narrativa).

> Fase 2 – Paso actual:  
> Empezaremos con 1–2 enemigos ancla y más adelante introduciremos enemigos transdimensionales y jefes para demostrar el potencial del sistema.

---

## 7. Riesgos de diseño y contramedidas

### 7.1 Un mundo claramente superior

Riesgo:

- Que, por ejemplo, el lado Cyber sea siempre preferible al lado Medieval.

Mitigación:

- El diseño de la matriz de tipos y las cartas debe forzar compensaciones:
  - Los tipos fuertes de un mundo no deben cubrir automáticamente todas las debilidades del otro.
  - Algunas sinergias clave solo existen en el lado B para incentivar el cambio.

### 7.2 El cambio de mundo siempre es beneficio neto

Riesgo:

- Si cambiar de mundo siempre es bueno y nunca tiene coste, el jugador cambia “porque sí” en todos los combates.

Mitigación:

- Introducir:
  - Enemigos transdimensionales que se benefician de tu cambio.
  - Efectos negativos que se disparan al cambiar (debuffs, pérdida de recursos).
  - Bonus limitados por combate (`SwitchWorld()` solo 1 vez salvo que una reliquia lo aumente).

### 7.3 Abusar del doble cambio

Riesgo:

- Si puedes cambiar adelante y atrás sin restricción, el sistema se vuelve trivial o rompe el balance.

Mitigación:

- Regla base estricta:
  - `SwitchWorld()` solo 1 vez por combate.
  - Cualquier cambio adicional requiere una reliquia / poder específico y viene con coste claro (HP, energía, cartas, debuffs).

---

## 8. Roadmap de implementación (alto nivel)

Orden recomendado de trabajo:

1. **Prototipo de mundo dual**:
   - Estado de mundo en `TurnManager` (WorldSide A/B).
   - Botón de prueba “Change World” que alterna entre A y B sin límite (para testear visualmente).
   - Feedback visual claro: cambios de color/fondo y etiqueta “World: A/B”.

2. **Cartas duales básicas**:
   - Extender el modelo de datos para soportar lado A/B.
   - Definir algunas parejas A/B fijas en ScriptableObjects.
   - Al cambiar de mundo, las cartas de la mano se actualizan a su lado correspondiente.

3. **Tipos y debilidades base**:
   - `enum ElementType` con 6 tipos.
   - Asignar tipos sencillos a cartas y enemigos.
   - Implementar `IsWeakness(attackerType, defenderType)` sin aún dar recompensas complejas.

4. **Primer prototipo de One More**:
   - Si `IsWeakness` es `true`, otorgar +1 energía o permitir una acción extra.
   - Ajustar UI para mostrar claramente cuándo se ha activado.

5. **Extender a enemigos transdimensionales y reliquias**:
   - Enemigos que cambian de tipo al cambiar de mundo.
   - Primeras reliquias que modifican la regla de cambio de mundo.

Este roadmap se traduce en issues concretos que iremos creando y pasando a CODEX HIGH para su implementación.



