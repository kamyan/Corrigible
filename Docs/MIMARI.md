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
| `Corrigible.Desk` | Core (yalnızca `Loc`), UnityEngine.UI, Unity.TextMeshPro, Unity.InputSystem | Analist masası arayüzü (UI_C); oyun kuralı görmez |
| `Corrigible.Desk.Editor` | Core, Desk, UI, TMP | Masa varlık hattı (sprite/atlas/font/tema), yakalama, `DeskUiKit` |
| `Corrigible.Desk.Tests` | Desk, Desk.Editor (+ test framework) | Masa EditMode testleri |

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
  `team, hire, raid, raidres` (`RaidResult.Encode`). Yeni ekonomi komutları `operation, recovery, diplomacy, research_pause`.
  Deneysel ayarlar `hasExp/exp` alanlarında. Biçim `version=3`; ekonomik kural profili ayrı `ruleVersion` alanında.
  v1/v2 kayıtlar eski kural profiliyle yeniden oynatılır ve devam eder.
- `Simulation.Record` her başarılı komutu `Rec(...)` ile ekler. `Replay.Restore(rec)` yeni oyunu kurar, her komutun gününe
  kadar `Step` eder ve komutu uygular, sonra kayıt gününe kadar ilerler.
- `GameController.Save()` → `persistentDataPath/savegame.json` (JsonUtility). `SaveAndRestore_ReproducesExactState`
  testi JSON gidiş-dönüşünü doğrular.
- **Yeni bir oyuncu komutu eklerken:** `Simulation`'a `XCore` + `public ActResult X(...) => RunCommand(() => XCore(...), "kind", ...)`,
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
│  ├─ App           TopBar (Brand, Day, Ayarlar, Speed), Gauges (5× Gauge), Advisor (Göster, Anladım),
│  │                Main/MapCard (Map [MapView] + Legend, bütün genişlik), News (yalnızca kayan Ticker),
│  │                Dock (pencere düğmeleri + KillSwitch)
│  ├─ Windows       [DeskWindows] sürüklenebilir pencereler: Objective (hedef kartı), RegionWindow (RegionPanel),
│  │                ResearchWindow, LogWindow, GuideWindow, TeamsWindow, NewsWindow (haber listesi)
│  └─ Tooltip       [TooltipView]
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

## Pencereler (kriz masası)

- Harita ekranın merkezidir; geri kalan her şey küçük pencerelerde. `WindowView` (Game/UI/Windows): başlık çubuğundan
  sürüklenir, bırakınca konumu PlayerPrefs `corrigible.window.<key>`'e yazılır, başlığa çift tıklamak varsayılan yere
  döndürür, ekrandan taşmaz. Pencereler `Windows` katmanında merkez çapalı, sol üst pivotlu.
- `DeskWindows`: yan pencerelerden (0 araştırma, 1 günlük, 2 rehber, 3 ekipler, 4 haberler; eski sekme sırası) aynı anda
  biri açık. Düğmeler alt çubukta (Dock), kısayollar R, L, H, T, N. Kapalı pencerede yeni bir şey olunca düğmede rozet
  (araştırma masası boş, okunmamış günlük, ekip vardı, alarm haberi). Esc önce pencereleri kapatır, sonra Ayarlar'ı açar.
- Bölge kartı (`RegionWindow`) haritada bölge seçilince düğümün yanında, açık yan pencereyle çakışmayan tarafta açılır;
  oyuncu taşıdıysa taşıdığı yerde. Kapatmak seçimi kaldırır. Hedef kartı (`Objective`) da sürüklenebilir pencere.
