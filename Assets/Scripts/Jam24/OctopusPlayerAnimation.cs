using UnityEngine;

namespace Jam24
{
    /// <summary>Feeds gameplay state into the Unity Animator without restarting held states.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(OctopusPlayerMovement), typeof(Rigidbody2D), typeof(Animator))]
    public sealed class OctopusPlayerAnimation : MonoBehaviour
    {
        private enum PlayerAnimState { Idle, SwimHorizontal, SwimUp, SwimDown, Push }
        private static readonly int StateParameter = Animator.StringToHash("State");

        private OctopusPlayerMovement movement;
        private Rigidbody2D body;
        private Animator animator;
        private SpriteRenderer[] renderers;
        private SpriteRenderer headRenderer;
        private Color[] normalColors;
        private PlayerAnimState currentState = (PlayerAnimState)(-1);
        private float pushContactUntil;
        private bool facingLeft;

        private void Awake()
        {
            movement = GetComponent<OctopusPlayerMovement>();
            body = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            Transform head = transform.Find("Head");
            if (head != null) headRenderer = head.GetComponent<SpriteRenderer>();
            normalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) normalColors[i] = renderers[i].color;
        }

        private void Update()
        {
            Vector2 input = movement.MoveInput;
            PlayerAnimState nextState = ResolveState(input, body.linearVelocity,
                Time.time <= pushContactUntil && Mathf.Abs(input.x) > .1f);

            // Important: never write/replay the same state while a key is held.
            if (nextState != currentState)
            {
                currentState = nextState;
                animator.SetInteger(StateParameter, (int)currentState);
            }

            if (input.x < -.08f) facingLeft = true;
            else if (input.x > .08f) facingLeft = false;

            for (int i = 0; i < renderers.Length; i++)
            {
                // Tentacle sprites have off-centre pivots. Flipping them individually
                // moves their artwork away from the body, which looks like a missing leg.
                renderers[i].flipX = renderers[i] == headRenderer && facingLeft;
                renderers[i].color = movement.IsGripping
                    ? new Color(1f, .48f, .72f, normalColors[i].a)
                    : normalColors[i];
            }
        }

        private static PlayerAnimState ResolveState(Vector2 input, Vector2 velocity, bool pushing)
        {
            if (pushing) return PlayerAnimState.Push;
            float vertical = Mathf.Abs(input.y) > .08f ? input.y : velocity.y;
            if (vertical > .15f) return PlayerAnimState.SwimUp;
            if (vertical < -.15f) return PlayerAnimState.SwimDown;
            if (Mathf.Abs(input.x) > .08f || Mathf.Abs(velocity.x) > .2f) return PlayerAnimState.SwimHorizontal;
            return PlayerAnimState.Idle;
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            Rigidbody2D other = collision.rigidbody;
            if (other == null || other.bodyType != RigidbodyType2D.Dynamic) return;

            Vector2 input = movement.MoveInput;
            for (int i = 0; i < collision.contactCount; i++)
            {
                if (Vector2.Dot(input, -collision.GetContact(i).normal) <= .35f) continue;
                pushContactUntil = Time.time + .12f;
                return;
            }
        }
    }
}
