using System;
using System.Collections.Generic;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Combat.Actions;
using RoguelikeCardBattler.Gameplay.Enemies;
using RoguelikeCardBattler.Gameplay.Relics;
using RoguelikeCardBattler.Gameplay.Relics.Hooks;
using RoguelikeCardBattler.Run;

namespace RoguelikeCardBattler.Gameplay.Combat
{
    /// <summary>
    /// Orquesta el flujo de combate: fases de turno, jugar cartas, encolar efectos
    /// en la ActionQueue y aplicar efectividad/Contador de Estilo. Gestiona también Change World
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
        [SerializeField, Min(1)] private int maxHandSize = 7;
        [Header("Player Element Types (defaults; override via ConfigureCombat)")]
        [SerializeField] private ElementType playerWorldATypeDefault = ElementType.Rojo;
        [SerializeField] private ElementType playerWorldBTypeDefault = ElementType.Amarillo;
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
        private int _styleCharges;
        // Switches extra otorgados al llegar a 5 cargas de Estilo. No acumulable: máx 1 a la vez.
        private int _bonusWorldSwitches;
        // Tipos elegidos al inicio del run (uno por mundo). Inyectados desde
        // RunState via ConfigureCombat; en escenas independientes (BattleScene
        // standalone, tests) caen al default del SerializeField.
        private ElementType _playerWorldAType;
        private ElementType _playerWorldBType;
        private bool _autoEndedThisTurn;
        private bool _resolvingCard;
        // Flags del nodo actual cableados desde BattleFlowController via ConfigureCombat.
        // Se inyectan en CombatStartHookData/CombatEndHookData para que Retazos como
        // R-END-3 ("+oro extra al ganar Elite") puedan filtrar por contexto del combate.
        private bool _isCurrentCombatElite;
        private bool _isCurrentCombatBoss;

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
        public int MaxHandSize => _player?.MaxHandSize ?? maxHandSize;
        public int EnemyHP => _enemy?.CurrentHP ?? 0;
        public int EnemyMaxHP => _enemy?.MaxHP ?? enemyDefinition?.MaxHP ?? 0;
        public int EnemyBlock => _enemy?.Block ?? 0;
        public CombatPhase CurrentPhase => _phase;
        public bool IsCombatFinished => _phase == CombatPhase.Victory || _phase == CombatPhase.Defeat;
        public EnemyMove PlannedEnemyMove => _plannedEnemyMove;
        public EnemyIntentType PlannedEnemyIntentType => _plannedEnemyMove?.IntentType ?? EnemyIntentType.Unknown;
        public int PlannedEnemyIntentValue => CalculateIntentValue(_plannedEnemyMove);
        public WorldSide CurrentWorld => currentWorld;

        /// <summary>
        /// Tipo activo del jugador, derivado del mundo actual y los 2 tipos
        /// elegidos al inicio del run. Sub-PR A solo lo expone — sub-PR B lo
        /// consume para aplicar efectividad bidireccional al daño enemigo.
        /// </summary>
        public ElementType PlayerActiveType =>
            currentWorld == WorldSide.A ? _playerWorldAType : _playerWorldBType;

        public float CurrentEnemyAvatarScale => enemyDefinition != null ? Mathf.Max(0.1f, enemyDefinition.AvatarScale) : 1f;
        public Vector2 CurrentEnemyAvatarOffset => enemyDefinition != null ? enemyDefinition.AvatarOffset : Vector2.zero;
        public ElementType EnemyElementType => enemyDefinition != null ? enemyDefinition.ElementType : ElementType.None;
        public EnemyDefinition CurrentEnemyDefinition => enemyDefinition;
        public int StyleCharges => _styleCharges;
        // Refs read-only para que IRelicEffect (Sub-PR 3B) pueda invocar la API
        // ctx.Grant*(ctx.TurnManager.Player, n). Solo getters — los Retazos no
        // mutan el estado del actor directamente, sino vía RelicGrant*.
        public ICombatActor Player => _player;
        public ICombatActor Enemy => _enemy;
        /// <summary>
        /// Cap dinámico de cambios de mundo = base + bonus acumulado por Contador de Estilo.
        /// Reemplaza MaxWorldSwitchesPerCombat como límite operativo en TryChangeWorld.
        /// </summary>
        public int TotalAvailableWorldSwitches => maxWorldSwitchesPerCombat + _bonusWorldSwitches;
        public int WorldSwitchesUsed => _worldSwitchesUsed;
        public int MaxWorldSwitchesPerCombat => maxWorldSwitchesPerCombat;
        public bool DebugUnlimitedWorldSwitches => debugUnlimitedWorldSwitches;

