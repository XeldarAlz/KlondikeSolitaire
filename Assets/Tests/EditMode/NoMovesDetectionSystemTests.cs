using System;
using KlondikeSolitaire.Core;
using KlondikeSolitaire.Systems;
using NUnit.Framework;

namespace KlondikeSolitaire.Tests
{
    [TestFixture]
    public sealed class NoMovesDetectionSystemTests
    {
        private BoardModel _board;
        private MoveValidationSystem _moveValidation;
        private GamePhaseModel _gamePhase;
        private TestSubscriber<BoardStateChangedMessage> _boardStateSubscriber;
        private TestPublisher<NoMovesDetectedMessage> _noMovesPublisher;
        private NoMovesDetectionSystem _sut;

        [SetUp]
        public void SetUp()
        {
            _board = TestBoardFactory.NoMovesBoard();
            _moveValidation = new MoveValidationSystem();
            _gamePhase = new GamePhaseModel();
            _boardStateSubscriber = new TestSubscriber<BoardStateChangedMessage>();
            _noMovesPublisher = new TestPublisher<NoMovesDetectedMessage>();
            _sut = new NoMovesDetectionSystem(
                _board,
                _moveValidation,
                _gamePhase,
                _boardStateSubscriber,
                _noMovesPublisher);
        }

        [TearDown]
        public void TearDown()
        {
            _sut.Dispose();
        }

        // --- Constructor null guard tests ---

        [Test]
        public void Constructor_NullBoard_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new NoMovesDetectionSystem(null, _moveValidation, _gamePhase, _boardStateSubscriber, _noMovesPublisher));
        }

        [Test]
        public void Constructor_NullMoveValidation_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new NoMovesDetectionSystem(_board, null, _gamePhase, _boardStateSubscriber, _noMovesPublisher));
        }

        [Test]
        public void Constructor_NullGamePhase_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new NoMovesDetectionSystem(_board, _moveValidation, null, _boardStateSubscriber, _noMovesPublisher));
        }

        [Test]
        public void Constructor_NullBoardStateSubscriber_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new NoMovesDetectionSystem(_board, _moveValidation, _gamePhase, null, _noMovesPublisher));
        }

        [Test]
        public void Constructor_NullNoMovesPublisher_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new NoMovesDetectionSystem(_board, _moveValidation, _gamePhase, _boardStateSubscriber, null));
        }

        // --- Playing phase: publishes when no valid moves ---

        [Test]
        public void OnBoardStateChanged_PlayingPhaseNoMoves_PublishesNoMovesDetectedMessage()
        {
            // Arrange
            _gamePhase.Phase.Value = GamePhase.Playing;

            // Act
            _boardStateSubscriber.Trigger(new BoardStateChangedMessage());

            // Assert
            Assert.That(_noMovesPublisher.MessageCount, Is.EqualTo(1));
        }

        // --- Playing phase: does NOT publish when valid moves exist ---

        [Test]
        public void OnBoardStateChanged_PlayingPhaseWithStockCards_DoesNotPublish()
        {
            // Arrange — stock has cards, so drawing is a valid move
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                CardModel stockCard = new CardModel(Suit.Hearts, Rank.Ace);
                b.Stock.AddCard(stockCard);
            });
            var sut = new NoMovesDetectionSystem(board, _moveValidation, _gamePhase, _boardStateSubscriber, _noMovesPublisher);
            _gamePhase.Phase.Value = GamePhase.Playing;

            // Act
            _boardStateSubscriber.Trigger(new BoardStateChangedMessage());

            // Assert
            Assert.That(_noMovesPublisher.MessageCount, Is.EqualTo(0));

            sut.Dispose();
        }

        [Test]
        public void OnBoardStateChanged_PlayingPhaseWithValidTableauMoves_DoesNotPublish()
        {
            // Arrange — black 7 on red 8 is a valid tableau move
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                CardModel blackSeven = new CardModel(Suit.Clubs, Rank.Seven);
                blackSeven.IsFaceUp.Value = true;
                b.Tableau[0].AddCard(blackSeven);

                CardModel redEight = new CardModel(Suit.Hearts, Rank.Eight);
                redEight.IsFaceUp.Value = true;
                b.Tableau[1].AddCard(redEight);
            });
            var sut = new NoMovesDetectionSystem(board, _moveValidation, _gamePhase, _boardStateSubscriber, _noMovesPublisher);
            _gamePhase.Phase.Value = GamePhase.Playing;

            // Act
            _boardStateSubscriber.Trigger(new BoardStateChangedMessage());

            // Assert
            Assert.That(_noMovesPublisher.MessageCount, Is.EqualTo(0));

            sut.Dispose();
        }

        // --- Non-Playing phases: all filtered out ---

        [Test]
        public void OnBoardStateChanged_DealingPhase_DoesNotPublish()
        {
            // Arrange — default phase is Dealing; _board has no valid moves
            Assert.That(_gamePhase.Phase.Value, Is.EqualTo(GamePhase.Dealing));

            // Act
            _boardStateSubscriber.Trigger(new BoardStateChangedMessage());

            // Assert
            Assert.That(_noMovesPublisher.MessageCount, Is.EqualTo(0));
        }

        [Test]
        public void OnBoardStateChanged_AutoCompletingPhase_DoesNotPublish()
        {
            // Arrange
            _gamePhase.Phase.Value = GamePhase.AutoCompleting;

            // Act
            _boardStateSubscriber.Trigger(new BoardStateChangedMessage());

            // Assert
            Assert.That(_noMovesPublisher.MessageCount, Is.EqualTo(0));
        }

        [Test]
        public void OnBoardStateChanged_WonPhase_DoesNotPublish()
        {
            // Arrange
            _gamePhase.Phase.Value = GamePhase.Won;

            // Act
            _boardStateSubscriber.Trigger(new BoardStateChangedMessage());

            // Assert
            Assert.That(_noMovesPublisher.MessageCount, Is.EqualTo(0));
        }

        [Test]
        public void OnBoardStateChanged_NoMovesPhase_DoesNotPublish()
        {
            // Arrange
            _gamePhase.Phase.Value = GamePhase.NoMoves;

            // Act
            _boardStateSubscriber.Trigger(new BoardStateChangedMessage());

            // Assert
            Assert.That(_noMovesPublisher.MessageCount, Is.EqualTo(0));
        }

        // --- Dispose: unsubscribes from BoardStateChangedMessage ---

        [Test]
        public void Dispose_UnsubscribesFromBoardStateChanged()
        {
            // Arrange
            _gamePhase.Phase.Value = GamePhase.Playing;
            _sut.Dispose();

            // Act — trigger after dispose; handler should be gone
            _boardStateSubscriber.Trigger(new BoardStateChangedMessage());

            // Assert
            Assert.That(_noMovesPublisher.MessageCount, Is.EqualTo(0));
        }
    }
}
