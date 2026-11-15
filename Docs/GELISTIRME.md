# Geliştirme

## Ortam

- Unity **6000.3.12f1** (proje: bu deponun kökü). Editor açık olmalı; ajanlar onu `unity` CLI + Pipeline paketiyle
  (`com.unity.pipeline`) sürer. Editor derleme hatasıyla Safe Mode'a düşerse CLI bağlanamaz: hatayı dosyadan düzelt.
- Yardımcılar: `source Tools/unity-helpers.sh` → `ueval`, `ucompile`, `utest`, `uptest`, `uplay`, `ustop`, `ushot`, `uerrors`.
  Yardımcılar `--project-path` ile Corrigible'a bağlanır; başka bir Editor açıkken doğrudan CLI çağrılarında da
  `--project-path /Users/selimkamyan/Gits/Corrigible` belirt. Çalışma dizini tek başına doğru Editor'ü seçmeyebilir.

## Döngü

1. Kodu değiştir.
2. `ucompile` → "HATA YOK" görmeden devam etme. (`recompile_status` hata bildirmez; konsolu zaman damgasıyla oku.
   Konsolda eski derlemelerden kalma hatalar durur; yalnızca son derlemeden sonrakilere bak.)
3. Testler: önce **boş sahne aç** (aşağıda "Testler açık sahneyi kirletir"), sonra `utest`. Domain reload sırasında
   `run_tests` zaman aşımına uğrar; `utest` yeniden dener. Tam takım birkaç dakika sürer; doğrudan CLI'da
   `--timeout 900` ver ve arka planda çalıştır.
   `utest` yalnız EditMode'u koşar (`--mode editor`); PlayMode testleri `uptest` ile ayrı koşar (aşağıda "PlayMode
   testleri").
4. Oyunda dene (aşağıda). Görsel değişiklikte ekran görüntüsünü gerçekten aç ve bak.
5. Play'deysen `ustop`. Kayıt dosyalarını geri yükle, geçici görüntüleri sil (`Temp/*.png`). Dinamik font asset'lerindeki
   (`* Dyn SDF.asset`) oyun sırasında oluşan değişiklikleri `git checkout` ile geri al. Commit'i kullanıcı isterse yap.

```bash
ueval 'UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single); return "boş";'
```

## Testler

