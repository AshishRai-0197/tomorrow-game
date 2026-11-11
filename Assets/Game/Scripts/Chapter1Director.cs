using System;
using System.Collections;
using UnityEngine;

namespace Tomorrow
{
    /// <summary>
    /// Chapter 1 — "Borrow. Use. Build. Return."
    /// Intro, five puzzles and the ending, as one coroutine (see <see cref="ChapterDirector"/> for the engine).
    /// </summary>
    public class Chapter1Director : ChapterDirector
    {
        [Header("Cast")]
        public GameObject oskar, nell, bram;

        [Header("World state")]
        public GameObject brokenBridge, fixedBridge, riverBlocker;
        public GameObject greenhouseFlowers, greenhouseHeater, townGlowFlowers;
        public Light ovenLight, towerLight;
        public GameObject towerLamp, bakery, flourCloud, greySpot;

        [Header("Pacing")]
        public float sunsetCountdown = 60f;

        // story flags
        bool ovenFirst, choiceMade, bonusGiven, windowSeen, gotGlowFlower;

        protected override void Setup()
        {
            if (fixedBridge) fixedBridge.SetActive(false);
            if (townGlowFlowers) townGlowFlowers.SetActive(false);
            if (greenhouseHeater) greenhouseHeater.SetActive(false);
            if (flourCloud) flourCloud.SetActive(false);
            if (greySpot) greySpot.SetActive(false);
            if (ovenLight) ovenLight.enabled = false;
            if (towerLight) towerLight.enabled = false;
            if (greenhouseFlowers) Visuals.Tint(greenhouseFlowers, new Color(0.35f, 0.55f, 1f), 0.75f);
            tock.gameObject.SetActive(false);
        }

        void RegisterWorkshopTools() => toolsOn = true;

        // ------------------------------------------------------------------ the chapter
        protected override IEnumerator Run()
        {
            yield return Intro();
            yield return Puzzle1_StuckDoor();
            yield return Puzzle2_BrokenBridge();
            yield return Puzzle3_FreezingGreenhouse();
            yield return Puzzle4_ColdOven();
            yield return Puzzle5_GreatClock();
            yield return Ending();
        }

        IEnumerator Intro()
        {
            cutscene = true;
            clock.hour = 7f; clock.cap = 18f; clock.running = false;
            ui.SetHudVisible(false);
            sfx.Ticking(true);
            ui.SetCaption("The workshop, morning");
            yield return ui.Fade(0f, 2.5f);
            yield return Wait(1.5f);
            ui.SetCaption("");
            sfx.Ticking(false);                       // a hundred clocks stop at once
            yield return Wait(1.2f);
            ui.SetCaption("Silence.");
            yield return Wait(1.6f);
            ui.SetCaption("");
            yield return Say(PIP, "Grandma? …Grandma Ada?");
            ui.SetHudVisible(true);
            cutscene = false;

            Objective("Read the note pinned to the wall");
            bool read = false;
            On("Note", () => ReadNote(() => read = true));
            while (!read) yield return null;

            yield return Say(PIP, "Borrow? Borrow what?");

            // Tock climbs out of the drawer
            cutscene = true;
            yield return Wait(0.4f);
            sfx.Tick();
            ui.Toast("A drawer rattles…");
            yield return TockClimbsOut();
            yield return Say(TOCK, "Finally. Do you know how long I've been in that drawer? Three years. No snacks.");
            yield return Say(PIP, "You… talk?");
            yield return Say(TOCK, "I tick. Talking is a hobby. I'm Tock. Your grandma built me. Now listen. Something big just arrived.");

            DropBox("Drop_Workshop", "Box1");
            yield return Wait(1.3f);
            yield return Note("A label on the box", "FROM: TOMORROW.\nRETURN BY: SUNSET.");
            cutscene = false;
            clock.running = true;
            RegisterWorkshopTools();
        }

        IEnumerator ReadNote(Action done)
        {
            yield return Note("A note in Grandma's handwriting",
                "\"Pip. I have to go fix something.\nTake care of the town.\nAnd never borrow more than you can return.\n\n— Ada\"", ADA);
            Off("Note");
            done();
        }

