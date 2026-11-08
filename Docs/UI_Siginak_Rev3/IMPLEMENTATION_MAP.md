# Sığınak · Revizyon 3 + Ek 11-05 — Uygulama haritası (son hâli)

**Durum:** Aşama 0–10 bitti (2026-11-08). Son koşu: EditMode **988/988**, PlayMode **85/85**. Kayıt biçimi **15** (`GameRecord.CurrentVersion`). Görüntüler `screens_unity_rev3/` (A10: 259 JPG). Ham kanıt [A10_DOGRULAMA.md](A10_DOGRULAMA.md). Aşama 9 sonundaki tam metin [IMPLEMENTATION_MAP_A9.md](IMPLEMENTATION_MAP_A9.md): bu sadeleştirmede kısalan anlatılar orada durur (A10-D12).

**Taban:** dal `siginak-ui`, `c1a743a Before Design implementation.` Üstüne kartlı pano ve Revizyon 3 işi commit edilmemiş çalışma ağacındadır.

**Kısaltmalar:** `S` = `Sim.S`, `Bd` = `Sim.Board`, `Adm` = `Sim.Admission`. `PBP` = `Desk/Scripts/Bridge/PlanningBoardPresenter.cs`, `SPB` = `Core/Simulation.PlanningBoard.cs`, `SCA` = `Core/Simulation.CommunityAdmissions.cs`. Yollar `Assets/Corrigible/` altındadır.

**Eski bölüm numaraları.** CHANGELOG, karar metinleri, kod yorumları ve `DURUM.md` eski numaralarla atıf yapar; Soru, D, K ve R numaraları değişmedi.

| Eski | Yeni | Eski | Yeni |
|---|---|---|---|
| §1 Önemli bulgular | §11 (seçilmiş), tamamı A9 kopyasında | §7–§9 Aşama 1–3 | §10.1–§10.3 |
| §2 Yerleşim | §2 | §10–§12 Aşama 4–6 | §10.4–§10.6 |
| §3 Eşleme | §3 | §13–§15 Aşama 7–9 | §10.7–§10.9 |
| §4 Açık sorular | §8 | §6.1–§6.7 Kararlar | §4.1–§4.7 |
| §5 Riskler | §9 | | |

---

## 1. Son durum özeti

