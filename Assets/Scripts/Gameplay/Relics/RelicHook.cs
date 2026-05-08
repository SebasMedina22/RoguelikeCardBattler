namespace RoguelikeCardBattler.Gameplay.Relics
{
    /// <summary>
    /// Lista cerrada de eventos que un Retazo puede escuchar. Ampliarla requiere
    /// nueva sub-PR + tests + actualización del spec en Docs/dev/specs/m3_hooks_spec.md.
    /// Los call sites de OnCampfireOptionsBuilt y OnShopStockBuilt se implementan
    /// en sub-PRs 3C/3D respectivamente; en 3A solo viven en este enum.
    /// </summary>
    public enum RelicHook
    {
        OnCombatStart,
        OnPlayerTurnStart,
        OnDamageDealt,
        OnDamageTaken,
        OnWorldSwitch,
        OnCombatEnd,
        OnCardPlayed,
        OnCampfireOptionsBuilt,
        OnShopStockBuilt
    }
}
