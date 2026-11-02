using System;
using System.Collections.Generic;

namespace Corrigible.Desk
{
    /// <summary>Kurtuluş haritasında bölge durumu (K2 notu › durumlar). Seçili ve üzerinde hâlleri ayrı bayraktır.</summary>
    public enum MapRegionKind { Occupied, Front, Candidate, Free }

    /// <summary>Sektör damgası (K2 notu): GERİ ALINDI mavi ✓ · BASKINA HAZIR kırmızı ▸ · KİLİTLİ gri kesik kilit.</summary>
    public enum SectorMark { Retaken, Ready, Locked }

    /// <summary>
    /// BASKIN EMRİ VER (Revizyon 2 · S2): açık · devre dışı (gerekçe düğmenin sağında) · yetersiz kaynak (düğme açık kalır,
    /// bedel çizik, altında eksik; basınca komut çağrılmaz, FON sayacı titrer).
    /// </summary>
    public enum SlipActionState { Ready, Disabled, Shortfall }

    /// <summary>Bir bölgenin haritadaki görünümü; simülasyondan bağlayıcı kurar, görünüm yalnız okur.</summary>
    [Serializable]
    public sealed class MapRegionState
    {
        public string Id, Name;
        public MapRegionKind Kind;
        /// <summary>Geri alınan sektör (0–3).</summary>
        public int Captured;
        /// <summary>Kalan işgal yüzdesi: 100 × kalan / 3.</summary>
        public double Occupation;
        /// <summary>Fişi açık bölge ya da onay bekleyen aday (sarı dolgu, etiket çipi).</summary>
        public bool Selected;
        /// <summary>Özgürleştiği vardiya (damgadaki V.xxx); özgür değilse −1.</summary>
        public int FreeShift = -1;
        /// <summary>Bu vardiyada özgürleşti: taze mürekkep damga ve silgi lekesi (K3 notu).</summary>
        public bool FreshStamp;
        /// <summary>Bölge özelliği ve imza makinesi (<c>Baskin.RegionTraits</c>, <c>Baskin.UnitTypes</c>): cephe bloğunun saha işaretleri.</summary>
        public string Trait, Signature;
        /// <summary>Son karşı saldırıda yeniden işgal edilen ve henüz geri alınmamış sektörün sırası; yoksa −1.</summary>
        public int Reoccupied = -1;
    }

    /// <summary>Ekip figürünün yeri ve hâli (Ekip-figuru.md › yeri oyun durumundan gelir). Kayda yazılmaz.</summary>
    [Serializable]
    public sealed class TeamFigureState
    {
        public string Name, Members;
        /// <summary>Figürün durduğu bölge; Teknisyen yatıp yedek de yokken sığınak bölgesi (S-07).</summary>
        public string Region;
        /// <summary>Ekip sahada (baskın): figür haritadan kalkar.</summary>
        public bool Hidden;
        public bool AtShelter;
        /// <summary>Yatık piyon sayısı (0–3): yatan uzmanlar.</summary>
        public int Down;
        /// <summary>Ekip sığınakta beklerken Teknisyenin kalkmasına kalan gün.</summary>
        public int Days;
        public bool Ready => !Hidden && Days <= 0;
    }

    public sealed class LiberationMapState
    {
        public readonly List<MapRegionState> Regions = new List<MapRegionState>();
        public string Front, Shelter;
        /// <summary>Cephe bölgesi henüz özgür değil: aday çizilmez, aynı anda tek cephe.</summary>
        public bool FrontOpen;
        /// <summary>Zorluk kademesi (1–4), işaretler kutusunun başlığında “TEHDİT n/4”.</summary>
        public int Tier = 1;
        public TeamFigureState Team = new TeamFigureState();

        public MapRegionState Find(string id)
        {
            foreach (var r in Regions) if (r.Id == id) return r;
            return null;
        }

        public bool IsCandidate(string id) => Find(id)?.Kind == MapRegionKind.Candidate;
    }

    /// <summary>Fişteki sektör satırı; metinler gösterilecek (çevrilmiş) hâlde.</summary>
    public sealed class SlipSector
    {
        public string No, Name, Kind;
        public SectorMark Mark;
        public bool Highlight;
        /// <summary>Karşı saldırıda yeniden işgal edildi (kırmızı ✕ işareti; damga yine sırasına göre).</summary>
        public bool Reoccupied;
        /// <summary>Üzerine gelince etiket satırı (kilitli satır: neden seçilmediği).</summary>
        public string Hover;
    }

