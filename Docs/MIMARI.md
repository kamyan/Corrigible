# Mimari

## Tesis baskını hangi katmandan geçer?

| Sorumluluk | Güncel kaynak |
|---|---|
| Emir, pafta ve yeniden oynatma | `Simulation.Facility` emri açar, `FacilityMaps` ve `FacilityRoom` deterministik kurulumu verir, `RaidChain` oda raporlarını uygular. `AdvanceRaidRoom` her oynanan odanın raporunu kaydeder. `Replay.Restore` komutları yeniden yürütür. Dosya sürümü 21, üretilmiş harita g5, tesis izi saha kapalı f8 / açık f9. |
| Tahta | `Baskin.Actions/Gear/Counters/Conditions/Signatures/Infiltrators` hamle, araç, önlem, şart, niyet ve saha dönüşünü çözer. `FacilityMarks` kanat izlerini, `FacilityTitles` kişilerin iş ve unvanını tutar. |
| Sığınak ve savunma | `FacilityGear` tarif ve takım kazanımlarını, `FacilityCounters` öğrenme ve cevapları tanımlar. `Simulation.Siege` kuşatma kararı, `Baskin.Defense` dalga ve röle içindir. |
| Görünüm | Görev fişi ve bina planı emir öncesini, `LiveRaidDataSource` ve `RaidDataSource` aktif paftayı tahtaya aktarır. `SiginakTutorial` mevcut adımı `PlanningTutorialFlow.BodyText` ile çevirir. Notun açılması komut üretmez. |
| Ölçüm | `Camp8` gerçek Core'u, `CampaignBot` ve `RaidBot` sığınak ve tahtayı çalıştırır. `tesis17.txt` koşulları, `ozet.py --tesis/--esli` görev paydaları ve eşli farkları tanımlar. R saldırı, S savunma, C harekât sonudur. |

Tesis baskını oyunun tek baskın kipidir (kayıt 21; ana anahtar `raidFacilityEnabled` ve eski tek tahta yolu kaldırıldı). Alt seçenekler `facilityCountermeasures`, `facilityMarks`, `facilityDefenseEnabled`, `facilityKatar`, `facilityYalpa`, `facilitySignatureMachines`, `facilityTitlesEnabled` ve `infiltratorFieldEnabled`; hepsinin C# varsayılanı açık. `facilityStretcherAlways`, `facilityTallyAmbush` ve `facilityLastBreathDies` deneysel kural seçenekleridir. Rehber ve ölçüm aracı değişikliği sürümü yükseltmez.

## Klasörler

