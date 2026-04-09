using KlondikeSolitaire.Core;
using KlondikeSolitaire.Systems;
using NUnit.Framework;

namespace KlondikeSolitaire.Tests
{
    public sealed class MoveValidationSystemTests
    {
        // ---------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------

        private static CardModel FaceUp(Suit suit, Rank rank)
        {
            CardModel card = new CardModel(suit, rank);
            card.IsFaceUp.Value = true;
            return card;
        }

        private static CardModel FaceDown(Suit suit, Rank rank)
        {
            return new CardModel(suit, rank); // IsFaceUp defaults to false
        }

        // ---------------------------------------------------------------------------
        // Tableau → Tableau
        // ---------------------------------------------------------------------------

        [Test]
        public void IsValidMove_TableauToTableau_RedOnBlackDescending_ReturnsTrue()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[0].AddCard(FaceUp(Suit.Hearts, Rank.Seven));   // red 7 to move
                b.Tableau[1].AddCard(FaceUp(Suit.Spades, Rank.Eight));   // black 8 target
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            bool result = sut.IsValidMove(board, PileId.Tableau(0), PileId.Tableau(1), 1);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsValidMove_TableauToTableau_BlackOnRedDescending_ReturnsTrue()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[0].AddCard(FaceUp(Suit.Clubs, Rank.Four));     // black 4 to move
                b.Tableau[1].AddCard(FaceUp(Suit.Hearts, Rank.Five));    // red 5 target
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            bool result = sut.IsValidMove(board, PileId.Tableau(0), PileId.Tableau(1), 1);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsValidMove_TableauToTableau_SameColorRedOnRed_ReturnsFalse()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[0].AddCard(FaceUp(Suit.Hearts, Rank.Seven));   // red 7 to move
                b.Tableau[1].AddCard(FaceUp(Suit.Diamonds, Rank.Eight)); // red 8 target
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            bool result = sut.IsValidMove(board, PileId.Tableau(0), PileId.Tableau(1), 1);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsValidMove_TableauToTableau_SameColorBlackOnBlack_ReturnsFalse()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[0].AddCard(FaceUp(Suit.Clubs, Rank.Seven));
                b.Tableau[1].AddCard(FaceUp(Suit.Spades, Rank.Eight));
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            bool result = sut.IsValidMove(board, PileId.Tableau(0), PileId.Tableau(1), 1);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsValidMove_TableauToTableau_WrongRankNotDescendingByOne_ReturnsFalse()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[0].AddCard(FaceUp(Suit.Hearts, Rank.Six));     // red 6 to move
                b.Tableau[1].AddCard(FaceUp(Suit.Spades, Rank.Eight));   // black 8 target (gap of 2)
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            bool result = sut.IsValidMove(board, PileId.Tableau(0), PileId.Tableau(1), 1);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsValidMove_TableauToTableau_KingOnEmptyColumn_ReturnsTrue()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[0].AddCard(FaceUp(Suit.Hearts, Rank.King));    // red King to move
                // Tableau[1] is empty
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            bool result = sut.IsValidMove(board, PileId.Tableau(0), PileId.Tableau(1), 1);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsValidMove_TableauToTableau_NonKingOnEmptyColumn_ReturnsFalse()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[0].AddCard(FaceUp(Suit.Hearts, Rank.Queen));   // red Queen to move
                // Tableau[1] is empty
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            bool result = sut.IsValidMove(board, PileId.Tableau(0), PileId.Tableau(1), 1);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsValidMove_TableauToTableau_ValidSequenceSameColorDest_ReturnsFalse()
        {
            // Arrange — black 8, red 7 is a valid internal sequence but dest is red 9 (same color as black 8? no)
            // Actually black 8 onto red 9: black != red → alternating ✓, but 8 != 9-1=8 ✓
            // Wait — black 8 on red 9 IS valid. Use same-color destination to make it invalid.
            // black 8 (bottom of seq) onto BLACK 9 = same color → invalid
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[0].AddCard(FaceDown(Suit.Clubs, Rank.Nine));   // face-down base (not part of move)
                b.Tableau[0].AddCard(FaceUp(Suit.Spades, Rank.Eight));   // black 8
                b.Tableau[0].AddCard(FaceUp(Suit.Hearts, Rank.Seven));   // red 7

                // Same-color destination: black 9 → black 8 on black 9 = same color
                b.Tableau[1].AddCard(FaceUp(Suit.Clubs, Rank.Nine));     // black 9
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            bool result = sut.IsValidMove(board, PileId.Tableau(0), PileId.Tableau(1), 2);

            // Assert — bottom of sequence is black 8, dest top is black 9 → same color → false
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsValidMove_TableauToTableau_ValidMultiCardSequenceOntoAlternatingColor_ReturnsTrue()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                // Tableau[0]: face-up black 8, red 7 (valid sequence)
                b.Tableau[0].AddCard(FaceUp(Suit.Spades, Rank.Eight));   // black 8
                b.Tableau[0].AddCard(FaceUp(Suit.Hearts, Rank.Seven));   // red 7

                // Tableau[1]: black 9 target — black 8 can go onto black 9? No, same color
                // Need red 9 target for black 8? No, black 8 onto red 9 → valid alternating
                // The bottom card being moved is black 8; dest top = red 9 → valid
                b.Tableau[1].AddCard(FaceUp(Suit.Hearts, Rank.Nine));    // red 9
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act — move 2 cards (black 8, red 7) onto red 9 → black 8 on red 9 = alternating, rank 8 = 9-1 ✓
            bool result = sut.IsValidMove(board, PileId.Tableau(0), PileId.Tableau(1), 2);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsValidMove_TableauToTableau_MultiCardBrokenSequence_ReturnsFalse()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                // Tableau[0]: black 8, then black 7 (same color — broken sequence)
                b.Tableau[0].AddCard(FaceUp(Suit.Spades, Rank.Eight));   // black 8
                b.Tableau[0].AddCard(FaceUp(Suit.Clubs, Rank.Seven));    // black 7 (same color — invalid)

                b.Tableau[1].AddCard(FaceUp(Suit.Hearts, Rank.Nine));    // red 9
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            bool result = sut.IsValidMove(board, PileId.Tableau(0), PileId.Tableau(1), 2);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsValidMove_TableauToTableau_FaceDownCardAtBottom_ReturnsFalse()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                // Tableau[0]: face-down 8 (move it? can't)
                b.Tableau[0].AddCard(FaceDown(Suit.Spades, Rank.Eight));  // face-down

                b.Tableau[1].AddCard(FaceUp(Suit.Hearts, Rank.Nine));
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            bool result = sut.IsValidMove(board, PileId.Tableau(0), PileId.Tableau(1), 1);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsValidMove_TableauToTableau_CardCountExceedsPileCount_ReturnsFalse()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[0].AddCard(FaceUp(Suit.Spades, Rank.Eight));
                b.Tableau[1].AddCard(FaceUp(Suit.Hearts, Rank.Nine));
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act — try to move 3 cards when only 1 is available
            bool result = sut.IsValidMove(board, PileId.Tableau(0), PileId.Tableau(1), 3);

            // Assert
            Assert.That(result, Is.False);
        }

