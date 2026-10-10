# Mimari

## Klasörler

```
Corrigible/                      (Unity proje kökü, git deposu)
├─ AGENTS.md, CLAUDE.md          yapay zeka ajanları için giriş
├─ Docs/                         bu belgeler
├─ Prototype/                    tarayıcı prototipi (referans simülasyon, denge ve parite script'leri)
└─ Assets/
   ├─ Corrigible/
   │  ├─ Core/                   saf C# simülasyon (asmdef Corrigible.Core, motor referansı yok)
   │  ├─ Game/                   MonoBehaviour'lar: GameController, kalıcılık, platform (asmdef Corrigible.Game)
   │  │  ├─ UI/                  uGUI görünümleri, özel Graphic'ler
   │  │  │  ├─ Widgets/          küçük, tek işlevli bileşenler (prefab'ların kökleri)
   │  │  │  ├─ Modals/           pencere bileşenleri
   │  │  │  ├─ Teams/            saha ekipleri kartı ve harita işaretleri (deneysel mod)
   │  │  │  └─ Raid/             baskın sahnesinin paneli (RaidView)
   │  │  ├─ Raid/                3D baskın tahtası, parçalar, RaidLauncher (deneysel mod)
   │  │  └─ Tutorial/            eğitim sistemi
   │  ├─ Resources/Localization/strings.txt   çeviri tablosu (tr → en)
   │  ├─ Steam/                  Steamworks.NET entegrasyonu (paket yoksa derlenmez)
   │  ├─ Editor/                 editör araçları (CorrigibleDebug)
   │  ├─ Tests/EditMode/         NUnit testleri
   │  ├─ Prefabs/UI/             UI prefab'ları
   │  ├─ Scenes/Main.unity       ana sahne (build listesinde ilk)
   │  ├─ Scenes/Baskin.unity     baskın: 3D taktik savaş, ana sahnenin üstüne eklemeli yüklenir
   │  ├─ Art/Raid/               baskın malzemeleri ve son işlem profili
   │  ├─ Prefabs/Raid/           3D baskın parçaları (karo, duvar, dolaplar, birimler)
   │  ├─ Settings/               BalanceConfig, UiTheme, TutorialSequence asset'leri
   │  └─ UI/Sprites/             Rounded.png, Outline.png (9-dilimli yuvarlak köşe)
   └─ TextMesh Pro/              TMP Essential Resources (LiberationSans SDF + Türkçe için dinamik fallback)
```

## Assembly'ler

| asmdef | Referanslar | Not |
|---|---|---|
| `Corrigible.Core` | — | `noEngineReferences: true`. Deterministik simülasyon, veri, profil, kayıt |
| `Corrigible.Game` | Core, Unity.InputSystem, UnityEngine.UI, Unity.TextMeshPro | Sunum ve oyun döngüsü |
| `Corrigible.Steam` | Game, com.rlabrecque.steamworks.net | `defineConstraints: STEAMWORKS_NET`; paket kurulunca otomatik derlenir |
| `Corrigible.Editor` | Core, Game, UnityEngine.UI, Unity.TextMeshPro, Unity.InputSystem | yalnızca Editor |
| `Corrigible.Tests.EditMode` | Core (+ test framework) | EditMode testleri |

## Katmanlar

```
 Girdi (uGUI tıklamaları, Boşluk)
        │
        ▼
 Görünümler (CrisisDeskView, ModalView, TutorialController…)  ──okur──►  Algı Katmanı: sim.Shown / sim.Global / sim.EndRevealed
        │ komut                                                                         ▲
        ▼                                                                               │
 GameController  ── Step/komut ──►  Simulation (Core)  ── WorldState ───────────────────┘
   (zaman, kayıt,                    │  olaylar (SimEvent: Log, News, Gift, End, Advice, Veil)
    profil, başarım)  ◄── Flush ─────┘
```

- **`Simulation`** (`Core/Simulation.cs`): tüm kurallar. `NewGame(profile, type, seed, balance)`, `Step()`, komutlar
  (`Act`, `StartResearch`, `ResolveGift`, `Verify`, `GlobalKillSwitch`, `DismissAdvice`, `SetResearchReserve`),
  Algı Katmanı (`Shown`, `Global`) ve türetilmiş değerler (`FundRate`, `SgRate`, `AlignSpeed`, `ResearchNeed`,
  `VetoThreshold`, `SalterFon/Sg`, `MonitorCost`, `AuditCost`…). Komutlar `ActResult { Ok, Msg }` döner.
