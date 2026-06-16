# Spec — Tinte por tipo elemental en selectores de Hoguera y Tienda (pulido pre-M4, #104)

> **ESTADO: IMPLEMENTADO — PR #107 (2026-06-09).** (`#104` es el issue de backlog de origen.)

## Origen
Backlog #104 / pedido directo de Sebastián. Pulido pre-M4. Continúa la línea de
trabajo de la auditoría de arte C8 (tinte por tipo, `ElementTypeColors`) que ya
se aplicó en combate (`CardHandView`) y NewRun, pero quedó sin aplicar en los
dos selectores de carta de la meta-capa: Hoguera (mejorar) y Tienda
(stock de cartas + panel "eliminar carta").

## Objetivo
En un juego donde el tipo elemental es central, hoy estos dos selectores listan
las cartas como texto plano sin indicar tipo/color. Esta feature pinta el
nombre de cada carta con un prefijo `[Tipo]` coloreado (rich text), igual que en
combate, para que el jugador decida qué mejorar / comprar / eliminar viendo el
tipo de un vistazo. Es puramente cosmético: no toca gameplay ni datos.

## Comportamiento esperado
- **Hoguera (panel mejorar carta):** cada botón de carta muestra el prefijo de
  tipo coloreado antes del nombre. En cartas duales muestra AMBOS lados con su
  tipo y color propios.
- **Tienda — stock:** los ítems de tipo carta muestran el prefijo de tipo
  coloreado antes del título. Retazos y el servicio "Eliminar carta" no llevan
  prefijo (no tienen tipo). Cartas duales en stock muestran ambos tipos.
- **Tienda — panel "eliminar carta":** igual que Hoguera; cada carta del mazo
  con su prefijo de tipo, duales con ambos lados.
- El color del prefijo es el mismo que en combate (`ElementTypeColors.ReadableOnDark`,
  legible sobre fondo oscuro). Los botones de ambas pantallas tienen fondo
  oscuro (`~0.12, 0.10, 0.08`), así que la legibilidad ya está cubierta por ese
  helper.

## Sistemas afectados
- Combate (helper compartido): `ElementTypeColors` gana un método nuevo `TypePrefix`.
- UI Run: `CampfireNodeController` y `ShopNodeController` (solo render, no lógica).
- Gameplay/RunState/SOs: **sin cambios** (no se tocan datos).

## Archivos a crear
- `Assets/Tests/EditMode/ElementTypeColorsTests.cs` — tests EditMode del nuevo
  helper `TypePrefix` (puro/estático, sin UI).

## Archivos a modificar
- `Assets/Scripts/Gameplay/Combat/ElementTypeColors.cs` — agregar
  `TypePrefix(ElementType)`.
- `Assets/Scripts/Run/Campfire/CampfireNodeController.cs` — `BuildCardSelectLabel`
  (~L319): inyectar el prefijo de tipo por lado.
- `Assets/Scripts/Run/Shop/ShopNodeController.cs` — `CreateItemButton` (~L385):
  prefijo en el render del stock; `BuildCardSelectLabel` (~L490): prefijo por
  lado en el panel "eliminar carta".
- `Assets/Scripts/Gameplay/Combat/CardHandView.cs` (`BuildCardLabel`, L394-405) —
  **[PROPUESTA] recomendada**: refactor para consumir `TypePrefix` y eliminar la
  duplicación (el patrón hoy vive inline acá). Behavior-preserving. Ver Contratos.

## Archivos protegidos involucrados
- [x] Ninguno. `ElementTypeColors.cs`, `CardHandView.cs`, `CampfireNodeController.cs`
  y `ShopNodeController.cs` NO son archivos protegidos. (Los protegidos son
  `TurnManager.cs`, `ActionQueue.cs`, `PlayerCombatActor.cs` y no se tocan.)

## Contratos

### API pública nueva — `ElementTypeColors.TypePrefix`
```csharp
/// <summary>
/// Prefijo rich-text "[Tipo]" coloreado con el color legible del tipo sobre
/// fondo oscuro (ReadableOnDark). Fuente única de verdad del patrón que antes
/// vivía inline en CardHandView.BuildCardLabel. Devuelve string.Empty para
/// ElementType.None (sin prefijo). NO incluye espacio final ni separador — el
/// caller decide el formato.
/// </summary>
public static string TypePrefix(ElementType type)
{
    if (type == ElementType.None) return string.Empty;
    string hex = ColorUtility.ToHtmlStringRGB(ReadableOnDark(type));
    return $"<color=#{hex}>[{type}]</color>";
}
```
- **Entrada:** un `ElementType`.
- **Salida:** `""` si `None`; si no, `<color=#RRGGBB>[Tipo]</color>` (sin espacio final).
- **Side effects:** ninguno (función pura).
- **Decisión de formato:** sin espacio final. El caller añade el separador (espacio)
  solo cuando el prefijo no está vacío, así `None` no deja un espacio colgante.

