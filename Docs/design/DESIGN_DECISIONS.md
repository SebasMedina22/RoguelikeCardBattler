# DESIGN DECISIONS — Decisiones Abiertas y Propuestas

> Este documento recoge las decisiones de diseno que estan EN DISCUSION.
> Nada aqui es definitivo hasta que se pruebe y se mueva a GOLDEN_RULES.md.
> Formato: Pregunta -> Opciones -> Recomendacion -> Estado

---

## DD-001: Cuando y como se cambia de mundo

### Pregunta central
El cambio de mundo es la mecanica diferenciadora. Pero: cuando el jugador decide cambiar? Que lo motiva? Actualmente hay 1 switch por combate, pero no hay un detonante claro que haga que el momento del cambio importe.

### Opciones

**A) Cambio dinamico (multiples cambios por combate)**
- El jugador puede cambiar varias veces si cumple condiciones (ej: encadenar SuperEficaces)
- Pro: combos explosivos, se siente activo y recompensa jugar bien
- Contra: puede volverse "spam" optimo, pierde peso estrategico si es muy facil
- Riesgo: si cambiar es frecuente y barato, deja de ser una decision

**B) Cambio unico e irreversible**
- 1 cambio por combate, no se puede volver
- Pro: decision de altisimo peso, define el combate entero
- Contra: puede sentirse limitante, el jugador se queda "atrapado" si cambia mal
- Riesgo: si el jugador cambia en mal momento, el combate se vuelve injusto

**C) Hibrido: 1 cambio gratis + cambios extra por Momentum (RECOMENDADO)**
- Empiezas con 1 switch gratuito por combate
- Encadenar SuperEficaces (Momentum) desbloquea switches adicionales
- Loop: buen mazo -> explotas tipos -> ganas momentum -> cambias mundo -> explotas OTROS tipos -> mas momentum
- Pro: el cambio de mundo interactua directamente con tipos y Momentum, creando un loop de recompensa
- Contra: requiere balanceo fino del costo en Momentum para un switch extra
- Esto responde "cuando cambio?" -> cambias cuando puedes encadenar

### Estado: POR DECIDIR
La implementacion actual (1 switch fijo) sirve como placeholder. Hay que prototipar la opcion C y testear si el loop se siente bien.

---

## DD-002: Que hace que el sistema de tipos sea unico (no solo Pokemon)

### Problema
6 tipos con multiplicadores 1.5x/1.0x/0.75x suena a Pokemon simplificado. Necesita un "twist" que lo haga propio.

### Lo que ya tenemos a favor
- La tabla ES ASIMETRICA: Rojo->Amarillo es SuperEficaz, pero Amarillo->Rojo es Neutro. Esto ya es diferente a Pokemon y significa que EL ORDEN IMPORTA.
- Las cartas duales significan que una carta puede tener tipo Rojo en Mundo A y tipo Morado en Mundo B. El jugador puede "elegir" con que tipo atacar cambiando de mundo.
- Combinado con el cambio de mundo, el sistema de tipos se vuelve un puzzle: en que mundo estoy? que tipo tiene el enemigo? con que lado de mis cartas duales puedo encadenar SuperEficaces?

### Preguntas abiertas

**El jugador ve el tipo del enemigo antes del combate?**
- Recomendacion: SI, en el nodo del mapa. Mostrar icono del tipo del enemigo.
- Razon: permite que la decision del mapa sea estrategica ("voy por este camino porque tengo cartas fuertes contra ese tipo")
- Opcion alternativa: solo mostrar para enemigos ya enfrentados en runs anteriores (como un bestiario que se desbloquea)

**Que pasa con mazo mono-tipo?**
- Mono-tipo es viable si el jugador elige combates donde su tipo es fuerte
- El mapa deberia forzar 1-2 combates donde no lo sea (elites, boss)
- Las cartas duales naturalmente desincentivan mono-tipo porque cada carta tiene DOS tipos
- Recomendacion: no prohibir mono-tipo, pero que el mapa y los bosses lo castiguen naturalmente

