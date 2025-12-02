namespace RoguelikeCardBattler.Gameplay.Combat
{
    /// <summary>
    /// Minimal interface for anything that can be targeted by gameplay actions.
    /// </summary>
    public interface ICombatActor
    {
        string Id { get; }
        string DisplayName { get; }
        int CurrentHP { get; }
        int MaxHP { get; }
        int Block { get; }

        void TakeDamage(int amount, ICombatActor source = null);
        void GainBlock(int amount);
        void LoseBlock(int amount);
        void DrawCards(int amount);
    }
}

