using System;

namespace RoguelikeCardBattler.Gameplay.Combat.Actions
{
    public sealed class DamageAction : IGameAction
    {
        private readonly ICombatActor _source;
        private readonly ICombatActor _target;
        private readonly int _amount;
        private readonly Action<int> _onDamageApplied;

        public DamageAction(ICombatActor source, ICombatActor target, int amount, Action<int> onDamageApplied = null)
        {
            _source = source;
            _target = target;
            _amount = Math.Max(0, amount);
            _onDamageApplied = onDamageApplied;
        }

        public void Execute(ActionContext context)
        {
            if (_amount <= 0 || _target == null)
            {
                return;
            }

            int hpBefore = _target.CurrentHP;
            _target.TakeDamage(_amount, _source);
            int damageApplied = Math.Max(0, hpBefore - (_target?.CurrentHP ?? hpBefore));
            if (damageApplied > 0)
            {
                _onDamageApplied?.Invoke(damageApplied);
            }
        }

        public override string ToString() =>
            $"DamageAction({_source?.DisplayName} -> {_target?.DisplayName}, amount: {_amount})";
    }
}

