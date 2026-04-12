using System.Collections.Generic;
using KlondikeSolitaire.Core;

namespace KlondikeSolitaire.Systems
{
    public sealed class MoveEnumerator
    {
        private readonly MoveValidationSystem _validation;

        public MoveEnumerator(MoveValidationSystem validation)
        {
            _validation = validation;
        }

        public void EnumerateAllValidMoves(BoardModel board, List<Move> results)
        {
            results.Clear();
            FindValidMoves(board, results);
        }

        public bool HasAnyValidMove(BoardModel board)
        {
            return FindValidMoves(board, null);
        }

        private bool FindValidMoves(BoardModel board, List<Move> results)
        {
            bool earlyExit = results == null;

            if (earlyExit && board.Stock.Count > 0)
            {
                return true;
            }

            PileId wasteId = PileId.Waste();
            PileModel wastePile = board.Waste;

            if (wastePile.Count > 0)
            {
                PileId canonicalFoundation = PileId.Foundation((int)wastePile.TopCard.Suit);
                if (_validation.IsValidMove(board, wasteId, canonicalFoundation, 1))
                {
                    if (earlyExit)
                    {
                        return true;
                    }
                    results.Add(new Move(wasteId, canonicalFoundation, 1));
                }
            }

            for (int tableauIndex = 0; tableauIndex < BoardModel.TABLEAU_COUNT; tableauIndex++)
            {
                PileId tableauId = PileId.Tableau(tableauIndex);
                if (_validation.IsValidMove(board, wasteId, tableauId, 1))
                {
                    if (earlyExit)
                    {
                        return true;
                    }
                    results.Add(new Move(wasteId, tableauId, 1));
                }
            }

            for (int sourceIndex = 0; sourceIndex < BoardModel.TABLEAU_COUNT; sourceIndex++)
            {
                PileModel sourcePile = board.Tableau[sourceIndex];
                if (sourcePile.TopCard == null)
                {
                    continue;
                }

                PileId sourceId = PileId.Tableau(sourceIndex);
                PileId canonicalFoundation = PileId.Foundation((int)sourcePile.TopCard.Suit);
                if (_validation.IsValidMove(board, sourceId, canonicalFoundation, 1))
                {
                    if (earlyExit)
                    {
                        return true;
                    }
                    results.Add(new Move(sourceId, canonicalFoundation, 1));
                }
            }

            for (int sourceIndex = 0; sourceIndex < BoardModel.TABLEAU_COUNT; sourceIndex++)
            {
                PileModel sourcePile = board.Tableau[sourceIndex];
                if (sourcePile.Count == 0)
                {
                    continue;
                }

                PileId sourceId = PileId.Tableau(sourceIndex);

                int firstFaceUpIndex = FindFirstFaceUpIndex(sourcePile);
                if (firstFaceUpIndex < 0)
                {
                    continue;
                }

                for (int startIndex = firstFaceUpIndex; startIndex < sourcePile.Count; startIndex++)
                {
                    int cardCount = sourcePile.Count - startIndex;

                    for (int destIndex = 0; destIndex < BoardModel.TABLEAU_COUNT; destIndex++)
                    {
                        if (destIndex == sourceIndex)
                        {
                            continue;
                        }

                        PileId destId = PileId.Tableau(destIndex);
                        if (_validation.IsValidMove(board, sourceId, destId, cardCount))
                        {
                            if (earlyExit)
                            {
                                return true;
                            }
                            results.Add(new Move(sourceId, destId, cardCount));
                        }
                    }
                }
            }

            if (board.Stock.Count > 0)
            {
                if (earlyExit)
                {
                    return true;
                }
                results.Add(new Move(PileId.Stock(), PileId.Waste(), 1));
            }

            return !earlyExit && results.Count > 0;
        }

        private int FindFirstFaceUpIndex(PileModel pile)
        {
            for (int cardIndex = 0; cardIndex < pile.Count; cardIndex++)
            {
                if (pile.Cards[cardIndex].IsFaceUp.Value)
                {
                    return cardIndex;
                }
            }

            return -1;
        }
    }
}
