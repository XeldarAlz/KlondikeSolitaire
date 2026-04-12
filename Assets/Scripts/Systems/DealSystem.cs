using System;
using System.Collections.Generic;
using KlondikeSolitaire.Core;
using MessagePipe;

namespace KlondikeSolitaire.Systems
{
    public sealed class DealSystem
    {
        private static readonly Suit[] Suits = { Suit.Hearts, Suit.Diamonds, Suit.Clubs, Suit.Spades };
        private static readonly Rank[] Ranks =
        {
            Rank.Ace, Rank.Two, Rank.Three, Rank.Four, Rank.Five, Rank.Six, Rank.Seven,
            Rank.Eight, Rank.Nine, Rank.Ten, Rank.Jack, Rank.Queen, Rank.King
        };

        private readonly BoardModel _board;
        private readonly IPublisher<DealCompletedMessage> _dealCompletedPublisher;
        private readonly List<CardModel> _deck;
        private readonly CardModel[] _canonicalOrder;

        public DealSystem(BoardModel board, IPublisher<DealCompletedMessage> dealCompletedPublisher)
        {
            _board = board;
            _dealCompletedPublisher = dealCompletedPublisher;
            _deck = CreateDeck();
            _canonicalOrder = new CardModel[_deck.Count];
            _deck.CopyTo(_canonicalOrder);
        }

        public void CreateDeal(int seed)
        {
            Reset();
            ResetDeck();
            ShuffleDeck(new Random(seed));

            DealToTableau();
            DealToStock();

            _dealCompletedPublisher.Publish(new DealCompletedMessage(seed));
        }

        public void Reset()
        {
            for (int pileIndex = 0; pileIndex < _board.AllPiles.Length; pileIndex++)
            {
                _board.AllPiles[pileIndex].Clear();
            }
        }

        private List<CardModel> CreateDeck()
        {
            List<CardModel> deck = new List<CardModel>(capacity: BoardModel.DECK_SIZE);

            for (int suitIndex = 0; suitIndex < Suits.Length; suitIndex++)
            {
                for (int rankIndex = 0; rankIndex < Ranks.Length; rankIndex++)
                {
                    deck.Add(new CardModel(Suits[suitIndex], Ranks[rankIndex]));
                }
            }

            return deck;
        }

        private void ResetDeck()
        {
            for (int cardIndex = 0; cardIndex < _deck.Count; cardIndex++)
            {
                _deck[cardIndex] = _canonicalOrder[cardIndex];
                _deck[cardIndex].IsFaceUp.Value = false;
            }
        }

        private void ShuffleDeck(Random random)
        {
            for (int cardIndex = _deck.Count - 1; cardIndex > 0; cardIndex--)
            {
                int randomIndex = random.Next(cardIndex + 1);

                (_deck[cardIndex], _deck[randomIndex]) = (_deck[randomIndex], _deck[cardIndex]);
            }
        }

        private void DealToTableau()
        {
            int cardIndex = 0;

            for (int columnIndex = 0; columnIndex < BoardModel.TABLEAU_COUNT; columnIndex++)
            {
                for (int rowIndex = 0; rowIndex <= columnIndex; rowIndex++)
                {
                    _board.Tableau[columnIndex].AddCard(_deck[cardIndex]);
                    cardIndex++;
                }

                _board.Tableau[columnIndex].TopCard.IsFaceUp.Value = true;
            }
        }

        private void DealToStock()
        {
            for (int cardIndex = BoardModel.TABLEAU_DEAL_COUNT; cardIndex < _deck.Count; cardIndex++)
            {
                _board.Stock.AddCard(_deck[cardIndex]);
            }
        }
    }
}
