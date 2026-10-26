# K1 · BAŞLANGIÇ POZU

_UYGULAMA NOTU · UNITY 6 URP · uGUI + TMP_

## NESNELER

ad · boyut (m) · malzeme (renk · R pürüz · M metal)

## 4,4 × 2,5

Havalandırma kanalı, 2 boru + vana 4,4 × 0,22 · Ø7/Ø6 cm Galvaniz #70756E · R .5 · M .45 / boya #3F4A3D · vana #9A1E14

Duvar yazısı “KYGA / SIĞINAK 07” 0,84 × 0,15 Şablon boya #D9D2BF, kırık boya maskeli decal (Big Shoulders Stencil)

Doktrin afişi · mantar pano 0,42 × 0,57 · 0,64 × 0,46 Kâğıt #EAD9C0, baskı #9A1E14 · mantar #A27B52; KURTARILAN BÖLGELER listesi + 3 KAYIP ilanı

Tavan ampulü + tel kafes Ø 0,06 Emissive #FFD27A · Point Light #FFCF86, 2700 K, gölge açık (titrer)

Komuta masası 1,84 × 1,10 × 0,76 Üst linolyum #34503F · R .72 · gövde çelik #4A5045 · R .55 · M .25 · ön kenar pirinç şerit #8C6F3E

Sayaç rafı (5 modül) 1,60 × 0,20 × 0,20 · ön yüz 45° Bakalit #24180F · R .38 · plaka pirinç #B8955A · R .36 · M .85 · tekerlek #1C150E, rakam #EFDCAE, ondalık tekeri #801810

Anahtar rayı (5 tuş) 1,27 × 0,15 · 12° eğik Bakalit gövde · tuş başlığı 56 mm, krem rakam · pirinç plaka 150 × 50 mm · amber gösterge lambası

Vardiya şalteri 0,30 × 0,20 × 0,09 Döküm zeytin #4B5340 · R .62 · kapak #B8241A · krom kol · pirinç plaka 270 × 62 mm · yeşil HAZIR lambası

Teleks 0,30 × 0,36 × 0,20 Boyalı çelik #5C6455 · kâğıt rulo Ø9 cm · çıkan şerit 0,20 m, #EFE4BC, delikli kenar

Masa lambası (banker) h 0,40 Pirinç gövde · yeşil cam abajur #1E5236 · R .18 · Spot Light #FFD9A0, 47°, gölge açık

Kâğıt harita 1,20 × 0,675 World Space Canvas 1920 × 1080 (Sade çerçevesi) · ışık almaz, lamba dokusu üstte

Evrak tepsisi · kupa · kalem + talaş · damgalar Tel çelik #33382F, “EVRAK” plakası · diğerleri anlatı nesnesi, tıklanmaz

Sığınak rafı · CRT masası 35° içe dönük Çelik #3D443B, kesit maketi (K4) · CRT kasası #4F5446, ekran emissive, tüplü telsiz #4A3020

## EKRAN KATMANI

Overlay Canvas

## AYARLAR etiketi — sağ 16, üst 14, h 36 · bakalit #0E0906, kenar 1 px #6E5A33, yazı CP 700 15 #C9A86A, tuş fişi “Esc”. Her pozda tek sabit ekran öğesi.

Üzerine gelme etiketi — 9 dilim kâğıt #FBF7EC, 1,5 px mürekkep, gölge (3, 5) α .45 · başlık CP 700 18, satır LF 400 15 · imlecin sol üstünde, ekrandan taşarsa yansır.

## KAMERA

Konum (0 · 1,52 · −0,15) m, dönüş 0° — yatay bakış. Physical Camera: dikey FOV 56,7°, Lens Shift Y −0,324; ufuk 190. pikselde, dikeyler dik kalır, duvar yazıları düz okunur.

Mevcut pozdan farkı: 0,8 m öne, 0,3 m aşağı. Raf ve CRT masası 35° içe döner (U düzeni); iki yan istasyon kadraja girer, masa nesneleri 2 kat büyür.

## ETKİLEŞİM VE DURUMLAR

Kural: pirinç plakası olan her nesne tıklanır; plakasız nesne dekordur. Oyuncu bunu ilk dakikada öğrenir.

Üzerine gelme (400 ms gecikme): nesne 4 mm kalkar, plaka emissive +%15, kâğıt etiket (ad + kısayol). Büyük hedeflerde (kesit maketi, harita) fosforlu sarı çerçeve: #F5D646 5 px + mürekkep 1,5 px.

Hedefler: 1 KOMUTA (harita, F) · 2 SIĞINAK (raf) · 3 GÖREVLER (görev fişi) · H KILAVUZ · Home MASA · Enter VARDİYAYI BİTİR · teleks (son emirler) · evrak tepsisi (brifing, raporlar) · ekip figürü (haritada, sürüklenir) · Esc AYARLAR.

Anahtar: normal · üzerinde (kalkar) · basılı/etkin (6 mm içeride, amber lamba) · devre dışı (baskında; lamba sönük, α .6).

Kenar satırı bu pozda gizli; sayaç rafı kadrajdayken tekrar gerekmez. Ayarlar › Kenar satırı: Otomatik / Her zaman / Kapalı.

## HAREKET VE SES

- **0,45 s** — Poz geçişi (ease-in-out); anahtar “klak” 60 ms, amber lamba 80 ms’de yanar.
- **3–9 s** — Ampul titremesi: 2–3 kırpışma × 60–120 ms, %100 → %60; elektrik cızırtısı.
- **0,35 s** — Sayaç değişimi: tekerlek yuvarlanır + tık; üstte ± fişi 1,5 s kalır.
- **0,4 s** — Teleks yeni satır: tıkırtı, kâğıt 12 mm ilerler.
- **sürekli** — Jeneratör uğultusu −24 dB, havalandırma, uzak su damlası.

## UMUT İŞARETLERİ · ÖNERİ

- **1/12** — Panodaki listeye ilk isim (bugünkü hâl).
- **3/12** — Raftaki tenekede filiz; KAYIP ilanlarından biri “BULUNDU” damgası alır.
- **6/12** — Panoya çocuk çizimi; masada ikinci kupa.
- **9/12** — Telsizden kısık müzik; ampul titremesi seyrekleşir.
- **12/12** — Afişin üstüne el yazısı “YUKARI ÇIKIYORUZ”; havalandırmadan gün ışığı.
