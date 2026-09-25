using System;
using GridPuzzle.Core;
using UnityEngine;

namespace GridPuzzle.Input
{
    /// <summary>
    /// Tracks a pointer/touch drag and converts it into a discrete
    /// Direction event once the movement clears a threshold. Works with
    /// both touch (mobile) and mouse (editor testing) via the legacy Input
    /// class's UnityEngine.Input.touches / mouse fallback, so it needs no
    /// extra package dependency.
    ///
    /// This is the ONLY class that knows about raw pointer coordinates -
    /// everything downstream deals exclusively with the Direction enum.
    /// </summary>
    public sealed class SwipeInputController : MonoBehaviour
    {
        public event Action<Direction> OnSwipe;

        [SerializeField] private float swipeThresholdPixels = 60f;

        private Vector2 _dragStart;
        private bool _dragging;
        private bool _consumedThisDrag;

        public void SetThreshold(float pixels) => swipeThresholdPixels = pixels;

        private void Update()
        {
            if (UnityEngine.Input.touchCount > 0)
            {
                var touch = UnityEngine.Input.GetTouch(0);
                HandlePhase(touch.phase, touch.position);
            }
            else
            {
                HandleMouseFallback();
            }
        }

        private void HandleMouseFallback()
        {
            if (UnityEngine.Input.GetMouseButtonDown(0))
                HandlePhase(TouchPhase.Began, UnityEngine.Input.mousePosition);
            else if (UnityEngine.Input.GetMouseButton(0) && _dragging)
                HandlePhase(TouchPhase.Moved, UnityEngine.Input.mousePosition);
            else if (UnityEngine.Input.GetMouseButtonUp(0))
                HandlePhase(TouchPhase.Ended, UnityEngine.Input.mousePosition);
        }

        private void HandlePhase(TouchPhase phase, Vector2 position)
        {
            switch (phase)
            {
                case TouchPhase.Began:
                    _dragStart = position;
                    _dragging = true;
                    _consumedThisDrag = false;
                    break;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (_dragging && !_consumedThisDrag)
                        TryResolveSwipe(position);
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (_dragging && !_consumedThisDrag)
                        TryResolveSwipe(position);
                    _dragging = false;
                    break;
            }
        }

        private void TryResolveSwipe(Vector2 currentPos)
        {
            Vector2 delta = currentPos - _dragStart;
            if (delta.magnitude < swipeThresholdPixels) return;

            Direction dir = Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                ? (delta.x > 0 ? Direction.Right : Direction.Left)
                : (delta.y > 0 ? Direction.Up : Direction.Down);

            _consumedThisDrag = true; // one discrete command per drag gesture

            OnSwipe?.Invoke(dir);
        }
    }
}
