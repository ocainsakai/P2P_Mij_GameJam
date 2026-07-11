using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jam24
{
    public sealed class GameplayCheatConsole : MonoBehaviour
    {
        private const string ControlName = "GameplayCheatConsoleInput";

        private readonly List<string> log = new();
        private GameplayManager gameplay;
        private string input = string.Empty;
        private bool visible;
        private bool focusInput;
        private Vector2 scroll;

        private void Awake()
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            Destroy(this);
            return;
#else
            gameplay = GetComponent<GameplayManager>();
            Write("Cheat console ready. Type 'help' for commands.");
#endif
        }

        private void OnGUI()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Event current = Event.current;
            if (current.type == EventType.KeyDown &&
                (current.keyCode == KeyCode.BackQuote || current.keyCode == KeyCode.F1))
            {
                visible = !visible;
                focusInput = visible;
                current.Use();
            }

            if (!visible) return;

            float width = Mathf.Min(720f, Screen.width - 24f);
            float height = Mathf.Min(420f, Screen.height - 24f);
            Rect panel = new(12f, 12f, width, height);
            GUI.Box(panel, GUIContent.none);

            GUILayout.BeginArea(new Rect(panel.x + 10f, panel.y + 10f, panel.width - 20f, panel.height - 20f));
            GUILayout.Label("GAMEPLAY CHEAT CONSOLE   [` or F1 to close]");

            scroll = GUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));
            foreach (string line in log) GUILayout.Label(line);
            GUILayout.EndScrollView();

            GUI.SetNextControlName(ControlName);
            input = GUILayout.TextField(input);

            if (focusInput)
            {
                GUI.FocusControl(ControlName);
                focusInput = false;
            }

            if (current.type == EventType.KeyDown &&
                (current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter))
            {
                Execute(input);
                input = string.Empty;
                focusInput = true;
                current.Use();
            }

            GUILayout.EndArea();
#endif
        }

        private void Execute(string commandLine)
        {
            commandLine = commandLine.Trim();
            if (commandLine.Length == 0) return;

            Write($"> {commandLine}");
            string[] parts = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string command = parts[0].ToLowerInvariant();

            switch (command)
            {
                case "help":
                    Write("level <index>  - load Level index directly (0-based)");
                    Write("next / prev    - load adjacent level");
                    Write("reload         - reload current level prefab");
                    Write("win / lose     - force gameplay result");
                    Write("pause / resume - change gameplay state");
                    Write("home           - return to Home scene");
                    Write("status         - show current level and state");
                    Write("clear          - clear console output");
                    break;

                case "level":
                    if (parts.Length < 2 || !int.TryParse(parts[1], out int levelIndex))
                    {
                        Write("Usage: level <index>. Example: level 2");
                        break;
                    }
                    LoadLevel(levelIndex);
                    break;

                case "next":
                    LoadLevel(CurrentLevelIndex + 1);
                    break;

                case "prev":
                    LoadLevel(CurrentLevelIndex - 1);
                    break;

                case "reload":
                    LoadLevel(CurrentLevelIndex);
                    break;

                case "win":
                    GameFlow.Instance?.Win();
                    Write("Forced Win state.");
                    break;

                case "lose":
                    GameFlow.Instance?.Lose();
                    Write("Forced Lose state.");
                    break;

                case "pause":
                    GameFlow.Instance?.Pause();
                    Write("Paused.");
                    break;

                case "resume":
                    GameFlow.Instance?.Resume();
                    Write("Resumed.");
                    break;

                case "home":
                    GameFlow.Instance?.Home();
                    break;

                case "status":
                    Write($"Level: {CurrentLevelIndex}, State: {GameFlow.Instance?.State.ToString() ?? "No GameFlow"}");
                    break;

                case "clear":
                    log.Clear();
                    break;

                default:
                    Write($"Unknown command '{command}'. Type 'help'.");
                    break;
            }

            scroll.y = float.MaxValue;
        }

        private int CurrentLevelIndex => GameFlow.Instance == null ? 0 : GameFlow.Instance.CurrentLevel;

        private void LoadLevel(int levelIndex)
        {
            if (gameplay == null)
            {
                Write("GameplayManager was not found on this GameObject.");
                return;
            }

            if (!gameplay.IsValidLevelIndex(levelIndex))
            {
                Write($"Invalid level index {levelIndex}. Valid range: 0-{gameplay.LevelCount - 1}.");
                return;
            }

            GameFlow.Instance?.PrepareRetry();
            GameFlow.Instance?.SetCurrentLevel(levelIndex);
            if (gameplay.LoadLevel(levelIndex))
                Write($"Loaded level {levelIndex}: {gameplay.CurrentLevel.name}");
            else
                Write($"Level {levelIndex} failed validation. Check the Unity Console.");
        }

        private void Write(string message)
        {
            log.Add(message);
            if (log.Count > 80) log.RemoveAt(0);
        }
    }
}