        /// <summary>
        /// Evento disparado al aplicar daño de carta del jugador al enemigo.
        /// Entrega la efectividad (WEAK/RESIST/NEUTRAL) y si se otorgó una carga de Estilo.
        /// Consumido por CombatFeedbackView para mostrar popups WEAK/RESIST/+ESTILO.
        /// </summary>
        public event Action<Effectiveness, bool> PlayerHitEffectiveness;

        /// <summary>
        /// Evento disparado cuando el enemigo recibe daño > 0. Valor es el daño final aplicado a HP.
        /// </summary>
        public event Action<int> EnemyTookDamage;

        /// <summary>
        /// Evento disparado cuando el jugador intenta robar con la mano llena.
        /// </summary>
        public event Action<int> PlayerHandLimitReached;

        /// <summary>
        /// Evento disparado cuando el enemigo aplica daño al jugador y hubo efectividad
        /// calculada (incluido Neutro). Sub-PR C lo consume para ajustar cargas del
        /// Contador de Estilo (-1 si SuperEficaz). En Sub-PR B no hay suscriptores;
        /// el evento existe pero queda silente hasta que la UI lo enchufe en C.
        /// </summary>
        public event Action<Effectiveness> EnemyHitEffectiveness;

        private void Start()
        {
            if (_externalConfigApplied)
            {
                return;
            }
            InitializeCombat();
        }

