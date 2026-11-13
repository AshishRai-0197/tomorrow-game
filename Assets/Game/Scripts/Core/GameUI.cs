using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Tomorrow
{
    /// <summary>All on-screen UI, built in code so the scene stays simple: dialogue, notes, choices, HUD, fades.</summary>
    public class GameUI : MonoBehaviour
    {
        static readonly Color Ink = new Color(0.20f, 0.14f, 0.10f);
        static readonly Color Paper = new Color(0.98f, 0.93f, 0.82f, 0.97f);
        static readonly Color Brass = new Color(0.86f, 0.64f, 0.28f);
        static readonly Color TomorrowBlue = new Color(0.45f, 0.78f, 1f);

        Font font;
        RectTransform root;
        GameObject dialogPanel, notePanel, choicePanel;
        Text speakerText, lineText, nextHint, noteTitle, noteBody, choiceQuestion;
        Text promptText, objectiveText, clockText, invText, debtText, timerText, toastText, captionText;
        Image fade;
        readonly List<Button> choiceButtons = new List<Button>();
        float toastUntil;

        public bool Busy { get; private set; }

        /// <summary>Test hook: dialogue advances by itself and choices pick option 1.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            // Play mode may skip the domain reload, so clear anything a previous run (or the test bot) left behind.
            AutoAdvance = false; AutoChoice = 0; AdvanceOverride = null; VoiceCount = 0; LastVoice = null; PlayerController.ScriptedMove = Vector3.zero; useFrame = -100; nextFrame = -100;
            VirtualJoystick.Value = Vector2.zero;
            Interactable.All.Clear();
            Time.timeScale = 1f;
        }

        public static bool AutoAdvance;
        public static int AutoChoice;

        // On-screen buttons (mouse or touch) leave a short-lived "pressed" mark that game code consumes.
        static int useFrame = -100, nextFrame = -100;
        public static bool ConsumeUse()
        {
            if (Time.frameCount - useFrame > 1) return false;
            useFrame = -100; return true;
        }
        static bool ConsumeNext()
        {
            if (Time.frameCount - nextFrame > 1) return false;
            nextFrame = -100; return true;
        }

        /// <summary>Test/trailer hook: when set, decides when dialogue advances.</summary>
        public static System.Func<bool> AdvanceOverride;

        public static bool AdvancePressed()
        {
            if (AdvanceOverride != null) return AdvanceOverride();
            if (AutoAdvance) return Time.frameCount % 3 == 0;
            if (ConsumeNext()) return true;
            var k = Keyboard.current; var m = Mouse.current; var g = Gamepad.current;
            return (k != null && (k.spaceKey.wasPressedThisFrame || k.eKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame))
                || (m != null && m.leftButton.wasPressedThisFrame)
                || (g != null && g.buttonSouth.wasPressedThisFrame)
                || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);
        }

        static GameUI current;

        void Awake()
        {
            current = this;
            font = GameFont();
            var canvasGo = new GameObject("UI Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            root = canvasGo.GetComponent<RectTransform>();

            // HUD
            clockText = MakeText(root, "Clock", 34, TextAnchor.UpperRight, Color.white, new Vector2(1, 1), new Vector2(-30, -24), new Vector2(420, 60));
            hudBack = MakePanel(root, "HUD Back", new Color(0.16f, 0.10f, 0.07f, 0.72f), new Vector2(0, 1), new Vector2(16, -12), new Vector2(940, 134));
            var hb = hudBack.GetComponent<RectTransform>(); hb.pivot = new Vector2(0, 1); hb.anchoredPosition = new Vector2(16, -12);
            partText = MakeText(root, "Part", 24, TextAnchor.UpperLeft, Brass, new Vector2(0, 1), new Vector2(30, -22), new Vector2(900, 36));
            partText.fontStyle = FontStyle.Bold;
            objectiveText = MakeText(root, "Objective", 28, TextAnchor.UpperLeft, Color.white, new Vector2(0, 1), new Vector2(30, -58), new Vector2(900, 80));
            debtText = MakeText(root, "Debts", 24, TextAnchor.UpperLeft, TomorrowBlue, new Vector2(0, 1), new Vector2(30, -150), new Vector2(700, 220));
            invText = MakeText(root, "Inventory", 24, TextAnchor.LowerLeft, Color.white, new Vector2(0, 0), new Vector2(30, 330), new Vector2(560, 300));
            promptText = MakeText(root, "Prompt", 30, TextAnchor.MiddleCenter, Color.white, new Vector2(0.5f, 0), new Vector2(0, 300), new Vector2(900, 50));
            timerText = MakeText(root, "Timer", 64, TextAnchor.UpperCenter, new Color(1f, 0.55f, 0.35f), new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(500, 90));
            toastText = MakeText(root, "Toast", 30, TextAnchor.MiddleCenter, Brass, new Vector2(0.5f, 0.5f), new Vector2(0, 160), new Vector2(1000, 60));
            heartText = MakeText(root, "Hearts", 30, TextAnchor.UpperRight, new Color(1f, 0.6f, 0.75f), new Vector2(1, 1), new Vector2(-30, -200), new Vector2(420, 120));
            captionSub = MakeText(root, "Caption Sub", 28, TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.8f), new Vector2(0.5f, 0.5f), new Vector2(0, -120), new Vector2(1200, 60));
            captionText = MakeText(root, "Caption", 54, TextAnchor.MiddleCenter, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600, 300));
            bannerPanel = MakePanel(root, "Part Banner", new Color(0.24f, 0.15f, 0.10f, 0.92f), new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(760, 130));
            bannerTop = MakeText(bannerPanel.transform, "Top", 26, TextAnchor.MiddleCenter, Brass, new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(720, 40));
            bannerTitle = MakeText(bannerPanel.transform, "Title", 44, TextAnchor.MiddleCenter, Color.white, new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(720, 60));
            bannerTitle.fontStyle = FontStyle.Bold;
            bannerPanel.SetActive(false);
            foreach (var t in new[] { clockText, objectiveText, debtText, invText, promptText, timerText, toastText, captionText, partText })
                t.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);

            // Dialogue box
            dialogPanel = MakePanel(root, "Dialogue", Paper, new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(1300, 210));
            speakerText = MakeText(dialogPanel.transform, "Speaker", 32, TextAnchor.UpperLeft, new Color(0.62f, 0.33f, 0.12f), new Vector2(0, 1), new Vector2(36, -18), new Vector2(800, 44));
            speakerText.fontStyle = FontStyle.Bold;
            lineText = MakeText(dialogPanel.transform, "Line", 32, TextAnchor.UpperLeft, Ink, new Vector2(0, 1), new Vector2(36, -64), new Vector2(1228, 130));
            nextHint = MakeText(dialogPanel.transform, "Next", 22, TextAnchor.LowerRight, new Color(0.5f, 0.4f, 0.3f), new Vector2(1, 0), new Vector2(-24, 14), new Vector2(400, 30));
            nextHint.text = "Space ▸";
            dialogPanel.SetActive(false);

            // Note / letter
            notePanel = MakePanel(root, "Note", Paper, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 560));
            noteTitle = MakeText(notePanel.transform, "Title", 36, TextAnchor.UpperCenter, new Color(0.62f, 0.33f, 0.12f), new Vector2(0.5f, 1), new Vector2(0, -36), new Vector2(900, 50));
            noteTitle.fontStyle = FontStyle.Bold;
            noteBody = MakeText(notePanel.transform, "Body", 34, TextAnchor.MiddleCenter, Ink, new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(860, 380));
            noteBody.fontStyle = FontStyle.Italic;
            var nh = MakeText(notePanel.transform, "Next", 22, TextAnchor.LowerRight, new Color(0.5f, 0.4f, 0.3f), new Vector2(1, 0), new Vector2(-24, 18), new Vector2(400, 30));
            nh.text = "Space ▸";
            notePanel.SetActive(false);

            // Choice
            choicePanel = MakePanel(root, "Choice", new Color(0.24f, 0.15f, 0.10f, 0.94f), new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(980, 380));
            choiceQuestion = MakeText(choicePanel.transform, "Question", 34, TextAnchor.UpperCenter, Color.white, new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(900, 90));
            for (int i = 0; i < 2; i++)
            {
                var b = MakePanel(choicePanel.transform, "Option" + i, Brass, new Vector2(0.5f, 1), new Vector2(0, -150 - i * 105), new Vector2(820, 86));
                var btn = b.AddComponent<Button>();
                var lt = MakeText(b.transform, "Label", 30, TextAnchor.MiddleCenter, Ink, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800, 80));
                lt.raycastTarget = false;
                choiceButtons.Add(btn);
            }
            choicePanel.SetActive(false);

            // Chapter complete
            endPanel = MakePanel(root, "Chapter Complete", new Color(0.24f, 0.15f, 0.10f, 0.96f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100, 560));
            endTitle = MakeText(endPanel.transform, "Title", 64, TextAnchor.MiddleCenter, Brass, new Vector2(0.5f, 1), new Vector2(0, -90), new Vector2(1000, 90));
            endTitle.fontStyle = FontStyle.Bold;
            endBody = MakeText(endPanel.transform, "Body", 32, TextAnchor.MiddleCenter, Color.white, new Vector2(0.5f, 1), new Vector2(0, -215), new Vector2(980, 140));
            for (int i = 0; i < 2; i++)
            {
                var b = MakePanel(endPanel.transform, "Button" + i, i == 0 ? Brass : new Color(0.55f, 0.42f, 0.32f), new Vector2(0.5f, 0), new Vector2(i == 0 ? -230 : 230, 70), new Vector2(420, 90));
                var btn = b.AddComponent<Button>(); int idx = i; btn.onClick.AddListener(() => endPick = idx);
                var lt = MakeText(b.transform, "Label", 32, TextAnchor.MiddleCenter, Ink, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 80));
                lt.raycastTarget = false; lt.fontStyle = FontStyle.Bold;
                endButtons.Add(lt);
            }
            endPanel.SetActive(false);

            // Fade on top of everything
            var fgo = new GameObject("Fade", typeof(RectTransform), typeof(Image));
            fgo.transform.SetParent(root, false);
            var frt = fgo.GetComponent<RectTransform>();
            frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one; frt.offsetMin = frt.offsetMax = Vector2.zero;
            fade = fgo.GetComponent<Image>();
            fade.color = new Color(0, 0, 0, 1);
            fade.raycastTarget = false;
            captionText.transform.SetAsLastSibling();

            BuildOnScreenControls();
            SetPrompt(null); SetTimer(null); captionText.text = ""; toastText.text = "";
        }

        Text[] allTexts;
        GameObject hudBack, bannerPanel, endPanel;
        Text partText, bannerTop, bannerTitle, endTitle, endBody;
        readonly List<Text> endButtons = new List<Text>();
        float bannerUntil; int endPick = -1;

        /// <summary>
        /// The built-in UI font has no fallback on Web/Android, so swap the few fancy symbols
        /// the script uses for plain ones every platform can draw.
        /// </summary>
        public static string Safe(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            bool fancy = false;
            foreach (var c in s) if (c > 0xFF) { fancy = true; break; }
            if (!fancy) return s;
            return s.Replace("—", "-").Replace("–", "-").Replace("…", "...").Replace("’", "'").Replace("‘", "'")
                    .Replace("“", "\"").Replace("”", "\"").Replace("○", "-").Replace("✓", "+").Replace("▸", ">")
                    .Replace("◆", "*").Replace("♥", "<3").Replace("♡", "").Replace("•", "-").Replace("⧗", "-");
        }

        void LateUpdate()
        {
            if (allTexts == null || allTexts.Length == 0) allTexts = GetComponentsInChildren<Text>(true);
            foreach (var t in allTexts)
            {
                var fixedText = Safe(t.text);
                if (!ReferenceEquals(fixedText, t.text)) t.text = fixedText;
            }
        }

        void Update()
        {
            if (toastText.text.Length > 0 && Time.time > toastUntil) toastText.text = "";
            if (bannerPanel.activeSelf && Time.time > bannerUntil) bannerPanel.SetActive(false);
        }

        // ---------- HUD ----------
        public void SetPrompt(string s) { promptText.text = s == null ? "" : "[E]  " + s; }
        public void SetObjective(string s) { objectiveText.text = string.IsNullOrEmpty(s) ? "" : "◆ " + s; hudBack.SetActive(objectiveText.enabled && (objectiveText.text.Length > 0 || partText.text.Length > 0)); }
        public void SetPart(string s) { partText.text = s ?? ""; hudBack.SetActive(objectiveText.enabled && (objectiveText.text.Length > 0 || partText.text.Length > 0)); }

        /// <summary>Big "Part 2 of 5" banner at the top for a few seconds.</summary>
        public void ShowBanner(string top, string title, float secs = 3.2f)
        {
            bannerTop.text = top; bannerTitle.text = title; bannerPanel.SetActive(true); bannerUntil = Time.time + secs;
        }

        /// <summary>End-of-chapter screen. picked(0) = continue, picked(1) = title screen.</summary>
        public IEnumerator ChapterComplete(string title, string body, string continueLabel, string secondLabel, System.Action<int> picked)
        {
            Busy = true; endPick = -1;
            endTitle.text = title; endBody.text = body;
            endButtons[0].text = continueLabel;
            endButtons[1].transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(secondLabel));
            endButtons[1].text = secondLabel ?? "";
            bannerPanel.SetActive(false);
            endPanel.SetActive(true); endPanel.transform.SetAsLastSibling();
            Cursor.visible = true;
            yield return new WaitForSeconds(0.6f);
            while (endPick < 0)
            {
                if (AdvancePressed()) endPick = 0;
                var k = Keyboard.current;
                if (k != null && k.escapeKey.wasPressedThisFrame && endButtons[1].transform.parent.gameObject.activeSelf) endPick = 1;
                yield return null;
            }
            endPanel.SetActive(false); Busy = false;
            picked(endPick);
        }
        public void SetClock(string s) { clockText.text = s; }
        public void SetTimer(string s) { timerText.text = s ?? ""; }
        public void Toast(string s, float secs = 2.2f) { toastText.text = s; toastUntil = Time.time + secs; }
        public void SetCaption(string s) { captionText.text = s ?? ""; if (string.IsNullOrEmpty(s)) captionSub.text = ""; }
        public void SetCaptionSub(string s) { captionSub.text = s ?? ""; captionSub.transform.SetAsLastSibling(); }
        public void SetHudVisible(bool v)
        {
            foreach (var t in new[] { clockText, objectiveText, debtText, invText, promptText, heartText, partText }) t.enabled = v;
            hudBack.SetActive(v && (objectiveText.text.Length > 0 || partText.text.Length > 0));
            if (helpText) helpText.enabled = v;
        }

        public void ShowInventory(Inventory inv)
        {
            var sb = new StringBuilder();
            foreach (var kv in inv.All())
                if (kv.Value > 0) sb.AppendLine(kv.Value > 1 ? $"• {Items.Name(kv.Key)} ×{kv.Value}" : $"• {Items.Name(kv.Key)}");
            invText.text = sb.Length > 0 ? "POCKETS\n" + sb : "";
        }

        public static string HourLabel(float hour)
        {
            int h = Mathf.FloorToInt(hour) % 24, m = Mathf.RoundToInt((hour - Mathf.Floor(hour)) * 60f) % 60;
            int h12 = h % 12 == 0 ? 12 : h % 12;
            return $"{h12}:{m:00} {(h < 12 ? "am" : "pm")}";
        }

        /// <summary>The Due Board: every open loan, earliest first, with what it takes to rebuild it.</summary>
        public void ShowDebts(IEnumerable<Debt> debts, Inventory inv = null)
        {
            var open = new List<Debt>();
            foreach (var d in debts) if (!d.returned) open.Add(d);
            open.Sort((a, b) => a.dueHour.CompareTo(b.dueHour));
            var sb = new StringBuilder();
            foreach (var d in open)
            {
                var amount = d.amount > 1 ? $" ×{d.amount}" : "";
                var due = d.dueHour < 99f ? $"  [due {HourLabel(d.dueHour)}]" : "  [due sunset]";
                var from = d.friend != null ? $"  ({d.friend}'s)" : "";
                var recipe = Recipes.Describe(d.item);
                string state;
                if (inv != null && inv.Has(d.item, d.amount))
                    state = d.built || recipe.Length == 0 ? "ready: put it in the Return Box" : "in your bag: use it first";
                else
                    state = recipe.Length > 0 ? $"build: {recipe}" : "find one";
                sb.AppendLine($"○ {Items.Name(d.item)}{amount}{from}{due}  —  {state}");
            }
            foreach (var d in debts)
                if (d.returned) sb.AppendLine($"✓ {Items.Name(d.item)}  (returned)");
            debtText.text = sb.Length > 0 ? "DUE BOARD — earliest first\n" + sb : "";
        }

        public void ShowHearts(Dictionary<string, int> hearts)
        {
            var sb = new StringBuilder();
            foreach (var kv in hearts)
                sb.AppendLine($"{kv.Key}: {kv.Value}/3 hearts");
            heartText.text = sb.ToString();
        }

        // ---------- Story UI ----------
        public IEnumerator Say(string speaker, string line)
        {
            Busy = true;
            dialogPanel.SetActive(true);
            speakerText.text = speaker;
            lineText.text = "";
            nextHint.enabled = false;
            PlayVoice(speaker, line);
            yield return null; // don't let the key that opened this also skip it
            for (int i = 1; i <= line.Length; i++)
            {
                lineText.text = line.Substring(0, i);
                if (AdvancePressed()) { lineText.text = line; yield return null; break; }
                yield return new WaitForSeconds(0.018f);
            }
            nextHint.enabled = true;
            while (!AdvancePressed()) yield return null;
            StopVoice();
            yield return null;
            dialogPanel.SetActive(false);
            Busy = false;
        }

        public IEnumerator Note(string title, string body, string voiceSpeaker = "NARRATOR")
        {
            Busy = true;
            notePanel.SetActive(true);
            noteTitle.text = title;
            noteBody.text = body;
            PlayVoice(voiceSpeaker, body);
            yield return null;
            while (!AdvancePressed()) yield return null;
            StopVoice();
            yield return null;
            notePanel.SetActive(false);
            Busy = false;
        }

        public IEnumerator Choose(string question, string a, string b, Action<int> picked)
        {
            Busy = true;
            choicePanel.SetActive(true);
            choiceQuestion.text = question;
            int result = -1;
            string[] labels = { "1 — " + a, "2 — " + b };
            for (int i = 0; i < 2; i++)
            {
                int idx = i;
                choiceButtons[i].GetComponentInChildren<Text>().text = labels[i];
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => result = idx);
            }
            Cursor.visible = true;
            while (result < 0)
            {
                if (AutoAdvance && (AdvanceOverride == null || AdvanceOverride())) { result = AutoChoice; break; }
                var k = Keyboard.current;
                if (k != null && (k.digit1Key.wasPressedThisFrame || k.numpad1Key.wasPressedThisFrame)) result = 0;
                if (k != null && (k.digit2Key.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame)) result = 1;
                yield return null;
            }
            choicePanel.SetActive(false);
            Busy = false;
            picked(result);
        }

        public IEnumerator Fade(float to, float seconds)
        {
            float from = fade.color.a;
            for (float t = 0; t < seconds; t += Time.deltaTime)
            {
                fade.color = new Color(0, 0, 0, Mathf.Lerp(from, to, t / seconds));
                yield return null;
            }
            fade.color = new Color(0, 0, 0, to);
        }

        // ---------- voices ----------
        /// <summary>Plays Resources/Voice/&lt;key&gt; for this exact line, if it was generated (Tools/make_voices.py).</summary>
        void PlayVoice(string speaker, string line)
        {
            if (AutoAdvance && AdvanceOverride == null) return;
            if (!voice) { voice = gameObject.AddComponent<AudioSource>(); voice.playOnAwake = false; voice.spatialBlend = 0f; }
            var clip = Resources.Load<AudioClip>("Voice/" + VoiceKey(speaker, line));
            voice.Stop();
            if (clip) { voice.clip = clip; voice.Play(); VoiceCount++; LastVoice = clip; }
        }

        /// <summary>Counts voice lines started, and the latest clip (used by the trailer recorder).</summary>
        public static int VoiceCount;
        public static AudioClip LastVoice;

        void StopVoice() { if (voice) voice.Stop(); }

        /// <summary>True while a voice line is playing (the music ducks under it).</summary>
        public static bool VoicePlaying => current && current.voice && current.voice.isPlaying;

        public static string VoiceKey(string speaker, string line)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(speaker + "|" + line));
                var sb = new StringBuilder();
                for (int i = 0; i < 6; i++) sb.Append(bytes[i].ToString("x2"));
                return sb.ToString();
            }
        }

        // ---------- helpers ----------
        Text MakeText(Transform parent, string name, int size, TextAnchor anchor, Color color, Vector2 pivotAnchor, Vector2 pos, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = pivotAnchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = sizeDelta;
            var t = go.GetComponent<Text>();
            t.font = font; t.fontSize = size; t.alignment = anchor; t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        Text helpText, heartText, captionSub;
        AudioSource voice;
        GameObject onScreen;

        void BuildOnScreenControls()
        {
            var circle = CircleSprite();
            onScreen = new GameObject("On-Screen Controls", typeof(RectTransform));
            onScreen.transform.SetParent(root, false);
            var ort = onScreen.GetComponent<RectTransform>();
            ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one; ort.offsetMin = ort.offsetMax = Vector2.zero;

            // Joystick (bottom-left)
            var stick = MakeCircle(onScreen.transform, "Joystick", circle, new Color(1, 1, 1, 0.22f), new Vector2(0, 0), new Vector2(190, 170), 230);
            var knob = MakeCircle(stick.transform, "Knob", circle, new Color(1f, 0.85f, 0.5f, 0.85f), new Vector2(0.5f, 0.5f), Vector2.zero, 100);
            knob.GetComponent<Image>().raycastTarget = false;
            var vj = stick.AddComponent<VirtualJoystick>();
            vj.knob = knob.GetComponent<RectTransform>();
            var mv = MakeText(stick.transform, "Label", 22, TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.8f), new Vector2(0.5f, 0), new Vector2(0, -26), new Vector2(240, 30));
            mv.text = "MOVE";

            // USE button (bottom-right, big) and NEXT button (above it)
            var use = MakeCircle(onScreen.transform, "Use Button", circle, new Color(0.86f, 0.64f, 0.28f, 0.92f), new Vector2(1, 0), new Vector2(-170, 170), 190);
            use.AddComponent<Button>().onClick.AddListener(() => useFrame = Time.frameCount);
            var ul = MakeText(use.transform, "Label", 34, TextAnchor.MiddleCenter, Ink, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180, 120));
            ul.text = "USE\n(E)"; ul.fontStyle = FontStyle.Bold;

            var next = MakeCircle(onScreen.transform, "Next Button", circle, new Color(0.98f, 0.93f, 0.82f, 0.92f), new Vector2(1, 0), new Vector2(-170, 365), 150);
            next.AddComponent<Button>().onClick.AddListener(() => nextFrame = Time.frameCount);
            var nl = MakeText(next.transform, "Label", 26, TextAnchor.MiddleCenter, Ink, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(130, 100));
            nl.text = "NEXT ▸\n(Space)"; nl.fontStyle = FontStyle.Bold;

            // Keyboard help under the clock
            helpText = MakeText(root, "Controls Help", 22, TextAnchor.UpperRight, new Color(1, 1, 1, 0.85f), new Vector2(1, 1), new Vector2(-30, -80), new Vector2(620, 140));
            helpText.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);
            if (Application.isMobilePlatform) helpText.text = "Drag MOVE to walk  •  USE to talk / pick up\nTap NEXT (or anywhere) for the next line";
            else helpText.text = "Click the game window first, then:\nMove: W A S D / arrows  •  Use / Talk: E\nNext line: Space or click  •  Camera: hold right mouse\nOr use the buttons on screen";
            // keep the buttons under the fade-to-black and the captions
            int fadeIndex = fade.transform.GetSiblingIndex();
            onScreen.transform.SetSiblingIndex(fadeIndex);
            helpText.transform.SetSiblingIndex(fadeIndex);
        }

        GameObject MakeCircle(Transform parent, string name, Sprite sprite, Color color, Vector2 anchor, Vector2 pos, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(size, size);
            var img = go.GetComponent<Image>();
            img.sprite = sprite; img.color = color;
            return go;
        }

        static Sprite CircleSprite()
        {
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f));
                    byte a = (byte)(Mathf.Clamp01(n / 2f - 1f - d) * 255);
                    px[y * n + x] = new Color32(255, 255, 255, a);
                }
            tex.SetPixels32(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        }

        GameObject MakePanel(Transform parent, string name, Color color, Vector2 pivotAnchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = pivotAnchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.sprite = RoundedSprite(); img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 1f;
            img.color = color;
            go.AddComponent<Shadow>().effectDistance = new Vector2(0, -6);
            return go;
        }

        // ---------- house style ----------
        static Font gameFont;
        /// <summary>The game's font (Fredoka, OFL) — falls back to Unity's built-in font.</summary>
        public static Font GameFont()
        {
            if (!gameFont) gameFont = Resources.Load<Font>("Fonts/Fredoka");
            if (!gameFont) gameFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return gameFont;
        }

        static Sprite rounded;
        /// <summary>A white rounded rectangle with a soft brass rim, 9-sliced so any panel size keeps round corners.</summary>
        public static Sprite RoundedSprite()
        {
            if (rounded) return rounded;
            const int n = 96, r = 30, rim = 5;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            var rimCol = new Color(0.78f, 0.58f, 0.32f);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, r, n - r), cy = Mathf.Clamp(y + 0.5f, r, n - r);
                    float d = r - Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy)); // distance inside the edge
                    float a = Mathf.Clamp01(d);
                    var c = Color.Lerp(rimCol, Color.white, Mathf.Clamp01(d - rim));
                    px[y * n + x] = new Color(c.r, c.g, c.b, a);
                }
            tex.SetPixels32(px); tex.Apply();
            rounded = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(r + 2, r + 2, r + 2, r + 2));
            return rounded;
        }
    }
}
