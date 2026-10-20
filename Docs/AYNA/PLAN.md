# AYNA · Uygulama planı

Güncel: 20.10.2026 · F4 tamam. F5 ölçüm turu tamamlandı; genel denge kabulü açık. Yavaş ekonomi adayı F3 takasını bozduğu için reddedildi; F4 varsayılanları korundu. Son ölçümler [DENGE.md](DENGE.md) içinde.

Bu planın kural kaynağı [CODEX_PROMPT.md](CODEX_PROMPT.md) sürüm 1.0'dır. Kullanıcının eklediği dosyayla depodaki bu dosyanın SHA-256 özeti aynıdır. İkinci kaynaklar [oyun fikri](../YZ-SAVASI-OYUN-FIKRI.md) ve [A1–A12 tasarım brifi](../UI_Fx/AYNA_TASARIM_PROMPT.md). Çelişkide uygulama talimatı geçerlidir. §11 S1–S8 önerileri 19.10.2026 tarihinde kullanıcı tarafından kabul edildi. Bu bölümdeki soru biçimi F0 karar geçmişi olarak korunur; F1 bu önerileri uygular.

## 1. İnceleme ve kapsam

F0 okuma listesi tamamlandı: `AGENTS.md`, `Docs/MIMARI.md`, `Docs/GELISTIRME.md`, iki AYNA tasarım belgesi; eski `Core/Rng.cs`, `Loc.cs`, `GameData.cs`, `Baskin.cs`, `PlayerProfile.cs`, `GameRecord.cs`; `Game/ProfileStore.cs`, `GameController.cs`. Assembly ve mevcut paket listesi de kontrol edildi.

İnceleme başlangıcında çalışma ağacı temiz, dal `main`. Brifte örnek verilen commit edilmemiş eski oyun değişiklikleri bu oturumun başlangıcında görünmüyor; yine de eski dosyalara dokunma yasağı geçerli. F0'da Unity sahnesi açılmadı, Play/derleme/test çalıştırılmadı. Editörün kaydedilmemiş sahne durumu kontrol edilmedi; bu belgeden temiz olduğu sonucu çıkarılamaz.

Mevcut koddan alınacak kalıplar:

| Kaynak | Kullanım | AYNA'daki fark |
|---|---|---|
| `Rng`, `Js.Round`, `Loc` | İzin verilen ortak yardımcıları asmdef referansıyla kullan | Başka eski oyun tipi çağrılmaz |
| `GameData` | 12 bölgenin koordinatlarını, 18 bağlantıyı, 5 uydu hattını kopyala | Yeni adlar, kapasite ve veri aileleri AYNA kataloğunda bağımsızdır |
| `Baskin` | Derin kopya, komut öncesi anlık görüntü, kopyada önizleme fikri | Birim, niyet, tur ve savaş kuralları yeniden yazılır |
| `GameRecord` / `Replay` | Tohum + başlangıç görüntüsü + komut geçmişi | Döngü ortası, bekleyen savaş ve taktik girdiler de yeniden kurulur |
| `PlayerProfile` / `ProfileStore` | Kalıcı profil ve oyun sonunda birleştirme fikri | Ayrı, sürümlü AYNA profili; eski profil okunmaz/yazılmaz |
| `GameController` | Tek komut kapısı ve değişim bildirimi | Gerçek zaman saati yok; iki dış plan ve duraklatılabilir çözüm var |

F0 yalnız bu planı, karar kaydını ve durum raporunu üretir. F1–F5 yeni `Assets/Corrigible/Ayna/` altında ilerler. Eski Core/Game/Desk/Fx, sahneler, prefab'lar ve Build Settings korunur. Ortak TR/EN tablosuna ileride yalnız AYNA çevirileri eklenir. Yeni paket, sistem aracı veya proje ayarı değişikliği öngörülmüyor; gerekirse önce kullanıcıya sorulur. Fx arayüzü, sanat, ses, ağ kodu ve eski oyunun kaldırılması kapsam dışı.

## 2. Assembly ve klasörler

| Yol (`Assets/Corrigible/Ayna/`) | Assembly | Bağımlılık / sorumluluk |
|---|---|---|
| `Core/` | `Corrigible.Ayna.Core` | Yalnız eski Core'dan izinli üç yardımcı; `noEngineReferences: true` |
| `Mind/` | `Corrigible.Ayna.Mind` | Ayna.Core ve gerekirse izinli yardımcılar; `noEngineReferences: true` |
| `Mind/Bots/` | Mind içinde | Yedi test botu, üretim koduyla aynı görüş sınırı |
| `Game/` | `Corrigible.Ayna.Game` | Ayna.Core, Ayna.Mind, uGUI, TMP, Input System |
| `Editor/` | `Corrigible.Ayna.Editor` | Yalnız Editor; Core, Mind, Game; ölçüm ve tek seferlik sahne yazarlığı |
| `Tests/EditMode/` | `Corrigible.Ayna.Tests` | NUnit, yeni katmanlar; Core'un tek `InternalsVisibleTo` alıcısı |
| `Scenes/` | — | F4: `AynaDebug.unity` |
| `Settings/` | — | F4: `AynaBalance.asset` |

Core → Mind veya Game bağımlılığı kurulmaz. Oyunlar arası profilin seri hale getirilebilir **veri şeması** Core'da, model güncelleme ve seçim algoritması Mind'da durur. Böylece Core kaydı, Mind tipine ihtiyaç duymaz. Mevcut Newtonsoft JSON paketi kullanılabilir; saf veri serileştirmesi için Core asmdef'inde gerekli DLL referansı doğrulanır, yeni paket kurulmaz.

Her MonoBehaviour / ScriptableObject kendi dosyasında bulunur. F4 sahnesi tek seferlik Editor script'iyle oluşturulup kaydedilir, script silinir. Referanslar `[SerializeField]`; çalışma zamanında nesne adıyla arama veya kalıcı sahne kurucu yoktur.

## 3. Veri modeli ve API

Aşağıdaki adlar planlanan sınıflardır; F0'da oluşturulmadı. Sayısal oyun büyüklükleri `double`; kimlik, sayaç, tur ve ızgara indeksleri `int`, tohumlar `uint`. Yuvarlama `Js.Round`. Şema sürümleri ve enum değerleri sabit kimlikle saklanır.

### Core

