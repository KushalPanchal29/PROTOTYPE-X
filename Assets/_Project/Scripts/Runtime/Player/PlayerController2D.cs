using UnityEngine;
using UnityEngine.InputSystem;

namespace HeightIsTime
{
    /// <summary>
    /// Run and jump. Coyote time and jump buffering make jumps forgiving; releasing jump early cuts the jump short.
    /// The player is the only thing in the world that time does not affect.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController2D : MonoBehaviour
    {
        [SerializeField] InputActionAsset actions;
        [SerializeField] float moveSpeed = 6f;
        [Tooltip("Full jump height in meters. With 1 s per meter this is also how far one jump moves time.")]
        [SerializeField] float jumpHeight = 1.6f;
        [SerializeField] float coyoteTime = 0.1f;
        [SerializeField] float jumpBufferTime = 0.12f;
        [Range(0f, 1f)] [SerializeField] float jumpCutMultiplier = 0.5f;
        [Tooltip("Holding Down in the air dives at this speed. Falling faster also rewinds time faster.")]
        [SerializeField] float fastFallSpeed = 20f;

        Rigidbody2D body;
        InputAction moveAction;
        InputAction jumpAction;
        ContactFilter2D groundFilter;
        float lastGroundedTime = float.NegativeInfinity;
        float lastJumpPressedTime = float.NegativeInfinity;
        bool jumpCutRequested;
        float moveInput;
        bool fastFallHeld;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            moveAction = actions.FindAction("Player/Move", true);
            jumpAction = actions.FindAction("Player/Jump", true);

            // Touching something below us (normal pointing up) counts as ground.
            groundFilter = new ContactFilter2D { useTriggers = false };
            groundFilter.SetNormalAngle(45f, 135f);
        }

        void OnEnable()
        {
            moveAction.Enable();
            jumpAction.Enable();
        }

        void OnDisable()
        {
            moveAction.Disable();
            jumpAction.Disable();
        }

        // Input is read every frame and used in the next physics step.
        void Update()
        {
            Vector2 move = moveAction.ReadValue<Vector2>();
            moveInput = move.x;
            fastFallHeld = move.y < -0.5f;
            if (jumpAction.WasPressedThisFrame()) lastJumpPressedTime = Time.time;
            if (jumpAction.WasReleasedThisFrame()) jumpCutRequested = true;
        }

        void FixedUpdate()
        {
            bool grounded = body.IsTouching(groundFilter);
            if (grounded) lastGroundedTime = Time.time;

            Vector2 velocity = body.linearVelocity;
            velocity.x = moveInput * moveSpeed;

            bool jumpBuffered = Time.time - lastJumpPressedTime <= jumpBufferTime;
            bool canJump = Time.time - lastGroundedTime <= coyoteTime;
            if (jumpBuffered && canJump)
            {
                velocity.y = JumpVelocity();
                lastJumpPressedTime = float.NegativeInfinity;
                lastGroundedTime = float.NegativeInfinity;
                jumpCutRequested = false;
            }

            if (jumpCutRequested)
            {
                if (velocity.y > 0f) velocity.y *= jumpCutMultiplier;
                jumpCutRequested = false;
            }

            if (fastFallHeld && !grounded) velocity.y = Mathf.Min(velocity.y, -fastFallSpeed);

            body.linearVelocity = velocity;
        }

        /// <summary>Stops the player and ignores input (used by the exit).</summary>
        public void Freeze()
        {
            enabled = false;
            body.linearVelocity = Vector2.zero;
        }

        float JumpVelocity()
        {
            return Mathf.Sqrt(2f * jumpHeight * Mathf.Abs(Physics2D.gravity.y * body.gravityScale));
        }
    }
}
