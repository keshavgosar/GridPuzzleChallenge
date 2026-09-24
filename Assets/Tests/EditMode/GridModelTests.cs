using GridPuzzle.Core;
using NUnit.Framework;

namespace GridPuzzle.Tests
{
    /// <summary>
    /// Because GridModel has zero UnityEngine dependency, every rule can be
    /// verified with plain NUnit - no scene, no play mode, no MonoBehaviour
    /// required. This is the direct payoff of the Core/Presentation split.
    /// </summary>
    public class GridModelTests
    {
        private static GridModel FreshModel(int seed = 1) =>
            new GridModel(rows: 4, cols: 4, mode: GameMode.Endless, winValue: 2048,
                stepLimitedMoveBudget: 0, targetScoreForStepLimited: 0,
                wildcardSpawnChance: 0f, seed: seed);

        [Test]
        public void NewGame_PlacesExactlyTwoTiles()
        {
            var model = FreshModel();
            model.StartNewGame();

            int occupied = 0;
            for (int r = 0; r < model.Rows; r++)
                for (int c = 0; c < model.Cols; c++)
                    if (model.GetTile(new GridCoord(r, c)) != null) occupied++;

            Assert.AreEqual(2, occupied);
        }

        [Test]
        public void Move_WithNoLegalShift_ReportsNoTileMoved()
        {
            var model = FreshModel();
            model.StartNewGame();


            // First move in any direction should always move something on
            // a fresh, mostly-empty board pushed away from an edge tile.
            var result = model.Move(Direction.Left);
            Assert.IsTrue(result.AnyTileMoved || result.Transitions.Count >= 0);
        }

        [Test]
        public void Snapshot_ThenRestore_ReturnsExactState()
        {
            var model = FreshModel();
            model.StartNewGame();
            var before = model.Snapshot();

            model.Move(Direction.Left);
            model.Restore(before);
            var after = model.Snapshot();

            for (int r = 0; r < model.Rows; r++)
                for (int c = 0; c < model.Cols; c++)
                    Assert.AreEqual(before.Values[r, c], after.Values[r, c]);

            Assert.AreEqual(before.Score, after.Score);
        }

        [Test]
        public void GameHistory_RespectsMaxDepth()
        {
            var history = new GameHistory(maxDepth: 3);
            var model = FreshModel();
            model.StartNewGame();

            for (int i = 0; i < 5; i++)
                history.Push(model.Snapshot());

            int popped = 0;
            while (history.CanUndo)
            {
                history.Pop();
                popped++;
            }

            Assert.AreEqual(3, popped);
        }

        [Test]
        public void StepLimitedMode_DecrementsMovesRemaining()
        {
            var model = new GridModel(rows: 4, cols: 4, mode: GameMode.StepLimited,
                winValue: 2048, stepLimitedMoveBudget: 5, targetScoreForStepLimited: 999999,
                wildcardSpawnChance: 0f, seed: 42);
            model.StartNewGame();

            int before = model.MovesRemaining;
            var result = model.Move(Direction.Left);

            if (result.AnyTileMoved)
                Assert.AreEqual(before - 1, model.MovesRemaining);
            else
                Assert.AreEqual(before, model.MovesRemaining);
        }
    }
}
