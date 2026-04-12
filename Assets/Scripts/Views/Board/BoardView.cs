using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using KlondikeSolitaire.Core;
using KlondikeSolitaire.Systems;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace KlondikeSolitaire.Views
{
    public sealed class BoardView : MonoBehaviour
    {
        [SerializeField] private CardView _cardPrefab;
        [SerializeField] private PileView[] _pileViews;
        [SerializeField] private Transform _cardPoolParent;

        private BoardModel _model;
        private CardSpriteMapping _mapping;
        private CardAnimator _animator;
        private AnimationConfig _animConfig;
        private LayoutConfig _layout;
        private AutoCompleteSystem _autoComplete;
        private MoveExecutionSystem _execution;
        private IPublisher<DealAnimationCompletedMessage> _dealAnimCompletedPublisher;

        private CardView[] _cardViews;
        private readonly Dictionary<CardModel, CardView> _cardViewMap = new(BoardModel.DECK_SIZE);
        private readonly Dictionary<CardModel, PileId> _cardPileMap = new(BoardModel.DECK_SIZE);
        private readonly Dictionary<Collider2D, CardView> _colliderToCard = new(BoardModel.DECK_SIZE);
        private readonly Dictionary<Collider2D, PileView> _colliderToPile = new();
        private CardView _pendingFlipCard;
        private CancellationTokenSource _dealCts;
        private CancellationTokenSource _autoCompleteCts;
        private CancellationTokenSource _moveAnimCts;
        private GamePhase _currentPhase;
        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public void Construct(
            BoardModel model,
            CardSpriteMapping mapping,
            CardAnimator animator,
            AnimationConfig animConfig,
            LayoutConfig layout,
            AutoCompleteSystem autoComplete,
            MoveExecutionSystem execution,
            IPublisher<DealAnimationCompletedMessage> dealAnimCompletedPublisher,
            ISubscriber<DealCompletedMessage> dealSubscriber,
            ISubscriber<CardMovedMessage> cardMovedSubscriber,
            ISubscriber<CardFlippedMessage> cardFlippedSubscriber,
            ISubscriber<GamePhaseChangedMessage> phaseSubscriber)
        {
            _model = model;
            _mapping = mapping;
            _animator = animator;
            _animConfig = animConfig;
            _layout = layout;
            _autoComplete = autoComplete;
            _execution = execution;
            _dealAnimCompletedPublisher = dealAnimCompletedPublisher;

            for (int pileIndex = 0; pileIndex < _pileViews.Length; pileIndex++)
            {
                _pileViews[pileIndex].Construct(layout);
            }

            dealSubscriber.Subscribe(OnDealCompleted).AddTo(_disposables);
            cardMovedSubscriber.Subscribe(OnCardMoved).AddTo(_disposables);
            cardFlippedSubscriber.Subscribe(OnCardFlipped).AddTo(_disposables);
            phaseSubscriber.Subscribe(OnGamePhaseChanged).AddTo(_disposables);
        }

        private void Start()
        {
            _cardViews = new CardView[BoardModel.DECK_SIZE];
            for (int cardIndex = 0; cardIndex < BoardModel.DECK_SIZE; cardIndex++)
            {
                CardView cardView = Instantiate(_cardPrefab, _cardPoolParent);
                _cardViews[cardIndex] = cardView;
                _colliderToCard[cardView.Collider] = cardView;
                cardView.gameObject.SetActive(false);
            }

            for (int pileIndex = 0; pileIndex < _pileViews.Length; pileIndex++)
            {
                PileView pileView = _pileViews[pileIndex];
                _colliderToPile[pileView.Collider] = pileView;
            }
        }

        private void OnDealCompleted(DealCompletedMessage message)
        {
            AssignCardViewsToModel();
            _dealCts = new CancellationTokenSource();
            PlayDealAnimationAsync(_dealCts.Token).Forget();
        }

        private void AssignCardViewsToModel()
        {
            _cardViewMap.Clear();
            _cardPileMap.Clear();

            int viewIndex = 0;
            PileModel[] allPiles = _model.AllPiles;

            for (int pileIndex = 0; pileIndex < allPiles.Length; pileIndex++)
            {
                PileModel pile = allPiles[pileIndex];
                IReadOnlyList<CardModel> cards = pile.Cards;

                for (int cardIndex = 0; cardIndex < cards.Count; cardIndex++)
                {
                    CardModel cardModel = cards[cardIndex];
                    CardView cardView = _cardViews[viewIndex++];
                    cardView.gameObject.SetActive(true);

                    cardView.Initialize(
                        cardModel,
                        _mapping.GetFaceSprite(cardModel.Suit, cardModel.Rank),
                        _mapping.GetFaceStripSprite(cardModel.Suit, cardModel.Rank),
                        _mapping.BackSprite,
                        _mapping.BackStripSprite,
                        _animator);

                    _cardViewMap[cardModel] = cardView;
                    _cardPileMap[cardModel] = pile.Id;
                }
            }

            for (int pileIndex = 0; pileIndex < allPiles.Length; pileIndex++)
            {
                PileModel pile = allPiles[pileIndex];
                PileView pileView = GetPileView(pile.Id);
                IReadOnlyList<CardModel> cards = pile.Cards;
                var cardViewList = new List<CardView>(cards.Count);

                for (int cardIndex = 0; cardIndex < cards.Count; cardIndex++)
                {
                    cardViewList.Add(_cardViewMap[cards[cardIndex]]);
                }

                pileView.AssignCards(cardViewList);
            }
        }

        private async UniTaskVoid PlayDealAnimationAsync(CancellationToken cancellationToken)
        {
            PileView stockView = GetPileView(PileId.Stock());
            Vector3 stockPosition = stockView.transform.position;

            for (int cardViewIndex = 0; cardViewIndex < _cardViews.Length; cardViewIndex++)
            {
                CardView cardView = _cardViews[cardViewIndex];
                if (cardView.gameObject.activeSelf)
                {
                    cardView.SetStripMode(false);
                    cardView.ResetSpriteToBack();
                    cardView.SetRendererEnabled(false);
                    cardView.transform.position = stockPosition;
                }
            }

            GetPileView(PileId.Stock()).UpdateCardPositions();

            var dealTasks = new List<UniTask>();
            float cumulativeDelay = 0f;

            PileModel[] tableau = _model.Tableau;
            CardView[] previousCardInColumn = new CardView[BoardModel.TABLEAU_COUNT];

            for (int rowIndex = 0; rowIndex < BoardModel.TABLEAU_COUNT; rowIndex++)
            {
                for (int columnIndex = rowIndex; columnIndex < BoardModel.TABLEAU_COUNT; columnIndex++)
                {
                    PileModel pile = tableau[columnIndex];
                    PileView pileView = GetPileView(pile.Id);
                    IReadOnlyList<CardModel> cards = pile.Cards;

                    CardView cardView = GetCardView(cards[rowIndex]);
                    if (cardView == null)
                    {
                        continue;
                    }

                    Vector3 pilePos = pileView.transform.position;
                    float dealYOffset = rowIndex * _layout.FaceDownYOffset;
                    Vector3 targetPosition = pilePos + new Vector3(0f, -dealYOffset, rowIndex * CardView.Z_STEP);
                    bool isTopCard = rowIndex == cards.Count - 1;
                    float delay = cumulativeDelay;

                    CardView cardToStrip = previousCardInColumn[columnIndex];
                    bool shouldStripPrevious = cardToStrip != null && (rowIndex - 1) < cards.Count - 2;

                    if (isTopCard)
                    {
                        dealTasks.Add(DealCardWithFlipAsync(cardView, targetPosition, delay, cancellationToken));
                    }
                    else
                    {
                        dealTasks.Add(DealCardAndStripPreviousAsync(
                            cardView, cardToStrip, shouldStripPrevious, targetPosition, delay, cancellationToken));
                    }

                    previousCardInColumn[columnIndex] = cardView;
                    cumulativeDelay += _animConfig.DealDelay;
                }
            }

            await UniTask.WhenAll(dealTasks);

            PileModel[] allPiles = _model.AllPiles;
            for (int pileIndex = 0; pileIndex < allPiles.Length; pileIndex++)
            {
                GetPileView(allPiles[pileIndex].Id).UpdateCardPositions();
            }

            _dealAnimCompletedPublisher.Publish(new DealAnimationCompletedMessage());
        }

        private async UniTask DealCardAndStripPreviousAsync(
            CardView cardView,
            CardView previousCard,
            bool shouldStrip,
            Vector3 targetPosition,
            float delay,
            CancellationToken cancellationToken)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: cancellationToken);
            cardView.SetRendererEnabled(true);
            await _animator.MoveCard(cardView.transform, targetPosition, cancellationToken);

            if (shouldStrip)
            {
                previousCard.SetStripMode(true);
                Vector3 pos = previousCard.transform.position;
                previousCard.transform.position = new Vector3(pos.x, pos.y + previousCard.StripAlignOffset, pos.z);
            }
        }

        private async UniTask DealCardWithFlipAsync(CardView cardView, Vector3 target, float delay, CancellationToken cancellationToken)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: cancellationToken);
            cardView.SetRendererEnabled(true);
            await _animator.MoveCard(cardView.transform, target, cancellationToken);
            await cardView.PlayFlipAnimation(true, cancellationToken);
        }

        private void OnCardMoved(CardMovedMessage message)
        {
            PileView sourcePileView = GetPileView(message.SourcePileId);
            PileView destPileView = GetPileView(message.DestPileId);

            List<CardView> movedCards = sourcePileView.RemoveTopCards(message.CardCount);
            if (message.IsReversed)
            {
                movedCards.Reverse();
            }
            destPileView.AddCards(movedCards);

            PileModel destPile = _model.GetPile(message.DestPileId);
            IReadOnlyList<CardModel> destCards = destPile.Cards;
            for (int cardIndex = destCards.Count - message.CardCount; cardIndex < destCards.Count; cardIndex++)
            {
                _cardPileMap[destCards[cardIndex]] = message.DestPileId;
            }

            _moveAnimCts ??= new CancellationTokenSource();
            CancellationToken token = _moveAnimCts.Token;

            if (message.SourcePileId.Type == PileType.Stock && message.DestPileId.Type == PileType.Waste && message.CardCount == 1)
            {
                AnimateStockDrawAsync(movedCards[0], destPileView, token).Forget();
            }
            else
            {
                AnimateCardMoveAsync(movedCards, destPileView, token).Forget();
            }
        }

        private async UniTaskVoid AnimateStockDrawAsync(CardView cardView, PileView wastePileView, CancellationToken cancellationToken)
        {
            cardView.ResetSpriteToBack();
            cardView.SetRendererEnabled(true);
            cardView.SetColliderEnabled(false);

            const float DRAW_Z = -1f;
            Vector3 startPos = cardView.transform.position;
            cardView.transform.position = new Vector3(startPos.x, startPos.y, DRAW_Z);

            Vector3 wastePos = wastePileView.transform.position;
            Vector3 moveTarget = new Vector3(wastePos.x, wastePos.y, DRAW_Z);

            await _animator.MoveCard(cardView.transform, moveTarget, cancellationToken);
            await cardView.PlayFlipAnimation(true, cancellationToken);

            cardView.SetColliderEnabled(true);
            wastePileView.UpdateCardPositions();
        }

        private async UniTaskVoid AnimateCardMoveAsync(List<CardView> movedCards, PileView destPileView, CancellationToken cancellationToken)
        {
            CardView flipCard = _pendingFlipCard;
            _pendingFlipCard = null;

            if (flipCard != null)
            {
                flipCard.ResetSpriteToBack();
            }

            IReadOnlyList<CardView> allDestCards = destPileView.GetCardViews();
            int destCardCount = allDestCards.Count;
            int movedCount = movedCards.Count;
            int startIndex = destCardCount - movedCount;

            for (int cardIndex = 0; cardIndex < movedCount; cardIndex++)
            {
                movedCards[cardIndex].SetColliderEnabled(false);
            }

            var moveTasks = new List<UniTask>(movedCount);
            for (int cardIndex = 0; cardIndex < movedCount; cardIndex++)
            {
                CardView cardView = movedCards[cardIndex];
                Vector3 targetPosition = destPileView.GetCardWorldPosition(startIndex + cardIndex);
                moveTasks.Add(_animator.MoveCard(cardView.transform, targetPosition, cancellationToken));
            }

            await UniTask.WhenAll(moveTasks);

            for (int cardIndex = 0; cardIndex < movedCount; cardIndex++)
            {
                movedCards[cardIndex].SetColliderEnabled(true);
            }

            destPileView.UpdateCardPositions();

            if (flipCard != null)
            {
                await flipCard.PlayFlipAnimation(true, cancellationToken);
            }
        }

        private void OnCardFlipped(CardFlippedMessage message)
        {
            PileModel pileModel = _model.GetPile(message.PileId);

            if (message.CardIndex >= 0 && message.CardIndex < pileModel.Count)
            {
                CardModel cardModel = pileModel.Cards[message.CardIndex];
                CardView cardView = GetCardView(cardModel);

                if (cardView != null && cardModel.IsFaceUp.Value)
                {
                    _pendingFlipCard = cardView;
                }
            }
        }

        private void OnGamePhaseChanged(GamePhaseChangedMessage message)
        {
            _currentPhase = message.NewPhase;

            if (message.NewPhase == GamePhase.Dealing)
            {
                CancelAndDispose(ref _dealCts);
                CancelAndDispose(ref _autoCompleteCts);
                CancelAndDispose(ref _moveAnimCts);

                _animator.KillAllOnTargets(_cardViews);

                for (int cardIndex = 0; cardIndex < _cardViews.Length; cardIndex++)
                {
                    CardView cardView = _cardViews[cardIndex];
                    cardView.transform.SetParent(_cardPoolParent);
                    cardView.transform.localScale = Vector3.one;
                    cardView.gameObject.SetActive(false);
                }

                _cardViewMap.Clear();
                _cardPileMap.Clear();

                for (int pileIndex = 0; pileIndex < _pileViews.Length; pileIndex++)
                {
                    _pileViews[pileIndex].ClearCards();
                }
            }
            else if (message.NewPhase == GamePhase.AutoCompleting)
            {
                RunAutoCompleteAsync().Forget();
            }
        }

        private async UniTaskVoid RunAutoCompleteAsync()
        {
            CancelAndDispose(ref _autoCompleteCts);
            _autoCompleteCts = new CancellationTokenSource();
            CancellationToken token = _autoCompleteCts.Token;

            List<Move> moves = _autoComplete.GenerateMoveSequence();

            for (int moveIndex = 0; moveIndex < moves.Count; moveIndex++)
            {
                if (_currentPhase != GamePhase.AutoCompleting || token.IsCancellationRequested)
                {
                    break;
                }

                Move move = moves[moveIndex];
                _execution.ExecuteMove(move.Source, move.Destination, move.CardCount);

                await UniTask.Delay(TimeSpan.FromSeconds(_animConfig.AutoCompleteDelay), cancellationToken: token);
            }
        }

        public PileView GetPileView(PileId pileId)
        {
            int index = pileId.Type switch
            {
                PileType.Stock => 0,
                PileType.Waste => 1,
                PileType.Foundation => 2 + pileId.Index,
                PileType.Tableau => 2 + BoardModel.FOUNDATION_COUNT + pileId.Index,
                _ => throw new System.ArgumentOutOfRangeException(nameof(pileId), pileId.Type, "Unknown PileType")
            };

            return _pileViews[index];
        }

        public CardView GetCardView(CardModel model)
        {
            _cardViewMap.TryGetValue(model, out CardView cardView);
            return cardView;
        }

        public PileView FindPileViewForCard(CardModel card)
        {
            return _cardPileMap.TryGetValue(card, out PileId pileId) ? GetPileView(pileId) : null;
        }

        public bool TryGetCardViewByCollider(Collider2D collider, out CardView cardView)
        {
            return _colliderToCard.TryGetValue(collider, out cardView);
        }

        public bool TryGetPileViewByCollider(Collider2D collider, out PileView pileView)
        {
            return _colliderToPile.TryGetValue(collider, out pileView);
        }

        private void OnDestroy()
        {
            CancelAndDispose(ref _dealCts);
            CancelAndDispose(ref _autoCompleteCts);
            CancelAndDispose(ref _moveAnimCts);
            _disposables.Dispose();
        }

        private static void CancelAndDispose(ref CancellationTokenSource cts)
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
        }
    }
}
