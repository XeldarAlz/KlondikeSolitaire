using KlondikeSolitaire.Core;
using KlondikeSolitaire.Systems;
using NUnit.Framework;

namespace KlondikeSolitaire.Tests
{
    public sealed class ScoringSystemTests
    {
        private ScoreModel _scoreModel;
        private ScoringTable _scoringTable;
        private TestPublisher<ScoreChangedMessage> _scoreChangedPublisher;
        private ScoringSystem _sut;

        [SetUp]
        public void SetUp()
        {
            _scoreModel = new ScoreModel();
            _scoringTable = new ScoringTable(
                wasteToTableau: 5,
                wasteToFoundation: 10,
                tableauToFoundation: 10,
                foundationToTableau: -15,
                flipCard: 5);
            _scoreChangedPublisher = new TestPublisher<ScoreChangedMessage>();
            _sut = new ScoringSystem(_scoreModel, _scoringTable, _scoreChangedPublisher);
        }

        [Test]
        public void ApplyDelta_PositiveDelta_IncreasesScore()
        {
            _sut.ApplyDelta(10);

            Assert.That(_scoreModel.Score.Value, Is.EqualTo(10));
        }

        [Test]
        public void ApplyDelta_NegativeDelta_DecreasesScore()
        {
            _scoreModel.Score.Value = 20;
            _sut.ApplyDelta(-5);

            Assert.That(_scoreModel.Score.Value, Is.EqualTo(15));
        }

        [Test]
        public void ApplyDelta_NegativeDeltaBeyondZero_ClampsToZero()
        {
            _scoreModel.Score.Value = 5;
            _sut.ApplyDelta(-10);

            Assert.That(_scoreModel.Score.Value, Is.EqualTo(0));
        }

        [Test]
        public void ApplyDelta_PublishesScoreChangedMessageWithCorrectValues()
        {
            _scoreModel.Score.Value = 5;
            _sut.ApplyDelta(15);

            Assert.That(_scoreChangedPublisher.MessageCount, Is.EqualTo(1));
            ScoreChangedMessage message = _scoreChangedPublisher.LastMessage;
            Assert.That(message.NewScore, Is.EqualTo(20));
            Assert.That(message.Delta, Is.EqualTo(15));
        }

        [Test]
        public void ApplyDelta_ZeroDelta_DoesNotPublishMessage()
        {
            _sut.ApplyDelta(0);

            Assert.That(_scoreChangedPublisher.MessageCount, Is.EqualTo(0));
        }

        [Test]
        public void Reset_SetsScoreToZeroAndPublishesMessage()
        {
            _scoreModel.Score.Value = 100;
            _sut.Reset();

            Assert.That(_scoreModel.Score.Value, Is.EqualTo(0));
            Assert.That(_scoreChangedPublisher.MessageCount, Is.EqualTo(1));
        }

        [Test]
        public void Reset_PublishesMessageWithZeroScoreAndZeroDelta()
        {
            _scoreModel.Score.Value = 50;
            _sut.Reset();

            ScoreChangedMessage message = _scoreChangedPublisher.LastMessage;
            Assert.That(message.NewScore, Is.EqualTo(0));
            Assert.That(message.Delta, Is.EqualTo(0));
        }

        // --- CalculateScore: all MoveType values ---

        [Test]
        public void CalculateScore_WasteToTableau_ReturnsConfiguredValue()
        {
            int result = _sut.CalculateScore(MoveType.WasteToTableau);

            Assert.That(result, Is.EqualTo(5));
        }

        [Test]
        public void CalculateScore_WasteToFoundation_ReturnsConfiguredValue()
        {
            int result = _sut.CalculateScore(MoveType.WasteToFoundation);

            Assert.That(result, Is.EqualTo(10));
        }

        [Test]
        public void CalculateScore_TableauToFoundation_ReturnsConfiguredValue()
        {
            int result = _sut.CalculateScore(MoveType.TableauToFoundation);

            Assert.That(result, Is.EqualTo(10));
        }

        [Test]
        public void CalculateScore_FoundationToTableau_ReturnsNegativeValue()
        {
            int result = _sut.CalculateScore(MoveType.FoundationToTableau);

            Assert.That(result, Is.EqualTo(-15));
        }

