namespace KlondikeSolitaire.Core
{
    public readonly struct MoveCommand
    {
        public readonly MoveType Type;
        public readonly PileId Source;
        public readonly PileId Destination;
        public readonly int CardCount;
        public readonly int ScoreDelta;
        public readonly bool WasCardFlipped;
        public readonly int WasteCardCount;

        public MoveCommand(
            MoveType type,
            PileId source,
            PileId destination,
            int cardCount,
            int scoreDelta,
            bool wasCardFlipped,
            int wasteCardCount)
        {
            Type = type;
            Source = source;
            Destination = destination;
            CardCount = cardCount;
            ScoreDelta = scoreDelta;
            WasCardFlipped = wasCardFlipped;
            WasteCardCount = wasteCardCount;
        }
    }
}
