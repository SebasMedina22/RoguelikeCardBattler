namespace RoguelikeCardBattler.Gameplay.Combat
{
    /// <summary>
    /// Contrato mínimo para acciones encoladas en la ActionQueue.
    /// Deben ser puras (sin asumir orden distinto) y tolerar nulls de contexto si aplica.
    /// </summary>
    public interface IGameAction
    {
        void Execute(ActionContext context);
    }
}

