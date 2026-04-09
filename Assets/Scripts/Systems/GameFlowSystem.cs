using System;
using KlondikeSolitaire.Core;
using MessagePipe;

namespace KlondikeSolitaire.Systems
{
    public sealed class GameFlowSystem : IDisposable
    {
        private readonly GamePhaseModel _gamePhase;
        private readonly DealSystem _dealSystem;
        private readonly BoardModel _board;
        private readonly ScoreModel _scoreModel;
        private readonly UndoSystem _undoSystem;
        private readonly HintSystem _hintSystem;
        private readonly IPublisher<GamePhaseChangedMessage> _phaseChangedPublisher;
        private readonly IPublisher<WinDetectedMessage> _winDetectedPublisher;
        private readonly IPublisher<DealCompletedMessage> _dealCompletedPublisher;
        private readonly CompositeDisposable _subscriptions;

        public GameFlowSystem(
            GamePhaseModel gamePhase,
            DealSystem dealSystem,
            BoardModel board,
            ScoreModel scoreModel,
            UndoSystem undoSystem,
            HintSystem hintSystem,
            ISubscriber<BoardStateChangedMessage> boardStateSubscriber,
            ISubscriber<NoMovesDetectedMessage> noMovesSubscriber,
            ISubscriber<NewGameRequestedMessage> newGameSubscriber,
            IPublisher<GamePhaseChangedMessage> phaseChangedPublisher,
            IPublisher<WinDetectedMessage> winDetectedPublisher,
            IPublisher<DealCompletedMessage> dealCompletedPublisher)
        {
            _gamePhase = gamePhase ?? throw new ArgumentNullException(nameof(gamePhase));
            _dealSystem = dealSystem ?? throw new ArgumentNullException(nameof(dealSystem));
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _scoreModel = scoreModel ?? throw new ArgumentNullException(nameof(scoreModel));
            _undoSystem = undoSystem ?? throw new ArgumentNullException(nameof(undoSystem));
            _hintSystem = hintSystem ?? throw new ArgumentNullException(nameof(hintSystem));
            _phaseChangedPublisher = phaseChangedPublisher ?? throw new ArgumentNullException(nameof(phaseChangedPublisher));
            _winDetectedPublisher = winDetectedPublisher ?? throw new ArgumentNullException(nameof(winDetectedPublisher));
            _dealCompletedPublisher = dealCompletedPublisher ?? throw new ArgumentNullException(nameof(dealCompletedPublisher));

            _subscriptions = new CompositeDisposable();

            (boardStateSubscriber ?? throw new ArgumentNullException(nameof(boardStateSubscriber)))
                .Subscribe(OnBoardStateChanged)
                .AddTo(_subscriptions);

            (noMovesSubscriber ?? throw new ArgumentNullException(nameof(noMovesSubscriber)))
                .Subscribe(OnNoMovesDetected)
                .AddTo(_subscriptions);

            (newGameSubscriber ?? throw new ArgumentNullException(nameof(newGameSubscriber)))
                .Subscribe(OnNewGameRequested)
                .AddTo(_subscriptions);
        }

        public void StartNewGame()
        {
            _gamePhase.Phase.Value = GamePhase.Dealing;
            _phaseChangedPublisher.Publish(new GamePhaseChangedMessage(GamePhase.Dealing));

            _scoreModel.Score.Value = 0;
            _undoSystem.Clear();
            _hintSystem.Reset();

            _dealSystem.CreateDeal();

            _gamePhase.Phase.Value = GamePhase.Playing;
            _phaseChangedPublisher.Publish(new GamePhaseChangedMessage(GamePhase.Playing));
            _dealCompletedPublisher.Publish(new DealCompletedMessage());
        }

        public void StartAutoComplete()
        {
            _gamePhase.Phase.Value = GamePhase.AutoCompleting;
            _phaseChangedPublisher.Publish(new GamePhaseChangedMessage(GamePhase.AutoCompleting));
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
        }

        private void OnBoardStateChanged(BoardStateChangedMessage _)
        {
            GamePhase currentPhase = _gamePhase.Phase.Value;
            if (currentPhase != GamePhase.Playing && currentPhase != GamePhase.AutoCompleting)
            {
                return;
            }

            if (!CheckWin())
            {
                return;
            }

            _gamePhase.Phase.Value = GamePhase.Won;
            _winDetectedPublisher.Publish(new WinDetectedMessage(_scoreModel.Score.Value));
            _phaseChangedPublisher.Publish(new GamePhaseChangedMessage(GamePhase.Won));
        }

        private void OnNoMovesDetected(NoMovesDetectedMessage _)
        {
            _gamePhase.Phase.Value = GamePhase.NoMoves;
            _phaseChangedPublisher.Publish(new GamePhaseChangedMessage(GamePhase.NoMoves));
        }

        private void OnNewGameRequested(NewGameRequestedMessage _)
        {
            StartNewGame();
        }

        private bool CheckWin()
        {
            for (int foundationIndex = 0; foundationIndex < _board.Foundations.Length; foundationIndex++)
            {
                if (_board.Foundations[foundationIndex].Count != 13)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
