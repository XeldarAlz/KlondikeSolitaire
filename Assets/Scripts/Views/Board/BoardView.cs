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
        [SerializeField] private GameObject _cardPrefab;
        [SerializeField] private PileView[] _pileViews;
        [SerializeField] private Transform _cardPoolParent;

        private BoardModel _model;
        private CardSpriteMapping _mapping;
        private CardAnimator _animator;
        private AnimationConfig _animConfig;
        private LayoutConfig _layout;
        private AutoCompleteSystem _autoComplete;
        private MoveExecutionSystem _execution;

        private CardView[] _cardViews;
        private readonly Dictionary<CardModel, CardView> _cardViewMap = new(52);
        private CardView _pendingFlipCard;
        private CancellationTokenSource _dealCts;
        private CancellationTokenSource _autoCompleteCts;
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
            _cardViews = new CardView[52];
            for (int cardIndex = 0; cardIndex < 52; cardIndex++)
            {
                GameObject instance = Instantiate(_cardPrefab, _cardPoolParent);
                _cardViews[cardIndex] = instance.GetComponent<CardView>();
                instance.SetActive(false);
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
                        _mapping.BackSprite,
                        _mapping.BackStripSprite,
                        _animator);

                    _cardViewMap[cardModel] = cardView;
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
                    cardView.transform.position = stockPosition;
                }
            }

            var dealTasks = new List<UniTask>();
            float cumulativeDelay = 0f;

            PileModel[] tableau = _model.Tableau;

            for (int rowIndex = 0; rowIndex < 7; rowIndex++)
            {
                for (int columnIndex = rowIndex; columnIndex < 7; columnIndex++)
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

                    if (isTopCard)
                    {
                        dealTasks.Add(DealCardWithFlipAsync(cardView, targetPosition, delay, cancellationToken));
                    }
                    else
                    {
                        dealTasks.Add(_animator.DealCard(cardView.transform, targetPosition, delay, cancellationToken));
                    }

                    cumulativeDelay += _animConfig.DealDelay;
                }
            }

            await UniTask.WhenAll(dealTasks);

            PileModel[] allPiles = _model.AllPiles;
            for (int pileIndex = 0; pileIndex < allPiles.Length; pileIndex++)
            {
                GetPileView(allPiles[pileIndex].Id).UpdateCardPositions();
            }
        }

        private async UniTask DealCardWithFlipAsync(CardView cardView, Vector3 target, float delay, CancellationToken cancellationToken)
        {
            await _animator.DealCard(cardView.transform, target, delay, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            await cardView.PlayFlipAnimation(true);
        }

        private void OnCardMoved(CardMovedMessage message)
        {
            PileView sourcePileView = GetPileView(message.SourcePileId);
            PileView destPileView = GetPileView(message.DestPileId);

            List<CardView> movedCards = sourcePileView.RemoveTopCards(message.CardCount);
            destPileView.AddCards(movedCards);

            if (message.SourcePileId.Type == PileType.Stock && message.DestPileId.Type == PileType.Waste)
            {
                AnimateStockDrawAsync(movedCards[0], destPileView).Forget();
            }
            else
            {
                AnimateCardMoveAsync(movedCards, destPileView).Forget();
            }
        }

        private async UniTaskVoid AnimateStockDrawAsync(CardView cardView, PileView wastePileView)
        {
            cardView.ResetSpriteToBack();
            cardView.SetRendererEnabled(true);
            cardView.SetColliderEnabled(false);

            const float DRAW_Z = -1f;
            Vector3 startPos = cardView.transform.position;
            cardView.transform.position = new Vector3(startPos.x, startPos.y, DRAW_Z);

            Vector3 wastePos = wastePileView.transform.position;
            Vector3 moveTarget = new Vector3(wastePos.x, wastePos.y, DRAW_Z);

            await _animator.MoveCard(cardView.transform, moveTarget);
            await cardView.PlayFlipAnimation(true);

            cardView.SetColliderEnabled(true);
            wastePileView.UpdateCardPositions();
        }

        private async UniTaskVoid AnimateCardMoveAsync(List<CardView> movedCards, PileView destPileView)
        {
            CardView flipCard = _pendingFlipCard;
            _pendingFlipCard = null;

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
                moveTasks.Add(_animator.MoveCard(cardView.transform, targetPosition));
            }

            await UniTask.WhenAll(moveTasks);

            for (int cardIndex = 0; cardIndex < movedCount; cardIndex++)
            {
                movedCards[cardIndex].SetColliderEnabled(true);
            }

            destPileView.UpdateCardPositions();

            if (flipCard != null)
            {
                await flipCard.PlayFlipAnimation(true);
            }
        }

        private void OnCardFlipped(CardFlippedMessage message)
        {
            PileView pileView = GetPileView(message.PileId);
            PileModel pileModel = _model.GetPile(message.PileId);

            if (message.CardIndex >= 0 && message.CardIndex < pileModel.Count)
            {
                CardModel cardModel = pileModel.Cards[message.CardIndex];
                CardView cardView = GetCardView(cardModel);

                if (cardView != null && cardModel.IsFaceUp.Value)
                {
                    _pendingFlipCard = cardView;
                    cardView.ResetSpriteToBack();
                }
            }

            pileView.UpdateCardPositions();
        }

        private void OnGamePhaseChanged(GamePhaseChangedMessage message)
        {
            _currentPhase = message.NewPhase;

            if (message.NewPhase == GamePhase.Dealing)
            {
                _dealCts?.Cancel();
                _dealCts?.Dispose();
                _dealCts = null;

                _autoCompleteCts?.Cancel();
                _autoCompleteCts?.Dispose();
                _autoCompleteCts = null;

                _animator.KillAllOnTargets(_cardViews);

                for (int cardIndex = 0; cardIndex < _cardViews.Length; cardIndex++)
                {
                    CardView cardView = _cardViews[cardIndex];
                    cardView.transform.SetParent(_cardPoolParent);
                    cardView.transform.localScale = Vector3.one;
                    cardView.gameObject.SetActive(false);
                }

                _cardViewMap.Clear();

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
            _autoCompleteCts?.Cancel();
            _autoCompleteCts?.Dispose();
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
                _ => 0
            };

            return _pileViews[index];
        }

        public CardView GetCardView(CardModel model)
        {
            _cardViewMap.TryGetValue(model, out CardView cardView);
            return cardView;
        }

        private void OnDestroy()
        {
            _dealCts?.Cancel();
            _dealCts?.Dispose();
            _autoCompleteCts?.Cancel();
            _autoCompleteCts?.Dispose();
            _disposables.Dispose();
        }
    }
}
