using System.Collections.Generic;
using KlondikeSolitaire.Core;
using KlondikeSolitaire.Systems;
using NUnit.Framework;

namespace KlondikeSolitaire.Tests
{
    [TestFixture]
    public sealed class MoveEnumeratorTests
    {
        private MoveValidationSystem _validation;
        private List<Move> _results;

        [SetUp]
        public void SetUp()
        {
            _validation = new MoveValidationSystem();
            _results = new List<Move>();
        }

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
            return new CardModel(suit, rank);
        }

        private static bool ContainsMove(List<Move> moves, PileId source, PileId dest, int cardCount)
        {
            for (int moveIndex = 0; moveIndex < moves.Count; moveIndex++)
            {
                Move move = moves[moveIndex];
                if (move.Source == source && move.Destination == dest && move.CardCount == cardCount)
                {
                    return true;
                }
            }
            return false;
        }

        // ---------------------------------------------------------------------------
        // EnumerateAllValidMoves — Empty Board
        // ---------------------------------------------------------------------------

        [Test]
        public void EnumerateAllValidMoves_EmptyBoard_ReturnsNoMoves()
        {
            // Arrange
            BoardModel board = TestBoardFactory.EmptyBoard();

            // Act
            MoveEnumerator.EnumerateAllValidMoves(board, _validation, _results);

            // Assert
            Assert.That(_results.Count, Is.EqualTo(0));
        }

        [Test]
        public void EnumerateAllValidMoves_EmptyBoard_ClearsExistingResults()
        {
            // Arrange
            BoardModel board = TestBoardFactory.EmptyBoard();
            _results.Add(new Move(PileId.Stock(), PileId.Waste(), 1));

            // Act
            MoveEnumerator.EnumerateAllValidMoves(board, _validation, _results);

            // Assert
            Assert.That(_results.Count, Is.EqualTo(0));
        }

        // ---------------------------------------------------------------------------
        // EnumerateAllValidMoves — Stock Draw
        // ---------------------------------------------------------------------------

