# Vardiya gündemi — yönetim döngüsü taslağı

_2026-11-04 · Kullanıcının seçtiği yön: “Vardiya gündemi ana sistem olsun; operasyon hazırlığı ve topluluk kararları onun içinde yer alsın.”_

Bu belge bir tasarım önerisidir; oyun koduna uygulanmadı. Ana sistem yönü seçildi; aşağıdaki faaliyet bütçeleri,
bedeller, kapasite değerleri ve süreler ilk denge taslağıdır. Önceki mekanik incelemesindeki oda uzmanlaşması,
bu modelin ön koşulu değildir. Uygulanmış hâli anlatan OYUN-TASARIMI.md değiştirilmez.

**Sonraki tasarım önerisi:** Kullanıcının isimli sakinler, ortak sığınak/saha kadrosu, kalıcı ölüm ve gecikmeli
robot sızması fikri [Topluluk, kadro ve sızma taslağında](TOPLULUK-KADRO-VE-SIZMA-TASARIMI.md) ayrıntılandırıldı.
Bu yönde aşağıdaki anonim yedekler ve garanti uzman katılımı yeniden ele alınır; bir ana faaliyet + bir destek
emri bütçesi korunur. İki belge de henüz uygulanmış sistemi anlatmaz.

**Daha yeni pano önerisi:** Kullanıcının sekiz sakin, kişi/teçhizat kartları ve sürükle bırak görevlendirme fikri
[Kartlı vardiya panosu taslağında](KARTLI-VARDIYA-PANOSU-TASARIMI.md) ele alındı. O modelde bu belgedeki bir ana
faaliyet + bir destek sınırı yerine kişi-vardiya kapasitesi önerilir; üç kişi baskındayken kalanlar sığınakta
çalışabilir. Topluluk gündemi panodaki olay fişleriyle sürer. Bu, sonraki tasarım yönüdür; henüz kod değişikliği değildir.

## 1. Oyuncunun rolü

Oyuncu KYGA'nın sığınak yöneticisidir. Hazır bir sığınağın insanlarını, stoklarını ve saha ekibini günün ihtiyaçları
arasında paylaştırır. Soru: **Bugün neyi çözeceğim, kime ne söz vereceğim ve hangi riski taşıyacağım?**

Oda seviye yükseltme ve tek tek personel artırıp azaltma ana döngüden kalkar. Odalar fiziksel olarak kalır; görevli
insanların yaşadığı ve kararların sonuçlarının göründüğü yerler olur. Temel işleyiş otomatik, olağan dışı işler emirlidir.

Önemli mevcut temel korunur: manuel vardiya, harita ve sektör ilerlemesi, güven, YZ alarmı, kadro seçimi, yaralar,
kaçış ve kapıdaki insanlar. Yeni bir enerji, moral, nüfuz ya da yönetim puanı göstergesi eklenmez.

## 2. Karar bütçesi

Bir vardiyada **bir ana faaliyet ve bir destek emri** vardır. Destek zorunlu değildir. Bunlar süre/organizasyon
kapasitesini temsil eder; ayrıca biriktirilebilen kaynak değildir ve sonraki vardiyaya devretmez.

| İşlem | Bütçesi | Ne zaman sonuç verir? |
|---|---|---|
| Baskın | Ana faaliyet | Mevcut taktik sahnede; dönüşte operasyon raporu |
| Odaklı toparlanma | Ana faaliyet | Vardiya kapatılırken |
| Sivil destek | Ana faaliyet | Vardiya kapatılırken, belirli bir ihtiyaç için |
| Plaka hazırlığı | Destek, ayrıca malzeme | Baskın başlamadan hazırlanır, aynı baskında kullanılır |
| Dışarıdan yardım | Destek, ayrıca erzak | Plan kesinleşirken ilgili ziyaretçiye teslim edilir |
| Kayıt teyidi | Destek, ayrıca fon | Planlamada sonuç gelir; yalnız seçilen iddiayı doğrular |
| Belge/rapor okuma, gözetleme, görüşme | Faaliyet harcamaz | Bilgi verir; kendi başına kaynak veya iyileşme vermez |

