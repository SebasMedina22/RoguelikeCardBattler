using UnityEngine;

namespace RoguelikeCardBattler.Gameplay.Relics
{
    /// <summary>
    /// SO base de un Retazo. El efecto se serializa inline vía [SerializeReference]
    /// (spec [CERRADO 1]): cada implementación de IRelicEffect lleva [System.Serializable]
    /// y sus parámetros se guardan como campos de la clase. Type-safe, sin reflexión.
    /// </summary>
    [CreateAssetMenu(menuName = "Roguelike/Relic", fileName = "NewRelic")]
    public class RelicDefinition : ScriptableObject
    {
        public string DisplayName;
        [TextArea] public string Description;
        [TextArea] public string FlavorText;
        public Sprite Icon;
        public RelicCategory Category;
        public RelicHook[] Hooks;
        [SerializeReference] public IRelicEffect Effect;
    }
}
