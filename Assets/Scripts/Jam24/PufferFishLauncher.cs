using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jam24
{
    /// <summary>
    /// A puffer fish that periodically inflates. While inflated, landing on its
    /// head launches the player or Flip along a right-facing parabolic arc.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class PufferFishLauncher : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer visualRenderer;
        [SerializeField] private CircleCollider2D bodyCollider;
        [SerializeField] private CircleCollider2D topTrigger;

        [Header("Visual States")]
        [SerializeField] private Sprite deflatedSprite;
        [SerializeField] private Sprite inflatedSprite;
        [SerializeField, Range(0f, 1f)] private float spriteSwapThreshold = .5f;

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
            if (visualRenderer == null && visualRoot != null)
                visualRenderer = visualRoot.GetComponent<SpriteRenderer>();
            if (visualRenderer == null)
                visualRenderer = GetComponentInChildren<SpriteRenderer>(true);
            if (deflatedSprite == null && visualRenderer != null)
                deflatedSprite = visualRenderer.sprite;
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

            if (visualRenderer != null)
            {
                Sprite stateSprite = amount >= spriteSwapThreshold ? inflatedSprite : deflatedSprite;
                if (stateSprite != null && visualRenderer.sprite != stateSprite)
                    visualRenderer.sprite = stateSprite;
            }

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
            bool isFlip = other.GetComponentInParent<FlipWallBounce2D>() != null ||
                          GameplayManager.Instance != null && GameplayManager.Instance.IsFlip(other.gameObject);
            if (movement == null && !isFlip) return;

            Rigidbody2D launchedBody = other.attachedRigidbody;
            if (launchedBody == null && movement != null)
                launchedBody = movement.GetComponent<Rigidbody2D>();
            if (launchedBody == null || activeLaunches.ContainsKey(launchedBody)) return;
            if (nextLaunchTimes.TryGetValue(launchedBody, out float nextTime) && Time.time < nextTime) return;

            // The trigger overlaps only the upper area; this additional check
            // prevents a side touch from counting as landing on the fish's head.
            if (launchedBody.worldCenterOfMass.y < transform.position.y + minimumTopHeight) return;

            StartCoroutine(LaunchBody(launchedBody, movement));
        }

        private IEnumerator LaunchBody(Rigidbody2D launchedBody, OctopusPlayerMovement movement)
        {
            LaunchState state = new()
            {
                BodyType = launchedBody.bodyType,
                MovementWasEnabled = movement != null && movement.enabled,
                Movement = movement
            };
            activeLaunches.Add(launchedBody, state);
            JamAudioManager.Play(GameSfxType.PufferFishBounce);

            if (movement != null) movement.enabled = false;
            launchedBody.linearVelocity = Vector2.zero;
            launchedBody.angularVelocity = 0f;
            launchedBody.bodyType = RigidbodyType2D.Kinematic;

            Vector2 start = launchedBody.position;
            Vector2 end = start + new Vector2(launchDistance, landingHeightOffset);
            float elapsed = 0f;

            while (elapsed < launchDuration && launchedBody != null)
            {
                elapsed += Time.fixedDeltaTime;
                float t = Mathf.Clamp01(elapsed / launchDuration);
                Vector2 position = Vector2.Lerp(start, end, t);
                position.y += 4f * launchHeight * t * (1f - t);
                launchedBody.MovePosition(position);
                yield return new WaitForFixedUpdate();
            }

            if (launchedBody != null)
            {
                launchedBody.position = end;
                RestoreBody(launchedBody, state);
                launchedBody.linearVelocity = new Vector2(launchDistance / launchDuration, 0f);
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
            if (inflationRoutine != null) StopCoroutine(inflationRoutine);
            inflationRoutine = null;
            StopAllCoroutines();

            foreach (KeyValuePair<Rigidbody2D, LaunchState> pair in activeLaunches)
            {
                if (pair.Key != null) RestoreBody(pair.Key, pair.Value);
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
            spriteSwapThreshold = Mathf.Clamp01(spriteSwapThreshold);
        }
    }
}
