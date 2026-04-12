using System.Collections.Generic;
using KlondikeSolitaire.Core;
using KlondikeSolitaire.Systems;
using KlondikeSolitaire.Views;
using UnityEditor;
using UnityEngine;
using VContainer;

namespace KlondikeSolitaire.Editor
{
    public sealed class AutoPlayWindow : EditorWindow
    {
        private const int MAX_MOVES_WITHOUT_FOUNDATION_PROGRESS = 200;
        private const int RECENT_MOVE_MEMORY = 12;
        private const int REPEAT_PENALTY = 8000;

        private MoveEnumerator _moveEnumerator;
        private MoveValidationSystem _moveValidation;
        private MoveExecutionSystem _moveExecution;
        private AutoCompleteSystem _autoComplete;
        private GameFlowSystem _gameFlow;
        private GamePhaseModel _gamePhase;
        private BoardModel _board;

        private bool _isResolved;
        private bool _isAutoPlaying;
        private float _stepInterval = 0.15f;
        private double _nextStepTime;
        private int _movesSinceFoundationProgress;
        private int _totalMoves;
        private int _lastFoundationTotal;

        private bool _isBatchRunning;
        private int _batchTotal = 100;
        private int _batchCompleted;
        private int _batchWins;
        private int _batchStuck;
        private int _batchTotalMoves;

        private readonly List<Move> _moveBuffer = new(64);
        private readonly List<Move> _recentMoves = new(RECENT_MOVE_MEMORY);

        [MenuItem("Tools/Auto Play")]
        private static void ShowWindow()
        {
            GetWindow<AutoPlayWindow>("Auto Play");
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            _isAutoPlaying = false;
            _isBatchRunning = false;
        }

