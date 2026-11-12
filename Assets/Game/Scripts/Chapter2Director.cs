using System;
using System.Collections;
using UnityEngine;

namespace Tomorrow
{
    /// <summary>
    /// Chapter 2 — the harbor. Two debts at once on the Due Board; the earlier one comes first.
    /// Lost anchor → two boats, one rope → fog on the water → the seagull thief → Oskar's old boat → sunset on the deck.
    /// </summary>
    public class Chapter2Director : ChapterDirector
    {
        [Header("Harbor")]
        public GameObject oskar;
        public Transform oskarsBoat, boatA, boatB, sundayPromise, gull, floatingBox;
        public GameObject dueBoard, doorNote, anchor, magnetOnLine, ropeLoop, fogHiddenParts;

        bool boatDrifting;
        Vector3 boatHome;
        bool lanternOut;
        bool gullFrozen;

        protected override void Setup()
        {
            clock.secondsPerHour = 110f;
            boatHome = oskarsBoat ? oskarsBoat.position : Vector3.zero;
            Show("Anchor", false);
            Show("Magnet On Line", false);
            Show("Rope Loop", false);
            if (floatingBox) floatingBox.gameObject.SetActive(false);
            if (doorNote) doorNote.SetActive(false);
            if (fogHiddenParts) SetHiddenParts(false);
        }

        protected override void Update()
        {
            base.Update();
            if (boatDrifting && oskarsBoat)
                oskarsBoat.position = Vector3.MoveTowards(oskarsBoat.position, boatHome + new Vector3(6f, 0, 4f), Time.deltaTime * 0.08f);
            if (lanternOut) RevealNearLantern();
            if (gullFrozen && UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.pKey.wasPressedThisFrame)
                TakePhoto();
        }

        protected override int ChapterNumber => 2;
        protected override string ChapterTitle => "The Harbor";
        protected override string NextChapterTitle => "Market Day";

        protected override IEnumerator Run()
        {
            yield return Opening();
            yield return Part(1, "The Lost Anchor", Puzzle1_LostAnchor());
            yield return Part(2, "Two Boats, One Rope", Puzzle2_TwoBoats());
            yield return Part(3, "Fog on the Water", Puzzle3_Fog());
            yield return Part(4, "The Seagull Thief", Puzzle4_SeagullThief());
            yield return Part(5, "The Sunday Promise", Puzzle5_SundayPromise());
            yield return Ending();
        }

        // ---------------------------------------------------------- Opening
        IEnumerator Opening()
        {
            cutscene = true;
            clock.hour = 7.5f; clock.cap = 18f; clock.running = false;
            ui.SetHudVisible(false);
            // Pip asleep over the bench, letter in hand
            player.visual.localRotation = Quaternion.Euler(28, 0, 0);
            inv.Add(ItemId.Letter);
            tock.gameObject.SetActive(true);
            tock.active = false;
            tock.Warp(Spot("Tock_Nose").position);
            tock.transform.rotation = Spot("Tock_Nose").rotation;
            ui.SetCaption("Chapter 2 — The workshop, morning");
            yield return ui.Fade(0f, 2f);
            yield return Wait(1.2f);
            ui.SetCaption("");
            sfx.Tick(); yield return Wait(0.25f); sfx.Tick();
            yield return Say(TOCK, "Wake up. The clock ticked twice last night. Twice!");
            player.visual.localRotation = Quaternion.identity;
            yield return Say(PIP, "(sleepy) Did I… write that letter?");
            yield return Say(TOCK, "Not yet. That's the scary part.");

            if (dueBoard) Visuals.Glow(dueBoard, new Color(0.45f, 0.75f, 1f), 1.6f);
            ui.Toast("The Due Board glows. Two empty slots light up.");
            yield return LookAt("Cam_DueBoard", 1.4f);
            yield return Say(TOCK, "New rule. Today you can owe two things at once. Look at the board. Always look at the board.");
            cam.Release();

            // a note slides under the door
            if (doorNote)
            {
                doorNote.SetActive(true);
                var from = doorNote.transform.position;
                var to = Spot("DoorNote_End").position;
                for (float a = 0; a < 0.8f; a += Time.deltaTime) { doorNote.transform.position = Vector3.Lerp(from, to, a / 0.8f); yield return null; }
            }
            ui.SetHudVisible(true);
            tock.follow = player.transform;
            tock.active = true;
            sfx.Ticking(true);
            cutscene = false;

            Objective("A note slid under the door. Read it.");
            bool read = false;
            On("DoorNote", () => ReadOskarNote(() => read = true));
            while (!read) yield return null;

            Objective("Go to the harbor (out the door)");
            bool left = false;
            On("Door", () => LeaveForHarbor(() => left = true));
            while (!left) yield return null;
            clock.running = true;
            toolsOn = true;
        }

