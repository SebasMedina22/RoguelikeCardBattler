using RoguelikeCardBattler.Gameplay.Relics.Hooks;

namespace RoguelikeCardBattler.Gameplay.Relics
{
    /// <summary>
    /// Contrato ejecutable de un Retazo. Cada implementación lleva [System.Serializable]
    /// para que el SO RelicDefinition pueda serializarla vía [SerializeReference]
    /// (ver spec, [CERRADO 1]). El método se invoca una vez por evento, recibiendo
    /// el hook que se está disparando para que un mismo efecto pueda escuchar varios.
    /// </summary>
    public interface IRelicEffect
    {
        void OnHook(RelicHook hook, RelicHookContext ctx);
    }
}
