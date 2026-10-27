# Mimari

## Klasörler

```
Corrigible/                          (Unity proje kökü, git deposu)
├─ AGENTS.md, CLAUDE.md              yapay zeka ajanları için giriş
├─ Docs/                             bu belgeler; UI_Siginak (oda tasarımı), UI_Sade ve UI_C (masa/baskın arayüzü
│                                    kökenleri), SonSalter_Baskin_Paketi (baskın görsel referansı)
├─ Tools/unity-helpers.sh            Unity CLI kısayolları
└─ Assets/Corrigible/
   ├─ Core/                          saf C# simülasyon (asmdef Corrigible.Core, motor referansı yok)
   ├─ Game/                          GameController, BalanceConfig, platform; Audio/, Localization/, Raid/ (3B baskın tahtası)
   ├─ Desk/                          komuta odası ve ekran kâğıtları
   │  ├─ Scripts/Runtime/            asmdef Corrigible.Desk: Siginak/ (oda bileşenleri), Screens/ (rapor, son rapor,
   │  │                              ayarlar), Raid/ (baskın HUD'u), Sade/Map/ (kâğıt harita), Components/, Theme/, Core/
   │  ├─ Scripts/Bridge/             asmdef Corrigible.Desk.Bridge: oyunu sunuma bağlar (Siginak/ bağlayıcılar, SimDesk*,
   │  │                              baskın veri kaynağı)
   │  ├─ Scripts/Editor/             varlık hattı (Import/) ve oda araçları (Siginak/)
   │  ├─ Scenes/DeskGame.unity       oyunun sahnesi (build listesinde ilk)
   │  ├─ Prefabs/                    Components/ (kâğıt bileşenleri), Screens/ (EventModal, GameOver, SettingsCard),
   │  │                              Sade/Raid/ (baskın HUD, kart, panel, etiket prefab'ları)
   │  ├─ Resources/                  temalar (DeskTheme, SadeTheme, RaidTheme, SiginakTheme), DeskSprites, fontlar
   │  ├─ Data/                       tema kaynakları (tokens_*.json), harita ekip noktaları, stil sayfası
   │  ├─ Art/, Fonts/                sprite'lar, atlaslar, dokular, modeller, malzemeler; font dosyaları ve lisansları
   │  └─ Tests/EditMode/             asmdef Corrigible.Desk.Tests
   ├─ Scenes/Baskin.unity            taktik baskın; DeskGame'in üstüne eklemeli yüklenir
   ├─ Prefabs/Raid/                  3B baskın parçaları (karo, duvar, dolaplar, birimler)
   ├─ Art/Raid/                      baskın malzemeleri, dokuları, mesh'leri
   ├─ Audio/Placeholder/             yer tutucu sesler (SoundBank'a bağlı olanlar)
   ├─ Settings/                      BalanceConfig, RaidPieceLibrary, SoundBank
   ├─ Resources/Localization/        strings.txt (çeviri tablosu, tr → en)
   ├─ Editor/                        CorrigibleDebug, LocalizationTool
   ├─ Tests/EditMode/                Core ve lokalizasyon testleri
   └─ Steam/                         Steamworks.NET entegrasyonu (paket yoksa derlenmez)
```

## Assembly'ler

| asmdef | Referanslar | Not |
|---|---|---|
| `Corrigible.Core` | — | `noEngineReferences: true`. Deterministik simülasyon, veri, kayıt, taktik motor, `Loc` |
| `Corrigible.Game` | Core, Input System, UI, TMP | GameController, ses, dil seçimi, 3B baskın tahtası ve RaidLauncher |
| `Corrigible.Desk` | Core (yalnız `Loc` ve veri tipleri), UI, TMP, Input System | Oda ve ekran kâğıtları; oyun kuralı görmez |
| `Corrigible.Desk.Bridge` | Core, Game, Desk, UI, TMP, Input System | Oyunu odaya bağlar; Game'den Bridge'e ters bağımlılık yok |
| `Corrigible.Steam` | Game, com.rlabrecque.steamworks.net | `defineConstraints: STEAMWORKS_NET`; paket kurulunca derlenir |
| `Corrigible.Editor` | Core, Game, UI, TMP, URP | Editör araçları |
| `Corrigible.Desk.Editor` | Core, Desk, Bridge, Game, Editor, URP | Varlık hattı, oda yakalama ve denetim araçları |
| `Corrigible.Tests.EditMode` | Core, Game, Editor | Simülasyon, taktik motor, sığınak, lokalizasyon testleri |
| `Corrigible.Desk.Tests` | Core, Desk, Desk.Editor, Bridge, Game | Sahne bağlantıları, oda, raporlar, baskın sunumu testleri |

