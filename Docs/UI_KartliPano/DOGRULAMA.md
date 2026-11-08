# Birbirini izleyen kart hareketleri ve alttan toplama · güncel doğrulama

2026-11-05. Sahne gecikmesi 70 ms, kart hareketi 320 ms'dir. Açılış görünen üst karttan, kapanış destenin en altındaki
karttan başlar. Her kart önceki bitmeden harekete geçer; açılma/kapanma aynı zaman çizelgesini kullanır. Yön değişiminde
anlık konum ve ölçek alınır; yeni Boşta bırakışının kendi noktasından yerleşmesi korunur.

Gerçek Play'de üç kartın üstten açılma başlangıçları yaklaşık 9/79/160 ms, kapanma başlangıçları üstten alta
159/79/21 ms ölçüldü. Kare hızı ve küçük hareketi algılama eşiği bu gözlem sürelerini etkiler. İkinci kart başladığında
öncekinin hedefe ulaşmadığı iki yönde de doğrulandı. Yarım açılmada kapanma, yarım kapanmada açılma konumu sıçratmadı.
Animasyon komut geçmişini değiştirmedi; iki hareket görüntüsü açılıp incelendi.

Boş sahnede tam EditMode koşusu 51,68 saniyede **546/546 geçti**. Yeni hata ve çeviri eksiği yok. Oyuncunun beş
kayıt dosyası SHA-256 ile korundu. Editor Play kapalı, DeskGame açık bırakıldı.

[Açılma akışı](screens/bosta_acilma_akis_TR.png) · [Alttan toplanma](screens/bosta_toplanma_akis_TR.png)

---
Aşağıdaki bölümler önceki revizyonların tarihsel doğrulamasıdır.

# Boşta bırakış noktası ve tekrar tıklamayla toplama · güncel doğrulama

2026-11-05. Gerçek Play'de görevdeki kişi kartı Boşta alanına bırakıldı. Bırakış sonundaki koordinat aynen korundu;
sonraki 14 karede kart eski görev konumuna dönmedi ve desteye mesafesi azaldı. Kaynak/hedef gerçek raycast ile bulundu.

Kapalı desteye tıklama eli açtı. Açık kişi kartına, görevdeki ayrı bir kişiye, boş pano yüzeyine ve boş görev alanına
yeniden tıklama kartları topladı. Tıklamalar komut geçmişini değiştirmedi. Görevdeki kişi için ayrıca gerçek tıklama
hedefinin o kart olduğu doğrulandı. Toplanmış pano görüntüsü açılıp incelendi.

Başarılı bırakış ve ilk deste yerleşimini kapsayan regresyon, eski göreve geri dönüşü kilitler. Yüzey tıklama testi
sürükleme/sağ tık/kilitli yüzeyin tıklama üretmemesini doğrular. Core, RNG ve kayıt biçimi14 değişmedi.

Boş sahnede tam EditMode koşusu 51,18 saniyede **546/546 geçti**. Çeviri eksiği yok; yeni görünür metin yok.
Oyuncunun beş kayıt dosyası SHA-256 ile doğrulandı; Editor Play kapalı, DeskGame açık bırakıldı.

[Toplanmış pano](screens/bosta_toplandi_TR.png)

---
Aşağıdaki bölümler önceki revizyonların tarihsel doğrulamasıdır.

# Boşta üst kartı ve panelsiz el · güncel doğrulama

2026-11-05. Kapalı Boşta destesindeki görünen üst kişi doğrudan sürüklenir; diğer kartlar destede kalır ve açılma
başlamaz. Yalnız tıklama görünen üst karttan başlayarak kartları sola sıralar. Siyah el paneli, ek başlık ve Topla
düğmesi sahneden kaldırılmıştır. Açık elden kişi sürüklenince kalan kartlar desteye döner.

Gerçek Play'de kaynak ve hedef GraphicRaycaster ile doğrulandı. Üst kartın doğrudan alınması, alttakinin destede
kalması, tutma sırasında komut oluşmaması, Eğitim alanına atama ve sürükleme bırakılışının tıklama sayılmaması geçti.
Saf tıklamada ilk kart hareket ederken ikincinin gecikmesini beklediği ölçüldü. Açık elden sürükleme de geçti.
Kapalı sürükleme ve panelsiz açık el görüntüleri açılıp incelendi.

Boş sahnede tam EditMode koşusu 50,58 saniyede **544/544 geçti**; başarısız/atlanan yok. Ek regresyon sürükleme
bırakılışı ile yeni saf tıklamayı ayırır; sahne testi eski el panelinin yokluğunu kilitler. Çeviri eksiksiz; punto
taraması 923 metin, 0 ihlal, 90 gerekçeli istisna. Core/RNG/kayıt14 değişmedi. Oyuncunun en son beş kayıt dosyası
SHA-256 ile korundu; denemeler ayrı depodaydı. Editor Play kapalı, DeskGame açık bırakıldı.