        private void Update()
        {
            if (!_initialized || IsCombatFinished || _phase != CombatPhase.PlayerTurn)
            {
                return;
            }

            if (_autoEndedThisTurn || _resolvingCard)
            {
                return;
            }

            if (_actionQueue != null && _actionQueue.PendingCount > 0)
            {
                return;
            }

            if (HasAnyPlayableCardInHand())
            {
                return;
            }

            // Anti-softlock: si no hay jugadas posibles, termina el turno automáticamente una vez.
            _autoEndedThisTurn = true;
            EndPlayerTurn();
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

        public void SetStyleChargesForTest(int value)
        {
            _styleCharges = Mathf.Clamp(value, 0, 5);
        }

        public void SetBonusWorldSwitchesForTest(int value)
        {
            _bonusWorldSwitches = Mathf.Max(0, value);
        }

        public void SetPlayerTypesForTest(ElementType worldAType, ElementType worldBType)
        {
            _playerWorldAType = worldAType;
            _playerWorldBType = worldBType;
        }

        public void SetCurrentWorldForTest(WorldSide world)
        {
            currentWorld = world;
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

            // Fallback a defaults del inspector cuando ConfigureCombat no inyectó
            // tipos válidos (escenas standalone, tests sin override explícito).
            if (_playerWorldAType == ElementType.None) _playerWorldAType = playerWorldATypeDefault;
            if (_playerWorldBType == ElementType.None) _playerWorldBType = playerWorldBTypeDefault;

            int maxHp = _initialPlayerMaxHpOverride.HasValue
                ? Mathf.Max(1, _initialPlayerMaxHpOverride.Value)
                : playerMaxHP;
            _player = new PlayerCombatActor("player", playerDisplayName, maxHp, energyPerTurn, starterDeck, maxHandSize, _random);
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

            _player.HandLimitReached += OnPlayerHandLimitReached;
            _worldSwitchesUsed = 0;
            _styleCharges = 0;
            _bonusWorldSwitches = 0;
            _initialized = true;

            PlanNextEnemyMove();
            BeginPlayerTurn(useStartingHand: true);

            // Hook OnCombatStart: insertado DESPUÉS de BeginPlayerTurn para que
            // ClearBlock(_player) (parte de BeginPlayerTurn) no borre el bloque
            // que un Retazo "+N bloque al iniciar combate" otorgue. En el primer
            // turno ClearBlock es no-op (block = 0), así que disparar aquí es
            // semánticamente correcto: combate inicializado, primer turno comenzó.
            if (TryGetRelicContext(out RunState csRs, out RelicHookDispatcher csDisp))
            {
                CombatStartHookData csData = new CombatStartHookData(
                    csRs, this, csDisp, enemyDefinition,
                    isBoss: _isCurrentCombatBoss, isElite: _isCurrentCombatElite);
                csDisp.Dispatch(RelicHook.OnCombatStart, csData);
            }
        }

        /// <summary>
        /// Configura el combate con un deck y enemigo específicos antes de inicializar.
        /// </summary>
        public void ConfigureCombat(
            List<CardDeckEntry> deck,
            EnemyDefinition enemy,
            int? playerCurrentHpOverride = null,
            int? playerMaxHpOverride = null,
            bool initializeImmediately = true,
            ElementType playerWorldAType = ElementType.None,
            ElementType playerWorldBType = ElementType.None,
            bool isElite = false,
            bool isBoss = false)
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
            // None marca "sin override"; InitializeCombat aplica el default del
            // SerializeField si los campos quedan en None.
            _playerWorldAType = playerWorldAType;
            _playerWorldBType = playerWorldBType;
            _isCurrentCombatElite = isElite;
            _isCurrentCombatBoss = isBoss;
            _externalConfigApplied = true;

            if (initializeImmediately)
            {
                InitializeCombat();
            }
        }

        private void OnPlayerHandLimitReached(int maxHandSize)
        {
            PlayerHandLimitReached?.Invoke(maxHandSize);
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
            _autoEndedThisTurn = false;
            _player.ResetEnergy();
            ClearBlock(_player);

            // Hook OnPlayerTurnStart: post-ResetEnergy + ClearBlock, pre-DrawCards.
            // Permite Retazos que modifican draw size, energía extra, etc.
            if (TryGetRelicContext(out RunState ptRs, out RelicHookDispatcher ptDisp))
            {
                PlayerTurnStartHookData ptData = new PlayerTurnStartHookData(ptRs, this, ptDisp, currentWorld);
                ptDisp.Dispatch(RelicHook.OnPlayerTurnStart, ptData);
            }

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

                case EnemyAIPattern.PhaseBased:
                    // Filtra los moves cuyo rango [MinHpPercent, MaxHpPercent] incluye
                    // el HP% actual del enemigo. Los campos default (0/100) cubren siempre,
                    // por lo que PhaseBased es compatible con SOs sin configurar rangos.
                    int currentHpPercent = (_enemy.MaxHP > 0)
                        ? Mathf.RoundToInt(_enemy.CurrentHP * 100f / _enemy.MaxHP)
                        : 0;
                    List<EnemyMove> availableMoves = new List<EnemyMove>();
                    foreach (EnemyMove candidate in moves)
                    {
                        if (currentHpPercent >= candidate.MinHpPercent &&
                            currentHpPercent <= candidate.MaxHpPercent)
                        {
                            availableMoves.Add(candidate);
                        }
                    }
                    // Fallback defensivo: SO mal configurado (ningún rango cubre el HP actual).
                    if (availableMoves.Count == 0)
                    {
                        availableMoves.AddRange(moves);
                    }
                    int phaseTotal = 0;
                    foreach (EnemyMove m in availableMoves)
                    {
                        phaseTotal += Math.Max(1, m.Weight);
                    }
                    int phaseRoll = _random.Next(Math.Max(1, phaseTotal));
                    int phaseAccum = 0;
                    foreach (EnemyMove m in availableMoves)
                    {
                        phaseAccum += Math.Max(1, m.Weight);
                        if (phaseRoll < phaseAccum)
                        {
                            return m;
                        }
                    }
                    return availableMoves[availableMoves.Count - 1];

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
                case EffectType.Heal:
                    return new HealAction(target, amount);
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

            // Dirección 1: jugador ataca al enemigo. Emite PlayerHitEffectiveness
            // para popups WEAK/RESIST/+ESTILO en UI.
            if (source == _player && target == _enemy)
            {
                return ApplyPlayerToEnemyEffectiveness(sourceElementType, baseAmount);
            }

            // Dirección 2 (DD-018, Sub-PR B): enemigo ataca al jugador. El defensor
            // es PlayerActiveType (derivado del mundo activo). Sin Momentum — ese
            // sistema desaparece en Sub-PR C. Emite EnemyHitEffectiveness para que
            // Sub-PR C aplique cargas y la UI muestre feedback cuando se enchufe.
            if (source == _enemy && target == _player)
            {
                return ApplyEnemyToPlayerEffectiveness(sourceElementType, baseAmount);
            }

            // Cualquier otra combinación (self-damage, tests sintéticos): sin modificadores.
            return baseAmount;
        }

