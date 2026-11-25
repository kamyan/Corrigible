using System;
using System.Linq;
using Corrigible.Core;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Tesis aşama 9: panonun ekipman sözcükleri (aşama 9 tasarımı §7.1–7.3): eşya kartının kullanım satırı, dinlenen köpeğin damgası,
    /// defterin ödül ve kapalı sayfa notları. Kaynak yalnız Core'un public okumaları (<c>FacilityGear</c>, <c>Facility.Recipes/Perks</c>,
    /// <c>BoardEquipment.Rest</c>) ve denge değerleri. Saf: durum değiştirmez, RNG çekmez, komut yazmaz. Tesis durumu yoksa (koddan kurulan simülasyonsuz
    /// çağrı; <see cref="On"/>) çağıranlar eski metne düşer.
    /// </summary>
    public static class FacilityBoardText
    {
        /// <summary>Tesis kipi: tesis durumu var (oyunda hep; tek baskın kipi).</summary>
        public static bool On(Simulation sim) => sim?.Facility != null;

        /// <summary>
        /// Ekipman kartının kullanım satırı: "baskında {0} hak · {etki}" (hak <c>FacilityGear.RaidCharges</c>, Yedek çanta dahil); köpek
        /// "dördüncü birim · havla, getir, kokla", kelepçe "1 hak · kullanılınca tükenir". Ekipman değilse null.
        /// </summary>
        public static string UseLine(Simulation sim, string kind)
        {
            if (!FacilityGear.IsGear(kind)) return null;
            var b = sim?.B ?? new Balance();
            string line;
            switch (kind)
            {
                case FacilityGear.Dog: return Loc.T("dördüncü birim · havla, getir, kokla");
                case FacilityGear.Clamp: line = Loc.T("1 hak · kullanılınca tükenir"); break;
                default: line = Loc.F("baskında {0} hak · {1}", FacilityGear.RaidCharges(b, kind, sim?.Facility?.Perks), Effect(b, kind)); break;
            }
            // Tesis aşama 10 (K45): cevap tarifi açıksa (ekip düzeyinde) eşyanın değeri değişir: kısa not
            string note = AnswerNote(sim, kind);
            return string.IsNullOrEmpty(note) ? line : Loc.F("{0} · {1}", line, note);
        }

        /// <summary>Tesis aşama 10: eşyanın açık cevabının kısa notu ("tel 4 can", "+1 tur", "çift cidar 1 hak"); önlem defteri kapalıyken
        /// ya da cevap açık değilken boş.</summary>
        public static string AnswerNote(Simulation sim, string kind)
        {
            if (!FacilityCounterText.On(sim)) return string.Empty;
            var b = sim.B;
            switch (FacilityGear.AnswerFor(kind, sim.Facility.Recipes))
            {
                case FacilityGear.AnsSteelWire: return Loc.F("tel {0} can", b.facilityWireHp + Math.Max(0, b.facilitySteelWireHp));
                case FacilityGear.AnsRelay: return Loc.F("+{0} tur", Math.Max(0, b.facilityRelayTurns));
                case FacilityGear.AnsSpring: return Loc.F("+{0} tur", Math.Max(0, b.facilitySpringTurns));
                case FacilityGear.AnsPiercer: return Loc.F("çift cidar {0} hak", Math.Max(1, b.facilityPiercerCharges));
                default: return string.Empty;
            }
        }

        static string Effect(Balance b, string kind)
        {
            switch (kind)
            {
                case FacilityGear.Wire: return Loc.T("tel ya da kalas");
                case FacilityGear.Charge: return Loc.T("duvar yıkar");
                default: return Loc.F("{0} tur çalışır", Math.Max(1, b.facilityDeviceTurns));
            }
        }

        /// <summary>Dinlenen köpeğin damgası: "DİNLENİYOR · {0} V" (kalan vardiya).</summary>
        public static string RestStamp(int rest) => Loc.F("DİNLENİYOR · {0} V", rest);

        /// <summary>Bırakma önizlemesinin notu: "Köpek dinleniyor: {0} vardiya baskına gitmez".</summary>
        public static string RestNote(int rest) => Loc.F("Köpek dinleniyor: {0} vardiya baskına gitmez", rest);


        /// <summary>Ödülle açılan sayfanın el notu: "{0} sunucusundan" (+ Tezgâh açıksa "Tezgâh: −1 vardiya").</summary>
        public static string RewardNote(Simulation sim, BoardRecipe r)
        {
            string source = FacilityRewardText.SourceName(sim, r.Id);
            string note = string.IsNullOrEmpty(source) ? string.Empty : Loc.F("{0} sunucusundan", source);
            if (sim?.Facility != null && sim.Facility.Perks.Contains(FacilityGear.Workbench))
            {
                string bench = Loc.F("Tezgâh: −{0} vardiya", Math.Max(0, sim.B.facilityWorkbenchShifts));
                note = note.Length == 0 ? bench : Loc.F("{0} · {1}", note, bench);
            }
            return note;
        }

        /// <summary>Kapalı sayfanın damgası: "KAPALI · {0} sunucusundan" (YZ adı; tabloda yoksa yalnız "KAPALI").</summary>
        public static string LockedStamp(Simulation sim, string kind)
        {
            string source = FacilityRewardText.SourceName(sim, kind);
            return string.IsNullOrEmpty(source) ? Loc.T("KAPALI") : Loc.F("KAPALI · {0} sunucusundan", source);
        }

        /// <summary>Kapalı temel tarifler, <c>FacilityGear.Kinds</c> sırasıyla (tesis kipi değilse boş).</summary>
        public static string[] LockedKinds(Simulation sim) =>
            On(sim) ? FacilityGear.Kinds.Where(k => !sim.Facility.Recipes.Contains(k)).ToArray() : Array.Empty<string>();
    }
}
