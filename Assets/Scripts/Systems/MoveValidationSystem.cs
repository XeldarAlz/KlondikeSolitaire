using KlondikeSolitaire.Core;

namespace KlondikeSolitaire.Systems
{
    public sealed class MoveValidationSystem
    {
        public bool IsValidMove(BoardModel board, PileId source, PileId dest, int cardCount)
        {
            if (cardCount <= 0)
            {
                return false;
            }

            PileModel sourcePile = board.GetPile(source);
            PileModel destPile = board.GetPile(dest);

            if (sourcePile == null || destPile == null)
            {
                return false;
            }

            if (sourcePile.Count < cardCount)
            {
                return false;
            }

            return (source.Type, dest.Type) switch
            {
                (PileType.Tableau, PileType.Tableau) => IsValidTableauToTableau(sourcePile, destPile, cardCount),
                (PileType.Tableau, PileType.Foundation) => IsValidTableauToFoundation(sourcePile, destPile, cardCount),
                (PileType.Waste, PileType.Tableau) => IsValidWasteToTableau(sourcePile, destPile),
                (PileType.Waste, PileType.Foundation) => IsValidWasteToFoundation(sourcePile, destPile),
                (PileType.Foundation, PileType.Tableau) => IsValidFoundationToTableau(sourcePile, destPile),
                _ => false
            };
        }

        public PileId? FindBestTarget(BoardModel board, PileId source, int cardCount)
        {
            PileModel sourcePile = board.GetPile(source);
            if (sourcePile == null || sourcePile.Count < cardCount)
            {
                return null;
            }

            if (cardCount == 1)
            {
                for (int foundationIndex = 0; foundationIndex < BoardModel.FOUNDATION_COUNT; foundationIndex++)
                {
                    PileId foundationId = PileId.Foundation(foundationIndex);
                    if (IsValidMove(board, source, foundationId, cardCount))
                    {
                        return foundationId;
                    }
                }
            }

            for (int tableauIndex = 0; tableauIndex < BoardModel.TABLEAU_COUNT; tableauIndex++)
            {
                PileId tableauId = PileId.Tableau(tableauIndex);
                if (tableauId == source)
                {
                    continue;
                }

                if (IsValidMove(board, source, tableauId, cardCount))
                {
                    return tableauId;
                }
            }

            return null;
        }

        private static bool IsValidTableauToTableau(PileModel source, PileModel dest, int cardCount)
        {
            if (!IsValidSequence(source, cardCount))
            {
                return false;
            }

            CardModel bottomCard = source.Cards[source.Count - cardCount];

            if (dest.Count == 0)
            {
                return bottomCard.Rank == Rank.King;
            }

            CardModel destTop = dest.TopCard;
            return IsAlternatingColor(bottomCard, destTop) && bottomCard.Value == destTop.Value - 1;
        }

        private static bool IsValidTableauToFoundation(PileModel source, PileModel dest, int cardCount)
        {
            if (cardCount != 1)
            {
                return false;
            }

            CardModel card = source.TopCard;

            if (dest.Count == 0)
            {
                return card.Rank == Rank.Ace;
            }

            CardModel destTop = dest.TopCard;
            return card.Suit == destTop.Suit && card.Value == destTop.Value + 1;
        }

        private static bool IsValidWasteToTableau(PileModel source, PileModel dest)
        {
            if (source.Count == 0)
            {
                return false;
            }

            CardModel card = source.TopCard;

            if (dest.Count == 0)
            {
                return card.Rank == Rank.King;
            }

            CardModel destTop = dest.TopCard;
            return IsAlternatingColor(card, destTop) && card.Value == destTop.Value - 1;
        }

        private static bool IsValidWasteToFoundation(PileModel source, PileModel dest)
        {
            if (source.Count == 0)
            {
                return false;
            }

            CardModel card = source.TopCard;

            if (dest.Count == 0)
            {
                return card.Rank == Rank.Ace;
            }

            CardModel destTop = dest.TopCard;
            return card.Suit == destTop.Suit && card.Value == destTop.Value + 1;
        }

        private static bool IsValidFoundationToTableau(PileModel source, PileModel dest)
        {
            if (source.Count == 0)
            {
                return false;
            }

            CardModel card = source.TopCard;

            if (dest.Count == 0)
            {
                return card.Rank == Rank.King;
            }

            CardModel destTop = dest.TopCard;
            return IsAlternatingColor(card, destTop) && card.Value == destTop.Value - 1;
        }

        private static bool IsValidSequence(PileModel pile, int cardCount)
        {
            int startIndex = pile.Count - cardCount;

            for (int cardIndex = startIndex; cardIndex < pile.Count - 1; cardIndex++)
            {
                CardModel lower = pile.Cards[cardIndex];
                CardModel upper = pile.Cards[cardIndex + 1];

                if (!lower.IsFaceUp.Value || !upper.IsFaceUp.Value)
                {
                    return false;
                }

                if (!IsAlternatingColor(upper, lower))
                {
                    return false;
                }

                if (upper.Value != lower.Value - 1)
                {
                    return false;
                }
            }

            if (!pile.Cards[startIndex].IsFaceUp.Value)
            {
                return false;
            }

            return true;
        }

        private static bool IsAlternatingColor(CardModel card, CardModel other)
        {
            return card.Color != other.Color;
        }
    }
}