        private int ApplyPlayerToEnemyEffectiveness(ElementType attackerType, int baseAmount)
        {
            // Cartas sin tipo (None) aplican 90% del daño base (DD-002).
            // No pasan por la tabla de efectividad ni modifican cargas.
            // 8º dispatch (Sub-PR 3B, aprobado): permitir que Retazos de modificador
            // de daño afecten también cartas neutras. Mismo contrato mutable que el
            // path tipado — los Retazos mutan data.Amount.
            if (attackerType == ElementType.None)
            {
                int neutralAmount = Math.Max(0, Mathf.RoundToInt(baseAmount * EffectivenessMultipliers.NeutralCardDamage));
                if (TryGetRelicContext(out RunState ndRs, out RelicHookDispatcher ndDisp))
                {
                    DamageDealtHookData ndData = new DamageDealtHookData(
                        ndRs, this, ndDisp, neutralAmount, Effectiveness.Neutro, ElementType.None, _enemy);
                    ndDisp.Dispatch(RelicHook.OnDamageDealt, ndData);
                    neutralAmount = Math.Max(0, ndData.Amount);
                }
                return neutralAmount;
            }

            ElementType defenderType = enemyDefinition != null ? enemyDefinition.ElementType : ElementType.None;
            Effectiveness effectiveness = ElementEffectiveness.GetEffectiveness(attackerType, defenderType);
            float multiplier = EffectivenessMultipliers.For(effectiveness);
            int finalAmount = Math.Max(0, Mathf.RoundToInt(baseAmount * multiplier));

            // Contador de Estilo: +1 carga al hacer SuperEficaz con daño real.
            // Lógica de "5 cargas → +1 switch (no acumulable) + reset" centralizada
            // en IncrementStyleCharges (golden rule §4) — la comparten este path
            // orgánico y RelicGrantStyleCharge para Retazos.
            bool styleChargeGranted = false;
            if (effectiveness == Effectiveness.SuperEficaz && finalAmount > 0)
            {
                IncrementStyleCharges(1);
                styleChargeGranted = true;
            }

            // Hook OnDamageDealt: post-efectividad y post-style-charge, pre-event.
            // Los Retazos pueden mutar finalAmount; el valor final es el que se
            // pasa a new DamageAction(...). Cadena secuencial por AcquisitionOrder.
            if (TryGetRelicContext(out RunState ddRs, out RelicHookDispatcher ddDisp))
            {
                DamageDealtHookData ddData = new DamageDealtHookData(
                    ddRs, this, ddDisp, finalAmount, effectiveness, attackerType, _enemy);
                ddDisp.Dispatch(RelicHook.OnDamageDealt, ddData);
                finalAmount = Math.Max(0, ddData.Amount);
            }

            PlayerHitEffectiveness?.Invoke(effectiveness, styleChargeGranted);
            return finalAmount;
        }

