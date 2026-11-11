using UnityEngine;

namespace Tomorrow
{
    /// <summary>
    /// Procedural walk cycle for the chibi models: swings the *_Leg_L/R and *_Arm_L/R parts
    /// (pivoted at hips and shoulders) in step with how fast the character is actually moving.
    /// </summary>
    public class LimbAnimator : MonoBehaviour
    {
        [Tooltip("Whatever actually moves (the CharacterController root). Defaults to the parent.")]
        public Transform mover;
        public float legSwing = 38f, armSwing = 32f;
        [Tooltip("Steps per metre walked.")]
        public float stride = 2.6f;
        public float runSpeed = 2.6f;

        Transform legL, legR, armL, armR;
        Quaternion legL0, legR0, armL0, armR0;
        Vector3 lastPos;
        float phase, amount, idleT;

        void Awake()
        {
            if (!mover) mover = transform.parent ? transform.parent : transform;
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name.EndsWith("_Leg_L")) legL = t;
                else if (t.name.EndsWith("_Leg_R")) legR = t;
                else if (t.name.EndsWith("_Arm_L")) armL = t;
                else if (t.name.EndsWith("_Arm_R")) armR = t;
            }
            if (legL) legL0 = legL.localRotation;
            if (legR) legR0 = legR.localRotation;
            if (armL) armL0 = armL.localRotation;
            if (armR) armR0 = armR.localRotation;
            lastPos = mover.position;
        }

        void LateUpdate()
        {
            var p = mover.position;
            var delta = p - lastPos; delta.y = 0;
            lastPos = p;
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            float speed = delta.magnitude / dt;
            if (speed > 12f) speed = 0f;                     // a teleport, not a step

            amount = Mathf.MoveTowards(amount, Mathf.Clamp01(speed / runSpeed), dt * 6f);
            phase += delta.magnitude * stride * Mathf.PI;    // one full cycle every two steps
            idleT += dt;

            float s = Mathf.Sin(phase) * amount;
            float breathe = Mathf.Sin(idleT * 2.2f) * 3f * (1f - amount);

            if (legL) legL.localRotation = legL0 * Quaternion.Euler(s * legSwing, 0, 0);
            if (legR) legR.localRotation = legR0 * Quaternion.Euler(-s * legSwing, 0, 0);
            // arms swing opposite to the legs, and hang a little out from the body
            if (armL) armL.localRotation = armL0 * Quaternion.Euler(-s * armSwing, 0, breathe);
            if (armR) armR.localRotation = armR0 * Quaternion.Euler(s * armSwing, 0, -breathe);
        }
    }
}
