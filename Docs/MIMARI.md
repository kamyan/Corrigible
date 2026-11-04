# Mimari

## Klasörler

```
Corrigible/                          (Unity proje kökü, git deposu)
├─ AGENTS.md, CLAUDE.md              yapay zeka ajanları için giriş
├─ Docs/                             bu belgeler; UI_Siginak (oda tasarımı, ilk tur), UI_Siginak_Rev2 (Revizyon 2: güncel eşleme,
│                                    S1–S9 panoları, görüntü seti), UI_Sade ve UI_C (masa/baskın arayüzü
│                                    kökenleri), SonSalter_Baskin_Paketi (baskın görsel referansı)
├─ Tools/unity-helpers.sh            Unity CLI kısayolları
├─ Tools/Audio/raid_site_audio.py    baskın yerinin yer tutucu seslerini (9 yatak, 22 olay sesi) sentezleyen üretici
└─ Assets/Corrigible/
   ├─ Core/                          saf C# simülasyon (asmdef Corrigible.Core, motor referansı yok)
   ├─ Game/                          GameController, BalanceConfig, platform; Audio/, Localization/, Raid/ (3B baskın tahtası)
   ├─ Desk/                          komuta odası ve ekran kâğıtları
   │  ├─ Scripts/Runtime/            asmdef Corrigible.Desk: Siginak/ (oda bileşenleri, vardiya ve operasyon raporu),
   │  │                              Settings/ (ayarlar dolabı), Entrance/ (menü sahnesi), Raid/ (baskın HUD'u),
   │  │                              Sade/Map/ (kâğıt harita), Components/,
   │  │                              Theme/, Core/
   │  ├─ Scripts/Bridge/             asmdef Corrigible.Desk.Bridge: oyunu sunuma bağlar (Siginak/ bağlayıcılar, Entrance/
   │  │                              menü sahnesi, SimDesk*, baskın veri kaynağı)
   │  ├─ Scripts/Editor/             varlık hattı (Import/), oda araçları (Siginak/), bina kiti araçları (Raid/)
   │  ├─ Scenes/Giris.unity          menü sahnesi: sığınağın kapısının dışı (build listesinde ilk)
   │  ├─ Scenes/DeskGame.unity       oyunun sahnesi (build listesinde ikinci)
   │  ├─ Prefabs/                    Components/ (kâğıt bileşenleri), Siginak/ (SettingsCabinet: ayarlar dolabı),
   │  │                              Sade/Raid/ (baskın HUD, kart, panel, etiket prefab'ları)
   │  ├─ Resources/                  temalar (DeskTheme, SadeTheme, RaidTheme, SiginakTheme), DeskSprites, fontlar
   │  ├─ Data/                       tema kaynakları (tokens_*.json), harita ekip noktaları, stil sayfası
   │  ├─ Art/, Fonts/                sprite'lar, atlaslar, dokular, modeller, malzemeler; font dosyaları ve lisansları
   │  └─ Tests/EditMode/             asmdef Corrigible.Desk.Tests
   ├─ Scenes/Baskin.unity            taktik baskın; DeskGame'in üstüne eklemeli yüklenir
   ├─ Prefabs/Raid/                  3B baskın parçaları (karo, duvar, kanal, dolaplar, olay nesneleri, birimler; Kits/ gedik molozları)
   ├─ Art/Raid/                      baskın malzemeleri, dokuları, mesh'leri; Kits/<bölge>/ bina kitlerinin dokuları ve malzemeleri
   ├─ Audio/Placeholder/             yer tutucu sesler (SoundBank'a bağlı olanlar)
   ├─ Settings/                      BalanceConfig, RaidPieceLibrary, SoundBank; Kits/ (bina kitleri, Kit_<bölge>.asset)
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
 Oda bağlayıcıları (SiginakDesk, SiginakMap, SiginakShelter, SiginakCrt, SiginakShift, SiginakBriefing, SiginakCabinet)
        │ komut                                            ▲ okur
        ▼                                                  │
 LiberationDeskView ── GameController ── komut/Step ──► Simulation (Core) ── WorldState
   (komut kapısı,       (kayıt, olaylar,                    │ olaylar (SimEvent: Raid, RegionLiberated, End)
    teleks gerekçesi)    vardiya)  ◄── Flush ───────────────┘
                                   │
                                   ├─► SimDeskDataSource → SimDeskBuilder → DeskPresenter (son rapor)
                                   ├─► RaidLauncher (Baskin sahnesini yükler/kaldırır)
                                   └─► AudioDirector (yalnız okur)
```

