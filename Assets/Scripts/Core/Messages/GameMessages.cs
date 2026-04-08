namespace KlondikeSolitaire.Core
{
    public readonly struct CardMovedMessage
    {
        public readonly PileId SourcePileId;
        public readonly PileId DestPileId;
        public readonly int CardCount;

        public CardMovedMessage(PileId sourcePileId, PileId destPileId, int cardCount)
        {
            SourcePileId = sourcePileId;
            DestPileId = destPileId;
            CardCount = cardCount;
        }
    }

    public readonly struct CardFlippedMessage
    {
        public readonly PileId PileId;
        public readonly int CardIndex;

        public CardFlippedMessage(PileId pileId, int cardIndex)
        {
            PileId = pileId;
            CardIndex = cardIndex;
        }
    }

    public readonly struct ScoreChangedMessage
    {
        public readonly int NewScore;
        public readonly int Delta;

        public ScoreChangedMessage(int newScore, int delta)
        {
            NewScore = newScore;
            Delta = delta;
        }
    }

    public readonly struct GamePhaseChangedMessage
    {
        public readonly GamePhase NewPhase;

        public GamePhaseChangedMessage(GamePhase newPhase)
        {
            NewPhase = newPhase;
        }
    }

    public readonly struct AutoCompleteAvailableMessage
    {
        public readonly bool IsAvailable;

        public AutoCompleteAvailableMessage(bool isAvailable)
        {
            IsAvailable = isAvailable;
        }
    }

    public readonly struct UndoAvailabilityChangedMessage
    {
        public readonly bool IsAvailable;

        public UndoAvailabilityChangedMessage(bool isAvailable)
        {
            IsAvailable = isAvailable;
        }
    }

    public readonly struct HintHighlightMessage
    {
        public readonly int SourceCardIndex;
        public readonly PileId SourcePileId;
        public readonly PileId[] DestPileIds;

        public HintHighlightMessage(int sourceCardIndex, PileId sourcePileId, PileId[] destPileIds)
        {
            SourceCardIndex = sourceCardIndex;
            SourcePileId = sourcePileId;
            DestPileIds = destPileIds;
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
        public readonly int FinalScore;

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
