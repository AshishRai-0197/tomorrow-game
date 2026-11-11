using System.IO;
using System.Linq;
using UnityEditor;
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
            Report(BuildPipeline.BuildPlayer(Scenes, outDir, BuildTarget.WebGL, BuildOptions.None), "Web");
        }

        [MenuItem("Tomorrow/Build/Android APK")]
        public static void BuildAndroid()
        {
            Common();
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
