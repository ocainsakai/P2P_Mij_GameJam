using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace Jam24
{
    [DisallowMultipleComponent]
    public sealed class FlowLayerLooper2D : MonoBehaviour
    {
        [SerializeField] private Transform[] layers;
        [SerializeField] private BoxCollider2D flowZone;
        [SerializeField] private Material fadeMaterial;
        [SerializeField, Range(.01f, .49f)] private float edgeFade = .18f;
        [FormerlySerializedAs("fadeAllEdges")]
        [SerializeField] private bool blurWholeFlow;
        [SerializeField, Range(.25f, 4f)] private float blurSize = 1.25f;
        [SerializeField] private float speed = .6f;
        [SerializeField] private Vector2 layerSpeedMultiplierRange = new(.75f, 1.35f);
        [SerializeField, Min(.01f)] private float loopWidth = 1f;

        private Vector3[] startPositions;
        private float[] tileWidths;
        private float[] layerSpeedMultipliers;
        private Vector2 fittedSize;
        private Vector2 fittedOffset;
        private Vector3 fittedLossyScale;
        private bool fitPending = true;
        private readonly Dictionary<SeaweedTrap2D, int> overlappingSeaweed = new();

        private void Awake()
        {
            ResolveReferences();
            fitPending = true;
        }

        private void OnEnable()
        {
            ResolveReferences();
            fitPending = true;
        }

        private void Start()
        {
            FitLayersToZone();
        }

        private void Update()
        {
            if (fitPending || flowZone != null &&
                (flowZone.size != fittedSize || flowZone.offset != fittedOffset || transform.lossyScale != fittedLossyScale))
                FitLayersToZone();
            if (!Application.isPlaying || layers == null || startPositions == null || tileWidths == null || layerSpeedMultipliers == null) return;

            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i] == null) continue;
                float width = Mathf.Max(.01f, tileWidths[i]);
                float multiplier = i < layerSpeedMultipliers.Length ? layerSpeedMultipliers[i] : 1f;
                float offset = Mathf.Repeat(Time.time * speed * multiplier, width);
                Vector3 position = startPositions[i];
                position.x += offset;
                layers[i].localPosition = position;
            }
        }

        private void OnDisable()
        {
            foreach (SeaweedTrap2D trap in overlappingSeaweed.Keys)
                if (trap != null) trap.SetFlowState(this, false);
            overlappingSeaweed.Clear();
            RestorePositions();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            SeaweedTrap2D trap = other.GetComponentInParent<SeaweedTrap2D>();
            if (trap == null) return;

            overlappingSeaweed.TryGetValue(trap, out int overlapCount);
            overlappingSeaweed[trap] = overlapCount + 1;
            if (overlapCount == 0) trap.SetFlowState(this, true);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            SeaweedTrap2D trap = other.GetComponentInParent<SeaweedTrap2D>();
            if (trap == null || !overlappingSeaweed.TryGetValue(trap, out int overlapCount)) return;

            overlapCount--;
            if (overlapCount > 0)
            {
                overlappingSeaweed[trap] = overlapCount;
                return;
            }

            overlappingSeaweed.Remove(trap);
            trap.SetFlowState(this, false);
        }

        private void OnValidate()
        {
            loopWidth = Mathf.Max(.01f, loopWidth);
            edgeFade = Mathf.Clamp(edgeFade, .01f, .49f);
            blurSize = Mathf.Clamp(blurSize, .25f, 4f);
            if (layerSpeedMultiplierRange.x > layerSpeedMultiplierRange.y)
                layerSpeedMultiplierRange = new Vector2(layerSpeedMultiplierRange.y, layerSpeedMultiplierRange.x);
            layerSpeedMultipliers = null;
            ResolveReferences();
            fitPending = true;
        }

        public void Configure(BoxCollider2D collider, Transform[] movingLayers)
        {
            flowZone = collider;
            layers = movingLayers;
            if (flowZone != null) loopWidth = flowZone.size.x;
            fitPending = true;
        }

        private void ResolveReferences()
        {
            if (flowZone == null) flowZone = GetComponent<BoxCollider2D>();
            if (layers == null || layers.Length == 0)
            {
                layers = GetComponentsInChildren<SpriteRenderer>(true)
                    .Select(renderer => renderer.transform)
                    .Where(layer => layer != transform)
                    .ToArray();
            }
        }

        [ContextMenu("Fit Tiled Layers To Flow Zone")]
        public void FitLayersToZone()
        {
            if (flowZone == null || layers == null) return;
            fitPending = false;
            fittedSize = flowZone.size;
            fittedOffset = flowZone.offset;
            fittedLossyScale = transform.lossyScale;
            startPositions = new Vector3[layers.Length];
            tileWidths = new float[layers.Length];
            EnsureLayerSpeeds();

            float parentScaleX = Mathf.Max(.0001f, Mathf.Abs(fittedLossyScale.x));
            float parentScaleY = Mathf.Max(.0001f, Mathf.Abs(fittedLossyScale.y));
            float zoneWorldWidth = flowZone.size.x * parentScaleX;
            float zoneWorldHeight = flowZone.size.y * parentScaleY;

            for (int i = 0; i < layers.Length; i++)
            {
                Transform layer = layers[i];
                if (layer == null || !layer.TryGetComponent(out SpriteRenderer renderer) || renderer.sprite == null) continue;

                Vector2 spriteSize = renderer.sprite.bounds.size;
                float targetWorldScale = zoneWorldHeight / Mathf.Max(.01f, spriteSize.y);
                float tileWorldWidth = spriteSize.x * targetWorldScale;
                float tileLocalWidth = tileWorldWidth / parentScaleX;

                renderer.drawMode = SpriteDrawMode.Tiled;
                renderer.tileMode = SpriteTileMode.Continuous;
                if (fadeMaterial != null) renderer.sharedMaterial = fadeMaterial;
                renderer.size = new Vector2(
                    (zoneWorldWidth + tileWorldWidth * 2f) / targetWorldScale,
                    spriteSize.y);

                layer.localRotation = Quaternion.identity;
                layer.localScale = new Vector3(
                    targetWorldScale / parentScaleX,
                    targetWorldScale / parentScaleY,
                    1f);
                layer.localPosition = new Vector3(
                    flowZone.offset.x - tileLocalWidth,
                    flowZone.offset.y,
                    layer.localPosition.z);

                startPositions[i] = layer.localPosition;
                tileWidths[i] = tileLocalWidth;
                ApplyEdgeFade(renderer);
            }
        }

        private void ApplyEdgeFade(SpriteRenderer renderer)
        {
            float halfWidth = flowZone.size.x * .5f;
            Vector3 start = transform.TransformPoint(new Vector3(
                flowZone.offset.x - halfWidth, flowZone.offset.y, 0f));
            Vector3 end = transform.TransformPoint(new Vector3(
                flowZone.offset.x + halfWidth, flowZone.offset.y, 0f));
            Vector3 direction = end - start;
            float width = Mathf.Max(.01f, direction.magnitude);
            direction /= width;

            var properties = new MaterialPropertyBlock();
            properties.SetVector("_FadeOrigin", start);
            properties.SetVector("_FadeDirection", direction);
            properties.SetFloat("_FadeLength", width);
            properties.SetFloat("_FadeDistance", width * edgeFade);
            properties.SetFloat("_BlurWholeFlow", blurWholeFlow ? 1f : 0f);
            properties.SetFloat("_BlurSize", blurSize);
            renderer.SetPropertyBlock(properties);
        }

        private void RestorePositions()
        {
            if (layers == null || startPositions == null || layers.Length != startPositions.Length) return;
            for (int i = 0; i < layers.Length; i++)
                if (layers[i] != null) layers[i].localPosition = startPositions[i];
        }

        private void EnsureLayerSpeeds()
        {
            if (layers == null) return;
            if (layerSpeedMultipliers != null && layerSpeedMultipliers.Length == layers.Length) return;

            layerSpeedMultipliers = new float[layers.Length];
            float min = layerSpeedMultiplierRange.x;
            float max = Mathf.Max(min, layerSpeedMultiplierRange.y);

            for (int i = 0; i < layerSpeedMultipliers.Length; i++)
            {
                float multiplier = Random.Range(min, max);
                if (i > 0 && Mathf.Abs(multiplier - layerSpeedMultipliers[i - 1]) < .05f)
                    multiplier = Mathf.Clamp(multiplier + .12f, min, max);

                layerSpeedMultipliers[i] = multiplier;
            }
        }
    }
}
