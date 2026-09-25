using UnityEngine;

namespace HeightIsTime
{
    /// <summary>Sends the player back to the active room's respawn point on touch (the rock, pits).</summary>
    public class Hazard : MonoBehaviour
    {
        void OnTriggerEnter2D(Collider2D other) => TryKill(other);
        void OnCollisionEnter2D(Collision2D collision) => TryKill(collision.collider);

        static void TryKill(Collider2D other)
        {
            PlayerRespawn player = other.GetComponentInParent<PlayerRespawn>();
            if (player != null) player.Respawn();
        }
    }
}