        [Test]
        public void EnumerateAllValidMoves_StockHasCards_IncludesStockToWasteMove()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Stock.AddCard(FaceDown(Suit.Hearts, Rank.Five));
            });

            // Act
            MoveEnumerator.EnumerateAllValidMoves(board, _validation, _results);

            // Assert
            Assert.That(ContainsMove(_results, PileId.Stock(), PileId.Waste(), 1), Is.True);
        }

        [Test]
        public void EnumerateAllValidMoves_EmptyStock_DoesNotIncludeStockToWasteMove()
        {
            // Arrange
            BoardModel board = TestBoardFactory.EmptyBoard();

            // Act
            MoveEnumerator.EnumerateAllValidMoves(board, _validation, _results);

            // Assert
            Assert.That(ContainsMove(_results, PileId.Stock(), PileId.Waste(), 1), Is.False);
        }

        // ---------------------------------------------------------------------------
        // EnumerateAllValidMoves — Waste to Foundation
        // ---------------------------------------------------------------------------

        [Test]
        public void EnumerateAllValidMoves_WasteAceOnEmptyFoundation_IncludesWasteToFoundationMove()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Waste.AddCard(FaceUp(Suit.Hearts, Rank.Ace));
            });

            // Act
            MoveEnumerator.EnumerateAllValidMoves(board, _validation, _results);

            // Assert
            bool foundMove = false;
            for (int foundationIndex = 0; foundationIndex < 4; foundationIndex++)
            {
                if (ContainsMove(_results, PileId.Waste(), PileId.Foundation(foundationIndex), 1))
                {
                    foundMove = true;
                    break;
                }
            }
            Assert.That(foundMove, Is.True);
        }

        [Test]
        public void EnumerateAllValidMoves_WasteTwoOnAceFoundation_IncludesWasteToFoundationMove()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Foundations[0].AddCard(FaceUp(Suit.Hearts, Rank.Ace));
                b.Waste.AddCard(FaceUp(Suit.Hearts, Rank.Two));
            });

            // Act
            MoveEnumerator.EnumerateAllValidMoves(board, _validation, _results);

            // Assert
            Assert.That(ContainsMove(_results, PileId.Waste(), PileId.Foundation(0), 1), Is.True);
        }

        [Test]
        public void EnumerateAllValidMoves_WasteTwoOnWrongSuitFoundation_DoesNotIncludeWasteToFoundationMove()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Foundations[0].AddCard(FaceUp(Suit.Clubs, Rank.Ace));
                b.Waste.AddCard(FaceUp(Suit.Hearts, Rank.Two));
            });

            // Act
            MoveEnumerator.EnumerateAllValidMoves(board, _validation, _results);

            // Assert
            Assert.That(ContainsMove(_results, PileId.Waste(), PileId.Foundation(0), 1), Is.False);
        }

        // ---------------------------------------------------------------------------
        // EnumerateAllValidMoves — Waste to Tableau
        // ---------------------------------------------------------------------------

        [Test]
        public void EnumerateAllValidMoves_WasteKingOnEmptyTableau_IncludesWasteToTableauMove()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Waste.AddCard(FaceUp(Suit.Hearts, Rank.King));
            });

            // Act
            MoveEnumerator.EnumerateAllValidMoves(board, _validation, _results);

            // Assert
            bool foundMove = false;
            for (int tableauIndex = 0; tableauIndex < 7; tableauIndex++)
            {
                if (ContainsMove(_results, PileId.Waste(), PileId.Tableau(tableauIndex), 1))
                {
                    foundMove = true;
                    break;
                }
            }
            Assert.That(foundMove, Is.True);
        }

        [Test]
        public void EnumerateAllValidMoves_WasteRedSevenOnBlackEightTableau_IncludesWasteToTableauMove()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Waste.AddCard(FaceUp(Suit.Hearts, Rank.Seven));
                b.Tableau[2].AddCard(FaceUp(Suit.Spades, Rank.Eight));
            });

            // Act
            MoveEnumerator.EnumerateAllValidMoves(board, _validation, _results);

            // Assert
            Assert.That(ContainsMove(_results, PileId.Waste(), PileId.Tableau(2), 1), Is.True);
        }

        [Test]
        public void EnumerateAllValidMoves_WasteKingOnNonEmptyTableau_DoesNotIncludeWasteToTableauMove()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Waste.AddCard(FaceUp(Suit.Hearts, Rank.King));
                b.Tableau[0].AddCard(FaceUp(Suit.Spades, Rank.Eight));
            });

            // Act
            MoveEnumerator.EnumerateAllValidMoves(board, _validation, _results);

            // Assert
            Assert.That(ContainsMove(_results, PileId.Waste(), PileId.Tableau(0), 1), Is.False);
        }

        // ---------------------------------------------------------------------------
        // EnumerateAllValidMoves — Tableau to Foundation
        // ---------------------------------------------------------------------------

        [Test]
        public void EnumerateAllValidMoves_TableauAceOnEmptyFoundation_IncludesTableauToFoundationMove()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[0].AddCard(FaceUp(Suit.Clubs, Rank.Ace));
            });

            // Act
            MoveEnumerator.EnumerateAllValidMoves(board, _validation, _results);

            // Assert
            bool foundMove = false;
            for (int foundationIndex = 0; foundationIndex < 4; foundationIndex++)
            {
                if (ContainsMove(_results, PileId.Tableau(0), PileId.Foundation(foundationIndex), 1))
                {
                    foundMove = true;
                    break;
                }
            }
            Assert.That(foundMove, Is.True);
        }

        [Test]
        public void EnumerateAllValidMoves_TableauMatchingSuitRankOnFoundation_IncludesTableauToFoundationMove()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Foundations[1].AddCard(FaceUp(Suit.Diamonds, Rank.Ace));
                b.Foundations[1].AddCard(FaceUp(Suit.Diamonds, Rank.Two));
                b.Tableau[3].AddCard(FaceUp(Suit.Diamonds, Rank.Three));
            });

            // Act
            MoveEnumerator.EnumerateAllValidMoves(board, _validation, _results);

            // Assert
            Assert.That(ContainsMove(_results, PileId.Tableau(3), PileId.Foundation(1), 1), Is.True);
        }

        [Test]
        public void EnumerateAllValidMoves_TableauNonAceFaceUpTopCard_DoesNotIncludeTableauToFoundationMove()
        {
            // Arrange: a face-up non-Ace with empty foundation — cannot go to foundation
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[0].AddCard(FaceUp(Suit.Clubs, Rank.Five));
            });

            // Act
            MoveEnumerator.EnumerateAllValidMoves(board, _validation, _results);

            // Assert
            for (int foundationIndex = 0; foundationIndex < 4; foundationIndex++)
            {
                Assert.That(ContainsMove(_results, PileId.Tableau(0), PileId.Foundation(foundationIndex), 1), Is.False);
            }
        }

        // ---------------------------------------------------------------------------
        // EnumerateAllValidMoves — Tableau to Tableau (single card)
        // ---------------------------------------------------------------------------

        [Test]
        public void EnumerateAllValidMoves_TableauRedSixOnBlackSeven_IncludesTableauToTableauMove()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[0].AddCard(FaceUp(Suit.Hearts, Rank.Six));
                b.Tableau[1].AddCard(FaceUp(Suit.Clubs, Rank.Seven));
            });

            // Act
            MoveEnumerator.EnumerateAllValidMoves(board, _validation, _results);

            // Assert
            Assert.That(ContainsMove(_results, PileId.Tableau(0), PileId.Tableau(1), 1), Is.True);
        }

        [Test]
        public void EnumerateAllValidMoves_TableauKingOnEmptyTableau_IncludesTableauToTableauMove()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[0].AddCard(FaceUp(Suit.Spades, Rank.King));
            });

            // Act
            MoveEnumerator.EnumerateAllValidMoves(board, _validation, _results);

            // Assert
            bool foundMove = false;
            for (int destIndex = 1; destIndex < 7; destIndex++)
            {
                if (ContainsMove(_results, PileId.Tableau(0), PileId.Tableau(destIndex), 1))
                {
                    foundMove = true;
                    break;
                }
            }
            Assert.That(foundMove, Is.True);
        }

        [Test]
        public void EnumerateAllValidMoves_TableauCardToSamePile_NeverIncludesSelfMove()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[0].AddCard(FaceUp(Suit.Hearts, Rank.Seven));
            });

            // Act
            MoveEnumerator.EnumerateAllValidMoves(board, _validation, _results);

            // Assert
            Assert.That(ContainsMove(_results, PileId.Tableau(0), PileId.Tableau(0), 1), Is.False);
        }

        // ---------------------------------------------------------------------------
        // EnumerateAllValidMoves — Tableau sub-sequence enumeration
        // ---------------------------------------------------------------------------

        [Test]
        public void EnumerateAllValidMoves_TableauValidRunOfTwo_IncludesMultiCardMove()
        {
            // Arrange
            // Tableau[0]: black-8 (Spades), red-7 (Hearts) — valid alternating run of 2
            // Tableau[1]: red-9 (Diamonds) — receives black-8 at bottom (alternating, 8 = 9-1) → valid 2-card move
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[0].AddCard(FaceUp(Suit.Spades, Rank.Eight));   // black 8 (bottom of run)
                b.Tableau[0].AddCard(FaceUp(Suit.Hearts, Rank.Seven));   // red 7 (top of run)
                b.Tableau[1].AddCard(FaceUp(Suit.Diamonds, Rank.Nine));  // red 9 — accepts black 8 run (2 cards)
            });

            // Act
            MoveEnumerator.EnumerateAllValidMoves(board, _validation, _results);

            // Assert
            Assert.That(ContainsMove(_results, PileId.Tableau(0), PileId.Tableau(1), 2), Is.True);
        }

        [Test]
        public void EnumerateAllValidMoves_TableauValidRunOfThree_IncludesThreeCardMove()
        {
            // Arrange
            // Run: black-8, red-7, black-6. Dest has red-9.
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[0].AddCard(FaceUp(Suit.Spades, Rank.Eight));   // black 8
                b.Tableau[0].AddCard(FaceUp(Suit.Hearts, Rank.Seven));   // red 7
                b.Tableau[0].AddCard(FaceUp(Suit.Clubs, Rank.Six));      // black 6
                b.Tableau[1].AddCard(FaceUp(Suit.Diamonds, Rank.Nine));  // red 9 destination
            });

            // Act
            MoveEnumerator.EnumerateAllValidMoves(board, _validation, _results);

            // Assert
            Assert.That(ContainsMove(_results, PileId.Tableau(0), PileId.Tableau(1), 3), Is.True);
        }

        [Test]
        public void EnumerateAllValidMoves_TableauRunWithMultipleDestinations_IncludesFullRunMove()
        {
            // Arrange
            // Run: black-8(Spades), red-7(Hearts), black-6(Clubs)
            // Tableau[1]: red-9 (Diamonds) → accepts full run of 3 (black-8 on red-9)
            // Tableau[2]: red-7 (Hearts) → accepts top card only: black-6 on red-7 (count=1)
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[0].AddCard(FaceUp(Suit.Spades, Rank.Eight));   // black 8 (bottom)
                b.Tableau[0].AddCard(FaceUp(Suit.Hearts, Rank.Seven));   // red 7 (middle)
                b.Tableau[0].AddCard(FaceUp(Suit.Clubs, Rank.Six));      // black 6 (top)
                b.Tableau[1].AddCard(FaceUp(Suit.Diamonds, Rank.Nine));  // red 9 → accepts 3-card run
                b.Tableau[2].AddCard(FaceUp(Suit.Hearts, Rank.Seven));   // red 7 → accepts black-6 alone
            });

            // Act
            MoveEnumerator.EnumerateAllValidMoves(board, _validation, _results);

            // Assert — move of 3 cards to Tableau[1] exists
            Assert.That(ContainsMove(_results, PileId.Tableau(0), PileId.Tableau(1), 3), Is.True);
        }

        [Test]
        public void EnumerateAllValidMoves_TableauRunWithMultipleDestinations_IncludesSubsequentSingleCardMove()
        {
            // Arrange
            // Run: black-8(Spades), red-7(Hearts), black-6(Clubs)
            // Tableau[1]: red-9 (Diamonds) → accepts full run of 3 (black-8 on red-9)
            // Tableau[2]: red-7 (Hearts) → accepts top card only: black-6 on red-7 (count=1)
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[0].AddCard(FaceUp(Suit.Spades, Rank.Eight));   // black 8 (bottom)
                b.Tableau[0].AddCard(FaceUp(Suit.Hearts, Rank.Seven));   // red 7 (middle)
                b.Tableau[0].AddCard(FaceUp(Suit.Clubs, Rank.Six));      // black 6 (top)
                b.Tableau[1].AddCard(FaceUp(Suit.Diamonds, Rank.Nine));  // red 9 → accepts 3-card run
                b.Tableau[2].AddCard(FaceUp(Suit.Hearts, Rank.Seven));   // red 7 → accepts black-6 alone
            });

            // Act
            MoveEnumerator.EnumerateAllValidMoves(board, _validation, _results);

            // Assert — single card move (black-6) to Tableau[2] also enumerated
            Assert.That(ContainsMove(_results, PileId.Tableau(0), PileId.Tableau(2), 1), Is.True);
        }

        [Test]
        public void EnumerateAllValidMoves_TableauRunSubsequenceTopCard_IncludesSingleCardMove()
        {
            // Arrange
            // Run: black-8, red-7, black-6 — only black-6 (top card, count=1) can go on red-7 elsewhere
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[0].AddCard(FaceUp(Suit.Spades, Rank.Eight));   // black 8
                b.Tableau[0].AddCard(FaceUp(Suit.Hearts, Rank.Seven));   // red 7
                b.Tableau[0].AddCard(FaceUp(Suit.Clubs, Rank.Six));      // black 6 (top)
                b.Tableau[1].AddCard(FaceUp(Suit.Hearts, Rank.Seven));   // red 7 → accepts black 6 (count=1)
            });

            // Act
            MoveEnumerator.EnumerateAllValidMoves(board, _validation, _results);

            // Assert
            Assert.That(ContainsMove(_results, PileId.Tableau(0), PileId.Tableau(1), 1), Is.True);
        }

        [Test]
        public void EnumerateAllValidMoves_TableauFaceDownCardsNotIncludedInRun()
        {
            // Arrange
            // Tableau[0]: face-down 9, face-up black-8, face-up red-7
            // Face-down card should not be part of any enumerated run
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[0].AddCard(FaceDown(Suit.Hearts, Rank.Nine));  // face-down — cannot be moved as part of run
                b.Tableau[0].AddCard(FaceUp(Suit.Spades, Rank.Eight));   // black 8
                b.Tableau[0].AddCard(FaceUp(Suit.Hearts, Rank.Seven));   // red 7 (top)
                b.Tableau[1].AddCard(FaceUp(Suit.Clubs, Rank.Nine));     // black 9 destination for black-8+run
            });

            // Act
            MoveEnumerator.EnumerateAllValidMoves(board, _validation, _results);

            // Assert — cannot move 3 cards because face-down card is not part of valid sequence
            Assert.That(ContainsMove(_results, PileId.Tableau(0), PileId.Tableau(1), 3), Is.False);
        }

        [Test]
        public void EnumerateAllValidMoves_TableauFaceDownCardsAllowFaceUpSubsequence()
        {
            // Arrange
            // Tableau[0]: face-down 9, face-up black-8, face-up red-7
            // The face-up portion (black-8, red-7) is a valid run of 2
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[0].AddCard(FaceDown(Suit.Hearts, Rank.Nine));  // face-down
                b.Tableau[0].AddCard(FaceUp(Suit.Spades, Rank.Eight));   // black 8 (face-up)
                b.Tableau[0].AddCard(FaceUp(Suit.Hearts, Rank.Seven));   // red 7 (top)
                b.Tableau[1].AddCard(FaceUp(Suit.Diamonds, Rank.Nine));  // red 9 accepts black-8 run
            });

            // Act
            MoveEnumerator.EnumerateAllValidMoves(board, _validation, _results);

            // Assert — can move 2 cards (face-up portion of the run)
            Assert.That(ContainsMove(_results, PileId.Tableau(0), PileId.Tableau(1), 2), Is.True);
        }

        // ---------------------------------------------------------------------------
        // EnumerateAllValidMoves — Completeness on known board
        // ---------------------------------------------------------------------------

        [Test]
        public void EnumerateAllValidMoves_KnownBoardWithOneWasteToFoundation_ExactlyOneFoundationMoveFound()
        {
            // Arrange: Ace of Hearts in waste, all foundations empty
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Waste.AddCard(FaceUp(Suit.Hearts, Rank.Ace));
            });

            // Act
            MoveEnumerator.EnumerateAllValidMoves(board, _validation, _results);

            // Assert — exactly one waste-to-foundation move
            int wasteToFoundationCount = 0;
            for (int moveIndex = 0; moveIndex < _results.Count; moveIndex++)
            {
                Move move = _results[moveIndex];
                if (move.Source == PileId.Waste() && move.Destination.Type == PileType.Foundation)
                {
                    wasteToFoundationCount++;
                }
            }
            Assert.That(wasteToFoundationCount, Is.EqualTo(1));
        }

        [Test]
        public void EnumerateAllValidMoves_KnownBoardWithOneWasteToTableau_ExactlyOneTableauMoveFound()
        {
            // Arrange: red 7 in waste, one black 8 tableau column, all other tableau empty
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Waste.AddCard(FaceUp(Suit.Hearts, Rank.Seven));
                b.Tableau[0].AddCard(FaceUp(Suit.Clubs, Rank.Eight));
            });

            // Act
            MoveEnumerator.EnumerateAllValidMoves(board, _validation, _results);

            // Assert — waste to tableau[0] is the only valid waste-to-tableau move
            int wasteToTableauCount = 0;
            for (int moveIndex = 0; moveIndex < _results.Count; moveIndex++)
            {
                Move move = _results[moveIndex];
                if (move.Source == PileId.Waste() && move.Destination.Type == PileType.Tableau)
                {
                    wasteToTableauCount++;
                }
            }
            Assert.That(wasteToTableauCount, Is.EqualTo(1));
        }

        [Test]
        public void EnumerateAllValidMoves_KingInWasteWithSixEmptyTableauColumns_FindsSixWasteToTableauMoves()
        {
            // Arrange: King of Spades in waste, Tableau[0] has a card, rest are empty
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Waste.AddCard(FaceUp(Suit.Spades, Rank.King));
                b.Tableau[0].AddCard(FaceUp(Suit.Hearts, Rank.Two)); // blocks Tableau[0] from receiving King
            });

            // Act
            MoveEnumerator.EnumerateAllValidMoves(board, _validation, _results);

            // Assert — 6 empty tableau columns can receive King
            int wasteToTableauCount = 0;
            for (int moveIndex = 0; moveIndex < _results.Count; moveIndex++)
            {
                Move move = _results[moveIndex];
                if (move.Source == PileId.Waste() && move.Destination.Type == PileType.Tableau)
                {
                    wasteToTableauCount++;
                }
            }
            Assert.That(wasteToTableauCount, Is.EqualTo(6));
        }

        [Test]
        public void EnumerateAllValidMoves_NoMovesBoard_ReturnsEmptyList()
        {
            // Arrange
            BoardModel board = TestBoardFactory.NoMovesBoard();

            // Act
            MoveEnumerator.EnumerateAllValidMoves(board, _validation, _results);

            // Assert
            Assert.That(_results.Count, Is.EqualTo(0));
        }

        [Test]
        public void EnumerateAllValidMoves_AlmostWonBoard_IncludesTableauKingToFoundationMove()
        {
            // Arrange: AlmostWonBoard has King of Spades on Tableau[0], foundation[3] has Q of Spades
            BoardModel board = TestBoardFactory.AlmostWonBoard();

            // Act
            MoveEnumerator.EnumerateAllValidMoves(board, _validation, _results);

            // Assert — tableau[0] King of Spades can go to foundation[3]
            Assert.That(ContainsMove(_results, PileId.Tableau(0), PileId.Foundation(3), 1), Is.True);
        }

        [Test]
        public void EnumerateAllValidMoves_StockAndNoOtherMoves_ReturnsExactlyOneMove()
        {
            // Arrange: only stock card, all tableau/waste/foundations empty
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Stock.AddCard(FaceDown(Suit.Clubs, Rank.Three));
            });

            // Act
            MoveEnumerator.EnumerateAllValidMoves(board, _validation, _results);

            // Assert
            Assert.That(_results.Count, Is.EqualTo(1));
        }

        [Test]
        public void EnumerateAllValidMoves_StockAndNoOtherMoves_TheSingleMoveIsStockToWaste()
        {
            // Arrange: only stock card, all tableau/waste/foundations empty
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Stock.AddCard(FaceDown(Suit.Clubs, Rank.Three));
            });

            // Act
            MoveEnumerator.EnumerateAllValidMoves(board, _validation, _results);

            // Assert
            Assert.That(ContainsMove(_results, PileId.Stock(), PileId.Waste(), 1), Is.True);
        }

        // ---------------------------------------------------------------------------
        // HasAnyValidMove — Stock short-circuit
        // ---------------------------------------------------------------------------

        [Test]
        public void HasAnyValidMove_StockHasCards_ReturnsTrueImmediately()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Stock.AddCard(FaceDown(Suit.Hearts, Rank.Five));
            });

            // Act
            bool result = MoveEnumerator.HasAnyValidMove(board, _validation);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void HasAnyValidMove_EmptyStockNoMoves_ReturnsFalse()
        {
            // Arrange
            BoardModel board = TestBoardFactory.EmptyBoard();

            // Act
            bool result = MoveEnumerator.HasAnyValidMove(board, _validation);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void HasAnyValidMove_NoMovesBoard_ReturnsFalse()
        {
            // Arrange
            BoardModel board = TestBoardFactory.NoMovesBoard();

            // Act
            bool result = MoveEnumerator.HasAnyValidMove(board, _validation);

            // Assert
            Assert.That(result, Is.False);
        }

        // ---------------------------------------------------------------------------
        // HasAnyValidMove — Waste moves
        // ---------------------------------------------------------------------------

        [Test]
        public void HasAnyValidMove_WasteAceFoundationAvailable_ReturnsTrue()
        {
            // Arrange: Ace in waste, foundation empty
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Waste.AddCard(FaceUp(Suit.Spades, Rank.Ace));
            });

            // Act
            bool result = MoveEnumerator.HasAnyValidMove(board, _validation);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void HasAnyValidMove_WasteKingEmptyTableauAvailable_ReturnsTrue()
        {
            // Arrange: King in waste, at least one empty tableau column
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Waste.AddCard(FaceUp(Suit.Diamonds, Rank.King));
            });

            // Act
            bool result = MoveEnumerator.HasAnyValidMove(board, _validation);

            // Assert
            Assert.That(result, Is.True);
        }

        // ---------------------------------------------------------------------------
        // HasAnyValidMove — Tableau moves
        // ---------------------------------------------------------------------------

        [Test]
        public void HasAnyValidMove_ValidTableauToTableauMoveExists_ReturnsTrue()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[0].AddCard(FaceUp(Suit.Hearts, Rank.Six));
                b.Tableau[1].AddCard(FaceUp(Suit.Clubs, Rank.Seven));
            });

            // Act
            bool result = MoveEnumerator.HasAnyValidMove(board, _validation);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void HasAnyValidMove_ValidTableauToFoundationMoveExists_ReturnsTrue()
        {
            // Arrange
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[0].AddCard(FaceUp(Suit.Clubs, Rank.Ace));
            });

            // Act
            bool result = MoveEnumerator.HasAnyValidMove(board, _validation);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void HasAnyValidMove_OnlyFaceDownTableauCards_ReturnsFalse()
        {
            // Arrange: cards all face down — no valid moves possible
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Tableau[0].AddCard(FaceDown(Suit.Hearts, Rank.Six));
                b.Tableau[1].AddCard(FaceDown(Suit.Clubs, Rank.Seven));
                b.Tableau[2].AddCard(FaceDown(Suit.Spades, Rank.Eight));
            });

            // Act
            bool result = MoveEnumerator.HasAnyValidMove(board, _validation);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void HasAnyValidMove_AlmostWonBoard_ReturnsTrue()
        {
            // Arrange
            BoardModel board = TestBoardFactory.AlmostWonBoard();

            // Act
            bool result = MoveEnumerator.HasAnyValidMove(board, _validation);

            // Assert
            Assert.That(result, Is.True);
        }

        // ---------------------------------------------------------------------------
        // EnumerateAllValidMoves — Result list reuse
        // ---------------------------------------------------------------------------

        [Test]
        public void EnumerateAllValidMoves_CalledTwice_ResultsReflectSecondBoardState()
        {
            // Arrange
            BoardModel boardWithMove = TestBoardFactory.CustomBoard(b =>
            {
                b.Waste.AddCard(FaceUp(Suit.Hearts, Rank.Ace));
            });
            BoardModel boardWithNoMoves = TestBoardFactory.NoMovesBoard();

            MoveEnumerator.EnumerateAllValidMoves(boardWithMove, _validation, _results);
            int firstCount = _results.Count;

            // Act — second call with no-moves board
            MoveEnumerator.EnumerateAllValidMoves(boardWithNoMoves, _validation, _results);

            // Assert — first call had moves, second call cleared and found none
            Assert.That(firstCount, Is.GreaterThan(0));
            Assert.That(_results.Count, Is.EqualTo(0));
            // Note: both assertions together verify the clear+refill behavior in one test
        }

        // ---------------------------------------------------------------------------
        // EnumerateAllValidMoves — Multiple moves of different types on same board
        // ---------------------------------------------------------------------------

        [Test]
        public void EnumerateAllValidMoves_BoardWithWasteToTableauMove_IncludesWasteToTableauMove()
        {
            // Arrange: red 7 in waste, black 8 in tableau[0] — valid waste→tableau move
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Waste.AddCard(FaceUp(Suit.Hearts, Rank.Seven));
                b.Tableau[0].AddCard(FaceUp(Suit.Clubs, Rank.Eight));
                b.Tableau[1].AddCard(FaceUp(Suit.Spades, Rank.Ace));
            });

            // Act
            MoveEnumerator.EnumerateAllValidMoves(board, _validation, _results);

            // Assert
            Assert.That(ContainsMove(_results, PileId.Waste(), PileId.Tableau(0), 1), Is.True);
        }

        [Test]
        public void EnumerateAllValidMoves_BoardWithTableauToFoundationMove_IncludesTableauToFoundationMove()
        {
            // Arrange: Ace of Spades in tableau[1] — valid tableau→foundation move
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Waste.AddCard(FaceUp(Suit.Hearts, Rank.Seven));
                b.Tableau[0].AddCard(FaceUp(Suit.Clubs, Rank.Eight));
                b.Tableau[1].AddCard(FaceUp(Suit.Spades, Rank.Ace));
            });

            // Act
            MoveEnumerator.EnumerateAllValidMoves(board, _validation, _results);

            // Assert — tableau ace can go to any empty foundation
            bool foundMove = false;
            for (int foundationIndex = 0; foundationIndex < 4; foundationIndex++)
            {
                if (ContainsMove(_results, PileId.Tableau(1), PileId.Foundation(foundationIndex), 1))
                {
                    foundMove = true;
                    break;
                }
            }
            Assert.That(foundMove, Is.True);
        }

        [Test]
        public void EnumerateAllValidMoves_BoardWithStockAndOtherMoves_StockMoveAppearOnce()
        {
            // Arrange: stock has a card, waste has a king that can go on any empty tableau
            BoardModel board = TestBoardFactory.CustomBoard(b =>
            {
                b.Stock.AddCard(FaceDown(Suit.Hearts, Rank.Five));
                b.Waste.AddCard(FaceUp(Suit.Spades, Rank.King));
            });

            // Act
            MoveEnumerator.EnumerateAllValidMoves(board, _validation, _results);

            // Assert — stock-to-waste move appears exactly once
            int stockMoveCount = 0;
            for (int moveIndex = 0; moveIndex < _results.Count; moveIndex++)
            {
                if (_results[moveIndex].Source == PileId.Stock())
                {
                    stockMoveCount++;
                }
            }
            Assert.That(stockMoveCount, Is.EqualTo(1));
        }
    }
}
