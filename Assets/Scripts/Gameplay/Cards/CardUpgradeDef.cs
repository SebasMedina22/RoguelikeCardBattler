using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoguelikeCardBattler.Gameplay.Cards
{
    /// <summary>
    /// Datos de mejora "bake-in" de una CardDefinition. Se serializa inline en
    /// la carta base; al aplicar la mejora se clona la SO con estos overrides
    /// (ver CardDefinition.CreateUpgradedClone). Categorías cubiertas:
    ///   1. Override de coste (overrideCost + upgradedCost).
    ///   2. Lista de efectos mejorados (upgradedEffects, reemplaza la base si Count > 0).
    ///   3. Nombre/descripción overrides (vacíos = se conserva el original "+").
    /// Extensible: agregar campos aquí + cablearlos en CreateUpgradedClone.
    /// </summary>
    [Serializable]
    public class CardUpgradeDef
    {
        [SerializeField] private bool overrideCost = false;
        [SerializeField] private int upgradedCost = 0;
        [SerializeField] private List<EffectRef> upgradedEffects = new List<EffectRef>();
        [SerializeField] private string upgradedName = string.Empty;
        [SerializeField, TextArea] private string upgradedDescription = string.Empty;

        public bool OverrideCost => overrideCost;
        public int UpgradedCost => upgradedCost;
        public IReadOnlyList<EffectRef> UpgradedEffects => upgradedEffects;
        public string UpgradedName => upgradedName;
        public string UpgradedDescription => upgradedDescription;

        /// <summary>
        /// True si cualquier campo lleva un valor distinto del default. Se usa
        /// para evitar generar clones equivalentes al original (CanUpgrade).
        /// </summary>
        public bool HasUpgrade =>
            overrideCost ||
            (upgradedEffects != null && upgradedEffects.Count > 0) ||
            !string.IsNullOrEmpty(upgradedName) ||
            !string.IsNullOrEmpty(upgradedDescription);

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
        public void SetTestData(bool overrideCost, int upgradedCost, List<EffectRef> upgradedEffects, string upgradedName, string upgradedDescription)
        {
            this.overrideCost = overrideCost;
            this.upgradedCost = upgradedCost;
            this.upgradedEffects = upgradedEffects ?? new List<EffectRef>();
            this.upgradedName = upgradedName ?? string.Empty;
            this.upgradedDescription = upgradedDescription ?? string.Empty;
        }
#endif
    }
}
