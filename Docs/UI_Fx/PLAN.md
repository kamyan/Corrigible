# Son Şalter · FX arayüzü · F0 keşif ve uygulama planı

19 Ekim 2026. Durum: F0–F7 ve canlı entegrasyon tamamlandı. Faz kanıtları F1–F7_NOTLARI.md ve captures/ altında; güncel canlı giriş/komut/rapor akışı CANLI_ENTEGRASYON.md içinde. Aşağıdaki plan tarihsel tasarım kapsamını korur.

Kaynak: `SonSalter_Fx_Paketi/PROMPT.md` (1.0). Kullanıcının eklediği metin bu dosyayla, sondaki boşluklar dışında aynıdır. Paket kökü `Docs/UI_Fx/SonSalter_Fx_Paketi/`; aşağıdaki tasarım yolları bu köke göredir. Sade kökü `Docs/UI_Sade/SonSalter_Sade_Paketi/`.

## 1. Paket özeti

1. FX, mevcut Sade yerleşimi ve komutlarının üzerine ayrı bir görsel katmandır; ekonomi, araştırma, baskın kuralları ve kayıt biçimi bu paketin kapsamına girmez.
2. Toplam 19 kare vardır: F-YZ laboratuvarı, F-Masa1–9, F-Baskin1–9. F-YZ oyuncunun ana ekranı değil, geliştirme/doğrulama sahnesidir.
3. Haritada her bölgenin görünen YZ düzeyi parçacık küresiyle gösterilir: altın UYUMLU, karışık KARARSIZ, mavi kristal UYANMIŞ. Bilinmeyen, karantina ve çekirdek özel türlerdir.
4. Küre motorunun kanonik kaynağı `art/orb/fx_orb.js`'tir. Sprite dizisi, video, Shuriken veya VFX Graph ile taklit edilmez; sabit adım ve ayrı RNG ile C#'a taşınır.
5. Harita çokgenleri korunur. Nokta matrisi, bölgesel parıltı ve karantina deseni kürenin görünen düzeyiyle aynı anda değişir; gerçek gizli sızma okunmaz.
6. Kâğıt paneller yerini koyu cam, parantez, neon kenar ve süpürme ışığına bırakır. Chakra Petch, IBM Plex Mono ve IBM Plex Sans kullanılır; Sade'den yalnız izinli beyaz simgeler alınır.
7. Holo Baskın mevcut tahta geometrisi ve kamera düzenini kullanır. Yüzler Unlit, kenarlar ayrı geometri; varsayılan ışıma ikinci yarı saydam kenar geçişidir, post-processing Bloom eklenmez.
8. 27 giriş ve 35 döngü tanımı tek `FxClock` üzerinden yürür. Oyun hızı görsel saati değiştirmez; her nesne için ayrı Update kurulmaz.
9. “Hareketi azalt” döngüleri tanımlı evrede durdurur, tarama ışıklarını gizler, küreleri 8 fps'e indirir, girişleri atlar ve daktiloyu tam metin gösterir.
10. Kabul: 22 altın küre karesi; maskeli 19 ekran karşılaştırması; TR/EN, dört çözünürlük, hareket ve performans kayıtları. Ana masa ≤40 UI batch, Baskın ≤30; en kalabalık rehberde 19 küre ölçülür.

## 2. Proje incelemesi

**Durum A: Sade kurulu.** Yeni bir Sade altyapısı kurulmayacak. Son Sade F7 raporu 345/345 EditMode, 144 görünüm/dil/oran birleşimi, 11.344 metin denetimi, 24 sanal girdi ve 59 canlı köprü kontrolü içeriyor. Bu F0'da testler yeniden çalıştırılmadı; önceki doğrulanmış başlangıç olarak kaydedildi.

