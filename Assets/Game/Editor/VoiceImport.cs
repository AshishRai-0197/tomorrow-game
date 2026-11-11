using UnityEditor;
using UnityEngine;

namespace Tomorrow.EditorTools
{
    /// <summary>Voice lines and music import small: mono Vorbis (voices in memory, music streamed).</summary>
    public class VoiceImport : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            var imp = (AudioImporter)assetImporter;
            if (assetPath.Contains("/Resources/Music/"))
            {
                // music: streamed from disk, so it costs almost no memory
                imp.forceToMono = true;
                var m = imp.defaultSampleSettings;
                m.loadType = AudioClipLoadType.Streaming;
                m.compressionFormat = AudioCompressionFormat.Vorbis;
                m.quality = 0.45f;
                imp.defaultSampleSettings = m;
                return;
            }
            if (!assetPath.Contains("/Resources/Voice/")) return;
            imp.forceToMono = true;
            imp.loadInBackground = true;
            var s = imp.defaultSampleSettings;
            s.loadType = AudioClipLoadType.CompressedInMemory;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.75f;
            imp.defaultSampleSettings = s;
        }
    }
}

namespace Tomorrow.EditorTools
{
    /// <summary>Logo / UI art: crisp, keeps transparency, no mipmaps.</summary>
    public class UiArtImport : UnityEditor.AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Contains("/Resources/UI/") && !assetPath.Contains("/Game/Art/")) return;
            var imp = (UnityEditor.TextureImporter)assetImporter;
            imp.textureType = UnityEditor.TextureImporterType.Default;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.npotScale = UnityEditor.TextureImporterNPOTScale.None;
            imp.maxTextureSize = 2048;
            imp.wrapMode = UnityEngine.TextureWrapMode.Clamp;
        }
    }
}
