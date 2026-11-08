# R4 · Uygulama notu
*Pano: `R4-not.dc.html` · görüntü: `../panolar/R4-not.jpg`*

### R4 · İÇERİ GİRME ANI
TOPLAM ≈ 5,1 S · GERİ DÖNÜLMEZ · GİRDİ KİLİTLİ
ESKİ → YENİ
- **bugün** — Kart saate gider, kapı açılır, kamera eşiğe gider; an tasarlanmadı, hızları ayrı ayrı.
- **yeni** — Tek bir sekans, dört vuruş: kart → zil → kapı → eşik. Her vuruşun sesi bir sonrakini çağırır; kamera yalnız iki kez hareket eder (M2 → M0, M0 → M3). Kararma eşiğin içinde olur: oyuncu içeriyi görmez, sıcaklığı görür.
- **DeskGame** — Kısa dönüş aynı kalır (siyah → ampul 0,6 s → K1); yeni: ampul tek başına yanarken masa lambası 0,15 s sonra gelir, teleks “V.014 · VARDİYA” ilk karede değil ampulden sonra basar.
- **yükleme** — LoadSceneAsync(DeskGame) kanat açılmaya başlarken başlar, siyahta etkinleşir; siyah en az 0,2 s, yükleme bitene kadar.
- **yeni harekât** — Aynı sekans; kart yerine boş kart desteden saate gider (deliksiz). DeskGame'de açılış: siyah → jeneratör → ampul → HAREKÂT EMRİ.

SIRA (SANİYE)
- **0,00** — M2 · kart cepten kalkar (0,12).
- **0,12** — Kart yay çizerek saatin boğazına (0,40).
- **0,52** — Yarığa iner (0,14); piston (0,10) “ka-çunk”; delik açılır (0,30); amber lamba.
- **0,96** — Zil (0,35) · aynı anda kamera M2 → M0 (0,45).
- **1,41** — Dış çark 1,25 tur (0,50); kilit dilleri çekilir.
- **1,91** — Kanat −95° sahanlığa (0,80); sıcak ışık 0,30'da yayılır; yükleme başlar.
- **2,71** — Kamera M0 → M3, eşikten geçer (1,60); son 0,30'da kararır.
- **4,31** — Siyah (≥ 0,20).
- **4,51** — DeskGame: ampul iki kırpışmayla (0,60), masa lambası +0,15, K1.

KAMERA VE IŞIK
- **M2 → M0** — Pozlar arası eğri (poseS 0,45), zil sesinin başında çıkar.
- **M3** — (2,55 · 1,55 · 0,75) → içeri (1,2 · 1,4 · 0,75), 56°: kamera kapı eksenine girer, 0,2 m alçalır (eşik basamağı), hızlanıp yavaşlamadan sabit hızla yürür.
- **sıcak ışık** — İçeriden spot (#FFCF86, gölgeli) 0 → 14'e; sahanlığın soğuk lambaları sabit; kararmada ikisi birlikte söner.
- **gölgeli ışık** — Kapı açılırken saat lambasının gölgesi kapanır: en çok 3 (sahanlık, merdiven, içerisi).

SES
- **kart** — Karton sürtmesi, boğazda kısa kağıt sesi; “ka-çunk” (yaylı piston + zımba).
- **zil** — İki vuruşlu mekanik zil (dırr-dırr), içeriden boğuk yankı.
- **kapı** — Çarkın üç “klonk”u, kilit dillerinin sürtmesi, menteşe gıcırtısı; içeriden jeneratör vınlaması yükselir, sahanlığın uğultusu eşikte kesilir.
- **DeskGame** — Siyahta yalnız jeneratör; ampulün “tık”ı; teleks.

ETKİLEŞİM
- **kilit** — Kart kalktığı an bütün girdiler kapanır (Esc dahil); imleç gizlenir, eşikte geri gelmez.
- **atlama** — Yok (5 s, bir kez). İsteğe bağlı: ikinci girişten sonra tık 0,5'e kadar hızlandırır.
- **hata** — Kayıt açılamazsa sekans aynı biter; DeskGame yeni oyunla açılır, teleks “KAYIT AÇILAMADI”.

SEÇENEK (KARAR 10)
“Kapı arkandan kapanır”: DeskGame kamerası açık kapının önünden başlar, K1'e yürür, kapı arkada çarkıyla kapanır (+1,6 s). En güzel geçiş, ama her devamda uzar; ilk adım olarak kısa dönüş kalsın.
