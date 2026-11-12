using UnityEditor;
using UnityEngine;
using K = Tomorrow.EditorTools.Chapter1Builder;

namespace Tomorrow.EditorTools
{
    /// <summary>
    /// Chapter 3 scene: the same town as Chapter 1 (bridge now fixed) plus Mira's market stall,
    /// Nell's flower bed, Grandma's rose bush, the cart, the mud, the cracked greenhouse roof and the frozen corner.
    /// Menu: Tomorrow ▸ Build Chapter 3 Scene.
    /// </summary>
    public static class Chapter3Builder
    {
        const string ScenePath = "Assets/Game/Scenes/Chapter3.unity";
        const string P = K.PropDir;
        static readonly Vector3 GH = new Vector3(-8.5f, 0, 22.5f);   // greenhouse centre (matches Chapter1Builder)

        [MenuItem("Tomorrow/Build Chapter 3 Scene")]
        public static void Build()
        {
            var scene = K.NewChapterScene<Chapter3Director>(out var dir);
            dir.nextScene = "Title";

            K.BuildWorkshop(dir);
            K.BuildOutdoors(dir);
            K.BuildRiverAndDock(dir);
            K.BuildGreenhouse(dir);
            K.BuildBakeryAndTower(dir);
            K.BuildDecor();
            K.BuildTownDressing();
            // make room on the square for the market
            foreach (var n in new[] { "Bench", "Bench" }) { var b = GameObject.Find(n); if (b) Object.DestroyImmediate(b); }
            var barrels = GameObject.Find("World/Town Dressing/Barrels"); if (barrels) Object.DestroyImmediate(barrels);

            dir.nell = GameObject.Find("Nell");
            // Nell walks around this chapter, so her "talk" point travels with her
            GameObject.Find("Use_Nell").transform.SetParent(dir.nell.transform, true);
            BuildMarket(dir);
            BuildGardens(dir);
            BuildGreenhouseExtras(dir);

            K.AddPipAndTock(dir, new Vector3(0f, 0.02f, 19.5f), 0f, new Vector3(0.7f, 0.02f, 19.2f));
            K.SaveChapterScene(scene, ScenePath);
        }

