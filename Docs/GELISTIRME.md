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
3. `utest` → 13/13 (ya da yeni sayı). Domain reload sırasında `run_tests` zaman aşımına uğrar; `utest` yeniden dener.
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
- Test oyunları **gerçek oyuncu profilini ve PlayerPrefs'i değiştirir** (`player_profile.json`, `corrigible.*`).
  İş bitince eğitim anahtarlarını sıfırla (`PlayerPrefs.DeleteKey("corrigible.tutorialDone")`, `"corrigible.tipsSeen"`)
  ve kullanıcıya profilin değiştiğini söyle.
- eval kodu tek tırnak içinde gönderilir; C# karakter sabiti (`' '`) kullanma, `"".PadLeft(n)` gibi dizgi kullan.

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
`CorrigibleDebug.Advance(n)` ile ilerlet, bölgeyi seçip `CorrigibleDebug.Click("Actions/Raid")`. Baskın kareleri
`Tile x,y` adlıdır; `RaidTileView.OnPointerEnter/OnPointerClick(null)` ile üzerine gelme/tıklama taklit edilir.
Brifing ve sonuç düğmeleri: `Brief/Modal/Buttons/Start`, `Result/Modal/Buttons/Back`. Geri çekilme 3 sn içinde iki basış ister.

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
