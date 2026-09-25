using UnityEngine;

namespace HeightIsTime
{
    /// <summary>
    /// Solid only while world time is inside [timeMin, timeMax]. Outside that window it shows as a faint ghost,
    /// so the player can see what exists at other times. Used by the future bridge and doors.
    /// </summary>
    public class TimeToggle : TimeObject
    {
        [SerializeField] float timeMin = 6f;
        [SerializeField] float timeMax = 999f;
        [Tooltip("Seconds of tolerance once solid, so it does not flicker at the edge.")]
        [SerializeField] float hysteresis = 0.2f;
        [Range(0f, 1f)] [SerializeField] float ghostAlpha = 0.2f;

        Collider2D[] colliders;
        SpriteRenderer[] sprites;
        bool active = true;

        void Awake()
        {
            colliders = GetComponentsInChildren<Collider2D>();
            sprites = GetComponentsInChildren<SpriteRenderer>();
        }

        protected override void ApplyTime(float time)
        {
            bool shouldBeActive = TimeMath.InWindowWithHysteresis(time, timeMin, timeMax, hysteresis, active);
            if (shouldBeActive != active) SetActive(shouldBeActive);
        }

        void SetActive(bool value)
        {
            active = value;
            foreach (Collider2D c in colliders) c.enabled = value;
            foreach (SpriteRenderer s in sprites)
            {
                Color color = s.color;
                color.a = value ? 1f : ghostAlpha;
                s.color = color;
            }
        }
    }
}