| PROMPT §0 ölçütü | Sonuç | Kanıt |
|---|---|---|
| 1. Bütün pozlar referansa denk (TR; R3, R6, R11, R15, R17'de EN) | **Karşılandı, kayıtlı farklarla** | 86 durum TR + EN (B1 yalnız EN, plan gereği), 259 görüntü; tasarımla yan yana bakıldı (farklar §5). Kalan ışık ve albedo farkları: G23, G27, G29, K8A gölgesi, %13–20 parlaklık |
| 2. Modeller manifestle kurulu | **Karşılandı** | `Rev3ModelTests`, `model_import_report.txt` (198 model, uyarı yok) |
| 3. Etkileşim durumları, `motion.*` süreleri | **Karşılandı** | Aşama başına testler ve görüntüler (§10); PlayMode 85/85, Aşama 10'da kişi kartı sürükleme testi eklendi. Üzerine gelme süpürmesi Crt, Cabinet, MS, M1'de satır üretmedi (§6) |
| 4. Her metin TR + EN; punto küçülmez, satır sarar | **Sapmayla karşılandı** | 17.939 canlı satır: küçük 0, `Loc.Missing` 0, EN'de Türkçe 0, `.notdef` 0, gerçek taşma 0. Sapma: damga rafı, sap, teneke ve buğu camı plakası otomatik küçülür (D14, G01) |
| 5. Oyun mantığı, denge, kayıt biçimi değişmez | **Onaylı sapma** | Biçim 14 → 15 yalnız K4-A gözetmen kuralı için (§4.6). Aşama 10'da Core, Balance, RNG değişmedi (`Core` altında plan tarihinden yeni dosya 0) |
| 6. Rev2/Rev3 arasında anahtar | **Karar K1 ile düştü** | Önce/sonra görüntüleri. Rev2 görünümü onayla T1 kapsamında kaldırıldı (A10-D1) |

**Son ölçümler**

| Ölçüt | Değer |
|---|---|
| Testler | EditMode 988/988, PlayMode 85/85 (Aşama 9: 969, 83) |
| Çeviri · eksik script | Validate 0 · üç sahnede 0 |
| Statik punto (`punto_taramasi.txt`) | 2321 metin, 0 ihlal, 149 gerekçeli istisna (Aşama 9: 2438 / 150) |
| Canlı dil ve punto (`A10_dil_olcumleri.json`) | 17.939 satır, 86 durum, 169 yakalama adımı OK, 0 FAIL; okuma durumunda < 14 px 0 |
| Taşma bayrağı | 1368 dikey bayrak (1222 tek satırda font satır ölçüsü, 146 tek satırlık yuvada sarılı metin); sınıf bazında incelendi, kutudan kesilen yazı 0 (A10_DOGRULAMA §Dil) |
| Otomatik punto alt sınırı | 6 satır, BREATH GLASS / BUĞU CAMI %75 (G1'de 36 px okunur) |
| Kare süresi hedefi (A10-D8) | Medyan ≤ 16,7 / p95 ≤ 20 ms: onarımlı bütün senaryolar sağlar. En kötü medyan TR2 5,73 ms, en kötü p95 6,96 ms (P20.in, tek tur) |
| Ölçüm koşulu (uyarı) | Dev Mac (M4 Pro, Metal), Editor Play. Ortam satırları `screen=555x808`, `focus=False` yazar: oyun döngüsü süreleri 1920 × 1080 sayılmamalı. 1920 × 1080 olduğu kesin olan 30× RT çizimleri: en kötü MSAA4 11,46 ms (P5), MSAA1 14,27 ms (P6). PC vekili değildir |
| Gölge atlası | Ölçek uyarısı 0, Point gölge 0. K1 3 × 2048, K4 2048 (eskiden 4095), K8A 2048 + 1024 (tezgâh spotu 32'den 1024'e), K8 kapı açık 2048, 2048, 1024, 1024 |
| Sahne | `DeskGame.unity` 14,77 → 11,72 MB (aşama başında 14,44); 1.160 GameObject silindi |
| Korunanlar | Kayıt, masa, düzen, profil dosyaları ve 25 font dosyası başlangıç yedeğiyle bayt bayt aynı; dil `tr` |

Yapılmayan ölçümler: R15 (proje kopyası gerekir), oyuncu derlemesi, PC ölçümü, sahne yükleme süresi, kulak kontrolü (§6).

---

## 2. Klasör ve assembly yerleşimi

PROMPT'un önerdiği `Assets/Corrigible/Presentation/Siginak/Rev3/` yolunda assembly yok. Oraya konan script'ler
`Corrigible.Desk` / `Corrigible.Desk.Bridge` içinden görünmez. İlk iki turun düzeni sürdürüldü (K3):

| İçerik | Yer |
|---|---|
| OBJ kopyaları | `Desk/Art/Rev3/Models/<aile>/<Model>.obj`. 198 `.mtl` de kopyalanır ama kullanılmaz (`materialImportMode = None`; PROMPT yalnız kullanmamayı ister) |
| Prefab'lar | `Desk/Prefabs/Rev3/<aile>/<Model>.prefab` |
| Malzemeler | `Desk/Art/Rev3/Materials/` (~45 paylaşılan URP Lit + kit malzemeleri). `Rev3MaterialLibrary.asset` de burada (Resources değil; editörde `AssetDatabase` ile yüklenir). Sahneye özel dokulu malzemeler `Desk/Art/Rev3/SceneMaterials/` |
| Dokular | `Desk/Art/Rev3/Textures/`: `tx_*`, `papers_new`, portreler |
| Model altyapısı (`PartDriver`, `Rev3TextSlot`, poz ışık tarifi verisi) | `Desk/Scripts/Runtime/Rev3/` (`Corrigible.Desk`). Ses ve URP ışık verisi Bridge'de (§11 bulgu 14) |
| Pano, kart, kabul görünümleri | Var olanların yanına: `Desk/Scripts/Runtime/PlanningBoard/` ve `Runtime/Siginak/` |
| Oyun bağlantısı | `Desk/Scripts/Bridge/` (`PlanningBoardPresenter`, `Siginak/*`) |
| Editör araçları | `Desk/Scripts/Editor/Rev3/` (`Corrigible.Desk.Editor`): `Rev3ModelImporter`, malzeme kurulumu, doğrulama |
| Model verisi (manifest, sayfa) | `Docs/UI_Siginak_Rev3/modeller/`'dan okunur, Assets'e kopyalanmaz (Rev2'de sprite manifesti de böyleydi) |
| Testler | `Desk/Tests/EditMode/Rev3*Tests.cs`. PlayMode için Karar K10 |
| Görüntüler | `Docs/UI_Siginak_Rev3/screens_unity_rev3/` |

---

## 3. Eşleme

Biçim: öğe · şimdiki karşılık (dosya, prefab, sahne) · durum · veri kaynağı ya da komut. Aşama 0'daki "Mevcut karşılık / Yapılacak" sütunları `IMPLEMENTATION_MAP_A9.md` §3'tedir.

### 3.1 Altyapı (§4, §5, R14)

| Revizyon 3 öğesi | Şimdiki karşılık | Durum | Veri kaynağı / komut |
|---|---|---|---|
| `_AxisTest` | Geçici içe aktarıldı, ölçüldü, silindi (bbox, üçgen 176, z +90°); `A0_axistest_*.png` | DOĞRULANDI | `manifest/_AxisTest.json` |
| Model hattı | `Desk/Scripts/Editor/Rev3/`: `Rev3ModelImporter`, `Rev3Manifest`, `Rev3ModelPostprocessor`, `Rev3MaterialSetup`, `Rev3ModelSheet`. 198 prefab `Desk/Prefabs/Rev3/<aile>/`. Kalıcı asset hattı (K2); çarpıştırıcıyı nesneyi bağlayan aşama ekler | UYGULANDI | `modeller/manifest/*.json`, `obj/*.obj` |
| `Rev3MaterialLibrary` | `Desk/Art/Rev3/Materials/`: 340 anahtar → 335 malzeme; emission `RealtimeEmissive`; sahneye özel olanlar `SceneMaterials/` | UYGULANDI; dokulu anahtarların bir kısmı düz (Soru 20) | token 1.3 `materials.*`, `materials3d.*`, `kits.*` |
| `PartDriver`, `Rev3Model`, `Rev3TextSlot` | `Desk/Scripts/Runtime/Rev3/`; görünümler parçayı `PartDriver` üzerinden sürer | UYGULANDI | manifest `parts[]`, `motion.*` |
| Etkileşim durumları | `DeskTarget`, `HoverTag`, `IHoverSource`; sürüklemede `BoardDragController`, `StampDecision`, `ShiftReceiptPaper` | UYGULANDI | `motion.*` |
| Yerelleştirme | `Loc`, `DeskLocText`, `strings.txt`; bağlamlı anahtar `DeskLoc.Ctx` (A10-D3, `kaynak@@bağlam`) | UYGULANDI | Asıl kaynak `strings.txt`; `LocalizationTool` |
| Token 1.3 (sığınak) | `Desk/Data/Siginak/tokens_siginak.json` → `SiginakThemeSetup` → `SiginakTheme.asset`; kaldırılan 26 düğüm `_was_1_3`'ten korunur; yerel notlar `_local_a4`..`_local_a8` | UYGULANDI | `tokens/tokens_siginak.json` |
| Token 1.3 (baskın) | `Desk/Data/Sade/raid_tokens.json` → `SadeThemeSetup.ImportRaid` → `RaidTheme.asset`; `_local_a9` | UYGULANDI | `tokens/raid_tokens.json` |
| Fontlar | Big Shoulders Stencil ExtraBold (`DeskFont.StencilExtraBold`, statik + Dyn SDF); rol kazanır (Soru 18) | UYGULANDI | K6 |
| Poz başına ışık | `Desk/Data/Rev3/PoseLightRecipe.asset` + `PoseLighting` (`SiginakRoom/Pose lighting`); tarif §3.13 | UYGULANDI | `lightsRecipe.*`, `Rev3RoomTests` |
| Yıpranma / detay haritası (R14b) | Yapılmadı | SAPMA (A10-D10) | §5 |
| Ses | `Sfx.Play(Cue)` → `AudioDirector` → `Settings/SoundBank.asset`; bütün cue'lar bağlı; 42 yer tutucu `Audio/Placeholder/Cue*.wav` | UYGULANDI, kulak kontrolü açık (A10-D5) | `Tools/Audio/rev3_placeholder_audio.py` |
| Rev2/Rev3 anahtarı | Yok | DÜŞTÜ (K1) | — |
| Sunum durumu | `liberation_desk.json` (`SiginakDeskStore`: poz, son 5 fiş, `AdmissionSeen`), `planning_board_layout.json` v3 (fiş açıları, deste, YENİ). Oyun kaydına girmez | UYGULANDI | — |

### 3.2 Komuta odası temeli (R5, R14; Aşama 2)

| Revizyon 3 öğesi | Şimdiki karşılık | Durum | Veri kaynağı / komut |
|---|---|---|---|
| Oda kabuğu | `SiginakRoom/Shell` ilkel | İLKEL KALDI (paket modellemiyor) | — |
| Masa ve süs nesneleri | `Desk`, `TeamFigure`, `DeskLamp`, `StampRack`, `Pencil`, `Shavings`, `Eraser`, `Mug` ×2, `CoffeeRing`; geç katman `CampaignWear` (V.016) | UYGULANDI | `S.Day` |
| Sayaç rafı | `CounterRack` + `CounterModule`, `CounterWheel` (36·d); ERZAK eki TMP (Soru 11); HAMMADDE / RAW MAT. (Soru 3) | UYGULANDI | `S.Day`, `S.Fon`, `Shelter.Supplies` / `SuppliesCapacity`, `Shelter.Materials`, özgür bölge, `S.Trust` |
| Anahtar rayı | `KeyRail` + 4 `KeyKey` (`StationKeyRail`) | UYGULANDI | rig pozu; `Adm.Candidate` |
| Vardiya şalteri | `Desk/Shift switch`, model parçaları; §3.5 | UYGULANDI | §3.5 |
| Teleks | `Telex`, `TelexPaper` (dört yuva), `TelexRoll` (`Teleprinter`, `TelexFeed`) | UYGULANDI | `TelexFeed` |
| EVRAK | `PaperTray` (`EvrakTray`; Soru 14, §4.4 D7) | UYGULANDI | son 5 fiş |
| Duvarlar | `AlarmBoard`, `CorkBoard`, `Poster`, `StencilSign`, `Duct`, `PipeRun`, `JunctionBox`, `CeilingBulb`, `PencilNote`; `FreedRegionsBoard`, `BulbFlicker` yerinde | UYGULANDI | `S.Day` |
| Tebeşir çetelesi | `ChalkTally`, 16 çentik yeri; kartlı oyunda yalnız vardiya | UYGULANDI | `S.Day` |
| Pano | `PlanningBoard` (−1,62 · 0 · 1,70, yaw −48); §3.3 | UYGULANDI | — |
| Işık K1 | Ampul spotu 120°, masa lambası, pano lambası gölgeli; ortam dolgusu `Env fill`; üç ışık 2048 sahne override'ı (A10-D7) | UYGULANDI | `PoseLightRecipe`; `Rev3RoomTests` |
| Kamera K1 | `Poses/K1`, 56,7°, lensShiftY −0,324 | KULLANILDI | — |
| Kenar satırı | `EdgeLine`; K4, K6, kartlı oyunda K8 ailesinde gizli (`SiginakDesk.EdgeHidden`) | UYGULANDI | rig pozu |

### 3.3 Kartlı pano ve kartlar (R15, R16; Aşama 3)

| Revizyon 3 öğesi | Şimdiki karşılık | Durum | Veri kaynağı / komut |
|---|---|---|---|
| Veri yolu | `PlanningBoardSnapshotBuilder` → `BoardSnapshot` → `BoardView3D`; yalnız değişimde çizer | UYGULANDI | `Bd` |
| Pano yüzü, plakalar | `tx_board_face` (sözcüksüz), 25 plaka yuvası; `Rev3BoardTextureBaker` | UYGULANDI | token `board.plates` |
| 6 cep, saha bandı | `BoardMarks`, `BoardDragController` cep dikdörtgenleri | UYGULANDI | `AssignResident` → `board_assign` |
| Boşta destesi | `BostaDeckView` (0,32 s, 0,07 s; min(82, 636/(n−1))) | UYGULANDI | `job == "rest"` |
| Kişi kartı | `Prefabs/Rev3Board/PersonCard` + `PersonCardFace`, `CardStateOverlay`, kimliğe göre havuz; `PortraitLibrary` (32) | UYGULANDI | `ResidentState`, `Adm.File(id)` |
| Ustalık sütunu | 7 satır, kademe 2 / 6 / 12 (Soru 1) | UYGULANDI | `Experience`, `BoardSkillTier` |
| KAYIPLAR zarfı | `LostEnvelopeView`, kişi dosyasının arkasında (§4.3 D5); `BoardDeathLog` | UYGULANDI | `People.Where(!Alive)` |
| Eşya kartı | `ItemCard`, kişinin arkasında 14 mm şerit | UYGULANDI | `EquipResident` → `board_equip` |
| Depo | `StoreView`, `MaterialCard`, 6 kit yığını | UYGULANDI | `Material(kind)`, `BoardMaterialFree` |
| Hurda | `HurdaBin` | UYGULANDI | `ScrapEquipment` → `board_scrap` |
| Eğitim | `TrainingTopics`, 7 satır | UYGULANDI | `TrainResident` → `board_train` |
| Kişi dosyası | `PersonFolderView`, `PersonDossierFace` (iki sayfa + ek yaprak, Aşama 7), `FileTagView`; sekme alt satırları (A10-D18) | UYGULANDI | `ReviewResident`, `RestrictResident`, `DismissResident` |
| Başlık arduvazı, kilit, MASAYA DÖN | `TXT_Header`, Flap, LockRod, BackSign | UYGULANDI | `Bd.Locked`, `Sim.BoardFoodPreview` |
| Pano lambası | Model ışık noktası; gölge yakın düzlemi 0,1; 2048 override | UYGULANDI | `PoseLightRecipe` |
| Sürükle-bırak | `BoardDragController` + `BoardDropRules.CommandFor` + `BoardPreviewText`; klavye yolu korunur; `PersonCardDragPlayTests` | UYGULANDI | aynı komutlar |
| K4 kamerası | Model `Cam_K4`, FOV 41° (§4.3 D1), K1 → K4 0,6 s, geri 0,5 s | UYGULANDI | `cameras.K4` |
| Emekli görevler | `PBP.RetireJobs` | KORUNDU (Soru 10) | `board_assign … rest` |

### 3.4 Tarif defteri ve üretim (R18; Aşama 5)

Soru 9 **B** ile çözüldü (§4.5 D1).

| Revizyon 3 öğesi | Şimdiki karşılık | Durum | Veri kaynağı / komut |
|---|---|---|---|
| `RecipeBook` | `RecipeBookView`, `RecipeBookPage`, `BookPageShape`, `BookTypes`; World Space TMP + `tx_recipe_page`; okuma pozu 0,35 s, çevirme 0,25 s | UYGULANDI | `BoardRecipes` (6) |
| Tarif kartı | `Prefabs/Rev3Board/RecipeCard`, `RecipeCardFace`; defterden sürüklenir | UYGULANDI | `board_queue`; dolu emre `board_cancel` + `board_queue` |
| İş emri formu | `WorkOrderView`; durumlar boş · tarif var · eksik · tam · başladı · durdu · tamamlandı; ipucu "GİRDİLER (hammaddeyi buraya bırak)" (A10-D18) | UYGULANDI | `Bd.Orders`, `BoardPlanCheck` |
| Hammadde fişi | `ChipCard` | UYGULANDI | `board_reserve` ±1 |
| Yetersiz kaynak | Fiş kalkmaz, 2 mm titreme, sayı 0,2 s kırmızı | UYGULANDI | `BoardMaterialFree == 0` |
| Eksik emir | Şalter kilitlenir | UYGULANDI | `BoardPlanCheck` |
| Ürün kayışı, YENİ | `NewItemTag`, depoya 0,3 s | UYGULANDI | `Bd.LastCompleted`; layout v3 |
| Eski kayıtta konmuş fiş | Devamda her tür için bir kez `board_stage(kind, 0)` | UYGULANDI | §4.5 D2 |

### 3.5 Vardiya kapanışı (R19; Aşama 4)

| Revizyon 3 öğesi | Şimdiki karşılık | Durum | Veri kaynağı / komut |
|---|---|---|---|
| Şalter durumları | `ShiftSwitch` kip `shiftClose`, `ShiftPhase`; dolap ve menü şalteri eski kipte | UYGULANDI | `Bd.Printed`, `S.Raid` |
| Plakanın 2. satırı | `ShiftPlate` (kilit nedenleri §4.4 D2) | UYGULANDI | `shiftClose.states.*` |
| `SlipMouth` | Model; `ShiftReceiptMachine.slipLamp` | UYGULANDI | `Bd.Printed` |
| Devir fişi | `HandoverSlipFace`, `HandoverSlipFactsBuilder`, prefab `Prefabs/Siginak/HandoverSlipView` | UYGULANDI | `Bd.ReceiptId` |
| Fiş yolu | `ShiftReceiptPaper`: ağız 0,35 s (160 mm), el 0,25 s, tepsi 0,2 s, dile 0,25 s | UYGULANDI | `board_print`, `board_file` |
| EVRAK hedef ışığı | Gölgesiz amber nokta | UYGULANDI | `DragChanged` |
| Kapanış | `SiginakShiftClose`: kararma, tam karanlık kare, `FileBoardShift`, K1'e kesme, sayaçlar 0,4 s, teleks 1,2 s | UYGULANDI | `GameController.FileBoardShift` |
| K6 pozu | `SiginakPose.ShiftClose`, `Poses/K6`, 42° | UYGULANDI | `cameras.K6` |
| Ölçütler (fiş olmadan gün ilerlemez, okuma ücretsiz, tek kullanım, kayıttan devam) | `ShiftClosePlayTests`, Core testleri | KİLİTLİ | `FileShift`, `LastFiledReceiptId` |

### 3.6 Kapı ve kabul (R9, R17; Aşama 6)

Aşama 6 sonrası güncel (2026-11-07; ayrıntı §10.6). Yollar `Assets/Corrigible/Desk/` altında; Core yolları `Core/`.

| Revizyon 3 öğesi | Şimdiki karşılık (dosya) | Durum | Veri kaynağı / komut |
|---|---|---|---|
| Kapı ve koridor modelleri (`DoorFrame`, `DoorLeaf`, `HazardHeader`, `KnockLamp`, `IntercomBox`, `Corridor`, `Handrail`, `CorridorLamp` ×2) | Aşama 2 modelleri; `DoorProp` (`Runtime/Siginak`: iç saat `Advance(dt)`, süreli `SetOpen(…, wheelS, leafS)`, `KnockPlaying`/`WheelDegrees`/`LeafDegrees`/`StairGlow`); `SiginakDoor` (`Bridge/Siginak`) kartlı oyunda kapıyı `SiginakAdmission`'a devreder; kapı kutusu kanadın arkasına (D25); kartlı oyunda siluet, çetele, ölümcül kırmızı yok | UYGULANDI | `Adm.Candidate` |
| Vuruş (üç vuruş 0,9 s) ve lamba | `SiginakAdmission`: canlı oyunda aday başına bir kez `DoorProp.Knock()` (parlama 0 / 0,3 / 0,6 s, her biri 0,08 s, sonra sürekli amber) + `Knock` sesi; yüklemede vuruşsuz amber (D12) | UYGULANDI | aday kimliği değişimi (`announcedId`) |
| Kararda kapı açılır, koridor boş, kart panoya | KABUL **ve KISITLI**'da (D10) `DecisionTimeline`: kamera K8, tezgâh katlanır, 0,93 s'de çark 450° 0,5 s, kanat 95° 0,8 s, merdiven ışığı 0,3 s, bekleme 0,6 s, kapanış 0,6 s; koridor boş; kart Boşta destesinin sonunda. RET/YARDIM'da kapı kapalı, kamera K8A | UYGULANDI | `decideApplicant admit/restricted` |
| Kabul istasyonu (`AdmissionCounter`, `AdmissionPanel`, `CounterLamp`) | `SiginakRoom/Admission station` (`Runtime/Admission/AdmissionStation`: tezgâh T1–T5, çekmece, işaretçi + yakut, belge, şerit, ESKİ cebi, 5 fiş yuvası, `CounterSpot` 68°/2,0, tezgâh ortasına hedefli); eski uGUI pencere (`SiginakRoom/Admission papers`) silindi (D20) | UYGULANDI | `AdmissionSnapshotBuilder.Build(sim, ui)` → `AdmissionSnapshot` |
| Talimat şeridi 80 × 210 (v1–v3; işaretçi; ESKİ cebi) | `OrdersStripFace` (World Space TMP, ışıksız mürekkep; `Prefabs/Rev3Admission/OrdersStrip`); metin ve sıra Core'dan, YENİ · V.nnn / "sürüm n’den"; değişim ilk K8/K8A varışında (ESKİ'ye 0,25 s, yenisi 0,3 s) | UYGULANDI | `Adm.Protocol`, `ProtocolArchive`; görülen sürüm `liberation_desk.json` (`AdmissionSeen`) |
| Kimlik / sevk belgesi 148 × 105 | `AdmissionDocumentFace` (`Prefabs/Rev3Admission/AdmissionDocument`): künye, ad, portre (D18 ayna), mühür halkası, alanlar, koçan, damga izi | UYGULANDI | `AdmissionDocument`; `PortraitLibrary` |
| Dört inceleme, dört araç | `MagnifierTool` (KARŞILAŞTIR), `IntercomTool` (GÖRÜŞME), `ColdGlassView` (SOĞUK GÖZLEM, K8G; `DoorLeaf/Hinge` çocuğu, buğu `tx_fog`), `RequestPadTool` + `RequestFormFace` (DIŞ KAYIT; GİDEN yaprağı ve karbon, `Prefabs/Rev3Admission/RequestForm`); fiş `FindingSlipFace` 90 × 60 (`Prefabs/Rev3Admission/FindingSlip`) kıskaçta | UYGULANDI | `inspectApplicant papers/interview/breath/record`; sonuç `AdmissionFindingCode` |
| Dört karar, dört damga (`DecisionStampRack` + 4 `DecisionStamp`) | `StampDecision` ×4 (sürükle-bırak ya da tık-tık; kilitliyken pirinç dil D21, titreme, `Deny`; sap yazısı `capLabel`) | UYGULANDI | `decideApplicant`; kilit `ApplicantDecisionCheck` (kısa neden kapasite/erzak) |
| `RationTin` ×2 | çekmecede yan yatık (`Teneke L/R`); YARDIM'da çekmeceyle dışarı | UYGULANDI | `aid` |
| `FileTag` | `FileTagView` (`Runtime/PlanningBoard/Rev3`), kişi dosyasının sağ üst kenarında: "İNCELEME" / "KISITLI · n V" | UYGULANDI | `FolderData.TagText` |
| Dış kayıt bekliyor → sonuç sonraki kapanışta | Core K4-A: gözetmen = Boşta'nın ilk uygun kişisi (`AdmissionWatcher`, dinamik); BEKLENİYOR fişi ve karbonda canlı ad ya da kırmızı "gözetmen yok"; sonuç fişi ilk K8/K8A varışında çıkar | UYGULANDI (Core, biçim 15) | `Adm.ObservationTarget`, `record_done`, `AdmissionWatcher(targetId)` |
| Kişi dosyasında iz ("robot" yazmaz) | `PersonFolderView` (pano): KARAR satırı, bulgular koddan, İZLER bölümü, FileTag. R17f'nin açık dosya düzeni Aşama 7'de tamamlandı (§10.7) | UYGULANDI | `ResidentAdmissionFile`, `AdmissionFindingCode` |
| Pozlar: K8 → K8A, K8G, K8R | `SiginakPose.Admission` 8 (K8A 47°, 0,45 s), `BreathGlass` 9 (K8G 30°, 0,45 s), `AdmissionRead` 10 (K8R 40°, kâğıda göre dinamik çapa 0,318 m, 0,25 s); `Poses/K8A`, `K8G`, `K8R`. K8 58° aynı | UYGULANDI | `cameras.K8A/K8G/K8R` |
| Tezgâhtaki MASA tuşu, sağ tık, Esc | `KeyKey` + MASA plakası (tezgâh Top'unda); geri zinciri bir seviye (D13); MASA doğrudan K1 | UYGULANDI | — |
| Eski yol: 23 kart, `DoorCardView`, siluetler, ölümcül kapı | `SiginakDoor` eski dalı, `DoorViewBuilder`, `VisitorSilhouette` aynen (D19) | KULLAN | `Door.cs`, `DoorCards.cs` |

### 3.7 Harita, kâğıtlar, CRT, açılış (R6, R8, R10; Aşama 7)

| Revizyon 3 öğesi | Uygulama | Durum | Veri kaynağı / komut |
|---|---|---|---|
| Görev fişi: üç isimli SAHA KADROSU, CAN/HAR/GÜÇ, üzerindeki eşya ve altı ödül | `OperationSlip`, `FieldMemberFace` ← `LiberationMapBuilder`, `RaidPaperFacts` | UYGULANDI | `PlanningSquad()`, `PlanningRaidCheck`, Balance; kişi adına tık yalnız panodaki kartı seçer |
| Kadro raporu 470 px, üç eşya yuvası ve sağlık damgası | `SquadReportSlip` | UYGULANDI | aynı; eski rol/yedek satırları yalnız eski yolda |
| Operasyon raporu: altı kazanç, isimli dönüş/yaralanma/kayıp/deneyim | `OperationReportView` ← `OperationReportBuilder` | UYGULANDI | `RaidLogEntry`; ilgili `raidres` öncesi kayıt kopyasıyla gerçek depo sınırı |
| Emir belgesi, açılış ışığı | `BriefingBuilder.Planning`, `SiginakBriefing`, `OpeningSequence` | UYGULANDI | mevcut açılış; ampulden 0,15 s sonra masa lambası |
| İki sayfalık kişi dosyası ve devam yaprakları | `PersonDossierFace`, `PersonFolderView` | UYGULANDI | `PlanningBoardSnapshotBuilder`; yedi ustalık, bilinen belge/erişim/izler; mevcut üç karar komutu |
| CRT, telsiz, mikrofon, kayıt defteri | Aşama 2 modelleri + `CrtInstruments`, `SiginakCrt` | UYGULANDI | `CrtViewBuilder`, `RadioLogBuilder`; .3 s arama/sinyal, son .6 s 1,4× ışıma, dönüşte kapanma |
| K5a önerisi | aynı yön, CRT yerel sağına +0,14 m, FOV 52° | DOĞRULANDI | önce/sonra görüntüsü; `cameras.K5a`, K5b dalış hedefi değişmedi |

### 3.8 Menü sahanlığı ve ayarlar dolabı (R1–R4; Aşama 8)

| Revizyon 3 öğesi | Mevcut karşılık | Durum | Veri kaynağı / komut |
|---|---|---|---|
| Sahanlık kabuğu, merdiven, lambalar, başlık | `Giris › Entrance/Landing`; kabuk primitif, eşyalar paket modeli | UYGULANDI | 1,45 m karo; beton/ıslak zemin; drenaj, eksik karo, kanallar |
| Kapı, dış çark, gözetleme, interkom ve zil | `DoorProp` + `EntranceInstruments` | UYGULANDI | dekor; kapı giriş sekansında açılır |
| Puantaj, kart, delik, raf, deste ve iptal fişi | `TimeClock` + R2 modelleri ve UV ağ kopyaları | UYGULANDI | `SavedSummary`; dört satır; en çok 80 delik; 4 s iki tıklı onay |
| `LastSwitch` | mevcut `ShiftSwitch` | UYGULANDI | kapak 115°, kol ±55°; `PowerDown` |
| Oda ve menü dolabı, sürgü/gösterge/sayaç/anahtarlar | `SettingsCabinet.prefab` + ayrı `SettingsCabinetMenu.prefab` | UYGULANDI | aynı ayar servisi, sessizde değer korunur; −95° / −120°; menüde kör kapak |
| Kilitli menü kolu | `LiberationDeskView.MenuExitBlocked` → `SiginakCabinet` → `ShiftSwitch` | UYGULANDI | dolabın kendisi kilit nedeni sayılmaz; kâğıt/işlem kilitliyken titreme ve ses |
| R4 giriş dizisi | `EntranceController`, `TimeClock`, `EntranceCamera` | UYGULANDI | zımba → zil → çark → kanat → yürüyüş → siyah; yükleme siyahta etkinleşir |
| M0/M1/M2/MS/M3, K9 | sahne çapaları + token'lar | UYGULANDI | M0 60°, M1/K9 48°; okunurluk kalibrasyonu §10.8 |
| Ortak sahanlık prefab'ı | iki ayrı kurulum | KARAR KORUNDU | Aşama 0: menü sahanlığı ve iç koridor ayrı kalır |

### 3.9 Baskın (R11, R12; Aşama 9)

Ayrıntı [A9_DOGRULAMA.md](A9_DOGRULAMA.md).

| Revizyon 3 öğesi | Şimdiki karşılık | Durum | Veri kaynağı / komut |
|---|---|---|---|
| Tahta kurucusu | `RaidBoard3D`; kaynak değişti, kurulum mantığı aynı. Birleşik prefab'lar `Desk/Prefabs/Rev3Raid` (53) | UYGULANDI | `RaidPieceLibrary`, `RaidRoomKit` |
| Kit modelleri (12 + varsayılan) | Relief, ReliefIso, Bridge, Rubble; moloz 90° adımla kendi karesinde | UYGULANDI | `lib.KitFor(region)` |
| `BoardBase`, `Tile`, `WallBlock` | Model | UYGULANDI | — |
| Sivil cihaz, giriş kapısı (13 + 13) | Model; kapı ≤ 0,29 m, çarpıştırıcısız dekor | UYGULANDI | bölge |
| Dolaplar | `CoreCabinet`, `NodeCabinet`, `CoolingUnit`, `CopyCabinet`; `BackupCore` bağlanmadı (Soru 15) | UYGULANDI | `Baskin` nesne türleri |
| 8 makine | Model | UYGULANDI | `Baskin.UnitTypes` |
| Piyonlar, ad şeridi | `Pawn_T/E/A`; 180° kraft yüz, −70° düşüş; gerçek ad, pip, plaka | UYGULANDI | `Unit.PersonName` |
| Madenci ve sis piyonları | Kartlı oyunda gelmez; prefab'lar eski yolda durur | KALDI | `FieldJobs` |
| İşaretler | `MoveQuad`, `HitQuad`, `GhostRing`, `WakePin` model; oklar `RaidArrow`, `RaidPencil.shader` (A10-D11) | UYGULANDI | `Baskin.Intents` |
| Olay nesneleri | `Tube`, `WeldArm` + taban, `CeilingDebris`, `LootCrate` (kapalı/açık), `SlidingShelf` | UYGULANDI | olaylar |
| Kanal | 16 maske tek renderer, gölgesiz | UYGULANDI | `RaidTileMask` |
| Kâğıt paneller, antet, anahtar plan | `RaidHud.prefab`, `RaidPanels.prefab`; 400 ms bina notu | UYGULANDI | `RaidViewModel`, `region.Building` |
| Baskın ışığı | Ortak `PC_RPAsset` 2048, 4 kaskad, tarifin 4096'sı uygulanmadı | KARAR BEKLER (R15) | `lightsRecipe.RAID` |
| Ölçek | 132,58 px/birim; zemin kenarı projeksiyonu 108,08 px | KULLANILDI (Soru 7) | `tokens_sade.json → raid.camera` |

### 3.10 Kalıcı doğrulama araçları

| Araç | Yer ve çalıştırma | Çıktı |
|---|---|---|
| Poz ve durum yakalama | `Rev3CaptureSet.Start(oturum, dil, only)`; oturumlar `menu`, `k1`, `k4`, `k6`, `k8`, `hover`, `raid` (Play) | `screens_unity_rev3/A10_<ID>_{TR,EN}`, `_gray`; günlük `Temp/capture_set.txt` |
| Punto taraması | `SiginakTextScan.Run()` statik; `ResetLiveAll()`, `ScanLiveAll(cam, etiket)`, `WriteLiveAll()` canlı | `punto_taramasi.txt`, `A10_dil_olcumleri.json` |
| Tek kare | `SiginakCapture.Render` (3B, MSAA); `CorrigibleDebug.Capture` (3B + Overlay, TAA'lı, Play) | PNG |
| Fikstürler | `AdmissionFixtures`, `HandoverSlipFixtures`, `ProductionFixtures`, `SiginakFixtures` | Sunum durumları |
| Sahne referans denetimi | `Rev3SceneRefAudit.Snapshot` / `Compare` (R8) | Boşalan ve değişen alanlar |
| Model ve doku | `Rev3ModelImporter`, `Rev3ModelSheet.RenderModelSheets`, `Rev3TextureBaker` (`BakeFog`, `BakeStage8`), `Rev3BoardTextureBaker` | Prefab, sayfa, doku; `model_import_report.txt` |
| Çeviri | Corrigible > Lokalizasyon > Çeviri tablosunu güncelle; `LocalizationTool.Validate` | `strings.txt` |
| Testler | `utest` EditMode (boş sahne), `uptest` PlayMode (`Corrigible.Desk.PlayTests`) | GELISTIRME.md |
| Yer tutucu sesler | `Tools/Audio/rev3_placeholder_audio.py` (stdlib, tohumlu) | `Audio/Placeholder/Cue*.wav` |

Emekli: `SiginakCaptureSet` (Rev2 çıktısı, A10-D1). Tek seferlik `Rev3Stage*` ve `A10WireHints` script'leri Assets'ten silindi.

### 3.11 Girdi ve kısayollar

Her kısayolun tıklanan bir karşılığı var. "Bugün" sütunu Aşama 0 hâlidir; "Revizyon 3" uygulanan hâldir.

| Girdi | Bugün | Revizyon 3 |
|---|---|---|
| 2 | `FocusShelter` (K4), `PBP.Show`'u atlıyor | K4. Poza bağlı işler rig'de (A9 kopyası §1 bulgu 11) |
| 3 / F | harita / harita aç-kapa | aynı |
| 4 / K | `SiginakDoor.Visit` → kabul (Door) | K8 (aday varsa K8A) |
| Home | K1 | aynı |
| Enter | `DeskInput`; eski raporu dosyalar; pano: seçili kişiyi alana bırakır | aynı. K6'da fişi EVRAK'a koymaz: dosyalama yalnız fiziksel |
| Esc | `DeskInput.Escape`; pano `HandleEscape`; kapı dosyasını kapatır | açık kâğıdı ya da defteri kapatır, yoksa masaya döner |
| Sağ tık | sürükleme bakış | sürüklemesiz tık = MASAYA DÖN (K4, K6, K8, K8A/G/R; Karar K7) |
| Boşluk | dalış, açılış ve brif atlama | aynı |
| Tab / ↑↓ (panoda) | alanlar arası gezinme, kişi seçme | korunur |

### 3.12 Aile kapsamı

| Aile (sayı) | Aşama | Bu haritadaki yer |
|---|---|---|
| `test` (1) | 0 | §3.1 |
| `desk` (20) | 2, 4 | §3.2, §3.5 |
| `walls` (12) | 2 | §3.2 |
| `board` (8: `PlanningBoard`, 5 kart, `RecipeBook`, `PersonFolder`) | 3, 5 | §3.3, §3.4 |
| `door` (8) | 6 | §3.6 |
| `admission` (10) | 6 | §3.6 |
| `crt` (5) | 7 | §3.7 |
| `menu` (14) | 8 | §3.8 |
| `cabinet` (12) | 8 | §3.8 |
| `raidkit` (52: 12 × Relief, ReliefIso, Bridge, Rubble + 4 kanal) | 9 | §3.9 |
| `raidcivil` (30: 13 Civil, 13 EntryDoor, Tube, WeldArm, WeldArmBase, CeilingDebris) | 9 | §3.9 |
| `raidpieces` (26: 8 makine, 3 piyon, 5 dolap, 2 sandık, BoardBase, Tile, WallBlock, 4 işaret, SlidingShelf) | 9 | §3.9 |

### 3.13 Poz başına ışık (tarif özeti)

Gerçek tablo `Desk/Data/Rev3/PoseLightRecipe.asset`. K8'de aday varken tezgâh lambası da açıktır (3 gölgeli). Koridor lambası Spot 110° (A10-D7). BASKIN'da yön ışığı ortak 2048 (R15).

| Poz | Gölgeli | Gölgesiz |
|---|---|---|
| K1 | ampul (spot 120°), masa lambası, pano lambası | ampul dolgusu, CRT sızıntısı, telsiz kadranı |
| K4 | pano lambası | ampul dolgusu, ortam 0,5 |
| K5a | ampul, masa lambası | CRT, kadran |
| K6 | masa lambası, ampul | EVRAK hedef ışığı |
| K8 | ampul, masa lambası (+ açıkken koridor) | vuruş lambası |
| K8A/K8G/K8R | ampul, masa lambası, tezgâh lambası | buğu camı, vuruş lambası (emission) |
| K9 | ampul | dolap iç ampulü |
| M0 | sahanlık lambaları ve merdiven (en çok 3; token `M0`) | — |
| M0open / M3 | + içeriden sıcak spot | — |
| BASKIN | yön ışığı | dolgu, hedef spot |

---

## 4. Kararlar

Tablolar aslına sadıktır; satır durumu her tablonun altındadır. Eski numaralar ve kimlikler geçerlidir (§6.1 = §4.1 …).

### 4.1 (eski §6.1) Paketin önerdiği beş karar (PROMPT §3)

| # | Konu | Paketin önerisi | Bulgu | Önerim |
|---|---|---|---|---|
| 1 | M0 kamerası | (4,50 · 1,52 · 0,30) → (2,2 · 1,34 · 0,30), 56° | Bugün (4,30 · 1,66 · 0,20), 52° | **Yeni değerler** |
| 2 | Baskında giriş kapısı | alçak (≤ 0,30) ya da açık kasa | Bugün kapı yok; kamera güneybatıdan bakıyor | **Alçak 0,29**, 13 kapı aynı kural |
| 3 | Piyonun "tur bitti" yüzü | koyu kraft `r_pawnDone` #8C734E | Bugün `spentTint` (0,549 · 0,451 · 0,306) ≈ #8C734E | **Koyu kraft** (bugünküyle aynı) |
| 4 | Pano | ayaklı, (−1,62 · 0 · 1,70) yaw −48 | Bugün de ayaklı, eski rafın yerinde | **Ayaklı** |
| 5 | Kabul | kapıdaki tezgâh | Bugün kapı pozunda World Space pencere | **Kapıda** |

**Satır durumu.** 2–5 yürürlükte. 1: yeni konum yürürlükte, FOV ve hedef Aşama 8 okunurluk kalibrasyonuyla değişti (M0 hedef y = 1,25, 60°; §10.8).

### 4.2 (eski §6.2) Benim sorduğum kararlar

| # | Konu | Seçenekler | Önerim |
|---|---|---|---|
| K1 | Rev2/Rev3 anahtarı (iş bitti ölçütü 6) | A: anahtar yok, git · B: `SiginakPresentation` anahtarı, iki hiyerarşi | **A** (Rev2'deki kararın). Ölçüt 6 ve "her aşamada anahtar çalışır" düşer. Yerine her aşamada önce/sonra görüntüleri (`screens_unity_rev3/`), commit'i sen atarsın |
| K2 | `Rev3RoomBuilder` | AGENTS kural 5: kalıcı sahne kurucusu yok | Model importer kalıcı (asset hattı); odaya yerleştirme aşama başına **tek seferlik** script |
| K3 | Klasörler | PROMPT `Presentation/Siginak/Rev3/` (assembly yok) | §2 düzeni |
| K4 | DIŞ KAYIT gözetmeni (Soru 2) | A · B · C | **A**, `CurrentVersion` 15 ile |
| K5 | R18 üretim (Soru 9) | A · B · C | **A** (kural değişmez). B tasarıma birebir ve Core'a dokunmuyor; istersen B |
| K6 | Big Shoulders Stencil Display | indirme izni (Google Fonts, OFL, TTF) · dosyayı sen eklersin · CP Bold | **İndirme izni** |
| K7 | Sağ tık | bugün sağ sürükleme bakış | Sürüklemesiz sağ tık = MASAYA DÖN (K4, K6, K8, K8A/G/R); sürükleme bakış |
| K8 | K6 pozu | ayrı yeni poz · K6a çapası taşınır | Ayrı yeni poz |
| K9 | Eski yol görselleri (Soru 5) | yükselt · bırak · sil | **Bırak**; silme Aşama 10'da |
| K10 | PlayMode testleri (§8.6) | yeni PlayMode asmdef · EditMode + Play'de elle deneme | **Yeni asmdef** (`Corrigible.Desk.PlayTests`): sürükle-bırak ve şalter → fiş → EVRAK; `GELISTIRME.md`'ye çalıştırma notu |
| K11 | Portreler (Soru 12) | A · B · C | **A** |

**Satır durumu.** K1–K4, K6–K8, K10, K11 yürürlükte (K4 uygulandı: §4.6 D1–D4, biçim 15). K5 §4.5 D1 ile **B**'ye değişti. K9: bırakıldı; silme A10-D1 (T1) ile, kod A10-D2 ile kalır.

### 4.3 (eski §6.3) Aşama 3 kararları (2026-11-06: kullanıcı önerileri onayladı)

Aşama 3 planı (beş okuyucu + planlayıcı + eleştirmen) paketle PROMPT arasındaki çelişkileri çıkardı:

| # | Konu | Karar |
|---|---|---|
| D1 | K4 okunurluğu: FOV 42,5°'de kart düzlemi 1,44 px/mm; meslek, değer etiketleri, ustalık harfleri, şerit, eşya ve hammadde adları 13,3–13,7 px (eşik 14; PROMPT 1,52 px/mm diyor) | **K4 FOV 41°** (manifest `Cam_K4` notu): 1,50 px/mm; K4'te yakınlaşma geri ≤ 1,0. Şerit 9,2 → 9,4 mm; EN değer etiketleri 9,5 mm, hücreler genişler. "stok n / ayrı n" gerekçeli istisna |
| D2 | Punto küçülmez, kart alanı sarmaz; tasarımın bazı metinleri taşıyor | Ad **Libre Franklin 700** (görsellerdeki gibi; token Courier diyor), şerit karışık harf ve kısa biçimler, eşya adı LF 600. EN meslekler kısalır: Communications specialist → Radio operator, Repair technician → Mechanic, Healthcare worker → Medic, Electronics technician → Electronics. ZIRH → ARMOUR (sözlük). Taşma testi geçidi |
| D3 | Aşama 1'in büyük harf kuralı tek satırlık pano plakalarını taşırıyor | Pano plakalarında referans em (çizim büyük harfi / 0,7); küçük bilgi yuvalarında 9,4 mm taban; `TXT_P_emp` 10,6 mm; üç gerekçeli istisna (saha bandı, kilit etiketi, hurda 2. satır) |
| D4 | Gözetmen kuralı (Soru 2 / K4-A, `CurrentVersion` 15) | **Aşama 6'da**, kabulle birlikte; Aşama 3'te Core değişmez. **Uygulandı (biçim 15)**: Aşama 6, §6.6 D1–D4, §12 |
| D5 | KAYIPLAR zarfının yeri (tasarımın "pano yanı" K4 kadrajı dışında) | Kişi dosyasının arkasında, tel sepette, +50 mm sağda; dikey "KAYIPLAR · n" görünür, tıklayınca liste |
| D6 | Tarif defteri Aşama 5'e kadar | 3B kapalı `RecipeBook` kancada; tıklanınca bugünkü okunabilir defter (geçici tuval) açılır |
| D7 | Bakım ve İkmal'in etkisi yok (`RetireJobs`) | Core'un gerçek metni ("panoda bakım cebi yok"); ceplerin dönüşü ayrı tasarım konusu |
| D8 | 22 isim için portre yok (Derya, Orhan, adaylar) | Paketin üreticisi (`threejs/r3/portraits.js`) yeni yüz tarifleriyle; portre adı izler, robot durumunu asla |
| D9 | Kart yüzünü RenderTexture'a pişirmek (PROMPT §5: onayla) | Pişirilmez; görünüm ya da performans tutmazsa sorulur |

Ayrıca (onaylı önerilerden): Soru 1 → 7 konu satırı ve 7 ustalık satırı; TAHLİYE R15f'deki gibi 1,2 s basılı tutma (klavyede
dosya odaktayken Enter basılı); Boşta destesinde üst kart Boşta'nın ilk uygun kişisi (yatan ve kısıtlı olmayan), sonra kalanlar.

**Satır durumu.** D1–D3, D5, D7–D9 yürürlükte. D4 uygulandı (§4.6). D6 Aşama 5'in 3B defteriyle değişti (§4.5 D4, D10). D2'nin `Electronics` karşılığı A10'da da kaldı (G18).

### 4.4 (eski §6.4) Aşama 4 kararları (2026-11-06: kullanıcı önerileri onayladı)

Aşama 4 planı (beş okuyucu + planlayıcı + eleştirmen) R19 notları, three.js kaynağı, token ve kod arasındaki boşlukları çıkardı.
Core ve kayıt biçimi (14) değişmez; yalnız `PrintBoardShift` ve `FileBoardShift` çağrılır.

| # | Konu | Karar |
|---|---|---|
| D1 | Geçersiz plan "kilitli" mi, "yetersiz kaynak" mı | **Kilitli** (bant + kısa neden + Core'un iletisi üzerine gelme satırında). "Kol iner, ağız boş, 0,4 s'de kalkar" yolu yalnız beklenmeyen basım reddi için |
| D2 | Tasarlanmamış kilit nedenlerinin plaka metni | baskın sürüyor · harekât bitti · belge açık · rapor açık (eski yol) · plan düzeltilmeli · fiş basılamadı; uzun metin üzerine gelme satırında |
| D3 | EN terimleri | EN'de EVRAK yerine her yerde **PAPERS** (tepsi etiketi gibi): "file the slip in PAPERS"; FIELD · RAID TEAM; injured / laid up / restricted; kesme işareti ’ |
| D4 | Fiş yüzü tekniği | World Space TMP tuvali (1 birim = 1 mm), satırlar modelin 4 mm kıvrımının önünde; görünüm tutmazsa RenderTexture için ayrıca sorulur |
| D5 | Eldeyken girdi | tık / Esc / sürüklemesiz sağ tık dile döndürür; sürükleme elden de başlar; dilden sürükleme doğrudan EVRAK'a; EVRAK dışı bırakış 0,25 s kavisle dile; K1'den fişe tık K6 + el; elde x eğimi 0°, z −2,9° |
| D6 | Kapak 4 s'de kendiliğinden kapanınca | kamera K6'da kalır; dosyalama başarısında tam karanlıkta K1'e kesme |
| D7 | EVRAK kartlı oyunda (Soru 14) | dosyalanan fiş tepside üst kâğıt; kartlı oyunda Rev2 boş raporu eklenmez; EVRAK'a tık (fiş basılı değilken) son fişi elde salt okunur gösterir, ← emir belgesine geçer; son 5 fiş `liberation_desk.json`'da; KAYITTAN DEVAM'ın rapor parlaması kartlı oyunda atlanır |
| D8 | Geç durum görüntüleri | R19c V.001/V.031 için sunum fikstürü; kilitli ve kayıt/devam için gerçek komutlarla kısa kayıt |
| D9 | Fiş taşması ve punto | DOSYA uyarıları tür başına tek kutu, son çare "… +n", punto küçülmez; elde adlar ≥ 18 px, öteki ≥ 14 px |
| D10 | Sesler | yeni cue 107–114 klipsiz yer tutucu (kol geçici `SwitchClack`); kilitli tık 92, ele 97, dönüş 99; klipler ayrı istek |
| D11 | İki plaka rengi ve iki süre | `tokens_siginak.json`'a yerel anahtar ("_local_a4"); paket dosyasından ayrılış burada belgelenir |
| D12 | Fişin çıkış yolu | ağzın 40 mm içinden başlar (ilk karede kesik görünmez): yol 160 mm (tasarım 120 mm), süre 0,35 s; sapma belgelenir |

**Satır durumu.** D1, D2, D4–D9, D11, D12 yürürlükte. D3 yürürlükte; LAID UP A10-D4 ile tamamlandı, son EVRAK → PAPERS Aşama 10'da. D10'un klip kısmı A10-D5 ile değişti (cue numaraları geçerli).

### 4.5 (eski §6.5) Aşama 5 kararları (2026-11-06: kullanıcı D1'de B'yi, öteki önerileri onayladı)

Core ve kayıt biçimi (14) değişmez; yalnız var olan komutlar (`board_queue`, `board_reserve`, `board_cancel`, D2 için `board_stage`).

| # | Konu | Karar |
|---|---|---|
| D1 | K5 / Soru 9 yeniden: üretim A (Core tarifi seçer) mı B (oyuncu tarifi seçer) mi | **B**: tarif kartı defterden iş emrine = `board_queue`; fiş = `board_reserve` ±1; form dışına tarif = `board_cancel`. İlerleme kutuları, iki vardiyalık tarif, "başladı/durdu" gerçek; eksik iş emri vardiya şalterini kilitler. **K5'in A kararı bu aşamada B ile değişti.** R18c'nin "eşleşme · eksik saha kiti önce" cümlesi ve 04 sayfasının el notu B'de çizilmez (sapma) |
| D2 | Eski kayıtlarda konmuş fişler (`ProductionInputs`) | devamda her tür için bir kez `board_stage(kind, 0)` (kayıtlı); fişler stoka döner |
| D3 | Dolu emre yeni tarif; formdaki kart dışarı | tek bırakışta `board_cancel` + `board_queue`; formdaki kart form dışına = `board_cancel` (önizlemeli), kart deftere döner |
| D4 | Defter sayfaları | World Space TMP tuvali + sözcüksüz pişirilmiş taban, içerik `BoardRecipes`'ten; paket PNG'leri karşılaştırma için Docs'ta |
| D5 | Okuma pozunda punto | küçük etiketler ≈ 14 px'e büyür (poz three.js'teki gibi) |
| D6 | K4 punto kuralı yeni öğelere | tarif kartı adı 12 mm em, değerler ≥ 9,4 mm; YENİ 9,4 mm; form okuma pozu K4 yakın 0,6 |
| D7 | İlerleme kutuları, başlamış emrin fişleri | kutular yalnız `Turns > 1`; başlamış emirde fiş çizilmez ("girdiler ödendi · 1/2") |
| D8 | Form notları | durum ve "eksik" notu mavi kalem #1A2F5A, durma kırmızı DURDU damgası; açlık tahmini devir fişiyle aynı kural |
| D9 | Ürün kayışı ve YENİ | kart K4'e ilk gelişe dek TARİF kutusunda, sonra depoya kayar; YENİ ilk işaretçi girişi ya da tıkta düşer; durum layout dosyasında (oyun kaydına girmez) |
| D10 | Defter etkileşimi | öne gelme ile kapak örtüşük (0,35 s), kapanış 0,3 s; dışarı tık / Esc / sağ tık / MASAYA DÖN kapatır; açıkken öteki pano tıkları önce defteri kapatır; ←/→ sayfa; üzerine gelme 4 mm öteleme (tasarımın "eğilme"si sapma) |
| D11 | Kanca ve okuma pozu | Aşama 3 kanca pozu; three.js okuma pozu (0,315 m) |
| D12 | Kapak | sözcüksüz `tx_book_cover` + TMP yazılar |
| D13 | Yaprak çevirme | Leaf altında iki dörtgen (ön/arka sayfa) |
| D14 | Sesler | yeni cue 115–119 klipsiz yer tutucu; tarif bırakma 98, dönüş 99, ret 100 |
| D15 | Yerel token anahtarları | `tokens_siginak.json`'a `_local_a5` notuyla |

**Satır durumu.** D1–D13, D15 yürürlükte. D14'ün klip kısmı A10-D5 ile değişti.

### 4.6 (eski §6.6) Aşama 6 kararları (2026-11-07: kullanıcı bütün önerileri onayladı)

Core değişir (onaylı K4-A / D4): yalnız `Simulation.CommunityAdmissions.cs` ve `GameRecord.cs`; yeni komut, yeni kayıt türü ve RNG
çağrısı yok, `Replay.Apply` aynı. `GameRecord.CurrentVersion` 14 → 15: biçim 14 kayıtları yüklenmez, ilk yazışta
`liberation_savegame.json.v14.backup` olarak arşivlenir (biçim 14 hiç commit edilmedi; kayıp yalnız geliştirme makinesindeki kayıt).

| # | Konu | Karar |
|---|---|---|
| D1 | Gözetmen kuralının biçimi | gözetmen = Boşta'nın (`Job == "rest"`) ilk canlı, sağlam, kısıtsız ve hedeften farklı kişisi, `Board.People` sırasıyla (`AdmissionWatcher(targetId)`); `watch` artık sayılmaz (görev listede kalır, `RetireJobs` onu Boşta'ya alır) |
| D2 | Gözetmen sabitlensin mi | **dinamik**: her an Boşta'nın ilk uygunu; talep fişindeki ad canlı, Boşta boşsa kırmızı "gözetmen yok · sonuç gelmez". Tasarımın "o vardiya başka iş almaz" sözü kural değil uyarı (sapma) |
| D3 | Tek `ObservationTarget` yuvası | H1 (İNCELE + DIŞ KAYIT takılan `ReviewPending`) ve H3 (kurucu dosyasında yanlış mühür bulgusu) düzeltilir; H2/H4 son istek kazanır, sunum önceden uyarır |
| D4 | Sunum bulgu sonucunu nereden okur | Core'a salt okunur `AdmissionFindingCode(personId, kind)` ve `AdmissionWatcher(targetId)`; metinler aynı, sunum metin eşlemez |
| D5 | "robot" sözcüğü | kişi dosyası ve bulgu fişleri sunum ifadeleriyle (kodlardan), Core metinleri değişmez; talimat şeridinde kalır |
| D6 | Talimat metinleri ve sırası | Core'dan (v1'in iki kuralı dahil); YENİ / "sürüm n'den" önceki sürümle karşılaştırmadan; tasarımın İLKE satırı çizilmez |
| D7 | Talimat tarihi | oyunun kuralı V.005 / V.009 (tasarımın V.004 / V.008'i sapma) |
| D8 | Pozlar | K8 58° kalır; K8A, K8G token; K8R kâğıda bakan dinamik çapa (0,318 m, ≈ 4,67 px/mm, 0,25 s); tezgâh spotu 68° |
| D9 | Kâğıt tekniği | World Space TMP + düz kâğıt malzemesi (ışıksız font malzemesi); R10 PNG'leri yalnız karşılaştırma; buğu fırınlanmış `tx_fog` + alfa/ölçek |
| D10 | Kararın sunumu | kapı KABUL **ve KISITLI**'da açılır (R9-not "yalnız KABUL"dan sapma); kabulde kamera kendiliğinden K8'e, ret/yardımda K8A'da kalır; kanat açılmadan tezgâh katlanır |
| D11 | Etkileşim | damga sürükle-bırak + tık-tık; büyüteç tık ya da sürükle; talep tık ya da yaprak sürükle; buğu camı ve interkom tık; mercek büyütmesi yok |
| D12 | Vuruş ve tezgâh | vuruş canlı oyunda yeni adayda bir kez, yüklemede yok; tezgâh aday gelince iner; talimat değişimi ilk K8/K8A varışında (görülen sürüm `liberation_desk.json`'da, yeni oyunda güncel sürüm) |
| D13 | Geri zinciri | Esc / sağ tık bir seviye (damga → dizi → K8R → K8A → K8 → K1; K8G geldiği poza); MASA tuşu ve masa kenarı doğrudan K1 |
| D14 | K8A'da eşik altı plakalar | TALİMAT, BULGULAR büyür; raf plakaları ve damga başları gerekçeli istisna |
| D15 | Tasarımda olmayan fiş durumları, EN çakışmaları | BUĞU YOK / NO FOG ve KAYIT FARKLI / RECORD DIFFERS eklenir; tek TR anahtarın iki EN anlamı kullanım taramasıyla çözülür (gerekirse yeni TR anahtar) |
| D16 | `FileTag` | "İNCELEME" / "REVIEW"; kısıtlıda "KISITLI · {n} V" eki sığarsa |
| D17 | Kişi dosyasındaki iz | panodaki `PersonFolderView` + `FileTag`; R17f'nin açık dosya düzeni Aşama 7'ye (R17f bu aşamada kısmen) |
| D18 | Portreler | sayılı adda ("Suna 9") belirlenimci ayna portre; robottan bağımsız |
| D19 | Eski yol | dokunulmaz (silme Aşama 10) |
| D20 | Eski uGUI kabul penceresi | alt ağaç silinir; `PlanningBoardView` alanları null, public API korunur (`ShowAdmissions` → yeni istasyon) |
| D21 | Damga kilidi görseli | yuva önünde küçük pirinç küp (yer tutucu, tasarımcıya bildirilir) |
| D22 | `RequestPad` yaprak açısı (Soru 21) | görünüm +35° çevirir, prefab değişmez |
| D23 | Sesler | yeni cue 120–128 klipsiz yer tutucu; damga 63, dönüş 99, ret 100, kapı 50/51/62 |
| D24 | Yerel token anahtarları | `tokens_siginak.json`'a `_local_a6` notuyla |
| D25 | Kapı çarpıştırıcısı | çerçeveye küçültülür, interkoma kendi çarpıştırıcısı |
| D26 | Teleks | yalnız "TALİMAT n" |

**Uygulamada değişen (Aşama 6 sonucu, 2026-11-07; ayrıntı §12):**
- **Büyüteç kuralı (PLAN §1.7 → tasarım kuralı):** PLAN "kullanılmışsa büyüteç inceleme pozunda kalır" diyordu. Görüntü
  karşılaştırmasında tasarım büyüteci yalnız karşılaştırma anında belgede gösteriyor (R17_inspect/read); sonraki durumlarda
  dinlenmede (R17_pending/stamped), aksi hâlde kıskaçtaki fişleri örtüyordu. Yeni kural: `PlayInspect` belgeye götürür; başka
  araç ya da damga işe girince `ReturnToRest(true)`; `Bind` götürmez; kayıttan devamda dinlenmede. Ayrıca damga eldeyken büyüteç
  çarpıştırıcısı kapalı (tık-tık'ın ikinci tıkı büyütece düşüp istenmeyen `inspectApplicant papers` yazıyordu).
- **D2 ekleri:** talep yuvası başka dosyaya geçince fiş ve formda "talep iptal edildi · sonuç gelmez" (PLAN'da yoktu). Dosyanın
  İNCELE sekmesinde üzerine gelme satırı olmadığından H2/H4 uyarısı iki tıklıdır (ilk tık not satırına uyarı, ikinci tık komut).
- **Dış kayıt sonucu** kapanışta gelir ama ilk K8/K8A varışına dek BEKLENİYOR tutulur (`AdmissionSnapshotBuilder.HoldsResult`);
  yüklemede zaten gelmiş sonuç görülmüş sayılır (`SiginakAdmission.ArrivedResultFor`). Bekleyen fişin notu "sonuç V.{gün+1}
  kapanışında" (tasarım sonucu görülen vardiyayı yazıyor).
- **D8:** `counterSpot` K8'de de açık (aday varken K8 = ampul + masa + tezgâh = 3 gölgeli), öteki pozlarda tarifte açıkça kapalı.
  Modelin spotu dik aşağı bakıyordu; `AdmissionStation.AimCounterSpot` tezgâh ortasına (~11° eğik) çevirir. Şiddet 2,0 kaldı.
- **D10 zamanlaması:** çark 0,93 s'de (PLAN 0,95), çekmece KABUL/KISITLI'da gidiş-dönüş 0,7 s; REDDET'te katlanış 1,1 s (PLAN 1,2),
  YARDIM'da çekmece kapanışı 0,95 s (PLAN 1,0).
- **D14:** TALİMAT prefab'da zaten eşiği geçiyor. BULGULAR, BULGU ve ESKİ sahnede büyütüldü (em 16,6 / 14,8 / 14,5 mm); BULGULAR
  rengi `materials.stencilPaint` #D9D2BF. Raf plakaları ve damga sapları tasarım puntosunda en çok, otomatik küçülme (en az %45),
  sarmasız; teneke ERZAK aynı biçimde (en az %70) ve gerekçeli istisna.
- **D15 (EN çakışmaları):** KABUL → ADMISSION (sap EN tasarımda "ADMIT"), REDDET → REJECT, DIŞ KAYIT → OUTSIDE RECORD; KISITLI
  RESTRICTED kalır (sap EN tasarımda "LIMIT"), BEKLENİYOR AWAITING kalır (fiş damgası tasarımda "PENDING"); BELGELERİ KARŞILAŞTIR ve
  DIŞ KAYIT TEYİDİ değişmedi. KABUL izinin iki satırı "KABUL · TAM ERİŞİM" anahtarından bölünür (EN ADMIT / FULL ACCESS).
- **D16:** FileTag sığmazsa önce sarar, en çok %55'e küçülür; EN eki "LIMIT · n S".
- **D21–D25:** pirinç kilit dili 40 × 6 × 3 mm küp; kapı kutusu PLAN'daki yerinde buğu camını yutuyordu → merkez (0; 1,05; −0,13),
  derinlik 0,23 m (kanadın arkası); masa kenarı hedefi (`SiginakAdmission.deskEdge`) K8 kadrajında görünmediği için kurulmadı;
  tenekeler yan yatık (DA:149); interkom `TXT_Plate` öteki plakalarla aynı ışıklı malzemeye geçti.
- **GİDEN yaprağı:** GİDEN'de yalnız alt 67 mm görünür (`RequestFormFace.ShowBottom`: dörtgen kısalır + `RectMask2D`); bütün
  kâğıt ağzın üstünde kalıp GİDEN · TELSİZ plakasını örtüyordu.
- **Kâğıt ayrıntıları:** YARDIM izi oval değil dikdörtgen çift kenar; mühür halkasının "KYGA · MÜHÜR" yay yazısı çizilmez; BEDEN
  satırı CAN/HAR/GÜÇ (Core'da profil metni ayrı alan değil); koçan künyesi 14 px (13 px'te K8R'nin uzak köşesinde 13,7 px kalıyordu).
- **W7:** P19'da takastan sonra gözetmen İpek değil **Suna** (takasla Boşta'ya dönen cep sahibi `Board.People`'da önde);
  P6 K8'den; test kurulumları kayıt fikstürüyle (Core komutları + kayıttan devam).

**Satır durumu.** D1–D13, D16, D18, D20, D22, D24–D26 yürürlükte. D14 yürürlükte, BUĞU CAMI plakası da otomatik puntoya geçti (G01). D15'in EN damga ve fiş kısmı A10-D3 ile değişti (ADMIT / LIMIT / PENDING; `Ctx`). D17 Aşama 7'de tamamlandı. D19: A10-D1 ve A10-D2 ile sonuçlandı. D21 yer tutucu, tasarımcıya (§7). D23'ün klip kısmı A10-D5 ile değişti.

### 4.7 (eski §6.7) Aşama 10 kararları (2026-11-08: kullanıcı cevapladı)

Haritanın A9 sonundaki tam metni `IMPLEMENTATION_MAP_A9.md`'dedir. Kararlar `A10-D` önekiyle anılır (öteki D numaralarıyla karışmasın).

| # | Konu | Karar |
|---|---|---|
| A10-D1 | Rev2 görünümünden ne kalkar | **T1**: kapalı raf ve maket (`SiginakRoom/Shelf`, `Shelter controller`), eski uGUI pano tuvali (`Serbest pano kartları`), erişilemeyen asset'ler, bayat `SiginakCaptureSet`. Silinenler önce `A10_silinenler.zip`'e. Oyun kodu silinmez; T2 yapılmaz |
| A10-D2 | Eski oyun yolu (Core dahil) | **Kalır**; `LegacySimulation` testleri yerinde |
| A10-D3 | EN damga sözcükleri | ADMIT / LIMIT / PENDING (`DeskLoc.Ctx`, Core'a dokunmaz) |
| A10-D4 | YATIYOR EN | LAID UP |
| A10-D5 | 42 sessiz ses ipucu | **Hepsine yer tutucu klip** (`Tools/Audio` ile sentez); klip yok kilitleri güncellenir (D10/D14/D23'ün "klipler ayrı istek" çizgisini değiştirir) |
| A10-D6 | Q13 gölge türü | Yumuşak kalır; D8 hedefi aşılırsa kullanıcıya dönülür |
| A10-D7 | Gölge çözünürlüğü hatası | Onarım B: üç ışık 2048 (sahne override'ı) |
| A10-D8 | Kare hedefi | Medyan ≤ 16,7 ms, p95 ≤ 20 ms (dev Mac, Editor, en kötü durumlar); PC vekili değil |
| A10-D9 | Oyuncuya TAA/post ayarı | Eklenmez; açık/kapalı görüntü alınır |
| A10-D10 | R14b yıpranma detay haritası | Sapma olarak kaydedilir |
| A10-D11 | `RaidPencil.shader` | Kabul edilmiş sapma |
| A10-D12 | Haritanın son hâli | Sadeleşir; tam metin `IMPLEMENTATION_MAP_A9.md` |
| A10-D13 | D-A · Bulgu fişi cetvelin altında kalıyor (G06) | Tasarıma uyduruldu: karşılaştırma fişi, cetvel belgedeyken ağızda kalır, cetvel kalkınca kıskaca düşer |
| A10-D14 | D-B · Ayarlar rozetindeki dişli (G22) | Eklendi (`ic_gear_l`, kanat parçasında) |
| A10-D15 | D-C · K3b/K3c EN sözcükleri tasarımdan farklı (G28) | Kalır, sapma olarak yazılır (§5) |
| A10-D16 | D-D · Plaka terimleri (`GRID`, `BACK TO MENU`, `Analyst kit`, `COMPARE PAPERS`) | Kalır, sapma olarak yazılır (§5) |
| A10-D17 | D-E · Telsiz anteni K9'da kanadın yayına giriyor | K9'da telsiz gizlenir (`SiginakCabinet.radio`) |
| A10-D18 | D-F · Eksik tasarım ayrıntıları | Yapıldı: M0 tehlike bandı, iş emri ipucu GİRDİLER (hammaddeyi buraya bırak), dosya sekmesi alt satırları |

**Satır durumu.** A10-D1..D18 yürürlükte. A10-D1, D3–D5, D7, D12, D13, D14, D17, D18 uygulandı; A10-D2, D6, D9, D10, D11, D15, D16 kararı gereği uygulama değişikliği gerektirmedi ya da sapma olarak yazıldı; A10-D8 ölçüldü (§1).

---

## 5. Tasarımdan sapmalar

Tek liste. "Karar" sütunu kullanıcı onayını ya da kaynağı gösterir.

**Çerçeve ve kural**

| Konu | Sapma | Karar / kaynak |
|---|---|---|
| Anahtar (ölçüt 6) | Rev2/Rev3 anahtarı yok; önce/sonra görüntüleri | K1 |
| Kayıt biçimi (ölçüt 5) | 14 → 15, yalnız gözetmen kuralı | K4, §4.6 |
| `Rev3RoomBuilder` | Kalıcı oda kurucusu yok; aşama başına tek seferlik script | K2, AGENTS 5 |
| Klasörler | PROMPT `Presentation/Siginak/Rev3/` yerine §2 düzeni | K3 |
| Üretim | Tarifi oyuncu seçer (R18c "eşleşme" cümlesi çizilmez) | §4.5 D1 |
| "Özel shader yok" (PROMPT §2) | `RaidPencil.shader` kaldı (kare işaretleri) | A10-D11 |
| R14b yıpranma detay haritası | Yapılmadı | A10-D10 |
| Kâğıt albedo ≤ 0,8 (PROMPT §2) | `Recipe page L/R.mat` r = 0,9: kamera dolgusu sayfayı griye çekiyordu | §10.5 |
| Punto küçülmez (ölçüt 4) | Gerekçeli istisnalar 149; damga rafı, sap, teneke otomatik küçülür (rafta en düşük 0,57); BREATH GLASS / BUĞU CAMI %75 | §4.6 D14, G01 |
| Baskın gölgesi | Tarifin 4096'sı yerine ortak 2048 | R15, §9 |
| Gölge doğrulaması | PROMPT'un seçeneği Frame Debugger yerine Light listesi + atlas yoklaması (CLI sürülemez) | Aşama 10 |
| Oyuncuya TAA/post ayarı | Eklenmedi | A10-D9, Soru 13 |

**Aşama 4–6 bilinçli sapmaları** (§10 özetlerinden taşındı)

| Aşama | Sapma |
|---|---|
| 4 | Fiş yolu 160 mm (D12); kilitli titreme üç tümsek; fiş yüzü ışıksız mürekkep; K6 ortam dolgusu 0,3; EN "HAND-OVER" iki satır; `motion.shiftClose.slipPickS` temada yok, kod yedeği |
| 5 | Defterin üzerine gelmede eğilme yerine 4 mm öteleme; tarif kartının ilerleme kutuları kendi satırında; sayfa ve kutu renkleri ışık telafili; EN İŞ EMRİ sekmesi uzar; okuma pozunda alt satırlar 19 px; 04 sayfasının el notu yok |
| 6 | Kapı KISITLI'da da açılır (D10); gözetmen dinamik, "başka iş almaz" kural değil uyarı (D2); talimat tarihi V.005 / V.009 (D7); İLKE satırı yok (D6); "robot" yalnız talimat şeridinde (D5); mercek büyütmesi yok; büyüteç yalnız karşılaştırmada belgede; BUĞU YOK ve KAYIT FARKLI fişleri (D15); YARDIM izi dikdörtgen; mühür yay yazısı yok; BEDEN satırı CAN/HAR/GÜÇ; fiş yolu 160 mm; pirinç kilit dili yer tutucu (D21); masa kenarı hedefi (`deskEdge`) bilinçli boş |
| 6 (gözlem) | Katlı tezgâh KABUL şablonu referanstan iki kat yüksek; `AdmissionPanel`, `AdmissionCounter`, `CounterLamp` duvar plakaları tek yüzlü, çift yüzlü malzeme eklenmedi |
| 7 | Görev fişi ~950 px (referans 887), üst pay 128 px; kişi dosyası sağda ek yaprak kullanır; R10 başlık çizgileri kırmızıya uyarlandı |
| 8 | R4 süreleri kart hareketinden sayılır, M0'dan seçilirse 0,35 s saat yaklaşımı eklenir; süs parafı 10,62 px gerekçeli; DİL plakası 112 mm (Aşama 10'da daha geniş), menü plakası 120 mm |
| 9 | `BackupCore` kullanılmaz; uzun EN bina adı için antet 364 × 184 px; R12 malzeme testleri paket paletini denetler; bağımsız örnek sahnede kampanya vardiyası yazılmaz |

**Aşama 10: İngilizce**

| Kimlik | Sapma | Karar |
|---|---|---|
| D-C (A10-D15) | K3b/K3c tasarım EN'inden farklı sözcükler: `FIT`/`HEALTHY`, `DEPLOY`/`LAUNCH THE RAID`, `Cancel`/`cancel · back to the slip`, `POWER`/`STR`, `FIELD SQUAD`/`· TEAM ALPHA`; kutulu uyarı, `READY` damgası, Esc/Enter tuş başlıkları (G28) | Kullanıcı: kalır |
| D-D (A10-D16) | Plaka terimleri: `GRID`/`MAINS`, `BACK TO MENU`/`TO MENU`, `Analyst kit`/`Analysis kit`, `COMPARE PAPERS`/`DOCUMENTS` | Kullanıcı: kalır |
| G18 | `Elektronikçi` EN `Electronics` kalır: `Electronics tech` 73,4 mm, `Meslek` alanı 72 mm | Sığma |
| Eski kayıtlı | `SES` → `SOUND`, `DEVİR FİŞİ` → `HAND-OVER` (plaka satır kırar) | §10.2, §10.4 |
| WP-H | Kişi dosyası sekme başlığı `DISMISS` (tasarım `RELEASE`); `iki adım / two steps` tasarım metni, gerçek eylem 1,2 s basılı tutma | Kullanıcıya not |
| Gösterim | Kurucu dosyalarında `BELGE NO` / `SEVK NO` `K-003`, `PORTRE İZİ` `P-003` biçiminde (ham `sakin-3` sızıyordu). TR'de de görünür; Core, kayıt, RNG aynı | Aşama 10 |

**Aşama 10: görsel ve okunurluk** (kimlikler A10_DOGRULAMA §Dil)

| Kimlik | Durum |
|---|---|
| G07 | Dolap dolgu ışığı 0,35 → 0,04 ile sıcak nokta kalktı; `AMBIENCE` plakasında perçinler sözcüğe değer; sayaç rakamları yumuşak, teker 0–9 tamamı kanıtlanmadı |
| G09 | A3'te iki fiş üst üste okunur (0,6 mm aralık, 3,3 px); render hatası gösterilemedi. `PENDING` yalnız tarama değeriyle kanıtlı. Yelpaze katman adımı 1 mm (tasarım 0,4 mm) |
| G13 | Üç sunucu simgesi + sayı 5 tasarlanmış davranış (`SadeRaidStateTests` kilitli) |
| G23, G27, G29 | Özgür bölge damgaları soluk; saat kadranı ve kart beyaz, ray rafın önünde; dosya kâğıtları ve tarif kutuları referanstan gri (181,179,169 / 210,189,149) |
| G30 | Küçükler: REVIEW etiket rengi, deste kenarında üçüncü şerit (K4b), boş sol sayfa (K4e), teleks durum kodu soluk, üstte `-2` uçan sayı (Z2), figür halkası |
| G22 (D-B) | `Badge icon` kanat parçasına taşındı; dişli güneş simgesine benzer, tasarımdan soluk |
| G26 | Klips gövdeye pişmiş, yazı altına taşındı; iptal fişinde çift çerçeve yok; üst yarı boş |
| G06 (D-A) | Karşılaştırma fişi cetvel kalkana dek ağızda bekler (manifest kıskaca düşer der). Kalan: karşılaştırma öteki araçlardan sonra yapılırsa kıskaçtaki eski fişlerin alt ~13 mm'si cetvelin altında kalır |
| D-E (A10-D17) | Telsiz K9'da tümüyle gizlenir; geri çekilişin başında geri gelir, kanat kapanırken anten kısa süre görünebilir |
| D-F (A10-D18) | İş emri ipucu K4h'de soluk (~1,16:1; tasarım ~1,3:1); K4f'de durmuş emirde kural gereği yok; sekme alt satırları sabit 3,0 mm ve durağan |
| K4c | PLAN LOCKED plakası ~1,18:1 (tasarım 1,59:1); ray, kilit ve not durumu taşır |
| Işık | K8A gölgeleri tasarımdan yumuşak; K4 ve K8A ~%13–20 koyu; AR4 kâğıdı soluk, kabul yeşil-sarı; damga başı mürekkebi |
| Öteki | S4 `LOCKED` çizgili zeminde zayıf; `TXT_Findings` üst kenarı en çok 1 px belirsiz; K1 teleksinde boş gövde satırı (V2-11); G08 boş yuva çerçevesi Tiled |

---

## 6. Açık kalanlar

| Konu | Neden açık | Kim | Bağlam |
|---|---|---|---|
| Yer tutucu seslerin kulak kontrolü | 42 klip yalnız biçim ve düzeyle ölçüldü | Kullanıcı | A10-D5, `ShiftLever` hâlâ `SwitchClack` |
| R15 baskın 4096 | URP asset'i ortak; proje kopyası gerekir, yapılmadı | Geliştirici | §9 |
| Oyuncu derlemesi ölçümü | Proje kopyası gerekir | Geliştirici | A10_DOGRULAMA §Performans |
| PC ölçümü | Steam PC donanımı yok; Mac vekil değil | Kullanıcı | A10-D8 |
| Kare süresinin 1920 × 1080 oyun döngüsünde teyidi | Ölçüm ortamı `screen=555x808`, odak yok yazıyor; hedef geçerli sayıldı ama çözünürlük doğrulanmadı | Geliştirici | A10_DOGRULAMA §Performans |
| Sahne yükleme süresi | T1 sonrası ölçülmedi; yalnız dosya boyutu | Geliştirici | A10_DOGRULAMA §Performans |
| TAA / post görüntü karşılaştırması | Görüntüler `Temp/a10_*` altında alındı, görsel hüküm kayda geçmedi; Temp kalıcı değil | Geliştirici | Soru 13, A10-D9 |
| Işık ve albedo kalibrasyonu | Şalter plakası 58/78, kâğıtlar, G23, G27, G29 onarım sonrası da kaldı | Tasarım / geliştirici | §5 |
| Soru 20 | 45 dokulu anahtardan 36'sı düz; görünen eksikler §5'te, tam göz turu yok | Tasarım | §8 |
| Soru 21 `CabMenuLever`, `CabToggle` | 0° dinlenme ve işaret çevirme kaydı yok; K9/M1 görüntüsünde özel teyit yazılmadı | Geliştirici | §7 |
| Büyüteç çarpıştırıcısı | K8A'da elde damga yokken belgenin altına tık sınanmadı | Geliştirici | §10.6 |
| `MagnifierTool` alan merkezleri | DÜZ., MÜHÜR, GELDİĞİ YER tahmini | Geliştirici | `AdmissionDocumentFace` |
| 2 cm altı parça gölgesi | Sahne denetimi yapılmadı (`noShadow` işareti var) | Geliştirici | PROMPT §2 |
| Üzerine gelme süpürmesi | Crt, Cabinet, MS, M1'de satır yok (0 hedef); ses açık/kapalı satırı ölçülmedi, sığma riski düşük | Geliştirici | A10_DOGRULAMA §Dil |
| Telsizin K9 dönüşü | Kod ve sahne bağı doğrulandı, K9 → K1 görüntüsü yok | Geliştirici | A10-D17 |
| TR'de ayrıntı | G21 B1'de, K3c HP pipleri; F2 yalnız EN görüldü, TR tarama değeriyle | Geliştirici | A10_DOGRULAMA §Dil |
| Kart TMP malzemesi 4 (hedef ≤ 3), 904 batch (dolu pano) | Kare süresi tutuyor; cilalama | Geliştirici | R1, R6 |
| Soru 10 emekli görevler | `PBP.RetireJobs` sunumda komut yazar | Kullanıcı | §8 |
| Q8 GÜVEN şeridi, Q12 onarım rengi | Rev2'den devreden tasarım soruları | Tasarım | §8 Soru 16 |
| Bakım ve İkmal cepleri | Eğitimde var, görev cebi yok | Tasarım | §4.3 D7 |
| R19b2 kilit çubuğu geri kayışı, K6 alt teleks şeridi | Kartlı pano K4 dışında anında oturur; şerit teleksi okunur tutar | Tasarım | §10.4 |
| Defter sayfa çizgileri, uyarı kutusu çerçevesi | Işıkta silik; kesik çizgi | Geliştirici | §10.4, §10.5 |
| Eski yol sunumu (T2) ve Core (T3) | Bilinçli kalır; `SiginakDoor`, `SiginakShift`, `PlanningBoardView` alanları null | Kullanıcı | A10-D1, A10-D2, A10_DOGRULAMA §Kaldırma |
| Eski baskın sanatı (169 dosya) | Test ve araç bağlı | Geliştirici | `SadeRaidBoardTests`, `RaidTileSkinTests`, `RaidRoomKitTests`, `RaidKitPreview` |
| Ana dilde EN incelemesi, Steam, gamepad, denge | Revizyon 3 kapsamı dışı | Kullanıcı | DURUM |

**Bilgi (karar değil):** `Docs/UI_Siginak_Rev3/` git'te izlenmiyor (~360 MB; `modeller/` 63 MB'ı `Rev3Manifest` okur, depoda olması gerekir), bu harita ve A10 kanıtları da öyle. `Corrigible_Build_1*` izleniyor (196 dosya). `UserSettings/CodexRecovery` (ignored, kayıt kopyaları) dokunulmaz. `.idea/.idea.Corrigible/.idea/indexLayout.xml` çalışma ağacında silinmiş.

---

## 7. Tasarımcıya bildirilecekler

Hiçbiri gönderilmedi; gönderim kullanıcıdadır.

1. `HandoverSlip.obj`: dışa aktarıcı indeksli `PlaneGeometry`'yi indekssiz yazmış, 64 yerine 15 yüz (içe aktarıcıda onarıldı; Soru 19).
2. 13 modelde manifest kutusu geniş: `Bomber`, `Civil_oc`, `LandingLamp`, `LastSwitch`, `LootCrateOpen`, `MaintRobot`, `Microphone`, `PlanningBoard`, `Replicator`, `Shavings`, `ShieldDrone`, `Stairs`, `Tube` (Soru 19).
3. Tek yüzlü duvar plakaları: `AdmissionPanel`, `AdmissionCounter`, `CounterLamp` için −z kapağı (Soru 21).
4. `BackupCore`: oyunda hangi nesne? (Soru 15).
5. Pirinç kilit dili yer tutucu, 40 × 6 × 3 mm (D21).
6. Telsiz anteni dolap kanadının yayına giriyor; bugün K9'da telsiz gizleniyor (A10-D17).
7. `CabMenuLever` dinlenme +35° kolu gömüyor; `CabToggle` parça notu işareti ters (Soru 21).
8. `tx_counterFraction` hücreleri "/30", gerçek ekler /60, /12, /20; `CounterRack`'te ek yuvası yok (Soru 11).
9. Katlı tezgâh KABUL şablonunun yüksekliği referansın iki katı (§10.6).
10. Terim ve tarih sapmaları: V.005 / V.009 (tasarım V.004 / V.008, D7), kart adı Libre Franklin (token Courier, §4.3 D2), EN sözcük farkları (A10-D15, A10-D16).
11. `lightsRecipe` M0 dışında düz metin; `board.layoutMm` gibi yapılandırılmış olması önerilir (gerçek tablo `Desk/Data/Rev3/PoseLightRecipe.asset`).

---

## 8. Çözülen sorular

Numaralar korunur: `DURUM.md`, kod yorumları ve testler "Soru n" diye atıf yapar. Tam tartışma A9 kopyası §4.

| Soru | Sonuç | Durum |
|---|---|---|
| 1 Eğitim konuları | 7 konu, 7 ustalık satırı, kademe 2 / 6 / 12 (§4.3 sonu). Bakım/İkmal cebi §6 | Çözüldü |
| 2 DIŞ KAYIT gözetmeni | A: Boşta'nın ilk uygun kişisi, biçim 15 (§4.6 D1–D4) | Çözüldü |
| 3 Sayaçtaki MALZEME | Plaka HAMMADDE / RAW MAT.; kırmızı nokta yerine form notları ve DURDU (§4.5 D8) | Çözüldü |
| 4 KAYIPLAR zarfı | Klasör modeli, dosyanın arkasında (§4.3 D5) | Çözüldü |
| 5 Eski yol | Core ve kod kalır (A10-D2), ölü raf ve tuval silindi (A10-D1) | Çözüldü |
| 6 Tek Sahanlık prefab'ı | İki ayrı kurulum kalır (§3.8) | Çözüldü |
| 7 Baskın ölçeği | 132,58 px/birim; `RaidKitPreview.cs:30` yorumu düzeltildi | Çözüldü |
| 8 K5a kamerası | +0,14 m, 52° (§10.7) | Çözüldü |
| 9 Üretim | B: oyuncu seçer (§4.5 D1) | Çözüldü |
| 10 Emekli görevler | `RetireJobs` kaldı; bilgi | Açık (§6) |
| 11 ERZAK "/60" eki | TMP ek, doku eksiz | Çözüldü (§7 madde 8) |
| 12 Portreler | A: paket üreticisiyle 22 yeni; ayna kuralı D18 (§4.3 D8) | Çözüldü |
| 13 Oyuncu TAA/post ayarı | Eklenmez (A10-D9); açık/kapalı ölçüldü | Çözüldü; görsel hüküm §6 |
| 14 EVRAK içeriği | Fişler (§4.4 D7) | Çözüldü |
| 15 `BackupCore` | İçe aktarıldı, bağlanmadı | Tasarımcıya (§7) |
| 16 Rev2'den devreden | Q3 K = 4 eş anlamlı. Q13 yumuşak gölge kalır (A10-D6). Q5, Q6, Q10 kartlı oyunda konusuz. Q8, Q12 açık | Kısmen (§6) |
| 17 Punto kuralı | Gerçek oran + pano plakalarında referans em (§4.3 D3) | Çözüldü |
| 18 Yuvanın fontu | Rol kazanır | Çözüldü |
| 19 Paket kusurları | İçe aktarıcıda onarıldı | Tasarımcıya (§7) |
| 20 Eksik dokular | Fırınlayıcılarla kısmen; 36 anahtar düz | Açık (§6) |
| 21 Paket verisi sorunları | `RequestPad` D22; `TelexPaper` Aşama 2; buğu camı D9; `LogBook` Aşama 7; `CabMenuLever`, `CabToggle`, tek yüzlü plakalar açık | Kısmen (§6, §7) |

---

## 9. Riskler

| # | Sonuç |
|---|---|
| R1 kart başına tuval | Aşama 3 ve 10 ölçümü: dolu pano 904 batch, medyan 4,4–4,7 ms; pişirme gerekmedi (§4.3 D9) |
| R2 sürükleme geçişi | Kapandı; `PersonCardDragPlayTests`, klavye yolu korundu |
| R3 `ShiftSwitch` ortak | Kapandı; kip alanı, üç yerin testleri |
| R4 token düğümleri | Kapandı; `_was_1_3` korunur |
| **R5 gölge atlası ve TAA** | **Sonuç:** ek ışık gölgeleri Spot; atlas ölçek uyarısı 0, Point 0, poz başına ≤ 4 dilim. URP Custom −1 hatası (65535 isteği) üç ışıkta 2048 ile giderildi (A10-D7), `Rev3RoomTests` kilitli. TAA kapalı ölçüm P1, P3, P19a; görsel hüküm §6 |
| R6 198 model | Batch ölçüldü (A10_DOGRULAMA §Performans); ek önlem gerekmedi |
| R7 yol bağlı testler | Kapandı |
| **R8 kayıp referans** | **Sonuç:** her sahne adımında `Rev3SceneRefAudit`; T1 boşalanları yalnız beklenenler (`CampaignWear` 8, View/Motion 96); üç sahnede eksik script 0 |
| R9 anahtar | K1 ile düştü |
| R10 font | Kapandı (K6) |
| R11 kural değişikliği | Biçim 15; oyuncu kaybı yok, biçim 14 hiç commit edilmedi |
| **R12 ses klipleri** | **Sonuç:** 42 sessiz cue'ya yer tutucu klip (A10-D5); bütün cue'lar bağlı, kulak kontrolü açık |
| R13 fiş tepside kalır | Kapandı; her ret `ReturnToOutput` |
| R14 sırayla bağlı kartlar | Kapandı; kimliğe göre havuz |
| **R15 baskın 4096** | **Sonuç:** değiştirilmedi; ortak 2048, 4 kaskad, baskın medyanı 2,7–3,2 ms. Kit kenarları Aşama 9 görüntülerinde ayrılıyor. Proje kopyasında deneme yapılmadı (§6). R15'in "DeskGame ve Giris de etkilenir" cümlesi ana ışık haritası için yanlıştır: o sahnelerde Directional yok |
| R16 portre üretimi | Kapandı |

---

## 10. Aşama sonuçları

Her aşamanın "Yapılan" anlatısı kısaltıldı; tam metin `IMPLEMENTATION_MAP_A9.md` §7–§15. Ölçüm tabloları, kabul eşlemesi, tek seferlik script adları ve görüntü adları korunur. Bütün tek seferlik script'ler çalıştırılıp silindi (AGENTS 5).

### 10.1 Aşama 1 · model hattı (2026-11-06; eski §7)

- Token 1.3 yerinde, `SiginakThemeSetup` kaldırılan 26 düğümü `_was_1_3`'ten korur. Big Shoulders Stencil ExtraBold (statik OTF, `xotypeco/big_shoulders`; OFL `Fonts/OFL-BigShoulders.txt`; 148 karakterlik atlas, 7 işaret DejaVu yedeğinde).
- Model hattı ve 198 prefab; 340 anahtar → 335 malzeme (45'i swap durumu, 6 baskın işareti Unlit, dokulu 45 anahtar düz renkle başladı). `HandoverSlip` içe aktarıcıda onarıldı; ışımalı malzeme `RealtimeEmissive`; prefab ışıkları kapalı, açan poz ışığı; çarpıştırıcıları bağlayan aşama ekler.
- Punto kuralı: gerçek büyük harf oranı, taşan yazı sarar (Soru 17); şablon rolü Big Shoulders (Soru 18); notu "N satır" olan örneklerde " / " satır sonu.
- Tek satıra sığmayan yuva: TR 20, EN 23 (liste A9 kopyası §7; her biri yerleştiği aşamada ele alındı).
- Doğrulama: `model_import_report.txt` 198 model, uyarı yok; üçgen eşit, sıfır pozda kutu OBJ köşeleriyle ≤ 0,1 mm. `Rev3ModelTests` 8 test (76 dönen, 13 kayan parça). Dokuz ajanla sayfa karşılaştırması: **182** birebir, **13** küçük fark, **3** sorun (`CabMenuLever`, `CabToggle`, `TelexPaper`; Soru 21).
- Testler: EditMode **556/556**. Görüntüler `A1_<aile>_TR/EN.jpg`.

### 10.2 Aşama 2 · komuta odası temeli (2026-11-06; eski §8)

Tek seferlik `Rev3Stage2`. Core ve kayıt biçimi değişmedi.

- Odanın modelleri manifest `place` konumunda; Rev2 primitifleri silindi, bileşen kökleri ve dış referanslar korundu (`Rev3SceneRefAudit` her grupta).
- Kararlar: Aşama 4'e dek Rev2 fişi; CRT tuvali camın 10 mm önünde; geç masa eşiği V.016; teleks çarpıştırıcısı yalnız makine; çark +450°; telsiz düğmesi EN "SOUND"; kupa z 1,10; plaka yazıları ışıklı TMP; teleks iletisi 0,182 punto (gerekçeli istisna).
- Poz ışığı: `PoseLightRecipe.asset` + `PoseLighting`; K1 3 gölgeli, K4 1, öteki 2. Ampul 120° spot + gölgesiz dolgu (`BulbFlicker`). three.js koni açısı yarım açıdır (58° → 116°, 47° → 94°). Kalibrasyon: ampul spot 2,0 / dolgu 2,0, masa lambası 3,5, pano lambası 0,9 (K4'te 1,0); ortam gök ×2,5, ekvator ×5, yer ×4; `Env fill` K1 0,4, K4 0,3, K8 0. K8: ortam 0,3, ampul dolgusu 0,7.
- Kod (yalnız sunum): `CounterWheel`, `CounterModule`, `ChalkTally`, `BulbFlicker`, `ShiftReceiptMachine.slipLamp`, `Teleprinter`, `CampaignWear`, `RadioLogBook`, `EdgeLine`, `PoseLighting`. `Rev3TextureBaker` (teker 128 × 1408, 11 hücre).
- Ölçüler: V.001 sayaçları R5a ile birebir (001 · 060,0 · 24/60 · 008 · 00/12 · 10/20); bölge parlaklığı K1'de 86'ya karşı 88.
- Denetim: beş gözden geçirici + şüpheci, 26 bulgu. Testler: EditMode **562/562**. Görüntüler `A2_K1_once`, `A2_K1_V001`, `A2_K1_V031`, `A2_K4`, `A2_K5a`, `A2_K6a`, `A2_K8`, yan yana `A2_*_R5a/R5b/R9`, `A2_sayac_yakin`, `A2_telsiz_yakin`.

### 10.3 Aşama 3 · kartlı pano ve kartlar (2026-11-06; eski §9)

Tek seferlik `Rev3Stage3Cards`, `Rev3Stage3Board`, denetim `Rev3Stage3Check`. Kararlar §4.3.

- `PlanningBoardSnapshotBuilder` → `BoardSnapshot` → `BoardView3D` (RNG çekmez, komut yazmaz). `BoardDragController` → `BoardDropRules.CommandFor` → `GameController.ExecuteBoardCommand` → `drag.Resolve(ok)`. Sunum durumu `BoardUiState`, `planning_board_layout.json` v2.
- Kart prefab'ları `Prefabs/Rev3Board/{PersonCard, ItemCard, MaterialCard, ChipCard}`: paket kartı + World Space yüz + sözcüksüz pişirilmiş taban; yalnız değişince bağlanır (`CardFace.ChangedBinds`). `CardStateOverlay`, `PortraitLibrary` (32), kimliğe göre havuz.
- Bileşenler: `BostaDeckView`, `StoreView`, `WorkOrderView`, `TrainingTopics`, `PersonFolderView`, `LostEnvelopeView`, `HurdaBin`, `BoardMarks`; sesler `BoardSound` → 97–106. K4 FOV 41°, `PoseSeconds`, `BackClicked`.
- Kabulde: ışıklı TMP malzemesinin camgöbeği dış çizgisi saydam yapıldı; pano lambası yakın düzlemi 0,05 → 0,1; EN etiket aralığı; İNCELE sığdırması çizilen sınırla; EN kısaltmalar.
- Görüntüler: `A3_K4_V001_TR/EN`, `A3_drag_*`, `A3_swap_*`, `A3_invalid*`, `A3_deck_*`, `A3_locked*`, `A3_fixture_max_*`, `A3_folder_*`, `A3_R16a/b/c`.

- **Ölçüler (Play, 1920 × 1080):**
  - K1 → K4 0,61 s (0,6), K4 → K1 0,51 s (0,5).
  - Canlı K4 taraması TR ve EN, V.001 ve en dolu pano: ad **21,0 px** (≥ 18), değerler (meslek/şerit, eşya, hammadde ve fiş
    adları) **14,1 px** (≥ 14), taşma 0. Gerekçeli küçükler: İNCELE 6,8, köşe sözcükleri 5,0–7,0, yuva etiketi ve kullanım
    satırı 8,1, "serbest" 7,5 px. Editör taraması 1414 metin, 0 ihlal, 144 gerekçeli istisna (`punto_taramasi.txt`).
  - Başarım (editör; 300 kare + eşzamanlı 30 çizim MSAA 4×):

    | Senaryo | Kart | Kare medyan / p95 | Toplu çizim / SetPass | GC/kare | Bağlama Δ | 30× çizim |
    |---|---|---|---|---|---|---|
    | K1 masa (karşılaştırma) | 18 | 5,2 / 5,7 ms | 790 / 110 | 16,8 KB | 0 | 9,33 ms |
    | V.001 (K4) | 18 | 4,8 / 5,7 ms | 472 / 115 | 16,9 KB | 0 | 6,46 ms |
    | En dolu: 10 kişi + 32 eşya + 4 hammadde + 4 fiş | 50 | 5,9 / 6,4 ms | 902 / 248 | 16,9 KB | 0 | 9,43 ms |
    | En dolu, deste açık | 50 | 5,8 / 6,3 ms | 902 / 251 | 16,9 KB | 0 | 8,78 ms |
    | En dolu, 260 karelik sürükleme | 50 | 5,8 / 6,6 ms | 903 / 250 | 17,2 KB | 0 | 8,68 ms |

    Kartların A/B'si (50 kart kapalı/açık, 3 × 100 çizim): açık 8,8–11,0, kapalı 9,0–9,3 ms; fark ölçüm gürültüsünde
    (≤ ~2 ms). 16,8 KB/kare GC K1'de de var (pano değil); sürükleme +0,3 KB/kare. Kartlarda 4 TMP malzemesi, örnek yok.
    D9'un eşiği aşılmadı; pişirme yapılmadı.
  - Kayıt/devam: komutlarla takas, deste, eğitim dışı atama → kaydet → devam: cepler, eşya, komut geçmişi aynı; kayıt sürümü 14.

- **Testler:** EditMode **638/638**, PlayMode **6/6**, çeviri 1846 metin.

### 10.4 Aşama 4 · vardiya kapanışı (2026-11-06; eski §10)

Tek seferlik `Rev3Stage4Slip`, `Rev3Stage4Scene`, denetim `Rev3Stage4Check`. Core ve kayıt biçimi (14) değişmedi. Kararlar §4.4.

- Şalter kipi `shiftClose`, `ShiftPhase`; kapak 0 → 112° 0,25 s, kapanış 0,3 s, kendiliğinden 4 s; kol +35 → −35° 0,3 s. `ShiftPlate` (6,4 mm, sarmasız). Süreler `motion.shiftClose.*`.
- Fiş: `ShiftReceiptPaper`, `ShiftReceiptMachine`, `EvrakTray`; `HandoverSlipFace`, `HandoverSlipRow`, `HandoverSlipWarning`, `HandoverSlipFormat`; prefab `Prefabs/Siginak/HandoverSlipView`; `HandoverSlipFactsBuilder` saf.
- Akış: `SiginakShiftClose` (kilit sırası baskın > harekât bitti > belge açık > rapor açık > plan; tam karanlık bir kare; son 5 fiş `liberation_desk.json`). Sesler `ShiftSound` → 107–114. K6: `SiginakPose.ShiftClose`, `Poses/K6`, ortam dolgusu 0,3.
- Sahne: `Physical receipt`'in Rev2 küpü kalktı; EVRAK'ta `Slip over`, `Slip target light`, `Filed slip`; referans denetimi yalnız dört beklenen fark. Fikstür `HandoverSlipFixtures`; `SiginakFixtures` kartlı kipte günü fişle ilerletir.
- Kabulde: ışıksız fiş mürekkebi; kararma tam siyaha ulaşmıyordu (alfa 0,5); EN "HAND-OVER"; `CP-Regular SDF URP Lit.mat` silindi; P1 ölçüsü.
- Görüntüler: `A4_R19a_*`, `A4_R19b_*`, `A4_R19c_V001/V031_TR/EN`, `A4_R19_evrak_arsiv`.

- **Ölçüler (Play, 1920 × 1080):**
  - Fiş elde: ad 18,2 px (≥ 18), öteki 14,1 px (≥ 14), TR ve EN, V.001 / V.031 / en kötü fiş, 0 ihlal. Plakanın 2. satırı K6'da 23,5 px,
    K1'de 14,4 px. Editör taraması 1658 metin, 0 ihlal, 143 gerekçeli istisna.
  - Kol tıkı → kol −35° 0,30 s, çıkış 0,35 s (yarı sürede ~%80); el 0,25 s; tam karanlık bırakıştan 0,30 s sonra, gün ilerlediği karede
    alfa 1,00; K1'e kesme karanlıkta.
  - Ölçütler: fiş EVRAK'a girmeden gün ilerlemedi (dil, el, EVRAK dışı bırakış: komut 0); fişi okumak zaman ilerletmedi; EVRAK'a
    bırakış tam bir `board_file`; aynı fiş yeniden kullanılmaz (Core testleri + P7); kayıttan devam basılı fişi dile ve şalteri bekleme
    hâline anında getirdi (fiş no ve komut sayısı aynı).

- **Testler:** EditMode **717/717**, PlayMode **20/20**, çeviri 1881 metin.

### 10.5 Aşama 5 · tarif defteri ve üretim (2026-11-07; eski §11)

Tek seferlik `Rev3Stage5Book`, `Rev3Stage5Cards`, `Rev3Stage5Scene`, denetim `Rev3Stage5Check`. Core ve kayıt biçimi (14) değişmedi. Kararlar §4.5 (**D1 = B**).

- Defter: `RecipeBookView`, `RecipeBookPage`, `BookPageShape`, `BookTypes`; World Space TMP + `tx_recipe_page`, kapak `tx_book_cover` + TMP; açılış 0,35 s, −178°, çevirme 0,25 s, kapanış 0,3 s.
- `RecipeCard` (112 × 80 mm, ad 12,1 mm em), `WorkOrderView`, `BoardDropRules`, `BoardPreviewText`, `ProductionForecast`; eksik emir şalteri kilitler; ürün kayışı, `NewItemTag`, layout v3. Sesler 115–119; `_local_a5`.
- Kabulde: `PartDriver.Value` eşik hatası; defter kipinde dosya tıkı; EN İŞ EMRİ sekmesi; alt satır 19 px; sayfa `_BaseColor` (0,9; 0,74; 0,62), tarif kutusu #D2C39E; `►` glifi `charset.txt` + DejaVu.
- Görüntüler: `A5_R18a_*`, `A5_R18b_surukle`, `A5_R18c_*`, `A5_R18_yetersiz`, `A5_R18_kilitli*`, `A5_R18d_*`, `A5_sayfa_1..3_tr/en_yan`.

- **Ölçüler (Play, 1920 × 1080):** defter okuma pozunda ad 20,6 px, öteki 14,7 px, TR ve EN, 0 ihlal; açılış 0,35 s, kapak −178°;
  tarif kopyasının bırakılışı tam bir `board_queue`; kilitli planda defter açılır, tarif ve fiş kalkmaz; kapanış → ürün depoda YENİ ile.

- **Testler:** EditMode **811/811**, PlayMode **41/41**.

### 10.6 Aşama 6 · kapı ve kabul (2026-11-07; eski §12)

Tek seferlik `Rev3Stage6PoseRecipe`, `Rev3Stage6Papers`, `Rev3Stage6Scene`, `Rev3Stage6Fix`, denetim `Rev3Stage6Check`. Kararlar §4.6. **Core değişti** yalnız `Core/Simulation.CommunityAdmissions.cs` ve `Core/GameRecord.cs`; yeni komut, kayıt türü ve RNG çağrısı yok.

- Core: C1 `AdmissionWatcher(targetId)`; C2 gözetmensiz red iletisi; C3 (H1) `InspectApplicant("record")` önce `ReviewPending` temizler; C4 (H3) `FounderIdentity`; C5 `PaperCode`; C6 salt okunur `AdmissionFindingCode`; C7 `CurrentVersion` 14 → 15, biçim 14 ilk yazışta `.v14.backup`. IL karşılaştırması: `Replay`, `AdvanceAdmissions`, `GenerateApplicant`, `Balance` aynı; tohum 79 çizelgesi `AdmissionSupervisorTests.C8_*`.
- Kapı (`DoorProp` iç saat, süreli `SetOpen`; kartlı oyunda `SiginakAdmission`'a devreder). İstasyon: `AdmissionStation`, `StampDecision`, `MagnifierTool`, `IntercomTool`, `ColdGlassView`, `RequestPadTool`. Kâğıtlar: `AdmissionPaperFace`, `AdmissionDocumentFace`, `OrdersStripFace`, `FindingSlipFace`, `RequestFormFace`, prefab'lar `Prefabs/Rev3Admission/*`; buğu `Rev3TextureBaker.BakeFog`.
- Veri ve akış: `AdmissionSnapshotBuilder` + `AdmissionRuleMap`; `SiginakAdmission` (geri zinciri D13, `DecisionTimeline` 0,35 / 0,58 / 0,93 / 2,83 s, teleks D26, sesler 120–128). Sunum durumu `SiginakDeskStore.AdmissionSeen`. Pozlar K8A 8, K8G 9, K8R 10.
- Sahne: `SiginakRoom/Admission station`, buğu camı kanatta, eski `Admission papers` alt ağacı silindi (D20). Referans: 15 beklenen BOŞALDI + 2 kabul edilen satır. `_local_a6` 43 yol; 96 yeni çeviri.
- Kabulde: `ArrivedResultFor`; karar sonrası istasyon görüntüsü; buğu tepesi; koçan künyesi 14 px; damga eldeyken büyüteç çarpıştırıcısı kapalı; `SnapOutgoing`; `AimCounterSpot`; `ClipLocal` y; BULGULAR #D9D2BF; `RectMask2D` ile GİDEN; raf ve teneke yazıları otomatik punto.
- Fikstür `AdmissionFixtures`; `SiginakTextScan.ScanLiveAdmission`.

**Kabul eşlemesi** (görüntüler `screens_unity_rev3/`, 1920 × 1080; 31 görüntü + `A6_R17_sayfa` 3 × 3):

| Ölçüt | Kanıt | Görüntü |
|---|---|---|
| R9a boş kapı | P2 (tezgâh katlı, lamba sönük, komut yok), S3 | `A6_R9a_bos` |
| R9a vuruş | `Rev3DoorTests` M1, P1 (yalnız canlı kapanışta, yüklemede yok; kamera dönmez; tezgâh K1'den iner) | `A6_R9a_vurus`, `_lamba` |
| R9b kapı açık, koridor boş | P8, P9 (KISITLI da açar), `Rev3DoorTests` M3, S3 | `A6_R9b_acik` |
| R17a aday sunulur | P3, `Rev3AdmissionMotionTests` (T1–T3) | `A6_R17a_aday` (+ `_EN`) |
| R17b dört inceleme, okuma | P4–P7, P17, `Rev3AdmissionFlowTests.Inspect_*` | `A6_R17b_karsilastir` (+ `_EN`), `_gorusme`, `_bugu`, `_okuma`, `_fikstur_rauf`, `_fikstur_okuma` |
| R17c dış kayıt bekliyor → sonuç | P7, P18, P19, P19b, `AdmissionSupervisorTests` C1–C5 | `A6_R17c_bekliyor`, `_sonuc` |
| R17d damgalar, YARDIM, kilitler | P8–P13 (P12a–e kilitler) | `A6_R17d_damga`, `_yardim` (+ `_EN`); kilit görüntüsü alınmadı |
| R17e talimat v2/v3 | P14, P14b, P14c, C8 (v2 Day 4, v3 Day 8) | `A6_R17e_v3_degisim`, `A6_R17e_v3` |
| R17f kişi dosyasında iz | P16, `Rev3BoardSnapshotTests` `Folder_*` (E14–E16). **Kısmen** (D17): içerik ve FileTag var, iki sayfalık açık dosya Aşama 7'de | `A6_R17f_iz` (+ `_EN`) |
| R17-EN | EN fikstürler, düz kâğıt yan yana | `A6_R17_EN_fikstur_rauf`, `A6_R17_kagit_{belge,serit_v2,serit_v3,fis}_{tr,en}_yan` |
| Core K4-A, biçim 15 | `AdmissionSupervisorTests`, `SaveStoreTests` (biçim 14 arşivi), P15 (kayıt komutları canlıyla birebir) | — |
| Eski yol bozulmaz | `DoorTests`, `SiginakDoorTests`, `LiberationCampaignTests` (yalnız sürüm bekçisi), `Rev3DoorTests.Legacy_Unchanged` | — |

**Ölçüler (Play ve editör, 1920 × 1080):**
- Punto taraması 2371 metin, **0 ihlal**, 155 gerekçeli istisna (Aşama 4: 1658 / 143); yeni istisnalar yalnız raf plakaları
  (`DecisionStampRack/`), damga sapları (`/TXT_Cap`) ve teneke şablonu (`/Drawer/Teneke `). K8R'de kâğıt türü başına en küçük metin
  (TR ve EN): belge SEVK No. etiketi 14,1 px, fiş künyesi 15,2, şerit eki 15,2, talep alt satırı 15,6, karbon KOPYA 15,6, ESKİ
  künyesi 16,3; belge adı ≥ 18 px (P17).
- Raf plakaları ve damga sapları TR/EN sekizi de yuvaya sığar (otomatik punto, tasarım puntosu en çok: iki satır cap 5,2 mm → em
  8,83 mm, tek satır 6,5 → 11,0, sap 3,6 → 6,11).
- Tezgâh spotu (şiddet 2, bölge parlaklığı, biz / referans): keçe orta 115 / 117, sol 104 / 110, raf 108 / 105.
- Süreler (PlayMode, token + iki kare): vuruş 0,9 s; K8 → K8A 0,45, K8G 0,45, K8R 0,25 (çapa kâğıttan 0,318 m); fiş 0,35 + 0,15;
  işaretçi 0,2; buğu 0,6 / 2,5; damga basışı 0,12, dönüş 0,25; çark 0,93 s'de başlar, 0,5 s, kanat 0,8, merdiven 0,3, bekleme 0,6,
  kapanış 0,6; şerit değişimi 0,25 + 0,3.
- Ölçütler: her araç tam bir `inspectApplicant`, her damga tam bir `decideApplicant`; poz, okuma, belgeyi tezgâha alma, ret ve geri
  zinciri komut yazmaz; kayıttan devamda komutlar ve `Rng.State` aynı, vuruş yok, belge, fişler ve GİDEN yaprağı yerinde, büyüteç dinlenmede.
- PLAN §5.3'teki Frame Debugger gölge sayımı ve K8A 300 kare başarım ölçümü bu turda yapılmadı; gölgeli ışık sayısını tarif testi
  (`Rev3RoomTests`: K8A/G/R tam 3) kilitler.

**Testler:** EditMode **945/945**, PlayMode **67/67** (`AdmissionPlayTests` 26 + 41). Yeni EditMode 134 koşu: `AdmissionSupervisorTests` 16, `Rev3Stage6ContractTests` 11, `Rev3DoorTests` 8, `Rev3AdmissionMotionTests` 25, `Rev3AdmissionPaperTests` 13, `Rev3AdmissionSnapshotTests` 23, `Rev3AdmissionFlowTests` 25, `Rev3AdmissionSceneTests` 10, `Rev3BoardSnapshotTests` +3.

### 10.7 Aşama 7 · harita, kâğıtlar ve CRT (2026-11-07; eski §13)

Ayrıntı [A7_DOGRULAMA.md](A7_DOGRULAMA.md). R6-TR/EN, R8a/b, R10a–c, R17f.

- `RaidPaperFacts`: gerçek üç saha kişisi; `OperationReportBuilder` altı kazanç, ilgili `raidres` öncesi kayıttan gerçek depo sınırı. `PersonDossierFace` iki sayfa + ek yaprak, FileTag kırpılmaz; **R17f tamamlandı.**
- `CrtInstruments` (son 0,6 s 1,4× ışıma; render texture yok). K5a +14 cm, 52°. Açılışta ampulden 0,15 s sonra masa lambası.
- `AdmissionStation.ReadPose` tezgâh kalkışını çıkarır (K8R 4 mm sapması); `A7_R17d_kilitli`, `A7_R17e_degisim_ani`.
- Ölçü: görev fişi ~950 px, kadro raporu 470 × ~674, K4 canlı en küçük yeni metin 14,21 px; punto 2431 / 0 / 150.
- Testler: EditMode **955/955**; PlayMode 71 senaryo (son tam turda 69/71; düzeltmeden sonra ilgili 26/26; öteki 45 aynı turda geçti); yeni R6/dosya/CRT 4/4. Biçim 15 değişmedi.
- Tek seferlik sahne kurucusu silindi. Kesintide test klasöründe oynanan ilerleme kullanıcı onayıyla normal kayda taşındı; yedek `UserSettings/CodexRecovery/`.

### 10.8 Aşama 8 · menü sahanlığı ve ayarlar dolabı (2026-11-07; eski §14)

Ayrıntı [A8_DOGRULAMA.md](A8_DOGRULAMA.md). R1–R4.

- Menü kabuğu, kapı dış donanımı, saat/kart/raf/deste, ana şalter ve iki dolap paket modelleriyle değişti. Sahne ve prefab kökleri korunarak referanslar bağlandı. Kaynak 198 model değiştirilmedi; baskı yüzlerinde UV'si düzeltilmiş ağ kopyaları kullanıldı.
- Kart dört ayrı satır ve 80 delikle gerçek kayıt özetini gösterir. Yeni harekâtın mevcut iki tık / 4 s onayı, iptal fişi ve devrilme hareketi modele bağlıdır. Saat tıkı da devam eder. Yeni kart ilk vardiyadır.
- Giriş R4 zamanlamasıyla, tüm girdiler ve imleç kilitli oynar. Yükleme kanat açılırken başlar, en az 0,2 s siyah ardından etkinleşir. Kapı açılırken en çok üç gölgeli ışık bulunur.
- Dolap sürgüleri, ibreleri, sayaçları, lambaları ve SES/DİL anahtarları mevcut ayar servisini kullanır. Oda menü kolunda açık belge/işlem kilidi, neden satırı, mühür bandı ve reddedilen tık hareketi vardır; dolabın açık olması kendi kolunu kilitlemez.
- M0 hedef y=1,25 / 60°; M1 x=3,32 / 48°; K9 yerel z=−1,10 / 48°. Plaka büyük harfi 7,2 mm; DİL plakası 112, menü plakası 120 mm. İptal fişi iki satırdır. Gölgesiz okuma dolgusu devrilen kartın kararmasını önler.
- Tek seferlik sahne/UV kurucuları ve QA oturum koruyucusu silindi. Kalıcı sahne kurucusu eklenmedi. `Rev3TextureBaker.BakeStage8()` baskıları yeniden üretebilir. Core, denge, RNG ve kayıt biçimi değişmedi (15).

Doğrulama: tam EditMode **964/964**, tam PlayMode **79/79**. Punto 2419 / 0 / 150; canlı menü metninde en küçük gerekli punto 14,30 px; üç sahnede eksik script yok; 2011 çeviride eksik yok.

### 10.9 Aşama 9 · baskın kitleri ve isimli saha (2026-11-08; eski §15)

Ayrıntı [A9_DOGRULAMA.md](A9_DOGRULAMA.md). R11, R12.

- 12 bölge + varsayılanın Relief/ReliefIso/Bridge/Rubble, sivil cihaz ve düşük giriş kapısı; BoardBase, Tile/WallBlock, 16 maskeli kanal; sekiz makine, T/E/A, dolaplar ve olay parçaları paket modellerine bağlandı. 53 birleşik prefab `Desk/Prefabs/Rev3Raid` altında; kaynak 198 modele dokunulmadı. BackupCore kullanılmaz; Madenci/Sis eski yolda durur.
- Piyonun Letter/Tilt/Tape/Tick/Plate parçaları; 180° kraft yüz, −70° düşüş, gerçek kişi adı/pip/plaka şeridi. Kaynak kolu ve kaidenin burnu birlikte döner. Kanallar tek gölgesiz renderer; kare tabanlı moloz 90° adımla kendi karesinde kalır.
- Kâğıt kenarı/ızgara/bant/gölge, asetatlar, antet/anahtar plan/damga/bina notu; gerçek CAN/HAR/GÜÇ/eşya; Balance'tan kasa dökümü ve kampanyanın gerçek vardiyası. Son kaçış turunda isimli kalıcı ölüm/eşya kaybı onayı, haklar bitse de zorunludur; Core kuralı değişmedi.
- Kabulde: EN adını gizleyen eski kısaltma, yetenek/Q taşması, alt kartların ekran dışına sarkması, etiketlerin mod değişiminde dar kalması, kanal lejantının yüksekliği ve çift çeviri çağrıları düzeltildi.
- Tam EditMode **969/969**, PlayMode **83/83**; son yerleşim sonrası baskın sözleşmeleri **5/5**. Genel punto **2438 / 0 ihlal**, canlı EN **min 14 px / 0 taşma**; üç sahnede eksik script ve boşalan referans **0**. 24 bina notu iki dilde sığar. Ölçek **132,58 px/birim** (zemin kenarı projeksiyonu 108,08 px); kapılar ≤0,29 m. R15 gereği ortak 2048 gölge atlası korundu.
- Ayrıntı ve R11a–c / EN / kaçış / 13 kit gri atlası: `A9_DOGRULAMA.md`. Geçici kurucular silindi, gerçek kayıtlar/tercihler ve başlangıç fontları korundu; Giris Edit Mode açık. Core/Balance/RNG/kayıt biçimi **15** değişmedi. Sonraki aşama **10**.

### 10.10 Aşama 10 · dil, okunurluk, performans, temizlik (2026-11-08)

Ayrıntı ve ham kanıt [A10_DOGRULAMA.md](A10_DOGRULAMA.md). Kararlar §4.7. Tek seferlik `Rev3Stage10Sounds`, `Rev3Stage10Lights`, `Rev3Stage10Fix`, `Rev3Stage10T1`, `Rev3Stage10Perf`, `A10WireHints` silindi. Core, Balance, RNG, kayıt biçimi (15) değişmedi.

- **Dil ve punto:** `Rev3CaptureSet` + `SiginakTextScan.ScanLiveAll`, 86 durum × 2 dil (B1 yalnız EN), 17.939 satır, 0 FAIL. Düzeltmeler: `DeskLoc.Ctx` (ADMIT/LIMIT/PENDING, SURE?), LAID UP, `TrNumber.WithAblative`, EVRAK → PAPERS, armour yazımı, kalıcı ölüm metinleri, CRATE satırı, Y ekseni yüzdesi, bölge kısaltmaları, 18 yerde çifte `Loc.T`.
- **Sunum:** BREATH GLASS plakası, bulgu fişi (A10-D13), kadro raporu kenar boşluğu, sayaç UV dikişi, DİL plakası (gövde 1,96×), dolap dolgu ışığı 0,35 → 0,04, iptal fişi, kart serbest etiketi, dişli rozet (A10-D14), telsiz K9'da gizli (A10-D17), M0 tehlike bandı, iş emri ipucu, dosya sekmesi alt satırları (A10-D18). G13 önerisi geri alındı (tasarlanmış davranış).
- **Performans:** hedef (A10-D8) sağlandı; ölçüm koşulu uyarısı §1. Gölge onarımı A10-D7: `Bulb`, `Desk lamp`, `BoardLamp` `m_Resolution` −1 → 2048 (sahne override); `CorridorLight` Point → Spot 110°; `CeilingBulb/BulbLight` gölgesiz; `Rev3RoomTests` kilidi. Q13: çift farkları gürültüde, yumuşak kalır (A10-D6).
- **T1 (A10-D1):** 675 + 485 = 1.160 GO; `CampaignWear` 8 ve View/Motion 96 referans beklendiği gibi boşaldı; 165 asset ve 2 klasör silindi; yedek `A10_silinenler.zip` (335 dosya, önceki `DeskGame.unity` dahil). 2 test silindi. `SiginakCaptureSet` emekli.
- **Sesler (A10-D5):** 42 yer tutucu; `SoundBank` bağlı; kulak kontrolü kullanıcıda.
- **Testler:** EditMode **988/988**, PlayMode **85/85** (`PersonCardDragPlayTests` 2 yeni). Validate 0. Punto 2321 / 0 / 149.

---

## 11. Aşama 0 bulguları

Yalnız bugün doğru ve yol gösteren olanlar. Bütün liste A9 kopyası §1.

4. **Eski yol oyuncuya kapalı.** `Balance.planningBoardEnabled` varsayılanı ve `BalanceConfig.asset` `true`; yol yalnız bu ayar elle kapatılınca açılır. Kapının eski yolu (`DoorCardView`, siluetler) bağlı ve testli (`DoorTests`, `SiginakDoorTests`; ikisi `LegacySimulation`). `LiberationCampaignTests`'in bütün testleri `LegacySimulation` kullanır; kartlı oyunun kayıt testleri `SaveStoreTests`, `PlanningBoardTests`, `CommunityAdmissionTests`'te. Kapalı raf, maket ve `SiginakShelter` nesnesi Aşama 10'da sahneden silindi (A10-D1); kod durur.
10. **Baskın kamerası 132,58 px/birim** (1 kare = 1 dünya birimi). Kaynak `SadeRaidCameraFraming` → `tokens_sade.json → raid.camera`; orto 4,0730 (16:9 ve üstü; 16:10'da ≈ 4,525), açılar 35°/45°. `raid_tokens.json → camera` (88,39) çalışma anında okunmaz. İnce parça kuralı 0,025 birim.
14. **Assembly sınırları.** `Corrigible.Desk` (Runtime) `Corrigible.Game`'i ve URP'yi referans almaz. Runtime bileşenleri doğrudan ses çalamaz: olay yayar, sesi Bridge çalar. URP ışık verisi Runtime'dan okunmaz.
16. **`ShiftSwitch` üç yerde ortak:** odadaki vardiya şalteri, dolabın MENÜYE DÖN kolu, menüdeki son şalter. Yeni davranış alanla açılır.

---

## CHANGELOG

- **2026-11-05 / 06 · Aşama 0 (keşif).**
  - `PROMPT.md`'nin tamamı ve paket belgeleri okundu; 198 manifest ve token 1.3 projeyle karşılaştırıldı.
  - Kod ve sahneler dört okuma ajanıyla tarandı.
  - `_AxisTest` geçici olarak içe aktarıldı, eksenler sayı ve görüntüyle doğrulandı, sonra silindi
    (`screens_unity_rev3/A0_axistest_*.png`).
  - Taslak harita altı ajanlı çürütme turundan geçti. Düzeltmeler:
    - LiberationCampaignTests eski yolda;
    - token'da 26 kaldırılan, 2 değişen düğüm;
    - `RoomView` roomBulb okumuyor;
    - FOV rig'de;
    - otomatik üretim süre tanımıyor ve iş emri yolu kapalı değil;
    - fiş gövdesi `BoardReceiptPreview`;
    - TAA kalitesi.
  - Ayrıca 20 eksik eklendi:
    - portreler, çarpıştırıcılar, "/60" dokusu;
    - `RaidBoard3D`, baskın ışığı ve dolaplar;
    - kısayol tablosu, etkileşim durumları, EVRAK içeriği, açılış;
    - yerelleştirme kaynağı, sunum durumu dosyaları, PlayMode;
    - aile kapsamı, kabuk yüzeyleri, menü komutları, Rev2 devri.
  - Kod değişmedi.
- **2026-11-06 · Aşama 1 (model hattı).**
  - Kullanıcı §6 kararlarını onayladı.
  - Token 1.3 ve eski değerlerin korunması; Big Shoulders Stencil fontu.
  - Model hattı ve 198 prefab; malzeme kitaplığı: Unlit asetatlar, gölgesiz lekeler, token'dan swap durumları.
  - Devir fişinin yüz onarımı.
  - Testler: `Rev3ModelTests` + token 1.3 testleri; `RaidRoomKitTests` ve `SiginakFoundationTests` 1.3'e göre
    güncellendi; 556/556.
  - Aile görüntüleri; dokuz ajanlı sayfa karşılaştırması (182 / 13 / 3).
  - Ayrıntı §7.
- **2026-11-06 · Aşama 2 (komuta odasının temeli).**
  - Odanın modelleri yerinde (masa, sayaç, duvar, şalter, teleks, EVRAK, CRT köşesi, kapı, koridor, pano); eski
    primitifler silindi, bileşen kökleri korundu, referans denetimi her grupta.
  - Poz ışığı tarifi ve bileşeni; ışık ve ortam R5a'ya kalibre. K4 panonun kamerası.
  - Sunum kodu: tambur, çetele çentikleri, teleks yuvaları, defter sayfası, fiş ağzı lambası, kenar satırı pozu.
  - Testler: `Rev3RoomTests` (6) ve yol değişikliklerine göre güncellenen sahne testleri; tam EditMode 562/562.
  - Beş gözden geçiricili denetim (26 doğrulanmış bulgu düzeltildi ya da açık kalana yazıldı); Play'de V.001 ve V.031.
  - Ayrıntı §8.
- **2026-11-06 · Aşama 3 (kartlı pano ve kartlar).**
  - Kullanıcı D1–D9'u onayladı (§6.3).
  - Anlık görüntü + 3B görünüm + sürükleme denetleyicisi; dört kart prefab'ı, durum katmanları, portre kitaplığı; deste,
    depo, iş emri fişleri, eğitim konuları, kişi dosyası, KAYIPLAR, hurda, başlık, kilit, MASAYA DÖN; K4 41°, poz süreleri.
  - Kabulde: ışıklı TMP malzemesinin camgöbeği dış çizgisi, pano lambasının siper gölgesi, EN etiket aralığı, İNCELE bayrağı ve
    mıknatıs, EN kısaltmalar.
  - Testler: tam EditMode 638/638, PlayMode 6/6; Play'de V.001, sürükleme durumları, deste, dosya, kilit, en dolu pano,
    başarım, kayıt/devam.
  - Ayrıntı §9.
- **2026-11-06 · Aşama 4 (vardiya kapanışı).**
  - Kullanıcı D1–D12'yi onayladı (§6.4).
  - Şalterin R19 kipi ve plaka; fiş yolu, el ve EVRAK hedefi; R10 düzeninde fiş yüzü ve saf veri kurucu; kapanış akışı (kararma, K1,
    sayaç, teleks, arşiv); K6 pozu ve ışığı; sahne bağları.
  - Kabulde: ışıksız fiş mürekkebi, tam karanlık karesi, K6 dolgusu, EN plaka, P1 ölçüsü.
  - Testler: EditMode 717/717, PlayMode 20/20; Play'de R19a/b/c TR/EN, kayıttan devam, arşiv okuma.
  - Ayrıntı §10.
- **2026-11-07 · Aşama 5 (tarif defteri ve üretim).**
  - Kullanıcı D1'de B'yi (tarifi oyuncu seçer; K5 değişti) ve D2–D15 önerilerini onayladı (§6.5).
  - 3B tarif defteri, tarif kartı kopyası, iş emri durumları ve fişler, yetersiz stok, kilitli plan, ürün kayışı ve YENİ; geçici defter
    tuvali kalktı.
  - Kabulde: kapak değeri, defter kipinde dosya tıkı, EN sekme, kart ve alt satır puntoları, sayfa renkleri, ► glifi.
  - Testler: EditMode 811/811, PlayMode 41/41; Play'de R18a–d TR/EN.
  - Ayrıntı §11.
- **2026-11-07 · Aşama 6 (kapı ve kabul).**
  - Kullanıcı D1–D26 önerilerini onayladı (§6.6).
  - Core K4-A: gözetmen Boşta'nın ilk uygun kişisi (`AdmissionWatcher`), salt okunur `AdmissionFindingCode`, H1/H3 düzeltmesi;
    kayıt biçimi 15 (biçim 14 kaydı `.v14.backup`'a arşivlenir).
  - Kapı (iç saatli vuruş, süreli açılış, kabule devir); kabul istasyonu, dört araç, dört damga, kâğıt yüzleri, FileTag, buğu dokusu;
    anlık görüntü kurucusu; akış (komutlar, K8A/K8G/K8R, geri zinciri, karar dizileri, teleks, sesler); sahne; eski uGUI pencere silindi.
  - Kabulde: kayıttan devamda sonuç fişi, karar sonrası tezgâh, buğu tepesi, koçan künyesi, tık-tık'ta büyüteç, GİDEN yaprağı, üç plaka
    puntosu, tezgâh spotunun yönü, kıskaç fişleri, BULGULAR rengi, büyüteç kuralı, otomatik punto (raf plakaları, saplar, teneke).
  - Testler: EditMode 945/945, PlayMode 67/67; punto taraması 0 ihlal (2371 metin); Play'de R9a/b, R17a–f TR/EN (R17f kısmen).
  - Ayrıntı §12.

- **2026-11-07 · Aşama 7 (harita, kâğıtlar, CRT).**
  - İsimli görev fişi ve kadro raporu; altı gerçek kazanç ve kişi bazında operasyon sonucu; R10 künye/çizgileri.
  - İki sayfalık kişi dosyası, uzun geçmiş için ek yaprak, kırpılmayan FileTag; R17f tamamlandı.
  - CRT model parçaları/ışıma/kapanma, K5a +14 cm / 52°, açılış masa lambası; K8R hover sapması düzeltildi.
  - EditMode 955/955; PlayMode 71 senaryo (45 tam turda, düzeltme sonrası kabul paketi 26/26), yeni R6/dosya/CRT testleri 4/4; ayrıntı §13 ve `A7_DOGRULAMA.md`.

- **2026-11-07 · Aşama 8 (menü sahanlığı ve dolap).**
  - R1–R4 modelleri, kart/zımba/onay, saat ve kapı giriş dizisi, iki dolap ve kilitli menü kolu; UV baskı düzeltmeleri ve TR/EN okunurluk.
  - EditMode 964/964, PlayMode 79/79. Menüde gerekli metin ≥14,30 px; girişte en çok 3 gölgeli ışık. Kaynak modeller ve kayıt biçimi 15 korundu.
  - Kanıtlar §14 ve `A8_DOGRULAMA.md`; sahne kurucuları silindi, gerçek kayıtlar ve tercihler aynı, Giris Edit Mode açık.

- **2026-11-08 · Aşama 9 (baskın kitleri).**
  - R11/R12 modelleri, isimli piyonlar ve gerçek kart bilgileri, ozalit paneller/antet, kasa içeriği ve isimli son tur onayı.
  - EditMode 969/969, PlayMode 83/83; son antet denetimi 5/5. 2438 metin / 0 ihlal; gerçek kayıtlar ve tercihler aynı.
  - Kanıtlar §15 ve `A9_DOGRULAMA.md`; kurucular silindi, kayıt biçimi 15, Giris Edit Mode açık.

- **2026-11-08 · Aşama 10 (dil, okunurluk, performans, temizlik).**
  - Kullanıcı A10-D1..D12'yi ve sonradan D-A..D-F'yi (A10-D13..D18) cevapladı (§4.7).
  - 86 durum TR/EN canlı tarama (`A10_dil_olcumleri.json`, 17.939 satır, 0 FAIL), `Rev3CaptureSet`, `ScanLiveAll`, EN kurucu testi; dil, punto ve sunum düzeltmeleri.
  - Gölge çözünürlüğü onarımı (A10-D7), koridor lambası Spot; kare süresi, atlas, Q13, TAA ve post ölçümleri; hedef sağlandı (dev Mac, Editor; çözünürlük uyarısı A10_DOGRULAMA'da).
  - T1 kaldırma: 1.160 GO, 165 asset, `SiginakCaptureSet`; yedek `A10_silinenler.zip`. 42 yer tutucu ses.
  - EditMode 988/988, PlayMode 85/85; punto 2321 / 0 / 149; kayıt biçimi 15, Core değişmedi. Harita sadeleşti, tam metin `IMPLEMENTATION_MAP_A9.md`.
