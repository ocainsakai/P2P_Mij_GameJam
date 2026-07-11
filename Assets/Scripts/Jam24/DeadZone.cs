using System;
using UnityEngine;

namespace Jam24
{
    [Flags]
    public enum DeadZoneTarget
    {
        Player = 1 << 0,
        Flip = 1 << 1,
        Both = Player | Flip
    }

    [RequireComponent(typeof(Collider2D))]
    public sealed class DeadZone : MonoBehaviour
    {
        [SerializeField] private DeadZoneTarget targets = DeadZoneTarget.Both;

        public DeadZoneTarget Targets => targets;

        private GameObject player;
        private GameObject flip;
        private Action playerEntered;
        private Action flipEntered;
        private bool consumed;

        public void Configure(GameObject targetPlayer, GameObject targetFlip,
            Action onPlayerEntered, Action onFlipEntered)
        {
            player = targetPlayer;
            flip = targetFlip;
            playerEntered = onPlayerEntered;
            flipEntered = onFlipEntered;
            consumed = false;
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (consumed) return;

            GameObject entered = other.attachedRigidbody != null
                ? other.attachedRigidbody.gameObject
                : other.gameObject;

            if (targets.HasFlag(DeadZoneTarget.Flip) && Matches(entered, flip))
            {
                consumed = true;
                flipEntered?.Invoke();
            }
            else if (targets.HasFlag(DeadZoneTarget.Player) && Matches(entered, player))
            {
                consumed = true;
                playerEntered?.Invoke();
            }
        }

        private static bool Matches(GameObject entered, GameObject target) =>
            target != null && (entered == target || entered.transform.IsChildOf(target.transform));
    }
}
