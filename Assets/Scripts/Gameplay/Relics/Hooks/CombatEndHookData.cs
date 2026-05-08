using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Gameplay.Enemies;
using RoguelikeCardBattler.Run;

namespace RoguelikeCardBattler.Gameplay.Relics.Hooks
{
    /// <summary>
    /// Disparado dentro de CheckCombatEndConditions, inmediatamente después de
    /// setear _phase = Victory o _phase = Defeat. Los Retazos mutan RunState
    /// directamente desde aquí (ej: ctx.RunState.Gold += 5) — NO se encolan
    /// acciones, la ActionQueue ya no se procesa post-fin de combate ([CERRADO 3]).
    /// </summary>
    public class CombatEndHookData : RelicHookContext
    {
        public bool Victory { get; }
        public EnemyDefinition Enemy { get; }
        public bool IsBoss { get; }
        public bool IsElite { get; }

        public CombatEndHookData(
            RunState runState,
            TurnManager turnManager,
            RelicHookDispatcher dispatcher,
            bool victory,
            EnemyDefinition enemy,
            bool isBoss = false,
            bool isElite = false)
            : base(runState, turnManager, dispatcher)
        {
            Victory = victory;
            Enemy = enemy;
            IsBoss = isBoss;
            IsElite = isElite;
        }
    }
}
