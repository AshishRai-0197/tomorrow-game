using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tomorrow.EditorTools
{
    /// <summary>
    /// QA tool: lists characters and pickups that sit inside other objects, in every chapter scene.
    /// Menu: Tomorrow ▸ Debug ▸ Overlap Report. The report is written to the Console and returned by Run().
    /// </summary>
    public static class OverlapReport
    {
        static readonly string[] Scenes = { "Chapter1", "Chapter2", "Chapter3" };

        [MenuItem("Tomorrow/Debug/Overlap Report")]
        static void Menu() => Debug.Log(Run());

        public static string Run(string only = null)
        {
            var sb = new StringBuilder();
            foreach (var s in Scenes)
            {
                if (only != null && s != only) continue;
                EditorSceneManager.OpenScene($"Assets/Game/Scenes/{s}.unity");
                sb.AppendLine($"== {s}");
                foreach (var line in Check()) sb.AppendLine("  " + line);
            }
            return sb.ToString();
        }

        struct Unit { public string name; public Bounds b; public bool actor; public GameObject go; }

        static IEnumerable<string> Check()
        {
            var units = new List<Unit>();
            var world = GameObject.Find("World");
            var inter = GameObject.Find("Interactables");
            var actors = new HashSet<GameObject>();
            foreach (var idle in Object.FindObjectsByType<Idle>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (idle.GetComponent<CapsuleCollider>() || (idle.GetComponent<Interactable>() && idle.GetComponent<Interactable>().pickupItem != ItemId.None))
                    actors.Add(idle.gameObject);
            foreach (var n in new[] { "Pip", "Tock" }) { var g = GameObject.Find(n); if (g) actors.Add(g); }

            void AddUnit(GameObject g, bool actor)
            {
                var rs = g.GetComponentsInChildren<Renderer>(true).Where(r => !(r is ParticleSystemRenderer)).ToArray();
                if (rs.Length == 0) return;
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                if (!actor && (b.size.x > 14 || b.size.z > 14 || b.size.y < 0.04f)) return;   // ground, water, roads
                units.Add(new Unit { name = Path(g), b = b, actor = actor, go = g });
            }
            foreach (var a in actors) AddUnit(a, true);
            if (world)
                foreach (Transform grp in world.transform)
                {
                    if (actors.Contains(grp.gameObject)) continue;
                    if (grp.GetComponent<Renderer>()) { AddUnit(grp.gameObject, false); continue; }
                    foreach (Transform c in grp) if (!actors.Contains(c.gameObject)) AddUnit(c.gameObject, false);
                }
            if (inter)
                foreach (Transform c in inter.transform)
                    if (!actors.Contains(c.gameObject) && !c.name.StartsWith("Pickup_")) AddUnit(c.gameObject, false);

            foreach (var a in units.Where(u => u.actor))
            {
                var core = a.b; core.Expand(-Vector3.Scale(a.b.size, new Vector3(0.35f, 0.3f, 0.35f)));
                foreach (var o in units)
                {
                    if (o.go == a.go || a.go.transform.IsChildOf(o.go.transform) || o.go.transform.IsChildOf(a.go.transform)) continue;
                    if (!core.Intersects(o.b)) continue;
                    var lo = Vector3.Max(core.min, o.b.min); var hi = Vector3.Min(core.max, o.b.max);
                    var d = hi - lo; float vol = d.x * d.y * d.z;
                    float coreVol = core.size.x * core.size.y * core.size.z;
                    if (coreVol <= 0 || vol / coreVol < 0.15f) continue;
                    yield return $"{a.name} @({a.b.center.x:F1},{a.b.center.z:F1})  overlaps  {o.name}  ({vol / coreVol:P0})";
                }
            }
        }

        static string Path(GameObject g)
        {
            var t = g.transform; var s = t.name;
            if (t.parent && t.parent.name != "World" && t.parent.name != "Interactables") s = t.parent.name + "/" + s;
            return s;
        }
    }
}
