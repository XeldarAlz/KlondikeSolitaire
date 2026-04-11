using System.Collections.Generic;
using KlondikeSolitaire.Core;

namespace KlondikeSolitaire.Systems
{
    public static class MoveEnumerator
    {
        private const int FOUNDATION_COUNT = 4;
        private const int TABLEAU_COUNT = 7;

        public static void EnumerateAllValidMoves(BoardModel board, MoveValidationSystem validation, List<Move> results)
        {
            results.Clear();

            PileId wasteId = PileId.Waste();

            PileModel wastePile = board.Waste;
            if (wastePile.Count > 0)
            {
                PileId canonicalFoundation = PileId.Foundation((int)wastePile.TopCard.Suit);
                if (validation.IsValidMove(board, wasteId, canonicalFoundation, 1))
                {
                    results.Add(new Move(wasteId, canonicalFoundation, 1));
                }
            }

            for (int tableauIndex = 0; tableauIndex < TABLEAU_COUNT; tableauIndex++)
            {
                PileId tableauId = PileId.Tableau(tableauIndex);
                if (validation.IsValidMove(board, wasteId, tableauId, 1))
                {
                    results.Add(new Move(wasteId, tableauId, 1));
                }
            }

            for (int sourceIndex = 0; sourceIndex < TABLEAU_COUNT; sourceIndex++)
            {
                PileModel sourcePile = board.Tableau[sourceIndex];
                if (sourcePile.TopCard == null)
                {
                    continue;
                }

                PileId sourceId = PileId.Tableau(sourceIndex);
                PileId canonicalFoundation = PileId.Foundation((int)sourcePile.TopCard.Suit);
                if (validation.IsValidMove(board, sourceId, canonicalFoundation, 1))
                {
                    results.Add(new Move(sourceId, canonicalFoundation, 1));
                }
            }

            for (int sourceIndex = 0; sourceIndex < TABLEAU_COUNT; sourceIndex++)
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

                    for (int destIndex = 0; destIndex < TABLEAU_COUNT; destIndex++)
                    {
                        if (destIndex == sourceIndex)
                        {
                            continue;
                        }

                        PileId destId = PileId.Tableau(destIndex);
                        if (validation.IsValidMove(board, sourceId, destId, cardCount))
                        {
                            results.Add(new Move(sourceId, destId, cardCount));
                        }
                    }
                }
            }

            if (board.Stock.Count > 0)
            {
                results.Add(new Move(PileId.Stock(), PileId.Waste(), 1));
            }
        }

        public static bool HasAnyValidMove(BoardModel board, MoveValidationSystem validation)
        {
            if (board.Stock.Count > 0)
            {
                return true;
            }

            PileId wasteId = PileId.Waste();

            PileModel wastePile = board.Waste;
            if (wastePile.Count > 0)
            {
                PileId canonicalFoundation = PileId.Foundation((int)wastePile.TopCard.Suit);
                if (validation.IsValidMove(board, wasteId, canonicalFoundation, 1))
                {
                    return true;
                }
            }

            for (int tableauIndex = 0; tableauIndex < TABLEAU_COUNT; tableauIndex++)
            {
                PileId tableauId = PileId.Tableau(tableauIndex);
                if (validation.IsValidMove(board, wasteId, tableauId, 1))
                {
                    return true;
                }
            }

            for (int sourceIndex = 0; sourceIndex < TABLEAU_COUNT; sourceIndex++)
            {
                PileModel sourcePile = board.Tableau[sourceIndex];
                if (sourcePile.TopCard == null)
                {
                    continue;
                }

                PileId sourceId = PileId.Tableau(sourceIndex);
                PileId canonicalFoundation = PileId.Foundation((int)sourcePile.TopCard.Suit);
                if (validation.IsValidMove(board, sourceId, canonicalFoundation, 1))
                {
                    return true;
                }
            }

            for (int sourceIndex = 0; sourceIndex < TABLEAU_COUNT; sourceIndex++)
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

                    for (int destIndex = 0; destIndex < TABLEAU_COUNT; destIndex++)
                    {
                        if (destIndex == sourceIndex)
                        {
                            continue;
                        }

                        PileId destId = PileId.Tableau(destIndex);
                        if (validation.IsValidMove(board, sourceId, destId, cardCount))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static int FindFirstFaceUpIndex(PileModel pile)
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
