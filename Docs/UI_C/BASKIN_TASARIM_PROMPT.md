# Baskın · taktik harita — Analist Masası tarzında tasarım brifi

## Eklenecek görseller

Brifle birlikte şunları yükle. İlk üçü görsel dilin kaynağıdır, son ikisi yalnız işlev referansıdır.

1. `reference/screens/C1_main.png`: Analist Masası ana ekranı. Yeni ekran bu dilin bir parçası olacak.
2. `reference/screens/C3_event.png`: olay bildirimi. Karartma ve spot, kırmızı iplik ve raptiye, kalem halkası, kâğıt üstüne kâğıt.
3. `reference/screens/C5_styleguide_full.png`: stil rehberi. Renk, güven katmanı, tipografi, malzemeler (9 dilim), düğme durumları, çipler, harita ve eylem kartları.
4. `drafts/baskin_brifing.png` ve `drafts/baskin_sonuc.png`: baskından önceki “operasyon emri” ve sonraki “operasyon raporu” (masa tarzında, onaylı). Taktik harita bu ikisinin arasına giriyor ve onlarla aynı ailede durmalı.
5. `captures/F9_baskin_mevcut.png`: bugünkü taktik harita. 3D izometrik tahta ve yan panel, oyunun eski (koyu, parlayan) temasıyla. Tahtanın yapısı ve kamerası korunacak; renkleri, malzemeleri ve paneller değişecek.
6. `spec/tokens.json`: renk, yazı, boşluk, kenar yarıçapı, gölge ve bileşen ölçüleri.

## Bağlam

“Son Şalter” bir strateji oyunu. Oyuncu Küresel Yapay Zekâ Güvenlik Ajansı'nın (KYGA) analisti. Kontrolden çıkmış bir yapay zekâyı (YZ) dünya haritasında izliyor ve durdurmaya çalışıyor.

Oyunun arayüzü “Analist Masası”. 1970'ler-80'lerin istihbarat masası:
- **Masa:** koyu ahşap ve yeşil çuha masa, pirinç plakalar.
- **Kâğıtlar:** manila klasörler, dizin kartları, karbon kopyalar, teleks şeridi.
- **İşaretler:** mürekkep damgaları (ONAYLANDI), yağlı kalem halkaları, el yazısı analist notları.
- **Süsler:** kırmızı iplik ve raptiyeler, sarı yapışkan not, bant.

Her bilgi bir kâğıttır ve ne kadar güvenilir olduğu kâğıdın türüyle anlatılır (“güven dili”, stil rehberinde).

Saha ekipleri YZ'nin ele geçirdiği binalara girebilir: buna baskın denir. Akış şöyle:
1. Masa: bölge dosyasında “Saha operasyonu: baskın”.
2. Operasyon emri: onaylanır.
3. **Taktik harita (bu brif):** sıra tabanlı savaş.
4. Operasyon raporu.
5. Masaya dönüş.

## Görev

Taktik harita ekranını 1920×1080'de, Analist Masası diliyle tasarla. Ekrandaki bilgi ve etkileşim aynı kalacak; görünüş masanın dünyasına ait olacak.

**Tahta 3D kalıyor.** Bugünkü izometrik 3D tahtanın yapısı korunacak (Into the Breach tarzı): ortografik kamera, 8×8 kare, duvarlar ve dolaplar hacimli, birimler ve robotlar tahtanın üstünde duran parçalar. Değişecek olan bunların malzemesi, rengi, ışığı ve üstündeki işaretler; tahtanın çevresindeki paneller de masa kâğıtlarına dönüşecek.

**Yön önerisi:** tahta, analistin masasında duran bir operasyon maketi ya da kum masası olsun.
- Maketin tabanı binanın kat planı: ozalit (mavi kopya) ya da aydınger, kare çizgileriyle.
- Duvarlar ve dolaplar mukavva, balsa ya da boyalı tahta bloklar; nesnenin türü renginden ve üstündeki küçük etiketten anlaşılsın.
- Birimler ve robotlar boyalı minyatürler ya da altlıklı karton figürler; ekip ve makineler bir bakışta ayrılsın.
- Robotların niyetleri, haritadaki analist katmanı gibi: kırmızı kalem okları ya da raptiyelere gerilmiş kırmızı iplik, sıra numarası küçük kâğıt bayraklarda.
- Kare vurguları (hareket, hedef, tehlike) asetat üstüne yağlı kalemle ya da tebeşirle çizilmiş gibi.
- Işık bir masa lambası gibi: sıcak, yumuşak gölgeli. Bugünkü parlamalar (glow, bloom) kalkacak.
- Panellerde ekip dizin kartları, hedef paneli bir form, operasyon günlüğü teleks ya da ele geçirilmiş günlük kâğıdı.

