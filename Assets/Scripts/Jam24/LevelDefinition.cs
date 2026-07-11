using System.Text;
using UnityEngine;

namespace Jam24
{
    public sealed class LevelDefinition : MonoBehaviour
    {
        [Header("Spawn points")]
        [SerializeField] private Transform playerSpawn;
        [SerializeField] private Transform flipSpawn;

        [Header("Goals")]
        [SerializeField] private Transform[] finishers;

        [Header("Rules")]
        [SerializeField, Min(1)] private int startingFlipCount = 3;

        [Header("Failure")]
        [SerializeField] private Transform[] deadZones;

        public Transform PlayerSpawn => playerSpawn;
        public Transform FlipSpawn => flipSpawn;
        public Transform[] Finishers => finishers;
        public int StartingFlipCount => Mathf.Max(1, startingFlipCount);
        public Transform[] DeadZones => deadZones;

        public bool TryValidate(out string error)
        {
            var problems = new StringBuilder();
            if (playerSpawn == null) problems.AppendLine("- Player Spawn is not assigned.");
            if (flipSpawn == null) problems.AppendLine("- Flip Spawn is not assigned.");

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
                    else if (deadZones[i].TryGetComponent(out DeadZone zone) && zone.Targets == 0)
                        problems.AppendLine($"- Dead Zone '{deadZones[i].name}' must target Player, Flip, or Both.");
                }
            }

            error = problems.ToString().TrimEnd();
            return error.Length == 0;
        }
    }
}