| Tip | Temel alan / davranış |
|---|---|
| `AynaBalance` | §5 maliyetler, gelir, gürültü, eşikler, kademe/birim tabloları; otomatik güç ve kişilik katsayıları; N/K; derin kopya ve doğrulama |
| `AynaSettings`, `StartChoice`, `StartSetup` | Öğrenme düzeyi; iki mimari, sıralı başlangıç adayları, kabul edilmiş başlangıçlar, tohum |
| `AynaWorldData`, `RegionDefinition`, `LinkDefinition` | 12 sabit kimlik, konum, kapasite, veri ailesi; kara/kablo/uydu, Wrap; bağımsız kopyalar |
| `WorldState` | **Internal üyeler**: Cycle, Phase, Regions, Sides, Awareness, Outages, WorldRng, pending resolution, terminal outcome; internal `Clone()` / `Hash()` |
| `RegionState` | Free, Owned[2], Wall[2]; kapasiteyi koruyan sunucu transferleri |
| `SideState` | Architecture, Compute, CoreRegion, CoreMove, Tiers[5], Training, Singularity, Armies |
| `ArmyState`, `UnitState` | Kararlı kimlik, sahip, bölge, birimler; tür, mevcut can, durum etkileri |
| `Plan`, `Order` | Sıralı emirler; tür, bölge, birlik/birim, miktar, aile/kademe, hedef; plan kilitlendikten sonra değişmez |
| `PlanValidation`, `OrderReceipt` | Önizleme / otoriter doğrulama; kabul/ret kodu, ücret, gürültü, sıra |
| `AynaSimulation` | `Create(StartSetup, AynaBalance)`, `View(Side)`, `ResolveCycle(Plan player, Plan ayna)`; savaş seçimi ve tahta komutları |
| `CycleResolution`, `BattleRequest` | Çözüm imleci, kabul edilmiş emirler, savaş kuyruğu, uygulanan sonuç kimlikleri, ilk-vuruş hakkı |
| `BattleResult`, `AutoResolver` | Düğüm ve sunucu değişimleri, kalıcı hasar/kayıp, sivil hasar, çekirdek kırılması; aynı sonuç iki kez uygulanamaz |
| `SideView`, `RegionView`, `Observation` | Salt okunur kopyalar; kesin/yaklaşık/bilinmeyen bilgi, aralık, kaynak ve gözlem döngüsü; açılmış olaylar |
| `BeliefSetup`, `BeliefSimulator` | Yalnız görüş + varsayımlardan yeni örnek dünya; canlı dünyanın kopyasını kabul etmez |
| `BoardState`, `BoardView`, `BoardCommand` | F2: 64 hücre, birimler, niyetler, tuzaklar, düğümler, dolap; hareket/yetenek/tur/geri alma/çekilme |
| `AynaRecord`, `RecordedInput`, `AynaProfileData` | Sürüm, başlangıç görüntüleri, komut akışı ve saf profil DTO'ları |

Dünyada **72** sunucu vardır; hakimiyet için tam sayı sınırı **44**. Her bölgede `Free + Owned[Player] + Owned[Ayna] = Capacity`, negatif değer yok; yaşayan her çekirdeğin konumunda kendi sunucusu vardır. İzinli yardımcılar dışında eski `GameData`, `Simulation`, `Baskin`, `PlayerProfile` kullanılmaz.

### Mind

