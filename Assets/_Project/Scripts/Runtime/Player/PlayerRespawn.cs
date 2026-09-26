using UnityEngine;
using UnityEngine.InputSystem;

namespace HeightIsTime
{
    /// <summary>
    /// Puts the player back at the active room's respawn point, on death or when R is pressed. World time is
    /// derived from height, so the room resets by itself: no saved state to restore.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerRespawn : MonoBehaviour
    {
        Rigidbody2D body;

        /// <summary>Raised after every respawn (deaths and R presses).</summary>
        public event System.Action Respawned;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
        }

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) Respawn();
        }

        public void Respawn()
        {
            WorldClock clock = WorldClock.Instance;
            if (clock == null || clock.ActiveRoom == null) return;

            Vector2 spawn = clock.ActiveRoom.RespawnPosition;
            body.linearVelocity = Vector2.zero;
            body.position = spawn;
            transform.position = spawn;
            Respawned?.Invoke();
        }
    }
}
