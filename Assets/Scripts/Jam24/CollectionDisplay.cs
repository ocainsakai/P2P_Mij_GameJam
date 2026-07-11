using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Jam24
{
    public sealed class CollectionDisplay : MonoBehaviour
    {
        [SerializeField] private Text content;

        public void Configure(Text label) => content = label;

        private void Start()
        {
            if (content == null) return;
            var text = new StringBuilder();
            int collected = 0;
            for (int i = 0; i < SoleLevelCatalog.Count; i++)
            {
                string slipper = SaveData.CollectedSlipper(i);
                if (string.IsNullOrEmpty(slipper)) continue;
                collected++;
                text.Append("• ").Append(slipper).Append('\n');
            }
            content.text = collected == 0 ? "No slippers yet.\nOcto's shelf is waiting!" : $"{collected}/10 COLLECTED\n\n{text}";
        }
    }
}