        IEnumerator TockClimbsOut()
        {
            var start = Spot("Tock_Drawer");
            var land = Spot("Tock_Land");
            tock.gameObject.SetActive(true);
            tock.active = false;
            var t = tock.transform;
            t.SetPositionAndRotation(start.position, start.rotation);
            for (float a = 0; a < 0.6f; a += Time.deltaTime)
            {
                t.localScale = Vector3.one * Mathf.Lerp(0.2f, 1f, a / 0.6f);
                t.position = start.position + Vector3.up * Mathf.Sin(a / 0.6f * Mathf.PI) * 0.12f;
                yield return null;
            }
            t.localScale = Vector3.one;
            var from = t.position;
            for (float a = 0; a < 0.7f; a += Time.deltaTime)
            {
                float k = a / 0.7f;
                t.position = Vector3.Lerp(from, land.position, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.4f;
                yield return null;
            }
            t.position = land.position;
            // a stretch
            for (float a = 0; a < 0.6f; a += Time.deltaTime)
            {
                float s = 1f + Mathf.Sin(a / 0.6f * Mathf.PI) * 0.18f;
                t.localScale = new Vector3(1f / Mathf.Sqrt(s), s, 1f / Mathf.Sqrt(s));
                yield return null;
            }
            t.localScale = Vector3.one;
            sfx.Ticking(true);                        // Tock's own little tick
            tock.follow = player.transform;
            tock.active = true;
        }

        // ---------------------------------------------------------- Puzzle 1
        IEnumerator Puzzle1_StuckDoor()
        {
            Objective("Get out of the workshop");
            bool tried = false;
            On("Door", () => DoorStuck(() => tried = true));
            while (!tried) yield return null;

            Objective("Open the Tomorrow Box");
            bool opened = false;
            On("Box1", () => OpenBox1(() => opened = true));
            while (!opened) yield return null;

            Objective("Use the crowbar on the stuck door");
            bool open = false;
            On("Door", () => PryDoor(() => open = true));
            while (!open) yield return null;

            Objective("Find an iron pipe and red tape, build a crowbar at the workbench, and drop it in the Return Box");
            Pickup("Pickup_IronPipe"); Pickup("Pickup_RedTape");
            while (!Repaid(ItemId.Crowbar)) yield return null;
            AtLeastHour(8.5f);
            yield return Say(TOCK, "See? Borrow. Use. Build. Return. Easy. Like breathing, but with more paperwork.");
        }

        IEnumerator DoorStuck(Action done)
        {
            yield return Say(PIP, "The door won't open!");
            yield return Say(TOCK, "Open the box. Tomorrow knows you need help.");
            Off("Door");
            done();
        }

        IEnumerator OpenBox1(Action done)
        {
            Off("Box1");
            var box = Find("Box1");
            ui.Toast("Inside: a crowbar.");
            Borrow(ItemId.Crowbar);
            yield return Say(TOCK, "That's tomorrow's crowbar. You can use it today. But tomorrow, you must build it and send it back. Otherwise, tomorrow has no crowbar. And tomorrow gets very upset.");
            yield return Say(PIP, "How upset?");
            yield return Say(TOCK, "Grey. Very grey.");
            yield return Say(PIP, "And if I'm late?");
            yield return Say(TOCK, "Then tomorrow charges interest. One extra part. Like a library, but angrier.");
            yield return Say(PIP, "Got it. Borrow fast. Return faster.");
            if (box) yield return VanishBox(box);
            done();
        }

        IEnumerator PryDoor(Action done)
        {
            Off("Door");
            inv.Remove(ItemId.Crowbar);                 // tomorrow's crowbar is used up today
            sfx.Build();
            var from = workshopDoor.localRotation;
            var to = from * Quaternion.Euler(0, -100f, 0);
            for (float a = 0; a < 0.8f; a += Time.deltaTime)
            {
                workshopDoor.localRotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0, 1, a / 0.8f));
                yield return null;
            }
            if (doorBlocker) doorBlocker.enabled = false;
            ui.Toast("The door creaks open.");
            done();
        }

