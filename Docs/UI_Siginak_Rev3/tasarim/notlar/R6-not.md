# R6 · Uygulama notu
*Pano: `R6-not.dc.html` · görüntü: `../panolar/R6-not.jpg`*

### R6 · HARİTA, GÖREV FİŞİ, KADRO RAPORU
UYGULAMA NOTU · HARİTA POZU · 2026-11-05 eki
ESKİ → YENİ
- **görev fişi** — “EKİP ALFA · T 3/3 · E 3/3 · A 2/2” → SAHA KADROSU: panodaki Teknisyen, EMP ve Analist alanlarındaki üç kişi adıyla (Deniz, Cem, Zeynep), gerçek CAN/HAR/GÜÇ ve üzerlerindeki eşya. Kişi kartında ne varsa fişte o.
- **ödül** — +7 malzeme + kurtulan → türlerine ayrılmış hammadde: +10 fon · +6 erzak · +3 metal · +2 devre · +2 kumaş · +1 kimyasal (ikmal). Sunucu sektöründe +8 fon · +2 metal · +2 devre · +1 kumaş · +1 kimyasal.
- **sektörler** — Binanın adı alt satırda (DEVLET ARŞİVİ); sektör adları BASKIN-MEKANLARI’ndan: 01 Mikrofilm deposu · 02 Dosya katı · 03 Tarama odası. 2026-11-03 görüntüsünde eski adlar vardı (Tedarik deposu…), oyunda hangisi geçerliyse o.
- **kadro raporu** — Yedek, plaka ve “tam iyileşmemiş” satırları kalktı. Kişi başına kit · zırh · yardımcı yuvası (boş = kesikli), durum damgası; altta kalıcı ölüm uyarısı. 432 → 470 px.
- **yerleşim** — Fiş solda, iple bölgeye bağlı; kadro raporu fişin iç yanında açılır, ip raporun üstünden kavisle geçer. Ayarlar plakası yok (ayarlar dolabı).
- **render** — Arka plan Revizyon 3 harita render’ı (V.014), ek ekonomisiyle yeniden çekildi: ERZAK kesri /60, MALZEME dört hammaddenin toplamı.

NESNELER
- **görev fişi** — Overlay 432 × 887, #FBF7EC, kenar #D2C7AF, −0,6°, gölge (4, 5) α.45. Saha kadrosu satırı: disk 24 · ad LF 600 18 · değer CP 700 15 · eşya LF 400 15.
- **kadro raporu** — Overlay 470 × 673, aynı kâğıt, −0,4°. Satır: disk · ad + meslek · CAN kareleri 10 px · HAR · GÜÇ · üç yuva (14 + 15 px) · SAĞLAM/YARALI S damgası.
- **ip · iğne** — İp 2,6 px #A3241A, gölge (3, 4) α.28; iğne Ø 18 #B8241A. Hedef bölge kenarında ikinci iğne.
- **ödül** — CP 700 18 yeşil sayı + LF 17 ad, üç sütun. Hammadde işaretleri R10b’de (kartlarla ortak).

ETKİLEŞİM VE DURUMLAR
- **BASKIN EMRİ VER** — Normal / üzerinde / basılı (R10b). Basınca önce kadro raporu açılır; emir rapordaki BASKINA ÇIK ile verilir.
- **devre dışı** — Neden düğmenin sağında: KİT EKSİK (saha kişisinde rol kiti yok), YARALI KİŞİ (yaralı ya da yatan kişi sahada), KADRO EKSİK (alan boş), BASKIN YAPILDI.
- **yetersiz fon** — Düğme açık kalır; bedel çizik, “fon yetersiz”; basınca FON sayacı iki kez titrer, teleks gerekçeyi basar.
- **kadro raporu** — BASKINA ÇIK (Enter) emri verir ve planı kilitler · Esc vazgeçer, fişe döner. Rapor açıkken panoda kadro değişirse rapor yenilenir.
- **kişi satırı** — Üzerinde: satır α.18 sarı, 400 ms sonra kişi etiketi (ustalık kademesi). Tıklamak panoda o kişinin kartını gösterir; fişten atama yapılmaz.

HAREKET VE SES
- **açılış** — Bölgeye tıklanır → fiş slipOpenS 0,14 → ip stringS 0,25 ile gerilir; kâğıt kayma sesi.
- **kadro raporu** — BASKIN EMRİ VER → rapor fişin altından sağa slipSlideS 0,22; SAĞLAM damgaları kişi başına stampS 0,1, 0,08 arayla.
- **emir** — BASKINA ÇIK → plan kilitlenir (fişte KİLİTLİ damgası yok, düğmeler devre dışı) → raidOrderDelayS 0,3 → kamera CRT’ye crtDiveS 2,4.
- **vazgeç** — Esc → rapor slipCloseS 0,09 ile fişin altına döner.

İNGİLİZCE
- **başlıklar** — OPERATION FORM · EASTERN EUROPE · STATE ARCHIVE · SECTOR 1/3 · OCCUPIED 66.7% · FIELD SQUAD · TEAM ALPHA · SQUAD REPORT · LAUNCH THE RAID · cancel · back to the slip.
- **kişiler** — İsimler çevrilmez (Deniz, Cem, Zeynep); meslekler çevrilir: Mechanic’s apprentice, Former security guard, Maths teacher.
- **değerler** — HP · MOVE · STR; eşya Technician kit · Armour plate · EMP cell · Analysis kit · Care kit; ödül funds · rations · metal · circuit · cloth · chemical.
- **uzunluk** — EN fiş 908 px (TR 887): alt satır iki satıra sarar. Kadro raporu aynı boy (673); uyarı iki satır.
