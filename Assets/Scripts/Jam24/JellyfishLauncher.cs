using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jam24
{
    /// <summary>
    /// A vertically bobbing jellyfish. Touching the trigger above its head
    /// launches the player in a straight line at the configured angle.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class JellyfishLauncher : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Rigidbody2D jellyfishBody;
        [SerializeField] private CircleCollider2D headTrigger;

        [Header("Vertical Movement")]
        [SerializeField, Min(0f)] private float movementHeight = .65f;
        [SerializeField, Min(.1f)] private float movementCycleDuration = 2.8f;

        [Header("Straight Bounce")]
        [SerializeField, Range(0f, 180f)] private float bounceAngle = 45f;
        [SerializeField, Min(.1f)] private float bounceDistance = 3.2f;
        [SerializeField, Min(.1f)] private float bounceDuration = .85f;
        [SerializeField, Min(0f)] private float relaunchCooldown = .3f;
        [SerializeField, Min(0f)] private float minimumTopHeight = .2f;

        private readonly Dictionary<Rigidbody2D, LaunchState> activeLaunches = new();
        private readonly Dictionary<Rigidbody2D, float> nextLaunchTimes = new();
        private Vector2 movementCenter;
        private float movementElapsed;

        private sealed class LaunchState
        {
            public RigidbodyType2D BodyType;
            public bool MovementWasEnabled;
            public OctopusPlayerMovement Movement;
        }

        private void Awake()
        {
            if (jellyfishBody == null) jellyfishBody = GetComponent<Rigidbody2D>();
            if (headTrigger != null) headTrigger.isTrigger = true;
        }

        private void OnEnable()
        {
            if (jellyfishBody == null) jellyfishBody = GetComponent<Rigidbody2D>();
            movementCenter = jellyfishBody != null
                ? jellyfishBody.position
                : (Vector2)transform.position;
            movementElapsed = 0f;
        }

        private void FixedUpdate()
        {
            if (jellyfishBody == null || movementHeight <= 0f) return;

            movementElapsed += Time.fixedDeltaTime;
            float angle = movementElapsed / movementCycleDuration * Mathf.PI * 2f;
            Vector2 position = movementCenter + Vector2.up * (Mathf.Sin(angle) * movementHeight);
            jellyfishBody.MovePosition(position);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryBounce(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryBounce(other);
        }

        private void TryBounce(Collider2D other)
        {
            OctopusPlayerMovement movement = other.GetComponentInParent<OctopusPlayerMovement>();
            if (movement == null) return;

            Rigidbody2D playerBody = movement.GetComponent<Rigidbody2D>();
            if (playerBody == null || activeLaunches.ContainsKey(playerBody)) return;
            if (nextLaunchTimes.TryGetValue(playerBody, out float nextTime) && Time.time < nextTime) return;

            float jellyfishY = jellyfishBody != null ? jellyfishBody.position.y : transform.position.y;
            if (playerBody.worldCenterOfMass.y < jellyfishY + minimumTopHeight) return;

            StartCoroutine(BouncePlayer(playerBody, movement));
        }

        private IEnumerator BouncePlayer(Rigidbody2D playerBody, OctopusPlayerMovement movement)
        {
            LaunchState state = new()
            {
                BodyType = playerBody.bodyType,
                MovementWasEnabled = movement.enabled,
                Movement = movement
            };
            activeLaunches.Add(playerBody, state);

            movement.enabled = false;
            playerBody.linearVelocity = Vector2.zero;
            playerBody.angularVelocity = 0f;
            playerBody.bodyType = RigidbodyType2D.Kinematic;

            float radians = bounceAngle * Mathf.Deg2Rad;
            Vector2 direction = new(Mathf.Cos(radians), Mathf.Sin(radians));
            Vector2 start = playerBody.position;
            Vector2 end = start + direction * bounceDistance;
            float elapsed = 0f;

            while (elapsed < bounceDuration && playerBody != null)
            {
                elapsed += Time.fixedDeltaTime;
                float t = Mathf.Clamp01(elapsed / bounceDuration);
                float easedT = 1f - (1f - t) * (1f - t);
                playerBody.MovePosition(Vector2.Lerp(start, end, easedT));
                yield return new WaitForFixedUpdate();
            }

            if (playerBody != null)
            {
                playerBody.position = end;
                RestorePlayer(playerBody, state);
                playerBody.linearVelocity = direction;
                nextLaunchTimes[playerBody] = Time.time + relaunchCooldown;
            }

            activeLaunches.Remove(playerBody);
        }

        private static void RestorePlayer(Rigidbody2D body, LaunchState state)
        {
            body.bodyType = state.BodyType;
            if (state.Movement != null) state.Movement.enabled = state.MovementWasEnabled;
        }

        private void OnDisable()
        {
            StopAllCoroutines();

            foreach (KeyValuePair<Rigidbody2D, LaunchState> pair in activeLaunches)
            {
                if (pair.Key != null) RestorePlayer(pair.Key, pair.Value);
            }

            activeLaunches.Clear();
            nextLaunchTimes.Clear();
            if (jellyfishBody != null) jellyfishBody.position = movementCenter;
        }

        private void OnValidate()
        {
            movementHeight = Mathf.Max(0f, movementHeight);
            movementCycleDuration = Mathf.Max(.1f, movementCycleDuration);
            bounceDistance = Mathf.Max(.1f, bounceDistance);
            bounceDuration = Mathf.Max(.1f, bounceDuration);
            relaunchCooldown = Mathf.Max(0f, relaunchCooldown);
            minimumTopHeight = Mathf.Max(0f, minimumTopHeight);
        }
    }
}
