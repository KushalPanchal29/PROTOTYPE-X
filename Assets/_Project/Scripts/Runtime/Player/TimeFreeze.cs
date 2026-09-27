using UnityEngine;
using UnityEngine.InputSystem;

namespace HeightIsTime
{
    /// <summary>
    /// Hold Shift to freeze world time at its current value, so you can change height without changing time.
    /// Freezing drains a meter; it refills while not frozen. When the meter runs out, time unfreezes and you
    /// must release the key before freezing again.
    /// </summary>
    [RequireComponent(typeof(PlayerRespawn))]
    public class TimeFreeze : MonoBehaviour
    {
        [SerializeField] float capacity = 4f;
        [SerializeField] float refillPerSecond = 1f;
        [SerializeField] Color frozenTint = new Color(0.55f, 0.9f, 1f);

        InputAction freezeAction;
        SpriteRenderer sprite;
        Color normalColor;
        bool needsRelease;

        public float Meter { get; private set; }
        public float Meter01 => capacity > 0f ? Meter / capacity : 0f;

        void Awake()
        {
            freezeAction = new InputAction("Freeze", InputActionType.Button);
            freezeAction.AddBinding("<Keyboard>/leftShift");
            freezeAction.AddBinding("<Keyboard>/rightShift");
            freezeAction.AddBinding("<Gamepad>/rightTrigger");

            sprite = GetComponent<SpriteRenderer>();
            normalColor = sprite.color;
            Meter = capacity;
            GetComponent<PlayerRespawn>().Respawned += OnRespawned;
        }

        void OnEnable() => freezeAction.Enable();
        void OnDisable() => freezeAction.Disable();
        void OnDestroy() => freezeAction.Dispose();

        void Update()
        {
            WorldClock clock = WorldClock.Instance;
            if (clock == null) return;

            bool held = freezeAction.IsPressed();
            if (!held) needsRelease = false;

            bool freeze = held && !needsRelease && Meter > 0f;
            if (freeze != clock.IsFrozen) clock.SetFrozen(freeze);

            if (clock.IsFrozen)
            {
                Meter = Mathf.Max(0f, Meter - Time.deltaTime);
                if (Meter <= 0f)
                {
                    needsRelease = true;
                    clock.SetFrozen(false);
                }
            }
            else
            {
                Meter = Mathf.Min(capacity, Meter + refillPerSecond * Time.deltaTime);
            }

            sprite.color = clock.IsFrozen ? frozenTint : normalColor;
        }

        void OnRespawned()
        {
            Meter = capacity;
            needsRelease = freezeAction.IsPressed();
            if (WorldClock.Instance != null) WorldClock.Instance.SetFrozen(false);
        }
    }
}