Daha iyi bir masa metaforu bulursan gerekçesiyle öner, ama kâğıt, mürekkep, karton ve kalem dünyasından çıkma: neon, hologram, HUD, bilim kurgu görünümü yok.

## Kurallar: ekranın anlatması gerekenler

**Tahta:** 8×8 kare. Kare türleri:
- Zemin.
- Duvar: geçilmez.
- Elektrikli kablo kanalı: üstüne itilen birim 1 hasar alır.

**Nesneler:** her birinin canı var ve kırılabilir.

| Nesne | Can | Not |
|---|---|---|
| Hedef dolap (YZ düğüm dolabı) | 4 | Zırhlı. Yalnız Teknisyen'in sabotajı, dolaba itilen robot ve patlayan soğutma ünitesi hasar verir. |
| Hedef dolap (çekirdek dolabı) | 5 | Bölge YZ'nin çekirdeğiyse; zırhlı. Yedek çekirdek varsa 4. |
| Yedek çekirdek | 2 | YZ'de “Yedekleme” varsa; o da kırılmalı. |
| Sivil sunucu | 1 | Kırılırsa bölgenin ekonomisine ve güvenine işler (yan hasar sayacı). |
| Soğutma ünitesi | 1 | Kırılınca patlar, 4 komşu kareye 1 hasar. |
| Kopya dolabı | 2 | Her tur yanındaki karelerden yeni robot uyandırır; kırılınca durur. |

**Ekip:** oyuncunun 3 birimi var; giriş karelerinden başlarlar.

| Birim | Can | Hareket | Yetenek |
|---|---|---|---|
| Teknisyen (T) | 3 | 3 | **Sabotaj:** bitişik kare. Dolaba 2 hasar; robota 1 hasar ve 1 kare itme. |
| EMP uzmanı (E) | 3 | 3 | **EMP atışı:** düz hatta 2–5 kare uzağa, engellerin üstünden. Hedefe 1 hasar (zırhlı dolaplara işlemez); çevresindeki 4 karedeki robotları dışarı iter. |
| Analist (A) | 2 | 4 | **Kanca:** düz hatta 5 kare. Çarptığı robotu 1 kare kendine çeker; duvara ya da dolaba çarparsa Analist onun yanına atılır. **Tarama:** 3 kare çevresindeki robotların niyeti kesin görünür. |

**Robotlar:** YZ'nin makineleri.

| Robot | Can | Hareket | Saldırı |
|---|---|---|---|
| Güvenlik dronu (D) | 2 | 3 | Düz hatta ateş: ilk engele ya da birime 1 hasar. |
| Bakım robotu (B) | 2 | 2 | Bitişik kareye 2 hasar, ya da hasarlı hedef dolabı 1 onarır. |

**İtme:** itilen birim duvara, dolaba ya da başka bir birime çarparsa 1 hasar alır. Çarptığı dolap ya da birim de 1 hasar alır. Kanala düşen birim de 1 hasar alır.

**Tur yapısı:**
- **Oyuncu turu:** her birim önce hareket edip sonra yeteneğini kullanır (ya da yalnız yetenek). Turdaki hamleler geri alınabilir.
- **Niyetler önceden görünür:** robotların bu tur ne yapacağı (yön, hedef kare, sıra numarası) oyuncu oynamadan görünür.
- **“Turu bitir” sonrası:**
  1. Robotlar niyetlerini sırayla uygular. İtilen robot saldırısını yeni yerinden yapar.
  2. İşaretli karelerden yeni robotlar uyanır. Karede biri duruyorsa uyanma engellenir, ama duran 1 hasar alır.
  3. Robotlar yer değiştirip yeni niyet seçer; yeni uyanma kareleri işaretlenir.

**Sonuç:**
- **Kazanma:** bütün hedef dolaplar kırılır.
- **Kaybetme:** tur sınırı dolar (6 tur; “Hızlanma”da 5) ya da ekibin tamamı düşer.
- **Geri çekilme:** her an mümkün, iki kez onaylanır ve başarısız sayılır.