- **Deneysel mod** (`Core/Teams.cs`, `Simulation`'ın partial'ı): `NewGame(..., exp: ExpOptions)` ile açılır (`S.Exp`).
  Zaman ölçeği yardımcıları `Per`/`Prob`/`Dur`/`Span`; saha ekipleri (`S.Teams`, `MoveTeam`, `HireTeam`, `StepTeams`);
  baskın (`RaidCheck`, `StartRaid` → `S.Raid`, `ResolveRaid(RaidResult)`). `S.Raid` varken `Step` hiçbir şey yapmaz.
  `S.Exp` yoksa bu kodların hiçbiri çalışmaz ve RNG tüketmez.
- **`Baskin`** (`Core/Baskin.cs`): `Prototype/baskin.js`'in birebir portu. 8×8 taktik savaş motoru, kendi RNG'si,
  DOM/Unity yok. `Create(RaidParams)` → `BState`; `DoMove`, `DoAct`, `Undo`, `Preview`, `EndTurn`, `Retreat`, `Result`.
  Strateji tarafına yalnızca `RaidResult` döner (kayda `raidres` komutu olarak yazılır; taktik hamleler kaydedilmez).
- **`WorldState`** (`Core/WorldState.cs`): oyunun tüm durumu (bölgeler, haberler, günlük, araştırma, illüzyon, çekirdek,
  tahminler…). Görünümler doğrudan yazmaz.
- **`GameController`** (`Game/GameController.cs`): gerçek zamanı oyun gününe çevirir (hız, `Hold`, `TutorialHold`),
  `Step` + `Flush` (olayları `EventsRaised` ile dağıtır, oyun sonunda profil/başarım), kayıt/yükleme, yeni oyun.
  Görünümler simülasyonu **yalnızca** onun komut metotlarıyla değiştirir. Olaylar: `Changed`, `EventsRaised`,
  `NewGameStarted`, `GameLoaded`.
- **Olay akışı:** `Simulation` olayları `S.Events`'e ekler; `GameController.Flush()` bunları boşaltıp dağıtır. Pencereler
  (`ModalView`) Gift/Veil/End olaylarına, masa (`CrisisDeskView`) News olaylarına tepki verir.

## Determinizm ve parite

- RNG: `Rng` (mulberry32, `Core/Rng.cs`), JS'teki ile bit düzeyinde aynı. Durumu `S.Rng.State`.
  `Js.Round` = JS `Math.round` (`Math.Floor(v + 0.5)`). Tüm sayılar `double`.
- C# `Simulation` ile `Prototype/sim.js` aynı tohum ve komutlarla aynı oyunu oynar. Bu,
  `SimulationTests.Rng_MatchesBrowserPrototype` ve `FullGame_MatchesBrowserPrototype` ile kilitlidir.
  JS'teki `withRng` sarmalayıcısı, günler arasındaki komutların RNG durumunu oyun durumuna (`S.rngState`) yazar;
  C#'ta RNG zaten `WorldState`'in parçasıdır.
- Aynı tohum + aynı komutlar = aynı oyun (`SameSeedAndCommands_GiveSameGame`).

## Kayıt / devam

- `GameRecord` (`Core/GameRecord.cs`): `type, seed, hasProfile, profileAtStart, balance, commands[], day`.
  `SavedCommand { day, kind, a, b }`; türler: `act, kill, research, gift, verify, advice, reserve` ve deneysel modda
  `team, hire, raid, raidres` (`RaidResult.Encode`). Deneysel ayarlar `hasExp/exp` alanlarında.
- `Simulation.Record` her başarılı komutu `Rec(...)` ile ekler. `Replay.Restore(rec)` yeni oyunu kurar, her komutun gününe
  kadar `Step` eder ve komutu uygular, sonra kayıt gününe kadar ilerler.
- `GameController.Save()` → `persistentDataPath/savegame.json` (JsonUtility). `SaveAndRestore_ReproducesExactState`
  testi JSON gidiş-dönüşünü doğrular.