        // ---------------------------------------------------------- Puzzle 2
        IEnumerator Puzzle2_BrokenBridge()
        {
            Objective("Head out to the bridge into town");
            yield return WaitNear("Bridge_Near", 2.2f);
            cutscene = true;
            yield return LookAt("Cam_Bridge", 1.2f);
            yield return Say(OSKAR, "Bridge fell this morning. Same minute the clock stopped. Strange day.");
            yield return Say(PIP, "I can fix it! Probably!");
            cam.Release();
            DropBox("Drop_Bridge", "Box2");
            yield return Wait(1.2f);
            cutscene = false;

            Objective("Open the Tomorrow Box");
            bool opened = false;
            On("Box2", () => OpenSimpleBox("Box2", ItemId.BridgeKit, "Inside: a Plank Bridge Kit.", () => opened = true));
            while (!opened) yield return null;

            Objective("Lay the Plank Bridge Kit across the river");
            bool crossed = false;
            On("BridgeGap", () => BuildBridge(() => crossed = true));
            while (!crossed) yield return null;

            yield return Say(TOCK, "Now we need three planks and some rope. Before sunset.");
            Objective("Talk to Captain Oskar");
            bool talked = false;
            On("Oskar", () => OskarPlanks(() => talked = true));
            while (!talked) yield return null;

            Objective("Take 3 planks from Oskar's old boat. Find rope on the dock.");
            Pickups(true, "Pickup_Plank");
            DropBox("Drop_Dock", "Box3", "Open the small Tomorrow Box");
            bool rope = false;
            On("Box3", () => OskarsRope(() => rope = true));
            while (!rope) yield return null;

            Objective("Build the bridge kit at the workbench and return it");
            while (!Repaid(ItemId.BridgeKit)) yield return null;
            AtLeastHour(10f);
        }

        IEnumerator OpenSimpleBox(string id, ItemId item, string toast, Action done)
        {
            Off(id);
            var box = Find(id);
            ui.Toast(toast);
            Borrow(item);
            if (box) yield return VanishBox(box);
            done();
        }

        IEnumerator BuildBridge(Action done)
        {
            Off("BridgeGap");
            inv.Remove(ItemId.BridgeKit);
            sfx.Build();
            fixedBridge.SetActive(true);
            var t = fixedBridge.transform;
            var s = t.localScale;
            for (float a = 0; a < 0.7f; a += Time.deltaTime) { t.localScale = new Vector3(s.x, s.y, s.z * (a / 0.7f)); yield return null; }
            t.localScale = s;
            if (riverBlocker) riverBlocker.SetActive(false);
            ui.Toast("A quick bridge! It wobbles, but it holds.");
            yield return Wait(0.6f);
            done();
        }

        IEnumerator OskarPlanks(Action done)
        {
            Off("Oskar");
            yield return Say(OSKAR, "Planks? My old boat is full of them. Take them. She hasn't sailed since your grandma was young.");
            yield return Say(PIP, "You knew Grandma when she was young?");
            yield return Say(OSKAR, "Everyone did. She was the only person who was ever on time. (He laughs.) Go on. Bring her back.");
            done();
        }

        IEnumerator OskarsRope(Action done)
        {
            Off("Box3");
            var box = Find("Box3");
            yield return Note("A tag on the little box", "OSKAR'S ROPE —\nFROM HIS TOMORROW.");
            yield return Say(OSKAR, "That's MY rope. From tomorrow? Then bring it back, lad. Tomorrow I'm fishing.");
            yield return Say(TOCK, "Borrowing from a friend. Higher stakes. Lower interest. Friends are nice like that.");
            inv.Add(ItemId.Rope);
            sfx.Pickup();
            ui.Toast("+ Rope (Oskar's, from tomorrow)");
            if (box) yield return VanishBox(box);
            done();
        }

        // ---------------------------------------------------------- Puzzle 3
        IEnumerator Puzzle3_FreezingGreenhouse()
        {
            Objective("Nell is waving from her greenhouse");
            bool talked = false;
            On("Nell", () => NellCold(() => talked = true));
            while (!talked) yield return null;

            Objective("Open the Tomorrow Box");
            bool opened = false;
            On("Box4", () => OpenSimpleBox("Box4", ItemId.Heater, "Inside: a little heater.", () => opened = true));
            while (!opened) yield return null;

            Objective("Set the heater down in the greenhouse");
            bool warm = false;
            On("HeaterSpot", () => WarmGreenhouse(() => warm = true));
            while (!warm) yield return null;

            Objective("Ask Nell for a glow-flower");
            On("Nell", NellFlower);
            Pickup("Pickup_TinCan");
            while (!gotGlowFlower) yield return null;

            Objective("Find a tin can, build a heater and return it — before noon for a surprise!");
            while (!Repaid(ItemId.Heater)) yield return null;

            if (clock.hour < DayClock.Noon && !bonusGiven)
            {
                bonusGiven = true;
                townGlowFlowers.SetActive(true);
                Visuals.Glow(townGlowFlowers, new Color(1f, 0.85f, 0.4f), 1.4f);
                cutscene = true;
                yield return LookAt("Cam_Town", 1.5f);
                yield return Say(NELL, "Pip! Look! The whole town is glowing!");
                yield return Say(TOCK, "Early payment. Tomorrow is very pleased. Tomorrow sends flowers.");
                cam.Release();
                cutscene = false;
            }
            AtLeastHour(12.5f);
        }

