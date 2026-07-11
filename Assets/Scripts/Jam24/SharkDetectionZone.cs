using UnityEngine;

namespace Jam24
{
    /// <summary>Relays trigger contacts from the shark's child detection zone.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class SharkDetectionZone : MonoBehaviour
    {
        private SharkStalker owner;

        public void SetOwner(SharkStalker value) => owner = value;

        private void Awake()
        {
            if (owner == null) owner = GetComponentInParent<SharkStalker>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            OctopusPlayerMovement player = other.GetComponentInParent<OctopusPlayerMovement>();
            if (player != null) owner?.PlayerEntered(player);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            OctopusPlayerMovement player = other.GetComponentInParent<OctopusPlayerMovement>();
            if (player != null) owner?.PlayerExited(player);
        }
    }
}
