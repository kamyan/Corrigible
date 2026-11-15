using System.Collections.Generic;

namespace Corrigible.Desk
{
    /// <summary>
    /// Olay tutanağının (KOS aşama 4, tasarım §7) anlık görüntüsü: açık sığınak olayının kâğıda yazılacak her şeyi, o anki dilde.
    /// Köprü (<c>ShelterEventSnapshotBuilder</c>) simülasyondan kurar; yüz (<see cref="ShelterEventPaperFace"/>) ve görünüm
    /// (<see cref="ShelterEventPaperView"/>) yalnız bunu okur, <c>Loc</c> çağırmaz. İçinde Core nesnesi yoktur.
    /// <para>Gizlilik: metinler yalnız Core'un public okuyucularından gelir (başlık, gövde, seçenekler); iz kâğıdı insan ve
    /// sızmacı için aynıdır.</para>
    /// </summary>
    public sealed class ShelterEventPaperData
    {
        /// <summary>En çok satır (Core'da en çok üç seçenek).</summary>
        public const int MaxRows = 3;

        /// <summary>"Aynı kâğıt" anahtarı: tür, açılış vardiyası, kişiler (seçim yalnız aynı kâğıtta geçerli).</summary>
        public string Key;
        /// <summary>Olayın Core kimliği (görünmez; testler ve ses için).</summary>
        public string Kind;
        /// <summary>Künyenin sağı: "V.012" (olayın açıldığı kapanıştan sonraki vardiya).</summary>
        public string Shift;
        public string Title, Person, Body;
        /// <summary>Seçenek satırları, Core sırasıyla (en çok <see cref="MaxRows"/>).</summary>
        public readonly List<ShelterEventRowData> Rows = new List<ShelterEventRowData>();
        /// <summary>KARAR kutusunun ikinci satırı: seçim yoksa ipucu, varsa "{seçenek} · damgala".</summary>
        public string Confirm;
        /// <summary>Bir satır seçili ve karar verilebilir (KARAR kutusu mürekkeple).</summary>
        public bool ConfirmReady;
        /// <summary>Alttaki not satırı: genel kilit ya da son ret (Core metni); yoksa null.</summary>
        public string Note;
        /// <summary>Karar izi satırları ("KARARA BAĞLANDI", seçenek adı); null = iz yok.</summary>
        public string[] StampLines;
        /// <summary>Tutanak tepsideyken EVRAK hedefinin üzerine gelme satırı (başlık sabit: "EVRAK · OLAY TUTANAĞI").</summary>
        public string HoverLine;

        public ShelterEventRowData Selected
        {
            get
            {
                foreach (var r in Rows) if (r.Selected) return r;
                return null;
            }
        }
    }

    /// <summary>
    /// Tutanağın bir seçenek satırı. <see cref="Number"/> "1".."3"; <see cref="SafeTag"/> güvenli seçenekte "GÜVENLİ" (yoksa null);
    /// <see cref="Cost"/>, <see cref="Risk"/> ("Risk: …"), <see cref="KitLock"/> ve <see cref="Closed"/> ("KAPALI · gerekçe") sağ sütunda
    /// alt alta; <see cref="ClosedStamp"/> kapalı satırın sol sütunundaki küçük iz. <see cref="Enabled"/>: seçilebilir (seçenek açık
    /// ve genel kilit yok).
    /// </summary>
    public sealed class ShelterEventRowData
    {
        public string Id, Number, Name, SafeTag, Cost, Risk, KitLock, Closed, ClosedStamp;
        public bool Enabled, Safe, Selected;
        /// <summary>KOS aşama 6: "güvenli" seçenek ifşayı eşiğe vardırır; etiket "EŞİK" ve uyarı mürekkebinde.</summary>
        public bool SafeWarn;
    }

    /// <summary>Olay tutanağının ses olayları (görünüm yayar, köprü <c>Sfx</c>'e çevirir).</summary>
    public enum ShelterEventSound { Drop, ToHand, Mark, Stamp, Deny, ToTray }
}
