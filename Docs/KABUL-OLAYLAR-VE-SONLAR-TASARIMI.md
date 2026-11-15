# Aday seçimi, sığınak olayları ve yeni sonlar

_2026-11-13 · Tasarım belgesi. 2026-11-14: aşama 1–8 uygulandı (aşama 7 temizliği: §6 "Silinenler", §7 "Söküldü", §9; aşama 8 rehber, ikinci tur kararları ve denge: §8 "Ölçüm sonuçları", §9)._

**Durum:** Onaylanmış kararların ayrıntılı tasarımı. Yalnız yayındaki kartlı yol (`Balance.planningBoardEnabled = true`)
kapsamdadır. Uygulanmış oyunu anlatan [OYUN-TASARIMI.md](OYUN-TASARIMI.md) bu belgeyle değişmez; uygulama bittikçe
oraya işlenir. Bu belge [isimli topluluk ve sızma](TOPLULUK-KADRO-VE-SIZMA-TASARIMI.md) ile
[kartlı vardiya panosu](KARTLI-VARDIYA-PANOSU-TASARIMI.md) belgelerinin ilkelerini korur; kapıdaki inceleme
bölümlerinin yerine geçer.

Sayılar “(b)” ile işaretli başlangıç değerleriydi; aşama 8'de bot ölçümüyle altı alan değişti (§5 "Aşama 8 dengesi", §8
"Ölçüm sonuçları"), ötekiler başlangıç değerinde kaldı. Kod yolları `Assets/Corrigible/` altındadır.
Kodda ya da ölçümle doğrulanamayan varsayımlar bölüm 12'de toplanmıştır.

## 1. Amaç ve kapsam

Bugünkü oyunda üç sorun var: kapıdaki inceleme karar üretmiyor (en iyi politika “herkesi al”), sığınağın içinde
oyuncunun vereceği karar yok, kötü giden harekât bitmiyor ve boş vardiyalarla sürüyor. Bu belge üç değişikliği
birlikte tanımlar:

1. **Aday seçimi.** Kapıda test kalkar. Birkaç aday birlikte gelir, oyuncu en çok birini alır. Karar teşhis değil
   takastır: şüpheli aday daha çok şey getirir.
2. **Sığınak olayları.** Durumdan doğan, bedeli açık yazılı kararlar. Bugünkü sızma zinciri bu sisteme taşınır.
3. **Yeni sonlar.** Sığınak düşer, terk, son şalter ve zaferin üç tonu. Kötü giden harekât ya toparlanır ya biter.

| Kalkar | Yerine gelen | Aynen kalır |
|---|---|---|
| Belge, görüşme, nefes, dış kayıt incelemesi; talimat sürümleri | Aday kartındaki şüphe satırı (kademe) | İsimli kişi, tek sahipli teçhizat, kalıcı kayıp |
| Tek aday, dört karar (kabul, kısıtlı kabul, ret, dış yardım) | 2–3 adaylı grup, “birini al ya da hiçbirini” | Kurucular güvenilir insandır |
| Gözetmenli dosya incelemesi, elle kısıt, serbest yollama | Olay kararı: tecrit, yolla, görmezden gel | İz → ikinci iz → müdahale vardiyası → sabotaj sırası |
| Tek seferlik, sonuçsuz sabotaj | Sabotaj ve görmezden gelinen iz ifşa sayacını doldurur | Zafer `silme`, dağılma (güven 0), son kişinin kaybı |
| Açlığın yalnız üretimi durdurması | Aç vardiya sayacı, ayrılmalar, yarım tayın | Vardiya kapanışı, fiş, baskın, bölge harekâtı |

**Alınmayanlar (ertelendi):** isyan sonu, süre sınırı, robotu “düzeltme” kararı, dış kaydın gerçek teste çevrilmesi.

**İlkeler.**

- Kimlik üretim anında sabitlenir, sonradan değişmez. Kurucular ve ilk grubun adayları sızmacı olamaz. Her harekâtta
  içeride sızmacı bulunması şart değildir.
- Gizli kimlik sunuma sızmaz. Oyuncunun gördüğü hiçbir alan (profil, bedel, getiri, şüphe satırı, olay kâğıdı,
  seçeneklerin açıklığı, sayaçlar, izin **zamanı**) aynı kademedeki insan ile sızmacıyı kesin ayırmaz. Kesinlik
  yalnız sabotajdan, tecrit bulgusundan ve son rapordan gelir.
- Okunabilir uyarı ve müdahale fırsatı olmadan ölüm ya da oyun sonu olmaz. Her sonun sayacı odada ya da fişte görünür.
- Tek karar harekâtı bitirmez. Tek bilinçli istisna oyuncunun kendi çektiği son şalterdir. Her olayda son üretmeyen,
  her zaman açık en az bir seçenek vardır.
- Şüphe insanlık ölçümü değildir: kefilli aday da sızmacı çıkabilir, şüpheli adayların çoğu insandır.
- Fiş ve makineler Core'un yazdığını birebir yazar; gelecek gizli olayı açmaz.
- Ölüme otomatik büyük güven, erzak ya da fon cezası yığılmaz.
- Simülasyon kararı metne göre vermez; olay, aday ve son kimlikleri kısa ASCII dizgilerdir.

## 2. Aday seçimi

**Geliş.** Aday grupları `admissionKnockMin`–`admissionKnockMax` (3–4 (b); bugün 2–4) vardiyada bir gelir. Alan adları
korunur (bkz. bölüm 8). Aralık grubun **geldiği** kapanıştan sayılır; bekleme süresi (2) aralıktan (en az 3) kısa
olduğu için gruplar çakışmaz ve ritim 3–4 vardiyada kalır. İlk grup en erken 3. vardiyada gelir. Grup, vardiya
kapanışında üretilir ve yeni vardiyanın başında kapıda görünür.

**Grup boyu.** 2 ya da 3 aday (`candidateGroupMin`, `candidateGroupMax`; eşit olasılık (b)). Oyuncu en çok **birini**
alır ya da hiçbirini almaz. Alınmayanlar gider, geri çağrılamaz.

**Bekleme.** Grup, geldiği vardiya ve onu izleyen vardiya boyunca bekler (`candidateWaitShifts` = 2 (b)). İkinci
kapanışta karar yoksa grup gider; bu “hiçbiri” ile aynıdır, cezası yoktur. Bekleyen grup fiş basmayı engellemez;
son vardiyada fiş “KAPI · son vardiya” uyarısını yazar.

**Kart alanları.**

| Alan | İçerik | Kaynak |
|---|---|---|
| Ad, portre | İsim havuzundan | RNG'siz (aşağıda) |
| Profil | Can / hareket / güç | `CreateBoardResident`'ın dört profili |
| Ustalık | Bir konu ve kademesi | Yayındaki beş konu: `tek`, `emp`, `ana`, `craft`, `cook` |
| Bedel ya da getiri | Tablodan bir ya da iki özellik, etkisi sayıyla yazılı | Kademeye göre çekilir |
| Şüphe satırı | Tek cümle ve kademe işareti | Kademeye göre çekilir |

Ustalık konusu doğrudan beş konuluk aday listesinden eşit olasılıkla çekilir. Bugün aday konusu yedi konudan
(`PlanningBoardData.Subjects`) çekiliyor; ikisinin (`care`, `gather`) yayındaki panoda yuvası yok. Kurucuların
`index` ile konu alması değişmez.

**Adlar.** Bugünkü havuz 24 addır (`Core/Simulation.CommunityAdmissions.cs`), ikisi (“Eren”, “Suna”) kurucu listesinde
de vardır ve çakışma denetimi yalnız kabul edilmişlere bakar. Harekâtta 25–36 aday, acil gruplarla daha çoğu
üretilir. Yeni kural: havuz en az 60 ada çıkar, kurucu adları havuzdan çıkarılır; ad `GeneratedApplicants` sırasıyla
alınır; yaşayan ya da kayıtlı bir kişiyle ya da aynı gruptaki bir adayla çakışan ad numara alır.

**Bedel ve getiri tablosu.** Kimlikler kayıtta saklanır, adlar çevrilir. Durum alanının adı `Perks`'tir
(`ResidentState.Trait` profil metnidir, karışmasın).

| Kimlik | Kartta yazan | Etki (b) | Tür |
|---|---|---|---|
| `kit` | Kendi kitiyle gelir | Ustalık konusunun rol kiti üzerinde gelir; **kişiye bağlıdır** | Getiri |
| `zirh` | Zırhıyla gelir | Üzerinde bir zırh; **kişiye bağlıdır** | Getiri |
| `devre` | 2 devre getirir | Katıldıktan sonraki 2 kapanışta 1'er devre | Getiri |
| `erzak` | Erzak çuvalıyla gelir | Katıldıktan sonraki 3 kapanışta 2'şer erzak | Getiri |
| `usta` | Deneyimli | Ustalığı kademe 2'den başlar (6 puan, `boardExperiencedAt`) | Getiri |
| `saglam` | Dayanıklı | En yüksek can +1 | Getiri |
| `yarali` | Yaralı gelir | Canı 1; 2 vardiya yatar | Bedel |
| `obur` | Çift tayın yer | Kişi başı tüketimin iki katı (0,70) | Bedel |
| `acemi` | Eğitimsiz | Ustalık puanı 0; kademe 1 için 2 puan gerekir (`boardTrainedAt`) | Bedel |
| `yavas` | Aksak | Hareket −1 | Bedel |

Kurallar:

- **Getiri kişiye bağlıdır.** Kişiye bağlı kit ve zırh başkasına verilemez, sökülemez; kişi yollanır ya da ayrılırsa
  onunla gider (ölürse bugünkü kural: teçhizat kaybolur). `devre` ve `erzak` taksitle gelir; kişi sığınaktan çıkarsa
  kalan taksit gelmez. Böylece “al, getirisini depoya koy, yolla” yolu kapanır.
- `kit` saha dışı konuda çekilirse yerine `devre` verilir. `usta` ile `acemi` aynı kartta bulunmaz; ikincisi düşer.
- `yavas`, hareketi zaten 2 olan profilde (dört profilin ikisi hareketi `max(2, 3 − 1)` yapar) etkisizdir; o durumda
  RNG'siz `obur`'a çevrilir. Kartta hareket değeri bedel uygulanmış hâliyle yazılır.
- Kişiye bağlı `kit` ve `zirh`, depo tavanı (`boardEquipmentCapacity`) dolu olsa da verilir.
- Dönüşümler çekimden sonra, RNG'siz yapılır; çekim sayısı değişmez.

**Şüphe kademeleri.** Kademe görünür; sızmacı olasılığı yalnız kademeye bağlıdır.

| Kademe | Örnek şüphe satırları | Çıkma payı (b) | Sızmacı olasılığı (b) | Kartın değeri (b) |
|---|---|---|---|---|
| 0 · Kefilli | “kefili var: {kurucu adı}”, “KYGA kaydı tam” | %30 | %5 | Bir özellik; %35 bedel, %65 getiri |
| 1 · Kayıtsız | “kaydı yok”, “belgesi yanmış”, “adı listede iki kez geçiyor” | %40 | %20 | Bir getiri |
| 2 · Şüpheli | “işgal bölgesinden tek başına geldi”, “üç gün yürümüş, yarası yok” | %30 | %45 | İki ayrı getiri |

Bir grupta bütün adaylar aynı kademeden çıkarsa son adayın kademesi bir kaydırılır (0 → 1, 1 → 2, 2 → 1); özellikleri
yeni kademenin kuralına göre aynı çekimlerden yeniden okunur. Bu paylarla bir adayın sızmacı olma olasılığı
ortalama %23 dolayındadır (bugün %35). Şüphe satırının metni kademe içinde çekilir ve kimlikten bağımsızdır. Kefil adı
yaşayan bir kurucudan seçilir; kurucu kalmadıysa “KYGA kaydı tam” satırı kullanılır. Kefil yalnız metindir.

**Sızmacı gizli kalır.** Kapıda inceleme, araç ya da soru yoktur. Sızmacı, insanla aynı profil, ustalık ve özelliklerle
üretilir; getirdikleri gerçektir. Kimlik yalnız eylemiyle (bölüm 3) ya da son raporda ortaya çıkar.

**Dolu sığınak.** Canlı sayısı `boardResidentCapacity`'ye (10) ulaşmışsa aday almak için içeriden biri yollanır.
Seçim ve yollama tek komuttur: aday ile gidecek kişi birlikte verilir. Yollamanın bedeli her yerde aynıdır (bölüm 3,
“Yollama”). Yollanan kişi sızmacıysa bu bilinmez; son raporda görünür.

**Serbest yollama kalkar.** Bugünkü `DismissResident` bedelsizdir ve son canlı kişiyi bile yollayabilir. Komut
kaldırılır; kişi yalnız dolu sığınakta aday için ya da bir olay seçeneğiyle yollanır.

**Acil aday.** Kapanışta canlı sayısı `candidateEmergencyBelow`'un (3) altındaysa ve kapıda grup yoksa aynı kapanışta
acil grup üretilir (ertesi vardiya kapıdadır) ve sayı 3'e çıkana kadar her vardiya yenisi gelir. Acil grup hep 3
kişidir; adayların konuları sırayla eksik saha rollerine (`tek`, `emp`, `ana`) çevrilir ve her biri çekilen
özelliğine ek olarak kişiye bağlı `kit` taşır. Kademe ve sızmacı çekimleri olağan kurallarla yapılır: çaresiz
sığınak riski de alır. “En çok biri” kuralı acil grupta da geçerlidir. Acil geliş olağan aralık sayacını değiştirmez.

**Plan kilidi ve baskın emri.** Bugün baskın emri planı kilitler ve kilitli planda bütün kapı komutları reddedilir
(`AdmissionCommandCheck`); aday sessizce süre aşımına düşer. Yeni kural: aday seçimi ve “hiçbiri” kararı, baskın
tahtası açık değilken (`S.Raid == null`) ve fiş basılmamışken (`Board.Printed == false`) kilitli planda da geçerlidir.
Alınan kişi Boşta gelir, kilitli planı değiştirmez, o vardiyanın tüketimine katılır. Baskın sürerken ve fiş
basıldıktan sonra kapı kapalıdır. Baskın emri `S.Raid`'i hemen kurduğu için “kilitli plan ve baskın yok” durumu
baskının çözüldüğü anlamına gelir; bu durumda kadroyu koruyan ayrı bir kural gerekmez.

**Kısıtlı kabul yoktur.** Aday ya tam erişimle girer ya girmez. Kısıt ve tecrit yalnız olay kararlarıyla gelir.

## 3. Sığınak olayları

**Tetikleme.** Olaylar desteden çekilmez; her kapanışta durum koşulları sınanır.

1. Aynı anda en çok bir olay açıktır.
2. Olağan olaylar için aralık **açılıştan açılışa** sayılır: bir olay açıldıktan sonra `eventGapMin`–`eventGapMax`
   (2–3 (b)) vardiya yeni olağan olay açılmaz. Aralık, olay açılırken çekilir (açılışta iki çekim: aralık ve hedef).