        // ---------------------------------------------------------------------------
        // Tableau → Foundation
        // ---------------------------------------------------------------------------

        [Test]
        public void IsValidMove_TableauToFoundation_AceOnEmptyFoundation_ReturnsTrue()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[0].AddCard(FaceUp(Suit.Hearts, Rank.Ace));
                // Foundation[0] is empty
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            bool result = sut.IsValidMove(board, PileId.Tableau(0), PileId.Foundation(0), 1);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsValidMove_TableauToFoundation_NonAceOnEmptyFoundation_ReturnsFalse()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[0].AddCard(FaceUp(Suit.Hearts, Rank.Two));
                // Foundation[0] is empty
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            bool result = sut.IsValidMove(board, PileId.Tableau(0), PileId.Foundation(0), 1);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsValidMove_TableauToFoundation_CorrectSuitAndRankPlusOne_ReturnsTrue()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[0].AddCard(FaceUp(Suit.Hearts, Rank.Three));

                // Foundation has Ace + Two of Hearts
                b.Foundations[0].AddCard(FaceUp(Suit.Hearts, Rank.Ace));
                b.Foundations[0].AddCard(FaceUp(Suit.Hearts, Rank.Two));
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            bool result = sut.IsValidMove(board, PileId.Tableau(0), PileId.Foundation(0), 1);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsValidMove_TableauToFoundation_WrongSuit_ReturnsFalse()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[0].AddCard(FaceUp(Suit.Spades, Rank.Two));     // black 2

