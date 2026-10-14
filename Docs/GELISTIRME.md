# Geliştirme

## Ortam

- Unity **6000.3.12f1** (proje: bu deponun kökü). Editor açık olmalı; ajanlar onu `unity` CLI + Pipeline paketiyle
  (`com.unity.pipeline`) sürer. Editor derleme hatasıyla Safe Mode'a düşerse CLI bağlanamaz: hatayı dosyadan düzelt.
- Prototip ve denge testleri: macOS'ta yerleşik JavaScriptCore
  (`/System/Library/Frameworks/JavaScriptCore.framework/Versions/Current/Helpers/jsc`). Node gerekmez (varsa o da çalışır).
- Yardımcılar: `source Tools/unity-helpers.sh` → `ueval`, `ucompile`, `utest`, `uplay`, `ustop`, `ushot`, `uerrors`.

## Döngü

1. Kodu değiştir.
2. `ucompile` → "HATA YOK" görmeden devam etme. (`recompile_status` hata bildirmez; konsolu zaman damgasıyla oku.
   Konsolda eski derlemelerden kalma hatalar durur; yalnızca son derlemeden sonrakilere bak.)
3. `utest` → başarısız test olmadığını doğrula. Domain reload sırasında `run_tests` zaman aşımına uğrar; `utest` yeniden dener.
4. Oyunda dene (aşağıda). Görsel değişiklikte ekran görüntüsünü gerçekten aç ve bak.
5. Simülasyon kuralı değiştiyse: `Prototype/sim.js`'i de değiştir, `parite.js` ile beklenenleri güncelle, denge testini
   önce/sonra karşılaştır.
6. Play'deysen `ustop`. Geçici görüntüleri sil (`Temp/*.png`). Commit'i kullanıcı isterse yap.

## Oyunu ajan olarak test etmek