3. **Zincir olayları** (`iz`, `ikinci_iz`, `sabotaj`) aralık bütçesinin dışındadır: aralığı beklemez ve yeniden
   başlatmaz. Yine de açık olay varken sıraya girer. Sıradaki bir izin sahibi için sızma saati durur; oyuncu
   görmediği bir izin bedelini ödemez.
4. Koşulu sağlanan birden çok olay varsa öncelik: zincir, hayatta kalma, kişiler, fırsatlar; aynı öncelikte tablo sırası.
5. Her olayın harekât başına tekrar sınırı ve iki çıkışı arasında bekleme süresi vardır (düz `Balance` alanları).
6. Açık olay karar verilmeden **fiş basılamaz**; fiş ön izlemesi ve vardiya şalteri gerekçeyi söyler. Bu denetim
   `PrintShift`'e özeldir, `BoardPlanCheck`'e eklenmez: açık olay baskın emrini engellemez. Olay kararı, aday seçimiyle
   aynı koşulda kilitli planda da verilebilir. Sessiz varsayılan seçenek yoktur.
7. Her seçeneğin anlık bedeli ve bilinen gecikmeli riski kâğıtta yazar. Olasılığın sayısı yazılmaz; riskin varlığı ve
   yönü yazılır. Karşılanamayan seçenek kapalı görünür, nedeni yazar.
8. **Hedefi giden olay kapanır.** Olayın kişisi karar verilmeden ölür ya da sığınaktan çıkarsa olay kendiliğinden
   kapanır, aralığı ve tekrar sınırını tüketmez; fiş kilidi kalkar.
9. **Güvenli seçenek şartı.** Güveni düşüren seçenek güven 1 iken, kişi çıkaran seçenek canlı sayısı 1 iken kapalıdır
   (bulguyla kişi çıkarabilen `tecrit` de).
   Gecikmeli güven kaybı güveni 1'in altına indirmez. Her olayda bu durumlarda da açık kalan bir seçenek vardır
   (tabloda “güvenli” sütunu).

Hedef 35–45 vardiyalık harekâtta 12–15 olaydır. Açılıştan açılışa 2–3 vardiya olağan olaylar için üst sınırı 35
vardiyada 14, 45 vardiyada 18 yapar; koşullar ve tekrar sınırları bunu hedefe indirir. Zincir olayları bunun üstüne
gelir (alınan aday başına en çok üç). Gerçek sayı bot ölçümüyle ayarlanır.

**Olay tablosu.** Bütün sayılar (b).

| # | Kimlik | Tetik | Seçenekler ve anlık bedel | Gecikmeli sonuç | Güvenli | Sınır |
|---|---|---|---|---|---|---|
| 1 | `iz` | Bir kişi iz bıraktı | **Tecrit:** kişi 3 vardiya çalışmaz, yer. **Yolla:** kişi ve kişiye bağlı teçhizatı gider, yolluk. **Görmezden gel:** ifşa +1 | Tecrit sonu bulgu kâğıdı. Görmezden gelinirse ikinci iz gelebilir | Görmezden gel | Kişi başı 1 |
| 2 | `ikinci_iz` | Aynı kişiden ikinci iz | `iz` ile aynı üç seçenek | Görmezden gelinirse ve kişi sızmacıysa müdahale vardiyasından sonra sabotaj | Görmezden gel | Kişi başı 1 |
| 3 | `sabotaj` | Sabotaj tamamlandı, kişi ortaya çıktı. Kayıp uygulanmıştır (3 erzak ya da 2 metal), ifşa +2 | **Sök:** kişi listeden çıkar. **Sorgula:** 3 vardiya yer tutmadan bekletilir (tecrit yeri istemez), sonra sökülür; ifşa −1 | Yok | Sök | Sızmacı başı 1 |
| 4 | `buyuk_hirsizlik` | Affedilmiş kişi yeniden çaldı; en çok 5 erzak gitti | **Yolla.** **Kısıtla:** 5 vardiya | Yok | Kısıtla | Kişi başı 1 |
| 5 | `kacak` | Açlık sayacı 2; bir kişi gitmek istiyor | **İkna et:** kişi kalır, sonraki vardiya görev alamaz. **Bırak:** kişi gider. **Kapıyı kilitle:** kişi 3 vardiya kısıtlı, güven −1 | Açlık sürerse terk kuralı işler | İkna et | 3; bekleme 3 |
| 6 | `erzak_azaliyor` | Erzak üç vardiyalık tüketimin altında, yarım tayın yok | **Yarım tayın:** 4 vardiya tüketim yarıya iner; dinlenme iyileştirmez, baskın kadrosu 1 can eksik çıkar (en az 1). **Tam tayın** | Yok | Tam tayın | 3; bekleme 5 |
| 7 | `hirsizlik` | Yarım tayın sürüyor ya da erzak üç vardiyalık tüketimin altında; **ve** erzak en az 2. 2 erzak eksilir | **Yolla.** **Affet:** bedel yok. **Kısıtla:** 3 vardiya | Affedilirse %35 olasılıkla 3–5 vardiya sonra `buyuk_hirsizlik` | Affet | 2; bekleme 6 |
| 8 | `agir_yarali` | Bir kişi bu kapanışta yatışa girdi ve kalan yatışı en az 2 | **Erzakla ayağa kaldır:** 3 erzak; kişi 2 canla kalkar. **Beklet** | Bekletilirse %25 olasılıkla yatış 1 vardiya uzar | Beklet | 3; bekleme 4 |
| 9 | `kavga` | Başarısız ya da ölümlü baskından dönen iki sağ kişi | **Ayır:** ikisi 2 vardiya aynı baskına giremez. **Karışma** | Karışılmazsa %50 olasılıkla biri 1 can kaybeder, 1 vardiya yatar | Karışma | 2; bekleme 6 |
| 10 | `anma` | Önceki vardiyada baskında ölüm | **Anma yap:** 2 erzak, güven +1. **İşe devam** | Devam edilirse %40 olasılıkla ölenin kadro arkadaşlarından biri bir vardiya görev alamaz | İşe devam | 3; bekleme 4 |
| 11 | `karartma` | YZ alarmı eşiğin 2 altına ulaştı | **Karart:** bu vardiya Üretim ve Mutfak çalışmaz, alarm −2. **Çalışmaya devam** | Yok | Çalışmaya devam | 3; bekleme 5 |
| 12 | `takas` | Erzak 8'in altında ve en az 2 metal; ya da bir saha kiti eksik ve erzak 12'nin üstünde | **Metal ver:** 2 metal → 5 erzak. **Erzak ver:** 4 erzak → 1 devre. **Geri çevir** | Yok | Geri çevir | 3; bekleme 5 |
| 13 | `cagri` | Başarılı baskından sonra, erzak 12'nin üstünde | **Erzak yolla:** 4 erzak, güven +2. **Yollama** | Yollanmazsa %50 olasılıkla güven −1 (1'in altına inmez) | Yollama | 2; bekleme 8 |
| 14 | `konvoy` | Güven 15 ve üstü | **Kabul et:** +5 erzak, alarm +2. **Geri çevir** | Yok | Geri çevir | 2; bekleme 8 |

Açıklamalar:

- İfşayı yalnız iki şey doldurur: görmezden gelinen iz ve tamamlanan sabotaj. `konvoy` ifşaya dokunmaz.
- Aç kapanışta stok 0'a kırpılır (`AdvancePlanningBoard`); bu yüzden `hirsizlik` yalnız çalınacak erzak varken
  tetiklenir, kayıp eldeki stokla sınırlıdır ve kâğıtta gerçek miktar yazar. `buyuk_hirsizlik` vadesinde stok 1'in
  altındaysa düşer, ertelenmez. `kacak` iknası erzak istemez.
- `buyuk_hirsizlik` zincir değildir: vadesi gelince olağan olay gibi aralığı bekler, açılınca aralığı yeniden başlatır ve
  “kişiler” önceliğinin başındadır. Bekleme sırasında kişi gitmişse ya da stok 1'in altına inmişse kayıt düşer; kişi
  yatıyor, kısıtlı ya da tecritteyse (çalamaz) kayıt bekler.
- Bugünkü denge ile yatış en çok 3 vardiyadır (`boardInjuryTurns`), saha ustalığı kademe 1'de 2; bu yüzden
  `agir_yarali` yatışın başladığı kapanışta tetiklenir.
- Yarım tayında tüketim, aşçı tasarrufu düşüldükten sonra çarpılır (8 kişi, kademe 1 aşçı: 2,1 → 1,05).
- `cagri`, bugün hiçbir yerde okunmayan “dış yardım” kararının yerini alır. Hiçbir olayda kişi doğrudan ölmez.

**Olayın kişisi.** Hedef olay açılırken belirlenir ve kâğıtta adıyla yazar. `hirsizlik` ve `kacak` için hedef uygun
kişiler arasından (yatan, kısıtlı ve tecritteki dışında; kimlik gözetilmez) tek çekimle seçilir. `agir_yarali` için
yatışa giren kişi, `kavga` için kadro sırasındaki ilk iki sağ kişi, `anma` için ölen kişi RNG'siz seçilir.

**Yollama.** Bir kişiyi kapıdan çıkarmanın bedeli her yerde aynıdır ve kâğıtta yazar: kişi, ustalığı, kişiye bağlı
teçhizatı ve kalan taksitleri gider; `dismissSupplies` (2 (b)) erzak yolluk verilir (stok azsa eldeki kadar). Kişiye
bağlı olmayan teçhizatı depoya döner. Canlı sayısı 1 iken yollama kapalıdır. Yollanan insansa oyun ayrıca
cezalandırmaz; son rapor söyler.

**Kişi durumları.**

| Durum | Nasıl gelir | Etki | Görünürlük |
|---|---|---|---|
| Kısıtlı | `hirsizlik`, `buyuk_hirsizlik`, `kacak` | Saha, Üretim ve Mutfak kapalı; Eğitim ve Boşta açık; kişiye bağlı olmayan teçhizatı depoya döner | Kişi kartında etiket ve kalan vardiya |
| Tecritte | `iz`, `ikinci_iz` | Hiçbir yuvaya konamaz; yer; sızma saati durur | Kişi kartında etiket ve kalan vardiya |
| İzli | Bir ya da iki iz | Kısıt getirmez | Dosyada iz sayısı ve yeri |
| Affedilmiş | `hirsizlik` → affet | İkinci kez affedilemez | Yalnız dosya notu |
| Ayrı tutulan çift | `kavga` → ayır | İki kişi aynı baskın kadrosunda olamaz | Yalnız baskın denetiminin gerekçesinde |

Bugünkü “tek gözetim yeri” ve gözetmen şartı kalkar: kısıtlı kişi gözetmensiz çalışır, birden çok kişi kısıtlı
olabilir. Tecrit yeri `isolationCapacity` = 1'dir (b); yer doluysa `tecrit` kapalıdır. Ortaya çıkmış sabotajcı (kararını bekleyen ya da
sorgulanan) yer tutmaz; `sorgula` tecrit yeri istemez (ikinci tur kararı, aşama 8).

**Sızma zinciri.** Sıra korunur, kararları olay kâğıdına taşınır; zamanlaması artık üretimde çekilen gizli
değerlere bağlıdır.

1. **Saat.** Kişinin sığınakta, ayakta ve tecrit dışında geçirdiği her kapanış saati bir ilerletir. **Görev fark
   etmez**: Boşta ya da Eğitimde tutulan kişinin saati de işler. Yalnız yatış, tecrit ve sıradaki açılmamış iz saati
   durdurur. (Bugün Boşta ve Eğitim erişim sayılmıyor; şüpheliyi Boşta tutmak sızmacıyı etkisiz kılıyor.)
2. **Birinci iz.** Saat, adayın üretiminde çekilen gizli gecikmeye (`traceDelayMin`–`traceDelayMax`, 2–6 (b)) ulaşınca
   dosyaya iz düşer ve `iz` olayı sıraya girer. Kâğıt yeri söyler (“üretim dolabında yetkisiz erişim”, “erzak sayımı
   tutmuyor”), nedeni söylemez.
3. **İkinci iz.** Birinci iz görmezden gelinirse, üretimde çekilen ikinci gecikmeden (`secondTraceGapMin`–`Max`, 1–3
   (b)) sonra `ikinci_iz`.
4. **Müdahale vardiyası.** İkinci iz kâğıdı kapandıktan sonra bir tam vardiya geçer.
5. **Sabotaj.** O da kullanılmazsa kayıp uygulanır (son görevi Üretim ya da sahaysa 2 metal, değilse 3 erzak), ifşa
   +2 olur, kişi ortaya çıkar ve `sabotaj` olayı açılır. Ortaya çıkan sızmacı her iki seçenekte de sığınaktan çıkar.
   Sökümden malzeme gelmez: sızmacı almak ödüllendirilmez.

**İnsan da iz bırakır.** İz kesin kanıt değildir, zamanı da değildir. Sonradan kabul edilen her insan için üretimde
iki gizli çekim daha yapılır: `humanTraceChance` (%20 (b)) olasılıkla o kişi de, sızmacıyla **aynı gecikme
dağılımından** çekilen bir vardiyada iz bırakır; iz bırakan insan `humanSecondTraceChance` (%25 (b)) olasılıkla
ikinci izi de bırakır. İnsan sabotaj yapmaz. Kurucular iz bırakmaz (sızmacı olamayacakları bilindiği için izleri
karar değil vergi olurdu). Bu değerlerle bir izin sızmacıdan gelme olasılığı kademe 0'da yaklaşık %21, kademe 1'de
%56, kademe 2'de %80'dir; iki izde kademe 2 için yaklaşık %94. Rehberde söylenen kural: **bir iz şüphedir, iki iz
güçlü kanıttır, sabotaj kesindir.**

**Tecrit.** Tecrit bir test değil, bir önlemdir: kişi `isolationShifts` (3) vardiya çalışmaz, yer ve saati durur.
Süre dolunca bulgu kâğıdı gelir (olay sayılmaz, karar istemez). Kapanışta tek çekim yapılır: kişi sızmacıysa
`isolationFindChance` (tek izde %60, iki izde %85 (b)) olasılıkla bulgu çıkar ve kişi sökülür; değilse kâğıt “bulgu
yok” der ve kişi Boşta döner. İnsan için sonuç her zaman “bulgu yok”tur. “Bulgu yok” kimseyi temize çıkarmaz: izler
dosyada kalır, sızmacının saati kaldığı yerden sürer. Aynı iz için ikinci kez tecrit yoktur; ikinci izde yeniden
seçilebilir. Bedel: üç kişi-vardiya, tecrit süresince tüketim ve tek tecrit yeri.

