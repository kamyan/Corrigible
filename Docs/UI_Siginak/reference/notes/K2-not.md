# K2 · KOMUTA / HARİTA ODAĞI

_UYGULAMA NOTU · TR + EN_

## KAMERA

Konum (0 · 2,75 · 1,30), hedef (0 · 0,76 · 1,36), dikey FOV 30°. Neredeyse dik bakış: harita 1210 × 684 px, yazı basıklığı yok (eğik pozda üst kenar %21 basılıyordu). Sayaç rafı üstte, anahtar rayı altta kalır; HUD gerekmez.

K1 → K2: 0,45 s, kamera önce 0,15 s yükselir sonra eğilir. Kısayol 1 veya F; bölgeye çift tık da yakınlaştırır.

## KÂĞIT HARİTA (World Space Canvas 1920 × 1080)

Sade çerçevesi aynen: Miller, 6,25 px/derece, λ0 24,7° D, ekvator y 639. Katmanlar: map_base → bölge dolgu meshleri + tarama dokusu → sınırlar (kâğıt altlık 4,2 px + mürekkep 1,6 px) → kıyı → etiketler → işaretler → halka, raptiye, ip → tx_lamp_desk.

İŞGAL Çapraz kırmızı tarama #9A1E14: 45° α .50 + −45° α .32, aralık 11 px. Alt satır “%100”. CEPHE Tek yön tarama (%66,7: 11 px · %33,3: 22 px) + 5 px mürekkep iç kontur + yağlı kalem halkası + kırmızı raptiye + ekip figürü (3D, sürüklenir; bkz. Ekip figürü panosu). ADAY Tarama korunur + kesik grafit iç kontur (12/8) + kesik halka + “%100 · ADAY”. ÖZGÜR Tarama silinir (α .07 hayalet iz + silgi lekesi) + mavi yuvarlak damga “ÖZGÜR · V.011” + üç ✓.

Etiket CP 700 28 harita px → ekranda 17,6 px · alt satır 23 → 14,5 px · işaret kutusu 26 → 16,4 px. 330 px’i aşan ad “·” noktasından iki satıra bölünür. Basılı lejant sol altta (Güney Pasifik); EN’de sütunlar metne göre açılır.

## DURUM VE ETKİLEŞİM

Bölgeye tıkla: fişi açılır. Pasif işgal bölgesinde fiş yalnız bilgi verir: “Cephe açmak için komşusunu özgürleştir.”

Üzerine gel: bölge dolgusuna #F5D646 α .12, etiket fosforlu çipe döner (Sade seçimi).

Baskın emri: “EMİR VERİLDİ” damgası 0,1 s’de iner (kırmızı, −6°), FON tekeri 12 geri sayar, 0,3 s sonra kamera CRT’ye döner (K5b).

Yapılamadı: düğme devre dışı + gerekçe teleksi: “YAPILAMADI · fon yetersiz (12 gerekli / 9 var)”.

## GÖREV FİŞİ (haritanın 2 mm üstünde)

436 × ~650 ekran px · 9 dilim dizin kâğıdı #FBF7EC + tx_paper_grain · kenar 1 px #D2C7AF · gölge (4, 5) α .42 · −1° dönük.

Yerleşim kuralı: fiş, cepheyi, özgür bölgeleri ve adayları örtmeyen yarıya konur (Doğu Avrupa → sağ, Asya üstü). Raptiye fişin sol üstünde; kırmızı ip #A3241A 2,6 px + gölge, cephe raptiyesine bağlanır (UILineGraphic, 3 noktalı eğri).

- **başlık · 104** — OP kodu CP 700 14 · bölge CP 700 28 · ilerleme CP 700 15 · altında 2 px #D2483F.
- **sektör · 3 × 62** — İşaret 26 + no + ad LF 600 18 + tür CP 700 14 + damga. Seçili satır #F5D646 α .32.
- **görev · 120** — Başlık CP 700 13 + açıklama LF 400 18 / 1,38 (EN’de 4 satıra kadar).
- **bedel/ödül · 72** — BEDEL CP 700 20 #9A1E14 · ÖDÜL LF 15, sayılar CP 700 #24592E.
- **ekip · 56** — Figür simgesi 34 × 26 + ad CP 700 15 + üyeler LF 15 + HAZIR / YARALI · n gün damgası.
- **eylem · 72** — Mürekkep düğme 54 px, LF 700 20, “Enter” fişi + bedel. Kapat: Esc.

Damgalar renk + kenar deseni + işaretle ayrılır: GERİ ALINDI mavi düz ✓ · BASKINA HAZIR kırmızı düz ▸ · KİLİTLİ gri kesik kilit. Sektör türü TR’de YAĞMA, EN’de LOOT; “LOOT” TR arayüzde kullanılmaz.

## HAREKET VE SES

- **0,14 s** — Fiş açılır: 12 px yukarıdan kayar, hışırtı; ip 0,12 s’de gerilir.
- **0,09 s** — Fiş kapanır, K1’deki katlı etikete döner.
- **0,10 s** — Damga iner + tok “tak”; mürekkep 0,2 s’de koyulaşır.
- **0,35 s** — FON tekeri döner; modülün altında “−12” fişi 1,5 s.

## EN VARYANTI

Ölçüler aynı; metin %20–30 uzar. Fiş 452 px’e açılır, satır yükseklikleri sabit kalır. Harita etiketleri ve sayaç plakaları dile göre ayrı doku/TMP metni; “MIDDLE EAST · N. AFRICA” iki satır.
