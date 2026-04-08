using System;
using KlondikeSolitaire.Core;

namespace KlondikeSolitaire.Tests
{
    public static class TestBoardFactory
    {
        public static BoardModel EmptyBoard()
        {
            return new BoardModel();
        }

        public static BoardModel StandardDealBoard()
        {
            BoardModel board = new BoardModel();

            CardModel[] deck = BuildOrderedDeck();
            int deckIndex = 0;

            for (int columnIndex = 0; columnIndex < 7; columnIndex++)
            {
                int cardCount = columnIndex + 1;
                for (int cardIndex = 0; cardIndex < cardCount; cardIndex++)
                {
                    CardModel card = deck[deckIndex];
                    deckIndex++;

                    if (cardIndex == cardCount - 1)
                    {
                        card.IsFaceUp.Value = true;
                    }

                    board.Tableau[columnIndex].AddCard(card);
                }
            }

            while (deckIndex < deck.Length)
            {
                board.Stock.AddCard(deck[deckIndex]);
                deckIndex++;
            }

            return board;
        }

        public static BoardModel AlmostWonBoard()
        {
            BoardModel board = new BoardModel();

            Suit[] suits = { Suit.Hearts, Suit.Diamonds, Suit.Clubs, Suit.Spades };

            for (int foundationIndex = 0; foundationIndex < 3; foundationIndex++)
            {
                for (int rankValue = 1; rankValue <= 13; rankValue++)
                {
                    CardModel card = new CardModel(suits[foundationIndex], (Rank)rankValue);
                    card.IsFaceUp.Value = true;
                    board.Foundations[foundationIndex].AddCard(card);
                }
            }

            for (int rankValue = 1; rankValue <= 12; rankValue++)
            {
                CardModel card = new CardModel(suits[3], (Rank)rankValue);
                card.IsFaceUp.Value = true;
                board.Foundations[3].AddCard(card);
            }

            CardModel kingOfSpades = new CardModel(Suit.Spades, Rank.King);
            kingOfSpades.IsFaceUp.Value = true;
            board.Tableau[0].AddCard(kingOfSpades);

            return board;
        }

        public static BoardModel NoMovesBoard()
        {
            BoardModel board = new BoardModel();

            Rank[] ranks = { Rank.Six, Rank.Six, Rank.Six, Rank.Six, Rank.Eight, Rank.Eight, Rank.Eight };
            Suit[] suits = { Suit.Hearts, Suit.Diamonds, Suit.Clubs, Suit.Spades, Suit.Hearts, Suit.Diamonds, Suit.Clubs };

            for (int columnIndex = 0; columnIndex < 7; columnIndex++)
            {
                CardModel card = new CardModel(suits[columnIndex], ranks[columnIndex]);
                card.IsFaceUp.Value = true;
                board.Tableau[columnIndex].AddCard(card);
            }

            return board;
        }

        public static BoardModel AutoCompletableBoard()
        {
            BoardModel board = new BoardModel();

            Suit[][] columnSequences = new Suit[4][];
            columnSequences[0] = new Suit[] { Suit.Spades, Suit.Hearts, Suit.Spades, Suit.Hearts, Suit.Spades, Suit.Hearts, Suit.Spades, Suit.Hearts, Suit.Spades, Suit.Hearts, Suit.Spades, Suit.Hearts, Suit.Spades };
            columnSequences[1] = new Suit[] { Suit.Diamonds, Suit.Clubs, Suit.Diamonds, Suit.Clubs, Suit.Diamonds, Suit.Clubs, Suit.Diamonds, Suit.Clubs, Suit.Diamonds, Suit.Clubs, Suit.Diamonds, Suit.Clubs, Suit.Diamonds };
            columnSequences[2] = new Suit[] { Suit.Hearts, Suit.Spades, Suit.Hearts, Suit.Spades, Suit.Hearts, Suit.Spades, Suit.Hearts, Suit.Spades, Suit.Hearts, Suit.Spades, Suit.Hearts, Suit.Spades, Suit.Hearts };
            columnSequences[3] = new Suit[] { Suit.Clubs, Suit.Diamonds, Suit.Clubs, Suit.Diamonds, Suit.Clubs, Suit.Diamonds, Suit.Clubs, Suit.Diamonds, Suit.Clubs, Suit.Diamonds, Suit.Clubs, Suit.Diamonds, Suit.Clubs };

            for (int columnIndex = 0; columnIndex < 4; columnIndex++)
            {
                for (int rankValue = 13; rankValue >= 1; rankValue--)
                {
                    Suit suit = columnSequences[columnIndex][13 - rankValue];
                    CardModel card = new CardModel(suit, (Rank)rankValue);
                    card.IsFaceUp.Value = true;
                    board.Tableau[columnIndex].AddCard(card);
                }
            }

            return board;
        }

        public static BoardModel CustomBoard(Action<BoardModel> setup)
        {
            BoardModel board = new BoardModel();
            setup(board);
            return board;
        }

        private static CardModel[] BuildOrderedDeck()
        {
            Suit[] suits = { Suit.Hearts, Suit.Diamonds, Suit.Clubs, Suit.Spades };
            CardModel[] deck = new CardModel[52];
            int deckIndex = 0;

            for (int suitIndex = 0; suitIndex < suits.Length; suitIndex++)
            {
                for (int rankValue = 1; rankValue <= 13; rankValue++)
                {
                    deck[deckIndex] = new CardModel(suits[suitIndex], (Rank)rankValue);
                    deckIndex++;
                }
            }

            return deck;
        }
    }
}
