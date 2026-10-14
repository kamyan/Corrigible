# Son Şalter — fon sistemi önerisi

14 Ekim 2026. Bu bir tasarım önerisidir; oyun koduna uygulanmadı. Rakamlar ilk denge denemesi için başlangıç
değerleridir. Mevcut davranış [fon geliri incelemesinde](FON-GELIRI-INCELEMESI.md), anlık operasyon ve uzun
proje ayrımı [operasyon/Hizalama örneğinde](OPERASYON-VE-HIZALAMA-ORNEGI.md) açıklanır.

Öneri: ekonomi ve güvene bağlı bölgesel katkıyı koru; küçük bir merkez ödeneği, sınırlı gelir yatırımları ve
toparlanma araçları ekle. Fon göstergesi bütün girişleri ve gerçek araştırma ödemesini açıklasın.

## 1. Merkez ödeneği ve bölgesel katkı

```text
Yeni gelir = Merkez ödeneği
             + mevcut bölgesel gelir ×0,8×bütçe anlaşması çarpanı
```

Varsayılan k=1,5 dünyasında merkez ödeneği 0,50 fon/gün olsun. Ayarlarda nominal 0,75 tutulup diğer günlük
oranlar gibi k'ya bölünsün. Hem tespit öncesinde hem sonrasında aynı merkez ödeneği verilsin. Bölgesel katkının
tespit öncesi 0,4/sonrası 1 katsayısı korunur. Bitmiş veya duraklatılmış oyunda yeni günlük ödeme yapılmaz.

Bu küçük sabit kısım birkaç bölge hasar görünce bütçenin tamamının aynı oranda aşınmasını önler. Sağlıklı
bölgesel katkılar hâlâ gelirin büyük bölümüdür. Erken oyunda daha fazla seçim alanı açılması amaçlanır.

Örnek: başlangıç ekonomileri, bütün bölgeler online/ittifakta, güven 70:

| Durum | Mevcut gelir | Önerilen gelir |
|---|---:|---:|
| Tespit öncesi | 0,952 | 1,262 |
| Tespit sonrası | 2,380 | 2,404 |

Bu örnekte erken gelir yaklaşık%32,5 artar; tespit sonrası sağlıklı gelir yaklaşık%1 artar. Tespit yine önemlidir,
fakat gelir sıçraması 2,5 kat yerine yaklaşık 1,91 kat olur. Erken oyunu ne kadar kolaylaştırdığı test edilmelidir.

## 2. Sınırlı bütçe yatırımları

Operasyon geliştirmelerine yeni bir bütçe dalı ekle. İki kademe de YZ tespitinden sonra satın alınabilsin; II için
I gerekir. Satın alma anında uygulanır ve yalnız mevcut kriz/kayıtlarında kalır. Yeni para birimi eklenmez.

| Kademe | Peşin bedel | Bölgesel gelir bonusu | Sağlıklı örnekte toplam gelir |
|---|---:|---:|---:|
| Bütçe Anlaşması I | 25 fon | +%15 | 2,690 fon/gün |
| Bütçe Anlaşması II | 40 fon | Başlangıca göre toplam +%35 | 3,070 fon/gün |

Bonus merkez ödeneğine değil, gerçek bölgesel katkıya uygulanır. II'nin çarpanı 1,35'tir;1,15×1,35 değildir.
Karantina, düşük güven, offline veya ittifak kaybı anlaşmanın sağladığı ek parayı da azaltır. Her kademe bir kez
alınır; sınırsız tekrar yoktur. Mevcut kampanya Uluslararası fon yükseltmesi ayrı sistemdir; fiyat ve açıklama
karışıklığı olmaması için yeni dalın adı farklı tutulur.

Sabit sağlıklı gelir varsayımında ilk yatırımın 25 fonu yaklaşık 88 günde, ikinci yatırımın 40 fonu kendi ek getirisiyle
yaklaşık 105 günde geri döner. İki kademeye harcanan 65 fonun toplam ek getirisiyle geri dönüşü yaklaşık 98 gündür.
Bu süreler dünya durumu sabitken aritmetik örneklerdir. Kısa Silme oyununda yatırıma para bağlamak kârlı olmayabilir;
uzun Hizalama oyununda gelir kapasitesi seçimi anlam kazanır. Araştırmanın daha geç başlaması da maliyettir.

Dört gözlem/k=1,5 ile Değer Kilidi tam hız talebi 2,895 fon/gündür. Yeni modelde iki anlaşma ve sağlıklı dünya
yaklaşık+0,176 fon/gün bırakır. Güven bütün dünyada 60'a düşerse gelir 2,703 olur ve aynı projede günlük açık
yaklaşık 0,192 fondur. Böylece yatırım tam hız kapasitesine yardım eder, güven/ekonomi kaybını önemsizleştirmez.