```
Corrigible/                          (Unity proje kökü, git deposu)
├─ AGENTS.md, CLAUDE.md              yapay zeka ajanları için giriş
├─ Docs/                             bu belgeler; UI_Siginak (oda tasarımı, ilk tur), UI_Siginak_Rev2 (Revizyon 2: önceki tur eşlemesi,
│                                    S1–S9 panoları, görüntü seti), UI_Siginak_Rev3 (Revizyon 3 + ek 11-05: model paketi,
│                                    token 1.3, IMPLEMENTATION_MAP: güncel eşleme), UI_Sade ve UI_C (masa/baskın arayüzü
│                                    kökenleri), SonSalter_Baskin_Paketi (baskın görsel referansı)
├─ Tools/unity-helpers.sh            Unity CLI kısayolları
├─ Tools/Audio/                      yer tutucu ses üreticileri: raid_site_audio.py (9 yatak, 22 olay sesi), cabinet_audio.py (dolap, menü),
│                                    rev3_placeholder_audio.py (Revizyon 3'ün 42 ipucu)
└─ Assets/Corrigible/
   ├─ Core/                          saf C# simülasyon (asmdef Corrigible.Core, motor referansı yok)
   ├─ Game/                          GameController, BalanceConfig, platform; Audio/, Localization/, Raid/ (3B baskın tahtası)
   ├─ Desk/                          komuta odası ve ekran kâğıtları
   │  ├─ Scripts/Runtime/            asmdef Corrigible.Desk: Siginak/ (oda bileşenleri, vardiya ve operasyon raporu),
   │  │                              PlanningBoard/ (kart, cep ve görünüm), Settings/ (ayarlar dolabı),
   │  │                              Entrance/ (menü sahnesi), Raid/ (baskın HUD'u),
   │  │                              Sade/Map/ (kâğıt harita), Components/,
   │  │                              Theme/, Core/, Rev3/ (model prefab'ının kökü, parça sürücüsü, yazı yuvası,
   │  │                              malzeme kitaplığı)
   │  ├─ Scripts/Bridge/             asmdef Corrigible.Desk.Bridge: oyunu sunuma bağlar (Siginak/ bağlayıcılar, Entrance/
   │  │                              menü sahnesi, SimDesk*, baskın veri kaynağı)
   │  ├─ Scripts/Editor/             varlık hattı (Import/), oda araçları (Siginak/), bina kiti araçları (Raid/),
   │  │                              Revizyon 3 model hattı (Rev3/)
   │  ├─ Scenes/Giris.unity          menü sahnesi: sığınağın kapısının dışı (build listesinde ilk)
   │  ├─ Scenes/DeskGame.unity       oyunun sahnesi (build listesinde ikinci)
   │  ├─ Prefabs/                    Siginak/ (SettingsCabinet: ayarlar dolabı),
   │  │                              Sade/Raid/ (baskın HUD, kart, panel, etiket prefab'ları), Rev3/<aile>/ (198 model)
   │  ├─ Resources/                  temalar (DeskTheme, SadeTheme, RaidTheme, SiginakTheme), DeskSprites, fontlar
   │  ├─ Data/                       tema kaynakları (tokens_*.json), harita ekip noktaları, stil sayfası
   │  ├─ Art/, Fonts/                sprite'lar, atlaslar, dokular, modeller, malzemeler (Rev3/: OBJ kopyaları ve
   │  │                              malzeme kitaplığı); font dosyaları ve lisansları
   │  └─ Tests/EditMode/             asmdef Corrigible.Desk.Tests
   ├─ Scenes/Baskin.unity            taktik baskın; DeskGame'in üstüne eklemeli yüklenir
   ├─ Prefabs/Raid/                  3B baskın parçaları (karo, duvar, kanal, dolaplar, olay nesneleri, birimler; Kits/ gedik molozları)
   ├─ Art/Raid/                      baskın malzemeleri, dokuları, mesh'leri; Kits/<bölge>/ bina kitlerinin dokuları ve malzemeleri
   ├─ Audio/Placeholder/             yer tutucu sesler (SoundBank'a bağlı olanlar)
   ├─ Settings/                      BalanceConfig, RaidPieceLibrary, SoundBank; Kits/ (bina kitleri, Kit_<bölge>.asset)
   ├─ Resources/Localization/        strings.txt (çeviri tablosu, tr → en)
   ├─ Editor/                        CorrigibleDebug, LocalizationTool
   ├─ Tests/EditMode/                Core ve lokalizasyon testleri; harekât botunun baskın (RaidBot.cs) ve vardiya planı
   │                                 (CampaignBot.cs, aşama 8; Camp8 ve FacilityBotTests ortak, Tools/DengeBotu/derle.sh derler)
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
 Oda bağlayıcıları (PlanningBoardPresenter, SiginakDesk/Map/Crt/Shift/Briefing/Cabinet)
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
  komutlar (`StartLiberationRaid`, `ResolveRaid`, `SelectLiberationRegion`, `AssignResident`, `TrainResident`,
  `EquipResident`, `ScrapEquipment`, `QueueProduction`, `ReserveProductionMaterial`, `CancelProduction`, `PrintShift`, `FileShift`,
  `ChooseCandidate`, `PassCandidates`, `DecideShelterEvent`, `PullLastSwitch`),
  denetimler (`LiberationRaidCheck`, `PlanningRaidCheck`, `BoardPlanCheck`, `CandidateChoiceCheck`, `CandidatePassCheck`, olay ve
  son şalter `*Check` metotları: komut yapılamıyorsa gerekçe metni) ve türetilmiş değerler (`Occupation`, `LiberationFrontier`,
  `NextLiberationSector`, `IncomeBreakdown`, `DoorVisitor`, `DoorOptions`, `DoorOptionEffect`, `ShelterEventOptions`,
  `DismissSuppliesPreview`, son varken `EndReport`). Eski tek adaylı kabulün komutları (`InspectApplicant`, `DecideApplicant`,
  `ReviewResident`, `RestrictResident`, `DismissResident`) ve okuyucuları KOS aşama 7'de silindi.
  Komutlar `RunCommand` üstünden geçer: `ActResult { Ok, Msg }` döner, fon değişimini `TransactionLedger`'a, komutu kayda
  yazar. Dosyalar: `CampaignLiberation.cs` (bölge/sektör ve baskın), `Simulation.PlanningBoard.cs` (kart komutları,
  üretim, eğitim, devir), `Simulation.PlanningRaid.cs` (gerçek kişiden kadro ve kişiye dönüş),
  `Simulation.CommunityAdmissions.cs` (kurucu dosyaları, kapı aralığı, grubun süre aşımı), `Simulation.CandidateGroups.cs` (aday
  grubu, seçim, geri çevirme, yollama), `Simulation.ShelterEvents.cs` (olaylar, sızma zinciri, tecrit, kısıt), `Simulation.Endings.cs`
  (ifşa, `dusus`, `terk`, son şalter, zafer tonu), veri ve durum dosyaları `CommunityAdmissionState.cs` (aday, kişi dosyası,
  gizli kimlik), `ShelterEventData.cs` (olay tabloları, seçenekler, `SafeOption`), `ShelterEventState.cs` (açık olay, sayaçlar,
  günlük, vadeli kayıtlar), `EndReport.cs` (son raporun Core verisi), `Teams.cs` (ekip), `Economy.cs`
  (gelir ve işlem defteri). `Simulation.Shelter.cs`/`ShelterLogic.cs`, `Door.cs`/`DoorCards.cs` eski oda ve ziyaretçi
  yolunu korur; `Balance.planningBoardEnabled = true` varsayılanında bu döngü çalışmaz.
- **`WorldState`** (`Core/WorldState.cs`): oyunun tüm durumu (gün, fon, ekipler, süren baskın, baskın günlüğü, harekât ve
  sığınak, kapı (`DoorState`: bekleyen ziyaretçi, sıradaki vuruş, bayraklar, zincirler, ziyaretler), işgal geçmişi, kırılma
  anları, son). `Shelter.Plan` (`PlanningBoardState`) isimli sakinler, tek sahipli eşya, tür bazlı stok, üretim emri,
  ikmal hammadde ilerlemesi, plan kilidi ve fiş kimliğini taşır; `Simulation.Board` burayı okur. `Board.Admission` kapıdaki aday grubu,
  geri çevrilenler, kalıcı kişi dosyası, gizli kimlik ve izleri tutar; `Board.ShelterEvents` açık olayı, sırayı ve günlüğü. Görünümler doğrudan yazmaz. `S.Raid` varken veya kartlı
  oyunda etkin fiş dosyalanmıyorken `Step` çalışmaz.
- **`Baskin`** (`Core/Baskin.cs`): 8×8 taktik savaş motoru, kendi RNG'si (`BState.Rs`), Unity yok. `Create(RaidParams)` →
  `BState`; `DoMove`, `DoAct`, `Undo`, `Preview`, `EndTurn`, `Retreat`, `Result`, `Intents`. Stratejiye yalnız
  `RaidResult` döner (kayda `raidres` olarak yazılır; taktik hamleler kaydedilmez). `Create` kampanyada kullanılmaz: örnek/test
  tahtasıdır (elle çizilmiş `Layouts`; Baskin sahnesi tek başına, editör önizlemesi, motor testleri ve `RaidEngineTests`'in altın
  imzaları). Kampanya baskını (tesis, `Docs/TESIS-BASKINLARI-TASARIMI.md`) her odayı ayrı bir `BState` olarak `CreateRoom` ile kurar; kapı ve geçiş
  `Baskin.Chain.cs`'te, oda zincirinin kuralları saf `RaidChain`'de, durumu `RaidState.Reports` (`RoomReport`). Bölgenin
  gösterilen adı YZ'nin adıdır (`RegionDef.Ai`, `Simulation.RegionName`; iki dilde aynı, `strings.txt`'te kendine çevrilir);
  coğrafi `RegionDef.Name` yalnız iç kimlik ve eski metinlerde kalır.
- **Tesis hamleleri** (aşama 6; `Core/Baskin.Actions.cs`, `Baskin` `partial`; kurallar `Docs/TESIS-BASKINLARI-TASARIMI.md` §4):
  - Eski API (`Reachable`, `Targets`, `CanMove`, `CanAct`, `DoMove`, `DoAct`, `Preview`) uzmanın tek yeteneğidir ve değişmedi.
    Yeni hamleler ayrı API'den girer: `CanAction`, `ActionTargets`, `DoAction`, `PreviewAction` (kimlikler `ActionKinds`:
    `pusu`, `kosu`, `surukle`, `firlat`, `ortak`, `menfez`; bedel `CostOf`). `Preview`'ın gövdesi saf `Diff(B, C)`'dir; ikisi de
    onu kullanır.
  - Değerler `Balance` → `MoveRules.From` → `RoomSetup.Moves` → `BState.Moves` (örnek tahtada null; `CanAction` false). Her
    yeni kural `B.Room != null` arkasındadır; hiçbiri RNG çekmez; makine aklı pusu hattını, gövdeyi, yerdeki yükü görmez.
  - Tur içi durum: `Unit.AmbushDir/X/Y`, `Unit.Breath`, `BState.CargoX/Y`, `HoldId/HoldX/HoldY`, `CrateOpen`, `Cell.Crate`.
    Hepsi `Snapshot`/`Undo` ile geri gelir (`CopyInto` taşır); `Signature` onları yalnız varsayılan dışındayken basar.
    `EndTurn` pusuyu `PlanEnemies`'ten sonra ateşler (`FireAmbush`), tutuşu bırakır (`ClearHold`) ve son nefes için eski gövdeyi
    (`EndTurnCore`) sarar.
  - **Gövde modeli:** düşen kişi için yeni alan yok. `IsBody` (ekip, `Dead`, `!Left`, `Slot ≥ 0`); yerde yatan `LyingBody`
    (`!AtGate && !Out`), kapıya sürüklenen `AtGate`, tesisten çıkarılan `Out`. `OnBoard` gövdeyi dışlar: kareyi kapatmaz, hedef
    alınmaz, itilmez; yalnız `surukle` taşır. Zincirde `RoomMember.Body` (0 yerde, 1 ekiple, 2 çıkarıldı) ve `Picked`;
    `RaidChain.Collect` dönüşte atlanan temiz odanın gövdelerini toplar, `RaidChain.IsExtracted` → `RaidResult.Extracted`,
    `ReturnPlanningSquad`'ın tesis dalı yalnız çıkarılan düşeni kurtarır. Ekiple gelen gövdeler sonraki tahtada `B.Units`'in
    sonuna eklenir (kimlik sırası değişmez).
  - Büyük sandık plan bayrağıdır (`Cell.Crate`, duvar nişi; `Baskin.PlaceBigCrate`, RNG'siz); `FacilityMaps.GeneratorVersion`
    aşama 6'da 2, aşama 8'de 3.
- **Tesis ekipmanı** (aşama 8; `Core/FacilityGear.cs`, `Core/Baskin.Gear.cs`, kurallar `Docs/TESIS-BASKINLARI-TASARIMI.md` §5,
  §6.2):
  - Tablolar (kimlikler, bölge → tarif/kazanım, adlar, tarif, hak) `FacilityGear`'da statik, sayılar `Balance`'ta.
  - `Unit.Class` (null uzman, `kopek`, `aygit`, `tutsak`) ve `Baskin.IsSpecialist`: kaçış, GEÇ, son nefes, sonuç ve bot yalnız
    uzmanı sayar. Bölge olaylarının nişanı (`Baskin.Events.cs` `EventAim`) aşama 10'dan beri `TeamLike`'a bakar (uzman, yürüyen yem,
    erimde havlayan köpek; yağ yalnız uzmana).
  - Eylemler `ActionKinds` (altı, aşama 7'nin tuşları) ile ayrı `GearActionKinds`/`UnitActionKinds`/`AllActionKinds`; aynı
    `CanAction`/`ActionTargets`/`DoAction`/`PreviewAction` API'si. Aygıtlar `EndTurnCore`'da niyetlerden sonra, uyanmadan önce
    (`RunDevices`); tutsakların süresi planlamadan önce (`AdvanceCaptives`).
  - Simülasyon: `FacilityState.Recipes/Perks/PendingReward`, `server_reward` (`Core/Simulation.Facility.cs`), `BoardRecipes`
    açılan tariflerle, `SquadSlot.Gear`, `RaidParams.Perks`; oda raporu `Units`, `Sniff`, `CargoDog`. Yeni RNG çekimi yok.
- **Önlem defteri** (aşama 10; `Core/FacilityCounters.cs`, `Core/Baskin.Counters.cs`, kurallar `Docs/TESIS-BASKINLARI-TASARIMI.md`
  §6.3 "Uygulanan hâl (aşama 10)"):
  - `FacilityCounters`: altı sınıfın tablosu (adlar, etkiler, cevap eşlemesi, imza makinesi), saf öğrenme (`Learn`) ve sınıf başına
    tavanlı toplama (`RaidTally`). Durum `FacilityState.Tally/Counter/Learned`; alt bayrak `facilityCountermeasures`
    (`Simulation.CountermeasuresOn`), ölçüm anahtarı `facilityTallyAmbush`.
  - `Baskin.Counters.cs` (`Baskin` `partial`): sayım (`BState.Tally*`, emirde `MoveRules.Tally`), altı önlemin tahtadaki kuralları,
    beş cevabın etkisi, kaplama (`ApplyCounterCells`: çift cidar, koku yayıcı; oda üretimine girmez, Digest aynı) ve bot ile sunumun
    okuduğu public yardımcılar (`Bolted`, `IsDoubleWall`, `DoubleWallCost`, `EmitterBlocks`, `DeviceTurnsFor`, `ClampTurnsFor`,
    `DangerTiles`; vinç için `Baskin.Gear.cs`'te `WinchPulls`). Kademeler ve cevaplar emirde kopyalanır (`RaidParams.Counters/Answers`
    → `MoveRules.From`). Tek yeni `Rnd`: kısa bağın 2. kademesi.
  - Simülasyon: `ResolveRaid` sayımı toplar ve bölgenin ilk tamamlanışında öğrenir; cevap tarifi temelin sayfasına geçer
    (`BoardRecipe.Answer`, aynı `Id`, +1 devre) ve başlamamış taslak emri siler. Yeni komut yok; tesis izi f4.
- **Kalıcı tahta ve savunma** (aşama 11; `Core/FacilityMarks.cs`, `Core/Baskin.Defense.cs`, `Core/Simulation.Siege.cs`, kurallar
  `Docs/TESIS-BASKINLARI-TASARIMI.md` §9 "Uygulanan hâl (aşama 11)"; alt bayraklar `facilityMarks` → `Simulation.MarksOn`,
  `facilityDefenseEnabled` → `Simulation.DefenseOn`):
  - `FacilityMarks` (saf, çekimsiz): `Apply(cells, room, marks, rubble, events)` izleri plan kopyasına uygular ve uygulanamayanları döner
    (`RoomBoard`'da önlem kaplamasından sonra, `DefenseBoard`'da, `Simulation.FacilityPlanCells`'te; plan = emirdeki tahta), `Derive`/`Merge`
    (raporlardan iz), `Wipe`, `InWing`, `Copy`, `Links` (bağ kuralı). Durum `FacilityState.Marks/Repairs`; emirde `RaidParams.Marks`
    (`Simulation.WingMarks`). `RaidChain.Setup`'ın isteğe bağlı `FacilityPlan plan` parametresi kırık donanım izini hazırlığa katar
    (`MarkedDevices`).
  - Savunma tahtası (`Baskin` `partial`): `RaidParams.Mode = "defense"` → `RaidChain.SetupDefense` → `RoomSetup.Defense/StartTurn/Followers/
    Abandoned/RelayHp/Marks/Waves` → `Baskin.DefenseBoard`; `BState.Defense/StartTurn/Abandoned/Incoming/Waves` (`Clone`, `CopyInto`,
    `Signature` "yalnız varsayılan dışındayken"). `EndTurnCore`'a `ReturnFromGate` (başta) ve `PlaceIncoming` (tur arttıktan sonra);
    `PassRoom` kuyruğu rapora takipçi yazar; `Turn > MaxTurns` savunmada kazanma. Röle nesne `role` (`Baskin.RelayKind`, `ObjNames`'te
    adı var; `IsTarget` değil). Takvim `Baskin.DefenseSchedule` (saf; dengeden `RaidChain.DefenseSchedule`). Okuyucular `IsDefense`,
    `DefenseWaves`, `DefenseIncoming`, `RelayHp/RelayMax`, `BarricadeTurn`, `CanEndTurn`, `Abandoned`, `StartTurn`; test kancası
    `Baskin.TestPlan` (internal). Savunma aynı zincir API'siyle oynanır (`OpenRaidRoom`, `AdvanceRaidRoom`, `FacilityRaidResult`,
    `ResolveRaid`); sunum (`FacilityRaidSession`) ve bot (`RaidBot.PlayFacility`) kip bilmeden sürer.
  - Simülasyon (`Simulation.Siege.cs`): `ChangeAlarm` eşikte `DefenseOn` iken `BeginSiege` (yoksa `Counterattack` = `PickCounterTarget` +
    `FallSector`); kuşatma günlüğü `FacilityState.Sieges` (`FacilitySiegeEntry`; tek kaynak), karar vardiyası (`stepping` bayrağı,
    `Step` → `StepCore`), kilitler `LiberationRaidCheck`'te; komutlar `DefendSector` (`siege_defend`; ortak emir gövdesi `OrderRaid`,
    tohum `Rng.Mix(S.Seed, 0x53494547 + dizin)`) ve `YieldSector` (`siege_yield`); `ResolveDefense`; `Step`'te `AdvanceFacilitySiege` →
    `AdvanceFacilityRepairs` → `StepAlarm`; `raidres`'te `UpdateFacilityMarks`. Okuma sözleşmesi: `PendingSiege`, `ActiveSiege`,
    `SiegeDecisionDue`, `SiegeSector`, `YieldedToday`, `DefendSectorCheck`, `YieldSectorCheck`, `FacilityMarksOf`, `WingMarks`,
    `RegionRewarded`, `RegionFundsDue`, `DefenseWaveSize`, `DefenseRelayHpFor`, `DefenseRoomTrust`, `DefenseTurns`, `DefenseWaveCount`,
    `DefenseSchedulePreview`, `IsDefense`, sabitler `DefenseMode`, `Siege*`. Kartsız kapının ziyareti kuşatmayı bilir (`DoorVisit.Siege`).
    Yeni RNG çekimi yok; tesis izi f5.
- **Tesis şartları Katar ve Yalpa** (aşama 12; `Core/Baskin.Conditions.cs`, kurallar `Docs/TESIS-BASKINLARI-TASARIMI.md` §10 "Uygulanan
  hâl (aşama 12)"; alt bayraklar `facilityKatar`, `facilityYalpa`):
  - Kimlik: `RegionDef.FacilityEvent` → tek okuyucu `FacilityMaps.EventFor(def, katar, yalpa)` (plan, `OrderRaid`, `Simulation.RegionEvent`);
    plan önbelleği (tohum, iki bayrak); Katar'da ileri geçit doğu sütununda (`Dress(…, eastOnly)`; `GeneratorVersion` 6). Şart parçası yok.
  - Katar (`Baskin` `partial`): tek giriş `Push` (cıvata → açık kenar `FallOff` → turnike/duvar); `Unit.Overboard` (`Dead` ile; gövde ve
    ayakta değil), `fall` Fx; takip `FollowerIds` (batı geçidine yürüme uzaklığı ≤ `MoveRules.FollowRange`) → `RoomReport.Follow` →
    `RaidChain.Next`/`Setup.Followers` → `ReturnBoard` sonunda ortak `EntryTile` (aşama 11 `IncomingTile` ile ortak; `StepsFrom`),
    gelen takipçi `Unit.Follower` (okuyucu `FollowedInIds`); AYIR `Baskin.Chain.cs` `DecoupleBlock`/`CanDecouple`/`Decouple`
    (`BState.Decoupled`, `RoomReport.Decouple`). Kişi durumu `"k"`, `RaidResult.Overboard`.
  - Yalpa: `EventEnd` → `EndTilt` (takvim `TiltAt/TiltDir/TiltSchedule`), `Push(…, slide: true)` kayma kipi, hurda/gövde/yük kayması;
    `Slides(B)` kopyada uygulamayla hatlar (`Hazards`'ın `slide` çizgileri); makine aklı yatış turunda −0,5.
  - Simülasyon: `ReturnPlanningSquad`'ın `over` dalı (yaşar, `facilityFallInjuryTurns` düz yatış; `SquadOutcome.Overboard`),
    `KatarReportCheck` (`AdvanceRaidRoomCheck`'in sonunda), okuyucular `RegionEvent`, `RegionNote`, `FacilityRoomTurns` (ilk ziyaret tur
    bütçesi: `RaidChain.Setup` + `Baskin.FirstVisitTurns`; `RoomBoard` aynı kuralı okur). Yeni komut ve yeni RNG çekimi yok; tesis izi f6.
- **Tesis imza makineleri** (aşama 13). `Simulation.RegionSignature` imza alt bayrağını (`facilitySignatureMachines`) okur. `Baskin.Signatures`
  niyeti hedef ve alan olarak üretir, makine aklıyla puanlar ve makine evresinde uygular. Baskın sunumu `Intents`, `Hazards` ve
  `Fx` üzerinden çizilir. `Cell.FrostUntil` oda tahtasına aittir. `RoomMachine` ve `RoomUnit` içindeki `Assembled` dronla taşınır.
  `RoomObject.MachineBuilt` geçici kalası ve hurdayı dönüş odasında kurar. `FacilityMarks.Derive` kalıcı izleri bundan ayırır.
- **Tek kip ve kayıt uyumu (aşama 16, kayıt 21'de yeniden yazıldı).** Her harekât emri üç oda taşır (`RaidParams.Rooms`); `LiveRaidDataSource` emri oda oturumuna verir, odasız emirde `Debug.LogError` basıp sahneyi kapatır, `Baskin.Create`'i yalnız sahne tek başına açılınca (örnek baskın) çağırır. `ResolveRaid` verilen `RaidResult`'ı yok sayar: son oda raporlanmadıysa reddeder, sonucu oda raporlarından türetir (`FacilityRaidResult`; yalnız `Reason` korunur). Harita torbası, `MapSlot`, `ExtraTurns` ve görev ek turları kaldırıldı. G5 paketinin YZ adı `MapRegion.SourceName` olarak harita izine girer. Pano ve canlı hurda komutu `Simulation.ScrapEquipmentCheck` ile aynı kurala bağlıdır. Kaydedilmiş hurdanın tekrarında eski köpek işlemi de desteklenir.
- **Sahadaki sızmacı (aşama 15).** `Simulation.SnapshotFieldIdentities` mevcut aday değerlerini emir anında `RaidParams` içindeki internal kadro maskelerine dönüştürür. Masa, HUD ve plan bu alanları okuyamaz. Gözlenen taraf değişiminden sonra `Baskin.IsTurncoat`, niyet ve telsiz satırları okunabilir. Oda raporunda `t` ve `x` kalıcı sonuçtur. Yeni komut gerekmez. Saha tutanağı mevcut olay kararlarını kullanır.
- **Kişisel unvanlar** (aşama 14). `FacilityTitles` işi ve eşikleri tanımlar. Birimin oda içi `Work` ve `Dragged` bitleri
  `RaidChain.Report` üzerinden `RoomMember`'a, oradan `raid_room` kaydına eklenir. `ReturnPlanningSquad` yaşayan kişiye
  kazanılmış baskın başına en çok bir iş ilerlemesi verir. `ResidentState.Deeds/Titles` replay sırasında tekrar türetilir.
  Kişisel etkiler emir anında `SquadSlot.Titles` olarak kilitlenir. Bot ve baskın önizlemesi `Baskin` menzil ve süre
  okuyucularını kullanır. `OperationReportBuilder` sonuçtan yeni unvanı, kişi dosyası mevcut unvan ve etkisini okur.
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
- **`RaidMaps`** (`Core/RaidMaps.cs`): tesis odalarının harita üreticisi (`FacilityMaps` çağırır). Altı tür (`Kinds`: koridor,
  kasa odası, iki hat, açık salon, şebeke, rehine), türün kendi oda adları (`Names`). `Generate(kind, rng)` türün yapısını
  kurar (`BuildVault`, `BuildLanes`…), sonra dolapları ve makineleri yerleştirir; `Valid(rows, kind)` ortak kuralları ve türün
  adım aralığını (`PathRange`, bot ölçümüyle ayarlı) sınar (bağlantı, hedefe uzaklık, sayılar, dronların girişe açık atış hattı
  yok); kendi üreteciyle çalışır, `S.Rng`'den tüketmez. Üretici `RaidMapsTests.Generate_Golden` ile bit bit kilitli. Eski
  harekât havuzu (`Pool`, `Slot`, `PoolSize`, `KindOf`, tek argümanlı `Valid`) kayıt 21'de kaldırıldı. `StartLiberationRaid`
  `RaidParams.MapSeed = S.Seed` ve `Tactics = 1` (gelişmiş makine aklı) yazar; `MapSeed` Core'da tahtayı seçmez (örnek tahta
  `Baskin.Create` onu yok sayar, `RaidMapsTests.Create_IgnoresMapSeed`), sunumda bölgenin bina kitini seçer (0 = varsayılan
  kit). Kayıt etkilenmez: tahta stratejiye yalnız sonuç verir.
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
- **`GameController`** (`Game/GameController.cs`): komut kapısı. `NewGame`, komut metotları,
  `ExecuteBoardCommand` (pano/kabul için `Run` ile kayıt ve olay dağıtımı), `PrintBoardShift`, `FileBoardShift`;
  `AdvanceShift` yalnız eski oyunda doğrudan vardiya ilerletir,
  `Flush` (olayları `EventsRaised` ile dağıtır; harekât bitince başarımı açar, kaydı siler), `Save`/`ContinueSaved` (dosya
  `SaveStore`'da; menü sahnesi kartın özetini aynı yerden okur).
  `StoryHold` açılış ve emir belgesi açıkken vardiyayı engeller. Olaylar: `Changed`, `EventsRaised`, `NewGameStarted`,
  `GameLoaded`. Zaman kendiliğinden akmaz.
- **`LiberationDeskView`** (Bridge): odanın komut kapısı (`IDeskSurface`). Komutları GameController'a iletir, sonucu
  gerekçesiyle teleksten bastırır (`TelexReasons`), girdi kilidini (`InputBlocked`: açık ekran kâğıdı, hikâye, baskına
  geçiş) tek yerde hesaplar.

## Determinizm ve kayıt

- RNG: `Rng` (mulberry32, `Core/Rng.cs`). Durumu `S.Rng.State`. Kişi profili, geliş aralığı, aday ve gizli kimlik
  bu üreteçle ve sabit tüketim sırasıyla kurulur. Kimlik sonradan karar anında yeniden çekilmez. Eski ziyaretçi kapısı
  kendi üretecini harekât tohumu, gün ve amaçtan kurar (`Simulation.DoorRng`); ana sıraya dokunmaz (`DoorTests`). `Js.Round` = JS `Math.round` (`Math.Floor(v + 0.5)`);
  tüm sayılar `double`. Aynı tohum + aynı komutlar = aynı oyun (`LiberationCampaignTests`).
- **Ayrı akışlar:** tesis planı (`FacilityMaps`, `RaidMaps` üreticisiyle) ve üretilmiş dünya haritası (`WorldMap`) kendi üreteçlerini harekât
  tohumundan ve bir tuzdan kurar (`Rng.Mix`, ortak splitmix karıştırıcı; `WorldMap.Salt` = 0x4D415031 "MAP1"); ana sırayı
  tüketmez, altın zaman çizgileri anahtardan bağımsızdır (`WorldMapTests.Generation_DoesNotConsumeTheMainRng`).
- **Üretilmiş harita (`Core/WorldMap.cs`, `WorldMap.Gen.cs`, `WorldMap.Names.cs`):** `Balance.liberationMapGen` = 1 iken
  `Simulation.NewGame` haritayı kurar (`Simulation.Map`; 0'da null). Simülasyon haritayı yalnız `Simulation.Links` (sınır),
  `Simulation.RegionName(id)` (anlar, baskın parametresi, mesajlar) ve `Simulation.HQ` (hep "weu" paketi, sabit kalır) üstünden
  bilir. Sunumun okuyacağı geometri: `MapCell` (6 köşe `X/Y` double, ortak köşe kimliği `Vertex`, `Neighbor`, `Land`,
  `Region`, `Continent`, `Depth`, merkez `CX/CY`), `MapRegion` (`Cells`, en içten dışa `Inner`, `Neighbors`, `Distance`,
  `Stage`, `SourceName`), `MapLink` (`Cable`, uç hücreler ve merkezleri), `Coast` ve `Borders` kenar
  listeleri (köşe kimlikleriyle zincirlenir), `HqCell`. Harita salt okunurdur (dışarıya `IReadOnlyList`, iç alanlar `internal`;
  testler `Core/AssemblyInfo.cs` ile iç üyeleri görür) ve son üretilen harita önbellekte paylaşılır (`WorldMap.Generate`; kayıt
  süzgeci, yeniden oynatma ve özet aynı tohumu art arda ister). Sunum kenar kırarken kendi kopyasını kurar. Kararlarda
  trigonometri ve kayan noktalı karekök yok (tam sayı elips ve karekök, BFS, altıgen uzaklığı, kablo hattı için tam sayı
  örnekleme); köşe sapması yalnız çizim içindir. Geçerlilik `WorldMap.Validate()`; altın harita tohum 79
  (`WorldMapTests.Seed79_GoldenMap`): üretici ya da tabloları değişirse `WorldMap.GeneratorVersion` (parmak izine girer) ve
  yayındaysa kayıt sürümü artar (şimdi **5**. Ayrı `KARI` akışındaki açılış karıştırması ve karargâh alan eşikleri g3/g4'ten
  gelir. g5'te yer adları ve ortak imza için komşuluk takasları kalktı). Görsel döküm `Tools/HaritaDokumu/dokum.sh`. Anahtar `Settings/BalanceConfig.asset`'te **1** (oyun
  üretilmiş haritayla açılır); C# varsayılanı 0 kalır (dünya haritası; eski testler ve geri dönüş yolu).
- `GameRecord` (`Core/GameRecord.cs`): `version` (**21**; 16 aday grubu ve olaylar, 17 ifşa ve sonlar, 18 üretilmiş harita,
  19 tesis oda zinciri, 20 g5 harita, 21 tek kip: tek tahta kaldırıldı, tesis izi hep f8/f9), `seed`, `balance`, `commands[]`, `day`, `map` (haritanın parmak izi: `m0-…` dünya, `m1-…` üretilmiş;
  `Replay.MapMatches` yükleme süzgeci ve `SaveStore.Write`'ta `.map.backup` arşivi, `Replay.Restore` uyuşmazsa hata verir,
  izsiz geçici kayıtları denetlemez ve izlerini doldurur; `GameController.ContinueSaved` başarısız komut sayısını loglar),
  `facility` (saha alt bayrağına göre `f8` veya `f9`; kayıt 21'den beri hiç boş değil, boş izli kayıt `MapMatches`'ten geçmez).
  `MapMatches` iki izi de sınar. f2–f6 tesis kurallarını, f7 imza makinelerini ve Katar/Yalpa pafta adlarını, f8 unvan işlerini, f9 sahadaki sızmacının sonucunu izler.
  g5 üretilmiş harita sonucu tesis kipinden bağımsız değiştiği için kayıt süzgeci tüm sürüm 19 dosyalarını reddeder; sürüm 20
  dosyaları tek tahtanın sonucunu taşıyabildiği için reddedilir (`SaveStoreTests.Load_RejectsVersion20`).
  `SavedCommand { day, kind, a, b }`. Harekât komutları `liberation_region`, `liberation_raid`, `raid_room` (tesis
  baskınında biten oda, a = `RoomReport.Encode()`), `raidres`, `server_reward` (tesis, aşama 8: a ödülü bekleyen bölge, b
  `tarif` ya da `kazanim`), `siege_defend` ve `siege_yield` (tesis, aşama 11: kuşatmanın karar vardiyasında savun ya da bırak; argümansız);
  pano komutları `board_assign`, `board_train`, `board_equip`, `board_queue`, `board_reserve`, `board_cancel`,
  `board_print`, `board_file`, `board_scrap`; kapı, olay ve son komutları `candidate_choose` (a aday, b yollanacak kişi),
  `candidate_pass`, `event_decide` (a seçenek), `last_switch`. `shelter_upgrade`, `shelter_assign`, `door_answer` eski yol için
  korunur. Eski kabulün beş türü (`inspectApplicant`, `decideApplicant`, `reviewResident`, `restrictResident`, `dismissResident`)
  aşama 7'de `Replay.Apply`'dan silindi (bilinmeyen tür sessizce atlanır); bunlardan birini taşıyan kayıt `SaveStore.Load`'da
  yüklenmez (`HasRetiredCommand`) ve yeni oyun yazılırken `.retired.backup` olarak arşivlenir (sürüm 17 kalır).
- **Denge kaydın parçasıdır.** `GameController.NewGame` dengeyi `BalanceConfig.CreateRuntimeCopy()` ile alır (asset'in
  `Clone()`'u; `Balance` düz alanlıdır, sığ kopya yeter), `Simulation.NewGame` kopyasını `GameRecord.balance`'a yazar ve
  JsonUtility bütün public alanları serileştirir. Kayıttan devam kaydın dengesiyle kurulur, `BalanceConfig.asset` okunmaz. Bu
  yüzden yalnız varsayılan sayıyı değiştirmek (`Balance.cs` ve asset) sürüm istemez; kural ya da RNG sırası değişirse ister
  (AGENTS kural 1). Yeni bir `Balance` alanı eklenince eski kayıtta o alan yoktur ve JsonUtility `new Balance()` varsayılanını
  bırakır: alanın varsayılanı eski davranışı vermiyorsa sürüm gerekir. `BalanceConfig.asset` `Balance` varsayılanlarıyla
  alan alan aynı tutulur (KOS aşama 8'de yansımayla doğrulandı); testlerin çoğu `new Balance()` kullanır, oyun asset'i.
- `Replay.Restore(rec)` aynı tohumu ve dengeyi kurar, komutları sırasıyla uygular; başarısız komutları
  `Simulation.ReplayFailures`'ta sayar (0 olmalı; sunum isterse loglar). Kartlı oyunda `board_file`
  kapanan güne kaydedilir ve `FileShift` günü kendisi ilerletir. Gün atlamak için eksik fişle doğrudan `Step` eden
  bozuk kayıt sonsuz döngü yerine hata verir. Baskın sonucu `RaidResult.Encode` ile kadro canı, zırhı ve geride kalan
  sıralarını da taşır; aynı kişi kimlikleri başlangıç/atama komutlarından yeniden kurulur.
- `GameController.Save()` → `SaveStore.Write` → `persistentDataPath/liberation_savegame.json` (JsonUtility); her
  başarılı komutta, kapanışta ve uygulama kapanırken. Dosya önce `.writing` olarak yazılıp atomik değiştirilir. Farklı
  sürümdeki eski dosya üzerine ilk yazmada `.v<eski sürüm>.backup` kopyası tutulur; bu, sürüm göçü değildir.
  Sürümü 22 olmayan dosya (22: görevde iyileşme) `SaveStore.Load`'da yüklenmez (`rec.version != GameRecord.CurrentVersion`); eski harekât yeni
  sürümde devam etmez. Kayda girmeyen oda durumu (`SiginakDeskStore`, `liberation_desk.json`)
  tohum eşleşmezse yok sayılır.
- Editörde `SessionState["Corrigible.TestStorage"]` doluysa `SaveStore.StorageDirectory` ve `SiginakDeskStore` ayrı
  doğrulama klasörüne gider. Bu anahtar oyun kaydının parçası değildir; test sonunda silinmelidir.
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
SiginakRoom              oda: masa (sayaçlar, anahtar rayı, şalter, teleks daktilosu, evrak tepsisi, Desk/MapMount altında kâğıt
                         harita SadeMapCanvas), vardiya şalteri (basma düğmesi; devir fişi teleks daktilosundan çıkar) ve mevcut EVRAK kutusu; Planning board (daha küçük ayaklı pano,
                         kişi kartları, görev alanları, ayrı depo ve üretim alanı),
                         CRT, duvar; sağ duvarda Bunker door
                         (DoorProp, DeskTarget; SM_Door = kasa, Leaf = menteşe pivotu (LeafMesh, Wheel, PeepCover, ColdGlass),
                         kanat odaya açılır; ziyaretçi çentiği; yanda IntercomBox) ve ardında Corridor (merdiven boşluğu, iki lamba, Visitors:
                         VisitorSilhouette); Admission station (AdmissionStation: kabul tezgâhı, panel, tezgâh lambası,
                         damga rafı (KABUL, GERİ ÇEVİR), Aday grubu (CandidateGroupView: üç kart, grup fişi)),
                         Admission controller (SiginakAdmission); Poses/K8 (kapı pozu), K8A, K8R (K8G emekli çapa);
                         arka duvarın sağında, CRT'nin üstünde Settings cabinet (SettingsCabinet prefab'ı; K9 çapası içinde) ve Cabinet controller
                         (SiginakCabinet);
                         SiginakHud (Overlay 45: karartma, kenar satırı, görev fişi, ATLA plakası, vardiya raporu,
                         operasyon raporu, emir belgesi, kapı kâğıdı, teleks şeridi, üzerine gelme etiketi)
Planning closing fade   Overlay; fiziksel devir kâğıdı mevcut EVRAK kutusuna girince kararıp açılır
SiginakCrtOverlay        Overlay 60, CRT'ye dalış geçişi
```

