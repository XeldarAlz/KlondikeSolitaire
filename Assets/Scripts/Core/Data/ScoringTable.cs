namespace KlondikeSolitaire.Core
{
    public readonly struct ScoringTable
    {
        public readonly int WasteToTableau;
        public readonly int WasteToFoundation;
        public readonly int TableauToFoundation;
        public readonly int FoundationToTableau;
        public readonly int FlipCard;

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
