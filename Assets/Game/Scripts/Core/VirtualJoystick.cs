using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Tomorrow
{
    /// <summary>On-screen stick: drag with the mouse or a finger to walk.</summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public static Vector2 Value;
        public RectTransform knob;
        public float radius = 90f;
        RectTransform rt;

        void Awake() => rt = (RectTransform)transform;
        void OnDisable() { Value = Vector2.zero; if (knob) knob.anchoredPosition = Vector2.zero; }

        void Update()
        {
            // Safety net: if nothing is pressing the screen any more, let go of the stick.
            bool mouse = Mouse.current != null && Mouse.current.leftButton.isPressed;
            bool touch = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed;
            if (!mouse && !touch && Value != Vector2.zero) OnPointerUp(null);
        }

        public void OnPointerDown(PointerEventData e) => OnDrag(e);

        public void OnDrag(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, e.pressEventCamera, out var local);
            var v = Vector2.ClampMagnitude(local, radius);
            knob.anchoredPosition = v;
            Value = v / radius;
        }

        public void OnPointerUp(PointerEventData e)
        {
            Value = Vector2.zero;
            knob.anchoredPosition = Vector2.zero;
        }
    }
}
