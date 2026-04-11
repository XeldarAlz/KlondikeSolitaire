using KlondikeSolitaire.Core;
using UnityEngine;

namespace KlondikeSolitaire.Views
{
    public sealed class CardView : MonoBehaviour
    {
        private SpriteRenderer _spriteRenderer;

        private CardModel _model;
        private Sprite _faceSprite;
        private Sprite _backSprite;
        private Sprite _backStripSprite;
        private bool _isStripMode;

        private static readonly int CardsLayerId = SortingLayer.NameToID("Cards");

        private readonly CompositeDisposable _disposables = new();

        public CardModel Model => _model;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Initialize(CardModel model, Sprite faceSprite, Sprite backSprite, Sprite backStripSprite)
        {
            _model = model;
            _faceSprite = faceSprite;
            _backSprite = backSprite;
            _backStripSprite = backStripSprite;

            model.IsFaceUp.Subscribe(OnFaceUpChanged).AddTo(_disposables);

            UpdateSprite(model.IsFaceUp.Value);
        }

        public void SetSortingOrder(int order)
        {
            _spriteRenderer.sortingOrder = order;
        }

        public void SetSortingLayer(int layerId)
        {
            _spriteRenderer.sortingLayerID = layerId;
        }

        public void SetHighlight(bool active)
        {
            _spriteRenderer.color = active ? Color.yellow : Color.white;
        }

        public void SetStripMode(bool strip)
        {
            _isStripMode = strip;
            if (_model != null && !_model.IsFaceUp.Value)
            {
                _spriteRenderer.sprite = _isStripMode ? _backStripSprite : _backSprite;
            }
        }

        private void OnFaceUpChanged(bool isFaceUp)
        {
            UpdateSprite(isFaceUp);
        }

        private void UpdateSprite(bool isFaceUp)
        {
            if (isFaceUp)
            {
                _spriteRenderer.sprite = _faceSprite;
            }
            else
            {
                _spriteRenderer.sprite = _isStripMode ? _backStripSprite : _backSprite;
            }
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }
    }
}
