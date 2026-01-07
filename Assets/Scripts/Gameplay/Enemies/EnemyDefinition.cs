using System.Collections.Generic;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Combat;

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
        [SerializeField] private float avatarScale = 1f;
        [SerializeField] private Vector2 avatarOffset = Vector2.zero;
        [SerializeField] private ElementType elementType = ElementType.None;

        public string Id => id;
        public string EnemyName => enemyName;
        public int MaxHP => maxHP;
        public int BaseBlock => baseBlock;
        public EnemyAIPattern AIPattern => aiPattern;
        public IReadOnlyList<string> Tags => tags;
        public IReadOnlyList<EnemyMove> Moves => moves;
        public float AvatarScale => avatarScale;
        public Vector2 AvatarOffset => avatarOffset;
        public ElementType ElementType => elementType;

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
        public void SetDebugData(
            string newId,
            string newName,
            int newMaxHP,
            int newBaseBlock,
            EnemyAIPattern newPattern,
            List<string> newTags,
            List<EnemyMove> newMoves,
            float newAvatarScale = 1f,
            Vector2? newAvatarOffset = null,
            ElementType newElementType = ElementType.None)
        {
            id = newId;
            enemyName = newName;
            maxHP = Mathf.Max(1, newMaxHP);
            baseBlock = Mathf.Max(0, newBaseBlock);
            aiPattern = newPattern;
            tags = newTags ?? new List<string>();
            moves = newMoves ?? new List<EnemyMove>();
            avatarScale = Mathf.Max(0.1f, newAvatarScale);
            avatarOffset = newAvatarOffset ?? Vector2.zero;
            elementType = newElementType;
        }
#endif
    }
}

