using System;

namespace RoguelikeCardBattler.Run.Campfire
{
    /// <summary>
    /// Una opción del menú de la Hoguera. Data class pura: el controller
    /// construye la lista, el dispatcher de Retazos puede agregar opciones
    /// extra vía OnCampfireOptionsBuilt, y la UI dibuja un botón por opción.
    /// </summary>
    public class CampfireOption
    {
        public string Title { get; }
        public string Description { get; }
        public bool IsAvailable { get; }
        public Action OnSelect { get; }

        public CampfireOption(string title, string description, bool isAvailable, Action onSelect)
        {
            Title = title;
            Description = description;
            IsAvailable = isAvailable;
            OnSelect = onSelect;
        }
    }
}
