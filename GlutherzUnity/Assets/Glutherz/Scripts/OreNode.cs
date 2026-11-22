using UnityEngine;

namespace Glutherz
{
    /// <summary>Erzader: drei Treffer mit der Waffe bauen sie ab, nach einer Weile wächst sie nach.</summary>
    public class OreNode : MonoBehaviour, IDamageable
    {
        public int zone;
        GameObject model;
        Collider col;
        int hits;

        public string Title { get { return Db.MatName[zone] + "-Ader"; } }
        public int Defense { get { return 0; } }
        public float HpFraction { get { return 1f - hits / 3f; } }

        public static OreNode Create(int zone, Vector3 pos, Transform parent)
        {
            var go = new GameObject("Erzader");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var node = go.AddComponent<OreNode>();
            node.zone = zone;
            node.model = new GameObject("Model");
            node.model.transform.SetParent(go.transform, false);
            Prim.Make(PrimitiveType.Cube, node.model.transform, new Vector3(0, 0.5f, 0), new Vector3(1.6f, 1.1f, 1.4f), new Color(0.3f, 0.3f, 0.32f), false)
                .transform.localRotation = Quaternion.Euler(8, 30, 5);
            for (int i = 0; i < 4; i++)
            {
                var c = Prim.Make(PrimitiveType.Cube, node.model.transform,
                    new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0.6f, 1.2f), Random.Range(-0.5f, 0.5f)),
                    Vector3.one * Random.Range(0.35f, 0.6f), Db.MatColor[zone], false);
                c.transform.localRotation = Random.rotation;
            }
            var box = go.AddComponent<BoxCollider>();
            box.center = new Vector3(0, 0.7f, 0);
            box.size = new Vector3(1.8f, 1.5f, 1.6f);
            node.col = box;
            return node;
        }

        public void TakeHit(int damage, bool crit)
        {
            hits++;
            Sfx.Play(Sfx.Clang, 0.6f);
            if (hits < 3) return;
            int n = Random.Range(2, 4);
            Game.S.mats[zone] += n;
            GameUI.Msg("+" + n + " " + Db.MatName[zone]);
            Game.Touch();
            model.SetActive(false);
            col.enabled = false;
            Invoke("Regrow", 40f);
        }

        void Regrow()
        {
            hits = 0;
            model.SetActive(true);
            col.enabled = true;
        }
    }
}