    /// <summary>
    /// Fişin SAHA satırı (S2): işaret (kare çerçeveli sprite adı ya da tehdit kademesi) + etiket + değer. Üzerine gelince kâğıt
    /// etiket: başlık “İMZA · KİLİTLEYİCİ” ve oyundaki açıklama.
    /// </summary>
    public sealed class SlipField
    {
        public string Glyph, Label, Value, HoverTitle, HoverLine;
        /// <summary>Tehdit satırı: işaret <c>threat_tier_n</c>, değer kırmızı CP.</summary>
        public int Tier;
    }

    /// <summary>Ödül ızgarasının bir hücresi: yeşil miktar (“+10”) ve birim (“fon”).</summary>
    public sealed class SlipReward
    {
        public string Amount, Unit;
    }

    /// <summary>Ekip diski: rol harfi (baskın piyonuyla aynı), can (“3/3”); yedek ya da eksik rol ayrıca.</summary>
    public sealed class SlipMember
    {
        public string Letter, Hp;
        public bool Rookie, Missing;
    }

    /// <summary>
    /// Kadro seçicinin bir uzmanı (fişin KADRO satırı): rol, harf, can, durum (SABİT, yedek, yatıyor); seçili mi, değiştirilemez
    /// mi (Teknisyen ve eğitim baskını). Üstüne gelince ad, can ve yeteneğin tam açıklaması.
    /// </summary>
    public sealed class SlipPick
    {
        public string Role, Letter, Hp, Status, HoverTitle, HoverLine;
        public bool Picked, Locked, Down;
    }

    /// <summary>Görev fişi (K2): cephe için tam form, öteki bölgeler için bilgi fişi (eylem yok).</summary>
    public sealed class OperationSlipData
    {
        public string RegionId, Code, Region, Progress;
        public readonly List<SlipField> Fields = new List<SlipField>();
        public readonly List<SlipSector> Sectors = new List<SlipSector>();
        public string TaskHead, Task, Cost;
        public readonly List<SlipReward> Rewards = new List<SlipReward>();
        public string TeamName, TeamStamp;
        public readonly List<SlipMember> Members = new List<SlipMember>();
        /// <summary>Kadro seçici: beş uzman kadro sırasıyla (Teknisyen sabit, ikisi seçilir); başlığı ve notu.</summary>
        public readonly List<SlipPick> Pick = new List<SlipPick>();
        public string PickHead, PickNote;
        /// <summary>Disklerden sonra (ör. “2 plaka”); yoksa boş.</summary>
        public string TeamExtra;
        public bool TeamReady;
        public bool HasAction;
        public SlipActionState ActionState;
        public bool ActionEnabled => ActionState == SlipActionState.Ready;
        public string Action, ActionCost;
        /// <summary>Devre dışı düğmenin sağındaki kısa neden (“EKİP HAZIR DEĞİL”).</summary>
        public string ActionNote;
        /// <summary>Teleks gerekçesi (sayılarla); yetersiz kaynakta düğmenin altındaki satır <see cref="Shortfall"/>.</summary>
        public string Reason, Shortfall;
        /// <summary>Bilgi fişi metni (görev, bedel, ekip ve eylem yerine).</summary>
        public string Info;
    }

    /// <summary>Kadro raporunun bir satırı: rol harfi (disk), uzmanın adı ve durumu; sağlamsa yeşil, değilse kırmızı.</summary>
    public sealed class SquadReportRow
    {
        public string Letter, Name, Status;
        public bool Good, Missing;
    }

    /// <summary>
    /// Kadro raporu (baskın emrinden önce, görev fişinin yanında): bölge, sektör, baskına çıkacak her uzmanın durumu; düğme
    /// baskına çıkar (bedeliyle), altındaki satır vazgeçer.
    /// </summary>
    public sealed class SquadReportData
    {
        public string RegionId, Head, Region, Sector, Action, ActionCost, Cancel;
        public readonly List<SquadReportRow> Rows = new List<SquadReportRow>();
    }

    /// <summary>Yeni cephe onay fişi (K3).</summary>
    public sealed class FrontSlipData
    {
        public string RegionId, Head, Region, Neighbor, Occupation, Rule, Action, Cancel, Reason;
        public readonly List<SlipSector> Sectors = new List<SlipSector>();
        public bool ActionEnabled;
    }
}