[Doğrudan sürükleme](screens/bosta_surukle_TR.png) · [Panelsiz açık el](screens/bosta_acik_TR.png) · [Kapalı pano](screens/pano_TR.png)

---
Aşağıdaki bölümler önceki revizyonların tarihsel doğrulamasıdır.

# Küçük eğitim listesi ve serbest üretim konumu · güncel doğrulama

2026-11-05. Eğitim alanı 280 yerine 380 genişliktedir; sol kenarı/kart yuvası aynı yerde kalır, yalnız sağa 100 birim
uzar. Beş seçenek 19 puntoyla aynı X üzerinde alt alta dizilir; seçimin kalem halkası korunur.

Gerçek Play denemesinde hammadde üretime (585,375;322,625) konumunda bırakıldı ve aynı noktada kaldı. Üretimdeki kart
(735,125;205,875) konumuna tekrar sürüklendi; miktar, komut geçmişi ve RNG değişmeden orada kaldı. Yeniden çizim,
kayıttan devam ve dil değişimi konumu değiştirmedi. Kaynak kart ve hedef gerçek GraphicRaycaster ile kontrol edildi.
Türkçe/İngilizce son ekranlar açılıp incelendi; Play akışı PASS/DONE ile bitti.

Üretim kuralları/Core değişmedi. Serbest konum `planning_board_layout.json` içinde kayıt tohumu ve `production:<kind>`
anahtarıyla sunum verisidir. Boş sahnedeki son EditMode koşusu 44,74 saniyede **543/543 geçti**. Son otomatik sonuç `test_sonucu.json` içinde; çeviri eksiksiz ve punto taraması
925 metin,0 ihlal,90 gerekçeli istisnadır. Gerçek oyuncunun açık oturumundaki son ilerleme kaydedilerek korunmuştur.

[Güncel pano](screens/pano_TR.png) · [English](screens/pano_EN.png)

---
Aşağıdaki bölümler önceki revizyonların tarihsel doğrulamasıdır.

# Malzemeyle üretim ve sıralı Boşta · güncel doğrulama

2026-11-05. Gerçek Play denemesi en son oyuncu kaydının kopyasında yapıldı. Açık oyuncu oturumu durdurulurken alınan
son kayıt esas alındı; testler `Corrigible.TestStorage` ile ayrı klasördedir.

- Kaynak kartın görünür ve gerçek raycast ile yakalanabilir olduğu her sürükleme öncesinde kontrol edildi.
- Bir görevden kapalı Boşta destesine kişi bırakıldı. Açılmada ilk kart sola hareket ederken ikinci kartın beklediği,
  sonra tüm kartların yan yana ulaştığı doğrulandı.
- Eğitim seçimi ayrı panelden kaldırıldı; alandaki Analiz seçeneğinin kalem halkası ve kişinin gerçek eğitimi eşleşti.
- Üretim içi boş başladı. İki metal ve bir kumaş sürüklenerek ayrıldı; bu sırada stok harcanmadı. Bir metal depoya
  geri alınıp tekrar kondu. Girdilerden zırh tarifi kendiliğinden seçildi.
- Gerçek 3D fiş basımı günü ilerletmedi. EVRAK'a bırakma sonraki vardiyada girdileri harcayıp tek ürün ekledi.
  Ürün, otomatik mod ve tüketim kayıttan tekrar aynı kuruldu.
- Türkçe/İngilizce son pano ve sıralanmış Boşta görüntüleri açılıp incelendi.

Yeni Core regresyonları eksik tarifte koruma, eksik saha kitine öncelik, üretici/depo yeri yokken koruma, depoya iade,
kilitli plan, stok sınırı ve yeni komutların deterministik Replay sonucunu kapsar. Önceki 539 testin yanında dört yeni
vaka vardır; boş sahnedeki son koşu 51,78 saniyede **543/543 geçti**; başarısız/atlanan vaka yok. Sonuç `test_sonucu.json` içindedir. Çeviri doğrulaması eksiksiz; punto taraması 925 metin, 0 ihlal,
90 gerekçeli istisnadır. Kayıt biçimi14 korunur; yeni moda geçiş ayrı komutla yapılır ve eski komut sonuçları değiştirilmez.
Bu doğrulama nihai ekonomi/başarı oranı ölçümü değildir.

[Güncel pano](screens/pano_TR.png) · [English](screens/pano_EN.png) · [Sola açılan Boşta](screens/bosta_acik_TR.png)

---
Aşağıdaki bölümler önceki revizyonların tarihsel doğrulamasıdır.