- **`Simulation`** (`Core/Simulation.cs` + partial'lar): bütün kurallar. `NewGame(seed, balance)`, `Step()` (bir vardiya),
  komutlar (`StartLiberationRaid`, `ResolveRaid`, `SelectLiberationRegion`, `ShelterUpgrade`, `ShelterAssign`, `AnswerDoor`),
  denetimler (`LiberationRaidCheck`, `SelectLiberationRegionCheck`, `ShelterUpgradeCheck`, `ShelterAssignCheck`,
  `DoorAnswerCheck`: komut yapılamıyorsa gerekçe metni) ve türetilmiş değerler (`Occupation`, `LiberationFrontier`,
  `NextLiberationSector`, `IncomeBreakdown`, `DoorVisitor`, `DoorOptions`, `DoorOptionEffect`).
  Komutlar `RunCommand` üstünden geçer: `ActResult { Ok, Msg }` döner, fon değişimini `TransactionLedger`'a, komutu kayda
  yazar. Dosyalar: `CampaignLiberation.cs` (bölge/sektör durumu ve baskın), `Simulation.Shelter.cs` + `ShelterLogic.cs`
  (sığınak), `Teams.cs` (ekip), `Economy.cs` (gelir ve işlem defteri), `Door.cs` (sığınağın kapısı: durum, vuruş, kart seçimi,
  `door_answer`) ve `DoorCards.cs` (ziyaretçi tablosu: metinler `Loc.N`, koşullar, seçenekler ve etkileri).
- **`WorldState`** (`Core/WorldState.cs`): oyunun tüm durumu (gün, fon, ekipler, süren baskın, baskın günlüğü, harekât ve
  sığınak, kapı (`DoorState`: bekleyen ziyaretçi, sıradaki vuruş, bayraklar, zincirler, ziyaretler), işgal geçmişi, kırılma
  anları, son). Görünümler doğrudan yazmaz. `S.Raid` varken `Step` çalışmaz (`Blocked`).
- **`Baskin`** (`Core/Baskin.cs`): 8×8 taktik savaş motoru, kendi RNG'si (`BState.Rs`), Unity yok. `Create(RaidParams)` →
  `BState`; `DoMove`, `DoAct`, `Undo`, `Preview`, `EndTurn`, `Retreat`, `Result`, `Intents`. Stratejiye yalnız
  `RaidResult` döner (kayda `raidres` olarak yazılır; taktik hamleler kaydedilmez).
- **Bölge olayları** (`Core/Baskin.Events.cs`, `Baskin` `partial`; kurallar `Docs/BOLGE-OLAYLARI.md`): `RegionDef.Event` →
  `RaidParams.Event` (`Balance.liberationRegionEvents`) → `BState.Ev` (`EvState`), kare bayrakları (`Cell.Cloud/Gas/Crack/Oil/
  Shelf/Phase`), birim `Frozen/Ice/Cab`. `EventSetup` `Create`'te bütün RNG çekimlerinden sonra (RNG çekmez), `EventEnd`
  `EndTurn`'de uyanmadan sonra ve `CheckEscape`'ten önce (son turda yok); tepkili kurallar çağrı noktalarında yalnız olay
  varken. `BState.InEvent` hasar tavanını ve nesne muafiyetini açar. Kabindeki birim `OnBoard` değildir ama baskındadır
  (`CheckEscape`, makine sınırı `Machines`). Sunuma `Baskin.Hazards(B)` (kare işaretleri, turnike kenarları, hatlar, birim
  uyarıları, tur şeridi; kopyada niyetler ve olay adımı oynatılarak), `PreviewMove`, `PreviewResult.Hazards`, `CanStep`
  (turnike yönü; `Reachable`, `MachinePath`, bot) ve `RoomName`. Sunum: `RaidBoard3D.Marks.Hazards` → olay katmanı (renkler
  `RaidPieceLibrary.hazard*`, yeni nesneler `tup`/`kol`/`enkaz`), `RaidViewModel.TurnEvents` → tur şeridi, takım kartının
  `Threat` satırı, hover satırları (`RaidDataSource.HazardRow`), görev fişinin OLAY satırı (`OperationSlip` dördüncü alan).
- **`RaidMaps`** (`Core/RaidMaps.cs`): harekâtın harita havuzu. `Pool(MapSeed)` yeni oyunun tohumundan 30 harita üretir (`PoolSize`): altı türün (`Kinds`: koridor, kasa odası, iki hat,
  açık salon, şebeke, rehine) her birinden beşer, türün kendi oda adlarıyla (`KindOf`). `Generate(kind, rng)` türün yapısını
  kurar (`BuildVault`, `BuildLanes`…), sonra dolapları ve makineleri yerleştirir; `Valid(rows)` ortak kurallar, `Valid(rows,
  kind)` türün adım aralığı (`PathRange`, bot ölçümüyle ayarlı) ve imzası
  (`Generate` + `Valid`: bağlantı, hedefe uzaklık, sayılar, dronların girişe açık atış hattı yok); karıştırma kendi
  üreteciyle, `S.Rng`'den tüketmez. `StartLiberationRaid` `RaidParams.MapSeed = S.Seed`, `MapSlot = S.Raids` (harekâttaki
  baskın sırası) ve `Tactics = 1` (gelişmiş makine aklı) yazar; `Baskin.Create` havuzdan `RaidMaps.Slot` ile (karıştırılmış
  torbalar) seçer. `MapSeed` 0 ise (eğitim baskını, örnek baskın, eski testler) elle çizilmiş `Baskin.Layouts`. Kayıt
  etkilenmez: tahta stratejiye yalnız sonuç verir.
- **Kaçış ve makine aklı** (`Baskin`): `BState.Carrier` (kasayı açan Teknisyen), `Exits` (giriş kareleri, her görevde) ve
  `Escape` (görev hedefi tamam: kasa açıldı ya da hedef dolaplar kırıldı). `CheckEscape` yürüyüş, yetenek ve makine
  evresinden sonra kaçışta çıkıştaki uzmanı tahliye eder (`Unit.Out`), tahtada uzman kalmadıysa `FinishEscape` sonucu yazar;
  süre dolarken kalanlar `Unit.Left` (düşmüş, `RaidResult.Left`). Tahtadaki birim `Baskin.OnBoard` (düşmemiş, çıkmamış):
  `UnitAt`, `Alive`, `CanMove/CanAct` onu kullanır; tahliye edilen kadro sırası (`Result.Hp`) için `Units`'te kalır. `BState.Smart` (`RaidParams.Tactics > 0`) iken
  `PlanEnemies` `KeyTiles` (hedefin önü ya da kaçış yolu), `Zone` (niyetin tuttuğu kilit kareler) ve `Covers` (eşgüdüm)
  ile puanlar; RNG tüketimi temel akılla aynı. `RaidParams.TargetHpBonus/ExtraMachines` sunucu görevinin sertliği
  (kampanya dengeden yazar; düğüm dolabına ek can, çekirdek hariç; baştaki ek makine). Sunum: `RaidBoard3D.Marks.Exits/Carrier` (yeşil kesik çıkış çerçevesi,
  "YÜKLÜ" etiketi), `RaidBoard3D.AnimateLeave` ve "evac" efekti (tahliye), `RaidViewModel.Escaping/CargoLost/CanRetreat`
  (amaç satırı, ALINDI/KAYBOLDU damgası, soluk GERİ ÇEKİL), `RaidTeamModel.Out/Left`, `PreviewResult.Escape`; tur şeridi 8
  kutuya kadar, plaka tur sayısıyla kayar.
