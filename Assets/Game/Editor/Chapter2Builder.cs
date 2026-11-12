using UnityEditor;
using UnityEngine;
using K = Tomorrow.EditorTools.Chapter1Builder;

namespace Tomorrow.EditorTools
{
    /// <summary>
    /// Chapter 2 scene: Grandma's workshop (opening) plus the harbor to the east — a dock into the sea,
    /// Oskar's boat, two fishing boats, the Sunday Promise, the lighthouse, and a little island.
    /// Menu: Tomorrow ▸ Build Chapter 2 Scene.
    /// </summary>
    public static class Chapter2Builder
    {
        const string ScenePath = "Assets/Game/Scenes/Chapter2.unity";
        const string P = K.PropDir;

        [MenuItem("Tomorrow/Build Chapter 2 Scene")]
        public static void Build()
        {
            var scene = K.NewChapterScene<Chapter2Director>(out var dir);
            dir.nextScene = "Chapter3";

            // ---------------- the workshop, morning
            K.BuildWorkshop(dir);
            var ws = GameObject.Find("Workshop").transform;
            var grassOut = K.Mat("Grass", new Color(0.52f, 0.68f, 0.36f));
            K.Box("Yard", ws, new Vector3(0, -0.06f, 8f), new Vector3(16f, 0.1f, 10f), grassOut, true);
            dir.dueBoard = GameObject.Find("Due_Board");
            K.SpotLook("Cam_DueBoard", new Vector3(-1.2f, 1.4f, -0.6f), new Vector3(-3.5f, 1.1f, -1.6f));
            var note = K.Box("Door Note", ws, new Vector3(0.1f, 0.012f, 3.4f), new Vector3(0.24f, 0.01f, 0.3f), K.Mat("Paper", new Color(0.98f, 0.95f, 0.85f)), false);
            dir.doorNote = note;
            K.Spot("DoorNote_End", new Vector3(0.15f, 0.012f, 2.55f), 0);
            K.Use("DoorNote", "Read Oskar's note", new Vector3(0.15f, 0, 2.3f), 1.0f);
            K.Spot("Tock_Nose", new Vector3(-1.55f, 0.02f, 1.35f), 230);

            BuildHarbor(dir);
            BuildIsland(dir);

            // Pip starts asleep at the workbench
            K.AddPipAndTock(dir, new Vector3(-2f, 0.02f, 1.55f), 0f, new Vector3(-1.5f, 0.02f, 1.3f));
            K.SaveChapterScene(scene, ScenePath);
        }

