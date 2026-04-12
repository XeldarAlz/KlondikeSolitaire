namespace KlondikeSolitaire.Core
{
    public sealed class DealModel
    {
        public ReactiveProperty<int> Seed { get; } = new(0);
    }
}
