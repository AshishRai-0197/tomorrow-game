using UnityEngine;

namespace Tomorrow
{
    /// <summary>Tiny synthesized sounds (no audio files needed): clock ticks, the due bell, pickups, the box arriving.</summary>
    public class Sfx : MonoBehaviour
    {
        AudioSource oneShot, ticking;
        AudioClip tick, bell, pickup, whoosh, build;
        const int Rate = 44100;

        void Awake()
        {
            oneShot = gameObject.AddComponent<AudioSource>();
            ticking = gameObject.AddComponent<AudioSource>();
            ticking.loop = true;
            ticking.volume = 0.35f;

            tick = Make(0.6f, (t) => (t % 0.5f < 0.012f ? Mathf.Sin(t * 2 * Mathf.PI * 2400) * (1 - (t % 0.5f) / 0.012f) : 0f)
                                    + (Mathf.Repeat(t + 0.17f, 0.33f) < 0.008f ? Mathf.Sin(t * 2 * Mathf.PI * 1800) * 0.5f : 0f));
            bell = Make(3.2f, (t) => Mathf.Exp(-t * 1.3f) * (Mathf.Sin(t * 2 * Mathf.PI * 392) + 0.5f * Mathf.Sin(t * 2 * Mathf.PI * 784 * 1.002f) + 0.25f * Mathf.Sin(t * 2 * Mathf.PI * 1180)) * 0.45f);
            pickup = Make(0.25f, (t) => Mathf.Exp(-t * 14f) * Mathf.Sin(t * 2 * Mathf.PI * Mathf.Lerp(660, 1320, t * 4)) * 0.5f);
            whoosh = Make(0.9f, (t) => (Random.value * 2 - 1) * Mathf.Sin(Mathf.Clamp01(t / 0.9f) * Mathf.PI) * 0.25f
                                     + Mathf.Exp(-Mathf.Abs(t - 0.82f) * 40f) * Mathf.Sin(t * 2 * Mathf.PI * 120) * 0.6f);
            build = Make(0.6f, (t) => (Mathf.Repeat(t, 0.15f) < 0.03f ? (Random.value * 2 - 1) * 0.5f : 0f) + Mathf.Exp(-t * 6) * Mathf.Sin(t * 2 * Mathf.PI * 523) * 0.3f * (t > 0.45f ? 1 : 0));
        }

        static AudioClip Make(float seconds, System.Func<float, float> f)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f(i / (float)Rate), -1f, 1f);
            var c = AudioClip.Create("sfx", n, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }

        public void Ticking(bool on)
        {
            if (on && !ticking.isPlaying) { ticking.clip = tick; ticking.Play(); }
            if (!on) ticking.Stop();
        }
        public void Tick() => oneShot.PlayOneShot(tick, 0.8f);
        public void Bell() => oneShot.PlayOneShot(bell, 0.9f);
        public void Pickup() => oneShot.PlayOneShot(pickup, 0.7f);
        public void Whoosh() => oneShot.PlayOneShot(whoosh, 0.8f);
        public void Build() => oneShot.PlayOneShot(build, 0.8f);
        public void Squawk()
        {
            if (!squawk) squawk = Make(0.45f, t => Mathf.Sin(t * 2 * Mathf.PI * (900 + 500 * Mathf.Sin(t * 40))) * Mathf.Exp(-t * 5) * 0.5f
                                                + (Random.value * 2 - 1) * 0.15f * Mathf.Exp(-t * 8));
            oneShot.PlayOneShot(squawk, 0.9f);
        }
        AudioClip squawk;
    }
}
