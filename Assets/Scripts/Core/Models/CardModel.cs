namespace KlondikeSolitaire.Core
{
    public sealed class CardModel
    {
        public Suit Suit { get; }
        public Rank Rank { get; }
        public ReactiveProperty<bool> IsFaceUp { get; }

        public CardColor Color => Suit.Color();
        public int Value => (int)Rank;

        public CardModel(Suit suit, Rank rank)
        {
            Suit = suit;
            Rank = rank;
            IsFaceUp = new ReactiveProperty<bool>(false);
        }
    }
}
