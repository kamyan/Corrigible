# Son Şalter — Bütünleşik Ekonomi Sistemi v 1

14 Ekim 2026. Model C#/JS çekirdeğine, Unity BalanceConfig asset'ine, analist masasına ve Main sahnesine uygulandı.
Kayıt biçimi 3; eski 1/2 ekonomi profilleri korunur. 196 EditMode testi ve kontrollü Play senaryoları doğrulandı.
Geçici aday deneyleri ile uygulama sonrası bot ölçümü ayrı tutulur; insan oynanış dengesi ve kapsamlı olay/kampanya
bileşimi sonraki denge turunun konusudur. Uygulama kanıtı: `BUTUNLESIK-EKONOMI-UYGULAMA-SONUCLARI.json`.

Bu belge, önceki fon sistemi ve operasyon/Hizalama taslaklarını tek bir adayda birleştirir. Toparlanma fiyatları
yeniden ayarlandı: altyapı 2 fon, diplomasi 8 fon +2 SG. Önceki 8 fonluk onarım ve 20 fonluk diplomasi bu adayın
fiyatları değildir. İlk sürüme yeni başarı hibesi eklenmez.

## 1. Amaç ve başlangıç koşulları

Oyuncu aynı fonla üç karar verir: hemen kullanılabilen operasyon kapasitesi satın almak, uzun Hizalama
projesini finanse etmek ve hasarlı dünyanın gelirini/işbirliğini toparlamak. Yeni para birimi yoktur.
Siyasi güç sert müdahaleler ve diplomasi için kullanılmaya devam eder.

| Başlangıç ayarı | Yeni sistemdeki değer |
|---|---:|
| Fon | 60 |
| Siyasi güç | 5 SG |
| Saha ekipleri açıkken ekip sayısı | 3 |
| Varsayılan dünya ölçeği | 1,5 |
| Varsayılan Hizalama rezervi | 15 fon |
| Bütçe Anlaşması seviyesi | 0 |
| Kriz içi operasyon seviyeleri | Satın alınmamış |

Kampanyanın başlangıç/yükseltme etkileri bu tabanın üstüne uygulanır. Saha ekipleri kapalı modda saha ekibi
oluşturulmaz; toparlanma görevi yine çalışır. Yeni ayarlar yalnız yeni sistemle başlayan krizleri etkiler.
Eski kayıtların başlangıç koşulları veya ödeme kuralları bu varsayılanlarla sessizce değiştirilmez.

## 2. Brüt fon geliri

Gelir gerçek bölgesel ekonomi ve güvene bağlı kalır. Çevrimdışı veya ittifak dışı bölgenin katkısı sıfırdır.
Karantina bölgesel katkıyı doğrudan sıfırlamaz; ekonomi ve güveni düşürerek sonraki katkıları azaltır.

```text
k = dünya ölçeği
A = YZ tespit edilmişse 1, değilse erken uyarı katsayısı (normalde 0,4)
M = Bütçe Anlaşması seviyesine göre 1 / 1,15 / 1,35
R = Σ(online ve ittifakta bölgenin ekonomi × güven / 100) / fonDiv × A / k

Merkez ödeneği       = 0,75 / k
Bölgesel taban katkı = R × 0,8
Bütçe anlaşması     = R × 0,8 × (M − 1)
Brüt gelir          = 0,75 / k + R × 0,8 × M
```

Normal `fonDiv=150` korunur. Merkez ödeneği ilk tespitten önce ve sonra aynıdır; Anlaşma veya Uluslararası
fon bonusu almaz. Dünya duraklatıldığında, olay/hediye/baskın yüzünden gün ilerlemediğinde ve oyun bittikten
sonra yeni günlük ödeme yapılmaz. Merkez ödeneği gerçek dünya gün adımıyla ödenir; saat veya gerçek saniye
başına ayrı ödeme yoktur.

Varsayılan k=1,5, başlangıç ekonomi toplamı 765 ve bütün bölgelerde güven 70 örneği:

| Durum | Brüt gelir, fon/gün |
|---|---:|
| Tespit öncesi, anlaşmasız | 1,2616 |
| Tespit sonrası, anlaşmasız | 2,4040 |
| Tespit sonrası, Anlaşma I | 2,6896 |
| Tespit sonrası, Anlaşma II | 3,0704 |

Bu tablo sabit sağlıklı dünya hesabıdır; tohumdan gelen farklı güven, karantina, medya zararları, bölge
kaybı veya hediyeler gerçek oyundaki tutarı değiştirir. Anlaşmalar tespitten önce satın alınamaz.

