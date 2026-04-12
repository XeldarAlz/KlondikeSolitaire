using System;
using System.Collections.Generic;
using KlondikeSolitaire.Core;
using MessagePipe;

namespace KlondikeSolitaire.Systems
{
    public sealed class AutoCompleteSystem : IDisposable
    {
        private readonly BoardModel _board;
        private readonly IPublisher<AutoCompleteAvailableMessage> _autoCompletePublisher;
        private readonly CompositeDisposable _disposables;

        public AutoCompleteSystem(
            BoardModel board,
            ISubscriber<BoardStateChangedMessage> boardStateSubscriber,
            IPublisher<AutoCompleteAvailableMessage> autoCompletePublisher)
        {
            _board = board;
            _autoCompletePublisher = autoCompletePublisher;
            _disposables = new CompositeDisposable();
            boardStateSubscriber.Subscribe(OnBoardStateChanged).AddTo(_disposables);
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
            int[] tableauCounts = new int[BoardModel.TABLEAU_COUNT];
            int[] foundationCounts = new int[BoardModel.FOUNDATION_COUNT];

            for (int tableauIndex = 0; tableauIndex < BoardModel.TABLEAU_COUNT; tableauIndex++)
            {
                tableauCounts[tableauIndex] = _board.Tableau[tableauIndex].Count;
            }

            for (int foundationIndex = 0; foundationIndex < BoardModel.FOUNDATION_COUNT; foundationIndex++)
            {
                foundationCounts[foundationIndex] = _board.Foundations[foundationIndex].Count;
            }

            List<Move> moves = new();

            while (true)
            {
                Move? lowestMove = null;
                int lowestRank = int.MaxValue;

                for (int tableauIndex = 0; tableauIndex < BoardModel.TABLEAU_COUNT; tableauIndex++)
                {
                    int topIndex = tableauCounts[tableauIndex] - 1;
                    if (topIndex < 0)
                    {
                        continue;
                    }

                    CardModel topCard = _board.Tableau[tableauIndex].Cards[topIndex];
                    int foundationIndex = (int)topCard.Suit;
                    int expectedRank = foundationCounts[foundationIndex] + 1;

                    if (topCard.Value == expectedRank && topCard.Value < lowestRank)
                    {
                        lowestRank = topCard.Value;
                        lowestMove = new Move(PileId.Tableau(tableauIndex), PileId.Foundation(foundationIndex), 1);
                    }
                }

                if (lowestMove == null)
                {
                    break;
                }

                Move move = lowestMove.Value;
                tableauCounts[move.Source.Index]--;
                foundationCounts[move.Destination.Index]++;
                moves.Add(move);
            }

            return moves;
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
