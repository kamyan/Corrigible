# S2 · HARİTA VE GÖREV FİŞİ
*KAMERA AYNI · HARİTA POZU, FOV 30°*

## ESKİ → YENİ

- **etiketler** — DOĞU AVRUPA figür ve şeritle çakışıyordu → ad iki satır, figür adın üstünde, sektör şeridi ve saha işaretleri adın sağında. JAPONYA-KORE denizde, ince çizgiyle adaya bağlı; ÇİN ile arası açıldı. Özgür damgaları adların dışında sabit yerlerde.
- **fişin yeri** — Sağ kenar (Rusya ve Çin kapanıyordu) → sol masa kenarı, x 22–454. Haritadan yalnız Kuzey Amerika'nın batı kıyısı kapanır. Kuzey ya da Güney Amerika seçilince fiş sağda açılır. Ayrıntı: Karar 1.
- **damga** — Kâğıttan taşıyordu → damgalar sabit 138 px sütunda; en uzun damga (BASKINA HAZIR / READY TO RAID) 132 px. Damga yazısı 13 → 14 px.
- **saha** — “Bölge: Dar koridor · İmza: Kilitleyici · Tehdit 1/4” düz satırı → üç satır: işaret + etiket + değer. Aynı işaretler haritada cephe bloğunda ve baskın tahtasında (S9) tekrar eder; oyuncu bir kez öğrenir.
- **tehdit** — Yalnız sayı → dört basamaklı kademe işareti (dolu basamak sayısı = kademe) + “1/4”. Harita işaretler kutusunun başlığında da TEHDİT 1/4.
- **bedel, ödül** — Bedel kırmızıydı → mürekkep (kırmızı yalnız işgal ve tehdit). Ödül tek satır, 15 px → 2 × 2 ızgara, 18 px, sayılar yeşil.
- **ekip** — “T 3/3 · E 3/3 · A 2/2” → harfli ahşap diskler + can; baskın piyonlarıyla aynı harf ve renk.
- **seçili satır** — Sarı zemin + soldaki siyah ok ucu; durum renge kalmaz.

## FİŞİN YERİ · KURAL

*Şema:* Fişin yeri: varsayılan solda; Kuzey ya da Güney Amerika seçiliyken sağda

İplik fişin iğnesinden bölgenin iğnesine bir yayla gider; yay haritadaki adların ve damgaların üstünden geçmeyecek şekilde sarkar (30–40 px).

## NESNELER

- **görev fişi** — Overlay Canvas, 432 × ~728 px, kâğıt #FBF7EC, kenar #D2C7AF, gölge (4, 5) %42, −0,6°. 9 dilim, köşe 24 px. Üst çizgi 2 px #D2483F.
- **saha satırı** — İşaret 30 px (36 birimlik çizim, çizgi 2,9) · etiket CP 700 14, sütun 92 px · değer LF 600 18.
- **sektör satırı** — 30 px simge · ad LF 600 18 · tür CP 700 14 · damga sütunu 138 px (CP 700 14, kenar 2 px, −2…−4°).
- **ekip diskleri** — Ø 22 px, #C9A36B, kenar #5C4826, harf CP 700 14.
- **iğne, iplik** — İğne Ø 18 px #B8241A + parlama; iplik 2,6 px #A3241A, gölge 3 px %28.
- **harita işaretleri** — Dünya dokusu 1920 × 1080: sektör işareti 38 px (ekranda ~24 px), saha işareti 34 px (~22 px), ad 40–48 px (~25–30 px).
- **işaretler kutusu** — Haritanın sağ altında, doku 410 × 170 px (ekranda ~260 × 108 px).

## ETKİLEŞİM VE DURUMLAR

- **sektör satırı** — Normal. Üzerinde: sarı %18 zemin. Seçili: sarı %34 + ok ucu. Kilitli: seçilmez; üzerinde “Önceki sektör alınınca açılır”.
- **BASKIN EMRİ VER** — Normal: mürekkep zemin. Üzerinde: 2 px yükselir, gölge 5 px. Basılı: 2 px iner, gölge 0. Devre dışı: zemin #8A8374, yazı #EFE7D6, sağda neden (“EKİP HAZIR DEĞİL”). Yetersiz kaynak: düğme açık kalır, “−12 fon” üstü çizik, altında “Fon yetersiz · 12 gerekli”; basınca FON sayacı iki kez titrer.
- **saha satırı** — Üzerinde: kâğıt etiket, başlık “İMZA · KİLİTLEYİCİ” + oyundaki açıklama satırı.
- **Esc** — Fişi kapatır; üzerinde çerçeve koyulaşır.

## HAREKET VE SES

- **fiş açılır** — Bölgeye iğne düşer 0,12 s (“tık”) → iplik bölgeden çekilir 0,25 s → fiş soldan kayar 0,22 s, −0,6°'ye oturur; kâğıt hışırtısı.
- **sektör seçimi** — Sarı zemin ve ok ucu 0,1 s; kurşun kalem sesi.
- **emir** — Düğme 0,06 s iner → fiş 0,18 s'de kapanır → CRT'ye dalış ve HAT AÇILIYOR (bugünkü gibi).
- **kapanış** — İplik 0,15 s'de sarılır, fiş 0,18 s'de sola çekilir.
