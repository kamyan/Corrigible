using System;
using System.Collections.Generic;

namespace Corrigible.Core
{
    /// <summary>Sığınak: baskının hazırlık yeri (odalar, erzak, malzeme, zırh plakaları, yedek uzmanlar).</summary>
    [Serializable]
    public sealed class ShelterState
    {
        /// <summary>Kişi, görev ve eşya sahipliğiyle vardiya planı; eski oda modeli kapalıyken asıl kaynaktır.</summary>
        public PlanningBoardState Plan;
        public double Supplies, Materials;
        public int Population;
        public bool Starving;
        public double LastSuppliesChange, LastMaterialsChange;
        public List<ShelterRoomState> Rooms = new List<ShelterRoomState>();
        /// <summary>Atölyenin döktüğü zırh plakaları; baskında takılır, kırılan düşer.</summary>
        public int Plates;
        /// <summary>Atölyenin plaka ve revirin yara sarma birikimi (vardiya vardiya dolar).</summary>
        public double PlateProgress, HealProgress;
        /// <summary>Yaşam alanının yedek uzmanları: düşen ya da yatan uzmanın yerine çıkar.</summary>
        public List<SpecialistState> Reserves = new List<SpecialistState>();
        /// <summary>Art arda aç kapanış sayacı: aç kapanışta +1, tok kapanışta −1 (sıfırın altına inmez).</summary>
        public int HungryStreak;
        /// <summary>Açlıktan ayrılan ve bırakılan kişi sayısı.</summary>
        public int Deserted;
        /// <summary>Yarım tayın, kapanış günü bu değeri aşmayan kapanışlarda geçerlidir; yoksa −1.</summary>
        public int HalfRationUntil = -1;
        /// <summary>Karartma bu güne açılan kapanışta geçerlidir (Üretim ve Mutfak çalışmaz); yoksa −1.</summary>
        public int BlackoutShift = -1;
        /// <summary>İfşa sayacı: görmezden gelinen iz, tamamlanan sabotaj ve sorgu yazar. Tavanı yoktur (gösterge sunumda kırpar).</summary>
        public int Exposure;
        /// <summary>
        /// "Konum çözüldü": ifşa eşiğe ulaştığı kapanışta kurulur; ifşa uyarı süresince (<see cref="FallGrace"/>) eşikte kalırsa
        /// dusus gelir. Sorgu ifşayı eşiğin altına indirdiği anda kalkar.
        /// </summary>
        public bool FallPending;
        /// <summary>
        /// "Konum çözüldü" uyarısının düşüşten önce kalan ek vardiyası: ifşa eşiğe bir kararla (görmezden gelinen iz) vardığında
        /// <c>exposureDecisionWarnShifts − 1</c> yazılır, sabotajla vardığında 0. Uyarı sürerken her kapanış bir azaltır; 0'ken
        /// kapanış düşürür. İfşa eşiğin altına inince sıfırlanır. Yalnız public karar ve sayaçtan türer.
        /// </summary>
        public int FallGrace;
        /// <summary>
        /// Kapıda acil grupla açılmış vardiyada canlı sayısı acil eşiğinin altında kapanan üst üste vardiya (yalnız
        /// <see cref="DesertedSinceFull"/> iken artar).
        /// </summary>
        public int UnderstaffedStreak;
        /// <summary>
        /// Sığınak son kez canlı sayısı acil eşiğinde ya da üstünde kapandığından beri en az bir kişi ayrıldı (açlık ya da
        /// <c>birak</c>). Eşikte ya da üstünde kapanan vardiya sıfırlar. Terk yalnız böyle bir dönemde işler: yalnız ölümlerle
        /// eksilen kadro, harekâtın başındaki eski bir ayrılmayla terk sayılmaz.
        /// </summary>
        public bool DesertedSinceFull;
        /// <summary>Kapıda acil grupla açılan vardiyanın günü (kapanışta yazılır); yoksa −1. Public kapı durumundan türer.</summary>
        public int EmergencyDoorShift = -1;
        /// <summary>Harekât boyunca görülen en düşük güven (zaferin tonu).</summary>
        public int MinTrust;
        /// <summary>Son şalterin çeşidi ("cekilme" | "tahliye") ve nedenleri ("konum", "kadro", "aclik", "guven"); çekilmediyse boş.</summary>
        public string SwitchKind;
        public List<string> SwitchReasons = new List<string>();
        /// <summary>Şalterin çekildiği gün (<c>S.Day</c>, 0 tabanlı); oyuncunun gördüğü vardiya bunun bir fazlasıdır. Çekilmediyse −1.</summary>
        public int SwitchShift = -1;

        public ShelterRoomState Room(string id) => Rooms.Find(r => r.Id == id);
    }

    [Serializable]
    public sealed class ShelterRoomState
    {
        public string Id;
        /// <summary>Sıfır: onarılmayı bekleyen oda; 1–3: kullanılabilir oda.</summary>
        public int Level, Workers;
    }

    public struct ShelterUpgradeCost
    {
        public double Fon, Materials;
    }
}
