using System.Linq;
using UnityEngine;

namespace Jam24
{
    [DisallowMultipleComponent]
    public sealed class FlowLayerLooper2D : MonoBehaviour
    {
        [SerializeField] private Transform[] layers;
        [SerializeField] private BoxCollider2D flowZone;
        [SerializeField] private float speed = .6f;
        [SerializeField, Min(.01f)] private float loopWidth = 1f;

        private Vector3[] startPositions;

        private void Awake()
        {
            ResolveReferences();
            CachePositions();
        }

        private void OnEnable()
        {
            ResolveReferences();
            CachePositions();
        }

        private void Update()
        {
            if (layers == null || startPositions == null || layers.Length != startPositions.Length) return;
            float width = Mathf.Max(.01f, flowZone != null ? flowZone.size.x : loopWidth);
            float offset = Mathf.Repeat(Time.time * speed, width);

            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i] == null) continue;
                Vector3 position = startPositions[i];
                position.x += offset;
                if (position.x > startPositions[i].x + width * .5f) position.x -= width;
                layers[i].localPosition = position;
            }
        }

        private void OnDisable()
        {
            RestorePositions();
        }

        private void OnValidate()
        {
            loopWidth = Mathf.Max(.01f, loopWidth);
            ResolveReferences();
        }

        public void Configure(BoxCollider2D collider, Transform[] movingLayers)
        {
            flowZone = collider;
            layers = movingLayers;
            if (flowZone != null) loopWidth = flowZone.size.x;
            CachePositions();
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

        private void CachePositions()
        {
            if (layers == null) return;
            startPositions = layers.Select(layer => layer == null ? Vector3.zero : layer.localPosition).ToArray();
        }

        private void RestorePositions()
        {
            if (layers == null || startPositions == null || layers.Length != startPositions.Length) return;
            for (int i = 0; i < layers.Length; i++)
                if (layers[i] != null) layers[i].localPosition = startPositions[i];
        }
    }
}
