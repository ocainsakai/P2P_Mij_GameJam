using System.Collections;
using UnityEngine;

namespace Jam24
{
    /// <summary>
    /// Grabs the first dynamic Rigidbody2D entering its detection zone.
    /// A grabbed Flip consumes one Flip life through GameplayManager.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class GrabCrab : MonoBehaviour
    {
        [SerializeField] private Collider2D detectionZone;
        [SerializeField] private Transform holdPoint;
        [SerializeField] private Transform leftClaw;
        [SerializeField] private Transform rightClaw;
        [SerializeField, Min(.05f)] private float grabDuration = .25f;
        [SerializeField, Range(0f, 60f)] private float clawCloseAngle = 28f;

        [Header("Escape After Stealing")]
        [SerializeField, Min(0f)] private float fleeDistance = 3f;
        [SerializeField, Min(.05f)] private float fleeDuration = 1f;

        public bool IsGrabbing { get; private set; }
        public bool IsFleeing { get; private set; }

        private Quaternion leftClawOpenRotation;
        private Quaternion rightClawOpenRotation;

        private void Awake()
        {
            if (detectionZone == null) detectionZone = GetComponentInChildren<Collider2D>(true);
            if (holdPoint == null) holdPoint = transform;
            if (leftClaw != null) leftClawOpenRotation = leftClaw.localRotation;
            if (rightClaw != null) rightClawOpenRotation = rightClaw.localRotation;

            Rigidbody2D crabBody = GetComponent<Rigidbody2D>();
            crabBody.bodyType = RigidbodyType2D.Static;
            if (detectionZone != null) detectionZone.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsGrabbing || IsFleeing) return;
            Rigidbody2D targetBody = other.attachedRigidbody;
            if (targetBody == null || targetBody.bodyType != RigidbodyType2D.Dynamic) return;
            if (targetBody.transform == transform || targetBody.transform.IsChildOf(transform)) return;

            // The crab faces left to steal and then escapes to the right.
            // Checking local space keeps this rule correct even if the prefab is rotated.
            Vector3 localTargetPosition = transform.InverseTransformPoint(targetBody.worldCenterOfMass);
            if (localTargetPosition.x >= 0f) return;

            StartCoroutine(Grab(targetBody));
        }

        private IEnumerator Grab(Rigidbody2D targetBody)
        {
            IsGrabbing = true;
            GameObject target = targetBody.gameObject;
            Vector3 startPosition = target.transform.position;
            Vector3 startScale = target.transform.localScale;

            foreach (Collider2D targetCollider in target.GetComponentsInChildren<Collider2D>())
                targetCollider.enabled = false;
            targetBody.simulated = false;
            target.transform.SetParent(holdPoint, true);

            float elapsed = 0f;
            while (elapsed < grabDuration && target != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / grabDuration));
                target.transform.position = Vector3.LerpUnclamped(startPosition, holdPoint.position, t);
                target.transform.localScale = Vector3.LerpUnclamped(startScale, startScale * .15f, t);
                AnimateClaws(t);
                yield return null;
            }

            if (target != null)
            {
                target.SetActive(false);
                bool wasFlip = GameplayManager.Instance != null && GameplayManager.Instance.TryConsumeFlip(target);
                if (!wasFlip)
                {
                    if (target.GetComponent<OctopusPlayerMovement>() != null) GameFlow.Instance?.Lose();
                    Destroy(target);
                }
            }

            yield return null;
            IsGrabbing = false;
            yield return FleeRight();
        }

        private IEnumerator FleeRight()
        {
            IsFleeing = true;
            if (detectionZone != null) detectionZone.enabled = false;

            Vector3 start = transform.position;
            Vector3 destination = start + Vector3.right * fleeDistance;
            float elapsed = 0f;
            while (elapsed < fleeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fleeDuration));
                transform.position = Vector3.LerpUnclamped(start, destination, t);
                yield return null;
            }

            transform.position = destination;
            AnimateClaws(0f);
            IsFleeing = false;
        }

        private void AnimateClaws(float amount)
        {
            if (leftClaw != null)
                leftClaw.localRotation = leftClawOpenRotation * Quaternion.Euler(0f, 0f, -clawCloseAngle * amount);
            if (rightClaw != null)
                rightClaw.localRotation = rightClawOpenRotation * Quaternion.Euler(0f, 0f, clawCloseAngle * amount);
        }

        private void OnDrawGizmosSelected()
        {
            if (detectionZone == null) return;
            Gizmos.color = new Color(1f, .25f, .2f, .18f);
            Gizmos.DrawWireCube(detectionZone.bounds.center, detectionZone.bounds.size);

            Gizmos.color = new Color(1f, .85f, .2f, .8f);
            Vector3 destination = transform.position + Vector3.right * fleeDistance;
            Gizmos.DrawLine(transform.position, destination);
            Gizmos.DrawWireSphere(destination, .12f);
        }

        private void OnValidate()
        {
            grabDuration = Mathf.Max(.05f, grabDuration);
            fleeDistance = Mathf.Max(0f, fleeDistance);
            fleeDuration = Mathf.Max(.05f, fleeDuration);
        }
    }
}
