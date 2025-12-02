using System.Collections.Generic;
using UnityEngine;

namespace RoguelikeCardBattler.Gameplay.Enemies
{
    [CreateAssetMenu(menuName = "Enemies/Enemy Definition", fileName = "EnemyDefinition")]
    public class EnemyDefinition : ScriptableObject
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string enemyName = "New Enemy";
        [SerializeField] private int maxHP = 30;
        [SerializeField] private int baseBlock = 0;
        [SerializeField] private EnemyAIPattern aiPattern = EnemyAIPattern.RandomWeighted;
        [SerializeField] private List<string> tags = new List<string>();
        [SerializeField] private List<EnemyMove> moves = new List<EnemyMove>();

        public string Id => id;
        public string EnemyName => enemyName;
        public int MaxHP => maxHP;
        public int BaseBlock => baseBlock;
        public EnemyAIPattern AIPattern => aiPattern;
        public IReadOnlyList<string> Tags => tags;
        public IReadOnlyList<EnemyMove> Moves => moves;
    }
}