        IEnumerator ReadOskarNote(Action done)
        {
            Off("DoorNote");
            if (doorNote) doorNote.SetActive(false);
            yield return Note("A note in Oskar's handwriting", "\"Harbor. Now.\nBring that talking watch.\n\n— O.\"", OSKAR);
            done();
        }

        IEnumerator LeaveForHarbor(Action done)
        {
            Off("Door");
            yield return TravelTo("Harbor_Arrive", "The harbor");
            done();
        }

        // ---------------------------------------------------------- Puzzle 1 — Lost Anchor
        IEnumerator Puzzle1_LostAnchor()
        {
            boatDrifting = true;
            Objective("Captain Oskar is waiting on the dock");
            bool talked = false;
            On("Oskar", () => OskarAnchor(() => talked = true));
            while (!talked) yield return null;

            yield return OpenBox("Box1", "Inside: a big red magnet.", Loan(ItemId.Magnet, 10f));

            Objective("Lower the magnet on a rope from the end of the dock");
            bool fished = false;
            On("AnchorSpot", () => LowerMagnet(() => fished = true));
            while (!fished) yield return null;

            Objective("Build a magnet at the harbor bench (iron bar + copper wire) and drop it in the Return Box");
            while (!Repaid(ItemId.Magnet)) yield return null;
            yield return Say(OSKAR, "Your grandma did that trick once. Same anchor. Same fish, probably.");
            yield return Say(PIP, "Grandma came here?");
            yield return Say(OSKAR, "Every Sunday. Sit. I'll tell you later. Work first.");
        }

        IEnumerator OskarAnchor(Action done)
        {
            Off("Oskar");
            yield return Say(OSKAR, "Chain snapped. Anchor's down there with the fish.");
            yield return Say(PIP, "I'll get it!");
            yield return Say(TOCK, "You can't swim.");
            yield return Say(PIP, "I'll get it… differently!");
            DropBox("Drop_Dock", "Box1");
            yield return Wait(1.2f);
            done();
        }

        IEnumerator LowerMagnet(Action done)
        {
            Off("AnchorSpot");
            inv.Remove(ItemId.Magnet);
            var line = Find("Magnet On Line");
            var anc = Find("Anchor");
            yield return LookAt("Cam_Anchor", 0.6f);
            if (line)
            {
                line.SetActive(true);
                var top = line.transform.position;
                for (float a = 0; a < 1f; a += Time.deltaTime) { line.transform.position = top + Vector3.down * a * 1.2f; yield return null; }
                sfx.Build();
                if (anc) anc.SetActive(true);
                for (float a = 0; a < 1.2f; a += Time.deltaTime)
                {
                    line.transform.position = top + Vector3.down * (1.2f - a);
                    if (anc) anc.transform.position = line.transform.position + Vector3.down * 0.25f;
                    yield return null;
                }
                line.SetActive(false);
            }
            ui.Toast("Clank! The anchor comes up — with an old iron bar and a coil of copper wire.");
            inv.Add(ItemId.IronBar); inv.Add(ItemId.CopperWire);
            sfx.Pickup();
            boatDrifting = false;
            StartCoroutine(BoatHome());
            yield return Say(TOCK, "Look at that. The magnet fished up its own parts. Tomorrow is showing off.");
            cam.Release();
            done();
        }

        IEnumerator BoatHome()
        {
            while (oskarsBoat && Vector3.Distance(oskarsBoat.position, boatHome) > 0.02f)
            {
                oskarsBoat.position = Vector3.MoveTowards(oskarsBoat.position, boatHome, Time.deltaTime * 0.8f);
                yield return null;
            }
        }

