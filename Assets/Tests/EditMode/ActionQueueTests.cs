using System.Collections.Generic;
using NUnit.Framework;
using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Gameplay.Combat.Actions;

namespace RoguelikeCardBattler.Tests.EditMode
{
    public class ActionQueueTests
    {
        private class TestActor : ICombatActor
        {
            public string Id { get; } = "actor";
            public string DisplayName { get; } = "Actor";
            public int CurrentHP { get; private set; } = 10;
            public int MaxHP { get; } = 10;
            public int Block { get; private set; }

            public readonly List<string> Log = new List<string>();

            public void TakeDamage(int amount, ICombatActor source = null)
            {
                Log.Add($"Damage:{amount}");
            }

            public void GainBlock(int amount)
            {
                Log.Add($"Block:{amount}");
            }

            public void LoseBlock(int amount)
            {
                Block = 0;
            }

            public void DrawCards(int amount)
            {
                Log.Add($"Draw:{amount}");
            }
        }

        [Test]
        public void ActionsExecuteInOrder()
        {
            var target = new TestActor();
            var queue = new ActionQueue();

            queue.Enqueue(new DamageAction(null, target, 5));
            queue.Enqueue(new BlockAction(target, 3));
            queue.Enqueue(new DrawCardsAction(target, 2));

            queue.ProcessAll();

            CollectionAssert.AreEqual(
                new[] { "Damage:5", "Block:3", "Draw:2" },
                target.Log);
        }

        [Test]
        public void NullOrNoOpActionsDoNotBreakProcessing()
        {
            var actor = new TestActor();
            var queue = new ActionQueue();

            queue.Enqueue(null);
            queue.Enqueue(new DamageAction(null, actor, 0));
            queue.Enqueue(new BlockAction(null, 5));
            queue.Enqueue(new DrawCardsAction(actor, 1));

            queue.ProcessAll();

            CollectionAssert.AreEqual(new[] { "Draw:1" }, actor.Log);
        }
    }
}

