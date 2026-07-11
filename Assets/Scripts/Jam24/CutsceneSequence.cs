using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Jam24
{
    public enum CutscenePlaybackType
    {
        Timeline,
        Video,
        Animator,
        DurationOnly
    }

    public sealed class CutsceneSequence : MonoBehaviour
    {
        [Header("Playback")]
        [SerializeField] private CutscenePlaybackType playbackType = CutscenePlaybackType.Timeline;
        [SerializeField] private PlayableDirector timeline;
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private string videoUrl;
        [SerializeField] private bool videoUrlIsStreamingAssetsPath = true;
        [SerializeField] private VideoAspectRatio videoAspectRatio = VideoAspectRatio.FitOutside;
        [SerializeField] private Animator animator;
        [SerializeField] private string animatorTrigger = "Play";
        [SerializeField, Min(0f)] private float duration = 1f;

        [Header("Presentation")]
        [SerializeField] private GameObject presentationRoot;
        [SerializeField] private bool hideOnComplete = true;

        [Header("Behaviour")]
        [SerializeField] private bool pauseGameplay = true;
        [SerializeField] private bool allowSkip = true;

        [Header("Events")]
        [SerializeField] private UnityEvent started;
        [SerializeField] private UnityEvent completed;

        public CutscenePlaybackType PlaybackType => playbackType;
        public PlayableDirector Timeline => timeline;
        public VideoPlayer VideoPlayer => videoPlayer;
        public Animator Animator => animator;
        public string AnimatorTrigger => animatorTrigger;
        public float Duration => duration;
        public GameObject PresentationRoot => presentationRoot;
        public bool HideOnComplete => hideOnComplete;
        public bool PauseGameplay => pauseGameplay;
        public bool AllowSkip => allowSkip;

        private RenderTexture runtimeVideoTexture;

        public void InvokeStarted() => started?.Invoke();
        public void InvokeCompleted() => completed?.Invoke();

        public bool TryConfigureVideoUrl(out string error)
        {
            error = string.Empty;
            if (playbackType != CutscenePlaybackType.Video) return true;
            if (videoPlayer == null)
            {
                videoPlayer = GetComponent<VideoPlayer>();
                if (videoPlayer == null) videoPlayer = gameObject.AddComponent<VideoPlayer>();
            }

            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.waitForFirstFrame = true;
            EnsureFullscreenVideoOverlay();
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = runtimeVideoTexture;
            videoPlayer.aspectRatio = videoAspectRatio;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

            string configuredUrl = videoUrl?.Trim() ?? string.Empty;
            if (configuredUrl.Length > 0)
            {
                videoPlayer.source = VideoSource.Url;
                videoPlayer.url = videoUrlIsStreamingAssetsPath
                    ? $"{Application.streamingAssetsPath.TrimEnd('/', '\\')}/{configuredUrl.TrimStart('/', '\\')}"
                    : configuredUrl;
            }

            if (videoPlayer.source != VideoSource.Url || string.IsNullOrWhiteSpace(videoPlayer.url))
            {
                error = "Video cutscenes require a URL. For WebGL, put the file in StreamingAssets and set Video URL.";
                return false;
            }

            return true;
        }

        private void EnsureFullscreenVideoOverlay()
        {
            if (presentationRoot != null && runtimeVideoTexture != null) return;

            int width = Mathf.Max(1, Screen.width);
            int height = Mathf.Max(1, Screen.height);
            runtimeVideoTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                name = $"{name}_VideoTexture"
            };
            runtimeVideoTexture.Create();

            var overlay = new GameObject("FullscreenVideoOverlay",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(RawImage));
            overlay.transform.SetParent(transform, false);

            Canvas canvas = overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = short.MaxValue;

            CanvasScaler scaler = overlay.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            CanvasGroup group = overlay.GetComponent<CanvasGroup>();
            group.interactable = true;
            group.blocksRaycasts = true;

            RawImage image = overlay.GetComponent<RawImage>();
            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            image.texture = runtimeVideoTexture;
            image.raycastTarget = true;
            image.color = Color.white;

            presentationRoot = overlay;
            presentationRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (runtimeVideoTexture == null) return;
            runtimeVideoTexture.Release();
            Destroy(runtimeVideoTexture);
            runtimeVideoTexture = null;
        }

        public bool TryValidate(out string error)
        {
            error = playbackType switch
            {
                CutscenePlaybackType.Timeline when timeline == null => "Timeline mode needs a PlayableDirector.",
                CutscenePlaybackType.Video when !TryConfigureVideoUrl(out string videoError) => videoError,
                CutscenePlaybackType.Animator when animator == null => "Animator mode needs an Animator.",
                CutscenePlaybackType.Animator when string.IsNullOrWhiteSpace(animatorTrigger) => "Animator mode needs a trigger name.",
                CutscenePlaybackType.Animator when duration <= 0f => "Animator mode needs Duration greater than zero.",
                CutscenePlaybackType.DurationOnly when duration <= 0f => "Duration Only mode needs Duration greater than zero.",
                _ => string.Empty
            };
            return error.Length == 0;
        }
    }
}