### Bütçe Anlaşmaları

| Kademe | Peşin bedel | Önkoşul | Bölgesel katkı çarpanı |
|---|---:|---|---:|
| I | 25 fon | YZ tespit edilmiş | 1,15 |
| II | 40 fon | I alınmış | Toplam 1,35 |

II, I'in yerine geçer; `1,15 × 1,35` uygulanmaz. Her kademe kriz başına bir kez alınır. Bonus satın alındığı
anda canlı gelir tahminine yansır; günlük ödeme sonraki gün adımında yapılır. İki kademe toplam 65 fon ister.
Sabit sağlıklı örnekte I'in geri dönüşü yaklaşık 88, II'nin kendi ek getirisiyle geri dönüşü 105, birlikteki
65 fonun geri dönüşü 98 gündür. Bunlar garanti kazanç veya araştırma süresi tahmini değildir.

## 3. Anlık operasyon geliştirmeleri

Operasyon geliştirmesi peşin ödenir ve komut anında etkinleşir. Hizalama sürerken de alınabilir; uzun projenin
kimliği veya ilerlemesi sıfırlanmaz. Aynı geliştirme veya kademe tekrar alınamaz, fon eksiye düşürülemez.
Operasyonlarda araştırma süresi ve ilerleme sabotajı yoktur. Kriz sonrasında sıfırlanırlar; kampanya
yükseltmeleri ayrı ve kalıcıdır.

| Dal | Düğüm | Fon | Önkoşul | Satın alma sonrası toplam etki |
|---|---|---:|---|---|
| Tespit | Anomali I | 12 | Yok | Otomatik tespit ihtimali ×1,5 |
| Tespit | Anomali II | 18 | Anomali I | Otomatik tespit ihtimali ×2 |
| Tespit | Doğrulama Ağı | 40 | Anomali II ve bu krizde medya kuralları açık | Başlık doğrulama; başlık başına 2 SG; sahte kaynak işaretleme ve karalama zararını geri alma |
| Savunma | Ağ Güçlendirme I | 16 | Yok | Bölgeler arası yayılma −%15; Hizalama sabotaj ihtimali −%25 |
| Savunma | Ağ Güçlendirme II | 24 | Güçlendirme I | Bölgeler arası yayılma toplam −%30; sabotaj ihtimali toplam −%50 |
| Savunma | Kuantum Karantina | 60 | Güçlendirme II | Mevcut karantinalar uydu geçişini keser; Uydu Ağı'nın kara karantinasını zayıflatmasını engeller |
| Politika | Kamu İletişimi I | 15 | Yok | İlgili müdahale/olay güven kayıpları −%25 |
| Politika | Kamu İletişimi II | 20 | İletişim I | Aynı güven kayıpları toplam −%50 |
| Politika | Analog Yedekler | 50 | İletişim II | Bir kez bütün bölgelerde bağımlılık −20; Analog veto eşiği; saha denetimi ve denetimli hediye kabulü |

Dokuz düğüm toplam 255 fon; Bütçe Anlaşmaları bu toplama dahil değildir. Kademeler başlangıca göre toplam
etkiyi belirtir: Anomali II ×3 değildir; Güçlendirme ve İletişim I/II de üst üste çarpılmaz.

Anomali izleme ağı kurmaz ve tespit ihtimalini yalnız mevcut tespit koşulları içinde artırır. Güçlendirme
bölge içi büyümeyi azaltmaz. Kuantum yeni karantina oluşturmaz; karantina yine 8 SG ister. Uydu yeteneğinin
henüz bulunmaması Kuantum satın alma engeli değildir; kartta hazırlık olduğu açıklanır.

Medya kuralları öğrenme aşamasında kapalıysa Doğrulama satın alma kapalıdır: “Bu krizde medya tehdidi kapalı”.
Medya kuralları açık, fakat YZ henüz Medya Kontrolü almamışsa hazırlık olarak satın alınabilir.

Kamu İletişimi mevcut güven koruma kapsamını korur; medya karalaması ve perde yırtma şokuna uygulanmaz.
Analog satın alma, denetimin eylem maliyetini 15→20 fon yapar; 1 SG ve ekip gerekliliği devam eder. Denetimli
hediye kabulü ayrıca 20 fon ister. Bu maliyet artışı Analog kartında gösterilmelidir.

