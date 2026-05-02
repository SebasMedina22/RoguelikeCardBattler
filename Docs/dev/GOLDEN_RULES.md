# GOLDEN RULES — Reglas Irrompibles del Proyecto

> Este documento define las reglas fundamentales del juego y del codigo.
> Ningun cambio, feature ni refactor puede romper estas reglas.
> Si un issue o prompt contradice algo de aqui, este documento tiene prioridad.

> Estado de cada regla:
> - ✓ implementado y funcionando en el build actual
> - ⏳ cerrado por diseno (GDD v2), implementacion pendiente
>
> Para decisiones de diseno abiertas, ver Docs/design/DESIGN_DECISIONS.md

---

## 1. IDENTIDAD DEL JUEGO

### Que es
- Roguelite deckbuilder con combate por turnos 1v1
- Mecanica diferenciadora: sistema de Mundos Duales (A/B) que afecta cartas, tipos y estrategia
- Estetica handmade: carton, crayones, papel recortado, materiales escolares
- Narrativa: dos hermanos que discuten sobre que jugar y mezclan sus mundos
- Mundo A: **Medieval Oscuro**. Mundo B: **Cyberpunk**

### Roguelite, no Roguelike
- Las runs son permadeath (al morir, se pierde el progreso de esa run)
- Hay progresion permanente entre runs por XP (ver §11)
- El jugador siempre debe sentir que avanza, incluso al perder

### Principio de diseno central
- El RNG determina las opciones, la habilidad determina las decisiones
- Cada run debe sentirse unica
- Perder debe sentirse justo — el jugador debe poder identificar que hizo mal

---

## 2. REGLAS DE COMBATE (IRROMPIBLES)

### Flujo de turno (✓)
1. Turno del jugador: robar cartas -> jugar cartas -> fin de turno
2. Turno del enemigo: limpiar bloqueo -> ejecutar movimiento -> planear siguiente -> verificar fin de combate
3. Este ciclo se repite hasta Victoria o Derrota
4. NUNCA alterar este orden

### Energia (✓)
- El jugador recibe energia fija al inicio de cada turno (default: 3)
- Las cartas cuestan energia para jugarse
- La energia NO se acumula entre turnos

### Bloqueo (✓)
- Se limpia al inicio de cada fase de turno
- Bloqueo del enemigo se limpia cuando empieza turno del jugador
- Bloqueo del jugador se limpia cuando empieza turno del enemigo
- El bloqueo NUNCA persiste entre turnos

### Limite de mano (✓)
- Maximo 7 cartas en mano (configurable)
- Si se intenta robar con mano llena, se descarta

### Cambio de mundo
- ⏳ 1 cambio gratuito por combate (base, no acumulable entre combates)
- ⏳ Cambios adicionales pueden venir de:
  - Sistema de Contador de Estilo (ver §4): 5 cargas otorgan 1 cambio extra (no acumulable)
  - Cartas con efecto de cambio de mundo
  - Items (Retazos, ver §10)
- ⏳ Cambiar de mundo afecta:
  - Que lado de las cartas duales esta activo
  - El tipo activo del jugador (= tipo asignado al mundo)
  - El tipo activo de enemigos transdimensionales (ver §6)
  - NO afecta enemigos ancla (ver §6)
- ✓ Hoy implementado: 1 cambio por combate (placeholder, sera reemplazado en M2)

---

## 3. SISTEMA DE TIPOS Y EFECTIVIDAD

### Tipos elementales (✓)
- 6 tipos: Rojo, Amarillo, Azul, Morado, Negro, Blanco + None
- Los nombres son placeholder; la mecanica se mantiene

### Tipo activo del jugador (⏳ DD-002, DD-007)
- El jugador elige 2 tipos elementales al inicio del run, asignando uno a cada mundo
- El tipo activo del jugador = tipo asignado al mundo en que esta parado
- El cambio de mundo cambia instantaneamente el tipo activo del jugador
- Esto afecta:
  - Que ataques enemigos son SuperEficaces contra el (ver "reglas de aplicacion")
  - Que cartas con afinidad cambian su tipo (ver §5)

### Tabla de efectividad (ASIMETRICA — implementada) (✓)

