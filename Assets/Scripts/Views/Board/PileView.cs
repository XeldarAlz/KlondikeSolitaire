using System.Collections.Generic;
using KlondikeSolitaire.Core;
using UnityEngine;
using VContainer;

namespace KlondikeSolitaire.Views
{
    public sealed class PileView : MonoBehaviour
    {
        [SerializeField] private PileType _pileType;
        [SerializeField] private int _pileIndex;

        private LayoutConfig _layout;
        private readonly List<CardView> _cards = new();

        public PileId PileId => new((_pileType), _pileIndex);

        [Inject]
        public void Construct(LayoutConfig layout)
        {
            _layout = layout;
        }

        private void Awake()
        {
        }

        public void AssignCards(List<CardView> cards)
        {
            _cards.Clear();
            _cards.AddRange(cards);
            UpdateCardPositions();
        }

        public void AddCards(List<CardView> cards)
        {
            _cards.AddRange(cards);
            UpdateCardPositions();
        }

        public List<CardView> RemoveTopCards(int count)
        {
            int startIndex = _cards.Count - count;
            var removed = new List<CardView>(count);
            for (int cardIndex = startIndex; cardIndex < _cards.Count; cardIndex++)
            {
                removed.Add(_cards[cardIndex]);
            }
            _cards.RemoveRange(startIndex, count);
            UpdateCardPositions();
            return removed;
        }

        public void UpdateCardPositions()
        {
            if (_pileType == PileType.Tableau)
            {
                UpdateTableauPositions();
            }
            else
            {
                UpdateStackedPositions();
            }
        }

        public Vector3 GetCardWorldPosition(int index)
        {
            if (_pileType == PileType.Tableau)
            {
                return GetTableauWorldPosition(index);
            }
            return transform.position;
        }

        public List<CardView> GetCardViews()
        {
            return _cards;
        }

        private void UpdateStackedPositions()
        {
            Vector3 pilePosition = transform.position;
            for (int cardIndex = 0; cardIndex < _cards.Count; cardIndex++)
            {
                CardView cardView = _cards[cardIndex];
                cardView.transform.localPosition = transform.InverseTransformPoint(pilePosition);
                cardView.SetSortingOrder(cardIndex);
                cardView.SetStripMode(false);
            }
        }

        private void UpdateTableauPositions()
        {
            if (_layout == null)
            {
                return;
            }

            int lastFaceDownIndex = FindLastFaceDownIndex();

            float yOffset = 0f;
            for (int cardIndex = 0; cardIndex < _cards.Count; cardIndex++)
            {
                CardView cardView = _cards[cardIndex];
                Vector3 localPos = new Vector3(0f, -yOffset, 0f);
                cardView.transform.localPosition = localPos;
                cardView.SetSortingOrder(cardIndex);

                bool isFaceUp = cardView.Model != null && cardView.Model.IsFaceUp.Value;

                if (!isFaceUp)
                {
                    bool hasCardOnTop = cardIndex < _cards.Count - 1;
                    bool isLastFaceDown = cardIndex == lastFaceDownIndex;

                    if (hasCardOnTop && !isLastFaceDown)
                    {
                        cardView.SetStripMode(true);
                    }
                    else
                    {
                        cardView.SetStripMode(false);
                    }

                    yOffset += _layout.FaceDownYOffset;
                }
                else
                {
                    cardView.SetStripMode(false);
                    yOffset += _layout.FaceUpYOffset;
                }
            }
        }

        private int FindLastFaceDownIndex()
        {
            int lastFaceDown = -1;
            for (int cardIndex = 0; cardIndex < _cards.Count; cardIndex++)
            {
                CardView cardView = _cards[cardIndex];
                bool isFaceUp = cardView.Model != null && cardView.Model.IsFaceUp.Value;
                if (!isFaceUp)
                {
                    lastFaceDown = cardIndex;
                }
                else
                {
                    break;
                }
            }
            return lastFaceDown;
        }

        private Vector3 GetTableauWorldPosition(int index)
        {
            if (_layout == null || index < 0 || index >= _cards.Count)
            {
                return transform.position;
            }

            float yOffset = 0f;
            for (int cardIndex = 0; cardIndex < index; cardIndex++)
            {
                CardView cardView = _cards[cardIndex];
                bool isFaceUp = cardView.Model != null && cardView.Model.IsFaceUp.Value;
                yOffset += isFaceUp ? _layout.FaceUpYOffset : _layout.FaceDownYOffset;
            }

            return transform.position + new Vector3(0f, -yOffset, 0f);
        }
    }
}