İllüzyon operasyon satın alımını tek başına engellemez. Oyuncu İllüzyon başladıktan sonra da İletişim I→II→Analog
alabilir; ardından erişilebilir bir bölgede saha denetimiyle perdeyi yırtabilir. Satın almak perdeyi kendiliğinden
yırtmaz. Fon, önkoşul, siyasi güç, boşta ekip ve bölgesel erişim yine gerçek uygulama sınırlarıdır.

## 4. Hizalama projesi ve finansman kontrolü

| Aşama | Katalog fon bedeli | Nominal iş günü | Önkoşul | Tamamlanma katkısı |
|---|---:|---:|---|---|
| Yorumlanabilirlik | 68 | 25 | YZ tespiti ve en az bir gözlem | Çekirdeğin o anki konumu ve o anki yetenek tahmini |
| Düzeltilebilirlik | 98 | 30 | Yorumlanabilirlik | Bundan sonraki Tekillik artışı ×0,75 |
| Değer Kilidi | 165 | 38 | Düzeltilebilirlik | Hizalama zaferi |

Toplam 331 fon/93 nominal iş günü korunur. Sonraki aşama otomatik başlamaz; oyuncu başlatır. Operasyon
dallarının tamamlanması Hizalama önkoşulu değildir. Aynı anda yalnız bir Hizalama aşaması vardır.
Her aşamanın başlangıcında YZ tespiti ve en az bir gözlem koşulu denetlenir; tamamlanan önceki aşama
tek başına gözlemsiz başlangıç izni vermez.

Gözlem, izlenen ve YZ varlığı tespit edilmiş bölgedir. Mevcut sayım semantiği korunur: bölgeyi denetimle
temizlemek gözlemi silmez. Normal gözlem hızı 1/2/3/4 gözlemde %25/%50/%75/%100, dörtte tavan 1'dir.
İş ilerlemesi ve günlük tam hız fon talebi dünya ölçeğine bölünür. Dört sabit gözlem, k=1,5, yeterli fon ve
sabotajsız durumda zincir yaklaşık 140 simülasyon günüdür. Sabotaj veya fon kıtlığı bu süreyi uzatır.

```text
Tam hız talebi = aşama maliyeti / iş günü × gözlem hızı / k
Ödenebilir fon = max(0, gelir eklenmeden önceki fon stoku + o gün ödenen gelir − rezerv)
Gerçek ödeme  = min(tam hız talebi, ödenebilir fon)
İlerleme      = gözlem hızı / k × gerçek ödeme / tam hız talebi
```

Günlük gelir önce eklenir; sonra otomatik araştırma ödemesi yapılır. Rezerv yalnız bu otomatik ödeme için
tabandır. Oyuncu açık operasyon, müdahale veya toparlanma işlemiyle rezervin altına inebilir. Varsayılan rezerv
15; mevcut seçenekler 0/15/30/60 korunur. Rezerv elde olmayan fonu yaratmaz ve elle harcamayı engellemez.

### Duraklat / sürdür

- Duraklatılmış projede günlük ödeme, ilerleme ve araştırma sabotajı durur. Dünya, ekipler ve YZ gelişimi sürer.
- Aşama kimliği, ilerleme ve gerçek toplam ödeme saklanır; geri ödeme veya proje sıfırlama yoktur.
- Sürdür aynı aşamaya döner. Tekrar başlatma komutu veya ikinci bir aşama açma yolu değildir.
- YZ'nin Hizalama'yı fark etmiş olması duraklatmayla unutulmaz; Hızlanma tercihi için proje kimliği korunur.
- İllüzyon araştırmayı yine durdurur. Elle “sürdür” İllüzyon engelini aşmaz. Perde yırtıldıktan sonra oyuncunun
  elle duraklatma tercihi korunur; elle duraklatılmamış proje normal finansmanla devam eder.
- Araştırma ilerlemesini değiştiren olay kartları yalnız duraklatılmamış projede seçilir. Bekleyen kart
  çözülmeden duraklat/sürdür uygulanmaz; kart açılınca projeyi duraklatarak koşulu değiştirme yolu yoktur.

Sabotaj ilerlemeyi geri alır ve tekrar iş/ödeme gerektirir; gerçek toplam harcama katalog 331 fonu aşabilir.
Yorumlanabilirlik anlık analizdir; çekirdek sonradan kaçarsa sürekli konum takibi sağlamaz. Düzeltilebilirlik
birikmiş Tekillik'i düşürmez. Bu sınırlar kart katkılarında açıkça görünmelidir.

## 5. Altyapı desteği

