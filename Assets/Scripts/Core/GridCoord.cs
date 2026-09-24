using System;

namespace GridPuzzle.Core
{
    /// <summary>
    /// Lightweight, engine-agnostic 2D grid coordinate.
    /// Deliberately not UnityEngine.Vector2Int - Core have zero engine
    /// dependency so it can be unit tested outside the Unity runtime and
    /// reused by any renderer (2D, 3D, or a debug console view).
    /// </summary>
    public readonly struct GridCoord : IEquatable<GridCoord>
    {
        public readonly int Row;
        public readonly int Col;

        public GridCoord(int row, int col)
        {
            Row = row;
            Col = col;
        }

        public bool Equals(GridCoord other) => Row == other.Row && Col == other.Col;
        public override bool Equals(object obj) => obj is GridCoord other && Equals(other);
        public override int GetHashCode() => (Row * 397) ^ Col;
        public static bool operator ==(GridCoord a, GridCoord b) => a.Equals(b);
        public static bool operator !=(GridCoord a, GridCoord b) => !a.Equals(b);
        public override string ToString() => $"({Row},{Col})";
    }
}