### Composición de etiqueta de carta (Hoguera y Tienda "eliminar carta")
Patrón idéntico en ambos `BuildCardSelectLabel` (ambos métodos son `static` y ya
importan `RoguelikeCardBattler.Gameplay.Combat`, así que `ElementType` /
`ElementTypeColors` están disponibles):

```csharp
// helper local (lambda/función local) dentro del método o privado estático:
static string Token(CardDefinition c)
{
    if (c == null) return "?";
    string p = ElementTypeColors.TypePrefix(c.ElementType);
    return string.IsNullOrEmpty(p) ? c.CardName : $"{p} {c.CardName}";
}
```
- **Single:** `Token(entry.SingleCard)` (fallback `"Carta"` si `SingleCard == null`).
- **Dual:** mostrar AMBOS lados (decisión cerrada 1).
  - Si `nameA == nameB && typeA == typeB` → un solo token `Token(SideA)` (colapso
    solo cuando nombre Y tipo coinciden; antes colapsaba solo por nombre).
  - Si no → `$"{Token(SideA)} / {Token(SideB)}"`.
- Conserva los fallbacks existentes (`"Carta"`, `"?"`).

### Composición del stock de la Tienda (`CreateItemButton`, render — decisión cerrada 2)
El tinte va en el RENDER, NO en `BuildStock` (helper puro testeado por igualdad
de `Title`). En `CreateItemButton`, antes de armar `label`, derivar el prefijo
desde `item.CardPayload` (reutilizando el helper existente `SideType(CardDefinition)`,
null-safe → `None`):

```csharp
string typeTint = string.Empty;
if (item.Kind == ShopItemKind.Card && item.CardPayload != null)
{
    CardDeckEntry e = item.CardPayload;
    if (e.DualCard != null)
    {
        ElementType ta = SideType(e.DualCard.SideA);
        ElementType tb = SideType(e.DualCard.SideB);
        typeTint = ta == tb
            ? ElementTypeColors.TypePrefix(ta)
            : $"{ElementTypeColors.TypePrefix(ta)}{ElementTypeColors.TypePrefix(tb)}";
    }
    else if (e.SingleCard != null)
    {
        typeTint = ElementTypeColors.TypePrefix(e.SingleCard.ElementType);
    }
    if (!string.IsNullOrEmpty(typeTint)) typeTint += " ";
}
// title coloreado solo en el render; item.Title NO se muta:
string titled = $"{typeTint}{item.Title}";
string label = item.Purchased
    ? $"{titled}  (comprado)"
    : $"{titled} — {item.Price} oro\n<{item.Description}>";
```
- Solo `ShopItemKind.Card` con `CardPayload != null` lleva prefijo. `Relic` y
  `RemoveCard` quedan sin prefijo (no tienen tipo).
- El stock muestra solo el nombre del representante (`item.Title`), pero para
  duales se anteponen AMBOS tokens de tipo (cumple decisión 1): el color
  transmite la dualidad aunque solo se vea un nombre.
- `item.Title` NO se modifica (sigue siendo el string crudo que arma `BuildStock`);
  el rich text vive únicamente en la variable `label` del render → `ShopTests`
  intactos.

### Eventos
Ninguno nuevo.

## Reuso
- `ElementTypeColors.ReadableOnDark` / `For` — paleta existente (C8). No se crea
  paleta nueva.
- `SideType(CardDefinition)` — helper estático ya existente en `ShopNodeController`
  (null-safe). Reusar para el stock.
- Patrón rich-text `<color=#hex>[Tipo]</color>` — se centraliza en `TypePrefix`;
  `CardHandView` pasa a consumirlo (refactor recomendado, behavior-preserving):
  ```csharp
  // CardHandView.BuildCardLabel, reemplaza L397-402:
  string typePrefix = ElementTypeColors.TypePrefix(activeCard.ElementType);
  if (!string.IsNullOrEmpty(typePrefix)) typePrefix += " ";
  ```
  Resultado idéntico al actual (mismo color, mismo `[Tipo] ` con espacio).
- Unity legacy `Text` tiene `supportRichText = true` por defecto en GameObjects
  creados por código (igual que `CardHandView` ya hace). No requiere setear nada.

## Casos de prueba (EditMode)
En `ElementTypeColorsTests.cs` (el helper es puro y estático):
1. `TypePrefix(ElementType.None)` devuelve `string.Empty`.
2. `TypePrefix(ElementType.Rojo)` contiene el literal `[Rojo]` y abre con
   `<color=#` y cierra con `</color>`.
