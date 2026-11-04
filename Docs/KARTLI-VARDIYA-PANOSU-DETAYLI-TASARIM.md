# Kartlı vardiya panosu — bütünleşik ayrıntılı tasarım

_2026-11-04 · Tasarım önerisi; Unity sahnesine veya oyun simülasyonuna uygulanmadı._

Bu belge [önceki taslağı](KARTLI-VARDIYA-PANOSU-TASARIMI.md) ayrıntılandırır. Kullanıcının istediği yön:
sekiz isimli rastgele özellikli sakin, ayaklı planlama panosu, sürükle bırak görev ve teçhizat dağıtımı, seçilebilir
eğitim, tarif ve hammaddeyle vardiya süren üretim, kalıcı ölüm, belgeyle yeni sakin kabulü, gecikmeli robot ihaneti
ve kâğıt çıkaran vardiya devir makinesidir. Aşağıdaki kapasite, miktar, aralık ve ödüller deneme önerileridir.
Uygulanmış sistemi anlatan OYUN-TASARIMI.md değiştirilmez; eski teknik testler bu modelin dengesini doğrulamaz.

## 1. Oyuncunun işi ve gelişim

Oyuncu bir komutandır. Bir vardiyanın temel soruları: kimi dışarı çıkaracağım, kim içeride ne yapacak, hangi malzemeyi
gelecek baskına yatıracağım, kimi yetiştireceğim ve kapıdaki kişiye ne kadar erişim vereceğim?

Harita harekât hedefini, pano insan ve eşya dağılımını, kapı güven kararını taşır. CRT baskına geçiş/izleme yüzeyi
olmaya devam eder. Oda seviyeleri yerine insanlar, ustalık, kurtarılan tarifler, yeni kitler ve mevcut harekât
ilerlemesi gelişim sağlar. Eğitim veya yeni tarif için bir karakterin ölmesi gerekmez; ilk ekiple kazanmak mümkündür.

Odada kart taşırken, belge okurken ve düşünürken simülasyon zamanı akmaz. Bir kişi bir vardiyada bir iş yapar.
Üç kişi baskına çıktığında sekiz kişilik başlangıçtan beş kişi sığınak işleri için kalır. Ayrıca bir ana faaliyet/
bir destek kotası yoktur. Bakım, eğitim ve üretim aynı kişi-vardiya kapasitesini paylaşır.

## 2. Odanın ve panonun yerleşimi

Soldaki sığınak ünitesinin yerine ayaklı pano gelir. Ayarlar dolabı ayrı işlevdir. Panoya yaklaşınca kartları
okunabilir ölçekte gösteren sabit bir görüş açılır; sürükleme sırasında oda kamerası dönmez.

| Fiziksel alan | İçerik | Oyuncunun kararı |
|---|---|---|
| Üst şerit | Vardiya, erzak gidişi, serbest/ayrılmış girdiler, plan kilidi | Bugünkü plan sürdürülebilir mi? |
| Topluluk sicili | Sekiz kişinin ad/portre izleri; kim nerede, yaralı veya boşta | Uygun kişi kim? |
| Saha kadrosu | Teknisyen ve iki diğer rol; kişi, kit ve ek donanım | Baskına kim hangi işle çıkacak? |
| Sığınak işleri | Üretim, mutfak, bakım, ikmal; gerektiğinde gözetim | Hangi iş eksik kalacak? |
| Eğitim | Kişi, seçilen alan ve bir vardiya sonrasının kazanımı | Bugünkü katkıyı geleceğe yatıracak mıyım? |
| Üretim cebi | Tarif/iş emri, girdiler, çalışan ve kalan vardiya | Eldeki parçayla ne yapacağım? |
| Alt raf/depo | Hammadde yığınları, boşta kitler, bitmiş ürünler | Neyi saklayacağım, kime vereceğim? |
| Defter ve dosya cepleri | Tarifler, kişi belgeleri, güncel kabul talimatı, olay fişi | Hangi bilgiyi karşılaştıracağım? |

Sicilde kalan portre izi ikinci taşınabilir kişi kartı değildir; kişinin görevini bulmaya yarar. Örneğin
“CEM · EĞİTİMDE” izine basınca ilgili gerçek kart gösterilir. Aynı kişi iki yerde çalışmaz.

İlk deneme bir üretim ve bir eğitim cebiyle yapılabilir. Bir vardiyada birden fazla kişiyi aynı işe ayırmanın
kapasite/verim kuralları ayrıca ölçülür; bu cepler oda seviye ağacına dönüşmez. İlk pano bitmiş ve kullanılabilirdir.

## 3. Kart dili ve sürükleme kuralları

