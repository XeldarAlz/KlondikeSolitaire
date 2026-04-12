using System.Collections.Generic;

namespace KlondikeSolitaire.Core
{
    public sealed class PileModel
    {
        private readonly List<CardModel> _cards;

        public PileType PileType { get; }
        public int PileIndex { get; }
        public PileId Id => new(PileType, PileIndex);
        public IReadOnlyList<CardModel> Cards => _cards;
        public CardModel TopCard => _cards.Count > 0 ? _cards[_cards.Count - 1] : null;
        public int Count => _cards.Count;

        public int FaceUpCount
        {
            get
            {
                int count = 0;
                for (int cardIndex = 0; cardIndex < _cards.Count; cardIndex++)
                {
                    if (_cards[cardIndex].IsFaceUp.Value)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        public PileModel(PileType pileType, int pileIndex)
        {
            PileType = pileType;
            PileIndex = pileIndex;
            _cards = new List<CardModel>(capacity: 24);
        }

        public void AddCard(CardModel card)
        {
            _cards.Add(card);
        }

        public void RemoveTop(int count)
        {
            _cards.RemoveRange(_cards.Count - count, count);
        }

        public void TransferTop(int count, PileModel destination)
        {
            int startIndex = _cards.Count - count;
            for (int cardIndex = startIndex; cardIndex < _cards.Count; cardIndex++)
            {
                destination._cards.Add(_cards[cardIndex]);
            }
            _cards.RemoveRange(startIndex, count);
        }

        public void TransferAllReversed(PileModel destination)
        {
            for (int cardIndex = _cards.Count - 1; cardIndex >= 0; cardIndex--)
            {
                destination._cards.Add(_cards[cardIndex]);
            }
            _cards.Clear();
        }

        public void Clear()
        {
            _cards.Clear();
        }
    }
}
