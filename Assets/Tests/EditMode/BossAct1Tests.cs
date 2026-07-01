using System.Collections.Generic;
using NUnit.Framework;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Gameplay.Enemies;

namespace RoguelikeCardBattler.Tests.EditMode
{
    /// <summary>
    /// M5 Sub-PR A — boss del Acto 1 con fases por HP (PhaseBased) + dos tipos
    /// (transdim, reuso 4c). Cubre la SELECCIÓN de move por fase (caso 1 del spec):
    /// con HP > 50% el boss solo planea moves de fase 1 ([51,100]); con HP ≤ 50%
    /// solo de fase 2 ([0,50], DC3 obligatoria al 50%). No re-asierta el selector
    /// PhaseBased (ya está implementado y cubierto) — asierta que el move PLANEADO
    /// pertenece a la fase correcta según el HP% del boss. Además valida la regla
    /// ASIMÉTRICA de tipos del boss (DC2/§6) contra la matriz de ElementEffectiveness
    /// en CÓDIGO: el boss pega SuperEficaz en ambos mundos, pero el jugador solo
    /// devuelve SuperEficaz en Mundo A (Mundo B favorece al boss).
    ///
    /// Plantilla: EnemyTransdimTests (mismo CreateTurnManager con SetTestConfig +
    /// SetTestData + InitializeCombat). Extiende CombatTestBase; no hizo falta
    /// agregarle factories nuevas — CreateEnemyDefinition (tipos/ancla) y
    /// CreateEnemyMove (HP% + intent) ya cubren todo lo que el boss necesita.
    ///
    /// FUERA de scope de A: el "umbral del Desfase 3→2 por fase" (caso 2 del spec)
    /// es parte del Desfase Dimensional (Sub-PR C), que todavía no existe; sus
    /// campos de SO y su contador de cartas se agregan ahí.
    /// </summary>
    public class BossAct1Tests : CombatTestBase
    {
        // Rangos de fase espejados del SO del boss (ver BossConfigSetup).
        private const int Phase1Min = 51;
        private const int Phase1Max = 100;
        private const int Phase2Min = 0;
        private const int Phase2Max = 50;

        // Boss de prueba: PhaseBased, transdim, con kit ataque/defensa por fase.
        // maxHp chico para que cruzar el 50% sea aritmética limpia en el test (los
        // rangos de fase son por porcentaje, así que el comportamiento es idéntico
        // al boss real de 140 HP).
        private EnemyDefinition CreatePhaseBoss(int maxHp, ElementType typeA, ElementType typeB)
        {
            var moves = new List<EnemyMove>
            {
                CreateEnemyMove("p1_atk", "P1 Ataque", string.Empty,
                    new List<EffectRef> { CreateEffect(EffectType.Damage, 6, EffectTarget.SingleEnemy) },
                    intentType: EnemyIntentType.Attack, minHpPercent: Phase1Min, maxHpPercent: Phase1Max),
                CreateEnemyMove("p1_def", "P1 Defensa", string.Empty,
                    new List<EffectRef> { CreateEffect(EffectType.Block, 6, EffectTarget.Self) },
                    intentType: EnemyIntentType.Defend, minHpPercent: Phase1Min, maxHpPercent: Phase1Max),
                CreateEnemyMove("p2_atk", "P2 Ataque", string.Empty,
                    new List<EffectRef> { CreateEffect(EffectType.Damage, 6, EffectTarget.SingleEnemy) },
                    intentType: EnemyIntentType.Attack, minHpPercent: Phase2Min, maxHpPercent: Phase2Max),
                CreateEnemyMove("p2_def", "P2 Defensa", string.Empty,
                    new List<EffectRef> { CreateEffect(EffectType.Block, 6, EffectTarget.Self) },
                    intentType: EnemyIntentType.Defend, minHpPercent: Phase2Min, maxHpPercent: Phase2Max),
            };

            return CreateEnemyDefinition(
                "boss_test", "Boss Test", maxHp, EnemyAIPattern.PhaseBased, moves,
                elementType: typeA, typeWorldB: typeB, isAnchor: false);
        }

