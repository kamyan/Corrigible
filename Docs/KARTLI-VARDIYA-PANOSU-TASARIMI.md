# Kartlı vardiya panosu — tasarım taslağı

**Durum:** Tasarım geçmişi. Temel sistem 2026-11-05'te Unity ve Core'a uygulandı; geçerli maliyet ve kurallar
[OYUN-TASARIMI.md](OYUN-TASARIMI.md)'dedir. Buradaki öneriler uygulanan değerlerin yerine geçmez.
Bu belge mevcut oyunu tarif etmez; sekiz isimli sakin ve sürükle bırak kart panosu yönünü somutlaştırır.
Bu modelde önceki “bir ana faaliyet + bir destek gündemi” sınırının yerini kişi-vardiya kapasitesi alır.

**Ayrıntılı devam:** [Bütünleşik ayrıntılı tasarım](KARTLI-VARDIYA-PANOSU-DETAYLI-TASARIM.md), bu yönün pano
etkileşimlerini, kişi/rol/kit ayrımını, üretim ve devir evrelerini, belge kontrolünü, somut vardiya örneğini ve ilk
kapsamını birlikte açıklar. İki belge de tasarım geçmişini korur; uygulanmış oyun için OYUN-TASARIMI.md okunur.

Son değerlendirme, kullanıcının ayaklı fiziksel pano, kişi savaş değerleri, hammadde kartları ve tarif defteri,
vardiya devir makinesi ve değişen kabul talimatları önerisini de kapsar. Bunların temel işlevleri artık Unity sahnesinde vardır; kapsam sınırları aşağıdaki uygulama notundadır.
Vardiya akışı ve üretim bölümleri aşağıda bu öneriye göre güncellenmiştir; değerler denge kararı değildir.


## Uygulama notu (2026-11-05)

Temel kart, görev/eşya, üretim, seçilebilir eğitim, kişi bazlı baskın ve belgeyle kabul sistemi uygulanmıştır.
İlk kapsamda üç saha rolü Teknisyen/EMP/Analisttir; tarifler baştan açıktır. Tahtada geride kalan kalıcı ölür;
canı sıfırlanan ama geride bırakılmayan kişi sağ kalan ekip varsa otomatik kurtarılır. Ayrı taşıma/kurtarma hamlesi
yoktur. Robotun iki kalıcı izi ve müdahale aralığı vardır; ilk sabotaj kaynak kaybettirir, kişiyi aniden öldürmez.
Portre/yaşam öyküsü varlıkları, tarif keşfi ve yeni çok aşamalı öğretici bu ilk uygulamaya dahil değildir.
Sayılar `Balance.board*`/`admission*` alanlarındadır; eski bot ölçümleri yeni panonun dengesini kanıtlamaz.


## Ana karar

- Oda geliştirmeleri kaldırılır. Soldaki sığınak ünitesinin yerine ayaklı bir planlama panosu gelir; sakinler,
  görevler, üretim ve stok kartlarla yönetilir. Bu tanım sığınak ünitesidir; ayarlar dolabını kendiliğinden kapsamaz.
- Ana sınırlar kullanılabilir kişiler, sağlıkları, görev kitleri ve mevcut kaynaklardır; ayrıca faaliyet kotası yoktur.
- Baskın düzeni üç saha kişisidir: bir zorunlu teknisyen ve iki saha görevlisi. Sekiz kişilik başlangıçta beş kişi sığınakta kalır.
- Bir kişi bir vardiyada yalnız bir görev üstlenir. Baskından dönen kişi o vardiyada üretime geçirilmez.
- Bir işi boş bırakmak geçerli karardır; vardiya önizlemesi bunun tüketim, üretim ve risk karşılığını gösterir.

## Kişi, görev ve ekipman

| Katman | Anlamı | Örnek |
|---|---|---|
| Meslek ve yatkınlık | Kişinin kalıcı geçmişi ve becerileri | Elektronikçi; dikkatli, dayanıklılığı düşük |
| Vardiya görevi | Bu vardiya harcanacak kişi kapasitesi | Baskın teknisyeni, yemek hazırlama, üretim, ikmal |
| Görev kiti ve ekipman | Stoktan kişiye verilen, devredilebilir araçlar | Teknisyen kiti, EMP kiti, silah, koruyucu donanım |