        // ---------------------------------------------------------- Puzzle 2 — Two Boats, One Rope
        IEnumerator Puzzle2_TwoBoats()
        {
            AtLeastHour(9.5f);
            Objective("The tide is turning — two fishing boats need tying to the dock");
            DropBox("Drop_Dock", "Box2");
            yield return OpenBox("Box2", "Inside: one long rope — and a spanner for the rusty dock cleats.",
                Loan(ItemId.LongRope, 12f), Loan(ItemId.Spanner, 18f));
            ui.Toast("The Due Board: Rope (due at noon) · Magnet (returned) · Spanner (due at the bell)");
            yield return Say(TOCK, "Careful. When you owe two things, the earlier one comes first. Always.");
            yield return Say(PIP, "So I tie both boats with one rope?");
            yield return Say(TOCK, "You tie both boats with one rope. Then you make three short ropes into one long rope. Then you send it back. Easy.");
            yield return Say(PIP, "You keep saying \"easy.\"");
            yield return Say(TOCK, "It keeps not being easy. I'm an optimist.");

            Objective("Tie both boats to the dock with the long rope");
            bool a = false, b = false;
            On("BoatA", () => TieBoat("BoatA", () => a = true));
            On("BoatB", () => TieBoat("BoatB", () => b = true));
            while (!(a && b)) yield return null;
            inv.Remove(ItemId.LongRope);
            inv.Remove(ItemId.Spanner);
            Show("Rope Loop", true);
            ui.Toast("Both boats tied in one big loop. Tomorrow's rope and spanner did their job.");

            Objective("Find 3 short ropes on the dock, braid them at the bench, and return the long rope before noon");
            Pickups(true, "Pickup_ShortRope");
            while (!Repaid(ItemId.LongRope)) yield return null;
        }

        IEnumerator TieBoat(string id, Action done)
        {
            Off(id);
            sfx.Build();
            ui.Toast(id == "BoatA" ? "The cleat is rusted solid — tomorrow's spanner loosens it. First boat tied!" : "Second boat tied — same rope, one loop!");
            yield return Wait(0.6f);
            done();
        }

        // ---------------------------------------------------------- Puzzle 3 — Fog on the Water
        IEnumerator Puzzle3_Fog()
        {
            AtLeastHour(11.5f);
            yield return FogTo(0.16f, 2f);
            yield return Say(OSKAR, "Fog like this, you can't see your own boots.");
            DropBox("Drop_Harbor", "Box3");
            yield return OpenBox("Box3", "Inside: a lantern.", Loan(ItemId.Lantern, 14f));
            lanternOut = true;
            var lamp = new GameObject("Lantern Light").AddComponent<Light>();
            lamp.type = LightType.Point; lamp.color = new Color(1f, 0.85f, 0.5f); lamp.range = 4f; lamp.intensity = 2.5f;
            lamp.transform.SetParent(player.transform, false); lamp.transform.localPosition = new Vector3(0.25f, 0.7f, 0.1f);
            SetHiddenParts(true, false);
            Objective("Hold up the lantern — walk around the dock to find what's hidden in the fog");
            yield return WaitUntilRevealed();
            yield return Say(PIP, "The light shows things!");
            yield return Say(TOCK, "Tomorrow's light. It remembers where things will be.");

            Objective("Collect the jam jar, glow-flower and wire, build the lantern, and return it");
            while (!Repaid(ItemId.Lantern)) yield return null;
            lanternOut = false;
            Destroy(lamp.gameObject);
            yield return FogTo(0f, 3f);
            ui.Toast("The fog lifts. The sea shines.");
            cutscene = true;
            yield return LookAt("Cam_Sea", 1.2f);
            yield return Say(OSKAR, "(quietly) Ada loved this view. Said the sea was just the sky lying down.");
            cam.Release();
            cutscene = false;
        }

        IEnumerator FogTo(float density, float secs)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            float from = RenderSettings.fogDensity;
            for (float t = 0; t < secs; t += Time.deltaTime) { RenderSettings.fogDensity = Mathf.Lerp(from, density, t / secs); yield return null; }
            RenderSettings.fogDensity = density;
            if (density <= 0f) RenderSettings.fog = false;
        }

        bool anyRevealed;
        void SetHiddenParts(bool active, bool visible = false)
        {
            if (!fogHiddenParts) return;
            fogHiddenParts.SetActive(active);
            foreach (var it in fogHiddenParts.GetComponentsInChildren<Interactable>(true))
            {
                it.gameObject.SetActive(active);
                it.available = visible;
                foreach (var r in it.GetComponentsInChildren<Renderer>(true)) r.enabled = visible;
                foreach (var l in it.GetComponentsInChildren<Light>(true)) l.enabled = visible;
            }
        }

