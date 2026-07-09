using System.Collections.Generic;
using NUnit.Framework;
using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Gameplay.Enemies;
using UnityEngine;

namespace RoguelikeCardBattler.Tests.EditMode
{
    /// <summary>
    /// C4 — swap de avatar del enemigo por mundo. Cubre EnemyDefinition.ResolveAvatar(bool):
    /// conmuta a la cara de Mundo B SOLO para transdimensionales con arte de B asignado;
    /// en cualquier otro caso (Mundo A, transdim sin B, tipo-único, ancla) devuelve el
    /// avatar base → cero regresión. No testea el swap visual ni SetRestingSprite (UI, se
    /// valida por eyeball); solo la lógica pura de resolución.
    /// </summary>
    public class EnemyAvatarByWorldTests : CombatTestBase
    {
        // ── Caso 1: transdim con B asignado, Mundo A → avatar base (A) ──────────
        [Test]
        public void ResolveAvatar_Transdim_WorldA_ReturnsAvatarA()
        {
            var spriteA = CreateDummySprite();
            var spriteB = CreateDummySprite();
            var enemy = CreateEnemyDefinition(
                "td", "Transdim", 10, EnemyAIPattern.Sequence, new List<EnemyMove>(),
                elementType: ElementType.Rojo, typeWorldB: ElementType.Azul, isAnchor: false);
            enemy.SetAvatarsForTest(spriteA, spriteB);

            Assert.AreSame(spriteA, enemy.ResolveAvatar(false),
                "Mundo A: aunque sea transdim con B, en A se usa el avatar base.");
        }

        // ── Caso 2: transdim con B asignado, Mundo B → avatar B ─────────────────
        [Test]
        public void ResolveAvatar_Transdim_WorldB_ReturnsAvatarB()
        {
            var spriteA = CreateDummySprite();
            var spriteB = CreateDummySprite();
            var enemy = CreateEnemyDefinition(
                "td", "Transdim", 10, EnemyAIPattern.Sequence, new List<EnemyMove>(),
                elementType: ElementType.Rojo, typeWorldB: ElementType.Azul, isAnchor: false);
            enemy.SetAvatarsForTest(spriteA, spriteB);

            Assert.AreSame(spriteB, enemy.ResolveAvatar(true),
                "Mundo B: transdim con arte de B → conmuta a la cara de B.");
        }

        // ── Caso 3: transdim SIN arte de B → fallback al avatar base ────────────
        [Test]
        public void ResolveAvatar_Transdim_NoAvatarB_FallsBackToAvatarA()
        {
            var spriteA = CreateDummySprite();
            var enemy = CreateEnemyDefinition(
                "td", "Transdim", 10, EnemyAIPattern.Sequence, new List<EnemyMove>(),
                elementType: ElementType.Rojo, typeWorldB: ElementType.Azul, isAnchor: false);
            enemy.SetAvatarsForTest(spriteA, null);

            Assert.AreSame(spriteA, enemy.ResolveAvatar(true),
                "Transdim sin avatar de B → usa el base aunque el mundo sea B (cero regresión).");
        }

        // ── Caso 4: tipo único (no transdim) → nunca conmuta ────────────────────
        [Test]
        public void ResolveAvatar_SingleType_NeverSwaps()
        {
            var spriteA = CreateDummySprite();
            var spriteB = CreateDummySprite();
            var enemy = CreateEnemyDefinition(
                "sg", "Single", 10, EnemyAIPattern.Sequence, new List<EnemyMove>(),
                elementType: ElementType.Rojo, typeWorldB: ElementType.None, isAnchor: false);
            enemy.SetAvatarsForTest(spriteA, spriteB);

            Assert.IsFalse(enemy.IsTransdimensional);
            Assert.AreSame(spriteA, enemy.ResolveAvatar(true),
                "Tipo único: no conmuta aunque el flag de mundo diga B.");
        }

        // ── Caso 5: ancla → nunca conmuta (isAnchor precede a typeWorldB) ───────
        [Test]
        public void ResolveAvatar_Anchor_NeverSwaps()
        {
            var spriteA = CreateDummySprite();
            var spriteB = CreateDummySprite();
            // Ancla con typeWorldB seteado por error → IsTransdimensional=false igual.
            var enemy = CreateEnemyDefinition(
                "an", "Anchor", 10, EnemyAIPattern.Sequence, new List<EnemyMove>(),
                elementType: ElementType.Morado, typeWorldB: ElementType.Azul, isAnchor: true);
            enemy.SetAvatarsForTest(spriteA, spriteB);

            Assert.IsFalse(enemy.IsTransdimensional);
            Assert.AreSame(spriteA, enemy.ResolveAvatar(true),
                "Ancla: nunca conmuta el avatar, aunque tenga typeWorldB y arte de B.");
        }
    }
}
