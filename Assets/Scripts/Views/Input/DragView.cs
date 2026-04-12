using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace KlondikeSolitaire.Views
{
    public sealed class DragView : MonoBehaviour
    {
        private const int MAX_DRAG_COUNT = 13;
        private const float DRAG_BASE_Z = -1f;

        private CardView[] _draggedCards;
        private int _dragCount;
        private readonly Vector3[] _originPositions = new Vector3[MAX_DRAG_COUNT];
        private readonly Vector3[] _dragOffsets = new Vector3[MAX_DRAG_COUNT];
        private readonly UniTask[] _moveTasks = new UniTask[MAX_DRAG_COUNT];
        private bool _isDragging;
        private int _dragGeneration;
        private CancellationToken _destroyToken;

        private CardAnimator _animator;

        public bool IsDragging => _isDragging;

        [Inject]
        public void Construct(CardAnimator animator)
        {
            _animator = animator;
        }

        private void Awake()
        {
            _destroyToken = this.GetCancellationTokenOnDestroy();
        }

        public void BeginDrag(CardView[] cards, int count, PileView originPile, Vector3 pointerWorldPos)
        {
            _dragGeneration++;
            _draggedCards = cards;
            _dragCount = count;

            for (int cardIndex = 0; cardIndex < count; cardIndex++)
            {
                _originPositions[cardIndex] = cards[cardIndex].transform.position;
                cards[cardIndex].SetRendererEnabled(true);
                cards[cardIndex].SetSortingOrder(cardIndex, DRAG_BASE_Z);
                _dragOffsets[cardIndex] = cards[cardIndex].transform.position - pointerWorldPos;
            }

            _isDragging = true;
        }

        public void UpdateDragPosition(Vector3 worldPos)
        {
            if (!_isDragging || _draggedCards == null)
            {
                return;
            }

            for (int cardIndex = 0; cardIndex < _dragCount; cardIndex++)
            {
                _draggedCards[cardIndex].transform.position = worldPos + _dragOffsets[cardIndex];
            }
        }

        public async UniTask CompleteDrag(PileView targetPile)
        {
            if (_draggedCards == null)
            {
                return;
            }

            int generation = _dragGeneration;

            IReadOnlyList<CardView> targetCards = targetPile.GetCardViews();
            int startIndex = targetCards.Count - _dragCount;

            for (int cardIndex = 0; cardIndex < _dragCount; cardIndex++)
            {
                Vector3 targetPos = targetPile.GetCardWorldPosition(startIndex + cardIndex);
                _moveTasks[cardIndex] = _animator.MoveCard(_draggedCards[cardIndex].transform, targetPos);
            }

            for (int taskIndex = _dragCount; taskIndex < MAX_DRAG_COUNT; taskIndex++)
            {
                _moveTasks[taskIndex] = default;
            }
            await UniTask.WhenAll(_moveTasks).AttachExternalCancellation(_destroyToken);

            targetPile.UpdateCardPositions();

            if (_dragGeneration == generation)
            {
                ClearDragState();
            }
        }

        public async UniTask CancelDrag()
        {
            if (_draggedCards == null)
            {
                return;
            }

            int generation = _dragGeneration;

            for (int cardIndex = 0; cardIndex < _dragCount; cardIndex++)
            {
                _moveTasks[cardIndex] = _animator.MoveCard(_draggedCards[cardIndex].transform, _originPositions[cardIndex]);
            }

            Transform firstCardTransform = _dragCount > 0 ? _draggedCards[0].transform : null;

            for (int taskIndex = _dragCount; taskIndex < MAX_DRAG_COUNT; taskIndex++)
            {
                _moveTasks[taskIndex] = default;
            }
            await UniTask.WhenAll(_moveTasks).AttachExternalCancellation(_destroyToken);

            if (firstCardTransform != null)
            {
                await _animator.ShakeCard(firstCardTransform);
            }

            if (_dragGeneration == generation)
            {
                ClearDragState();
            }
        }

        public bool IsCardBeingDragged(CardView card)
        {
            if (!_isDragging || _draggedCards == null)
            {
                return false;
            }

            for (int cardIndex = 0; cardIndex < _dragCount; cardIndex++)
            {
                if (_draggedCards[cardIndex] == card)
                {
                    return true;
                }
            }

            return false;
        }

        private void ClearDragState()
        {
            _draggedCards = null;
            _dragCount = 0;
            _isDragging = false;
        }
    }
}