# Defter ve açılan Boşta eli · son revizyon

2026-11-05. Gerçek Play denemesinde kalıcı tarif şeridinin yokluğu, defter açılması, iki tarifli üç açılımın tamamına
gezinti, sayfa gezinmesinin komut geçmişini değiştirmemesi, sayfadan Üretim'e tarif sürüklenirken defterin kapanması,
bütün Boşta kişilerinin yan yana açılması, kişi alınırken diğerlerinin geri dönmesi ve seçilen kişinin göreve atanması
GraphicRaycaster/EventSystem yoluyla doğrulandı. El yeniden açılıp kapatıldı. Türkçe/İngilizce defter ve Türkçe kart eli
görüntüleri açılıp incelendi. Son Play akışı PASS/DONE ile bitti.

Son tam EditMode koşusu boş sahnede 41,22 saniyede **539/539 geçti**. Son otomatik sonuç `test_sonucu.json` içindedir. Yeni sahne testi kapak/sayfa/düğme/el bağlantılarını, kapalı defterde
tariflerin gizlenmesini ve eski yedek kaydırma alanının kapalı olmasını doğrular. 914 metin, 0 punto ihlali, 90 gerekçeli
istisna; eksik çeviri yok. Gerçek oyuncu dosyaları başlangıç SHA-256 değerleriyle karşılaştırılarak korundu.

[Defter](screens/defter_acik_TR.png) · [English](screens/defter_acik_EN.png) · [Boşta eli](screens/bosta_acik_TR.png)

---
Aşağıdaki bölümler önceki revizyonların doğrulama geçmişidir.

# Kutu oyunu tahtası · son revizyon

2026-11-05. Kişi kartları 210×294 boyutunda dikey koleksiyon kartlarıdır. Ad, stilize portre alanı, can/hareket/güç ve
rol satırı ayrıdır. Altın çerçeveli koyu yeşil tahtada altı görev yuvası (Teknisyen, EMP, Analist, Üretim, Mutfak,
Eğitim) ve kaydırılan Boşta alanı vardır. Bakım, İkmal, Gözetim, Dış temas kaldırıldı. Üretim/depo sağda, tarifler altta.

Gerçek Play denemesinde mevcut kayıttan devam, kaldırılmış görevlerin mevcut `board_assign(rest)` komutuyla
bırakılması, dikey kart oranı, gerçek GraphicRaycaster ile görev yuvasına atama, Boşta'ya taşıma ve tekrar atama,
kayıt tekrarı doğrulandı. Türkçe/İngilizce son görüntüler açılıp incelendi. Portreler şu aşamada stilize yer tutucudur.

Kilitli eski planın komutları değiştirilmez; kaldırılmış görevlilerin kartı Boşta'da gösterilir ve yeni kilitsiz planda
görevi bırakılır. Core replay kuralları/sürüm14 değişmedi. Son tam EditMode koşusu boş sahnede 44,25 saniyede **538/538 geçti**. Son test sonucu `test_sonucu.json` içindedir.
Punto taraması: 907 metin,0 ihlal,90 gerekçeli istisna. Çeviri kontrolünde eksik yok.
Bu kapsam nihai ekonomi dengesi ölçümü değildir; ikmal/bakım görevlerinin kaldırılması ekonomi akışını etkiler.

[Güncel tahta](screens/pano_TR.png) · [İngilizce](screens/pano_EN.png)

---

Aşağıdaki kayıt önceki alanlı panonun doğrulama geçmişidir; sayıları ve görsellerle ilgili ölçümleri kendi revizyonuna aittir.

# Görev alanlı kart panosu · doğrulama

2026-11-05 · Unity 6000.3.12f1 · kayıt biçimi 14

## Güncel davranış

Panoda 11 başlıklı görev alanı vardır: Teknisyen, EMP uzmanı, Analist, Üretim ekibi, Mutfak, Bakım, İkmal,
Eğitim, Gözetim, Dış temas ve Boşta/Dinlenme. Gezici görev etiketleri kaldırıldı. Kişiyi alana bırakmak görevi atar;
dolu alandaki geçerli atama iki kişiyi takas eder. Boşta alanı çok sayıda kişiyi kaydırılan ve maskelenen içerikte tutar.

Üretim iş emri, dört hammadde girdisi, tarif defteri ve Depo ayrı bölümlerdir. Tarif/hammadde kartı kaynak yerine
geri gelir; seçilen iş emri ve ayrılan miktar üretim özetinde gösterilir. Depoda malzeme sayıları ve boştaki ekipman
vardır. Ekipman kişinin üzerine bırakılır, Depo'ya bırakılarak çıkarılır. Kartların yerleri Core görev/sahipliğinden
ve sahnedeki alan/ofsetlerden hesaplanır; eski serbest yerleşim yan dosyası bunları değiştirmez.

