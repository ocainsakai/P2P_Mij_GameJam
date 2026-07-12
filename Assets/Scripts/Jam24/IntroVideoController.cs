using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

namespace Jam24
{
    /// <summary>Plays the startup movie once, then continues into the normal loading flow.</summary>
    [DisallowMultipleComponent]
    public sealed class IntroVideoController : MonoBehaviour
    {
        private const string IntroSceneName = "IntroVideo";

        [SerializeField] private string videoRelativePath = "Videos/intro_cutscene.mp4";
        [SerializeField] private string nextScene = GameFlow.LoadingScene;
        [SerializeField, Min(1f)] private float prepareTimeout = 15f;

        private VideoPlayer videoPlayer;
        private bool isFinishing;
        private static bool introWasRequested;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSessionState()
        {
            introWasRequested = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureIntroPlaysFirst()
        {
            if (introWasRequested) return;
            introWasRequested = true;

            if (SceneManager.GetActiveScene().name != IntroSceneName)
                SceneManager.LoadScene(IntroSceneName);
        }

        private void Awake()
        {
            Time.timeScale = 1f;

            Camera targetCamera = Camera.main;
            if (targetCamera == null)
            {
                GameObject cameraObject = new("Intro Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                targetCamera = cameraObject.GetComponent<Camera>();
            }

            targetCamera.clearFlags = CameraClearFlags.SolidColor;
            targetCamera.backgroundColor = Color.black;

            videoPlayer = gameObject.AddComponent<VideoPlayer>();
            videoPlayer.playOnAwake = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.skipOnDrop = true;
            videoPlayer.isLooping = false;
            videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
            videoPlayer.targetCamera = targetCamera;
            videoPlayer.targetCameraAlpha = 1f;
            videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
            videoPlayer.url = Path.Combine(Application.streamingAssetsPath, videoRelativePath).Replace('\\', '/');

            videoPlayer.prepareCompleted += HandlePrepared;
            videoPlayer.loopPointReached += HandleFinished;
            videoPlayer.errorReceived += HandleError;
            videoPlayer.Prepare();
            StartCoroutine(WaitForPrepare());
        }

        private IEnumerator WaitForPrepare()
        {
            float startedAt = Time.realtimeSinceStartup;
            while (!isFinishing && videoPlayer != null && !videoPlayer.isPrepared)
            {
                if (Time.realtimeSinceStartup - startedAt >= prepareTimeout)
                {
                    Debug.LogError($"Intro video preparation timed out: {videoPlayer.url}", this);
                    ContinueToGame();
                    yield break;
                }

                yield return null;
            }
        }

        private void HandlePrepared(VideoPlayer player)
        {
            if (!isFinishing) player.Play();
        }

        private void HandleFinished(VideoPlayer player)
        {
            ContinueToGame();
        }

        private void HandleError(VideoPlayer player, string message)
        {
            Debug.LogError($"Could not play intro video: {message}", this);
            ContinueToGame();
        }

        private void ContinueToGame()
        {
            if (isFinishing) return;
            isFinishing = true;
            SceneManager.LoadScene(nextScene);
        }

        private void OnDestroy()
        {
            if (videoPlayer == null) return;
            videoPlayer.prepareCompleted -= HandlePrepared;
            videoPlayer.loopPointReached -= HandleFinished;
            videoPlayer.errorReceived -= HandleError;
        }
    }
}
