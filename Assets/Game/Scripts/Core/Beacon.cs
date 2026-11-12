using UnityEngine;

namespace Tomorrow
{
    /// <summary>The spinning gold marker above a part that can be picked up, so parts are easy to spot.</summary>
    public class Beacon : MonoBehaviour
    {
        public float spin = 120f, bob = 0.08f, speed = 3f;
        Vector3 p0;

        void Start() => p0 = transform.localPosition;

        void Update()
        {
            transform.localPosition = p0 + Vector3.up * Mathf.Sin(Time.time * speed) * bob;
            transform.Rotate(0, spin * Time.deltaTime, 0, Space.World);
        }
    }
}