| Kural | Karar |
|---|---|
| Peşin bedel | 2 fon |
| Başlangıç şartı | İttifakta, online, karantinasız; başlangıç ekonomisine göre en az 5 ekonomi açığı |
| Ekipler açıkken | Hedef bölgede boşta ekip; yolculuk ayrı yapılır |
| Ekipler kapalıyken | Aynı dört günlük bölgesel görev; ekip gerektirmez |
| Görev süresi | 4 simülasyon günü; dünya ölçeğiyle çarpılmaz |
| Kazanç | Tamamlanınca ekonomi+15; en fazla bölgenin başlangıç ekonomisine kadar |
| Eşzamanlılık | Bölge başına en fazla bir toparlanma görevi; kullanılan ekip başka görev alamaz |
| Sonuç zamanı | Dördüncü gün sonunda; artan katkı ilk kez beşinci günlük gelirde |
| Bitişte erişim geçersizse | Görev iptal, ekonomi kazancı yok, fon iadesi yok |

En az 5 açık mekanik başlangıç eşiğidir. Deney botunun yalnız en az 15 açıkta onarım yapması ayrı bir strateji
tercihidir; oyuncu için 15 şartı değildir. Tavan üstünde veya 0–4 açıkta düğme kapalıdır ve nedeni gösterilir.

Bitişte bölge yeniden online/ittifakta/karantinasız olmalıdır; ekipli görevde atanmış ekip de bulunmalıdır.
Görevin ortasında geçici erişim kaybı süreyi uzatmaz: sonuç anında geçerli koşullar denetlenir. Ekip normal
görev süresinin sonunda boşalır; başarısız görev ekibi ayrıca yaralamaz. Ekip kaybında görev sonuç vermez.
Manuel iptal ilk sürümde yoktur; oyuncu riskli bölgede emri vermeden sonucu ve iade olmadığını görür.

Görev sırasında doğal ekonomi toparlanması sürer. Tamamlama anında gerçek kalan açığa göre kazanç hesaplanır;
önizlemedeki+15 üst sınırdır. Hediye veya başka etki ekonomiyi başlangıç değerinin üstüne çıkarırsa görev
bu değeri aşağı indirmez. Peşin fon iadesi yapılmaz. Altyapı desteği güven/bağımlılık/sızmayı değiştirmez.

## 6. Diplomatik görüşme ve ittifaka dönüş

Bedel 8 fon+2 SG; ekip ve yolculuk gerekmez, komut anında sonuçlanır. Online, çevrimdışı, karantinadaki
ve ittifak dışı hükümetlerle görüşülebilir. Çevrimdışı bölgenin yeniden üyeliği gelirini açmaz; blackout
süresi bitene kadar katkısı sıfırdır. Karantina görüşmeyle kaldırılmaz.

- Üye bölgede güven+15, en fazla 70. Güven zaten 70 veya üstündeyse işlem kapalıdır; değer düşürülmez.
- İttifak dışı bölgede aynı artışa ek olarak sonuç güveni en az 35'e çıkarılır ve `Left=false` aynı komutta
  uygulanır. Örneğin 0→35 veya 18→35. Bu dönüş güvencesi düz+15 değildir; önizlemede gerçek sonuç yazılır.
- Başarılı görüşme o bölge için 20 nominal günlük bekleme başlatır: k=1,5'te 30 simülasyon günü.
- Bekleme bölgeseldir; başka uygun bölgeyle görüşme yapılabilir. Başarısız komut fon/SG harcamaz ve sayaç başlatmaz.
- Güven yeniden 20 altına düşerse bölge mevcut kuralla tekrar çekilebilir; görüşme kalıcı üyelik veya dokunulmazlık vermez.
- Görüşme ekonomi, sızma ve bağımlılığı değiştirmez. Ekiplerin ve bölge müdahalelerinin erişimi üyelik dönüşünden
  sonra açılır. Oyun sonu oluşmuşsa diplomasi oyunu yeniden canlandırmaz.

Peşin harcama ve üyelik dönüşü aynı kayıtlı işlem olmalıdır. Oyuncu dönüşten sonra aynı duraklatılmış gün
içinde ekip gönderebilir; gelir ise bir sonraki dünya gününde ödenir.

## 7. Kampanyayla bileşim

Kriz içi operasyon, Anlaşma, toparlanma görevi ve diplomasi beklemeleri yeni krizde sıfırlanır; kampanya
yükseltmeleri kalır. Yeni kriz yine taban 60/5 üzerine yükseltmeleri uygular; önceki krizin nakit stoku taşınmaz.

