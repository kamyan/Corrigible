# Sığınak Komuta Odası — Uygulama haritası

**Durum:** Aşama 0 (keşif) bitti, onay bekleniyor. Kod yazılmadı.
**Taban:** `main` · `6647af3 Before design implementation.` · çalışma ağacı temiz (yalnız `Docs/UI_Siginak/` izlenmiyor).
**Okunanlar:** `PROMPT.md`, `reference/BRIEF.md`, `reference/notes/*.md`, `spec/tokens_siginak.json` (yapı + kamera/ışık/malzeme),
`sprites/README.md`, `reference/scene_threejs/README.md`, `reference/screens/K1, K2-TR, K4-TR, K6b`. Sahne Unity CLI ile
salt okunur tarandı (`DeskGame.unity`, Edit modu).

---

## 1. Önemli bulgular

1. **Ölçek farkı: oda yeniden kurulmalı.** Bugünkü `CommandPost3D` tasarımın yaklaşık 2,4 katı ölçekte. Masa 4,8 × 2,6 m,
   harita 2,69 × 1,51 m (`SadeMapCanvas`, lossy 0,0014), başlangıç kamerası (0, 3,2, −5,6) ve FOV 58. Yeni oda gerçek
   ölçekte: masa 1,84 × 1,10 m, harita 1,20 × 0,675 m (0,000625 → 1600 px/m), kamera (0, 1,52, −0,15). Eski düzen
   taşınamaz; yeni oda ayrı bir kök olarak kurulur, eskisi anahtar kapalıyken çalışmaya devam eder.
2. **Etkileşim mantığı tek yerde.** Kurtuluş masasının bütün komut çağrıları, `*Check()` gerekçeleri ve girdi kilidi
   [LiberationDeskView.cs](../../Assets/Corrigible/Desk/Scripts/Bridge/LiberationDeskView.cs) içinde. Yeni sunum bu
   komutları olduğu gibi kullanır; yalnız görünüm değişir.
3. **Token kalıbı var.** `TokenTheme` (JSON yolu → renk/sayı) + `SadeTheme` (tip, gölge) +
   `Editor/Import/SadeThemeSetup` (JSON → `Resources/*.asset`). `SiginakTheme` aynı kalıpla kurulur; yeni bir yükleyici
   icat edilmez.
4. **Fontlar hazır, iki eksik var.** Projede CP 400/700, LF 400/500/600/700/800, Kalam 400/700 ve DejaVu yedek fontu var
   (`Desk/Resources/Fonts & Materials`). Big Shoulders Stencil yok. `Desk/Fonts/charset.txt` içinde `▸` (U+25B8) ve
   `▮` (U+25AE) yok; öteki zorunlu glifler var.
5. **URP ayarı büyük olasılıkla değişmeyecek.** `PC_RPAsset` zaten Forward+ (nesne başı ışık sınırı yok), ek ışıklar
   piksel başına ve gölgeli, atlas 2048. Sahnede Volume yok. Ortam ışığı Flat; Gradient'e geçmek sahne ayarıdır.
6. **Ses kurgusu.** Bütün sesler `Sfx.Play(Cue)` → `AudioDirector` → `SoundBank` yolundan çalar; mixer yok. Yeni sesler
   için `Cue` enum'una ekleme gerekir (Game assembly, ekleme). Klip olmayan ipucu sessiz geçer.
7. **Kayıt yalnız komut geçmişi.** `GameRecord` tohum ve komutlarla tutulur, `Replay.Restore` ile geri yüklenir. Kamera
   pozu, teleks geçmişi ve vardiya raporları bu kayda yazılamaz (bkz. Soru 4).
8. **ENERJİSİZ hâli bugünkü kurallarla oluşamıyor.** `ShelterUpgradeCheck`, talep kapasiteyi aşacaksa atölye/revir
   geliştirmeyi engelliyor. `ShelterAssignCheck` da enerjiyi kesecek jeneratör personel azaltımını engelliyor. Jeneratör
   seviyesi düşmüyor. Bu yüzden V.014 fikstüründeki “atölye SV3, enerjisiz” durumu komutlarla kurulamaz (bkz. Soru 1).
9. **Boşluk tuşu bugün vardiyayı tek basışta bitiriyor.** Yol: `DeskInput` → `Commands.TogglePause` →
   `SimDeskCommands.TogglePause` → `Game.AdvanceLiberationDay()`. Tasarımda Boşluk yalnız diziyi atlıyor (bkz. Soru 3).

---

## 2. Klasör ve assembly yerleşimi

Varsayılan `Assets/Corrigible/Presentation/Siginak/` yerine projenin düzenine uyuldu. Bu yolda assembly yok; script'ler
`Assembly-CSharp`'a düşer ve `Corrigible.Desk` / `Bridge` assembly'lerinden görünmez.

