using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Tomorrow
{
    /// <summary>
    /// Shared story engine for every chapter: interaction routing, Tomorrow Boxes, the Due Board
    /// (loans with due times, earliest first), friend hearts, the workbench and the Return Box.
    /// A chapter subclasses this and writes its script in <see cref="Run"/>.
    /// </summary>
    public abstract class ChapterDirector : MonoBehaviour
    {
        public static ChapterDirector Instance { get; private set; }

        [Header("Core")]
        public PlayerController player;
        public FollowCamera cam;
        public GameUI ui;
        public DayClock clock;
        public Sfx sfx;
        public Transform spots;            // named empty transforms: drops, camera poses, spawn points
        public GameObject tomorrowBoxPrefab;
        public Companion tock;

        [Header("Workshop (shared)")]
        public Transform workshopDoor;
        public Collider doorBlocker;
        public GameObject tomorrowWindow;
        public GreatClock greatClock;

        [Header("Flow")]
        [Tooltip("Scene loaded after the chapter's last caption.")]
        public string nextScene = "Title";

        public readonly Inventory inv = new Inventory();
        protected readonly List<Debt> debts = new List<Debt>();
        protected readonly Dictionary<string, int> hearts = new Dictionary<string, int>();
        readonly Dictionary<string, Func<IEnumerator>> handlers = new Dictionary<string, Func<IEnumerator>>();
        Interactable focused;
        protected bool cutscene;
        protected bool toolsOn;            // workbench + Return Box usable
        protected Volume greyVolume;

        // speakers (also used by the voice generator — keep names and strings in sync with Tools/make_voices.py)
        protected const string PIP = "PIP", TOCK = "TOCK", OSKAR = "CAPTAIN OSKAR", NELL = "NELL", BRAM = "BRAM",
            MIRA = "MIRA", ADA = "GRANDMA ADA", OLDPIP = "PIP (OLDER)", SEAGULL = "SEAGULL", NARRATOR = "NARRATOR";

        // ------------------------------------------------------------------ lifecycle
        protected virtual void Awake()
        {
            Instance = this;
            inv.Changed += RefreshHud;
            BuildGreyVolume();
        }

        void Start()
        {
            Cursor.visible = true;
            ui.SetObjective(null);
            Pickups(false, "Pickup_");                 // everything appears when the story needs it
            Setup();
            cam.SnapNow();
            RefreshHud();
            StartCoroutine(RunAndFinish());
        }

        /// <summary>Put the world in the state this chapter starts in.</summary>
        protected virtual void Setup() { }
        protected abstract IEnumerator Run();

        /// <summary>Chapter number, title and the 5 part names, for the progress line and the end screen.</summary>
        protected virtual int ChapterNumber => 1;
        protected virtual string ChapterTitle => "";
        protected virtual string NextChapterTitle => "";

        /// <summary>Runs one part of the chapter and shows "Part n of 5" so the player always knows where they are.</summary>
        protected IEnumerator Part(int n, string title, IEnumerator body)
        {
            ui.SetPart($"CHAPTER {ChapterNumber}  •  PART {n} OF 5  •  {title.ToUpperInvariant()}");
            ui.ShowBanner($"Chapter {ChapterNumber}  •  Part {n} of 5", title);
            yield return body;
        }

        IEnumerator RunAndFinish()
        {
            yield return Run();
            yield return Wait(1.5f);
            ui.SetCaption(""); ui.SetPart(null); ui.SetObjective(null);
            bool last = string.IsNullOrEmpty(NextChapterTitle);
            int pick = 0;
            yield return ui.ChapterComplete(
                $"Chapter {ChapterNumber} complete!",
                last ? $"\"{ChapterTitle}\"\nYou finished all three chapters. Pip's story continues soon..."
                     : $"\"{ChapterTitle}\"\nNext: Chapter {ChapterNumber + 1}  -  {NextChapterTitle}",
                last ? "Back to title" : "Continue",
                last ? null : "Title screen",
                i => pick = i);
            var target = pick == 1 || last ? "Title" : nextScene;
            if (!string.IsNullOrEmpty(target) && Application.CanStreamedLevelBeLoaded(target))
                SceneManager.LoadScene(target);
        }

        // ------------------------------------------------------------------ loop
        protected virtual void Update()
        {
            ui.SetClock(clock.Label);
            player.canMove = !cutscene && !ui.Busy;

            focused = null;
            if (!cutscene && !ui.Busy)
            {
                float best = float.MaxValue;
                var p = player.transform.position; p.y = 0;
                foreach (var it in Interactable.All)
                {
                    if (!it.available || !it.isActiveAndEnabled) continue;
                    if (it.pickupItem == ItemId.None && !HasHandler(it.id)) continue;
                    float d = Vector3.Distance(p, it.Point);
                    if (d < it.range && d < best) { best = d; focused = it; }
                }
            }
            ui.SetPrompt(focused ? focused.Prompt : null);

            var k = Keyboard.current; var g = Gamepad.current;
            bool use = (k != null && k.eKey.wasPressedThisFrame) || (g != null && g.buttonSouth.wasPressedThisFrame) || GameUI.ConsumeUse();
            if (use && focused) StartCoroutine(Use(focused));

            CheckDueTimes();
        }

        IEnumerator Use(Interactable it)
        {
            if (it.pickupItem != ItemId.None)
            {
                inv.Add(it.pickupItem, it.pickupAmount);
                sfx.Pickup();
                ui.Toast($"+ {Items.Name(it.pickupItem)}{(it.pickupAmount > 1 ? " ×" + it.pickupAmount : "")}");
                it.gameObject.SetActive(false);
                yield break;
            }
            Func<IEnumerator> h = null;
            if (handlers.TryGetValue(it.id, out var found)) h = found;
            else if (toolsOn && it.id.StartsWith("Workbench")) h = Workbench;
            else if (toolsOn && it.id.StartsWith("ReturnBox")) h = ReturnBox;
            if (h == null) yield break;
            cutscene = true;
            ui.SetPrompt(null);
            yield return h();
            cutscene = false;
        }

        // test hooks used by the Autopilot playtester
        public bool HasHandler(string id) => handlers.ContainsKey(id) || toolsOn && (id.StartsWith("Workbench") || id.StartsWith("ReturnBox"));
        public bool IsIdle => !cutscene && !ui.Busy;
        public IEnumerator DebugUse(Interactable it) => Use(it);
        public string DebugState => $"hour={clock.hour:F2} debts=[{string.Join(",", debts.ConvertAll(d => d.item + (d.returned ? "✓" : "")))}]";

        protected void On(string id, Func<IEnumerator> h) => handlers[id] = h;
        protected void Off(string id) => handlers.Remove(id);

        // ------------------------------------------------------------------ dialogue
        protected IEnumerator Say(string who, string line) => ui.Say(who, line);
        protected IEnumerator Note(string title, string body, string voice = NARRATOR) => ui.Note(title, body, voice);
        protected void Objective(string s) => ui.SetObjective(s);

        // ------------------------------------------------------------------ world helpers
        protected Transform Spot(string name) => spots ? spots.Find(name) : null;

        protected GameObject Find(string name)
        {
            foreach (var it in Resources.FindObjectsOfTypeAll<Transform>())
                if (it.name == name && it.gameObject.scene.IsValid()) return it.gameObject;
            return null;
        }

        protected void Show(string name, bool on = true) { var go = Find(name); if (go) go.SetActive(on); }
        protected void Pickup(string name, bool on = true) => Show(name, on);
        protected void Pickups(bool on, string prefix)
        {
            foreach (var t in Resources.FindObjectsOfTypeAll<Interactable>())
                if (t.gameObject.scene.IsValid() && t.name.StartsWith(prefix)) t.gameObject.SetActive(on);
        }

        protected IEnumerator WaitNear(string spot, float radius)
        {
            var t = Spot(spot);
            while (t && Vector3.Distance(Flat(player.transform.position), Flat(t.position)) > radius) yield return null;
        }

        protected static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0, v.z);
        protected IEnumerator Wait(float s) { yield return new WaitForSeconds(s); }
        protected void AtLeastHour(float h) { if (clock.hour < h) clock.hour = h; }

        protected IEnumerator LookAt(string pose, float hold)
        {
            var t = Spot(pose);
            if (!t) yield break;
            cam.Focus(t);
            yield return Wait(hold);
        }

        /// <summary>Fade out, move Pip (and Tock) to a spot, fade in.</summary>
        protected IEnumerator TravelTo(string spotName, string caption = null)
        {
            bool was = cutscene; cutscene = true;
            yield return ui.Fade(1f, 0.6f);
            var s = Spot(spotName);
            player.Teleport(s.position, s.eulerAngles.y);
            if (tock && tock.gameObject.activeSelf) tock.Warp(s.position + s.right * 0.6f - s.forward * 0.3f);
            cam.yaw = s.eulerAngles.y;
            cam.SnapNow();
            if (caption != null) ui.SetCaption(caption);
            yield return ui.Fade(0f, 0.8f);
            if (caption != null) { yield return Wait(1.4f); ui.SetCaption(""); }
            cutscene = was;
        }

        void BuildGreyVolume()
        {
            var go = new GameObject("Tomorrow Window Grade");
            go.transform.SetParent(transform, false);
            greyVolume = go.AddComponent<Volume>();
            greyVolume.isGlobal = true;
            greyVolume.priority = 50;
            greyVolume.weight = 0;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var ca = profile.Add<ColorAdjustments>(true);
            ca.saturation.Override(-95f);
            ca.postExposure.Override(-0.6f);
            ca.colorFilter.Override(new Color(0.8f, 0.85f, 1f));
            var vig = profile.Add<Vignette>(true);
            vig.intensity.Override(0.45f);
            greyVolume.sharedProfile = profile;
        }

        protected IEnumerator GreyTo(float w, float secs)
        {
            float from = greyVolume.weight;
            for (float t = 0; t < secs; t += Time.deltaTime) { greyVolume.weight = Mathf.Lerp(from, w, t / secs); yield return null; }
            greyVolume.weight = w;
        }

        /// <summary>Show one "tomorrow" preview through the Tomorrow Window: fade, grey, a camera pose, a caption.</summary>
        protected IEnumerator WindowPreview(string camPose, string caption, float hold = 3f, Action before = null, Action after = null)
        {
            yield return ui.Fade(1f, 0.4f);
            var pose = Spot(camPose);
            cam.Focus(pose);
            cam.transform.SetPositionAndRotation(pose.position, pose.rotation);
            ui.SetHudVisible(false);
            greyVolume.weight = 1f;
            before?.Invoke();
            ui.SetCaption(caption);
            yield return ui.Fade(0f, 0.6f);
            yield return Wait(hold);
            yield return ui.Fade(1f, 0.4f);
            after?.Invoke();
            greyVolume.weight = 0f;
            ui.SetCaption("");
            cam.Release();
            cam.SnapNow();
            ui.SetHudVisible(true);
            yield return ui.Fade(0f, 0.5f);
        }

        // ------------------------------------------------------------------ Tomorrow Boxes
        static readonly Color BlueGlow = new Color(0.45f, 0.75f, 1f), PinkGlow = new Color(1f, 0.55f, 0.75f);

        /// <summary>A glowing box falls out of the sky and waits to be opened. Pink = from a friend's tomorrow.</summary>
        protected GameObject DropBox(string spotName, string id, string prompt = "Open the Tomorrow Box", bool pink = false)
        {
            var spot = Spot(spotName);
            var box = Instantiate(tomorrowBoxPrefab, spot.position + Vector3.up * 4f, spot.rotation);
            box.name = id;
            var c = pink ? PinkGlow : BlueGlow;
            var light = new GameObject("Glow").AddComponent<Light>();
            light.type = LightType.Point; light.color = c; light.range = 2.2f; light.intensity = 3f;
            light.transform.SetParent(box.transform, false);
            light.transform.localPosition = Vector3.up * 0.35f;
            if (pink)
            {
                Visuals.Tint(box, new Color(1f, 0.7f, 0.82f), 0.55f);
                var ribbon = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(ribbon.GetComponent<Collider>());
                ribbon.name = "Ribbon";
                ribbon.transform.SetParent(box.transform, false);
                var b = Visuals.WorldBounds(box);
                ribbon.transform.position = new Vector3(box.transform.position.x, b.max.y + 0.01f, box.transform.position.z);
                ribbon.transform.localScale = new Vector3(0.08f, 0.03f, b.size.z * 1.02f);
                ribbon.GetComponent<Renderer>().material = Visuals.Lit(new Color(1f, 0.5f, 0.7f));
            }
            Visuals.Glow(box, c * 0.6f, 1f);
            var it = box.AddComponent<Interactable>();
            it.id = id; it.prompt = prompt; it.range = 1.3f;
            it.available = false;                      // can't be opened mid-air
            StartCoroutine(Fall(box.transform, spot.position));
            sfx.Whoosh();
            return box;
        }

        IEnumerator Fall(Transform t, Vector3 to)
        {
            var from = t.position;
            for (float a = 0; a < 0.8f; a += Time.deltaTime)
            {
                if (!t) yield break;
                float k = a / 0.8f;
                t.position = Vector3.Lerp(from, to, k * k);
                yield return null;
            }
            if (!t) yield break;
            t.position = to;
            for (float a = 0; a < 0.25f; a += Time.deltaTime)
            {
                if (!t) yield break;
                float s = 1f + Mathf.Sin(a / 0.25f * Mathf.PI) * 0.12f;
                t.localScale = new Vector3(s, 1f / s, s);
                yield return null;
            }
            if (!t) yield break;
            t.localScale = Vector3.one;
            t.GetComponent<Interactable>().available = true;
        }

        protected IEnumerator VanishBox(GameObject box)
        {
            if (!box) yield break;
            var t = box.transform;
            for (float a = 0; a < 0.5f; a += Time.deltaTime)
            {
                if (!t) yield break;
                t.localScale = Vector3.one * (1f - a / 0.5f);
                t.position += Vector3.up * Time.deltaTime * 1.5f;
                yield return null;
            }
            Destroy(box);
        }

        /// <summary>Wait for the player to open box <paramref name="id"/>; borrow its item(s); the box vanishes.</summary>
        protected IEnumerator OpenBox(string id, string toast, params Debt[] loans)
        {
            bool opened = false;
            On(id, () => OpenBoxNow(id, toast, loans, () => opened = true));
            while (!opened) yield return null;
        }

        IEnumerator OpenBoxNow(string id, string toast, Debt[] loans, Action done)
        {
            Off(id);
            var box = Find(id);
            if (!string.IsNullOrEmpty(toast)) ui.Toast(toast);
            foreach (var d in loans) Borrow(d);
            yield return VanishBox(box);
            done();
        }

        // ------------------------------------------------------------------ the Due Board
        /// <summary>Create a loan. <paramref name="due"/> is the in-game hour it must be back (99 = no clock).</summary>
        protected static Debt Loan(ItemId item, float due = 99f, string friend = null, int amount = 1) =>
            new Debt { item = item, amount = amount, dueHour = due, friend = friend, fromWhom = friend != null ? friend + "'s tomorrow" : "Tomorrow" };

        protected void Borrow(ItemId item, int amount = 1, string from = "Tomorrow") =>
            Borrow(new Debt { item = item, amount = amount, fromWhom = from });

        protected void Borrow(Debt d)
        {
            inv.Add(d.item, d.amount);
            debts.Add(d);
            sfx.Pickup();
            ui.Toast($"Borrowed from {d.fromWhom}: {Items.Name(d.item)}");
            RefreshHud();
        }

        /// <summary>Add a debt with nothing in hand (interest, or something already used).</summary>
        protected void Owe(Debt d) { debts.Add(d); RefreshHud(); }

        protected bool Repaid(ItemId item) => debts.Exists(d => d.item == item && d.returned);
        protected bool AllRepaid => debts.TrueForAll(d => d.returned);
        protected Debt Find(ItemId item) => debts.FindLast(d => d.item == item);

        protected void SetHearts(string friend, int n) { hearts[friend] = n; RefreshHud(); }
        protected void LoseHeart(string friend)
        {
            if (hearts.TryGetValue(friend, out var n) && n > 0) hearts[friend] = n - 1;
            RefreshHud();
        }
        protected void GainHeart(string friend, int max = 3)
        {
            if (hearts.TryGetValue(friend, out var n) && n < max) hearts[friend] = n + 1;
            RefreshHud();
        }

        protected void RefreshHud()
        {
            ui.ShowInventory(inv);
            ui.ShowDebts(debts);
            ui.ShowHearts(hearts);
        }

        void CheckDueTimes()
        {
            if (cutscene) return;
            foreach (var d in debts)
            {
                if (d.returned || d.lateHandled || d.dueHour >= 99f || clock.hour < d.dueHour) continue;
                d.lateHandled = true;
                StartCoroutine(LateRoutine(d));
                return;
            }
        }

        IEnumerator LateRoutine(Debt d)
        {
            while (cutscene || ui.Busy) yield return null;
            cutscene = true;
            ui.SetPrompt(null);
            yield return OnLate(d);
            cutscene = false;
        }

        /// <summary>What happens when a loan goes past its due time. Default: one extra part of interest.</summary>
        protected virtual IEnumerator OnLate(Debt d)
        {
            sfx.Bell();
            if (d.friend != null)
            {
                LoseHeart(d.friend);
                yield return Say(TOCK, $"Late. {d.friend} is having a bad day now. Not the town. {d.friend}.");
            }
            else
            {
                var extra = Interest(d.item);
                yield return Say(TOCK, $"Late! Tomorrow charges interest. One extra {Items.Name(extra)}. Like a library, but angrier.");
                Owe(new Debt { item = extra, fromWhom = "Tomorrow (interest)" });
                OnInterestOwed(extra);
            }
        }

        protected static ItemId Interest(ItemId item)
        {
            if (Recipes.All.TryGetValue(item, out var parts)) foreach (var p in parts) return p.Key;
            return item;
        }

        /// <summary>Chapters put a spare part somewhere so interest can be paid.</summary>
        protected virtual void OnInterestOwed(ItemId part)
        {
            var spot = Spot("Interest_Spot");
            if (!spot) return;
            var go = new GameObject("Pickup_Interest_" + part);
            go.transform.position = spot.position + new Vector3(UnityEngine.Random.Range(-0.4f, 0.4f), 0.05f, UnityEngine.Random.Range(-0.4f, 0.4f));
            var it = go.AddComponent<Interactable>();
            it.id = go.name; it.pickupItem = part; it.range = 1.1f;
            var vis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(vis.GetComponent<Collider>());
            vis.transform.SetParent(go.transform, false);
            vis.transform.localPosition = Vector3.up * 0.08f; vis.transform.localScale = Vector3.one * 0.16f;
            vis.GetComponent<Renderer>().material = Visuals.Lit(new Color(1f, 0.8f, 0.4f));
            var l = new GameObject("Sparkle").AddComponent<Light>();
            l.type = LightType.Point; l.range = 1f; l.intensity = 1.5f; l.color = new Color(1f, 0.85f, 0.5f);
            l.transform.SetParent(go.transform, false); l.transform.localPosition = Vector3.up * 0.3f;
            go.AddComponent<Idle>().bob = 0.02f;
            ui.Toast($"The interest part ({Items.Name(part)}) is waiting by the Return Box.");
        }

        // ------------------------------------------------------------------ workbench & Return Box
        protected IEnumerator Workbench()
        {
            var order = new List<ItemId>();
            foreach (var d in debts) if (!d.returned && Recipes.All.ContainsKey(d.item) && !order.Contains(d.item)) order.Add(d.item);
            foreach (var r in Recipes.All.Keys) if (!order.Contains(r)) order.Add(r);

            foreach (var r in order)
            {
                // don't burn parts on something nobody owes, unless the chapter asked for it
                bool owed = debts.Exists(d => !d.returned && d.item == r && !inv.Has(d.item, d.amount));
                if (!owed && !ExtraBuilds.Contains(r)) continue;
                if (inv.CanBuild(r))
                {
                    inv.Build(r);
                    sfx.Build();
                    ui.Toast($"Built: {Items.Name(r)}!");
                    if (ExtraBuilds.Contains(r) && !owed) ExtraBuilds.Remove(r);
                    yield return Wait(0.5f);
                    yield break;
                }
            }
            var need = debts.Find(d => !d.returned && Recipes.All.ContainsKey(d.item) && !inv.Has(d.item, d.amount));
            if (need != null)
                yield return Say(TOCK, $"To build the {Items.Name(need.item)} we need: {Recipes.Describe(need.item)}. Go find them. I'll wait. I'm good at waiting.");
            else
                yield return Say(TOCK, "Nothing to build right now. The bench is resting. Let it.");
        }

        /// <summary>Things the workbench may build even though no one is owed them (e.g. a second battery).</summary>
        protected readonly List<ItemId> ExtraBuilds = new List<ItemId>();

        protected IEnumerator ReturnBox()
        {
            bool any = false;
            var open = debts.FindAll(d => !d.returned);
            open.Sort((a, b) => a.dueHour.CompareTo(b.dueHour));
            foreach (var d in open)
            {
                if (!inv.Has(d.item, d.amount))
                {
                    // the earlier one comes first. Always.
                    if (d.dueHour < 99f && open.Exists(o => o != d && !o.returned && o.dueHour > d.dueHour && inv.Has(o.item, o.amount)))
                    {
                        yield return Say(TOCK, $"Careful. When you owe two things, the earlier one comes first. Always. The {Items.Name(d.item)} is due first.");
                        RefreshHud();
                        yield break;
                    }
                    continue;
                }
                inv.Remove(d.item, d.amount);
                d.returned = true;
                any = true;
                sfx.Whoosh();
                ui.Toast($"Returned to {d.fromWhom}: {Items.Name(d.item)}");
                yield return Wait(0.7f);
            }
            RefreshHud();
            if (!any)
            {
                var first = open.Find(d => !d.returned);
                if (first != null) yield return Say(TOCK, $"The Return Box is hungry for a {Items.Name(first.item)}. Build it first.");
                else yield return Say(TOCK, "We owe tomorrow nothing. Enjoy it. It never lasts.");
            }
        }
    }
}
