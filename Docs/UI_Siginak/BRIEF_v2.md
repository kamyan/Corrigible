# Son Şalter · Tasarım revizyonu 2 (Claude Design brifi)

Kullanım: çizginin altındaki metni Claude Design'a yapıştır. Ek olarak `screens_v2/` klasöründeki 15 görüntüyü,
`reference/screens/K8.jpg` (ilk turun stil sayfası), `spec/tokens_siginak.json` ve
`Assets/Corrigible/Desk/Data/Sade/raid_tokens.json` dosyalarını yükle.

---

## Oyun

"Son Şalter" (İngilizce adı Corrigible), Steam/PC için tek oyunculu bir strateji oyunu, 1920×1080. Yapay zekâ (YZ)
dünyanın bütün ağlarını ele geçirmiş; oyuncu KYGA'nın Batı Avrupa'daki son sığınağı SIĞINAK 07'nin komutanı. Doktrin
duvardaki afişte: "AĞA BAĞLANMA · ANALOG KAL". Komuta odası bir masa oyunu gibi çalışır: kâğıt harita, mekanik
sayaçlar, teleks, sığınağın kesit maketi, tüplü telsiz göstergesi ve vardiyayı bitiren kapaklı şalter. Zaman yalnız
şalter indirilince bir vardiya ilerler. Sahayla tek bağ telsizdir.

**Döngü:** haritada cephe bölgesinin sıradaki sektörüne baskın emri (12 fon) → kamera CRT'ye dalar → 8×8 izometrik
taktik tahta (Teknisyen, EMP uzmanı, Analist; makinelerin hamlesi kırmızı okla önceden görünür, hamleler sıra
numaralı) → sonuç kartı → masaya dönüş, operasyon raporu → sığınakta hazırlık (dört oda: yaşam alanı, telsiz odası,
atölye, revir; personel ve geliştirme) → kapıyı çalan ziyaretçi (gözetleme deliği, interkom, aç/açma, seçenekler) →
şalter → vardiya raporu.

**Harekât:** 12 bölge × 3 sektör (Tedarik deposu, Tahliye geçidi: YAĞMA; Sunucu merkezi: SUNUCU YIKIMI). 36 sektörün
hepsi alınınca zafer. Kaybetme: sivil güven 0 (DAĞILMA), kapıyı silahlı birine (VURULDU) ya da YZ'nin kopyasına
(SIZMA) açmak. YZ alarmı 10'a varınca karşı saldırı son alınan sektörü geri alır. Her bölgenin bir özelliği (Dar
koridor, Kum fırtınası…) ve bir imza makinesi (Kilitleyici, Havan robotu, Kalkan dronu…) vardır; harekât ilerledikçe
tehdit kademesi 1/4'ten 4/4'e çıkar.

**Göstergeler:** VARDİYA, FON (ondalıklı), ERZAK x/depo, MALZEME, ÖZGÜR BÖLGE x/12, GÜVEN x/20 masadaki sayaç rafında;
YZ ALARMI n/10 arka duvardaki panoda.

**His:** Soğuk Savaş kriz sığınağı. Loş; sıcak lamba ışığı, soğuk beton, kâğıt, mürekkep, pirinç, bakalit, tüp.
Yorgun ama inatçı bir umut.

## Bugünkü hâl

İlk tasarım turunda (K1–K8) oda tek bir analog dile çevrildi ve oyuna uygulandı; ekteki K8 stil sayfası o turun
kurallarıdır. O günden bu yana tasarımcı gözü olmadan şunlar eklendi: sağ duvarda sığınağın kapısı ve kapı kâğıdı, YZ
alarmı panosu, GÜVEN sayacı, telsiz göstergesi ile masadaki mikrofon ve telsiz kayıt defteri, baskın tahtası ve
paneli (görev çipi, birim kartları, TELSİZ şeridi, EMRİ İLET), baskın sonuç kartı, operasyon raporu, vardiya raporu.
Bu turun konusu bu yeni parçaları ilk turun diline bağlamak ve bütünün okunurluğunu artırmaktır.

Ekteki görüntüler oyundan alındı (1920×1080, Türkçe; sayılar örnektir, tasarım onları göstermeli, değiştirmemeli):

