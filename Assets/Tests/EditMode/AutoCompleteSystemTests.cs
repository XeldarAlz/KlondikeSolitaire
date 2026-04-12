using System.Collections.Generic;
using KlondikeSolitaire.Core;
using KlondikeSolitaire.Systems;
using NUnit.Framework;

namespace KlondikeSolitaire.Tests
{
    [TestFixture]
    public sealed class AutoCompleteSystemTests
    {
        private BoardModel _board;
        private TestSubscriber<BoardStateChangedMessage> _boardStateSubscriber;
        private TestPublisher<AutoCompleteAvailableMessage> _autoCompletePublisher;
        private AutoCompleteSystem _sut;

        [SetUp]
        public void SetUp()
        {
            _board = TestBoardFactory.EmptyBoard();
            _boardStateSubscriber = new TestSubscriber<BoardStateChangedMessage>();
            _autoCompletePublisher = new TestPublisher<AutoCompleteAvailableMessage>();
            _sut = new AutoCompleteSystem(_board, _boardStateSubscriber, _autoCompletePublisher);
        }

        [TearDown]
        public void TearDown()
        {
            _sut.Dispose();
        }

        // --- IsAutoCompletePossible: true conditions ---

        [Test]
        public void IsAutoCompletePossible_EmptyBoard_ReturnsTrue()
        {
            bool result = _sut.IsAutoCompletePossible();

            Assert.That(result, Is.True);
        }

        [Test]
        public void IsAutoCompletePossible_AutoCompletableBoard_ReturnsTrue()
        {
            BoardModel board = TestBoardFactory.AutoCompletableBoard();
            var sut = new AutoCompleteSystem(board, _boardStateSubscriber, _autoCompletePublisher);

            bool result = sut.IsAutoCompletePossible();

            sut.Dispose();
            Assert.That(result, Is.True);
        }

        // --- IsAutoCompletePossible: false when stock has cards ---

        [Test]
        public void IsAutoCompletePossible_StockHasCards_ReturnsFalse()
        {
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                CardModel card = new CardModel(Suit.Hearts, Rank.Ace);
                b.Stock.AddCard(card);
            });
            var sut = new AutoCompleteSystem(board, _boardStateSubscriber, _autoCompletePublisher);

            bool result = sut.IsAutoCompletePossible();

            sut.Dispose();
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsAutoCompletePossible_StandardDealBoard_ReturnsFalse()
        {
            BoardModel board = TestBoardFactory.StandardDealBoard();
            var sut = new AutoCompleteSystem(board, _boardStateSubscriber, _autoCompletePublisher);

            bool result = sut.IsAutoCompletePossible();

            sut.Dispose();
            Assert.That(result, Is.False);
        }

        // --- IsAutoCompletePossible: false when waste has cards ---

