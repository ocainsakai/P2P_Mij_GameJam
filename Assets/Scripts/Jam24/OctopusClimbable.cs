using UnityEngine;

namespace Jam24
{
    /// <summary>Marks a collider as a surface the octopus can crawl across.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class OctopusClimbable : MonoBehaviour
    {
    }
}
