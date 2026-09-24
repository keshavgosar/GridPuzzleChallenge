namespace GridPuzzle.Core
{
    /// <summary>
    /// A full deep copy of board state at one point in time, used by
    /// GameHistory to support Undo.
    ///
    /// Deep-copy cost is O(Rows*Cols). For typical puzzle grids (4x4 up to
    /// 8x8) that is a handful of int writes - far cheaper than the
    /// complexity of a diff/command-based undo, and it fully avoids a class
    /// of "state-tracking leak" bugs where a partially-applied diff could
    /// desync the board. History depth is capped (see GameHistory) so
    /// memory stays bounded regardless of session length.
    /// </summary>
    public sealed class GridSnapshot
    {
        public readonly int[,] Values;      // 0 == empty cell
        public readonly bool[,] Wildcards;
        public readonly int Score;
        public readonly int MovesRemaining;

        public GridSnapshot(int[,] values, bool[,] wildcards, int score, int movesRemaining)
        {
            Values = values;
            Wildcards = wildcards;
            Score = score;
            MovesRemaining = movesRemaining;
        }
    }
}
