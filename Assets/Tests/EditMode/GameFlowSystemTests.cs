using KlondikeSolitaire.Core;
using KlondikeSolitaire.Systems;
using NUnit.Framework;

namespace KlondikeSolitaire.Tests
{
    [TestFixture]
    public sealed class GameFlowSystemTests
    {
        private GamePhaseModel _gamePhase;
        private DealModel _dealModel;
        private BoardModel _board;
        private ScoreModel _scoreModel;
        private ScoringSystem _scoringSystem;
        private DealSystem _dealSystem;
        private UndoSystem _undoSystem;
        private HintSystem _hintSystem;
        private System.Random _random;
        private TestSubscriber<BoardStateChangedMessage> _boardStateSubscriber;
        private TestSubscriber<NoMovesDetectedMessage> _noMovesSubscriber;
        private TestSubscriber<NewGameRequestedMessage> _newGameSubscriber;
        private TestSubscriber<AutoCompleteRequestedMessage> _autoCompleteSubscriber;
        private TestSubscriber<DealAnimationCompletedMessage> _dealAnimCompletedSubscriber;
        private TestPublisher<GamePhaseChangedMessage> _phaseChangedPublisher;
        private TestPublisher<WinDetectedMessage> _winDetectedPublisher;
        private TestPublisher<DealCompletedMessage> _dealCompletedPublisher;
        private GameFlowSystem _sut;

        [SetUp]
        public void SetUp()
        {
            _gamePhase = new GamePhaseModel();
            _dealModel = new DealModel();
            _board = TestBoardFactory.EmptyBoard();
            _scoreModel = new ScoreModel();
            _random = new System.Random(42);

            _dealCompletedPublisher = new TestPublisher<DealCompletedMessage>();
            _dealSystem = new DealSystem(_board, _dealCompletedPublisher);

            var scoreChangedPublisher = new TestPublisher<ScoreChangedMessage>();
            _scoringSystem = new ScoringSystem(_scoreModel, new ScoringTable(5, 10, 10, -15, 5), scoreChangedPublisher);
            var undoAvailabilityPublisher = new TestPublisher<UndoAvailabilityChangedMessage>();
            var boardStateForUndo = new TestPublisher<BoardStateChangedMessage>();
            var cardFlippedPublisher = new TestPublisher<CardFlippedMessage>();
            var cardMovedPublisher = new TestPublisher<CardMovedMessage>();
            var undoRequestedSubscriber = new TestSubscriber<UndoRequestedMessage>();
            _undoSystem = new UndoSystem(
                _board,
                _scoringSystem,
                undoRequestedSubscriber,
                undoAvailabilityPublisher,
                boardStateForUndo,
                cardFlippedPublisher,
                cardMovedPublisher);

            var moveEnumerator = new MoveEnumerator(new MoveValidationSystem());
            var boardStateForHint = new TestSubscriber<BoardStateChangedMessage>();
            var hintHighlightPublisher = new TestPublisher<HintHighlightMessage>();
            var hintClearedPublisher = new TestPublisher<HintClearedMessage>();
            var hintRequestedSubscriber = new TestSubscriber<HintRequestedMessage>();
            _hintSystem = new HintSystem(
                _board,
                moveEnumerator,
                boardStateForHint,
                hintRequestedSubscriber,
                hintHighlightPublisher,
                hintClearedPublisher);

            _boardStateSubscriber = new TestSubscriber<BoardStateChangedMessage>();
            _noMovesSubscriber = new TestSubscriber<NoMovesDetectedMessage>();
            _newGameSubscriber = new TestSubscriber<NewGameRequestedMessage>();
            _autoCompleteSubscriber = new TestSubscriber<AutoCompleteRequestedMessage>();
            _dealAnimCompletedSubscriber = new TestSubscriber<DealAnimationCompletedMessage>();
            _phaseChangedPublisher = new TestPublisher<GamePhaseChangedMessage>();
            _winDetectedPublisher = new TestPublisher<WinDetectedMessage>();

            _sut = CreateSystem();
        }

        [TearDown]
        public void TearDown()
        {
            _sut.Dispose();
            _hintSystem.Dispose();
        }

        private GameFlowSystem CreateSystem()
        {
            return new GameFlowSystem(
                _gamePhase,
                _dealModel,
                _dealSystem,
                _board,
                _scoreModel,
                _scoringSystem,
                _undoSystem,
                _hintSystem,
                _random,
                _boardStateSubscriber,
                _noMovesSubscriber,
                _newGameSubscriber,
                _autoCompleteSubscriber,
                _dealAnimCompletedSubscriber,
                _phaseChangedPublisher,
                _winDetectedPublisher);
        }

        // --- StartNewGame: phase transitions ---

        [Test]
        public void StartNewGame_SetsPhaseToDealing()
        {
            _sut.StartNewGame();

            Assert.That(_gamePhase.Phase.Value, Is.EqualTo(GamePhase.Dealing));
        }

        [Test]
        public void DealAnimationCompleted_DuringDealing_SetsPhaseToPlaying()
        {
            _sut.StartNewGame();

            _dealAnimCompletedSubscriber.Trigger(new DealAnimationCompletedMessage());

            Assert.That(_gamePhase.Phase.Value, Is.EqualTo(GamePhase.Playing));
        }

        [Test]
        public void DealAnimationCompleted_NotDuringDealing_DoesNotChangePhase()
        {
            _gamePhase.Phase.Value = GamePhase.Playing;

            _dealAnimCompletedSubscriber.Trigger(new DealAnimationCompletedMessage());

            Assert.That(_gamePhase.Phase.Value, Is.EqualTo(GamePhase.Playing));
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
                false);
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
        public void OnBoardStateChanged_AllFoundationsComplete_WinDetectedMessageContainsFinalScore()
        {
            FillAllFoundations(_board);
            _gamePhase.Phase.Value = GamePhase.Playing;
            _scoreModel.Score.Value = 150;

            _boardStateSubscriber.Trigger(new BoardStateChangedMessage());

            Assert.That(_winDetectedPublisher.LastMessage.FinalScore, Is.EqualTo(150));
        }

        [Test]
        public void OnBoardStateChanged_IncompleteFoundations_DoesNotSetPhaseToWon()
        {
            FillFoundationsExceptLast(_board);
            _gamePhase.Phase.Value = GamePhase.Playing;

            _boardStateSubscriber.Trigger(new BoardStateChangedMessage());

            Assert.That(_gamePhase.Phase.Value, Is.EqualTo(GamePhase.Playing));
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
        public void OnNewGameRequested_SetsPhaseToDealing()
        {
            _gamePhase.Phase.Value = GamePhase.Won;

            _newGameSubscriber.Trigger(new NewGameRequestedMessage());

            Assert.That(_gamePhase.Phase.Value, Is.EqualTo(GamePhase.Dealing));
        }

        [Test]
        public void OnNewGameRequested_ResetsScoreToZero()
        {
            _scoreModel.Score.Value = 500;

            _newGameSubscriber.Trigger(new NewGameRequestedMessage());

            Assert.That(_scoreModel.Score.Value, Is.EqualTo(0));
        }

        // --- StartAutoComplete ---

        [Test]
        public void StartAutoComplete_SetsPhaseToAutoCompleting()
        {
            _gamePhase.Phase.Value = GamePhase.Playing;

            _sut.StartAutoComplete();

            Assert.That(_gamePhase.Phase.Value, Is.EqualTo(GamePhase.AutoCompleting));
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
