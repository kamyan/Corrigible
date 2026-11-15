using System.Collections.Generic;

namespace Corrigible.Desk
{
    /// <summary>
    /// Kabul istasyonunun bir anlık görüntüsü (Revizyon 3 · Aşama 6, PLAN §1.2, §3 W0; KOS aşama 2 ve 7): <c>AdmissionSnapshotBuilder</c>
    /// (Bridge) Core'un salt okunur durumundan ve sunum durumundan (<c>AdmissionUi</c>) kurar; görünümler yalnız bunu çizer. Metinler
    /// çevrilmiş gelir (görünümler <c>Loc</c> çağırmaz). Robotluk hiçbir alana girmez. Eski tek adaylı kabulün alanları (belge, talimat
    /// şeridi, araçlar, bulgu fişleri, talep formu) aşama 7'de söküldü.
    /// </summary>
    public sealed class AdmissionSnapshot
    {
        /// <summary>Kartlı oyun ve kabul var.</summary>
        public bool Active;
        /// <summary>Bütün araç ve damgalar kilitli (plan kilitli, baskın, oyun bitti); okuma serbest. Neden Core'un iletisi.</summary>
        public bool Locked;
        public string LockReason;
        /// <summary>Hedef tezgâh durumu: kapıda aday grubu bekliyorsa <see cref="CounterState.Down"/>.</summary>
        public CounterState Counter;
        /// <summary><see cref="StampChoice"/> sırasıyla dört yer: yalnız KABUL ve GERİ ÇEVİR dolu; KISITLI ve YARDIM metinsiz, kapalı (aşama 7'de söküldü).</summary>
        public readonly StampData[] Stamps = new StampData[4];
        /// <summary>Plaka ve üzerine gelme sözcükleri (çevrilmiş).</summary>
        public AdmissionWords Words;
        public string CounterHoverTitle, CounterHoverLine;
        /// <summary>Kapıdaki aday grubu (KOS aşama 2); null = grup yok.</summary>
        public CandidateGroupData Group;
    }

    /// <summary>
    /// Aday grubu (2–3 kart), grup fişi ve dolu sığınak listesi (KOS aşama 2). Metinler çevrilmiş gelir; gizli kimlik hiçbir alana girmez.
    /// Komut bağımsız değişkenleri metin değildir: <c>candidate_choose</c> a = kart <see cref="CandidateCardData.Id"/>, b =
    /// <see cref="ReplaceId"/>; <c>candidate_pass</c> bağımsız değişkensiz.
    /// </summary>
    public sealed class CandidateGroupData
    {
        /// <summary>Animasyon ve vuruş kimliği: "group:{geliş vardiyası}:{ilk aday kimliği}".</summary>
        public string Key;
        public readonly List<CandidateCardData> Cards = new List<CandidateCardData>();
        public bool Emergency, LastShift, Full;
        /// <summary>Kalan vardiya (bekleme sonu − gün; 2 ya da 1).</summary>
        public int WaitLeft;
        /// <summary>Grup fişi, sol blok.</summary>
        public string SlipHeader, SlipShift, SlipTitle, WaitLine, EmergencyLine, HintLine;
        /// <summary>Grup fişi, sağ blok (yer durumu).</summary>
        public string RoomTitle, RoomHint, ReplaceLine;
        /// <summary>Dolu sığınakta yollanabilir kişiler (pano sırası); dolu değilse boş.</summary>
        public readonly List<ReplaceRowData> Replace = new List<ReplaceRowData>();
        /// <summary>Seçili yollanacak kişi (sunum durumu); null = seçilmedi.</summary>
        public string ReplaceId;
        /// <summary>Grup geri çevrilebilir mi; neden Core iletisi.</summary>
        public bool CanPass;
        public string PassReason;
        /// <summary>GERİ ÇEVİR izi (basıştan sonra; null = iz yok).</summary>
        public string[] PassStampLines;
        public string PassStampColor;
        public string SlipHoverTitle, SlipHoverLine, RowHoverLine;
    }

    /// <summary>Bir aday kartı (148 × 105 mm): ad, ustalık, kademe, şüphe satırı, özellikler ve KABUL izi.</summary>
    public sealed class CandidateCardData
    {
        public string Id, Name, PortraitKey, Arrived, Mastery, Stats, TierName, TierColor, Suspicion, HoverLine;
        public bool PortraitFlip;
        public int Tier;
        public readonly List<PerkRowData> Perks = new List<PerkRowData>();
        /// <summary>Core'un seçim denetimi (kart ve seçili yollanacak kişi); neden Core iletisi aynen.</summary>
        public bool CanChoose;
        public string ChooseReason;
        /// <summary>KABUL izi: Core seçim kimliği (null = iz yok), çevrilmiş satırlar, "#RRGGBB" renk.</summary>
        public string StampChoiceId, StampColor;
        public string[] StampLines;
    }

    /// <summary>Kartın bir özellik satırı. <see cref="Tag"/>: çevrilmiş "GETİRİ" / "BEDEL".</summary>
    public sealed class PerkRowData
    {
        public string Id, Tag, Name, Effect;
        public bool Cost;
    }

    /// <summary>
    /// Dolu sığınak listesinin bir satırı. <see cref="Job"/>: çevrilmiş yer; <see cref="Label"/>: fişte yazılan satır ("Ad · Yer");
    /// <see cref="Reason"/>: seçilemiyorsa Core iletisi.
    /// </summary>
    public sealed class ReplaceRowData
    {
        public string Id, Name, Job, Label, Reason;
        public bool Selected, Enabled, BoundGear;
    }

    /// <summary>Bir karar damgasının durumu: raf plakası, sap yazısı, kilit nedeni (Core) ve kısa nedeni, band rengi.</summary>
    public sealed class StampData
    {
        public string Plate, Cap, Reason, ShortReason;
        public bool Enabled;
        public string BandColor;
    }

    /// <summary>Plaka ve üzerine gelme sözcükleri. İlkleyici yok (AGENTS kural 9: Türkçe sabit kodda durmaz; kurucu çevirip doldurur).</summary>
    public sealed class AdmissionWords
    {
        public string Drawer, Under, Desk;
    }
}