| Başlık | Yerel dosya ve editör incelemesi |
|---|---|
| Motor | Unity 6000.3.12f1 |
| URP | Paket 17.3.0; etkin asset `Assets/Settings/PC_RPAsset.asset` |
| Renk | Linear |
| HDR / SRP Batcher | İkisi de açık |
| Post-processing / Bloom | Açık DeskGame sahnesinin kamerasında post-processing kapalı, etkin Volume yok. Projede Bloom içeren başka profiller var; dosyanın varlığı etkin olduğu anlamına gelmez. |
| Arayüz | uGUI 2.0 + TextMeshPro; UI Toolkit'e geçilmeyecek |
| Girdi | Input System 1.19.0, yeni girdi sistemi |
| JSON | Newtonsoft 3.2.2 |
| Dil | `Core.Loc`, `DeskLoc`, `Resources/Localization/strings.txt`; TR kaynak + EN |
| Tween | Projeye ait değer tipi `DeskTween`; ikinci derece ease-out. FX'in cubic-bezier eğrisi bununla birebir aynı değil. |
| DI / async | Yerleşik DI çatısı veya oyun akışında async düzeni yok; serialized referans, olay, Update/coroutine düzeni var. Yeni paket gerekmiyor. |
| C bileşenleri | `DeskFormat`, `UILineGraphic`, `UIPolygonGraphic`, `ReferenceOverlay` ve genel giriş/olay/son belge altyapısı var. Kâğıt görselleri FX'e taşınmayacak. |
| Unity MCP | Kaynak sorgusunda bağlı instance sayısı 0. `ready_for_tools` alanı tek başına canlı Unity bağlantısı kanıtı değil. |
| Unity CLI | Pipeline 0.8.0-exp.1 çalışıyor; aynı proje Editor'ı üzerinden salt okunur sorgu başarılı. Proje rehberindeki CLI yolu kullanılabilir. |
| Açık sahne | İnceleme anında `Desk/Scenes/DeskGame.unity`, Edit modunda ve temiz. F0 sahneyi değiştirmedi. |

### Yeniden kullanılacak parçalar

- Veri: `SadeDeskViewModel`, `SadeDeskData`, `RaidViewModel`, `IDeskCommands`, `IRaidCommands`; Sade mock JSON/TextAsset'leri.
- Geometri: `MapProjection`, `MapCoverFitter`, `SadeMapData`, `RegionMeshGraphic`'teki çokgen/hit testi; `RaidBoard3D` geometrisi ve ekran etiketi izdüşümü.
- Davranış: saf `SadeInteractionState` seçim/çekmece/Esc sırası; komut niyetleri ve görünüm modeli kurulumu. Gerçek oyun için daha sonra `SimDeskBuilder`/`SimDeskCommands` ve `LiveRaidDataSource` sınırı.
- Test ve yakalama: Sade'nin tipografi, en-boy, girdi, gerçek Baskın dönüşü ve kayıt geri yükleme desenleri. F7'deki 30 batch masa / 50 batch kart açık ölçümü, FX performansı için garanti değildir.

### Ayrım gerektiren somut noktalar

- `SadeDeskInteraction` doğrudan `SadeRegionCardView`, `SadeDrawerFolderView`, `SadePaper`, `KillSwitchEnvelopeView` gibi sınıflara bağlı. Seçim/komut ile kâğıt yerleşimini aynı nesne yönetiyor.
- `RaidHudView` komut bağlama ve klavye ile birlikte kâğıt renklerini, sprite adlarını ve sabit boyları da yazıyor.
- `RaidDataSource` VM/komut sınırını taşıyor; fakat serialized `RaidHudView` ve `RaidBoard3D` bağlantıları da var. Yalnız farklı bir prefab bağlamak tam FX görünümü sağlamıyor.
- `DeskTween` eğrisini değiştirerek bütün Sade'yi etkilemek yerine FX için ayrı eğri değerlendiricisi kullanılmalı.

**Öneri:** F1–F2 yeni FX dosyaları ve ayrı sahnelerde kurulabilir. F3/F4 ve F5/F6 öncesinde gereken en küçük davranış/görünüm ayrımı somut değişiklik listesiyle sunulur; Sade koduna dokunma onayı alınır. Saf durum makinesi ve komutlar paylaşılır; kâğıt görünüm sınıfları FX sahnesinde kullanılmaz. İki bağımsız oyun kuralı veya iki Esc/komut uygulaması oluşturulmaz. Sade regresyonları her ayrımda çalıştırılır.

## 3. Kaynak envanteri ve inceleme kapsamı

