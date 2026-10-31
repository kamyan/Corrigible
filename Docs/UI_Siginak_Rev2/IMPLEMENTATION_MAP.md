# Sığınak · Revizyon 2 — Uygulama haritası

**Durum:** Aşama 0, 1 (token, sprite, font), 2 (başlangıç pozu), 3 (harita ve görev fişi), 4 (sığınak), 5 (kapı), 6
(vardiya raporu ve karşı saldırı), 7 (baskın), 8 (sonuç kartı ve operasyon raporu), 9 (son rapor) ve 10 (dil, erişilebilirlik,
temizlik) bitti; 1.2 eki (eksik işaretler, üç makine, dört niyet) Aşama 7'de bağlandı. §8'deki açık sorular kaldı. Tam görüntü
seti `screens_unity_rev2/` (TR, `_EN`, `_gray`), punto taraması `punto_taramasi.txt`.
**Taban:** `siginak-ui` · `9d16037 Radio added.` + commit edilmemiş değişiklikler (telsiz vurgusunun geri alınması, robot
itme çubuğunun kaldırılması, `screens_v2/`, `BRIEF_v2.md`). Revizyon 2 bu çalışma ağacının üstüne gelir.
**Okunanlar:** `PROMPT.md`, `reference/notes/*.md` (S1–S9, Kararlar), `reference/scene_threejs_rev2/README.md` ve `door.js`
(ziyaretçi türleri), `sprites/README.md`, iki `spec/*.json` (projedekilerle yol yol karşılaştırıldı), ilk turun
`Docs/UI_Siginak/IMPLEMENTATION_MAP.md`'si ve CHANGELOG'u. `DeskGame.unity` Unity CLI ile salt okunur tarandı;
`RaidHud`, `RaidPanels`, `RaidTeamCard`, `UnitCard` prefab'larının nesne adları okundu. Core, Bridge ve Siginak kodu tarandı.

---

## 1. Önemli bulgular (Aşama 0'daki durum)

1. **İlk turun anahtarı da kurucusu da yok.** `SiginakPresentation` anahtarı ve `SiginakRoomBuilder` ilk turun Aşama 10
   sonunda (`eed2824`'ten sonra) silindi. Sahne asıl kaynak; AGENTS.md kural 5 kalıcı kurucuyu bilerek yasaklıyor
   ("tek seferlik editör script'i yaz, çalıştır, sil"). PROMPT'un "anahtara bir seviye ekle" ve "builder'ı genişlet"
   adımlarının projede karşılığı yok (bkz. Soru 1).
2. **Revizyon 2'nin "eklenen parçalar"ının çoğu zaten ilk turun bileşenleriyle yapılmış.** Kapı (`SiginakDoor`,
   `DoorProp`, `DoorCardView`), alarm panosu (`AlarmPanel`), GÜVEN modülü (`CounterBank.Slot.Trust`), vardiya raporu
   (`ShiftReportBuilder/View`), eğitim notu (`SiginakTutorial`, `TutorialNoteView`), son rapor (`GameOverView`,
   `StripChartView`, `TimelineCardView`) projede var. İş çoğunlukla **yeniden giydirme**; yeni bileşen az.
3. **Harekât geçmişi kayıtta var; `campaign_history.json` gerekmiyor.** `WorldState.Hist` her vardiya sonunda işgal
   oranını (`HistPoint{Day, Real}`), `WorldState.Moments` kırılma anlarını (geri alınan sektör, karşı saldırı) tutuyor.
   Kayıt komut geçmişi olduğu ve devam ederken yeniden oynatıldığı için eski kayıtlarda da tam geçmiş oluşur. Bugünkü
   son rapor grafiği zaten `S.Hist`'ten çiziliyor. Eksik olan yalnız vardiya başına **GÜVEN** (S8 önerisi; bkz. Soru 8).
4. **Oyunun içeriği tasarımın çizdiğinden geniş.**
   - **Bölge özelliği:** 12 tane (`Baskin.RegionTraits`). Pakette yalnız 2'sinin işareti var: `glyph_corridor` = dar,
     `glyph_sand` = sis (Kum fırtınası). Eksik 10: güvenli, kısa, takviye, açık, soğutma, kalabalık, zırhlı, şebeke,
     fabrika, devriye.
   - **Makine türü:** 8 tane (`Baskin.UnitTypes`). Pakette 5'inin işareti ve 3D biçimi var: dron, bakım, kilit, havan,
     kalkan. Eksik 3: zırhlı dron (`zdron`; Batı Avrupa ve Okyanusya imzası, yani **ilk cephenin** makinesi), patlayan
     robot (`bomba`), çoğaltıcı (`cogalt`).
   - **Niyet türü:** 8 tane (`shoot`, `melee`, `repair`, `lock`, `lob`, `blast`, `shield`, `clone`). Tasarım 4'ünü
     çiziyor (ateş, yakın dövüş, kilit, onarım). Havan atışı (`lob`), patlama, kalkan ve kopyalama tasarımsız.
   - Bkz. Soru 4.
5. **Ziyaretçi türü alanı yok.** `DoorCard`'da yalnız `Id` var. 23 kartın 2'si kimsesiz (paket, zarf); 4'ü grup (heyet,
   çocuklar, aile, hırsız). Kart → siluet eşleme tablosu §4'te (bkz. Soru 5).
6. **Baskın kamerası referansla uyumlu.** Oyun `tokens_sade.json → raid.camera`'yı okuyor (`SadeRaidCameraFraming`):
   132,58 px/birim, orto 4,073, 35°/45°, merkez (960, 528). Rev2 görüntüleri bununla çekilmiş. `raid_tokens.json →
   camera` (88,39 px/birim) eski ve hiçbir kod onu okumuyor; PROMPT'un dediği gibi dokunulmaz.
7. **`raid_tokens.json` 1.1 doğrudan konabilir; `tokens_siginak.json` 1.1 konamaz.**
   - Baskın dosyası projedekinin üst kümesi: silinen yol yok, 21 değer değişmiş (`_changed_1_1`'deki 11 grup). Okuyan
     tek kod `SadeThemeSetup.ImportRaid` → `RaidTheme.asset`.
   - Sığınak dosyası, projede ilk tur boyunca eklenen **249 uygulama yolunu** ("UYGULAMA EKİ": `colors.ui.report*`,
     `colors.mapLiberation.*`, `space.stringPx`, `motion.candidate*` …) içermiyor. Dosyayı değiştirmek bu değerleri siler.
     Doğru yol 1.1'i projedeki dosyaya **birleştirmek**.
   - Listede olmayan iki fark var: `type.taskHead` 14 → 13 (14'e ilk turun punto taraması çıkarmıştı; 13 px "ikincil ≥ 14"
     kuralını bozar) ve `cameras.K6a.pos` z 0,36 → −0,36. İkisinde de projedeki değer korunur (bkz. Kararlar §6).
8. **İşaret glifleri eksik.** `Desk/Fonts/charset.txt`'te `✕ ▶ ◆ ● ■` yok (Türkçe glifler, `✓ ✗ − ·` var). Ya charset'e
   eklenip atlaslar yeniden kurulur ya da bu işaretler sprite'la çizilir. Ölçek ve evre işaretleri zaten sprite; geriye
   metin içindeki `✕` ve `●` kalıyor.
9. **Alarm ondalıklı.** `S.Alarm` double (telsiz görevlisi −0,05, kopya ziyaretçi +0,6). Pano bugün `Math.Floor`
   gösteriyor; 10 adımlık ölçek de aşağı yuvarlanır.
10. **Sığınağın depo sınırı 30'da kalmıyor.** Erzak sınırı = yaşam alanı seviyesi × 30 (SV3'te 90). Malzeme sınırı 60,
    plaka sınırı = atölye seviyesi × 1 (en çok 3). "1 sandık = 3 erzak" yalnız SV1'de 10 sandığa sığıyor (bkz. Soru 6).
11. **4 tuşu boş.** Masada 1, 3 (harita/görevler), 2 (sığınak), H, Home bağlı. Kapı bugün **K** tuşuyla açılıyor
    (`SiginakDoor`). Baskında 1–3 uzman seçiyor; 4 boş. Kapı kâğıdı açıkken rakamlar seçenekleri seçiyor (çakışma yok:
    kâğıt açıkken 4 dördüncü seçenektir).
12. **Yetenek düğmesinin anlamı tasarımın adından geniş.** `RaidDataSource:216`: Teknisyen'in Sabotaj'ı robota 1 hasar
    verir **ve 1 kare iter**; Analist'in Kanca'sı da "−1 robot" yazıyor (1 hasar + çeker). "Robota vur · −1 can" itmeyi
    söylemiyor (bkz. Soru 7).

---

## 2. Klasör ve assembly yerleşimi

PROMPT'un `Assets/Corrigible/Presentation/Siginak/` yolu yerine ilk turda kurulan düzen sürer (ilk turun Karar 1'i).
O yolda assembly yok; oraya konan script'ler `Corrigible.Desk`/`Bridge`'den görünmez.

| İçerik | Yer |
|---|---|
| Görünümler | `Desk/Scripts/Runtime/Siginak/` (`Corrigible.Desk`) |
| Oyun bağlantısı ve görünüm durumu kurucuları | `Desk/Scripts/Bridge/Siginak/` (`Corrigible.Desk.Bridge`) |
| Baskın paneli görünümleri | `Desk/Scripts/Runtime/Raid/`; 3D etiket ve oklar `Game/Raid/` |
| Editör araçları, tek seferlik sahne script'leri | `Desk/Scripts/Editor/Siginak/` (tek seferlikler iş bitince silinir) |
| Yeni sprite'lar | `Desk/Art/Siginak/Sprites/Rev2/` |
| Token dosyaları | `Desk/Data/Siginak/tokens_siginak.json`, `Desk/Data/Sade/raid_tokens.json` |
| Testler | `Desk/Tests/EditMode/Siginak*Tests.cs` |
| Görüntüler | `Docs/UI_Siginak_Rev2/screens_unity_rev2/` |

---

## 3. Eşleme tablosu

**Yapılacak:** KULLAN = olduğu gibi · DEĞİŞTİR = görünüm değişir, mantık aynı · YENİ = yeni bileşen/nesne.
**Mevcut karşılık** sütunu Aşama 0'daki hâli anlatır (`9d16037` + çalışma ağacı); uygulanan hâl ve silinenler CHANGELOG'da.
Kısaltmalar: `S` = `Sim.S`, `Sh` = `Sim.Shelter`, `B` = `Sim.B`, `Lib` = `S.Liberation`.

### 3.1 Altyapı

| Revizyon 2 öğesi | Mevcut karşılık | Yapılacak | Veri kaynağı |
|---|---|---|---|
| Rev1/Rev2 anahtarı | yok (ilk turda silindi) | YAPILMAZ (karar: git yeterli, §8 Soru 1) | — |
| Token 1.1 (sığınak) | `SiginakTheme` + `SiginakThemeSetup`, `Desk/Data/Siginak/tokens_siginak.json` (1.0 + 249 uygulama yolu) | DEĞİŞTİR: 1.1 projedeki dosyaya birleştirilir, eskisi `tokens_siginak.v1_0.json`. Importer `_new_1_1`, `_changed_1_1`, `_was_1_1`'i okur | `spec/tokens_siginak.json` |
| Token 1.1 → 1.2 (baskın) | `RaidTheme` + `SadeThemeSetup.ImportRaid`, `Desk/Data/Sade/raid_tokens.json` (1.0) | DEĞİŞTİR: dosya yerine konur (1.2 eki: yalnız ekleme; yedek yazılmaz, git yeterli); importer yeni grupları tipler (`panel`, `turnStrip`, `phaseChip`, `machineTypes`, `radioSlip`, `abilityCard`, `resultCard`, `trainingNote`) | `spec/raid_tokens.json` |
| Sprite kiti | `SiginakSpriteManifest` + `DeskSpriteImporter` | DEĞİŞTİR: manifeste 46 Rev2 sprite'ı (dilim, Tiled/Sliced, PPU 100) + 1.2'nin 28'i; bütün `glyph_*` mipmap'li, Trilinear; işaretler `DeskSprites` kaydında (adla seçilir) | `sprites/README.md` |
| `tx_counter_digits_tenths` | `CounterModule` kırmızı onda bir malzemesi (`tx_counter_digits_red`) | DEĞİŞTİR: yeni doku ve malzeme | — |
| Glifler `✕ ▶ ◆ ● ■` | `charset.txt` (yok) | DEĞİŞTİR: charset'e ekle, `DeskFontSetup` ile atlaslar yeniden | — |
| Yerelleştirme | `Loc`, `DeskLoc`, `DeskLocText`, `strings.txt`, `DeskFormat` | KULLAN. §9 sözlüğü `strings.txt`'e | — |
| Ses | `Sfx.Play(Cue)`, `SoundBank` | DEĞİŞTİR (yalnız ekleme): yeni `Cue` değerleri; klip yoksa sessiz | — |
| Üzerinde etiketi | `HoverTag` + `DeskTarget` (Title/LineOverride) | KULLAN; yeni hedeflere `DeskTarget` | — |

### 3.2 S1 · Başlangıç pozu