| Dosya | Ekran |
|---|---|
| `00_emir_belgesi.png` | Açılış: harekât emri ve BAŞLA |
| `01a_masa_ilk_vardiya.png` | Başlangıç pozu, harekâtın ilk vardiyası |
| `01b_masa_vardiya14.png` | Başlangıç pozu, vardiya 014 (bir bölge özgür, cephe Doğu Avrupa) |
| `02_harita_gorev_fisi.png` | Harita pozu ve görev fişi |
| `03_siginak.png` | Sığınak maketi, atölye seçili, oda kartı ve sığınak özeti |
| `04_crt.png` | CRT telsiz göstergesi (DİNLEMEDE), mikrofon, telsiz kayıt defteri |
| `05_kapi_kapali.png`, `05b_kapi_acik.png` | Kapı kâğıdı: kapalı kapı; açık kapı ve ziyaretçinin silueti |
| `06_vardiya_raporu.png` | Vardiya raporu (teleks kâğıdı) ve sayaçların altındaki ± fişleri |
| `06b_crt_gecis.png` | Baskın emri: CRT'ye dalış, HAT AÇILIYOR |
| `08_baskin_secim.png` | Baskın, 1. tur: Teknisyen seçili, yetenek kartı, makinelerin niyetleri |
| `09_baskin_makineler.png` | Makine evresi: TELSİZ şeridi ("Yeni bir robot uyandı: H2."), oklar |
| `10_baskin_tur2.png` | 2. tur planlama |
| `11_baskin_sonuc.png` | Baskın sonuç kartı (GERİ ÇEKİLDİ) |
| `12_operasyon_raporu.png` | Masaya dönüş: operasyon raporu |

## Korunacaklar (iyi çalışıyor)

- Sayaç rafı, pirinç anahtar rayı ve kapaklı şalter; Esc AYARLAR plakası; kısayolların tuş fişleriyle görünmesi.
- Kâğıt dili: görev fişi, kapı kâğıdı, vardiya raporu, emir belgesi; durumların damgayla verilmesi.
- Haritanın tarama dili (işgal, cephe, aday, özgür) ve ekip figürü.
- Baskın tahtasının ozalit pafta + kontrplak maket dili, harfli ahşap uzman piyonları (T, E, A), kenardaki kare adları
  (A–H, 1–8) ve kırmızı niyet okları. Telsiz kurgusu bugünkü ölçüsünde kalsın; daha fazla vurgu istenmiyor.

## Görülen sorunlar

**Başlangıç pozu (01a, 01b)**
- Haritadaki bölge adları, yüzdeler ve sektör işaretleri bu pozda okunmuyor; rafın oda kartları, soldaki teleks,
  mantar panodaki notlar ve CRT metni de çok küçük. Mantar pano ("Kurtarılan bölgeler") içerik taşımıyor gibi duruyor.
- FON sayacı ondalığı kırmızı çarkla veriyor, virgül yok: 62,0 "0620" okunuyor. Kenar satırında aynı değer "FON 62,0".
- Kapı bu pozdan görünmüyor; biri çalınca kamera kendiliğinden döner. Masada kapının durumunu gösteren küçük bir
  işaret (lamba, fiş) olabilir.
- Odanın harekât boyunca değiştiği pek hissedilmiyor (ilk vardiya ile vardiya 14 neredeyse aynı).

**Harita ve görev fişi (02)**
- Bölge adları sektör işaretleri ve ekip figürüyle çakışıyor (DOĞU AVRUPA; ÇİN ile JAPONYA-KORE).
- Fişteki durum damgası ("BASKINA HAZIR") kâğıdın kenarından taşıyor.
- Görev açıklaması ve "Bölge: Dar koridor · İmza: Kilitleyici · Tehdit 1/4" satırı sıkışık düz metin; bölgenin
  özelliği, imza makinesi ve tehdit kademesi görev seçimini etkileyen bilgiler ama görsel bir karşılıkları yok.
- Fiş açıkken haritanın doğusu (Rusya, Çin) kapanıyor.

