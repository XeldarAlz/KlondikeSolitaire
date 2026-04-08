namespace KlondikeSolitaire.Core
{
    public enum CardColor
    {
        Red,
        Black
    }

    public static class SuitExtensions
    {
        public static CardColor Color(this Suit suit) => suit switch
        {
            Suit.Hearts => CardColor.Red,
            Suit.Diamonds => CardColor.Red,
            Suit.Clubs => CardColor.Black,
            Suit.Spades => CardColor.Black,
            _ => CardColor.Black
        };
    }
}
