using System.Collections.Generic;

namespace Corrigible.Desk
{
    /// <summary>
    /// Kabul istasyonunun bir anlık görüntüsü (Revizyon 3 · Aşama 6, PLAN §1.2, §3 W0): <c>AdmissionSnapshotBuilder</c> (Bridge) Core'un
    /// salt okunur durumundan ve sunum durumundan (<c>AdmissionUi</c>) kurar; görünümler yalnız bunu çizer. Metinler çevrilmiş gelir
    /// (görünümler <c>Loc</c> çağırmaz). Robotluk hiçbir alana girmez.
    /// </summary>
    public sealed class AdmissionSnapshot
    {
        /// <summary>Kartlı oyun ve kabul var.</summary>
        public bool Active;
        /// <summary>Bütün araç ve damgalar kilitli (plan kilitli, baskın, oyun bitti); okuma serbest. Neden Core'un iletisi.</summary>
        public bool Locked;
        public string LockReason;
        /// <summary>Hedef tezgâh durumu: aday var ya da görülmemiş talimat varsa <see cref="CounterState.Down"/>.</summary>
        public CounterState Counter;
        /// <summary>Kapıdaki adayın kimliği; null = aday yok.</summary>
        public string CandidateId;
        /// <summary>Kimlik/sevk belgesi; null = aday yok.</summary>
        public DocumentData Document;
        /// <summary>Belge çekmeceden tezgâha alındı (sunum durumu).</summary>
        public bool DocumentOnCounter;
        /// <summary>Yuvadaki talimat şeridi.</summary>
        public OrdersData Orders;
        /// <summary>ESKİ cebi: arşivdeki sürümler, yeniden eskiye.</summary>
        public readonly List<OrdersData> Archive = new List<OrdersData>();
        /// <summary>Talimat değişim animasyonu henüz oynamadı.</summary>
        public bool OrdersUnseen;
        /// <summary><see cref="AdmissionTool"/> sırasıyla dört araç.</summary>
        public readonly ToolData[] Tools = new ToolData[4];
        /// <summary><see cref="StampChoice"/> sırasıyla dört damga.</summary>
        public readonly StampData[] Stamps = new StampData[4];
        /// <summary>Kıskaçtaki fişler, alttan üste.</summary>
        public readonly List<SlipData> Slips = new List<SlipData>();
        public RequestData Request;
        /// <summary>Soğuk gözlemde buğu görüldü (Core kodu "fog").</summary>
        public bool BreathFog;
        /// <summary>İşaretçinin hizalandığı madde dizini; −1 = en üst.</summary>
        public int MarkerRule = -1;
        /// <summary>Plaka ve üzerine gelme sözcükleri (çevrilmiş).</summary>
        public AdmissionWords Words;
        public string CounterHoverTitle, CounterHoverLine;
    }

    /// <summary>Kimlik/sevk belgesi (148 × 105 mm) ve kararın damga izi.</summary>
    public sealed class DocumentData
    {
        public string Name, Profession, Origin, Issued, Serial, Referral, Seal, PortraitKey, Stats;
        public bool PortraitFlip;
        /// <summary>Basılan damganın Core seçimi (karar sonrası dizide); null = iz yok.</summary>
        public string StampChoiceId;
        public string[] StampLines;
        public string StampColor;
    }

    /// <summary>Talimat şeridi (80 × 210 mm) ya da ESKİ cebindeki başı.</summary>
    public sealed class OrdersData
    {
        public int Version;
        public string Header, Number, Title, Subtitle, ArchiveTitle, ArchiveRule, Footer, Footer2;
        public readonly List<RuleData> Rules = new List<RuleData>();
    }

    /// <summary>Şeridin bir maddesi. <see cref="Tag"/>: "sürüm 2’den" ya da "YENİ · V.009".</summary>
    public sealed class RuleData
    {
        public string Number, Text, Tag;
        public bool IsNew;
    }

    /// <summary>Bir inceleme aracının durumu. <see cref="Reason"/>: Core iletisi ya da kısa etiket.</summary>
    public sealed class ToolData
    {
        public bool Used, Enabled;
        public string Title, Reason;
    }

    /// <summary>Bir karar damgasının durumu: raf plakası, sap yazısı, kilit nedeni (Core) ve kısa nedeni, band rengi.</summary>
    public sealed class StampData
    {
        public string Plate, Cap, Reason, ShortReason;
        public bool Enabled;
        public string BandColor;
    }

    /// <summary>
    /// Kıskaçtaki bir fiş (90 × 60 mm). <see cref="Key"/>: animasyon kimliği ("cand:resident-9:papers", "review:4",
    /// "record:resident-9:result"); aynı anahtarlı fiş ikinci kez ağızdan çıkmaz.
    /// </summary>
    public sealed class SlipData
    {
        /// <summary>Fişi veren araç; null = İNCELEME FİŞİ.</summary>
        public AdmissionTool? Tool;
        public bool Review;
        public string Header, Shift, Title, Icon, Body, Note, Verdict;
        public SlipVerdict Kind;
        public bool Dashed;
        public string Key;
    }

    /// <summary>DIŞ KAYIT talep yaprağı ve karbon kopyası; gözetmen adı her çizimde Core'dan.</summary>
    public sealed class RequestData
    {
        public bool Sent, Enabled;
        public string Reason, Warning;
        public string[] Labels, Values;
        public string Supervisor;
        public bool SupervisorMissing;
    }

    /// <summary>Plaka ve üzerine gelme sözcükleri. İlkleyici yok (AGENTS kural 9: Türkçe sabit kodda durmaz; kurucu çevirip doldurur).</summary>
    public sealed class AdmissionWords
    {
        public string Drawer, Out, Holder, Archive, Slip, Findings, Under, Glass, Intercom, Desk, Rations;
    }
}
