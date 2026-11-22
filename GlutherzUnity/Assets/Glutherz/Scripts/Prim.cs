using UnityEngine;

namespace Glutherz
{
    /// <summary>Alles, was der Spieler mit der Waffe treffen kann.</summary>
    public interface IDamageable
    {
        string Title { get; }
        int Defense { get; }
        float HpFraction { get; }
        void TakeHit(int damage, bool crit);
    }

    /// <summary>Hilfen, um die Welt aus einfachen Grundformen zu bauen (keine Modelle nötig).</summary>
    public static class Prim
    {
        public static GameObject Make(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale, Color color, bool collider)
        {
            var go = GameObject.CreatePrimitive(type);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            Tint(go, color);
            if (!collider)
            {
                var col = go.GetComponent<Collider>();
                if (col != null) Object.Destroy(col);
            }
            return go;
        }

        public static void Tint(GameObject go, Color color)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null) r.material.color = color;
        }

        public static Color Hex(string html)
        {
            Color c;
            return ColorUtility.TryParseHtmlString(html, out c) ? c : Color.white;
        }
    }
}