| Revizyon 2 öğesi | Mevcut karşılık | Yapılacak | Veri kaynağı |
|---|---|---|---|
| Sayaç rafı 6 × 0,247 m | `Desk/Counter bank` (`CounterBank`, 6 `CounterModule`, merkezler 0,26 m, plakalar %85) | DEĞİŞTİR: modül aralığı ve plaka ölçüleri (sahne, tek seferlik script) | `colors.counter.*` |
| FON virgülü + açık onda bir çarkı | `CounterModule` kırmızı onda bir çarkı, virgül yok | DEĞİŞTİR: `ui_counter_comma` Quad (EN'de kuyruksuz nokta), çarklar arası 15 mm | `S.Fon` |
| GÜVEN x/20 | `CounterBank.Slot.Trust` (var) | KULLAN; plaka ölçüsü öteki modüllerle eşitlenir | `S.Trust` / `Simulation.TrustMax` |
| ± fişi 72 × 28 | `DeltaSlip` (84 × 36) | DEĞİŞTİR (ölçü token'dan) | önceki çizim ile fark |
| Anahtar rayı 6 tuş | `Desk/Key rail` (`StationKeyRail`, 5 `StationKey`: 1, 2, 3, H, Home) | DEĞİŞTİR: 4 · KAPI tuşu eklenir, aralık 0,226, plaka 0,128 (Home 0,10) | etkin = rig pozu |
| KAPI lambası + el yazısı fiş | `DoorProp.SetKnocking` (kapının kendi lambası) | YENİ `DoorKeyIndicator` (mücevher emissive #FFB347 3,4, 1 Hz; fiş 0,2 s sallanır) | `Sim.DoorVisitor != null && !Open`, `SiginakDoor` kâğıdı kapalı |
| 4 kısayolu | yok (kapı **K**) | YENİ: `SiginakDesk` girdi okuyucusuna 4 → kapı pozu + kâğıdı yeniden aç (`SiginakDoor.Reopen` ile aynı yol). Baskın sürerken devre dışı, etiket nedeni yazar | `surface.BlockedExceptDoor` |
| Kenar satırı | `EdgeLine` (VARDİYA · FON · ERZAK · MALZEME · ÖZGÜR · GÜVEN · ALARM + tuş fişleri) | DEĞİŞTİR: 4 KAPI fişi; EN'de tek satır (gerekirse aralık 14 px) | aynı |
| YZ ALARMI panosu | `Shell/Alarm panel` (`AlarmPanel`: iki çarklı sayaç "/10" + kırmızı ışık; eşiğe 3 kala yanar) | DEĞİŞTİR → 0,48 × 0,29 pano; sayaç (n/10) ve lamba kalır, altına 10 hücreli ölçek (1–7 krem, 8–10 kırmızı) ve “10'DA KARŞI SALDIRI”; lamba ≥ 8; karşı saldırıdan önce üç kez yanar | `⌊S.Alarm⌋`, `B.liberationAlarmThreshold` |
| KURTARILAN BÖLGELER | `Shell/Cork board` (`Freed regions` TMP listesi `SiginakDesk.freedEntries`; `Missing A/B/C` ilanları) | DEĞİŞTİR → `FreedRegionsBoard`: başlık kartı x/12, 12 kart yuvası, ÖZGÜR damgası, kurşun kalem "sıradaki hedef". KAYIP ilanlarının yeri Soru 9 | `Lib.Regions` (`Completed`, son sektörün `CapturedDay`), `Sim.LiberationFrontier` |
| Tebeşir vardiya çentiği | yok | YENİ decal (`decal_tally5`, beşli) | `S.Day` |
| Teleks ön kâğıdı | `Desk/Teleprinter` (`Teleprinter`, `TelexFeed`; `Paper flat` + `Paper text`) | DEĞİŞTİR: 0,26 × 0,19 asılı ön kâğıt, krom klips; yeni mesajda eskisi koparılıp yığına | `TelexFeed.Post` |
| CRT boşta dört satır | `CRT desk/CRT` + `CrtViewBuilder` (yedi satır: TELSİZ · KANAL 07, DİNLEMEDE …) | DEĞİŞTİR: DİNLEMEDE (ters video) · ALFA · HAZIR · cephe/sektör · YZ ALARMI n/10 | mevcut `CrtViewBuilder` girdileri |
| Telsiz yüzü, kayıt defteri | `CRT desk/Radio`, `Radio log` (DeskLocText "TELSİZ KAYDI") | DEĞİŞTİR (yalnız görünüş): kanal skalası K-07, SİNYAL göstergesi, SES/AYAR düğmeleri; defterde son dört kayıt, Kalam 40 doku px. Vurgu artmaz (`BRIEF_v2` kısıtı) | `CrtViewBuilder` |
| Raf oda kartları iki satır | `RoomView` kartları (ad 29 / satır 21 doku px) | DEĞİŞTİR: ad 84 / satır 68 doku px, "SV n · kişi işareti a/b" | `Sh.Rooms[]` |
| Harekât izleri | `Hope marks` (`HopeMarks`: Child drawing, Second mug, Daylight; özgür bölge sayısına bağlı, `SiginakSettings.hopeMarks`), `Shelf/Hope sprout` + `Jerrycan` | YENİ `CampaignWear` (vardiyaya bağlı) + `HopeMarks` korunur (bkz. Karar 4) | `S.Day`, `S.RaidLog.Count`, rapor sayısı |
| EVRAK yüksekliği | `Paper tray` (`EvrakTray`, son 5 rapor) | DEĞİŞTİR: yığın kalınlığı ⌊vardiya ÷ 3⌋ (en çok 10) | `S.Day` |
| Eğitim notu (masa) | `SiginakTutorial/Note` (`TutorialNoteView`, 340 × 258, "ANALİST NOTU · n/12") | DEĞİŞTİR: 338 px, −1,5°, #F2E9CC, "ANALİST · NOT" + "ADIM n/12", Kalam 22 | `TutorialFlow.Steps` (12 adım) |

### 3.3 S2 · Harita ve görev fişi

| Revizyon 2 öğesi | Mevcut karşılık | Yapılacak | Veri kaynağı |
|---|---|---|---|
| Etiket yerleri, iki satırlı ad, JAPONYA-KORE denizde | `SadeRegionLabelView`, `LiberationRegionMarks`, `map_regions.json → Label` | DEĞİŞTİR: yerler `map2.js → LAB` (harita dokusu px), ad 40–48 doku px | `GameData.Region(id).Name` |
| ÖZGÜR damgası yerleri | `LiberationRegionMarks` (damga etiketin yanında) | DEĞİŞTİR: `map2.js → STAMP` | son sektörün `CapturedDay` |
| Cephe bloğu (ad + sektör şeridi + yüzde + saha işaretleri) | `LiberationMapLayer` + `LiberationRegionMarks` | DEĞİŞTİR | `Lib.CurrentRegion`, `Sectors[i]`, bölge `Trait`, imza `Enemy` |
| Sektör işareti 38 px, `mark_sector_reocc` | `mark_sector_*` (ilk tur) | DEĞİŞTİR + YENİ yeniden işgal işareti | `S.LastCounterattack` |
| İşaretler kutusu sağ altta + TEHDİT n/4 | `MapLegend` | DEĞİŞTİR: yer, `threat_tier_n` | `Sim.Tier` |
| Görev fişi solda (Amerika seçiliyse sağda) | `OperationSlip` (`SiginakMap`, World Space, cepheyi örtmeyen yarıya yerleşir) | DEĞİŞTİR: yerleşim kuralı Karar 1 B; −0,6°, 432 px | seçili bölge `na`/`sa` → sağ |
| SAHA satırları (BÖLGE · İMZA · TEHDİT) | fişte düz metin satırı (2026-10-27) | DEĞİŞTİR: işaret 30 px + etiket 92 px sütun + değer | `Baskin.RegionTraits[trait]`, `Baskin.UnitTypes[enemy].Name`, `Sim.Tier` |
| 138 px damga sütunu, seçili satır ok ucu | fiş damgaları (sütunsuz) | DEĞİŞTİR | `Captured`, `NextLiberationSector`, `LiberationRaidCheck` |
| Ödül 2 × 2, bedel mürekkep | ödül tek satır, bedel kırmızı | DEĞİŞTİR | `B.liberationRaidCost`, `liberationLoot*`, `liberationServer*`, `liberationCaptureFunds` |
| Ekip diskleri T/E/A + can | kadro satırı | DEĞİŞTİR | `S.Teams[0]`, `Sh.Reserves`, yara/`Down` |
| BASKIN EMRİ VER beş durum; fon yetmezken açık + FON iki kez titrer | düğme devre dışı + gerekçe | DEĞİŞTİR: yetersiz fonda düğme açık kalır, basınca komut çağrılmaz, gerekçe teleksten ve FON titrer | `LiberationRaidCheck` (yalnız fon eksikse yetersiz kaynak durumu) |
| Yay biçimli iplik | `UILineGraphic` (Bézier) | DEĞİŞTİR: sarkma 30–40 px, adların altından | — |

### 3.4 S3 · Sığınak

| Revizyon 2 öğesi | Mevcut karşılık | Yapılacak | Veri kaynağı |
|---|---|---|---|
| Pano yeri (raf yereli −0,46 · 0,86 · −0,02) + ayak + BOŞTA tepsisi | `Shelf/Clipboard`, `Clipboard ledge`, `Idle tray` (`IdleTray`) | DEĞİŞTİR (sahne, tek seferlik script) | — |
| ERZAK YETER · n VARDİYA | `ShelterClipboard` özet satırları (TOPLULUK · UZMANLAR · ERZAK · PLAKA) | DEĞİŞTİR: başlık; n = ⌊`Sh.Supplies` ÷ \|`Sh.LastSuppliesChange`\|⌋, fark ≥ 0 → ERZAK ARTIYOR, n ≤ 3 kırmızı + AZ KALDI. Saf hesap `ShelterViewBuilder`'a (EditMode testli) | `Sh.Supplies`, `Sh.LastSuppliesChange` |
| Personel adımlayıcısı [−] a/b [+] | `ShelterClipboard` "− PERS." "+ PERS." | DEĞİŞTİR | `Workers`, `WorkerCapacity`, `AvailableWorkers`, `ShelterAssignCheck` |
| EN ÜST SEVİYE damgası | düğme metni | DEĞİŞTİR | `Level == B.shelterMaxRoomLevel` |
| GELİŞTİR yetersiz kaynakta açık | düğme devre dışı + gerekçe | DEĞİŞTİR: açık kalır, eksik kalem üstü çizik, basınca teleks YAPILAMADI | `ShelterUpgradeCheck`, `UpgradeCost` |
| Kalam öneri notu | sarı not | DEĞİŞTİR (Kalam 20) | mevcut not kuralları |
| DEPO bandı | yok (maketin üst bandı boş) | YENİ `ShelterStore` (sandık, boş yer gölgesi, kova, plaka rafı, tebeşir tahtası) | `Sh.Supplies`/`SuppliesCapacity`, `Sh.Materials`/`B.shelterMaterialsStorage`, `Sh.Plates`/`PlateCapacity` (bkz. Soru 6) |
| Oda çıktıları (ranzada yedek, telsiz başında görevli, döküm kalıbı, revirde hasta) | `RoomView` (piyonlar, durum damgaları) | DEĞİŞTİR (ekleme) | `Sh.Reserves`, `Workers`, `Sh.PlateProgress`, yatan uzmanlar (`Down > 0`) |
| Maket kartı iki satır (24 / 19 px) | `RoomView` kartı | DEĞİŞTİR | aynı |
| TELSİZ ODASI ETKİN (öneri) | yok | YENİ (pirinç fiş alarm panosunun altında + odada amber lamba) | `ShelterLogic.RadioWorkers(Sh) > 0` |

### 3.5 S4 · Kapı

| Revizyon 2 öğesi | Mevcut karşılık | Yapılacak | Veri kaynağı |
|---|---|---|---|
| Kapı dekoru | `SiginakRoom/Bunker door` (kasa, menteşe, kanat, kırmızı çark, gözetleme deliği + kayan kapak, "07", lamba + ışık, interkom; `DoorProp`) | DEĞİŞTİR: tehlike bandı ölçüsü, vuruş lambası kafesli cam, interkom ışığı, kapı yanında tebeşir ziyaretçi çentiği | `S.Door.Log.Count` |
| Aydınlık merdiven | `SiginakRoom/Corridor` (kısa koridor, tek soğuk ışık) | DEĞİŞTİR: merdiven boşluğu, iki nokta ışık, fayans | — |
| Altı siluet | `Corridor/Silhouette` (tek siluet) | YENİ `VisitorSilhouette` (tür + boy ±%4, aksesuar tarafı); yalnız kapı açıkken | kart → tür tablosu §4 |
| Kapı kâğıdı blokları | `SiginakHud/Door card` (`DoorCardView`: başlık, gövde, 6 seçenek yuvası, damga, kısayol satırı) + `DoorViewBuilder` | DEĞİŞTİR: gözlem bloğu (göz işareti + GÖZETLEME DELİĞİ), BAKILDI, sağda etki/neden sütunu, seçenek satırı 50 px, Overlay sol 40 / üst 64 / 470 px | `Sim.DoorOptions()`, `DoorOption.Need`, `DoorCard.Peek/Ask` |
| Ölümcül varyant (VURULDU / SIZMA) | `DoorCardView` kırmızı damga + "Son rapor" düğmesi | DEĞİŞTİR: çift çizgili damga 66 px, −8°; oda kırmızı ışık, lamba kırmızı; yalnız Enter / SON RAPORA GEÇ | `DoorVisit.Ending` (`vuruldu`, `sizma`) |
| Kenar satırının tuşları soluk | `EdgeLine` | DEĞİŞTİR | `S.Ending != null` |

### 3.6 S5 · Vardiya raporu ve karşı saldırı

| Revizyon 2 öğesi | Mevcut karşılık | Yapılacak | Veri kaynağı |
|---|---|---|---|
| Rapor kâğıdı (Overlay sol 24, üst 274, 540 px) | `SiginakHud/Shift report` (`ShiftReportView`, 600 px; Header, Stamp, Funds, Production, Team, Failed, Action) | DEĞİŞTİR | `ShiftReportBuilder` |
| Üç göz: ERZAK · YZ ALARMI · FON | yok (eşit satırlar) | YENİ blok; erzak ve alarm satırları dökümden çıkar | snapshot farkları (`ShiftReportBuilder`), `n vardiya` S3 hesabıyla aynı |
| Satır içi açıklama 15 px, eksi mürekkep, + yeşil, ±0 gri | kırmızı eksiler | DEĞİŞTİR: kural `ShiftReportBuilder`'da (EditMode testli) | — |
| Karşı saldırı kutusu + haritada ✕ + YENİDEN İŞGAL + teleks "YZ KARŞI SALDIRISI" + pano kartının düşmesi | rapor "EKİP VE YZ ALARMI" bölümünde KARŞI SALDIRI satırı, teleks `TelexKind.Alarm` | DEĞİŞTİR + YENİ (harita işareti, pano kartı) | `S.LastCounterattack`, `S.LastCounterattackDay`, `SimEventType.Counterattack` |
| Alarm lambası raporsuz önce üç kez yanar | `AlarmPanel` | DEĞİŞTİR (dizide) | aynı |

### 3.7 S6 · Baskın

| Revizyon 2 öğesi | Mevcut karşılık | Yapılacak | Veri kaynağı |
|---|---|---|---|
| Kenar panelleri: ozalit + kontrplak + bant | `RaidHud.prefab` (`RaidHudView`), `RaidPanels.prefab` (`RaidPanelsView`) — Sade kâğıt kartlar | YAPILDI (ayrı bileşen yok): `RaidHud.prefab › Corners` köşe grupları; kontrplak şerit, ozalit pafta ve bant prefab nesneleri | `raid_tokens.json → panel, layout1080` |
| Tur şeridi 34 px + SON 2 TUR + SON TUR | `RaidHud › TurnPlaque` (Turn1–8, 26 px, `LastTurn`) | DEĞİŞTİR | `RaidViewModel` tur alanları (`MaxTurns`, tur) |
| Evre çipi | `RaidHud › Phase` (`PhaseText`) | DEĞİŞTİR (daire/baklava/kare işaret) | evre |
| Görev çipi üç satır | `RaidHud › Objective` (`AiAbility`, `TargetThumb0/1`, `TargetName0/1`, `Can 0–7`, `Civilian0–2`) | DEĞİŞTİR | `RaidViewModel` (görev, özellik, hedefler, GÜVEN, sivil) |
| Birim kartları 237 × 84 (alt sıra) | `RaidTeamCard.prefab` (`RaidTeamCardView`, 376 × 140, sol sütun; prefab Aşama 7'de silindi) | YAPILDI: üç kart `RaidHud.prefab › Corners › Team` içinde | `layout1080.teamCards` |
| Yetenek kartı + alt düğmeler | `UnitCard.prefab` (`UnitCardView`, `RaidAbilityChipView`; prefab Aşama 7'de silindi) | YAPILDI: `RaidHud.prefab › Corners › Ability card`; adlar S6'nın (Soru 7 ara cevabı) | `RaidDataSource:216` efektleri |
| TELSİZ fişi | `RaidPanels › EventSlip` | DEĞİŞTİR (`radioSlip`); okunmamış sayısı TELSİZ düğmesinde (`Unread` var) | olay listesi |
| Makine evresinde kilitli kâğıt | `RaidHud › EndTurn` + `CommandStatus` | DEĞİŞTİR | `src.Busy`, hamle sayısı |
| Can etiketi (tür işareti + 11 px pip) | `RaidWorldLabel` / `RaidScreenLabel` (7 px pip) | DEĞİŞTİR | `Baskin` birim canı, türü |
| Uzman etiketi altlık önünde | piyon üstü kutular | DEĞİŞTİR (`labels.pawnTag`) | can, plaka, yara |
| Sıra bayrağı 30 × 28 / 20 px | `orderLabels` (`RaidBoard3D`, 14 px) | DEĞİŞTİR | `Baskin.Intents(B)` |
| Niyet okları + yürütme durumları | `RaidArrow` (UI/Default tabanlı mesh, yağlı kalem), `activeIntentHighlight`, `SetIntentPresentation` | DEĞİŞTİR: kalınlık, kilit/onarım rozetleri (`badge_clamp`, `badge_wrench`), biten %35 + çizik rakam (var). "Her zaman üstte": UI/Default'un `unity_GUIZTestMode` özelliği Always'e çekilir (özel shader yok) | `IntentView.Kind` |
| UYANDI etiketi | uyanma halkası (var) | YENİ etiket | uyanan robot |
| Makine 3D biçimleri (S9-A) | `RaidPieceLibrary` (8 tür ayrı prefab: drone, maintenance, armoredDrone, mortar, bomber, locker, shieldDrone, replicator) | DEĞİŞTİR: 8 türün hepsi S9-A'ya göre (1.2: `raid.js` → `armorDrone()`, `bomber()`, `replicator()`; ölçüler `materials3d.armorDrone/bomber/replicator`; gövde ortak malzeme, kırmızı yalnız göz ve halkada, zırh çelik #9A9C98) | `RaidPieceLibrary.asset` |
| Uzman piyonları (S9-B): harfli ahşap T, E, A; NORMAL, TUR BİTTİ · ✓, YARALI · BANT, PLAKALI, DÜŞTÜ · YATIK | `Technician`, `EmpSpecialist`, `Analyst.prefab`: karton figür (krem kart + Courier harf); Aşama 0 eşlemesinde satır yoktu | YAPILDI (2026-10-31): `raid.js › pawn()` ölçüleriyle `PawnLetterT/E/A` mesh'leri; durumlar `RaidPiece` (`spentMark`, `plateMark`, `downEuler`, `downLift`, `spentBlend`) | `Baskin` birimi (tur bitti, can, `Armor`, `Dead`) |
| Niyetler 1.2: havan atışı, patlama, kalkan, kopyalama | `RaidBoard3D` niyet okları (`RaidArrow`): bugün hepsi `lib.threat` kırmızısı; `shoot` düz, öteki yedi tür aynı yayla (0,30) | DEĞİŞTİR: `intents.lob` yüksek yay; `intents.blast` ok yok, artı kontur + dört kısa çıkış oku; `intents.shield` çift şerit (#854005, ok ucu yok, `badge_shield`); `intents.clone` noktalı çizgi + `marks.ghostWake` + `badge_clone`, uygulanınca `marks.wake`. Shader yok: yay ve oklar `RaidArrow` mesh'i, halka ve alan düz quad | `Baskin.Intents(B)` |
| Etiketteki zırh (1.2) | yok | YENİ: `labels.hpTag.armorIcon` (`icon_armor`, #463F34) zırhlı dronda ve kalkan alan makinede; sıra tür işareti → zırh → pipler | birimin zırhı |
| 1.2 doğrulama fikstürleri | yok | YENİ: S9-C2'nin dört durumu (havan 3 kare + iki duvar; patlama dört komşu, birinde uzman, birinde sivil; kalkan komşu bakım robotuna; kopyalama boş kareye), `Capture Poses` → S9 ile karşılaştırma. **AÇIK:** dört durum kurulmadı; karşılaştırma `SiginakCaptureSet.StartRaid` ya da `SiginakCapture.Render` ile yapılabilir | taktik tahta fikstürü |
| Eğitim notu (baskın) | `SiginakTutorial` baskın kipi ("BASKIN · ADIM n/N") | DEĞİŞTİR: 1560 × 796, 336 px | `TutorialRaid.Steps()` (adım sayısı çözümden türer) |

### 3.8 S7 · Sonuç kartı ve operasyon raporu

| Revizyon 2 öğesi | Mevcut karşılık | Yapılacak | Veri kaynağı |
|---|---|---|---|
| Sonuç kartı (tek damga, 500 px, x 1290) | `RaidPanels › ResultMemo` + `ResultStamp` + `Continue` (`RaidPanelsView`) | DEĞİŞTİR: başlık yerinde damga, yer satırı, üç sayı, tek düğme | baskın sonucu |
| Rapora geçişte harita pozu | dönüşte K1, rapor sol üst + karartma (`DeskUI/Modal/EventModal`, Aşama 8'de silindi) | YAPILDI: `SiginakOperation` kamerayı harita pozuna alır; karartma yok | — |
| Operasyon raporu (560 px, 24 × 274) | `EventModalView` + `EventDocView` ← `SimDeskBuilder.Report` (Aşama 8'de silindi) | YAPILDI (`OperationReportView`, `OperationReportBuilder`, `SiginakOperation`): başlık = sonuç, başarılıda KAZANÇ ızgarası + ± fişleri, başarısızda yalnız sonuçlar + alarm ölçeği; uyarı bandı kalkar | `RaidLogEntry.LiberationReward` |
| İplik + halka ekip figürüne | `TeamFigure` (haritada) | YENİ çizim (rapor iğnesinden figüre) | figürün ekran konumu |

### 3.9 S8 · Son rapor

| Revizyon 2 öğesi | Mevcut karşılık | Yapılacak | Veri kaynağı |
|---|---|---|---|
| Tam ekran rapor 1620 × 952, harita %62 karartılmış | `DeskUI/Modal/GameOver` (`GameOverView`) ← `SimDeskBuilder.GameOver` (Aşama 9'da silindi) | YAPILDI: `FinalReportView` (`DeskUI/Modal › FinalReport`) ← `FinalReportBuilder`; `SiginakFinal` harita pozunda açar | `S.Ending`, `GameData.Ending` |
| Sonuç + neden damgası | `BigStamp` (KAZANILDI/KAYBEDİLDİ; silindi) | YAPILDI: neden damgası (DAĞILMA/VURULDU/SIZMA) | `S.Ending` |
| Dört gösterge | 4 `KpiCardView` (SÜRE, BASKIN, ÖZGÜR BÖLGE, SIĞINAK; silindi) | YAPILDI (görünüş) | aynı |
| İşgal şeridi 1…N tam genişlik | `StripChartView` (gün ekseni 25'in katına yuvarlanıyor; kısa harekât sola sıkışır; silindi) | YAPILDI → `OccupationChart` (`Graphic.OnPopulateMesh`), x = 1…N | `S.Hist` |
| Kırılma anları (en çok 5–6) | `TimelineCardView` (`S.Moments` + sonuç kartı; silindi) | YAPILDI: ilk özgür bölge, karşı saldırılar, kaybın nedeni, son olay; sunum tarafında türetilir | `S.Moments`, `Lib.Regions` (`CapturedDay`), `S.Door.Log` (`Ending`, `Day`), `S.RaidLog` |
| GÜVEN şeridi (Dağılma, öneri) | yok | Soru 8 | — |

### 3.10 Doğrulama araçları

| Revizyon 2 öğesi | Mevcut karşılık | Yapılacak |
|---|---|---|
| Durum fikstürleri | `SiginakFixtures` (V001, V014, V019, K6, injured) | DEĞİŞTİR: V.015 (rapor, karşı saldırı, başarılı/başarısız), V.031, kapı (kapalı/açık/ölümcül), son (kazanılan V.048, Dağılma V.029). Komut dizisi + `ResolveRaid`; simülasyon değişmez |
| Poz görüntüleri | `SiginakCapture`, `SiginakCaptureSet` | YAPILDI (Aşama 10): `SiginakCaptureSet.Start` (DeskGame: S1a…S8b) ve `StartRaid` (Baskin: S6a–c) → `screens_unity_rev2/` (TR, `_EN`, `_gray`); `SiginakCapture.Render` poz adıyla |
| Punto taraması | `SiginakTextScan` (Overlay + dünya metni, poz kamerasıyla) | YAPILDI (Aşama 10): kökler + `SiginakTutorial`, `DeskUI`; Baskin sahnesi ve çalışırken kurulan prefab'lar; rapor `punto_taramasi.txt` |
| EditMode testleri | Son tam EditMode koşusu (bu oturum): 332/332 | YENİ: token 1.1 importu, n vardiya, DEPO eşlemesi, rapor işaret/renk kuralı, kart → siluet tablosu tamlığı |

---

## 4. Ziyaretçi kartı → siluet türü (öneri; Aşama 5'te ara karar olarak uygulandı)

Altı tür `door.js → visitor()`'dan: `anne · yasli · silahli · asker · yarali · kopya`. Dosyada yedinci bir `generic`
biçim de var (tür dışı varsayılan).

| Kart | Kim (oyundaki metin) | Siluet |
|---|---|---|
| `ilk` | Nermin, yaşlı kadın | yasli |
| `anne` | Genç kadın, battaniyeli çocuk | anne |
| `anne_oglu` | Genç, sırtında alet çantası | asker |
| `kurye` | Yağmurluklu kadın, sırtta koli | asker |
| `kacakci` | Deri ceketli adam, iki kasa | generic |
| `kacakci_intikam` | Kimse yok, paket | — |
| `teknisyen` | Zayıf adam, kablo izi, titriyor | yarali |
| `teknisyen_sadik` | Kaçak teknisyen, elinde plaka | generic |
| `teknisyen_casus` | Telsiz görevlisi | generic |
| `gozcu` | Kolu kanlı sargıda | yarali |
| `kopya` | Kıpırdamayan siluet, buğu yok | kopya |
| `disarida` | Karla kaplı adam | asker |
| `heyet` | Altı yedi kişi | grup: 3 × generic |
| `silahli`, `silahli_baba` | Tüfek / tabanca | silahli |
| `baba` | Orta yaşlı adam, siyah bant | generic |
| `hekim` | Yaşlı kadın, deri çanta | yasli |
| `cocuklar` | Üç çocuk | grup: 3 × generic, boy %70 |
| `amator` | Kulaklıklı genç kadın | generic |
| `aile` | Kadın, adam, bebek | grup: anne + generic |
| `zarf` | Kimse yok, zarf | — |
| `ogretmen` | Gözlüklü kadın, defterler | yasli |
| `hirsiz` | İki sakin + genç | grup: 3 × generic |

Tablo Bridge'de durur (`DoorViewBuilder.Silhouette`); `EveryCard_HasASilhouette_AndNobodyCardsShowNone` her kartın bir
karşılığı olduğunu denetler. Yeni kart eklenince test kırılır. Çocuklar genel biçimin %70'i (`VisitorSilhouette.Child`);
kalabalıkta en çok üç yuva.

---

## 5. İşaret kapsamı (sprite ve 3D)

1.2 ekiyle (`EK_1_2.md`) kapsam tam:

| Tür | Oyunda | Paket 1.1 | 1.2 eki |
|---|---|---|---|
| Bölge özelliği | 12 | dar (`glyph_corridor`), sis (`glyph_sand`) | öteki 10 |
| İmza / makine | 8 | dron, bakım, kilit, havan, kalkan | zırhlı dron, patlayan robot, çoğaltıcı |
| 3D makine biçimi (S9-A) | 8 prefab | 5 | aynı üçü (`materials3d.*`, `mdl_*.png`) |
| Niyet oku | 8 tür | ateş, yakın dövüş, kilit, onarım (+ belki/tarandı) | havan atışı, patlama, kalkan, kopyalama + `marks.ghostWake` |

**Eşleme (1.2 eki › Uygularken 3).** Paketin anahtar adları öneri; oyundaki kimliğe bağlanması `SiginakGlyphs`'te
(`Desk/Scripts/Runtime/Siginak/SiginakGlyphs.cs`). `Glyphs_CoverGameTraitsAndMachines` testi her bölge özelliğinin ve
makinenin satırı olduğunu, JSON'un aynı sprite'ı verdiğini ve iki sürümün de `DeskSprites` kaydında olduğunu denetler.
Kare çerçeveli sürüm `SiginakGlyphs.Trait(id, framed: true)`.

| Oyundaki kimlik | Oyundaki ad (TR · EN) | `raid_tokens.json` anahtarı | Sprite |
|---|---|---|---|
| `guvenli` | Güvenli giriş · Safe entry | `regionTraits.safeEntry` | `glyph_safe` |
| `dar` | Dar koridor · Narrow halls | `regionTraits.narrowCorridor` | `glyph_corridor` |
| `kisa` | Kısa nöbet · Short watch | `regionTraits.shortWatch` | `glyph_short` |
| `takviye` | Takviye · **Reinforced** | `regionTraits.reinforcement` | `glyph_reinforce` |
| `acik` | Açık arazi · Open ground | `regionTraits.openGround` | `glyph_open` |
| `sis` | Kum fırtınası · Sandstorm | `regionTraits.sandstorm` | `glyph_sand` |
| `sogutma` | Soğutma ağı · **Cooling grid** | `regionTraits.coolingNet` | `glyph_cooling` |
| `kalabalik` | Kalabalık şehir · Crowded city | `regionTraits.crowdedCity` | `glyph_crowd` |
| `zirhli` | Zırhlı dolap · Armored cabinets | `regionTraits.armoredCabinet` | `glyph_armored` |
| `sebeke` | Elektrik şebekesi · Power grid | `regionTraits.powerGrid` | `glyph_grid` |
| `fabrika` | Kopya fabrikası · Copy factory | `regionTraits.copyFactory` | `glyph_factory` |
| `devriye` | Hızlı devriye · Fast patrol | `regionTraits.fastPatrol` | `glyph_patrol` |
| `dron` | Güvenlik dronu · Security drone | `machineTypes.glyph.drone` | `glyph_drone` |
| `zdron` | Zırhlı dron · Armored drone | `machineTypes.glyph.armorDrone` | `glyph_armordrone` |
| `bakim` | Bakım robotu · Maintenance robot | `machineTypes.glyph.maintenanceRobot` | `glyph_maint` |
| `kilit` | Kilitleyici · Locker | `machineTypes.glyph.locker` | `glyph_locker` |
| `havan` | Havan robotu · Mortar bot | `machineTypes.glyph.mortar` | `glyph_mortar` |
| `kalkan` | Kalkan dronu · Shield drone | `machineTypes.glyph.shieldDrone` | `glyph_shield` |
| `bomba` | Patlayan robot · **Bomber bot** | `machineTypes.glyph.bomber` | `glyph_bomber` |
| `cogalt` | Çoğaltıcı · Replicator | `machineTypes.glyph.replicator` | `glyph_replicator` |

Niyetler: `IntentView.Kind` → `intents.*` anahtarı: `shoot` → `fire`, `melee` → `melee`, `repair` → `repair`,
`lock` → `lock`, `lob` → `lob`, `blast` → `blast`, `shield` → `shield`, `clone` → `clone` (Aşama 7'de bağlandı:
`RaidBoard3D.DrawIntent`).
Zorluk kademesinin getirdikleri (`kopya`, `guclu`) bölge özelliği değil; işaretleri yok.

**1.2'nin açık noktaları:**
- **İngilizce adlar:** oyundaki ad geçerli (ek de böyle diyor). Üçü paketin önerisinden farklı, kalın yazılı:
  Reinforced (öneri Reinforcements), Cooling grid (Cooling network), Bomber bot (Exploder). Niyet adları (Havan atışı,
  Patlama, Kalkan, Kopyalama) oyunda henüz metin değil; Aşama 7'de gösterilirse `strings.txt`'e paketin önerisiyle
  (Lob, Blast, Shield, Clone) girer.
- **Can değerleri:** oyundaki değerler geçerli (`Baskin.UnitTypes`; S9'daki 2 pip örnek). 1.2'deki `materials3d.*.hp`,
  `armor`, `move` oyunla aynı: zırhlı dron 2 · 1 · 3, patlayan robot 1 · 0 · 3, çoğaltıcı 2 · 0 · 2.
- **Onarım rengi:** §8 Soru 12.

Not: Rev2 görüntülerindeki bölge/özellik eşleşmeleri örnek. Örneğin V.031 "Hindistan · kum fırtınası · havan" der;
oyunda Hindistan = Kalabalık şehir + Kalkan dronu, kum fırtınası Orta Doğu'da. Fikstürler oyundaki eşleşmeyi gösterir.

---

## 6. Kararlarım (itiraz etmezsen böyle uygulanacak)

1. **Kalıcı kurucu yok.** Sahne değişiklikleri tek seferlik editör script'leriyle yapılır, çalıştırılır, silinir
   (AGENTS.md kural 5). PROMPT'un "builder'ı genişlet" adımı bu biçimde uygulanır.
2. **Klasör:** §2 (ilk turun düzeni). `Presentation/Siginak/` kullanılmaz.
3. **`tokens_siginak.json` birleştirilir, değiştirilmez.** 1.1'in yeni ve değişen yolları projedeki dosyaya yazılır,
   249 uygulama yolu korunur. Liste dışı iki farkta projedeki değer kalır: `type.taskHead` 14 (≥ 14 kuralı) ve
   `cameras.K6a.pos` (sahnedeki poz değişmez). `raid_tokens.json` olduğu gibi konur.
4. **`CampaignWear` + `HopeMarks`:** İlk turun umut işaretleri (özgür bölgeye bağlı: çocuk resmi, ikinci kupa, gün
   ışığı) kalır; Rev2'nin vardiyaya bağlı izleri ayrı bileşen olarak eklenir. İkisi aynı nesneyi sürerse (ikinci kupa),
   Rev2 eşiği kazanır (V.016). Ayar `SiginakSettings.campaignWear` (açık).
5. **Alarm ölçeği aşağı yuvarlanır** (bugünkü pano gibi). 4,6 → 4 dolu hücre.
6. **Kırılma anları sunumda türetilir.** Core'a yeni `Moment` eklenmez. Kaynaklar: `S.Moments`, bölgelerin son
   `CapturedDay`'i (ilk özgür bölge), `S.Door.Log` (ölümcül kapı), `RaidLog` (güven kaybı). En çok 6 an.
7. **Yetersiz kaynak durumu yalnız sunumda.** BASKIN EMRİ VER ve GELİŞTİR açık görünür; basınca önce `*Check()` okunur.
   Eksik yalnız kaynaksa komut çağrılmaz, sayaç titrer, teleks gerekçeyi basar. Başka gerekçede düğme devre dışıdır.
8. **K kısayolu kalır.** 4 · KAPI eklenir, K eş anlamlı olarak çalışmaya devam eder (kenar satırında yalnız 4 görünür).
9. **Sayılar oyundan gelir.** Görüntülerdeki sayılar, bölge eşleşmeleri ve tur sayıları (TUR 1/8) örnektir. Oyunda
    baskın 5–7 tur sürer; şerit tur sayısı kadar hücre çizer.
10. **Baskın kamerası değişmez** (132,58 px/birim, `tokens_sade.json`).

---

## 7. Teyit edilecek kararlar (paketin önerileri)

| Karar | Öneri | Not |
|---|---|---|
| Karar 1 · Görev fişinin yeri | **B · sol kenar** (Kuzey/Güney Amerika seçiliyse sağ) | |
| Karar 2 · Kapı işareti | **A · rayda 4 · KAPI** | K kısayolu da kalır (§6 madde 8) |
| Karar 3 · Maketin üst bandı | **A · DEPO** | Soru 6'daki ölçek sorunu bu seçeneğe bağlı |
| CampaignWear eşikleri | Tebeşir çentiği = vardiya no (beşli) · umut tenekesi V.001–009 3 yaprak, V.010–024 7, V.025+ 11 + sap · yırtık teleks ve EVRAK katı ⌊vardiya ÷ 3⌋ (en çok 10) · teleks kuyruğu masadan sarkar, ikinci kupa ve teneke V.016'dan | Referans yalnız erken ve geç hâli çizer |

---

## 8. Açık sorular

1. ~~**Rev1/Rev2 anahtarı.**~~ **Karar (2026-10-30, kullanıcı): anahtar yok; git yeterli.** Her aşama ayrı bir
   değişiklik (kullanıcı commit eder); `screens_v2/` bugünkü hâl, `screens_unity_rev2/` yeni hâl. PROMPT'un başarı
   ölçütü 5 ve Aşama 10'daki "Rev1 görünümünü kaldır" adımı düştü. Token'ların `*.v1_0.json` yedekleri de yazılmaz:
   1.0 dosyaları git'te (`9d16037`).
2. ~~**Karar 1 B · 2 A · 3 A ve CampaignWear eşikleri**~~ **Onaylandı (2026-10-30, kullanıcı):** fiş solda, rayda 4 · KAPI, DEPO bandı, §7'deki eşikler.
3. **Kapı kısayolu:** 4 eklenip K da kalsın mı, yoksa K kaldırılsın mı? *Cevap gelene kadar §6 madde 8: K eş anlamlı kalır.*
4. ~~**Eksik işaretler ve biçimler** (§5)~~ **Karar (2026-10-30, kullanıcı): tasarımcıdan istenecek.** İstek
   `ISTEK_isaretler.md`'de. **Geldi (2026-10-30, 1.2 eki, `EK_1_2.md`):** 13 işaret (düz + `_frame`), iki rozet, üç
   makinenin 3D biçimi, dört niyet. Temele alındı (CHANGELOG); 3D biçim, niyetler ve fikstürler Aşama 7'de.
5. **Ziyaretçi siluetleri:** §4 tablosu uygun mu? Altı tür oyundaki 23 kartı karşılamıyor; tablo `door.js`'teki
   `generic` biçimini yedinci tür olarak ve grup dizilimini (heyet, çocuklar, aile, hırsız) kullanıyor. *Cevap gelene
   kadar (Aşama 5) tablo olduğu gibi uygulandı.*
6. **DEPO ölçeği:** Erzak sınırı yaşam alanı seviyesiyle 30 → 60 → 90'a çıkıyor. "1 sandık = 3 erzak" SV3'te 30 sandık
   ister; bant 10 sandık alıyor. Seçenekler: (a) sandık değeri sınırla büyür (sınır ÷ 10: 3, 6, 9) ve tebeşir tahtası
   değeri yazar; (b) sandık 3 erzak kalır, yığın iki-üç kat yükselir. Öneri (a). *Cevap gelene kadar (Aşama 4) (a)
   uygulandı:* 10 sandık = erzak sınırı (3 / 6 / 9), 5 kova = malzeme deposu (kova 12; tasarımın “1 kova = 5”i 60'lık
   depoda 12 kova ister, iki sıra kova arka duvardaki tebeşir tahtasını örtüyordu). Tebeşir yalnız “ERZAK · n VARDİYA”
   yazar; sandık değeri yazılmadı (tahta tek satır).
7. **Yetenek adları:** Teknisyen'in Sabotaj'ı robota 1 hasar verip 1 kare iter; Analist'in Kanca'sı 1 hasar verip çeker.
   Öneri: T "Kasadan ikmal al" · "Robota vur · −1 can · iter"; A "Robota vur · −1 can · çeker" (bugün "−1 robot ·
   çeker"). E değişmez ("−1 hedef · itme"). *Cevap gelene kadar (Aşama 7) S6'nın adları uygulandı:* T "Kasadan ikmal al"
   (sunucuda "Dolaba vur · −2 can") · "Robota vur · −1 can"; E "−1 hedef" · "itme"; A "Robota vur · −1 can" · "çeker".
   İtme/çekme ayrı alt düğmede durduğu için adlara eklenmedi.
8. **Son rapordaki GÜVEN şeridi (öneri):** vardiya başına güven kayıtta yok. Seçenekler: (a) `HistPoint`'e `Trust` alanı
   eklemek (Core'da pasif istatistik; RNG ve komut sonuçları değişmez, kayıt biçimi değişmez, eski kayıtlar yeniden
   oynatılınca dolar, `GameRecord.CurrentVersion` artmaz); (b) şeridi yapmamak. Öneri (a), ama Core'a dokunduğu için
   onayını istiyorum. *Aşama 9'da yapılmadı: cevap gelince şerit ve Dağılma'nın ikinci satırı (“Son vardiyada güven 1'den
   0'a düştü”) eklenir; raporda yer var (grafiğin altı).*
9. **Mantar panodaki KAYIP ilanları** (`Missing A/B/C`, ilk turun "KAYIP ilanları"): KURTARILAN BÖLGELER panosu tüm
   panoyu kaplıyor. İlanlar kalksın mı, panonun kenarına mı kaysın? *Cevap gelene kadar referans (S1a/S1b) izlenir: panoda ilan yok, ilanlar kalkar.*
10. **Ölümcül kapıda oda ışığı:** "Oda kırmızı" bütün odanın ışığını mı değiştirir (tavan ampulü + banker lambası
    kırmızıya), yoksa yalnız kapı lambası ve koridor mu? Öneri: ampul ve banker lambası 0,05 s'de #F0443A'ya, kâğıt kapanana
    kadar. *Cevap gelene kadar (Aşama 5) öneri uygulandı; renk tokenlardaki `door.fatalRoomLight` (#C0281C), kapı lambası
    `door.knockLampFatal` (#F0443A).*
11. **Telsiz yüzü ve kayıt defteri (S1 notu › CRT pozu):** tasarım telsize kanal skalası, SİNYAL göstergesi, SES/AYAR
    düğmeleri ve defterde son dört kaydı ekliyor. Daha önce “telsiz vurgusu artmasın” dediğin için Aşama 2'de yapmadım.
    Yapılsın mı? **Cevap (2026-10-31): yapılsın — yüz ve defterdeki son dört kayıt yapıldı.** SİNYAL ibresi sabit (HAT
    AÇILIYOR'da saklanan bir sinyal değeri yok).
12. **Onarım niyetinin rengi (1.2 eki):** yeni kural "kırmızı yalnız uzmana ya da sivile zarar veren niyette". Onarım
    zarar vermiyor ama 1.1'deki gibi kırmızı noktalı çiziliyor; tasarımcı değiştirmemiş, karar sende. Onarım da
    #854005'e (kalkan ve kopyalamayla aynı) mı çekilsin, kırmızı mı kalsın? Çekilirse `intents.repair.color = #854005`
    tek değeri eklenir. Öneri: çekilsin (kural tutarlı olur; bakım robotunun onarımı oyuncuya zarar vermez). *Aşama 7'de
    kırmızı kaldı (kesik 2,6 px + anahtar rozeti); cevap gelince tek renk değişir.*

13. ~~**Kare süresi (Aşama 10)**~~ **Karar (2026-10-31, kullanıcı): yumuşak gölge kalır; fark kabul.** Revizyon 2, Revizyon 2 öncesinden (`9d16037`) masa ve sığınak pozunda %2–3 (0,08–0,14 ms)
    yavaş; harita ve CRT eşit (ölçüm ve yapılanlar CHANGELOG'da). Fark yeni içeriğin kendisi (DEPO bandı, tebeşir çentiği,
    alarm ölçeği, KAPI tuşu). Ampul ve masa lambası gölgeleri yumuşak yerine sert olursa Revizyon 2 öncekinden %3–6 hızlı olur
    (masa 4,11–4,25 ms ↔ önceki 4,41–4,45 ms); gölge kenarları sertleşir (ilk turdan beri bekleyen karar). Seçenekler: (a) böyle
    kalsın, (b) sert gölge, (c) tebeşir çentiğini ve DEPO sandıklarını tek mesh'e birleştirmek (~0,05 ms, görünüş aynı).

---

## 9. Riskler

1. **Çalışma ağacı temiz değil.** Bu oturumdan commit edilmemiş değişiklikler var (telsiz vurgusunun geri alınması, robot
   itme çubuğunun kaldırılması, iki dinamik font asset'i). Revizyon 2 bunların üstüne gelir.
2. **Sahne kaydında gürültü.** İlk turda uzun açık kalan sahnenin kaydı ilgisiz ~260 nesneye düzenleme anı değeri yazdı.
   Her tek seferlik script'ten sonra sahne diff'i gözden geçirilip ayıklanmalı.
3. **TMP RectTransform kaydı.** Sayaç modülleri altındaki TMP'leri `localPosition` ile taşımak kaydedilmiyor;
   `anchoredPosition` kullanılmalı (ilk tur CHANGELOG'u).
4. **Baskın prefab'ları büyük ve dağınık.** `RaidHud` 253, `RaidPanels` 293 nesne; konumların bir kısmı kodda
   (`RaidPanelsView` sıra kutularını başlık genişliğine göre diziyor, tur şeridi tur sayısıyla kayıyor). Yeniden giydirme
   kod ile prefab'ı birlikte değiştirir.
5. **Dinamik font asset'leri.** Play'de `CP-Bold Dyn SDF` ve `DejaVuSans-Bold Dyn SDF` atlasları değişip diff'e düşüyor;
   charset'e glif eklenince statik atlaslar da yeniden kurulur.
6. **Niyet okları "her zaman üstte":** `unity_GUIZTestMode` Always'e çekilince oklar parçaların da üstünde çizilir;
   robotun gövdesinin arkasından çıkan ok gövdenin önünde görünür. Tasarım bunu istiyor; bugünkü okunuştan farklı.

---

## 10. Görsel farklar

**S1 (Aşama 2)** — referans `S1a.jpg`, `S1b.jpg`:

| Öğe | Fark | Neden / durum |
|---|---|---|
| Alarm panosu | x 0,06 (referansta 0,15), ölçek 1,3 | 2026-10-31: room.js/tex2.js'e göre (x 0,15, ölçek 1, küre lamba, boyalı çerçeve). Kapandı |
| Pano kartları | Mavi satır çizgileri yok; kalem kartında kesik kenar yok | Kâğıt düz Quad; ayrıntı uzaktan seçilmiyor. Kapandı |
| Oda kartı | Kişi işareti yok (“SV 1 · 2/2”) | Satır içi işaret için TMP sprite asset'i gerekir. Açık |
| Teleks ön kâğıdı | Delik şeridi yok; kâğıt ışıktan bağımsız | Okunurluk için. Kapandı |
| Telsiz yüzü ve kayıt defteri | Kanal skalası, SİNYAL göstergesi, son dört kayıt yapılmadı | 2026-10-31: yüz, kapak ve son dört kayıt yapıldı (Soru 11); SİNYAL ibresi sabit. Kapandı |
| Umut tenekesi | Raftaki teneke ilk turun yerinde (üst raf, sağ) | Referansla aynı köşe; yaprak eşikleri uygulandı. Kapandı |
| Kapı tuşunun fişi | K1'de yazısı seçilmiyor (tuşun arkasında masada) | Referansta da üzerinde etiketi okunur. Kapandı |

**S2 (Aşama 3)** — referans `S2-TR.jpg`, `S2-EN.jpg`; oyunda V.001, V.014 (TR/EN), V.015 karşı saldırı, V.031 (TR/EN):

| Öğe | Fark | Neden / durum |
|---|---|---|
| Ekip noktaları | Kuzey Amerika, Doğu Avrupa, Japonya-Kore 12–34 px kaydı; Batı Avrupa'da figür etiketin üstünde (referansta V.001'de solunda) | `map2.js → LAB.team`'in bu dördü denizde; oyunun kuralı figürün karada durması (bırakma isabeti, `DeskGameScene_LiberationMapIsWired`). Batı Avrupa'nın etiket solundaki karası figüre yetmiyor. Kapandı |
| Hindistan'ın cephe şeridi | Adın sağında (referansta solunda) | Solda Orta Doğu'nun ✓✓✓ işaretleriyle ve kendi adıyla çakışıyordu (V.031); sağda 10 px yukarıda. Kapandı |
| İplik | Uzak bölgede (Amerika seçiliyken fiş sağda, ya da V.031) ad ve damgaların üstünden geçer | Sarkma tasarımdaki gibi 30–40 px; kiriş uzayınca adlardan kaçamıyor. Açık (kabul) |
| Özgür damgası ve ekip figürü | V.031'de Orta Doğu damgası figürün sol üstüne değer | İkisi de tasarımdaki yerinde; etiket değil. Kapandı |
| Ekip diskleri | Yedek uzman “2/2 yedek”, eksik rol soluk disk + “—” | Referans yalnız tam kadroyu çizer. Kapandı |
| Uzun ekip damgası | “SONRAKİ VARDİYA”, “TEKNİSYEN YARALI · n GÜN” sütunu en çok 160 px'e genişletir, sığmazsa iki satır | Referans yalnız HAZIR'ı çizer. Kapandı |
| Devre dışı düğme | Enter fişi kalkar, kısa neden sağda (14 px) | 432 px'e ad ve neden birlikte sığmıyordu. Kapandı |

**S3 (Aşama 4)** — referans `S3-TR.jpg`, `S3-EN.jpg`; oyunda V.001, V.014 (TR/EN; atölye, telsiz odası, revir seçili;
boşta kimse yokken; telsizde görevliyle), masa pozu:

| Öğe | Fark | Neden / durum |
|---|---|---|
| Malzeme kovaları | 2 kova (referansta 4) | Soru 6 ara kararı: kova = depo ÷ 5 = 12. Açık (Soru 6) |
| Kısmi kova | Son kova kısalır (referansta görünmez, iç dolgu kapalı kutuda) | Kamera bandı alttan görür; iç dolgu seçilmiyordu. Kapandı |
| Tebeşir tahtası | 8 mm yukarıda (y 0,556) | Kamera alttan baktığı için öndeki kova yazının başını örtüyordu. Kapandı |
| Üst kat | Toprak üç katmanı inceltildi, yıkıntılar %60 boyda, YZ direği 3 cm | DEPO bandı 0,1 m ekledi; maket 0,70 m'de kalmazsa üst rafa değiyordu (referans 0,72 m, direği tahtaya giriyor). Kapandı |
| Pano | Ekranda %10 büyük, 30 px yukarıda | Kamera ve pano yeri tokenlardaki gibi; fark referansın HTML yerleşiminden. Kapandı |
| BOŞTA levhası | Gölgede, yazı soluk | Pirinç az metalik yapıldı; levha panonun gölgesinde. Açık |
| Oda kartında kişi işareti | Yok (“SV 1 · 2/2”) | Aşama 2'den (TMP sprite asset'i gerekir). Açık |
| V.001 erzak | “34 VARDİYA” (referansta 17) | Oyunda V.001 nüfusu 6 (referans fikstüründe 8); ilk vardiyada fark beklenen tüketimden hesaplanır. Kapandı |

**S4 (Aşama 5)** — referans `S4a.jpg`, `S4b.jpg`, `S4c.jpg`, `S4d.jpg`; oyunda V.014 (TR/EN): vuruş, kapalı kapı (bakıldı,
soruldu), açık kapı (yetersiz erzak, revirde görevli yok), sonuç, VURULDU (silahlı sakin), SIZMA (kopya), siluet kataloğu:

| Öğe | Fark | Neden / durum |
|---|---|---|
| Seçim satırındaki rakam | Silahlı sakinde “SEÇİM · 4 KAPIYI AÇ” (referansta 3) | Rakam seçeneğin kâğıttaki kısayolu; silahlı sakinin kapalı kapıda “İnterkomdan konuş” seçeneği var. Kapandı |
| Ölümcül son cümle | VURULDU ve SIZMA için ayrı cümle; sonun uzun metni son raporda | Referans metinleri örnek. Kapandı |
| İnterkom | İNTERKOM levhası yok; gövde bakalit #2A2622 (referansta açık gri metal) | S4 notundaki nesne tanımı levhasız; renk tokenda. Kapandı |
| Siluet | Kapının 0,62 m gerisinde (referansta 0,42) | Kamera kapıya yakın; ilk yerde siluet kapı boşluğunu dolduruyordu. Kapandı |
| Merdiven | 7 basamak × 0,24 m (referansta 9 × 0,28) | Merdiven boşluğu 2,4 m; kapıdan görünen kısım aynı. Kapandı |
| Gözetleme bloğu ikonu | İnterkom bloğunda `icon_radio` | Pakette interkom ikonu yok. Açık (tasarımcıya sorulabilir) |
| Yetersiz kaynak satırı | Çizik bedel ve “erzak yetersiz” sağ sütunda iki satıra sarar | 470 px'te sütun 210 px; satır 50 px'te kalır. Kapandı |
| Dünyada tıklanan gözetleme deliği ve interkom | Yapılmadı (kâğıttaki 1 ve 2 aynı işi yapar) | Kâğıt açıkken masa hedefleri kilitli; sarı kontur için kontur gölgelendiricisi gerekir. Açık |

**S5 (Aşama 6)** — referans `S5a.jpg`, `S5b.jpg`; oyunda V.014 (TR/EN): normal rapor, karşı saldırılı rapor, tepsiden okuma:

| Öğe | Fark | Neden / durum |
|---|---|---|
| FON gözünün farkı | Vardiyanın bütünü (açılış → kapanış; baskın emri ve onarım dahil) | Göz altındaki “62,0 → 64,5” satırıyla tutarlı olsun; S5a'da ikisi aynı (+2,5). Sayacın ± fişi kol inişinin farkını gösterir. Kapandı |
| Alarm satırı | “4 → 5 · 10'da karşı saldırı” iki satıra sarar | Referansta da sarar; EN'de eşik satırı da iki satır. Kapandı |
| Özet gözlerinin araları | Düz 1 px çizgi (referansta noktalı) | Dikey noktalı sprite yok. Açık (küçük) |
| İkinci kupa | V.016'dan sonra harita pozunda VARDİYA sayacının bir kısmını örtüyor | Aşama 2'nin harekât izleri (`CampaignWear`, onaylı eşik); S5 V.015'te çizildiği için görünmüyor. Açık: kupanın yeri kaydırılabilir |
| Bölge kartının düşmesi | Kol iner inmez (kamera vardiya pozundayken) | Pano arka duvarda; harita pozundan görünmez. Açık (kabul) |
| ± fişlerinin süresi | Rapor kapanana kadar asılı (referans: 2,5 s, 0,4 s'de solar) | Rapor açıkken fişler okunur kalsın diye ilk turun davranışı korundu. Açık (kabul) |

**S6 (Aşama 7)** — referans `S6a.jpg`, `S6b.jpg`, `S6c.jpg`, `S6-not.md`; oyunda `Baskin` tek başına, ikmal baskını (tohum
914, DAR KORİDOR, kilitleyici imzası): S6a TR/EN, S6b makine evresi, S6c son tur, UYANDI, kilit tehdidi:

| Öğe | Fark | Neden / durum |
|---|---|---|
| “Belki” / taranmış niyet | Çizilmedi | Core'da niyetler kesin; böyle bir durum yok. Açık: Core'a girerse stil eklenir |
| Kopyalamanın hayalet uyanma halkası (`marks.ghostWake`) | Bugünkü kırmızı uyanma halkası | 2026-10-31: kesikli kahverengi halka + ışınlar, iğnesiz (`Marks.Clones`); dolgu yok. Kapandı |
| Patlamanın artı konturu | Yalnız kırmızı tarama + dört kısa çıkış oku | Açık (küçük) |
| Onarım oku | Kırmızı kesik | Soru 12 |
| EN'de sol üst grup | 592 px (not: EN'de 620 px) | İçerik sığıyor (NARROW HALLS, CIVILIAN SERVERS); tahtaya değmiyor. Açık (küçük) |
| EN'de EMP adı kartta | “EMP SPEC.” (listede EMP SPECIALIST) | 237 px kartta sığmıyor; brifin kısa biçimi. Kapandı |
| Kilit tehdidi | Kilit niyetinin hedef karesindeki uzmanın kartında; uzman kareden çıkınca kalkar | Core kilidi kareye atar; S6c'deki satır. Kapandı |
| Uyanma, son tur sesleri | Uyanmada `RaidSpawn`; SON TUR damgasında ayrı “tak” ve zil yok | Klip yok. Açık (ses) |

**S7 (Aşama 8)** — referans `S7a.jpg`, `S7b.jpg`, `S7c.jpg`, `S7d.jpg`, `S7-not.md`; oyunda V.014 → Doğu Avrupa 02 baskını
(başarı ve geri çekilme), TR/EN:

| Öğe | Fark | Neden / durum |
|---|---|---|
| Alarm ölçeğinin kırmızı hücreleri | Düz 1,5 px kırmızı kenar (referansta kesik) | 10 px'te kesik desen okunmuyordu; vardiya raporunun ölçeği de aynı. Kapandı |
| Sonuç kartının satırları | Hedef satırı görevin adıyla (“İkmal kasası”, “Düğüm”); sunucu görevinde iki hedef → dört satır | S7 ikmal görevini gösteriyor. Kapandı |
| Rapor cümlesi | Teknisyen yedeksiz düştüyse ya da güven 3'ün altına indiyse cümle o uyarıdır | Eski uyarı bandı kalktı; uyarı tek cümlede. Kapandı |
| SONUÇLAR'ın ek satırları | Düşen uzman, yeni yara, kırılan plaka, sivil güven, karşı saldırı satırları (S7'de yok) | Oyunun bilgisi; tekrar etmez. Kapandı |
| KAZANÇ'ta sıfır göz | “±0” gri (S7c'de dört göz de artı) | Depo doluysa ya da sunucu görevinde erzak/kurtulan gelmez. Kapandı |
| İngilizce künye | Etiket sütunu en az 62 px, “DURATION” sığsın diye genişler | Sütunlar satırlar arasında birkaç px kayabilir. Açık (küçük) |
| Rapor tepside | Tepsiye kayar, ama tepside okunmaz (tepsi yalnız vardiya raporlarını tutar) | Tepsinin arşivi `ShiftReportInput`; operasyon raporu kayıttan kurulabilir. Açık |
| Sonuç sesleri | `ResultBell` (65) klipsiz; başarısızda `StampHeavy` | Klip yok. Açık (ses) |

**S8 (Aşama 9)** — referans `S8a.jpg`, `S8b.jpg`, `S8-not.md`; oyunda V.014 fikstüründen dört son: Silme (46 vardiya, karşı
saldırılı), Dağılma (29 vardiya), Vuruldu ve Sızma (kapı), TR ve EN:

| Öğe | Fark | Neden / durum |
|---|---|---|
| GÜVEN şeridi (Dağılma) | Yok | Vardiya başına güven kayıtta yok; Soru 8 (Core'a pasif alan). Açık |
| Dağılma'nın ikinci satırı | Sonun metni (“Hastaneler ve bankalar…”); referansta “Son vardiyada güven 1'den 0'a düştü…” | Önceki güven kayıtta yok (Soru 8). Açık |
| Kazanınca cümle | “Bütün bölgeler özgür. YZ'nin son sunucusu 46. vardiyada düştü.” (referans “On iki bölgenin hepsi…”) | Bölge sayısı veriden; sayı yazıyla yazılmıyor. Kapandı |
| Vuruldu / Sızma cümlesi | “Komutan kapıda vuruldu; Sığınak 07 n. vardiyada düştü.” / “Sığınak 07'nin yeri n. vardiyada YZ'ye sızdı.” | Referansta yok (yalnız neden damgası ve son an); ikinci satır sonun metni. Kapandı |
| Kapıda güven düşüşü satırı | “Kapı: Ziyaretçi · “Seçim” · güven −1” (referans “Kapıda “İlaç yok” dendi · güven −1”) | Kartın seçim etiketi ve ziyaretçinin adı kullanılıyor. Kapandı |
| Aynı vardiyadaki anlar | İşaretler çizgide sola dizilir (son an noktanın üstünde) | Referansta örnek yok. Kapandı |
| Grafikte çizgi sesi | Teleks sesi yok | Klip yok. Açık (ses) |
| Damga sesi | Kazanınca `FinalBell` (66, klipsiz), kaybedince `StampHeavy` | Klip yok. Açık (ses) |


**Aşama 10 (tam set, `screens_unity_rev2/S*.png`, `_EN`, `_gray` ↔ `reference/screens/`):** S1a, S1b, S2, S3, S4a–c, S5a–b,
S6a–c, S7a–d, S8a–b; TR ve EN (S4d siluet kataloğu oyunda bir durum değil, sette yok).

| Öğe | Fark | Neden / durum |
|---|---|---|
| Kapı pozu | Kamera kapıya bakıyor, kapı ekranın ~%56'sında (önceden ~%62, sağda) | Pozun hedefi kapının 0,4 m yanındaydı; S4a–c'deki gibi. Kapandı |
| Teleks ön kâğıdı | Durum satırı tek satır; uzunsa %132'den küçülür (en az ileti boyu) | “KYGA-07 HAT AÇIK” ve EN “KNOCK AT THE DOOR” sarıyordu; S1a/S1b'deki gibi. Kapandı |
| S1a eğitim notu | “Burası Sığınak 07…” (referansta “Önce cepheye bak…”) | Oyunun eğitim metni, ilk adım. Kapandı |
| S1b | Kapıda bekleyen ziyaretçi el yazısı fişle (referansta üzerine gelme etiketi) | Görüntüde imleç yok. Kapandı |
| S5b | V.014 → 015 karşı saldırılı rapor | Referansla aynı vardiya. Kapandı |
| S6a | Eğitim notu yok | Eğitim yalnız eğitim baskınında; set örnek ikmal baskınını çeker. Kapandı |
| EN: SON TUR | Damga yazıya göre genişler; EMRİ İLET'in adı “LAST TURN”a yer bırakır (gerekirse 21 → 15 punto) | EN “LAST TURN” damgadan, “SEND ORDERS” notun üstüne taşıyordu. Kapandı |
| EN: neden damgası | Sonuç damgasının sağ ucuna bağlı | Kısa “LOST” yanında “DISBANDED” boşta kalıyordu. Kapandı |
| EN: rapor başlığı | “KYGA · OPERATION REPORT”, “KYGA · FINAL CAMPAIGN REPORT” | “AISA” yalnız bu iki başlıktaydı; oyunun İngilizcesi her yerde KYGA. Kapandı |
| Operasyon raporu numarası | İğneden 18 px uzak | Sağdaki “tek nüsha” iğnenin altına giriyordu (TR ve EN). Kapandı |
| Gri tonlama | Durumlar sözcük, işaret ve desenle ayrılıyor (damgalar, tarama, ✓/✕, ±); makine türleri biçimden ve etiket işaretinden | Son raporda işgal ve geri alınan alanlar gride yakın tonda; çizgi ve yazılar (İŞGAL · %92, GERİ ALINAN) ayırıyor. Kapandı |

---

## CHANGELOG

- **2026-11-01 · Haritanın kenarı (S2):** bölge çokgenleri kâğıdın dışına 12 px'e kadar taşıyordu (Grönland, Rusya'nın kuzeyi,
  Çukotka, Yeni Zelanda, Alaska). `SadeMapCanvas`'a `RectMask2D` (tek seferlik eval, sahnede); referanstaki gibi harita
  kâğıdın kenarında kesilir. Test `DeskGameScene_MapIsClippedToPaper`.

- **2026-11-01 · DEPO plaka rafı (S3):** plaka belirince maketin önünde 1 m'lik levha oluyordu: `ShelterStore` ölçeği
  `Vector3.one`'a yazıyordu, plakalar doğrudan ölçekli küp. Artık sahnedeki ölçek saklanır; kalkarken gelen "göster" de
  nesneyi geri getirir. Sahne değişmedi. Ayrıntı: `Docs/DURUM.md` → "Maketteki plaka rafı".

- **2026-10-31 · Kadro seçimi ve iki yeni uzman (referansta yok):** `DeskGame.unity` iki tek seferlik eval'le değişti
  (depoya girmedi). Görev fişinde `Team`'in altında **KADRO** satırı (`Paper/Pick`: başlık + beş `Chip`; çip = dolgu
  Image, Button, `UiHoverTarget`, `Member 1`'in `Disc` ve `Hp` kopyaları; 64 × 44, sayfa iç payı 18/16); `OperationSlip`
  `pickBlock/pickHead/pickHover/pickChips`, `SiginakMap.tutorial`. Seçili çip `slip.rowSelected`, üzerinde `slip.rowHover`,
  seçilmeyenin diski `ui.disabledFill`; T kilitli (SABİT). Pano: `Row UZMANLAR`'da beş üye `Discs` ızgarasında (3 sütun,
  72 × 22, iki satır; tek satıra 438 px gerekiyordu, 364 px var). Baskın: `Miner`, `SmokeSpecialist` piyonları ve
  `thumb_standee_M/S` (S9-B yöntemiyle), duvar karoları durumdan, sis bulutu ve kesik ok (`RaidBoard3D`). Ayrıntı:
  `Docs/DURUM.md` → "Madenci, Sis uzmanı ve kadro seçimi".

- **2026-10-31 · Telsiz defteri (Soru 11, S1 notu “son dört kayıt”):** `RadioLogBuilder` (Bridge) hattın geçmişini
  simülasyondan türetir: her baskının dönüşü (“Alfa döndü · D. Avrupa 02 alındı” / “geri çekildi”), düşen uzman, kırılan
  sivil sunucu, son karşı saldırı (“YZ karşı saldırısı · … yeniden işgal”) ve bugünün satırları (ekip hazır / sahada / Teknisyen
  yaralı, dinleyen telsiz odası, hat durumu: “Dinlemede — emir bekleniyor”…). Kod “vardiya/sıra” (sayaçtaki vardiya, 014/02),
  son dört satır. `RadioLogBook` (sayfada CP kod + Kalam kayıt, mürekkep mavisi) `SiginakCrt` her yenilemede yazar. Sayfa
  tex2.js logBook düzeninde: başlık “TELSİZ KAYDI · K-07”, mavi satır çizgileri, kırmızı kenar boşluğu; sahte el yazısı
  çizgileri kalktı. 16 yeni metin çevrildi; punto taraması istisnası güncellendi. Test `RadioLog_LastFourEntriesFromRaidHistory`.

- **2026-10-31 · Model denetimi (DeskGame + Baskin, referansla nesne nesne):** üç ajan `room.js`, `door.js` + `diorama.js` ve
  `raid.js`'i sahne/prefab dökümüyle karşılaştırdı; değişmemiş modeller referansa getirildi (tek seferlik script'ler, depoya
  girmedi). Kullanıcı kararları: telsiz yüzü yapılsın (Soru 11), kapı referansın yerine taşınsın, tarama yönü ve işaret
  derinliği referans görüntülerindeki gibi.
  - **Oda:** YZ alarm panosu (0,15 · 1,33 · 2,27, ölçek 1; tex2 düzeni: iç pirinç çerçeve, etiket, sayaç penceresi, 10 hücre;
    lamba Ø 0,09 küre, `Alarm lamp off/on`, boyalı halka çerçeve), tebeşir çentiği x −0,083, mikrofon (0,24 m, siyah baş,
    krom boyun ve ızgara, kırmızı tuş), telsiz kaydı (yeşil kapak, kalem), telsiz yüzü (hoparlör bezi, kadran penceresi + parıltı,
    SİNYAL göstergesi, SES/AYAR, TELSİZ · K-07 plakası, küçük düğmeler; `tx_radio_dial` Rev2 düzeninde, `SiginakTextureBaker`),
    teleks plakası (0,08 · 0,152), afiş x 0,68 + geç evrede yıpranan köşe bandı, EVRAK yığını (taban 0,02, kat 3,6 mm), umut
    bitkisi V.025'te room.js'in 11 büyük yaprağı + sapı (`CampaignWear.Stage.untilShift`), boş raptiyeler woodLight, teleks
    kâğıdı #EFE4BC, çocuk resmi panonun boş sağ üst köşesinde, teleks kuyruğu masanın kenarından düz sarkar.
  - **Kapı:** açıklık z 0,32–1,18 (kanat 0,84 × 1,98), kasa 0,08 × 0,14, üç oliveLight takviye, el çarkı R 0,15 / 0,018 + 8 kol,
    gözetleme camı parlak, 07 y 0,42, tehlike bandı kasanın üstünde, vuruş lambası y 2,31 (kafes dikey meridyenler, kapalı
    #4A3A2A), interkom düğmesi bakalit, LED kapıdan uzak yanda, galvaniz boru. Koridor, siluetler ve kapı pozu +0,15; ziyaretçi
    çentiği kapıya göre yerinde, referans boyunda (×1,68, genişlik kapıya taşmaz). Kapı pozunun sağındaki boşluk: sağ duvar
    uzantısı, ön duvar, zemin ve tavan uzantısı (door.js:16-20). Sahanlıkta üç moloz. Siluetlerde kask, eşarp, kapüşon yarım küre.
  - **Maket:** odaların tavanı 0,016 / y 0,498 (üst lambalar ve telsiz lambası görünür), DEPO bandı +0,016, toprak katmanları
    inceldi (çerçeve 0,70 kaldı); telsiz odası Rev2 (dar masa, kahverengi telsiz + kadran penceresi = `activeLights`, krom anten,
    tavana kablo; ilk görevlide kulaklık); yaralı piyonun bandı #3F5F8A + baş sargısı; plakalar metalik 0,8; döküm kalıbı #2A2622.
  - **Baskın parçaları:** Node (hedef dolabı: 0,8 × 0,9, dört köşe dikmesi, ortalı etiket, kırmızı kare), kablo kanalı (1,0 boy,
    üç bakır, sürekli tehlike şeritleri), sivil sunucu (güneyde 5 menfez, 0,06 bant), kopya dolabı (iki krem sayfa, parlak ayna),
    duvar (iki kat), soğutucu (#9DB2B1, çapraz pervane, dikdörtgen etiket), uyanma işareti (dolgu diski + bilyeli iğne), patlayan
    robot tam boy. Core değişmedi (referansta yok).
  - **Baskın işaretleri:** karo işaretleri `Pencil marks.mat` (ZTest LessEqual; parçalar örter), oklar ve eğitim çerçevesi
    `Pencil.mat`'ta üstte. Tarama köşegen (hareket ekranda dikey, isabet yatay; pitch 0,076); seçim ve yetenek hedefi kalın/ince
    çift çerçeve + α .10 dolgu; uyanma halkası 0,31–0,345; kopyalama hedefi kesikli kahverengi hayalet halka. `tx_ozalit`
    raid.js'ten yeniden üretildi (rastgele girişlerle uyuşmayan basılı kapı yayları kalktı). Kontrplağın güney yüzü batıyla aynı.
  - **Not:** README'deki “bütün açılar işaret değiştirir” kuralı rz için yanlış (z aynası rz'yi korur; yalnız rx, ry döner).
    Bilinçli bırakılanlar: siluetlerin kask kenarı yatay ve bebek bohçası eğik (referans `add()` döndürmeyi siliyor), yedek uzman
    ranzada yatık (referansta başı üst ranzadan çıkıyor), seçim çerçevesi (maket kartı) ilk tur stilinde, `raid_tokens.json`'daki
    tarama yönü ve kart tarifi eski hâliyle (oyun referans görüntülerini izler).
  - Tam EditMode (boş sahne) sonucu ve oyunda deneme aşağıdaki DURUM notunda.

- **2026-10-31 · Uzman piyonları (S9-B):** Aşama 7 makineleri S9-A'ya geçirmişti, uzman piyonları karton figür kalmıştı
  (eşlemede satırı yoktu). Üç prefab tek seferlik bir script'le değişti (çalıştırıldı, depoya girmedi).
  - **Harf:** `Art/Raid/Analyst/Meshes/PawnLetterT/E/A.asset`, referansın yazı tipi ve ayarlarıyla: three.js'in
    `droid_serif_bold.typeface.json` ana hatları (Droid Serif Bold, Apache 2.0), `TextGeometry` karşılığı size 0,5,
    kalınlık 0,13, 6 eğri parçası, pah 0,012 × 0,01 (2 adım), düz gölgeleme; x ve z'de ortalı, taban çizgisi y = 0.
    Malzeme `PawnWood` (#C9A77A, pürüzlülük 0,75). Altlık ve krem halka eskisi gibi (aynı ölçü).
  - **Durumlar** (`raid.js › pawn()` ile): TUR BİTTİ'de harf #8C734E olur ve Y'de 180° döner, altlığın önünde krem ✓
    etiketi (`DoneSticker`, `TickGreen`); YARALI'da harfin ön yüzünün 1 mm arkasında bant (boşluklarda ve kenarda görünür);
    PLAKALI'da altlığın sağında çelik levha (`RobotSteel`, 0,2 × 0,12 × 0,02; `RaidBoard3D` birimin zırhına göre açar);
    DÜŞTÜ'de harf kameradan uzağa ve sola 1,2 rad yatar, 0,045 yükselir. `RaidPiece`: `spentMark`, `plateMark`/`SetPlate`,
    `downEuler`/`downLift` (öteki parçalarda eski 85°), `spentBlend`; karton figürün boyanmama istisnası kalktı.
  - **İpucu kartı:** üç piyonun ve sekiz makinenin resmi tahtadaki modelden çizildi (Baskin Play'de, tahtanın kamerası
    35°/45°, saydam zemin, 768 px'ten küçültülerek, alttan 2 px boşlukla 80 × 92). Yeni sprite'lar `thumb_armordrone`,
    `thumb_shielddrone`, `thumb_mortar`, `thumb_bomber`, `thumb_locker`, `thumb_replicator`; `thumb_drone`, `thumb_maint`
    ve `thumb_standee_T/E/A` yenilendi. Kartın resim çerçevesi 40 × 46 ve oranı korumuyor: piyon resmi 40 × 56'dan
    40 × 46'ya indi (önceden eziliyordu). `RaidDataSource.EnemyThumb` her türe kendi resmini verir (önceden havadakiler
    dron, yerdekiler bakım robotu). Dolap (`thumb_cabinet_*`) ve soğutma ünitesi resimleri Sade çizimi kaldı.
  - **Silinen:** kartın mesh'i ve malzemeleri (`RoundedCard`, `Card`, `CardBack`, `CardBackInk`, `CardRule`).
  - **Not:** `raid_tokens.json → materials3d.standee` (1.1) hâlâ kartı tarif ediyor; model `board_assets/mdl_pawn*.png`'yi
    ve S9-B'yi izler.
  - Tam EditMode (boş sahne) **361/361**. Baskin'de beş durum referansla yan yana karşılaştırıldı; tahtada hamle ve tur
    sonu (koyulaşma, dönüş, ✓) denendi, konsolda hata yok.

- **2026-10-31 · Aşama 10 (dil, erişilebilirlik, temizlik):** `DeskGame.unity` beş, `RaidHud.prefab` üç, `RaidPanels.prefab` ve
  `HoverInspector.prefab` birer tek seferlik script'le değişti (çalıştırıldı, silindi; her birinin diff'i yalnız beklenen satırlar).
  - **Tam görüntü seti** (`SiginakCaptureSet` yeniden yazıldı; menü “Tam görüntü seti (TR/EN, Play)” ve “Baskın görüntüleri”):
    DeskGame Play'de S1a (V.001, eğitim notu), S1b (V.031, kapıda bekleyen), S2, S3, S4a–c, S5a–b, S7a–d, S8a–b; Baskin tek
    başına S6a–c. Çıktı `screens_unity_rev2/S*.png`, `_EN`, TR koşusunun sonunda `_gray` (54 dosya). `SiginakCapture.Render`
    poz adını `SiginakPose`'tan alır, Rev2 klasörüne yazar. Karşılaştırma §10'da.
  - **Kapı pozu:** `SiginakRoom/Poses/K8` kapıya bakıyor (dönüş 5°, 85° → 4°, 90°); kapı S4a–c'deki gibi kâğıdın sağında, ortaya
    yakın.
  - **Dil:** teleks ön kâğıdında durum satırı tek satıra sığar (`Teleprinter.StatusFit`: %132 → en az %100). EN taşmaları:
    SON TUR damgası yazıya göre genişler (`RaidHudView`), EMRİ İLET'in adı notun yanında küçülür (21 → 15; “LAST TURN” en az
    84 px), neden damgası sonuç damgasının sağ ucuna bağlı (`FinalReportView.reasonOffset`), operasyon raporunun numarası
    iğneden uzak (başlık satırı sağ boşluk 18). “AISA” başlıkları KYGA oldu. Odadaki üzerine gelme etiketleri (`DeskTarget`
    başlık ve satırı) artık çeviri tablosunda (`LocalizationTool`; 13 yeni satır: TELSİZ HATTI, Masaya dön, Jeneratör…);
    ayarların “Kapat”ı `DeskLocText` ile çevriliyor.
  - **Punto taraması** (`SiginakTextScan`): kökler + `SiginakTutorial`, `DeskUI` (son rapor, ayarlar); Baskin sahnesinin
    Overlay canvas'ları ve çalışırken kurulan prefab'lar (telsiz kaydı satırı drawer'ın ölçeğiyle). Rapor
    `punto_taramasi.txt`: **781 metin, 0 ihlal**, 93 gerekçeli istisna. Düzeltilen: baskının eski sade panelleri (kurallar kartı,
    telsiz kaydı, iki onay notu, “tekrar bas” sekmesi) ve üzerine gelme kartı en küçük yazıları 14 px olacak ölçekte (×14/11;
    sekme ×14/13; düzen aynı); üzerine gelme kartı 258 → 330 px, tarif satırları sarar ve kart uzar (`HoverInspectorView`;
    önceden “…” ile kesiliyordu); ayarlar kartı ×14/11 (ortada); eğitim notunun tuş fişleri 11 → 13 (keyChip 13, istisna).
    Yeni test `BaskinScene_NoTextBelowMinimum`; ölü iki istisna satırı silindi.
  - **Gri tonlama:** bütün durumlar sözcük, işaret ya da desenle ayrılıyor; makine türleri biçimden ve etiket işaretinden (§10).
  - **Kare süresi** (Play, V.014; kamera 1920×1080 MSAA 4× hedefe 30 çizim × 7 tur, GPU beklenerek; ms/kare ortanca).
    Revizyon 2 öncesi (`9d16037`) ile Revizyon 2, proje kopyalarında (`git archive` ve çalışma ağacı, batch mod, ayrı kayıt
    klasörü) aynı ölçümle, art arda koşulan üç çiftin ortalama farkı (Revizyon 2 − öncesi; turlar arası kayma ±0,1 ms, Rev2
    öncesi masa pozu 4,2–4,8 ms):
    | Poz | Önce | Sonra | Sonra + sert gölge |
    |---|---|---|---|
    | Masa | +0,10 | +0,08 | −0,25 |
    | Harita | ±0 | +0,08 | −0,25 |
    | Sığınak | +0,19 | +0,14 | −0,15 |
    | CRT | ±0 | ±0 | −0,10 |
    Yapılanlar: ilk turun kuralı Revizyon 2 nesnelerine de uygulandı (6 cm'den küçük 103 parça gölge düşürmez, ekip figürü hariç; kapalı kapının ardındaki koridor ve
    merdiven 24 parça gölge düşürmez): gölge düşüren 293 → 215 (Rev2 öncesi 210). Hareketsiz yeni parçalar statik (63: alarm
    panosu gövdesi ve ölçeği, DEPO duvarları, tahta ve plaka, KURTARILAN BÖLGELER çerçevesi ve başlığı, koridor, kapı kasası ve
    lamba kafesi; sayaç çarkları, ziyaretçi siluetleri ve yazılar hariç). Kalan %2–3 yeni içeriğin kendisi (Rev2'de çalışırken
    +128 renderer: DEPO 42, tebeşir çentiği 15, alarm ölçeği 11, KAPI tuşu 10…). Soru 13: yumuşak gölge kaldı, fark kabul edildi.
  - **Temizlik:** silinenler `CarbonText` (eski olay satırının), `DeskPool` ve `DeskUi.MarkPreview` (eski rapor ve grafiğin),
    `StampView` + `Desk/Prefabs/Components/Stamp.prefab` ve damga tema alanları (`StampInk`, `StampSize`, `StampKey`,
    `StampAngle`, `stampRotationRange`, `stampOpacity`), kullanılmayan `IRaidPreviewSource`. Baskın tahtası sahne kapanırken
    kamera gittiyse çizmez (Play durunca konsola düşen `MissingReferenceException`). Belgelerdeki eskimiş satırlar düzeltildi
    (AGENTS, MIMARI, OYUN-TASARIMI, DURUM; bu haritada §3 tablosunun “Mevcut karşılık” sütunu Aşama 0'ı anlatır). Daha eski
    (UI_C/Sade) kalıntılar ayrı iş: kullanılmayan ~250 sprite ve doku, `Tools/Fx`, `Tools/Ayna`, `TutorialInfo`, ölü yardımcılar.
  - **Testler:** tam EditMode (boş sahne) **361/361**. **Oyunda:** set TR ve EN iki kez çekildi; konsolda hata yok (Play
    durunca düşen tek hata düzeltildi). Kayıtlar ve dinamik font asset'leri geri yüklendi.

- **2026-10-31 · Aşama 9 (son rapor, S8):** `DeskGame.unity` iki tek seferlik script'le değişti (çalıştırıldı, silindi).
  - **Rapor** (yeni `FinalReportView`, `FinalReportBuilder`, `FinalReportData`, `OccupationChart`, `SiginakFinal`;
    `DeskUI/Modal › FinalReport`): Overlay, 1620 × 952 (sol 150 · üst 64), #F1E9D6, kenar #CDBF97, gölge (6, 8) %50;
    harita %62 karartılır (`report.dim`). Başlık satırı “KYGA · HAREKÂT SONU RAPORU · SIĞINAK 07 · V.046 · tek nüsha” ve 3 px
    çift çizgi. Sonuç damgası CP 700 88, 8 px çift kenar (3 + 2 + 3), −4° (KAZANILDI yeşil / KAYBEDİLDİ kırmızı); kayıpta
    neden damgası CP 700 28, 3 px, +3° (DAĞILMA, VURULDU, SIZMA), büyük damganın sağ altına biner. Sağda cümle (LF 600 28) ve
    ikinci satır (kazanınca parola CP 16; kayıpta sonun metni LF 19). Dört gösterge (1,5 px #463F34, #F7F1E1; değer CP 46,
    açıklama LF 17): SÜRE, BASKIN, ÖZGÜR BÖLGE (hepsi özgürse mavi), SIĞINAK NÜFUSU (kayıpta kırmızı, “kişi kaldı”).
  - **İşgal şeridi** (`OccupationChart`, özel Graphic: dörtgen zinciri, shader yok): alan 1480 × 148; zemin geri alınan
    mavi %10, çizginin altı işgal kırmızı %22, çizgi 3 px, çerçeve 1,5 px; renkler temadan (`report.chart*`). x = vardiya
    1…N her zaman tam genişlik (29 ve 48 vardiya testli); etiketler 1, 5'in katları ve N; İŞGAL (kayıpta “· %92”) ve GERİ
    ALINAN. Seri `S.Hist`'ten, tam işgalle başlar, bugünkü oranla biter.
  - **Kırılma anları** (en çok altı, sunumda türetilir): ilk özgür bölge (baskın kaydı), karşı saldırılar (`S.Moments`;
    sektör anın metninden her dilde tanınır: `DeskLoc.In`, `RegionDef.SourceName`), öteki özgür bölgeler, Dağılma'da son
    güven düşüşü (kapı kararı ya da sivil sunucu kıran baskın), son olay (son sunucu düştü / güven 0 / kapı açıldı ·
    ziyaretçi). Grafikte numaralı yuvarlak Ø24 (kaybın son anı kırmızı dolu; aynı vardiyadakiler sola dizilir), altta V.### ve
    olay. İşaretin ya da satırın üzerine gelince satır sarı, işaret 1,2 kat.
  - **Açılış:** kamera harita pozuna 0,6 s (`SiginakFinal`), harita 0,5 s'de kararır, kâğıt aşağıdan 0,4 s, damga 0,15 s
    (kazanınca `Cue.FinalBell`, kaybedince `StampHeavy`; klipsiz), neden damgası 0,3 s sonra, göstergeler 0,12 s arayla
    (sayılar daktiloyla), çizgi 1,2 s'de soldan sağa, işaretler çizgi geldikçe 0,1 s, satırlar 0,08 s arayla. YENİDEN BAŞLA
    (Enter; üzerinde kalkar, basılı iner).
  - **Kaldırılanlar:** eski son rapor (`GameOverView`, `KpiCardView`, `StripChartView`, `TimelineCardView`, `DeskChart`,
    `GameOver.prefab`, sahnede `DeskUI/Modal/GameOver`), `SimDeskBuilder.GameOver` ve `GameOverVM` ailesi (`CommandVM`,
    `HeaderVM`, `StampVM` dahil). `DoorViewBuilder.ChoiceLabel` kapı kararının etiketini paylaşır.
  - **Core:** `RegionDef.SourceName` (salt okunur, çevrilmemiş ad; simülasyonu, RNG'yi ve kayıt biçimini değiştirmez).
  - **Çeviri:** 26 yeni metin (S8'in İngilizce listesiyle); listeye göre İŞGAL ORANI → OCCUPATION, İŞGAL → OCCUPIED,
    KIRILMA ANLARI → TURNING POINTS, Dağılma → Disbanded, Sızma → Infiltrated.
  - **Testler:** `DeskBridgeTests` kazanılan harekât (damga, göstergeler, şerit 1 → N, ilk ve son an), 29/48 vardiyada şerit,
    Dağılma (neden damgası, kırmızı son an, kaybın nedeni), karşı saldırı anı (dil değişince de), sahnede raporun yeri ve
    şeridin tam genişliği; `SiginakDoorTests` Sızma ve Vuruldu'nun son anı. Tam EditMode (boş sahne) **360/360**.
  - **Oyunda:** V.014 fikstüründen Silme (46 vardiya), Dağılma (29), Vuruldu ve Sızma; TR ve EN. Konsolda hata yok. Kayıtlar
    ve dinamik font asset'leri geri yüklendi.

- **2026-10-31 · Aşama 8 (sonuç kartı ve operasyon raporu, S7):** `RaidPanels.prefab` bir, `DeskGame.unity` üç tek seferlik
  script'le değişti (çalıştırıldı, silindi).
  - **Sonuç kartı** (`RaidPanels › ResultMemo › Card`, `RaidPanelsView`): 500 px, x 1290 · y 318, CSS +0,8°; #FBF7EC, kenar
    #D2C7AF, gölge (5, 6) %50. Üstte “OPERASYON SONUCU · TUR 6/8” ve 2 px çizgi; başlık yerinde tek damga (CP 700 34, çift kenar
    2 + 2 + 2 px, −3°): başarıda SEKTÖR GERİ ALINDI mavi, değilse GERİ ÇEKİLDİ / EKİP DÜŞTÜ / İKMAL KAYBEDİLDİ / SÜRE DOLDU
    kırmızı. Yer satırı “Doğu Avrupa · 02 Tahliye geçidi” (`LiveRaidDataSource.ResultPlace`), noktalı çizginin altında
    hedef, kırılan sivil sunucu, ekip (`RaidResultRow`), tek düğme OPERASYON RAPORUNA GEÇ (Enter fişi; üzerinde kalkar).
    Eski göz satırı, alt not ve büyük damga kalktı.
  - **Dizi:** tahta 0,4 s'de kararır, spot 0,1–0,4 s'de nedenin üstünde açılır (alınan kasada Teknisyen, öteki hâllerde
    hedef), kart 0,35–0,6 s'de sağdan kayar, damga 0,12 s (başarıda `Cue.ResultBell`, değilse `StampHeavy`; klipsiz).
    Kart gelene kadar girdi kilitli (`RaidDataSource.Continue`, 0,6 s). Sonuçta tahtanın etiketleri ve bayrakları gizli
    (`RaidBoard3D.LabelsHidden`).
  - **Operasyon raporu** (yeni `OperationReportView`, `OperationReportBuilder`, `SiginakOperation`; `SiginakHud ›
    Operation report`): Overlay, sol 24 · üst 274, 560 px, CSS −0,4°; #F1E9D6, kenar #CDBF97, üstte 3 px çift çizgi, sağ
    üstte iğne Ø17. Künye (TARİH, SONUÇ damgası BAŞARILI yeşil / BAŞARISIZ kırmızı, HEDEF, SÜRE; sağ sütun damga kadar),
    başlık CP 700 36 (SEKTÖR GERİ ALINDI mavi / SEKTÖR İŞGAL ALTINDA kırmızı) ve tek cümle, başarılıda KAZANÇ kutusu (FON,
    MALZEME, ERZAK, KURTULAN; 32 px yeşil, ±0 gri), SONUÇLAR (başarısızda YZ alarmı on hücreli ölçekle), MASAYA DÖN.
  - **Dönüş dizisi:** baskın sahnesi kapanınca kamera CRT'den harita pozuna 0,6 s (karartma yok), rapor 0,4 s'de iner,
    KAZANÇ gözleri 0,12 s arayla, iplik iğneden ekip figürüne 0,25 s (`SiginakMap.SetReportString`, haritanın ipi), figürün
    çevresine kırmızı halka 0,2 s (`modal_marker_ring`, radyal dolgu). Sayaçlar o ana kadar baskından önceki değerde bekler
    (`SiginakOperation.HoldCounters`), sonra döner; başarılıda FON/ERZAK/MALZEME ± fişleri ve sektör işaretinin mavi onayı
    (`LiberationRegionMarks.AnimateRetaken`). Enter/Esc/düğme: tepsiye 0,25 s, kamera masa pozuna 0,6 s, rapor okundu
    (`LiberationDeskView.MarkRaidReportSeen`). Rapor açıkken masa kilitli; oyun bittiyse rapor açılmaz.
  - **Kaldırılanlar:** eski olay modalı (`EventModalView`, `EventDocView`, `EventMemoView`, `EventLineRow`, `EventModal.prefab`,
    sahnede `DeskUI/Modal/EventModal`), `SimDeskBuilder.Report`, `EventVM` ailesi, `"Doc:Close"` komutu,
    `SimDeskBuilder.Build`'in `raidScene` parametresi. `CounterBank.Pin` boş metne fiş asmaz. Vardiya raporunun alarm
    ölçeğinde de kırmızı hücreler düz kenarlı.
  - **Çeviri:** 15 yeni metin (S7'nin İngilizce listesiyle: SECTOR RETAKEN, SECTOR STILL OCCUPIED, BACK TO DESK, GO TO
    OPERATION REPORT…); listeye göre KAZANÇ → GAINS, SONUÇ → RESULT, GERİ ÇEKİLDİ → WITHDRAWN, alındı → taken.
  - **Testler:** `LiberationReportTests` yeni rapora göre yeniden yazıldı (KAZANÇ ve fişler, başarısızda ödül alanı yok,
    aynı söz iki kez geçmez, karşı saldırı satırda ve cümlede tekrar yok, depo sınırı, kayıttan aynı rapor);
    `DeskBridgeTests` güven satırı ve raporun yeri/bağlantıları; `SadeRaidRev2Tests` sonuç kartının yeri, tek damga, satır
    renkleri. Tam EditMode (boş sahne) **356/356**.
  - **Oyunda:** V.014'ten gerçek baskın emri → tahtada sonuç (başarı ve geri çekilme) → OPERASYON RAPORUNA GEÇ → CRT dönüşü
    → harita pozunda rapor (S7a–d) → MASAYA DÖN; TR ve EN. Konsolda hata yok. Kayıtlar ve dinamik font asset'leri geri
    yüklendi.

- **2026-10-31 · Aşama 7 (baskın, S6):** prefab'lar dört tek seferlik script'le değişti (çalıştırıldı, silindi): `RaidHud`
  köşe panelleri, `RaidPanels › EventSlip`, `RaidScreenLabel`, sekiz makine prefab'ı. Kamera değişmedi.
  - **Panel kiti** (`RaidHud.prefab › Corners`): kontrplak şerit (#C9A36B, üst 1 px #DDBF8A, alt 4 px #93703F, gölge 3, 4),
    üstünde ozalit pafta (#D3DBD6 + 24 px mavi ızgara, 1 px kenar) ve köşede bant. Beş grup S6a'nın yerinde: sol üst
    Status (20, 16 · 592 px), sağ üst Buttons (548), sağ alt Command (474), sol alt Ability card (432) ve Team (736; üç kart
    237 × 84). Hiçbiri tahtanın kenarına değmez (testli).
  - **Tur şeridi ve evre:** “TUR 3/6” (CP 30) + 34 px hücreler (şimdiki mürekkep dolu, geçmiş #B9C3BE, son iki tur 2 px kesik
    kırmızı + SON 2 TUR). Son turda hücre kırmızı dolu, şeride SON TUR damgası 0,12 s'de iner, EMRİ İLET'in sağında “SON TUR”.
    Evre çipi SIRA SENDE (yeşil daire) · MAKİNELER (kırmızı baklava) · OPERASYON BİTTİ (kare).
  - **Görev çipi:** GÖREV · İKMAL + bölge özelliği çipi (işaret + DAR KORİDOR); hedef çizimi, “İkmal kasası · G4” (kare
    `RaidBoard3D.CellName`), amaç satırı, CAN pipleri (kırılan hedefe damga); GÜVEN + SİVİL SUNUCU simgeleri.
  - **Düğmeler:** TELSİZ [L] (okunmamış sayısı kırmızı dairede, kayıt açılınca sıfırlanır) · KURALLAR [?] · GERİ ÇEKİL
    (kırmızı); Geri al [Z] + EMRİ İLET [Enter] (üzerinde 2 px kalkar, basılı iner). Makine evresinde düğmeler soluk, EMRİ
    İLET'in yerinde kilitli kâğıt (#5E584E): “MAKİNELER HAREKET EDİYOR · girdi kilitli · 2/4 hamle”.
  - **Ekip kartları** (`RaidTeamCardView` yeniden yazıldı): kısayol fişi, disk Ø44 (hakkı bitince #8C734E), ad, pipler
    (kayıp pip kırmızı kenarlı boş), YARALI / plaka kalkanı + PLAKA, hak satırı HAREKET + YETENEK (yeşil) · ✓ TUR BİTTİ ·
    DÜŞTÜ · SIĞINAKTA YARALI · **KİLİT TEHDİDİ · #n** (kilit niyetinin hedef karesindeki uzman, `RaidTeamModel.Threat`).
    Seçili #EFE3A2 + 3 px #F5D646, 6 px yukarı; üzerinde 2 px. Tokenlar `panel.teamCard` (UYGULAMA EKİ).
  - **Yetenek kartı** (`UnitCardView` yeniden yazıldı): disk + ad + pipler; 44 px satırlar (Hareket “3 kare”, yetenek + Q fişi
    + menzil, plaka ya da yedek); alt düğmeler görev eylemi yeşil, robota vuran kırmızı, hedef yokken kesik kenar. Adlar
    S6'nın (Soru 7 ara cevabı).
  - **Telsiz fişi** (`RaidPanels › EventSlip`): teleks kâğıdı (26, 262) 440 × 64, anten + “TELSİZ · K-07”; telsiz kurgusu
    bugünkü ölçüde. Eğitim notu Aşama 2'den beri S6'nın yerinde (sağ üstten −24, −796; 336 px), değişmedi.
  - **Dünya etiketleri** (`RaidScreenLabel`, `RaidWorldLabel`): yükseklik 24, pip 11 (aralık 3), kâğıt #FBF7EC, kenar 1 px
    #463F34. Robotta sıra tür işareti (18 px, `RaidPieceLibrary.Glyph`) → zırh (`icon_armor`) → pipler; uzman etiketi
    altlığın önünde, plaka piplerden sonra. Sıra bayrağı 30 × 28, rakam 20 px, 34 px iğne; yürütülen niyette kırmızı dolu,
    biteni soluk ve rakamı çizik. **UYANDI:** uyanmadan sonraki tur robotun tepesinde, bayrağın hemen altında; bayrakla
    birlikte yerleşir, 0,12 s damga (`RaidScreenLabel.Stamp`). Uyanan robot aynı adımda planlanıp yürüyebildiği için kareyle
    değil kimlikle bulunur.
  - **Niyetler** (`RaidBoard3D.DrawIntent`): ateş 3,4 px düz; yakın dövüş ve kilit 4,6 px yay (20 px), kilitte kıskaç
    rozeti; onarım 2,6 px kesik + anahtar; havan yüksek yay (kirişin 0,4'ü, en az 90 px); kalkan çift şerit, ucu yok, kalkan
    rozeti; kopyalama kesik + dron rozeti; patlama tarama + dört kısa çıkış oku. Zarar verenler #B8241A, kalkan ve
    kopyalama #854005; altta siyah %28 gölge. Oklar `Pencil.mat`'ta (ZTest Always): her zaman üstte.
  - **Makineler (S9-A):** sekiz prefab `raid.js`'in biçimleriyle yeniden kuruldu (dron, bakım, kilitleyici, havan, kalkan
    dronu, zırhlı dron, patlayan, çoğaltıcı). Gövde ortak malzeme; kırmızı yalnız gözde ve taban halkasında (kalkanın kenarı
    hariç), zırh çelik #9A9C98, kilitleyicide sarı uyarı şeridi. Yeni malzemeler `Art/Raid/Analyst/Materials/Robot{South,
    Hull,Metal,Steel,Track,Wheel,Warn}`, meshler `Art/Raid/Analyst/Meshes/` (`torus_*`, `prism_8`, `arcwall_*`). Gri tonlamada
    beş tür biçimden ayırt ediliyor.
  - **Kütüphane:** `RaidPieceLibrary` sekiz tür işareti, `armorIcon`, `badgeClamp/Wrench/Shield/Clone`, `intent`,
    `intentQuiet`; `Glyph(type)`.
  - **Silinenler:** `RaidTeamCard.prefab`, `UnitCard.prefab`; kartlar `RaidHud.prefab`'ın içinde yeniden kuruldu, ikisine de
    hiçbir yerden bağ yoktu.
  - **Çeviri:** 24 yeni metin; EMRİ İLET'in İngilizcesi S6 listesine göre SEND ORDERS (eskiden TRANSMIT), onay notu
    “Enter send anyway”. UYANDI → WOKE UP, KİLİT TEHDİDİ · #n → LOCK THREAT · #n.
  - **Testler:** yeni `SadeRaidRev2Tests` (son tur, makine evresi, kilit tehdidi; panellerin köşeleri; HUD yazıları ≥ 14 px,
    etiket ölçüleri; her makine türünün işareti), `SadeRaidStateTests` GÜVEN. Tam EditMode (boş sahne) **354/354**.
  - **Oyunda:** S6a TR/EN, S6b (telsiz fişi, ✓ TUR BİTTİ, kilitli kâğıt), S6c (6/6 kırmızı, SON TUR damgası, EMRİ İLET'te
    SON TUR), UYANDI (bakım robotu ve dron; damga ortasında 1,37 kat, sonra 1), kilit tehdidi TR/EN, makine dizisi gri
    tonlamada. Play'den çıkarken görülen eski NullReference (etiket yok edildikten sonra `Sync`) `RaidWorldLabel.Show`'da
    kapatıldı. Kayıtlar ve dinamik font asset'leri geri yüklendi.

- **2026-10-31 · Aşama 6 (vardiya raporu ve karşı saldırı, S5):** sahne iki tek seferlik script'le değişti (çalıştırıldı,
  silindi): `SiginakHud/Shift report` S5a/S5b ölçüleriyle yeniden kuruldu.
  - **Rapor** (`ShiftReportView` yeniden yazıldı; Overlay sol 24 · üst 274, 540 px, CSS −0,5°): teleks kâğıdı #EFE4BC, kenar
    #C9B98A, sol kenarda delik şeridi, gölge (4, 5); TELEKS · KYGA-07 → KOMUTA … kod, VARDİYA RAPORU · 014 (CP 30), “014 → 015
    · bir gün ilerledi” + KAPANDI damgası, 2 px kırmızı çizgi. Özet kutusu (#F7F1E1, 1,5 px #463F34; gözler 1,15 : 1 : 0,85):
    ERZAK (değer/sınır CP 32, “n vardiya yeter” S3'ün `ShiftsLeft` hesabıyla), YZ ALARMI (değer/eşik, 10 × 11 px ölçek: dolu
    mürekkep, 8–10 boşken kesik kırmızı, doluyken kırmızı; “4 → 5 · 10'da karşı saldırı”), FON (önce → sonra). Altında alarm
    notu. Döküm: FON · GELİR / GİDER, ÜRETİM VE TÜKETİM (erzak toplamı satırı kalktı, özette), analist notu (Kalam 20,
    #1A2F5A, sağa, −2°), EKİP (satır + sağda damga: HAZIR yeşil, öteki mürekkep; YZ alarmı satırı kalktı), YAPILAMADI.
    54 px BAŞLAT düğmesi (üzerinde 2 px kalkar, gölge 5; basılı 2 px iner) ve [Esc] satırı; tepside [←] [Esc] … [→].
  - **Hareket:** kâğıt yukarıdan iner (`motion.reportDropS`), başlık 0,03 s/harf daktilo, gözler sırayla
    (`motion.summaryStaggerS`), karşı saldırı kutusu damga gibi (`motion.fatalStampS`, ağır “tak”); yalnız taze raporda —
    tepsiden açılan rapor canlandırmasız. Dosyalama `motion.reportFileS`.
  - **Renk kuralı** (`ReportTones.ColorKey`, `ShiftReportBuilder.ToneColor`): + yeşil (`report.positive`), − mürekkep
    (`report.negative`, U+2212), ±0 gri (`report.neutral`), kırmızı yalnız uyarı (`report.counterAttackLine`: alarm eşiği,
    işgal). ± fişleri aynı kuralla. Alarmın artısı yeşil değil (düz mürekkep).
  - **Karşı saldırı** (S5b): dizinin teleks adımında duvardaki alarm lambası üç kez yanar (`AlarmPanel.Flash`, `Cue.AlarmHum`
    klipsiz); rapordan önce teleks “YZ KARŞI SALDIRISI” basar, kodu raporun kutusuna yazılır (`ShiftReportInput.counterCode`,
    tepsideki raporda da). Kutu: 6 px çift çizgi #9A1E14, zemin #F4EBCB, ✕ 34 px, “YZ KARŞI SALDIRISI” (CP 24) + kod,
    “Doğu Avrupa · 01 Tedarik deposu yeniden işgal altında.” Alarm gözü “10/10”, “9 → 10 · EŞİK” kırmızı, ölçek dolu.
    Kutu basılınca haritada sektöre ✕ 0,2 s'de çizilir, YENİDEN İŞGAL damgası 0,12 s'de iner
    (`LiberationRegionMarks.AnimateCounterattack`); kutunun üzerine gelince ✕ iki kez yanıp söner. Bölge özgürlüğünü
    yitirdiyse kurtarılan bölgeler panosundaki kartı raptiyesinden döner ve düşer (`FreedRegionsBoard`).
  - **Üzerine gelme:** ERZAK ve FON gözünde ilgili sayaç modülü (ve fişi) kalkar (`CounterModule.Raise`); alarm gözünde
    etiket “duvarda: YZ ALARMI”.
  - **Testler:** `ToneRule_PlusGreen_MinusInk_ZeroGrey_RedOnlyForThreshold` (işaret ve renk kuralı, raporun satırları ve
    fişleri); `Report_V014_MatchesK6b` özet gözleri ve tekrarın kalkması; karşı saldırı testi kutu, eşik ve ölçek; sahne
    testi kâğıdın yerini, gözleri, ölçeği ve kutuyu denetler. Tam EditMode (boş sahne) **350/350**.
  - **Oyunda:** V.014 TR/EN normal rapor (S5a) ve karşı saldırı (S5b: kod 015/01, haritada ✕ + YENİDEN İŞGAL, teleks ön
    kâğıdı), kutunun üzerine gelme, tepsiden okuma; konsolda hata yok. Kayıtlar ve dinamik font asset'leri geri yüklendi.

- **2026-10-31 · Aşama 5 (kapı, S4):** sahne altı tek seferlik script'le değişti (çalıştırıldı, silindi); yeni malzemeler
  `Art/Siginak/Materials` (`Door leaf`, `Door wheel`, `Knock lamp amber/red`, `Intercom`, `Intercom led on/off`,
  `Stair tile floor/side/end`, `Stair step`, `Stair edge`, `Visitor *`), yeni mesh'ler `Art/Siginak/Meshes` (`torus_*`,
  `frustum_0.68_24`), yeni doku `tx_stair_tiles` (`SiginakTextureBaker.StairTiles`, kendi tohumuyla; manifestte üretilen).
  - **Kâğıt** (`DoorCardView` yeniden yazıldı, Overlay sol 40 · üst 64, 470 px, CSS −0,8° / ölümcülde −1,4°): KAPI ·
    SIĞINAK 07 … VARDİYA 014 (CP 14, #675F50), ziyaretçi (CP 28) + 2 px #D2483F; kapıdan duyulan; gözlem blokları (göz
    işareti + GÖZETLEME DELİĞİ / İNTERKOM; #EAE0C2, kesik #A8997A); gözetleme 0,25 s'de dolar, interkom cızırtıdan 0,2 s
    sonra 0,03 s/harf yazılır. Seçenek satırı 50 px (yazı sararsa uzar): normal #FBF7EC + 2 px gölge; üzerinde mürekkep
    zemin, 2 px kalkar, fiş pirinç, sağdaki bedel pirinç; basılı 1 px iner; devre dışı ve kullanılmış kesik kenar, gri,
    neden sağda küçük harfle (“revirde görevli yok”); kullanılmış satırda BAKILDI / SORULDU damgası (0,1 s). Sağ sütunda
    bedel ve açık kapıda güvene etkisi (“−8 erzak”, “güven −1”); yetersiz kaynakta satır açık, eksik kalem çizik + “erzak
    yetersiz”, basınca komut denenir (teleks YAPILAMADI) ve kâğıt 0,1 s titrer. Esc satırı. Sonuçta “SEÇİM · n …”, metin
    ve etki, tek geniş Enter düğmesi (KAPIYI KAPAT); ölümcülde çift çizgili damga (CP 66, `ui_frame_double_6px`, −8°),
    son cümle, düğme damgadan 0,6 s sonra (SON RAPORA GEÇ). Kâğıt 0,22 s'de gelir, 0,18 s'de çekilir. Renkler
    `colors.doorPaper` (UYGULAMA EKİ).
  - **Veri** (`DoorViewBuilder`, `DoorCardModel`): `DoorButtonData.State` (normal / devre dışı / yetersiz) + `Used`;
    `DoorObservation`; `DoorCardData.Choice`, `Fatal`, `Epilogue`, `ActionId/Action`, `HintKey/Hint`. Kart → siluet
    tablosu (§4) ve ziyarete bağlı çeşitleme (`Variation`: boy 0,96–1,04, aynalama; gün ve karttan, RNG'ye dokunmaz).
  - **Kapı** (`DoorProp`): tehlike bandı 0,96 × 0,11; kafesli vuruş lambası Ø 0,08 (üç meridyen + kenar halkası), amber
    #FFB347 (ziyaretçi beklerken sürekli, üç vuruşta 0,08 s parlar), ölümcülde #F0443A; pirinç gözetleme halkası, koyu
    mercek, 0,07 × 0,05 kapak 0,15 s'de kayar; interkom bakalit, cevap yazılırken amber nokta; kanat #3A3F33, çark #A3241A,
    şablon #D8D2BE. Açılış: çark 0,5 s → kanat 0,8 s koridora (95°; referans ve `layout3d.door.leafOpenDeg`), merdiven
    ışıkları 0,3 s'de; kapanış kanat 0,6 s → çark. Kapının solunda tebeşir ziyaretçi çentiği (`ChalkTally`,
    `S.Door.Log.Count`).
  - **Merdiven boşluğu:** 2,4 × 1,6 × 2,6 m, fayans (#DDE3DD, 4/m), solda yedi basamak (koyu ön kenar #5A5A52), tırabzan,
    iki tavan lambası #F4F1E6 (kapı kapalıyken kapalı).
  - **Siluetler** (`VisitorSilhouette`, S4d): anne, yaşlı, silahlı, asker, yaralı, kopya ve üç genel biçim; `door.js
    → visitor()` ölçüleriyle (kapsül, küre, silindir; etek kesik koni; miğfer kenarı ve askı torus). Kumaş #121212 mat,
    kopya #1A1C20 metalik 0,6 + dikiş. Üç yuva (kalabalık); yalnız kanat aralıkken görünür.
  - **Ölümcül varyant:** kapı açılınca 0,9 s sonra atış (VURULDU; SIZMA'da sessiz), tavan ampulü ve banker lambası
    `door.fatalRoomLight`, lamba kırmızı, kâğıt sarsılır, damga iner (`Cue.StampHeavy`); yalnız Enter / SON RAPORA GEÇ
    (Esc ve rakamlar kapalı); kenar satırının tuş kutuları kesik, çipler %45 (`EdgeLine.SetEnded`). Son rapor düğmeye
    basılınca açılır; oda ışığı geri döner.
  - **Sesler:** `DoorPeep`, `DoorClose`, `StampHeavy` (klipsiz, sessiz).
  - **Soru 5** ve **Soru 10** ara cevaplarla uygulandı (§8).
  - **Testler:** `SiginakDoorTests` yeni kâğıda göre güncellendi (gözlem blokları, BAKILDI/SORULDU, yetersiz kaynak,
    güven etkisi, seçim satırı, ölümcül); yeni `EveryCard_HasASilhouette_AndNobodyCardsShowNone`,
    `SilhouetteVariation_IsSmallAndStablePerVisit`; sahne testi kâğıdın yerini, satır yuvalarını, lamba / merdiven /
    siluet bağlarını ve kenar satırını denetler. Tam EditMode (boş sahne) **349/349**.
  - **Oyunda:** V.014 TR/EN; vuruş, bakıldı, interkom, açık kapı, yetersiz erzak (teleks + titreme), sonuç, VURULDU, SIZMA →
    son rapor, dokuz siluet dizilimi ve aynalı çeşitleme; kapı kapanınca siluet yok. Kayıtlar ve dinamik font asset'leri
    geri yüklendi.

- **2026-10-31 · Aşama 4 (sığınak, S3):** sahne beş tek seferlik script'le değişti (çalıştırıldı, silindi); yeni
  malzemeler `Art/Siginak/Materials` (`Depot *`, `Idle plate brass`, `Radio dial on/off`, `Mold glow on/off`).
  - **Pano:** raf yereli (−0,46 · 0,86), 0,40 × 0,60 gövde, 0,37 × 0,54 kâğıt, altında ahşap ayak (0,36 × 0,08); BOŞTA
    tepsisi (−0,46 · 0,78 · −0,155), pirinç levha. Kâğıt World Space 400 × 594, S3-TR panosunun ölçüleriyle yeniden
    kuruldu (`ShelterClipboard` yeniden yazıldı): SIĞINAK ÖZETİ · V.014; asıl soru **ERZAK YETER · n VARDİYA** (CP 40;
    3 ve altı kırmızı + AZ KALDI, açlıkta AÇLIK; artıyorsa ERZAK · ARTIYOR); TOPLULUK · UZMANLAR (diskler, yatan uzman soluk
    disk + gün) · PLAKA; seçili oda: ad + SEÇİLİ, satır, açıklama, analistin Kalam notu (#1A2F5A), personel adımlayıcısı
    [−] 2/6 [+] (52 × 46, üzerinde kalkar, basılı iner, devre dışında kesik kenar; üzerine gelince “Oda dolu” / “Görevli
    yok” / “Boşta kimse yok”, boşta kimse yokken altında not), GELİŞTİRME (en üst seviyede yeşil damga; değilse mürekkep
    düğme “GELİŞTİR · 20 fon + 14 malzeme”, yetersiz kaynakta açık ve eksik kalem çizik + “yetersiz”, kilitliyse devre dışı
    ve altında gerekçe). Sayfa değişimi 0,15 s.
  - **Hesap** (`ShelterViewBuilder`, EditMode testli): `ShiftsLeft` = ⌊erzak ÷ |fark|⌋, fark ≥ 0 → null; ilk vardiyada
    fark beklenen tüketim (görevli × toplama − nüfus × tüketim). `Store`: 10 sandık = erzak sınırı (son sandık yarım,
    gölge yuvalar), 5 kova = malzeme deposu (son kova kısmi), plaka rafı 3 yuva (atölye seviyesi kadar açık).
  - **DEPO bandı** (`ShelterStore`): maketin taban 0,49 – tavan 0,59 arası; sandık, yarım sandık (iki kutu), %22 gölge,
    galvaniz kova, çelik plaka rafı, arka duvarda tebeşir tahtası (“ERZAK · 15 VARDİYA”), pirinç DEPO levhası, sıcak
    lamba. Vardiya sonunda değişen sandık ve kova 0,25 s'de belirir/kalkar, tebeşir 0,3 s'de silinip yazılır. Üst kat
    yeniden yerleşti (çerçeve 0,70 m; toprak, moloz, yıkıntılar ve YZ direği bandın üstünde, üst rafa değmez).
  - **Oda çıktıları** (`RoomView`: `activeObjects`, `activeLights` + on/off malzeme, `occupants`): yaşam alanında alt
    ranzalarda yatan yedek uzmanlar ve görevli varken erzak kasası; telsiz odasında görevli varken kadran ve tavandaki
    amber lamba; atölyede tezgâhta döküm kalıbı (plaka dökülürken akkor); revirde yatakta sargılı hasta (yatan uzman varken).
  - **Telsiz odası etkin** (öneri): alarm panosunun altında pirinç fiş “TELSİZ ODASI ETKİN”, telsizde görevli varken
    (`AlarmPanel.SetRadioActive`).
  - **Sesler:** oda seçiminde `PaperSlide`, piyon odaya / tepsiye `PawnPlace`, geliştirmede `Hammer` (klipsiz, sessiz).
  - **Soru 6** (a) ile uygulandı (§8).
  - **Testler:** `ShiftsLeft_FloorsAndFlagsLowAndRising`, `Store_CountsCratesBinsAndPlates`, `Board_Stepper_NoIdleAndNoStaff`,
    `Rooms_ShowOutputs`; sahne testi panonun, DEPO'nun ve oda çıktılarının bağlarını denetler. Tam EditMode (boş sahne)
    **347/347**.
  - **Oyunda:** V.001 ve V.014 (TR/EN), boşta kimse yok, telsizde görevli, revir, masa pozu; konsolda hata yok. Kayıtlar
    ve dinamik font asset'leri geri yüklendi.

- **2026-10-31 · Aşama 3 (harita ve görev fişi, S2):** sahne sekiz tek seferlik script'le değişti (çalıştırıldı, silindi);
  sahne farkında yalnız harita katmanı ve fiş (temizleyici: Aşama 2'nin önekleri + iki yeni yol).
  - **Harita** (`LiberationRegionMarks` yeniden yazıldı): etiket yeri ve boyu `map2.js → LAB` (34–48 px, CP 700, %4
    aralık); ad bölgeye göre bir eşikten geniş olunca boşluktan ya da tireden iki satır (`maxLineWidth`: TR'de GÜNEY
    AMERİKA, DOĞU AVRUPA, JAPONYA-KORE, GÜNEYDOĞU ASYA; EN'de ayrıca WESTERN EUROPE). JAPONYA-KORE denizde, 2,4 px grafit
    çizgi + uç noktası. Yağlı kalem ve aday halkaları kalktı: cephe koyu kontur, aday kesik kontur + kesik ADAY damgası.
    Cephe bloğu: 38 px sektör şeridi (adın altında; Doğu Avrupa ve Hindistan'da sağında) + yüzde (CP 700 32) + kâğıt
    zeminde saha işaretleri (bölge özelliği mürekkep, imza makinesi kırmızı; `_frame` sprite'ları `SiginakGlyphs`'ten) +
    raptiye adın solunda. İşgalde “%100” (CP 700 28, ink3). Özgürde ✓✓✓ (36 px) ve ÖZGÜR damgası `STAMP` yerinde, taze
    damga 1,1 kat. Yeniden işgal: karşı saldırının aldığı sektör geri alınana kadar şeritte ve fişte `mark_sector_reocc`,
    bloğun altında kırmızı YENİDEN İŞGAL damgası (`MapRegionState.Reoccupied`). K3 dizisinde adaylara ADAY damgası iner,
    yeni cepheye raptiye düşer.
  - **İşaretler kutusu** sağ altta (1030, 904), başlıkta “TEHDİT n/4” (`LiberationMapState.Tier`); cephe düz, aday kesik
    çerçeve. EN başlık KEY (S2-EN). S-07 (600, 268). Ekip noktaları `LAB.team` (dördü karaya kaydı, §10).
  - **Görev fişi** ekran katmanına (`SiginakHud`, iç içe Overlay canvas + kendi ışın atıcısı) taşındı ve S2-TR panosunun
    ölçüleriyle yeniden kuruldu: sol 22 / üst 236, 432 px, +0,6° (CSS −0,6°), Kuzey/Güney Amerika seçiliyse sağda (Karar 1
    B). Başlık (kod CP 14 · Esc kutusu, üzerinde çerçeve koyulaşır · bölge CP 30 · ilerleme CP 16 · 2 px kırmızı çizgi),
    SAHA (işaret 30 · etiket 92 · değer LF 600 18; tehdit `threat_tier_n` + kırmızı “1/4”; üzerinde kâğıt etiket
    “İMZA · KİLİTLEYİCİ” + oyundaki açıklama), sektör satırları 52 px (seçili sarı %34 + ok ucu, üzerinde %18, kilitli
    satırda “Önceki sektör alınınca açılır.”), damga sütunu 138 px (−4/−3/−2°), görev, BEDEL / ÖDÜL 2 × 2 (yeşil CP
    miktar + LF birim), ekip diskleri (`ui_team_disc`, üretilen 44 px sprite), BASKIN EMRİ VER beş durumu.
  - **Yetersiz kaynak (Karar 7):** `TelexReasons.RaidFundsOnly` (eksik yalnız fon: sıra, hazır ekip, Teknisyen ya da
    yedeği tamam; önceki özel yardımcı Teknisyen'i denetlemiyordu). Fişte bedel çizik, altında “Fon yetersiz · 12 gerekli”;
    basınca komut çağrılmaz, teleks gerekçeyi basar, `CounterModule.Shake(2)`. Devre dışında Enter fişi kalkar, sağda kısa
    neden (EKİP HAZIR DEĞİL · TEKNİSYEN YARALI · SAHADA); uzun gerekçe düğmenin üzerine gelince etikette.
  - **Hareket:** kamera haritaya varınca iğne düşer (0,12 s, `Cue.PinDrop`) → iplik bölgeden fişin iğnesine çekilir
    (0,25 s) → fiş kenardan kayar (0,22 s, `Cue.PaperSlide`). Kapanış: iplik 0,15 s'de sarılır, fiş 0,18 s'de çekilir.
    Emir: düğme 0,06 s iner, fiş kapanır, CRT'ye dalış bugünkü gibi; EMİR VERİLDİ damgası kalktı. İp fişin iğnesinin ekran
    noktasından kamerayla her karede haritaya izdüşer; sarkma 30–40 px. Yeni sesler (`PinDrop`, `PaperSlide`, `Pencil`)
    `SoundBank`'ta klipsiz, sessiz.
  - **Üzerine gelme:** `IHoverSource` (`DeskTarget` ve yeni `UiHoverTarget`); `HoverTag` ikisini de gösterir.
  - **Fikstürler:** V.031 (beş bölge özgür, cephe Hindistan 1/3, ekip hazır) ve V.015 karşı saldırı (`V015ca`).
  - **Tokenlar:** `colors.slip` uygulama eki (`buttonDisabled`, `buttonDisabledText`, `buttonKey`, `rule`, `edge`);
    `SiginakTheme` yeniden içe aktarıldı.
  - **Testler:** fiş içeriği (SAHA, ödül, diskler, üç düğme durumu), karşı saldırı işareti, V.031 fikstürü, sahne bağları
    (fiş ekran katmanında, her alan bağlı, bölge işaretleri). Tam EditMode (boş sahne) **343/343**.
  - **Oyunda:** V.001, V.014 (TR/EN), yetersiz fon (komut çağrılmadı, teleks, FON titredi), sağdaki bilgi fişi (Kuzey
    Amerika), V.015 karşı saldırı, V.031 (TR/EN). Kayıtlar ve dinamik font asset'leri geri yüklendi.

- **2026-10-30 · 1.2 eki (eksik işaretler, makineler, niyetler; `EK_1_2.md`):**
  - **Paket klasörü:** ekin zip'i `sprites/`, `reference/`, `spec/` alt klasörlerini birleştirmek yerine yerine koymuştu
    (Finder "Değiştir"); paketin 46 sprite'ı, S1–S8 referansları ve `tokens_siginak.json` silinmişti.
    `~/Downloads/UI_Siginak_Rev2.zip`'ten yalnız eksik dosyalar geri kondu (`rsync --ignore-existing`); ekin üstüne
    yazdığı sekiz dosya 1.2 hâlinde kaldı. Klasör git'te değil.
  - **Sprite'lar:** manifeste 28 girdi (13 işaret × 2, `badge_shield`, `badge_clone`). README notu gereği bütün `glyph_*`
    (eskiler dahil) Generate Mip Maps açık, Trilinear (`Entry.Mip`, `Entry.Filter`); rozetler ve öteki sprite'lar
    değişmedi. "Sprite'ları içe aktar" 112 dosya kopyaladı; Rev2 klasöründe 71 sprite.
  - **Tokenlar:** `raid_tokens.json` 1.2 ile değişti (1.1'in hiçbir değeri değişmedi, betikle karşılaştırıldı).
    `raid_tokens.v1_1.json` yazılmadı (§8 Soru 1: git yeterli). `RaidTheme` yeniden içe aktarıldı: 152 renk, 506 sayı.
  - **Eşleme:** `SiginakGlyphs` (oyundaki kimlik → token anahtarı → sprite) ve `DeskSprites` kaydına 40 `glyph_*`
    sprite'ı (`DeskSpriteRegistry`); tablo §5'te. Henüz hiçbir görünüm kullanmıyor: harita ve fiş Aşama 3'te, can etiketi
    Aşama 7'de bağlanır.
  - **Aşama 7'ye kalanlar** (§3.7): üç makinenin 3D biçimi, dört niyetin çizimi ve `marks.ghostWake`, etiketteki zırh
    simgesi, S9-C2'nin dört fikstürü.
  - **Testler:** `RaidTokens_Rev2Addendum12` (21 yeni yolun temada karşılığı, örnek değerler, zarar vermeyen niyetler
    #854005), `Glyphs_CoverGameTraitsAndMachines`; sprite testi mipmap ve filtreyi, kayıt testi 40 işareti denetler.
    Tam EditMode (boş sahne) **339/339**.
  - **Oyunda:** V.001 açılışı hatasız; görünür değişiklik yok (işaretler henüz bağlı değil). Kayıtlar ve dinamik font
    asset'leri geri yüklendi.

- **2026-10-30 · Aşama 2 (başlangıç pozu, S1):** sahne dört tek seferlik script'le değişti (çalıştırıldı, silindi);
  Unity'nin kayıtta düzen gruplarına yazdığı çapa/boyut değerleri her seferinde ayıklandı (yalnız dokunulan nesneler kaldı).
  - **Sayaç rafı:** FON'da çarklar arası 15 mm, pirinç virgül (`Decimal comma`, `Counter comma.mat`), açık renk onda bir
    çarkı (`Counter wheel tenths.mat`); EN'de virgül yerine kuyruksuz pirinç nokta (`CounterModule.decimalComma/Point`,
    dil değişince). ± fişleri 70 × 30 → 60 × 23 mm (72 × 28 px); artı yeşil, eksi **mürekkep** (canlı fiş ve vardiya
    raporunun iğnelediği fişler). Yazının puntosu korundu (küçültülünce punto taraması 11 px ölçtü; sınır 14). Modül aralığı
    0,26 m kaldı (Rev2'nin 0,257'si; fark ~3 mm).
  - **Anahtar rayı:** 1,40 m, 6 tuş (aralık 0,226, plaka 0,128 / Home 0,10, plaka yazısı sığmazsa %70'e kadar küçülür).
    `Key 4` · KAPI (`StationKeyRail.Door = 3`; Guide 4, Desk 5). `StationKey` dikkat durumu: `jewelDoor.mat` (emissive
    ×3,4/2,4) 1 Hz yanıp söner, amber halka ve masada sarı el yazısı fiş “kapıda biri bekliyor” (Kalam), dikkat başlarken
    ve kapıdan masaya dönünce fiş 0,2 s sallanır. `SiginakDesk.DoorKey`: ziyaretçi beklerken dikkat + etiket “KAPI ·
    ZİYARETÇİ VAR / Kapıya dön…”, baskında neden, boşta “Kapıda kimse yok…”. 4 (ve K) `SiginakDoor.Visit`: bekleyenin
    kâğıdı açılır, kimse yoksa kamera kapıya bakar. `DeskTarget.TitleOverride` eklendi. Teleks “4 ile kapıya bak”.
  - **Kenar satırı:** `Chip 4` KAPI (KILAVUZ'dan önce; dizi sırası raydakiyle aynı). Yeni fişle değerler fişlerin altına
    taşıyordu: ayırıcılar tek boşluk (“VARDİYA 001 · FON 60,0 · …”), fiş aralığı 9 → 6, fiş iç payı 6/8 → 4/5; TR ve EN tek satır.
  - **YZ ALARMI panosu:** arka plaka aşağı uzadı, sayacın altında 10 hücre (dolu 1–7 krem, 8–10 kırmızı; boş 8–10
    `cell_dashed` kesik kırmızı) ve “10'DA KARŞI SALDIRI”; lamba ≥ 8 (`warnSteps` 3 → 2), 9'da yanıp söner. Pano yeri
    değişmedi (x 0,06; Rev2'de 0,15).
  - **KURTARILAN BÖLGELER** (`Shell/Freed board`, `FreedRegionsBoard`): 0,82 × 0,58 çerçeve, 0,78 × 0,54 mantar, başlık
    kartı (sayı “00” + “/12”), 12 yuva: özgür bölge kartı (ad iki satır, mavi ÖZGÜR damgası + V.###, mavi raptiye), sıradaki
    hedefte kurşun kalem kartı (cephe bölgesi + “?”, kırmızı raptiye), boşta ahşap raptiye. Yeni kart 0,35 s'de üstten düşer.
    Eski pano, liste ve **KAYIP ilanları** silindi (referansta yok; Soru 9 cevabı gelene kadar). `SiginakDesk.freedEntries`
    → `freedBoard`.
  - **Tebeşir çentiği** (`Shell/Chalk tally`, `ChalkTally`): vardiya başına çizgi, beşli çapraz; grup sığmazsa sıklaşır;
    “vardiya” etiketi.
  - **Teleks:** makinenin önünde asılı ön kâğıt 0,26 × 0,19 (kod, büyük durum, ileti; altında önceki satır), krom klips,
    ışıktan bağımsız kâğıt (`Telex front sheet.mat`, URP Unlit #CFC6A6: makinenin ışık almayan yüzünde ışıklı kâğıt ve ×0,35
    ışıma okunmuyordu). Yeni iletide eski ön kâğıdın
    kopyası 0,4 s'de koparılıp düşer. Alt teleks şeridi ön kâğıt okunuyorsa çıkmaz (`IsReadableFrom` ön kâğıdı ölçer).
  - **CRT:** boşta dört satır — ters video durum (128), ekip (94), kısa cephe “BATI AVR. / 01” (89, `CrtViewBuilder.ShortRegion`),
    YZ ALARMI (55); sığmayan satır orantılı küçülür. Kanal satırı, imleç ve test deseni kalktı; ipucu yalnız hat kapalıyken.
  - **Oda kartları:** 0,23 × 0,057 m, iki satır: ad 40 (en az 28) + “SV 1 · 2/2” 32 (EN “LV”); damga sağ üst köşede; pirinç
    tutucu 0,20 → 0,24 m (eski yan çubuklar ilk harfi örtüyordu).
  - **Harekât izleri** (`Campaign wear`, `CampaignWear`, `SiginakSettings.campaignWear`): umut tenekesinde 3 → 7 → 11
    yaprak + sap (V.001 / V.010 / V.025), masada yırtık teleks yığını ve EVRAK kalınlığı ⌊vardiya ÷ 3⌋ (en çok 10), V.016'dan
    teleks kuyruğu (masada + önden sarkan), ikinci kupa (umut işaretlerinden taşındı, masanın sol arkası) ve teneke.
    `HopeMarks`'tan ikinci kupa, yapraklar ve silinen ilanların boş yuvaları çıktı.
  - **Eğitim notu:** 338 px, −1,5°, #F2E9CC; başlık “ANALİST · NOT” + “ADIM n/12” (mavi), metin Kalam 22 #1A2F5A, kâğıt
    metinle uzar; alt satır tuş fişleriyle “Boşluk sonraki · Esc notları kapat”. Masada sağ alt (−16, 16), baskında sağ üstten
    (−24, −796). Boşluk bilgi adımını geçer (baskında değil: orada makineleri hızlandırır); Esc başka kâğıt açık değilse
    notları kapatır (`SiginakDesk.HandleEscape` → `SiginakTutorial.HandleEscape`).
  - **Çeviri:** 16 yeni metin (EN); hover satırları koda taşındı (sahnedeki `DeskTarget` satırları çeviri aracına girmiyor).
    Tablodan düşen satırlar: sökülen KAYIP ilanları, eski pano listesi, eski CRT/oda kartı biçimleri ve daha önce geri alınan
    telsiz vurgusunun metinleri.
  - **Testler:** `SiginakDeskTests` (6 tuş, yalnız kapı tuşunda dikkat lambası, `door`/`freedBoard`/`tally` bağlı, 12 yuva,
    ikinci kupa harekât izlerinde), `SiginakCrtTests` (dört satır, `ShortRegion`), `TutorialFlowTests` (`step` alanı). Tam
    EditMode **337/337** (boş sahne).
  - **Oyunda:** V.001 (eğitim açık) ve V.014 (fikstür) TR/EN çekildi: FON “060,0” / “062.0”, 6 tuş + KAPI, pano “00/12 ·
    Batı Avrupa?” ve “01/12 · Doğu Avrupa?”, alarm 0 ve 4 hücre, çentik 1 ve 14 çizgi, CRT dört satır, eğitim notu “ADIM 1/12”.
    V.014'te ziyaretçi çaldı (amatör); kâğıt kapanınca 4 · KAPI dikkat durumuna geçti, 4 kâğıdı yeniden açtı. Konsolda hata yok;
    kayıtlar ve dinamik font asset'leri her denemeden sonra geri yüklendi.
  - **İş akışı notu:** Sahne dosyası Unity açıkken dışarıdan temizlenince editör “dışarıdan değiştirildi” kutusunda bekledi
    (kullanıcı kapattı). Temizleyici artık yalnız boş sahne açıkken çalışıyor.

- **2026-10-30 · Aşama 1 (token, sprite, font):**
  - **Karar:** Rev1/Rev2 anahtarı yok, güvenli geçiş git'le (kullanıcı). Token'ların `*.v1_0.json` yedekleri yazılmadı;
    1.0 dosyaları `9d16037`'de.
  - **`tokens_siginak.json` 1.1** projedeki dosyaya birleştirildi (tek seferlik Python betiği, `json.dump(indent=1)`
    dosyayı birebir yeniden üretiyor; diff yalnız gerçek değişiklik). `_new_1_1`'in 87 yolu R'deki sırasıyla eklendi,
    `_changed_1_1`'in 9 yolu güncellendi, `_new_1_1`/`_changed_1_1`/`_was_1_1` listeleri ve meta 1.1 oldu. İlk turun
    uygulama yollarından hiçbiri kaybolmadı. 1.1 ile farklı kalan iki yol bilinçli: `type.taskHead` 14 (≥ 14 kuralı) ve
    `cameras.K6a.pos` (0,32 · 1,28 · **0,36**; sahnedeki `Poses/K6a` de bu, 1.1'deki −0,36 yazım hatası).
  - **`raid_tokens.json` 1.1** olduğu gibi kondu (1.0'ın üst kümesi). `RaidTheme`'i çalışma anında okuyan kod yok
    (oyunun baskın kamerası ve HUD'u `tokens_sade.json → raid.*`'ı okuyor); değişen yerleşim değerleri bugünkü HUD'u
    oynatmaz. `SadeThemeSetup.ImportRaid` kökteki `_` açıklamalarını artık atlıyor (önceden `_was_1_1` da düzleşirdi) ve
    test için `FillRaid` olarak açıldı. `SiginakThemeSetup` değişmedi: yeni gruplar genel kurallarla zaten okunuyor.
  - **Çalışma anında okunan tek değişen token** `space.slipW` (görev fişi 436/452 → 432 px). Öteki değişenler (`type.*`,
    `deltaTag`, `reportW`, sayaç renkleri) sahneye işli; ilgili aşamada uygulanacak.
  - **Sprite'lar:** `SiginakSpriteManifest`'e 46 Rev2 girdisi (`R2(...)`): sprite'lar `Art/Siginak/Sprites/Rev2/`,
    Quad dokuları (`ui_counter_comma`, `tx_counter_digits_tenths`, `decal_tally5`) `Art/Siginak/Textures/`. Eşit olmayan
    9 dilim kenarı için `Entry.SpriteBorder` (kontrplak şeridi L 2 · T 2 · R 2 · B 6). "Sprite'ları içe aktar" 84 dosya
    kopyaladı; ilk turun 38 dosyası paketle birebir aynıydı.
  - **Malzeme:** `Art/Siginak/Materials/Counter wheel tenths.mat` (kırmızı çarkın kopyası, doku `tx_counter_digits_tenths`,
    tiling 1 × 1/11). Sahneye Aşama 2'de bağlanacak.
  - **Glifler:** `charset.txt`'e `✕ ▶ ◆ ● ■`; "Karakter setindeki eksikleri ekle" bunları DejaVu sembol asset'lerine ekledi
    (atlas baştan çizilmedi, GUID'ler aynı). Dokuz yüzün hepsinde statik zincir tam; `SiginakFontPreview.RequiredGlyphs`
    beş işareti de denetler.
  - **Testler** (`SiginakFoundationTests`): `Tokens_Rev2NewPathsResolve`, `Tokens_Rev2ChangedValues`, `RaidTokens_Rev2`,
    `RaidTokens_AssetIsUpToDate`; sprite testi Rev2 paketini ve eşit olmayan kenarı da denetler. Tam EditMode (boş sahne)
    **336/336**.
  - **Oyunda:** V.014'te görev fişinin kâğıdı 432 px, fiş ve harita bozulmadan çiziliyor; konsolda hata yok. Kayıtlar ve
    Play'de değişen dinamik font asset'leri geri yüklendi.

- **2026-10-30 · Aşama 0:** Paket ve proje incelendi; eşleme tablosu, ziyaretçi eşlemesi, işaret kapsamı, kararlar, açık
  sorular ve riskler yazıldı. Kod, sahne ve prefab değişmedi.
