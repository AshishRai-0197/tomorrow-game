using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Tomorrow.EditorTools
{
    /// <summary>
    /// Lays out the whole Chapter 1 world from the Cozy Characters models and wires up the story director.
    /// Menu: Tomorrow ▸ Build Chapter 1 Scene. Safe to run again — it rebuilds the scene from scratch.
    /// Front of every model is +Z, so "facing the camera" means yaw 180.
    /// </summary>
    public static class Chapter1Builder
    {
        const string ScenePath = "Assets/Game/Scenes/Chapter1.unity";
        public const string MatDir = "Assets/Game/Materials";
        public const string PropDir = "Assets/Models/Props/";
        public const string CharDir = "Assets/Models/Characters/";

        public static Transform world, spots, interactables;

        [MenuItem("Tomorrow/Build Chapter 1 Scene")]
        public static void Build()
        {
            var scene = NewChapterScene<Chapter1Director>(out var dir);
            dir.nextScene = "Chapter2";

            BuildWorkshop(dir);
            BuildOutdoors(dir);
            BuildRiverAndDock(dir);
            BuildGreenhouse(dir);
            BuildBakeryAndTower(dir);
            BuildDecor();
            BuildTownDressing();

            AddPipAndTock(dir, new Vector3(-1.4f, 0.02f, 1.1f), -30f, new Vector3(-2f, 0.8f, 2.1f));
            Spot("Pip_Ending", new Vector3(-0.6f, 0.02f, -0.4f), 0);
            SaveChapterScene(scene, ScenePath);
        }

        /// <summary>New empty scene with sun, camera, post, UI and the chapter's director wired up.</summary>
        public static UnityEngine.SceneManagement.Scene NewChapterScene<T>(out T dir) where T : ChapterDirector
        {
            Directory.CreateDirectory(MatDir);
            Directory.CreateDirectory("Assets/Game/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            world = new GameObject("World").transform;
            spots = new GameObject("Spots").transform;
            interactables = new GameObject("Interactables").transform;

            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.75f;
            sunGo.transform.rotation = Quaternion.Euler(45, -30, 0);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.sun = sun;

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.fieldOfView = 38f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 250f;
            camGo.AddComponent<AudioListener>();
            var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            var follow = camGo.AddComponent<FollowCamera>();
            follow.distance = 5.2f; follow.pitch = 40f; follow.lookHeight = 0.45f;

            var volGo = new GameObject("Post Volume");
            var vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = PostProfile();

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var game = new GameObject("Game");
            var ui = game.AddComponent<GameUI>();
            var sfx = game.AddComponent<Sfx>();
            var clock = game.AddComponent<DayClock>();
            clock.sun = sun;
            dir = game.AddComponent<T>();
            dir.ui = ui; dir.sfx = sfx; dir.clock = clock; dir.cam = follow; dir.spots = spots;
            dir.tomorrowBoxPrefab = Load(PropDir + "Tomorrow_Box.glb");
            return scene;
        }

        public static void AddPipAndTock(ChapterDirector dir, Vector3 pipPos, float pipYaw, Vector3 tockPos)
        {
            var pipRoot = new GameObject("Pip");
            pipRoot.transform.position = pipPos;
            pipRoot.transform.rotation = Quaternion.Euler(0, pipYaw, 0);
            var cc = pipRoot.AddComponent<CharacterController>();
            cc.height = 0.8f; cc.radius = 0.2f; cc.center = new Vector3(0, 0.42f, 0); cc.stepOffset = 0.15f; cc.skinWidth = 0.02f;
            var pipModel = Spawn(CharDir + "Pip.glb", pipRoot.transform, Vector3.zero, 0);
            pipModel.transform.localPosition = Vector3.zero; pipModel.transform.localRotation = Quaternion.identity;
            var pc = pipRoot.AddComponent<PlayerController>();
            pc.visual = pipModel.transform;
            pipModel.AddComponent<LimbAnimator>().mover = pipRoot.transform;
            dir.player = pc;
            dir.cam.target = pipRoot.transform;

            var tockRoot = new GameObject("Tock");
            tockRoot.transform.position = tockPos;
            var tockModel = Spawn(CharDir + "Tock.glb", tockRoot.transform, Vector3.zero, 0);
            tockModel.transform.localPosition = Vector3.zero; tockModel.transform.localRotation = Quaternion.identity;
            var tcc = tockRoot.AddComponent<CharacterController>();
            tcc.height = 0.72f; tcc.radius = 0.16f; tcc.center = new Vector3(0, 0.38f, 0); tcc.stepOffset = 0.12f; tcc.skinWidth = 0.02f;
            tcc.enabled = false;
            var limbs = tockModel.AddComponent<LimbAnimator>();
            limbs.mover = tockRoot.transform; limbs.stride = 3.4f; limbs.legSwing = 42f;
            dir.tock = tockRoot.AddComponent<Companion>();
            dir.tock.visual = tockModel.transform;
            dir.tock.follow = pipRoot.transform;
        }

        public static void SaveChapterScene(UnityEngine.SceneManagement.Scene scene, string path)
        {
            EditorSceneManager.SaveScene(scene, path);
            AssetDatabase.SaveAssets();
            Debug.Log("[Tomorrow] Scene built: " + path);
        }

        // =====================================================================
        public static void BuildWorkshop(ChapterDirector dir)
        {
            var ws = Group("Workshop");
            var wood = Mat("Floor_Wood", new Color(0.55f, 0.38f, 0.24f));
            var wall = Mat("Wall_Plaster", new Color(0.93f, 0.85f, 0.72f));
            var trim = Mat("Wall_Trim", new Color(0.42f, 0.28f, 0.18f));

            Box("Floor", ws, new Vector3(0, -0.05f, 0), new Vector3(7.2f, 0.1f, 6.2f), wood, true);
            float h = 2.2f;
            // north wall with a door gap (x -0.5..0.5)
            Box("Wall_N_L", ws, new Vector3(-2.05f, h / 2, 3f), new Vector3(3.1f, h, 0.2f), wall, true);
            Box("Wall_N_R", ws, new Vector3(2.05f, h / 2, 3f), new Vector3(3.1f, h, 0.2f), wall, true);
            Box("Wall_N_Top", ws, new Vector3(0, h - 0.25f, 3f), new Vector3(1.0f, 0.5f, 0.2f), wall, false);
            Box("Wall_W", ws, new Vector3(-3.6f, h / 2, 0), new Vector3(0.2f, h, 6.2f), wall, true);
            Box("Wall_E", ws, new Vector3(3.6f, h / 2, 0), new Vector3(0.2f, h, 6.2f), wall, true);
            Box("Wall_S_Low", ws, new Vector3(0, 0.2f, -3.05f), new Vector3(7.4f, 0.4f, 0.2f), trim, true); // cut-away wall
            Box("Skirting", ws, new Vector3(0, 0.06f, 2.88f), new Vector3(7f, 0.12f, 0.04f), trim, false);

            var bench = Spawn(PropDir + "Workbench.glb", ws, new Vector3(-2f, 0, 2.35f), 180);
            Solid(bench);
            Use("Workbench", "Build at Grandma's workbench", new Vector3(-2f, 0, 1.75f), 1.1f);
            Spawn(PropDir + "Wall_Shelf_Clocks.glb", ws, new Vector3(-2.2f, 1.25f, 2.9f), 180);
            Spawn(PropDir + "Window.glb", ws, new Vector3(-3.5f, 0.8f, 0.2f), 90);
            var tw = Spawn(PropDir + "Tomorrow_Window.glb", ws, new Vector3(3.5f, 1.0f, 0.6f), -90);
            dir.tomorrowWindow = tw;
            Use("TomorrowWindow", "Look into the Tomorrow Window", new Vector3(3.0f, 0, 0.6f), 1.1f);
            Spawn(PropDir + "Due_Board.glb", ws, new Vector3(-3.5f, 0.9f, -1.6f), 90);
            Spawn(PropDir + "Rug.glb", ws, new Vector3(0, 0.005f, 0.2f), 0);
            Spawn(PropDir + "Hanging_Lamp.glb", ws, new Vector3(0.2f, h, 0.4f), 0);
            var lampLight = new GameObject("Lamp Light").AddComponent<Light>();
            lampLight.type = LightType.Point; lampLight.color = new Color(1f, 0.8f, 0.55f); lampLight.range = 5f; lampLight.intensity = 1.6f;
            lampLight.transform.SetParent(ws, false); lampLight.transform.position = new Vector3(0.2f, 1.5f, 0.4f);
            var rbox = Spawn(PropDir + "Return_Box.glb", ws, new Vector3(2.3f, 0, 2.2f), 180);
            Solid(rbox);
            Use("ReturnBox", "Drop it in the Return Box", new Vector3(2.3f, 0, 1.6f), 1.1f);
            Solid(Spawn(PropDir + "Barrel.glb", ws, new Vector3(3.0f, 0, -2.3f), 20));
            var crate = Spawn(PropDir + "Crate.glb", ws, new Vector3(-2.9f, 0, -2.2f), 10);
            Solid(crate);

            // Grandma's note pinned to the wall
            var paper = Mat("Paper", new Color(0.98f, 0.95f, 0.85f));
            var note = Box("Note", ws, new Vector3(-0.95f, 1.15f, 2.88f), new Vector3(0.28f, 0.34f, 0.01f), paper, false);
            note.transform.rotation = Quaternion.Euler(0, 0, 4);
            Use("Note", "Read the note", new Vector3(-0.95f, 0, 2.4f), 1.0f);

            // The Great Clock (stopped)
            var gc = new GameObject("Great Clock");
            gc.transform.SetParent(ws, false);
            gc.transform.position = new Vector3(1.35f, 1.35f, 2.86f);
            var brass = Mat("Brass", new Color(0.85f, 0.62f, 0.25f), 0.6f, 0.8f);
            var face = Mat("Clock_Face", new Color(0.98f, 0.94f, 0.82f));
            var ink = Mat("Clock_Ink", new Color(0.15f, 0.1f, 0.08f));
            var rim = Prim(PrimitiveType.Cylinder, "Rim", gc.transform, Vector3.zero, new Vector3(1.05f, 0.04f, 1.05f), brass);
            rim.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var dial = Prim(PrimitiveType.Cylinder, "Face", gc.transform, new Vector3(0, 0, -0.03f), new Vector3(0.92f, 0.02f, 0.92f), face);
            dial.transform.localRotation = Quaternion.Euler(90, 0, 0);
            for (int i = 0; i < 12; i++)
            {
                var a = i * 30f * Mathf.Deg2Rad;
                Prim(PrimitiveType.Cube, "Mark" + i, gc.transform, new Vector3(Mathf.Sin(a) * 0.38f, Mathf.Cos(a) * 0.38f, -0.06f), new Vector3(0.03f, 0.07f, 0.01f), ink)
                    .transform.localRotation = Quaternion.Euler(0, 0, -i * 30f);
            }
            var minute = new GameObject("Minute Hand").transform; minute.SetParent(gc.transform, false); minute.localPosition = new Vector3(0, 0, -0.07f);
            Prim(PrimitiveType.Cube, "Bar", minute, new Vector3(0, 0.17f, 0), new Vector3(0.025f, 0.36f, 0.01f), ink);
            var hour = new GameObject("Hour Hand").transform; hour.SetParent(gc.transform, false); hour.localPosition = new Vector3(0, 0, -0.075f);
            hour.localRotation = Quaternion.Euler(0, 0, -120);
            Prim(PrimitiveType.Cube, "Bar", hour, new Vector3(0, 0.11f, 0), new Vector3(0.04f, 0.24f, 0.01f), ink);
            Prim(PrimitiveType.Cylinder, "Pin", gc.transform, new Vector3(0, 0, -0.085f), new Vector3(0.06f, 0.01f, 0.06f), brass).transform.localRotation = Quaternion.Euler(90, 0, 0);
            var great = gc.AddComponent<GreatClock>();
            great.minuteHand = minute; great.hourHand = hour;
            dir.greatClock = great;

            // Door in the north gap: a hinge pivot so it swings open
            var hinge = new GameObject("Door Hinge").transform;
            hinge.SetParent(ws, false);
            hinge.position = new Vector3(-0.48f, 0, 2.95f);
            var door = Spawn(PropDir + "Door.glb", hinge, hinge.position, 180);
            var db = Visuals.WorldBounds(door);
            float doorW = db.size.x, doorH = db.size.y;
            float s = Mathf.Min(0.96f / Mathf.Max(0.01f, doorW), 1.75f / Mathf.Max(0.01f, doorH));
            door.transform.localScale = Vector3.one * s;
            db = Visuals.WorldBounds(door);
            door.transform.position += new Vector3(hinge.position.x - db.min.x, 0, 0);
            dir.workshopDoor = hinge;
            var blocker = Box("Door Blocker", ws, new Vector3(0, 1f, 3f), new Vector3(1f, 2f, 0.25f), null, true);
            dir.doorBlocker = blocker.GetComponent<Collider>();
            Use("Door", "Open the door", new Vector3(0, 0, 2.45f), 1.0f);

            // Story spots
            Spot("Drop_Workshop", new Vector3(0.1f, 0, 0.9f), 180);
            Spot("Drop_Final", new Vector3(0.2f, 0, 0.5f), 180);
            Spot("Tock_Drawer", new Vector3(-2f, 0.75f, 1.95f), 180);
            Spot("Tock_Land", new Vector3(-1.2f, 0.02f, 1.4f), 200);
            SpotLook("Cam_GreatClock", new Vector3(1.2f, 1.35f, 0.4f), new Vector3(1.35f, 1.35f, 2.86f));

            // Pickups for the crowbar, and tomorrow's wrench under the bench
            var iron = Mat("Iron", new Color(0.45f, 0.47f, 0.5f), 0.5f, 0.9f);
            var pipe = Pickup("Pickup_IronPipe", ItemId.IronPipe, new Vector3(-3.1f, 0, 0.9f));
            Prim(PrimitiveType.Cylinder, "Pipe", pipe.transform, new Vector3(0, 0.04f, 0), new Vector3(0.07f, 0.3f, 0.07f), iron).transform.localRotation = Quaternion.Euler(0, 30, 90);
            var cb = Visuals.WorldBounds(crate);
            var tape = Pickup("Pickup_RedTape", ItemId.RedTape, new Vector3(-2.9f, cb.max.y, -2.2f));
            Prim(PrimitiveType.Cylinder, "Tape", tape.transform, new Vector3(0, 0.03f, 0), new Vector3(0.14f, 0.03f, 0.14f), Mat("Red_Tape", new Color(0.85f, 0.15f, 0.12f)));
            tape.GetComponent<Interactable>().range = 1.3f;
            var wrench = Pickup("Pickup_Wrench", ItemId.GrandmasWrench, new Vector3(-2f, 0.01f, 2.0f));
            var wm = Spawn(PropDir + "Grandmas_Wrench.glb", wrench.transform, Vector3.zero, 70);
            wm.transform.localPosition = Vector3.zero; wm.transform.localRotation = Quaternion.Euler(0, 70, 0);
            wrench.GetComponent<Interactable>().range = 1.2f;
        }

        // =====================================================================
        public static void BuildOutdoors(ChapterDirector dir)
        {
            var o = Group("Outdoors");
            var grass = Mat("Grass", new Color(0.52f, 0.68f, 0.36f));
            var dirt = Mat("Path_Dirt", new Color(0.78f, 0.66f, 0.47f));
            var stone = Mat("Cobble", new Color(0.72f, 0.68f, 0.62f));
            Box("Ground_S", o, new Vector3(0, -0.06f, 1.375f), new Vector3(60f, 0.1f, 22.75f), grass, true);
            Box("Ground_N", o, new Vector3(0, -0.06f, 33.125f), new Vector3(60f, 0.1f, 33.75f), grass, true);
            Box("Path_Workshop", o, new Vector3(0, -0.005f, 7.5f), new Vector3(1.3f, 0.02f, 9f), dirt, false);
            Box("Path_Town", o, new Vector3(0, -0.005f, 21.5f), new Vector3(1.6f, 0.02f, 11f), stone, false);
            Box("Square", o, new Vector3(0, -0.004f, 24f), new Vector3(9f, 0.02f, 6f), stone, false);
            Box("Path_West", o, new Vector3(-5f, -0.005f, 23f), new Vector3(4f, 0.02f, 1.4f), stone, false);
            Box("Path_East", o, new Vector3(4.5f, -0.005f, 20f), new Vector3(1.4f, 0.02f, 6f), stone, false);

            // Workshop exterior shell (roof and outer walls seen from outside)
            var roof = Mat("Roof_Tiles", new Color(0.62f, 0.3f, 0.22f));
            var shell = Mat("Wall_Outer", new Color(0.86f, 0.74f, 0.58f));
            Spawn(PropDir + "Workshop_Front.glb", o, new Vector3(0, 0, 3.1f), 0);   // timber false-front, door gap matches the wall

            // Boundaries
            Box("Bound_W", o, new Vector3(-18f, 1f, 20f), new Vector3(0.5f, 3f, 50f), null, true);
            Box("Bound_E", o, new Vector3(18f, 1f, 20f), new Vector3(0.5f, 3f, 50f), null, true);
            Box("Bound_N", o, new Vector3(0, 1f, 42f), new Vector3(36f, 3f, 0.5f), null, true);
            Box("Bound_S", o, new Vector3(0, 1f, -4.2f), new Vector3(36f, 3f, 0.5f), null, true);

            // Town square lamps
            foreach (var p in new[] { new Vector3(-4f, 0, 21.5f), new Vector3(4f, 0, 21.5f), new Vector3(-4f, 0, 26.5f), new Vector3(4f, 0, 26.5f) })
            {
                var lamp = Spawn(PropDir + "Street_Lamp.glb", o, p, 180);
                Solid(lamp, 0.4f);
            }

            // Glow-flowers along every street — the early-return bonus
            var glow = Group("Town Glow Flowers");
            for (float z = 17.5f; z <= 29.5f; z += 2.4f)
            {
                Spawn(PropDir + "Flowers.glb", glow, new Vector3(-1.3f, 0, z), Random.Range(0, 360));
                Spawn(PropDir + "Flowers.glb", glow, new Vector3(1.3f, 0, z + 1.2f), Random.Range(0, 360));
            }
            for (float x = -6f; x <= 6f; x += 3f) Spawn(PropDir + "Flowers.glb", glow, new Vector3(x, 0, 27.2f), Random.Range(0, 360));
            Spawn(PropDir + "Flowers.glb", glow, new Vector3(0.9f, 0, 4f), 0);
            Spawn(PropDir + "Flowers.glb", glow, new Vector3(-0.9f, 0, 6f), 0);
            { var __v = glow.gameObject; if (dir is Chapter1Director c1) c1.townGlowFlowers = __v; }
            SpotLook("Cam_Town", new Vector3(0, 7f, 13.5f), new Vector3(0, 0, 25f));
        }

        // =====================================================================
        public static void BuildRiverAndDock(ChapterDirector dir)
        {
            var r = Group("River and Dock");
            var water = MatTransparent("Water", new Color(0.35f, 0.62f, 0.85f, 0.85f));
            var bank = Mat("River_Bank", new Color(0.62f, 0.55f, 0.42f));
            var plank = Mat("Plank", new Color(0.62f, 0.44f, 0.28f));
            Box("Water", r, new Vector3(0, -0.12f, 14.5f), new Vector3(60f, 0.05f, 3f), water, false);
            Box("Riverbed", r, new Vector3(0, -0.4f, 14.5f), new Vector3(60f, 0.1f, 3f), Mat("Riverbed", new Color(0.25f, 0.35f, 0.4f)), false);
            Box("Bank_S", r, new Vector3(0, -0.05f, 12.9f), new Vector3(60f, 0.12f, 0.3f), bank, false);
            Box("Bank_N", r, new Vector3(0, -0.05f, 16.1f), new Vector3(60f, 0.12f, 0.3f), bank, false);
            // Cut a hole in the ground over the river: two invisible walls either side of the bridge + one in the gap
            Box("River_Block_W", r, new Vector3(-15.4f, 1f, 14.5f), new Vector3(29.6f, 3f, 3f), null, true);
            Box("River_Block_E", r, new Vector3(15.4f, 1f, 14.5f), new Vector3(29.6f, 3f, 3f), null, true);
            { var __v = Box("River_Block_Gap", r, new Vector3(0, 1f, 14.5f), new Vector3(1.2f, 3f, 3f), null, true); if (dir is Chapter1Director c1) c1.riverBlocker = __v; }
            // the ground plane sits over the river; hide that strip with the water box (it renders above ground)

            // Broken bridge
            var broken = Group("Broken Bridge", r);
            Box("Stub_S", broken, new Vector3(0, 0.02f, 13.25f), new Vector3(1.3f, 0.08f, 0.7f), plank, false);
            Box("Stub_N", broken, new Vector3(0, 0.02f, 15.75f), new Vector3(1.3f, 0.08f, 0.7f), plank, false);
            Box("Snapped_1", broken, new Vector3(0.4f, -0.1f, 14.4f), new Vector3(0.25f, 0.05f, 1.0f), plank, false).transform.rotation = Quaternion.Euler(8, 25, 4);
            Box("Snapped_2", broken, new Vector3(-0.5f, -0.12f, 14.8f), new Vector3(0.25f, 0.05f, 0.9f), plank, false).transform.rotation = Quaternion.Euler(-6, -40, 0);
            { var __v = broken.gameObject; if (dir is Chapter1Director c1) c1.brokenBridge = __v; }

            // Fixed bridge (grows out along +Z from the south bank)
            var fixedRoot = new GameObject("Fixed Bridge").transform;
            fixedRoot.SetParent(r, false);
            fixedRoot.position = new Vector3(0, 0, 12.7f);
            for (int i = 0; i < 9; i++)
                Box("Deck_" + i, fixedRoot, new Vector3(0, 0.03f, 0.2f + i * 0.45f), new Vector3(1.25f, 0.07f, 0.38f), plank, true)
                    .transform.localRotation = Quaternion.Euler(0, Random.Range(-3f, 3f), 0);
            Box("Rail_L", fixedRoot, new Vector3(-0.62f, 0.35f, 1.9f), new Vector3(0.06f, 0.06f, 4f), plank, true);
            Box("Rail_R", fixedRoot, new Vector3(0.62f, 0.35f, 1.9f), new Vector3(0.06f, 0.06f, 4f), plank, true);
            for (int i = 0; i < 4; i++)
            {
                Box("Post_L" + i, fixedRoot, new Vector3(-0.62f, 0.17f, 0.2f + i * 1.15f), new Vector3(0.07f, 0.36f, 0.07f), plank, false);
                Box("Post_R" + i, fixedRoot, new Vector3(0.62f, 0.17f, 0.2f + i * 1.15f), new Vector3(0.07f, 0.36f, 0.07f), plank, false);
            }
            { var __v = fixedRoot.gameObject; if (dir is Chapter1Director c1) c1.fixedBridge = __v; }
            Use("BridgeGap", "Lay the bridge kit across", new Vector3(0, 0, 12.4f), 1.6f);
            Spot("Bridge_Near", new Vector3(0, 0, 11.2f), 0);
            Spot("Drop_Bridge", new Vector3(1.3f, 0, 11.0f), 180);
            SpotLook("Cam_Bridge", new Vector3(-1.5f, 2.6f, 9.0f), new Vector3(0.6f, 0.6f, 17f));

            // Oskar waits on the far bank, by his old boat and the dock
            { var __v = Character("Captain_Oskar", new Vector3(1.1f, 0, 17.4f), 200); if (dir is Chapter1Director c1) c1.oskar = __v; }
            Use("Oskar", "Talk to Captain Oskar", new Vector3(1.1f, 0, 17.4f), 1.2f);
            var boat = Spawn(PropDir + "Oskars_Sailboat.glb", r, new Vector3(7.9f, -0.15f, 14.5f), 90);
            boat.AddComponent<Idle>().bob = 0.02f;
            Spawn(PropDir + "Dock_Section.glb", r, new Vector3(5.2f, -0.02f, 15.2f), 0);   // over the water
            Solid(Spawn(PropDir + "Barrels.glb", r, new Vector3(8.2f, 0, 17.6f), 30));
            var dockCrate = Spawn(PropDir + "Crate.glb", r, new Vector3(3.2f, 0, 17.9f), -15);
            Solid(dockCrate);
            Solid(Spawn(PropDir + "Harbor_House.glb", r, new Vector3(11f, 0, 20f), 200));
            Solid(Spawn(PropDir + "Lighthouse.glb", r, new Vector3(15f, 0, 17.5f), 180));
            var gull = Spawn(PropDir + "Seagull.glb", r, new Vector3(6f, 3.2f, 15f), 120);
            var gi = gull.AddComponent<Idle>(); gi.bob = 0.15f; gi.speed = 1.2f;
            Spawn(PropDir + "Rope_Coil.glb", r, new Vector3(7.3f, 0.02f, 16.9f), 0);

            for (int i = 0; i < 3; i++)
            {
                var pk = Pickup("Pickup_Plank" + (i + 1), ItemId.Plank, new Vector3(4.0f + i * 1.0f, 0.12f, 16.75f + (i % 2) * 0.15f));
                Box("Plank", pk.transform, new Vector3(0, 0.03f, 0), new Vector3(0.18f, 0.05f, 0.75f), plank, false).transform.localRotation = Quaternion.Euler(0, 70 + i * 12, 0);
            }
            Spot("Drop_Dock", new Vector3(6.8f, 0.12f, 17.2f), 180);

            var cb = Visuals.WorldBounds(dockCrate);
            var can = Pickup("Pickup_TinCan", ItemId.TinCan, new Vector3(3.2f, cb.max.y, 17.9f));
            Prim(PrimitiveType.Cylinder, "Can", can.transform, new Vector3(0, 0.06f, 0), new Vector3(0.1f, 0.06f, 0.1f), Mat("Tin", new Color(0.75f, 0.77f, 0.8f), 0.6f, 0.9f));
            can.GetComponent<Interactable>().range = 1.3f;

            // Interest plank (late penalty) lies by the bridge
            var ip = Pickup("Pickup_InterestPlank", ItemId.Plank, new Vector3(-1.6f, 0.02f, 11.6f));
            Box("Plank", ip.transform, new Vector3(0, 0.03f, 0), new Vector3(0.18f, 0.05f, 0.75f), plank, false).transform.localRotation = Quaternion.Euler(0, 40, 0);
        }

        // =====================================================================
        public static void BuildGreenhouse(ChapterDirector dir)
        {
            var g = Group("Greenhouse");
            var c = new Vector3(-8.5f, 0, 22.5f);
            var glass = MatTransparent("Glass", new Color(0.8f, 0.93f, 1f, 0.22f));
            var frame = Mat("Greenhouse_Frame", new Color(0.95f, 0.95f, 0.92f));
            float w = 3.6f, d = 3.0f, h = 1.9f;
            Box("Floor", g, c + new Vector3(0, 0.0f, 0), new Vector3(w, 0.04f, d), Mat("Greenhouse_Tiles", new Color(0.7f, 0.55f, 0.45f)), false);
            Box("Glass_N", g, c + new Vector3(0, h / 2, d / 2), new Vector3(w, h, 0.04f), glass, true);
            Box("Glass_W", g, c + new Vector3(-w / 2, h / 2, 0), new Vector3(0.04f, h, d), glass, true);
            Box("Glass_E", g, c + new Vector3(w / 2, h / 2, 0), new Vector3(0.04f, h, d), glass, true);
            Box("Glass_S_L", g, c + new Vector3(-1.2f, h / 2, -d / 2), new Vector3(1.2f, h, 0.04f), glass, true);
            Box("Glass_S_R", g, c + new Vector3(1.2f, h / 2, -d / 2), new Vector3(1.2f, h, 0.04f), glass, true);
            var roofL = Box("Roof_L", g, c + new Vector3(-w / 4, h + 0.35f, 0), new Vector3(w / 2 + 0.2f, 0.04f, d + 0.1f), glass, false);
            roofL.transform.rotation = Quaternion.Euler(0, 0, 20);
            var roofR = Box("Roof_R", g, c + new Vector3(w / 4, h + 0.35f, 0), new Vector3(w / 2 + 0.2f, 0.04f, d + 0.1f), glass, false);
            roofR.transform.rotation = Quaternion.Euler(0, 0, -20);
            foreach (var x in new[] { -w / 2, -0.6f, 0.6f, w / 2 })
                foreach (var z in new[] { -d / 2, d / 2 })
                    Box("Post", g, c + new Vector3(x, h / 2, z), new Vector3(0.06f, h, 0.06f), frame, false);
            Box("Beam", g, c + new Vector3(0, h + 0.7f, 0), new Vector3(0.06f, 0.06f, d), frame, false);

            var flowers = Group("Greenhouse Flowers", g);
            var bed = Mat("Planter", new Color(0.48f, 0.32f, 0.22f));
            Box("Bed_N", flowers, c + new Vector3(0, 0.12f, 1.0f), new Vector3(3f, 0.24f, 0.6f), bed, true);
            Box("Bed_W", flowers, c + new Vector3(-1.4f, 0.12f, -0.2f), new Vector3(0.6f, 0.24f, 1.6f), bed, true);
            foreach (var p in new[] { new Vector3(-1f, 0.24f, 1f), new Vector3(0f, 0.24f, 1f), new Vector3(1f, 0.24f, 1f), new Vector3(-1.4f, 0.24f, 0.3f), new Vector3(-1.4f, 0.24f, -0.6f) })
                Spawn(PropDir + "Flowers.glb", flowers, c + p, Random.Range(0, 360));
            { var __v = flowers.gameObject; if (dir is Chapter1Director c1) c1.greenhouseFlowers = __v; }

            var heater = Spawn(PropDir + "Heater.glb", g, c + new Vector3(1.3f, 0.02f, -0.45f), 200);
            { var __v = heater; if (dir is Chapter1Director c1) c1.greenhouseHeater = __v; }
            Use("HeaterSpot", "Set the heater down", c + new Vector3(0.9f, 0, -0.6f), 1.1f);

            { var __v = Character("Nell", c + new Vector3(0.3f, 0.02f, 0.1f), 180); if (dir is Chapter1Director c1) c1.nell = __v; }
            Use("Nell", "Talk to Nell", c + new Vector3(0.3f, 0, 0.1f), 1.2f);
            Spot("Drop_Greenhouse", c + new Vector3(2.6f, 0, -2.2f), 180);
            Spawn(PropDir + "Nells_Watering_Can.glb", g, c + new Vector3(0.35f, 0.02f, -1.05f), 40);
        }

        // =====================================================================
        public static void BuildBakeryAndTower(ChapterDirector dir)
        {
            var b = Group("Bakery");
            var house = Spawn(PropDir + "Rooftop_House.glb", b, new Vector3(0, 0, 31.5f), 180);
            FitHeight(house, 3.4f);
            Solid(house);
            var awning = Mat("Awning", new Color(0.9f, 0.45f, 0.35f));
            Spawn(PropDir + "Striped_Awning.glb", b, new Vector3(0, 0, 29.1f), 180);
            var counter = Box("Counter", b, new Vector3(-1.0f, 0.3f, 29.4f), new Vector3(1.3f, 0.6f, 0.5f), Mat("Counter", new Color(0.55f, 0.36f, 0.22f)), true);
            var basket = Prim(PrimitiveType.Cylinder, "Empty Bread Basket", b, new Vector3(-1.3f, 0.65f, 29.4f), new Vector3(0.35f, 0.05f, 0.25f), Mat("Wicker", new Color(0.78f, 0.6f, 0.35f)));
            var oven = Spawn(PropDir + "Extra_Oven.glb", b, new Vector3(1.0f, 0, 29.6f), 180);
            Solid(oven);
            var ol = new GameObject("Oven Light").AddComponent<Light>();
            ol.type = LightType.Point; ol.color = new Color(1f, 0.55f, 0.2f); ol.range = 3.5f; ol.intensity = 3f;
            ol.transform.SetParent(b, false); ol.transform.position = new Vector3(1.0f, 0.5f, 29.0f);
            { var __v = ol; if (dir is Chapter1Director c1) c1.ovenLight = __v; }
            Use("Oven", "Put the battery in the oven", new Vector3(1.0f, 0, 28.9f), 1.0f);
            { var __v = b.gameObject; if (dir is Chapter1Director c1) c1.bakery = __v; }

            { var __v = Character("Bram", new Vector3(-0.2f, 0.02f, 28.8f), 180); if (dir is Chapter1Director c1) c1.bram = __v; }
            Use("Bram", "Talk to Bram", new Vector3(-0.2f, 0, 28.8f), 1.2f);
            Spot("Drop_Bakery", new Vector3(0.4f, 0, 27.0f), 180);
            SpotLook("Cam_Bakery", new Vector3(-0.4f, 1.3f, 26.4f), new Vector3(-0.2f, 0.45f, 28.8f));

            var jar = Pickup("Pickup_JamJar", ItemId.JamJar, new Vector3(-0.7f, 0.6f, 29.4f));
            Prim(PrimitiveType.Cylinder, "Jar", jar.transform, new Vector3(0, 0.06f, 0), new Vector3(0.1f, 0.06f, 0.1f), MatTransparent("Jar_Glass", new Color(0.85f, 0.95f, 1f, 0.5f)));
            jar.GetComponent<Interactable>().range = 1.2f;

            // Flour cloud and the grey spot (late penalty)
            var flour = Group("Flour Cloud", b);
            var flourMat = MatTransparent("Flour", new Color(1f, 1f, 0.97f, 0.7f));
            for (int i = 0; i < 9; i++)
                Prim(PrimitiveType.Sphere, "Puff", flour, new Vector3(-0.2f + Random.Range(-0.3f, 0.3f), 0.85f + Random.Range(-0.1f, 0.25f), 28.55f + Random.Range(-0.2f, 0.1f)), Vector3.one * Random.Range(0.12f, 0.25f), flourMat);
            { var __v = flour.gameObject; if (dir is Chapter1Director c1) c1.flourCloud = __v; }
            var spot = Prim(PrimitiveType.Cylinder, "Grey Spot", b, new Vector3(0, 0.01f, 29.5f), new Vector3(7f, 0.005f, 7f), MatTransparent("Grey_Spot", new Color(0.5f, 0.5f, 0.52f, 0.55f)));
            { var __v = spot; if (dir is Chapter1Director c1) c1.greySpot = __v; }

            // Clock tower
            var t = Group("Clock Tower");
            var stone = Mat("Tower_Stone", new Color(0.78f, 0.72f, 0.64f));
            var roof = Mat("Roof_Tiles", new Color(0.62f, 0.3f, 0.22f));
            var tc = new Vector3(8f, 0, 30f);
            Spawn(PropDir + "Clock_Tower.glb", t, tc, 180);                         // stone tower, clocks on four sides, open belfry
            Box("Tower Collider", t, tc + new Vector3(0, 3f, 0), new Vector3(2.1f, 6f, 2.1f), null, true);
            var lamp = Spawn(PropDir + "Lantern.glb", t, tc + new Vector3(0, 5.42f, 0), 180);   // stands in the belfry
            FitHeight(lamp, 0.7f);
            { var __v = lamp; if (dir is Chapter1Director c1) c1.towerLamp = __v; }
            var tl = new GameObject("Tower Light").AddComponent<Light>();
            tl.type = LightType.Point; tl.color = new Color(1f, 0.85f, 0.5f); tl.range = 16f; tl.intensity = 6f;
            tl.transform.SetParent(t, false); tl.transform.position = tc + new Vector3(0, 6.0f, 0);
            { var __v = tl; if (dir is Chapter1Director c1) c1.towerLight = __v; }
            Use("TowerLight", "Power the clock tower light", tc + new Vector3(0, 0, -1.7f), 1.2f);
            SpotLook("Cam_Tower", new Vector3(4.5f, 2.6f, 23.5f), tc + new Vector3(0, 4.5f, 0));

            // Post office, with copper wire behind it (the long way)
            var po = Group("Post Office");
            var poHouse = Spawn(PropDir + "Harbor_House.glb", po, new Vector3(-8.5f, 0, 31f), 180);
            FitHeight(poHouse, 2.8f);
            Solid(poHouse);
            Box("Post Sign", po, new Vector3(-8.5f, 1.9f, 29.3f), new Vector3(1.2f, 0.3f, 0.05f), Mat("Post_Red", new Color(0.8f, 0.2f, 0.18f)), false);
            var wire = Pickup("Pickup_CopperWire", ItemId.CopperWire, new Vector3(-8.5f, 0.02f, 33.6f));
            Prim(PrimitiveType.Cylinder, "Coil", wire.transform, new Vector3(0, 0.05f, 0), new Vector3(0.22f, 0.05f, 0.22f), Mat("Copper", new Color(0.85f, 0.45f, 0.25f), 0.6f, 0.9f));
        }

        // =====================================================================
        public static void BuildDecor()
        {
            var d = Group("Decor");
            var trees = new[]
            {
                new Vector3(-5, 0, 6), new Vector3(5.5f, 0, 7), new Vector3(-7, 0, 10), new Vector3(8, 0, 9), new Vector3(-12, 0, 5),
                new Vector3(12, 0, 4), new Vector3(-14, 0, 18), new Vector3(-13, 0, 27), new Vector3(14, 0, 26), new Vector3(13, 0, 33),
                new Vector3(-15, 0, 36), new Vector3(7, 0, 38.5f), new Vector3(-4, 0, 38.5f), new Vector3(-5.3f, 0, -1), new Vector3(4.6f, 0, 1.5f),
            };
            foreach (var p in trees) Solid(Spawn(PropDir + "Pine_Tree.glb", d, p, Random.Range(0, 360)), 0.35f);
            foreach (var p in new[] { new Vector3(-6, 0, 3), new Vector3(7, 0, 12), new Vector3(-10, 0, 15), new Vector3(10, 0, 27) })
                Solid(Spawn(PropDir + "Rocks.glb", d, p, Random.Range(0, 360)), 0.6f);
            var hill = Spawn(PropDir + "Hill.glb", d, new Vector3(-12, 0, 49), 180); FitHeight(hill, 3f);
            var hill2 = Spawn(PropDir + "Hill.glb", d, new Vector3(12, 0, 49), 160); FitHeight(hill2, 2.4f);
            var vane = Spawn(PropDir + "Weather_Vane_Tower.glb", d, new Vector3(-12, 0, 49.5f), 180); FitHeight(vane, 3.2f);
            var hb = Visuals.WorldBounds(hill);
            vane.transform.position = new Vector3(-12, hb.max.y * 0.85f, 49.5f);
            Spawn(PropDir + "Stone_Steps.glb", d, new Vector3(-12, 0, 45.2f), 180);
            var kite = Spawn(PropDir + "Kite.glb", d, new Vector3(-6, 6, 38), 160);
            var ki = kite.AddComponent<Idle>(); ki.bob = 0.4f; ki.speed = 0.8f;
            var moon = Spawn(PropDir + "Moon.glb", d, new Vector3(-14, 14, 46), 160); FitHeight(moon, 3f);
            Spawn(PropDir + "Rope_Line.glb", d, new Vector3(-4.5f, 0, 30.5f), 180);
        }


        // =====================================================================
        public static void BuildTownDressing()
        {
            var t = Group("Town Dressing");
            var stone = Mat("Fountain_Stone", new Color(0.8f, 0.76f, 0.7f));
            var water = MatTransparent("Fountain_Water", new Color(0.45f, 0.75f, 0.95f, 0.8f));
            var wood = Mat("Bench_Wood", new Color(0.6f, 0.4f, 0.25f));
            var iron = Mat("Bench_Iron", new Color(0.2f, 0.2f, 0.22f));

            // Fountain
            var f = new Vector3(-2.7f, 0, 24.6f);
            Spawn(PropDir + "Fountain.glb", t, f, 0);
            var fc = Box("Fountain Collider", t, f + new Vector3(0, 0.5f, 0), new Vector3(1.6f, 1f, 1.6f), null, true);

            // Benches
            foreach (var (pos, yaw) in new[] { (new Vector3(2.6f, 0, 25.6f), 180f), (new Vector3(2.6f, 0, 22.4f), 0f) })
            {
                var b = new GameObject("Bench").transform;
                b.SetParent(t, false); b.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
                var bm = Spawn(PropDir + "Park_Bench.glb", b, pos, yaw + 180);
                bm.transform.localPosition = Vector3.zero; bm.transform.localRotation = Quaternion.Euler(0, 180, 0);
                Box("Seat", b, new Vector3(0, 0.22f, 0), new Vector3(1.1f, 0.3f, 0.36f), null, true);
            }

            // Market corner
            Solid(Spawn(PropDir + "Barrels.glb", t, new Vector3(5.7f, 0, 24.2f), -30));
            Solid(Spawn(PropDir + "Crate.glb", t, new Vector3(-4.8f, 0, 22.9f), 15));
            Solid(Spawn(PropDir + "Barrel.glb", t, new Vector3(-3.3f, 0, 21.6f), 0));
            Solid(Spawn(PropDir + "Crate.glb", t, new Vector3(9.5f, 0, 18.4f), 40));

            // Neighbours' houses around the square
            void House(string model, Vector3 pos, float yaw, float h)
            {
                var go = Spawn(PropDir + model, t, pos, yaw);
                FitHeight(go, h);
                Solid(go);
            }
            House("Rooftop_House.glb", new Vector3(-4.6f, 0, 34.5f), 170, 3.0f);
            House("Rooftop_House.glb", new Vector3(4.2f, 0, 35.5f), 195, 3.2f);
            House("Harbor_House.glb", new Vector3(-13f, 0, 23f), 100, 2.6f);
            House("Rooftop_House.glb", new Vector3(13.5f, 0, 31f), 250, 3.0f);
            House("Harbor_House.glb", new Vector3(-12.5f, 0, 31.5f), 120, 2.4f);

            // Flower pots by the door of the workshop and the bakery
            foreach (var p in new[] { new Vector3(-1.1f, 0, 3.6f), new Vector3(1.1f, 0, 3.6f), new Vector3(-2.1f, 0, 28.6f), new Vector3(2.1f, 0, 28.6f) })
            {
                Prim(PrimitiveType.Cylinder, "Pot", t, p + new Vector3(0, 0.12f, 0), new Vector3(0.3f, 0.12f, 0.3f), Mat("Terracotta", new Color(0.78f, 0.42f, 0.28f)));
                Spawn(PropDir + "Flowers.glb", t, p + new Vector3(0, 0.24f, 0), Random.Range(0, 360));
            }
        }

        // =====================================================================
        // helpers
        public static Transform Group(string name, Transform parent = null)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent ? parent : world, false);
            return t;
        }

        public static GameObject Load(string path)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!go) Debug.LogWarning("[Tomorrow] Missing model: " + path);
            return go;
        }

        public static GameObject Spawn(string path, Transform parent, Vector3 pos, float yaw)
        {
            var src = Load(path);
            GameObject go = src ? (GameObject)PrefabUtility.InstantiatePrefab(src) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(parent, true);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            return go;
        }

        public static GameObject Character(string name, Vector3 pos, float yaw)
        {
            var root = new GameObject(name).transform;
            root.SetParent(world, false);
            root.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            var m = Spawn(CharDir + name + ".glb", root, pos, yaw);
            var idle = root.gameObject.AddComponent<Idle>();
            idle.bob = 0.01f; idle.facePlayer = true;
            var col = root.gameObject.AddComponent<CapsuleCollider>();
            col.height = 0.8f; col.radius = 0.22f; col.center = new Vector3(0, 0.4f, 0);
            return root.gameObject;
        }

        public static void FitHeight(GameObject go, float height)
        {
            var b = Visuals.WorldBounds(go);
            if (b.size.y < 0.001f) return;
            go.transform.localScale *= height / b.size.y;
        }

        public static void Solid(GameObject go, float shrink = 0.9f)
        {
            var b = Visuals.WorldBounds(go);
            var col = go.AddComponent<BoxCollider>();
            // axis-aligned in world → approximate in local space (models are only yawed)
            col.center = go.transform.InverseTransformPoint(b.center);
            var size = go.transform.InverseTransformVector(b.size);
            size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
            col.size = new Vector3(size.x * shrink, size.y, size.z * shrink);
        }

        public static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 size, Material mat, bool collide)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            if (mat) go.GetComponent<Renderer>().sharedMaterial = mat;
            else Object.DestroyImmediate(go.GetComponent<Renderer>());
            if (!collide) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        public static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        public static Interactable Use(string id, string prompt, Vector3 pos, float range)
        {
            var go = new GameObject("Use_" + id);
            go.transform.SetParent(interactables, false);
            go.transform.position = pos;
            var it = go.AddComponent<Interactable>();
            it.id = id; it.prompt = prompt; it.range = range;
            return it;
        }

        public static GameObject Pickup(string name, ItemId item, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(interactables, false);
            go.transform.position = pos;
            var it = go.AddComponent<Interactable>();
            it.id = name; it.pickupItem = item; it.range = 1.0f;
            var idle = go.AddComponent<Idle>(); idle.bob = 0.02f; idle.speed = 3f;
            // a soft sparkle so pickups read from the follow camera
            var l = new GameObject("Sparkle").AddComponent<Light>();
            l.type = LightType.Point; l.range = 0.9f; l.intensity = 1.2f; l.color = new Color(1f, 0.9f, 0.6f);
            l.transform.SetParent(go.transform, false); l.transform.localPosition = Vector3.up * 0.3f;
            // a spinning gold diamond above it, so the part is easy to find in daylight
            var marker = new GameObject("Beacon").transform;
            marker.SetParent(go.transform, false); marker.localPosition = Vector3.up * 0.95f;
            var gem = Prim(PrimitiveType.Cube, "Diamond", marker, Vector3.zero, new Vector3(0.16f, 0.16f, 0.16f), BeaconMat());
            gem.transform.localRotation = Quaternion.Euler(45f, 0, 45f);
            gem.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            marker.gameObject.AddComponent<Beacon>();
            return go;
        }

        public static void Spot(string name, Vector3 pos, float yaw)
        {
            var t = new GameObject(name).transform;
            t.SetParent(spots, false);
            t.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
        }

        public static void SpotLook(string name, Vector3 pos, Vector3 lookAt)
        {
            var t = new GameObject(name).transform;
            t.SetParent(spots, false);
            t.SetPositionAndRotation(pos, Quaternion.LookRotation(lookAt - pos));
        }

        static Material BeaconMat()
        {
            var m = Mat("Pickup_Beacon", new Color(1f, 0.78f, 0.25f), 0.6f, 0.2f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", new Color(1f, 0.72f, 0.2f) * 2.2f);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(m);
            return m;
        }

        public static Material Mat(string name, Color c, float smooth = 0.25f, float metal = 0f)
        {
            var path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", metal);
            EditorUtility.SetDirty(m);
            return m;
        }

        public static Material MatTransparent(string name, Color c)
        {
            var m = Mat(name, c, 0.85f);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>The game's warm, cozy colour grade (shared by every scene). Rebuilt each time scenes are built.</summary>
        static bool postBuiltThisRun;
        [InitializeOnLoadMethod] static void ResetPost() => postBuiltThisRun = false;

        public static VolumeProfile PostProfile()
        {
            const string path = "Assets/Game/Settings/Tomorrow_Warm.asset";
            Directory.CreateDirectory("Assets/Game/Settings");
            var p = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (p && postBuiltThisRun) return p;
            if (p) AssetDatabase.DeleteAsset(path);
            postBuiltThisRun = true;
            p = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(p, path);
            var bloom = p.Add<Bloom>(true); bloom.intensity.Override(0.8f); bloom.threshold.Override(0.92f); bloom.scatter.Override(0.7f);
            bloom.tint.Override(new Color(1f, 0.86f, 0.7f));
            var tone = p.Add<Tonemapping>(true); tone.mode.Override(TonemappingMode.Neutral);
            var wb = p.Add<WhiteBalance>(true); wb.temperature.Override(14f); wb.tint.Override(3f);
            var ca = p.Add<ColorAdjustments>(true); ca.saturation.Override(14f); ca.contrast.Override(8f); ca.postExposure.Override(0.2f);
            ca.colorFilter.Override(new Color(1f, 0.97f, 0.92f));
            var st = p.Add<SplitToning>(true); st.shadows.Override(new Color(0.42f, 0.36f, 0.55f)); st.highlights.Override(new Color(1f, 0.82f, 0.6f)); st.balance.Override(-10f);
            var vig = p.Add<Vignette>(true); vig.intensity.Override(0.26f); vig.smoothness.Override(0.45f); vig.color.Override(new Color(0.22f, 0.1f, 0.04f));
            foreach (var comp in p.components) AssetDatabase.AddObjectToAsset(comp, p);
            EditorUtility.SetDirty(p);
            AssetDatabase.SaveAssets();
            return p;
        }
    }
}
