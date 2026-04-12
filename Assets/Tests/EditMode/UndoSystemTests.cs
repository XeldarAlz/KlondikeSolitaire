using KlondikeSolitaire.Core;
using KlondikeSolitaire.Systems;
using NUnit.Framework;

namespace KlondikeSolitaire.Tests
{
    [TestFixture]
    public sealed class UndoSystemTests
    {
        private BoardModel _board;
        private ScoreModel _scoreModel;
        private ScoringSystem _scoringSystem;
        private TestSubscriber<UndoRequestedMessage> _undoRequestedSubscriber;
        private TestPublisher<UndoAvailabilityChangedMessage> _undoAvailabilityPublisher;
        private TestPublisher<BoardStateChangedMessage> _boardStatePublisher;
        private TestPublisher<CardFlippedMessage> _cardFlippedPublisher;
        private TestPublisher<CardMovedMessage> _cardMovedPublisher;
        private TestPublisher<ScoreChangedMessage> _scoreChangedPublisher;
        private UndoSystem _sut;

        [SetUp]
        public void SetUp()
        {
            _board = TestBoardFactory.EmptyBoard();
            _scoreModel = new ScoreModel();
            _scoreChangedPublisher = new TestPublisher<ScoreChangedMessage>();
            _scoringSystem = new ScoringSystem(_scoreModel, new ScoringTable(5, 10, 10, -15, 5), _scoreChangedPublisher);
            _undoRequestedSubscriber = new TestSubscriber<UndoRequestedMessage>();
            _undoAvailabilityPublisher = new TestPublisher<UndoAvailabilityChangedMessage>();
            _boardStatePublisher = new TestPublisher<BoardStateChangedMessage>();
            _cardFlippedPublisher = new TestPublisher<CardFlippedMessage>();
            _cardMovedPublisher = new TestPublisher<CardMovedMessage>();
            _sut = new UndoSystem(
                _board,
                _scoringSystem,
                _undoRequestedSubscriber,
                _undoAvailabilityPublisher,
                _boardStatePublisher,
                _cardFlippedPublisher,
                _cardMovedPublisher);
        }

        [TearDown]
        public void TearDown()
        {
            _sut.Dispose();
        }

        // --- CanUndo state ---

        [Test]
        public void CanUndo_InitialState_IsFalse()
        {
            Assert.That(_sut.CanUndo, Is.False);
        }

        [Test]
        public void CanUndo_AfterPush_IsTrue()
        {
            MoveCommand command = BuildNormalMoveCommand(PileId.Waste(), PileId.Tableau(0), 1, 5, wasCardFlipped: false);

            _sut.Push(command);

            Assert.That(_sut.CanUndo, Is.True);
        }

        [Test]
        public void CanUndo_AfterClear_IsFalse()
        {
            MoveCommand command = BuildNormalMoveCommand(PileId.Waste(), PileId.Tableau(0), 1, 5, wasCardFlipped: false);
            _sut.Push(command);

            _sut.Clear();

            Assert.That(_sut.CanUndo, Is.False);
        }

        [Test]
        public void CanUndo_AfterUndoingLastCommand_IsFalse()
        {
            CardModel card = new CardModel(Suit.Hearts, Rank.Ace);
            card.IsFaceUp.Value = true;
            _board.Waste.AddCard(card);
            _board.Tableau[0].AddCard(card);
            _board.Waste.RemoveTop(1);

            MoveCommand command = BuildNormalMoveCommand(PileId.Waste(), PileId.Tableau(0), 1, 5, wasCardFlipped: false);
            _sut.Push(command);

            _sut.Undo();

            Assert.That(_sut.CanUndo, Is.False);
        }

        // --- Push ---

        [Test]
        public void Push_PublishesUndoAvailabilityChangedMessageWithTrue()
        {
            MoveCommand command = BuildNormalMoveCommand(PileId.Waste(), PileId.Tableau(0), 1, 5, wasCardFlipped: false);

            _sut.Push(command);

            Assert.That(_undoAvailabilityPublisher.MessageCount, Is.EqualTo(1));
            Assert.That(_undoAvailabilityPublisher.LastMessage.IsAvailable, Is.True);
        }

        [Test]
        public void Push_MultipleTimes_PublishesMessageEachTime()
        {
            MoveCommand command1 = BuildNormalMoveCommand(PileId.Waste(), PileId.Tableau(0), 1, 5, wasCardFlipped: false);
            MoveCommand command2 = BuildNormalMoveCommand(PileId.Waste(), PileId.Tableau(1), 1, 5, wasCardFlipped: false);

            _sut.Push(command1);
            _sut.Push(command2);

            Assert.That(_undoAvailabilityPublisher.MessageCount, Is.EqualTo(2));
        }