        IEnumerator NellCold(Action done)
        {
            Off("Nell");
            yield return Say(NELL, "Pip! My glow-flowers are turning blue. Flowers aren't supposed to be blue when they're SAD.");
            DropBox("Drop_Greenhouse", "Box4");
            yield return Wait(1.2f);
            done();
        }

        IEnumerator WarmGreenhouse(Action done)
        {
            Off("HeaterSpot");
            inv.Remove(ItemId.Heater);
            greenhouseHeater.SetActive(true);
            Visuals.Glow(greenhouseHeater, new Color(1f, 0.45f, 0.15f), 2f);
            sfx.Build();
            for (float a = 0; a < 1.5f; a += Time.deltaTime)
            {
                Visuals.Tint(greenhouseFlowers, new Color(0.35f, 0.55f, 1f), 0.75f * (1f - a / 1.5f));
                yield return null;
            }
            Visuals.Tint(greenhouseFlowers, Color.white, 0f);
            Visuals.Glow(greenhouseFlowers, new Color(1f, 0.8f, 0.35f), 0.9f);
            ui.Toast("The greenhouse warms. The flowers glow again.");
            yield return Say(TOCK, "Now we must build a heater. We need a glow-flower and a tin can.");
            yield return Say(PIP, "But the glow-flowers only bloomed because of the heater…");
            yield return Say(TOCK, "Yes. The heater saved the flowers. The flowers make the heater. Time is weird. Don't think about it too hard. I tried once and my spring popped.");
            done();
        }

        IEnumerator NellFlower()
        {
            Off("Nell");
            yield return Say(NELL, "You can have one flower. But you have to give it a nice home.");
            yield return Say(PIP, "The nicest. A heater.");
            yield return Say(NELL, "…That's actually a really nice home.");
            inv.Add(ItemId.GlowFlower);
            sfx.Pickup();
            ui.Toast("+ Glow-flower");
            gotGlowFlower = true;
        }

        // ---------------------------------------------------------- Puzzle 4
        IEnumerator Puzzle4_ColdOven()
        {
            if (bakery) Visuals.Grey(bakery, 0f);
            Objective("Something smells wrong at Bram's bakery. Nothing smells at all.");
            bool talked = false;
            On("Bram", () => BramNoBread(() => talked = true));
            while (!talked) yield return null;

            Objective("Open the Tomorrow Box");
            bool opened = false;
            On("Box5", () => OpenBatteryBox(() => opened = true));
            while (!opened) yield return null;

            Objective("Look in the Tomorrow Window at the workshop — then choose: the oven, or the clock tower light");
            On("TomorrowWindow", TomorrowWindow);
            On("Oven", () => PlaceBattery(true));
            On("TowerLight", () => PlaceBattery(false));
            while (!choiceMade) yield return null;
            Off("Oven"); Off("TowerLight");

            yield return Say(TOCK, "Good choice. Probably. I'm a watch, not a judge.");

            if (ovenFirst)
            {
                Objective("Bram is baking! Go see him.");
                bool loaf = false;
                On("Bram", () => BramFirstLoaf(() => loaf = true));
                while (!loaf) yield return null;

                Objective("Bring the warm bread to Captain Oskar");
                bool traded = false;
                On("Oskar", () => OskarBread(() => traded = true));
                while (!traded) yield return null;

                ExtraBuilds.Add(ItemId.Battery);
                Objective("Build batteries from copper wire + jam jars: one for the clock tower, one to return");
                On("TowerLight", LightTowerWithBuiltBattery);
            }
            else
            {
                Objective("No bread to trade. Find copper wire the long way — behind the post office");
                Pickup("Pickup_CopperWire");
                Pickup("Pickup_JamJar");
            }

            Objective(ovenFirst
                ? "Build a battery (wire + jam jar) and return it. Light the clock tower too!"
                : "Build a battery from copper wire and the jam jar in the bakery, then return it");
            while (!Repaid(ItemId.Battery)) yield return null;
            Off("TowerLight"); Off("TomorrowWindow");
            if (tomorrowWindow) Visuals.Glow(tomorrowWindow, Color.black, 0f);
        }

