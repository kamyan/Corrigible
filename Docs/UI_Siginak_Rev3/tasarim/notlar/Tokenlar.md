# Token 1.3 ve paket
*Pano: `Tokenlar.dc.html` · görüntü: `../panolar/Tokenlar.jpg`*

### TOKEN 1.3 VE PAKET
tokens_siginak.json · raid_tokens.json · 1.3 · 2026-11-05
117 SIĞINAK · _new_1_3
28 SIĞINAK · _changed_1_3
72 BASKIN · _new_1_3
İşaretler. _new_1_3 yeni yollar · _changed_1_3 değişenler, eski değerleri _was_1_3 ’te. Ekle kalkan bir şey {"removed": true, "why": …} olur.
EK 11-05 BÖLÜMLERİ
- **board.*** — ayaklı pano: yerleşim (dünya), iç alan ve cepler mm (layoutMm), lamba, parçalar, deste süreleri, plakalar TR/EN, sınırlar

- **cards.*** — beş kart boyu, 0,6 mm, mıknatıs, kişi kartı yazı ölçüleri, beş durum, teçhizat yığını, etiketler, portre kırpması

- **recipeBook · personFile** — tarif defteri (3 açılım × 2 tarif) ve kişi dosyası (İNCELE / ERİŞİM / TAHLİYE)

- **shiftClose.*** — dört şalter durumu, plaka 2. satırı TR/EN, fiş çıkış noktası ve süreleri, EVRAK hedefi, kararma, teleks

- **admission.*** — talimat sürümleri (4. ve 8. vardiya), dört inceleme aracı, dört damga ve kilit kuralları, gözetmen (açık karar)

- **cameras.K4 · K6 · K8A · K8G · K8R** — K4 panoya bağlı (eski değer _was_1_3’te), şalter ve kabul pozları

- **motion.board · motion.shiftClose** — sürükleme, takas, deste, kilit, defter ve kapanış süreleri (saniye)

- **lightsRecipe** — K4 pano lambası, K6, K8 (ziyaretçisiz), K8A/G/R; M0 lambaları 110° spot

- **materials.* (+20)** — pano, kart, defter, buğu camı, damga, teneke; R14a’da

- **models3d.removed_1_3** — 39 model ve 7 malzeme anahtarı ekle çıktı

"board.deck" : {"fanFromTop": true, "collectFromBottom": true, "perCardMs": 320, "gapMs": 70, "handStepMm": "min(82, 636 / (n − 1))", "takeOne": "bir kart alınınca diğerleri desteye döner"}
"shiftClose.states.waiting" : {"cover": 112, "lever": -35, "lamp": "readyLampOff", "clickable": false, "plate2": {"tr": "fişi EVRAK’a koy", "en": "file the slip in EVRAK"}, "plate2Color": "kırmızı"}
"cards.sizesMm" : {"person": [80, 120], "item": [80, 50], "material": [80, 56], "chip": [56, 40], "recipe": [112, 80], "thicknessMm": 0.6, "magnet": {"diaMm": 10, "hMm": 3.5}}
"layout3d.idleTray" : {"_": "KALDIRILDI (ek 11-05)", "removed": true, "why": "BOŞTA tepsisi ve askılar → panodaki Boşta destesi (board.deck)"}
PAKET · SonSalter_Revizyon3_Ek1105_paket.zip
- **SonSalter_R3/** — 

- **README.md** — ek özeti, işaretler, açık kararlar

- **modeller/** — 198 model · 102 399 üçgen

- **obj/** — OBJ + MTL (396 dosya)

- **manifest/** — 198 JSON: pivot, TMP, çapa, ışık

- **sayfa/** — 198 model sayfası

- **MODELLER.md · modeller.csv · models.json** — tablo

- **threejs/** — r3 (sahne, aileler, giydirme), rev2, r10/papers_new (50 PNG)

- **tokens/** — tokens_siginak.json · raid_tokens.json (1.3)

- **ekranlar/** — R01-R14 (74) · ek_11-05 (57)

PAKETTE OLMAYANLAR
Raf ve maket ailesinin 28 modeli, 9 ziyaretçi silüeti, Pawn_M ve Pawn_S (kaynakta legacy, sahnede gizli: &legacy=1 ). Eski R5, R6, R7, R9 ve R11 render’ları ekranlar klasörüne konmadı.
UNITY’DE SIRA
- **1** — _AxisTest: eksenler doğru mu (Scale 1, Convert Units kapalı).

- **2** — Token 1.3 → URP Lit malzemeler (anahtar başına bir), ışık tarifi.

- **3** — Pano ailesi + K4 kamerası (R15, R16); kart yüzü çalışma anında (8 px/mm).

- **4** — Şalter + fiş ağzı + devir fişi (R19).

- **5** — Kabul tezgâhı (R17), tarif defteri ve iş emri (R18).
