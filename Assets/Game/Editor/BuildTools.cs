using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Tomorrow.EditorTools
{
    /// <summary>
    /// One-click test builds. Output goes to &lt;project&gt;/Builds/.
    ///   Tomorrow ▸ Build ▸ Web (WebGL)  → Builds/Web/index.html   (works on any static host, no server config)
    ///   Tomorrow ▸ Build ▸ Android APK   → Builds/Android/Tomorrow.apk
    /// </summary>
    public static class BuildTools
    {
        static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        static string[] Scenes => EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

        static void Common()
        {
            PlayerSettings.companyName = "Cozy Characters";
            PlayerSettings.productName = "Tomorrow";
            PlayerSettings.bundleVersion = "0.3.0";
            PlayerSettings.runInBackground = false;
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Game/Art/Icon.png");
            if (icon) PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            PlayerSettings.SplashScreen.backgroundColor = new Color(0.96f, 0.78f, 0.48f);
        }

        // ------------------------------------------------------------------ icons
        const string IconPath = "Assets/Game/Art/Icon.png";
        const string IconDir = "Assets/Game/Art/Icons";

        /// <summary>Read Icon.png straight from disk (no need to make the asset readable).</summary>
        static Texture2D LoadIconPixels()
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(Path.Combine(Root, IconPath)));
            return tex;
        }

        /// <summary>
        /// A new square picture of <paramref name="size"/> pixels: the icon shrunk to <paramref name="scale"/>,
        /// centred, cut to a circle, over a solid <paramref name="fill"/> colour (clear = transparent).
        /// </summary>
        static Texture2D Compose(Texture2D src, int size, float scale, Color fill, bool circle)
        {
            var dst = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color[size * size];
            float c = (size - 1) / 2f, r = size * scale / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x - c, dy = y - c;
                    // where this pixel lands in the original picture (0..1)
                    float u = dx / (size * scale) + 0.5f, v = dy / (size * scale) + 0.5f;
                    bool inside = circle ? dx * dx + dy * dy <= r * r : u >= 0 && u <= 1 && v >= 0 && v <= 1;
                    if (!inside) { px[y * size + x] = fill; continue; }
                    var col = src.GetPixelBilinear(u, v);
                    // soften the circle's edge by one pixel so it isn't jagged
                    if (circle) col.a *= Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy));
                    px[y * size + x] = Color.Lerp(fill, col, col.a);
                }
            dst.SetPixels(px); dst.Apply();
            return dst;
        }

        static Texture2D SaveIcon(Texture2D tex, string name)
        {
            Directory.CreateDirectory(Path.Combine(Root, IconDir));
            var path = $"{IconDir}/{name}.png";
            File.WriteAllBytes(Path.Combine(Root, path), tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>
        /// Android 8+ phones show an "adaptive" icon: a background layer and a foreground layer that the
        /// phone cuts into a circle or rounded square, hiding the outer third. If these slots are empty the
        /// phone shows the Unity icon. (Unity 6.6 dropped the old "round" and "legacy" slots: adaptive covers all phones.)
        /// </summary>
        [MenuItem("Tomorrow/Build/Make App Icons")]
        public static void MakeIcons()
        {
            var src = LoadIconPixels();
            var orange = src.GetPixel(4, 4);                                         // the icon's outer colour
            // The phone only shows the middle two thirds of the layer. Shrink the icon to 78% so that window
            // shows the clock and the box; the space around it is filled with the icon's own orange.
            var bg = SaveIcon(Compose(src, 432, 0.78f, orange, false), "Adaptive_Background");
            var fg = SaveIcon(Compose(src, 432, 0f, Color.clear, false), "Adaptive_Foreground");   // empty layer

            var android = UnityEditor.Build.NamedBuildTarget.Android;
            void Fill(PlatformIconKind kind, params Texture2D[] layers)
            {
                var icons = PlayerSettings.GetPlatformIcons(android, kind);
                foreach (var icon in icons) icon.SetTextures(layers);
                PlayerSettings.SetPlatformIcons(android, kind, icons);
            }
            Fill(AndroidPlatformIconKind.Adaptive, bg, fg);   // layer 0 = background, layer 1 = foreground
            AssetDatabase.SaveAssets();
            Debug.Log("[Tomorrow] App icon set (adaptive: background + foreground)");
        }

        /// <summary>Swap Unity's favicon and loading logo in a finished web build for the game icon.</summary>
        static void BrandWebBuild(string outDir)
        {
            var src = LoadIconPixels();
            var td = Path.Combine(outDir, "TemplateData");
            File.WriteAllBytes(Path.Combine(td, "favicon.png"), Compose(src, 64, 1f, Color.clear, true).EncodeToPNG());
            File.WriteAllBytes(Path.Combine(td, "apple-touch-icon.png"), Compose(src, 180, 1f, Color.white, false).EncodeToPNG());
            File.WriteAllBytes(Path.Combine(td, "unity-logo-dark.png"), Compose(src, 154, 1f, Color.clear, true).EncodeToPNG());
            File.WriteAllBytes(Path.Combine(td, "unity-logo-light.png"), Compose(src, 154, 1f, Color.clear, true).EncodeToPNG());
            File.WriteAllBytes(Path.Combine(td, "unity-logo-title-footer.png"), Compose(src, 38, 1f, Color.clear, true).EncodeToPNG());
            var index = Path.Combine(outDir, "index.html");
            var html = File.ReadAllText(index)
                .Replace("<title>Unity Web Player | ", "<title>")
                .Replace("<link rel=\"shortcut icon\" href=\"TemplateData/favicon.ico\">",
                         "<link rel=\"icon\" type=\"image/png\" href=\"TemplateData/favicon.png\">\n    <link rel=\"apple-touch-icon\" href=\"TemplateData/apple-touch-icon.png\">");
            File.WriteAllText(index, html);
            File.Delete(Path.Combine(td, "favicon.ico"));
        }

        [MenuItem("Tomorrow/Build/Web (WebGL)")]
        public static void BuildWeb()
        {
            Common();
            // gzip + decompression fallback: the page unpacks itself, so it runs on any plain static host
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.nameFilesAsHashes = false;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            var outDir = Path.Combine(Root, "Builds/Web");
            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
            var report = BuildPipeline.BuildPlayer(Scenes, outDir, BuildTarget.WebGL, BuildOptions.None);
            if (report.summary.result == BuildResult.Succeeded) BrandWebBuild(outDir);
            Report(report, "Web");
        }

        [MenuItem("Tomorrow/Build/Android APK")]
        public static void BuildAndroid()
        {
            Common();
            MakeIcons();
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.cozycharacters.tomorrow");
            PlayerSettings.Android.bundleVersionCode = 3;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            EditorUserBuildSettings.buildAppBundle = false;   // a plain .apk for side-loading
            var outDir = Path.Combine(Root, "Builds/Android");
            Directory.CreateDirectory(outDir);
            Report(BuildPipeline.BuildPlayer(Scenes, Path.Combine(outDir, "Tomorrow.apk"), BuildTarget.Android, BuildOptions.None), "Android");
        }

        static void Report(BuildReport r, string what)
        {
            var s = r.summary;
            Debug.Log($"[Tomorrow] {what} build: {s.result}, {s.totalSize / (1024f * 1024f):F1} MB, {s.totalTime.TotalSeconds:F0}s, errors={s.totalErrors} → {s.outputPath}");
        }
    }
}