Tek bir topluluk ihtiyacı için ana faaliyet ve destek emriyle iki ayrı ödül alınamaz. Dış yardım, sivil desteğin
daha küçük kapsamlı alternatifidir; bir ihtiyaç bir kez çözüldüğünde kapanır.

**Sakin vardiya** her zaman seçilebilir: ana faaliyet ve destek kullanılmadan şalter indirilebilir. Doğal iyileşme,
gelir, tüketim ve alarm yine işler. Çıkış yolu olmayan bir ekibi ücret ödemeye zorlamaz; ücretsiz özel iyileşme,
malzeme veya güven üretmez.

## 3. Gündem alanı

Masada üç fiş yuvası bulunur. Bu, üç eylem hakkı anlamına gelmez:

1. **Saha:** Mevcut sektör, bedel, kadro ve varsa süreli fırsat. Kalıcı görev fişi her zaman erişilebilirdir.
2. **Ekip / hazırlık:** Yaralar, yedek durumu ve o an anlamlı hazırlık seçenekleri. Sağlam ekibe sahte acil durum üretilmez.
3. **Topluluk:** Kapıdan gelen kişi veya içeriden yükselen bir talep. Ertelenen talep bu yuvada kalır.

Her vardiya üç yeni sorun üretilmez. Bazı vardiyalar yalnız saha ve ekip durumunu gösterir. Bekleyen işler saha
yuvasını doldurup baskın erişimini engellemez. İlk prototipte tek açık topluluk meselesi yeterlidir.

Tam modelde **bir iç talep bekleyebilir**; yeni iç talep eskisi çözülene/sonuçlanana kadar üretilmez. Zamanı gelen kapı
ziyareti topluluk yuvasını alır; iç talep, vadesi görünen kısa bir BEKLEYEN fişiyle arşivde erişilebilir kalır. Kartın
ekranda önde olmaması süresini durdurmaz. Kapı zincirleri klinik kartı açık tutularak geciktirilemez. Sonuç yaklaşınca
özet yeniden uyarır; gösterilmemiş yeni bir tehdide habersiz ceza eklenmez.

Fiş alanları: talep sahibi, somut ihtiyaç, kaynak bedeli, ana/destek kullanımı, bilinen etki ve vade. Sonucu belirsiz
bir hikâyede belirsizlik açıkça yazılır; kaynak bedeli kesin gösterilir. “Acil” işaretinin tek başına karar yönlendirmesi
yerine “V013 kapanışına kadar” veya “bir vardiya bekleyebilir” gibi anlaşılır vade bulunur.

## 4. Bir vardiyanın akışı

1. **Brifing:** Önceki sonuçlar gelir; gündem mevcut durumdan hazırlanır. Erzak dayanma süresi, ekip hazırlığı ve
   bu vardiya kapanırsa alarmın nereye ulaşacağı özetlenir.
2. **İnceleme:** Oyuncu fişleri, haritayı ve kadroyu okur; ziyaretçiye bakar ve konuşur. Ekranı açıp kapamak fişi değiştirmez.
   Kapıyı açmak gibi geri alınamaz fiziksel kararlar bu aşamada, baskın planı kilitlenmeden önce verilir. Ölümcül sonuçta
   henüz verilmemiş baskın emrinin bedeli kesilmez. Dışarıdan yardım kilitli servis gözünden verilebilir; kapıyı açmayı gerektirmez.
3. **Plan:** Ana faaliyeti ve isteğe bağlı desteği seçer. Henüz kesinleşmemiş faaliyeti değiştirmek bedelsizdir.
   Kaynaklar “ayrılacak” olarak gösterilir; önizleme ücret kesmez.
4. **Kesinleştirme:** Baskın seçildiyse BASKINA ÇIK kadroyu, desteği ve bedelleri kilitler. Destek koşulları yeniden
   kontrol edilir. Plaka aynı baskına takılır. Baskından sonra yeni destek seçilip geçmiş sonuç iyileştirilemez.
   Baskınsız plan ise vardiya şalterinin onayında kesinleşir.