**İfşa neden kimliği sızdırmaz.** Görmezden gelinen her iz, sahibi kim olursa olsun ifşayı 1 artırır: araştırılmamış
iz bir güvenlik açığıdır. Sabotajın +2'si kimliği zaten açık eden olaydan gelir.

**Adalet ilkesi.** (a) Oyuncuya zarar veren her gecikmeli sonucun riski, kararın verildiği kâğıtta yazılıdır. (b) Büyük
kayıptan (sabotaj, sığınağın düşmesi, terk) önce en az iki görünür uyarı ve en az bir karar vardiyası vardır. (c) Aynı
tohumda **aynı komut geçmişi** aynı sonucu verir; olasılıklı sonuçlar kayıttan yeniden oynatmada değişmez. (d) Hiçbir
seçenek gizli kimliğe göre farklı metin, bedel ya da kapalılık göstermez. (e) Yanlış kişiyi yollamanın bedeli o
kişinin kendisi ve yolluktur.

## 4. Sonlar

Var olan üç sonun kimliği ve koşulu değişmez: `silme` (bütün bölgeler özgür), `dagilma` (güven 0), `topluluk`
(canlı kimse kalmadı).

| Son | Kimlik | Kesin koşul | Sayaç (b) | Önceden nasıl görünür |
|---|---|---|---|---|
| Sığınak düşer | `dusus` | İfşa eşiğe ulaştıktan sonra uyarı süresinin sonundaki kapanışta hâlâ eşikte (sabotajla varılan eşikte bir, kararla varılanda `exposureDecisionWarnShifts` = 2 vardiya) | `Exposure`, 0'dan; eşik `exposureThreshold` = 6 | Odada altı lambalı İFŞA göstergesi; 2'de ve 4'te teleks; dolunca “KONUM ÇÖZÜLDÜ” ve kalan vardiya |
| Terk | `terk` | En az bir kişi açlıktan ayrılmış ve canlı sayısı, kapıda acil grup bulunan üst üste 3 vardiyanın kapanışında 3'ün altında | `HungryStreak`, `UnderstaffedStreak` | ERZAK sayacında “AÇ n”; `kacak` olayı; fişte sıradaki ayrılacak kişinin adı ve “SIĞINAK EKSİK n/3” |
| Son şalter | `salter` | Oyuncu şalteri çeker ve çekilme emrini damgalar | Yok | Şalter odada görünür; onay kâğıdı kimlerin çıkacağını yazar |
| Zaferin tonu | `silme` (değişmez) | Zafer anındaki kayıt | Yok | Oyun içinde gösterilmez; yalnız son raporda |

