# Aşama 10 · Dil, okunurluk, performans, temizlik — doğrulama

2026-11-08 · Unity 6000.3.12f1 · kayıt biçimi 15. Kararlar: `IMPLEMENTATION_MAP.md` §4.7 (A10-D1..D18). Sapmaların tek listesi aynı belgenin §5'i, açık kalanlar §6.

## Son kontroller

- Tam **EditMode 988/988**, tam **PlayMode 85/85**; başarısız ya da atlanan yok (Aşama 9: 969, 83). PlayMode'da yeni `PersonCardDragPlayTests` 2 test.
- Çeviri doğrulaması 0 eksik. Üç sahnede eksik script 0.
- Statik punto taraması (`punto_taramasi.txt`): **2321 metin, 0 ihlal, 149 gerekçeli istisna**. Aşama 9'da 2438 / 150 idi: T1 eski tuval metinlerini düşürdü, Giris taramaya girdi, `RaidScreenLabel` istisnası kalktı (punto 12 → 14); sayı farkı satır satır çıkarılmadı.
- Core, Balance, RNG sırası, kayıt biçimi değişmedi (`Core` altında plan tarihinden yeni dosya 0; `CurrentVersion = 15`).

## Dil

### Yöntem

- `Rev3CaptureSet` (kalıcı editör aracı) oturumları `menu`, `k1`, `k4`, `k6`, `k8`, `hover`, `raid`; her durumda `SiginakTextScan.ScanLiveAll` + görüntü. 1920 × 1080, tohum 79.
- Dil, oyun kurulmadan **önce** `DeskLoc.Instance.SetLanguage` ile değişir; böylece Core metinleri ölçülen dilde üretilir. B1 (temiz EN açılış) `PlayerPrefs corrigible.language` ile.
- Satır başına: gerçek px (dünya metni kamera yüksekliğine normalize), eşik, otomatik punto oranı, iki eksende taşma, `Loc.Missing`, EN'de Türkçe harf ve sözcük (izin listesiyle), `.notdef`, Dyn yedeğe düşen karakter.
- `küçük` bayrağı yalnız okuma durumunda sayılır (`uzak`, `kagit`, `form` ayrı; 2462 eşik altı satırın tamamı okuma dışı).
- Ek test: `EnglishCardBuilders_ProduceNoTurkishText` (V.001, V.005, V.009, V.031 kurucuları; Unity dışı ön koşuda 1279 dizge, sızıntı 0).

### Durumlar

