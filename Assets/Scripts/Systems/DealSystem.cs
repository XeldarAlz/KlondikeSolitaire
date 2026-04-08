using System;
using System.Collections.Generic;
using KlondikeSolitaire.Core;
using MessagePipe;

namespace KlondikeSolitaire.Systems
{
    public sealed class DealSystem
    {
        private readonly BoardModel _board;
        private readonly IPublisher<DealCompletedMessage> _dealCompletedPublisher;

        public DealSystem(BoardModel board, IPublisher<DealCompletedMessage> dealCompletedPublisher)
        {
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _dealCompletedPublisher = dealCompletedPublisher ?? throw new ArgumentNullException(nameof(dealCompletedPublisher));
        }

        public void CreateDeal()
        {
            Reset();

            List<CardModel> deck = CreateDeck();
            ShuffleDeck(deck);

            DealToTableau(deck);
            DealToStock(deck);

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

            Suit[] suits = new[] { Suit.Hearts, Suit.Diamonds, Suit.Clubs, Suit.Spades };
            Rank[] ranks = new[]
            {
                Rank.Ace, Rank.Two, Rank.Three, Rank.Four, Rank.Five, Rank.Six, Rank.Seven,
                Rank.Eight, Rank.Nine, Rank.Ten, Rank.Jack, Rank.Queen, Rank.King
            };

            for (int suitIndex = 0; suitIndex < suits.Length; suitIndex++)
            {
                for (int rankIndex = 0; rankIndex < ranks.Length; rankIndex++)
                {
                    deck.Add(new CardModel(suits[suitIndex], ranks[rankIndex]));
                }
            }

            return deck;
        }

        private void ShuffleDeck(List<CardModel> deck)
        {
            Random random = new Random();

            for (int cardIndex = deck.Count - 1; cardIndex > 0; cardIndex--)
            {
                int randomIndex = random.Next(cardIndex + 1);

                CardModel temp = deck[cardIndex];
                deck[cardIndex] = deck[randomIndex];
                deck[randomIndex] = temp;
            }
        }

        private void DealToTableau(List<CardModel> deck)
        {
            int cardIndex = 0;

            for (int columnIndex = 0; columnIndex < 7; columnIndex++)
            {
                for (int rowIndex = 0; rowIndex <= columnIndex; rowIndex++)
                {
                    _board.Tableau[columnIndex].AddCard(deck[cardIndex]);
                    cardIndex++;
                }

                _board.Tableau[columnIndex].TopCard.IsFaceUp.Value = true;
            }
        }

        private void DealToStock(List<CardModel> deck)
        {
            for (int cardIndex = 28; cardIndex < deck.Count; cardIndex++)
            {
                _board.Stock.AddCard(deck[cardIndex]);
            }
        }
    }
}
