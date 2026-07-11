using System.Collections.Generic;
using UnityEngine;

namespace Jam24
{
    /// <summary>
    /// Waits after the player enters its activation zone, displays a warning,
    /// then sweeps from Start Point to End Point and permanently removes every
    /// dynamic item it touches. The player and static mechanisms are ignored.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ThiefShark : MonoBehaviour
    {
        private enum ThiefSharkState { Idle, Warning, Sweeping, Finished }

        [Header("References")]
        [SerializeField] private Rigidbody2D sharkBody;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private Transform startPoint;
        [SerializeField] private Transform endPoint;
        [SerializeField] private ThiefSharkDetectionZone activationZone;
        [SerializeField] private Collider2D theftZone;
        [SerializeField] private ThiefSharkMouth stealTrigger;
        [SerializeField] private TextMesh warningLabel;

        [Header("Warning")]
        [SerializeField, Min(.1f)] private float warningDuration = 5f;
        [SerializeField] private bool cancelIfPlayerLeaves = true;

        [Header("Sweep")]
        [SerializeField, Min(.1f)] private float sweepSpeed = 9f;

        private readonly HashSet<Rigidbody2D> stolenItems = new();
        private OctopusPlayerMovement triggeringPlayer;
        private ThiefSharkState state;
        private float warningTimer;

        private void Awake()
        {
            if (sharkBody == null) sharkBody = GetComponentInChildren<Rigidbody2D>(true);
            if (bodyRenderer == null && sharkBody != null)
                bodyRenderer = sharkBody.GetComponent<SpriteRenderer>();
            if (activationZone == null)
                activationZone = GetComponentInChildren<ThiefSharkDetectionZone>(true);
            if (stealTrigger == null)
                stealTrigger = GetComponentInChildren<ThiefSharkMouth>(true);
            if (warningLabel == null) warningLabel = GetComponentInChildren<TextMesh>(true);

            activationZone?.SetOwner(this);
            stealTrigger?.SetOwner(this);
            if (theftZone != null) theftZone.isTrigger = true;

            if (sharkBody == null || bodyRenderer == null || startPoint == null || endPoint == null)
            {
                Debug.LogError("Thief Shark needs a body renderer, Rigidbody2D, Start Point and End Point.", this);
                enabled = false;
                return;
            }

            sharkBody.bodyType = RigidbodyType2D.Kinematic;
            sharkBody.gravityScale = 0f;
            sharkBody.freezeRotation = true;
            SetWarningVisible(false);
            SetSharkVisible(false);
        }

        private void OnEnable()
        {
            state = ThiefSharkState.Idle;
            triggeringPlayer = null;
            warningTimer = 0f;
            stolenItems.Clear();
        }

        private void Update()
        {
            if (state != ThiefSharkState.Warning) return;
            if (GameFlow.Instance != null && GameFlow.Instance.State == GameState.Paused) return;

            if (triggeringPlayer == null)
            {
                CancelWarning();
                return;
            }

            warningTimer += Time.deltaTime;
            float remaining = Mathf.Max(0f, warningDuration - warningTimer);
            SetWarningText($"WARNING!\nTHIEF SHARK IN {remaining:0.0}s");

            if (warningTimer >= warningDuration) BeginSweep();
        }

        private void FixedUpdate()
        {
            if (state != ThiefSharkState.Sweeping) return;
            if (GameFlow.Instance != null && GameFlow.Instance.State == GameState.Paused) return;

            Vector2 destination = endPoint.position;
            Vector2 next = Vector2.MoveTowards(
                sharkBody.position,
                destination,
                sweepSpeed * Time.fixedDeltaTime);
            sharkBody.MovePosition(next);

            if (Vector2.Distance(next, destination) <= .02f) FinishSweep();
        }

        public void PlayerEntered(OctopusPlayerMovement player)
        {
            if (player == null || state != ThiefSharkState.Idle) return;

            triggeringPlayer = player;
            warningTimer = 0f;
            state = ThiefSharkState.Warning;
            SetWarningVisible(true);
            SetWarningText($"WARNING!\nTHIEF SHARK IN {warningDuration:0.0}s");
        }

        public void PlayerExited(OctopusPlayerMovement player)
        {
            if (!cancelIfPlayerLeaves) return;
            if (state == ThiefSharkState.Warning && triggeringPlayer == player) CancelWarning();
        }

        public void TrySteal(Collider2D other)
        {
            if (state != ThiefSharkState.Sweeping || other == null) return;
            if (other.GetComponentInParent<OctopusPlayerMovement>() != null) return;

            Rigidbody2D itemBody = other.attachedRigidbody;
            if (itemBody == null || itemBody == sharkBody || !itemBody.simulated) return;
            if (itemBody.bodyType != RigidbodyType2D.Dynamic) return;
            if (itemBody.transform.IsChildOf(transform)) return;

            GameObject item = itemBody.gameObject;
            if (!stolenItems.Add(itemBody)) return;

            GameplayManager.Instance?.TryStealFlipPermanently(item);

            foreach (Collider2D itemCollider in item.GetComponentsInChildren<Collider2D>(true))
                itemCollider.enabled = false;
            foreach (Renderer itemRenderer in item.GetComponentsInChildren<Renderer>(true))
                itemRenderer.enabled = false;

            itemBody.simulated = false;
            item.SetActive(false);
            Destroy(item);
        }

        private void BeginSweep()
        {
            state = ThiefSharkState.Sweeping;
            triggeringPlayer = null;
            GameplayManager.Instance?.DisableFlipRespawnForCurrentLevel();

            sharkBody.position = startPoint.position;
            bodyRenderer.flipX = endPoint.position.x < startPoint.position.x;
            SetSharkVisible(true);
            SetWarningText("THIEF SHARK!");
            StealAllItemsInZone();
        }

        private void StealAllItemsInZone()
        {
            if (theftZone == null) return;

            Bounds bounds = theftZone.bounds;
            Collider2D[] hits = Physics2D.OverlapBoxAll(bounds.center, bounds.size, 0f);
            foreach (Collider2D hit in hits) TrySteal(hit);
        }

        private void FinishSweep()
        {
            state = ThiefSharkState.Finished;
            sharkBody.position = endPoint.position;
            SetSharkVisible(false);
            SetWarningVisible(false);
            if (activationZone != null) activationZone.gameObject.SetActive(false);
        }

        private void CancelWarning()
        {
            triggeringPlayer = null;
            warningTimer = 0f;
            state = ThiefSharkState.Idle;
            SetWarningVisible(false);
        }

        private void SetSharkVisible(bool visible)
        {
            if (sharkBody != null) sharkBody.gameObject.SetActive(visible);
        }

        private void SetWarningVisible(bool visible)
        {
            if (warningLabel != null) warningLabel.gameObject.SetActive(visible);
        }

        private void SetWarningText(string value)
        {
            if (warningLabel != null) warningLabel.text = value;
        }

        private void OnDrawGizmosSelected()
        {
            if (startPoint == null || endPoint == null) return;

            Gizmos.color = new Color(1f, .2f, .1f, .85f);
            Gizmos.DrawLine(startPoint.position, endPoint.position);
            Gizmos.DrawWireSphere(startPoint.position, .18f);
            Gizmos.DrawWireSphere(endPoint.position, .18f);
        }

        private void OnValidate()
        {
            warningDuration = Mathf.Max(.1f, warningDuration);
            sweepSpeed = Mathf.Max(.1f, sweepSpeed);
        }
    }
}
