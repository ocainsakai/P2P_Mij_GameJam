using UnityEngine;

namespace Jam24
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class FlipWallBounce2D : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float bounceSpeed = 1.5f;
        [SerializeField, Range(0f, 1f)] private float minimumHorizontalNormal = .65f;
        [SerializeField, Min(0f)] private float cooldown = .12f;

        private Rigidbody2D body;
        private float nextBounceTime;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (Time.time < nextBounceTime || body == null || body.constraints == RigidbodyConstraints2D.FreezeAll)
                return;

            Rigidbody2D otherBody = collision.rigidbody;
            if (otherBody != null && otherBody.bodyType == RigidbodyType2D.Dynamic) return;

            for (int i = 0; i < collision.contactCount; i++)
            {
                Vector2 normal = collision.GetContact(i).normal;
                if (Mathf.Abs(normal.x) < minimumHorizontalNormal) continue;

                Vector2 velocity = body.linearVelocity;
                velocity.x = Mathf.Sign(normal.x) * Mathf.Max(bounceSpeed, Mathf.Abs(velocity.x) * .35f);
                body.linearVelocity = velocity;
                nextBounceTime = Time.time + cooldown;
                return;
            }
        }

        private void OnValidate()
        {
            bounceSpeed = Mathf.Max(0f, bounceSpeed);
            minimumHorizontalNormal = Mathf.Clamp01(minimumHorizontalNormal);
            cooldown = Mathf.Max(0f, cooldown);
        }
    }
}
