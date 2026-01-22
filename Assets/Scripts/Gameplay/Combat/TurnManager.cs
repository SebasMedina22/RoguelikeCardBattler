using System;
using System.Collections.Generic;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Combat.Actions;
using RoguelikeCardBattler.Gameplay.Enemies;

namespace RoguelikeCardBattler.Gameplay.Combat
{
    /// <summary>
    /// Orquesta el flujo de combate: fases de turno, jugar cartas, encolar efectos
    /// en la ActionQueue y aplicar efectividad/Momentum. Gestiona también Change World
    /// (limitado por combate con override de debug) y expone eventos para feedback UI.
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private List<CardDeckEntry> starterDeck = new List<CardDeckEntry>();
        [SerializeField] private EnemyDefinition enemyDefinition;

        [Header("Player Settings")]
        [SerializeField] private string playerDisplayName = "Test Pilot";
        [SerializeField] private int playerMaxHP = 60;
        [SerializeField] private int energyPerTurn = 3;
        [SerializeField, Min(1)] private int startingHandSize = 5;
        [SerializeField, Min(1)] private int cardsPerTurn = 5;
        [Header("World Switch Settings")]
        [SerializeField, Min(1)] private int maxWorldSwitchesPerCombat = 1;
        [SerializeField] private bool debugUnlimitedWorldSwitches = false;

        public enum WorldSide
        {
            A,
            B
        }

        private PlayerCombatActor _player;
        private EnemyCombatActor _enemy;
        private ActionQueue _actionQueue;
        private CombatPhase _phase = CombatPhase.None;
        private bool _initialized;
        private bool _externalConfigApplied;
        private int? _initialPlayerHpOverride;
        private int? _initialPlayerMaxHpOverride;
        private readonly System.Random _random = new System.Random();
        private int _enemySequenceCursor;
        private EnemyMove _plannedEnemyMove;
        [SerializeField] private WorldSide currentWorld = WorldSide.A;
        private int _worldSwitchesUsed;
        private int _freePlays;

        public enum CombatPhase
        {
            None,
            PlayerTurn,
            EnemyTurn,
            Victory,
            Defeat
        }

        public IReadOnlyList<CardDeckEntry> PlayerHand => _player?.Hand ?? Array.Empty<CardDeckEntry>();
        public int PlayerEnergy => _player?.CurrentEnergy ?? 0;
        public int PlayerHP => _player?.CurrentHP ?? 0;
        public int PlayerMaxHP => _player?.MaxHP ?? playerMaxHP;
        public int PlayerMaxEnergy => _player?.MaxEnergy ?? energyPerTurn;
        public int PlayerDrawPileCount => _player?.DrawPileCount ?? 0;
        public int PlayerDiscardPileCount => _player?.DiscardPileCount ?? 0;
        public int PlayerHandCount => _player?.HandCount ?? 0;
        public int PlayerBlock => _player?.Block ?? 0;
        public int EnemyHP => _enemy?.CurrentHP ?? 0;
        public int EnemyMaxHP => _enemy?.MaxHP ?? enemyDefinition?.MaxHP ?? 0;
        public int EnemyBlock => _enemy?.Block ?? 0;
        public CombatPhase CurrentPhase => _phase;
        public bool IsCombatFinished => _phase == CombatPhase.Victory || _phase == CombatPhase.Defeat;
        public EnemyMove PlannedEnemyMove => _plannedEnemyMove;
        public EnemyIntentType PlannedEnemyIntentType => _plannedEnemyMove?.IntentType ?? EnemyIntentType.Unknown;
        public int PlannedEnemyIntentValue => CalculateIntentValue(_plannedEnemyMove);
        public WorldSide CurrentWorld => currentWorld;
        public float CurrentEnemyAvatarScale => enemyDefinition != null ? Mathf.Max(0.1f, enemyDefinition.AvatarScale) : 1f;
        public Vector2 CurrentEnemyAvatarOffset => enemyDefinition != null ? enemyDefinition.AvatarOffset : Vector2.zero;
        public ElementType EnemyElementType => enemyDefinition != null ? enemyDefinition.ElementType : ElementType.None;
        public int FreePlays => _freePlays;
        public int WorldSwitchesUsed => _worldSwitchesUsed;
        public int MaxWorldSwitchesPerCombat => maxWorldSwitchesPerCombat;
        public bool DebugUnlimitedWorldSwitches => debugUnlimitedWorldSwitches;

