using KlondikeSolitaire.Core;
using KlondikeSolitaire.Systems;
using NUnit.Framework;

namespace KlondikeSolitaire.Tests
{
    [TestFixture]
    public sealed class MoveExecutionSystemTests
    {
        private BoardModel _board;
        private ScoreModel _scoreModel;
        private ScoringSystem _scoringSystem;
        private UndoSystem _undoSystem;
        private TestPublisher<CardMovedMessage> _cardMovedPublisher;
        private TestPublisher<CardFlippedMessage> _cardFlippedPublisher;
        private TestPublisher<BoardStateChangedMessage> _boardStatePublisher;
        private TestPublisher<ScoreChangedMessage> _scoreChangedPublisher;
        private TestPublisher<UndoAvailabilityChangedMessage> _undoAvailabilityPublisher;
        private TestPublisher<CardFlippedMessage> _undoCardFlippedPublisher;
        private MoveExecutionSystem _sut;

        [SetUp]
        public void SetUp()
        {
            _board = TestBoardFactory.EmptyBoard();
            _scoreModel = new ScoreModel();
            _scoreChangedPublisher = new TestPublisher<ScoreChangedMessage>();
            _scoringSystem = new ScoringSystem(_scoreModel, new ScoringTable(5, 10, 10, -15, 5), _scoreChangedPublisher);

            _undoAvailabilityPublisher = new TestPublisher<UndoAvailabilityChangedMessage>();
            _boardStatePublisher = new TestPublisher<BoardStateChangedMessage>();
            _undoCardFlippedPublisher = new TestPublisher<CardFlippedMessage>();

            var undoCardMovedPublisher = new TestPublisher<CardMovedMessage>();
            _undoSystem = new UndoSystem(
                _board,
                _scoringSystem,
                _undoAvailabilityPublisher,
                _boardStatePublisher,
                _undoCardFlippedPublisher,
                undoCardMovedPublisher);

            _cardMovedPublisher = new TestPublisher<CardMovedMessage>();
            _cardFlippedPublisher = new TestPublisher<CardFlippedMessage>();

            _sut = new MoveExecutionSystem(
                _board,
                _scoringSystem,
                _undoSystem,
                _cardMovedPublisher,
                _cardFlippedPublisher,
                _boardStatePublisher);
        }

        [TearDown]
        public void TearDown()
        {
        }

        // --- ExecuteMove: cards transferred correctly ---

        [Test]
        public void ExecuteMove_WasteToTableau_TransferredCardIsOriginalCard()
        {
            CardModel card = new CardModel(Suit.Hearts, Rank.Ace);
            card.IsFaceUp.Value = true;
            _board.Waste.AddCard(card);

            _sut.ExecuteMove(PileId.Waste(), PileId.Tableau(0), 1);

            Assert.That(_board.Tableau[0].TopCard, Is.SameAs(card));
        }

        [Test]
        public void ExecuteMove_TableauToTableauMultipleCards_TransfersAllCards()
        {
            CardModel kingCard = new CardModel(Suit.Hearts, Rank.King);
            CardModel queenCard = new CardModel(Suit.Spades, Rank.Queen);
            kingCard.IsFaceUp.Value = true;
            queenCard.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(kingCard);
            _board.Tableau[0].AddCard(queenCard);

            _sut.ExecuteMove(PileId.Tableau(0), PileId.Tableau(1), 2);

            Assert.That(_board.Tableau[1].Count, Is.EqualTo(2));
        }

        [Test]
        public void ExecuteMove_TableauToTableauMultipleCards_OrderPreservedInDestination()
        {
            CardModel kingCard = new CardModel(Suit.Hearts, Rank.King);
            CardModel queenCard = new CardModel(Suit.Spades, Rank.Queen);
            kingCard.IsFaceUp.Value = true;
            queenCard.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(kingCard);
            _board.Tableau[0].AddCard(queenCard);

            _sut.ExecuteMove(PileId.Tableau(0), PileId.Tableau(1), 2);

            Assert.That(_board.Tableau[1].Cards[0], Is.SameAs(kingCard));
            Assert.That(_board.Tableau[1].Cards[1], Is.SameAs(queenCard));
        }

        // --- ExecuteMove: auto-flip on tableau source ---

