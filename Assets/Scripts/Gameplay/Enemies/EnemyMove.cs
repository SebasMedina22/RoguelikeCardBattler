using System;
using System.Collections.Generic;
using RoguelikeCardBattler.Gameplay.Cards;
using UnityEngine;

namespace RoguelikeCardBattler.Gameplay.Enemies
{
    [Serializable]
    public class EnemyMove
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string moveName = "New Move";
        [SerializeField, TextArea] private string description = string.Empty;
        [SerializeField] private List<EffectRef> effects = new List<EffectRef>();
        [SerializeField] private int weight = 1;
        [SerializeField] private int sequenceIndex = -1;

        public string Id => id;
        public string MoveName => moveName;
        public string Description => description;
        public IReadOnlyList<EffectRef> Effects => effects;
        public int Weight => weight;
        public int SequenceIndex => sequenceIndex;
    }
}

