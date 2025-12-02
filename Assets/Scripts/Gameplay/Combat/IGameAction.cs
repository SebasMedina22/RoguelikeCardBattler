namespace RoguelikeCardBattler.Gameplay.Combat
{
    public interface IGameAction
    {
        void Execute(ActionContext context);
    }
}

