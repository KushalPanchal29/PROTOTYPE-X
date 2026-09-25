using UnityEngine;

namespace HeightIsTime
{
    /// <summary>
    /// One room of the level. Defines where time 0 is (the ground line), how fast height turns into time, and
    /// where the player respawns. Entering the trigger makes this the active room.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class RoomZone : MonoBehaviour
    {
        [Tooltip("World Y of the player's centre when standing at time 0.")]
        [SerializeField] float groundY;
        [SerializeField] float secondsPerMeter = 1f;
        [SerializeField] float maxTime = 10f;
        [SerializeField] Transform respawnPoint;

        public float GroundY => groundY;
        public float SecondsPerMeter => secondsPerMeter;
        public float MaxTime => maxTime;
        public Vector2 RespawnPosition => respawnPoint != null ? (Vector2)respawnPoint.position : (Vector2)transform.position;

        void Reset()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            WorldClock clock = WorldClock.Instance;
            if (clock != null && other.attachedRigidbody == clock.Player) clock.EnterRoom(this);
        }

        // Draws the time scale in the Scene view so designers can see which height is which second.
        void OnDrawGizmos()
        {
            BoxCollider2D box = GetComponent<BoxCollider2D>();
            if (box == null || secondsPerMeter <= 0f) return;
            Bounds b = box.bounds;
            for (int second = 0; second <= Mathf.FloorToInt(maxTime); second++)
            {
                float y = groundY + second / secondsPerMeter;
                Gizmos.color = second == 0 ? Color.green : new Color(1f, 1f, 1f, 0.25f);
                Gizmos.DrawLine(new Vector3(b.min.x, y), new Vector3(b.max.x, y));
            }
            Gizmos.color = new Color(1f, 0.8f, 0f, 0.5f);
            Gizmos.DrawWireCube(b.center, b.size);
        }
    }
}