## 3. Bölgeleri toparlama araçları

İki oyuncu eylemi önerilir; sayılar ikinci denge kalemidir:

- **Altyapı desteği:** online ve karantinasız bir bölgede 8 fon ve boşta ekip karşılığı 4 günlük görev. Tamamlanınca
  ekonomi+15, en fazla bölgenin başlangıç ekonomisine kadar. Üretim hasarını onarır; kendi başına başlangıç üstü
  kalıcı ekonomi oluşturmaz. Görev iptal/erişim kaybı davranışı uygulamadan önce tanımlanmalıdır.
- **Diplomatik görüşme:**20 fon+3 SG karşılığı hedefte güven+15, en fazla 70'e kadar. İttifak dışı bölgeye de
  uygulanabilir;35 eşiğine ulaşıyorsa işbirliğine dönüş sağlanır. Böylece yalnız doğal güven toparlanmasını
  yüzlerce gün beklemek zorunlu olmaz. Eşik 20/35 korunur; komut sonrası üyelik durumunun güncellemesi açık olmalıdır.

Bu araçlar kötü durumdan çıkış verir. Ekonomi ve güven sınırları, aynı eylemi tekrarlayarak sağlıklı dünyada
sınırsız gelir büyütmeyi engeller. Bedeller ile bölgesel onarımın parasal geri dönüşü birlikte denenmelidir.

## 4. Başarıya bağlı tek seferlik hibeler

Kriz başında oyuncuya bir koruma hedefi göster: örneğin belirtilen üç bölgeyi YZ tespitinden sonra 20 gün boyunca
ittifakta ve güven 55 veya üstünde tutmak 25 fonluk bir dayanışma hibesi açsın. Hedefteki bölgeler yalnız en
zenginlerden seçilmesin. Hibe kriz başına bir kez verilsin ve başarısı kayıtta saklansın.

Bu ödeme günlük gelir değildir. Düşük fonu tekrar tekrar ödüllendiren yardım döngüsü kurulmasın. Şartlar gizli
gerçek sızmaya dayanmasın; oyuncunun görebildiği güven ve üyelik kullanılsın. Sonradan bilerek hasar verip
onararak aynı ödülü toplama mümkün olmasın. Mevcut bağış ve YZ tekliflerinin bağımlılık bedelleri ayrı kalabilir.

## 5. Bütçeyi doğru göster

Örnek sağlıklı, iki anlaşmalı ve tam hız Değer Kilidi bütçesi:

```text
Merkez ödeneği             +0,50 /gün
Bölgesel ödenekler         +1,90 /gün
Bütçe anlaşmaları         +0,67 /gün
Brüt gelir                +3,07 /gün
Gerçek araştırma ödemesi   −2,89 /gün
Net günlük birikim         +0,18 /gün
```

Araştırmanın tam hız ihtiyacı ayrıca gösterilsin. Rezerv veya fon yetersizliğinde gerçek ödeme daha küçükse net
akış bu gerçek ödeme üzerinden hesaplanmalı. Son ödenen gelir ile canlı sonraki-gün tahmini birbirinden
ayırt edilmeli. Operasyon, baskın, ekip gibi peşin işlemler ayrı harcama kaydında görünmeli.

Hizalama için önceki taslaktaki duraklat/sürdür ve acil durum rezervi birlikte çalışsın. Oyuncu gelir yatırımı
yaparken ilerlemesini kaybetmeden araştırmayı duraklatabilsin; dünya ilerlemeye devam etsin.

## Uygulama önceliği

1. Mevcut gelir/gider açıklamalarını ve gerçek ödeme gösterimini düzelt; yeni araştırma kontrolünü ekle.
2. Merkez/bölgesel ayrımı ve iki bütçe yatırımını deneysel karşılaştırmada uygula.
3. Bölgesel onarım ve diplomatik geri dönüşü ekle.
4. Hibeleri en son ekle; nakit akışı zaten yeterliyse ek ödüle ihtiyaç olmayabilir.

İlk aşamada iki kademeli bütçe yatırımları zorunlu en iyi açılış hâline geliyor mu, erken gelir artışı tehdidi
çok kolaylaştırıyor mu ve bölge kaybı hâlâ anlamlı mı ölçülmeli. Kampanya bonuslarıyla birleşimi ayrıca sınanmalı.
Fiyatlar ve katsayılar Balance/BAL içinde tutulmalı; ortak simülasyon değişiklikleri C#/JS birlikte güncellenip
parite ve denge testleriyle doğrulanmalı. Bu belge bu doğrulamanın yerine geçmez.