**Sığınak düşer.** İfşa kaynakları: görmezden gelinen iz +1, tamamlanan sabotaj +2. Azaltan tek şey ortaya çıkan
sabotajcının sorgusudur (−1). Sayaç kendiliğinden azalmaz. Tek bir sızmacıyı baştan sona görmezden gelmek 4 eder;
sığınağın düşmesi için ikinci bir ihmal gerekir. **Ara bedel:** ifşa `exposureAlarmAt` (3 (b)) ve üstündeyken YZ
alarmı vardiyada `exposureAlarmBonus` (0,5 (b)) fazla artar; böylece “görmezden gel” eşikten uzakken de bedelsiz
değildir. İfşa eşiğe ulaştığı kapanışta harekât bitmez: teleks ve gösterge konumun çözüldüğünü söyler. Eşiğe kapanışta
açılan bir sabotajla varıldıysa oyuncunun bir vardiyası kalır; vardiya içindeki bir kararla (görmezden gelinen iz) varıldıysa
uyarı `exposureDecisionWarnShifts` (2 (b)) vardiya sürer ve sığınak ikinci kapanışta düşer (ikinci tur kararı; süre
`Shelter.FallGrace`'te, okuyucu `FallShiftsLeft`; uyarı sürerken gelen sabotaj süreyi değiştirmez). Uyarı vardiyalarında son
baskınla zafer alınabilir, sorguyla ifşa eşiğin altına indirilebilir (açık bir sabotaj kâğıdı gerekir, tecrit yeri gerekmez;
uyarı ve ek süre o anda kalkar) ya da son şalter çekilebilir. Uyarı süresinin sonundaki kapanışta uyarı sürüyorsa `dusus`
gelir. Sayacın tavanı yoktur: eşikteki ifşaya gelen sabotaj da +2 yazar. Eşiğe vardıran “görmezden gel” kâğıdı bunu
bedel satırında söyler; ikinci izin risk satırı sabotajın ifşa bedelini yazar. Son
rapor: başlık “SIĞINAK DÜŞTÜ”, neden satırı ifşayı dolduran kararları sırayla sayar, içeride kalanların adları ve
kurtarılan bölge sayısı yazılır.

**Terk.** Aç kapanış bugünkü tanımla sayılır (`Starving`). `HungryStreak` her aç kapanışta 1 artar, tok kapanışta
**1 azalır** (sıfırlanmaz). Sayaç `desertStreak` (3) ve üstündeyken her aç kapanışta bir kişi ayrılır. Sıra
RNG'sizdir ve sıradaki kişi fişte adıyla yazar:

1. Önce kurucu olmayanlar (en son katılandan başlayarak), sonra kurucular (kimlik sırasının tersiyle).
2. Kendi rol kitini taşıyan saha üyeleri her iki grupta en sona kalır: açlık önce baskın kadrosunu sökmez.
3. Yatan kişi ve o vardiya katılmış kişi ayrılmaz.
4. Ayrılma canlı sayısını 1'in altına indirmez; son kişi açlıktan gitmez. Böylece açlıkla boşalan sığınak `topluluk`
   değil `terk` ile biter.

Ayrılanın kişiye bağlı olmayan teçhizatı depoda kalır, dosyasına “açlıktan ayrıldı” yazılır. Sızmacı da herkes gibi
ayrılır. Canlı sayısı 3'ün altına inince aynı kapanışta acil grup üretilir. `UnderstaffedStreak`, son kez canlı ≥ 3 ile
kapandığından beri en az bir ayrılma yaşanmış sığınakta (`DesertedSinceFull`), **vardiya boyunca kapıda acil grup beklemişken** canlı sayısı 3'ün altında kapanan her vardiyada
1 artar, 3 ve üstüyle kapanan vardiyada sıfırlanır; `abandonUnderstaffedShifts` (3) olunca `terk` gelir. Sayım grup
kapıya geldikten sonra başladığı için oyuncunun üç alım fırsatı vardır. Yalnız ölümlerle 3'ün altına inen, hiç
ayrılma yaşamamış sığınakta bu son işlemez; orada acil adaylar ve son şalter vardır. Son rapor: başlık “SIĞINAK
BOŞALDI”, ayrılanların adları ve vardiyaları, kalanlar.

**Son şalter.** Oyuncunun kendi eliyle verdiği çekilme kararıdır. Koşullar: harekât sürüyor, baskın tahtası açık
değil, en az bir kişi canlı, vardiya en az `lastSwitchMinShift` (5 (b)). Kilitli planda ve fiş basıldıktan sonra da
çekilebilir. Odadaki yeri: arka duvarda, kapaklı ve mühürlü ayrı bir şalter (vardiya şalteriyle karışmayacak
biçimde). Onay üç adımdır: kapağı kaldır, kolu indir, masaya düşen “ÇEKİLME EMRİ” kâğıdını damgala. Kâğıt çıkacak
kişileri, bırakılacak bölgeleri ve “bu karar geri alınmaz” satırını yazar; damgadan önce kol geri kaldırılabilir.
Komut yalnız damgayla ve bir kez işlenir.

Son rapor çekilmenin **hangi durumda** yapıldığını yazar; son kimliği tektir, çeşit türetilir:

| Çeşit | Koşul | Başlık ve damga | Kayıt ve başarım |
|---|---|---|---|
| Düzenli çekilme | Aşağıdakilerin hiçbiri yok | “ÇEKİLME” · ÇEKİLDİ | Yenilgi sayılmaz |
| Tahliye | Çekildiği anda “konum çözüldü” uyarısı açık, eksik kadro sayacı işliyor, aç sığınaktan ayrılmalar sürüyor (son kapanış aç, açlık sayacı ayrılma eşiğinde ve sıradaki ayrılacak kişi belli; `aclik`, ikinci tur kararı) ya da güven 2'nin altında | “TAHLİYE” ve nedeni (“konum çözülmüştü”, “açlık sürüyordu” …) · TAHLİYE | Yenilgi sayılır |

Böylece son şalter, uyarısı verilmiş bir yenilgiyi temiz bir sonuca çevirmez. İki raporda da kurtarılanların adları
(yatanlar “sedyeyle” notuyla), özgür bırakılan bölge ve sektör sayısı ve kayıplar yazılır; kurtarılanlar arasında
ortaya çıkmamış sızmacı varsa rapor ayrı satırda söyler.

**Zaferin tonları.** Kimlik `silme` kalır; ton zafer anındaki kayıttan türetilir ve oyun sürerken okunamaz. Ölçütler
sırayla sınanır ve rapor her ölçütü satır satır “tuttu / tutmadı” diye yazar.

| Ton | Ölçüt (b) | Raporda |
|---|---|---|
| Gölgeli | Zafer anında sığınakta canlı, ortaya çıkmamış ve **en az bir izi görmezden gelinmiş** (`victoryShadowIgnoredTraces`) bir sızmacı var; araştırılmış (tecrit, bulgu yok) iz gölgelemez (ikinci tur kararı) | “Son sunucu düştü. İçeride {ad} hâlâ dinliyordu.” Adı, kabul vardiyası, görmezden gelinen izleri |
| Temiz | Gölgeli değil; ölüm en çok vardiya ÷ `victoryCleanDeathShifts` (15; taban `victoryCleanMaxDeaths` 0; 30–44. vardiyada 2, 45–59'da 3: harekât uzunluğuyla ölçeklenir, ikinci tur kararı); açlıktan ayrılan yok; ifşa 3'ün altında; harekât boyunca güven 5'in altına inmedi | Sağ kurucuların sayısı ve adları |
| Bedelli | Diğer bütün zaferler | Kayıpların adları, ayrılanlar, ifşa |

Hiç iz bırakmamış sızmacı (ör. son vardiyalarda alınmış) tonu gölgelemez: oyuncunun okuyabileceği bir uyarı
olmamıştır; rapor onu yalnız “kapı” bölümünde söyler. Her üç tonda “kapı” bölümü aynı verileri yazar: alınan aday
sayısı, yollanan kişi sayısı ve kaçının insan olduğu, geri çevrilen aday sayısı ve kaçının sızmacı olduğu.

**Öncelik.** Aynı kapanışta birden çok son koşulu sağlanırsa sıra: `topluluk`, `dusus`, `terk`; sonra alarm adımından
doğabilecek `dagilma`. Baskın çözümünde bugünkü sıra korunur (kadro dönüşü zaferden önce işlenir). `salter` komut
anında işlenir.

## 5. Denge

**Yeni `Balance` alanları.** Hepsi (b). `Balance.Clone()` sığ kopyadır (`MemberwiseClone`) ve sınıfta bugün liste
yoktur; bu yüzden olay sınırları dahil **her şey düz alandır**, liste ya da tablo eklenmez.

| Alan | Varsayılan | Anlam |
|---|---|---|
| `admissionKnockMin`, `admissionKnockMax` | 3, 4 | Grup aralığı (ad korunur; bugün 2, 4) |
| `candidateGroupMin`, `candidateGroupMax` | 2, 3 | Grup boyu |
| `candidateWaitShifts` | 2 | Grubun beklediği vardiya |
| `candidateEmergencyBelow` | 3 | Bu sayının altında acil grup gelir |
| `candidateTier0Share`, `candidateTier2Share` | 0,30; 0,30 | Kademe payları (kalan kademe 1) |
| `candidateTier0Robot`, `candidateTier1Robot`, `candidateTier2Robot` | 0,05; 0,20; 0,45 | Kademeye göre sızmacı olasılığı |
| `candidateTier0CostChance` | 0,35 | Kademe 0'da özelliğin bedel olma olasılığı |
| `perkCircuits`, `perkCircuitShifts` | 2; 2 | `devre` toplamı ve taksit sayısı |
| `perkSupplies`, `perkSupplyShifts` | 6; 3 | `erzak` toplamı ve taksit sayısı |
| `perkWoundedShifts`, `perkDoubleRationFactor`, `perkSturdyHp`, `perkSlowMove` | 2; 2; 1; 1 | `yarali`, `obur`, `saglam`, `yavas` |
| `dismissSupplies` | 2 | Yolluk |
| `traceDelayMin`, `traceDelayMax` | 2, 6 | Birinci iz gecikmesi (saat vardiyası) |
| `secondTraceGapMin`, `secondTraceGapMax` | 1, 3 | İkinci iz gecikmesi |
| `humanTraceChance`, `humanSecondTraceChance` | 0,20; 0,25 | Sonradan gelen insanın iz ve ikinci iz olasılığı |
| `isolationShifts`, `isolationCapacity` | 3, 1 | Tecrit süresi ve yeri |
| `isolationFindChance`, `isolationFindChanceTwo` | 0,60; 0,85 | Tecritte sızmacının bulunma olasılığı (bir iz, iki iz) |
| `shelterEventsEnabled` | true | Olayları kapatır (testler ve ölçüm için) |
| `eventGapMin`, `eventGapMax` | 2, 3 (aşama 8'de 2, 2 denendi; sunum kuralıyla olay sayısı bandı aştı, 3'e dönüldü) | Olağan olaylarda açılıştan açılışa aralık |
| `event<Ad>Max`, `event<Ad>Cooldown` | Tablodaki “Sınır” | Her olağan olay için tekrar sınırı ve bekleme (11 çift) |
| `eventRestrictShifts`, `eventRestrictLongShifts` | 3, 5 | Olay kısıtı süreleri |
| `exposureThreshold` | 6 | İfşa eşiği |
| `exposureIgnoredTrace`, `exposureSabotage`, `exposureInterrogation` | 1, 2, −1 | İfşa değişimleri |
| `exposureWarnAt1`, `exposureWarnAt2` | 2, 4 | Teleks uyarı eşikleri |
| `exposureAlarmAt`, `exposureAlarmBonus` | 3; 0,5 | İfşanın alarm ara bedeli |
| `desertStreak`, `abandonUnderstaffedShifts` | 3, 3 | Ayrılmaların başladığı açlık sayacı; `terk` için eksik kadrolu vardiya |
| `lastSwitchMinShift`, `evacuationTrustBelow` | 5; 2 | Son şalterin en erken vardiyası; tahliye sayılma eşiği |
| `rationWarnShifts` | 3 | `erzak_azaliyor` ve `hirsizlik` eşiği (vardiyalık tüketim) |
| `halfRationShifts`, `halfRationFactor`, `halfRationRaidHp` | 4; 0,5; 1 | Yarım tayın süresi, çarpanı, baskın can kaybı |
| `theftSupplies`, `bigTheftSupplies`, `bigTheftChance`, `bigTheftDelayMin`, `bigTheftDelayMax` | 2; 5; 0,35; 3; 5 | Hırsızlık zinciri |
| `reviveSupplies`, `reviveHp`, `woundWorsenChance` | 3; 2; 0,25 | `agir_yarali` |
| `feudShifts`, `feudInjuryChance` | 2; 0,50 | `kavga` |
| `memorialSupplies`, `memorialTrust`, `griefChance` | 2; 1; 0,40 | `anma` |
| `blackoutAlarm`, `blackoutAlarmMargin` | 2; 2 | `karartma` |
| `tradeMetal`, `tradeMetalSupplies`, `tradeSupplies`, `tradeCircuits`, `tradeLowSupplies`, `tradeHighSupplies` | 2; 5; 4; 1; 8; 12 | `takas` |
| `callSupplies`, `callTrust`, `callRefuseChance` | 4; 2; 0,50 | `cagri` |
| `convoySupplies`, `convoyTrustAt`, `convoyAlarm` | 5; 15; 2 | `konvoy` |
| `victoryCleanMaxDeaths`, `victoryCleanDeathShifts`, `victoryCleanMaxExposure`, `victoryCleanMinTrust` | 0; 15; 2; 5 | Temiz zafer ölçütleri (ölüm sınırı taban + her `victoryCleanDeathShifts` vardiyada +1; 0: sabit) |
| `victoryShadowIgnoredTraces` | 1 | Gölgeli zafer: içeride en az bu kadar izi görmezden gelinmiş, ortaya çıkmamış sızmacı |
| `exposureDecisionWarnShifts` | 2 | Kararla varılan ifşa eşiğinde uyarının süresi (sabotajla varılanda 1) |

**Kalan alanlar:** `admissionKnockMin/Max`, `admissionSabotageRations`, `admissionSabotageMaterials`. **Kalkan alanlar
(aşama 7'de kalktı; `BalanceConfig.asset`'ten de):** `admissionContactAdvance`, `admissionRobotChance`, `admissionPaperFaultChance`, `admissionAidRations`,
`admissionRestrictedShifts`, `admissionSettleShifts`, `admissionClueGap`, `admissionSabotageRestrictedScale`,
`admissionProtocolInterval`.

**Aşama 8 dengesi (2026-11-14, bot ölçümü; §8).** `liberationServerExtraTurns` 1 → 2, `liberationTier3At` 6 → 7,
`liberationTier4At` 10 → 12 (12 bölgelik harekâtta 4. kademe gelmez), `liberationTrustPerRegion` 3 → 4,
`boardSuppliesPerResident` 0,35 → 0,40 (ölçümün önerisi 0,575 toplayıcılı botla yapılmıştı; §8 "Ölçüm sonuçları"),
`boardLootSupplies` 6 → 4. `eventGapMax` 3'te kaldı (inceleme sonrası: 2 toplayıcılı botun kısa harekâtına göre seçilmişti;
sunumun kuralıyla olay/harekât 16,6, 3 ile 14,7). `Balance.cs` ve `BalanceConfig.asset` aynı; kayıt sürümü 17 kalır (denge
kayda yazılır, yalnız varsayılan değişti).

**Erzak hesabı (aşama 8 dengesiyle).** Sekiz kişi ve kademe 1 aşçıyla vardiya tüketimi 8 × 0,40 − 0,7 = 2,5 (önce 2,1).
Yayındaki panoda İkmal ve Bakım yuvası yoktur (soru 12; sunum bu görevleri Boşta'ya indirir), toplayıcı katkısı yok: 24 erzak
baskınsız yedinci kapanışta 6,8'e iner ve `erzak_azaliyor` açılır (tohum 79'da V.008). Bir ikmal baskını 4 erzak getirir (`boardLootSupplies`,
önce 6). `erzak` getirisi (3 × 2) iki buçuk vardiyaya yakındır; `obur` vardiyada 0,40 ekler; onuncu kişiyle tüketim 3,3'e
çıkar; yarım tayın dört vardiyada yaklaşık 5 erzak kazandırır.

**Açlık kolunun yeri.** (Aşama 8 öncesi sayılarla yazıldı; aşama 8 dengesiyle iyi politikada `erzak_azaliyor` harekâtların
yarısında görülür, §8.) Bugünkü sayılarla iyi giden harekâtta erzak hiç baskı olmaz (40 vardiyada tüketim yaklaşık
84, yalnız ikmal sektörlerinden gelir 144 ve başlangıç 24); baskın yapamayan oyuncu ise yaklaşık 12. vardiyadan sonra
aç kalır ve aç kapanışta üretim durduğu için eksik kiti üretemez. Yani `erzak_azaliyor`, `hirsizlik`, `kacak` ve
`terk` bugünkü dengeyle yalnız zaten kaybeden oyuncuyu bulur. Bu tasarım iki şeyi sağlar, üçüncüsünü karara bırakır:

- **Bitiş:** kilide giren harekât boş vardiyalarla sürmez; `terk` ile biter ya da oyuncu şalteri çeker.
- **Çıkışlar:** kişiye bağlı kitle gelen adaylar (özellikle acil grup), `takas`, yarım tayın.
- **Karar bekleyen:** erzakın iyi giden harekâtta da baskı olması için ganimet ya da tüketim sayısının değişmesi; ve
  kit kilidini gevşeten iki küçük kural (bölüm 11, soru 1 ve 2).

## 6. Core tasarımı

**Durum alanları.** Hepsi `Corrigible.Core` içinde, motor bağımsız; sayılar `int` ya da `double`. Projede
`InternalsVisibleTo` yoktur; gizliliğin tek koruması erişim düzeyidir. “Erişim” sütunu bu yüzden bağlayıcıdır.

| Yer | Alan | Erişim |
|---|---|---|
| `CommunityAdmissionState` | `Group` (aday listesi), `GroupWaitingUntil`, `GroupEmergency` | public |
| | `Passed` (geri çevrilenler): ad, kademe, vardiya | public |
| | `Passed` içindeki kimlik | internal |
| | `Candidate` (tekil) | aşama 7'de silindi |
| Aday (`CommunityApplicant`) | `Tier`, `SuspicionKey`, `SuspicionArg`, `Perks`, `Portrait` | public |
| `AdmissionIdentity` | `Robot`, `TraceDelay`, `SecondTraceGap`, `HumanTrace`, `HumanSecondTrace` | internal |
| `ResidentAdmissionFile` | `Tier`, `SuspicionKey`, `Perks`, `Pardoned`, `IsolatedUntil`, `RestrictedUntil`, `DepartReason`, `DepartShift`, `ClueCount`, `TraceAreas`, `Founder`, `Departed` | public |
| | `Clock` (saat), `Sabotaged`, `IsolationFound` | internal |
| `ResidentState` | `Isolated`, `Restricted`, `BoundKit`/`BoundArmor` işareti (teçhizat kaydında `Bound`) | public |
| `PlanningBoardState` | `ShelterEvents` (`ShelterEventState`) | — |
| `ShelterEventState` | `Pending` (açık olay), `NextEventShift`, `Counts`, `LastShift`, `Log`, `Feuds` | public |
| | `Queue` (sıradaki zincir olayları), `Delayed` (vadeli sonuçlar ve çekilmiş değerleri) | internal |
| Sığınak durumu | `Exposure`, `FallPending`, `HungryStreak`, `UnderstaffedStreak`, `DesertedSinceFull`, `Deserted`, `HalfRationUntil`, `MinTrust`, `EmergencyDoorShift`, `SwitchKind`, `SwitchReasons`, `SwitchShift` | public |

Adlar: `ShelterEvents` (var olan `CommunityAdmissionState.Events` ve `WorldState.Events` ile karışmasın), `Perks`
(`ResidentState.Trait` ile karışmasın). `ShelterEvents.Log` son rapor içindir; kimlik ve sayı tutar, metin tutmaz.
Eski `Admission.Events` listesi (`Loc.N` anahtarı saklıyordu) aşama 7'de silindi; kişi dosyasının KARAR satırı `AcceptedShift`'ten
kurulur.

Son raporun ihtiyacı olan gizli bilgi için `Simulation` türetilmiş değerler verir; bunlar **yalnız son varken** dolu
döner, harekât sürerken `null`: `VictoryTone`, `EndReport` (kapı sayımları, ortaya çıkmamış sızmacılar, yollanan
insanlar, çekilme çeşidi).

**Komutlar.** Her biri bir denetim metodu (`…Check`) ve bir eylem metodu taşır; başarıda `Rec(...)` ile yazılır.
`Replay.Apply` (`Core/GameRecord.cs`) karşılığı eklenir.

| Metot | Argümanlar | `Rec` adı | `Replay.Apply` karşılığı |
|---|---|---|---|
| `ChooseCandidate(candidateId, replaceId)` | Aday kimliği; yer açmak için yollanacak kişi ya da boş | `candidate_choose`, a, b | `sim.ChooseCandidate(c.a, c.b)` |
| `PassCandidates()` | Yok | `candidate_pass` | `sim.PassCandidates()` |
| `DecideShelterEvent(optionId)` | Seçenek kimliği (`tecrit`, `yolla`, `yoksay`, `affet`, …) | `event_decide`, a | `sim.DecideShelterEvent(c.a)` |
| `PullLastSwitch()` | Yok | `last_switch` | `sim.PullLastSwitch()` |

- Olay kararı hedef kişi almaz: hedef olay açılırken Core'da belirlenir.
- Ortak denetim: son varken ve baskın sürerken ret; kilitli planda fiş basılmamışsa kabul. `PullLastSwitch` fiş
  basıldıktan sonra da geçerlidir.
- `replaceId` boşluğu `string.IsNullOrEmpty` ile sınanır: kayıt `JsonUtility` ile yazılır ve `null` dizgi `""`
  olarak geri gelir (`EquipResident`'taki kalıp).
- Dolu sığınakta seçim yalnız geçerli `replaceId` ile geçer; boş yer varken `replaceId` verilirse reddedilir.

**Silinenler (aşama 7, 2026-11-14).** `InspectApplicant`, `DecideApplicant`, `ReviewResident`, `RestrictResident`,
`DismissResident` ve bunların denetim, ön izleme ve bulgu okuyucuları; `Replay.Apply`'daki beş karşılık; belge üretimi
ve karşılaştırması, talimat sürümü ve arşivi, gözetmen ve `ObservationTarget`, dış kayıt beklemesi, `watch` ve
`contact` görevlerinin kabul bağları, tekil `Candidate`, `Admission.Events`, eski sızma zincirinin ölü alanları ve dokuz
denge alanı. Eski komut adlarına geriye dönük karşılık tutulmaz: `Replay.Apply` bilinmeyen türü sessizce atlar. Davranış
değişmedi, sürüm **17**'de kaldı (50 tohum × 60 vardiyada RNG ve durum bayt bayt aynı). Olaylar açık oynanmış sürüm 17
kaydında bu türler yoktu; yalnız olaylar kapalı oynanmış (yayınlanmamış) bir kayıt onları taşıyabilirdi. Öyle bir kayıt bu
komutlar olmadan başka oyuna döneceği için yüklenmez (`SaveStore.HasRetiredCommand`; yükleme süzgeci, kural değil) ve yeni oyun
yazılırken `.retired.backup` olarak arşivlenir. Diskte sürüm 17 kaydı yoktu.

**Olaylar kapalıyken (aşama 7'den beri).** `shelterEventsEnabled = false` yalnız testlerin olaysız kapanış dizileri içindir.
Olay, sızma zinciri, tecrit, kısıt, tayın kipi, karartma, açlıktan ayrılma, ifşa, `dusus`/`terk` ve son şalterin ifşa/eksik
nedenleri çalışmaz; aday grubu kapısı (seçim, geri çevirme, dolu sığınakta yollama, acil grup) çalışır; `HungryStreak` sayılır;
sığınağın kendi sonu yalnız `topluluk`; son şalter (`salter`) olaylardan bağımsız çalışır, tahliye nedeni yalnız güven (`guven`) olabilir.
Kısıtı ya da kapı dışı yollamayı kuran eski elle yol yoktur: kapalı oyunda kimse kısıtlı olmaz.

Silinenlere bağlı, birlikte değişen yerler (aşama 7'de değişti):

- Core: `Simulation.PlanningBoard.cs` içinde `CanRestrictedResidentWork` ve gözetmen şartı, `ResidentAdmissionRisk`
  ve fişe her zaman eklenen “Açık kapı başvurusu…” satırı, kapanıştaki `RestrictedUntil` okuması;
  `Simulation.PlanningRaid.cs` içindeki “kısıtlı kabulde” metni. Fişin “KADRO EKSİK n/m” satırı “SIĞINAK EKSİK n/m” oldu
(sunumdaki adla aynı).
- Bridge: `Desk/Scripts/Bridge/Board3D/BoardDropRules.cs`, `Board3D/PlanningBoardSnapshotBuilder.cs`,
  `PlanningBoardPresenter.cs`, `Siginak/HandoverSlipFactsBuilder.cs`, `Siginak/SiginakDoor.cs`,
  `Siginak/SiginakDesk.cs`, `Siginak/SiginakMap.cs`, `Siginak/AdmissionSnapshotBuilder.cs`,
  `Siginak/SiginakAdmission.cs`, `Desk/Scripts/Editor/Siginak/AdmissionFixtures.cs`.
- `Core/GameData.cs` `Endings` tablosuna `dusus`, `terk`, `salter` için `EndingDef` eklenir (başlık ve metin `Loc.N`).
  `GameData.Ending(id)` sözlük dizinleyicisidir; tabloda olmayan kimlikte son rapor kurucusu
  (`FinalReportBuilder`) istisna fırlatır. Tabloda duran `sizma` ve `vuruldu` eski kapı yolunun sonlarıdır,
  dokunulmaz; `dusus` metni `sizma` ile aynı cümleyi kullanmaz.
- `Game/Platform.cs` `Achievements.ForEnding` yalnız `silme` tanır; yeni sonlar için eşleme bölüm 11, soru 7.

**`Step` içindeki sıra (kartlı yol).**

1. Gün artar, gelir, ekipler (bugünkü gibi).
2. Kişiler: tecrit ve kısıt süreleri; saatler; vadesi gelen izler sıraya girer; bekleyen grubun süresi.
3. Pano kapanışı: erzak dengesi (tayın kipi, `obur`, taksitler) → `Starving`, `HungryStreak` → ayrılma → kişi döngüsü
   (yarım tayında iyileşme yok) → üretim (karartmada yok) → sayımlar → kilit ve fiş alanları.
4. Vadeli sonuçlar: karardan sonraki ilk kapanışta çekimleri yapılır; vadesi gelenler uygulanır; tecrit bulgusu.
5. Kapı: acil koşulu (ayrılmadan **sonra** sınanır) ve grup üretimi.
6. Sayaçlar ve sonlar (`AdvanceShelterEndings`): `UnderstaffedStreak` ve `DesertedSinceFull`; sırayla `topluluk`,
   `dusus` (önceki kapanışta `FallPending` kurulmuş ve vardiya boyunca kalkmamışsa), `terk`. Son gelirse olay açılmaz.
7. Olay açılışı: sıradaki zincir olayı, yoksa olağan olay seçimi (aralık, öncelik, sınır).
8. Uyarı (`AdvanceShelterWarnings`): ifşa eşikteyse `FallPending` (sabotajla varılan eşik aynı kapanışta uyarır);
   kapının acil grubu ertesi vardiya için `EmergencyDoorShift`.
9. YZ alarmı (ifşa ara bedeliyle), geçmiş kaydı.

Aşama 5 incelemesinden sonra sonlar olay açılışının önüne alındı: aynı kapanışta açılan sabotaj düşüşün nedeni olamaz
(uyarı vardiyasında sorguyla eşiğin altına inilmiş, ertesi kapanışta başka bir sızmacının sabotajı eşiği yeniden aşmışsa o
kapanış yalnız uyarır) ve son kapanışta kâğıdı görünmeyecek bir olay açılmaz. Olay açılışı canlı sayısını değiştirmediği
için öncelik ve `terk` sonucu aynıdır.

Bugünkü sıra `AdvanceAdmissions` → `AdvancePlanningBoard` → `topluluk` denetimidir; grup üretimi pano kapanışının
önündedir. Yeni sırada grup üretimi kapanışın arkasına alınır ki ayrılmayla 3'ün altına inen sığınak acil grubu aynı
kapanışta üretsin.

**RNG çekim noktaları.** Hepsi `S.Rng`.

- **Yeni komutlar ve okuyucular çekim yapmaz.** Var olan baskın emri (`StartLiberationRaid`) komut anında bir çekim
  yapar (baskın tohumu); bu değişmez. Bu yüzden belirlenimcilik “aynı komut geçmişi” üzerinden tanımlıdır.
- **Çekim sayısı yalnız kayıtlı duruma ve dengeye bağlıdır, gizli kimliğe bağlı değildir.** İnsan için de sızmacı için
  de aynı sayıda çekim yapılır; kullanılmayan sonuç atılır.
- Kurulumdaki çekim sayısı değişmez (ilk aralık bugün de çekiliyor).

| Nokta (kapanıştaki sırayla) | Çekimler | Sayı |
|---|---|---|
| Vadeli sonuç | Karardan sonraki ilk kapanışta, karar sırasıyla: olur mu, ne zaman | Sonuç başına 2 |
| Tecrit bulgusu | Tecrit biten her kişi için | 1 |
| Grup üretimi | Sonraki aralık, grup boyu; sonra **her zaman üç aday** için: konu, profil, kademe, özellik 1, özellik 2, şüphe satırı, sızmacı, iz gecikmesi, insan izi, ikinci iz | 2 + 3 × 10 |
| Olay açılışı (olağan) | Sonraki aralık, hedef kişi | 2 |

Grup iki kişilikse üçüncü adayın çekimleri yapılır ve atılır. Gizli dört çekim (sızmacı, iz gecikmesi, insan izi,
ikinci iz) her adayın **son** çekimleridir ve görünür alanları etkilemez; ilk grupta ve kurucularda sızmacı sonucu
“insan”a kırpılır. Zincir olaylarının açılışı çekim yapmaz.

**Sürüm.** `GameRecord.CurrentVersion` 15 → 16; sürüm tarihçesi yorumuna satır eklenir. Sürüm 15 kayıtları yüklenmez.
Son kimliği kayda yazılmadığı için yeni kimlikler kayıt biçimini değiştirmez; sürümü gerektiren, `Step` sonuçlarının
ve RNG sırasının değişmesidir. Aşama 5 sürümü 17'ye çıkarır: ifşa tavanı ve alarm ara bedeli `board_file` (`Step`) sonucunu
değiştirir (alarm, karşı saldırı, güven ve `karartma` tetiği; dolayısıyla olay çekimleri), `dusus` ve `terk` kapanışta
harekâtı bitirir, `last_switch` yeni komuttur. Aşama 3–4'te oynanmış sürüm 16 kayıtları `shelterEventsEnabled` anahtarını
taşıdığı için o süzgeçten geçerdi ve başka oyuna dönerdi; sürüm 17 onları yüklemez (`SaveStore` `.v16.backup` olarak
arşivler). Aşama 5 incelemesinin düzeltmeleri (tavansız sayaç, sorguda uyarının hemen kalkması, sonların olay açılışından
önce sınanması, `DesertedSinceFull`) `Step` sonucunu yine değiştirir ama sürüm 17'de kalır: sürüm 17 henüz yayınlanmadı,
diskte sürüm 17 kaydı yok (14 Kasım'da gerçek kayıt yeri sürüm 16 taşıyordu). Bundan sonra `Step` ya da RNG sonucunu
değiştiren her Core değişikliği, sürüm 17 ile oynanmış bir yapı varsa, 18'e çıkarır. Aşama 8'in ikinci tur kararları
(kararla varılan eşikte iki uyarı vardiyası, sorgunun tecrit yeri istememesi, `aclik` tahliye nedeni) `Step` ve komut
sonucunu değiştirir; sürüm yine 17'de kalır: sürüm 17 yayınlanmadı, gerçek kayıt yerinde 14 Kasım 2026'da yalnız sürüm 16
kaydı var. Yeni `Balance` alanlarını taşımayan bir sürüm 17 geliştirme kaydı varsayılan değerleri alır ve yeni kuralla
oynanır; tonun ölçütleri `Step`'e girmez (yalnız son raporu okur).

**Motor bağımsızlığı.** Yeni dosyalar `Simulation`'ın parçalı dosyalarıdır (aday grubu bugünkü kabul dosyasında;
olaylar ve sonlar için birer yeni dosya). `UnityEngine`, `System.Random`, `DateTime`, `float` yok; yuvarlama
`Js.Round`.

## 7. Sunum

| Ne | Yüzey | Not |
|---|---|---|
| Aday grubu | Kabul tezgâhı (K8A) | 2–3 aday kartı yan yana; KABUL damgası bir karta basılır |
| “Hiçbiri” | Tezgâh | GERİ ÇEVİR damgası grup fişine basılır |
| Dolu sığınakta yollanacak kişi | Tezgâhta “YER YOK” listesi | Bir satır seçilir, sonra damga geçerli olur; yolluk yazar |
| Olay | Masadaki evrak tepsisinde “OLAY TUTANAĞI” kâğıdı | Satır seç, onayla; bedel sütunu sağda; kapalı satır nedeniyle |
| Olay duyurusu, bulgu ve sonuç | Teleks | Yalnız duyuru |
| Bekleyen karar | Devir fişi uyarısı, vardiya şalteri gerekçesi | Açık olayda fiş basılamaz |
| İfşa | Arka duvarda altı lambalı gösterge | Odaya eklenen **tek** kalıcı yeni gösterge. **Aşama 6:** YZ ALARMI panosunun hemen altında İFŞA şeridi (`Shell/Exposure panel`, 0,50 × 0,136 m; pirinç kenarlı levha, başlık plakası, altı hücre, KONUM lambası). Hücreler eşikte kırpılır, `exposureAlarmAt`'tan sonrası kırmızı; lamba eşikte yanar, “konum çözüldü” vardiyasında yanıp söner; üzerine gelme satırı gerçek değeri ve Balance sayılarını yazar. Kâğıttaki “İfşa +n” kararla birlikte duvarda yanan hücre (sesli), tutanak elden çıkınca üç flaş ve İFŞA teleks satırıyla karşılık bulur. **Aşama 6 düzeltme:** kâğıt elde şeridi örttüğü için ifşayı değiştiren satırın bedeline şerit önizlemesi eklenir (“İfşa 2/6 → 3/6. Alarm her vardiya +0,5.”; sorguda iniş ve “Alarm ara bedeli kalkar.”); alarm bölgesindeki ve eşikteki boş hücre koyu kırmızı (`materialsExtra.exposureStepOffHigh`), lambanın altında “KONUM” plakası, lambanın ışığı (`AlarmGlow`, `exposure.glowIntensity` 0,8) uyarıda yanar. “Konum çözüldü” metinleri son şalteri ancak 5. vardiyadan sonra seçenek olarak sayar |
| Açlık ve tayın | ERZAK sayacı etiketi (“AÇ 2”, “YARIM TAYIN 3 V”), fiş satırı | Fiş sıradaki ayrılacak kişiyi adıyla yazar |
| Eksik kadro | Yalnız fişte geri sayım | Ayrı gösterge yok. **Aşama 6:** sunumda adı **“SIĞINAK EKSİK n/3” / “SHELTER SHORT”** (baskın kadrosunun “KADRO EKSİK”iyle karışmasın); fişte ayrı kutu, CRT ipucunda satır. Core'un oyuncuya gösterilmeyen `BoardReceiptPreview` metni aşama 7'de aynı ada geçti (“SIĞINAK EKSİK n/m”) |
| Kişi durumu | Kişi kartındaki dosya etiketi | KISITLI · n V, TECRİT · n V, İZ 1/2 |
| Son şalter | Arka duvarda kapaklı şalter, masada ÇEKİLME EMRİ kâğıdı | **Aşama 6:** menünün `LastSwitch` modelinin ikinci örneği, doktrin afişinin eski yerinde (afiş dolabın sağına taşındı); K1'den görünür. Kapak (1. tık, KLS pozu) → kol (2. tık) → ÇEKİLME / TAHLİYE EMRİ kâğıdı → kâğıttaki DAMGALA'ya fare tıkı (Enter bağlı değil) komutu yazar. Damgadan önce Esc ya da sağ tık vazgeçer, komut yazılmaz. Rehber sürerken ve 5. vardiyadan önce mühürlü (KİLİTLİ bandı, gerekçe plakası). Plaka çekilirse gelecek çeşidi önceden yazar. **Aşama 6 düzeltme:** kapak kalkıkken ve kol inikken de çeşit plakada (“TAHLİYE · kolu indir”, “ÇEKİLME · damgala”); mühürlüyken tık teleksle nedeni söyler; emir kâğıdında görünür vazgeçiş satırı (“Damgalamadan vazgeç: Esc ya da sağ tık.”). Şalterin adı ve tahliye damgası kendi anahtarlarıyla: oyunun adı olan “SON ŞALTER” (EN CORRIGIBLE) ve baskının “TAHLİYE” (EN EXTRACTED) açılır yazısı paylaşılmaz; adı `son şalter` → LAST SWITCH, damga `tahliye` → EVACUATED (ikisi de büyük harfle yazılır), üzerine gelme başlığı “SON ŞALTERİ ÇEK” |
| Sonlar | Son rapor | Yeni başlıklar, damgalar, ölçüt satırları, kapı bölümü. **Aşama 6:** düzenli çekilme mavi ÇEKİLDİ damgası alır (yenilgi değil, hiçbir yerde KAYBEDİLDİ yok); tahliye kırmızı TAHLİYE ve ilk nedeni. Ton satırı, ölçüt kutusu (TON ÖLÇÜTLERİ / ÇEŞİT / NEDEN), HAREKÂT ve SİCİL sekmeleri; gizli bilgi (ortaya çıkmamış sızmacılar, ifşa adımlarının kişileri) yalnız SİCİL'de |

**Yeniden kullanılanlar.** Aday kartı için `AdmissionDocumentFace` ve `AdmissionDocument.prefab`; kâğıt tabanı
`AdmissionPaperFace`; `StampDecision`; tezgâhın kabuğu, lamba, kapı vuruşu, K8/K8A pozları; teleks kuyruğu; `FileTagView`;
`HandoverSlipFacts` uyarı türleri (yenileri enum'un **sonuna** eklenir).

**Değişenler.** `AdmissionStation` (1600 satır) tek belge varsayar; belge kökü listeye çevrilir ve damganın hedefi
basılan karta göre çözülür. `AdmissionSnapshot.Document` tekilden listeye geçer; şüphe satırı, kademe işareti ve
özellik satırları Bridge'de çevrilmiş olarak doldurulur. `SiginakAdmission` yeni komutları `ExecuteBoardCommand`
üzerinden yollar. `StampChoice` dört değerden ikisini kullanır; enum değerleri silinmez. Son rapor kurucusu yeni
kimlikler için başlık, neden ve son an dallarını alır; `won` ikilisi üç değere çıkar (kazanıldı, kaybedildi, çekildi).

**Söküldü (aşama 7).** Büyüteç, diyafon (interkomun etkileşimi), soğuk cam (buğu camının etkileşimi; gövdeleri kapıda durur) ve
K8G pozu (enum değeri ve çapa durur, odaklanmaz), talep defteri ve formu, kalem, bulgu fişi, talimat şeridi, ESKİ cebi, kimlik
belgesi, tenekeler, KISITLI ve YARDIM damgaları, panelin boş plakalarının yazıları ve işaretçisi; kişi dosyasının İNCELE / ERİŞİM /
TAHLİYE sekmeleri ve künyesi; bunların sınıfları, prefab'ları, snapshot alanları, çevirileri ve yalnız bunlara ait sesler (`Cue`
123–126). Raf (K1-a): iki damga orta yuvalarda, dış yuvaların dili ve plakası söküldü, yuva dipleri ahşapla boyandı; model hâlâ dört
yuvalı ve paneldeki boş tutucular mesh'in parçası (iki yuvalı raf ve sade panel modeli açık iş). Kartlı yolda kullanılmayan eski kapı kâğıdı
(`DoorCardView`) `DeskGame.unity` içinde hâlâ bağlıdır; sahneden çıkarılması bu işin parçası değildir.

**Sahne değişiklikleri.** Tek seferlik editör script'iyle yapılır, çalıştırılır, silinir; yeni `MonoBehaviour`'lar
kendi dosyasında, bağlar `[SerializeField]` ile: tezgâhta iki ek belge kökü ve grup fişi; emekli araçların
kapatılması; evrak tepsisinde olay tutanağı (yeni prefab); arka duvarda İFŞA göstergesi ve kapaklı son şalter; masada
ÇEKİLME EMRİ kâğıdı.

**Rehber.** Yeni adımlar adım listesinin **sonuna** eklenir:

1. İlk grupta: “Birini alabilirsin ya da hiçbirini. Şüphe satırını ve getirdiğini birlikte oku. Getirdiği onunla kalır.”
2. İlk olayda: “Her satırın bedeli yanında yazar. Karar vermeden vardiya kapanmaz.”
3. İlk izde: “Bir iz şüphedir, iki iz güçlü kanıttır. Görmezden gelinen iz ifşayı artırır. Tecrit her zaman bulgu vermez.”
4. Son bilgi kartına tek cümle: son şalterin yeri ve anlamı.

**Aşama 8 uygulaması (2026-11-14).** Rehber 26 adım. İlk vardiya dosyalanınca hedef satırı (ONERILER Ö4: sektör, güven,
alarm, ifşa; canlı), sonra konu bölümü: ilk olay tutanağı (iki adım: tepsiden al; satır seç, KARAR; güvenli satır ve fiş
kilidi) ve ilk aday grubu (iki adım: KAPI → tezgâh; şüphe satırı, getirdiği, KABUL/GERİ ÇEVİR, dolu sığınakta yollama ve
yolluk) **geldikleri sırayla**; ikisi yokken bekleme adımı. Madde 3 (ilk iz) ayrı bir bekleme adımı değil, İFŞA bilgi
adımının içinde (iz rehber süresinde gelmeyebilir; gelirse tutanak adımları onu da anlatır). Ardından İFŞA ve konum uyarısı,
son şalter (madde 4; mühürlü, ÇEKİLME/TAHLİYE) ve kapanış. Konu çıkmazsa bekleme V.011'e girilince biter (`WatchLastDay` = 10
kapanış): rehber son şalteri sonsuza dek mühürlemez. Konu bölümünde not sayacı "REHBER · KONU" yazar (adım numarası konuya göre
ileri geri gidiyordu); rehber sürerken İFŞA ve teleks metinleri mühürlü şalteri önermez. Ayrıntı ve doğrulama: `DURUM.md` (KOS aşama 8: rehber).

**Lokalizasyon.** Bütün yeni metinler Türkçe yazılır ve `Loc` ile sarılır: on özellik, şüphe satırları (kademe başına
en az üç), on dört olayın başlık, gövde, seçenek, bedel ve sonuç metinleri, kişi durumu etiketleri, üç yeni sonun
başlık ve metinleri, çekilme çeşitleri, ton cümleleri ve ölçüt satırları, fiş ve teleks satırları, rehber adımları.
Statik tablolar `Loc.N`, gösterim `Loc.T`, biçimli metin `Loc.F`. Çeviri tablosu güncellenir ve `strings.txt`'e
İngilizceleri yazılır. Olay günlüğü ve rapor verisi kimlik saklar; rapor gösterildiği dilde kurulur.

## 8. Testler ve ölçüm

**Var olan testleri ayakta tutmak.** On beş test dosyası kapıyı `admissionKnockMin = 100` ile susturuyor (Core:
`PlanningBoardTests`, `ShiftCloseCoreTests`, `CommunityAdmissionTests`, `AdmissionSupervisorTests`; sunum:
`Rev3BoardSnapshotTests`, `Rev3BoardDropRulesTests`, `Rev3RecipeBookTests`, `Rev3RecipeDragTests`,
`Rev3Stage5ContractTests`, `Rev3Stage6ContractTests`, `Rev3HandoverSlipFactsTests`, `Rev3AdmissionSnapshotTests`,
`Rev3AdmissionPaperTests`, `Rev3AdmissionFlowTests`, `SaveStoreTests`). Bu yüzden alan adı korunur. Çok vardiya
kapatan testler açık olay yüzünden fiş basamaz hâle gelmesin diye olayları kapatan `shelterEventsEnabled` eklenir ve
bu testlerin kurulumunda `false` yapılır. Kapı ve olaylar kapalıyken kapanış başına yeni çekim yapılmaz; tohuma
kilitli altın değerler bu testlerde kaymaz (yine de aşama 1 ve 3 sonunda doğrulanır).

**Silinecek ya da baştan yazılacak (aşama 7'de yapıldı: dosyalar silindi ya da yeni yola taşındı, atlanan test kalmadı).** Core: `CommunityAdmissionTests` (11 test) ve `AdmissionSupervisorTests` (16
test); inceleme, talimat, gözetmen ve bulgu testleri silinir; zincir, replay ve gizlilik testleri yeni kurala göre
yazılır. Sunum: `Rev3AdmissionSnapshotTests`, `Rev3AdmissionFlowTests`, `Rev3AdmissionMotionTests`,
`Rev3AdmissionPaperTests`, `Rev3AdmissionSceneTests`, `Rev3Stage6ContractTests`, `Rev3DoorTests`,
`Rev3HandoverSlipFactsTests`; `RestrictResident`, `ReviewResident` ve gözetmen kullanan `Rev3BoardSnapshotTests` ve
`Rev3BoardDropRulesTests` bölümleri, `EnglishCardBuildersTests` kabul satırları; PlayMode `AdmissionPlayTests`,
`AdmissionPlayKit` ve fikstürü.

**Güncellenecek.** Sürüm 15'i sabitleyen PlayMode testleri (`ShiftClosePlayTests`, `ProductionPlayTests`,
`AdmissionPlayTests`) ve `SaveStoreTests`; `LiberationCampaignTests` ve `PlanningBoardTests`'in kayıt ve son
beklentileri; `DeskBridgeTests`'in son rapor testleri; `SiginakMapTests`; `LocalizationTests` yeni tabloları tarar.

**Eklenecek Core testleri.**

- Çekim sırası: sabit tohumda iki grup ve üç olaylık altın zaman çizelgesi; olasılıklar değişince çekim sayısı değişmez.
- Yeni komutlar ve okuyucular RNG tüketmez (baskın emri bu testin dışındadır).
- Gizlilik, iki parça: (1) aynı tohumda bir adayın sızmacı çekimi zorla çevrildiğinde **ilk ize kadar** bütün public
  durum (aday kartı, pano JSON'u, sayaçlar) birebir aynıdır; (2) iz anında, aynı vardiyada iz bırakan bir insan ile
  bir sızmacı için olay kâğıdı, seçeneklerin açıklığı ve ifşa değişimi aynıdır. Ayrıca public JSON'da `internal`
  alanların hiçbiri yoktur; `VictoryTone` ve `EndReport` son yokken `null` döner.
- İlk grup ve kurucular sızmacı olamaz; kurucu iz bırakmaz; kademe kaydırma; `kit` → `devre`, `yavas` → `obur` dönüşümleri.
- Adlar: 60 adayda kurucuyla ve grup içinde çakışma yok.
- Kişiye bağlı teçhizat devredilemez ve kişiyle gider; taksitler kişi çıkınca kesilir.
- Dolu sığınakta seçim yalnız `replaceId` ile; boş `replaceId` JSON gidiş-dönüşünde `""` olarak da doğru işlenir.
- Acil grup: ayrılmayla 3'ün altına inilen kapanışta üretilir, üç eksik rolle ve kitle.
- Kilitli planda seçim ve olay kararı geçer; baskın sürerken ve fişten sonra geçmez; açık olay baskın emrini engellemez.
- Olay aralığı (açılıştan açılışa), öncelik, tekrar sınırı; zincir olayı aralığı tüketmez; sıradaki izde saat durur;
  hedefi giden olay kapanır ve fiş kilidi kalkar.
- Zincir: iz → ikinci iz → müdahale vardiyası → sabotaj; Boşta tutulan sızmacının saati işler; tecrit bulgusu iki
  sonuçla; insan sabotaj yapmaz.
- **Kilitsizlik:** her olay × uç durum (canlı 1, erzak 0, güven 1, tecrit yeri dolu) için en az bir açık seçenek ve
  hiçbir seçenekte karar anında son yok.
- İfşa: iki kaynak, eşik, sabotajla varılan eşikte bir, kararla varılanda iki vardiyalık süre, sorguyla eşiğin altına inince `dusus` gelmez (sorgu tecrit yeri istemez), alarm ara bedeli.
- Açlık: `HungryStreak` artışı ve 1'er azalışı, ayrılma sırası, kit sahibinin en sona kalması, son kişinin gitmemesi,
  `terk` koşulu ve sayımın acil grupla başlaması, ayrılmasız eksik kadroda `terk` yok.
- Son şalter: en erken vardiya, tek kez işlenme, iki çeşit. Zafer tonları: üç ton ve izsiz sızmacının gölgelememesi.
- `GameData.Endings` her yeni kimliği içerir; son rapor kurucusu hepsinde istisnasız çalışır.
- `Balance.Clone()` sonrası kopya ile kaynak bağımsızdır (yeni alanlar düz kaldı).
- Her yeni komut için JSON gidiş-dönüş ve “kayıttan devam aynı duruma ve aynı sona varır”.

**Eklenecek sunum testleri.** Aday snapshot'ı (2 ve 3 kart, çevrilmiş satırlar, kapalı damga gerekçesi); olay kâğıdı
snapshot'ı (kapalı seçenek ve nedeni); fiş uyarıları; son rapor (yeni başlıklar, damgalar, ölçüt satırları, kapı
bölümü); metin taşması taraması (en uzun şüphe satırı ve özellik çifti iki dilde karta sığar).

**Bot ölçümü.** Gerçek Core ve `RaidBot` ile, yayın kuralıyla. Kapı politikaları: hiçbirini alma, yalnız kefilli, en
cömert kartı al, karışık (kadro eksiğine göre), **al ve ilk fırsatta yolla**. Olay politikaları: hep görmezden gel,
hep tecrit, hep yolla, bedeli en düşük. En az 40 tohum, 120 vardiya sınırı. Bot insan değildir; sayılar yön gösterir.

| Ölçü | Hedef (b) |
|---|---|
| Harekât uzunluğu (zaferle biten) | 35–45 vardiya |
| Harekât başına açılan olay (zincir dahil) | 12–15 |
| Olay türü çeşitliliği | Tek bir tür olayların %35'ini geçmez |
| Kapı politikaları arası zafer farkı | En iyi ile “yalnız kefilli” arasında en çok 15 puan; “hiçbirini alma” belirgin kötü |
| “Al ve yolla” | Hiçbir kapı politikasını geçmez |
| “En cömert kartı al + hep görmezden gel” | `dusus` payı %40 ve üstü |
| “Karışık kapı + hep tecrit” | `dusus` payı %15'in altında; “bedeli en düşük”ten belirgin iyi değil |
| Zaferlerde gölgeli ton | %5–15 |
| Zaferlerde temiz ton | %10–30 |
| 60. vardiyada hâlâ süren harekât | 0 |
| `terk` payı (iyi politikada) | %10'un altında |
| İyi politikada `erzak_azaliyor` görülen harekât | En az %30 (açlık kolu yalnız kaybedene görünmemeli) |
| İnsanı yollama (“hep yolla”) | Yollananların çoğunluğu insan çıkmalı |

Elle sınama: iki tam harekât; biri bilinçli ihmalle (`dusus` görülmeli, uyarılar sayılmalı), biri açlıkla (`kacak` ve
ayrılmalar okunuyor mu).

**Ölçüm sonuçları (aşama 8, 2026-11-14/15; inceleme sonrası son hâl).** Düzenek `Tools/DengeBotu/` (gerçek Core, gerçek
`RaidBot` 8 seçenek, olaylar açık, 120 vardiya; `asama8.txt` bu tabloyu yeniden üretir). İlk ölçüm (40 tohum, 960 harekât, 30
aday set) botun yayındaki panoda bulunmayan İkmal ve Bakım görevlerini kullanmasıyla yapılmıştı (sunum onları Boşta'ya indirir;
toplayıcı kademe 1'de +1,4 erzak). **Bütün sayılar sunumun kuralıyla** (`RETIRE=1`) yeniden ölçüldü; tüketim 0,575 yerine 0,40,
olay aralığı 2–3 kaldı. Uygulanan: 80 tohum; "iyi" = karışık+tecrit, karışık+ucuz, kefilli+ucuz (240 harekât). Taban: aynı
kural, eski yedi değer, 40 tohum. Gürültü: üç "iyi" yapılandırma tohumların çoğunda aynı yolu izlediği için 40 tohumda "iyi"
küme yaklaşık ±7 puan oynar (tohum 1–40 ile 41–80 arasında zafer %62 / %66, temiz ton %11 / %5).

| Ölçü | Hedef | Taban (sunum kuralı) | Uygulanan (80 tohum) | Durum |
|---|---|---|---|---|
| Harekât uzunluğu (zafer) | 35–45 | ort 48,2 / med 48 | ort 48,3 / med 48 | **Tutmuyor** (daha iyi baskın oyunuyla tutuyor, aşağıda) |
| Olay / harekât | 12–15 | 11,9 | 14,7 (iyi), 14,6 (6 yapılandırma) | Tutuyor |
| En sık tür | ≤ %35 | agir_yarali %18 | agir_yarali %15 | Tutuyor |
| Kapı farkı (en iyi − kefilli) | ≤ 15 puan | 8 | 5 (karışık %69, kefilli %64; ucuz) | Tutuyor |
| "Hiç alma" | belirgin kötü | %20 | %34 (iyi %59–69) | Tutuyor |
| "Al ve yolla" | hiçbirini geçmez | — | %76 > cömert+yolla %71, karışık+ucuz %69 | **Tutmuyor** (gürültü sınırında; `dismissSupplies` 4 ile %71) |
| Cömert + görmezden gel `dusus` | ≥ %40 | %62 | %61 | Tutuyor |
| Karışık + tecrit `dusus`; ucuzdan iyi değil | < %15 | %0; %38 = %38 | %0; %59 < %69 | Tutuyor |
| Gölgeli ton | %5–15 | %0 | %5 (iyi), %6 (6 yapılandırma) | Sınırda |
| Temiz ton | %10–30 | %7 | %8 (iyi), %7 (6 yapılandırma) | **Tutmuyor (bot)**; usta botta %13 |
| 60. vardiyada süren | 0 | 5/120 | 8/240 | **Tutmuyor** |
| `terk` (iyi) | < %10 | %0 | %5 | Tutuyor |
| `erzak_azaliyor` görülen (iyi) | ≥ %30 | %0 | %53 | Tutuyor |
| "Hep yolla"da yollananların çoğu insan | çoğunluk | — | karışık %82, cömert %33, al ve yolla %45 | Kapıya bağlı |
| dagilma (iyi) | belirgin düşsün | %62 | %28 | Tutuyor |

Sapmaların yorumu (`DURUM.md` › KOS aşama 8: denge): uzunluk ve 60. vardiyada süren harekât 36 sektörlük harekâtın kayıp
vardiyalarından gelir (başarısız ~5, boş ~7, karşı saldırı ~1,4) ve Balance'la kısalmaz; seçeneği iki katına çıkan bot
(`ALT=16`, karışık+ucuz, 40 tohum) zafer %65 → %80, dagilma %25 → %15, uzunluk medyanı 48,5 → 43,5 ve temiz ton %8 → %13 verdi:
uzunluk ve temiz ton zayıf baskın oyununun ölümlerinden (harekât başına ~6,4) etkilenir. Gölgeli ton bot politikalarında ancak
"ucuz"un görmezden geldiği sızmacıdan doğar. "Al ve yolla" cömert kapıyı sürekli yenileyerek yeniden en iyi olabiliyor; yolluk 4
erzak olunca fark kapanıyor ve öteki politikalar değişmiyor (karışık+ucuz %69), ama yolluk metinlere ve testlere de dokunduğu
için değiştirilmedi (`DURUM.md` açık karar). Tüketim eğimi diktir: 0,375 / 0,40 / 0,425'te iyi politikada `erzak_azaliyor` %28 /
%47 / %62, terk %0 / %6 / %6 (40 tohum). dagilma'nın son darbesi çoğunlukla baskında kırılan sivildir (%72); dağılmaların %41'i
ilk 20 vardiyada, %49'u 35. vardiyadan sonra; `liberationTrustStart` 12 bunu değiştirmedi (%29). Soru 14 (`anma`): olayların
%8'i, her harekâtta ölüm var; tetik değişmedi.

## 9. Uygulama aşamaları

Her aşama derlenir, EditMode testleri boş sahnede geçer, oyunda denenir. Sürüm ilk aşamada 16 olur; aşamalar arasında
üretilen geliştirme kayıtları geçersizdir.

| # | Aşama | İçerik | Bitti sayılır |
|---|---|---|---|
| 0 | Ölçüm tabanı | Düzenekte bugünkü kuralla taban ölçümü; oyun kodu değişmez | Taban tablo kayıtlı |
| 1 | Core · aday grubu | Yeni durum, `candidate_choose`, `candidate_pass`, özellikler, kişiye bağlı teçhizat, kademeler, acil grup, kilit kuralı, yolluk, sürüm 16, `Balance` alanları. **`Candidate` ve eski alanlar durur** (`Candidate` hep `null`); eski komutlar derlenir ama aday bulamaz | Yeni Core testleri geçer; sunum ve var olan testler derlenir, kapı sessizdir |
| 2 | Sunum · aday grubu | Tezgâhta çok kart, damga hedefi, dolu sığınak listesi, fiş uyarısı, metinler | Oyunda bir grup alınır ve geri çevrilir |
| 3 | Core · olaylar ve açlık | `ShelterEvents`, `event_decide`, on dört olay, kişi durumları, zincirin taşınması, insan izi, tecrit bulgusu, tayın, `HungryStreak`, ayrılma, `shelterEventsEnabled` | Zincir, kilitsizlik ve gizlilik testleri geçer; düzenekte olay sayısı ölçülür |
| 4 | Sunum · olaylar | Olay tutanağı, teleks, fiş, kişi etiketleri, sayaç etiketleri | Her olay oyunda en az bir kez görülür (hata ayıklama kurulumuyla) |
| 5 | Core · sonlar | `Exposure`, `dusus`, `terk`, `last_switch`, `VictoryTone`, `EndReport`, `GameData.Endings`, öncelik | Son testleri ve replay testleri geçer |
| 6 | Sunum · sonlar | İFŞA göstergesi, son şalter ve çekilme emri, son rapor, ses, başarım eşlemesi | Yeni raporlar oyunda görülür |
| 7 | Temizlik | Eski komutlar ve `Candidate`, inceleme kodu, bağlı Bridge okuyucuları, emekli araçlar ve testleri, yetim çeviriler, kalkan `Balance` alanları | Derleme ve bütün testler temiz. **Bitti (2026-11-14):** davranış ve sürüm 17 aynı, sahneden emekli nesneler söküldü, 188 yetim çeviri silindi, Ignore'lu test kalmadı; inceleme sonrası K7 bayrağı, K8 token'ları, buğu malzemesi, ölü prefab geçersiz kılmaları söküldü ve eski komutlu kayda yükleme süzgeci eklendi (DURUM.md) |
| 8 | Rehber ve denge | Rehber adımları, bot ölçümü, sayıların ayarı, `OYUN-TASARIMI.md`, `MIMARI.md`, `DURUM.md` | Bölüm 8 hedefleri ölçülmüş, sapmalar yazılmış. **Bitti (2026-11-15):** ikinci tur kararları ve rehber (26 adım) uygulandı; altı `Balance` alanı değişti (§5 "Aşama 8 dengesi"; bütün ölçüm sunumun kuralıyla, 80 tohum; tüketim 0,40, olay aralığı 2–3 kaldı), `BalanceConfig.asset` kodla alan alan aynı; bot düzeneği `Tools/DengeBotu/`; sürüm 17 (denge kayda yazılır; eski dengeli kayıt yeni kodla birebir döndü); kayan altın değerler gerçek Core'dan yeniden üretildi; ölçüm sonuçları §8; belgeler son hâlinde. Elle sınamanın iki harekâtı yapılmadı |

## 10. Riskler

- **Yeni tek doğru.** “Hep şüpheliyi al, ilk izde tecrit et” yine güçlü olabilir. Karşı ağırlıklar: tecrit kesin
  sonuç vermez, getiri kişiye bağlıdır, yollama yolluk ister, insan izleri vardır. Ölçüm yetmezse
  `isolationFindChance` düşürülür ya da tecrit süresi uzatılır.
- **Olasılıklı tecrit haksız gelebilir.** Oyuncu “tecrit ettim, yine sabotaj yaptı” diyebilir. Kâğıt bunu baştan
  söylemeli (“tecrit her zaman bulgu vermez”); bkz. bölüm 11, soru 3.
- **Açlık sarmalı.** Aç sığınak kişi kaybeder, kişi kaybı baskını zorlaştırır. Kit sahibinin en sona kalması ve acil
  grubun kitle gelmesi bunu yumuşatır, kapatmaz.
- **Açlık kolu görünmeyebilir.** Sayılar değişmezse bölüm 3'ün üç olayı ve `terk` iyi oyuncuya hiç görünmez.
- **Olay yorgunluğu.** 12–15 olay ve 10–12 aday grubu, vardiya başına ortalama bir ek karar demektir.
- **Gizlilik sızıntısı.** Her yeni public alan kimlikle ilişkilenebilir (ör. ayrılma sırası, hırsız seçimi, taksit
  zamanı). Gizlilik testi public durumun tamamını karşılaştırmalıdır.
- **Çekim sırası kırılganlığı.** Kapanışta dört çekim noktası var; araya eklenen her çekim sürüm ister.
- **Sunum yükü.** Tezgâhın çok kartlı yeniden yazımı ve emekli araçların sökülmesi en büyük iştir.
- **Ton okunmaz kalabilir.** Zaferin tonu yalnız raporda görünür; rapor ölçütleri satır satır yazmazsa keyfî gelir.

## 11. Açık sorular (geliştirici kararı)

**Kararlar (14 Kasım 2026, ikinci tur).** Geliştirici kalan açık soruları önerilen varsayılanlarla kapattı. Uygulama
aşama 8'in ilk adımıdır. **Uygulandı (2026-11-14, aşama 8 ilk adım):** Core, metinler, sahne ve belgeler; sürüm 17'de kalır
(gerekçe §6 "Sürüm" ve `DURUM.md`). Uygulamada sayılar ve sapmalar: kararla varılan eşikte uyarı `exposureDecisionWarnShifts`
= 2; ölüm sınırı taban `victoryCleanMaxDeaths` = 0 ve her `victoryCleanDeathShifts` = 15 vardiyada +1 (inceleme sonrası bot
ölçümüyle: önceki taban 2 + her 30 vardiya 30–59 vardiyada hep 3 veriyordu, yani pratikte sabitti; yeni biçimle tekil 174 zaferde
temiz %24,7, gölgeli %2,9:
bant altı, açık; `DURUM.md`); gölge eşiği `victoryShadowIgnoredTraces` = 1; afiş son şalterin üstüne (0,68; 1,53; 2,30) ölçek 0,40;
telsiz ve cihaz kapıdan uzağa **9,7 cm** (masa ekseninde 9,3 cm geri, 2,7 cm sola; kanatla ve CRT ile 1 cm pay). İnceleme
sonrası **sapma, geliştirici onayı bekliyor:** ahize kordonu hiçbir konumda hem kanattan hem CRT'den uzak kalmadığı için (ekran
metninin önünden geçiyor ya da CRT camını deliyordu) telsiz kordonsuz türev mesh'lerle çizilir (`DURUM.md`); hurda kutusu 20 mm sola
(`layoutMm.hurda` [992, 474, 130, 126]).

| Konu | Karar |
|---|---|
| Sorgu ve tecrit yeri | Ortaya çıkmış sabotajcının sorgusu tecrit yeri istemez (kişi zaten yer tutmadan bekletiliyor) |
| Kararla varılan ifşa eşiği | Eşiğe bir kararla (görmezden gelinen iz) varılırsa uyarı iki vardiya sürer; sığınak ikinci kapanışta düşer. Sabotajla varılan eşikte bir vardiya kalır |
| Aşama 5 düzeltmeleri (tavansız sayaç, uyarının hemen kalkması, terk için son tam kadrodan beri ayrılma, "kadro" nedeninin ilk eksik vardiyadan başlaması) | Onaylandı |
| Açlık tahliye nedeni | Eklenir: aç sığınaktan ayrılmalar sürerken (açlık sayacı ayrılma eşiğinde ve sıradaki ayrılacak kişi belli) son şalter tahliye sayılır |
| Fiş etiketi | "SIĞINAK EKSİK" (uygulandı) |
| Ton ölçütleri | Ölüm sınırı harekât uzunluğuyla ölçeklenir; "gölgeli" yalnız görmezden gelinmiş en az bir izi olan ve ortaya çıkmamış sızmacıda verilir. Sayılar aşama 8 ölçümüyle ayarlanır |
| Afişin yeri | Son şalter afişin eski yerinde kalır; afiş tamamen görünür olacak biçimde yeniden yerleştirilir |
| Gölgeli zaferin metni | "Son sunucu düştü. İçeride {ad} hâlâ dinliyordu." |
| Steam başarımları | Kodda eşlendi; Steamworks'te tanımlanması geliştiricinin işi (listesi DURUM.md'de) |
| Menüdeki ayarlar | İki sahnedeki elektrik kutusuna küçük bir AYARLAR plakası eklenir (oda ve menü aynı kalır) |
| Buğu camı ve interkom bileşenlerinin sökümü | Onaylandı |
| Raf ve panel modeli | Dört yuvalı raf ve panel şimdilik böyle kalır; yeni model geliştiricinin işi |
| Kişi dosyasının sağ sayfası | Üst satıra "KALICI KİŞİ DOSYASI" başlığı gelir |
| Eski komutlu v17 kaydı süzgeci | Kalır |
| AGENTS.md belge tablosu | Bu belgenin satırı eklenir |
| Kapı kanadı ile telsiz cihazının çakışması ve ahize kordonu | Telsiz ve altındaki cihaz kapıdan uzağa (yaklaşık 8 cm) birlikte kaydırılır; kordon ekran metnini kesmeyecek biçimde |
| Hurda kutusu | Yeni panonun çerçevesinin içine alınır |
| Menü kapısı (odaya açılma, yazı, başlık, şeritler) | Böyle kalır |
| Kullanılmayan Rev3 kapı ve menü modelleri | Manifest hattında kalır |
| `SiginakTextScan` içindeki kullanılmayan kapı istisnası | Silinir |

**Kararlar (13 Kasım 2026).** Geliştirici aşağıdaki soruları önerilen varsayılanlarla kapattı; kalanlar sunum
aşamalarına kadar açık.

| Soru | Karar |
|---|---|
| 1 · Kit kilidi | Alındı: aç kapanışta eksik temel rol kitinin üretimi durmaz; hiç bulunmayan rol kiti devresiz, yalnız metalle üretilebilir. Aşama 1'de, sürüm 16 ile birlikte girer |
| 2 · Erzak baskısı | `boardLootSupplies` 6 → 4 denenir ve botla ölçülür (aşama 8); ölçüm kötü çıkarsa geri alınır. **Aşama 8:** ganimet tek başına etkisizdi (erzak hiç bağlamıyordu); 4 kaldı ve tüketim 0,35 → 0,40 ile birlikte iyi politikada `erzak_azaliyor` %53, terk %5 (80 tohum, sunumun kuralıyla) |
| 3 · Tecrit | Olasılıklı kalır (bu belgedeki hâl) |
| 4 · Zincir olayları | Aralık bütçesinin dışında kalır |
| 5 · İfşanın ara bedeli ve sorgu | Kalır |
| D3 · Kısıtlı ya da tecritteki kişinin kiti (aşama 3) | Kişiye bağlı kit kit kilidinde “var” sayılır: bedava kit üretilmez, takas tetiği ve acil grubun rol sırası da öyle sayar. Bedel olay kâğıdında ayrı satırda yazar (“Bu sürede … kullanılamaz: kendi kiti ondadır.”, `ShelterEventOption.KitLock`); kişi çıkınca kit onunla gider ve kilit açılır |
| Aşama 3 incelemesi · büyük hırsızlık | Zincir değildir: olağan olay aralığına ve “kişiler” önceliğine uyar; hırsız çalamayacak durumdaysa (yatıyor, kısıtlı, tecritte) bekler (bölüm 3, açıklamalar) |
| Aşama 3 incelemesi · son kişi | `tecrit` son canlı kişide kapalıdır. Ortaya çıkan sızmacı son kişiyse `sok` kapalı, `sorgula` açık kalır ve sorgu bitince sığınak boşalır (`topluluk`): sığınak zaten yalnız sızmacıdan ibarettir. Tecrit sürerken öteki herkes ölürse bulgu yine sızmacıyı söker (aynı gerekçe) |
| Aşama 3 incelemesi · kısıtın bitişi | Kısıt, süresinin son kapanışı bittikten sonra kalkar: o kapanışın açlıktan ayrılması kişiyi kısıtlı sayar, fişin “ayrılır” satırı da (3 vardiyalık kısıt üç kapanışı korur) |
| Aşama 3 incelemesi · kilitli plan | Kısıt ve tecrit kararı kilitli planda (baskın emrinden sonra) da görevi hemen Boşta'ya indirir; `ikna` kilitli planda görevi değiştirmez, bedeli sonraki vardiyaya kayar |
| Aşama 3 · kayıt | Sürüm 16 kalır; olay alanını (`shelterEventsEnabled`) taşımayan sürüm 16 kaydı (aşama 1–2) yüklenmez |
| 12 · İkmal ve Bakım yuvaları | Bu iş boyunca geri gelmeyecek sayılır |
| 6 · Ölümle eksilen kadro | Varsayılan: yalnız ölümlerle 3'ün altına inen, ayrılma yaşamamış sığınak kendiliğinden bitmez |
| 7 · Son şalter (Core kısmı) | En erken 5. vardiya (`lastSwitchMinShift`); "konum çözüldü", işleyen eksik kadro sayacı ya da güven 2'nin altı tahliye sayılır (yenilgi). Rehber sürerken kapalı tutmak ve başarım eşlemesi aşama 6'da (sunum) |
| 7 · Son şalter (sunum, aşama 6) | Yer: doktrin afişinin yeri. **Varsayılan, kullanıcı onayı bekliyor (S1):** afiş (0,68; 1,095; 2,30) ölçek 1'den (2,01; 1,22; 2,30) ölçek 0,70'e taşındı (dolabın sağı); onaylanmazsa şalter için başka yer seçilir ve afiş eski değerlerine döner. İki adım: kapak + kol, sonra kâğıtta damga (Enter damgalamaz). Rehber sürerken mühürlü. Başarımlar: `ACH_ERADICATION` (var), `ACH_ERADICATION_CLEAN`, `ACH_WITHDRAWAL`, `ACH_EVACUATION`, `ACH_SHELTER_FALLEN`, `ACH_SHELTER_ABANDONED`; **yalnız kod eşlemesi, Steamworks'te tanımlanacak** (tanımlanmazsa sessizce düşer) |
| Aşama 7 · K1 raf | (a) iki damga orta yuvalara (Slot2 KABUL, Slot3 GERİ ÇEVİR), dış yuvaların dili ve plakası sökülür, yuva dipleri ahşap; iki yuvalı model (c) açık iş |
| Aşama 7 · K2 model içi parçalar | Prefab örneğinden "removed GameObject" geçersiz kılmasıyla silindi (Unity 6 izin verdi; gizleme gerekmedi) |
| Aşama 7 · K3 `Admission.Events` | Silindi; KARAR satırı `AcceptedShift`'ten |
| Aşama 7 · K4 buğu camı ve interkom | Yalnız etkileşim bileşenleri, çarpıştırıcıları ve buğu dairesi söküldü; gövde, kapak, plaka ve `Poses/K8G` kalır |
| Aşama 7 · K5 kişi dosyası | KAYIT: KURUCU, GELİŞ, KABUL, KARAR, KISITLAMA BİTİŞİ, BAĞIMSIZ BULGU; olay satırları koşulsuz; sekmeler, künye ve belge satırları yok |
| Aşama 7 · K6 eski komutlar | `Replay.Apply` atlar; inceleme sonrası yükleme süzgeci: beş eski türden birini taşıyan kayıt yüklenmez, yeni oyun yazılırken `.retired.backup` arşivi (sürüm 17 kalır) |
| Aşama 7 · K7 İNCELE bayrağı | İnceleme sonrası söküldü: `PersonCardData.Review`, `BoardWords.Review`, `CardStateOverlay`'in bayrak/sözcük alanları ve çizimi, kart prefab'ındaki kapalı “Bayrak sözcüğü”; modelin `Flag` parçasını `BoardCardView` kurulumda gizler; `PersonStates.Review` sayısı kalır |
| Aşama 7 · K8 token'lar | İnceleme sonrası söküldü: okuyucusuz `admission.instruction/checks/decisions/supervisor`, emekli araçların 14 süresi, `slipPathMm`, `slipVerdict`, kısıtlı/yardım izleri, şerit/karbon/mühür renkleri, `personFile.pages`; içe aktarıldı (sahne kirlenmedi) |
| Aşama 7 · buğu camı malzemesi | İnceleme sonrası söküldü: `Cold fog.mat`, `tx_fog.png` ve fırını (tek kullanıcısı silinen `Fog/Buğu`); `SiginakPose.BreathGlass` ve `Poses/K8G` kalır |
| Aşama 7 · `SlipWarningKind` | Eski dört üye (0–3) silindi, kalanların sayısı açık (4–7); eski yan dosyadaki 0–3 fişte yazılmaz |
| Aşama 6 · fiş etiketi (A5 soru 5) | Sunumda “SIĞINAK EKSİK” / “SHELTER SHORT”; baskın kadrosunun “KADRO EKSİK”i değişmez |
| Aşama 6 · GÜVENLİ etiketi | Eşiğe vardıran “görmezden gel”de kâğıdın GÜVENLİ etiketi kırmızı “EŞİK” olur (yalnız public durumdan) |
| Aşama 6 · doktrin notu | İFŞA şeridinde sabit not yok (Balance değişince yanlış kalırdı); eşikler üzerine gelme satırında Balance'tan. Düzeltmede kapalı `TXT_Note`'un metni boşaltıldı, DeskLocText'i kaldırıldı |
| Aşama 6 · bilinmeyen kimlik | Sunum Core'un bilmediği tahliye nedenini ya da ifşa kaynağını ham yazmaz: “başka neden” / “başka kaynak”. `KosFinalReportTests.Ids_CoreIdsMapped_UnknownIdsNeverRaw` Core'un kaynağını tarar; “aclik” gibi yeni bir kimlik eklenirse kırılır ve eşleme ister |
| Aşama 6 · şalter raporu | ÇEŞİT satırlarının evet/hayır ve işliyor/işlemiyor değeri Core'un çekimde yazdığı nedenlerden (`SwitchReasons`); sayılar son anın durumundan. Bağ testle kilitli (`Switch_LiveReadersAgreeWithRecordedReasons`); değerlerin `EndReport`'a taşınması Core işi (aşama 7/8) |
| 13 · `sizma` | Varsayılan: tabloda kalır |
| Aşama 5 · K1 ifşa tavanı | **İnceleme sonrası kaldırıldı:** sayacın tavanı yoktur ("dört iz + iki sabotaj" 8 eder); gösterge sunumda kırpar. Kırpma, eşikteki ifşaya gelen sabotajı bedelsiz yapıyor ve ardından gelen sorgu sığınağı kurtarıyordu (ek bir kötü olay oyuncuya yarıyordu). Sorgu eşiği yalnız sabotaj ifşayı tam eşiğe taşıdığında geri alabilir. `exposureThreshold` ≤ 0 iken `dusus` yok |
| Aşama 5 · K2 açlıktan ayrılmış | `Shelter.Deserted > 0`: açlık ayrılması ve `kacak` olayındaki `birak` birlikte sayılır |
| Aşama 5 · K3 acil grup bekledi | Vardiya kapıda acil grupla açıldıysa sayılır (`EmergencyDoorShift`, kapanışta yazılır), vardiya içinde aday alınsa da. Olağan grupla açılan vardiyada sayaç tutulur (artmaz, sıfırlanmaz) |
| Aşama 5 · K4 uyarı anı | `FallPending` kapanışta kurulur. Kararla eşiğe varılırsa o vardiyanın kapanışı uyarır, sonraki kapanış düşürür; sabotajla varılırsa aynı kapanış uyarır. İnceleme sonrası: sorgu ifşayı eşiğin altına indirdiği anda uyarı kalkar (`konum_kalkti` karar anında; fiş satırı ve "konum" tahliye nedeni de kalkar); sonlar olay açılışından önce sınanır (§6 sıra) |
| Aşama 5 · K7 terk ve ayrılma dönemi | İnceleme sonrası: terk için gereken ayrılma, sığınağın son kez canlı ≥ 3 ile kapandığından beri yaşanmış olmalı (`DesertedSinceFull`, eşikte ya da üstünde kapanan vardiya sıfırlar). Harekâtın başındaki tek bir ayrılmadan sonra yalnız ölümlerle 3'ün altına inen sığınak terk ile bitmez (soru 6'nın ruhu; son metni "insanlar birer birer kapıdan çıktı" der) |
| Aşama 5 · K8 "kadro" tahliye nedeni | İnceleme sonrası: terk sayacı **işliyorsa** (`UnderstaffedCounting`: terk açık, ayrılma döneminde, canlı < 3 ve sayaç > 0 ya da bu kapanışta işleyecek). Ayrılmadan sonraki ilk eksik vardiya da tahliyedir; fiş "KADRO EKSİK n/3" sayacı bu kapanıştan sonraki değeriyle yazar, son kapanışta "bu kapanışta sığınak boşalır" ekler |
| Aşama 5 · K5 kayıt | Sürüm 17 (aşağıda §6 "Sürüm") |
| Aşama 5 · K6 son şalter | Olaylardan bağımsız, kartlı yolda geçerli; açık olay, kilitli plan ve basılmış fiş engel değil. Eski oda yolunda yok |