        private void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingPlayMode)
            {
                _isResolved = false;
                _isAutoPlaying = false;
                _isBatchRunning = false;
                _moveEnumerator = null;
                _moveValidation = null;
                _moveExecution = null;
                _autoComplete = null;
                _gameFlow = null;
                _gamePhase = null;
                _board = null;
            }
        }

        private bool TryResolve()
        {
            if (_isResolved && _board != null && _gamePhase != null)
            {
                return true;
            }

            _isResolved = false;

            if (!Application.isPlaying)
            {
                return false;
            }

            GameLifetimeScope scope = FindAnyObjectByType<GameLifetimeScope>();
            if (scope == null || scope.Container == null)
            {
                return false;
            }

            IObjectResolver container = scope.Container;
            _board = container.Resolve<BoardModel>();
            _gamePhase = container.Resolve<GamePhaseModel>();
            _moveEnumerator = container.Resolve<MoveEnumerator>();
            _moveValidation = container.Resolve<MoveValidationSystem>();
            _moveExecution = container.Resolve<MoveExecutionSystem>();
            _autoComplete = container.Resolve<AutoCompleteSystem>();
            _gameFlow = container.Resolve<GameFlowSystem>();

            _isResolved = true;
            return true;
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to use Auto Play.", MessageType.Info);
                return;
            }

            if (!TryResolve())
            {
                EditorGUILayout.HelpBox("Waiting for GameLifetimeScope...", MessageType.Warning);
                return;
            }

            DrawBoardState();
            EditorGUILayout.Space(8);
            DrawControls();
            EditorGUILayout.Space(8);
            DrawBatchTest();
        }

        private void DrawBoardState()
        {
            EditorGUILayout.LabelField("Board State", EditorStyles.boldLabel);

            GamePhase phase = _gamePhase.Phase.Value;
            string phaseColor = phase switch
            {
                GamePhase.Playing => "white",
                GamePhase.Won => "lime",
                GamePhase.NoMoves => "red",
                GamePhase.AutoCompleting => "cyan",
                _ => "yellow"
            };

            GUIStyle richLabel = new GUIStyle(EditorStyles.label) { richText = true };

            EditorGUILayout.LabelField("Phase", $"<color={phaseColor}>{phase}</color>", richLabel);
            EditorGUILayout.LabelField("Stock", _board.Stock.Count.ToString());
            EditorGUILayout.LabelField("Waste", _board.Waste.Count.ToString());

            string foundations = $"{_board.Foundations[0].Count}  {_board.Foundations[1].Count}  {_board.Foundations[2].Count}  {_board.Foundations[3].Count}";
            EditorGUILayout.LabelField("Foundations", foundations);

            int faceDown = 0;
            for (int tableauIndex = 0; tableauIndex < BoardModel.TABLEAU_COUNT; tableauIndex++)
            {
                PileModel pile = _board.Tableau[tableauIndex];
                faceDown += pile.Count - pile.FaceUpCount;
            }
            EditorGUILayout.LabelField("Tableau Hidden", faceDown.ToString());

            if (_isAutoPlaying || _isBatchRunning)
            {
                EditorGUILayout.LabelField("Moves", _totalMoves.ToString());
                EditorGUILayout.LabelField("Stall Counter", $"{_movesSinceFoundationProgress} / {MAX_MOVES_WITHOUT_FOUNDATION_PROGRESS}");
            }
        }

        private void DrawControls()
        {
            EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);

            _stepInterval = EditorGUILayout.Slider("Step Interval (s)", _stepInterval, 0f, 1f);

            bool isPlaying = _gamePhase.Phase.Value == GamePhase.Playing;

            EditorGUI.BeginDisabledGroup(!isPlaying || _isBatchRunning);

            EditorGUILayout.BeginHorizontal();
            if (_isAutoPlaying)
            {
                if (GUILayout.Button("Stop", GUILayout.Height(30)))
                {
                    _isAutoPlaying = false;
                }
            }
            else
            {
                if (GUILayout.Button("Auto Play", GUILayout.Height(30)))
                {
                    StartAutoPlay();
                }
            }

            if (GUILayout.Button("Step", GUILayout.Height(30)))
            {
                StepOnce();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Instant Solve", GUILayout.Height(24)))
            {
                RunInstantSolve();
            }

            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(_isBatchRunning);
            if (GUILayout.Button("New Game", GUILayout.Height(24)))
            {
                _isAutoPlaying = false;
                _gameFlow.StartNewGame();
                ResetCounters();
            }
            EditorGUI.EndDisabledGroup();
        }

        private void DrawBatchTest()
        {
            EditorGUILayout.LabelField("Batch Test", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(_isBatchRunning);
            _batchTotal = EditorGUILayout.IntSlider("Games to Play", _batchTotal, 10, 1000);

            if (GUILayout.Button("Run Batch", GUILayout.Height(30)))
            {
                StartBatch();
            }
            EditorGUI.EndDisabledGroup();

            if (_isBatchRunning || _batchCompleted > 0)
            {
                EditorGUILayout.Space(4);

                if (_isBatchRunning)
                {
                    float progress = (float)_batchCompleted / _batchTotal;
                    Rect rect = GUILayoutUtility.GetRect(18, 18, "TextField");
                    EditorGUI.ProgressBar(rect, progress, $"{_batchCompleted} / {_batchTotal}");

                    if (GUILayout.Button("Cancel Batch"))
                    {
                        _isBatchRunning = false;
                    }
                }

                if (_batchCompleted > 0)
                {
                    float winRate = (float)_batchWins / _batchCompleted * 100f;
                    float avgMoves = (float)_batchTotalMoves / _batchCompleted;

                    EditorGUILayout.LabelField("Results", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("Completed", _batchCompleted.ToString());
                    EditorGUILayout.LabelField("Wins", $"{_batchWins}  ({winRate:F1}%)");
                    EditorGUILayout.LabelField("Stuck", _batchStuck.ToString());
                    EditorGUILayout.LabelField("Avg Moves", $"{avgMoves:F1}");
                }
            }
        }

        private void StartAutoPlay()
        {
            _isAutoPlaying = true;
            _nextStepTime = EditorApplication.timeSinceStartup;
            ResetCounters();
        }

        private void ResetCounters()
        {
            _totalMoves = 0;
            _movesSinceFoundationProgress = 0;
            _lastFoundationTotal = GetFoundationTotal();
            _recentMoves.Clear();
        }

        private void OnEditorUpdate()
        {
            if (!_isResolved || !Application.isPlaying)
            {
                return;
            }

            if (_isBatchRunning)
            {
                TickBatch();
                Repaint();
                return;
            }

            if (!_isAutoPlaying)
            {
                return;
            }

            GamePhase phase = _gamePhase.Phase.Value;
            if (phase == GamePhase.Won || phase == GamePhase.NoMoves)
            {
                _isAutoPlaying = false;
                Repaint();
                return;
            }

            if (phase != GamePhase.Playing)
            {
                return;
            }

            if (EditorApplication.timeSinceStartup < _nextStepTime)
            {
                return;
            }

            StepOnce();
            _nextStepTime = EditorApplication.timeSinceStartup + _stepInterval;
            Repaint();
        }

        private void StepOnce()
        {
            if (_gamePhase.Phase.Value != GamePhase.Playing)
            {
                return;
            }

            if (_autoComplete.IsAutoCompletePossible())
            {
                RunAutoComplete();
                return;
            }

            Move? move = PickBestMove();
            if (move == null)
            {
                return;
            }

            ExecuteMove(move.Value);
            TrackFoundationProgress();
        }

        private void RunInstantSolve()
        {
            ResetCounters();

            int safetyLimit = 2000;
            for (int step = 0; step < safetyLimit; step++)
            {
                GamePhase phase = _gamePhase.Phase.Value;
                if (phase == GamePhase.Won || phase == GamePhase.NoMoves)
                {
                    break;
                }

                if (phase != GamePhase.Playing)
                {
                    break;
                }

                if (_autoComplete.IsAutoCompletePossible())
                {
                    RunAutoComplete();
                    break;
                }

                Move? move = PickBestMove();
                if (move == null)
                {
                    break;
                }

                ExecuteMove(move.Value);
                TrackFoundationProgress();

                if (_movesSinceFoundationProgress >= MAX_MOVES_WITHOUT_FOUNDATION_PROGRESS)
                {
                    break;
                }
            }

            Repaint();
        }

        private void RunAutoComplete()
        {
            _gameFlow.StartAutoComplete();

            List<Move> sequence = _autoComplete.GenerateMoveSequence();
            for (int moveIndex = 0; moveIndex < sequence.Count; moveIndex++)
            {
                Move move = sequence[moveIndex];
                _moveExecution.ExecuteMove(move.Source, move.Destination, move.CardCount);
                _totalMoves++;
            }
        }

        private Move? PickBestMove()
        {
            _moveEnumerator.EnumerateAllValidMoves(_board, _moveBuffer);

            // MoveEnumerator doesn't include waste recycle — add it manually
            if (_board.Stock.Count == 0 && _board.Waste.Count > 0)
            {
                _moveBuffer.Add(new Move(PileId.Waste(), PileId.Stock(), _board.Waste.Count));
            }

            if (_moveBuffer.Count == 0)
            {
                return null;
            }

            Move? bestMove = null;
            int bestScore = int.MinValue;

            for (int moveIndex = 0; moveIndex < _moveBuffer.Count; moveIndex++)
            {
                Move move = _moveBuffer[moveIndex];
                int score = ScoreMove(move);

                int repeats = CountRecentOccurrences(move);
                score -= repeats * REPEAT_PENALTY;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
            }

            return bestMove;
        }

        private int ScoreMove(Move move)
        {
            PileModel sourcePile = _board.GetPile(move.Source);
            PileModel destPile = _board.GetPile(move.Destination);

            // Move to foundation — highest priority
            if (move.Destination.Type == PileType.Foundation)
            {
                int score = 10000;

                // Prefer moving aces and twos immediately
                CardModel card = sourcePile.TopCard;
                if (card.Rank == Rank.Ace)
                {
                    score += 500;
                }
                else if (card.Rank == Rank.Two)
                {
                    score += 400;
                }

                // Prefer foundation moves that come from waste (clears waste)
                if (move.Source.Type == PileType.Waste)
                {
                    score += 100;
                }

                return score;
            }

            // Tableau to tableau
            if (move.Source.Type == PileType.Tableau && move.Destination.Type == PileType.Tableau)
            {
                int score = 0;

                int faceDownCount = sourcePile.Count - sourcePile.FaceUpCount;
                bool revealsHidden = move.CardCount == sourcePile.FaceUpCount && faceDownCount > 0;

                // Revealing a face-down card is very valuable
                if (revealsHidden)
                {
                    score += 5000 + faceDownCount * 100;
                }

                // Moving a King to an empty column is only good if it reveals a card
                CardModel bottomMovedCard = sourcePile.Cards[sourcePile.Count - move.CardCount];
                if (bottomMovedCard.Rank == Rank.King && destPile.Count == 0)
                {
                    if (revealsHidden)
                    {
                        score += 3000;
                    }
                    else
                    {
                        // Moving King to empty with nothing to reveal — pointless
                        score -= 5000;
                    }
                }
                else if (destPile.Count == 0)
                {
                    // Non-King to empty — only if reveals
                    if (revealsHidden)
                    {
                        score += 2000;
                    }
                    else
                    {
                        score -= 3000;
                    }
                }
                else
                {
                    // Building tableau sequences
                    score += 1000;

                    // Prefer moving larger sequences
                    score += move.CardCount * 50;
                }

                return score;
            }

            // Waste to tableau
            if (move.Source.Type == PileType.Waste && move.Destination.Type == PileType.Tableau)
            {
                int score = 2000;

                // Prefer placing on non-empty tableau (building sequences)
                if (destPile.Count > 0)
                {
                    score += 500;
                }

                return score;
            }

            // Draw from stock
            if (move.Source.Type == PileType.Stock)
            {
                return -1000;
            }

            // Recycle waste (last resort)
            if (move.Source.Type == PileType.Waste && move.Destination.Type == PileType.Stock)
            {
                return -2000;
            }

            return 0;
        }

        private void ExecuteMove(Move move)
        {
            if (move.Source.Type == PileType.Stock && move.Destination.Type == PileType.Waste)
            {
                _moveExecution.DrawFromStock();
            }
            else if (move.Source.Type == PileType.Waste && move.Destination.Type == PileType.Stock)
            {
                _moveExecution.RecycleWaste();
            }
            else
            {
                _moveExecution.ExecuteMove(move.Source, move.Destination, move.CardCount);
            }

            RecordMove(move);
            _totalMoves++;
        }

        private void RecordMove(Move move)
        {
            if (_recentMoves.Count >= RECENT_MOVE_MEMORY)
            {
                _recentMoves.RemoveAt(0);
            }
            _recentMoves.Add(move);
        }

        private int CountRecentOccurrences(Move move)
        {
            int count = 0;
            for (int moveIndex = 0; moveIndex < _recentMoves.Count; moveIndex++)
            {
                Move recent = _recentMoves[moveIndex];
                // Exact same move
                if (recent == move)
                {
                    count++;
                }
                // Reverse of this move (A→B vs B→A with same card count)
                else if (recent.Source == move.Destination && recent.Destination == move.Source
                         && recent.CardCount == move.CardCount)
                {
                    count++;
                }
            }
            return count;
        }

        private void TrackFoundationProgress()
        {
            int currentTotal = GetFoundationTotal();
            if (currentTotal > _lastFoundationTotal)
            {
                _lastFoundationTotal = currentTotal;
                _movesSinceFoundationProgress = 0;
            }
            else
            {
                _movesSinceFoundationProgress++;
            }

            if (_movesSinceFoundationProgress >= MAX_MOVES_WITHOUT_FOUNDATION_PROGRESS)
            {
                _isAutoPlaying = false;
            }
        }

        private int GetFoundationTotal()
        {
            int total = 0;
            for (int foundationIndex = 0; foundationIndex < BoardModel.FOUNDATION_COUNT; foundationIndex++)
            {
                total += _board.Foundations[foundationIndex].Count;
            }
            return total;
        }

        // --- Batch testing ---

        private void StartBatch()
        {
            _isBatchRunning = true;
            _batchCompleted = 0;
            _batchWins = 0;
            _batchStuck = 0;
            _batchTotalMoves = 0;
            StartNextBatchGame();
        }

        private void StartNextBatchGame()
        {
            _gameFlow.StartNewGame();
            ResetCounters();
        }

        private void TickBatch()
        {
            if (!_isBatchRunning)
            {
                return;
            }

            GamePhase phase = _gamePhase.Phase.Value;

            // Current game finished
            if (phase == GamePhase.Won || phase == GamePhase.NoMoves
                || _movesSinceFoundationProgress >= MAX_MOVES_WITHOUT_FOUNDATION_PROGRESS)
            {
                RecordBatchResult(phase == GamePhase.Won);

                if (_batchCompleted >= _batchTotal)
                {
                    _isBatchRunning = false;
                    return;
                }

                StartNextBatchGame();
                return;
            }

            if (phase != GamePhase.Playing)
            {
                return;
            }

            // Run multiple steps per tick for speed
            int stepsPerTick = 20;
            for (int step = 0; step < stepsPerTick; step++)
            {
                phase = _gamePhase.Phase.Value;
                if (phase != GamePhase.Playing)
                {
                    break;
                }

                if (_autoComplete.IsAutoCompletePossible())
                {
                    RunAutoComplete();
                    break;
                }

                Move? move = PickBestMove();
                if (move == null)
                {
                    break;
                }

                ExecuteMove(move.Value);
                TrackFoundationProgress();

                if (_movesSinceFoundationProgress >= MAX_MOVES_WITHOUT_FOUNDATION_PROGRESS)
                {
                    break;
                }
            }
        }

        private void RecordBatchResult(bool won)
        {
            _batchCompleted++;
            _batchTotalMoves += _totalMoves;

            if (won)
            {
                _batchWins++;
            }
            else
            {
                _batchStuck++;
            }
        }
    }
}