| Kampanya etkisi | Yeni sistemde bileşim |
|---|---|
| Ek bütçe | Başlangıç fonu 60→100; günlük gelir değişmez |
| Erken uyarı bütçesi | Tespit öncesi bölgesel katsayı 0,4→0,6; merkez ödeneği değişmez |
| Uluslararası fon | Açıklama ve matematik eşitlenir: `fonDiv /= 1,15`; bölgesel gelir tam+%15; merkeze uygulanmaz |
| Ucuz sensörler | Mevcut izleme maliyetine ×0,7 ve mevcut yuvarlama; operasyon düğümü fiyatlarını azaltmaz |
| Laboratuvar güvenliği | Sabotaj ihtimaline ×0,6; GüçlendirmeI ile toplam×0,45, II ile×0,30 |
| Hizalama ekibi | Gözlem başına hız 0,25→0,31; tavan 1 korunur; dört gözlemde ek hız yoktur |
| İletişim ofisi | Hediye reddinde diğer bölgelerde güven kaybı 0; SG bedeli 1; hedefteki yerel kayıp Kamu İletişimi çarpanıyla sürer |
| Acil durum yasası | Normal/Analog veto eşikleri 78/93; Analog kartı sabit 70→85 yazmaz |
| Dördüncü saha ekibi | Başlangıçta 4 ekip; ekipler açıkken uygulanır |

Sağlıklı 70 güven/k=1,5/BudgetII örneğinde Uluslararası fon ile toplam brüt gelir 3,456; tam hız Değer Kilidi
sonrası yaklaşık +0,561/gün olur. Aynı dünya güveni 60'a inerse brüt yaklaşık 3,034, net+0,139/gündür.
Bu bileşim kriz içi anlaşmaları önemsiz veya zorunlu yapıyor mu henüz kampanyada denenmedi.

Kampanya rakipleri, öğrenme aşamaları, oyuncu profilinden gelen karşı hamleler ve Unity olay kartları ayrıca
denenmelidir. Geçici bot tabloları bu mekaniklerin tamamını kapsıyormuş gibi sunulamaz.

## 8. Bütçe arayüzü ve oyuncu karar akışı

Bütçe görünümü gerçek stok ile gelir oranını ayırır. Ayrı alanlar:

1. Elde bulunan fon ve Hizalama rezervi.
2. Son ödenmiş günün merkez/bölge/anlaşma gelirleri, gerçek araştırma ödemesi ve net düzenli nakit akışı.
3. Bugünkü duruma göre sonraki günün gelir/ödeme tahmini; bunun tahmin olduğu etiketi.
4. Projenin tam hız fon ihtiyacı, gerçek finansman oranı, ilerleme, gözlem sayısı ve koşullu süre.
5. Peşin işlem defteri: operasyon, anlaşma, izleme, denetim, onarım, diplomasi, ekip, baskın, hediye ve olaylar.

Araştırma nominal ihtiyacı gerçek ödemeden büyükse iki sayı da görünür. Duraklatılmış veya İllüzyonla durmuş
projenin gerçek ödemesi 0'dır. Peşin harcamalar günlük brüt gelire veya günlük araştırma giderine eklenmez;
işlem defterinde ayrı gösterilir. Son günlük stok değişimi açıklanırken bu peşin işlemler de hesaba katılır.

Operasyon/anlaşma kartı satın alma sonrası fonu, rezervin altına düşüşü, önceki→yeni toplam kademe etkisini
ve yürüyen projenin finansman durumunu gösterir. “Hemen etkinleşir” ile “günlük gelirde sonraki ödeme” ayrımı
korunur. Araştırma pause düğmesi dünya saati duraklatma düğmesinden ayrı adlandırılır.

Bölge dosyasında onarım/diplomasi şartları, gerçek kazanç, bedel ve zaman açıkça yazılır. Onarımda ekibin
yolculuk süresi dört günlük göreve eklenir; ertelenmiş sonucu “hemen” diye anlatılmaz. Bitiş geçersizse
iade olmadığı emirde görünür. Diplomaside Left tabanı ve üyelik dönüşü ayrıca görünür.

Saha ekipleri penceresinde mevcut 45 fon/4 gün ekip kurma ve kayıptan sonra ikame yolu erişilebilir olmalıdır.
Yeni Desk'teki yalnız ekip fişleri gösterimi bu ekonomi için yeterli değildir. Hazır olmayan/yoldaki/görevli
ekiple ikinci görev yapılamaz. Fon eksiye düşürmeyen, önkoşulu açıklayan kapalı kartlar bütün yeni akışlarda
aynı komut kontrolüyle tutarlı çalışmalıdır.

