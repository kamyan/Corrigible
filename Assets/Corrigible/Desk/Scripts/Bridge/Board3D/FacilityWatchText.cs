using System;
using System.Linq;
using Corrigible.Core;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// GÖZETİM cebinin (tesis aşama 5, dinleme nöbeti; belge §7.2) görünür sözcükleri: dinleme puanı ve nöbetin açacağı pafta. Kaynak
    /// yalnız Core'un public okumaları: <c>sim.Facility.Intel</c>, <c>Balance.facilityIntelCost</c>, <c>Balance.facilityWatchIntel</c>
    /// ve <c>sim.FacilityIntelTarget</c> (bulunulan bölgenin sıradaki kanadında planı bilinmeyen ilk oda; G, A, H sırası). Saf okuma:
    /// durum değiştirmez, RNG çekmez, komut yazmaz. Pafta numarası odanın kanattaki sırasıdır (görev fişinin yanındaki "PAFTA n/3").
    /// </summary>
    public static class FacilityWatchText
    {
        /// <summary>Harcanmamış dinleme puanı (simülasyonsuz ya da tesis durumu yokken 0).</summary>
        public static int Intel(Simulation sim) => BoardDropRules.WatchOpen(sim) ? Math.Max(0, sim.Facility?.Intel ?? 0) : 0;

        /// <summary>Bir planın bedeli (<c>facilityIntelCost</c>, en az 1; Core'un <c>SpendFacilityIntel</c> kuralı).</summary>
        public static int Cost(Simulation sim) => Math.Max(1, sim?.B?.facilityIntelCost ?? 1);

        /// <summary>
        /// Çalışan GÖZETİM görevlisinin bir kapanışta getirdiği puan (<c>facilityWatchIntel</c>; tesis aşama 9: Kulak kazanımı açıksa
        /// + <c>facilityEarIntel</c>, Core'un <c>AdvanceFacilityIntel</c>'i ile aynı).
        /// </summary>
        public static int Gain(Simulation sim) => Math.Max(0, sim?.B?.facilityWatchIntel ?? 0)
            + (sim?.Facility != null && sim.Facility.Perks.Contains(FacilityGear.Ear) ? Math.Max(0, sim.B.facilityEarIntel) : 0);

        /// <summary>Nöbetin açacağı oda; yoksa (bölge bitti, kanat biliniyor, bayrak kapalı) null.</summary>
        public static FacilityRoom Target(Simulation sim) => BoardDropRules.WatchOpen(sim) ? sim.FacilityIntelTarget : null;

        /// <summary>Nöbet her kapanışta en az bir paftayı açar (puan bedele hemen yetişir; 2026-11-24'ten beri varsayılan): sayaç anlamsız,
        /// şerit doğrudan açılacak paftayı söyler.</summary>
        public static bool EveryShift(Simulation sim) => Gain(sim) >= Cost(sim);

        /// <summary>Bir kapanışın açacağı pafta sayısı (Core'un <c>SpendFacilityIntel</c> döngüsü: puan bedele yettikçe, sıradaki kanadın
        /// bilinmeyen odaları kadar); Kulak'la iki olabilir.</summary>
        public static int SheetsPerClose(Simulation sim)
        {
            if (Target(sim) == null) return 0;
            int n = (Intel(sim) + Gain(sim)) / Cost(sim);
            var sector = sim.NextLiberationSector(sim.S.Liberation.CurrentRegion);
            int unknown = sector == null ? 0 : sim.FacilityPlan.Wing(sector.Id).Count(r => !sim.FacilityKnown(r.Id));
            return Math.Max(0, Math.Min(n, unknown));
        }

        /// <summary>Kart şeridi (≤ 66 mm): her vardiya pafta açılıyorsa "Dinleme · PAFTA 2"; değilse (eski kayıtta bedel 3) "Dinleme 1/3";
        /// açılacak plan yoksa "Dinleme · bitti" (puan yine bir plan bedeline kadar birikir; üzerine gelme satırı "açılacak plan yok" der).</summary>
        public static string Strip(Simulation sim)
        {
            var room = Target(sim);
            if (room == null) return Loc.T("Dinleme · bitti");
            return EveryShift(sim) ? Loc.F("Dinleme · PAFTA {0}", room.Index) : Loc.F("Dinleme {0}/{1}", Intel(sim), Cost(sim));
        }

        /// <summary>Açılacak pafta: "sıradaki: PAFTA 2"; yoksa "açılacak plan yok".</summary>
        public static string Next(Simulation sim)
        {
            var room = Target(sim);
            return room != null ? Loc.F("sıradaki: PAFTA {0}", room.Index) : Loc.T("açılacak plan yok");
        }

        /// <summary>Kartın üzerine gelme satırı: "her vardiya bir pafta · sıradaki: PAFTA 2" (eski bedelde "dinleme 1/3 · sıradaki: PAFTA 2").</summary>
        public static string Line(Simulation sim)
        {
            if (!EveryShift(sim) || Target(sim) == null) return Loc.F("dinleme {0}/{1} · {2}", Intel(sim), Cost(sim), Next(sim));
            int per = Gain(sim) / Cost(sim);
            return per > 1 ? Loc.F("her vardiya {0} pafta · {1}", per, Next(sim)) : Loc.F("her vardiya bir pafta · {0}", Next(sim));
        }

        /// <summary>
        /// Bırakma önizlemesinin sonuç satırı, kapanıştan sonraki hâl: "kapanışta dinleme 0/3 → 1/3"; puan plan bedeline varıyorsa
        /// "kapanışta PAFTA 2 çıkar". Açılacak oda yokken puan bedelde durur (Core'un tavanı).
        /// </summary>
        public static string CloseLine(Simulation sim)
        {
            int now = Intel(sim), cost = Cost(sim), gain = Gain(sim);
            var room = Target(sim);
            int sheets = SheetsPerClose(sim);
            if (room != null && sheets > 1) return Loc.F("kapanışta PAFTA {0}–{1} çıkar", room.Index, room.Index + sheets - 1);
            if (room != null && gain > 0 && now + gain >= cost) return Loc.F("kapanışta PAFTA {0} çıkar", room.Index);
            return Loc.F("kapanışta dinleme {0}/{1} → {2}/{1}", now, cost, Math.Min(now + gain, cost));
        }
    }
}
