using System;
using System.Collections.Generic;

namespace GridPuzzle.Core
{
    /// <summary>
    /// The authoritative logical grid. Owns board state, merge rules, score,
    /// and win/loss evaluation. Contains no UnityEngine references at all -
    /// this class can be constructed and unit tested in a plain C# project.
    ///
    /// Responsibility (SRP): "what is the board state, and how does a move
    /// transform it". Nothing about drawing, input, or animation lives here.
    /// </summary>
    public sealed class GridModel
    {
        public int Rows { get; }
        public int Cols { get; }
        public int Score { get; private set; }
        public int MovesRemaining { get; private set; }
        public GameMode Mode { get; }
        public int WinValue { get; }
        public int TargetScoreForStepLimited { get; }

        private readonly Tile[,] _board;
        private readonly Random _rng;
        private readonly float _wildcardSpawnChance;

        public GridModel(int rows, int cols, GameMode mode, int winValue,
            int stepLimitedMoveBudget, int targetScoreForStepLimited,
            float wildcardSpawnChance = 0.06f, int? seed = null)
        {
            Rows = rows;
            Cols = cols;
            Mode = mode;
            WinValue = winValue;
            TargetScoreForStepLimited = targetScoreForStepLimited;
            MovesRemaining = mode == GameMode.StepLimited ? stepLimitedMoveBudget : int.MaxValue;
            _wildcardSpawnChance = wildcardSpawnChance;
            _rng = seed.HasValue ? new Random(seed.Value) : new Random();
            _board = new Tile[rows, cols];
        }

        public Tile GetTile(GridCoord c) => _board[c.Row, c.Col];

        /// <summary>Starts a session by placing two seed tiles.</summary>
        public void StartNewGame()
        {
            Array.Clear(_board, 0, _board.Length);
            Score = 0;
            SpawnRandomTile();
            SpawnRandomTile();
        }

        /// <summary>
        /// Applies one discrete move. Returns a MoveResult describing every
        /// transition so the view layer can animate without ever peeking
        /// at _board directly.
        /// </summary>
        public MoveResult Move(Direction dir)
        {
            var transitions = new List<TileTransition>();
            int scoreDelta = 0;

            int lineCount = (dir == Direction.Left || dir == Direction.Right) ? Rows : Cols;
            for (int lineIndex = 0; lineIndex < lineCount; lineIndex++)
            {
                var orderedCoords = GetLineCoords(dir, lineIndex);
                scoreDelta += CompactAndMergeLine(orderedCoords, transitions);
            }

            bool anyMoved = transitions.Count > 0;
            Score += scoreDelta;

            GridCoord? spawned = null;
            bool spawnedWildcard = false;
            if (anyMoved)
            {
                if (Mode == GameMode.StepLimited && MovesRemaining > 0)
                    MovesRemaining--;

                var spawnCoord = SpawnRandomTile();
                if (spawnCoord.HasValue)
                {
                    spawned = spawnCoord;
                    spawnedWildcard = _board[spawnCoord.Value.Row, spawnCoord.Value.Col].IsWildcard;
                }
            }

            bool isWin = CheckWinCondition();
            bool isGameOver = !isWin && CheckGameOverCondition();

            return new MoveResult
            {
                AnyTileMoved = anyMoved,
                ScoreDelta = scoreDelta,
                Transitions = transitions,
                SpawnedTile = spawned,
                SpawnedIsWildcard = spawnedWildcard,
                IsGameOver = isGameOver,
                IsWin = isWin,
                MovesRemaining = MovesRemaining
            };
        }

        // ---- line extraction -------------------------------------------------

        /// <summary>
        /// Returns the coordinates of one row/column ordered from the edge
        /// tiles are pushed TOWARD, outward to the far edge. This lets
        /// CompactAndMergeLine treat all four directions with one algorithm.
        /// </summary>
        private List<GridCoord> GetLineCoords(Direction dir, int lineIndex)
        {
            var coords = new List<GridCoord>();
            switch (dir)
            {
                case Direction.Left:
                    for (int c = 0; c < Cols; c++) coords.Add(new GridCoord(lineIndex, c));
                    break;
                case Direction.Right:
                    for (int c = Cols - 1; c >= 0; c--) coords.Add(new GridCoord(lineIndex, c));
                    break;
                case Direction.Up:
                    for (int r = 0; r < Rows; r++) coords.Add(new GridCoord(r, lineIndex));
                    break;
                case Direction.Down:
                    for (int r = Rows - 1; r >= 0; r--) coords.Add(new GridCoord(r, lineIndex));
                    break;
            }
            return coords;
        }

