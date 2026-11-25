using System.Collections.Generic;

namespace Corrigible.Desk
{
    /// <summary>
    /// Kartlı panonun bir anlık görüntüsü (Revizyon 3 · R15/R16): panoda görünen her şey, yerelleştirilmiş ve biçimlenmiş. Köprü
    /// (<c>PlanningBoardSnapshotBuilder</c>) simülasyondan kurar; görünüm (<see cref="BoardView3D"/>) yalnız bunu okur. İçinde Core
    /// nesnesi yoktur; kurulması RNG çekmez, komut yazmaz.
    /// </summary>
    public sealed class BoardSnapshot
    {
        public bool English;
        /// <summary>Tebeşir başlık: "V.001 · erzak 24 (−2,1) · 8/10 kişi".</summary>
        public string Header;
        public bool Locked;
        /// <summary>Kilit nedeni (etiketin 2. satırı); açıkken boş.</summary>
        public string LockReason;
        /// <summary>Kartlar sürüklenebilir mi (plan açık, girdi kilidi yok, hikâye beklemiyor).</summary>
        public bool Editable;
        /// <summary>Pano erişimi üzerine gelme satırı: "PLAN AÇIK · 8/10 kişi".</summary>
        public string EntryLine;
        /// <summary>Hurda üzerine gelme satırı: "1 eşya → 1 metal".</summary>
        public string HurdaLine;
        /// <summary>Hurda plakasının 2. satırı (dinamik; denge değerinden).</summary>
        public string HurdaPlateLine;
        /// <summary>
        /// GÖZETİM cebi açık (tesis aşama 5, <c>BoardDropRules.WatchOpen</c>; oyunda hep): plaka, pirinç bant ve şeritler görünür, cep bırakma ve
        /// klavye hedefidir. Kapalıyken o köşe boştur ve BOŞTA sayılır.
        /// </summary>
        public bool WatchOpen;

        public readonly List<PersonCardData> People = new List<PersonCardData>();
        /// <summary>Cepteki kişi: görev (tek, emp, ana, craft, cook, train; GÖZETİM açıkken watch) → kişi kimliği.</summary>
        public readonly Dictionary<string, string> PocketOccupant = new Dictionary<string, string>();
        /// <summary>Boşta destesi, üstten alta (0 = üst kart = ilk uygun kişi).</summary>
        public readonly List<string> DeckOrder = new List<string>();
        public readonly List<ItemCardData> Items = new List<ItemCardData>();
        /// <summary>Dört hammadde, sırayla metal, devre, kumaş, kimyasal.</summary>
        public readonly MaterialCardData[] Materials = new MaterialCardData[4];
        /// <summary>Altı kit yığını, sırayla kit_tek, kit_emp, kit_ana, armor, medkit, emp_cell.</summary>
        public readonly KitStackData[] Kits = new KitStackData[6];
        /// <summary>
        /// Tesis aşama 9: yedi ekipman yığını (<see cref="BoardLayout.GearKinds"/> sırası; depodaki ikinci sıra). Yalnız türün depoda
        /// kartı varken dolu; öbür yuvalar null (boş yığın gölgesi yok). Tesis durumu yokken hepsi null.
        /// </summary>
        public readonly KitStackData[] Gear = new KitStackData[7];
        public readonly List<ChipData> Chips = new List<ChipData>();
        /// <summary>Seçili kişinin dosyası; seçim yoksa null.</summary>
        public FolderData Folder;
        /// <summary>Dosya kapağının 2. satırı (seçili kişinin adı büyük harfle ya da "— kart seç —").</summary>
        public string FolderCoverLine;
        public readonly List<LostEntry> Lost = new List<LostEntry>();
        /// <summary>KAYIPLAR zarfının yazısı ("KAYIPLAR · 2"); kayıp yoksa boş.</summary>
        public string LostCover;
        public string SelectedId;
        /// <summary>Kartların sabit sözcükleri (bu dilde).</summary>
        public BoardWords Words = new BoardWords();

        // ------------------------------------------------------------------ tarif defteri ve üretim (Aşama 5 · R18, PLAN §3 W0)

        /// <summary>Defterin altı sayfası, Core sırasıyla: armor, medkit, emp_cell, kit_tek, kit_emp, kit_ana (denge değerlerinden).</summary>
        public readonly List<RecipeData> Recipes = new List<RecipeData>();
        /// <summary>ÜRETİM · İŞ EMRİ formunun durumu ve TARİF kutusundaki kart.</summary>
        public OrderData Order = new OrderData();
        /// <summary>Son kapanışta depoya gelen yeni ürün (kayış ve YENİ etiketi); yoksa null.</summary>
        public NewItemData NewItem;
        /// <summary>Defter sayfalarının sabit sözcükleri (bu dilde).</summary>
        public BookWords Book = new BookWords();
        /// <summary>Kapalı defterin üzerine gelme etiketi: başlık ("TARİF DEFTERİ") ve satır ("6 tarif").</summary>
        public string BookTitle, BookLine;
        /// <summary>[B] Tarifler defterden iş emrine sürüklenebilir mi (<see cref="Editable"/>); [A] hep false.</summary>
        public bool RecipesDraggable;

