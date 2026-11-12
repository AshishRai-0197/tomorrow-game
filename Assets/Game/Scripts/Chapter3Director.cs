using System;
using System.Collections;
using UnityEngine;

namespace Tomorrow
{
    /// <summary>
    /// Chapter 3 — Main Street and the greenhouse. Pink boxes come from a friend's tomorrow:
    /// late costs that friend a heart, not the town. Market day → seed mix-up → the wilted gift →
    /// trading promises → the glass roof → Grandma's diary page.
    /// </summary>
    public class Chapter3Director : ChapterDirector
    {
        [Header("Cast")]
        public GameObject nell, mira;

        [Header("Market and greenhouse")]
        public Transform cart, mirror, iceBlock, rollingFruit;
        public GameObject roseBush, carrots, glowSprouts, mud, roofCrack, sunBeam;

        bool miraFirst;
        int mirrorStep;
        const int MirrorSolved = 3;     // presses (30° each) until the beam hits the frozen corner
        bool toldOtherLeft;

        protected override void Setup()
        {
            clock.secondsPerHour = 100f;
            Show("Fixed Bridge", true);
            Show("Broken Bridge", false);
            Show("River_Block_Gap", false);
            Show("Flour Cloud", false);
            Show("Grey Spot", false);
            Show("Town Glow Flowers", false);   // Chapter 1's street flowers would crowd the market
            var ol = Find("Oven Light"); if (ol) ol.GetComponent<Light>().enabled = true;
            var tl = Find("Tower Light"); if (tl) tl.GetComponent<Light>().enabled = true;
            if (glowSprouts) glowSprouts.SetActive(false);
            if (sunBeam) sunBeam.SetActive(false);
            if (rollingFruit) rollingFruit.gameObject.SetActive(false);
            if (roseBush) Visuals.Tint(roseBush, new Color(0.45f, 0.38f, 0.3f), 0.8f);   // wilted
            if (mirror) mirror.gameObject.SetActive(false);
        }

        protected override int ChapterNumber => 3;
        protected override string ChapterTitle => "Market Day";
        protected override string NextChapterTitle => "";

        protected override IEnumerator Run()
        {
            yield return Opening();
            yield return Part(1, "Market Day", Puzzle1_MarketDay());
            yield return Part(2, "Seeds for Tomorrow", Puzzle2_Seeds());
            yield return Part(3, "Grandma's Roses", Puzzle3_WiltedGift());
            yield return Part(4, "Trading Promises", Puzzle4_TradingPromises());
            yield return Part(5, "The Glass Roof", Puzzle5_GlassRoof());
            yield return Ending();
        }

        // ---------------------------------------------------------- Opening
        IEnumerator Opening()
        {
            cutscene = true;
            clock.hour = 8.5f; clock.cap = 18.5f; clock.running = false;
            tock.gameObject.SetActive(true);
            tock.follow = player.transform;
            tock.active = true;
            sfx.Ticking(true);
            ui.SetHudVisible(false);
            ui.SetCaption("Chapter 3 — Main Street, morning");
            yield return ui.Fade(0f, 2f);
            yield return Wait(1.2f);
            ui.SetCaption("");
            // Nell runs up, dirt on her nose
            yield return MoveTo(nell.transform, Spot("Nell_Greet").position, 2.4f);
            yield return Say(NELL, "Pip! The market is a mess. Mira's scale broke, and nobody can sell anything.");
            yield return Say(PIP, "I can fix it.");
            yield return Say(NELL, "You always say that.");
            yield return Say(PIP, "And I'm always… mostly right!");
            yield return Say(TOCK, "Fifty percent. Generously.");
            ui.SetHudVisible(true);
            cutscene = false;
            clock.running = true;
            toolsOn = true;
        }

        void Place(GameObject who, string spot)
        {
            var s = Spot(spot);
            if (s) who.transform.SetPositionAndRotation(s.position, s.rotation);
        }

        IEnumerator MoveTo(Transform who, Vector3 to, float speed)
        {
            to.y = who.position.y;
            while (Vector3.Distance(who.position, to) > 0.05f)
            {
                var step = Vector3.MoveTowards(who.position, to, speed * Time.deltaTime);
                var d = to - who.position; d.y = 0;
                if (d.sqrMagnitude > 0.001f) who.rotation = Quaternion.LookRotation(d);
                who.position = step;
                yield return null;
            }
        }