- **Yeni bir oyuncu komutu eklerken:** `Simulation`'a `XCore` + `public ActResult X(...) => Rec(XCore(...), "kind", ...)`,
  `Replay.Restore`'a `case "kind"`, JS'e aynı fonksiyon, test.

## Baskın sahnesi (`Scenes/Baskin.unity`)

```
Raid Camera     ortografik, (35°, 45°) izometrik; tahta ekranın solunda. URP son işlem açık
Key Light, Rim Light, Global Volume (Art/Raid/RaidVolume: bloom, ton eşleme, vinyet)
Board           [RaidBoard3D] Tiles / Pieces / Marks çalışma anında kurulur (Settings/RaidPieceLibrary)
Baskin          Canvas sıra 12 [RaidView]: Root → Title, Turn, Side (hedef, ekip kartları, düğmeler, günlük),
                Legend, WinLabel, Brief, Result
```

- `RaidLauncher` (ana sahnede, Game nesnesi): `S.Raid` oluşunca sahneyi `LoadSceneMode.Additive` ile yükler, etkin
  sahne yapar (ışık ayarları için) ve ana kamerayı, dinleyicisini, KrizMasasi/Pencereler/Egitim canvas'larını kapatır
  (nesneleri değil bileşenleri; görünümlerin olay abonelikleri sürer). Sahne kapanınca hepsini geri açar.
- `RaidView` sahne açılınca `GameController`'ı bulur ve `S.Raid.Params` ile `Baskin.Create`'i çağırır; bitince
  `ResolveRaid` ve sahneyi kapatır. Ana sahne yoksa (sahne editörde tek başına açıldı) örnek bir baskın kurar.
