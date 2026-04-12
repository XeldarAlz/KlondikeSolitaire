using System.Collections.Generic;
using KlondikeSolitaire.Core;
using KlondikeSolitaire.Systems;
using NUnit.Framework;

namespace KlondikeSolitaire.Tests
{
    [TestFixture]
    public sealed class DealSystemTests
    {
        private const int TEST_SEED = 42;

        private BoardModel _board;
        private TestPublisher<DealCompletedMessage> _publisher;
        private DealSystem _sut;

        [SetUp]
        public void SetUp()
        {
            _board = TestBoardFactory.EmptyBoard();
            _publisher = new TestPublisher<DealCompletedMessage>();
            _sut = new DealSystem(_board, _publisher);
        }

        // --- CreateDeal: deck uniqueness (Fisher-Yates produces 52 unique cards) ---

        [Test]
        public void CreateDeal_AllPiles_ContainExactly52TotalCards()
        {
            _sut.CreateDeal(TEST_SEED);

            int totalCards = 0;
            for (int pileIndex = 0; pileIndex < _board.AllPiles.Length; pileIndex++)
            {
                totalCards += _board.AllPiles[pileIndex].Count;
            }

            Assert.That(totalCards, Is.EqualTo(52));
        }

        [Test]
        public void CreateDeal_AllCards_AreUniqueSuitRankCombinations()
        {
            _sut.CreateDeal(TEST_SEED);

            var seen = new HashSet<(Suit, Rank)>();
            for (int pileIndex = 0; pileIndex < _board.AllPiles.Length; pileIndex++)
            {
                IReadOnlyList<CardModel> cards = _board.AllPiles[pileIndex].Cards;
                for (int cardIndex = 0; cardIndex < cards.Count; cardIndex++)
                {
                    bool added = seen.Add((cards[cardIndex].Suit, cards[cardIndex].Rank));
                    Assert.That(added, Is.True,
                        $"Duplicate card found: {cards[cardIndex].Suit} {cards[cardIndex].Rank}");
                }
            }

            Assert.That(seen.Count, Is.EqualTo(52));
        }

        // --- CreateDeal: tableau column card counts ---

        [Test]
        public void CreateDeal_EachTableauColumnI_HasIPlusOneCards()
        {
            _sut.CreateDeal(TEST_SEED);

            for (int columnIndex = 0; columnIndex < 7; columnIndex++)
            {
                Assert.That(_board.Tableau[columnIndex].Count, Is.EqualTo(columnIndex + 1),
                    $"Tableau column {columnIndex} should have {columnIndex + 1} cards");
            }
        }

        // --- CreateDeal: face-up/face-down state on tableau ---

        [Test]
        public void CreateDeal_TopCardOfEachTableauColumn_IsFaceUp()
        {
            _sut.CreateDeal(TEST_SEED);

            for (int columnIndex = 0; columnIndex < 7; columnIndex++)
            {
                Assert.That(_board.Tableau[columnIndex].TopCard.IsFaceUp.Value, Is.True,
                    $"Top card of tableau column {columnIndex} should be face up");
            }
        }

        [Test]
        public void CreateDeal_NonTopCardsOfEachTableauColumn_AreFaceDown()
        {
            _sut.CreateDeal(TEST_SEED);

            for (int columnIndex = 0; columnIndex < 7; columnIndex++)
            {
                IReadOnlyList<CardModel> cards = _board.Tableau[columnIndex].Cards;
                int nonTopCount = cards.Count - 1;
                for (int cardIndex = 0; cardIndex < nonTopCount; cardIndex++)
                {
                    Assert.That(cards[cardIndex].IsFaceUp.Value, Is.False,
                        $"Card at index {cardIndex} in tableau column {columnIndex} should be face down");
                }
            }
        }

        [Test]
        public void CreateDeal_EachTableauColumn_HasExactlyOneFaceUpCard()
        {
            _sut.CreateDeal(TEST_SEED);

            for (int columnIndex = 0; columnIndex < 7; columnIndex++)
            {
                Assert.That(_board.Tableau[columnIndex].FaceUpCount, Is.EqualTo(1),
                    $"Tableau column {columnIndex} should have exactly 1 face-up card");
            }
        }

        // --- CreateDeal: stock pile ---

        [Test]
        public void CreateDeal_Stock_Has24Cards()
        {
            _sut.CreateDeal(TEST_SEED);

            Assert.That(_board.Stock.Count, Is.EqualTo(24));
        }

        [Test]
        public void CreateDeal_AllStockCards_AreFaceDown()
        {
            _sut.CreateDeal(TEST_SEED);

            IReadOnlyList<CardModel> stockCards = _board.Stock.Cards;
            for (int cardIndex = 0; cardIndex < stockCards.Count; cardIndex++)
            {
                Assert.That(stockCards[cardIndex].IsFaceUp.Value, Is.False,
                    $"Stock card at index {cardIndex} should be face down");
            }
        }