        static void BuildHarbor(Chapter2Director dir)
        {
            var h = K.Group("Harbor");
            var sand = K.Mat("Sand", new Color(0.88f, 0.8f, 0.62f));
            var grass = K.Mat("Grass", new Color(0.52f, 0.68f, 0.36f));
            var plank = K.Mat("Plank", new Color(0.62f, 0.44f, 0.28f));
            var post = K.Mat("Dock_Post", new Color(0.4f, 0.28f, 0.18f));
            var sea = K.MatTransparent("Sea", new Color(0.3f, 0.58f, 0.82f, 0.88f));

            // land, beach, sea
            K.Box("Harbor Grass", h, new Vector3(54f, -0.06f, 0f), new Vector3(20f, 0.1f, 30f), grass, true);
            K.Box("Beach", h, new Vector3(67f, -0.07f, 0f), new Vector3(6f, 0.1f, 30f), sand, true);
            K.Box("Sea", h, new Vector3(150f, -0.15f, 10f), new Vector3(160f, 0.05f, 300f), sea, false);
            K.Box("Sea Bed", h, new Vector3(150f, -0.9f, 10f), new Vector3(160f, 0.1f, 300f), K.Mat("Sea_Bed", new Color(0.18f, 0.3f, 0.38f)), false);
            // walls: sea edge (with a gap for the dock), and the land's edges
            K.Box("Sea Wall N", h, new Vector3(70.3f, 1f, 8.1f), new Vector3(0.4f, 3f, 13.8f), null, true);
            K.Box("Sea Wall S", h, new Vector3(70.3f, 1f, -8.1f), new Vector3(0.4f, 3f, 13.8f), null, true);
            K.Box("Bound W", h, new Vector3(44f, 1f, 0f), new Vector3(0.4f, 3f, 30f), null, true);
            K.Box("Bound N", h, new Vector3(57f, 1f, 15f), new Vector3(27f, 3f, 0.4f), null, true);
            K.Box("Bound S", h, new Vector3(57f, 1f, -15f), new Vector3(27f, 3f, 0.4f), null, true);

            // the dock: x 70..81 along z = 0
            K.Box("Dock Deck", h, new Vector3(75.5f, 0.0f, 0f), new Vector3(11f, 0.1f, 2.2f), plank, true);
            for (float x = 70.6f; x <= 81f; x += 2.2f)
                foreach (var z in new[] { -1.05f, 1.05f })
                    K.Box("Dock Post", h, new Vector3(x, -0.25f, z), new Vector3(0.16f, 0.9f, 0.16f), post, false);
            K.Box("Dock Rail N", h, new Vector3(75.5f, 1f, 1.25f), new Vector3(11f, 3f, 0.3f), null, true);
            K.Box("Dock Rail S", h, new Vector3(75.5f, 1f, -1.25f), new Vector3(11f, 3f, 0.3f), null, true);
            K.Box("Dock End", h, new Vector3(81.15f, 1f, 0f), new Vector3(0.3f, 3f, 2.8f), null, true);
            K.Spawn(P + "Dock_Section.glb", h, new Vector3(70.8f, 0.02f, 0f), 90);

            // Oskar and his working boat (it drifts until the anchor is back)
            dir.oskar = K.Character("Captain_Oskar", new Vector3(71.2f, 0.05f, 0.6f), 250);
            K.Use("Oskar", "Talk to Captain Oskar", new Vector3(71.2f, 0, 0.6f), 1.3f).transform.SetParent(dir.oskar.transform, true);
            K.Spot("Oskar_Dock", new Vector3(71.2f, 0.05f, 0.6f), 250);
            var work = K.Spawn(P + "Oskars_Sailboat.glb", h, new Vector3(79f, -0.18f, 2.7f), 90);
            work.name = "Oskars Boat";
            work.transform.localScale = Vector3.one * 2.2f;
            work.AddComponent<Idle>().bob = 0.03f;
            dir.oskarsBoat = work.transform;
            K.Use("AnchorSpot", "Lower the magnet on a rope", new Vector3(80.2f, 0, 0.6f), 1.2f);
            K.SpotLook("Cam_Anchor", new Vector3(77.5f, 2.2f, -2.5f), new Vector3(80.4f, -0.2f, 1.5f));
            var line = new GameObject("Magnet On Line").transform; line.SetParent(h, false); line.position = new Vector3(80.4f, 0.9f, 1.5f);
            K.Prim(PrimitiveType.Cylinder, "Rope", line, new Vector3(0, 0.6f, 0), new Vector3(0.02f, 0.6f, 0.02f), K.Mat("Rope", new Color(0.8f, 0.68f, 0.45f)));
            var mag = K.Spawn(P + "Magnet.glb", line, line.position, 0);
            K.FitHeight(mag, 0.3f);
            dir.magnetOnLine = line.gameObject;
            var anchor = new GameObject("Anchor").transform; anchor.SetParent(h, false); anchor.position = new Vector3(80.4f, -0.6f, 1.5f);
            var iron = K.Mat("Iron", new Color(0.45f, 0.47f, 0.5f), 0.5f, 0.9f);
            K.Prim(PrimitiveType.Cylinder, "Shank", anchor, new Vector3(0, 0, 0), new Vector3(0.06f, 0.22f, 0.06f), iron);
            K.Prim(PrimitiveType.Cube, "Arms", anchor, new Vector3(0, -0.2f, 0), new Vector3(0.45f, 0.06f, 0.06f), iron);
            K.Prim(PrimitiveType.Cube, "Stock", anchor, new Vector3(0, 0.2f, 0), new Vector3(0.3f, 0.04f, 0.04f), iron);
            dir.anchor = anchor.gameObject;

            // two fishing boats on the south side of the dock
            var a = K.Spawn(P + "Oskars_Sailboat.glb", h, new Vector3(74f, -0.18f, -2.8f), 90);
            a.name = "Fishing Boat A"; a.transform.localScale = Vector3.one * 1.8f; a.AddComponent<Idle>().bob = 0.03f;
            var b = K.Spawn(P + "Oskars_Sailboat.glb", h, new Vector3(78f, -0.18f, -2.8f), 90);
            b.name = "Fishing Boat B"; b.transform.localScale = Vector3.one * 1.8f; b.AddComponent<Idle>().bob = 0.03f;
            dir.boatA = a.transform; dir.boatB = b.transform;
            K.Use("BoatA", "Tie this boat to the dock", new Vector3(74f, 0, -0.7f), 1.2f);
            K.Use("BoatB", "Tie this boat to the dock", new Vector3(78f, 0, -0.7f), 1.2f);
            var loop = new GameObject("Rope Loop").transform; loop.SetParent(h, false);
            var rope = K.Mat("Rope", new Color(0.8f, 0.68f, 0.45f));
            K.Box("Rope 1", loop, new Vector3(76f, 0.25f, -1.15f), new Vector3(4.2f, 0.04f, 0.04f), rope, false);
            K.Box("Rope 2", loop, new Vector3(74f, 0.2f, -1.9f), new Vector3(0.04f, 0.04f, 1.6f), rope, false);
            K.Box("Rope 3", loop, new Vector3(78f, 0.2f, -1.9f), new Vector3(0.04f, 0.04f, 1.6f), rope, false);
            dir.ropeLoop = loop.gameObject;

            // the Sunday Promise, Oskar's old boat, on the north side by the shore
            var sp = K.Spawn(P + "Oskars_Sailboat.glb", h, new Vector3(73.8f, -0.3f, 2.9f), 90);
            sp.name = "Sunday Promise";
            sp.transform.localScale = Vector3.one * 2.6f;   // big enough for Pip, Oskar and Tock to sit in
            var spb = Visuals.WorldBounds(sp);
            float deckY = spb.min.y + spb.size.y * 0.2f;
            dir.sundayPromise = sp.transform;
            K.Spot("Boat_Harbor", sp.transform.position, 90);
            K.Use("SundayEngine", "Fit the engine part", new Vector3(73.8f, 0, 0.7f), 1.2f);
            K.Spot("Deck_Pip", new Vector3(spb.center.x - 0.6f, deckY, spb.center.z), 160);
            K.Spot("Deck_Oskar", new Vector3(spb.center.x + 0.6f, deckY, spb.center.z), 200);
            K.Spot("Deck_Tock", new Vector3(spb.min.x + 0.15f, deckY + 0.05f, spb.center.z - 0.2f), 180);
            K.SpotLook("Cam_Deck", new Vector3(spb.center.x - 0.4f, deckY + 1.1f, spb.min.z - 1.9f), new Vector3(spb.center.x, deckY + 0.45f, spb.center.z));
            K.Spot("Cam_Sail", new Vector3(68f, 4f, -4f), 45);
            var fb = K.Spawn(P + "Tomorrow_Box.glb", h, new Vector3(77f, -0.1f, 5.5f), 30);
            fb.transform.localScale = Vector3.one * 0.5f;
            fb.name = "Floating Box";
            dir.floatingBox = fb.transform;
            K.Spot("FloatBox_End", new Vector3(74.4f, -0.1f, 3.9f), 0);

            // harbor bench, Return Box, crates, bench, houses
            var bench = K.Spawn(P + "Workbench.glb", h, new Vector3(60f, 0, 3f), 180);
            K.Solid(bench);
            K.Use("Workbench_Harbor", "Build at Oskar's harbor bench", new Vector3(60f, 0, 2.3f), 1.2f);
            var rb = K.Spawn(P + "Return_Box.glb", h, new Vector3(62.6f, 0, 3f), 180);
            K.Solid(rb);
            K.Use("ReturnBox_Harbor", "Drop it in the Return Box", new Vector3(62.6f, 0, 2.3f), 1.2f);
            K.Spot("Interest_Spot", new Vector3(61.3f, 0, 1.8f), 0);
            K.Solid(K.Spawn(P + "Harbor_House.glb", h, new Vector3(53f, 0, 8f), 160));
            K.Solid(K.Spawn(P + "Harbor_House.glb", h, new Vector3(50f, 0, -6f), 60));
            K.Solid(K.Spawn(P + "Barrels.glb", h, new Vector3(67.5f, 0, 4.5f), 20));
            K.Solid(K.Spawn(P + "Crate.glb", h, new Vector3(64.5f, 0, 6.2f), 10));
            K.Solid(K.Spawn(P + "Crate.glb", h, new Vector3(57.5f, 0, -8.5f), 35));
            var lamp = K.Spawn(P + "Street_Lamp.glb", h, new Vector3(68.5f, 0, -1.6f), 180); K.Solid(lamp, 0.4f);
            K.Solid(K.Spawn(P + "Pine_Tree.glb", h, new Vector3(47f, 0, 11f), 0), 0.35f);
            K.Solid(K.Spawn(P + "Pine_Tree.glb", h, new Vector3(46.5f, 0, -11f), 90), 0.35f);
            K.Solid(K.Spawn(P + "Rocks.glb", h, new Vector3(68f, 0, 10f), 0), 0.6f);
            K.Solid(K.Spawn(P + "Rocks.glb", h, new Vector3(68.5f, 0, -12f), 140), 0.6f);
            // a harbor bench to sit on (the jam jar hides under it)
            var benchWood = K.Mat("Bench_Wood", new Color(0.6f, 0.4f, 0.25f));
            var hb = K.Spawn(P + "Park_Bench.glb", h, new Vector3(58f, 0, -3.2f), 0);
            hb.transform.localScale = new Vector3(1.18f, 1.05f, 1.05f);
            K.Box("Harbor Bench Seat", h, new Vector3(58f, 0.2f, -3.2f), new Vector3(1.3f, 0.3f, 0.4f), null, true);

            // lighthouse (the seagull's hideout)
            var lh = K.Spawn(P + "Lighthouse.glb", h, new Vector3(64f, 0, -10.5f), 200);
            K.FitHeight(lh, 6f);
            K.Solid(lh, 0.8f);
            var lb = Visuals.WorldBounds(lh);
            K.Use("Lighthouse", "Climb the lighthouse stairs", new Vector3(64f, 0, lb.min.z - 0.5f), 1.4f);
            K.Spot("Gull_Perch", new Vector3(64f, lb.max.y + 0.1f, -10.5f), 0);
            K.Spot("Gull_Freeze", new Vector3(64.5f, lb.max.y * 0.7f, lb.min.z - 1.2f), 0);
            K.SpotLook("Cam_LighthouseTop", new Vector3(61.5f, lb.max.y + 1.2f, -7.5f), new Vector3(64f, lb.max.y, -10.5f));
            K.SpotLook("Cam_GullFreeze", new Vector3(62.5f, lb.max.y * 0.6f, -4.5f), new Vector3(64.5f, lb.max.y * 0.7f, lb.min.z - 1.2f));
            var gull = K.Spawn(P + "Seagull.glb", h, new Vector3(66f, 3f, -2f), 120);
            gull.name = "Thief Gull";
            var gi = gull.AddComponent<Idle>(); gi.bob = 0.15f; gi.speed = 1.3f;
            dir.gull = gull.transform;
            K.SpotLook("Cam_Sea", new Vector3(69f, 2.2f, -1.5f), new Vector3(95f, 0f, 8f));

            // spots
            K.Spot("Harbor_Arrive", new Vector3(55f, 0.02f, 0f), 90);
            K.Spot("Harbor_FromSea", new Vector3(72f, 0.05f, 0.2f), 270);
            K.Spot("Drop_Dock", new Vector3(72.6f, 0.05f, -0.4f), 180);
            K.Spot("Drop_Harbor", new Vector3(61.3f, 0.0f, -0.6f), 180);

            // pickups: three short ropes (puzzle 2)
            var coilA = K.Pickup("Pickup_ShortRope1", ItemId.ShortRope, new Vector3(69f, 0, 2.6f));
            K.Spawn(P + "Rope_Coil.glb", coilA.transform, coilA.transform.position, 0);
            var coilB = K.Pickup("Pickup_ShortRope2", ItemId.ShortRope, new Vector3(63f, 0, -5.5f));
            K.Spawn(P + "Rope_Coil.glb", coilB.transform, coilB.transform.position, 0);
            var coilC = K.Pickup("Pickup_ShortRope3", ItemId.ShortRope, new Vector3(55.5f, 0, 3.2f));
            K.Spawn(P + "Rope_Coil.glb", coilC.transform, coilC.transform.position, 0);

            // pickups hidden in the fog (puzzle 3) — only the lantern shows them
            var hidden = K.Group("Fog Hidden Parts", h);
            var jar = K.Pickup("Pickup_FogJamJar", ItemId.JamJar, new Vector3(59.4f, 0.02f, -2.2f));
            jar.transform.SetParent(hidden, true);
            K.Prim(PrimitiveType.Cylinder, "Jar", jar.transform, new Vector3(0, 0.06f, 0), new Vector3(0.1f, 0.06f, 0.1f), K.MatTransparent("Jar_Glass", new Color(0.85f, 0.95f, 1f, 0.5f)));
            var crateTop = Visuals.WorldBounds(GameObject.Find("Harbor").transform.Find("Crate").gameObject).max.y;
            var flower = K.Pickup("Pickup_FogGlowFlower", ItemId.GlowFlower, new Vector3(64.5f, crateTop, 6.2f));
            flower.transform.SetParent(hidden, true);
            var fm = K.Spawn(P + "Flowers.glb", flower.transform, flower.transform.position, 0); K.FitHeight(fm, 0.25f);
            var wire = K.Pickup("Pickup_FogWire", ItemId.CopperWire, new Vector3(67.6f, 0.02f, -4f));
            wire.transform.SetParent(hidden, true);
            K.Prim(PrimitiveType.Cylinder, "Coil", wire.transform, new Vector3(0, 0.05f, 0), new Vector3(0.22f, 0.05f, 0.22f), K.Mat("Copper", new Color(0.85f, 0.45f, 0.25f), 0.6f, 0.9f));
            dir.fogHiddenParts = hidden.gameObject;
            K.Spot("Fog_1", new Vector3(58.6f, 0.02f, -2.2f), 0);
            K.Spot("Fog_2", new Vector3(64.5f, 0.02f, 5f), 0);
            K.Spot("Fog_3", new Vector3(67f, 0.02f, -3.2f), 0);

            // pickups dropped by the seagull (puzzle 4) + a second stick on the beach
            var stickMat = K.Mat("Stick", new Color(0.55f, 0.38f, 0.22f));
            var s1 = K.Pickup("Pickup_String", ItemId.String, new Vector3(63f, 0.02f, lb.min.z - 1.1f));
            K.Prim(PrimitiveType.Cylinder, "Ball", s1.transform, new Vector3(0, 0.05f, 0), new Vector3(0.1f, 0.05f, 0.1f), K.Mat("String", new Color(0.95f, 0.9f, 0.8f)));
            var s2 = K.Pickup("Pickup_Stick1", ItemId.Stick, new Vector3(64.2f, 0.02f, lb.min.z - 1.4f));
            K.Prim(PrimitiveType.Cylinder, "Stick", s2.transform, new Vector3(0, 0.03f, 0), new Vector3(0.03f, 0.25f, 0.03f), stickMat).transform.localRotation = Quaternion.Euler(0, 20, 90);
            var s3 = K.Pickup("Pickup_Button", ItemId.Button, new Vector3(65.2f, 0.02f, lb.min.z - 1.1f));
            K.Prim(PrimitiveType.Cylinder, "Button", s3.transform, new Vector3(0, 0.02f, 0), new Vector3(0.08f, 0.01f, 0.08f), K.Mat("Brass", new Color(0.85f, 0.62f, 0.25f), 0.6f, 0.8f));
            var s4 = K.Pickup("Pickup_Stick2", ItemId.Stick, new Vector3(67.2f, 0.0f, -7.5f));
            K.Prim(PrimitiveType.Cylinder, "Driftwood", s4.transform, new Vector3(0, 0.03f, 0), new Vector3(0.035f, 0.3f, 0.035f), stickMat).transform.localRotation = Quaternion.Euler(0, 70, 90);
        }

