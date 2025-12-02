using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoguelikeCardBattler.Gameplay.Cards
{
    [Serializable]
    public class EffectParam
    {
        public string key;
        public string value;
    }

    /// <summary>
    /// Lightweight reference to a gameplay effect that cards and enemies can reuse.
    /// </summary>
    [Serializable]
    public class EffectRef
    {
        public EffectType effectType;
        public int value;
        public EffectTarget target;
        public StatusType statusType = StatusType.None;

        [SerializeField]
        private List<EffectParam> extraParams = new List<EffectParam>();

        public IReadOnlyList<EffectParam> ExtraParams => extraParams;

        /// <summary>
        /// Quick helper for runtime lookups without exposing mutable lists.
        /// </summary>
        public Dictionary<string, string> GetExtraParamsDictionary()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (EffectParam param in extraParams)
            {
                if (!string.IsNullOrEmpty(param?.key))
                {
                    result[param.key] = param.value;
                }
            }

            return result;
        }
    }
}

