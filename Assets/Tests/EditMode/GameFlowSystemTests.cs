using System;
using KlondikeSolitaire.Core;
using KlondikeSolitaire.Systems;
using NUnit.Framework;

namespace KlondikeSolitaire.Tests
{
    [TestFixture]
    public sealed class GameFlowSystemTests
    {
        private GamePhaseModel _gamePhase;
        private BoardModel _board;
        private ScoreModel _scoreModel;
        private DealSystem _dealSystem;
        private UndoSystem _undoSystem;
        private HintSystem _hintSystem;
        private TestSubscriber<BoardStateChangedMessage> _boardStateSubscriber;
        private TestSubscriber<NoMovesDetectedMessage> _noMovesSubscriber;
        private TestSubscriber<NewGameRequestedMessage> _newGameSubscriber;
        private TestPublisher<GamePhaseChangedMessage> _phaseChangedPublisher;
        private TestPublisher<WinDetectedMessage> _winDetectedPublisher;
        private TestPublisher<DealCompletedMessage> _dealCompletedPublisher;
        private GameFlowSystem _sut;

        [SetUp]
        public void SetUp()
        {
            _gamePhase = new GamePhaseModel();
            _board = TestBoardFactory.EmptyBoard();
            _scoreModel = new ScoreModel();

            _dealCompletedPublisher = new TestPublisher<DealCompletedMessage>();
            _dealSystem = new DealSystem(_board, _dealCompletedPublisher);

            var scoreChangedPublisher = new TestPublisher<ScoreChangedMessage>();
            var scoringSystem = new ScoringSystem(_scoreModel, new ScoringTable(5, 10, 10, -15, 5), scoreChangedPublisher);
            var undoAvailabilityPublisher = new TestPublisher<UndoAvailabilityChangedMessage>();
            var boardStateForUndo = new TestPublisher<BoardStateChangedMessage>();
            var cardFlippedPublisher = new TestPublisher<CardFlippedMessage>();
            _undoSystem = new UndoSystem(
                _board,
                scoringSystem,
                undoAvailabilityPublisher,
                boardStateForUndo,
                cardFlippedPublisher);

            var moveValidation = new MoveValidationSystem();
            var boardStateForHint = new TestSubscriber<BoardStateChangedMessage>();
            var hintHighlightPublisher = new TestPublisher<HintHighlightMessage>();
            var hintClearedPublisher = new TestPublisher<HintClearedMessage>();
            _hintSystem = new HintSystem(
                _board,
                moveValidation,
                boardStateForHint,
                hintHighlightPublisher,
                hintClearedPublisher);

            _boardStateSubscriber = new TestSubscriber<BoardStateChangedMessage>();
            _noMovesSubscriber = new TestSubscriber<NoMovesDetectedMessage>();
            _newGameSubscriber = new TestSubscriber<NewGameRequestedMessage>();
            _phaseChangedPublisher = new TestPublisher<GamePhaseChangedMessage>();
            _winDetectedPublisher = new TestPublisher<WinDetectedMessage>();

            _sut = CreateSystem();
        }

        [TearDown]
        public void TearDown()
        {
            _sut.Dispose();
            _hintSystem.Dispose();
            _undoSystem.Dispose();
        }

        private GameFlowSystem CreateSystem()
        {
            return new GameFlowSystem(
                _gamePhase,
                _dealSystem,
                _board,
                _scoreModel,
                _undoSystem,
                _hintSystem,
                _boardStateSubscriber,
                _noMovesSubscriber,
                _newGameSubscriber,
                _phaseChangedPublisher,
                _winDetectedPublisher,
                _dealCompletedPublisher);
        }

        // --- Constructor null guard tests ---