| Kart | Üzerinde görünenler | Nasıl kullanılır? |
|---|---|---|
| Kişi | Ad, portre, meslek, can, hareket, fiziksel güç, mevcut durum | Bir görev/eğitim yuvasına bırakılır |
| Rol kiti | Rol, verdiği yetenek, varsa eğitim/ekipman koşulu | İlgili kişinin görev kiti cebine takılır |
| Ek teçhizat | Etki, uyumlu yuva, tek kullanımlı olup olmadığı | Kişinin zırh veya yardımcı cebine bırakılır |
| Hammadde | Tür, toplam, ayrılmış ve serbest adet | Tarifin girdi yuvasına gereken adedi ayırır |
| Tarif | Çıktı, gereken girdiler, çalışma vardiyası | Üretim iş emri oluşturur |
| Devam eden iş | Ürün, ödenmiş girdiler, çalışan, ilerleme | Üreticisi değişebilir; ilerleme işe aittir |
| Olay/dosya | İlgili kişi, bilgi, seçenek, varsa vade | İnceleme ve karar alanını açar |

Kartı tutunca uygun hedefler görünür. Hedef üzerinde bırakmadan önce sonuç önizlenir: yeni görev, kaybolacak eski
katkı, ekipman eksikliği ve kaynak rezervasyonu. Geçersiz bırakmada kart eski yerine döner; malzeme harcanmaz.
Hiçbir görev yalnız nesnenin ekran koordinatından anlaşılmaz; açık görev yuvası vardır.

Dolu görev yuvasına bırakıldığında iki kişinin yeni yerleri önizlemede gösterilir; yalnız iki atama da geçerliyse
takas yapılır. Uygun değilse bırakma reddedilir. Eğitim alanı seçimi kişiye ait kalır; başka kişiye aktarılmaz.
Kit/teçhizat kendiliğinden üretilmez. Takastan sonra eksik veya uyumsuz kit varsa emirden önce açıkça tamamlanmalıdır.

Kişi başka göreve geçtiğinde kişisel ekipmanı üzerinde kalır; başka kişiye doğrudan aktarılabilir veya depoya
konabilir. Ekipman kartı kişideyken stokta ikinci kullanılabilir kopyası görünmez. Başlangıç önerisi: bir rol kiti,
bir koruyucu ve bir yardımcı teçhizat yeri. Çanta geometrisi, parça dayanıklılığı ve ağırlık sistemi ilk kapsamda yoktur.

Sürükleme, kartı seçip hedefe basma ve klavye aynı işlemi yapar. Ayrıntı kartı tıklamayla büyür; kritik bilgi yalnız
fareyle üzerine gelince görünmez. Yığınlar gerektiği kadar bölünür: 12 metalden 2 ayrılınca serbest miktar 10 olur.

## 4. Sekiz kişinin oluşturulması

İlk sekiz sakin bilinen insanlardır. Rastgele isim/portre/geçmiş eşleşmeleri tutarlı olur; yaş ve meslek geçmişi
birbiriyle çelişmez. Aynı kişi iki kez üretilmez. Daha sonra gelen adayların gizli kimliği ayrı belirlenir.

Kişinin beş ayrı katmanı bulunur:

1. **Kimlik:** Adı, portresi, geçmişi, geliş tarihi; kalıcı kişi kimliği.
2. **Beden:** Azami can, hareket ve fiziksel güç; küçük aralıklarda çeşitlenen temel değerler.
3. **Meslek/yatkınlık:** Elektronikçi, sağlık çalışanı, aşçı gibi başlangıç avantajları; bir güçlü ve bir zorlayıcı özellik.
4. **Öğrenilen ustalık:** Her alanın kişisel ilerlemesi; görev değişince silinmez.
5. **Güncel durum:** Yaralar, mevcut görev, kit ve teçhizat; etkili değerleri değiştirir.

Bütün değerler birbirinden bağımsız en yüksek/en düşük zarlarla oluşturulmaz. Bir avantaj bütçesi kullanılır:
dayanıklı/yavaş, çevik/kırılgan, becerikli/daha az saha dayanıklılığı gibi farklı profiller oluşur. Her rastgele
ekibin ilk görevler için kullanılabilir üç kişisi, temel kitleri ve temel sığınak işlerini yapabilen sakinleri vardır.

Deneme başlangıcı için mevcut rol tabanına göre can/harekette en çok bir basamaklık fark düşünülebilir. Bu sayılar
kilitlenmez: mevcut dar ve süreli tahtalarda bir kare fazlası bile çok değerlidir. Yeni profil havuzu görevlerin
çözülebilirliğiyle birlikte ölçülür. Eksiksiz güç tablosu ilk prototipin denge işidir.

