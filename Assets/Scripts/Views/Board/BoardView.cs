using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using KlondikeSolitaire.Core;
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
        private AnimationConfig _animConfig;

        private CardView[] _cardViews;
        private readonly Dictionary<CardModel, CardView> _cardViewMap = new(52);
        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public void Construct(
            BoardModel model,
            CardSpriteMapping mapping,
            AnimationConfig animConfig,
            ISubscriber<DealCompletedMessage> dealSubscriber,
            ISubscriber<CardMovedMessage> cardMovedSubscriber,
            ISubscriber<CardFlippedMessage> cardFlippedSubscriber,
            ISubscriber<GamePhaseChangedMessage> phaseSubscriber)
        {
            _model = model;
            _mapping = mapping;
            _animConfig = animConfig;

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
            PlayDealAnimationAsync().Forget();
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
                        _mapping.BackStripSprite);

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

        private async UniTaskVoid PlayDealAnimationAsync()
        {
            PileView stockView = GetPileView(PileId.Stock());
            Vector3 stockPosition = stockView.transform.position;

            for (int cardViewIndex = 0; cardViewIndex < _cardViews.Length; cardViewIndex++)
            {
                CardView cardView = _cardViews[cardViewIndex];
                if (cardView.gameObject.activeSelf)
                {
                    cardView.transform.position = stockPosition;
                }
            }

            var dealTasks = new List<UniTask>();
            float cumulativeDelay = 0f;

            PileModel[] tableau = _model.Tableau;
            for (int tableauIndex = 0; tableauIndex < tableau.Length; tableauIndex++)
            {
                PileModel pile = tableau[tableauIndex];
                PileView pileView = GetPileView(pile.Id);
                IReadOnlyList<CardModel> cards = pile.Cards;

                for (int cardIndex = 0; cardIndex < cards.Count; cardIndex++)
                {
                    CardView cardView = GetCardView(cards[cardIndex]);
                    if (cardView == null)
                    {
                        continue;
                    }

                    Vector3 targetPosition = pileView.GetCardWorldPosition(cardIndex);
                    float delay = cumulativeDelay;
                    dealTasks.Add(CardAnimator.DealCard(cardView.transform, targetPosition, delay, _animConfig));
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

        private void OnCardMoved(CardMovedMessage message)
        {
            PileView sourcePileView = GetPileView(message.SourcePileId);
            PileView destPileView = GetPileView(message.DestPileId);

            List<CardView> movedCards = sourcePileView.RemoveTopCards(message.CardCount);
            destPileView.AddCards(movedCards);

            AnimateCardMoveAsync(movedCards, destPileView).Forget();
        }

        private async UniTaskVoid AnimateCardMoveAsync(List<CardView> movedCards, PileView destPileView)
        {
            List<CardView> allDestCards = destPileView.GetCardViews();
            int destCardCount = allDestCards.Count;
            int movedCount = movedCards.Count;
            int startIndex = destCardCount - movedCount;

            var moveTasks = new List<UniTask>(movedCount);
            for (int cardIndex = 0; cardIndex < movedCount; cardIndex++)
            {
                CardView cardView = movedCards[cardIndex];
                Vector3 targetPosition = destPileView.GetCardWorldPosition(startIndex + cardIndex);
                moveTasks.Add(CardAnimator.MoveCard(cardView.transform, targetPosition, _animConfig));
            }

            await UniTask.WhenAll(moveTasks);

            destPileView.UpdateCardPositions();
        }

        private void OnCardFlipped(CardFlippedMessage message)
        {
            PileView pileView = GetPileView(message.PileId);
            pileView.UpdateCardPositions();
        }

        private void OnGamePhaseChanged(GamePhaseChangedMessage message)
        {
            if (message.NewPhase != GamePhase.Dealing)
            {
                return;
            }

            CardAnimator.KillAll();

            for (int cardIndex = 0; cardIndex < _cardViews.Length; cardIndex++)
            {
                CardView cardView = _cardViews[cardIndex];
                cardView.transform.SetParent(_cardPoolParent);
                cardView.gameObject.SetActive(false);
            }

            _cardViewMap.Clear();

            for (int pileIndex = 0; pileIndex < _pileViews.Length; pileIndex++)
            {
                _pileViews[pileIndex].AssignCards(new List<CardView>());
            }
        }

        public async UniTaskVoid ExecuteAutoComplete(List<Move> moves)
        {
            for (int moveIndex = 0; moveIndex < moves.Count; moveIndex++)
            {
                Move move = moves[moveIndex];
                PileView sourcePileView = GetPileView(move.Source);
                PileView destPileView = GetPileView(move.Destination);

                List<CardView> movedCards = sourcePileView.RemoveTopCards(move.CardCount);
                destPileView.AddCards(movedCards);

                List<CardView> allDestCards = destPileView.GetCardViews();
                int destCardCount = allDestCards.Count;
                int startIndex = destCardCount - move.CardCount;

                var moveTasks = new List<UniTask>(move.CardCount);
                for (int cardIndex = 0; cardIndex < move.CardCount; cardIndex++)
                {
                    CardView cardView = movedCards[cardIndex];
                    Vector3 targetPosition = destPileView.GetCardWorldPosition(startIndex + cardIndex);
                    moveTasks.Add(CardAnimator.MoveCard(cardView.transform, targetPosition, _animConfig));
                }

                await UniTask.WhenAll(moveTasks);
                destPileView.UpdateCardPositions();

                await UniTask.Delay(System.TimeSpan.FromSeconds(_animConfig.AutoCompleteDelay));
            }
        }

        public PileView GetPileView(PileId pileId)
        {
            int index = pileId.Type switch
            {
                PileType.Stock => 0,
                PileType.Waste => 1,
                PileType.Foundation => 2 + pileId.Index,
                PileType.Tableau => 6 + pileId.Index,
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
            _disposables.Dispose();
        }
    }
}