Editor arka plandayken Game görünümü güncellenmez; bu yüzden ekran görüntüsü `CorrigibleDebug.Capture` ile alınır
(Canvas'ları geçici olarak kameraya bağlar, edit modda da çalışır).

```bash
source Tools/unity-helpers.sh
uplay
ueval 'return Corrigible.EditorTools.CorrigibleDebug.Click("IntroModal/Buttons/Start");'   # hiyerarşi yolunun SONU
ueval 'return Corrigible.EditorTools.CorrigibleDebug.Click("Kuzey Amerika") + Corrigible.EditorTools.CorrigibleDebug.Click("Actions/Row1/Monitor");'
ueval 'return Corrigible.EditorTools.CorrigibleDebug.Advance(60);'    # 60 gün ilerlet (hediyeleri reddeder), durum özeti
ushot Temp/orta.png                                                    # sonra dosyayı aç ve bak
ustop
```

- `Click` görünmeyen/devre dışı düğmeye tıklamaz ("devre dışı" döner). Sık yollar: `Speed/Speed1x`, `Speed/Pause`,
  `TabBar/LogTab`, `Actions/Row1/Audit`, `Actions/KillSwitch`, `GlobalKillSwitch/KillSwitch`, `Yorumlanabilirlik/Row/Start`,
  `Options/Reserve30`, `GiftModal/Choices/Reject`, `EndModal/Buttons/LookAtMap`, `Callout/Buttons/Next`.
- Belirli bir duruma hızlı gitmek için oyunu `GameController` üzerinden doğrudan sürebilirsin
  (`g.Sim.Step(); g.Flush();` döngüsü, `g.Act(...)`). Durumu elle değiştirmek (`s.Fon = 2000`) testi hızlandırır ama
  kayda girmez; kayıt/devam testinde kullanma.
- Sahte zafer gibi zamanlı akışları yakalamak için süreyi yansıma ile uzat:
  `FakeWinModal.holdSeconds`. Bütün değişiklikler Play'den çıkınca geri döner.
- Test oyunları **gerçek oyuncu profilini ve PlayerPrefs'i değiştirir**. Play öncesi `player_profile.json`,
  `campaign.json`, `savegame.json` ve `corrigible.*` tercihlerini yedekle; test sonunda özgün dosya/tercihleri geri yükle.
  Önceden olmayan test kaydını temizle. Geri yükleyemediğin değişiklik varsa kullanıcıya açıkça söyle.
- Play sırasında script derlemek statik tekil örnekleri sıfırlayabilir. Script değişikliklerini Edit modunda yap;
  yeniden derleme sırasında Play açıksa doğrulamadan önce oturumu kapatıp tekrar aç.
- eval kodu tek tırnak içinde gönderilir; C# karakter sabiti (`' '`) kullanma, `"".PadLeft(n)` gibi dizgi kullan.
- Uzun `eval` çağrılarında **iki ayrı zaman aşımı** var: ikinci konumsal argüman sunucudaki C# çalışma süresi
  (milisaniye), global `--timeout` ise CLI'ın yanıtı bekleme süresi (saniye). Yalnızca `--timeout` artırmak sunucu
  süresini uzatmaz. Örnek: sunucuda 60 sn, istemcide 90 sn:

  ```bash
  unity command eval 'return Corrigible.EditorTools.CorrigibleDebug.Capture("Temp/shot.png");' 60000 --timeout 90 --caller plugin --skill unity-cli
  ```

  `ueval` kısa çağrılar için mevcut varsayılanları kullanır; uzun asset/sahne işlemini bu doğrudan biçimle gönder.

## Testler (`Tests/EditMode/SimulationTests.cs`, `LocalizationTests.cs`)

| Test | Ne kilitler |
|---|---|
| `Rng_MatchesBrowserPrototype` | mulberry32 çıktıları JS ile aynı |
| `SameSeedAndCommands_GiveSameGame` | determinizm |
| `FullGame_MatchesBrowserPrototype` (3 vaka) | C# = JS tam oyun (bitiş, gün, fon 6 basamak) |
| `PassivePlayer_AlwaysLoses` | hiçbir şey yapmayan her rakibe kaybeder |
| `MonitorEverythingAuditEverything_CannotEradicateEarly` | "her yeri izle, her şeyi denetle" stratejisiyle 120 günden önce Silme olmaz (oyuncu raporlu açık) |
| `Alignment_CannotStartBeforeDetection` | Hizalama tespit öncesi başlamaz |
| `Core_EscapesOnFirstDetection` | çekirdek ilk tespitte kaçar |
| `SaveAndRestore_ReproducesExactState` (3 vaka) | kayıt JSON gidiş-dönüşü birebir aynı durum |
| `ResearchReserve_KeepsFundsForActions` | acil durum fonu araştırmanın fonu indirmesini sınırlar |

## Parite (C# ↔ JS)

```bash
cd Prototype
$JSC sim.js parite.js
# [TestCase("manipulator", 7u, "acik@185 fon=350.837433")] ... çıktısını SimulationTests'teki TestCase'lerle değiştir
```

`parite.js`'teki senaryo C# `PlayScripted` ile aynı olmalı (na ve cn'ye izleme, hediyeleri kabul, tespitte
Yorumlanabilirlik). Parite bozulursa farkı bulmanın yolu: aynı tohumla iki tarafta gün gün `fon`, `rngState`,
bölge sızmalarını yazdırıp ilk ayrışan günü bulmak. Tipik nedenler: RNG çağrı sırası, `Math.round` yerine C# yuvarlaması,
`float` kullanımı, LINQ sırası, JS'te `||` ile `??` farkı.

## Denge testi

```bash
cd Prototype && echo 'globalThis.N_GAMES = 200;' > /tmp/n.js && $JSC /tmp/n.js sim.js denge-testi.js
```

Her rakip için beş oyuncu modeli: Pasif, Acemi, Haritasız usta, Usta, Dikkatli usta; ayrıca "çekirdek avcısı" ve
"imha botu". Ana testteki oyunlar rastgele tohumla oynar, sonuçlar ±%5 oynar; karşılaştırmada N ≥ 200 kullan.
Güncel referans (N=200, rezerv 0):

| Rakip | Pasif | Acemi | Haritasız usta | Usta | Dikkatli usta |
|---|---|---|---|---|---|
| Solucan | 0% | ~25–30% | ~85% | ~95% | ~90–95% |
| Manipülatör | 0% | ~7% | ~50% | ~85% | ~83% |
| Kâhin | 0% (sessiz) | ~5–8% | ~60% | ~62–66% | ~62–65% |

Hedefler (tasarım): pasif her zaman kaybeder; usta oyuncu kazanabilir ama Kâhin belirgin şekilde zor; Silme hızlı bir
kestirme değil (imha botu çoğunlukla kaybeder).

## Deneysel prototip testleri

```bash
cd Prototype
echo 'globalThis.N_RAIDS = 200;' > /tmp/r.js && $JSC /tmp/r.js baskin.js baskin-testi.js        # baskın motoru
echo 'globalThis.DENGE_NO_RUN = true; globalThis.BASKIN_NO_RUN = true; globalThis.N_GAMES = 1;' > /tmp/pre.js
$JSC /tmp/pre.js sim.js baskin.js baskin-testi.js denge-testi.js denge-deney.js                # ekipler + ölçek + baskın
```

