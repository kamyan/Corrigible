# Model kütüphanesi · Revizyon 3 + Ek 11-05

Her model için: `obj/<Model>.obj` + `obj/<Model>.mtl`, `manifest/<Model>.json`, `sayfa/<Model>.jpg` (model sayfası) ve
three.js kaynağı (`../threejs/r3/models/<aile>.js`, fonksiyon adı = model adı). Bütün modellerin özeti `MODELLER.md`
(tablo) ve `models.json`.

## OBJ kuralı

- **Y yukarı, metre, sağ el.** Unity'nin OBJ içe aktarıcısı X'i aynaladığı için köşeler **(−x, y, z)** olarak yazıldı;
  Unity'ye girince model Unity koordinatında doğru durur (x sağ, y yukarı, ön yüz −z).
- **İlk iş `_AxisTest`:** uzun kol +x'e (sağa), kısa kol +z'ye (arkaya), tehlike küpü üstte ve öne bakar; `Flap` parçası
  +z ekseninde 0→90° döner. Unity'de böyle görünmüyorsa içe aktarma ayarı yanlıştır (Scale Factor 1, "Convert Units"
  kapalı, Axis Conversion yok); kütüphanenin geri kalanına geçmeden düzelt.
- **Her hareketli parça ayrı bir `o`/`g` grubudur** ve köşeleri **pivotuna göre** yazılmıştır: Unity'de alt nesnenin
  `localPosition`'ı manifestteki `pivotLocal` (ebeveyn parçaya göre), dönüş ekseni `axis` (yerel x/y/z), aralık `range`
  (derece; `slide` için metre), dinlenme `rest`. Hiyerarşi `parent` alanından kurulur (ör. dolapta `Latch` → `Leaf`).
  `swap` parçalar (lamba camı, KİLİTLİ bandı, piyon bantları) yalnız malzeme/görünürlük değiştirir.
- **Body** model uzayındadır; orijin manifestte `origin` (Türkçe): masa/zemin nesnesi = taban izinin ortası, y = 0;
  duvar nesnesi = arka yüzün alt kenar ortası, z = 0; baskın parçası = taş ortası, y = 0.
- **Malzemeler:** grup başına `usemtl <anahtar>`; MTL'de Kd (sRGB), Ns (pürüzlülükten), d (saydamlık), Ke (emission).
  Unity'de MTL'yi kullanma: her anahtar için bir URP Lit malzeme kur, değerleri token 1.3'ten al (`materials.*`,
  `materials3d.*`). Özel shader yok.
- **Yazı yok:** modellerde hiç kelime yoktur. `texts` listesindeki her `TXT_*` yuvası bir TMP nesnesidir: `center`, `size`
  (m), `normal`, `up`, `capMm` (büyük harf yüksekliği), `role` (tip token'ı), `sample` (TR) ve `sampleEn` (EN, %30'a kadar
  uzun; yuva EN'e göre ölçülü).
- `anchors` (kart yuvası, kamera çapaları, delik yerleri H01–H80…), `lights` (ışık noktaları, gölge bayrağı), `place`
  (sahnedeki dünya yerleşimi) manifestte.

## Bütçe ve kurallar

Büyük nesne ≤ 4000 üçgen, küçük ≤ 800, baskın parçası ≤ 400, makine/piyon ≤ 1500 (`budgetTris`, `overBudget`; bütçeyi aşan model yok).
Pah: ana pozunda ≥ 3 ekran px (K1 ≈ 6 mm, K9 ≈ 2 mm, M0 ≈ 5–6 mm, M2 ≈ 1,5–2 mm, baskın 0,02–0,03 birim). Ekranda 3 px'ten
ince olan (tel, ölçek çizgisi, vida yarığı, ızgara) dokuya gider. Dokular ≤ 1024 px. 2 cm'den küçük parçalar gölge vermez
(Unity: Cast Shadows Off).

## Unity'ye alma sırası

1. `_AxisTest` → eksenler doğru mu.
2. Aile aile: OBJ'yi içe aktar → prefab → parçaları manifestteki `parent`/`pivotLocal` ile yerleştir (OBJ zaten pivot
   uzayında; yalnız konumla) → malzeme anahtarlarını URP Lit malzemelere bağla → `TXT_*` yuvalarına TMP koy.
3. Sahnede `place` konumlarına koy (Revizyon 2'deki eski nesnenin yerine; konumlar aynı ya da notta yazan kadar farklı).
4. Kaynak (three.js) görünüş içindir; sayılar manifestte ve token 1.3'te.

## Ek 11-05 (kartlı pano, kapıda kabul, vardiya devri)

- **Yeni aile `board`** (planlama panosu ve kartlar): `PlanningBoard` (ayaklı, mıknatıslı çelik; eski rafın yerinde,
  `place` (−1,62 · 0 · 1,70) yaw −48°), `CardPerson` 80 × 120, `CardItem` 80 × 50, `CardMaterial` 80 × 56, `CardChip` 56 × 40,
  `CardRecipe` 112 × 80, `RecipeBook` (A6 ciltli), `PersonFolder` (manila). Bütün kartlar aynı kalınlıkta (0,6 mm) ve
  panodayken Ø 10 mm mıknatıs düğmesi (`Magnet` swap) taşır. **Kart yüzü doku dosyası değildir:** UV 0–1 bir düzlem; yüz
  çalışma anında çizilir (8 px/mm; düzen `threejs/r3/boardtex.js`, ölçüler token `cards.*`). Pano yüzündeki baskı
  kelimesizdir (`tx_board_face`); bütün başlıklar `TXT_*` yuvaları (TR/EN). Cep, deste, iş emri, depo ve hurda konumları
  hem çapalarda (`Slot_*`, `Bosta_Deck`, `Order_*`, `Mat_*`, `Kit_*`, `Hurda_Slot`) hem token `board.layoutMm`’de.
- **Yeni aile `admission`** (kapıda kabul, R17): `AdmissionCounter`, `AdmissionPanel`, `ColdGlass` (buğu camı, kanatta),
  `DecisionStampRack` + `DecisionStamp`, `Magnifier`, `RequestPad`, `CounterLamp`, `RationTin`, `FileTag`.
- **Masa ailesine eklenen:** `SlipMouth` (fiş ağzı, şalterle aynı yerleşim; `SlipStart` → `SlipExit` çapaları, şalter
  yerelinde x −0,18 → −0,30, y 0,14, z 0,015) ve `HandoverSlip` (devir fişi 100 × 150 mm, kıvrık düzlem; doku R10
  `devir_fisi_*`). `ShiftSwitch` değişmedi; plakası iki satırlı kullanılır.
- **Pakette olmayanlar** (oyundan çıktı; kaynakta legacy olarak duruyor, token `models3d.removed_1_3`): raf ve maket
  ailesinin 28 modeli (`Shelf`, `Diorama*`, `Depot*`, `RoomCard*`, `ClipboardStand`, `IdleTray`, `Peg*`, `Crate`,
  `Jerrycan`, `HopePlant1–3`), 9 ziyaretçi silüeti (`Visitor_*`), `Pawn_M` ve `Pawn_S`.

