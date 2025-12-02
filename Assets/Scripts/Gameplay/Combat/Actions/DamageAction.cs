using System;

namespace RoguelikeCardBattler.Gameplay.Combat.Actions
{
    public sealed class DamageAction : IGameAction
    {
        private readonly ICombatActor _source;
        private readonly ICombatActor _target;
        private readonly int _amount;

        public DamageAction(ICombatActor source, ICombatActor target, int amount)
        {
            _source = source;
            _target = target;
            _amount = Math.Max(0, amount);
        }

        public void Execute(ActionContext context)
        {
            if (_amount <= 0 || _target == null)
            {
                return;
            }

            _target.TakeDamage(_amount, _source);
        }

        public override string ToString() =>
            $"DamageAction({_source?.DisplayName} -> {_target?.DisplayName}, amount: {_amount})";
    }
}