- `RaidBoard3D`: kare (x, y) → dünya `(x−3.5, 0, 3.5−y)`. Karolar `RaidCell`, parçalar `RaidPiece` taşır; fare ışını
  hangi kareye çarptığını buradan okur (arayüzün üstündeyse yok sayılır). Vurgular ve oklar `UI/Default` tabanlı
  malzemelerle (renk köşe renginden/MPB'den; oklar `unity_GUIZTestMode = Always` ile her zaman üstte).
- Canlandırma: bir eylem ya da tur bitince motor durumu anında değiştirir ve `FxList`'i doldurur; tahta görseli eski
  hâlindeyken efektleri sırayla oynatır (saldırı/atış, hasar yazısı, itme, ölüm, kırılma, uyanma), sonra `Sync` ile
  son duruma eşitlenir. Oynatma sırasında girdi kapalıdır (`Busy`).
- Parçalar Unity primitive'lerinden yapılmış prefab'lardır (`Prefabs/Raid`); görsel iyileştirme için prefab'ın
  `Body` altını gerçek modelle değiştirmek yeterli (`RaidPiece` alanları: body, tinted, hp, order, flag, selectRing).

## Sahne (`Scenes/Main.unity`)

```
Main Camera
Game                [GameController]           BalanceConfig bağlı
EventSystem         [InputSystemUIInputModule]
UI
├─ KrizMasasi       Canvas sıra 0   [CrisisDeskView]
│  └─ App           TopBar (Brand, Day, Speed), Gauges (5× Gauge prefab), Advisor,
│                   Main (MapCard/MapArea/Map [MapView] + Legend, SidePanel: RegionPanel [RegionPanelView] (Detail/Field: Saha),
│                         Teams [TeamsView], Tabs, GlobalKillSwitch),
│                   News (Ticker, Feed: 8× HeadlineRow prefab)
├─ Pencereler       Canvas sıra 10  [ModalView]
│  └─ Overlay (kapalı)  IntroModal (+ Experimental düğmeleri), GiftModal, VeilModal, FakeWinModal, EndModal
└─ (Game nesnesinde RaidLauncher: baskında Baskin sahnesini yükler, ana kamera ve masa canvas'larını kapatır)
├─ Egitim           Canvas sıra 15  [TutorialController]
│  └─ Layer (kapalı)    DimTop/Bottom/Left/Right, Highlight, Callout
└─ Bildirim         Canvas sıra 20  [ToastView]
```

- Tüm Canvas'lar Screen Space Overlay, CanvasScaler 1920×1080, match 0,5. Scene görünümünde (2D) görünürler.
- **Pencereler ve eğitim katmanı edit modda kapalıdır** (masayı örtmesin). Düzenlemek için Hierarchy'de Overlay'i/Layer'ı
  ve ilgili pencereyi aç, iş bitince kapat.
- Harita: `Map` (AspectRatioFitter 1000:480) içinde `Land` (`MapLandGraphic`: enlem çizgileri + kara noktaları),
  enlem etiketleri, `Links` (`MapLinksGraphic`: bağlantılar, uydu hatları), `Regions` altında 12× `RegionNode` prefab'ı,
  `TeamPaths` (`MapTeamsGraphic`: yoldaki ekiplerin kesik çizgileri) ve `Teams` (`TeamMarker` prefab'ları).
  Düğümler haritaya **oranlı anchor**larla yerleşir (`RegionNode.Setup(id)`), harita büyüyünce ölçeklenir.
- Harita koordinatları simülasyonla aynı 1000×480 eşdikdörtgen düzlem (`MapSpace`, `GameData.Regions[].X/Y`).

## UI yaklaşımı

- **uGUI + TextMeshPro.** (Önce UI Toolkit denendi; Scene görünümünde görünmediği için kullanıcı isteğiyle uGUI'ye geçildi.)
- **Prefab'lar** (`Prefabs/UI`): `RegionNode`, `ResearchRow`, `HeadlineRow`, `LogLine`, `TextLine`, `AxisLabel`, `Gauge`,
  `Chip`, `ActionButton`, `PrimaryButton`, `TextButton`; deneysel mod için `TeamRow`, `TeamMarker`,
  `RaidUnitCard` (3D baskın parçaları `Prefabs/Raid`'de; dünya yazılarının malzemesi `Art/Raid/Materials/WorldLabel.mat`). Örneğe özel veri override olarak durur
  (ör. `RegionNode.regionId`, `ResearchRowView.researchId`).
- **Widget bileşenleri** (`Game/UI/Widgets`): `StyledButton` (zemin/çerçeve/yazı + devre dışı soluklaşma; sekmelerde
  çerçeve = alt çizgi), `GaugeView`, `ChipView`, `ActionButtonView`, `ResearchRowView`, `HeadlineRowView`, `LogLineView`.
  Görünüm sınıfları bunları `[SerializeField]` ile tutar; dinamik listeler prefab'tan üretilir (`UiUtil.Respawn`, `FillLines`).
- **Panel görünümü:** her panelin/düğmenin ilk çocukları `Background` (yuvarlak köşe Image) ve `Border` (çerçeve Image);
  ikisi de `LayoutElement.ignoreLayout`. Image'ı doğrudan layout'lu objeye koyma (bkz. GELISTIRME.md → tuzaklar).
- **Özel çizim:** `UiMesh` (kenarı yumuşatılmış çizgi/disk/yay), `ShapeGraphic` (disk, halka, kesik halka, tarama,
  onay/ünlem rozeti), `MapLandGraphic`, `MapLinksGraphic`, `EndChart` (oyun sonu grafiği, üzerine gelince bilgi).
  Hepsi `MaskableGraphic`; edit modda da çizildikleri için Scene'de görünürler.
- **Tema:** `UiTheme` ScriptableObject (`Settings/UiTheme.asset`): durum renkleri (tehdit, uyarı, doğrulama, iyi…)
  koddan buradan okunur. Sabit panel/yazı renkleri prefab ve sahnede durur. `Palette` sınıfı yalnızca varsayılanları tutar.
- **Etkileşim yardımcıları:** `UiUtil.OnClick` (tıklamadan sonra seçimi bırakır; Boşluk tuşu düğmeye gitmesin),
  `UiHover` (üzerine gelince çerçeve/yazı rengi), `StyledButton.SetEnabled`.
- Görünümler ve sorumlulukları:
  - `CrisisDeskView`: üst şerit, göstergeler (fon notunda gelir ve araştırma harcaması), danışman, sekmeler, araştırma
    listesi, acil durum fonu seçici, YZ günlüğü, dosya, Küresel Son Şalter (iki aşamalı onay), haber bandı, Boşluk tuşu.
  - `MapView` + `RegionNode`: harita durumu, seçim.
  - `RegionPanelView`: seçili bölge, dört eylem; deneysel modda baskın düğmesi ve Saha bölümü (ekip gönderme).
  - `TeamsView` + `MapTeamsGraphic`/`TeamMarkerView`: ekip listesi, işe alım, haritada ekipler.
  - `RaidView`: baskın ekranı. `Raid` olayında ya da kayıt `S.Raid` ile yüklenince açılır; `Baskin` motorunu doğrudan
    sürer, bitince `GameController.ResolveRaid`. Klavye: 1–3 birim, Q yetenek, Z geri al, Enter tur bitir, Esc.
  - `ModalView`: hangi pencere ne zaman; sahte zafer sırasında `Sim.HideEnding`.
  - `TutorialController`: eğitim (aşağıda). `ToastView`: kısa bildirimler.

## Eğitim sistemi (`Game/Tutorial`)

- `TutorialSequence` (ScriptableObject): `steps` (başlık, metin, hedef, `showWhen`, `completeWhen`, `pauseGame`,
  `openTab`) ve `tips` (kimlik, başlık, metin, hedef, tetikleyici).
- `TutorialCondition` → `TutorialConditions.Check(...)`: oyun durumundan okunur (RegionSelected, MonitorBuilt,
  TimeRunning, AiDetected, AlignmentStarted, ObservationPoint, AuditUsed, GiftOffered, CoreKnown, VeilTorn,
  DependencyAbove40, TrustBelow30, ResearchCompleted, ResearchStarved…).
- `TutorialTarget` → `TutorialController.targets` listesinde sahnedeki RectTransform'a bağlanır. Controller hedefin
  çevresini karartır (4 dikdörtgen, tıklamayı engellemez), çerçeve çizer, açıklama kutusunu sığan tarafa yerleştirir
  (sığmazsa hedefin sol alt köşesine), hedef bir ScrollRect içindeyse görünür alana kaydırır.
- Pencere açıkken yalnızca o pencereyi hedefleyen içerik gösterilir (ör. hediye ipucu). İpuçları rehberli adım
  görünmüyorken çıkar. Kayıt: PlayerPrefs `corrigible.tutorialDone`, `corrigible.tipsSeen` (virgüllü kimlik listesi).
- **Enum'a değer eklerken sona ekle:** asset ve sahne enum'ları sayı olarak saklar; araya eklemek mevcut verileri kaydırır.

## Lokalizasyon (Türkçe kaynak, İngilizce çeviri)

Unity Localization paketi kullanılmadı: metinlerin çoğu motordan bağımsız Core'da (haber, günlük, danışman, eylem
mesajları) üretiliyor ve Core `UnityEngine`'e bağlanamaz. Bunun yerine gettext tarzı hafif bir sistem var:

- **`Core/Loc.cs`:** kaynak dil Türkçe; koddaki Türkçe metin anahtardır. `Loc.T(src)` çevirir, `Loc.F(src, args)`
  çevirip dilin sayı biçimiyle (`1,5` / `1.5`) biçimler, `Loc.N(src)` yalnızca işaretler (statik başlatıcılarda;
  gösterimde `Loc.T`). Tabloda olmayan metin kaynak hâliyle gösterilir ve `Loc.Missing` tetiklenir (geliştirme
  sürümünde konsola uyarı).
- **Tablo:** `Resources/Localization/strings.txt`, sekmeyle ayrılmış: `tr<TAB>en`, ilk satır dil kodları. Yeni dil =
  yeni sütun + `Loc.Languages`'e kod. `\n`, `\t`, `\\` kaçış dizileri. `#` satırları yorum (kaynağın geldiği dosya).
- **Statik veri:** `GameData` tanımlarının `Name`/`Desc`/`Title`... alanları okunurken çevrilen özelliklerdir
  (`Loc.N` ile işaretli kaynak saklanır). `Baskin.UnitTypes[..].Name` de öyle; `ObjNames`, `Abilities`, `Layouts`,
  ajans ve ekip adları gösterimde `Loc.T` ile çevrilir. Araştırma kolu (`Branch`) kimliktir; gösterirken `Loc.T`.
- **Sahne/prefab:** sabit yazılarda `LocalizedText` bileşeni (`Game/Localization`): kaynak metni saklar, açılışta TMP
  metni hâlâ kaynağa eşitse çevirir (görünümlerin doldurduğu metne dokunmaz). Eğitim metinleri
  (`TutorialSequence.asset`) gösterilirken çevrilir.
- **Dil seçimi:** `Game/Localization/Localization.cs`. Sahne yüklenmeden önce (`RuntimeInitializeOnLoadMethod`)
  PlayerPrefs `corrigible.language`'dan, yoksa sistem diline göre (Türkçe → tr, diğerleri → en) ayarlanır. Giriş
  penceresindeki dil düğmesi dili değiştirip sahneyi yeniden yükler; simülasyonun ürettiği metinler kayıttan yeniden
  oynatılırken yeni dilde üretilir. Oyun ortasında dil değiştirilmez.
- **Büyük harf:** `UiUtil.Up` Türkçede `i → İ`, diğer dillerde `i → I`.
- **Araç:** `Editor/LocalizationTool.cs` (menü **Corrigible > Lokalizasyon**): kaynakları koddan (`Loc.T/F/N("...")`),
  eğitim asset'inden, prefab ve sahnelerdeki `LocalizedText`'lerden toplar; tabloyu günceller (yeni satırlar boş
  çeviriyle, silinenler dosya sonunda yorum olarak), doğrular (eksik çeviri, `{n}` yer tutucu ve `<b>/<color>` etiket
  uyumu), yeni TMP metinlerine `LocalizedText` ekler.
- **Testler** (`LocalizationTests`): tablo tam mı; üç tam oyun + baskınlar İngilizce oynanınca hiç eksik çeviri ya da
  Türkçe metin çıkmıyor mu; dil oyunun sonucunu değiştiriyor mu.

## Ses (`Game/Audio`)

- **`AudioDirector`** (ana sahnede `Audio` nesnesi): simülasyonu yalnızca okur; `EventsRaised` olaylarından ve her
  karede durumdan (yetenek sayısı, Yorumlanabilirlik, bölgelerin `Off` değeri) ses tetikler. Rastgelelik
  `UnityEngine.Random`; simülasyon RNG'sine dokunmaz, parite etkilenmez. `AudioDirector.Instance.GlobalKillArmed()`
  Küresel Son Şalter'in ilk basışında `CrisisDeskView`'den çağrılır.
- **Uyarlanır müzik:** altı döngü katmanı aynı anda (`PlayScheduled`) başlar, sesleri hedefe kayar. Katmanlar
  **gösterilen** değerlere bağlı (`sim.Global()`): drone her zaman, nabız Tekillik %25+, arpej %50+, gerilim %75+,
  sıcak majör ped bağımlılıkla (Sessiz Zafer'in tehlikeli huzuru), "grit" (şebeke uğultusu, cızırtı) gerçek dünya.
  **İllüzyonda ses de yalan söyler:** gösterilen Tekillik düştüğü için gerilim katmanları çekilir, grit susar, alarm
  haberleri yumuşak bildirime döner. Perde yırtılınca parazit patlaması ve grit geri gelir. Duraklatınca alçak
  geçiren filtre (900 Hz); baskın sahnesi açıkken müzik kısılır.
- **YZ motifi:** her yetenek bir nota ekler (`SoundBank.motif`); tını rakibe göre (Solucan bit-crush kare dalga,
  Manipülatör nefesli testere, Kâhin cam çan). Yorumlanabilirlik bitmişse sıradaki nota soluk olarak önceden çalar.
- **Haber bandı:** ajansa özel jingle; sahte ajansın jingle'ının üçüncü notası yarım ses kayık. Ard arda haberlerde
  teleks tıkırtısı.
- **Son Şalter:** şalter + şebeke inişi. Küresel: kapak sesi; ikinci basışta (Karanlık Çağ) bütün ses kesilir,
  4 sn sonra yalnızca rüzgâr.
- **`SoundBank`** (`Settings/SoundBank.asset`): bütün klipler ve karışım ayarları. Şimdiki klipler yer tutucu,
  `Editor/PlaceholderAudio.cs` (menü **Corrigible > Ses > Yer tutucu sesleri üret**) kodla sentezler
  (`Audio/Placeholder/*.wav`; Re minör, 90 BPM, 16 vuruşluk döngüler). Gerçek seslerle değiştirmek için klipleri
  SoundBank'ta değiştir; müzik katmanları aynı uzunlukta ve döngüye uygun olmalı.
- **Tek seferlik sesler:** görünümler `Sfx.Play(Cue.X, volume, pan, pitch)` çağırır (`Game/Audio/Sfx.cs`); klipler
  `SoundBank.cues`'da (ses düzeyi + perde oynaması). Bağlantılar: `StyledButton` (tık, üzerine gelme), `ToastView`
  (hata), `CrisisDeskView` (bölge seçimi: haritadaki konuma göre sağ-sol, izlenmeyen bölge telsiz filtresinden;
  eylem sonuçları, veto, doğrulama), `ModalView` (hediye kararı; kabul edilen hediye YZ motifine karışır),
  `FakeWinModal` (sahte fanfar, bozulma). Yönetmen durumdan da çalar: araştırma başladı/bitti/sabote edildi, danışman,
  ekip telsizi, YZ günlüğü harf sesleri (profil büyüdükçe oyuncunun tık sesine dönüşür), sonlara özel kapanışlar.
- **Baskın:** `RaidBoard3D` efekt oynatırken (`Animate`) her efektin sesini ekran konumuna göre sağ-sol ile çalar;
  `RaidView` birim seçimi, yetenek (kesici/EMP şarjı/kanca), tur bitirme, niyet "kilit" sesleri (sıra numarasıyla
  yükselen perde, yalan olabilecek niyette titrek), kapı kırma, geri çekilme telsizi, sonuç ve son tur saatini çalar.
- **Döngüler** (`LoopCue`): güven 30'un altına inerken kalp atışı, 2×/4× hızda saat, baskında bina uğultusu ve son tur
  saati.
- **Dinleyici:** ana kamerada AudioListener var; baskında RaidLauncher onu kapatır, baskın kamerasınınki çalışır
  (iki dinleyici aynı anda açık olmamalı).
- **Ayarlar penceresi** (`SettingsModal`, Pencereler canvas'ında; görsel kökü `Settings`, edit modda kapalı): genel,
  müzik, efekt ve ortam kaydırıcıları, ses aç/kapa, dil. Üst çubuktaki "Ayarlar", giriş penceresindeki "Ses ayarları"
  ya da Esc ile açılır (baskın sırasında değil); açıkken oyun zamanı durur. Değerler `AudioPrefs`'te (PlayerPrefs
  `corrigible.audio.*`): genel düzey ve sessiz mod `AudioListener.volume`'a, kanallar `AudioDirector`'ın kaynaklarına
  uygulanır (ortam = grit, rüzgâr, saatler, bina uğultusu; kalp atışı efektlere sayılır).
- AudioMixer yok (betikle oluşturulamıyor); karışım `AudioDirector`'da ses düzeyi ve `AudioLowPassFilter`'la yapılıyor.
  Ses ayarları menüsü gelince bir mixer'a geçmek doğal adım.

## Kalıcılık ve platform

- `ProfileStore` → `player_profile.json`; `GameController` → `savegame.json` (ikisi `persistentDataPath`).
- `Platform.Current` (`IPlatformServices`): varsayılan `LocalPlatform` başarımları konsola yazar. `Corrigible.Steam`
  derlenince `SteamPlatform` kendini `RuntimeInitializeOnLoadMethod` ile kaydeder. Steam Cloud için
  `persistentDataPath` Steamworks'te Auto-Cloud'a eklenir (kod gerekmez).

## Editör araçları

- `CorrigibleDebug` (`Editor/CorrigibleDebug.cs`): ajanların oyunu test etmesi için. `Capture(path)` (Canvas'ları
  geçici olarak kameraya bağlayıp PNG alır; edit ve Play modda çalışır), `Click("hiyerarşi/yolu/sonu")`,
  `ButtonText(...)`, `Advance(gün, hediyeKararı)`, `Status()`, `Select(bölgeId)`, `CloseModal()`, `Render()`.
