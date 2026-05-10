using UnityEngine;
using RoguelikeCardBattler.Gameplay.Combat;

namespace RoguelikeCardBattler.Gameplay.Cards
{
    /// <summary>
    /// Carta dual con dos lados (A/B) elegidos según WorldSide. Ambas mitades comparten ID y coste.
    /// </summary>
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

        /// <summary>
        /// Setter runtime usado por <see cref="CreateUpgradedClone"/> para poblar
        /// la instancia clonada sin requerir SerializedObject. NO está bajo
        /// UNITY_EDITOR: el flujo de mejora corre en runtime de play (Hoguera).
        /// </summary>
        public void InitRuntimeSides(string newId, string newDisplayName, CardDefinition newSideA, CardDefinition newSideB)
        {
            id = newId;
            displayName = newDisplayName;
            sideA = newSideA;
            sideB = newSideB;
        }

        /// <summary>
        /// Clona la carta dual aplicando la mejora a cada lado que tenga upgrade
        /// definido. DD-013: la mejora afecta ambos lados; los que no tengan
        /// upgrade conservan la referencia original (no se clonan innecesariamente).
        /// </summary>
        public DualCardDefinition CreateUpgradedClone()
        {
            DualCardDefinition clone = ScriptableObject.CreateInstance<DualCardDefinition>();

            CardDefinition newSideA = sideA != null && sideA.Upgrade != null && sideA.Upgrade.HasUpgrade
                ? sideA.CreateUpgradedClone()
                : sideA;

            CardDefinition newSideB = sideB != null && sideB.Upgrade != null && sideB.Upgrade.HasUpgrade
                ? sideB.CreateUpgradedClone()
                : sideB;

            clone.InitRuntimeSides(id, displayName + "+", newSideA, newSideB);
            return clone;
        }
    }
}
