using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Tomorrow
{
    /// <summary>Title screen: the game's name and a button per chapter. Keys 1–3 work too.</summary>
    public class TitleMenu : MonoBehaviour
    {
        static readonly string[] Chapters = { "Chapter1", "Chapter2", "Chapter3" };
        static readonly string[] Names = { "Chapter 1 - Borrow. Use. Build. Return.", "Chapter 2 - The Harbor", "Chapter 3 - Market Day" };

        void Start()
        {
            Time.timeScale = 1f;
            var font = GameUI.GameFont();
            var cgo = new GameObject("Title Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = cgo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var sc = cgo.GetComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;

            var logoTex = Resources.Load<Texture2D>("UI/Logo");
            if (logoTex)
            {
                var lg = new GameObject("Logo", typeof(RectTransform), typeof(RawImage));
                lg.transform.SetParent(cgo.transform, false);
                var lrt = lg.GetComponent<RectTransform>();
                float lw = 900f; lrt.sizeDelta = new Vector2(lw, lw * logoTex.height / logoTex.width); lrt.anchoredPosition = new Vector2(0, 320);
                lg.GetComponent<RawImage>().texture = logoTex;
                lg.AddComponent<LogoBob>();
            }
            else Label(cgo.transform, font, "TOMORROW", 120, new Vector2(0, 300), new Color(1f, 0.92f, 0.75f), FontStyle.Bold);
            Label(cgo.transform, font, "Borrow from tomorrow. Build it today. Give it back by sunset.", 34, new Vector2(0, 160), new Color(1f, 0.96f, 0.86f), FontStyle.Normal);
            for (int i = 0; i < Chapters.Length; i++)
            {
                int idx = i;
                var b = new GameObject(Chapters[i], typeof(RectTransform), typeof(Image), typeof(Button));
                b.transform.SetParent(cgo.transform, false);
                var rt = b.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(820, 96); rt.anchoredPosition = new Vector2(0, 40 - i * 125);
                var bimg = b.GetComponent<Image>();
                bimg.sprite = GameUI.RoundedSprite(); bimg.type = Image.Type.Sliced;
                bimg.color = new Color(0.98f, 0.80f, 0.48f, 0.97f);
                b.AddComponent<Shadow>().effectDistance = new Vector2(0, -6);
                var cb = b.GetComponent<Button>().colors; cb.highlightedColor = new Color(1f, 0.92f, 0.75f); cb.pressedColor = new Color(0.9f, 0.7f, 0.4f); b.GetComponent<Button>().colors = cb;
                b.GetComponent<Button>().onClick.AddListener(() => Load(idx));
                Label(b.transform, font, $"{i + 1}.  {Names[i]}", 34, Vector2.zero, new Color(0.2f, 0.14f, 0.1f), FontStyle.Bold).raycastTarget = false;
            }
            Label(cgo.transform, font, "Click a chapter, or press 1, 2 or 3", 26, new Vector2(0, -380), new Color(1, 1, 1, 0.7f), FontStyle.Normal);
        }

        void Update()
        {
            var k = Keyboard.current;
            if (k == null) return;
            if (k.digit1Key.wasPressedThisFrame) Load(0);
            if (k.digit2Key.wasPressedThisFrame) Load(1);
            if (k.digit3Key.wasPressedThisFrame) Load(2);
        }

        void Load(int i)
        {
            if (Application.CanStreamedLevelBeLoaded(Chapters[i])) SceneManager.LoadScene(Chapters[i]);
        }

        static Text Label(Transform parent, Font font, string text, int size, Vector2 pos, Color color, FontStyle style)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(1600, size * 1.6f); rt.anchoredPosition = pos;
            var t = go.GetComponent<Text>();
            t.font = font; t.text = text; t.fontSize = size; t.color = color; t.fontStyle = style; t.alignment = TextAnchor.MiddleCenter;
            go.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);
            return t;
        }
    }

    /// <summary>Gentle float for the title logo.</summary>
    public class LogoBob : MonoBehaviour
    {
        Vector2 p0; RectTransform rt;
        void Start() { rt = (RectTransform)transform; p0 = rt.anchoredPosition; }
        void Update()
        {
            rt.anchoredPosition = p0 + new Vector2(0, Mathf.Sin(Time.time * 1.3f) * 8f);
            rt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 0.8f) * 1.2f);
        }
    }
}