- 19 ekran JPEG'i, 36 yakın plan, 9 H.264 video, 73 manifest sprite'ı ve 10 TTF mevcut. `art/sprites/` içindeki 74. PNG temas sayfasıdır.
- 22 altın küre PNG'si ve `golden.json`, sprite temas sayfası, tokenlar, çokgen/renk/VM ekleri ve holo paleti mevcut. 40 yeni/değişen TR/EN CSV girdisi var.
- Ekranların tamamı toplu görsel incelemeden geçirildi; sprite ve altın kare temas sayfaları incelendi. Prompt baştan sona, küre algoritması ve kanonik JS'nin kurulum/sabit adım/zamanlama sınırları okundu.
- Dokuz videonun her birinden başlangıç/orta/son örnekleri çıkarılıp incelendi. Süreler: holo tahta 8 s; makineler ve sonuç 5 s; canlı küre 19,5 s; üç hâl yakın 6,5 s; masa açılışı 6,8 s; bölge kartı 4,5 s; çekmece 4,17 s; günlük 4,37 s.
- Bazı masa ve Baskın video örneklerinde statik harita/tahta/panel katmanları görünmüyor; küre laboratuvarı örneklerinde bu sorun aynı biçimde yok. Bu yüzden eksik video katmanları hedef kabul edilmeyecek. Statik görünüm ekran/TSV'den, hareket süresi CSS/token/video birlikte değerlendirilerek alınacak. Uygulama karşılaştırmasında bu fark ayrıca raporlanmalı.
- `tools/orb_lab.html` için uygulama içi tarayıcı açılışı zaman aşımına uğradı; Safari erişimi de kullanıcı tarafından kesilen araç çağrısında tamamlanmadı. Canlı sürükleme etkileşimi bu F0'da doğrulanmış sayılmıyor. F2'nin gerekli kontrolüdür.
- `Docs/UI_Fx/AYNA_TASARIM_PROMPT.md` ayrı oyun tasarımını anlatıyor. Bu isteğin Son Şalter FX kapsamına yeni sunucu ekonomisi, iki YZ savaşı veya eşzamanlı döngü mekaniği eklenmeyecek.

## 4. Klasörler ve assembly sınırları

Projenin `Corrigible` adlandırması korunur:

| Yol | İçerik |
|---|---|
| `Assets/Corrigible/Fx/Scripts/Orb/` | Saf sayısal küre modeli, RNG, ikosküre, DrawList; `Corrigible.Fx.Orb`, `noEngineReferences: true` |
| `Assets/Corrigible/Fx/Scripts/Runtime/` | `Corrigible.Fx`: tema, saat, hareket, ortak cam bileşenler, OrbRenderer/View, masa ve Baskın görünümleri |
| `Assets/Corrigible/Fx/Scripts/Bridge/` | `Corrigible.Fx.Bridge`: Sade VM/mock ekleri ve sunum bağdaştırıcıları; gerektiğinde mevcut Desk.Bridge/Game/Core'a tek yönlü referans |
| `Assets/Corrigible/Fx/Scripts/Editor/` | Importer, TMP/tema/atlas ve doğrulama menüleri; `Corrigible.Fx.Editor` |
| `Assets/Corrigible/Fx/Tests/EditMode/` | Sayısal, geometri, VM, metin, asset ve regresyon testleri |
| `Assets/Corrigible/Fx/{Shaders,Prefabs,Art,Data,Settings}/` | Yeni shader, prefab, kaynak kopyası ve temalar; Sade asset'leri üzerine yazılmaz |
| `Assets/Corrigible/Fx/Scenes/` | `FxUI_Gallery`, `FxOrbLab`, `FxDesk_Sandbox`, `FxRaid_Sandbox` |
| `Docs/UI_Fx/` | Bu plan, kararlar, faz notları ve `captures/` |

`Corrigible.Fx` → saf Orb + mevcut Desk + uGUI/TMP/Input System. Desk assembly'si FX'e referans vermez; çevrim oluşturulmaz. Shader'da float kullanımı sunum katmanıdır; saf Orb hesapları JS eşleşmesi için double tutulur. Oyun Core'u değiştirilmez.

`Docs/` çalışma zamanında okunacak kaynak klasörü değildir. Gerekli JSON/TTF/dokular Editor importer'ıyla yeni FX asset'lerine alınır; tema JSON'dan ScriptableObject'e dönüştürülür. Kanonik JS ve referanslar Docs'ta kalır. Sade kaynakları mantıksal brif yollarıyla değil, mevcut paket/asset yollarıyla çözülür.