        private TurnManager CreateTurnManager(CardDefinition card, EnemyDefinition enemy, int playerMaxHp = 40)
        {
            var deck = new List<CardDeckEntry> { CreateSingleCardEntry(card) };
            var go = CreateGameObject("TurnManager");
            var manager = AddComponent<TurnManager>(go);
            manager.SetTestConfig(maxHp: playerMaxHp, energy: 3, startingHand: 1, cardsPerTurnCount: 1);
            manager.SetTestData(deck, enemy);
            return manager;
        }

        // Carta Blanco para golpear al boss Blanco(A) en Neutro (mismo tipo) → daño
        // limpio sin multiplicador de efectividad, para controlar el HP% del boss.
        private CardDefinition CreateCleanStrike(int damage)
        {
            return CreateCardWithElement(
                "strike_blanco", CardType.Attack, CardTarget.SingleEnemy, cost: 0, ElementType.Blanco,
                CreateEffect(EffectType.Damage, damage, EffectTarget.SingleEnemy));
        }

        // ── Caso 1a: HP > 50% → planea move de fase 1 ───────────────────────────
        [Test]
        public void PhaseBased_AboveHalfHp_PlansPhase1Move()
        {
            var boss = CreatePhaseBoss(maxHp: 20, ElementType.Blanco, ElementType.Azul);
            var manager = CreateTurnManager(CreateCleanStrike(12), boss);
            manager.InitializeCombat();

            // Boss a 20/20 = 100% → InitializeCombat planea un move de fase 1.
            Assert.AreEqual(20, manager.EnemyHP);
            EnemyMove planned = manager.PlannedEnemyMove;
            Assert.IsNotNull(planned, "Debe haber un move planeado tras InitializeCombat.");
            Assert.AreEqual(Phase1Min, planned.MinHpPercent, "A 100% HP el move planeado debe ser de fase 1 [51,100].");
            Assert.AreEqual(Phase1Max, planned.MaxHpPercent);
        }

        // ── Caso 1b: HP ≤ 50% → planea move de fase 2 ───────────────────────────
        [Test]
        public void PhaseBased_BelowHalfHp_PlansPhase2Move()
        {
            var boss = CreatePhaseBoss(maxHp: 20, ElementType.Blanco, ElementType.Azul);
            var manager = CreateTurnManager(CreateCleanStrike(12), boss);
            manager.InitializeCombat();

            // Golpe Neutro (Blanco vs Blanco) = 12 → boss a 8/20 = 40% (≤ 50%).
            manager.PlayCard(manager.PlayerHand[0]);
            Assert.AreEqual(8, manager.EnemyHP, "Blanco vs Blanco = Neutro → 12 daño limpio.");

            // Cierra el turno: el enemigo ejecuta su move (fase 1, planeado a 100%)
            // y RE-PLANEA a su HP actual (40%) → debe quedar un move de fase 2.
            manager.EndPlayerTurn();

            Assert.Greater(manager.EnemyHP, 0, "El boss sigue vivo para re-planear.");
            EnemyMove planned = manager.PlannedEnemyMove;
            Assert.IsNotNull(planned);
            Assert.AreEqual(Phase2Min, planned.MinHpPercent, "A ≤50% HP el move planeado debe ser de fase 2 [0,50].");
            Assert.AreEqual(Phase2Max, planned.MaxHpPercent);
        }

        // ── Caso 1c: el 50% exacto cae en fase 2 (DC3 "obligatoria al 50%") ──────
        [Test]
        public void PhaseBased_ExactlyHalfHp_PlansPhase2Move()
        {
            var boss = CreatePhaseBoss(maxHp: 20, ElementType.Blanco, ElementType.Azul);
            var manager = CreateTurnManager(CreateCleanStrike(10), boss);
            manager.InitializeCombat();

            manager.PlayCard(manager.PlayerHand[0]);   // 10 daño limpio → 10/20 = 50%.
            Assert.AreEqual(10, manager.EnemyHP);
            manager.EndPlayerTurn();

            EnemyMove planned = manager.PlannedEnemyMove;
            Assert.IsNotNull(planned);
            Assert.AreEqual(Phase2Max, planned.MaxHpPercent,
                "El 50% exacto cae en fase 2: el rango [0,50] incluye 50 y [51,100] lo excluye.");
        }