| Dosya | Ne kilitler |
|---|---|
| `Tests/EditMode/LiberationCampaignTests` | açılış, sektör sırası, ödüller, sınır seçimi, bütün harekâtın kazanılabilmesi, makinelerin kopyalanması, sivil güven (kayıp, ayakta kalan sivil ödülü, tavan), kayıt/devam |
| `Tests/EditMode/DoorTests` | sığınağın kapısı: ilk vuruş, aç/açma ve seçim, cevapsız ziyaretçi, vuruş aralığı ve ana RNG'ye dokunmama, kopya ve silahlı sakinin koşulları, bedeller, kart tablosunun bütünlüğü, kayıttan devam |
| `Tests/EditMode/PlanningBoardTests` | isimli kadro, tek kişi/tek iş, kit/eşya sahipliği, üretim girdisinin bir kez ödenmesi, eğitim, fiş, kalıcı kayıp, hurdalama, ikmalden kit toparlama ve kayıt/devam |
| `Desk/Tests/EditMode/PlanningBoardSceneTests` | Revizyon 3 kartlı pano sahnede: bileşenler ve bağları, plakalar ve yazılar, eski tuvalin, rafın ve `Shelter controller`'ın yokluğu (Aşama 10), eski kabul penceresinin yokluğu ve kabul istasyonunun bağları, şalter (kapak, kol)/kâğıt/EVRAK kutusu, karartma ve eksik script yokluğu |
| `Desk/Tests/EditMode/Rev3BoardLayoutTests`, `Rev3BoardSnapshotTests`, `Rev3BoardDropRulesTests` | pano mm ↔ model yereli, cepler, deste aralığı min(82, 636/(n−1)); anlık görüntünün Core'dan kurulması (RNG/komut yok); bırakmanın tek Core komutuna çevrilmesi ve önizleme metni |
| `Desk/Tests/EditMode/Rev3CardFaceTests` | dört kart prefab'ı (model, çarpıştırıcı, yüz tuvali, iç içe modelde geçersiz kılma yok), kişi yüzünün yerleşimi ve durum katmanları (eski İNCELE bayrağı aşama 7'de söküldü: overlay'de alanı yok, modelin `Flag` parçası kurulumda gizli), TR/EN taşma geçidi (meslekler, şeritler, eşya kullanım satırı ≤ 3 satır) |
| `Tests/EditMode/ShelterTests` | eski model (`planningBoardEnabled = false`): sığınak tüketimi, plaka dökümü, revir, yedekler, telsiz, depo sınırları |
| `Tests/EditMode/RaidEngineTests` | mulberry32, aynı hamlelerle aynı savaş (beklenen imzalar, bütün ekip kaçışla çıkar), geri alma, önizleme, ikmal görevi, kaçış (son çıkan kazandırır, süre dolarken kalan düşmüş sayılır, sunucu kaçışı, geri çekilme yok), sunucu sertliği (düğüm canı, ek makine; kasa ve çekirdek hariç) |
| `Tests/EditMode/RaidMapsTests` | harekâtın harita havuzu: aynı tohum aynı havuz, harekâtlar harita paylaşmaz, üretilen haritaların kuralları, altı türden eşit sayıda ve her harita türünün kurallarına uyar, torba (her harita bir kez, art arda tekrar yok), havuz yalnız `MapSeed` ile |
| `Desk/Tests/EditMode/SiginakDoorTests` | kapı kâğıdının evreleri (bak, sor, aç, açma; bedel ve gerekçe; sonuç ve damga), son raporun bekletilmesi, vardiya raporunun kapı satırı, sahne bağlantıları ve kapı pozu |
| `Desk/Tests/EditMode/TutorialFlowTests` | eğitim adımlarının sırası, geçilen adımlar, raporlar, atlama, sahne bağlantıları |
| `Desk/Tests/EditMode/TutorialRaidTests` | senaryolu ilk baskın: her harekât tohumunda aynı tahta, rehberin adımları hasarsız zafere varır |
| `Tests/EditMode/RegionIdentityTests` | bölge özellikleri ve imza makineleri, zorluk kademesi, yeni makinelerin niyetleri, oda seviyesi kilidi |
| `Desk/Tests/EditMode/SiginakSoftEdgeTests` | eğik kâğıtlarda kenar: yumuşak türevler (`*_aa`) kaynağın 1 px saydam halkalı kopyası, çizgi sprite'ları, DeskGame ve baskın panellerinde eğik sert kenarlı görsel yok |
| `Tests/EditMode/RaidEventTests` | bölge olayları: her bölgenin kendi olayı, her haritada yer bulma ve çıkış güvencesi, RNG'nin aynı kalması, imza eki, geri alma, her olayın kuralı iki tarafa da, hasar tavanı, nesne muafiyeti, önizleme (`Hazards`, tur şeridi) |
| `Tests/EditMode/LocalizationTests` | çeviri tablosu tam; İngilizce oynanan harekâtta Türkçe metin yok; dil sonucu değiştirmez |
| `Desk/Tests/EditMode/DeskBridgeTests` | DeskGame bağlantıları, ekran katmanlarının sırası, operasyon raporunun yeri ve güven satırı, harekât sonu raporu (damga, gösterge, 29 ve 48 vardiyada tam genişlik şerit, kırılma anları; dil değişince de) |
| `Desk/Tests/EditMode/LiberationReportTests` | operasyon raporu (S7): KAZANÇ ve fişler, başarısızda ödül alanı yok, aynı söz iki kez geçmez, depo sınırı, kayıttan aynı rapor |
| `Desk/Tests/EditMode/Siginak*Tests` | oda, kamera, harita, sığınak, CRT, vardiya, emir belgesi, punto taraması (oda + baskın) |
| `Desk/Tests/PlayMode/BoardCameraPlayTests` | (PlayMode) SIĞINAK tuşu K4'e 0,6 s, kenar satırı ilk kareden gizli; MASAYA DÖN / Esc / sağ tık 0,5 s'de masaya; 40 px sağ sürükleme bakış (3,6°), geri değil; açık dosya Esc'i önce tüketir; hiçbiri komut yazmaz |
| `Desk/Tests/PlayMode/ShiftClosePlayTests` | (PlayMode, 14) şalter → fiş → EVRAK: kapak 0,25 s / kol 0,3 s / tam bir `board_print`; fiş kol inince 0,35 s'de dile; kapak 4 s'de kapanır; el (%67) ve okumak zaman ilerletmez; EVRAK dışı ve reddedilen bırakış dile döner; EVRAK'ta tam bir gün (kararma, K1, VARDİYA, teleks); aynı fiş yeniden kullanılmaz; geçersiz plan ve baskın kilidi; basım reddi; kayıttan devam; Esc/sağ tık; arşiv okuma |
| `Desk/Tests/PlayMode/MapPaperPlayTests` | (PlayMode, 4) görev fişi ve kadro raporunun TR/EN düğmeleri kadrajda; kişi satırına gerçek ışınla tık panoda aynı kişiyi seçer; uzun dosyanın sayfa çarpıştırıcısı çalışır; CRT ışığı/kapanması kayıt ve RNG değiştirmez |
| `Desk/Tests/EditMode/Rev3Stage7PaperTests` | (10) isimli saha kadrosu, eşya sahipliği, gerçek istatistikler; eksik kit ve yatan kişi; kapasite sınırındaki gerçek baskın ganimeti, kayıt/devam ve okuma saflığı; isimli kurtuluş/kayıp; TR/EN uzun kişi dosyasında sayfalama |
| `Desk/Tests/PlayMode/CandidatePlayTests` | (PlayMode, 10) aday grubu, tohum 79 gerçek DeskGame: grup canlı kapanışta gelir (tek vuruş, teleks, 2–3 kart + grup fişi, emekli araçlar ve beş etiket sahnede yok (aşama 7)); KABUL karta → `candidate_choose` ve kişi Boşta'da; GERİ ÇEVİR fişe → `candidate_pass`; dolu sığınakta işaretsiz KABUL kilitli, satır tıkı işaretler, tek komutla takas ve yolluk; iki vardiya kararsız → fiş uyarısı, grup gider, tezgâh katlanır; fiş basılıyken damga komut yazmaz; kayıttan devam aynı kartlar, vuruşsuz; EN'de Türkçe harf yok; K8A'da karta ve fişe gerçek ışın (araya yalnız `SM_Door` girerse günlüğe yazar, sentetik yola düşer) |
| `Tests/EditMode/CandidateGroupTests` | (KOS, 43) aday grubu: geliş aralığı ve ilk grup, 2–3 aday, bekleme ve süre aşımı, kademe payları ve kaydırma, özellikler ve dönüşümleri, kişiye bağlı teçhizat ve taksit, dolu sığınakta `replaceId` ve yolluk (`DismissSuppliesPreview`), acil grup, kilitli planda seçim, adlar, altın zaman çizelgesi (tohum 79, ilk üç grup), kit kilidi, kayıttan devam |
| `Tests/EditMode/ShelterEventTests`, `ShelterEventCaseTests` (yardımcı `ShelterEventKit`) | (KOS, 31 + 27) olaylar: çekim sırası (altın zaman çizelgesi, tohum 79; kapanış başına 32/2/0 çekim), aralık, öncelik, tekrar sınırı, hedefi giden olay, sızma zinciri ve tecrit, kilitsizlik, gizlilik, komut koşulları, kayıttan devam (60 tohum, on dört olayın hepsi); tek tek her olayın tetiği ve seçenekleri, açlık ve ayrılma, tayın kipi, karartma, kişi durumları. `ShelterEventKit.Quiet/On/Robots/Humans` olayları ve kimlikleri test için ayarlar |
| `Tests/EditMode/EndingTests` | (KOS, 30) ifşa (kaynaklar, eşik, bir ve iki uyarı vardiyası, sorgu), `dusus`, `terk` (eksik kadro sayacı ve acil grup), son şalter ve iki çeşidi (nedenler `konum`, `kadro`, `aclik`, `guven`), zaferin tonu (ölüm sınırının ölçeklenmesi, yalnız görmezden gelinmiş izin gölgelemesi), `EndReport`, öncelik, gizlilik, kayıt |
| `Desk/Tests/EditMode/Rev3CandidatePaperTests`, `Rev3CandidateViewTests` | aday kartı ve grup fişi prefab'ları (satırlar, seçili satır, GERİ ÇEVİR izi), tezgâhtaki kart düzeni ve çekmece |
| `Desk/Tests/EditMode/Rev3ShelterEventSnapshotTests`, `Rev3ShelterEventSceneTests` | olay tutanağının anlık görüntüsü Core'dan (14 tür TR/EN, kapalı seçenek ve nedeni, GÜVENLİ/EŞİK etiketi, kişiye bağlı kit satırı), sahnede tutanak, tepsi ve bağlar |
| `Desk/Tests/EditMode/Rev3ExposurePanelTests`, `Rev3LastSwitchTests`, `KosFinalReportTests`, `Rev3KosStage6SceneTests` | İFŞA göstergesi (lambalar, kalan uyarı vardiyası, CRT önceliği, fişin İFŞA ve SIĞINAK EKSİK kutuları); son şalterin kilidi, plakası ve ÇEKİLME EMRİ (TR/EN); son raporun yeni sonları, ton ve ölçüt kutusu, SİCİL, başarım eşlemesi, Core kimliklerinin ham yazılmaması; sahnede şalter, gösterge ve rapor yuvaları |
| `Desk/Tests/EditMode/KosStage8SceneTests` | (KOS aşama 8, ikinci tur) telsizin yeri, kordonsuz LOD'lar, CRT payı, kapı kanadının süpürmesi |
| `Desk/Tests/PlayMode/ShelterEventPlayTests`, `KosEndingsPlayTests` | (PlayMode, 9 + 6) gerçek DeskGame'de tutanak: tepsi, ele alma, satır ve KARAR, Esc, K6'dan tık, hedefi giden olayın kapanması, 14 tür hata ayıklama kurulumuyla; sonlar: ifşa göstergesi ve uyarı, son şalterin iki adımı ve damga (`last_switch`), son raporlar |
| `Desk/Tests/EditMode/Rev3ShiftSwitchTests`, `Rev3SlipMotionTests`, `Rev3ShiftCloseFlowTests` | şalterin R19 durumları ve süreleri (token), plaka metni/rengi TR/EN, eski kip aynen; fiş pozları (dil, el, tepsi), çıkış ease-out ve 160 mm, ret dönüşü, EVRAK ışığı; kilit nedeni önceliği, ses eşlemesi, dosyalama zaman çizelgesi, arşiv gidiş-dönüşü |
| `Desk/Tests/EditMode/Rev3HandoverSlipFaceTests`, `Rev3HandoverSlipFactsTests`, `Tests/EditMode/ShiftCloseCoreTests` | fiş biçimi R19c V.001/V.031 TR/EN, R10 düzeni mm, en kötü fiş taşmasız, prefab (ışıksız TMP, çarpıştırıcısız); fiş verisinin saflığı ve kapanışla eşleşmesi; Core: basılıyken Step ilerlemez, dünkü fiş reddi, replay |
| `Desk/Tests/EditMode/Rev3ShiftCloseSceneTests` | sahnede kapanış bağları (model parçaları, SlipMouth çapaları, EVRAK ışığı, K6 kadrajı), plaka iki satırı TR/EN sığar, token ↔ tablo metinleri, ad araması yok |
| `Desk/Tests/EditMode/Rev3Stage6ContractTests`, `Rev3CandidateSnapshotTests` | Aşama 6 sözleşmeleri (poz 8–10, cue 120–122/127–128 ve 123–126 boş, damga kimlikleri ↔ Core, `SlipWarningKind` sayıları, `_local_a6` token yolları, K8 ailesinin poz ışığı); aday grubunun anlık görüntüsü Core'dan (kartlar, fiş, dolu sığınak, damga kilitleri, gizli kimlik sızmaz, TR/EN) |
| `Desk/Tests/EditMode/Rev3DoorTests`, `Rev3AdmissionMotionTests` | kapı: iç saatli vuruş (0/0,3/0,6 s, 0,9 s), süreli açılış (çark 0,5, kanat 0,8, merdiven 0,3, kapanış 0,6), kartlı oyunda kırmızı yok, kabule devir, eski yol aynen; istasyon: tezgâh T1–T5, çekmece, aday kartları ve grup fişi, karar dizileri (KABUL, GERİ ÇEVİR), damga (kalkış, basış, dönüş, kilit, tık-tık), okuma pozu, çarpıştırıcılar |
| `Desk/Tests/EditMode/Rev3AdmissionPaperTests`, `Rev3AdmissionFlowTests`, `Rev3AdmissionSceneTests` | kâğıt prefab kuralları (aday kartı, grup fişi; ışıksız mürekkep, FileTag sığması, kâğıt malzemeleri; buğu dokusu aşama 7'de söküldü); akış (her damga tam bir komut, kilitte komut yok, vuruş grup başına bir kez, geri zinciri, KABUL'de kapı, işaret yan dosyaya yazılmaz, teleks, ad araması yok); sahnede istasyon bağları, model yerleri (damgalar orta yuvalarda), emekli nesnelerin ve eksik script'in yokluğu, çarpıştırıcılar ve gerçek ışınlar, K8A/K8R pozları, plakalar TR/EN |
| `Desk/Tests/EditMode/SadeRaid*Tests`, `LootRaidPresentationTests` | baskın HUD komutları, kamera izdüşümü, Baskin sahnesi bağlantıları |
| `Desk/Tests/EditMode/DeskAssetTests`, `CommandPostProjectionTests` | sprite/font/tema varlıkları, dünya-ekran izdüşümü |
| `Desk/Tests/EditMode/Rev3RoomTests` | Revizyon 3 odası: sayaç tamburu rakam başına 36°, tebeşir çetelesinin hücreleri, teleks kâğıdının dört yuvası, telsiz defterinin sayfası, poz ışığı tarifi (her pozda en çok 4 gölgeli ışık, K1'de 3, K4'te yalnız pano lambası), gölgeli her ışık Spot (DeskGame ve Giris, kapalılar dahil), üç ana ışık 2048 ve sahnedeki bağlamalar |
| `Desk/Tests/EditMode/Rev3ModelTests` | Revizyon 3 model hattı: 198 manifestin prefab'ı, üçgen sayısı ve bütçe, sıfır pozda kutu (OBJ köşeleriyle ≤ 0,1 mm, manifestin ≤ 1 mm içinde), parça hiyerarşisi ve pivotlar, yazı yuvalarının yeri ve normali, çapa ve ışık sayıları, parçaların aralığında kendi ekseninde dönmesi/kayması, swap malzemeleri, `_AxisTest` eksenleri, büyük harf yüksekliği ve şablon fontu, malzeme kitaplığının değerleri, onarılan devir fişi |
| `Desk/Tests/EditMode/PlanningTutorialFlowTests` | İlk vardiyada gerçek eylem sırası, bilgi onayı, fişin basılması ile dosyalanmasının ayrımı, doğrudan atama/erken baskın/kayıp, bloke durum ve geçerli devam adımı; aşama 8: dosyalanmış vardiyada hedefe atlama, olay/grup konularının geliş sırasına göre seçilmesi, tezgâhtan ya da tutanaktan geri dönüş, süresi dolan grup ve bekleme sınırı, konu içinde devam, canlı yer tutucular (hedef satırı), eski kabul sözcüklerinin yokluğu |
| `Desk/Tests/PlayMode/PlanningTutorialPlayTests` | Rehber açık gerçek DeskGame: gerçek kart sürüklemesiyle ilerleme, kendi komut/RNG'sini yazmaması, aynı kayıttan rehbere devam ve kapatma, eski kayda dayatılmama, açılışta ilerlemenin korunması, TR/EN okunurluk ve Esc geri zinciri; aşama 8: gerçek akışta hedef satırı, ilk olay tutanağı (EVRAK'a gerçek ışınla tık, satır, KARAR) ve ilk aday grubu (KAPI, tezgâh, GERİ ÇEVİR damgası fişe), rehber sürerken son şalterin "mühürlü · rehber" olması ve bitince kalkması; 26 adımın canlı metninin TR/EN notun kâğıdına ve ekrana sığması |
| `Desk/Tests/EditMode/Rev3Stage9RaidTests` | 13 model kiti ve 0,29 m giriş kapıları, 16 tek renderer'lı/gölgesiz kanal maskesi, piyon menteşeleri, kaynak kolu, antet/istatistik/kalıcı kayıp bağları; sağlık şeridi → bayrak/durum geçişinde taşma yok |
| `Desk/Tests/PlayMode/RaidModelPlayTests` | (PlayMode, 4) gerçek kampanyadan Baskin eklemeli yükleme; TR/EN isimli kartların kadrajı, gerçek kasa içeriği, hareket/geri al ve kayıt saflığı; hasar/plaka/tik/180°/−70°; son kaçış turunda hak kalmasa da isimli ölüm onayı; iki tıklı geri çekilme → masa → kayıt/replay ve aynı canlı kişiler |
| `Desk/Tests/EditMode/EnglishCardBuildersTests` | Kartlı harekât gerçek çeviri tablosuyla EN (V.001/005/009/031): Bridge kurucularının her dizgesi yansımayla toplanır; Türkçe harf, harfsiz Türkçe sözcük (KABUL, SEVK), çevrilmemiş kaynak yok, `Loc.Missing` hiç tetiklenmez |
| `Desk/Tests/PlayMode/PersonCardDragPlayTests` | (PlayMode, 2) gerçek DeskGame K4: deste kartını boş cebe sürükleme atar, dolu ceplerde takas; `BoardDragController` → `BoardDropRules` → `ExecuteBoardCommand`, tek `board_assign` |
| `Tests/EditMode/SoundBankTests` | bölge yatakları, olay sesleri ve `EventFx` eşlemesi, saha ipuçlarının telsiz bandı, her `Cue`'nun duyulur klibi (`EveryCue_HasAnAudibleClip`) |
| `Desk/Tests/EditMode/Rev3Stage8EntranceTests` | R1–R4: dolap pivotları/plakaları, 80 ayrı delik çapası, yeni kart alanları ve baskı UV'leri |
| `Desk/Tests/PlayMode/EntrancePlayTests`, `CabinetRoomPlayTests` | Kartın gerçek vardiyası/TR–EN, 80 delik sınırı, onay zaman aşımı, sessizde değer korunması, gerçek ışınla sürgü, kayıt/devam ve yeni girişin sahne yükleme/siyah sırası; açık belgenin menü kolunu kilitlemesi, boş masada kapağın açılması |

### Yeni pano ve eski testler

Yeni `Simulation.NewGame` varsayılanı kartlı panodur. Oda/ziyaretçi kurallarını kilitleyen eski testler test
assembly'lerindeki `LegacySimulation.NewGame` ile açıkça `planningBoardEnabled = false` kullanır. Yeni pano ve kabul
testleri gerçek varsayılanı kullanır; eski testlerin geçmesi yeni ekonominin dengelendiği anlamına gelmez.

Kayıt biçimi 15'te (14'ten beri) `Step` ile gün atlamak yerine `PrintShift` + `FileShift(receiptId)` kullan. Üretim testi tarif
koymakla bitmez: girdiler `ReserveProductionMaterial` ile ayrılmalı, üretici atanmalı, kapanış yapılmalı; stok bir kez
azalmalı. Üç saha kiti birlikte kaybedildiğinde ikmal → metal/devre → yeni kit üretim yolunun hâlâ açık kaldığını
doğrula. Dolu eşya deposundan `ScrapEquipment` ile yer açılmalı; aynı eşya ikinci kez hurdaya çevrilememeli.
`FileShift` kayda kapanan günle girer. Basılı fişle kaydet/devam ve devam eden iki vardiyalık üretimi de doğrula.

### PlayMode testleri

