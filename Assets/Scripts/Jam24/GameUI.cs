using UnityEngine;
using UnityEngine.UI;

namespace Jam24
{
    public sealed class GameUI : MonoBehaviour
    {
        [SerializeField] private Text levelLabel;
        [SerializeField] private GameObject pausePopup;
        [SerializeField] private GameObject winPopup;
        [SerializeField] private GameObject losePopup;

        private void OnEnable()
        {
            GameFlow.StateChanged += RefreshState;
            if (GameFlow.Instance != null) RefreshState(GameFlow.Instance.State);
        }

        private void Start()
        {
            if (levelLabel != null && GameFlow.Instance != null)
                levelLabel.text = $"LEVEL {GameFlow.Instance.CurrentLevel + 1}";
        }

        private void OnDisable()
        {
            GameFlow.StateChanged -= RefreshState;
        }

        public void Configure(Text level, GameObject pause, GameObject win, GameObject lose)
        {
            levelLabel = level;
            pausePopup = pause;
            winPopup = win;
            losePopup = lose;
        }

        public void Play() => GameFlow.Instance?.Continue();
        public void PlayLevel(int index) => GameFlow.Instance?.PlayLevel(index);
        public void Pause() => GameFlow.Instance?.Pause();
        public void Resume() => GameFlow.Instance?.Resume();
        public void Restart() => GameFlow.Instance?.Restart();
        public void Home() => GameFlow.Instance?.Home();
        public void Next() => GameFlow.Instance?.NextLevel();
        public void ShowCutscene() => GameFlow.Instance?.ShowCutscene();
        public void ResetSave() => SaveData.ResetAll();

        private void RefreshState(GameState state)
        {
            if (pausePopup != null) pausePopup.SetActive(state == GameState.Paused);
            if (winPopup != null) winPopup.SetActive(state == GameState.Win);
            if (losePopup != null) losePopup.SetActive(state == GameState.Lose);
        }
    }
}
