using GridPuzzle.Core;
using UnityEngine;

namespace GridPuzzle.Config
{
    /// <summary>
    /// Designer-facing tuning values. Kept as a ScriptableObject so game
    /// balance (grid size, win value, undo depth, step budget) can be
    /// iterated on in the Editor without touching code - a concrete example
    /// of separating data from logic.
    /// </summary>
    
    [CreateAssetMenu(fileName = "GridConfig", menuName = "GridPuzzle/GridConfig")]
    public sealed class GridConfig : ScriptableObject
    {
        [Header("Board")]
        public int rows = 4;
        public int cols = 4;

        [Header("Mode")]
        public GameMode mode = GameMode.Endless;
        public int winValue = 2048;
        public int stepLimitedMoveBudget = 30;
        public int targetScoreForStepLimited = 1000;

        [Header("Hook: Wildcard Tile")]
        [Range(0f, 0.3f)] public float wildcardSpawnChance = 0.06f;

        [Header("Undo")]
        public int maxUndoDepth = 20;

        [Header("Input")]
        [Tooltip("Minimum drag distance in pixels before a swipe is recognized.")]
        public float swipeThresholdPixels = 60f;
    }
}
