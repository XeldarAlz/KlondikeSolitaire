using System;
using KlondikeSolitaire.Core;
using MessagePipe;

namespace KlondikeSolitaire.Systems
{
    public sealed class NoMovesDetectionSystem : IDisposable
    {
        private readonly BoardModel _board;
        private readonly MoveValidationSystem _moveValidation;
        private readonly GamePhaseModel _gamePhase;
        private readonly IPublisher<NoMovesDetectedMessage> _noMovesPublisher;
        private IDisposable _subscription;

        public NoMovesDetectionSystem(
            BoardModel board,
            MoveValidationSystem moveValidation,
            GamePhaseModel gamePhase,
            ISubscriber<BoardStateChangedMessage> boardStateSubscriber,
            IPublisher<NoMovesDetectedMessage> noMovesPublisher)
        {
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _moveValidation = moveValidation ?? throw new ArgumentNullException(nameof(moveValidation));
            _gamePhase = gamePhase ?? throw new ArgumentNullException(nameof(gamePhase));
            _noMovesPublisher = noMovesPublisher ?? throw new ArgumentNullException(nameof(noMovesPublisher));
            _subscription = (boardStateSubscriber ?? throw new ArgumentNullException(nameof(boardStateSubscriber)))
                .Subscribe(OnBoardStateChanged);
        }

        private void OnBoardStateChanged(BoardStateChangedMessage _)
        {
            if (_gamePhase.Phase.Value != GamePhase.Playing)
            {
                return;
            }

            if (MoveEnumerator.HasAnyValidMove(_board, _moveValidation))
            {
                return;
            }

            _noMovesPublisher.Publish(new NoMovesDetectedMessage());
        }

        public void Dispose()
        {
            _subscription?.Dispose();
            _subscription = null;
        }
    }
}
