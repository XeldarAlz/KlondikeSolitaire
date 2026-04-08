namespace KlondikeSolitaire.Core
{
    public sealed class ScoreModel
    {
        public ReactiveProperty<int> Score { get; }

        public ScoreModel()
        {
            Score = new ReactiveProperty<int>(0);
        }
    }
}