**Fiziksel güç**, yalnız ilgili fiziksel vuruşun kuralında kullanılır. EMP aygıtının hasarı, kancanın çekmesi ve
sis etkisi ayrıca tanımlanır; kişisel güç hepsine genel hasar eklemez. Kartta seçili rolün gerçek saldırı/itme/çekme
etkisi ayrıca okunur. Öğrenme veya rastgele özellik kritik hamleyi görünmeyen bir başarısızlık zarına dönüştürmez.

Meslek belge üzerinde beyan edilebilir; bu beyan güvenilir kimlik kanıtı değildir. Oynanışta kullanılan can, hareket,
hasar ve üretim hesabı ise doğru gösterilir. Robotluğu gizlemek için oyuncuya yanlış hamle önizlemesi verilmez.

## 5. Roller ve sığınak işleri

| Görev | Bir vardiyada yaptığı | Açık karşılığı |
|---|---|---|
| Teknisyen | Baskında temel sabotaj ve görev etkileşimleri | Üç saha yerinden birini ve kişinin vardiyasını kullanır |
| EMP uzmanı | Mevcut EMP/itme yaklaşımı | Kişi ve uygun kit gerekir |
| Analist | Mevcut kanca/çekme yaklaşımı | Kişi ve uygun kit gerekir; temel niyet bilgisi herkese açık kalır |
| Üretici | Tek bir iş emrinin çalışma vardiyasını ilerletir | Üretim girdisi ve tam kişi-vardiya |
| Mutfak | Ortak erzak tüketimini azaltır | Erzak yaratmaz; aşçısız kuru erzakla yaşamak mümkündür |
| İkmal | Sınırlı yerel erzak katkısı sağlar | Büyük hammadde/ilerleme ödüllerinin yerine geçmez |
| Bakım | Uygun yaralıya açık iyileşme katkısı verir | Ayrı sakin gerekir; ölü kişiyi geri getirmez |
| Eğitim | Seçilen alana kişisel ilerleme verir | Üretim, saha veya rutin iş yapılmaz |
| Gözetim/inceleme | Tek bir kişi veya dosyaya ilişkin kontrollü inceleme | Gözetmen başka iş yapmaz; kesin insanlık garantisi vermez |
| Dinlenme/yedek | Temel toparlanma ve sonraki vardiya için hazır kalma | Üretim ve eğitim katkısı yoktur |

Bakım önceden belirli bir yaralıya veya **dönüş triyajına** ayrılabilir. Triyajda görevli, baskından dönen en
yaralı uygun kurtulana kapanışta bakım yapar; öncelik önceden bilinir, baskın sonucu görüldükten sonra yeni görev
atanmaz. Uygun yaralı yoksa bakım sarfı ve deneyim üretilmez. Bu, bakım verenin tam vardiyalık görevidir; yere
düşmüş birini uzaktan kurtarmak veya aynı kişiye iki iş yazmak anlamına gelmez.

Rol, kişinin bütün hayatı boyunca bağlı olduğu sınıf değildir. Kit ve temel yeterlilikle görev değiştirilebilir;
uzmanlık aynı görevi daha iyi veya daha çeşitli yapmayı sağlar. Son teknisyen kaybı rastgele bir ziyaretçi
beklemeyi zorunlu kılmaz: hayatta uygun bir sakin varsa temel rolü üstlenme ve kit edinme yolu açık kalır.

Madenci ve Sis uzmanı ileride kurtarılan tarif/kit veya harekât temaslarıyla açılabilir. Bunlar artık atölye oda
seviyesine bağlanmaz. İlk öğrenme bölümünde mevcut Teknisyen/EMP/Analist üçlüsü yeterlidir.

İkmal her vardiya istediği türde sınırsız hammadde üretmez. Ana parça arzı baskınlardan, sınırlı dış ticaret veya
tanımlanmış temaslardan gelir. Böylece yalnız içeride beklemek bütün ekipman ihtiyacını çözmez.

## 6. Eğitim ve güçlenme

Kişi Eğitim alanına konur, ardından alanı seçilir: teknisyenlik, EMP, analiz, üretim, mutfak, bakım veya ikmal.
Önizleme o vardiyanın sonunda kazanacağı ilerlemeyi, basamak değişimini ve varsa yeni avantajı gösterir.
Eğitmen, ayrı ders parası veya oda seviyesi ilk modelde zorunlu değildir.

| Basamak | Kümülatif alan deneyimi — deneme değeri |
|---|---|
| Temel | 0 |
| Eğitimli | 2 |
| Deneyimli | 6 |
| Uzman | 12 |

Tamamlanan bir eğitim ilgili alana +2; gerçekten yapılan ilgili iş veya o rolde katılıp sağ dönülen baskın +1 verir.
Bu harcanabilir para/puan değildir; kişinin alan sayacıdır. Üst basamaklar daha çok zaman ister. Alan değiştirmek
önceki öğrenmeyi silmez. İlk eğitim sıfırdan Eğitimli basamağına çıkar; ilk görünür katkı küçük tutulur.