        /// <summary>
        /// Classic 2048-style compact+merge over one ordered line.
        /// Wildcard tiles merge with ANY neighbour (the "hook" mechanic).
        /// Returns the score gained from merges on this line.
        /// </summary>
        private int CompactAndMergeLine(List<GridCoord> orderedCoords, List<TileTransition> transitions)
        {
            var gathered = new List<(Tile tile, GridCoord origin)>();
            foreach (var coord in orderedCoords)
            {
                var t = _board[coord.Row, coord.Col];
                if (t != null) gathered.Add((t, coord));
            }

            var resultSlots = new List<Tile>();
            var slotOrigins = new List<List<GridCoord>>(); // origins that fed each result slot
            int scoreGained = 0;

            for (int i = 0; i < gathered.Count; i++)
            {
                var (tile, origin) = gathered[i];

                if (resultSlots.Count > 0)
                {
                    var lastSlot = resultSlots[^1];
                    bool canMerge = !slotOrigins[^1].Contains(origin) &&
                                     slotOrigins[^1].Count == 1 && // only merge once per slot
                                     (lastSlot.Value == tile.Value || lastSlot.IsWildcard || tile.IsWildcard);

                    if (canMerge)
                    {
                        int mergedValue = lastSlot.IsWildcard || tile.IsWildcard
                            ? Math.Max(lastSlot.Value, tile.Value) * 2
                            : lastSlot.Value * 2;

                        resultSlots[^1] = new Tile(mergedValue, false);
                        slotOrigins[^1].Add(origin);
                        scoreGained += mergedValue;
                        continue;
                    }
                }

                resultSlots.Add(tile.Clone());
                slotOrigins.Add(new List<GridCoord> { origin });
            }

            // Write result slots back into the line's coordinates (from push-edge outward)
            // and record transitions for every original tile.
            for (int slot = 0; slot < resultSlots.Count; slot++)
            {
                var destCoord = orderedCoords[slot];
                var origins = slotOrigins[slot];
                bool wasMerge = origins.Count > 1;

                for (int k = 0; k < origins.Count; k++)
                {
                    bool thisOneMergedAway = wasMerge && k > 0; // first origin "survives", rest merge in
                    transitions.Add(new TileTransition(origins[k], destCoord, thisOneMergedAway, resultSlots[slot].Value));
                }

                _board[destCoord.Row, destCoord.Col] = resultSlots[slot];
            }

            // Clear any cells beyond the written slots (they are now empty).
            for (int slot = resultSlots.Count; slot < orderedCoords.Count; slot++)
            {
                var coord = orderedCoords[slot];
                _board[coord.Row, coord.Col] = null;
            }

            return scoreGained;
        }

        // ---- spawning ----------------------------------------------------

        private GridCoord? SpawnRandomTile()
        {
            var empties = new List<GridCoord>();
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                    if (_board[r, c] == null) empties.Add(new GridCoord(r, c));

            if (empties.Count == 0) return null;

            var coord = empties[_rng.Next(empties.Count)];
            bool isWildcard = _rng.NextDouble() < _wildcardSpawnChance;
            int value = isWildcard ? 2 : (_rng.NextDouble() < 0.9 ? 2 : 4);
            _board[coord.Row, coord.Col] = new Tile(value, isWildcard);
            return coord;
        }

        // ---- win / loss ----------------------------------------------------

        private bool CheckWinCondition()
        {
            if (Mode == GameMode.Endless)
            {
                for (int r = 0; r < Rows; r++)
                    for (int c = 0; c < Cols; c++)
                        if (_board[r, c] != null && _board[r, c].Value >= WinValue)
                            return true;
                return false;
            }

            // StepLimited: win if target score reached (regardless of moves left).
            return Score >= TargetScoreForStepLimited;
        }

        /// <summary>
        /// O(Rows*Cols) game-over check: no empty cell AND no adjacent pair
        /// that could merge (equal values, or either side a wildcard).
        /// Deliberately avoids simulating all four moves on a cloned board -
        /// that would be O(4 * Rows * Cols) and unnecessary.
        /// </summary>
        private bool CheckGameOverCondition()
        {
            if (Mode == GameMode.StepLimited && MovesRemaining <= 0)
                return true;

            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    var t = _board[r, c];
                    if (t == null) return false;

                    if (c + 1 < Cols)
                    {
                        var right = _board[r, c + 1];
                        if (right == null || right.Value == t.Value || right.IsWildcard || t.IsWildcard)
                            return false;
                    }
                    if (r + 1 < Rows)
                    {
                        var down = _board[r + 1, c];
                        if (down == null || down.Value == t.Value || down.IsWildcard || t.IsWildcard)
                            return false;
                    }
                }
            }
            return true;
        }

        // ---- undo support ----------------------------------------------------

        public GridSnapshot Snapshot()
        {
            var values = new int[Rows, Cols];
            var wildcards = new bool[Rows, Cols];
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                {
                    var t = _board[r, c];
                    values[r, c] = t?.Value ?? 0;
                    wildcards[r, c] = t?.IsWildcard ?? false;
                }
            return new GridSnapshot(values, wildcards, Score, MovesRemaining);
        }

        public void Restore(GridSnapshot snapshot)
        {
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                {
                    int v = snapshot.Values[r, c];
                    _board[r, c] = v == 0 ? null : new Tile(v, snapshot.Wildcards[r, c]);
                }
            Score = snapshot.Score;
            MovesRemaining = snapshot.MovesRemaining;
        }
    }
}