Fon ve gerçek ödeme İllüzyonda da doğru kalır. Risk/sızma/Tekillik önizlemeleri Algı Katmanından okunur;
gizli gerçek sızma üzerinden “onarım güvenli”, “baskın garantili” veya fon hedefi koşulu üretilmez. Bölgesel
ekonomi, yerel güven ve üyelik için mevcut oyuncuya açık veri kapsamı korunur. Küresel sahte güven artışı
gerçek gelir artışı gibi gösterilmez.

## 9. İlk sürüm kapsamı ve deney sınırları

İlk sürümde başarı hibesi yoktur. Mevcut bağımlılık bedelli bağış/YZ teklifi kartları kendi kurallarıyla kalır;
bunların tek seferlik fonları düzenli gelir değildir. Hibe eklenirse ayrı denge turu gerekir.

Geçici JS deneyi karşılaştırmalı bir aday incelemesidir; yayın dengesi kanıtı değildir. Botlar operasyon,
bütçe ve toparlanma tercihleri için belirli bir strateji uygular. İnsan karar süresi, taktik baskın becerisi,
Unity olay kartları, öğrenme aşaması, kampanya yükseltmeleri ve gerçek UI okunabilirliği ayrıca doğrulanır.
Aynı tohum eski/yeni kuralların tamamen aynı RNG akışını sürdürdüğü anlamına gelmez.

Beklenen kontroller: başlangıç ops harcaması izlemeyi geciktiriyor mu; anlaşmalar her stratejide zorunlu mu;
rezerv 15 müdahale payı bırakırken Hizalama'yı aşırı yavaşlatıyor mu;2 fon onarım faydalı ama sınırsız gelir
kaynağı olmuyor mu;8 fon/2 SG dönüş zorluğu anlamsızlaştırıyor mu; Analog'u İllüzyonda alma yolu gerçekten
erişilebilir mi; pause sabotajı/YZ karşı hamlesini bedelsiz aşma hilesine dönüşüyor mu.

## 10. Ölçüm sonuçları

Ölçümün ana turu: 80 sabit tohum (17000–17079), üç rakip, mevcut modelde üç ve adayda dokuz bot politikası. Toplam **2.880 bot oyunu**: 720 mevcut, 2.160 aday. Saha ekipleri ve deterministik taktik baskın botu açık; k=1,5; öğrenmiş oyuncu profili, kampanya ve Unity olay kartları yok. Erken deneme turları aşağıdaki tablolara dahil edilmedi.

Legacy politikaları aynı karar kodu ve aynı tohum listesiyle iki modelde çalıştı. Yeni modelin varsayılan rezervi 15, eskisinin 0; operasyonların anlık olması ve diğer ekonomi kuralları birlikte değişti. Bu tablo yalnız gelir formülünün etkisini ayırmaz:

| Rakip | Eski usta bot kazanma | Yeni usta bot kazanma | Eski / yeni fon kıtlığı günleri |
|---|---:|---:|---:|
| Solucan | %72,5 | %75 | %33,51 / %31,65 |
| Manipülatör | %56,25 | %70 | %30,79 / %26,19 |
| Kâhin | %50 | %91,25 | %49,8 / %32,35 |

Fon kıtlığı, çalışan araştırmanın ödeme talebinin karşılanamadığı gün oranıdır; yalnız nakit göstergesinin sıfıra inmesi değildir. Yeni esnek politikalar otomatik duraklatma yapmadı; kıtlık duraklama diye gizlenmedi. Duraklatma komutu ayrıca kontrollü mekanik testinde sınandı.

Yeni araçları kullanan ayrı politikalar (her hücre 80 oyun; kazanma yüzdesi):

| Politika | Solucan | Manipülatör | Kâhin |
|---|---:|---:|---:|
| Hizalama; bütçe yatırımı yok, toparlanma var | %55 | %50 | %86,25 |
| Hizalama; geç bütçe yatırımı ve toparlanma | %57,5 | %46,25 | %86,25 |
| Hizalama; bütçe yatırımına erken öncelik | %65 | %50 | %95 |
| Geç bütçe yatırımı; toparlanma yok | %51,25 | %46,25 | %90 |
| Araştırmasız Silme; operasyon/toparlanma var | %48,75 | %62,5 | %42,5 |