Sahne ve prefab'lar kalıcı kaynak, bağlantılar `[SerializeField]` olur. Yeni sahneler tek seferlik Editor betikleriyle kurulur, kurucu betik silinir; kalıcı çalışma zamanı hiyerarşi kurucusu eklenmez. F0'da Assets'e dosya yazılmadı.

## 5. Küre portu ve renk hattı

1. `OrbSimulation` kendi RNG durumuna sahip olur; `WorldState.Rng` kullanmaz. `seed*9973+17`, JS taşan uint aritmetiği, RNG tüketim sırası ve ikosküre kenar sırası korunur. `Math.round` eşdeğeri kullanılır; varsayılan C# bankacı yuvarlaması kullanılmaz.
2. Kurulum/kare ilerleme/DrawList üretimi ayrılır. Parçacıklar, kıvılcımlar, lifler, yaylar ve mesh tamponları önceden ayrılır; kare içinde LINQ, yeni listeler veya kapanış tahsisleri yapılmaz.
3. Kanonik sabitler aynen taşınır; `6.283` geçen yerler gerekçesiz `2*Math.PI` ile değiştirilmez. JS renk kuantizasyonu, tam sayıya dönüşüm ve alfa yuvarlaması görüntüyü etkilediği için karşılaştırmada izlenir.
4. `OrbRenderer`: Linear işaretli ARGB32 RT içinde gama değerleri eklenir (`Blend One One`). `OrbView` kompozitinde önceden çarpılmış alfa ve Linear projeye dönüşüm doğrulanır. İki kez gama dönüşümü yapılmaz.
5. Başlangıç yolu RT'dir; normal SS 1,5, altın yakalamada 2. Harita/panel üst sınırı 512, laboratuvar 1024. Kaynaklar yeniden kullanılır ve sahne kapanışında serbest bırakılır.
6. `OrbClock` 40 fps üst sınır, maliyette 16'ya kadar, azaltılmış harekette 8 fps; görünmeyen küreler güncellenmez. Yeniden görünmede uzun dt sıçraması ve giriş animasyonunun tekrar başlaması önlenir. Deterministik yakalamada adaptif bütçe ve gerçek saat devre dışıdır.
7. Önce RNG (3/5/7/9 tohumları, ilk 10 değer), 42/80/120 ikosküre ve kenar sırası, Sweep ve ağırlık örnekleri test edilir. Ardından 22 durumda `dt=1/40`, 40/100 adım, R=48, 422×422 çıktı alınır.
8. Her altın karede ortalama fark ≤6/255 ve kenar örtüşmesi ≥%70 aranır. Yakalamalar sırayla üretilir; 22 RT aynı anda tutulmaz.
9. Harita ve küre aynı `DisplayedLevel` değerini paylaşır. `OrbStateMapper` yalnız gösterilen düzey/güven/çekirdek bilgisiyle eşler. İllüzyon sırasında gizli gerçek değer bu katmandan okunmaz.

## 6. Hareket, shader ve bellek planı

- Tek `FxClock` / `FxLoopRunner`, global `_FxTime` ve veri tanımlı döngüler. FX giriş eğrisi ayrı tutulur; mevcut `DeskTween` değiştirilmez. `Freeze` modu ile referans evreleri ve “Hareketi azalt” ayrı durumlar olarak test edilir.
- `UI/FxAdditive`, `UI/FxLine`, `UI/FxRegion`, `UI/FxOrbComposite`; `Hidden/FxOrbDraw` ve dünya kenarı için `FxEdgeLine`. UI shader'larında stencil, RectMask2D, `_ClipRect`, alfa kırpma ve UI sıralama korunur.
- Bölge üçgenleri durağan kalır. Her kare vertex verisi değiştirerek tek batch uğruna Canvas rebuild üretilmez. Bölge kimliği sabit UV kanalında, 12 bölgenin değişen renk/düzey verisi ortak materyalin yeniden kullanılan parametre dizisinde taşınması önerilir. F3'te batch ve clipping ile doğrulanır; 12 ayrı materyalin maliyeti hedefe göre ölçülür.
- 12 bağımsız küre RT'si RawImage batch'lerini böler. 40 batch bütçesine shader/maske/10 TMP glow ön ayarının etkisi F1–F3'ten itibaren izlenir; son faza bırakılmaz.
- Haritanın iki 3840×2160 dokusu sıkıştırmasız RGB24+RGBA32 yaklaşık 55,4 MiB; ikisi BC7 olduğunda yaklaşık 15,8 MiB (mipmap olmadan). Yeni import ayarlarında PC yüksek kalite BC7 başlangıç önerisi; bantlaşma/alpha sorununda maliyet raporlanarak geri dönüş.
- Harita RT küreleri SS1,5'te tipik olarak toplam yaklaşık 4–6 MiB; gerçek tahsis Profiler'da ölçülür. Orb RT bütçesi ≤12 MiB. Font atlasları ve ana harita bu sayıya dahil değildir; ayrı raporlanır.
- Ana brif küre CPU'sunu ≤2,5 ms, ORB belgesinin bir bölümü ≤1,5 ms veriyor. Ana kabul ≤2,5 ms; 1,5 ms iyileştirme hedefi olarak tutulur. Hedef PC sınıfı belirtilmediği için bu Mac Editor ölçümü Steam PC sonucu olarak sunulmaz.

