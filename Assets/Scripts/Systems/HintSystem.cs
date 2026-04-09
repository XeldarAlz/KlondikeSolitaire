using System;
using System.Collections.Generic;
using KlondikeSolitaire.Core;
using MessagePipe;

namespace KlondikeSolitaire.Systems
{
    public sealed class HintSystem : IDisposable
    {
        private readonly BoardModel _board;
        private readonly MoveValidationSystem _moveValidation;
        private readonly IPublisher<HintHighlightMessage> _hintHighlightPublisher;
        private readonly IPublisher<HintClearedMessage> _hintClearedPublisher;
        private readonly List<Move> _cachedMoves;
        private int _hintIndex;
        private IDisposable _subscription;

        public HintSystem(
            BoardModel board,
            MoveValidationSystem moveValidation,
            ISubscriber<BoardStateChangedMessage> boardStateSubscriber,
            IPublisher<HintHighlightMessage> hintHighlightPublisher,
            IPublisher<HintClearedMessage> hintClearedPublisher)
        {
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _moveValidation = moveValidation ?? throw new ArgumentNullException(nameof(moveValidation));
            _hintHighlightPublisher = hintHighlightPublisher ?? throw new ArgumentNullException(nameof(hintHighlightPublisher));
            _hintClearedPublisher = hintClearedPublisher ?? throw new ArgumentNullException(nameof(hintClearedPublisher));
            _cachedMoves = new List<Move>();
            _hintIndex = -1;
            _subscription = (boardStateSubscriber ?? throw new ArgumentNullException(nameof(boardStateSubscriber)))
                .Subscribe(OnBoardStateChanged);
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
                MoveEnumerator.EnumerateAllValidMoves(_board, _moveValidation, _cachedMoves);

                if (_cachedMoves.Count == 0)
                {
                    return;
                }
            }

            _hintIndex = (_hintIndex + 1) % _cachedMoves.Count;
            Move currentHint = _cachedMoves[_hintIndex];

            PileModel sourcePile = _board.GetPile(currentHint.Source);
            int sourceCardIndex = sourcePile.Count - currentHint.CardCount;

            PileId[] destPileIds = new PileId[] { currentHint.Destination };
            _hintHighlightPublisher.Publish(new HintHighlightMessage(sourceCardIndex, currentHint.Source, destPileIds));
        }

        public void Reset()
        {
            _cachedMoves.Clear();
            _hintIndex = -1;
            _hintClearedPublisher.Publish(new HintClearedMessage());
        }

        public void Dispose()
        {
            _subscription?.Dispose();
            _subscription = null;
        }
    }
}