        [Test]
        public void IsAutoCompletePossible_WasteHasCards_ReturnsFalse()
        {
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                CardModel card = new CardModel(Suit.Hearts, Rank.Ace);
                card.IsFaceUp.Value = true;
                b.Waste.AddCard(card);
            });
            var sut = new AutoCompleteSystem(board, _boardStateSubscriber, _autoCompletePublisher);

            bool result = sut.IsAutoCompletePossible();

            sut.Dispose();
            Assert.That(result, Is.False);
        }

        // --- IsAutoCompletePossible: false when any tableau card is face-down ---

        [Test]
        public void IsAutoCompletePossible_TableauHasFaceDownCard_ReturnsFalse()
        {
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                CardModel faceDownCard = new CardModel(Suit.Hearts, Rank.Two);
                CardModel faceUpCard = new CardModel(Suit.Spades, Rank.Ace);
                faceUpCard.IsFaceUp.Value = true;
                b.Tableau[0].AddCard(faceDownCard);
                b.Tableau[0].AddCard(faceUpCard);
            });
            var sut = new AutoCompleteSystem(board, _boardStateSubscriber, _autoCompletePublisher);

            bool result = sut.IsAutoCompletePossible();

            sut.Dispose();
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsAutoCompletePossible_AllTableauFaceUp_StockAndWasteEmpty_ReturnsTrue()
        {
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                CardModel card = new CardModel(Suit.Hearts, Rank.Ace);
                card.IsFaceUp.Value = true;
                b.Tableau[0].AddCard(card);
            });
            var sut = new AutoCompleteSystem(board, _boardStateSubscriber, _autoCompletePublisher);

            bool result = sut.IsAutoCompletePossible();

            sut.Dispose();
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsAutoCompletePossible_FaceDownCardInSecondTableauColumn_ReturnsFalse()
        {
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                CardModel faceUpCard = new CardModel(Suit.Hearts, Rank.Ace);
                faceUpCard.IsFaceUp.Value = true;
                b.Tableau[0].AddCard(faceUpCard);

                CardModel faceDownCard = new CardModel(Suit.Spades, Rank.King);
                b.Tableau[1].AddCard(faceDownCard);
            });
            var sut = new AutoCompleteSystem(board, _boardStateSubscriber, _autoCompletePublisher);

            bool result = sut.IsAutoCompletePossible();

            sut.Dispose();
            Assert.That(result, Is.False);
        }

        // --- GenerateMoveSequence ---

        [Test]
        public void GenerateMoveSequence_EmptyBoard_ReturnsEmptyList()
        {
            List<Move> result = _sut.GenerateMoveSequence();

            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void GenerateMoveSequence_AutoCompletableBoard_Returns52Moves()
        {
            BoardModel board = TestBoardFactory.AutoCompletableBoard();
            var sut = new AutoCompleteSystem(board, _boardStateSubscriber, _autoCompletePublisher);

            List<Move> result = sut.GenerateMoveSequence();

            sut.Dispose();
            Assert.That(result.Count, Is.EqualTo(52));
        }

        [Test]
        public void GenerateMoveSequence_AutoCompletableBoard_FirstMoveIsAce()
        {
            BoardModel board = TestBoardFactory.AutoCompletableBoard();
            var sut = new AutoCompleteSystem(board, _boardStateSubscriber, _autoCompletePublisher);

            List<Move> result = sut.GenerateMoveSequence();

            sut.Dispose();
            Assert.That(result[0].CardCount, Is.EqualTo(1));
            Assert.That(result[0].Destination.Type, Is.EqualTo(PileType.Foundation));
        }

        [Test]
        public void GenerateMoveSequence_AutoCompletableBoard_AllMovesAreToFoundation()
        {
            BoardModel board = TestBoardFactory.AutoCompletableBoard();
            var sut = new AutoCompleteSystem(board, _boardStateSubscriber, _autoCompletePublisher);

            List<Move> result = sut.GenerateMoveSequence();

            sut.Dispose();
            for (int moveIndex = 0; moveIndex < result.Count; moveIndex++)
            {
                Assert.That(result[moveIndex].Destination.Type, Is.EqualTo(PileType.Foundation),
                    $"Move {moveIndex} should go to a foundation pile");
            }
        }

        [Test]
        public void GenerateMoveSequence_AutoCompletableBoard_AllMovesAreFromTableau()
        {
            BoardModel board = TestBoardFactory.AutoCompletableBoard();
            var sut = new AutoCompleteSystem(board, _boardStateSubscriber, _autoCompletePublisher);

            List<Move> result = sut.GenerateMoveSequence();

            sut.Dispose();
            for (int moveIndex = 0; moveIndex < result.Count; moveIndex++)
            {
                Assert.That(result[moveIndex].Source.Type, Is.EqualTo(PileType.Tableau),
                    $"Move {moveIndex} should come from a tableau pile");
            }
        }

        [Test]
        public void GenerateMoveSequence_AutoCompletableBoard_AllMovesHaveCardCountOne()
        {
            BoardModel board = TestBoardFactory.AutoCompletableBoard();
            var sut = new AutoCompleteSystem(board, _boardStateSubscriber, _autoCompletePublisher);

            List<Move> result = sut.GenerateMoveSequence();

            sut.Dispose();
            for (int moveIndex = 0; moveIndex < result.Count; moveIndex++)
            {
                Assert.That(result[moveIndex].CardCount, Is.EqualTo(1),
                    $"Move {moveIndex} should move exactly one card");
            }
        }

        [Test]
        public void GenerateMoveSequence_SingleAceOnTableau_ReturnsSingleMove()
        {
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                CardModel ace = new CardModel(Suit.Hearts, Rank.Ace);
                ace.IsFaceUp.Value = true;
                b.Tableau[0].AddCard(ace);
            });
            var sut = new AutoCompleteSystem(board, _boardStateSubscriber, _autoCompletePublisher);

            List<Move> result = sut.GenerateMoveSequence();

            sut.Dispose();
            Assert.That(result.Count, Is.EqualTo(1));
        }

        [Test]
        public void GenerateMoveSequence_SingleAceOnTableau_MoveSourceIsTableau0()
        {
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                CardModel ace = new CardModel(Suit.Hearts, Rank.Ace);
                ace.IsFaceUp.Value = true;
                b.Tableau[0].AddCard(ace);
            });
            var sut = new AutoCompleteSystem(board, _boardStateSubscriber, _autoCompletePublisher);

            List<Move> result = sut.GenerateMoveSequence();

            sut.Dispose();
            Assert.That(result[0].Source, Is.EqualTo(PileId.Tableau(0)));
        }

        [Test]
        public void GenerateMoveSequence_SingleAceOnTableau_MoveDestinationIsHeartsFoundation()
        {
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                CardModel ace = new CardModel(Suit.Hearts, Rank.Ace);
                ace.IsFaceUp.Value = true;
                b.Tableau[0].AddCard(ace);
            });
            var sut = new AutoCompleteSystem(board, _boardStateSubscriber, _autoCompletePublisher);

            List<Move> result = sut.GenerateMoveSequence();

            sut.Dispose();
            Assert.That(result[0].Destination, Is.EqualTo(PileId.Foundation((int)Suit.Hearts)));
        }

        [Test]
        public void GenerateMoveSequence_DoesNotMutateBoard_TableauUnchanged()
        {
            BoardModel board = TestBoardFactory.AutoCompletableBoard();
            int[] originalCounts = new int[board.Tableau.Length];
            for (int tableauIndex = 0; tableauIndex < board.Tableau.Length; tableauIndex++)
            {
                originalCounts[tableauIndex] = board.Tableau[tableauIndex].Count;
            }

            var sut = new AutoCompleteSystem(board, _boardStateSubscriber, _autoCompletePublisher);
            sut.GenerateMoveSequence();

            sut.Dispose();
            for (int tableauIndex = 0; tableauIndex < board.Tableau.Length; tableauIndex++)
            {
                Assert.That(board.Tableau[tableauIndex].Count, Is.EqualTo(originalCounts[tableauIndex]),
                    $"Tableau[{tableauIndex}] should be unchanged after GenerateMoveSequence");
            }
        }

        [Test]
        public void GenerateMoveSequence_DoesNotMutateBoard_FoundationsUnchanged()
        {
            BoardModel board = TestBoardFactory.AutoCompletableBoard();
            int[] originalCounts = new int[board.Foundations.Length];
            for (int foundationIndex = 0; foundationIndex < board.Foundations.Length; foundationIndex++)
            {
                originalCounts[foundationIndex] = board.Foundations[foundationIndex].Count;
            }

            var sut = new AutoCompleteSystem(board, _boardStateSubscriber, _autoCompletePublisher);
            sut.GenerateMoveSequence();

            sut.Dispose();
            for (int foundationIndex = 0; foundationIndex < board.Foundations.Length; foundationIndex++)
            {
                Assert.That(board.Foundations[foundationIndex].Count, Is.EqualTo(originalCounts[foundationIndex]),
                    $"Foundation[{foundationIndex}] should be unchanged after GenerateMoveSequence");
            }
        }

        [Test]
        public void GenerateMoveSequence_LowestRankMovedFirst_AceBeforeTwoWhenBothEligible()
        {
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                CardModel aceSpades = new CardModel(Suit.Spades, Rank.Ace);
                aceSpades.IsFaceUp.Value = true;
                b.Tableau[1].AddCard(aceSpades);

                CardModel aceHearts = new CardModel(Suit.Hearts, Rank.Ace);
                aceHearts.IsFaceUp.Value = true;
                b.Foundations[(int)Suit.Hearts].AddCard(aceHearts);

                CardModel twoHearts = new CardModel(Suit.Hearts, Rank.Two);
                twoHearts.IsFaceUp.Value = true;
                b.Tableau[0].AddCard(twoHearts);
            });
            var sut = new AutoCompleteSystem(board, _boardStateSubscriber, _autoCompletePublisher);

            List<Move> result = sut.GenerateMoveSequence();

            sut.Dispose();
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0].Source, Is.EqualTo(PileId.Tableau(1)));
            Assert.That(result[0].Destination, Is.EqualTo(PileId.Foundation((int)Suit.Spades)));
            Assert.That(result[1].Source, Is.EqualTo(PileId.Tableau(0)));
            Assert.That(result[1].Destination, Is.EqualTo(PileId.Foundation((int)Suit.Hearts)));
        }

        [Test]
        public void GenerateMoveSequence_TwoAcesOnTableau_ReturnsTwoMovesTotal()
        {
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                CardModel aceHearts = new CardModel(Suit.Hearts, Rank.Ace);
                aceHearts.IsFaceUp.Value = true;
                b.Tableau[0].AddCard(aceHearts);

                CardModel aceSpades = new CardModel(Suit.Spades, Rank.Ace);
                aceSpades.IsFaceUp.Value = true;
                b.Tableau[1].AddCard(aceSpades);
            });
            var sut = new AutoCompleteSystem(board, _boardStateSubscriber, _autoCompletePublisher);

            List<Move> result = sut.GenerateMoveSequence();

            sut.Dispose();
            Assert.That(result.Count, Is.EqualTo(2));
        }

        [Test]
        public void GenerateMoveSequence_AceOnOneColumnAndTwoOnAnother_ReturnsTwoMoves()
        {
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                CardModel ace = new CardModel(Suit.Hearts, Rank.Ace);
                ace.IsFaceUp.Value = true;
                b.Tableau[0].AddCard(ace);

                CardModel two = new CardModel(Suit.Hearts, Rank.Two);
                two.IsFaceUp.Value = true;
                b.Tableau[1].AddCard(two);
            });
            var sut = new AutoCompleteSystem(board, _boardStateSubscriber, _autoCompletePublisher);

            List<Move> result = sut.GenerateMoveSequence();

            sut.Dispose();
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0].Source, Is.EqualTo(PileId.Tableau(0)));
            Assert.That(result[1].Source, Is.EqualTo(PileId.Tableau(1)));
        }

        // --- BoardStateChanged subscription tests ---

        [Test]
        public void OnBoardStateChanged_AutoCompletableBoardTriggered_PublishesIsAvailableTrue()
        {
            BoardModel board = TestBoardFactory.AutoCompletableBoard();
            var subscriber = new TestSubscriber<BoardStateChangedMessage>();
            var publisher = new TestPublisher<AutoCompleteAvailableMessage>();
            var sut = new AutoCompleteSystem(board, subscriber, publisher);

            subscriber.Trigger(new BoardStateChangedMessage());

            sut.Dispose();
            Assert.That(publisher.MessageCount, Is.EqualTo(1));
            Assert.That(publisher.LastMessage.IsAvailable, Is.True);
        }

        [Test]
        public void OnBoardStateChanged_NonAutoCompletableBoard_PublishesIsAvailableFalse()
        {
            BoardModel board = TestBoardFactory.StandardDealBoard();
            var subscriber = new TestSubscriber<BoardStateChangedMessage>();
            var publisher = new TestPublisher<AutoCompleteAvailableMessage>();
            var sut = new AutoCompleteSystem(board, subscriber, publisher);

            subscriber.Trigger(new BoardStateChangedMessage());

            sut.Dispose();
            Assert.That(publisher.MessageCount, Is.EqualTo(1));
            Assert.That(publisher.LastMessage.IsAvailable, Is.False);
        }

        // --- Dispose tests ---

        [Test]
        public void Dispose_AfterDispose_SubscriptionIsUnregistered()
        {
            var subscriber = new TestSubscriber<BoardStateChangedMessage>();
            var publisher = new TestPublisher<AutoCompleteAvailableMessage>();
            var sut = new AutoCompleteSystem(_board, subscriber, publisher);

            sut.Dispose();
            subscriber.Trigger(new BoardStateChangedMessage());

            Assert.That(publisher.MessageCount, Is.EqualTo(0));
        }

        [Test]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            var sut = new AutoCompleteSystem(_board, _boardStateSubscriber, _autoCompletePublisher);

            sut.Dispose();

            Assert.DoesNotThrow(() => sut.Dispose());
        }
    }
}
