# Sığınak · Revizyon 3 + Ek 11-05 — Uygulama haritası

**Durum:** Aşama 0 (keşif), Aşama 1 (model hattı), Aşama 2 (komuta odasının temeli), Aşama 3 (kartlı pano ve kartlar),
Aşama 4 (vardiya kapanışı), Aşama 5 (tarif defteri ve üretim), Aşama 6 (kapı ve kabul), Aşama 7 (harita, kâğıtlar, CRT), Aşama 8 (menü sahanlığı ve dolap) ve Aşama 9 (baskın kitleri) bitti; sonuçlar §7–§15'te; Aşama 3–6 kararları
§6.3–§6.6. Aşama 6'da Core değişti (K4-A gözetmen kuralı): kayıt biçimi **15**. Aşama 0 kararları kullanıcı tarafından
onaylandı (2026-11-06: "önerilerle geç"); §6 öneriler artık karardır.

**Taban:**
- `siginak-ui` · `c1a743a Before Design implementation.`
- Üstüne commit edilmemiş kartlı pano çalışması:
  - `PlanningBoardState`, `CommunityAdmissionState`
  - `Simulation.PlanningBoard`, `Simulation.PlanningRaid`, `Simulation.CommunityAdmissions`
  - `GameRecord` biçim 14 (Aşama 6'da 15: K4-A, §12)
  - eski vardiya şalterinin geri gelmesi (2026-11-05)

**Okunanlar:**
- Paket belgeleri: `PROMPT.md` (tamamı), `README.md`, `modeller/README.md`, `threejs/README.md`, `tasarim/README.md`.
- Uygulama notları (`tasarim/notlar/`):
  - R1–R6, R8–R12, R15–R19;
  - `Oncelik`, `Sorunlar`, `Tokenlar`;
  - `Karsilastirma-1/2/kapi`.
- 198 manifestin özeti: aile, üçgen, parça, yuva, çapa, ışık, `place`.
- `tokens/*.json` 1.3, projedekilerle yol yol karşılaştırıldı.
- Core, Bridge, Runtime ve Editor kodu; `DeskGame.unity`, `Giris.unity`, `Baskin.unity`.
  - Kod ve sahneler dört okuma ajanıyla tarandı.
  - Harita yazıldıktan sonra altı ajanlı bir çürütme turundan geçti: beş bölüm şüphecisi ve bir eksiklik denetçisi.
  - Kilit iddialar ayrıca elle doğrulandı. Düzeltmeler bu sürüme işlendi.

**Kısaltmalar:**
- Simülasyon: `S` = `Sim.S`, `Bd` = `Sim.Board`, `Adm` = `Sim.Admission`.
- Dosyalar:
  - `PBP` = `Desk/Scripts/Bridge/PlanningBoardPresenter.cs`
  - `SPB` = `Core/Simulation.PlanningBoard.cs`
  - `SCA` = `Core/Simulation.CommunityAdmissions.cs`
- Yollar `Assets/Corrigible/` altındadır.

---

## 1. Önemli bulgular

### Kural ve veri

1. **DIŞ KAYIT ve İNCELE bugün canlı oyunda hiç başarılı olamıyor (kural boşluğu).**
   - Gözetmen kuralı: `HasAdmissionWatcher` (SCA:170) aday dışında, sağlam (`Down <= 0`), kısıtsız ve **`Job == "watch"`**
     olan birini istiyor.
   - Panoda Gözetim alanı yok. `PBP.RetireJobs` (PBP:178–185) plan açıkken her `LateUpdate`'te `watch`, `contact`, `care`
     ve `gather` görevindekileri `board_assign … rest` ile Boşta'ya alıyor. Bu kayda geçen bir komut.
   - Sahnedeki görev alanları yalnız `tek`, `emp`, `ana`, `craft`, `cook`, `train` ve `rest`. `watch` atayan başka bir yol yok.
   - Bu yüzden ölü olanlar:
     - kapıdaki `record` incelemesi (DIŞ KAYIT);
     - `ReviewResident` (İNCELE). Düğme yine de kişinin kalıcı dosyasını kapı pozunda açıyor (PBP:336); dosyayı görmenin tek
       yolu bu;
     - kısıtlı sakinin çalışabilmesi. `CanRestrictedResidentWork` (SCA:258–262) gözetmen yanında `ObservationTarget == id`
       de istiyor. `ObservationTarget` tek bir yuva: İNCELE onu doldurur, adaya DIŞ KAYIT ezer;
     - dış temasla erken aday.
   - Tasarımın önerisi Core'da yok: "Boşta destesinin üst kartı gözetmen" (token `admission.supervisor`, "açık karar").
     Bkz. Soru 2.
2. **Kayıt biçimi 14 hiçbir dalda commit edilmedi.**
   - `git log --all -S "CurrentVersion = 14"` boş dönüyor; HEAD hâlâ 13.
   - Kartlı pano ve kabul yalnız çalışma ağacında. Elinde biçim 14 kaydı olan bir oyuncu yok.
   - Bir kural değişikliğinde `CurrentVersion`'ı 15'e çıkarmak (AGENTS kural 1) yalnız geliştirme makinesindeki kayıtları
     geçersiz kılar.
3. **Üretim bugün otomatik; R18 tarifi oyuncunun seçtiğini varsayıyor.**
   - `PBP.RetireJobs` ilk karede `board_auto` kaydediyor (`EnableAutomaticProduction`). Kapatan komut yok.
   - Oyuncu yalnız hammadde koyuyor (`board_stage`). Tarifi Core seçiyor: `AutomaticProductionRecipe` (SPB:243–245) eksik
     saha kitini öne alır, yoksa liste sırasına bakar.
   - Başlangıçta bütün kitler var (SPB:51), bu yüzden metal + devre her zaman EMP hücresi verir. Tarif kartları sürüklenmiyor
     (`card.Draggable = false`, PBP:408).
   - Otomatik yolun sınırları (SPB:445–455):
     - vardiya başına en çok bir ürün çıkar;
     - `Turns` ve üretim hızı kademesi yok sayılır; iki vardiyalık EMP hücresi de bir vardiyada çıkar;
     - üretim ekibinde biri yoksa, açlık varsa ya da depo doluysa (32) sessizce atlanır;
     - artan fişler sessizce bir sonraki vardiyaya kalır.
   - **Tarif seçen iş emri yolu kapalı değil.** `board_queue`, `board_reserve` ve `board_cancel` otomatik kipte de çalışıyor
     ve var olan emir otomatik yoldan önce işleniyor (SPB:447–457). Bu yolun özellikleri:
     - tek emir olabilir;
     - ayrılan girdiler `BoardPlanCheck` (SPB:312–319) ile tam tutmadan fiş basılmaz;
     - `Turns` ve hız kademesi geçerli.
   - Bkz. Soru 9.
4. **Eski yol oyuncuya kapalı.**
   - `SaveStore.Load` sürümü tutmayan kaydı yüklemiyor. HEAD'deki 13 kayıtları geçersiz.
   - `Balance.planningBoardEnabled` varsayılanı ve `BalanceConfig.asset` değeri `true`. Eski yol yalnız bu ayar elle
     kapatılınca açılır.
   - Sunumun bugünkü hâli:
     - Sahnede `SiginakRoom/Shelf` kapalı (maket, oda kartları, BOŞTA tepsisi, pano). `SiginakShelter` devre dışı ve hiçbir kod
       açmıyor (`PlanningBoardSceneTests:67–73`).
     - Kapının eski yolu (23 kart, siluetler, `DoorCardView`) bağlı ve testli: `DoorTests` 12, `SiginakDoorTests` 9; ikisi de
       `LegacySimulation`.
     - `LiberationCampaignTests`'in bütün testleri `LegacySimulation` kullanıyor. Kartlı oyunun kayıt testleri
       `SaveStoreTests`, `PlanningBoardTests` ve `CommunityAdmissionTests`'te.
5. **Sayaçlar tasarımla uyumlu, ama ek dokuya basılmak isteniyor.**
   - ERZAK eki bugün çalışma anında TMP: `/` + `SuppliesCapacity` (`boardSuppliesCapacity = 60`). Sahnedeki sabit "/40"
     eziliyor.
   - MALZEME = `Shelter.Materials` = dört hammaddenin toplamı (SPB:510).
   - `CounterRack` modeli "/60, /12, /20" eklerini `tx_counterFraction` dokusuna basılı istiyor. Modelde ek için TMP yuvası
     yok, dokunun hücreleri de "/30". Basmak denge değerini dondurur. Bkz. Soru 11.
6. **Portreler eksik.**
   - Pakette 10 portre var: Selin, İpek, Deniz, Nermin, Zeynep, Cem, Eren, Suna ve adaylardan Aylin, Rauf.
   - Kurucu ad havuzu 10 isim (SPB:32). Derya ve Orhan'ın portresi yok.
   - Aday havuzu 24 isim (SCA:12). Yalnız Aylin ve Rauf'un portresi var; Suna ve Eren her iki havuzda.
   - `AdmissionDocument.Portrait` dosya adı değil, kişi kimliği (`sakin-N` / aday kimliği).
   - Kural gerekiyor; robotu ele vermemeli. Bkz. Soru 12.

### Sunum ve yapı

7. **Rev2/Rev3 anahtarı da kalıcı oda kurucusu da yok.**
   - Revizyon 2'de kullanıcı kararı "anahtar yok, güvenli geçiş git'le" idi (Rev2 haritası §8 Soru 1).
   - AGENTS.md kural 5 kalıcı sahne kurucusunu yasaklıyor.
   - PROMPT ise anahtar (iş bitti ölçütü 6) ve `Rev3RoomBuilder` istiyor. Bkz. Karar K1–K2.
8. **Oyunda model hattı hiç yok.**
   - Oda, menü ve dolap ilkel şekillerden ve 38 prosedürel mesh'ten kurulu.
   - Projede `.obj`/`.fbx` yok, `ModelImporter` kullanan kod yok. Tek `AssetPostprocessor` sprite içindir (`DeskSpriteImporter`).
   - Manifestlerde **çarpıştırıcı verisi yok.** `DeskTarget` Collider + `PhysicsRaycaster` istiyor; importer tıklanan
     parçalara kutu çarpıştırıcı kurmalı.
   - Paketin modellemediği yüzeyler: oda kabuğu, sahanlık duvarları, zemin ve tavan. Bunlar ilkel kalır; R14 malzemesi ve
     detay haritasıyla giydirilir.
9. **`_AxisTest` geçti** (§3.1): Scale Factor 1, Convert Units kapalı, eksen dönüşümü yok. OBJ için Unity varsayılanı
   (`useFileScale` açık, `fileScale` 1) da aynı sonucu veriyor.
10. **Baskın kamerası 132,58 px/kare** (1 kare = 1 dünya birimi).
    - Kaynak: `SadeRaidCameraFraming` → `tokens_sade.json → raid.camera`. Orto 4,0730 (16:9 ve daha geniş; 16:10'da ≈ 4,525),
      açılar 35°/45°.
    - `raid_tokens.json → camera` (88,39) çalışma anında okunmuyor; yalnız `SiginakFoundationTests:242` okuyor.
    - Sonuç: ince parça kuralı **0,025 birimde kalır** (Soru 7).
11. **Kamera değerleri sahnede; token'dan okunmuyor.**
    - Çapalar `SiginakRoom/Poses/*` altında. K9 dolap prefab'ının `Camera anchor`'ı, menüde de odada da.
    - FOV ve `lensShiftY` rig'in poz yuvalarında: 56,7 · 30 · 43 · 46 · 53 · 58 · 45.
    - Menünün FOV'ları `EntranceController`'da (52 / 40). M3'ün iki çapası var (eşik, içerisi).
    - `cameras.*` yalnız testlerde karşılaştırılıyor.
    - Pozlar: `Desk`=K1, `Map`, `Shelf`=K4, `Crt`=K5a, `Shift`=K6a, `Door`=K8, `Cabinet`=K9.
    - Yeni gerekenler: **K6**, **K8A/K8G/K8R**, menüde **MS**. Her biri enum değeri, çapa ve FOV ister.
    - Pano birden çok yoldan açılıyor:
      - "2" tuşu `PBP.Show`'u atlayıp doğrudan `rig.FocusShelter`'a gidiyor;
      - aynı `DeskTarget`'e iki abone var, bir tık `Show`'u iki kez çağırıyor.
    - Bu yüzden poza bağlı ışık, süre ve şerit gizleme `SiginakCameraRig`'in poz değişiminde yapılmalı.
12. **Token 1.3 projedeki 1.1'in üst kümesi; 26 düğüm "KALDIRILDI" oluyor.**
    - `_changed_1_3`'teki 28 yoldan 26'sı kaldırılmış; `cabinet` ve `cameras.K4` yalnız değişmiş.
    - `SiginakThemeSetup` `_` ile başlayan anahtarları atlıyor, kaldırılan düğümleri `{removed, why}` olarak düzleştiriyor.
      Eski değerler kaybolur.
    - Etkiler, hepsi yalnız eski yolda:
      - `lights.roomBulb.k4Scale` 0,55 → 1 (devre dışı `SiginakShelter`);
      - `colors.door.fatalRoomLight` → eflatun hata rengi (`SiginakDoor` ölümcül kapı);
      - `motion.summaryStaggerS` → 0,12 (aynı).
    - **Test kırılır:** `SiginakFoundationTests:193–194` `roomCardNameTex` / `roomCardLineTex` rollerini bekliyor; 1.3 ile
      546/546 korunmaz. Test aynı adımda güncellenmeli.
    - Yeni gruplar: `board` (`layoutMm`, `plates`, `limits`, `readability`: K4 1,52 px/mm, kart adı 21 px, değer 14–17 px),
      `cards`, `recipeBook`, `personFile`, `shiftClose`, `admission` (`supervisor` dahil), `lightsRecipe`, `menu`, `models3d`.
    - `raid_tokens.json` 1.3 doğrudan konabilir: üst küme, silinen yol yok.
13. **Işık.**
    - DeskGame'de 2 gölgeli ışık var: tavan ampulü **nokta** (Soft; atlasta 6 dilim, Sorunlar 1) ve masa lambası spot 94°.
    - Poz başına ışık tarifi ve poza göre gölge açıp kapatan kod yok. Var olan ışık kodları:
      - `ShiftSequence` karartması: kartlı oyunda çalışmıyor, `SiginakShift.Throw` önce dönüyor;
      - `SiginakDoor.RoomRed`: eski yol;
      - `DoorProp` lambaları;
      - `SettingsCabinet` ampulü.
    - **`lightsRecipe` çoğunlukla düz metin.** Yalnız `M0` yapılandırılmış (5 ışık). K1, K4, K6, K8A … cümle. Poz tarifleri
      elle bir asset'e yazılmalı; test de oradan okur. 1.3 `lights.bulb`'u hâlâ gölgeli nokta veriyor.
    - Boru hattı:
      - `PC_RPAsset`: ana ışık gölge haritası 2048, ek ışık atlası 4096, yumuşak gölge açık, Forward+.
      - `lightsRecipe.RAID`'in 4096'lık gölgeli yön ışığı ortak asset değişikliği ister.
    - **TAA:** kamera TAA çiziyor (`m_Antialiasing: 3`, `m_TaaSettings.m_Quality: 4`).
      - Oyuncuya post-processing ya da TAA ayarı **yok**; PROMPT'un "oyuncu kapatabilir" varsayımı karşılıksız. Bkz. Soru 13.
      - `SiginakCapture` tek `Render` ile MSAA 4 dokuya çiziyor; TAA'lı oyun görüntüsünü vermiyor. TAA karşılaştırması Play'de
        `CorrigibleDebug.Capture` ile yapılmalı.
14. **Assembly sınırları.**
    - `Corrigible.Desk` (Runtime) `Corrigible.Game`'i ve URP'yi referans almıyor.
    - Runtime bileşenleri (`ShiftSwitch`, yeni `PartDriver` …) doğrudan ses çalamaz. Olay yayar, sesi Bridge çalar
      (bugün `SettingsCabinet` → `SiginakCabinet` böyle).
    - URP ışık verisi (`UniversalAdditionalLightData`) için yeni referans gerekir. Çekirdek `Light` alanları yeter.
15. **Ses: odadaki vardiya şalteri hiç ses çalmıyor; pano kodunda `Sfx` çağrısı yok.**
    - Yazıcı, kopma, mıknatıs, tebeşir, interkom, kanat ("tank"), damga ("tak") ve gong için cue yok. Son cue `PowerDown = 96`.
    - Rev2 kuralı sürer: yeni `Cue` eklenir, klip yoksa sessiz.
16. **`ShiftSwitch` üç yerde ortak:** odadaki vardiya şalteri, dolabın MENÜYE DÖN kolu, menüdeki son şalter. R19
    değişikliği ötekileri bozmamalı.
17. **Kartlı oyunda vardiya kapanışı eski diziyi kullanmıyor.**
    - `SiginakShift.FileReceiptFlow`'da sayaç tekeri, `report.Show` ve ses yok. Sayaçlar `Changed` ile anında atlıyor.
    - EVRAK'a Rev2 biçiminde boş bir vardiya raporu ekleniyor (SiginakShift.cs:165).
    - Fişin gövdesi `Bd.ReceiptText` değil, `Sim.BoardReceiptPreview` (her seferinde geçerli dilde yeniden kuruluyor).
      Başlığı görünüm yazıyor.
    - **Hata:** tepsi üstüne bırakılan ama dosyalanamayan fiş (`receiptFiling`, `InputBlocked`, kimlik uyuşmazlığı ya da
      `FileBoardShift` reddi) dile dönmüyor, bırakıldığı yerde kalıyor. Aşama 4'te düzeltilir.
18. **Pano bugün tek World Space uGUI tuvali.**
    - Boyut 1800 × 1260 px × 0,00088 m. 56 kart ve 11 bırakma alanı sahneye elle kaydedilmiş, prefab yok.
    - Kartlar sıraya göre bağlanıyor, kimliğe göre değil (`shown[i]`, `Equipment[i]`, PBP:366–377):
      - bir ölüm, hurda ya da kırılan zırh hangi kartın neyi gösterdiğini kaydırır;
      - `ShownPeople` 10'dan fazlasını `Take(10)` ile sessizce kesiyor. Ölüler listede kaldığı için `Bd.People` 10'u geçebilir.
    - Eşya kartı kişinin **önüne** çiziliyor (`SetAsLastSibling`). Tasarım **arkasına** 14 mm şerit istiyor.
    - Kilit görseli yalnız metin. `machine`, `receipt`, `cover`, `fade` alanları sahnede null.
    - `PlanningBoardSceneTests` bugünkü uGUI yapısını kilitliyor (10/32/4/6 kart, 7 alan, tuval < 2,4 × 1,7 m …). Model geçişinde
      yeniden yazılır.
19. **SIĞINAK tuşu zaten K4'e gidiyor.** Yol: `StationKeyRail.Shelter` → `LiberationDeskView.ShowShelter` → `PBP.Show` →
    `rig.FocusShelter`. K4 çapası bugün (−0,59 · 1,42 · 0,42), yaw −35°, FOV 43.
20. **Sayaç şeridi ve istasyon çipleri (EdgeLine) poza bağlı gizlenmiyor.** Auto kipinde sayaç rafının yüzü kadraj
    dışındaysa görünüyor (`SiginakDesk.cs:169, 199–205`). PROMPT K4, K6 ve K8A/G/R'de gizlenmesini istiyor.
21. **Sağ tık bugün bakış.** Sağ tuşla sürükleme ±25° / −15…18° çeviriyor. PROMPT panoda ve kapıda "sağ tık = MASAYA DÖN"
    istiyor (Karar K7).
22. **Kapı modeli ile `DoorProp` aynı yerde, eksen kuralı farklı.**
    - `DoorProp` (2,20 · 0 · 0,75) yaw 270; paketteki `DoorFrame`/`DoorLeaf` yaw 90 (ön yüz −z, odaya bakar).
    - Model `place` değeriyle konur; `DoorProp` referansları modelin parçalarına bağlanır.
    - Konumlar birebir tutuyor: şalter (0,765 · 0,76 · 0,885, yaw 10), EVRAK (0,765 · 0,76 · 1,30, yaw −4), teleks, alarm panosu.
23. **Test altyapısı yalnız EditMode.** PROMPT §8.6'nın PlayMode testleri (sürükle-bırak, şalter → fiş → EVRAK) yeni bir
    asmdef ister (Karar K10).

---

## 2. Klasör ve assembly yerleşimi (öneri)

PROMPT'un önerdiği `Assets/Corrigible/Presentation/Siginak/Rev3/` yolunda assembly yok. Oraya konan script'ler
`Corrigible.Desk` / `Corrigible.Desk.Bridge` içinden görünmez. Önerim, ilk iki turun düzenini sürdürmek:

| İçerik | Yer |
|---|---|
| OBJ kopyaları | `Desk/Art/Rev3/Models/<aile>/<Model>.obj`. MTL kopyalanmaz; `materialImportMode = None` |
| Prefab'lar | `Desk/Prefabs/Rev3/<aile>/<Model>.prefab` |
| Malzemeler | `Desk/Art/Rev3/Materials/` (~45 paylaşılan URP Lit + kit malzemeleri). `Rev3MaterialLibrary.asset` de burada (Resources değil; editörde `AssetDatabase` ile yüklenir). Sahneye özel dokulu malzemeler `Desk/Art/Rev3/SceneMaterials/` |
| Dokular | `Desk/Art/Rev3/Textures/`: `tx_*`, `papers_new`, portreler |
| Model altyapısı (`PartDriver`, `TmpSlot`, poz ışık tarifi verisi) | `Desk/Scripts/Runtime/Rev3/` (`Corrigible.Desk`). Ses ve URP ışık verisi Bridge'de (§1 bulgu 14) |
| Pano, kart, kabul görünümleri | Var olanların yanına: `Desk/Scripts/Runtime/PlanningBoard/` ve `Runtime/Siginak/` |
| Oyun bağlantısı | `Desk/Scripts/Bridge/` (`PlanningBoardPresenter`, `Siginak/*`) |
| Editör araçları | `Desk/Scripts/Editor/Rev3/` (`Corrigible.Desk.Editor`): `Rev3ModelImporter`, malzeme kurulumu, doğrulama |
| Model verisi (manifest, sayfa) | `Docs/UI_Siginak_Rev3/modeller/`'dan okunur, Assets'e kopyalanmaz (Rev2'de sprite manifesti de böyleydi) |
| Testler | `Desk/Tests/EditMode/Rev3*Tests.cs`. PlayMode için Karar K10 |
| Görüntüler | `Docs/UI_Siginak_Rev3/screens_unity_rev3/` |

---

## 3. Eşleme tablosu

**Yapılacak sütunu:**
- **KULLAN:** olduğu gibi kalır.
- **DEĞİŞTİR:** görünüm değişir, mantık aynı.
- **YENİ:** yeni bileşen ya da nesne.
- **MODEL:** ilkel şekiller paketteki modelle değişir.

"Mevcut karşılık" Aşama 0'daki hâli anlatır.

### 3.1 Altyapı (§4, §5, R14)

| Revizyon 3 öğesi | Mevcut karşılık | Yapılacak | Veri kaynağı |
|---|---|---|---|
| `_AxisTest` | yok | **YAPILDI (Aşama 0).** Body bbox (−0,04 · 0 · −0,08)…(0,50 · 0,16 · 0,25). Flap mesh'i pivotsuz (0…0,12, ±0,03); `pivotLocal` (0,5 · 0,05 · 0) eklenince bbox max x 0,62 = manifest. Üçgen 132 + 44 = 176; sarım normallerle uyumlu; z +90° Flap'ı dik kaldırıyor. Görüntüler `screens_unity_rev3/A0_axistest_*.png` | `manifest/_AxisTest.json` |
| `Rev3ModelImporter` (OBJ + manifest → prefab) | yok | YENİ, kalıcı editör aracı. Asset hattıdır, sahne kurucusu değil; AGENTS kural 5'e uyar. İşleri: (1) `parent`/`pivotLocal` ile parça hiyerarşisi; (2) `PartDriver` verisi; (3) malzeme anahtarı → paylaşılan malzeme; (4) `TXT_*` → TMP (`capMm` → font boyutu fontun büyük harf oranıyla; three.js 0,7 kullandı); (5) `anchors` → boş nesne; (6) `lights` → Light; (7) 2 cm altı parçada Cast Shadows Off; (8) **tıklanan parçalara kutu çarpıştırıcı** (parça bbox'ından, MeshCollider yok). Doğrulama: bbox ± 1 mm, üçgen eşit | `modeller/manifest/*.json`, `obj/*.obj` |
| `Rev3MaterialLibrary` | Elle kurulmuş `.mat`'lar (`Desk/Art/Siginak`, `Art/Raid/Kits/<id>`) | YENİ: anahtar → URP Lit. Aynı değerli anahtarlar `usedBy` ile birleşir; emission ve saydam varyantları ayrı | token 1.3 `materials.*`, `materials3d.*`, `kits.*` (295 anahtar) |
| `PartDriver` (açı, kayma, swap) | Her görünüm kendi parçasını sürüyor (`ShiftSwitch`, `DoorProp`, `SettingsCabinet` …) | YENİ, küçük bileşen. Var olan görünümler pivotlarını modelin parçasına bağlar; mantık yerinde | manifest `parts[]`, `motion.*` |
| Etkileşim durumları (normal · üzerinde · tutuldu · hedef üstünde · bırakıldı · geçersiz · devre dışı · kilitli) | `DeskTarget` (kalkış, 400 ms `HoverTag`, `Interactable`/`Blocked`/`LineOverride`), `UiHoverTarget`, `PointerHover`, `IHoverSource` | KULLAN + DEĞİŞTİR: 3B nesnelerde `DeskTarget`; tutuldu, hedef, bırakıldı ve geçersiz yeni sürükleme denetleyicisinde (§3.3); süreler `motion.*` | — |
| Yerelleştirme (`TXT_*` yuvaları) | `DeskLocText` (sahne), `Loc.T/F/N` (kod), `strings.txt` (1883 satır; `LocalizationTool` .cs, prefab ve sahneleri tarar); sayı biçimi `Loc.Culture` | KULLAN: importer yuvaya TMP + `DeskLocText` koyar (`sample` TR kaynak, `sampleEn` → `strings.txt`). **Asıl kaynak `strings.txt`.** Token'daki `tr`/`en` (`board.plates`, `shiftClose.*`, `admission.decisions`) yalnız denetim içindir; tutarlılık testi eklenir. Bugün olmayanlar: "PLAN AÇIK", "HURDA · 1 eşya → 1 metal", kısa "PLAN KİLİTLİ" | — |
| Token 1.3 (sığınak) | `Desk/Data/Siginak/tokens_siginak.json` 1.1 → `SiginakThemeSetup` → `SiginakTheme.asset` | DEĞİŞTİR: dosya 1.3 olur. Importer: (1) kaldırılan düğümlerde `_was_1_3` değerini korur (Soru 5); (2) yeni grupları tipler; (3) eşlenmeyen fontu bildirir. `SiginakFoundationTests:193–194` aynı adımda güncellenir | `tokens/tokens_siginak.json` |
| Token 1.3 (baskın) | `Desk/Data/Sade/raid_tokens.json` 1.2 → `SadeThemeSetup.ImportRaid` → `RaidTheme.asset` (yalnız renk ve sayı) | DEĞİŞTİR: dosya yerine konur. `kits.*` ve `materials3d.*` malzeme kitaplığına | `tokens/raid_tokens.json` |
| Fontlar | TMP: CP 400/700, LF 400–800, Kalam 400/700, DejaVu; `charset.txt`; `DeskFontSetup` | DEĞİŞTİR: Big Shoulders Stencil Display 800 eklenir (Karar K6): statik + dinamik TMP, `DeskFont` enum'u, `SiginakThemeSetup` eşlemesi | — |
| Poz başına ışık (≤ 4 gölgeli) | yok | YENİ: poz → ışık tablosu elle yazılmış bir asset'te (token metni yalnız kaynak); `SiginakCameraRig` poz değişiminde uygulanır. Ampul → aşağı bakan 120° gölgeli spot + gölgesiz dolgu noktası. Şiddet çevrimi: three.js önizlemesi (decay 2) → URP, bugünkü Rev2 ışıklarıyla kalibre. Tarif tablosu §3.13 | `lightsRecipe.*`, R14c |
| Yıpranma / detay haritası | yok | YENİ: `tx_wear_detail` (metrik UV, 0,25 m), URP Lit Detail Map | R14b |
| Ses | `Sfx.Play(Cue)` → `AudioDirector` → `SoundBank.asset` (son cue 96) | DEĞİŞTİR (yalnız ekleme): yeni cue'lar §7 listesinden. Runtime olay yayar, Bridge çalar. Klip yoksa sessiz | R15/R17/R19 notları |
| Rev2/Rev3 anahtarı | yok | Karar K1 | — |
| Sunum durumu kalıcılığı | `liberation_desk.json` (`SiginakDeskStore`: tohum, teleks, raporlar, `pose` dizgesi); `planning_board_layout.json` (`PlanningBoardLayoutStore`, yalnız `production:*` konumları geri okunuyor) | DEĞİŞTİR: yeni poz kimlikleri (K4, K6, K8A/G/R) `liberation_desk.json`'a, `pose` dizgesiyle; K8A/G/R'de açılırsa K8'e döner. Deste açık/kapalı, defter sayfası ve fiş açıları `planning_board_layout.json`'a. Oyun kaydına hiçbiri girmez | — |

### 3.2 Komuta odası temeli (R5, R14; Aşama 2)

| Revizyon 3 öğesi | Mevcut karşılık | Yapılacak | Veri kaynağı / komut |
|---|---|---|---|
| Oda kabuğu (duvarlar, zemin, tavan) | `SiginakRoom/Shell` ilkel şekiller | İLKEL KALIR: R14 malzemesi ve detay haritası; değişen ölçü tek seferlik script'le | — |
| Masa (`Desk`) | `SiginakRoom/Desk` (Top, Frame, Pedestal L/R, Modesty panel) | MODEL. `MapMount` (harita tuvali, `TeamFigure`) çocuk olarak kalır | — |
| Sayaç rafı `CounterRack` (6 modül, 16 teker, `TXT_Label_0–5`) | `Desk/Counter bank` (`CounterBank`, 6 `CounterModule`, TMP ekler) | MODEL + DEĞİŞTİR: tekerler `CounterModule`'e bağlanır (rakam d için yerel x ekseninde 36·d; 0° = 0. İlk taslaktaki 162 − 36·d bu modellerde yanlıştı). ERZAK eki Soru 11. MALZEME plakası Soru 3 | `S.Day`, `S.Fon`, `Shelter.Supplies` / `SuppliesCapacity`, `Shelter.Materials`, özgür bölge, `S.Trust` |
| Anahtar rayı (`KeyRail` + 4 `KeyKey`) | `Desk/Key rail` (`StationKeyRail`: SIĞINAK, GÖREVLER, KAPI, MASA) | MODEL: Cap (kayma), Jewel (swap; KAPI 1 Hz), Tag (±12°, "kapıda biri / bekliyor") mevcut durumlara | rig pozu; `Adm.Candidate != null` |
| Vardiya şalteri | `Desk/Shift switch` (`ShiftSwitch` + `ShiftReceiptMachine`) | MODEL (yerleşim birebir). Ayrıntı §3.5 | — |
| Teleks (`Telex`, `TelexPaper`, `TelexRoll`) | `Desk/Teleprinter` (`Teleprinter`, `TelexFeed`) | MODEL. Kâğıt yazıları `TelexPaper` yuvalarına | `TelexFeed` |
| EVRAK (`PaperTray`; StackLate swap) | `Desk/Paper tray` (`EvrakTray`: son 5 rapor, `receiptDropArea`, `ShowArchived` / `FlashLatest`) | MODEL. Kartlı oyunda ne tuttuğu: Soru 14 | — |
| Masa lambası `DeskLamp` | `Desk/Desk lamp` (mesh) + `Desk lamp light` (spot 94°) | MODEL. Işık modelin ışık noktasına; açı tariften | `lightsRecipe.K1` |
| Damga rafı `StampRack` | `Desk/Stamp rack` (script yok) | MODEL | — |
| Süs nesneleri (kupa, kalem, talaş, silgi, kahve halkası, kâğıt yığını, `TeamFigure`) | `Desk/Mug`, `Pencil`, `Shavings`, `Eraser`, `TeamFigure` | MODEL. Geç harekât katmanı `CampaignWear`'e | `S.Day` |
| Duvarlar (`AlarmBoard`, `CorkBoard` + `CorkCard`, `Poster`, `StencilSign`, `Duct`/`DuctEnd`, `PipeRun`, `JunctionBox`, `CeilingBulb`, `PencilNote`) | `Shell/*` (`AlarmPanel`, `FreedRegionsBoard`, Doctrine poster, Wall stencil …), `Ceiling bulb` (`BulbFlicker`) | MODEL. Davranış bileşenleri yerinde | aynı |
| Tebeşir çetelesi `ChalkTally` | `Shell/Chalk tally` (`ChalkTally`); kapıda `Visitor tally` | MODEL. Kartlı oyunda yalnız vardiya çetelesi; kapıdaki gizlenir | `S.Day` |
| Pano | `SiginakRoom/Planning board` (−1,50 · 0 · 1,72), yaw −35 | MODEL `PlanningBoard` (−1,62 · 0 · 1,70), yaw −48. §3.3 | — |
| Işık K1 (3 gölgeli: ampul, masa lambası, pano lambası) | 2 gölgeli | §3.1 poz ışığı | `lightsRecipe.K1` |
| Kamera K1 | `Poses/K1` (0 · 1,52 · −0,15), 56,7°, lensShiftY −0,324 | KULLAN | — |
| Kenar satırı / sayaç şeridi | `EdgeLine` (Auto) | DEĞİŞTİR: K4, K6, K8, K8A/G/R'de gizli; kural rig'in poz değişiminde | rig pozu |

### 3.3 Kartlı pano ve kartlar (R15, R16; Aşama 3)

| Revizyon 3 öğesi | Mevcut karşılık | Yapılacak | Veri kaynağı / komut |
|---|---|---|---|
| `PlanningBoard` (1196 × 636 mm; BackSign, Flap, HurdaLid, LockRod, Lamp; 32 `TXT_*`; 26 çapa) | Mantar arkalık + ahşap çıtalar + ayaklar; tuval 1,58 × 1,11 m | MODEL. `PlanningBoardView` model prefab'ına taşınır; alanlar çapalara bağlanır | `Bd` |
| Kelimesiz baskı (`tx_board_face`) | uGUI başlık ve yönerge metinleri | DEĞİŞTİR: başlıklar `TXT_P_*` / `TXT_S_*` | token `board.plates` |
| 6 cep (`Slot_tek/emp/ana/uret/mutf/egit`), SAHA · BASKIN KADROSU şeridi, "KİT · GEREKLİ" | 6 `PlanningBoardDropZone` (`Görev · tek/emp/ana/craft/cook/train`) | DEĞİŞTİR: hedef, pano düzleminde dikdörtgen (çapa + `board.layoutMm`). Cep tek kişi; dolu cebe bırakma takas (Core'da) | `AssignResident(personId, job)` → `board_assign` |
| Boşta destesi (`Bosta_Deck`, `Bosta_HandLeft`) | `PlanningBoardMotion.ToggleReserve` (üst 3 kart; açıkken sola yelpaze, 230 px, y 285) | DEĞİŞTİR `BostaDeckView`: kart başına 320 ms, 70 ms ara, aralık min(82, 636/(n−1)) mm (636 = deste x 1098 − elin sol ucu 462; R15c'nin 690'ı değil); üstten açılır, alttan toplanır; yarım harekette yön değişir | `job == "rest"`; `AssignResident(id, "rest")` |
| Kişi kartı `CardPerson` 80 × 120 (Magnet, Flag, Tape; `ItemStrip`) | `PlanningBoardCard` 210 × 294 px, sırayla bağlanan 10 kart | YENİ prefab + `CardFace` (ön yüzde World Space Canvas, 1 birim = 1 mm; düzen `boardtex.js`). **Kimliğe göre havuz**, yaşayan sayısı kadar. K4'te 1,52 px/mm → ad ≥ 18 px. Portre Soru 12 | `ResidentState` |
| Ustalık sütunu | kart istatistik satırı | YENİ: 7 satır (Soru 1), kademe 2 / 6 / 12 | `Experience`, `BoardSkillTier`, `PlanningBoardData.Subjects` |
| Durum kaplamaları: YARALI, YATIYOR, KISITLI, İNCELE, KAYIP | metin satırı | YENİ `CardStateOverlay` | Hp < MaxHp; `Down > 0`; `Restricted`; `Adm.File(id).ReviewPending/ReviewNeeded`; `!Alive` |
| KAYIPLAR zarfı | ölünün kartı gizleniyor | YENİ (Soru 4). "KAYIP · V.nnn" Core değişmeden çıkar | `People.Where(!Alive)`; gün `S.RaidLog[].Day` + `LiberationReward.Squad[]` (`Dead`, `PersonId`) |
| Eşya kartı `CardItem` 80 × 50, kişinin arkasında 14 mm şerit | `PlanningBoardCard` (Equipment) 110 × 58 px, önde, adım 20 px, sırayla bağlı | DEĞİŞTİR `ItemStackLayout`: arkaya; kit · zırh · yardımcı sırası; yuva deseni (düz / taralı / noktalı); kimliğe göre | `BoardEquipment`; `EquipResident(eq, person)` → `board_equip` |
| Depo: 4 hammadde kartı `CardMaterial` (`Mat_*`) + 6 kit yığını (`Kit_0–5`, ×n) | Hammadde kartı 0–3 ("STOK / BOŞ / AYRI"), Depo ızgarası | DEĞİŞTİR `StoreView`: büyük serbest sayı, birim noktaları (en çok 20, "+n"), ayrılan kırmızı halka, alt satırda stok / ayrı; ×0 sönük | `Material(kind).Quantity`, `BoardMaterialFree(kind)`; depodaki eşya (`OwnerId == null`) |
| Hurda (`Hurda_Slot`, HurdaLid 28°) | `Depo/Geri dönüşüm` (610 × 32 px) | DEĞİŞTİR `HurdaBin` | `ScrapEquipment(id)` → `board_scrap` |
| Eğitim konuları (`Topics`) + kalem halkası | `Görev · train` / `Eğitim · 0..6` / `Kalemle seçim` | DEĞİŞTİR `TrainingTopics`: 7 satır, aralık 16 mm | `TrainResident(person, subject)` → `board_train` |
| Kişi dosyası `PersonFolder` (manila, Cover −176°) | `Seçilen kişi dosyası` (uGUI: Dosya incele, Erişim, Tahliye) + KİŞİ DOSYASI düğmesi; kalıcı dosya kapı pozunda (PBP:528–538) | DEĞİŞTİR `PersonFolderView`: kapakta ad; açılınca R10 kişi dosyası (TMP). İNCELE · ERİŞİM · TAHLİYE (TAHLİYE iki basış). Kalıcı dosya klasörde de okunur | `ReviewResident` (Soru 2), `RestrictResident(id, bool)`, `DismissResident(id)`; `Adm.File(id)` |
| Başlık arduvazı (tebeşir; 0,2 s silinme) | `Pano başlığı`, `Kaynak özeti`, `Plan durumu`, `Durum notu` | DEĞİŞTİR → `TXT_Header` "V.{n} · erzak {r} ({d}) · {a}/{c} kişi" | `S.Day`, `Shelter.Supplies`, `Sim.BoardFoodPreview` (kapanıştaki beklenen değişim; `Bd.LastSuppliesChange` değil), yaşayan / kapasite |
| PLAN AÇIK/KİLİTLİ plakası (Flap 0/180, 0,3 s) + kilit çubuğu (`TXT_LockTag` 2. satır neden) | yalnız metin, `Draggable = false` | YENİ | `Bd.Locked`; neden `Bd.Printed` ya da baskın emri |
| MASAYA DÖN levhası (BackSign) | `Masaya dön` düğmesi → `rig.ResetView()` | DEĞİŞTİR (levha `DeskTarget`); Esc ve sağ tık (Karar K7) | — |
| Pano lambası (spot, gölgeli; K1'de 2,4) | yok | YENİ (model ışık noktası) | `lightsRecipe.K4/K1` |
| Sürükle-bırak (4 / 10 mm kalkış, 3°; takas 0,32 s; geri 0,25 s kavis; oturma 0,06 s + "tık"; kilitliyken 2 mm titreme) | uGUI EventSystem, `rig.InputSuspended`, klavye Tab / Enter / ↑↓ | DEĞİŞTİR `BoardDragController`: pano düzlemine ışın, hedef dikdörtgenleri; aynı `Drop` çözümü; klavye yolu korunur | aynı komutlar |
| K4 kamerası (`Cam_K4` / `Look_K4`; K1 → K4 0,6 s, geri 0,5 s) | `Poses/K4`, geçiş 0,45 s | DEĞİŞTİR: `Shelf` yuvasının çapası modelin `Cam_K4`'ü, FOV 42,5; poz başına süre | token `cameras.K4` |
| Emekli görevler ve otomatik üretim | `PBP.RetireJobs` | KORUNUR. Soru 2 ve Soru 9'a göre değişebilir | — |

### 3.4 Tarif defteri ve üretim (R18; Aşama 5)

Soru 9'un **A** seçeneğine göre yazıldı. B seçilirse iş emri satırları `board_queue` / `board_reserve` / `board_cancel`'a
bağlanır.

| Revizyon 3 öğesi | Mevcut karşılık | Yapılacak | Veri kaynağı / komut |
|---|---|---|---|
| `RecipeBook` (A6; Cover −178°, Leaf −176°; `Book_Hook`; 3 açılım × 2 tarif) | `PlanningBoardMotion` defteri (300 × 170 px, kapak −165°, ÖNCEKİ / SONRAKİ / DEFTERİ KAPAT) | DEĞİŞTİR `RecipeBookView`: sekmeler ve kapak; sayfa dokusu `tarif_acilim_1..3` TR/EN; ekranda düğme yok | `BoardRecipes` (6 tarif) |
| Tarif kartı `CardRecipe` 112 × 80 | 440 × 390 px, sürüklenmiyor | A: TARİF kutusunda Core'un seçeceği tarif; defterden sürükleme yok. İlerleme kutuları otomatik yolda veri olmadığı için çizilmez | `AutomaticProductionRecipe` |
| İş emri formu (440 × 170 mm + klips; `Order_Recipe`, `Order_Inputs`) | `Üretim iş emri` (460 × 410 px), `Üretimdeki hammadde · 0..3` | DEĞİŞTİR `WorkOrderView` | `Bd.ProductionInputs`, üretim ekibi (`job == "craft"`) |
| Hammadde fişi `CardChip` 56 × 40 (bir adet) | Üretimdeki hammadde kartı (miktar) | DEĞİŞTİR: her fiş +1; fişi geri almak −1 | `StageProductionMaterial(kind, n)` → `board_stage` |
| Yetersiz kaynak: fiş kalkmaz, kart 2 mm titrer, sayı 0,2 s kırmızı | `board_stage` reddi → durum metni | DEĞİŞTİR (sunum) | `BoardMaterialFree(kind) == 0` |
| Üretilmeyecek iş uyarısı (tasarımda yok, otomatik yol sessiz) | yok | YENİ (öneri): formda kırmızı kalem notu. Durumlar: tarife uymuyor; üretim ekibi boş; açlık; depo dolu | `AutomaticProductionRecipe`, `craft` kişisi, `Shelter.Starving`, `Equipment.Count` |
| Ürün depoya kayar (0,3 s, yeni vardiyada) | yok | YENİ | `Bd.LastCompleted` |

### 3.5 Vardiya kapanışı (R19; Aşama 4)

| Revizyon 3 öğesi | Mevcut karşılık | Yapılacak | Veri kaynağı / komut |
|---|---|---|---|
| Şalter durumları: hazır · kapak kalkık · fiş bekliyor · kilitli | `ShiftSwitch` (Idle, CoverOpen, Thrown, Disabled; `SetAwaitingReceipt`) | KULLAN + DEĞİŞTİR: parçalar modele; kapak üzerindeyken 3 mm; sesler Bridge'den | `Bd.Printed`, `S.Raid` |
| Plakanın 2. satırı (kapağı kaldır / kolu indir / fişi EVRAK'a koy kırmızı / neden) | `ShiftReceiptMachine.status` = `Plate/Text 1` (iki metin) | DEĞİŞTİR: dört metin + kilit nedeni; renk token'dan | `shiftClose.states.*.plate2` |
| `SlipMouth` (Lamp amber swap; `SlipStart` → `SlipExit`) | `Shift switch/Kâğıt çıkış pozu` (yerel −0,300 · 0,143 · 0,015); `Amber light` | MODEL + DEĞİŞTİR: çapalar modelden; amber lamba fiş EVRAK'a girene kadar yanık | `Bd.Printed` |
| `HandoverSlip` 100 × 150 mm (kıvrık; doku `devir_fisi_*`) | `Desk/Physical receipt` (`ShiftReceiptPaper`: düz quad + TMP) | MODEL + DEĞİŞTİR: yüz R10 düzeniyle TMP; doku yalnız örnek | `Sim.BoardReceiptPreview` (bugün gösterilen); `Bd.ReceiptId` |
| Fiş yolu: çıkış 0,35 s → elde 0,25 s → tepsiye 0,2 s; EVRAK dışında dile döner | `ShiftReceiptPaper.Present` / `ReturnToOutput`, `EvrakTray.ContainsReceiptDrop` | KULLAN + DÜZELT: dosyalanamayan her bırakmada `ReturnToOutput` (§1 bulgu 17) | `PrintShift` → `board_print`; `FileShift(id)` → `board_file` |
| EVRAK hedef ışığı (gölgesiz amber nokta #FFB44A, 0,55, 0,35 m) | yok | YENİ | `ShiftReceiptMachine.DragChanged` |
| Kararma 0,1 + 0,1 s (arada `Step`) + gong; teleks 1,2 s; sayaç tekerleri 0,4 s; kapak kendiliğinden kapanır | `SiginakShift.FileReceiptFlow` (0,2 s kararma, `FileBoardShift`, teleks, `Release`, `ResetView`); sayaç ve ses yok | DEĞİŞTİR: süreler bölünür; sayaçlar tutulur ve tekerle döner; ses | `GameController.FileBoardShift` |
| Yetersiz plan: kol iner, ağız boş "tık", teleks gerekçe, kol 0,4 s'de kalkar | `SiginakShift.Throw` → `PrintBoardShift` reddi | DEĞİŞTİR (sunum) | `BoardPlanCheck` |
| K6 pozu (0,40 · 1,28 · 0,42) → (0,665 · 0,82 · 1,08), 42° | `Shift` = `Poses/K6a` (0,765 · 1,65 · 0,43), 53° | YENİ poz (Karar K8) | `cameras.K6` |
| Ölçütler: fiş EVRAK'a girmeden gün ilerlemez; okumak zaman ilerletmez; aynı fiş yeniden kullanılmaz; kayıttan basılı fiş geri gelir | Bugün sağlanıyor (`FileShift` kimlik denetimi, `LastFiledReceiptId`, kayıttan açılışta `SetAwaitingReceipt`) | KULLAN; Aşama 4'te testle kilitlenir | — |

### 3.6 Kapı ve kabul (R9, R17; Aşama 6)

Aşama 6 sonrası güncel (2026-11-07; ayrıntı §12). Yollar `Assets/Corrigible/Desk/` altında; Core yolları `Core/`.

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
| Kişi dosyasında iz ("robot" yazmaz) | `PersonFolderView` (pano): KARAR satırı, bulgular koddan, İZLER bölümü, FileTag. R17f'nin açık dosya düzeni Aşama 7'de tamamlandı (§13) | UYGULANDI | `ResidentAdmissionFile`, `AdmissionFindingCode` |
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
| M0/M1/M2/MS/M3, K9 | sahne çapaları + token'lar | UYGULANDI | M0 60°, M1/K9 48°; okunurluk kalibrasyonu §14 |
| Ortak sahanlık prefab'ı | iki ayrı kurulum | KARAR KORUNDU | Aşama 0: menü sahanlığı ve iç koridor ayrı kalır |

### 3.9 Baskın (R11, R12; Aşama 9)

| Revizyon 3 öğesi | Mevcut karşılık | Yapılacak | Veri kaynağı / komut |
|---|---|---|---|
| Tahta kurucusu | `RaidBoard3D` (2153 satır). Kare 1 birim, `W(x,y)`. Bileşenler: `Build` (karo, vurgu, `RaidArrow` işaretleri, pafta yazıları), `Sync` (yeniden giydirme, gedik molozu, sis, nesneler, birimler, niyet okları), `LayoutLabels`. Duvar tepesi 0,56 | DEĞİŞTİR (yalnız kaynak değişir): `lib.*` alanları modellere; kurulum mantığı aynı | `RaidPieceLibrary`, `RaidRoomKit` |
| Kit modelleri (12 kit + varsayılan): `Relief`, `ReliefIso`, `Bridge`, `Rubble` | `RaidRoomKit` (`Settings/Kits/Kit_*.asset`: malzemeler + `WallRubble_<id>`), `RaidTileSkin` (`Wall.prefab` Body/BridgeE/BridgeN/ShelfGear; ayrık blok kolon malzemesi), `RaidTileMask` | MODEL + DEĞİŞTİR: kabartma ve ayrık kolon kit başına mesh; köprü ve moloz modelden; maske kuralı aynı | `lib.KitFor(region)` |
| `BoardBase`, `Tile`, `WallBlock` | `BoardBase` (Plywood, Ozalit, PlyWest/South), `lib.floor`, `lib.wall` (`WallBlock` mesh) | MODEL | — |
| Sivil cihazı (`Civil_*`, 13) | tek `Civilian` prefab | MODEL (kit başına) | bölge |
| Giriş kapısı (`EntryDoor_*`, 13) | yok (yalnız kaçışta yeşil kesik çerçeve) | YENİ; yükseklik Karar 2 | giriş satırı |
| Dolaplar: `CoreCabinet`, `NodeCabinet`, `CoolingUnit`, `CopyCabinet`, `BackupCore` | `lib.core`, `lib.node`, `lib.cool`, `lib.spawner` (`RaidPieceLibrary.cs:135–141`) | MODEL. **`BackupCore`'un oyunda karşılığı yok** (Soru 15) | `Baskin` nesne türleri |
| 8 makine siluet modeli (Drone, ArmorDrone, ShieldDrone, MaintRobot, Locker, Mortar, Replicator, Bomber) | `RaidPieceLibrary`, 8 prefab | MODEL | `Baskin.UnitTypes` |
| Piyonlar `Pawn_T/E/A` (Letter, Tilt, Tape, Tick, Plate) + ad şeridi (TMP 14 px) | `Technician`/`EmpSpecialist`/`Analyst.prefab`; tahtada kişi adı yok (yalnız HUD) | MODEL + YENİ ad şeridi; tur bitti yüzü Karar 3 | `Unit.PersonName` |
| Madenci ve sis piyonları kalkar | `Miner`, `SmokeSpecialist.prefab`; `Baskin.cs:47–48`; dinamit ve sis kodu | Kartlı oyunda gelmez (`FieldJobs = {tek, emp, ana}`); prefab'lar eski yol için durur | — |
| İşaretler: `MoveQuad`, `HitQuad`, `GhostRing`, `WakePin` | `RaidArrow` prosedürel tarama, halka ve çerçeveler (`RaidBoard3D.cs:214–225`) | Öneri: düz quad olanlar (Move/Hit) model, halka ve iğne model; oklar `RaidArrow`'da kalır | `Baskin.Intents` |
| Olay nesneleri: `Tube`, `WeldArmBase` + `WeldArm`, `CeilingDebris`, `LootCrate` / `LootCrateOpen`, `SlidingShelf` (Doğu Avrupa raylı raf) | tek prefab'lar; raf `ShelfGear` | MODEL | olaylar |
| Kanal (`ChannelHub/Arm/Edge/Single`) | `Cable.prefab` | MODEL (maske kuralı aynı) | `RaidTileMask` |
| Kâğıt paneller, antet, anahtar plan | `RaidHud.prefab`, `RaidPanels.prefab` | DEĞİŞTİR | `RaidViewModel`, `region.Building`, `Baskin.RoomName` |
| Baskın ışığı (gölgeli yön ışığı 4096 + dolgu + hedef spot) | `Desk Lamp`, `Cool Fill`, `BoardBackground/Lamp` | DEĞİŞTİR. Ana ışık gölge haritası `PC_RPAsset`'te 2048; 4096 ortak asset değişikliği (Risk R15) | `lightsRecipe.RAID` |
| Ölçek | 132,58 px/kare | KULLAN; ince parça 0,025 (Soru 7) | `tokens_sade.json → raid.camera` |

### 3.10 Doğrulama araçları

| Revizyon 3 öğesi | Mevcut karşılık | Yapılacak |
|---|---|---|
| Poz görüntüleri | `SiginakCapture.Render` (yalnız 3B, MSAA, TAA yok), `CorrigibleDebug.Capture` (3B + Overlay, Play'de), `SiginakCaptureSet` (S1a–S8b) | DEĞİŞTİR: Rev3 poz listesi; çıktı `screens_unity_rev3/`. TAA karşılaştırması `CorrigibleDebug.Capture` ile |
| Durum fikstürleri | `SiginakFixtures` (V014, V031, V015ca, K6, V019, injured) | DEĞİŞTİR: kartlı oyun fikstürleri (V.001, V.031, fiş bekliyor, kilitli, aday var / inceleme / damgalı, defter açık, iş emri dolu). Komut dizileriyle; simülasyon değişmez |
| Punto taraması | `SiginakTextScan` (≥ 14 px) | DEĞİŞTİR: kart adı ≥ 18 px (`board.readability`), Rev3 pozları; rapor `Docs/UI_Siginak_Rev3/punto_taramasi.txt` |
| Model denetimi | yok | YENİ EditMode: bbox ± 1 mm, üçgen eşit, parça aralıkları, `TXT_*` sayısı, çarpıştırıcılar |
| Gölgeli ışık sayısı | yok | YENİ EditMode: poz tarif asset'inden ≤ 4 |
| Kart yüzü, deste açılımı | `PlanningBoardCardTests` (8), `PlanningBoardLayoutTests` (2), `PlanningBoardSceneTests` (9) | DEĞİŞTİR: mm → RectTransform, min(82, 636/(n−1)); sahne testi yeni yapıya |
| PlayMode testleri (sürükle-bırak; şalter → fiş → EVRAK) | yok (yalnız EditMode asmdef'leri) | Karar K10 |
| Son tam EditMode koşusu | 546/546 (2026-11-05) | Token 1.3 ile `SiginakFoundationTests:193–194` güncellenir |

### 3.11 Girdi ve kısayollar

Her kısayolun tıklanan bir karşılığı var ya da olacak.

| Girdi | Bugün | Revizyon 3 |
|---|---|---|
| 2 | `FocusShelter` (K4), `PBP.Show`'u atlıyor | K4. Poza bağlı işler rig'de (§1 bulgu 11) |
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

### 3.13 Poz başına ışık (tarif özeti; asset'e elle yazılacak)

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

## 4. Açık sorular

1. **Eğitim konuları ve ustalık.**
   - Core'da 7 konu var (`PlanningBoardData.Subjects`): `tek`, `emp`, `ana`, `craft`, `cook`, `care`, `gather`. Yani
     Teknisyen, EMP, Analist, Üretim, Mutfak, Bakım, İkmal.
   - Bakım ve İkmal'in görev cebi yok; eğitim konusu ve ustalık satırı olarak duruyorlar.
   - **Öneri:** konu listesi 7 satır (aralık 22 → 16 mm), ustalık sütunu 7 satır, kademe 2 / 6 / 12.
   - Sorum: Bakım ve İkmal eğitimde kalsın mı? Kalkmaları kural değişikliği olur.
2. **DIŞ KAYIT gözetmeni.**
   - Gerçek kural (SCA:170): aday dışında, sağlam, kısıtsız ve Gözetim (`watch`) görevinde biri.
   - Bugün bu kişi olamaz (§1 bulgu 1). Tasarımcı da bunu açık bırakmış (`admission.supervisor`).
   - **A (öneri): gözetmen = Boşta'daki ilk uygun kişi.**
     - `Bd.People` sırasıyla belirlenir; deste aynı sırayı gösterir, "üst kart" = gözetmen.
     - Kural değişir: `CurrentVersion` 14 → 15. Biçim 14 hiç commit edilmediği için oyuncu kaybı yok (§1 bulgu 2).
     - Alternatif: `board_auto` gibi tek seferlik kayıtlı komut. Eski kuralı korur, ama `Replay.Restore` karşılığı ve iki kural
       ister. Denge alanı yolunu önermiyorum: `new Balance()` kullanan testler ve yedek yollar sessizce eski kurala düşer.
     - Kısıtlı sakinin çalışması için yine kişiye İNCELE gerekir; tek `ObservationTarget` yuvası paylaşılır. Bu ayrıca
       düşünülmeli.
     - Testler: `CommunityAdmissionTests`, `SaveStoreTests`, `PlanningBoardTests`.
   - **B:** panoya yedinci cep (GÖZETİM). Core değişmez, tasarım (6 cep, `layoutMm`) değişir.
   - **C:** DIŞ KAYIT ve İNCELE kapalı kalır, talep defteri "gözetmen yok" ile kilitli. Robotu ayırt etmenin bağımsız yolu
     kaybolur.
   - **Çözüldü (Aşama 6, K4-A; 2026-11-07):** Core'da `AdmissionWatcher(targetId)` = Boşta'nın (`Job == "rest"`) ilk canlı,
     sağlam, kısıtsız ve hedeften farklı kişisi, `Board.People` sırasıyla; `watch` artık sayılmaz (D1), kural dinamik (D2).
     Tek `ObservationTarget` yuvası korunur; H1 ve H3 düzeltildi, H2/H4 son istek kazanır ve sunum önceden uyarır (D3).
     `GameRecord.CurrentVersion` 15. Ayrıntı §12.
3. **Sayaçtaki MALZEME.**
   - Teker zaten dört hammaddenin toplamı.
   - **Öneri:** plaka HAMMADDE / RAW MAT. (`TXT_Label_3`, sunum).
   - Kırmızı nokta: "üretime konmuş ama bu vardiya üretilmeyecek". Durumlar: tarife uymuyor, üretim ekibi boş, açlık, depo
     dolu (§3.4). Soru 9'a bağlı.
4. **KAYIPLAR zarfı.**
   - Veri var (`Alive == false`). "KAYIP · V.nnn" baskın kaydından çıkar; Core değişmez.
   - Pakette model yok. **Öneri:** yeni geometri yerine `PersonFolder` modeli, kapak kapalı, `TXT_Face` "KAYIPLAR · n";
     panonun sağ ayağının yanında. Tık → kayıpların dar listesi (ad, KAYIP · V.nnn).
   - Ayrı model istersen three.js kaynağına eklenmeli (geometri bana yasak).
5. **Eski yol (`planningBoardEnabled = false`).**
   - Oyuncu bu yola giremiyor; sunumu zaten yarım (Shelf kapalı, `SiginakShelter` devre dışı).
   - **Öneri:** Core, kapının eski yolu ve `LegacySimulation` testleri yerinde kalsın. Revizyon 3 eski yolun görselini
     yükseltmesin.
   - Importer, eski yolun okuduğu üç değeri (`lights.roomBulb.k4Scale`, `colors.door.fatalRoomLight`,
     `motion.summaryStaggerS`) `_was_1_3`'ten korusun.
   - Kapalı raf ve maket nesnelerinin silinmesini Aşama 10'da ayrıca sorarım.
   - **Aşama 6:** eski kapı yolu dokunulmadı (D19): `SiginakDoor` eski dalı, `DoorViewBuilder`, siluetler ve ölümcül kapı aynen;
     `DoorTests` ve `SiginakDoorTests` değişmeden yeşil. Silme kararı Aşama 10'da.
6. **Tek Sahanlık prefab'ı.**
   - Bugün iki ayrı ilkel kurulum var; paketteki `Corridor` (1,64 × 4,0 × 3,37, 9 basamak) ve `Stairs` (0,98 × 2,79 × 2,95,
     10 basamak) da farklı.
   - **Öneri:** Aşama 6 ve 8'de ikisi kendi modeliyle kurulsun. Birleştirmek ayrı tasarım turu ister; PROMPT da "önce sor"
     diyor.
7. **Baskın ölçeği.**
   - Oyun 132,58 px/kare kullanıyor; ince parça 0,025 birimde kalır.
   - **Öneri:** `raid_tokens.json → camera`'ya dokunulmasın. `RaidKitPreview.cs:30` yorumundaki yanlış kaynak düzeltilsin.
8. **K5a kamerası.** Öneri (+0,14 m, 52°) ancak telsiz modeli yerindeyken doğrulanır. **Öneri:** Aşama 7'de iki görüntüyle
   karar.
9. **R18 üretim etkileşimi ile otomatik üretim.**
   - **A (öneri; bugünkü kural):**
     - İş emrinin TARİF kutusu konan fişlerden Core'un seçeceği tarifi gösterir.
     - Defter yalnız okunur; tarif kartı sürüklenmez, ilerleme kutusu yok (otomatik yol süre tanımıyor).
     - Fişler `board_stage` ile konur. Üretilmeyecek iş kırmızı notla söylenir.
   - **B (Core aynı, tasarıma birebir):**
     - Tarif kartını iş emrine sürüklemek `board_queue`, fişler `board_reserve`, geri alma `board_reserve n−1` /
       `board_cancel` olur.
     - Otomatik kip açık kalsa da emir önce işlenir. Yeni komut yok, `Replay` değişmez.
     - Kazandırdıkları:
       - "girdiler eksik" kırmızı notu `BoardPlanCheck`'in kendisi;
       - iki vardiyalık EMP hücresi ilerleme kutularıyla;
       - hız kademesi.
     - Bedelleri:
       - senin otomatik üretime geçişini oyuncu açısından geri alır;
       - konan fişler ve ayrılan girdiler iki ayrı havuz olur (eski kayıttaki konmuş fişler gösterilmeli ya da sıfırlanmalı);
       - tek emir kuralı geçerli.
   - **C:** otomatik yolda oyuncunun seçtiği tarif öncelik alır. Kural değişikliği, yeni komut.
10. **Emekli görevler.**
    - `PBP.RetireJobs` sunum katmanında komut kaydediyor.
    - Başlangıçta 6. ve 7. sakin `care` ve `gather` görevinde kuruluyor (SPB:38). Açılış belgesi kapanınca Boşta'ya alınıyorlar.
    - Revizyon 3 bunu değiştirmez. Core'un başlangıç dağılımı panoyla uyuşmuyor; bilgin olsun.
11. **ERZAK "/60" eki.**
    - Model "/60, /12, /20" eklerini `tx_counterFraction` dokusuna basılı istiyor; doku hücreleri "/30".
    - Bugün ek çalışma anında TMP ve denge değerinden (`boardSuppliesCapacity`).
    - **Öneri:** pencerede TMP ek kalsın, doku eksiz olsun. Basılı ek, `BalanceConfig`'teki değeri dondurur.
12. **Portreler.**
    - İki havuzda 32 benzersiz ad var (kurucu 10 + aday 24; Suna ve Eren ikisinde de). Portresi olan 10.
    - Eksik: Derya, Orhan ve 20 aday.
    - Seçenekler:
    - **A (öneri):** paketteki üretici (`threejs/r3/portraits.js` + `portraits_write.mjs`) çevrimdışı çalıştırılır; eksik 22
      ad için PNG üretilir. Yerel `node` ve `npm i` gerekir; gerekirse izin isterim. Kural:
      - portre ada bağlıdır;
      - robot ile insan aynı üreticiden gelir; robotluk portreye yansımaz.
    - **B:** üreticinin Unity'ye taşınması (çalışma anında çizim). Daha pahalı.
    - **C:** eksik adlara nötr siluet. Tasarımın "gri yer tutucu → mürekkep kimlik" hedefine aykırı.
    - **Durum:** A uygulandı (Aşama 3, §6.3 D8). Aşama 6: kabul belgesinde aynı portre; sayılı adlarda ("Suna 9") belirlenimci
      ayna portre (D18, `DocumentData.PortraitFlip`), robot durumundan bağımsız.
13. **Oyuncu ayarı: post-processing ve TAA.**
    - PROMPT "oyuncu kapatabilir" diyor ama böyle bir ayar yok. Dolap modelinde de yuva yok: 4 kanal, SES, DİL, MENÜYE DÖN.
    - **Öneri:** ayar eklenmesin. Görünüş iki durumda da test edilsin (Play'de TAA kapalı ve açık görüntü). Ayar istenirse
      dolaba yeni kontrol tasarımı gerekir.
14. **EVRAK kartlı oyunda ne tutar?**
    - Bugün fiş dosyalanınca EVRAK'a Rev2 biçiminde boş bir vardiya raporu ekleniyor; tıklayınca eski raporlar geziliyor. R10
      vardiya raporunu yalnız eski yola bırakıyor.
    - **Öneri:** EVRAK devir fişlerini yığar (StackLate). Tıklayınca son fiş okunur; Core değişmez.
    - `Bd.ReceiptText` devirde silindiği için (SPB:440) fişin metni dosyalanırken `liberation_desk.json`'daki rapor
      arşivine yazılır.
15. **`BackupCore`.**
    - Pakette yedek çekirdek dolabı var, oyunda karşılığı yok (`Baskin`'de nesne türü değil).
    - **Öneri:** içe aktarılır, kullanılmaz; tasarımcıya "hangi nesne?" diye sorulur.
16. **Revizyon 2'den devredenler** (Rev2 haritası §8):
    - **Q3** (K kısayolu): K, 4 ile eş anlamlı kalır.
    - **Q8** (GÜVEN şeridi): açık, Revizyon 3 değinmiyor.
    - **Q12** (onarım niyetinin rengi): açık; R11/R12 kâğıt panellerinde kendiliğinden gündeme gelir.
    - **Q13** (yumuşak gölge kalır): yeni ışık tarifleriyle yeniden ölçülür (Aşama 10).
    - **Q5, Q6, Q10** (ziyaretçi siluetleri, DEPO ölçeği, ölümcül kapı ışığı): kartlı oyunda konusuz, eski yolda kalır.
17. **(Aşama 1) Punto kuralı.**
    - PROMPT §4 punto için fontun gerçek büyük harf oranını istiyor (Courier Prime 0,59 · Big Shoulders 0,80 · Kalam 0,76).
    - three.js yuvaları `em = capMm / 0,7` ile çizip ölçmüş, sığmayanı da küçültmüş (`textCanvas`).
    - **Uygulanan:** gerçek oran, satır sarar (PROMPT §0 ölçüt 4: punto küçülmez).
    - **Sonuç:** 121 yazı yuvasının 20'si (TR) ve 23'ü (EN) tek satıra sığmıyor; listesi Aşama 1 raporunda (§7). Çoğu Courier
      plaka yazısı, taşma %5–40.
    - **Alternatif:** three.js kuralı (`em = capMm / 0,7`). Görüntülerle aynı ölçü, ama Courier büyük harfi `capMm`'nin
      0,84'ü kalır.
    - Plaka yazıları her biri yerleştirildiği aşamada ayrıca ele alınacak (ör. şalter plakası R19'da iki satır, 11,5 + 6,4 mm).
18. **(Aşama 1) Yuvanın fontu.**
    - Manifestin `font` alanı ile rolü çelişen yuvalar var: `TitleStencil`, `SettingsCabinet` ve kabul tenekesi alanda
      Courier, rolde "wallStencil (Big Shoulders …)".
    - Giydirme modülleri şablon fontuyla çiziyor, token `type.wallStencil` de Big Shoulders. **Uygulanan:** rol kazanır.
19. **(Aşama 1) Paketteki iki kusur; tasarımcıya bildirilmeli.**
    - **`HandoverSlip.obj` bozuk:** dışa aktarıcı indeksli `PlaneGeometry`'yi indekssiz yazmış, 64 yerine 15 yüz. İçe
      aktarıcı yüzleri kaynağın dizin sırasıyla yeniden kurar; köşe, normal ve UV aynı.
    - **13 modelde manifest kutusu geniş:** `Bomber`, `Civil_oc`, `LandingLamp` (24 mm), `LastSwitch`, `LootCrateOpen`,
      `MaintRobot`, `Microphone`, `PlanningBoard` (26 mm), `Replicator`, `Shavings`, `ShieldDrone`, `Stairs`, `Tube`.
      Neden: three.js `setFromObject` döndürülmüş parçada yaklaşık kutu. Doğrulama OBJ köşelerine karşı.
20. **(Aşama 1) Eksik dokular.** 45 malzeme anahtarı doku istiyor (`tx_board_face`, `tx_counterDigits`, `tx_hazard`, `tx_wood`,
    `tx_recipe_spread_1..3` …). Pakette yalnız R10 kâğıt PNG'leri ve portreler hazır. Öteki dokular three.js'te kodla
    üretiliyor (`prints.js`, `rev2/textures.js`). **Öneri:** her doku ilk kullanıldığı aşamada C# fırınlayıcıyla üretilir
    (Rev2'deki `SiginakTextureBaker` gibi); o zamana kadar düz renk.
21. **(Aşama 1) Görsel karşılaştırmada çıkan paket verisi sorunları.** Bunlar içe aktarma hatası değil; ilgili aşamada
    çözülecek ya da tasarımcıya sorulacak.
    - **`CabMenuLever`:** dinlenme +35° (manifest) kolu panelin içine gömüyor. Model sayfası kolu dik çiziyor. Şalterdeki
      +35° burada geçerli değil. Dolapta (Aşama 8) dinlenme 0° önerilir.
    - **`CabToggle`:** parça notu "−28 = yukarı (AÇIK)" diyor, geometri ve sayfadaki hayalet ise +28'de yukarı. Görünüm
      (Aşama 8) işareti çevirir.
    - **`RequestPad`:** Sheet −35°'de bloğun içine iniyor, not "kıskaçtan kalkar" diyor. Kalkması isteniyorsa +35° olmalı
      (Aşama 6). **Çözüldü (Aşama 6, D22):** `RequestPadTool` görünümde +35° çevirir, prefab değişmedi (`Rev3AdmissionMotionTests`
      yaprağın defter çerçevesinde > 50 mm kalktığını kilitler).
    - **`AdmissionPanel`, `AdmissionCounter`, `CounterLamp`:** duvar plakalarının yalnız duvara bakan (+z) yüzü var; odadan
      bakınca içi görünüyor. Sayfada da öyle. Tasarımcıdan −z kapağı istenmeli; yoksa Aşama 6'da çift yüzlü ayrı bir
      malzeme kullanılır. **Aşama 6:** çift yüzlü malzeme eklenmedi (W6 "yalnız görüntüde içi görünürse" koşuluna bıraktı;
      görüntü karşılaştırmasında ayrıca değerlendirilmedi) → açık, tasarımcıya bildirilecek.
    - **`TelexPaper`:** kâğıdın kaba mesh'i üst satır hizasında düz `TXT_Message` yuvasının ~3 mm önüne çıkıyor, alt
      satırlarda 7–10 mm geride kalıyor (three.js `depthWrite` kapalı çizdiği için görünmüyordu). Aşama 2'de teleks
      satırları kıvrımı izleyen ayrı yazılar olur.
    - **Yazı renkleri:** manifest rengi yalnız bazı rollerde veriyor. Öteki yuvalar mürekkep (#1E1B16) aldı; koyu bakalit
      üstündeki telsiz etiketleri bu yüzden okunmuyor. Renkler yuvanın yerleştirildiği aşamada giydirme modülünden alınır.
    - **`LogBook`:** kayıt satırları yuvanın ortasında (üstten başlamalı); saat sütunu Courier istiyor, yuva tek font.
      Aşama 7'de zengin metinle.
    - **Saydam ve cam malzemeler** (`CoffeeRing`, kart damgası, gözetleme camı, gösterge camı, telsiz camı) model
      sayfalarında soluk ve mavimsi. Neden: boş sahnenin gökyüzü yansıması. Oda ışığında yeniden bakılacak.
      **Aşama 6:** buğu camının modeldeki `Fog` meshi kapalı; yerine Ø 108 mm Quad, `Cold fog.mat` (URP Lit saydam, fırınlanmış
      `tx_fog`), alfa ve ölçekle (D9).

---

## 5. Riskler

| # | Risk | Etki | Önlem |
|---|---|---|---|
| R1 | Kart yüzü başına World Space Canvas (10 kişi + 32 eşya + 4 hammadde + fişler + 6 tarif ≈ 60) | K4'te çizim sayısı, canvas yeniden kurulumu | Statik içerik, kart başına alt tuval; Aşama 3'te 10 kişi + 32 eşya ölçümü. Gerekirse RenderTexture'a pişirme (onayınla) |
| R2 | Sürüklemenin uGUI EventSystem'den 3B ışına geçmesi | Klavye yolu, `rig.InputSuspended`, dosya/defter tıklamaları | Aynı `Drop` çözümü; klavye yolu korunur; PlayMode testi (K10) |
| R3 | `ShiftSwitch` üç yerde ortak | R19 değişikliği dolabı ve menü şalterini bozabilir | Yeni davranış alanla açılır (varsayılan eski davranış); üç yerin testleri |
| R4 | Token 1.3'ün KALDIRILDI düğümleri | Eski yol değerleri düşer (eflatun ölümcül ışık); `SiginakFoundationTests` kırılır | Importer `_was_1_3`'ü korur; test aynı adımda |
| R5 | Gölge atlası ve TAA | Nokta ışığı küp gölgesi; ince parçalar TAA'da titrer | Gölgeyi spot taşır; 2 cm altı gölgesiz; Play'de TAA kapalı ve açık görüntü |
| R6 | 198 model, 295 malzeme anahtarı | İçe aktarma süresi, çizim sayısı | Aile aile; paylaşılan malzeme; kit başına ≤ 9 |
| R7 | Sahne testleri yol adlarına bağlı | Model geçişinde kırılırlar | Aynı adımda güncelleme |
| R8 | Kayıp referans | `SaveScene` sonrası silinen nesneye referans fileID 0 olur, taramada görünmez | Her sahne değişikliğinden sonra HEAD sahnesiyle gelen-referans karşılaştırması (2026-11-05'te `ShiftSequence.amber`'ı bu buldu) |
| R9 | Rev2/Rev3 anahtarı | İki hiyerarşi, iki bağlama | Karar K1 |
| R10 | Font indirme | Şablon yazılar CP Bold'a düşer | Karar K6 |
| R11 | Kural değişiklikleri (Soru 2, 9C) | Kayıtlar başka oyuna döner | `CurrentVersion` 15; oyuncu kaybı yok |
| R12 | Ses klipleri yok | Mıknatıs, damga, kanat sessiz | Cue + yer tutucu |
| R13 | Fiş tepside kalabiliyor (bugünkü hata) | Fiş tutulamaz hâle gelebilir | Aşama 4'te her reddedilen bırakmada `ReturnToOutput` |
| R14 | Kartlar sırayla bağlı | Ölüm, hurda ya da kırılan zırhtan sonra kart başka kişiyi gösterir | Revizyon 3 havuzu kimliğe göre |
| R15 | Baskın 4096 gölgesi | `PC_RPAsset` ortak; DeskGame ve Giris de etkilenir | Ölçüp karar: 2048 yeterliyse dokunulmaz |
| R16 | Portre üretimi node/npm ister | Yerel araç kurulumu | Soru 12; çıktı PNG olarak projeye girer |

---

## 6. Kararlar (2026-11-06: kullanıcı önerileri onayladı)

### 6.1 Paketin önerdiği beş karar (PROMPT §3)

| # | Konu | Paketin önerisi | Bulgu | Önerim |
|---|---|---|---|---|
| 1 | M0 kamerası | (4,50 · 1,52 · 0,30) → (2,2 · 1,34 · 0,30), 56° | Bugün (4,30 · 1,66 · 0,20), 52° | **Yeni değerler** |
| 2 | Baskında giriş kapısı | alçak (≤ 0,30) ya da açık kasa | Bugün kapı yok; kamera güneybatıdan bakıyor | **Alçak 0,29**, 13 kapı aynı kural |
| 3 | Piyonun "tur bitti" yüzü | koyu kraft `r_pawnDone` #8C734E | Bugün `spentTint` (0,549 · 0,451 · 0,306) ≈ #8C734E | **Koyu kraft** (bugünküyle aynı) |
| 4 | Pano | ayaklı, (−1,62 · 0 · 1,70) yaw −48 | Bugün de ayaklı, eski rafın yerinde | **Ayaklı** |
| 5 | Kabul | kapıdaki tezgâh | Bugün kapı pozunda World Space pencere | **Kapıda** |

### 6.2 Benim sorduğum kararlar

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


### 6.3 Aşama 3 kararları (2026-11-06: kullanıcı önerileri onayladı)

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
---

### 6.4 Aşama 4 kararları (2026-11-06: kullanıcı önerileri onayladı)

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

### 6.5 Aşama 5 kararları (2026-11-06: kullanıcı D1'de B'yi, öteki önerileri onayladı)

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

### 6.6 Aşama 6 kararları (2026-11-07: kullanıcı bütün önerileri onayladı)

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

## 7. Aşama 1 · model hattı (sonuç, 2026-11-06)

**Yapılan:**
- **Token 1.3:** `tokens_siginak.json` ve `raid_tokens.json` yerinde. `SiginakThemeSetup` kaldırılan 26 düğümü
  `_was_1_3`'ten geri koyar. Temada yalnız K4 kamerası değişti (panoya bağlı); eski yolun değerleri aynı.
- **Font:** Big Shoulders Stencil Display ExtraBold. Kaynak `xotypeco/big_shoulders` statik OTF; Google Fonts'taki sürüm
  yalnız değişken ağırlıklı, TMP 800'ü seçemiyor. OFL lisansı `Fonts/OFL-BigShoulders.txt`.
  - `DeskFontSetup.BuildOne("BSS-ExtraBold")`: 148 karakterlik statik atlas, 7 işaret glifi DejaVu yedeğinde.
  - `DeskFont.StencilExtraBold` (enum'un sonunda), `DeskTheme.stencilExtraBold`. Token `type.wallStencil` artık bu font.
- **Model hattı** (klasörler §2):
  - Editör: `Rev3Manifest`, `Rev3ModelPostprocessor`, `Rev3MaterialSetup`, `Rev3ModelImporter`, `Rev3ModelSheet`.
  - Çalışma anı: `Rev3Model`, `PartDriver`, `Rev3TextSlot`, `Rev3MaterialLibrary`.
  - 198 prefab `Desk/Prefabs/Rev3/<aile>/` altında. Malzemeler `Desk/Art/Rev3/Materials/`: 340 anahtar → 335 malzeme;
    45'i yalnız token'da olan swap durumu; 6 baskın işareti Unlit; dokulu 45 anahtar şimdilik düz renk.
- **Doğrulama:**
  - Rapor `Docs/UI_Siginak_Rev3/model_import_report.txt`: 198 model, uyarı yok. Üçgen manifestle eşit; sıfır pozda kutu OBJ
    köşeleriyle ≤ 0,1 mm, manifest kutusunun içinde.
  - `Rev3ModelTests` (8 test): hiyerarşi, pivot, yuva yeri ve normali, çapa, ışık, 76 dönen ve 13 kayan parça aralığında,
    swap, `_AxisTest`, büyük harf yüksekliği ±%6, şablon fontu, malzeme değerleri, devir fişi.
  - Token 1.3 için iki test. Tam EditMode **556/556**.
  - Görüntüler: aile görüntüleri `screens_unity_rev3/A1_<aile>_TR/EN.jpg` (baskın aileleri yazısız, tek). Model sayfaları
    (her model 6 görünüm) geçici klasörde üretildi; `Rev3ModelSheet.RenderModelSheets` ile yeniden çizilir.
  - **Paketteki sayfalarla karşılaştırma:** dokuz ajan 198 modeli `modeller/sayfa/*.jpg` ile karşılaştırdı.
    - **182** birebir.
    - **13** küçük fark: cam ve saydam malzemelerin soluk görünmesi, yuva renkleri, defter hizası.
    - **3** sorun: `CabMenuLever` dinlenmesi, `CabToggle` işareti, `TelexPaper` yuvası.
    - Hepsi paket verisinden; Soru 21. Unlit ve gölgesiz malzemeler bu turda düzeltildi.

**Kararlar (bu aşamada; itiraz edersen değişir):**
1. **Punto:** gerçek büyük harf oranı (PROMPT §4), taşan yazı sarar (Soru 17). Tek satıra sığmayan yuvalar:
   - **TR (20):** DecisionStamp/Cap, DecisionStampRack/S1 ve S4, PlanningBoard/Back · P_emp · P_uret · S_tek/emp/ana ·
     Topics, RecipeBook/Cover, Crt/Plate, KeyRail/Key_1, ShiftSwitch/Plate, SlipMouth/Plate, TeamFigure/Letter,
     TelexPaper/Message, CancelSlip/Stamp, ShiftCard/Free, PencilNote/Note.
   - **EN (23):** ColdGlass/Plate, DecisionStamp/Cap, DecisionStampRack/S1 ve S2, RationTin/Stencil, PersonFolder/Face,
     PlanningBoard/Back · P_tek · P_emp · P_uret · Topics · P_order · LockTag, RecipeBook/Cover, Crt/Plate, Radio/Volume,
     CounterRack/Label_4, KeyRail/Key_1, SlipMouth/Plate, TeamFigure/Letter, TelexPaper/Message, CancelSlip/Stamp,
     PencilNote/Note.
2. **Font:** şablon rolü Big Shoulders (Soru 18).
3. **Satır ayırıcı:** notu "N satır" diyen örneklerde " / " satır sonudur (yalnız örnek metin).
4. **Devir fişi:** `HandoverSlip`'in yüzleri içe aktarıcıda onarıldı (Soru 19).
5. **Işıma:** ışımalı malzeme `RealtimeEmissive`. `None` ile `_EMISSION` test koşusunda bellekte sıfırlanıyordu.
6. **Prefab ışıkları kapalı:** açıp kapamak poz ışığının işi (Aşama 2). Spot yönü ve açısı nottan okundu; şiddet 1,
   tarif Aşama 2'de.
7. **Çarpıştırıcı:** prefab'larda yok; tıklanan parçaya kutu çarpıştırıcıyı nesneyi bağlayan aşamanın script'i ekler
   (hangi parçanın tıklandığı görünümün kararı).

**Aşama 2'ye devreden:**
- poz başına ışık tarifi asset'i;
- sayaç tekeri dokusu ve "/60" eki (Soru 11);
- teleks satırları (Soru 21);
- `tx_*` dokuları;
- odaya yerleştirme (tek seferlik script);
- `EdgeLine` poz kuralı.

## 8. Aşama 2 · komuta odası temeli (sonuç, 2026-11-06)

**Yapılan** (tek seferlik `Rev3Stage2` ile; script aşama sonunda silindi, kurallar AGENTS 5):
- **Yerleşim:** odanın modelleri manifestin `place` konumlarında; eski Revizyon 2 primitifleri silindi. Bileşen taşıyan kökler
  (`Desk/Shift switch`, `Desk/Teleprinter`, `Desk/Paper tray`, `Bunker door`, `CRT desk/CRT`, `Radio log`, `Planning board`…)
  ve adları kaldı; dışarıdan gelen bütün referanslar onlarda. Sahne referans denetimi (`Rev3SceneRefAudit`) her gruptan sonra
  çalıştı: boşalan ya da hedefi değişen her alan kasıtlı.
  - **Masa grubu:** `Desk`, `TeamFigure`, `DeskLamp`, `StampRack`, `Pencil`, `Shavings`, `Eraser`, `Mug` ×2, `CoffeeRing`
    (geç; teneke kutunun yerine), `CeilingBulb`, kabuk (`Duct` ×7, `DuctEnd`, `PipeRun`, `JunctionBox`, `StencilSign`,
    `Poster`).
  - **Sayaç ve duvar:** `CounterRack` (tekerler `CounterWheel` → `PartDriver`, rakam d = 36·d; FON'un virgülü modelin
    gövdesinde, EN'de kuyruğu sahnedeki bir örtü (`CounterModule.decimalPoint`) kapatır, nokta kalır), `KeyRail` + 4 `KeyKey`
    (ikon yuvası krem karo, koyu çizgi), `AlarmBoard`, `CorkBoard` (12 yuva, kart raptiye ekseninde döner), `ChalkTally`
    (16 çentik yeri, atlas hücresi). Revizyon 2'nin kablo borusu ve kablosu silindi (buat modelinin içinden geçiyordu).
  - **Şalter grubu:** `ShiftSwitch` (plaka iki satır: 11,5 + 6,4 mm; ikinci satır `TXT_Plate2`), `SlipMouth` (lamba
    `ShiftReceiptMachine.slipLamp`), `PaperTray` (geç yığın aşama nesnesi; A5 kâğıt yığının üstüne çıkar), `Telex` +
    `TelexPaper` (dört yuva; paketteki 6° yuva eğikliği sahnede düzeltildi, Soru 21), `TelexRoll`, `PaperPile`. Teleks
    kuyruğu ve yırtık kâğıtlar kalktı (şerit hep masadan sarkar).
  - **CRT köşesi:** `CrtTable`, `Crt` (ekran tuvali camın 10 mm önünde, 1024 × 802; ekran camı Revizyon 2'nin koyu camı),
    `Radio` (kadran ve gösterge dokusu `tx_dialScale`, `tx_meterFace`; kanal rakamları TMP; ibre 07'de), `Microphone`,
    `LogBook` (kayıtlar sayfa yuvasında tek metin, asılı girinti). K5a ve dalış çapası ekranla taşındı (token kuralı:
    0,62 m önü, 0,02 m altı; dalış 0,2 m önü).
  - **Kapı:** `DoorFrame`, `DoorLeaf`, `HazardHeader`, `KnockLamp`, `IntercomBox`; koridor `Corridor`, `Handrail`,
    `CorridorLamp` ×2 (yakın lamba gölgeli). `DoorProp` alanları model parçalarına; davranış aynı.
  - **Pano:** `PlanningBoard` (−1,62 · 0 · 1,70, yaw −48); kart tuvali iç yüze oturdu (600 mm = 1260 px, kilit
    kılavuzlarının önünde). K4 = modelin `Cam_K4` çapası, FOV 42,5. Model yazı yuvaları Aşama 3'te bağlanacak, şimdilik kapalı.
- **Poz ışığı:** `PoseLightRecipe` (`Desk/Data/Rev3/PoseLightRecipe.asset`) + `PoseLighting` (`SiginakRoom/Pose lighting`).
  Rig'in `PoseChanged`'inde taban şiddetleri geçiş boyunca karışır; gölge geçişin başında açılır, sonunda kapanır.
  - K1: ampul spotu (120°), masa lambası, pano lambası gölgeli (3); K4: yalnız pano lambası (1); diğer pozlar 2.
  - Ampul gölgeli 120° spot + gölgesiz dolgu, ikisi `BulbFlicker` üzerinden kırpışır.
  - Pano lambası koni 116° (three.js `SpotLight` açısı yarım açıdır: token 58° → Unity 116°; masa lambası 47° → 94° gibi).
  - Kalibrasyon referans görüntülere bölge parlaklığıyla: ampul spot 2,0 / dolgu 2,0, masa lambası 3,5, pano lambası 0,9
    (K4'te 1,0). three.js her malzemeye ortam haritası (0,22) ve ACES uyguluyor; URP karşılığı iki parça:
    - sahnenin üç renkli ortamı yükseltildi (gök ×2,5, ekvator ×5, yer ×4);
    - kamerayı izleyen gölgesiz bir **ortam dolgusu** (`Pose lighting/Env fill`, tarifte `envFill`: K1 0,4, K4 0,3, K8 0)
      kameraya bakan yüzeyleri (teleks kâğıdı, plakalar) aydınlatır. Oda gizlenince (baskın) söner.
  - K8 R9'a göre: ortam 0,3, ampul dolgusu 0,7 (duvar, kanat, CRT bölgeleri ±3 düzey). Pişirilmiş yansıma sondası denendi:
    plakalara katkısı 2 düzey, eklenmedi.
- **Kenar satırı:** K4'te gizli. K6 ve K8 sağ tıkla dönüş (K7) gelince eklenecek. Sayacı "HAMMADDE" (EN "RAW MAT."),
  sayaç plakasıyla aynı (Soru 3).
- **Kod (yalnız sunum):** `CounterWheel` (tambur), `CounterModule` (çocuk olmayan teker), `ChalkTally` (çentik yerleri),
  `BulbFlicker` (cam yuvası, dolgu ışıkları, taban şiddeti), `ShiftReceiptMachine.slipLamp`, `Teleprinter` (dört yuva),
  `CampaignWear` (tepsi kâğıdı yüksekliği), `RadioLogBook` (sayfa), `EdgeLine` (poz), `PoseLighting`, `PoseLightRecipe`.
  Core ve kayıt biçimi değişmedi.
- **Dokular:** `Rev3TextureBaker` sayaç teker şeritleri (`dress_room_desk.js` › `digitStrip` tarifi: 128 × 1408, 11 hücre,
  rakamlar CP-Bold SDF atlasından), çetele atlası, afiş, kahve halkası, akıntı, tehlike bandı, damga halkası, telsiz kadranı
  ve göstergesi, tuş ikonu karoları (krem zemin, koyu çizgi). İçe aktarmada NPOT küçültmesi kapalı. Sahneye özel malzemeler
  (`Hazard metric`, `Key icon tile *`, `Radio dial scale`, `Radio meter face`) `Art/Rev3/SceneMaterials/`.

- **Görüntüler** (`screens_unity_rev3/`): `A2_K1_once.jpg` (Revizyon 2), `A2_K1_V001.jpg`, `A2_K1_V031.jpg`, `A2_K4.jpg`,
  `A2_K5a.jpg`, `A2_K6a.jpg` (fiş basılmış), `A2_K8.jpg`; referansla yan yana `A2_K1_V001_R5a.jpg`, `A2_K1_V031_R5b.jpg`,
  `A2_K8_R9.jpg`; yakın `A2_sayac_yakin.jpg`, `A2_telsiz_yakin.jpg`. Play'de V.001 sayaçları R5a ile birebir
  (001 · 060,0 · 24/60 · 008 · 00/12 · 10/20); bölge parlaklığı K1'de toplamda 86'ya karşı 88.
- **Testler:** tam EditMode **562/562** (boş sahnede), metin taraması ihlalsiz, çeviri tablosu tam.
- **Denetim:** beş bağımsız gözden geçirici (kod, araç/test, şartname uyumu, belge, görüntü) ve her birine bir şüpheci;
  26 bulgu doğrulandı, hepsi düzeltildi ya da aşağıda açık kalan olarak yazıldı.

**Kararlar (bu aşamada; itiraz edersen değişir):**
1. **Devir fişi** (100 × 150 mm `HandoverSlip`) Aşama 4'e kaldı. O zaman elde okuma pozu da gelir; bugünkü fiş metni 13 mm
   em'le yazılıyor, 3,1 mm em'le K6a'dan okunmaz. Şimdilik Revizyon 2 kâğıdı yeni fiş ağzından çıkar.
2. **CRT ekranı:** tuval camın önünde (render texture yok). Kenarlarda yazı camdan 10 mm önde kalır, K1 ve K5a'dan fark
   edilmez. R14c'nin dalış parlaması (emission 1,4×) Aşama 7'de render texture'la gelebilir.
3. **Geç masa eşiği** V.016 (bugünkü aşama). İkinci kupa manifestteki yerinde (−0,70 · 1,47), kâğıt yığınının üstünde (4,7 cm); K1’de teleksin arkasında kalır (R5b gibi).
4. **Teleks çarpıştırıcısı** yalnız makineyi kapsar; şerit haritanın sol kenarına tıklamayı çalmaz.
5. **Kapının çarkı** manifestteki yönde (+450°) döner; Revizyon 2'de odadan saat yönündeydi.
6. **Telsiz düğmesi** "SES": tek Türkçe anahtar tek İngilizce karşılık taşır, EN'de "SOUND" (paket "VOLUME" diyor).
7. **Masa kupası** z 1,08 → 1,10: manifest yerinde şalterin arka ayağı kupaya ~1,5 cm giriyor.
8. **Plaka yazıları** (şalterin iki satırı, fiş ağzı, TELEKS, EVRAK) ışıklı TMP malzemesiyle: ışıksız koyu mürekkep gölgedeki
   plakadan açık kalıyordu.
9. **Teleks iletisi** 0,182 punto (büyük harf ~10,7 mm; tasarım 10 mm) K1'de 14 px; kod ve önceki satır tasarım boyunda,
   gerekçeli istisna.

**Açık kalan, sonraki aşamalara:**
- Pano kartlarının modele taşınması, yuvaların bağlanması (Aşama 3). Tuvalin bölgeleri modelin ceplerine oturmuyor.
- `HandoverSlip`, EVRAK hedef ışığı, şalterin bekleme rengi (Aşama 4).
- K5a'da defter kadraj dışında (Soru 8; Aşama 7'de iki görüntüyle).
- Koridor ve pano yüzü dokuları, `tx_telexPaper`; MASA ikonu Revizyon 2 çiziminden (tasarımın lambalı masası çizimle aynı fikir).
- Şalter plakası R5a'dan biraz koyu (58'e karşı 78), masa önü ve tuş plakaları biraz açık; ışık kalibrasyonu Aşama 7'de K5a
  ile birlikte gözden geçirilecek.
- Telsizin anteni ayarlar dolabının kanat yayına giriyor (dolap açılırken içinden geçer); Revizyon 3 dolabında da aynı yer.
  Aşama 8'de dolapla birlikte (tasarımcıya soru).
- Eski vardiya dizisi (`planningBoardEnabled = false`): `ShiftSequence` masa lambasını kendi tabanıyla kısar; poz ışığı aynı
  şiddeti yazmadığı için çakışmaz, ama karartmayı tabana çarpan olarak uygulamak Aşama 4'te.
- Revizyon 2'nin fikstürleri (`SiginakFixtures`: V014, V031…) pano kipinde günü ilerletemez (`Step` fiş dosyalanmadan durur).
  Döngü sonsuza gidip editörü kilitliyordu; artık hata verir. Fikstürler Aşama 4'te pano kipine uyarlanmalı.

## 9. Aşama 3 · kartlı pano ve kartlar (sonuç, 2026-11-06)

**Yapılan** (tek seferlik `Rev3Stage3Cards`, `Rev3Stage3Board` ve denetim için `Rev3Stage3Check`; üçü aşama sonunda silindi):
- **Veri yolu:** `PlanningBoardSnapshotBuilder` (Bridge/Board3D) simülasyondan yerelleştirilmiş bir `BoardSnapshot` kurar
  (RNG çekmez, komut yazmaz); `BoardView3D` yalnız onu çizer. Bırakma: `BoardDragController.DropRequested` → sunucu →
  `BoardDropRules.CommandFor` → `GameController.ExecuteBoardCommand` → `drag.Resolve(ok)`. Önizleme metni
  `BoardPreviewText` (mavi çerçeve; uyarıda amber: "takas: Deniz → EMP UZMANI, Analist kiti onunla gider · KİT YOK…").
  Seçim, eğitim konusu ve fiş yerleri sunumdur (`BoardUiState`, `planning_board_layout.json` v2). KAYIP · V.nnn
  `BoardDeathLog` ile baskın kaydından çıkar. Çizim yalnız değişimde (oyun `Changed`, yükleme, dil, poz, sürükleme sonu).
- **Kartlar:** `Prefabs/Rev3Board/{PersonCard, ItemCard, MaterialCard, ChipCard}` = paketin kart modeli (iç içe, geçersiz
  kılmasız) + `BoardCardView` + yüzde World Space tuval (1 birim = 1 mm) ve altında pişirilmiş, sözcüksüz ışıklı taban
  malzemesi. Yüzler `PersonCardFace`, `ItemCardFace`, `MaterialCardFace`, `ChipCardFace`: yalnız değişince bağlar
  (`CardFace.ChangedBinds`). `CardStateOverlay`: YARALI bandı, YATIYOR damgası, KISITLI ve KAYIP köşesi (köşe varken
  mıknatıs çizilmez), İNCELE bayrağı (model parçası 6 mm yukarıda gösterilir; ataş adı örtmez), seçim çerçevesi.
  Portreler `PortraitLibrary` (32 kişi, paketin üreticisi; 512, sıkıştırılmış, mip'li; kırpma döşemesi). Havuzlar Core
  kimliğine göre.
- **Pano bileşenleri:** `BoardDragController` (pano düzlemine ışın, cep dikdörtgenleri + çarpıştırıcılar; 4/10 mm kalkış,
  3° eğim, tek komutla takas, 0,25 s geri kavis, kilitliyken 2 mm titreme, klavye yolu), `BostaDeckView` (kart başına
  0,32 s, 0,07 s ara; elden kart alınınca kalanı toplanır), `StoreView` (4 hammadde + 6 kit yığını), `WorkOrderView`
  (fişler), `TrainingTopics` (7 satır, kalem halkası), `PersonFolderView` (kapakta ad; açılınca okuma pozu; İNCELE ·
  ERİŞİM · TAHLİYE, TAHLİYE 1,2 s basılı), `LostEnvelopeView` (KAYIPLAR, dosyanın arkasında; D5), `HurdaBin`,
  `BoardMarks` (cep vurgusu, KİT · GEREKLİ); başlık arduvazı, PLAN AÇIK/KİLİTLİ kapağı, kilit çubuğu ve etiketi, MASAYA
  DÖN levhası. Sesler `BoardSound` → `Sfx` 97–106.
- **Kamera:** K4 FOV 41° (D1); `PoseSeconds` K1 → K4 0,6 s, geri 0,5 s; `BackClicked` (Esc, sağ tık, MASAYA DÖN); K4'te
  yakınlaşma ≤ 1,0.
- **Doku ve sahne:** `Rev3BoardTextureBaker` (pano yüzü 2048 × 1024 sözcüksüz; kart tabanları; 24 sprite). 25 plaka yuvası
  giydirildi, 7'si kapalı. Eski "Serbest pano kartları" tuvali gizli; kapının kabul penceresi Aşama 6'ya dek eski
  `PlanningBoardView` üzerinden. Tarif defteri D6'daki gibi geçici tuval.
- **Kabul sırasında düzeltilenler:**
  - Işıklı TMP malzemelerinin (`CP-Bold SDF URP Lit`, `BSS-ExtraBold SDF URP Lit`) dış çizgi renkleri varsayılan camgöbeğiydi:
    genişlik 0 olsa da yumuşak kenar piksellerine karışıyor, küçük koyu yazı (plakalar) mavi-yeşil görünüyordu. Saydam yapıldı.
  - Pano lambasının gölge yakın düzlemi 0,05 → 0,1: lambanın kendi siperi panonun üst ortasına V biçimli gölge düşürüyordu.
  - EN değer etiketleri (HP · MOVE · POWER) −6 harf aralığıyla; CP-Bold eşaralıklı, hücreleri kenara kadar dolduruyordu.
  - İNCELE sözcüğünün sığdırılması çizilen sınırla ölçülür: TMP'nin `preferredWidth`'i 0,01 birime yukarı yuvarlar, metre
    ölçekli 3B sözcükte bu 10 mm demek.
  - EN kısaltmalar: "WORK ORDER · PROD." (kıskaç çubuğunun altına giriyordu), şerit "Restr. · {0} sh." / "Restr. · ended",
    üç eşya kullanım satırı (en çok 3 satır).
- **Görüntüler** (`screens_unity_rev3/`): `A3_K4_V001_TR/EN` (R15a), `A3_drag_hover`, `_held`, `_target`, `_swap_preview`,
  `_swap_mid`, `_swap_done`, `_invalid`, `_invalid_return` (R15b), `A3_deck_open`, `_taken`, `_after` (R15c), `A3_locked`,
  `_EN` (R15d), `A3_fixture_max_TR/EN`, `_deck` (R15e; sunum fikstürü), `A3_folder_cover`, `_open`, `_open_EN` (R15f),
  `A3_R16a_TR/EN` (kişi kartı durumları), `A3_R16b_TR/EN` (hammadde, kit, fiş), `A3_R16c_portreler`.
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
- **Testler:** tam EditMode **638/638** (boş sahnede), PlayMode **6/6** (`uptest`), çeviri tablosu tam (1846 metin).
  Yeni: `Rev3BoardLayoutTests`, `Rev3BoardSnapshotTests`, `Rev3BoardDropRulesTests`, `Rev3CardFaceTests` (TR/EN taşma
  geçidi dahil), `PlanningBoardSceneTests` (yeniden yazıldı), `PlanningBoardLayoutTests` (+v2), `SiginakCameraTests`
  (+3), `SiginakTextScanTests` (+2), PlayMode `BoardCameraPlayTests`. `SiginakRoomTests` pano girdi alanlarını tanır.

**Açık kalan, sonraki aşamalara:**
- **Kişi dosyasının biçimi** R15f'ye uymuyor: içerik doğru (künye, ustalık, teçhizat, kalıcı dosya, kayıt, sekmeler) ama
  sayfalar düz renk etiket/değer listesi; R15f'nin form düzeni (KYGA başlığı, portre, CAN/HAR/GÜÇ kutusu, ustalık çubukları,
  geçmiş, izler, damgalar, sekmelerin alt satırı) yok. Kâğıtlarla birlikte (Aşama 7) ya da ayrı bir turda.
- PLAN KİLİTLİ kapağının yazısı R15d'den soluk (kapak ışıkta açık, krem yazı karşıtlığı düşük).
- R15e'nin geç panosu yalnız sunum fikstürüyle (`A3_fixture_max_*`): gerçek Core'da fiş ve tarifli iş emri Aşama 5'te.
- PlayMode'da sürükleme testleri (planın P5–P9) yazılmadı; sürükleme durumları Play'de elle doğrulandı (görüntüler yukarıda).
- En dolu panoda 902 toplu çizim (V.001'de 472): kart başına ~13. Kare süresi tutuyor; cilalamada yeniden bakılabilir.
- Kart TMP malzemesi 4 (hedef ≤ 3).

## 10. Aşama 4 · vardiya kapanışı (sonuç, 2026-11-06)

**Yapılan** (tek seferlik `Rev3Stage4Slip`, `Rev3Stage4Scene` ve denetim için `Rev3Stage4Check`; üçü aşama sonunda silindi). Core ve
kayıt biçimi (14) değişmedi; yalnız `PrintBoardShift` ve `FileBoardShift` çağrılır.
- **Şalter** (`ShiftSwitch`, R19 kipi `shiftClose`; dolabın MENÜYE DÖN'ü ve menünün son şalteri eski kipte): hazır · kapak kalkık · fiş
  bekliyor · kilitli · ret durumları (`ShiftPhase`); kapak 0 → 112° 0,25 s, kapanış 0,3 s, kendiliğinden kapanma 4 s; kol +35 → −35° iki
  aşamalı 0,3 s; üzerine gelince ön kenar 3 mm; kilitliyken tık 2 mm titreme + "klak"; basım reddinde kol 0,4 s'de geri kalkar. Süreler
  token'dan (`motion.shiftClose.*`, `shiftClose.states.*`). Plakanın 2. satırı `ShiftPlate`: kapağı kaldır / kolu indir / fişi EVRAK’a koy
  (kırmızı #7A1810) / kilit nedeni (D2), punto sabit 6,4 mm büyük harf, sarmasız; üzerine gelme satırını yalnız o yazar.
- **Fiş** (`ShiftReceiptPaper`, `ShiftReceiptMachine`, `EvrakTray`): kol −35°'ye varınca ağzın 40 mm içinden dile 0,35 s ease-out
  (D12: 160 mm), dilde üst kenar ağızdan uzakta; tık → el (0,25 s; ekranın %67'si, kamera eksenlerinde, x 0°, z −2,9°); tık / Esc /
  sürüklemesiz sağ tık → dile; sürükleme dilden ve elden; EVRAK üstünde gölgesiz amber nokta ışık (#FFB44A, 0,55, 0,35 m); EVRAK dışında
  ya da reddedilen her bırakışta 0,25 s kavisle dile (bulgu 17); kabulde tepsiye 0,2 s. Fiş ağzı lambası fiş EVRAK'a girene dek yanık.
  Fiş `LateUpdate`'te yürür (el canlı kamerayı bir kare gecikmesiz izler).
- **Fiş yüzü** (`HandoverSlipFace`, `HandoverSlipRow`, `HandoverSlipWarning`, `HandoverSlipFormat`; prefab `Prefabs/Siginak/HandoverSlipView`,
  `Handover slip.mat`, `tx_handoverSlip.png` + 5 sprite): R10 devir fişi düzeni mm'si mm'sine World Space tuvalde (1 birim = 1 mm), her
  satır modelin 4 mm kıvrımının 0,4 mm önünde (D4). Veri `HandoverSlipFactsBuilder` (Bridge, saf; gizli gelecek okunmaz) → dilden bağımsız
  `HandoverSlipFacts` → o anki dilde satırlar. DOSYA uyarıları tür başına tek kutu, punto küçülmez (D9).
- **Akış** (`SiginakShiftClose`, Bridge): basım, kilit nedenleri (baskın > harekât bitti > belge açık > rapor açık > plan; D1/D2), dosyalama
  zaman çizelgesi (bırakış → tepsi 0,2 s → kararma 0,1 s → **tam karanlık bir kare** → gong, `FileBoardShift`, arşiv, K1'e kesme → bir kare
  daha karanlık → açılma 0,1 s → şalter hazıra 0,3 s, sayaçlar 0,4 s, teleks "VARDİYA DEVREDİLDİ" 1,2 s), eldeyken Esc ve sağ tıkı kendisi
  okur, EVRAK'tan son fişi salt okunur okuma (D7; ← emir belgesine), son 5 fiş `liberation_desk.json`'da. Sesler `ShiftSound` → `Sfx`
  107–114 (yer tutucu; kol geçici `SwitchClack`; D10). `SiginakShift`'in kartlı fiş kodu buraya taşındı; eski yol aynen.
- **Kamera:** yeni poz `SiginakPose.ShiftClose` (K6: (0,40; 1,28; 0,42) → (0,665; 0,82; 1,08), 42°), çapa `SiginakRoom/Poses/K6`; poz ışığı
  K6a'nın kopyası, ortam dolgusu 0,3 (kamerayı izleyen dolgu eldeki fişi aşırı aydınlatıyordu; R19b_elde ve R19a_inik'e göre). Kenar
  satırı K6'da gizli. Eski `Poses/K6a` yalnız eski yolda.
- **Sahne:** `Physical receipt`'in Revizyon 2 küpü ve tuvali kalktı, yerine fiş prefab'ı; EVRAK'ta `Slip over`, `Slip target light`,
  `Filed slip`; öksüz ikinci karartma ve `Kâğıt taşıma düzlemi` silindi; referans denetimi yalnız dört beklenen fark.
- **Kabulde düzeltilenler:**
  - Fiş yüzünün yazıları ışıksız font malzemesiyle: ışıklı TMP gölgelendiricisi World Space **uGUI** tuvalinde mürekkebi soldurdu
    (K6 elde ad satırı #1E1B16 yerine ~(175, 166, 151), kâğıt (209, 209, 198)). Kullanılmayan `CP-Regular SDF URP Lit.mat` silindi.
  - Kararma tam siyaha hiç ulaşmıyordu (alfa en çok 0,5): dosyalama ve kesme aynı ağır karede açılmayı da başlatıyordu.
  - SlipMouth plakası EN "HANDOVER" harf harf bölünüyordu (HANDOVE / R) → "HAND-OVER" (iki satır; yerel token ezmesi).
  - P1'in ease-out ölçüsü (örnek LateUpdate'ten önce alınıyordu) düzeltildi.
- **Görüntüler** (`screens_unity_rev3/`): `A4_R19a_hazir` (+`_EN`), `_hover`, `_kapak`, `_inik` (+`_EN`, `_devam` kayıttan devam),
  `_kilitli` (+`_EN`; plan düzeltilmeli); `A4_R19b_cikis` (0,17 s), `_elde` (+`_EN`), `_evrak`, `_birakildi`, `_kararma`, `_yeni_vardiya`;
  `A4_R19c_V001/V031_TR/EN` (elde, fikstür) ve `_yan` (düz çizim, tasarım PNG'siyle yan yana); `A4_R19_evrak_arsiv`.
- **Ölçüler (Play, 1920 × 1080):**
  - Fiş elde: ad 18,2 px (≥ 18), öteki 14,1 px (≥ 14), TR ve EN, V.001 / V.031 / en kötü fiş, 0 ihlal. Plakanın 2. satırı K6'da 23,5 px,
    K1'de 14,4 px. Editör taraması 1658 metin, 0 ihlal, 143 gerekçeli istisna.
  - Kol tıkı → kol −35° 0,30 s, çıkış 0,35 s (yarı sürede ~%80); el 0,25 s; tam karanlık bırakıştan 0,30 s sonra, gün ilerlediği karede
    alfa 1,00; K1'e kesme karanlıkta.
  - Ölçütler: fiş EVRAK'a girmeden gün ilerlemedi (dil, el, EVRAK dışı bırakış: komut 0); fişi okumak zaman ilerletmedi; EVRAK'a
    bırakış tam bir `board_file`; aynı fiş yeniden kullanılmaz (Core testleri + P7); kayıttan devam basılı fişi dile ve şalteri bekleme
    hâline anında getirdi (fiş no ve komut sayısı aynı).
- **Testler:** tam EditMode **717/717** (boş sahnede) ve PlayMode **20/20** (`ShiftClosePlayTests` 14 + `BoardCameraPlayTests` 6). Yeni EditMode:
  `Rev3ShiftSwitchTests`, `Rev3SlipMotionTests`, `Rev3HandoverSlipFaceTests`, `Rev3HandoverSlipFactsTests`, `ShiftCloseCoreTests`,
  `Rev3ShiftCloseFlowTests`, `Rev3ShiftCloseSceneTests`; güncellenen sahne testleri (`PlanningBoardSceneTests`, `SiginakRoomTests`,
  `SiginakCameraTests`, `SiginakShiftTests`, `Rev3RoomTests`, `SiginakBriefingTests`). Çeviri tablosu tam (1881 metin; 36 yeni).
- **Fikstürler:** `HandoverSlipFixtures` (R19c V.001 / V.031 / en kötü; `Present` Play'de, `RenderFlat` düz çizim), `SiginakFixtures`
  artık kartlı kipte günü fiş basıp dosyalayarak ilerletir.

**Tasarımdan bilinçli sapmalar:** fiş yolu 160 mm (D12); kilitli titreme üç tümsek (panonunki gibi); fiş yüzü ışıksız mürekkep; K6 ortam
dolgusu 0,3; EN "HAND-OVER"; `motion.shiftClose.slipPickS` (sürükleme başı 0,1 s) temada yok, kod yedeği.

**Açık kalan, sonraki aşamalara:**
- Sesler yer tutucu (107, 108, 110–114 klipsiz; 109 geçici `SwitchClack`).
- Uyarı kutularının çerçevesi kesikli çiziliyor (tasarımda düz çizgi).
- K6'da ekranın altındaki teleks şeridi (Revizyon 2 kalıntısı) kapanış görüntülerinde görünüyor.
- R19b2'nin iki öğesi: "tamamlanan ürün depoda YENİ etiketiyle" (Aşama 5) ve kilit çubuğunun geri kayışı (kartlı pano K4 dışında anında
  oturur; K1'den görülmez).
- EN en kötü fişte uzun kursiyer adı + "→ EXPERIENCED" EĞİTİM satırını sardırabilir (~0,2 mm taşma riski; W3 notu).
- Aşama 2'den devreden eski `ShiftSequence` karartmasının poz ışığına çarpan olarak uygulanması yalnız eski yolda; bilinçli ertelendi.

## 11. Aşama 5 · tarif defteri ve üretim (sonuç, 2026-11-07)

**Yapılan** (tek seferlik `Rev3Stage5Book`, `Rev3Stage5Cards`, `Rev3Stage5Scene` ve denetim için `Rev3Stage5Check`; dördü aşama sonunda
silindi). Core ve kayıt biçimi (14) değişmedi. **D1 = B**: tarifi oyuncu seçer; yalnız var olan komutlar (`board_queue`, `board_reserve`,
`board_cancel`; eski kayıtlar için bir kez `board_stage(kind, 0)`).
- **Defter** (`RecipeBookView`, `RecipeBookPage`, `BookPageShape`, `BookTypes`): paketin `RecipeBook` modeli kancada (Aşama 3 pozu);
  tık → okuma pozuna 0,35 s (kapak örtüşük, −178°), sayfa köşeleri ve ←/→ ile çevirme 0,25 s (Leaf altında iki sayfa dörtgeni),
  kapanış 0,3 s; dışarı tık / Esc / sağ tık / MASAYA DÖN kapatır; açıkken pano tıkları önce defteri kapatır (defter kipi,
  `BoardView3D.SetBookMode`). Sayfalar World Space TMP tuvali (1 birim = 1 mm, ışıksız font malzemesi) + pişirilmiş sözcüksüz taban
  (`tx_recipe_page`, `Recipe page L/R.mat`), içerik `BoardRecipes`'ten (D4). Kapak sözcüksüz `tx_book_cover` + TMP (D12).
- **Tarif kartı** (`RecipeCardFace`, `Prefabs/Rev3Board/RecipeCard.prefab`, 112 × 80 mm): sayfanın tarif kutusundan kopya kalkar
  (defter alçalır), iş emrine bırakılınca `board_queue`; dolu emre iptal + yeni emir (D3); formdaki kart dışarı = iptal (önizlemeli).
  İki vardiyalık tarifte ilerleme kutuları (D7). Ad 12,1 mm em (K4'te ≥ 18 px), değerler 9,4 mm (D6).
- **İş emri ve fişler** (`WorkOrderView`, `StoreView`, `BoardDropRules`, `BoardPreviewText`, `ProductionForecast`): hammadde fişi
  `board_reserve` ±1; serbest stoğu 0 olan kart kalkmaz, 2 mm titrer ve büyük sayısı 0,2 s kırmızı yanar; form durumları (boş · tarif var ·
  girdiler eksik · tam · başladı · durdu · tamamlandı), mavi kalem notu, kırmızı DURDU damgası (D8); eksik emir şalteri kilitler
  (Aşama 4 D1).
- **Ürün kayışı:** kapanıştan sonra K4'e ilk gelişte ürün kartı TARİF kutusundan depoya kayar, YENİ etiketi (`NewItemTag`) ilk dokunuşta
  düşer (D9); durum `planning_board_layout.json` v3'te, oyun kaydına girmez.
- **Sesler:** cue 115–119 yer tutucu (D14). **Token:** `_local_a5` yerel anahtarları (D15).
- **Kabulde düzeltilenler:**
  - Defter kapağı açık uca varınca `PartDriver.Value` 0'da kalıyordu (`Mathf.Approximately` 0,99999 → 1 geçişini değişmedi sayıyordu).
  - Defter açıkken dosya kapağının yerine tık geçiyordu: kişi dosyası ve KAYIPLAR'ın sayfa yüzeyi çarpıştırıcıları yalnız açıkken.
  - EN İŞ EMRİ sekmesi taşıyordu: sekme metne göre uzar (punto küçülmez).
  - Tarif kartı: ad 12 → 12,1 mm (18,0 px sınırdaydı), alan 81 mm; çıktı sütunu sağ kenarı 106 → 107 mm (süre ile arası 2 mm).
  - Okuma pozunda sağ sayfanın alt satırları 13,9 px'ti: alt satır ve köşe yazısı 18 → 19 tasarım px (14,7 px).
  - Sayfa ve kutu renkleri: kamerayı izleyen dolgu 0,3 m'deki defteri soldurup griye çekiyordu; sayfa malzemesi `_BaseColor`
    (0,9; 0,74; 0,62), tarif kutusu #D2C39E (R18a'ya göre bölge rengi).
  - `►` glifi statik atlaslarda yoktu (`charset.txt` + DejaVu yedeği).
- **Görüntüler** (`screens_unity_rev3/`): `A5_R18a_kapali` (+`_EN`), `_hover`, `_acik1/2/3` (+`_acik3_EN`), `_cevir`, `_sayfa_1..3_tr/en_yan`
  (düz çizim, tasarım PNG'siyle yan yana); `A5_R18b_surukle`; `A5_R18c_is_emri` (+`_EN`), `_eksik`, `_tarif_formda`; `A5_R18_yetersiz`;
  `A5_R18_kilitli` (+`_EN`); `A5_R18d_kayis`, `_urun` (+`_EN`).
- **Ölçüler (Play, 1920 × 1080):** defter okuma pozunda ad 20,6 px, öteki 14,7 px, TR ve EN, 0 ihlal; açılış 0,35 s, kapak −178°;
  tarif kopyasının bırakılışı tam bir `board_queue`; kilitli planda defter açılır, tarif ve fiş kalkmaz; kapanış → ürün depoda YENİ ile.
- **Testler:** tam EditMode **811/811** (boş sahnede) ve PlayMode **41/41** (`ProductionPlayTests` 21 + Aşama 3/4). Yeni EditMode: `Rev3Stage5ContractTests`,
  `Rev3RecipeBookTests`, `Rev3RecipeBookSceneTests`, `Rev3WorkOrderViewTests`, `Rev3RecipeDragTests`, `Rev3ProductionFlowTests`;
  genişleyen `Rev3CardFaceTests`, `Rev3BoardSnapshotTests`, `Rev3BoardDropRulesTests`, `Rev3BoardLayoutTests`, `PlanningBoardLayoutTests`.

**Tasarımdan bilinçli sapmalar:** R18c'nin "eşleşme · eksik saha kiti önce" cümlesi ve 04 sayfasının el notu çizilmez (D1-B); defterin
üzerine gelmede eğilme yerine 4 mm öteleme; tarif kartının ilerleme kutuları kendi satırında; renkler ışık telafisiyle; EN İŞ EMRİ
sekmesi uzar; alt satırlar 19 px.

**Açık kalan:** sesler yer tutucu; sayfa çizgileri (tasarımdaki soluk yatay çizgiler) ışıkta çok silik; uyarı/kutu çerçeveleri;
defter açıkken R18a'daki gibi kâğıt dokusu.

## 12. Aşama 6 · kapı ve kabul (sonuç, 2026-11-07)

**Yapılan** (tek seferlik `Rev3Stage6PoseRecipe` (poz tarifinin 8–10 girdisi), `Rev3Stage6Papers` (doku, sprite, malzeme, kâğıt
prefab'ları), `Rev3Stage6Scene` (sahne), `Rev3Stage6Fix` (üç plaka puntosu) ve denetim için `Rev3Stage6Check`; hepsi çalıştırıldı
ve silindi). Kararlar §6.6 (D1–D26), uygulamada değişenler §6.6 sonunda. **Core değişti** (onaylı K4-A / D4): yalnız
`Core/Simulation.CommunityAdmissions.cs` ve `Core/GameRecord.cs`; yeni komut, kayıt türü ve RNG çağrısı yok, `Replay` aynı.
- **Core (C1–C7):**
  - **C1** `AdmissionWatcher(targetId)`: Boşta'nın (`Job == "rest"`) ilk canlı, yatmayan, kısıtsız, hedeften farklı kişisi,
    `Board.People` sırasıyla; `HasAdmissionWatcher`, `IsResidentObserved`, `CanRestrictedResidentWork` ve kapanıştaki
    `CompleteAdmissionObservation` bunu okur. `watch` sayılmaz (görev listede kalır, `RetireJobs` Boşta'ya alır). Dinamik (D2):
    komut anında, kapanışta ve plan denetiminde yeniden okunur.
  - **C2** gözetmensiz red iletisi iki yerde (inceleme denetimi, `ResidentReviewCheck`): "Gözetmen yok: dış kayıt için Boşta'da
    ayrı, sağlam ve kısıtsız bir sakin bırak."
  - **C3 (H1)** `InspectApplicant("record")` önce bütün dosyaların `ReviewPending`'ini temizler (İNCELE + DIŞ KAYIT aynı vardiyada
    dosyayı takılı bırakıyordu).
  - **C4 (H3)** kurucu dosyasının kimliği belgesinden (`FounderIdentity`): kurucu incelemesi yanlış mühür bulgusu vermez.
  - **C5** `PaperCode(papers, identity)` → `date | serial | seal | origin | match`; `CompareAdmissionPapers` aynı beş metni bu koddan
    döner.
  - **C6** salt okunur `AdmissionFindingCode(personId, kind)`: `papers`/`record` → `PaperCode`, `breath` → `fog`/`clear`,
    `interview` → `north`/`east`/`south`; yapılmamış incelemede null. Gizli kimlik alanları okunmaz (sızıntı yok).
  - **C7** `GameRecord.CurrentVersion` 14 → **15**. Biçim 14 kayıtları yüklenmez; ilk yazışta `liberation_savegame.json.v14.backup`
    arşivlenir. Biçim 14 hiç commit edilmedi: kayıp yalnız geliştirme makinesindeki kayıt (KAYITTAN DEVAM görünmez, yeni oyun).
  - IL karşılaştırması (R0): değişen yöntemler yalnız bunlar; `Replay`, `AdvanceAdmissions`, `GenerateApplicant`, `Balance`
    birebir aynı. Tohum 79 zaman çizelgesi `AdmissionSupervisorTests.C8_*`'de kilitli.
- **Kapı** (`DoorProp`, `SiginakDoor`): vuruş saati iç (`Advance(dt)`; parlama 0 / 0,3 / 0,6 s, 0,08 s, 0,9 s sonra amber); süreli
  `SetOpen` evreler arası kalan süreyi devreder; kartlı oyunda kapı `SiginakAdmission`'a devredilir (`Delegated`), K8 ailesinde
  kapı hedefi `BlockedOverride` ile susar; kartlı satır "Kapıda başvuru var: tezgâha bak." / "Kapıda kimse yok."; eski dal aynen.
- **İstasyon ve araçlar** (`Runtime/Admission`): `AdmissionStation` (MB) + aynı dosyada `AdmissionTween`, `AdmissionClock`: tezgâh
  T1 (konsollar 0,15 → Top 88 → 0° 0,4 → lamba 0,08), T2 çekmece 0,35, T3 belge tezgâha 0,2, T4 katlanış, işaretçi 0,2, fiş ağzın
  40 mm içinden 0,35 s (160 mm) + kıskaca 0,15 s, talimat değişimi 0,25 + 0,3 s, karar dizileri (KABUL/KISITLI, RET, YARDIM
  tenekeleri), okuma pozu `ReadPose` (kâğıda göre), parça parça üzerine gelme; `StampDecision` (kalkış, basış 0,12 s, kavisle
  rafa 0,25 s, kilitte titreme), `MagnifierTool`, `IntercomTool` (düğme 4 mm, LED cızırtı + döküm süresi), `ColdGlassView` (kapak
  105° 0,2 s, buğu 0,6 s belirir / 2,5 s söner, ölçek 0,85), `RequestPadTool` (yaprak görünümde +35°, koparma, GİDEN'e 0,35 s, karbon).
  Hepsi ilk bağlamada animasyonsuz oturur.
- **Kâğıt yüzleri** (`AdmissionPaperFace` tabanı + `AdmissionDocumentFace`, `OrdersStripFace`, `FindingSlipFace`, `RequestFormFace`;
  prefab'lar `Prefabs/Rev3Admission/{AdmissionDocument, OrdersStrip, FindingSlip, RequestForm}`): World Space TMP tuvali (1 birim =
  1 mm), ışıksız font malzemesi, düz kâğıt malzemesi (`Art/Rev3/SceneMaterials/admission/`), sprite'lar `Art/Rev3/Sprites/admission/`;
  sarar, küçülmez (el yazısı satırı en çok %70'e yatay sıkışır). Sabit etiketler `DeskLocText`. Buğu dokusu `tx_fog` kalıcı
  `Rev3TextureBaker.BakeFog` ile (Corrigible › Revizyon 3 › Buğu dokusunu üret). `FileTagView` (`Runtime/PlanningBoard/Rev3`) kişi
  dosyasında, `PersonFolderView.fileTag` (`tag` adı `Component.tag`'i gizlerdi).
- **Veri** (`Bridge/Siginak/AdmissionSnapshotBuilder` + `AdmissionRuleMap`): `Build(sim, ui)` → dilden bağımsız sözleşmeli
  `AdmissionSnapshot` (çevrilmiş metinler; RNG çekmez, komut yazmaz). Kilit, araç ve damga durumları yalnız Core denetimlerinden;
  bulgu damgaları `AdmissionFindingCode`'dan (sunum metin eşlemez, D4); gözetmen adı `AdmissionWatcher(aday)`'dan; "robot" sözcüğü
  kişi dosyası ve fişlerde yok (D5); uyarılar `RequestWarning` / `ReviewWarning` (H2/H4). `PlanningBoardSnapshotBuilder.Folder` KARAR
  satırı, bulgular, İZLER, `TagText`.
- **Akış** (`Bridge/Siginak/SiginakAdmission`, `SiginakRoom/Admission controller`): komutlar (araç → tek `inspectApplicant`, damga →
  tek `decideApplicant`; ret komutsuz `Deny`), vuruş, poz ve geri zinciri (D13: elde damga → dizi → K8R → K8A → K8 → K1; K8G geldiği
  poza), K8G'de buğu kamera oturunca, talimat değişimi ve dış kayıt sonucu ilk K8/K8A varışında, karar dizisi (`DecisionTimeline`
  token'dan: 0,35 / 0,58 / 0,93 / 2,83 s; dizide rig kilitli, Esc ve sağ tık diziyi sona atlatır), teleks "TALİMAT n" ("VARDİYA
  DEVREDİLDİ"den sonra, D26), sesler (`CueOf`, `Sfx` 120–128 yer tutucu, D23). Sunum durumu `AdmissionUi` (görülen talimat sürümü,
  sonucu görülen aday, tezgâhtaki belge) `liberation_desk.json`'da (`SiginakDeskStore.AdmissionSeen`); yeni oyunda güncel sürüm
  görülmüş sayılır. `PlanningBoardPresenter` kabul penceresi kodu kalktı; `ShowAdmissions`/`CloseAdmissions` → `Enter`/`Leave` (D20).
  `SiginakDesk` (KAPI tuşu, K8 ailesinde sağ tık, kenar satırı gizli), `LiberationDeskView` (Esc önce kabule) bağlı.
- **Kamera:** `SiginakPose.Admission` 8 (K8A), `BreathGlass` 9 (K8G), `AdmissionRead` 10 (K8R, çapa `Poses/K8R` her okumada
  kâğıttan); `PoseSeconds` K8A 0,45, K8G 0,45, K8R 0,25. Poz ışığı K8 kopyası + `counterSpot`; K8A/G/R tam 3 gölgeli.
- **Sahne** (`DeskGame.unity`): `SiginakRoom/Admission station` (tezgâh, panel, lamba, raf + 4 damga + kilit dilleri, büyüteç, talep
  defteri + yaprak + karbon, kalem, MASA tuşu ve plakası, tenekeler, belge, şerit, 2 ESKİ şeridi, 5 fiş), buğu camı kanatta, interkoma
  kendi kutusu, kapı kutusu kanadın arkasına (D25), `CounterSpot`, pozlar, `Admission controller` ve dört `admission` bağı, FileTag;
  eski `SiginakRoom/Admission papers` alt ağacı silindi, `PlanningBoardView` kabul alanları boş. Kayıtlı hâl katlı tezgâh.
  Referans denetimi: 15 beklenen BOŞALDI (eski pencere) + 2 beklenmeyen satır (interkom `TXT_Plate` malzemesi `CP-Bold SDF Material` →
  `CP-Bold SDF URP Lit`, öteki oda plakalarıyla aynı ışıklı malzeme; kabul edildi).
- **Token ve çeviri:** `_local_a6` 43 yol (`admission.motion.*`, `readDistanceM`, `slipPathMm`, `stampMarks.*`, `slipVerdict.*`,
  `counterSpot.*`, `paper.*`); 96 yeni çeviri satırı + 3 var olan anahtarın EN'i (D15); `UpdateTable` + `Validate` 0.
- **Kabulde düzeltilenler:**
  - Kod: kayıttan devamda `SeenResultFor` koşulsuz yazılıyordu (sonuç fişi K1'de animasyonsuz çıkardı) → `ArrivedResultFor`; karar
    dizisinde istasyona komut sonrası görüntü bağlanmıyordu (dizi bitince tezgâh yeniden iniyor, eski belge bir kare görünüyordu);
    buğu tepesinde `Snap(1)`; koçan künyesi 14 px; damga eldeyken büyüteç çarpıştırıcısı kapalı; animasyonsuz bağlamada GİDEN yaprağı
    `SnapOutgoing` (kayıttan devamda yaprak uçuyordu).
  - Sahne: BULGULAR / BULGU / ESKİ puntoları (K8A'da 10,5–13,8 px'ti).
  - Görüntü karşılaştırmasından: tezgâh spotunun yönü (`AimCounterSpot`), kıskaç fişleri BULGULAR yazısının altında kalıyordu
    (`ClipLocal` y 4 → 6 mm), BULGULAR rengi #D9D2BF, GİDEN yaprağının kırpılması (`ShowBottom` + `RectMask2D`), büyüteç kuralı
    (§6.6 sonu), teneke ERZAK / RATIONS (EN 60 mm > 50 mm yuva) ve raf plakaları / damga sapları (TR'de bile sözcük ortasından
    bölünüyordu: KABUL · TAM ERİŞİM üç satır, EN ADMIS/SION, RESTR/ICTED, REJEC/T) → otomatik punto, sarmasız.
  - Testler (kod doğruydu): `Rev3DoorTests` çarpıştırıcısız `DeskTarget`, Motion testlerinde karar sonrası görüntü / EaseOut eğrisi /
    yaprak ölçüsü defter çerçevesinde, `AssertWired` Unity iç alanları, S5 3B denetim, `SiginakRoomTests` kabul işleyicileri, punto
    taramasında FileTag K4 ölçümü; PlayMode: kapasite fikstüründe erzak, Boşta boşaltmada takassız cepler, buğu tepesi ölçümü.

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

**Testler:** tam EditMode **945/945** (boş sahnede; Aşama 5: 811) ve PlayMode **67/67** (`AdmissionPlayTests` 26 + önceki 41).
- Yeni EditMode (134 koşu): `AdmissionSupervisorTests` 16 (Core C1–C11, H1, H3, bulgu kodları; tohum 79 altın zaman çizelgesi),
  `Rev3Stage6ContractTests` 11 (poz/cue numaraları, kimlikler, sözleşme kabukları, `_local_a6`), `Rev3DoorTests` 8,
  `Rev3AdmissionMotionTests` 25, `Rev3AdmissionPaperTests` 13, `Rev3AdmissionSnapshotTests` 23, `Rev3AdmissionFlowTests` 21 yöntem
  (25 koşu), `Rev3AdmissionSceneTests` 10; `Rev3BoardSnapshotTests` +3 (`Folder_NeverSaysRobot_TrEn`, `Folder_TraceRows_…`,
  `Folder_FileTag_ReviewThenRestricted`).
- Güncellenen: `CommunityAdmissionTests` (gözetmen Boşta'dan; iki test yeniden yazıldı), `LiberationCampaignTests` ve
  `ShiftClosePlayTests` / `ProductionPlayTests` sürüm bekçileri 15, `Rev3BoardDropRulesTests.FolderTabs_AreOneCommandEach_ReviewNeedsAnIdleWatcher`,
  `Rev3HandoverSlipFactsTests`, `PlanningBoardSceneTests.SavedScene_OldAdmissionWindowRemoved_StationWired`, `Rev3RoomTests`
  (K8 ailesi tarifi), `SiginakRoomTests` (kabul girdi bileşenleri), `SiginakTextScan` ("KAPI VE KABUL" bölümü).
- PlayMode `AdmissionPlayTests` (P1–P19; P12 a–e, P14 b/c ve P19b ayrı testler; kit `AdmissionPlayKit`: gerçek ışınla tık, Input System 1.19 tık kuralı,
  kayıt fikstürüyle kurulum, son kapanış gerçek arayüzle).
- **Fikstürler:** `AdmissionFixtures` (`present`, `inspect`, `pending`, `stamped`, `aid`, `v3`, `trace`, `read`, `inspect_en`;
  `Present`/`PresentAndCapture`/`Restore`, düz kâğıt `RenderAllPapersFlat`), `SiginakTextScan.ScanLiveAdmission`.

**Tasarımdan bilinçli sapmalar:** kapı KISITLI'da da açılır (D10); gözetmen dinamik (D2); talimat tarihi V.005 / V.009 (D7); İLKE
satırı yok (D6); "robot" talimat şeridinde kalır, dosyada ve fişlerde yok (D5); mercek büyütmesi yok; büyüteç yalnız karşılaştırmada
belgede (§6.6 sonu); BUĞU YOK ve KAYIT FARKLI fişleri (D15); EN damga sapları ADMISSION / RESTRICTED (tasarım ADMIT / LIMIT; tek TR
anahtar, bağlamsız `Loc`); YARDIM izi dikdörtgen; mühür yay yazısı yok; BEDEN satırı CAN/HAR/GÜÇ; fiş yolu 160 mm (Aşama 4 D12 gibi);
raf plakası, sap ve teneke yazıları otomatik küçülür; pirinç kilit dili yer tutucu (D21); R17f kısmen (D17).

**Gözlemler (düzeltilmedi):**
- Işık: kâğıtlar referanstan ~%15 koyu ve daha sarı (kâğıt albedosu 0,8 tavanı önceki aşamalardan bilinçli; amber vuruş lambası 1,2
  tezgâhı da boyar).
- Katlı tezgâhın alt yüzündeki KABUL şablonu manifestteki 80 mm cap'le çizili; referansta aynı genişlikte ama yarı yükseklikte (font
  oranı farkı).
- R17f: iz satırları veride ve testte (P16), ama kişi dosyasının sağ sayfası kurucu dosyasında taşar (fikstürde İZ satırları sayfanın
  dışında); FileTag K4'teki okuma pozunda kadrajın üst kenarında kırpılı.
- R17e: talimat değişimi kamera K8A'ya varmadan bitiyor; görüntüde v3 yerinde, v1 + v2 ESKİ'de ("şerit ESKİ'ye çıkarken" anı yakalanmadı).
- Elde damga yokken K8A'da belgenin alt kısmına tık büyütecin (modelin sınır kutusu) çarpıştırıcısına düşebilir; kutu
  küçültülmeli ya da dinlenme pozu taşınmalı (sahne kararı).
- İç içe prefab örneklerinde TMP geçersiz kılmaları (renk, otomatik punto) sahnede; parça hareketi geçersiz kılması yok.

**Açık kalan, sonraki aşamalara:**
- **Aşama 7:** R17f'nin iki sayfalık açık dosya düzeni (D17) ve sağ sayfanın taşması; FileTag'in K4 okuma pozunda kırpılması;
  `A6_R17d_kilitli` görüntüsü; R17e değişim anının görüntüsü (kamera varışına göre zamanlama).
- Sesler 120–128 klipsiz yer tutucu (D23).
- Duvar plakalarının çift yüzü (Soru 21), büyüteç çarpıştırıcısı, büyüteç alan tablosunun tahmini üç alanı (DÜZ., MÜHÜR, GELDİĞİ YER;
  `AdmissionDocumentFace.FieldCenterMm` yok), masa kenarı hedefi (`deskEdge` boş).
- Kâğıt ışığı ve KABUL şablonunun yüksekliği (gözlemler); Frame Debugger gölge sayımı ve K8A başarım ölçümü.
- "V.{0:000}’ten geçerli" anahtarı V.009 için "V.009’ten" yazar (doğrusu "’dan"); Türkçe ek uyumu ayrı ele alınmalı.
- Pirinç kilit dili ve tek yüzlü plakalar tasarımcıya bildirilecek; eski yolun silinmesi Aşama 10'da.

## 13. Aşama 7 sonucu — harita, kâğıtlar ve CRT

**2026-11-07.** R6-TR/EN, R8a/b, R10a–c ve D17'den devreden iki sayfalık kişi dosyası uygulandı. Ayrıntı ve görüntü listesi:
[A7_DOGRULAMA.md](A7_DOGRULAMA.md). Aşağıdaki sonuçlar önceki aşamaların “Aşama 7'ye kalan” notlarını günceller.

- `RaidPaperFacts` gerçek üç saha kişisini ve üzerlerindeki eşyayı okur. Görev fişi/kadro raporu isim, meslek, CAN/HAR/GÜÇ ve
  kit/zırh/yardımcı gösterir; isim tıkı `PlanningBoardPresenter.ShowPerson` ile aynı kartı seçer. Komut yazılmaz.
- `OperationReportBuilder`: fon, erzak, metal, devre, kumaş, kimyasal; ilgili `raidres` öncesi kayıt kopyasından gerçek kapasite
  sınırı; kişi adıyla dönüş, yara, yatış, kalıcı kayıp, deneyim ve plaka kırılması. Tarih V.001/S.001'den başlar.
- `PersonDossierFace`: sol kimlik/portre/yedi ustalık/üç eşya; sağ bilinen belge/karar/erişim/geçmiş/iz. Uzun sağ sayfa ek yapraklara
  bölünür. Tek yaprakta oklar gizli; var olan üç dosya komutu korunur. FileTag sağ kenarda kadraj içinde. **R17f tamamlandı.**
- `CrtInstruments`: sinyal ve kanal arama, son .6 s'de 1,4× ışıma, dönüşte kapanma. R8 için render texture eklenmedi; modelin URP
  emission değeri ve sahnedeki mevcut ışığı kullanılır. Telsiz/defter üzerine gelme hedefleri bağlı.
- **K5a kararı:** 46° ve özgün konumun karşısına 52° / CRT yerel sağına +14 cm görüntüsü kondu. İkinci kadraj telsizin tamamını,
  mikrofonu ve açık defteri gösteriyor; sahneye ve yerel token kopyasına kaydedildi. K5b hedefi korunur.
- Açılışta ampul → .15 s sonra masa lambası; pano kipine uygun emir metni. R10 künye/çizgileri, altı ödülün düzeni ve kadro
  raporunun .22 s açılışı bağlı. Eski dört ödüllü / anonim rol-yedek dalı `planningBoardEnabled=false` için korunur.
- Tam PlayMode denetiminin yakaladığı K8R zamanlama sorunu: tezgâh hover kalkışı bittiğinde okuma çapası 4 mm sapabiliyordu.
  `AdmissionStation.ReadPose` yatay kâğıdın son konumunu hedefliyor; EditMode regresyonu eklendi.
- Aşama 6'dan kalan kilitli damga ve talimatın değişim anı görüntüleri `A7_R17d_kilitli`, `A7_R17e_degisim_ani` ile tamamlandı.
  Değişim anı aynı animasyonun K8A'ya varıldıktan sonraki sunum fikstürüdür; canlı kamera zamanlaması değiştirilmedi.

**Doğrulama:** EditMode 955/955; PlayMode 71 senaryo (45 tam turda, düzeltme sonrası kabul paketi 26/26), yeni R6/dosya/CRT testleri 4/4

**Ölçü ve sapmalar:** görev fişi ~950 px (referans 887), üst pay 128 px; TR/EN ana düğmeler kadrajda. Kadro raporu 470 × ~674.
Açık dosya sahnenin 120 × 168 mm sayfalarını kullanır; sağda ek yapraklar vardır. K4 canlı ölçümde en küçük yeni dosya metni
14,21 px. Tüm sahne punto taraması 2431 metin, 0 ihlal (150 önceki gerekçeli istisna). Core, RNG sırası, denge ve kayıt biçimi
bu aşamada değişmedi; biçim **15**. Sahne tek seferlik editör kurucusuyla değiştirildi, kurucu silindi. Kullanıcı kayıtları test
klasöründen ayrıldı. Kesintide test klasöründe oynanan yeni ilerleme kullanıcının onayıyla normal devam kaydına taşındı; önceki
kayıt ve yeni ilerleme `UserSettings/CodexRecovery/` altında yedeklendi. Son sayfa alt boşluğu düzenlemesinden sonra kâğıt testleri
10/10, ilgili PlayMode testleri 4/4 yeniden geçti. Sıradaki aşama **8**; bu aşamada menü sahanlığı/dolap uygulamasına başlanmadı.

## 14. Aşama 8 sonucu — menü sahanlığı ve ayarlar dolabı

R1–R4 uygulandı. Kanıtlar ve referansa göre farklar `A8_DOGRULAMA.md` içinde.

- Menü kabuğu, kapı dış donanımı, saat/kart/raf/deste, ana şalter ve iki dolap paket modelleriyle değişti. Sahne ve prefab kökleri korunarak referanslar bağlandı. Kaynak 198 model değiştirilmedi; baskı yüzlerinde UV'si düzeltilmiş ağ kopyaları kullanıldı.
- Kart dört ayrı satır ve 80 delikle gerçek kayıt özetini gösterir. Yeni harekâtın mevcut iki tık / 4 s onayı, iptal fişi ve devrilme hareketi modele bağlıdır. Saat tıkı da devam eder. Yeni kart ilk vardiyadır.
- Giriş R4 zamanlamasıyla, tüm girdiler ve imleç kilitli oynar. Yükleme kanat açılırken başlar, en az 0,2 s siyah ardından etkinleşir. Kapı açılırken en çok üç gölgeli ışık bulunur.
- Dolap sürgüleri, ibreleri, sayaçları, lambaları ve SES/DİL anahtarları mevcut ayar servisini kullanır. Oda menü kolunda açık belge/işlem kilidi, neden satırı, mühür bandı ve reddedilen tık hareketi vardır; dolabın açık olması kendi kolunu kilitlemez.
- M0 hedef y=1,25 / 60°; M1 x=3,32 / 48°; K9 yerel z=−1,10 / 48°. Plaka büyük harfi 7,2 mm; DİL plakası 112, menü plakası 120 mm. İptal fişi iki satırdır. Gölgesiz okuma dolgusu devrilen kartın kararmasını önler.
- Tek seferlik sahne/UV kurucuları ve QA oturum koruyucusu silindi. Kalıcı sahne kurucusu eklenmedi. `Rev3TextureBaker.BakeStage8()` baskıları yeniden üretebilir. Core, denge, RNG ve kayıt biçimi değişmedi (15).

Doğrulama: tam EditMode **964/964**, tam PlayMode **79/79**. Son Play turundan sonra yalnız iptal fişinin iki dilli kutusu büyütüldü; iki ek taşma testi son EditMode turuna dahildir. Ana punto taraması 2419 metin / 0 ihlal / 150 gerekçeli istisna; canlı menü metninde en küçük gerekli punto 14,30 px. Üç sahnede eksik script yok; 2011 çeviride eksik yok. Beş gerçek kayıt, tercihler ve başlangıç fontları korunur; test yönlendirmesi kaldırıldı, Giris Edit Mode açık. Sıradaki aşama **9** (baskın kitleri).

## 15. Aşama 9 sonucu — baskın kitleri ve isimli saha (2026-11-08)

- 12 bölge + varsayılanın Relief/ReliefIso/Bridge/Rubble, sivil cihaz ve düşük giriş kapısı; BoardBase, Tile/WallBlock, 16 maskeli kanal; sekiz makine, T/E/A, dolaplar ve olay parçaları paket modellerine bağlandı. 53 birleşik prefab `Desk/Prefabs/Rev3Raid` altında; kaynak 198 modele dokunulmadı. BackupCore kullanılmaz; Madenci/Sis eski yolda durur.
- Piyonun Letter/Tilt/Tape/Tick/Plate parçaları; 180° kraft yüz, −70° düşüş, gerçek kişi adı/pip/plaka şeridi. Kaynak kolu ve kaidenin burnu birlikte döner. Kanallar tek gölgesiz renderer; kare tabanlı moloz 90° adımla kendi karesinde kalır.
- Kâğıt kenarı/ızgara/bant/gölge, asetatlar, antet/anahtar plan/damga/bina notu; gerçek CAN/HAR/GÜÇ/eşya; Balance'tan kasa dökümü ve kampanyanın gerçek vardiyası. Son kaçış turunda isimli kalıcı ölüm/eşya kaybı onayı, haklar bitse de zorunludur; Core kuralı değişmedi.
- Kabulde: EN adını gizleyen eski kısaltma, yetenek/Q taşması, alt kartların ekran dışına sarkması, etiketlerin mod değişiminde dar kalması, kanal lejantının yüksekliği ve çift çeviri çağrıları düzeltildi.
- Tam EditMode **969/969**, PlayMode **83/83**; son yerleşim sonrası baskın sözleşmeleri **5/5**. Genel punto **2438 / 0 ihlal**, canlı EN **min 14 px / 0 taşma**; üç sahnede eksik script ve boşalan referans **0**. 24 bina notu iki dilde sığar. Ölçek **132,58 px/birim** (zemin kenarı projeksiyonu 108,08 px); kapılar ≤0,29 m. R15 gereği ortak 2048 gölge atlası korundu.
- Ayrıntı ve R11a–c / EN / kaçış / 13 kit gri atlası: `A9_DOGRULAMA.md`. Geçici kurucular silindi, gerçek kayıtlar/tercihler ve başlangıç fontları korundu; Giris Edit Mode açık. Core/Balance/RNG/kayıt biçimi **15** değişmedi. Sonraki aşama **10**.

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
