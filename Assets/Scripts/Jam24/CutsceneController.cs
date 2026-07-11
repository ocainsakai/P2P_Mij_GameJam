using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Jam24
{
    public sealed class CutsceneController : MonoBehaviour
    {
        private static readonly string[] Titles =
        {
            "OCTO'S TREASURE",
            "A LOST SOLE",
            "ENGINEER THE CURRENT"
        };

        private static readonly string[] Stories =
        {
            "Deep below the summer waves, Octo keeps the ocean's strangest slipper collection.",
            "A new sole has fallen into dangerous currents — too far for any tentacle to reach.",
            "Turn jets, open valves and lift it with bubbles. Never grab the sole: make the ocean carry it home."
        };

        [SerializeField] private Text titleLabel;
        [SerializeField] private Text storyLabel;
        [SerializeField] private Text pageLabel;
        [SerializeField] private CanvasGroup storyGroup;
        [SerializeField, Min(1f)] private float autoAdvanceAfter = 4.5f;

        private int page;
        private Coroutine autoRoutine;

        public void Configure(Text title, Text story, Text pageCounter, CanvasGroup group)
        {
            titleLabel = title;
            storyLabel = story;
            pageLabel = pageCounter;
            storyGroup = group;
        }

        private void Start() => ShowPage(0);

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame) Skip();
            else if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame) Next();
        }

        public void Next()
        {
            if (page + 1 >= Titles.Length) { Finish(); return; }
            ShowPage(page + 1);
        }

        public void Skip() => Finish();

        private void ShowPage(int index)
        {
            page = Mathf.Clamp(index, 0, Titles.Length - 1);
            Debug.Log($"Beach Flow Cutscene: page {page + 1}/{Titles.Length} - {Titles[page]}", this);
            if (titleLabel != null) titleLabel.text = Titles[page];
            if (storyLabel != null) storyLabel.text = Stories[page];
            if (pageLabel != null) pageLabel.text = $"{page + 1}  /  {Titles.Length}";
            if (storyGroup != null)
            {
                storyGroup.alpha = 0f;
                StartCoroutine(FadeIn());
            }
            if (autoRoutine != null) StopCoroutine(autoRoutine);
            autoRoutine = StartCoroutine(AutoAdvance());
        }

        private IEnumerator FadeIn()
        {
            float time = 0f;
            while (time < .45f)
            {
                time += Time.unscaledDeltaTime;
                storyGroup.alpha = Mathf.Clamp01(time / .45f);
                yield return null;
            }
        }

        private IEnumerator AutoAdvance()
        {
            yield return new WaitForSecondsRealtime(autoAdvanceAfter);
            Next();
        }

        private void Finish()
        {
            PlayerPrefs.SetInt(GameFlow.CutsceneSeenKey, 1);
            PlayerPrefs.Save();
            GameFlow.Instance?.Home();
        }
    }
}
