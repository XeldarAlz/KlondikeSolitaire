namespace KlondikeSolitaire.Core
{
    public sealed class BoardModel
    {
        public const int FOUNDATION_COUNT = 4;
        public const int TABLEAU_COUNT = 7;
        public const int DECK_SIZE = 52;
        public const int RANK_COUNT = 13;
        public const int TABLEAU_DEAL_COUNT = 28;
        private const int PILE_COUNT = 2 + FOUNDATION_COUNT + TABLEAU_COUNT;

        private readonly PileModel[] _allPiles;

        public PileModel Stock { get; }
        public PileModel Waste { get; }
        public PileModel[] Foundations { get; }
        public PileModel[] Tableau { get; }

        public PileModel[] AllPiles => _allPiles;

        public BoardModel()
        {
            Stock = new PileModel(PileType.Stock, 0);
            Waste = new PileModel(PileType.Waste, 0);

            Foundations = new PileModel[FOUNDATION_COUNT];
            for (int foundationIndex = 0; foundationIndex < FOUNDATION_COUNT; foundationIndex++)
            {
                Foundations[foundationIndex] = new PileModel(PileType.Foundation, foundationIndex);
            }

            Tableau = new PileModel[TABLEAU_COUNT];
            for (int tableauIndex = 0; tableauIndex < TABLEAU_COUNT; tableauIndex++)
            {
                Tableau[tableauIndex] = new PileModel(PileType.Tableau, tableauIndex);
            }

            if (FOUNDATION_COUNT != System.Enum.GetValues(typeof(Suit)).Length)
            {
                throw new System.InvalidOperationException("Foundation count must match Suit count");
            }

            _allPiles = new PileModel[PILE_COUNT];
            _allPiles[0] = Stock;
            _allPiles[1] = Waste;
            for (int foundationIndex = 0; foundationIndex < FOUNDATION_COUNT; foundationIndex++)
            {
                _allPiles[2 + foundationIndex] = Foundations[foundationIndex];
            }
            for (int tableauIndex = 0; tableauIndex < TABLEAU_COUNT; tableauIndex++)
            {
                _allPiles[2 + FOUNDATION_COUNT + tableauIndex] = Tableau[tableauIndex];
            }
        }

        public static PileId FoundationIdForSuit(Suit suit) => PileId.Foundation((int)suit);

        public PileModel GetPile(PileId id)
        {
            return id.Type switch
            {
                PileType.Stock => Stock,
                PileType.Waste => Waste,
                PileType.Foundation => Foundations[id.Index],
                PileType.Tableau => Tableau[id.Index],
                _ => throw new System.ArgumentOutOfRangeException(nameof(id), id.Type, "Unknown PileType")
            };
        }
    }
}
