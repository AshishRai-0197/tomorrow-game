using UnityEngine;
using UnityEngine.InputSystem;

namespace Tomorrow
{
    /// <summary>Pip: WASD / arrow / gamepad stick movement relative to the camera, with a little walk hop.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public float speed = 2.6f;
        public float turnSpeed = 720f;
        public Transform visual;              // the model child, bobbed while walking
        public bool canMove = true;
        /// <summary>Trailer hook: a world-space walk direction that replaces player input when non-zero.</summary>
        public static Vector3 ScriptedMove;

        CharacterController cc;
        float vy, walkPhase;
        Vector3 visualBase;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            if (visual) visualBase = visual.localPosition;
        }

        public static Vector2 ReadMove()
        {
            var v = Vector2.zero;
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.wKey.isPressed || k.upArrowKey.isPressed) v.y += 1;
                if (k.sKey.isPressed || k.downArrowKey.isPressed) v.y -= 1;
                if (k.dKey.isPressed || k.rightArrowKey.isPressed) v.x += 1;
                if (k.aKey.isPressed || k.leftArrowKey.isPressed) v.x -= 1;
            }
            var g = Gamepad.current;
            if (g != null && g.leftStick.ReadValue().sqrMagnitude > 0.04f) v = g.leftStick.ReadValue();
            if (v == Vector2.zero && VirtualJoystick.Value.sqrMagnitude > 0.01f) v = VirtualJoystick.Value;   // keys/stick win
            return Vector2.ClampMagnitude(v, 1f);
        }

        void Update()
        {
            var input = canMove ? ReadMove() : Vector2.zero;
            var cam = Camera.main ? Camera.main.transform : transform;
            var fwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            var right = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
            var move = fwd * input.y + right * input.x;
            if (canMove && ScriptedMove.sqrMagnitude > 0.0001f) move = Vector3.ClampMagnitude(ScriptedMove, 1f);

            if (move.sqrMagnitude > 0.001f)
            {
                var target = Quaternion.LookRotation(move, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
            }

            vy = cc.isGrounded ? -1f : vy - 9.81f * Time.deltaTime;
            cc.Move((move * speed + Vector3.up * vy) * Time.deltaTime);

            // Hop-walk: the chibis aren't rigged, so a bounce and a waddle sell the motion.
            if (visual)
            {
                float m = move.magnitude;
                walkPhase += Time.deltaTime * 21f * m;   // one bob per step, matching LimbAnimator
                float hop = Mathf.Abs(Mathf.Sin(walkPhase)) * 0.025f * m;
                visual.localPosition = visualBase + Vector3.up * hop;
                visual.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(walkPhase) * 2.5f * m);
            }
        }

        /// <summary>Hold Pip still and out of physics (sitting on a boat deck, cutscenes).</summary>
        public void Freeze(bool on)
        {
            canMove = !on;
            enabled = !on;
            cc.enabled = !on;
        }

        public void Teleport(Vector3 pos, float yaw)
        {
            cc.enabled = false;
            transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            cc.enabled = true;
        }
    }
}