        [Test]
        public void ExecuteMove_TableauSourceHasFaceDownTopAfterMove_FlipsNewTopCard()
        {
            CardModel faceDownCard = new CardModel(Suit.Clubs, Rank.Five);
            CardModel faceUpCard = new CardModel(Suit.Hearts, Rank.Six);
            faceDownCard.IsFaceUp.Value = false;
            faceUpCard.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(faceDownCard);
            _board.Tableau[0].AddCard(faceUpCard);

            _sut.ExecuteMove(PileId.Tableau(0), PileId.Tableau(1), 1);

            Assert.That(_board.Tableau[0].TopCard.IsFaceUp.Value, Is.True);
        }

        [Test]
        public void ExecuteMove_TableauSourceHasFaceDownTopAfterMove_PublishesCardFlippedMessage()
        {
            CardModel faceDownCard = new CardModel(Suit.Clubs, Rank.Five);
            CardModel faceUpCard = new CardModel(Suit.Hearts, Rank.Six);
            faceDownCard.IsFaceUp.Value = false;
            faceUpCard.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(faceDownCard);
            _board.Tableau[0].AddCard(faceUpCard);

            _sut.ExecuteMove(PileId.Tableau(0), PileId.Tableau(1), 1);

            Assert.That(_cardFlippedPublisher.MessageCount, Is.EqualTo(1));
        }

        [Test]
        public void ExecuteMove_TableauSourceHasFaceDownTopAfterMove_CardFlippedMessageHasCorrectPileId()
        {
            CardModel faceDownCard = new CardModel(Suit.Clubs, Rank.Five);
            CardModel faceUpCard = new CardModel(Suit.Hearts, Rank.Six);
            faceDownCard.IsFaceUp.Value = false;
            faceUpCard.IsFaceUp.Value = true;
            _board.Tableau[2].AddCard(faceDownCard);
            _board.Tableau[2].AddCard(faceUpCard);

            _sut.ExecuteMove(PileId.Tableau(2), PileId.Tableau(4), 1);

            Assert.That(_cardFlippedPublisher.LastMessage.PileId, Is.EqualTo(PileId.Tableau(2)));
        }

        [Test]
        public void ExecuteMove_TableauSourceBecomesEmpty_DoesNotPublishCardFlippedMessage()
        {
            CardModel faceUpCard = new CardModel(Suit.Hearts, Rank.King);
            faceUpCard.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(faceUpCard);

            _sut.ExecuteMove(PileId.Tableau(0), PileId.Tableau(1), 1);

            Assert.That(_cardFlippedPublisher.MessageCount, Is.EqualTo(0));
        }

        [Test]
        public void ExecuteMove_NonTableauSource_DoesNotPublishCardFlippedMessage()
        {
            CardModel card = new CardModel(Suit.Hearts, Rank.Ace);
            card.IsFaceUp.Value = true;
            _board.Waste.AddCard(card);

            _sut.ExecuteMove(PileId.Waste(), PileId.Tableau(0), 1);

            Assert.That(_cardFlippedPublisher.MessageCount, Is.EqualTo(0));
        }

        [Test]
        public void ExecuteMove_TableauSourceTopAlreadyFaceUp_DoesNotPublishCardFlippedMessage()
        {
            CardModel bottomCard = new CardModel(Suit.Clubs, Rank.Five);
            CardModel topCard = new CardModel(Suit.Hearts, Rank.Six);
            bottomCard.IsFaceUp.Value = true;
            topCard.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(bottomCard);
            _board.Tableau[0].AddCard(topCard);

            _sut.ExecuteMove(PileId.Tableau(0), PileId.Tableau(1), 1);

            Assert.That(_cardFlippedPublisher.MessageCount, Is.EqualTo(0));
        }

        // --- ExecuteMove: score delta calculated and applied ---

        [Test]
        public void ExecuteMove_WasteToTableau_AppliesCorrectScoreDelta()
        {
            CardModel card = new CardModel(Suit.Hearts, Rank.Ace);
            card.IsFaceUp.Value = true;
            _board.Waste.AddCard(card);

            _sut.ExecuteMove(PileId.Waste(), PileId.Tableau(0), 1);

            Assert.That(_scoreModel.Score.Value, Is.EqualTo(5));
        }

        [Test]
        public void ExecuteMove_WasteToFoundation_AppliesCorrectScoreDelta()
        {
            CardModel card = new CardModel(Suit.Hearts, Rank.Ace);
            card.IsFaceUp.Value = true;
            _board.Waste.AddCard(card);

            _sut.ExecuteMove(PileId.Waste(), PileId.Foundation(0), 1);

            Assert.That(_scoreModel.Score.Value, Is.EqualTo(10));
        }

