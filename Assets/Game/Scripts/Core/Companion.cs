using System.Collections.Generic;
using UnityEngine;

namespace Tomorrow
{
    /// <summary>
    /// Tock walks on his own. When Pip is far he follows Pip's footsteps (so he goes round walls and
    /// through doors rather than through them). When Pip is close he potters about near Pip.
    /// He has a CharacterController, so walls, furniture and people all block him.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class Companion : MonoBehaviour
    {
        public Transform follow;
        public Transform visual;                 // model child, for the little hop
        public bool active;

        [Header("Distances")]
        public float closeEnough = 1.1f;         // inside this: wander near Pip
        public float catchUp = 1.8f;             // beyond this: follow Pip's trail
        public float wanderMin = 0.6f, wanderMax = 1.4f;

        [Header("Speeds")]
        public float followSpeed = 3.0f;
        public float wanderSpeed = 1.1f;

        CharacterController cc;
        readonly List<Vector3> trail = new List<Vector3>();
        Vector3 wanderTarget;
        bool hasWander, following;
        float wanderTimer, stuckTimer, vy, hopPhase, sideStepTimer, rethink;
        Vector3 sideStep, followTarget;
        Vector3 lastPos, visualBase;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            if (visual) visualBase = visual.localPosition;
        }

        void Start()
        {
            if (follow)
            {
                var other = follow.GetComponent<Collider>();
                if (other) Physics.IgnoreCollision(cc, other);   // never shove Pip out of a doorway
            }
        }

        /// <summary>Place Tock somewhere directly (cutscenes).</summary>
        public void Warp(Vector3 pos)
        {
            bool was = cc.enabled;
            cc.enabled = false;
            transform.position = pos;
            cc.enabled = was;
            trail.Clear(); hasWander = false;
            lastPos = pos;
        }

        void Update()
        {
            cc.enabled = active;                    // when not active, cutscenes move him by hand
            if (!active || !follow) return;

            RecordTrail();

            var me = Flat(transform.position);
            var pip = Flat(follow.position);
            float dist = Vector3.Distance(me, pip);

            if (dist > catchUp) following = true;
            else if (dist < closeEnough) following = false;

            Vector3 target; float speed;
            if (following)
            {
                hasWander = false;
                rethink -= Time.deltaTime;
                if (rethink <= 0f) { rethink = 0.1f; followTarget = FurthestVisibleTrailPoint(me, pip); }
                target = followTarget;
                speed = Mathf.Lerp(followSpeed * 0.8f, followSpeed * 1.4f, Mathf.InverseLerp(catchUp, 6f, dist));
            }
            else
            {
                wanderTimer -= Time.deltaTime;
                if (!hasWander || wanderTimer <= 0f) PickWanderPoint(pip);
                target = wanderTarget;
                speed = Vector3.Distance(me, wanderTarget) < 0.12f ? 0f : wanderSpeed;
            }

            var to = target - me;
            Vector3 move = to.magnitude > 0.05f ? to.normalized * speed : Vector3.zero;
            if (sideStepTimer > 0f) { sideStepTimer -= Time.deltaTime; move = sideStep * wanderSpeed; }

            vy = cc.isGrounded ? -1f : vy - 9.81f * Time.deltaTime;
            cc.Move((move + Vector3.up * vy) * Time.deltaTime);

            // Blocked? (pushing into something and not getting anywhere) → choose another way.
            float moved = Vector3.Distance(Flat(transform.position), Flat(lastPos));
            lastPos = transform.position;
            if (move.sqrMagnitude > 0.01f && moved < speed * Time.deltaTime * 0.25f) stuckTimer += Time.deltaTime;
            else stuckTimer = 0f;
            if (stuckTimer > 0.5f)
            {
                stuckTimer = 0f;
                if (following)
                {
                    // shuffle sideways a moment to get round a corner, then look again
                    sideStep = Vector3.Cross(Vector3.up, to.normalized) * (Random.value < 0.5f ? 1f : -1f);
                    sideStepTimer = 0.35f;
                    rethink = 0f;
                }
                else PickWanderPoint(pip);
            }

            // Hopelessly lost (e.g. a cutscene moved Pip far away): reappear just behind Pip.
            if (dist > 14f) Warp(follow.position - follow.forward * 0.8f);

            // Face where he walks; when standing still, look at Pip.
            var look = move.sqrMagnitude > 0.01f ? move : (pip - me);
            if (look.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), Time.deltaTime * 8f);

            if (visual)
            {
                float m = Mathf.Clamp01(move.magnitude / followSpeed);
                hopPhase += Time.deltaTime * 16f * m;
                visual.localPosition = visualBase + Vector3.up * Mathf.Abs(Mathf.Sin(hopPhase)) * 0.03f * m;
            }
        }

        /// <summary>
        /// Walk toward the newest of Pip's footsteps that Tock can reach in a straight line.
        /// Older footsteps lead back through doorways, so he never cuts through a wall.
        /// </summary>
        Vector3 FurthestVisibleTrailPoint(Vector3 me, Vector3 pip)
        {
            if (Clear(me, pip)) return pip;
            int checks = 0;
            for (int i = trail.Count - 1; i >= 0 && checks < 60; i -= 2, checks++)
                if (Clear(me, trail[i])) return trail[i];
            return trail.Count > 0 ? trail[0] : pip;
        }

        bool Clear(Vector3 a, Vector3 b)
        {
            var up = Vector3.up * 0.35f;
            var d = b - a;
            if (d.sqrMagnitude < 0.0001f) return true;
            bool was = cc.enabled; cc.enabled = false;            // don't hit ourselves
            bool blocked = Physics.SphereCast(a + up, 0.14f, d.normalized, out var hit, d.magnitude, ~0, QueryTriggerInteraction.Ignore)
                           && hit.transform != follow;
            cc.enabled = was;
            return !blocked;
        }

        void RecordTrail()
        {
            var p = Flat(follow.position);
            if (trail.Count == 0 || Vector3.Distance(trail[trail.Count - 1], p) > 0.2f)
            {
                trail.Add(p);
                if (trail.Count > 200) trail.RemoveAt(0);
            }
        }

        void PickWanderPoint(Vector3 pip)
        {
            wanderTimer = Random.Range(2f, 4.5f);
            for (int i = 0; i < 8; i++)
            {
                var dir = Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward;
                var p = pip + dir * Random.Range(wanderMin, wanderMax);
                var eye = Vector3.up * 0.3f;
                // only spots in clear view of Pip (no wandering behind walls), with ground under them
                if (Physics.Linecast(pip + eye, p + eye, out var hit, ~0, QueryTriggerInteraction.Ignore) && hit.collider != cc) continue;
                if (!Physics.Raycast(p + Vector3.up * 1f, Vector3.down, 1.5f, ~0, QueryTriggerInteraction.Ignore)) continue;
                wanderTarget = p; hasWander = true;
                return;
            }
            wanderTarget = Flat(transform.position); hasWander = true;   // nowhere good: stay put a moment
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0, v.z);
    }
}