## Katmanlar

```
 Girdi (fare: odadaki hedefler, harita, fişler; klavye: DeskInput, SiginakDesk anahtarları)
        │
        ▼
 Oda bağlayıcıları (SiginakDesk, SiginakMap, SiginakShelter, SiginakCrt, SiginakShift, SiginakBriefing)
        │ komut                                            ▲ okur
        ▼                                                  │
 LiberationDeskView ── GameController ── komut/Step ──► Simulation (Core) ── WorldState
   (komut kapısı,       (kayıt, olaylar,                    │ olaylar (SimEvent: Raid, RegionLiberated, End)
    teleks gerekçesi)    vardiya)  ◄── Flush ───────────────┘
                                   │
                                   ├─► SimDeskDataSource → SimDeskBuilder → DeskPresenter (baskın raporu, son rapor)
                                   ├─► RaidLauncher (Baskin sahnesini yükler/kaldırır)
                                   └─► AudioDirector (yalnız okur)
```

- **`Simulation`** (`Core/Simulation.cs` + partial'lar): bütün kurallar. `NewGame(seed, balance)`, `Step()` (bir vardiya),
  komutlar (`StartLiberationRaid`, `ResolveRaid`, `SelectLiberationRegion`, `ShelterUpgrade`, `ShelterAssign`), denetimler
  (`LiberationRaidCheck`, `SelectLiberationRegionCheck`, `ShelterUpgradeCheck`, `ShelterAssignCheck`: komut yapılamıyorsa
  gerekçe metni) ve türetilmiş değerler (`Occupation`, `LiberationFrontier`, `NextLiberationSector`, `IncomeBreakdown`).
  Komutlar `RunCommand` üstünden geçer: `ActResult { Ok, Msg }` döner, fon değişimini `TransactionLedger`'a, komutu kayda
  yazar. Dosyalar: `CampaignLiberation.cs` (bölge/sektör durumu ve baskın), `Simulation.Shelter.cs` + `ShelterLogic.cs`
  (sığınak), `Teams.cs` (ekip), `Economy.cs` (gelir ve işlem defteri).
- **`WorldState`** (`Core/WorldState.cs`): oyunun tüm durumu (gün, fon, ekipler, süren baskın, baskın günlüğü, harekât ve
  sığınak, işgal geçmişi, kırılma anları, son). Görünümler doğrudan yazmaz. `S.Raid` varken `Step` çalışmaz (`Blocked`).
- **`Baskin`** (`Core/Baskin.cs`): 8×8 taktik savaş motoru, kendi RNG'si (`BState.Rs`), Unity yok. `Create(RaidParams)` →
  `BState`; `DoMove`, `DoAct`, `Undo`, `Preview`, `EndTurn`, `Retreat`, `Result`, `Intents`. Stratejiye yalnız
  `RaidResult` döner (kayda `raidres` olarak yazılır; taktik hamleler kaydedilmez).
- **`GameController`** (`Game/GameController.cs`): komut kapısı. `NewGame`, komut metotları, `AdvanceShift` (vardiya),
  `Flush` (olayları `EventsRaised` ile dağıtır; harekât bitince başarımı açar, kaydı siler), `Save`/`ContinueSaved`.
  `StoryHold` açılış ve emir belgesi açıkken vardiyayı engeller. Olaylar: `Changed`, `EventsRaised`, `NewGameStarted`,
  `GameLoaded`. Zaman kendiliğinden akmaz.
- **`LiberationDeskView`** (Bridge): odanın komut kapısı (`IDeskSurface`). Komutları GameController'a iletir, sonucu
  gerekçesiyle teleksten bastırır (`TelexReasons`), girdi kilidini (`InputBlocked`: açık ekran kâğıdı, hikâye, baskına
  geçiş) tek yerde hesaplar.

## Determinizm ve kayıt

- RNG: `Rng` (mulberry32, `Core/Rng.cs`). Durumu `S.Rng.State`. `Js.Round` = JS `Math.round` (`Math.Floor(v + 0.5)`);
  tüm sayılar `double`. Aynı tohum + aynı komutlar = aynı oyun (`LiberationCampaignTests`).
- `GameRecord` (`Core/GameRecord.cs`): `version` (4), `seed`, `balance`, `commands[]`, `day`. `SavedCommand { day, kind, a, b }`;
  türler `liberation_region`, `liberation_raid`, `raidres` (`RaidResult.Encode`), `shelter_upgrade`, `shelter_assign`.
- `Replay.Restore(rec)` yeni oyunu kurar, her komutun gününe kadar `Step` eder ve komutu uygular, sonra kayıt gününe
  kadar ilerler. Bu sürümden eski kayıtlar (kriz oyunu dönemi) yüklenmez.
- `GameController.Save()` → `persistentDataPath/liberation_savegame.json` (JsonUtility); her başarılı komutta, vardiya
  sonunda ve uygulama kapanırken. Kayda girmeyen oda durumu (teleks, evrak tepsisi, kamera pozu) `SiginakDeskStore` ile
  `liberation_desk.json`'a yazılır; tohum eşleşmezse yok sayılır.
- **Yeni bir oyuncu komutu eklerken:** `Simulation`'a `XCore` + `public ActResult X(...) => RunCommand(() => XCore(...), "kind", ...)`,
  `Replay.Restore`'a `case "kind"`, GameController'a ince bir metot, `LiberationDeskView`'a gerekçeli komut, test.

## DeskGame sahnesi (`Desk/Scenes/DeskGame.unity`)

```
EventSystem              [InputSystemUIInputModule]
DeskUI                   Overlay; Modal (sıra 100): EventModal (baskın raporu), GameOver (son rapor), SettingsCard
DeskPresenter (oyun)     DeskPresenter, DeskInput, SimDeskDataSource, SimDeskCommands, BridgeSettings, LiberationDeskView
Game                     GameController (BalanceConfig), RaidLauncher
Audio                    AudioDirector (SoundBank)
CommandPost3D            CommandPostDesk (kamera girdisi kapısı); Main Camera + SiginakCameraRig
SiginakRaidLink          baskın sahnesi yüklüyken odayı gizler, CRT geçişini RaidLauncher'a verir
SiginakRoom              oda: masa (sayaçlar, anahtar rayı, şalter, teleks, evrak tepsisi, Desk/MapMount altında kâğıt
                         harita SadeMapCanvas), raf (sığınak maketi, oda kartları), CRT, duvar; SiginakHud (Overlay 45:
                         kenar satırı, ayarlar plakası, vardiya raporu, emir belgesi, teleks şeridi, üzerine gelme etiketi)
SiginakCrtOverlay        Overlay 60, CRT'ye dalış geçişi
```

- Oda, yerleşim ve bileşen bağlantıları sahnede serileştirilir; kalıcı kurucu yoktur (son kurucu `eed2824`'te). Tasarım
  eşlemesi, poz/kamera ayrıntısı ve değişiklik günlüğü: `Docs/UI_Siginak/IMPLEMENTATION_MAP.md`.
- **Bağlayıcılar** (Bridge/Siginak): `SiginakDesk` (sayaçlar, anahtarlar, şalter), `SiginakMap` (kâğıt harita, görev ve
  cephe fişleri, ekip figürü), `SiginakShelter` (maket ve oda kartları), `SiginakCrt` (saha yayını, `IRaidTransition`),
  `SiginakShift` (vardiya dizisi, vardiya raporu, evrak tepsisi), `SiginakBriefing` (açılış ve emir belgesi),
  `SiginakTutorial` (eğitim: sahne kökünde kendi tuvali `SiginakTutorial`, sıralama 70, baskın HUD'unun üstünde; odanın
  dışında olduğu için baskında da çalışır; adımlar saf C# `TutorialFlow`, not `TutorialNoteView`; senaryolu ilk baskın
  `TutorialRaid` ve rehberi `RaidGuide`: `LiveRaidDataSource` eğitim baskın adımındaysa tahtayı senaryonun tohumuyla kurar,
  `RaidDataSource` girdiyi rehberin sıradaki adımıyla süzer, `RaidBoard3D.Marks.Guide` sarı çerçeveyi çizer). Modelleri saf
  C# kurucular üretir (`LiberationMapBuilder`, `ShelterViewBuilder`, `CrtViewBuilder`, `ShiftReportBuilder`,
  `BriefingBuilder`); odanın bileşenleri (Runtime/Siginak) yalnız modeli çizer.
- `CommandPostDesk` masa kamerasının girdi kapısıdır: açık bir ekran kâğıdı ya da sunumun kilidi (`ExternalBlocked`)
  varken kamera girdisi kapalıdır. Kamerayı `SiginakCameraRig` sürer (K1 masa, K2/K3 harita, K4 raf, K5 CRT).
- **Ekran kâğıtları:** `SimDeskDataSource` GameController olaylarıyla kirlenir ve karede en çok bir kez
  `SimDeskBuilder.Build(sim, state, raidScene)` çağırır: okunmamış baskın günlüğü varsa operasyon raporu (`EventVM`),
  harekât bittiyse son rapor (`GameOverVM`). `DeskPresenter` modeli `EventModalView` ve `GameOverView`'a dağıtır, kâğıt
  düğmelerinin komut dizilerini (`"Doc:Close"`, `"Restart"`) `SimDeskCommands`'a iletir. Ayarlar kartı `IDeskSettings`
  (`BridgeSettings`: `AudioPrefs`, dil) ile çalışır.
- `DeskInput` klavyenin tek sahibidir: ayarlar kartı açıkken Esc; son raporda Enter; rapor açıkken Enter/Esc kâğıt
  düğmeleri, Boşluk haritaya bakma; aksi hâlde 1/3 planlama, H kılavuz, Esc ayarlar/geri. Odanın anahtarları
  (`StationKeyRail`) ve vardiya raporu kendi tuşlarını dinler.

## Baskın sahnesi (`Scenes/Baskin.unity`)

```
Raid Camera       ortografik; SadeRaidCameraFraming, tahta merkezde
Desk Lamp, Cool Fill
BoardBase, Board  [RaidBoard3D] karolar / parçalar / işaretler (Settings/RaidPieceLibrary)
WorldLabels       dünya etiketleri
BoardBackground   [BoardBackgroundView] keçe, lamba ve tahta gölgesi (kamera Canvas'ı)
RaidHUDCanvas     LiveRaidDataSource, RaidCommands, RaidPreviewOverlay; RaidHud, ekip/birim/inceleme kartları
RaidEventSystem   [RaidEventSystemGate] ana EventSystem varken yereli kapatır
RaidPanelCanvas   RaidPanels (olay, onay, günlük, kurallar, geri çekilme, sonuç), RaidOutcomeSpot
```

- `RaidLauncher` (DeskGame'de, Game nesnesi): `S.Raid` oluşunca önce kamerayı CRT'ye daldırır (`SiginakCrt`), sonra
  sahneyi `LoadSceneMode.Additive` ile yükler; odanın kamera, dinleyici, canvas ve girdilerini kapatır (`hideDuringRaid`:
  nesneler değil bileşenler). Sahne kapanınca önceki `enabled` durumlarını geri yükler.
- `LiveRaidDataSource`, DeskGame'deki `GameController` ve `S.Raid.Params` ile `Baskin.Create`'i bir kez çağırır. Oyuncu
  sonuçtan rapora geçtiğinde `GameController.ResolveRaid` çalışır ve sahne kapanır. Baskın boyunca odanın `DeskInput`'u
  askıya alınır. Sahne tek başına açılırsa örnek bir sunucu baskını kurulur.
- `RaidDataSource` taktik komut/sunum sınırıdır: kurallar, hedefler ve önizleme Core'dan; görünür niyetler
  `Baskin.Intents` üzerinden gelir. UI kural, hasar ya da RNG hesaplamaz. `RaidHudView` klavyenin tek giriş noktasıdır
  (1–3 birim, Q yetenek, Z geri al, Enter tur bitir, Esc).
- `RaidBoard3D`: kare (x, y) → dünya `(x − 3,5, 0, 3,5 − y)`. Bir eylem ya da tur bitince motor durumu anında değişir;
  tahta efektleri (`FxList`) sırayla oynatır, sonra `Sync` ile eşitlenir. Oynatma sırasında girdi kapalıdır (`Busy`).
  Can/sıra etiketleri `RaidScreenLabel` prefab'ıyla ayrı Overlay canvas'ta çizilir.

## UI yaklaşımı ve tema

- **uGUI + TextMeshPro**; oda 3B, yazılar dünya ya da Overlay canvas'larda. Prefab ve sahne asıl kaynaktır.
- **Temalar** (ScriptableObject, `Desk/Resources`): `SiginakTheme` (oda), `SadeTheme` (harita, kâğıt), `RaidTheme`
  (baskın), `DeskTheme` (fontlar, ortak kâğıt renkleri). Token kaynakları `Desk/Data/*/tokens_*.json` ve
  `Docs/UI_C/spec/tokens.json`; içe aktarıcılar Corrigible menüsünde. Bileşenler renkleri anahtarla alır (`ThemeTint`).
- **Kâğıt bileşenleri** (`Desk/Prefabs/Components`): PaperPanel, Stamp, DeskButton (+ varyantlar), KeyChip,
  RadioCheck, Paperclip, Pin. Özel Graphic'ler: `UILineGraphic`, `UIPolygonGraphic`, `SpotlightGraphic`,
  `RegionMeshGraphic` (harita bölgeleri: sahnede serileştirilmiş çokgenler ve tıklama testi).
- **Biçim:** `DeskFormat` (sayı, yüzde, vardiya kodu), `DeskText` (tr-TR büyük harf), `TrNumber` (Türkçe ekler),
  `DeskMath`, `DeskChart` (son raporun şerit grafiği).
- **Sprite'lar ve fontlar:** `Desk/Art` (Sprite Atlas V2, `DeskSpriteImporter` ayarları `sprites.json`'dan), fontlar
  `Desk/Resources/Fonts & Materials` (statik SDF + dinamik kardeş + DejaVu sembol yedeği).

## Lokalizasyon (Türkçe kaynak, İngilizce çeviri)

Unity Localization paketi kullanılmadı: metinlerin bir kısmı motordan bağımsız Core'da üretiliyor ve Core
`UnityEngine`'e bağlanamaz. Bunun yerine gettext tarzı hafif bir sistem var:

- **`Core/Loc.cs`:** kaynak dil Türkçe; koddaki Türkçe metin anahtardır. `Loc.T(src)` çevirir, `Loc.F(src, args)`
  çevirip dilin sayı biçimiyle biçimler, `Loc.N(src)` yalnızca işaretler (statik tablolarda; gösterimde `Loc.T`).
  Tabloda olmayan metin kaynak hâliyle gösterilir ve `Loc.Missing` tetiklenir.
- **Tablo:** `Resources/Localization/strings.txt`, sekmeyle ayrılmış `tr<TAB>en`; `\n`, `\t`, `\\` kaçış dizileri; `#`
  satırları yorum (kaynağın geldiği dosya).
- **Sahne/prefab:** sabit yazılarda `DeskLocText` bileşeni kaynağı saklar ve Play'de çevirir.
- **Dil seçimi:** `Game/Localization/Localization.cs`: sahne yüklenmeden önce PlayerPrefs `corrigible.language`'dan,
  yoksa sistem diline göre. Masa (`DeskLoc`) ayarlar kartından dili sahneyi yeniden yüklemeden değiştirebilir.
- **Araç:** `Editor/LocalizationTool.cs` (menü **Corrigible > Lokalizasyon**): kaynakları koddan (`Loc.T/F/N("...")`) ve
  prefab/sahnelerdeki `DeskLocText`'lerden toplar, tabloyu günceller (yeni satırlar boş çeviriyle; kullanılmayanlar önce
  dosya sonunda yorum olarak durur, sonraki güncellemede düşer),
  doğrular (eksik çeviri, `{n}` yer tutucu ve `<b>` gibi etiket uyumu).