Anlaşmasız ve araştırmasız politikalar da kazanabiliyor. Bütçe ve toparlanma harcamaları her rakipte aynı sonucu vermedi; kısa nakit, ekip meşguliyeti ve müdahale önceliği gerçek takas oluşturuyor. N=80 gruplarında küçük yüzde farkları birkaç oyundur; bu oranlar insan oyuncu başarı tahmini değildir. Kâhin karşısında eğitimli usta/finans politikaları çok güçlü çıktı; insan ve kampanya testinde özellikle bu karşı strateji kontrol edilmeli.

Pasif bot bütün modellerde ve üç rakipte 0 zafer aldı. Hiçbir ana tur oyunu 1500 gün sınırında kalmadı. Taktik bot insan başarısı değildir; baskın zamanı strateji günlerinden ayrı tutuldu.

Kontrollü mekanik sonuçları:

- Dokuz operasyonun toplam bedeli 255 fon; alımlar yürüyen Hizalama kimliği/ilerlemesi/Starved değerini korudu. Çift satın alma engellendi; Analog bağımlılık indirimi yalnız bir kez uygulandı.
- Sağlıklı gelir seviyeleri 2,4040/2,6896/3,0704 gerçek aday fundRate ile doğrulandı.
- Onarım 2 fon/+15 ekonomi/4 ekipgünü; güven 70 ve yeterli açık örneğinde fon açısından geri dönüş yaklaşık 100/79/62 gün (Anlaşma yok/I/II). İlkgelir 5.günde; 360.gündeki net ek fon yaklaşık 0,74/1,15/1,70. Doğal toparlanma ve ekip fırsat maliyeti nedeniyle bu anlık kâr değildir.
- Güven 0/Left örneğinde diplomasi 8 fon+2 SG ile 35 güvene ve üyeliğe döndürdü;30 simülasyon günlük bekleme tekrar komutunu engelledi.
- Ekonomi/güven tavanı üstünde değerler düşürülmedi ve no-op harcama kapandı.
- Pause ödeme/ilerlemeyi durdurdu; proje kimliği korunarak sürdürüldü.
- Bütün ana tur oyunlarında fon/SG negatif olmadı; son fon, başlangıç+gerçek gelir−bütün harcamalar muhasebesiyle uyuştu. Aynı aday ve mevcut senaryo tekrarları deterministikti. Ekip kimliği tekrarı ana turda görülmedi.
- Ölçüm instrumentationı 18 sabit senaryoda orijinal prototiple aynı sonuç/RNG imzası verdi. Ana Prototype/sim.js başlangıç ve bitişSHA-256 eşit; oyun kaynakları değiştirilmedi.

Kapsam sınırı: Legacy botlar haberde hint türü ve bazı gerçek bağımlılık verilerini kullanıyor. Yeni esnek politikalar alışveriş için shownGlobal kullanıyor; ortak hediye seçimi yardımcı kodu gerçek bağımlılık ortalamasını kullanmaya devam ediyor. Attentive adlı ek politika illüzyonu doğrudan fark eden üst senaryodur. Bu nedenle bütün botlar normal insan gibi bilgi edinmiş sayılmaz. Her politika bütün düğümleri sık almadı; düğüm erişimi/bedeli kontrollü testle, stratejik kullanımı bot politikasıyla ayrı değerlendirildi. Unity'ye özgü medya öğrenme kapısı, pause/olay etkileşimi ve düzeltilmiş kampanya bonusu bu JS turunda çalıştırılmadı.

Yeniden üretim: Temp/integrated_economy_build.py, Temp/integrated-economy/driver.js, final-settings.js ve mechanics.js. Sonuçlar final-current.jsonl, final-candidate.jsonl, mechanics-results.jsonl dosyalarında; kalıcı özet bu belgededir. İlk keşif turları farklı otomatik pause politikası kullandı ve nihai tablolardan çıkarıldı.

Ana tur ayarları, bütün grup sonuçları ve kontrollü mekanik çıktıları kalıcı olarak
[ölçüm verisinde](BUTUNLESIK-EKONOMI-SONUCLARI.json) saklandı. Geçici deney kaynakları oyun kodunun parçası değildir.

## 11. Uygulama, kayıt sürümü ve doğrulama notları

Bu bölüm sonraki uygulamanın kontrol listesidir; uygulanmış değişiklik değildir.

