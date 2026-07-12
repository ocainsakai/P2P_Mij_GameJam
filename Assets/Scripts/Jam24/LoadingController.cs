using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Jam24
{
    public sealed class LoadingController : MonoBehaviour
    {
        [SerializeField] private Image progressFill;
        [SerializeField, Min(.1f)] private float minimumDisplayTime = 2f;

        private RectTransform progressRect;

        private void Awake()
        {
            CacheProgressRect();
            SetProgress(0f);
        }

        public void Configure(Image fill)
        {
            progressFill = fill;
            CacheProgressRect();
            SetProgress(0f);
        }

        private IEnumerator Start()
        {
            string target = GameFlow.PendingScene;
            if (string.IsNullOrEmpty(target) || target == GameFlow.LoadingScene) target = GameFlow.HomeScene;
            Debug.Log($"Beach Flow Loading: async loading '{target}'.", this);

            float startedAt = Time.realtimeSinceStartup;
            AsyncOperation operation = SceneManager.LoadSceneAsync(target);
            operation.allowSceneActivation = false;

            while (operation.progress < .9f || Time.realtimeSinceStartup - startedAt < minimumDisplayTime)
            {
                float loadProgress = Mathf.Clamp01(operation.progress / .9f);
                float timeProgress = Mathf.Clamp01((Time.realtimeSinceStartup - startedAt) / minimumDisplayTime);
                float shown = Mathf.SmoothStep(0f, 1f, Mathf.Min(loadProgress, timeProgress));
                SetProgress(shown);
                yield return null;
            }

            SetProgress(1f);
            yield return new WaitForSecondsRealtime(.12f);
            operation.allowSceneActivation = true;
        }

        private void CacheProgressRect()
        {
            progressRect = progressFill != null ? progressFill.rectTransform : null;
        }

        private void SetProgress(float value)
        {
            if (progressFill == null) return;
            if (progressRect == null) CacheProgressRect();

            progressFill.fillAmount = 1f;
            Vector2 anchorMax = progressRect.anchorMax;
            anchorMax.x = Mathf.Clamp01(value);
            progressRect.anchorMax = anchorMax;
        }
    }
}
