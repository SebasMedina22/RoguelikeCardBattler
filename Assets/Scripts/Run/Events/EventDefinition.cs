using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoguelikeCardBattler.Run.Events
{
    /// <summary>
    /// Variante de un evento multidimensional: enunciado (Body) y decisiones propios
    /// del mundo. 4b-2 agrega este tipo; los eventos NO multidimensionales ignoran
    /// WorldA/WorldB y usan el <see cref="EventDefinition.Choices"/> raíz.
    /// </summary>
    [Serializable]
    public class EventVariant
    {
        [SerializeField, TextArea] private string body;
        [SerializeField] private List<EventChoice> choices = new List<EventChoice>();

        public string Body => body;
        public IReadOnlyList<EventChoice> Choices => choices;

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
        public void SetData(string newBody, List<EventChoice> newChoices)
        {
            body = newBody;
            choices = newChoices ?? new List<EventChoice>();
        }
#endif
    }

    /// <summary>
    /// ScriptableObject de un encuentro de evento. Cubre eventos simples (4b-1) y
    /// multidimensionales (4b-2, <see cref="IsMultidimensional"/>=true).
    /// Los multidimensionales muestran primero una pantalla de elección de mundo (A/B)
    /// y luego la variante correspondiente (<see cref="WorldA"/> / <see cref="WorldB"/>).
    /// Los simples usan <see cref="Choices"/> directamente.
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

        [Header("Multidimensional (4b-2)")]
        [SerializeField] private bool isMultidimensional;
        [SerializeField] private EventVariant worldA = new EventVariant();
        [SerializeField] private EventVariant worldB = new EventVariant();

        public string Id => id;
        public string Title => title;
        public string Body => body;
        public Sprite BackgroundSprite => backgroundSprite;
        public IReadOnlyList<EventChoice> Choices => choices;
        public bool IsMultidimensional => isMultidimensional;
        public EventVariant WorldA => worldA;
        public EventVariant WorldB => worldB;

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
        /// <summary>
        /// Setter para tests EditMode y para el tooling editor (EventConfigSetup),
        /// mismo patrón que <c>CardDefinition.SetDebugData</c>. Los parámetros de
        /// multidimensionalidad son opcionales (defecto = evento simple).
        /// </summary>
        public void SetDebugData(
            string newId, string newTitle, string newBody, List<EventChoice> newChoices,
            Sprite newBackground = null,
            bool multidim = false, EventVariant newWorldA = null, EventVariant newWorldB = null)
        {
            id = newId;
            title = newTitle;
            body = newBody;
            choices = newChoices ?? new List<EventChoice>();
            backgroundSprite = newBackground;
            isMultidimensional = multidim;
            worldA = newWorldA ?? new EventVariant();
            worldB = newWorldB ?? new EventVariant();
        }
#endif
    }
}
