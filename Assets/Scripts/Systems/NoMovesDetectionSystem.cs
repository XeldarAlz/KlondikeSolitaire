using System;
using KlondikeSolitaire.Core;
using MessagePipe;

namespace KlondikeSolitaire.Systems
{
    public sealed class NoMovesDetectionSystem : IDisposable
    {
        private readonly BoardModel _board;
        private readonly MoveEnumerator _moveEnumerator;
        private readonly GamePhaseModel _gamePhase;
        private readonly IPublisher<NoMovesDetectedMessage> _noMovesPublisher;
        private readonly CompositeDisposable _disposables;

        public NoMovesDetectionSystem(
            BoardModel board,
            MoveEnumerator moveEnumerator,
            GamePhaseModel gamePhase,
            ISubscriber<BoardStateChangedMessage> boardStateSubscriber,
            IPublisher<NoMovesDetectedMessage> noMovesPublisher)
        {
            _board = board;
            _moveEnumerator = moveEnumerator;
            _gamePhase = gamePhase;
            _noMovesPublisher = noMovesPublisher;
            _disposables = new CompositeDisposable();
            boardStateSubscriber.Subscribe(OnBoardStateChanged).AddTo(_disposables);
        }

        private void OnBoardStateChanged(BoardStateChangedMessage _)
        {
            if (_gamePhase.Phase.Value != GamePhase.Playing)
            {
                return;
            }

            if (_moveEnumerator.HasAnyValidMove(_board))
            {
                return;
            }

            _noMovesPublisher.Publish(new NoMovesDetectedMessage());
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