        static void BuildIsland(Chapter2Director dir)
        {
            var i = K.Group("Island");
            var c = new Vector3(100f, 0, 22f);
            var sand = K.Mat("Sand", new Color(0.88f, 0.8f, 0.62f));
            K.Prim(PrimitiveType.Cylinder, "Island Sand", i, c + new Vector3(0, -0.08f, 0), new Vector3(11f, 0.1f, 11f), sand)
                .AddComponent<MeshCollider>();
            var hill = K.Spawn(P + "Hill.glb", i, c + new Vector3(1.5f, 0, 2.5f), 0);
            K.FitHeight(hill, 1.4f);
            K.Solid(K.Spawn(P + "Pine_Tree.glb", i, c + new Vector3(-2f, 0, 2.5f), 0), 0.35f);
            // walls round the island (an octagon of invisible boxes)
            for (int k = 0; k < 8; k++)
            {
                float ang = k * 45f;
                var dirv = Quaternion.Euler(0, ang, 0) * Vector3.forward;
                var wall = K.Box("Shore", i, c + dirv * 5.2f + Vector3.up, new Vector3(4.6f, 3f, 0.3f), null, true);
                wall.transform.rotation = Quaternion.Euler(0, ang, 0);
            }
            // Grandma's toolbox (doubles as a workbench)
            var tb = new GameObject("Grandma's Toolbox").transform; tb.SetParent(i, false); tb.position = c + new Vector3(0.8f, 0, 0.6f);
            K.Box("Box", tb, new Vector3(0, 0.15f, 0), new Vector3(0.6f, 0.3f, 0.35f), K.Mat("Toolbox_Red", new Color(0.78f, 0.2f, 0.16f)), true);
            K.Box("Handle", tb, new Vector3(0, 0.34f, 0), new Vector3(0.3f, 0.04f, 0.04f), K.Mat("Iron", new Color(0.45f, 0.47f, 0.5f), 0.5f, 0.9f), false);
            K.Use("Toolbox", "Open Grandma's old toolbox", c + new Vector3(0.8f, 0, 0.1f), 1.2f);
            K.Use("Workbench_Island", "Build with Grandma's tools", c + new Vector3(0.8f, 0, 1.25f), 1.0f);
            K.Use("SailBack", "Sail back to the harbor", c + new Vector3(-3.6f, 0, -2.6f), 1.5f);
            K.Spot("Boat_Island", c + new Vector3(-6f, -0.3f, -4.4f), 45);
            K.Spot("Island_Arrive", c + new Vector3(-3f, 0.05f, -2f), 45);
            K.Spot("Oskar_Island", c + new Vector3(-2.6f, 0.05f, -2.8f), 45);
        }
    }
}
