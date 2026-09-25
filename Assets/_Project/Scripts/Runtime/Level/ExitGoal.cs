using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace HeightIsTime
{
    /// <summary>End of the level. Shows the win panel and freezes the player; R restarts the scene.</summary>
    public class ExitGoal : MonoBehaviour
    {
        [SerializeField] GameObject winPanel;

        bool won;

        void Start()
        {
            if (winPanel != null) winPanel.SetActive(false);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (won) return;
            PlayerController2D player = other.GetComponentInParent<PlayerController2D>();
            if (player == null) return;

            won = true;
            player.Freeze();
            if (winPanel != null) winPanel.SetActive(true);
        }

        void Update()
        {
            if (won && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