        public PersonCardData Person(string id)
        {
            foreach (var p in People) if (p.Id == id) return p;
            return null;
        }

        public ItemCardData Item(string id)
        {
            foreach (var i in Items) if (i.Id == id) return i;
            return null;
        }
    }

    /// <summary>Kart yüzlerinin dile bağlı sabit sözcükleri.</summary>
    public sealed class BoardWords
    {
        public string Injured = "YARALI", Bedridden = "YATIYOR", Restricted = "KISITLI", Lost = "KAYIP";
        /// <summary>Tecrit köşesinin sözcüğü (KOS aşama 4; kısıt köşesini kullanır).</summary>
        public string Isolated = "TECRİT";
        public string Free = "serbest";
        public string Kit = "KİT", Armor = "ZIRH", Aid = "YARDIMCI";
    }

    public sealed class PersonCardData
    {
        public string Id, Name, Job, PortraitKey;
        /// <summary>Core görevi: tek, emp, ana, craft, cook, train, watch (yalnız GÖZETİM açıkken), rest (emekli görevler rest sayılır).</summary>
        public string JobId;
        /// <summary>Yedi konu harfi (TR: T E A Ü M B İ · EN: T E A P K C F) ve kademe 0–3.</summary>
        public readonly string[] MasteryLetters = new string[7];
        public readonly int[] MasteryTiers = new int[7];
        /// <summary>CAN/HAR/GÜÇ etiketleri ve değerleri ("2/3", "3", "1").</summary>
        public readonly string[] StatLabels = new string[3];
        public readonly string[] StatValues = new string[3];
        public bool Hurt;
        public string Strip;
        public StripTone StripTone;
        public bool Injured, Bedridden, Restricted, Lost;
        /// <summary>Tecritte (KOS aşama 4): kart kısıt köşesini <see cref="BoardWords.Isolated"/> sözcüğüyle taşır.</summary>
        public bool Isolated;
        public bool Selected;
        public string HoverTitle, HoverLine;
    }

    public sealed class ItemCardData
    {
        public string Id, Kind, Name, SlotLabel, UseLine;
        public ItemSlotKind Slot;
        /// <summary>Tesis aşama 9: dinlenen köpeğin kalan vardiyası (<c>BoardEquipment.Rest</c>); 0 = dinlenmiyor.</summary>
        public int Rest;
        /// <summary>Tesis aşama 9 (K19): depodaki ekipman yığınının üst kartında yığının kart sayısı (yüzde "×n", yalnız n &gt; 1); öbürlerinde 0.</summary>
        public int Count;
        /// <summary>Sahibi (kişi kimliği); depodaysa null.</summary>
        public string OwnerId;
        public string HoverTitle, HoverLine;
    }

    public sealed class MaterialCardData
    {
        public string Kind, Name;
        public int Stock, Free, Held;
        /// <summary>"stok n" ve "ayrı n" (ayrılan yoksa ikincisi soluk).</summary>
        public string StockLine, HeldLine;
        public string HoverTitle, HoverLine;
    }

    public sealed class KitStackData
    {
        public string Kind;
        public int Count;
        /// <summary>Çizilen en çok üç kart, alttan üste (en yeni üstte).</summary>
        public readonly List<string> TopIds = new List<string>();
        public string CountText;
    }

    public sealed class ChipData
    {
        /// <summary>"chip:metal:0" biçiminde; aynı türün sırası.</summary>
        public string Id, Kind, Name;
        public int Index;
    }

    /// <summary>Kişi dosyası: kapak ve iki sayfa satırları (eski kabulün sekmeleri ve künyesi aşama 7'de söküldü).</summary>
    public sealed class FolderData
    {
        public string PersonId, Name, PortraitKey;
        public PersonCardData Card;
        public string Profession, Trait, Job, Header;
        /// <summary>Sağ sayfanın üst satırı (soldaki <see cref="Header"/> hizasında): "KALICI KİŞİ DOSYASI". Çevrilmiş gelir.</summary>
        public string RightHeader;
        public readonly List<FolderLine> Mastery = new List<FolderLine>();
        public string[] Gear = new string[3];
        public readonly List<FolderLine> DossierRight = new List<FolderLine>();
        public readonly List<FolderLine> Left = new List<FolderLine>();
        public readonly List<FolderLine> Right = new List<FolderLine>();
        /// <summary>
        /// Dosyanın kıskaçlı etiketi (Aşama 6, R17f; <see cref="FileTagView"/>): "KISITLI · n V", "TECRİT", "İZ n" vb.; null = etiket yok.
        /// Çevrilmiş gelir.
        /// </summary>
        public string TagText;
    }

