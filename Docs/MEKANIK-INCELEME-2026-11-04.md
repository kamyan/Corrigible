# Mekanik incelemesi ve geliştirme fikirleri

_2026-11-04 · Güncel çalışma ağacına göre tasarım değerlendirmesi. Bu belgede önerilen mekanikler uygulanmadı._

## Dayanak ve değerlendirme sınırı

Mevcut oyunda bir baskın/bir vardiya, güven ve alarm, karşı saldırı, beş uzmanlı kadro, yaralar, plakalar, yedekler,
kaçış, altı türde 30 harita, bölge özellikleri ve olayları, kapı ziyaretçileri ve zincirleri zaten var. Yeni fikirler bu
temelin üstüne kuruluyor. Mekaniklerin kapsamı koddan doğrulandı; tek doğru strateji, tekrar hissi ve zorluk üzerindeki
etkileri oyuncu testinde sınanacak hipotezlerdir. Bu çalışma yeni bir denge ölçümü değildir.

Önceki incelemede geçen sınırsız güvenli bekleme, aynı vardiyada bütün baskınları yapma ve bölge kimliğinin olmaması
güncel durum için geçerli değildir. Sığınak çıktılarının baskına etkisi de gerçek; geliştirilecek alan yatırım kararlarının
çeşitliliği ve kişisel anlamıdır.

## 1. İkmalde ihtiyaç seçimi

**Mevcut sınır:** Her bölgenin ilk iki sektörü ikmal, üçüncüsü sunucudur. İkmal başarıları aynı kaynak paketini verir.
Taktik çözüm değişirken oyuncunun o sefer neyi kurtarmaya gittiği fazla değişmez.

**Öneri:** Görev öncesi yük önceliği: erzak, malzeme veya insan tahliyesi. Bir kazanım yükselirken ötekiler azalır;
kaçışın bedeli ve sivil sonuçları sürer. Mevcut ikmal tahtası ilk denemede korunabilir.

**Yeni karar:** Açlığı şimdi gidermek mi, bir oda yatırımına ulaşmak mı, yeni sakinler için kapasite ayırmak mı?

**Küçük deneme:** Tek bir ikmal sektöründe iki öncelik: erzak ve malzeme. Rapor önceden seçilen yükü gösterir.
Kazanımlar dengelenmeden üçlü seçime genişletilmez. Bir seçeneğin diğer kaynaklara çevrilerek her durumda üstün
olması ve reddedilen nüfusun belirsizce kaybolması önlenmeli.

**Kaynak:** `Core/CampaignLiberation.cs:83,243`, `Core/ShelterLogic.cs:134`.

## 2. Sığınakta farklı gelişim yolları

**Mevcut sınır:** Oda durumu kimlik, seviye ve görevli sayısından oluşur. Kapasite ve iş gücü artar; bütün odalar aynı
özgür bölge eşikleriyle yükselir. Atölyenin Madenci ve Sis uzmanını açması yeni eylem kazandıran güçlü örnektir.

**Öneri:** Temel oda işlevi korunur; seviye 2'de bir uzmanlaşma seçilir. Telsizde alarm baskılama veya baskın hazırlığı
için istihbarat; revirde saha ekibinin tedavisi veya kapıdaki siviller için gözlem kapasitesi. Uzmanlaşma her vardiya
ücretsiz değiştirilmez; gerektiğinde bedelli geri dönüş yolu bulunur.

**Yeni karar:** Hangi tarz direnişi destekleyeceğim; hangi faydadan vazgeçiyorum?

**Küçük deneme:** Yalnız telsizde iki kol. İstihbarat, gelecek baskında bir hazırlık tercihi hakkında bilgi verir;
oyuncunun normalde görmesi gereken düşman niyetlerini gizleyip sonra geri satmaz. İşlevsiz bir kolu bırakmak oyuncuyu
geri dönülmez biçimde kilitlememeli.

**Kaynak:** `Core/ShelterState.cs:26`, `Core/Simulation.Shelter.cs:33`, `Core/Teams.cs:28`.

## 3. Cephe seçiminin kalıcı getirisi

