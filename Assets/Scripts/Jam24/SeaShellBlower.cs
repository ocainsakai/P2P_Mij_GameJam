using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace Jam24
{
    /// <summary>
    /// Touch-activated shell. It opens through an Animator, enables its Flow
    /// effector for a limited time, then closes automatically.
    /// The blow direction is configured by the AreaEffector2D force angle.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D), typeof(Animator))]
    public sealed class SeaShellBlower : MonoBehaviour
    {
        private static readonly int IsOpenParameter = Animator.StringToHash("IsOpen");

        [Header("References")]
        [SerializeField] private Animator shellAnimator;
        [SerializeField] private SpriteRenderer shellRenderer;
        [SerializeField] private GameObject flowObject;
        [SerializeField] private AreaEffector2D waterEffector;
        [SerializeField] private ParticleSystem bubbleParticles;

        [Header("Activation")]
        [SerializeField, Min(.1f)] private float openDuration = 3f;
        [SerializeField, Min(0f)] private float flowStartDelay = .2f;
        [SerializeField, Min(0f)] private float flowDuration;
        [SerializeField] private bool autoCycle;
        [SerializeField, Min(0f)] private float closedDuration = 2f;

        [Header("Water Current")]
        [SerializeField, Min(0f)] private float flowForce = 12f;

        [Header("Optional Open Look")]
        [SerializeField] private Sprite openSprite;

        public bool IsOpen { get; private set; }

        private Sprite closedSprite;
        private Coroutine activeRoutine;

        private void Awake()
        {
            if (shellAnimator == null) shellAnimator = GetComponent<Animator>();
            if (shellRenderer == null) shellRenderer = GetComponentInChildren<SpriteRenderer>(true);
            if (waterEffector == null) waterEffector = GetComponentInChildren<AreaEffector2D>(true);
            if (flowObject == null && waterEffector != null) flowObject = waterEffector.gameObject;

            if (shellRenderer != null) closedSprite = shellRenderer.sprite;
            if (waterEffector != null)
            {
                waterEffector.useGlobalAngle = false;
                waterEffector.forceMagnitude = flowForce;
                waterEffector.forceVariation = 0f;
            }

            ApplyClosedState();
        }

        private void OnEnable()
        {
            if (autoCycle) activeRoutine = StartCoroutine(AutoCycle());
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryActivate(collision.collider);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryActivate(other);
        }

        private void TryActivate(Collider2D other)
        {
            if (other.GetComponentInParent<OctopusPlayerMovement>() == null) return;
            Activate();
        }

        public void Activate()
        {
            if (!isActiveAndEnabled) return;
            if (autoCycle) return;

            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
                activeRoutine = null;
            }

            activeRoutine = StartCoroutine(OpenForDuration());
        }

        private IEnumerator OpenForDuration()
        {
            yield return RunOpenCycle();
            activeRoutine = null;
        }

        private IEnumerator AutoCycle()
        {
            while (true)
            {
                ApplyClosedState();
                if (closedDuration > 0f) yield return new WaitForSeconds(closedDuration);
                yield return RunOpenCycle();
            }
        }

        private IEnumerator RunOpenCycle()
        {
            IsOpen = true;
            JamAudioManager.Play(GameSfxType.ShellOpen);
            if (shellAnimator != null) shellAnimator.SetBool(IsOpenParameter, true);
            if (openSprite != null && shellRenderer != null) shellRenderer.sprite = openSprite;

            float delay = Mathf.Min(flowStartDelay, openDuration);
            if (delay > 0f) yield return new WaitForSeconds(delay);
            SetFlowActive(true);

            float activeFlowDuration = flowDuration > 0f ? flowDuration : openDuration - delay;
            if (activeFlowDuration > 0f) yield return new WaitForSeconds(activeFlowDuration);

            SetFlowActive(false);
            IsOpen = false;
            JamAudioManager.Play(GameSfxType.ShellClose);
            if (shellAnimator != null) shellAnimator.SetBool(IsOpenParameter, false);
            if (shellRenderer != null && closedSprite != null) shellRenderer.sprite = closedSprite;
        }

        private void OnDisable()
        {
            if (activeRoutine != null) StopCoroutine(activeRoutine);
            activeRoutine = null;
            ApplyClosedState();
        }

        private void ApplyClosedState()
        {
            IsOpen = false;
            SetFlowActive(false);
            if (shellAnimator != null) shellAnimator.SetBool(IsOpenParameter, false);
            if (shellRenderer != null && closedSprite != null) shellRenderer.sprite = closedSprite;
        }

        private void SetFlowActive(bool value)
        {
            if (flowObject != null) flowObject.SetActive(value);
            else if (waterEffector != null) waterEffector.enabled = value;

            if (bubbleParticles == null) return;
            if (value) bubbleParticles.Play(true);
            else bubbleParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void OnValidate()
        {
            openDuration = Mathf.Max(.1f, openDuration);
            flowStartDelay = Mathf.Clamp(flowStartDelay, 0f, openDuration);
            flowDuration = Mathf.Max(0f, flowDuration);
            closedDuration = Mathf.Max(0f, closedDuration);
            if (waterEffector != null) waterEffector.forceMagnitude = flowForce;
        }
    }
}
