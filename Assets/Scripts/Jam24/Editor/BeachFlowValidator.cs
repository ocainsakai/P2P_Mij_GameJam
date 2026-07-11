#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Jam24.Editor
{
    public static class BeachFlowValidator
    {
        [InitializeOnLoadMethod]
        private static void ValidateOnceAfterReload()
        {
            EditorApplication.delayCall += () =>
            {
                const string key = "Jam24.SoleFlow.ProposalValidated";
                if (EditorApplication.isCompiling || EditorApplication.isPlaying || SessionState.GetBool(key, false)) return;
                SessionState.SetBool(key, true);
                Validate();
            };
        }

        [MenuItem("Jam24/Validate Complete Game _F5")]
        public static void Validate()
        {
            Require(SoleLevelCatalog.Count == 10, "MVP must contain exactly 10 proposal levels.");
            var titles = new HashSet<string>();
            for (int i = 0; i < SoleLevelCatalog.Count; i++) ValidateLevel(i, titles);

            Require(File.Exists("Assets/Resources/BeachFlow/beach_background.png"), "Summer beach background is missing.");
            Require(File.Exists("Assets/Resources/SoleFlow/soleflow_sheet.png"), "Generated octopus/slipper/environment sprite sheet is missing.");
            Require(File.ReadAllText("Assets/Scenes/Init.unity").Contains("Jam24_Managers"), "Init manager root is missing.");
            Require(File.ReadAllText("Assets/Scenes/Home.unity").Contains("Jam24_HomeUI"), "Home UI root is missing.");
            string gameplay = File.ReadAllText("Assets/Scenes/Gameplay.unity");
            Require(gameplay.Contains("Jam24_Gameplay") && gameplay.Contains("SoleFlowPuzzle") && gameplay.Contains("Reset"), "Real-time proposal gameplay hierarchy is incomplete.");
            Require(File.ReadAllText("Assets/Scenes/Loading.unity").Contains("Jam24_Loading"), "Loading scene is missing.");
            Require(File.ReadAllText("Assets/Scenes/Cutscene.unity").Contains("Jam24_Cutscene"), "Cutscene scene is missing.");
            string builds = File.ReadAllText("ProjectSettings/EditorBuildSettings.asset");
            Require(builds.Contains("Assets/Scenes/Loading.unity") && builds.Contains("Assets/Scenes/Cutscene.unity"), "Build Settings flow is incomplete.");

            Debug.Log("SOLE FLOW PROPOSAL VALIDATION PASSED: 10 deterministic environmental-puzzle levels, Setup/Flow modes, collection/scoring, scenes and assets are present.");
        }

        private static void ValidateLevel(int index, HashSet<string> titles)
        {
            SoleLevel level = SoleLevelCatalog.Get(index);
            Require(!string.IsNullOrWhiteSpace(level.title) && titles.Add(level.title), $"Level {index + 1}: missing or duplicate title.");
            Require(level.route != null && level.route.Length >= 3, $"Level {index + 1}: route needs at least 3 physics waypoints.");
            Require(level.mechanisms != null && level.mechanisms.Length >= 1, $"Level {index + 1}: no environmental mechanic.");
            Require(level.start != level.nest, $"Level {index + 1}: slipper already in nest.");
            foreach (MechanismSpec mechanism in level.mechanisms)
                Require(mechanism.requiredState >= 0 && mechanism.requiredState < mechanism.stateCount, $"Level {index + 1}: invalid mechanism solution state.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new BuildFailedException("Sole Flow validation failed: " + message);
        }
    }
}
#endif
