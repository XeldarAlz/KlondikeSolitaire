using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace KlondikeSolitaire.Views
{
    public sealed class DragView : MonoBehaviour
    {
        private CardView[] _draggedCards;
        private Vector3[] _originPositions;
        private Vector3[] _dragOffsets;
        private bool _isDragging;

        private static int CardsLayerId;
        private static int DragLayerId;

        private AnimationConfig _animConfig;

        public bool IsDragging => _isDragging;

        private void Awake()
        {
            CardsLayerId = SortingLayer.NameToID("Cards");
            DragLayerId = SortingLayer.NameToID("Drag");
        }

        public void Initialize(AnimationConfig animConfig)
        {
            _animConfig = animConfig;
        }

        public void BeginDrag(CardView[] cards, PileView originPile, Vector3 pointerWorldPos)
        {
            _draggedCards = cards;
            _originPositions = new Vector3[cards.Length];
            _dragOffsets = new Vector3[cards.Length];

            for (int cardIndex = 0; cardIndex < cards.Length; cardIndex++)
            {
                _originPositions[cardIndex] = cards[cardIndex].transform.position;
                _dragOffsets[cardIndex] = cards[cardIndex].transform.position - pointerWorldPos;
                cards[cardIndex].SetSortingLayer(DragLayerId);
                cards[cardIndex].SetSortingOrder(cardIndex);
            }

            _isDragging = true;
        }

        public void UpdateDragPosition(Vector3 worldPos)
        {
            if (!_isDragging || _draggedCards == null)
            {
                return;
            }

            for (int cardIndex = 0; cardIndex < _draggedCards.Length; cardIndex++)
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

            UniTask[] moveTasks = new UniTask[_draggedCards.Length];
            List<CardView> targetCards = targetPile.GetCardViews();
            int startIndex = targetCards.Count - _draggedCards.Length;

            for (int cardIndex = 0; cardIndex < _draggedCards.Length; cardIndex++)
            {
                _draggedCards[cardIndex].SetSortingLayer(CardsLayerId);
                Vector3 targetPos = targetPile.GetCardWorldPosition(startIndex + cardIndex);
                moveTasks[cardIndex] = CardAnimator.MoveCard(_draggedCards[cardIndex].transform, targetPos, _animConfig);
            }

            await UniTask.WhenAll(moveTasks);

            ClearDragState();
        }

        public async UniTask CancelDrag()
        {
            if (_draggedCards == null)
            {
                return;
            }

            UniTask[] moveTasks = new UniTask[_draggedCards.Length];
            for (int cardIndex = 0; cardIndex < _draggedCards.Length; cardIndex++)
            {
                _draggedCards[cardIndex].SetSortingLayer(CardsLayerId);
                moveTasks[cardIndex] = CardAnimator.MoveCard(_draggedCards[cardIndex].transform, _originPositions[cardIndex], _animConfig);
            }

            Transform firstCardTransform = _draggedCards.Length > 0 ? _draggedCards[0].transform : null;

            await UniTask.WhenAll(moveTasks);

            if (firstCardTransform != null)
            {
                await CardAnimator.ShakeCard(firstCardTransform, _animConfig);
            }

            ClearDragState();
        }

        private void ClearDragState()
        {
            _draggedCards = null;
            _originPositions = null;
            _dragOffsets = null;
            _isDragging = false;
        }
    }
}
