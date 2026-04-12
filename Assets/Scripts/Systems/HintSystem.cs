using System;
using System.Collections.Generic;
using KlondikeSolitaire.Core;
using MessagePipe;

namespace KlondikeSolitaire.Systems
{
    public sealed class HintSystem : IDisposable
    {
        private readonly BoardModel _board;
        private readonly MoveEnumerator _moveEnumerator;
        private readonly IPublisher<HintHighlightMessage> _hintHighlightPublisher;
        private readonly IPublisher<HintClearedMessage> _hintClearedPublisher;
        private readonly List<Move> _cachedMoves;
        private readonly CompositeDisposable _disposables;
        private int _hintIndex;

        public HintSystem(
            BoardModel board,
            MoveEnumerator moveEnumerator,
            ISubscriber<BoardStateChangedMessage> boardStateSubscriber,
            ISubscriber<HintRequestedMessage> hintRequestedSubscriber,
            IPublisher<HintHighlightMessage> hintHighlightPublisher,
            IPublisher<HintClearedMessage> hintClearedPublisher)
        {
            _board = board;
            _moveEnumerator = moveEnumerator;
            _hintHighlightPublisher = hintHighlightPublisher;
            _hintClearedPublisher = hintClearedPublisher;
            _cachedMoves = new List<Move>();
            _hintIndex = -1;
            _disposables = new CompositeDisposable();
            boardStateSubscriber.Subscribe(OnBoardStateChanged).AddTo(_disposables);
            hintRequestedSubscriber.Subscribe(OnHintRequested).AddTo(_disposables);
        }

        private void OnHintRequested(HintRequestedMessage _)
        {
            GetNextHint();
        }

        private void OnBoardStateChanged(BoardStateChangedMessage _)
        {
            _cachedMoves.Clear();
            _hintIndex = -1;
            _hintClearedPublisher.Publish(new HintClearedMessage());
        }

        public void GetNextHint()
        {
            if (_cachedMoves.Count == 0)
            {
                _moveEnumerator.EnumerateAllValidMoves(_board, _cachedMoves);

                if (_cachedMoves.Count == 0)
                {
                    return;
                }
            }

            _hintIndex = (_hintIndex + 1) % _cachedMoves.Count;
            Move currentHint = _cachedMoves[_hintIndex];

            PileModel sourcePile = _board.GetPile(currentHint.Source);
            int sourceCardIndex = sourcePile.Count - currentHint.CardCount;

            _hintHighlightPublisher.Publish(new HintHighlightMessage(sourceCardIndex, currentHint.Source, currentHint.Destination));
        }

        public void Reset()
        {
            _cachedMoves.Clear();
            _hintIndex = -1;
            _hintClearedPublisher.Publish(new HintClearedMessage());
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
