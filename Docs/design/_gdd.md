Proyecto C
Diseñado por TrickShotStudios Para Pc Fecha de salida esperada: 15/06/202?
Refinación de mecánicas claves
En este documento se encuentra el contenido de la refinación de mecánicas claves para el loop jugable del videojuego.
DESCRIPCIÓN CORTA DEL VIDEOJUEGO :
Deckbuilder roguelite donde el jugador manipula dos mundos simultáneamente, explotando debilidades de tipos mediante un sistema de cambio dimensional + One More (Nombre provisional) DD-OO1 - Sistema de cambio de mundos Un cambio GRATIS por combate
Posibilidad de más cambios mediante cartas, items y desencadenar combos.
El sistema de combos funciona con un contador de debilidades. Dependiendo del tipo que lleves en ese momento.
Al salir de combate, el mapa tendrá los mismos nodos en ambos mundos, pero todas las mejoras, y eventos serán específicos de ese mundo, exceptuando los eventos multidimensionales.
Sistema de combos El juego cuenta con un sistema de Contador de Estilo, que recompensa al jugador por explotar correctamente las debilidades de los enemigos. Cada vez que el jugador golpea a un enemigo con un ataque súper eficaz, se acumula 1 carga de estilo. Al alcanzar un total de 5 cargas, el jugador obtiene automáticamente un cambio de mundo gratuito, que puede activar en cualquier momento durante ese combate. Este cambio de mundo funciona como un recurso adicional independiente del cambio base del combate. Sin embargo, no puede acumularse: si el jugador ya tiene un cambio gratuito disponible, seguir generando cargas no otorgará uno adicional.
El contador de estilo también introduce riesgo. Cada vez que el jugador recibe daño de un ataque que explota una de sus debilidades de tipo, pierde 1 carga de estilo, incentivando tanto la ofensiva inteligente como la defensa estratégica.
DD-002 - Sistema de tipos
Sistema de Tipos Iniciales y Mazo Base
Al inicio de cada partida, el jugador debe seleccionar dos tipos elementales, asignando uno a cada mundo de la run:
● Mundo A → Tipo 1 (ej. Rojo)
● Mundo B → Tipo 2 (ej. Amarillo)
Esta elección define tanto el pool de cartas disponible durante la partida como la afinidad inicial del mazo.
Mazo Inicial
El jugador comienza con un mazo de 10 cartas:
● 5 cartas de ataque (Strike)
● 4 cartas de defensa (Defend)
● 1 carta especial
De estas cartas:
● 3 Strikes y 2 Defends tendrán afinidad de tipo
● Las cartas restantes serán neutras
Interacción con el Cambio de Mundo
Las cartas con afinidad de tipo no son fijas, sino que cambian su tipo según el mundo activo:
● En Mundo A, adoptan el primer tipo elegido
● En Mundo B, adoptan el segundo tipo elegido
Esto permite que una misma carta pueda adaptarse estratégicamente para explotar distintas debilidades dependiendo del momento del combate.
Cartas Neutras
Las cartas neutras:
● Infligen 90% del daño base
● No generan puntos de estilo
● No interactúan con debilidades ni resistencias
Cumplen una función de estabilidad dentro del mazo, pero son menos eficientes en términos ofensivos y de generación de combos.
Carta Especial Inicial
El jugador comienza con una carta especial dual, que se construye al inicio de la partida:
● Se presentan 6 opciones en total
○ 3 correspondientes al Mundo A
○ 3 correspondientes al Mundo B
El jugador selecciona una combinación, forjando una carta que integra ambos mundos y establece la base de su estrategia inicial.
Este sistema asegura que, desde el primer combate, el jugador tenga acceso a herramientas que interactúan con los tipos elementales, al mismo tiempo que introduce decisiones estratégicas claras entre consistencia (cartas neutras) y potencial de combos (cartas con afinidad).
Sistema de Tipos Asimétrico
El juego utiliza un sistema de tipos elementales asimétrico, lo que significa que las relaciones de ventaja y desventaja entre tipos no son necesariamente recíprocas. A diferencia de los sistemas tradicionales donde si un tipo es fuerte contra otro, este último suele ser débil en la misma proporción, aquí las interacciones están diseñadas para ser más dinámicas y menos predecibles.
Cada tipo puede tener tres tipos de interacción frente a otro:
● Súper eficaz (1.5x daño)
● Neutral (1.0x daño)
● Poco eficaz (0.75x daño)
Por ejemplo, el tipo Rojo puede ser súper eficaz contra Amarillo, pero Amarillo no necesariamente será débil contra Rojo, pudiendo ser neutral o incluso tener otra interacción distinta con otros tipos. Esto rompe la lógica circular clásica y obliga al jugador a aprender y adaptarse, en lugar de memorizar relaciones simples.
Este sistema cobra aún más profundidad al combinarse con las cartas duales y el cambio de mundo, ya que una misma carta puede tener diferentes tipos según el mundo activo, permitiendo al jugador manipular estratégicamente sus opciones ofensivas para explotar debilidades específicas en el momento adecuado.
En conjunto, la asimetría en la tabla de tipos fomenta la experimentación, evita estrategias dominantes simples y convierte cada combate en un ejercicio de lectura, planificación y adaptación constante. (DD-003 notas para merlo: Esta se vio contestada en el apartado DD-002) 
DD-004 Diseño de bosses Los Bosses tendrá habilidades únicas, y fases. En su mayoría relacionadas con las mecánicas de tipo y cambio de mundo. Cada uno tendrá sus 2 tipos, dependiendo del mundo y cada boss podrá tener cada uno de los 6 tipos, y tenderá a escogerse de manera aleotaria, uno al que seas supereficaz y el otro debil. Ejemplo claves de mecanicas:
Bloquea cambio de mundo
Fuerza cambio
Invierte debilidades
Ejemplo de boss: Mundo A (Medieval): Costura maldita - Mundo B (Futurista) UNIT-RB7 A: Medieval, un peluche de paja en forma de conejo, poseído por una maldición, en su pecho se ve como sale una mano oscura con la cual ataca (inspiración en mimikyu) y en el mundo B: Futurista, es un robot de juguete infectado por un virus de una IA mutada
MECÁNICA PRINCIPAL: Desfase Dimensional”
El boss entra en un estado cargado:
● Carga durante 1 turno
● Luego activa: “Cada 3 cartas jugadas en un solo turno → cambio automático de mundo”
● Dura 3 turnos
● Se pone un contador de cartas que le dice al jugador cuando se va a activar
El cambio es automático y arruina la cadena de combos del jugador.
Otros ataques:
“Costura Viva / Código Reescrito”+
Medieval:
● La mano oscura sale del pecho y:
○ Aplica debuff (maldición / sangrado) El sangrado: Te quita una cantidad de vida cada vez que lanzas un ataque
Futurista:
● Se convierte en:
○ brazo mecánico glitch
○ aplica “Virus” (cartas fallan / cambian) Virus: Tus cartas de defensa bajan su efectividad a un 80%
FASE 2 (OBLIGATORIA)
Cuando baja a 50% HP:
● Ya no espera 3 cartas
● Cambia mundo cada 2 cartas
DD-005 Sistema de nodos El jugador atravesará un mapa de scroll lateral (izquierda a derecha) que se creará a través de un sistema de semillas, y podrá encontrar 6 tipos de NODOS:
- Peleas: Luchas contra enemigos normales
- Peleas Elite: Luchas contra minijefes
- Peleas jefe
- Tiendas
- Hogueras: Para curarse, y mejorar cartas
- Eventos
El mapa se creará a través de un sistema de semillas. Eventos: En los eventos habrá una gran variedad, y se podrán encontrar: Reliquias, mejoras, maldiciones, cartas, y quests. Además, habrán eventos especiales que te dejarán elegir en que mundo quieres hacerlos Ejemplo de Evento tipo quest multidimensional: Al entrar a la quest decides en que mundo quieres hacerlo. La quests es exactamente la misma, solo cambia; el enunciado y la recompensa. Enunciado mundo A (medieval)
Te encuentras un hombre moribundo, que dice ser parte de una legión de magos. Te pide llevar un objeto místico a determinado sitio (MCguffin) y, si lo haces, se te dará una recompensa. Enunciado mundo B (Futurista) Te encuentras un Robot con todas sus piezas rotas y haciendo cortocircuito, te dice que es parte de una facción revolucionaria y tiene que llevar un disco duro con información importante Mundo A (medieval) Aceptar: Se te da el objeto que te da 2 más de oro en cada combate, y tienes que llevarlo a un punto resaltado en el mapa. Robar: El objeto se destruye y le robas 100 de oro al hombre.
Mundo B (Futurista) Aceptar: Se te da el objeto, el cual te da 1 de escudo adicional cada que juegues una carta de escudo, y tienes que llevarlo a un punto resaltado en el mapa.
Robar: El objeto se destruye y le robas 100 de oro al hombre. DD-006 progresión Roguelite El juego contará con un sistema de progresión roguelite, en el cual el jugador irá desbloqueando cartas, fragmentos (reliquias) y eventos a medida que complete runs.
Estos desbloqueos no aumentan directamente el poder del jugador, sino que amplían las opciones disponibles, enriqueciendo la variedad de estrategias, builds y situaciones posibles en futuras partidas. DD-009 Economía El oro es la principal moneda del juego y funciona como un recurso limitado que el jugador obtiene y gestiona únicamente durante cada run.
Se consigue principalmente al:
● Completar combates
● Derrotar enemigos élite y jefes (mayor cantidad)
● Participar en eventos
● Activar efectos de ciertas cartas o fragmentos
El oro se utiliza en nodos de tienda, donde el jugador puede:
● Comprar cartas
● Adquirir fragmentos (reliquias)
● Obtener consumibles (si aplica)
● Eliminar cartas del mazo
Combate - 10-20 Oro Elite - 30 - 50 Boss - 100
El oro no se conserva entre partidas, asegurando que cada run sea una experiencia independiente y balanceada, centrada en la toma de decisiones y la adaptación, en lugar de la acumulación de recursos a largo plazo.
DD - 010 Tamaño del mazo
Tamaño típico del mazo al final
● Inicio: 10–12 cartas
● Acto 1: 15–20
● Ideal final: 20–25
Tamaño típico del mazo al final
● 20 a 30 cartas - rango más común
● 15–20 cartas - mazos muy optimizados (alta consistencia)
● 30–40 cartas - más raros, suelen ser builds específicos
DD - 011 Dificultad
La dificultad de Proyecto C escala a lo largo de los 3 actos de cada run siguiendo el modelo de Slay the Spire: los enemigos no solo aumentan sus estadísticas base, sino que incrementan progresivamente la complejidad de sus patrones, la cantidad de mecánicas activas y su interacción con los sistemas de mundo y tipo elemental del jugador.
HP base del jugador El héroe inicia cada run con 70 HP. Este valor puede aumentarse durante la run mediante reliquias, eventos o mejoras de inicio desbloqueadas en el meta. No hay un máximo definido por diseño; el techo real lo pone la economía de cada run.
Estructura de escalado por acto
Acto 1 — Introducción al sistema Los enemigos tienen 1 tipo elemental fijo y patrones de ataque simples y predecibles (atacar, defender, cargar). El HP enemigo es bajo. Este acto funciona como zona de aprendizaje: el jugador practica One More, el cambio de mundo y la lectura de intenciones sin presión severa. Los elites de acto 1 tienen una mecánica única pero sencilla, suficiente para distinguirlos de los enemigos normales sin abrumar al jugador.
Acto 2 — Presión real Los enemigos aumentan HP y comienzan a presentar 2 tipos elementales simultáneos, lo que complica la explotación de debilidades. Los patrones incorporan fases simples: un enemigo puede cambiar su comportamiento al llegar a cierto umbral de HP. Aparecen los primeros enemigos transdimensionales, que cambian su tipo activo cuando el jugador cambia de mundo. Los elites de acto 2 combinan al menos dos mecánicas y representan el primer desafío de decisión real: enfrentarlos tiene riesgo genuino.
Acto 3 — Dominio o muerte Los enemigos alcanzan su HP máximo y sus patrones son multifase. Aparecen los primeros enemigos ancla (inmunes al cambio dimensional) y combinaciones de tipos que crean conflictos directos con los atributos del héroe. Los jefes de acto 3 tienen fases que interactúan directamente con el cambio de mundo: pueden bloquear temporalmente el cambio, forzar al jugador a un mundo específico, o alterar su comportamiento drásticamente según el mundo activo. El margen de error es mínimo.
Elites: Los elites no son enemigos normales con más HP. Cada elite posee al menos una mecánica exclusiva que no aparece en los enemigos comunes de su acto. Derrotar un elite garantiza una reliquia como recompensa, lo que justifica el riesgo de enfrentarlos. El jugador
nunca está obligado a pelear un elite; siempre existe una ruta alternativa en el mapa, pero evitarlos tiene un coste de progresión real.
DD-012 — Retazos
Los Retazos son objetos pasivos que el héroe acumula durante la run y permanecen activos hasta el final de ella. Representan físicamente fragmentos del mundo imaginario de los hermanos: pedazos de cartón, recortes de revista, figuras rotas, trozos de cinta. Cada Retazo otorga un efecto permanente que modifica pasivamente el comportamiento del héroe, sus cartas o los combates.
Obtención Los Retazos se obtienen de cuatro fuentes:
● Elites: garantizan un Retazo al ser derrotados. Es la recompensa principal que justifica el riesgo de enfrentarlos.
● Bosses: otorgan un Retazo de mayor poder al final de cada acto. Estos Retazos de boss son únicos y temáticamente ligados al jefe derrotado.
● Tienda: el jugador puede comprar Retazos con monedas. La tienda ofrece una selección aleatoria por run.
● Eventos: algunos nodos de evento otorgan un Retazo como resultado de una decisión, con o sin condición asociada.
Cantidad activa No existe un límite numérico de Retazos activos. El jugador puede acumular todos los que obtenga durante la run. El límite real lo imponen la economía del juego y la frecuencia de obtención por run.
Categorías
Retazos neutros: funcionan de forma constante independientemente del mundo activo. Son la categoría base y la más común. Ejemplos de efecto: ganar 1 bloqueo al inicio de cada combate, robar 1 carta adicional al comienzo del turno, recuperar 3 HP al derrotar un enemigo.
Retazos de cambio: se activan en el momento exacto en que el jugador ejecuta un cambio de mundo, ya sea el cambio base o uno obtenido mediante el Contador de Estilo. Son la categoría más singular del juego y la que mejor representa la dualidad dimensional. Son raros y más poderosos que los neutros. Estado actual: pendiente de decisión de implementación para contenido base.
Retazos de mundo: se activan únicamente cuando el jugador se encuentra en un mundo específico (Cyberpunk o Medieval Oscuro). Definidos como contenido posterior, no incluidos en el contenido base.
Interacción con sistemas existentes Los Retazos son la capa de personalización pasiva de la run. Funcionan en conjunto con el mazo del jugador y el Contador de Estilo, pero no los reemplazan. Un Retazo nunca debe resolver un combate por sí solo; su función es amplificar las decisiones del jugador, no sustituirlas.
Retazos de boss Al derrotar al jefe de cada acto, el jugador recibe un Retazo especial vinculado narrativamente a ese jefe. Estos Retazos tienen efectos más radicales que los comunes y una pequeña viñeta de texto que los contextualiza dentro de la historia de los hermanos. Son los Retazos de mayor impacto por run y condicionan significativamente las estrategias disponibles en los actos siguientes.
DD-013 — Mejora de cartas
Las cartas del mazo pueden mejorarse una vez durante la run, pasando de su versión base a su versión mejorada. Esta mejora es permanente dentro de la run y no puede revertirse.
Dualidad de la mejora Dado que cada carta existe en dos versiones simultáneas (una por mundo), mejorar una carta mejora automáticamente ambos lados. El jugador nunca mejora solo la versión Cyberpunk o solo la versión Medieval Oscuro de una carta: la mejora es siempre total. Esto garantiza que la decisión de mejorar una carta tenga peso real en ambos mundos y simplifica la carga cognitiva del jugador.
Dónde se mejoran
Hoguera: es el nodo principal de mejora. Al visitar una hoguera, el jugador puede elegir entre descansar (recuperar HP) o mejorar una carta de su mazo. Es la misma decisión de Slay the Spire: salud vs. poder. Solo se puede mejorar una carta por visita a hoguera.
Eventos especiales: ciertos nodos de evento ofrecen la mejora de una carta como resultado de una decisión narrativa. Estos eventos son la excepción, no la regla, y su condición de obtención varía caso a caso.
Lo que cambia al mejorar La mejora no transforma la identidad de la carta sino que amplifica su efecto. Los patrones generales son: reducción de coste de energía, aumento de daño o bloqueo, adición de un efecto secundario menor, o eliminación de una condición limitante. Cada carta tiene su propia mejora definida en su ficha individual.
DD-014 — Enemigos transdimensionales
Todo enemigo en Proyecto C tiene uno o más tipos elementales que determinan sus debilidades y resistencias. Sin embargo, no todos los enemigos se relacionan igual con el sistema de cambio de mundo. Existen tres categorías de comportamiento dimensional:
Enemigos estándar: Cada enemigo estándar tiene un tipo elemental fijo asociado al mundo en el que aparece. El enemigo cambia su tipo activo instantáneamente. Esto significa que un cambio de mundo puede convertir a un enemigo de resistente a vulnerable, o viceversa, en el mismo turno. El jugador siempre puede ver ambos tipos del enemigo en su ficha, anticipando el efecto del cambio antes de ejecutarlo.
Enemigos ancla: Los enemigos ancla existen fuera de la lógica dimensional. Su tipo elemental es fijo e invariable sin importar en qué mundo se encuentre el jugador ni cuántas veces cambie. No reaccionan al cambio de mundo de ninguna forma. Su función estratégica es actuar como contrapunto al cambio de mundo: cuando el jugador enfrenta un ancla, el cambio de mundo no altera la ecuación ofensiva contra ese enemigo, forzando al jugador a resolverlo con lo que tiene en el mundo actual o a construir su mazo considerando tipos que
DD-016 — Progresión meta y desbloqueos
El sistema de progresión meta de Proyecto C funciona mediante experiencia acumulada entre runs, siguiendo el modelo de Slay the Spire. Cada run otorga experiencia independientemente de su resultado, y al alcanzar ciertos umbrales se desbloquea contenido nuevo de forma permanente que expande el pool de cartas duales, Retazos y otras opciones disponibles en futuras runs.
Experiencia y progreso: Al finalizar una run, el jugador recibe experiencia basada en qué tan lejos llegó. Los factores que otorgan experiencia son los siguientes, de menor a mayor peso: iniciar una run, superar cada acto, derrotar elites, derrotar bosses de acto y completar la run con victoria. La derrota siempre otorga experiencia proporcional al progreso alcanzado: morir en acto 3 otorga más que morir en acto 1. Ninguna run es tiempo perdido.
Qué se desbloquea El contenido que se va abriendo con la progresión meta es de dos tipos principales:
Cartas duales: nuevas cartas se agregan al pool de draft, ampliando las combinaciones posibles en futuras runs. Los primeros desbloqueos son cartas base accesibles y fáciles de integrar. A medida que avanza la progresión, se desbloquean cartas más radicales con efectos que cambian significativamente la forma de construir el mazo.
Retazos: nuevos Retazos se agregan al pool general de obtención en run. Al igual que las cartas, los primeros son simples y los posteriores más complejos e interactivos con los sistemas del juego.
Curva de desbloqueo: La curva está diseñada para que las primeras runs siempre entreguen algo nuevo, generando sensación constante de progreso. A medida que el jugador avanza, los desbloqueos se espacian pero aumentan en impacto. El contenido final de la curva son cartas y Retazos que habilitan estrategias completamente distintas a las del contenido base, recompensando al jugador veterano con nuevas formas de jugar.
Viñetas narrativas: Ciertos hitos de progresión, como derrotar un boss por primera vez o completar la primera run, desbloquean viñetas cortas sobre la historia de los hermanos. Estas viñetas no son contenido de gameplay sino recompensa narrativa que contextualiza el mundo y los personajes a medida que el jugador avanza.funcionen en ambos mundos simultáneamente.
