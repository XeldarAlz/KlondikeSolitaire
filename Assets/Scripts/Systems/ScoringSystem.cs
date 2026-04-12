using KlondikeSolitaire.Core;
using MessagePipe;

namespace KlondikeSolitaire.Systems
{
    public sealed class ScoringSystem
    {
        private readonly ScoreModel _scoreModel;
        private readonly ScoringTable _scoringTable;
        private readonly IPublisher<ScoreChangedMessage> _scoreChangedPublisher;

        public ScoringSystem(ScoreModel scoreModel, ScoringTable scoringTable, IPublisher<ScoreChangedMessage> scoreChangedPublisher)
        {
            _scoreModel = scoreModel;
            _scoringTable = scoringTable;
            _scoreChangedPublisher = scoreChangedPublisher;
        }

        public int CalculateScore(MoveType moveType)
        {
            return moveType switch
            {
                MoveType.WasteToTableau => _scoringTable.WasteToTableau,
                MoveType.WasteToFoundation => _scoringTable.WasteToFoundation,
                MoveType.TableauToFoundation => _scoringTable.TableauToFoundation,
                MoveType.FoundationToTableau => _scoringTable.FoundationToTableau,
                MoveType.FlipCard => _scoringTable.FlipCard,
                _ => 0
            };
        }

        public void ApplyDelta(int delta)
        {
            if (delta == 0)
            {
                return;
            }

            int newScore = System.Math.Max(0, _scoreModel.Score.Value + delta);
            _scoreModel.Score.Value = newScore;
            _scoreChangedPublisher.Publish(new ScoreChangedMessage(newScore, delta));
        }

        public void Reset()
        {
            _scoreModel.Score.Value = 0;
            _scoreChangedPublisher.Publish(new ScoreChangedMessage(0, 0));
        }
    }
}
