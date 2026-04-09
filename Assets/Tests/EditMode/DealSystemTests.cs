using System.Collections.Generic;
using KlondikeSolitaire.Core;
using KlondikeSolitaire.Systems;
using NUnit.Framework;

namespace KlondikeSolitaire.Tests
{
    [TestFixture]
    public sealed class DealSystemTests
    {
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

        // --- Constructor guard tests ---

        [Test]
        public void Constructor_NullBoard_ThrowsArgumentNullException()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                new DealSystem(null, _publisher));
        }

        [Test]
        public void Constructor_NullPublisher_ThrowsArgumentNullException()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                new DealSystem(_board, null));
        }

        // --- CreateDeal: deck uniqueness (Fisher-Yates produces 52 unique cards) ---

        [Test]
        public void CreateDeal_AllPiles_ContainExactly52TotalCards()
        {
            _sut.CreateDeal();

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
            _sut.CreateDeal();

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
        public void CreateDeal_TableauColumn0_HasOneCard()
        {
            _sut.CreateDeal();

            Assert.That(_board.Tableau[0].Count, Is.EqualTo(1));
        }

        [Test]
        public void CreateDeal_TableauColumn1_HasTwoCards()
        {
            _sut.CreateDeal();

            Assert.That(_board.Tableau[1].Count, Is.EqualTo(2));
        }

        [Test]
        public void CreateDeal_TableauColumn2_HasThreeCards()
        {
            _sut.CreateDeal();

            Assert.That(_board.Tableau[2].Count, Is.EqualTo(3));
        }

        [Test]
        public void CreateDeal_TableauColumn3_HasFourCards()
        {
            _sut.CreateDeal();

            Assert.That(_board.Tableau[3].Count, Is.EqualTo(4));
        }

        [Test]
        public void CreateDeal_TableauColumn4_HasFiveCards()
        {
            _sut.CreateDeal();

            Assert.That(_board.Tableau[4].Count, Is.EqualTo(5));
        }

        [Test]
        public void CreateDeal_TableauColumn5_HasSixCards()
        {
            _sut.CreateDeal();

            Assert.That(_board.Tableau[5].Count, Is.EqualTo(6));
        }

        [Test]
        public void CreateDeal_TableauColumn6_HasSevenCards()
        {
            _sut.CreateDeal();

            Assert.That(_board.Tableau[6].Count, Is.EqualTo(7));
        }

        [Test]
        public void CreateDeal_EachTableauColumnI_HasIPlusOneCards()
        {
            _sut.CreateDeal();

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
            _sut.CreateDeal();

            for (int columnIndex = 0; columnIndex < 7; columnIndex++)
            {
                Assert.That(_board.Tableau[columnIndex].TopCard.IsFaceUp.Value, Is.True,
                    $"Top card of tableau column {columnIndex} should be face up");
            }
        }

        [Test]
        public void CreateDeal_NonTopCardsOfEachTableauColumn_AreFaceDown()
        {
            _sut.CreateDeal();

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
            _sut.CreateDeal();

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
            _sut.CreateDeal();

            Assert.That(_board.Stock.Count, Is.EqualTo(24));
        }

        [Test]
        public void CreateDeal_AllStockCards_AreFaceDown()
        {
            _sut.CreateDeal();

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
            _sut.CreateDeal();

            Assert.That(_board.Waste.Count, Is.EqualTo(0));
        }

        // --- CreateDeal: foundation piles ---

        [Test]
        public void CreateDeal_AllFoundations_AreEmpty()
        {
            _sut.CreateDeal();

            for (int foundationIndex = 0; foundationIndex < 4; foundationIndex++)
            {
                Assert.That(_board.Foundations[foundationIndex].Count, Is.EqualTo(0),
                    $"Foundation {foundationIndex} should be empty after deal");
            }
        }

        // --- CreateDeal: message publishing ---

        [Test]
        public void CreateDeal_PublishesDealCompletedMessage()
        {
            _sut.CreateDeal();

            Assert.That(_publisher.MessageCount, Is.EqualTo(1));
        }

        [Test]
        public void CreateDeal_CalledTwice_PublishesTwoDealCompletedMessages()
        {
            _sut.CreateDeal();
            _sut.CreateDeal();

            Assert.That(_publisher.MessageCount, Is.EqualTo(2));
        }

        // --- Reset ---

        [Test]
        public void Reset_AfterDeal_StockIsEmpty()
        {
            _sut.CreateDeal();

            _sut.Reset();

            Assert.That(_board.Stock.Count, Is.EqualTo(0));
        }

        [Test]
        public void Reset_AfterDeal_WasteIsEmpty()
        {
            _sut.CreateDeal();

            _sut.Reset();

            Assert.That(_board.Waste.Count, Is.EqualTo(0));
        }

        [Test]
        public void Reset_AfterDeal_AllFoundationsAreEmpty()
        {
            _sut.CreateDeal();

            _sut.Reset();

            for (int foundationIndex = 0; foundationIndex < 4; foundationIndex++)
            {
                Assert.That(_board.Foundations[foundationIndex].Count, Is.EqualTo(0),
                    $"Foundation {foundationIndex} should be empty after Reset");
            }
        }

        [Test]
        public void Reset_AfterDeal_AllTableauColumnsAreEmpty()
        {
            _sut.CreateDeal();

            _sut.Reset();

            for (int columnIndex = 0; columnIndex < 7; columnIndex++)
            {
                Assert.That(_board.Tableau[columnIndex].Count, Is.EqualTo(0),
                    $"Tableau column {columnIndex} should be empty after Reset");
            }
        }

        [Test]
        public void Reset_AfterDeal_AllPilesAreEmpty()
        {
            _sut.CreateDeal();

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

        [Test]
        public void Reset_DoesNotPublishDealCompletedMessage()
        {
            _sut.Reset();

            Assert.That(_publisher.MessageCount, Is.EqualTo(0));
        }

        // --- CreateDeal: re-deal produces a valid board ---

        [Test]
        public void CreateDeal_AfterReset_StillDeals52Cards()
        {
            _sut.CreateDeal();
            _sut.Reset();
            _sut.CreateDeal();

            int totalCards = 0;
            for (int pileIndex = 0; pileIndex < _board.AllPiles.Length; pileIndex++)
            {
                totalCards += _board.AllPiles[pileIndex].Count;
            }

            Assert.That(totalCards, Is.EqualTo(52));
        }

        [Test]
        public void CreateDeal_TableauAndStockCardCounts_SumTo52()
        {
            _sut.CreateDeal();

            int tableauTotal = 0;
            for (int columnIndex = 0; columnIndex < 7; columnIndex++)
            {
                tableauTotal += _board.Tableau[columnIndex].Count;
            }

            int stockTotal = _board.Stock.Count;

            Assert.That(tableauTotal + stockTotal, Is.EqualTo(52));
        }
    }
}
