using System;
using System.Collections.Generic;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Kapı açılınca merdiven boşluğunda duran ziyaretçi (Revizyon 2 · S4d): altı tür (anne, yaşlı, silahlı, asker, yaralı,
    /// kopya) ve genel biçim; ayrım yalnız biçimle, renk ve ışık aynı. Biçimler sahnede hazır durur (kapalı); kalabalık kartta
    /// sıradaki yuvalara birden çok siluet konur, çocuklar genel biçimin küçültülmüşü. Her ziyarette boy ±%4 ve aksesuarın
    /// tarafı değişir (aynalanır). Kapı kapalıyken görünmez; ne zaman görüneceğini <see cref="DoorProp"/> söyler.
    /// </summary>
    public sealed class VisitorSilhouette : MonoBehaviour
    {
        /// <summary>Tür adları (kart → tür tablosu kapının kurucusunda).</summary>
        public const string Anne = "anne", Yasli = "yasli", Silahli = "silahli", Asker = "asker", Yarali = "yarali", Kopya = "kopya",
            Generic = "generic", Child = "cocuk";

        [Serializable]
        public sealed class Figure
        {
            public string kind;
            public Transform root;
        }

        [Tooltip("Hazır biçimler (genel biçimden kalabalık için birden çok).")]
        [SerializeField] Figure[] figures = Array.Empty<Figure>();
        [Tooltip("Durulan yerler: ilki tek ziyaretçinin yeri (kapının ortası), ötekiler kalabalıkta sırayla.")]
        [SerializeField] Transform[] slots = Array.Empty<Transform>();
        [SerializeField, Range(0.3f, 1)] float childScale = 0.7f;

        readonly HashSet<Figure> used = new HashSet<Figure>();

        public bool Visible
        {
            get
            {
                foreach (var f in figures) if (f.root != null && f.root.gameObject.activeSelf) return true;
                return false;
            }
        }

        /// <summary>Türleri yuvalara dizer; <paramref name="height"/> boy çarpanı (0,96–1,04), <paramref name="mirror"/> aksesuar öbür tarafta.</summary>
        public void Show(IReadOnlyList<string> kinds, float height, bool mirror)
        {
            Hide();
            used.Clear();
            for (int i = 0; i < kinds.Count && i < slots.Length; i++)
            {
                bool child = kinds[i] == Child;
                var f = Take(child ? Generic : kinds[i]) ?? Take(Generic);
                if (f == null) continue;
                // Kalabalıkta komşular birbirinin aynası ve biraz farklı boyda (aynı biçim yan yana tekrar etmesin)
                float s = height * (child ? childScale : 1) * (1 - 0.03f * (i % 2));
                bool m = mirror ^ (i % 2 == 1);
                f.root.localPosition = slots[i].localPosition;
                f.root.localRotation = slots[i].localRotation;
                f.root.localScale = new Vector3(m ? -s : s, s, s);
                f.root.gameObject.SetActive(true);
            }
        }

        public void Hide()
        {
            foreach (var f in figures) if (f.root != null && f.root.gameObject.activeSelf) f.root.gameObject.SetActive(false);
        }

        Figure Take(string kind)
        {
            foreach (var f in figures)
            {
                if (f.root == null || f.kind != kind || used.Contains(f)) continue;
                used.Add(f);
                return f;
            }
            return null;
        }
    }
}