Ustalık avantajları rol bazında seçilir. Örnek adaylar: üretimde belirli tarife erişim veya sınırlı girdi verimi;
mutfakta sınırlı tüketim indirimi; bakımda ek iyileşme; sahada baskın başına sınırlı bir kit kullanımı/alternatif
etkileşim. Bunların tam sayıları ve saha yetenekleri henüz seçilip ölçülmedi. Her basamak bütün canı, hareketi ve
hasarı aynı anda artırmaz; azami basamak kitleri ve taktik kararları gereksizleştirmez.

Eğitim rastgele başarısız olmaz. Boş üretim tezgâhı, menü açma, kartı tekrar bırakma veya bitmemiş faaliyet deneyim
vermez. Bir kişi aynı vardiyada eğitim ve iş deneyimi alamaz. Uzman olduğu alana faydasız eğitim onayı verilmez.
Tam yatış gerektiren yaralı eğitim alamaz; eğitim normal dinlenme/tedavi vardiyasının yerine bedelsiz geçmez.

Eğitim tek vardiyalıktır; kapanışta kişi yedeğe döner, yeniden eğitim açıkça seçilir. Diğer görevler düzenini korur.
Eğitim sırasında erzak tüketilir ve dünya/alarm ilerler. Temel işlerin eğitimsiz yapılabilmesi kayıp sonrası
toparlanmayı mümkün kılar. Ölenin ustalığı yeni gelene geçmez. Robot da eğitilebilir; öğrenmesi insanlık kanıtı değildir.

## 7. Hammadde, tarif ve bitmiş ürün

İlk hammadde önerisi metal, devre, kumaş ve kimyasaldır; erzak tek ayrı stok olarak kalır. Fon mevcut harekât ve
tanımlanmış ticaret/ikmal işlerinde kullanılabilir. Her tarife otomatik olarak ilave fon bedeli konmaz. Eski oda
geliştirme giderleri kalktığından mevcut fon ve malzeme dengesi aynen geçerli sayılmaz.

Defterde sayfa seçilince üretim alanında çıktı ve girdi cepleri görünür. Oyuncu hammadde kartlarını ve üreticiyi
koyar. Yanlış girdi tüketilmez; eksik tür/adet açıkça yazılır. Gizli karışım bulmacaları ilerleme şartı değildir.

| Deneme tarifi | Girdiler | Çalışma süresi | Önerilen ürün davranışı |
|---|---|---|---|
| Zırh plakası | 2 metal + 1 kumaş | 1 vardiya | İlk hasarı karşılama; kullanıldığında kırılır |
| Bakım kiti | 1 kumaş + 1 kimyasal | 1 vardiya | Uygun bakımda tüketilen sarf; ölümden dönüş sağlamaz |
| EMP hücresi | 1 metal + 1 devre | 2 vardiya | EMP kitine sınırlı ek kullanım/seçenek; temel rolü zorunlu sarfa bağlamaz |

Bu tablo dengelenmiş reçete değildir. EMP hücresinin tam taktik etkisi, bakım kitinin miktarı ve ustalık indirimi
prototipte seçilip ölçülmelidir. Rolün ana kiti ile tek kullanımlık ürün ayrıdır: ana kit temel yeteneği verir,
ek ürün sınırlı avantaj sağlar. Birkaç sarf bitti diye bütün temel saha rolleri devre dışı kalmaz.

Üretim durumu: **taslak → girdileri ayrılmış → başladı → devam ediyor/durakladı → tamamlandı.** İlerleme kişide
değil iş emrindedir. Toplam girdi ilk çalışma kapanışında bir kez tüketilir. Sonraki çalışma vardiyasında girdi
yeniden alınmaz; ürün ancak gereken vardiya sayısı tamamlanınca depoya girer.

Üretici başka işe çekilirse veya kaybedilirse iş durur; başka bir sakin sonraki vardiyada devralabilir. İlerleme
korunur. Yeni işçi koymak aynı gün ikinci kez ilerletmez. Bir vardiya sonunda bir işi bitirip kalan vakitle
kendiliğinden ikinci tarif tamamlanmaz. İşçi hızı değişecekse toplam/kalan süre planlamada açıkça yeniden hesaplanır;
işçi değiştirerek süre azaltma açığı oluşmamalıdır. İlk prototipte sabit tarif süresi daha anlaşılırdır.

Başlamamış iş iptali ücretsizdir. Başlamış işin tüketilmiş girdileri tam iade edilmez; ilk modelde iptal uyarısı
bunu söyler. Hurda geri dönüşü daha sonra eklenebilir. Varsayılan sipariş bir adettir. Daha sonra sınırlı adet veya
hedef stok emri eklenirse, her vardiya aynı girdileri tekrar dizmek gerekmez; bitmiş sipariş habersiz yenilenmez.