        IEnumerator BramNoBread(Action done)
        {
            Off("Bram");
            yield return Say(BRAM, "No bread. No bread! Pip, a town without bread is just… a place.");
            DropBox("Drop_Bakery", "Box5");
            yield return Wait(1.2f);
            done();
        }

        IEnumerator OpenBatteryBox(Action done)
        {
            Off("Box5");
            var box = Find("Box5");
            Borrow(ItemId.Battery);
            ui.Toast("Inside: one battery.");
            yield return LookAt("Cam_Tower", 1.4f);
            yield return Say(TOCK, "Careful. One battery. Two problems. The oven, or the clock tower light. Choose.");
            cam.Release();
            if (tomorrowWindow) Visuals.Glow(tomorrowWindow, new Color(0.4f, 0.75f, 1f), 2.5f);
            yield return Say(TOCK, "And back home, a round brass window on the workshop wall just started to glow. The Tomorrow Window. Before you borrow, look. That window shows tomorrow if you never pay it back.");
            if (box) yield return VanishBox(box);
            done();
        }

        IEnumerator TomorrowWindow()
        {
            // Preview 1: the bakery, grey and sad.
            yield return ui.Fade(1f, 0.4f);
            cam.Focus(Spot("Cam_Bakery"));
            cam.transform.SetPositionAndRotation(Spot("Cam_Bakery").position, Spot("Cam_Bakery").rotation);
            var bramIdle = bram.GetComponentInChildren<Idle>();
            var bramRot = bram.transform.rotation;
            bram.transform.rotation = Spot("Cam_Bakery").rotation * Quaternion.Euler(0, 180, 0);
            bram.transform.localScale = new Vector3(1.05f, 0.9f, 1.05f);    // slumped
            ui.SetHudVisible(false);
            yield return GreyTo(1f, 0.01f);
            ui.SetCaption("Tomorrow, if never paid back…");
            yield return ui.Fade(0f, 0.6f);
            yield return Wait(2.2f);
            ui.SetCaption("The bakery: dark and grey. Bram sits with an empty bread basket.");
            yield return Wait(2.6f);

            // Preview 2: the tower, dark, the town lost in fog.
            yield return ui.Fade(1f, 0.4f);
            bram.transform.rotation = bramRot;
            bram.transform.localScale = Vector3.one;
            cam.Focus(Spot("Cam_Tower"));
            cam.transform.SetPositionAndRotation(Spot("Cam_Tower").position, Spot("Cam_Tower").rotation);
            bool fog = RenderSettings.fog; var fd = RenderSettings.fogDensity; var fm = RenderSettings.fogMode;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Exponential; RenderSettings.fogDensity = 0.09f;
            ui.SetCaption("The clock tower: dark. The town lost in the night fog.");
            yield return ui.Fade(0f, 0.6f);
            yield return Wait(3f);
            yield return ui.Fade(1f, 0.4f);
            RenderSettings.fog = fog; RenderSettings.fogDensity = fd; RenderSettings.fogMode = fm;
            greyVolume.weight = 0f;
            ui.SetCaption("");
            cam.Release();
            cam.SnapNow();
            ui.SetHudVisible(true);
            yield return ui.Fade(0f, 0.5f);
            windowSeen = true;
            yield return Say(PIP, "Both look terrible.");
            yield return Say(TOCK, "Welcome to adulthood.");
        }