- **`GameController`** (`Game/GameController.cs`): komut kapısı. `NewGame`, komut metotları, `AdvanceShift` (vardiya),
  `Flush` (olayları `EventsRaised` ile dağıtır; harekât bitince başarımı açar, kaydı siler), `Save`/`ContinueSaved` (dosya
  `SaveStore`'da; menü sahnesi kartın özetini aynı yerden okur).
  `StoryHold` açılış ve emir belgesi açıkken vardiyayı engeller. Olaylar: `Changed`, `EventsRaised`, `NewGameStarted`,
  `GameLoaded`. Zaman kendiliğinden akmaz.
- **`LiberationDeskView`** (Bridge): odanın komut kapısı (`IDeskSurface`). Komutları GameController'a iletir, sonucu
  gerekçesiyle teleksten bastırır (`TelexReasons`), girdi kilidini (`InputBlocked`: açık ekran kâğıdı, hikâye, baskına
  geçiş) tek yerde hesaplar.

## Determinizm ve kayıt

- RNG: `Rng` (mulberry32, `Core/Rng.cs`). Durumu `S.Rng.State`. Kapı kendi üretecini her kullanımda harekât tohumu, gün ve
  amaçtan kurar (`Simulation.DoorRng`); ana sıraya dokunmaz (`DoorTests`). `Js.Round` = JS `Math.round` (`Math.Floor(v + 0.5)`);
  tüm sayılar `double`. Aynı tohum + aynı komutlar = aynı oyun (`LiberationCampaignTests`).
- `GameRecord` (`Core/GameRecord.cs`): `version` (12), `seed`, `balance`, `commands[]`, `day`. `SavedCommand { day, kind, a, b }`;
  türler `liberation_region`, `liberation_raid` (a: sektör, b: Teknisyen'in yanına seçilen roller kadro sırasıyla, çözülmüş
  hâliyle; boş b yeniden oynatmada o anki varsayılan seçime döner; defter kimliği yine sektördür: `RunCommand`'ın `ledger`
  argümanı), `raidres` (`RaidResult.Encode`: başarı, geri çekilme, kırılan sivil, düşen,
  tur, kadro canı, zırh, ayakta kalan sivil), `shelter_upgrade`, `shelter_assign`, `door_answer` (a: seçenek, b: kart).
  Ziyaretçi bir vardiyanın sonunda (`Step`) gelir; cevabı o günün komutudur. Cevapsız ziyaretçi bir sonraki `Step`'te
  açılmadı sayılır (komut değildir, yeniden oynatmada da aynı olur).
- `Replay.Restore(rec)` yeni oyunu kurar, her komutun gününe kadar `Step` eder ve komutu uygular, sonra kayıt gününe
  kadar ilerler. Bu sürümden eski kayıtlar (kriz oyunu dönemi) yüklenmez.
- `GameController.Save()` → `SaveStore.Write` → `persistentDataPath/liberation_savegame.json` (JsonUtility); her başarılı komutta, vardiya
  sonunda ve uygulama kapanırken. Kayda girmeyen oda durumu (teleks, evrak tepsisi, kamera pozu) `SiginakDeskStore` ile
  `liberation_desk.json`'a yazılır; tohum eşleşmezse yok sayılır.
- **Yeni bir oyuncu komutu eklerken:** `Simulation`'a `XCore` + `public ActResult X(...) => RunCommand(() => XCore(...), "kind", ...)`,
  `Replay.Restore`'a `case "kind"`, GameController'a ince bir metot, `LiberationDeskView`'a gerekçeli komut, test.

## DeskGame sahnesi (`Desk/Scenes/DeskGame.unity`)

```
EventSystem              [InputSystemUIInputModule]
DeskUI                   Overlay; Modal (sıra 100): FinalReport (harekât sonu raporu)
DeskPresenter (oyun)     DeskPresenter, DeskInput, SimDeskDataSource, SimDeskCommands, BridgeSettings, LiberationDeskView
Game                     GameController (BalanceConfig), RaidLauncher
Audio                    AudioDirector (SoundBank)
CommandPost3D            CommandPostDesk (kamera girdisi kapısı); Main Camera + SiginakCameraRig
SiginakRaidLink          baskın sahnesi yüklüyken odayı gizler, CRT geçişini RaidLauncher'a verir
SiginakRoom              oda: masa (sayaçlar, anahtar rayı, şalter, teleks, evrak tepsisi, Desk/MapMount altında kâğıt
                         harita SadeMapCanvas), raf (sığınak maketi, oda kartları), CRT, duvar; sağ duvarda Bunker door
                         (DoorProp, DeskTarget; duvarın mesh'i kapı boşluklu, wall_doorway; ziyaretçi çentiği) ve ardında
                         Corridor (merdiven boşluğu, iki lamba, Visitors: VisitorSilhouette); Poses/K8 (kapı pozu);
                         arka duvarın sağında, CRT'nin üstünde Settings cabinet (SettingsCabinet prefab'ı; K9 çapası içinde) ve Cabinet controller
                         (SiginakCabinet);
                         SiginakHud (Overlay 45: karartma, kenar satırı, görev fişi, ATLA plakası, vardiya raporu,
                         operasyon raporu, emir belgesi, kapı kâğıdı, teleks şeridi, üzerine gelme etiketi)
SiginakCrtOverlay        Overlay 60, CRT'ye dalış geçişi
```

- Oda, yerleşim ve bileşen bağlantıları sahnede serileştirilir; kalıcı kurucu yoktur (son kurucu `eed2824`'te). Tasarım
  eşlemesi ve değişiklik günlüğü: `Docs/UI_Siginak_Rev2/IMPLEMENTATION_MAP.md` (Revizyon 2, güncel); ilk tur ve poz/kamera
  ayrıntısı `Docs/UI_Siginak/IMPLEMENTATION_MAP.md`.
- **Ayarlar dolabı** (`Docs/AYARLAR-DOLABI-VE-MENU.md`; Runtime/Settings, prefab `Desk/Prefabs/Siginak/SettingsCabinet`):
  arka duvarın sağında, CRT masasının üstünde (K1'den görünür) çelik hat dolabı. `SettingsCabinet` kapağı (mandal 0,15 s, kanat 0,45 s), iç ampulü, kontrolleri ve
  `IDeskSettings` bağını yönetir; `CabinetFader` (kollu dikey sürgü: işaretçinin ışını panel düzlemine düşürülür, %5 adım,
  %10'da çentik, sayaç tekerleri, yaylı ibre, lamba; tekerlek ±%5, çift tık varsayılan), `CabinetToggle` (yarasa kollu
  sessiz mod anahtarı), `CabinetSelector` (dil kademe anahtarı; TÜRKÇE / ENGLISH ve “DİL · LANGUAGE” çevrilmez), kapaklı
  kol MENÜYE DÖN (`ShiftSwitch`, vardiya şalterinin bileşeni). Kontroller `DeskTarget`'tır (üzerine gelme etiketi, çeviri
  aracı başlıkları toplar) ama odanın kilidine değil dolabın açık olmasına bakar (`DeskTarget.BlockedOverride`); etiketin
  satırı değeri gösterir (`HoverTag` değişen satırı yeniler). Dolap Desk'te olduğu için ses çalmaz: `Sound` olayını
  bağlayıcı `Sfx`'e çevirir (`Cue.CabinetOpen` 89 … `SelectorClunk` 93). `SettingsCabinet.Instance.IsOpen` odanın beş
  kilidinde okunur (`LiberationDeskView.InputBlocked`, `CommandPostDesk`, eğitim, emir belgesi, `DeskInput`). Oda tarafı
  `SiginakCabinet`: dolaba tıklama (K1'den görünür) ve Esc `RequestOpen` → kamera K9'a (`SiginakPose.Cabinet`,
  dikey 45°), kapak kamera varınca açılır; kapanınca önceki poz. Emir belgesi açıkken belge gizlenir
  (`BriefingView.SetVeiled`); dolapta gün 0'da dil değişirse oyun dolap kapanınca yeni dilde kurulur
  (`BridgeSettings.RebuildIfPending`). MENÜYE DÖN: kayıt, `OpeningSequence.FadeOut`, `Giris` sahnesi (build'de yoksa çıkış).
- **Bağlayıcılar** (Bridge/Siginak): `SiginakDesk` (sayaçlar, anahtarlar, şalter), `SiginakMap` (kâğıt harita, görev ve
  cephe fişleri, ekip figürü), `SiginakShelter` (maket ve oda kartları), `SiginakCrt` (telsiz hattı, `IRaidTransition`),
  `SiginakShift` (vardiya dizisi, vardiya raporu, evrak tepsisi, karşı saldırının alarm lambası ve harita ✕'i; raporu saf C#
  `ShiftReportBuilder` kurar, renk kuralı `ReportTones`), `SiginakBriefing` (açılış ve emir belgesi),
  `SiginakDoor` (kapı: vuruş, kamera, kâğıdın evreleri, `LiberationDeskView.AnswerDoor`; kâğıdı saf C# `DoorViewBuilder`
  kurar (kart → siluet tablosu da orada), `DoorCardView` çizer, kapıyı `DoorProp`, ziyaretçiyi `VisitorSilhouette`
  canlandırır; ölümcül kararda oda ışığını kırmızıya çevirir ve son raporu `DeskBridgeState.HoldEnding` ile kâğıt
  kapanana kadar bekletir),
  `SiginakTutorial` (eğitim: sahne kökünde kendi tuvali `SiginakTutorial`, sıralama 70, baskın HUD'unun üstünde; odanın
  dışında olduğu için baskında da çalışır; adımlar saf C# `TutorialFlow`, not `TutorialNoteView`; senaryolu ilk baskın
  `TutorialRaid` ve rehberi `RaidGuide`: `LiveRaidDataSource` eğitim baskın adımındaysa tahtayı senaryonun tohumuyla kurar,
  `RaidDataSource` girdiyi rehberin sıradaki adımıyla süzer, `RaidBoard3D.Marks.Guide` sarı çerçeveyi çizer). Modelleri saf
  C# kurucular üretir (`LiberationMapBuilder`, `ShelterViewBuilder`, `CrtViewBuilder`, `ShiftReportBuilder`,
  `BriefingBuilder`); odanın bileşenleri (Runtime/Siginak) yalnız modeli çizer.
- `CommandPostDesk` masa kamerasının girdi kapısıdır: açık bir ekran kâğıdı ya da sunumun kilidi (`ExternalBlocked`)
  varken kamera girdisi kapalıdır. Kamerayı `SiginakCameraRig` sürer (K1 masa, K2/K3 harita, K4 raf, K5 CRT).
- **Ekran kâğıtları:** `SimDeskDataSource` GameController olaylarıyla kirlenir ve karede en çok bir kez
  `SimDeskBuilder.Build(sim, state)` çağırır: harekât bittiyse (kapının kâğıdı kapandıysa) son rapor
  (`FinalReportBuilder` → `FinalReportData`). `DeskPresenter` modeli `FinalReportView`'a dağıtır, YENİDEN BAŞLA'yı
  (`"Restart"`) `SimDeskCommands`'a iletir; `SiginakFinal` rapor açılınca kamerayı harita pozuna alır ve damganın sesini
  çalar. Rapor (Revizyon 2 · S8): işgal şeridi `OccupationChart` (`Graphic.OnPopulateMesh`, x = vardiya 1…N tam genişlik),
  kırılma anları sunumda türetilir (baskın kaydı, `S.Moments`, kapı kaydı). Ayarlar dolabı `IDeskSettings`
  (`BridgeSettings`: `AudioPrefs`, dil) ile çalışır; `DeskPresenter.settingsCabinet` bağlar.
- **Operasyon raporu** (Revizyon 2 · S7, `SiginakOperation` + `OperationReportBuilder` + `OperationReportView`,
  `SiginakHud › Operation report`): okunmamış baskın günlüğü (`LiberationDeskView.RaidReportPending`) baskın sahnesi
  kapanınca açılır: kamera harita pozuna, kâğıt iner, iplik iğneden ekip figürüne (`SiginakMap.SetReportString`), halka
  figürün çevresine. Sayaçlar o ana kadar bekler (`SiginakOperation.HoldCounters`), sonra döner; başarılıda ± fişleri ve
  sektör işaretinin mavi onayı (`LiberationRegionMarks.AnimateRetaken`). Enter/Esc/düğme tepsiye dosyalar ve
  `LiberationDeskView.MarkRaidReportSeen` raporu okundu sayar. Açıkken masa kilitli (`InputBlocked`).
- `DeskInput` klavyenin tek sahibidir: ayarlar dolabı açıkken Esc (dolabın kendi Tab/okları ayrıca); son raporda Enter; aksi hâlde 3 planlama, Esc
  ayarlar/geri. Odanın anahtarları (`StationKeyRail`: SIĞINAK, GÖREVLER, KAPI, MASA; üst kenar şeridi `EdgeLine` aynı sıra;
  AYARLAR çipi yok: ayarlar odadaki dolapta, sağ üstteki plaka yalnız dizilerde ATLA), vardiya raporu ve operasyon raporu kendi tuşlarını dinler. DeskGame klavye tuşu **yazmaz**: tuş
  fişi, “Enter/Esc/Home” ipucu ve üzerine gelme etiketinde kısayol yok (`IHoverSource` yalnız başlık ve satır); her
  kısayolun tıklanan bir karşılığı vardır (kapı kâğıdının alt satırı `DoorCardView.Dismissed`). Tuş başlıklarında ve
  şeridin tuş kutularında istasyon simgeleri (`tx_key_*`, `SiginakTextureBaker.KeyIcons`; başlıkta aydınlatılan saydam
  dörtgen `Key icon *.mat`, şeritte RawImage). Baskın tahtası (Baskin) kendi kısayol ipuçlarını gösterir.

## Menü sahnesi (`Desk/Scenes/Giris.unity`)

Sığınak 07'nin kapısının önündeki sahanlık (Docs/AYARLAR-DOLABI-VE-MENU.md Katman 6–9). Build listesinde ilk; oyun buradan
açılır. Sahne tek seferlik bir betikle kuruldu (kalıcı kurucu yok); dünya koordinatları DeskGame'inkilerle aynı (kapı x 2,2).

```
EventSystem          [InputSystemUIInputModule]
Entrance             Landing (koridorun malzemeleriyle genişletilmiş sahanlık, merdiven ve yukarı kat, üç lamba),
                     Bunker door (DeskGame'deki kapının kopyası: dış yüzde çark, “07”, SIĞINAK 07, gözetleme halkası;
                     DoorProp dıştaki çarkı döndürür, içeriden sızan ışığı yakar), Inside (karanlık iç, sıcak ışık),
                     Intercom outside, Title root (“SON ŞALTER” / “CORRIGIBLE”), Settings cabinet (prefab; MENÜYE DÖN kolu
                     kapalı), Time clock (TimeClock: puantaj saati ve kart rafı), Main switch (ShiftSwitch: son şalter),
                     Poses (M0 sahanlık, M2 saat, M3 eşik ve içerisi), Main Camera (EntranceCamera, PhysicsRaycaster)
EntranceUI           Overlay 45: HoverTag (DeskGame'deki etiketin kopyası), Fade (Overlay 100, siyah örtü)
Audio                AudioDirector (game boş)
Entrance controller  EntranceController, BridgeSettings (veri kaynağı yok)
```

- `EntranceController` (Bridge/Entrance): karanlıktan açılış (lambalar iki kırpışmayla), kayıt özeti
  (`SaveStore.Load` → `BriefingBuilder.Summarize`, arka iş parçacığında; Core saf C#) karta yazılır. Karta tıklamak
  kayıttan devam, boş deste yeni harekât (kayıt varsa kıskaç iki adım: `TimeClock.Armed`, 4 s). Seçilen kart saate basılır
  (`TimeClock.Punch` → `Stamped`), kapının zili, kapı açılır, kamera eşikten geçer (`EntranceCamera.Walk`), ekran kararır;
  DeskGame `LoadSceneAsync` ile kart basılırken yüklenmeye başlar ve karanlıkta etkinleşir. Niyet `GameLaunch.Pending`
  (Game, statik). Son şalter (`ShiftSwitch`: kapak + kol) lambaları söndürür, `Application.Quit` (editörde Play durur).
  Esc dolabı kapatır, kıskacı bırakır ya da saatten sahanlığa döner; oyundan çıkarmaz. `DeskTarget.Blocked` menüde
  geçişler ve dolap için kurulur.
- **DeskGame açılışı:** `LiberationDeskView.Start` `GameLaunch.Take()`'i okur. Devam: `ContinueSaved` →
  `SiginakBriefing.OpenReturn` (siyahtan ampul 0,6 s, `OpeningSequence.Play(..., shortReturn: true)`, kamera K1, teleks
  KAYITTAN DEVAM · V.nnn); açılamazsa teleks ve yeni oyun. Yeni: açılış + emir belgesi dosyasız (`BriefingData.NoDossier`,
  BAŞLA tek basış). Niyet yoksa (DeskGame doğrudan açıldı: editör, testler, görüntü seti) bugünkü açılış.

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

- `RaidLauncher` (DeskGame'de, Game nesnesi): `S.Raid` oluşunca önce kamerayı CRT'ye daldırır, sonra sahneyi
  `LoadSceneMode.Additive` ile yükler; odanın kamera, dinleyici, canvas ve girdilerini kapatır (`hideDuringRaid`: nesneler
  değil bileşenler). Sahne kapanınca önceki `enabled` durumlarını geri yükler. Yolu `IRaidTransition` kancası sürer
  (`SiginakCrt.Approach` → `SiginakCameraRig.BeginDive`): kamera geri çekilme pozuna (`Poses/CrtBack`) doğru çıkar, CRT'ye
  döner ve `CrtDive`'a yaklaşır; tek kesintisiz hareket (`motion.crtDiveS`), vuruşlar yolun kesirleri olarak üst üste biner
  (`diveBackSpan/diveTurnSpan/divePushSpan`, smootherstep ağırlıklar; yol ortasında durma yok). Dönüşte
  kamera monitörde kalmıştır: `Depart` → `BeginReturn` aynı yolu tersinden yürür (`crtReturnDelayS` bekleyip), bitene kadar
  `IsTransitioning` doğrudur (masa kilitli, operasyon raporu kamera varınca iner). Boşluk ya da tık dalışı ve CRT dizisini,
  dönüşte de kamera yolunu atlar. Kanca yoksa eski düz yaklaşma ve anında dönüş.
- `LiveRaidDataSource`, DeskGame'deki `GameController` ve `S.Raid.Params` ile `Baskin.Create`'i bir kez çağırır. Oyuncu
  sonuçtan rapora geçtiğinde `GameController.ResolveRaid` çalışır ve sahne kapanır. Baskın boyunca odanın `DeskInput`'u
  askıya alınır. Sahne tek başına açılırsa örnek bir sunucu baskını kurulur.
- `RaidDataSource` taktik komut/sunum sınırıdır: kurallar, hedefler ve önizleme Core'dan; görünür niyetler
  `Baskin.Intents` üzerinden gelir. UI kural, hasar ya da RNG hesaplamaz. `RaidHudView` klavyenin tek giriş noktasıdır
  (1–3 birim, Q yetenek, Z geri al, Enter tur bitir, Esc). Ekip kartları ve 1–3 sırası baskının kadrosundan
  (`RaidParams.Roles`, yoksa `Squad`): Teknisyen ve seçilen iki uzman; yetenek satırı yeteneğin kimliğinden (`AbilityName`,
  `AbilityRange`; Madenci ve Sis uzmanında kalan hak).
- `RaidBoard3D`: kare (x, y) → dünya `(x − 3,5, 0, 3,5 − y)`. Bir eylem ya da tur bitince motor durumu anında değişir;
  tahta efektleri (`FxList`) sırayla oynatır, sonra `Sync` ile eşitlenir. Oynatma sırasında girdi kapalıdır (`Busy`).
  Makine evresinden sonra Core robotları yeni plana doğrudan taşır (efekt yok); tahta onları `AnimateMachineMoves` ile
  yeni sıralarıyla tek tek, kare kare yürütür (`MachinePath`: Core.Reachable'ın kuralıyla en kısa yol), uygulanan
  niyetlerin ok ve bayraklarını önce kaldırır, can etiketini robotla taşır. Süreler `RaidBoard3D`'nin alanlarında
  (`machineStepSeconds` 0,16 · `machinePauseSeconds` 0,08 · üst sınır `machineMovesMaxSeconds` 3); hızlı kipte ÷3,125.
  Can/sıra etiketleri `RaidScreenLabel` prefab'ıyla ayrı Overlay canvas'ta çizilir: robotta tür işareti
  (`RaidPieceLibrary.Glyph`) → zırh → pipler, uzmanda altlığın önünde; sıra bayrağı 30 × 28; uyanmadan sonraki tur UYANDI
  (bayrakla bir grup). Etiketler `LayoutLabels`'ta gruplar hâlinde çakışmadan yerleşir. Karolar durumdan kurulur:
  `Sync` her karonun türünü (`Cell.T`) karşılaştırır, Madenci'nin yıktığı duvar zemine döner, geri almada duvar geri gelir
  (`BuildTile`); yıkık duvarın yerinde alçak moloz (`BState.Rubble`). Duvar karesindeki hedef çerçevesi bloğun üstünde
  (`WallTop`). Sis (`Cell.Smoke`, yalnız bu turun makine evresinde) kare başına dört soluk küre ve SİS etiketiyle
  (`Marks.Smoke`, önizlemede `SmokePreview`); sise kesilen niyet gri kesik çizgi (`Marks.SmokedArrows`). Niyetler `DrawIntent`'te türüne
  göre (kalınlık, yay, kesik, çift şerit, rozet); oklar `Pencil.mat`'ta ZTest Always; karo işaretleri (tarama, seçim, hedef, uyanma halkası) `Pencil marks.mat`'ta derinlik testli, parçalar örter (`RaidPieceLibrary.tileMarks`).
- **Bina kiti ve karo giydirme** (`Docs/BASKIN-MEKANLARI.md` Katman 3–5): `RaidRoomKit` (ScriptableObject,
  `Settings/Kits/Kit_<bölge>.asset`) duvarın, sütunun, kanal ve kuyu zemininin malzemelerini, gedik molozunu ve köprü
  bayrağını tutar. `RaidPieceLibrary.KitFor(bölge, MapSeed)` seçer (MapSeed 0 → `defaultKit`: eğitim, örnek baskın, görüntü
  seti); `RaidDataSource.OpenState` `Build`'den önce `RaidBoard3D.SetDress` çağırır (`kitOverride` yalnız editörde). Karo
  hiç yeni nesne kurmaz: `RaidTileMask` komşulardan maskeyi (K=1 y−1, D=2, G=4, B=8), yalıtılmışlığı ve rafın ray yönünü
  çıkarır, `RaidTileSkin.Apply` yalnız çocukları açıp kapar ve `sharedMaterials` atar. Duvar (`Wall.prefab`): Body (tek
  `WallBlock` mesh, 3 alt mesh), komşu duvara BridgeE/N köprüleri (varsayılan kitte kapalı), kayar rafta ShelfGear (el
  çarkı ve teker; kare başına ≤3 renderer). Kanal (`Cable.prefab`): Hub (+Junction), dört Edge ve dört Arm, tek karede
  Single. `Build` bütün kareleri, `Sync` türü değişen karenin 3×3 komşuluğunu giydirir; raf ve dinamit canlandırmasında
  köprüler gizlenir (`HideBridges`), bitince yeniden giydirilir. Gedikte `RaidPieceLibrary.BreachRubble(kit)` (kitin molozu;
  yoksa eski dolap enkazı 0,9/0,3/0,9). Olay nesneleri kendi prefab'larıyla (`Tube`, `WeldArmBase`, `CeilingDebris`;
  `lib.tube/weldArm/debris`). Kitin adları `RaidKitText`'te (Bridge; inceleme kartının gri alt satırı ve KURALLAR lejantı).
- **Baskın HUD'u** (`RaidHud.prefab`, `RaidHudView`): köşe panelleri (Status, Buttons, Command, Ability card, Team)
  prefab'da; ekip kartları (`RaidTeamCardView`) ve yetenek kartı (`UnitCardView`) aynı prefab'ın içinde, ayrı prefab yok.
  Renkler `RaidHudView.C(key)` ile Baskın → Masa → Sığınak temasından. Telsiz fişi `RaidPanels › EventSlip`. Kadro raporu baskında değil, baskın emrinden önce masada (`SiginakHud › Squad report`, `SquadReportSlip`; `Simulation.LiberationSquadPreview` → `Baskin.SquadReport` → `LiberationMapBuilder.SquadReport`).
- **Saha masası** (yalnız sunum): `Build` paftanın kameraya bakan kenarlarına kare adlarını yazar (güneyde A–H, batıda 1–8;
  1 kameraya en yakın); `RaidBoard3D.CellName(x, y)` telsiz satırlarında kullanılır ("TEKNİSYEN · A6"). Niyet okları
  `RaidArrow.Set(..., hand)` ile yağlı kalem çizgisi (dalga uçların konumundan, eşitlemede titremez; uç açık V).

## UI yaklaşımı ve tema

- **uGUI + TextMeshPro**; oda 3B, yazılar dünya ya da Overlay canvas'larda. Prefab ve sahne asıl kaynaktır.
- **Temalar** (ScriptableObject, `Desk/Resources`): `SiginakTheme` (oda), `SadeTheme` (harita, kâğıt), `RaidTheme`
  (baskın), `DeskTheme` (fontlar, ortak kâğıt renkleri). Token kaynakları `Desk/Data/*/tokens_*.json`,
  `Desk/Data/Sade/raid_tokens.json` ve `Docs/UI_C/spec/tokens.json`; içe aktarıcılar Corrigible menüsünde. Bileşenler renkleri anahtarla alır (`ThemeTint`).
- **Kâğıt bileşenleri** (`Desk/Prefabs/Components`): PaperPanel, DeskButton (+ kapatma varyantı), KeyChip,
  RadioCheck. Özel Graphic'ler: `UILineGraphic`, `UIPolygonGraphic`, `SpotlightGraphic`,
  `RegionMeshGraphic` (harita bölgeleri: sahnede serileştirilmiş çokgenler ve tıklama testi). Çokgenler tasarım
  dikdörtgeninden (1920 × 1080) taşar; `SadeMapCanvas`'taki `RectMask2D` haritayı kâğıdın kenarında keser (testli).
- **Biçim:** `DeskFormat` (sayı, yüzde, vardiya kodu), `DeskText` (tr-TR büyük harf), `TrNumber` (Türkçe ekler),
  `DeskMath`.
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
  yoksa sistem diline göre. Masa ve menü (`DeskLoc`) ayarlar dolabının kademe anahtarından dili sahneyi yeniden yüklemeden
  değiştirir; `DeskLoc` statik olduğu için menüde seçilen dil DeskGame'e taşınır.
- **Araç:** `Editor/LocalizationTool.cs` (menü **Corrigible > Lokalizasyon**): kaynakları koddan (`Loc.T/F/N("...")`) ve
  prefab/sahnelerdeki `DeskLocText`'lerden ve odadaki `DeskTarget`'ların üzerine gelme başlığı ve satırından toplar, tabloyu günceller (yeni satırlar boş çeviriyle; kullanılmayanlar önce
  dosya sonunda yorum olarak durur, sonraki güncellemede düşer),
  doğrular (eksik çeviri, `{n}` yer tutucu ve `<b>` gibi etiket uyumu).
- **Testler** (`LocalizationTests`): tablo tam mı; rastgele oynanan harekâtlar ve baskınlar İngilizce oynanınca hiç
  eksik çeviri ya da Türkçe metin çıkmıyor mu; dil oyunun sonucunu değiştiriyor mu.

## Ses (`Game/Audio`)

- **`AudioDirector`** (DeskGame'de `Audio` nesnesi): simülasyonu yalnızca okur; rastgelelik `UnityEngine.Random`.
  Müzik katmanları: drone her zaman, warm işgal oranıyla, grit dünyanın uğultusu; hepsi alçak geçiren filtreyle
  "duvarın ardından". Bölge özgürleşince alarm, harekât bitince kapanış; baskında müzik kısılır, telsiz hışırtısı
  (`LoopCue.RadioHiss`) ve binanın sesi girer.
- **Baskın yeri** (`Docs/BASKIN-MEKANLARI.md` Katman 7–8): `RaidDataSource.OpenState` → `Sfx.RaidSite(bölge, ayrıntılar)` →
  `AudioDirector.SetRaidSite`. Bölgenin ortam yatağı `SoundBank.sites` (`RaidSite`: bölge, yatak, `bedVolume`, ayrıntılar,
  `detailVolume`, `detailGap`) iki kaynakta (Bed A/B) çalar: yeni yatak sessiz olanda döngünün rastgele bir yerinden başlar,
  öbürü söner; aynı yatak çalıyorsa baştan başlamaz. Yatağı olan bölgede genel bina uğultusu (`LoopCue.RaidAmbience`) susar,
  yatağı yoksa yedek olarak çalar. Seyrek ayrıntılar (Bed Detail) son 0,4 sn'de bir saha sesi ya da gözcü bildirimi
  başladıysa atlanır; eğitim baskınında ayrıntı yok. Baskın kapanınca yer unutulur. Yeni `LoopCue` eklenmez (her değeri hep
  çalan bir kaynak olur).
- **Telsiz:** saha görülmez, telsizden duyulur. Baskın sahnesi açıkken saha sesleri (`Cue.RaidStep` … `RaidSpawn`) ve bina
  uğultusu ses kaynağındaki yüksek ve alçak geçiren filtreyle telsiz bandından çalar (`SoundBank.fieldThroughRadio`,
  `radioLowCut` 450 Hz, `radioHighCut` 3000 Hz); arayüz sesleri düz kalır. `RadioTune` 53 (hat açılırken, `SiginakCrt`),
  `RadioSend` 54 (EMRİ İLET, `RaidDataSource.Machines`), `RadioReport` 55 (gözcü her makine hamlesini ve uyanmayı
  bildirirken, hat kapanırken).
- **Tek seferlik sesler:** `Sfx.Play(Cue.X, volume, pan, pitch)` (kapı: `DoorKnock` 50, `DoorOpen` 51, `DoorShot` 52;
  yer tutucular `Audio/Placeholder/CueDoor*.wav` ve `CueRadio*.wav`, `LoopRadioHiss.wav` kodla sentezlendi); klipler `Settings/SoundBank.asset` → `cues`
  (`Cue` enum'ı asset'te sayı olarak saklandığı için her üyenin sayısı sabittir). Baskın tahtası efektleri ekran
  konumuna göre sağ-sol çalar. Bölge olaylarının sesleri `Cue.EvCo2Burst` 67 … `EvOilSlide` 88 (`RaidBoard3D.EventFx`);
  `FromField` 31–49 ve 67–88'i telsiz bandından geçirir. Klipler yer tutucudur (`Audio/Placeholder`; yataklar ve olay sesleri
  `Tools/Audio/raid_site_audio.py` ile sentezlendi).
- **Ayarlar** (ayarlar dolabı, `AudioPrefs`, PlayerPrefs `corrigible.audio.*`): genel, müzik, ortam, efekt, sessiz mod, dil.
- **Menü sahnesi:** aynı `AudioDirector` `game` boşken yalnız drone ve dünyanın uğultusunu çalar; dolabın, saatin ve son
  şalterin sesleri `Cue` 89–96 (yer tutucular `Tools/Audio/cabinet_audio.py` ile sentezlendi).
  AudioMixer yok; karışım `AudioDirector`'da ses düzeyi ve `AudioLowPassFilter`'la.

## Kalıcılık ve platform

- `persistentDataPath` (`SaveStore`): `liberation_savegame.json`, `liberation_desk.json`. PlayerPrefs: `corrigible.language`,
  `corrigible.audio.*`.
- `Platform.Current` (`IPlatformServices`): varsayılan `LocalPlatform` başarımları konsola yazar. `Corrigible.Steam`
  derlenince `SteamPlatform` kendini `RuntimeInitializeOnLoadMethod` ile kaydeder. Steam Cloud için
  `persistentDataPath` Steamworks'te Auto-Cloud'a eklenir (kod gerekmez).

## Editör araçları

- `CorrigibleDebug` (`Editor/CorrigibleDebug.cs`): `Capture(path)` (Overlay canvas'ları geçici olarak kameraya bağlayıp
  PNG alır; edit ve Play modda), `Status()` (harekât özeti).
- Oda araçları (`Desk/Scripts/Editor/Siginak`): `SiginakCaptureSet` (Revizyon 2 tam görüntü seti: DeskGame'de S1a…S8b,
  Baskin tek başına S6a–c; TR, `_EN`, `_gray` → `Docs/UI_Siginak_Rev2/screens_unity_rev2/`), `SiginakCapture`
  (`Render(poz)`: Overlay'siz 3B görüntü, `SiginakPose` adıyla), `SiginakFixtures` (V014, V031, K6, V019, injured, V015ca),
  `SiginakTextScan` (punto taraması: oda, CRT, eğitim notu, DeskUI ve Baskin sahnesi; rapor
  `Docs/UI_Siginak_Rev2/punto_taramasi.txt`), `SiginakFontPreview`, `SiginakTextureBaker`, token içe aktarıcı.
- Varlık hattı (`Desk/Scripts/Editor/Import`): sprite içe aktarıcı ve kaydı, atlas, font asset'leri, tema içe
  aktarıcıları.
- Bina kiti araçları (`Desk/Scripts/Editor/Raid`): `RaidKitTextureBaker` (Corrigible › Baskın › Kit dokularını üret: 12
  kitin dokuları, malzemeleri ve `Kit_<bölge>.asset`'leri; motifler `RaidKitTextureBaker.Motifs*.cs`'te), `RaidKitGate`
  (okunabilirlik kapısı; baker kapıdan geçmeyen kiti kütüphaneden çıkarır, `RaidRoomKitTests` de aynı kapıyı kullanır),
  `RaidKitPreview` (Corrigible › Baskın › Kit önizleme sayfası: gerçek prefab ve giydirme koduyla 12 kit, gri tonlu kopya).
  Baskın parçalarının küçük resimleri `SiginakTextureBaker.PieceThumbs` (Corrigible › Sığınak › Baskın parçalarının küçük
  resimlerini üret).