- Saha rolünü temel görev kiti açar; kişinin becerileri etkinliği, güvenilirliği veya kullanılabilir taktikleri etkiler.
- Rastgele kişilik özelliği kritik eylemi habersiz başarısızlığa çevirmez. Katkı önizlemede anlaşılır; taktik tahta
  niyet/sonuç bilgisini korur. Yatkınlık, rolün verdiği temel yeteneği sessizce değiştirmez.
- Zorunlu teknisyen için temel kit başlangıçta vardır. İlk baskının üç rolünü dolduracak kişi ve kit yeterliliği garanti edilir.
- Herkes temel sığınak işlerini yapabilir. Uzmanlık avantaj sağlar; tek uzmanın ölümü bir üretim dalını kapatmaz.
- Görev kitleri kişiye kalıcı bağlanmaz. Kit kaybında uzman gerektirmeyen temel üretim veya ikmal yolu bulunur.
- Kişi kartı yalnız atandığı yerde bulunur; başka listelerde ikinci atanabilir kopyası oluşmaz.
- Ekipman kartı stokta veya tek kişinin üzerinde görünür. Aynı eşya iki kişiye ya da iki işe ayrılamaz.
- İlk denemede görev kitine ek bir koruyucu ve bir yardımcı teçhizat yeri yeterlidir. Ayrı ağırlık, çanta dizme
  veya bütün parçalara dayanıklılık yönetimi eklenmez; üretim ve dağıtımın anlamı önce bu küçük kapsamda denenir.
- Kişi kartında isim/portre, güncel ve azami can, hareket mesafesi, vuruş gücü, meslek ve mevcut görev okunur.
  Eğitim ve ayrıntılı özellikler kart büyütülünce açılır; sekiz kişinin bütün ustalık çizgileri aynı anda gösterilmez.
- Savaş değerleri kişinin tabanı, rol/kit, ustalık, ekipman ve yara etkilerinden hesaplanır; baskına girmeden görünen
  son değerler tahtada da aynıdır. Güç özelliği yalnız ilgili fiziksel saldırıya etki eder; EMP gibi aygıtın sabit
  çıkışı veya itme etkisi için ayrı, açık kural gerekir. Yüksek güç bütün yeteneklere sessizce hasar eklemez.

## Başlangıç ve rastgelelik

- Başlangıç ekibi sekiz kurgu isimli insandan oluşur; ilk plan gizli robot tehdidiyle cezalandırılmaz.
- İsimli kişilerin yatkınlıkları ve sınırlı olumsuz özellikleri oyun tohumuna bağlı olarak çeşitlenir.
- Rastgelelik oynanabilir başlangıç koşullarını bozmaz: gerekli beceri kapsamı, temel kitler ve ilk üç saha rolü korunur.
- Can, hareket ve güç bağımsız sınırsız zarlar değildir; küçük aralıklar ve toplam avantaj bütçesi kullanılır.
  Dayanıklı fakat yavaş veya çevik fakat kırılgan profiller anlamlı seçim yaratır; bütün değerleri üstün kişilerin
  aynı zamanda en iyi üretici olduğu başlangıçlar önlenir. Meslek, görev ve bireysel savaş profili ayrı tutulur.
- Hareket ve hasarda küçük sayılar bile mevcut 8×8 tahtada tur/öldürme eşiklerini değiştirir. Geçerli başlangıç
  ekibinin ilk görevleri çözebilmesi ve değişken değerlerin bütün hamle önizlemelerine yansıması ayrıca sınanır.
- Sonradan gelen kişinin insan/robot kimliği girişte sabitlenir; oyuncunun başarısına veya şüphesine göre sonradan değiştirilmez.
- Yeni kayıt aynı tohum ve aynı komutlarla aynı kişileri, olayları ve sonuçları üretmelidir; uygulama kayıt/replay kurallarına uyar.

## Planlama, kilitleme ve vardiyanın çözümü