    public readonly struct FolderLine
    {
        public readonly string Label, Value;
        public readonly StripTone Tone;
        /// <summary>Bölüm başlığı (kırmızı çizgili etiket).</summary>
        public readonly bool Section;

        public FolderLine(string label, string value, StripTone tone = StripTone.Ink, bool section = false)
        {
            Label = label; Value = value; Tone = tone; Section = section;
        }
    }

    public sealed class LostEntry
    {
        public string Id, Name, Line;
    }

    // ---------------------------------------------------------------------- tarif defteri ve üretim (Aşama 5 · R18)

    /// <summary>
    /// Defterin bir sayfası (bir tarif; PLAN §1.5). <see cref="Index"/> 0–5: açılım <c>Index / 2</c>, sayfa <c>Index % 2</c>
    /// (0 sol, 1 sağ). Metinler çevrilmiş ve biçimlenmiştir; görünüm <c>Loc</c> çağırmaz.
    /// </summary>
    public sealed class RecipeData
    {
        /// <summary>Core tarif kimliği (armor, medkit, emp_cell, kit_tek, kit_emp, kit_ana).</summary>
        public string Id;
        /// <summary>Tarifin adı (cümle düzeni) ve sayfa başlığı (büyük harf, <c>DeskText.Upper</c>).</summary>
        public string Name, Title;
        /// <summary>Tek kip: tarif kartının kısa adı (<see cref="RecipeCardData.ShortName"/>); yoksa null.</summary>
        public string ShortName;
        /// <summary>Sayfa künyesinin sağ yarısı ("TARİF 01").</summary>
        public string Number;
        /// <summary>Çıktının yuva sözcüğü ("zırh yuvası", "yardımcı yuvası", "kit yuvası").</summary>
        public string SlotWord;
        /// <summary>Çıktı satırının sayısı ("1 ×").</summary>
        public string OutLine;
        /// <summary>Süre ("1 vardiya", "2 vardiya").</summary>
        public string TurnsLine;
        /// <summary>El notu (kurşun kalem); yoksa boş.</summary>
        public string Note;
        /// <summary>Sayfa altı ("s. 3") ve açılım ("AÇILIM 2/3"; yalnız sağ sayfada çizilir).</summary>
        public string PageLabel, SpreadLabel;
        public int Turns, Index;
        /// <summary>
        /// Girdiler, Core sırasıyla (baştan açık tariflerde iki; tesis aşama 9: yemde üç). Erzak bedeli olan tarifte (köpek) son satır
        /// erzaktır (<see cref="RecipeInput.Supplies"/>).
        /// </summary>
        public readonly List<RecipeInput> Inputs = new List<RecipeInput>();
        /// <summary>Tesis aşama 9: tarif sunucu ödülüyle açıldı (<c>BoardRecipe.Reward</c>).</summary>
        public bool Reward;
        /// <summary>Tesis aşama 9: tarifi veren sunucunun YZ adı (ödül sayfası ve kapalı sayfa); yoksa boş.</summary>
        public string Source;
        /// <summary>Tesis aşama 9: erzak bedeli (köpek); yoksa 0.</summary>
        public int Supplies;
        /// <summary>
        /// Tesis aşama 9 (K22): kapalı sayfa (henüz açılmamış temel ekipman tarifi). Kutu yerine <see cref="LockedStamp"/> çizilir, tarif
        /// kutusu sürüklenmez (<see cref="BookZone.Page"/>); girdiler boş.
        /// </summary>
        public bool Locked;
        /// <summary>Kapalı sayfanın damgası: "KAPALI · VERITY sunucusundan" (çevrilmiş).</summary>
        public string LockedStamp;
        /// <summary>Tesis aşama 10 (K45): sayfa cevap tarifine dönüştü (Core <c>BoardRecipe.Answer</c>: sokum, delici, celik_tel,
        /// yedek_role, cift_zemberek); temel sayfada null. Kimlik ve dizin temelinkidir; ad, girdi ve el notu cevabın.</summary>
        public string Answer;
    }

