using System;
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
        [SerializeField] private SpriteRenderer _cascadeStampPrefab;
        [SerializeField] private Transform _stampPoolParent;

        private AnimationConfig _config;
        private BoardModel _boardModel;
        private CardSpriteMapping _spriteMapping;

        private const int STAMP_POOL_SIZE = 104;
        private const float GRAVITY = -14f;
        private const float BOUNCE_DAMPEN = 0.72f;
        private const float STAMP_INTERVAL = 0.12f;
        private const float CARD_LAUNCH_DELAY = 0.45f;
        private const float CARD_TWEEN_DURATION = 3.0f;
        private const float INITIAL_SPEED = 6.5f;
        private const float STAMP_LIFETIME = 0.8f;
        private const float MIN_CASCADE_SPEED = 0.8f;
        private const float FOUNDATION_X_SPACING = 0.3f;
        private const float FOUNDATION_X_OFFSET = 0.6f;
        private const float LAUNCH_Y_FRACTION = 0.3f;
        private const float INITIAL_VERTICAL_SCALE = 0.8f;

        private Camera _mainCamera;
        private int _cascadeLayerId;

        private readonly SpriteRenderer[] _stampRenderers = new SpriteRenderer[STAMP_POOL_SIZE];
        private readonly GameObject[] _stampObjects = new GameObject[STAMP_POOL_SIZE];
        private readonly float[] _stampBirthTime = new float[STAMP_POOL_SIZE];
        private int _nextStampIndex;
        private bool _isCascading;

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
            _mainCamera = Camera.main;
            _cascadeLayerId = SortingLayer.NameToID("Cascade");
            for (int stampIndex = 0; stampIndex < STAMP_POOL_SIZE; stampIndex++)
            {
                SpriteRenderer renderer = Instantiate(_cascadeStampPrefab, _stampPoolParent);
                _stampObjects[stampIndex] = renderer.gameObject;
                _stampRenderers[stampIndex] = renderer;
                renderer.sortingLayerID = _cascadeLayerId;
                renderer.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (!_isCascading)
            {
                return;
            }

            float now = Time.time;
            for (int stampIndex = 0; stampIndex < STAMP_POOL_SIZE; stampIndex++)
            {
                if (!_stampObjects[stampIndex].activeSelf)
                {
                    continue;
                }

                float age = now - _stampBirthTime[stampIndex];
                if (age > STAMP_LIFETIME)
                {
                    _stampObjects[stampIndex].SetActive(false);
                    _stampRenderers[stampIndex].color = Color.white;
                    continue;
                }

                float alpha = 1f - (age / STAMP_LIFETIME);
                Color color = _stampRenderers[stampIndex].color;
                color.a = alpha;
                _stampRenderers[stampIndex].color = color;
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
            _isCascading = true;

            float screenHalfHeight = _mainCamera.orthographicSize;
            float screenHalfWidth = screenHalfHeight * _mainCamera.aspect;

            float bottomBound = _mainCamera.transform.position.y - screenHalfHeight;
            float leftBound = _mainCamera.transform.position.x - screenHalfWidth;
            float rightBound = _mainCamera.transform.position.x + screenHalfWidth;

            PileModel[] foundations = _boardModel.Foundations;

            int maxCards = 0;
            for (int foundationIndex = 0; foundationIndex < foundations.Length; foundationIndex++)
            {
                int count = foundations[foundationIndex].Cards.Count;
                if (count > maxCards)
                {
                    maxCards = count;
                }
            }

            for (int rankOffset = 0; rankOffset < maxCards; rankOffset++)
            {
                int cardIndex = maxCards - 1 - rankOffset;

                for (int foundationIndex = 0; foundationIndex < foundations.Length; foundationIndex++)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    PileModel foundation = foundations[foundationIndex];
                    IReadOnlyList<CardModel> cards = foundation.Cards;

                    if (cardIndex >= cards.Count)
                    {
                        continue;
                    }

                    CardModel card = cards[cardIndex];
                    Sprite cardSprite = _spriteMapping.GetFaceSprite(card.Suit, card.Rank);

                    float directionSign = ((foundationIndex + cardIndex) % 2 == 0) ? 1f : -1f;

                    LaunchCardAsync(cardSprite, foundation, cardIndex, directionSign,
                        bottomBound, leftBound, rightBound, token).Forget();

                    await UniTask.Delay(TimeSpan.FromSeconds(CARD_LAUNCH_DELAY), cancellationToken: token);
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
            float screenHalfHeight = _mainCamera.orthographicSize;
            float screenHalfWidth = screenHalfHeight * _mainCamera.aspect;
            float startX = _mainCamera.transform.position.x + foundation.PileIndex * (screenHalfWidth * FOUNDATION_X_SPACING) - screenHalfWidth * FOUNDATION_X_OFFSET;
            float startY = _mainCamera.transform.position.y + screenHalfHeight * LAUNCH_Y_FRACTION;

            Vector2 velocity = new Vector2(directionSign * INITIAL_SPEED, INITIAL_SPEED * INITIAL_VERTICAL_SCALE);

            float elapsed = 0f;
            Vector2 currentPos = new Vector2(startX, startY);
            float lastStampTime = -STAMP_INTERVAL;

            float cascadeSpeed = Mathf.Max(_config.CascadeSpeed, MIN_CASCADE_SPEED);

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
                        PlaceStamp(sprite, new Vector3(currentPos.x, currentPos.y, 0f));
                    }
                });

            _activeTweens.Add(tween);

            await tween;
        }

        private void PlaceStamp(Sprite sprite, Vector3 position)
        {
            int stampIndex = _nextStampIndex % STAMP_POOL_SIZE;

            GameObject stampObject = _stampObjects[stampIndex];
            SpriteRenderer stampRenderer = _stampRenderers[stampIndex];

            stampRenderer.sprite = sprite;
            stampRenderer.color = Color.white;
            _stampBirthTime[stampIndex] = Time.time;
            stampObject.transform.position = position;
            stampObject.SetActive(true);

            _nextStampIndex++;
        }

        public void StopCascade()
        {
            _isCascading = false;
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
