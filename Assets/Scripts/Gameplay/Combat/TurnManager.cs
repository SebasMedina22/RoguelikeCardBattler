using System;
using System.Collections.Generic;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Combat.Actions;
using RoguelikeCardBattler.Gameplay.Enemies;

namespace RoguelikeCardBattler.Gameplay.Combat
{
    public class TurnManager : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private List<CardDefinition> starterDeck = new List<CardDefinition>();
        [SerializeField] private EnemyDefinition enemyDefinition;

        [Header("Player Settings")]
        [SerializeField] private string playerDisplayName = "Test Pilot";
        [SerializeField] private int playerMaxHP = 60;
        [SerializeField] private int energyPerTurn = 3;
        [SerializeField, Min(1)] private int startingHandSize = 5;
        [SerializeField, Min(1)] private int cardsPerTurn = 5;

        private PlayerCombatActor _player;
        private EnemyCombatActor _enemy;
        private ActionQueue _actionQueue;
        private CombatPhase _phase = CombatPhase.None;
        private bool _initialized;
        private readonly System.Random _random = new System.Random();
        private int _enemySequenceCursor;

        public enum CombatPhase
        {
            None,
            PlayerTurn,
            EnemyTurn,
            Victory,
            Defeat
        }

        public IReadOnlyList<CardDefinition> PlayerHand => _player?.Hand ?? Array.Empty<CardDefinition>();
        public int PlayerEnergy => _player?.CurrentEnergy ?? 0;
        public int PlayerHP => _player?.CurrentHP ?? 0;
        public int PlayerMaxHP => _player?.MaxHP ?? playerMaxHP;
        public int PlayerMaxEnergy => _player?.MaxEnergy ?? energyPerTurn;
        public int PlayerDrawPileCount => _player?.DrawPileCount ?? 0;
        public int PlayerDiscardPileCount => _player?.DiscardPileCount ?? 0;
        public int PlayerHandCount => _player?.HandCount ?? 0;
        public int EnemyHP => _enemy?.CurrentHP ?? 0;
        public int EnemyMaxHP => _enemy?.MaxHP ?? enemyDefinition?.MaxHP ?? 0;
        public CombatPhase CurrentPhase => _phase;
        public bool IsCombatFinished => _phase == CombatPhase.Victory || _phase == CombatPhase.Defeat;

        private void Start()
        {
            InitializeCombat();
        }

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
        public void SetTestData(List<CardDefinition> deck, EnemyDefinition enemy)
        {
            starterDeck = deck;
            enemyDefinition = enemy;
        }

        public void SetTestConfig(int maxHp, int energy, int startingHand, int cardsPerTurnCount)
        {
            playerMaxHP = Mathf.Max(1, maxHp);
            energyPerTurn = Mathf.Max(0, energy);
            startingHandSize = Mathf.Max(1, startingHand);
            cardsPerTurn = Mathf.Max(1, cardsPerTurnCount);
        }
#endif

        public void InitializeCombat()
        {
            if (starterDeck == null || starterDeck.Count == 0)
            {
                Debug.LogError("Starter deck is empty. Assign CardDefinitions in the inspector.");
                return;
            }

            if (enemyDefinition == null)
            {
                Debug.LogError("Enemy definition is missing.");
                return;
            }

            _player = new PlayerCombatActor("player", playerDisplayName, playerMaxHP, energyPerTurn, starterDeck, _random);
            _enemy = new EnemyCombatActor(enemyDefinition);
            _actionQueue = new ActionQueue();
            _phase = CombatPhase.None;
            _enemySequenceCursor = 0;
            _initialized = true;

            BeginPlayerTurn(useStartingHand: true);
        }

        public bool PlayCard(CardDefinition card, ICombatActor explicitTarget = null)
        {
            if (!_initialized || _phase != CombatPhase.PlayerTurn || card == null)
            {
                return false;
            }

            if (!_player.IsCardInHand(card))
            {
                Debug.LogWarning($"Card {card.name} not in hand.");
                return false;
            }

            if (!_player.SpendEnergy(card.Cost))
            {
                Debug.LogWarning("Not enough energy to play card.");
                return false;
            }

            _player.RemoveCardFromHand(card);

            ICombatActor target = explicitTarget ?? GetDefaultOpponent(_player);
            QueueEffects(card.Effects, _player, target);
            _actionQueue.ProcessAll();
            _player.DiscardCard(card);

            CheckCombatEndConditions();
            return true;
        }

        public void EndPlayerTurn()
        {
            if (!_initialized || _phase != CombatPhase.PlayerTurn)
            {
                return;
            }

            _player.DiscardHand();
            _phase = CombatPhase.EnemyTurn;
            ExecuteEnemyTurn();
        }