        // ---------------------------------------------------------- Puzzle 1 — Market Day
        IEnumerator Puzzle1_MarketDay()
        {
            Objective("Talk to Mira at her market stall");
            bool talked = false;
            On("Mira", () => Talk(() => talked = true, (MIRA, "No scale, no market! I can't just GUESS how much a pumpkin weighs.")));
            while (!talked) yield return null;

            DropBox("Drop_Stall", "Box1", "Open the pink-ribbon box", pink: true);
            yield return Wait(1.3f);
            yield return Note("A tag on the pink ribbon", "FROM: MIRA'S TOMORROW.");
            yield return Say(TOCK, "New kind of box. This one's from a friend's tomorrow. Lower interest. But…");
            yield return Say(PIP, "But?");
            yield return Say(TOCK, "If you're late, Mira has a bad day. Not the town. Her.");
            SetHearts("Mira", 3);
            ui.Toast("Three small hearts appear above Mira's head.");
            yield return OpenBox("Box1", "Inside: Mira's scale — from tomorrow.", Loan(ItemId.Scale, 11f, "Mira"));
            yield return Say(MIRA, "Is that… MY scale? From tomorrow? Then it's still mine. Bring it back, sweetie.");

            Objective("Set the scale up on Mira's stall");
            bool weighed = false;
            On("Stall", () => UseScale(() => weighed = true));
            while (!weighed) yield return null;

            Objective("Find a spring and a brass plate, build the scale, and return it");
            Pickup("Pickup_Spring"); Pickup("Pickup_BrassPlate");
            while (!Repaid(ItemId.Scale)) yield return null;
            if (!Find(ItemId.Scale).lateHandled)
            {
                yield return Say(MIRA, "Right on time! You get a free apple. Don't tell anyone.");
                inv.Add(ItemId.Apple);
            }
        }

        IEnumerator Talk(Action done, params (string who, string line)[] lines)
        {
            foreach (var l in lines) yield return Say(l.who, l.line);
            done();
        }

        IEnumerator UseScale(Action done)
        {
            Off("Stall");
            inv.Remove(ItemId.Scale);
            Show("Stall Scale", true);
            sfx.Build();
            ui.Toast("Pumpkin: 4 kilos exactly. The queue cheers. Market's open!");
            yield return Wait(0.6f);
            done();
        }

        // ---------------------------------------------------------- Puzzle 2 — Nell's Seed Mix-up
        IEnumerator Puzzle2_Seeds()
        {
            AtLeastHour(9.5f);
            Place(nell, "Nell_Bed");
            Objective("Nell is staring at her flower bed by the greenhouse");
            bool talked = false;
            On("Nell", () => Talk(() => talked = true, (NELL, "I wanted flowers for Grandma Ada's window. Now it's carrots. Very proud carrots.")));
            while (!talked) yield return null;

            DropBox("Drop_Bed", "Box2");
            yield return OpenBox("Box2", "Inside: a packet of glow-flower seeds.", Loan(ItemId.SeedPacket, 13.5f));
            yield return Say(TOCK, "Plant them today. They grow. They make new seeds. You send the seeds back. It's farming, but confusing.");

            Objective("Plant the glow-flower seeds in Nell's bed");
            bool planted = false;
            On("FlowerBed", () => Plant(() => planted = true));
            while (!planted) yield return null;

            int tended = 0;
            Objective("Tend the bed — time moves with each action (3 times)");
            while (tended < 3)
            {
                bool t = false;
                On("FlowerBed", () => Tend(++tended, () => t = true));
                while (!t) yield return null;
            }
            Off("FlowerBed");
            Pickups(true, "Pickup_GlowSeed");
            ui.Toast("The flowers bloom — and drop shiny new seeds!");
            Objective("Collect the 3 new seeds, fill the packet at a workbench, and return it");
            while (!Repaid(ItemId.SeedPacket)) yield return null;
            yield return Say(NELL, "They glow! Grandma always said glow-flowers only grow for people who keep promises.");
            yield return Say(PIP, "Then I'd better keep mine.");
        }

        IEnumerator Plant(Action done)
        {
            Off("FlowerBed");
            inv.Remove(ItemId.SeedPacket);
            if (carrots) carrots.SetActive(false);
            sfx.Build();
            ui.Toast("Seeds planted. The carrots move politely aside.");
            yield return Wait(0.5f);
            done();
        }

