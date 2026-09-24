namespace GridPuzzle.Core
{
    /// <summary>How a win/loss condition is evaluated for a session.</summary>
    public enum GameMode
    {
        /// Play until the board fills and no merges are possible.
        Endless,

        /// Player has a fixed number of moves to reach the target score.
        /// Satisfies the "solve the layout within a rigid step framework" requirement.
        StepLimited
    }
}
