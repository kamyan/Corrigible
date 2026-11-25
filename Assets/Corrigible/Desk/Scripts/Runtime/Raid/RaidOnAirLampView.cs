using Corrigible.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// “YAYINDA” lambası (tesis aşama 5, Docs/TESIS-BASKINLARI-TASARIMI.md §7.3): oyuncu emri telsizle iletince (EMRİ İLET ya da GEÇ)
    /// yanar, gözcünün ilk rapor satırıyla söner. Durum <see cref="RaidViewModel.OnAir"/>'dan gelir; görünüm lambayı en az
    /// <c>radioDesk.lamp.holdS</c> yanık tutar (ilk efekt 0,25 s'de gelebilir, göz yine de görsün). Simülasyona ve kaynağa dokunmaz,
    /// kaynağın meşguliyetini uzatmaz. Telsiz masasının (HUD › Corners › Radio desk) sol yarısı; renkler raid_tokens.json › radioDesk.lamp.
    /// </summary>
    public sealed class RaidOnAirLampView : MonoBehaviour
    {
        [SerializeField] Graphic bezel, lens, glow;
        [SerializeField] TMP_Text label;

        bool wanted, painted;
        float litAt = -1;

        /// <summary>Lamba şu an yanık (en az yanık kalma süresi dahil).</summary>
        public bool Lit { get; private set; }
        /// <summary>Lambanın en az yanık kalma süresi (s).</summary>
        public static float HoldSeconds => RaidTheme.Active != null ? RaidTheme.Active.Number("radioDesk.lamp.holdS", .4f) : .4f;
        /// <summary>Lambanın yazısı (testler için).</summary>
        public TMP_Text Label => label;

        // Gerçek zaman: düzenleme modunda da ilerler (testler), oyun duraklasa da lamba söner
        static float Now => Time.realtimeSinceStartup;

        public void Render(RaidViewModel vm)
        {
            if (label != null) label.text = Loc.T("YAYINDA");
            bool on = vm.OnAir && !vm.Finished;
            if (on && !wanted) litAt = Now;
            // Sonuç kartında lamba hemen söner (köşeler zaten kararır)
            if (vm.Finished) litAt = -1;
            wanted = on;
            Apply(true);
        }

        void Update() => Apply(false);

        void OnDisable() { wanted = false; litAt = -1; Apply(true); }

        void Apply(bool force)
        {
            bool lit = wanted || litAt >= 0 && Now - litAt < HoldSeconds;
            if (!lit) litAt = -1;
            if (lit == Lit && painted && !force) return;
            Lit = lit; painted = true;
            if (bezel != null) bezel.color = RaidHudView.C("radioDesk.lamp.bezel");
            if (lens != null) lens.color = RaidHudView.C(lit ? "radioDesk.lamp.on" : "radioDesk.lamp.off");
            if (glow != null) { glow.color = RaidHudView.C("radioDesk.lamp.glow"); glow.enabled = lit; }
            if (label != null) label.color = RaidHudView.C(lit ? "radioDesk.lamp.labelOn" : "radioDesk.lamp.labelOff");
        }
    }
}
