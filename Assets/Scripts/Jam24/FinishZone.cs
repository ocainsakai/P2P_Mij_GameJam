using System;
using UnityEngine;

namespace Jam24
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class FinishZone : MonoBehaviour
    {
        private GameObject targetFlip;
        private Func<GameObject, bool> matchesFlip;
        private Action reached;
        private Action<GameObject> reachedWithObject;
        private bool completed;

        public void Configure(GameObject flip, Action onReached)
        {
            targetFlip = flip;
            matchesFlip = null;
            reached = onReached;
            reachedWithObject = null;
            completed = false;
            GetComponent<Collider2D>().isTrigger = true;
        }

        public void Configure(Func<GameObject, bool> flipMatcher, Action<GameObject> onReached)
        {
            targetFlip = null;
            matchesFlip = flipMatcher;
            reached = null;
            reachedWithObject = onReached;
            completed = false;
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (completed || targetFlip == null && matchesFlip == null) return;

            GameObject enteredObject = other.attachedRigidbody != null
                ? other.attachedRigidbody.gameObject
                : other.gameObject;

            bool matches = matchesFlip != null
                ? matchesFlip(enteredObject)
                : enteredObject == targetFlip || enteredObject.transform.IsChildOf(targetFlip.transform);
            if (!matches) return;

            if (reachedWithObject != null)
                reachedWithObject.Invoke(enteredObject);
            else
            {
                completed = true;
                reached?.Invoke();
            }
        }
    }
}