        // --- CreateDeal: waste pile ---

        [Test]
        public void CreateDeal_Waste_IsEmpty()
        {
            _sut.CreateDeal(TEST_SEED);

            Assert.That(_board.Waste.Count, Is.EqualTo(0));
        }

        // --- CreateDeal: foundation piles ---

        [Test]
        public void CreateDeal_AllFoundations_AreEmpty()
        {
            _sut.CreateDeal(TEST_SEED);

            for (int foundationIndex = 0; foundationIndex < 4; foundationIndex++)
            {
                Assert.That(_board.Foundations[foundationIndex].Count, Is.EqualTo(0),
                    $"Foundation {foundationIndex} should be empty after deal");
            }
        }

        // --- Reset ---

        [Test]
        public void Reset_AfterDeal_AllPilesAreEmpty()
        {
            _sut.CreateDeal(TEST_SEED);

            _sut.Reset();

            for (int pileIndex = 0; pileIndex < _board.AllPiles.Length; pileIndex++)
            {
                Assert.That(_board.AllPiles[pileIndex].Count, Is.EqualTo(0),
                    $"Pile at index {pileIndex} should be empty after Reset");
            }
        }

        [Test]
        public void Reset_OnEmptyBoard_AllPilesRemainEmpty()
        {
            _sut.Reset();

            for (int pileIndex = 0; pileIndex < _board.AllPiles.Length; pileIndex++)
            {
                Assert.That(_board.AllPiles[pileIndex].Count, Is.EqualTo(0),
                    $"Pile at index {pileIndex} should remain empty");
            }
        }

        // --- CreateDeal: re-deal produces a valid board ---

        [Test]
        public void CreateDeal_AfterReset_StillDeals52Cards()
        {
            _sut.CreateDeal(TEST_SEED);
            _sut.Reset();
            _sut.CreateDeal(TEST_SEED);

            int totalCards = 0;
            for (int pileIndex = 0; pileIndex < _board.AllPiles.Length; pileIndex++)
            {
                totalCards += _board.AllPiles[pileIndex].Count;
            }

            Assert.That(totalCards, Is.EqualTo(52));
        }

        // --- Determinism: same seed produces same deal ---

        [Test]
        public void CreateDeal_SameSeed_ProducesIdenticalCardOrder()
        {
            _sut.CreateDeal(TEST_SEED);

            var firstDealCards = new List<(Suit, Rank)>();
            for (int pileIndex = 0; pileIndex < _board.AllPiles.Length; pileIndex++)
            {
                IReadOnlyList<CardModel> cards = _board.AllPiles[pileIndex].Cards;
                for (int cardIndex = 0; cardIndex < cards.Count; cardIndex++)
                {
                    firstDealCards.Add((cards[cardIndex].Suit, cards[cardIndex].Rank));
                }
            }

            _sut.CreateDeal(TEST_SEED);

            int verifyIndex = 0;
            for (int pileIndex = 0; pileIndex < _board.AllPiles.Length; pileIndex++)
            {
                IReadOnlyList<CardModel> cards = _board.AllPiles[pileIndex].Cards;
                for (int cardIndex = 0; cardIndex < cards.Count; cardIndex++)
                {
                    Assert.That(cards[cardIndex].Suit, Is.EqualTo(firstDealCards[verifyIndex].Item1),
                        $"Suit mismatch at position {verifyIndex}");
                    Assert.That(cards[cardIndex].Rank, Is.EqualTo(firstDealCards[verifyIndex].Item2),
                        $"Rank mismatch at position {verifyIndex}");
                    verifyIndex++;
                }
            }
        }

        [Test]
        public void CreateDeal_DifferentSeeds_ProduceDifferentCardOrder()
        {
            _sut.CreateDeal(1);

            var firstDealCards = new List<(Suit, Rank)>();
            for (int pileIndex = 0; pileIndex < _board.AllPiles.Length; pileIndex++)
            {
                IReadOnlyList<CardModel> cards = _board.AllPiles[pileIndex].Cards;
                for (int cardIndex = 0; cardIndex < cards.Count; cardIndex++)
                {
                    firstDealCards.Add((cards[cardIndex].Suit, cards[cardIndex].Rank));
                }
            }

            _sut.CreateDeal(2);

            bool anyDifference = false;
            int verifyIndex = 0;
            for (int pileIndex = 0; pileIndex < _board.AllPiles.Length; pileIndex++)
            {
                IReadOnlyList<CardModel> cards = _board.AllPiles[pileIndex].Cards;
                for (int cardIndex = 0; cardIndex < cards.Count; cardIndex++)
                {
                    if (cards[cardIndex].Suit != firstDealCards[verifyIndex].Item1
                        || cards[cardIndex].Rank != firstDealCards[verifyIndex].Item2)
                    {
                        anyDifference = true;
                    }
                    verifyIndex++;
                }
            }

            Assert.That(anyDifference, Is.True, "Different seeds should produce different deals");
        }
    }
}
