using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Video;

namespace Jam24
{
    public sealed class CutsceneManager : MonoBehaviour
    {
        public bool IsPlaying { get; private set; }

        private CutsceneSequence activeSequence;
        private bool skipRequested;

        private void Update()
        {
            if (!IsPlaying || activeSequence == null || !activeSequence.AllowSkip) return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null &&
                (keyboard.escapeKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
                skipRequested = true;

            Gamepad gamepad = Gamepad.current;
            if (gamepad != null && gamepad.buttonEast.wasPressedThisFrame)
                skipRequested = true;
        }

        public IEnumerator Play(CutsceneSequence sequence)
        {
            if (sequence == null || IsPlaying) yield break;
            if (!sequence.TryValidate(out string validationError))
            {
                Debug.LogError($"Invalid cutscene '{sequence.name}': {validationError}", sequence);
                yield break;
            }

            IsPlaying = true;
            activeSequence = sequence;
            skipRequested = false;

            float previousTimeScale = Time.timeScale;
            if (sequence.PauseGameplay) Time.timeScale = 0f;
            if (sequence.PresentationRoot != null) sequence.PresentationRoot.SetActive(true);
            sequence.InvokeStarted();

            yield return sequence.PlaybackType switch
            {
                CutscenePlaybackType.Timeline => PlayTimeline(sequence),
                CutscenePlaybackType.Video => PlayVideo(sequence),
                CutscenePlaybackType.Animator => PlayAnimator(sequence),
                _ => WaitForDuration(sequence.Duration)
            };

            StopPlayback(sequence);
            if (sequence.HideOnComplete && sequence.PresentationRoot != null)
                sequence.PresentationRoot.SetActive(false);
            if (sequence.PauseGameplay) Time.timeScale = previousTimeScale;

            sequence.InvokeCompleted();
            activeSequence = null;
            skipRequested = false;
            IsPlaying = false;
        }

        public void Skip()
        {
            if (IsPlaying && activeSequence != null && activeSequence.AllowSkip)
                skipRequested = true;
        }

        private IEnumerator PlayTimeline(CutsceneSequence sequence)
        {
            PlayableDirector director = sequence.Timeline;
            director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
            director.time = 0d;
            director.Play();
            yield return null;
            while (!skipRequested && director != null && director.state == PlayState.Playing)
                yield return null;
        }

        private IEnumerator PlayVideo(CutsceneSequence sequence)
        {
            if (!sequence.TryConfigureVideoUrl(out string error))
            {
                Debug.LogError($"Cannot play video cutscene '{sequence.name}': {error}", sequence);
                yield break;
            }

            VideoPlayer player = sequence.VideoPlayer;
            player.Prepare();
            while (!skipRequested && player != null && !player.isPrepared)
                yield return null;
            if (skipRequested || player == null) yield break;

            player.Play();
            yield return null;
            while (!skipRequested && player != null && player.isPlaying)
                yield return null;
        }

        private IEnumerator PlayAnimator(CutsceneSequence sequence)
        {
            Animator animator = sequence.Animator;
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.ResetTrigger(sequence.AnimatorTrigger);
            animator.SetTrigger(sequence.AnimatorTrigger);
            yield return WaitForDuration(sequence.Duration);
        }

        private IEnumerator WaitForDuration(float duration)
        {
            float elapsed = 0f;
            while (!skipRequested && elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private static void StopPlayback(CutsceneSequence sequence)
        {
            if (sequence.Timeline != null) sequence.Timeline.Stop();
            if (sequence.VideoPlayer != null) sequence.VideoPlayer.Stop();
        }
    }
}
