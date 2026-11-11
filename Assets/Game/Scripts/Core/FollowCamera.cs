using UnityEngine;
using UnityEngine.InputSystem;

namespace Tomorrow
{
    /// <summary>Cozy diorama camera: follows Pip from above and behind. Hold right mouse (or right stick) to orbit, scroll to zoom.</summary>
    public class FollowCamera : MonoBehaviour
    {
        public Transform target;
        public float distance = 6.5f;
        public float pitch = 42f;
        public float yaw = 0f;
        public float lookHeight = 0.5f;
        public float smooth = 6f;

        // A cutscene can take the camera somewhere else for a moment.
        Transform overridePose;
        public void Focus(Transform pose) => overridePose = pose;
        public void Release() => overridePose = null;

        void LateUpdate()
        {
            if (overridePose)
            {
                transform.position = Vector3.Lerp(transform.position, overridePose.position, Time.deltaTime * 3f);
                transform.rotation = Quaternion.Slerp(transform.rotation, overridePose.rotation, Time.deltaTime * 3f);
                return;
            }
            if (!target) return;

            var m = Mouse.current;
            if (m != null)
            {
                if (m.rightButton.isPressed) yaw += m.delta.ReadValue().x * 0.2f;
                distance = Mathf.Clamp(distance - m.scroll.ReadValue().y * 0.004f, 3.5f, 11f);
            }
            var g = Gamepad.current;
            if (g != null) yaw += g.rightStick.ReadValue().x * 120f * Time.deltaTime;

            var rot = Quaternion.Euler(pitch, yaw, 0);
            var focus = target.position + Vector3.up * lookHeight;
            var want = focus - rot * Vector3.forward * distance;
            transform.position = Vector3.Lerp(transform.position, want, 1f - Mathf.Exp(-smooth * Time.deltaTime));
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, 1f - Mathf.Exp(-smooth * Time.deltaTime));
        }

        public void SnapNow()
        {
            if (!target) return;
            var rot = Quaternion.Euler(pitch, yaw, 0);
            transform.SetPositionAndRotation(target.position + Vector3.up * lookHeight - rot * Vector3.forward * distance, rot);
        }
    }
}
