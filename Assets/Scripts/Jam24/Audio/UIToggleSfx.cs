using UnityEngine;
using UnityEngine.UI;

namespace Jam24
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Toggle))]
    public sealed class UIToggleSfx : MonoBehaviour
    {
        private Toggle toggle;

        private void Awake()
        {
            toggle = GetComponent<Toggle>();
            toggle.onValueChanged.AddListener(HandleChanged);
        }

        private void OnDestroy()
        {
            if (toggle != null) toggle.onValueChanged.RemoveListener(HandleChanged);
        }

        private static void HandleChanged(bool enabled)
        {
            JamAudioManager.Play(enabled ? UiSfxType.ToggleOn : UiSfxType.ToggleOff);
        }
    }
}
