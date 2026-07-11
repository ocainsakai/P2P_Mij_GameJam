using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Jam24
{
    public sealed class LoadingController : MonoBehaviour
    {
        [SerializeField] private Image progressFill;
        [SerializeField] private Text statusLabel;
        [SerializeField, Min(.1f)] private float minimumDisplayTime = .8f;

        public void Configure(Image fill, Text status)
        {
            progressFill = fill;
            statusLabel = status;
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
                float shown = Mathf.Min(loadProgress, timeProgress);
                if (progressFill != null) progressFill.fillAmount = shown;
                if (statusLabel != null) statusLabel.text = $"FOLLOWING THE TIDE...  {Mathf.RoundToInt(shown * 100f)}%";
                yield return null;
            }

            if (progressFill != null) progressFill.fillAmount = 1f;
            if (statusLabel != null) statusLabel.text = "READY!";
            yield return new WaitForSecondsRealtime(.12f);
            operation.allowSceneActivation = true;
        }
    }
}
