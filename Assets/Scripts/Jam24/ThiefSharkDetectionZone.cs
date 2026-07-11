using UnityEngine;

namespace Jam24
{
    /// <summary>Relays player contacts from the thief shark activation zone.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class ThiefSharkDetectionZone : MonoBehaviour
    {
        private ThiefShark owner;

        public void SetOwner(ThiefShark value) => owner = value;

        private void Awake()
        {
            if (owner == null) owner = GetComponentInParent<ThiefShark>();
            GetComponent<Collider2D>().isTrigger = true;
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
