namespace RoguelikeCardBattler.Gameplay.Relics
{
    /// <summary>
    /// Clasificación de Retazos según GOLDEN_RULES §10.
    /// Neutral: efectos genéricos no atados al cambio de mundo.
    /// Switch: se activan en o modifican cambios de mundo (DD-017).
    /// World: ligados a un mundo específico (A o B).
    /// </summary>
    public enum RelicCategory
    {
        Neutral,
        Switch,
        World
    }
}
