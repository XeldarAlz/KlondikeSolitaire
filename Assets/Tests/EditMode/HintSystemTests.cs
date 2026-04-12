using KlondikeSolitaire.Core;
using KlondikeSolitaire.Systems;
using NUnit.Framework;

namespace KlondikeSolitaire.Tests
{
    [TestFixture]
    public sealed class HintSystemTests
    {
        private BoardModel _board;
        private MoveEnumerator _moveEnumerator;
        private TestSubscriber<BoardStateChangedMessage> _boardStateSubscriber;
        private TestPublisher<HintHighlightMessage> _hintHighlightPublisher;
        private TestPublisher<HintClearedMessage> _hintClearedPublisher;
        private HintSystem _sut;

        [SetUp]
        public void SetUp()
        {
            _board = TestBoardFactory.EmptyBoard();
            _moveEnumerator = new MoveEnumerator(new MoveValidationSystem());
            _boardStateSubscriber = new TestSubscriber<BoardStateChangedMessage>();
            _hintHighlightPublisher = new TestPublisher<HintHighlightMessage>();
            _hintClearedPublisher = new TestPublisher<HintClearedMessage>();
            _sut = new HintSystem(
                _board,
                _moveEnumerator,
                _boardStateSubscriber,
                _hintHighlightPublisher,
                _hintClearedPublisher);
        }

        [TearDown]
        public void TearDown()
        {
            _sut.Dispose();
        }

        // --- GetNextHint: no valid moves is a no-op ---

        [Test]
        public void GetNextHint_EmptyBoard_DoesNotPublishHintHighlightMessage()
        {
            _sut.GetNextHint();

            Assert.That(_hintHighlightPublisher.MessageCount, Is.EqualTo(0));
        }

        [Test]
        public void GetNextHint_NoMovesBoard_DoesNotPublishHintHighlightMessage()
        {
            BoardModel board = TestBoardFactory.NoMovesBoard();
            var sut = new HintSystem(board, _moveEnumerator, _boardStateSubscriber, _hintHighlightPublisher, _hintClearedPublisher);

            sut.GetNextHint();

            sut.Dispose();
            Assert.That(_hintHighlightPublisher.MessageCount, Is.EqualTo(0));
        }

        // --- GetNextHint: publishes HintHighlightMessage when valid move exists ---

        [Test]
        public void GetNextHint_WithValidMove_PublishesHintHighlightMessage()
        {
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                CardModel ace = new CardModel(Suit.Hearts, Rank.Ace);
                ace.IsFaceUp.Value = true;
                b.Tableau[0].AddCard(ace);
            });
            var sut = new HintSystem(board, _moveEnumerator, _boardStateSubscriber, _hintHighlightPublisher, _hintClearedPublisher);

            sut.GetNextHint();

