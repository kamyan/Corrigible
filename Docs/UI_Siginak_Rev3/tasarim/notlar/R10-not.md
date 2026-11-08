# R10 · Uygulama notu
*Pano: `R10-not.dc.html` · görüntü: `../panolar/R10-not.jpg`*

### R10 · KÂĞITLAR AİLESİ
UYGULAMA NOTU · R10a · R10b · R10c · 2026-11-05 eki
ESKİ → YENİ
- **genel** — Bütün kâğıtlarda künye “KYGA · tür | no. · V.nnn”, başlık altında 2 px kırmızı çizgi, bölüm etiketi + noktalı çizgi, öncülü satır, noktalı göz arası. Kişiler adıyla yazılır; isimler İngilizcede de Türkçe büyür (SELİN, AYLİN).
- **devir fişi** — Düz kâğıt, düzensiz → şalterin yazıcısından çıkan teleks fişi: SAHA (T/E/A + eşya), SIĞINAK (görev · durum), KAPANIŞTA BEKLENEN (erzak, eğitim, üretim, kapı), uyarı kutuları, “fişi EVRAK’a koy”. Yalnız bilinen yazılır.
- **görev fişi** — “EKİP ALFA · T 3/3…” → SAHA KADROSU: üç kişi adıyla, CAN/HAR/GÜÇ ve eşyası. ÖDÜL hammadde türleriyle (+3 metal +2 devre +2 kumaş +1 kimyasal +6 erzak); kurtulan yok. Sektör adları binadan (Devlet arşivi).
- **kadro raporu** — Yedek/plaka satırları kalktı. Kişi başına kit · zırh · yardımcı yuvası, SAĞLAM/YARALI damgası; kalıcı ölüm uyarısı. 432 → 470 px.
- **operasyon raporu** — KAZANÇ altı göz (fon, erzak, dört hammadde); SONUÇLAR’da her kişi adıyla (döndü · yeni yara · +1 deneyim).
- **kişi dosyası** — Yeni. Sol: kimlik, portre, beden, CAN/HAR/GÜÇ, ustalık izi, eşya yuvaları. Sağ: belgeler, karar damgası, erişim, geçmiş, izler, inceleme fişi; İNCELE · ERİŞİM · TAHLİYE.
- **kabul seti** — Yeni: talimat şeridi (sürüm, yeni kural ters zeminli), kimlik/sevk belgesi (sevk no koparılan koçanda), inceleme bulgusu (inceleme başına fiş), dört karar damgası.
- **tarif sayfası** — Yeni: ciltli defter, sayfa başına bir tarif; tarif kartı köşelikte, “İŞ EMRİ” sekmesi sürükleme yönünü gösterir; köşe sekmeleri önceki/sonraki.
- **eski yol** — Vardiya raporu, kapı kâğıdı ve eğitim notu yalnız planningBoardEnabled = false. K4 SIĞINAK ÖZETİ maketle birlikte kalktı.

NESNELER
- KÂĞIT | ÖLÇÜ (1×) | CİNS | EĞİM

- emir belgesi | 760 × ~690 px | beyaz | −0,8°

- görev fişi | 432 × 887 | beyaz | −0,6°

- kadro raporu | 470 × 673 | beyaz | −0,4°

- devir fişi | 100 × 150 mm · 480 × 720 | teleks | 0° elde

- operasyon raporu | 560 × 627 | krem | −0,4°

- son rapor | 1620 × 952 | krem | 0°

- kişi dosyası | 296 × 210 mm · 1184 × 840 | manila | masada

- tarif açılımı | 280 × 200 mm · 1120 × 800 | defter | defter

- kabul talimatı | 80 × 210 mm · 320 × 840 | beyaz | tutucuda

- kimlik / sevk | 148 × 105 mm · 592 × 420 | kart | −0,6°

- inceleme bulgusu | 90 × 60 mm · 360 × 240 | krem | −1°

- karar damgası | ~60–150 × 25–32 mm | mürekkep | −4°