## 7. Fazlar ve çıkış kapıları

| Faz | Somut teslim | Kabul |
|---|---|---|
| F0 | PLAN ve KARARLAR, kaynak/bağımlılık incelemesi | Kullanıcı plan onayı |
| F1 | 10 font/TMP, 73 sprite/FxUI atlası, doku importer'ları, FxTheme, FxClock/Loop/Tween, ortak bileşenler ve FxUI_Gallery | Yeni asset'ler; üç boyda 9-dilim, tek saat, clipping/stencil, TR/EN ve temiz derleme |
| F2 | Birebir OrbSimulation portu, çizim hattı, altın yakalama ve FxOrbLab | 22 altın kare, sayı/RNG testleri, canlı geçiş ve 12 küre bütçesi |
| F3 | F-Masa1, bölge boyası, küre/değer/etiket, iplik/rota/jeton, ekran FX, HUD ve girişler | Maskeli F-Masa1; cover ve dört oran; ilk batch ölçümü |
| F4 | F-Masa2–9, kart/çekmece/ipucu/dosya, YZ hâlleri rehberi, eylemler, İllüzyon, basılı tutma | Ortak davranış onayı sonrası regresyonlar, panel kuralları, 8 karşılaştırma, hareket |
| F5 | Ayrı FX tahta malzemeleri, palet/kenar mesh'leri, holo tarama/titreme, etiket/niyet/düğüm küresi | Geometri/kamera ±4 px; palet ve video; mevcut Sade maket korunur |
| F6 | F-Baskin1–9 HUD ve durumlar | Girdi/makine kilidi, onay/sonuç/çöküş, 9 karşılaştırma; mock kaynak |
| F7 | EN/taşma, dört oran, azaltılmış hareket, CPU/GC/batch, UYGULAMA_NOTLARI | Tüm kontrol listeleri ve bilinen sapmalar açıkça raporlu |

Her faz sonunda kanıt ve kısa raporla durulur. Mevcut sahne/prefab, Sade kodu veya Project/Player/Quality/URP/Volume değişikliği gerekirse ayrıca somut kapsam sunulur. Yeni paket kurulması planlanmıyor.

**Canlı oyuna geçiş:** Bu brif mock görsel katmandır. `DeskGame` veya `Baskin`in FX'e geçirilmesi ayrı açık kullanıcı adımıdır; sandbox çalışması bu sahneleri otomatik değiştirmez. UiSkin geçişinin oyundaki bağlantısı da bu adımda kurulur. Baskın küresinin gerçek düzeyini bölgenin gizli sızmasına veya keyfî tur artışına bağlamak bu pakette yapılmaz; şimdilik VM/mock tablosu kullanılır.

## 8. Riskler ve bilinen karar gereksinimleri