## Gerçek Play denemesi

Oyuncunun en son biçim 14 kaydı, masa notları ve eski yerleşim dosyası ayrı `Corrigible.TestStorage` klasörüne
kopyalandı. Kayıttaki 6. vardiyadan devam edildi. Gerçek GraphicRaycaster sonuçları ve EventSystem'in
BeginDrag/Drag/Drop/EndDrag işleyicileri kullanıldı; bırakma hedefleri doğrudan komut çağrısıyla taklit edilmedi.

- Kayıttaki kişiler mevcut görev alanlarına yerleşti; eski kesirli pano konumları alanların yerini ezmedi.
- Yanlış arka plana bırakılan kişi yerine döndü; komut geçmişi ve RNG değişmedi.
- Kişi boş EMP alanına atandı. Dolu Teknisyen alanındaki kişinin üstüne bırakılınca iki kişinin görevi takas edildi.
- Kişideki zırh Depo'ya ve depodaki başka bir eşya kartına bırakılarak çıkarıldı; yeniden kişi kartına verilebildi.
- Boşta alanına bırakma görevi kaldırdı. Kişi ve bağlı ekipman kaydırılan/maskelenen Boşta içeriğine geçti.
- Eğitim konusu seçmek tek başına kişiyi görevden ayırmadı. Kişi Eğitim alanına bırakılınca seçilen eğitim başladı;
  kapanışta deneyim arttı ve kişi bir vardiyalık eğitimini bitirerek Boşta'ya döndü.
- Zırh tarifi Üretim'e, metal/kumaş kartları ilgili girdilere bırakıldı. Ayrılan miktarlar görünür oldu; mevcut
  üretici kapanışta zırhı bitirdi ve yeni ekipman depoya geldi.
- Fiziksel kapağı açmak veya tuşa basarak fiş basmak günü ilerletmedi. Gerçek kâğıt mevcut EVRAK kutusuna bırakılınca
  yalnız bir vardiya ilerledi. Panoda vardiya kontrolü yoktur.
- Kayıttan tekrar devam; gün, bitmiş eğitim, görev ve eşya sahipliğini aynı şekilde kurdu.
- Türkçe/İngilizce görüntüler açılıp incelendi. Dil değişince eski durum mesajı yeni dildeki yönergeyle yenilenir.
  Son Play akışında yeni Error/Exception oluşmadı.

## Otomatik kontroller

Son tam EditMode koşusu **538/538 geçti**; başarısız, atlanan veya belirsiz vaka yok. Boş sahnede 50,18 saniye
sürdü. [Makinece okunabilir sonuç](test_sonucu.json).
Yeni regresyonlar yanlış kart türünün hedefe olay iletmemesini ve görev alanı merkezinin eski serbest konumu
geçersiz kılmasını kapsar. Sahne testleri 11 görev kimliğini, ayrı Depo/Üretim/girdi hedeflerini, boş rol etiketi
dizisini, kaydırılan alanları, fiziksel fiş bağlantılarını ve eksik script bulunmamasını denetler.

Çeviri doğrulamasında **0 eksik**; 1920×1080 punto taramasında **911 metin, 0 ihlal, 90 gerekçeli istisna** vardır.
Punto raporu `Docs/UI_Siginak_Rev2/punto_taramasi.txt` içindedir. Bu testler nihai ekonomi veya taktik başarı oranı
ölçümü değildir; önceki bot denge sayıları bu sürümü doğrulamaz.

## Kayıt koruması ve görüntüler

Oyuncunun `player_profile.json`, `liberation_savegame.json`, `savegame.json`, `liberation_desk.json` ve
`planning_board_layout.json` dosyaları başlangıç SHA-256 değerleriyle aynı kaldı. Kayıt biçimi değişmedi;
mevcut biçim 14 kaydıyla devam edilir. Test dili/öğretici tercihleri ve açılış sahnesi geri yüklenir.

- [Güncel Türkçe görev panosu](screens/pano_TR.png) · [İngilizce](screens/pano_EN.png)
- [Fiziksel devir makinesi — önceki revizyondaki görünüm, bu revizyonda akışı tekrar denendi](screens/makine_fis_TR.png)
- [Kapı belgeleri — ilk pano sürümündeki deneme](screens/kabul_TR.png)

Önceki ilk pano sürümü 532/532; serbest pano/3D makine revizyonu 536/536 testle doğrulanmıştı. Bu sayılar
geçmişe aittir. Yeni panonun çok aşamalı öğreticisi, ayrı portre varlıkları ve tarif keşfi bu sürümde yoktur.