`MindSession` (kendi RNG'si, gözlem imleci ve modeli), `PlayerModel`, `PredictionDistribution`, `ForecastSnapshot`, `BeliefSampler`, `MacroPlanner`, `BoardPlanner`, `TrapPlanner`, `ProfileLearning`, `IPlanBot`.

Plan üretme girişi yalnız `SideView`, kendi `MindSession` geçmişi ve açık ayarlardır. `Plan` sonucu ve önceden kilitlenen tahmin görüntüsü döner. Mind'a mevcut oyuncu kuyruğu, gerçek `WorldState`, canlı kayıt veya gizli bölge verisi verilmez. Başlangıç kararında yalnız önceki profil ve herkese açık dünya kataloğu bulunur; diğer tarafın yeni seçimi bulunmaz. Minimum iki bağlantı uzaklık kontrolünü sonrasında Core yapar.

### Game

`AynaGameController`: başlangıç, plan taslağı, kilitleme, AYNA karar çağrısı, çözüm/savaş komut kapısı, Changed/Events bildirimi. Planlama taslağı dünya durumunu değiştirmez. `AynaSaveStore`, `AynaProfileStore`, `AynaBalanceConfig`, `AynaViewModelBuilder`, `AynaDebugView` ayrı sorumluluklardır. Kurallar ve maliyetler UI'da yeniden hesaplanmaz; Core önizlemesi gösterilir. Game hiçbir `WorldState` üyesine erişemez.

## 4. Döngü çözümü

Önerilen sıra §3.3'ü korur. İki ayrıntı açık hale getirilir: **AÇ verisi ücret/etki doğrulamasından sonra hazırlanır**; **komşuya Saldır hareketi Yürüt ile aynı eşzamanlı hareket adımına katılır**. Böylece geçersiz emir açılmaz ve saldırı hareketi karşılaşma önceliğini atlayamaz.

| Sıra | İşlem |
|---|---|
| 0 · PLANLA | İki plan ve tur başlamadan üretilmiş tahminler kilitlenir. AYNA, oyuncunun düzenlediği planı görmez. |
| 1 | Döngü başı durumuna göre emirler kendi liste sırasıyla doğrulanır ve ödenir. Sonradan kazanılacak gelir/Sürpriz bu bütçeye eklenmez. Geçersiz/ödenemeyen emir etkisizdir; ret nedeni sahibine görünür. |
| 2 | Ödenmiş Gizlen / Sahte yığınak bayrakları hazırlanır. Gizlen konumsal emirlerin gürültüsünü de sıfırlar; yalnız görsel gizleme olarak uygulanmaz. |
| 2a · AÇ | Kabul edilmiş emirlerin her tarafça görülebilen izdüşümü oluşturulur. Bayraklar AÇ'tan önce hesaba katılır; gizlenen emrin hedefi açığa sızmaz. Sürpriz değerlendirmesi için yalnız kabul edilmiş gerçek emirler dikkate alınır (§6 ve S2). |
| 3 · ÇÖZ | Yürüt ve komşuya Saldır hareketleri ortak başlangıç görüntüsünden uygulanır. Kenarda savaş yok. Nihai bölgede iki taraf varsa karşılaşma talebi oluşur. |
| 4 | Yayıl istekleri bölge bazında birlikte çözülür. Toplam istek boşa sığıyorsa ikisi alır; taşmada yüksek teklif önce, eşitlikte ikisi de sıfır. Ödenmiş fakat kazanılmamış teklif iade edilmez. Teklif tanımı S3'te. |
| 5 | Bölge başına tek savaş oluşturulur; karşılaşma önceliklidir. Bölge kimlik sırası, sonra birlik kimlik sırası kullanılır. Birliksiz savunma duvar otomatiğine gider. |
| 6 | Savaş kuyruğu çözülür. F1 tamamen otomatik; F2 sonrası komuta seçimi çözümü bekletir. Sonuçlar savaş kimliğiyle bir kez uygulanır; hasar, sunucular, duvar ve çekirdek kaçışı işlenir. |
| 7 | Duvar, eğitim, üretim, onarım, çekirdek taşıma ilerlemesi. Döngü başında ödenen emir sonradan koşulunu kaybettiyse davranışı makbuzla bildirilir; önerilen sunk-cost kuralı S3/S7'de. Yeni duvar bu döngünün savaşını savunmaz. |
| 8 | Yeni sunucu dağılımından gelir: sunucu başına 1, çekirdek bölgesine +2. |
| 9 | Gürültü × mimari çarpanı, ardından −2; farkındalık sınırları. Tarama, beş döngülük bağlantı kesintisi. Gürültü eşitliği/uygun bağlantı yokluğu kararlı şekilde ele alınır. |
| 10 | Son Şalter → Silme → Tekillik → Hakimiyet → 40. döngü. Ortak gerçekleşme testleri ayrıca yazılır. |
| 11 | Her tarafa yalnız gördüğü gözlemler teslim edilir; Mind güncellenir. Yeni öğrenme bir sonraki planı etkiler. |

`ResolveCycle` bitmeden yeni döngü kabul etmez. Bekleyen savaş varsa sonuç, aşama ve imleç korunur; tahta bitince aynı çözüm kaldığı yerden sürer. Önceki savaşın sonucu, aynı döngünün sonradan oluşturulmuş savaşına tekrar bütçe veya emir hakkı sağlamaz.

Önerilen teknik ayrıntılar (oyunu etkileyenleri §11'de onaya sunulmuştur):

- Kesinti son kullanma döngüsüyle tutulur; beş **gelecek planlama** döngüsünde kenarı kapatır. Hattın kesilmesi mevcut sunucuları silmez. Yeni kesinti aynı bağlantıdaysa süreyi en ileri bitişe uzatır. Gürültü eşitse aday taraf/bölge/kenarlar kimlikle sıralanıp WorldRng ile seçilir; aday yoksa RNG tüketilmez.
- Son Şalter denetimi azalmadan sonraki farkındalık değerine bakar; geçici toplam ve net toplam test edilir.
- Çekirdek kaybında kaçış aynı tarafın en çok sunuculu bölgesine, eşitlikte ordinal kimliğe göre olur. Dolap kırılması kaçış sağlamaz. Aynı döngü iki çekirdek silinirse öneri beraberliktir.
- Yeni eğitim/taşıma için kabul edildiği döngü ilk ilerleme sayılır; kademe eğitimi sırasında veri erişimini kaybetmenin etkisi S7'ye bağlıdır. Gelir, taşınma bittiyse yeni çekirdek konumundan hesaplanır.

## 5. Görüş, inanç ve determinizm

### Adalet sınırı

`WorldState` üyeleri internal; yalnız `Corrigible.Ayna.Tests` için arkadaş assembly. Mind/Game/Editor için arkadaş erişimi yok. UI ve AI'ya verilen nesne grafiklerinde gerçek duruma geri bağlantı, değiştirilebilir dizi veya gizli değer tutulmaz. `View(side, revealAll: true)` oyun sürerken reddedilir; yalnız terminal durumda ayrı sonuç incelemesi içindir. Oyun sonu açıklaması, sonradan gizli hamleleri profile öğretmez.

Görüş matrisi henüz kuralda eksik (S1). Modelde kesin sayı, tahmin aralığı ve bilinmeyen değer birbirinden ayrılır; bilinmeyen sıfır yapılmaz. Tahminler gerçek düşman sayısına gürültü ekleyerek üretilmez; son görülen bilgi ve erişilebilir gözlemlerden kurulur. Sahte yığınak `ObservedSignal` üretir; doğrulanmadıkça gerçek Saldır etiketiyle eğitilmez. Gizlenme sayısı da ancak görülebilir kanıt varsa öğrenilir.

### İnanç durumu

`BeliefSampler`, SideView'daki kendi kesin bilgilerini sabitler; bilinmeyen düşman sunucusu, birlik, eğitim ve çekirdeği bölge kapasitesi, son gözlem ve erişilebilirlik kısıtlarıyla örnekler. Gizli gerçek durumdan değer almaz. Core'daki `BeliefSimulator` yalnız bu örnek kurulumdan bağımsız simülasyon yaratır. Aynı `ResolveCycle` kuralları ve otomatik savaş uygulanır. İmkansız örnek reddedilir; sınırlı denemeden sonra deterministik, görüşle tutarlı bir tamamlama kullanılır, ölçümde sayılır. Gerçek dünya hiçbir zaman aday değerlendirmesinde klonlanmaz.

### Rastgelelik ve özet

- WorldRng, MindRng ve savaşın BoardRng'si ayrıdır. Her MindSession kendi tarafına ait akışı taşır; self-play'de bir botun çektiği sayı ötekinin akışını kaydırmaz.
- BoardSeed = oyun tohumu XOR döngü XOR bölgenin sabit sayısal anahtarı. `string.GetHashCode()` kullanılmaz. Bölge anahtarları kayıt şemasında sabittir; bölge başına tek savaş aynı döngüde tohum tekrarını önler.
- Mind adaylarının benzetim RNG'leri MindRng'den ayrılmış örnek kopyalarıdır; canlı WorldRng'ye dokunmaz. Aynı örnek için bütün K aday aynı benzetim tohumu kullanır.
- ID sırası ordinal; bölgeler, birlikler, birimler, komutlar ve olaylar kanonik sıradadır. Hash; bütün kural durumu, RNG durumu ve çözüm imlecini kapsar; çevrilmiş metin, süre ölçümü ve UI seçimini kapsamaz. Sayılar kültürden bağımsız, tam bit temsiliyle özetlenir.
- Önizleme canlı RNG/gözlem/profil/kayıt değiştirmez. Geri alma F2'de tahta RNG'si ve ilgili yerel olayları da geri getirir. Geri alınan eylem kalıcı öğrenmeye eklenmez.

## 6. Oyuncu modeli, tahmin ve planlayıcı

### Gösterim

İlk tercih **Dirichlet/Laplace düzeltmeli kategorik sayaçlar**; softmax veya sinir ağı yok. Emir türü, hedef veri ailesi, hedef bölge, mimari, açılış, saldırı zamanı, tepki, kadro, blöf ve normalize 8×8 ilk iki tur ısı haritası ayrı özelliklerdir. Seyrek bağlamda sırayla genel özellik ve ön dağılıma geri dönülür. Prior kütlesi, minimum örnek ve bağlam ağırlıkları AynaBalance'ta yer alır.

Gözlemin `Id`, `Cycle`, `Source`, `Confidence` ve açılan alanları tutulur; aynı gözlem tekrar sayılmaz. Gizli hamle başarı/başarısızlık etiketi oluşturmaz. Mimari/başlangıç ancak görülebilir şekilde ortaya çıktıysa öğrenilir. Oyun sonunda gizli gerçeklerin açıklanmasıyla eğitim verisi tamamlanmaz.

Oyun içi geçerli sayımlar devamlı güncellenir. Oyunlar arası dağılımlar gözlenen aileler için `0,65 eski + 0,35 bu oyun`; hiç gözlenmemiş aile korunur. Ham toplamları karıştırmak yerine normalize dağılım + gözlem miktarı saklanır; çok emir veren tek oyun bütün geçmişi ezmez. Hafif düzeyde yeni oyun ağırlığı 0,175 ve karşı hamle sınırı 1; Tam'da 0,35 ve 2. Kapalı'da disk profili okunmaz/yazılmaz, yalnız ön dağılım kullanılır; oyun içi öğrenmenin de kapalı olması önerilir. Kapalı ardışık eş tohum testini bu yorumla kilitlemek gerekir.

Mimari üstünlük üçgeni metinde tanımlanmamış (S6). Başlangıç karşı hamleleri en fazla iki, çifte sayılmayan somut etkiler ve neden kodlarıdır. Bedelsiz başlangıç etkilerinin büyüklükleri Balance'ta olur; seçilecek duvar bölgesi başlangıçta sahip olunan bölge olmalıdır. A1'de kapalı karşı hamlenin nedeni erken açılmaz; görünür olduğunda açıklanır.

### Tahmin, Sürpriz, Okunabilirlik

Plan öncesinde emir türü × hedef bölge olasılıkları normalize edilip kilitlenir; top 3 bu aynı dağılımdan alınır. Kadro/tepki gibi farklı olay uzayındaki yüzdeler ayrı dağılım ve `ForecastKind` taşır; birbirleriyle toplam 1 diye gösterilmez. Bölgesiz emirler için ayrı sabit hedef kimliği bulunur. Geçerli olabilecek gizli hedefler gerçek dünyayla elenmez.

Sürpriz için mevcut kural, kabul edilmiş emirlerin en düşük olasılıklısını ve katı `< 0,15` sınırını kullanır; döngü başına en fazla +3, yalnız o döngünün ilk taktik savaşında ilk vuruş. Otomatik savaşı seçmek hakkı sonraki döngüye taşımaz. Sürpriz geliri mevcut kilitli planı yeniden finanse etmez.

**Risk:** çok sayıda emir×bölge kategorisi doğal olarak %15'in altında kalır. Boş/ucuz ek emirle her tur ödül kazanmak mümkün olabilir. Eşiği veya dağılım tanımını sessizce değiştirmeyeceğiz (S2). F1'de öğrenme kapalı ön dağılım ve tahmin kaydı bulunur; öğrenme/Sürpriz etkileşimli kabul ölçümleri F3'tedir.

Gizli bir emrin Sürpriz olup olmadığını **Core'un otoriter çözümü** kilitli tahmin görüntüsünden değerlendirebilir; bu, emrin içeriğini Mind'a öğrenme girdisi yapmaz. Snapshot'ın taşınma biçimi iki planlı API'yi bozmayacak bir karar bağlamıdır; AI girdisi canlı kuyruğa erişmeden kapanır. Kayda önceden kilitlenen tahminin özeti de yazılır. Gizli ödül için emrin kendisini açmanın gerekip gerekmediği S2'de sorulur.

Okunabilirlik önerisi: her döngüde kabul edilmiş emirlerin olasılık ortalaması, ardından son 5 dolu döngünün eşit ağırlıklı ortalaması. Emir sayısı fazla olan döngü daha ağır sayılmaz. Hiç emir yoksa ayrı `Wait` tahmini kullanılması S2'nin dağılım kararıyla birlikte netleşir. Son 12 döngü görüntü geçmişi ayrıca tutulabilir; 5 döngülük kural penceresini değiştirmez.

### Makro planlayıcı

1. SideView + modelden N=8 tutarlı inanç/oyuncu plan örneği üret.
2. Nakit/veri erişimi/üçgen/çekirdek güvenliğine göre K=12 farklı makro aday üret: yayıl, duvar, saldırı, eğitim, üretim, Tekillik, çekirdek taşıma, gizlenme/blöf. Aday ve emir sırası kararlı.
3. Her adayı aynı N örneğe karşı bir döngü ileri çöz; savaşlar otomatik. Gerçek oyuncu planı okunmaz.
4. Sunucu payı, İşlem, veri çeşitliliği, çekirdek güvenliği, Tekillik ve farkındalık riski puanları; bütün katsayılar Balance'ta. İşlem birikimi zaferin yerini alacak sınırsız değer taşımaz.
5. En yüksek beklenen puanı seç; eşitlik MindRng. Hayalet için yalnız yeterince iyi adaylar arasında daha geniş örnekleme; eşik Balance'ta.

Gerçek süreye göre aramayı kesmek tekrar oynatmayı bozacağından karar **sabit örnek/adım sayısıyla** üretilir. Stopwatch yalnız Editor ölçümünde kullanılır. Isınma sonrası p50 ≤50 ms; p95, en yüksek süre, örnek sayısı ve benzetim adımları da kaydedilir. Bütçe aşılırsa N/K ölçülerek değiştirilir; gizlice daha az arama yapılmaz.

### Tahta aklı (F2)

Önce geçerli hareket/yetenek adayları, sonra dar beam araması. Puanlar: düğüm kontrolü, can, sınıf üstünlüğü, dolap tehdidi, sivil kaçınma; oyuncunun modelden örneklenen yanıtlarına karşı değerlendirme. Sıra birim kimliğiyle kararlı, çakışan niyetler Core doğrulamasından geçer. Niyet hedefi/türü/sırası tahta görüşünden gösterilir; gizli niyetin görünürlük kuralı S1/S4'te tamamlanır.

Tuzaklar, kendi konuş köşesine göre aynı referansa döndürülmüş gözlem ısı haritasından seçilir; geçilebilir, düğüm ve konuş karesi olmayan en fazla iki kare. Isı haritası yoksa ön dağılım. Gerçek sonraki oyuncu hareketi veya undo geçmişi okunmaz. Hayalet Emir mevcut niyetin geçerli hedefini değiştirir; Yem menzildeki aday seçiminde önceliklidir. Ortanca ≤30 ms; sabit düğüm bütçesi.

F1'de `HabitBot`, `RandomBot`, `GreedyBot`, `AggroBot`, `StealthBot`, `SurpriseBot`, `MindBot` aynı `IPlanBot` sınırını kullanır. SurpriseBot ön tahminle az beklenen **geçerli** emri seçer; ödülün öğrenme öncesi/sonrası etkisi ayrı ölçülür. F2'de taktik politikaları, F3'te profile dayalı güncellemeler etkinleşir.

## 7. Kayıt, profil, devam

Önerilen dosyalar `ayna_savegame.json` ve `ayna_profile.json`; eski `savegame.json` ve `player_profile.json` ile çakışmaz. F0'da dosya oluşturulmaz. Core DTO'ları saf JSON; disk işlemleri yalnız Game/Editor'de.

`AynaRecord` içeriği:

- `formatVersion`, `rulesVersion`, `mindVersion`, `worldDataVersion`, tohum;
- başlangıç AynaBalance kopyası ve ayarlar (öğrenme düzeyi dahil);
- iki mimari, sıralı başlangıç tercihleri, kabul edilmiş bölgeler ve karşı hamle kararları;
- oyun başındaki AYNA profilinin **derin kopyası**; self-play'de iki tarafın ayrı başlangıç profil görüntüleri;
- sıralı oyuncu döngü planları; savaş kimliği, komuta/otomatik seçimi, kadro, hareket/yetenek, undo, tur bitir, çekilme girdileri;
- döngü/evre/savaş imleci, girdi sayacı; teşhis için üretilen AI planı/tahmin özeti ve kural hash kontrol noktaları;
- oyun sonu profil güncellemesinin bir kez uygulanmasını sağlayan `runId` / profil uygulama kaydı.

Core, Mind'ı çağırmaz. `AynaReplayRunner` Mind tarafında profil ve gözlemlerden AYNA kararını yeniden üretir; Core'a iki planı dışarıdan verir. F4 Game bunu diskten yüklenen DTO ile çağırır. Eski `Replay.Restore` değiştirilmez. AYNA kararları yalnız kayıttaki başlangıç profili/dengeyle üretilir, kullanıcının bugün diskteki daha yeni profiliyle üretilmez. AI plan özeti uyuşmazsa sessiz devam edilmez; desteklenmeyen sürüm açıklanır, özgün dosya korunur. İleri sürümü eski kuralla açma yoktur.

Kayıt yalnız döngü sonunu değil bekleyen çatışmayı, kadro seçimini ve tur içi undo dizisini de yeniden kurmalıdır. Önizleme kayda girmez; dünya/tahta değiştiren başarılı komutlar sırayla girer. Geçersiz komutlar kurala etki etmez; UI teşhisinde ayrı kalabilir. Disk yazımı geçici dosya + atomik değiştirme/yedekle korunur; bozuk JSON, eksik sürüm ve yarım yazım için test bulunur. Yeniden oynatma dosya/profil yazmaz.

Profil: sürüm, nesil, tamamlanan oyun/sonuç sayıları, özellik dağılımları, gözlem miktarları, ısı haritası, uygulanmış oyun kimlikleri. Profil öğrenmesi yalnız terminal sonuçta ve bir kez; yarım oyun nesil artırmaz. “Hafızayı sil” yalnız AYNA profilini etkiler; çalışan oyunun başlangıç görüntüsü değişmez. Yeni oyun temiz modelle başlar.

## 8. Görünüm modelleri ve debug sahnesi (F4)

VM'ler Game assembly'sinde Unity tipleri içermeyen veri sınıflarıdır; builder yalnız `View(Player)`, oyuncunun kendi taslak planı ve oyuncuya açılmasına izin verilen model özetini alır. Kaynak/yaş/belirsizlik alanları taşınır; gizli gerçeği `hidden=true` işaretiyle VM'ye koymak yasaktır.

| Kare | Planlanan VM | Veri |
|---|---|---|
| A1 | `StartVM` | Mimariler, bölge seçenekleri, nesil/skor, öğrenme ayarı, açılabilir karşı hamle nedenleri |
| A2 | `PlanningVM` | Harita, sunucu aralıkları, İşlem/gelir, veri erişimi, plan makbuzu, tahminler, farkındalık/okunabilirlik |
| A3 | `RevealVM` | Görünür iki plan, sisli sinyaller, çakışmalar, Sürpriz bildirimi |
| A4 | `ConflictListVM` | Kararlı çözüm sırası, görünen kuvvetler, olasılık, komuta/otomatik seçenekleri |
| A5 | `RosterVM` | Seçilebilir birimler/can, yuvalar, bilinen rakip kadrosu, tahta önizlemesi |
| A6 | `BoardVM` | Hücreler, açık niyet/tuzağı, düğüm, tur, seçilebilir komut ve güvenli önizleme |
| A7 | `BattleResultVM` | Düğüm/sunucu değişimi, hasar, sivil gürültü, sonuçta açılan tuzaklar |
| A8 | `DevelopmentVM` | 5×3 kademe, maliyet/koşul/üretim hattı, eğitim ve Tekillik |
| A9 | `ArmiesVM` | Birlikler, birimler, üretim/onarım, maliyetler |
| A10 | `ModelVM` | Tahminler, gözlemlenmiş alışkanlıklar, normalize ısı haritası, okunabilirlik geçmişi |
| A11 | `SingularityEventVM` | İlan, kalan ilerleme, yalnız bilinen veri hedefleri ve belirsizlik |
| A12 | `GameOverVM` | Sonuç, tahmin karnesi, nesil, izinli gerçek dünya incelemesi ve öğrenme özeti |

Döngü 7 örneği deterministik fixture olacak. A1–A12 aynı oyunun uygun evrelerinden türetilecek; A11 döngü 19 ve A12 döngü 22 ayrı devam fixture'larıdır, hepsi döngü 7 diye gösterilmeyecek. Gizli Doğu Asya çekirdeği sadece testin yetkili kurulumunda bulunur, A2 VM'sine sızmaz. JSON çıktıları F4'te `Docs/AYNA/vm_ornek/` altında oluşur. Kaynak örneğin maliyet/savaş sonucu tutarsızlıkları S5 kararıyla düzeltilir.

Debug sahnesi akışı: başlangıç → plan/emir kuyruğu → kilitle → AÇ günlüğü → çatışmalar → kadro/tahta veya otomatik → sonuç → sonraki döngü → oyun sonu. Basit, okunur uGUI+TMP; Input System ve serileştirilmiş referanslar. TR/EN bütün metinler Loc üzerinden; yeni anahtarlar ortak tabloya eklenip doğrulanır. Mevcut Fx başlangıç sahnesi veya build sırası değiştirilmez.

## 9. Test ve ölçüm planı

F0'daki belge düzenlemesi için kod testi/Play uygulanmaz. F1 itibarıyla her fazda yeni kural testleri ve **eski oyun dahil tüm EditMode testleri** zorunlu; konsolda yeni hata/uyarı olmayacak. Her fazın ölçülmeyen maddesi açıkça “ölçülmedi” yazılacak; hedef, sonuç gibi gösterilmeyecek.

| Alan | Kilitlenecek davranış |
|---|---|
| Katalog / invariant | 12/72, 18+5 hat, kapasite, negatif bütçe yok, çekirdek ve birlik kimliği |
| Başlangıç | Bütün mimariler, uzaklık ≥2, çakışma ve sıralı yedek aday; rakip yeni seçimine erişim yok |
| Her emir | Geçerli/geçersiz, maliyet, sıra, koşul, gürültü, tekrar/sınır, etki kaybı; 12 emir + Tekillik |
| Eşzamanlılık | Kenarda geçiş, aynı bölge, çapraz saldırı, karşılaşma önceliği, kısıtlı boş kapasite, eşit teklif, aynı bölge çok birlik |
| Sonlar / çevre | 39/40 döngü, 43/44 sunucu, eşzamanlı zaferler, eşik ±ε, 5 döngü kesinti, kaçış ve dolap silme |
| Determinizm | Aynı tohum/komutların her adımda hash eşliği; dil değişiminde aynı kural sonucu; kültür ve koleksiyon sırası bağımsızlığı |
| Adalet | Yalnız gizli gerçekte farklı iki dünya → aynı SideView + geçmiş + MindRng → aynı plan/tahmin/tuzak; gizli oyuncu planı değişince AYNA planı değişmez |
| API sınırı | WorldState public üye sızıntısı/yanlış arkadaş assembly yok; Mind/Game gerçek durum tipi veya eski kural API'sini kullanmaz; View'a mutasyon geri yansımaz |
| Kayıt | Başlangıç, planlama, savaş seçimi, tur içi, undo sonrası, sonuç öncesi/sonrası kayıt → hash eşliği; çift profil uygulama yok; bozuk/ileri sürüm güvenli reddedilir |
| Tahta F2 | 10 yetenek, menzil/engel, üçgen ve zırh, EMP süreleri, kontrol ve yönlendirme, tuzak/gözcü, düğüm, sivil, dolap, çekilme, tur sınırı |
| Undo / preview | Tam kopya eşliği; RNG, tuzak, gözlem ve kayıt yan etkisi yok; geri alınan hamle öğrenilmez |
| Öğrenme F3 | Kapalı eş davranış, Hafif ağırlığı, birleştirme, sürüm geçişi, hafıza silme, görünmeyeni öğrenmeme; %15 eşik, bir ödül, geçersiz emirden ödül yok |
| UI F4 | A1–A12 şema/örnek JSON, gizli alan yok, TR/EN eksiksiz, baştan sona oyun ve devam; gerçek sahnede görüntü kontrolü |

### Kendi kendine oynatma

Editor menüsü **Corrigible > AYNA > Kendi kendine oynat**, `[Explicit]` uzun test ve statik `AynaSelfPlay.Run(SelfPlayOptions)` aynı koşu motorunu kullanacak. Henüz mevcut değiller. Editor veri üretimi kullanıcı profiline yazmayacak; her koşunun kendi profil kopyaları ve tohumları olacak.

Önerilen CLI girişi (F1'de sınıf yazılıp doğrulandıktan sonra):

```bash
unity command eval 'return Corrigible.Ayna.EditorTools.AynaSelfPlay.RunDefault(500, "Docs/AYNA/denge/F1");' 600000 --timeout 660 --caller plugin --skill unity-cli --format json
unity command recompile --caller plugin --skill unity-cli
unity command run_tests --caller plugin --skill unity-cli --format json
unity command console --caller plugin --skill unity-cli --format json
```

Uzun koşu CLI sunucu zaman aşımıyla da sınırlı kalmasın diye parçalı ilerleme ve durum sorgusu sağlanacak; düzenleyiciyi tek uzun ana-thread döngüsünde kilitlemek yerine partiler halinde çalıştırma tercih edilir. Saf Core/Mind kısmında Unity API yoktur. Ortanca plan zamanı ile toplam koşu süresi ayrı ölçülür. Derleme Play kapalıyken; testten/sahne açmadan önce bütün açık sahnelerde dirty kontrolü. Kullanıcı sahnesi dirty ise otomatik kaydetme/atma yok; durum kullanıcıya bildirilir.

500 oyun dokuz sıralı mimari eşleşmesine 55/56 olarak paylaştırılır; mümkün olan oyunlar aynı tohum/karşılıklı taraf değişimiyle eşlenir. Taraf ve mimari etkisi ayrı raporlanır. Yaklaşık 55 oyunluk hücrede %45–55 sonucunun istatistiksel gücü düşüktür: ham oran, n ve %95 Wilson aralığı birlikte verilir; gerekirse sabit ek doğrulama tohumlarıyla örnek büyütülür. Beraberlik/ortak yenilgi ayrı kategori, payda açıkça bütün oyunlar; karar verilen oyunlar için kazanma oranı ayrıca gösterilir.

Her koşu: seçenekler, tohum listesi, denge görüntüsü/özeti, kod sürümü, mimari/bot/rol, bitiş ve döngü, savaş sayısı, Sürpriz, gürültü, tahmin isabeti, p50/p95 süre ve toplam süre. JSON + Markdown, deterministik adlandırılmış yeni çıktı dosyaları; önceki rapor ezilmez. F3 serilerinde eğitim tohumları ile doğrulama tohumları ayrılır, 100 seri × 5 oyun profil taşınır; RandomBot kontrolü ve aynı eşleşmenin öğrenmesiz baz çizgisi bulunur.

| Faz | Brifin geçiş kapısı |
|---|---|
| F1 | 500 oyun <5 dk; ortanca 25–35 döngü; ≥%90 oyun 40'tan önce; aynı mimaride %45–55, hiçbir eşleşmede >%65 yok; her zafer ≥%10, Son Şalter %3–15; 6–10 savaş; Random ≥%90, Greedy ≥%60; plan p50 ≤50 ms |
| F2 | Üstün sınıf %60–75; auto/taktik olasılık kovaları farkı ±10 puan; Habit tuzak isabet ≥%40, Random ≤%20; tahta p50 ≤30 ms |
| F3 | Habit 1→5. oyun +≥15 puan, Random ±5; 3. oyundan top-1 ≥%60; Sürpriz Habit <%5, Surprise ≥%30; Surprise kazanma Greedy'yi aşmıyor |
| F4 | Baştan sona oyun, kayıt/devam, kalıcı profil, A1–A12 JSON, TR/EN ve ekran görüntüleri, temiz konsol |
| F5 | Bot × mimari × auto/taktik tabloları; yalnız AynaBalance değişikliğiyle önce/sonra ölçüm |

F1'de çekirdek dolabı otomatikte kırılmaz; **Silme ancak sunucuların tamamını kaybedip kaçacak yer kalmamasıyla** oluşabilir. Taktik dolap kırma oranı F2'den önce ölçülemez. Bütün hedeflerin ilk değerlerle birlikte sağlanacağı varsayılmaz; geçmeyen faz “tamamlandı” sayılmaz. Tasarım değişikliği gerektiren sapma sonraki onaya sunulur, hedefler geriye dönük yumuşatılmaz.

## 10. Riskler ve efor tahmini

| Risk | Etki / yaklaşım |
|---|---|
| Sisin tanımsız olması | Adalet, öğrenme ve UI aynı kurala bağlı; S1 F1 başında çözülmeli |
| %15 Sürpriz ve geniş kategoriler | Tekrarlanan ucuz ödül / öğrenme testi çelişkisi; S2 ve ön dağılım frekans ölçümü |
| Sunucu başına gelir, tekrar Yayıl | Hızlı bileşik büyüme, kısa oyun; önce baz ölçüm, sonra yalnız Balance ve onaylı emir sınırları |
| Erken mimari kadroları | EMP/yem saldırı hasarı taşımıyor; otomatik güç yalnız cana dayanırsa taktiği temsil etmez; F2 utility katsayı kalibrasyonu |
| Taktik tur asimetrisi | Oyuncu cevabı ile AYNA niyeti rol avantajı yaratabilir; S8, rol değiştirmeli ayrı rapor |
| Tahmin ve gizlilik | Ödül için Core'un değerlendirmesini modelin eğitiminden ayır; gizli emri öğrenme testleri |
| Profil/AI sürüm değişimi | Eski kayıt yeni kararla kayar; sürüm kapısı, başlangıç görüntüsü, kontrol hash'leri |
| F1 toplam hız | 500×~30×2×96 ≈2,88 milyon bir-döngü benzetim; 50 ms plan üst sınırı tek başına 5 dakikayı garanti etmez. Isınma sonrası toplam koşu ölçülür |
| Örnek veri çelişkileri | Maliyet, sunucu transferi ve bazı görsel emirler S5'te düzeltilecek; kurala aykırı fixture yazılmayacak |

İnsan geliştirici için odaklı çalışma eforu tahmini (takvim taahhüdü değil): F1 5–8 gün, F2 6–10, F3 4–7, F4 4–6, F5 3–5; toplam **22–36 geliştirici günü**. Eksik kural kararları ve ölçümde çıkacak denge sorunları bu aralığı büyütebilir. Ajan uygulaması her fazın doğrulanmış çıktısı üzerinden izlenir; süre doldu diye kabul verilmez.

## 11. Açık sorular — en fazla sekiz

**S1 · Sis hangi bilgileri ne zaman açsın?** Öneri: kendi durumun kesin; kendi sunucun/birliğin bulunan bölgede rakibin sunucu ve birlik varlığı kesin, komşuda son gözleme dayanan aralık, uzakta bilinmeyen. Rakip çekirdeği bölgede birlikle temas/çekirdek savaşı veya Tarama açar. Tarama bir sonraki çözüm sonuna kadar kesin bilgi verir; sonra tarihli son gözlem olur. Genel ortak sahiplik çekirdeği otomatik açmaz. Bu görüş matrisi kabul ediliyor mu?

**S2 · Sürpriz hangi olasılıkla hesaplansın?** Ortak emir×hedef dağılımında %15, çok kategorili oyunda hemen her tur tetiklenebilir. Öneri: emir türünü ayrı tahmin edip **o türün koşullu hedef olasılığını** Sürpriz için kullanmak; geçerli hedefler üzerinden normalize etmek, aynı kuralı ekranda açıkça söylemek. Bu brifin ortak dağılımına göre kural değişikliğidir; onay olmadan uygulanmaz. Gizli emrin ödülü Core'da hesaplansın, hedefi Mind'a öğretilmesin. Ortak dağılımı aynen koruyalım mı, bu öneriye geçelim mi?

**S3 · Emir tekrarları ve teklif sınırı nasıl olsun?** Öneri: bölge başına bir Yayıl; miktar×2 o bölgenin toplam teklifi, ek gizli teklif bedeli yok. Miktar mimari sınırını aşamaz; Ağ 3 varsa toplam sınır 5 olur. Birlik başına Yürüt/Saldır ortak tek hak; sonradan yeni kazanılan sunucu yeni emir koşulu sağlamaz. Sonradan koşulunu yitiren ödenmiş emrin bedeli yanar. Bu sınırlama ve teklif yorumu kabul ediliyor mu?

**S4 · Hasarsız/destek birimlerinin temel saldırısı ve tahta duvarı nasıl olsun?** Öneri: bütün birimlerde yetenekten ayrı bitişik 1 hasarlı temel saldırı; tur başına bir hareket + bir eylem. Normal düğüm, tur sonunda üstünde duran tarafça alınır; iki taraf bitişikse değişmez. Röle işgali anında alır. Duvarı ilk sürümde auto güç ve harita savunmasıyla sınırlı tutalım; taretin can/menzil/hasarı ayrıca tasarlanmadıkça tahtaya taret eklemeyelim. Çekirdek zırhını hasar −1, en az 1 olarak tanımlayalım. Bu eksik taktik kuralları için öneri kabul mü?

**S5 · Çelişkili örnek sayılarda kuralı esas alalım mı?** Öneri: evet. A2 Hayalet planı Gizlen/Sahte yığınak indirimleriyle **21 yerine 19 İşlem**, mimari gürültü çarpanıyla **4 yerine 2 net gürültü** olmalı. Güney Asya'da iki taraf da tuttuğu düğüm kadar rakipten alırsa `3/2 → 4/1`; örnekteki `5/1` çıkmıyor. Öneri karşılıklı sunucu taleplerini aynı başlangıç görüntüsünde netleştirip `4/1` kullanmak. A3'teki “Savun” yeni emir değil bekleme/bilgi etiketi, A9 üretim kuyruğu anlık üretim makbuzu olsun. Fixture bu düzeltmelerle hazırlansın mı?

**S6 · Mimari karşı-seçim üçgeni ne olsun?** Brifte birim sınıf üçgeni var, mimari üçgeni tanımsız. Öneri: **Sürü > Hayalet > Kale > Sürü**; profil en çok Hayalet ise Sürü seçme örneğiyle uyumlu. Bu, başlangıç karşı-seçim önceliği olsun; ayrıca gizli hasar bonusu eklenmesin. Kabul mü?

**S7 · Eğitim veri kaybında dursun mu?** Öneri: normal kademe eğitimi ve Tekillik, gereken aile sunucu eşiği sağlanmayan döngüde ilerlemesin, iptal/iade olmasın; yeni eğitim kabul döngüsünde ilk ilerlemeyi alsın. Tamamlanmış kademe kalıcı, üretim ayrıca canlı veri erişimi istesin. Tekillik üç ailesi başlarken sabitlensin ve kademe eğitimiyle aynı anda yürüyebilsin. Kabul mü?

**S8 · İki botlu taktik savaşta niyet/cevap rolü nasıl dağıtılsın?** Öneri: F2 ölçümlerinde eş tohumlu çift savaş; birinde ilk bot, diğerinde ikinci bot niyet tarafı. Gerçek oyunda mavi AYNA niyet tarafı olmaya devam etsin. Böylece rol avantajı ayrı ölçülür; iki bot da oyuncu gibi önce cevap vermeye çalışmaz. Simetrik ekonomi/Sürpriz ve asimetrik taktik rol için bu ölçüm kabul mü?

19.10.2026: Kullanıcı sekiz öneriyi kabul etti ve F1 geçişini onayladı.

## F1 uygulama notu · 19.10.2026

F1 teknik uygulama ve ölçümleri [F1_NOTLARI.md](F1_NOTLARI.md) içinde. İlk N=2/K=12 yerine son stratejide N=1/K=12 kullanıldı; bütün oyun/AI katsayıları AynaBalance içinde. Gönüllü geri çekilme adayı ve çatışma maliyeti, savaş sayısını hedef aralığına indirdi. Taraf kimlik sayaçları ayrıldı, sabit yol tablosu eklendi. F1 kaydı iki dış planı da taşır; saf Core replay böylece Mind bağımlılığı gerektirmez. F3/F4 profil/sürüm ve AI oturumu devamı ayrıca tamamlanacak. Denge kapısı tamamen geçilmediği için bu plan F2 başlamış sayılmaz.

## F2 uygulama notu · 19.10.2026

Kullanıcı F1 raporundaki üç kabul düzenlemesini ve F2 geçişini onayladı (K35). Taktik tahta, komuta akışı, kayıt/replay, görüşten planlama ve ölçüm araçları kuruldu. Son makro bütçesi N=1/K=8; önceki N/K değerleri tarihsel ölçümlerdir. F2 raporu [F2_NOTLARI.md](F2_NOTLARI.md) içindedir. Sınıf karşılaştırması 64 sonlu düzen, kalibrasyon ise ayrı tohumlu kadro matrisiyle ölçülür. F3 bu kapsamda başlamaz.

## F3 uygulama notu · 19.10.2026

Kullanıcı F3 geçişini onayladı. Profil v2 / akıl sürümü 3, koşullu tahmin ve Sürpriz, Okunabilirlik, karşı açılış, öğrenme düzeyleri, dosya deposu ve oturum devamı uygulandı. Detaylı kapsam, son test ve 100×5 seri kabul ölçümleri [F3_NOTLARI.md](F3_NOTLARI.md) içindedir. F4 ayrı kullanıcı onayı gerektirir.

## 20.10.2026 · Onaylanan devam

Kullanıcı iki F3 denge sorununu düzeltmeyi ve F4’e başlamayı onayladı. F4 controller, kayıt/devam, A1–A12 görünüm modelleri, örnek JSON, denge/tema/font asset’leri ve oynanabilir AynaDebug sahnesi uygulandı. 611/611 test ve canlı oyun akışı geçti. Son geniş denge kabulünün bütün kapıları geçti; nihai durum [F4_NOTLARI.md](F4_NOTLARI.md) içinde tutulur. F4 için yeni izin gerekmez. F5 bu onayın kapsamına girmez ve başlamadı.

## F5 · 20.10.2026

Kullanıcı geçişi onayladı. Yedi bot × dokuz sabit mimari eşleşmesi × otomatik/taktik çözüm ölçülür. Coğrafya aynalama ve niyet rolü ayrı faktörlerdir; oyun içi öğrenme açık, oyunlar arası profil boş ve disksizdir. Önce 8 tohumla 3.024 oyun baz çizgisi; dar adaylar aynı ilk 4 tohumda; seçilen değer aynı baz tohumlarında ve ayrı doğrulama tohumlarında ölçülür. Oyun kuralı/planlayıcı algoritması değiştirilmez; katsayılar yalnız AynaBalance ve kendi asset’ine yazılır. Genel hedef sapmaları saklanır, hedefler gevşetilmez.