        void RevealNearLantern()
        {
            if (!fogHiddenParts) return;
            foreach (var it in fogHiddenParts.GetComponentsInChildren<Interactable>())
            {
                if (it.available) continue;
                if (Vector3.Distance(Flat(it.transform.position), Flat(player.transform.position)) > 3.2f) continue;
                it.available = true;
                foreach (var r in it.GetComponentsInChildren<Renderer>(true)) { r.enabled = true; }
                foreach (var l in it.GetComponentsInChildren<Light>(true)) { l.enabled = true; l.color = new Color(1f, 0.9f, 0.4f); l.intensity = 2.5f; }
                Visuals.Glow(it.gameObject, new Color(1f, 0.85f, 0.3f), 1.5f);
                sfx.Pickup();
                ui.Toast("A glowing outline appears in the fog!");
                anyRevealed = true;
            }
        }

        IEnumerator WaitUntilRevealed() { while (!anyRevealed) yield return null; }

        // ---------------------------------------------------------- Puzzle 4 — The Seagull Thief
        IEnumerator Puzzle4_SeagullThief()
        {
            AtLeastHour(13.5f);
            inv.Add(ItemId.String); inv.Add(ItemId.Stick);
            cutscene = true;
            yield return Wait(0.5f);
            yield return GullSwoop();
            inv.Remove(ItemId.String); inv.Remove(ItemId.Stick);
            yield return Say(PIP, "Hey! That's MY string!");
            yield return Say(TOCK, "Technically, it's tomorrow's string.");
            cutscene = false;
            DropBox("Drop_Harbor", "Box4");
            yield return OpenBox("Box4", "Inside: a fishing net.", Loan(ItemId.FishingNet, 16f));

            Objective("Climb the lighthouse stairs and catch the seagull — gently");
            bool caught = false;
            On("Lighthouse", () => CatchGull(() => caught = true));
            while (!caught) yield return null;

            Objective("Build a net from the string and two sticks, then return it");
            Pickup("Pickup_Stick2");
            while (!Repaid(ItemId.FishingNet)) yield return null;
            if (gullFrozen) yield return UnfreezeGull();
        }

        IEnumerator GullSwoop()
        {
            if (!gull) yield break;
            var start = gull.position;
            var pip = player.transform.position + Vector3.up * 0.6f;
            var perch = Spot("Gull_Perch").position;
            sfx.Squawk();
            for (float a = 0; a < 1f; a += Time.deltaTime) { gull.position = Vector3.Lerp(start, pip, Mathf.SmoothStep(0, 1, a)); gull.LookAt(pip); yield return null; }
            ui.Toast("A cheeky seagull snatches the string and a stick from Pip's bag!");
            for (float a = 0; a < 2f; a += Time.deltaTime)
            {
                float k = a / 2f;
                gull.position = Vector3.Lerp(pip, perch, Mathf.SmoothStep(0, 1, k)) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 1.5f;
                gull.rotation = Quaternion.LookRotation(Flat(perch - pip).normalized + Vector3.up * 0.01f);
                yield return null;
            }
            gull.position = perch;
            var idle = gull.GetComponent<Idle>(); if (idle) idle.enabled = false;
        }

        IEnumerator CatchGull(Action done)
        {
            if (!inv.Has(ItemId.FishingNet))
            {
                yield return Say(TOCK, "Up there with bare hands? Open tomorrow's box first. The net.");
                yield break;
            }
            Off("Lighthouse");
            inv.Remove(ItemId.FishingNet);
            yield return ui.Fade(1f, 0.5f);
            ui.SetCaption("Pip climbs the lighthouse stairs… round and round and round…");
            yield return Wait(1.6f);
            cam.Focus(Spot("Cam_LighthouseTop"));
            cam.transform.SetPositionAndRotation(Spot("Cam_LighthouseTop").position, Spot("Cam_LighthouseTop").rotation);
            ui.SetCaption("");
            yield return ui.Fade(0f, 0.5f);
            ui.Toast("Swish! The net — gently — and the seagull drops everything.");
            sfx.Squawk();
            yield return Wait(1f);
            yield return Say(PIP, "Sorry, bird. You can keep the button.");
            sfx.Squawk();
            yield return Say(SEAGULL, "(happy squawk)");
            yield return ui.Fade(1f, 0.4f);
            cam.Release();
            cam.SnapNow();
            yield return ui.Fade(0f, 0.5f);
            Pickup("Pickup_String"); Pickup("Pickup_Stick1"); Pickup("Pickup_Button");
            if (gull) { var idle = gull.GetComponent<Idle>(); if (idle) idle.enabled = true; }
            done();
        }