| Atacante \ Defensor | Rojo    | Amarillo    | Azul        | Morado      | Negro       | Blanco      |
|---------------------|---------|-------------|-------------|-------------|-------------|-------------|
| Rojo                | Neutro  | SuperEficaz | Poco eficaz | Neutro      | Poco eficaz | SuperEficaz |
| Amarillo            | Neutro  | Neutro      | Poco eficaz | SuperEficaz | Neutro      | Poco eficaz |
| Azul                | Poco ef.| SuperEficaz | Neutro      | Poco eficaz | SuperEficaz | Neutro      |
| Morado              | Poco ef.| Neutro      | SuperEficaz | Neutro      | Poco eficaz | SuperEficaz |
| Negro               | SuperEf.| Poco eficaz | Neutro      | SuperEficaz | Neutro      | Poco eficaz |
| Blanco              | Neutro  | SuperEficaz | Poco eficaz | Neutro      | SuperEficaz | Neutro      |

### Reglas de aplicacion
- Multiplicadores: SuperEficaz **1.5x**, Neutro **1.0x**, PocoEficaz **0.75x**
- Multiplicadores configurables (constantes en codigo, ajustables sin cambiar logica)
- ✓ Aplica a: dano de carta del jugador vs tipo del enemigo (multiplicador + cargas de Estilo)
- ✓ Aplica a: dano del enemigo vs tipo activo del jugador — multiplicador (DD-018, Sub-PR B). Cargas de Estilo pendientes (Sub-PR C, marcado ⏳ en §4).
- NO aplica a: bloqueo, robo de cartas, ni ningun otro efecto fuera de dano
- None vs cualquier tipo = Neutro (no genera ni quita cargas)
- La tabla es ASIMETRICA: Rojo->Amarillo es SuperEficaz, pero Amarillo->Rojo es Neutro

### Cartas neutras (⏳ DD-002)
- Cartas sin afinidad de tipo (None)
- Infligen 90% del dano base (multiplicador adicional al base, configurable)
- NO generan ni quitan cargas de Contador de Estilo
- NO interactuan con debilidades ni resistencias

---

## 4. CONTADOR DE ESTILO (⏳ DD-001 — reemplaza Momentum)

> Recurso por combate. Reemplaza completamente el sistema antiguo de Momentum.

### Reglas
- El Contador de Estilo se reinicia a 0 al iniciar cada combate
- **+1 carga** cuando un ataque del jugador hace dano SuperEficaz contra el enemigo
- **-1 carga** cuando un ataque del enemigo hace dano SuperEficaz contra el tipo activo del jugador
- El contador no baja de 0
- Al alcanzar **5 cargas**:
  - Se otorga 1 cambio de mundo adicional para ese combate
  - El contador se reinicia a 0
  - El cambio adicional NO se acumula: si ya hay un cambio extra disponible y aun no se uso, alcanzar 5 otra vez no genera otro
- Las cargas no persisten entre combates

### El sistema viejo de Momentum se elimina
- Carta gratis por SuperEficaz: ELIMINADO
- No hay recompensa de energia por SuperEficaz
- La unica recompensa por SuperEficaz es la acumulacion de cargas

---

## 5. CARTAS

### Tipos de carta (✓)
- Attack, Skill, Power, Curse, Status

### Cartas simples vs duales (✓)
- Simple: una sola definicion, funciona igual en Mundo A y B
- Dual: dos definiciones (sideA, sideB). El mundo activo determina cual esta activa
- Al construir el mazo, el jugador esta construyendo DOS estrategias al mismo tiempo

### Resolucion de efectos (✓)
- Los efectos de una carta se encolan en ActionQueue en orden
- ActionQueue.ProcessAll() los ejecuta secuencialmente (deterministico)
- NUNCA cambiar el orden de resolucion
- NUNCA hacer que la resolucion dependa de callbacks de animacion

### Mazo inicial (⏳ DD-002, DD-008, DD-020)
- Al inicio de cada run el jugador elige 2 tipos elementales (uno por mundo)
- Esa eleccion define:
  - El tipo activo del jugador en cada mundo (ver §3)
  - El pool de cartas disponible durante el run (filtrado por los 2 tipos elegidos)
  - La afinidad inicial del mazo
- Mazo inicial: **10 cartas exactas**
  - 5 Strike (3 con afinidad de tipo, 2 neutras)
  - 4 Defend (2 con afinidad de tipo, 2 neutras)
  - 1 carta especial dual elegida en draft inicial
- Cartas con afinidad cambian su tipo segun el mundo activo (Mundo A = tipo 1, Mundo B = tipo 2)
- Carta especial dual: el jugador elige una combinacion entre 6 opciones (3 por mundo) al inicio del run, filtradas por los 2 tipos elegidos

### Tamano del mazo (⏳ DD-010)
- Inicio: 10 cartas exactas
- Fin de Acto 1 (objetivo): 15-20
- Fin de run (objetivo): 20-25
- Sin limite duro; el balance asume estos rangos

