using System.Collections.Generic;

namespace KlondikeSolitaire.Core
{
    public sealed class PileModel
    {
        private readonly List<CardModel> _cards;

        public PileType PileType { get; }
        public int PileIndex { get; }
        public PileId Id => new PileId(PileType, PileIndex);
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

        public void AddCards(IReadOnlyList<CardModel> cards)
        {
            for (int cardIndex = 0; cardIndex < cards.Count; cardIndex++)
            {
                _cards.Add(cards[cardIndex]);
            }
        }

        public List<CardModel> RemoveTop(int count)
        {
            int startIndex = _cards.Count - count;
            List<CardModel> removed = _cards.GetRange(startIndex, count);
            _cards.RemoveRange(startIndex, count);
            return removed;
        }

        public List<CardModel> RemoveAll()
        {
            List<CardModel> removed = new List<CardModel>(_cards);
            _cards.Clear();
            return removed;
        }

        public void Clear()
        {
            _cards.Clear();
        }
    }
}