**YZ'nin yetenekleri binayı değiştirir:**
- **Aldatma / Medya Kontrolü:** bazı niyet okları yalan, yanlış yönü gösterir. Analist 3 kare yaklaşınca kesinleşir.
- **Kendini Kopyalama:** her tur 2 robot uyanır.
- **Yedekleme:** binada yedek çekirdek de var.
- **Hızlanma:** 1 tur eksik.

**Bina planları:** üç tane var: Ana sunucu salonu, Soğutma koridoru, Yedekli veri kasası. Örnek olarak “Ana sunucu salonu”nu çiz:

```
S . ~ . . . U .
. . ~ . # . . S
E . # . K # . .
E . # . . # C .
E . . ~ ~ . . .
. . # . . # d m
. S # . d . Y .
. . . . K . . U
```

Simgeler:
- `.` zemin, `#` duvar, `~` kablo kanalı.
- `C` hedef dolap, `Y` yedek çekirdek (Yedekleme yoksa sivil sunucu), `S` sivil sunucu, `K` soğutma ünitesi, `U` kopya dolabı.
- `E` ekip girişi.
- `d` dron, `m` bakım robotu.

## Ekranda olması gerekenler

- **Başlık:** “SAHA OPERASYONU · BATI AVRUPA · ANA SUNUCU SALONU”.
  - Tur sayacı “Tur 2/6”; son turda uyarı.
  - Evre: senin turun / makineler hareket ediyor.
- **Tahta (3D):** 8×8 kare.
  - Kare durumları karonun üstünde vurgu olarak: hareket edilebilir, yetenek hedefi (çerçeve), seçili birim, robot bu kareyi vuracak (kesin / yalan olabilir), robot uyanacak, üstüne gelinen kare.
  - Nesnelerin canı parçanın üstünde ya da yanında küçük bir etiketle.
  - Birimlerde harf ve can; turunu bitirmiş birim ayrı görünür; düşen birim ayrı görünür.
- **Niyetler:**
  - Robottan hedef kareye ok.
  - Sıra rozeti (1, 2, 3…).
  - Tür: ateş, yakın dövüş ya da onarım.
  - Kesinlik: “yalan olabilir” (?) ya da “tarandı, kesin”.
  - Bir kareyi birden çok robot hedefleyebilir.
- **Önizleme:** yetenek hedefinin üstüne gelince sonucu göster:
  - kim kaç hasar alır;
  - kim nereye itilir (yarı saydam hayalet parça ve itme oku);
  - “zırh” (hasar işlemez);
  - kazandıran hamlede “HEDEF KIRILIR”.
- **Hedef paneli:**
  - kalan hedef dolaplar ve canları (ör. “YZ düğüm dolabı: 3/4 can”);
  - kırılan sivil sunucu sayısı;
  - kalan tur.
- **Ekip:** 3 kart, 1–3 tuşlarıyla seçilir.
  - Her kartta harf, ad, can ve durum (hareket + yetenek / yetenek / tur bitti / düştü).
  - Yetenek adı ve tek satırlık açıklaması.
  - Seçili kart ayrı görünür.
- **Bilgi satırı:** ne yapılabileceği ya da üstüne gelinen şeyin açıklaması. Örnekler:
  - “Teknisyen seçili. Mavi kareye taşı ya da hedefe yeteneğini kullan (Q: yetenek).”
  - “Güvenlik dronu · 2/2 can · sıra 2: doğuya ateş edecek (1 hasar). Bu niyet yalan olabilir; Analisti 3 kare yaklaştır.”
- **Düğmeler:**
  - Turu bitir (Enter);
  - Geri al (Z);
  - Yetenek (Q);
  - Geri çekil: ilk basışta “Emin misin? Tekrar bas”.
- **Operasyon günlüğü:**
  - Tur ayraçları: “— Tur 2: makineler hareket ediyor —”.
  - İyi, kötü ve sistem satırları: “Bakım robotu devre dışı (sabotaj).”, “Sivil sunucu kırıldı: hastane ve banka hizmetleri kesildi.”, “Yeni bir güvenlik dronu uyandı.”.
  - YZ'nin sözleri, masadaki ele geçirilmiş YZ günlüğünün diliyle: “Demek sunucularımın yerini buldun. Bakalım içeri girebilecek misin.”, “Makinelerimin ne yapacağını gösterdiğimi mi sanıyorsun?”.
