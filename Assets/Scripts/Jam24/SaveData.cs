using UnityEngine;

namespace Jam24
{
    public static class SaveData
    {
        public const string LastLevelKey = "jam24.lastLevel";
        private const string UnlockedKey = "jam24.unlocked";
        private const string FishSchoolHasRollKey = "jam24.fishSchool.hasRoll";
        private const string FishSchoolLastLevelKey = "jam24.fishSchool.lastLevel";
        private const string FishSchoolLastResultKey = "jam24.fishSchool.lastResult";
        private const string FishSchoolNextChanceKey = "jam24.fishSchool.nextChance";

        public static int HighestUnlocked => PlayerPrefs.GetInt(UnlockedKey, 0);
        public static bool IsUnlocked(int level) => level <= HighestUnlocked;
        public static bool IsComplete(int level) => PlayerPrefs.GetInt($"jam24.level.{level}.done", 0) == 1;

        public static bool TryGetFishSchoolResult(int level, out bool appeared)
        {
            bool hasRoll = PlayerPrefs.GetInt(FishSchoolHasRollKey, 0) == 1;
            bool isSameLevel = PlayerPrefs.GetInt(FishSchoolLastLevelKey, -1) == level;
            appeared = PlayerPrefs.GetInt(FishSchoolLastResultKey, 0) == 1;
            return hasRoll && isSameLevel;
        }

        public static float GetFishSchoolNextChance(float fallbackChance) =>
            PlayerPrefs.GetFloat(FishSchoolNextChanceKey, fallbackChance);

        public static void SaveFishSchoolResult(int level, bool appeared, float nextChance)
        {
            PlayerPrefs.SetInt(FishSchoolHasRollKey, 1);
            PlayerPrefs.SetInt(FishSchoolLastLevelKey, level);
            PlayerPrefs.SetInt(FishSchoolLastResultKey, appeared ? 1 : 0);
            PlayerPrefs.SetFloat(FishSchoolNextChanceKey, Mathf.Clamp01(nextChance));
            PlayerPrefs.Save();
        }

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
