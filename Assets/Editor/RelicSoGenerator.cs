using System.IO;
using UnityEditor;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Relics;
using RoguelikeCardBattler.Gameplay.Relics.Effects;

namespace RoguelikeCardBattler.Editor
{
    /// <summary>
    /// Genera los 23 RelicDefinition.asset de Sub-PR 3B desde una sola acción
    /// de menú. Idempotente: salta cualquier .asset ya existente para no
    /// pisar tweaks manuales del Inspector. Borrar el .asset y re-correr para
    /// regenerar uno específico. Datos vienen de Docs/design/RELICS.md tabla
    /// "Resumen del pool sugerido (23 Retazos)".
    /// </summary>
    public static class RelicSoGenerator
    {
        private const string FolderRoot = "Assets/ScriptableObjects";
        private const string FolderRelics = "Assets/ScriptableObjects/Relics";

        [MenuItem("Roguelike/Generate Relic Assets")]
        public static void Generate()
        {
            EnsureFolder(FolderRoot, "ScriptableObjects");
            EnsureFolder(FolderRelics, "Relics");

            int created = 0, skipped = 0;
            foreach (RelicSpec spec in BuildSpecs())
            {
                string path = $"{FolderRelics}/{spec.SlotCode}_{spec.SafeName}.asset";
                if (File.Exists(path))
                {
                    skipped++;
                    continue;
                }
                RelicDefinition def = ScriptableObject.CreateInstance<RelicDefinition>();
                def.DisplayName = spec.DisplayName;
                def.Description = spec.Description;
                def.FlavorText = spec.FlavorText;
                def.Category = spec.Category;
                def.Hooks = spec.Hooks;
                def.Effect = spec.Effect;
                AssetDatabase.CreateAsset(def, path);
                created++;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[RelicSoGenerator] Created {created}, skipped {skipped} (already existed).");
        }

        private static void EnsureFolder(string fullPath, string name)
        {
            if (AssetDatabase.IsValidFolder(fullPath)) return;
            string parent = Path.GetDirectoryName(fullPath).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent, Path.GetFileName(parent));
            }
            AssetDatabase.CreateFolder(parent, name);
        }

        private struct RelicSpec
        {
            public string SlotCode;       // "R-OPEN-1"
            public string SafeName;       // file-system safe short name
            public string DisplayName;
            public string Description;
            public string FlavorText;
            public RelicCategory Category;
            public RelicHook[] Hooks;
            public IRelicEffect Effect;
        }