        [Test]
        public void ExecuteMove_TableauToFoundation_AppliesCorrectScoreDelta()
        {
            CardModel card = new CardModel(Suit.Hearts, Rank.Ace);
            card.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(card);

            _sut.ExecuteMove(PileId.Tableau(0), PileId.Foundation(0), 1);

            Assert.That(_scoreModel.Score.Value, Is.EqualTo(10));
        }

        [Test]
        public void ExecuteMove_TableauToTableau_AppliesZeroScoreDelta()
        {
            CardModel kingCard = new CardModel(Suit.Hearts, Rank.King);
            kingCard.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(kingCard);

            _sut.ExecuteMove(PileId.Tableau(0), PileId.Tableau(1), 1);

            Assert.That(_scoreModel.Score.Value, Is.EqualTo(0));
        }

        [Test]
        public void ExecuteMove_FoundationToTableau_AppliesNegativeScoreDelta()
        {
            CardModel card = new CardModel(Suit.Hearts, Rank.Ace);
            card.IsFaceUp.Value = true;
            _board.Foundations[0].AddCard(card);
            _scoreModel.Score.Value = 20;

            _sut.ExecuteMove(PileId.Foundation(0), PileId.Tableau(0), 1);

            Assert.That(_scoreModel.Score.Value, Is.EqualTo(5));
        }

        [Test]
        public void ExecuteMove_WithAutoFlip_IncludesFlipCardScoreInDelta()
        {
            CardModel faceDownCard = new CardModel(Suit.Clubs, Rank.Five);
            CardModel faceUpCard = new CardModel(Suit.Hearts, Rank.Six);
            faceDownCard.IsFaceUp.Value = false;
            faceUpCard.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(faceDownCard);
            _board.Tableau[0].AddCard(faceUpCard);

            _sut.ExecuteMove(PileId.Tableau(0), PileId.Foundation(0), 1);

            // TableauToFoundation = 10, FlipCard = 5 => total 15
            Assert.That(_scoreModel.Score.Value, Is.EqualTo(15));
        }

        // --- ExecuteMove: undo command pushed with correct metadata ---

        [Test]
        public void ExecuteMove_Always_PushesUndoCommand()
        {
            CardModel card = new CardModel(Suit.Hearts, Rank.Ace);
            card.IsFaceUp.Value = true;
            _board.Waste.AddCard(card);

            _sut.ExecuteMove(PileId.Waste(), PileId.Tableau(0), 1);

            Assert.That(_undoSystem.CanUndo, Is.True);
        }

        [Test]
        public void ExecuteMove_WasteToTableau_UndoCommandHasCorrectSource()
        {
            CardModel card = new CardModel(Suit.Hearts, Rank.Ace);
            card.IsFaceUp.Value = true;
            _board.Waste.AddCard(card);

            _sut.ExecuteMove(PileId.Waste(), PileId.Tableau(3), 1);
            _undoSystem.Undo();

            // After undo the card should be back in waste — confirms source was correct
            Assert.That(_board.Waste.Count, Is.EqualTo(1));
        }

        [Test]
        public void ExecuteMove_WasteToTableau_UndoCommandHasCorrectDestination()
        {
            CardModel card = new CardModel(Suit.Hearts, Rank.Ace);
            card.IsFaceUp.Value = true;
            _board.Waste.AddCard(card);

            _sut.ExecuteMove(PileId.Waste(), PileId.Tableau(3), 1);
            _undoSystem.Undo();

            Assert.That(_board.Tableau[3].Count, Is.EqualTo(0));
        }

        [Test]
        public void ExecuteMove_WasteToTableau_UndoCommandHasCorrectCardCount()
        {
            CardModel card = new CardModel(Suit.Hearts, Rank.Ace);
            card.IsFaceUp.Value = true;
            _board.Waste.AddCard(card);
            // Place a second card at tableau[0] as a placeholder
            CardModel existingCard = new CardModel(Suit.Clubs, Rank.Two);
            existingCard.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(existingCard);

            _sut.ExecuteMove(PileId.Waste(), PileId.Tableau(0), 1);
            _undoSystem.Undo();

            // After undo, only the existing card should remain in tableau[0]
            Assert.That(_board.Tableau[0].Count, Is.EqualTo(1));
        }

