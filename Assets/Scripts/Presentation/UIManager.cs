using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GridPuzzle.Presentation
{
    /// <summary>
    /// Renders score/steps HUD and win/lose overlays. Reads only the plain
    /// values it's given by GameController - it has no reference to
    /// GridModel and cannot mutate game state directly (aside from raising
    /// the Undo button click, which GameController listens to).
    /// </summary>
    public sealed class UIManager : MonoBehaviour
    {
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text stepsText;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject winPanel;
        [SerializeField] private Button undoButton;

        public System.Action OnUndoRequested;

        private void Awake()
        {
            undoButton.onClick.AddListener(() => OnUndoRequested?.Invoke());
            gameOverPanel.SetActive(false);
            winPanel.SetActive(false);
        }

        public void UpdateScore(int score) => scoreText.text = $"Score: {score}";

        public void UpdateSteps(int movesRemaining, bool stepLimitedMode)
        {
            stepsText.gameObject.SetActive(stepLimitedMode);
            if (stepLimitedMode) stepsText.text = $"Moves left: {movesRemaining}";
        }

        public void SetUndoAvailable(bool available) => undoButton.interactable = available;

        public void ShowGameOver() => gameOverPanel.SetActive(true);
        public void ShowWin() => winPanel.SetActive(true);

        public void ResetForNewGame()
        {
            gameOverPanel.SetActive(false);
            winPanel.SetActive(false);
        }
    }
}
