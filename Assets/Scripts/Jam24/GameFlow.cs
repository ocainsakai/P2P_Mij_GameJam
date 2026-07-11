using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Jam24
{
    public enum GameState { MainMenu, Playing, Paused, Win, Lose }

    /// <summary>Persistent, dependency-free game flow for a small jam project.</summary>
    public sealed class GameFlow : MonoBehaviour
    {
        public const string HomeScene = "Home";
        public const string GameplayScene = "Gameplay";
        public const string LoadingScene = "Loading";
        public const string CutsceneScene = "Cutscene";
        public const string CutsceneSeenKey = "jam24.cutsceneSeen";

        public static string PendingScene { get; private set; } = HomeScene;

        public static GameFlow Instance { get; private set; }
        public static event Action<GameState> StateChanged;
        public GameState State { get; private set; }
        public int CurrentLevel { get; private set; }
        [field: SerializeField, Min(1)] public int TotalLevels { get; private set; } = 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInstanceForDirectScenePlay()
        {
            if (Instance != null) return;
            var root = new GameObject("[GameFlow - Direct Play]");
            var flow = root.AddComponent<GameFlow>();
            flow.SyncScene(SceneManager.GetActiveScene());
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
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Time.timeScale = 1f;
            SyncScene(scene);
        }

        private void SyncScene(Scene scene)
        {
            if (scene.name == "Init")
            {
                RequestScene(PlayerPrefs.GetInt(CutsceneSeenKey, 0) == 0 ? CutsceneScene : HomeScene);
                return;
            }
            SetState(scene.name == GameplayScene ? GameState.Playing : GameState.MainMenu);
        }

        public void PlayLevel(int levelIndex)
        {
            CurrentLevel = Mathf.Clamp(levelIndex, 0, TotalLevels - 1);
            PlayerPrefs.SetInt(SaveData.LastLevelKey, CurrentLevel);
            RequestScene(GameplayScene);
        }

        public void Continue() => PlayLevel(PlayerPrefs.GetInt(SaveData.LastLevelKey, 0));
        public void Home() => RequestScene(HomeScene);
        public void ShowCutscene() => RequestScene(CutsceneScene);
        public void Restart() => RequestScene(GameplayScene);

        public void TogglePause()
        {
            if (State == GameState.Playing) Pause();
            else if (State == GameState.Paused) Resume();
        }

        public void Pause()
        {
            if (State != GameState.Playing) return;
            Time.timeScale = 0f;
            SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            Time.timeScale = 1f;
            SetState(GameState.Playing);
        }

        public void Win(int moves)
        {
            if (State != GameState.Playing) return;
            SetState(GameState.Win);
        }

        public void Lose()
        {
            if (State == GameState.Playing) SetState(GameState.Lose);
        }

        public void PrepareRetry()
        {
            Time.timeScale = 1f;
            if (SceneManager.GetActiveScene().name == GameplayScene) SetState(GameState.Playing);
        }

        public void NextLevel()
        {
            if (CurrentLevel + 1 < TotalLevels) PlayLevel(CurrentLevel + 1);
            else Home();
        }

        public void RequestScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName) || sceneName == LoadingScene) return;
            PendingScene = sceneName;
            Time.timeScale = 1f;
            Debug.Log($"Beach Flow: transition via Loading to '{sceneName}'.", this);
            SceneManager.LoadScene(LoadingScene);
        }

        private void SetState(GameState value)
        {
            State = value;
            StateChanged?.Invoke(value);
        }
    }
}
