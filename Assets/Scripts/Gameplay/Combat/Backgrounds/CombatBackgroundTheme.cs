using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoguelikeCardBattler.Gameplay.Combat.Backgrounds
{
    /// <summary>
    /// Define sprites por capa y por mundo (A/B) para backgrounds de combate.
    /// Es data-driven para iterar arte sin tocar código.
    /// </summary>
    [CreateAssetMenu(menuName = "Roguelike/Combat/Background Theme", fileName = "CombatBackgroundTheme")]
    public class CombatBackgroundTheme : ScriptableObject
    {
        [Serializable]
        public class LayerSprites
        {
            public string layerName = "BG_Far";
            public Sprite worldASprite;
            public Sprite worldBSprite;
            [Range(0f, 0.2f)]
            public float parallaxFactor = 0.02f;
        }

        public List<LayerSprites> layers = new List<LayerSprites>();

        public LayerSprites GetLayer(string layerName)
        {
            if (string.IsNullOrEmpty(layerName))
            {
                return null;
            }

            for (int i = 0; i < layers.Count; i++)
            {
                LayerSprites layer = layers[i];
                if (layer != null && layer.layerName == layerName)
                {
                    return layer;
                }
            }

            return null;
        }
    }
}