        IEnumerator Tend(int n, Action done)
        {
            Off("FlowerBed");
            clock.hour += 0.75f;
            if (glowSprouts)
            {
                glowSprouts.SetActive(true);
                glowSprouts.transform.localScale = Vector3.one * (0.3f + 0.25f * n);
                if (n >= 3) Visuals.Glow(glowSprouts, new Color(1f, 0.85f, 0.4f), 1.4f);
            }
            sfx.Pickup();
            ui.Toast(n == 1 ? "Water. Sprouts!" : n == 2 ? "Sunshine. Buds!" : "A little song. They bloom!");
            yield return Wait(0.4f);
            done();
        }

        // ---------------------------------------------------------- Puzzle 3 — The Wilted Gift
        IEnumerator Puzzle3_WiltedGift()
        {
            clock.hour = Mathf.Max(clock.hour, 15.5f);
            clock.secondsPerHour = 70f;
            clock.cap = 21f;
            Place(nell, "Nell_Roses");
            Objective("Grandma's old rose bush is dying. Talk to Nell (by the workshop door).");
            bool talked = false;
            On("Nell", () => Talk(() => talked = true,
                (NELL, "You can borrow my watering can. From my tomorrow. But I need it back by sunset. It's… important."),
                (PIP, "Why sunset?"),
                (NELL, "Tomorrow is my birthday. Grandma Ada always watered the birthday roses with me. Every year.")));
            while (!talked) yield return null;
            SetHearts("Nell", 3);

            DropBox("Drop_Roses", "Box3", "Open the pink-ribbon box", pink: true);
            yield return OpenBox("Box3", "Inside: Nell's watering can — from her tomorrow.", Loan(ItemId.WateringCan, 19f, "Nell"));

            Objective("Water Grandma's roses (by the workshop door)");
            bool watered = false;
            On("Roses", () => WaterRoses(() => watered = true));
            while (!watered) yield return null;

            Objective("Find a tin can, a pipe and a handle, rebuild the watering can, and give it back before sunset");
            Pickup("Pickup_TinCan"); Pickup("Pickup_IronPipe"); Pickup("Pickup_Handle");
            while (!Repaid(ItemId.WateringCan)) yield return null;
            clock.running = false;

            if (!Find(ItemId.WateringCan).lateHandled)
            {
                yield return Say(NELL, "You brought it back. You remembered.");
                yield return Say(PIP, "Of course. Happy almost-birthday.");
            }
            else
            {
                ui.Toast("Nell takes the can without a word.");
                yield return Wait(1.2f);
            }
        }

        IEnumerator WaterRoses(Action done)
        {
            Off("Roses");
            inv.Remove(ItemId.WateringCan);
            sfx.Pickup();
            for (float a = 0; a < 1.5f; a += Time.deltaTime) { Visuals.Tint(roseBush, new Color(0.45f, 0.38f, 0.3f), 0.8f * (1 - a / 1.5f)); yield return null; }
            Visuals.Tint(roseBush, Color.white, 0f);
            Visuals.Glow(roseBush, new Color(1f, 0.3f, 0.35f), 0.5f);
            ui.Toast("The roses drink, lift their heads… and bloom.");
            yield return Wait(0.6f);
            done();
        }

        protected override IEnumerator OnLate(Debt d)
        {
            if (d.item != ItemId.WateringCan) { yield return base.OnLate(d); yield break; }
            // Nell's sad scene
            LoseHeart("Nell");
            sfx.Bell();
            var spot = Spot("Nell_Roses");
            nell.transform.SetPositionAndRotation(spot.position, spot.rotation);
            yield return LookAt("Cam_Roses", 0.8f);
            ui.SetCaption("Nell sits alone by the roses. No watering can.\nShe waters them with her hands, one handful at a time.");
            yield return Wait(3f);
            ui.SetCaption("");
            yield return Say(NELL, "(quietly) It's fine. It's fine.");
            yield return Say(TOCK, "(to Pip, softly) It's not fine.");
            cam.Release();
        }

