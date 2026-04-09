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
        private readonly Stack<MoveCommand> _commandStack;

        public bool CanUndo => _commandStack.Count > 0;

        public UndoSystem(
            BoardModel boardModel,
            ScoringSystem scoringSystem,
            IPublisher<UndoAvailabilityChangedMessage> undoAvailabilityPublisher,
            IPublisher<BoardStateChangedMessage> boardStatePublisher,
            IPublisher<CardFlippedMessage> cardFlippedPublisher)
        {
            _boardModel = boardModel ?? throw new ArgumentNullException(nameof(boardModel));
            _scoringSystem = scoringSystem ?? throw new ArgumentNullException(nameof(scoringSystem));
            _undoAvailabilityPublisher = undoAvailabilityPublisher ?? throw new ArgumentNullException(nameof(undoAvailabilityPublisher));
            _boardStatePublisher = boardStatePublisher ?? throw new ArgumentNullException(nameof(boardStatePublisher));
            _cardFlippedPublisher = cardFlippedPublisher ?? throw new ArgumentNullException(nameof(cardFlippedPublisher));
            _commandStack = new Stack<MoveCommand>();
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

        public void Dispose() { }

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

            List<CardModel> cards = destPile.RemoveTop(command.CardCount);
            sourcePile.AddCards(cards);

            _scoringSystem.ApplyDelta(-command.ScoreDelta);
        }

        private void UndoDrawFromStock(MoveCommand command)
        {
            PileModel waste = _boardModel.Waste;
            PileModel stock = _boardModel.Stock;

            List<CardModel> card = waste.RemoveTop(1);
            card[0].IsFaceUp.Value = false;
            stock.AddCards(card);
        }

        private void UndoRecycleWaste(MoveCommand command)
        {
            PileModel stock = _boardModel.Stock;
            PileModel waste = _boardModel.Waste;

            List<CardModel> cards = stock.RemoveAll();
            cards.Reverse();

            for (int cardIndex = 0; cardIndex < cards.Count; cardIndex++)
            {
                cards[cardIndex].IsFaceUp.Value = true;
            }

            waste.AddCards(cards);
        }
    }
}
