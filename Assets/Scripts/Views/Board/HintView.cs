using System.Collections.Generic;
using KlondikeSolitaire.Core;
using MessagePipe;
using PrimeTween;
using UnityEngine;
using VContainer;

namespace KlondikeSolitaire.Views
{
    public sealed class HintView : MonoBehaviour
    {
        private BoardView _boardView;
        private BoardModel _boardModel;

        private readonly List<CardView> _highlightedCards = new();
        private readonly List<Vector3> _originalPositions = new();
        private readonly List<Tween> _activeTweens = new();
        private readonly CompositeDisposable _disposables = new();

        private const float NUDGE_DISTANCE = 0.15f;
        private const float NUDGE_DURATION = 0.2f;
        private const float PULSE_DURATION = 0.5f;
        private static readonly Color HighlightColor = new(1f, 0.85f, 0.4f, 1f);

        [Inject]
        public void Construct(
            BoardView boardView,
            BoardModel boardModel,
            ISubscriber<HintHighlightMessage> hintSubscriber,
            ISubscriber<HintClearedMessage> clearedSubscriber)
        {
            _boardView = boardView;
            _boardModel = boardModel;

            hintSubscriber.Subscribe(OnHintHighlight).AddTo(_disposables);
            clearedSubscriber.Subscribe(OnHintCleared).AddTo(_disposables);
        }

        private void OnHintHighlight(HintHighlightMessage message)
        {
            ClearHighlights();

            PileModel sourcePile = _boardModel.GetPile(message.SourcePileId);
            IReadOnlyList<CardModel> cards = sourcePile.Cards;

            for (int cardIndex = message.SourceCardIndex; cardIndex < cards.Count; cardIndex++)
            {
                CardView cardView = _boardView.GetCardView(cards[cardIndex]);
                if (cardView != null)
                {
                    _highlightedCards.Add(cardView);
                    _originalPositions.Add(cardView.transform.position);
                }
            }

            if (_highlightedCards.Count == 0)
            {
                return;
            }

            PileView destPileView = _boardView.GetPileView(message.DestPileId);
            Vector3 destPos = destPileView.transform.position;
            Vector3 sourcePos = _highlightedCards[0].transform.position;

            Vector3 direction = (destPos - sourcePos).normalized;
            Vector3 nudgeOffset = direction * NUDGE_DISTANCE;

            for (int cardIndex = 0; cardIndex < _highlightedCards.Count; cardIndex++)
            {
                CardView card = _highlightedCards[cardIndex];
                Transform cardTransform = card.transform;
                Vector3 originalPos = cardTransform.position;
                Vector3 nudgeTarget = originalPos + nudgeOffset;

                Tween nudge = Tween.Position(cardTransform, nudgeTarget, NUDGE_DURATION,
                    Ease.OutCubic, cycles: 2, cycleMode: CycleMode.Yoyo);
                _activeTweens.Add(nudge);

                Tween pulse = Tween.Color(card.SpriteRenderer, Color.white, HighlightColor,
                    PULSE_DURATION, Ease.InOutSine, cycles: -1, cycleMode: CycleMode.Yoyo,
                    startDelay: NUDGE_DURATION * 2f);
                _activeTweens.Add(pulse);
            }
        }

        private void OnHintCleared(HintClearedMessage message)
        {
            ClearHighlights();
        }

        private void ClearHighlights()
        {
            for (int tweenIndex = 0; tweenIndex < _activeTweens.Count; tweenIndex++)
            {
                Tween tween = _activeTweens[tweenIndex];
                if (tween.isAlive)
                {
                    tween.Stop();
                }
            }
            _activeTweens.Clear();

            for (int cardIndex = 0; cardIndex < _highlightedCards.Count; cardIndex++)
            {
                _highlightedCards[cardIndex].SpriteRenderer.color = Color.white;
                if (cardIndex < _originalPositions.Count)
                {
                    _highlightedCards[cardIndex].transform.position = _originalPositions[cardIndex];
                }
            }
            _highlightedCards.Clear();
            _originalPositions.Clear();
        }

        private void OnDestroy()
        {
            ClearHighlights();
            _disposables.Dispose();
        }
    }
}
