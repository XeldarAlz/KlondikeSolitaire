namespace KlondikeSolitaire.Core
{
    public sealed class GamePhaseModel
    {
        public ReactiveProperty<GamePhase> Phase { get; }

        public GamePhaseModel()
        {
            Phase = new ReactiveProperty<GamePhase>(GamePhase.Dealing);
        }
    }
}