1. Oyuncu sakinleri görevlere, stok ekipmanını kişilere yerleştirir; tarifi ve üretim girdilerini, varsa baskın hedefini seçer.
2. Pano beklenen tüketimi, ayrılacak girdileri, ücretleri, tamamlanacak işleri ve riskleri gösterir. Önizleme harcama yapmaz.
3. “Baskına çık” emri panonun tamamını kilitler: kadro, sığınak işleri, sipariş ve ekipman sabitlenir. Baskın bedeli o anda
   bir kez ödenir; üretim girdileri ayrılmış kalır. Sonraki baskın ganimeti o vardiyanın kilitli üretimine yeni sipariş açmaz.
4. Baskın oynanır. Dönen kişi aynı vardiyada üretime veya eğitime geçirilemez; sonuçlar incelenebilir.
5. Devir makinesinin kapağını açmak zamanı ilerletmez. Tuşa basmak hazır planı kontrol edip vardiya devir fişini çıkarır;
   baskın yapılmadıysa plan bu aşamada kilitlenir. Fiş bilinen işleri ve sonuç önizlemesini gösterir, gelecek gizli olayı açmaz.
6. Fişin devir kutusuna konması tek vardiya ilerletme anıdır. Başlatılan siparişlerin girdileri yalnız bir kez tüketilir;
   o vardiya yapılan işler, eğitim, tüketim, gelir ve normal dünya gelişimi bir kez çözülür.
7. Kısa kararma/açılmayla yeni vardiya başlar. Süresi biten işlerin ürün kartları depoya girer, devam edenler kalan süreyi
   gösterir; yeni brifing ve varsa talimat/ziyaret görünür. Yeni ürünü bu vardiyanın sonraki baskınına verebilirsin.

- Son onaydan önceki yer değiştirme ve plan iptali ücretsizdir; onaydan sonra aynı vardiya yeniden planlanmaz.
- Plan önizlemesindeki rezervasyon ayrılacak miktarı hesaplar; kilitli planın hammaddeleri başka işe kullanılamaz.
  Kalıcı stok fiş kutuya konup ilk üretim vardiyası işlendiğinde bir kez düşer; devam eden iş sonraki geçişte yeniden ödemez.
- Üstlenilmeyen, karşılanamayan veya hedef stokuna zaten ulaşmış sipariş üretmez ve girdi harcamaz.
- Üretim, tarifteki çalışma vardiyası sayısı tamamlandığında çıkar. Yeni ekipman üretildiği eski vardiyanın baskınına yetişmez.
- Ölüm veya sabotaj bir işi keserse sonuç kaydı tamamlanamayan işi ve kaybolan girdiyi açıkça gösterir; gizli yeniden ödeme olmaz.
- Aynı fişi tekrar basmak, kutuya eski fiş bırakmak, kararmada çıkıp devam etmek ikinci gün veya ikinci ürün üretmez.
  Fiş, planın ve vardiyanın kimliğine bağlıdır; malzeme/ilerleme ödülü yalnız tek kesin geçişte işlenir.

## Kaynaklar ve üretim

- Erzak mevcut tek stok olarak kalır; ayrıca yiyecek, yemek veya öğün para birimi eklenmez.
- Aşçı erzak yaratmaz; hazırlık yaparak vardiyanın erzak tüketimini azaltır. Temel kuru erzakla aşçısız hayatta kalınabilir.
- Erzak toplama/ikmal ayrı görevdir. Malzemenin ana kaynağı baskın ve sınırlı dış ikmaldir; atölye ham malzeme yaratmaz.
  Oda giderleri kalktığı için malzeme arzı, üretim giderleri ve fon kullanımı birlikte yeniden dengelenir.
- Üretim için kişi atanması, uygun tarif/kit ve yeterli malzeme gerekir. Malzemeye göre erişilebilir tarifler seçilir.
- Varsayılan sipariş tek adettir. Tekrarlanan siparişlerde sınırlı adet veya hedef stok belirtilir; belirsiz sonsuz üretim yoktur.
- Üretici, sipariş ve ekipman sonraki planlamaya taşınır; oyuncu yalnız değişiklikleri yapar. Her vardiya kartları yeniden dizmek gerekmez.
- Hedef stoğu dolan veya girdisi bulunmayan görev görünür biçimde durur; kişi habersizce başka işte çalıştırılmaz.