Tarayıcıda test: `python3 -m http.server 8732 --directory Prototype`. Konsolda `SonSalterDebug.S` (strateji durumu),
`SonSalterDebug.RB` (süren baskın), `SonSalterDebug.render()`. Baskını bot oynatmak için sayfaya `baskin-testi.js`'i
`window.BASKIN_NO_RUN = true` ile yükleyip `BaskinBot.botTurn(SonSalterDebug.RB)` çağırılabilir.

Unity'de baskını denemek (Play modunda, `source Tools/unity-helpers.sh` sonrası): bir ekibi `g.MoveTeam(...)` ile gönder,
`CorrigibleDebug.Advance(n)` ile ilerlet, bölgeyi seçip `CorrigibleDebug.Click("Actions/Raid")`. Baskin sahnesi birkaç
yüz milisaniyede eklemeli yüklenir. Kareye tıklama/üzerine gelme: `FindFirstObjectByType<Corrigible.Game.Raid.RaidBoard3D>()`
üzerinde `Click(x, y)` / `Hover(x, y)`. Analist Masası sunumunda açık başlayan kurallar kartı vardır (`Rules/Toggle`);
sonuç düğmesi `Result/Memo/Back`. Geri çekilme 3 sn içinde iki basış ister; arada onay fişi ve kararma gösterilir,
Vazgeç/Esc onayı kapatır. Hamleler ve makineler evresi canlandırılırken (`Busy`) girdi yok sayılır; ekran görüntüsünden
önce animasyonun bitmesini bekle. Tahtayı hızlı denemek için `Scenes/Baskin.unity`'yi tek başına açıp Play'e bas
(örnek baskın). 1/2/3 ekip seçer, Q yetenek, Z geri al, Enter turu bitirir; sonuçta Enter rapora geçer.

## Baskın sunumunu düzenlemek

Görsel kaynak `Docs/SonSalter_Baskin_Paketi`: PNG'ler görünüşü, `reference/measure` TSV'leri sayısal yerleşimi,
`spec/raid_tokens.json` kamera/malzeme/işaret değerlerini anlatır. `spec/board_states.json` tasarım örnekleridir;
gerçek geçerli hamle, tarama, hasar ve niyet hesabı `Core/Baskin.cs`'ten gelir. Görseli uydurmak için Core kuralı
değiştirme; aynı durumları gerçek kuralla kurup karşılaştır.

- Kalıcı sahne `Assets/Corrigible/Scenes/Baskin.unity`; `RaidLauncher` onu Main veya DeskGame üstüne eklemeli yükler.
  UI, bu sahnedeki Overlay `Baskin` Canvas'ındadır. Referanslar `[SerializeField]` ile bağlıdır.
- Parçalar `Assets/Corrigible/Prefabs/Raid` altında; `BoardBase` kontrplak/ozalit maket, `OperationTable` masa/çuha.
  Var olan parçayı güncellerken prefab'ı yerinde kaydet, GUID'ini ve `RaidPiece` referanslarını koru.
- Dokular, mesh'ler, malzemeler ve `RaidPencil.shader`: `Assets/Corrigible/Art/Raid/Analyst`.
  Parça/palet/font bağlantıları `Settings/RaidPieceLibrary.asset`, panel renkleri `Settings/RaidUiTheme.asset`.
  URP malzemeleri mat; emission ve kamera post-processing kapalı. Kartın ön/arka/kenar malzemelerini birlikte koru.
- UI prefab'ları `Prefabs/UI/RaidUnitCard.prefab`, `RaidLogLine.prefab`, `RaidLogLinePlain.prefab`;
  panellerin yerleşimi sahnede. Courier Prime, Libre Franklin, Kalam ve DejaVu yedeği mevcut Desk TMP asset'lerinden.
  Kurallar kartı katlanır; hedef formu uzun metne göre büyür, teleks günlüğünün yüksekliği ona göre değişir.

**Izgara ve kamera:** bir karo bir dünya birimi, merkezler `RaidBoard3D.W(c, r) = (c − 3,5, TileTop, 3,5 − r)`.
Kamera ortografik, rotasyon `(35°, 45°, 0)`. CanvasScaler `Scale With Screen Size`, referans `1920×1080`,
genişlik/yükseklik eşleme `0,5`. `RaidCameraFraming` her `LateUpdate`'te
`orthographicSize = camera.pixelHeight / canvas.scaleFactor / (88.3885 × 2)` hesaplar ve tahtanın merkezini
`BoardViewport` merkezine kamera sağ/yukarı ekseninde kaydırır. Referansta ortografik boyut yaklaşık `6,1094`,
karo elması yaklaşık `125×71,7 px`; ekran karosunu sabit `110 px` varsayma.

