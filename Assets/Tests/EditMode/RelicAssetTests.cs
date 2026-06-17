using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Gameplay.Relics;
using RoguelikeCardBattler.Gameplay.Relics.Hooks;
using RoguelikeCardBattler.Run;
using RoguelikeCardBattler.Run.Campfire;
using RoguelikeCardBattler.Run.Shop;

namespace RoguelikeCardBattler.Tests.EditMode
{
    /// <summary>
    /// T5 (auditoría 2026-06, SUB-PR 2): única validación datos↔código. Carga TODOS
    /// los .asset reales de Retazos y verifica que cada hook que un asset DECLARA es
    /// despachable por el RelicHookDispatcher real sin lanzar — es decir, el efecto
    /// realmente maneja el hook que dice escuchar. Si un efecto tira al manejar un hook
    /// declarado, el dispatcher loguea LogError y el Test Runner falla el test.
    /// </summary>
    public class RelicAssetTests
    {
        private const string RelicsFolder = "Assets/ScriptableObjects/Relics";

        [Test]
        public void RelicAssets_AllDeclaredHooksAreDispatchable()
        {
            string[] guids = AssetDatabase.FindAssets("t:RelicDefinition", new[] { RelicsFolder });
            Assert.Greater(guids.Length, 0, $"No se encontraron RelicDefinition en {RelicsFolder}.");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                RelicDefinition def = AssetDatabase.LoadAssetAtPath<RelicDefinition>(path);
                Assert.IsNotNull(def, $"No se pudo cargar {path}.");
                Assert.IsNotNull(def.Effect, $"{def.name}: Effect sin asignar ([SerializeReference] nulo).");
                Assert.IsNotNull(def.Hooks, $"{def.name}: Hooks sin asignar.");
                Assert.Greater(def.Hooks.Length, 0, $"{def.name}: no declara ningún hook.");

                // RunState + dispatcher dedicados por asset. Se despacha vía el dispatcher
                // (no por OnHook directo) porque CurrentRelic tiene internal set y solo
                // el dispatcher lo cablea antes de invocar al efecto.
                RunState rs = new RunState();
                RelicHookDispatcher disp = new RelicHookDispatcher(rs);
                rs.Relics.Add(new RelicInstance(def, 0));

                foreach (RelicHook hook in def.Hooks)
                {
                    RelicHookContext payload = BuildPayload(hook, rs, disp);
                    // El dispatcher captura excepciones del efecto y las loguea como
                    // LogError → NUnit falla el test (LogAssert). El DoesNotThrow cubre
                    // además cualquier fallo en la construcción/recorrido del dispatch.
                    Assert.DoesNotThrow(() => disp.Dispatch(hook, payload),
                        $"{def.name}: el dispatch de {hook} lanzó una excepción no capturada.");
                }
            }
        }

        // Payload representativo por hook. TurnManager null: los Grant* hacen no-op por
        // guard — lo que se valida es que el efecto maneje el hook declarado sin tirar.
        private static RelicHookContext BuildPayload(RelicHook hook, RunState rs, RelicHookDispatcher disp)
        {
            switch (hook)
            {
                case RelicHook.OnCombatStart:
                    return new CombatStartHookData(rs, null, disp, null);
                case RelicHook.OnPlayerTurnStart:
                    return new PlayerTurnStartHookData(rs, null, disp, TurnManager.WorldSide.A);
                case RelicHook.OnDamageDealt:
                    return new DamageDealtHookData(rs, null, disp, 10, Effectiveness.SuperEficaz, ElementType.Rojo, null);
                case RelicHook.OnDamageTaken:
                    return new DamageTakenHookData(rs, null, disp, 10, Effectiveness.SuperEficaz, ElementType.Rojo, null);
                case RelicHook.OnWorldSwitch:
                    return new WorldSwitchHookData(rs, null, disp, TurnManager.WorldSide.A, TurnManager.WorldSide.B);
                case RelicHook.OnCombatEnd:
                    return new CombatEndHookData(rs, null, disp, true, null, isBoss: true, isElite: true);
                case RelicHook.OnCardPlayed:
                    return new CardPlayedHookData(rs, null, disp, null, TurnManager.WorldSide.A, 0);
                case RelicHook.OnCampfireOptionsBuilt:
                    return new CampfireOptionsBuiltHookData(rs, disp, new List<CampfireOption>());
                case RelicHook.OnShopStockBuilt:
                    return new ShopStockBuiltHookData(rs, disp, new List<ShopItem>());
                default:
                    Assert.Fail($"Hook {hook} sin payload de prueba definido.");
                    return null;
            }
        }
    }
}
