using System;

namespace RoguelikeCardBattler.Gameplay.Combat.Actions
{
    public sealed class BlockAction : IGameAction
    {
        private readonly ICombatActor _target;
        private readonly int _amount;

        public BlockAction(ICombatActor target, int amount)
        {
            _target = target;
            _amount = Math.Max(0, amount);
        }

        public void Execute(ActionContext context)
        {
            if (_amount <= 0 || _target == null)
            {
                return;
            }

            _target.GainBlock(_amount);
        }

        public override string ToString() =>
            $"BlockAction({_target?.DisplayName}, amount: {_amount})";
    }
}

