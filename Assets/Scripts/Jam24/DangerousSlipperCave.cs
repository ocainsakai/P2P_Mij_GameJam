using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jam24
{
    /// <summary>
    /// Warns the player when they enter the slipper cave, then lashes the cave
    /// interior with a procedural tentacle. Escaping during the warning cancels
    /// the attack; being hit knocks the player out without consuming the Flip.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
    public sealed class DangerousSlipperCave : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer caveRenderer;
        [SerializeField] private BoxCollider2D dangerZone;
        [SerializeField] private Transform tentacleOrigin;
        [SerializeField] private Transform flipSpawnPoint;

        [Header("Slipper Reward")]
        [SerializeField] private bool hideFlipUntilPlayerEntry = true;
        [SerializeField] private bool requireJellyfishLaunch;
        [SerializeField, Min(0f)] private float minimumImpactSpeed = 3.5f;

        [Header("Attack Timing")]
        [SerializeField, Min(.1f)] private float warningDuration = 1.2f;
        [SerializeField, Min(.05f)] private float extendDuration = .16f;
        [SerializeField, Min(.05f)] private float activeDuration = .22f;
        [SerializeField, Min(.05f)] private float retractDuration = .25f;
        [SerializeField, Min(0f)] private float repeatCooldown = .8f;

        [Header("Knockback")]
        [SerializeField, Min(.1f)] private float knockbackSpeed = 7f;
        [SerializeField, Min(0f)] private float knockbackLift = 2f;
        [SerializeField, Min(.05f)] private float movementLockDuration = .45f;

        [Header("Visuals")]
        [SerializeField] private Color warningColor = new(1f, .25f, .35f, 1f);
        [SerializeField] private Color tentacleColor = new(.58f, .14f, .78f, 1f);
        [SerializeField, Min(.05f)] private float tentacleWidth = .3f;
        [SerializeField, Range(6, 24)] private int tentacleSegments = 12;
        [SerializeField, Min(0f)] private float tentacleCurve = .65f;
        [SerializeField, Min(0f)] private float warningPulseScale = .045f;

        public Transform FlipSpawnPoint => flipSpawnPoint;

        private readonly HashSet<Rigidbody2D> hitThisStrike = new();
        private readonly Dictionary<OctopusPlayerMovement, bool> lockedPlayers = new();
        private OctopusPlayerMovement playerInside;
        private Coroutine attackRoutine;
        private LineRenderer tentacleLine;
        private Material runtimeTentacleMaterial;
        private GameObject storedFlip;
        private bool rewardReleased;
        private Color normalCaveColor;
        private Vector3 normalCaveScale;

        private void Awake()
        {
            if (caveRenderer == null) caveRenderer = GetComponent<SpriteRenderer>();
            if (dangerZone != null) dangerZone.enabled = false;

            normalCaveColor = caveRenderer != null ? caveRenderer.color : Color.white;
            normalCaveScale = caveRenderer != null ? caveRenderer.transform.localScale : Vector3.one;
            CreateTentacleVisual();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            OctopusPlayerMovement movement = other.GetComponentInParent<OctopusPlayerMovement>();
            if (movement == null) return;

            if (!rewardReleased && CanReleaseReward(movement))
            {
                rewardReleased = true;
                if (!RevealStoredFlip())
                    GameplayManager.Instance?.SpawnAdditionalFlip(flipSpawnPoint);
            }

            playerInside = movement;
            if (attackRoutine == null)
                attackRoutine = StartCoroutine(AttackLoop(movement));
        }

        private bool CanReleaseReward(OctopusPlayerMovement movement)
        {
            Rigidbody2D playerBody = movement.GetComponent<Rigidbody2D>();
            if (playerBody == null) return false;
            if (requireJellyfishLaunch && !JellyfishLauncher.IsLaunching(playerBody)) return false;
            float impactSpeed = Mathf.Max(
                playerBody.linearVelocity.magnitude,
                JellyfishLauncher.GetLaunchSpeed(playerBody));
            return impactSpeed >= minimumImpactSpeed;
        }

        public void StoreFlipUntilPlayerEnters(GameObject flip)
        {
            if (!hideFlipUntilPlayerEntry || flip == null || flipSpawnPoint == null) return;

            storedFlip = flip;
            storedFlip.transform.SetPositionAndRotation(
                flipSpawnPoint.position,
                flipSpawnPoint.rotation);
            storedFlip.SetActive(false);
        }

        private bool RevealStoredFlip()
        {
            if (storedFlip == null) return false;

            GameObject flip = storedFlip;
            storedFlip = null;
            flip.transform.SetPositionAndRotation(
                flipSpawnPoint.position,
                flipSpawnPoint.rotation);
            flip.SetActive(true);

            Rigidbody2D flipBody = flip.GetComponent<Rigidbody2D>();
            if (flipBody == null) return true;
            flipBody.linearVelocity = Vector2.zero;
            flipBody.angularVelocity = 0f;
            flipBody.WakeUp();
            return true;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            OctopusPlayerMovement movement = other.GetComponentInParent<OctopusPlayerMovement>();
            if (movement == null || movement != playerInside) return;

            playerInside = null;
        }

        private IEnumerator AttackLoop(OctopusPlayerMovement target)
        {
            while (target != null && playerInside == target)
            {
                yield return PlayWarning(target);
                if (target == null || playerInside != target) break;

                yield return Strike(target);
                ResetCaveVisual();

                float cooldown = 0f;
                while (cooldown < repeatCooldown && target != null && playerInside == target)
                {
                    cooldown += Time.deltaTime;
                    yield return null;
                }
            }

            ResetCaveVisual();
            HideTentacle();
            attackRoutine = null;
        }

        private IEnumerator PlayWarning(OctopusPlayerMovement target)
        {
            float elapsed = 0f;
            while (elapsed < warningDuration && target != null && playerInside == target)
            {
                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / warningDuration);
                float flash = .5f + .5f * Mathf.Sin(normalized * Mathf.PI * 8f);

                if (caveRenderer != null)
                {
                    caveRenderer.color = Color.Lerp(normalCaveColor, warningColor, flash * .72f);
                    caveRenderer.transform.localScale = normalCaveScale *
                        (1f + Mathf.Sin(normalized * Mathf.PI * 6f) * warningPulseScale);
                }

                yield return null;
            }
        }

        private IEnumerator Strike(OctopusPlayerMovement target)
        {
            hitThisStrike.Clear();
            ResetCaveVisual();
            if (tentacleLine != null) tentacleLine.enabled = true;

            Vector2 targetPoint = GetAttackTarget(target);
            float elapsed = 0f;
            while (elapsed < extendDuration)
            {
                elapsed += Time.deltaTime;
                if (target != null && playerInside == target) targetPoint = GetAttackTarget(target);
                float amount = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / extendDuration));
                DrawTentacle(targetPoint, amount);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < activeDuration)
            {
                elapsed += Time.deltaTime;
                CheckForPlayerHit();
                DrawTentacle(targetPoint, 1f);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < retractDuration)
            {
                elapsed += Time.deltaTime;
                float amount = 1f - Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(elapsed / retractDuration));
                DrawTentacle(targetPoint, amount);
                yield return null;
            }

            HideTentacle();
        }

        private void CheckForPlayerHit()
        {
            if (dangerZone == null) return;

            Transform zoneTransform = dangerZone.transform;
            Vector2 center = zoneTransform.TransformPoint(dangerZone.offset);
            Vector3 scale = zoneTransform.lossyScale;
            Vector2 size = new(
                dangerZone.size.x * Mathf.Abs(scale.x),
                dangerZone.size.y * Mathf.Abs(scale.y));
            float angle = zoneTransform.eulerAngles.z;

            Collider2D[] overlaps = Physics2D.OverlapBoxAll(center, size, angle);
            foreach (Collider2D overlap in overlaps)
            {
                OctopusPlayerMovement movement = overlap.GetComponentInParent<OctopusPlayerMovement>();
                if (movement == null) continue;

                Rigidbody2D body = movement.GetComponent<Rigidbody2D>();
                if (body == null || body.bodyType != RigidbodyType2D.Dynamic ||
                    hitThisStrike.Contains(body)) continue;

                hitThisStrike.Add(body);
                StartCoroutine(KnockPlayerOut(movement, body, center));
            }
        }

        private IEnumerator KnockPlayerOut(
            OctopusPlayerMovement movement,
            Rigidbody2D body,
            Vector2 dangerCenter)
        {
            if (lockedPlayers.ContainsKey(movement)) yield break;

            bool movementWasEnabled = movement.enabled;
            lockedPlayers.Add(movement, movementWasEnabled);
            movement.enabled = false;

            Vector2 away = body.worldCenterOfMass - dangerCenter;
            if (away.sqrMagnitude < .01f) away = Vector2.left;
            away.Normalize();
            body.linearVelocity = away * knockbackSpeed + Vector2.up * knockbackLift;
            body.angularVelocity = 0f;

            float elapsed = 0f;
            while (elapsed < movementLockDuration && movement != null && body != null)
            {
                elapsed += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            if (movement != null) movement.enabled = movementWasEnabled;
            lockedPlayers.Remove(movement);
        }

        private Vector2 GetAttackTarget(OctopusPlayerMovement target)
        {
            if (target != null) return target.transform.position;
            if (dangerZone != null) return dangerZone.transform.position;
            return transform.position;
        }

        private void CreateTentacleVisual()
        {
            GameObject visual = new("Tentacle Visual (Runtime)");
            visual.transform.SetParent(transform, false);
            tentacleLine = visual.AddComponent<LineRenderer>();
            tentacleLine.useWorldSpace = true;
            tentacleLine.positionCount = tentacleSegments;
            tentacleLine.startWidth = tentacleWidth;
            tentacleLine.endWidth = tentacleWidth * .42f;
            tentacleLine.startColor = tentacleColor;
            tentacleLine.endColor = Color.Lerp(tentacleColor, Color.white, .22f);
            tentacleLine.numCapVertices = 6;
            tentacleLine.numCornerVertices = 4;
            tentacleLine.sortingOrder = 8;

            Shader spriteShader = Shader.Find("Sprites/Default");
            if (spriteShader != null)
            {
                runtimeTentacleMaterial = new Material(spriteShader)
                {
                    name = "Dangerous Cave Tentacle (Runtime)"
                };
                tentacleLine.material = runtimeTentacleMaterial;
            }

            tentacleLine.enabled = false;
        }

        private void DrawTentacle(Vector2 finalTarget, float extension)
        {
            if (tentacleLine == null || tentacleOrigin == null) return;

            int segments = Mathf.Max(6, tentacleSegments);
            if (tentacleLine.positionCount != segments) tentacleLine.positionCount = segments;

            Vector2 start = tentacleOrigin.position;
            Vector2 end = Vector2.Lerp(start, finalTarget, Mathf.Clamp01(extension));
            Vector2 direction = end - start;
            Vector2 perpendicular = direction.sqrMagnitude > .001f
                ? new Vector2(-direction.y, direction.x).normalized
                : Vector2.up;
            Vector2 control = Vector2.Lerp(start, end, .5f) + perpendicular * tentacleCurve;

            for (int i = 0; i < segments; i++)
            {
                float t = i / (segments - 1f);
                float inverse = 1f - t;
                Vector2 point = inverse * inverse * start +
                                2f * inverse * t * control +
                                t * t * end;
                tentacleLine.SetPosition(i, point);
            }
        }

        private void HideTentacle()
        {
            if (tentacleLine != null) tentacleLine.enabled = false;
        }

        private void ResetCaveVisual()
        {
            if (caveRenderer == null) return;
            caveRenderer.color = normalCaveColor;
            caveRenderer.transform.localScale = normalCaveScale;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            foreach (KeyValuePair<OctopusPlayerMovement, bool> pair in lockedPlayers)
            {
                if (pair.Key != null) pair.Key.enabled = pair.Value;
            }

            lockedPlayers.Clear();
            hitThisStrike.Clear();
            playerInside = null;
            attackRoutine = null;
            ResetCaveVisual();
            HideTentacle();
        }

        private void OnDestroy()
        {
            if (runtimeTentacleMaterial != null) Destroy(runtimeTentacleMaterial);
        }

        private void OnValidate()
        {
            warningDuration = Mathf.Max(.1f, warningDuration);
            extendDuration = Mathf.Max(.05f, extendDuration);
            activeDuration = Mathf.Max(.05f, activeDuration);
            retractDuration = Mathf.Max(.05f, retractDuration);
            repeatCooldown = Mathf.Max(0f, repeatCooldown);
            knockbackSpeed = Mathf.Max(.1f, knockbackSpeed);
            knockbackLift = Mathf.Max(0f, knockbackLift);
            movementLockDuration = Mathf.Max(.05f, movementLockDuration);
            minimumImpactSpeed = Mathf.Max(0f, minimumImpactSpeed);
            tentacleWidth = Mathf.Max(.05f, tentacleWidth);
            tentacleSegments = Mathf.Clamp(tentacleSegments, 6, 24);
            tentacleCurve = Mathf.Max(0f, tentacleCurve);
            warningPulseScale = Mathf.Max(0f, warningPulseScale);
            if (dangerZone != null) dangerZone.enabled = false;
        }

        private void OnDrawGizmosSelected()
        {
            if (dangerZone != null)
            {
                Gizmos.color = new Color(1f, .15f, .25f, .8f);
                Matrix4x4 previous = Gizmos.matrix;
                Gizmos.matrix = dangerZone.transform.localToWorldMatrix;
                Gizmos.DrawWireCube(dangerZone.offset, dangerZone.size);
                Gizmos.matrix = previous;
            }

            if (flipSpawnPoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(flipSpawnPoint.position, .18f);
            }
        }
    }
}