5. **Faaliyet:** Baskın mevcut sahnede oynanır ve sonuç anında kaydedilir. Diğer ana faaliyetler şalterde işler.
6. **Kapanış:** Baskından dönüşte şalter hâlâ oyuncudadır; rapor incelenebilir, fakat ana faaliyet/destek yeniden
   kullanılamaz. Yeni kapı kararları da sonraki vardiyaya kadar kilitlidir; sonuçlar okunabilir. Şalter temel günlük
   işleyişi ve vade sonuçlarını bir kez çözer, sonraki vardiyayı açar.

Kayıt/devam, planlanmış seçim ile kesinleşmiş ve uygulanmış sonucu ayırmalıdır. Dönüşte aynı plaka, ücret veya yardım
ikinci kez işlenmez. Baskın başarısız olsa da ana faaliyet harcanmıştır.

## 5. Ana faaliyetler

### Baskın

Mevcut sektör sırası, 12 fonluk emir, kadro ve taktik kurallar ilk denemede korunur. Örnek hazırlık bir adet plakadır;
oyuncu isterse desteksiz çıkabilir. Normal niyet önizlemesi ve temel güvenlik bilgileri hazırlık satın alma şartına bağlanmaz.

### Odaklı toparlanma

Doğal dinlenmenin üstüne belirli bir uzmana ilave tedavi uygulanır. Örnek ilk ayar: **6 fon; seçilen ayaktaki uzmana ek
1 yara iyileşmesi veya seçilen yatan uzmanın süresinden ek 1 vardiya düşmesi**; baskın yapılmaz. İki fayda birlikte
verilmez. Düşmüş uzmanların doğal yatış sayacı işlemeye devam eder. Özel tedaviye para yetmemesi doğal iyileşmeyi
durdurmaz. Yeni ilaç kaynağı gerekmez.

İyileşme tavanı uzmanın tam canıdır. Önce doğal dinlenme/yatış, sonra hâlâ gerekli odaklı bakım uygulanır. Doğal
iyileşme yatışı zaten bitiriyorsa odaklı bakım ayrıca yara iyileştirmeye dönmez. Planlanan ek tedavi gereksiz olacaksa
kapanış ön kontrolü uyarır; oyuncu başka bir plan seçebilir. Temel dinlenme ve ek tedavi raporda ayrı gösterilir;
aynı yaraya iki kez fayda yazılmaz.

### Sivil destek

Topluluktaki belirli bir soruna daha kapsamlı müdahaledir. Örnek: baskında kararan kliniği onarmak için **8 fon ve
2 malzeme, tamamlanınca +2 güven**. O vardiya baskın yapılamaz; ekip doğal dinlenir, alarm normal ilerler.

Bu, her gün basılabilen bir güven satın alma düğmesi değildir. Açık bir vaka gerekir; aynı vaka tekrar ödül vermez.
Vardiya brifingi yalnız güveni doldurmak için her gün yeni hasar üretmez. Tavan yüzünden boşa gidecek fayda görünür olur.

## 6. Destek emirleri

İlk denge örnekleri:

| Destek | Bedel | İşlev ve sınır |
|---|---|---|
| Plaka hazırla | 4 malzeme | Baskındaki bir uzmana bir plaka; temel motorun ilk hasarı karşılama kuralı. Yalnız baskın planıyla seçilir. |
| Dışarıdan yardım | 4 erzak | İlgili kapı vakasında acil ihtiyacı karşılar; örneğin güven kaybını önler. Her vakada aynı sonucu vermek zorunda değil. |
| Kayıt teyidi | 2 fon | Bir sevk numarası, kurye veya iddia için bilgi sağlar. Ziyaretçiye genel “güvenli” damgası vermez. |

Teyit sonucu planlama sırasında alınır; bu destek o vardiya kullanılmış sayılır, sonuç beğenilmeyince plaka veya yardıma
çevrilemez. Kaynaklar ve destek hakkı bir kez gider. Bu açık istisna fişte “Teyit alındı, destek kullanıldı” diye görünür.
Diğer destekler son onaya kadar değişebilir; teyit kullanıldıysa başka destek seçilemez.

