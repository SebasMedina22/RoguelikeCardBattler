using System.Collections.Generic;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Combat;

namespace RoguelikeCardBattler.Gameplay.Enemies
{
    /// <summary>
    /// ScriptableObject de enemigo: HP base, patrón de IA (RandomWeighted/Sequence), moves, tipo elemental y sprite.
    /// </summary>
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
        // Tipo en Mundo B. Solo significativo si el enemigo es transdimensional
        // (typeWorldB != None && !isAnchor). Para tipo-único y ancla queda en None.
        [SerializeField] private ElementType typeWorldB = ElementType.None;
        // Si true, el tipo NO reacciona al cambio de mundo (ancla). Precede a typeWorldB.
        [SerializeField] private bool isAnchor = false;
        [SerializeField] private Sprite avatar = null;

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
        public ElementType TypeWorldB => typeWorldB;
        public bool IsAnchor => isAnchor;

        // Derivada: un enemigo es transdimensional si tiene un tipo de Mundo B
        // distinto y NO es ancla. No es un flag serializado (evita superficie de
        // autoría redundante) — se computa de los otros dos campos.
        public bool IsTransdimensional => !isAnchor && typeWorldB != ElementType.None;
        public Sprite Avatar => avatar;

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
            ElementType newElementType = ElementType.None,
            ElementType newTypeWorldB = ElementType.None,
            bool newIsAnchor = false)
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
            typeWorldB = newTypeWorldB;
            isAnchor = newIsAnchor;
        }
#endif
    }
}

