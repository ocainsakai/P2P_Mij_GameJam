using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Jam24
{
    /// <summary>Connects the authored Select level prefab to Home and game progress.</summary>
    [DisallowMultipleComponent]
    public sealed class HomeLevelSelectPopup : MonoBehaviour
    {
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private Button playButton;
        [SerializeField] private LevelCatalog levelCatalog;
        [SerializeField] private Sprite openShell;
        [SerializeField] private Sprite closedShell;
        [SerializeField, Min(1)] private int visibleLevels = 8;
        [SerializeField] private bool unlockAllLevelsForTesting;

        private RectTransform animatedHolder;
        private GridLayoutGroup levelGrid;
        private CanvasGroup canvasGroup;
        private Font font;
        private bool initialized;

        private void Awake()
        {
            if (playButton != null) playButton.onClick.AddListener(Open);
            if (popupRoot != null) popupRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (playButton != null) playButton.onClick.RemoveListener(Open);
        }

        public void Open()
        {
            if (popupRoot == null) return;
            if (!initialized) InitializeAuthoredPopup();
            RefreshLevels();
            popupRoot.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(Animate(true));
        }

        public void Close()
        {
            if (popupRoot == null || !popupRoot.activeSelf) return;
            StopAllCoroutines();
            StartCoroutine(Animate(false));
        }

        private void InitializeAuthoredPopup()
        {
            initialized = true;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Transform holder = FindChild(popupRoot.transform, "Holder");
            Transform grid = FindChild(popupRoot.transform, "LevelGrid");
            animatedHolder = holder as RectTransform;
            if (animatedHolder == null) animatedHolder = popupRoot.GetComponent<RectTransform>();
            levelGrid = grid != null ? grid.GetComponent<GridLayoutGroup>() : null;
            canvasGroup = animatedHolder.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = animatedHolder.gameObject.AddComponent<CanvasGroup>();

            if (levelGrid == null)
            {
                Debug.LogError("Select level prefab needs a LevelGrid with GridLayoutGroup.", popupRoot);
                return;
            }

            levelGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            levelGrid.constraintCount = 4;
            levelGrid.cellSize = new Vector2(250f, 205f);
            levelGrid.spacing = new Vector2(46f, 36f);
            levelGrid.childAlignment = TextAnchor.MiddleCenter;

            Button backdrop = popupRoot.GetComponent<Button>();
            if (backdrop != null) backdrop.onClick.AddListener(Close);

            Button cardBlocker = FindChild(popupRoot.transform, "LevelCard")?.GetComponent<Button>();
            if (cardBlocker != null) cardBlocker.onClick.RemoveAllListeners();

            BuildLevelSlots();
        }

        private void BuildLevelSlots()
        {
            for (int i = levelGrid.transform.childCount - 1; i >= 0; i--)
                Destroy(levelGrid.transform.GetChild(i).gameObject);

            int available = levelCatalog != null ? levelCatalog.Count : visibleLevels;
            int count = Mathf.Min(visibleLevels, available);
            for (int i = 0; i < count; i++) CreateSlot(i);
        }

        private void CreateSlot(int index)
        {
            GameObject slot = new GameObject($"Level {index + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            RectTransform rect = slot.GetComponent<RectTransform>();
            rect.SetParent(levelGrid.transform, false);

            Image image = slot.GetComponent<Image>();
            image.preserveAspect = true;
            Button button = slot.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            slot.AddComponent<UIButtonSfx>();

            int captured = index;
            button.onClick.AddListener(() => PlayLevel(captured));

            Text label = CreateLabel(slot.transform, "Number", 48, new Color(.03f, .20f, .33f, 1f));
            label.rectTransform.anchorMin = new Vector2(.5f, .5f);
            label.rectTransform.anchorMax = new Vector2(.5f, .5f);
            label.rectTransform.sizeDelta = new Vector2(115f, 70f);
            label.rectTransform.anchoredPosition = new Vector2(0f, 20f);

            Text stars = CreateLabel(slot.transform, "Stars", 30, new Color(1f, .74f, .04f, 1f));
            stars.rectTransform.anchorMin = new Vector2(.5f, 0f);
            stars.rectTransform.anchorMax = new Vector2(.5f, 0f);
            stars.rectTransform.sizeDelta = new Vector2(180f, 45f);
            stars.rectTransform.anchoredPosition = new Vector2(0f, 20f);
        }

        private void RefreshLevels()
        {
            if (levelGrid == null) return;
            for (int i = 0; i < levelGrid.transform.childCount; i++)
            {
                Transform slot = levelGrid.transform.GetChild(i);
                bool unlocked = IsLevelUnlocked(i);
                Image image = slot.GetComponent<Image>();
                Button button = slot.GetComponent<Button>();
                Text number = slot.Find("Number")?.GetComponent<Text>();
                Text stars = slot.Find("Stars")?.GetComponent<Text>();

                image.sprite = unlocked ? openShell : closedShell;
                image.color = unlocked ? Color.white : new Color(.78f, .86f, .9f, 1f);
                button.interactable = unlocked;
                if (number != null) number.text = unlocked ? (i + 1).ToString() : string.Empty;
                if (stars != null) stars.text = unlocked ? (SaveData.IsComplete(i) ? "★ ★ ★" : "☆ ☆ ☆") : string.Empty;
            }
        }

        private Text CreateLabel(Transform parent, string objectName, int fontSize, Color color)
        {
            GameObject item = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            item.transform.SetParent(parent, false);
            Text text = item.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 15;
            text.resizeTextMaxSize = fontSize;
            return text;
        }

        private void PlayLevel(int index)
        {
            if (IsLevelUnlocked(index)) GameFlow.Instance?.PlayLevel(index);
        }

        private bool IsLevelUnlocked(int index) =>
            Application.isEditor || unlockAllLevelsForTesting || SaveData.IsUnlocked(index);

        private IEnumerator Animate(bool opening)
        {
            if (animatedHolder == null) yield break;
            if (canvasGroup == null)
            {
                canvasGroup = animatedHolder.GetComponent<CanvasGroup>();
                if (canvasGroup == null) canvasGroup = animatedHolder.gameObject.AddComponent<CanvasGroup>();
            }

            const float duration = .24f;
            float elapsed = 0f;
            float fromScale = opening ? .82f : 1f;
            float toScale = opening ? 1f : .88f;
            float fromAlpha = opening ? 0f : 1f;
            float toAlpha = opening ? 1f : 0f;
            animatedHolder.localScale = Vector3.one * fromScale;
            canvasGroup.alpha = fromAlpha;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = opening ? 1f - Mathf.Pow(1f - t, 3f) : t * t;
                animatedHolder.localScale = Vector3.one * Mathf.Lerp(fromScale, toScale, eased);
                canvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
                yield return null;
            }

            animatedHolder.localScale = Vector3.one * toScale;
            canvasGroup.alpha = toAlpha;
            if (!opening) popupRoot.SetActive(false);
        }

        private static Transform FindChild(Transform root, string childName)
        {
            if (root.name == childName) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform result = FindChild(root.GetChild(i), childName);
                if (result != null) return result;
            }
            return null;
        }
    }
}