## Tarif defteri, hammaddeler ve devam eden üretim

- İlk denemede az sayıda hammadde türü yeterlidir: örneğin metal, devre, kumaş ve kimyasal. Bunlar tasarım önerisidir;
  mevcut tek malzeme stoğu türlere ayrılacaksa baskın ödülleri, ikmal ve denge birlikte değişir. Erzak ayrıca tek stok kalır.
- Aynı tür girdiler sayılı kart yığınıdır. Yüz metal için yüz kart taşınmaz; üretim yuvasına bırakınca tarifin istediği
  miktar ayrılır ve yığındaki serbest/ayrılmış miktar görünür. Tamamlanan aynı tür ürünler de stokta yığınlanabilir.
- Defterde tarifin adı, girdileri, çıktısı ve çalışma vardiyası sayısı bulunur. Tarif seçildiğinde üretim alanında
  gerekli girdilerin silik yuvaları açılır. Oyuncu doğru hammadde kartlarını ve üreticiyi bu işe atar.
- Uygun olmayan girdi tüketilmez, stoğa döner ve nedeni gösterilir. Açıklanmamış karışım deneyleri ana ilerlemenin şartı değildir.
- Örnek deneme tarifleri: zırh = 2 metal + 1 kumaş / 1 vardiya; EMP hücresi = 1 metal + 1 devre / 2 vardiya;
  bakım kiti = 1 kumaş + 1 kimyasal / 1 vardiya. Miktarlar, süreler ve tariflerin taktik etkileri henüz dengelenmedi.
- Bir vardiyalık iş kurulduğunda ilk devirden sonra ürün depoda olur. İki vardiyalık iş ilk devirde **1/2** iş kartına
  dönüşür; ikinci tamamlanan çalışma vardiyasından sonra ürün verir. İki vardiyalık ürün ilk sabah bedelsiz tamamlanmaz.
- Uzun işte her vardiya üretici gerekir. Üretici çekilirse işin kalan süresi korunur, ilerleme durur. Yeni üretici
  devam edebilir; daha önce tüketilen girdi yeniden alınmaz. Sınırsız ücretsiz işçi değiştirme verim kazancı üretmez.
- Başlamamış plan iptali ayrılmış girdiyi serbest bırakır. Başlamış işin iptali tüketilen girdiyi tam iade etmez;
  varsa hurda geri dönüş miktarı iptalden önce açıkça gösterilir. Üretim tamamlanınca iş kartı yerini ürün kartına bırakır.
- Başlangıçta tek adet sipariş varsayılır. Sonradan sınırlı adet/hedef stok emriyle aynı kurulum korunabilir; her vardiya
  aynı malzemeleri yeniden dizmek rutin zorunluluk olmaz. Uzun üretim eğitimle aynı kişi-vardiya kaynağını paylaşır.

## Kalıcı ölüm ve toparlanma

- Ölüm kalıcıdır. Kişi kartı aktif panodan kaldırılır ve isimli kayıp arşivine taşınır; boşalan iş otomatik doldurulmaz.
- Yere düşme ile ölüm ayrıdır: kurtarılan kişi yaralı döner; açıkça belirtilen ölüm koşulunda veya ölümle sonuçlanan
  terk kararında kaybedilen kişi geri gelmez. Mevcut bütün yaralanmalar sessizce ölüm sayılmaz.
- Sığınakta kalan ekipman stoğa döner. Sahada çıkarılarak kurtarılan ekipman korunur; terk edilen ekipman kaybedilir.
- Kurtarma veya terk kararı ve sonucu açık gösterilir; ölüm otomatik ekipman iadesi ya da bütün stok kaybı anlamına gelmez.
- Azalan kadro işlerin önceliğini değiştirmelidir. Temel görevlerin açık kalması ve düşük verimli ikmal yolu toparlanmayı mümkün kılar.
- Erzak yetersizliği önceden görünür, kademeli sonuçlar doğurur; tek kayıptan sonra uyarısız, kaçınılmaz bir ölüm zinciri oluşmamalıdır.
- Bir kişi ölünce otomatik bir yeni kişi üretilmez. Azalan kadro, tohumlu ve sınırlı ziyaret olasılığını artırabilir.
- Ziyaret sıklığı ve toplamı sınırlıdır; kayıp vererek sınırsız iş gücü veya ekipman elde etmek avantajlı değildir.
- Ziyareti beklemenin yanında gecikmesi görünen bir temas/kurtarma yolu bulunur; oyuncu toparlanmak için somut karar alabilir.
- Acil toparlanma seçeneği az kadroyla erişilebilir olmalı; gerekli maliyet, gecikme ve risk ayrıca dengelenmelidir.