        // --- Undo normal move: cards move back from destination to source ---

        [Test]
        public void Undo_NormalMove_CardReturnedToSourceIsOriginalCard()
        {
            CardModel card = new CardModel(Suit.Hearts, Rank.Ace);
            card.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(card);
            MoveCommand command = BuildNormalMoveCommand(PileId.Waste(), PileId.Tableau(0), 1, 5, wasCardFlipped: false);
            _sut.Push(command);

            _sut.Undo();

            Assert.That(_board.Waste.TopCard, Is.SameAs(card));
        }

        [Test]
        public void Undo_NormalMoveWithMultipleCards_MovesAllCardsBackToSource()
        {
            CardModel card1 = new CardModel(Suit.Hearts, Rank.King);
            CardModel card2 = new CardModel(Suit.Spades, Rank.Queen);
            card1.IsFaceUp.Value = true;
            card2.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(card1);
            _board.Tableau[0].AddCard(card2);
            MoveCommand command = BuildNormalMoveCommand(PileId.Tableau(1), PileId.Tableau(0), 2, 0, wasCardFlipped: false);
            _sut.Push(command);

            _sut.Undo();

            Assert.That(_board.Tableau[0].Count, Is.EqualTo(0));
            Assert.That(_board.Tableau[1].Count, Is.EqualTo(2));
        }

        // --- Undo normal move: score reversal ---

        [Test]
        public void Undo_NormalMoveWithPositiveScore_DecreasesScore()
        {
            CardModel card = new CardModel(Suit.Hearts, Rank.Ace);
            card.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(card);
            _scoreModel.Score.Value = 10;
            MoveCommand command = BuildNormalMoveCommand(PileId.Waste(), PileId.Tableau(0), 1, 10, wasCardFlipped: false);
            _sut.Push(command);

            _sut.Undo();

            Assert.That(_scoreModel.Score.Value, Is.EqualTo(0));
        }

        [Test]
        public void Undo_NormalMoveWithZeroScore_ScoreRemainsUnchanged()
        {
            CardModel card = new CardModel(Suit.Hearts, Rank.Ace);
            card.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(card);
            _scoreModel.Score.Value = 25;
            MoveCommand command = BuildNormalMoveCommand(PileId.Tableau(1), PileId.Tableau(0), 1, 0, wasCardFlipped: false);
            _sut.Push(command);

            _sut.Undo();

            Assert.That(_scoreModel.Score.Value, Is.EqualTo(25));
        }

        // --- Undo with WasCardFlipped: source top card gets unflipped ---

        [Test]
        public void Undo_WithWasCardFlipped_SourceTopCardBecomessFaceDown()
        {
            CardModel faceUpCardInSource = new CardModel(Suit.Clubs, Rank.Five);
            faceUpCardInSource.IsFaceUp.Value = true;
            _board.Tableau[1].AddCard(faceUpCardInSource);

            CardModel movedCard = new CardModel(Suit.Hearts, Rank.Six);
            movedCard.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(movedCard);

            MoveCommand command = BuildNormalMoveCommand(PileId.Tableau(1), PileId.Tableau(0), 1, 5, wasCardFlipped: true);
            _sut.Push(command);

            _sut.Undo();

            Assert.That(faceUpCardInSource.IsFaceUp.Value, Is.False);
        }

        [Test]
        public void Undo_WithWasCardFlipped_PublishesCardFlippedMessage()
        {
            CardModel faceUpCardInSource = new CardModel(Suit.Clubs, Rank.Five);
            faceUpCardInSource.IsFaceUp.Value = true;
            _board.Tableau[1].AddCard(faceUpCardInSource);

            CardModel movedCard = new CardModel(Suit.Hearts, Rank.Six);
            movedCard.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(movedCard);

            MoveCommand command = BuildNormalMoveCommand(PileId.Tableau(1), PileId.Tableau(0), 1, 5, wasCardFlipped: true);
            _sut.Push(command);

            _sut.Undo();

            Assert.That(_cardFlippedPublisher.MessageCount, Is.EqualTo(1));
        }

