# GOLDEN RULES — Reglas Irrompibles del Proyecto

> Este documento define las reglas fundamentales del juego y del codigo.
> Ningun cambio, feature ni refactor puede romper estas reglas.
> Si un issue o prompt contradice algo de aqui, este documento tiene prioridad.
> Solo se agregan reglas aqui cuando estan PROBADAS y FUNCIONANDO en el build actual.

---

## 1. IDENTIDAD DEL JUEGO

### Que es
- Roguelite deckbuilder con combate por turnos 1v1
- Mecanica diferenciadora: sistema de Mundos Duales (A/B) que afecta cartas, tipos y estrategia
- Estetica handmade: carton, crayones, papel recortado, materiales escolares
- Narrativa: dos hermanos que discuten sobre que jugar y mezclan sus mundos (cyberpunk vs medieval oscuro)

### Roguelite, no Roguelike
- Las runs son permadeath (al morir, se pierde el progreso de esa run)
- Habra progresion permanente entre runs (desbloqueos de cartas, reliquias, eventos, potencialmente personajes)
- El jugador siempre debe sentir que avanza, incluso al perder

### Principio de diseno central
- El RNG determina las opciones, la habilidad determina las decisiones
- Cada run debe sentirse unica
- Perder debe sentirse justo — el jugador debe poder identificar que hizo mal

---

## 2. REGLAS DE COMBATE (IRROMPIBLES)

### Flujo de turno
1. Turno del jugador: robar cartas -> jugar cartas -> fin de turno
2. Turno del enemigo: limpiar bloqueo -> ejecutar movimiento -> planear siguiente -> verificar fin de combate
3. Este ciclo se repite hasta Victoria o Derrota
4. NUNCA alterar este orden

### Energia
- El jugador recibe energia fija al inicio de cada turno (default: 3)
- Las cartas cuestan energia para jugarse
- La energia NO se acumula entre turnos

### Momentum (One More)
- Se gana +1 Momentum cuando un ataque del jugador hace dano SuperEficaz > 0
- Momentum permite jugar la siguiente carta gratis (sin coste de energia)
- Momentum se consume al jugar una carta, no al seleccionarla
- Es la recompensa por explotar el sistema de tipos

### Bloqueo
- Se limpia al inicio de cada fase de turno
- Bloqueo del enemigo se limpia cuando empieza turno del jugador
- Bloqueo del jugador se limpia cuando empieza turno del enemigo
- El bloqueo NUNCA persiste entre turnos

### Limite de mano
- Maximo 7 cartas en mano (configurable)
- Si se intenta robar con mano llena, se descarta

### Cambio de mundo (implementacion actual)
- Limitado a 1 cambio por combate (funcional, pero las reglas definitivas estan en discusion — ver DESIGN_DECISIONS.md)
- Cambiar de mundo afecta que lado de las cartas duales esta activo
- El cambio afecta tipos, costos y efectos de las cartas duales

---

## 3. SISTEMA DE TIPOS Y EFECTIVIDAD

### Tipos elementales (nombres placeholder)
- Rojo, Amarillo, Azul, Morado, Negro, Blanco, None
- Los nombres cambiaran pero la mecanica se mantiene

### Tabla de efectividad (ASIMETRICA — implementada)

| Atacante \ Defensor | Rojo    | Amarillo    | Azul        | Morado      | Negro       | Blanco      |
|---------------------|---------|-------------|-------------|-------------|-------------|-------------|
| Rojo                | Neutro  | SuperEficaz | Poco eficaz | Neutro      | Poco eficaz | SuperEficaz |
| Amarillo            | Neutro  | Neutro      | Poco eficaz | SuperEficaz | Neutro      | Poco eficaz |
| Azul                | Poco ef.| SuperEficaz | Neutro      | Poco eficaz | SuperEficaz | Neutro      |
| Morado              | Poco ef.| Neutro      | SuperEficaz | Neutro      | Poco eficaz | SuperEficaz |
| Negro               | SuperEf.| Poco eficaz | Neutro      | SuperEficaz | Neutro      | Poco eficaz |
| Blanco              | Neutro  | SuperEficaz | Poco eficaz | Neutro      | SuperEficaz | Neutro      |

