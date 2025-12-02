using UnityEngine;

namespace RoguelikeCardBattler.Gameplay.Cards
{
    /// <summary>
    /// Categorization of card behaviors for gameplay and UI filtering.
    /// </summary>
    public enum CardType
    {
        Attack,
        Skill,
        Power,
        Curse,
        Status
    }

    public enum CardRarity
    {
        Common,
        Uncommon,
        Rare,
        Legendary
    }

    /// <summary>
    /// Primary targeting intent for a card or effect.
    /// </summary>
    public enum CardTarget
    {
        None,
        Self,
        SingleEnemy,
        AllEnemies
    }

    public enum EffectType
    {
        Damage,
        Block,
        DrawCards,
        GainEnergy,
        ApplyStatus,
        Heal
    }

    public enum EffectTarget
    {
        Self,
        SingleEnemy,
        AllEnemies
    }

    public enum StatusType
    {
        None,
        Poison,
        Weak,
        Vulnerable,
        Custom
    }
}