1. **Sade bağımlılıkları:** somut view referansları ve kâğıt teması ayrılmadan FX'e bağlanamaz. F1–F2 bununla bloke değil; F3/F4 ve F5/F6 öncesi sınırlı ayrım için onay gerekir.
2. **Renk/AA:** Canvas2D sRGB toplama, shader Linear kompozit, kesik çizgi AA'sı ve JS kuantizasyonu altın eşleşmesinin en yüksek riskidir. F2 tamamlanmadan masa seri üretimine geçilmez.
3. **Performans:** 19 küre, RT geçişleri, çoklu font ve sürekli glow. Sade F7'de açık kart 50 batch'ti; yeni tasarımın 40 hedefi ayrıca kanıtlanmalıdır. Hedef tutmuyorsa “tamamlandı” kabul edilmez, sapma ve seçenekler sunulur.
4. **Görünüm eşliği:** mevcut Sade 3D referans farkları FX'e otomatik kabul edilmez. Yeni holo tahta köşeleri ve işaret merkezleri erken ölçülür.
5. **Kaynak videolar:** bazı görünmeyen katmanlar nedeniyle video statik yerleşim kaynağı yapılmaz. F2’de yerel Chrome laboratuvarı ve 0→100 sürükleme doğrulandı.
6. **Bölge eşlemesi:** 12 tasarım bölgesi oyundaki 12 kimlikle birebir değil; Sade'deki A eşlemesi korunur. Referans tohum sırası tasarım kimliğine bağlı, gerçek bölge ID'sine göre keyfî yeniden sıralanmaz.
7. **Terim:** mevcut güven anahtarı ONAYLANDI, FX çiziminde DOĞRULANDI. Kullanıcı FX için DOĞRULANDI terimini seçti; mevcut Sade metinleri korunur.
8. **AYNA belgesi:** farklı oyun brifi kapsam dışıdır; görsel geçiş sırasında kural değişikliği yapılmaz.

## 9. Onaylanan F1 kapsamı

**F1 teslimi:** Yalnız yeni `Assets/Corrigible/Fx/` altyapısı, fontlar, shader'lar, tema ve galeri. Mevcut Sade sahneleri/prefab'ları, oyun kuralları, URP/Volume ve Project ayarları değiştirilmez. Küre motoru ve laboratuvar F2'dir.

Brif §0: “Onay almadan kod yazma.” Kullanıcı “1.Başlat, 2.Doğrulandı” yanıtıyla F1 uygulamasını onayladı; diğer açık kararlar `KARARLAR.md`'de brifin varsayılanlarıyla kayıtlıdır.

## 10. F1 sonucu

357/357 EditMode ve 27/27 Play kontrolü geçti. Yeni FX galerisinde 17 ortak prefab, 10 statik TMP fontu, 8 ışıma ön ayarı, 73 FX sprite’ı ve 108 beyaz simge bulunuyor. Güven terimi DOĞRULANDI / VERIFIED. Saatin 1.000 doğrudan güncellemesinde 0 B yönetilen tahsis ve dört çizgide sıfır geometri yenilemesi ölçüldü. Ayrıntı, bellek maliyeti ve kontrol sınırları `F1_NOTLARI.md` içinde. Bu F1 sonucu sonrasında kullanıcı F2’yi onayladı; aşağıdaki F2 sonucu günceldir.

## 11. F2 sonucu

Kullanıcının “F2 ye geç” talebiyle kanonik küre portu ve FxOrbLab uygulandı. 394/394 EditMode, 38/38 Play kontrolü ve 22/22 altın kare geçti. 12 harita küresi ortalama 2,346 ms, %95 2,496 ms, 0 B yönetilen tahsis ve 2,985 MiB RT kullandı (M4 Pro Editor/Metal). F-YZ maskeli renk farkı 5,01/255, kenar örtüşmesi %82,0. TR/EN ekranları ve 18 sn geçiş kaydı `captures/` altında. Ayrıntılar ve platform/görsel sınırlar `F2_NOTLARI.md` içinde. Başlangıçtaki temiz DeskGame, ayarlar ve oyuncu profili geri yüklendi. Bu F2 sonucu sonrasında kullanıcı F3’ü onayladı.

## 12. F3 sonucu

Kullanıcının “F3 e geç” talebiyle ayrı `FxDesk_Sandbox` sahnesi uygulandı. Sade VM/geometri/cover/hit testi ve saf etkileşim durumu yeniden kullanıldı; Sade kaynak kodu ve canlı sahneler değiştirilmedi. 400/400 EditMode, 44/44 Play denetimi ve 22/22 altın kare geçti. TR/EN dört çözünürlükte incelendi. Durağan sahne 33 UI batch; 12 F3 küresi ortalama 2,398 ms, %95 2,510 ms, 0 B tahsis; tüm FX döngüsü ortalama 2,641 ms ve 0 B (M4 Pro Editor/Metal). 6,80 sn açılış kaydı var. Ayrıntılar, Profiler çökme sınırı ve geri yükleme kanıtı `F3_NOTLARI.md` içinde. F4 başlatılmadı.

