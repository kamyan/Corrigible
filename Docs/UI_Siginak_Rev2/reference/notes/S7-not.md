# S7 · SONUÇ KARTI VE OPERASYON RAPORU
*TAHTA KAMERASI · HARİTA POZU*

## ESKİ → YENİ

- **sonuç kartı** — Damga, başlık ve alt not aynı sözü üç kez söylüyordu (GERİ ÇEKİLDİ ×2 + “Ekip geri çekildi.”) → damga tek ve başlık yerinde: SEKTÖR GERİ ALINDI (mavi) ya da GERİ ÇEKİLDİ (kırmızı), 6 px çift kenar. Altında yer satırı, üç sayı, tek düğme.
- **kartın yeri** — Tahtanın ortasından sağa (x 1290–1790); spot ışığı sonucun nedenini (kasa ya da Teknisyen) açıkta bırakır.
- **rapor yeri** — K1'de sol üst + kararmış boş ekran → harita pozunda solda (x 24–584). Harita aydınlık: sonuç sektör işaretinde ve yüzdede görünür. İplik rapordan ekip figürüne gider, figürün çevresinde kırmızı halka; figür karanlıkta kaybolmaz.
- **tekrar** — Başlık altındaki cümle uyarı bandında yineleniyordu → bant kalktı. Başlık sonucu söyler (SEKTÖR GERİ ALINDI / SEKTÖR İŞGAL ALTINDA), altında tek cümle.
- **zafer anı** — Başarılıda mavi başlık, yeşil BAŞARILI damgası, KAZANÇ ızgarası (32 px yeşil sayılar), sayaçların altında ± fişleri. Başarısızda yalnız sonuçlar ve alarm ölçeği; ödül alanı hiç çıkmaz.
- **kamera** — Rapor için yeni poz yok: var olan harita pozu kullanılır.

## NESNELER

- **sonuç kartı** — Overlay, 500 px, #FBF7EC, kenar #D2C7AF, gölge (5, 6) %50, +0,8°; damga CP 700 34–38, çift kenar 6 px, −3°.
- **rapor kâğıdı** — 560 px, #F1E9D6, üstte 3 px çift çizgi; meta ızgarası 62 px etiket sütunu; KAZANÇ kutusu 1,5 px #463F34, dört göz.
- **iğne, iplik** — İğne Ø17 #2A2622; iplik 2,6 px #A3241A (gölge 3 px %28), adların ve damgaların altından sarkan yay; halka 76 × 54, 2,6 px #B8241A.
- **± fişleri** — S5 ile aynı; yalnız başarılı raporda.

## ETKİLEŞİM VE DURUMLAR

- **RAPORA GEÇ** — Enter. Normal: mürekkep. Üzerinde: 2 px yükselir. Basılı: 2 px iner. Devre dışı ve yetersiz kaynak durumu yok. Kart gelene kadar girdi kilitli (0,6 s).
- **MASAYA DÖN** — Enter; aynı durumlar. Rapor EVRAK tepsisine dosyalanır.
- **satırlar** — Bilgi; tıklanmaz.

## HAREKET VE SES

- **sonuç** — Son hamle → tahta 0,4 s'de kararır, spot 0,3 s'de nedenin üstünde açılır → kart sağdan 0,25 s'de kayar → damga 0,12 s (başarılıda kısa zil, başarısızda boğuk “tak”).
- **rapora geçiş** — Kamera CRT'den çıkıp harita pozuna 0,6 s → rapor 0,4 s'de yukarıdan iner → iplik 0,25 s'de figüre gerilir, halka 0,2 s'de çizilir.
- **başarılıda** — KAZANÇ gözleri sırayla 0,12 s; sayaçlar 0,35 s döner, ± fişleri düşer; haritada sektör işareti 0,2 s'de mavi onaya döner.
- **masaya dön** — Rapor tepsiye 0,25 s, kamera başlangıç pozuna 0,6 s.

## İNGİLİZCE

- **kart** — OPERATION RESULT · TURN 6/8 · SECTOR RETAKEN · WITHDRAWN · Supply crate · taken · Civilian servers broken · Team · 3/3 standing · GO TO OPERATION REPORT
- **rapor** — KYGA · OPERATION REPORT · No. SO-014/5 · single copy · DATE · RESULT · SUCCESS · FAILED · TARGET · DURATION · 6 turns · SECTOR STILL OCCUPIED · “KYGA holds 2/3 sectors of Eastern Europe; one more frees the region.” · GAINS · FUNDS · MATERIALS · RATIONS · SURVIVOR · RESULTS · RETAKEN · OCCUPIED · AI alarm · unchanged · “The team returned. Regroup and try again.” · BACK TO DESK
