using UnityEngine;

namespace Jam24
{
    /// <summary>Ten hand-authored levels available without scene assets or extra setup.</summary>
    public static class FlowLevelCatalog
    {
        private const string Coral = "#FF6B6B";
        private const string Ocean = "#35B8E0";
        private const string Sun = "#FFD166";
        private const string Palm = "#55C78A";
        private const string Shell = "#B88AE8";

        public static int Count => 10;

        public static LevelDefinition Get(int index)
        {
            index = Mathf.Clamp(index, 0, Count - 1);
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.name = $"Beach Flow {index + 1}";
            level.hideFlags = HideFlags.DontSave;

            switch (index)
            {
                case 0: Configure(level, 4, 4, P("Coral", Coral, 0,0, 3,0), P("Ocean", Ocean, 0,3, 3,3)); break;
                case 1: Configure(level, 4, 4, P("Coral", Coral, 0,0, 3,1), P("Ocean", Ocean, 0,3, 3,2)); break;
                case 2: Configure(level, 5, 5, P("Coral", Coral, 0,0, 4,0), P("Ocean", Ocean, 0,4, 4,4), P("Sun", Sun, 1,2, 3,2)); break;
                case 3: Configure(level, 5, 5, P("Coral", Coral, 0,0, 4,1), P("Ocean", Ocean, 0,4, 4,3), P("Sun", Sun, 1,1, 1,3)); break;
                case 4: Configure(level, 5, 5, P("Coral", Coral, 0,1, 4,1), P("Ocean", Ocean, 0,3, 4,3), P("Sun", Sun, 1,4, 3,4)); break;
                case 5: Configure(level, 6, 6, P("Coral", Coral, 0,0, 5,1), P("Ocean", Ocean, 0,5, 5,4), P("Sun", Sun, 1,2, 4,2), P("Palm", Palm, 1,3, 4,3)); break;
                case 6: Configure(level, 6, 6, P("Coral", Coral, 0,1, 5,0), P("Ocean", Ocean, 0,4, 5,5), P("Sun", Sun, 1,2, 4,2), P("Palm", Palm, 1,3, 4,3)); break;
                case 7: Configure(level, 6, 6, P("Coral", Coral, 0,0, 5,0), P("Ocean", Ocean, 0,5, 5,5), P("Sun", Sun, 0,2, 2,3), P("Palm", Palm, 3,2, 5,3)); break;
                case 8: Configure(level, 7, 7, P("Coral", Coral, 0,0, 6,1), P("Ocean", Ocean, 0,6, 6,5), P("Sun", Sun, 1,2, 5,2), P("Palm", Palm, 1,4, 5,4), P("Shell", Shell, 2,3, 4,3)); break;
                default: Configure(level, 7, 7, P("Coral", Coral, 0,1, 6,0), P("Ocean", Ocean, 0,5, 6,6), P("Sun", Sun, 1,2, 5,2), P("Palm", Palm, 1,4, 5,4), P("Shell", Shell, 2,3, 4,3)); break;
            }
            return level;
        }

        private static FlowPair P(string name, string color, int ax, int ay, int bx, int by) =>
            new(name, color, new Vector2Int(ax, ay), new Vector2Int(bx, by));

        private static void Configure(LevelDefinition level, int width, int height, params FlowPair[] pairs)
        {
            level.width = width;
            level.height = height;
            level.pairs = pairs;
            level.requireFullBoard = false;
        }
    }
}