        [Test]
        public void ExecuteMove_WithAutoFlip_UndoCommandHasWasCardFlippedTrue()
        {
            CardModel faceDownCard = new CardModel(Suit.Clubs, Rank.Five);
            CardModel faceUpCard = new CardModel(Suit.Hearts, Rank.Six);
            faceDownCard.IsFaceUp.Value = false;
            faceUpCard.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(faceDownCard);
            _board.Tableau[0].AddCard(faceUpCard);

            _sut.ExecuteMove(PileId.Tableau(0), PileId.Tableau(1), 1);
            _undoSystem.Undo();

            // After undo the auto-flipped card should be face down again
            Assert.That(faceDownCard.IsFaceUp.Value, Is.False);
        }

        [Test]
        public void ExecuteMove_WithoutAutoFlip_UndoDoesNotFlipSourceCard()
        {
            CardModel bottomCard = new CardModel(Suit.Clubs, Rank.Five);
            CardModel topCard = new CardModel(Suit.Hearts, Rank.Six);
            bottomCard.IsFaceUp.Value = true;
            topCard.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(bottomCard);
            _board.Tableau[0].AddCard(topCard);

            _sut.ExecuteMove(PileId.Tableau(0), PileId.Tableau(1), 1);
            _undoSystem.Undo();

            Assert.That(_board.Tableau[0].TopCard.IsFaceUp.Value, Is.True);
        }

        [Test]
        public void ExecuteMove_WasteToTableau_UndoRestoresScore()
        {
            CardModel card = new CardModel(Suit.Hearts, Rank.Ace);
            card.IsFaceUp.Value = true;
            _board.Waste.AddCard(card);

            _sut.ExecuteMove(PileId.Waste(), PileId.Tableau(0), 1);
            _undoSystem.Undo();

            Assert.That(_scoreModel.Score.Value, Is.EqualTo(0));
        }

        // --- ExecuteMove: messages published ---

        [Test]
        public void ExecuteMove_Always_CardMovedMessageHasCorrectSourcePileId()
        {
            CardModel card = new CardModel(Suit.Hearts, Rank.Ace);
            card.IsFaceUp.Value = true;
            _board.Waste.AddCard(card);

            _sut.ExecuteMove(PileId.Waste(), PileId.Tableau(0), 1);

            Assert.That(_cardMovedPublisher.LastMessage.SourcePileId, Is.EqualTo(PileId.Waste()));
        }

        [Test]
        public void ExecuteMove_Always_CardMovedMessageHasCorrectDestPileId()
        {
            CardModel card = new CardModel(Suit.Hearts, Rank.Ace);
            card.IsFaceUp.Value = true;
            _board.Waste.AddCard(card);

            _sut.ExecuteMove(PileId.Waste(), PileId.Tableau(2), 1);

            Assert.That(_cardMovedPublisher.LastMessage.DestPileId, Is.EqualTo(PileId.Tableau(2)));
        }

        [Test]
        public void ExecuteMove_Always_CardMovedMessageHasCorrectCardCount()
        {
            CardModel card1 = new CardModel(Suit.Hearts, Rank.King);
            CardModel card2 = new CardModel(Suit.Spades, Rank.Queen);
            card1.IsFaceUp.Value = true;
            card2.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(card1);
            _board.Tableau[0].AddCard(card2);

            _sut.ExecuteMove(PileId.Tableau(0), PileId.Tableau(1), 2);

            Assert.That(_cardMovedPublisher.LastMessage.CardCount, Is.EqualTo(2));
        }

        [Test]
        public void ExecuteMove_Always_PublishesBoardStateChangedMessage()
        {
            CardModel card = new CardModel(Suit.Hearts, Rank.Ace);
            card.IsFaceUp.Value = true;
            _board.Waste.AddCard(card);
            _boardStatePublisher.Clear();

            _sut.ExecuteMove(PileId.Waste(), PileId.Tableau(0), 1);

            Assert.That(_boardStatePublisher.MessageCount, Is.EqualTo(1));
        }

        // --- ExecuteMove: edge case — pile becomes empty, no auto-flip ---

