using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GridPuzzle.Presentation
{
    /// <summary>
    /// Purely visual. Knows how to display a value and animate to a
    /// position; knows nothing about grid rules, merge logic, or scoring.
    /// </summary>
    public sealed class TileView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text label;
        [SerializeField] private float moveDuration = 0.12f;
        [SerializeField] private Color wildcardColor = new Color(0.85f, 0.55f, 0.95f);

        private RectTransform _rect;
        private Coroutine _moveRoutine;

        private void Awake() => _rect = (RectTransform)transform;

        public void SetValue(int value, bool isWildcard)
        {
            
            label.text = isWildcard ? "*" : value.ToString();
            background.color = isWildcard ? wildcardColor : ColorForValue(value);
        }

        public void SnapToPosition(Vector2 anchoredPos) => _rect.anchoredPosition = anchoredPos;

        public void AnimateToPosition(Vector2 anchoredPos)
        {
            if (_moveRoutine != null) StopCoroutine(_moveRoutine);
            _moveRoutine = StartCoroutine(MoveRoutine(anchoredPos));
        }

        public void PlaySpawnPop()
        {
            transform.localScale = Vector3.zero;
            StartCoroutine(ScaleRoutine(Vector3.zero, Vector3.one, moveDuration));
        }

        public void PlayMergePulse()
        {
            StartCoroutine(ScaleRoutine(Vector3.one * 1.15f, Vector3.one, moveDuration));
        }

        private System.Collections.IEnumerator MoveRoutine(Vector2 target)
        {
            Vector2 start = _rect.anchoredPosition;
            float t = 0f;
            while (t < moveDuration)
            {
                t += Time.deltaTime;
                _rect.anchoredPosition = Vector2.Lerp(start, target, t / moveDuration);
                yield return null;
            }
            _rect.anchoredPosition = target;
        }

        private System.Collections.IEnumerator ScaleRoutine(Vector3 from, Vector3 to, float duration)
        {
            transform.localScale = from;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                transform.localScale = Vector3.Lerp(from, to, t / duration);
                yield return null;
            }
            transform.localScale = to;
        }

        private static Color ColorForValue(int value)
        {
            // Simple deterministic ramp - swap for a curated palette later.
            float t = Mathf.Log(Mathf.Max(value, 2), 2) / 11f;
            return Color.Lerp(new Color(0.93f, 0.89f, 0.82f), new Color(0.93f, 0.4f, 0.15f), t);
        }
    }
}