| İçerik | Yer | Assembly |
|---|---|---|
| Görünümler (sayaç, anahtar, şalter, teleks, kâğıtlar, maket, CRT, figür) | `Desk/Scripts/Runtime/Siginak/` | `Corrigible.Desk` (Core + UI + TMP + Input System; Game'e bağımlı değil) |
| Oyun bağlantısı (görünüm durumu kurucu, rapor kurucu, sunum anahtarı, CRT/raid geçişi) | `Desk/Scripts/Bridge/Siginak/` | `Corrigible.Desk.Bridge` (Game'e erişir) |
| Builder, importer'lar, fikstür, poz yakalama, punto tarayıcı | `Desk/Scripts/Editor/Siginak/` | `Corrigible.Desk.Editor` |
| Malzemeler, sprite'lar, prefab'lar, üretilen dokular | `Desk/Art/Siginak/{Materials,Sprites,Textures}`, `Desk/Prefabs/Siginak/` | — |
| Tema sınıfı | `Desk/Scripts/Runtime/Theme/SiginakTheme.cs` (öteki temaların yanında) | `Corrigible.Desk` |
| Deneme sahnesi | `Desk/Scenes/Siginak_FontTest.unity` | — |
| Tokenlar ve bölge verisi | `Desk/Data/Siginak/tokens_siginak.json`, `team_anchors.json` | — |
| Tema asset'i | `Desk/Resources/SiginakTheme.asset` | — |
| Testler | `Desk/Tests/EditMode/Siginak*Tests.cs` | mevcut Desk test assembly'si |

Kurallar (AGENTS.md): her MonoBehaviour kendi dosyasında, adı sınıf adıyla aynı. Bağlantılar `[SerializeField]`; `Find`
yok. Metin `Loc.T/F/N` ile Türkçe kaynaktan, İngilizcesi `strings.txt`'te. Projede kaynak metin anahtardır, ayrı
anahtar sistemi kurulmaz.

---

## 3. Eşleme tablosu

**Yapılacak:** KULLAN = yeniden kullan · DEĞİŞTİR = mevcut kod/sahne değişir · YENİ = yeni bileşen/asset.
Kısaltmalar: `Lib` = `Sim.S.Liberation`, `Sh` = `Sim.Shelter`, `B` = `Sim.B` (Balance), `G` = `GameController`.

### 3.1 Genel altyapı

| Tasarım öğesi | Mevcut karşılık | Yapılacak | Veri kaynağı |
|---|---|---|---|
| Sunum anahtarı `SiginakPresentation` | yok | YENİ: `SiginakSettings` (SO, `Resources`) + `SiginakPresentationSwitch` (Bridge) + `DeskDebugMenu`'de satır. Eski/yeni kökleri, kamera rig'ini, harita canvas'ının yerini ve raid geçiş kancasını değiştirir | — |
| Tokenlar | `TokenTheme`, `SadeTheme`, `SadeThemeSetup`, `Data/Sade/tokens_sade.json` | YENİ `SiginakTheme : TokenTheme` (renk/sayı + tip, gölge, kamera, ışık, malzeme). `SiginakThemeSetup` importer'ı. “reuse” anahtarları `SadeTheme.Active`'ten okunur | `tokens_siginak.json` |
| Fontlar | `DeskFontSetup`, `charset.txt`, `Fonts & Materials/*` | DEĞİŞTİR: `charset.txt`'e `▸ ▮` eklenir, atlaslar yeniden kurulur. Big Shoulders için bkz. Soru 8 | — |
| Sprite'lar | `DeskSpriteImporter`, `SadeSpriteManifest` | YENİ `SiginakSpriteManifest` (README'deki dilim, Tiled/Sliced, PPU) mevcut importer'la | `sprites/README.md` |
| Yerelleştirme | `Loc`, `DeskLoc`, `LocalizedText`, `DeskFormat` (TR virgül / EN nokta, yüzde) | KULLAN. `LiberationDeskView:185`'teki TR kaynak `"LOOT"` → `"YAĞMA"` (`strings.txt`: YAĞMA → LOOT) | — |
| Ses | `Sfx`, `Cue`, `SoundBank`, `AudioDirector` | DEĞİŞTİR (yalnız ekleme): yeni `Cue` değerleri ve `SoundBank` alanları. `SiginakAudio` adlandırılmış çağrıları toplar. Klip yoksa yer tutucu | — |
| Girdi kilidi | `LiberationDeskView.InputBlocked`, `CommandPostDesk.InteractionBlocked`, `DeskInput` | KULLAN. Yeni bileşenler aynı `InputBlocked`'ı okur. Dünya raycaster kilit listesi yeni World canvas'larla yeniden kurulur; Overlay canvas'lar listeye girmez (2026-10-21 hatası) | — |
| Görünüm durumu | `LiberationDeskView.Refresh()` her şeyi doğrudan Sim'den okuyor | YENİ `LiberationViewBuilder` (Bridge) → saf veri `LiberationViewState` (Desk). `LiberationDeskView` tek `IDeskSurface` ve komut kapısı olarak kalır; anahtar açıkken çizimi `SiginakDesk`'e devreder | aşağıdaki satırlar |

### 3.2 K1 · oda ve masa

| Tasarım öğesi | Mevcut karşılık | Yapılacak | Veri kaynağı |
|---|---|---|---|
| Oda kabuğu, kanal/borular, duvar yazısı, doktrin afişi | `CommandPost3D/Environment` (duvarlar, 11 takviye, 3 boru, `DesignationPlate`, `WarningPlate`) | YENİ `SiginakRoom` kökü (builder, `room.js` ölçüleri). Eski kök anahtar kapalıyken kalır, Aşama 10'da kaldırılır | — |
| Mantar pano + KURTARILAN BÖLGELER + KAYIP ilanları | yok | YENİ (World TMP liste) | `Lib.Regions` (`Completed`, sunucu sektörünün `CapturedDay`) |
| Umut işaretleri (1/3/6/9/12) | yok | YENİ `HopeMarks`: ayrı ve kapatılabilir (`SiginakSettings`) | özgür bölge sayısı |
| Tavan ampulü + titreme | `Dim overhead bunker lamp` (Point, gölgesiz) | YENİ Point + emissive kafes + `BulbFlicker` (3–9 s, 2–3 kırpışma) | `lights.bulb`, `motion.bulbFlicker` |
| Masa (linolyum, çelik, pirinç şerit) | `Table` (meşe 4,8 × 2,6) | YENİ | `materials.lino/steel/brass` |
| Banker lambası (sağ arka, Spot) | `Lamp` + `Warm tungsten pool` (Point) | YENİ | `lights.deskLamp` |
| Ortam ışığı Gradient | `RenderSettings` Flat (0,22, 0,26, 0,24) | DEĞİŞTİR (sahne ayarı, anahtarla birlikte) | `lights.ambient` |
| **Sayaç rafı** (5 modül, çark, ± fişi) | `LiberationHudCanvas/Top summary backing` (Overlay TMP), eski `Ledger` (World TMP) | YENİ `CounterBank`, `CounterWheel` (Quad + `mainTextureOffset`), `DeltaSlip` | VARDİYA `S.Day` · FON `S.Fon` (onda bir çarkı) · ERZAK `Sh.Supplies` / `ShelterLogic.SuppliesCapacity` · MALZEME `Sh.Materials` · ÖZGÜR `Lib.Regions.Count(Completed)` / 12. Fark = önceki çizim ile yenisi |
| Kenar satırı (40 px) | aynı Top summary | YENİ `EdgeLine` (Otomatik / Her zaman / Kapalı; tercih `PlayerPrefs` `corrigible.siginak.edgeLine`) | sayaçla aynı + etkin istasyon |
| **Anahtar rayı** (1 · 2 · 3 · H · Home) | `Station controls` (`commandStation`, `shelterStation`, `operationStation`), eski `CommandPost3D/Tabs` | YENİ `StationKeyRail`, `StationKey`; tıklama mevcut çağrılara gider (`ShowPlanning`, `FocusShelter`, `OpenWindow(Guide)`, `ResetView`) | etkin = rig pozu; devre dışı = `InputBlocked` / baskın |
| **Vardiya şalteri** | `advanceDay` düğmesi + Boşluk kısayolu | YENİ `ShiftSwitch` (Idle → Hover → CoverOpen 4 s → Thrown → Sequence · Disabled) → `G.AdvanceLiberationDay()` | devre dışı: `S.Raid != null`, `S.Blocked`, `S.Ending != null`, rapor açık. Ön kontrol satırı: `AvailableWorkers`, enerji açığı, hazır ekip ve hazır sektör varken verilmemiş baskın |
| **Teleks + teleks şeridi** | `Result note` (Overlay TMP, `LiberationDeskView.Report`), eski `Fax`/`Radio` World canvas'ları (haber), C1 `TelexStripView` | YENİ `TelexFeed.Post(code, status, message, kind)` (tek kapı), `Teleprinter` (World), `TelexStrip` (Overlay 820 × 52). Bütün `Report()` çağrıları buraya taşınır | `ActResult.Ok/Msg`, `*Check()` gerekçeleri, kod `VVV/NN` (vardiya / o vardiyanın satır sayacı, yalnız sunum) |
| Üzerinde etiketi + sarı çerçeve | `CommandPostControl` (IPointerEnter) + `CommandPostDesk.SetHover` (alt yardım satırı) | YENİ `HoverTag` (400 ms, kâğıt); `HoverHighlight` (Image çerçeve) 2026-10-27'de kaldırıldı. Etkileşim kalıbı `CommandPostControl`'den alınır | — |
| AYARLAR plakası / ATLA | `Campaign settings` → `SettingsView.Instance.Open()` | DEĞİŞTİR → `SettingsPlaque` (aynı çağrı; kilitli dizide ATLA) | dizi durumu |
| EVRAK tepsisi | `CommandPost3D/Files` (dekor) | YENİ `EvrakTray` (son 5 rapor + brifing) | oturum içi liste (bkz. Soru 4) |
| Kupa, kalem, talaş, damga rafı | yok | YENİ dekor (tıklanmaz) | — |

### 3.3 Kamera ve kontroller

| Tasarım öğesi | Mevcut karşılık | Yapılacak | Veri kaynağı |
|---|---|---|---|
| Pozlar: K1 · harita (K2/K3/K6b/K7) · K4 · K5a · K6a | `CommandPostCamera` (home/map/shelter/operations pozları, sabit FOV 48/46/34/52, üstel yumuşatma) | YENİ `SiginakCameraRig` (aynı `Main Camera` üstünde; anahtarla biri etkin). Sağ tuş bakışı, tekerlek, F ve Home davranışı taşınır. Geçiş 0,45 s ease-in-out; K1 → K2'de önce 0,15 s yükselme | `tokens.cameras` |
| K1 Physical Camera + lens shift | yok (`usePhysicalProperties=false`) | K1'de fiziksel kamera (sensör 36 × 20,25, odak 18,76, shift Y −0,324). Öteki pozlarda shift 0; FOV geçişi odak uzaklığı üzerinden | `cameras.K1` |
| K5a CRT bakışı (istasyon değil) | `OperationsPose` | YENİ poz (ekranın yerel uzayında) | `cameras.K5a` |
| K6a vardiya anı | yok | YENİ poz (yalnız dizide) | `cameras.K6a` |
| Figür taşınırken bakış/tekerlek kapalı | yok | `SiginakCameraRig.InputSuspended` | `TeamFigureDrag` |

### 3.4 Harita (K2, K3)

| Tasarım öğesi | Mevcut karşılık | Yapılacak | Veri kaynağı |
|---|---|---|---|
| Kâğıt harita (World Space, 1920 × 1080) | `MapMount/SadeMapCanvas` (`SadeMapView`, `MapCoverFitter`, `RegionMeshGraphic` × 12) | KULLAN. Anahtar, canvas'ı yeni masanın `MapMount`'una taşır ve ölçeğini 0,000625'e ayarlar. Tek harita örneği kalır, kopyalanmaz | `map_regions.json` |
| Bölge durumları: işgal · cephe · aday · özgür · seçili · üzerinde | `SadeMapView.Bind` (sızma rampası; hepsi %100 olduğu için koyu kırmızı) | YENİ `LiberationMapLayer` (tasarımdaki `MapRegionView`). Her bölgeye aynı poligonla ek `RegionMeshGraphic` (tarama dokusu + tint: `SetAppearance(color, map_hatch_*, tile)`). Sade dolgusu anahtar açıkken kâğıt rengine çekilir | işgal: `!Completed`, cephe ve aday değil · cephe: `Lib.CurrentRegion` ve `!Completed` · aday: cephe tamamlanmışken `Sim.LiberationFrontier` · özgür: `Completed` · oran: `100 × kalan / 3` |
| Halkalar, raptiye, ÖZGÜR damgası, ✓✓✓, sektör işaretleri | Sade seçim/düğüm görselleri | YENİ (sprite'lar: `map_grease_ring`, `map_candidate_ring`, `map_pin`, `map_stamp_free`, `mark_sector_*`) | damga günü = sunucu sektörünün `CapturedDay` |
| Etiketler (CP 700 28, hale, 330 px'te iki satır) | `SadeRegionLabelView`, `SadeMapTextLayer`, `Sade Map Label.mat` | DEĞİŞTİR: kurtuluş alt satırı (`%100 · ADAY`), satır bölme kuralı, `labelScale` (bugün 1,6, büyük masaya göre) yeni ölçeğe göre | ad `GameData.Region(id).Name` |
| Basılı lejant | `SadeLegendCardView` (kriz lejantı) | YENİ `MapLegend` (sabit, harita üstünde; EN'de sütunlar açılır) | — |
| **Görev fişi** (436/452 px, dizin kâğıdı, −1°) | `LiberationMapCanvas/Region briefing` (Overlay, sort 40) + `UpdateBriefingPlacement()` | DEĞİŞTİR → `OperationSlip` (World Space, haritadan 2 mm yukarıda). Yerleşim: cepheyi, özgürleri ve adayları örtmeyen yarı. Esc ile kapanır | başlık/oran: `LiberationRegion(id)`. Sektör satırları: `Sectors[i]` (`Captured`, `MissionKind`). Durum: GERİ ALINDI = `Captured` · BASKINA HAZIR = `NextLiberationSector(cephe)` · KİLİTLİ = geri kalan. Düğme ve gerekçe: `LiberationRaidCheck`. BEDEL `B.liberationRaidCost`. ÖDÜL: yağma `B.liberationLootFunds` + `shelterLoot*`, sunucu `B.liberationServerFunds` + `shelterServerMaterials`, bölge `liberationCaptureFunds`. Ekip: `S.Teams[0]` (`Down`, `TeamReady`) |
| Kırmızı ip (3 noktalı eğri, 2,6 px + gölge) | `briefingLink` (`UILineGraphic`, 2 nokta), `CardStringView` | KULLAN `UILineGraphic` (Bézier destekli) + ince `UiStringLine` sarmalayıcısı | — |
| Baskın emri (EMİR VERİLDİ damgası, FON −12, 0,3 s sonra CRT) | `sectorButtons` → `G.StartLiberationRaid(id)` | KORU (aynı komut); damga/sayaç/gecikme sunumda | — |
| K3 akışı (damga, silinme, adaylar 0,12 s arayla, onay fişi) | `chooseRegion` → `G.SelectLiberationRegion(id)` | YENİ `FrontConfirmSlip` + `MapRevealSequence` | `SelectLiberationRegionCheck` (cephe açıkken: “Önce mevcut bölgenin…”) |
| **Ekip figürü** (3D, sürüklenir) | `SadeTeamTokenView` (düz jeton; `SadeMapView.Teams`) | YENİ `TeamFigure` prefab'ı + `TeamFigureDrag` (`Plane.Raycast`, 6 px eşik / 150 ms). Sade jetonu yeni sunumda gizlenir | yer: `Raid != null` → kalkık; `Down > 0` → S-07 (BA); değilse cephe; cephe tamamsa özgürleşen bölge. Yatık piyon = son `S.RaidLog` kaydındaki `Down` (yoksa 1). Bölge isabeti: `RegionMeshGraphic.ContainsMapPoint`. Bırakma = aynı `SelectRegion` / `SelectLiberationRegion` yolu |
| `teamAnchor` | `map_regions.json` → `Label (x, y)` | YENİ `Data/Siginak/team_anchors.json` (yoksa etiket + (0, 110)) | — |
| Masa lambası dairesi | Sade `tx_lamp_desk` | KULLAN | — |

### 3.5 Sığınak (K4)

| Tasarım öğesi | Mevcut karşılık | Yapılacak | Veri kaynağı |
|---|---|---|---|
| Kesit maketi (2 × 2 oda, yüzey, YZ direği, toprak) | `LiberationRoom/ShelterWindow` (raf, primitive odalar) + `ShelterScreen` | YENİ `ShelterDiorama`, `RoomView` (çalışan · seçili · enerjisiz · hasarlı · en üst seviye · dolu · yeni) | `Sh.Rooms[]` (`Level`, `Workers`), `WorkerCapacity`. Enerjisiz = atölye/revir, `Level > 0` ve `EnergyCapacity < EnergyDemand`. Hasarlı = `Level == 0`. Seviye sınırı `B.shelterMaxRoomLevel` |
| Piyonlar, pirinç yuvalar, BOŞTA tepsisi | yok | YENİ | `Workers`, `WorkerCapacity`, `ShelterLogic.AvailableWorkers` |
| Oda kartları (World 380 × 110) | `roomLabels` + `roomButtons` (yarı saydam renkli düğmeler) | YENİ `RoomCard` | aynı |
| Özet panosu (600 × 800) + seçili oda kartı | `shelterSummary`, `roomDetail`, `upgradeRoom`, `workerMinus/Plus` | YENİ `ShelterClipboard`; komutlar aynı (`G.ShelterUpgrade`, `G.ShelterAssign`) | TOPLULUK `Population / Capacity · AvailableWorkers` · ENERJİ `EnergyCapacity / EnergyDemand` · ERZAK · SON VARDİYA `LastSuppliesChange / LastMaterialsChange`. Eylem etiketi: `Level` 0 → ONAR, 1–2 → GELİŞTİR, 3 → EN ÜST SEVİYE. Maliyet `UpgradeCost`. “Eksik: n malzeme” = maliyet − stok. Gerekçe `ShelterUpgradeCheck` / `ShelterAssignCheck` |
| Sonuç notu (sarı yapışkan) | yok | YENİ (kural tablosu sunumda: enerji açığı, boşta kişi, hasarlı oda) | aynı alanlar |

### 3.6 CRT (K5)

| Tasarım öğesi | Mevcut karşılık | Yapılacak | Veri kaynağı |
|---|---|---|---|
| CRT ekranı (World; Bekleniyor · Kuruluyor · Yayın · Kesildi · Devre dışı) | `OperationsScreen` + `monitorStatus` (iki hâl) | YENİ `CrtScreen` (TMP + `crt_scanlines` / `crt_vignette` / `tx_crt_static` sprite katmanları) | Bekleniyor: `S.Raid == null`. Yayın: `RaidLauncher.IsRaidLoaded`. Kesildi: dönüşten sonra (oturum). Devre dışı: hazır ekip yok (`Down > 0`, ters video satır) |
| Baskın geçişi (2,3 s) + dönüş (0,6 s) | `RaidLauncher.LoadRoutine` (0,8 s düz yaklaşma, `monitorFocusPose`, `lockDuringTransition`, `hideDuringRaid`) | DEĞİŞTİR (ekleme, bkz. Soru 5): RaidLauncher'a bekletilebilir geçiş kancası. YENİ `CrtTransition` (Overlay cam, senkron kutuları, 8 × 8 ızgara) | ızgara: `tokens_sade.json → raid.camera.screenFromGrid1080` |

### 3.7 Vardiya geçişi (K6)

| Tasarım öğesi | Mevcut karşılık | Yapılacak | Veri kaynağı |
|---|---|---|---|
| Şalterin iki adımı, ön kontrol satırı | yok | `ShiftSwitch` (3.2) | 3.2 |
| Dizi (ışık %30, sessizlik, amber, sayaçlar, teleks, jeneratör, kamera, raporun düşmesi) | yok | YENİ `ShiftSequence` (Boşluk ile atlanır) | — |
| **Vardiya raporu** | yok (yalnız `shelterSummary`'de SON VARDİYA satırı) | YENİ `ShiftReportBuilder` (saf C#, EditMode testli) + `ShiftReportView` (Overlay 600 px) + masada A5 kâğıt | FON: `Sim.TransactionLedger` (o vardiyanın kayıtları: `liberation_raid`, `shelter_upgrade`, baskın ödülü) + `S.LastIncome` (`Central` + `RegionalBase`; gelir kaydının `Day`'i yeni gündür). Açılış/kapanış: vardiya başı/sonu snapshot. ÜRETİM: `Step` öncesi snapshot (`living.Workers`, `Population`, `workshop.Workers`, enerji) + `LastSuppliesChange`/`LastMaterialsChange` (bkz. Soru 6). EKİP: `Team.Down` önce/sonra; revir koşulu = `Level > 0`, `Workers ≥ B.shelterHealingWorkers`, enerji yeterli, `!Starving`. YAPILAMADI: o vardiyanın `TelexFeed` hata satırları |
| Fark fişleri, ALFA etiketi, EVRAK'a dosyalama | `DeltaView` (C tarzı ok) | YENİ `DeltaSlip`, `TeamTag`, `EvrakTray` | snapshot farkı, `Team.Down` |

### 3.8 Brifing ve açılış (K7)

| Tasarım öğesi | Mevcut karşılık | Yapılacak | Veri kaynağı |
|---|---|---|---|
| Açılış dizisi (siyah → jeneratör → ampul) | yok (`NewGame()` story canvas'ı hemen açıyor) | YENİ `OpeningSequence` | — |
| Emir belgesi + BAŞLA (Enter) | `LiberationStoryCanvas/Story paper`, `beginStory` → `CloseStory()` | YENİ `BriefingView`; `NewGame()`/`CloseStory()` akışı ve `TutorialHold` korunur | metin `Loc` (TR ≤ 520, EN ≤ 680) |
| Kayıt varken iki adımlı BAŞLA | yok: ilk komut eski kaydın üstüne sessizce yazar (`G.Run` → `Save`) | YENİ: ilk Enter teleks uyarısı, ikinci Enter onay | `G.SavedGameSummary() != null` |
| KAYITTAN DEVAM sekmesi (Tab → Enter) | `continueStory` → `G.ContinueSaved()` | YENİ sekme; kayıt yoksa “KAYIT YOK” | etiket “Vardiya 014 · özgür 1/12”. `SavedGameSummary()` bugün kriz biçiminde (“rakip, n. gün”) ve özgür sayısını bilmiyor (bkz. Soru 4) |
| KILAVUZ (H) | `OpenWindow(Guide)` → story canvas'ı yeniden açar | bkz. Soru 7 · **Aşama 9:** H belgeyi okuma kipinde açar | — |

### 3.9 Doğrulama araçları

| Tasarım öğesi | Mevcut karşılık | Yapılacak | Veri kaynağı |
|---|---|---|---|
| Durum fikstürleri V.001 / V.014 / V.014 → 015 / V.019 | `CorrigibleDebug` (`Advance`, `Status`, `Capture`), `LiberationReportTests` (sim kurulum deseni) | YENİ `SiginakFixtures` (Editor). Komut dizisi + `ResolveRaid` ile kurulur. ENERJİSİZ için bkz. Soru 1 | `GameRecord`, `G.ContinueSaved` |
| `Tools/Sığınak/Capture Poses` | `CorrigibleDebug.Capture` (iç içe Overlay'leri de yakalar), `DeskCapture`, `SadeCapture` | YENİ menü: poz + durum kurar, TR/EN PNG ve gri tonlama kopyası çeker | — |
| Punto tarayıcı | `SadeTextChecker`, `DeskTextChecker` (taşma, iki dil) | YENİ: World Space TMP'yi poz kamerasıyla izdüşürüp < 14 px olanları listeler | — |
| EditMode testleri | 655 test; sahne testleri `DeskGameScene_*` (`DeskBridgeTests`) | YENİ: token importu, rapor kurucu, TR/EN tamlığı (`LocalizationTests` zaten var), görünüm durumu kurucu (cephe/aday/özgür), figür yer kuralı. Eski sahne testleri Aşama 10'da güncellenir | — |

---

## 4. Girdi eşlemesi

| Girdi | Bugün | Yeni sunumda |
|---|---|---|
| `1` | `DeskInput` → `RegionAction(0)` → `ShowPlanning`; aynı karede `CommandPostCamera` → `FocusMap` | 1 KOMUTA (harita pozu). Tek işleyici: `SiginakCameraRig` + anahtar rayı. `RegionAction` çift çağrısı kapanır |
| `2` | `CommandPostCamera.FocusShelter` (+ `RegionAction(1)` boş) | 2 SIĞINAK (K4) |
| `3` | `RegionAction(2)` → `ShowPlanning`, rig `FocusMap` | 3 GÖREVLER: harita pozu + görev fişi açık |
| `H` | `OpenWindow(Guide)` → hikâye kâğıdı | H KILAVUZ (bkz. Soru 7) |
| `Home` / `F` | başlangıç / harita aç-kapa | aynı |
| `Enter` | modal/eğitim imzaları (`DeskInput`) | açık kâğıttaki asıl eylem; kâğıt yoksa şalter (1. basış kapak, 4 s içinde 2. basış kol). Modal/eğitim önceliği korunur |
| `Esc` | `presenter.Escape()` → hikâye kapat / Ayarlar | kâğıdı kapat → figür taşımayı iptal et → kapağı kapat → Ayarlar |
| `Boşluk` | **vardiyayı hemen bitirir** (`TogglePause` → `AdvanceLiberationDay`) | yalnız kilitli diziyi atla (bkz. Soru 3) |
| `Tab` | `presenter.Sade?.SelectNext` (kurtuluşta etkisiz) | K7'de KAYITTAN DEVAM sekmesine odak |
| Sağ tuş / tekerlek | bakış / FOV | aynı; figür taşınırken kapalı |
| Sol tuş sürükleme | yok | yalnız ekip figürü (6 px eşik; altı tık) |

---

## 5. Sunum anahtarı nasıl çalışacak

> Tarihsel: anahtar Aşama 10 sonunda kaldırıldı; baskın kısmı `SiginakRaidLink`'te (bkz. CHANGELOG).

- `SiginakSettings.asset` (Resources): `newPresentation`, `hopeMarks`, `edgeLine` varsayılanı. Geliştirmede debug menüsünden
  Play içinde de değiştirilebilir.
- `SiginakPresentationSwitch` (Bridge, sahnede) şu işleri yapar:
  1. `CommandPost3D` odasını, `LiberationHudCanvas`'ı ve `LiberationMapCanvas`'ı kapatır; `SiginakRoom`'u açar
     (tersi de olur).
  2. `Main Camera` üstünde `CommandPostCamera` ↔ `SiginakCameraRig` bileşenlerinden birini etkinleştirir.
  3. `SadeMapCanvas`'ı ilgili `MapMount`'a taşır ve ölçeğini ayarlar.
  4. `RaidLauncher`'a etkin geçişi ve monitör pozunu bildirir.
  5. `LiberationDeskView`'a çizimin hangi tarafta yapılacağını söyler.
- Baskın sürerken anahtar değişmez. `RaidLauncher` kapattığı bileşenlerin eski durumunu saklıyor; o sırada kök
  değiştirmek bu durumu bozar.
- Sahnedeki iki kök de serialized referanslarla bağlıdır. Aşama 10'da (onayla) eski kök, HUD ve story canvas'ı silinir,
  varsayılan yeni sunum olur.

---

## 6. Kararlarım (itiraz etmezsen böyle uygulanacak)

1. **Klasör:** §2'deki yerleşim (Desk assembly'leri içinde). Varsayılan `Presentation/Siginak` yolu kullanılmaz.
2. **Projeksiyon:** Harita, projedeki `MapProjection`'ı kullanır (λ0 25,5°, 360,68 px/rad, ekvator 640). K2 notundaki
   değerler (24,7°, 6,25 px/°, 639) three.js'in yaklaşık kopyası; aradaki fark birkaç piksel.
3. **Sektör damgası:** Cephedeki ilk alınmamış sektör, fon ya da ekip eksik olsa bile **BASKINA HAZIR** görünür; düğme
   devre dışı kalır ve gerekçeyi gösterir. Bugün böyle bir durumda KİLİTLİ yazıyor. KİLİTLİ yalnız sıra kilidi içindir.
4. **OP kodu:** `OP-` + sektör numarası (`OP-02` = 02 Tahliye geçidi), K2 ve K6b'deki gibi.
5. **Görüntülerdeki sayılar örnektir:** Fikstürler gerçek denge değerlerini gösterir. Örneğin revir onarımı gerçekte
   12 fon + 8 malzeme; panoda 40 + 24 yazıyor. Bu fark görsel karşılaştırmada sapma sayılmaz.
6. **Metin:** Bütün yeni metinler Türkçe kaynakla `Loc` üzerinden yazılır; İngilizcesi `strings.txt`'e girer (§9 sözlüğü).
   Sayılar `DeskFormat` ile biçimlenir.
7. **URP:** `PC_RPAsset` değiştirilmez (gerekenler zaten açık). Görüntüler farklı çıkarsa önce sahne ışıklarını
   ayarlarım, asset'e dokunmadan önce sorarım.

---

## 7. Açık sorular

> **Yanıt (2026-10-26):** Kullanıcı on sorudaki önerilerin hepsini onayladı. Uygulanacak olanlar: (1) fikstür
> enerjisiz hâli bellekte kurar · (2) builder Aşama 10'da silinir · (3) Boşluk yalnız diziyi atlar · (4) ayrı
> `liberation_desk.json` sunum dosyası, özet replay'den · (5) `RaidLauncher`'a ekleme olarak geçiş kancası, eski
> operasyon raporu modalı kalır · (6) rapor satırları `Balance`'tan, toplam simülasyondan, gerekirse düzeltme satırı ·
> (7) H brifing belgesini açar · (8) Big Shoulders yerine CP 700 + şablon maskesi · (9) AÇLIK fişi ve damgası ·
> (10) zafer/oyun sonu kapsam dışı.

1. **ENERJİSİZ hâli kurallarla oluşamıyor.** Fikstürde ve testte bu hâli nasıl kurayım?
   - (a) *Önerim:* Yalnız editörde çalışan fikstür, bellekteki simülasyonda `Shelter.Rooms` alanlarını doğrudan ayarlar.
     Kayıt dosyasına yazılmaz, simülasyon kodu değişmez. Görünüm bu hâli doğru çizer; oyunda bu hâle düşülemez.
   - (b) Fikstürden çıkarılır; ENERJİSİZ yalnız `RoomView` birim testinde sınanır.
   - Bu hâlin oyunda gerçekten oluşabilmesi isteniyorsa bu bir kural değişikliğidir ve bu işin kapsamı dışında kalır.
2. **Kalıcı builder ile AGENTS.md Kural 5 çelişiyor.** Kural 5: “Sahneyi sıfırdan üreten kalıcı bir kurucu YOK; tek
   seferlik script yaz, çalıştır, sil.” PROMPT ise tekrar çalıştırılabilen `Tools/Sığınak/Build Room` istiyor.
   *Önerim:* Builder Aşama 2–9 boyunca kalır ve tekrar çalıştırılabilir olur. Aşama 10'da sahne asıl kaynak olarak kalır,
   builder silinir. Alternatif: builder kalıcı olur ve AGENTS.md güncellenir.
3. **Boşluk tuşu.** Bugün tek basışta vardiyayı onaysız bitiriyor; tasarımda yalnız diziyi atlıyor. Eski davranışı
   değiştirmemin onayını istiyorum (önerim: değiştir; vardiya yalnız şalter / iki adımlı Enter ile biter).
4. **Kayıt biçimine girmeyen sunum durumu.** Teleks geçmişi ve satır kodları, EVRAK'taki son 5 rapor, “kayıttaki
   poz”, K7'deki son rapor ve “Vardiya 014 · özgür 1/12” özeti `GameRecord`'da yok.
   - (a) Yalnız oturum boyunca tutulur; kayıttan devam edince teleks boş başlar, EVRAK'ta yalnız brifing olur. Özet,
     başlangıçta kaydın bir kez `Replay.Restore` ile yürütülmesinden hesaplanır.
   - (b) *Önerim:* Kaydın yanında ayrı bir sunum dosyası (`liberation_desk.json`: son 5 rapor, teleks son 30 satır, poz).
     Kayıt biçimi değişmez; dosya kayıtla eşleşmezse yok sayılır. Özet yine replay'den hesaplanır.
5. **RaidLauncher'a geçiş kancası (Game assembly).** 2,3 s'lik CRT dizisi için `RaidLauncher`'ın yükleme öncesinde
   yeni sunumun geçişini beklemesi gerekiyor. Bugün kendi 0,8 s'lik düz yaklaşmasını yapıyor. Silme ya da yeniden
   adlandırma olmadan, yalnız ekleme olarak kanca (`IRaidTransition` alanı + setter) eklememe onay veriyor musun?
   - Bununla bağlantılı ikinci konu: Baskından dönünce bugün eski kâğıt stilindeki “operasyon raporu” modalı açılıyor
     (`SimDeskBuilder.LiberationRaidReport` → `EventModal`). Tasarım bu ekranı yeniden çizmiyor. *Önerim:* Modal olduğu
     gibi kalsın; K3 dizisi (damga, silinme, adaylar) modal kapandıktan sonra başlasın.
6. **Vardiya raporundaki ayrı satırlar.** Simülasyon yalnız net değişimi saklıyor (`LastSuppliesChange`). Toplama
   (+2,0), tüketim (−1,6) ve atölye üretimi ayrı ayrı yok. Bu satırlar ancak `Balance` değerleri × snapshot sayılarıyla
   sunumda hesaplanabilir; bu da PROMPT'taki “formülü yeniden yazma” kuralına dokunuyor.
   *Önerim:* Ayrı satırlar `Balance` değerlerinden okunarak hesaplanır, toplam satırı simülasyonun net değeridir.
   İkisi tutmazsa (depo sınırı ya da açlık) araya “depo sınırı” / “açlık −%50” düzeltme satırı girer; böylece rapor hep
   simülasyonla aynı sonuca çıkar.
7. **KILAVUZ (H) ne gösterecek?** Tasarımda bu ekran yok. Bugün H hikâye kâğıdını yeniden açıyor.
   *Önerim:* H, EVRAK'taki brifing belgesini okuma görünümünde açsın (K7 kâğıdı, BAŞLA yerine “Kapat”). Karar 3/B'deki
   “sığınak planı” sayfası sonraya kalsın.
8. **Big Shoulders Stencil.** Font projede yok. *Önerim:* Önce PROMPT'taki yedeği kullanayım (Courier Prime 700 + şablon
   köprüsü maskesi). Gerçek fontu istersen OFL dosyasını sen ekle ya da indirmem için onay ver.
9. **Açlık hâli tasarımda yok.** Simülasyonda `Shelter.Starving` var: erzak 0 olunca üretim yarıya iner ve revir
   iyileştirmesi durur. K4 ve K8'de karşılığı yok. *Önerim:* ERZAK sayacının altında kalıcı kırmızı “AÇLIK” fişi,
   özet panosunda ERZAK satırında damga. Raporun düzeltme satırı da buna bağlanır.
10. **Zafer ve oyun sonu.** 36 sektör alınınca `End("silme")` → eski `GameOverView` açılıyor. Kapsam dışı bırakıyorum;
    yanlışsa söyle.
11. **(Aşama 1'de çıktı) URP gölge atlası.** `PC_RPAsset`'te ek ışık gölge atlası 2048. Bugünkü sahnede bile Unity,
    nokta ışığın 6 gölge haritasını sığdırmak için çözünürlüğü düşürdüğünü yazıyor. PROMPT tavan ampulüne 1024 (nokta
    ışık = 6 harita), masa lambasına 2048 istiyor; bunlar 2048'lik atlasa sığmaz. *Önerim:* atlası 4096'ya çıkarmak
    (`m_AdditionalLightsShadowmapResolution`; bütün sahneleri etkiler, GPU belleği 4096² derinlik dokusu, ~32–64 MB). Alternatif: asset aynı kalır,
    lamba 1024 ile yetinir. **Yanıt (2026-10-26): 4096 yapıldı.**

---

## 8. Riskler

| Risk | Etki | Önlem |
|---|---|---|
| Oda tamamen yeniden kuruluyor (`room.js` 488 satır, yüzlerce primitive) | Aşama 2 uzun sürer, ışık ayarı deneme ister | Builder bölümlere ayrılır (kabuk, masa, raf, CRT); her bölümden sonra K1 render'ıyla karşılaştırılır |
| `SadeMapCanvas`'ın taşınması (ölçek 0,0014 → 0,000625) | Etiket ölçeği (`labelScale` 1,6), hit alanları, `DeskCanvasProjection` ve `MapCoverFitter` hesapları değişir | Taşıma tek noktada (anahtar); harita testleri iki ölçekte de koşar |
| Tarama dokusu uzakta titreyebilir (moiré): K1'de 11 harita px ≈ 4 ekran px | K1'de bölge durumları okunmaz | Mipmap + anizotropi; gerekirse K1 için alfa düşürülür. Gri tonlama kontrolü |
| `RaidLauncher`'ın `hideDuringRaid` / `lockDuringTransition` listeleri eski bileşenleri gösteriyor | Baskında yeni oda görünür kalır ya da girdi açık kalır | Builder iki takımı da listeye ekler; anahtar listeyi etkin tarafa göre süzer |
| Kilit mantığı üç yerde (`LiberationDeskView`, `CommandPostDesk`, `DeskInput`) | Çift tetiklenen kısayollar (bugün `1` iki yerde işleniyor) | Yeni sunumda tek işleyici; eski yol anahtar kapalıyken aynı kalır |
| Fiziksel kamera ile FOV yumuşatmasının çakışması | K1 ↔ harita geçişinde sıçrama | Geçiş sırasında odak uzaklığı ve lens shift birlikte enterpole edilir |
| World Space metinlerin en dar pozda 14 px altına düşmesi (teleks, oda kartları) | Okunurluk kabulü geçmez | Punto tarayıcı Aşama 4'ten başlayarak her aşamada koşar |
| Eğitim yürütücüsü (`DeskTutorialRunner`) eski hedeflere bağlı; kurtuluşta çalışıp çalışmadığı doğrulanmadı | Görünmez hedefe işaret eden not | Aşama 4'te canlı kontrol; gerekirse kurtuluşta eğitim kapatılır (önce sorarım) |
| Eski sahne testleri (`DeskGameScene_*`) eski nesnelere bakıyor | Aşama 10'da testler kırılır | Yeni karşılıklarıyla birlikte güncellenir |
| Ses klipleri yok | Dizilerin sesi olmaz | Adlandırılmış ipuçları yer tutucu olarak bırakılır; liste raporda verilir |

---

## 9. Görsel farklar

Karşılaştırma: `screens_unity/A2_K1.png` (+ `_gray`) ↔ `reference/renders/K1.jpg`. Bölge ortalamaları (RGB, 0–255):

| Bölge | Unity | Referans |
|---|---|---|
| Arka duvar | 131 · 103 · 62 | 134 · 103 · 63 |
| Sol raf | 82 · 69 · 42 | 78 · 61 · 35 |
| CRT | 51 · 66 · 40 | 40 · 57 · 31 |
| Anahtar rayı | 76 · 58 · 30 | 79 · 60 · 31 |
| Masa sağı | 35 · 29 · 17 | 36 · 28 · 16 |
| Bütün kare | 78 · 67 · 45 | 80 · 69 · 47 |

Açık farklar (sahibi olan aşama):
- Sayaç rakamları, plaka yazıları ve pencereler boş (Aşama 4 `CounterBank`). Anahtarların etkin hâli ve lambaları (Aşama 4).
- Harita eski sızma rampasıyla kırmızı, lamba dairesi yok, etiketler küçük (Aşama 5). Ekip figürü yok (Aşama 5).
- Kesit maketinde oda kartları, piyonlar, yuvalar yok; oda ışıkları V.014'e göre sabit (Aşama 6 `RoomView`).
- Teleks kâğıdında satır yok (Aşama 4). CRT ekranında yer tutucu üç satır (Aşama 7).
- Duvar yazısı şablon fontu değil, CP 700 (Soru 8); şablon köprüsü maskesi ve boya dökülmesi yok (sonra decal ile).
- Referans ACES ton eşlemeli; URP'de post-processing yok. Unity görüntüsü vurgularda biraz daha doygun, CRT çevresi biraz
  daha yeşil (sızıntı ışığı 0,3).
- Yakalama aracı notu: `CorrigibleDebug.Capture` Overlay canvas'ları kameraya 1 m'de bağlıyor; yeni odanın 1 m'den yakın
  nesneleri (anahtar rayı) bu yakalamada kâğıdın önüne çıkıyor. Oyunda Overlay her zaman üsttedir; Aşama 4'te yakalama
  düzlemi kameranın yakın düzlemine çekilecek.

**Aşama 5 (K2/K3, `screens_unity/A5_*.png` ↔ `reference/screens/K2-TR`, `K3`):**
- Yerleşim ve içerik eşleşiyor: görev fişi sağda (Asya üstü), onay fişi sağ altta (Hint Okyanusu), ip, lejant sol altta,
  sayaç rafı ve anahtar rayı aynı yerde.
- Bölge adları ve sınırları oyunun 12 bölgesidir (Rusya, Çin, Japonya-Kore, Hindistan…); panodaki Avrasya, Orta Amerika
  gibi adlar tasarım örneğidir. Etiket yerleri Sade verisinden; bazıları referanstakinden 20–60 px kaymış.
- Tarama ince doku çizgisiyle çiziliyor; referanstaki kalınlığa kazançla (×1,8) yaklaşıldı. Moiré görülmedi.
- Yağlı kalem halkası paket sprite'ı (ince gri çizgi) iki kez basılarak koyulaştırıldı; referanstaki kadar kalın değil.
- Ekip figürünün tabanı masa lambası altında referanstan daha turuncu (metal .7). Aşama 6/10'da ışıkla birlikte bakılacak.
- ÖZGÜR damgasındaki küçük yazılar (bant ve vardiya kodu) referansta da ~9 ekran px; dekor sayıldı.

**Aşama 6 (K4, `screens_unity/A6_K4*.png` ↔ `reference/screens/K4-TR`, `K4-EN`):**
- Yerleşim, kartlar, damgalar, piyonlar, pano içeriği ve kenar satırı eşleşiyor. Maliyetler gerçek denge değerleri
  (jeneratör geliştirme 12 fon + 8 malzeme; panoda 20 + 14 örnek).
- Bölge ortalamaları (RGB, Unity / referans): yaşam alanı duvarı 252 · 231 · 150 / 225 · 204 · 157 (bizde biraz daha sarı ve
  parlak, ton eşleme yok) · jeneratör 175 · 147 · 100 / 158 · 134 · 87 · atölye (enerjisiz) 55 · 39 · 19 / 37 · 22 · 7 · revir
  57 · 45 · 27 / 53 · 39 · 20 · pano 215 · 206 · 188 / 206 · 198 · 179 · sol beton duvar 18 · 16 · 10 / 54 · 44 · 21 (bizde koyu:
  oda ampulü dışındaki dolgu ışığı az; çoğu pano arkasında kalıyor).
- Pano kâğıdı canvas içinde ışıksız krem (referanstaki düz parlak pano); kıskaçlı panonun sağ kenarı K4 açısında maket
  çerçevesinin arkasında kaldığından içerik sağdan 70 px içeride.
- Odaya “yeni eşya” gelmiyor; geliştirme yalnız ışık, damga ve kart satırıyla görülür.

**Aşama 7 (K5, `screens_unity/A7_*.png` ↔ `reference/screens/K5a`, `K5b`):**
- K5a: yerleşim, satırlar, test deseni ve ekran tonu referansla aynı. İpucu bizde üç satıra kırılıyor (test desenine
  değmesin diye sağdan 260 px kenar boşluğu); referansta iki satır.
- K5b: satır y konumları referansla eşleşiyor (106 · 388 · 480 · 540 · 598 · 971). Tahtadaki K, T, E işaretleri yok.
- Izgara, baskın kamerasının gerçek görüntüsüne karşı denetlendi (karo derzleri üst üste).

**Aşama 8 (K6, `screens_unity/A8_*.png` ↔ `reference/screens/K6a`, `K6b`):**
- K6b: rapor kâğıdı, bölümler, satır sırası, sayılar, damgalar, Kalam notu, ALFA etiketi ve dört fiş referansla aynı yerde.
  Kâğıdın alt kenarı 940 px (referans 975): satır aralığı biraz daha sıkı. YAPILAMADI satırı fikstürde gerçek bir komuttan
  (“Atölye geliştirme: Oda en yüksek seviyede.”); referanstaki “Revir onarımı: malzeme yetersiz (24 / 19)” bu dengede
  oluşamıyor (revir onarımı 8 malzeme).
- K6a: kadraj, ATLA plakası, şalterde amber ışık, inik kol, dönen sayaçlar referansla aynı. Oda referanstaki kadar kararmıyor
  (ortam ×0,7, ampul ×0,3; ton eşleme yok). Teleks şeridi ortada (referansta sol alt).
- Gri tonlama: “+”, “−”, “±0” işaretleri ve çerçeveli damgalar (KAPANDI, ENERJİSİZ, YARALI) renksiz de ayrılıyor.

**Aşama 9 (K7, `screens_unity/A9_*.png` ↔ `reference/screens/K7`):**
- Belge, dosya, sekme, ataş, GİZLİ damgası ve BAŞLA referansla aynı yerde; gövde satırları aynı yerden kırılıyor. İmza
  çizgisi referansta yazının genişliğinde, bizde 220 px sabit. Sayaçlar oyunun gerçek başlangıç değerleri (FON 60,0;
  referansta 40,0 örnek).
- “İŞARETLER” lejantı ve harita belgenin arkasında referanstaki gibi kalıyor. Kahve halkası soluk (α .16), referanstaki gibi.
- Gri tonlama: KAYIT YOK sekmesi soluk zemin + kesik kenarla renksiz de ayrılıyor.

**Aşama 10 (tam set, `screens_unity/K1…K7.png`, `_EN`, `_gray` ↔ `reference/screens/`):**
- Bütün pozlar TR ve EN'de taşmasız (iki EN taşması düzeltildi). Gri tonlamada durumlar biçimle ayrılıyor: tarama yönleri,
  kesik / sürekli damga kenarları, ters video ekip satırı, ±0 / + / − işaretleri.
- Teleks şeridi artık her pozda görünür (K1 ve K2 referanslarında yok; okunurluk kararı, yukarıda).
- K3'te aday seçimi oyunun bölge sırasındaki ilk aday (Kuzey Amerika); referans Avrasya'yı gösterir.

---

## CHANGELOG

- **2026-10-26/27 · Maketin sarı çerçevesi kaldırıldı, hedef kutuları modele oturtuldu:**
  - **Sorun:** kesit maketinin üzerine gelince çıkan sarı çerçeve maketin çok dışına (ekran kenarı, sayaç rafı, teleks)
    taşıyordu. Çerçeve ~150 parçanın dünya eksenli sınır kutularından hesaplanıyordu; 35° dönük rafta kutu derinliği de
    kapsıyor. Aynı hata kurucunun çarpıştırıcılarında da vardı: dönük beş hedefin kutusu modelden çok büyüktü (maket
    1,12 × 1,09 m, model 0,80 × 0,34 m; maketin 45 cm önündeki teleks bile “SIĞINAK” etiketi çıkarıyordu).
  - **Hedef kutuları:** maket, CRT, teleks, vardiya şalteri ve evrak tepsisinin çarpıştırıcıları kendi eksenlerindeki mesh
    sınırlarına + 5 mm pay oturtuldu (tek seferlik script, sahnede). Test:
    `SiginakRoomTests.RoomTargets_CollidersFitTheirModels` (hiçbir hedefin kutusu modelini 5 cm'den fazla aşmaz). Oyunda:
    fare maketin üstünde → SIĞINAK etiketi; CRT üstünde → CRT; maketin önünde → teleks.
  - **Büyük hedef çerçevesi kaldırıldı** (kullanıcı kararı; K1 notundaki “büyük hedeflerde sarı çerçeve” uygulanmıyor).
    Çerçeve önce çarpıştırıcının yerel kutusundan sıkı çizilecek biçimde düzeltildi, sonra tümüyle kaldırıldı:
    `HoverHighlight`, sahnedeki `SiginakHud/Highlight` nesnesi, `DeskTarget`'ın `frame`, `highlight` ve `bounds` alanları.
    Üzerine gelince nesne yine 4 mm kalkar ve kâğıt etiket çıkar. Sarı çerçeve yalnız seçili odada kalır.
  - **Seçili oda yalnız K4'te:** açıklıktaki sarı çerçeve, karttaki iç sarı çerçeve ve açık kart kâğıdı artık yalnız raf
    pozunda görünür (`RoomView.SelectionShown`, `SiginakShelter` poza göre kurar). K1'de maket bütün olarak tek hedeftir;
    referans K1'de de oda çerçevesi yok. Seçim durumu korunur, K4'e dönünce aynı oda seçili. `K1.png`, `K1_EN.png`,
    `K1_gray.png` yeniden çekildi.

- **2026-10-26 · Aşama 10 sonu: eski görünüm ve kurucu kaldırıldı** (Aşama 1–10 önce `eed2824` olarak commit edildi):
  - **Sahne asıl kaynak** (AGENTS kural 5): sahne tek seferlik bir editör script'iyle yeni görünüme kalıcı geçirildi (script
    silindi). `CommandPost3D` altında yalnız `Main Camera` kaldı (`SiginakCameraRig` açık, `CommandPostCamera` bileşeni
    silindi); 13 eski oda nesnesi, `LiberationHudCanvas` (üst HUD, alt çubuk, sonuç notu), eski hikâye kâğıdı
    (`LiberationStoryCanvas`), eski görev fişi canvas'ı (`LiberationMapCanvas`) ve eski kamera pozları silindi. Kâğıt harita
    canvas'ı `SiginakRoom/Desk/MapMount` altında; ortam ışığı (üç renkli, yansıma .15) sahnenin ışık ayarına işli;
    `RaidLauncher`'ın monitör pozu CrtDive. Baskında gizlenecekler listesinden yalnız silinen nesneler düştü (60 → 21).
    Eksik script 0; sahne dosyası ~35 000 satır kısaldı.
  - **Baskın bağlantısı** `SiginakRaidLink` (Bridge, sahne kökünde, odanın dışında): açılışta `RaidLauncher`'a CRT geçişini ve
    emirden sonraki 0,3 s beklemeyi verir; baskın sahnesi ek yüklüyken odayı gizler. Sunum anahtarından yalnız bu kısım kaldı.
  - **Silinen kod:** `SiginakRoomBuilder*.cs` (12 dosya), `SiginakMaterials`, `SiginakMeshLib` (yalnız kurucuya hizmet
    ediyordu; mesh ve malzemeler `Art/Siginak` altında varlık olarak kalır), `SiginakPresentationSwitch`, `CommandPostCamera`,
    `CommandPostControl`, `IDeskCameraRig`, “Görünümü değiştir” menüsü. `SiginakSettings` yalnız `hopeMarks` taşır (ışık ve
    kamera ayarları kurucunun girdisiydi, değerleri sahnede). `LiberationDeskView` eski HUD alanlarından ve dallarından
    arındı: komut kapısı, teleks gerekçesi, açılış ve hikâye kilidi. `CommandPostDesk` yalnız girdi kapısı
    (`ExternalBlocked`, `InteractionBlocked`, `PointerOverOverlay`). Kalan editör araçları: `SiginakCapture`,
    `SiginakCaptureSet`, `SiginakTextScan`, `SiginakFixtures`, token içe aktarıcı, `SiginakTextureBaker`.
  - **Oda artık sahnede düzenlenir**; yeniden üretme yolu yok. Kurucunun son hâli `eed2824`'te
    (`Desk/Scripts/Editor/Siginak/SiginakRoomBuilder*.cs`); bölüm 5'teki anahtar tarifi tarihsel kayıttır.
  - **Punto taraması kâğıt haritayı da kapsıyor:** harita canvas'ı kayıtlı sahnede odanın dışında olduğu için önceki tarama
    onu hiç görmemişti (275 → 393 metin). Görünür harita yazıları (bölge adı 28, alt satır ve lejant 23) eşiği geçiyor.
    Aşama 5'in ÖZGÜR damgası istisnası ölü bir yola (`Free stamp/`) yazılmıştı; gerçek yol (`/Stamp/Word`, `/Stamp/Day`)
    düzeltildi. Sızma kipinin harita işaretleri (`SadeMapCanvas/MapMarkers/`: ekip, düğüm, saha raporu, bölge adı katmanı)
    kurtuluş biçiminde çizilmediği için gerekçeli istisna. Sonuç **0 ihlal**, 113 istisna.
  - Çeviri tablosu: silinen kodun ve eski sahne nesnelerinin 38 metni “#” ile emekliye ayrıldı; yeni metin yok.
  - Testler eski görünüm yerine yeni sahneyi denetler: `SiginakRoomTests` (oda tek sunum; `CommandPost3D`'de yalnız kamera;
    `SiginakRaidLink` bağlantıları), `DeskBridgeTests` (vardiya şalterle biter; ekran katmanları HUD canvas'ının üstünde; emir
    belgesinin BAŞLA'sı Overlay'de, dünya kilidinden bağımsız; görev fişi masadaki haritada, yalnız baskın CRT'ye yaklaşır;
    `LiberationDeskView` bağlantıları), `SiginakCameraTests`, `SiginakCrtTests`, `SiginakDeskTests`, `SiginakMapTests`.
    Tam EditMode **713/713**: kurucunun ve anahtarın 2 testi silindi (`ThreeEuler_MatchesMirroredThreeRotation`,
    `PresentationSwitch_AppliesAndRestores`); `DeskBridgeTests`'in eski hikâye ve görev fişi testleri yeni karşılıklarıyla değişti.
  - Oyunda: yeni oyun → açılış dizisi ve emir belgesi (oda kilitli, harita pozu) → BAŞLA (kayıt uyarısı, ikinci basışta
    onay) → belge tepsiye, kilit açık → şalter → vardiya raporu → tepsiye → baskın emri (FON −12) → CRT “BAĞLANTI
    KURULUYOR” → baskın tahtası (oda gizli) → geri çekil → odaya dönüş, operasyon raporu açık, rig etkin. Konsolda hata yok.
    Kayıt dosyaları geri yüklendi.

- **2026-10-26 · Aşama 10 (dil, erişilebilirlik, performans; kaldırma onay bekliyor):**
  - **Punto taraması** (`SiginakTextScan`, menü “Corrigible/Sığınak/Punto taraması”, rapor `Docs/UI_Siginak/punto_taramasi.txt`,
    EditMode testi): odadaki ve ekran katmanlarındaki 275 TMP metni 1920×1080'de ölçer. Overlay: punto × canvas'a göre ölçek;
    dünya metni: her rig pozunun kamerasıyla izdüşürülür, kadrajda olduğu pozların en büyüğü (okunduğu poz). Sonuç **0 ihlal**,
    39 gerekçeli istisna (tasarımın 13 px tuş fişleri, mantar pano notları ve afiş satırları, ÖZGÜR damgasının küçük yazıları,
    figür plakasındaki “A”, EVRAK plakası ve A5 kâğıt başlığı, masadaki teleks kâğıdı).
  - Düzeltilen ihlaller: `type.stamp` ve `type.taskHead` 13 → 14 (görev fişindeki sektör damgaları, BEDEL/ÖDÜL); K4 panosunun
    satır başlıkları 18 → 22, SEÇİLİ çipi, EKSİK ve AÇLIK damgaları 18 → 22 (pano pikseli K4'te ≈ 0,65 ekran pikseli); KİLİTLİ
    bandı biraz büyüdü. **Teleks:** masadaki kâğıdın satırları hiçbir pozda okunur boyda değil (en çok 5,6 px); satır artık
    kâğıt okunur boyda değilse (≥ 14 px) şeritte de basılır (Aşama 4'te yalnız kâğıt kadraj dışındayken). Satırlar her pozda
    şeride düştüğü için kuyruk birikiyordu: sırada satır varken gösterilen satır en az 1,2 s kalır, şerit inmeden yerini
    sıradakine bırakır; yeni oyun / kayıttan devamda kuyruk boşalır.
  - **İngilizce taşmalar:** onay fişinin “OPEN FRONT IN SELECTED REGION” düğmesi en çok 5 punto küçülür; K4 panosunda oda adı
    satırın çipten kalan genişliğine yayılır ve gerekirse küçülür (“LIVING QUARTERS” SELECTED çipinin altına giriyordu).
  - **Performans** (Play, aynı oyun durumu, kamera 1920×1080 MSAA 4× hedefe 30 × 4 kez çizilip GPU beklenerek; ms/kare):
    | Görünüm | Önce | Sonra |
    |---|---|---|
    | Eski (CommandPost3D) | 2,8–3,2 | 2,8–2,9 |
    | Yeni (K1) | 3,9–4,3 | 3,6 |
    Yapılanlar: hareket etmeyen bütün parçalar statik birleştirilir (Play'de 260 renderer; önce yalnız duvar), küçük parçalar
    (sınır yarıçapı < 6 cm, ekip figürü hariç) gölge düşürmez (tavan ampulünün nokta ışığı her düşüreni altı yüze çiziyordu;
    205 → 140), sayaç çarkları ve anahtar plakaları Play'de property block yerine kendi malzeme kopyasını kullanır (SRP Batcher
    dışına düşüyorlardı). **Kalan fark ~0,7 ms:** odanın içeriği (52 3B yazı ~0,4 ms; oda kartları, pano ve CRT ekranı canvas'ları
    ~0,3 ms; ek ışıklar ~0,15 ms). Ölçülen seçenek: ampul ve lamba gölgeleri yumuşak yerine sert → ~0,5 ms (görünüş değişir;
    karar bekliyor).
  - **Tam görüntü seti** (`SiginakCaptureSet`, menü “Tam görüntü seti (TR/EN, Play)”): K7 açılış, V.014'te K1 · K2 · K4 · K5a ·
    K5b, V.019'da K3 (özgürleşme + aday), V.014 → 015'te K6a · K6b; fikstürlerle kurulur, Overlay dahil çekilir, yakalama
    sırasında odanın fizik ışını kapalı (editör imleci etiket düşürmesin). Çıktı `screens_unity/K1…K7.png`, `K*_EN.png`,
    `K*_gray.png` (27 dosya).
  - **Varsayılan yeni görünüm:** `SiginakSettings.newPresentation = true` (varlık ve sınıf varsayılanı). Sahne dosyası eski
    hâlde kayıtlı; anahtar açılışta (`Awake`) yeni görünümü uygular, eskisi menüden ve ayardan hâlâ seçilebilir.
  - Testler: `SiginakTextScanTests` (sahnede 14 px altı metin yok); `SiginakRoomTests` varsayılanı yeni görünüm olarak denetler.
    Tam EditMode **715/715**.
  - **Onay bekleyen kaldırma** (PROMPT §2: “Eski HUD'u Aşama 10'da, ben onayladıktan sonra kaldır”; Soru 2: kurucu Aşama 10'da
    silinir). Aşama 1–10'un hiçbiri commit edilmedi; kurucu odanın tek yeniden üretim yolu olduğu için önce commit öneriyorum.
    Kaldırılacaklar: (1) sahnede `CommandPost3D` altındaki 13 eski nesne (Environment, Table, MapMount, StatusNote, Radio,
    RadioTranscript, Fax, Ledger, Files, Tabs, Lamp, LiberationRoom, LiberationHudCanvas: üst HUD, alt çubuk, sonuç notu), eski
    hikâye kâğıdı ve eski görev fişi canvas'ı; (2) `LiberationDeskView`'ün eski HUD alanları ve dalları, eski görünüme dönüş
    (`SiginakPresentationSwitch`'in eski hâle döndüren kısmı, `SiginakSettings.newPresentation`, “Görünümü değiştir” menüsü),
    yeni görünüm sahneye kalıcı uygulanır; `CommandPostDesk` ve ana kamera kalır (yeni görünüm de kullanıyor); (3) kurucu
    (`SiginakRoomBuilder*.cs`, 12 dosya; `SiginakMaterials`, `SiginakMeshLib` yalnız kurucuya hizmet ediyor), sahne asıl kaynak
    olur (AGENTS kural 5); `SiginakCapture`, `SiginakTextScan`, `SiginakCaptureSet`, `SiginakFixtures`, token içe aktarıcı kalır;
    (4) eski görünüme ve kurucuya bakan testler yeni hâle göre güncellenir.
- **2026-10-26 · Aşama 9 (brifing ve açılış, K7):**
  - **Açılış dizisi** (`OpeningSequence`; yeni oyunda `LiberationDeskView.NewGame` eski hikâye kâğıdı yerine
    `SiginakBriefing.OpenOpening`'i çağırır): kamera harita pozuna oturur; 0–0,8 s siyah (HUD canvas'ının en altında tam
    ekran örtü; ATLA plakası ve teleks şeridi üstünde kalır, jeneratör marşı sesi yer tutucu); 0,8–1,4 s tavan ampulü üç
    kırpışmayla yanar, örtü ışığın tersiyle açılır; 1,4 s teleks “001/00 KYGA-07 HAT AÇIK” (`TelexFeed.Post(…, header)`:
    “/00” satırı, sayaç ilerlemez); 1,5 s emir belgesi teleks yönünden kayıp haritaya düşer (0,35 s), okuma görünümüne kalkar
    (0,25 s), manila dosya belirir. Boşluk ya da ATLA plakası açılışı keser, belge animasyonsuz açılır.
  - **Emir belgesi** (`BriefingView`, HUD canvas'ında 760 px; K7 ölçüleri): KYGA · KÜRESEL YAPAY ZEKÂ GÜVENLİK AJANSI /
    EMİR NO. 001, HAREKÂT EMRİ, KİME / KONU, GİZLİ damgası (−8°), dört paragraf daktilo gövde (CP 400 20, satır 1,5; son
    satır kalın), imza sprite'ı + KYGA MERKEZ KOMUTANLIĞI / Vardiya 001 · analog hat, mürekkep düğme “Enter · BAŞLA”
    (300 × 58), ataş, arkada manila dosya (+2°) ve kahve halkası. Sağ kenarda **KAYITTAN DEVAM** sekmesi: etikette
    “V.004 · özgür 0/12” (kayıt bir kez yeniden oynatılarak; `GameController.SavedRecord()` yalnız ekleme) ve “Tab · dosyayı
    aç”; üzerine gelince ya da odakta 6 px dışarı kayar, Tab odağı sekmeye geçirir (sarı çerçeve). Kayıt yoksa sekme soluk
    (#CFC6AE), kesik kenarlı KAYIT YOK damgası; tıklanmaz, Tab atlar.
  - **BAŞLA:** kayıt varken ilk Enter teleks uyarısı “KAYIT VAR · V.004 kaydının üstüne yazılır · onay: Enter” (K7 notundaki
    “yeni harekât V.014 kaydının yerine geçer · onay için yeniden Enter” 820 px'lik teleks şeridine sığmıyordu) ve düğme
    “BAŞLA · ONAY”; ikinci Enter onaydır. Kayıt yoksa tek Enter. Sonra ALINDI damgası (0,1 s), belge
    küçülerek EVRAK tepsisine (0,4 s), kamera K1'e (0,8 s). Oynanmamış oyun zaten kaydedilmez (`GameController.Save`); ilk
    komut eski kaydın üstüne yazar, bu yüzden onay ondan önce alınır. Uyarı türü teleks satırları teleks kadrajdayken de şeritte
    görünür.
  - **KAYITTAN DEVAM** (tık ya da Tab → Enter): `G.ContinueSaved()`; belge tepsiye, dosyadaki son vardiya raporu 0,3 s
    görünür ve tepsiye döner (`SiginakShift.FlashLatest`), kamera kayıttaki poza geçer. Poz yan dosyaya yazılır
    (`liberation_desk.json › pose`: harita / raf / masa).
  - **H ve EVRAK:** H (ya da KILAVUZ anahtarı) belgeyi okuma kipinde açar (dosya ve sekme yok, “Enter · KAPAT”; Soru 7).
    EVRAK tepsisinde belge raporların altındadır: rapor yokken tepsi belgeyi açar, raporlar okunurken ilk rapordan ← belgeye
    geçer. Rapor yokken tepsideki A5 kâğıt “HAREKÂT EMRİ”dir. Esc belge açıkken AYARLAR'ı açar (LateUpdate'te; DeskInput
    aynı karede kapatmasın), okuma kipinde belgeyi kapatır.
  - **Hata düzeltmesi (Aşama 4'ten):** oynanmamış yeni oyunda basılan teleks satırı `liberation_desk.json`'u yeni oyunun
    tohumuyla yazıyordu; kayıttan devam edince eski kaydın teleksi ve raporları kayboluyordu. Yan dosya artık
    `GameController.Save` ile aynı kuralla yazılır (gün 0 ve komut yoksa yazılmaz).
  - **Kararlar:** ALINDI damgası “ALINDI · V.001” (yalın “ALINDI” baskın ekranında ganimet anlamında çevrili: COLLECTED; ayrı
    anahtar EN “RECEIVED · S.001”). GİZLİ mevcut çevirisiyle CLASSIFIED (sözlükte TOP SECRET; aynı anahtar Sade masasında
    da kullanılıyor). Belge haritaya “düşerken” ayrı bir World Space A4 yok; Overlay belge küçük ve eğik başlayıp büyür.
  - Yeni tokenlar (UYGULAMA EKİ): `motion.opening.*`, `colors.ui.folderOff`, `colors.ui.paperclip`, `type.folderTabSub`.
  - Play doğrulaması (yeni görünüm): açılış karanlıktan kırpışarak açıldı, ATLA plakası; kayıtla: Enter → KAYIT VAR uyarısı
    + “BAŞLA · ONAY”, Enter → belge tepside, kamera K1, kayıt dosyası değişmedi; H → okuma kipi, Esc kapattı (ayarlar açılmadı);
    Boşluk açılışı 0,01 s'de kesti; ikinci oturumda Tab → sekme dışarıda + sarı çerçeve, Enter → gün 1'e devam, vardiya 001
    raporu göründü, kamera kayıttaki raf pozuna döndü; kayıtsız: KAYIT YOK, Tab yok sayıldı, tek Enter başlattı, kayıt
    dosyası yazılmadı; rapor yokken tepsi belgeyi açtı. EN: OPERATION ORDER · CONTINUE · BEGIN. Konsol temiz.
  - Görüntüler: `screens_unity/A9_K7.png`, `A9_K7_EN.png`, `A9_K7_opening.png` ve `_EN` (1,0 s, kırpışma), `A9_K7_armed.png`
    (BAŞLA · ONAY, şeritte KAYIT VAR), `A9_K7_tab.png` (Tab odağı), `A9_K7_continue.png` (son rapor görünüyor), `A9_K7_reading.png` (H),
    `A9_K7_nosave.png`, `A9_K7_gray.png`, `A9_K7_nosave_gray.png`.
  - Testler: `SiginakBriefingTests` (7 vaka: kipler ve uyarı metni, kayıt özeti yeniden oynatmayla, TR ≤ 520 / EN ≤ 680,
    açılışın atlanması, teleks “/00” satırı, EVRAK'ta belge ve raporlar, sahne bağlantıları). Tam EditMode **714/714**.
  - Ertelenenler: sesler (jeneratör marşı, ampul cızırtısı, kâğıt, damga “tak”, dosya kapağı) yer tutucu · oyuncunun imzası
    (BAŞLA anında soldan açılan) yok · “dosya açılır” anı ayrı animasyon değil (belge tepsiye kayar, rapor görünür).
- **2026-10-26 · Aşama 8 (vardiya geçişi ve rapor, K6):**
  - **Akış:** kol inince (`SiginakShift`) kapanış öncesi anlık görüntü (`ShiftReportBuilder.Capture`), vardiyanın fon
    hareketleri (`Ledger`: simülasyonun işlem defterinden, gelir hariç) ve kapanan vardiyanın teleks YAPILAMADI satırları
    alınır; vardiya hemen bitirilir (simülasyon ilerler, kayıt yazılır). Sunum `ShiftSequence` çizelgesiyle yetişir: sayaçlar
    ve ekip figürü o ana kadar eski hâlde bekler (`SiginakDesk.Refresh` / `SiginakMap.HoldFigure`).
  - **Dizi (K6 notu, 0,00–2,60 s):** 0,00 kol iner, kamera K6a'ya (0,4 s) · 0,05 tavan ampulü üç titremeyle %30'a (0,35 s),
    masa lambası %60 · 0,15 şaltere amber nokta ışık (#FFB060 0,35, 0,8 m; 0,2 s) ve oda kararır (ortam ×0,7) · 0,40 sayaçlar
    sırayla döner (VARDİYA 0,6 s, modüller 0,3 s arayla, çark başına 0,12 s) · 0,60 teleks “VARDİYA 014 KAPANDI · Rapor
    basılıyor…” (teleks kadraj dışındaysa alt şerit) · 1,40 jeneratör: ampul ve lamba 0,3 s'de geri, amber söner · 1,60 kamera
    harita pozuna (0,6 s) · 2,20 rapor teleksten koparılıp 3° dönerek düşer (0,35 s), fark fişleri sallanarak iner ve rapor
    açıkken asılı kalır, iyileşen ekibin figürü S-07'den cepheye kayar (0,6 s) ve teleks “ALFA CEPHEDE” basar · 2,60 kol kalkar.
    Dizi boyunca sağ üst plaka **Boşluk · ATLA**; Boşluk ya da plaka kalan adımları anında uygular (sayaçlar oturur, kamera
    harita pozunda, rapor düşmeden açık).
  - **Rapor** (`ShiftReportBuilder.Build` → `ShiftReportView`, HUD canvas'ında 600 px manila teleks kâğıdı, K6b ölçüleri):
    başlık TELEKS · KYGA-07 → KOMUTA + teleks kodu, VARDİYA RAPORU · 014, “014 → 015 · bir gün ilerledi”, KAPANDI damgası;
    **FON** açılış (kapanış öncesi fon − vardiyanın hareketleri) · her hareket (baskın emri OP-02, baskın ödülü, onarım /
    geliştirme) · vardiya geliri “2 + 0,5 × n özgür bölge” · (gelirden başka fark olursa “Diğer”) · kapanış; **ÜRETİM VE
    TÜKETİM** erzak toplama (yaşam alanı · n kişi) · tüketim (n kişi × 0,2) · (depo sınırı / erzak tükendi düzeltmesi) ·
    malzeme · atölye (HASARLI / ENERJİSİZ üretim/talep damgası ya da “n kişi × 0,5”) · Erzak a → b / kapasite; Kalam
    **analist notu** (tek öneri: jeneratör açığı → yaşam alanı → revir → boştakiler); **EKİP TOPARLANMASI** “Alfa · yaralı
    2 → 1 gün” + revir cümlesi (çalışıyorsa “n gün hızlandı”, değilse nedeni) + “Vardiya 016'da baskına hazır.” (ekip
    toparlandıysa “Alfa · toparlandı · Ekip cepheye döndü.”);
    **YAPILAMADI** kapanan vardiyanın teleks hataları (en çok 4 + “… ve n satır daha”). Değerler simülasyondan; ayrı satırlar
    Balance oranlarıyla (Soru 6), toplam hep simülasyonun net değişimi. İşaret: “+” yeşil, “−” kırmızı, “±0” gri.
  - **Çıkış:** Enter, düğme (“Enter · VARDİYA 015'İ BAŞLAT”) ya da Esc: rapor EVRAK tepsisine kayar (0,4 s), fişler kalkar,
    kamera masa pozuna döner. Rapor açıkken masa kilitli (`LiberationDeskView.InputBlocked`), şalter devre dışı (KİLİTLİ bandı,
    etiket “Rapor açık: önce EVRAK tepsisine dosyala.”), kamera kısayolları kapalı (`SiginakCameraRig.Locked`).
  - **EVRAK tepsisi** (`EvrakTray`): son 5 raporun girdisi (metin değil; dil değişince yeniden kurulur), tepside son raporun
    A5 kâğıdı. Tepsiye tıklayınca son rapor açılır, ← → ile öncekiler (“TEPSİYE KOY”). Rapor yokken tepsi kılavuzu açar
    (brifing Aşama 9'da). Raporlar `liberation_desk.json` yan dosyasında (kayıt biçimi değişmedi).
  - **Türkçe ekler:** `TrNumber` (rakamla yazılmış sayıya hâl eki, okunuşun son sözcüğüne göre): “VARDİYA 015'İ”,
    “016'da”, “017'de”. İngilizce çeviri eki yok sayar.
  - Yeni tokenlar (UYGULAMA EKİ): `motion.shift.*` (çizelge), `lights.ambient.k6aScale` 0,7, `colors.ui.reportRule`,
    `colors.ui.reportPerforation`, `type.reportStamp`, `type.reportFailure`.
  - Fikstür `K6`: V.014 + Doğu Avrupa 02 baskını başarısız (fon 62 → 50, Alfa 2 gün yaralı) + en üst seviyedeki atölyeyi
    geliştirme denemesi (teleks YAPILAMADI).
  - **ALFA etiketi** (K6b): Aşama 5'teki çıplak kırmızı 3D yazı (~8 ekran px, harita katmanlarının altında kalıyordu) yerine
    figürün altında dizin kâğıdı “ALFA [YARALI] 1 GÜN” (World Space canvas, 1 px = K2'de 1 ekran px, kırmızı çerçeveli damga).
  - **Çizim sırası düzeltmesi:** harita canvas'ları sıra 0'da ve saydam nesneler uzaklığa göre sıralandığından haritanın
    kenarına sarkan ± fişleri ve figürün taşıma gölgesi haritanın altında kalıyordu (K3'te de). Fişler sıra 20/21, gölge 1,
    ALFA etiketi 2 (harita fişleri 10).
  - **Kararlar:** raporun teleks kodu, dizinin bastığı “VARDİYA 014 KAPANDI” satırının kodudur (referansta 014/10 satırı +
    014/11 rapor; ayrı satır basmamak için tek kod). Baskının BAŞARISIZ satırı dizide yeniden basılmaz (baskın dönüşünde
    basılan satırdır). Türkçe ek sayıyla tek yer tutucudur (“VARDİYA {0} BAŞLAT”): İngilizce çeviride yer tutucular aynı kalır.
  - Play doğrulaması (yeni görünüm, fikstür K6): şalter iki adım → gün 13 → 14, fon 50,0 → 52,5, Alfa 2 → 1; dizi sırasında
    masa kilitli, şalter inik, sağ üstte ATLA; 2,6 s'de rapor açık, şalter KİLİTLİ, tepside 1 rapor. Enter → rapor tepsiye,
    kamera K1, şalter hazır, fişler kalktı, tepside “VARDİYA RAPORU · 014” kâğıdı. Tepsiye tıklama → rapor “TEPSİYE KOY” ve
    ← → ile; Esc kapatır. İkinci vardiyada Boşluk (0,5 s'de): dizi 0,03 s'de bitti, sayaçlar oturdu, rapor açık, Alfa
    toparlandı, figür cepheye geçti, teleks “016/01 ALFA CEPHEDE · Ekip toparlandı · Doğu Avrupa”. EN: SHIFT REPORT · CLOSED ·
    START SHIFT 015 · NO POWER 4/6. Konsol temiz.
  - Görüntüler: `screens_unity/A8_K6a.png`, `A8_K6b.png`, `A8_K6b_EN.png`, `A8_K6_tray.png` (tepsiden okuma), `A8_K6_skip.png`
    (Boşluk, ekip döndü, çarklar oturmuş), `A8_K6a_gray.png`,
    `A8_K6b_gray.png`.
  - Testler: `SiginakShiftTests` (9 vaka: K6b'deki V.014 → 015 satırları ve toplamları, YAPILAMADI süzgeci ve taşma, işaretler +
    düzeltme satırları + defter etiketleri, ekip toparlanması, simülasyon defterinden etiket sayıları, Türkçe ekler, dizinin
    atlanması, JSON gidiş-dönüş, sahne bağlantıları). Tam EditMode **707/707**.
  - Ertelenenler: sesler (klak + röle, tık, yırtılma, havalandırma uğultusu) yer tutucu · jeneratör “yük alır” yalnız ışıkla
    (makette jeneratör hareketi yok) · teleks şeridi ekranın ortasında (Aşama 4 düzeni; K6a'da sol altta) · tepsideki A5
    kâğıt yalnız başlık ve çizgiler · brifing tepsiye Aşama 9'da girer (rapor yokken tepsi kılavuzu açar) · İngilizcede tekil /
    çoğul ayrımı yok (“DAYS: 1” biçimiyle kaçınıldı).
- **2026-10-26 · Aşama 7 (CRT, K5):**
  - **Geçiş kancası** (Game, yalnız ekleme, Soru 5): `IRaidTransition` (yaklaşma süresi, yaklaşma başlangıcı, sahneden önce
    beklenen dizi, sahne açıldı, dönüş) ve `RaidLauncher.Transition`. Başlatıcı kanca varsa yaklaşmayı onun süresiyle yapar,
    yüklemeden önce dizisini bekler (giriş kilitli), sahne açılınca ve kapanınca haber verir. Kanca yoksa (eski görünüm)
    akış birebir aynı. Sunum anahtarı yeni görünümde kancayı (`SiginakCrt`) yazar, eskide boşaltır.
  - **CRT ekranı** (`CrtScreen`, World Space 1024 × 768 = 0,30 × 0,225 m, tek renk fosfor): SAHA YAYINI · KANAL 07,
    durum satırı, ekip ve cephe satırları, ipucu, 0,53 s'de yanıp sönen ▮, sağ altta %28 test deseni (iki daire, artı,
    K-07), tarama çizgisi ve köşe kararması sprite katmanları. Durumlar (`CrtViewBuilder.State`): bekleniyor · kuruluyor
    (emir verildi, sahne açılmadı; statik) · yayın açık · kesildi (bu vardiyada baskın: “BAĞLANTI KESİLDİ · SONUÇ TELEKSTE”) ·
    devre dışı (ekip yaralı; ekip satırı **ters video**, renk değil biçim). Üzerine gelme etiketi K5a'daki gibi
    “SAHA YAYINI / Yalnız baskın emriyle açılır. Emri görev fişinden ver (3).”
  - **Baskın geçişi** (`CrtTransition`, sahnenin kökünde Overlay, sıra 60; oda kökü baskında gizlendiği için dışarıda):
    çerçeve + cam (34/22 kenar, köşe örtüsü `ui_crt_glass_cover` 72 px: SiginakTextureBaker'da üretilir), RectMask2D'li tüp,
    8 × 8 izometrik ızgara (tokens_sade › raid.camera.screenFromGrid1080; köşeler 210,528 · 960,98 · 1710,528 ·
    960,958), statik (α .55, her kare rastgele ofset), 96 px yuvarlanan bant (1,2 s), BAĞLANTI KURULUYOR (CP 700 76 + 7 px
    hayalet α .22), SENKRON 10 kutu + yüzde, hedef ve kamera satırları, “Boşluk atla”, KYGA-07 · ANALOG HAT. Zaman çizelgesi:
    0,00 damga · 0,30 kamera CRT'ye (0,5 s, son 0,25 s'de katman belirir) · 0,80 tüp 4 px çizgiden 0,15 s'de açılır · 0,95
    statik + başlık, kutular 0,1 s arayla · 2,00 ızgara parlar, statik %20 · 2,30 sahne açılır, katman 0,2 s'de söner.
    Dönüş 0,6 s (kesildi yazısı, tüp çizgiye kapanır); kamera ve görev fişi önceki hâline döner. **Boşluk** diziyi atlar.
  - **Doğrulama:** baskın sahnesinin görüntüsüne formül ızgarası çizildi, karo derzlerine birebir oturuyor. Play: emir → 0,88
    s'de tüp açılışı, ~2,3 s'de sahne açıldı, katman söndü; sonuç → masaya dönüş, CRT “BAĞLANTI KESİLDİ”; Boşluk'la atlanan
    dizide ızgara 0,92 s'de parlak (α .34), atlanmayanda α .20; yaralı ekipte ters video satır.
  - **Hata düzeltmesi (Aşama 5–6'dan):** `SiginakMap` ve `SiginakShelter`'daki `Reset()` adı Unity'nin düzenleyicide bileşen
    eklenince çağırdığı özel yöntemdi; kurucu her `AddComponent`'te boş alanlarla çağırıyordu (konsolda NullReferenceException).
    `ResetState` oldu; kurulum konsolu temiz.
  - `Height` kurucu yardımcısı sabit yükseklikte `flexibleHeight = 0` yazar (iç düzen grubunun esnekliği sütun boşluğunu
    yutuyordu).
  - Görüntüler: `screens_unity/A7_K5a.png`, `A7_K5a_EN.png`, `A7_K5a_injured.png`, `A7_K5b.png`, `A7_K5a_gray.png`, `A7_K5b_gray.png`.
  - Testler: `SiginakCrtTests` (4 vaka: durum makinesi + ters video, bekleme metinleri, geçiş metinleri, sahne bağlantıları
    ve ızgaranın kamera formülüne oturması). Tam EditMode **698/698**.
  - Ertelenenler: “sağ tuşla bakarken K5a” (yalnız CRT'ye tıklayınca) · 0,30'daki ampul titremesi ve röle/bip/tunk sesleri
    yer tutucu · K5b'deki tahta işaretleri (K, T, E) — baskın tahtası sahne yüklenmeden bilinmediğinden çizilmedi.
- **2026-10-26 · Aşama 6 (sığınak, K4):**
  - **Kesit maketi odaları** (`RoomView`, her odada): çalışan oda aydınlık (ampul + nokta ışık), **enerjisiz** ve **hasarlı**
    oda karanlık (duvar `roomWallOff`, sönük ampul), hasarlıda moloz + düşmüş kiriş + çatlak (revirdeki desen öteki
    odalara da kuruldu; başlangıçta atölye de hasarlı). Kapasite = pirinç disk yuva (seviye × 2, 0,26 m'ye yayılır),
    dolu yuva = piyon (room.js › slot/peg). Pirinç çerçevede dizin kartı (World Space 380 × 110, 0,5 mm/px): ad CP 700 29,
    satır CP 700 21 (en üst seviyede “3/3”), damga HASARLI / ENERJİSİZ / EMİR UYGULANDI (yeni: bu vardiya, kayıttaki
    `shelter_upgrade` komutundan). Seçili: oda ağzına 8 px sarı + 2 px mürekkep, kart #FFFDF5 + iç 4 px sarı.
  - **BOŞTA tepsisi** (`IdleTray`): en çok 6 piyon, tepsiye sığacak kadar sıkışır; plakada “BOŞTA n” (kâğıt etiket).
  - **Sığınak özeti panosu** (`ShelterClipboard`, World Space 600 × 800 kâğıt): SIĞINAK ÖZETİ + V.014, TOPLULUK ·
    ENERJİ (açık kırmızı) · ERZAK (açlıkta AÇLIK damgası, Soru 9) · SON VARDİYA; seçili odanın kartı −1,2°: ad + SEÇİLİ,
    SEVİYE n/3 · PERSONEL, açıklama (Balance sayılarıyla), sarı sonuç notu, maliyet, hazırlık, eylemler. Kart oda
    değişince 0,14 s'de kayar.
  - **Eylemler** (`SiginakShelter` → `LiberationDeskView.UpgradeRoom/AssignRoom`, simülasyon komutları): HASARLI → ONAR ·
    1–2 → GELİŞTİR · 3 → EN ÜST SEVİYE (devre dışı). Düğme hâlleri K8'deki gibi: normal · devre dışı (soluk, kesik kenar) ·
    yetersiz kaynak (kırmızı kenar + EKSİK damgası + “Eksik: 1 malzeme”) · + PERS. dolu odada DOLU. Düğmeler her zaman
    tıklanır: yapılamayan eylem komutu dener, düğme ±3 px sarsılır, gerekçe teleks şeridinde. Başarılı ONAR/GELİŞTİR:
    kartta EMİR UYGULANDI damgası, oda ışığı 0,2 s'de yanar ve bir kez titrer. ± PERS.: piyon tepsi ↔ yuva arasında
    kavisle zıplar (0,3 s).
  - **Sonuç notu** (sunum kuralı, `ShelterViewBuilder.Note`): hasar → enerji açığı (jeneratörde “Atölye için 2 enerji
    eksik: 2 personel ata ya da geliştir.”, tüketici odada “Enerji yok…”) → açlık (yaşam alanı) → boştaki kişi; yoksa not yok.
  - **Raf pozunda tıklama:** K1'de maket bütün olarak “2 SIĞINAK” hedefi; K4'te o çarpıştırıcı kapanır, odalar (ağız +
    kart) tek tek seçilir.
  - **K4 ışığı:** three.js'teki ampul ×2,4 / ortam 0,22 değerleri ACES ton eşlemesiyle yumuşuyor; URP'de ×1 bile yaşam alanı
    duvarı beyaza patlıyordu (254 / referans 225). Referans render'la ölçülerek proje tokenı `lights.roomBulb.k4Scale` 0,55,
    `lights.ambient.k4Scale` 1 (ortamı azaltmak sol duvarı referanstan 3 kat karartıyordu). Geçiş poz süresince.
  - Fikstür: `SiginakFixtures.ApplyShelterV014` (Soru 1: yalnız bellekte; atölye SV3 enerjisiz, revir hasarlı, boşta 4,
    topluluk 8/8, erzak 31/40, malzeme 19, fon 62,0); `Load("V014")` artık sığınağı da kurar.
  - Play doğrulaması (V.014): K4'te kartlar, damgalar, notlar referansla aynı; revir ONAR → “014/01 YAPILAMADI · Revir
    onarımı: Enerji yetersiz…” şeritte; jeneratör GELİŞTİR → seviye 2, enerji 8/6, atölye aydınlanır, EMİR UYGULANDI,
    FON 62 → 50, MALZEME 19 → 11; yaşam alanı − PERS. → piyon tepsiye zıplar (BOŞTA 5). Konsol temiz.
  - Görüntüler: `screens_unity/A6_K4.png`, `A6_K4_EN.png`, `A6_K4_upgrade.png`, `A6_K4_gray.png`.
  - Testler: `SiginakShelterTests` (6 vaka: başlangıç hâlleri, V.014 enerji açığı + not, eksik malzeme/EKSİK, en üst
    seviye ve DOLU, EMİR UYGULANDI bir vardiya, sahne bağlantıları). Tam EditMode **694/694**.
  - Ertelenenler: odada “toz + yeni eşya” (geliştirmede) yok, yalnız ışık ve damga · sürekli sesler (ampul vızıltısı,
    jeneratör uğultusu, çakıl) yer tutucu · V.014 fikstüründe jeneratöre + PERS. devre dışı: simülasyon enerji açığında
    jeneratör personelinin değişmesini engelliyor (“çalışan odaların enerjisini keser”); bu açık kurallarla oluşamadığı
    için yalnız fikstürde görülür (Soru 1).
- **2026-10-26 · Aşama 5 (harita ve görev fişi):**
  - **Kurtuluş haritası** (`LiberationMapLayer`, `LiberationRegionMarks`; Sade harita canvas'ında iki katman, yalnız yeni
    görünümde açık): taramalar bölge dolgusunun üstünde, sınır çizgilerinin altında (“Liberation fills”); kontur, halkalar,
    etiketler, S-07 fişi, kırmızı ip, lejant ve masa lambası dairesi en üstte (“Liberation marks”). Durumlar
    (`LiberationMapBuilder`, simülasyondan): **işgal** çapraz tarama (45° α .50 + −45° α .32) · **cephe** tek yön tarama
    (%33,3'te 22 px), 5 px mürekkep kontur, yağlı kalem halkası, raptiye, sektör işaretleri + yüzde · **aday** tarama +
    2,5 px kesik grafit kontur (12/8) + kesik halka + “%100 · ADAY” · **özgür** α .07 hayalet iz, mavi ÖZGÜR damgası
    (V.xxx; taze α .97, sonra .86), ✓✓✓ · **seçili aday** sarı α .34 + etiket çipi + kalın halka · **üzerinde** sarı α .12.
    Etiket CP 700 28 harita px (K2'de 17,6 ekran px), alt satır 23 (14,5), hale `Sade Map Label`. `SadeMapView`'a yalnız
    ekleme: `SetLiberationStyle` (Sade düğüm, etiket, iplik, jeton gizli; dolgu kâğıt). `RegionMeshGraphic`'e `Hovered`,
    `LargestOuterRing`, `CopyGeometryFrom`.
  - **Görev fişi** (`OperationSlip`, World Space, haritanın 2 mm üstü, −1°, 436/452 px): başlık (OP-02 · bölge · SEKTÖRLER
    1/3 · İŞGAL %66,7 · kırmızı çizgi), 3 sektör satırı (işaret, no, ad, tür, damga; BASKINA HAZIR satırı sarı), görev,
    BEDEL / ÖDÜL (sayılar kalın), ekip satırı (HAZIR / YARALI · n GÜN / SAHADA), mürekkep düğme “Enter · BASKIN EMRİ VER ·
    −12 fon”, × Esc. Cephe dışı bölgede bilgi fişi. Karar 3 uygulandı: fon/ekip eksikken de ilk sektör BASKINA HAZIR,
    düğme kapalı ve gerekçe sayılı. Emir: kırmızı “EMİR VERİLDİ” damgası 0,1 s'de iner; `RaidLauncher.TransitionDelaySeconds`
    (yalnız ekleme, varsayılan 0) yeni görünümde 0,3 s: damga görünür, sonra kamera CRT'ye.
  - **Yeni cephe onay fişi** (`FrontSlip`, 404 px, +1,2°): YENİ CEPHE · ONAY, bölge, komşu · İŞGAL %100, sektörler, tek cephe
    kuralı, “Enter · SEÇİLİ BÖLGEDE CEPHE AÇ”, “Esc · Vazgeç”. Yalnız aday seçiliyken açılır.
  - **Fiş yerleşimi:** cepheyi, özgür bölgeleri, adayları, figürü ve lejantı örtmeyen yarı (görev fişi) / çeyrek (onay fişi);
    eşitlikte daha az bölge etiketi örten. Sonuç referansla aynı: K2 sağ (Asya üstü), K3 sağ alt (Hint Okyanusu).
  - **K3 dizisi** (`SiginakMap`): yeni özgürleşen bölge algılanır; baskın sahnesi kapanıp operasyon raporu okunduktan
    0,35 s sonra damga iner (0,2 s), tarama 0,5 s'de silinir, teleks “BÖLGE ÖZGÜR · Doğu Avrupa · +30 fon” ve
    “BEKLENİYOR · Sıradaki cepheyi seç”, adaylar 0,12 s arayla kesik halkayla çizilir (radyal dolum 0,3 s). Onay: yağlı
    kalem halkası 0,4 s, fiş görev fişine döner, figür yeni cepheye kayar. Yüklemede ve yeni oyunda dizi oynamaz.
  - **Ekip figürü** (`TeamFigure`, `TeamFigureDrag`; room.js › buildTeamFigure): pirinç oval taban, üç piyon (mavi, kehribar,
    yeşil bant), “A” plakası; seçim halkası, taşıma gölgesi, YARALI etiketi. Yer oyun durumundan (kayda yazılmaz): baskında
    kalkık, yaralıyken S-07 (son baskında düşen üye kadar piyon yatık), yoksa cephe / özgürleşen bölge. Taşıma: 6 px ya da
    150 ms → 25 mm kalkar, −6°; harita düzlemine ışın; kamera bakışı ve tekerlek kapalı. Bırakma kuralı `TelexReasons.Drop`:
    cephe açıkken yalnız kendi cephesi (görev fişi açılır), cephe özgürken yalnız aday (onay fişi; figür adayda bekler, Esc
    0,25 s'de geri kayar). Geçersiz: geri kayar + teleks (“Cephe açık: Rusya 0/3”, “Okyanusya: özgür bölgeye komşu değil”).
    Yaralı figür alınamaz: sallanır + “Alfa yaralı (2 gün)”. Tık: durduğu bölgenin fişi.
  - **Ekip noktası** `Data/Siginak/team_anchors.json`: Sade etiketleri çokgen kenarına yakın olduğundan PROMPT'taki
    “etiket + (0, 110)” varsayılanı 10 bölgede komşu bölgeye düşüyordu (Rusya → Çin). Noktalar çokgen taramasıyla seçildi
    (figür tabanı sığar, etiketin ≥ 105 px altı ya da 80 px üstü); test her noktanın kendi bölgesinde olduğunu sınar.
  - **Girdi:** Enter önce açık fişin eylemi (`SiginakMap.HandleEnter`), fiş yoksa şalter. Esc: taşımayı bırak → onaydan vazgeç →
    fişi kapat → şalter kapağı → ayarlar. 3 GÖREVLER cephenin görev fişini açar. Eski görev fişi ve bölge seçimi yeni
    görünümde devre dışı (`LiberationDeskView.siginakMap`). Teleks: başarılı baskın emri ve cephe artık neyin uygulandığını
    yazar (“Baskın emri: Doğu Avrupa · Tahliye geçidi · −12 fon”, “Yeni cephe: Rusya”).
  - **Tarama kazancı** (`colors.mapLiberation.hatchGain` 1,8, proje eki): K2'de doku pikseli ≈ 0,63 ekran pikseli, 1,5 px'lik
    tarama çizgisi süzülürken soluyordu; referansla ölçülerek. Tarama dokularına mip bias −1.
  - Fikstür `Editor/Siginak/SiginakFixtures` (Play içinde, yalnız komutlarla): V.014, V.019, yaralı V.015.
  - Proje token ekleri: `mapLiberation.land/string/stringShadow/hatchGain`, `space.stringPx/stringShadowPx/frontSlipW/mapK2ScreenW`,
    `motion.candidateStepS/candidateDrawS/greaseRingS/raidOrderDelayS/freeStampDelayS/hatchEraseS`,
    `materialsExtra.teamTechnician/teamEmp/teamAnalyst`.
  - Lejant “alındı” → “geri alındı” (EN “retaken”): “alındı” anahtarı baskın panelinde “collected” anlamında kullanılıyor.
  - Play doğrulaması (yeni görünüm, fikstürle): V.014'te Doğu Avrupa'ya tık → OP-02 fişi sağda; Enter → “EMİR VERİLDİ”, FON
    147,5 → 135,5, teleks “Baskın emri: …”, 0,3 s sonra CRT dalışı, baskın sahnesi yüklenir; sonuç → masaya dönüş. V.019 →
    rapor modalı kapanınca damga + silinme + 4 aday (Kuzey Amerika, Rusya, Orta Doğu, Afrika); figürü Orta Doğu'ya bırak →
    onay fişi, Esc → figür geri kayar; Rusya'ya tık → onay fişi sağ altta; Enter → Rusya cephe, OP-01, figür kayar. Cephe
    açıkken Afrika'ya bırakma → “YAPILAMADI · Cephe açık: Rusya 0/3”. Konsol temiz.
  - Görüntüler: `screens_unity/A5_K2.png`, `A5_K2_EN.png`, `A5_K3.png`, `A5_K3_reveal.png`, `A5_K3_drop.png`,
    `A5_K2_gray.png`, `A5_K3_gray.png` (gri tonlamada durumlar desen, işaret ve kelimeyle ayrılıyor).
  - Testler: `SiginakMapTests` (9 vaka: durum kurucu, figür yeri, fiş içerikleri, Karar 3, onay fişi, bırakma kuralları,
    V.014 fikstürü, sahne bağlantıları + ekip noktası kendi bölgesinde). `SiginakDeskTests` HUD canvas sorgusu fiş canvas'ına
    takılmasın diye düzeltildi. Tam EditMode **688/688**. Kayıt dosyaları geri yüklendi, ayar eski görünümde, dinamik font
    önbellekleri geri alındı.
  - Ertelenenler: K1'deki “katlı etiket” (fiş kapanınca masada kalan etiket) yok, fiş harita pozu dışında kapanır · ses
    ipuçları (hışırtı, tak, kalem) yer tutucu · K3'te sayaçlar baskın sonucunda (oda gizliyken) dönmüş oluyor, “0,3 s:
    tekerler döner” adımı Aşama 7/8'deki dönüş dizisiyle · figür taşırken “geçerli bölgeler parlar” yalnız altındaki bölgenin
    sarı dolgusu · eski LiberationMapCanvas görev fişi yeni görünümde gizli, Aşama 10'da silinecek.
- **2026-10-26 · Aşama 4 (masa kontrolleri ve geri bildirim):**
  - **Sayaç rafı** (`CounterWheel`, `CounterModule`, `CounterBank`, `DeltaSlip`): VARDİYA, FON (onda birli), ERZAK (/kap),
    MALZEME, ÖZGÜR BÖLGE (/12). Çark ofseti README formülü (`1 − (d+1)/11`, MPB `_BaseMap_ST`); değişimde sağdan sola
    0,12 s aralıkla yuvarlanır, ± fişi 1,5 s asılı kalır (artış `status.good`, azalış `status.threat`).
  - **Anahtar rayı** (`StationKey`, `StationKeyRail`): KOMUTA 1 · SIĞINAK 2 · GÖREVLER 3 · KILAVUZ H · MASA Home. Basılı
    tuş rig pozundan gelir; kilitliyken tuşlar inmez.
  - **Kapaklı şalter** (`ShiftSwitch`): boşta → kapak açık (4 s sonra kendiliğinden kapanır) → kol iner → bırakılır;
    devre dışı durumu gerekçeli. Enter iki adımı sürer, kapak açıkken Esc kapağı kapatır (ayarlara düşmez). Kapak
    açılınca teleksten “ÖN KONTROL” (boştaki kişi, güç açığı, verilmemiş baskın emri) basılır.
  - **Teleks** (`TelexFeed`, `Teleprinter`, `TelexStrip`): kod `VVV/NN` (vardiya başına sayaç), son 30 satır;
    yazıcı kadrajda değilse satır ekranın altındaki şeritte (0,15 s giriş, 3 s, 0,12 s çıkış). Bütün “Emir uygulandı” /
    “Yapılamadı” geri bildirimi `LiberationDeskView.Report` → teleks. Gerekçeler sayılı (`TelexReasons`, ör.
    “Revir onarımı: malzeme yetersiz (8 gerekli / 3 var)”, “Baskın: fon yetersiz (…)”, “… yaralı (n gün)”).
    Satırlar `liberation_desk.json` yan dosyasında (kayıt tohumu + türüyle eşlenir) saklanır.
  - **Kenar satırı** (`EdgeLine`): otomatikte sayaç rafı kadraj dışındayken (K4 vb.) açılır; Her zaman / Kapalı tercihi
    `PlayerPrefs`'te. Değerler ve istasyon çipleri; etkin çip pirinç zeminde koyu yazı.
  - **Üzerine gelme** (`DeskTarget`, `HoverTag`, `HoverHighlight`): `PhysicsRaycaster` (yalnız yeni görünümde açık) +
    kutu çarpıştırıcılar; 4 mm kalkma, 400 ms gecikmeli kâğıt etiket (ayrı tuş fişi + başlık + satır), 5 px sarı çerçeve
    (+1,5 px mürekkep). Hikâye/ayarlar/modal açıkken hedefler kilitli.
  - **AYARLAR plakası** (`SettingsPlaque`): sağ üstte “Esc · AYARLAR”; kenar satırı açıkken gizli. ATLA modu Aşama 8'de.
  - **Umut işaretleri** (`HopeMarks`): 3/6/9/12 özgür bölgede pano notu, ikinci kupa, sakin ampul (9+), afişte el
    yazısı ve gün ışığı (12).
  - Bağlayıcı `SiginakDesk` (Bridge): sayaçlar, ray, kenar satırı, şalter, teleks ve tıklanabilir nesneler oyuna bağlı;
    Space liberation'da artık vardiya bitirmez (`SimDeskCommands.TogglePause`), vardiya yalnız şalterle biter.
  - **Sprite dilim düzeltmesi:** `ui_frame_8px_r0` çizgisi kenardan 4 px içeride başlıyor; README kenarı 10 çizgiyi
    bölüyor ve orta dilim gerilince yarı saydam dolgu veriyordu. Kenar 13 (`ui_frame_4px_r0` 6 → 8).
  - **TMP `<mark>` tuzağı:** vurgu yazının üstüne çizilir; opak tuş fişi tuşu örtüyordu. Etikette fiş ayrı öğe.
  - Play doğrulaması (yeni görünüm): sayaçlar simülasyonla aynı; Enter × 2 → gün 0 → 1, VARDİYA çarkı 002'ye döner, FON
    60 → 62 (± fişi); teleks “001/01 ÖN KONTROL · boşta 4 kişi · baskın emri verilmedi: Tedarik deposu” … “VARDİYA 001
    KAPANDI”; 2 SIĞINAK → raf pozu ve kenar satırı açılır; malzemesiz revir onarımı → “YAPILAMADI · Revir onarımı:
    malzeme yetersiz (8 gerekli / 3 var)” şeritte; kapak açıkken Esc kapağı kapatır, ikinci Esc ayarları açar.
  - Görüntüler: `screens_unity/A4_K1.png` (12/12 umut işaretleriyle), `A4_K2.png`, `A4_K1_hover.png` (etiket + çerçeve).
  - Testler: `SiginakDeskTests` (6 vaka) + mevcutlar. Tam EditMode **679/679**. Kayıt dosyaları geri yüklendi, test
    teleks dosyası silindi, ayar eski görünüme alındı, dinamik font önbellekleri geri alındı.
  - Ertelenenler: ayarlar ekranında kenar satırı satırı (tercih şimdilik yalnız `EdgeLine.Preference`); ses ipuçları
    yer tutucu (henüz `SiginakAudio` yok); eski `LiberationMapCanvas` görev paneli yeni görünümde hâlâ çıkıyor
    (Aşama 5'te `OperationSlip` ile değişir).
- **2026-10-26 · Aşama 3 (kameralar):**
  - `IDeskCameraRig` (`Runtime/CommandPost`): eski `CommandPostCamera` ve yeni `SiginakCameraRig` aynı yüzü taşır.
    `LiberationDeskView` bütün kamera çağrılarını etkin rig'e yönlendirir (yeni alan `siginakRig`); eski kamera
    denetimi değişmedi.
  - `SiginakCameraRig` (`Runtime/Siginak`, ana kamerada; eski görünümde kapalı): pozlar Desk (K1), Map (K2/K3/K6b/K7),
    Shelf (K4), Crt (K5a), Shift (K6a). Kamera her pozda fiziksel; FOV odak uzaklığıyla, K1'in ufku lens kaydırmasıyla,
    geçişte ikisi birlikte değişir. 0,45 s ease-in-out; K1 → harita önce 0,15 s yalnız yükselir (0,35 m), sonra eğilir.
    Sağ tuş bakışı (yaw ±25°, pitch −15…+18°), tekerlek (FOV × 0,6–1,1), 1/3 harita, 2 raf, F harita ↔ masa, Home masa.
    `InputSuspended` (figür taşıma için). `Step(dt)` editörde test edilebilir.
  - **K4 poz düzeltmesi:** render.html'deki `localToWorld(c.set(...))` ham three.js yereli; Unity'de z işareti ters
    (K5a'da doğruydu, K4'te kamera rafın arkasında kalmıştı).
  - **K6a token düzeltmesi:** paket z −0,36; referans render ve görüntü z +0,36 (şaltere 0,5 m). Proje kopyasında
    düzeltildi, `cameras.K6a._fix` notu var.
  - **Kesit maketi gölge düşürmez:** tavan ampulüyle kıskaçlı pano arasında kalıp özet panosunu (K4'ün ana içeriği)
    karartıyordu; referansta pano aydınlık. Maket gölge almaya devam eder.
  - `RaidLauncher` (Game, yalnız ekleme): `MonitorFocusPose`, `MonitorFieldOfView`, `SetMonitorTransition`. Sunum anahtarı
    yeni görünümde yaklaşmayı `Poses/CrtDive`'a (ekranın 0,2 m önü, 40°) çevirir, eskide geri yazar. Rig RaidLauncher'ın
    `lockDuringTransition` listesinde.
  - Yakalama `SiginakCapture.Render` artık rig pozlarını kullanır (K1, Map/K2/K3/K6b/K7, K4, K5a, K6a).
  - **Çerçeve karşılaştırması** (`screens_unity/A3_*.png` ↔ `reference/renders`, kenar haritasında kaydırma ±8 × ölçek
    0,94–1,06 araması): K1, K2, K4, K5a, K6a için en iyi eşleşme kaydırma (0, 0), ölçek 1,00. Kabul ±%3.
  - Testler: `SiginakCameraTests` (5 vaka). Tam EditMode **672/672**. Play: hikâye kapanınca harita pozu, Ekipler →
    raf pozu, baskın emri → CRT dalışı (oda gizlenir) → sonuçtan sonra rig etkin, kamera harita pozunda; konsol temiz.
    Kayıt dosyası geri yüklendi, ayar eski görünüme alındı.
  - Not: K4 pozunun ışığı (referansta ortam 0,22, oda ışıkları ×2,4) Aşama 6'da `RoomView` ile.
- **2026-10-26 · Aşama 2 (oda ve ışık):**
  - `Editor/Siginak/SiginakRoomBuilder` (+ `.Desk`, `.Shelf`, `.Crt`, `.Wire`), menü *Corrigible › Sığınak › Odayı kur*:
    `room.js`'ten satır satır kabuk, duvar yazısı, afiş, mantar pano ve notlar, tavan ampulü, masa, sayaç rafı gövdesi,
    anahtar rayı, kapaklı şalter, teleks, banker lambası, evrak tepsisi, damga rafı, kupa, kalem, sol raf, sandıklar,
    filizli teneke, kesit maketi (çerçeve, yüzey, YZ direği, toprak, 2 × 2 oda, şaft, merdiven, eşyalar, revir hasarı),
    kıskaçlı pano, BOŞTA tepsisi, CRT masası, monitör, klavye, telsiz, ışıklar ve kamera pozları (K1, Map, K4, K5a, K6a).
    403 mesh, 43 TMP yazı, 8 ışık; primitive çarpıştırıcıları silinir. Tekrar çalıştırınca eski kök silinip yeniden kurulur.
  - **Dönüşüm kuralı düzeltmesi:** `scene_threejs/README.md` “bütün açıların işareti ters” diyor; Z ekseni için yanlış.
    Doğrusu: three.js Euler XYZ → quaternion → (−x, −y, z, w). `ThreeEuler` testle sabitlendi.
  - `SiginakMeshLib`: konik/açık/yarım silindir, simit, halka mesh'leri (`Art/Siginak/Meshes`). `SiginakMaterials`: URP Lit
    malzemeler (`Art/Siginak/Materials`), emissive doğrusal değerle; odadaki açık renkli yazılar TMP'nin URP Lit
    shader'ıyla ışık alır, koyu mürekkep ışıksız TMP ile koyu kalır.
  - `SiginakTextureBaker` (menü *Dokuları üret*): beton (arka/yan/zemin), linolyum, klavye, telsiz kadranı, afiş
    piktogramı; textures.js'in yazısız C# karşılığı, sabit tohumlu.
  - Token kopyasına uygulama eki `materialsExtra` (50 + `plate`): lib.js paletinde olup tokenlarda olmayan malzemeler.
    `plate` (metal .35): metal .85 pirinç plakayı yazısından koyu gösteriyordu. Paketteki `spec/` dosyası değişmedi.
  - `SiginakRoomWords` (Loc.N): odanın sabit 25 yazısı; `strings.txt`'e İngilizceleri (sözlük §9). “KYGA / SIĞINAK 07”
    EN'i BUNKER → SHELTER oldu. Hikâye metnindeki “Loot görevinde” → “Yağma görevinde”. `charset.txt`'e `ń` (Gdańsk).
  - Sunum anahtarı: `Resources/SiginakSettings.asset` (varsayılan eski görünüm) + `Bridge/Siginak/SiginakPresentationSwitch`
    (sahnede `SiginakPresentation`). Yeni görünüm: 12 eski oda nesnesi kapanır, tek dünya haritası yeni masaya
    (1,2 m genişlik) taşınır, eski kamera denetimi kapanır, kamera K1 fiziksel (sensör 36 × 20,25, odak 18,76, kaydırma
    −0,324), ortam üç renkli, yansıma .15. Baskın sahnesi yüklüyken oda kapanır (ışıkları tahtayı aydınlatmasın).
    Menü *Görünümü değiştir (eski ↔ yeni)*. Kamera rig'i Aşama 3'te.
  - Işık ayarı (`SiginakSettings`, tokenlardaki urpStart başlangıçtı): ampul 1,5 · masa lambası 3,5 (koni 94°, iç 42°) ·
    CRT sızıntısı 0,3 · ortam çarpanı 0,08 · yansıma 0,15. Gölge: ampul 1024, lamba 2048; URP ek ışık atlası 4096 (Soru 11).
  - `BulbFlicker`: 3–9 s'de 2–3 kırpışma (%60), ışık + cam emissive (MaterialPropertyBlock).
  - Yakalama: `SiginakCapture.Render(poz)` / `RenderFrom` (3B, Overlay'siz) → `screens_unity/A2_K1.png`, `A2_K1_gray.png`.
  - Testler: `SiginakRoomTests` (5 vaka). Tam EditMode **667/667**. Play'de yeni görünüm açıldı, konsol temiz; kayıt ve
    profil dosyaları değişmedi, ayar eski görünüme geri alındı.
  - Ertelenenler: umut işaretleri (pano listesine bağlı; Aşama 4), K6a kamerasında tokens (z −0,36) ile render.html
    (z +0,36) çelişkisi (Aşama 3'te render'a göre karar).
- **2026-10-26 · Aşama 1 (token, font, sprite):**
  - Tokenlar: `Desk/Data/Siginak/tokens_siginak.json` (paketten kopya) → `SiginakTheme` (`Runtime/Theme`, `TokenTheme`
    üstüne metin, yazı, gölge listeleri; ortak anahtarlar Sade'den) → `Resources/SiginakTheme.asset`. İçe aktarıcı
    `Editor/Siginak/SiginakThemeSetup` (menü *Corrigible › Sığınak › Tokenları içe aktar*): 172 renk, 287 sayı, 29 metin,
    58 yazı rolü, 9 gölge. `wallStencil` CP 700'e düşer, asıl aile `type.wallStencil.fallback`'te.
  - Fontlar: `charset.txt`'e `▮ ▸` eklendi. `DeskFontSetup.AddMissingCharacters` (menü *Desk UI › Karakter setindeki
    eksikleri ekle*) atlasları baştan çizmeden ekler. İki glif DejaVu statik yedeklerine girdi; ana yüzlerde eksik yok.
  - Sprite'lar: `Desk/Art/Siginak/Sprites` (31 uGUI sprite, PPU 100, Full Rect, README dilimleri) ve `.../Textures`
    (7 doku: harita taraması, sayaç çarkı/gölgesi, CRT statiği; mipmap'li). Tablo `Editor/Siginak/SiginakSpriteManifest`,
    `DeskSpriteImporter` buna devreder. Menü *Sığınak › Sprite'ları içe aktar* paketten yeniden kopyalar.
  - Deneme: `Scenes/Siginak_FontTest.unity` + `screens_unity/A1_fonts.png` (menü *Sığınak › Font deneme sahnesi*).
  - Metin: Türkçe kaynak `LOOT` → `YAĞMA` (`LiberationDeskView`, `strings.txt`: YAĞMA → LOOT).
  - Testler: `SiginakFoundationTests` (7 vaka). Tam EditMode **662/662**.
  - Sunum anahtarı henüz yok: değiştirecek yeni görünüm Aşama 2'de geliyor; anahtar orada kurulur.
- **2026-10-26 · Aşama 0:** Paket ve sahne incelendi; eşleme tablosu, girdi eşlemesi, anahtar planı, kararlar,
  10 açık soru ve riskler yazıldı. Kod ve sahne değişmedi.
