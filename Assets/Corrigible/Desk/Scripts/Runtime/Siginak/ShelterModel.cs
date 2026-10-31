using System;
using System.Collections.Generic;

namespace Corrigible.Desk
{
    /// <summary>Kesit maketinde odanın hâli (K4 notu, K8 › oda durumları). Seçili ayrı bayraktır.</summary>
    /// <summary>Oda hâli: çalışıyor · durdu (atölye plaka dökemiyor, revir açlıkta) · hasarlı (onarılmayı bekliyor).</summary>
    public enum RoomCondition { Working, Stopped, Damaged }

    /// <summary>Düğme hâli (K8 › düğme durumları): normal · devre dışı (soluk, kesik kenar) · yetersiz kaynak (açık kalır, eksik çizik).</summary>
    public enum SlipButtonState { Normal, Disabled, Short }

    /// <summary>Kesit maketindeki bir odanın görünümü; bağlayıcı simülasyondan kurar.</summary>
    [Serializable]
    public sealed class ShelterRoomData
    {
        public string Id, Name;
        public int Level, MaxLevel, Workers, Capacity;
        public RoomCondition Condition;
        public bool Selected;
        /// <summary>Bu vardiyada onarıldı / geliştirildi: kartta “EMİR UYGULANDI” damgası bir vardiya kalır.</summary>
        public bool Fresh;
        /// <summary>Kart satırı: “SV 1 · 2/2” (en üst seviyede “SV 3/3 · 4/6”).</summary>
        public string Line;
        /// <summary>
        /// Odanın çıktısı görünür (Revizyon 2 · S3): yaşam alanında erzak kasası, telsiz odasında cihazın kadranı ve amber
        /// lamba (görevli var), atölyede döküm kalıbı (plaka dökülüyor).
        /// </summary>
        public bool Active;
        /// <summary>Yaşam alanı: ranzada yatan yedek uzman sayısı. Revir: yatakta sargılı yaralı uzman var (1).</summary>
        public int Occupants;
        public bool Top => Level >= MaxLevel && MaxLevel > 0;
        public bool Full => Capacity > 0 && Workers >= Capacity;
    }

    /// <summary>Düğme: etiket, hâl ve üzerine gelince neden (devre dışıysa).</summary>
    public sealed class ShelterButtonData
    {
        public string Label, Hover;
        public SlipButtonState State;
    }

    /// <summary>
    /// Kıskaçlı panonun içeriği (Revizyon 2 · S3): başta asıl soru “ERZAK YETER · n VARDİYA”, sonra TOPLULUK · UZMANLAR
    /// (diskler) · PLAKA; altında seçili odanın sayfası. Metinler gösterilecek hâlde.
    /// </summary>
    public sealed class ShelterBoardData
    {
        public string Title, Shift;
        /// <summary>“ERZAK YETER” + “15 VARDİYA”; erzak artıyorsa “ERZAK” + “ARTIYOR”.</summary>
        public string SuppliesLabel, SuppliesBig;
        /// <summary>3 vardiya ve altı (ya da açlık): büyük yazı kırmızı, yanında damga (AZ KALDI / AÇLIK).</summary>
        public bool SuppliesLow;
        public string SuppliesStamp;
        /// <summary>“22/30 · son vardiya −1,4” (zengin metin).</summary>
        public string SuppliesLine, Community, Plates;
        public readonly List<SlipMember> Squad = new List<SlipMember>();
        public string RoomId, RoomName, SelectedChip, RoomLine, Description, Note;
        /// <summary>Personel adımlayıcısı: değer “2/6”, altında engel (“Boşta kimse yok”).</summary>
        public string Staff, StaffNote;
        public ShelterButtonData Minus = new ShelterButtonData(), Plus = new ShelterButtonData();
        /// <summary>En üst seviyede düğme yerine yeşil EN ÜST SEVİYE damgası.</summary>
        public bool Top;
        /// <summary>GELİŞTİR / ONAR düğmesi: “GELİŞTİR · 20 fon + 14 malzeme”; eksik kalem çizik + “yetersiz”.</summary>
        public ShelterButtonData Action = new ShelterButtonData();
        /// <summary>Kilitli seviye ya da öteki gerekçe (düğmenin altında); yoksa null.</summary>
        public string ActionNote;
    }

    /// <summary>
    /// Maketin üst bandındaki DEPO (Revizyon 2 · S3, Karar 3 A): erzak sandıkları (10 yuva = erzak sınırı), malzeme kovaları
    /// (5 yuva = malzeme deposu, son kova kısmi), plaka rafı (3 yuva) ve tebeşir tahtası.
    /// </summary>
    public sealed class ShelterStoreData
    {
        public const int CrateSlots = 10, BinSlots = 5, PlateSlots = 3;
        /// <summary>Bir sandığın ve bir kovanın değeri (erzak sınırı ÷ 10, malzeme deposu ÷ 5).</summary>
        public double CrateValue, BinValue;
        /// <summary>Dolu sandık; yarım sandık (açık, iki kutulu) varsa <see cref="CratePartial"/>. Gölge yuvalar sınıra kadar.</summary>
        public int Crates;
        public bool CratePartial;
        /// <summary>Dolu kova ve son kovanın doluluğu (0–1; tamsa 1).</summary>
        public int Bins;
        public double LastBinFill = 1;
        /// <summary>Raftaki plaka ve rafın açık yuvası (atölye seviyesi kadar).</summary>
        public int Plates, PlateCapacity;
        /// <summary>Tebeşir: “ERZAK · 15 VARDİYA”.</summary>
        public string Slate;
    }
}
