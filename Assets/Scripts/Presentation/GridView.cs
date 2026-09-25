using System.Collections.Generic;
using GridPuzzle.Core;
using UnityEngine;

namespace GridPuzzle.Presentation
{
    /// <summary>
    /// Owns the visual grid of TileView instances. Reads a GridModel only
    /// through Redraw(model) (full refresh) or Animate(MoveResult, model)
    /// (incremental) - it never mutates model state and never contains
    /// merge/scoring logic. This is the decoupling boundary the "System
    /// Architecture Map" diagram documents.
    /// </summary>
    public sealed class GridView : MonoBehaviour
    {
        [SerializeField] private RectTransform boardRoot;
        [SerializeField] private TileView tilePrefab;
        [SerializeField] private float cellSize = 150f;
        [SerializeField] private float cellSpacing = 12f;

        private readonly Dictionary<GridCoord, TileView> _views = new();
        private int _rows, _cols;

        public void Init(int rows, int cols)
        {
            _rows = rows;
            _cols = cols;
        }

        private Vector2 AnchoredPosFor(GridCoord c)
        {
            float totalWidth = _cols * cellSize + (_cols - 1) * cellSpacing;
            float totalHeight = _rows * cellSize + (_rows - 1) * cellSpacing;

            float startX = -totalWidth / 2f + cellSize / 2f;
            float startY = totalHeight / 2f - cellSize / 2f;

            float x = startX + c.Col * (cellSize + cellSpacing);
            float y = startY - c.Row * (cellSize + cellSpacing);
            return new Vector2(x, y);
        }

        /// <summary>Full redraw - used after Undo/Restore where a diff isn't tracked.</summary>
        public void Redraw(GridModel model)
        {
            foreach (var v in _views.Values) Destroy(v.gameObject);
            _views.Clear();

            for (int r = 0; r < _rows; r++)
                for (int c = 0; c < _cols; c++)
                {
                    var coord = new GridCoord(r, c);
                    var tile = model.GetTile(coord);
                    if (tile == null) continue;

                    var view = Instantiate(tilePrefab, boardRoot);
                    view.SetValue(tile.Value, tile.IsWildcard);
                    view.SnapToPosition(AnchoredPosFor(coord));
                    _views[coord] = view;
                }
        }

        /// <summary>Incremental animation driven purely by the MoveResult contract.</summary>
        public void Animate(MoveResult result)
        {
            var newViews = new Dictionary<GridCoord, TileView>();

            foreach (var transition in result.Transitions)
            {
                if (!_views.TryGetValue(transition.From, out var view))
                    continue; // already consumed by an earlier transition sharing the same origin

                _views.Remove(transition.From);

                if (transition.Merged)
                {
                    view.AnimateToPosition(AnchoredPosFor(transition.To));
                    Destroy(view.gameObject, 0.15f);
                }
                else
                {
                    view.AnimateToPosition(AnchoredPosFor(transition.To));
                    view.SetValue(transition.ResultValue, false);
                    newViews[transition.To] = view;
                    if (newViews.ContainsKey(transition.To) && result.Transitions.Exists(
                            t => t.To == transition.To && t.Merged))
                    {
                        view.PlayMergePulse();
                    }
                }
            }

            foreach (var kv in newViews) _views[kv.Key] = kv.Value;

            if (result.SpawnedTile.HasValue)
            {
                var coord = result.SpawnedTile.Value;
                var view = Instantiate(tilePrefab, boardRoot);
                view.SetValue(2, result.SpawnedIsWildcard);
                view.SnapToPosition(AnchoredPosFor(coord));
                view.PlaySpawnPop();
                _views[coord] = view;
            }
        }
    }
}