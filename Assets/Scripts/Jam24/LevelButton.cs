using UnityEngine;
using UnityEngine.UI;

namespace Jam24
{
    [RequireComponent(typeof(Button))]
    public sealed class LevelButton : MonoBehaviour
    {
        [SerializeField] private int levelIndex;
        [SerializeField] private Text label;

        public void Configure(int index, Text text)
        {
            levelIndex = index;
            label = text;
        }

        private void Start()
        {
            bool unlocked = SaveData.IsUnlocked(levelIndex);
            GetComponent<Button>().interactable = unlocked;
            if (label != null)
            {
                string best = SaveData.BestMoves(levelIndex) > 0 ? $"\nBEST {SaveData.BestMoves(levelIndex)}" : string.Empty;
                label.text = unlocked ? $"{levelIndex + 1}{best}" : "LOCKED";
            }
        }

        public void Play()
        {
            if (SaveData.IsUnlocked(levelIndex)) GameFlow.Instance?.PlayLevel(levelIndex);
        }
    }
}