3. El hex embebido coincide con `ColorUtility.ToHtmlStringRGB(ElementTypeColors.ReadableOnDark(type))`
   para al menos dos tipos (p.ej. `Rojo` y `Negro` — Negro ejercita el
   levantado de luminancia de `ReadableOnDark`).
4. (Opcional) Itera todos los `ElementType` != `None` y verifica que cada uno
   produce un prefijo no vacío con su nombre entre corchetes.

No se agregan tests de UI (composición en render). `ShopTests` y `CampfireTests`
existentes deben seguir en verde sin cambios (no se toca `BuildStock` ni la
lógica de mejora/compra).

## Validación manual (RunScene / NewRunScene → nodos Hoguera y Tienda)
1. Entrar a un nodo Hoguera con un mazo que incluya al menos una carta single de
   tipo conocido y una dual con tipos distintos por lado.
   - Esperado: cada botón muestra `[Tipo]` coloreado antes del nombre; la dual
     muestra ambos lados con sus dos colores (`[TipoA] NombreA / [TipoB] NombreB`).
2. Entrar a un nodo Tienda.
   - Stock: las cartas muestran prefijo de tipo coloreado; Retazos y "Eliminar
     carta" sin prefijo. Una carta dual en stock muestra ambos tokens de tipo.
   - Abrir "Eliminar carta": el panel lista el mazo con prefijos de tipo, duales
     con ambos lados.
3. Verificar que el Negro (tipo oscuro) se lee correctamente sobre el fondo
   oscuro del botón (el levantado de `ReadableOnDark` debe evitar que se pierda).
4. Combate (regresión del refactor de `CardHandView`): la mano sigue mostrando el
   prefijo de tipo exactamente como antes.

> Nota Unity-MCP: el Game View no repinta vía CLI sin foco. Validar la
> composición de strings por diagnóstico de código / tests; la verificación
> visual final la hace Sebastián en Play con foco en el editor.

## Decisiones cerradas
1. **Duales muestran AMBOS tipos** (lado A y B), no solo el representante.
2. **El tinte del stock va en el RENDER** (`CreateItemButton`), NO en `BuildStock`
   (helper puro testeado por igualdad de `Title`; meter rich text ahí rompe `ShopTests`).
3. **Helper compartido nuevo** `ElementTypeColors.TypePrefix(ElementType)`, reusado
   en los tres sitios (Hoguera, stock Tienda, panel eliminar) + refactor de
   `CardHandView` para consumirlo.
4. `TypePrefix` devuelve el token SIN espacio final; el caller añade el separador
   solo si el prefijo no está vacío (evita espacio colgante en `None`).
5. Para duales, el colapso a un solo token ocurre solo si coinciden nombre Y tipo
   de ambos lados.

## Decisiones abiertas
Ninguna. Spec cerrado.

> Nota menor (fuera de scope, no bloqueante): en el stock, los botones no
> interactuables/comprados grisan el texto base, pero el prefijo coloreado
> conserva su color (igual que en `CardHandView`, que tampoco atenúa cartas no
> jugables). Si más adelante se quiere atenuar el prefijo en estados
> deshabilitados, existe `ElementTypeColors.Dim`. NO se implementa ahora.

## Alternativas consideradas
- **Tintar en `BuildStock`** (poner el rich text en `Title`): descartada —
  rompería `ShopTests` (comparan `Title` por igualdad) y mezclaría presentación
  con datos. Decisión cerrada 2.
- **Mostrar solo el tipo representante en duales:** descartada — el tipo es
  central y el lado B es jugable; ocultar su tipo engaña al jugador. Decisión 1.
- **No refactorizar `CardHandView`:** posible (los tres sitios nuevos podrían
  usar `TypePrefix` y dejar `CardHandView` con su copia inline), pero deja la
  duplicación que la decisión 3 busca eliminar. Se recomienda el refactor por ser
  behavior-preserving y barato.

## Estimación
- Complejidad: **baja**. Toda la infraestructura existe (paleta C8, helpers,
  rich text ya en uso). Es un método nuevo + tres ediciones de render + un
  archivo de tests.
- Sub-tareas: (1) `TypePrefix` + tests; (2) refactor `CardHandView`; (3) Hoguera;
  (4) Tienda stock; (5) Tienda eliminar.
- Riesgo: **bajo**. Único punto de atención: no tocar `BuildStock` y confirmar que
  los tests existentes de Shop/Campfire siguen verdes. El refactor de `CardHandView`
  debe quedar visualmente idéntico.

## Prompt de handoff para `modo:implementacion`
Ver bloque al final del chat / sección dedicada.