        private int ApplyEnemyToPlayerEffectiveness(ElementType attackerType, int baseAmount)
        {
            ElementType defenderType = PlayerActiveType;
            Effectiveness effectiveness = ElementEffectiveness.GetEffectiveness(attackerType, defenderType);
            float multiplier = EffectivenessMultipliers.For(effectiveness);

            int finalAmount = Math.Max(0, Mathf.RoundToInt(baseAmount * multiplier));

            // Contador de Estilo: recibir un hit SuperEficaz enemigo resta 1 carga.
            if (effectiveness == Effectiveness.SuperEficaz && _styleCharges > 0)
            {
                _styleCharges--;
            }

            // Hook OnDamageTaken: post-efectividad, pre-event. Retazos defensivos
            // ("daño recibido -1") mutan Amount aquí.
            if (TryGetRelicContext(out RunState dtRs, out RelicHookDispatcher dtDisp))
            {
                DamageTakenHookData dtData = new DamageTakenHookData(
                    dtRs, this, dtDisp, finalAmount, effectiveness, attackerType, _enemy);
                dtDisp.Dispatch(RelicHook.OnDamageTaken, dtData);
                finalAmount = Math.Max(0, dtData.Amount);
            }

            EnemyHitEffectiveness?.Invoke(effectiveness);
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
                DispatchCombatEnd(victory: true);
                return;
            }

