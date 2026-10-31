using System;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Maketin üst bandındaki DEPO (Revizyon 2 · S3, Karar 3 A): erzak sandıkları (dolu · yarım, açık kutulu · boş yer gölgesi),
    /// malzeme kovaları (son kova kısmi dolu), 3 yuvalı plaka rafı ve arka duvarda tebeşir tahtası (“ERZAK · 15 VARDİYA”).
    /// Sayıları bağlayıcı verir (<see cref="ShelterStoreData"/>); vardiya sonunda eklenen sandık ve kova 0,25 s'de belirir ya
    /// da kalkar, tebeşir yazısı silinip 0,3 s'de yeniden yazılır.
    /// </summary>
    public sealed class ShelterStore : MonoBehaviour
    {
        [Serializable] public sealed class Crate
        {
            public GameObject full, partial, ghost;
        }

        [Serializable] public sealed class Bin
        {
            public GameObject body;
            [Tooltip("İç dolgu kutusu: yüksekliği doluluğa göre ölçeklenir (tabanı sabit).")]
            public Transform fill;
        }

        [Serializable] public sealed class PlateSlot
        {
            public GameObject plate, ghost;
        }

        [SerializeField] Crate[] crates = Array.Empty<Crate>();
        [SerializeField] Bin[] bins = Array.Empty<Bin>();
        [SerializeField] PlateSlot[] plates = Array.Empty<PlateSlot>();
        [SerializeField] TMP_Text slate;
        [SerializeField, Min(0.01f)] float popSeconds = 0.25f, slateSeconds = 0.3f;

        float[] binHeight;
        float[] popAt;
        bool shown;
        string slateText;
        float slateAt = -1;

        public string SlateText => slateText;

        void Awake()
        {
            binHeight = new float[bins.Length];
            for (int i = 0; i < bins.Length; i++) binHeight[i] = bins[i].fill != null ? bins[i].fill.localScale.y : 0;
            popAt = new float[crates.Length * 2 + bins.Length + plates.Length];
            for (int i = 0; i < popAt.Length; i++) popAt[i] = -1;
        }

        /// <param name="animate">Değişen sandık, kova ve plaka belirir; tebeşir silinip yeniden yazılır (vardiya sonu).</param>
        public void Show(ShelterStoreData d, bool animate)
        {
            if (binHeight == null) Awake();
            animate &= shown && Application.isPlaying;
            int k = 0;
            for (int i = 0; i < crates.Length; i++, k += 2)
            {
                var c = crates[i];
                bool full = i < d.Crates, partial = !full && d.CratePartial && i == d.Crates;
                Toggle(c.full, full, animate, k);
                Toggle(c.partial, partial, animate, k + 1);
                if (c.ghost != null) c.ghost.SetActive(!full && !partial);
            }
            for (int i = 0; i < bins.Length; i++, k++)
            {
                var b = bins[i];
                bool on = i < d.Bins;
                Toggle(b.body, on, animate, k);
                if (!on || b.fill == null) continue;
                // Son kova kısmi: dolgu tabanından yükselir
                float fill = i == d.Bins - 1 ? (float)d.LastBinFill : 1;
                var s = b.fill.localScale;
                var p = b.fill.localPosition;
                float bottom = p.y - s.y * 0.5f;
                s.y = Mathf.Max(0.0005f, binHeight[i] * fill);
                b.fill.localScale = s;
                b.fill.localPosition = new Vector3(p.x, bottom + s.y * 0.5f, p.z);
            }
            for (int i = 0; i < plates.Length; i++, k++)
            {
                var p = plates[i];
                bool open = i < d.PlateCapacity, on = open && i < d.Plates;
                Toggle(p.plate, on, animate, k);
                if (p.ghost != null) p.ghost.SetActive(open && !on);
            }
            if (slate != null && slateText != d.Slate)
            {
                if (animate && slateText != null) slateAt = Time.unscaledTime;
                else slate.text = d.Slate;
                slateText = d.Slate;
            }
            shown = true;
            Update();
        }

        void Toggle(GameObject go, bool on, bool animate, int index)
        {
            if (go == null || go.activeSelf == on && popAt[index] < 0) return;
            if (!animate)
            {
                go.SetActive(on);
                go.transform.localScale = Vector3.one;
                popAt[index] = -1;
                return;
            }
            // Belirme: 0 → 1; kalkma: 1 → 0, sonra gizlenir (işaret: negatif başlangıç)
            if (go.activeSelf == on) return;
            if (on) go.SetActive(true);
            popAt[index] = on ? Time.unscaledTime : -(Time.unscaledTime + 1);
        }

        void Update()
        {
            if (popAt == null) return;
            float now = Time.unscaledTime;
            int k = 0;
            foreach (var c in crates) { Pop(c.full, k++); Pop(c.partial, k++); }
            foreach (var b in bins) Pop(b.body, k++);
            foreach (var p in plates) Pop(p.plate, k++);
            if (slateAt >= 0 && slate != null)
            {
                // Tebeşir: yarı sürede silinir, kalan yarıda yeni yazı belirir
                float p = Mathf.Clamp01((now - slateAt) / slateSeconds);
                if (p >= 0.5f && slate.text != slateText) slate.text = slateText;
                slate.alpha = p < 0.5f ? 1 - p * 2 : (p - 0.5f) * 2;
                if (p >= 1) { slateAt = -1; slate.alpha = 1; }
            }
        }

        void Pop(GameObject go, int index)
        {
            float at = popAt[index];
            if (go == null || at == -1 || !go.activeSelf) return;
            bool hiding = at < -1;
            float start = hiding ? -at - 1 : at;
            float p = Mathf.Clamp01((Time.unscaledTime - start) / popSeconds);
            float e = 1 - (1 - p) * (1 - p);
            go.transform.localScale = Vector3.one * (hiding ? 1 - e : e);
            if (p < 1) return;
            popAt[index] = -1;
            go.transform.localScale = Vector3.one;
            if (hiding) go.SetActive(false);
        }
    }
}
