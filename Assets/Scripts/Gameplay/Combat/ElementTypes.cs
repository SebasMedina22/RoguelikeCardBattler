namespace RoguelikeCardBattler.Gameplay.Combat
{
    /// <summary>
    /// Tipos elementales placeholder por color. Se renombrarán a tipos finales.
    /// </summary>
    public enum ElementType
    {
        None = 0,
        Rojo,
        Amarillo,
        Azul,
        Morado,
        Negro,
        Blanco
    }

    /// <summary>
    /// Resultado de atacar con un elemento contra otro.
    /// </summary>
    public enum Effectiveness
    {
        PocoEficaz,
        Neutro,
        SuperEficaz
    }

    /// <summary>
    /// Matriz de efectividad atacante/defensor. Neutro para None o mismo tipo.
    /// </summary>
    public static class ElementEffectiveness
    {
        public static Effectiveness GetEffectiveness(ElementType attacker, ElementType defender)
        {
            if (attacker == defender || attacker == ElementType.None || defender == ElementType.None)
            {
                return Effectiveness.Neutro;
            }

            switch (attacker)
            {
                case ElementType.Rojo:
                    return defender switch
                    {
                        ElementType.Amarillo => Effectiveness.PocoEficaz,
                        ElementType.Azul => Effectiveness.SuperEficaz,
                        ElementType.Morado => Effectiveness.Neutro,
                        ElementType.Negro => Effectiveness.PocoEficaz,
                        ElementType.Blanco => Effectiveness.SuperEficaz,
                        _ => Effectiveness.Neutro
                    };
                case ElementType.Amarillo:
                    return defender switch
                    {
                        ElementType.Rojo => Effectiveness.SuperEficaz,
                        ElementType.Azul => Effectiveness.PocoEficaz,
                        ElementType.Morado => Effectiveness.SuperEficaz,
                        ElementType.Negro => Effectiveness.Neutro,
                        ElementType.Blanco => Effectiveness.PocoEficaz,
                        _ => Effectiveness.Neutro
                    };
                case ElementType.Azul:
                    return defender switch
                    {
                        ElementType.Rojo => Effectiveness.PocoEficaz,
                        ElementType.Amarillo => Effectiveness.SuperEficaz,
                        ElementType.Morado => Effectiveness.PocoEficaz,
                        ElementType.Negro => Effectiveness.SuperEficaz,
                        ElementType.Blanco => Effectiveness.Neutro,
                        _ => Effectiveness.Neutro
                    };
                case ElementType.Morado:
                    return defender switch
                    {
                        ElementType.Rojo => Effectiveness.Neutro,
                        ElementType.Amarillo => Effectiveness.SuperEficaz,
                        ElementType.Azul => Effectiveness.PocoEficaz,
                        ElementType.Negro => Effectiveness.SuperEficaz,
                        ElementType.Blanco => Effectiveness.PocoEficaz,
                        _ => Effectiveness.Neutro
                    };
                case ElementType.Negro:
                    return defender switch
                    {
                        ElementType.Rojo => Effectiveness.PocoEficaz,
                        ElementType.Amarillo => Effectiveness.Neutro,
                        ElementType.Azul => Effectiveness.SuperEficaz,
                        ElementType.Morado => Effectiveness.PocoEficaz,
                        ElementType.Blanco => Effectiveness.SuperEficaz,
                        _ => Effectiveness.Neutro
                    };
                case ElementType.Blanco:
                    return defender switch
                    {
                        ElementType.Rojo => Effectiveness.SuperEficaz,
                        ElementType.Amarillo => Effectiveness.PocoEficaz,
                        ElementType.Azul => Effectiveness.Neutro,
                        ElementType.Morado => Effectiveness.SuperEficaz,
                        ElementType.Negro => Effectiveness.PocoEficaz,
                        _ => Effectiveness.Neutro
                    };
                default:
                    return Effectiveness.Neutro;
            }
        }
    }

    /// <summary>
    /// Multiplicadores de efectividad centralizados (DD-018). Antes vivían como
    /// literales dentro de TurnManager.AdjustDamageForEffectiveness; ahora se
    /// declaran aquí para que sub-PRs siguientes (efectividad bidireccional,
    /// daño x1.5 al jugador, cartas neutras al 90%) los reutilicen sin
    /// duplicación.
    /// </summary>
    public static class EffectivenessMultipliers
    {
        public const float SuperEficaz = 1.5f;
        public const float Neutro = 1.0f;
        public const float PocoEficaz = 0.75f;

        // Cartas con tipo None aplican este multiplicador adicional al daño base
        // (DD-002). Declarado en sub-PR A; sub-PR C lo aplica.
        public const float NeutralCardDamage = 0.9f;

        public static float For(Effectiveness effectiveness) =>
            effectiveness switch
            {
                Effectiveness.SuperEficaz => SuperEficaz,
                Effectiveness.PocoEficaz => PocoEficaz,
                _ => Neutro
            };
    }
}

