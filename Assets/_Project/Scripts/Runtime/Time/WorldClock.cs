using UnityEngine;

namespace HeightIsTime
{
    /// <summary>
    /// The one source of world time. Each physics step it turns the player's height in the active room into a
    /// time value. Every <see cref="TimeObject"/> reads <see cref="CurrentTime"/>; nothing else keeps its own clock.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class WorldClock : MonoBehaviour
    {
        public static WorldClock Instance { get; private set; }

        [SerializeField] Rigidbody2D player;
        [SerializeField] RoomZone startingRoom;

        public float CurrentTime { get; private set; }

        /// <summary>+1 while time moves forward, -1 while it rewinds, 0 when it holds still.</summary>
        public int Direction { get; private set; }

        /// <summary>While frozen, time holds its value no matter how the player moves.</summary>
        public bool IsFrozen { get; private set; }

        public RoomZone ActiveRoom { get; private set; }
        public Rigidbody2D Player => player;

        void Awake()
        {
            Instance = this;
            ActiveRoom = startingRoom;
            CurrentTime = ComputeTime();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void FixedUpdate()
        {
            if (IsFrozen)
            {
                Direction = 0;
                return;
            }
            float previous = CurrentTime;
            CurrentTime = ComputeTime();
            float delta = CurrentTime - previous;
            Direction = Mathf.Abs(delta) < 0.001f ? 0 : (delta > 0f ? 1 : -1);
        }

        /// <summary>Freezing keeps the current time; unfreezing snaps back to the player's height next step.</summary>
        public void SetFrozen(bool frozen)
        {
            IsFrozen = frozen;
        }

        // A new room always starts from the player's height, even while frozen.
        public void EnterRoom(RoomZone room)
        {
            if (room == ActiveRoom) return;
            ActiveRoom = room;
            CurrentTime = ComputeTime();
            Direction = 0;
        }

        float ComputeTime()
        {
            if (player == null || ActiveRoom == null) return 0f;
            return TimeMath.HeightToTime(player.position.y, ActiveRoom.GroundY, ActiveRoom.SecondsPerMeter,
                ActiveRoom.MaxTime);
        }
    }
}