        static void BuildMarket(Chapter3Director dir)
        {
            var m = K.Group("Market");
            var wood = K.Mat("Stall_Wood", new Color(0.62f, 0.42f, 0.26f));
            var cloth = K.Mat("Stall_Cloth", new Color(0.95f, 0.6f, 0.72f));
            var s = new Vector3(3.3f, 0, 24f);    // stall, facing west onto the square
            K.Spawn(P + "Market_Stall.glb", m, s, -90);                           // counter, fruit crates, pink-striped roof
            K.Box("Stall Counter", m, s + new Vector3(0, 0.4f, 0), new Vector3(0.78f, 0.8f, 2.4f), null, true);
            // the scale (hidden until tomorrow's scale is set on the counter)
            var scale = new GameObject("Stall Scale").transform; scale.SetParent(m, false); scale.position = s + new Vector3(-0.05f, 0.8f, 0f);
            var brass = K.Mat("Brass", new Color(0.85f, 0.62f, 0.25f), 0.6f, 0.8f);
            K.Prim(PrimitiveType.Cylinder, "Base", scale, new Vector3(0, 0.03f, 0), new Vector3(0.18f, 0.03f, 0.18f), brass);
            K.Prim(PrimitiveType.Cylinder, "Post", scale, new Vector3(0, 0.15f, 0), new Vector3(0.03f, 0.12f, 0.03f), brass);
            K.Prim(PrimitiveType.Cylinder, "Pan", scale, new Vector3(0, 0.28f, 0), new Vector3(0.26f, 0.015f, 0.26f), brass);
            scale.gameObject.SetActive(false);
            K.Use("Stall", "Set the scale on Mira's counter", s + new Vector3(-0.9f, 0, 0), 1.3f);
            K.Spot("Drop_Stall", s + new Vector3(-1.4f, 0, -1.2f), 270);
            K.SpotLook("Cam_Stall", s + new Vector3(-3.5f, 1.6f, -3.5f), s + new Vector3(-1f, 0.3f, -2f));

            dir.mira = K.Character("Mira", s + new Vector3(0.75f, 0.02f, 0.4f), 270);
            K.Use("Mira", "Talk to Mira", s + new Vector3(-0.6f, 0, 0.4f), 1.4f);

            // fruit that rolls down the street in the Tomorrow Window preview
            var pumpkin = K.Mat("Pumpkin", new Color(0.95f, 0.55f, 0.15f));
            var apple = K.Mat("Apple", new Color(0.85f, 0.18f, 0.15f));
            var roll = new GameObject("Rolling Fruit").transform; roll.SetParent(m, false); roll.position = s + new Vector3(-1.5f, 0.12f, -1.5f);
            for (int i = 0; i < 6; i++)
                K.Prim(PrimitiveType.Sphere, "Fruit", roll, new Vector3((i % 3) * 0.3f - 0.3f, 0, -(i / 3) * 0.35f), Vector3.one * 0.2f, i % 2 == 0 ? pumpkin : apple);
            dir.rollingFruit = roll;

            // market workbench + Return Box on the east side of the square
            var wb = K.Spawn(P + "Workbench.glb", m, new Vector3(6.6f, 0, 21.6f), 270);
            K.Solid(wb);
            K.Use("Workbench_Market", "Build at the market bench", new Vector3(5.8f, 0, 21.6f), 1.2f);
            var rb = K.Spawn(P + "Return_Box.glb", m, new Vector3(6.6f, 0, 26.2f), 270);
            K.Solid(rb);
            K.Use("ReturnBox_Market", "Drop it in the Return Box", new Vector3(5.8f, 0, 26.2f), 1.2f);
            K.Spot("Interest_Spot", new Vector3(5.6f, 0, 24f), 0);

            // the market cart, wheel missing
            var cart = new GameObject("Market Cart").transform; cart.SetParent(m, false); cart.position = new Vector3(-1.9f, 0, 20.4f);
            cart.localRotation = Quaternion.Euler(0, 0, 8);   // slumped on the broken corner
            K.Box("Bed", cart, new Vector3(0, 0.38f, 0), new Vector3(0.8f, 0.18f, 1.2f), wood, false);
            K.Box("Handle", cart, new Vector3(0, 0.45f, -0.8f), new Vector3(0.6f, 0.04f, 0.4f), wood, false);
            var wheelMat = K.Mat("Wheel", new Color(0.35f, 0.24f, 0.16f));
            foreach (var p in new[] { new Vector3(-0.45f, 0.18f, 0.4f), new Vector3(0.45f, 0.18f, 0.4f), new Vector3(0.45f, 0.18f, -0.4f) })
                K.Prim(PrimitiveType.Cylinder, "Wheel", cart, p, new Vector3(0.36f, 0.03f, 0.36f), wheelMat).transform.localRotation = Quaternion.Euler(0, 0, 90);
            var fixedWheel = K.Prim(PrimitiveType.Cylinder, "Cart Wheel Fixed", cart, new Vector3(-0.45f, 0.18f, -0.4f), new Vector3(0.36f, 0.03f, 0.36f), wheelMat);
            fixedWheel.transform.localRotation = Quaternion.Euler(0, 0, 90);
            fixedWheel.SetActive(false);
            var col = cart.gameObject.AddComponent<BoxCollider>(); col.center = new Vector3(0, 0.35f, 0); col.size = new Vector3(1f, 0.7f, 1.3f);
            dir.cart = cart;
            K.Use("Cart", "Use the market cart", new Vector3(-1.9f, 0, 19.3f), 1.3f);
            K.Spot("Drop_Cart", new Vector3(-0.9f, 0, 19f), 180);

            // parts around the market
            var spring = K.Pickup("Pickup_Spring", ItemId.Spring, new Vector3(-5.4f, 0.02f, 26.6f));
            K.Prim(PrimitiveType.Cylinder, "Spring", spring.transform, new Vector3(0, 0.06f, 0), new Vector3(0.06f, 0.06f, 0.06f), K.Mat("Iron", new Color(0.45f, 0.47f, 0.5f), 0.5f, 0.9f));
            var plate = K.Pickup("Pickup_BrassPlate", ItemId.BrassPlate, new Vector3(2.4f, 0.02f, 25.6f));
            K.Prim(PrimitiveType.Cylinder, "Plate", plate.transform, new Vector3(0, 0.01f, 0), new Vector3(0.22f, 0.01f, 0.22f), brass);
            var bar = K.Pickup("Pickup_IronBar", ItemId.IronBar, new Vector3(2.4f, 0.02f, 30.4f));
            K.Prim(PrimitiveType.Cylinder, "Bar", bar.transform, new Vector3(0, 0.04f, 0), new Vector3(0.05f, 0.25f, 0.05f), K.Mat("Iron", new Color(0.45f, 0.47f, 0.5f), 0.5f, 0.9f)).transform.localRotation = Quaternion.Euler(0, 40, 90);
            var polish = K.Pickup("Pickup_Polish", ItemId.Polish, new Vector3(2.6f, 0.02f, 25.4f));
            K.Prim(PrimitiveType.Cylinder, "Tin of Polish", polish.transform, new Vector3(0, 0.04f, 0), new Vector3(0.12f, 0.04f, 0.12f), K.Mat("Polish_Tin", new Color(0.2f, 0.45f, 0.75f)));

            K.Spot("Nell_Greet", new Vector3(-0.6f, 0.02f, 20.8f), 180);
            K.Spot("Nell_Bed", new Vector3(-6.6f, 0.02f, 18.9f), 60);
            K.Spot("Nell_GH", GH + new Vector3(0.7f, 0.04f, -0.9f), 300);
        }

