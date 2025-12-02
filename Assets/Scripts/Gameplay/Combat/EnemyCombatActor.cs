using System;
using RoguelikeCardBattler.Gameplay.Enemies;

namespace RoguelikeCardBattler.Gameplay.Combat
{
    public class EnemyCombatActor : ICombatActor
    {
        public EnemyDefinition Definition { get; }

        public string Id => Definition?.Id ?? "enemy";
        public string DisplayName => Definition?.EnemyName ?? "Enemy";
        public int CurrentHP { get; private set; }
        public int MaxHP { get; }
        public int Block { get; private set; }

        public EnemyCombatActor(EnemyDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            MaxHP = Math.Max(1, Definition.MaxHP);
            CurrentHP = MaxHP;
            Block = Math.Max(0, Definition.BaseBlock);
        }

        public void TakeDamage(int amount, ICombatActor source = null)
        {
            if (amount <= 0 || CurrentHP <= 0)
            {
                return;
            }

            int remaining = amount;
            if (Block > 0)
            {
                int absorbed = Math.Min(Block, remaining);
                LoseBlock(absorbed);
                remaining -= absorbed;
            }

            if (remaining <= 0)
            {
                return;
            }

            CurrentHP = Math.Max(0, CurrentHP - remaining);
        }

        public void GainBlock(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Block += amount;
        }

        public void LoseBlock(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Block = Math.Max(0, Block - amount);
        }

        public void DrawCards(int amount)
        {
            // Enemigos básicos no roban cartas; reservado para futuras mecánicas.
        }
    }
}