        [Test]
        public void Undo_WithWasCardFlipped_CardFlippedMessageHasCorrectPileId()
        {
            CardModel faceUpCardInSource = new CardModel(Suit.Clubs, Rank.Five);
            faceUpCardInSource.IsFaceUp.Value = true;
            _board.Tableau[2].AddCard(faceUpCardInSource);

            CardModel movedCard = new CardModel(Suit.Hearts, Rank.Six);
            movedCard.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(movedCard);

            MoveCommand command = BuildNormalMoveCommand(PileId.Tableau(2), PileId.Tableau(0), 1, 5, wasCardFlipped: true);
            _sut.Push(command);

            _sut.Undo();

            Assert.That(_cardFlippedPublisher.LastMessage.PileId, Is.EqualTo(PileId.Tableau(2)));
        }

        [Test]
        public void Undo_WithWasCardFlippedButSourceEmpty_DoesNotPublishCardFlippedMessage()
        {
            CardModel movedCard = new CardModel(Suit.Hearts, Rank.Six);
            movedCard.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(movedCard);

            MoveCommand command = BuildNormalMoveCommand(PileId.Tableau(1), PileId.Tableau(0), 1, 5, wasCardFlipped: true);
            _sut.Push(command);

            _sut.Undo();

            Assert.That(_cardFlippedPublisher.MessageCount, Is.EqualTo(0));
        }

        [Test]
        public void Undo_WithoutWasCardFlipped_DoesNotPublishCardFlippedMessage()
        {
            CardModel card = new CardModel(Suit.Hearts, Rank.Ace);
            card.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(card);

            MoveCommand command = BuildNormalMoveCommand(PileId.Waste(), PileId.Tableau(0), 1, 5, wasCardFlipped: false);
            _sut.Push(command);

            _sut.Undo();

            Assert.That(_cardFlippedPublisher.MessageCount, Is.EqualTo(0));
        }

        // --- Undo DrawFromStock ---

        [Test]
        public void Undo_DrawFromStock_CardInStockIsFaceDown()
        {
            CardModel card = new CardModel(Suit.Diamonds, Rank.Ten);
            card.IsFaceUp.Value = true;
            _board.Waste.AddCard(card);

            MoveCommand command = BuildDrawFromStockCommand();
            _sut.Push(command);

            _sut.Undo();

            Assert.That(_board.Stock.TopCard.IsFaceUp.Value, Is.False);
        }

        [Test]
        public void Undo_DrawFromStock_StockCardIsOriginalCard()
        {
            CardModel card = new CardModel(Suit.Diamonds, Rank.Ten);
            card.IsFaceUp.Value = true;
            _board.Waste.AddCard(card);

            MoveCommand command = BuildDrawFromStockCommand();
            _sut.Push(command);

            _sut.Undo();

            Assert.That(_board.Stock.TopCard, Is.SameAs(card));
        }

        // --- Undo RecycleWaste ---

        [Test]
        public void Undo_RecycleWaste_AddsAllCardsToWaste()
        {
            CardModel card1 = new CardModel(Suit.Clubs, Rank.Two);
            CardModel card2 = new CardModel(Suit.Spades, Rank.Three);
            _board.Stock.AddCard(card1);
            _board.Stock.AddCard(card2);

            MoveCommand command = BuildRecycleWasteCommand();
            _sut.Push(command);

            _sut.Undo();

            Assert.That(_board.Waste.Count, Is.EqualTo(2));
        }

        [Test]
        public void Undo_RecycleWaste_CardsInWasteAreFaceUp()
        {
            CardModel card1 = new CardModel(Suit.Clubs, Rank.Two);
            CardModel card2 = new CardModel(Suit.Spades, Rank.Three);
            _board.Stock.AddCard(card1);
            _board.Stock.AddCard(card2);

            MoveCommand command = BuildRecycleWasteCommand();
            _sut.Push(command);

            _sut.Undo();

            for (int cardIndex = 0; cardIndex < _board.Waste.Cards.Count; cardIndex++)
            {
                Assert.That(_board.Waste.Cards[cardIndex].IsFaceUp.Value, Is.True,
                    $"Waste card at index {cardIndex} should be face up after undo RecycleWaste");
            }
        }

        [Test]
        public void Undo_RecycleWaste_CardsAreReversedBackToOriginalWasteOrder()
        {
            CardModel card1 = new CardModel(Suit.Clubs, Rank.Two);
            CardModel card2 = new CardModel(Suit.Spades, Rank.Three);
            CardModel card3 = new CardModel(Suit.Hearts, Rank.Four);
            _board.Stock.AddCard(card1);
            _board.Stock.AddCard(card2);
            _board.Stock.AddCard(card3);

            MoveCommand command = BuildRecycleWasteCommand();
            _sut.Push(command);

            _sut.Undo();

            Assert.That(_board.Waste.Cards[0], Is.SameAs(card3));
            Assert.That(_board.Waste.Cards[1], Is.SameAs(card2));
            Assert.That(_board.Waste.Cards[2], Is.SameAs(card1));
        }