        [Test]
        public void ExecuteMove_TableauSingleFaceUpCard_SourceBecomesEmptyAndNoFlip()
        {
            CardModel card = new CardModel(Suit.Hearts, Rank.King);
            card.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(card);

            _sut.ExecuteMove(PileId.Tableau(0), PileId.Tableau(1), 1);

            Assert.That(_board.Tableau[0].Count, Is.EqualTo(0));
            Assert.That(_cardFlippedPublisher.MessageCount, Is.EqualTo(0));
        }

        // --- DrawFromStock ---

        [Test]
        public void DrawFromStock_DrawnCardIsSameCard()
        {
            CardModel card = new CardModel(Suit.Diamonds, Rank.Ten);
            _board.Stock.AddCard(card);

            _sut.DrawFromStock();

            Assert.That(_board.Waste.TopCard, Is.SameAs(card));
        }

        [Test]
        public void DrawFromStock_DrawnCardIsFlippedFaceUp()
        {
            CardModel card = new CardModel(Suit.Diamonds, Rank.Ten);
            card.IsFaceUp.Value = false;
            _board.Stock.AddCard(card);

            _sut.DrawFromStock();

            Assert.That(_board.Waste.TopCard.IsFaceUp.Value, Is.True);
        }

        [Test]
        public void DrawFromStock_DrawsTopCardOfStock()
        {
            CardModel bottomCard = new CardModel(Suit.Clubs, Rank.Two);
            CardModel topCard = new CardModel(Suit.Diamonds, Rank.Ten);
            _board.Stock.AddCard(bottomCard);
            _board.Stock.AddCard(topCard);

            _sut.DrawFromStock();

            Assert.That(_board.Waste.TopCard, Is.SameAs(topCard));
        }

        [Test]
        public void DrawFromStock_CreatesUndoCommand()
        {
            CardModel card = new CardModel(Suit.Diamonds, Rank.Ten);
            _board.Stock.AddCard(card);

            _sut.DrawFromStock();

            Assert.That(_undoSystem.CanUndo, Is.True);
        }

        [Test]
        public void DrawFromStock_UndoMovesCardBackToStock()
        {
            CardModel card = new CardModel(Suit.Diamonds, Rank.Ten);
            _board.Stock.AddCard(card);

            _sut.DrawFromStock();
            _undoSystem.Undo();

            Assert.That(_board.Stock.Count, Is.EqualTo(1));
        }

        [Test]
        public void DrawFromStock_UndoCardInStockIsFaceDown()
        {
            CardModel card = new CardModel(Suit.Diamonds, Rank.Ten);
            _board.Stock.AddCard(card);

            _sut.DrawFromStock();
            _undoSystem.Undo();

            Assert.That(_board.Stock.TopCard.IsFaceUp.Value, Is.False);
        }

        [Test]
        public void DrawFromStock_CardMovedMessageHasStockAsSource()
        {
            CardModel card = new CardModel(Suit.Diamonds, Rank.Ten);
            _board.Stock.AddCard(card);

            _sut.DrawFromStock();

            Assert.That(_cardMovedPublisher.LastMessage.SourcePileId, Is.EqualTo(PileId.Stock()));
        }

        [Test]
        public void DrawFromStock_CardMovedMessageHasWasteAsDest()
        {
            CardModel card = new CardModel(Suit.Diamonds, Rank.Ten);
            _board.Stock.AddCard(card);

            _sut.DrawFromStock();

            Assert.That(_cardMovedPublisher.LastMessage.DestPileId, Is.EqualTo(PileId.Waste()));
        }

        [Test]
        public void DrawFromStock_CardMovedMessageHasCardCountOfOne()
        {
            CardModel card = new CardModel(Suit.Diamonds, Rank.Ten);
            _board.Stock.AddCard(card);

            _sut.DrawFromStock();

            Assert.That(_cardMovedPublisher.LastMessage.CardCount, Is.EqualTo(1));
        }

        [Test]
        public void DrawFromStock_PublishesBoardStateChangedMessage()
        {
            CardModel card = new CardModel(Suit.Diamonds, Rank.Ten);
            _board.Stock.AddCard(card);
            _boardStatePublisher.Clear();

            _sut.DrawFromStock();

            Assert.That(_boardStatePublisher.MessageCount, Is.EqualTo(1));
        }

        // --- RecycleWaste ---