Alarm azaltan haberleşme desteği ilk pakette bulunmaz. Sonradan eklenirse olağan vardiyanın alarm artışını tamamen
silmemeli; bedelsiz güvenli bekleme geri gelmemeli. Oyuncu destek kullanmadan da temel görevleri çözebilmelidir.

## 7. Taleplerin vadesi ve sonuçları

| Talep türü | Örnek | Vade aşılınca |
|---|---|---|
| Fırsat | Kurye iki vardiya daha bekleyebilir | Fırsat gider; ayrıca güven/alarm cezası otomatik eklenmez |
| İhtiyaç | Bir ailenin erzak talebi | Önceden belirtilen tek sonuç ya da sonraki hikâye evresi |
| Kalıcı sorun | Hasarlı sivil klinik | Aynı kart sürer; belirtilen eşikte bir kez ağırlaşır |

“Ertele”, “Reddet” ve “Karşıla” ayrı kararlardır. Ertelemek bir süre ve mümkünse somut söz içerir. Tutulan söz güven
yaratabilir; tutulmayan sözün bedeli sözü verirken görünür. Aynı olaya erteleme cezası, kapı ret cezası ve süre aşımı
cezası birlikte yığılmaz. Her etki tek vaka kimliğine bağlıdır.

Plan onayında aynı anda kaynaklar, faaliyet hakları, vade ve olayın hâlâ geçerli olup olmadığı kontrol edilir. Kaynak
yetmiyorsa plan başlamaz; kısmi ücret kesilmez. Belirsiz hikâye sonuçları olabilir, ancak inceleme kurallarını doğru
uygulayan oyuncuya habersiz ölüm zarı atılmaz.

## 8. Topluluk ve kapı

Topluluğa birkaç tanıdık yüz yeterlidir; bütün sakinler için karakter ekranı gerekmez. Örneğin Nermin gündelik
erzak ihtiyacını, hekim yaralıları, Deniz hazırlık işlerini, telsizci saha haberlerini taşır. Bu roller şimdilik
öneridir; mevcut kart kişileri uygun olan yerde kullanılır.

Kapıdaki aile aynı anda ayrı bir gündem olayı değildir; topluluk fişindeki vakanın yüzüdür. “Dışarıdan yardım et”
aynı olayın destek çözümüdür; kapıdan ayrıca ikinci bedel/ödül üretilmez. Ertelenen dış ziyaretçiye geri geleceği tarih
verilebilir; kişi günlerce hareketsiz kapının önünde bekletilmez. İç talep bekliyorsa yeni kapı ziyareti öne gelir,
iç talep görünür bekleyenler satırına geçer; iki vakanın süreleri ayrı işlemeye devam eder. Kapı ziyareti vardiya
sonuna kadar çözümsüzse tek bir “açılmadı” sonucu alır; iç talep yüzünden zaman dışına çıkmaz.

Papers, Please benzeri belge karşılaştırması ayrı genişletmedir. İlk gündem denemesi mevcut gözetleme/interkom ve
kararlarla çalışabilir; belge sisteminin tamamlanmasını beklemek zorunda değildir.

## 9. Otomatik sığınak ve kaldırılan seviyelerin görevleri

**Otomatik kalanlar:** temel erzak toplama/tüketim, olağan vardiya geliri, doğal dinlenme ve yatış sayacı, stok takibi.
Personel artırıp azaltmak için oda oda dolaşılmaz. Plaka üretimi özel destek emrine taşınır; eski pasif plaka ve revir
birikimi aynı anda çalışıp ikinci kez fayda vermez.