        protected override IEnumerator OnLate(Debt d)
        {
            if (d.item != ItemId.FishingNet) { yield return base.OnLate(d); yield break; }
            // FUNNY FREEZE: the seagull stops mid-air, upside down, still holding the string.
            sfx.Bell();
            gullFrozen = true;
            if (gull)
            {
                var idle = gull.GetComponent<Idle>(); if (idle) idle.enabled = false;
                gull.position = Spot("Gull_Freeze").position;
                gull.rotation = Quaternion.Euler(0, 200, 180);
            }
            yield return LookAt("Cam_GullFreeze", 0.8f);
            ui.SetCaption("The seagull froze mid-air. Upside down. Still holding the string.");
            ui.SetCaptionSub("Press P to take a photo!");
            yield return Wait(2.6f);
            ui.SetCaption("");
            cam.Release();
            yield return Say(TOCK, "Late. Tomorrow charges interest: one extra string. Also, that's the best photo you'll ever take.");
            Owe(new Debt { item = ItemId.String, fromWhom = "Tomorrow (interest)" });
            OnInterestOwed(ItemId.String);
        }

        IEnumerator UnfreezeGull()
        {
            gullFrozen = false;
            sfx.Squawk();
            if (gull) { gull.rotation = Quaternion.Euler(0, 200, 0); var idle = gull.GetComponent<Idle>(); if (idle) idle.enabled = true; }
            ui.Toast("The seagull unfreezes, squawks, and flaps off in a huff.");
            yield return null;
        }

        void TakePhoto()
        {
            var dir = System.IO.Path.Combine(Application.persistentDataPath, "Photos");
            System.IO.Directory.CreateDirectory(dir);
            var path = System.IO.Path.Combine(dir, $"seagull_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            ScreenCapture.CaptureScreenshot(path);
            sfx.Build();
            ui.Toast("Click! Photo saved to " + path, 4f);
        }

        // ---------------------------------------------------------- Puzzle 5 — Oskar's Old Boat
        IEnumerator Puzzle5_SundayPromise()
        {
            AtLeastHour(16.25f);
            Objective("Talk to Oskar about his old boat");
            bool talked = false;
            On("Oskar", () => OskarSunday(() => talked = true));
            while (!talked) yield return null;

            DropBox("Drop_Dock", "Box5");
            yield return OpenBox("Box5", "Inside: an engine part.", Loan(ItemId.EnginePart, 18f));
            yield return Note("A tag on the engine part", "RETURN BY: BELL.\nTWO DEBTS OPEN.");
            ui.Toast("The Due Board also shows the Spanner — borrowed earlier for the dock.");
            yield return Say(TOCK, "Two debts, one bell. Pick the order wisely.");

            Objective("Fit the engine part into the Sunday Promise");
            bool fixedIt = false;
            On("SundayEngine", () => FixEngine(() => fixedIt = true));
            while (!fixedIt) yield return null;

            Objective("Find Grandma's old toolbox on the island");
            bool found = false;
            On("Toolbox", () => OpenToolbox(() => found = true));
            while (!found) yield return null;

            Objective("Build the engine part and a spanner with Grandma's tools, sail back, and return both before the bell");
            On("SailBack", SailBack);
            while (!AllRepaid) yield return null;
            Off("SailBack");
            sfx.Bell();
            ui.Toast("The bell rings over the harbor. Every debt is paid.");
            yield return Wait(1.5f);
        }

        IEnumerator OskarSunday(Action done)
        {
            Off("Oskar");
            yield return Say(OSKAR, "That boat hasn't moved in twenty years. Not since Ada and I…");
            yield return Say(PIP, "Since you what?");
            yield return Say(OSKAR, "Since we stopped going on Sundays. Fix the engine. Then we talk.");
            done();
        }

        IEnumerator FixEngine(Action done)
        {
            if (!inv.Has(ItemId.EnginePart)) { yield return Say(TOCK, "The engine needs tomorrow's part first."); yield break; }
            Off("SundayEngine");
            inv.Remove(ItemId.EnginePart);
            sfx.Build();
            ui.Toast("Put-put-put… The Sunday Promise coughs awake!");
            yield return Sail(true);
            done();
        }

        IEnumerator Sail(bool toIsland)
        {
            yield return ui.Fade(1f, 0.6f);
            ui.SetCaption(toIsland ? "Sailing to the little island…" : "Sailing back to the harbor…");
            var from = Spot(toIsland ? "Boat_Harbor" : "Boat_Island");
            var to = Spot(toIsland ? "Boat_Island" : "Boat_Harbor");
            sundayPromise.SetPositionAndRotation(from.position, from.rotation);
            cam.Focus(Spot("Cam_Sail"));
            var camPose = Spot("Cam_Sail");
            cam.transform.SetPositionAndRotation(camPose.position, camPose.rotation);
            yield return ui.Fade(0f, 0.5f);
            for (float a = 0; a < 2.5f; a += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, a / 2.5f);
                sundayPromise.position = Vector3.Lerp(from.position, to.position, k);
                camPose.position = sundayPromise.position + new Vector3(-6f, 4f, -6f);
                camPose.rotation = Quaternion.LookRotation(sundayPromise.position - camPose.position);
                yield return null;
            }
            sundayPromise.SetPositionAndRotation(to.position, to.rotation);
            yield return ui.Fade(1f, 0.5f);
            ui.SetCaption("");
            cam.Release();
            var arrive = Spot(toIsland ? "Island_Arrive" : "Harbor_FromSea");
            player.Teleport(arrive.position, arrive.eulerAngles.y);
            tock.Warp(arrive.position + arrive.right * 0.6f);
            if (oskar) oskar.transform.position = Spot(toIsland ? "Oskar_Island" : "Oskar_Dock").position;
            cam.yaw = arrive.eulerAngles.y; cam.SnapNow();
            yield return ui.Fade(0f, 0.6f);
        }

