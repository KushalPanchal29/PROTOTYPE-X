using UnityEngine;

namespace HeightIsTime
{
    /// <summary>
    /// Faint horizontal lines, one per second of world time (line i sits at the feet height where time = i).
    /// Lines at or below the current time light up, so the room reads like a thermometer of time.
    /// </summary>
    public class TimeRuler : TimeObject
    {
        [SerializeField] SpriteRenderer[] lines;
        [SerializeField] Color color = Color.white;
        [Range(0f, 1f)] [SerializeField] float dimAlpha = 0.05f;
        [Range(0f, 1f)] [SerializeField] float litAlpha = 0.22f;

        protected override void ApplyTime(float time)
        {
            for (int second = 0; second < lines.Length; second++)
            {
                if (lines[second] == null) continue;
                Color c = color;
                c.a = second <= time ? litAlpha : dimAlpha;
                lines[second].color = c;
            }
        }
    }
}