        IEnumerator PlaceBattery(bool oven)
        {
            if (!inv.Has(ItemId.Battery))
            {
                yield return Say(TOCK, "We need tomorrow's battery first.");
                yield break;
            }
            if (!windowSeen)
                yield return Say(TOCK, "You could peek in the Tomorrow Window first. Or not. Living dangerously suits you.");
            int pick = -1;
            yield return ui.Choose("One battery. Two problems. Where does it go?",
                "The oven — bread for the town", "The clock tower light", i => pick = i);
            ovenFirst = pick == 0;
            inv.Remove(ItemId.Battery);
            sfx.Build();
            if (ovenFirst)
            {
                if (ovenLight) ovenLight.enabled = true;
                ui.Toast("The oven hums. The smell of bread rolls out the door.");
            }
            else
            {
                LightTower();
                ui.Toast("The clock tower light blinks on over the town.");
            }
            choiceMade = true;
        }

        void LightTower()
        {
            if (towerLight) towerLight.enabled = true;
            if (towerLamp) Visuals.Glow(towerLamp, new Color(1f, 0.85f, 0.45f), 4f);
        }

        IEnumerator BramFirstLoaf(Action done)
        {
            Off("Bram");
            yield return Say(BRAM, "Here. First loaf of the day. Your grandma always got the first one. Today, you get it.");
            yield return Say(PIP, "(quietly) Thank you, Bram.");
            yield return Say(BRAM, "And take this one to Oskar. He's been eating dry biscuits since dawn. Oh — and jam jars. Empty ones. You look like someone who needs jars.");
            inv.Add(ItemId.Bread);
            inv.Add(ItemId.JamJar, 2);
            sfx.Pickup();
            ui.Toast("+ Warm bread   + Jam jar ×2");
            done();
        }

        IEnumerator OskarBread(Action done)
        {
            Off("Oskar");
            inv.Remove(ItemId.Bread);
            yield return Say(OSKAR, "Warm bread! Now THAT is a proper morning. Here, lad — copper wire, off the old mast. Plenty for two.");
            inv.Add(ItemId.CopperWire, 2);
            sfx.Pickup();
            ui.Toast("+ Copper wire ×2");
            done();
        }

        IEnumerator LightTowerWithBuiltBattery()
        {
            if (!inv.Has(ItemId.Battery))
            {
                yield return Say(TOCK, "The tower needs a battery. Build one at the workbench.");
                yield break;
            }
            // Keep one battery for tomorrow if it isn't paid yet.
            bool owing = !Repaid(ItemId.Battery);
            if (owing && inv.Count(ItemId.Battery) < 2)
            {
                yield return Say(TOCK, "That's our only battery, and tomorrow wants one back. Build a second one first.");
                yield break;
            }
            Off("TowerLight");
            inv.Remove(ItemId.Battery);
            sfx.Build();
            LightTower();
            ui.Toast("The clock tower light blinks on over the town.");
        }

        // ---------------------------------------------------------- Puzzle 5
        IEnumerator Puzzle5_GreatClock()
        {
            cutscene = true;
            clock.cap = 19f;
            clock.hour = 18.97f;
            clock.running = false;
            yield return Say(TOCK, "Pip. The sun is setting. Everything we borrowed has to be in the Return Box before the due bell.");
            debts.Add(new Debt { item = ItemId.GrandmasWrench, fromWhom = "Tomorrow" });
            RefreshHud();
            yield return Say(TOCK, "Wait. The ledger says five things. We've returned four. …The wrench! Tomorrow tucked a wrench in with the very first box. Every build today used it.");
            yield return Say(TOCK, "Pip. Sunset in one minute. One thing is still out there!");
            Pickup("Pickup_Wrench");
            cutscene = false;
            Objective("Find tomorrow's wrench and put it in the Return Box before the bell!");

            float left = sunsetCountdown;
            while (left > 0 && !Repaid(ItemId.GrandmasWrench))
            {
                left -= Time.deltaTime;
                ui.SetTimer($"{Mathf.CeilToInt(left)}");
                clock.hour = Mathf.Lerp(19f, 18.97f, left / sunsetCountdown);
                yield return null;
            }
            ui.SetTimer(null);
            bool onTime = Repaid(ItemId.GrandmasWrench);
            clock.hour = 19f;
            sfx.Bell();

            if (!onTime) yield return LatePenalty();

            cutscene = true;
            yield return LookAt("Cam_GreatClock", 1.2f);
            yield return greatClock.TickOnce();
            sfx.Tick();
            yield return Wait(0.6f);
            yield return Say(PIP, "It moved! It moved!");
            yield return Say(TOCK, "One tick. That's a start. Tomorrow we try for two.");
            cam.Release();
            cutscene = false;
        }