1. **Kit kilidi.** Devresi kalmayan oyuncu bir kit sahibini kaybedince baskına çıkamıyor; aç kapanışta üretim de
   duruyor. İki küçük kural önerilir ve bu işin ön koşulu sayılmalıdır: aç kapanışta eksik temel rol kitinin üretimi
   durmaz; hiç bulunmayan rol kiti devresiz, yalnız metalle üretilebilir. Alınsın mı? Alınmazsa kilidin tek çıkışı
   aday getirileri ve `takas` olur, `terk` payı yükselir.
2. **Erzak baskısı.** Açlık olayları ve `terk` iyi giden harekâtta da görünsün isteniyorsa bir sayı değişmeli (ör.
   `boardLootSupplies` 6 → 4; hedef “iki ikmal atlanırsa `erzak_azaliyor`”). Değişsin mi, yoksa açlık kolu yalnız
   kötü giden harekâtın kolu mu kalsın?
3. **Tecrit kesin mi?** Bu belge tecridi olasılıklı yaptı (sızmacıyı %60/%85 bulur, insanı temize çıkarmaz); çünkü
   kesin sonuç veren tecrit, kapıdan kaldırılan “robot mu insan mı” testini üç kişi-vardiya bedelle içeri taşır ve
   “ilk izde tecrit” tek doğru olur. Alternatif: tecrit kesin kalır ama pahalılaşır (temiz çıkan insan için güven −1
   ya da daha uzun süre). Hangisi?
