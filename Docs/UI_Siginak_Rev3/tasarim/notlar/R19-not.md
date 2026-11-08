# R19 · Uygulama notu
*Pano: `R19-not.dc.html` · görüntü: `../panolar/R19-not.jpg`*

### R19 · VARDİYA KAPANIŞI (K6)
ESKİ → YENİ · NESNELER · DURUMLAR · HAREKET VE SES
ESKİ → YENİ
- **şalter** — 11-05’te denenen kapaklı kutu + iç tuşlu “devir makinesi” → brifteki kapaklı vardiya şalteri olduğu gibi (Cover x 0→112°, Lever x +35→−35°).
- **fiş çıkışı** — masaya düşen düz kâğıt → şalterin sol yanına bağlı fiş ağzı (72 × 156 × 180 mm) ve pirinç fiş dili; çıkış noktası uygulamadaki gibi (−0,30; 0,14; 0,015), fiş 12 cm sağdan kayar.
- **fiş** — tasarlanmamış kâğıt → R10 ailesinden 100 × 150 mm devir fişi (TR/EN), delikli koparma kenarı.
- **plaka** — tek satır → iki satır: VARDİYAYI BİTİR + durum (“kapağı kaldır” / “kolu indir” / “fişi EVRAK’a koy” / neden).

NESNELER
- **ShiftSwitch** — değişmedi · 1542 üçgen. TXT_Plate iki satırlı kullanılır (11,5 mm + 6,4 mm büyük harf).
- **SlipMouth** — yeni · şalterle aynı yerleşim · zeytin döküm kutu, sol yüzde 116 mm ağız (y 0,14), pirinç dudaklar, koparma çubuğu, 150 mm pirinç dil, DEVİR FİŞİ plakası, Lamp (swap, amber). Çapalar SlipStart (−0,18; 0,14; 0,015), SlipExit (−0,30; 0,14; 0,015).
- **HandoverSlip** — yeni · 100 × 150 mm, 0,2 mm, hafif kıvrık (4 × 8 bölüm); doku R10 devir_fisi_*.png (4,8 px/mm @1×).
- **PaperTray** — değişmedi · EVRAK hedefi; hedefken dudağa gölgesiz amber nokta ışık (0,55, menzil 0,35 m).

ETKİLEŞİM VE DURUMLAR
- **hazır** — kapak tıklanır; üzerinde kapak 3 mm kalkar, 400 ms sonra etiket “VARDİYAYI BİTİR · kapağı kaldır”.
- **kapak kalkık** — kol tıklanır (ikinci tık); 4 s geçerse kapak kapanır.
- **fiş bekliyor** — şalter tıklanmaz; fiş tıklanır/sürüklenir. Okumak zamanı ilerletmez. EVRAK dışına bırakılan fiş dile döner.
- **kilitli** — baskın, açılış/emir belgesi, düzeltilmesi gereken plan (eksik girdi, yaralıyı göreve koyma): bant + neden; tık “klak”.
- **yetersiz kaynak** — plan geçersizse fiş basılmaz: kol iner ama ağız “tık” edip boş kalır, teleks gerekçeyi basar, kol 0,4 s’de geri kalkar.
- **dikkat** — fiş ağzının amber lambası fiş EVRAK’a girene kadar yanar.

HAREKET VE SES
- **kapak** — 0,25 s, metal menteşe “tık”; kapanma 0,3 s.
- **kol** — 0,3 s, iki aşamalı “klak-klunk”.
- **fiş** — çıkış 0,35 s (yazıcı “tırrt” 0,3 s + kopma “çıt”); ele 0,25 s; tepsiye 0,2 s “şap”.
- **kararma** — 0,1 s kapanma + 0,1 s açılma; arada Step; kısık “gong”.
- **yeni vardiya** — sayaç tekerleri 0,4 s; teleks 1,2 s baskı; şalter kapağı kendiliğinden kapanır.
- **ışık** — K6: masa lambası (spot, gölgeli) + tavan ampulü + ortam; EVRAK hedef ışığı gölgesiz.