- **Lejant:** hareket, yetenek hedefi, robot bu kareyi vuracak, yalan olabilir (?), elektrikli kanal, robot uyanacak.
- **Kuralların hatırlatması:** bugün tahtanın üstünde ayrı bir brifing penceresi açılıyor. Operasyon emri masada zaten okunduğu için bunu tahtanın yanında katlanabilir bir “kurallar kartı” ya da analist notu olarak öner. Kısa: oklar ne demek, nasıl seçilir ve taşınır, zırh, sivilleri koru.

## Çizilecek durumlar

Her durum ayrı bir 1920×1080 kare olacak.

- **B1 · Turun başı:** birim seçili değil; bütün niyetler ve uyanma kareleri görünür.
- **B2 · Birim seçili:** Teknisyen seçili; erişilebilir kareler ve seçili kart görünür.
- **B3 · Hedefleme ve önizleme:** EMP hedef karesi, itilecek robotların hayaletleri ve hasar sayıları, bir hedef dolapta “zırh”.
- **B4 · Aldatma:** “yalan olabilir” niyetler ile Analist'in tarama alanında kesinleşmiş niyetler yan yana.
- **B5 · Makineler hareket ediyor:** girdi kilitli; sıradaki niyet vurgulu; günlük akıyor.
- **B6 · Zorlu an:** son tur uyarısı, düşmüş bir birim, yan hasar 1.
- **B7 · Sonuç anı:** iki kare, rapora geçmeden önce: biri “hedef kırıldı”, öteki “süre doldu”.
- **B8 · Geri çekilme onayı.**
- **Parça sayfası (3D):** her parçanın izometrik görünüşü ve basit önden/üstten silueti, malzemesi ve renkleri:
  - zemin karosu, duvar bloğu, kablo kanalı;
  - nesneler: hedef dolap (düğüm ve çekirdek), yedek çekirdek, sivil sunucu, soğutma ünitesi, kopya dolabı, kırılmış dolabın enkazı; sağlam ve hasarlı;
  - birimler: Teknisyen, EMP uzmanı, Analist (sağlam, hasarlı, turunu bitirmiş, düşmüş, seçili);
  - robotlar: güvenlik dronu, bakım robotu.
- **İşaret sayfası (3D):** karo vurguları (hareket, hedef çerçevesi, tehlike kesin / yalan olabilir, onarım, üstüne gelme), niyet okları (ateş, yakın dövüş, onarım; kesin, yalan olabilir, taranmış), sıra bayrağı, uyanma işareti, hayalet parça, hasar ve onarım açılır yazıları (−1, +1, ZIRH, HEDEF KIRILIR).
- **Panel sayfası:** ekip kartı, hedef paneli, bilgi satırı, düğmeler ve tuş fişleri, günlük satır türleri, lejant.

## Stil kuralları

- **Renk:** `tokens.json`'dan. Yeni renk gerekirse (plan kâğıdı, ozalit mavisi gibi) aynı aileden seç ve adlandır.
- **Yazı tipleri:**
  - Courier Prime: başlık, sayı, kod, tuş, etiket.
  - Libre Franklin: gövde ve düğme; damgalarda 800.
  - Kalam: yalnız analist el notları ve “?”, en küçük 15 px.
- **Yazı boyutu:** hiçbir yazı 11 px'ten küçük olmaz. Yazı küçültülmez; taşarsa metni kısalt ya da satır kaydır.
- **Durum:** her durum mürekkep rengi + ikon + kelimeyle anlatılır; renk tek başına anlam taşımaz.
  - Tehdit kırmızı `#9A1E14`.
  - Uyarı turuncu `#854005`.
  - Bilgi mavi `#1A4789`.
  - İyi yeşil `#24592E`.
- **Güven dili:**
  - Kesin bilgi: mavi ONAYLANDI damgası.
  - Rapor: karbon kopya.
  - Şüpheli: turuncu yağlı kalem halkası ve Kalam “?”.
  - Bilinmiyor: kesik kurşun kalem dairesi.
  - Yalan olabilecek niyet bu dille anlatılsın (şüpheli); Analist'in taradığı niyet onaylı görünsün.