`Desk/Tests/PlayMode/` (asmdef `Corrigible.Desk.PlayTests`, `includePlatforms: []`, editör asmdef'lerine bağlanmaz) gerçek
DeskGame'i Play'de yükler. Ortak düzen `BoardPlayKit`:

- `LoadDesk(seed, passBriefing: true, tutorial: false)` kayıtları geçici klasöre ayırır (`SessionState "Corrigible.TestStorage"`, sahne yüklenmeden önce), DeskGame'i
  açar, `NewGame(seed)`, emir belgesini BAŞLA ile geçer (özel `SiginakBriefing.Action`, yansımayla), kamerayı K1'e oturtur,
  `RetireJobs` için üç kare bekler ve `Baseline`'a komut sayısı + `S.Rng.State` yazar. `[UnityTearDown]`'da
  `BoardPlayKit.TearDown()`: önce DeskGame boşaltılır, sonra klasör silinir (sıra önemli; yoksa kapanışta gerçek kayıt ezilir).
- Tık ve sürükleme `ExecuteEvents` ile doğrudan nesneye gider (`Click`, `Drag`); pano hedefi `eventData.position`'dan
  çözülmeli. Klavye ve sağ tuş testin eklediği sanal aygıtlarla (`PressKey`, `RightDrag`); test boyunca Input System "girdi her
  zaman oyuna" kipindedir, söküm geri alır.
- Süreler token'dan (`motion.board.*`), tolerans token + iki kare (`FrameTolerance`); kamera olayları `PoseLog` ile.
- Kabul testleri `AdmissionPlayKit` kullanır: tutamaklar `SiginakAdmission`'ın sahne bağlarından (yansıma), kurulum **kayıt
  fikstürüyle** (`FixtureGame` + `Retire` + `FixtureToDay` → `SaveStore.Write` → aynı karede KAYITTAN DEVAM; masa yan dosyası silinir),
  canlı vuruş ve teleks gereken testlerde son kapanış gerçek arayüzle (`ShiftPlayKit.PrintToTongue` + `FileOnEvrak`). Tık gerçek
  ışınla (`HitOn`: çarpıştırıcının ekranda ilk çarpılan noktası, örtülme raporlanır) ve **Input System 1.19 kuralıyla** (`ClickHit`:
  basış hiyerarşideki ilk `IPointerDownHandler`'a, tık en yakın `IPointerClickHandler`'a); `BoardPlayKit.ClickHit` eski kuralı uygular.
  Uzun kurulumlarda `liberationAlarmPerShift = 0` (karşı saldırı araya girmesin; alarm RNG çekmez).

Rehber testlerinde `LoadDesk(tutorial: true)` kullanılır. Normal pano testleri `TutorialPreferenceScope` ile rehberi kapatır;
önceki Done/Seed/Step değerleri sahne boşaltıldıktan sonra geri gelir. Aynı koruma menü testlerinde de vardır.
Manuel deneme: yeni harekât başlat, emir belgesinde BAŞLA; rehber daha önce bitmişse **Corrigible › Eğitim › Eğitimi sıfırla**
ve yeni harekât. Oyuncunun kaydında denemek yerine kayıt alanını ayır; gerçek tercihleri de yedekle.

Koşmak: önce **boş sahne aç**, Play'de olmadığından emin ol, sonra `uptest` (ya da `uptest BoardCameraPlayTests`). PlayMode
HTTP üstünden eşzamanlı koşamaz: `uptest` `run_tests --mode playmode --async_tests true` ile başlatır ve `test_status`'u
bitene dek yoklar (Play'e girişteki domain reload sırasında bağlantı hataları beklenir). Bittikten sonra `* Dyn SDF.asset`
değişikliklerini geri al (asset'lerde commit edilmemiş iş varsa `git checkout` onu da siler; önceden aldığın yedekten dön) ve kendi sahneni yeniden aç.

## Otomatik üretimi doğrulamak

`EnableAutomaticProduction` ile yeni yolu aç, `StageProductionMaterial(kind, amount)` ile ayrılmış toplam adedi ver.
Eksik tarifte kapanış hiçbir şeyi harcamamalı. Tam tarifte bir sonraki kapanış tek ürün vermeli; fazla girdiler kalmalı.
Üretici yokken ve depo doluyken harcama olmamalı. Aday tarif önceliğini, kilitli planı ve yeni komutların Replay sonucunu dene.
Eski `QueueProduction`/`ReserveProductionMaterial` testleri eski kayıt yolunu korumak içindir.

## Oyunu ajan olarak test etmek

Editor arka plandayken Game görünümü güncellenmez; ekran görüntüsü `CorrigibleDebug.Capture` ile alınır (Overlay
canvas'ları geçici olarak kameraya bağlar; odanın dünya uzayı canvas'ları yerinde kalır).

Oyunun akışı bir eval'in içinde coroutine olarak sürülür (GameController'da `StartCoroutine`); her adımdan sonra
bekle, durumu bir `Temp/*.txt` dosyasına yaz, ekran görüntüsü al. Sık kullanılan tutamaklar:

```csharp
var game = Object.FindFirstObjectByType<Corrigible.Game.GameController>();
var surface = Object.FindFirstObjectByType<Corrigible.Desk.Bridge.LiberationDeskView>();
// Emir belgesi: BAŞLA iki basış ister (kayıt uyarısı + onay). Düğme BriefingView'ın özel "action" alanında.
// Eski oda modelinde vardiya: ShiftSwitch.Press() iki kez (kapak + kol); SiginakShift.Skip() diziyi atlar; rapor özel File() ile tepsiye.
// Eski ziyaretçi kapısının kâğıdı açıkken masa kilitli (V031'de ziyaretçi var): DoorCardView'ın özel hintButton'ı (alt satır) kâğıdı kapatır.
surface.LaunchSector(game.Sim.NextLiberationSector(game.Sim.S.Liberation.CurrentRegion).Id);   // baskın emri (fişi ve raporu atlar)
// Fişten emir: SiginakMap.ShowMissions(), HandleEnter() iki kez (BASKIN EMRİ VER → kadro raporu → BASKINA ÇIK); HandleEscape()
// raporu kapatır. SquadReportOpen raporun açık olduğunu söyler.
// Baskın: RaidLauncher.IsRaidLoaded; FindFirstObjectByType<RaidDataSource>() → Select, Move, Arm, Use, EndTurn,
// Retreat() iki kez, Continue() rapora geçer (sonuç kartı 0,6 s girdi kilitli). Dönüşte SiginakOperation.ReportOpen;
// rapor özel File() ile tepsiye. FinalReportView.Instance → IsOpen, PressRestart().
```

- **Revizyon 3 baskın (R11/R12):** `RaidPieceLibrary` → `Desk/Prefabs/Rev3Raid` birleşik prefab'ları; ham modeller
  `Desk/Prefabs/Rev3/raidkit`, `raidcivil`, `raidpieces` altında kalır. `RaidRoomKit` kendi duvar/ayrık/köprü mesh ve paletini,
  sivil cihazını, molozunu ve düşük kapısını taşır. `RaidTileSkin` komşuluk maskesini değiştirmeden bu kaynakları giydirir;
  kanalın 16 önceden birleşmiş meshi `Art/Rev3/SceneMeshes/Raid` altında, gölgesiz tek renderer'dır. Kare tabanlı model molozu
  `BreachRubbleYaw` ile 90° adımlarla döner. `RaidPiece`'te `letterPivot` ve `fallPivot` ayrıdır; Game assembly Desk'i bilmez.
  Antet ve kartlar salt okunur `RaidViewModel` üzerinden gelir; kampanya yoksa vardiya/kasa içeriği uydurulmaz.
  `Rev3TextureBaker.BakeStage9()` ve `BakeRaidPaper()` kelimesiz baskıları üretir. Tek seferlik sahne kurucusu tutulmaz.
  Kanıt: `UI_Siginak_Rev3/A9_DOGRULAMA.md`. Kamera ölçeğini `1080/(2*orthographicSize)` ile ölç: 132,58 px/birim;
  izometrik zemin kenarının projeksiyonu 108,08 px'dir.

- **Revizyon 3 kartlı pano (K4):** `PlanningBoardPresenter.Show()` K4'e gider; kartlar `Board3D.CardFor(id)`,
  `ActiveCards`. Sürüklemeyi `BoardPlayKit.Drag`'deki sırayla sür (giriş, basış, olası sürükleme, ilk adımda
  `beginDrag`, her karede `drag`, bırakış): hedef `eventData.position`'dan pano düzlemine ışınla çözülür. Ara durumu
  görmek için bırakmadan önce ekran görüntüsü al. Üzerine gelme etiketi `Mouse.current`'ı izler; sentetik
  `PointerEventData` onu taşımaz. Sanal fare ekle (`InputSystem.AddDevice<Mouse>`, `MakeCurrent`; ayarlarda
  `backgroundBehavior = IgnoreFocus`, `editorInputBehaviorInPlayMode = AllDeviceInputAlwaysGoesToGameView`) ve
  `InputSystem.QueueStateEvent(mouse, new MouseState { position = … })` ile taşı; eval'den okunan fare konumu editörün
  arabelleğidir (0, 0 görünür), oyun doğru konumu görür. Kamera piksel boyu Game görünümüne bağlı:
  `PlayModeWindow.SetCustomRenderingResolution(1920, 1080, …)` ekran koordinatlarını yakalama boyuna eşitler (editör
  tercihidir, sonra geri al). Sunum fikstürü (10 kişi + 32 eşya gibi) `Board3D.Render(snapshot, false)` ile çizilir, Core'a
  dokunmaz; bir sonraki komut ya da sürükleme sonu gerçek görüntüye döner. Başarım ve canlı punto için bkz.
  `IMPLEMENTATION_MAP` §9 (300 kare + eşzamanlı 30 çizim; `SiginakTextScan.FieldsOf` + `MeasureWorld`).
- **Görev alanlı kart panosu (eski uGUI; Revizyon 3'te tuvali gizli, kabul penceresi Aşama 6'da silindi):** `PlanningBoardPresenter.Show()` K4'e, `ShowAdmissions()` kabul istasyonuna (`SiginakAdmission.Enter()`, K8) gider.
  `View.People`, `Equipment`, `Materials`, `Recipes` ve `Zones` sahnenin gerçek kart/hedef referanslarıdır; `RoleTags`
  boştur. Kişiyi Teknisyen/Analist/EMP veya başka bir görev alanına bırak: Core görevi ve kayıt komutu değişmeli, kart
  o alanda görünmeli. Dolu alana bırakıldığında iki kişinin geçerli takasını; Boşta alanında birden fazla kişiyi dene.
  Boş panoya veya geçersiz hedefe bırakmak kartı geri getirmeli ve komut/RNG değişmemeli. Eşyayı kişinin üstüne bırak:
  sahiplenilmeli ve kişiyle taşınmalı; Depo alanına bırakınca sahipliği kaldırılmalı. Başka bir boş alana bırakmak
  eşyayı kişiden çıkarmamalı. Tarif→Üretim ve hammadde→eşleşen girdi yollarını ayrıca dene; üretilen eşya Depo'da
  görünmeli. Kayıttan devamda kişi kartları Core görev alanlarından yeniden yerleşmeli; eski serbest kişi konumu
  atamayı ezmemeli. Önceki `planning_board_layout.json` ofsetleri ekipmanı görev alanı dışına taşımamalı;
  konumlar sahnedeki alanlardan/ofsetlerden hesaplanır. Panoda sicil, makine, fiş veya Inbox yoktur.
- **Revizyon 3 vardiya kapanışı (K6, R19):** `SiginakShiftClose` (`SiginakRoom/Shift controller`) ve özel alanları `shiftSwitch`,
  `machine` (`Paper`), `tray`, `fade` tutamaklardır; sahnede iki `ShiftSwitch` var (dolabınki eski kipte), şalteri bu alandan al.
  Şalterin tıklanır hedefi `ShiftSwitch.Target` (iki tık: kapak, kol); fiş `Paper.State` (`Where`) ile izlenir, kol −35°'ye varınca çıkar.
  Fişi ele almak için fişin GameObject'ine tık; EVRAK'a sürüklemek için `EvrakTray`'in `receiptDropArea` merkezinin ekran noktasına
  `BoardPlayKit.Drag` sırasıyla. Eldeyken Esc ve sağ tık yalnız gerçek (sanal) aygıttan okunur, `HandleEscape()` çağrısı fişi geri koymaz.
  R19c fikstürü Play'de fiş eldeyken `HandoverSlipFixtures.Present("V031")` (yalnız sunum), düz çizim `RenderAllFlat()`; canlı punto
  `SiginakTextScan.ScanLiveSlip(view)`. PlayMode yardımcıları `ShiftPlayKit` (`Begin`, `Log`, `DragSlip`, `TrayScreen`).
  Kartlı kipte `SiginakFixtures` günü fiş basıp dosyalayarak ilerletir.
- **Revizyon 3 kapı ve kabul (K8 ailesi, R9 / R17; KOS aşama 2 ve 7):** tutamak `SiginakAdmission` (`SiginakRoom/Admission controller`):
  `Enter()` / `Leave()`, `Busy`, `Active`, `Last` (son anlık görüntü), `Ui` (dolu sığınakta işaretli yollanacak kişi); sentetik girdi
  `PressStamp(StampChoice, adayId)`, `ClickReplace(kişiId)`, `ClickCounter()`, `ClickPaper(AdmissionPaper.Candidate | GroupSlip, i)`,
  `ClickDeskKey()`, `HandleBack()` (gerçek tık yolu ayrıca denenmeli; `AdmissionPlayKit.HitOn`/`ClickHit`). İstasyon ve iki damga
  akışın özel alanlarında (`station`, `stamps`, `door`, `readAnchor`); kartlar `station.Candidates`.
  - **Tohum 79 durumları** (varsayılan denge; `RetireJobs` sonrası Boşta Cem, İpek, Zeynep; Day n = n kapanıştan sonra = V.(n+1)):
    ilk grup V.005'te 2 aday (`AdmissionFixtures.FirstGroup()`), tohum 3'te V.003'te 3 aday. Dolu sığınak için
    `boardResidentCapacity = boardStartResidents` (`FullGroup`). Gün atlamak için `PrintShift` + `FileShift(Board.ReceiptId)` (Core)
    ya da şalter → fiş → EVRAK (arayüz).
  - **Sunum fikstürleri:** Play'de `AdmissionFixtures.PresentAndCapture("group", "Temp/a.png")` (`group`, `group_full`,
    `group_stamped`, `group_en`), sonra `AdmissionFixtures.Restore()`. Canlı punto K8R'de `SiginakTextScan.ScanLiveAdmission(Camera.main)`.
  - **Beklenen davranış:** vuruş yalnız canlı kapanışta (yüklemede yok, kamera dönmez); KABUL bir karta, GERİ ÇEVİR grup fişine;
    KABUL'de kamera K8'e döner, tezgâh katlanır, kapı açılır; dizi sırasında Esc diziyi sona atlatır. Kayıttan devam vuruşsuz.
  - **Geçmiş (aşama 7'de söküldü):** eski tek adaylı kabulün fikstürleri (`present`, `inspect`, `pending`, `stamped`, `aid`, `v3`,
    `read`, `trace`), Suna 9 / Eren / gözetmen Cem senaryosu, büyüteç cetveli, buğu camı (K8G) ve talep defteri notları.