            if (_player != null && _player.CurrentHP <= 0)
            {
                _phase = CombatPhase.Defeat;
                Debug.Log("Player was defeated.");
                DispatchCombatEnd(victory: false);
            }
        }

        // Hook OnCombatEnd: invocado inmediatamente tras setear Victory/Defeat.
        // Los Retazos mutan RunState directamente (Gold += N, etc.) — la
        // ActionQueue ya no se procesa post-fin de combate ([CERRADO 3]).
        private void DispatchCombatEnd(bool victory)
        {
            if (TryGetRelicContext(out RunState ceRs, out RelicHookDispatcher ceDisp))
            {
                // Fix H1/H2: el HP del run = HP del actor al cerrar combate, ANTES de
                // que los Retazos OnCombatEnd lean/muten RunState. Sin esto, ceRs
                // conserva el HP PRE-combate y BattleFlowController lo sincronizaba
                // tarde (pisando curaciones de fin de combate).
                if (_player != null)
                {
                    ceRs.PlayerCurrentHP = _player.CurrentHP;
                    ceRs.PlayerMaxHP = _player.MaxHP;
                }

                CombatEndHookData ceData = new CombatEndHookData(
                    ceRs, this, ceDisp, victory, enemyDefinition,
                    isBoss: _isCurrentCombatBoss, isElite: _isCurrentCombatElite);
                ceDisp.Dispatch(RelicHook.OnCombatEnd, ceData);
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

            if (!debugUnlimitedWorldSwitches && _worldSwitchesUsed >= TotalAvailableWorldSwitches)
            {
                Debug.LogWarning("World change limit reached for this combat.");
                return false;
            }

            WorldSide previous = currentWorld;
            currentWorld = currentWorld == WorldSide.A ? WorldSide.B : WorldSide.A;

            // Hook OnWorldSwitch: después de mutar currentWorld, antes de
            // incrementar _worldSwitchesUsed. Permite Retazos de cambio (DD-017).
            if (TryGetRelicContext(out RunState wsRs, out RelicHookDispatcher wsDisp))
            {
                WorldSwitchHookData wsData = new WorldSwitchHookData(wsRs, this, wsDisp, previous, currentWorld);
                wsDisp.Dispatch(RelicHook.OnWorldSwitch, wsData);
            }

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
        /// Valida fase/energía/cargas de Estilo, remueve la carta de la mano y encola acciones.
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

            if (!_player.SpendEnergy(activeCard.Cost))
            {
                Debug.LogWarning("Not enough energy to play card.");
                return false;
            }

            _player.RemoveCardFromHand(cardEntry);

            ICombatActor target = explicitTarget ?? GetDefaultOpponent(_player);
            QueueEffects(activeCard.Effects, _player, target, activeCard.ElementType);

            prepared = new PreparedCardPlay(cardEntry, activeCard, target, activeCard.Type == CardType.Attack);
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

            _resolvingCard = true;
            try
            {
                _actionQueue.ProcessAll();
                _player.DiscardCard(prepared.Entry);

                // Hook OnCardPlayed: post-ProcessAll y post-DiscardCard, pre-CheckCombatEndConditions
                // ([CERRADO 2]). Si un Retazo encola daño extra que mata al enemigo,
                // CheckCombatEndConditions detecta la victoria normalmente.
                if (TryGetRelicContext(out RunState cpRs, out RelicHookDispatcher cpDisp))
                {
                    CardPlayedHookData cpData = new CardPlayedHookData(
                        cpRs, this, cpDisp, prepared.ActiveCard, currentWorld,
                        prepared.ActiveCard != null ? prepared.ActiveCard.Cost : 0);
                    cpDisp.Dispatch(RelicHook.OnCardPlayed, cpData);
                }

                CheckCombatEndConditions();
            }
            finally
            {
                _resolvingCard = false;
            }
        }

        private bool HasAnyPlayableCardInHand()
        {
            if (_player == null)
            {
                return false;
            }

            IReadOnlyList<CardDeckEntry> hand = _player.Hand;
            if (hand.Count == 0)
            {
                return false;
            }

            foreach (CardDeckEntry entry in hand)
            {
                if (CanPlayCard(entry))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Devuelve si una carta es jugable en el estado actual.
        /// No tiene side effects ni consume recursos; úsalo desde UI y auto-end turn.
        /// </summary>
        public bool CanPlayCard(CardDeckEntry entry)
        {
            if (_player == null || entry == null)
            {
                return false;
            }

            if (_phase != CombatPhase.PlayerTurn || IsCombatFinished)
            {
                return false;
            }

            CardDefinition activeCard = GetActiveCardDefinition(entry);
            if (activeCard == null)
            {
                return false;
            }

            return _player.CanPayEnergy(activeCard.Cost);
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
                    case EnemyIntentType.Defend when effect.effectType == EffectType.Heal:
                        total += effect.value;
                        break;
                }
            }

            return total;
        }

        // ───────── Retazos: API delegada y plomería del dispatcher ─────────
        // Los métodos RelicGrant*/RelicEnqueueExtraDamage son el contrato que
        // RelicHookContext consume desde los IRelicEffect. Viven aquí porque
        // todos mutan estado privado de combate (energía del jugador, cargas
        // de Estilo, _bonusWorldSwitches, _actionQueue). Guards comunes:
        // early-return si IsCombatFinished, validación actor != null donde aplique.

        /// <summary>
        /// Pull silencioso del dispatcher de RunSession. Devuelve false (no-op
        /// limpio) si no hay sesión — caso de tests legacy / escenas standalone
        /// que instancian TurnManager sin pasar por RunSession.
        /// </summary>
        private bool TryGetRelicContext(out RunState runState, out RelicHookDispatcher dispatcher)
        {
            RunSession session = RunSession.Instance;
            if (session != null && session.RelicDispatcher != null)
            {
                runState = session.State;
                dispatcher = session.RelicDispatcher;
                return true;
            }
            runState = null;
            dispatcher = null;
            return false;
        }

        /// <summary>
        /// Aplica la regla §4 de GOLDEN_RULES (Contador de Estilo): incrementa
        /// _styleCharges por amount; si llega a 5 con _bonusWorldSwitches == 0,
        /// otorga 1 switch extra (no acumulable) y resetea las cargas a 0.
        /// Llamado desde ApplyPlayerToEnemyEffectiveness (cuando un SuperEficaz
        /// orgánico genera +1 carga) y desde RelicGrantStyleCharge (cuando un
        /// Retazo otorga cargas vía RelicHookContext). Una sola fuente de
        /// verdad para la regla del threshold.
        /// </summary>
        private void IncrementStyleCharges(int amount)
        {
            _styleCharges += amount;
            if (_styleCharges >= 5 && _bonusWorldSwitches == 0)
            {
                _bonusWorldSwitches = 1;
                _styleCharges = 0;
            }
            else if (_styleCharges > 5)
            {
                // Cap a 5 (D-B). Si ya hay un switch bonus pendiente (_bonusWorldSwitches
                // != 0), la rama de arriba no dispara y las cargas crecerían sin techo
                // vía Retazos (RelicGrantStyleCharge). El Contador de Estilo nunca debe
                // exceder 5 (golden rule §4); SetStyleChargesForTest ya clampaba, este
                // es el path de producción.
                _styleCharges = 5;
            }
        }

        internal void RelicGrantBlock(ICombatActor actor, int amount)
        {
            if (IsCombatFinished || actor == null || amount <= 0) return;
            actor.GainBlock(amount);
        }

        internal void RelicGrantHeal(ICombatActor actor, int amount)
        {
            if (IsCombatFinished || actor == null || amount <= 0) return;
            actor.Heal(amount);
        }

        internal void RelicGrantDrawCards(ICombatActor actor, int amount)
        {
            if (IsCombatFinished || actor == null || amount <= 0) return;
            actor.DrawCards(amount);
        }

        internal void RelicGrantEnergy(ICombatActor actor, int amount)
        {
            if (IsCombatFinished || amount <= 0) return;
            // La energía es concepto del jugador; non-player actors son no-op.
            if (actor == _player && _player != null)
            {
                _player.GainEnergy(amount);
            }
        }

        internal void RelicGrantStyleCharge(int amount)
        {
            if (IsCombatFinished || amount <= 0) return;
            IncrementStyleCharges(amount);
        }

        internal void RelicGrantBonusWorldSwitch()
        {
            if (IsCombatFinished) return;
            // Respeta el "no acumulable" de la golden rule §4.
            if (_bonusWorldSwitches == 0)
            {
                _bonusWorldSwitches = 1;
            }
        }

        internal void RelicEnqueueExtraDamage(ICombatActor target, int amount, ElementType type)
        {
            if (IsCombatFinished || target == null || amount <= 0 || _actionQueue == null) return;
            // Daño RAW: NO pasa por ApplyPlayerToEnemyEffectiveness, NO aplica
            // multiplicador WEAK/RESIST, NO otorga ni resta cargas de Estilo,
            // y NO re-dispara OnDamageDealt (deliberado: previene loops infinitos
            // cuando dos Retazos se modifican mutuamente). Para daño extra que
            // SÍ se beneficie de la efectividad, mutar data.Amount en OnDamageDealt.
            // El parámetro 'type' es metadata para futuros usos (animaciones/UI
            // en 3B); en 3A no se aplica al cálculo porque sería duplicar la
            // efectividad orgánica.
            Action<int> onEnemyDamage = null;
            if (target == _enemy)
            {
                onEnemyDamage = dmg =>
                {
                    if (dmg > 0)
                    {
                        EnemyTookDamage?.Invoke(dmg);
                    }
                };
            }
            _actionQueue.Enqueue(new DamageAction(_player, target, amount, onEnemyDamage));
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

        public PreparedCardPlay(CardDeckEntry entry, CardDefinition activeCard, ICombatActor target, bool isAttackCard)
        {
            Entry = entry;
            ActiveCard = activeCard;
            Target = target;
            IsAttackCard = isAttackCard;
        }
    }
}

