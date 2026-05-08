using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Relics.Hooks;
using RoguelikeCardBattler.Run;

namespace RoguelikeCardBattler.Gameplay.Relics
{
    /// <summary>
    /// Bus único por el que TurnManager y los NodeControllers (3C/3D) emiten
    /// eventos de Retazos. Filtra por hook, ordena por AcquisitionOrder
    /// ascendente y ejecuta cada efecto pasando el payload tipado. Vive en
    /// RunSession (cruza escenas con la run). Sin Retazos = cero overhead.
    /// </summary>
    public class RelicHookDispatcher
    {
        private readonly RunState _runState;

        // Default OFF para no spammear consola. Cuando se prende, los logs
        // van por Debug.Log directo (no detrás de #if UNITY_EDITOR) para que
        // funcionen también en builds de desarrollo ([CERRADO 5]).
        public bool LogDispatches { get; set; } = false;

        public RelicHookDispatcher(RunState runState)
        {
            _runState = runState ?? throw new ArgumentNullException(nameof(runState));
        }

        public void Dispatch<TData>(RelicHook hook, TData data) where TData : RelicHookContext
        {
            if (data == null) return;

            IReadOnlyList<RelicInstance> subscribers = GetSubscribers(hook);
            if (subscribers.Count == 0) return;

            if (LogDispatches)
            {
                Debug.Log($"[Relics] {hook} → {subscribers.Count} subscribers ({JoinNames(subscribers)})");
            }

            for (int i = 0; i < subscribers.Count; i++)
            {
                RelicInstance instance = subscribers[i];
                IRelicEffect effect = instance.Effect;
                if (effect == null) continue;

                data.CurrentRelic = instance;
                try
                {
                    effect.OnHook(hook, data);
                }
                catch (Exception e)
                {
                    // Un Retazo defectuoso no debe abortar el resto de la cadena
                    // ni el flujo del combate. Logueamos y seguimos.
                    Debug.LogError($"[Relics] {hook} threw in '{NameOf(instance)}': {e}");
                }
            }

            data.CurrentRelic = null;
        }

        public IReadOnlyList<RelicInstance> GetSubscribers(RelicHook hook)
        {
            List<RelicInstance> result = new List<RelicInstance>();
            IReadOnlyList<RelicInstance> all = _runState.Relics;
            for (int i = 0; i < all.Count; i++)
            {
                RelicInstance r = all[i];
                if (r == null || r.Definition == null || r.Definition.Hooks == null) continue;
                if (Array.IndexOf(r.Definition.Hooks, hook) < 0) continue;
                result.Add(r);
            }
            result.Sort(CompareByAcquisitionOrder);
            return result;
        }

        private static int CompareByAcquisitionOrder(RelicInstance a, RelicInstance b)
        {
            return a.AcquisitionOrder.CompareTo(b.AcquisitionOrder);
        }

        private static string JoinNames(IReadOnlyList<RelicInstance> list)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(NameOf(list[i]));
            }
            return sb.ToString();
        }

        private static string NameOf(RelicInstance r)
        {
            if (r == null || r.Definition == null) return "<null>";
            return string.IsNullOrEmpty(r.Definition.DisplayName)
                ? r.Definition.name
                : r.Definition.DisplayName;
        }
    }
}
