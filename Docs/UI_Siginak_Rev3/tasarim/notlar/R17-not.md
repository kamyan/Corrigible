# R17 · Uygulama notu
*Pano: `R17-not.dc.html` · görüntü: `../panolar/R17-not.jpg`*

### R17 · KAPI KABULÜ (K8A)
K8 · K8A · K8G · K8R · 3 GÖLGELİ IŞIK
ESKİ → YENİ
- **kabul** — Kahverengi ekran penceresi (üç kâğıt sütunu, sekiz düz düğme, MASAYA DÖN) → kapının sağında fiziksel istasyon: katlanır tezgâh, panel, tezgâh lambası; kanatta buğu camı.
- **kâğıtlar** — Sütun metinleri → R10 kâğıtları nesne olur: talimat şeridi 80 × 210 pirinç yuvada, kimlik / sevk belgesi 148 × 105 evrak gözünden gelir, bulgu fişleri 90 × 60 BULGU ağzından kayar.
- **incelemeler** — Dört düğme → dört araç: cetvel + büyüteç (KARŞILAŞTIR), interkom (GÖRÜŞME), buğu camı (SOĞUK GÖZLEM), talep defteri + GİDEN · TELSİZ ağzı (DIŞ KAYIT).
- **kararlar** — Dört düğme → dört lastik damga: kaldır + bas, geri alınmaz; iz R10 damgasıdır.
- **ekran** — Üst sayaç şeridi ve istasyon çipleri K8, K8A, K8G, K8R’de gizli; dönüş tezgâhtaki MASA tuşu.
- **konseptten** — Evrak gözü kanat yerine duvarda, iç interkomun altında: kanadın iç yüzü dolu (çark, dil çubukları, gözetleme); belge doğrudan tezgâha iner, tenekeler aynı yoldan çıkar. Dış kapağı dış interkomun altında (R1’e ek). Şerit dik olduğu için yakutlar yerine tek kayan satır işaretçisi. R10 fişi “telsizle soruldu” dediği için ağız GİDEN · TELSİZ.

NESNELER
- **Counter** — AdmissionCounter 0,84 × 0,38 (sabit raf 0,085 + kanat) · paintOlive, feltBlotter, brassDark, steelCast · 870 · Top x 0 → 88°, BracketL / R y ±90°.
- **Panel** — AdmissionPanel 0,84 × 0,30 · paintOliveDark, steelCast, paintCream, brassWorn, glassClear · 1588 · Drawer z 0 → −0,22 m, Marker y 0 → −0,17 m, Jewel swap.
- **ColdGlass** — Ø 0,16 kanatta · castIron, coldGlass α.42, fogFrost (doku tx_fog) · 720 · Cover x 0 → 105°, Fog swap.
- **damgalar** — DecisionStampRack 0,25 × 0,07 stampWood + plaka · 632; DecisionStamp ×4 54 × 85 · rubber, Band swap · 352.
- **araçlar** — Magnifier 0,30 cetvel + Ø 70 mercek · 672 · Carriage x 0 → 0,22 m. RequestPad 0,11 × 0,15 · 208 · Sheet x 0 → −35°. RationTin Ø 75 × 110 · 448. FileTag · 56.
- **ışık** — CounterLamp 0,30 m kol, emaye çan · 386 · CounterSpot #FFD9A0, 70°, gölgeli. İç interkom, KeyKey (MASA), kalem yeniden kullanılır.

DURUMLAR
- **başvuru yok** — Kanat katlı, KABUL şablonu odaya; tezgâh lambası sönük (R9a).
- **aday var** — Kanat iner, çekmece belgeyle çıkar, vuruş lambası amber (R17a).
- **inceleme** — Kullanılan araç fişini basar; fiş kıskaca düşer, kullanılmış inceleme bir daha basmaz (R17b).
- **dış kayıt** — Yaprak GİDEN ağzında, karbon kopya, BEKLENİYOR fişi; sonuç sonraki kapanışta (R17c).
- **damgalandı** — İz belgede, damga rafına döner; belge ve fişler kişinin dosyasına; KABUL’de kapı açılır (R17d, R9b).
- **talimat** — Yeni şerit yuvada, eski ESKİ cebinde, eşleşen eski dosyaya İNCELEME FİŞİ (R17e).
- **iz** — Kişi dosyasında inceleme fişi ve iki iz; “robot” hiçbir yerde yazmaz (R17f).

ETKİLEŞİM
- **üzerinde** — Araç 4 mm kalkar, 400 ms sonra kâğıt etiket (“BÜYÜTEÇ · belgeyle karşılaştır”); sürüklenen nesne 25 mm kalkar.
- **devre dışı** — Kullanılmış inceleme (fişi kıskaçta); gözetmen yoksa defter: “gözetmen yok · BOŞTA boş”. Kapasite 10/10 iken KABUL ve KISITLI damgası rafta kilitli (pirinç dil), etiket nedenini yazar.
- **yetersiz** — Erzak 2’den azsa YARDIM damgası kilitli: “erzak yetersiz”.
- **dikkat** — Aday bekliyor: vuruş lambası amber, KAPI tuşu 1 Hz; cevapsız başvuru kapanışta ret, devir fişinde uyarı satırı.
- **okuma** — Kâğıda tık: 0,25 s’de kameraya döner (1 mm = 4 px); ikinci tık yerine koyar.
- **gözetmen** — Açık soru. Öneri: BOŞTA destesinin üst kartı gözetmen olur; adı talebe yazılır, o vardiya başka iş almaz.

HAREKET VE SES
- **istasyon** — Kanat 0,4 s iner (“tank”), lamba 0,08 s; çekmece 0,35 s kayar, belge 0,2 s düşer.
- **araçlar** — Eşleşmede işaretçi 0,2 s kayar, yakut yanar, “tık”; fiş 0,35 s’de 12 cm kayar (yazıcı tıkırtısı). İnterkom cızırtı 0,2 s, döküm 0,03 s/harf. Buğu camı: K8G’ye 0,45 s, kapak 0,2 s, buğu 0,6 s’de belirir, 2,5 s’de söner.
- **karar** — Damga kalkar 0,1 s, basar 0,12 s (“tak”); KABUL: çark 0,5 s, kanat 0,8 s; YARDIM: çekmece tenekelerle dışarı 0,35 s.
- **ışık** — K8A: ampul + masa lambası + tezgâh lambası (3 gölgeli); buğu camı ve vuruş lambası emission.

İNGİLİZCE
ADMISSION · PAPERS · OUT · RADIO · FINDING(S) · ORDERS · OLD · BREATH GLASS · DESK · ADMIT · FULL ACCESS · RESTRICTED ADMISSION · REJECT · OUTSIDE AID. İsimler Türkçe kalır (R17-EN).
