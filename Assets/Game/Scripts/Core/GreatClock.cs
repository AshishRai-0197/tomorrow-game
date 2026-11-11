using System.Collections;
using UnityEngine;

namespace Tomorrow
{
    /// <summary>The Great Clock's hands: they tick only when the story says so.</summary>
    public class GreatClock : MonoBehaviour
    {
        public Transform minuteHand, hourHand;
        public IEnumerator TickOnce()
        {
            var from = minuteHand.localRotation;
            var to = from * Quaternion.Euler(0, 0, -6f);
            for (float t = 0; t < 0.25f; t += Time.deltaTime)
            {
                minuteHand.localRotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0, 1, t / 0.25f));
                yield return null;
            }
            minuteHand.localRotation = to;
        }
    }
}
