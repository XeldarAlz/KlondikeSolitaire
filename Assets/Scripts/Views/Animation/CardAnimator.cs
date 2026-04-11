using Cysharp.Threading.Tasks;
using PrimeTween;
using TMPro;
using UnityEngine;

namespace KlondikeSolitaire.Views
{
    public static class CardAnimator
    {
        public static async UniTask MoveCard(Transform card, Vector3 target, AnimationConfig config)
        {
            await Tween.Position(card, target, config.MoveDuration, Ease.OutCubic);
        }

        public static async UniTask FlipCard(SpriteRenderer renderer, Sprite faceSprite, Sprite backSprite, bool toFaceUp, AnimationConfig config)
        {
            float halfDuration = config.FlipDuration * 0.5f;
            float originalScaleX = renderer.transform.localScale.x;

            await Tween.ScaleX(renderer.transform, 0f, halfDuration, Ease.InCubic);

            renderer.sprite = toFaceUp ? faceSprite : backSprite;

            await Tween.ScaleX(renderer.transform, originalScaleX, halfDuration, Ease.OutCubic);
        }

        public static async UniTask ShakeCard(Transform card, AnimationConfig config)
        {
            await Tween.ShakeLocalPosition(card, new Vector3(config.ShakeAmplitude, 0f, 0f), config.SnapBackDuration);
        }

        public static async UniTask DealCard(Transform card, Vector3 target, float delay, AnimationConfig config)
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(delay));
            await Tween.Position(card, target, config.MoveDuration, Ease.OutCubic);
        }

        public static async UniTask AnimateScore(TMP_Text text, int from, int to, AnimationConfig config)
        {
            await Tween.Custom(text, (float)from, (float)to, config.MoveDuration, (target, value) =>
            {
                target.text = Mathf.RoundToInt(value).ToString();
            }, Ease.OutCubic);
        }

        public static void KillAll()
        {
            Tween.StopAll();
        }
    }
}