86 durum etiketi, B1 dışında TR + EN (aşağıdaki listede `C3_canli` bu 86'ya ek birleşik yakalamadır). Canlı oyun, kayıttan devam ve sunum fikstürü karışıktır (`Rev3CaptureSet` içinde durum başına):

| Aile | Etiketler |
|---|---|
| Menü | B1, M0a, M0b, M1, M2, M2c, MS, M3 |
| Masa | K1a, K1b, K1c, K1d, BR, O1, O2, FR1, FR2 |
| Harita | K2a, K3a, K3b, K3c, K3d1, K3d2 |
| Pano | K4a–K4n, F1, F2, F3, BK1, BK2, BK3 |
| CRT | C1, C2, C3, C3_canli, C4 |
| Şalter ve fiş | S1–S8 |
| Kapı ve kabul | D1, D2, A1–A6, A7 (Admit, Aid, Reject, Restricted, rack), G1, AR1–AR4 |
| Dolap | K9a, K9b |
| Baskın | Z1–Z11 |
| Üzerine gelme | H1: 41 hedef, 140 satır + `H1_longest` (Desk, Map, Shelf, Shift, Door, Admission, M0, M2, M2c) |

### Sayılar (`A10_dil_olcumleri.json`)

| Ölçüt | Değer |
|---|---|
| Satır | 17.939 (EN 8.979, TR 8.960) |
| Etiket | 129 (86 durum, `C3_canli`, 42 H1) |
| Yakalama adımı | İlk tam tur 169 OK, 0 FAIL; son düzeltmelerden sonra `menu`, `k8`, `k6` yeniden çekildi: +73 OK, 0 FAIL, günlükte toplam 242 (`Temp/capture_set_all.txt`) |
| İstisna satırı | 1.637 |
| Okuma durumunda < 14 px (`küçük`) | **0** |
| `Loc.Missing` · `.notdef` · EN'de Türkçe | **0 · 0 · 0** |
| Yatay taşma, `kesildi`, satır sayısı | **0** |
| Dikey taşma bayrağı, istisna dışı | 1.368 (684 EN, 684 TR) |
| · tek satır, font satır ölçüsü | 1.222 |
| · sarılı kâğıt ya da plaka, tek satırlık yuva | 146 (kâğıt akışı 115, plaka 24, HUD Detail 5, anahtar etiketi 2) |
| Dikey bayrak, istisna altında | 112 |
| Kutudan kesilen yazı | **0** |
| Otomatik punto alt sınırı | 6 istisna dışı (BREATH GLASS / BUĞU CAMI %75; G1'de 36 px, tek satır), 8 istisna altı |
| Otomatik punto, alt sınır üstü | 271 satır; en düşük 0,57 (`TXT_S`, istisna; A7 üzerine gelme yazısı anlamı taşır) |
| Dyn yedek | 4 satır (`◀`, F1/F2) |

**1.368 bayrak hakkında.** Bayrak `textBounds.size.y`'yi kutu yüksekliğiyle kıyaslar; TMP bu değeri glif mürekkebinden değil font satır ölçüsünden kurar (Courier 1,123 em, büyük harf 0,57–0,59). Her sınıf sayısal incelendi, çoğu görüntüyle teyit edildi; `İz` ve `Koçan başlığı` yalnız sayısal. Bu, "görüntüde kesilen yazı yok" demektir; 1.368 satırın tek tek piksel kanıtı değildir.

### Düzeltmeler

| Kimlik | Konu | Durum | Not |
|---|---|---|---|
| B01 | Çifte `Loc.T` (46 sahte `Loc.Missing`) | Giderildi | 18 yer; `Loc.Missing` 0 |
| B02 | Yakalama boşlukları | Giderildi | 86/86 etiket; A3 görsel olarak belirsiz (G09) |
| G01 | BREATH GLASS plakası | Giderildi | Otomatik punto, %75 alt sınır |
| G02 | Kalıcı ölüm metni | Giderildi | `is lost for good`; 0 `counts as down` |
| G03 | Geri çekil düğmesi | Giderildi | `Ctx`: EN `SURE?` 56,3 px, TR 109,4 px, düğme 116,8 px; banner altında kaldığı için kanıt sayısal |
| G04 | Kasa satırı | Giderildi | `CRATE: …` tek satır |
| G05 | Y ekseni yüzdesi | Giderildi | `FinalReportView.yLabels`, `DeskFormat.Percent` |
| G06 | Bulgu fişi | Giderildi (A10-D13) | AR3, A2 |
| G07 | Dolap: sıcak nokta, perçin, sayaç | Sıcak nokta giderildi (dolgu 0,35 → 0,04); DİL plakası gövde 1,96×, perçinler harfleri kesmez; sayaç dikişi giderildi | `AMBIENCE` perçinleri ve yumuşak rakamlar sapma |
| G08 | Kadro raporu kenar boşluğu | Giderildi | K3c |
| G09 | A3 iki fiş | Sapma | Yelpaze 1 mm adım; 0,6 mm aralık; `PENDING` tarama değeriyle kanıtlı |
| G10 | Yanlış teknisyen satırı | Kodda giderildi | `Kadro hazır değil` |
| G11 | Kaçış gövdesi (kasa görevi) | Giderildi | |
| G12 | Teleks uyarısı | Uygulandı, yalnız geliştirici yolu | |
| G13 | Sunucu simgesi | Hata değil | `SadeRaidStateTests` kilitli; öneri geri alındı |
| G14 | Kasa değeri | Giderildi | `left behind` |
| G15 | armour yazımı | Giderildi | `strings.txt`'te `armor` 0 |
| G16, G17 | Sözcük | Tabloda giderildi | Görüntüde görülmedi |
| G18 | `Electronics` | Uygulanmadı, sapma | 73,4 / 72 mm |
| G19 | Bölge kısaltması | Giderildi | `CrtViewBuilder.ShortRegion` |
| G20 | Yetim sözcük | Giderildi | `blocks first hit` |
| G21 | Kart serbest etiketi | Giderildi | Giris sahne override'ı; B1 çok küçük, belirsiz |
| G22 | Dişli rozet | Giderildi (A10-D14) | Tasarımdan soluk |
| G23, G27, G29 | Işık ve albedo | Açık, sapma | Harita §5 |
| G24, G25 | Baskın adı, RULES | Giderildi | |
| G26 | İptal fişi | Giderildi | Çift çerçeve yok |
| G28 | `READY` | Kalır (A10-D15) | |
| G30 | Küçükler | Sapma | Harita §5 |
| Ek uyumu | V.001’den, V.005’ten, V.009’dan | Giderildi | `TrNumber.WithAblative`, 8 vakalı test |

EN sözcük kararları: ADMIT / LIMIT / PENDING (`DeskLoc.Ctx`), LAID UP, EVRAK → PAPERS. Yorum ve token notları: `RaidKitPreview` (Soru 7), `CounterModule`, `EdgeLine`, `SiginakAdmission`, `AdmissionFixtures`; `_local_a7/a8/a9`.

Doğrulanamayanlar: telsizin K9'dan dönüşü görüntülenmedi (kod ve sahne bağı doğrulandı); G21 B1'de; K3c HP pipleri; F2 yalnız EN görüldü; Crt, Cabinet, MS, M1'de H1 `0 hedef` verdi (neden belirsiz), ses açık/kapalı satırı ölçülmedi.

## Performans

### Ortam ve sınır

Apple M4 Pro, Metal, Editor Play, kalite 0, vSync 0, `targetFrameRate` −1, TR, git `c1a743a` + çalışma ağacı, 120 kare oturma, 600 kare örnek, 3 × 30 eşzamanlı çizim. İki tur, sıra ters. Kaynak: `perf_asis.tsv`, `perf_asis2.tsv` (onarım öncesi), `perf_fixed.tsv` (onarımlı), `perf_short.tsv` (onarımlı + T1 sonrası kısa tur; çalışma klasöründe, kalıcı değil).

**Uyarı.** Her ortam satırı `screen=555x808; focus=False; focused=yok; scene_views=1; profiler=False` yazar. Game görünümü 1920 × 1080'e ayarlanmıştı ama `Screen.width/height` bunu göstermiyor; Editör de odakta değildi. Sütunlar buna göre okunmalı:

- **Δt, CPU, GPU, batch:** oyun döngüsü, 1920 × 1080 oldukları kanıtlanmadı.
- **30× MSAA4 / MSAA1:** 1920 × 1080 RT'ye `cam.Render()`, tek çizim başına ms. Çözünürlüğü kesin; ama setler arasında gürültülü (örn. P4 MSAA4 6,25–12,89).
- Mac Metal sayıları Steam PC'nin vekili değildir.

### Hedefler (A10-D8 ve plan)

| Ölçüt | Hedef | Sonuç |
|---|---|---|
| Medyan · p95, en kötü durumlar | ≤ 16,7 · ≤ 20 ms | **Sağlar.** Bütün onarımlı senaryolarda en kötü medyan TR2 5,73, en kötü p95 6,96 (P20.in tur 1; aynı kit tur 2'de 3,25; ondan sonra en kötü TR1 6,39) |
| Atlas | Ölçek uyarısı yok, Point yok, poz başına ≤ 4 ışık | **Sağlar.** Uyarı 0, Point 0, en çok 4 dilim (P11 tur 2) |
| Dolu pano 30× − K1 temeli | ≤ +1,5 ms (Aşama 3 sürekliliği) | **Belirsiz.** MSAA4 farkı +1,46 (tur 1), +4,33 (tur 2), +1,96 (T1 sonrası); setler gürültülü |
| Batch, shadow caster | Aşama 3'ten kötü değil | **Sağlar.** P1 781–795 / 790, P3 477 / 472, P4 904 / 902 |
| K4 boşta | `ChangedBinds` Δ 0; pano GC 0 | Bağlama Δ 0. GC/kare ~18,1 KB (Aşama 3: 16,8–17,2); pano payı ayrıca ölçülmedi |
| Sahne yükleme | T1 kazancı | Ölçülmedi; dosya 14,77 → 11,72 MB (aşama başında 14,44) |

Önceden önerilen K8A 300 kare ölçümü P13 ile yapıldı. Frame Debugger CLI ile sürülemediği için kullanılmadı; yerine ışık listesi ve URP geçiş yansıması (dilim, atlas, kaskad).

### Onarım (A10-D7)

`Bulb light`, `Desk lamp light`, `BoardLamp` Custom kademede `m_Resolution` −1 idi; URP bunu 65535 px isteğine çeviriyordu. Üç ışık 2048 (sahne override'ı, prefab aynı). `CorridorLamp near` Point → Spot 110°. `CeilingBulb/BulbLight` gölgesiz. `Rev3RoomTests`: her gölgeli ışık Spot, üç ışık 2048 kilitli.

### Atlas

| Poz | Olduğu gibi | Onarımlı |
|---|---|---|
| K1 | 3 × 2047, atlas 4096 × 4096, ölçek uyarısı var | 3 × 2048, uyarı yok |
| K4 | 1 × 4095, uyarı ÷16 | 1 × 2048, atlas 2048 × 2048 |
| K8 (adaysız) | 2 × 2047 | 2 × 2048 |
| K8 kapı açık (P11) | 2047, 2047, 6–7 × 32 (Point küp) | 2048, 2048, 1024 (tur 1) ya da 2048, 2048, 1024, 1024 (tur 2) |
| K8A / G / R | 32 + 2047 (tezgâh spotu **32 px**) | 1024 + 2048 |
| K9 (P16) | 4095 ya da 2 × 2047 | 2048 ya da 2 × 2048 |
| Giris M0 / M1 / M2 / MS | 3 × 1024 (değişmedi) | 3 × 1024 |
| Baskın | Ana 2048 × 2048, 4 kaskad; P19–P20'de ek dilim yok, P21 (eklemeli) 2 × 2047 | Ana aynı; P21 2 × 2048 |

`Rev3RoomTests` tarif kilidi: K8A/G/R tam 3 gölgeli; çizimde kameraya göre 2 dilim kullanılır.

### Onarımlı (`perf_fixed.tsv`)

CPU, GPU ve GC/kare sütunları iki turun ortalamasıdır ("Olduğu gibi" tablosunda da); öteki sütunlar tur başınadır.

| Senaryo | Poz | Gölgeli ışıklar | Dilimler (px) | Atlas | Ölçek uyarısı | Δt medyan/p95/maks ms (tur 1 · tur 2) | CPU ana/render | GPU | Batch/SetPass | Shadow caster | GC/kare | 30× MSAA4 / MSAA1 | Aşama 3 farkı | Hedef |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| P1 K1 V.001 | Desk | BoardLamp, Bulb, Desk lamp | 2048,2048,2048 | 4096x4096 | yok | 4,64/5,44/7,75 · 4,20/4,53/5,90 | 1,57/1,64 | 5,90 | 795/109 / 781/111 | 470 | 18,1 KB | 9,46 / 9,35 · 6,00 / 6,04 | medyan -0,8 ms; A3 batch 790/110; 30× -1,6 | sağlar |
| P2 K1 V.031 | Desk | BoardLamp, Bulb, Desk lamp | 2048,2048,2048 | 4096x4096 | yok | 5,38/6,21/9,63 · 4,64/5,18/6,18 | 1,87/1,88 | 6,21 | 877/129 / 871/133 | 503 | 18,1 KB | 11,30 / 10,68 · 8,71 / 10,48 | - | sağlar |
| P3 K4 V.001 | Shelf | BoardLamp | 2048 | 2048x2048 | yok | 3,86/4,66/25,90 · 3,79/4,21/5,16 | 1,42/1,27 | 6,37 | 477/114 | 92 | 18,1 KB | 5,37 / 5,19 · 5,08 / 4,77 | medyan -1,0 ms; A3 batch 472/115; 30× -1,2 | sağlar |
| P4 K4 dolu 10+32+4+4 | Shelf | BoardLamp | 2048 | 2048x2048 | yok | 4,71/5,60/6,22 · 4,44/4,88/5,76 | 1,73/1,81 | 5,80 | 904/249 | 188 | 18,1 KB | 10,92 / 8,75 · 10,33 / 8,77 | medyan -1,3 ms; A3 batch 902/248; 30× 1,2 | sağlar |
| P5 K4 dolu, deste açık | Shelf | BoardLamp | 2048 | 2048x2048 | yok | 4,54/4,97/6,50 · 4,43/4,78/5,93 | 1,69/1,81 | 5,86 | 904/254 | 188 | 18,1 KB | 11,46 / 10,47 · 10,35 / 8,48 | medyan -1,3 ms; A3 batch 902/251; 30× 2,1 | sağlar |
| P6 K4 dolu, sürükleme | Shelf | BoardLamp | 2048 | 2048x2048 | yok | 5,17/5,67/7,76 · 4,69/5,15/6,18 | 1,94/1,86 | 6,08 | 917/253 | 187 | 20,0 KB | 10,38 / 14,27 · 9,17 / 10,87 | medyan -0,9 ms; A3 batch 903/250; 30× 1,1 | sağlar |
| P7 K4 kişi dosyası | Shelf | BoardLamp | 2048 | 2048x2048 | yok | 3,99/4,63/5,74 · 3,78/4,16/5,42 | 1,42/1,30 | 6,28 | 486/128 / 486/127 | 89 | 18,1 KB | 5,60 / 5,41 · 5,17 / 4,80 | - | sağlar |
| P8a Harita | Map | Bulb, Desk lamp | 2048,2048 | 4096x2048 | yok | 4,26/4,74/5,57 · 4,16/4,62/5,06 | 2,05/1,06 | 7,53 | 398/54 | 274 | 18,1 KB | 5,04 / 4,99 · 5,02 / 5,04 | - | sağlar |
| P8b K5a CRT | Crt | Bulb, Desk lamp | 2048 | 2048x2048 | yok | 4,23/4,76/5,81 · 4,17/4,70/5,62 | 2,28/0,85 | 7,61 | 226/48 / 226/50 | 97 | 18,1 KB | 4,78 / 4,75 · 4,78 / 4,59 | - | sağlar |
| P9 K6 fiş bekliyor (worst) | ShiftClose | Bulb, Desk lamp | 2048,2048 | 4096x2048 | yok | 4,45/4,94/6,12 · 4,28/4,74/5,64 | 2,02/1,27 | 7,79 | 449/131 / 451/135 | 248 | 14,4 KB | 5,99 / 5,80 · 5,85 / 5,38 | - | sağlar |
| P10 K8 adaysız | Door | Bulb, Desk lamp | 2048,2048 / 2048,2048,1024 | 4096x2048 / 4096x4096 | yok | 3,42/3,75/5,01 · 3,77/4,57/30,28 | 1,30/1,15 | 5,34 | 393/57 / 564/80 | 152 / 253 | 18,1 KB | 4,11 / 4,23 · 5,06 / 5,25 | - | sağlar |
| P11 K8 kapı açık | Door | Bulb, CorridorLight, Desk lamp | 2048,2048,1024 / 2048,2048,1024,1024 | 4096x4096 | yok | 3,84/4,30/5,32 · 4,17/4,70/5,39 | 1,54/1,25 | 6,20 | 464/59 / 670/86 | 322 / 458 | 18,1 KB | 4,67 / 4,55 · 5,64 / 5,57 | - | sağlar |
| P12 K8A present | Admission | Bulb, CounterSpot, Desk lamp | 1024,2048 | 4096x2048 | yok | 4,08/4,79/26,86 · 3,92/4,40/5,02 | 1,80/1,04 | 7,16 | 372/69 | 248 | 18,1 KB | 5,21 / 4,93 · 5,48 / 5,44 | - | sağlar |
| P13 K8A tüm fişler | Admission | Bulb, CounterSpot, Desk lamp | 1024,2048 | 4096x2048 | yok | 4,07/4,55/5,71 · 3,92/4,36/5,69 | 1,73/1,11 | 7,18 | 412/88 | 248 | 18,1 KB | 5,26 / 5,22 · 5,21 / 5,18 | - | sağlar |
| P14 K8G buğu | BreathGlass | Bulb, CounterSpot, Desk lamp | 1024,2048 | 4096x2048 | yok | 3,86/4,44/5,07 · 3,81/4,34/4,79 | 1,94/0,72 | 6,92 | 107/34 | 75 | 18,1 KB | 4,84 / 4,42 · 4,27 / 4,09 | - | sağlar |
| P15 K8R | AdmissionRead | Bulb, CounterSpot, Desk lamp | 1024,2048 | 4096x2048 | yok | 4,15/4,69/5,75 · 4,02/4,43/4,90 | 2,12/0,83 | 7,38 | 222/44 | 230 | 18,1 KB | 4,83 / 4,57 · 4,67 / 4,50 | - | sağlar |
| P16 K9 dolap açık | Cabinet | Bulb, Desk lamp | 2048 / 2048,2048 | 2048x2048 / 4096x2048 | yok | 3,86/4,38/5,78 · 3,82/4,51/6,09 | 1,61/0,94 | 6,83 | 315/49 / 333/51 | 37 / 90 | 18,7 KB | 5,14 / 5,00 · 4,49 / 4,31 | - | sağlar |
| TR1 K1>K8A | Desk>Admission | - | 2048,2048,2048 | - | yok | 5,36/6,39/7,12 · 4,80/5,74/7,48 | -/- | - | - | - | - | - | - | sağlar |
| TR2 K1>K4 | Desk>Shelf | - | 2048,2048,2048 | - | yok | 5,73/6,34/7,96 · 5,37/5,76/7,83 | -/- | - | - | - | - | - | - | sağlar |
| P17.M0 M0 sahanlık | P17.M0 | Bulb, Bulb, Merdiven soğuğu | 1024,1024,1024 | 4096x1024 | yok | 3,48/3,86/4,90 · 3,47/3,82/4,81 | 1,85/0,97 | 6,12 | 515/44 / 516/44 | 240 | 13,3 KB | 4,29 / 4,14 · 4,23 / 4,01 | - | sağlar |
| P17.M1 M1 dolap | P17.M1 | Bulb, Bulb, Merdiven soğuğu | 1024,1024,1024 | 4096x1024 | yok | 4,12/4,53/4,91 · 4,12/4,53/5,14 | 2,46/1,04 | 7,46 | 597/44 | 279 | 13,9 KB | 4,93 / 4,77 · 4,91 / 4,72 | - | sağlar |
| P17.M2 M2 saat | P17.M2 | Bulb, Bulb, Merdiven soğuğu | 1024,1024,1024 | 4096x1024 | yok | 4,43/4,95/5,98 · 4,43/4,87/5,40 | 2,84/0,90 | 8,11 | 343/37 | 189 | 13,4 KB | 4,97 / 4,89 · 5,02 / 4,82 | - | sağlar |
| P17.MS MS son şalter | P17.MS | Bulb, Bulb, Merdiven soğuğu | 1024,1024,1024 | 4096x1024 | yok | 4,17/4,69/19,80 · 4,16/4,58/5,43 | 2,61/0,82 | 7,60 | 193/33 | 88 | 13,3 KB | 4,53 / 4,37 · 4,62 / 4,37 | - | sağlar |
| P18 M3 kapı açılışı | P18 | Bulb, Bulb, Merdiven soğuğu | 1024,1024,1024 | 4096x1024 | yok | 3,36/3,80/5,29 · 3,40/3,82/4,18 | 1,76/0,94 | 5,88 | 457/44 | 174 | 13,3 KB | 4,04 / 3,85 · 3,95 / 3,82 | - | sağlar |
| P19a Baskın plan, T seçili | raid | Desk Lamp (Directional) | - | 0x0 | yok | 2,81/3,12/5,02 · 2,82/3,11/3,67 | 0,77/1,27 | 3,65 | 865/94 | 355 | 12,9 KB | 6,88 / 7,99 · 4,83 / 7,52 | - | sağlar |
| P19b Baskın makine evresi | raid | Desk Lamp (Directional) | - | 0x0 | yok | 3,24/3,81/5,39 · 3,19/3,48/5,21 | 1,04/1,40 | 4,11 | 870/177 | 368 | 13,5 KB | - | - | sağlar |
| P19c Baskın son tur | raid | Desk Lamp (Directional) | - | 0x0 | yok | 2,76/3,09/3,56 · 2,77/3,11/3,61 | 0,74/1,25 | 3,53 | 849/83 | 355 | 12,9 KB | 4,12 / 4,59 · 4,10 / 4,24 | - | sağlar |
| P20 Baskın kit eeu | raid | Desk Lamp (Directional) | - | 0x0 | yok | 3,00/3,31/3,97 · 3,01/3,27/3,67 | 0,82/1,41 | 3,92 | 1063/102 | 493 | 12,9 KB | 5,50 / 7,21 · 6,22 / 8,17 | - | sağlar |
| P21 Baskın eklemeli, oda gizli | raid | Desk Lamp (Directional) | 2048,2048 | 4096x2048 | yok | 2,72/3,03/3,64 · 2,76/3,08/3,67 | 0,80/1,22 | 3,54 | 1020/106 | 464 | 13,4 KB | 4,25 / 6,49 · 4,87 / 7,46 | - | sağlar |

Baskın kit ayrıntısı:

| Kit | Olduğu gibi Δt medyan/p95 (tur 1 · tur 2) | Onarımlı Δt medyan/p95 (tur 1 · tur 2) | Batch | Shadow caster |
|---|---|---|---|---|
| weu | 2,77/3,08 · 2,79/3,05 | 2,92/3,10 · 2,92/3,20 | 1025 | 470 |
| eeu | 2,91/3,30 · 2,87/3,15 | 3,02/3,31 · 3,00/3,22 | 1055 | 490 |
| me | 2,88/3,14 · 2,86/3,12 | 2,98/3,28 · 2,95/3,21 | 1022 | 470 |
| af | 2,75/3,16 · 2,71/2,99 | 2,90/3,25 · 2,85/3,21 | 845 | 371 |
| na | 2,87/3,41 · 2,82/3,15 | 2,94/3,23 · 2,97/3,22 | 1000 | 462 |
| ru | 2,83/3,11 · 2,86/3,12 | 3,04/3,37 · 2,97/3,23 | 1027 | 464 |
| in | 2,86/3,12 · 2,85/3,09 | 3,06/6,96 · 2,98/3,25 | 1046 | 474 |
| cn | 2,90/3,12 · 2,90/3,17 | 3,01/3,28 · 3,04/3,45 | 1033 | 478 |
| jk | 2,86/3,22 · 2,80/3,11 | 2,91/3,19 · 2,92/3,20 | 1011 | 453 |
| sea | 2,80/3,08 · 2,78/3,08 | 2,90/3,16 · 2,96/3,26 | 966 | 434 |
| sa | 2,90/3,51 · 2,85/3,18 | 3,01/3,35 · 3,01/3,31 | 1012 | 451 |
| oc | 2,88/3,21 · 2,82/3,20 | 2,97/3,26 · 3,01/3,32 | 989 | 446 |

### Olduğu gibi (`perf_asis.tsv`, `perf_asis2.tsv`)

| Senaryo | Poz | Gölgeli ışıklar | Dilimler (px) | Atlas | Ölçek uyarısı | Δt medyan/p95/maks ms (tur 1 · tur 2) | CPU ana/render | GPU | Batch/SetPass | Shadow caster | GC/kare | 30× MSAA4 / MSAA1 | Aşama 3 farkı | Hedef |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| P1 K1 V.001 | Desk | BoardLamp, Bulb, Desk lamp | 2047,2047,2047 | 4096x4096 | yok / ÷32 (3 harita) | 4,87/28,44/46,03 · 4,41/4,73/16,45 | 1,56/1,64 | 6,29 | 775/106 / 773/109 | 470 | 19,9 KB | 11,44 / 8,34 · 6,55 / 8,61 | medyan -0,6 ms; A3 batch 790/110; 30× -0,3 | aşar (yük, aşağıya bak) |
| P2 K1 V.031 | Desk | BoardLamp, Bulb, Desk lamp | 2047,2047,2047 | 4096x4096 | yok | 5,32/33,82/60,43 · 4,76/5,08/6,19 | 1,78/1,83 | 6,67 | 864/126 / 864/130 | 503 | 18,9 KB | 11,09 / 9,43 · 9,44 / 10,25 | - | aşar (yük, aşağıya bak) |
| P3 K4 V.001 | Shelf | BoardLamp | 4095 | 4096x4096 | ÷16 (1 harita) / yok | 4,28/23,65/49,89 · 3,89/4,32/5,72 | 1,37/1,29 | 6,71 | 476/114 | 92 | 19,1 KB | 7,74 / 7,39 · 5,25 / 5,04 | medyan -0,7 ms; A3 batch 472/115; 30× 0,0 | aşar (yük, aşağıya bak) |
| P4 K4 dolu 10+32+4+4 | Shelf | BoardLamp | 4095 | 4096x4096 | yok | 5,20/32,12/73,61 · 4,65/5,13/6,26 | 1,75/1,87 | 6,73 | 903/249 | 188 | 18,9 KB | 6,46 / 7,59 · 10,20 / 8,87 | medyan -1,0 ms; A3 batch 902/248; 30× -1,1 | aşar (yük, aşağıya bak) |
| P5 K4 dolu, deste açık | Shelf | BoardLamp | 4095 | 4096x4096 | yok | 5,27/30,98/54,51 · 5,04/5,49/7,09 | 1,83/1,92 | 6,70 | 903/254 | 188 | 18,9 KB | 12,33 / 8,70 · 9,11 / 14,12 | medyan -0,6 ms; A3 batch 902/251; 30× 1,9 | aşar (yük, aşağıya bak) |
| P6 K4 dolu, sürükleme | Shelf | BoardLamp | 4095 | 4096x4096 | yok | 5,42/34,04/43,88 · 4,98/5,62/6,52 | 1,94/1,90 | 7,12 | 916/252 / 916/253 | 187 | 19,9 KB | 9,26 / 10,62 · 8,96 / 9,83 | medyan -0,6 ms; A3 batch 903/250; 30× 0,4 | aşar (yük, aşağıya bak) |
| P7 K4 kişi dosyası | Shelf | BoardLamp | 4095 | 4096x4096 | yok / ÷16 (1 harita) | 4,39/27,21/55,68 · 4,04/4,41/5,43 | 1,52/1,33 | 6,81 | 485/128 | 89 | 18,9 KB | 7,62 / 8,97 · 5,37 / 5,23 | - | aşar (yük, aşağıya bak) |
| P8a Harita | Map | Bulb, Desk lamp | 2047,2047 | 4096x2048 | ÷32 (2 harita) | 4,10/22,63/90,58 · 4,31/4,89/18,84 | 1,41/1,14 | 7,38 | 390/54 | 274 | 19,8 KB | 5,20 / 4,99 · 5,85 / 5,45 | - | aşar (yük, aşağıya bak) |
| P8b K5a CRT | Crt | Bulb, Desk lamp | 4095 | 4096x4096 | ÷16 (1 harita) | 3,97/23,38/57,09 · 4,37/6,01/33,18 | 1,56/0,94 | 7,67 | 227/47 / 227/49 | 97 | 19,9 KB | 5,20 / 5,02 · 5,27 / 5,34 | - | aşar (yük, aşağıya bak) |
| P9 K6 fiş bekliyor (worst) | ShiftClose | Bulb, Desk lamp | 2047,2047 | 4096x2048 | ÷32 (3 harita) | 4,35/24,86/36,74 · 4,37/4,76/6,01 | 1,67/1,29 | 7,45 | 445/131 / 445/135 | 248 | 14,3 KB | 8,86 / 7,90 · 5,69 / 5,40 | - | aşar (yük, aşağıya bak) |
| P10 K8 adaysız | Door | Bulb, Desk lamp | 2047,2047 / 2047,2047,32 | 4096x2048 / 4096x4096 | ÷32 (2 harita) / yok | 3,99/23,10/147,78 · 4,07/9,20/44,51 | 1,31/1,20 | 5,21 | 390/56 / 562/79 | 152 / 253 | 20,0 KB | 5,08 / 6,26 · 5,83 / 8,88 | - | aşar (yük, aşağıya bak) |
| P11 K8 kapı açık | Door | Bulb, CorridorLight (Point), Desk lamp | 2047,2047,32,32,32,32,32,32 / 2047,2047,32,32,32,32,32,32,32 | 4096x4096 | ÷32 (8 harita) / ÷32 (3 harita) | 4,32/24,80/43,72 · 4,60/8,18/687,14 | 1,50/1,40 | 5,78 | 558/63 / 812/96 | 591 / 775 | 19,2 KB | 7,83 / 6,76 · 11,79 / 8,53 | - | aşar (yük, aşağıya bak) |
| P12 K8A present | Admission | Bulb, CounterSpot, Desk lamp | 32,2047 | 4096x2048 | ÷32 (2 harita) / yok | 4,00/20,44/34,50 · 4,03/19,72/74,56 | 1,27/1,08 | 6,40 | 372/69 | 248 | 20,1 KB | 5,34 / 4,89 · 5,11 / 4,87 | - | aşar (yük, aşağıya bak) |
| P13 K8A tüm fişler | Admission | Bulb, CounterSpot, Desk lamp | 32,2047 | 4096x2048 | yok | 4,14/22,88/43,84 · 4,19/23,50/41,09 | 1,33/1,16 | 6,14 | 412/88 | 248 | 20,1 KB | 6,13 / 9,17 · 5,60 / 7,06 | - | aşar (yük, aşağıya bak) |
| P14 K8G buğu | BreathGlass | Bulb, CounterSpot, Desk lamp | 32,2047 | 4096x2048 | yok | 3,57/17,12/51,14 · 3,64/16,92/53,19 | 1,12/0,71 | 6,64 | 107/34 | 75 | 18,8 KB | 4,38 / 4,23 · 4,60 / 4,23 | - | sağlar |
| P15 K8R | AdmissionRead | Bulb, CounterSpot, Desk lamp | 32,2047 | 4096x2048 | yok / ÷32 (2 harita) | 3,80/20,62/41,20 · 3,76/20,88/35,40 | 1,19/0,86 | 7,10 | 229/48 | 230 | 20,1 KB | 4,80 / 4,55 · 4,91 / 4,64 | - | aşar (yük, aşağıya bak) |
| P16 K9 dolap açık | Cabinet | Bulb, Desk lamp | 4095 / 2047,2047 | 4096x4096 / 4096x2048 | ÷16 (1 harita) / ÷32 (3 harita) | 3,93/20,20/50,24 · 4,02/20,27/56,07 | 1,17/0,98 | 6,26 | 333/50 / 336/50 | 46 / 52 | 21,6 KB | 4,86 / 4,67 · 5,21 / 5,02 | - | aşar (yük, aşağıya bak) |
| TR1 K1>K8A | Desk>Admission | - | 2047,2047,2047 | - | ÷32 (3 harita) / ÷32 (2 harita) | 5,78/28,08/35,79 · 4,85/5,83/7,64 | -/- | - | - | - | - | - | - | aşar (yük, aşağıya bak) |
| TR2 K1>K4 | Desk>Shelf | - | 2047,2047,2047 | - | ÷32 (3 harita) | 6,06/42,34/52,48 · 5,55/5,95/8,47 | -/- | - | - | - | - | - | - | aşar (yük, aşağıya bak) |
| P17.M0 M0 sahanlık | P17.M0 | Bulb, Bulb, Merdiven soğuğu | 1024,1024,1024 | 4096x1024 | yok | 3,46/3,75/4,25 · 3,47/3,81/4,44 | 1,89/0,91 | 6,12 | 500/43 / 501/43 | 237 | 13,3 KB | 8,01 / 7,48 · 4,07 / 3,94 | - | sağlar |
| P17.M1 M1 dolap | P17.M1 | Bulb, Bulb, Merdiven soğuğu | 1024,1024,1024 | 4096x1024 | yok | 4,10/4,47/4,84 · 4,12/4,58/17,03 | 2,50/0,97 | 7,45 | 586/43 | 276 | 14,0 KB | 4,87 / 4,65 · 4,93 / 4,68 | - | sağlar |
| P17.M2 M2 saat | P17.M2 | Bulb, Bulb, Merdiven soğuğu | 1024,1024,1024 | 4096x1024 | yok | 4,41/4,88/16,19 · 4,42/4,81/6,21 | 2,75/0,92 | 8,09 | 329/35 | 186 | 14,3 KB | 4,95 / 4,78 · 4,95 / 4,76 | - | sağlar |
| P17.MS MS son şalter | P17.MS | Bulb, Bulb, Merdiven soğuğu | 1024,1024,1024 | 4096x1024 | yok | 4,17/4,52/5,42 · 4,16/4,60/5,44 | 2,62/0,81 | 7,58 | 193/33 | 88 | 13,3 KB | 4,55 / 4,35 · 4,54 / 4,38 | - | sağlar |
| P18 M3 kapı açılışı | P18 | Bulb, Bulb, Merdiven soğuğu | 1024,1024,1024 | 4096x1024 | yok | 3,24/3,61/5,32 · 3,28/3,65/5,75 | 1,71/0,90 | 5,72 | 443/43 / 442/43 | 172 | 13,4 KB | 3,93 / 3,75 · 3,91 / 3,85 | - | sağlar |
| P19a Baskın plan, T seçili | raid | Desk Lamp (Directional) | - | 0x0 | yok | 2,68/2,97/4,68 · 2,68/2,98/3,44 | 0,72/1,22 | 3,50 | 865/94 | 355 | 12,9 KB | 4,82 / 4,76 · 4,76 / 4,76 | - | sağlar |
| P19b Baskın makine evresi | raid | Desk Lamp (Directional) | - | 0x0 | yok | 3,08/3,40/5,43 · 3,08/3,42/4,88 | 0,98/1,35 | 3,95 | 871/174 | 368 | 13,6 KB | - | - | sağlar |
| P19c Baskın son tur | raid | Desk Lamp (Directional) | - | 0x0 | yok | 2,62/2,92/3,31 · 2,61/2,93/3,30 | 0,70/1,19 | 3,33 | 849/83 / 851/83 | 355 / 356 | 12,9 KB | 3,90 / 4,76 · 3,86 / 4,09 | - | sağlar |
| P20 Baskın kit eeu | raid | Desk Lamp (Directional) | - | 0x0 | yok | 2,88/3,15/3,48 · 2,90/3,20/3,90 | 0,78/1,35 | 3,71 | 1063/102 | 493 | 12,9 KB | 4,96 / 7,50 · 4,97 / 7,65 | - | sağlar |
| P21 Baskın eklemeli, oda gizli | raid | Desk Lamp (Directional) | 2047,2047 | 4096x2048 | ÷32 (2 harita) | 3,57/16,42/29,70 · 2,99/3,16/3,55 | 0,85/1,35 | 4,20 | 1020/106 | 464 | 13,4 KB | 4,79 / 6,18 · 5,91 / 7,75 | - | sağlar |

### Karşılaştırma

| Senaryo | Olduğu gibi medyan (tur 1 · tur 2) | Onarımlı medyan (tur 1 · tur 2) | Olduğu gibi p95 (tur 1 · tur 2) | Onarımlı p95 (tur 1 · tur 2) | 30× MSAA1 olduğu gibi → onarımlı (ort.) | Dilimler olduğu gibi → onarımlı |
|---|---|---|---|---|---|---|
| P1 | 4,87 · 4,41 | 4,64 · 4,20 | 28,44 · 4,73 | 5,44 · 4,53 | 8,47 → 7,70 | 2047,2047,2047 → 2048,2048,2048 |
| P2 | 5,32 · 4,76 | 5,38 · 4,64 | 33,82 · 5,08 | 6,21 · 5,18 | 9,84 → 10,58 | 2047,2047,2047 → 2048,2048,2048 |
| P3 | 4,28 · 3,89 | 3,86 · 3,79 | 23,65 · 4,32 | 4,66 · 4,21 | 6,21 → 4,98 | 4095 → 2048 |
| P4 | 5,20 · 4,65 | 4,71 · 4,44 | 32,12 · 5,13 | 5,60 · 4,88 | 8,23 → 8,76 | 4095 → 2048 |
| P5 | 5,27 · 5,04 | 4,54 · 4,43 | 30,98 · 5,49 | 4,97 · 4,78 | 11,41 → 9,48 | 4095 → 2048 |
| P6 | 5,42 · 4,98 | 5,17 · 4,69 | 34,04 · 5,62 | 5,67 · 5,15 | 10,22 → 12,57 | 4095 → 2048 |
| P7 | 4,39 · 4,04 | 3,99 · 3,78 | 27,21 · 4,41 | 4,63 · 4,16 | 7,10 → 5,11 | 4095 → 2048 |
| P8a | 4,10 · 4,31 | 4,26 · 4,16 | 22,63 · 4,89 | 4,74 · 4,62 | 5,22 → 5,02 | 2047,2047 → 2048,2048 |
| P8b | 3,97 · 4,37 | 4,23 · 4,17 | 23,38 · 6,01 | 4,76 · 4,70 | 5,18 → 4,67 | 4095 → 2048 |
| P9 | 4,35 · 4,37 | 4,45 · 4,28 | 24,86 · 4,76 | 4,94 · 4,74 | 6,65 → 5,59 | 2047,2047 → 2048,2048 |
| P10 | 3,99 · 4,07 | 3,42 · 3,77 | 23,10 · 9,20 | 3,75 · 4,57 | 7,57 → 4,74 | 2047,2047 / 2047,2047,32 → 2048,2048 / 2048,2048,1024 |
| P11 | 4,32 · 4,60 | 3,84 · 4,17 | 24,80 · 8,18 | 4,30 · 4,70 | 7,64 → 5,06 | 2047,2047,32,32,32,32,32,32 / 2047,2047,32,32,32,32,32,32,32 → 2048,2048,1024 / 2048,2048,1024,1024 |
| P12 | 4,00 · 4,03 | 4,08 · 3,92 | 20,44 · 19,72 | 4,79 · 4,40 | 4,88 → 5,19 | 32,2047 → 1024,2048 |
| P13 | 4,14 · 4,19 | 4,07 · 3,92 | 22,88 · 23,50 | 4,55 · 4,36 | 8,12 → 5,20 | 32,2047 → 1024,2048 |
| P14 | 3,57 · 3,64 | 3,86 · 3,81 | 17,12 · 16,92 | 4,44 · 4,34 | 4,23 → 4,25 | 32,2047 → 1024,2048 |
| P15 | 3,80 · 3,76 | 4,15 · 4,02 | 20,62 · 20,88 | 4,69 · 4,43 | 4,59 → 4,54 | 32,2047 → 1024,2048 |
| P16 | 3,93 · 4,02 | 3,86 · 3,82 | 20,20 · 20,27 | 4,38 · 4,51 | 4,84 → 4,65 | 4095 / 2047,2047 → 2048 / 2048,2048 |
| TR1 | 5,78 · 4,85 | 5,36 · 4,80 | 28,08 · 5,83 | 6,39 · 5,74 | - | 2047,2047,2047 → 2048,2048,2048 |
| TR2 | 6,06 · 5,55 | 5,73 · 5,37 | 42,34 · 5,95 | 6,34 · 5,76 | - | 2047,2047,2047 → 2048,2048,2048 |
| P17.M0 | 3,46 · 3,47 | 3,48 · 3,47 | 3,75 · 3,81 | 3,86 · 3,82 | 5,71 → 4,07 | 1024,1024,1024 → 1024,1024,1024 |
| P17.M1 | 4,10 · 4,12 | 4,12 · 4,12 | 4,47 · 4,58 | 4,53 · 4,53 | 4,67 → 4,74 | 1024,1024,1024 → 1024,1024,1024 |
| P17.M2 | 4,41 · 4,42 | 4,43 · 4,43 | 4,88 · 4,81 | 4,95 · 4,87 | 4,77 → 4,86 | 1024,1024,1024 → 1024,1024,1024 |
| P17.MS | 4,17 · 4,16 | 4,17 · 4,16 | 4,52 · 4,60 | 4,69 · 4,58 | 4,37 → 4,37 | 1024,1024,1024 → 1024,1024,1024 |
| P18 | 3,24 · 3,28 | 3,36 · 3,40 | 3,61 · 3,65 | 3,80 · 3,82 | 3,80 → 3,83 | 1024,1024,1024 → 1024,1024,1024 |
| P19a | 2,68 · 2,68 | 2,81 · 2,82 | 2,97 · 2,98 | 3,12 · 3,11 | 4,76 → 7,75 | - → - |
| P19b | 3,08 · 3,08 | 3,24 · 3,19 | 3,40 · 3,42 | 3,81 · 3,48 | - | - → - |
| P19c | 2,62 · 2,61 | 2,76 · 2,77 | 2,92 · 2,93 | 3,09 · 3,11 | 4,42 → 4,42 | - → - |
| P20 | 2,88 · 2,90 | 3,00 · 3,01 | 3,15 · 3,20 | 3,31 · 3,27 | 7,58 → 7,69 | - → - |
| P21 | 3,57 · 2,99 | 2,72 · 2,76 | 16,42 · 3,16 | 3,03 · 3,08 | 6,96 → 6,97 | 2047,2047 → 2048,2048 |

Medyanlarda fark küçüktür (K4 ailesinde −0,1…−0,7 ms; K8A ailesinde −0,3…+0,35 ms, tezgâh gölgesi 32 → 1024). **p95 farkı onarıma atfedilemez:** olduğu gibi turu 1'de DeskGame'in bütün senaryoları 16–34 ms'dir (TR2 geçişi 42); tur 2'de yalnız turun ilk beş senaryosu (P16–P12, ters sıra) 17–24 ms'dir, sonrakiler ≤ 9,2 ms. Bu zamana bağlı, sahneden bağımsız bir dış yük örüntüsüdür. Onarımın kesin etkileri: atlas uyarısının kalkması ve dilim boyutları. P11'de 3–4 dilime düşüş batch'i 558–812 → 464–670'e indirdi.

### Geçiş takılması (F4)

| Geçiş | Durum | Tur | Δt medyan/p95/maks | Gölgeli ışık | Dilimler | Not |
|---|---|---|---|---|---|---|
| TR1 K1>K8A | olduğu gibi | 1 | 5,78/28,08/35,79 | 3 | 2047,2047,2047 | planlanan=0.45s; atlas_kare=46 |
| TR2 K1>K4 | olduğu gibi | 1 | 6,06/42,34/52,48 | 3 | 2047,2047,2047 | planlanan=0.60s; atlas_kare=53 |
| TR1 K1>K8A | olduğu gibi | 2 | 4,85/5,83/7,64 | 3 | 2047,2047,2047 | planlanan=0.45s; atlas_kare=90 |
| TR2 K1>K4 | olduğu gibi | 2 | 5,55/5,95/8,47 | 3 | 2047,2047,2047 | planlanan=0.60s; atlas_kare=109 |
| TR1 K1>K8A | onarımlı | 1 | 5,36/6,39/7,12 | 3 | 2048,2048,2048 | planlanan=0.45s; atlas_kare=85 |
| TR2 K1>K4 | onarımlı | 1 | 5,73/6,34/7,96 | 3 | 2048,2048,2048 | planlanan=0.60s; atlas_kare=106 |
| TR1 K1>K8A | onarımlı | 2 | 4,80/5,74/7,48 | 3 | 2048,2048,2048 | planlanan=0.45s; atlas_kare=92 |
| TR2 K1>K4 | onarımlı | 2 | 5,37/5,76/7,83 | 3 | 2048,2048,2048 | planlanan=0.60s; atlas_kare=113 |

Hedef geçiş kümesi ≤ 4 dilim; ölçülen 3. Yoklama TR1/TR2'de kameranın aldığı dilimleri geçişin kendi karesinden okur.

### T1 sonrası kısa tur (`perf_short.tsv`, 17:12–17:13)

| Senaryo | Onarımlı (T1 öncesi, tur 1 · tur 2) Δt medyan/p95 | T1 sonrası Δt medyan/p95/maks | Batch/SetPass | 30× MSAA4 / MSAA1 | Dilimler |
|---|---|---|---|---|---|
| P1 | 4,64/5,44 · 4,20/4,53 | 4,21/4,57/7,48 | 785/109 | 6,36 / 6,07 | 2048,2048,2048 |
| P3 | 3,86/4,66 · 3,79/4,21 | 3,78/4,26/6,22 | 477/114 | 5,17 / 4,80 | 2048 |
| P4 | 4,71/5,60 · 4,44/4,88 | 4,56/5,17/6,07 | 904/249 | 8,32 / 9,26 | 2048 |
| P13 | 4,07/4,55 · 3,92/4,36 | 3,93/4,34/5,17 | 412/88 | 5,14 / 5,01 | 1024,2048 |

Fark gürültü aralığında; T1'in kapalı nesneleri çizim yapmıyordu.

### Q13: yumuşak ve sert gölge

Çiftli ABAB, 5 çift, 30× RT çizim; sert kümesi her ışığı `LightShadows.Hard` yapar. Yalnız onarımlı sahnede ölçüldü (`as` kipi); plandaki "olduğu gibi" iki sütun alınmadı.

| Poz (onarımlı sahne) | Çift | Yumuşak MSAA4 ort. | Sert MSAA4 ort. | Fark (sert − yumuşak) MSAA4 ort. [en az … en çok] | Yumuşak MSAA1 ort. | Sert MSAA1 ort. | Fark MSAA1 ort. [en az … en çok] |
|---|---|---|---|---|---|---|---|
| P1 | 5 | 7,97 | 8,73 | 0,77 [-1,20 … 3,16] | 8,61 | 8,88 | 0,27 [-1,32 … 1,40] |
| P3 | 5 | 5,96 | 7,18 | 1,23 [-0,26 … 4,51] | 6,80 | 5,76 | -1,04 [-6,47 … 0,88] |
| P13 | 5 | 6,68 | 6,25 | -0,43 [-1,81 … 0,97] | 6,16 | 6,94 | 0,78 [-0,67 … 2,64] |
| P8a | 5 | 6,25 | 5,46 | -0,79 [-3,31 … 0,31] | 6,01 | 5,46 | -0,56 [-4,22 … 1,73] |
| P19a | 5 | 3,84 | 5,53 | 1,69 [0,91 … 3,20] | 4,05 | 6,88 | 2,83 [0,19 … 4,19] |

Fark işaretleri pozlar arasında değişir ve pek çok çift gürültü eşiğini (|d| ≤ 0,15 ms) aşar; sert gölge beklendiği gibi ucuz da çıkmıyor (P19a'da sert daha pahalı görünür). Tek başına bir kazanç gösterilemedi. **Karar A10-D6: yumuşak kalır.** Görüntü çiftleri `Temp/a10_q13_P1_as_{soft,hard}.png`, `Temp/a10_q13_P13_as_{soft,hard}.png`.

### TAA ve post-processing

| Senaryo | Değişim | Taban Δt medyan/p95 | Değişken Δt medyan/p95 | Taban GPU | Değişken GPU | Taban batch/SetPass | Değişken batch/SetPass | 30× MSAA1 taban → değişken |
|---|---|---|---|---|---|---|---|---|
| P1 | TAA kapalı | 4,29/4,81 | 4,14/4,56 | 6,05 | 5,22 | 785/107 | 782/104 | 5,97 → 7,35 |
| P3 | TAA kapalı | 3,80/4,28 | 3,44/3,94 | 6,87 | 5,07 | 477/114 | 474/111 | 4,88 → 4,41 |
| P1 | post kapalı | 4,26/4,68 | 3,90/4,40 | 6,21 | 4,85 | 785/107 | 765/87 | 5,89 → 6,57 |
| P3 | post kapalı | 3,78/4,34 | 3,22/3,67 | 6,80 | 4,67 | 477/114 | 457/94 | 4,91 → 3,98 |
| P13 | post kapalı | 3,91/4,41 | 3,27/3,71 | 7,05 | 5,90 | 412/88 | 392/68 | 5,15 → 4,12 |
| P19a | TAA kapalı | 2,77/3,08 | 2,62/2,99 | 3,56 | 3,25 | 864/94 | 848/90 | 7,87 → 6,23 |
| P19a | post kapalı | 2,78/3,12 | 2,42/2,78 | 3,59 | 2,90 | 864/94 | 831/73 | 6,58 → 4,74 |

Oyun döngüsünde TAA kapatmak 0,15–0,36 ms, post kapatmak 0,36–0,64 ms kazandırır; 30× sütunu gürültülüdür. Oyuncuya ayar eklenmedi (A10-D9). Baskın TAA kalitesi 3, öteki kameralar 4. Görüntüler `Temp/a10_{P1,P3,P13,P19a}_{base,notaa,nopost}.png`: `Temp` kalıcı değildir ve görsel karşılaştırma sonucu kayda geçmedi.

### Yapılmayanlar

R15 (baskın 2048/4096, proje kopyası gerekir), oyuncu derlemesi, PC ölçümü, sahne yükleme süresi. Ayrıntı haritada §6 ve §9.

## Kaldırma (T1, A10-D1)

Silmeden önce üç sahnenin referans anlık görüntüsü alındı; her adımda `Rev3SceneRefAudit.Compare` ve eksik script sayımı.

| Kademe | İçerik | Doğrulama |
|---|---|---|
| T1a | `SiginakRoom/Shelf` (674 GO) ve `SiginakRoom/Shelter controller` (1 GO) = **675** | `CampaignWear.stages[0..2].objects` 8 referans BOŞALDI, 0 DEĞİŞTİ. `stages` girdileri kaldı (`stages[3]` kaymasın) |
| T1c | `Planning board/Serbest pano kartları` = **485** GO | `PlanningBoardView` 95 + `PlanningBoardMotion` 1 (`detailsRoot`) = 96 BOŞALDI. Bileşenler yerinde, alanlar null (D20 emsali) |
| Toplam | **1.160 GO** | DeskGame: boşalan 104, değişen 0; Giris, Baskin 0 / 0 / 0 |
| T1b | Erişilemez asset **165**: `Desk/Art/Siginak/Materials` 127, `Desk/Art/Siginak/Meshes` 27, `Desk/Art/PlanningBoard` 6, `Desk/Prefabs/Components` 5; 2 klasör | GUID kapanışı üç build sahnesi, `Resources`, ProjectSettings üstünden; liste dışı başvuru 0. Sprite'lara dokunulmadı |
| Araç | `SiginakCaptureSet.cs` emekli (`Rev3CaptureSet` yerine); `SiginakCapture.OutDir` Rev3 klasörünü kullanır | Derleme bağımlılığı yok |

`DeskGame.unity` T1 öncesi 14,77 MB (14.766.918 bayt, zip'teki kopya; aşama başında 14,44) → 11,72 MB (11.720.579 bayt).

**Yedek:** `A10_silinenler.zip`, 335 dosya (165 × 2 + 2 klasör `.meta` + 3: `t1b_list.txt`, önceki `DeskGame.unity` ve `.meta`), açılmış 15,7 MB. `Docs/UI_Siginak_Rev3` ve `Desk/Art/PlanningBoard` git'te izlenmediği için yedek tek geri dönüş yoludur. Çalışma kopyası `Library/A10T1/`.

**Testler:** `SiginakShelterTests`'ten 2 test silindi (`DeskGameScene_ShelterIsWired`, `DeskGameScene_StoreKeepsAuthoredScale`). `SiginakRoomTests` (Shelf yol listesi; malzeme eşiği 50 → 30, kalan 34), `PlanningBoardSceneTests` (raf konumu sabit, eski tuval yok, `detailsRoot` boş) güncellendi.

**Yapılmayan:** T2 (eski kapı, vardiya raporu, görünüm kodu) ve T3 (Core) kapsam dışı (A10-D1, A10-D2). Yeniden düşünülürse derleme ve canlı yol bağımlılıkları: `SiginakDoor` ve `SiginakShift` `OnEnable`'ları korumasız; `ReportTone`/`ReportTones`, `SlipButtonState`, `RaidGuide`/`RaidGuideStep` silinecek dosyalarda ama canlı yolda kullanılıyor; `Poses/K6a` ve `SiginakPose.Shift` enum kilitli ve masa deposunda. T3 altın replay kanıtı ister, fark çıkarsa kayıtlar düşer.

## Sesler (A10-D5)

`Tools/Audio/rev3_placeholder_audio.py` (stdlib, tohumlu, iki koşuda aynı bayt) 42 klibi `Audio/Placeholder/Cue<ad>.wav` olarak üretti; `Settings/SoundBank.asset` bağlandı. Biçim: mono, 44,1 kHz, 16 bit, DecompressOnLoad; süre 0,06–2,6 s, tepe 0,45–0,80.

| Aralık | Cue |
|---|---|
| 56–66 | PinDrop, PaperSlide, Pencil, PawnPlace, Hammer, DoorPeep, DoorClose, StampHeavy, AlarmHum, ResultBell, FinalBell |
| 97–106 | BoardLift, BoardMagnet, BoardReturn, BoardDeny, BoardDeckSlide, BoardDeckCollect, BoardItemTuck, BoardHurda, BoardPadlock, BoardFlap |
| 107, 108, 110–114 | ShiftCoverOpen, ShiftCoverShut, SlipPrint, SlipTear, SlipFile, ShiftGong, SlipMouthEmpty |
| 115–119 | BoardBookOpen, BoardPageTurn, BoardBookClose, BoardRecipeLift, BoardProductSlide |
| 120–128 | AdmissionCounterDown, AdmissionDrawer, AdmissionMarker, AdmissionSlip, AdmissionIntercom, AdmissionGlass, AdmissionPadTear, AdmissionStampLift, AdmissionCounterFold |

`ShiftLever` (109) sessiz değildi, geçici `SwitchClack` kaldı. Testler: "klip yok" kilitleri "klip var"a döndü; `EveryCue_HasAnAudibleClip`, `SoundBank_ShiftCloseCuesHaveOwnClips` yeni. **Kulakla dinlenmedi:** düzey ve karakter kullanıcının kontrolüne bırakıldı.

## Koruma

Bu belge yazılırken (Play kapalı, salt okuma) başlangıç yedeğiyle karşılaştırıldı:

| Veri | Sonuç |
|---|---|
| `liberation_savegame.json`, `.v13.backup`, `.v14.backup`, `savegame.json`, `liberation_desk.json`, `planning_board_layout.json`, `player_profile.json` | Bayt bayt aynı |
| Font dosyaları (24 SDF + `BSS-ExtraBold SDF URP Lit.mat`) | 25/25 aynı |
| Dil tercihi | Başlangıçta `tr`; EN ölçümleri test yönlendirmesi ve geçici tercih ile |

Ölçümler `Temp/A10Storage` test klasöründe koştu. Tek seferlik `Rev3Stage10*` ve `A10WireHints` Assets'ten silindi. Commit/push yapılmadı. Plandaki `A10_koruma_denetimi.json` yazılmadı; karşılaştırma yukarıdaki tablodadır.

## Görüntüler

`screens_unity_rev3/A10_<ID>_{TR,EN}.jpg` ve renk denetimi için `A10_<ID>_TR_gray.jpg`; toplam **259 JPG** (87 etiket × 3, B1 yalnız EN). PNG'ler boyut için JPG'ye çevrildi.

| Aile | ID |
|---|---|
| Menü | B1, M0a, M0b, M1, M2, M2c, MS, M3 |
| Masa | K1a, K1b, K1c, K1d, BR, O1, O2, FR1, FR2 |
| Harita | K2a, K3a, K3b, K3c, K3d1, K3d2 |
| Pano | K4a, K4b, K4c, K4d, K4e, K4f, K4g, K4h, K4i, K4j, K4k, K4l, K4m, K4n, F1, F2, F3, BK1, BK2, BK3 |
| CRT | C1, C2, C3, C3_canli, C4 |
| Şalter | S1, S2, S3, S4, S5, S6, S7, S8 |
| Kapı | D1, D2, A1, A2, A3, A4, A5, A6, A7_Admit, A7_Aid, A7_Reject, A7_Restricted, G1, AR1, AR2, AR3, AR4 |
| Dolap | K9a, K9b |
| Baskın | Z1, Z2, Z3, Z4, Z5, Z6, Z7, Z8, Z9, Z10, Z11 |
| Üzerine gelme | H1_longest |

`A7_rack` yalnız taramadadır. C3 baskın kuruluyken alınır, `C3_canli` birleşik görüntüdür. Sim durumları sunum fikstüründen ya da gerçek komutlarla kurulmuştur (tohum 79). Ham ölçümler: `A10_dil_olcumleri.json`, `punto_taramasi.txt`.

## Sapmalar (bu aşamaya özgü)

- Oyun döngüsü ölçümleri 1920 × 1080 olarak kanıtlanmadı (`screen=555x808`, odak yok). 1920 × 1080 kesin olan 30× RT çizimleri en kötü MSAA4 11,46, MSAA1 14,27 ms.
- Q13'ün olduğu gibi sütunları, R15, oyuncu derlemesi, PC ölçümü, sahne yükleme süresi yapılmadı.
- p95 kazancı ilk ölçüm turunda dış yükle karışıktır; yalnız K8 ailesi değil, olduğu gibi turu 1'de bütün DeskGame senaryoları etkilenmişti.
- Görsel karşılaştırma 2 geçişle (6 + 3 bölüm) yapıldı; G09 yakın kamerayla çözülmedi.
- H1, Crt, Cabinet, MS, M1'de satır üretmedi. `A10_koruma_denetimi.json` yazılmadı.
- Sapmaların bütünü (EN D-C/D-D, G18, görsel farklar): `IMPLEMENTATION_MAP.md` §5.
