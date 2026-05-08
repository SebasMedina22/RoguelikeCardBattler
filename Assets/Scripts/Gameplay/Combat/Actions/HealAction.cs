using System;

namespace RoguelikeCardBattler.Gameplay.Combat.Actions
{
    public sealed class HealAction : IGameAction
    {
        private readonly ICombatActor _target;
        private readonly int _amount;

        public HealAction(ICombatActor target, int amount)
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

            _target.Heal(_amount);
        }

        public override string ToString() =>
            $"HealAction({_target?.DisplayName}, amount: {_amount})";
    }
}
