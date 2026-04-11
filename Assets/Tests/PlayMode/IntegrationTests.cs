using System;
using System.Collections;
using System.Collections.Generic;
using KlondikeSolitaire.Core;
using KlondikeSolitaire.Systems;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace KlondikeSolitaire.Tests.PlayMode
{
    [TestFixture]
    public sealed class IntegrationTests
    {
        private BoardModel _board;
        private ScoreModel _scoreModel;
        private GamePhaseModel _gamePhaseModel;
        private ScoringTable _scoringTable;

        private TestPublisher _cardMovedPublisher;
        private TestPublisher _cardFlippedPublisher;
        private TestPublisher _boardStatePublisher;
        private TestPublisher _scoreChangedPublisher;
        private TestPublisher _undoAvailabilityPublisher;
        private TestPublisher _dealCompletedPublisher;
        private TestPublisher _autoCompleteAvailablePublisher;
        private TestPublisher _hintHighlightPublisher;
        private TestPublisher _hintClearedPublisher;
        private TestPublisher _noMovesPublisher;
        private TestPublisher _gamePhaseChangedPublisher;
        private TestPublisher _winDetectedPublisher;

        private TestBoardStateSubscriber _boardStateSubscriber;
        private TestNoMovesSubscriber _noMovesSubscriber;
        private TestNewGameSubscriber _newGameSubscriber;

        private MoveValidationSystem _moveValidation;
        private ScoringSystem _scoringSystem;
        private UndoSystem _undoSystem;
        private DealSystem _dealSystem;
        private MoveExecutionSystem _moveExecution;
        private HintSystem _hintSystem;
        private AutoCompleteSystem _autoComplete;
        private NoMovesDetectionSystem _noMovesDetection;
        private GameFlowSystem _gameFlow;

        [SetUp]
        public void SetUp()
        {
            _board = new BoardModel();
            _scoreModel = new ScoreModel();
            _gamePhaseModel = new GamePhaseModel();
            _scoringTable = new ScoringTable(5, 10, 10, -15, 5);

            _cardMovedPublisher = new TestPublisher();
            _cardFlippedPublisher = new TestPublisher();
            _boardStatePublisher = new TestPublisher();
            _scoreChangedPublisher = new TestPublisher();
            _undoAvailabilityPublisher = new TestPublisher();
            _dealCompletedPublisher = new TestPublisher();
            _autoCompleteAvailablePublisher = new TestPublisher();
            _hintHighlightPublisher = new TestPublisher();
            _hintClearedPublisher = new TestPublisher();
            _noMovesPublisher = new TestPublisher();
            _gamePhaseChangedPublisher = new TestPublisher();
            _winDetectedPublisher = new TestPublisher();

            _boardStateSubscriber = new TestBoardStateSubscriber();
            _noMovesSubscriber = new TestNoMovesSubscriber();
            _newGameSubscriber = new TestNewGameSubscriber();

            _moveValidation = new MoveValidationSystem();
            _scoringSystem = new ScoringSystem(
                _scoreModel,
                _scoringTable,
                new TypedPublisher<ScoreChangedMessage>(_scoreChangedPublisher));
            _undoSystem = new UndoSystem(
                _board,
                _scoringSystem,
                new TypedPublisher<UndoAvailabilityChangedMessage>(_undoAvailabilityPublisher),
                new TypedPublisher<BoardStateChangedMessage>(_boardStatePublisher),
                new TypedPublisher<CardFlippedMessage>(_cardFlippedPublisher));
            _dealSystem = new DealSystem(
                _board,
                new TypedPublisher<DealCompletedMessage>(_dealCompletedPublisher));
            _moveExecution = new MoveExecutionSystem(
                _board,
                _scoringSystem,
                _undoSystem,
                new TypedPublisher<CardMovedMessage>(_cardMovedPublisher),
                new TypedPublisher<CardFlippedMessage>(_cardFlippedPublisher),
                new TypedPublisher<BoardStateChangedMessage>(_boardStatePublisher));
            _hintSystem = new HintSystem(
                _board,
                _moveValidation,
                _boardStateSubscriber,
                new TypedPublisher<HintHighlightMessage>(_hintHighlightPublisher),
                new TypedPublisher<HintClearedMessage>(_hintClearedPublisher));
            _autoComplete = new AutoCompleteSystem(
                _board,
                _moveValidation,
                _boardStateSubscriber,
                new TypedPublisher<AutoCompleteAvailableMessage>(_autoCompleteAvailablePublisher));
            _noMovesDetection = new NoMovesDetectionSystem(
                _board,
                _moveValidation,
                _gamePhaseModel,
                _boardStateSubscriber,
                new TypedPublisher<NoMovesDetectedMessage>(_noMovesPublisher));
            _gameFlow = new GameFlowSystem(
                _gamePhaseModel,
                _dealSystem,
                _board,
                _scoreModel,
                _undoSystem,
                _hintSystem,
                _boardStateSubscriber,
                _noMovesSubscriber,
                _newGameSubscriber,
                new TypedPublisher<GamePhaseChangedMessage>(_gamePhaseChangedPublisher),
                new TypedPublisher<WinDetectedMessage>(_winDetectedPublisher),
                new TypedPublisher<DealCompletedMessage>(_dealCompletedPublisher));
        }

        [TearDown]
        public void TearDown()
        {
            _gameFlow.Dispose();
            _noMovesDetection.Dispose();
            _autoComplete.Dispose();
            _hintSystem.Dispose();
            _moveExecution.Dispose();
            _undoSystem.Dispose();
        }

        // --- Full game flow: deal → execute moves → verify score updates ---

        [UnityTest]
        public IEnumerator FullGameFlow_DealCards_BoardHas28TableauCards()
        {
            _gameFlow.StartNewGame();
            yield return null;

            int totalTableauCards = 0;
            for (int columnIndex = 0; columnIndex < _board.Tableau.Length; columnIndex++)
            {
                totalTableauCards += _board.Tableau[columnIndex].Count;
            }

            Assert.That(totalTableauCards, Is.EqualTo(28));
        }

        [UnityTest]
        public IEnumerator FullGameFlow_DealCards_StockHas24Cards()
        {
            _gameFlow.StartNewGame();
            yield return null;

            Assert.That(_board.Stock.Count, Is.EqualTo(24));
        }

        [UnityTest]
        public IEnumerator FullGameFlow_DealCards_EachTableauColumnTopCardIsFaceUp()
        {
            _gameFlow.StartNewGame();
            yield return null;

            for (int columnIndex = 0; columnIndex < _board.Tableau.Length; columnIndex++)
            {
                Assert.That(_board.Tableau[columnIndex].TopCard.IsFaceUp.Value, Is.True,
                    $"Tableau[{columnIndex}] top card should be face up after deal");
            }
        }

        [UnityTest]
        public IEnumerator FullGameFlow_DealCards_GamePhaseIsPlaying()
        {
            _gameFlow.StartNewGame();
            yield return null;

            Assert.That(_gamePhaseModel.Phase.Value, Is.EqualTo(GamePhase.Playing));
        }

        [UnityTest]
        public IEnumerator FullGameFlow_DealCards_DealCompletedMessagePublished()
        {
            _gameFlow.StartNewGame();
            yield return null;

            Assert.That(_dealCompletedPublisher.MessageCount, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator FullGameFlow_ExecuteTableauToFoundationMove_ScoreIncreases()
        {
            _board.Tableau[0].Clear();
            CardModel aceHearts = new CardModel(Suit.Hearts, Rank.Ace);
            aceHearts.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(aceHearts);
            _gamePhaseModel.Phase.Value = GamePhase.Playing;
            yield return null;

            _moveExecution.ExecuteMove(PileId.Tableau(0), PileId.Foundation(0), 1);

            Assert.That(_scoreModel.Score.Value, Is.EqualTo(10));
        }

        [UnityTest]
        public IEnumerator FullGameFlow_ExecuteTableauToFoundationMove_CardMovedMessagePublished()
        {
            _board.Tableau[0].Clear();
            CardModel aceHearts = new CardModel(Suit.Hearts, Rank.Ace);
            aceHearts.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(aceHearts);
            _gamePhaseModel.Phase.Value = GamePhase.Playing;
            yield return null;

            _moveExecution.ExecuteMove(PileId.Tableau(0), PileId.Foundation(0), 1);

            Assert.That(_cardMovedPublisher.MessageCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator FullGameFlow_ExecuteTableauToFoundationMove_BoardStateChangedMessagePublished()
        {
            _board.Tableau[0].Clear();
            CardModel aceHearts = new CardModel(Suit.Hearts, Rank.Ace);
            aceHearts.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(aceHearts);
            _gamePhaseModel.Phase.Value = GamePhase.Playing;
            yield return null;

            _moveExecution.ExecuteMove(PileId.Tableau(0), PileId.Foundation(0), 1);

            Assert.That(_boardStatePublisher.MessageCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator FullGameFlow_ExecuteTableauToFoundationMove_CardIsOnFoundation()
        {
            _board.Tableau[0].Clear();
            CardModel aceHearts = new CardModel(Suit.Hearts, Rank.Ace);
            aceHearts.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(aceHearts);
            _gamePhaseModel.Phase.Value = GamePhase.Playing;
            yield return null;

            _moveExecution.ExecuteMove(PileId.Tableau(0), PileId.Foundation(0), 1);

            Assert.That(_board.Foundations[0].Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator FullGameFlow_DrawFromStock_WasteGainsCard()
        {
            CardModel card = new CardModel(Suit.Clubs, Rank.Seven);
            _board.Stock.AddCard(card);
            _gamePhaseModel.Phase.Value = GamePhase.Playing;
            yield return null;

            _moveExecution.DrawFromStock();

            Assert.That(_board.Waste.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator FullGameFlow_DrawFromStock_DrawnCardIsFaceUp()
        {
            CardModel card = new CardModel(Suit.Clubs, Rank.Seven);
            _board.Stock.AddCard(card);
            _gamePhaseModel.Phase.Value = GamePhase.Playing;
            yield return null;

            _moveExecution.DrawFromStock();

            Assert.That(_board.Waste.TopCard.IsFaceUp.Value, Is.True);
        }

        [UnityTest]
        public IEnumerator FullGameFlow_MultipleMovesIncreaseScore_ScoreAccumulates()
        {
            CardModel aceHearts = new CardModel(Suit.Hearts, Rank.Ace);
            aceHearts.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(aceHearts);

            CardModel aceDiamonds = new CardModel(Suit.Diamonds, Rank.Ace);
            aceDiamonds.IsFaceUp.Value = true;
            _board.Tableau[1].AddCard(aceDiamonds);

            _gamePhaseModel.Phase.Value = GamePhase.Playing;
            yield return null;

            _moveExecution.ExecuteMove(PileId.Tableau(0), PileId.Foundation((int)Suit.Hearts), 1);
            _moveExecution.ExecuteMove(PileId.Tableau(1), PileId.Foundation((int)Suit.Diamonds), 1);

            Assert.That(_scoreModel.Score.Value, Is.EqualTo(20));
        }

        // --- Undo cycle: execute move, undo, verify board restored ---

        [UnityTest]
        public IEnumerator UndoCycle_AfterTableauToFoundationMove_BoardRestoredToPreMoveState()
        {
            CardModel aceHearts = new CardModel(Suit.Hearts, Rank.Ace);
            aceHearts.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(aceHearts);
            _gamePhaseModel.Phase.Value = GamePhase.Playing;
            yield return null;

            _moveExecution.ExecuteMove(PileId.Tableau(0), PileId.Foundation(0), 1);
            _undoSystem.Undo();

            Assert.That(_board.Tableau[0].Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator UndoCycle_AfterTableauToFoundationMove_FoundationEmptyAfterUndo()
        {
            CardModel aceHearts = new CardModel(Suit.Hearts, Rank.Ace);
            aceHearts.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(aceHearts);
            _gamePhaseModel.Phase.Value = GamePhase.Playing;
            yield return null;

            _moveExecution.ExecuteMove(PileId.Tableau(0), PileId.Foundation(0), 1);
            _undoSystem.Undo();

            Assert.That(_board.Foundations[0].Count, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator UndoCycle_AfterTableauToFoundationMove_ScoreRevertedAfterUndo()
        {
            CardModel aceHearts = new CardModel(Suit.Hearts, Rank.Ace);
            aceHearts.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(aceHearts);
            _gamePhaseModel.Phase.Value = GamePhase.Playing;
            yield return null;

            _moveExecution.ExecuteMove(PileId.Tableau(0), PileId.Foundation(0), 1);
            _undoSystem.Undo();

            Assert.That(_scoreModel.Score.Value, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator UndoCycle_AfterTableauToFoundationMove_OriginalCardRestoredToTableau()
        {
            CardModel aceHearts = new CardModel(Suit.Hearts, Rank.Ace);
            aceHearts.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(aceHearts);
            _gamePhaseModel.Phase.Value = GamePhase.Playing;
            yield return null;

            _moveExecution.ExecuteMove(PileId.Tableau(0), PileId.Foundation(0), 1);
            _undoSystem.Undo();

            Assert.That(_board.Tableau[0].TopCard, Is.SameAs(aceHearts));
        }

        [UnityTest]
        public IEnumerator UndoCycle_AfterDrawFromStock_CardReturnedToStock()
        {
            CardModel card = new CardModel(Suit.Spades, Rank.King);
            _board.Stock.AddCard(card);
            _gamePhaseModel.Phase.Value = GamePhase.Playing;
            yield return null;

            _moveExecution.DrawFromStock();
            _undoSystem.Undo();

            Assert.That(_board.Stock.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator UndoCycle_AfterDrawFromStock_WasteEmptyAfterUndo()
        {
            CardModel card = new CardModel(Suit.Spades, Rank.King);
            _board.Stock.AddCard(card);
            _gamePhaseModel.Phase.Value = GamePhase.Playing;
            yield return null;

            _moveExecution.DrawFromStock();
            _undoSystem.Undo();

            Assert.That(_board.Waste.Count, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator UndoCycle_AfterDrawFromStock_CardInStockIsFaceDownAfterUndo()
        {
            CardModel card = new CardModel(Suit.Spades, Rank.King);
            _board.Stock.AddCard(card);
            _gamePhaseModel.Phase.Value = GamePhase.Playing;
            yield return null;

            _moveExecution.DrawFromStock();
            _undoSystem.Undo();

            Assert.That(_board.Stock.TopCard.IsFaceUp.Value, Is.False);
        }

        [UnityTest]
        public IEnumerator UndoCycle_UndoAfterFlippingCard_FlippedCardBecomesFaceDownAgain()
        {
            CardModel hiddenCard = new CardModel(Suit.Clubs, Rank.Five);
            hiddenCard.IsFaceUp.Value = false;
            _board.Tableau[0].AddCard(hiddenCard);

            CardModel topCard = new CardModel(Suit.Hearts, Rank.Six);
            topCard.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(topCard);

            CardModel destCard = new CardModel(Suit.Spades, Rank.Seven);
            destCard.IsFaceUp.Value = true;
            _board.Tableau[1].AddCard(destCard);

            _gamePhaseModel.Phase.Value = GamePhase.Playing;
            yield return null;

            _moveExecution.ExecuteMove(PileId.Tableau(0), PileId.Tableau(1), 1);
            _undoSystem.Undo();

            Assert.That(hiddenCard.IsFaceUp.Value, Is.False);
        }

        [UnityTest]
        public IEnumerator UndoCycle_TwoMovesUndoBoth_CanUndoIsFalseAfterBothUndone()
        {
            CardModel aceHearts = new CardModel(Suit.Hearts, Rank.Ace);
            aceHearts.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(aceHearts);

            CardModel aceDiamonds = new CardModel(Suit.Diamonds, Rank.Ace);
            aceDiamonds.IsFaceUp.Value = true;
            _board.Tableau[1].AddCard(aceDiamonds);

            _gamePhaseModel.Phase.Value = GamePhase.Playing;
            yield return null;

            _moveExecution.ExecuteMove(PileId.Tableau(0), PileId.Foundation((int)Suit.Hearts), 1);
            _moveExecution.ExecuteMove(PileId.Tableau(1), PileId.Foundation((int)Suit.Diamonds), 1);
            _undoSystem.Undo();
            _undoSystem.Undo();

            Assert.That(_undoSystem.CanUndo, Is.False);
        }

        [UnityTest]
        public IEnumerator UndoCycle_TwoMovesUndoBoth_ScoreIsZeroAfterBothUndone()
        {
            CardModel aceHearts = new CardModel(Suit.Hearts, Rank.Ace);
            aceHearts.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(aceHearts);

            CardModel aceDiamonds = new CardModel(Suit.Diamonds, Rank.Ace);
            aceDiamonds.IsFaceUp.Value = true;
            _board.Tableau[1].AddCard(aceDiamonds);

            _gamePhaseModel.Phase.Value = GamePhase.Playing;
            yield return null;

            _moveExecution.ExecuteMove(PileId.Tableau(0), PileId.Foundation((int)Suit.Hearts), 1);
            _moveExecution.ExecuteMove(PileId.Tableau(1), PileId.Foundation((int)Suit.Diamonds), 1);
            _undoSystem.Undo();
            _undoSystem.Undo();

            Assert.That(_scoreModel.Score.Value, Is.EqualTo(0));
        }

        // --- Auto-complete: set up auto-completable state, trigger, verify win-path sequence ---

        [UnityTest]
        public IEnumerator AutoComplete_AutoCompletableBoard_IsAutoCompletePossibleReturnsTrue()
        {
            SetupAutoCompletableBoard();
            yield return null;

            bool result = _autoComplete.IsAutoCompletePossible();

            Assert.That(result, Is.True);
        }

        [UnityTest]
        public IEnumerator AutoComplete_AutoCompletableBoard_GenerateMoveSequenceReturns52Moves()
        {
            SetupAutoCompletableBoard();
            yield return null;

            List<Move> moves = _autoComplete.GenerateMoveSequence();

            Assert.That(moves.Count, Is.EqualTo(52));
        }

        [UnityTest]
        public IEnumerator AutoComplete_AutoCompletableBoard_AllMovesGoToFoundation()
        {
            SetupAutoCompletableBoard();
            yield return null;

            List<Move> moves = _autoComplete.GenerateMoveSequence();

            for (int moveIndex = 0; moveIndex < moves.Count; moveIndex++)
            {
                Assert.That(moves[moveIndex].Destination.Type, Is.EqualTo(PileType.Foundation),
                    $"Move {moveIndex} should go to a foundation pile");
            }
        }

        [UnityTest]
        public IEnumerator AutoComplete_AutoCompletableBoard_AllMovesFromTableau()
        {
            SetupAutoCompletableBoard();
            yield return null;

            List<Move> moves = _autoComplete.GenerateMoveSequence();

            for (int moveIndex = 0; moveIndex < moves.Count; moveIndex++)
            {
                Assert.That(moves[moveIndex].Source.Type, Is.EqualTo(PileType.Tableau),
                    $"Move {moveIndex} should come from a tableau pile");
            }
        }

        [UnityTest]
        public IEnumerator AutoComplete_AfterGenerateMoveSequence_AllFoundationsHave13Cards()
        {
            SetupAutoCompletableBoard();
            yield return null;

            _autoComplete.GenerateMoveSequence();

            for (int foundationIndex = 0; foundationIndex < _board.Foundations.Length; foundationIndex++)
            {
                Assert.That(_board.Foundations[foundationIndex].Count, Is.EqualTo(13),
                    $"Foundation[{foundationIndex}] should have 13 cards after auto-complete");
            }
        }

        [UnityTest]
        public IEnumerator AutoComplete_AfterGenerateMoveSequence_AllTableauColumnsAreEmpty()
        {
            SetupAutoCompletableBoard();
            yield return null;

            _autoComplete.GenerateMoveSequence();

            for (int tableauIndex = 0; tableauIndex < _board.Tableau.Length; tableauIndex++)
            {
                Assert.That(_board.Tableau[tableauIndex].Count, Is.EqualTo(0),
                    $"Tableau[{tableauIndex}] should be empty after auto-complete");
            }
        }

        [UnityTest]
        public IEnumerator AutoComplete_StandardDealBoard_IsAutoCompletePossibleReturnsFalse()
        {
            FillBoardWithStandardDeal();
            yield return null;

            bool result = _autoComplete.IsAutoCompletePossible();

            Assert.That(result, Is.False);
        }

        [UnityTest]
        public IEnumerator AutoComplete_StartAutoCompleteTransition_GamePhaseIsAutoCompleting()
        {
            _gamePhaseModel.Phase.Value = GamePhase.Playing;
            yield return null;

            _gameFlow.StartAutoComplete();

            Assert.That(_gamePhaseModel.Phase.Value, Is.EqualTo(GamePhase.AutoCompleting));
        }

        [UnityTest]
        public IEnumerator AutoComplete_StartAutoCompleteTransition_GamePhaseChangedMessagePublished()
        {
            _gamePhaseModel.Phase.Value = GamePhase.Playing;
            yield return null;

            _gameFlow.StartAutoComplete();

            Assert.That(_gamePhaseChangedPublisher.MessageCount, Is.GreaterThan(0));
        }

        // --- Win detection: fill all foundations, board state triggers win ---

        [UnityTest]
        public IEnumerator WinDetection_AllFoundationsHave13Cards_WinDetectedMessagePublished()
        {
            FillAllFoundations();
            _gamePhaseModel.Phase.Value = GamePhase.Playing;
            yield return null;

            _boardStateSubscriber.TriggerBoardStateChanged();

            Assert.That(_winDetectedPublisher.MessageCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator WinDetection_AllFoundationsHave13Cards_GamePhaseIsWon()
        {
            FillAllFoundations();
            _gamePhaseModel.Phase.Value = GamePhase.Playing;
            yield return null;

            _boardStateSubscriber.TriggerBoardStateChanged();

            Assert.That(_gamePhaseModel.Phase.Value, Is.EqualTo(GamePhase.Won));
        }

        [UnityTest]
        public IEnumerator WinDetection_IncompleteFoundations_WinNotDetected()
        {
            for (int foundationIndex = 0; foundationIndex < 3; foundationIndex++)
            {
                for (int rankValue = 1; rankValue <= 13; rankValue++)
                {
                    CardModel card = new CardModel(Suit.Hearts, (Rank)rankValue);
                    card.IsFaceUp.Value = true;
                    _board.Foundations[foundationIndex].AddCard(card);
                }
            }
            _gamePhaseModel.Phase.Value = GamePhase.Playing;
            yield return null;

            _boardStateSubscriber.TriggerBoardStateChanged();

            Assert.That(_winDetectedPublisher.MessageCount, Is.EqualTo(0));
        }

        // --- New game: StartNewGame resets board and score ---

        [UnityTest]
        public IEnumerator NewGame_AfterStartNewGame_ScoreIsZero()
        {
            _scoreModel.Score.Value = 250;
            yield return null;

            _gameFlow.StartNewGame();

            Assert.That(_scoreModel.Score.Value, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator NewGame_AfterStartNewGame_UndoStackIsEmpty()
        {
            CardModel aceHearts = new CardModel(Suit.Hearts, Rank.Ace);
            aceHearts.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(aceHearts);
            _gamePhaseModel.Phase.Value = GamePhase.Playing;
            _moveExecution.ExecuteMove(PileId.Tableau(0), PileId.Foundation(0), 1);
            yield return null;

            _gameFlow.StartNewGame();

            Assert.That(_undoSystem.CanUndo, Is.False);
        }

        [UnityTest]
        public IEnumerator NewGame_AfterStartNewGame_BoardHasCards()
        {
            yield return null;

            _gameFlow.StartNewGame();

            int totalCards = 0;
            for (int pileIndex = 0; pileIndex < _board.AllPiles.Length; pileIndex++)
            {
                totalCards += _board.AllPiles[pileIndex].Count;
            }

            Assert.That(totalCards, Is.EqualTo(52));
        }

        // --- Private helpers ---

        private void SetupAutoCompletableBoard()
        {
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
                    _board.Tableau[columnIndex].AddCard(card);
                }
            }
        }

        private void FillBoardWithStandardDeal()
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

            deckIndex = 0;
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
                    _board.Tableau[columnIndex].AddCard(card);
                }
            }

            while (deckIndex < deck.Length)
            {
                _board.Stock.AddCard(deck[deckIndex]);
                deckIndex++;
            }
        }

        private void FillAllFoundations()
        {
            Suit[] suits = { Suit.Hearts, Suit.Diamonds, Suit.Clubs, Suit.Spades };
            for (int foundationIndex = 0; foundationIndex < 4; foundationIndex++)
            {
                for (int rankValue = 1; rankValue <= 13; rankValue++)
                {
                    CardModel card = new CardModel(suits[foundationIndex], (Rank)rankValue);
                    card.IsFaceUp.Value = true;
                    _board.Foundations[foundationIndex].AddCard(card);
                }
            }
        }
    }

    internal sealed class TestPublisher
    {
        private int _messageCount;

        public int MessageCount => _messageCount;

        public void Increment()
        {
            _messageCount++;
        }

        public void Reset()
        {
            _messageCount = 0;
        }
    }

    internal sealed class TypedPublisher<T> : MessagePipe.IPublisher<T>
    {
        private readonly TestPublisher _counter;

        public TypedPublisher(TestPublisher counter)
        {
            _counter = counter;
        }

        public void Publish(T message)
        {
            _counter.Increment();
        }
    }

    internal sealed class TestBoardStateSubscriber : MessagePipe.ISubscriber<BoardStateChangedMessage>
    {
        private System.Action<BoardStateChangedMessage> _handler;

        public void TriggerBoardStateChanged()
        {
            _handler?.Invoke(new BoardStateChangedMessage());
        }

        public IDisposable Subscribe(
            MessagePipe.IMessageHandler<BoardStateChangedMessage> handler,
            params MessagePipe.MessageHandlerFilter<BoardStateChangedMessage>[] filters)
        {
            _handler = handler.Handle;
            return new TestDisposableInternal(() => _handler = null);
        }
    }

    internal sealed class TestNoMovesSubscriber : MessagePipe.ISubscriber<NoMovesDetectedMessage>
    {
        private System.Action<NoMovesDetectedMessage> _handler;

        public IDisposable Subscribe(
            MessagePipe.IMessageHandler<NoMovesDetectedMessage> handler,
            params MessagePipe.MessageHandlerFilter<NoMovesDetectedMessage>[] filters)
        {
            _handler = handler.Handle;
            return new TestDisposableInternal(() => _handler = null);
        }

        public void Trigger()
        {
            _handler?.Invoke(new NoMovesDetectedMessage());
        }
    }

    internal sealed class TestNewGameSubscriber : MessagePipe.ISubscriber<NewGameRequestedMessage>
    {
        private System.Action<NewGameRequestedMessage> _handler;

        public IDisposable Subscribe(
            MessagePipe.IMessageHandler<NewGameRequestedMessage> handler,
            params MessagePipe.MessageHandlerFilter<NewGameRequestedMessage>[] filters)
        {
            _handler = handler.Handle;
            return new TestDisposableInternal(() => _handler = null);
        }

        public void Trigger()
        {
            _handler?.Invoke(new NewGameRequestedMessage());
        }
    }

    internal sealed class TestDisposableInternal : System.IDisposable
    {
        private readonly System.Action _onDispose;

        public TestDisposableInternal(System.Action onDispose)
        {
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            _onDispose?.Invoke();
        }
    }
}