        // ---------------------------------------------------------- Puzzle 4 — Trading Promises
        IEnumerator Puzzle4_TradingPromises()
        {
            yield return Say(TOCK, "Two friends. Two favors. One Pip. Mira's cart has a broken wheel, and mud has the greenhouse door stuck shut. Choose who you borrow from first.");
            if (tomorrowWindow) Visuals.Glow(tomorrowWindow, new Color(0.4f, 0.75f, 1f), 2.5f);
            Objective("Look into the Tomorrow Window at the workshop");
            bool seen = false;
            On("TomorrowWindow", () => Previews(() => seen = true));
            while (!seen) yield return null;

            int pick = -1;
            cutscene = true;
            yield return ui.Choose("Whose tomorrow do you borrow from first?", "Mira's — her cart wheel", "Nell's — her spade", i => pick = i);
            cutscene = false;
            miraFirst = pick == 0;

            if (miraFirst)
            {
                DropBox("Drop_Cart", "Box4", "Open the pink-ribbon box", pink: true);
                yield return OpenBox("Box4", "Inside: Mira's cart wheel — from her tomorrow.", Loan(ItemId.CartWheel, 99f, "Mira"));
                Objective("Fix Mira's cart with the wheel");
                yield return Until("Cart", FixCart);
                Objective("Build a cart wheel (2 planks + iron bar) and return it to Mira's tomorrow");
                Pickups(true, "Pickup_Plank"); Pickup("Pickup_IronBar");
                while (!Repaid(ItemId.CartWheel)) yield return null;
                Objective("The greenhouse door is still stuck. Maybe the cart can help…");
                yield return Until("Cart", PushCartToGreenhouse);
                yield return Until("Mud", ClearMudWithCart);
            }
            else
            {
                DropBox("Drop_Mud", "Box4", "Open the pink-ribbon box", pink: true);
                yield return OpenBox("Box4", "Inside: Nell's spade — from her tomorrow.", Loan(ItemId.Spade, 99f, "Nell"));
                Objective("Dig the mud away from the greenhouse door");
                yield return Until("Mud", DigMud);
                GainHeart("Nell");
                Objective("Build a spade (stick + tin can) and return it to Nell's tomorrow");
                Pickup("Pickup_Stick"); Pickup("Pickup_TinCan2");
                while (!Repaid(ItemId.Spade)) yield return null;
                Objective("Mira's cart still needs a wheel — build one yourself (2 planks + iron bar)");
                ExtraBuilds.Add(ItemId.CartWheel);
                Pickups(true, "Pickup_Plank"); Pickup("Pickup_IronBar");
                yield return Until("Cart", FixCart);
            }
            yield return Say(PIP, "I think the trick is… helping one friend can help the other.");
            yield return Say(TOCK, "That was almost wise. Write it down.");
        }

        IEnumerator Until(string id, Func<Action, IEnumerator> handler)
        {
            bool done = false;
            On(id, () => handler(() => { done = true; Off(id); }));
            while (!done) yield return null;
        }

        IEnumerator Previews(Action done)
        {
            Off("TomorrowWindow");
            yield return WindowPreview("Cam_Stall", "Mira's tomorrow without her cart wheel:\nher fruit rolls down the street. Everyone laughs. She laughs too.", 3.2f,
                () => { if (rollingFruit) { rollingFruit.gameObject.SetActive(true); StartCoroutine(RollFruit()); } },
                () => { if (rollingFruit) rollingFruit.gameObject.SetActive(false); });
            var nellPos = nell.transform.position; var nellRot = nell.transform.rotation;
            yield return WindowPreview("Cam_GreenhouseDoor", "Nell's tomorrow without her spade:\nthe greenhouse door stays shut. Her plants are cold. She cries a little.", 3.2f,
                () => { var s = Spot("Nell_Sad"); nell.transform.SetPositionAndRotation(s.position, s.rotation); },
                () => { nell.transform.SetPositionAndRotation(nellPos, nellRot); });
            yield return Say(PIP, "…Both look bad.");
            yield return Say(TOCK, "Welcome back to adulthood.");
            done();
        }

        IEnumerator RollFruit()
        {
            var start = rollingFruit.position;
            for (float a = 0; a < 3.2f && rollingFruit.gameObject.activeSelf; a += Time.deltaTime)
            {
                rollingFruit.position = start + new Vector3(0, 0, -a * 1.6f);
                foreach (Transform f in rollingFruit) f.Rotate(-400f * Time.deltaTime, 0, 0, Space.World);
                yield return null;
            }
            rollingFruit.position = start;
        }

