using System.Collections.Generic;
using UnityEngine;

namespace RoguelikeCardBattler.Run.Events
{
    /// <summary>
    /// ScriptableObject de un encuentro de evento. 4b-1 cubre los eventos NO
    /// multidimensionales: título + texto narrativo + lista de decisiones
    /// (<see cref="EventChoice"/>) con consecuencias data-driven.
    ///
    /// Los campos de multidimensionalidad (IsMultidimensional + variantes
    /// WorldA/WorldB) los AGREGA 4b-2; en 4b-1 todo evento usa <see cref="Choices"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Roguelike/Event Definition", fileName = "EventDefinition")]
    public class EventDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string title;
        [SerializeField, TextArea] private string body;
        [Header("Fondo del panel (opcional; cae al fondo del pool y luego a color)")]
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private List<EventChoice> choices = new List<EventChoice>();

        public string Id => id;
        public string Title => title;
        public string Body => body;
        public Sprite BackgroundSprite => backgroundSprite;
        public IReadOnlyList<EventChoice> Choices => choices;

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
        /// <summary>
        /// Setter para tests EditMode y para el tooling editor (EventConfigSetup),
        /// mismo patrón que <c>CardDefinition.SetDebugData</c>. Permite armar un
        /// EventDefinition sin asset en disco.
        /// </summary>
        public void SetDebugData(string newId, string newTitle, string newBody, List<EventChoice> newChoices, Sprite newBackground = null)
        {
            id = newId;
            title = newTitle;
            body = newBody;
            choices = newChoices ?? new List<EventChoice>();
            backgroundSprite = newBackground;
        }
#endif
    }
}
