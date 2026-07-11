using UnityEngine;
using UnityEngine.UI;

namespace Jam24
{
    public sealed class GameUI : MonoBehaviour
    {
        [SerializeField] private SoleFlowPuzzle puzzle;
        [SerializeField] private Text actionsLabel;
        [SerializeField] private Text modeLabel;
        [SerializeField] private Text levelLabel;
        [SerializeField] private Text tutorialLabel;
        [SerializeField] private Text starsLabel;
        [SerializeField] private GameObject pausePopup;
        [SerializeField] private GameObject winPopup;
        [SerializeField] private GameObject losePopup;

        private void OnEnable()
        {
            GameFlow.StateChanged += RefreshState;
            if (puzzle != null)
            {
                puzzle.ActionsChanged += RefreshActions;
                puzzle.ModeChanged += RefreshMode;
                puzzle.TutorialChanged += RefreshTutorial;
                puzzle.StarsEarned += RefreshStars;
            }
            if (GameFlow.Instance != null) RefreshState(GameFlow.Instance.State);
        }

        private void Start()
        {
            if (puzzle == null) return;
            RefreshActions(puzzle.Actions);
            RefreshMode(puzzle.Mode);
            if (levelLabel != null && puzzle.Level != null)
                levelLabel.text = $"LEVEL {puzzle.LevelNumber}/10  •  {puzzle.Level.title.ToUpperInvariant()}";
        }

        private void OnDisable()
        {
            GameFlow.StateChanged -= RefreshState;
            if (puzzle == null) return;
            puzzle.ActionsChanged -= RefreshActions;
            puzzle.ModeChanged -= RefreshMode;
            puzzle.TutorialChanged -= RefreshTutorial;
            puzzle.StarsEarned -= RefreshStars;
        }

        public void Configure(SoleFlowPuzzle game, Text actions, Text mode, Text level, Text tutorial, Text stars, GameObject pause, GameObject win, GameObject lose)
        {
            puzzle=game; actionsLabel=actions; modeLabel=mode; levelLabel=level; tutorialLabel=tutorial; starsLabel=stars;
            pausePopup=pause; winPopup=win; losePopup=lose;
        }

        public void Play() => GameFlow.Instance?.Continue();
        public void PlayLevel(int index) => GameFlow.Instance?.PlayLevel(index);
        public void Pause() => GameFlow.Instance?.Pause();
        public void Resume() => GameFlow.Instance?.Resume();
        public void Restart() => GameFlow.Instance?.Restart();
        public void Home() => GameFlow.Instance?.Home();
        public void Next() => GameFlow.Instance?.NextLevel();
        public void ShowCutscene() => GameFlow.Instance?.ShowCutscene();
        public void Undo() => puzzle?.Undo();
        public void ResetPuzzle() => puzzle?.ResetPuzzle();
        public void ResetSave() => SaveData.ResetAll();

        private void RefreshActions(int actions)
        {
            if (actionsLabel != null) actionsLabel.text = $"ACTIONS  {actions}";
        }

        private void RefreshMode(PuzzleMode mode)
        {
            if (modeLabel == null) return;
            modeLabel.text = mode switch
            {
                PuzzleMode.Countdown => "CURRENT STARTING  •  GET READY",
                PuzzleMode.Flowing => "REAL-TIME FLOW  •  ACT NOW",
                PuzzleMode.Won => "COLLECTED!",
                _ => "STUCK — TRY AGAIN"
            };
            modeLabel.color = mode == PuzzleMode.Countdown ? new Color32(255,231,147,255) : new Color32(129,241,229,255);
        }

        private void RefreshTutorial(string message)
        {
            if (tutorialLabel != null) tutorialLabel.text = message;
        }

        private void RefreshStars(int stars)
        {
            if (starsLabel != null) starsLabel.text = new string('★', stars) + new string('☆', 3 - stars);
        }

        private void RefreshState(GameState state)
        {
            if (pausePopup != null) pausePopup.SetActive(state == GameState.Paused);
            if (winPopup != null) winPopup.SetActive(state == GameState.Win);
            if (losePopup != null) losePopup.SetActive(state == GameState.Lose);
        }
    }
}