## 8. Planlama, baskın ve vardiya makinesi

| Evre | Oyuncunun yapabildiği | Değişen oyun durumu |
|---|---|---|
| Brifing | Yeni rapor, talimat ve ziyaretçiyi görmek | Önceki kapanışın sonuçları okunur |
| Serbest plan | Kart, kit, tarif ve eğitim değiştirmek; kapı kararları | Önizleme/rezervasyon; henüz üretim ödülü yok |
| Baskın emri | Bütün planı kontrol edip baskına çıkmak | Kadro/işler kilitlenir; baskın bedeli bir kez ödenir |
| Baskın/dönüş | Taktik oyunu oynamak, gerçek dönüş sonucunu okumak | Baskın sonucu bir kez kaydedilir; plan yeniden açılmaz |
| Devir fişi | Kapağı açmak, tuşa basmak, bilinen planı okumak | Baskın yoksa plan bu aşamada kilitlenir; gün hâlâ ilerlemez |
| Fişi kutuya koyma | Vardiyayı kesin devretmek | İşler, girdiler, tüketim, eğitim ve dünya bir kez işlenir |
| Yeni vardiya | Kısa kararma sonrası ürün/ilerleme ve yeni brifing | Tamamlanan ürünler yeni plan için kullanılabilir |

Rezervasyon stoktan ödeme değildir. Örneğin 4 metalin 2'si ayrıldığında toplam hâlâ 4'tür, serbest miktar 2'dir.
Kilitlenen 2 metal başka tarif/ziyaretçi yardımına ayrılamaz. İlk iş kapanışında toplamdan bir kez düşer. Baskın
ganimeti mevcut kilitli emre yeni tarif ekleme hakkı vermez; yeni planlamanın stoğuna katılır.

Makine oyuncunun istediği **kapağı aç → tuşa bas → fişi alıp devir kutusuna bırak** ritüelini korur. Kutuya son
bırakma tek gün ilerletme anıdır. Fiş plan kimliğine bağlıdır; yeniden basmak veya eski fişi kutuya atmak ikinci
gün başlatmaz. Arşiv kutusuyla aktif kapanış gözü anlaşılır biçimde ayrılır.

Fişin üzerinde saha kadrosu/gerçek dönüş sonucu, tüketim, tamamlanacak işler, eğitim kazanımı ve bilinen riskler
bulunur. Henüz oluşmamış ziyaretçiyi veya gizli ihaneti önceden açmaz. Bekleyen kapı kararının varsayılan sonucu
ve açık vade varsa kapanmadan gösterilir. Üretim eksiği veya yaralı birini göreve koyma gibi geçersiz plan önce çözülür.

Kâğıt serbest fizik nesnesi gibi kaybolmaz: belirgin tutma ve bırakma hedefleri vardır; yanlış yerde çıkış tepsisine
döner. Animasyonlar kısa, okumak oyuncunun kontrolündedir. Baskın sürerken veya zorunlu bir karar çözülürken devir
başlatılamaz. Kayıt/devam basılmış fişi, kilitli planı, ödenmiş bedelleri ve tek kapanış durumunu korur.

## 9. Kapı, belgeler ve değişen talimat

Geliş aralığı için ilk deneme 2–4 vardiya olabilir; ilk öğretici aday güvenli ve anlaşılır olur. Aynı anda tek açık
kapı vakası tutulur. Ziyaret üretimi ekrana bakmaya bağlı değildir. Bir kayıp anında yerine otomatik yeni kişi
yaratılmaz; aday aynı kişinin deneyimli kopyası değildir.

İlk modelde ziyaretçinin kimlik/başvuru kâğıdı ve gerektiğinde sevk belgesi yeterlidir. Ad, fotoğraf, önceki iş,
geldiği yer, düzenleme tarihi, belge/sevk numarası ve düzenleyen mühür karşılaştırılır. Öğretici ilk vakada bütün
alanları aynı anda sorgulatmaz. Kabul edilen kişi aynı kimlikle panonun yeni sakin kartı olur; belgeleri dosyasına girer.

Oyuncunun kontrol kâğıdında sürüm/tarih, hangi bilginin eklendiği ve en çok 2–3 etkin kural bulunur. Kurallar yeni
istihbarat, kurtarılan üretim kayıtları veya çalınmış belgelerden gelir. Eski kurallar arşivlenir; yeni metin eskisini
habersizce tersine çevirmez. Robotların özellikleri sırf takvim ilerledi diye aynı gövdede değişmez.

Örnek talimat maddeleri — kurgusal ve henüz seçilmemiş senaryo adayları:

