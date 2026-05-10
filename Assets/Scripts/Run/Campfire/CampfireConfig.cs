using UnityEngine;

namespace RoguelikeCardBattler.Run.Campfire
{
    /// <summary>
    /// Configuración del nodo Hoguera. Mantenemos los sprites del parallax aquí
    /// (no en el controller) para que el arte se asigne sin tocar código.
    /// Si los sprites son null el controller usa colores de fallback —
    /// el código debe funcionar antes de tener arte real.
    /// </summary>
    [CreateAssetMenu(menuName = "Roguelike/Campfire Config", fileName = "CampfireConfig")]
    public class CampfireConfig : ScriptableObject
    {
        [SerializeField, Tooltip("Porcentaje del HP máximo que recupera la opción Descansar.")]
        private int campfireHealPercent = 30;

        [SerializeField] private Sprite skySprite;
        [SerializeField] private Sprite midSprite;
        [SerializeField] private Sprite fireSprite;

        public int CampfireHealPercent => Mathf.Clamp(campfireHealPercent, 0, 100);
        public Sprite SkySprite => skySprite;
        public Sprite MidSprite => midSprite;
        public Sprite FireSprite => fireSprite;
    }
}