        static void BuildGardens(Chapter3Director dir)
        {
            var g = K.Group("Gardens");
            var soil = K.Mat("Soil", new Color(0.35f, 0.24f, 0.16f));
            var leaf = K.Mat("Leaf", new Color(0.3f, 0.55f, 0.25f));
            // Nell's flower bed (full of carrots), just east of the greenhouse
            var bed = new Vector3(-5.4f, 0, 19.6f);
            K.Box("Flower Bed", g, bed + new Vector3(0, 0.1f, 0), new Vector3(1.6f, 0.2f, 0.9f), soil, true);
            var carrots = K.Group("Carrots", g);
            var carrot = K.Mat("Carrot", new Color(0.95f, 0.5f, 0.1f));
            for (int i = 0; i < 6; i++)
            {
                var p = bed + new Vector3(-0.6f + (i % 3) * 0.6f, 0.22f, -0.2f + (i / 3) * 0.4f);
                K.Prim(PrimitiveType.Capsule, "Carrot", carrots, p, new Vector3(0.08f, 0.06f, 0.08f), carrot);
                K.Prim(PrimitiveType.Sphere, "Carrot Top", carrots, p + Vector3.up * 0.08f, new Vector3(0.12f, 0.08f, 0.12f), leaf);
            }
            dir.carrots = carrots.gameObject;
            var sprouts = K.Group("Glow Sprouts", g);
            sprouts.position = bed + Vector3.up * 0.2f;
            for (int i = 0; i < 3; i++)
                K.Spawn(P + "Flowers.glb", sprouts, bed + new Vector3(-0.5f + i * 0.5f, 0.2f, 0), i * 70);
            dir.glowSprouts = sprouts.gameObject;
            K.Use("FlowerBed", "Plant / tend Nell's flower bed", bed + new Vector3(0, 0, -0.9f), 1.3f);
            K.Spot("Drop_Bed", bed + new Vector3(1.6f, 0, -1.2f), 180);
            for (int i = 0; i < 3; i++)
            {
                var sd = K.Pickup("Pickup_GlowSeed" + (i + 1), ItemId.GlowSeed, bed + new Vector3(-0.8f + i * 0.8f, 0.02f, -0.85f - (i % 2) * 0.3f));
                K.Prim(PrimitiveType.Sphere, "Seed", sd.transform, new Vector3(0, 0.05f, 0), Vector3.one * 0.07f, K.Mat("Glow_Seed", new Color(1f, 0.85f, 0.35f)));
            }

            // Grandma's rose bush by the workshop door
            var roses = new GameObject("Grandma's Rose Bush").transform; roses.SetParent(g, false); roses.position = new Vector3(1.9f, 0, 4.6f);   // in front of the timber workshop front (which reaches z 3.93)
            K.Prim(PrimitiveType.Sphere, "Bush", roses, new Vector3(0, 0.28f, 0), new Vector3(0.7f, 0.55f, 0.6f), leaf);
            var rose = K.Mat("Rose", new Color(0.9f, 0.2f, 0.3f));
            for (int i = 0; i < 7; i++)
            {
                float a = i * 51f * Mathf.Deg2Rad;
                K.Prim(PrimitiveType.Sphere, "Rose", roses, new Vector3(Mathf.Cos(a) * 0.28f, 0.3f + (i % 3) * 0.1f, Mathf.Sin(a) * 0.24f - 0.05f), Vector3.one * 0.1f, rose);
            }
            dir.roseBush = roses.gameObject;
            K.Use("Roses", "Water Grandma's roses", new Vector3(1.9f, 0, 5.3f), 1.2f);
            K.Spot("Drop_Roses", new Vector3(0.4f, 0, 5.9f), 180);
            K.Spot("Nell_Roses", new Vector3(3.0f, 0.02f, 4.9f), 250);
            K.SpotLook("Cam_Roses", new Vector3(0.4f, 1.8f, 7.8f), new Vector3(2.1f, 0.4f, 4.6f));
            var handle = K.Pickup("Pickup_Handle", ItemId.Handle, new Vector3(-8.5f, 0.02f, 29.0f));
            K.Box("Handle", handle.transform, new Vector3(0, 0.04f, 0), new Vector3(0.25f, 0.04f, 0.05f), K.Mat("Iron", new Color(0.45f, 0.47f, 0.5f), 0.5f, 0.9f), false);
            var stick = K.Pickup("Pickup_Stick", ItemId.Stick, new Vector3(-3.5f, 0.02f, 5.0f));
            K.Prim(PrimitiveType.Cylinder, "Stick", stick.transform, new Vector3(0, 0.03f, 0), new Vector3(0.03f, 0.25f, 0.03f), K.Mat("Stick", new Color(0.55f, 0.38f, 0.22f))).transform.localRotation = Quaternion.Euler(0, 20, 90);
            var can2 = K.Pickup("Pickup_TinCan2", ItemId.TinCan, new Vector3(-11f, 0.02f, 19.8f));
            K.Prim(PrimitiveType.Cylinder, "Can", can2.transform, new Vector3(0, 0.06f, 0), new Vector3(0.1f, 0.06f, 0.1f), K.Mat("Tin", new Color(0.75f, 0.77f, 0.8f), 0.6f, 0.9f));
        }

