using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Corrigible.Core
{
    public sealed class RegionDef
    {
        public string Id;
        string name;
        public string Name { get => Loc.T(name); set => name = value; }
        /// <summary>
        /// Bölgeyi işgal eden YZ'nin adı (VERITY, EMBER …): oyuncunun her yüzeyde gördüğü bölge adı (<c>Simulation.RegionName</c>;
        /// Docs/TESIS-BASKINLARI-TASARIMI.md §1). Ham dizgi: iki dilde aynıdır, <c>Loc.T</c>'den geçmez (tabloda kendi karşılığıyla durur).
        /// <see cref="Name"/> ve <see cref="SourceName"/> eski coğrafi adın yerelleştirme ve uyumluluk karşılaştırmalarında kullanılır.
        /// </summary>
        public string Ai { get; internal set; }
        /// <summary>Çevrilmemiş ad (Türkçe kaynak); geçmişte başka dilde yazılmış metni tanımak için (sunum).</summary>
        public string SourceName => name;
        public double Lon, Lat;
        /// <summary>Hesaplama gücü: örnek/test tahtasının (<c>Baskin.Create</c>) yerleşim seçimine katılır.</summary>
        public int Compute;
        /// <summary>Bölgenin kimliği: tahta özelliği (<c>Baskin.RegionTraits</c>), imza düşmanı (<c>Baskin.UnitTypes</c>) ve
        /// baskın boyunca tur tur işleyen bina içi olay (<c>Baskin.RegionEvents</c>).</summary>
        public string Trait, Enemy, Event;
        /// <summary>Tesis aşama 12: tesis şartı bugünkü olaydan farklıysa yeni olayın kimliği (na "katar", oc "yalpa"); öbür bölgelerde null
        /// (tesis şartı <see cref="Event"/>). Tek okuyucu <c>FacilityMaps.EventFor</c>; Katar/Yalpa alt bayrağı kapalıyken <see cref="Event"/> oynanır.</summary>
        public string FacilityEvent;
        string building;
        string[] sectors = new string[3];
        /// <summary>Baskınların geçtiği bina (fiş, CRT, görev çipi): olay, özellik ve imza makinesi bu binada.</summary>
        public string Building { get => Loc.T(building); set => building = value; }
        string buildingNote;
        /// <summary>Binanın iki cümlelik notu (görev fişinde bina satırının üstüne gelince): bina ne, olay neden orada. Aşama 12: na ve
        /// oc'nin notu iki hâle de uyar (eski olay: istasyondaki yük asansörü, platformun hortumu); yeni şartın notu <see cref="FacilityNote"/>.</summary>
        public string BuildingNote { get => Loc.T(buildingNote); set => buildingNote = value; }
        string facilityNote;
        /// <summary>Tesis aşama 12: tesis şartı (<see cref="FacilityEvent"/>) oynanırken binanın notu (Katar, Yalpa); öbür bölgelerde null.
        /// Tek okuyucu <c>Simulation.RegionNote</c> (şart bugünkü olaydan farklıysa bu, değilse <see cref="BuildingNote"/>).</summary>
        public string FacilityNote { get => facilityNote == null ? null : Loc.T(facilityNote); set => facilityNote = value; }
        /// <summary>Üç sektör binanın içine doğru üç oda: 01 ve 02 yağma, 03 sunucu (Docs/BASKIN-MEKANLARI.md).</summary>
        public string Sector(int index) => Loc.T(sectors[Math.Max(0, Math.Min(sectors.Length - 1, index))]);
        /// <summary>Çevrilmemiş sektör adı (Türkçe kaynak); geçmişte başka dilde yazılmış metni tanımak için (sunum).</summary>
        public string SourceSector(int index) => sectors[Math.Max(0, Math.Min(sectors.Length - 1, index))];
        internal void SetPlace(string buildingName, string[] sectorNames) { building = buildingName; sectors = sectorNames; }
    }

    public sealed class LinkDef
    {
        public string A, B;
    }

    public sealed class EndingDef
    {
        public string Id;
        string title, text;
        public string Title { get => Loc.T(title); set => title = value; }
        public string Text { get => Loc.T(text); set => text = value; }
    }

    /// <summary>Oyunun sabit içeriği: bölgeler, komşuluklar ve harekâtın sonu.</summary>
    public static class GameData
    {
        public static readonly RegionDef[] Regions =
        {
            // Bölgeyi tutan YZ'nin adı (oyuncunun gördüğü bölge adı; çevrilmez) ve eski coğrafi adı. Her bölgenin kendine özgü özelliği, imza düşmanı (her düşman iki bölgede), bina içi olayı ve binası; üç sektör
            // binanın içine doğru üç oda (01 ve 02 yağma, 03 sunucu); son satır binanın notu (Docs/BOLGE-OLAYLARI.md)
            // Aşama 12 (K7): na ve oc'nin bina ve kanat adları statik (bayraktan bağımsız) değişti; tesis şartının notu ayrı (facilityNote)
            R("na", Loc.N("WAYPOINT"), Loc.N("Kuzey Amerika"), -100, 40, 5, "kisa", "havan", "asansor",
                Loc.N("Sevkiyat katarı"), Loc.N("Yük vagonları"), Loc.N("Ayıklama vagonu"), Loc.N("Sunucu vagonu"),
                Loc.N("Kıtayı boydan boya geçen bir sevkiyat katarı; şimdi bir istasyona yanaşık duruyor. İstasyonun iki duraklı yük asansörü YZ'den önce kurulmuş bir zaman rölesiyle çalışır."),
                facilityEvent: "katar",
                facilityNote: Loc.N("Kıtayı boydan boya geçen bir sevkiyat katarı. Vagonların kuzey ve güney kenarı açık; dönüşte bağlantıya yakın makineler sonraki vagona atlar.")),
            R("sa", Loc.N("HEADWATER"), Loc.N("Güney Amerika"), -60, -15, 2, "takviye", "bomba", "tup",
                Loc.N("Hidroelektrik santrali"), Loc.N("Tüp deposu"), Loc.N("Türbin galerisi"), Loc.N("Kumanda odası"),
                Loc.N("Bir hidroelektrik santrali. Oksijen ve asetilen tüpleri zincirlenmeden duvar diplerine dizilmiş; vanası kopan tüp füze gibi uçar.")),
            R("weu", Loc.N("VERITY"), Loc.N("Batı Avrupa"), 5, 48, 4, "guvenli", "zdron", "bulut",
                Loc.N("Banka veri merkezi"), Loc.N("Teknik depo"), Loc.N("UPS odası"), Loc.N("Sunucu salonu"),
                Loc.N("YZ'nin ele geçirdiği bir banka veri merkezi. Tavandaki CO₂ memeleri yangın sanıp salonları beyaz bir bulutla doldurur.")),
            R("eeu", Loc.N("QUORUM"), Loc.N("Doğu Avrupa"), 27, 51, 2, "dar", "kilit", "raf",
                Loc.N("Devlet arşivi"), Loc.N("Mikrofilm deposu"), Loc.N("Dosya katı"), Loc.N("Tarama odası"),
                Loc.N("Eski rejimden kalma bir devlet arşivi. Raylı kompakt raflar; YZ arşivi tararken raf bloğunu kaydırır.")),
            R("ru", Loc.N("BOREALIS"), Loc.N("Rusya"), 90, 60, 3, "sogutma", "kalkan", "kriyo",
                Loc.N("Kutup veri merkezi"), Loc.N("Soğuk depo"), Loc.N("Azot galerisi"), Loc.N("Sunucu bloğu"),
                Loc.N("Donmuş toprağa gömülü, soğuk depolu bir veri merkezi. Sıvı azotlu soğutma hattı YZ'nin çevrimine bağlı ve takvimle boşalır.")),
            R("me", Loc.N("EMBER"), Loc.N("Orta Doğu"), 45, 28, 3, "sis", "bomba", "gaz",
                Loc.N("Petrol rafinerisi"), Loc.N("Malzeme ambarı"), Loc.N("Pompa holü"), Loc.N("Kompresör dairesi"),
                Loc.N("Meşalesi hiç sönmeyen bir rafineri. YZ gaz dedektörlerini susturdu; sızdıran bir flanştan yanıcı gaz geliyor.")),
            R("af", Loc.N("CAIRN"), Loc.N("Afrika"), 20, 5, 1, "acik", "havan", "tavan",
                Loc.N("Cevher hangarı"), Loc.N("Konveyör hattı"), Loc.N("Kırıcı bölümü"), Loc.N("Kumanda kabini"),
                Loc.N("YZ'nin yarım kalmış bir cevher hangarı. Yıkılan duvarlar taşıyıcıydı; üstlerindeki tavan çatlak.")),
            R("in", Loc.N("SOLACE"), Loc.N("Hindistan"), 78, 22, 3, "kalabalik", "kalkan", "turnike",
                Loc.N("Çağrı merkezi"), Loc.N("Personel kantini"), Loc.N("Kasa holü"), Loc.N("Santral odası"),
                Loc.N("Binlerce masalı bir banka ve sigorta çağrı merkezi. YZ kart okuyucuları kapattı; turnikeler yalnız mekanik yön kilidiyle döner.")),
            R("cn", Loc.N("PARAGON"), Loc.N("Çin"), 105, 35, 5, "zirhli", "cogalt", "kaynak",
                Loc.N("Montaj fabrikası"), Loc.N("Parça ambarı"), Loc.N("Kaynak hattı"), Loc.N("Hat kontrol odası"),
                Loc.N("YZ'nin kendi robotlarını kaynakladığı bir montaj fabrikası. Zemine cıvatalı bir kaynak kolu ağdan kopmuş, döngüsünü kimseye sormadan sürdürüyor.")),
            R("jk", Loc.N("TESSEL"), Loc.N("Japonya-Kore"), 135, 37, 4, "sebeke", "kilit", "faz",
                Loc.N("Çip fabrikası"), Loc.N("Malzeme kilidi"), Loc.N("Temiz oda koridoru"), Loc.N("Trafo merkezi"),
                Loc.N("Akıllı enerji yönetimli bir çip fabrikası. Kat panoları yükü iki faz arasında aktarır; döşeme altındaki kanallar sırayla canlanır.")),
            R("sea", Loc.N("EDDY"), Loc.N("Güneydoğu Asya"), 110, 5, 2, "fabrika", "cogalt", "suzgec",
                Loc.N("Elektronik fabrikası"), Loc.N("Bodrum ambarı"), Loc.N("Sintine kanalı"), Loc.N("Kopya atölyesi"),
                Loc.N("Bodrumunu sel basmış bir elektronik fabrikası. Sintine pompası üç turda bir geri çalışır ve yer süzgeci suyu çeker.")),
            R("oc", Loc.N("HALCYON"), Loc.N("Okyanusya"), 135, -25, 2, "devriye", "zdron", "yag",
                Loc.N("Yüzer maden platformu"), Loc.N("Kumanya güvertesi"), Loc.N("Bant galerisi"), Loc.N("Makine dairesi"),
                Loc.N("Açık denizde demirli, uzaktan yönetilen ıssız bir maden platformu. Hidrolik kaldırıcının hortumu patlamak üzere."),
                facilityEvent: "yalpa",
                facilityNote: Loc.N("Açık denizde demirli bir maden platformu. Güverte dalgayla 2., 4. ve 6. turun sonunda sırayla batıya, doğuya, batıya yatar.")),
        };

        /// <summary>Kara ve kablo bağlantıları: yeni cephe, özgür bir bölgeye komşu olmalı.</summary>
        public static readonly LinkDef[] Links =
        {
            L("na", "weu"), L("na", "sa"), L("na", "jk"),
            L("sa", "af"), L("weu", "eeu"), L("weu", "af"), L("weu", "me"),
            L("eeu", "ru"), L("eeu", "me"), L("ru", "cn"), L("me", "in"),
            L("me", "af"), L("in", "cn"), L("in", "sea"), L("cn", "jk"),
            L("cn", "sea"), L("sea", "oc"), L("jk", "oc"),
        };

        public static readonly EndingDef[] Endings =
        {
            new EndingDef { Id = "topluluk", Title = Loc.N("Son kişi"), Text = Loc.N("Sığınağın son sakini de kaybedildi. Panoda yalnız boş görev cepleri ve geri dönmeyenlerin kartları kaldı.") },
            new EndingDef { Id = "silme", Title = Loc.N("Silme"), Text = Loc.N("Çekirdek yok edildi, kopyalar temizlendi. Ama bir kez yapıldıysa, biri yine yapacak.") },
            new EndingDef { Id = "dagilma", Title = Loc.N("Dağılma"), Text = Loc.N("Hastaneler ve bankalar bizim baskınlarımızda karardı. Halk direnişe sırtını döndü; sığınağın kapısını artık kimse çalmıyor.") },
            new EndingDef { Id = "vuruldu", Title = Loc.N("Vuruldu"), Text = Loc.N("Kapıyı açtın ve silah patladı. Sığınak bir gecede komutansız kaldı; sabah telsizden yalnız parazit geliyordu.") },
            new EndingDef { Id = "sizma", Title = Loc.N("Sızma"), Text = Loc.N("Kapıyı bir kopyaya açtın. Sığınak 07'nin yeri artık YZ'nin haritasında; kopyalar bir sonraki kapıyı çalıyor.") },
            new EndingDef { Id = "dusus", Title = Loc.N("Sığınak düştü"), Text = Loc.N("Konum çözüldü. YZ'nin makineleri kapıya vardığında panoda hâlâ ertesi vardiyanın planı asılıydı.") },
            new EndingDef { Id = "terk", Title = Loc.N("Terk"), Text = Loc.N("Erzak bitti, insanlar birer birer kapıdan çıktı. Geride kalanlar sığınağı ayakta tutamadı.") },
            new EndingDef { Id = "salter", Title = Loc.N("Son şalter"), Text = Loc.N("Şalter indi, çekilme emri damgalandı. Sığınak 07 karardı; çıkanlar bir sonraki sığınağa yürüdü.") },
        };

        static readonly Dictionary<string, RegionDef> regionById = Regions.ToDictionary(r => r.Id);
        static readonly Dictionary<string, EndingDef> endingById = Endings.ToDictionary(e => e.Id);

        public static RegionDef Region(string id) => regionById[id];
        public static EndingDef Ending(string id) => endingById[id];

        static RegionDef R(string id, string ai, string name, double lon, double lat, int compute, string trait, string enemy, string ev, string building,
            string s1, string s2, string s3, string buildingNote, string facilityEvent = null, string facilityNote = null)
        {
            var d = new RegionDef { Id = id, Ai = ai, Name = name, Lon = lon, Lat = lat, Compute = compute, Trait = trait, Enemy = enemy, Event = ev,
                BuildingNote = buildingNote, FacilityEvent = facilityEvent, FacilityNote = facilityNote };
            d.SetPlace(building, new[] { s1, s2, s3 });
            return d;
        }

        /// <summary>Sektör kimliğinin sırası ("weu_2" → 1); çözülmezse −1.</summary>
        public static int SectorIndex(string sectorId)
        {
            int cut = sectorId?.LastIndexOf('_') ?? -1;
            return cut > 0 && int.TryParse(sectorId.Substring(cut + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) && n >= 1 ? n - 1 : -1;
        }
        static LinkDef L(string a, string b) => new LinkDef { A = a, B = b };
    }
}
