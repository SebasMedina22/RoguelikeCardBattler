using System;

namespace RoguelikeCardBattler.Gameplay.Combat.Actions
{
    public sealed class DrawCardsAction : IGameAction
    {
        private readonly ICombatActor _target;
        private readonly int _amount;

        public DrawCardsAction(ICombatActor target, int amount)
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

            _target.DrawCards(_amount);
        }

        public override string ToString() =>
            $"DrawCardsAction({_target?.DisplayName}, amount: {_amount})";
    }
}

