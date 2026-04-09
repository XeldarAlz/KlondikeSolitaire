using System;
using System.Collections.Generic;
using KlondikeSolitaire.Core;
using MessagePipe;

namespace KlondikeSolitaire.Systems
{
    public sealed class MoveExecutionSystem : IDisposable
    {
        private readonly BoardModel _boardModel;
        private readonly ScoringSystem _scoringSystem;
        private readonly UndoSystem _undoSystem;
        private readonly IPublisher<CardMovedMessage> _cardMovedPublisher;
        private readonly IPublisher<CardFlippedMessage> _cardFlippedPublisher;
        private readonly IPublisher<BoardStateChangedMessage> _boardStatePublisher;

        public MoveExecutionSystem(
            BoardModel boardModel,
            ScoringSystem scoringSystem,
            UndoSystem undoSystem,
            IPublisher<CardMovedMessage> cardMovedPublisher,
            IPublisher<CardFlippedMessage> cardFlippedPublisher,
            IPublisher<BoardStateChangedMessage> boardStatePublisher)
        {
            _boardModel = boardModel ?? throw new ArgumentNullException(nameof(boardModel));
            _scoringSystem = scoringSystem ?? throw new ArgumentNullException(nameof(scoringSystem));
            _undoSystem = undoSystem ?? throw new ArgumentNullException(nameof(undoSystem));
            _cardMovedPublisher = cardMovedPublisher ?? throw new ArgumentNullException(nameof(cardMovedPublisher));
            _cardFlippedPublisher = cardFlippedPublisher ?? throw new ArgumentNullException(nameof(cardFlippedPublisher));
            _boardStatePublisher = boardStatePublisher ?? throw new ArgumentNullException(nameof(boardStatePublisher));
        }

        public void ExecuteMove(PileId source, PileId dest, int cardCount)
        {
            PileModel sourcePile = _boardModel.GetPile(source);
            PileModel destPile = _boardModel.GetPile(dest);

            List<CardModel> cards = sourcePile.RemoveTop(cardCount);
            destPile.AddCards(cards);

            MoveType moveType = DetermineMoveType(source.Type, dest.Type);
            int scoreDelta = _scoringSystem.CalculateScore(moveType);

            bool wasCardFlipped = false;
            CardModel newTop = sourcePile.TopCard;
            if (source.Type == PileType.Tableau && newTop != null && !newTop.IsFaceUp.Value)
            {
                newTop.IsFaceUp.Value = true;
                scoreDelta += _scoringSystem.CalculateScore(MoveType.FlipCard);
                wasCardFlipped = true;
                _cardFlippedPublisher.Publish(new CardFlippedMessage(source, sourcePile.Count - 1));
            }

            _scoringSystem.ApplyDelta(scoreDelta);

            var command = new MoveCommand(moveType, source, dest, cardCount, scoreDelta, wasCardFlipped, 0);
            _undoSystem.Push(command);

            _cardMovedPublisher.Publish(new CardMovedMessage(source, dest, cardCount));
            _boardStatePublisher.Publish(new BoardStateChangedMessage());
        }

        public void DrawFromStock()
        {
            PileModel stock = _boardModel.Stock;
            PileModel waste = _boardModel.Waste;

            List<CardModel> cards = stock.RemoveTop(1);
            cards[0].IsFaceUp.Value = true;
            waste.AddCards(cards);

            PileId stockId = PileId.Stock();
            PileId wasteId = PileId.Waste();
            var command = new MoveCommand(MoveType.DrawFromStock, stockId, wasteId, 1, 0, false, 0);
            _undoSystem.Push(command);

            _cardMovedPublisher.Publish(new CardMovedMessage(stockId, wasteId, 1));
            _boardStatePublisher.Publish(new BoardStateChangedMessage());
        }

        public void RecycleWaste()
        {
            PileModel waste = _boardModel.Waste;
            PileModel stock = _boardModel.Stock;

            int wasteCardCount = waste.Count;
            List<CardModel> cards = waste.RemoveAll();

            cards.Reverse();

            for (int cardIndex = 0; cardIndex < cards.Count; cardIndex++)
            {
                cards[cardIndex].IsFaceUp.Value = false;
            }

            stock.AddCards(cards);

            PileId wasteId = PileId.Waste();
            PileId stockId = PileId.Stock();
            var command = new MoveCommand(MoveType.RecycleWaste, wasteId, stockId, wasteCardCount, 0, false, wasteCardCount);
            _undoSystem.Push(command);

            _cardMovedPublisher.Publish(new CardMovedMessage(wasteId, stockId, wasteCardCount));
            _boardStatePublisher.Publish(new BoardStateChangedMessage());
        }

        public void Dispose() { }

        private static MoveType DetermineMoveType(PileType sourceType, PileType destType)
        {
            return (sourceType, destType) switch
            {
                (PileType.Waste, PileType.Tableau) => MoveType.WasteToTableau,
                (PileType.Waste, PileType.Foundation) => MoveType.WasteToFoundation,
                (PileType.Tableau, PileType.Foundation) => MoveType.TableauToFoundation,
                (PileType.Foundation, PileType.Tableau) => MoveType.FoundationToTableau,
                (PileType.Tableau, PileType.Tableau) => MoveType.TableauToTableau,
                _ => throw new ArgumentOutOfRangeException($"Unexpected move: {sourceType} -> {destType}")
            };
        }
    }
}