        IEnumerator OpenToolbox(Action done)
        {
            Off("Toolbox");
            ui.Toast("Grandma's old toolbox! Inside: a gear, a spring, an iron bar and a bolt.");
            inv.Add(ItemId.Gear); inv.Add(ItemId.Spring); inv.Add(ItemId.IronBar); inv.Add(ItemId.Bolt);
            sfx.Pickup();
            yield return Wait(0.6f);
            done();
        }

        IEnumerator SailBack()
        {
            Off("SailBack");
            yield return Sail(false);
        }

        // ---------------------------------------------------------- Ending
        IEnumerator Ending()
        {
            cutscene = true;
            Objective(null);
            yield return ui.Fade(1f, 1.2f);
            clock.cap = 19.5f; clock.hour = 18.8f; clock.running = false;
            // everyone sits on the deck of the Sunday Promise
            var deck = Spot("Deck_Pip");
            player.Freeze(true);
            player.transform.SetPositionAndRotation(deck.position, deck.rotation);
            tock.active = false;
            tock.Warp(Spot("Deck_Tock").position);
            tock.transform.rotation = Spot("Deck_Tock").rotation;
            if (oskar) oskar.transform.SetPositionAndRotation(Spot("Deck_Oskar").position, Spot("Deck_Oskar").rotation);
            cam.Focus(Spot("Cam_Deck"));
            cam.transform.SetPositionAndRotation(Spot("Cam_Deck").position, Spot("Cam_Deck").rotation);
            ui.SetHudVisible(false);
            ui.SetCaption("On the boat, sunset");
            yield return ui.Fade(0f, 1.5f);
            yield return Wait(1.2f);
            ui.SetCaption("");
            yield return Say(OSKAR, "Ada and I came here every Sunday. She fixed clocks. I fixed boats. We fixed each other's bad days.");
            yield return Say(PIP, "Why did you stop?");
            yield return Say(OSKAR, "One Sunday she said, \"I borrowed something big, Oskar. Something I can't give back yet.\" Then she stopped coming.");
            yield return Say(PIP, "What did she borrow?");
            yield return Say(OSKAR, "She wouldn't say. But she looked at you a lot that year, kid. Like you were the thing she was afraid to lose.");
            ui.SetCaption("Silence. The waves rock the boat.");
            yield return Wait(2.2f);
            ui.SetCaption("");
            // a tiny Tomorrow Box floats by
            if (floatingBox)
            {
                floatingBox.gameObject.SetActive(true);
                var from = floatingBox.position; var to = Spot("FloatBox_End").position;
                for (float a = 0; a < 3f; a += Time.deltaTime)
                {
                    floatingBox.position = Vector3.Lerp(from, to, a / 3f) + Vector3.up * Mathf.Sin(a * 3f) * 0.03f;
                    yield return null;
                }
            }
            ui.Toast("A tiny Tomorrow Box floats by on the water. Inside: a note.");
            yield return Wait(1f);
            yield return Note("A note (in your older handwriting)", "\"Be kind to Nell tomorrow.\nYou'll need her more than you think.\n\n— P.\"", OLDPIP);
            yield return Say(TOCK, "You give very short advice in the future.");
            yield return Say(PIP, "I'm busy, apparently.");
            yield return ui.Fade(1f, 2f);
            player.Freeze(false);
        }
    }
}
