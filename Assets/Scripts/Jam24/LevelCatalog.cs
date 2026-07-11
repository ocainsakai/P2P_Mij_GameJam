using System.Collections.Generic;
using UnityEngine;

namespace Jam24
{
    [CreateAssetMenu(fileName = "LevelCatalog", menuName = "Jam24/Level Catalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        [SerializeField] private GameObject[] levelPrefabs;

        public int Count => levelPrefabs?.Length ?? 0;
        public IReadOnlyList<GameObject> Levels => levelPrefabs;

        public bool TryGetLevel(int index, out GameObject levelPrefab)
        {
            levelPrefab = index >= 0 && index < Count ? levelPrefabs[index] : null;
            return levelPrefab != null;
        }

        private void OnValidate()
        {
            if (levelPrefabs == null) return;
            for (int i = 0; i < levelPrefabs.Length; i++)
            {
                GameObject prefab = levelPrefabs[i];
                if (prefab != null && prefab.GetComponent<LevelDefinition>() == null)
                    Debug.LogWarning($"Level Catalog element {i} ('{prefab.name}') needs a LevelDefinition.", this);
            }
        }
    }
}
