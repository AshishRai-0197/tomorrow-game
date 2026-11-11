using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tomorrow
{
    /// <summary>
    /// Background music. Created automatically, survives scene changes, and cross-fades to the
    /// track for each scene (Resources/Music, made by Tools/make_music.py). Ducks under voice lines.
    /// </summary>
    public class MusicPlayer : MonoBehaviour
    {
        public static float Volume = 0.32f;
        const float DuckTo = 0.4f;   // fraction of Volume while someone speaks

        AudioSource a, b;            // two sources so tracks can cross-fade
        string playing;
        float duck = 1f;

        static string TrackFor(string scene)
        {
            switch (scene)
            {
                case "Chapter1": return "workshop";
                case "Chapter2": return "harbor";
                case "Chapter3": return "market";
                default: return "title";
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<MusicPlayer>()) return;
            var go = new GameObject("Music");
            DontDestroyOnLoad(go);
            var mp = go.AddComponent<MusicPlayer>();
            mp.Switch(SceneManager.GetActiveScene().name);
            SceneManager.sceneLoaded += (s, _) => { if (mp) mp.Switch(s.name); };
        }

        AudioSource NewSource()
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.loop = true; s.playOnAwake = false; s.spatialBlend = 0f; s.volume = 0f; s.priority = 200;
            return s;
        }

        void Switch(string scene)
        {
            var name = TrackFor(scene);
            if (name == playing) return;
            var clip = Resources.Load<AudioClip>("Music/" + name);
            if (!clip) return;
            playing = name;
            if (a == null) { a = NewSource(); b = NewSource(); }
            StopAllCoroutines();
            StartCoroutine(CrossFade(clip));
        }

        IEnumerator CrossFade(AudioClip clip)
        {
            fading = true;
            var from = a; var to = b;
            (a, b) = (b, a);
            to.clip = clip; to.time = 0f; to.volume = 0f; to.Play();
            float v0 = from.volume;
            for (float t = 0; t < 1.5f; t += Time.unscaledDeltaTime)
            {
                float k = t / 1.5f;
                from.volume = v0 * (1 - k);
                to.volume = Volume * duck * k;
                yield return null;
            }
            from.Stop(); from.volume = 0f;
            fading = false;
        }

        bool fading = true;

        void Update()
        {
            duck = Mathf.MoveTowards(duck, GameUI.VoicePlaying ? DuckTo : 1f, Time.unscaledDeltaTime * 2.5f);
            if (!fading && a) a.volume = Volume * duck;
        }
    }
}
