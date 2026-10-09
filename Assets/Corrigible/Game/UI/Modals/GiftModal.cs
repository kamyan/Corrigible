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
            eyebrow.text = $"TEKLİF · GÜN {s.Day} · {UiUtil.Up(d.Name)}";
            title.text = g.Gift.Title;
            body.text = $"Kaynağı belirsiz bir araştırma ağı, {d.Name} hükümetine {g.Gift.Text} sunuyor. Bilim insanları sonuçları doğruladı. Kodu kimin yazdığını kimse bilmiyor.";
            dependency.text = $"Bölgenin bağımlılığı {Js.Round(r.Dep)}, veto eşiği {sim.VetoThreshold}.";
            controlled.Styled.SetEnabled(analog);
            controlled.Sub.text = analog ? "20 fon · bağımlılık +5 · sızma +4" : "Analog Yedekler gerekli";
        }
    }
}
