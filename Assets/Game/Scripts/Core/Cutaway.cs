using System.Collections.Generic;
using UnityEngine;

namespace Tomorrow
{
    /// <summary>
    /// A wall or roof that gets out of the way of the camera, like a doll's house.
    /// It hides when it stands between the camera and Pip, and (roofs) while Pip is inside the room.
    /// Things hanging on a wall (clocks, windows, notes) hide together with it.
    /// The FollowCamera calls <see cref="UpdateAll"/> once per frame.
    /// </summary>
    public class Cutaway : MonoBehaviour
    {
        [Tooltip("Roofs: always hidden while Pip is inside the room.")]
        public bool hideWhenInside;
        [Tooltip("The room under this roof, in world space.")]
        public Bounds interior;

        static readonly List<Cutaway> all = new List<Cutaway>();

        readonly List<Renderer> rends = new List<Renderer>();
        Bounds bounds;
        bool hidden, ready;
        float showAt;                       // keep hidden until this time (stops flicker at the edge)

        void OnEnable() => all.Add(this);
        void OnDisable() { all.Remove(this); SetHidden(false); }

        void Start()
        {
            rends.AddRange(GetComponentsInChildren<Renderer>(true));
            if (rends.Count == 0) return;
            bounds = rends[0].bounds;
            foreach (var r in rends) bounds.Encapsulate(r.bounds);

            // A wall also takes the small things fixed to it (but never another wall, or the floor props).
            if (!hideWhenInside && transform.parent)
            {
                var zone = bounds; zone.Expand(new Vector3(0.3f, 0f, 0.3f));
                zone.SetMinMax(new Vector3(zone.min.x, 0f, zone.min.z), zone.max);   // down to the floor (the door)
                foreach (var r in transform.parent.GetComponentsInChildren<Renderer>(true))
                {
                    if (rends.Contains(r) || r.GetComponentInParent<Cutaway>() != null) continue;
                    if (r is ParticleSystemRenderer) continue;
                    var b = r.bounds;
                    if (b.center.y < 0.25f || !zone.Contains(b.center)) continue;   // floor props stay
                    if (b.size.x > bounds.size.x + 0.2f && b.size.z > bounds.size.z + 0.2f) continue;
                    rends.Add(r);
                }
            }
            ready = true;
        }

        void SetHidden(bool h)
        {
            if (hidden == h) return;
            hidden = h;
            foreach (var r in rends) if (r) r.enabled = !h;
        }

        /// <summary>Hide every part that blocks the view from <paramref name="cam"/> to Pip at <paramref name="pip"/>.</summary>
        public static void UpdateAll(Vector3 cam, Vector3 pip)
        {
            foreach (var c in all)
            {
                if (!c.ready) continue;
                bool hide = c.hideWhenInside && c.interior.Contains(pip + Vector3.up * 0.3f);
                if (!hide) hide = Blocks(c.bounds, cam, pip + Vector3.up * 1.0f) || Blocks(c.bounds, cam, pip + Vector3.up * 0.3f);
                if (hide) c.showAt = Time.time + 0.35f;
                c.SetHidden(Time.time < c.showAt);
            }
        }

        static bool Blocks(Bounds b, Vector3 from, Vector3 to)
        {
            var d = to - from;
            float len = d.magnitude;
            if (len < 0.01f) return false;
            b.Expand(-0.04f);                                    // grazing an edge doesn't count
            return b.IntersectRay(new Ray(from, d / len), out float hit) && hit < len - 0.25f;
        }
    }
}