- “N-17 sevk serisinin boş belgeleri ele geçirildi. Bu seriyi bağımsız teyit olmadan güvenilir saymayın.”
- “S-6 gövdelerinde soğuk ortam gözleminde nefes buğusu oluşmuyor. Bu işaret bütün robot serilerini kapsamaz.”
- Daha sonraki bilgi: “Yeni R serileri buğuyu taklit edebiliyor; kimlik ve sevk kayıtlarını da karşılaştırın.”

Belge kusuru, kişisel çekingenlik, hastalık veya geçmişteki zorluk tek başına robot kanıtı değildir. Geçerli belge
de insanlık garantisi değildir. Gözlem için gereken koşul ve sınırlar oyuncuya anlatılır; kontroller rastgele
başarısız olmaz. Her ziyaretçiye üç ayrı inceleme veya yeni bir tıbbi cihaz minioyunu zorunlu tutulmaz.

Temel görüşme ve belge okuma faaliyet harcamaz. Dış kayıt teyidi veya gözetim için pano görevi istenirse bir
sakinin vardiyası kullanılır. Bunun neyi kontrol edeceği önceden yazılır; beklemek otomatik insanlık sertifikası vermez.

Kararlar: **kabul, kısıtlı kabul, ret, dışarıdan yardım.** Kısıtlı kabulde adayın kritik saha/kit/erişim kullanımı
kapalıdır; çalıştırılacaksa ayrılan gözetmenin vardiya katkısı da gider. İlk denemede bir gözlem yeri yeterlidir.
Kısıt süresi ve bundan sonraki karar görünür; herkesi süresiz bekletmek ücretsiz kusursuz çözüm olmaz.

Yeni talimat eski bir dosyayla eşleşirse yalnız ilgili kart inceleme fişi alır. Diğer yedi kişiyi her gün yeniden
kontrol etmek gerekmez. Oyuncu eski belgeyi açıp erişimi kısıtlayabilir ve inceleme isteyebilir. Yeni kuralın gelmesi
geçmiş kabul kararına otomatik ceza veya anlık ihanet eklemez.

## 10. Gizli robotun eylemi

Kimlik ve bulunabilir ipuçları kişinin gelişi sırasında sabitlenir. Gizli durum görünür kişi kartına yazılmaz.
Yüksek can, iyi meslek veya hızlı eğitim robot işareti değildir. Robot normal çalışabilir, eğitilebilir ve güven
kazanabilir; insanlarla bağ kurulmuş olması sonraki eylemin ağırlığını artırır.

| Aşama | Olan şey | Oyuncunun olanağı |
|---|---|---|
| Yerleşme | Normal katkı verir; verilen görev ve erişim kaydedilir | Belgelerini inceleme, erişimi baştan sınırlama |
| Erişim arayışı | Görevle ilgili somut bir kayıt/istek çelişkisi | İlk iz üzerinde soru veya görev değişikliği |
| Hazırlık | İkinci bağımsız bulgu, belirli bir hedefe yaklaşma | İnceleme, kit/erişim kaldırma, kişiyi ayırma |
| Eylem | Önceden hazırlanan görevle ilişkili sabotaj | Bilinen bedellerle zararı sınırlayan kriz kararı |

İki bağımsız iz ve müdahale olanağı, büyük kalıcı zarardan önce bulunur. İzler yalnız ölebilen tek tanığa bağlı
değildir; gerektiğinde erişim kaydı gibi alternatif vardır. Yanlış tahmin bir masumu mağdur edebilir; bunun bedeli
karar öncesi anlaşılır olur. Yeterli kanıtı erken bulmak robotu eyleminden önce yakalayabilmelidir.

Eylem kişiye verilen işe uyar: depoya erişen stok/rota bilgisiyle, üretici teçhizatla, telsiz erişimi olan dış
haberleşmeyle oynar. Kısıtlı erişim bütün olası zararı otomatik silmez ama kapsamını küçültür. Tek bir kabul,
uyarısız ve kaçınılmaz oyun sonu değildir. Bir robot aynı anda bütün sistemleri bozmaz.

İlk örnek: Elektrikçi Rauf geçerli mühürlü ama şüpheli sevk numaralı belgeyle gelir. Derya yaralı olduğu için işe
alınır. Sonra görev dışı telsiz erişimi görünür; daha sonra bakım kaydıyla uyuşmayan bir köprü parçası bulunur.
Oyuncu hassas görevini kaldırabilir veya inceletebilir. İzleri görmezden gelirse bir brifingde dış kapı motorunu
YZ komutuna açmaya çalışır. Enerjiyi kesmek hizmeti durdurabilir, diğer müdahale içeridekileri riske atabilir;
seçeneklerin bilinen bedelleri gösterilir. Tam erişim daha ağır, kısıtlı erişim daha yerel krize yol açar.

