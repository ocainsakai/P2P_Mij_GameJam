using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

namespace Jam24
{
    /// <summary>
    /// Water drag zone: slows every simulated Rigidbody2D except the player
    /// while it overlaps the seaweed, then restores its original damping.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SeaweedTrap2D : MonoBehaviour
    {
        private static readonly Dictionary<Rigidbody2D, SeaweedTrap2D> HeldFlipOwners = new();

        [SerializeField] private Collider2D triggerZone;
        [SerializeField, Range(.05f, 1f)] private float entryVelocityMultiplier = .45f;
        [SerializeField, Min(0f)] private float extraLinearDamping = 6f;
        [SerializeField, Min(0f)] private float pullToCenterDuration = .25f;
        [FormerlySerializedAs("trappedYOffset")]
        [SerializeField] private float trappedXOffset;
        [SerializeField, Min(0f)] private float currentReleaseSpeed = 3.5f;

        private readonly Dictionary<Rigidbody2D, SlowState> slowedBodies = new();
        private readonly HashSet<FlowLayerLooper2D> activeFlows = new();
        private Rigidbody2D seaweedBody;
        private GameObject targetFlip;

        public bool IsInFlow => activeFlows.Count > 0;
        public event Action<bool> FlowStateChanged;

        private sealed class SlowState
        {
            public float OriginalLinearDamping;
            public float OriginalAngularDamping;
            public RigidbodyConstraints2D OriginalConstraints;
            public int OverlapCount;
            public bool IsFlip;
            public bool IsHeldFlip;
            public Tween PullTween;
        }

        public void ConfigureTargets(GameObject targetPlayer, GameObject flip)
        {
            targetFlip = flip;
        }

        public static bool TryReleaseHeldFlip(Rigidbody2D body, FlowLayerLooper2D flow)
        {
            if (body == null || flow == null || !HeldFlipOwners.TryGetValue(body, out SeaweedTrap2D owner))
                return false;

            if (owner == null)
            {
                HeldFlipOwners.Remove(body);
                return false;
            }

            return owner.ReleaseHeldFlip(body, flow);
        }

        public void SetFlowState(FlowLayerLooper2D flow, bool isFlowingOver)
        {
            if (flow == null) return;

            bool wasInFlow = IsInFlow;
            if (isFlowingOver) activeFlows.Add(flow);
            else activeFlows.Remove(flow);

            if (wasInFlow != IsInFlow)
            {
                if (IsInFlow) ReleaseHeldFlips(flow);
                else HoldOverlappingFlips();
                FlowStateChanged?.Invoke(IsInFlow);
            }
        }

        private void Awake()
        {
            seaweedBody = GetComponent<Rigidbody2D>();
            if (triggerZone == null) triggerZone = GetComponentInChildren<Collider2D>(true);
            if (triggerZone == null)
            {
                Debug.LogError($"Seaweed '{name}' needs a trigger Collider2D.", this);
                enabled = false;
                return;
            }
            triggerZone.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Rigidbody2D body = GetSlowableBody(other);
            if (body == null) return;

            if (slowedBodies.TryGetValue(body, out SlowState existing))
            {
                existing.OverlapCount++;
                return;
            }

            var state = new SlowState
            {
                OriginalLinearDamping = body.linearDamping,
                OriginalAngularDamping = body.angularDamping,
                OriginalConstraints = body.constraints,
                IsFlip = IsTargetFlip(body.gameObject),
                OverlapCount = 1
            };
            slowedBodies.Add(body, state);

            if (state.IsFlip && !IsInFlow)
            {
                HoldFlip(body, state);
                return;
            }

            body.linearVelocity *= entryVelocityMultiplier;
            body.angularVelocity *= entryVelocityMultiplier;
            body.linearDamping = state.OriginalLinearDamping + extraLinearDamping;
            body.angularDamping = state.OriginalAngularDamping + extraLinearDamping;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            Rigidbody2D body = other.attachedRigidbody;
            if (body == null || !slowedBodies.TryGetValue(body, out SlowState state)) return;

            state.OverlapCount--;
            if (state.OverlapCount > 0) return;
            RemoveHeldFlipOwner(body);
            RestoreBody(body, state);
            slowedBodies.Remove(body);
        }

        private Rigidbody2D GetSlowableBody(Collider2D other)
        {
            if (other.GetComponentInParent<OctopusPlayerMovement>() != null) return null;

            Rigidbody2D body = other.attachedRigidbody;
            if (body == null || body == seaweedBody || !body.simulated || body.bodyType == RigidbodyType2D.Static)
                return null;
            return body;
        }

        private bool IsTargetFlip(GameObject candidate)
        {
            if (GameplayManager.Instance != null && GameplayManager.Instance.IsFlip(candidate))
                return true;

            if (targetFlip != null)
                return candidate == targetFlip || candidate.transform.IsChildOf(targetFlip.transform);

            return false;
        }

        private void ReleaseHeldFlips(FlowLayerLooper2D flow)
        {
            foreach (KeyValuePair<Rigidbody2D, SlowState> pair in slowedBodies)
            {
                Rigidbody2D body = pair.Key;
                SlowState state = pair.Value;
                if (body == null || !state.IsHeldFlip) continue;

                ReleaseHeldFlip(body, flow);
            }
        }

        private bool ReleaseHeldFlip(Rigidbody2D body, FlowLayerLooper2D flow)
        {
            if (body == null || !slowedBodies.TryGetValue(body, out SlowState state) || !state.IsHeldFlip)
                return false;

            state.IsHeldFlip = false;
            RemoveHeldFlipOwner(body);
            state.PullTween?.Kill();
            state.PullTween = null;
            body.constraints = state.OriginalConstraints;
            body.linearDamping = state.OriginalLinearDamping;
            body.angularDamping = state.OriginalAngularDamping;
            PushAlongCurrent(body, flow);
            body.WakeUp();
            return true;
        }

        private void PushAlongCurrent(Rigidbody2D body, FlowLayerLooper2D flow)
        {
            if (body == null || flow == null) return;

            AreaEffector2D effector = flow.GetComponent<AreaEffector2D>();
            if (effector == null) effector = flow.GetComponentInChildren<AreaEffector2D>(true);
            if (effector == null) return;

            float angle = effector.forceAngle * Mathf.Deg2Rad;
            Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
            if (!effector.useGlobalAngle)
                direction = flow.transform.TransformDirection(direction).normalized;

            body.linearVelocity = direction * currentReleaseSpeed;
        }

        private void HoldOverlappingFlips()
        {
            foreach (KeyValuePair<Rigidbody2D, SlowState> pair in slowedBodies)
                if (pair.Key != null && pair.Value.IsFlip && !pair.Value.IsHeldFlip)
                    HoldFlip(pair.Key, pair.Value);
        }

        private void HoldFlip(Rigidbody2D body, SlowState state)
        {
            state.IsHeldFlip = true;
            HeldFlipOwners[body] = this;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            state.PullTween?.Kill();

            if (pullToCenterDuration <= 0f)
            {
                body.position = new Vector2(transform.position.x + trappedXOffset, body.position.y);
                body.constraints = RigidbodyConstraints2D.FreezeAll;
                GameplayManager.Instance?.TryLoseFromSeaweedTrap(body.gameObject);
            }
            else
            {
                body.constraints = state.OriginalConstraints |
                                   RigidbodyConstraints2D.FreezePositionY |
                                   RigidbodyConstraints2D.FreezeRotation;
                state.PullTween = body.DOMoveX(transform.position.x + trappedXOffset, pullToCenterDuration)
                    .SetEase(Ease.OutQuad)
                    .SetTarget(this)
                    .OnComplete(() =>
                    {
                        state.PullTween = null;
                        if (body != null && state.IsHeldFlip)
                        {
                            body.constraints = RigidbodyConstraints2D.FreezeAll;
                            GameplayManager.Instance?.TryLoseFromSeaweedTrap(body.gameObject);
                        }
                    });
            }
        }

        private void OnDisable()
        {
            foreach (KeyValuePair<Rigidbody2D, SlowState> pair in slowedBodies)
            {
                if (pair.Key == null) continue;
                RemoveHeldFlipOwner(pair.Key);
                RestoreBody(pair.Key, pair.Value);
            }
            slowedBodies.Clear();

            bool wasInFlow = IsInFlow;
            activeFlows.Clear();
            if (wasInFlow) FlowStateChanged?.Invoke(false);
        }

        private void RemoveHeldFlipOwner(Rigidbody2D body)
        {
            if (body != null && HeldFlipOwners.TryGetValue(body, out SeaweedTrap2D owner) && owner == this)
                HeldFlipOwners.Remove(body);
        }

        private static void RestoreBody(Rigidbody2D body, SlowState state)
        {
            state.PullTween?.Kill();
            state.PullTween = null;
            body.linearDamping = state.OriginalLinearDamping;
            body.angularDamping = state.OriginalAngularDamping;
            body.constraints = state.OriginalConstraints;
            body.WakeUp();
        }

        private void OnValidate()
        {
            entryVelocityMultiplier = Mathf.Clamp(entryVelocityMultiplier, .05f, 1f);
            extraLinearDamping = Mathf.Max(0f, extraLinearDamping);
            pullToCenterDuration = Mathf.Max(0f, pullToCenterDuration);
            currentReleaseSpeed = Mathf.Max(0f, currentReleaseSpeed);
            if (triggerZone != null) triggerZone.isTrigger = true;
        }
    }
}
