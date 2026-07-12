using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Jam24
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class UIButtonSfx : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private UiSfxType sfxType = UiSfxType.Click;
        [SerializeField] private AudioClip overrideClip;
        [SerializeField, Range(0f, 1f)] private float volumeScale = 1f;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            if (sfxType == UiSfxType.Click) sfxType = ResolveType(name);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (button == null || !button.IsActive() || !button.interactable) return;
            if (overrideClip != null) JamAudioManager.PlayClip(overrideClip, volumeScale);
            else JamAudioManager.Play(sfxType);
        }

        private static UiSfxType ResolveType(string objectName)
        {
            string value = objectName.ToLowerInvariant();
            if (value.Contains("back") || value.Contains("close") || value.Contains("home") ||
                value.Contains("exit") || value.Contains("cancel")) return UiSfxType.Back;
            if (value.Contains("play") || value.Contains("start") || value.Contains("next"))
                return UiSfxType.Play;
            if (value.Contains("ok") || value.Contains("confirm") || value.Contains("retry") ||
                value.Contains("select")) return UiSfxType.Confirm;
            return UiSfxType.Click;
        }
    }
}
