using UnityEngine;

namespace Jam24
{
    public static class SaveData
    {
        public const string LastLevelKey = "jam24.lastLevel";
        private const string UnlockedKey = "jam24.unlocked";

        public static int HighestUnlocked => PlayerPrefs.GetInt(UnlockedKey, 0);
        public static bool IsUnlocked(int level) => level <= HighestUnlocked;
        public static bool IsComplete(int level) => PlayerPrefs.GetInt($"jam24.level.{level}.done", 0) == 1;

        public static void CompleteLevel(int level, int totalLevels)
        {
            PlayerPrefs.SetInt($"jam24.level.{level}.done", 1);
            int next = Mathf.Min(level + 1, Mathf.Max(0, totalLevels - 1));
            PlayerPrefs.SetInt(UnlockedKey, Mathf.Max(HighestUnlocked, next));
            PlayerPrefs.SetInt(LastLevelKey, next);
            PlayerPrefs.Save();
        }

        public static void ResetAll()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }
    }
}
