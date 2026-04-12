using KlondikeSolitaire.Core;
using MessagePipe;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace KlondikeSolitaire.Views
{
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private Button _undoButton;
        [SerializeField] private Button _hintButton;
        [SerializeField] private Button _newGameButton;
        [SerializeField] private Button _autoCompleteButton;
        [SerializeField] private CanvasGroup _autoCompleteGroup;

        private IPublisher<UndoRequestedMessage> _undoPublisher;
        private IPublisher<HintRequestedMessage> _hintPublisher;
        private IPublisher<NewGameRequestedMessage> _newGamePublisher;
        private IPublisher<AutoCompleteRequestedMessage> _autoCompletePublisher;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public void Construct(
            ISubscriber<ScoreChangedMessage> scoreSubscriber,
            ISubscriber<UndoAvailabilityChangedMessage> undoSubscriber,
            ISubscriber<AutoCompleteAvailableMessage> autoCompleteSubscriber,
            ISubscriber<GamePhaseChangedMessage> phaseSubscriber,
            IPublisher<UndoRequestedMessage> undoPublisher,
            IPublisher<HintRequestedMessage> hintPublisher,
            IPublisher<NewGameRequestedMessage> newGamePublisher,
            IPublisher<AutoCompleteRequestedMessage> autoCompletePublisher)
        {
            _undoPublisher = undoPublisher;
            _hintPublisher = hintPublisher;
            _newGamePublisher = newGamePublisher;
            _autoCompletePublisher = autoCompletePublisher;

            scoreSubscriber.Subscribe(OnScoreChanged).AddTo(_disposables);
            undoSubscriber.Subscribe(OnUndoAvailabilityChanged).AddTo(_disposables);
            autoCompleteSubscriber.Subscribe(OnAutoCompleteAvailable).AddTo(_disposables);
            phaseSubscriber.Subscribe(OnGamePhaseChanged).AddTo(_disposables);
        }

        private void OnEnable()
        {
            _undoButton.onClick.AddListener(OnUndoClicked);
            _hintButton.onClick.AddListener(OnHintClicked);
            _newGameButton.onClick.AddListener(OnNewGameClicked);
            _autoCompleteButton.onClick.AddListener(OnAutoCompleteClicked);
        }

        private void OnDisable()
        {
            _undoButton.onClick.RemoveListener(OnUndoClicked);
            _hintButton.onClick.RemoveListener(OnHintClicked);
            _newGameButton.onClick.RemoveListener(OnNewGameClicked);
            _autoCompleteButton.onClick.RemoveListener(OnAutoCompleteClicked);
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }

        private void OnScoreChanged(ScoreChangedMessage message)
        {
            _scoreText.text = message.NewScore.ToString();
        }

        private void OnUndoAvailabilityChanged(UndoAvailabilityChangedMessage message)
        {
            _undoButton.interactable = message.IsAvailable;
        }

        private void OnAutoCompleteAvailable(AutoCompleteAvailableMessage message)
        {
            _autoCompleteGroup.alpha = message.IsAvailable ? 1f : 0f;
            _autoCompleteGroup.blocksRaycasts = message.IsAvailable;
        }

        private void OnGamePhaseChanged(GamePhaseChangedMessage message)
        {
            switch (message.NewPhase)
            {
                case GamePhase.Dealing:
                    SetAllButtonsInteractable(false);
                    break;
                case GamePhase.Playing:
                    SetAllButtonsInteractable(true);
                    break;
                case GamePhase.AutoCompleting:
                    SetAllButtonsInteractable(false);
                    break;
                case GamePhase.Won:
                case GamePhase.NoMoves:
                    _undoButton.interactable = false;
                    _hintButton.interactable = false;
                    _autoCompleteButton.interactable = false;
                    _newGameButton.interactable = true;
                    break;
            }
        }

        private void SetAllButtonsInteractable(bool interactable)
        {
            _undoButton.interactable = interactable;
            _hintButton.interactable = interactable;
            _newGameButton.interactable = interactable;
            _autoCompleteButton.interactable = interactable;
        }

        private void OnUndoClicked()
        {
            _undoPublisher.Publish(new UndoRequestedMessage());
        }

        private void OnHintClicked()
        {
            _hintPublisher.Publish(new HintRequestedMessage());
        }

        private void OnNewGameClicked()
        {
            _newGamePublisher.Publish(new NewGameRequestedMessage());
        }

        private void OnAutoCompleteClicked()
        {
            _autoCompletePublisher.Publish(new AutoCompleteRequestedMessage());
        }
    }
}
