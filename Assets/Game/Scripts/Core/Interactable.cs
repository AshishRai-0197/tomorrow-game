using System.Collections.Generic;
using UnityEngine;

namespace Tomorrow
{
    /// <summary>
    /// Anything Pip can use with E. The Chapter director decides what happens,
    /// keyed on <see cref="id"/>; pickups handle themselves.
    /// </summary>
    public class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();

        public string id;
        public string prompt = "Use";
        public float range = 1.4f;
        [Tooltip("Hidden from the prompt until the story enables it.")]
        public bool available = true;

        [Header("Pickup (optional)")]
        public ItemId pickupItem = ItemId.None;
        public int pickupAmount = 1;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public Vector3 Point
        {
            get
            {
                var p = transform.position;
                return new Vector3(p.x, 0f, p.z);
            }
        }

        public virtual string Prompt => pickupItem != ItemId.None ? $"Pick up {Items.Name(pickupItem)}" : prompt;
    }
}
