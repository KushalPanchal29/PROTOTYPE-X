using UnityEngine;

namespace HeightIsTime
{
    /// <summary>
    /// Shows world time as a filling bar and tints the background while time moves: warm going forward,
    /// cold while rewinding. Shapes only, no text or fonts.
    /// </summary>
    public class TimeHud : MonoBehaviour
    {
        [SerializeField] RectTransform fill;
        [SerializeField] Camera targetCamera;
        [SerializeField] Color idleColor = new Color(0.12f, 0.12f, 0.14f);
        [SerializeField] Color forwardColor = new Color(0.22f, 0.16f, 0.10f);
        [SerializeField] Color rewindColor = new Color(0.08f, 0.14f, 0.24f);
        [SerializeField] float tintSpeed = 6f;

        void Update()
        {
            WorldClock clock = WorldClock.Instance;
            if (clock == null || clock.ActiveRoom == null) return;

            float t01 = clock.ActiveRoom.MaxTime > 0f ? clock.CurrentTime / clock.ActiveRoom.MaxTime : 0f;
            if (fill != null) fill.anchorMax = new Vector2(t01, 1f);

            if (targetCamera != null)
            {
                Color goal = clock.Direction > 0 ? forwardColor : clock.Direction < 0 ? rewindColor : idleColor;
                targetCamera.backgroundColor =
                    Color.Lerp(targetCamera.backgroundColor, goal, 1f - Mathf.Exp(-tintSpeed * Time.deltaTime));
            }
        }
    }
}