- **Üçüncü taraf oda modelleri (2026-11-13):** masa (`Desk/DeskModel`, `SM_Table_2`), kapı (`Bunker door/SM_Door`), monitör
  (`CRT desk/CRT/CRT`, `SM_TV_2`), telsiz (`CRT desk/Radio/SM_Radio`, `SM_Device_1`) ve duvarlar (`Shell/SM_Wall4x4`, `SM_Wall15`)
  `Assets/LeartesStudios/ColdWarUndergroundBunker` paketinden, kullanıcının yerleştirdiği yerde durur; Rev3 manifest hattının
  (`Rev3Model`, `PartDriver`) dışındadır ve çarpıştırıcı taşımaz (tık hedefleri köklerdeki `DeskTarget` kutuları).
  - Kapı tek parça geldiği için `SM_Door_LOD0` üç türev mesh'e bölündü (`Art/Rev3/SceneMeshes/Door/DoorFrame_SM`, `DoorLeaf_SM`,
    `DoorWheel_SM`; köşeler SM_Door koordinatında): kasa `SM_Door_LOD0`'da kalır (LODGroup kapalı, LOD1–4 kapalı), kanat ve çark
    `Bunker door/Leaf` pivotunun altında. Pivot menteşe ekseninde (SM_Door yereli −0,1265; y; −0,540) ve yerel y'si aşağı bakar:
    `DoorProp`'un +95°'si kanadı odaya açar. Yeni kapıda gözetleme deliği yok; `Leaf/PeepCover` görünmeyen bir Transform.
  - Monitörün ekranı eski `Crt` modelinin `Screen` parçası (tüp + `Display` tuvali + `ScreenLight`), TV'nin tüp çukurunda
    (`CRT/Body/Screen`); `Body` yalnız taşıyıcı. Telsizde ayrı ibre/düğme yok: `CrtInstruments.dial/needle/tune` boş, `DialLight`
    `Radio` kökünde. KOS aşama 8'den beri `SM_Radio`'nun LOD0–4 renderer'ları ahize kordonu çıkarılmış türev mesh'leri çizer
    (`SceneMeshes/Radio/SM_Radio_LOD*_NoCord`, prefab örneğinde mesh geçersiz kılması; kordon kanat ve CRT arasında yer bulamıyordu).
  - Model entegrasyonu 2: masa lambası (`Desk/Desk lamp/SM_Lamp`; ışık `Desk lamp light` abajurun ağzında), fincan (`Desk/Mug/SM_Cup`),
    kapı lambası (`Bunker door/KnockLamp/SM_SmallLight`; `SceneMeshes/KnockLight` kafes + cam, cam `DoorProp.lamp`), kurtarılan bölgeler
    panosu (`Shell/Freed board/CorkBoard/SM_Board`) ve sığınak panosu (`Planning board/PlanningBoard/SM_Board (1)`). Panolarda Rev3
    modeli (parçalar, çapalar, yazılar) mesh'siz iskelet olarak kalır; kökler eski baskı yüzü yeni yüze denk gelecek biçimde oturtuldu
    (`BoardLayout.FaceZ` = yeni yüz). Sığınak panosunun işlevsel yüzeyleri `Body/Pano ekleri` (`SceneMeshes/PlanningBoard`).
  - Ayarlar dolabı `SM_PowerBox1` (odada ve menüde aynı): `SceneMeshes/PowerBox/PowerBox1*`
    gövde/kapak/kilit; kapak `<model>/Body/Leaf`, kilit `Leaf/Latch`, gövde iç içe Rev3 modelinin altında (hover kaldırma).
    `Interior` 0,70 ölçekli, `Camera anchor` aynı afinle yaklaşır. Menüde (`Giris`) `SettingsCabinetMenu` prefab örneği aynı sahne
    override'larıyla çevrildi (prefab'lar Rev3 dolabını göstermeye devam eder); MENÜYE DÖN yok, kör kapak kalır.
  - Teleks daktilosu (kullanıcının modeli `Assets/Typewriter/Prefabs/Typewriter_FBX_Unity`, `Desk/Teleprinter` altında, çarpıştırıcısız;
    eski Rev3 `Telex` örneği silindi): kâğıt `TelexPaper` silindirden çıkar (kök 13° döndürülüp silindire oturtuldu, türev mesh
    `SceneMeshes/Telex/TypewriterTelexPaper_SM` yalnız ön kâğıt, alt kenarı silindirin altında), rulo `Paper roll`
    (`TypewriterTelexRoll_SM`, eski Telex.obj'nin rulosu, `Teleprinter.roll` harf başına döner) ve besleme şeridi `Paper feed` arkada,
    `Telex plate` (`TelexPlate_SM` + `TXT_Plate` "TELEKS") kapakta. Kökteki DeskTarget kutusu gövdeyle sınırlı, kâğıdın kendi
    `Paper target`'ı var. Devir fişinin çıkış çapaları `Teleprinter/Slip feed/SlipStart, SlipExit` (token
    `shiftClose.slip.typewriterLocal*`); şalterin fiş ağzı (`SlipMouth`) odadan kalktı.
  - Menü kapısı (`Giris/Entrance/Bunker door`) odadaki kapının aynısı ve aynı dünya pozunda (iki sahne aynı koordinatları paylaşır):
    `SM_Door` (LOD0 = `DoorFrame_SM`), `Leaf/LeafMesh`, `Leaf/Wheel/WheelMesh` (çark mesh'i iki yüzdeki çarkı tek milde taşır), kanat
    odaya açılır. Koridor yüzünde `Leaf/TXT_StencilOuter` ("SIĞINAK 07 · KYGA"). Eski Rev3 `DoorFrame`/`DoorLeaf`/`OuterWheel`/`PeepRing`
    hiçbir sahnede yok (manifest hattında duruyorlar).
- Oda, yerleşim ve bileşen bağlantıları sahnede serileştirilir; kalıcı kurucu yoktur (son kurucu `eed2824`'te). Tasarım
  eşlemesi ve değişiklik günlüğü: `Docs/UI_Siginak_Rev3/IMPLEMENTATION_MAP.md` (Revizyon 3, güncel); önceki turlar
  `Docs/UI_Siginak_Rev2/IMPLEMENTATION_MAP.md` ve `Docs/UI_Siginak/IMPLEMENTATION_MAP.md` (ilk tur, poz/kamera ayrıntısı).
- **Malzeme üretimi:** Core `EnableAutomaticProduction` ve `StageProductionMaterial` komutları `Rec`/Replay ile kayıtlıdır.
  `AutomaticProduction` yalnız yeni komutla açılır; eski geçmişte false kalıp eski emir sonuçlarını korur. `ProductionInputs`
  serbest stok hesabına dahildir. Kapanışta uygun tarif/üretici/yer varsa tek kez ödenip tek ürün depoya eklenir. Sunum
  `ProductionMaterials` kartlarıyla yalnız bu durumu gösterir. Eğitim işaretleri sahnede serileştirilmiş kalem halkalarıdır.
- **Kartlı pano (Revizyon 3 · Aşama 3; `Desk/Scripts/Runtime/PlanningBoard/Rev3`, `Bridge/Board3D`):** pano modelin
  (`SiginakRoom/Planning board`, `PlanningBoard` modeli) üstünde 3B kartlarla çizilir.
  - **Akış:** `PlanningBoardPresenter` simülasyondan `PlanningBoardSnapshotBuilder` ile yerelleştirilmiş bir `BoardSnapshot`
    kurar (RNG çekmez, komut yazmaz) ve `BoardView3D.Render`'a verir. Çizim yalnız değişimde (oyun `Changed`, yükleme, dil,
    poz, sürükleme sonu). Bırakma: `BoardDragController.DropRequested` → `BoardDropRules.CommandFor` (tek Core komutu) →
    `GameController.ExecuteBoardCommand` → `drag.Resolve(ok)`; önizleme `BoardPreviewText`. Seçim, eğitim konusu ve fiş
    yerleri sunumdur (`BoardUiState`; `PlanningBoardLayoutStore` v2, `planning_board_layout.json`).
  - **Geometri:** `BoardLayout` pano mm'sini (iç alanın sol üstü, y aşağı) model yereline çevirir (x = −0,580 + mm/1000,
    y = 1,593 − mm/1000; yüz z −0,014, kartlar seviyeyle öne). Ölçüler tema (`board.layoutMm`); saf hesap, testten çağrılır.
  - **Kartlar:** `Prefabs/Rev3Board/{PersonCard, ItemCard, MaterialCard, ChipCard}` = paketin kart modeli (iç içe, geçersiz
    kılmasız) + `BoardCardView` (havuz Core kimliğiyle, `Place`/`MoveTo`/`ArcTo`, mıknatıs) + yüzde tek World Space tuval
    (1 birim = 1 mm, ölçek 0,001; raycaster yok) ve altında pişirilmiş sözcüksüz ışıklı taban malzemesi. Yüzler
    (`PersonCardFace`, `ItemCardFace`, `MaterialCardFace`, `ChipCardFace`) yalnız değişince bağlar (`CardFace.ChangedBinds`).
    `CardStateOverlay` durum katmanlarını ve modelin bayrak parçasını sürer. Portre `PortraitLibrary` (kırpma döşemesi).
  - **Pano parçaları:** `BostaDeckView`, `StoreView`, `WorkOrderView`, `TrainingTopics`, `PersonFolderView`,
    `LostEnvelopeView`, `HurdaBin`, `BoardMarks`; üzerine gelme etiketi `HoverTag` (bırakma önizlemesi `IHoverFrame` ile
    mavi/amber çerçeve). Sesler `BoardSound` → sunucu → `Sfx` (97–106).
  - **Girdi:** kartın ve pano parçalarının çarpıştırıcıları odanın `PhysicsRaycaster`'ıyla; boş pano tıklaması hiyerarşi
    boyunca `BoardDragController`'a çıkar. Sürükleme pano düzlemine ışınla; kamera girdisi bu sırada askıda.
    Esc/sağ tık/MASAYA DÖN `SiginakCameraRig.BackClicked` → sunucunun geri zinciri (açık dosya, deste, sonra masa).
  - **Sahne:** bileşenler ve bağları `DeskGame.unity`'de (tek seferlik `Rev3Stage3Board` kurdu, silindi). Eski uGUI pano
    tuvali ("Serbest pano kartları") Aşama 10'da (T1c) sahneden silindi: `PlanningBoardView` ve `PlanningBoardMotion` kökte kalır, tuvale bakan alanları boştur,
    public API'leri durur (`SetCamera`, `SetStatus` boş alanla güvenli; `Motion`'ın deste yolu kartlı oyunda çalışmaz). Kapının eski uGUI kabul penceresi Aşama 6'da silindi (`PlanningBoardView` kabul alanları
    boş, yerine kabul istasyonu), tarif defteri Aşama 5'ten beri 3B. Aşağıdaki üç madde eski uGUI panonun kodunu anlatır; sahnede karşılığı kalmadı.
