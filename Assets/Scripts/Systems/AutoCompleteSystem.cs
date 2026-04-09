using System;
using System.Collections.Generic;
using KlondikeSolitaire.Core;
using MessagePipe;

namespace KlondikeSolitaire.Systems
{
    public sealed class AutoCompleteSystem : IDisposable
    {
        private readonly BoardModel _board;
        private readonly MoveValidationSystem _moveValidation;
        private readonly IPublisher<AutoCompleteAvailableMessage> _autoCompletePublisher;
        private IDisposable _subscription;

        public AutoCompleteSystem(
            BoardModel board,
            MoveValidationSystem moveValidation,
            ISubscriber<BoardStateChangedMessage> boardStateSubscriber,
            IPublisher<AutoCompleteAvailableMessage> autoCompletePublisher)
        {
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _moveValidation = moveValidation ?? throw new ArgumentNullException(nameof(moveValidation));
            _autoCompletePublisher = autoCompletePublisher ?? throw new ArgumentNullException(nameof(autoCompletePublisher));
            _subscription = (boardStateSubscriber ?? throw new ArgumentNullException(nameof(boardStateSubscriber)))
                .Subscribe(OnBoardStateChanged);
        }

        private void OnBoardStateChanged(BoardStateChangedMessage _)
        {
            bool isAvailable = IsAutoCompletePossible();
            _autoCompletePublisher.Publish(new AutoCompleteAvailableMessage(isAvailable));
        }

        public bool IsAutoCompletePossible()
        {
            if (_board.Stock.Count > 0)
            {
                return false;
            }

            if (_board.Waste.Count > 0)
            {
                return false;
            }

            for (int tableauIndex = 0; tableauIndex < _board.Tableau.Length; tableauIndex++)
            {
                PileModel tableauPile = _board.Tableau[tableauIndex];
                for (int cardIndex = 0; cardIndex < tableauPile.Count; cardIndex++)
                {
                    if (!tableauPile.Cards[cardIndex].IsFaceUp.Value)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public List<Move> GenerateMoveSequence()
        {
            List<Move> moves = new();

            while (true)
            {
                Move? lowestMove = null;
                int lowestRank = int.MaxValue;

                for (int tableauIndex = 0; tableauIndex < _board.Tableau.Length; tableauIndex++)
                {
                    PileModel tableauPile = _board.Tableau[tableauIndex];

                    if (tableauPile.Count == 0)
                    {
                        continue;
                    }

                    CardModel topCard = tableauPile.TopCard;
                    int foundationIndex = (int)topCard.Suit;
                    PileId foundationId = PileId.Foundation(foundationIndex);
                    PileId tableauId = PileId.Tableau(tableauIndex);

                    if (_moveValidation.IsValidMove(_board, tableauId, foundationId, 1))
                    {
                        if (topCard.Value < lowestRank)
                        {
                            lowestRank = topCard.Value;
                            lowestMove = new Move(tableauId, foundationId, 1);
                        }
                    }
                }

                if (lowestMove == null)
                {
                    break;
                }

                Move move = lowestMove.Value;
                moves.Add(move);

                PileModel sourcePile = _board.GetPile(move.Source);
                PileModel destPile = _board.GetPile(move.Destination);
                List<CardModel> removedCards = sourcePile.RemoveTop(move.CardCount);
                destPile.AddCards(removedCards);
            }

            return moves;
        }

        public void Dispose()
        {
            _subscription?.Dispose();
        }
    }
}