### Mejora de cartas (⏳ DD-013)
- Cada carta puede mejorarse 1 sola vez por run. La mejora es permanente dentro del run y no se revierte
- En cartas duales, mejorar la carta mejora AMBOS lados (sideA y sideB) simultaneamente
- Donde se mejora: Hoguera (decision exclusiva con descansar) y eventos especiales especificos
- Patrones: reducir coste, aumentar dano/bloqueo, anadir efecto secundario menor, eliminar condicion limitante
- La mejora amplifica la carta, no transforma su identidad

---

## 6. ENEMIGOS

### Estructura (✓)
- Cada enemigo tiene: HP, tipos elementales, patron de IA, lista de movimientos
- Cada movimiento tiene: efectos, peso (para random), tipo de intencion (Attack/Defend)

### Patrones de IA (✓)
- RandomWeighted: seleccion por peso (mas peso = mas probable)
- Sequence: cicla por movimientos en orden
- PhaseBased: declarado, no implementado aun (pendiente en M2)

### Categorias dimensionales (⏳ DD-014)
- **Estandar**: 1 o mas tipos elementales. El tipo activo cambia instantaneamente con el cambio de mundo del jugador. La ficha muestra ambos tipos cuando aplica
- **Ancla**: tipo elemental fijo e invariable. No reacciona al cambio de mundo. Funcion estrategica: forzar al jugador a resolver con el mundo actual o construir mazo versatil

### Bosses (⏳ DD-004)
- Cada boss tiene 2 tipos elementales (uno por mundo)
- Los tipos del boss se eligen para que: 1 sea SuperEficaz contra el jugador y 1 sea debilidad del jugador
- Cada boss tiene al menos una mecanica unica que interactua con el cambio de mundo (bloquearlo, forzarlo, invertir debilidades, etc)
- Cada boss tiene al menos 2 fases. Fase 2 obligatoria al 50% HP
- Los bosses pueden aplicar debuffs unicos vinculados a su tematica (Sangrado en Medieval, Virus en Cyberpunk del Boss Acto 1 — DD-019). **Estos debuffs son exclusivos del boss, no genericos**

### Asignacion en mapa (✓)
- Los enemigos se asignan por profundidad BFS desde nodo inicio
- Seleccion por peso desde el pool de esa profundidad
- El boss se asigna por separado desde RunSession
- Misma seed = mismos enemigos (reproducible)

---

## 7. RUNS Y MAPA

### Generacion del mapa (✓)
- Basada en seed: misma seed = mismo mapa
- Seed 0 = aleatorio cada run
- Topologia de templates predefinidos (DAG valido garantizado)
- Tipos de nodo asignados por peso con minimos forzados

### Tipos de nodo (⏳ DD-005 — 6 tipos)
- ✓ Combat: pelea normal
- ✓ Elite: pelea dificil con mejor recompensa, garantiza 1 Retazo (ver §10)
- ✓ Boss: pelea final del acto, otorga 1 Retazo unico vinculado al boss
- ⏳ Shop (Tienda): comprar cartas, Retazos, consumibles, eliminar cartas
- ⏳ Campfire (Hoguera): elegir entre descansar (recuperar HP) o mejorar 1 carta
- ⏳ Event (Evento): encuentros con decisiones; algunos son multidimensionales (el jugador elige en que mundo realizarlos)

### Estructura del mapa
- Scroll lateral (izquierda a derecha)
- Mismos nodos en ambos mundos; las recompensas y eventos especificos cambian segun mundo activo (excepto eventos multidimensionales)

### RunState = solo datos
- NUNCA poner logica de flujo en RunState
- HP del jugador persiste entre combates dentro de la misma run
- El mazo crece durante la run via recompensas

---

## 8. ECONOMIA (⏳ DD-009)

- El oro es la unica moneda. No persiste entre runs
- Recompensas: Combate **10-20**, Elite **30-50**, Boss **100**. Eventos pueden otorgar oro variable
- Usos: comprar cartas (Tienda), comprar Retazos (Tienda), eliminar cartas del mazo (Tienda), consumibles (si aplica)
- Ciertos efectos de cartas y Retazos pueden generar oro durante combate o eventos

---

## 9. ACTOS Y DIFICULTAD (⏳ DD-011)

