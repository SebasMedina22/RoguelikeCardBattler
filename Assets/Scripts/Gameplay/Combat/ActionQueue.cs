using System;
using System.Collections.Generic;

namespace RoguelikeCardBattler.Gameplay.Combat
{
    /// <summary>
    /// Deterministic FIFO queue that resolves gameplay actions one at a time.
    /// </summary>
    public class ActionQueue
    {
        private readonly Queue<IGameAction> _pending = new Queue<IGameAction>();
        private bool _isProcessing;

        public event Action<IGameAction> ActionStarted;
        public event Action<IGameAction> ActionCompleted;

        public ActionContext Context { get; }

        public int PendingCount => _pending.Count;

        public ActionQueue(IServiceProvider services = null)
        {
            Context = new ActionContext(this, services);
        }

        public void Enqueue(IGameAction action)
        {
            if (action == null)
            {
                return;
            }

            _pending.Enqueue(action);
        }

        public void EnqueueRange(IEnumerable<IGameAction> actions)
        {
            if (actions == null)
            {
                return;
            }

            foreach (IGameAction action in actions)
            {
                Enqueue(action);
            }
        }

        public bool ProcessNext()
        {
            if (_pending.Count == 0)
            {
                return false;
            }

            IGameAction action = _pending.Dequeue();
            ActionStarted?.Invoke(action);

            action.Execute(Context);

            ActionCompleted?.Invoke(action);
            return true;
        }

        public void ProcessAll()
        {
            if (_isProcessing)
            {
                return;
            }

            _isProcessing = true;
            while (ProcessNext())
            {
                // Actions can enqueue more work while processing.
            }

            _isProcessing = false;
        }

        public void Clear() => _pending.Clear();
    }
}

