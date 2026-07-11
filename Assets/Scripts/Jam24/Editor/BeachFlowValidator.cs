#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Jam24.Editor
{
    public static class BeachFlowValidator
    {
        [MenuItem("Jam24/Validate Complete Game _F5")]
        public static void Validate()
        {
            Require(FlowLevelCatalog.Count == 10, "Catalog must contain exactly 10 levels.");
            for (int i = 0; i < FlowLevelCatalog.Count; i++) ValidateLevel(i);

            Require(File.Exists("Assets/Resources/BeachFlow/beach_background.png"), "Beach background asset is missing.");
            Require(File.ReadAllText("Assets/Scenes/Init.unity").Contains("Jam24_Managers"), "Init manager root is missing.");
            Require(File.ReadAllText("Assets/Scenes/Home.unity").Contains("Jam24_HomeUI"), "Home UI root is missing.");
            string gameplay = File.ReadAllText("Assets/Scenes/Gameplay.unity");
            Require(gameplay.Contains("Jam24_Gameplay") && gameplay.Contains("FlowBoard") && gameplay.Contains("WinPopup"), "Gameplay hierarchy is incomplete.");

            Debug.Log("BEACH FLOW VALIDATION PASSED: 10/10 solvable levels, scenes, progression UI and summer beach asset are present.");
        }

        private static void ValidateLevel(int index)
        {
            LevelDefinition level = FlowLevelCatalog.Get(index);
            try
            {
                Require(level.width >= 3 && level.height >= 3, $"Level {index + 1}: invalid board size.");
                Require(level.pairs != null && level.pairs.Length >= 2, $"Level {index + 1}: needs at least two flows.");
                var endpoints = new HashSet<Vector2Int>();
                foreach (FlowPair pair in level.pairs)
                {
                    Require(Inside(pair.start, level) && Inside(pair.end, level), $"Level {index + 1}: endpoint outside board.");
                    Require(pair.start != pair.end, $"Level {index + 1}: identical endpoints.");
                    Require(endpoints.Add(pair.start) && endpoints.Add(pair.end), $"Level {index + 1}: overlapping endpoints.");
                }
                Require(HasDisjointSolution(level, endpoints, 0, new HashSet<Vector2Int>()), $"Level {index + 1}: no non-overlapping Flow solution found.");
            }
            finally { UnityEngine.Object.DestroyImmediate(level); }
        }

        private static bool HasDisjointSolution(LevelDefinition level, HashSet<Vector2Int> endpoints, int pairIndex, HashSet<Vector2Int> occupied)
        {
            if (pairIndex == level.pairs.Length) return true;
            FlowPair pair = level.pairs[pairIndex];
            var paths = new List<List<Vector2Int>>();
            EnumeratePaths(level, pair.start, pair.end, endpoints, occupied, new HashSet<Vector2Int> { pair.start }, new List<Vector2Int> { pair.start }, paths);
            foreach (List<Vector2Int> path in paths)
            {
                foreach (Vector2Int cell in path) occupied.Add(cell);
                if (HasDisjointSolution(level, endpoints, pairIndex + 1, occupied)) return true;
                foreach (Vector2Int cell in path) occupied.Remove(cell);
            }
            return false;
        }

        private static void EnumeratePaths(LevelDefinition level, Vector2Int current, Vector2Int target, HashSet<Vector2Int> endpoints,
            HashSet<Vector2Int> occupied, HashSet<Vector2Int> visited, List<Vector2Int> path, List<List<Vector2Int>> results)
        {
            if (results.Count >= 1200) return;
            if (current == target) { results.Add(new List<Vector2Int>(path)); return; }

            Vector2Int[] next = { current + Vector2Int.right, current + Vector2Int.left, current + Vector2Int.up, current + Vector2Int.down };
            Array.Sort(next, (a, b) => Manhattan(a, target).CompareTo(Manhattan(b, target)));
            foreach (Vector2Int cell in next)
            {
                if (!Inside(cell, level) || visited.Contains(cell) || occupied.Contains(cell)) continue;
                if (cell != target && endpoints.Contains(cell)) continue;
                visited.Add(cell);
                path.Add(cell);
                EnumeratePaths(level, cell, target, endpoints, occupied, visited, path, results);
                path.RemoveAt(path.Count - 1);
                visited.Remove(cell);
            }
        }

        private static int Manhattan(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        private static bool Inside(Vector2Int cell, LevelDefinition level) => cell.x >= 0 && cell.y >= 0 && cell.x < level.width && cell.y < level.height;

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new BuildFailedException("Beach Flow validation failed: " + message);
        }
    }
}
#endif
