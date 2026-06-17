using System.Collections.Generic;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Enemies;
using RoguelikeCardBattler.Gameplay.Relics;

namespace RoguelikeCardBattler.Run
{
    [CreateAssetMenu(menuName = "Run/Combat Config", fileName = "RunCombatConfig")]
    public class RunCombatConfig : ScriptableObject
    {
        [SerializeField] private int goldReward = 10;
        [SerializeField] private int choicesCount = 3;
        [SerializeField] private EnemyDefinition defaultEnemy;
        [SerializeField] private List<CardDeckEntry> starterDeck = new List<CardDeckEntry>();
        [SerializeField] private List<CardDeckEntry> rewardPool = new List<CardDeckEntry>();
        // Pools de drop garantizados al ganar Elite/Boss (Sub-PR 3B). El Elite
        // se randomiza sin duplicados; el Boss es único y siempre cae el mismo.
        [SerializeField] private List<RelicDefinition> eliteRelicDropPool = new List<RelicDefinition>();
        [SerializeField] private RelicDefinition bossRelicDrop;

        public int GoldReward => Mathf.Max(0, goldReward);
        public int ChoicesCount => Mathf.Max(1, choicesCount);
        public EnemyDefinition DefaultEnemy => defaultEnemy;
        public IReadOnlyList<CardDeckEntry> StarterDeck => starterDeck;
        public IReadOnlyList<CardDeckEntry> RewardPool => rewardPool;
        public IReadOnlyList<RelicDefinition> EliteRelicDropPool => eliteRelicDropPool;
        public RelicDefinition BossRelicDrop => bossRelicDrop;

#if UNITY_EDITOR
        /// <summary>
        /// Setter editor-only para que <c>StarterDeckSetup</c> (4a) reescriba el
        /// starter deck a la composición GDD §5 sin edición manual del inspector.
        /// Mismo patrón que <c>NewRunConfig.EditorPopulateFaces</c> /
        /// <c>ShopConfig.EditorPopulatePools</c>. No disponible en runtime.
        /// </summary>
        public void EditorPopulateStarterDeck(List<CardDeckEntry> entries)
        {
            starterDeck = entries ?? new List<CardDeckEntry>();
        }
#endif
    }
}
