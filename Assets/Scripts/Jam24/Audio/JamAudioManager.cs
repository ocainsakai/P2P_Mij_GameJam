using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Jam24
{
    public sealed class JamAudioManager : MonoBehaviour
    {
        private const string SoundEnabledKey = "jam24.audio.soundEnabled";
        private const string UiVolumeKey = "jam24.audio.uiVolume";
        private const string GameVolumeKey = "jam24.audio.gameVolume";
        private const string MusicEnabledKey = "jam24.audio.musicEnabled";
        private const string MusicVolumeKey = "jam24.audio.musicVolume";

        public static JamAudioManager Instance { get; private set; }

        private JamSfxConfig config;
        private AudioSource uiSource;
        private AudioSource gameSource;
        private AudioSource ambienceSource;
        private AudioSource musicSource;
        private Coroutine musicFadeRoutine;
        private float currentMusicScale = 1f;
        private float currentAmbienceScale = 1f;
        private AudioClip lastUiClip;
        private float lastUiPlayTime = float.NegativeInfinity;

        public bool SoundEnabled { get; private set; }
        public float UiVolume { get; private set; }
        public float GameVolume { get; private set; }
        public bool MusicEnabled { get; private set; }
        public float MusicVolume { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var root = new GameObject("[Jam24 Audio]");
            root.AddComponent<JamAudioManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            config = Resources.Load<JamSfxConfig>("JamSfxConfig");
            uiSource = CreateSource("UI SFX");
            gameSource = CreateSource("Gameplay SFX");
            ambienceSource = CreateSource("Ambience");
            ambienceSource.loop = true;
            musicSource = CreateSource("Music");
            musicSource.loop = true;
            SoundEnabled = PlayerPrefs.GetInt(SoundEnabledKey, 1) != 0;
            UiVolume = PlayerPrefs.GetFloat(UiVolumeKey, 1f);
            GameVolume = PlayerPrefs.GetFloat(GameVolumeKey, 1f);
            MusicEnabled = PlayerPrefs.GetInt(MusicEnabledKey, 1) != 0;
            MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, .7f);
            SceneManager.sceneLoaded += HandleSceneLoaded;
            GameFlow.StateChanged += HandleGameStateChanged;
        }

        private AudioSource CreateSource(string sourceName)
        {
            var child = new GameObject(sourceName);
            child.transform.SetParent(transform, false);
            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            return source;
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            GameFlow.StateChanged -= HandleGameStateChanged;
            Instance = null;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Button button in root.GetComponentsInChildren<Button>(true))
                    if (button.GetComponent<UIButtonSfx>() == null)
                        button.gameObject.AddComponent<UIButtonSfx>();

                foreach (Toggle toggle in root.GetComponentsInChildren<Toggle>(true))
                    if (toggle.GetComponent<UIToggleSfx>() == null)
                        toggle.gameObject.AddComponent<UIToggleSfx>();
            }

            if (scene.name == GameFlow.LoadingScene) return;
            if (scene.name == GameFlow.HomeScene)
            {
                PlayMusic(MusicType.Menu);
                StopAmbience();
            }
            else if (scene.name == GameFlow.GameplayScene)
            {
                PlayMusic(MusicType.Gameplay);
                PlayAmbience(AmbienceType.Underwater);
            }
            else
            {
                StopMusic();
                StopAmbience();
            }
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.Win) Play(GameSfxType.Win);
            else if (state == GameState.Lose) Play(GameSfxType.Lose);
        }

        public static void Play(UiSfxType type)
        {
            if (Instance == null || Instance.config == null ||
                !Instance.config.TryGet(
                    type, out AudioClip clip, out float scale, out float startOffset)) return;
            if (Instance.lastUiClip == clip &&
                Time.unscaledTime - Instance.lastUiPlayTime < .08f) return;
            Instance.lastUiClip = clip;
            Instance.lastUiPlayTime = Time.unscaledTime;
            Instance.PlayUiClip(clip, Instance.UiVolume * scale, startOffset);
        }

        private void PlayUiClip(AudioClip clip, float volume, float startOffset)
        {
            if (!SoundEnabled || uiSource == null || clip == null || volume <= 0f) return;
            uiSource.Stop();
            uiSource.clip = clip;
            uiSource.volume = Mathf.Clamp01(volume);
            uiSource.time = clip.length > startOffset + .02f ? startOffset : 0f;
            uiSource.Play();
        }

        public static void Play(GameSfxType type)
        {
            if (Instance == null || Instance.config == null ||
                !Instance.config.TryGet(type, out AudioClip clip, out float scale)) return;
            Instance.PlayClip(Instance.gameSource, clip, Instance.GameVolume * scale);
        }

        public static void PlayClip(AudioClip clip, float volumeScale = 1f)
        {
            Instance?.PlayClip(Instance.gameSource, clip, Instance.GameVolume * volumeScale);
        }

        public static void PlayMusic(MusicType type)
        {
            if (Instance == null || Instance.config == null ||
                !Instance.config.TryGet(type, out AudioClip clip, out float scale)) return;
            Instance.BeginMusicFade(clip, scale);
        }

        public static void PlayAmbience(AmbienceType type)
        {
            if (Instance == null || Instance.config == null ||
                !Instance.config.TryGet(type, out AudioClip clip, out float scale)) return;
            if (Instance.ambienceSource.clip == clip && Instance.ambienceSource.isPlaying) return;
            Instance.currentAmbienceScale = scale;
            Instance.ambienceSource.clip = clip;
            Instance.ambienceSource.volume = Instance.SoundEnabled
                ? Instance.GameVolume * scale
                : 0f;
            Instance.ambienceSource.Play();
        }

        public static void StopAmbience()
        {
            if (Instance == null) return;
            Instance.ambienceSource.Stop();
            Instance.ambienceSource.clip = null;
        }

        public static void StopMusic()
        {
            if (Instance == null) return;
            Instance.BeginMusicFade(null, 1f);
        }

        private void BeginMusicFade(AudioClip nextClip, float scale)
        {
            if (musicSource.clip == nextClip && musicSource.isPlaying) return;
            if (musicFadeRoutine != null) StopCoroutine(musicFadeRoutine);
            musicFadeRoutine = StartCoroutine(FadeMusic(nextClip, scale));
        }

        private IEnumerator FadeMusic(AudioClip nextClip, float nextScale)
        {
            const float fadeDuration = .45f;
            float startVolume = musicSource.volume;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
                yield return null;
            }

            musicSource.Stop();
            musicSource.clip = nextClip;
            currentMusicScale = nextScale;
            if (nextClip != null && MusicEnabled)
            {
                musicSource.Play();
                elapsed = 0f;
                float targetVolume = MusicVolume * currentMusicScale;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    musicSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / fadeDuration);
                    yield return null;
                }
                musicSource.volume = targetVolume;
            }
            musicFadeRoutine = null;
        }

        private void PlayClip(AudioSource source, AudioClip clip, float volume)
        {
            if (!SoundEnabled || source == null || clip == null || volume <= 0f) return;
            source.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        public void SetSoundEnabled(bool enabled)
        {
            SoundEnabled = enabled;
            PlayerPrefs.SetInt(SoundEnabledKey, enabled ? 1 : 0);
            if (!enabled)
            {
                uiSource.Stop();
                gameSource.Stop();
                ambienceSource.volume = 0f;
            }
            else if (ambienceSource.isPlaying)
                ambienceSource.volume = GameVolume * currentAmbienceScale;
            PlayerPrefs.Save();
        }

        public void SetUiVolume(float volume)
        {
            UiVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(UiVolumeKey, UiVolume);
            PlayerPrefs.Save();
        }

        public void SetGameVolume(float volume)
        {
            GameVolume = Mathf.Clamp01(volume);
            if (ambienceSource != null && SoundEnabled)
                ambienceSource.volume = GameVolume * currentAmbienceScale;
            PlayerPrefs.SetFloat(GameVolumeKey, GameVolume);
            PlayerPrefs.Save();
        }

        public void SetMusicEnabled(bool enabled)
        {
            MusicEnabled = enabled;
            PlayerPrefs.SetInt(MusicEnabledKey, enabled ? 1 : 0);
            if (!enabled) musicSource.Pause();
            else if (musicSource.clip != null)
            {
                musicSource.volume = MusicVolume * currentMusicScale;
                musicSource.UnPause();
                if (!musicSource.isPlaying) musicSource.Play();
            }
            PlayerPrefs.Save();
        }

        public void SetMusicVolume(float volume)
        {
            MusicVolume = Mathf.Clamp01(volume);
            if (musicSource != null && MusicEnabled)
                musicSource.volume = MusicVolume * currentMusicScale;
            PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
            PlayerPrefs.Save();
        }
    }
}
