using System;
using Cysharp.Threading.Tasks;
using KlondikeSolitaire.Core;
using UnityEngine;

namespace KlondikeSolitaire.Views
{
    public sealed class CardView : MonoBehaviour
    {
        private SpriteRenderer _spriteRenderer;
        private BoxCollider2D _collider;

        private CardModel _model;
        private Sprite _faceSprite;
        private Sprite _backSprite;
        private Sprite _backStripSprite;
        private CardAnimator _animator;
        private bool _isStripMode;
        private float _stripAlignOffset;
        private IDisposable _faceUpSubscription;

        public const float Z_STEP = -0.01f;

        public CardModel Model => _model;
        public SpriteRenderer SpriteRenderer => _spriteRenderer;
        public float StripAlignOffset => _stripAlignOffset;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _collider = GetComponent<BoxCollider2D>();
        }

        public void Initialize(CardModel model, Sprite faceSprite, Sprite backSprite, Sprite backStripSprite, CardAnimator animator)
        {
            _faceUpSubscription?.Dispose();

            _model = model;
            _faceSprite = faceSprite;
            _backSprite = backSprite;
            _backStripSprite = backStripSprite;
            _animator = animator;
            _stripAlignOffset = (_backSprite.bounds.size.y - _backStripSprite.bounds.size.y) * 0.5f;

            _faceUpSubscription = model.IsFaceUp.Subscribe(OnFaceUpChanged);

            UpdateSprite(model.IsFaceUp.Value);
        }

        public void SetSortingOrder(int order, float baseZ = 0f)
        {
            Vector3 pos = transform.position;
            transform.position = new Vector3(pos.x, pos.y, baseZ + order * Z_STEP);
        }

        public void SetColliderEnabled(bool enabled)
        {
            _collider.enabled = enabled;
        }

        public void SetRendererEnabled(bool enabled)
        {
            _spriteRenderer.enabled = enabled;
        }

        public void ResetSpriteToBack()
        {
            _spriteRenderer.sprite = _backSprite;
        }

        public UniTask PlayFlipAnimation(bool toFaceUp)
        {
            return _animator.FlipCard(_spriteRenderer, _faceSprite, _backSprite, toFaceUp);
        }

        public void SetStripMode(bool strip)
        {
            _isStripMode = strip;
            if (!_model.IsFaceUp.Value)
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
            _faceUpSubscription?.Dispose();
        }
    }
}
