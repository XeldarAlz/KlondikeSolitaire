namespace KlondikeSolitaire.Core
{
    public readonly struct CardMovedMessage
    {
        public PileId SourcePileId { get; }
        public PileId DestPileId { get; }
        public int CardCount { get; }

        public CardMovedMessage(PileId sourcePileId, PileId destPileId, int cardCount)
        {
            SourcePileId = sourcePileId;
            DestPileId = destPileId;
            CardCount = cardCount;
        }
    }

    public readonly struct CardFlippedMessage
    {
        public PileId PileId { get; }
        public int CardIndex { get; }

        public CardFlippedMessage(PileId pileId, int cardIndex)
        {
            PileId = pileId;
            CardIndex = cardIndex;
        }
    }

    public readonly struct ScoreChangedMessage
    {
        public int NewScore { get; }
        public int Delta { get; }

        public ScoreChangedMessage(int newScore, int delta)
        {
            NewScore = newScore;
            Delta = delta;
        }
    }

    public readonly struct GamePhaseChangedMessage
    {
        public GamePhase NewPhase { get; }

        public GamePhaseChangedMessage(GamePhase newPhase)
        {
            NewPhase = newPhase;
        }
    }

    public readonly struct AutoCompleteAvailableMessage
    {
        public bool IsAvailable { get; }

        public AutoCompleteAvailableMessage(bool isAvailable)
        {
            IsAvailable = isAvailable;
        }
    }

    public readonly struct UndoAvailabilityChangedMessage
    {
        public bool IsAvailable { get; }

        public UndoAvailabilityChangedMessage(bool isAvailable)
        {
            IsAvailable = isAvailable;
        }
    }

    public readonly struct HintHighlightMessage
    {
        public int SourceCardIndex { get; }
        public PileId SourcePileId { get; }
        public PileId DestPileId { get; }

        public HintHighlightMessage(int sourceCardIndex, PileId sourcePileId, PileId destPileId)
        {
            SourceCardIndex = sourceCardIndex;
            SourcePileId = sourcePileId;
            DestPileId = destPileId;
        }
    }

    public readonly struct HintClearedMessage
    {
    }

    public readonly struct BoardStateChangedMessage
    {
    }

    public readonly struct NoMovesDetectedMessage
    {
    }

    public readonly struct WinDetectedMessage
    {
        public int FinalScore { get; }

        public WinDetectedMessage(int finalScore)
        {
            FinalScore = finalScore;
        }
    }

    public readonly struct NewGameRequestedMessage
    {
    }

    public readonly struct DealCompletedMessage
    {
    }
}