İlk sabotaj uygulaması plan öncesi brifingde çalışır; yeni FPS çatışması veya baskın ortasında ikinci zaman sistemi
gerektirmez. Sabotaj sırasında yapılan acil güvenlik kararı ücretsiz yeni üretim/eğitim vardiyası değildir.
Kalıcı hasarı onarmak sonraki planın işi olabilir. İnsan hain, iyi niyetli robot ve büyük ilişki ağı ilk kapsamda yoktur.

## 11. Kalıcı ölüm, eksilen kadro ve yeni gelen

Sıfır can önce yere düşmedir. Kurtarılan kişi yaralı döner; açık ölüm koşulunda veya ölümle sonuçlanacağı belirtilen
terk kararında kaybedilen kişi geri gelmez. Kaçışta kimin geride kaldığı ve ekipman sonucu onaydan önce adlarıyla
gösterilir. Eski oyundaki bütün yaralanmalar sessizce kalıcı ölüme çevrilmez; kurtarma ve kaçış yeniden tasarlanır.

Ölenin kartı aktif görevden çıkar, kayıp arşivine geçer; adı ve geçmişi kalır. Kurtarılan ekipman depoya döner;
geride kalan kaybedilir. Kişinin ustalığı yeni gelene aktarılmaz. Boş göreve otomatik yedek atanmaz. Her ölümün
üstüne ayrıca büyük güven, erzak ve fon cezaları yığılmaz; insan ve yetişmiş iş gücü kaybının kendisi ağırdır.

Başlangıç sekizdir. İlk deneme üst kapasitesi on kişi olabilir; bu sayı teyit edilmiş karar değildir. Doluyken
adayı kabul etmenin sonucu önceden yazılır; sessizce kaybolan sakin veya sınırsız çoğalan nüfus olmaz. Dış yardım
ve ayrılan kişinin güvenli dış yerleşime yönlendirilmesi ileride açık seçenek olabilir.

Az kişi kalınca düşük tüketim, temel işlerin açık kalması ve erişilebilir temas toparlanmayı destekler. Öneri:
bir sakini dış temas için ayırmak, açık bir süre sonunda yeni aday görüşmesi sağlar; adayın insanlığını satın almaz.
Bu, zorunlu tam kadro baskınına çıkamayan topluluğa çıkış yolu verir. Ücretsiz sonsuz insan üretmez; süre/maliyet
ve mevcut tehdit birlikte dengelenir. Topluluk tamamen kaybolursa gerçek yenilgi olabilir.

Harekâtın harita/bölge hedefi korunur. Yeni kart sistemi ayrı bir sonsuz sığınak simülasyonuna dönüşmez. Yöneticinin
ölüp halefle devam etmesi daha sonraki genişletmedir; bu döngünün çalışması ona bağlı değildir.

## 12. Bir vardiyanın somut örneği

Örnek, önceki pano denemesindeki sekiz isimle kuruludur; stok ve sonuç sayıları deneme içindir.

| Kişi | Bugünkü görev | Bugün vazgeçilen katkı |
|---|---|---|
| Derya | Saha teknisyeni; zırh takılı | Sığınak üretimi/onarımı |
| Selin | EMP; zırh takılı | Becerikli üretici |
| İpek | Analist; yardımcı kit takılı | Dış kayıt incelemesi |
| Deniz | Bir zırh plakası üretimi | Teknisyen eğitimi veya yedek |
| Nermin | Mutfak | Bakım veya eğitim |
| Zeynep | Bakım ve dönüş triyajı | Gözetim |
| Orhan | Erzak ikmali | Eğitim |
| Cem | Üretim eğitimi | İkinci saha adayı veya sığınak işi |

Örnek stokta 3 metal, 2 devre, 2 kumaş ve 1 kimyasal vardır. Zırh için 2 metal/1 kumaş ayrılır; planlamada toplam
silinmez. Baskın emri bütün dağılımı kilitler; saha sonucu belirlenir. Makine fişi gösterir. Fiş kutuya konunca
bir üretim vardiyası tamamlanır: metal 1, devre 2, kumaş 1, kimyasal 1 kalır ve depo bir zırh kazanır. Gerçek baskın
ganimeti varsa ayrıca kayda geçer; bu örnek üretim hesabına karıştırılmaz.

Cem üretimde +2 alıp Eğitimli olur, yedeğe döner. Deniz tamamladığı üretimden +1 alır. Nermin tüketimi düşürür,
Orhan erzak getirir; net erzak farkı panoda birlikte gösterilir. Zeynep'in önceden seçilen triyajı yalnız uygun
yaralı kurtulan varsa işler; boş bakım görevinden ödül/deneyim çıkmaz. Derya'nın yarası, varsa uygulanmış bakım
ve doğal toparlanma ayrı gösterilerek gerçek dönüş sonucuna göre korunur.