        IEnumerator FixCart(Action done)
        {
            if (!inv.Has(ItemId.CartWheel)) { yield return Say(TOCK, "The cart needs a wheel. We don't have one. Yet."); yield break; }
            inv.Remove(ItemId.CartWheel);
            Show("Cart Wheel Missing", false); Show("Cart Wheel Fixed", true);
            if (cart) cart.localRotation = Quaternion.identity;
            sfx.Build();
            ui.Toast("The cart stands up straight on four wheels. Mira claps.");
            yield return Wait(0.5f);
            done();
        }

        IEnumerator PushCartToGreenhouse(Action done)
        {
            var to = Spot("Cart_Greenhouse");
            yield return ui.Fade(1f, 0.4f);
            cart.SetPositionAndRotation(to.position, to.rotation);
            player.Teleport(to.position + to.forward * -1.2f + to.right * 0.8f, to.eulerAngles.y);
            tock.Warp(to.position + to.right * 1.4f);
            cam.SnapNow();
            yield return ui.Fade(0f, 0.4f);
            ui.Toast("Pip pushes the cart all the way to the greenhouse.");
            done();
        }

        IEnumerator ClearMudWithCart(Action done)
        {
            yield return ClearMud("Shovel, load, wheel it away. The cart carries the mud off. The door swings open!");
            done();
        }

        IEnumerator DigMud(Action done)
        {
            if (!inv.Has(ItemId.Spade)) { yield return Say(TOCK, "Bare hands? Brave. Also slow. Open the box."); yield break; }
            inv.Remove(ItemId.Spade);
            yield return ClearMud("Dig, dig, dig. The door swings open!");
            done();
        }

        IEnumerator ClearMud(string toast)
        {
            sfx.Build();
            if (mud)
            {
                var t = mud.transform; var s = t.localScale;
                for (float a = 0; a < 1f; a += Time.deltaTime) { t.localScale = s * (1f - a); yield return null; }
                mud.SetActive(false);
            }
            ui.Toast(toast);
            yield return Wait(0.6f);
        }

        // ---------------------------------------------------------- Puzzle 5 — The Glass Roof
        IEnumerator Puzzle5_GlassRoof()
        {
            Place(nell, "Nell_GH");
            Objective("The greenhouse roof is cracked. Go inside.");
            yield return WaitNear("Greenhouse_Inside", 1.6f);
            DropBox("Drop_Greenhouse_In", "Box5");
            yield return OpenBox("Box5", "Inside: glass panes and a mirror.", Loan(ItemId.GlassPanes, 99f), Loan(ItemId.Mirror, 99f));

            Objective("Fix the cracked roof with the glass panes");
            yield return Until("Roof", FixRoof);

            Objective("Set the mirror on its stand and turn it to bounce the sunbeam onto the frozen corner");
            yield return Until("MirrorStand", PlaceMirror);
            bool melted = false;
            On("MirrorStand", () => TurnMirror(() => melted = true));
            while (!melted) yield return null;
            Off("MirrorStand");

            Objective("Something is stuck to the floor where the ice was");
            Pickup("Pickup_AdaPaper");
            while (!inv.Has(ItemId.DiaryPage)) yield return null;
            yield return Say(NELL, "Pip… this is Grandma Ada's handwriting.");

            Objective("Build new panes (2 glass shards + frame) and a mirror (tin can + polish), and return both");
            Pickups(true, "Pickup_GlassShard"); Pickup("Pickup_Frame"); Pickup("Pickup_TinCan3"); Pickup("Pickup_Polish");
            while (!AllRepaid) yield return null;
        }

        IEnumerator FixRoof(Action done)
        {
            if (!inv.Has(ItemId.GlassPanes)) { yield return Say(TOCK, "Glass first. Then roof."); yield break; }
            inv.Remove(ItemId.GlassPanes);
            if (roofCrack) roofCrack.SetActive(false);
            sfx.Build();
            ui.Toast("New panes in. No more cold draught.");
            yield return Wait(0.5f);
            done();
        }