## F4 tamamlanma kaydı

Kullanıcının “F4 e geç / devam et” talebiyle F-Masa2–9 ayrı FX sahnesine uygulandı: bölge kartı, beş çekmece, YZ hâlleri rehberi, HUD/düğüm ipuçları, lejant, tam dosya, önizleme, klavye, onay ve İllüzyon. Ortak Sade VM/durum/komut sınırı korundu; Core/Prototype/canlı DeskGame kaynakları değişmedi. 409/409 EditMode, 74/74 Play ve 22/22 küre altın karesi geçti. TR/EN dört oranda 64 yakalama var. Son yerel Profiler'da 19 küreli rehber 36 batch ve FX betiklerinde 0 B/31 kare; 2,1 ms sıra bütçesiyle küre işi en yüksek 2,338 ms. Üç hareket kaydı ve sekiz maskeli karşılaştırma alındı. Kaynak başına mesh önbelleği, kırpma/örtüşme ve panel orb atlası kullanıldı; tek seferlik sahne yazıcısı kaldırıldı. Ayrıntılar ve sınırlamalar `F4_NOTLARI.md` içinde. F5 başlatılmadı.

## F5 tamamlanma kaydı

Kullanıcının “F5 e geç” talebiyle ayrı `FxRaid_Sandbox` sahnesi, holo yüz/kenar malzemeleri, plan/arka plan, tarama/titreme, zemin işaretleri, niyet akışı, dünya etiketleri ve düğüm küresi tamamlandı. Sade geometri/kamera ve mock parser paylaşılır; Core/Prototype/canlı sahneler korunur. 419/419 EditMode, 76/76 Play, dört oranda TR/EN72 görüntü ve0 taşma; kamera sapması0,026px. Sekiz saniyelik video doğrulandı. Sıcak saat+Canvas ortalama0,236ms/0B; GPU ve birleşik bütçe F7'de ölçülür. Tek seferlik yazıcı kaldırıldı. Ayrıntılar ve görsel farklar `F5_NOTLARI.md` içindedir. Bu kayıt sonrasında kullanıcı F6’yı onayladı.

## F6 tamamlanma kaydı

“F6 ya geç” talebiyle ayrı `FxRaidHud_Sandbox` ve F-Baskin1–9 HUD/durum akışları uygulandı. Ortak RaidDataSource/VM/komutlar kullanılır; makine çözümü bir kez hesaplanıp sunum kopyasında gösterilir. 436/436 EditMode, 102/102 Play, 75 TR/EN yakalama ve 0 taşma; iki 8 saniyelik video ve dokuz maskeli karşılaştırma alındı. Sıcak FX saat+Canvas 0,182–0,462 ms/0 B. Başlangıç FxDesk_Sandbox, profil/tercihler ve ayarlar geri yüklendi; tek seferlik yazıcı kaldırıldı. Piksel/ışıma/kutu farkları ile birleşik GPU/batch kabulü F7 için açıktır. Ayrıntılar `F6_NOTLARI.md` içindedir. F7 başlatılmadı.

## F7 uygulama ve kabul kaydı

Kullanıcının “F7 ye geç” talebiyle üç örnek sahnede ortak referans/denetim araçları, kalıcı azaltılmış hareket, metin düzeltmeleri ve iki kenar mesh'i uygulandı. 440/440 EditMode; 176 yakalama ve 13.392 metinde0 hata; 22/22 küre golden. Son native kayıtta Masa≤36 UI batch, Baskın30 UI batch; iki FX saatinde0 B; 19 küre CPU ort.2,164 ms. GPU süresi bu Metal Editor kaydında geçerli çıkmadı. Referansın katı piksel eşliği ve Windows PC/GPU kabulü açıktır; tüm kabul listesi yeşil değildir. Bileşenler/bağlama `UYGULAMA_NOTLARI.md`, ölçüm ve sapmalar `F7_NOTLARI.md` içinde. Canlı entegrasyona geçilmedi.
