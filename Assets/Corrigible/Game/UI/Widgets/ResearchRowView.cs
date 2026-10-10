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

        void Awake()
        {
            UiUtil.OnClick(start.Button, () => StartClicked?.Invoke(researchId));
        }

        /// <summary>Satırı bir araştırmaya bağlar ve sabit yazıları doldurur (editörde de çağrılır).</summary>
        public void Setup(string id)
        {
            researchId = id;
            def = System.Array.Find(GameData.Research, r => r.Id == id);
            title.text = def.Name + (def.Branch == GameData.BranchAlign ? Loc.T("  · çare") : "");
            desc.text = Loc.F("{0} · {1} fon, {2} gün", def.Desc, def.Cost, def.Days);
            start.SetText(Loc.F("Başlat · {0:0.0} fon/gün", def.Cost / def.Days));
        }

        public void Refresh(Simulation sim, UiTheme theme)
        {
            def ??= System.Array.Find(GameData.Research, r => r.Id == researchId);
            var s = sim.S;
            bool done = s.Done(def.Id), cur = s.Research.Cur == def.Id;
            bool locked = def.Branch == GameData.BranchAlign && !s.Aware;
            UiUtil.Show(start, !done && !cur && !locked);
            start.SetEnabled(!(def.Req != null && !s.Done(def.Req)) && s.Research.Cur == null && s.Ending == null);
            progress.SetActive(cur);
            if (done) { state.text = Loc.T("TAMAM"); state.color = theme.good; }
            else if (cur)
            {
                double spd = def.Branch == GameData.BranchAlign ? sim.AlignSpeed : 1;
                bool starved = s.Research.Starved > 0;
                state.text = s.Illusion != null ? Loc.T("veri bekleniyor") : starved ? Loc.T("fon yetmiyor") : spd > 0 ? Loc.F("~{0} gün", Math.Ceiling((def.Days - s.Research.Prog) / spd)) : Loc.T("gözlem yok");
                state.color = starved || spd == 0 || s.Illusion != null ? theme.warn : theme.verify;
                progressFill.anchorMax = new Vector2((float)(s.Research.Prog / def.Days), 1);
            }
            else if (locked) { state.text = Loc.T("tespit bekleniyor"); state.color = theme.dim; }
            else state.text = "";
        }
    }
}