## Yeni gelenler ve adil gecikmeli ihanet

- Mevcut kapı ziyaretçileri ve anlık sızma olayları ile bu öneri ayrıdır. Bu taslağın ardından uzun süre içeride kalan gizli robot sistemi de uygulandı; güncel sınırlar uygulama notundadır.
- Kabul edilen yeni sakin görev yapabilir ve güven kazanabilir; ihanet yalnız kabul ekranında verilen bir rastgele ceza değildir.
- Büyük ihanet öncesinde iki bağımsız, gözlenebilir ipucu ve bunlara karşı müdahale fırsatı bulunur.
- İlk ipucu plan kilitlenmeden önce vardiya brifinginde görünür. İkinci ipucundan sonra da bir müdahale penceresi korunur.
- İpuçları şüphe doğurur; tek tuhaflık kesin robot teşhisi olmaz. Geriye bakıldığında olayın izleri anlaşılabilir olmalıdır.
- Gözetim, silah/kit erişimini kısıtlama, güvenli görev veya karantina oyuncuya karşılık verir; bunların kişi kapasitesi karşılığı görünürdür.
- Sabotaj erişilen göreve uyar: üretimde malzeme kaybı, ikmalde yanlış bilgi, sahada ekipman veya konum riski gibi.
- Robot her göreve atanırken bütün sığınağı aynı ölçüde yok edemez. Ağır kayıp, uyarılar ve erişim kararlarıyla ilişkilidir.
- İhanet zamanı ve etkileri tohum/komut geçmişine bağlıdır; kartı tekrar açmak veya yüklemek kimliği yeniden zar atmaz.

## Pano etkileşimi ve topluluk olayları

- Sürükle bırak ana harekettir; kart seçip hedef seçme ve klavyeyle aynı işlemi yapma alternatifleri bulunur.
- Tıklama kart ayrıntısını açar. Kritik bilgi yalnız fareyle üzerine gelince görünmez; kart veya ayrıntı panelinden okunur.
- Görev yuvası gerekliliği ve beklenen katkıyı, ekipman yuvası uyumluluğu gösterir; geçersiz atama nedenini açıklar.
- Brifingler, ziyaretçiler, anlaşmazlıklar ve topluluk talepleri panoda olay fişi olarak kalır.
- Olay fişi ayrı bir “gündem bütçesi” tüketmez. Gerekiyorsa aynı kişi-vardiya kapasitesini, kaynağı veya sonuç riskini kullanır.
- Yeni vardiya önce kısa sonuç/brifing verir, sonra mevcut düzeni yeniden açar; rutin görevler kendiliğinden sıfırlanmaz.

## Fiziksel pano ve vardiya makinesi

- Panoya yaklaşınca okunabilir, sabit bir ön görünüş açılır. Kart taşırken kamera dönmez. Uzaktan atmosfer ve genel
  düzen görülür; yakından görev, kişi, üretim, depo ve eğitim aynı planın parçalarıdır.
- Fiziksel bölümler görevdeki kişi kartları, boşta/yaralı kişiler, üretim işi ve girdileri, hammadde/ürün deposu ve
  eğitim alanıdır. Kart kişideyse stokta ikinci kullanılabilir kopyası yoktur; bağ veya takılı kart biçiminde gösterilir.
- Tarif defteri pano yanında erişilebilir kalır. Her tarifi hatırlamak veya aynı vardiyada defteri defalarca taşıyıp
  kapatmak gerekmez. Ayrıntı görmek veya boş bir yere kart bırakmak simülasyon süresini ilerletmez.
