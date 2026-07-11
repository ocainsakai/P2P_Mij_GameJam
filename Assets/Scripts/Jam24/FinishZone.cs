using System;
using UnityEngine;

namespace Jam24
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class FinishZone : MonoBehaviour
    {
        private GameObject targetFlip;
        private Action reached;
        private bool completed;

        public void Configure(GameObject flip, Action onReached)
        {
            targetFlip = flip;
            reached = onReached;
            completed = false;
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (completed || targetFlip == null) return;

            GameObject enteredObject = other.attachedRigidbody != null
                ? other.attachedRigidbody.gameObject
                : other.gameObject;

            if (enteredObject != targetFlip && !enteredObject.transform.IsChildOf(targetFlip.transform)) return;

            completed = true;
            reached?.Invoke();
        }
    }
}