- Ortak kurallar `Balance/BAL` değerlerinden okunmalıdır: gelir tabanı, bölgesel pay, kademe fiyat/çarpanları,
  rezerv başlangıcı, onarım bedel/etki/eşik/süre ve diplomasi bedel/etki/tavan/dönüş tabanı/bekleme değerleri.
  `BalanceConfig.asset` bunlarla tutarlı düzenlenir; kampanya runtime kopyasını değiştirir, ortak asset'i değiştirmez.
- Çekirdek kimlikleri ve mevcut Türkçe ASCII simülasyon kimlikleri korunur. Operasyon I/II state'i açık kademe
  olarak tutulur; eski `Done("guclu")` benzeri tam etki kontrolleri yeni kademe katsayılarıyla tutarlı yapılır.
- Satın alma, Anlaşma, onarım başlangıcı, diplomasi ve pause/resume `GameController → Simulation` public komutları
  üzerinden gider. Her başarılı oyuncu komutu `Rec` ile kaydedilir ve Replay'da karşılığı bulunur. Başarısız/no-op
  komut işlem defterine harcama yazmaz. Aynı gündeki komut sırası korunur.
- Otomatik görev tamamlanması/başarısızlığı günlük adımın deterministik sonucudur. Görev hedefi, bitiş günü ve
  ekip kimliği; diplomasi bekleme bitişi; operasyon/Anlaşma seviyeleri; araştırma pause tercihi ve gerçek ödeme
  bilgileri tekrar üretilebilir durum olmalıdır. Core'da zaman için `DateTime`, sayı için `float` kullanılmaz.
- Ayrı nakit işlem defteri geliri, gerçek araştırma ödemesini ve peşin işlemleri kaynağıyla tutar. Son ödenen gün
  görüntüsü komut sonrası canlı tahminle üstüne yazılmaz. Araştırma toplam gerçek ödemesi sabotajla silinmez.
- Mevcut kayıt sürümü 2, altı operasyonu zamanlı araştırma diye oynatır. Yeni ekonomi için ayrı sürüm gerekir;
  v 1/v 2 kayıtlar legacy kurallarla açılır. Eski kaydı anlık satın alma,
  merkez ödeneği veya rezerv 15 ile sessizce oynatmak kabul edilemez. Yeni kayıt sürümü 3 olur; v 1/v 2 kayıtlar eski kural profiliyle açılır.
- Onarım/tier satın alma eski dört eylemli `ActionLog` içine kontrolsüz yeni kimlik eklememelidir; YZ tercih/profil
  sayaçlarının bu komutları nasıl sınıflandırdığı açık olmalı ve mevcut sözlükleri bozmamalıdır.
- Ortak C#/JS değişiklikleri birlikte uygulanır; parite beklenenleri yeniden üretilir. Unity'ye özgü olaylar ve
  kampanya kendi kapıları altında kalır. Geçici aday dosyaları C#/JS parite doğrulaması yerine geçmez.
- EditMode doğrulaması; kayıt+replay eşitliği; farklı k/ekip modu; aynı gün peşin harcama+pause/rezerv; invalid
  onarım; Left dönüşü/bekleme; olay kartı pause engeli; İllüzyonda Analog; kampanya bileşimi ve gerçek Play gerekir.
- UI yeni kademe kartları, pause/sürdür, bütçe dökümü, toparlanma ve ekip kurma akışlarını sahne/prefab kaynakları
  ve `[SerializeField]` bağlantılarla taşımalıdır. Oyuncuya görünen bütün yeni metinler TR+EN `Loc` akışına girer.

## Dayanaklar

- `Docs/FON-SISTEMI-ONERISI.md`: merkez/bölge ayrımı, sınırlı anlaşmalar, toparlanma ve gerçek ödeme görünümü.
- `Docs/OPERASYON-VE-HIZALAMA-ORNEGI.md`: dokuz anlık operasyon, uzun Hizalama, ortak fon ve pause/rezerv.
- `AGENTS.md`: Core/Algı ayrımı, komut kaydı, C#/JS paritesi, Balance, sahne/prefab ve lokalizasyon kuralları.
- `Assets/Corrigible/Core/Simulation.cs`: mevcut gelir, araştırma, Algı ve oyuncu komut kapıları.
- `Assets/Corrigible/Core/Teams.cs`: ekip erişimi, yolculuk/görev ve baskın koşulları.
- `Assets/Corrigible/Core/GameRecord.cs`: sürüm 2 komut replay modeli.
- `Assets/Corrigible/Game/Campaign/CampaignUpgrade.cs`: kalıcı yükseltmelerin mevcut alan etkileri.
