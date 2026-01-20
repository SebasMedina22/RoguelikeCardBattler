using System.Collections.Generic;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Enemies;

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

        public int GoldReward => Mathf.Max(0, goldReward);
        public int ChoicesCount => Mathf.Max(1, choicesCount);
        public EnemyDefinition DefaultEnemy => defaultEnemy;
        public IReadOnlyList<CardDeckEntry> StarterDeck => starterDeck;
        public IReadOnlyList<CardDeckEntry> RewardPool => rewardPool;
    }
}
