using System;
using System.Collections.Generic;
using RoguelikeCardBattler.Gameplay.Cards;
using UnityEngine;

namespace RoguelikeCardBattler.Gameplay.Enemies
{
    /// <summary>
    /// Tipo de intención mostrado en UI (básico: Attack / Defend / Unknown).
    /// </summary>
    public enum EnemyIntentType
    {
        Unknown,
        Attack,
        Defend
    }

    [Serializable]
    /// <summary>
    /// Movimiento de enemigo con efectos, peso/orden y tipo de intención para UI.
    /// </summary>
    public class EnemyMove
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string moveName = "New Move";
        [SerializeField, TextArea] private string description = string.Empty;
        [SerializeField] private List<EffectRef> effects = new List<EffectRef>();
        [SerializeField] private int weight = 1;
        [SerializeField] private int sequenceIndex = -1;
        [SerializeField] private EnemyIntentType intentType = EnemyIntentType.Unknown;
        // Rango de HP% del enemigo en el que este move está disponible para PhaseBased AI.
        // Defaults 0/100 = siempre disponible (compatible con RandomWeighted y Sequence).
        [SerializeField, Range(0, 100)] private int minHpPercent = 0;
        [SerializeField, Range(0, 100)] private int maxHpPercent = 100;

        public string Id => id;
        public string MoveName => moveName;
        public string Description => description;
        public IReadOnlyList<EffectRef> Effects => effects;
        public int Weight => weight;
        public int SequenceIndex => sequenceIndex;
        public EnemyIntentType IntentType => intentType;
        public int MinHpPercent => minHpPercent;
        public int MaxHpPercent => maxHpPercent;

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
        public void SetDebugData(
            string newId,
            string newName,
            string newDescription,
            List<EffectRef> newEffects,
            int newWeight = 1,
            int newSequenceIndex = -1,
            EnemyIntentType newIntentType = EnemyIntentType.Unknown,
            int newMinHpPercent = 0,
            int newMaxHpPercent = 100)
        {
            id = newId;
            moveName = newName;
            description = newDescription;
            effects = newEffects ?? new List<EffectRef>();
            weight = newWeight;
            sequenceIndex = newSequenceIndex;
            intentType = newIntentType;
            minHpPercent = newMinHpPercent;
            maxHpPercent = newMaxHpPercent;
        }
#endif
    }
}