- `CrisisDeskView.ShowTab(i)` (eğitimin `openTab`'ı) ilgili pencereyi açar; danışmanın "Göster" düğmesi öğüdün
  pencereyi açar (`AdviceWindow`).

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

## Anlaşılırlık, aşamalı öğrenme, olay kartları, kampanya

- **`RuleOptions`** (`Core/Rules.cs`, `NewGame(..., rules)`, kayıtta `hasRules/rules`): Unity'ye özgü kurallar; JS
  prototipinde yok. Null = temel kurallar (parite).
  - `Stage` (aşamalı öğrenme): 1 hediye yok + YZ Medya Kontrolü/Hediye Diplomasisi kazanamaz + illüzyon yok; 2 hediyeler;
    3 medya; 4 her şey. `GameController.OnboardingStage` = oynanan oyun sayısı + 1 (profilden); giriş penceresinden kapatılabilir.
  - `Events`: olay kartları (`Deck`, 10 ikilem). İlki 28. günde, sonra 22–34 günde bir; koşulu tutan kartlardan biri
    seçilir, oyun `S.PendingEvent` çözülene kadar durur (`S.Blocked`). Komut: `ResolveEvent(i)` → kayıtta `event`.
    Aşamalı öğrenmede 2. oyundan itibaren; kampanyada her zaman açık.
  - `ExtraTraits`: kampanya zorluğu (YZ bu yeteneklerle başlar).
- **Kırılma anları** (`S.Moments`): tespit, çekirdek bulundu/yok edildi/kaçtı, yeni yetenek, hediye kabulü, izlenmeyen
  bölgede sızma 50+, veto eşiği, ittifaktan çekilme, illüzyonun başladığı gün (oyun sırasında gizli), perde, araştırma.
  Yalnızca metin; kararları etkilemez. Oyun sonu penceresinde "Kırılma anları" listesi.
- **Kampanya** (`Game/Campaign`): 5 kriz (Solucan, Manipülatör, Kâhin, Manipülatör, Kâhin); 4. krizden itibaren YZ
  Aldatma, 5.'de Hızlanma ile başlar; kaybedilen her kriz bir yetenek daha ekler. Krizler arası `CampaignModal`
  12 yükseltmeden 3'ünü sunar (`CampaignUpgrade`: Balance değerlerini ya da başlangıç ekiplerini değiştirir).
  Durum `persistentDataPath/campaign.json`; süren krizin kaydı normal kayıt dosyasında.
- **Anlaşılırlık arayüzü** (`Game/UI/Explain.cs`, simülasyonu değiştirmez; yalan söyleyebilen değerleri `Shown`/`Global`
  üzerinden okur, illüzyonda ayrıntı vermez):
  - `ObjectiveView` (yan panelin üstü): Hizalama ve Silme ilerleme şeritleri, sıradaki adım.
  - Gösterge notlarında gidişat ("100'e ~48 gün", "15'e ~70 gün"; son ~10 günün **gösterilen** değişimi).
  - Göstergelerin üstüne gelince döküm (`TooltipView` + `HoverRelay`): tanım, formül, artıran/azaltanlar, öne çıkan bölgeler.
  - Bölge eylemlerinin üstüne gelince önizleme satırı (`RegionPanelView.preview`): bedel ve beklenen sonuç, veto ve çekirdek kaçışı uyarısı.

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

## Analist masası (`Assets/Corrigible/Desk`, UI_C)

Claude Design'ın "C · Analist Masası" yönü, mevcut kriz masasından ayrı bir katmanda kurulur. Talimat ve referanslar
`Docs/UI_C/` altında (`PROMPT.md`, `PLAN.md`, `KARARLAR.md`). Görünüm yalnızca görünüm modelini çizer; oyuna
`IDeskDataSource`/`IDeskCommands` ile bağlanacak (F8, `Corrigible.Desk.Bridge`).

- **Varlıklar:** `Art/Sprites` (135 sprite @2x, PPU 200; `DeskSpriteImporter` ayarları `sprites.json`'dan uygular),
  `Art/DeskUI.spriteatlasv2` (Sprite Atlas V2), `Art/Textures` (atlas dışı, Wrap Repeat, RawImage), `Art/Map`.
- **Fontlar:** `Resources/Fonts & Materials/` altında statik SDF + dinamik kardeş + DejaVu sembol yedeği; `<font>` etiketi
  TMP Settings değişmeden çalışır. Statik atlasta boşluk (U+0020) da bulunmalı; yoksa her boşluklu metin dinamik
  yedekten ayrı bir draw call açar (testle sabitlendi). Stil sayfası `Data/Theme/DeskStyles` metin başına atanır.
- **Tema:** `Resources/DeskTheme` (`Corrigible/Desk UI/Temayı tokens.json'dan doldur`); bileşenler renkleri `ThemeTint`
  anahtarıyla alır; `DeskTheme.Illusion` İllüzyon paletine geçirir. Sprite'lar adla `Resources/DeskSprites`'tan.
- **Bileşenler** (`Prefabs/Components`): PaperPanel, Stamp, TrustMark, CarbonValue, HandRing, DeskButton (+ varyantlar),
  KeyChip, SpeedKey, RadioCheck, Delta, ProgressBoxes, HatchBar, RangeBar, StatusChip, CountBadge, süsler;
  özel Graphic'ler `UILineGraphic`, `UIPolygonGraphic`. Prefab'lar kaynaktır (builder yok).
- **Biçim:** `DeskFormat` (47,3 / %37,2 / U+2212 / G-074 ↔ D-074), `DeskText` (tr-TR büyük harf), `DeskMath`
  (ibre, kama, projeksiyon).
- **C1 ekranı** (`Prefabs/Screens`): TopRail, GaugeCell ×5, AdvisorBand, GoalCard, ResearchFolder (FolderWindow + havuzlanan
  dal/satır), Map (pafta, yollar, düğüm/etiket/jeton, lejant, ölçek, analist katmanı, seçim ipliği), RegionDossier (kapak,
  sızma, kaynaklar, istatistik, eylem kartları, önizleme, saha), Telex, Dock, KillSwitch, Background. Kök `DeskPresenter`
  görünüm modelini `Bind` ile dağıtır; veri `MockDeskDataSource` (`Data/Mock/*.json` + `map.json`), komutlar `MockDeskCommands`.
- **Etkileşim (F3):**
  - `DeskPresenter` görünüm olaylarını niyete (`IDeskCommands`) çevirir.
  - `DeskInput` klavye haritasının tek sahibidir (R/N/T/L/H, 1–4, B, Boşluk, +/−, Esc; onay kartı açıkken yalnız Enter/Esc).
  - `WindowManager` pencere tablosunu, odak sırasını ve sekmeden serbest sürüklemeyi yönetir; yerleşim yapmaz (konum, boyut ve
    açılış durumu sahnede). Pencere olmayan paneller (harita, hedef kartı) `DeskDraggablePanel` ile taşınır. Haritaya bağlı
    çizimler `MapView.MapToWorld` ile düğümün o anki yerini izler.
  - Edit mod önizlemesi (`DeskEditPreview`, editör) sahnenin veri kaynağı `IDeskPreviewSource` ise onun önizlemesini bağlar:
    oyun sahnesinde `SimDeskDataSource.BuildPreview` → `GameController.PreviewOpeningGame` (açılış oyunu, hiçbir şey yazmaz).
    Önizleme kopyaları kaydedilmez ve düzenlenemez (`DeskUi.MarkPreview`).
    `FolderWindow` + `FolderTabHandle` klasör ve sekmeden oluşur.
  - `ContentWindowView` Haberler, Ekipler, YZ Günlüğü ve Rehber pencerelerini (`Prefabs/Windows`) çizer.
  - `DeskTooltip` metin ipucu ve `BreakdownTooltipView` döküm fişi için tek örnektir (`Screens/Tooltip`).
  - `ConfirmModalView` onay kartıdır (`Screens/ConfirmModal`).
  - `DeskHover` üstüne gelme olaylarını taşır; `DeskTween` bellek ayırmayan ease-out geçiştir.
- **İllüzyon (F4):**
  - `IllusionPresenter.Set(bool)` `DeskTheme.Illusion`'ı (palet, damgalar) değiştirir ve `IIllusionAware` görünümlere bildirir:
    MapView (analist katmanı → izler), GaugeCellView, AdvisorBandView, RegionDossierView, DockView, KillSwitch, `IllusionStraighten`.
  - `DeskPresenter` her bağlamada `Set(model.Illusion)` çağırır. Gösterilen değerler VM'dendir.
  - `ClueOverlay` (F10) C2 ipuçlarını gösteren geliştirici katmanıdır.
  - Geliştirme kısayolu: F6 sunumu veri değişmeden açıp kapar.
- **Olay bildirimi (F5):**
  - `EventModalView` (`Screens/EventModal`, Modal canvas) `DeskViewModel.Event`'i çizer. Parçaları:
    - `SpotlightGraphic`: karartma + delik + halka, tek mesh
    - `EventMemoView` + `EventSourceRow`
    - `EventOptionView` + `EventLineList`/`EventLineRow` + `EventPreviewRow`
    - YZ günlüğü, danışman notu, iplik, alt şerit
  - İmza `IDeskCommands.SignEventOption(i)`'ye gider.
  - Olay açıkken `DeskInput` yalnızca 1/2/3, Enter ve Boşluk (haritaya bak) dinler.
- **Oyun sonu (F6):**
  - `GameOverView` (`Screens/GameOver`, Modal canvas, tam ekran) `DeskViewModel.GameOver`'ı çizer.
  - Parçalar: `KpiCardView`, `StripChartView` (geometri `DeskChart`: eşleme, Catmull-Rom, bant), `TimelineCardView`.
  - Açıkken masa canvas'larını kapatır. Alt çubuk komutları `IDeskCommands`'a gider: ReturnToMainMenu, ReviewLogs, RetrySameSeed, Restart.
  - Varlık hattı: `DeskLinearTextures`, gömülü alfalı desenlerin Linear telafili kardeşini üretir.
- **Dil ve cila (F7):**
  - Sabit prefab metinleri `DeskLocText`'tir; menüyle `DeskStaticWords.cs`'ye (`Loc.N`) toplanır.
  - Sahte verinin İngilizcesi `MockEnglish` + `Data/Mock/mock_en.txt`'dir (yalnız sandbox).
  - Dile göre akış: `MapLegendFlow` (lejant), `EventMemoView.LayoutFields` (CSS ızgarası), `GameOverView` (damga başlığı izler).
  - Denetim araçları (Editor, Play):
    - `DeskTextChecker`: iki dilde taşma/çakışma/kalıntı ve kurallar → `captures/F7_metin_denetimi.txt`.
    - `DeskPerfProbe`: Profiler UI Details'tan canvas başına batch ve kırılma nedeni, boşta GC → `captures/F7_performans.txt`.
  - Kullanım ve oyuna bağlama rehberi: `Docs/UI_C/UYGULAMA_NOTLARI.md`.
- **Oyun köprüsü (F8, `Scripts/Bridge`, asmdef `Corrigible.Desk.Bridge`: Core + Game + Desk):**
  - `SimDeskBuilder` oyun durumundan `DeskViewModel` kurar (saf C#, testli). Algı Katmanı'ndan okur, kural uygulamaz.
  - Kısmi dosyalar: harita (projeksiyon + tasarımın yol kuralı), dosya ve araştırma, olay (ikilem/hediye, etki ayrıştırma), oyun sonu,
    ekranlar (giriş, kampanya, sahte zafer, uyarı fişleri, belge kipi: perde raporu, baskın emri, operasyon raporu).
  - `SimDeskDataSource` GameController olaylarıyla kirlenir, karede en çok bir kez kurar. `SimDeskCommands` niyetleri GameController'a iletir.
  - Görünüm durumu (seçim, okundu, uyarılar, gösterge geçmişi, açık kâğıtlar) `DeskBridgeState`'tedir; açık kâğıtlar
    `GameController.Hold`'u tutar.
  - Yeni ekranların düğmeleri komut dizisi taşır (`"Intro:Start"`, `"Raid:Start:weu"`…) ve `IDeskCommands.Run`'a gider.
  - Ayarlar `IDeskSettings` ile: oyunda `BridgeSettings` (`AudioPrefs`, dil sahneyi yeniden yüklemeden), sandbox'ta `MockDeskSettings`.
  - Taktik harita: DeskGame'de `RaidLauncher`, Baskin sahnesini masanın üstüne yükler; baskın sürerken masa canvas'ı, kamera ve girdi kapanır.
  - Eğitim: `DeskTutorialRunner` oyunun `TutorialSequence`/`TutorialConditions`'ıyla adımı ya da ipucunu seçer (`TutorialVM`);
    `DeskTutorialView` kendi canvas'ında (sıra 105) hedefi sahnedeki dikdörtgenlere bağlayıp karartır ve notu yerleştirir.
  - Araştırma panosu: `ResearchFolderView` dört aşama sütununu ve mevcut dal sırasını korur; `ResearchProjectCardView`
    kartları prefab şablonundan havuzlanır, `ResearchConnectionGraphic` gerçek `PrerequisiteId` bağlantılarını uGUI
    mesh'iyle çizer. `Stage`/`Unlock` Core metadatasıdır; açılma kararını Core `Req` ve YZ tespit koşulu verir.
    Kartlar yalnız `ResearchAction` yollar; köprü katalog kimliğiyle `BuyOperation`, Hizalama ile `StartResearch` /
    `SetResearchPaused` komutlarını seçer. Yeni krizlerde 14 kart ve Bütçe dahil 5 dal; eski kayıtlarda 9 kart vardır.
    Kaynak `ResearchFolder.prefab` ve `DeskGame.unity`dir; tek seferlik düzenleyici kaldırılmıştır. Taşan içerik dikey kayar.
    `BranchSpan` Hizalama eğitim hedefini korur. Biçim sürümü 3'tür; eski 1/2 kural profilleri korunur.
  - Dar alanlar için bağlamlı çeviri anahtarı: `SimDeskBuilder.Ctx("[bağlam] metin")`.
  - Ekonomi `SimDeskBuilder.ResearchEconomy` / `.Economy` ile sunulur; son günlük ödeme, canlı tahmin ve peşin
    işlem defteri ayrıdır. `EconomyRecoveryView` bölge dosyasında `Economy:Recover` / `Economy:Diplomacy` emirlerini,
    ekip penceresi `Team:Hire` emrini yayınlar. Düğme/metin/kaydırma referansları prefab ve sahnelerde serileştirilmiştir.
  - `DeskGame.unity` ve eski `Main.unity` yeni ekonomi işlemlerini sunar; sandbox mock'ları eski 9 kartı korur.
- **Sahneler:** `Scenes/DeskUI_Sandbox.unity` (C1; canvas'lar BG 0 / Map 10 / HUD 20 / Windows 30 / Ticker 40 / Bottom 41 /
  Overlay 60 / Modal 100 / Tooltip 110 / Debug 1000; F1–F4 ön ayar, F9 dil, F11/F12 referans katmanı) ve
  `Scenes/DeskUI_Gallery.unity` (bileşenler C5 stil rehberi koordinatlarında; ←/→ bölüm, F9 dil).
  Yakalama `Corrigible/Desk UI/Capture Game View` → `Docs/UI_C/captures/`, karşılaştırma `Docs/UI_C/tools/compare.py`.