**Sığınak (03)**
- Oda kartının düğmeleri dar: "EN ÜST SEVİYE" taşıyor, "− PERS." ve "+ PERS." sıkışık.
- Maketin üst bandı boş ve karanlık. Odaların çıktısı (atölyede dökülen plaka, revirde sarılan yara, telsiz odasının
  alarm etkisi, yaşam alanındaki yedek uzman) maket üzerinde görünmüyor, yalnız metinde.
- Sığınak özeti yoğun; erzağın kaç vardiyaya yettiği gibi asıl soru bir bakışta okunmuyor.

**CRT ve telsiz (04, 06b)**
- Telsiz cihazının (sağdaki ahşap radyo) yüzü boş; telsiz kayıt defteri okunmuyor.
- HAT AÇILIYOR ekranı iyi; masa ile tahta arasındaki tek geçiş anı bu.

**Kapı (05, 05b)**
- Kâğıt iyi çalışıyor. Kapı ve koridor çıplak; ziyaretçi silueti her ziyaretçide aynı. Ölümcül sonuçlarda (VURULDU,
  SIZMA) kâğıda kırmızı damga basılıyor; bunun tasarımı yok.

**Vardiya raporu (06)**
- Okunuyor ama uzun; açıklama satırları küçük ("2 kişi × 0,35" gibi) ve önemli satır (erzak kaç vardiyaya yeter,
  alarm kaça çıktı) ötekilerden ayrılmıyor.

**Baskın (08, 09, 10)**
- Ekranın kenarındaki beyaz kartlar (görev çipi, birim kartları, TELSİZ / KURALLAR / GERİ ÇEKİL, EMRİ İLET) tahtanın
  ozalit ve kontrplak diliyle aynı malzemeden değil; düz web arayüzü gibi duruyor.
- Görev çipi sıkışık: hedefin adı, canı, bölge özelliği ve güven aynı küçük kartta, ikon metnin üstüne biniyor.
- Makine canı ■■ işaretleri çok küçük ve hepsi aynı; makine türleri (dron, bakım robotu, kilitleyici, havan…) tahtada
  zor ayrılıyor, hepsi aynı gri. Niyet sıra numaraları küçük.
- Uzmanın canı ve zırh plakası piyonun üstünde küçük kutucuklar; kim yaralı, kimin plakası var bir bakışta seçilmiyor.
- Tur şeridi (1/8 … 8) küçük; son turların aciliyeti hissedilmiyor.
- Yetenek kartındaki düğme adları belirsiz ("kasayı al", "−1 robot").

**Sonuç kartı ve operasyon raporu (11, 12)**
- Sonuç kartında damga ile başlık aynı sözü tekrarlıyor (GERİ ÇEKİLDİ iki kez; alt not da "Ekip geri çekildi.").
- Operasyon raporunda aynı cümle hem başlığın altında hem uyarı bandında. Rapor sol üstte, ekranın kalanı karartılmış
  ve boş; kırmızı iplik haritadaki küçük ekip figürüne gidiyor ama figür karanlıkta kayboluyor.
- Başarılı baskının raporu (ödüller, kurtarılan sivil, güven) ile başarısızınki aynı kalıpta; zafer anı yok.

**Görüntüsü olmayan ama tasarlanması gereken**
- Son rapor (harekât bitince tam ekran): sonuç damgası (KAZANILDI; KAYBEDİLDİ: Dağılma, Vuruldu, Sızma), dört gösterge
  (süre, baskın sayısı, özgür bölge, sığınak nüfusu), işgal oranı şerit grafiği (kısa harekâtta sola sıkışıyor),
  kırılma anları listesi, Yeniden başla.
- Eğitim notu: analistin sağ altta çıkan el notu (12 adım; baskında "BASKIN · ADIM n/30").
- Karşı saldırı anı: alarm eşiğe varınca teleks "YZ KARŞI SALDIRISI" basar, bir sektör yeniden işgal altına girer.

## Görev

Bilgi ve etkileşim aynı kalır; ilk turun dilini koruyarak yeni parçaları ona bağla, okunurluğu ve hiyerarşiyi düzelt.
Yukarıdaki sorunlardan daha önemlisini görürsen gerekçesiyle öner.