- Makinedeki **kapağı aç → tuşa bas → fişi devir kutusuna koy** hareketleri kullanıcının istediği kapanış ritüelidir.
  Kutuyu hedefleyen son hareket zamanı ilerletir. Özet fiş, boş rolü, açlık riskini ve tamamlanacak işi okunabilir gösterir.
- Fişe alma/bırakma hedefleri geniştir; serbest fizik veya dar bir delik şart değildir. Yanlış yere bırakılan fiş
  çıkış tepsisine döner; vardiyayı kaybetmek veya odaya düşmüş kâğıdı aramak gerekmez. Animasyonlar kısa tutulur.
- Önceki vardiya fişleri arşivden okunabilir. Arşiv ile bu vardiyanın kapanış kutusu anlaşılır biçimde ayrılır;
  eski raporu incelemek yeni bir gün başlatmaz. Makinenin gizli gelecek bilgisi vermemesi gerekir.

## Eğitim ve kişisel gelişim

- Oyuncu kişi kartını **Eğitim** yuvasına koyar ve bir alan seçer: teknisyen, EMP, analist, üretim, mutfak, bakım veya ikmal.
  Eğitim bir kişinin tam vardiyasını alır; öğretmen, ayrı oda seviyesi veya yeni para birimi gerektirmez.
- Eğitimdeki kişi üretim yapmaz, baskına katılmaz ve normal erzak tüketimini sürdürür. Asıl bedel, o vardiyada
  bırakılan iş veya saha katkısıdır; aynı kişi eğitimle işi birlikte yapamaz.
- Meslek ve doğal yatkınlık sabittir; gelişen şey kişiye ait, alan bazında kalıcı **ustalık** düzeyidir.
  Alan değiştirmek önceki ilerlemeyi silmez. Görev kiti temel rolü açmaya devam eder; eğitim kitin yerini almaz.
- Eğitim seçimi, mevcut düzey, sonraki kazanım ve göreve katkısı önizlemede görünür. Küçük, alanına özel avantajlar
  tercih edilir; mutlak hasarı veya niyet tahminini basamak basamak şişiren genel stat artışları kullanılmaz.
  Örneğin bakımda iyileştirme katkısı, ikmalde taşıma verimi artabilir; mutfakta tüketim indirimi ve üretimde kaynak
  verimi sınırlı tutulur. Temel niyet bilgisi ve teknisyenin görev etkileşimleri eğitim şartına bağlanmaz.

İlk prototip için önerilen eğri aşağıdadır; sayılar denge denemesi içindir, kesin karar değildir.
Deneyim harcanabilir yeni bir puan değildir; her kişinin ilgili alanında biriken kendi ilerleme sayacıdır.

| Düzey | Ad | Kümülatif alan deneyimi |
|---|---|---|
| 0 | Temel | 0 |
| 1 | Eğitimli | 2 |
| 2 | Deneyimli | 6 |
| 3 | Uzman | 12 |

- Tamamlanan bir eğitim vardiyası seçilen alana **+2**, gerçekten tamamlanan ilgili iş veya sağ dönülen operasyonda
  o rolde fiilen görev almak **+1** deneyim verir. İlk eğitim, sıfırdan Eğitimli düzeyine çıkarıp görünür fayda sağlar.
- Yalnız biten faaliyet deneyim getirir: boş atölye, kart sürükleme ve başarısız önizleme ilerletmez.
  Eğitim rastgele başarısız olmaz; eğitim ve iş aynı vardiyada çift yazılmaz, kayıttan devam aynı sonucu tekrar ödüllendirmez.
- Tam iyileşme için yatması veya dinlenmesi gereken yaralı aynı vardiyada eğitim alamaz. Uzman düzeyindeki alanda
  faydasız eğitim onayı verilmez; kart, nedenini ve seçilebilecek diğer alanları gösterir.
- Eğitim ataması yalnız bir vardiya sürer; kapanıştan sonra kişi otomatik yedeğe/dinlenmeye döner. Yeniden eğitim
  oyuncunun açık seçimidir. Bu, diğer işlerin düzeni koruma kuralından bilinçli bir istisnadır; ustalık korunur.
