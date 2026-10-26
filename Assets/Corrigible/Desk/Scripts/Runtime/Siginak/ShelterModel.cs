using System;
using System.Collections.Generic;

namespace Corrigible.Desk
{
    /// <summary>Kesit maketinde odanın hâli (K4 notu, K8 › oda durumları). Seçili ayrı bayraktır.</summary>
    public enum RoomCondition { Working, NoPower, Damaged }

    /// <summary>Düğme hâli (K8 › düğme durumları): normal · devre dışı (soluk, kesik kenar) · yetersiz kaynak (kırmızı kenar + damga).</summary>
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
        /// <summary>Kart satırı: “SEVİYE 1 · PERSONEL 2/2” (en üst seviyede “3/3”).</summary>
        public string Line;
        public bool Top => Level >= MaxLevel && MaxLevel > 0;
        public bool Full => Capacity > 0 && Workers >= Capacity;
    }

    /// <summary>Düğme: etiket, hâl ve (yetersizse) damga sözcüğü.</summary>
    public sealed class ShelterButtonData
    {
        public string Label, Stamp;
        public SlipButtonState State;
    }

    /// <summary>Kıskaçlı panonun içeriği (K4 › sığınak özeti + seçili odanın dizin kartı). Metinler gösterilecek hâlde.</summary>
    public sealed class ShelterBoardData
    {
        public string Title, Shift;
        /// <summary>Özet satırları: etiket ve değer (değer zengin metin olabilir).</summary>
        public readonly List<(string label, string value)> Summary = new List<(string, string)>();
        /// <summary>ERZAK satırındaki AÇLIK damgası (Soru 9); yoksa null.</summary>
        public string SummaryStamp;
        public string RoomId, RoomName, SelectedChip, RoomLine, Description, Note, Cost, Readiness;
        public bool ReadinessOk;
        public ShelterButtonData Action = new ShelterButtonData(), Minus = new ShelterButtonData(), Plus = new ShelterButtonData();
    }
}
