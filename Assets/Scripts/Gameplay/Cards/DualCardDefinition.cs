using UnityEngine;
using RoguelikeCardBattler.Gameplay.Combat;

namespace RoguelikeCardBattler.Gameplay.Cards
{
    [CreateAssetMenu(menuName = "Cards/Dual Card Definition", fileName = "DualCardDefinition")]
    public class DualCardDefinition : ScriptableObject
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string displayName = "Dual Card";
        [SerializeField] private CardDefinition sideA;
        [SerializeField] private CardDefinition sideB;

        public string Id => id;
        public string DisplayName => displayName;
        public CardDefinition SideA => sideA;
        public CardDefinition SideB => sideB;

        public CardDefinition GetSide(TurnManager.WorldSide worldSide)
        {
            return worldSide == TurnManager.WorldSide.A ? sideA : sideB;
        }
    }
}