Sol/sağ sütunlar Canvas referans uzayında `376 px` ve kendi kenarlarına çapalı; görüş alanı soldan/sağdan `408`,
üstten `50`, alttan `78` payla gerilir (referansta `1104×952`). Alt bilgi/düğme grubu da bu alana çapalıdır.
Kamera kadrajını veya karo ölçeğini elle sabitlemek yerine bu ilişkiyi koru. Dünya can/sıra/güven etiketleri
`RaidWorldLabel` ile kameraya dönük TMP ve kâğıt mesh olarak çizilir; glif yüksekliği en az 11 ekran pikseli tutulur.
Çakışma hesabı ekran uzayındadır; ekran/kamera boyutu değişince etiketler tekrar yerleştirilir.

Kontrol: iki dilde gerçek baskın aç; erişilebilir kareleri, EMP önizlemesini/çarpışmayı, Aldatma tarama sınırını,
makinelerin sıra vurgusunu ve girdi kilidini, düşmüş/tur bitirmiş figürleri, geri çekilme onayını ve sonuçtan rapora
dönüşü dene. 1920×1080'e ek olarak 16:10 ve ultra geniş kadrajı görüntüle; metin taşması ve etiket çakışmasını açıp
bak. Tasarım aktarımı simülasyon kuralını değiştirmediği için C#/JS parite değerleri yeniden üretilmez.

Aktarımda kullanılan `BaskinPieceAuthoring` ve `BaskinSceneAuthoring` tek seferlik editör yardımcılarıdır;
çalıştırılıp sahne/prefab/asset'ler kaydedildikten ve doğrulandıktan sonra silinir. Onları kalıcı kurucuya dönüştürme;
sonraki değişikliklerin kaynağı kaydedilmiş asset'lerdir.

## Sık yapılan işler

**Denge sayısı değiştirmek:** `Balance.cs` alanı + `sim.js` `BAL` aynı değer → parite değerlerini yeniden üret → denge testi.
Yalnızca oyunda denemek için `Settings/BalanceConfig.asset` yeterli (ama prototip ve testler varsayılanları kullanır).

**Araştırma eklemek:** `GameData.Research` + `sim.js RESEARCH` (aynı sıra, aynı kimlik). Sahnede yeni satır: araştırma
listesinde (`.../Tabs/ResearchPanel/Viewport/Content`) bir `ResearchRow` prefab örneği ekle, `ResearchRowView.researchId`'yi
gir ("Setup" sabit yazıları editörde doldurur; elle de girilebilir), dal başlığı gerekiyorsa ekle,
`CrisisDeskView.research` dizisine bağla. Etkisini `Simulation`'da uygula (ör. `S.Done("id")`).

**Oyuncu eylemi eklemek:** bkz. MIMARI.md → "Kayıt / devam". UI'da `GameController`'a ince bir komut metodu ekle;
görünüm onu çağırır, `ActResult.Msg`'i toast ile gösterir.

**Eğitim adımı / ipucu eklemek:** `Settings/TutorialSequence.asset`'i Inspector'dan düzenle. Yeni koşul gerekiyorsa
`TutorialCondition` enum'ının **sonuna** ekle ve `TutorialConditions.Check`'e yaz. Yeni hedef gerekiyorsa `TutorialTarget`
sonuna ekle ve `UI/Egitim` → `TutorialController.targets`'a RectTransform bağla. Eylem isteyen adımda kaynak yetmeyebiliyorsa
`pauseGame = false` yap (oyuncu kilitlenmesin).

**UI öğesi eklemek:** önce benzer bir prefab ya da sahne objesini kopyala (Ctrl+D / `Object.Instantiate`); referanslar
kopyanın içinde yeniden eşlenir. Yeni bileşen yazıyorsan alanları `[SerializeField]` yap ve Inspector'dan (ya da tek
seferlik editör script'inde `SerializedObject` ile) bağla. Renkleri `UiTheme`'den al.

**Sahneyi koddan değiştirmek:** `Assets/Corrigible/Editor` altına tek seferlik statik bir `Run()` yaz: sahneyi
`EditorSceneManager.OpenScene` ile aç, objeleri `Transform.Find` (pasifleri de bulur) ile bul, prefab örneğini
`PrefabUtility.InstantiatePrefab` ile koy, alanları `SerializedObject` ile bağla, prefab örneğinde değiştirdiğin bileşenler için
`PrefabUtility.RecordPrefabInstancePropertyModifications` çağır, `MarkSceneDirty` + `SaveScene`. `ueval` ile çalıştır,
sonucu doğrula, script'i sil.

