namespace KlondikeSolitaire.Core
{
    public readonly struct Move
    {
        public readonly PileId Source;
        public readonly PileId Destination;
        public readonly int CardCount;

        public Move(PileId source, PileId destination, int cardCount)
        {
            Source = source;
            Destination = destination;
            CardCount = cardCount;
        }
    }
}
