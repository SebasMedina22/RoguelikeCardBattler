using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Gameplay.Enemies;
using RoguelikeCardBattler.Run;

namespace RoguelikeCardBattler.Gameplay.Relics.Hooks
{
    /// <summary>
    /// Disparado dentro de InitializeCombat, DESPUÉS del primer BeginPlayerTurn
    /// (para evitar que ClearBlock borre el bloque que un Retazo "+N bloque al
    /// iniciar combate" haya otorgado — ver §Cambios en TurnManager del spec).
    /// Observable: no tiene campos mutables. IsBoss/IsElite cableados en 3B
    /// vía ConfigureCombat (BattleFlowController los deriva del NodeType activo).
    /// </summary>
    public class CombatStartHookData : RelicHookContext
    {
        public EnemyDefinition Enemy { get; }
        public bool IsBoss { get; }
        public bool IsElite { get; }

        public CombatStartHookData(
            RunState runState,
            TurnManager turnManager,
            RelicHookDispatcher dispatcher,
            EnemyDefinition enemy,
            bool isBoss = false,
            bool isElite = false)
            : base(runState, turnManager, dispatcher)
        {
            Enemy = enemy;
            IsBoss = isBoss;
            IsElite = isElite;
        }
    }
}
