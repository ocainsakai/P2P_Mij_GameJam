#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Jam24.Editor
{
    [InitializeOnLoad]
    public static class OctopusAnimatorBuilder
    {
        private const string ControllerPath = "Assets/Animations/OctopusPlayer.controller";
        private const string ClipFolder = "Assets/Animations/OctopusPlayerClips";
        private const string VersionPath = ClipFolder + "/v4.generated.txt";

        static OctopusAnimatorBuilder() => EditorApplication.delayCall += GenerateIfNeeded;

        [MenuItem("Jam24/Rebuild Octopus Animator")]
        public static void Rebuild()
        {
            EnsureFolder();
            BuildClipsAndController();
            File.WriteAllText(VersionPath, "Unity Animator clips v4 - static tentacles");
            AssetDatabase.ImportAsset(VersionPath);
            AssetDatabase.SaveAssets();
            Debug.Log("Rebuilt OctopusPlayer Unity Animator with five keyed clips.");
        }

        private static void GenerateIfNeeded()
        {
            if (File.Exists(VersionPath)) return;
            Rebuild();
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(ClipFolder))
                AssetDatabase.CreateFolder("Assets/Animations", "OctopusPlayerClips");
        }

        private static void BuildClipsAndController()
        {
            AnimationClip idle = CreateClip("Octopus_Idle", 1.5f,
                new[] { 0f, .75f, 1.5f }, new[] { 0f, .035f, 0f }, 0f);
            AnimationClip horizontal = CreateClip("Octopus_SwimHorizontal", .7f,
                new[] { 0f, .35f, .7f }, new[] { 0f, .025f, 0f }, 0f);
            AnimationClip up = CreateClip("Octopus_SwimUp", .65f,
                new[] { 0f, .325f, .65f }, new[] { 0f, .045f, 0f }, -7f);
            AnimationClip down = CreateClip("Octopus_SwimDown", .78f,
                new[] { 0f, .39f, .78f }, new[] { 0f, -.03f, 0f }, 7f);
            AnimationClip push = CreateClip("Octopus_Push", .55f,
                new[] { 0f, .275f, .55f }, new[] { 0f, -.018f, 0f }, 0f, true);

            AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("State", AnimatorControllerParameterType.Int);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState defaultState = AddState(machine, "Idle", idle, new Vector3(260, 40));
            AddState(machine, "SwimHorizontal", horizontal, new Vector3(500, 40));
            AddState(machine, "SwimUp", up, new Vector3(500, -70));
            AddState(machine, "SwimDown", down, new Vector3(500, 150));
            AddState(machine, "Push", push, new Vector3(740, 40));
            machine.defaultState = defaultState;

            ChildAnimatorState[] states = machine.states;
            for (int i = 0; i < states.Length; i++)
            {
                AnimatorStateTransition transition = machine.AddAnyStateTransition(states[i].state);
                transition.AddCondition(AnimatorConditionMode.Equals, i, "State");
                transition.hasExitTime = false;
                transition.hasFixedDuration = true;
                transition.duration = .12f;
                transition.canTransitionToSelf = false;
                transition.interruptionSource = TransitionInterruptionSource.SourceThenDestination;
            }

            EditorUtility.SetDirty(controller);
        }

        private static AnimatorState AddState(AnimatorStateMachine machine, string name, Motion clip, Vector3 position)
        {
            AnimatorState state = machine.AddState(name, position);
            state.motion = clip;
            state.writeDefaultValues = false;
            return state;
        }

        private static AnimationClip CreateClip(string name, float duration, float[] times, float[] headY,
            float headAngle, bool push = false)
        {
            string path = $"{ClipFolder}/{name}.anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip { name = name, frameRate = 60f };
                AssetDatabase.CreateAsset(clip, path);
            }
            clip.ClearCurves();

            SetCurve(clip, "Head", "m_LocalPosition.y", times, headY);
            SetConstant(clip, "Head", "localEulerAnglesRaw.z", times, headAngle);
            SetConstant(clip, "Head", "m_LocalScale.x", times, push ? 1.08f : 1f);
            SetConstant(clip, "Head", "m_LocalScale.y", times, push ? .9f : 1f);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.startTime = 0f;
            settings.stopTime = duration;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static void SetConstant(AnimationClip clip, string path, string property, float[] times, float value)
        {
            float[] values = new float[times.Length];
            for (int i = 0; i < values.Length; i++) values[i] = value;
            SetCurve(clip, path, property, times, values);
        }

        private static void SetCurve(AnimationClip clip, string path, string property, float[] times, float[] values)
        {
            AnimationCurve curve = new AnimationCurve();
            for (int i = 0; i < times.Length; i++) curve.AddKey(times[i], values[i]);
            for (int i = 0; i < curve.length; i++) AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
            EditorCurveBinding binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), property);
            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }
    }
}
#endif
