using System.Collections.Generic;
using UnityEngine;

namespace Jam24
{
    /// <summary>
    /// Prevents buoyancy from affecting the Flip while it is inside an active
    /// AreaEffector2D. Other objects using the same buoyancy keep their behavior.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class FlipAreaBuoyancyOverride2D : MonoBehaviour
    {
        private readonly HashSet<Collider2D> overlappingAreas = new();
        private readonly Dictionary<BuoyancyEffector2D, EffectorSettings> buoyancySettings = new();

        private struct EffectorSettings
        {
            public bool UseColliderMask;
            public int ColliderMask;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            RegisterArea(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            RegisterArea(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!overlappingAreas.Remove(other) || overlappingAreas.Count > 0) return;
            RestoreBuoyancy();
        }

        private void RegisterArea(Collider2D other)
        {
            AreaEffector2D area = other.GetComponent<AreaEffector2D>();
            if (area == null || !area.enabled || !other.usedByEffector) return;
            if (!overlappingAreas.Add(other) || overlappingAreas.Count > 1) return;
            SuppressBuoyancy();
        }

        private void SuppressBuoyancy()
        {
            int flipLayerMask = 1 << gameObject.layer;
            BuoyancyEffector2D[] effectors =
                FindObjectsByType<BuoyancyEffector2D>(FindObjectsSortMode.None);

            foreach (BuoyancyEffector2D buoyancy in effectors)
            {
                if (buoyancy == null || buoyancySettings.ContainsKey(buoyancy)) continue;

                buoyancySettings.Add(buoyancy, new EffectorSettings
                {
                    UseColliderMask = buoyancy.useColliderMask,
                    ColliderMask = buoyancy.colliderMask
                });

                buoyancy.useColliderMask = true;
                buoyancy.colliderMask &= ~flipLayerMask;
            }
        }

        private void RestoreBuoyancy()
        {
            foreach (KeyValuePair<BuoyancyEffector2D, EffectorSettings> pair in buoyancySettings)
            {
                if (pair.Key == null) continue;
                pair.Key.useColliderMask = pair.Value.UseColliderMask;
                pair.Key.colliderMask = pair.Value.ColliderMask;
            }

            buoyancySettings.Clear();
        }

        private void OnDisable()
        {
            overlappingAreas.Clear();
            RestoreBuoyancy();
        }
    }
}
