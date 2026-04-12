namespace KlondikeSolitaire.Core
{
    public readonly struct ScoringTable
    {
        public int WasteToTableau { get; }
        public int WasteToFoundation { get; }
        public int TableauToFoundation { get; }
        public int FoundationToTableau { get; }
        public int FlipCard { get; }

        public ScoringTable(
            int wasteToTableau = 5,
            int wasteToFoundation = 10,
            int tableauToFoundation = 10,
            int foundationToTableau = -15,
            int flipCard = 5)
        {
            WasteToTableau = wasteToTableau;
            WasteToFoundation = wasteToFoundation;
            TableauToFoundation = tableauToFoundation;
            FoundationToTableau = foundationToTableau;
            FlipCard = flipCard;
        }
    }
}
