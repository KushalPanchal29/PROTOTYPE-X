using UnityEngine;

namespace HeightIsTime
{
    /// <summary>
    /// Base for anything that changes with world time. Subclasses turn a time value into a state, so the same
    /// time always gives the same result and rewinding needs no recording.
    /// </summary>
    public abstract class TimeObject : MonoBehaviour
    {
        [Tooltip("Only follow time while the player is in this room. Leave empty to always follow.")]
        [SerializeField] RoomZone room;

        void FixedUpdate()
        {
            WorldClock clock = WorldClock.Instance;
            if (clock == null) return;
            if (room != null && clock.ActiveRoom != room) return;
            ApplyTime(clock.CurrentTime);
        }

        protected abstract void ApplyTime(float time);
    }
}
