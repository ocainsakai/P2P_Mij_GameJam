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
        public static int BestMoves(int level) => PlayerPrefs.GetInt($"jam24.level.{level}.moves", 0);
        public static int BestStars(int level) => PlayerPrefs.GetInt($"jam24.level.{level}.stars", 0);
        public static string CollectedSlipper(int level) => PlayerPrefs.GetString($"jam24.level.{level}.slipper", string.Empty);

        public static void CompleteLevel(int level, int moves, int stars, string slipperName)
        {
            PlayerPrefs.SetInt($"jam24.level.{level}.done", 1);
            int oldBest = BestMoves(level);
            if (oldBest == 0 || moves < oldBest) PlayerPrefs.SetInt($"jam24.level.{level}.moves", moves);
            PlayerPrefs.SetInt($"jam24.level.{level}.stars", Mathf.Max(BestStars(level), stars));
            PlayerPrefs.SetString($"jam24.level.{level}.slipper", slipperName);
            int next = Mathf.Min(level + 1, SoleLevelCatalog.Count - 1);
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
