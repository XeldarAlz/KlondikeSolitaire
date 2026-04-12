using System.Collections.Generic;
using KlondikeSolitaire.Core;
using UnityEngine;

namespace KlondikeSolitaire.Views
{
    public sealed class PileView : MonoBehaviour
    {
        [SerializeField] private PileType _pileType;
        [SerializeField] private int _pileIndex;
        [SerializeField] private Collider2D _collider;

        private LayoutConfig _layout;
        private readonly List<CardView> _cards = new();

        public PileId PileId => new(_pileType, _pileIndex);
        public Collider2D Collider => _collider;

        public void Construct(LayoutConfig layout)
        {
            _layout = layout;
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
            Vector3 pos = transform.position;
            return new Vector3(pos.x, pos.y, index * CardView.Z_STEP);
        }

        public IReadOnlyList<CardView> GetCardViews()
        {
            return _cards;
        }

        public void ClearCards()
        {
            _cards.Clear();
            UpdateCardPositions();
        }

        private void OnValidate()
        {
            if (_collider == null)
            {
                _collider = GetComponent<Collider2D>();
            }
        }

        private void UpdateStackedPositions()
        {
            Vector3 pilePosition = transform.position;
            int lastIndex = _cards.Count - 1;
            for (int cardIndex = 0; cardIndex < _cards.Count; cardIndex++)
            {
                CardView cardView = _cards[cardIndex];
                cardView.transform.position = pilePosition;
                cardView.SetSortingOrder(cardIndex);
                cardView.SetStripMode(false);
                cardView.SetRendererEnabled(cardIndex == lastIndex);
            }
        }

        private void UpdateTableauPositions()
        {
            float yOffset = 0f;
            Vector3 pilePosition = transform.position;

            for (int cardIndex = 0; cardIndex < _cards.Count; cardIndex++)
            {
                CardView cardView = _cards[cardIndex];
                bool isFaceUp = cardView.Model.IsFaceUp.Value;
                bool isStrip = IsCardStripped(cardIndex);

                cardView.SetStripMode(isStrip);
                float alignOffset = isStrip ? cardView.StripAlignOffset : 0f;
                cardView.transform.position = new Vector3(pilePosition.x, pilePosition.y - yOffset + alignOffset, pilePosition.z);
                yOffset += isFaceUp ? _layout.FaceUpYOffset : _layout.FaceDownYOffset;

                cardView.SetSortingOrder(cardIndex);
                cardView.SetRendererEnabled(true);
            }
        }

        private float ComputeTableauYOffset(int upToIndex)
        {
            float yOffset = 0f;
            for (int cardIndex = 0; cardIndex < upToIndex; cardIndex++)
            {
                bool isFaceUp = _cards[cardIndex].Model.IsFaceUp.Value;
                yOffset += isFaceUp ? _layout.FaceUpYOffset : _layout.FaceDownYOffset;
            }
            return yOffset;
        }

        private bool IsCardStripped(int cardIndex)
        {
            return cardIndex < _cards.Count - 1;
        }

        private Vector3 GetTableauWorldPosition(int index)
        {
            if (index < 0 || index >= _cards.Count)
            {
                return transform.position;
            }

            float yOffset = ComputeTableauYOffset(index);
            bool isStrip = IsCardStripped(index);
            float alignOffset = isStrip ? _cards[index].StripAlignOffset : 0f;

            return transform.position + new Vector3(0f, -yOffset + alignOffset, index * CardView.Z_STEP);
        }
    }
}
