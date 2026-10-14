using System;
using Corrigible.Core;
using TMPro;
using UnityEngine;

namespace Corrigible.Game.UI
{
    /// <summary>Araştırma listesindeki bir satır (ResearchRow prefab'ı). Hangi araştırmayı gösterdiği Inspector'da seçilir.</summary>
    public sealed class ResearchRowView : MonoBehaviour
    {
        [Tooltip("GameData.Research içindeki kimlik (ör. anomali, yorum).")]
        [SerializeField] string researchId;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text desc;
        [SerializeField] StyledButton start;
        [SerializeField] TMP_Text state;
        [SerializeField] GameObject progress;
        [SerializeField] RectTransform progressFill;

        public string ResearchId => researchId;
        public event Action<string> StartClicked;

        ResearchDef def;
        OperationDef operation;

        void Awake()
        {
            UiUtil.OnClick(start.Button, () => StartClicked?.Invoke(researchId));
        }

        /// <summary>Satırı bir araştırmaya bağlar ve sabit yazıları doldurur (editörde de çağrılır).</summary>
        public void Setup(string id)
        {
            researchId = id;
            def = System.Array.Find(GameData.Research, r => r.Id == id);
            operation = GameData.HasOperation(id) ? GameData.OperationItem(id) : null;
            if (def == null && operation != null) { title.text = operation.Name; desc.text = operation.Desc; return; }
            if (def != null)
            {
                title.text = def.Name + (def.Branch == GameData.BranchAlign ? Loc.T("  · çare") : "");
                desc.text = Loc.F("{0} · {1} fon, {2} gün", def.Desc, def.Cost, def.Days);
                start.SetText(Loc.F("Başlat · {0:0.0} fon/gün", def.Cost / def.Days));
            }
        }

        public void Refresh(Simulation sim, UiTheme theme)
        {
            def ??= System.Array.Find(GameData.Research, r => r.Id == researchId);
            operation ??= GameData.HasOperation(researchId) ? GameData.OperationItem(researchId) : null;
            bool visible = sim.ModernEconomy ? operation != null || def?.Branch == GameData.BranchAlign : def != null;
            gameObject.SetActive(visible);
            if (!visible) return;
            if (sim.ModernEconomy && operation != null)
            {
                bool purchased = sim.OperationPurchased(researchId);
                string why = sim.OperationCheck(researchId);
                double cost = sim.OperationCost(researchId);
                title.text = operation.Name;
                desc.text = operation.Desc;
                start.SetText(Loc.F("Satın al · {0} fon", cost));
                UiUtil.Show(start, !purchased);
                start.SetEnabled(why == null);
                progress.SetActive(false);
                state.text = purchased ? Loc.T("SATIN ALINDI") : why ?? Loc.T("Peşin · hemen kullanılabilir");
                state.color = purchased ? theme.good : why != null ? theme.warn : theme.dim;
                return;
            }
            title.text = def.Name;
            double days = sim.ModernEconomy ? sim.ResearchDays(researchId) : def.Days;
            double price = sim.ModernEconomy ? sim.ResearchCost(researchId) : def.Cost;
            desc.text = sim.ModernEconomy ? Loc.F("{0} · {1} fon, {2} iş günü", def.Desc, price, days)
                : Loc.F("{0} · {1} fon, {2} gün", def.Desc, price, days);
            var s = sim.S;
            bool done = s.Done(def.Id), cur = s.Research.Cur == def.Id;
            bool locked = def.Branch == GameData.BranchAlign && !s.Aware;
            UiUtil.Show(start, !done && (!cur || sim.ModernEconomy) && !locked);
            if (sim.ModernEconomy)
            {
                start.SetText(cur ? sim.ResearchPaused ? (Loc.IsSource ? Loc.T("Sürdür") : Loc.T("[araştırma] Sürdür")) : Loc.T("Duraklat") : Loc.F("Başlat · {0:0.0} fon/gün", price / days * sim.AlignSpeed / sim.TimeScale));
                start.SetEnabled(cur ? sim.SetResearchPausedCheck(!sim.ResearchPaused) == null : sim.ResearchCheck(researchId) == null);
            }
            else start.SetEnabled(!(def.Req != null && !s.Done(def.Req)) && s.Research.Cur == null && s.Ending == null);
            progress.SetActive(cur);
            if (done) { state.text = Loc.T("TAMAM"); state.color = theme.good; }
            else if (cur)
            {
                double spd = def.Branch == GameData.BranchAlign ? sim.AlignSpeed : 1;
                bool starved = s.Research.Starved > 0;
                state.text = sim.ModernEconomy && sim.ResearchPaused ? Loc.T("Duraklatıldı") : s.Illusion != null ? Loc.T("veri bekleniyor") : starved ? Loc.T("fon yetmiyor") : spd > 0 ? Loc.F("~{0} gün", Math.Ceiling((days - s.Research.Prog) / spd * (sim.ModernEconomy ? sim.TimeScale : 1))) : Loc.T("gözlem yok");
                state.color = starved || spd == 0 || s.Illusion != null ? theme.warn : theme.verify;
                progressFill.anchorMax = new Vector2((float)(s.Research.Prog / days), 1);
            }
            else if (locked) { state.text = Loc.T("tespit bekleniyor"); state.color = theme.dim; }
            else
            {
                state.text = sim.ModernEconomy ? sim.ResearchCheck(researchId) ?? "" : "";
                state.color = theme.dim;
            }
        }
    }
}