4. **Zincir olayları ve “2–3 vardiyada en çok bir olay”.** Bu belgede iz, ikinci iz ve sabotaj kâğıtları aralık
   bütçesinin dışındadır (güvenlik bekletilmez, 12–15 hedefi tutar). Sıkı okumada aynı 2–3 vardiyada iki kâğıt
   görülebilir. Kabul mü, yoksa zincir de aralığa mı uysun (o zaman olağan olay sayısı düşer)?
5. **İfşanın ara bedeli ve azalması.** İfşa 3 ve üstündeyken alarmın hızlanması ve sorgunun −1'i onaylanan kararlarda
   yok, bu belgenin eklemesidir. Kalsın mı? İfşa başka bir yolla (ör. sunucu baskını) azalsın mı?
6. **Ölümle eksilen kadro.** Yalnız ölümlerle 3'ün altına inen sığınak da belirli bir süre sonra kendiliğinden bitsin
   mi? Bu belgede bitmez; acil aday ve son şalter vardır.
7. **Son şalter.** Odadaki kesin yeri; en erken vardiya (öneri 5, rehber sürerken kapalı); “tahliye” çeşidinin
   yenilgi sayılması; `salter`, `dusus`, `terk` için başarım olacak mı?
8. **Olay kâğıdı** masadaki evrak tepsisinde mi (öneri), tezgâhta mı dursun?
9. **Kurucu izi.** Kurucular iz bırakmıyor; bu, “yeni gelen iz bırakır” bilgisini oyuncuya verir. Kabul mü?
10. **Kefil.** Kefil olan kurucu için bir sonuç olsun mu (kefil olduğu kişi sızmacı çıkarsa)? Bu belgede kefil yalnız metindir.
11. **Bekleyen grup.** Aday grubu beklerken fiş basılabiliyor (uyarıyla). Olay gibi kararı zorunlu mu olsun?
12. **İkmal ve Bakım yuvaları** geri gelecek mi? Gelirse aday konuları yediye çıkar, `agir_yarali` bakımcıyla çakışır.
13. **Eski kapı sonu `sizma`** tabloda duruyor ve anlamca `dusus`'a yakın. Kartlı yolda hiç üretilmiyor; silinsin mi?
14. **`anma` seyrek açılıyor** (aşama 3 ölçümü). Tetik yalnız baskının hemen ardından gelen kapanışa bakıyor; o kapanışta
    `kavga` ve `agir_yarali` önce geliyor, aralık da pencereyi sık kapatıyor (600 tohum, 831 ölümlü baskın: anma %14).
    Anılmamış ölüm birkaç kapanış beklesin mi, anma kavganın önüne mi alınsın? Aşama 8 ölçümüyle karar verilir.
    **Aşama 8:** olayların %8'i `anma` (sunum kuralıyla), her harekâtta ölüm var ve anma görülüyor; tetik değişmedi.

