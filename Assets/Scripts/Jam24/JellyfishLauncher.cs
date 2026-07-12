using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jam24
{
    /// <summary>
    /// A vertically bobbing jellyfish. Touching the trigger above its head
    /// launches the player or Flip in a straight line at the configured angle.
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
            bool isFlip = other.GetComponentInParent<FlipWallBounce2D>() != null ||
                          GameplayManager.Instance != null && GameplayManager.Instance.IsFlip(other.gameObject);
            if (movement == null && !isFlip) return;

            Rigidbody2D launchedBody = other.attachedRigidbody;
            if (launchedBody == null && movement != null)
                launchedBody = movement.GetComponent<Rigidbody2D>();
            if (launchedBody == null || activeLaunches.ContainsKey(launchedBody)) return;
            if (nextLaunchTimes.TryGetValue(launchedBody, out float nextTime) && Time.time < nextTime) return;

            float jellyfishY = jellyfishBody != null ? jellyfishBody.position.y : transform.position.y;
            if (launchedBody.worldCenterOfMass.y < jellyfishY + minimumTopHeight) return;

            StartCoroutine(BounceBody(launchedBody, movement));
        }

        private IEnumerator BounceBody(Rigidbody2D launchedBody, OctopusPlayerMovement movement)
        {
            LaunchState state = new()
            {
                BodyType = launchedBody.bodyType,
                MovementWasEnabled = movement != null && movement.enabled,
                Movement = movement
            };
            activeLaunches.Add(launchedBody, state);

            if (movement != null) movement.enabled = false;
            launchedBody.linearVelocity = Vector2.zero;
            launchedBody.angularVelocity = 0f;
            launchedBody.bodyType = RigidbodyType2D.Kinematic;

            float radians = bounceAngle * Mathf.Deg2Rad;
            Vector2 direction = new(Mathf.Cos(radians), Mathf.Sin(radians));
            Vector2 start = launchedBody.position;
            Vector2 end = start + direction * bounceDistance;
            float elapsed = 0f;

            while (elapsed < bounceDuration && launchedBody != null)
            {
                elapsed += Time.fixedDeltaTime;
                float t = Mathf.Clamp01(elapsed / bounceDuration);
                float easedT = 1f - (1f - t) * (1f - t);
                launchedBody.MovePosition(Vector2.Lerp(start, end, easedT));
                yield return new WaitForFixedUpdate();
            }

            if (launchedBody != null)
            {
                launchedBody.position = end;
                RestoreBody(launchedBody, state);
                launchedBody.linearVelocity = direction;
                nextLaunchTimes[launchedBody] = Time.time + relaunchCooldown;
            }

            activeLaunches.Remove(launchedBody);
        }

        private static void RestoreBody(Rigidbody2D body, LaunchState state)
        {
            body.bodyType = state.BodyType;
            if (state.Movement != null) state.Movement.enabled = state.MovementWasEnabled;
        }

        private void OnDisable()
        {
            StopAllCoroutines();

            foreach (KeyValuePair<Rigidbody2D, LaunchState> pair in activeLaunches)
            {
                if (pair.Key != null) RestoreBody(pair.Key, pair.Value);
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
