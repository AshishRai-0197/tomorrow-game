using System.Collections;
using UnityEngine;

namespace Tomorrow
{
    /// <summary>Gentle idle life for characters and props: breathe, bob, optionally face Pip.</summary>
    public class Idle : MonoBehaviour
    {
        public float bob = 0.015f, speed = 2f, spin = 0f;
        public bool facePlayer;
        public bool frozen;
        float phase, lastOffset;

        void Start() { phase = Random.value * 10f; }

        void Update()
        {
            if (frozen) return;
            phase += Time.deltaTime * speed;
            // apply only the change in bob, so cutscenes can still move this object around
            float off = Mathf.Sin(phase) * bob;
            transform.localPosition += Vector3.up * (off - lastOffset);
            lastOffset = off;
            if (spin != 0) transform.Rotate(0, spin * Time.deltaTime, 0, Space.World);
            if (facePlayer && ChapterDirector.Instance && ChapterDirector.Instance.player)
            {
                var to = ChapterDirector.Instance.player.transform.position - transform.position;
                to.y = 0;
                if (to.sqrMagnitude < 9f && to.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), Time.deltaTime * 4f);
            }
        }
    }
}
