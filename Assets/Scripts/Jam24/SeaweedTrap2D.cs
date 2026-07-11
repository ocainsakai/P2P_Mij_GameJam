using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Jam24
{
    [DisallowMultipleComponent]
    public sealed class SeaweedTrap2D : MonoBehaviour
    {
        [SerializeField] private Collider2D triggerZone;
        [SerializeField] private Transform holdPoint;
        [SerializeField] private string inWaterFlowBool = "InWaterFlow";
        [SerializeField] private bool releaseOnWaterFlow = true;

        private GameObject targetFlip;
        private Rigidbody2D caughtBody;
        private Transform originalParent;
        private int flowCount;

        public void ConfigureTargets(GameObject targetPlayer, GameObject flip) => targetFlip = flip;

        private void Awake()
        {
            if (triggerZone == null || holdPoint == null)
            {
                Debug.LogError($"Seaweed '{name}' needs Trigger Zone and Hold Point references.", this);
                enabled = false;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsFlip(other)) CatchFlip();
            if (other.GetComponentInParent<AreaEffector2D>() != null)
            {
                flowCount++;
                if (releaseOnWaterFlow) ReleaseFlip();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponentInParent<AreaEffector2D>() == null) return;
            flowCount = Mathf.Max(0, flowCount - 1);
        }

        private bool IsFlip(Collider2D other)
        {
            if (targetFlip == null || caughtBody != null) return false;
            GameObject entered = other.attachedRigidbody != null
                ? other.attachedRigidbody.gameObject
                : other.gameObject;
            return entered == targetFlip || entered.transform.IsChildOf(targetFlip.transform);
        }

        private void CatchFlip()
        {
            originalParent = targetFlip.transform.parent;
            caughtBody = targetFlip.GetComponent<Rigidbody2D>();
            if (caughtBody != null)
            {
                caughtBody.linearVelocity = Vector2.zero;
                caughtBody.angularVelocity = 0f;
                caughtBody.simulated = false;
            }
            targetFlip.transform.SetParent(holdPoint, true);
            targetFlip.transform.SetPositionAndRotation(holdPoint.position, holdPoint.rotation);
            if (releaseOnWaterFlow && flowCount > 0) ReleaseFlip();
        }

        public void ReleaseFlip()
        {
            if (targetFlip == null || caughtBody == null) return;
            targetFlip.transform.SetParent(originalParent, true);
            caughtBody.simulated = true;
            caughtBody.WakeUp();
            caughtBody = null;
            originalParent = null;
        }
#if UNITY_EDITOR
        [ContextMenu("Initialize Simple Setup")]
        private void InitializeSimpleSetup()
        {
            Undo.RecordObject(this, "Initialize Simple Seaweed");
            triggerZone = GetComponent<Collider2D>();
            if (triggerZone == null) triggerZone = Undo.AddComponent<BoxCollider2D>(gameObject);
            Undo.RecordObject(triggerZone, "Configure Seaweed Trigger");
            triggerZone.isTrigger = true;

            Rigidbody2D body = GetComponent<Rigidbody2D>();
            if (body == null) body = Undo.AddComponent<Rigidbody2D>(gameObject);
            Undo.RecordObject(body, "Configure Seaweed Rigidbody");
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;

            holdPoint = transform.Find("HoldPoint");
            if (holdPoint == null)
            {
                var go = new GameObject("HoldPoint");
                Undo.RegisterCreatedObjectUndo(go, "Create Seaweed Hold Point");
                Undo.SetTransformParent(go.transform, transform, "Parent Hold Point");
                holdPoint = go.transform;
                holdPoint.localPosition = Vector3.zero;
            }
            EditorUtility.SetDirty(this);
            PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        }
#endif
    }
}
