using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Game.Campaign;
using TMPro;
using UnityEngine;

namespace Corrigible.Game.UI
{
    /// <summary>Krizler arası: sonuçlar ve üç ajans yükseltmesinden biri. Kampanya bitince özet ve "Bitir".</summary>
    public sealed class CampaignModal : MonoBehaviour
    {
        [SerializeField] UiTheme theme;
        [SerializeField] TMP_Text eyebrow;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text body;
        [SerializeField] ActionButtonView[] offers = Array.Empty<ActionButtonView>();
        [SerializeField] StyledButton finish;

        public event Action<string> UpgradeChosen;
        public event Action FinishClicked;

        List<CampaignUpgrade> current = new List<CampaignUpgrade>();

        void Awake()
        {
            for (int i = 0; i < offers.Length; i++) { int k = i; UiUtil.OnClick(offers[i].Styled.Button, () => { if (k < current.Count) UpgradeChosen?.Invoke(current[k].Id); }); }
            UiUtil.OnClick(finish.Button, () => FinishClicked?.Invoke());
        }

        public void Show(CampaignState c, bool teamsOn)
        {
            bool done = c.results.Count >= CampaignState.Crises;
            eyebrow.text = Loc.F("KAMPANYA · {0}/{1} KRİZ", c.results.Count, CampaignState.Crises);
            var lines = c.results.Select((r, i) => Loc.F("{0}. {1} · {2} · {3}. gün", i + 1, GameData.AiType(r.type).Name, (r.won ? "<color=#7bd88f>" : "<color=#ff5b4d>") + GameData.Ending(r.ending).Title + "</color>", r.day)).ToList();
            if (done)
            {
                title.text = c.Wins >= 4 ? Loc.T("Ajans dünyayı korudu") : c.Wins >= 2 ? Loc.T("Kıl payı") : Loc.T("Kaybedilmiş bir on yıl");
                lines.Add("");
                lines.Add(Loc.F("Kazanılan kriz: <b>{0}/{1}</b> · yükseltme: {2}", c.Wins, CampaignState.Crises, c.upgrades.Count));
                body.text = string.Join("\n", lines);
                current.Clear();
                foreach (var o in offers) UiUtil.Show(o, false);
                finish.SetText(Loc.T("KAMPANYAYI BİTİR"));
                return;
            }
            var last = c.results.LastOrDefault();
            title.text = last != null && last.won ? Loc.T("Kriz atlatıldı") : Loc.T("Kriz kaybedildi");
            lines.Add("");
            lines.Add(last != null && !last.won
                ? Loc.T("YZ bu zaferden güçlenerek çıktı: sonraki krize bir yetenekle fazla başlayacak. Ajansın için bir yükseltme seç:")
                : Loc.T("Sonraki kriz daha zor bir rakiple. Ajansın için bir yükseltme seç:"));
            body.text = string.Join("\n", lines);
            current = c.Offers(teamsOn);
            for (int i = 0; i < offers.Length; i++)
            {
                bool on = i < current.Count;
                UiUtil.Show(offers[i], on);
                if (on) offers[i].Setup(Loc.T(current[i].Title), Loc.T(current[i].Desc));
            }
            finish.SetText(Loc.T("Kampanyadan çık"));
        }
    }
}