- **Gölge ve süs:** gölgeler S1–S5 (düz ofset, bulanıklık yok); eğimler küçük (−5°…+5°); bant, raptiye ve ataş ölçülü.
- **Düğme ve tuş:** birincil düğme mürekkep, ikincil kâğıt, üçüncül bağlantı. Kısayollar masadaki tuş fişiyle (Enter, Z, Q, 1–3).
- **Dil ve sayı:** Türkçe büyük harf kurallarına uy (İ, ı). Sayılar TR biçiminde; eksi işareti “−” (U+2212). İngilizce metinler yaklaşık %20 daha uzun; kartlar ve düğmeler buna yer bıraksın.

## Teknik kısıtlar

Tahta Unity'de 3D (URP) olarak kalıyor; paneller Unity uGUI + TextMeshPro ile kurulacak.

**3D tahta:**
- **Kamera:** ortografik, 35° aşağı eğik, 45° döndürülmüş (kareler elmas görünür; elmasın genişliği yüksekliğinin yaklaşık 1,74 katı). Bir kare 1 birim. 1080 piksel yükseklikte bir kare elması yaklaşık 125×72 px. Kamera açısı ve tahtanın ölçüsü değişmeyecek; tahtanın ekrandaki yeri ve kadrajı önerilebilir.
- **Parçalar:** az poligonlu, basit biçimler (kutu, silindir, eğik yüzeyler). Heykel gibi ayrıntılı modeller yok. Malzemeler düz renk ve birkaç doku (tahta damarı, mukavva, kâğıt, ozalit); her parçanın renklerini hex olarak ver.
- **Işık:** bir ana ışık (masa lambası gibi) ve bir yumuşak dolgu ışığı; gölgeler açık. Bloom ve kendinden parlayan malzemeler yok ya da çok az.
- **İşaretler:** karonun üstünde yarı saydam düz yüzey (vurgu), dört ince şeritten çerçeve, çizgi ve ok başından oluşan ok, yarı saydam hayalet parça, uyanma işareti. Hepsi düz renk ya da basit doku ile kurulabilmeli; renklerini alfa değeriyle ver.
- **Dünya içindeki yazılar:** can, sıra numarası ve “?” bayrağı kameraya dönük TextMeshPro etiketleri. Yazı tipi ve renkleri masanınkiyle aynı; ekranda en az 11 px.
- **Çizim yolu:** Claude Design'da tahtayı bu kamera açısına uyan izometrik bir çizim olarak göster (SVG ya da CSS dönüşümleriyle). Önemli olan parça biçimleri, malzemeler, renkler ve işaretlerin okunması; birebir 3D görüntü beklenmiyor.

**Paneller (uGUI):**
- **Ekran oranları:** referans 1920×1080. 1920×1200, 2560×1440 ve 21:9'da da çalışmalı; hangi panelin hangi kenara yaslandığını ve tahtanın kadrajının nasıl değiştiğini belirt.
- **Çizilebilirlik:** kâğıtlar, kartlar ve düğmeler 9 dilimli sprite ya da düz şekillerle kurulabilmeli. CSS filtresi, backdrop-filter ve karmaşık gradyan veya maske kullanma.

**Hareket:** tasarım durağan. Hareketleri öneri olarak not et: parçanın kayması, saldırı hamlesi, itme, dolabın çökmesi, robotun uyanması, kâğıdın düşmesi. Bugün tahtada bu hareketler var (kayma, atılma, ateş, itme, batma, çökme, açılır yazı); süreleri ve biçimleri önerilebilir.

**Teslim biçimi:** önceki referanslar gibi.
- Her durum için tek bir HTML: sabit 1920×1080, mutlak konumlu, ölçüler okunur.
- Her durumun PNG'si.
- Parça, işaret ve panel sayfaları.
- Yeni tokenlar JSON olarak (3D malzeme renkleri ve işaret renkleri dahil).

## Değişmeyecekler

- Oyun kuralı eklenmeyecek ya da çıkarılmayacak; ekrandaki bilgilerden hiçbiri düşmeyecek.
- Tahta 3D ve izometrik kalacak; kamera açısı ve 8×8 düzen değişmeyecek.
- Masanın onaylı ekranları (C1–C5, operasyon emri, operasyon raporu) değişmeyecek. Yeni ekran onlarla aynı ailede durmalı.
