using UnityEngine;

namespace Tomorrow
{
    /// <summary>Time of day from 7:00 (morning) to 19:00 (sunset) and on into night. Moves the sun and tints the sky.</summary>
    public class DayClock : MonoBehaviour
    {
        public Light sun;
        [Tooltip("Real seconds per in-game hour.")]
        public float secondsPerHour = 100f;
        public float hour = 7f;
        [Tooltip("The clock won't pass this hour until the story allows it.")]
        public float cap = 18f;
        public bool running = true;
        [System.NonSerialized] public int holds;   // > 0 while dialogue or a note is open: time stands still

        public Gradient sunColor;
        public Gradient skyColor;

        public const float Noon = 12f, Sunset = 19f;

        void Reset() => BuildDefaultGradients();
        void Awake() => BuildDefaultGradients();

        void BuildDefaultGradients()
        {
            sunColor = new Gradient();
            sunColor.SetKeys(new[]
            {
                new GradientColorKey(new Color(1f, 0.75f, 0.55f), 0f),     // 7:00
                new GradientColorKey(new Color(1f, 0.96f, 0.88f), 0.35f),  // midday
                new GradientColorKey(new Color(1f, 0.62f, 0.35f), 0.85f),  // late
                new GradientColorKey(new Color(0.45f, 0.5f, 0.9f), 1f),    // night
            }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
            skyColor = new Gradient();
            skyColor.SetKeys(new[]
            {
                new GradientColorKey(new Color(0.62f, 0.66f, 0.72f), 0f),
                new GradientColorKey(new Color(0.75f, 0.78f, 0.82f), 0.35f),
                new GradientColorKey(new Color(0.75f, 0.52f, 0.45f), 0.85f),
                new GradientColorKey(new Color(0.16f, 0.18f, 0.32f), 1f),
            }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
        }

        void Update()
        {
            if (running && holds == 0 && hour < cap)
                hour = Mathf.Min(cap, hour + Time.deltaTime / Mathf.Max(1f, secondsPerHour));
            Apply();
        }

        /// <summary>0 at 7:00, 1 at 21:00 (full night).</summary>
        float T => Mathf.InverseLerp(7f, 21f, hour);

        public void Apply()
        {
            float t = T;
            if (sun)
            {
                float elevation = Mathf.Lerp(18f, -8f, Mathf.Abs(t * 2f - 0.85f) / 1.15f);
                elevation = Mathf.Max(elevation, 12f); // keep a soft key light even at night (moonlight)
                sun.transform.rotation = Quaternion.Euler(elevation + 22f, Mathf.Lerp(-60f, 200f, t), 0);
                sun.color = sunColor.Evaluate(t);
                sun.intensity = Mathf.Lerp(1.35f, 0.35f, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.7f, 1f, t)));
            }
            var sky = skyColor.Evaluate(t);
            RenderSettings.ambientLight = sky;
            if (Camera.main) Camera.main.backgroundColor = Color.Lerp(sky, new Color(0.55f, 0.75f, 0.95f), t < 0.8f ? 0.55f : 0f);
            RenderSettings.fogColor = Camera.main ? Camera.main.backgroundColor : sky;
        }

        public string Label
        {
            get
            {
                int h = Mathf.FloorToInt(hour) % 24, m = Mathf.FloorToInt((hour - Mathf.Floor(hour)) * 60f);
                string part = hour < 12f ? "Morning" : hour < 17f ? "Afternoon" : hour < 19f ? "Evening" : "Night";
                int h12 = h % 12 == 0 ? 12 : h % 12;
                return $"{part}  {h12}:{m:00} {(h < 12 ? "am" : "pm")}";
            }
        }
    }
}