- HP base del jugador: **70** al inicio de cada run. Sin maximo definido por diseno (lo limita la economia del run)
- 3 actos. Cada acto incrementa HP enemigo, complejidad de patrones, mecanicas activas
- **Acto 1**: enemigos con 1 tipo fijo, patrones simples (atacar, defender, cargar). Elites con 1 mecanica unica simple
- **Acto 2**: enemigos con 2 tipos simultaneos, patrones con fases por umbral de HP. Aparecen enemigos transdimensionales. Elites combinan al menos 2 mecanicas
- **Acto 3**: enemigos multifase con HP maximo. Aparecen enemigos ancla. Bosses interactuan directamente con el cambio de mundo (bloquearlo, forzarlo, alterar comportamiento)
- Elites NUNCA son enemigos normales con mas HP. Cada elite tiene al menos una mecanica exclusiva. Derrotar un elite garantiza un Retazo
- El jugador siempre puede evitar elites pero pierde la recompensa

---

## 10. RETAZOS (⏳ DD-012)

> Reliquias del juego. Nombre canonico: **Retazos**.

### Reglas
- Objetos pasivos que persisten durante toda la run
- Sin limite numerico de Retazos activos por run
- Obtencion: Elite (garantizado), Boss (Retazo unico narrativo), Tienda (compra), Eventos (variable)

### Categorias
- **Neutros**: efecto constante, independiente del mundo. Categoria base, mas comun
- **De cambio**: se activan al ejecutar un cambio de mundo (base o por cargas). Mas raros y poderosos. **Inclusion en contenido base: pendiente, ver DD-017**
- **De mundo**: se activan solo en un mundo especifico (Cyberpunk o Medieval Oscuro). Contenido posterior, NO incluidos en contenido base

### Retazos de boss
- Vinculados narrativamente al boss derrotado
- Texto narrativo asociado
- Mayor impacto por run que los Retazos comunes

### Principio
- Un Retazo nunca debe resolver un combate por si solo
- Su funcion es amplificar las decisiones del jugador, no sustituirlas

---

## 11. META-PROGRESION (⏳ DD-006 + DD-016)

- Cada run otorga XP segun progreso, independiente del resultado
- Factores que dan XP (de menor a mayor peso): iniciar run, superar acto, derrotar elite, derrotar boss de acto, completar run con victoria
- La derrota otorga XP proporcional al progreso (acto 3 > acto 1)
- Desbloqueos al alcanzar umbrales:
  - Cartas duales nuevas (al pool de draft)
  - Retazos nuevos (al pool de obtencion)
  - Vinetas narrativas (recompensa narrativa, no gameplay)
- Los desbloqueos NUNCA aumentan poder directo, solo amplian opciones
- Curva: primeras runs desbloquean rapido para enganchar, luego se espacian con mayor impacto

---

## 12. REGLAS DE CODIGO (IRROMPIBLES)

### Arquitectura
- Scene-owned controllers: cada escena tiene su controlador
- RunSession es el UNICO DontDestroyOnLoad de gameplay
- RunState = datos. Controllers = flujo. UI/VFX = presentacion
- Data-driven: ScriptableObjects para cartas, enemigos, configuracion

### No tocar (core de combate, archivos protegidos)
- TurnManager: flujo de turnos, resolucion de efectividad, energia, IA enemiga, world switch
- ActionQueue: orden de ejecucion de acciones
- PlayerCombatActor / EnemyCombatActor: mecanicas del actor
- Cualquier modificacion requiere aprobacion explicita

### Regla de oro de animaciones
- Las animaciones son SIEMPRE fire-and-forget
- La logica de gameplay NUNCA debe estar dentro de callbacks de animacion cosmetica
- La logica continua inmediatamente; la animacion es cosmetica
- Excepcion unica: PlayAttackOnce() del flujo original de ataque

### Regla CS0104
- Si un archivo usa System y UnityEngine, siempre escribir UnityEngine.Object explicito

### Calidad minima
- Cero errores en consola = requisito base de todo cambio
- Comentarios de onboarding en todo codigo nuevo (explican el porque, no el que)

### No manual editor setup
- Features nuevas auto-crean GameObjects en runtime o las instancia un scene controller existente
- Nunca requerir attach manual en inspector para probar

### No duplicar logica
- Verificar UIAnimationHelper.cs (8 metodos DOTween) y AudioManager.cs antes de crear helpers nuevos
- Verificar patrones existentes (IGameAction, EffectRef, eventos de TurnManager) antes de crear abstracciones nuevas

---

> Ultima actualizacion: 2026-04-28 (post-GDD v2)
> Solo agregar reglas que esten cerradas por diseno o probadas en codigo.
> Para decisiones de diseno abiertas, ver Docs/design/DESIGN_DECISIONS.md
