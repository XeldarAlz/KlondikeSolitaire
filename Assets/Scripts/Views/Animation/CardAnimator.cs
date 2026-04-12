using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;

namespace KlondikeSolitaire.Views
{
    public sealed class CardAnimator
    {
        private readonly AnimationConfig _config;

        public CardAnimator(AnimationConfig config)
        {
            _config = config;
        }

        public async UniTask MoveCard(Transform card, Vector3 target, CancellationToken cancellationToken = default)
        {
            await Tween.Position(card, target, _config.MoveDuration, Ease.OutCubic);
            cancellationToken.ThrowIfCancellationRequested();
        }

        public async UniTask FlipCard(SpriteRenderer renderer, Sprite faceSprite, Sprite backSprite, bool toFaceUp, CancellationToken cancellationToken = default)
        {
            float halfDuration = _config.FlipDuration * 0.5f;
            float originalScaleX = renderer.transform.localScale.x;

            await Tween.ScaleX(renderer.transform, 0f, halfDuration, Ease.InCubic);
            cancellationToken.ThrowIfCancellationRequested();

            renderer.sprite = toFaceUp ? faceSprite : backSprite;

            await Tween.ScaleX(renderer.transform, originalScaleX, halfDuration, Ease.OutCubic);
            cancellationToken.ThrowIfCancellationRequested();
        }

        public async UniTask ShakeCard(Transform card, CancellationToken cancellationToken = default)
        {
            await Tween.ShakeLocalPosition(card, new Vector3(_config.ShakeAmplitude, 0f, 0f), _config.SnapBackDuration);
            cancellationToken.ThrowIfCancellationRequested();
        }

        public async UniTask DealCard(Transform card, Vector3 target, float delay, CancellationToken cancellationToken = default)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: cancellationToken);
            await Tween.Position(card, target, _config.MoveDuration, Ease.OutCubic);
        }

        public void KillAllOnTargets(CardView[] cardViews)
        {
            // Null check needed: OnGamePhaseChanged can fire before Start() initializes _cardViews
            if (cardViews == null)
            {
                return;
            }

            for (int cardIndex = 0; cardIndex < cardViews.Length; cardIndex++)
            {
                CardView cardView = cardViews[cardIndex];
                if (cardView != null)
                {
                    Tween.StopAll(cardView.transform);
                }
            }
        }
    }
}