        // ── Caso 1 (regresión transdim): el boss PhaseBased conmuta su tipo activo
        //     con el mundo igual que cualquier transdim (reuso 4c) ───────────────
        [Test]
        public void PhaseBoss_IsTransdimensional_ActiveTypeFollowsWorld()
        {
            var boss = CreatePhaseBoss(maxHp: 20, ElementType.Blanco, ElementType.Azul);
            Assert.IsTrue(boss.IsTransdimensional, "El boss es transdim (dos tipos, no ancla).");

            var manager = CreateTurnManager(CreateCleanStrike(1), boss);
            manager.InitializeCombat();

            Assert.AreEqual(TurnManager.WorldSide.A, manager.CurrentWorld);
            Assert.AreEqual(ElementType.Blanco, manager.EnemyElementType, "Mundo A → tipo de A (Blanco).");
            manager.SetCurrentWorldForTest(TurnManager.WorldSide.B);
            Assert.AreEqual(ElementType.Azul, manager.EnemyElementType, "Mundo B → tipo de B (Azul).");
        }

        // ── DC2 / §6 (regla ASIMÉTRICA del boss) ────────────────────────────────
        // §6/DD-004 pide dos tipos: 1 SuperEficaz CONTRA el jugador + 1 que sea
        // DEBILIDAD del jugador. La elección (Blanco A / Azul B vs jugador de ref.
        // Rojo A / Amarillo B) lo cumple, pero la asimetría vive en el eje
        // JUGADOR→BOSS, no en el ofensivo del boss:
        //   - El boss pega SuperEficaz al jugador en AMBOS mundos (boss letal).
        //   - PERO el jugador solo devuelve SuperEficaz en Mundo A (Rojo→Blanco) →
        //     Mundo A es EXPLOTABLE; en Mundo B pega PocoEficaz (Amarillo→Azul) →
        //     Mundo B FAVORECE al boss = la "debilidad del jugador" de §6.
        // Validado contra la matriz de CÓDIGO (no la tabla §3). Espeja los tipos de
        // BossConfigSetup y los defaults de jugador del TurnManager.
        [Test]
        public void BossTypeChoice_SatisfiesAsymmetricBossRule_PerCodeMatrix()
        {
            // Eje BOSS→JUGADOR: el boss es SuperEficaz atacando al jugador en ambos mundos.
            Assert.AreEqual(Effectiveness.SuperEficaz,
                ElementEffectiveness.GetEffectiveness(ElementType.Blanco, ElementType.Rojo),
                "Mundo A: el boss (Blanco) pega SuperEficaz al jugador de referencia (Rojo).");
            Assert.AreEqual(Effectiveness.SuperEficaz,
                ElementEffectiveness.GetEffectiveness(ElementType.Azul, ElementType.Amarillo),
                "Mundo B: el boss (Azul) pega SuperEficaz al jugador de referencia (Amarillo).");

            // Eje JUGADOR→BOSS: acá vive la asimetría (la mitad "debilidad" de §6).
            Assert.AreEqual(Effectiveness.SuperEficaz,
                ElementEffectiveness.GetEffectiveness(ElementType.Rojo, ElementType.Blanco),
                "Mundo A es EXPLOTABLE: el jugador de ref. (Rojo) pega SuperEficaz al tipo A del boss (Blanco).");
            Assert.AreEqual(Effectiveness.PocoEficaz,
                ElementEffectiveness.GetEffectiveness(ElementType.Amarillo, ElementType.Azul),
                "Mundo B FAVORECE al boss: el jugador de ref. (Amarillo) solo pega PocoEficaz al tipo B del boss (Azul).");
        }
    }
}
