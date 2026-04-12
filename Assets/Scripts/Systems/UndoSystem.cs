using System;
using System.Collections.Generic;
using KlondikeSolitaire.Core;
using MessagePipe;

namespace KlondikeSolitaire.Systems
{
    public sealed class UndoSystem : IDisposable
    {
        private readonly BoardModel _boardModel;
        private readonly ScoringSystem _scoringSystem;
        private readonly IPublisher<UndoAvailabilityChangedMessage> _undoAvailabilityPublisher;
        private readonly IPublisher<BoardStateChangedMessage> _boardStatePublisher;
        private readonly IPublisher<CardFlippedMessage> _cardFlippedPublisher;
        private readonly IPublisher<CardMovedMessage> _cardMovedPublisher;
        private readonly Stack<MoveCommand> _commandStack;
        private readonly CompositeDisposable _disposables;

        public bool CanUndo => _commandStack.Count > 0;

        public UndoSystem(
            BoardModel boardModel,
            ScoringSystem scoringSystem,
            ISubscriber<UndoRequestedMessage> undoRequestedSubscriber,
            IPublisher<UndoAvailabilityChangedMessage> undoAvailabilityPublisher,
            IPublisher<BoardStateChangedMessage> boardStatePublisher,
            IPublisher<CardFlippedMessage> cardFlippedPublisher,
            IPublisher<CardMovedMessage> cardMovedPublisher)
        {
            _boardModel = boardModel;
            _scoringSystem = scoringSystem;
            _undoAvailabilityPublisher = undoAvailabilityPublisher;
            _boardStatePublisher = boardStatePublisher;
            _cardFlippedPublisher = cardFlippedPublisher;
            _cardMovedPublisher = cardMovedPublisher;
            _commandStack = new Stack<MoveCommand>();
            _disposables = new CompositeDisposable();
            undoRequestedSubscriber.Subscribe(OnUndoRequested).AddTo(_disposables);
        }

        private void OnUndoRequested(UndoRequestedMessage _)
        {
            Undo();
        }

        public void Push(MoveCommand command)
        {
            _commandStack.Push(command);
            _undoAvailabilityPublisher.Publish(new UndoAvailabilityChangedMessage(true));
        }

        public void Undo()
        {
            if (_commandStack.Count == 0)
            {
                return;
            }

            MoveCommand command = _commandStack.Pop();

            switch (command.Type)
            {
                case MoveType.DrawFromStock:
                    UndoDrawFromStock(command);
                    break;
                case MoveType.RecycleWaste:
                    UndoRecycleWaste(command);
                    break;
                default:
                    UndoNormalMove(command);
                    break;
            }

            _boardStatePublisher.Publish(new BoardStateChangedMessage());
            _undoAvailabilityPublisher.Publish(new UndoAvailabilityChangedMessage(_commandStack.Count > 0));
        }

        public void Clear()
        {
            _commandStack.Clear();
            _undoAvailabilityPublisher.Publish(new UndoAvailabilityChangedMessage(false));
        }

        private void UndoNormalMove(MoveCommand command)
        {
            PileModel sourcePile = _boardModel.GetPile(command.Source);
            PileModel destPile = _boardModel.GetPile(command.Destination);

            if (command.WasCardFlipped && sourcePile.Count > 0)
            {
                CardModel autoFlippedCard = sourcePile.TopCard;
                autoFlippedCard.IsFaceUp.Value = false;
                _cardFlippedPublisher.Publish(new CardFlippedMessage(sourcePile.Id, sourcePile.Count - 1));
            }

            destPile.TransferTop(command.CardCount, sourcePile);

            _scoringSystem.ApplyDelta(-command.ScoreDelta);

            _cardMovedPublisher.Publish(new CardMovedMessage(command.Destination, command.Source, command.CardCount));
        }

        private void UndoDrawFromStock(MoveCommand command)
        {
            PileModel waste = _boardModel.Waste;
            PileModel stock = _boardModel.Stock;

            CardModel drawnCard = waste.TopCard;
            waste.TransferTop(1, stock);
            drawnCard.IsFaceUp.Value = false;

            _cardMovedPublisher.Publish(new CardMovedMessage(PileId.Waste(), PileId.Stock(), 1));
        }

        private void UndoRecycleWaste(MoveCommand command)
        {
            PileModel stock = _boardModel.Stock;
            PileModel waste = _boardModel.Waste;

            int cardCount = stock.Count;
            stock.TransferAllReversed(waste);

            IReadOnlyList<CardModel> wasteCards = waste.Cards;
            for (int cardIndex = 0; cardIndex < wasteCards.Count; cardIndex++)
            {
                wasteCards[cardIndex].IsFaceUp.Value = true;
            }

            _cardMovedPublisher.Publish(new CardMovedMessage(PileId.Stock(), PileId.Waste(), cardCount, isReversed: true));
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
