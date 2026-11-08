# Son Şalter · Revizyon 3 + Ek 11-05 — Unity uygulama talimatı

Bu dosya Claude Code içindir. Revizyon 1 ve 2 ile kurulan sığınak sunumunun üstüne **Revizyon 3 (modeller)** ve
**Ek 11-05 (kartlı pano, kapıda kabul, vardiya devri)** tasarımını uygulayacaksın. Önce bu dosyanın tamamını oku, sonra
**Aşama 0**'dan başla. **Aşama 0'dan sonra dur ve onay bekle.** Her aşamanın sonunda kısa rapor yaz; commit'ten önce sor.

---

## 0. Görev

Oyun: “Son Şalter / Corrigible”, Steam/PC için tek oyunculu strateji, 1920 × 1080, Unity 6 URP. Oyuncu KYGA'nın son
sığınağı SIĞINAK 07'nin komutanı; komuta odası analog bir masa oyunu gibi çalışır (doktrin: “AĞA BAĞLANMA · ANALOG
KAL”). Ekranda neredeyse hiç arayüz yok: her şey tıklanan, sürüklenen fiziksel nesnedir. Fare yeter.

**Bugünkü hâl.** Kurallar `Docs/` altındaki oyun belgelerinde (özellikle `OYUN-TASARIMI.md` “uygulanmış hâl”,
`KARTLI-VARDIYA-PANOSU-DETAYLI-TASARIM.md`, `TOPLULUK-KADRO-VE-SIZMA-TASARIMI.md`) ve kodda (`Core/Simulation*.cs`,
`PlanningBoardState`, `CommunityAdmissionState`, `GameController`) uygulanmış durumda: 8 isimli kişi, kartlı pano,
üretim, belgeyle kabul, şalter → devir fişi → EVRAK akışı çalışıyor. Ama sunum geçici: pano K1'de sol üstü kaplayan
düz bir levha, kartlar 210 × 294 px UI kartı, kapıdaki kabul kahverengi bir pencere ve düz düğmeler, oda nesnelerinin
çoğu kutu. Revizyon 2 sunumu `Docs/UI_Siginak_Rev2/` (PROMPT + `IMPLEMENTATION_MAP.md`) ile kurulu.

**Bu iş.** Sunumu bu paketteki tasarıma getir:

- **Revizyon 3:** odadaki, menüdeki ve baskın tahtasındaki bütün 3B nesneler gerçek ölçekte, pahlı, malzemesi ayrılmış,
  hareketli parçaları pivotlu **modeller** olur (198 model, OBJ + manifest). Menü sahanlığı ve ayarlar dolabı (R1–R4),
  komuta odası (R5–R10), baskın kitleri (R11–R12), malzeme/yıpranma/ışık (R14).
- **Ek 11-05:** ayaklı **kartlı pano** (R15, K4) ve kart ailesi (R16), **tarif defteri ve üretim** (R18), **kapıda
  belgeli kabul** (R17, K8A/K8G/K8R), **vardiya kapanışı ve devir fişi** (R19, K6). Değişen: R5, R6, R9, R10, R13.
  Kalkan: R7 (maket) ve onunla raf ailesi, oda kartları, BOŞTA tepsisi, özet panosu, ziyaretçi silüetleri, madenci ve
  sis piyonları.

**Kurallar, sayılar, kayıt biçimi ve oyun akışı değişmez; görünüş, malzeme, yerleşim, etkileşimin fiziksel biçimi ve
okunurluk değişir.**

İş bitmiş sayılır, eğer:

