using KlondikeSolitaire.Core;
using MessagePipe;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace KlondikeSolitaire.Views
{
    public sealed class OverlayView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _noMovesGroup;
        [SerializeField] private CanvasGroup _winGroup;
        [SerializeField] private TMP_Text _winScoreText;
        [SerializeField] private Button _noMovesNewGameButton;
        [SerializeField] private Button _winNewGameButton;

        private IPublisher<NewGameRequestedMessage> _newGamePublisher;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public void Construct(
            ISubscriber<NoMovesDetectedMessage> noMovesSubscriber,
            ISubscriber<WinDetectedMessage> winSubscriber,
            ISubscriber<GamePhaseChangedMessage> phaseSubscriber,
            IPublisher<NewGameRequestedMessage> newGamePublisher)
        {
            _newGamePublisher = newGamePublisher;

            noMovesSubscriber.Subscribe(OnNoMovesDetected).AddTo(_disposables);
            winSubscriber.Subscribe(OnWinDetected).AddTo(_disposables);
            phaseSubscriber.Subscribe(OnGamePhaseChanged).AddTo(_disposables);
        }

        private void OnEnable()
        {
            _noMovesNewGameButton.onClick.AddListener(OnNewGameClicked);
            _winNewGameButton.onClick.AddListener(OnNewGameClicked);
        }

        private void OnDisable()
        {
            _noMovesNewGameButton.onClick.RemoveListener(OnNewGameClicked);
            _winNewGameButton.onClick.RemoveListener(OnNewGameClicked);
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }

        private void OnNoMovesDetected(NoMovesDetectedMessage message)
        {
            ShowOverlay(_noMovesGroup);
        }

        private void OnWinDetected(WinDetectedMessage message)
        {
            _winScoreText.text = message.FinalScore.ToString();
            ShowOverlay(_winGroup);
        }

        private void OnGamePhaseChanged(GamePhaseChangedMessage message)
        {
            if (message.NewPhase == GamePhase.Dealing)
            {
                HideOverlay(_noMovesGroup);
                HideOverlay(_winGroup);
            }
        }

        private void ShowOverlay(CanvasGroup group)
        {
            group.alpha = 1f;
            group.blocksRaycasts = true;
            group.interactable = true;
        }

        private void HideOverlay(CanvasGroup group)
        {
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        private void OnNewGameClicked()
        {
            _newGamePublisher.Publish(new NewGameRequestedMessage());
        }
    }
}