        private void BeginPlayerTurn(bool useStartingHand = false)
        {
            if (!_initialized || IsCombatFinished)
            {
                return;
            }

            _phase = CombatPhase.PlayerTurn;
            _player.ResetEnergy();
            ClearBlock(_player);

            int cardsToDraw = useStartingHand ? startingHandSize : cardsPerTurn;
            if (cardsToDraw > 0)
            {
                _player.DrawCards(cardsToDraw);
            }
        }

        private void ExecuteEnemyTurn()
        {
            if (!_initialized || IsCombatFinished)
            {
                return;
            }

            ClearBlock(_enemy);

            EnemyMove move = SelectEnemyMove();
            if (move != null)
            {
                QueueEffects(move.Effects, _enemy, _player);
                _actionQueue.ProcessAll();
            }

            CheckCombatEndConditions();

            if (!IsCombatFinished)
            {
                BeginPlayerTurn();
            }
        }

        private EnemyMove SelectEnemyMove()
        {
            IReadOnlyList<EnemyMove> moves = enemyDefinition.Moves;
            if (moves == null || moves.Count == 0)
            {
                return null;
            }

            switch (enemyDefinition.AIPattern)
            {
                case EnemyAIPattern.RandomWeighted:
                    int totalWeight = 0;
                    foreach (EnemyMove move in moves)
                    {
                        totalWeight += Math.Max(1, move.Weight);
                    }

                    int roll = _random.Next(Math.Max(1, totalWeight));
                    int accumulator = 0;
                    foreach (EnemyMove move in moves)
                    {
                        accumulator += Math.Max(1, move.Weight);
                        if (roll < accumulator)
                        {
                            return move;
                        }
                    }

                    return moves[moves.Count - 1];

                case EnemyAIPattern.Sequence:
                    EnemyMove sequenceMove = moves[_enemySequenceCursor % moves.Count];
                    _enemySequenceCursor++;
                    return sequenceMove;

                default:
                    return moves[_random.Next(moves.Count)];
            }
        }

        private void QueueEffects(IEnumerable<EffectRef> effects, ICombatActor source, ICombatActor primaryTarget)
        {
            if (effects == null)
            {
                return;
            }

            foreach (EffectRef effect in effects)
            {
                foreach (ICombatActor target in ResolveTargets(effect, source, primaryTarget))
                {
                    IGameAction action = CreateAction(effect, source, target);
                    if (action != null)
                    {
                        _actionQueue.Enqueue(action);
                    }
                }
            }
        }

        private IEnumerable<ICombatActor> ResolveTargets(EffectRef effect, ICombatActor source, ICombatActor primaryTarget)
        {
            switch (effect.target)
            {
                case EffectTarget.Self:
                    yield return source;
                    break;
                case EffectTarget.SingleEnemy:
                    yield return primaryTarget ?? GetDefaultOpponent(source);
                    break;
                case EffectTarget.AllEnemies:
                    foreach (ICombatActor actor in GetAllOpponents(source))
                    {
                        yield return actor;
                    }
                    break;
            }
        }

        private IGameAction CreateAction(EffectRef effect, ICombatActor source, ICombatActor target)
        {
            int amount = Math.Max(0, effect.value);
            switch (effect.effectType)
            {
                case EffectType.Damage:
                    return new DamageAction(source, target, amount);
                case EffectType.Block:
                    return new BlockAction(target, amount);
                case EffectType.DrawCards:
                    return new DrawCardsAction(target, amount);
                default:
                    Debug.LogWarning($"Effect type {effect.effectType} not implemented for ActionQueue.");
                    return null;
            }
        }

        private ICombatActor GetDefaultOpponent(ICombatActor source)
        {
            if (source == _player)
            {
                return _enemy;
            }

            if (source == _enemy)
            {
                return _player;
            }

            return null;
        }

        private IEnumerable<ICombatActor> GetAllOpponents(ICombatActor source)
        {
            ICombatActor opponent = GetDefaultOpponent(source);
            if (opponent != null)
            {
                yield return opponent;
            }
        }

        private void ClearBlock(ICombatActor actor)
        {
            if (actor is null || actor.Block <= 0)
            {
                return;
            }

            actor.LoseBlock(actor.Block);
        }

        private void CheckCombatEndConditions()
        {
            if (_enemy != null && _enemy.CurrentHP <= 0)
            {
                _phase = CombatPhase.Victory;
                Debug.Log("Player wins the combat.");
                return;
            }

            if (_player != null && _player.CurrentHP <= 0)
            {
                _phase = CombatPhase.Defeat;
                Debug.Log("Player was defeated.");
            }
        }

        public bool IsPlayerTurn() => _phase == CombatPhase.PlayerTurn;
    }
}

