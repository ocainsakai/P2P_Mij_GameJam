using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jam24
{
    /// <summary>
    /// A puffer fish that periodically inflates. While inflated, landing on its
    /// head launches the player along a right-facing parabolic arc.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class PufferFishLauncher : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private CircleCollider2D bodyCollider;
        [SerializeField] private CircleCollider2D topTrigger;

        [Header("Inflation Cycle")]
        [SerializeField] private Vector3 deflatedScale = new(.72f, .72f, 1f);
        [SerializeField] private Vector3 inflatedScale = new(1.2f, 1.2f, 1f);
        [SerializeField, Min(0f)] private float deflatedDuration = 1.5f;
        [SerializeField, Min(.05f)] private float inflateDuration = .45f;
        [SerializeField, Min(0f)] private float inflatedDuration = 1.6f;
        [SerializeField, Min(.05f)] private float deflateDuration = .45f;
        [SerializeField, Range(0f, 1f)] private float activeInflationThreshold = .82f;

        [Header("Parabolic Launch")]
        [SerializeField, Min(.1f)] private float launchDistance = 5f;
        [SerializeField, Min(.1f)] private float launchHeight = 4f;
        [SerializeField] private float landingHeightOffset = .5f;
        [SerializeField, Min(.1f)] private float launchDuration = 1.5f;
        [SerializeField, Min(0f)] private float relaunchCooldown = .25f;
        [SerializeField, Min(0f)] private float minimumTopHeight = .25f;

        public bool IsInflated { get; private set; }

        private readonly Dictionary<Rigidbody2D, LaunchState> activeLaunches = new();
        private readonly Dictionary<Rigidbody2D, float> nextLaunchTimes = new();
        private Coroutine inflationRoutine;
        private float baseBodyRadius;
        private float baseTriggerRadius;
        private Vector2 baseTriggerOffset;

        private sealed class LaunchState
        {
            public RigidbodyType2D BodyType;
            public bool MovementWasEnabled;
            public OctopusPlayerMovement Movement;
        }

        private void Awake()
        {
            if (visualRoot == null && transform.childCount > 0) visualRoot = transform.GetChild(0);
            if (bodyCollider == null) bodyCollider = GetComponent<CircleCollider2D>();

            baseBodyRadius = bodyCollider != null ? bodyCollider.radius : .55f;
            if (topTrigger != null)
            {
                baseTriggerRadius = topTrigger.radius;
                baseTriggerOffset = topTrigger.offset;
            }

            ApplyInflation(0f);
        }

        private void OnEnable()
        {
            inflationRoutine = StartCoroutine(InflationCycle());
        }

        private IEnumerator InflationCycle()
        {
            while (true)
            {
                ApplyInflation(0f);
                if (deflatedDuration > 0f) yield return new WaitForSeconds(deflatedDuration);

                yield return AnimateInflation(0f, 1f, inflateDuration);
                ApplyInflation(1f);
                if (inflatedDuration > 0f) yield return new WaitForSeconds(inflatedDuration);

                yield return AnimateInflation(1f, 0f, deflateDuration);
            }
        }

        private IEnumerator AnimateInflation(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                ApplyInflation(Mathf.Lerp(from, to, t));
                yield return null;
            }

            ApplyInflation(to);
        }

        private void ApplyInflation(float amount)
        {
            amount = Mathf.Clamp01(amount);
            Vector3 scale = Vector3.Lerp(deflatedScale, inflatedScale, amount);
            if (visualRoot != null) visualRoot.localScale = scale;

            // Keep the physical body aligned with the visible inflated size.
            float colliderScale = Mathf.Lerp(deflatedScale.x, inflatedScale.x, amount);
            if (bodyCollider != null) bodyCollider.radius = baseBodyRadius * colliderScale;
            if (topTrigger != null)
            {
                topTrigger.radius = baseTriggerRadius * colliderScale;
                topTrigger.offset = baseTriggerOffset * colliderScale;
            }

            IsInflated = amount >= activeInflationThreshold;
            if (topTrigger != null) topTrigger.enabled = IsInflated;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryLaunch(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryLaunch(other);
        }

        private void TryLaunch(Collider2D other)
        {
            if (!IsInflated) return;

            OctopusPlayerMovement movement = other.GetComponentInParent<OctopusPlayerMovement>();
            if (movement == null) return;

            Rigidbody2D playerBody = movement.GetComponent<Rigidbody2D>();
            if (playerBody == null || activeLaunches.ContainsKey(playerBody)) return;
            if (nextLaunchTimes.TryGetValue(playerBody, out float nextTime) && Time.time < nextTime) return;

            // The trigger overlaps only the upper area; this additional check
            // prevents a side touch from counting as landing on the fish's head.
            if (playerBody.worldCenterOfMass.y < transform.position.y + minimumTopHeight) return;

            StartCoroutine(LaunchPlayer(playerBody, movement));
        }

        private IEnumerator LaunchPlayer(Rigidbody2D playerBody, OctopusPlayerMovement movement)
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

            Vector2 start = playerBody.position;
            Vector2 end = start + new Vector2(launchDistance, landingHeightOffset);
            float elapsed = 0f;

            while (elapsed < launchDuration && playerBody != null)
            {
                elapsed += Time.fixedDeltaTime;
                float t = Mathf.Clamp01(elapsed / launchDuration);
                Vector2 position = Vector2.Lerp(start, end, t);
                position.y += 4f * launchHeight * t * (1f - t);
                playerBody.MovePosition(position);
                yield return new WaitForFixedUpdate();
            }

            if (playerBody != null)
            {
                playerBody.position = end;
                RestorePlayer(playerBody, state);
                playerBody.linearVelocity = new Vector2(launchDistance / launchDuration, 0f);
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
            if (inflationRoutine != null) StopCoroutine(inflationRoutine);
            inflationRoutine = null;
            StopAllCoroutines();

            foreach (KeyValuePair<Rigidbody2D, LaunchState> pair in activeLaunches)
            {
                if (pair.Key != null) RestorePlayer(pair.Key, pair.Value);
            }

            activeLaunches.Clear();
            nextLaunchTimes.Clear();
            ApplyInflation(0f);
        }

        private void OnValidate()
        {
            inflateDuration = Mathf.Max(.05f, inflateDuration);
            deflateDuration = Mathf.Max(.05f, deflateDuration);
            launchDuration = Mathf.Max(.1f, launchDuration);
            launchDistance = Mathf.Max(.1f, launchDistance);
            launchHeight = Mathf.Max(.1f, launchHeight);
        }
    }
}
