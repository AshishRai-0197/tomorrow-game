using System.Collections;
using UnityEngine;

namespace Tomorrow
{
    /// <summary>
    /// Playtest bot: walks (teleports) Pip through every step of Chapter 1 and logs where it gets stuck.
    /// Add it to any object in Play mode. Not used by the game itself.
    /// </summary>
    public class Autopilot : MonoBehaviour
    {
        public static string[] Route;
        public float speed = 4f;

        IEnumerator Start()
        {
            GameUI.AutoAdvance = true;
            Time.timeScale = speed;
            var d = ChapterDirector.Instance;
            foreach (var step in Route)
            {
                if (step.StartsWith("~"))   // wait this many game seconds (to test late returns)
                {
                    yield return new WaitForSeconds(float.Parse(step.Substring(1)));
                    Debug.Log($"[Autopilot] waited {step}  {d.DebugState}");
                    continue;
                }
                if (step.StartsWith("@"))   // walk to a named spot
                {
                    var t = d.spots.Find(step.Substring(1));
                    d.player.Teleport(t.position, 0);
                    Debug.Log($"[Autopilot] at {step}");
                    yield return new WaitForSeconds(0.5f);
                    continue;
                }
                Interactable target = null;
                float waited = 0;
                while (target == null && waited < 40f)
                {
                    foreach (var it in Interactable.All)
                        if (it.id == step && it.available && it.isActiveAndEnabled && (it.pickupItem != ItemId.None || d.HasHandler(step)) && d.IsIdle)
                            target = it;
                    waited += Time.unscaledDeltaTime;
                    yield return null;
                }
                if (target == null) { Debug.LogError($"[Autopilot] STUCK waiting for '{step}'  {d.DebugState}"); yield break; }
                var p = target.Point - d.player.transform.forward * 0.1f;
                d.player.Teleport(new Vector3(p.x, 0.05f, p.z), d.player.transform.eulerAngles.y);
                yield return null;
                yield return d.DebugUse(target);
                while (!d.IsIdle) yield return null;
                Debug.Log($"[Autopilot] used {step}  {d.DebugState}");
            }
            Debug.Log("[Autopilot] ROUTE COMPLETE");
        }
    }
}