        IEnumerator LatePenalty()
        {
            cutscene = true;
            yield return LookAt("Cam_Bakery", 0.6f);
            if (greySpot) { greySpot.SetActive(true); StartCoroutine(Grow(greySpot.transform, 1.2f)); }
            if (bakery) for (float a = 0; a < 1.2f; a += Time.deltaTime) { Visuals.Grey(bakery, a / 1.2f); yield return null; }
            var bi = bram.GetComponentInChildren<Idle>(); if (bi) bi.frozen = true;
            Visuals.Grey(bram, 0.9f);
            if (flourCloud) flourCloud.SetActive(true);
            ui.SetCaption("A grey spot spreads over the bakery. Bram freezes mid-sneeze.");
            yield return Wait(2.6f);
            ui.SetCaption("");
            cam.Release();
            yield return Say(TOCK, "Too late. Tomorrow charges interest. One extra part. Like a library, but angrier.");
            debts.Add(new Debt { item = ItemId.Plank, fromWhom = "Tomorrow (interest)" });
            RefreshHud();
            Pickup("Pickup_InterestPlank");
            cutscene = false;
            Objective(Repaid(ItemId.GrandmasWrench)
                ? "Pay the interest: find one more plank and put it in the Return Box"
                : "Pay the interest: return the wrench AND one more plank");
            while (!AllRepaid) yield return null;

            cutscene = true;
            yield return LookAt("Cam_Bakery", 0.8f);
            if (greySpot) greySpot.SetActive(false);
            if (bakery) Visuals.Grey(bakery, 0f);
            Visuals.Grey(bram, 0f);
            if (bi) bi.frozen = false;
            if (flourCloud) flourCloud.SetActive(false);
            yield return Say(BRAM, "…CHOO! Why is it suddenly evening?");
            yield return Say(TOCK, "Long story. Mostly interest.");
            cam.Release();
            cutscene = false;
        }

        IEnumerator Grow(Transform t, float secs)
        {
            var s = t.localScale;
            for (float a = 0; a < secs; a += Time.deltaTime) { t.localScale = s * (a / secs); yield return null; }
            t.localScale = s;
        }

        // ---------------------------------------------------------- Ending
        IEnumerator Ending()
        {
            cutscene = true;
            Objective(null);
            yield return ui.Fade(1f, 1.2f);
            clock.hour = 21f;
            clock.cap = 21f;
            var spot = Spot("Pip_Ending");
            player.Teleport(spot.position, spot.eulerAngles.y);
            tock.Warp(spot.position + spot.right * 0.6f);
            cam.SnapNow();
            ui.SetCaption("The workshop, night");
            yield return ui.Fade(0f, 1.5f);
            yield return Wait(1.2f);
            ui.SetCaption("");
            DropBox("Drop_Final", "Box6", "Open the last Tomorrow Box");
            yield return Wait(1.3f);
            cutscene = false;
            Objective("One more box arrived");
            bool opened = false;
            On("Box6", () => FinalBox(() => opened = true));
            while (!opened) yield return null;

            cutscene = true;
            Objective(null);
            yield return Wait(1f);
            yield return ui.Fade(1f, 2.5f);
            ui.SetHudVisible(false);
            yield return Wait(1f);
            sfx.Tick();
            yield return greatClock.TickOnce();
            yield return Wait(0.8f);
            ui.SetCaption("END OF CHAPTER 1");
        }

        IEnumerator FinalBox(Action done)
        {
            Off("Box6");
            var box = Find("Box6");
            ui.Toast("Inside: no tool. Only a letter and a yellow glove.");
            inv.Add(ItemId.YellowGlove);
            inv.Add(ItemId.Letter);
            yield return Wait(1f);
            yield return Say(PIP, "That's my glove. I lost it last week.");
            ui.Toast("The handwriting is Pip's own.");
            yield return Wait(1.2f);
            yield return Note("A letter (in your own handwriting, older and tired)",
                "\"Pip.\nDon't trust everything I send you.\nGrandma is inside the clock.\nSave her, but don't break tomorrow.\n\n— Pip\"", OLDPIP);
            sfx.Ticking(false);                       // Tock stops ticking.
            tock.active = false;
            yield return Wait(1.5f);
            yield return Say(TOCK, "…Well. That's new.");
            if (box) yield return VanishBox(box);
            done();
        }
    }
}