1. Bütün pozlar (M0–M3, MS, K1, harita, K4, K5a, K6, K8, K8A/G/R, K9, baskın) `ekranlar/` ve `tasarim/panolar/`
   görüntülerine yerleşim, ölçü, ışık ve okunurluk bakımından denk düşüyorsa (TR; R3, R6, R11, R15, R17'de EN de),
2. modeller manifestteki boyut, pivot, parça aralığı, malzeme anahtarı ve TMP yuvalarıyla kuruluysa,
3. yeni ve değişen her etkileşimli öğenin durumları çalışıyorsa (normal · üzerinde · tutuldu · hedef üstünde · bırakıldı ·
   geçersiz · devre dışı · kilitli), süreler `motion.*` token'larıyla,
4. her metin TR + EN gösterilebiliyorsa (EN %30'a kadar uzun; punto küçülmez, satır sarar; kişi adları çevrilmez),
5. oyun mantığı, denge ve kayıt biçimi (`GameRecord` biçim 14) değişmemişse: aynı tohum + komut geçmişi aynı sonucu verir,
6. Revizyon 2 ile Revizyon 3 görünümü arasında bir anahtarla geçilebiliyorsa (eski görünüm onaydan sonra kaldırılır).

---

## 1. Paket içeriği ve doğruluk sırası

```
SonSalter_R3/                         → projede Docs/UI_Siginak_Rev3/ altına koy
  PROMPT.md                           ← bu dosya
  README.md                           ← paket özeti, token işaretleri, açık kararlar
  tokens/tokens_siginak.json          ← 1.3: “_new_1_3” (117 yol), “_changed_1_3” (28), “_was_1_3” (eski değerler)
  tokens/raid_tokens.json             ← 1.3: “_new_1_3” (72)
  modeller/
    README.md                         ← OBJ kuralı, Unity'ye alma sırası (OKU)
    obj/<Model>.obj + .mtl            ← 198 model; Y yukarı, metre, OBJ x = −Unity x
    manifest/<Model>.json             ← boyut, üçgen, parçalar (pivot/eksen/aralık/ebeveyn), malzeme anahtarları,
                                        TMP yuvaları (TXT_*), çapalar, ışık noktaları, dünya yerleşimi (place)
    sayfa/<Model>.jpg                 ← model sayfası (önden/sağdan/üstten/perspektif, ölçü, pivot)
    MODELLER.md · modeller.csv · models.json
  threejs/                            ← görüntüleri üreten sahne: KONUM, ÖLÇÜ, AÇI, ANİMASYON YOLU KAYNAĞI (README'yi oku)
    r3/models/*.js                    ← model kaynakları (açıklamalar Türkçe)
    r3/dress_room_board.js            ← pano: kart dizilişi, deste, sürükleme, takas, eşya yığını, defter/dosya pozları
    r3/dress_room_switch.js           ← şalter durumları, fiş çıkışı, el, EVRAK üstü, bırakma
    r3/dress_room_admission.js        ← kabul tezgâhı, araçlar, damgalar, K8A/K8G/K8R
    r3/boardtex.js · portraits.js     ← kart yüzü düzeni (mm) ve portre çizimi
    r3/community.json                 ← örnek kadro verisi (görüntülerde kullanılan durumlar)
    r10/papers_new/*@2x.png           ← R10 kâğıt dokuları (TR/EN)
  tasarim/
    README.md                         ← 92 panonun dizini
    panolar/*.jpg                     ← tuvaldeki bütün panolar (ekranlar, uygulama notları, model sayfaları, karşılaştırmalar)
    notlar/*.md                       ← aynı panoların metni; *-not.md = eski → yeni, nesneler, durumlar, hareket ve ses
    BRIEF_rev3.md · EK_11-05.md       ← brifler (kısıtların kaynağı)
    portre/p_*.png                    ← 10 portre (8 kişi + 2 aday)
    kagit_kaynak/*.html               ← R10 kâğıtlarının HTML kaynağı (px, font, renk)
  ekranlar/R01-R14/ · ekranlar/ek_11-05/   ← render'lar (panoların içindeki görüntülerin tam boyu)
```

Doğruluk sırası (çelişkide üstteki kazanır):

1. **bu dosya**
2. oyunun kuralları (`Docs/OYUN-TASARIMI.md` ve kod): tasarım bir kuralla çelişirse **kural kazanır**, bana sor
3. `tokens/*.json` (1.3)
4. `modeller/manifest/*.json` (ölçü, pivot, yuva, çapa, yerleşim)
5. `tasarim/notlar/*-not.md` (davranış, süre, sıra, ses)
6. `threejs/r3/` (giydirme modüllerindeki konumlar ve yollar)
7. `tasarim/kagit_kaynak/*.html` (kâğıtların px değerleri)
8. `tasarim/panolar/*.jpg`, `ekranlar/*` (görünüş)
9. Revizyon 2 paketi `Docs/UI_Siginak_Rev2/` (Revizyon 3'ün değinmediği her yerde geçerli)

Görüntülerdeki sayılar **örnektir**; sunum sayıları gösterir, değiştirmez. Çözemediğin çelişkiyi
`Docs/UI_Siginak_Rev3/IMPLEMENTATION_MAP.md` → “Açık sorular”a yaz ve bana sor.

---

## 2. Değişmez kurallar

**Motor ve teknik**
- Unity 6, URP. 3B sahne + **uGUI + TextMeshPro** (World Space ve Overlay Canvas). **UI Toolkit yok.**
- **Özel shader yok** (Shader Graph dahil): URP Lit / Simple Lit / Unlit, `UI/Default`, TMP shader'ları. Bloom ve
  post-processing'e dayanan efekt yok; parlama yalnız emission. Oyuncu post-processing'i ve TAA'yı kapatabilir: görünüş
  ikisi kapalıyken de doğru olmalı.
- Sahne/prefab YAML'ını elle düzenleme; kurulumu editor script'leriyle yap (Revizyon 1–2'nin builder ve importer'larını
  genişlet). Unity'ye MCP/CLI erişimin varsa sahneyi aç, builder'ı çalıştır, Play Mode'a gir, görüntü al; yoksa menü
  komutları yaz ve çalıştırmamı iste.

**Modeller ve malzemeler**
- Modeller `modeller/obj` + `manifest` ile içe aktarılır (§4). Geometri değiştirilmez; düzeltme gerekirse bana sor.
- Bütçe: büyük ≤ 4000, küçük ≤ 800, baskın parçası ≤ 400, makine/piyon ≤ 1500 üçgen (aşan yok).
- Her malzeme anahtarı için URP Lit değerleri token'dan (`materials.*`, `materials3d.*`, `kits.*`). Aynı değerli anahtarlar
  birleşebilir (oda + menü ≈ 45 paylaşılan malzeme; `usedBy` listesi yardımcı). MTL dosyalarını kullanma.
- **Modellerde ve dokularda kelime yoktur** (R10 kâğıt dokuları ve portreler hariç): her yazı bir `TXT_*` TMP yuvasıdır.
- Paylaşılan detay haritası (metrik UV, `tx_wear_detail` 0,25 m'de bir) ve pah kuralı R14b'de.

**Işık ve gölge**
- Bir pozda **en çok 4 gölgeli ışık**; gölgeyi **spot** taşır (nokta ışığı küp gölge = 6 dilim), nokta ışığı yalnız dolgu.
  Poz başına tarif `tokens_siginak.json → lightsRecipe` (K1 3 gölgeli: ampul, masa lambası, pano lambası; K4: pano lambası
  tek gölgeli; K8A 3). 2 cm altı parçalar Cast Shadows Off. Kâğıtların albedosu ≤ 0,8 (lamba altında beyaz patlamaz).

**Oyun mantığı**
- Simülasyon, denge, kampanya, pano, kabul ve baskın kuralları ile kayıt biçimi **değişmez.** Sunum yalnız okur ve
  **mevcut komutları** çağırır (`board_*`, `board_scrap`, `board_file`, `inspectApplicant`, `decideApplicant`,
  `reviewResident`, `restrictResident`, `dismissResident`, `GameController.FileBoardShift`, `PrintShift` …; gerçek adları
  Aşama 0'da eşle). Yeni bir sayı gerekiyorsa (ör. hammadde kartındaki serbest adet) sunum tarafında hesapla.
- Var olan public API'leri silme ya da yeniden adlandırma. Sunum durumu (kamera pozu, açık defter sayfası …) gerekiyorsa
  `liberation_desk.json` tarafına yaz, oyun kaydına değil.
- Eski yol (`Balance.planningBoardEnabled = false`: oda geliştirmeli sığınak, 23 ziyaretçili kapı) **bozulmaz.** Onun
  sunumu (maket, oda kartları, siluetler) Revizyon 2 hâlinde kalır; Revizyon 3 modelleri kartlı oyunu hedefler. Eski yolun
  görsellerini kaldırmak ya da yükseltmek Aşama 0'da benim kararım.

**Kamera ve kontroller**
- Pozlar: menüde M0, M1 (dolap), M2 (saat), MS (son şalter), M3 (eşik); odada K1, harita, **K4 (pano)**, K5a (CRT),
  **K6 (şalter)**, K8 (kapı), **K8A/K8G/K8R (kabul)**, K9 (dolap); baskında izometrik tahta. Değerler `cameras.*`.
  K4 kamerası panoya bağlıdır (`PlanningBoard.Cam_K4` / `Look_K4`).
- Kısayollar korunur (2, 3, 4/K, Home, F, Enter, Esc, Boşluk); her birinin tıklanan bir karşılığı vardır. Panoda Esc ve sağ
  tık = MASAYA DÖN. **SIĞINAK tuşu K4'e gider.** K4, K6 ve K8A/G/R pozlarında sayaç şeridi ve istasyon çipleri gizlenir.
- Kartlı oyunda **rehberli eğitim yok**: yeni yönerge metni, ok, parlayan çerçeve, ipucu balonu ekleme. Yol gösteren şey
  nesnenin kendisi (kabarık cep çerçeveleri, pano lambası, fiş ağzının amber lambası, şalter plakasının ikinci satırı,
  vuruş lambası). Tasarımdaki üzerine-gelme etiketleri (400 ms sonra ad + görev) kalır.

**Dil, yazı, okunurluk**
- Her metin TR + EN (projedeki yerelleştirme sistemi). Sayı biçimi: TR virgül (62,0), EN nokta (62.0). Kişi adları
  (Cem, İpek, Zeynep, Eren, Nermin, Suna, Deniz, Selin, Aylin, Rauf …) çevrilmez.
- Fontlar: **Courier Prime** 400/700, **Libre Franklin** 400/600/700, **Kalam** 700 (el yazısı: tebeşir başlık, etiketler),
  **Big Shoulders Stencil Display** 800 (şablon yazılar: pano başlığı, HURDA). TMP font asset'lerinde Türkçe glifler tam.
- 1920 × 1080'de **kart adı ≥ 18 px, kart değerleri ≥ 14 px** (K4'te tasarım 21 px ve 14–17 px; kamera 1,52 px/mm);
  öteki yerlerde gövde ≥ 18, ikincil ≥ 14 px. Dünya metinleri o nesnenin okunduğu pozda ölçülür.
- **Hiçbir durum yalnız renkle verilmez:** her durumun kelimesi, damgası, işareti ya da deseni vardır (ör. eşya yuvaları
  kit düz / zırh taralı / yardım noktalı, gri tonda ayrılır).
- Renk anlamı: kırmızı = YZ, tehdit, kilit · mavi = geri alınmış · yeşil = iyi · sarı = seçili. Neon, hologram, HUD yok.

**Sınırlar (veriden okunur, sunum bunları taşıyabilmeli):** en çok 10 kişi, 7 alan, 4 hammadde, 32 eşya kartı, erzak
tavanı 60.

**Güvenli geçiş:** `SiginakPresentation` anahtarına bir seviye ekle (Rev2 / Rev3). Rev2 görünümünü son aşamada, ben
onayladıktan sonra kaldır.

---

## 3. Değişikliklerin özeti

| Ekran | Eski | Yeni (ayrıntı `tasarim/notlar/<Rn>-not.md`) |
|---|---|---|
| R1 menü sahanlığı (M0) | Beyaz fayans, düz ışık, çıplak kapı, tabela | Sırlı fayans + kalıp izli beton, ıslak zemin; soğuk sahanlık lambaları, merdivenden mavi-gri ışık, kapı altından sıcak şerit; beton portal, iki yüzlü kanat; şablon başlık betonda; döküm interkom + zil; bıçaklı son şalter; M0 (4,50 · 1,52 · 0,30) → (2,2 · 1,34 · 0,30), 56° |
| R2 saat ve kart (M2) | Düz kutu saat, tasarımsız kart | Sivil savunma puantaj saati (3 tekerli VARDİYA penceresi), 90 × 184 mm kart (V.014, özgür 3/12, 10 × 8 zımba ızgarası), ahşap raf + çelik cep, 40 boş kartlı deste (iki tıklı kıskaç) |
| R3 ayarlar dolabı (M1, K9) | Düz kutu, panelde parlak leke | Pahlı sac kasa, siperlikli iç ampul (spot 70°, gölgesiz), bakalit panel, sürgüler, ibreli göstergeler, 80 mm pirinç plakalar; menüde kanat −120°, MENÜYE DÖN yerinde kör kapak |
| R4 içeri girme | Ayrı ayrı hızlar | Tek sekans: kart → zil → kapı → eşik; kamera iki kez hareket eder; `LoadSceneAsync(DeskGame)` kanat açılırken başlar |
| R5 K1 | Solda raf + maket; şalter tek satır plaka | Solda **ayaklı pano** (yaw −48°, sağ yarısı görünür); şalterin solunda **fiş ağzı**, plaka iki satır; ERZAK **/60**, MALZEME = dört hammaddenin toplamı; tebeşir çentiği vardiya çetelesi |
| R6 harita, görev fişi, kadro raporu | EKİP ALFA T/E/A; +malzeme | SAHA KADROSU üç kişi adıyla (CAN/HAR/GÜÇ + eşya); ödül hammadde türleriyle; kadro raporunda kişi başına kit · zırh · yardımcı yuvası, kalıcı ölüm uyarısı |
| R7 maket | Kesit maketi, oda kartları, BOŞTA tepsisi, özet panosu | **Kalktı** (kartlı oyunda yok) |
| R8 CRT ve dalış (K5a) | Kutular | Çelik konsol, 1950'ler CRT, ceviz tüplü telsiz, bas-konuş mikrofon, A5 kayıt defteri; K5a önerisi: ekran yerelinde +0,14 m sağa, FOV 52° (doğrula) |
| R9 kapı (K8) | Siluetli koridor, 23 ziyaretçi, ölümcül kırmızı ışık | Perçinli kanat, beton söve, döküm çark, vuruş lambası **sönük/amber** (kırmızı yok); gözetleme ve interkom kabulün araçları; koridor hep boş; kapının sağında kabul istasyonu |
| R10 kâğıtlar | Dağınık biçimler | Tek kâğıt sistemi (künye, kırmızı çizgi, bölüm etiketi); yeni: devir fişi, kişi dosyası, kabul seti (talimat v1–v3, kimlik/sevk belgesi, bulgu fişleri, damga izleri), tarif sayfaları. Vardiya raporu, kapı kâğıdı, eğitim notu yalnız eski yolda |
| R11–R12 baskın | 12 kit yalnız tonla ayrılıyor; anonim beş uzman | Kit başına kabartma, köprü, ayrık kolon, moloz; bölgenin sivil cihazı; giriş kapısı; 8 makine silueti; piyon T/E/A + ad şeridi (3 isimli kişi); kâğıt paneller, antet |
| R13 model kütüphanesi | — | Aile aile model sayfaları (pano, kart, kabul, fiş ağzı eklendi; raf ve ziyaretçiler çıktı) |
| R14 stil | — | 72 malzeme anahtarı (20'si ek), yıpranma kuralları, poz başına ışık tarifi |
| **R15 pano (K4)** | Düz yeşil uGUI tahta, düğmeler, yönerge satırları | Ayaklı mıknatıslı çelik pano 1196 × 636 mm; kelimesiz baskı; 6 görev cebi + Boşta destesi + iş emri + kişi dosyası + depo + hurda; MASAYA DÖN levhası, PLAN AÇIK/KİLİTLİ plakası, kilit çubuğu, tebeşir başlık, pano lambası |
| **R16 kartlar** | 210 × 294 px koleksiyon kartı, kısaltmalar | Kişi 80 × 120 · eşya 80 × 50 · hammadde 80 × 56 · fiş 56 × 40 · tarif 112 × 80 mm; hepsi 0,6 mm; mürekkep portre; beş durum kaplaması; eşya kartları kişinin arkasına sıkışır, 14 mm şerit görünür |
| **R17 kabul (K8A/G/R)** | Kahverengi pencere, üç sütun, sekiz düz düğme | Katlanır tezgâh + panel + tezgâh lambası; evrak gözü (duvarda); talimat şeridi (kayan çizgi işaretçisi); büyüteç, interkom, buğu camı (kanatta), GİDEN · TELSİZ; dört karar = dört damga |
| **R18 defter ve üretim** | “AÇMAK İÇİN TIKLA” kutusu, ÖNCEKİ/SONRAKİ düğmeleri | Kancada A6 ciltli defter; öne gelir, açılır, sekmelerle sayfa; tarif kartı sayfadan kopya olarak kalkar; klipsli iş emri formu; hammadde fişleri |
| **R19 vardiya kapanışı (K6)** | Kapaklı kutu + iç tuş denemesi | Brifteki kapaklı şalter olduğu gibi; sol yanında fiş ağzı; devir fişi 12 cm kayar (0,35 s) → elde → EVRAK → 0,2 s kararma → teleks “VARDİYA DEVREDİLDİ”; bekleme hâli plakanın 2. satırında |

Önerilen kararlar (tuvaldeki karşılaştırmalar; Aşama 0'da bana teyit ettir): **1 · M0 kamerası → yeni değerler**,
**2 · baskında giriş kapısı → alçak (≤ 0,30) ya da açık kasa**, **3 · piyonun “tur bitti” yüzü → koyu kraft
`r_pawnDone`**, **4 · pano → ayaklı** (duvarda K1'de görünmüyor), **5 · kabul → kapıdaki tezgâh**.

---

## 4. Model hattı (OBJ + manifest → prefab)

`modeller/README.md`'yi oku. Özet:

- **İlk iş `_AxisTest`:** uzun kol +x (sağ), kısa kol +z (arka), tehlike küpü üstte ve öne bakar; `Flap` +z ekseninde
  0 → 90°. İçe aktarma: Scale Factor 1, Convert Units kapalı, eksen dönüşümü yok. Böyle görünmüyorsa düzelt, sonra devam et.
- Her hareketli parça ayrı bir `o`/`g` grubudur ve köşeleri **pivotuna göre** yazılmıştır. Prefab builder: alt nesne
  `localPosition = pivotLocal` (ebeveyn parçaya göre, `parent` alanından hiyerarşi), dönüş ekseni `axis` (yerel x/y/z),
  aralık `range` (derece; `slide` için metre), dinlenme `rest`. `swap` parçalar yalnız malzeme ya da görünürlük değiştirir
  (lamba camı açık/kapalı, KİLİTLİ bandı, mıknatıs, bayrak, bant).
- `materials[]` → URP Lit malzeme (token değerleri). `texts[]` → TMP nesnesi: `center`, `size` (m), `normal`, `up`, `capMm`
  (büyük harf yüksekliği; font boyutu fontun cap-height oranından, three.js 0,7 kullandı), `role` (tip), `font`, `sample` (TR),
  `sampleEn` (EN; yuva EN'e göre ölçülü). `anchors[]` → boş alt nesne. `lights[]` → Light (tür, renk, gölge bayrağı;
  şiddet `lightsRecipe` + `preview` değerlerinden URP'ye çevrilir). `place[]` → sahnedeki dünya yerleşimi (Unity metre, yaw°).
- Aileler: `menu` 14, `cabinet` 12, `desk` 20 (SlipMouth, HandoverSlip dahil), `walls` 12, `crt` 5, `door` 8, `board` 8,
  `admission` 10, `raidkit` 52, `raidcivil` 30, `raidpieces` 26, `test` 1. Raf ailesi, `Visitor_*`, `Pawn_M`, `Pawn_S`
  pakette yok (token `models3d.removed_1_3`).
- Doğrulama: her prefab manifestteki `bbox` ± 1 mm, `tris` aynı; parçalar aralığında doğru yöne döner; `sayfa/<Model>.jpg`
  ile yan yana görüntü.

---

## 5. Mimari

Revizyon 1–2 bileşenlerini **genişlet**, yeniden yazma. Gerçek adlar `Docs/UI_Siginak_Rev2/IMPLEMENTATION_MAP.md`'de;
aşağıdakiler öneri. Yeni kod ve asset'ler: `Assets/Corrigible/Presentation/Siginak/Rev3/` (Scripts, Editor, Prefabs,
Materials, Models, Textures, Data). Sunum yalnız okur; her değişiklik mevcut komutla.

**Altyapı**

| Bileşen | Görev |
|---|---|
| `Rev3ModelImporter` (Editor) | OBJ + manifest → prefab (parça hiyerarşisi, malzeme, TMP yuvaları, çapalar, ışık noktaları); `_AxisTest` denetimi |
| `Rev3MaterialLibrary` (SO) | Token 1.3 malzemeleri → URP Lit; birleştirme tablosu; emission ve saydam varyantlar |
| `PartDriver` | Parçayı açıyla / kaymayla / swap ile sürer; süre ve eğri `motion.*` |
| `Rev3RoomBuilder` (Editor) | Modelleri `place` konumlarına koyar, eski nesneleri Rev3 seviyesinde gizler, poz başına ışık tarifini kurar |
| `PoseLightRig` | Poz değişince tarif ışıklarını açar/kapatır (≤ 4 gölgeli) |
| `TmpSlotBinder` | `TXT_*` yuvalarını yerelleştirme anahtarlarına bağlar (plaka metinleri token `board.plates`, `shiftClose.*`) |

**Kartlı pano (R15, R16, R18)** — veri: `PlanningBoardState`; yerleşim: token `board.layoutMm` + çapalar (`Slot_*`,
`Bosta_Deck`, `Bosta_HandLeft`, `Order_Recipe`, `Order_Inputs`, `Folder`, `Mat_*`, `Kit_0–5`, `Book_Hook`, `Hurda_Slot`).

| Bileşen | Görev |
|---|---|
| `BoardView3D` | Pano prefab'ı, kelimesiz baskı, plaka yazıları, tebeşir başlık (plan değişince 0,2 s silinme lekesiyle yeniden yazılır), PLAN AÇIK/KİLİTLİ plakası (0,3 s), kilit çubuğu (0,4 s + asma kilit; etiketin 2. satırı nedeni yazar), MASAYA DÖN levhası (üzerinde 4 mm öne), pano lambası |
| `CardView` + `CardFace` | Beş kart prefab'ı. **Yüz:** kartın ön yüzüne hizalı World Space Canvas (1 birim = 1 mm ölçek), TMP + Image; düzen `threejs/r3/boardtex.js` (mm) ve token `cards.*`. RenderTexture'a pişirmek yalnız performans gerekirse ve onayla. Portre: `tasarim/portre/p_*.png`, kırpma viewBox [36, 26, 228, 250] |
| `CardStateOverlay` | Yaralı (bant), yatıyor (mavi YATIYOR damgası), kısıtlı (kırmızı köşe), inceleme (kırmızı İNCELE bayrağı), kayıp (portre kararır + siyah yas köşesi + KAYIP · V.nnn) |
| `BoardDragController` | Fareyle sürükle-bırak: pano düzlemine ışın; üzerinde 4 mm, tutulunca 10 mm kalkış; hedef cep amber çerçeve; dolu cebe bırakma takas (0,32 s); geçersiz bırakma kavisle geri (0,25 s); oturma 0,06 s + mıknatıs “tık”; kilitli planda 2 mm titreme |
| `BostaDeckView` | Deste: tık → kartlar **üst karttan başlayarak sola** açılır (kart başına 320 ms, 70 ms ara, aralık min(82, 636/(n−1)) mm); tekrar tık ya da boşluğa tık → **alttan** toplanır; elden biri alınınca diğerleri desteye döner; yarım harekette yön değişimi mevcut konumdan |
| `ItemStackLayout` | Eşya kartları kişinin arkasında 14 mm aralıkla (kit, zırh, yardımcı); kişi taşınınca birlikte gider; depoya bırakılınca çıkar (0,15 s) |
| `StoreView` | Dört hammadde stok kartı (büyük serbest sayı, birim noktaları, ayrılan kırmızı halka, alt satırda stok/ayrı); kit yığınları ve ×n etiketi (×0 sönük) |
| `WorkOrderView` | Klipsli form: tarif kartı kutusu, girdi alanı; fişler bırakıldığı serbest açıyla kalır; yetersiz kaynakta fiş kalkmaz, stok kartının sayısı 0,2 s kırmızı yanar |
| `RecipeBookView` | Kancadan öne gelir (0,35 s), kapak 0,25 s, sayfa 0,25 s, kapanma 0,3 s; 3 açılım × 2 tarif (doku `tarif_acilim_1..3`); tarif sayfadan kopya kart olarak kalkar, defter kadrajın altına kayar |
| `PersonFolderView` | Tel sepetteki manila klasör; seçili kişinin adı kapakta; tık → öne gelir ve açılır; sayfalar R10 kişi dosyası (İNCELE · ERİŞİM · TAHLİYE mevcut komutlara) |
| `HurdaBin` | Eşya kartı bırakılınca kapak 28° (0,15 s) + “tunk”; `board_scrap` |
| `TrainingTopics` | Konu listesi + kırmızı yağlı kalem halkası (satıra tık halkayı yeniden çizer) |

**Vardiya kapanışı (R19)** — `ShiftSwitchView`'u genişlet; `SlipMouthView`, `HandoverSlipView`. Şalter yerelinde fiş
`SlipStart` (−0,18; 0,14; 0,015) → `SlipExit` (−0,30; 0,14; 0,015), 0,35 s. Fiş elde 0,25 s, tepsiye 0,2 s; EVRAK üstündeyken
gölgesiz amber nokta ışık (#FFB44A, 0,55, menzil 0,35 m); EVRAK dışına bırakılırsa dile döner. Bekleme: kapak açık, kol inik,
hazır lambası sönük, şalter tıklanmaz, plakanın 2. satırı “fişi EVRAK'a koy” (kırmızı). Fiş dokusu R10 `devir_fisi_*`
(ya da `kagit_kaynak/` düzeniyle uGUI). Bağlantı: kol inince `PrintShift` → fiş çıkar; tepsiye bırakınca
`GameController.FileBoardShift` → 0,2 s kararma → teleks.

**Kapıda kabul (R17)** — veri: `CommunityAdmissionState`. `AdmissionStation` (tezgâh, panel, tezgâh lambası, evrak gözü,
talimat şeridi tutucusu, GİDEN · TELSİZ, damga rafı + 4 damga, büyüteç, talep defteri, erzak tenekeleri, dosya etiketi),
`ColdGlassView` (kanatta, `Hinge`'e bağlı), `StampDecision` (kalkış 0,1 s, basış 0,12 s “tak”; kapasite 10/10 iken KABUL ve
KISITLI kilitli, erzak < 2 iken YARDIM kilitli). Pozlar K8A/K8G/K8R. Kâğıtlar R10 PNG'leri (`kabul_talimati_v2`, `_v3`,
`kimlik_sevk_*`, `bulgu_*`, `damga_*`, `kisi_dosyasi_*`); v1 için doku yok, `kagit_kaynak/kabul_talimati_v2_*.html` düzeniyle
yalnız ilk kuralı (tarih ve numara eşleşmesi) yazarak üret. Aday görünmez; robot gizli kalır.

---

## 6. Aşamalar ve kabul ölçütleri

Her aşamanın sonunda: proje derlenir, Console'da hata yok, testler geçer, Rev2/Rev3 anahtarı çalışır, ilgili pozların
TR (ve gerekiyorsa EN) görüntüsü alınır (§8), bana kısa rapor yazılır. **Aşama 0'dan sonra dur.** Commit'ten önce sor.
Sıra oynanma sıklığına göre: önce K1, K4 ve K6, sonra kapı ve üretim.

### Aşama 0 — Keşif (kod yazma)
Eşle: Revizyon 2 uygulaması (`IMPLEMENTATION_MAP.md`); bugünkü pano sunumu (kart prefab'ları, sürükleme, deste açma,
eşya yığını, depo, iş emri, hurda, defter, dosya) ve bunların çağırdığı komutlar; kabul yüzeyi ve komutları; şalter,
`PrintShift`, EVRAK kutusu, `FileBoardShift`; sayaç bankası (ERZAK /60 dokusu); anahtar rayı (SIĞINAK → K4); menü
(`Giris`), ayarlar dolabı, puantaj saati; CRT/telsiz; baskın tahtası (`RaidBoard3D`, `RaidPieceLibrary`, `RaidRoomKit`,
`RaidTileSkin`) ve **oyundaki baskın kamerasının px/kare değeri** (referans 132,6; token 88,4); yerelleştirme anahtarları;
eski yolun sunumu. `_AxisTest`'i içe aktar ve eksenleri doğrula.
**Çıktı:** `Docs/UI_Siginak_Rev3/IMPLEMENTATION_MAP.md` — tablo: *Revizyon 3 öğesi · Mevcut karşılık (script/prefab/
sahne yolu) · Yapılacak (yeniden kullan / değiştir / yeni) · Veri kaynağı / komut*; ardından açık sorular (§10), riskler
ve teyit edilecek beş karar.

### Aşama 1 — Model hattı
`Rev3ModelImporter`, `Rev3MaterialLibrary`, `PartDriver`, TMP yuva kurucusu. Bütün aileleri içe aktar. Test sahnesinde
aile aile görüntü. **Kabul:** bbox ± 1 mm, üçgen eşit, her parça aralığında doğru yöne döner, TMP yuvaları yerinde ve
TR/EN örnek metinle dolu; `sayfa/*.jpg` ile görsel karşılaştırma.

### Aşama 2 — Komuta odası temeli (R5, R14)
`Rev3RoomBuilder`: oda modelleri `place` konumlarında (masa, sayaç rafı, anahtar rayı, şalter + fiş ağzı, teleks, lamba,
EVRAK, damga rafı, duvarlar, CRT köşesi, kapı); kartlı oyunda raf/maket yok, yerinde pano. Işık tarifi (K1 3 gölgeli).
Sayaç: ERZAK /60, MALZEME toplamı. **Kabul:** K1 (V.001 ve V.031) `ekranlar/ek_11-05/R5a_K1_ilk.jpg`, `R5b_K1_gec.jpg` ile
denk; gölgeli ışık sayısı ≤ 4; K1'de panonun sağ yarısı (Boşta, dosya, hurda) görünür.

### Aşama 3 — Kartlı pano ve kartlar (R15, R16)
Pano, beş kart, kart yüzü, durum kaplamaları, sürükle-bırak, takas, deste, eşya yığını, depo, hurda, eğitim konusu, kişi
dosyası, başlık, plaka, kilit çubuğu, MASAYA DÖN; K4 kamerası (K1 → K4 0,6 s, geri 0,5 s). **Kabul:** R15a–f ve R16a–c
görüntüleri; 1920 × 1080'de kart adı ≥ 18 px, değerler ≥ 14 px (ölç ve rapora yaz); her durum (üzerinde, tutuldu, hedef,
takas, geçersiz, kilitli) videoda ya da görüntü dizisinde; 10 kişi + 32 eşya ile performans (K4'te kare süresi).

### Aşama 4 — Vardiya kapanışı (R19)
Şalter durumları (hazır · kapak kalkık · fiş bekliyor · kilitli), plakanın 2. satırı, fiş ağzı ve lambası, fiş yolu,
el, EVRAK hedefi, kararma, teleks. **Kabul:** R19a/b/c görüntüleri; fiş EVRAK'a girmeden gün ilerlemez; fişi okumak zamanı
ilerletmez; aynı fiş yeniden kullanılmaz; kayıttan yükleyince basılı fiş geri gelir.

### Aşama 5 — Tarif defteri ve üretim (R18)
Defter (kanca, öne gelme, sayfa), tarif kartı kopyası, iş emri formu, hammadde fişleri, ilerleme kutuları, ürünün depoya
kayması. **Kabul:** R18a–d; yetersiz kaynak ve kilitli plan durumları.

### Aşama 6 — Kapı ve kabul (R9, R17)
Kapı modelleri (kanat, kasa, çark, vuruş lambası sönük/amber, gözetleme, interkom, koridor boş); kabul istasyonu, araçlar,
talimat sürümleri (4. ve 8. vardiya), dış kayıt bekleme ve sonucu, damgalar ve kilitleri, buğu camı (K8G), okuma pozu (K8R),
kişi dosyasındaki iz. **Kabul:** R9a/b, R17a–f, R17-EN.

### Aşama 7 — Harita, kâğıtlar, CRT (R6, R8, R10)
Görev fişi (SAHA KADROSU), kadro raporu, operasyon raporu, kâğıt sistemi; CRT köşesi modelleri, K5a önerisini doğrula.
**Kabul:** R6-TR/EN, R8a/b, R10a–c.

### Aşama 8 — Menü sahanlığı ve ayarlar dolabı (R1–R4)
Sahanlık modelleri ve ışığı, puantaj saati, kart rafı ve deste, son şalter, dolap (menü ve oda), giriş sekansı. Tek
“Sahanlık” prefab'ı önerisi (menü = koridor) için önce bana sor. **Kabul:** R1a–f, R2, R3a–f, R4.

### Aşama 9 — Baskın kitleri (R11, R12)
Kit modelleri (Relief/ReliefIso/Bridge/Rubble, Civil, EntryDoor, kanal), makineler, piyonlar + ad şeridi, kâğıt paneller,
antet. Ölçek Aşama 0'daki ölçümle (88 px/kare ise ince parça ≥ 0,035 birim). **Kabul:** R11a–c, R11b-EN, R12a/b (gri tonda
kitler biçimle ayrılır).

### Aşama 10 — Dil, okunurluk, performans, temizlik
Bütün pozların EN geçişi, punto ölçümleri, gölge atlası ve kare süresi; Rev2 görünümünü **onayımdan sonra** kaldır;
`IMPLEMENTATION_MAP.md`'yi son hâline getir.

---

## 7. Ekran ekran kısa özet (ek)

- **R15 pano:** yerleşim (−1,62 · 0 · 1,70) yaw −48°; çerçeve 1196 × 636, iç alan 1160 × 600 mm (`board.layoutMm`).
  Cepler: TEKNİSYEN, EMP UZMANI, ANALİST (üstte kırmızı SAHA · BASKIN KADROSU şeridi; kit şeridi “KİT · GEREKLİ”),
  ÜRETİM EKİBİ, MUTFAK, EĞİTİM (konu listesi + kalem halkası). Her cep tek kişi; dolu cebe bırakmak takas eder; BOŞTA çok
  kişi alır. Başlık arduvazı: “V.001 · erzak 24 (−2,1) · 8/10 kişi”. K4'te pano lambası tek gölgeli + ampul dolgu + ortam 0,5.
- **R16 kartlar:** kişi kartı: ad 14 mm, meslek 9,5, değer etiketi 9,5 mono, değer 11,5 mono, şerit 9,2; portre 44 × 48 mm
  (5, 29); ustalık sütunu; seçili kart ince sarı çerçeve #E3B23C; kayıp kart KAYIPLAR zarfına geçer (ad ve geçmiş kalır).
  Eşya kartında kimlik bandı altta (14 mm). Hammadde kartında serbest sayı 22 mm, nokta Ø 3,4 mm (en çok 20, fazlası “+n”).
- **R17 kabul:** süreler token `admission.motion`; ses: kanat “tank”, damga “tak”, interkom cızırtısı 0,2 s, döküm 0,03 s/harf;
  buğu 0,6 s'de belirir, 2,5 s'de söner. EN etiketleri ADMISSION · PAPERS · OUT · RADIO · FINDING(S) · ORDERS · OLD ·
  BREATH GLASS · DESK · ADMIT · FULL ACCESS · RESTRICTED ADMISSION · REJECT · OUTSIDE AID.
- **R18 üretim:** tarifler (zırh plakası 2 metal + 1 kumaş · bakım kiti 1 kumaş + 1 kimyasal · EMP hücresi 1 metal + 1 devre,
  2 vardiya · üç rol kiti 1 metal + 1 devre) verinin kendisi; kartlar veriden çizilir.
- **R19 kapanış:** kapak 0,25 s (“tık”), kol 0,3 s (“klak-klunk”), fiş 0,35 s (yazıcı “tırrt” + kopma “çıt”), kararma 0,1 +
  0,1 s (arada `Step`, kısık “gong”), sayaç tekerleri 0,4 s, teleks 1,2 s; şalter kapağı kendiliğinden kapanır.

Ses adları öneridir; projedeki `AudioDirector`/`SoundBank` yapısına göre eşle, eksik sesi yer tutucuyla işaretle.

---

## 8. Doğrulama döngüsü

1. Play Mode'da 1920 × 1080, her poz için TR (gerekiyorsa EN) görüntü al; `ekranlar/` ve `tasarim/panolar/` ile yan yana
   koy, farkları rapora yaz (yerleşim, ölçü, ışık, okunurluk).
2. Punto ölçümü: kart adı, kart değerleri, plaka yazıları, kâğıt gövdesi (px). Eşiğin altı hata sayılır.
3. Gölgeli ışık sayısı (Frame Debugger ya da Light listesi) ≤ 4; gölge atlası dilim boyutu.
4. Üçgen ve bbox denetimi (importer testi); parça aralıkları.
5. Kayıt: yeni harekât → birkaç vardiya (atama, takas, eşya, üretim, kabul, fiş) → kaydet → yükle; durum ve kart dizilişi
   aynı kurulur; komut geçmişi değişmemiş.
6. EditMode testleri: importer (manifest → hiyerarşi), kart yüzü düzeni (mm → RectTransform), deste açılım hesapları.
   PlayMode testleri: sürükle-bırak akışları, şalter → fiş → EVRAK.

---

## 9. Çalışma biçimi

- Aşama 0'dan sonra dur. Her aşamadan sonra kısa rapor (yapılan, görüntüler, ölçümler, açık sorular). Commit'ten önce sor.
- Sahne ve prefab'ları builder'larla kur; elle YAML yok. Büyük değişiklikleri küçük, geri alınabilir adımlara böl.
- Kuralla çelişen ya da veride olmayan bir şey görürsen uydurma: `IMPLEMENTATION_MAP.md` → “Açık sorular”a yaz, sor.
- three.js sahnesini çalıştırman gerekmez; kaynakları okuyarak konum ve süreleri al (`threejs/README.md`).

---

## 10. Açık sorular (Aşama 0'da cevaplanacak)

1. **Eğitim konuları ve ustalık sayısı:** tasarım beş satır çizdi (teknisyen, EMP, analiz, üretim, mutfak); oyun belgesi yedi
   alan sayıyor (+ bakım, ikmal). Core'da kaç alan varsa o kadar satır: 7 ise konu satır aralığı 22 → 16 mm, kişi kartındaki
   ustalık sütunu 7 satır. Kontrol et, bana göster.
2. **DIŞ KAYIT gözetmeni:** Gözetim alanı kalktı. Öneri: Boşta destesinin üst kartı gözetmen olur, adı talep fişine basılır;
   Boşta boşsa talep defteri kilitlenir (“gözetmen yok”). Core'daki gerçek kuralı bul.
3. **Sayaçtaki MALZEME:** dört hammaddenin toplamı; plakanın HAMMADDE olması önerildi (ayrıntı panonun deposunda).
4. **KAYIPLAR zarfı:** kayıp kişinin kartı pano yanındaki bir zarfa geçer; modeli pakette yok. Basit bir manila zarf öner.
5. **Eski yol (`planningBoardEnabled = false`):** Rev2 sunumunda mı kalsın, kaldırılsın mı?
6. **Tek Sahanlık prefab'ı:** menü sahanlığı ile kapının ardındaki koridor aynı yer ama farklı ölçü (R9-not); birleştirelim mi?
7. **Baskın ölçeği:** oyundaki kamera 88 px/kare ise ince parça kuralı ≥ 0,035 birime çıkar.
8. **K5a kamerası:** önerilen +0,14 m ve 52° Unity'de doğrulanmalı.

---

## 11. Sözlük (ek'te yeni, TR → EN)

| TR | EN |
|---|---|
| VARDİYA PLANI · SIĞINAK 07 | SHIFT PLAN · SHELTER 07 |
| TEKNİSYEN · EMP UZMANI · ANALİST | TECHNICIAN · EMP SPECIALIST · ANALYST |
| ÜRETİM EKİBİ · MUTFAK · EĞİTİM | PRODUCTION · KITCHEN · TRAINING |
| BOŞTA / DİNLENME | IDLE / REST |
| SAHA · BASKIN KADROSU | FIELD · RAID TEAM |
| ÜRETİM · İŞ EMRİ | PRODUCTION · WORK ORDER |
| KİŞİ DOSYASI | PERSONAL FILE |
| DEPO · HAMMADDE · DEPO · KULLANILABİLİR KİTLER | STORE · RAW MATERIALS · STORE · AVAILABLE KITS |
| HURDA · 1 eşya → 1 metal | SCRAP · 1 item → 1 metal |
| ◄ MASAYA DÖN | ◄ BACK TO DESK |
| PLAN AÇIK · PLAN KİLİTLİ | PLAN OPEN · PLAN LOCKED |
| CAN · HAR · GÜÇ | HP · MOVE · POWER |
| KİT · ZIRH · YARDIMCI | KIT · ARMOUR · AID |
| YARALI · YATIYOR · KISITLI · İNCELE · KAYIP | INJURED · BEDRIDDEN · RESTRICTED · REVIEW · LOST |
| VARDİYAYI BİTİR · kapağı kaldır · kolu indir · fişi EVRAK'a koy · baskın sürüyor | END SHIFT · lift the cover · pull the lever · file the slip in EVRAK · raid in progress |
| DEVİR FİŞİ · VARDİYA DEVREDİLDİ | HANDOVER · SHIFT HANDED OVER |
| KABUL · TAM ERİŞİM · KISITLI KABUL · RET · DIŞARIDAN YARDIM | ADMIT · FULL ACCESS · RESTRICTED ADMISSION · REJECT · OUTSIDE AID |
| BUĞU CAMI · GİDEN · TELSİZ · BULGU · TALİMAT · ESKİ | BREATH GLASS · OUT · RADIO · FINDING · ORDERS · OLD |

Bütün plaka metinleri token `board.plates.tr/en`, `shiftClose.*`, `admission.decisions` içinde; öteki metinler panoların
notlarında.