Teslim edilecek ekranlar (1920×1080, Türkçe; S2, S3 ve S6'nın İngilizce varyantı da):

| Kod | Ekran | Odak |
|---|---|---|
| S1 | Başlangıç pozu | Okunurluk (harita, raf, teleks, CRT); FON'un ondalığı; kapı işareti; harekâtla değişen oda (ilk vardiya ve geç harekât yan yana) |
| S2 | Harita ve görev fişi | Etiket çakışmaları; bölge özelliği, imza makinesi ve tehdit kademesinin görsel karşılığı; damga taşması |
| S3 | Sığınak | Oda kartı ve düğmeleri; odaların çıktısının makette görünmesi; özetin tek bakışta okunması |
| S4 | Kapı | Kapalı kapı, açık kapı, ölümcül sonuç (VURULDU damgası); ziyaretçiye göre siluet çeşitlemesi önerisi |
| S5 | Vardiya raporu | Önemli satırların öne çıkması; karşı saldırı varyantı |
| S6 | Baskın | Planlama (uzman seçili), makine evresi (TELSİZ şeridi), son tur; kenar panelleri tahtanın malzemesiyle |
| S7 | Sonuç kartı ve operasyon raporu | Başarılı ve başarısız iki varyant; tekrarların kaldırılması; ekranın kalanının kullanımı |
| S8 | Son rapor | KAZANILDI ve KAYBEDİLDİ (Dağılma) varyantları |
| S9 | Stil sayfası eki | Makine türü işaretleri, can/zırh/plaka işaretleri, niyet okları ve sıra numaraları, tur şeridi, sonuç damgaları, kenar paneli malzemesi |

## Kısıtlar

- Motor: Unity 6 URP, 3D sahne + uGUI + TextMeshPro (World Space ve Overlay Canvas). UI Toolkit yok. Özel shader,
  gerçek zamanlı bulanıklık ve post-processing'e dayanan efekt yok; parlama emissive ile. 2D öğeler 9 dilimli
  sprite ile çizilebilir olmalı; 3D nesneler basit geometri (kutu, silindir, düşük poligon) ve düz ya da az dokulu
  malzemeyle.
- Oyun kuralları, sayılar ve etkileşimler değişmez. Kamera pozları korunur (başlangıç, harita, sığınak, CRT, kapı;
  baskında izometrik tahta kamerası). Yerleşimi değiştirmek istersen kamera pozunu da öner.
- Diller: her metin Türkçe ve İngilizce. İngilizce %30'a kadar uzun olabilir; punto küçülmez, satır sarar. Türkçe
  karakterler (Ğ Ü Ş İ Ö Ç ğ ü ş ı ö ç) her fontta doğru görünmeli.
- Yazı aileleri: Courier Prime (daktilo: başlık, sayı, etiket, damga, teleks, CRT), Libre Franklin (okuma, düğme),
  Kalam (yalnız el notu). Yeni font önerirsen gerekçe ver.
- Okunurluk: 1920×1080'de gövde en az 18 px, ikincil en az 14 px görünür boyutta; dünya metinleri en uzak pozda
  ölçülür. Hiçbir durum yalnız renkle verilmez; kelime, damga, işaret ya da desen de taşır.
- Renk anlam taşır: kırmızı işgal ve tehdit, mavi geri alınmış, yeşil iyi; sarı seçili.
- Neon, hologram, HUD çerçevesi, bilim kurgu görünümü yok.

## İstenen çıktı

- S1–S8 ekranları 1920×1080, S9 serbest boyut.
- Her ekranın yanında kısa uygulama notu: bugünkü görüntüden ne değişti (eski → yeni listesi), nesne listesi (ad,
  yaklaşık boyut, malzeme), etkileşimli öğeler ve durumları (normal, üzerinde, basılı, devre dışı, yetersiz kaynak),
  hareket ve ses tarifi (süre ve sıra).
- Token'lar ekteki `tokens_siginak.json` ve `raid_tokens.json` biçiminde; yeni ya da değişen değerleri ayrıca işaretle.
- Kararsız kaldığın en fazla 3 nokta için iki seçenekli küçük karşılaştırma.
