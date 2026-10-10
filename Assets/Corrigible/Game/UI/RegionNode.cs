using Corrigible.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Game.UI
{
    /// <summary>
    /// Haritadaki bir bölge düğümü (RegionNode prefab'ı). Hangi bölge olduğu Inspector'da seçilir; konum ve boyut
    /// haritaya oranlı anchor'larla verilir, böylece harita ölçeklenince düğüm de ölçeklenir.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class RegionNode : MonoBehaviour
    {
        [Tooltip("GameData.Regions içindeki kimlik (ör. na, weu, cn).")]
        [SerializeField] string regionId;
        [SerializeField] Button button;
        [SerializeField] ShapeGraphic fill;
        [SerializeField] ShapeGraphic ring;
        [SerializeField] ShapeGraphic hatch;
        [SerializeField] ShapeGraphic quarantineRing;
        [SerializeField] ShapeGraphic check;
        [SerializeField] ShapeGraphic alert;
        [SerializeField] GameObject core;
        [SerializeField] TMP_Text value;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] TMP_Text sub;

        public string RegionId => regionId;
        public Button Button => button;

        /// <summary>Düğümü bölgeye bağlar: harita üzerindeki yeri, boyutu ve etiket yerleşimi (editörde çağrılır).</summary>
        public void Setup(string id)
        {
            regionId = id;
            var d = GameData.Region(id);
            float r = MapSpace.Radius(d), cx = (float)d.X / MapSpace.W, cy = 1 - (float)d.Y / MapSpace.H;
            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(cx - r / MapSpace.W, cy - r / MapSpace.H);
            rt.anchorMax = new Vector2(cx + r / MapSpace.W, cy + r / MapSpace.H);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            float ext = 6 / (2 * r);
            var q = quarantineRing.rectTransform;
            q.anchorMin = new Vector2(-ext, -ext); q.anchorMax = new Vector2(1 + ext, 1 + ext);
            q.offsetMin = q.offsetMax = Vector2.zero;
            nameLabel.text = d.Name;
            switch (d.LabelPos)
            {
                case 't': Place(new Vector2(0.5f, 1), new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(0, 26), TextAlignmentOptions.Bottom); break;
                case 'l': Place(new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(-8, -14), TextAlignmentOptions.Right); break;
                case 'r': Place(new Vector2(1, 0.5f), new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(8, -14), TextAlignmentOptions.Left); break;
                default: Place(new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(0, -21), TextAlignmentOptions.Top); break;
            }
        }

        void Place(Vector2 anchor, Vector2 pivot, Vector2 namePos, Vector2 subPos, TextAlignmentOptions align)
        {
            foreach (var (t, pos) in new[] { (nameLabel, namePos), (sub, subPos) })
            {
                var rt = t.rectTransform;
                rt.anchorMin = rt.anchorMax = anchor; rt.pivot = pivot;
                rt.anchoredPosition = pos;
                t.alignment = align;
            }
        }

        public void Refresh(Simulation sim, RegionState r, bool selected, UiTheme theme)
        {
            var s = sim.S;
            bool ended = sim.EndRevealed;
            var sh = sim.Shown(r);
            bool unknown = sh.Src == "unknown", offNow = r.Off > 0 && !ended;

            var c = unknown ? theme.unknown : offNow ? theme.bg : theme.Infiltration(sh.V ?? 0);
            if (r.Left && !ended) c.a = 0.45f;
            fill.color = c;
            ring.color = selected ? theme.fg : theme.line2;
            ring.Thickness = selected ? 2.5f : 1.5f;
            UiUtil.Show(hatch, unknown);
            UiUtil.Show(quarantineRing, r.Q && !ended);
            UiUtil.Show(check, (sh.Src == "verified" || sh.Src == "field") && !offNow);
            UiUtil.Show(alert, sh.Flag && sh.Src == "report" && !offNow);
            core.SetActive(ended ? s.Core == r.Id : s.CoreKnown == r.Id);

            value.text = unknown ? "?" : offNow ? "—" : Js.Round(sh.V ?? 0).ToString();
            value.fontStyle = sh.Src == "report" ? FontStyles.Bold | FontStyles.Italic : FontStyles.Bold;
            value.color = sh.V.HasValue && sh.V > 55 && !offNow ? theme.inkOnThreat : theme.fg;

            sub.text = ended ? "" : r.Left ? Loc.T("İTTİFAK DIŞI") : offNow ? Loc.F("KAPALI · {0} G", r.Off) : r.Q ? Loc.T("KARANTİNA") : "";
            sub.color = r.Off > 0 ? theme.threat : r.Left ? theme.muted : theme.warn;
            nameLabel.alpha = r.Left && !ended ? 0.45f : 1f;
        }
    }
}