        [Test]
        public void RecycleWaste_MovesAllCardsToStock()
        {
            CardModel card1 = new CardModel(Suit.Hearts, Rank.Two);
            CardModel card2 = new CardModel(Suit.Spades, Rank.Three);
            card1.IsFaceUp.Value = true;
            card2.IsFaceUp.Value = true;
            _board.Waste.AddCard(card1);
            _board.Waste.AddCard(card2);

            _sut.RecycleWaste();

            Assert.That(_board.Stock.Count, Is.EqualTo(2));
        }

        [Test]
        public void RecycleWaste_CardsInStockAreFaceDown()
        {
            CardModel card1 = new CardModel(Suit.Hearts, Rank.Two);
            CardModel card2 = new CardModel(Suit.Spades, Rank.Three);
            card1.IsFaceUp.Value = true;
            card2.IsFaceUp.Value = true;
            _board.Waste.AddCard(card1);
            _board.Waste.AddCard(card2);

            _sut.RecycleWaste();

            for (int cardIndex = 0; cardIndex < _board.Stock.Cards.Count; cardIndex++)
            {
                Assert.That(_board.Stock.Cards[cardIndex].IsFaceUp.Value, Is.False,
                    $"Stock card at index {cardIndex} should be face down after RecycleWaste");
            }
        }

        [Test]
        public void RecycleWaste_OrderIsReversed_WasteBottomBecomesStockTop()
        {
            CardModel firstCard = new CardModel(Suit.Hearts, Rank.Two);
            CardModel secondCard = new CardModel(Suit.Spades, Rank.Three);
            CardModel thirdCard = new CardModel(Suit.Clubs, Rank.Four);
            firstCard.IsFaceUp.Value = true;
            secondCard.IsFaceUp.Value = true;
            thirdCard.IsFaceUp.Value = true;
            _board.Waste.AddCard(firstCard);
            _board.Waste.AddCard(secondCard);
            _board.Waste.AddCard(thirdCard);

            _sut.RecycleWaste();

            // firstCard was at waste[0] (bottom), after reverse it's at stock top
            Assert.That(_board.Stock.TopCard, Is.SameAs(firstCard));
        }

        [Test]
        public void RecycleWaste_OrderIsReversed_WasteTopBecomesStockBottom()
        {
            CardModel firstCard = new CardModel(Suit.Hearts, Rank.Two);
            CardModel secondCard = new CardModel(Suit.Spades, Rank.Three);
            CardModel thirdCard = new CardModel(Suit.Clubs, Rank.Four);
            firstCard.IsFaceUp.Value = true;
            secondCard.IsFaceUp.Value = true;
            thirdCard.IsFaceUp.Value = true;
            _board.Waste.AddCard(firstCard);
            _board.Waste.AddCard(secondCard);
            _board.Waste.AddCard(thirdCard);

            _sut.RecycleWaste();

            // thirdCard was at waste top, after reverse it's at stock[0] (bottom)
            Assert.That(_board.Stock.Cards[0], Is.SameAs(thirdCard));
        }

        [Test]
        public void RecycleWaste_CreatesUndoCommand()
        {
            CardModel card = new CardModel(Suit.Hearts, Rank.Two);
            card.IsFaceUp.Value = true;
            _board.Waste.AddCard(card);

            _sut.RecycleWaste();

            Assert.That(_undoSystem.CanUndo, Is.True);
        }

        [Test]
        public void RecycleWaste_UndoRestoresAllCardsToWaste()
        {
            CardModel card1 = new CardModel(Suit.Hearts, Rank.Two);
            CardModel card2 = new CardModel(Suit.Spades, Rank.Three);
            card1.IsFaceUp.Value = true;
            card2.IsFaceUp.Value = true;
            _board.Waste.AddCard(card1);
            _board.Waste.AddCard(card2);

            _sut.RecycleWaste();
            _undoSystem.Undo();

            Assert.That(_board.Waste.Count, Is.EqualTo(2));
        }

        [Test]
        public void RecycleWaste_UndoClearsStock()
        {
            CardModel card1 = new CardModel(Suit.Hearts, Rank.Two);
            CardModel card2 = new CardModel(Suit.Spades, Rank.Three);
            card1.IsFaceUp.Value = true;
            card2.IsFaceUp.Value = true;
            _board.Waste.AddCard(card1);
            _board.Waste.AddCard(card2);

            _sut.RecycleWaste();
            _undoSystem.Undo();

            Assert.That(_board.Stock.Count, Is.EqualTo(0));
        }

