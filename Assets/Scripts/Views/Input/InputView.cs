using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using KlondikeSolitaire.Core;
using KlondikeSolitaire.Systems;
using MessagePipe;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace KlondikeSolitaire.Views
{
    public sealed class InputView : MonoBehaviour
    {
        private MoveValidationSystem _validation;
        private MoveExecutionSystem _execution;
        private DragView _dragView;
        private BoardView _boardView;
        private BoardModel _board;
        private InputConfig _config;
        private IDisposable _phaseSubscription;

        private Camera _mainCamera;
        private readonly Collider2D[] _raycastBuffer = new Collider2D[20];

        private bool _isInputEnabled;
        private bool _pressingDown;
        private bool _dragThresholdReached;
        private Vector2 _pressStartScreenPos;
        private float _pressStartTime;

        private CardView _pressedCard;
        private PileView _pressedPile;
        private int _draggableCount;
        private CardView[] _dragCardBuffer;

        private float _lastTapTime;
        private CardView _lastTappedCard;

        [Inject]
        public void Construct(
            MoveValidationSystem validation,
            MoveExecutionSystem execution,
            DragView dragView,
            BoardView boardView,
            BoardModel board,
            InputConfig config,
            GamePhaseModel phase,
            AnimationConfig animConfig,
            ISubscriber<GamePhaseChangedMessage> phaseSubscriber)
        {
            _validation = validation;
            _execution = execution;
            _dragView = dragView;
            _boardView = boardView;
            _board = board;
            _config = config;

            _phaseSubscription = phaseSubscriber.Subscribe(OnGamePhaseChanged);
            _isInputEnabled = phase.Phase.Value == GamePhase.Playing;

            _dragView.Initialize(animConfig);
        }

        private void Awake()
        {
            _mainCamera = Camera.main;
            _dragCardBuffer = new CardView[13];
        }

        private void OnDisable()
        {
            if (_dragView != null && _dragView.IsDragging)
            {
                _dragView.CancelDrag().Forget();
            }

            _pressingDown = false;
            _dragThresholdReached = false;
            _pressedCard = null;
            _pressedPile = null;
        }

        private void Update()
        {
            if (!_isInputEnabled)
            {
                return;
            }

            Pointer pointer = Pointer.current;
            if (pointer == null)
            {
                return;
            }

            Vector2 screenPos = pointer.position.ReadValue();

            if (pointer.press.wasPressedThisFrame)
            {
                HandlePointerDown(screenPos);
            }
            else if (pointer.press.isPressed && _pressingDown)
            {
                HandlePointerMove(screenPos);
            }
            else if (pointer.press.wasReleasedThisFrame && _pressingDown)
            {
                HandlePointerUp(screenPos);
            }
        }

        private void HandlePointerDown(Vector2 screenPos)
        {
            _pressStartScreenPos = screenPos;
            _pressStartTime = Time.unscaledTime;
            _pressingDown = true;
            _dragThresholdReached = false;

            Vector3 worldPos = ScreenToWorld(screenPos);
            Vector2 worldPos2D = new Vector2(worldPos.x, worldPos.y);

            CardView hitCard = GetTopmostCardAt(worldPos2D);
            if (hitCard == null || hitCard.Model == null)
            {
                PileView stockPile = TryGetStockPileAt(worldPos2D);
                if (stockPile != null)
                {
                    HandleStockTap(stockPile);
                }
                _pressingDown = false;
                return;
            }

            PileView pileView = FindPileForCard(hitCard);
            if (pileView == null)
            {
                _pressingDown = false;
                return;
            }

            PileModel pileModel = _board.GetPile(pileView.PileId);
            int cardCount = DetermineDraggableCount(hitCard, pileModel, pileView.PileId);

            if (cardCount == 0)
            {
                _pressingDown = false;
                return;
            }

            _pressedCard = hitCard;
            _pressedPile = pileView;
            _draggableCount = cardCount;
        }

        private void HandlePointerMove(Vector2 screenPos)
        {
            if (_pressedCard == null)
            {
                return;
            }

            if (!_dragThresholdReached)
            {
                float distance = Vector2.Distance(screenPos, _pressStartScreenPos);
                if (distance < _config.DragStartThreshold)
                {
                    return;
                }

                _dragThresholdReached = true;
                BeginVisualDrag(screenPos);
            }

            if (_dragView.IsDragging)
            {
                Vector3 worldPos = ScreenToWorld(screenPos);
                _dragView.UpdateDragPosition(worldPos);
            }
        }

        private void HandlePointerUp(Vector2 screenPos)
        {
            _pressingDown = false;

            if (_dragThresholdReached && _dragView.IsDragging)
            {
                HandleDragRelease(screenPos).Forget();
            }
            else if (_pressedCard != null)
            {
                float pressDuration = (Time.unscaledTime - _pressStartTime) * 1000f;
                if (pressDuration <= _config.TapMaxDuration)
                {
                    HandleTap(_pressedCard, _pressedPile);
                }
            }

            _pressedCard = null;
            _pressedPile = null;
            _dragThresholdReached = false;
        }

        private void BeginVisualDrag(Vector2 screenPos)
        {
            if (_pressedCard == null || _pressedPile == null)
            {
                return;
            }

            PileModel pileModel = _board.GetPile(_pressedPile.PileId);
            int cardCountInPile = pileModel.Count;
            int startIndex = cardCountInPile - _draggableCount;

            int actualCount = 0;
            for (int cardIndex = startIndex; cardIndex < cardCountInPile && actualCount < _dragCardBuffer.Length; cardIndex++)
            {
                CardModel cardModel = pileModel.Cards[cardIndex];
                CardView cardView = _boardView.GetCardView(cardModel);
                if (cardView != null)
                {
                    _dragCardBuffer[actualCount++] = cardView;
                }
            }

            if (actualCount == 0)
            {
                return;
            }

            CardView[] dragCards = new CardView[actualCount];
            for (int cardIndex = 0; cardIndex < actualCount; cardIndex++)
            {
                dragCards[cardIndex] = _dragCardBuffer[cardIndex];
            }

            Vector3 worldPos = ScreenToWorld(screenPos);
            _dragView.BeginDrag(dragCards, _pressedPile, worldPos);
        }

        private async UniTaskVoid HandleDragRelease(Vector2 screenPos)
        {
            PileView sourcePile = _pressedPile;
            int cardCount = _draggableCount;

            Vector3 worldPos = ScreenToWorld(screenPos);
            Vector2 worldPos2D = new Vector2(worldPos.x, worldPos.y);

            PileView targetPile = FindPileViewAt(worldPos2D);

            if (targetPile != null && targetPile != sourcePile && sourcePile != null)
            {
                bool isValid = _validation.IsValidMove(_board, sourcePile.PileId, targetPile.PileId, cardCount);
                if (isValid)
                {
                    _execution.ExecuteMove(sourcePile.PileId, targetPile.PileId, cardCount);
                    await _dragView.CompleteDrag(targetPile);
                    return;
                }
            }

            await _dragView.CancelDrag();
        }

        private void HandleTap(CardView card, PileView pileView)
        {
            PileId sourcePileId = pileView.PileId;
            float now = Time.unscaledTime * 1000f;

            if (_lastTappedCard == card && (now - _lastTapTime) <= _config.DoubleTapWindow)
            {
                _lastTappedCard = null;
                _lastTapTime = 0f;
                HandleDoubleTap(sourcePileId);
                return;
            }

            _lastTappedCard = card;
            _lastTapTime = now;

            PileId? bestTarget = _validation.FindBestTarget(_board, sourcePileId, _draggableCount);
            if (bestTarget.HasValue)
            {
                _execution.ExecuteMove(sourcePileId, bestTarget.Value, _draggableCount);
            }
        }

        private void HandleDoubleTap(PileId sourcePileId)
        {
            for (int foundationIndex = 0; foundationIndex < 4; foundationIndex++)
            {
                PileId foundationId = PileId.Foundation(foundationIndex);
                if (_validation.IsValidMove(_board, sourcePileId, foundationId, 1))
                {
                    _execution.ExecuteMove(sourcePileId, foundationId, 1);
                    return;
                }
            }
        }

        private void HandleStockTap(PileView stockPile)
        {
            PileModel stockModel = _board.GetPile(stockPile.PileId);
            if (stockModel.Count > 0)
            {
                _execution.DrawFromStock();
            }
            else
            {
                PileModel wasteModel = _board.Waste;
                if (wasteModel.Count > 0)
                {
                    _execution.RecycleWaste();
                }
            }
        }

        private CardView GetTopmostCardAt(Vector2 worldPos2D)
        {
            int hitCount = Physics2D.OverlapPointNonAlloc(worldPos2D, _raycastBuffer);
            if (hitCount == 0)
            {
                return null;
            }

            CardView bestCard = null;
            int bestOrder = int.MinValue;

            for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                Collider2D col = _raycastBuffer[hitIndex];
                CardView cardView = col.GetComponent<CardView>();
                if (cardView == null)
                {
                    continue;
                }

                SpriteRenderer sr = col.GetComponent<SpriteRenderer>();
                if (sr == null)
                {
                    continue;
                }

                int order = sr.sortingOrder + sr.sortingLayerID * 10000;
                if (order > bestOrder)
                {
                    bestOrder = order;
                    bestCard = cardView;
                }
            }

            return bestCard;
        }

        private PileView FindPileViewAt(Vector2 worldPos2D)
        {
            int hitCount = Physics2D.OverlapPointNonAlloc(worldPos2D, _raycastBuffer);

            PileView closest = null;
            float closestDistance = float.MaxValue;

            for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                Collider2D col = _raycastBuffer[hitIndex];
                PileView pileView = col.GetComponent<PileView>();
                if (pileView != null)
                {
                    float dist = Vector2.Distance(worldPos2D, (Vector2)pileView.transform.position);
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        closest = pileView;
                    }
                }
            }

            return closest;
        }

        private PileView TryGetStockPileAt(Vector2 worldPos2D)
        {
            int hitCount = Physics2D.OverlapPointNonAlloc(worldPos2D, _raycastBuffer);
            for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                PileView pileView = _raycastBuffer[hitIndex].GetComponent<PileView>();
                if (pileView != null && pileView.PileId.Type == PileType.Stock)
                {
                    return pileView;
                }
            }
            return null;
        }

        private PileView FindPileForCard(CardView card)
        {
            PileModel[] allPiles = _board.AllPiles;
            for (int pileIndex = 0; pileIndex < allPiles.Length; pileIndex++)
            {
                PileModel pile = allPiles[pileIndex];
                IReadOnlyList<CardModel> cards = pile.Cards;
                for (int cardIndex = 0; cardIndex < cards.Count; cardIndex++)
                {
                    if (cards[cardIndex] == card.Model)
                    {
                        return _boardView.GetPileView(pile.Id);
                    }
                }
            }
            return null;
        }

        private int DetermineDraggableCount(CardView card, PileModel pile, PileId pileId)
        {
            if (pile == null || card.Model == null)
            {
                return 0;
            }

            switch (pileId.Type)
            {
                case PileType.Waste:
                    if (pile.TopCard == card.Model)
                    {
                        return 1;
                    }
                    return 0;

                case PileType.Foundation:
                    if (pile.TopCard == card.Model)
                    {
                        return 1;
                    }
                    return 0;

                case PileType.Tableau:
                    return DetermineTableauDraggableCount(card, pile);

                default:
                    return 0;
            }
        }

        private int DetermineTableauDraggableCount(CardView card, PileModel pile)
        {
            IReadOnlyList<CardModel> cards = pile.Cards;
            int clickedIndex = -1;

            for (int cardIndex = 0; cardIndex < cards.Count; cardIndex++)
            {
                if (cards[cardIndex] == card.Model)
                {
                    clickedIndex = cardIndex;
                    break;
                }
            }

            if (clickedIndex < 0 || !cards[clickedIndex].IsFaceUp.Value)
            {
                return 0;
            }

            int count = pile.Count - clickedIndex;
            return count;
        }

        private Vector3 ScreenToWorld(Vector2 screenPos)
        {
            Vector3 pos = _mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, _mainCamera.nearClipPlane));
            pos.z = 0f;
            return pos;
        }

        private void OnGamePhaseChanged(GamePhaseChangedMessage message)
        {
            _isInputEnabled = message.NewPhase == GamePhase.Playing;

            if (!_isInputEnabled && _dragView != null && _dragView.IsDragging)
            {
                _dragView.CancelDrag().Forget();
                _pressingDown = false;
                _dragThresholdReached = false;
                _pressedCard = null;
                _pressedPile = null;
            }
        }

        private void OnDestroy()
        {
            if (_phaseSubscription != null)
            {
                _phaseSubscription.Dispose();
            }
        }
    }
}
