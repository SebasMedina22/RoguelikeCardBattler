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
    /// La mutación directa de HP es correcta: DispatchCombatEnd sincroniza
    /// RunState.PlayerCurrentHP/PlayerMaxHP desde el actor JUSTO antes de este
    /// dispatch (Opción B del spec fix_combat_end_hp_sync), así los hooks leen/mutan
    /// HP fresco post-combate y BattleFlowController ya no lo pisa después.
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