## 12. Doğrulanmayanlar

- Aşama 8'de sayılar botla ölçüldü (§8 "Ölçüm sonuçları"); bot insan değildir, gerçek oyuncuyla sınanmadı. Elle sınamanın iki
  harekâtı (bilinçli ihmal, açlık) yapılmadı.
- **İyi oyuncu için fazla kolay mı** ölçülmedi, yalnız yönü görüldü: RaidBot'a iki kat seçenek verilince (`ALT=16`, karışık+ucuz,
  40 tohum) zafer %65 → %80, dagilma %25 → %15, zafer uzunluğu medyanı 48,5 → 43,5, temiz ton %8 → %13. Bot yarım tayın ve takas
  kullanmıyor, kapı ve olay kararları sabit kurallı; insan oyuncuda zafer daha da yüksek, açlık ve terk daha düşük çıkabilir. Elle
  sınamada bu noktaya ayrıca bakılmalı.
- Gölgeli ve temiz ton bandı bot politikalarıyla ölçüldü; oyuncunun bilinçli riski (izi görmezden gelip sızmacıyı içeride tutmak)
  ölçülmedi.
- Kayıt uyumluluğu (eski dengeyle oynanmış kaydın yeni kodla aynı oyuna dönmesi) baskınsız 24 kapanışla ve sentetik sonuçlu
  baskınlı 24 harekâtla (`ShelterEventTests.Play`, en çok 16 baskın, en çok bir özgür bölge) sınandı; kademe 3'e ulaşan uzun bir
  harekâtla sınanmadı. Mekanizma genel (bütün denge alanları kayda yazılır), risk düşük.

