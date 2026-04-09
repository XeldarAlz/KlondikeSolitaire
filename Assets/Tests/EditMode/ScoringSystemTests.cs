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

        #region CalculateScore Tests

        [Test]
        public void CalculateScore_WasteToTableau_Returns5()
        {
            int result = _sut.CalculateScore(MoveType.WasteToTableau);

            Assert.That(result, Is.EqualTo(5));
        }

        [Test]
        public void CalculateScore_WasteToFoundation_Returns10()
        {
            int result = _sut.CalculateScore(MoveType.WasteToFoundation);

            Assert.That(result, Is.EqualTo(10));
        }

        [Test]
        public void CalculateScore_TableauToFoundation_Returns10()
        {
            int result = _sut.CalculateScore(MoveType.TableauToFoundation);

            Assert.That(result, Is.EqualTo(10));
        }

        [Test]
        public void CalculateScore_FoundationToTableau_ReturnsNegative15()
        {
            int result = _sut.CalculateScore(MoveType.FoundationToTableau);

            Assert.That(result, Is.EqualTo(-15));
        }

        [Test]
        public void CalculateScore_FlipCard_Returns5()
        {
            int result = _sut.CalculateScore(MoveType.FlipCard);

            Assert.That(result, Is.EqualTo(5));
        }

        [Test]
        public void CalculateScore_DrawFromStock_Returns0()
        {
            int result = _sut.CalculateScore(MoveType.DrawFromStock);

            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void CalculateScore_RecycleWaste_Returns0()
        {
            int result = _sut.CalculateScore(MoveType.RecycleWaste);

            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void CalculateScore_TableauToTableau_Returns0()
        {
            int result = _sut.CalculateScore(MoveType.TableauToTableau);

            Assert.That(result, Is.EqualTo(0));
        }

        #endregion

        #region ApplyDelta Tests

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
        public void ApplyDelta_PublishesScoreChangedMessage()
        {
            _sut.ApplyDelta(10);

            Assert.That(_scoreChangedPublisher.MessageCount, Is.EqualTo(1));
        }

        [Test]
        public void ApplyDelta_MessageContainsCorrectNewScoreAndDelta()
        {
            _scoreModel.Score.Value = 5;
            _sut.ApplyDelta(15);

            ScoreChangedMessage message = _scoreChangedPublisher.LastMessage;
            Assert.That(message.NewScore, Is.EqualTo(20));
            Assert.That(message.Delta, Is.EqualTo(15));
        }

        #endregion

        #region Reset Tests

        [Test]
        public void Reset_SetsScoreToZero()
        {
            _scoreModel.Score.Value = 100;
            _sut.Reset();

            Assert.That(_scoreModel.Score.Value, Is.EqualTo(0));
        }

        [Test]
        public void Reset_PublishesScoreChangedMessage()
        {
            _scoreModel.Score.Value = 100;
            _sut.Reset();

            Assert.That(_scoreChangedPublisher.MessageCount, Is.EqualTo(1));
        }

        #endregion
    }
}