**Sistema de afinidades del heroe (del GDD original)**
- Al inicio de cada run, el jugador elige 2 atributos principales
- Cartas de esos tipos obtienen bonificaciones
- El heroe es mas vulnerable a tipos que contrarrestan sus atributos
- Estado: NO IMPLEMENTADO. Decidir si se implementa o si las cartas duales ya cubren este espacio estrategico.

### Estado: PARCIALMENTE IMPLEMENTADO
La tabla de tipos y el Momentum funcionan. Falta decidir afinidades del heroe y visibilidad de tipos en mapa.

---

## DD-003: Recompensas post-combate

### Pregunta
Cuantas cartas se ofrecen? Se puede skipear? Hay duplicados?

### Propuesta base
- 3 cartas a elegir despues de cada combate
- El jugador elige 1 o puede skipear (no tomar ninguna)
- Skip es una decision valida: a veces un mazo mas pequeno es mejor
- Al menos 1 de las 3 cartas deberia ser dual (para reforzar la mecanica central)
- Duplicados posibles pero con peso reducido
- Tambien se otorga oro

### Pool de cartas
- En Slay the Spire el pool depende del personaje
- En nuestro juego propuesta: el pool depende del PAR DE MUNDOS de la run
- Ejemplo: mundos Medieval/Cyberpunk tienen un pool, mundos Futurista/Retro otro
- Esto permite que cada combinacion de mundos se sienta como un "personaje" diferente
- Cada pool favorece builds y sinergias distintas

### Estado: POR IMPLEMENTAR
Hay issue #27 para shop minimo. Las recompensas post-combate necesitan su propio issue.

---

## DD-004: Diseno de bosses

### Pregunta
Que hace especial a un boss? Tiene fases? Interactua con el cambio de mundo?

### Ideas (no probadas)
- **Fases**: el boss cambia patron de IA al bajar de cierto % de HP
- **Interaccion con mundos**: el boss podria forzar un cambio de mundo, o bloquear el cambio temporalmente
- **Tipo variable**: el boss cambia su tipo elemental entre fases, obligando al jugador a adaptar su estrategia
- **Ataques exclusivos por mundo**: ciertos ataques del boss solo ocurren en Mundo A o B
- **Boss "ancla"**: no cambia nunca de tipo, es un check de si tu mazo tiene suficiente versatilidad

### Principio de diseno
- El boss debe ser el examen final del acto
- Debe testear si el jugador construyo un mazo equilibrado
- Debe interactuar con la mecanica de mundos duales de forma que no sea opcional

### Estado: POR DISENAR
El boss de Acto 1 existe como placeholder. El diseno de fases y mecanicas necesita su propio documento cuando se aborde.

---

## DD-005: Nodos Shop, Event y Campfire

### Pregunta
Que se hace en cada uno? Como complementan (no compiten con) el sistema de tipos y mundos?

### Propuestas

**Shop (Tienda)**
- Comprar cartas especificas (control sobre el mazo — el jugador elige exactamente que tipo quiere)
- Eliminar cartas (refinar estrategia — un mazo mas fino es mas consistente)
- Comprar reliquias (pasivas que alteran reglas)
- Posible: cartas exclusivas de tienda que no salen como recompensa
- Complementa tipos: el jugador puede comprar cartas del tipo que le falta

**Campfire (Hoguera)**
- Opcion A: Curar HP (seguridad)
- Opcion B: Mejorar una carta (subir dano, bajar costo, anadir efecto)
- Posible opcion C: Transformar el tipo de una carta (refuerza sistema de tipos)
- Decision siempre: salud inmediata vs poder a largo plazo
- Complementa mundos: mejorar una carta dual mejora AMBOS lados

**Event (Evento)**
- Encuentros narrativos con los hermanos discutiendo
- Decisiones con consecuencias mecanicas:
  - "Ganar una reliquia pero perder HP"
  - "Cambiar el tipo de una carta"
  - "Enfrentar un mini-combate opcional por recompensa rara"
  - "Sacrificar una carta para mejorar otra"
