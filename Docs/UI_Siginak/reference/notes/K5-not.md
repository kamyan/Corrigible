# K5 · CRT SAHA YAYINI

_UYGULAMA NOTU · K5a BEKLEME · K5b GEÇİŞ_

## KAMERA

K5a: ekran yerelinde (0 · −0,02 · 0,62) m, hedef ekran merkezinin 3,5 cm altı, FOV 46°. İstasyon değil: CRT’ye tıklayınca ya da sağ tuşla bakarken açılır. Kenar satırı görünür, etkin istasyon yok.

K5b: baskın emrinden 0,3 s sonra kamera 0,5 s’de CRT’ye yaklaşır; son 0,25 s’de camı geçer ve görüntü tam ekran Overlay Canvas’a devredilir.

## NESNELER

- **CRT kasası** — 0,42 × 0,34 × 0,36 + arka huni 0,30 × 0,26 × 0,18 · #4F5446 R .62 · çerçeve #1C1F19.
- **ekran** — 0,30 × 0,225 · emissive; içerik World Space Canvas (TMP) ya da Render Texture · zemin #07100B · fosfor #A6E8B8 · sönük #5E9C73 · R .9 (yansıma ve parlama yok).
- **ışık** — Point Light #7FD9A0, 0,8, menzil 2,4 m; klavyeye ve duvara soğuk yeşil sızıntı. Bloom yok.
- **ekler** — Pirinç plaka “SAHA YAYINI · K-07” · yeşil güç LED’i · 2 düğme · dokulu klavye · sağda tüplü telsiz (kadran #E9A948 emissive, Point Light #FFA850 0,12).
- **katmanlar** — Tarama çizgisi (4 px’te 2 px siyah α .28, tiling) + radyal köşe kararması sprite’ı. İkisi de 9 dilim/tiling, shader gerekmez.

## K5a · BEKLEME İÇERİĞİ

SAHA YAYINI · KANAL 07 · BAĞLANTI BEKLENİYOR (doku px 78) · ALFA · HAZIR · CEPHE: DOĞU AVRUPA / 02 · “Görev fişinden baskın emri verildiğinde yayın açılır.” · imleç ▮ 0,53 s’de yanıp söner. Sağ altta %28 test deseni (daire, artı, K-07).

Ekip yaralıysa satır ters video olur (fosfor zemin, koyu yazı): “ALFA · YARALI · 2 GÜN”. Fosfor tek renk kalır; durum renkle değil biçimle ayrılır.

## K5b · GEÇİŞ KARESİ (Overlay Canvas)

Cam: köşe 74/64 px, çerçeve #1A1D16 → #0F120D. Ortada BAĞLANTI KURULUYOR (CP 700 76, 7 px yatay hayalet α .22), SENKRON 10 kutu + yüzde, hedef satırı (CP 700 32), kamera satırı (CP 700 24 sönük).

Arkada 8×8 taktik tahta ızgarası, baskın kamerasının birebir çerçevesinde (gridOrigin 210, 528 · u/v adımı 93,75 / 53,775 px), α .2. Tahta yerine oturunca çizgiler söner ve gerçek sahne kalır — geçiş “ekranın içine” girer.

Statik: tx_crt_static (256 px döşeme, α .55, her kare rastgele ofset). Yuvarlanan parlak bant 96 px, 1,2 s’de yukarıdan aşağı. “Boşluk · atla” sol altta.

## ZAMAN ÇİZELGESİ

- **0,00** — K2’de “EMİR VERİLDİ” damgası, röle tıkı.
- **0,30** — Kamera CRT’ye döner (0,5 s ease-in); ampul bir kez titrer.
- **0,80** — Ekranda 4 px yatay çizgi açılır, 0,15 s’de tam boy (CRT açılışı).
- **0,95** — Statik + BAĞLANTI KURULUYOR; senkron kutuları 0,1 s arayla dolar, her kutuda telsizden kısa bip.
- **2,00** — Tahta ızgarası parlar, statik %20’ye iner.
- **2,30** — Baskın sahnesi açılır, ızgara 0,2 s’de söner, tok “tunk”.
- **dönüş** — Sonuç raporundan sonra tersi (0,6 s); masa, kamera ve görev fişi önceki hâline döner.

## DURUMLAR · SES

Bekleniyor · Kuruluyor · Yayın açık (baskın) · Kesildi (“BAĞLANTI KESİLDİ · SONUÇ TELEKSTE”) · Devre dışı (ekip yaralı). Ses: CRT’nin ince vızıltısı (çok kısık), statik hışırtı, senkron bipleri, röle ve “tunk”. Hepsi oda yankısıyla.