### Reglas de aplicacion
- SOLO aplica a: dano de carta del jugador vs tipo del enemigo
- NO aplica a: dano del enemigo, bloqueo, robo de cartas ni ningun otro efecto
- SuperEficaz: 1.5x dano + otorga Momentum
- Neutro: 1.0x dano
- PocoEficaz: 0.75x dano
- None vs cualquier tipo = Neutro
- La tabla es ASIMETRICA: Rojo->Amarillo es SuperEficaz, pero Amarillo->Rojo es Neutro

---

## 4. CARTAS (IMPLEMENTADO)

### Tipos de carta
- Attack, Skill, Power, Curse, Status

### Cartas simples vs duales
- Simple: una sola definicion, funciona igual en Mundo A y B
- Dual: dos definiciones (sideA, sideB). El mundo activo determina cual esta activa
- Al construir el mazo, el jugador esta construyendo DOS estrategias al mismo tiempo

### Resolucion de efectos
- Los efectos de una carta se encolan en ActionQueue en orden
- ActionQueue.ProcessAll() los ejecuta secuencialmente (deterministico)
- NUNCA cambiar el orden de resolucion
- NUNCA hacer que la resolucion dependa de callbacks de animacion

---

## 5. ENEMIGOS (IMPLEMENTADO)

### Estructura
- Cada enemigo tiene: HP, tipo elemental, patron de IA, lista de movimientos
- Cada movimiento tiene: efectos, peso (para random), tipo de intencion (Attack/Defend)

### Patrones de IA
- RandomWeighted: seleccion por peso (mas peso = mas probable)
- Sequence: cicla por movimientos en orden
- PhaseBased: declarado, no implementado aun

### Asignacion en mapa
- Los enemigos se asignan por profundidad BFS desde nodo inicio
- Seleccion por peso desde el pool de esa profundidad
- El boss se asigna por separado desde RunSession
- Misma seed = mismos enemigos (reproducible)

---

## 6. RUNS Y MAPA (IMPLEMENTADO)

### Generacion del mapa
- Basada en seed: misma seed = mismo mapa
- Seed 0 = aleatorio cada run
- Topologia de templates predefinidos (DAG valido garantizado)
- Tipos de nodo asignados por peso con minimos forzados

### Tipos de nodo
- Combat: pelea normal (funcional)
- Elite: pelea dificil con mejor recompensa (funcional)
- Boss: pelea final del acto (funcional)
- Shop: por implementar
- Event: por implementar
- Campfire: por implementar

### RunState = solo datos
- NUNCA poner logica de flujo en RunState
- HP del jugador persiste entre combates dentro de la misma run
- El mazo crece durante la run via recompensas

---

## 7. REGLAS DE CODIGO (IRROMPIBLES)

### Arquitectura
- Scene-owned controllers: cada escena tiene su controlador
- RunSession es el UNICO DontDestroyOnLoad
- RunState = datos. Controllers = flujo. UI/VFX = presentacion
- Data-driven: ScriptableObjects para cartas, enemigos, configuracion

### No tocar (core de combate)
- TurnManager: flujo de turnos, resolucion de efectividad, energia, momentum
- ActionQueue: orden de ejecucion de acciones
- PlayerCombatActor / EnemyCombatActor: mecanicas del actor

### Regla de oro de animaciones
- Las animaciones son SIEMPRE fire-and-forget
- La logica de gameplay NUNCA debe estar dentro de callbacks de animacion cosmetica
- La logica continua inmediatamente; la animacion es cosmetica
- Excepcion unica: PlayAttackOnce() del flujo original de ataque

### Regla CS0104
- Si un archivo usa System y UnityEngine, siempre escribir UnityEngine.Object

### Calidad minima
- Cero errores en consola = requisito base de todo cambio
- Comentarios de onboarding en todo codigo nuevo

---

> Ultima actualizacion: 2026-03-17
> Solo agregar reglas que esten probadas y funcionando.
> Para decisiones de diseno abiertas, ver Docs/design/DESIGN_DECISIONS.md