- Complementa narrativa: los hermanos proponen opciones desde sus mundos
- Complementa tipos: algunos eventos permiten manipular tipos del mazo

### Por que no compiten
- Shop da CONTROL (eliges que comprar)
- Campfire da MEJORA (fortaleces lo que tienes)
- Event da RIESGO/RECOMPENSA (apuestas por algo mejor)
- Combate da CARTAS NUEVAS + ORO (expandes el mazo)
- Cada uno llena un rol diferente en la construccion del mazo

### Estado: POR IMPLEMENTAR
Issues #27 (shop), #28 (bonfire), #29 (event) existen en Epic 4.

---

## DD-006: Progresion roguelite (meta entre runs)

### Pregunta
Que se desbloquea permanentemente? Como afecta el balance?

### Ideas del GDD original
- Nuevas cartas duales en el pool de draft
- Nuevas reliquias
- Nuevos mundos/actos
- Mejoras de inicio (boosts para la primera etapa)
- Cosmeticos y escenas narrativas

### Problemas a resolver
- Si desbloqueas cartas muy fuertes, las runs tempranas son dificiles y las tardias faciles
- Si desbloqueas demasiado rapido, el jugador se aburre
- Si desbloqueas demasiado lento, el jugador siente que perder no vale

### Principios propuestos
- Los desbloqueos deben ANADIR OPCIONES, no PODER. Cartas nuevas no son mejores, son diferentes.
- Las primeras 5-10 runs deben desbloquear contenido rapido para enganchar
- Despues, el ritmo baja y los desbloqueos son mas especializados
- Nunca desbloquear algo que haga que una estrategia sea objetivamente la mejor

### Estado: POR DISENAR
Necesita un documento propio cuando se aborde el sistema de progresion.

---

## DD-007: Atributos del heroe (sistema de debilidades)

### Del GDD original
- Al inicio de cada run, el jugador elige 2 atributos principales (de los 6 tipos)
- Cartas de esos tipos obtienen bonificaciones
- El heroe es mas vulnerable a esos tipos que lo contrarrestan

### Pregunta
Esto se implementa? O las cartas duales + el pool de mundos ya cubren el espacio de "identidad del mazo"?

### Argumentos a favor
- Anade una capa mas de decision al inicio de la run
- Cada combinacion de atributos crea un "build" diferente
- 15 combinaciones posibles (6 tipos, elegir 2) = alta rejugabilidad

### Argumentos en contra
- Ya hay mucha complejidad con mundos duales + tipos + cartas duales
- Puede confundir al jugador nuevo
- El pool de cartas por par de mundos ya cumple una funcion similar

### Posible compromiso
- No implementar al principio
- Si las cartas duales + mundos no generan suficiente variedad entre runs, agregar este sistema
- Podria ser un desbloqueo roguelite: "despues de X runs, desbloqueas el sistema de afinidades"

### Estado: POR DECIDIR

---

## DD-008: Pool de cartas por par de mundos

### Idea
En vez de un personaje (como Slay the Spire), el "personaje" es un par de mundos.

### Ejemplo
- Par Medieval/Cyberpunk: pool con cartas de espada+hacking, builds agresivos
- Par Futurista/Retro: pool con cartas de laser+vintage, builds defensivos/combo
- Cada par tiene sus propias sinergias, cartas exclusivas y cartas duales unicas

### Ventajas
- Rejugabilidad enorme: cada par se siente como un juego diferente
- Justifica narrativamente los mundos de los hermanos
- Permite balancear cada pool independientemente
- Los desbloqueos roguelite pueden anadir cartas a pools especificos

### Requisitos
- Minimo 2 pares de mundos para que funcione
- Cada par necesita ~30-40 cartas unicas para que haya variedad de builds
- El par inicial (Medieval/Cyberpunk) seria el "personaje base"

### Estado: POR DISENAR
Es una decision que afecta todo el contenido futuro. Debe decidirse antes de disenar cartas en masa.

---

> Ultima actualizacion: 2026-03-17
> Formato: cuando una decision se tome, documentar el resultado y mover las reglas resultantes a GOLDEN_RULES.md