- **Sığınak olayları, ifşa ve sonlar (KOS; KOS aşama 8'de Play'de denendi).** Önce kayıt alanını ayır, DeskGame açıkken
  Play'e gir, yeni harekâtı kur ve emir belgesini geç:

  ```bash
  ueval 'UnityEditor.SessionState.SetString("Corrigible.TestStorage", "/tmp/kos-deneme"); return "ok";'
  uplay
  ueval 'var g = UnityEngine.Object.FindFirstObjectByType<Corrigible.Game.GameController>(); g.NewGame(79); return "yeni";'
  # 2-3 kez: BAŞLA'ya basar (durum Ready değilse Skip)
  ueval 'var b = UnityEngine.Object.FindFirstObjectByType<Corrigible.Desk.Bridge.SiginakBriefing>(); var t = b.GetType(); var st = t.GetField("state", System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(b).ToString(); if (st != "Ready") { b.Skip(); return st; } t.GetMethod("Action", System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(b, null); return "BASLA";'
  ```

  - **Gerçek akışla olay:** kapanışları komutla geç (`PrintBoardShift` + `FileBoardShift`, sunum atlanır). Aşama 8 dengesiyle
    tohum 79'da baskınsız yedinci kapanışta (V.008) `erzak_azaliyor` açılır; kapıda ilk grup V.005'te, ikincisi V.008'de:

    ```bash
    ueval 'var g = UnityEngine.Object.FindFirstObjectByType<Corrigible.Game.GameController>(); var sim = g.Sim; for (int i = 0; i < 8 && sim.PendingShelterEvent == null; i++) { if (!g.PrintBoardShift().Ok) break; g.FileBoardShift(sim.Board.ReceiptId); } return (sim.PendingShelterEvent?.Kind ?? "-") + " V." + (sim.S.Day + 1);'
    ```

    Karar komutla: `g.ExecuteBoardCommand(s => s.DecideShelterEvent("tam"))` (seçenek kimlikleri `ShelterEventData.Options(tür)`,
    güvenlisi `SafeOption(tür)`).
  - **Belirli bir olayı hemen getirmek** (hata ayıklama kurulumu: komut değildir, kayda girmez, RNG çekmez; bu durumdan sonra
    kayıt başka oyuna döner, kaydetme ya da yalıtılmış klasörde kal). `iz`/`ikinci_iz`/`hirsizlik`/`kacak`/`agir_yarali`/`anma`
    için `PersonId = sim.Board.People[0].Id`, `kavga` için ayrıca `OtherId`; `iz` için `Area = "uretim"`. `Changed` olayı yansımayla
    tetiklenir ki oda yenilensin:

    ```bash
    ueval 'var g = UnityEngine.Object.FindFirstObjectByType<Corrigible.Game.GameController>(); var sim = g.Sim; sim.ShelterEvents.Pending = new Corrigible.Core.ShelterEventInstance { Kind = "konvoy", OpenedShift = sim.S.Day }; (typeof(Corrigible.Game.GameController).GetField("Changed", System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(g) as System.Action)?.Invoke(); return sim.PendingShelterEvent.Kind;'
    ```

    Kapalı satırı görmek için önce koşulu bozan değeri kur (ör. `karartma` için `sim.S.Alarm = sim.B.liberationAlarmThreshold - 1`).
  - **İfşa göstergesi:** `sim.Shelter.Exposure = 2` (şerit), `= sim.B.exposureThreshold` ve `sim.Shelter.FallPending = true`
    (KONUM ÇÖZÜLDÜ: lamba, CRT, teleks, fiş). `sim.Shelter.FallGrace = 1` kararla varılan eşiğin ikinci uyarı vardiyasıdır.
  - **Sonu hemen getirmek:** `dusus` için açık olayı kaldır (`sim.ShelterEvents.Pending = null`), `Exposure` eşikte,
    `FallPending = true`, `FallGrace = 0`, sonra bir kapanış: son rapor SIĞINAK DÜŞTÜ ile açılır.

    ```bash
    ueval 'var g = UnityEngine.Object.FindFirstObjectByType<Corrigible.Game.GameController>(); var sim = g.Sim; sim.ShelterEvents.Pending = null; sim.Shelter.Exposure = sim.B.exposureThreshold; sim.Shelter.FallPending = true; sim.Shelter.FallGrace = 0; g.PrintBoardShift(); g.FileBoardShift(sim.Board.ReceiptId); return sim.S.Ending;'
    ```

    Son şalter: `sim.S.Day = sim.B.lastSwitchMinShift - 1` şalterin mührünü açar (yalnız deneme; kayıtla çelişir), `sim.S.Trust = 1`
    plakayı "çekilirse: TAHLİYE" yapar; çekmek gerçek arayüzle (kapak, kol, ÇEKİLME EMRİ'ne damga). `terk` ve zafer tonları için
    gerçek akış gerekir: kayıt fikstürleri `KosEndingsPlayTests` ve `EndingTests`'te (ör. `Quiet` dengesi, eksik kadro kurulumu);
    zafer için aşağıdaki "harekâtın sonu" tarifi.
  - Sonda: `ustop`, `ueval 'UnityEditor.SessionState.EraseString("Corrigible.TestStorage"); return "ok";'`, klasörü sil,
    `* Dyn SDF.asset`'leri yedekten geri al.
- **Vardiya şalteri ve fiş:** şalterin kapağı ve kolu ayrı eylemlerdir (`ShiftSwitch.Press` iki kez). Kol inince
  kâğıt şalterin yanındaki çıkışta görünmeli, şalter inik kalmalı ve gün değişmemeli. `ShiftReceiptPaper` gerçek işaretçi sürüklemesiyle mevcut
  `EvrakTray` kutusuna bırakılınca kararma ve `GameController.FileBoardShift` ile yalnız bir gün geçmeli. Yanlış yere
  bırakılan kâğıt günü ilerletmemeli. Basılı kâğıtla kaydet/devam, eski fişin reddi ve kutunun rapor arşivini ayrı dene.
  Baskın sonrası kilitli plan kapanışa kadar değişmemeli. `Simulation.Step` veya eski `AdvanceShift` kestirme değildir.
- **Güvenli kayıt alanı:** Play öncesinde `UnityEditor.SessionState.SetString("Corrigible.TestStorage", testFolder)`
  kullanılırsa oyun ve masa JSON'ları yalnız test klasörüne gider. Önceki tercihleri ayrıca yedekle; sonda Play'i
  durdur, `SessionState.EraseString("Corrigible.TestStorage")` çağır ve tercihleri geri yükle. Kullanıcı bu arada
  gerçekten oynadıysa son ilerlemesini eski yedekle ezme. Dinamik font değişikliklerini ayrıca temizle.
- **Menü sahnesi ve dolap:** oyun build'de `Giris`'ten (kapının dışı) açılır; editörde DeskGame'i doğrudan açıp Play'e
  basmak menüyü atlar ve bugünkü açılışı (emir belgesi, BAŞLA, KAYITTAN DEVAM) oynatır. Menüyü denemek için `Giris`'i aç.
  Tutamaklar: `SettingsCabinet.Instance` → `RequestOpen()` (odada kamera K9'a döner, kapak kamera varınca açılır; menüde
  dolabın çapasına), `Close()`; kontroller özel `channels`, `sound`, `language` alanlarında (`CabinetFader.Nudge(±1)`,
  `CabinetToggle.Flip()`, `CabinetSelector.Nudge(1)`). Sürgüyü işaretçiyle denemek için `PointerEventData` kur,
  `pointerPressRaycast.module`'üne kameranın `PhysicsRaycaster`'ını ver, `OnPointerDown`/`OnDrag`/`OnPointerUp`'ı doğrudan
  çağır. Dolabın kontrolleri kapak tamamen açılmadan (`Current == Open`) çalışmaz. MENÜYE DÖN ve son şalter
  `ShiftSwitch.Press()` iki kez. Menüde: `EntranceController`'ın özel `Continue()`/`NewCampaign()` metotları kartın ve
  destenin tıklamasıdır (kayıt varken deste iki adım: `TimeClock`'ın özel `Stack()`'i iki kez); DeskGame'e geçişte niyet
  `GameLaunch.Pending`. Son şalter editörde Play'i durdurur.
  Revizyon 3 Aşama 8'de hazır durum `EntranceController.Ready`, dizi `Entering`, kart `TimeClock.HasCard` /
  `Armed` / `Punching` ile okunur. Saate tıklamak da doğrudan devam eder. M2'yi eylemsiz incelemek için `EntranceCamera.Go`
  ile `clockPose` çapasını kullan. Oda kolunun kilidi `LiberationDeskView.MenuExitBlocked`'dan gelir; dolap kendi kendini
  kilit nedeni saymaz. Model baskıları `Rev3TextureBaker.BakeStage8()` ile üretilir; UV ağ kopyaları
  `Art/Rev3/SceneMeshes/Menu` içindedir. Doğrulama ve kalibrasyon: `UI_Siginak_Rev3/A8_DOGRULAMA.md`.

- Belirli bir oda durumuna hızlı gitmek için `Corrigible.Desk.EditorTools.SiginakFixtures.Load("V014")` gibi hazır
  durumlar var (bkz. `SiginakFixtures.cs`). Durumu elle değiştirmek (`S.Fon = 200`) testi hızlandırır ama kayda girmez;
  kayıt/devam testinde kullanma.
- Harekâtın sonunu görmek için komutları doğrudan `Simulation` üstünden çalıştırıp (`StartLiberationRaid` +
  `ResolveRaid(new RaidResult { Success = true })`) sonra `game.Flush()` ve `SimDeskDataSource.MarkDirty()` çağır;
  `GameController` üstünden çalıştırırsan `RaidLauncher` her baskında sahneyi yüklemeye kalkar.
- `Scenes/Baskin.unity`'yi tek başına açıp Play'e basmak örnek bir sunucu baskını kurar (tahtayı hızlı denemek için).
  Hamleler ve makineler evresi canlandırılırken (`Busy`) girdi yok sayılır; ekran görüntüsünden önce bekle.
- Test oyunları **gerçek kayıt dosyalarını ve PlayerPrefs'i değiştirir**. Play öncesi `persistentDataPath` altındaki
  `liberation_savegame.json`, `liberation_desk.json` ve `corrigible.*` tercihlerini yedekle; sonunda geri yükle, önceden
  olmayan dosyaları sil. Harekât biterse kayıt silinir. Geri yükleyemediğin değişiklik varsa kullanıcıya söyle.
- Play sırasında script derlemek statik tekil örnekleri sıfırlayabilir. Script değişikliklerini Edit modunda yap.
- eval kodu tek tırnak içinde gönderilir; C# karakter sabiti (`' '`) kullanma, `"".PadLeft(n)` gibi dizgi kullan. Uzun
  kodu bir dosyaya yazıp `ueval "$(cat dosya.cs)"` ile gönder.
- Uzun `eval` çağrılarında **iki ayrı zaman aşımı** var: ikinci konumsal argüman sunucudaki C# çalışma süresi
  (milisaniye), global `--timeout` ise CLI'ın yanıtı bekleme süresi (saniye). `ueval` 5 sn'de "zaman aşımı" dönse de
  kod çoğunlukla tamamlanır; sonucu dosyadan oku.

  ```bash
  unity command eval 'return Corrigible.EditorTools.CorrigibleDebug.Capture("Temp/shot.png");' 60000 --timeout 90 --caller plugin --skill unity-cli
  ```

## Sığınak görünümünü doğrulamak (görüntü seti, punto, kare süresi)

- **Tam görüntü ve dil/punto seti (`Rev3CaptureSet`, Aşama 10):** 86 durum etiketini tohum 79'la, 1920×1080'de TR ve EN kurar (B1 yalnız EN); her durumda görüntü ve `ScanLiveAll`. Eski `SiginakCaptureSet` emekli.
  - **Önce:** kayıtları, tercihleri ve dinamik font asset'lerini yedekle. Edit modda `SessionState "Corrigible.TestStorage"` kur (yoksa koşu başlamaz) ve Game'i `PlayModeWindow.SetCustomRenderingResolution(1920, 1080, …)` yap.
  - **Bir kez:** `ueval 'return Corrigible.Desk.EditorTools.SiginakTextScan.ResetLiveAll();'` (yeniden çekimlerde çağırma; `WriteLiveAll` her durumun son koşusunu seçer).
  - **Oturumlar:** Giris açık Play'de `menu`, DeskGame açık Play'de `k1`, `k4`, `k6`, `k8`, `hover`, `raid`; her biri `tr` ve `en`, aralarda `ustop`/`uplay`: `ueval 'return Corrigible.Desk.EditorTools.Rev3CaptureSet.Start("k4","en");'`.
    `Sessions()` adımları listeler. Hatalıları yeniden çek: `Start("k4","en","K4c,K4n")`. `menu en` öncesi `PlayerPrefs corrigible.language=en` (B1 temiz EN açılış), sonda geri al.
    Dil, oyun kurulmadan seçilir: Core'un gün 0 metinleri üretildikleri dilde kalır, kurulu oyunda dil değiştirmek ölçüm sayılmaz.
  - **İzleme ve çıktı:** `Status()` ya da `Temp/capture_set.txt` (son satır `bitti` ile başlar); bütün koşuların özeti `Temp/capture_set_all.txt`, `FAIL` satırlarına bak. Görüntüler
    `Docs/UI_Siginak_Rev3/screens_unity_rev3/A10_<ID>_{TR,EN}.png`, TR'de `_gray` (Aşama 10'un 259 görüntüsü sonradan boyut için `.jpg`'ye çevrildi).
- **Canlı dil ve punto (`SiginakTextScan.ScanLiveAll`):** çizilen puntoyu 1080p'ye ölçer (14 px; kart adı, fiş adı, defter adı ve belgedeki ad 18 px). Bayraklar: kesilme, dikey/yatay taşma, otomatik punto alt sınırı, `.notdef`, Dyn yedek, EN'de Türkçe,
  çevrilmemiş kaynak, `Loc.Missing`. Sonda `WriteLiveAll()` satırları `Docs/UI_Siginak_Rev3/A10_dil_olcumleri.json`'a yazar ve okuma alanını atar (`okuma`, `uzak`, `kagit`, `form`, `gecis`); `küçük` yalnız `okuma` satırına konur.
  Dikey taşma bayrağını bilinen tuzaklardaki `textBounds` notuyla oku.
- **Punto taraması:** Edit modda `ueval 'return Corrigible.Desk.EditorTools.SiginakTextScan.Run();'` (DeskGame ve Baskin'i ek
  olarak açar; rapor `Docs/UI_Siginak_Rev3/punto_taramasi.txt`). 14 px altı her yeni metin ya düzeltilir ya da
  `SiginakTextScan.Exceptions`'a gerekçesiyle girer; `SiginakTextScanTests` ikisini de denetler.
- **Kare süresi:** Play'de aynı oyun durumunda (`SiginakFixtures.Load("V014")`) kamerayı 1920×1080 MSAA 4× bir hedefe 30 kez
  çiz, `ReadPixels` ile GPU'yu bekle, tur başına ms/kare al. Eski bir commit'le karşılaştırmak için kullanıcının editörüne
  dokunma: `git archive <commit>` ile geçici klasöre çıkar, `Library`'yi `cp -cR` ile kopyala (APFS klonu, yer tutmaz),
  kopyanın `ProjectSettings.asset`'inde `productName`'i değiştir (Play kayıtları gerçek kayıtların üstüne yazılmasın), ölçüm
  script'ini yalnız kopyaya koy ve `Unity -batchmode -projectPath <kopya> -executeMethod …` ile koştur (Play'e girmek için
  `EditorApplication.update` üstünde adım adım ilerleyen, `SessionState`'te adımı tutan bir `[InitializeOnLoad]` sınıf). Turlar
  arası kayma ±0,1 ms olduğu için iki kopyayı art arda, birkaç kez sırayla koştur ve çiftlerin farkına bak.
- **Gölge atlası ve poz başına kare süresi (Aşama 10 yöntemi):** betik (`Rev3Stage10Perf`) tek seferlikti, depoda yok; yeniden gerekirse bu tarifle yaz, ölçüp sil. Sonuçlar `UI_Siginak_Rev3/A10_DOGRULAMA.md`.
  - **Kayıt:** test kayıt klasörü, Game 1920×1080 önde ve odakta, Scene ve Profiler arkada. Satır başına HEAD, GPU, API, kalite, ekran ve vSync yazılır (Aşama 10 koşusu bu koşulu sağlamadı: ortam satırları `screen=555x808`, `focus=False`). Mac Metal sayıları Steam PC'nin vekili değildir.
  - **Atlas yoklaması:** Frame Debugger CLI'den sürülemez. `RenderPipelineManager.endCameraRendering`'de hedef kamera açıkça verilir (eklemeli baskında iki kamera da `MainCamera` etiketli), `UniversalAdditionalCameraData.scriptableRenderer`
    → yansımayla `m_AdditionalLightsShadowCasterPass.m_AdditionalLightsShadowSlices` (ilk `m_ShadowSliceToAdditionalLightIndex.Count` eleman) ve Baskın'da `m_MainLightShadowCasterPass`. Yanına gölgeli ışık listesi ve Editor.log'daki ölçek uyarıları.
  - **Örnekleyici:** `rig.Snap(poz)`, 120 kare oturma, 600 kare Δt + `FrameTimingManager` + ProfilerRecorder; medyan, p95, en büyük. İki tur, sıra değişimli; p95 > 2 × medyan kısma sayılıp tekrarlanır. Poz geçişi kare kare (gölgeli küme iki pozun birleşimidir).
    Yumuşak/sert için beş ABAB çifti, TAA/post için taban + değişken tek çift; |Δ| ≤ 0,15 ms gürültü. Hedef: medyan ≤ 16,7 ms, p95 ≤ 20 ms.
- **Gölge kuralı:** DeskGame ve Giris'te gölgeyi yalnız Spot taşır, kapalı ışıklar dahil; üç ana ışık (`bulbSpot`, `deskLamp`, `boardLamp`) 2048'e kilitli, sahne override'ı olarak. İkisi de `Rev3RoomTests`'te (`Scene_EveryShadowedLightIsASpot_EvenWhenDisabled`,
  `DeskGameScene_MainShadowLightsAskFor2048`). Yeni gölgeli ışık eklerken poz başına en çok 4 gölgeli ışık kuralına ve geçiş birleşimine de bak.
- Sahnede bir nesneyi statik işaretlemeden önce: çalışırken taşınmıyor, ölçeklenmiyor ve TMP değil mi bak (statik birleştirme
  dönüşü ve TMP'nin yeniden kurulan mesh'ini dondurur; renk ve malzeme değişimi sorun değil).

## Baskın sunumunu düzenlemek

Görsel kaynak `Docs/SonSalter_Baskin_Paketi`: PNG'ler görünüşü, `reference/measure` TSV'leri sayısal yerleşimi,
`spec/raid_tokens.json` kamera/malzeme/işaret değerlerini anlatır (oyundaki kopyası `Desk/Data/Sade/raid_tokens.json` →
`Desk/Resources/RaidTheme`). Geçerli hamle, hasar ve niyet hesabı `Core/Baskin.cs`'ten gelir; görseli uydurmak için Core
kuralı değiştirme.

- Kalıcı sahne `Scenes/Baskin.unity`; HUD ve paneller `Desk/Prefabs/Sade/Raid` prefab'larında. Referanslar
  `[SerializeField]` ile bağlıdır.
- Parçalar `Prefabs/Raid` altında; var olan parçayı güncellerken prefab'ı yerinde kaydet, GUID'ini ve `RaidPiece`
  referanslarını koru. Parça/palet bağlantıları `Settings/RaidPieceLibrary.asset`; dokular, mesh'ler ve malzemeler
  `Art/Raid`. URP malzemeleri mat; emission ve kamera post-processing kapalı.
- **Izgara ve kamera:** bir karo bir dünya birimi, merkezler `RaidBoard3D.W(c, r) = (c − 3,5, TileTop, 3,5 − r)`. Kamera
  ortografik, rotasyon `(35°, 45°, 0)`. `SadeRaidCameraFraming` ortografik boyutu ekran yüksekliği, canvas ölçeği ve
  `raid.camera.pixelsPerUnitAt1080` tema değerinden hesaplar; izdüşüm `SadeRaidBoardTests.Camera_ProjectsGridAndHeightToReferencePixels`
  ile kilitli. Kadrajı ya da karo ölçeğini elle sabitleme.
- Kontrol: iki dilde gerçek baskın aç; erişilebilir kareleri, EMP önizlemesini, makinelerin sıra vurgusunu ve girdi
  kilidini, geri çekilme onayını ve sonuçtan rapora dönüşü dene. 1920×1080'e ek olarak 16:10 ve ultra geniş kadrajı gör.

## Sık yapılan işler

**Denge sayısı değiştirmek:** kalıcı değişiklik iki yerde birden: `Balance.cs` varsayılanı (testler `new Balance()` kullanır)
ve `Settings/BalanceConfig.asset` (oyun asset'i okur). Asset'i tek seferlik, yalnız o alanlarla düzenle (metin düzenlemesi ya
da `SerializedObject`), sonra ikisinin alan alan aynı olduğunu yansımayla doğrula:

```bash
ueval 'var a = UnityEditor.AssetDatabase.LoadAssetAtPath<Corrigible.Game.BalanceConfig>("Assets/Corrigible/Settings/BalanceConfig.asset").balance; var d = new Corrigible.Core.Balance(); var sb = new System.Text.StringBuilder(); foreach (var f in typeof(Corrigible.Core.Balance).GetFields()) if (!object.Equals(f.GetValue(a), f.GetValue(d))) sb.Append(f.Name + " asset=" + f.GetValue(a) + " kod=" + f.GetValue(d) + "; "); return sb.Length == 0 ? "aynı" : sb.ToString();'
```

Yalnız oyunda denemek için asset yeter. Sonra bütün EditMode ve PlayMode testlerini koş: ödül ve gelir beklentileri
`Balance` alanlarından okunur, ama tohuma kilitli altın değerler ve bazı kurulumlar varsayılana bağlıdır. KOS aşama 8'de
(tüketim, ganimet, olay aralığı, kademe eşikleri) kayanlar: `ShelterEventTests` (tohum 79 zaman çizelgesi, 60 tohumluk kayıt
testi, varsayılan beklentileri), `ShelterEventCaseTests` (tüketim 2,5, yarım tayın kurulumu), `CandidateGroupTests` (düşük stok
kurulumu), `RegionIdentityTests` (kademe tablosu ve `MaxTier`), `Rev3BoardSnapshotTests` ve `Rev3HandoverSlipFactsTests` (V.001 erzak
satırı), PlayMode `CandidatePlayTests` (grupla aynı günlerde açılan erzak olayı). Altın değeri gerçek Core'dan yeniden üret
(test iletisi gözleneni yazar) ve gerekçesini yorumda bırak; senaryo bozuluyorsa (ör. baskınsız sığınak açlıktan boşalıyor)
kurulumu ayarla, kuralı değil. Denge kayda yazıldığı için yalnız sayı değişikliği sürüm istemez (MIMARI "Determinizm ve
kayıt"); RNG sırasını ya da var olan komutun sonucunu değiştiriyorsan `GameRecord.CurrentVersion`'ı artır (AGENTS kural 1).

**Bot ölçümü:** harekât düzeneği `Tools/DengeBotu/` (gerçek Core + `Tests/EditMode/RaidBot.cs`, Mono'da `csc` ile derlenen
konsol programı; Unity gerekmez, Assets dışında). `derle.sh` → `Library/DengeBotu/camp8.exe`, `kos.sh asama8.txt` aşama 8'in son
ölçümünü yeniden üretir, `ozet.py` özetler (README'de kullanım ve politikalar). Denge `BAL="alan=değer;…"`. Bot **sunumun
kurallarını taklit etmeli** (`RETIRE=1`, `kos.sh`'ta varsayılan): yayındaki panoda yuvası olmayan görevler (`care`, `gather`,
`watch`, `contact`) `PlanningBoardPresenter.RetireJobs` ile Boşta'ya iner; Core bu görevleri kabul ettiği için bot onları
kullanırsa oyuncunun sahip olmadığı erzak ve bakım kazanır (aşama 8'de yaşandı). Core değişince yeniden derle; sonuçları 80
tohumla oku (40 tohumda "iyi" küme yaklaşık ±7 puan oynar).

**Oyuncu eylemi eklemek:** bkz. MIMARI.md → "Determinizm ve kayıt". Odada eylemi `LiberationDeskView`'a gerekçeli bir
komut olarak ekle (`TelexReasons`'a yapılamama gerekçesi), bağlayıcı onu çağırır.

**Odaya öğe eklemek / sahneyi koddan değiştirmek:** önce boş sahneye geç, sahneyi diskten yeniden aç
(`EditorSceneManager.OpenScene`), objeleri `Transform.Find` (pasifleri de bulur) ile bul, alanları `SerializedObject` ile
bağla, prefab örneğinde değiştirdiğin bileşenler için `PrefabUtility.RecordPrefabInstancePropertyModifications` çağır,
`MarkSceneDirty` + `SaveScene`. Bunu tek seferlik bir eval ya da editör script'iyle yap, sonucu doğrula, script'i sil.
`git diff`'te yalnız beklediğin satırlar olmalı. Prefab'ı `PrefabUtility.LoadPrefabContents` ile aç,
`SaveAsPrefabAsset` ile kaydet, `UnloadPrefabContents` ile bırak (bıraktıktan sonra içindeki nesnelere dokunma).

**Revizyon 3 modellerini yeniden içe aktarmak** (paket ya da token değişince):
- **Menü:** Corrigible › Revizyon 3 › Modelleri içe aktar.
- **Ya da eval:** `Rev3ModelImporter.ImportAll()` ya da `ImportAll("desk")`. Uzun sürer; eval'e ana iş parçacığı zaman aşımını
  konumsal argüman olarak ver: `unity command … eval '<kod>' 1800000 --timeout 1900`. Varsayılan 5 sn'dir.
- **Sonra:** `Docs/UI_Siginak_Rev3/model_import_report.txt` uyarısız olmalı ve `Rev3ModelTests` geçmeli.
- **Görsel denetim:** `Rev3ModelSheet.RenderModelSheet(prefab, yol)` ya da `RenderFamily(aile, en)`. Paketteki
  `modeller/sayfa/<Model>.jpg` ile yan yana bakılır. Çizim boş bir sahne açar; sonra kendi sahneni yeniden aç.

**Bina kitini değiştirmek ya da yenisini eklemek** (`Docs/BASKIN-MEKANLARI.md` Katman 3): renkler
`Desk/Data/Sade/raid_tokens.json` → `kits.<bölge>.*` (yeni anahtarı `_new_1_3` listesine de ekle, sonra **Corrigible › Sade UI
› Tokenları içe aktar**); desen `RaidKitTextureBaker.Motifs*.cs`'teki motif satırında (yalnız kitin sabit tohumuyla, metin ve
`UnityEngine.Random` yok). **Corrigible › Baskın › Kit dokularını üret** (ya da `RaidKitTextureBaker.Bake(ids)`) dokuları,
malzemeleri ve `Settings/Kits/Kit_<bölge>.asset`'i yazar; `wallRubble` ve `bridges` el ile ayarlandıysa korunur. Kapıdan
(`RaidKitGate`) geçmeyen kit kütüphaneden çıkarılır ve rapor nedenini yazar. Sonucu **Corrigible › Baskın › Kit önizleme
sayfası** ile (gri tonlu kopyasıyla) bak, `RaidRoomKitTests`'i boş sahnede çalıştır. Oyunda denemek için baskın sahnesindeki
`LiveRaidDataSource`'un `kitOverride` alanına kit kimliğini yaz (yalnız editörde geçerli; MapSeed 0'da da çalışır).

**Ayarlar dolabına kontrol eklemek** (`Docs/AYARLAR-DOLABI-VE-MENU.md`): parça `Desk/Prefabs/Siginak/SettingsCabinet.prefab`'ın
`Interior`'ına (prefab'ı `LoadPrefabContents` ile aç); tıklanan kökte `BoxCollider` + `DeskTarget` (başlık çeviri aracına
girer) ve kontrol bileşeni. Dolap kontrolün `DeskTarget`'ına kendi kilidini (`BlockedOverride`) ve sahnenin etiketini
`Awake`'te verir: yeni kontrolü `SettingsCabinet.Targets()`'a ve gerekiyorsa klavye sırasına (`ControlTarget`) ekle. Değeri
`IDeskSettings`'e yazar; yeni bir tercih gerekiyorsa arayüzü ve `BridgeSettings`'i genişlet. Yazılar ≥ 14 px (K9'da 1 px ≈
0,64 mm: punto ≥ 0,09), ince parçalar ≥ 2 mm; punto taraması K9'u kendiliğinden ölçer. Ses `SettingsCabinet.Noise` → iki
bağlayıcıda (`SiginakCabinet`, `EntranceController`) `Cue`. Menü ve oda aynı prefab'ı kullanır; menüdeki örnekte
MENÜYE DÖN kolu kapalı.

**Baskın parçasının küçük resmi:** **Corrigible › Sığınak › Baskın parçalarının küçük resimlerini üret**
(`SiginakTextureBaker.PieceThumbs`) sandık, tüp, kaynak kolu, enkaz, sivil ve üreteç resimlerini 3B prefab'dan çizer;
sprite tablosuna `SiginakSpriteManifest`'teki üretilen kayıtla girer. Yeni nesne türü eklersen `RaidDataSource.ObjectThumb`'a
da ekle (yoksa kartta resim gizlenir).

**Baskın yerinin sesleri:** 9 ortam yatağı ve 22 olay sesi `Tools/Audio/raid_site_audio.py` ile sentezlendi (numpy; sabit
tohum, her koşu aynı dosyaları verir): proje kökünden `python3 -I Tools/Audio/raid_site_audio.py`. Çıktı ve ölçüm raporu
(bant kaybı, telsiz bandından sonraki RMS, döngü dikişi, bugünkü seslerle karşılaştırma) git dışı
`Temp/raid_site_audio/`'ya yazılır; tuttuğun klibi aynı adla `Audio/Placeholder`'a kopyala (`SoundBank.asset` ve `.meta`
içe aktarma ayarları değişmez). Yataklar telsiz bandından sonra bugünkü uğultunun düzeyinde (RMS 0,078) tutulur; döngü
noktasında Vorbis tık yaparsa o yatağı PCM'e çevir. `SoundBankTests` her bölgenin yatağını, olay seslerinin adlarını ve
uzunluğunu, `EventFx` eşlemesini denetler. Dolabın ve menünün sekiz kısa sesi (`Cue` 89–96) `Tools/Audio/cabinet_audio.py`
ile (yalnız standart kitaplık, numpy gerekmez) doğrudan `Audio/Placeholder`'a yazılır. Revizyon 3'ün 42 yer tutucusu (`Cue` 56–66, 97–128; 109 hariç)
aynı yolla `python3 -I Tools/Audio/rev3_placeholder_audio.py` ile üretilir, `SoundBankTests.EveryCue_HasAnAudibleClip` hiçbir sesin klipsiz kalmamasını denetler. Klipleri bankaya bağlayan betik tek seferlikti (silindi; üreticinin başlığındaki ad eski): yeni bir klibi `SoundBank.asset`'e bağla, var olanı aynı adla üzerine yaz.

## Lokalizasyon iş akışı

1. Kodda oyuncuya görünen metni Türkçe yaz ve sar: `Loc.T("Metin.")`, `Loc.F("{0} bölgesinde {1} gün.", ad, gün)`.
   Sayı biçimini yer tutucuda ver (`{0:0.0}`); `ToString` + birleştirme yapma. Türkçe ek uyumu gerekiyorsa `TrNumber` ya
   da `Loc.IsSource` ile Türkçeye özel dal yaz. Küçük harfe çevirirken kültürü dile göre seç (Türkçe `I → ı`).
2. Sahneye/prefab'a yeni sabit yazı eklediysen TMP nesnesine `DeskLocText` bileşeni ekle. Odadaki tıklanır nesnenin
   üzerine gelme etiketi (`DeskTarget` başlık ve satır) tabloya kendiliğinden girer.
3. **Corrigible > Lokalizasyon > Çeviri tablosunu güncelle**, sonra `Assets/Corrigible/Resources/Localization/strings.txt`'te
   boş kalan `en` hücrelerini doldur (satırın sonundaki sekmeden sonra). `{0}` ve `<b>` gibi işaretler aynen kalmalı.
4. **Çevirileri doğrula** menüsü ya da `LocalizationTests` → eksik/uyumsuz çeviri listesi.
5. Oyunda dene: ayarlar kartındaki dil seçimi ya da `PlayerPrefs.SetString("corrigible.language", "en")` + Play.

Ajan olarak: `ueval 'return Corrigible.EditorTools.LocalizationTool.UpdateTable();'` ve
`ueval 'return string.Join("\n", Corrigible.EditorTools.LocalizationTool.Validate());'`.

## Bilinen tuzaklar (bu projede yaşandı)

- **TMP `textBounds` mürekkep değil satır kutusudur:** yükseklik fontun `ascender`/`descender` değerlerinden gelir. Tek satırlık yuvada büyük harfler kutunun yarısını doldururken "dikey taşma" görünür (Aşama 10'da 1368 işaret sınıf bazında incelendi, kırpılma çıkmadı; tek tek piksel kanıtı değil).
  Dikey bayrağı tek başına hata sayma; `kesildi` ve yatay bayrağa bak, şüpheliyi görüntüde kontrol et.
- **URP Custom gölge kademesi `m_CustomResolution`'ı değil `Light.shadowResolution`'ı okur:** `m_Shadows.m_Resolution` −1 kalınca ışık 65535 px ister, atlas yarıya inerek küçülür (pano lambası 4095, tezgâh lambası 32 px). Belirti Editor.log'daki ölçek uyarısı.
  Çözünürlüğü sahne override'ı olarak yaz; `m_CustomResolution` tek başına yetmez.
- **Point gölge 6 atlas dilimi yer ve edit modda görünmez:** kapı açılınca yanan koridor lambası atlası 32 px'e düşürüyordu. Kapalı ışıkları da say (kural `Rev3RoomTests`'te).
- **Edit modda `DoorProp.SetOpen`/`ResetClosed` merdiven ışıklarını sıfırlar:** `Rest()` Edit modda ilk çağrıda çalışır, `Apply` şiddeti
  `taban × glow` yazar; kapalıya dönünce sahnedeki `CorridorLight` şiddeti 0 kalır ve URP ışığa `UniversalAdditionalLightData` ekler.
  Açık kapı görüntüsü aldıktan sonra sahneyi KAYDETME (yeniden aç) ya da şiddeti (2,4) geri yaz ve eklenen bileşeni sil.
- **Tek parça gelen modelin hareketli parçası:** üçüncü taraf kapı tek mesh'ti; bağlı parçalara (konum kaynağı + üçgen bağlılığı) ayrılıp
  kasa/kanat/çark türev mesh'leri üretildi (`Art/Rev3/SceneMeshes/Door`). `DoorProp` kanadı yerel y'de +95° döndürür: yönü pivotun
  duruşuyla seç (yerel y aşağı = ters yön), `openAngle`'ın işaretiyle değil (`Rev3DoorTests` 95 bekler).
- **Yakın nokta dolgu ışığı panelde sıcak leke yapar:** panele 13 cm'deki dolap dolgu ışığı (0,35) ortadaki kadranları beyaza gömüyordu; rengi ya da pürüzlülüğü kurcalamadan önce şiddeti sınamak için token'ı düşür (`cabinet.panelFillIntensity`, 0,04).
- **Kök ölçeği çocuğa çarpılır:** prefab örneğinin kökünde ölçek override'ı varsa çocuğa yazılan ölçek onunla çarpılır (`DİL · LANGUAGE` plakası 1,4 × 1,7 = 2,38 oldu). Plakayı genişletirken yazının ters ölçeğini birlikte güncelle.
- **Pano sunucusunun altında otomatik punto yasak:** `PlanningBoardSceneTests.SavedScene_NoMissingScriptsAndBoardTextsFollowTheirKind` kırılır; eski dosya sekmesinin alt satırları bu yüzden sabit 3,0 mm'ydi (sekmeler aşama 7'de söküldü).
- **Çevrilmiş değeri yeniden `Loc.T`'ye verme:** `RegionDef.Name`, `EndingDef.Title/Text` zaten çevrilmiş döner; TR'de fark edilmez, EN'de `Loc.Missing` tetikler.
- **Uzun ölçüm sürücüsü:** bitişi eval'in döndürdüğü dizgiden değil günlük dosyasından izle: adımdan önceki satır sayısını al, ondan sonra gelen `BİTTİ` satırını bekle (CLI çıktısında `ü`/`İ` eşleşmesine güvenilmez). Play'de koşan ölçümde Unity kapanırsa sürücü durur ve `Temp` günlüğü gider;
  koşuyu baştan başlat. Yeniden başlatmada kalması gereken tek seferlik iş klasörünü `Library/` altına koy.

- **`DeskTarget` çarpıştırıcı ister:** `[RequireComponent(typeof(Collider))]` var ama `Collider` soyut; Unity onu kendisi ekleyemez,
  çarpıştırıcısız nesneye `AddComponent<DeskTarget>()` "Adding component failed" yazıp null döner. Testte önce `BoxCollider` ekle
  (`Rev3DoorTests.Controller`).
- **Sarmalayan eşyordam iç `IEnumerator`'ın istisnasını yakalamaz:** `yield return Adım()` içinde atılan istisna konsola düşer, dış
  akış sessizce durur (C# `yield` içeren bloğu `try/catch` ile saramaz); eval'den sürülen görüntü/fikstür betiğinde "bitti" satırı hiç
  yazılmaz. Her adımı kendi içinde koru ya da `MoveNext`'i elle sürüp yakala; durumu `Temp/*.txt`'ye adım adım yaz.
- **Elde tutulan nesne işaretçiyi izler:** damga elde `Mouse.current`'ın konumunu izler (Update'te), çarpıştırıcısı bir kare geriden
  gelir (`autoSyncTransforms` kapalı). Sentetik tık-tık'ta ikinci tıktan önce sanal fareyi `QueueStateEvent` ile hedefe taşı ve bir kare
  bekle; yoksa ikinci tık eski yerdeki başka çarpıştırıcıya düşer (Aşama 6'da büyüteç `inspectApplicant papers` yazıyordu; büyüteç
  aşama 7'de söküldü).
- **Input System 1.19 tık kuralı:** basış hiyerarşideki ilk `IPointerDownHandler`'a, tık en yakın `IPointerClickHandler`'a gider; bir
  işleyicinin çocuğundaki işleyici (istasyon altındaki damga, aday görünümü) kendi tıkını alır. Sentetik tıkı bu kuralla kur
  (`AdmissionPlayKit.ClickHit`); `BoardPlayKit.ClickHit` tıkı yalnız basılan nesneye verir.
- **Model prefab'ının ışık yönü tasarımın hedefi olmayabilir:** manifestteki ışık noktası yalnız yeri verir; `CounterLamp` spotu dik
  aşağı bakıyordu, tasarımda tezgâhın ortasına ~11° eğik. Işığı koddan hedefe çevir (`AdmissionStation.AimCounterSpot`) ve referansla
  bölge parlaklığını karşılaştır.
- **Kısmen gizli World Space kâğıt:** ağza girmiş kâğıdın görünmeyen kısmını dörtgeni kısaltıp yüz tuvaline `RectMask2D` ekleyerek kırp;
  World Space uGUI'deki TMP `RectMask2D`'ye uyar. Kâğıdı olduğu gibi bırakmak plakayı örter. (Geçmiş: örneği talep defterinin
  `RequestFormFace.ShowBottom`'uydu; aşama 7'de söküldü.)
- **Sabit model yuvasında iki dil:** "punto küçülmez, sarar" kuralı modelin sabit plaka yuvasında (raf plakaları, damga sapları, teneke
  şablonu) EN'de sözcüğü harf ortasından böler (RATION/S, ADMIS/SION). Tasarım puntosunu üst sınır yap, TMP otomatik punto (en az
  %45–70), sarma kapalı; değişikliği sahne örneğine TMP geçersiz kılması olarak yaz, punto taramasına gerekçeli istisna ekle.
- **Testte ölçüm çerçevesi:** katlı tezgâhın (Top 88°) altındaki parçayı dünya y'siyle ölçmek işareti ters gösterir; hareketi parçanın
  kendi ebeveyn çerçevesinde ölç (`InverseTransformPoint`). Kübik EaseOut'un son 2,5 karesinde kalan yol mikron düzeyindedir; sabit eşik
  yerine eğrinin kendisiyle karşılaştır.
- **`SerializedObject` yinelemesi Unity iç alanlarını da verir:** `m_CorrespondingSourceObject`, `m_PrefabInstance`, `m_PrefabAsset`
  prefab varlığında doğal olarak boştur; "bütün alanlar bağlı mı" denetiminde derinlik 0'daki bu adları atla.
- **Tepe anı örneklenmez:** tutmasız bir canlandırmanın tepesi kare örneğine düşmez; PlayMode testinde tepe olarak olay sesinden sonraki
  en büyük örneği al ve ardından gelen evreyi tepe değerinden başlat (aşama 6'nın buğu camı örneği; cam aşama 7'de emekli).
- **Uç değerde `Mathf.Approximately` ile "değişti mi" sınaması son adımı yutar:** canlandırma 0,99999'dan 1'e geçerken "aynı" sayılır,
  uçtaki `PartDriver.Set` hiç çağrılmaz (defter kapağı görünüşte açık, değeri 0). Uygulanan değeri tam eşitlikle karşılaştır.
- **Sürükleme sırasında alıcı değişebilir:** defter tarif kopyasını kaldırınca `eventData.pointerDrag`'i karta çevirir. Sentetik
  sürüklemede drag/endDrag'i her adımda `ped.pointerDrag`'e gönder (`BoardPlayKit.PointerGesture` böyle).
- **İşleyicisiz alt çarpıştırıcılar kapalıyken de ışını yutar:** kişi dosyası/KAYIPLAR sayfa yüzeyleri dosya kapalıyken açık kalıp
  arkadaki pano tıkını engelliyordu; yalnız sayfa açıkken açılır (`PersonFolderView.UpdateColliders`).
- **Işıklı TMP gölgelendiricisi (`TMP_SDF-URP Lit`) World Space uGUI'de (`TextMeshProUGUI`) mürekkebi soldurur:** 3B `TextMeshPro`'da
  (plakalar) doğru, uGUI tuvalinde koyu yazı kâğıdın tonuna yaklaşır (Aşama 4 devir fişi: #1E1B16 → ~(175, 166, 151)). Kâğıt/kart
  tuvallerinde fontun kendi (ışıksız) malzemesini kullan.
- **PlayMode örneği `LateUpdate`'ten önce alınır:** eşyordamdaki `yield return null` sonrası okunan konum önceki karenin `LateUpdate`'idir.
  `LateUpdate`'te yürüyen nesneyi (devir fişi) ölçerken örneğin zamanını bir önceki karenin zamanı say ya da `WaitForEndOfFrame` kullan;
  test koşucusunun düşük kare hızında bir kare hareketin yarısı olabilir.
- **Tam karanlık bir kare çizilmeden komut ve kamera kesmesi yapma:** alfa 1 aynı karede geri düşerse (ağır kare) siyah hiç görünmez.
  `alpha = 1` → `yield return null` → komut + kesme → `yield return null` → açılma (`SiginakShiftClose.FilingFlow`).
- **TMP `preferredWidth` / `GetPreferredValues` 0,01 birime yukarı yuvarlanır.** Tuvalde (1 birim = 1 mm) önemsiz, metre
  ölçekli 3B TMP'de 10 mm: altı harflik İNCELE "20 mm" ölçülüp boşuna küçülüyordu. 3B sözcüğü ölçerken
  `ForceMeshUpdate` + `textBounds.size.x` kullan (`CardStateOverlay.Fit`).
- **Işıklı TMP malzemesinin dış çizgi renkleri saydam olmalı.** `TMP_SDF-URP Lit` gölgelendiricisinin varsayılan
  `_OutlineColor1` camgöbeği; genişlik 0'ken de yumuşak kenar piksellerine karışır ve küçük koyu yazı mavi-yeşil görünür
  (Aşama 3'te pano plakaları). Yeni ışıklı TMP malzemesinde `_OutlineColor1–3` = (0, 0, 0, 0).
- **Işığın kendi siperi gölge düşürür.** Model gövdesine gömülü lamba siperi (pano lambası) gölge haritasına girip panoya V
  biçimli gölge çiziyordu; ışığın `shadowNearPlane`'i siperi dışarıda bırakacak kadar (0,1) olmalı.
- **İç içe model prefab'ında parça taşıma geçersiz kılma yazar;** kart prefab testleri bunu yasaklar (model hattı kaynaktır).
  Parçanın yerini sunumda değiştirmek gerekirse bileşen sürer (`CardStateOverlay` bayrağı 6 mm yukarı koyar).
- **Bileşende `??` çalışmaz:** `go.GetComponent<T>() ?? go.AddComponent<T>()` Unity'nin “sahte null”ı yüzünden eksik
  bileşeni eklemez (`GetComponent` C# null değil, yok edilmiş gibi davranan bir nesne döner); açık `!= null` denetimi yaz.
- **`SaveAsPrefabAssetAndConnect` prefab varlığını döndürür,** sahnedeki örneği değil. Sahne nesnelerinin bağlarını (kamera
  çapası, bağlayıcı alanları) örneğe kur (kaydettiğin nesnenin kendisi örnek olur); varlığa bağlanan alan Play'de prefab
  varlığını gösterir ve testte `AreSame` kırılır. Örnekte bir alanı değiştirdikten sonra
  `PrefabUtility.RecordPrefabInstancePropertyModifications` çağır, yoksa geçersiz kılma kaydedilmeyebilir.
- **3B TMP'li prefab'ın sahnedeki örneği** `m_AddedComponents` altına `addedObject: {fileID: 0}` girdileri yazar (TMP her
  yazıya kaydedilmeyen gizli bir `MeshFilter` ekler). Zararsızdır; ayarlar dolabının örnekleri bu yüzden ~24 boş girdi taşır.
- **`run_tests` CLI'da 30 sn'de zaman aşımına uğrar:** bütün takım için global `--timeout` ver
  (`unity command --timeout 1500 run_tests --mode EditMode`); tek sınıf için `--filter Corrigible.Desk.Tests.SettingsCabinetTests`.
- **URP ışımalı malzeme:** `globalIlluminationFlags = None` olan URP Lit malzemesinde `_EMISSION` anahtar sözcüğü test
  koşusunda bellekte sıfırlanır; dosyada durur ama oyun bellekteki nesneyi çizer ve bir sonraki `SaveAssets` diske de
  yazar. Projedeki gibi `RealtimeEmissive` ver (`Rev3MaterialSetup`).
- **Yeni URP malzemesi ilk kayıtta “yükseltilir”:** `new Material` + `CreateAsset`'ten sonra URP postprocessor'ı anahtar
  sözcükleri o anki değerlerden kurar. Değerleri kayıttan sonra bir kez daha yaz.
- **Transform'u olan nesneye `TextMeshPro` / `RectTransform` eklemek eski `Transform`'u yok eder;** elindeki `Transform`
  referansı geçersizleşir. Önce `GameObject`'i tut, konumu ve dönüşü bileşenden sonra `RectTransform`'a yaz.
- **3B TMP'nin `MeshFilter`'ı** modelin sınır kutusuna ve üçgen sayısına karışır; geometriyi sayarken `TMP_Text` taşıyan
  filtreleri dışarıda bırak (`Rev3ModelImporter.GeometryFilters`).
- **Revizyon 3 paketi:** manifest kutusu 13 modelde geniş (three.js `setFromObject`), devir fişinin OBJ'si bozuk (indeksli
  `PlaneGeometry`); doğrulama OBJ köşelerine karşı, fiş içe aktarıcıda onarılır (IMPLEMENTATION_MAP › Aşama 1).
- **Tek seferlik sahne script'inde aynı ada sahip yeni nesne:** eskisini, yenisini koymadan önce sil (adla silen yardımcı
  yeni örneği de götürür). Sildikten sonra `Rev3SceneRefAudit.Compare` ile boşalan referansları denetle.
- **`SiginakCapture.Render*` çıktıyı `Docs/UI_Siginak_Rev2/screens_unity_rev2/`'ye yazar;** deneme görüntülerini oradan
  geçici klasöre taşı.
- **three.js referans görüntüleri** her malzemeye ortam haritası (`envMapIntensity` 0,22) ve ACES ton eşleme uygular;
  kameraya bakıp ışık almayan dikey yüzeyler (teleks kâğıdı, plakalar) orada aydınlık. URP'de yalnız ortam ışığı (Trilight)
  karşılık; kalibrasyonu bölge parlaklığı ölçerek yap (Aşama 2, IMPLEMENTATION_MAP §8).
- **three.js `SpotLight` açısı yarım açıdır:** token ya da manifestteki "58°" Unity'de `spotAngle` 116 olur (masa lambası 47 → 94).
- **Revizyon 2 fikstürleri (`SiginakFixtures`) pano kipinde gün ilerletemez:** `Step` fiş dosyalanmadan durur; fikstür artık hata
  verir (eskiden sonsuz döngüyle editörü kilitliyordu). Geç masa görüntüsü için `CampaignWear.Show(31, true)` yeter.
- **Işımalı kadranın dibindeki nokta ışığı** (birkaç cm) camda beyaz bir leke yapar; ışığı 15–20 cm öne al.
- **`DeskTarget.Blocked` statik:** her sahne kendi kilidini kurar (DeskGame'de `SiginakDesk`, menüde `EntranceController`)
  ve sahne kapanırken boşaltır. Odanın kilidi açıkken çalışması gereken parça (ayarlar dolabının kontrolleri)
  `BlockedOverride` kullanır.

- **TAA ve ince geometri:** URP TAA her karede alt-piksel jitter uygular; ekranda ~2 px'ten ince nesne TAA açıkken titrer
  (masa pozunda 2 m uzakta 1 px ≈ 2 mm). Odaya ince nesne (ip, kablo, çubuk) eklerken ekranda en az ~3 px yap (masa pozunda
  ≥ 6 mm). Kameranın TAA ayarı DeskGame'deki `Main Camera`'da (Very High, temel karışım 0,95, kıskaç 1,25); URP
  varsayılanına dönerse bütün kenarlar titrer. TAA'yı ölçerken aynı karede `cam.Render()`'ı ikinci kez çağırma: URP o
  karede geçmişi zaten güncellediği için bir kez daha karıştırır ve oyuncunun görmediği bir kare çıkar. Kamerayı bir
  RenderTexture'a bağla, motorun kendi çizimini `EditorApplication.update`'te `Time.frameCount` değişince oku.
  `CorrigibleDebug.Capture` tek kare çizdiği için titremeyi göstermez.
- **Baskın testinde kaçışı elle kurmak:** taşıyıcıyı elle yazan test (`state.Carrier = …`) `state.Escape = true`'yu da
  yazmalı; kaçış bayrağı olmadan çıkış tahliye etmez, süre dolunca kaçış öncesi gibi kaybedilir. Kaçışta tahliye edilen
  uzman `Units`'te kalır ama `UnitAt`/`Alive` onu görmez; birim aramalarında `!u.Dead` yerine `Baskin.OnBoard(u)` kullan.
  Geri alma birimleri kopyasıyla değiştirir: `Undo`'dan sonra eski `Unit` başvurusunu yeniden bul.
- **Baskın botu puanı sıçramamalı:** botun puanında bir aşamaya (kaçış) geçerken yeni bir terim birden eklenirse bot o
  hamleyi hiç yapmaz (kaçışta öteki uzmanların yolu yalnız kaçışta sayılınca ikmal kasası açılmadı, başarı %93 → %41). Her
  terim iki aşamada da aynı ağırlıkla sayılmalı.
- **Layout grubunun çocuğu:** `HorizontalLayoutGroup` altındaki nesnenin konumunu `anchoredPosition` ile değiştirmek çalışırken
  ezilir; boşluğu grubun `padding`'iyle ver. `KeyChipView` kendi `fontSize`/`height` alanını etikete uygular; puntoyu orada değiştir.

- **Testler açık sahneyi kirletir:** sahne testleri (`DeskGameScene_*`, `Siginak*Tests`) sahneyi `Additive` açar; sahne
  editörde zaten açıksa aynı bellekteki sahneyi kullanıp nesnelerini değiştirir ve Test Runner kirli sahneyi kaydedebilir.
  Testleri boş sahne açıkken çalıştır; sahneyi koddan değiştireceksen diskten yeniden aç.
- **Play açıkken `run_tests` komut sunucusunu kilitler.** Kullanıcı oyunu editörde oynayıp Play'de bıraktıysa boş sahne
  açılamaz (`NewScene` Play'de çalışmaz), Test Runner sahneyi kaydedemeyip takılır; ardından `eval`, `editor_stop`,
  `recompile` hepsi 30 sn'de zaman aşımına uğrar ve yalnız Unity'yi yeniden açmak çözer. Testten önce
  `EditorApplication.isPlaying`'i denetle; Play'deyse kullanıcıya sor.
- **Eğik kâğıtta sert kenarlı sprite kullanma.** Ekran katmanı kenar yumuşatmaz; dokunun kenarına kadar opak 9 dilim
  sprite eğik çizilince basamaklanır ("kırık kenar", en çok koyu düğmede ve kırmızı çizgide). Eğik kâğıda eklenen zemin,
  düğme, gölge ve çerçeve `*_aa` türevini (`ui_fill_r2_aa`, `ui_frame_1px_r2_aa`…), 1–2 px çizgi ise görselini 2 px kalın
  bir "Soft line" çocuğunda (`ui_line_h_aa`, `ui_line_v_aa`, `ui_dash_h_aa`) kullanmalı; `SiginakSoftEdgeTests` yakalar.
- **Uzun bot ölçümü yanıtsız kalır.** `RaidBotMeasurementTests.Measure_Quick` (`--include_explicit true`) ~25 dk tek
  eşzamanlı test olarak koşar; Pipeline kendi süresi dolunca koşuyu iptal edilmiş sayar ("Ignoring RunFinished from
  cancelled run"), test yine biter ve `Temp/raid_bot_quick.md`'yi yazar ama CLI yanıt almaz ve editor bir süre (20 dakikayı
  bulabilir) hiçbir komutu almaz. Arka planda başlat, bitişi raporun değişme zamanından izle, sonra `ueval` yanıt verene
  kadar bekle; bu arada C# değiştirme (yeniden derleme koşuyu bozar).
- **Büyük sahne düzenlemesi Editor'ü çökertebilir.** Çöküşten sonra Editor açılışta bir iletişim kutusunda bekler (CPU
  %0, `Editor.log` lisanstan sonra ilerlemez): çoğunlukla `Temp/__Backupscenes`'teki yarım sahneyi geri yükleme sorusu.
  Geri yükleme; Editor'ü kapat, `Temp/__Backupscenes` ve eski `Temp/UnityLockfile`'ı kenara taşı, yeniden aç.
- **Test assembly'sindeki MonoBehaviour** (`includePlatforms: Editor`) `AddComponent` ile eklenemez (null döner). Testte
  bileşen gerekiyorsa çalışma zamanı assembly'sindekini kullan (ör. `LiveRaidDataSource.OpenForValidation`).
- **Dosya silmeden önce içindeki bütün tipleri ara:** bir dosya adıyla aranan sınıf kullanılmıyor görünse de aynı dosyadaki
  başka tipler kullanılıyor olabilir.
- **Lokalizasyon:** statik `readonly` alanda `Loc.T` kullanma (dil, sınıf ilk yüklendiğinde donar; `Loc.N` + gösterimde
  `Loc.T`). Metne bakarak karar verme. Testte `RegexOptions.IgnoreCase` Türkçe `ı`'yı `I` ile eşleştirir.
- **Enum'lar asset'te sayı olarak saklanır:** `Cue`/`LoopCue` üyelerinin sayıları sabittir; araya değer ekleme, silinen
  sesin numarasını yeniden kullanma.
- **Image layout'u bozar:** sliced sprite'lı `Image` bir `ILayoutElement`'tir ve sprite kenarlarını minimum boyut olarak
  bildirir. Zemin Image'ı ayrı bir `Background` çocuğuna koy ve `ignoreLayout` yap.
- **Belirip kalkan nesneye `Vector3.one` yazma:** sahnede doğrudan ölçeklenmiş bir mesh (ör. maketin plakası, 0,006 × 0,07 ×
  0,05) 1 m'lik küp olur. Sahnedeki ölçeği Awake'te sakla ve ona göre canlandır (`ShelterStore.rest`; sahne testi Aşama 10'da maketle kalktı).
- **DeskGame klavye tuşu yazmaz:** yeni kâğıda ya da düğmeye “Enter”, “Esc”, tuş fişi koyma; kısayol çalışır ama her
  birinin tıklanan karşılığı olmalı (`SiginakDeskTests.DeskGameScene_ShowsNoKeyboardHints` yakalar). Baskın tahtası hariç.
- **Bir dosyada iki MonoBehaviour:** sahne kaydedilince ikincisi "Missing Script" olur. Dosya adı = sınıf adı.
- **`childForceExpandHeight`:** HorizontalLayoutGroup'ta açıkken grup esnek yükseklik bildirir; satıra
  `LayoutElement.flexibleHeight = 0` ver.
- **TMP harf aralığı:** `characterSpacing`, CSS piksel değerinin yaklaşık `px / fontSize × 50` karşılığıdır (100 değil).
- **Statik font atlası boşluğu içermeli;** yoksa her boşluklu metin dinamik yedekten ayrı draw call açar (testli).
- **`capture_game_view` / Editor ekran görüntüsü** arka planda güvenilmez; `CorrigibleDebug.Capture` kullan. Proje dışına
  (ör. /tmp) yazamaz; `Temp/` altına yaz.
- **Edit modda sahne kurulumu:** `EditorSceneManager.NewScene` kullanılmayan asset'leri boşaltır; asset referanslarını
  sahne oluşturulduktan sonra yükle.
- **Dönük nesnenin çarpıştırıcısı:** `Renderer.bounds` dünya eksenlidir; 35° dönük bir nesnede derinliği de kapsar. Kutu
  çarpıştırıcıyı nesnenin kendi ekseninde, mesh sınırlarından hesapla (`SiginakRoomTests.RoomTargets_CollidersFitTheirModels`).
- **Kapalı kameranın ışını:** `PhysicsRaycaster` kameradan ayrı bir bileşendir; kamera kapalıyken de ışın atar. Baskında
  odanın ışını açık kalırsa tahtanın parçaları "arayüz" sayılır ve karelere tıklanamaz; `RaidLauncher.hideDuringRaid`'de
  durur (testli). Tıklama yolunu doğrularken `RaidDataSource` metotlarını doğrudan çağırma: fareyi
  `InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = cam.WorldToScreenPoint(RaidBoard3D.W(x, y)), buttons = 1 })`
  ile sür.
- **Tahtada tıklama yükseklik ister:** karonun çarpıştırıcısı görünen gövdesine uzanmalı (duvar bloğu 0,55; testli), yoksa
  tepesine tıklayan arkadaki kareyi seçer. Kareyi seçen kural `RaidBoard3D.Pick` (vurgusuz duvar arkasındaki vurgulu
  kareyi bırakır, eğitim çerçevesi ışın üstündeyse kazanır). Tıklamayı doğrularken karo merkezine değil oyuncunun
  tıklayacağı yere (gövde, blok tepesi) tıkla.
- **Vardiya kaydı vardiya sonunda alınır:** işgal geçmişi (`S.Hist`) yalnız `Step`'te yazılır; vardiya içinde değişen
  değeri gösteren sunum (harekât sonu raporunun şeridi) güncel değeri ayrıca ekler.
