using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Jam24
{
    public sealed class LevelDefinition : MonoBehaviour
    {
        [Header("Spawn points")]
        [SerializeField] private Transform playerSpawn;
        [FormerlySerializedAs("flipSpawn")]
        [SerializeField, HideInInspector] private Transform legacyFlipSpawn;
        [SerializeField] private Transform[] flipSpawns;

        [Header("Goals")]
        [SerializeField] private Transform[] finishers;

        [Header("Rules")]
        [SerializeField, Min(1)] public int startingFlipCount = 3;
        [SerializeField] private bool loseWhenFlipTrappedBySeaweed;

        [Header("Camera")]
        [SerializeField, Min(0.1f)] private float cameraSize = 5f;

        [Header("Runtime Buoyancy")]
        [SerializeField] private bool overrideBuoyancy;
        [SerializeField] private BuoyancyEffector2D[] buoyancyEffectors;
        [SerializeField] private float buoyancySurfaceLevel;
        [SerializeField, Min(0f)] private float buoyancyDensity = 1.2f;
        [SerializeField, Min(0f)] private float buoyancyLinearDamping = 2f;
        [SerializeField, Min(0f)] private float buoyancyAngularDamping = 1.2f;
        [SerializeField] private float buoyancyFlowAngle;
        [SerializeField] private float buoyancyFlowMagnitude;
        [SerializeField, Min(0f)] private float buoyancyFlowVariation;

        [Header("Failure")]
        [SerializeField] private Transform[] deadZones;

        public Transform PlayerSpawn => playerSpawn;
        public IReadOnlyList<Transform> FlipSpawns =>
            flipSpawns != null && flipSpawns.Length > 0
                ? flipSpawns
                : legacyFlipSpawn != null ? new[] { legacyFlipSpawn } : System.Array.Empty<Transform>();
        public Transform[] Finishers => finishers;
        public int StartingFlipCount => Mathf.Max(1, startingFlipCount);
        public bool LoseWhenFlipTrappedBySeaweed => loseWhenFlipTrappedBySeaweed;
        public float CameraSize => Mathf.Max(0.1f, cameraSize);
        public Transform[] DeadZones => deadZones;
        public IReadOnlyList<BuoyancyEffector2D> BuoyancyEffectors => buoyancyEffectors;

        public Transform GetFlipSpawn(int index)
        {
            IReadOnlyList<Transform> spawns = FlipSpawns;
            if (spawns.Count == 0) return null;
            return spawns[Mathf.Abs(index) % spawns.Count];
        }

        private void Awake()
        {
            BindBuoyancyEffectors();
            ApplyBuoyancySettings();
        }

        [ContextMenu("Bind Buoyancy Effectors")]
        public void BindBuoyancyEffectors()
        {
            buoyancyEffectors = GetComponentsInChildren<BuoyancyEffector2D>(true);
        }

        public void ApplyBuoyancySettings()
        {
            if (!overrideBuoyancy) return;
            if (buoyancyEffectors == null || buoyancyEffectors.Length == 0)
                BindBuoyancyEffectors();

            foreach (BuoyancyEffector2D buoyancy in buoyancyEffectors)
            {
                if (buoyancy == null) continue;
                buoyancy.surfaceLevel = buoyancySurfaceLevel;
                buoyancy.density = buoyancyDensity;
                buoyancy.linearDamping = buoyancyLinearDamping;
                buoyancy.angularDamping = buoyancyAngularDamping;
                buoyancy.flowAngle = buoyancyFlowAngle;
                buoyancy.flowMagnitude = buoyancyFlowMagnitude;
                buoyancy.flowVariation = buoyancyFlowVariation;
            }
        }

        public void SetBuoyancy(float surfaceLevel, float density, float linearDamping, float angularDamping,
            float flowAngle, float flowMagnitude, float flowVariation)
        {
            overrideBuoyancy = true;
            buoyancySurfaceLevel = surfaceLevel;
            buoyancyDensity = Mathf.Max(0f, density);
            buoyancyLinearDamping = Mathf.Max(0f, linearDamping);
            buoyancyAngularDamping = Mathf.Max(0f, angularDamping);
            buoyancyFlowAngle = flowAngle;
            buoyancyFlowMagnitude = flowMagnitude;
            buoyancyFlowVariation = Mathf.Max(0f, flowVariation);
            ApplyBuoyancySettings();
        }

        public bool TryValidate(out string error)
        {
            var problems = new StringBuilder();
            if (playerSpawn == null) problems.AppendLine("- Player Spawn is not assigned.");
            if (FlipSpawns.Count == 0) problems.AppendLine("- At least one Flip Spawn is required.");
            else
                for (int i = 0; i < FlipSpawns.Count; i++)
                    if (FlipSpawns[i] == null) problems.AppendLine($"- Flip Spawn element {i} is not assigned.");

            if (finishers == null || finishers.Length == 0)
            {
                problems.AppendLine("- At least one Finisher is required.");
            }
            else
            {
                for (int i = 0; i < finishers.Length; i++)
                {
                    if (finishers[i] == null)
                        problems.AppendLine($"- Finisher element {i} is not assigned.");
                    else if (finishers[i].GetComponent<Collider2D>() == null)
                        problems.AppendLine($"- Finisher '{finishers[i].name}' needs a Collider2D.");
                }
            }

            if (deadZones != null)
            {
                for (int i = 0; i < deadZones.Length; i++)
                {
                    if (deadZones[i] == null)
                        problems.AppendLine($"- Dead Zone element {i} is not assigned.");
                    else if (deadZones[i].GetComponent<Collider2D>() == null)
                        problems.AppendLine($"- Dead Zone '{deadZones[i].name}' needs a Collider2D.");
                    else if (deadZones[i].GetComponent<PlayerDeadZone2D>() == null &&
                             deadZones[i].GetComponent<FlipDeadZone2D>() == null)
                        problems.AppendLine($"- Dead Zone '{deadZones[i].name}' needs PlayerDeadZone2D or FlipDeadZone2D.");
                }
            }

            error = problems.ToString().TrimEnd();
            return error.Length == 0;
        }

        private void OnValidate()
        {
            startingFlipCount = Mathf.Max(1, startingFlipCount);
            cameraSize = Mathf.Max(0.1f, cameraSize);
            buoyancyDensity = Mathf.Max(0f, buoyancyDensity);
            buoyancyLinearDamping = Mathf.Max(0f, buoyancyLinearDamping);
            buoyancyAngularDamping = Mathf.Max(0f, buoyancyAngularDamping);
            buoyancyFlowVariation = Mathf.Max(0f, buoyancyFlowVariation);
            BindBuoyancyEffectors();
        }
    }
}