## Lokalizasyon iş akışı

1. Kodda oyuncuya görünen metni Türkçe yaz ve sar: `Loc.T("Metin.")`, `Loc.F("{0} bölgesinde {1} gün.", ad, gün)`.
   Sayı biçimini yer tutucuda ver (`{0:0.0}`); `ToString` + birleştirme yapma. Türkçe ek uyumu gerekiyorsa (ör.
   `"15'in"`) `Loc.IsSource` ile Türkçeye özel dal yaz, diğer dillerde ek kullanma.
2. Sahneye/prefab'a yeni sabit yazı eklediysen: **Corrigible > Lokalizasyon > Sahne ve prefab'lardaki metinlere
   LocalizedText ekle** (tek harfli yazıları atlar; onlara bileşeni elle ekle).
3. **Corrigible > Lokalizasyon > Çeviri tablosunu güncelle**, sonra `Assets/Corrigible/Resources/Localization/strings.txt`'te
   boş kalan `en` hücrelerini doldur (satırın sonundaki sekmeden sonra). `{0}` ve `<b>` gibi işaretler aynen kalmalı.
4. **Çevirileri doğrula** menüsü ya da `LocalizationTests` → eksik/uyumsuz çeviri listesi.
5. Oyunda dene: giriş penceresindeki dil düğmesi ya da `PlayerPrefs.SetString("corrigible.language", "en")` + Play.

Ajan olarak: `ueval 'return Corrigible.EditorTools.LocalizationTool.UpdateTable();'` ve
`ueval 'return string.Join("\n", Corrigible.EditorTools.LocalizationTool.Validate());'`.

## Bilinen tuzaklar (bu projede yaşandı)

- **Lokalizasyon:** statik `readonly` alanda `Loc.T` kullanma (dil, sınıf ilk yüklendiğinde donar; `Loc.N` + gösterimde
  `Loc.T`). Metne bakarak karar verme (`msg.Contains("fon")` gibi; dil değişince bozulur). Testte `RegexOptions.IgnoreCase`
  Türkçe `ı`'yı `I` ile eşleştirir.

- **Image layout'u bozar:** sliced sprite'lı `Image` bir `ILayoutElement`'tir ve sprite kenarlarını minimum boyut (40) olarak
  bildirir. Zemin Image'ı ayrı bir `Background` çocuğuna koy ve `ignoreLayout` yap.
- **Bir dosyada iki MonoBehaviour:** sahne kaydedilince ikincisi "Missing Script" olur. Dosya adı = sınıf adı.
- **`childForceExpandHeight`:** HorizontalLayoutGroup'ta açıkken grup esnek yükseklik bildirir ve dikey yerleşimde fazladan
  alanı yutar; satıra `LayoutElement.flexibleHeight = 0` ver.
- **TMP harf aralığı:** `characterSpacing`, CSS piksel değerinin yaklaşık `px / fontSize × 50` karşılığıdır (100 değil).
- **TMP Türkçe karakterler:** LiberationSans SDF + dinamik fallback; çalışırken metin objelerinin altında
  "TMP SubMeshUI" çocukları oluşur, normaldir.
- **Enum'lar asset'te sayı olarak saklanır:** `TutorialTarget`/`TutorialCondition`'a araya değer ekleme.
- **`capture_game_view` / Editor ekran görüntüsü** arka planda güvenilmez; `CorrigibleDebug.Capture` kullan. Proje dışına
  (ör. /tmp) yazamaz; `Temp/` altına yaz.
- **Edit modda sahne kurulumu:** `EditorSceneManager.NewScene` kullanılmayan asset'leri boşaltır; asset referanslarını
  sahne oluşturulduktan sonra yükle.
- **Pencere/eğitim katmanı açık kaydedilirse** oyun açılışında masanın üstünde görünür; kaydetmeden önce kapat.
- **Hold çakışması:** pencereler `GameController.Hold`, eğitim `TutorialHold` kullanır; birbirini sıfırlamasınlar diye ayrı.
- **Sahte zafer:** son olayı geldiğinde `Sim.HideEnding = true` yapılmazsa harita ve haberler gerçeği erkenden gösterir.
- **Kazanma oranı deneyleri:** botlar biriken fonu harcamayı bilmez; fonu "saklayan" kurallar (rezerv, gelir payı) botlarda
  her zaman kötü görünür. Yorumlarken bunu hesaba kat.
