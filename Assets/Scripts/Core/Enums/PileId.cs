namespace KlondikeSolitaire.Core
{
    public readonly struct PileId : System.IEquatable<PileId>
    {
        public PileType Type { get; }
        public int Index { get; }

        public PileId(PileType type, int index)
        {
            Type = type;
            Index = index;
        }

        public static PileId Stock() => new(PileType.Stock, 0);
        public static PileId Waste() => new(PileType.Waste, 0);
        public static PileId Foundation(int index) => new(PileType.Foundation, index);
        public static PileId Tableau(int index) => new(PileType.Tableau, index);

        public bool Equals(PileId other) => Type == other.Type && Index == other.Index;
        public override bool Equals(object obj) => obj is PileId other && Equals(other);
        public override int GetHashCode() => System.HashCode.Combine(Type, Index);

        public override string ToString() => $"{Type}:{Index}";

        public static bool operator ==(PileId left, PileId right) => left.Equals(right);
        public static bool operator !=(PileId left, PileId right) => !left.Equals(right);
    }
}
