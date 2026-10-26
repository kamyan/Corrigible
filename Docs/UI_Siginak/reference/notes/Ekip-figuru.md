# EKİP FİGÜRÜ · HARİTA ÜSTÜNDE TAŞIMA

_K2–K3 · K6 · pano: `reference/screens/Ekip.jpg` · görüntüler: `reference/renders/TFS.jpg` (durumlar), `TFC.jpg` (taşıma anı)_

Haritadaki düz ekip jetonunun yerini masadaki fiziksel bir figür alır. Figürün yeri **oyun durumundan türetilir**
(yeni simülasyon alanı yok). Sürüklemek yalnızca var olan komutları tetikleyen bir giriş yoludur.

## YERİ · OYUN DURUMUNDAN GELİR

- **hazır** — Cephe bölgesinde, etiketin altındaki ekip noktasında (bölge verisi `teamAnchor`; yoksa etiket + 110 harita px).
- **baskın** — Figür haritadan kalkar (ekip sahada). Masaya dönünce sonuca göre iner.
- **başarısız** — Figür 0,6 s’de S-07’ye (sığınak işareti) kayar; düşen üye kadar piyon yatık (veri yoksa 1), yanında “ALFA · YARALI · n GÜN” etiketi.
- **iyileşince** — Ekibin iyileştiği vardiyanın dizisinde S-07’den cepheye kayar (0,6 s); teleks “ALFA CEPHEDE · <bölge>”.
- **bölge özgür** — Figür özgürleşen bölgede bekler; adaylar kesik halkayla çizilir (K3).
- **açılış (V.001)** — Figür S-07’de; ilk cephe Batı Avrupa olduğundan S-07 ile cephe aynı yerdir.

## TAŞIMA · SÜRÜKLE VE BIRAK

- **al** — Figüre bas, 6 px sürükle (ya da 150 ms basılı tut). Figür 25 mm kalkar, −6° eğilir, altındaki yumuşak gölge ayrılır; geçerli bölgeler parlar (kesik halka koyulaşır), imleç el olur.
- **bırak (K3)** — Bir adaya bırakmak = o adayı seçmek: dolgu sarı, onay fişi açılır (tıklamayla aynı komut). Enter cepheyi açar, figür orada kalır. Esc figürü önceki yerine gönderir (0,25 s).
- **bırak (K2)** — Cephe açıkken figür yalnız kendi cephesine bırakılabilir; bırakınca görev fişi açılır.
- **geçersiz** — Figür 0,25 s’de geri kayar, teleks gerekçeyi basar:
  - `019/06 YAPILAMADI · Kuzey Afrika: özgür bölgeye komşu değil`
  - `014/09 YAPILAMADI · Cephe açık: Doğu Avrupa 1/3`
  - `015/01 YAPILAMADI · Alfa yaralı (1 gün)` — yaralıyken figür alınamaz, yerinde sallanır.
- **tıkla** — Sürükleme zorunlu değil: bölgeye tıklamak ve Enter aynı işi yapar (erişilebilirlik).
- **üzerinde** — 400 ms sonra kâğıt etiket: “EKİP ALFA · HAZIR / Teknisyen · EMP uzmanı · Analist / Sürükle: adaya taşı”.

## NESNE · 3D

- **taban** — Pirinç oval 70 × 48 × 6 mm · #A8844E R .5 M .7 · kenar #5C4826.
- **piyonlar** — 3 ahşap piyon, boy 26 mm (#C9A77A), renkli baş + bant: Teknisyen #3F5F8A · EMP uzmanı #B07A2A · Analist #3E6B4A. Yaralı üye yatık.
- **plaka** — Bakalit disk Ø 21 mm #24180F, pirinç halka #C9A86A, CP 700 “A” #EFDCAE (ekip harfi; TMP ya da küçük doku).
- **boyut** — Ekranda K2’de ~70 px, K1’de ~30 px. Tık hedefi en az 44 px (görünmez kutu collider).
- **seçili / üzerinde** — Haritada figürün altında sarı halka (Quad, #F5D646, gölgesiz).
- **taşırken gölge** — Altta yumuşak oval gölge Quad (siyah α .28); yükseldikçe büyür ve imlecin ters yönüne kayar. Gerçek zamanlı bulanıklık değil.
- **fiş ve rapor simgesi** — `sprites/icon_team_figure.png` (34 × 26): figürün üstten görünüşü.

## HAREKET · SES

- **al** — 0,12 s kalkar (ahşap “tık”); imleci 0,08 s gecikmeyle izler (yay).
- **bırak** — 0,15 s iner, 2 mm seker (“tok”).
- **geri kayma** — 0,25 s ease-out (kâğıtta sürtünme).
- **kendiliğinden taşıma** — 0,6 s, 10 mm yay: S-07 ↔ cephe; yeni cephe tıklayarak açıldıysa onaydan sonra figür yeni cepheye kayar.
