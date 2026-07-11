#if UNITY_EDITOR
using UnityEditor;

namespace Jam24.Editor
{
    public sealed class SoleFlowArtImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (assetPath != "Assets/Resources/SoleFlow/soleflow_sheet.png") return;
            var importer = (TextureImporter)assetImporter;
            importer.isReadable = true;
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
        }
    }
}
#endif