**Mevcut sınır:** Bölge özelliği, imza makinesi ve olayı taktiksel kimlik sağlıyor. Özgür bölgenin düzenli fon katkısı ve
tamamlama ödülü ise bölgeden bağımsız. Rota seçimi ağırlıkla taktik koşulları ve harita komşuluğunu değiştiriyor.

**Öneri:** Özgürleştirilen bölgede bir direniş hücresi açılsın. Hücrelerin malzeme desteği, tedavi desteği veya kurye
istihbaratı gibi farklı rolleri olsun. İlk sürümde yalnız bir hücre etkin çalışır; işletmek personel veya erzak ister.

**Yeni karar:** Daha rahat ilerleyeceğim cepheyi mi, sığınağın ihtiyacını karşılayacak zor cepheyi mi seçiyorum?

**Küçük deneme:** Üç bölge, üç destek türü, tek etkin hücre. Tek seferlik özgürleştirme kazanımı yeniden işgalle tekrar
üretilmez; etkin destek işgalde durur. İstihbarat bir sonraki hücrenin yerini de önceden açıklamalı; seçim kör yapılmamalı.

**Kaynak:** `Core/CampaignLiberation.cs:70,102,243`, `Core/Economy.cs:30`.

## 4. Baskınsız vardiyada bilinçli toparlanma

**Mevcut sınır:** Bekleme gelir, iyileşme ve üretim sağlar; alarmı da artırır. Oyuncunun baskın dışındaki aktif kampanya
eylemleri büyük ölçüde oda/personel ve rastlantısal ziyaretçilerin seçenekleriyle sınırlıdır.

**Öneri:** Bir vardiyayı baskın yerine belirli bir sorunu çözmeye ayırmak: örneğin sivil onarım. Fon ve malzeme harcanır,
güven toparlanır; o vardiya baskın yapılamaz ve alarm normal ilerler. Karar sonrası aynı vardiyada ikinci faaliyet
yapılamaz. Beklemek kendiliğinden alarm düşürmez.

**Yeni karar:** Cepheyi ilerletmek mi, kırılgan toplumsal desteği pahalı ama öngörülebilir biçimde onarmak mı?

**Küçük deneme:** Yalnız sivil onarım. Yeni kaynak eklenmez. Kullanım sınırı ve azalan verim, harekâtı sonsuza kadar
uzatmayı kârlı kılmamalı. Kapı yardımları ve sunucu baskınının mevcut toparlanma faydalarıyla birlikte ölçülmeli.

**Kaynak:** `Core/Simulation.cs:63`, `Core/Balance.cs:33`, `Core/DoorCards.cs`.

## 5. Karşı saldırıya karşı hazırlık

**Mevcut sınır:** Alarm görünür; eşik aşılınca son geri alınan sektör otomatik yeniden işgal edilir ve güven düşer.
Oyuncu alarmı yönetebilir, fakat belirli bir kazanımı savunmak için ayrı bir hazırlık kararı veremez.

**Öneri:** Yaklaşan saldırı için bir savunma hazırlığı: malzeme ve görevli bağlayıp tek bir sektörün kaybını önlemek.
Görevli o süre atölyede veya revirde çalışmaz. Hazırlık bir kullanımlıktır; bütün cepheyi süresiz güvenceye almaz.

**Yeni karar:** Son kazanımı tutmak için hazırlıktan fedakârlık mı, geçici sektör kaybını kabul etmek mi?

**Küçük deneme:** Tek savunma fişi; yeni bir savunma tahtası yok. Saldırının hedefi, zamanı ve hazırlığın neyi
korumadığı önceden görünür olmalı. Yeniden özgürleştirilen bölgenin mevcut fon/güven ödülleriyle oluşan teşvik ayrıca
hesaplanmalı; kaybet-kazan tekrarı temel ilerleme yoluna dönüşmemeli.

**Kaynak:** `Core/CampaignLiberation.cs:259,315,327`.

## 6. Kapıda kanıta dayalı inceleme

