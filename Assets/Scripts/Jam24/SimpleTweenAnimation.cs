using DG.Tweening;
using UnityEngine;

namespace Jam24
{
    public enum SimpleTweenAnimationType
    {
        Rotation,
        Shake,
        Idle
    }

    [DisallowMultipleComponent]
    public sealed class SimpleTweenAnimation : MonoBehaviour
    {
        [Header("General")]
        [SerializeField] private Transform target;
        [SerializeField] private SimpleTweenAnimationType animationType = SimpleTweenAnimationType.Idle;
        [SerializeField] private bool playOnEnable = true;
        [SerializeField] private bool useUnscaledTime;

        [Header("Rotation")]
        [SerializeField] private Vector3 rotationPerLoop = new(0f, 0f, 360f);
        [SerializeField, Min(.01f)] private float rotationDuration = 4f;

        [Header("Shake")]
        [SerializeField, Min(.01f)] private float shakeDuration = 1f;
        [SerializeField] private Vector3 shakeStrength = new(.08f, .08f, 2f);
        [SerializeField, Range(1, 50)] private int shakeVibrato = 8;
        [SerializeField, Range(0f, 180f)] private float shakeRandomness = 45f;

        [Header("Light Idle")]
        [SerializeField] private Vector3 idleMove = new(0f, .06f, 0f);
        [SerializeField] private Vector3 idleRotation = new(0f, 0f, 2f);
        [SerializeField] private Vector3 idleScale = new(.015f, .015f, 0f);
        [SerializeField, Min(.01f)] private float idleHalfCycleDuration = 1.4f;
        [SerializeField] private Ease idleEase = Ease.InOutSine;

        public bool IsPlaying => tween != null && tween.IsActive() && tween.IsPlaying();

        private Tween tween;
        private Vector3 startLocalPosition;
        private Quaternion startLocalRotation;
        private Vector3 startLocalScale;
        private bool hasInitialState;

        private void Awake()
        {
            ResolveTarget();
            CaptureInitialState();
        }

        private void OnEnable()
        {
            ResolveTarget();
            if (!hasInitialState) CaptureInitialState();
            if (playOnEnable && Application.isPlaying) Play();
        }

        public void Play()
        {
            ResolveTarget();
            if (target == null) return;
            if (!hasInitialState) CaptureInitialState();

            KillTween(false);
            RestoreInitialState();

            tween = animationType switch
            {
                SimpleTweenAnimationType.Rotation => CreateRotationTween(),
                SimpleTweenAnimationType.Shake => CreateShakeTween(),
                _ => CreateIdleTween()
            };

            tween.SetTarget(this).SetUpdate(useUnscaledTime);
        }

        public void Stop()
        {
            KillTween(false);
            RestoreInitialState();
        }

        public void Restart()
        {
            Stop();
            Play();
        }

        private Tween CreateRotationTween()
        {
            return target.DOLocalRotate(rotationPerLoop, rotationDuration, RotateMode.LocalAxisAdd)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental);
        }

        private Tween CreateShakeTween()
        {
            return target.DOShakePosition(shakeDuration, shakeStrength, shakeVibrato, shakeRandomness,
                    false, true, ShakeRandomnessMode.Full)
                .SetLoops(-1, LoopType.Restart);
        }

        private Tween CreateIdleTween()
        {
            Sequence sequence = DOTween.Sequence();
            sequence.Append(target.DOLocalMove(startLocalPosition + idleMove, idleHalfCycleDuration));
            sequence.Join(target.DOLocalRotateQuaternion(startLocalRotation * Quaternion.Euler(idleRotation),
                idleHalfCycleDuration));
            sequence.Join(target.DOScale(startLocalScale + idleScale, idleHalfCycleDuration));
            return sequence.SetEase(idleEase).SetLoops(-1, LoopType.Yoyo);
        }

        private void OnDisable()
        {
            KillTween(false);
            RestoreInitialState();
        }

        private void OnDestroy()
        {
            KillTween(false);
        }

        private void ResolveTarget()
        {
            if (target == null) target = transform;
        }

        private void CaptureInitialState()
        {
            if (target == null) return;
            startLocalPosition = target.localPosition;
            startLocalRotation = target.localRotation;
            startLocalScale = target.localScale;
            hasInitialState = true;
        }

        private void RestoreInitialState()
        {
            if (!hasInitialState || target == null) return;
            target.localPosition = startLocalPosition;
            target.localRotation = startLocalRotation;
            target.localScale = startLocalScale;
        }

        private void KillTween(bool complete)
        {
            if (tween == null) return;
            tween.Kill(complete);
            tween = null;
        }

        private void OnValidate()
        {
            rotationDuration = Mathf.Max(.01f, rotationDuration);
            shakeDuration = Mathf.Max(.01f, shakeDuration);
            shakeVibrato = Mathf.Clamp(shakeVibrato, 1, 50);
            shakeRandomness = Mathf.Clamp(shakeRandomness, 0f, 180f);
            idleHalfCycleDuration = Mathf.Max(.01f, idleHalfCycleDuration);
        }
    }
}
