using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using KlondikeSolitaire.Core;
using UnityEngine;

namespace KlondikeSolitaire.Views
{
    [RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
    public sealed class CardView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private BoxCollider2D _collider;

        private CardModel _model;
        private Sprite _faceSprite;
        private Sprite _faceStripSprite;
        private Sprite _backSprite;
        private Sprite _backStripSprite;
        private CardAnimator _animator;
        private bool _isStripMode;
        private float _backStripAlignOffset;
        private float _faceStripAlignOffset;
        private IDisposable _faceUpSubscription;

        public const float Z_STEP = -0.01f;

        public CardModel Model => _model;
        public SpriteRenderer SpriteRenderer => _spriteRenderer;
        public BoxCollider2D Collider => _collider;

        public float StripAlignOffset => _model.IsFaceUp.Value ? _faceStripAlignOffset : _backStripAlignOffset;

        public void Initialize(CardModel model, Sprite faceSprite, Sprite faceStripSprite, Sprite backSprite, Sprite backStripSprite, CardAnimator animator)
        {
            _faceUpSubscription?.Dispose();

            _model = model;
            _faceSprite = faceSprite;
            _faceStripSprite = faceStripSprite;
            _backSprite = backSprite;
            _backStripSprite = backStripSprite;
            _animator = animator;
            _backStripAlignOffset = (_backSprite.bounds.size.y - _backStripSprite.bounds.size.y) * 0.5f;
            _faceStripAlignOffset = (_faceSprite.bounds.size.y - _faceStripSprite.bounds.size.y) * 0.5f;

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

        public UniTask PlayFlipAnimation(bool toFaceUp, CancellationToken cancellationToken = default)
        {
            return _animator.FlipCard(_spriteRenderer, _faceSprite, _backSprite, toFaceUp, cancellationToken);
        }

        public void SetStripMode(bool strip)
        {
            _isStripMode = strip;
            UpdateSprite(_model.IsFaceUp.Value);
        }

        private void OnFaceUpChanged(bool isFaceUp)
        {
            UpdateSprite(isFaceUp);
        }

        private void UpdateSprite(bool isFaceUp)
        {
            if (isFaceUp)
            {
                _spriteRenderer.sprite = _isStripMode ? _faceStripSprite : _faceSprite;
            }
            else
            {
                _spriteRenderer.sprite = _isStripMode ? _backStripSprite : _backSprite;
            }
        }

        private void OnValidate()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }
            if (_collider == null)
            {
                _collider = GetComponent<BoxCollider2D>();
            }
        }

        private void OnDestroy()
        {
            _faceUpSubscription?.Dispose();
        }
    }
}