- **Defter ve yedek eli:** `PlanningBoardMotion` sahneye kaydedilmiş kapak, iki sayfa, gezinme düğmeleri ve yedek elini unscaled zamanla hareket ettirir. Tarifler yalnız açık sayfada görünür; sürüklenen tarif kapanan defterden bağımsız kalır. Boşta kişiler kapalı deste/yan yana el arasında taşınır; ilk kişi en üstte çizilir ve kapalı desteden doğrudan sürüklenebilir. Yalnız tıklama üst karttan başlayarak eli açar; kişi alınırken diğerleri geri döner. Açık elde kart, boş görev alanı veya `PlanningBoardSurface.Clicked` eli kapatır. Başarılı Boşta bırakışında `AcceptReserveDrop`, kartın `KeepPosition` dönüş noktasını ve ilk deste yerleşimini korur; eski göreve sıçrama olmaz. Ek el paneli/başlığı/Topla düğmesi yoktur. `PlanningBoardCard` pointer-down ile tıklama durumunu sıfırlar ve sürüklemenin bırakılışını tıklama olarak iletmez. Açılış üstten, kapanış alttan 70 ms aralıkla 320 ms hareketleri başlatır. Geçiş başlangıcında konum/ölçek ve kart sırası yakalanır; yeniden çizim veya yön değişimi animasyonu eski konuma sıçratmaz. Konum/animasyon Core komutu değildir.
- **Güncel kutu oyunu tahtası:** dikey kişi kartları ve yedi alan (tek/emp/ana/craft/cook/train/rest). care/gather/watch/contact alanları kaldırıldı (KOS aşama 7: `watch`/`contact`'ın kabul bağları da söküldü; gözetmen yok). Eski kilitsiz plandaki bu görevler `PlanningBoardPresenter.RetireJobs` üzerinden kayıtlı AssignResident(rest) komutlarıyla bırakılır; kilitliyken kartlar Boşta gösterilip kapanış beklenir. Portreler sahnede stilize yer tutucudur.
- **Planlama panosu:** `PlanningBoardPresenter` (Bridge) → `GameController.ExecuteBoardCommand` → Core komutları.
  `PlanningBoardView`, `PlanningBoardCard`, `PlanningBoardSurface` ve `PlanningBoardDropZone` yalnız kartları çizer,
  seçme/sürükleme/bırakma olaylarını iletir. Her görev alanı `PlanningDropKind.Job` ve sabit Core görev kimliği taşır;
  kişi kartı bu alana bırakılınca `AssignResident` komuta gider (EĞİTİM alanı 2026-11-24'te kaldırıldı: `BoardLayout.PocketOpen("train")` hep false, `TrainingTopics` silindi; `TrainResident`/`board_train` Core'da durur ama kayıt 22 eski kayıtları yüklemediği için ölü yoldur (yalnız testler), `PlanningBoardPresenter.RetireJobs` eğitimdeki kişiyi Boşta'ya alır). Dolu alanın geçerli
  ataması iki kişinin görevini takas eder. Sürüklenirken kart ortak düzleme çıkar; Boşta kişileri ve depodaki teçhizat
  kaydırılan içeriğin altında maskelenir. Son konumlar görev alanlarının serileştirilmiş `CardAnchor` referanslarından
  ve Core durumundan hesaplanır. Eski serbest kişi konumları yüklenmez.
  Teçhizat kişi kartının üstüne bırakılınca `EquipResident` ile sahiplenilir; kişiyle taşınır. Ayrı `Inventory` alanı
  Depo'dur; buraya bırakmak sahipliği kaldırır. Tarif, girdi ve hurda hedefleri de açık tür/kimlik taşır. Boş panoya
  veya yanlış türde hedefe bırakmak komut üretmeden geri döner. Topluluk sicili ve pano üstünde vardiya kontrolü yoktur.
  Kişideki teçhizat sahnedeki `equipmentOffset` / `equipmentStackStep` ile üst üste yerleşir. Önceki serbest panonun
  `PlanningBoardLayoutStore` ve `planning_board_layout.json` verileri görev alanlı yerleşimi etkilemez; dosyadaki
  eski kişi/eşya ofsetleri okunmaz. Üretim kartları için `production:<kind>` anahtarları bırakılan serbest konumu saklar;
  stok ve rezervasyon bu dosyadan okunmaz. Dil/yeniden çizim ve kayıttan devamda bu konum kullanılır.
  Simülasyonun RNG veya komut geçmişi salt konum değişiminden etkilenmez.
  Drag sırasında `CanvasGroup.blocksRaycasts` kaldırılır ve kamera girdisi askıya alınır; bitince eski duruma döner.
  Bu uGUI kartları Aşama 10'da (T1c) silindi; çalışma anında hiyerarşi kurucu yoktur. Eski raf (`Shelf`) ve `Shelter controller` da silindi (T1a):
  `SiginakShelter` kodu durur, sahnede örneği yok; `CampaignWear.stages[0..2].objects` boş, `stages` dizisi kaymasın diye yerinde. Eski oda geliştirmeli TutorialFlow
  kapalıdır; ayarlar dolabı korunur.
- **Tarif defteri ve üretim (Revizyon 3 · Aşama 5, R18; karar D1-B):** tarifi oyuncu seçer. `RecipeBookView` (defter modeli; durum
  `BookState`: Closed, Opening, Open, Turning, Lowered, Closing) kancadan okuma pozuna gelir; sayfalar `RecipeBookPage` (World Space
  tuval, ışıksız TMP, içerik anlık görüntünün `Recipes`'inden). Açıkken `BoardView3D.SetBookMode(true)`: pano yüzü ve MASAYA DÖN dışındaki
  pano çarpıştırıcıları kapanır, tık defteri kapatır. Tarif kutusundan sürükleme `RecipeLifted` → sunucu kopyayı (`RecipeCardFace`)
  denetçiye verir; bırakma `BoardDropRules`: boş emre `board_queue`, dolu emre `board_cancel` + `board_queue`, formdaki kart dışarı
  `board_cancel`; hammadde fişi `board_reserve` ±1 (`ChipCount` ayrılanları sayar). Yetersiz stok: `CanPick` reddi → titreme + kırmızı.
  Form durumları ve not `ProductionForecast` (fişle aynı tahmin). Ürün kayışı ve YENİ etiketi (`NewItemTag`) sunum durumu
  (`BoardUiState.NewItem`, layout v3). Eski kayıttaki konmuş fişler devamda bir kez `board_stage(kind, 0)` ile stoka döner.
  Tesis aşama 9: defter `BoardRecipes`'in uzunluğunda (en çok 13 sayfa); açılmamış ekipman tarifleri kapalı sayfa (`RecipeData.Locked`;
  `RecipeBookView.RecipeZone` kutuyu `Page` sayar, sürüklenmez). Sayfada üç girdi satırı (`RecipeBookPage.InputRows`) ve erzak satırı,
  tarif kartında üç sütun ve üç kutu (`RecipeCardFace`). Depoda yedi ekipmanın ikinci sırası (`BoardLayout.GearKinds`, `Gear(i)`;
  `BoardSnapshot.Gear`, `BoardView3D.RenderStore`, `StoreView`), yalnız tesis kipinde; kart yüzü piktogramı adla (`ItemCardFace.GearPictogram`,
  `DeskSprites` `picto_<tür>`). Metinler `FacilityBoardText`; tahmin `ProductionForecast.NoSupplies` ve eksik kit (`KitFirst`).
- **Vardiya kapanışı (Revizyon 3 · Aşama 4, R19; kartlı oyun):** `SiginakShiftClose` (Bridge, `SiginakRoom/Shift controller`) akışın
  sahibidir; `SiginakShift` kartlı dalda ona devreder, eski yol (`ShiftSequence`, `Poses/K6a`) aynen kalır.
  - **Şalter:** `ShiftSwitch` R19 kipi (`shiftClose`; dolabın ve menünün şalterleri eski kipte): `ShiftPhase` (hazır · kapak kalkık · fiş
    bekliyor · kilitli · ret), süreler token'dan, `Advance(dt)` kendi saatiyle (EditMode testleri sürer). Plakanın 2. satırını ve üzerine
    gelme satırını yalnız `ShiftPlate` yazar. Kilit nedeni `SiginakShiftClose.LockReason` (saf): baskın > harekât bitti > belge açık > rapor
    açık > `BoardPlanCheck`.
  - **Fiş:** `ShiftReceiptMachine` kol inişini bekler, `ShiftReceiptPaper` (durumlar `Where`: Hidden, Feeding, Tongue, ToHand, Hand,
    Dragging, ToTray, Tray, Returning, Reading) `LateUpdate`'te yürür; el pozu canlı kameradan (`HandPose`, saf). `EvrakTray` hedef ışığını,
    bırakma pozunu ve dosyalanmış kopyayı tutar. Fiş yüzü `HandoverSlipFace` (World Space tuval, 1 birim = 1 mm, ışıksız TMP);
    veri `HandoverSlipFactsBuilder.Build(sim)` (saf, gizli gelecek okunmaz) → `HandoverSlipFacts` → `HandoverSlipFormat` (o anki dil).
  - **Komutlar:** kolun ikinci tıkı `PrintBoardShift` (`board_print`); EVRAK'a kabul edilen bırakıştan sonra tam karanlıkta
    `FileBoardShift(id)` (`board_file`). Okuma, el ve geri dönüş komut yazmaz. Kayıttan devamda `GameLoaded` → basılı fiş dile, şalter
    bekleme hâline anında.
  - **Arşiv:** son 5 fişin facts'i `liberation_desk.json`'da (`SiginakDeskStore`); EVRAK'a tık son fişi salt okunur gösterir. Kartlı
    oyunda Revizyon 2 raporu EVRAK'a eklenmez.
  - **Kamera ve girdi:** `SiginakPose.ShiftClose` (K6). Eldeyken masa kilitli; Esc ve sağ tıkı `SiginakShiftClose.Update` kendisi okur
    (`DeskInput` ve rig o sırada ulaştırmaz). Sesler `ShiftSound` → `Sfx` 107–114.
- **Kapı ve kabul (Revizyon 3 · Aşama 6, R9 / R17; kartlı oyun):** `SiginakAdmission` (Bridge, `SiginakRoom/Admission controller`)
  akışın sahibidir; `SiginakDoor` kartlı oyunda ona devreder (`Delegated`), eski kapı yolu (`Sim.Board == null`) aynen kalır ve akış
  hiçbir görünümü bağlamaz.
  - **Veri akışı:** Core → `AdmissionSnapshotBuilder.Build(sim, ui)` (Bridge, saf; RNG çekmez, komut yazmaz) → yerelleştirilmiş
    `AdmissionSnapshot` (`CandidateGroupData`/`CandidateCardData`/`PerkRowData`/`ReplaceRowData`, `StampData`, `AdmissionWords`;
    `Runtime/Admission/AdmissionSnapshot.cs`) → `AdmissionStation.Bind(s, animate)` → `CandidateGroupView` (kartlar
    `CandidateCardFace`, grup fişi `CandidateGroupSlipFace`; `AdmissionPaperFace` tabanı: World Space tuval, 1 birim = 1 mm, ışıksız
    TMP) ve iki `StampDecision`. `Runtime/Admission` `Loc` çağırmaz; metin anlık görüntüyle gelir. Kilitler yalnız Core
    denetimlerinden (`CandidateChoiceCheck`, `CandidatePassCheck`). Kişi dosyası `PlanningBoardSnapshotBuilder.Folder` (KAYIT, KARAR,
    olay satırları, `TagText` → `FileTagView`).
  - **Akış:** görünüm olayları (`station.CounterClicked/PaperClicked/ReplaceClicked`, damgaların `Pressed`) → `PressStamp` → denetim
    null ise tek `ChooseCandidate(aday, yollanacak)` (`candidate_choose`) ya da `PassCandidates()` (`candidate_pass`)
    `GameController.ExecuteBoardCommand` üstünden; ret komutsuz `Deny`. Yollanacak kişinin işareti sunum durumudur (`AdmissionUi`,
    kayda ve yan dosyaya girmez). Komut sırasında gelen `Changed` ertelenir, akış kendi sırasıyla çizer. Vuruş canlı oyunda grup
    anahtarı değişince bir kez (`announcedKey`; `Restore`/yeni oyun eşitler). Karar dizisi `DecisionTimeline` (token'dan): KABUL'de
    kamera K8, tezgâh katlanır, kapı `DoorProp.SetOpen(…, admitWheelS, admitLeafS)` ile açılıp kapanır; GERİ ÇEVİR istasyonun
    dizisi. Dizide rig kilitli; Esc ve sürüklemesiz sağ tıkı akış kendisi okur ve diziyi sona atlatır. Teleks "KAPI" satırları
    kapanış teleksinden sonra (`SiginakDesk.ShiftFiling`). Sesler görünümlerin `Sound` olaylarından `CueOf` → `Sfx` 120–122,
    127–128 (yer tutucu; 123–126 aşama 7'de söküldü; damga 63, dönüş 99, ret 100, kapı 50/51/62).
  - **Pozlar ve geri:** `SiginakPose.Admission` 8 (K8A), `AdmissionRead` 10 (K8R: çapa `Poses/K8R` her okumada
    `station.ReadPose(kâğıt, sıra, admission.readDistanceM)` ile kâğıda göre kurulur). `BreathGlass` 9 (K8G) emeklidir: sayısı
    `PoseLightRecipe`'te ve çapası sahnede durur, hiçbir yol odaklamaz, K8 ailesinden çıktı. Geri zinciri: elde damga → dizi → K8R →
    K8A → K8 → K1; MASA tuşu doğrudan K1. K8 ailesinde kenar satırı gizli, kapı hedefi `BlockedOverride` ile susar.
  - **Sahne:** `SiginakRoom/Admission station` (istasyon kökü; aday grubu ve damgalar onun altında, tıklar ata zincirinde
    istasyona/görünüme çıkar). Damgalar rafın orta yuvalarında (KABUL `Slot2`, GERİ ÇEVİR `Slot3`); rafın yuva dipleri gövdenin
    ahşabıyla boyalı (aşama 7: boş dış yuvalar görünmesin; model hâlâ dört yuvalı). Kapıdaki buğu camı ve interkomun gövdesi durur,
    etkileşimleri (bileşen ve çarpıştırıcı) söküldü. Kâğıt prefab'ları `Prefabs/Rev3Admission/` (aday kartı, grup fişi). Bağlar
    `Configure*` ve `[SerializeField]`; kayıtlı hâl katlı tezgâh. `PlanningBoardPresenter.ShowAdmissions`/`CloseAdmissions` →
    `admission.Enter()`/`Leave()` (public API korundu).
- **Kapanış (eski açıklama; kartlı oyunda yukarıdaki akış):** `ShiftReceiptMachine`/`ShiftReceiptPaper` masadaki kapaklı vardiya şalterini (`Shift switch`) sürer.
  Kapağın kalkması basım değildir; kolun inmesi (`ShiftSwitch.Thrown`) `PrintBoardShift` ile fiş üretir ve fiziksel
  kâğıt teleks daktilosunun silindirinden basılır (kamera K1'e döner; önce teleksin o anki satırı biter: `ShiftReceiptMachine.FeedHold`,
  `SiginakShiftClose.HoldFeed`; tek kâğıt akışı: fiş beslenince o anki teleks kâğıdı düşer, fiş aynı yerden teleks kâğıdının boyunda (260 × 225 mm) çıkar ve dosyalanana dek silindiri tutar, `ShiftReceiptPaper.HoldsPlaten` → `TelexFeed.PlatenBusy`; o sırada gelen teleks satırları şeritte; yeni teleks satırında eski kâğıt düşer, yenisi alttan yazılarak çıkar, `Teleprinter`/`PlatenFeed`; K6'dan daktiloya dönerken kamera K1'e döner). Fiş beklenirken `ShiftSwitch.SetAwaitingReceipt` şalteri inik ve kilitli
  tutar; durum satırı şalterin pirinç plakasının ikinci satırıdır (`kapağı kaldır` / `fişi EVRAK'a koy`). Kâğıt mevcut `EvrakTray`
  kutusuna bırakılınca karartma sırasında `FileBoardShift` çağrılır. Gün Core'da bir kez ilerler; fiş kimliği ve plan
  kilidi kayıttan geri kurulur. Pano presenter'ı kapanış yapmaz. Kutunun eski arşiv eylemleri korunur. Kabul kapıdaki
  kabul istasyonundadır (aşağıda).
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
  dikey 48°), kapak kamera varınca açılır; kapanınca önceki poz. Emir belgesi açıkken belge gizlenir
  (`BriefingView.SetVeiled`); dolapta gün 0'da dil değişirse oyun dolap kapanınca yeni dilde kurulur
  (`BridgeSettings.RebuildIfPending`). Aşama 8: model pivotları `PartDriver` üzerinden sürülür; menü dolabı ayrı
  `SettingsCabinetMenu` prefab'ıdır (kör kapak, MENÜYE DÖN yok). İki sahnedeki dolap da sahne override'larıyla aynı `SM_PowerBox1`
  kutusudur, kapak −160° (prefab'lardaki −95° / −120° Rev3 dolabının değerleri). `LiberationDeskView.MenuExitBlocked` dolabın kendi
  odağı dışındaki kâğıt/işlem kilitlerini okur; `SiginakCabinet` menü koluna neden ve mühür bandı uygular. Aşama 10: `SiginakCabinet.radio` K9'da gizlenir (anten açılan kanadın yayına giriyor), başka poza geçişin başında geri gelir.
  `ShiftSwitch.lockedFeedback` kilitli tıkı yalnız titreme ve sesle yanıtlar. MENÜYE DÖN: kayıt, `OpeningSequence.FadeOut`, `Giris` sahnesi (build'de yoksa çıkış).
- **Bağlayıcılar** (Bridge/Siginak): `SiginakDesk` (sayaçlar, anahtarlar, şalter), `SiginakMap` (kâğıt harita, görev ve
  cephe fişleri, ekip figürü), `SiginakShelter` (eski maket ve oda kartları; Aşama 10'dan beri sahnede örneği yok, eski yol kodu olarak durur), `SiginakCrt` (telsiz hattı, `IRaidTransition`),
  `SiginakShift` (kartlı oyunda panoya/basıma yönlendirir; eski vardiya dizisi, vardiya raporu, evrak tepsisi, karşı saldırının alarm lambası ve harita ✕'i; raporu saf C#
  `ShiftReportBuilder` kurar, renk kuralı `ReportTones`), `SiginakBriefing` (açılış ve emir belgesi),
  `SiginakDoor` (kartlı oyunda kapıyı `SiginakAdmission`'a devreder: KAPI/4/K ve kapıya tık → `admission.Enter()`, K8 ailesinde kapı hedefi susar; eski kapıda vuruş, kamera, kâğıdın evreleri, `LiberationDeskView.AnswerDoor`; kâğıdı saf C# `DoorViewBuilder`
  kurar (kart → siluet tablosu da orada), `DoorCardView` çizer, kapıyı `DoorProp`, ziyaretçiyi `VisitorSilhouette`
  canlandırır; ölümcül kararda oda ışığını kırmızıya çevirir ve son raporu `DeskBridgeState.HoldEnding` ile kâğıt
  kapanana kadar bekletir),
  `SiginakTutorial` (kartlı başlangıç için `PlanningTutorialFlow`, eski oda yolu için `TutorialFlow`: sahne kökünde kendi tuvali `SiginakTutorial`, sıralama 70, baskın HUD'unun üstünde; odanın
  dışında olduğu için baskında da çalışır; adımlar saf C# `TutorialFlow`, not `TutorialNoteView`; senaryolu ilk baskın
  `TutorialRaid`, rehberi `RaidGuide` ve `RaidBoard3D.Marks.Guide` kayıt 21'de silindi: hiçbir kipte ulaşılamıyordu). Modelleri saf
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
- **Sunucu ödülü** (tesis aşama 9; `SiginakServerReward`, `Desk/Scripts/Bridge/Siginak/`; veri `FacilityRewardText`): bölgeyi
  tamamlayan baskının raporu dosyalanınca (`SiginakOperation` → `Present`) ödül kâğıdı kapıdan gelir: kapının yanındaki panoya iner
  ve kapı vurur (`Docs/KAPI-BELGELERI-VE-YZ-MEKANLARI-TASARIMI.md` §A; olay tutanağı da aynı yoldan gelir). Kâğıt sığınak olayı tutanağının
  görünümünün (`ShelterEventPaper.prefab`, `ShelterEventPaperView`) DeskGame'deki ikinci örneğidir ("Sunucu ödülü", EVRAK tepsisinin
  ikinci çapasında); denetleyici "Sunucu ödülü denetleyicisi" nesnesinde, `LiberationDeskView`, `SiginakDesk` ve `SiginakOperation`'a
  `serverReward` ile bağlı. Karar tek komuttur: `GameController.ExecuteBoardCommand(s => s.ChooseServerReward(bölge, seçim))`
  (`server_reward`). 2026-11-23'ten beri kâğıt kapının yanındaki çift taraflı belge çekmecesine gelir (`SiginakRoom/Belge çekmecesi`,
  `PassDrawer` + köprüsü `SiginakPassDrawer`; tasarım §A.0): çekmece açılınca tabladaki kâğıt alınır, Esc tablaya geri koyar, karar
  verilince kâğıt tablaya konur (`ShelterEventPaperView.Where.Outbox`) ve şalter çekilince gizlenir (yalnız sunum, kayda girmez). Yeni
  belge çekmece kapalı ve boşken gelir (`RequestDelivery`). Çapasız sahnede eski tepsi yolu sürer. Kayıttan devamda kâğıt animasyonsuz
  çekmecededir, çekmece kapalıdır (`Where.Door`). Eldeyken masa
  kilitli (`LiberationDeskView.RewardBusy`). Bekleyen ödül kilidinin bütün yüzeyleri (`RaidPaperFacts.DisabledReason`,
  `TelexReasons`, `CrtViewBuilder`, `RadioLogBuilder`, `OperationReportBuilder`) tek yardımcıyı okur: `FacilityRewardText.Pending`
  (bayrak, kartlı oyun, `Ending == null`, baskın yok).
- `DeskInput` klavyenin tek sahibidir: ayarlar dolabı açıkken Esc (dolabın kendi Tab/okları ayrıca); son raporda Enter; aksi hâlde 3 planlama, Esc
  ayarlar/geri. Odanın anahtarları (`StationKeyRail`: SIĞINAK, KAPI, MASA; üst kenar şeridi `EdgeLine` aynı sıra;
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
Entrance             Landing (koridorun malzemeleriyle genişletilmiş sahanlık, merdiven, üç ana ışık + gölgesiz kart dolgusu),
                     Bunker door (DeskGame'deki kapının kopyası: dış yüzde çark, “07”, SIĞINAK 07, gözetleme halkası;
                     DoorProp dıştaki çarkı döndürür, içeriden sızan ışığı yakar), Inside (karanlık iç, sıcak ışık),
                     Intercom outside, Title root (“SON ŞALTER” / “CORRIGIBLE”), Settings cabinet (SettingsCabinetMenu prefab; kör kapak), Time clock (TimeClock: puantaj saati ve kart rafı), Main switch (ShiftSwitch: son şalter),
                     Poses (M0 sahanlık, M2 saat, yükseltilmiş M2, MS ana şalter, M3 eşik ve içerisi), Main Camera (EntranceCamera, PhysicsRaycaster)
EntranceUI           Overlay 45: HoverTag (DeskGame'deki etiketin kopyası), Fade (Overlay 100, siyah örtü)
Audio                AudioDirector (game boş)
Entrance controller  EntranceController, BridgeSettings (veri kaynağı yok)
```

- `EntranceController` (Bridge/Entrance): karanlıktan açılış (lambalar iki kırpışmayla), kayıt özeti
  (`SaveStore.Load` → `BriefingBuilder.Summarize`, arka iş parçacığında; Core saf C#) karta yazılır. Karta veya saate tıklamak
  kayıttan devam, boş deste yeni harekât (kayıt varsa kıskaç iki adım: `TimeClock.Armed`, 4 s). Seçilen kart saate basılır
  (`TimeClock.Punch` → `Stamped`), kapının zili, kapı açılır, kamera eşikten geçer (`EntranceCamera.Walk`), ekran kararır;
  DeskGame `LoadSceneAsync` ile çark bittikten sonra kanat açılırken yüklenmeye başlar; yürüyüşün son 0,3 s
  kararması ve en az 0,2 s tam siyah ardından etkinleşir. Girişte Esc dahil tüm girdiler kilitli ve imleç gizlidir. Niyet `GameLaunch.Pending`
  (Game, statik). Son şalter (`ShiftSwitch`: kapak + kol) lambaları söndürür, `Application.Quit` (editörde Play durur).
  Esc dolabı kapatır, kıskacı bırakır ya da saatten sahanlığa döner; oyundan çıkarmaz. `DeskTarget.Blocked` menüde
  geçişler ve dolap için kurulur.
  `EntranceInstruments` zil tokmağını, lamba ışımasını ve gölge geçişini yönetir. Saat kartında dört satır ve en çok
  80 bitmiş vardiya deliği vardır. Paket OBJ UV'leri için sahne ağ kopyaları `Art/Rev3/SceneMeshes/Menu`, baskı dokuları
  `Rev3TextureBaker.BakeStage8()` içindedir. R1–R4 ve kamera kalibrasyonu: `Docs/UI_Siginak_Rev3/A8_DOGRULAMA.md`.
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

- **Rehberin işaretçisi (2026-11-24):** `TutorialTargets.For(adım, bağlam, baskın, seçili)` (Bridge, saf) adımın hedefini seçer:
  sahnedeki `TutorialAnchor` kimliği (DeskGame'de 9 masa hedefi; `RaidHud.prefab`/`RaidPanels.prefab`'ta 6 baskın hedefi; kimlik
  listesi `TutorialTargets.DeskAnchors/RaidAnchors`), pano alanı (`BoardView3D.PocketCorners`: görev cebi, "bosta" tepsisi, "depot"
  satırları; pano dünyada dönük olduğu için dört köşe izdüşürülür) ya da baskın karesi (`MoveHint`, `GateTile`, `CrateTile`,
  `IntentTile`; tahtanın kamerası, `RaidDataSource.Board`). `SiginakTutorial.UpdatePointer` dikdörtgeni `TutorialPointerView`'e
  verir (rehber tuvalinde, notun altında; dört kenar ve kâğıt etiket, `ui.focusRing`; ışın almaz). Not hedefin üstüne düşerse
  `TutorialNoteView.SetLeft` karşı köşeye kaçar. "İlk kez" notları `PlanningTutorialFlow.FirstNotes` (konu koşullu; görülenler
  `Corrigible.PlanningTutorialFirstNotes`). Adım `Corrigible.PlanningTutorialStepId` ile kimlikle saklanır. İlk baskın bitene dek
  `SiginakTutorial.SealsShift` vardiya şalterini mühürler (`SiginakShiftClose.LockReason(…, tutorialSeal)`, masa üzerinden
  `SiginakDesk.Tutorial`).
- **Kartlı başlangıç rehberi:** `SiginakTutorial`'ın serileştirilmiş `planningBoard`, `shiftClose` ve (KOS aşama 8) `shelterEvent`
  bağları. Aşama 8'de akışa konu adımları eklendi (`PlanningTutorialStep.Topic`): ilk vardiyadan sonra `PlanningTutorialFlow`
  bekleme adımında durur, kapıda grup ya da açık tutanak belirince onun adımını seçer; "bir kez karara bağlandı" bilgisi
  kayıttan türetilir (`Admission.Passed`/kurucu olmayan dosya, `event_decide` komutu), devamda da doğrudur. Yer tutuculu
  metinler (`BodyText`, `GoalLine`) simülasyonu yalnız okur. `SiginakLastSwitch` rehber sürerken `Active`'i okuyup mühürler; İFŞA ve teleks metinleri de mühürden haberdardır
  (`SiginakDesk.TutorialSealsSwitch` → `ShelterEventSnapshotBuilder.SwitchOpenByShift(sim, tutorialSeal)`): rehber sürerken
  "konum çözüldü" son şalteri önermez. Konu bölümünde not sayacı "REHBER · KONU" yazar. Yeni,
  salt okunur
  `PlanningTutorialContext` → `PlanningTutorialFlow` → mevcut `TutorialNoteView`. `GameController.NewGameStarted` yeni akışı,
  `GameLoaded` yalnız aynı tohumun kayıtlı rehberini açar. İlk `Start` bir kare bekler: menünün DEVAM kararı ve tüm Start'lar
  işlenir. Rehber tercihi ancak emir belgesi kapandıktan sonra yazılır. İlerleme ayrı PlayerPrefs anahtarlarıdır; rehber
  komut yazmaz, `GameRecord`'a ve kayıt sürümüne (17) girmez. `FixedSquad` yalnız eski oyun yolundadır. Panel
  yalnız SONRAKİ/EĞİTİMİ BİTİR düğmelerinde ışın alır; metin ve zemin alttaki masanın tıklarını yutmaz. İçerik aynıysa
  her kare metin/boy yeniden yazılmaz. Baskında sağ üstten 310 px aşağıya taşınır; geçiş, açılış, dolap ve dosyalama
  sırasında gizlenir.

- `RaidLauncher` (DeskGame'de, Game nesnesi): `S.Raid` oluşunca önce kamerayı CRT'ye daldırır, sonra sahneyi
  `LoadSceneMode.Additive` ile yükler; odanın kamera, dinleyici, canvas ve girdilerini kapatır (`hideDuringRaid`: nesneler
  değil bileşenler). Sahne kapanınca önceki `enabled` durumlarını geri yükler. Yolu `IRaidTransition` kancası sürer
  (`SiginakCrt.Approach` → `SiginakCameraRig.BeginDive`): kamera geri çekilme pozuna (`Poses/CrtBack`) doğru çıkar, CRT'ye
  döner ve `CrtDive`'a yaklaşır; tek kesintisiz hareket (`motion.crtDiveS`), vuruşlar yolun kesirleri olarak üst üste biner
  (`diveBackSpan/diveTurnSpan/divePushSpan`, smootherstep ağırlıklar; yol ortasında durma yok). Dönüşte
  kamera monitörde kalmıştır: `Depart` → `BeginReturn` aynı yolu tersinden yürür (`crtReturnDelayS` bekleyip), bitene kadar
  `IsTransitioning` doğrudur (masa kilitli, operasyon raporu kamera varınca iner). Boşluk ya da tık dalışı ve CRT dizisini,
  dönüşte de kamera yolunu atlar. Kanca yoksa eski düz yaklaşma ve anında dönüş.
- `LiveRaidDataSource`, DeskGame'deki `GameController` ile emrin oda oturumunu kurar (aşağıda). Oyuncu sonuçtan rapora
  geçtiğinde `GameController.ResolveRaid` çalışır ve sahne kapanır. Baskın boyunca odanın `DeskInput`'u askıya alınır. Sahne
  tek başına açılırsa `Baskin.Create` ile örnek bir sunucu baskını kurulur (MapSeed 0, `Layouts`, varsayılan kit).
- **Tesis baskınının sunumu (oda zinciri; `Docs/TESIS-BASKINLARI-TASARIMI.md` §11.5):** emirde kanadın üç odası yazılıdır
  (`FacilityRaidSession.Applies`; değilse hata ve sahne kapanır); `LiveRaidDataSource` bir `FacilityRaidSession` kurar (Bridge, UnityEngine'siz): sıradaki odayı
  `Simulation.OpenRaidRoom` ile açar, biten odanın raporunu kurucuya verilen `advance` ile dosyalar (oyunda
  `GameController.AdvanceRaidRoom` → `raid_room`), sonucu raporlardan okur; kendi durumu yalnız açılan oda ve açılıştaki kırılan
  sivil toplamıdır (güven çift sayılmasın), gerisi simülasyondan. Oyuncu metinleri saf `FacilityRaidText`'te (gözcü raporu,
  oda kapanış ayracı, düşenler, temiz oda, yürüyerek çıkış, amaç ve geçit satırları, kare öneki "2-C3"). `RaidDataSource`
  odanın bitebildiği her yerde (hamle, yetenek, kapıya giriş, makine evresi, geri çekilme, GEÇ) `CheckRoomEnd` çağırır: rapor
  dosyalanır, zincir sürüyorsa `ResetBoard(first: false)` ile sıradaki tahta kurulur (sahne yeniden yüklenmez; `OpenState`
  yalnız ilk açılışta), son oda raporlandıysa sonuç kartı gösterilir. Herkes kapıdayken `RaidViewModel.CanPass` EMRİ İLET
  düğmesini GEÇ yapar. Kayıttan devam içinde bulunulan odayı baştan açar; sonuç kartında çıkılmışsa kart gösterilmez,
  `ResolveClosedRaid` sonucu yazar. `RaidBoard3D` paftayı kaydırır (tesis sürüm 10; `PageOut(dir)`/`PageIn(dir)`: biten paftanın
  kökleri kullanılan geçidin tersine kayıp çıkar, yeni pafta geçidin yönünden gelir; yön `RaidBoard3D.GateDirection(Baskin.GateTiles)`,
  `slideSeconds` 0,45, `slideDistance` en az kamera görüşü; sahnedeki `BoardBase` `slideWith` ile dünya uzayında birlikte kayar ve
  `BoardBackgroundView.boardBase` gölgeyi ona bağlar; kayarken emir iğnesi gizlenir; `Build` önce `ResetPagePose` çağırır), kapıdaki birimi çizmez,
  donanımı `RaidPieceLibrary.fusePanel/antenna/valve` (boşsa soğutma ünitesi) ve PANO/ANTEN/VANA etiketiyle çizer. Örnek
  tahtada (sahne tek başına) oturum yoktur ve her yeni dal kapalıdır (`Session == null`, `State.Room == null`).
- **Tesis bina planı (aşama 5; §7, §11.5):**
  - **Core:**
    - `LiberationCampaignState.Facility` (`FacilityState`: `Known`, `Intel`).
    - Teyp `AdvanceRaidRoom`'da, evrak `ResolveLiberationRaidCore`'da.
    - Dinleme `AdvanceFacilityIntel` (`AdvancePlanningBoard`, `AdvanceBoardCare`'den sonra) ve `SpendFacilityIntel`
      (`SelectLiberationRegion` de çağırır).
    - Okumalar: `FacilityKnown`, `FacilityIntelTarget`, `FacilityPlanCells` (odanın makinesiz plan tahtasının kopyası).
    - Hiçbiri RNG çekmez ve komut eklemez.
  - **Görev fişinin yanındaki paftalar:** `FacilityPlanBuilder` (Bridge, saf) → `FacilityPlanData` (Runtime) →
    `FacilityPlanSheets` (fişe yaslanır, fişle kayar ve kapanır) → `FacilityPlanSheet` (altı `UILineGraphic` katmanı; aşama 10'da
    yedincisi `counter`, önlemlerin kırmızı kalemi, `plan.counter`;
    geometri `FacilityPlanSketch`'te). Sahnede `SiginakHud › Facility plan`, kadro raporunun önünde; bağ `SiginakMap.plans`.
  - **Gözetim cebi:**
    - `BoardLayout.WatchJob/PocketOpen/BostaZone`, `BoardSnapshot.WatchOpen`, `BoardView3D.watchParts`.
    - Deste bayrağa bağlı: `BoardLayout.Deck(watchOpen)` (980 / 1098 mm), `HandStep`, `DeckClosed`, `HandOpen` ve
      `DeckMotion` aynı parametreyi alır; `BoardView3D` her çizimde `BostaDeckView.WatchOpen`'ı yazar ve tepsinin
      çarpıştırıcısını (`deckTray`) kaydırır. Sahne cepsiz yerde kayıtlı; kayıt 21'den beri oyunda değer hep açık (cepsiz
      geometrinin sadeleştirmesi ertelendi).
    - `BoardDropRules.WatchOpen` (`sim?.B != null`; denge yokken "watch"a komut yok, `PlanningBoardPresenter.RetiredJob`'ın
      "watch" kolu oyunda artık çalışmaz); metinler `FacilityWatchText`.
    - Sahnede `PlanningBoard/Body` altında `Slot_gozet`, `TXT_P_gozet`, `Gözetim bandı`, `Pano yazıları` altında
      `Şerit · gozet · 1..3`; hepsi kapalı kayıtlı.
  - **Telsiz masası:** `RaidHud.prefab › Corners › Radio desk` (`RaidHudView.radioDesk/onAirLamp/tapeDeck`), yalnız
    `vm.BlueprintWing > 0` iken.
    - `RaidOnAirLampView` `vm.OnAir`'i okur (`RaidDataSource.onAir`: emir ve GEÇ ile yanar, ilk değişen rapor satırında
      söner).
    - `RaidTapeDeckView` `vm.TapeSheets`'i okur (`RaidDataSource.TapeSheets()`: bu baskında dosyalanan ve bilinen odalar).
  - **Tokenlar:** `tokens_siginak.json › colors.plan.*`, `stamp.noPlan`, `board.layoutMm.*.gozet`; `raid_tokens.json › radioDesk.*`.
- **Tesis hamleleri (aşama 7; `Docs/TESIS-BASKINLARI-TASARIMI.md` §4 "Uygulanan hâl (aşama 7)"):** sahne ve prefab değişmedi;
  hamleler mevcut HUD'a çalışma anında bağlanır. Örnek tahtada (`State.Room == null`) her yeni dal kapalıdır.
  - **Girdi ve hamle kipi:** `IRaidCommands.ArmAction()` (E) ve `ChooseAction(int)` (kart satırı tıkı). `RaidHudView.LetterKeys`
    harf tuşlarının tek tablosudur: Q yetenek, **E hamle**, Z geri al, L günlük. Kip `RaidDataSource`'ta tutulur (`ActionKind`,
    `ActionOther`): E kullanılabilir seçeneklerde döner, sonuncudan sonra kapanır; Q, Esc, sağ tık, seçim, Z ve başarılı hamle
    kapatır.
  - **Model:** `RaidViewModel.ActionRows` (`RaidCardRow[]`), `ActionKey` (E'nin kuracağı satır, −1 yok), `ActionArmed`,
    `CargoOnFloor` (görev çipinin YERDE damgası). Örnek tahtada boş/−1/false.
  - **Kart:** `UnitCardView` hamle satırlarını prefab'da tutmaz. `EnsureActionRows`, Row 1'i çalışma anında kopyalar;
    `Mirror<T>` şablonun `[SerializeField]` referansını kardeş sırası yoluyla kopyada bulur (`Find` yok). Kopyalar havuzda
    kapanır; kopya tıkı `ChooseAction(i)`. Test erişimcisi `ActionRowCopies`.
  - **Kaynak (`RaidDataSource`):**
    - `ArmAction`, `ChooseAction`, `UseAction` (hedef tık → `Baskin.DoAction`) ve `ActionRoutine` (sürüklemede `AnimateWalk`,
      menfezde yakın uca yürüyüş, sonra `Animate`; kapı ya da çıkışta `AnimateLeave`). Yürüyüşte yük alımı `Carrier`
      farkından (`PickCargo`).
    - `MarksFor(vm)` public: tahtanın işaretleri görünüm modelinden kurulur; `DrawPreview` hamle önizlemesini çizer.
    - Yürüyüş yolu (2026-11-24): fare bir yürüyüş karesindeyken `Publish` `Baskin.MovePath` (Core; `WalkPath` + kişinin menzili,
      `Reachable` ile aynı kural, salt okuma) ile `vm.MovePath`'i doldurur; `DrawPreview` onu `RaidBoard3D.SetMovePath`'e verir (kırık
      çizgi `RaidArrow.SetPath`, köşeler dolu, renk `lib.move`, `lib.overlay`; ÇIKAR/KAPIYA/−N etiketli dallarda düz ok yerine).
      `Move` yolu `DoMove`'dan önce alır, `MoveRoutine` parçayı `AnimateWalk` ile aynı yoldan yürütür (duvarın içinden kaymaz).
    - `Machines`, ilk `ambush-hit`'te evreyi böler: saldırılar → `AnimateMachineMoves(after, alsoDead)` → `Animate(…, indexBase,
      total)`; iki parça da `EffectPlaying`'e tam listenin sırasını ve uzunluğunu bildirir.
    - `OnEffect`, `ambush-hit` ve `cargo-drop` için sunum kopyasını (`machineBefore`) günceller; evre içinde taşıyıcı, YERDE
      damgası ve amaç satırı bu kopyadan okunur.
    - Son nefes: oda bu evrede bittiyse `Breath` taşıyan gövdenin parçası evre sonunda ekiple kalkar; ekip kartının
      `BodyText`'i Core'un oda raporu kuralını izler (ÇIKARILDI / GÖVDE KAPIDA / EKİPLE TAŞINIYOR / YERDE · kare).
  - **Saf metin ve veri** (Bridge, UnityEngine'siz):
    - `FacilityMoveText`: seçenekler (`Options`, `Relevant`, `JointWorks`), ad, ayrıntı, neden, etki, fişler (`Slip`,
      `PickupSlip`, `PassSlip`, `EffectSlip`), hover (`BodyAt`, `TileTitle`, `TileRows`), gövde yardımcıları `Lying` (taşınmazsa
      ölecek gövde) ve `GoesWithTeam` (son nefes).
    - `FacilityMoveView.Preview`: önizleme verisi (`MovePreview`: `MoveStroke` çizgileri kare koordinatında, renkler anlam
      anahtarı "good"/"ink"/"graphite"/"threat"; `MoveGhost`, `MoveLabel`, odak karesi).
    - `FacilityRaidText`: gözcü, kapanış, amaç, notlar (SANDIK, TUTULUYOR, YÜK), `GateRows` ("Yerde kalan", "Yük yerde kalır")
      ve `CargoLeftRow`.
  - **Tahta (`RaidBoard3D`):**
    - `Marks.Ambush` (hat, sahibi tahtada değilse soluk), `Marks.Cargo`, `Marks.Focus`; `Sync` sonunda `SyncFacility` çizer.
    - `AddPreviewStroke`/`AddPreviewGhost` (`ClearPreview` siler).
    - On yeni Fx dalı (`ambush`, `ambush-hit`, `rush`, `drag`, `throw`, `hold`, `open`, `vent`, `cargo-pick`, `cargo-drop`),
      `AnimateWalk(id, path, followerId, followerEnd)`, `PickCargo`.
    - Test erişimcileri: `AmbushLineCount`, `CargoShown`, `FocusShown`, `PieceTile`.
    - `RaidPiece.Init()` tembeldir; düzenleme kipinde Awake çalışmaz.
  - **Operasyon raporu:** `RaidPaperFacts.MaterialGains` büyük sandığın ekini sayar; `OperationReportBuilder` sandık satırını
    sektör satırından sonra ekler; `OperationReportData.MaxResults` (7), sahnedeki `OperationReportView.rows` yuvasıyla aynıdır
    (sahne testi kilitler).
- **Tesis ekipmanı (aşama 9; `Docs/TESIS-BASKINLARI-TASARIMI.md` §5 "Uygulanan hâl (aşama 9)"):** örnek tahtada (`State.Room ==
  null`, `Class == null`) her yeni dal kapalı; kurallar Core'dan (`CanAction`, `ActionTargets`, `PreviewAction`, `DoAction`).
  - **Girdi:** `RaidHudView.LetterKeys`'e **R** ("gear" → `IRaidCommands.ArmGear`); `DigitKeys` 1–4 (kartın `UnitId > 0` ise
    `SelectUnitId`, değilse `SelectUnit`). `IRaidCommands.SelectUnitId/ArmGear/ChooseGear` varsayılan gövdeli (eski sahteler derlenir).
    Kip `RaidDataSource`'ta: `GearArmed`/`GearKind`; hamle ve ekipman kipi `Kip`/`KipKind`/`KipOther` üzerinden aynı `UseAction`
    yolunu kullanır; tek kip açık (E ekipman kipini, R hamle kipini kapatır).
  - **Seçim:** uzman harfle (`Selected`), köpek ve tutsak birim kimliğiyle (`SelectedOther`, `SelectId(id, cycle)`); harf araması
    (`Unit(char)`) ve sayımlar `Baskin.IsSpecialist`. Aygıt seçilmez.
  - **Model ve kart:** `RaidViewModel.GearRows/GearKey/GearArmed`, `RaidTeamModel.UnitId/Dog`; `UnitCardView` kopya havuzunda önce
    ekipman satırı, sonra hamle satırları (tık `i < gearCount ? ChooseGear : ChooseAction`). Kart 10 satırı aşınca katlanır
    (`MaxCardRows`, `actionRowMap`). Dördüncü ekip kartı `RaidHud.prefab › Corners › Team › Card 4` (280 px; kap `ContentSizeFitter`,
    `childForceExpandWidth` kapalı, sağ dolgu 7), `RaidHudView.team[3]`, yalnız köpek varken açık.
  - **Saf metin ve veri** (Bridge): `FacilityGearText` (seçenekler `GearOptions`/`UnitKinds`, ad, ayrıntı, neden, etki, fiş, kalan tur
    `CaptiveLeft`/`DeviceTurnsText`, hover `UnitRows`/`ObjectRows`, `FreeFloor`), `FacilityGearView` (aygıt hatları `TurretLine`,
    `WinchLine`, `PullEnd`, `DecoyPath`, havlama erimi; Core'un private kuralının kopyası, `FacilityGearViewTests` Core'un makine
    evresiyle eşler). `FacilityMoveText`/`FacilityMoveView` ekipman ve birim türlerini bunlara devreder.
  - **Tahta (`RaidBoard3D`):** `Marks.Devices` (aygıt hattı), `DecoyPaths`, `Bark` (erim karoları), `Tags` (uzman olmayan birimin
    etiketi ve kırmızı bayrağı); NİYETSİZ (planı atlanan) ve KİLİTLİ (kilitlenen) notları `MarksFor`'da. `Sync` yeni birimleri
    `RaidPieceLibrary`'nin kendi alanlarıyla kurar (`turret, decoy, winch, dog; wire, plank, scrap`; yoksa Teknisyen × 0,6 ya da enkaz),
    aygıtı `GadgetYaw(Dir)`'e çevirir, tesis odasında karo süslerini (`shutter`, `shutterOpen`, `crateNiche(Open)`, `holdJack`)
    koyar; kırılan engelin yerine enkaz koymaz. On altı Fx dalı `GearFx`'te; önizlemede `AddPreviewArea`. Çizgi kalınlığı ve alfa
    `RaidPieceLibrary`'de (`gearLinePx`, `decoyPathAlpha`, `barkAlpha`). Parça prefab'ları `Desk/Prefabs/Raid/Gear` (Analist
    mesh'leri + `Rev3MaterialLibrary`; yol E).
- **Önlem defterinin sunumu (aşama 10; `Docs/TESIS-BASKINLARI-TASARIMI.md` §6.3, §7.1, §7.3 "Uygulanan hâl (aşama 10)"):** bütün
  metinler saf `FacilityCounterText`'te (`Desk/Scripts/Bridge/Siginak/`; Core'un public okumalarından, kopya kural yok; defter kapalıyken
  hiçbir yüzey metin üretmez: `FacilityCounterText.On`).
  - **Görev fişi:** ÖNLEM satırı `SlipField.Alert` (kırmızı değer, mürekkep işareti); `LiberationMapBuilder.SlipFieldsMax` (5) dolunca
    KAZANIM ile birleşik satır (`MergeWithPerks`). Sahnede beş yuva kalır.
  - **Teleks:** ÖNLEM satırları `SiginakMap.PlayReveal` → `PostCountermeasures` ("BÖLGE ÖZGÜR"den sonra; `seenLearned` ilk bağlamada
    dolar, yalnız reveal sırasında ilerler). Sonuç teleksi `OperationReportBuilder.Telex`; `SiginakOperation`'ın yeni `[SerializeField]
    TelexFeed telex` bağı (= `SiginakMap.telex`), satır `Begin`'de kurulur, rapor inerken basılır (`telex != null` iken; kayıt 21'den beri ana bayrak koşulu yok).
    Telsiz günlüğü `RadioLogBuilder` "YZ önlemi · {ad}".
  - **Paftalar:** `FacilityPlanSheetData.Counters`/`CounterRange` (`FacilityPlanBuilder`), `FacilityPlanSketch.Counter`,
    `FacilityPlanSheet.counter` (üç paftada `drawing` altında `Counter`, `parts`'ın kardeşi).
  - **Baskın:** `RaidDataSource.GearMarks` CIVATALI etiketi; hover, neden ve gözcü satırları `FacilityCounterText`'ten; `FacilityGearView`
    vinç hattını Core'un `Baskin.WinchPulls`'undan, süreleri `DeviceTurnsFor/ClampTurnsFor`'dan okur. `RaidBoard3D` beş yeni Fx dalı
    (`bolted`, `unbolt`, `dwall`, `cut`, `jam`; yeni ses yok). Yayıcı parçası `RaidPieceLibrary.scentEmitter` (+ `emitterFallbackScale`)
    → `Desk/Prefabs/Raid/Gear/ScentEmitter.prefab` (kurgu, yol E); küçük resim `thumb_emitter` (`SiginakTextureBaker.PieceThumbSpecs`,
    `SiginakSpriteManifest`, `DeskSprites`).
  - **Defter ve ödül kâğıdı:** `RecipeData.Answer` (cevap sayfası, kimlik ve dizin temelin), `FacilityRewardText` cevap satırı ve
    `NewPageLine`; `BaseOf` kopyası kalktı (`FacilityGear.AnswerBases`).
- **Kalıcı tahta ve savunmanın sunumu (aşama 11; `Docs/TESIS-BASKINLARI-TASARIMI.md` §9 "Uygulanan hâl (aşama 11)"):** masa metinleri
  saf `FacilityDefenseText`'te (`Desk/Scripts/Bridge/Siginak/`), baskın sahnesininkiler `FacilityDefenseRaidText`'te (`Desk/Scripts/Bridge/`);
  ikisi de yalnız Core okuyucularından (dalga takvimi, süre, röle canı, kuşatma günlüğü, izler), savunma ve iz alt bayrakları kapalıyken
  hiçbir yüzey metin üretmez (`FacilityDefenseText.On`).
  - **Görev fişi:** `LiberationMapBuilder.SiegeSlip` (kuşatma kipi; `SlipData.Siege`, `HasYield/Yield/YieldCost/YieldEnabled/YieldReason`),
    sektör satırı `SlipSector.Siege`, kadro raporu `SquadReport(…, sectorId)`. Sahnede `OperationSlip.yieldAction/yieldLabel/yieldHover/
    yieldFill` (BIRAK; asıl düğmenin altında, kapalı kaydedildi; iki tık) → `IDeskSurface.YieldSector` → `GameController.YieldSector`; SAVUN
    `SiginakMap.SlipAction` → `ConfirmSquad` → `LiberationDeskView.DefendSector` → `GameController.DefendSector`. Kuşatma kipinde ödülsüz
    ÖDÜL satırı kalkar.
  - **Harita ve teleks:** `MapRegionState.Siege/SiegeDue`, `LiberationRegionMarks.SiegeMark` (nabız), `reoccStamp` "KUŞATMA"; `SiginakMap`
    kuşatma günlüğünü izler (kartlı oyunda "KARŞI SALDIRI BEKLİYOR" ve kendiliğinden bırakma teleksi birer kez; primed, kayıttan devam
    basmaz); kartsız vardiya raporu `ShiftReportInput.siege/siegeRegion/siegeFall/siegeFallRegion/siegeAuto`.
  - **Paftalar:** `FacilityPlanBuilder.Build(sim, regionId, sectorId)`; `FacilityPlanSheetData.Traces` (plana uygulanmış izler, kareye
    bakılarak), `FacilityPlanSketch.Traces` mürekkep (`lines`) katmanında, `PlanItem.Barrier/Scrap`.
  - **CRT, telsiz, devir fişi, pano:** `CrtViewBuilder`, `RadioLogBuilder`, `SlipWarningKind.Siege` (8; `HandoverSlipFacts.siegeTrust`),
    `SiginakDesk.PreCheck`/`SiginakShiftClose.PreCheck`, `PlanningBoardSnapshotBuilder.LockReason`. `RaidLog`'un savunma girdisi
    (`Mode == "defense"`) `OperationReportBuilder`, `ShiftReportBuilder.Ledger`, `FinalReportBuilder`, `RaidPaperFacts.MaterialGains`,
    `SiginakOperation`'da ayrılır.
  - **Baskın sahnesi:** `RaidViewModel.BlueprintDefense/Defense/PassLabel/PassNote/RetreatBody/RulesDefense`; `RaidDataSource`
    (`TimeoutEnd` savunmayı dışlar, `DefenseLastTurn`, `DefenseCard`, İÇERİ ÇEKİL onayı, dalga ve varış fişleri, gelecek makinelerin
    işaretleri `DefenseIncoming`'den); `RaidHudView.FitCells` (8'den çok turda hücreler 8 hücrelik genişliğe daralır; `RaidHud.prefab › Turn
    › Cells` Cell 9/10 ve `turnBoxes/turnFills/turnFrames`); `RaidBlueprintView` SAVUNMA anteti; `RaidPanelsView` kuralların savunma bölümü;
    `RaidBoard3D` `wave` ve `relay-break` popup'ı, "GELDİ" etiketi, RÖLE etiketi (ekip mürekkebi); saha rölesi `RaidPieceLibrary.relay`
    (+ `relayFallbackScale`) → `Desk/Prefabs/Raid/Gear/Relay.prefab` (kurgu, yol E); küçük resim `thumb_relay`.
- **Katar ve Yalpa'nın sunumu (aşama 12; `Docs/TESIS-BASKINLARI-TASARIMI.md` §10 "Uygulanan hâl (aşama 12)"):** bütün metinler saf
  `FacilityConditionText`'te (`Desk/Scripts/Bridge/`; yalnız Core okuyucularından: `RegionEvent`, `RegionNote`, `FacilityRoomTurns`,
  `OpenEdgeRows`, `IsOpenEdge`, `FollowerIds`, `FollowedInIds`, `CanDecouple`, `DecoupleBlock`, `TiltSchedule`, `Slides`, `Bolted`,
  `Hazards`; şart yoksa null).
  - **Masa:** `LiberationMapBuilder.Identity(…, sim)`/`Fields` fişin OLAY'ı `RegionEvent`'ten, bina hover'ı `RegionNote`'tan (kuşatma fişi
    `BuildingNote`); `FacilityPlanSheetData.OpenEdges` → `FacilityPlanSketch.DrawOpenEdges` (çerçeve açık kenarda çizilmez); paftanın
    hover'ı `PlanLine`; `OperationReportBuilder.TeamRows` kenardan düşeni `ReportOverboard(FallShifts(sim.B))` ile yazar.
  - **Baskın sahnesi:** `RaidDataSource` AYIR (`detach`, `Detach`, `CanDetach`, `ToggleDetach`, `Decouple`; `Arm` seçili birim yokken,
    `Select` Teknisyen'in kartı, `EndTurn` `Decouple` ya da `Pass`), `PassLabel/PassNote`, durum satırı, `OnEffect` `fall`/`slide`/`yalpa`,
    gözcünün takip satırı, "TAKİP EDER" (`MarksFor`), düşme önizlemesi; `RaidTeamModel.Detach` → `RaidTeamCardView` ("AYIR · Q", kart
    tıklanır). `RaidBoard3D`: `DrawGate` `brink` ve `tilt`/`tilt_next`, `DrawHazardLine` `slide`, `HzLabel`/`HazardInk`, `Sync`'te
    `Overboard` parçasız, `AddPreviewGhostAt` (kesirli kare), `Animate` `fall` ve `slide`, `EventFx` `yalpa` (`TiltRoll`, `SlideFx`,
    `FallOffEdge`, `playedSlides`), test okuyucusu `HazardShownCount`. Renkler `RaidPieceLibrary.hazardBrink/hazardTilt`. Sahne ve prefab
    değişmedi.
- `RaidDataSource` taktik komut/sunum sınırıdır: kurallar, hedefler ve önizleme Core'dan; görünür niyetler
  `Baskin.Intents` üzerinden gelir. UI kural, hasar ya da RNG hesaplamaz. `RaidHudView` klavyenin tek giriş noktasıdır
  (1 / 2 / 3 birim seçer, tesiste 4 köpeğin kartı · Q yetenek · E hamle (yalnız tesiste) · R ekipman (yalnız tesiste) · Z geri al · L günlük · Enter emri ilet / GEÇ · Esc vazgeç). Ekip kartları ve 1–3 sırası baskının kadrosundan
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
  bayrağını tutar. `RaidPieceLibrary.KitFor(bölge, MapSeed)` seçer (MapSeed 0 → `defaultKit`: örnek baskın, görüntü seti; tesis odasının
  `Params`'ı emrin kendisidir, MapSeed = harekât tohumu, `FacilityRaidSceneTests.FacilityRoom_UsesRegionKit` kilitler); `RaidDataSource.OpenState` `Build`'den önce `RaidBoard3D.SetDress` çağırır (`kitOverride` yalnız editörde). Karo
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
- **Kâğıt bileşenleri:** `Desk/Prefabs/Components` (PaperPanel, DeskButton, KeyChip, RadioCheck) hiçbir sahneden erişilmediği için Aşama 10'da (T1b) silindi; yedek `UI_Siginak_Rev3/A10_silinenler.zip`. Özel Graphic'ler: `UILineGraphic`, `UIPolygonGraphic`, `SpotlightGraphic`,
  `RegionMeshGraphic` (harita bölgeleri: sahnede serileştirilmiş çokgenler ve tıklama testi). Çokgenler tasarım
  dikdörtgeninden (1920 × 1080) taşar; `SadeMapCanvas`'taki `RectMask2D` haritayı kâğıdın kenarında keser (testli).
- **Üretilmiş haritanın sunumu (anahtar 1):** `SiginakMap.Refresh` her yenilemede `LiberationMapLayer.SetMap(sim.Map)` çağırır;
  aynı harita için iş yapmaz, yeni harekâtta bir kez kurar, null ise sahnedeki dünya haritasına döner (çokgenler, etiketler,
  `teamAnchors`, S-07, lejant yeri sahneden geri gelir; sahne asset'i hiç değişmez).
  - `GeneratedMapGeometry` (düz sınıf, `Runtime/Sade/Map`): Core haritasının kopyası. Köşe kimliğinden tohumlanan köşe sapması,
    kenar başına 7 ara nokta ve iki uç kimliğinden tohumlanan üç harmonikli dalga; kıyı kenarı ve köşesi iç sınırdan hırçın
    (`space.mapGen.coastAmplitude/coastJitter` ↔ `edgeAmplitude/vertexJitter`). Kenar kanonik yönde bir kez hesaplanır, komşular
    aynı çizgiyi görür; taşıyan hücre merkezlerinden bakınca geri dönen kenarın dalgası yarıya iner (yıldız biçim koruması,
    yelpaze üçgenleme bozulmaz). Hücre çokgenleri, bölge/kıyı halkaları, sınır zincirleri, kablo eğrileri, içe kaydırılmış kontur,
    su çizgisi, kalem geçişi, `CellAt/RegionAt` (64 px kova, dizi üstünde çift-tek testi).
  - `GeneratedMapLayout` (düz sınıf, `Runtime/Siginak`): otomatik yerleşim; metni dışarıdan ölçer (`Measure`, TMP
    `GetPreferredValues`), iki dilin büyüğüyle ve iki dilde aynı konumla. Birinci tur adın ve altındakilerin kendi bölgesinde
    kalmasını ister (10 px ızgara, toplam alan tablosu), olmazsa ikinci tur yalnız çakışmasızlık; o da bitmezse üçüncü tur ikinci
    turun kuralını adın hücre merkezinden daha uzağa kayabildiği geniş seçeneklerle dener (`LabelOptions(wide: true)`; tek
    sözcüklü YZ adı iki satıra bölünemez). İlk iki turda biten yerleşim üçüncü turdan etkilenmez. Sabit kutular: S-07, lejant,
    kablo raptiyeleri, örtücüler (`space.mapGen.occluderTypewriter/occluderPencil`: K2'de daktilo ve kalemin altı). Cephe bloğu
    başka bölgenin karasına ve kendi konturunun üstüne düşmemeye, figür tabanı (`FigureBase`, `figureOwnShare`) kendi karasında
    ve kablo raptiyesinden uzak durmaya puanlanır. `Exclusive` hangi iki kutunun aynı anda görünebildiğini söyler. Karargâh
    önce yerleşir ve birinci turda kademelidir (`HqFull` figür + bütün blok + raptiye kendi karasında, `HqFigure` figür +
    raptiye, `HqFigureOnly` yalnız figür, `HqLoose` genel kural; `Placement.HqStrict`). Raptiyenin yanı `Placement.PinRight` (`LiberationRegionMarks` aynı
    yana çizer); "karada" ölçüsü başın konturun içinde kalması (`PinInside`, `PinClearance` 19 px; kontur sınırın 5 px
    içinde); sıkı kademelerde figür tabanı `HqFigurePad` 6 px büyütülerek ölçülür;
    karargâhın adı kendi bölgesinde sayılmak için kutusu `HqLabelPad` 3 px büyütülünce de karargâhta kalmalı. Karargâhın komşusu gevşemeden önce karargâh `HqFull`'dan iner; lejant yeri bitmezse aynı yerde `HqLoose` ile bir
    kez daha. S-07 adayları (`ShelterSpots`) konturdan pay (`ShelterContourPad`), aday okları (`FrontierArrows`,
    `GeneratedMapGeometry.MiddleEdge` ile `SetFrontier`'la aynı) ve kablo ipi kuralına göre işaretlenir; `ChooseShelter`
    önce S-07'siz yerleşimin boş bıraktığı uyan adayı, yoksa karargâhı her adayla yeniden yerleştirip en iyisini seçer.
  - `GeneratedMapRenderer` (MonoBehaviour, "Liberation marks" üstünde): katmanları açar/kapar (`worldLayers` = map_base/map_lines,
    `generatedLayers` = GenPaper, GenHalo, GenLines, lejantın "kablo hattı" satırı), çizer (hale, su çizgileri, sınır, kıyı, kalem,
    kablo ipi, raptiye katmanları) ve yerleşimi `LiberationRegionMarks.SetGenerated`'e verir; adaylara oklar (`SetFrontier`).
    `BuildGeometry` ve `Names` (her dil, `Loc.Languages`) testlerle ortak.
  - `LiberationMapLayer`: ekip noktası yerleşimden (`team_anchors.json` yalnız anahtar 0), S-07 yerleşimin seçtiği yere
    taşınır (`GeneratedMapLayout.ShelterPoint`: karargâhın iç hücreleri ve küçük kaymalarından, karargâhın adı, ilk cephe
    bloğu ve figürüyle birlikte en iyi sığanı; `ChooseShelter`), lejant
    seçilen köşeye (sağ yarıda sağ kenarından tutulur). `SiginakMap.SlipOnRight`: anahtar 1'de etiket sol yarıdaysa fiş sağda,
    anahtar 0'da eski "na"/"sa" kuralı.
  - Adlar sunumun her yerinde `Simulation.RegionName/RegionSourceName/Links`'ten (telsiz ve CRT kısa adı harita başına tekil:
    çakışan kısaltma tam yazılır). Harekâtsız örnekler (editör araçları, tek açılan Baskin) `Bridge/SampleMap` (tohum 79,
    Core önbelleğine girmez). Renkler `colors.map.*`, `colors.mapLiberation.*`; ölçüler `space.mapGen.*` (`tokens_siginak.json`).
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
- **Bağlamlı çeviri (Aşama 10):** `DeskLoc.Instance.Ctx("bağlam", "KAYNAK")` aynı Türkçe kaynağa yere göre ayrı EN verir (KABUL damgada ADMIT, tezgâh şablonunda ADMISSION). Tablo anahtarı `kaynak@@bağlam`;
  satır yoksa düz `Loc.T` gibi davranır, Türkçede kaynağın kendisidir. İki argüman da dizgi sabiti olmalı (`LocalizationTool` toplar). Desk katmanındadır, Core kullanmaz.
- **Zaten çevrilmiş değerler:** `RegionDef.Name` ve `EndingDef.Title/Text` çevrilmiş döner; ikinci `Loc.T` EN'de `Loc.Missing` tetikler. Türkçe sayıya uyumlu ayrılma eki `TrNumber.WithAblative`.
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
  başladıysa atlanır. Baskın kapanınca yer unutulur. Yeni `LoopCue` eklenmez (her değeri hep
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
- **Revizyon 3 ipuçları:** `Cue` 56–66 ve 97–128'in (109 hariç) 42 yer tutucusu `Tools/Audio/rev3_placeholder_audio.py` ile sentezlendi (A10-D5), `Audio/Placeholder`'da durur ve `SoundBank`'a bağlıdır.
  Vardiya kolu (109) `SwitchClack` kliplerini kullanır.

## Kalıcılık ve platform

- `persistentDataPath` (`SaveStore`): `liberation_savegame.json`, `liberation_desk.json`. PlayerPrefs: `corrigible.language`,
  `corrigible.audio.*`.
- `Platform.Current` (`IPlatformServices`): varsayılan `LocalPlatform` başarımları konsola yazar. `Corrigible.Steam`
  derlenince `SteamPlatform` kendini `RuntimeInitializeOnLoadMethod` ile kaydeder. Steam Cloud için
  `persistentDataPath` Steamworks'te Auto-Cloud'a eklenir (kod gerekmez).

## Editör araçları

- `CorrigibleDebug` (`Editor/CorrigibleDebug.cs`): `Capture(path)` (Overlay canvas'ları geçici olarak kameraya bağlayıp
  PNG alır; edit ve Play modda), `Status()` (harekât özeti).
- Oda araçları (`Desk/Scripts/Editor/Siginak`): `Rev3CaptureSet` (Aşama 10 tam görüntü ve dil/punto seti: oturumlar `menu`, `k1`, `k4`, `k6`, `k8`,
  `hover`, `raid`; tohum 79, 1920×1080, TR ve EN → `Docs/UI_Siginak_Rev3/screens_unity_rev3/A10_*`; `EditorApplication.update` üstünde yürür, yeni MonoBehaviour yok), `SiginakCapture`
  (`Render(poz)`: Overlay'siz 3B görüntü, `SiginakPose` adıyla), `SiginakFixtures` (V014, V031, K6, V019, injured, V015ca),
  `SiginakTextScan` (punto taraması: oda, CRT, eğitim notu, DeskUI, Baskin sahnesi, devir fişi, defter ve kabul kâğıtları
  ("KAPI VE KABUL", K8R okuma pozunda); rapor `Docs/UI_Siginak_Rev3/punto_taramasi.txt`; canlı tarama `ScanLiveAll(cam, tag)` durum başına satır yazar, `ResetLiveAll`/`WriteLiveAll` →
  `Docs/UI_Siginak_Rev3/A10_dil_olcumleri.json`), `AdmissionFixtures` (kabul sunum
  fikstürleri `present`…`inspect_en`, düz kâğıt çizimi), `HandoverSlipFixtures`, `SiginakFontPreview`, `SiginakTextureBaker`,
  token içe aktarıcı.
- Varlık hattı (`Desk/Scripts/Editor/Import`): sprite içe aktarıcı ve kaydı, atlas, font asset'leri (tek font için
  `DeskFontSetup.BuildOne`), tema içe aktarıcıları (sığınak token'ının kaldırılmış düğümleri `_was_1_3`'ten korunur).
- **Revizyon 3 model hattı** (`Desk/Scripts/Editor/Rev3`; Docs/UI_Siginak_Rev3/PROMPT §4). Kalıcı bir asset hattıdır,
  sahne kurmaz.
  - **Kaynak:** `Rev3Manifest` paketteki manifesti okur (`Docs/UI_Siginak_Rev3/modeller`).
  - **İçe aktarma:** `Rev3ModelImporter` (Corrigible › Revizyon 3 › Modelleri içe aktar) OBJ'yi `Art/Rev3/Models/<aile>`'ye
    kopyalar; ayarı `Rev3ModelPostprocessor` kilitler (ölçek 1, dönüşüm yok, normaller dosyadan).
  - **Prefab:** `Prefabs/Rev3/<aile>/<Model>.prefab` kurulur. Kökte `Rev3Model`; altında `Body` ve manifestteki parçalar
    (`parent`, `pivotLocal`; hareketlide `PartDriver`, dinlenme pozunda).
  - **Yazı yuvası:** `TXT_*` → 3B TMP + `Rev3TextSlot`. Punto `capMm` ve fontun büyük harf oranından; şablon rolü Big
    Shoulders. Yüz yuvası TMP'siz bir dikdörtgendir.
  - **Çapa ve ışık:** çapalar boş nesne; ışık noktaları kapalı `Light`.
  - **Malzeme:** `Rev3MaterialSetup` her anahtar için URP Lit kurar (manifest + token 1.3, aynı değerli anahtarlar paylaşır)
    → `Art/Rev3/Materials/Rev3MaterialLibrary.asset`.
  - **Görüntü:** `Rev3ModelSheet` model sayfası ve aile görüntüsü çizer (boş, kaydedilmeyen sahnede).
  - **Rapor:** `Docs/UI_Siginak_Rev3/model_import_report.txt`.
  - Çalışma anı tarafı (`Desk/Scripts/Runtime/Rev3`): `PartDriver` pozu uygular (`Set`, `AnimateTo`, `SetSwap`,
    `SetVisible`); mantık görünümdedir.
  - **Odada (Aşama 2):** modeller sahnede prefab örneği olarak durur; sunum bileşenleri model parçalarına bağlanır
    (`CounterWheel.drum`, `StationKey.cap/jewel`, `ShiftSwitch.coverHinge/lever/lockBand`, `Teleprinter.frontCode…frontPrev`,
    `DoorProp.leaf/wheel/peepCover`, `RadioLogBook.page`…). Parçayı bileşen sürer (`localRotation`, `sharedMaterial`);
    aynı parçada `PartDriver.AnimateTo` ya da `Rev3Model.ResetPose` çağrılmaz. Sahneye özel dokulu malzemeler (metre
    cinsinden UV'li kadran, gösterge) `Art/Rev3/SceneMaterials/`'da; kitaplık yeniden kurulunca etkilenmez.
- **Poz ışığı** (`Runtime/Rev3/PoseLighting`, `PoseLightRecipe` → `Desk/Data/Rev3/PoseLightRecipe.asset`): rig'in
  `PoseChanged`'inde tarifteki ışıkların taban şiddeti geçiş boyunca karışır; gölge geçişin başında açılır, sonunda kapanır,
  sönecek ışık sonunda kapanır. Ampul ve dolgusu `BulbFlicker.BaseIntensity` / `SetFillBase` üzerinden (kırpışma ve vardiya
  karartması üstüne çarpar). Ortam: sahnenin üç renkli ortamı × poz çarpanı × `ShiftSequence.AmbientScale`, yalnız DeskGame
  etkin sahneyken. Bir pozda en çok 4 gölgeli ışık (`Rev3RoomTests`; K8A/K8R (ve emekli K8G) tam 3: ampul spotu, masa lambası, `counterSpot`;
  `counterSpot` K8 ailesi dışında tarifte açıkça kapalı). Tarifte olmayan ışıklara (kapı, koridor, dolap,
  şalterin amber ışığı, umut ışığı) sahipleri dokunur. İki pozda aynı kalan şiddet yazılmaz. `cameraFill` (tarifte
  `envFill`) kamerayı izleyen gölgesiz geniş spottur: three.js referansının ortam haritasının karşılığı; odanın içinde durur,
  baskında oda gizlenince söner.
- **Gölge kuralları (Aşama 10, `Rev3RoomTests`):** DeskGame ve Giris'te gölgeli her `Light` Spot'tur, kapalı olanlar dahil (Point gölge 6 atlas dilimi yer; koridor lambası bu yüzden Spot 110°,
  `CeilingBulb/BulbLight` gölgesiz). Üç ana ışık (`bulbSpot`, `deskLamp`, `boardLamp`; `PoseLighting` bağlamaları) Custom kademede `shadowResolution` 2048 ister (sahne override'ı): URP bu kademede
  `m_CustomResolution`'ı değil `Light.shadowResolution`'ı okur, −1 atlası küçültür. `counterSpot` Yüksek kademede (1024). Ölçülen atlas: K1 3 × 2048, K4 2048, K8A 2048 + 1024,
  K8 kapı açık 2048, 2048, 1024, 1024. Yönlü ışık ve `m_MainLightShadowmapResolution` yalnız Baskin'i etkiler (DeskGame ve Giris'te Directional yok).
- Bina kiti araçları (`Desk/Scripts/Editor/Raid`): `RaidKitTextureBaker` (Corrigible › Baskın › Kit dokularını üret: 12
  kitin dokuları, malzemeleri ve `Kit_<bölge>.asset`'leri; motifler `RaidKitTextureBaker.Motifs*.cs`'te), `RaidKitGate`
  (okunabilirlik kapısı; baker kapıdan geçmeyen kiti kütüphaneden çıkarır, `RaidRoomKitTests` de aynı kapıyı kullanır),
  `RaidKitPreview` (Corrigible › Baskın › Kit önizleme sayfası: gerçek prefab ve giydirme koduyla 12 kit, gri tonlu kopya).
  Baskın parçalarının küçük resimleri `SiginakTextureBaker.PieceThumbs` (Corrigible › Sığınak › Baskın parçalarının küçük
  resimlerini üret).
