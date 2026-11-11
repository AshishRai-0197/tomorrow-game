using System.Collections;
using System.IO;
using UnityEngine;

namespace Tomorrow
{
    /// <summary>
    /// Trailer and screenshot recorder (editor/testing only, not used by the game).
    /// Plays a route like Autopilot, but steps marked with * are played at normal speed: Pip walks there,
    /// dialogue waits for each voice line, and every frame is saved as a JPG at 30 fps game time.
    /// Route tokens:  id  = fast step (teleport, skipped dialogue, not recorded)
    ///                *id = recorded step   #name = start a new clip folder   ~N / *~N = wait N seconds
    ///                @spot / *@spot = go to a named spot
    /// Each clip folder also gets voices.txt ("frame clipName" per voice line) to rebuild the audio.
    /// </summary>
    public class TrailerPilot : MonoBehaviour
    {
        public static string[] Route;
        public static string OutDir;
        public const int Fps = 30;

        string clipDir; int frame; bool recording;
        float since, voiceEnds; int voiceSeen;
        StreamWriter voices;

        IEnumerator Start()
        {
            var d = ChapterDirector.Instance;
            GameUI.AutoAdvance = true; GameUI.AutoChoice = 0;
            GameUI.AdvanceOverride = Advance;
            foreach (var n in new[] { "On-Screen Controls", "Controls Help" })
            { var g = GameObject.Find(n); if (g) g.SetActive(false); }   // clean frames: no touch buttons or key help
            StartCoroutine(Capture());
            foreach (var raw in Route)
            {
                if (raw.StartsWith("#")) { NewClip(raw.Substring(1)); continue; }
                bool rec = raw.StartsWith("*");
                var step = rec ? raw.Substring(1) : raw;
                SetRecording(rec);
                if (step.StartsWith("~")) { yield return new WaitForSeconds(float.Parse(step.Substring(1))); continue; }
                if (step.StartsWith("@"))
                {
                    var t = d.spots.Find(step.Substring(1));
                    if (rec) yield return Walk(d, t.position, 0.4f); else d.player.Teleport(t.position, 0);
                    yield return new WaitForSeconds(0.4f);
                    continue;
                }
                Interactable target = null; float waited = 0;
                while (target == null && waited < 60f)
                {
                    foreach (var it in Interactable.All)
                        if (it.id == step && it.available && it.isActiveAndEnabled && (it.pickupItem != ItemId.None || d.HasHandler(step)) && d.IsIdle)
                            target = it;
                    waited += Time.unscaledDeltaTime;
                    yield return null;
                }
                if (target == null) { Debug.LogError($"[Trailer] STUCK waiting for '{step}'  {d.DebugState}"); break; }
                if (rec) yield return Walk(d, target.Point, 1.1f);
                else
                {
                    var p = target.Point - d.player.transform.forward * 0.1f;
                    d.player.Teleport(new Vector3(p.x, 0.05f, p.z), d.player.transform.eulerAngles.y);
                    yield return null;
                }
                since = 0;
                yield return d.DebugUse(target);
                while (!d.IsIdle) yield return null;
                Debug.Log($"[Trailer] used {raw}");
            }
            SetRecording(false);
            voices?.Close();
            GameUI.AdvanceOverride = null;
            Debug.Log("[Trailer] ROUTE COMPLETE");
        }

        void NewClip(string name)
        {
            voices?.Close();
            clipDir = Path.Combine(OutDir, name); Directory.CreateDirectory(clipDir);
            voices = new StreamWriter(Path.Combine(clipDir, "voices.txt"));
            frame = 0;
        }

        void SetRecording(bool on)
        {
            recording = on && clipDir != null;
            Time.timeScale = recording ? 1f : 4f;
            Time.captureFramerate = recording ? Fps : 0;
        }

        // Dialogue pacing in game time: wait for the voice clip to finish, then a short beat.
        bool Advance()
        {
            if (!recording) return Time.frameCount % 3 == 0;
            if (since < 1.4f || Time.time < voiceEnds + 0.45f) return false;
            since = 0; return true;
        }

        void Update()
        {
            since += Time.deltaTime;
            if (GameUI.VoiceCount != voiceSeen)
            {
                voiceSeen = GameUI.VoiceCount;
                if (GameUI.LastVoice)
                {
                    voiceEnds = Time.time + GameUI.LastVoice.length;
                    if (recording) voices?.WriteLine($"{frame} {GameUI.LastVoice.name}");
                }
            }
        }

        IEnumerator Walk(ChapterDirector d, Vector3 to, float reach)
        {
            float t = 0, stuck = 0; var last = d.player.transform.position;
            while (t < 14f)
            {
                var delta = to - d.player.transform.position; delta.y = 0;
                if (delta.magnitude < reach) break;
                PlayerController.ScriptedMove = delta.normalized;
                yield return null;
                t += Time.deltaTime;
                var now = d.player.transform.position;
                stuck = (now - last).magnitude < 0.3f * Time.deltaTime ? stuck + Time.deltaTime : 0;
                last = now;
                if (stuck > 0.8f) break;   // blocked: the next step teleports (a cut in the edit)
            }
            PlayerController.ScriptedMove = Vector3.zero;
            var p = to - d.player.transform.forward * 0.1f;
            if ((d.player.transform.position - new Vector3(p.x, d.player.transform.position.y, p.z)).magnitude > reach + 0.5f)
                d.player.Teleport(new Vector3(p.x, 0.05f, p.z), d.player.transform.eulerAngles.y);
        }

        // Renders the game camera (with the UI moved onto it) into a 1920x1080 texture every frame,
        // so recording doesn't depend on the editor's Game view being visible.
        RenderTexture rt; Texture2D grab;
        IEnumerator Capture() { yield break; }

        void LateUpdate()
        {
            if (!recording) return;
            var cam = Camera.main; if (!cam) return;
            if (!rt) { rt = new RenderTexture(1920, 1080, 24); grab = new Texture2D(1920, 1080, TextureFormat.RGB24, false); }
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (c.isRootCanvas && (c.renderMode == RenderMode.ScreenSpaceOverlay || c.worldCamera != cam) && c.renderMode != RenderMode.WorldSpace)
                {
                    c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = cam;
                    c.planeDistance = cam.nearClipPlane + 0.02f;
                }
            var prev = cam.targetTexture;
            cam.targetTexture = rt; cam.Render(); cam.targetTexture = prev;
            var old = RenderTexture.active; RenderTexture.active = rt;
            grab.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); grab.Apply(false);
            RenderTexture.active = old;
            File.WriteAllBytes(Path.Combine(clipDir, $"{frame:00000}.jpg"), grab.EncodeToJPG(90));
            frame++;
        }

        void OnDestroy()
        {
            voices?.Close();
            Time.captureFramerate = 0; Time.timeScale = 1f;
            PlayerController.ScriptedMove = Vector3.zero; GameUI.AdvanceOverride = null;
        }
    }
}