- Tavan ve ilerledikçe uzayan gelişim aralıkları, erzak tüketimi ve ilerleyen alarm/dünya giderleri, herkesi son düzeye
  getirene kadar beklemenin karşılığıdır. Eğitim zorunlu hazırlık değildir: ilk üç saha kişisi eğitimsiz oynanabilir;
  yeni gelenin mesleği temel işleri veya kitle açılan yeni rolü kilitlemez. Kayıplardan sonra yeniden başlamak mümkündür.
- Kişi ölürse ustalığı kaybolur; yeni gelen kişi bu ilerlemeyi devralmaz. Kiti, mevcut kurtarma kurallarıyla korunabilir.
- Robot da eğitimle öğrenebilir. Eğitim başarısı insanlık kanıtı sayılmaz ve tek seans kimliği kesin biçimde açığa çıkarmaz.

## Sekiz kişilik örnek vardiya

Bu dağılım bir karar örneğidir; kesin rol isimleri, çıktı miktarları ve dengeli başlangıç iddiası değildir.

| Kişi | Kalıcı geçmiş/yatkınlık | Bu vardiyanın görevi | Verilen kit/ekipman |
|---|---|---|---|
| Aylin Demir | Elektronikçi, dikkatli | Baskın teknisyeni | Temel teknisyen kiti |
| Baran Ekin | Telsizci, soğukkanlı | Baskında EMP görevi | EMP kiti |
| Cem Ardıç | Güvenlik görevlisi, dayanıklı | Baskında koruma | Temel saha kiti ve silah |
| Derya Ilgın | Aşçı, düzenli | Yemek hazırlama; erzak tüketimini azaltma | Temel mutfak araçları |
| Ece Sarp | Tamirci, el becerisi yüksek | Bir koruyucu ekipman üretme | Üretim aletleri; ayrılan malzeme |
| Fırat Yalın | İzci, yön duygusu güçlü | Erzak ikmali | Taşıma çantası |
| Güneş Kaya | Depocu, temkinli | Dış kayıt teyidi | Telsiz ve kayıt defteri |
| Hakan Uslu | Sağlık çalışanı, sabırlı | Yaralı bakımı | Bakım kiti; gereken sarf |

Üç kişi sahada, beş kişi sığınak işindedir; kimse iki iş yapmaz. O vardiya ikinci bir üretici ve gözetmen ayrılmamıştır.
Ece'nin ürettiği ekipman kapanışta stoğa girer. İkmalden dönen erzak ve baskından gelen malzeme sonraki planlamayı değiştirir.
Bir şüphe fişi gelirse oyuncu gözetim için mevcut beş sığınak işinden birini bırakmalıdır; ayrıca destek hakkı kazanmaz.

## Belge kontrolü ve değişen talimatlar

Bu bölüm ilk önerinin ayrıntılarını korur; belge kontrolünün uygulanmış hâli OYUN-TASARIMI.md'dedir. Toplam üretim hesabını,
pano kartlarını ve kişi-vardiya görev akışını değiştirmeden kapıdaki kabul kararını ayrıntılandırır.

- Aynı anda 2–3 aktif kontrol kuralı bulunur; bütün kriterler her gün keyfî biçimde yenilenmez.
  Yeni talimat, ele geçirilen üretim kayıtları veya çalınan kimlikler gibi yeni bir bilgiye dayanır.
- Kişinin insan/robot kimliği ve bulunabilir ipuçları ilk gelişinde sabittir; inceleme, bekleme veya
  kabul kararı bunları yeniden zar atmaz. Talimat değişmesi kişinin geçmişini veya kimliğini değiştirmez.
- Belgenin gerçekliği, kişinin insanlığı ve kendisine verilebilecek yetki ayrı değerlendirilir.
  Tek belge hatası robot kanıtı değildir; sahte belge taşıyan bir insan veya çalıntı kimlikli robot olabilir.
- Oyuncu gelen kişinin belgesini arşivlenmiş belgeler, örnek geçerli mühür, çalınan kod listesi ve
  interkomdan aldığı beyan/fiziksel gözlemle karşılaştırır. Her olay bütün yöntemleri zorunlu kılmaz.
- Talimatın tarihi ve dayandığı bilgi görülebilir; eski belge ve talimatlar karşılaştırma için saklanır.
  Yeni bilgi yalnız ilgili eski dosyaları işaretler; önceki kabulü kendiliğinden suç veya ceza saymaz.