        [Test]
        public void CalculateScore_FlipCard_ReturnsConfiguredValue()
        {
            int result = _sut.CalculateScore(MoveType.FlipCard);

            Assert.That(result, Is.EqualTo(5));
        }

        [Test]
        public void CalculateScore_DrawFromStock_ReturnsZero()
        {
            int result = _sut.CalculateScore(MoveType.DrawFromStock);

            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void CalculateScore_RecycleWaste_ReturnsZero()
        {
            int result = _sut.CalculateScore(MoveType.RecycleWaste);

            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void CalculateScore_TableauToTableau_ReturnsZero()
        {
            int result = _sut.CalculateScore(MoveType.TableauToTableau);

            Assert.That(result, Is.EqualTo(0));
        }

        // --- ApplyDelta: accumulation and clamping edge cases ---

        [Test]
        public void ApplyDelta_MultiplePositiveDeltas_Accumulates()
        {
            _sut.ApplyDelta(5);
            _sut.ApplyDelta(10);
            _sut.ApplyDelta(15);

            Assert.That(_scoreModel.Score.Value, Is.EqualTo(30));
        }

        [Test]
        public void ApplyDelta_MultiplePositiveDeltas_PublishesMessageEachTime()
        {
            _sut.ApplyDelta(5);
            _sut.ApplyDelta(10);
            _sut.ApplyDelta(15);

            Assert.That(_scoreChangedPublisher.MessageCount, Is.EqualTo(3));
        }

        [Test]
        public void ApplyDelta_ExactlyNegatingScore_ClampsToZero()
        {
            _scoreModel.Score.Value = 10;
            _sut.ApplyDelta(-10);

            Assert.That(_scoreModel.Score.Value, Is.EqualTo(0));
        }

        [Test]
        public void ApplyDelta_NegativeDeltaWhenAlreadyZero_StaysAtZero()
        {
            _sut.ApplyDelta(-15);

            Assert.That(_scoreModel.Score.Value, Is.EqualTo(0));
        }

        [Test]
        public void ApplyDelta_RepeatedNegativeDeltas_StaysClampedAtZero()
        {
            _sut.ApplyDelta(-5);
            _sut.ApplyDelta(-10);
            _sut.ApplyDelta(-15);

            Assert.That(_scoreModel.Score.Value, Is.EqualTo(0));
        }

        [Test]
        public void ApplyDelta_ClampedNegative_MessageNewScoreIsZero()
        {
            _scoreModel.Score.Value = 3;
            _sut.ApplyDelta(-15);

            ScoreChangedMessage message = _scoreChangedPublisher.LastMessage;
            Assert.That(message.NewScore, Is.EqualTo(0));
        }

        [Test]
        public void ApplyDelta_ClampedNegative_MessageDeltaIsOriginalDelta()
        {
            _scoreModel.Score.Value = 3;
            _sut.ApplyDelta(-15);

            ScoreChangedMessage message = _scoreChangedPublisher.LastMessage;
            Assert.That(message.Delta, Is.EqualTo(-15));
        }

        [Test]
        public void ApplyDelta_PositiveAfterReset_AccumulatesFromZero()
        {
            _scoreModel.Score.Value = 100;
            _sut.Reset();
            _scoreChangedPublisher.Clear();

            _sut.ApplyDelta(25);

            Assert.That(_scoreModel.Score.Value, Is.EqualTo(25));
        }

        [Test]
        public void ApplyDelta_PositiveThenNegative_NetResult()
        {
            _sut.ApplyDelta(20);
            _sut.ApplyDelta(-5);

            Assert.That(_scoreModel.Score.Value, Is.EqualTo(15));
        }

        [Test]
        public void ApplyDelta_LargePositiveValue_NoOverflow()
        {
            _sut.ApplyDelta(1000000);

            Assert.That(_scoreModel.Score.Value, Is.EqualTo(1000000));
        }

        [Test]
        public void Reset_WhenAlreadyZero_StillPublishesMessage()
        {
            _sut.Reset();

            Assert.That(_scoreChangedPublisher.MessageCount, Is.EqualTo(1));
            Assert.That(_scoreModel.Score.Value, Is.EqualTo(0));
        }
    }
}