                // Foundation has Ace of Hearts (red)
                b.Foundations[0].AddCard(FaceUp(Suit.Hearts, Rank.Ace));
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            bool result = sut.IsValidMove(board, PileId.Tableau(0), PileId.Foundation(0), 1);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsValidMove_TableauToFoundation_WrongRank_ReturnsFalse()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[0].AddCard(FaceUp(Suit.Hearts, Rank.Four));    // rank 4, skips Three

                b.Foundations[0].AddCard(FaceUp(Suit.Hearts, Rank.Ace));
                b.Foundations[0].AddCard(FaceUp(Suit.Hearts, Rank.Two));
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            bool result = sut.IsValidMove(board, PileId.Tableau(0), PileId.Foundation(0), 1);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsValidMove_TableauToFoundation_CardCountGreaterThanOne_ReturnsFalse()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[0].AddCard(FaceUp(Suit.Hearts, Rank.Ace));
                b.Tableau[0].AddCard(FaceUp(Suit.Diamonds, Rank.Ace));
                // Foundation[0] is empty
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act — attempt to move 2 cards to foundation
            bool result = sut.IsValidMove(board, PileId.Tableau(0), PileId.Foundation(0), 2);

            // Assert
            Assert.That(result, Is.False);
        }

        // ---------------------------------------------------------------------------
        // Waste → Tableau
        // ---------------------------------------------------------------------------

        [Test]
        public void IsValidMove_WasteToTableau_AlternatingColorDescending_ReturnsTrue()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Waste.AddCard(FaceUp(Suit.Hearts, Rank.Six));          // red 6 on waste
                b.Tableau[0].AddCard(FaceUp(Suit.Clubs, Rank.Seven));    // black 7 on tableau
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            bool result = sut.IsValidMove(board, PileId.Waste(), PileId.Tableau(0), 1);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsValidMove_WasteToTableau_SameColor_ReturnsFalse()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Waste.AddCard(FaceUp(Suit.Hearts, Rank.Six));          // red 6
                b.Tableau[0].AddCard(FaceUp(Suit.Diamonds, Rank.Seven)); // red 7
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            bool result = sut.IsValidMove(board, PileId.Waste(), PileId.Tableau(0), 1);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsValidMove_WasteToTableau_KingOnEmptyTableau_ReturnsTrue()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Waste.AddCard(FaceUp(Suit.Spades, Rank.King));         // black King
                // Tableau[0] is empty
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            bool result = sut.IsValidMove(board, PileId.Waste(), PileId.Tableau(0), 1);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsValidMove_WasteToTableau_EmptyWaste_ReturnsFalse()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[0].AddCard(FaceUp(Suit.Clubs, Rank.Seven));
                // Waste is empty
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            bool result = sut.IsValidMove(board, PileId.Waste(), PileId.Tableau(0), 1);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsValidMove_WasteToTableau_NonKingOnEmptyTableau_ReturnsFalse()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Waste.AddCard(FaceUp(Suit.Hearts, Rank.Jack));         // Jack, not King
                // Tableau[0] is empty
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            bool result = sut.IsValidMove(board, PileId.Waste(), PileId.Tableau(0), 1);

