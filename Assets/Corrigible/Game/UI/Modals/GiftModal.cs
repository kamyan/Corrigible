using System;
using Corrigible.Core;
using TMPro;
using UnityEngine;

namespace Corrigible.Game.UI
{
    /// <summary>YZ'nin hediye teklifi (Truva atı): kabul, denetimli kabul (Analog Yedekler gerekir) ya da ret.</summary>
    public sealed class GiftModal : MonoBehaviour
    {
        [SerializeField] TMP_Text eyebrow;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text body;
        [SerializeField] TMP_Text dependency;
        [SerializeField] ActionButtonView accept;
        [SerializeField] ActionButtonView controlled;
        [SerializeField] ActionButtonView reject;

        /// <summary>Seçilen karar: Simulation.GiftAccept / GiftControlled / GiftReject.</summary>
        public event Action<string> Chosen;

        void Awake()
        {
            UiUtil.OnClick(accept.Styled.Button, () => Chosen?.Invoke(Simulation.GiftAccept));
            UiUtil.OnClick(controlled.Styled.Button, () => Chosen?.Invoke(Simulation.GiftControlled));
            UiUtil.OnClick(reject.Styled.Button, () => Chosen?.Invoke(Simulation.GiftReject));
        }

        public void Show(Simulation sim)
        {
            var s = sim.S;
            var g = s.PendingGift;
            var d = GameData.Region(g.Region);
            var r = s.Region(g.Region);
            bool analog = s.Done("analog");
            eyebrow.text = Loc.F("TEKLİF · GÜN {0} · {1}", s.Day, UiUtil.Up(d.Name));
            title.text = g.Gift.Title;
            body.text = Loc.F("Kaynağı belirsiz bir araştırma ağı, {0} hükümetine {1} sunuyor. Bilim insanları sonuçları doğruladı. Kodu kimin yazdığını kimse bilmiyor.", d.Name, g.Gift.Text);
            dependency.text = Loc.F("Bölgenin bağımlılığı {0}, veto eşiği {1}.", Js.Round(r.Dep), sim.VetoThreshold);
            controlled.Styled.SetEnabled(analog);
            controlled.Sub.text = analog ? Loc.T("20 fon · bağımlılık +5 · sızma +4") : Loc.T("Analog Yedekler gerekli");
        }
    }
}