        [Test]
        public void Constructor_NullGamePhase_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new GameFlowSystem(null, _dealSystem, _board, _scoreModel, _undoSystem, _hintSystem,
                    _boardStateSubscriber, _noMovesSubscriber, _newGameSubscriber,
                    _phaseChangedPublisher, _winDetectedPublisher, _dealCompletedPublisher));
        }

        [Test]
        public void Constructor_NullDealSystem_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new GameFlowSystem(_gamePhase, null, _board, _scoreModel, _undoSystem, _hintSystem,
                    _boardStateSubscriber, _noMovesSubscriber, _newGameSubscriber,
                    _phaseChangedPublisher, _winDetectedPublisher, _dealCompletedPublisher));
        }

        [Test]
        public void Constructor_NullBoard_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new GameFlowSystem(_gamePhase, _dealSystem, null, _scoreModel, _undoSystem, _hintSystem,
                    _boardStateSubscriber, _noMovesSubscriber, _newGameSubscriber,
                    _phaseChangedPublisher, _winDetectedPublisher, _dealCompletedPublisher));
        }

        [Test]
        public void Constructor_NullScoreModel_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new GameFlowSystem(_gamePhase, _dealSystem, _board, null, _undoSystem, _hintSystem,
                    _boardStateSubscriber, _noMovesSubscriber, _newGameSubscriber,
                    _phaseChangedPublisher, _winDetectedPublisher, _dealCompletedPublisher));
        }

        [Test]
        public void Constructor_NullUndoSystem_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new GameFlowSystem(_gamePhase, _dealSystem, _board, _scoreModel, null, _hintSystem,
                    _boardStateSubscriber, _noMovesSubscriber, _newGameSubscriber,
                    _phaseChangedPublisher, _winDetectedPublisher, _dealCompletedPublisher));
        }

        [Test]
        public void Constructor_NullHintSystem_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new GameFlowSystem(_gamePhase, _dealSystem, _board, _scoreModel, _undoSystem, null,
                    _boardStateSubscriber, _noMovesSubscriber, _newGameSubscriber,
                    _phaseChangedPublisher, _winDetectedPublisher, _dealCompletedPublisher));
        }

        [Test]
        public void Constructor_NullBoardStateSubscriber_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new GameFlowSystem(_gamePhase, _dealSystem, _board, _scoreModel, _undoSystem, _hintSystem,
                    null, _noMovesSubscriber, _newGameSubscriber,
                    _phaseChangedPublisher, _winDetectedPublisher, _dealCompletedPublisher));
        }

        [Test]
        public void Constructor_NullNoMovesSubscriber_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new GameFlowSystem(_gamePhase, _dealSystem, _board, _scoreModel, _undoSystem, _hintSystem,
                    _boardStateSubscriber, null, _newGameSubscriber,
                    _phaseChangedPublisher, _winDetectedPublisher, _dealCompletedPublisher));
        }

        [Test]
        public void Constructor_NullNewGameSubscriber_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new GameFlowSystem(_gamePhase, _dealSystem, _board, _scoreModel, _undoSystem, _hintSystem,
                    _boardStateSubscriber, _noMovesSubscriber, null,
                    _phaseChangedPublisher, _winDetectedPublisher, _dealCompletedPublisher));
        }

        [Test]
        public void Constructor_NullPhaseChangedPublisher_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new GameFlowSystem(_gamePhase, _dealSystem, _board, _scoreModel, _undoSystem, _hintSystem,
                    _boardStateSubscriber, _noMovesSubscriber, _newGameSubscriber,
                    null, _winDetectedPublisher, _dealCompletedPublisher));
        }

        [Test]
        public void Constructor_NullWinDetectedPublisher_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new GameFlowSystem(_gamePhase, _dealSystem, _board, _scoreModel, _undoSystem, _hintSystem,
                    _boardStateSubscriber, _noMovesSubscriber, _newGameSubscriber,
                    _phaseChangedPublisher, null, _dealCompletedPublisher));
        }

        [Test]
        public void Constructor_NullDealCompletedPublisher_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new GameFlowSystem(_gamePhase, _dealSystem, _board, _scoreModel, _undoSystem, _hintSystem,
                    _boardStateSubscriber, _noMovesSubscriber, _newGameSubscriber,
                    _phaseChangedPublisher, _winDetectedPublisher, null));
        }

        // --- StartNewGame: phase transitions ---

        [Test]
        public void StartNewGame_SetsPhaseToPlaying()
        {
            _sut.StartNewGame();

            Assert.That(_gamePhase.Phase.Value, Is.EqualTo(GamePhase.Playing));
        }

        [Test]
        public void StartNewGame_PublishesDealingPhaseChangedMessage()
        {
            _sut.StartNewGame();

            bool hasDealingMessage = false;
            for (int messageIndex = 0; messageIndex < _phaseChangedPublisher.MessageCount; messageIndex++)
            {
                if (_phaseChangedPublisher.Messages[messageIndex].NewPhase == GamePhase.Dealing)
                {
                    hasDealingMessage = true;
                    break;
                }
            }

            Assert.That(hasDealingMessage, Is.True);
        }

        [Test]
        public void StartNewGame_PublishesPlayingPhaseChangedMessage()
        {
            _sut.StartNewGame();

            bool hasPlayingMessage = false;
            for (int messageIndex = 0; messageIndex < _phaseChangedPublisher.MessageCount; messageIndex++)
            {
                if (_phaseChangedPublisher.Messages[messageIndex].NewPhase == GamePhase.Playing)
                {
                    hasPlayingMessage = true;
                    break;
                }
            }

            Assert.That(hasPlayingMessage, Is.True);
        }

        [Test]
        public void StartNewGame_DealingPublishedBeforePlaying()
        {
            _sut.StartNewGame();

            int dealingIndex = -1;
            int playingIndex = -1;
            for (int messageIndex = 0; messageIndex < _phaseChangedPublisher.MessageCount; messageIndex++)
            {
                GamePhase phase = _phaseChangedPublisher.Messages[messageIndex].NewPhase;
                if (phase == GamePhase.Dealing && dealingIndex == -1)
                {
                    dealingIndex = messageIndex;
                }
                else if (phase == GamePhase.Playing && playingIndex == -1)
                {
                    playingIndex = messageIndex;
                }
            }

            Assert.That(dealingIndex, Is.LessThan(playingIndex));
        }

        [Test]
        public void StartNewGame_PublishesDealCompletedMessage()
        {
            _sut.StartNewGame();

            Assert.That(_dealCompletedPublisher.MessageCount, Is.GreaterThan(0));
        }

        // --- StartNewGame: score reset ---

        [Test]
        public void StartNewGame_ResetsScoreToZero()
        {
            _scoreModel.Score.Value = 250;

            _sut.StartNewGame();

            Assert.That(_scoreModel.Score.Value, Is.EqualTo(0));
        }

        // --- StartNewGame: undo clear ---

        [Test]
        public void StartNewGame_ClearsUndoStack()
        {
            PileModel source = _board.Tableau[0];
            PileModel dest = _board.Tableau[1];
            CardModel card = new CardModel(Suit.Hearts, Rank.King);
            card.IsFaceUp.Value = true;
            source.AddCard(card);

            var command = new MoveCommand(
                MoveType.TableauToTableau,
                source.Id,
                dest.Id,
                1,
                0,
                false,
                0);
            _undoSystem.Push(command);

            Assert.That(_undoSystem.CanUndo, Is.True);

            _sut.StartNewGame();

            Assert.That(_undoSystem.CanUndo, Is.False);
        }

        // --- Win detection: all foundations at 13 ---

        [Test]
        public void OnBoardStateChanged_AllFoundationsComplete_SetsPhaseToWon()
        {
            FillAllFoundations(_board);
            _gamePhase.Phase.Value = GamePhase.Playing;

            _boardStateSubscriber.Trigger(new BoardStateChangedMessage());

            Assert.That(_gamePhase.Phase.Value, Is.EqualTo(GamePhase.Won));
        }

        [Test]
        public void OnBoardStateChanged_AllFoundationsComplete_PublishesWinDetectedMessage()
        {
            FillAllFoundations(_board);
            _gamePhase.Phase.Value = GamePhase.Playing;

            _boardStateSubscriber.Trigger(new BoardStateChangedMessage());

            Assert.That(_winDetectedPublisher.MessageCount, Is.EqualTo(1));
        }

        [Test]
        public void OnBoardStateChanged_AllFoundationsComplete_WinDetectedMessageContainsFinalScore()
        {
            FillAllFoundations(_board);
            _gamePhase.Phase.Value = GamePhase.Playing;
            _scoreModel.Score.Value = 150;

            _boardStateSubscriber.Trigger(new BoardStateChangedMessage());

            Assert.That(_winDetectedPublisher.LastMessage.FinalScore, Is.EqualTo(150));
        }

        [Test]
        public void OnBoardStateChanged_AllFoundationsComplete_PublishesWonPhaseChangedMessage()
        {
            FillAllFoundations(_board);
            _gamePhase.Phase.Value = GamePhase.Playing;

            _boardStateSubscriber.Trigger(new BoardStateChangedMessage());

            Assert.That(_phaseChangedPublisher.LastMessage.NewPhase, Is.EqualTo(GamePhase.Won));
        }

        [Test]
        public void OnBoardStateChanged_IncompleteFoundations_DoesNotSetPhaseToWon()
        {
            FillFoundationsExceptLast(_board);
            _gamePhase.Phase.Value = GamePhase.Playing;

            _boardStateSubscriber.Trigger(new BoardStateChangedMessage());

            Assert.That(_gamePhase.Phase.Value, Is.EqualTo(GamePhase.Playing));
        }

        [Test]
        public void OnBoardStateChanged_IncompleteFoundations_DoesNotPublishWinDetectedMessage()
        {
            FillFoundationsExceptLast(_board);
            _gamePhase.Phase.Value = GamePhase.Playing;

            _boardStateSubscriber.Trigger(new BoardStateChangedMessage());

            Assert.That(_winDetectedPublisher.MessageCount, Is.EqualTo(0));
        }

        // --- Win detection: AutoCompleting phase also triggers win ---

        [Test]
        public void OnBoardStateChanged_AllFoundationsComplete_DuringAutoCompleting_SetsPhaseToWon()
        {
            FillAllFoundations(_board);
            _gamePhase.Phase.Value = GamePhase.AutoCompleting;

            _boardStateSubscriber.Trigger(new BoardStateChangedMessage());

            Assert.That(_gamePhase.Phase.Value, Is.EqualTo(GamePhase.Won));
        }

        [Test]
        public void OnBoardStateChanged_AllFoundationsComplete_DuringAutoCompleting_PublishesWinDetectedMessage()
        {
            FillAllFoundations(_board);
            _gamePhase.Phase.Value = GamePhase.AutoCompleting;

            _boardStateSubscriber.Trigger(new BoardStateChangedMessage());

            Assert.That(_winDetectedPublisher.MessageCount, Is.EqualTo(1));
        }

        // --- Win detection: filtered to Playing and AutoCompleting only ---

        [Test]
        public void OnBoardStateChanged_AllFoundationsComplete_DuringDealing_DoesNotTriggerWin()
        {
            FillAllFoundations(_board);
            _gamePhase.Phase.Value = GamePhase.Dealing;

            _boardStateSubscriber.Trigger(new BoardStateChangedMessage());

            Assert.That(_winDetectedPublisher.MessageCount, Is.EqualTo(0));
        }

        [Test]
        public void OnBoardStateChanged_AllFoundationsComplete_DuringDealing_DoesNotChangePhase()
        {
            FillAllFoundations(_board);
            _gamePhase.Phase.Value = GamePhase.Dealing;

            _boardStateSubscriber.Trigger(new BoardStateChangedMessage());

            Assert.That(_gamePhase.Phase.Value, Is.EqualTo(GamePhase.Dealing));
        }

        [Test]
        public void OnBoardStateChanged_AllFoundationsComplete_DuringNoMoves_DoesNotTriggerWin()
        {
            FillAllFoundations(_board);
            _gamePhase.Phase.Value = GamePhase.NoMoves;

            _boardStateSubscriber.Trigger(new BoardStateChangedMessage());

            Assert.That(_winDetectedPublisher.MessageCount, Is.EqualTo(0));
        }

        [Test]
        public void OnBoardStateChanged_AllFoundationsComplete_DuringWon_DoesNotTriggerWinAgain()
        {
            FillAllFoundations(_board);
            _gamePhase.Phase.Value = GamePhase.Won;

            _boardStateSubscriber.Trigger(new BoardStateChangedMessage());

            Assert.That(_winDetectedPublisher.MessageCount, Is.EqualTo(0));
        }

        // --- Won is terminal ---

        [Test]
        public void OnBoardStateChanged_AfterWon_SubsequentBoardChangeDoesNotPublishWinAgain()
        {
            FillAllFoundations(_board);
            _gamePhase.Phase.Value = GamePhase.Playing;
            _boardStateSubscriber.Trigger(new BoardStateChangedMessage());

            Assert.That(_gamePhase.Phase.Value, Is.EqualTo(GamePhase.Won));

            _winDetectedPublisher.Clear();
            _boardStateSubscriber.Trigger(new BoardStateChangedMessage());

            Assert.That(_winDetectedPublisher.MessageCount, Is.EqualTo(0));
        }

        [Test]
        public void OnBoardStateChanged_AfterWon_PhaseRemainsWon()
        {
            FillAllFoundations(_board);
            _gamePhase.Phase.Value = GamePhase.Playing;
            _boardStateSubscriber.Trigger(new BoardStateChangedMessage());
            _boardStateSubscriber.Trigger(new BoardStateChangedMessage());

            Assert.That(_gamePhase.Phase.Value, Is.EqualTo(GamePhase.Won));
        }

        // --- NoMovesDetectedMessage → NoMoves phase ---

        [Test]
        public void OnNoMovesDetected_SetsPhaseToNoMoves()
        {
            _gamePhase.Phase.Value = GamePhase.Playing;

            _noMovesSubscriber.Trigger(new NoMovesDetectedMessage());

            Assert.That(_gamePhase.Phase.Value, Is.EqualTo(GamePhase.NoMoves));
        }

        [Test]
        public void OnNoMovesDetected_PublishesNoMovesPhaseChangedMessage()
        {
            _gamePhase.Phase.Value = GamePhase.Playing;

            _noMovesSubscriber.Trigger(new NoMovesDetectedMessage());

            Assert.That(_phaseChangedPublisher.LastMessage.NewPhase, Is.EqualTo(GamePhase.NoMoves));
        }

        // --- NoMoves is terminal ---

        [Test]
        public void OnNoMovesDetected_WhenAlreadyInNoMoves_PhaseRemainsNoMoves()
        {
            _gamePhase.Phase.Value = GamePhase.NoMoves;

            _noMovesSubscriber.Trigger(new NoMovesDetectedMessage());

            Assert.That(_gamePhase.Phase.Value, Is.EqualTo(GamePhase.NoMoves));
        }

        [Test]
        public void OnBoardStateChanged_AllFoundationsComplete_WhenInNoMoves_DoesNotTriggerWin()
        {
            FillAllFoundations(_board);
            _gamePhase.Phase.Value = GamePhase.NoMoves;

            _boardStateSubscriber.Trigger(new BoardStateChangedMessage());

            Assert.That(_winDetectedPublisher.MessageCount, Is.EqualTo(0));
        }

        // --- NewGameRequestedMessage triggers StartNewGame ---

        [Test]
        public void OnNewGameRequested_SetsPhaseToPlaying()
        {
            _gamePhase.Phase.Value = GamePhase.Won;

            _newGameSubscriber.Trigger(new NewGameRequestedMessage());

            Assert.That(_gamePhase.Phase.Value, Is.EqualTo(GamePhase.Playing));
        }

        [Test]
        public void OnNewGameRequested_ResetsScoreToZero()
        {
            _scoreModel.Score.Value = 500;

            _newGameSubscriber.Trigger(new NewGameRequestedMessage());

            Assert.That(_scoreModel.Score.Value, Is.EqualTo(0));
        }

        [Test]
        public void OnNewGameRequested_PublishesPlayingPhaseChangedMessage()
        {
            _phaseChangedPublisher.Clear();

            _newGameSubscriber.Trigger(new NewGameRequestedMessage());

            bool hasPlayingMessage = false;
            for (int messageIndex = 0; messageIndex < _phaseChangedPublisher.MessageCount; messageIndex++)
            {
                if (_phaseChangedPublisher.Messages[messageIndex].NewPhase == GamePhase.Playing)
                {
                    hasPlayingMessage = true;
                    break;
                }
            }

            Assert.That(hasPlayingMessage, Is.True);
        }

        [Test]
        public void OnNewGameRequested_PublishesDealCompletedMessage()
        {
            _dealCompletedPublisher.Clear();

            _newGameSubscriber.Trigger(new NewGameRequestedMessage());

            Assert.That(_dealCompletedPublisher.MessageCount, Is.GreaterThan(0));
        }

        // --- StartAutoComplete ---

        [Test]
        public void StartAutoComplete_SetsPhaseToAutoCompleting()
        {
            _gamePhase.Phase.Value = GamePhase.Playing;

            _sut.StartAutoComplete();

            Assert.That(_gamePhase.Phase.Value, Is.EqualTo(GamePhase.AutoCompleting));
        }

        [Test]
        public void StartAutoComplete_PublishesAutoCompletingPhaseChangedMessage()
        {
            _gamePhase.Phase.Value = GamePhase.Playing;

            _sut.StartAutoComplete();

            Assert.That(_phaseChangedPublisher.LastMessage.NewPhase, Is.EqualTo(GamePhase.AutoCompleting));
        }

        // --- Dispose: unsubscribes all handlers ---

        [Test]
        public void Dispose_UnsubscribesFromBoardStateChanged()
        {
            FillAllFoundations(_board);
            _gamePhase.Phase.Value = GamePhase.Playing;
            _sut.Dispose();

            _boardStateSubscriber.Trigger(new BoardStateChangedMessage());

            Assert.That(_winDetectedPublisher.MessageCount, Is.EqualTo(0));
        }

        [Test]
        public void Dispose_UnsubscribesFromNoMovesDetected()
        {
            _gamePhase.Phase.Value = GamePhase.Playing;
            _sut.Dispose();

            _noMovesSubscriber.Trigger(new NoMovesDetectedMessage());

            Assert.That(_gamePhase.Phase.Value, Is.EqualTo(GamePhase.Playing));
        }

        [Test]
        public void Dispose_UnsubscribesFromNewGameRequested()
        {
            _gamePhase.Phase.Value = GamePhase.Won;
            _sut.Dispose();

            _newGameSubscriber.Trigger(new NewGameRequestedMessage());

            Assert.That(_gamePhase.Phase.Value, Is.EqualTo(GamePhase.Won));
        }

        [Test]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            _sut.Dispose();

            Assert.DoesNotThrow(() => _sut.Dispose());
        }

        // --- Private helpers ---

        private static void FillAllFoundations(BoardModel board)
        {
            Suit[] suits = { Suit.Hearts, Suit.Diamonds, Suit.Clubs, Suit.Spades };
            for (int foundationIndex = 0; foundationIndex < 4; foundationIndex++)
            {
                board.Foundations[foundationIndex].Clear();
                for (int rankValue = 1; rankValue <= 13; rankValue++)
                {
                    CardModel card = new CardModel(suits[foundationIndex], (Rank)rankValue);
                    card.IsFaceUp.Value = true;
                    board.Foundations[foundationIndex].AddCard(card);
                }
            }
        }

        private static void FillFoundationsExceptLast(BoardModel board)
        {
            Suit[] suits = { Suit.Hearts, Suit.Diamonds, Suit.Clubs, Suit.Spades };
            for (int foundationIndex = 0; foundationIndex < 3; foundationIndex++)
            {
                board.Foundations[foundationIndex].Clear();
                for (int rankValue = 1; rankValue <= 13; rankValue++)
                {
                    CardModel card = new CardModel(suits[foundationIndex], (Rank)rankValue);
                    card.IsFaceUp.Value = true;
                    board.Foundations[foundationIndex].AddCard(card);
                }
            }

            board.Foundations[3].Clear();
            for (int rankValue = 1; rankValue <= 12; rankValue++)
            {
                CardModel card = new CardModel(Suit.Spades, (Rank)rankValue);
                card.IsFaceUp.Value = true;
                board.Foundations[3].AddCard(card);
            }
        }
    }
}
