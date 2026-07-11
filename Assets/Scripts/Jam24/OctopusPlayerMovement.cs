using UnityEngine;
using UnityEngine.InputSystem;

namespace Jam24
{
    /// <summary>
    /// Underwater octopus movement ported from the Demo 1 prototype.
    /// WASD/arrows swim, Space grips, and marked surfaces can be crawled on.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer))]
    public sealed class OctopusPlayerMovement : MonoBehaviour
    {
        [Header("Swimming")]
        [SerializeField, Min(0f)] private float horizontalAcceleration = 21f;
        [SerializeField, Min(0f)] private float verticalAcceleration = 15f;
        [SerializeField, Min(0f)] private float underwaterGravity = 4.8f;
        [SerializeField, Min(0f)] private float waterDrag = 5f;
        [SerializeField, Min(0.1f)] private float maxSwimSpeed = 5.6f;

        [Header("Climbing")]
        [SerializeField, Min(0.1f)] private float climbSpeed = 3f;
        [SerializeField, Min(0f)] private float climbProbePadding = 0.12f;

        [Header("Screen Bounds")]
        [SerializeField, Min(0f)] private float viewportPadding = 0.35f;

        public Vector2 MoveInput { get; private set; }
        public bool IsGripping { get; private set; }
        public bool IsClimbing { get; private set; }

        private Rigidbody2D body;
        private CircleCollider2D bodyCollider;
        private SpriteRenderer spriteRenderer;
        private Camera gameplayCamera;
        private Color normalColor;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<CircleCollider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            gameplayCamera = Camera.main;
            normalColor = spriteRenderer.color;

            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        private void Update()
        {
            ReadInput();

            if (MoveInput.x < -0.01f) spriteRenderer.flipX = true;
            else if (MoveInput.x > 0.01f) spriteRenderer.flipX = false;

            spriteRenderer.color = IsGripping
                ? new Color(1f, 0.48f, 0.72f, normalColor.a)
                : normalColor;
        }

        private void FixedUpdate()
        {
            bool touchingClimbable = IsTouchingClimbable();
            IsClimbing = touchingClimbable && MoveInput.sqrMagnitude > 0.001f;

            if (IsGripping)
            {
                body.linearVelocity = Vector2.zero;
            }
            else if (IsClimbing)
            {
                body.linearVelocity = MoveInput.normalized * climbSpeed;
            }
            else
            {
                Vector2 velocity = body.linearVelocity;
                velocity.x += MoveInput.x * horizontalAcceleration * Time.fixedDeltaTime;
                velocity.y += (MoveInput.y * verticalAcceleration - underwaterGravity) * Time.fixedDeltaTime;

                // Demo 1 multiplies velocity by 0.92 each frame. Exponential drag
                // preserves that feel without depending on the physics frame rate.
                velocity *= Mathf.Exp(-waterDrag * Time.fixedDeltaTime);
                body.linearVelocity = Vector2.ClampMagnitude(velocity, maxSwimSpeed);
            }

            ClampToCameraBounds();
        }

        private void ReadInput()
        {
            Vector2 input = Vector2.zero;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
                IsGripping = keyboard.spaceKey.isPressed;
            }
            else
            {
                IsGripping = false;
            }

            Gamepad gamepad = Gamepad.current;
            if (gamepad != null)
            {
                Vector2 stick = gamepad.leftStick.ReadValue();
                if (stick.sqrMagnitude > input.sqrMagnitude) input = stick;
                IsGripping |= gamepad.rightShoulder.isPressed;
            }

            MoveInput = Vector2.ClampMagnitude(input, 1f);
        }

        private bool IsTouchingClimbable()
        {
            float radius = bodyCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y) + climbProbePadding;
            Collider2D[] hits = Physics2D.OverlapCircleAll(bodyCollider.bounds.center, radius);
            foreach (Collider2D hit in hits)
            {
                if (hit != bodyCollider && hit.GetComponentInParent<OctopusClimbable>() != null)
                    return true;
            }
            return false;
        }

        private void ClampToCameraBounds()
        {
            if (gameplayCamera == null) gameplayCamera = Camera.main;
            if (gameplayCamera == null || !gameplayCamera.orthographic) return;

            float halfHeight = gameplayCamera.orthographicSize;
            float halfWidth = halfHeight * gameplayCamera.aspect;
            Vector3 cameraPosition = gameplayCamera.transform.position;
            Vector2 position = body.position;
            position.x = Mathf.Clamp(position.x,
                cameraPosition.x - halfWidth + viewportPadding,
                cameraPosition.x + halfWidth - viewportPadding);
            position.y = Mathf.Clamp(position.y,
                cameraPosition.y - halfHeight + viewportPadding,
                cameraPosition.y + halfHeight - viewportPadding);

            if (position != body.position) body.position = position;
        }
    }
}
