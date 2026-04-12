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
        private readonly Random _random;
        private readonly List<CardModel> _deck;

        public DealSystem(BoardModel board, IPublisher<DealCompletedMessage> dealCompletedPublisher, Random random)
        {
            _board = board;
            _dealCompletedPublisher = dealCompletedPublisher;
            _random = random;
            _deck = CreateDeck();
        }

        public void CreateDeal()
        {
            Reset();
            ResetDeck();
            ShuffleDeck();

            DealToTableau();
            DealToStock();

            _dealCompletedPublisher.Publish(new DealCompletedMessage());
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
            List<CardModel> deck = new List<CardModel>(capacity: 52);

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
                _deck[cardIndex].IsFaceUp.Value = false;
            }
        }

        private void ShuffleDeck()
        {
            for (int cardIndex = _deck.Count - 1; cardIndex > 0; cardIndex--)
            {
                int randomIndex = _random.Next(cardIndex + 1);

                CardModel temp = _deck[cardIndex];
                _deck[cardIndex] = _deck[randomIndex];
                _deck[randomIndex] = temp;
            }
        }

        private void DealToTableau()
        {
            int cardIndex = 0;

            for (int columnIndex = 0; columnIndex < 7; columnIndex++)
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
            for (int cardIndex = 28; cardIndex < _deck.Count; cardIndex++)
            {
                _board.Stock.AddCard(_deck[cardIndex]);
            }
        }
    }
}
