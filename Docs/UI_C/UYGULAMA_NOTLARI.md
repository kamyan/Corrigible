# UI_C · Uygulama notları

Analist masası (C yönü) `Assets/Corrigible/Desk` altında, uGUI + TMP ile kuruldu. Bu not üç şeyi anlatır:
bileşenler nasıl kullanılır, masa oyuna (F8) nasıl bağlanır, tasarımdan bilinen sapmalar nelerdir.
Kararların gerekçeleri `KARARLAR.md`'de, mimari özet `Docs/MIMARI.md` → “Analist masası”nda.

## 1. Hızlı başlangıç

- **Sahneler:**
  - `Scenes/DeskUI_Sandbox.unity`: C1–C4, sahte veri.
  - `Scenes/DeskUI_Gallery.unity`: C5 stil rehberi; ←/→ bölüm, F9 dil.
  - `Main.unity`'ye dokunulmadı.
- **Sandbox tuşları:**
  - Masa: R/N/T/L/H pencereler, 1–4 bölge eylemleri, B saha operasyonu, Boşluk duraklat, +/− hız, Esc en üstteki pencere.
  - Geliştirici: F1–F4 ön ayar (C1–C4), F5 sahte veriyi kaydır (ibre yumuşatması, C4'te grafik), F6 İllüzyon sunumu, F9 TR↔EN, F10 ipucu katmanı, F11/F12 referans katmanı.
- **Menüler (`Corrigible/Desk UI/…`):**
  - Varlıklar: atlas, fontlar, tema, sprite kaydı, Linear telafili dokular.
  - **Statik fontlara boşluk ekle** (F7).
  - **Sabit metinleri topla (DeskLocText)**: prefab'lardaki sabit metinleri çeviri aracına `DeskStaticWords.cs` olarak verir.
  - **Metin denetimi (TR + EN, Play)**.
  - **Performans ölçümü (Play)**.
  - **Capture Game View**.
- **Karşılaştırma:**
  `python3 Docs/UI_C/tools/compare.py <yakalama> Docs/UI_C/reference/screens/<C?>.png --regions C1|C2|C3|C4`

### Edit mod önizlemesi

- **Ne yapar:** masa sahneleri edit modda Play'deki gibi görünür. Varsayılan “oyun verisi”: `DeskGame`'de oyunun açılış masası (oyuncunun ayarlarıyla yeni oyun, sabit tohum; `SimDeskDataSource.BuildPreview`, hiçbir şey yazmaz), yani Play'e basınca gelen bölgeler, yerler ve listeler. `DeskUI_Sandbox`'ta sahte C1.
- **Menü:** `Corrigible/Desk UI/Edit modda önizleme` (Göster, Yenile; oyun verisi: açılış masası ya da giriş kâğıdıyla; sahte C1–C4).
- **Kopyalar:** dinamik listelerin öğeleri (harita düğümleri, satırlar, teleks) önizlemenin kopyalarıdır: kaydedilmez, düzenlenemez (gri), Play'de veriden yeniden kurulur. Düzenlemek için şablonu (listenin kapalı ilk öğesi) ya da prefab'ı kullan.
- **Ne düzenlenir:** panellerin yeri, boyutu, ölçeği ve eğimi sahnede ve prefab'larda; önizleme ve Play bunlara dokunmaz (aşağıda “Yerleşim sahnede”). Veriden yerleşen özellikler (metinler, liste satırları, metne göre genişlikler) her önizlemede ve oyunda yeniden kurulur.
- **Yeni görünüm yazarken:**
  - Dinamik liste edit modda yalnız `DeskUi.EditPreview` açıkken kurulmalı.
  - Kopyalar `DeskPool` ile alınmalı ya da `DeskUi.MarkPreview` ile işaretlenmeli.
  - Çalışırken eklenen bileşenler `DeskUi.AddTransient` ile eklenmeli.
  - Edit modda canlandırma çalışmaz: son değer doğrudan yazılır.

### Yerleşim sahnede

- **Pencereler:** konum, boyut ve açılıştaki açık/kapalı durumu sahnedeki hâli (`WindowManager` yerleşim yapmaz). Oyuncu sekmeden taşır; görünen kısım ekranda kalır. Dosya penceresi seçimle açılıp kapanır.
- **Taşınabilir panel:** pencere olmayan bir panele `DeskDraggablePanel` ekle. Tutulacak yerde tıklama alan bir grafik olmalı (harita kâğıdı, hedef kartı dolgusu). `visible` ekranda tutulacak kısım; `bringToFront` taşırken öne getirir. Düğmeler tıklamayı kendileri alır; sürükleme onlardan da başlayabilir.
- **İçeriğe göre uzayan kâğıt** (olay günlüğü, danışman notu, analist notu): yer, genişlik ve eğim prefab'da. Görünüm yalnız yüksekliği `DeskUi.SetHeightKeepTop` ile değiştirir: dönüşümsüz üst kenar yerinde kalır. Dikey çapalar tek nokta olmalı.
- **Haritaya bağlı çizimler:** harita koordinatı (1920×1080, sol-üst köken) `MapView.MapToWorld` ile ekrana çevrilir. Harita taşınsa ya da ölçeklense de düğümde kalır. Olay kartının spotu, halkası ve ipliği böyle; seçim ipliği zaten düğümün kendisini izler.
- **Büyük düz renk alanı** (karartma): `DeskSolidGraphic` kullan. Küçük dolgu sprite'ını Image ile büyük alana germe; kenar pikselleri onlarca piksele yayılır, parçalar arasında açık dikiş çıkar.
- **Yeni görünüm yazarken:** bir panelin kökünün `anchoredPosition`, `sizeDelta`, `localScale` ya da dönüşünü veriden yazma. Veri yalnız içerik ve içeriğe bağlı boyut verir; panelin içindeki satır ve hücre düzeni (DeskRow, liste) veridir.

## 2. Bileşenler ve yazma kuralları

| Bileşen | Ne zaman | Not |
|---|---|---|
| `PaperPanel` | kâğıt/manila/dizin kartı zemini | 9-dilim + gölge; renk `ThemeTint` anahtarıyla |
| `StampView` | damga (ONAYLANDI, GİZLİ, ACİL…) | `Set(metin, mürekkep, boy, açı, anahtar)`; İllüzyonda tema damgaları değişir |
| `TrustMarkView` | güven işareti (mühür, ~ rapor, ? şüpheli, ⃝ bilinmiyor) | durum `TrustLevel`'dan, metin `DeskLoc` |
| `CarbonText` | karbon kopya (rapor) değerler | gölge metni `Ghost` adında |
| `DeskButton` | bütün düğmeler | `SetKind(variant, small)`; genişlik içerik + 2·iç boşluk + 2 px kenar (CSS border-box); raycast `DeskHitArea`'da |
| `KeyChipView`, `SpeedKeyView` | tuş fişi, hız düğmesi | fiş metni `DeskKeys` + `Loc` |
| `DeltaView`, `RangeBarView`, `HatchBarView`, `ProgressBoxesView`, `RadioCheckView`, `StatusChipView`, `CountBadgeView`, `HandRingView` | göstergeler ve küçük parçalar | hepsi `Bind`/`Set` ile; her karede yazmaz |
| `UILineGraphic`, `UIPolygonGraphic`, `SpotlightGraphic` | çizgi, iplik, bant, spot | atlas beyazının merkez UV'si (aynı batch); tarama dokusu ayrı draw call |
| `DeskRow` | yatay dizilim (LayoutGroup yerine) | yalnızca veri değişince `Layout()`; `ignoreLayout` çocuklar atlanır |
| `DeskPool<T>` | dinamik listeler | `Get(i)` + `Trim(n)`; çalışma anında örnekler |
| `DeskLocText` | prefab'daki sabit metin | kaynak Türkçe; menüden toplanır (`DeskStaticWords.cs`) |
| `MapLegendFlow` | harita lejantı | satırları dile göre akıtır; TR yerleşimi değişmez, sığmazsa aralıklar daralır |

**Kurallar:**

- **Metin:**
  - `DeskUi.SetText` ile yazılır. Değer değişmediyse dokunulmaz (GC 0, yeniden kurulum yok).
  - Genişlik `DeskUi.TextWidth` ile ölçülür; CSS gibi son harf aralığını da sayar.
  - Auto Size, UpperCase/SmallCaps ve Outline yasak. Büyük harf `DeskText` (tr-TR) ile yapılır.
- **Renk:** yalnızca tema anahtarıyla (`ThemeTint`). Eksik anahtar eflatun çizer ve testte yakalanır.
- **Linear telafi:** Linear renk uzayında kâğıt üstü yarı saydam katmanlar `DeskMath.LinearOverlayAlpha` ile çevrilir. Gömülü alfalı dokuların telafili kardeşini `DeskLinearTextures` üretir.
- **Sprite:**
  - Null sprite'lı `Image` kullanılmaz; düz renk için `sh_fill_r0` alınır.
  - Tekrar eden dokular `RawImage` ile çizilir ve atlas dışında kalır.
- **Raycast:** yalnızca etkileşimli öğelerde açıktır. Modal karartması (`Scrim`) arka planı bilerek engeller.
- **Prefab'lar:** asset kaynaktır. Yeni prefab geçici bir Editor betiğiyle bir kez kurulur, betik silinir. `DeskUiKit` yardımcıları kalır.
- **Yeni metin alanı:** dar alanlara uzun EN gelebilir. Önce **Metin denetimi** çalıştırılır. Denetimin bulduğu hatalar:
  - taşma ve kardeş metin çakışması,
  - damga/tuş fişi çakışması,
  - EN'de Türkçe kalıntı,
  - 11 px altı yazı, 15 px altı Kalam.

## 3. Oyuna bağlama (F8)

> **Durum (F8.4):** köprü ve bütün ekranlar `Scenes/DeskGame.unity`'de oyunla çalışıyor: masa, olay, oyun sonu, giriş, ayarlar,
> kampanya, uyarı fişi, perde raporu, baskın emri ve raporu (taktik harita Baskin sahnesinde), sahte zafer, eğitim.
> Ayrıntılı kararlar KARARLAR → Faz 8 / F8.1 / F8.3 / F8.4.

Masa oyun kuralı görmez. Yalnızca `DeskViewModel` çizer ve niyet yayınlar:

```
IDeskDataSource   DeskViewModel Current; event Changed      ← oyun durumundan üretilir
IDeskCommands     SelectRegion, ToggleWindow, ExecuteRegionAction, LaunchFieldOperation, StartResearch,
                  PauseResearch, SetEmergencyFund, TogglePause, SetSpeed, DismissAdvisor, RunAdvisorAction,
                  ConfirmGlobalKillSwitch, SignEventOption, ReturnToMainMenu, ReviewLogs, RetrySameSeed, Restart
```

**Önerilen yapı:**

- Yeni `Corrigible.Desk.Bridge` asmdef'i Core, Game ve Desk'e başvurur.
- `GameDeskDataSource : IDeskDataSource` kurulur. Değişim sinyali olarak oyunun mevcut olayları kullanılır: gün ilerlemesi, haberler, modal olayları.
- `GameDeskCommands : IDeskCommands` niyetleri `GameController`/`Simulation` çağrılarına çevirir.
- Sahnede `DeskPresenter`'a bu iki nesne verilir; `MockDeskDataSource`/`MockDeskCommands` sandbox'ta kalır.

**Eşleme notları:**

- **Algı katmanı:**
  - Göstergeler, dosya ve teleks oyunun **gösterilen** değerlerinden (`sim.Shown`) beslenir, gerçek değerlerden değil.
  - Güven durumu (`TrustLevel`: doğrulandı/rapor/şüpheli/bilinmiyor) her değerle birlikte gelir.
  - İllüzyon `DeskViewModel.Illusion = true` ile açılır. `IllusionPresenter` yalnız sunumu değiştirir; değerler yine VM'den gelir.
- **Harita:**
  - Düğümler oyunun enlem/boylamından tasarım projeksiyonuyla yerleşir: `x = 960 + (boylam − 27) × 3,5`, `y = 622 − enlem × 4,45`.
  - Yollar oyunun bağlantılarıdır. `map.json` yalnız sahte veridir.
- **Olay (C3):**
  - `DeskViewModel.Event` dolu olduğu sürece masa duraklatılmıştır.
  - İmza `SignEventOption(i)`'ye gider. Oyun olayı kapatınca `Event = null` gönderir.
- **Oyun sonu (C4):**
  - `DeskViewModel.GameOver` doluysa masa canvas'ları kapanır.
  - Grafik serileri (gerçek/gösterilen), bantlar, olay günleri ve etiket konumları oyun kaydından (`GameRecord`) üretilir.
  - Etiket konumları şu an sahte veride ölçümden gelir; köprü hesaplamalıdır.
- **Dil:**
  - Masa metni `Loc` ile çevrilir. Veri kaynağı metni **seçili dilde** verir: oyunda bu `Loc.T(...)` demektir.
  - `MockEnglish` yalnız sahte veri içindir, oyunda kullanılmaz.
  - Dar alanlar için EN kısa biçimleri: gösterge başlığı `POL. CAPITAL`, eylem başlığı `Monitoring`, maliyet satırında gün `d`. Oyun çevirileri de bu sınırlara uymalı; F8'de metin denetimi oyun verisiyle yeniden çalıştırılır.
- **Tuşlar:** `DeskInput` klavyenin tek sahibidir. Oyunun eski `CrisisDeskView` kısayollarıyla aynı anda açık olmamalı (F8'de sahne seçimiyle).
- **F8.3 ekranları:**
  - Yeni ekranlar VM'de ayrı alanlardır (`Intro`, `Campaign`, `FakeWin`, `Toasts`; belge kipi `Event.Doc`). Düğmeler komut dizisi taşır
    (`"Intro:Start"`, `"Campaign:Upgrade:butce"`, `"Raid:Start:weu"`, `"Doc:Close"`, `"Toast:Dismiss:12"`) ve `IDeskCommands.Run`'a gider.
    `"Settings:Open"` görünümde kalır.
  - Ayarlar `IDeskSettings` üzerinden okunur ve yazılır: kanal sırası ana ses, müzik, ortam, efektler. Açıkken zaman durur (`SetOpen`).
  - Görünüm durumu (`DeskBridgeState`): `IntroOpen`, `CampaignOpen`, `FakeWin`, `SettingsOpen`, `VeilOpen`, `RaidBrief`, `RaidLogSeen`.
    `WantsHold` bunlardan `GameController.Hold`'u türetir.
  - Fiş kimlikleri yeni oyunda sıfırlanmaz: görünüm her kimliği bir kez gösterir.
  - Oyunda denemek gerçek kayıt dosyalarını yazar (kayıt, profil, kampanya): denemeden önce `persistentDataPath`'teki JSON'ları yedekleyin.
- **Eğitim (F8.4):**
  - İçerik oyunun `TutorialSequence` asset'inden gelir; masa yalnızca hedef adlarını sahneye bağlar (`DeskTutorialView.targets`).
  - Yeni bir hedef eklenirse bağlama tablosuna da eklenmeli (test bunu denetler).
  - Eski arayüze göre yazılmış metinlerin masa karşılığı `DeskTutorialRunner.DeskWording`'de.
  - Eğitim tercihleri (`corrigible.tutorialDone`, `corrigible.tipsSeen`) eski oyunla ortak: denemeden önce not alın.
- **Kapsam dışı pencereler:** Haberler, Saha ekipleri, YZ günlüğü ve Rehber tasarlanmadı; mevcut bileşenlerle sade içerik çizer (`News`, `Teams`, `AiLog`, `Guide`).

## 4. Kalite durumu (F7 sonu)

- **Referansla toplam fark (1920×1080):** C1 %3,0 · C2 %3,0 · C3 %2,1 · C4 %2,5. Bölgeler ±1 px içinde.
- **En-boy:** 1920×1080, 2560×1440, 1920×1200 ve 3440×1440'ta dört ekran yakalandı (`captures/F7_C?_*.png`). Pencereler haritayı örtmez, kırpılan öğe yok.
- **EN:** dört ekran yakalandı (`captures/F7_C?_en.png`). Metin denetimi her iki dilde, C1'de bütün pencereler açıkken 0 hata, 0 uyarı verdi (`captures/F7_metin_denetimi.txt`).
- **Performans** (`captures/F7_performans.txt`, Profiler UI Details):
  - UI batch: C1 65 · C2 53 · C3 77 · C4 17.
  - Boşta GC: masa betiklerinde 0 B. Görülen 464 B, ölçümü süren Editor eval çağrılarının görev kuyruğundan.
  - Boşta `Graphic` yeniden kurulumu yok. Yalnızca teleks kayması Ticker canvas'ını her kare yeniden toplar (0,007 ms).
- **Testler:** EditMode 89/89.

## 5. Bilinen sapmalar

| Konu | Sapma | Neden |
|---|---|---|
| Draw call | C1 65 batch (hedef “kabaca ≤ 35”; kullanıcı kabul etti, 2026-10-12) | 8 font malzemesi + DejaVu/ağırlık yedekleri × 6 canvas; dizin kartı çizgileri (RawImage, PROMPT gereği) kart içeriğini ikinci derinlik katmanına itiyor. Ayrıntı: KARARLAR → Faz 7 |
| Renk uzayı | Linear (kullanıcı kararı) | sprite'taki 1 px çizgiler referanstan biraz açık; yarı saydam katmanlar telafi edildi |
| 1,5 px kenarlar | 1 px | referans PNG'de Chrome 1 px çiziyor |
| Seçim ipliği | harita canvas'ının üstünde, pencerelerin altında | referans PNG öyle (PROMPT Overlay diyordu) |
| Araştırma özeti | öğe aralığı CSS `gap: 6` yerine boşluk | tek satır rich text; 2–4 px |
| Hedef taraması | faz farkı (CSS periyodu 5,09 px, sprite 5 px) | görsel olarak aynı |
| Danışman notu bandı | dikey dolgu ile alt 7/15 | CSS `clip-path` karşılığı |
| Teleks | yoğun metnin kenar yumuşatması | konumlar hizalı |
| Döndürülmüş metin | kenar yumuşatma farkı | TMP ile Chrome arasındaki fark |
| EN kısa biçimler | `POL. CAPITAL`, `Monitoring`, maliyette `d` | dar alanlar; TR yerleşimi değişmedi |
| Uzun EN başlıkta damga | oyun sonu damgası başlığın en az 24 px sağında; olay alan ızgarası etiket genişliğinden | CSS'teki sabit konum EN'de çakışıyordu; TR'de konumlar aynı |
| Tasarlanmamış pencereler | sade içerik | açık karar 3 |