**Mevcut sınır:** Gözetleme ve interkom ipucu verir; ziyaretçi kartına bağlı aç/açma ve kaynak/hikâye seçenekleri vardır.
Belge alanları, kayıt karşılaştırması veya güncel kabul talimatı mekanik olarak bulunmaz. Zincir hikâyeler zaten mevcut.

**Öneri:** Geçiş kâğıdı, kayıt defteri ve tek etkin vardiya talimatı. Oyuncu iki bilgiyi işaretleyerek çelişkiyi sorar.
Kimlik doğruluğu, kabul şartlarına uygunluk ve yardım kararı ayrı değerlendirilir. Gerçek bir insanın evrakı eksik olabilir.

**Yeni karar:** Kanıt yeterli mi; istisna yapmanın sığınağa maliyeti kabul edilebilir mi?

**Küçük deneme:** Üç alanlı belge, bir talimat, altı varyant. Kabul/ret ve dışarıdan yardım mevcut kaynaklara bağlanır;
gözetim seçeneği ancak kapasite/serbest bırakma kuralıyla eklenir. Ölümcül vakada ulaşılabilir ayırt edici kanıt bulunmalı;
doğru incelemeden sonra gizli şansla ölüm üretilmemeli. Kapı sıklığı ilk denemede artırılmaz.

**Kaynak:** `Core/Door.cs:52`, `Core/DoorCards.cs:42`.

## 7. Oyuncunun alışkanlıklarına cevap veren YZ

**Mevcut sınır:** Makineler hedefe ve kaçış yoluna göre eşgüdümlü plan yapıyor. Bu gelişmiş taktik akıl, oyuncunun
baskınlar boyunca tekrar ettiği davranışları öğrenen bir kampanya hafızası değildir.

**Öneri:** Son birkaç baskındaki gözlemlenebilir alışkanlıklardan biri için geçici karşı doktrin. Örneğin yoğun EMP
kullanımından sonra makineler daha seyrek yerleşmeye çalışır. Teleks bunu baskından önce bildirir. Oyuncu davranışı
değiştirince karşılık zamanla söner; aynı anda tek doktrin çalışır.

**Yeni karar:** Alışkanlığımı sürdüreyim mi, kadroyu değiştireyim mi, rakibi farklı bir hazırlığa mı yönelteyim?

**Küçük deneme:** İki gözlem ve iki karşılık; yalnız mevcut harekât içinde hafıza. Oyuncunun gelecek hamlesini okuma,
sonradan niyet değiştirme veya bir yeteneği tamamen geçersiz kılma yok. Hedef oyuncuyu başarılı olduğu için sürekli
cezalandırmak değil, okunabilir bir karşı oyun oluşturmak.

**Kaynak:** `Core/Baskin.cs` içindeki gelişmiş planlama, `Core/CampaignLiberation.cs:174`, `Core/WorldState.cs`.

## 8. Uzmanlarda kişisel gelişim ve topluluk bağı

**Mevcut sınır:** Uzmanın kalıcı durumu rol, yara ve yatış süresinden oluşuyor. Yeni roller açılıyor; bir rolün zamanla
farklı bir oyun tarzına dönüşeceği seçimler yok. Sivil nüfus da oda kapasitesi ve personel sayısı olarak tutuluyor.

**Öneri:** Tek bir uzmana yatay uzmanlık seçimi; kapıdan kabul edilen tanıdık bir kişinin bir odanın sorumlusu olması.
Analist için kurtarma odaklı veya düşman konumu bozma odaklı kanca gibi karşılıklı ödünleşimler düşünülebilir. Oda
sorumlusu ayrı envanter gerektirmez; bir isim, yaklaşım ve mevcut kaynaklarla karşılanan seyrek proje yeterli olabilir.

**Yeni karar:** Bu kadroyu ve bu topluluğu hangi yönde yetiştiriyorum?

**Küçük deneme:** Yalnız Analistte iki seçenek veya yalnız telsizde bir tanıdık sorumlu; ikisi aynı ilk denemeye alınmaz.
Gelişim başarılı bölge kilometre taşından gelir; tekrar baskın ve kasıtlı kayıp deneyim toplama aracına dönüşmez.

