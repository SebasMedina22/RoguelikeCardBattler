using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoguelikeCardBattler.Run.Events
{
    /// <summary>
    /// Una decisión dentro de un evento: una etiqueta de botón, una lista de
    /// consecuencias que se aplican al elegirla, el texto de resultado que se
    /// muestra tras elegir, y un requisito opcional de oro (botón gris si el
    /// jugador no alcanza). Datos puros serializables.
    /// </summary>
    [Serializable]
    public class EventChoice
    {
        [SerializeField] private string label;
        [SerializeField, TextArea] private string resultText;
        [Tooltip("0 = sin condición. Si el oro del jugador es menor, la decisión queda deshabilitada.")]
        [SerializeField, Min(0)] private int minGoldRequired = 0;
        [SerializeField] private List<EventConsequence> consequences = new List<EventConsequence>();

        public string Label => label;
        public string ResultText => resultText;
        public int MinGoldRequired => Mathf.Max(0, minGoldRequired);
        public IReadOnlyList<EventConsequence> Consequences => consequences;

        public EventChoice() { }

        public EventChoice(
            string label,
            string resultText,
            List<EventConsequence> consequences,
            int minGoldRequired = 0)
        {
            this.label = label;
            this.resultText = resultText;
            this.consequences = consequences ?? new List<EventConsequence>();
            this.minGoldRequired = minGoldRequired;
        }
    }
}