- **Testler** (`LocalizationTests`): tablo tam mı; rastgele oynanan harekâtlar ve baskınlar İngilizce oynanınca hiç
  eksik çeviri ya da Türkçe metin çıkmıyor mu; dil oyunun sonucunu değiştiriyor mu.

## Ses (`Game/Audio`)

- **`AudioDirector`** (DeskGame'de `Audio` nesnesi): simülasyonu yalnızca okur; rastgelelik `UnityEngine.Random`.
  Müzik katmanları: drone her zaman, warm işgal oranıyla, grit dünyanın uğultusu; hepsi alçak geçiren filtreyle
  "duvarın ardından". Bölge özgürleşince alarm, harekât bitince kapanış; baskında müzik kısılır, bina uğultusu girer.
- **Tek seferlik sesler:** `Sfx.Play(Cue.X, volume, pan, pitch)`; klipler `Settings/SoundBank.asset` → `cues`
  (`Cue` enum'ı asset'te sayı olarak saklandığı için her üyenin sayısı sabittir). Baskın tahtası efektleri ekran
  konumuna göre sağ-sol çalar. Klipler yer tutucudur (`Audio/Placeholder`).
- **Ayarlar** (`SettingsCard`, `AudioPrefs`, PlayerPrefs `corrigible.audio.*`): genel, müzik, ortam, efekt, sessiz mod, dil.
  AudioMixer yok; karışım `AudioDirector`'da ses düzeyi ve `AudioLowPassFilter`'la.

## Kalıcılık ve platform

- `persistentDataPath`: `liberation_savegame.json`, `liberation_desk.json`. PlayerPrefs: `corrigible.language`,
  `corrigible.audio.*`.
- `Platform.Current` (`IPlatformServices`): varsayılan `LocalPlatform` başarımları konsola yazar. `Corrigible.Steam`
  derlenince `SteamPlatform` kendini `RuntimeInitializeOnLoadMethod` ile kaydeder. Steam Cloud için
  `persistentDataPath` Steamworks'te Auto-Cloud'a eklenir (kod gerekmez).

## Editör araçları

- `CorrigibleDebug` (`Editor/CorrigibleDebug.cs`): `Capture(path)` (Overlay canvas'ları geçici olarak kameraya bağlayıp
  PNG alır; edit ve Play modda), `Status()` (harekât özeti).
- Oda araçları (`Desk/Scripts/Editor/Siginak`): `SiginakCapture`/`SiginakCaptureSet` (K1–K7 yakalamaları),
  `SiginakFixtures` (belirli oyun durumları), `SiginakTextScan` (punto taraması), `SiginakFontPreview`,
  `SiginakTextureBaker`, token içe aktarıcı.
- Varlık hattı (`Desk/Scripts/Editor/Import`): sprite içe aktarıcı ve kaydı, atlas, font asset'leri, Linear telafili
  dokular, tema içe aktarıcıları.
