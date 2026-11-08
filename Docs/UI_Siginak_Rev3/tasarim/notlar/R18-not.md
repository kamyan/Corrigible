# R18 · Uygulama notu
*Pano: `R18-not.dc.html` · görüntü: `../panolar/R18-not.jpg`*

### R18 · TARİF DEFTERİ VE ÜRETİM
ESKİ → YENİ · NESNELER · DURUMLAR · HAREKET VE SES
ESKİ → YENİ
- **defter** — “TARİF DEFTERİ · AÇMAK İÇİN TIKLA” kutusu → rafta duran A6 ciltli defter; tıklayınca öne gelir ve açılır.
- **açık defter** — düz pencere ve ÖNCEKİ / SONRAKİ / DEFTERİ KAPAT düğmeleri → kâğıt sayfa köşeleri (sekmeler) ve kapak; ekranda düğme yok.
- **sürükleme** — tarif sayfadan kopya kart olarak kalkar, defter kadrajın altına kayar.
- **iş emri** — boş beyaz alan → klipsli form: TARİF kutusu, girdi alanı, üretici ve kapanış satırı.

NESNELER
- **RecipeBook** — 105 × 148 × 18 mm · Cover (y 0…−178°), Leaf (y 0…−176°) · bookCloth, pageFace (tarif_acilim_1..3 TR/EN, 280 × 200 mm doku sol/sağ yarı), pageEdge. Çapalar Hang, Read.
- **CardRecipe** — 112 × 80 mm · tarif kartı; iki vardiyalık tarifte ilerleme kutuları.
- **CardChip** — 56 × 40 mm · hammadde fişi (1 adet).
- **İş emri formu** — PlanningBoard üzerinde 440 × 170 mm kâğıt (paper) + pirinç klips; TMP satırları.

ETKİLEŞİM VE DURUMLAR
- **defter** — kapalı · üzerinde · açılıyor · açık (1/2/3) · sayfa çevriliyor · tarif kopyalandı (aşağıda) · kapanıyor.
- **tarif** — sayfada · tutuldu · iş emri üstünde (hedef) · bırakıldı · geri döndü.
- **iş emri** — boş · tarif var · girdiler eksik (kırmızı not) · tam · başladı · durdu · tamamlandı.
- **yetersiz kaynak** — serbest stok yoksa fiş sürüklenmez: kart 2 mm titrer, stok kartındaki büyük sayı 0,2 s kırmızı yanar.
- **kilitli plan** — defter okunur; tarif ve fiş sürüklenmez.

HAREKET VE SES
- **defter** — öne gelme 0,35 s ease-out, kapak açılma 0,25 s; sayfa çevirme 0,25 s kâğıt sesi; kapanma 0,3 s.
- **kart** — sayfadan kalkış 0,1 s “şırp”; bırakma “tık”; geri dönüş 0,25 s.
- **kapanış** — ürün kartı yeni vardiyada 0,3 s’de depoya kayar.
