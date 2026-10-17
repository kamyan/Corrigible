using Corrigible.Core;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>S-Masa8'in sabit basılı lejantı; örnekler oyuncunun bölge verisini okumaz.</summary>
    public sealed class SadeLegendCardView : SadeTooltipCardBase
    {
        [SerializeField] TMP_Text subtitle;
        [SerializeField] TMP_Text[] captions, trustLabels, regionLabels, markLabels;

        static readonly string[] Captions = {
            Loc.N("SIZMA · YZ’NİN PAYI %"), Loc.N("GÜVEN"), Loc.N("BÖLGE"), Loc.N("İŞARETLER")
        };
        static readonly string[] TrustLabels = {
            Loc.N("onaylandı"), Loc.N("rapor"), Loc.N("şüpheli"), Loc.N("bilinmiyor")
        };
        static readonly string[] RegionLabels = {
            Loc.N("bilinmiyor"), Loc.N("karantina"), Loc.N("YZ çekirdeği")
        };
        static readonly string[] MarkLabels = {
            Loc.N("yayılım"), Loc.N("ekip ve rota"), Loc.N("danışman notu")
        };

        protected override void Render(JObject data)
        {
            Label(title, Loc.N("LEJANT"));
            SadeUi.Type(title, "legendTitle");
            Label(subtitle, Loc.N("harita"));
            BindLabels(captions, Captions, "sectionCap", 390, 11);
            BindLabels(trustLabels, TrustLabels, "legendItem", 88, 12);
            BindLabels(regionLabels, RegionLabels, "legendItem", 90, 12);
            BindLabels(markLabels, MarkLabels, "legendItem", 95, 12);
            // Referans ölçüsü: 420×327. Satırlar 11 px altına inmeden İngilizceye de yer bırakır.
            Card.sizeDelta = new Vector2(420, 327);
        }

        static void BindLabels(TMP_Text[] views, string[] sources, string role, float width, float size)
        {
            if (views == null) return;
            for (int i = 0; i < views.Length && i < sources.Length; i++)
            {
                Label(views[i], sources[i]);
                SadeUi.Type(views[i], role);
                FitText(views[i], width, size);
            }
        }
    }
}