Yeni vardiyada aday gelirse üç kişinin dışarıda olduğu başka bir gün için hangi işi bırakıp inceleme yapacağına
karar verilir. Örneğin Zeynep gözetmen yapılırsa o gün bakım katkısı yoktur. Üretilen zırh depodan yeni saha kişisine
verilebilir. Aynı Cem hem eğitim hem üretim yapmış sayılmaz.

İki vardiyalık örnek: Deniz zırh yerine EMP hücresi seçseydi ilk geçişte hammadde ödenip **1/2** iş kalırdı;
sonraki vardiyada Deniz'i sahaya gönderirse iş dururdu. Cem üretici olarak devralırsa ikinci kapanışta tamamlanırdı.
Hammadde ikinci kez düşmez, Cem'in daha önce aldığı eğitim kaybolmaz.

## 13. Öğrenme eğrisi ve ilk kapsam

İlk sekiz kart önerilen görev dağılımında başlar. Oyuncu kartları görmekle sekiz ayrı ayar yapmaya zorlanmaz.

1. İlk bölümde üç saha kişisi tanıtılır; bir kartı role, bir zırhı kişiye bırakmak yeterlidir.
2. İlk dönüşte yarayı ve tek vardiyalık temel üretimi öğrenir; tarif, iki girdi türü ve devir makinesi kullanılır.
3. Sonraki uygun durumda ilk kişisel eğitim ve üretici/aday seçimi açılır. Zorunlu karakter ölümü yapılmaz.
4. Gerçek bir adayla belge ve kabul öğretilir; temel talimat bir veya iki maddelidir.
5. Sonra yeni talimat, ilgili eski dosya ve iki vardiyalık iş görünür. Şüpheli aday, oyuncu kontrolleri tanıdıktan sonra gelir.

Bunlar öğretim sırasıdır, her biri zorunlu ayrı takvim günü değildir. Önceden bilen oyuncu rehberi atlayabilir;
oyunun normal açık seçenekleri keyfî tutorial kilitleriyle ortadan kaldırılmaz.

İlk oynanabilir kapsam: bir bölgenin mevcut baskınları, sekiz sakin, üç saha rolü, tek üretim cebi, bir eğitim cebi,
üç tarif, dört hammadde, vardiya makinesi, iki belge türü, bir masum belge kusuru ve tek gecikmeli sabotaj zinciri.
Karmaşık ilişki/kişisel ihtiyaç çubukları, oda yükseltmeleri ve yönetici halefliği eklenmez.

## 14. Denge hedefleri ve doğrulama

Hedefler deneme ölçütleridir: başlangıç birkaç kapanışlık erzak hareket alanı sağlar; tek ölüm toparlanma yolunu
kapatmaz; yalnız bekleyerek bütün kitler ve ustalıklar bedelsiz tamamlanmaz; tek bir üretim veya kabul politikası
her durumda üstün olmaz. Ekipman avantajı ile kişi ustalığı birlikte ölçülür; güçlü bir rol diğerlerini silmez.

Ölçülecekler: vardiya başına kart değiştirme sayısı, planlama süresi, boş kalan işler, üretim/eğitim seçimi, erzak
eğilimi, malzeme darboğazı, baskın başarı ve kalıcı kayıp oranı, yeni adayı reddetme/kısıtlama oranı, ipucunu anlayan
oyuncu oranı ve kayıptan sonra toparlanma süresi. Rutin vardiyada bütün kartlar yeniden taşınıyorsa arayüz başarısızdır.

Teknik doğrulama uygulama aşamasında gerekir: tek kişi/tek eşya sahipliği; etkili kişisel değerlerin taktik önizleme
ve gerçek çözümde eşleşmesi; rezervasyon/ilk ödeme/çok vardiya iş ilerlemesi; üretici kaybı/değişimi; eğitimde çift
deneyim olmaması; fiş öncesi/kutulama/kararma sonrası kayıt-devam; eski fiş ve tekrar tıklama; sabit ziyaretçi kimliği;
değişen talimatta eski dosya ve ölümden sonra olay gönderen kayıp kişi bulunmaması.

Simülasyon uygulanırken yalnız komutlar durumu değiştirir, RNG yalnız mevcut deterministik kaynak üzerinden tüketilir,
kayıt/replay bütün yeni eylemleri kapsar ve değişen kurallar için kayıt sürümü artırılır. Denge Balance'da, oyuncuya
görünen metinler TR/EN lokalizasyonda tutulur. Sahne/prefab ve serialized referans kuralları korunur. Bu belge yazımı
oyunu değiştirmediği için Unity derlemesi veya oyun testinin geçtiği iddia edilmez.
