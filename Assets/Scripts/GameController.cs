using GridPuzzle.Config;
using GridPuzzle.Core;
using GridPuzzle.Input;
using GridPuzzle.Presentation;
using UnityEngine;

namespace GridPuzzle
{
    /// <summary>
    /// The single class allowed to know about ALL other layers. It reads
    /// input events, drives the core GridModel, and pushes the resulting
    /// MoveResult into the presentation layer. Input, Core and Presentation
    /// never reference each other directly - only GameController does,
    /// which is what makes each layer independently swappable/testable.
    ///
    /// Data flow per the required diagram:
    /// User Gesture -> SwipeInputController -> GameController.Move()
    ///   -> GridModel.Move() -> MoveResult -> GridView/UIManager
    /// </summary>
    public sealed class GameController : MonoBehaviour
    {
        [SerializeField] private GridConfig config;
        [SerializeField] private SwipeInputController inputController;
        [SerializeField] private GridView gridView;
        [SerializeField] private UIManager uiManager;

        private GridModel _model;
        private GameHistory _history;
        private bool _acceptingInput;

        private void Awake()
        {
            _model = new GridModel(
                config.rows, config.cols, config.mode, config.winValue,
                config.stepLimitedMoveBudget, config.targetScoreForStepLimited,
                config.wildcardSpawnChance);

            _history = new GameHistory(config.maxUndoDepth);

            gridView.Init(config.rows, config.cols);
            inputController.SetThreshold(config.swipeThresholdPixels);
        }

        private void OnEnable()
        {
            inputController.OnSwipe += HandleSwipe;
            uiManager.OnUndoRequested += HandleUndo;
        }

        private void OnDisable()
        {
            inputController.OnSwipe -= HandleSwipe;
            uiManager.OnUndoRequested -= HandleUndo;
        }

        private void Start() => StartNewGame();

        public void StartNewGame()
        {
            _history.Clear();
            _model.StartNewGame();
            uiManager.ResetForNewGame();
            uiManager.UpdateScore(0);
            uiManager.UpdateSteps(_model.MovesRemaining, config.mode == GameMode.StepLimited);
            uiManager.SetUndoAvailable(false);
            gridView.Redraw(_model);
            _acceptingInput = true;
        }

        private void HandleSwipe(Direction dir)
        {
            if (!_acceptingInput) return;

            // Snapshot before mutating, so Undo can always return to this point.
            var snapshotBeforeMove = _model.Snapshot();

            var result = _model.Move(dir);
            if (!result.AnyTileMoved) return; // illegal/no-op move: don't pollute history

            _history.Push(snapshotBeforeMove);
            uiManager.SetUndoAvailable(_history.CanUndo);

            gridView.Animate(result);
            uiManager.UpdateScore(_model.Score);
            uiManager.UpdateSteps(result.MovesRemaining, config.mode == GameMode.StepLimited);

            if (result.IsWin)
            {
                _acceptingInput = false;
                uiManager.ShowWin();
            }
            else if (result.IsGameOver)
            {
                _acceptingInput = false;
                uiManager.ShowGameOver();
            }
        }

        private void HandleUndo()
        {
            var snapshot = _history.Pop();
            if (snapshot == null) return;

            _model.Restore(snapshot);
            gridView.Redraw(_model);
            uiManager.UpdateScore(_model.Score);
            uiManager.UpdateSteps(_model.MovesRemaining, config.mode == GameMode.StepLimited);
            uiManager.SetUndoAvailable(_history.CanUndo);
            uiManager.ResetForNewGame(); // clears any win/lose panel if undo happened right after
            _acceptingInput = true;
        }
    }
}
