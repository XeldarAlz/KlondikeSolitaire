using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using KlondikeSolitaire.Core;
using MessagePipe;
using PrimeTween;
using UnityEngine;
using VContainer;

namespace KlondikeSolitaire.Views
{
    public sealed class WinCascadeView : MonoBehaviour
    {
        [SerializeField] private GameObject _cascadeStampPrefab;
        [SerializeField] private Transform _stampPoolParent;

        private AnimationConfig _config;
        private BoardModel _boardModel;
        private CardSpriteMapping _spriteMapping;

        private const int STAMP_POOL_SIZE = 150;
        private const float GRAVITY = -9.8f;
        private const float BOUNCE_DAMPEN = 0.65f;
        private const float STAMP_INTERVAL = 0.08f;
        private const float CARD_LAUNCH_DELAY = 0.12f;
        private const float CARD_TWEEN_DURATION = 4.0f;
        private const float INITIAL_SPEED = 5.5f;

        private static int CascadeLayerId;

        private readonly SpriteRenderer[] _stampRenderers = new SpriteRenderer[STAMP_POOL_SIZE];
        private readonly GameObject[] _stampObjects = new GameObject[STAMP_POOL_SIZE];
        private int _nextStampIndex;

        private CancellationTokenSource _cascadeCts;

        private readonly List<Tween> _activeTweens = new(52);
        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public void Construct(
            AnimationConfig config,
            BoardModel boardModel,
            CardSpriteMapping spriteMapping,
            ISubscriber<WinDetectedMessage> winSubscriber,
            ISubscriber<GamePhaseChangedMessage> phaseSubscriber)
        {
            _config = config;
            _boardModel = boardModel;
            _spriteMapping = spriteMapping;

            winSubscriber.Subscribe(OnWinDetected).AddTo(_disposables);
            phaseSubscriber.Subscribe(OnGamePhaseChanged).AddTo(_disposables);
        }

        private void Start()
        {
            CascadeLayerId = SortingLayer.NameToID("Cascade");
            for (int stampIndex = 0; stampIndex < STAMP_POOL_SIZE; stampIndex++)
            {
                GameObject instance = Instantiate(_cascadeStampPrefab, _stampPoolParent);
                _stampObjects[stampIndex] = instance;
                _stampRenderers[stampIndex] = instance.GetComponent<SpriteRenderer>();
                _stampRenderers[stampIndex].sortingLayerID = CascadeLayerId;
                instance.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
            _cascadeCts?.Cancel();
            _cascadeCts?.Dispose();
        }

        private void OnWinDetected(WinDetectedMessage message)
        {
            StartCascadeAsync().Forget();
        }

        private void OnGamePhaseChanged(GamePhaseChangedMessage message)
        {
            if (message.NewPhase == GamePhase.Dealing)
            {
                StopCascade();
            }
        }

        private async UniTaskVoid StartCascadeAsync()
        {
            _cascadeCts?.Cancel();
            _cascadeCts?.Dispose();
            _cascadeCts = new CancellationTokenSource();
            CancellationToken token = _cascadeCts.Token;

            ResetStampPool();

            Camera mainCamera = Camera.main;
            float screenHalfHeight = mainCamera.orthographicSize;
            float screenHalfWidth = screenHalfHeight * mainCamera.aspect;

            float bottomBound = mainCamera.transform.position.y - screenHalfHeight;
            float leftBound = mainCamera.transform.position.x - screenHalfWidth;
            float rightBound = mainCamera.transform.position.x + screenHalfWidth;

            PileModel[] foundations = _boardModel.Foundations;

            for (int foundationIndex = 0; foundationIndex < foundations.Length; foundationIndex++)
            {
                PileModel foundation = foundations[foundationIndex];
                IReadOnlyList<CardModel> cards = foundation.Cards;

                for (int cardIndex = 0; cardIndex < cards.Count; cardIndex++)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    CardModel card = cards[cardIndex];
                    Sprite cardSprite = _spriteMapping.GetFaceSprite(card.Suit, card.Rank);

                    float directionSign = ((foundationIndex + cardIndex) % 2 == 0) ? 1f : -1f;

                    LaunchCardAsync(cardSprite, foundations[foundationIndex], cardIndex, directionSign,
                        bottomBound, leftBound, rightBound, token).Forget();

                    await UniTask.Delay(System.TimeSpan.FromSeconds(CARD_LAUNCH_DELAY), cancellationToken: token);
                }
            }
        }

