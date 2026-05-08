using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Gameplay.Enemies;
using RoguelikeCardBattler.Run;

namespace RoguelikeCardBattler.Gameplay.Relics.Hooks
{
    /// <summary>
    /// Disparado dentro de InitializeCombat, DESPUÉS del primer BeginPlayerTurn
    /// (para evitar que ClearBlock borre el bloque que un Retazo "+N bloque al
    /// iniciar combate" haya otorgado — ver §Cambios en TurnManager del spec).
    /// Observable: no tiene campos mutables.
    /// Nota (3A): IsBoss/IsElite removidos — viven en NodeType del MapNode, no
    /// en EnemyDefinition, y TurnManager hoy no recibe la referencia del nodo.
    /// Cuando un Retazo concreto lo necesite, extender ConfigureCombat/
    /// RunCombatConfig para inyectar el NodeType activo (ver _insights.md).
    /// </summary>
    public class CombatStartHookData : RelicHookContext
    {
        public EnemyDefinition Enemy { get; }

        public CombatStartHookData(
            RunState runState,
            TurnManager turnManager,
            RelicHookDispatcher dispatcher,
            EnemyDefinition enemy)
            : base(runState, turnManager, dispatcher)
        {
            Enemy = enemy;
        }
    }
}
