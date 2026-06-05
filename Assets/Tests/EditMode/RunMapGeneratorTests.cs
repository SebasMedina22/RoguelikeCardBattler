using System.Collections.Generic;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Gameplay.Enemies;
using RoguelikeCardBattler.Run;

namespace RoguelikeCardBattler.Tests.EditMode
{
    /// <summary>
    /// Tests EditMode de <see cref="RunMapGenerator"/> (Sub-PR 3F). El refactor del
    /// mapa a scroll horizontal es puramente visual y vive entero en RunMapView; estos
    /// tests blindan que la GENERACIÓN (lógica pura, agnóstica al eje) no cambió:
    /// "misma seed = mismo mapa" + invariantes del DAG. Es la red de seguridad del
    /// refactor (antes de 3F no había ningún test del sistema de mapa).
    ///
    /// Patrón: se construye un <see cref="Act1MapConfig"/> con defaults vía
    /// <c>ScriptableObject.CreateInstance</c> (mismo patrón que <c>GenerateAct1</c>);
    /// el <see cref="EnemyPoolConfig"/> se puebla por reflexión sobre sus campos
    /// privados serializados (no expone setter de debug) para no salir del scope del
    /// spec (sólo se crea este archivo de test).
    /// </summary>
    public class RunMapGeneratorTests
    {
        // ────────────────────────────────────────
        // Helpers
        // ────────────────────────────────────────

        private static Act1MapConfig MakeConfig()
        {
            // Defaults: 8 nodos, start=Combat, end=Boss, pesos por tipo predefinidos.
            return ScriptableObject.CreateInstance<Act1MapConfig>();
        }

        private static EnemyDefinition MakeEnemy(string id)
        {
            EnemyDefinition enemy = ScriptableObject.CreateInstance<EnemyDefinition>();
            enemy.SetDebugData(
                id, id, 30, 0, EnemyAIPattern.RandomWeighted,
                new List<string>(), new List<EnemyMove>(), 1f, null, ElementType.None);
            return enemy;
        }

        // EnemyPoolConfig no tiene SetDebugData; poblamos su campo privado por reflexión.
        // Un único pool de rango amplio con varias entradas de peso distinto basta para
        // que la asignación varíe entre nodos (y por ende sea un test de determinismo real).
        private static EnemyPoolConfig MakePool(params EnemyDefinition[] enemies)
        {
            EnemyPoolConfig pool = ScriptableObject.CreateInstance<EnemyPoolConfig>();
            DepthPool dp = new DepthPool { label = "all", minDepth = 0, maxDepth = 99 };
            float w = 1f;
            foreach (EnemyDefinition e in enemies)
            {
                dp.entries.Add(new EnemyWeightEntry { enemy = e, weight = w });
                w += 1f;
            }

            FieldInfo field = typeof(EnemyPoolConfig).GetField(
                "depthPools", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(pool, new List<DepthPool> { dp });
            return pool;
        }

        // Firma estable de la topología + tipos de un mapa, para comparaciones profundas.
        private static string Signature(ActMap map)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("start=").Append(map.StartNodeId).Append(';');
            foreach (MapNode node in map.Nodes)
            {
                sb.Append(node.Id).Append(':').Append(node.Type).Append("->[");
                sb.Append(string.Join(",", node.Connections));
                sb.Append("];");
            }

            return sb.ToString();
        }

        // ────────────────────────────────────────
        // 1. Determinismo de topología — "misma seed = mismo mapa"
        // ────────────────────────────────────────

        [Test]
        public void Generate_SameSeed_ProducesIdenticalMap()
        {
            Act1MapConfig config = MakeConfig();

            ActMap a = RunMapGenerator.Generate(config, 12345);
            ActMap b = RunMapGenerator.Generate(config, 12345);

            Assert.AreEqual(a.StartNodeId, b.StartNodeId, "Mismo StartNodeId.");
            Assert.AreEqual(a.Nodes.Count, b.Nodes.Count, "Misma cantidad de nodos.");

            for (int i = 0; i < a.Nodes.Count; i++)
            {
                MapNode na = a.GetNode(i);
                MapNode nb = b.GetNode(i);
                Assert.IsNotNull(nb, $"El nodo {i} debe existir en ambos mapas.");
                Assert.AreEqual(na.Type, nb.Type, $"Mismo Type en el nodo {i}.");
                CollectionAssert.AreEqual(na.Connections, nb.Connections,
                    $"Mismas Connections en el nodo {i}.");
            }
        }

