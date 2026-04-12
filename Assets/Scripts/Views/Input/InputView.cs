using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using KlondikeSolitaire.Core;
using KlondikeSolitaire.Input;
using KlondikeSolitaire.Systems;
using MessagePipe;
using UnityEngine;
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

        private SolitaireControls _controls;
        private Camera _mainCamera;
        private readonly Collider2D[] _raycastBuffer = new Collider2D[20];
        private readonly CompositeDisposable _disposables = new();

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
            ISubscriber<GamePhaseChangedMessage> phaseSubscriber)
        {
            _validation = validation;
            _execution = execution;
            _dragView = dragView;
            _boardView = boardView;
            _board = board;
            _config = config;

            phaseSubscriber.Subscribe(OnGamePhaseChanged).AddTo(_disposables);
            _isInputEnabled = phase.Phase.Value == GamePhase.Playing;
        }

        private void Awake()
        {
            _mainCamera = Camera.main;
            _dragCardBuffer = new CardView[DragView.MAX_DRAG_COUNT];
            _controls = new SolitaireControls();
        }

        private void OnEnable()
        {
            _controls.Game.Enable();
        }

        private void OnDisable()
        {
            _controls.Game.Disable();

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

            Vector2 screenPos = _controls.Game.Position.ReadValue<Vector2>();

            if (_controls.Game.Press.WasPressedThisFrame())
            {
                HandlePointerDown(screenPos);
            }
            else if (_controls.Game.Press.IsPressed() && _pressingDown)
            {
                HandlePointerMove(screenPos);
            }
            else if (_controls.Game.Press.WasReleasedThisFrame() && _pressingDown)
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

            Physics2D.SyncTransforms();

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

            if (pileView.PileId.Type == PileType.Stock)
            {
                HandleStockTap(pileView);
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
                float pressDuration = Time.unscaledTime - _pressStartTime;
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

            Vector3 worldPos = ScreenToWorld(screenPos);
            _dragView.BeginDrag(_dragCardBuffer, actualCount, _pressedPile, worldPos);
        }

        private async UniTaskVoid HandleDragRelease(Vector2 screenPos)
        {
            PileView sourcePile = _pressedPile;
            int cardCount = _draggableCount;

            Physics2D.SyncTransforms();

            Vector3 worldPos = ScreenToWorld(screenPos);
            Vector2 worldPos2D = new Vector2(worldPos.x, worldPos.y);

            PileView targetPile = FindPileViewAt(worldPos2D);

            if (targetPile != null && targetPile != sourcePile && sourcePile != null)
            {
                bool isValid = _validation.IsValidMove(_board, sourcePile.PileId, targetPile.PileId, cardCount);
                if (isValid)
                {
                    _dragView.FinishDrag();
                    _execution.ExecuteMove(sourcePile.PileId, targetPile.PileId, cardCount);
                    return;
                }
            }

            await _dragView.CancelDrag();

            if (sourcePile != null)
            {
                sourcePile.UpdateCardPositions();
            }
        }

        private void HandleTap(CardView card, PileView pileView)
        {
            PileId sourcePileId = pileView.PileId;
            float now = Time.unscaledTime;

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
            for (int foundationIndex = 0; foundationIndex < BoardModel.FOUNDATION_COUNT; foundationIndex++)
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
            float bestZ = float.MaxValue;

            for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                Collider2D col = _raycastBuffer[hitIndex];
                if (!_boardView.TryGetCardViewByCollider(col, out CardView cardView))
                {
                    continue;
                }

                float z = col.transform.position.z;
                if (z < bestZ)
                {
                    bestZ = z;
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
                if (_boardView.TryGetPileViewByCollider(col, out PileView pileView))
                {
                    float dist = Vector2.Distance(worldPos2D, pileView.transform.position);
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        closest = pileView;
                    }
                }
            }

            if (closest != null)
            {
                return closest;
            }

            for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                Collider2D col = _raycastBuffer[hitIndex];
                if (_boardView.TryGetCardViewByCollider(col, out CardView cardView) && !_dragView.IsCardBeingDragged(cardView))
                {
                    return FindPileForCard(cardView);
                }
            }

            return null;
        }

        private PileView TryGetStockPileAt(Vector2 worldPos2D)
        {
            int hitCount = Physics2D.OverlapPointNonAlloc(worldPos2D, _raycastBuffer);
            for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                if (_boardView.TryGetPileViewByCollider(_raycastBuffer[hitIndex], out PileView pileView) && pileView.PileId.Type == PileType.Stock)
                {
                    return pileView;
                }
            }
            return null;
        }

        private PileView FindPileForCard(CardView card)
        {
            return _boardView.FindPileViewForCard(card.Model);
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
            _disposables.Dispose();
            _controls?.Dispose();
        }
    }
}
