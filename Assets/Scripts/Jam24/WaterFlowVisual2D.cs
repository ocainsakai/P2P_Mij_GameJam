using UnityEngine;

namespace Jam24
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class WaterFlowVisual2D : MonoBehaviour
    {
        [SerializeField] private BoxCollider2D flowZone;
        [SerializeField] private SpriteRenderer[] layers;
        [SerializeField, Min(.05f)] private float animationSpeed = .65f;
        [SerializeField, Range(0f, 1f)] private float opacity = .7f;
        [SerializeField, Range(0f, .1f)] private float horizontalDrift = .025f;
        [SerializeField] private Color tint = new(.55f, .95f, 1f, 1f);

        private Vector3[] fittedPositions;
        private Vector2 fittedColliderSize;
        private Vector2 fittedColliderOffset;

        private void Awake()
        {
            ResolveReferences();
            FitToCollider();
        }

        private void OnEnable()
        {
            ResolveReferences();
            FitToCollider();
        }

        private void Update()
        {
            if (flowZone != null && (flowZone.size != fittedColliderSize || flowZone.offset != fittedColliderOffset))
                FitToCollider();
            if (!Application.isPlaying || layers == null || layers.Length == 0) return;
            if (fittedPositions == null || fittedPositions.Length != layers.Length) FitToCollider();

            float time = Time.time * animationSpeed;
            float width = flowZone == null ? 1f : flowZone.size.x;
            for (int i = 0; i < layers.Length; i++)
            {
                SpriteRenderer layer = layers[i];
                if (layer == null) continue;

                float phase = time + i * 2.0943952f;
                Vector3 position = fittedPositions[i];
                position.x += Mathf.Sin(phase) * width * horizontalDrift;
                position.y += Mathf.Sin(phase * .73f) * .025f;
                layer.transform.localPosition = position;

                float alpha = opacity * Mathf.Lerp(.45f, 1f, (Mathf.Sin(phase) + 1f) * .5f);
                Color color = tint;
                color.a = alpha;
                layer.color = color;
            }
        }

        private void OnValidate()
        {
            animationSpeed = Mathf.Max(.05f, animationSpeed);
            ResolveReferences();
            FitToCollider();
        }

        private void ResolveReferences()
        {
            if (flowZone == null) flowZone = GetComponent<BoxCollider2D>();
            if (layers == null || layers.Length == 0)
            {
                Transform visualRoot = transform.Find("Flow Sprite Visual");
                if (visualRoot != null) layers = visualRoot.GetComponentsInChildren<SpriteRenderer>(true);
            }
        }

        public void Configure(BoxCollider2D collider, SpriteRenderer[] spriteLayers)
        {
            flowZone = collider;
            layers = spriteLayers;
            FitToCollider();
        }

        [ContextMenu("Fit Visual To Flow Zone")]
        public void FitToCollider()
        {
            if (flowZone == null || layers == null) return;
            fittedColliderSize = flowZone.size;
            fittedColliderOffset = flowZone.offset;
            fittedPositions = new Vector3[layers.Length];

            for (int i = 0; i < layers.Length; i++)
            {
                SpriteRenderer layer = layers[i];
                if (layer == null || layer.sprite == null) continue;

                Vector2 spriteSize = layer.sprite.bounds.size;
                if (spriteSize.x <= 0f || spriteSize.y <= 0f) continue;

                Transform layerTransform = layer.transform;
                layerTransform.localRotation = Quaternion.identity;
                layerTransform.localPosition = flowZone.offset;
                layerTransform.localScale = new Vector3(
                    flowZone.size.x / spriteSize.x,
                    flowZone.size.y / spriteSize.y,
                    1f);
                fittedPositions[i] = layerTransform.localPosition;

                Color color = tint;
                color.a = opacity * (.55f + i * .12f);
                layer.color = color;
                layer.sortingOrder = 4 + i;
            }
        }
    }
}