        // ────────────────────────────────────────
        // 2. Determinismo de enemigos — "misma seed = mismos enemigos"
        // ────────────────────────────────────────

        [Test]
        public void AssignEnemies_SameSeed_ProducesIdenticalAssignment()
        {
            Act1MapConfig config = MakeConfig();
            EnemyPoolConfig pool = MakePool(MakeEnemy("e1"), MakeEnemy("e2"), MakeEnemy("e3"));

            ActMap a = RunMapGenerator.Generate(config, 12345);
            ActMap b = RunMapGenerator.Generate(config, 12345);

            RunMapGenerator.AssignEnemies(a, pool, 12345);
            RunMapGenerator.AssignEnemies(b, pool, 12345);

            int checkedCombat = 0;
            for (int i = 0; i < a.Nodes.Count; i++)
            {
                MapNode na = a.GetNode(i);
                MapNode nb = b.GetNode(i);
                if (na.Type != NodeType.Combat && na.Type != NodeType.Elite)
                {
                    continue;
                }

                Assert.AreSame(na.SpecificEnemy, nb.SpecificEnemy,
                    $"Mismo enemigo asignado en el nodo {i} (misma seed).");
                checkedCombat++;
            }

            Assert.Greater(checkedCombat, 0, "Debe haber al menos un nodo Combat/Elite para validar.");
        }

        // ────────────────────────────────────────
        // 3. Seeds distintas pueden divergir
        // ────────────────────────────────────────

        [Test]
        public void Generate_DifferentSeeds_ProduceDivergentMaps()
        {
            // Guard suave anti-fragilidad: en vez de comparar dos seeds puntuales
            // (que por azar podrían coincidir), barremos un rango y exigimos que
            // existan al menos 2 firmas distintas — confirma que la seed afecta el mapa.
            Act1MapConfig config = MakeConfig();
            HashSet<string> signatures = new HashSet<string>();
            for (int seed = 1; seed <= 10; seed++)
            {
                signatures.Add(Signature(RunMapGenerator.Generate(config, seed)));
            }

            Assert.Greater(signatures.Count, 1,
                "Seeds distintas deben poder producir mapas distintos (topología o tipos).");
        }

        // ────────────────────────────────────────
        // 4. DAG válido invariante (independiente del eje)
        // ────────────────────────────────────────

        [Test]
        public void Generate_ProducesValidDag()
        {
            Act1MapConfig config = MakeConfig();
            ActMap map = RunMapGenerator.Generate(config, 777);
            int total = map.Nodes.Count;

            // Nodo 0 = start: no debe tener aristas entrantes.
            foreach (MapNode node in map.Nodes)
            {
                CollectionAssert.DoesNotContain(node.Connections, 0,
                    $"El nodo start (0) no puede tener entrantes (desde {node.Id}).");
            }

            // Nodo total-1 = end: no debe tener salientes.
            MapNode end = map.GetNode(total - 1);
            Assert.AreEqual(0, end.Connections.Count, "El nodo end no debe tener salientes.");

            // Todas las conexiones dentro de rango [0, total).
            foreach (MapNode node in map.Nodes)
            {
                foreach (int conn in node.Connections)
                {
                    Assert.GreaterOrEqual(conn, 0, $"connId fuera de rango en nodo {node.Id}.");
                    Assert.Less(conn, total, $"connId fuera de rango en nodo {node.Id}.");
                }
            }
        }

        // ────────────────────────────────────────
        // 5. Start/end forzados (pin defensivo, no se ve afectado por el refactor visual)
        // ────────────────────────────────────────

        [Test]
        public void Generate_ForcesStartAndEndTypes()
        {
            Act1MapConfig config = MakeConfig();
            ActMap map = RunMapGenerator.Generate(config, 999);
            int total = map.Nodes.Count;

            Assert.AreEqual(config.ForcedStartType, map.GetNode(0).Type,
                "El nodo 0 debe tener el tipo forzado de inicio.");
            Assert.AreEqual(config.ForcedEndType, map.GetNode(total - 1).Type,
                "El último nodo debe tener el tipo forzado de fin (Boss).");
        }
    }
}