            // Assert
            Assert.That(result, Is.False);
        }

        // ---------------------------------------------------------------------------
        // Waste → Foundation
        // ---------------------------------------------------------------------------

        [Test]
        public void IsValidMove_WasteToFoundation_AceOnEmptyFoundation_ReturnsTrue()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Waste.AddCard(FaceUp(Suit.Spades, Rank.Ace));
                // Foundation[2] is empty
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            bool result = sut.IsValidMove(board, PileId.Waste(), PileId.Foundation(2), 1);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsValidMove_WasteToFoundation_CorrectSuitAndSequentialRank_ReturnsTrue()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Waste.AddCard(FaceUp(Suit.Clubs, Rank.Three));

                b.Foundations[1].AddCard(FaceUp(Suit.Clubs, Rank.Ace));
                b.Foundations[1].AddCard(FaceUp(Suit.Clubs, Rank.Two));
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            bool result = sut.IsValidMove(board, PileId.Waste(), PileId.Foundation(1), 1);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsValidMove_WasteToFoundation_WrongSuit_ReturnsFalse()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Waste.AddCard(FaceUp(Suit.Diamonds, Rank.Two));        // Diamonds 2

                b.Foundations[0].AddCard(FaceUp(Suit.Hearts, Rank.Ace)); // Hearts foundation
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            bool result = sut.IsValidMove(board, PileId.Waste(), PileId.Foundation(0), 1);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsValidMove_WasteToFoundation_WrongRank_ReturnsFalse()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Waste.AddCard(FaceUp(Suit.Hearts, Rank.Three));        // Three but only Ace on foundation

                b.Foundations[0].AddCard(FaceUp(Suit.Hearts, Rank.Ace));
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            bool result = sut.IsValidMove(board, PileId.Waste(), PileId.Foundation(0), 1);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsValidMove_WasteToFoundation_EmptyWaste_ReturnsFalse()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                // Waste is empty, Foundation[0] is empty
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            bool result = sut.IsValidMove(board, PileId.Waste(), PileId.Foundation(0), 1);

            // Assert
            Assert.That(result, Is.False);
        }

        // ---------------------------------------------------------------------------
        // Foundation → Tableau
        // ---------------------------------------------------------------------------

        [Test]
        public void IsValidMove_FoundationToTableau_AlternatingColorDescending_ReturnsTrue()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Foundations[0].AddCard(FaceUp(Suit.Hearts, Rank.Ace));
                b.Foundations[0].AddCard(FaceUp(Suit.Hearts, Rank.Two)); // red 2 top

                b.Tableau[0].AddCard(FaceUp(Suit.Clubs, Rank.Three));    // black 3 target
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            bool result = sut.IsValidMove(board, PileId.Foundation(0), PileId.Tableau(0), 1);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsValidMove_FoundationToTableau_WrongColor_ReturnsFalse()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Foundations[0].AddCard(FaceUp(Suit.Hearts, Rank.Ace));
                b.Foundations[0].AddCard(FaceUp(Suit.Hearts, Rank.Two)); // red 2 top

                b.Tableau[0].AddCard(FaceUp(Suit.Diamonds, Rank.Three)); // red 3 — same color
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            bool result = sut.IsValidMove(board, PileId.Foundation(0), PileId.Tableau(0), 1);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsValidMove_FoundationToTableau_WrongRank_ReturnsFalse()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Foundations[0].AddCard(FaceUp(Suit.Hearts, Rank.Ace));
                b.Foundations[0].AddCard(FaceUp(Suit.Hearts, Rank.Two)); // red 2 top

                b.Tableau[0].AddCard(FaceUp(Suit.Clubs, Rank.Five));     // black 5 — rank gap
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            bool result = sut.IsValidMove(board, PileId.Foundation(0), PileId.Tableau(0), 1);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsValidMove_FoundationToTableau_KingOnEmptyTableau_ReturnsTrue()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                // Foundation with full Hearts up to King
                for (int rankValue = 1; rankValue <= 13; rankValue++)
                {
                    b.Foundations[0].AddCard(FaceUp(Suit.Hearts, (Rank)rankValue));
                }
                // Tableau[0] is empty
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act — move King of Hearts from foundation to empty tableau
            bool result = sut.IsValidMove(board, PileId.Foundation(0), PileId.Tableau(0), 1);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsValidMove_FoundationToTableau_EmptyFoundation_ReturnsFalse()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[0].AddCard(FaceUp(Suit.Clubs, Rank.Three));
                // Foundation[0] is empty
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            bool result = sut.IsValidMove(board, PileId.Foundation(0), PileId.Tableau(0), 1);

            // Assert
            Assert.That(result, Is.False);
        }

        // ---------------------------------------------------------------------------
        // Edge cases — general validation
        // ---------------------------------------------------------------------------

        [Test]
        public void IsValidMove_ZeroCardCount_ReturnsFalse()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[0].AddCard(FaceUp(Suit.Hearts, Rank.Seven));
                b.Tableau[1].AddCard(FaceUp(Suit.Spades, Rank.Eight));
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            bool result = sut.IsValidMove(board, PileId.Tableau(0), PileId.Tableau(1), 0);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsValidMove_NegativeCardCount_ReturnsFalse()
        {
            // Arrange
            BoardModel board = TestBoardFactory.EmptyBoard();
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            bool result = sut.IsValidMove(board, PileId.Tableau(0), PileId.Tableau(1), -1);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsValidMove_MoveFromEmptyTableau_ReturnsFalse()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[1].AddCard(FaceUp(Suit.Spades, Rank.Eight));
                // Tableau[0] is empty
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            bool result = sut.IsValidMove(board, PileId.Tableau(0), PileId.Tableau(1), 1);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsValidMove_StockToTableauIsUnsupportedMoveType_ReturnsFalse()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Stock.AddCard(FaceUp(Suit.Hearts, Rank.Seven));
                b.Tableau[0].AddCard(FaceUp(Suit.Spades, Rank.Eight));
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            bool result = sut.IsValidMove(board, PileId.Stock(), PileId.Tableau(0), 1);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsValidMove_TableauToWasteIsUnsupportedMoveType_ReturnsFalse()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[0].AddCard(FaceUp(Suit.Hearts, Rank.Seven));
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            bool result = sut.IsValidMove(board, PileId.Tableau(0), PileId.Waste(), 1);

            // Assert
            Assert.That(result, Is.False);
        }

        // ---------------------------------------------------------------------------
        // FindBestTarget — foundation preferred over tableau when single card
        // ---------------------------------------------------------------------------

        [Test]
        public void FindBestTarget_SingleCardWithValidFoundationAndTableau_PrefersFoundation()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Waste.AddCard(FaceUp(Suit.Hearts, Rank.Ace));
                // Foundation[0] is empty → Ace can go there
                // Tableau[0] also empty → King can go there, but this is an Ace so no
                // Add a tableau option: black 2 in tableau to place on
                // Actually an Ace can only go to a foundation, so just confirm foundation is picked
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            PileId? target = sut.FindBestTarget(board, PileId.Waste(), 1);

            // Assert
            Assert.That(target.HasValue, Is.True);
            Assert.That(target.Value.Type, Is.EqualTo(PileType.Foundation));
        }

        [Test]
        public void FindBestTarget_SingleCardValidForBothFoundationAndTableau_ReturnsFoundation()
        {
            // Arrange — red Two on waste, Hearts Ace on Foundation[0], black Three in Tableau[1]
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Waste.AddCard(FaceUp(Suit.Hearts, Rank.Two));

                b.Foundations[0].AddCard(FaceUp(Suit.Hearts, Rank.Ace)); // valid foundation target

                b.Tableau[1].AddCard(FaceUp(Suit.Spades, Rank.Three));   // valid tableau target
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            PileId? target = sut.FindBestTarget(board, PileId.Waste(), 1);

            // Assert — foundation wins
            Assert.That(target.HasValue, Is.True);
            Assert.That(target.Value.Type, Is.EqualTo(PileType.Foundation));
            Assert.That(target.Value.Index, Is.EqualTo(0));
        }

        [Test]
        public void FindBestTarget_SingleCardNoValidFoundation_ReturnsLeftmostValidTableau()
        {
            // Arrange — red King on waste, no valid foundation, empty tableau columns
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Waste.AddCard(FaceUp(Suit.Hearts, Rank.King));
                // Foundations all empty — King can't go there
                // Tableau[0] through [6] are all empty — King can go to any
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            PileId? target = sut.FindBestTarget(board, PileId.Waste(), 1);

            // Assert — leftmost tableau (index 0) is returned
            Assert.That(target.HasValue, Is.True);
            Assert.That(target.Value.Type, Is.EqualTo(PileType.Tableau));
            Assert.That(target.Value.Index, Is.EqualTo(0));
        }

        [Test]
        public void FindBestTarget_NoValidTarget_ReturnsNull()
        {
            // Arrange — black Seven in waste, every tableau column has a black card on top
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Waste.AddCard(FaceUp(Suit.Spades, Rank.Seven));

                // All 7 tableau columns have a black Eight on top (same color → invalid)
                // or some other pile that won't accept this card
                for (int columnIndex = 0; columnIndex < 7; columnIndex++)
                {
                    b.Tableau[columnIndex].AddCard(FaceUp(Suit.Clubs, Rank.Eight)); // black 8 → same color
                }
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            PileId? target = sut.FindBestTarget(board, PileId.Waste(), 1);

            // Assert
            Assert.That(target.HasValue, Is.False);
        }

        [Test]
        public void FindBestTarget_TableauSource_SkipsSelfInSearch()
        {
            // Arrange — Tableau[2] has red 7, all other tableau columns have red 8 (invalid for red 7)
            // except Tableau[2] itself which should be skipped
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[2].AddCard(FaceUp(Suit.Hearts, Rank.Seven));   // source

                // All other columns also have red 8 → same color, won't accept red 7
                for (int columnIndex = 0; columnIndex < 7; columnIndex++)
                {
                    if (columnIndex != 2)
                    {
                        b.Tableau[columnIndex].AddCard(FaceUp(Suit.Diamonds, Rank.Eight)); // red 8
                    }
                }
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            PileId? target = sut.FindBestTarget(board, PileId.Tableau(2), 1);

            // Assert — Tableau[2] itself not returned; no other valid column → null
            Assert.That(target.HasValue, Is.False);
        }

        [Test]
        public void FindBestTarget_MultiCardMove_DoesNotCheckFoundations()
        {
            // Arrange — Tableau[0] has black 8, red 7 (2-card sequence); Tableau[1] has red 9
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[0].AddCard(FaceUp(Suit.Spades, Rank.Eight));
                b.Tableau[0].AddCard(FaceUp(Suit.Hearts, Rank.Seven));

                b.Tableau[1].AddCard(FaceUp(Suit.Hearts, Rank.Nine));    // red 9 — black 8 can go here
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act — cardCount=2, foundation check is skipped
            PileId? target = sut.FindBestTarget(board, PileId.Tableau(0), 2);

            // Assert — should find Tableau[1] (not a Foundation)
            Assert.That(target.HasValue, Is.True);
            Assert.That(target.Value.Type, Is.EqualTo(PileType.Tableau));
            Assert.That(target.Value.Index, Is.EqualTo(1));
        }

        [Test]
        public void FindBestTarget_EmptySourcePile_ReturnsNull()
        {
            // Arrange
            BoardModel board = TestBoardFactory.EmptyBoard();
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            PileId? target = sut.FindBestTarget(board, PileId.Tableau(0), 1);

            // Assert
            Assert.That(target.HasValue, Is.False);
        }

        [Test]
        public void FindBestTarget_CardCountExceedsPileSize_ReturnsNull()
        {
            // Arrange — pile has only 1 card but we ask for 3
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[0].AddCard(FaceUp(Suit.Spades, Rank.Eight));
                b.Tableau[1].AddCard(FaceUp(Suit.Hearts, Rank.Nine));
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            PileId? target = sut.FindBestTarget(board, PileId.Tableau(0), 3);

            // Assert
            Assert.That(target.HasValue, Is.False);
        }

        // ---------------------------------------------------------------------------
        // Edge cases — full foundation / complex board states
        // ---------------------------------------------------------------------------

        [Test]
        public void IsValidMove_TableauToFoundation_FullFoundationRejected_ReturnsFalse()
        {
            // Arrange — Foundation already has all 13 cards; nothing more can be added
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                for (int rankValue = 1; rankValue <= 13; rankValue++)
                {
                    b.Foundations[0].AddCard(FaceUp(Suit.Hearts, (Rank)rankValue));
                }
                // Attempting to add another Hearts card (would be rank 14 — impossible)
                // Simulate with an Ace from a fresh pile placed on a full foundation
                b.Tableau[0].AddCard(FaceUp(Suit.Hearts, Rank.Ace));
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act — Ace (value=1) != King top (value=13) + 1 = 14 → false
            bool result = sut.IsValidMove(board, PileId.Tableau(0), PileId.Foundation(0), 1);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsValidMove_WasteToFoundation_NonAceOnEmptyFoundation_ReturnsFalse()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Waste.AddCard(FaceUp(Suit.Clubs, Rank.King));
                // Foundation[3] empty
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act
            bool result = sut.IsValidMove(board, PileId.Waste(), PileId.Foundation(3), 1);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsValidMove_TableauToTableau_FaceDownCardInSequenceMiddle_ReturnsFalse()
        {
            // Arrange — black 8 (face-up), FACE-DOWN red 7, black 6 (face-up) — broken sequence
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[0].AddCard(FaceUp(Suit.Spades, Rank.Eight));
                b.Tableau[0].AddCard(FaceDown(Suit.Hearts, Rank.Seven)); // face-down middle
                b.Tableau[0].AddCard(FaceUp(Suit.Clubs, Rank.Six));

                b.Tableau[1].AddCard(FaceUp(Suit.Hearts, Rank.Nine));
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act — try to move 3 cards including the face-down one
            bool result = sut.IsValidMove(board, PileId.Tableau(0), PileId.Tableau(1), 3);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsValidMove_TableauToTableau_SameSourceAndDest_ReturnsFalse()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[0].AddCard(FaceUp(Suit.Hearts, Rank.Seven));
            });
            MoveValidationSystem sut = new MoveValidationSystem();

            // Act — moving onto itself → non-King onto non-empty pile with same card
            // source top = red 7, dest top = red 7 (same) → same color → false
            bool result = sut.IsValidMove(board, PileId.Tableau(0), PileId.Tableau(0), 1);

            // Assert — same color (red on red) fails
            Assert.That(result, Is.False);
        }
    }
}