        private static RelicSpec[] BuildSpecs() => new[]
        {
            new RelicSpec
            {
                SlotCode = "R-OPEN-1", SafeName = "TapaCajaGalletas",
                DisplayName = "Tapa de Caja de Galletas",
                Description = "+4 bloque al iniciar combate.",
                FlavorText = "La levantás cada vez que va a empezar la pelea. Sirve, casi siempre.",
                Category = RelicCategory.Neutral,
                Hooks = new[] { RelicHook.OnCombatStart },
                Effect = new RelicOpenBlockEffect { Amount = 4 },
            },
            new RelicSpec
            {
                SlotCode = "R-OPEN-2", SafeName = "BolsilloRoto",
                DisplayName = "Bolsillo Roto",
                Description = "+1 carta en mano inicial.",
                FlavorText = "Siempre cae una carta de más. No sabés de cuál mundo viene.",
                Category = RelicCategory.Neutral,
                Hooks = new[] { RelicHook.OnCombatStart },
                Effect = new RelicOpenDrawEffect { Amount = 1 },
            },
            new RelicSpec
            {
                SlotCode = "R-OPEN-3", SafeName = "SorboRobado",
                DisplayName = "Sorbo Robado",
                Description = "+1 energía en el primer turno.",
                FlavorText = "La gaseosa de tu hermano. Menos mal que no se dio cuenta.",
                Category = RelicCategory.Neutral,
                Hooks = new[] { RelicHook.OnCombatStart },
                Effect = new RelicOpenEnergyEffect { Amount = 1 },
            },
            new RelicSpec
            {
                SlotCode = "R-TURN-1", SafeName = "ManoDeMas",
                DisplayName = "Mano de Más",
                Description = "+1 carta cada turno.",
                FlavorText = "Te dibujaste una tercera mano en el codo. Funciona.",
                Category = RelicCategory.Neutral,
                Hooks = new[] { RelicHook.OnPlayerTurnStart },
                Effect = new RelicTurnDrawEffect { Amount = 1 },
            },
            new RelicSpec
            {
                SlotCode = "R-TURN-2", SafeName = "AlmohadonReforzado",
                DisplayName = "Almohadón Reforzado",
                Description = "+3 bloque cada turno.",
                FlavorText = "Tres almohadones contra la pared. Inexpugnable.",
                Category = RelicCategory.Neutral,
                Hooks = new[] { RelicHook.OnPlayerTurnStart },
                Effect = new RelicTurnBlockEffect { Amount = 3 },
            },
            new RelicSpec
            {
                SlotCode = "R-TURN-3", SafeName = "RelojCocina",
                DisplayName = "Reloj de Cocina",
                Description = "Cada 3 turnos, +1 energía.",
                FlavorText = "Tic. Tic. Tic. Cada tres tics, vas más rápido.",
                Category = RelicCategory.Neutral,
                Hooks = new[] { RelicHook.OnPlayerTurnStart },
                Effect = new RelicTurnEnergyEveryNEffect { Period = 3, Amount = 1 },
            },
            new RelicSpec
            {
                SlotCode = "R-DMG-1", SafeName = "PuntaLapiz",
                DisplayName = "Punta de Lápiz",
                Description = "+2 daño a tus ataques.",
                FlavorText = "Afilada hace cinco minutos. Todavía duele si toca.",
                Category = RelicCategory.Neutral,
                Hooks = new[] { RelicHook.OnDamageDealt },
                Effect = new RelicDmgFlatBoostEffect { Amount = 2 },
            },
            new RelicSpec
            {
                SlotCode = "R-DMG-2", SafeName = "SorpresaGuardada",
                DisplayName = "Sorpresa Guardada",
                Description = "+5 al primer ataque del combate.",
                FlavorText = "El primer golpe siempre lo planeás. No es trampa, es preparación.",
                Category = RelicCategory.Neutral,
                Hooks = new[] { RelicHook.OnCombatStart, RelicHook.OnDamageDealt },
                Effect = new RelicDmgFirstHitEffect { Amount = 5 },
            },
            new RelicSpec
            {
                SlotCode = "R-DMG-3", SafeName = "CintaEmbalar",
                DisplayName = "Cinta de Embalar",
                Description = "-1 al daño recibido (mínimo 0).",
                FlavorText = "Tres vueltas alrededor del brazo. Improvisado, pero protege.",
                Category = RelicCategory.Neutral,
                Hooks = new[] { RelicHook.OnDamageTaken },
                Effect = new RelicDmgReduceEffect { Amount = 1 },
            },
            new RelicSpec
            {
                SlotCode = "R-ACC-1", SafeName = "CuadernoCuadriculado",
                DisplayName = "Cuaderno Cuadriculado",
                Description = "Cada 3 cartas Skill jugadas → +1 energía el siguiente turno.",
                FlavorText = "Cada tres planes anotados, te dan ganas de hacer uno más.",
                Category = RelicCategory.Neutral,
                Hooks = new[] { RelicHook.OnCardPlayed, RelicHook.OnPlayerTurnStart },
                Effect = new RelicAccSkillStackerEffect { Period = 3, Amount = 1 },
            },
            new RelicSpec
            {
                SlotCode = "R-ACC-2", SafeName = "TresEnRaya",
                DisplayName = "Tres en Raya",
                Description = "Cada 3er ataque del combate hace +4 daño.",
                FlavorText = "Uno, dos, tres — y caen. Siempre fue tu juego favorito.",
                Category = RelicCategory.Neutral,
                Hooks = new[] { RelicHook.OnCombatStart, RelicHook.OnDamageDealt },
                Effect = new RelicAccEveryNthAttackEffect { Period = 3, Amount = 4 },
            },
            new RelicSpec
            {
                SlotCode = "R-END-1", SafeName = "MochilaBotin",
                DisplayName = "Mochila de Botín",
                Description = "+5 oro por victoria.",
                FlavorText = "Vacía cuando empezás. Pesada cuando ganás.",
                Category = RelicCategory.Neutral,
                Hooks = new[] { RelicHook.OnCombatEnd },
                Effect = new RelicEndGoldEffect { Amount = 5 },
            },
            new RelicSpec
            {
                SlotCode = "R-END-2", SafeName = "CuritaEstampa",
                DisplayName = "Curita con Estampa",
                Description = "+4 HP por victoria.",
                FlavorText = "De los dinosaurios. Funciona después de cada pelea.",
                Category = RelicCategory.Neutral,
                Hooks = new[] { RelicHook.OnCombatEnd },
                Effect = new RelicEndHealEffect { Amount = 4 },
            },
            new RelicSpec
            {
                SlotCode = "R-END-3", SafeName = "FrascoConfianza",
                DisplayName = "Frasco de Confianza",
                Description = "+10 oro por Elite ganado.",
                FlavorText = "Lo abrís solo cuando ganás los grandes. Te da más cosas adentro.",
                Category = RelicCategory.Neutral,
                Hooks = new[] { RelicHook.OnCombatEnd },
                Effect = new RelicEndEliteGoldEffect { Amount = 10 },
            },
            new RelicSpec
            {
                SlotCode = "R-SW-4", SafeName = "AlientoEntreMundos",
                DisplayName = "Aliento Entre Mundos",
                Description = "+2 HP al cambiar de mundo.",
                FlavorText = "Cuando saltás de un lado a otro, un poco te recomponés.",
                Category = RelicCategory.Neutral,
                Hooks = new[] { RelicHook.OnWorldSwitch },
                Effect = new RelicSwitchHealEffect { Amount = 2 },
            },
            new RelicSpec
            {
                SlotCode = "R-SW-1", SafeName = "EscudoCosturas",
                DisplayName = "Escudo de Costuras",
                Description = "+5 bloque al cambiar de mundo.",
                FlavorText = "Mitad armadura medieval, mitad placa de circuito. Siempre te tapa.",
                Category = RelicCategory.Switch,
                Hooks = new[] { RelicHook.OnWorldSwitch },
                Effect = new RelicSwitchBlockEffect { Amount = 5 },
            },
            new RelicSpec
            {
                SlotCode = "R-SW-2", SafeName = "EstiloDoble",
                DisplayName = "Estilo Doble",
                Description = "+1 carga de Estilo al cambiar de mundo.",
                FlavorText = "Cada salto entre mundos te queda guardado en la cabeza.",
                Category = RelicCategory.Switch,
                Hooks = new[] { RelicHook.OnWorldSwitch },
                Effect = new RelicSwitchStyleChargeEffect { Amount = 1 },
            },
            new RelicSpec
            {
                SlotCode = "R-SW-3", SafeName = "OndaDimensional",
                DisplayName = "Onda Dimensional",
                Description = "+5 daño raw al enemigo al cambiar de mundo.",
                FlavorText = "El cambio no es silencioso. Algo se rompe del otro lado.",
                Category = RelicCategory.Switch,
                Hooks = new[] { RelicHook.OnWorldSwitch },
                Effect = new RelicSwitchDamageEffect { Amount = 5 },
            },
            new RelicSpec
            {
                SlotCode = "R-ELITE-1", SafeName = "DienteAfilado",
                DisplayName = "Diente Afilado",
                Description = "Al hacer SuperEficaz, heal 2 HP.",
                FlavorText = "Cuando muerde donde duele, te dejás un poco para vos.",
                Category = RelicCategory.Neutral,
                Hooks = new[] { RelicHook.OnDamageDealt },
                Effect = new RelicEliteVampiricEffect { Amount = 2 },
            },
            new RelicSpec
            {
                SlotCode = "R-ELITE-2", SafeName = "ErizoCarton",
                DisplayName = "Erizo de Cartón",
                Description = "Al recibir daño, devolvés 3 raw al atacante.",
                FlavorText = "Lo pegaste con espinas de plástico. Si te tocan, también las sienten.",
                Category = RelicCategory.Neutral,
                Hooks = new[] { RelicHook.OnDamageTaken },
                Effect = new RelicEliteSpinesEffect { Amount = 3 },
            },
            new RelicSpec
            {
                SlotCode = "R-ELITE-3", SafeName = "CajaIntacta",
                DisplayName = "Caja Intacta",
                Description = "Victoria con HP completo → +10 oro.",
                FlavorText = "Si no la abriste, vale más. Llegás entero, te llevás el premio.",
                Category = RelicCategory.Neutral,
                Hooks = new[] { RelicHook.OnCombatEnd },
                Effect = new RelicElitePuristEffect { Amount = 10 },
            },
            new RelicSpec
            {
                SlotCode = "R-ELITE-4", SafeName = "RitmoEncendido",
                DisplayName = "Ritmo Encendido",
                Description = "Con ≥3 cargas de Estilo, tus ataques hacen +4 daño.",
                FlavorText = "Cuando el ritmo es tuyo, todo lo que tirás pega más fuerte.",
                Category = RelicCategory.Neutral,
                Hooks = new[] { RelicHook.OnDamageDealt },
                Effect = new RelicEliteChargeBoostEffect { Threshold = 3, Amount = 4 },
            },
            new RelicSpec
            {
                SlotCode = "R-BOSS-1", SafeName = "HiloCosturaMaldita",
                DisplayName = "Hilo de Costura Maldita",
                Description = "Al ganar un combate, el siguiente arranca con +1 energía y +5 bloque.",
                FlavorText = "Lo arrancaste de su pecho. Sigue moviéndose. Te ayuda en el próximo combate, no sabés bien por qué.",
                Category = RelicCategory.Neutral,
                Hooks = new[] { RelicHook.OnCombatEnd, RelicHook.OnCombatStart },
                Effect = new RelicBossLastStitchEffect { EnergyBonus = 1, BlockBonus = 5 },
            },
        };
    }
}
