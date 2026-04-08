namespace KlondikeSolitaire.Core
{
    public sealed class BoardModel
    {
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

            Foundations = new PileModel[4];
            for (int foundationIndex = 0; foundationIndex < 4; foundationIndex++)
            {
                Foundations[foundationIndex] = new PileModel(PileType.Foundation, foundationIndex);
            }

            Tableau = new PileModel[7];
            for (int tableauIndex = 0; tableauIndex < 7; tableauIndex++)
            {
                Tableau[tableauIndex] = new PileModel(PileType.Tableau, tableauIndex);
            }

            _allPiles = new PileModel[13];
            _allPiles[0] = Stock;
            _allPiles[1] = Waste;
            for (int foundationIndex = 0; foundationIndex < 4; foundationIndex++)
            {
                _allPiles[2 + foundationIndex] = Foundations[foundationIndex];
            }
            for (int tableauIndex = 0; tableauIndex < 7; tableauIndex++)
            {
                _allPiles[6 + tableauIndex] = Tableau[tableauIndex];
            }
        }

        public PileModel GetPile(PileId id)
        {
            return id.Type switch
            {
                PileType.Stock => Stock,
                PileType.Waste => Waste,
                PileType.Foundation => Foundations[id.Index],
                PileType.Tableau => Tableau[id.Index],
                _ => null
            };
        }
    }
}