        private async UniTaskVoid LaunchCardAsync(
            Sprite sprite,
            PileModel foundation,
            int cardIndex,
            float directionSign,
            float bottomBound,
            float leftBound,
            float rightBound,
            CancellationToken token)
        {
            Camera mainCamera = Camera.main;

            float screenHalfHeight = mainCamera.orthographicSize;
            float screenHalfWidth = screenHalfHeight * mainCamera.aspect;
            float startX = mainCamera.transform.position.x + foundation.PileIndex * (screenHalfWidth * 0.3f) - screenHalfWidth * 0.6f;
            float startY = mainCamera.transform.position.y + screenHalfHeight * 0.3f;

            Vector2 velocity = new Vector2(directionSign * INITIAL_SPEED, INITIAL_SPEED * 0.8f);

            float elapsed = 0f;
            Vector2 currentPos = new Vector2(startX, startY);
            float lastStampTime = -STAMP_INTERVAL;

            float cascadeSpeed = _config.CascadeSpeed > 0f ? _config.CascadeSpeed : 1f;

            Tween tween = Tween.Custom(
                this,
                0f,
                CARD_TWEEN_DURATION,
                CARD_TWEEN_DURATION / cascadeSpeed,
                (target, value) =>
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    float dt = value - elapsed;
                    elapsed = value;

                    if (dt <= 0f)
                    {
                        return;
                    }

                    velocity.y += GRAVITY * dt;
                    currentPos.x += velocity.x * dt;
                    currentPos.y += velocity.y * dt;

                    if (currentPos.y < bottomBound)
                    {
                        currentPos.y = bottomBound;
                        velocity.y = -velocity.y * BOUNCE_DAMPEN;
                    }

                    if (currentPos.x < leftBound)
                    {
                        currentPos.x = leftBound;
                        velocity.x = -velocity.x;
                    }
                    else if (currentPos.x > rightBound)
                    {
                        currentPos.x = rightBound;
                        velocity.x = -velocity.x;
                    }

                    if (elapsed - lastStampTime >= STAMP_INTERVAL)
                    {
                        lastStampTime = elapsed;
                        PlaceStamp(sprite, new Vector3(currentPos.x, currentPos.y, 0f), cardIndex);
                    }
                });

            _activeTweens.Add(tween);

            await tween;
        }

        private void PlaceStamp(Sprite sprite, Vector3 position, int sortingOrderOffset)
        {
            int stampIndex = _nextStampIndex % STAMP_POOL_SIZE;
            _nextStampIndex++;

            GameObject stampObject = _stampObjects[stampIndex];
            SpriteRenderer stampRenderer = _stampRenderers[stampIndex];

            stampRenderer.sprite = sprite;
            stampRenderer.sortingLayerID = CascadeLayerId;
            stampRenderer.sortingOrder = sortingOrderOffset;
            stampObject.transform.position = position;
            stampObject.SetActive(true);
        }

        public void StopCascade()
        {
            _cascadeCts?.Cancel();
            _cascadeCts?.Dispose();
            _cascadeCts = null;

            for (int tweenIndex = _activeTweens.Count - 1; tweenIndex >= 0; tweenIndex--)
            {
                Tween tween = _activeTweens[tweenIndex];
                if (tween.isAlive)
                {
                    tween.Stop();
                }
            }
            _activeTweens.Clear();

            ResetStampPool();
        }

        private void ResetStampPool()
        {
            for (int stampIndex = 0; stampIndex < STAMP_POOL_SIZE; stampIndex++)
            {
                _stampObjects[stampIndex].SetActive(false);
            }
            _nextStampIndex = 0;
        }
    }
}
