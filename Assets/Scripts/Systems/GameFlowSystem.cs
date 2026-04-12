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
        private readonly ScoringSystem _scoringSystem;
        private readonly UndoSystem _undoSystem;
        private readonly HintSystem _hintSystem;
        private readonly IPublisher<GamePhaseChangedMessage> _phaseChangedPublisher;
        private readonly IPublisher<WinDetectedMessage> _winDetectedPublisher;
        private readonly CompositeDisposable _disposables;

        public GameFlowSystem(
            GamePhaseModel gamePhase,
            DealSystem dealSystem,
            BoardModel board,
            ScoreModel scoreModel,
            ScoringSystem scoringSystem,
            UndoSystem undoSystem,
            HintSystem hintSystem,
            ISubscriber<BoardStateChangedMessage> boardStateSubscriber,
            ISubscriber<NoMovesDetectedMessage> noMovesSubscriber,
            ISubscriber<NewGameRequestedMessage> newGameSubscriber,
            IPublisher<GamePhaseChangedMessage> phaseChangedPublisher,
            IPublisher<WinDetectedMessage> winDetectedPublisher)
        {
            _gamePhase = gamePhase;
            _dealSystem = dealSystem;
            _board = board;
            _scoreModel = scoreModel;
            _scoringSystem = scoringSystem;
            _undoSystem = undoSystem;
            _hintSystem = hintSystem;
            _phaseChangedPublisher = phaseChangedPublisher;
            _winDetectedPublisher = winDetectedPublisher;

            _disposables = new CompositeDisposable();

            boardStateSubscriber.Subscribe(OnBoardStateChanged).AddTo(_disposables);
            noMovesSubscriber.Subscribe(OnNoMovesDetected).AddTo(_disposables);
            newGameSubscriber.Subscribe(OnNewGameRequested).AddTo(_disposables);
        }

        public void StartNewGame()
        {
            _gamePhase.Phase.Value = GamePhase.Dealing;
            _phaseChangedPublisher.Publish(new GamePhaseChangedMessage(GamePhase.Dealing));

            _scoringSystem.Reset();
            _undoSystem.Clear();
            _hintSystem.Reset();

            _dealSystem.CreateDeal();

            _gamePhase.Phase.Value = GamePhase.Playing;
            _phaseChangedPublisher.Publish(new GamePhaseChangedMessage(GamePhase.Playing));
        }

        public void StartAutoComplete()
        {
            _gamePhase.Phase.Value = GamePhase.AutoCompleting;
            _phaseChangedPublisher.Publish(new GamePhaseChangedMessage(GamePhase.AutoCompleting));
        }

        public void Dispose()
        {
            _disposables.Dispose();
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
                if (_board.Foundations[foundationIndex].Count != BoardModel.RANK_COUNT)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
