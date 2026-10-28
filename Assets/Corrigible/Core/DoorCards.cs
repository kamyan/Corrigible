using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>
    /// Bir kapı kararının etkisi: kaynaklar, güven, alarm, sığınağa katılan ya da ayrılan kişi, plaka, uzmanların yarası,
    /// zincir bayrakları, sonraki ziyaretçi ve harekâtın sonu. Metin çevrilmemiş kaynaktır (gösterirken <c>Loc.T</c>).
    /// </summary>
    public sealed class DoorEffect
    {
        public double Supplies, Materials, Fon, Alarm;
        /// <summary>Population: katılan (+) ya da ayrılan (−) kişi; Plates: zırh plakası (depo doluysa malzemeye döner);
        /// Heal: uzmanların bütün yarası sarılır ve yatanlar bu kadar gün erken kalkar; Wound: ayaktaki her uzmana yara (düşürmez).</summary>
        public int Trust, Population, Plates, Heal, Wound;
        public string Flag, Unflag;
        /// <summary>Zincir: bu kartlardan biri (kapının tohumuyla seçilir) <see cref="ChainMin"/>–<see cref="ChainMax"/> gün sonra çalar.</summary>
        public string[] Chain;
        public int ChainMin = 3, ChainMax = 5;
        /// <summary>Harekâtı bitirir (<see cref="GameData.Endings"/>).</summary>
        public string End;
        public string Text;
    }

    /// <summary>Kapıdaki bir seçenek. Etkisi duruma göre değişebilir (silahlı sakinle konuşmak erzağa ve güvene bakar).</summary>
    public sealed class DoorOption
    {
        public string Id;
        public string Label;
        public Func<Simulation, DoorEffect> Effect;
        /// <summary>Seçeneğin koşulu: tutmuyorsa gerekçe (ör. revir çalışmıyor), tutuyorsa null.</summary>
        public Func<Simulation, string> Need;
        /// <summary>Etkideki eksi erzak, malzeme ve fon bir bedeldir: yetmiyorsa seçilemez ve düğmede yazar.</summary>
        public bool Paid = true;
    }

    /// <summary>
    /// Bir ziyaretçi: kim olduğu, kapalı kapıda duyulan (vuruş ve ses), gözetleme deliğinden görülen, interkomdan parolaya
    /// verilen cevap ve kapı açılınca söylediği. Gözetleme ve interkom sunumdur (durumu değiştirmez). Kopyalar parolayı bilmez,
    /// saat gibi vurur ve camı buğulamaz; dışarıdan gelen insanlar bilmeyebilir ama nefesleri camı buğular.
    /// </summary>
    public sealed class DoorCard
    {
        public string Id;
        public string Who, Knock, Peek, Ask, Opened;
        /// <summary>Kendiliğinden gelme koşulu; null ise yalnız zincirle (ya da ilk vuruşta) gelir.</summary>
        public Func<Simulation, bool> When;
        /// <summary>Zincirle gelen kartın o gün hâlâ gelmesi için koşul (null: her zaman).</summary>
        public Func<Simulation, bool> Still;
        public int Weight = 10;
        /// <summary>Once: bir kez gelir. Deadly: kapıyı açmak harekâtı bitirir; güvenli günlerde (<c>doorSafeShifts</c>) gelmez.
        /// Nobody: kapıda kimse durmuyor (zarf, paket; sunum siluet göstermez).</summary>
        public bool Once, Deadly, Nobody;
        /// <summary>Kapı açılınca hemen uygulanan etki (tuzak, ölüm, haber).</summary>
        public DoorEffect OnOpen;
        /// <summary>Açık kapıdaki seçenekler; boşsa kapı açılınca ziyaret biter.</summary>
        public DoorOption[] Options = Array.Empty<DoorOption>();
        /// <summary>Kapı kapalıyken Kapıyı aç ve Açma'dan önceki seçenekler (silahlı sakinle interkomdan konuşmak).</summary>
        public DoorOption[] Closed = Array.Empty<DoorOption>();
        /// <summary>Açılmayan kapının etkisi (Açma ya da vardiyanın bitmesi).</summary>
        public DoorEffect Ignore = new DoorEffect();
    }

    /// <summary>
    /// Kapının ziyaretçileri. Etkilerin sayıları kartın içeriğidir (birim türlerinin canı gibi); vuruş sıklığı, güvenli
    /// günler ve açlık eşiği <see cref="Balance"/>'tadır. Kimlikler kayda ve zincirlere yazılır; değiştirme.
    /// </summary>
    public static class DoorCards
    {
        /// <summary>İlk vuruş: senaryolu ve dostça; kapıyı öğretir.</summary>
        public const string First = "ilk";
        /// <summary>Bayraklar: kopyalar konusunda uyarıldın; aç sakinlerin heyetini geri çevirdin; yas tutan babayı cevapsız bıraktın.</summary>
        public const string CopyWarning = "kopya_uyarisi", DelegationRefused = "heyet_ret", GriefUnanswered = "baba_ofke";

        public static readonly DoorCard[] All =
        {
            new DoorCard
            {
                Id = First, Once = true,
                Who = Loc.N("Nermin, yaşam alanından"),
                Knock = Loc.N("Üç kısa, bir uzun vuruş. Yaşlı bir kadın sesi: \"Komutan? Benim, Nermin. Yaşam alanından.\""),
                Peek = Loc.N("Koridor lambasının altında yaşlı bir kadın. Kucağında bez bir kese; nefesi camı buğuluyor."),
                Ask = Loc.N("Parolayı soruyorsun. \"Yedi,\" diyor gülerek. \"Kapının üstünde yazıyor ya, evladım.\""),
                Opened = Loc.N("\"Mutfakta artanları kuruttuk, ekip yesin diye getirdim.\" Giderken dönüyor: \"Bu kapıyı her çalan dost değildir. Önce bak, sonra aç.\""),
                Options = new[]
                {
                    O("al", Loc.N("Keseyi al"), new DoorEffect { Supplies = 6, Text = Loc.N("Kese depoya girdi. Yaşam alanında konuşuluyor: komuta kapıyı açıyor.") }),
                    O("geri", Loc.N("Sakinlere geri ver"), new DoorEffect { Trust = 1, Text = Loc.N("\"Onlar daha aç,\" diyorsun. Nermin keseyi geri götürüyor; adın yaşam alanında iyi anılıyor.") }),
                },
                Ignore = new DoorEffect { Text = Loc.N("Kese kapının önünde kaldı. Sabaha biri almıştı.") },
            },
            new DoorCard
            {
                Id = "anne", When = s => true, Weight = 10,
                Who = Loc.N("Alt kattan bir anne"),
                Knock = Loc.N("Aceleci, düzensiz vuruşlar. \"Lütfen! Oğlum ateşler içinde, yardım edin!\""),
                Peek = Loc.N("Genç bir kadın; omzunda battaniyeye sarılı bir çocuk. Nefesi camı buğuluyor."),
                Ask = Loc.N("\"Parola?\" \"Yedi! Yedi, lütfen açın!\""),
                Opened = Loc.N("Çocuğun dudakları kurumuş. \"İlaç yoksa biraz yiyecek… İki gündür bir şey yemedi.\""),
                Options = new[]
                {
                    O("erzak", Loc.N("Erzak ver"), new DoorEffect { Supplies = -8, Chain = new[] { "anne_oglu" }, ChainMin = 4, ChainMax = 7,
                        Text = Loc.N("Kadın keseyi göğsüne bastırıyor. \"Bunu unutmayacağım.\"") }),
                    O("revir", Loc.N("Revire götür"), new DoorEffect { Chain = new[] { "anne_oglu" }, ChainMin = 4, ChainMax = 7,
                        Text = Loc.N("Revirdeki görevli çocuğu yatırıyor. Ateş gece düşüyor.") }, NeedInfirmary),
                    O("yok", Loc.N("\"İlaç yok\" de"), new DoorEffect { Trust = -1, Text = Loc.N("Kadın bir şey demeden dönüyor. Ayak sesleri merdivende kayboluyor.") }),
                },
                Ignore = new DoorEffect { Trust = -1, Text = Loc.N("Vuruşlar bir süre sürdü, sonra kesildi. Alt katta komutanın kapıyı açmadığı konuşuluyor.") },
            },
            new DoorCard
            {
                Id = "anne_oglu", Once = true,
                Who = Loc.N("İyileşen çocuğun ağabeyi"),
                Knock = Loc.N("İki sert vuruş. \"Komutan, ben Deniz. Kardeşim iyileşti.\""),
                Peek = Loc.N("Yirmilerinde bir genç; sırtında alet çantası."),
                Ask = Loc.N("\"Parola?\" \"Yedi. Annem her gece söylüyor zaten.\""),
                Opened = Loc.N("\"Kardeşim için borcumuz var. Kablo çekerim, kaynak yaparım; nerede lazımsam.\""),
                Options = new[]
                {
                    O("yasam", Loc.N("Yaşam alanına al"), new DoorEffect { Population = 1, Trust = 1, Text = Loc.N("Deniz yaşam alanına yerleşti. Bir çift el daha.") }),
                    O("hurda", Loc.N("Hurda toplasın"), new DoorEffect { Materials = 8, Text = Loc.N("Deniz iki gün sonra bir çuval kablo ve sacla döndü.") }),
                },
                Ignore = new DoorEffect { Materials = 2, Text = Loc.N("Genç bir süre bekledi, sonra alet çantasından birkaç parçayı kapının önüne bırakıp gitti.") },
            },
            new DoorCard
            {
                Id = "kurye", When = s => s.FreeRegions > s.DoorSeenCount("kurye"), Weight = 14,
                Who = Loc.N("Özgür bölgeden bir kurye"),
                Knock = Loc.N("Hızlı, kendinden emin üç vuruş. \"Kurye! İmza lazım.\""),
                Peek = Loc.N("Yağmurluklu bir kadın, sırtında mühürlü bir koli. Botları çamur içinde."),
                Ask = Loc.N("\"Parola?\" \"Yedi. Haritanızdaki mavi bölgeden geliyorum.\""),
                Opened = Loc.N("Kolide konserve, kablo ve bir mektup: \"Elektrik geri geldi, çocuklar okula dönüyor. Teşekkürler, Yedi.\""),
                Options = new[]
                {
                    O("al", Loc.N("Koliyi depoya al"), new DoorEffect { Supplies = 6, Materials = 6, Text = Loc.N("Koli depoya girdi; mektup mantar panoya iğnelendi.") }),
                    O("dagit", Loc.N("Sakinlere dağıt"), new DoorEffect { Trust = 1, Text = Loc.N("Konserveler yaşam alanında paylaşıldı, mektup yüksek sesle okundu.") }),
                },
                Ignore = new DoorEffect { Supplies = 3, Materials = 3, Text = Loc.N("Kurye koliyi kapının önüne bıraktı; sabaha yarısı kayıptı.") },
            },
            new DoorCard
            {
                Id = "kacakci", When = s => s.S.Fon >= 30, Weight = 8,
                Who = Loc.N("Bir kaçakçı"),
                Knock = Loc.N("Kapı tıklatılıyor, neredeyse kibarca. \"Komutan, iş konuşalım.\""),
                Peek = Loc.N("Deri ceketli bir adam; ayağının dibinde iki kasa. Arkasında kimse yok."),
                Ask = Loc.N("\"Parola?\" \"Yedi. Parolanızı bilmeyen kalmadı, komutan.\" Gülüyor."),
                Opened = Loc.N("\"Konserve, pirinç, kuru et. YZ'nin depolarından. Fiyatı fon; soru yok.\""),
                Options = new[]
                {
                    O("al", Loc.N("Fonla erzak al"), new DoorEffect { Fon = -10, Supplies = 12, Text = Loc.N("Kasalar depoya girdi. Adam fonu sayıp karanlığa karıştı.") }),
                    O("ret", Loc.N("Reddet"), new DoorEffect { Text = Loc.N("\"Fiyat yükselir,\" diyor ve kasalarını alıp gidiyor.") }),
                    O("ihbar", Loc.N("İhbar et"), new DoorEffect { Trust = 1, Chain = new[] { "kacakci_intikam" }, ChainMin = 3, ChainMax = 5,
                        Text = Loc.N("Sakinler adamı merdivende yakaladı; kasalar paylaşıldı. Giderken bağırıyordu: \"Bunu ödeyeceksiniz!\"") }),
                },
                Ignore = new DoorEffect { Text = Loc.N("Adam bir süre bekledi, sonra kasalarını alıp gitti.") },
            },
            new DoorCard
            {
                Id = "kacakci_intikam", Nobody = true,
                Who = Loc.N("Kapının önünde bir paket"),
                Knock = Loc.N("Tek bir vuruş, sonra koşan ayak sesleri."),
                Peek = Loc.N("Koridor boş. Kapının dibinde kahverengi bir paket; bir köşesinden tel uçları görünüyor."),
                Ask = Loc.N("İnterkomdan sesleniyorsun. Cevap yok; yalnız paketten gelen bir tıkırtı."),
                OnOpen = new DoorEffect { Materials = -10, Trust = -1,
                    Text = Loc.N("Kapıyı açtığın anda paket patladı. Kimse ölmedi ama depo hasar gördü. Duvarda tebeşirle: \"Muhbir.\"") },
                Ignore = new DoorEffect { Materials = -4, Text = Loc.N("Paket koridorda patladı. Kapı dayandı ama yandaki depo bölmesi hasar gördü; yaralanan olmadı.") },
            },
            new DoorCard
            {
                Id = "teknisyen", When = s => s.S.Day >= 10, Once = true, Weight = 8,
                Who = Loc.N("YZ'den kaçmış bir teknisyen"),
                Knock = Loc.N("Temkinli, aralıklı vuruşlar. \"Ağ'dan kaçtım. Bakım ünitesindeydim.\""),
                Peek = Loc.N("Zayıf bir adam; boynunda kesik bir veri kablosunun izi. Elleri titriyor, nefesi camı buğuluyor."),
                Ask = Loc.N("\"Parola?\" Bir an duraksıyor. \"Yedi… değil mi? Yolda bir kadın söyledi.\""),
                Opened = Loc.N("\"YZ'nin bakım rotalarını biliyorum. Plaka kalıpları, frekanslar… Beni içeri alın, işe yararım.\""),
                Options = new[]
                {
                    O("al", Loc.N("Sığınağa al"), new DoorEffect { Population = 1, Materials = 4, Chain = new[] { "teknisyen_sadik", "teknisyen_casus" }, ChainMin = 4, ChainMax = 6,
                        Text = Loc.N("Teknisyen atölyenin köşesine yerleşti. İlk gece hiç uyumadı.") }),
                    O("ret", Loc.N("Geri çevir"), new DoorEffect { Text = Loc.N("Adam başını eğip karanlığa döndü.") }),
                },
                Ignore = new DoorEffect { Text = Loc.N("Vuruşlar kesildi. Sabah koridorda bir kablo parçası buldular.") },
            },
            new DoorCard
            {
                Id = "teknisyen_sadik", Once = true,
                Who = Loc.N("Kaçak teknisyen"),
                Knock = Loc.N("Atölye tarafından iki vuruş. \"Komutan, bir şey göstereceğim.\""),
                Peek = Loc.N("Kaçak teknisyen; elinde gri, yeni dökülmüş bir plaka."),
                Ask = Loc.N("\"Parola?\" \"Yedi, komutan. Artık ben de buradanım.\""),
                Opened = Loc.N("\"YZ'nin kalıbını hatırladım: daha ince, daha sağlam. İlkini ekibe getirdim.\""),
                Options = new[]
                {
                    O("al", Loc.N("Plakayı al"), new DoorEffect { Plates = 1, Materials = 4, Text = Loc.N("Plaka rafa kondu. Atölye yeni kalıpla çalışıyor.") }),
                },
                Ignore = new DoorEffect { Plates = 1, Text = Loc.N("Teknisyen plakayı kapının önüne bıraktı.") },
            },
            new DoorCard
            {
                Id = "teknisyen_casus", Once = true,
                Who = Loc.N("Telsiz görevlisi"),
                Knock = Loc.N("Hızlı vuruşlar. \"Komutan! Telsiz odasından geliyorum, acil!\""),
                Peek = Loc.N("Telsiz görevlisi; elinde bir kâğıt şerit, yüzü bembeyaz."),
                Ask = Loc.N("\"Parola?\" \"Yedi! Açın, vakit yok!\""),
                Opened = Loc.N("\"Kaçak teknisyen geceleri YZ frekansına yayın yapıyormuş. Yerimizi veriyor olabilir.\""),
                Options = new[]
                {
                    O("cikar", Loc.N("Teknisyeni sığınaktan çıkar"), new DoorEffect { Population = -1, Alarm = 2,
                        Text = Loc.N("Teknisyen koridordan çıkarıldı. Yayın kesildi ama YZ bir şeyler duymuş.") }),
                    O("karistir", Loc.N("Frekansı karıştır"), new DoorEffect { Materials = -6, Population = -1, Alarm = 1,
                        Text = Loc.N("Telsizciler bütün gece parazit bastı. Teknisyen sabah ortadan kaybolmuştu.") }),
                },
                Ignore = new DoorEffect { Alarm = 4, Text = Loc.N("Kimse bir şey yapmadı. Yayın sabaha kadar sürdü.") },
            },
            new DoorCard
            {
                Id = "gozcu", Weight = 12,
                When = s => s.S.Day >= 5 && !s.DoorFlag(CopyWarning) && (s.S.Counterattacks > 0 || s.S.Alarm >= 5),
                Who = Loc.N("Yaralı bir gözcü"),
                Knock = Loc.N("Zayıf, düzensiz vuruşlar; biri kapıya yaslanmış. \"Komutan… dışarıdan geliyorum.\""),
                Peek = Loc.N("Yerde oturan bir adam, kolu kanlı bir sargıda. Her nefeste cam buğulanıp açılıyor."),
                Ask = Loc.N("\"Parola?\" \"Yedi… yedi numara.\" Öksürüyor."),
                Opened = Loc.N("\"Dinleyin. YZ'nin yeni kopyaları insan sesiyle konuşuyor, kusursuz. Açmadan parolayı sorun: bilmiyorlar. Bir de vuruşları… saat gibi.\""),
                OnOpen = new DoorEffect { Flag = CopyWarning },
                Options = new[]
                {
                    O("revir", Loc.N("Revire al"), new DoorEffect { Text = Loc.N("Gözcü revirde. Uyarısı telsizden bütün kata okundu.") }, NeedInfirmary),
                    O("sargi", Loc.N("Sargı ve erzak ver"), new DoorEffect { Supplies = -3, Text = Loc.N("Gözcü sargısını yenileyip yeniden karanlığa çıktı.") }),
                },
                Ignore = new DoorEffect { Text = Loc.N("Vuruşlar kesildi. Sabah kapının önünde kan izleri vardı.") },
            },
            new DoorCard
            {
                Id = "kopya", Deadly = true, Weight = 9,
                When = s => s.DoorFlag(CopyWarning) && (s.S.Alarm >= s.B.liberationAlarmThreshold * s.B.doorCopyAlarm
                    || s.S.LastCounterattackDay >= 0 && s.S.Day - s.S.LastCounterattackDay <= s.B.doorCopyAfterCounterattack),
                Who = Loc.N("Dışarıda kalan biri"),
                Knock = Loc.N("Vuruşlar saat gibi: tık, tık, tık, hep aynı aralıkla. \"Dışarıda kaldım. Açın lütfen, çok soğuk.\""),
                Peek = Loc.N("Koridor ışığı sönük. Siluet kıpırdamıyor; camda nefes buğusu yok."),
                Ask = Loc.N("\"Parola?\" Kısa bir sessizlik. \"Parola… Açın lütfen, çok soğuk.\" Ses, ilk cümleyle aynı tonda."),
                OnOpen = new DoorEffect { End = "sizma",
                    Text = Loc.N("Kapı aralandığı anda soğuk bir el içeri uzandı. Koridordaki şeyin yüzü yoktu; yalnız bir mercek ve senin sesin.") },
                Ignore = new DoorEffect { Text = Loc.N("Vuruşlar bir saat boyunca aynı aralıkla sürdü, sonra kesildi. Sabah koridordaki lamba kırıktı.") },
            },
            new DoorCard
            {
                Id = "disarida", When = s => s.S.Day >= 6, Weight = 7,
                Who = Loc.N("Dışarıda kalan biri"),
                Knock = Loc.N("Telaşlı, düzensiz vuruşlar. \"Dışarıda kaldım! Açın lütfen, çok soğuk!\""),
                Peek = Loc.N("Omuzları karla kaplı bir adam, ellerini koltuk altına sokmuş. Nefesi camı buğuluyor."),
                Ask = Loc.N("\"Parola?\" \"Yedi! Yemin ederim. Bulut Usta'nın oğluyum, ikinci kat!\""),
                Opened = Loc.N("Adam içeri yığılıyor. Parmakları morarmış ama yaşıyor. \"Devriye yolu kesti. Saatlerdir bekliyorum.\""),
                Options = new[]
                {
                    O("al", Loc.N("Yaşam alanına götür"), new DoorEffect { Population = 1, Text = Loc.N("Battaniye, sıcak çay. Bulut Usta oğlunu sabaha kadar bırakmadı.") }),
                },
                Ignore = new DoorEffect { Trust = -2, Text = Loc.N("Vuruşlar sabaha karşı kesildi. Koridorda donmuş bir adam buldular: Bulut Usta'nın oğlu.") },
            },
            new DoorCard
            {
                Id = "heyet", When = s => s.S.Door.HungryShifts >= 2, Weight = 30,
                Who = Loc.N("Aç sakinlerin heyeti"),
                Knock = Loc.N("Birçok yumruk aynı anda. \"Komutan! Konuşmamız lazım!\""),
                Peek = Loc.N("Koridorda altı yedi kişi; önde tanıdık yüzler. Kimsenin elinde silah yok."),
                Ask = Loc.N("\"Parola?\" \"Yedi! Biz buranın insanıyız, komutan. Ve açız.\""),
                Opened = Loc.N("\"Paylar yarıya indi, çocuklar geceleri ağlıyor. Ekibe ayrılan erzaktan pay istiyoruz.\""),
                Options = new[]
                {
                    O("fon", Loc.N("Fonla erzak bul"), new DoorEffect { Fon = -8, Supplies = 8, Unflag = DelegationRefused,
                        Text = Loc.N("Telsizle bir kaçakçıya ulaştın. Erzak geceye gelmeden dağıtıldı.") }),
                    O("ekip", Loc.N("Ekibin payını dağıt"), new DoorEffect { Supplies = 4, Wound = 1, Unflag = DelegationRefused,
                        Text = Loc.N("Ekip bu hafta yarım payla. Uzmanlar yorgun ama sakinler yatıştı.") }),
                    O("bekle", Loc.N("\"Bekleyin\" de"), new DoorEffect { Trust = -1, Flag = DelegationRefused, Chain = new[] { "silahli" }, ChainMin = 2, ChainMax = 3,
                        Text = Loc.N("Heyet sessizce dağıldı. Biri giderken kapıyı tekmeledi.") }),
                },
                Ignore = new DoorEffect { Trust = -2, Flag = DelegationRefused, Chain = new[] { "silahli" }, ChainMin = 2, ChainMax = 3,
                    Text = Loc.N("Kapıyı açmadın. Bağırışlar gece yarısına kadar sürdü.") },
            },
            new DoorCard
            {
                Id = "silahli", Deadly = true, Still = s => s.Hungry && s.DoorFlag(DelegationRefused),
                Who = Loc.N("Silahlı bir sakin"),
                Knock = Loc.N("Önce yumruk, sonra dipçik. \"Aç şu kapıyı! Çocuklar açken siz depoda oturuyorsunuz!\""),
                Peek = Loc.N("Bir tüfeğin namlusu cama dayalı. Arkasında kimse yok; adam titriyor."),
                Ask = Loc.N("\"Silahı indir, konuşalım.\" \"Önce erzak! Ya da bir söz… ama artık sözlere inanmıyorum.\""),
                OnOpen = new DoorEffect { End = "vuruldu", Text = Loc.N("Kapı açılır açılmaz silah patladı. Adam ne yaptığını ancak sonra anladı.") },
                Closed = new[]
                {
                    new DoorOption { Id = "konus", Label = Loc.N("İnterkomdan konuş"), Paid = false, Effect = s =>
                        s.Shelter.Supplies >= 8
                            ? new DoorEffect { Supplies = -8, Trust = 1, Unflag = DelegationRefused,
                                Text = Loc.N("Erzağı kapının altındaki kapaktan itiyorsun. Ayak sesleri uzaklaşıyor; tüfek koridorda kalmış.") }
                            : s.S.Trust >= 20
                                ? new DoorEffect { Trust = -1, Unflag = DelegationRefused, Text = Loc.N("Sesini tanıyor. \"Söz verdin,\" diyor ve namluyu indiriyor.") }
                                : new DoorEffect { Materials = -8, Trust = -2, Unflag = DelegationRefused,
                                    Text = Loc.N("Konuşma uzadıkça vuruşlar sertleşti. Kilit kırıldı; adam depodan ne bulduysa alıp kaçtı.") } },
                },
                Ignore = new DoorEffect { Materials = -8, Trust = -2, Unflag = DelegationRefused,
                    Text = Loc.N("Kapıyı zorladılar. Kilit kırıldı, depodan malzeme gitti; yaralanan olmadı.") },
            },
            new DoorCard
            {
                Id = "baba", Weight = 20, Once = false,
                When = s => s.S.RaidLog.Count > 0 && s.S.RaidLog.Last().Collateral >= 2 && s.S.Day - s.S.RaidLog.Last().Day <= 2
                    && s.S.RaidLog.Count(l => l.Collateral >= 2) > s.DoorSeenCount("baba"),
                Who = Loc.N("Kolunda siyah bant olan bir baba"),
                Knock = Loc.N("Ağır, yavaş vuruşlar. \"Komutan. Sizinle yüz yüze konuşmak istiyorum.\""),
                Peek = Loc.N("Orta yaşlı bir adam; kolunda siyah bir bant. Elleri boş."),
                Ask = Loc.N("\"Parola?\" \"Yedi. Kızım da bilirdi.\""),
                Opened = Loc.N("\"Baskınınızda hastanenin sunucusu kırıldı. Kızım solunum cihazındaydı.\" Bir süre susuyor. \"Neden?\""),
                Options = new[]
                {
                    O("ozur", Loc.N("Özür dile, erzak ver"), new DoorEffect { Supplies = -6, Trust = 1,
                        Text = Loc.N("Adam erzağı almıyor ama elini sıkıyor. \"Bir dahaki sefere dikkat edin.\"") }),
                    new DoorOption { Id = "sessiz", Label = Loc.N("Sessiz kal"), Effect = Silence(Loc.N("Adam cevap bekledi. Gelmeyince arkasını dönüp gitti.")) },
                },
                Ignore = new DoorEffect { Trust = -1, Text = Loc.N("Kapıyı açmadın. Adam sabaha kadar kapının önünde oturdu.") },
            },
            new DoorCard
            {
                Id = "silahli_baba", Deadly = true, Still = s => s.S.Trust <= 10,
                Who = Loc.N("Kolunda siyah bant olan adam"),
                Knock = Loc.N("Tek, ağır bir vuruş. \"Komutan. Bu sefer konuşmaya gelmedim.\""),
                Peek = Loc.N("Siyah bantlı adam. Elinde bir tabanca; namlusu yere bakıyor."),
                Ask = Loc.N("\"Silahı bırak.\" \"Kızım da nefes almak istemişti.\""),
                OnOpen = new DoorEffect { End = "vuruldu", Text = Loc.N("Kapıyı açtın. Adam bir an yüzüne baktı, sonra tetiği çekti.") },
                Closed = new[]
                {
                    new DoorOption { Id = "konus", Label = Loc.N("İnterkomdan özür dile"), Paid = false, Effect = s =>
                        s.S.Trust >= 8
                            ? new DoorEffect { Trust = 1, Unflag = GriefUnanswered, Text = Loc.N("Uzun bir sessizlik. Sonra tabancanın yere düştüğünü duyuyorsun.") }
                            : new DoorEffect { Materials = -6, Trust = -2, Unflag = GriefUnanswered,
                                Text = Loc.N("Kapıya üç el ateş etti ve kayboldu. Kilit parçalandı, depo yağmalandı.") } },
                },
                Ignore = new DoorEffect { Materials = -6, Trust = -2, Unflag = GriefUnanswered,
                    Text = Loc.N("Kapıya üç el ateş etti ve kayboldu. Kilit parçalandı, depo yağmalandı.") },
            },
            new DoorCard
            {
                Id = "hekim", Weight = 10,
                When = s => s.S.Teams.Any(t => t.Members.Any(m => m.Wounds > 0 || m.Down > 0)),
                Who = Loc.N("Gezgin bir hekim"),
                Knock = Loc.N("Kibar iki vuruş. \"İyi akşamlar. Hekimim; sığınak sığınak dolaşıyorum.\""),
                Peek = Loc.N("Yaşlı bir kadın, deri bir çanta. Gözlüğü buğulanmış."),
                Ask = Loc.N("\"Parola?\" \"Yedi. Kuzeydeki sığınaklar sizi soruyor.\""),
                Opened = Loc.N("\"Yaralılarınız var, değil mi? Bir gecelik yer ve bir tas çorba yeter.\""),
                Options = new[]
                {
                    O("ekip", Loc.N("Uzmanlara baktır"), new DoorEffect { Supplies = -6, Heal = 2, Text = Loc.N("Hekim gece boyunca ekiple uğraştı. Sabah yaralar sarılmıştı.") }),
                    O("sakin", Loc.N("Sakinlere ilaç al"), new DoorEffect { Fon = -5, Trust = 1, Text = Loc.N("Hekimin çantasındaki ilaçlar yaşam alanında dağıtıldı; çocuklara aşı yapıldı.") }),
                },
                Ignore = new DoorEffect { Text = Loc.N("Hekim bir süre bekledi, sonra bir sonraki sığınağa yürüdü.") },
            },
            new DoorCard
            {
                Id = "cocuklar", When = s => s.FreeRegions >= 1, Once = true, Weight = 6,
                Who = Loc.N("Yaşam alanından çocuklar"),
                Knock = Loc.N("Kapının alt kısmına küçük yumruklar. Kıkırdamalar."),
                Peek = Loc.N("Üç çocuk; birinin elinde katlanmış bir kâğıt."),
                Ask = Loc.N("\"Parola?\" Hep bir ağızdan: \"Yeediii!\""),
                Opened = Loc.N("Kâğıtta bir resim: güneş, bir ev, haritada maviye boyanmış bir bölge. \"Bunu panonuza asın!\""),
                Options = new[]
                {
                    O("as", Loc.N("Panoya as"), new DoorEffect { Trust = 1, Text = Loc.N("Resim mantar panoya iğnelendi. Çocuklar koşarak gitti.") }),
                },
                Ignore = new DoorEffect { Text = Loc.N("Resmi kapının altından içeri ittiler.") },
            },
            new DoorCard
            {
                Id = "amator", Weight = 8,
                When = s => (s.Shelter.Room(ShelterLogic.Radio)?.Level ?? 0) > 0 && s.S.Alarm >= 3,
                Who = Loc.N("Bir radyo amatörü"),
                Knock = Loc.N("Kısa, uzun, kısa: mors gibi. \"Komutan, elimde bir frekans çizelgesi var.\""),
                Peek = Loc.N("Kulaklıklı genç bir kadın; kucağında el yapımı bir alıcı. Nefesi camı buğuluyor."),
                Ask = Loc.N("\"Parola?\" \"Yedi. Telsizinizi her gece dinliyorum.\""),
                Opened = Loc.N("\"YZ'nin devriye frekanslarını çıkardım. Bir de şunu bilin: kopyalar insan sesiyle konuşuyor ama parolayı bilmiyor. Çizelgenin karşılığı biraz malzeme.\""),
                OnOpen = new DoorEffect { Flag = CopyWarning },
                Options = new[]
                {
                    O("al", Loc.N("Malzemeyle satın al"), new DoorEffect { Materials = -6, Alarm = -3,
                        Text = Loc.N("Çizelge telsiz odasına asıldı. YZ'nin devriyeleri bir süre yanlış yerde arayacak.") }),
                    O("ret", Loc.N("Reddet"), new DoorEffect { Text = Loc.N("\"Başka sığınaklar alır,\" diyor ve gidiyor.") }),
                },
                Ignore = new DoorEffect { Text = Loc.N("Mors vuruşları kesildi.") },
            },
            new DoorCard
            {
                Id = "aile", When = s => ShelterLogic.Capacity(s.Shelter, s.B) > s.Shelter.Population, Weight = 8,
                Who = Loc.N("Dışarıdan bir aile"),
                Knock = Loc.N("Çekingen vuruşlar, ardında bir bebek ağlıyor. \"Yer var mı? Kuzeyden yürüdük.\""),
                Peek = Loc.N("Bir kadın, bir adam, kucakta bebek. Sırtlarında bütün eşyaları; nefesleri camı buğuluyor."),
                Ask = Loc.N("\"Parola?\" \"Bilmiyoruz… Yolda biri yedi dedi. Yedi mi?\""),
                Opened = Loc.N("\"İki gündür yürüyoruz. Ne iş olsa yaparız.\""),
                Options = new[]
                {
                    O("al", Loc.N("İçeri al"), new DoorEffect { Population = 3, Text = Loc.N("Aile yaşam alanında bir köşeye yerleşti. Üç ağız daha, üç çift el daha.") }),
                    O("erzak", Loc.N("Erzak verip yolla"), new DoorEffect { Supplies = -6, Text = Loc.N("Aile erzağı alıp güneydeki sığınağa yürüdü.") }),
                    O("ret", Loc.N("Geri çevir"), new DoorEffect { Trust = -1, Text = Loc.N("Kadın bir şey demedi. Bebeğin ağlaması merdivende uzun süre duyuldu.") }),
                },
                Ignore = new DoorEffect { Trust = -1, Text = Loc.N("Ağlama sesi sabaha kadar koridorda yankılandı.") },
            },
            new DoorCard
            {
                Id = "zarf", Nobody = true, When = s => s.S.Alarm >= 5 || s.S.Counterattacks > 0, Weight = 6,
                Who = Loc.N("Kapının altından bir zarf"),
                Knock = Loc.N("Vuruş yok. Kapının altından bir zarf kayıyor."),
                Peek = Loc.N("Koridor boş. Lamba titriyor."),
                Ask = Loc.N("İnterkomdan sesleniyorsun. Hoparlörden bir saniye gecikmeyle kendi sesin dönüyor."),
                Opened = Loc.N("Zarfta tek satır, kusursuz harflerle: \"Teslim olursanız kimse incinmeyecek. Siz de bunu biliyorsunuz, komutan.\""),
                Options = new[]
                {
                    O("yak", Loc.N("Yak"), new DoorEffect { Text = Loc.N("Kâğıt kül oldu. Kokusu bütün gece odada kaldı.") }),
                    O("iz", Loc.N("Telsize ver, izini sür"), new DoorEffect { Alarm = -2, Text = Loc.N("Mürekkep bir devriye noktasını ele verdi. Telsizciler izleri karıştırdı.") }, NeedRadio),
                    O("as", Loc.N("Panoya as"), new DoorEffect { Trust = 1, Alarm = 2, Text = Loc.N("Herkes okudu; öfke korkudan ağır bastı. Ama YZ mesajın ulaştığını biliyor.") }),
                },
                Ignore = new DoorEffect { Text = Loc.N("Zarf sabaha kadar kapının altında kaldı, sonra kayboldu.") },
            },
            new DoorCard
            {
                Id = "ogretmen", When = s => s.Shelter.Population >= 8, Once = true, Weight = 6,
                Who = Loc.N("Bir öğretmen"),
                Knock = Loc.N("Düzenli ama yumuşak vuruşlar. \"Komutan, beş dakikanızı alabilir miyim?\""),
                Peek = Loc.N("Gözlüklü bir kadın, koltuğunun altında defterler."),
                Ask = Loc.N("\"Parola?\" \"Yedi. Çocuklara da öğrettim, kusura bakmayın.\""),
                Opened = Loc.N("\"Yaşam alanında çocuklar boş duruyor. Bir köşe, biraz ışık, biraz kâğıt istiyorum. Okul.\""),
                Options = new[]
                {
                    O("evet", Loc.N("Okul aç"), new DoorEffect { Materials = -6, Trust = 2, Text = Loc.N("Yaşam alanının köşesine bir karatahta asıldı. Akşamları alfabe sesleri geliyor.") }),
                    O("sonra", Loc.N("Şimdi değil"), new DoorEffect { Text = Loc.N("\"Anlıyorum,\" diyor. \"Ama çocuklar beklemez.\"") }),
                },
                Ignore = new DoorEffect { Text = Loc.N("Öğretmen kapının altından bir not itti: \"Okul için yine geleceğim.\"") },
            },
            new DoorCard
            {
                Id = "hirsiz", When = s => s.Shelter.Supplies >= 20 && s.Shelter.Population >= 7, Weight = 4,
                Who = Loc.N("Bir hırsız ve iki sakin"),
                Knock = Loc.N("İki kişi konuşuyor, biri itiraz ediyor. \"Komutan! Depodan erzak çalarken yakaladık!\""),
                Peek = Loc.N("İki sakin, aralarında kolundan tutulan bir genç. Gencin ceplerinden konserve kutuları taşıyor."),
                Ask = Loc.N("\"Parola?\" \"Yedi! Ne parolası komutan, hırsızı getirdik!\""),
                Opened = Loc.N("Genç başını kaldırmıyor. \"Kız kardeşim için aldım. Payı yetmiyor.\""),
                Options = new[]
                {
                    O("affet", Loc.N("Affet, erzak onun olsun"), new DoorEffect { Supplies = -4, Trust = 1,
                        Text = Loc.N("Sakinler söylendi ama gence kimse dokunmadı. Ertesi gün depoda gönüllü çalışıyordu.") }),
                    O("uyar", Loc.N("Erzağı geri al, uyar"), new DoorEffect { Text = Loc.N("Konserveler depoya döndü. Genç bir daha göze görünmedi.") }),
                    O("kov", Loc.N("Sığınaktan çıkar"), new DoorEffect { Population = -1, Trust = -1, Text = Loc.N("Gencin arkasından kapı kapandı. Kız kardeşi iki gün ağladı.") }),
                },
                Ignore = new DoorEffect { Supplies = -3, Text = Loc.N("Kapıyı açmadın; sakinler genci salıverdi. Konserveler de onunla gitti.") },
            },
        };

        static readonly Dictionary<string, DoorCard> byId = All.ToDictionary(c => c.Id);

        public static DoorCard Get(string id) => byId[id];
        public static bool Exists(string id) => id != null && byId.ContainsKey(id);

        static DoorOption O(string id, string label, DoorEffect effect, Func<Simulation, string> need = null) =>
            new DoorOption { Id = id, Label = label, Effect = _ => effect, Need = need };

        static string NeedInfirmary(Simulation s) => s.RoomStaffed(ShelterLogic.Infirmary) ? null : Loc.T("Revirde görevli yok.");
        static string NeedRadio(Simulation s) => s.RoomStaffed(ShelterLogic.Radio) ? null : Loc.T("Telsiz odasında görevli yok.");

        /// <summary>Yas tutan babaya sessiz kalmak: güven düşer; güven zaten düşükse adam birkaç gün sonra silahla döner.</summary>
        static Func<Simulation, DoorEffect> Silence(string text) => s => s.S.Trust <= 10
            ? new DoorEffect { Trust = -1, Flag = GriefUnanswered, Chain = new[] { "silahli_baba" }, ChainMin = 3, ChainMax = 5, Text = text }
            : new DoorEffect { Trust = -1, Text = text };
    }
}