        // --- Undo availability tracking ---

        [Test]
        public void Undo_LastCommandInStack_PublishesUndoAvailabilityChangedMessageWithFalse()
        {
            CardModel card = new CardModel(Suit.Hearts, Rank.Ace);
            card.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(card);
            MoveCommand command = BuildNormalMoveCommand(PileId.Waste(), PileId.Tableau(0), 1, 5, wasCardFlipped: false);
            _sut.Push(command);
            _undoAvailabilityPublisher.Clear();

            _sut.Undo();

            Assert.That(_undoAvailabilityPublisher.LastMessage.IsAvailable, Is.False);
        }

        [Test]
        public void Undo_WithMoreCommandsRemaining_PublishesUndoAvailabilityChangedMessageWithTrue()
        {
            CardModel card1 = new CardModel(Suit.Hearts, Rank.Ace);
            card1.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(card1);
            MoveCommand command1 = BuildNormalMoveCommand(PileId.Waste(), PileId.Tableau(0), 1, 5, wasCardFlipped: false);
            _sut.Push(command1);

            CardModel card2 = new CardModel(Suit.Diamonds, Rank.Two);
            card2.IsFaceUp.Value = true;
            _board.Tableau[1].AddCard(card2);
            MoveCommand command2 = BuildNormalMoveCommand(PileId.Waste(), PileId.Tableau(1), 1, 5, wasCardFlipped: false);
            _sut.Push(command2);
            _undoAvailabilityPublisher.Clear();

            _sut.Undo();

            Assert.That(_undoAvailabilityPublisher.LastMessage.IsAvailable, Is.True);
        }

        // --- Undo on empty stack does nothing ---