            sut.Dispose();
            Assert.That(_hintHighlightPublisher.MessageCount, Is.EqualTo(1));
        }

        [Test]
        public void GetNextHint_WithValidMove_SourcePileIdMatchesCardLocation()
        {
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                CardModel ace = new CardModel(Suit.Hearts, Rank.Ace);
                ace.IsFaceUp.Value = true;
                b.Tableau[0].AddCard(ace);
            });
            var sut = new HintSystem(board, _moveEnumerator, _boardStateSubscriber, _hintHighlightPublisher, _hintClearedPublisher);

            sut.GetNextHint();

            sut.Dispose();
            Assert.That(_hintHighlightPublisher.LastMessage.SourcePileId, Is.EqualTo(PileId.Tableau(0)));
        }

        [Test]
        public void GetNextHint_WithValidMove_SourceCardIndexIsCorrect()
        {
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                CardModel faceDown = new CardModel(Suit.Spades, Rank.Two);
                CardModel ace = new CardModel(Suit.Hearts, Rank.Ace);
                ace.IsFaceUp.Value = true;
                b.Tableau[0].AddCard(faceDown);
                b.Tableau[0].AddCard(ace);
            });
            var sut = new HintSystem(board, _moveEnumerator, _boardStateSubscriber, _hintHighlightPublisher, _hintClearedPublisher);

            sut.GetNextHint();

            sut.Dispose();
            // Pile has 2 cards, cardCount for an ace move is 1, so sourceCardIndex = 2 - 1 = 1
            Assert.That(_hintHighlightPublisher.LastMessage.SourceCardIndex, Is.EqualTo(1));
        }

        [Test]
        public void GetNextHint_WithValidMove_DestPileIdIsSet()
        {
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                CardModel ace = new CardModel(Suit.Hearts, Rank.Ace);
                ace.IsFaceUp.Value = true;
                b.Tableau[0].AddCard(ace);
            });
            var sut = new HintSystem(board, _moveEnumerator, _boardStateSubscriber, _hintHighlightPublisher, _hintClearedPublisher);

            sut.GetNextHint();

            sut.Dispose();
            Assert.That(_hintHighlightPublisher.LastMessage.DestPileId.Type, Is.EqualTo(PileType.Foundation));
        }

        // --- GetNextHint: cycling through hints ---

        [Test]
        public void GetNextHint_CalledTwice_PublishesTwoMessages()
        {
            BoardModel board = BuildBoardWithTwoValidMoves();
            var sut = new HintSystem(board, _moveEnumerator, _boardStateSubscriber, _hintHighlightPublisher, _hintClearedPublisher);

            sut.GetNextHint();
            sut.GetNextHint();

            sut.Dispose();
            Assert.That(_hintHighlightPublisher.MessageCount, Is.EqualTo(2));
        }

        [Test]
        public void GetNextHint_CalledTwice_SecondMessageDiffersFromFirst()
        {
            BoardModel board = BuildBoardWithTwoValidMoves();
            var sut = new HintSystem(board, _moveEnumerator, _boardStateSubscriber, _hintHighlightPublisher, _hintClearedPublisher);

            sut.GetNextHint();
            HintHighlightMessage firstMessage = _hintHighlightPublisher.LastMessage;
            sut.GetNextHint();
            HintHighlightMessage secondMessage = _hintHighlightPublisher.LastMessage;

            sut.Dispose();
            bool sourceDiffers = firstMessage.SourcePileId != secondMessage.SourcePileId;
            bool destDiffers = firstMessage.DestPileId != secondMessage.DestPileId;
            Assert.That(sourceDiffers || destDiffers, Is.True,
                "Second hint should differ from the first when multiple valid moves exist");
        }

        [Test]
        public void GetNextHint_CycledPastEnd_WrapsToFirstHintSource()
        {
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                CardModel ace = new CardModel(Suit.Hearts, Rank.Ace);
                ace.IsFaceUp.Value = true;
                b.Tableau[0].AddCard(ace);
            });
            var sut = new HintSystem(board, _moveEnumerator, _boardStateSubscriber, _hintHighlightPublisher, _hintClearedPublisher);

            // With only 1 valid move: calling GetNextHint twice wraps back to the same hint
            sut.GetNextHint();
            HintHighlightMessage firstMessage = _hintHighlightPublisher.LastMessage;
            sut.GetNextHint();
            HintHighlightMessage wrappedMessage = _hintHighlightPublisher.LastMessage;

            sut.Dispose();
            Assert.That(wrappedMessage.SourcePileId, Is.EqualTo(firstMessage.SourcePileId));
        }

        [Test]
        public void GetNextHint_CycledPastEnd_WrapsToFirstHintDest()
        {
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                CardModel ace = new CardModel(Suit.Hearts, Rank.Ace);
                ace.IsFaceUp.Value = true;
                b.Tableau[0].AddCard(ace);
            });
            var sut = new HintSystem(board, _moveEnumerator, _boardStateSubscriber, _hintHighlightPublisher, _hintClearedPublisher);

            sut.GetNextHint();
            HintHighlightMessage firstMessage = _hintHighlightPublisher.LastMessage;
            sut.GetNextHint();
            HintHighlightMessage wrappedMessage = _hintHighlightPublisher.LastMessage;

            sut.Dispose();
            Assert.That(wrappedMessage.DestPileId, Is.EqualTo(firstMessage.DestPileId));
        }

        // --- Cache: populated on first call, not on construction ---

        [Test]
        public void GetNextHint_FirstCall_PopulatesCacheAndPublishesMessage()
        {
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                CardModel ace = new CardModel(Suit.Hearts, Rank.Ace);
                ace.IsFaceUp.Value = true;
                b.Tableau[0].AddCard(ace);
            });
            var sut = new HintSystem(board, _moveEnumerator, _boardStateSubscriber, _hintHighlightPublisher, _hintClearedPublisher);

            // No message before first call
            Assert.That(_hintHighlightPublisher.MessageCount, Is.EqualTo(0));

            sut.GetNextHint();

            sut.Dispose();
            Assert.That(_hintHighlightPublisher.MessageCount, Is.EqualTo(1));
        }

        [Test]
        public void GetNextHint_CalledThreeTimes_WithTwoMoves_ThirdCallPublishesFirstMoveAgain()
        {
            BoardModel board = BuildBoardWithTwoValidMoves();
            var sut = new HintSystem(board, _moveEnumerator, _boardStateSubscriber, _hintHighlightPublisher, _hintClearedPublisher);

            sut.GetNextHint();
            HintHighlightMessage first = _hintHighlightPublisher.LastMessage;

            sut.GetNextHint();

            sut.GetNextHint();
            HintHighlightMessage third = _hintHighlightPublisher.LastMessage;

            sut.Dispose();
            Assert.That(third.SourcePileId, Is.EqualTo(first.SourcePileId));
            Assert.That(third.DestPileId, Is.EqualTo(first.DestPileId));
        }

        // --- BoardStateChanged: clears cache and publishes HintClearedMessage ---

        [Test]
        public void OnBoardStateChanged_PublishesHintClearedMessage()
        {
            _boardStateSubscriber.Trigger(new BoardStateChangedMessage());

            Assert.That(_hintClearedPublisher.MessageCount, Is.EqualTo(1));
        }

        [Test]
        public void OnBoardStateChanged_TriggeredTwice_PublishesTwoHintClearedMessages()
        {
            _boardStateSubscriber.Trigger(new BoardStateChangedMessage());
            _boardStateSubscriber.Trigger(new BoardStateChangedMessage());

            Assert.That(_hintClearedPublisher.MessageCount, Is.EqualTo(2));
        }

        [Test]
        public void OnBoardStateChanged_ClearsCache_SubsequentGetNextHintRecalculatesMoves()
        {
            // Start: board has an ace on tableau[0] — one valid move
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                CardModel ace = new CardModel(Suit.Hearts, Rank.Ace);
                ace.IsFaceUp.Value = true;
                b.Tableau[0].AddCard(ace);
            });
            var subscriber = new TestSubscriber<BoardStateChangedMessage>();
            var hintPublisher = new TestPublisher<HintHighlightMessage>();
            var clearedPublisher = new TestPublisher<HintClearedMessage>();
            var sut = new HintSystem(board, _moveEnumerator, subscriber, hintPublisher, clearedPublisher);

            // Get the first hint (caches moves)
            sut.GetNextHint();
            Assert.That(hintPublisher.MessageCount, Is.EqualTo(1));

            // Trigger board state change → clears cache
            subscriber.Trigger(new BoardStateChangedMessage());

            // Add a second ace on tableau[1] so we now have more valid moves
            CardModel aceSpades = new CardModel(Suit.Spades, Rank.Ace);
            aceSpades.IsFaceUp.Value = true;
            board.Tableau[1].AddCard(aceSpades);

            hintPublisher.Clear();

            // Next hint should re-enumerate from the new board state
            sut.GetNextHint();

            sut.Dispose();
            Assert.That(hintPublisher.MessageCount, Is.EqualTo(1));
        }

        [Test]
        public void OnBoardStateChanged_AfterCacheCleared_DoesNotPublishHintHighlightMessage()
        {
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                CardModel ace = new CardModel(Suit.Hearts, Rank.Ace);
                ace.IsFaceUp.Value = true;
                b.Tableau[0].AddCard(ace);
            });
            var subscriber = new TestSubscriber<BoardStateChangedMessage>();
            var hintPublisher = new TestPublisher<HintHighlightMessage>();
            var clearedPublisher = new TestPublisher<HintClearedMessage>();
            var sut = new HintSystem(board, _moveEnumerator, subscriber, hintPublisher, clearedPublisher);

            // Triggering board state change should NOT publish a hint highlight
            subscriber.Trigger(new BoardStateChangedMessage());

            sut.Dispose();
            Assert.That(hintPublisher.MessageCount, Is.EqualTo(0));
        }

        // --- Reset ---

        [Test]
        public void Reset_PublishesHintClearedMessage()
        {
            _sut.Reset();

            Assert.That(_hintClearedPublisher.MessageCount, Is.EqualTo(1));
        }

        [Test]
        public void Reset_CalledTwice_PublishesTwoHintClearedMessages()
        {
            _sut.Reset();
            _sut.Reset();

            Assert.That(_hintClearedPublisher.MessageCount, Is.EqualTo(2));
        }

        [Test]
        public void Reset_AfterGetNextHint_ClearsCache()
        {
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                CardModel ace = new CardModel(Suit.Hearts, Rank.Ace);
                ace.IsFaceUp.Value = true;
                b.Tableau[0].AddCard(ace);
            });
            var sut = new HintSystem(board, _moveEnumerator, _boardStateSubscriber, _hintHighlightPublisher, _hintClearedPublisher);

            sut.GetNextHint();
            HintHighlightMessage firstHint = _hintHighlightPublisher.LastMessage;
            sut.Reset();

            // Add a second ace so that after reset, re-enumeration sees the updated board
            CardModel aceSpades = new CardModel(Suit.Spades, Rank.Ace);
            aceSpades.IsFaceUp.Value = true;
            board.Tableau[1].AddCard(aceSpades);

            _hintHighlightPublisher.Clear();
            sut.GetNextHint();

            sut.Dispose();
            // After reset, the hint index resets to -1, so first GetNextHint cycles to index 0
            Assert.That(_hintHighlightPublisher.MessageCount, Is.EqualTo(1));
        }

        [Test]
        public void Reset_DoesNotPublishHintHighlightMessage()
        {
            _sut.Reset();

            Assert.That(_hintHighlightPublisher.MessageCount, Is.EqualTo(0));
        }

        // --- Dispose ---

        [Test]
        public void Dispose_AfterDispose_BoardStateChangedNoLongerPublishesHintCleared()
        {
            var subscriber = new TestSubscriber<BoardStateChangedMessage>();
            var clearedPublisher = new TestPublisher<HintClearedMessage>();
            var sut = new HintSystem(_board, _moveEnumerator, subscriber, _hintHighlightPublisher, clearedPublisher);

            sut.Dispose();
            subscriber.Trigger(new BoardStateChangedMessage());

            Assert.That(clearedPublisher.MessageCount, Is.EqualTo(0));
        }

        [Test]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            var sut = new HintSystem(_board, _moveEnumerator, _boardStateSubscriber, _hintHighlightPublisher, _hintClearedPublisher);
            sut.Dispose();

            Assert.DoesNotThrow(() => sut.Dispose());
        }

        // --- Private helpers ---

        private static BoardModel BuildBoardWithTwoValidMoves()
        {
            return TestBoardFactory.CustomBoard(b =>
            {
                CardModel aceHearts = new CardModel(Suit.Hearts, Rank.Ace);
                aceHearts.IsFaceUp.Value = true;
                b.Tableau[0].AddCard(aceHearts);

                CardModel aceSpades = new CardModel(Suit.Spades, Rank.Ace);
                aceSpades.IsFaceUp.Value = true;
                b.Tableau[1].AddCard(aceSpades);
            });
        }
    }
}