**Kaynak:** `Core/WorldState.cs:32`, `Core/ShelterState.cs:8`, `Core/DoorCards.cs`.

## Daha büyük ikinci tur fikirleri

- **Baskınların birbirini değiştirmesi:** İkmal sırasında isteğe bağlı bir röleyi kapatmak sonraki sunucu baskınında
  takviyeyi azaltır; fazladan iş dönüş güvenliğini riske atar. İlk deneme tek bölgedeki iki görev arasında bir bağ olur.
  Görevin asıl kazanma koşulu ve kaçış korunur; yan hedefi yapmamak cezaya dönüşmez.
- **Finalde birleşen beceriler:** Son çekirdeğin mevcut özel dolabı korunur; iki önceden öğretilmiş eylemi birleştiren
  aşamalı final denenebilir. Zorunlu bir uzman ikilisi veya finalde ilk kez görülen ölümcül kural kullanılmaz.
- **Halef yönetici:** Yönetici öldüğünde, sığınak hâlâ ayaktaysa yeni biri mevcut krizi devralabilir. Odalar/bilgiler
  miras, kişisel yönetici özellikleri geçici olur. Ölüm kaynak yenilemez. Sığınağın tümüyle çökmesi ayrı son olarak kalır.
  İlk sürüm yalnız bir ölüm türü ve tek halef hakkıdır; bütün sonları otomatik devire dönüştürmek geniş kapsamlıdır.

## Deneme sırası ve kabul ölçütleri

| Sıra | Paket | İlk sınama |
|---|---|---|
| 1 | İkmal önceliği | Aynı koşulda her oyuncu aynı yükü mü seçiyor; tercih sığınağın durumuna göre değişiyor mu? |
| 2 | Telsizde iki uzmanlaşma | Seçilen kol sonraki görev planını değiştiriyor mu; karar anlaşılır mı? |
| 3 | Üç bölgede destek hücresi | Rota yalnız kolaylık için mi, hedeflenen desteğe göre de mi seçiliyor? |
| 4 | Bir bedelli vardiya faaliyeti | Kötü gidişattan çıkış sağlıyor mu; sürekli beklemek üstün strateji oluyor mu? |
| 5 | Kapı belge prototipi | Hatalar bilgi eksikliğinden mi, okunurluktan mı, kuralları uygulayamamaktan mı kaynaklanıyor? |
| 6 | Tek YZ karşı doktrini | Oyuncu YZ'nin tepkisini fark edip karşı plan üretebiliyor mu? |
| Sonra | Karşı saldırı hazırlığı, uzman kimliği, yan hedefler ve halef | Önceki kararların sayısını ve oyun süresini şişirmeden anlamlı farklılık yaratıyor mu? |

İlk üç paket ayrı ayrı denenmeli. Küçük bir eşleştirilmiş koşu için aynı başlangıç tohumları ve benzer oyuncu
yaklaşımları kullanılabilir. Vardiya/bölge, tercih dağılımı, boşa giden ödül, yara/yatış, güven/alarm, yeniden oynanan
sektör, başarısızlık nedeni ve gerçek oturum süresi kaydedilir. Başarı oranı tek başına eğlence veya karar derinliği ölçmez.

Örnek bağlantı: malzeme ihtiyacı → ikmal yükü seçimi → telsiz uzmanlaşması → bilgi sağlayan bölgeyi hedefleme →
bilgiye göre kadro seçimi. Bu zincir mevcut kaynakları ve sahneleri kullanır; her halkası ayrı denge denemesidir.

## Uygulama sınırları

Uygulamaya geçilirse yeni durum/komutlar Core'da, denge değerleri Balance'da tanımlanır. Başarılı oyuncu eylemi
kaydedilir ve Replay'e eklenir; mevcut komut sonucu/RNG sırası değişirse GameRecord.CurrentVersion artırılır.
Önerilen kural metinleri Türkçe/İngilizce çevrilir. Sahne/prefab kaynakları, serialized bağlantılar ve mevcut taktik
niyet önizlemesi korunur. Bu belge tek başına oyun kodunda veya kayıt biçiminde değişiklik yapmaz.