        [Test]
        public void RecycleWaste_UndoCardsInWasteAreFaceUp()
        {
            CardModel card1 = new CardModel(Suit.Hearts, Rank.Two);
            CardModel card2 = new CardModel(Suit.Spades, Rank.Three);
            card1.IsFaceUp.Value = true;
            card2.IsFaceUp.Value = true;
            _board.Waste.AddCard(card1);
            _board.Waste.AddCard(card2);

            _sut.RecycleWaste();
            _undoSystem.Undo();

            for (int cardIndex = 0; cardIndex < _board.Waste.Cards.Count; cardIndex++)
            {
                Assert.That(_board.Waste.Cards[cardIndex].IsFaceUp.Value, Is.True,
                    $"Waste card at index {cardIndex} should be face up after undo RecycleWaste");
            }
        }

        [Test]
        public void RecycleWaste_UndoPreservesWasteOrder()
        {
            CardModel card1 = new CardModel(Suit.Hearts, Rank.Two);
            CardModel card2 = new CardModel(Suit.Spades, Rank.Three);
            CardModel card3 = new CardModel(Suit.Clubs, Rank.Four);
            card1.IsFaceUp.Value = true;
            card2.IsFaceUp.Value = true;
            card3.IsFaceUp.Value = true;
            _board.Waste.AddCard(card1);
            _board.Waste.AddCard(card2);
            _board.Waste.AddCard(card3);

            _sut.RecycleWaste();
            _undoSystem.Undo();

            Assert.That(_board.Waste.Cards[0], Is.SameAs(card1));
            Assert.That(_board.Waste.Cards[1], Is.SameAs(card2));
            Assert.That(_board.Waste.Cards[2], Is.SameAs(card3));
        }

        [Test]
        public void RecycleWaste_CardMovedMessageHasWasteAsSource()
        {
            CardModel card = new CardModel(Suit.Hearts, Rank.Two);
            card.IsFaceUp.Value = true;
            _board.Waste.AddCard(card);

            _sut.RecycleWaste();

            Assert.That(_cardMovedPublisher.LastMessage.SourcePileId, Is.EqualTo(PileId.Waste()));
        }

        [Test]
        public void RecycleWaste_CardMovedMessageHasStockAsDest()
        {
            CardModel card = new CardModel(Suit.Hearts, Rank.Two);
            card.IsFaceUp.Value = true;
            _board.Waste.AddCard(card);

            _sut.RecycleWaste();

            Assert.That(_cardMovedPublisher.LastMessage.DestPileId, Is.EqualTo(PileId.Stock()));
        }

        [Test]
        public void RecycleWaste_CardMovedMessageHasCorrectCardCount()
        {
            CardModel card1 = new CardModel(Suit.Hearts, Rank.Two);
            CardModel card2 = new CardModel(Suit.Spades, Rank.Three);
            card1.IsFaceUp.Value = true;
            card2.IsFaceUp.Value = true;
            _board.Waste.AddCard(card1);
            _board.Waste.AddCard(card2);

            _sut.RecycleWaste();

            Assert.That(_cardMovedPublisher.LastMessage.CardCount, Is.EqualTo(2));
        }

        [Test]
        public void RecycleWaste_PublishesBoardStateChangedMessage()
        {
            CardModel card = new CardModel(Suit.Hearts, Rank.Two);
            card.IsFaceUp.Value = true;
            _board.Waste.AddCard(card);
            _boardStatePublisher.Clear();

            _sut.RecycleWaste();

            Assert.That(_boardStatePublisher.MessageCount, Is.EqualTo(1));
        }

        // --- Multiple operations in sequence ---

        [Test]
        public void DrawFromStock_ThenExecuteMove_BothUndoCommandsStacked()
        {
            CardModel stockCard = new CardModel(Suit.Diamonds, Rank.Seven);
            _board.Stock.AddCard(stockCard);
            CardModel tableauCard = new CardModel(Suit.Hearts, Rank.Ace);
            tableauCard.IsFaceUp.Value = true;
            _board.Tableau[0].AddCard(tableauCard);

            _sut.DrawFromStock();
            _sut.ExecuteMove(PileId.Tableau(0), PileId.Foundation(0), 1);

            _undoSystem.Undo();
            Assert.That(_board.Tableau[0].Count, Is.EqualTo(1));

            _undoSystem.Undo();
            Assert.That(_board.Stock.Count, Is.EqualTo(1));
        }

    }
}
