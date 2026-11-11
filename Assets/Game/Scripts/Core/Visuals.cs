using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tomorrow
{
    /// <summary>Material helpers that work on glTFast and URP materials alike.</summary>
    public static class Visuals
    {
        static readonly string[] ColorProps = { "_BaseColor", "baseColorFactor", "_Color" };
        static readonly string[] EmissionProps = { "_EmissionColor", "emissiveFactor" };

        static readonly Dictionary<Renderer, Material[]> originals = new Dictionary<Renderer, Material[]>();

        /// <summary>Blend every material on the object toward a colour (t = 0 restores the original look).</summary>
        public static void Tint(GameObject go, Color tint, float t)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (!originals.TryGetValue(r, out var src))
                {
                    src = r.sharedMaterials;
                    originals[r] = src;
                    var copies = new Material[src.Length];
                    for (int i = 0; i < src.Length; i++) copies[i] = src[i] ? new Material(src[i]) : null;
                    r.materials = copies;
                }
                var mats = r.materials;
                for (int i = 0; i < mats.Length && i < src.Length; i++)
                {
                    if (!mats[i] || !src[i]) continue;
                    foreach (var p in ColorProps)
                        if (src[i].HasProperty(p)) mats[i].SetColor(p, Color.Lerp(src[i].GetColor(p), tint, t));
                }
            }
        }

        /// <summary>Washes an object out to grey — "tomorrow gets very grey".</summary>
        public static void Grey(GameObject go, float t)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (!originals.TryGetValue(r, out _)) Tint(r.gameObject, Color.white, 0f);
                var src = originals[r];
                var mats = r.materials;
                for (int i = 0; i < mats.Length && i < src.Length; i++)
                {
                    if (!mats[i] || !src[i]) continue;
                    foreach (var p in ColorProps)
                        if (src[i].HasProperty(p))
                        {
                            var c = src[i].GetColor(p);
                            float g = c.grayscale * 0.8f + 0.1f;
                            mats[i].SetColor(p, Color.Lerp(c, new Color(g, g, g, c.a), t));
                        }
                }
            }
        }

        public static void Glow(GameObject go, Color color, float strength)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                foreach (var m in r.materials)
                {
                    m.EnableKeyword("_EMISSION");
                    foreach (var p in EmissionProps)
                        if (m.HasProperty(p)) m.SetColor(p, color * strength);
                }
        }

        public static Material Lit(Color c, float smoothness = 0.3f)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var m = new Material(shader);
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smoothness);
            return m;
        }

        public static Bounds WorldBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }
    }

}
