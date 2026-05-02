# Marcadores Explícitos

> **Para qué se carga este archivo:** los modos especializados lo leen como
> parte de su ritual para usar la convención de marcadores en sus outputs.

Estos marcadores aparecen en outputs de los modos. Son obligatorios cuando aplican
para evitar confusión entre lo que **es** vs lo que **se propone** vs lo que **se asume**.

| Marcador | Significado | Cuándo se usa |
|----------|-------------|---------------|
| `[CÓDIGO ACTUAL]` | Verificado leyendo el repo hoy | Cuando describes el estado real del código |
| `[PROPUESTA]` | Sugerencia que aún no se aplica | Cuando sugieres un cambio |
| `[HIPÓTESIS]` | Explicación no verificada | Cuando especulas la causa de un bug sin haberlo confirmado |
| `[GDD]` | Lo que el documento dice textualmente | En `modo:gdd`, citas o referencias al GDD |
| `[INTERPRETACIÓN]` | Lectura nuestra del GDD | En `modo:gdd` o `modo:diseno`, cuando traduces |
| `[GAP]` | El GDD pide algo que no existe | En `modo:gdd`, mapeo de pendientes |
| `[CONTRADICCIÓN]` | Conflicto entre fuentes | Cuando GDD ↔ GOLDEN_RULES, o GDD ↔ código |
| `[NO CLARO]` | Información ambigua o faltante | Cuando hace falta preguntar a Sebastián |
| `[ABIERTO]` | Decisión pendiente de cierre | En specs, marca lo que requiere decisión antes de implementar |
| `[ALTERNATIVA]` | Opción descartada | En specs, contexto sobre qué se evaluó |
| `[REQUIERE APROBACIÓN]` | Toca archivo protegido o requiere autorización | Cuando un cambio necesita confirmación explícita de Sebastián |
