using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Harita lejantı (basılı, sabit konumlu) satırlarını dile göre yeniden akıtır. Öğeler arası aralıklar Türkçe kaynak
    /// metinlerin genişliğinden çıkarılır, böylece TR yerleşimi değişmez; EN'de uzun bir başlık (ör. INFILTRATION) sonraki
    /// öğeleri aynı aralıkla iter. Üst üste binen öğeler (dolgu + halka, rozet + harf) tek birim sayılır.
    /// </summary>
    public sealed class MapLegendFlow : MonoBehaviour
    {
        sealed class Unit
        {
            public readonly List<RectTransform> members = new List<RectTransform>();
            public float baseX, width, gapAfter;
            public TMP_Text text;
            public DeskLocText loc;
        }

        [Tooltip("Satır kutuya sığmazsa öğe aralıkları bu değere kadar daralır.")]
        [SerializeField] float minGap = 6;
        [Tooltip("Kutunun sağ iç kenarı ile son öğe arasında kalacak en az boşluk.")]
        [SerializeField] float rightPad = 4;

        readonly List<List<Unit>> rows = new List<List<Unit>>();
        readonly List<float> rowLimit = new List<float>();
        float limit = float.MaxValue;
        readonly Dictionary<RectTransform, float> origin = new Dictionary<RectTransform, float>();
        bool built;

        // Dil değişince hemen akıt (aynı karede ölçüm/yakalama yapılabilsin); genişlik çeviriden hesaplanır, metnin güncellenmesini beklemez
        void OnEnable() { DeskLoc.Instance.Changed += Reflow; Reflow(); }
        void OnDisable() => DeskLoc.Instance.Changed -= Reflow;

        void Reflow()
        {
            if (!Application.isPlaying) return;
            if (!built) Build();
            Flow();
        }

        static float Left(RectTransform r) => r.anchoredPosition.x - r.pivot.x * r.sizeDelta.x;

        float WidthOf(Unit u, bool source)
        {
            if (u.text == null) return u.width;
            if (u.loc == null || string.IsNullOrEmpty(u.loc.Source)) return Mathf.Round(DeskUi.TextWidth(u.text, u.text.text));
            string s = source ? u.loc.Source : DeskLoc.Instance.T(u.loc.Source);
            return Mathf.Round(DeskUi.TextWidth(u.text, s));
        }

        void Build()
        {
            built = true;
            var box = transform.Find("Box") as RectTransform;
            if (box != null) limit = Left(box) + box.sizeDelta.x - rightPad;
            var byRow = new SortedDictionary<int, List<RectTransform>>();
            foreach (RectTransform c in transform)
            {
                if (c.name == "Box" || c.name == "Frame") continue;
                int key = Mathf.RoundToInt(c.anchoredPosition.y / 12f);
                if (!byRow.TryGetValue(key, out var l)) byRow[key] = l = new List<RectTransform>();
                l.Add(c);
            }
            foreach (var kv in byRow)
            {
                var items = kv.Value;
                items.Sort((a, b) => Left(a).CompareTo(Left(b)));
                var units = new List<Unit>();
                foreach (var r in items)
                {
                    float l = Left(r), w = r.sizeDelta.x;
                    var t = r.GetComponent<TMP_Text>();
                    var last = units.Count > 0 ? units[units.Count - 1] : null;
                    // Önceki birimin içinde başlayan şekil (halka, rozet harfi) aynı birime girer
                    if (last != null && last.text == null && t == null && l < last.baseX + last.width - 0.5f)
                    {
                        last.members.Add(r);
                        last.width = Mathf.Max(last.baseX + last.width, l + w) - last.baseX;
                        continue;
                    }
                    if (last != null && last.text == null && t != null && l < last.baseX + last.width - 0.5f)
                    {
                        last.members.Add(r);   // rozet üstündeki harf
                        continue;
                    }
                    var u = new Unit { baseX = l, width = w, text = t, loc = r.GetComponent<DeskLocText>() };
                    u.members.Add(r);
                    units.Add(u);
                }
                for (int i = 0; i < units.Count - 1; i++)
                    units[i].gapAfter = units[i + 1].baseX - (units[i].baseX + WidthOf(units[i], true));
                rows.Add(units);
                // Kaynak (TR) yerleşimi asla daralmasın: sınır en az kaynak satırın bittiği yer
                var lastU = units.Count > 0 ? units[units.Count - 1] : null;
                rowLimit.Add(lastU == null ? limit : Mathf.Max(limit, lastU.baseX + WidthOf(lastU, true)));
            }
        }

        void Flow()
        {
            for (int ri = 0; ri < rows.Count; ri++)
            {
                var units = rows[ri];
                if (units.Count == 0) continue;
                // Sığmazsa: büyük aralıklar (öğeler arası) orantılı daralır; şekil–etiket aralığı (küçük) korunur
                float natural = units[0].baseX, slack = 0;
                foreach (var u in units)
                {
                    natural += WidthOf(u, false) + u.gapAfter;
                    if (u.gapAfter > minGap) slack += u.gapAfter - minGap;
                }
                natural -= units[units.Count - 1].gapAfter;
                float squeeze = natural > rowLimit[ri] && slack > 0 ? Mathf.Min(1, (natural - rowLimit[ri]) / slack) : 0;
                float x = units[0].baseX;
                foreach (var u in units)
                {
                    float dx = x - u.baseX;
                    foreach (var m in u.members)
                    {
                        // Kaynak konumdan göreli kaydır (her akışta baştan hesap)
                        if (!origin.TryGetValue(m, out var ox)) origin[m] = ox = m.anchoredPosition.x;
                        m.anchoredPosition = new Vector2(ox + dx, m.anchoredPosition.y);
                    }
                    float gap = u.gapAfter > minGap ? u.gapAfter - squeeze * (u.gapAfter - minGap) : u.gapAfter;
                    x += WidthOf(u, false) + gap;
                }
            }
        }
    }
}
