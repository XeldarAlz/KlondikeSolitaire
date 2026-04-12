namespace KlondikeSolitaire.Core
{
    public readonly struct Move : System.IEquatable<Move>
    {
        public PileId Source { get; }
        public PileId Destination { get; }
        public int CardCount { get; }

        public Move(PileId source, PileId destination, int cardCount)
        {
            Source = source;
            Destination = destination;
            CardCount = cardCount;
        }

        public bool Equals(Move other) =>
            Source == other.Source && Destination == other.Destination && CardCount == other.CardCount;

        public override bool Equals(object obj) => obj is Move other && Equals(other);
        public override int GetHashCode() => System.HashCode.Combine(Source, Destination, CardCount);

        public static bool operator ==(Move left, Move right) => left.Equals(right);
        public static bool operator !=(Move left, Move right) => !left.Equals(right);
    }
}
