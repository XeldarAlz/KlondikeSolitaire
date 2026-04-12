namespace KlondikeSolitaire.Core
{
    public readonly struct CardMovedMessage
    {
        public PileId SourcePileId { get; }
        public PileId DestPileId { get; }
        public int CardCount { get; }
        public bool IsReversed { get; }

        public CardMovedMessage(PileId sourcePileId, PileId destPileId, int cardCount, bool isReversed = false)
        {
            SourcePileId = sourcePileId;
            DestPileId = destPileId;
            CardCount = cardCount;
            IsReversed = isReversed;
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

    public readonly struct UndoRequestedMessage
    {
    }

    public readonly struct HintRequestedMessage
    {
    }

    public readonly struct AutoCompleteRequestedMessage
    {
    }

    public readonly struct DealCompletedMessage
    {
        public int Seed { get; }

        public DealCompletedMessage(int seed)
        {
            Seed = seed;
        }
    }

    public readonly struct DealAnimationCompletedMessage
    {
    }
}
