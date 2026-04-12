namespace KlondikeSolitaire.Core
{
    public readonly struct MoveCommand : System.IEquatable<MoveCommand>
    {
        public MoveType Type { get; }
        public PileId Source { get; }
        public PileId Destination { get; }
        public int CardCount { get; }
        public int ScoreDelta { get; }
        public bool WasCardFlipped { get; }

        public MoveCommand(
            MoveType type,
            PileId source,
            PileId destination,
            int cardCount,
            int scoreDelta,
            bool wasCardFlipped)
        {
            Type = type;
            Source = source;
            Destination = destination;
            CardCount = cardCount;
            ScoreDelta = scoreDelta;
            WasCardFlipped = wasCardFlipped;
        }

        public bool Equals(MoveCommand other) =>
            Type == other.Type
            && Source == other.Source
            && Destination == other.Destination
            && CardCount == other.CardCount
            && ScoreDelta == other.ScoreDelta
            && WasCardFlipped == other.WasCardFlipped;

        public override bool Equals(object obj) => obj is MoveCommand other && Equals(other);

        public override int GetHashCode() =>
            System.HashCode.Combine(Type, Source, Destination, CardCount, ScoreDelta, WasCardFlipped);

        public static bool operator ==(MoveCommand left, MoveCommand right) => left.Equals(right);
        public static bool operator !=(MoveCommand left, MoveCommand right) => !left.Equals(right);
    }
}