| Eski bağımlılık | Yeni karşılık |
|---|---|
| Oda seviye/personel sayısı | Sabit hizmetler, seçilen faaliyet ve destek emri |
| Yaşam alanı seviyesiyle nüfus/erzak kapasitesi | Görünür sabit kapasite; doluyken insan kabulünün sonucu önceden açıklanır |
| Yaşam alanı seviyesiyle yedek uzman | Başlangıç yedeği ve belirli kurtarma/katılım hikâyeleri; temel toparlanma her zaman açık |
| Atölye seviye 2/3 ile Madenci/Sis uzmanı | Harekâtın 2. ve 5. özgür bölgesinde garanti katılım olayı; atölyeye bağlılık kalkar |
| Çalışan revir/telsiz gerektiren kapı seçenekleri | İlgili faaliyetin, desteğin, uygun kişinin veya kaynağın bulunması |
| Oda geliştirme giderleri | Tedavi, hazırlık, yardım ve tek seferlik hikâye işleri; yeni ekonomi ayrıca ölçülür |

Sabit kapasiteler ilk denemede ayrıca ayarlanır. Eski seviye 1 değerlerini sonsuza kadar korumak veya bütün sığınağı
eski seviye 3 çıktısıyla başlatmak doğrudan kabul edilmez. Kapasite doluyken görevden kurtarılan insan sessizce silinmez;
dış yerleşime yönlendirme gibi açıklanmış bir sonuç gerekir. İlk pakette yeni inşaat/kapasite ağacı kurulmaz.

Atölye harcamaları kalkarken malzeme ve fon arzı aynı bırakılıp dengeli sayılmaz. Doğal erzak toplama nüfusa göre
ayarlanmalı; hazırlık seçmek sığınağı kaçınılmaz açlığa, hiç hazırlık yapmamak da sürekli kaynak fazlasına götürmemeli.

## 10. Somut vardiya örneği

**V012:** 20 fon, 9 erzak, 5 malzeme; güven 7/20, alarm 7/10. Teknisyenin iki yarası var. İlk iki fiş ikmal baskınını
ve ekibin durumunu gösteriyor; topluluk fişinde yarın geri dönebilecek bir ailenin yardım talebi var.

| Plan | Tercih | Taşınan risk |
|---|---|---|
| Baskın + plaka | 12 fon ve 4 malzeme; plaka yaralı uzmana gider | Aileye verilen söz sonraki vardiyaya kalır; baskın başarısı kesin değil |
| Baskın + dış yardım | 12 fon ve 4 erzak; ailenin acil ihtiyacı çözülür | Kadro ek plakasız çıkar, erzak azalır; ikmal başarılı olursa stok toparlanır |
| Toparlanma + dış yardım | 6 fon ve 4 erzak; ekip baskına çıkmaz, odaklı tedavi ve yardım uygulanır | Sektör ilerlemez; rutin alarm ve tüketim sürer |

Bunlar aynı vardiyadaki örnek alternatiflerdir, üçünün toplamı değildir. Temel gelir/tüketim ve doğal iyileşme
kapanışta ayrıca bir kez işlenir. Tedavi hedefi doğal dinlenmeyle zaten tamamen iyileşecekse ön kontrol fazlalığı söyler;
oyuncu ücretli hızlandırmayı başka bir hedefte kullanabilir veya sakin vardiyaya dönebilir.

İyi bir gündem, bütün seçenekleri “doğru” diye işaretlemez; her seçimin neyi çözdüğünü ve hangi sorunu taşıdığını anlatır.

## 11. Görsel ve fiziksel sunum

- **Masanın solunda gündem panosu:** saha, ekip, topluluk fişleri; seçili ana faaliyet kaşesi. Fişler tek bakışta bedeli,
  vadesi ve çözeceği sorunu gösterir. Sadece sorun olan alan vurgulanır.
- **Ortada harita:** bölge/sektör kararı ve kadro raporu korunur; hazırlık seçimi aynı emir belgesine bağlanır.
- **Sığınak maketi:** insanlar ve sonuçlar görünür. Yaralı yatak, hazırlanan plaka, dağıtılan erzak, boşalan yer;
  “Geliştir +” ve oda personel düğmeleri kalkar.
- **Kapı:** topluluk fişindeki kişiyle somut görüşme. Gelen kişi, ilgili fişi öne çıkarır.
- **Vardiya şalteri:** kapanış öncesi plan ve vadesi dolacak meselelerin kısa özeti; sonuçlar mevcut raporla dosyalanır.

