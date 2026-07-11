using UnityEngine;

namespace Jam24
{
    /// <summary>Relays item contacts from the moving thief shark body.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class ThiefSharkMouth : MonoBehaviour
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
            owner?.TrySteal(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            owner?.TrySteal(other);
        }
    }
}
