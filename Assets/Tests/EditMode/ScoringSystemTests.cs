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
    }
}