Hedef normal vardiyada birkaç anlamlı karar, ardından faaliyet ve sonuçtur. Her fiş ayrı onay penceresi açmaz. Rutin
vardiya fişleri kısa tutulur; hikâye yalnız önemli karar ve sonuçlarda genişler.

## 12. Açılma eğrisi ve uzun vadeli gelişim

1. İlk vardiya: yönlendirilen tek baskın; destek ve topluluk kotası anlatılmaz.
2. Sonraki vardiya: ilk yara üzerinden dinlenme/toparlanma; Nermin'le küçük yardım kararı.
3. İlk bölge boyunca: bir plaka desteği, erteleme ve vade öğretilir; üç fiş ancak bu işlemler tanındığında görünür.
4. İkinci özgür bölge: Madenci katılır, yeni kadro yaklaşımı açılır.
5. Beşinci özgür bölge: Sis uzmanı ve mevcut stratejilere yeni çözüm.

Kalıcı gelişim, kurtarılan insanlar, erişilen bölgeler, kazanılan bağlantılar ve yeni hazırlık seçenekleridir. Halef
yöneticiler sonraki genişletme olabilir; gündem sistemi onlara ihtiyaç duymadan çalışır. Ölüm zorunlu gelişim kapısı olmaz.

## 13. İlk prototip ve denge kapısı

**İlk kapsam:** bir bölge, üç fiş yuvası, baskın/toparlanma/sivil destek ana faaliyetleri, plaka/dış yardım destekleri,
tek ertelenebilir topluluk vakası. Teyit, belge karşılaştırması, yönetici özellikleri, yeni görev türleri ve kapsamlı
kalıcı ilişkiler sonraki aşamadır. Eski oda etkilerinin çift çalışması ilk prototipte bile engellenir.

**Davranış kontrolleri:** plan değiştirmenin ücretsizliği; kesinleşmiş desteğin değiştirilmemesi; başarısız baskında
faaliyetin geri gelmemesi; aynı vakanın tek bedel/sonucu; vade; baskından dönüş ve kayıt/devamda tekrar ödeme olmaması.

**Denge senaryoları:** sürekli baskın, sürekli sakin vardiya, sürekli sivil destek; iki başarısız baskından toparlanma;
Teknisyen ve yedeğin birlikte yatması; düşük erzak; destek için sıfır malzeme; güven/alarm eşiği; tam nüfus ve depo.

Ölçülecekler: seçim dağılımı, baskınlar arası vardiya, güven/alarm, stok fazlası/açlık, yatış, reddedilen talepler,
kaçırılan fırsatlar, karar verme süresi. Bir ana faaliyet her durumda üstünse veya iyi oynayan oyuncu çoğu vardiya yalnız
şaltere basıyorsa bütçe ve içerik yeniden ayarlanır. Önceki 489/489 teknik test ve eski bot ölçümleri bu tasarımın dengesini
kanıtlamaz.

## 14. Uygulama notu

Oyuna geçişte Core'a gündem vakaları, plan/destek kullanım durumu ve kaynak/vade çözümü eklenir. Yeni oyuncu eylemleri
komut olarak kaydedilip Replay'e bağlanır; tohumlu gündem yalnız vardiya sınırında bir kez üretilir. Ekranı, kaydı veya
dili yeniden açmak teklifleri değiştirmez. Önizleme simülasyonu yazmaz.

Plan seçimleri, bilginin alınmasıyla kesinleşen teyit gibi geri alınamaz işlemler, baskın emri ve vardiya kapanışı
ayrı durum geçişleridir. Kaydın hangi evrede olduğu, destek ücretinin ödendiği ve ana faaliyetin çözüldüğü korunur.
Eski oda/üretim ve komut kuralları değiştiği için kayıt sürümü artırılmalı; eski kayıtları başka sonuçla sessizce açmak
yerine uyumsuzluk açıkça bildirilmelidir. Denge Balance'da, metinler TR/EN Loc tablosunda tutulur. Sahne/prefab kaynakları
ve serialized bağlar korunur. Bu belge uygulama veya eski sığınak sistemini silme talimatı değildir.
