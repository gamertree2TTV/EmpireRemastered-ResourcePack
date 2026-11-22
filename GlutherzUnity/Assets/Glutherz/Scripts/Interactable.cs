using System.Collections.Generic;
using UnityEngine;

namespace Glutherz
{
    /// <summary>Objekt, das der Spieler aus der Nähe mit [E] benutzen kann.</summary>
    public abstract class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();

        public abstract string Prompt { get; }
        public abstract void Use();

        protected virtual void OnEnable() { All.Add(this); }
        protected virtual void OnDisable() { All.Remove(this); }
    }
}