        /// <summary>
        /// Evento disparado al aplicar daño de carta del jugador al enemigo.
        /// Entrega la efectividad (WEAK/RESIST/NEUTRAL) y si se otorgó Momentum (+1 free play).
        /// Consumido por la UI para mostrar popups/labels.
        /// </summary>
        public event Action<Effectiveness, bool> PlayerHitEffectiveness;

        /// <summary>
        /// Evento disparado cuando el enemigo recibe daño > 0. Valor es el daño final aplicado a HP.
        /// </summary>
        public event Action<int> EnemyTookDamage;

        private void Start()
        {
            if (_externalConfigApplied)
            {
                return;
            }
            InitializeCombat();
        }

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
        public void SetTestData(List<CardDeckEntry> deck, EnemyDefinition enemy)
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

        public void SetFreePlaysForTest(int value)
        {
            _freePlays = Mathf.Max(0, value);
        }

        public void SetWorldSwitchesForTest(int used, int? maxPerCombat = null, bool? unlimited = null)
        {
            _worldSwitchesUsed = Mathf.Max(0, used);
            if (maxPerCombat.HasValue)
            {
                maxWorldSwitchesPerCombat = Mathf.Max(1, maxPerCombat.Value);
            }

            if (unlimited.HasValue)
            {
                debugUnlimitedWorldSwitches = unlimited.Value;
            }
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

            int maxHp = _initialPlayerMaxHpOverride.HasValue
                ? Mathf.Max(1, _initialPlayerMaxHpOverride.Value)
                : playerMaxHP;
            _player = new PlayerCombatActor("player", playerDisplayName, maxHp, energyPerTurn, starterDeck, _random);
            if (_initialPlayerHpOverride.HasValue)
            {
                int desiredHp = Mathf.Clamp(_initialPlayerHpOverride.Value, 1, _player.MaxHP);
                int damage = _player.MaxHP - desiredHp;
                if (damage > 0)
                {
                    // Ajuste de HP inicial sin tocar mecánicas: aplicar daño directo al inicio.
                    _player.TakeDamage(damage);
                }
            }
            _initialPlayerHpOverride = null;
            _initialPlayerMaxHpOverride = null;
            _enemy = new EnemyCombatActor(enemyDefinition);
            _actionQueue = new ActionQueue();
            _phase = CombatPhase.None;
            _enemySequenceCursor = 0;
            _worldSwitchesUsed = 0;
            _freePlays = 0;
            _initialized = true;

            PlanNextEnemyMove();
            BeginPlayerTurn(useStartingHand: true);
        }

        /// <summary>
        /// Configura el combate con un deck y enemigo específicos antes de inicializar.
        /// </summary>
        public void ConfigureCombat(
            List<CardDeckEntry> deck,
            EnemyDefinition enemy,
            int? playerCurrentHpOverride = null,
            int? playerMaxHpOverride = null,
            bool initializeImmediately = true)
        {
            if (deck == null || deck.Count == 0)
            {
                Debug.LogError("ConfigureCombat: deck inválido.");
                return;
            }

            if (enemy == null)
            {
                Debug.LogError("ConfigureCombat: enemy inválido.");
                return;
            }

            starterDeck = new List<CardDeckEntry>(deck);
            enemyDefinition = enemy;
            _initialPlayerHpOverride = playerCurrentHpOverride;
            _initialPlayerMaxHpOverride = playerMaxHpOverride;
            _externalConfigApplied = true;

            if (initializeImmediately)
            {
                InitializeCombat();
            }
        }