        static void BuildGreenhouseExtras(Chapter3Director dir)
        {
            var g = K.Group("Greenhouse Extras");
            // mud heaped against the greenhouse door (south side gap)
            var mud = new GameObject("Mud").transform; mud.SetParent(g, false); mud.position = GH + new Vector3(0, 0, -1.9f);
            var mudMat = K.Mat("Mud", new Color(0.32f, 0.22f, 0.14f));
            for (int i = 0; i < 5; i++)
                K.Prim(PrimitiveType.Sphere, "Clod", mud, new Vector3(-0.45f + i * 0.22f, 0.08f + (i % 2) * 0.05f, (i % 2) * 0.12f), new Vector3(0.45f, 0.25f, 0.4f), mudMat);
            var mc = mud.gameObject.AddComponent<BoxCollider>(); mc.center = new Vector3(0, 0.3f, 0); mc.size = new Vector3(1.4f, 0.6f, 0.5f);
            dir.mud = mud.gameObject;
            K.Use("Mud", "Clear the mud from the door", GH + new Vector3(0, 0, -2.6f), 1.2f);
            K.Spot("Drop_Mud", GH + new Vector3(1.7f, 0, -3.2f), 180);
            K.Spot("Cart_Greenhouse", GH + new Vector3(1.5f, 0, -3.1f), 0);
            K.Spot("Nell_Sad", GH + new Vector3(-0.7f, 0.02f, -2.6f), 180);
            K.SpotLook("Cam_GreenhouseDoor", GH + new Vector3(1.2f, 1.6f, -6f), GH + new Vector3(0, 0.6f, -1.6f));

            // cracked roof
            var crack = K.Group("Roof Crack", g);
            var dark = K.Mat("Crack", new Color(0.15f, 0.15f, 0.18f));
            for (int i = 0; i < 4; i++)
                K.Box("Crack", crack, GH + new Vector3(0.6f + i * 0.18f, 2.35f - i * 0.06f, -0.5f + i * 0.25f), new Vector3(0.35f, 0.02f, 0.03f), dark, false)
                    .transform.rotation = Quaternion.Euler(0, 30 + i * 25, -20);
            dir.roofCrack = crack.gameObject;
            K.Use("Roof", "Fit the new glass panes", GH + new Vector3(0.5f, 0, 0.0f), 1.2f);
            K.Spot("Greenhouse_Inside", GH + new Vector3(0, 0, -0.8f), 0);
            K.Spot("Drop_Greenhouse_In", GH + new Vector3(-0.3f, 0.04f, -0.6f), 180);

            // the frozen corner and Grandma's paper under the ice
            var ice = K.Prim(PrimitiveType.Cube, "Frozen Corner", g, GH + new Vector3(1.63f, 0.16f, 1.2f), new Vector3(0.32f, 0.32f, 0.32f), K.MatTransparent("Ice", new Color(0.8f, 0.92f, 1f, 0.75f)));
            dir.iceBlock = ice.transform;
            var paper = K.Pickup("Pickup_AdaPaper", ItemId.DiaryPage, GH + new Vector3(1.6f, 0.02f, 1.2f));
            K.Box("Paper", paper.transform, new Vector3(0, 0.005f, 0), new Vector3(0.2f, 0.01f, 0.26f), K.Mat("Paper", new Color(0.98f, 0.95f, 0.85f)), false);
            paper.GetComponent<Interactable>().range = 1.3f;

            // mirror on a stand; the sunbeam comes in through the roof
            var stand = new GameObject("Mirror Stand").transform; stand.SetParent(g, false); stand.position = GH + new Vector3(-0.7f, 0, -0.6f);
            K.Prim(PrimitiveType.Cylinder, "Stand", stand, new Vector3(0, 0.15f, 0), new Vector3(0.06f, 0.15f, 0.06f), K.Mat("Iron", new Color(0.45f, 0.47f, 0.5f), 0.5f, 0.9f));
            var mir = K.Spawn(P + "Mirror.glb", stand, stand.position + Vector3.up * 0.3f, 0);
            K.FitHeight(mir, 0.4f);
            var pivot = new GameObject("Mirror").transform; pivot.SetParent(stand, false); pivot.position = stand.position + Vector3.up * 0.3f;
            mir.transform.SetParent(pivot, true);
            // start pointing away from the corner: 3 turns of 30° bring it round
            var toIce = (ice.transform.position - pivot.position); toIce.y = 0;
            pivot.rotation = Quaternion.LookRotation(toIce) * Quaternion.Euler(0, -90f, 0);
            dir.mirror = pivot;
            K.Use("MirrorStand", "Turn the mirror", stand.position + new Vector3(0, 0, -0.7f), 1.1f);
            K.Spot("Sun_Source", GH + new Vector3(-2.5f, 4.2f, -2.5f), 0);
            var beam = new GameObject("Sunbeam");
            beam.transform.SetParent(g, false);
            var lr = beam.AddComponent<LineRenderer>();
            lr.widthMultiplier = 0.06f;
            var beamMat = K.Mat("Sunbeam", new Color(1f, 0.92f, 0.55f));
            beamMat.EnableKeyword("_EMISSION");
            beamMat.SetColor("_EmissionColor", new Color(1f, 0.85f, 0.4f) * 3f);
            lr.sharedMaterial = beamMat;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            dir.sunBeam = beam;

            // parts for new panes and a new mirror
            var glass = K.MatTransparent("Glass", new Color(0.8f, 0.93f, 1f, 0.35f));
            var s1 = K.Pickup("Pickup_GlassShard1", ItemId.GlassShard, GH + new Vector3(2.6f, 0.02f, 0.5f));
            K.Box("Shard", s1.transform, new Vector3(0, 0.02f, 0), new Vector3(0.2f, 0.02f, 0.14f), glass, false).transform.localRotation = Quaternion.Euler(0, 30, 0);
            var s2 = K.Pickup("Pickup_GlassShard2", ItemId.GlassShard, GH + new Vector3(-2.6f, 0.02f, -1.5f));
            K.Box("Shard", s2.transform, new Vector3(0, 0.02f, 0), new Vector3(0.18f, 0.02f, 0.16f), glass, false).transform.localRotation = Quaternion.Euler(0, -20, 0);
            var frame = K.Pickup("Pickup_Frame", ItemId.Frame, new Vector3(-2.6f, 0.02f, 4.2f));
            K.Box("Frame", frame.transform, new Vector3(0, 0.03f, 0), new Vector3(0.4f, 0.04f, 0.3f), K.Mat("Greenhouse_Frame", new Color(0.95f, 0.95f, 0.92f)), false);
            var can3 = K.Pickup("Pickup_TinCan3", ItemId.TinCan, new Vector3(-6.8f, 0.02f, 31.2f));
            K.Prim(PrimitiveType.Cylinder, "Can", can3.transform, new Vector3(0, 0.06f, 0), new Vector3(0.1f, 0.06f, 0.1f), K.Mat("Tin", new Color(0.75f, 0.77f, 0.8f), 0.6f, 0.9f));

            // ending: sitting among the glowing flowers
            K.Spot("Ending_Pip", GH + new Vector3(-0.25f, 0.04f, 0.1f), 30);
            K.Spot("Ending_Nell", GH + new Vector3(0.35f, 0.04f, 0.25f), 300);
            K.Spot("Ending_Tock", GH + new Vector3(0.05f, 0.04f, -0.45f), 0);
            K.SpotLook("Cam_Ending", GH + new Vector3(0.2f, 1.2f, -2.3f), GH + new Vector3(0.05f, 0.4f, 0.2f));
            K.Spot("Drop_Table", GH + new Vector3(0.9f, 0.04f, -0.3f), 180);
        }
    }
}
