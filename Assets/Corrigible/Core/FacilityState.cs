using System;
using System.Collections.Generic;

namespace Corrigible.Core
{
    /// <summary>
    /// Tesis baskınlarının harekât boyunca süren durumu (Docs/TESIS-BASKINLARI-TASARIMI.md §11.2; aşama 5): bina planı bilinen
    /// odalar ve dinleme nöbetinin biriken puanı. Kayda yazılmaz; komutlardan yeniden kurulur (raid_room: teyp, raidres: evrak,
    /// board_file → Step: dinleme nöbeti; server_reward: açılan tarif ve kazanım; raidres: önlem defterinin sayımı ve öğrenmesi, aşama
    /// 10; aşama 11: izler <c>raidres</c>'ten, onarım ve kuşatmanın kendiliğinden bırakılması Step'ten, kuşatma ChangeAlarm ve
    /// <c>siege_defend</c>/<c>siege_yield</c> komutlarından). Alt bayrakları kapalı bölümler boş kalır.
    /// </summary>
    [Serializable]
    public sealed class FacilityState
    {
        /// <summary>Bina planı bilinen odaların kimlikleri ("weu_2.1"), öğrenilme sırasıyla, tekrarsız. Liste (HashSet değil):
        /// JsonUtility yazsın, yeniden oynatma karşılaştırması sırayı da görsün.</summary>
        public List<string> Known = new List<string>();
        /// <summary>Dinleme nöbetinin harcanmamış puanı (açılacak oda yokken en çok <see cref="Balance.facilityIntelCost"/>).</summary>
        public int Intel;
        /// <summary>Sunucu ödülüyle açılan tarifler (aşama 8), ödül sırasıyla: temel ekipman kimliği (<see cref="FacilityGear.Kinds"/>)
        /// ya da cevap tarifinin kimliği (<see cref="FacilityGear.Answers"/>; etkisi aşama 10).</summary>
        public List<string> Recipes = new List<string>();
        /// <summary>Sunucu ödülüyle seçilen kazanımlar (aşama 8; <see cref="FacilityGear.Perks"/>), ödül sırasıyla. Ekip çapındadır; baskın
        /// emrinde <see cref="RaidParams.Perks"/>'e kopyalanır.</summary>
        public List<string> Perks = new List<string>();
        /// <summary>Ödül seçimi bekleyen bölge (tamamlanan bölgenin kimliği); yoksa boş. JsonUtility null dizgiyi "" yazar: denetim
        /// <c>string.IsNullOrEmpty</c> ile. Doluyken baskın emri verilmez.</summary>
        public string PendingReward = "";
        /// <summary>Önlem defteri (aşama 10): sınıf başına birikmiş sayaç, <see cref="FacilityCounters.Classes"/> sırasıyla. Boş başlar,
        /// önlem defteri açıkken ilk yazışta altıya dolar (alt bayrak kapalıyken JSON değişmez). Baskın sonunda raporlardan tavanlı eklenir,
        /// sınıf önlem öğrenilince sıfırlanır; 2. kademedeki sınıf sayılmaz.</summary>
        public List<int> Tally = new List<int>();
        /// <summary>Önlem defteri (aşama 10): sınıf başına önlem kademesi 0..2 (<see cref="FacilityCounters.Classes"/> sırası); boş başlar,
        /// <see cref="Tally"/> ile birlikte dolar. Baskın emrinde <see cref="RaidParams.Counters"/>'a kopyalanır.</summary>
        public List<int> Counter = new List<int>();
        /// <summary>Önlem defterinin günlüğü (aşama 10), öğrenme sırasıyla: eşitlik kuralı (<see cref="FacilityCounters.Learn"/>, "en uzun
        /// süredir önlem alınmamış sınıf"), sunumun ÖNLEM teleksi ve telsiz günlüğü bunu okur.</summary>
        public List<FacilityCounterEntry> Learned = new List<FacilityCounterEntry>();
        /// <summary>Kalıcı tahta (aşama 11, belge §9.1): kanatların izleri. Sıra eklenme sırasıdır ve onarım sırasıdır (en eski önce).
        /// <c>raidres</c>'te oynanan odaların raporlarından türetilip birleştirilir (<see cref="FacilityMarks.Derive"/>,
        /// <see cref="FacilityMarks.Merge"/>), baskın emrinde <see cref="RaidParams.Marks"/>'a kopyalanır. Boş başlar; alt bayrak
        /// (<see cref="Balance.facilityMarks"/>) kapalıyken boş kalır.</summary>
        public List<FacilityMark> Marks = new List<FacilityMark>();
        /// <summary>Kuşatma günlüğü (aşama 11, belge §9.2), kurulma sırasıyla: bekleyen ya da süren kuşatma son girdidir (Outcome "" ya da
        /// "savun"). Kuşatmanın tek kaynağı: savunma tohumunun tuzu (girdinin dizini), "bırak" kilidi, sunumun teleks/kutu/telsiz satırları
        /// ve botun sayaçları bunu okur.</summary>
        public List<FacilitySiegeEntry> Sieges = new List<FacilitySiegeEntry>();
        /// <summary>Onarım günlüğü (aşama 11): YZ'nin sildiği izin kopyası, <see cref="FacilityMark.Day"/> onarıldığı gün.</summary>
        public List<FacilityMark> Repairs = new List<FacilityMark>();
    }

    /// <summary>
    /// Kalıcı tahtanın bir izi (aşama 11, belge §9.1): oda kimliği, tür, kare, kalan can ve ilk yazıldığı gün. Türler (kısa ASCII kimlik):
    /// "duvar" (yıkılan duvar zemin olur), "kepenk" (açık kalır), "donanim" (kırık; hedef odası hazırlığı sürer), "cool" ve "spawner"
    /// (kırılan soğutma ünitesi ve kopya dolabı yoktur), "tel", "kalas", "hurda" (engel, kalan canıyla; Hp/Max yalnız bunlarda anlamlı).
    /// Tahtaya ve bina planına <see cref="FacilityMarks.Apply"/> ile uygulanır.
    /// </summary>
    [Serializable]
    public sealed class FacilityMark
    {
        public string Room = "", Kind = "";
        public int X, Y, Hp, Max;
        public int Day;
    }

    /// <summary>
    /// Kuşatma günlüğünün bir girdisi (aşama 11, belge §9.2): kanat ve bölgesi, kurulduğu gün, karar vardiyası (Due), sonuç ("" karar
    /// bekliyor, "savun" savunma emri verildi, "tutuldu", "dustu", "birak" komutla bırakıldı, "kendi" kapanışta kendiliğinden bırakıldı),
    /// sonucun günü (Closed; yoksa −1) ve savunmada terk edilen oda sayısı. JsonUtility null dizgiyi "" yazar: denetimler
    /// <c>string.IsNullOrEmpty</c> ile.
    /// </summary>
    [Serializable]
    public sealed class FacilitySiegeEntry
    {
        public string Sector = "", Region = "";
        public int Day, Due;
        public string Outcome = "";
        public int Closed = -1;
        public int Abandoned;
    }

    /// <summary>
    /// Önlem defterinin bir girdisi (aşama 10): YZ'nin önlemi öğrendiği gün, bölge, sınıf (<see cref="FacilityCounters.Classes"/>) ve
    /// ulaştığı kademe. Öğrenmenin eşitlik kuralı (<see cref="FacilityCounters.Learn"/>) ve sunumun teleksi bu sırayı okur.
    /// </summary>
    [Serializable]
    public sealed class FacilityCounterEntry
    {
        public int Day;
        public string Region, Class;
        public int Level;
    }
}