- **cins** — beyaz #FBF7EC / #D2C7AF · krem #F2E9CC / #CDBF97 · teleks #EFE4BC / #C9B98A · kart #F6F0DE · defter #F3EBD3 · manila #D9C08B / #B99D63. Gölge (4, 5) α.45; doku malzemede (tx_paper_grain).
- **çıktı** — papers_new/: her kâğıt TR ve EN, 2× PNG (saydam, eğimsiz, gölgesiz) + HTML parçası + manifest.json (ad, dil, mm, px). 1× = 4 px/mm, fiş 4,8 px/mm (elde 720 px).
- **portre** — work/r3/portraits/p_<id>.svg satır içine alınır (300 × 340 oranı); insan ve robot aynı çizim.

ETKİLEŞİM VE DURUMLAR
- **normal** — Mürekkep düğme gölge 3 · kâğıt düğme #FBF7EC, kenar 1,5 px, gölge 3 · Esc satırı çerçeveli fiş.
- **üzerinde** — 2 px kalkar, gölge 5; kart/fiş tutulabilirse 4 mm kalkar, 400 ms sonra etiket.
- **basılı** — 1 px iner, gölge 0 (#2A2620); keyClickS 0,06.
- **devre dışı** — Gri zemin, neden sağda: KİT EKSİK, YARALI KİŞİ, KAPASİTE DOLU; tıklanmaz.
- **yetersiz kaynak** — Açık kalır: bedel çizik + “fon yetersiz” (#854005); basınca kâğıt 0,1 s titrer, sayaç iki kez titrer, teleks gerekçeyi basar.
- **dikkat** — Geri dönülmez iki adım (TAHLİYE, karar damgası): ilk basış kurar, üstte 3 px kırmızı şerit + “emin misin? tekrar bas”, 4 s; damga ancak ikinci basışta iner.
- **fiş** — Tutulur → yüzeyden kalkar; EVRAK ağzı üzerindeyken ağız 2 px sarı çerçeve; yanlış yere bırakılırsa çıkışa döner. Okumak zamanı ilerletmez.
- **dosya · defter** — Klasör açılır/kapanır; inceleme sekmesi kapalıyken de görünür. Defter köşesine tıklayınca sayfa çevrilir; tarif kartı sürüklenir.

HAREKET VE SES
- **devir fişi** — Kol iner → fiş solda 12 cm kayar 0,35 s (yazıcı tıkırtısı) → elde → EVRAK’a bırakılır (kâğıt sesi) → ekran 0,2 s kararır → teleks “VARDİYA DEVREDİLDİ”.
- **damga** — Kâğıt oturur → 0,1 s → stampS 0,1 (“tak”) → inkDarkenS 0,2; karar damgası ağır, fatalStampS 0,12.
- **kâğıtlar** — Fiş slipOpenS 0,14 / slipCloseS 0,09 · kadro raporu fişin altından slipSlideS 0,22 · rapor reportDropS 0,35, tepsiye reportFileS 0,4 · kâğıt değişimi sheetSwapS 0,15.
- **kabul** — Bulgu fişi büyütece/interkoma/buğu camına/telsize bağlı belirir 0,25 s; dış kayıt sonucu kapanışta teleksle. Talimat değişince eski şerit tutucudan kayar, yenisi 0,4 s.

İNGİLİZCE
- **künye** — KYGA · ORDER · OPERATION FORM · SQUAD REPORT · SHELTER 07 (slip) · OPERATION REPORT · FINAL CAMPAIGN REPORT · PERSONNEL FILE · ADMISSION AND ACCESS · RECIPE BOOK · ADMISSION RULES · IDENTITY / TRANSFER · CHECK FINDING. Vardiya S.nnn; ondalık nokta.
- **terim** — erzak rations · hammadde metal / circuit / cloth / chemical · CAN HP · HAR MOVE · GÜÇ STR · kit / armour / aid · fişi EVRAK’a koy → file in EVRAK.
- **damga** — KABUL · TAM ERİŞİM → ADMIT · FULL ACCESS · KISITLI KABUL · 3 VARDİYA → RESTRICTED ADMISSION · 3 SHIFTS · REDDET → REJECT · DIŞARIDAN YARDIM · 2 ERZAK → OUTSIDE AID · 2 RATIONS · SON KİŞİ → LAST PERSON.
- **uzunluk** — Punto küçülmez. Fiş boyu sabit (150 mm): V.031’de TR ve EN aynı 47 px boş pay. Dosya, talimat ve bulgu satır sarar; isimler çevrilmez.
