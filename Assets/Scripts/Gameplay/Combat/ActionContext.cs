using System;

namespace RoguelikeCardBattler.Gameplay.Combat
{
    /// <summary>
    /// Shared data passed to actions during execution. Keeps the design extensible for
    /// audio/visual hooks or service lookups later on.
    /// </summary>
    public class ActionContext
    {
        public ActionQueue Queue { get; }
        public IServiceProvider Services { get; }

        public ActionContext(ActionQueue queue, IServiceProvider services = null)
        {
            Queue = queue ?? throw new ArgumentNullException(nameof(queue));
            Services = services;
        }
    }
}

