using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using K = Tomorrow.EditorTools.Chapter1Builder;

namespace Tomorrow.EditorTools
{
    /// <summary>Title scene + "Build All" (title and the three chapters, in build order).</summary>
    public static class TitleBuilder
    {
        const string ScenePath = "Assets/Game/Scenes/Title.unity";

        [MenuItem("Tomorrow/Build Title Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            K.world = new GameObject("World").transform;
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional; sun.shadows = LightShadows.Soft; sun.color = new Color(1f, 0.8f, 0.6f);
            sun.transform.rotation = Quaternion.Euler(30, -40, 0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.5f, 0.6f);
            var cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.36f, 0.22f, 0.24f);
            cam.transform.SetPositionAndRotation(new Vector3(0, 1.1f, -3.2f), Quaternion.Euler(8, 0, 0));
            cam.gameObject.AddComponent<AudioListener>();
            cam.gameObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            var vol = new GameObject("Post Volume").AddComponent<UnityEngine.Rendering.Volume>();
            vol.isGlobal = true; vol.sharedProfile = K.PostProfile();
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            // a little scene behind the menu
            K.Box("Floor", K.world, new Vector3(0, -0.05f, 0), new Vector3(8, 0.1f, 6), K.Mat("Floor_Wood", new Color(0.55f, 0.38f, 0.24f)), false);
            var pip = K.Spawn(K.CharDir + "Pip.glb", K.world, new Vector3(-1.9f, 0, -0.2f), 200);
            var tock = K.Spawn(K.CharDir + "Tock.glb", K.world, new Vector3(1.9f, 0, -0.2f), 160);
            tock.AddComponent<Idle>().bob = 0.02f;
            var box = K.Spawn(K.PropDir + "Tomorrow_Box.glb", K.world, new Vector3(2.6f, 0, 1.4f), 200);
            var glow = new GameObject("Box Glow").AddComponent<Light>();
            glow.type = LightType.Point; glow.color = new Color(0.45f, 0.75f, 1f); glow.range = 3f; glow.intensity = 3f;
            glow.transform.position = new Vector3(2.6f, 0.6f, 1.2f);
            K.Spawn(K.PropDir + "Wall_Shelf_Clocks.glb", K.world, new Vector3(-2.6f, 0.95f, 2.2f), 180);

            new GameObject("Title Menu").AddComponent<TitleMenu>();
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        [MenuItem("Tomorrow/Build All Scenes")]
        public static void BuildAll()
        {
            Chapter1Builder.Build();
            Chapter2Builder.Build();
            Chapter3Builder.Build();
            Build();
            EditorBuildSettings.scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene("Assets/Game/Scenes/Chapter1.unity", true),
                new EditorBuildSettingsScene("Assets/Game/Scenes/Chapter2.unity", true),
                new EditorBuildSettingsScene("Assets/Game/Scenes/Chapter3.unity", true),
            }.ToArray();
            EditorSceneManager.OpenScene(ScenePath);
            Debug.Log("[Tomorrow] All scenes built: Title, Chapter1, Chapter2, Chapter3");
        }
    }
}