**Kapananlar** (aşama 8 incelemesi, 2026-11-15):
- "Hep yolla" ve "al ve yolla" sunumun kuralıyla 80 tohumla ölçüldü (§8).
- Bugünkü harekâtın ölçüm değerleri (uzunluk, ölüm, en düşük güven, açlık) aşama 8'de yeniden üretildi.
- Tezgâh eni üç aday kartına yetiyor (aşama 2: `Rev3CandidateViewTests.Layout_TwoAndThreeCards_Centred_NoOverlap`, sahnede üç kart
  yüzü).
- Yetim kabul çevirileri silindi ve `LocalizationTests` geçiyor (aşama 7: 188 çeviri).
- Silinecek sunum testleri ayrıldı ve silindi ya da yeni yola taşındı (aşama 7).
- Kapı ve olaylar kapalıyken tohuma kilitli altın değerler kaymıyor (`shelterEventsEnabled = false` kuran testler aşama 1'den beri
  geçiyor).
- Yarım tayının baskın kadrosuna etkisi testli (aşama 3: `ShelterEventCaseTests.HalfRation_AfterCookSaving_FourClosings_NoHeal_RaidHpRestored`).
- Son şalter ayrı bileşen ve model olarak uygulandı (aşama 6: `SiginakLastSwitch`, `LastSwitchViewBuilder`).
