using UnityEngine;
using UnityEngine.UI;

namespace Jam24
{
    /// <summary>Scene UI adapter. Public methods can be wired directly to Button.onClick.</summary>
    public sealed class GameUI : MonoBehaviour
    {
        [SerializeField] private GridPuzzle puzzle;
        [SerializeField] private Text moveLabel;
        [SerializeField] private Text connectionLabel;
        [SerializeField] private Text levelLabel;
        [SerializeField] private GameObject pausePopup;
        [SerializeField] private GameObject winPopup;
        [SerializeField] private GameObject losePopup;

        private void OnEnable()
        {
            GameFlow.StateChanged += RefreshState;
            if (puzzle != null)
            {
                puzzle.MovesChanged += RefreshMoves;
                puzzle.ConnectionsChanged += RefreshConnections;
                RefreshMoves(puzzle.Moves);
                RefreshConnections(puzzle.ConnectedCount, puzzle.PairCount);
            }
            if (GameFlow.Instance != null) RefreshState(GameFlow.Instance.State);
        }

        private void Start()
        {
            if (levelLabel != null && puzzle != null) levelLabel.text = $"LEVEL {puzzle.LevelNumber} / {FlowLevelCatalog.Count}";
        }

        private void OnDisable()
        {
            GameFlow.StateChanged -= RefreshState;
            if (puzzle != null)
            {
                puzzle.MovesChanged -= RefreshMoves;
                puzzle.ConnectionsChanged -= RefreshConnections;
            }
        }

        public void Configure(GridPuzzle board, Text moves, Text connections, Text level, GameObject pause, GameObject win, GameObject lose)
        {
            puzzle = board;
            moveLabel = moves;
            connectionLabel = connections;
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
        public void Undo() => puzzle?.Undo();
        public void ResetPuzzle() => puzzle?.ResetPuzzle();
        public void ResetSave() => SaveData.ResetAll();

        private void RefreshMoves(int moves)
        {
            if (moveLabel != null) moveLabel.text = $"MOVES  {moves}";
        }

        private void RefreshConnections(int connected, int total)
        {
            if (connectionLabel != null) connectionLabel.text = $"FLOWS  {connected}/{total}";
            if (levelLabel != null && puzzle != null) levelLabel.text = $"LEVEL {puzzle.LevelNumber} / {FlowLevelCatalog.Count}";
        }

        private void RefreshState(GameState state)
        {
            if (pausePopup != null) pausePopup.SetActive(state == GameState.Paused);
            if (winPopup != null) winPopup.SetActive(state == GameState.Win);
            if (losePopup != null) losePopup.SetActive(state == GameState.Lose);
        }
    }
}
