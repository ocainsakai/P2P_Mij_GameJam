using System;
using UnityEngine;

namespace Jam24
{
    [CreateAssetMenu(menuName = "Jam24/Flow Level", fileName = "FlowLevel_01")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [Min(3)] public int width = 5;
        [Min(3)] public int height = 5;
        public FlowPair[] pairs;
        [Tooltip("Classic Flow rule: every cell must be covered before winning.")]
        public bool requireFullBoard;
    }

    [Serializable]
    public struct FlowPair
    {
        public string name;
        public Color color;
        public Vector2Int start;
        public Vector2Int end;

        public FlowPair(string name, string hex, Vector2Int start, Vector2Int end)
        {
            this.name = name;
            ColorUtility.TryParseHtmlString(hex, out color);
            this.start = start;
            this.end = end;
        }
    }
}
