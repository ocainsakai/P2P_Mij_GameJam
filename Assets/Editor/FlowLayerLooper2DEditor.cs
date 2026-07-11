using UnityEditor;
using UnityEngine;

namespace Jam24.Editor
{
    [CustomEditor(typeof(FlowLayerLooper2D))]
    internal sealed class FlowLayerLooper2DEditor : UnityEditor.Editor
    {
        private Vector3 lastScale;
        private Vector2 lastColliderSize;
        private Vector2 lastColliderOffset;
        private bool fitScheduled;

        private FlowLayerLooper2D Looper => (FlowLayerLooper2D)target;

        private void OnEnable()
        {
            CacheState();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(6f);
            if (GUILayout.Button("Fit Tiled Layers To Flow Zone")) ScheduleFit();
            CheckForResize();
        }

        private void OnSceneGUI()
        {
            CheckForResize();
        }

        private void CheckForResize()
        {
            if (Looper == null) return;
            BoxCollider2D collider = Looper.GetComponent<BoxCollider2D>();
            if (collider == null) return;

            if (Looper.transform.lossyScale != lastScale ||
                collider.size != lastColliderSize || collider.offset != lastColliderOffset)
                ScheduleFit();
        }

        private void ScheduleFit()
        {
            if (fitScheduled) return;
            fitScheduled = true;
            EditorApplication.delayCall += ApplyFit;
        }

        private void ApplyFit()
        {
            fitScheduled = false;
            if (Looper == null) return;

            SpriteRenderer[] renderers = Looper.GetComponentsInChildren<SpriteRenderer>(true);
            Object[] undoObjects = new Object[renderers.Length * 2 + 1];
            undoObjects[0] = Looper;
            for (int i = 0; i < renderers.Length; i++)
            {
                undoObjects[i * 2 + 1] = renderers[i];
                undoObjects[i * 2 + 2] = renderers[i].transform;
            }
            Undo.RecordObjects(undoObjects, "Fit Flow Tiled Layers");

            Looper.FitLayersToZone();
            EditorUtility.SetDirty(Looper);
            foreach (SpriteRenderer renderer in renderers)
            {
                EditorUtility.SetDirty(renderer);
                EditorUtility.SetDirty(renderer.transform);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer.transform);
            }
            CacheState();
            SceneView.RepaintAll();
        }

        private void CacheState()
        {
            if (Looper == null) return;
            lastScale = Looper.transform.lossyScale;
            BoxCollider2D collider = Looper.GetComponent<BoxCollider2D>();
            if (collider == null) return;
            lastColliderSize = collider.size;
            lastColliderOffset = collider.offset;
        }
    }
}
