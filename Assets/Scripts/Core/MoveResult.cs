using System.Collections.Generic;

namespace GridPuzzle.Core
{
    /// <summary>One tile's journey during a single Move() call.</summary>
    public readonly struct TileTransition
    {
        public readonly GridCoord From;
        public readonly GridCoord To;
        public readonly bool Merged;      // true if this tile merged INTO another
        public readonly int ResultValue;  // the value at "To" after the move

        public TileTransition(GridCoord from, GridCoord to, bool merged, int resultValue)
        {
            From = from;
            To = to;
            Merged = merged;
            ResultValue = resultValue;
        }
    }

    /// <summary>
    /// Immutable description of everything that happened during one
    /// GridModel.Move() call. This is the ONLY channel through which the
    /// presentation layer learns what changed - it never reads GridModel's
    /// internal array directly for animation purposes, which is what keeps
    /// state logic and rendering decoupled.
    /// </summary>
    public sealed class MoveResult
    {
        public bool AnyTileMoved { get; init; }
        public int ScoreDelta { get; init; }
        public List<TileTransition> Transitions { get; init; } = new();
        public GridCoord? SpawnedTile { get; init; }
        public bool SpawnedIsWildcard { get; init; }
        public bool IsGameOver { get; init; }
        public bool IsWin { get; init; }
        public int MovesRemaining { get; init; }
    }
}
