using System.Collections.Generic;
using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Run;
using RoguelikeCardBattler.Run.Campfire;

namespace RoguelikeCardBattler.Gameplay.Relics.Hooks
{
    /// <summary>
    /// Disparado por <c>CampfireNodeController</c> después de construir las
    /// opciones base (Descansar, Mejorar carta) y antes de mostrar el panel.
    /// Los Retazos suscritos pueden agregar opciones a la lista mutable.
    /// TurnManager es null: estamos fuera de combate (los Grant* del context
    /// hacen no-op por guard).
    /// </summary>
    public class CampfireOptionsBuiltHookData : RelicHookContext
    {
        public List<CampfireOption> Options { get; }

        public CampfireOptionsBuiltHookData(
            RunState runState,
            RelicHookDispatcher dispatcher,
            List<CampfireOption> options)
            : base(runState, null, dispatcher)
        {
            Options = options;
        }
    }
}
