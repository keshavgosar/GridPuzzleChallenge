namespace GridPuzzle.Core
{
    /// <summary>
    /// A single tile's logical data only. Contains no rendering, no
    /// MonoBehaviour, no transform - purely a value holder.
    /// </summary>
    public sealed class Tile
    {
        public int Value { get; set; }
        public bool IsWildcard { get; set; }

        public Tile(int value, bool isWildcard = false)
        {
            Value = value;
            IsWildcard = isWildcard;
        }

        public Tile Clone() => new Tile(Value, IsWildcard);
    }
}