- Yeniden işaretlenen sakin için dosyayı inceleme ve hassas görev/yetkiyi geçici kısıtlama mümkündür.
  Talimatın gelmesi otomatik ihanet, görevden çıkarma veya kayıp başlatmaz.
- Ziyaretçi adayı kabul edilince aynı kimlikle isimli kişi kartına geçer; ayrı bir kişi üretilmez.
  Kişisel bilgilerde kabul öncesi beyan ile teyit edilmiş bilgi ayrılır; gizli robot kimliği veya
  programı, oyuncunun görebildiği profil metninde açık edilmez. İpuçları sonradan anlaşılabilir kalır.
- Kararlar kabul, ret, kısıtlı kabul veya ek incelemedir; bunların belge üzerindeki fiziksel uygulanışı
  ayrıca tasarlanır. Kısıtlı kabul hangi iş ve ekipmana erişilebildiğini açıkça gösterir.
- Beklemek garantili teşhis değildir. Gözetim, mevcut kadrodan kişi zamanı alır; gözetmen aynı vardiyada
  başka iş yapamaz. Bekleme kapasitesi sınırlıdır; bütün ziyaretçileri süresiz bekletmek bedelsiz olmaz.
- Herkesi reddetmenin karşılığı kaçırılan iş gücü ve toparlanma fırsatıdır; yeni sakinler sınırsız gelmez.
  Bu karşılık mevcut kayıp/toparlanma kurallarıyla dengelenir; ayrıca üretim veya gündem hakkı yaratılmaz.
- Kabul edilen robot önce yararlı iş yapabilir; gecikmeli eylemi verilen görev ve erişimle sınırlıdır.
  Büyük sabotaj öncesindeki iki bağımsız ipucu ve müdahale penceresi burada da geçerlidir.
- Mevcut anlık sızmanın oyun sonuna götürebildiği akıştan farklı olarak, bu taslakta tek kabul kararı
  doğrudan oyun sonu üretmez; uyarılar, verilen yetkiler ve müdahale kararları olayın ağırlığını belirler.
- Örnek: mühür geçerlidir fakat sevk kodu çalınan kod listesiyle eşleşir; bu bir araştırma nedenidir.
  Arşivdeki belge ve interkom beyanı birlikte incelenebilir; geçerli mühür tek başına güvence değildir.
- İlk prototip iki belge türü, iki teşhis kuralı, bir masum belge hatası ve bir gecikmeli sabotajla sınırlıdır.
  Kimliği bilmeyen oyuncunun ipuçlarını anlayabilmesi ve kararının bedelini görebilmesi sınanır.

## Uygulamadan önce doğrulanacaklar

- Aşçı verimi, temel erzak erişimi, kitlerin yeniden edinimi ve az kadroyla toparlanma maliyetleri birlikte denenmeli.
- Üç saha rolünün kit/beceri sınırları ve yaralıların çalışabilirliği prototipte netleştirilmeli.
- Rastgele can/hareket/gücün rol ve ekipmanla birleşimi, ilk görevlerin çözülebilirliği ve taktik önizlemenin doğruluğu sınanmalı.
- Bir ve iki vardiyalık tarif, üreticinin çekilmesi, aynı girdinin iki siparişe ayrılamaması ve her aşamada kayıt/devam doğrulanmalı.
- Fiş basıldıktan sonra, kutuya konma sırasında ve kararmada çıkıp devam etme; eski fiş; art arda tıklama yalnız bir geçiş vermeli.
- İpucu anlaşılabilirliği ve müdahale süresi, önceden kimliği bilmeyen oyuncularla sınanmalı.
- İlk öğrenme turunda sekiz kart önerilen düzende başlar; oyuncu önce bir kişiyi role, bir ürünü kişiye taşır.
  Ardından tek tarif/tek vardiya üretim, eğitim, devir makinesi ve en son belge kuralı açılır. Bütün işlemler ilk gün istenmez.
- Bu belge tasarım kararı taslağıdır; kaynak tüketimi, kayıt sürümü ve mevcut oyuna geçiş henüz uygulanmış değildir.
