using System;
using System.Collections.Generic;
using KlondikeSolitaire.Core;
using MessagePipe;

namespace KlondikeSolitaire.Systems
{
    public sealed class MoveExecutionSystem
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
            _boardModel = boardModel;
            _scoringSystem = scoringSystem;
            _undoSystem = undoSystem;
            _cardMovedPublisher = cardMovedPublisher;
            _cardFlippedPublisher = cardFlippedPublisher;
            _boardStatePublisher = boardStatePublisher;
        }

        public void ExecuteMove(PileId source, PileId dest, int cardCount)
        {
            if (source == dest)
            {
                return;
            }

            PileModel sourcePile = _boardModel.GetPile(source);

            if (sourcePile.Count < cardCount)
            {
                throw new InvalidOperationException(
                    $"Cannot move {cardCount} cards from {source}: pile only has {sourcePile.Count}");
            }

            PileModel destPile = _boardModel.GetPile(dest);

            sourcePile.TransferTop(cardCount, destPile);

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

            var command = new MoveCommand(moveType, source, dest, cardCount, scoreDelta, wasCardFlipped);
            _undoSystem.Push(command);

            _cardMovedPublisher.Publish(new CardMovedMessage(source, dest, cardCount));
            _boardStatePublisher.Publish(new BoardStateChangedMessage());
        }

        public void DrawFromStock()
        {
            PileModel stock = _boardModel.Stock;

            if (stock.Count == 0)
            {
                return;
            }

            PileModel waste = _boardModel.Waste;

            CardModel drawnCard = stock.TopCard;
            stock.TransferTop(1, waste);
            drawnCard.IsFaceUp.Value = true;

            PileId stockId = PileId.Stock();
            PileId wasteId = PileId.Waste();
            var command = new MoveCommand(MoveType.DrawFromStock, stockId, wasteId, 1, 0, false);
            _undoSystem.Push(command);

            _cardMovedPublisher.Publish(new CardMovedMessage(stockId, wasteId, 1));
            _boardStatePublisher.Publish(new BoardStateChangedMessage());
        }

        public void RecycleWaste()
        {
            PileModel waste = _boardModel.Waste;

            if (waste.Count == 0)
            {
                return;
            }

            PileModel stock = _boardModel.Stock;

            int wasteCardCount = waste.Count;
            waste.TransferAllReversed(stock);

            IReadOnlyList<CardModel> stockCards = stock.Cards;
            for (int cardIndex = 0; cardIndex < stockCards.Count; cardIndex++)
            {
                stockCards[cardIndex].IsFaceUp.Value = false;
            }

            PileId wasteId = PileId.Waste();
            PileId stockId = PileId.Stock();
            var command = new MoveCommand(MoveType.RecycleWaste, wasteId, stockId, wasteCardCount, 0, false);
            _undoSystem.Push(command);

            _cardMovedPublisher.Publish(new CardMovedMessage(wasteId, stockId, wasteCardCount, isReversed: true));
            _boardStatePublisher.Publish(new BoardStateChangedMessage());
        }

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