        public bool PlayCard(CardDeckEntry cardEntry, ICombatActor explicitTarget = null)
        {
            if (!TryPrepareCardPlay(cardEntry, out PreparedCardPlay prepared, explicitTarget))
            {
                return false;
            }

            ResolvePreparedCardPlay(prepared);
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

            EnemyMove move = _plannedEnemyMove ?? SelectEnemyMove();
            _plannedEnemyMove = null;
            if (move != null)
            {
                QueueEffects(move.Effects, _enemy, _player, enemyDefinition?.ElementType ?? ElementType.None);
                _actionQueue.ProcessAll();
            }

            CheckCombatEndConditions();

            if (!IsCombatFinished)
            {
                PlanNextEnemyMove();
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

        private void QueueEffects(IEnumerable<EffectRef> effects, ICombatActor source, ICombatActor primaryTarget, ElementType sourceElementType = ElementType.None)
        {
            if (effects == null)
            {
                return;
            }

            foreach (EffectRef effect in effects)
            {
                foreach (ICombatActor target in ResolveTargets(effect, source, primaryTarget))
                {
                    IGameAction action = CreateAction(effect, source, target, sourceElementType);
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

        private IGameAction CreateAction(EffectRef effect, ICombatActor source, ICombatActor target, ElementType sourceElementType)
        {
            int amount = Math.Max(0, effect.value);
            switch (effect.effectType)
            {
                case EffectType.Damage:
                    int adjustedDamage = AdjustDamageForEffectiveness(source, target, sourceElementType, amount);
                    Action<int> onEnemyDamage = null;
                    if (source == _player && target == _enemy)
                    {
                        onEnemyDamage = dmg =>
                        {
                            if (dmg > 0)
                            {
                                EnemyTookDamage?.Invoke(dmg);
                            }
                        };
                    }
                    return new DamageAction(source, target, adjustedDamage, onEnemyDamage);
                case EffectType.Block:
                    return new BlockAction(target, amount);
                case EffectType.DrawCards:
                    return new DrawCardsAction(target, amount);
                default:
                    Debug.LogWarning($"Effect type {effect.effectType} not implemented for ActionQueue.");
                    return null;
            }
        }

        private int AdjustDamageForEffectiveness(ICombatActor source, ICombatActor target, ElementType sourceElementType, int baseAmount)
        {
            if (baseAmount <= 0)
            {
                return 0;
            }

            // Apply elemental modifiers only for player card damage against the enemy.
            if (source != _player || target != _enemy)
            {
                return baseAmount;
            }

            ElementType defenderType = enemyDefinition != null ? enemyDefinition.ElementType : ElementType.None;
            Effectiveness effectiveness = ElementEffectiveness.GetEffectiveness(sourceElementType, defenderType);
            float multiplier = effectiveness switch
            {
                Effectiveness.SuperEficaz => 1.5f,
                Effectiveness.PocoEficaz => 0.75f,
                _ => 1f
            };

            int adjusted = Mathf.RoundToInt(baseAmount * multiplier);
            int finalAmount = Math.Max(0, adjusted);

            bool momentumGranted = false;
            if (effectiveness == Effectiveness.SuperEficaz && finalAmount > 0)
            {
                _freePlays++;
                momentumGranted = true;
                Debug.Log("MOMENTUM: Free Play +1");
            }

            PlayerHitEffectiveness?.Invoke(effectiveness, momentumGranted);
            return finalAmount;
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

        private void PlanNextEnemyMove()
        {
            _plannedEnemyMove = SelectEnemyMove();
        }

        public bool TryChangeWorld()
        {
            if (!_initialized)
            {
                return false;
            }

            if (!debugUnlimitedWorldSwitches && _worldSwitchesUsed >= maxWorldSwitchesPerCombat)
            {
                Debug.LogWarning("World change limit reached for this combat.");
                return false;
            }

            currentWorld = currentWorld == WorldSide.A ? WorldSide.B : WorldSide.A;

            if (!debugUnlimitedWorldSwitches)
            {
                _worldSwitchesUsed++;
            }

            return true;
        }

        public void ToggleWorldForDebug()
        {
            TryChangeWorld();
        }

        public CardDefinition GetActiveCardDefinition(CardDeckEntry entry)
        {
            return entry?.GetActiveCard(CurrentWorld);
        }

        /// <summary>
        /// Prepara la jugada de una carta sin ejecutar acciones todavía (para animaciones).
        /// Valida fase/energía/momentum, remueve la carta de la mano y encola acciones.
        /// </summary>
        public bool TryPrepareCardPlay(CardDeckEntry cardEntry, out PreparedCardPlay prepared, ICombatActor explicitTarget = null)
        {
            prepared = null;
            if (!_initialized || _phase != CombatPhase.PlayerTurn || cardEntry == null)
            {
                return false;
            }

            if (!_player.IsCardInHand(cardEntry))
            {
                Debug.LogWarning("Card not in hand.");
                return false;
            }

            CardDefinition activeCard = GetActiveCardDefinition(cardEntry);
            if (activeCard == null)
            {
                Debug.LogWarning("Active card definition missing.");
                return false;
            }

            bool usedFreePlay = false;
            if (_freePlays > 0)
            {
                _freePlays--;
                usedFreePlay = true;
            }
            else if (!_player.SpendEnergy(activeCard.Cost))
            {
                Debug.LogWarning("Not enough energy to play card.");
                return false;
            }

            _player.RemoveCardFromHand(cardEntry);

            ICombatActor target = explicitTarget ?? GetDefaultOpponent(_player);
            QueueEffects(activeCard.Effects, _player, target, activeCard.ElementType);

            prepared = new PreparedCardPlay(cardEntry, activeCard, target, activeCard.Type == CardType.Attack, usedFreePlay);
            return true;
        }

        /// <summary>
        /// Ejecuta las acciones encoladas para la carta preparada y descarta la carta.
        /// </summary>
        public void ResolvePreparedCardPlay(PreparedCardPlay prepared)
        {
            if (prepared == null || prepared.Entry == null)
            {
                return;
            }

            _actionQueue.ProcessAll();
            _player.DiscardCard(prepared.Entry);
            CheckCombatEndConditions();
        }

        private int CalculateIntentValue(EnemyMove move)
        {
            if (move == null || move.Effects == null)
            {
                return 0;
            }

            int total = 0;
            foreach (EffectRef effect in move.Effects)
            {
                if (effect == null)
                {
                    continue;
                }

                switch (move.IntentType)
                {
                    case EnemyIntentType.Attack when effect.effectType == EffectType.Damage:
                        total += effect.value;
                        break;
                    case EnemyIntentType.Defend when effect.effectType == EffectType.Block:
                        total += effect.value;
                        break;
                }
            }

            return total;
        }
    }

    /// <summary>
    /// Contenedor de datos de una carta preparada para ejecutar tras animación.
    /// </summary>
    public class PreparedCardPlay
    {
        public CardDeckEntry Entry { get; }
        public CardDefinition ActiveCard { get; }
        public ICombatActor Target { get; }
        public bool IsAttackCard { get; }
        public bool UsedFreePlay { get; }

        public PreparedCardPlay(CardDeckEntry entry, CardDefinition activeCard, ICombatActor target, bool isAttackCard, bool usedFreePlay)
        {
            Entry = entry;
            ActiveCard = activeCard;
            Target = target;
            IsAttackCard = isAttackCard;
            UsedFreePlay = usedFreePlay;
        }
    }
}