        [Test]
        public void Undo_OnEmptyStack_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _sut.Undo());
        }

        [Test]
        public void Undo_OnEmptyStack_DoesNotPublishBoardStateChangedMessage()
        {
            _sut.Undo();

            Assert.That(_boardStatePublisher.MessageCount, Is.EqualTo(0));
        }

        [Test]
        public void Undo_OnEmptyStack_DoesNotPublishUndoAvailabilityChangedMessage()
        {
            _sut.Undo();

            Assert.That(_undoAvailabilityPublisher.MessageCount, Is.EqualTo(0));
        }

        // --- Clear ---

        [Test]
        public void Clear_OnEmptyStack_PublishesUndoAvailabilityChangedMessageWithFalse()
        {
            _sut.Clear();

            Assert.That(_undoAvailabilityPublisher.MessageCount, Is.EqualTo(1));
            Assert.That(_undoAvailabilityPublisher.LastMessage.IsAvailable, Is.False);
        }

        [Test]
        public void Clear_AfterPush_PublishesUndoAvailabilityChangedMessageWithFalse()
        {
            MoveCommand command = BuildNormalMoveCommand(PileId.Waste(), PileId.Tableau(0), 1, 5, wasCardFlipped: false);
            _sut.Push(command);
            _undoAvailabilityPublisher.Clear();

            _sut.Clear();

            Assert.That(_undoAvailabilityPublisher.LastMessage.IsAvailable, Is.False);
        }

        [Test]
        public void Clear_AfterPushingMultipleCommands_EmptiesStack()
        {
            MoveCommand command1 = BuildNormalMoveCommand(PileId.Waste(), PileId.Tableau(0), 1, 5, wasCardFlipped: false);
            MoveCommand command2 = BuildNormalMoveCommand(PileId.Waste(), PileId.Tableau(1), 1, 5, wasCardFlipped: false);
            _sut.Push(command1);
            _sut.Push(command2);

            _sut.Clear();

            Assert.That(_sut.CanUndo, Is.False);
        }

        // --- Multi-step undo (LIFO order) ---

        [Test]
        public void MultiStepUndo_Push3Undo3_LastPushedIsUndoneFirst()
        {
            CardModel cardA = new CardModel(Suit.Hearts, Rank.Ace);
            cardA.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(cardA);
            MoveCommand command1 = BuildNormalMoveCommand(PileId.Waste(), PileId.Tableau(0), 1, 5, wasCardFlipped: false);
            _sut.Push(command1);
            _sut.Undo();

            CardModel cardB = new CardModel(Suit.Diamonds, Rank.Two);
            cardB.IsFaceUp.Value = true;
            _board.Tableau[1].AddCard(cardB);
            MoveCommand command2 = BuildNormalMoveCommand(PileId.Waste(), PileId.Tableau(1), 1, 5, wasCardFlipped: false);
            _sut.Push(command2);
            _sut.Undo();

            CardModel cardC = new CardModel(Suit.Clubs, Rank.Three);
            cardC.IsFaceUp.Value = true;
            _board.Tableau[2].AddCard(cardC);
            MoveCommand command3 = BuildNormalMoveCommand(PileId.Waste(), PileId.Tableau(2), 1, 5, wasCardFlipped: false);
            _sut.Push(command3);
            _sut.Undo();

            Assert.That(_sut.CanUndo, Is.False);
            Assert.That(_board.Tableau[0].Count, Is.EqualTo(0));
            Assert.That(_board.Tableau[1].Count, Is.EqualTo(0));
            Assert.That(_board.Tableau[2].Count, Is.EqualTo(0));
        }

        [Test]
        public void MultiStepUndo_Push3UndoAll_StackIsEmptyAfterThirdUndo()
        {
            for (int moveIndex = 0; moveIndex < 3; moveIndex++)
            {
                CardModel card = new CardModel(Suit.Hearts, (Rank)(moveIndex + 1));
                card.IsFaceUp.Value = true;
                _board.Tableau[moveIndex].AddCard(card);
                MoveCommand command = BuildNormalMoveCommand(PileId.Waste(), PileId.Tableau(moveIndex), 1, 5, wasCardFlipped: false);
                _sut.Push(command);
            }

            _sut.Undo();
            _sut.Undo();
            _sut.Undo();

            Assert.That(_sut.CanUndo, Is.False);
        }

        [Test]
        public void MultiStepUndo_Push3UndoAll_PublishesBoardStateChangedMessageThreeTimes()
        {
            for (int moveIndex = 0; moveIndex < 3; moveIndex++)
            {
                CardModel card = new CardModel(Suit.Hearts, (Rank)(moveIndex + 1));
                card.IsFaceUp.Value = true;
                _board.Tableau[moveIndex].AddCard(card);
                MoveCommand command = BuildNormalMoveCommand(PileId.Waste(), PileId.Tableau(moveIndex), 1, 5, wasCardFlipped: false);
                _sut.Push(command);
            }

            _sut.Undo();
            _sut.Undo();
            _sut.Undo();

            Assert.That(_boardStatePublisher.MessageCount, Is.EqualTo(3));
        }

        [Test]
        public void MultiStepUndo_UndoAfterPush_CanUndoRemainsTrue()
        {
            for (int moveIndex = 0; moveIndex < 3; moveIndex++)
            {
                CardModel card = new CardModel(Suit.Hearts, (Rank)(moveIndex + 1));
                card.IsFaceUp.Value = true;
                _board.Tableau[moveIndex].AddCard(card);
                MoveCommand command = BuildNormalMoveCommand(PileId.Waste(), PileId.Tableau(moveIndex), 1, 5, wasCardFlipped: false);
                _sut.Push(command);
            }

            _sut.Undo();

            Assert.That(_sut.CanUndo, Is.True);
        }

        [Test]
        public void MultiStepUndo_UndoTwice_CanUndoRemainsTrue()
        {
            for (int moveIndex = 0; moveIndex < 3; moveIndex++)
            {
                CardModel card = new CardModel(Suit.Hearts, (Rank)(moveIndex + 1));
                card.IsFaceUp.Value = true;
                _board.Tableau[moveIndex].AddCard(card);
                MoveCommand command = BuildNormalMoveCommand(PileId.Waste(), PileId.Tableau(moveIndex), 1, 5, wasCardFlipped: false);
                _sut.Push(command);
            }

            _sut.Undo();
            _sut.Undo();

            Assert.That(_sut.CanUndo, Is.True);
        }

        // --- Private helpers ---

        private static MoveCommand BuildNormalMoveCommand(
            PileId source,
            PileId destination,
            int cardCount,
            int scoreDelta,
            bool wasCardFlipped)
        {
            return new MoveCommand(
                MoveType.WasteToTableau,
                source,
                destination,
                cardCount,
                scoreDelta,
                wasCardFlipped);
        }

        private static MoveCommand BuildDrawFromStockCommand()
        {
            return new MoveCommand(
                MoveType.DrawFromStock,
                PileId.Stock(),
                PileId.Waste(),
                cardCount: 1,
                scoreDelta: 0,
                wasCardFlipped: false);
        }

        private static MoveCommand BuildRecycleWasteCommand()
        {
            return new MoveCommand(
                MoveType.RecycleWaste,
                PileId.Waste(),
                PileId.Stock(),
                cardCount: 0,
                scoreDelta: 0,
                wasCardFlipped: false);
        }
    }
}