        IEnumerator PlaceMirror(Action done)
        {
            if (!inv.Has(ItemId.Mirror)) { yield return Say(TOCK, "A mirror stand with no mirror is just a stand."); yield break; }
            inv.Remove(ItemId.Mirror);
            mirror.gameObject.SetActive(true);
            if (sunBeam) sunBeam.SetActive(true);
            UpdateBeam();
            sfx.Build();
            done();
            yield break;
        }

        IEnumerator TurnMirror(Action done)
        {
            mirrorStep++;
            for (float a = 0; a < 0.3f; a += Time.deltaTime) { mirror.Rotate(0, 30f * Time.deltaTime / 0.3f, 0, Space.World); UpdateBeam(); yield return null; }
            UpdateBeam();
            if (mirrorStep % 12 == MirrorSolved)
            {
                ui.Toast("The sunbeam hits the frozen corner. The ice melts!");
                if (iceBlock)
                {
                    var s = iceBlock.localScale;
                    for (float a = 0; a < 1.5f; a += Time.deltaTime) { iceBlock.localScale = s * (1f - a / 1.5f); yield return null; }
                    iceBlock.gameObject.SetActive(false);
                }
                done();
            }
            else if (!toldOtherLeft && mirrorStep == 1)
            {
                toldOtherLeft = true;
                yield return Say(NELL, "Left! No, your other left!");
            }
        }

        void UpdateBeam()
        {
            if (!sunBeam || !mirror) return;
            var lr = sunBeam.GetComponent<LineRenderer>();
            var m = mirror.position + Vector3.up * 0.35f;
            var dir = mirror.forward;
            float len = 4f;
            if (Physics.Raycast(m, dir, out var hit, 4f, ~0, QueryTriggerInteraction.Ignore)) len = hit.distance;
            if (mirrorStep % 12 == MirrorSolved && iceBlock) { dir = (iceBlock.position - m).normalized; len = Vector3.Distance(iceBlock.position, m); }
            lr.positionCount = 3;
            lr.SetPosition(0, Spot("Sun_Source").position);
            lr.SetPosition(1, m);
            lr.SetPosition(2, m + dir * len);
        }

        // ---------------------------------------------------------- Ending
        IEnumerator Ending()
        {
            cutscene = true;
            Objective(null);
            yield return ui.Fade(1f, 1.2f);
            clock.hour = 19.4f; clock.cap = 21f; clock.running = false;
            var ps = Spot("Ending_Pip");
            player.Teleport(ps.position, ps.eulerAngles.y);
            var ns = Spot("Ending_Nell");
            nell.transform.SetPositionAndRotation(ns.position, ns.rotation);
            tock.Warp(Spot("Ending_Tock").position);
            cam.Focus(Spot("Cam_Ending"));
            cam.transform.SetPositionAndRotation(Spot("Cam_Ending").position, Spot("Cam_Ending").rotation);
            ui.SetHudVisible(false);
            ui.SetCaption("The greenhouse, evening");
            yield return ui.Fade(0f, 1.5f);
            yield return Wait(1.2f);
            ui.SetCaption("");
            ui.Toast("Pip unfolds the paper. Grandma Ada's voice reads it.");
            yield return Wait(1f);
            yield return Note("Grandma Ada's diary page",
                "\"Today I borrowed a whole year from tomorrow.\nPip laughed for the first time since coming to live with me.\nI know I can't keep it forever.\nBut today, I don't care.\"", ADA);
            ui.SetCaption("Long silence.");
            yield return Wait(2.2f);
            ui.SetCaption("");
            yield return Say(PIP, "She borrowed… a year. For me.");
            yield return Say(NELL, "(takes Pip's hand) Then it's a good thing you're so good at paying things back.");
            sfx.Ticking(true);
            yield return Say(TOCK, "I'm not crying. My glass is just… cracked. More than usual.");
            DropBox("Drop_Table", "Box6", "Open the pink-ribbon box", pink: true);
            yield return Wait(1.3f);
            ui.Toast("Inside: a small birthday card, in Pip's older handwriting.");
            yield return Note("A birthday card", "\"Happy birthday, Nell.\nThank you for not giving up on me.\n\n— P.\"", OLDPIP);
            GainHeart("Nell");
            yield return Say(NELL, "…That's tomorrow's card. You haven't written it yet.");
            yield return Say(PIP, "Then I'd better start practicing.");
            yield return ui.Fade(1f, 2f);
        }
    }
}
