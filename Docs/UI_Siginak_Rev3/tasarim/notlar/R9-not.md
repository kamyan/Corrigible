# R9 · Uygulama notu
*Pano: `R9-not.dc.html` · görüntü: `../panolar/R9-not.jpg`*

### R9 · KAPI (K8) VE KORİDOR
DESKGAME · K8 · SİLUET YOK · 2–4 GÖLGELİ IŞIK
ESKİ → YENİ
- **kanat** — Düz kutu → perçinli çelik 0,86 × 1,98 × 0,07: iki yüzde 10 mm kabarık çerçeve ve üç bant, çökük paneller; menteşe ekseni kalınlığın ortasında, üç menteşe odadan görünür. Dış yüz menüdekiyle aynı kanat.
- **kasa** — Çıplak açıklık → çelik kasa, 0,25 m beton söve, baklava sac eşik, dayama lamaları.
- **çark · lamba** — Kırmızı halka → döküm göbekli kırmızı el çarkı (tehdit arayüzü, korunur), 1,25 tur. Küre → döküm gövde, 3 yassı lama kafesli vuruş lambası: sönük / amber (kırmızı durum kalktı).
- **gözetleme** — Pirinç halka, mercek, iki kılavuzlu kapak (50 mm). Artık ayrı seçenek değil; yanına buğu camı geldi, ikisi kabulün aracı (R17).
- **interkom** — Kara kutu → zeytin sac kutu, ızgara, bakalit düğme (4 mm), amber LED, pirinç İNTERKOM; GÖRÜŞME aracı (R17).
- **koridor** — Siluetli aydınlık fayans → ıslak beton, birikinti, sırlı fayans + beton, 9 basamak (0,17 / 0,26) kuyuya. 23 ziyaretçi ve dokuz siluet kalktı: koridor hep boş.
- **sahanlık** — Koridor (x 2,45–4,60, z −0,05…1,55; 9 basamak, gen. 0,70; kuyu ~x 5,8) ile menü sahanlığı (x 2,20–4,60, z −1,35…1,85; 10 basamak 0,18 / 0,27, gen. 0,90) aynı yer, farklı ölçü. Öneri: tek “Sahanlık” prefab’ı iki sahnede aynı koordinatta; kapı duvarı 0,25 m (x 2,20–2,45), menü sahanlığı x 2,45’ten başlar.
- **yeni** — Kapının sağında kabul istasyonu: katlanır tezgâh, panel, tezgâh lambası; kanatta buğu camı (R17).

NESNELER
- **DoorFrame** — 1,27 × 2,10 × 0,28 · paintOliveDark #353B30, concreteWall, treadPlate, steelCast · 1160 üçgen.
- **DoorLeaf** — 0,86 × 1,98 × 0,07 · paintOlive #4A5140, steelCast, brass, peepLens, red #9A1E14 · 3100 · Hinge y 0 → 95°, Wheel z 0 → 450°, PeepCover y 0 → 0,05 m.
- **KnockLamp** — 0,13 × 0,25 × 0,10 · castIron, jewelAmberOff ↔ On (#FFB347, 2,4) · 688 · KnockLight #FFA53A 1,2 / 3,5 m, gölgesiz.
- **IntercomBox** — 0,17 × 0,25 × 0,06 + boru · paintOlive, intercomGrille, chrome, bakeliteBlack · 762 · Button z 0 → 4 mm, Led swap.
- **Header · koridor** — HazardHeader 1,10 × 0,12 paintHazard · 114. Corridor 1678, Handrail Ø 42 galv 472, CorridorLamp ×2 (lampGlassCold ↔ roomBulbOff) 592.

DURUMLAR
- **başvuru yok** — Lamba sönük; tezgâh katlı, alt yüzündeki KABUL şablonu odaya bakar; tezgâh ve koridor lambaları sönük.
- **aday geldi** — Üç vuruş, lamba amber; tezgâh iner, lambası yanar, evrak gözünden belge gelir. Masadaki KAPI tuşunun yakutu 1 Hz.
- **açık** — Yalnız KABUL damgasından sonra: kanat koridora açılır, koridor boş kalır, kişinin kartı panoya düşer; kanat geri kapanır.
- **kaldırılanlar** — Siluetler, ölümcül kırmızı ışık, VURULDU / SIZMA kâğıtları (yeni oyunda yok; eski render &legacy=1).

ETKİLEŞİM
- **tıklananlar** — Tezgâh (başvuru varken K8A pozuna 0,45 s), buğu camı, interkom. Kapı, çark ve gözetleme kapağı tıklanmaz: kapı yalnız KABUL ile açılır.
- **üzerinde** — 4 mm kalkış, 400 ms sonra kâğıt etiket: “KABUL TEZGÂHI · başvuru · Aylin”, “BUĞU CAMI · soğuk gözlem”, “İNTERKOM · görüşme”.
- **devre dışı** — Başvuru yokken üçü de kalkmaz; etiket: “KABUL TEZGÂHI · başvuru yok”.
- **dikkat** — Aday bekliyor: lamba amber kalır, KAPI tuşu yanıp söner; cevapsız başvuru kapanışta ret sayılır (devir fişinde uyarı satırı).
- **masaya dön** — K8’de üst sayaç şeridi ve istasyon çipleri gizlenir. Dönüş: kadrajın altındaki masa kenarı (etiket “MASA · masaya dön”), K8A’da tezgâhtaki MASA tuşu; sağ tık ve Esc de döner.

HAREKET VE SES
- **vuruş** — Üç vuruş 0,9 s; lamba her vuruşta 0,08 s parlar. Kamera kendiliğinden dönmez (öneri): oyuncu KAPI tuşuyla gelir.
- **tezgâh** — Konsollar 0,15 s açılır, kanat 0,4 s iner (“tank”), lamba 0,08 s’de yanar; katlanış tersi, 0,35 s.
- **kapı** — Çark 0,5 s, kanat 0,8 s, merdiven ışığı 0,3 s; kapanış 0,6 s, çarkın “klak” sesi.
- **ışık K8** — Ampul + masa lambası gölgeli; başvuruda tezgâh lambası (3.), açıkken koridor (3.). Vuruş lambası gölgesiz nokta.

İNGİLİZCE
INTERCOM · BREATH GLASS · ADMISSION · DESK · SHELTER 07 · KYGA. 07 şablonu iki dilde aynı; tezgâhın alt yüzünde KABUL / ADMISSION.
