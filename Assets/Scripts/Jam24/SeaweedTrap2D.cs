using System.Collections.Generic;
using UnityEngine;

namespace Jam24
{
    /// <summary>
    /// Water drag zone: slows every simulated Rigidbody2D except the player
    /// while it overlaps the seaweed, then restores its original damping.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SeaweedTrap2D : MonoBehaviour
    {
        [SerializeField] private Collider2D triggerZone;
        [SerializeField, Range(.05f, 1f)] private float entryVelocityMultiplier = .45f;
        [SerializeField, Min(0f)] private float extraLinearDamping = 6f;

        private readonly Dictionary<Rigidbody2D, SlowState> slowedBodies = new();
        private Rigidbody2D seaweedBody;

        private sealed class SlowState
        {
            public float OriginalLinearDamping;
            public float OriginalAngularDamping;
            public int OverlapCount;
        }

        // Kept for compatibility with GameplayManager. Seaweed is now generic
        // and no longer needs explicit Player/Flip references.
        public void ConfigureTargets(GameObject targetPlayer, GameObject flip)
        {
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
                OverlapCount = 1
            };
            slowedBodies.Add(body, state);

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

        private void OnDisable()
        {
            foreach (KeyValuePair<Rigidbody2D, SlowState> pair in slowedBodies)
                if (pair.Key != null) RestoreBody(pair.Key, pair.Value);
            slowedBodies.Clear();
        }

        private static void RestoreBody(Rigidbody2D body, SlowState state)
        {
            body.linearDamping = state.OriginalLinearDamping;
            body.angularDamping = state.OriginalAngularDamping;
            body.WakeUp();
        }

        private void OnValidate()
        {
            entryVelocityMultiplier = Mathf.Clamp(entryVelocityMultiplier, .05f, 1f);
            extraLinearDamping = Mathf.Max(0f, extraLinearDamping);
            if (triggerZone != null) triggerZone.isTrigger = true;
        }
    }
}
