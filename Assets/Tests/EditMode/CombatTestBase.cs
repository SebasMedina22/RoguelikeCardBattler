using System.Collections.Generic;
using NUnit.Framework;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Gameplay.Combat.Actions;
using RoguelikeCardBattler.Gameplay.Enemies;
using UnityEngine;

namespace RoguelikeCardBattler.Tests.EditMode
{
    public abstract class CombatTestBase
    {
        private readonly List<UnityEngine.Object> _createdAssets = new List<UnityEngine.Object>();
        private readonly List<GameObject> _createdGameObjects = new List<GameObject>();

        protected CardDefinition CreateCard(
            string id,
            CardType type,
            CardTarget target,
            int cost,
            params EffectRef[] effects)
        {
            var card = ScriptableObject.CreateInstance<CardDefinition>();
            var effectList = new List<EffectRef>();
            if (effects != null)
            {
                effectList.AddRange(effects);
            }

            card.SetDebugData(
                id,
                id,
                $"{id} description",
                cost,
                type,
                CardRarity.Common,
                target,
                new List<string>(),
                effectList);

            _createdAssets.Add(card);
            return card;
        }

        protected EffectRef CreateEffect(EffectType type, int value, EffectTarget target)
        {
            return new EffectRef
            {
                effectType = type,
                value = value,
                target = target
            };
        }

        protected EnemyMove CreateEnemyMove(
            string id,
            string name,
            string description,
            List<EffectRef> effects,
            int weight = 1,
            int sequenceIndex = -1,
            EnemyIntentType intentType = EnemyIntentType.Unknown)
        {
            var move = new EnemyMove();
            move.SetDebugData(
                id,
                name,
                description,
                effects ?? new List<EffectRef>(),
                weight,
                sequenceIndex,
                intentType);
            return move;
        }

        protected EnemyDefinition CreateEnemyDefinition(
            string id,
            string name,
            int maxHp,
            EnemyAIPattern pattern,
            List<EnemyMove> moves)
        {
            var enemy = ScriptableObject.CreateInstance<EnemyDefinition>();
            enemy.SetDebugData(
                id,
                name,
                maxHp,
                0,
                pattern,
                new List<string>(),
                moves ?? new List<EnemyMove>());

            _createdAssets.Add(enemy);
            return enemy;
        }

        protected GameObject CreateGameObject(string name = "TestObject")
        {
            var go = new GameObject(name);
            _createdGameObjects.Add(go);
            return go;
        }

        protected T AddComponent<T>(GameObject go) where T : Component
        {
            var component = go.AddComponent<T>();
            return component;
        }

        [TearDown]
        public void CleanupAssets()
        {
            foreach (var asset in _createdAssets)
            {
                if (asset != null)
                {
                    UnityEngine.Object.DestroyImmediate(asset);
                }
            }

            _createdAssets.Clear();

            foreach (var go in _createdGameObjects)
            {
                if (go != null)
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }

            _createdGameObjects.Clear();
        }
    }
}

