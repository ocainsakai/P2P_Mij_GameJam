using UnityEngine;

namespace Jam24
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class PlayerDeadZone2D : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            GameObject entered = other.attachedRigidbody != null
                ? other.attachedRigidbody.gameObject
                : other.gameObject;

            GameplayManager.Instance?.TryLosePlayer(entered);
        }
    }
}