    /// <summary>Tarifin bir girdisi: hammadde türü, adı, defterdeki ("× 2") ve karttaki ("×2") miktar yazısı.</summary>
    public sealed class RecipeInput
    {
        public string Kind, Name, BookQty, CardQty;
        public int Quantity;
        /// <summary>Tesis aşama 9: erzak bedeli satırı (hammadde değil; fiş ve ayırma yok; simgesi <c>icon_erzak</c>).</summary>
        public bool Supplies;
    }

    /// <summary>
    /// 112 × 80 mm tarif kartı (PLAN §1.6): defterden alınan kopya ya da iş emrinin TARİF kutusundaki kart. <see cref="Id"/> kartın
    /// kimliğidir (formdaki kartta iş emri kimliği, kopyada tarif kimliği); <see cref="Done"/> &lt; 0 ise ilerleme kutusu çizilmez.
    /// </summary>
    public sealed class RecipeCardData
    {
        public string Id, RecipeId, Name, IconKey, TurnsLine, OutLine, NeedsLabel;
        /// <summary>Tek kip: kısa kart adı (<c>PlanningBoardData.CardName</c>); tam ad yüzün ad alanına sığmazsa yazılır. Yoksa null.</summary>
        public string ShortName;
        public readonly List<RecipeInput> Inputs = new List<RecipeInput>();
        public int Turns;
        /// <summary>Biten vardiya sayısı ([B] başlamış emir); &lt; 0: kutu yok (tek vardiyalık tarif ya da [A]).</summary>
        public int Done = -1;
    }

    /// <summary>İş emri formunun durumu (PLAN §1.7): boş, tarif konmuş, girdisi eksik, tam, başlamış ([B]), durmuş (DURDU).</summary>
    public enum OrderState { Empty, Recipe, Missing, Full, Started, Stopped }

    /// <summary>ÜRETİM · İŞ EMRİ formunun dinamik yazıları ve TARİF kutusundaki kart.</summary>
    public sealed class OrderData
    {
        public OrderState State;
        /// <summary>"No 031" (vardiya numarası).</summary>
        public string Number;
        /// <summary>[B] Core iş emri kimliği (is-N); emir yoksa null.</summary>
        public string OrderId;
        public string RecipeId;
        /// <summary>Durum satırı (mavi kalem): "eksik: 1 kumaş", "zırh plakası · girdiler ayrıldı · üretici: Suna" …</summary>
        public string Status;
        /// <summary>Damga sözcüğü ("DURDU"); damga yoksa boş.</summary>
        public string Stamp;
        /// <summary>
        /// Girdi alanının basılı ipucu (R18c, D-F: "GİRDİLER (hammaddeyi buraya bırak)"); yalnız düzenlenebilir ve girdisi eksik taslakta
        /// dolu (<see cref="OrderState.Recipe"/>, <see cref="OrderState.Missing"/>), öteki durumlarda boş. Çevrilmiş gelir.
        /// </summary>
        public string DropHint;
        /// <summary>Durma nedeni ("üretici yok", "açlık", "eşya deposu dolu"); durmuyorsa boş.</summary>
        public string StopReason;
        public bool Started;
        public int Turns, Done;
        /// <summary>TARİF kutusundaki kart; null = kutu boş.</summary>
        public RecipeCardData Card;
        /// <summary>[B] Formdaki kart tutulabilir mi (<see cref="BoardSnapshot.Editable"/> ve kart var).</summary>
        public bool CardDraggable;
    }

    /// <summary>
    /// Son kapanışta depoya gelen yeni ürün (PLAN §1.11): eşya kimliği (esya-N), türü, kit yığını (0–5, <see cref="BoardSnapshot.Kits"/>
    /// sırası; tesis aşama 9: ekipmanda <see cref="Gear"/> ve <see cref="BoardSnapshot.Gear"/> dizini) ve günü (S.Day).
    /// </summary>
    public sealed class NewItemData
    {
        public string ItemId, Kind;
        public int Stack, Day;
        /// <summary>Tesis aşama 9: <see cref="Stack"/> ekipman sırasının dizini (<see cref="BoardSnapshot.Gear"/>), değilse kit sırası.</summary>
        public bool Gear;
    }

    /// <summary>
    /// Defter sayfalarının dile bağlı sabit sözcükleri. Alan ilkleyicisi yoktur (AGENTS kural 9): kurucu her birini <c>Loc.T</c> ile
    /// doldurur. <see cref="Header"/> künyenin sol yarısı ("KYGA · TARİF DEFTERİ"); <see cref="Tab"/> sayfa kenarındaki İŞ EMRİ sekmesi.
    /// </summary>
    public sealed class BookWords
    {
        public string Header, Output, Input, Time, Next, Prev, Tab;
    }
}
