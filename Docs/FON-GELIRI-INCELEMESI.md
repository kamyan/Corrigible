# Son Şalter — günlük fon gelirini etkileyenler

14 Ekim 2026. Mevcut C# kaynakları, BalanceConfig, kampanya yükseltmeleri, iki arayüz ve JS referansında
yalıtılmış gelir ölçümleri incelendi. Oyun kuralları veya denge değerleri değiştirilmedi. Önceki operasyon/Hizalama
taslağı henüz uygulanmadı; aşağıdaki sonuçlar mevcut oyunun davranışıdır.

## Gerçek formül

```text
Günlük brüt fon = Σ(aktif bölgenin Ekonomi × Güven /100)
                 × (YZ tespit edildi ? 1 : 0,4)
                 /150 /dünya ölçeği
```

Aktif bölge ittifakta ve çevrimiçi demektir. Çevrimdışı veya ittifak dışındaki bölge sıfır katkı verir.
Varsayılan dünya ölçeği k=1,5; bu nedenle tespit sonrası bölge katkısı `Ekonomi×Güven/22500` olur.
Kampanya katsayıları bunu değiştirebilir. 1×/2×/4× oynatma hızı ayrı bir ayardır: günlük oranı değiştirmez,
simülasyon günlerini gerçek zamanda daha hızlı işletir.

Kaynaklar: [formül](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Simulation.cs:131),
[ayarlar](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Settings/BalanceConfig.asset:28),
[varsayılan ölçek](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Game/GameController.cs:34).

## Ne kadar değiştiriyor?

Karşılaştırma dünyası: 12 bölge çevrimiçi ve ittifakta, ekonomileri başlangıç değerlerinde (toplam 765), her
bölgede güven 70, k=1,5. Başlangıç güveni gerçek oyunda tohumla 60–75 arasında değişir; aşağıdaki dünya sabit örnektir.

| Değişim | Günlük brüt gelir | Başlangıç duruma göre fark |
|---|---:|---:|
| YZ henüz tespit edilmemiş | 0,952 | — |
| İlk tespit gerçekleşmiş | 2,380 | Öncesinin 2,5 katı; +1,428 |
| Bütün bölgelerde güven 70→60 | 2,040 | −0,340 |
| Bütün bölgelerde güven 70→80 | 2,720 | +0,340 |
| Yalnız Kuzey Amerika'da güven+10 | 2,420 | +0,040 |
| Yalnız Afrika'da güven+10 | 2,396 | +0,0156 |
| Güveni 70 olan bir bölgede ekonomi+10 | 2,411 | +0,0311 |
| Kuzey Amerika çevrimdışı veya ittifak dışı | 2,100 | −0,280 |
| Medya karalaması: bütün bölgelerde güven−3 | 2,278 | −0,102 |
| Perde yırtma şoku: bütün bölgelerde güven−5 | 2,210 | −0,170 |
| Hindistan'da hediye kabulü: ekonomi+10, güven+5 | 2,427 | +0,0467 |
| Hindistan'da hediye reddi: hedef−15, diğerleri−2 güven | 2,277 | −0,1027 |
| Aynı ret, Kamu Bilinçlendirme tamamlanmış | 2,329 | −0,0513 |
| Uluslararası fon kampanya yükseltmesi | 2,800 | +0,420; gerçek +%17,647 |
| Dünya ölçeği 1,5 yerine 1 | 3,570 | +%50; dünya ve araştırma oranları da değişir |

Tablo satırları bağımsız karşılaştırmalardır, üst üste uygulanmış olaylar değildir. Güven/ekonomi sınırlarına,
üyelik eşiklerine veya kampanya değişikliklerine takılınca farklar değişir. Örnekte hediye hedefinin ekonomisi 60'tır.
Denetimli kabul aynı gelir artışını verir fakat 20 fon peşin gideri vardır ve bağımlılık/sızma bedeli daha düşüktür.

Güvenin etkisi bölgenin ekonomisiyle ağırlıklıdır. NA ve Afrika'da aynı+10 güven, dünya güven ortalamasını aynı
miktarda artırır; NA'da fon katkısı yaklaşık 2,57 kat büyüktür. Fon hesabı küresel güven ortalamasını doğrudan
kullanmaz, bölgesel çarpımları toplar.

## Bölgesel katkılar

Tespit sonrası, güven 70 ve başlangıç ekonomileriyle:

| Bölge | Ekonomi | Fon/gün | Dünya gelirindeki payı |
|---|---:|---:|---:|
| Kuzey Amerika | 90 | 0,2800 | %11,76 |
| Batı Avrupa | 85 | 0,2644 | %11,11 |
| Çin | 85 | 0,2644 | %11,11 |
| Japonya-Kore | 80 | 0,2489 | %10,46 |
| Orta Doğu, Hindistan, Okyanusya (her biri) | 60 | 0,1867 | %7,84 |
| Doğu Avrupa, Güneydoğu Asya (her biri) | 55 | 0,1711 | %7,19 |
| Güney Amerika, Rusya (her biri) | 50 | 0,1556 | %6,54 |
| Afrika | 35 | 0,1089 | %4,58 |

İlk dört bölge başlangıç gelirinin toplam %44,44'ünü üretir. Bu değerler eşit güven ve hasarsız ekonomi varsayımıyla
geçerlidir; gerçek oyunda sıralama değişebilir.
[Bölge ekonomileri](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/GameData.cs:98).

## Karantina, şalter ve toparlanma

Karantinada çevrimiçi bölge günlük `DepMul/k` ekonomi ve `0,4×LossMul×DepMul/k` güven kaybeder. Ardından güven
70'e doğru 0,05/k hareket eder. `DepMul`: bağımlılık 40 altında 1;40 ve üstünde `1+Dep/50`. Eşik 40'ta katsayı
1'den 1,8'e sıçrar. Bilinçlendirme güven cezasını yarılar, ekonomi cezasını azaltmaz.

Kuzey Amerika için 30 günlük kontrollü ölçüm (ekonomi 90, güven 70 başlangıcı; bağımlılık sabit tutuldu):

| Durum | 30. gün ekonomi | 30. gün güven | Bölgenin fon/gün katkısı | Başlangıç 0,28'e göre kayıp |
|---|---:|---:|---:|---:|
| Bağımlılık 0, Bilinçlendirme yok | 70 | 63 | 0,1960 | %30,0 |
| Bağımlılık 0, Bilinçlendirme var | 70 | 67 | 0,2084 | %25,6 |
| Bağımlılık 50, Bilinçlendirme yok | 50 | 55 | 0,1222 | %56,3 |
| Bağımlılık 50, Bilinçlendirme var | 50 | 63 | 0,1400 | %50,0 |

Bilinçlendirme'nin güven kaybını yarılaması gelir kaybını yarılamaz; ekonomi de aşındığı için örnekte toplam
koruma daha küçüktür. Gerçek oyunda bağımlılık artışı ve olaylar bu örneği değiştirebilir.

Şalter bölgeyi 30 simülasyon günü çevrimdışı bırakır (k=1,5). Ekonomi−40 (taban 5), güven−30×DepMul×LossMul olur;
bağımlılık−40 da uygulanır. Hindistan E 60/T 70 ve düşük bağımlılık örneğinde:

- Önce 0,1867 fon/gün; şalter çekilince 0.
- 30. günde açılış tick'i dahil toparlanmayla E 20,53/T 41: yaklaşık 0,0374 fon/gün.
- 60.günde yaklaşık 0,0627;120.günde 0,0943 fon/gün. Başlangıca dönüş uzun sürer.

Offline sayacı günün başında azaltılır:30'dan başlatılan sayaç 30. tick'te 0 olur ve o gün bölge tekrar fon verir.
“30 gün çevrimdışı” ifadesi 30 tane sıfır-gelir ödemesi demek değildir; komut sonrası ilk 29 tick'te katkı 0'dır.
Offline iken ekonomi toparlanmaz ama güvenin 70'e kayması sürer.

Online ve karantinasız ekonomi, başlangıç değerine günlük `0,02/k` oranında yaklaşır. k=1,5'te başlangıçtan
ekonomi farkı yaklaşık 52 online günde yarıya iner. Bu hem hasara hem hediyenin ekonomi bonusuna uygulanır:
hediyedeki+10 ekonomi 52. günde yaklaşık+4,98 kalır. Güvenin dönüşü çok daha yavaştır:0,0333 puan/gün;20 altında
ittifaktan çıkan bölge 35'e ulaşana kadar fon vermez. Örneğin 20→35 yalnız doğal toparlanmayla yaklaşık 450 gün sürer.

Ekonomi tabanı karantinada 10, şalter/baskında 5'tir; bu nedenle ekonomisi 10 altındaki bölgeye karantina uygulamak
ilk tick'te ekonomiyi 10'a çıkarabilir. Bu bir sınır davranışıdır.

Kaynaklar: [günlük ekonomi ve üyelik](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Simulation.cs:250),
[şalter](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Simulation.cs:691).

## Diğer gelir etkileri

- Baskında her sivil sunucu hasarı ekonomi−5 ve güven−2×DepMul×LossMul; başarısız baskın ayrıca güven−10×LossMul.
  Ekonomi 60/güven 70/düşük bağımlılık örneğinde tek sivil sunucu katkıyı 0,1867→0,1662 düşürür.
- Seçim desteği hedefte+15 güven; ulusalYZ programı desteği+10 ekonomi; acil kesinti müdahalesi+6 güven;
  protestoda karantinayı kaldırmak+8 güven verir ve gelecekteki karantina aşınmasını bitirir.
- Belgeler sızdığında sessizlik bütün bölgelerde−5×LossMul güven; yaptırım/yerel yönetime bırakma hedefte−10×LossMul;
  seçimde karışmama ve protestoyu sürdürme hedefte−12×LossMul güven getirir.
- Sahte karalama haberini doğrulamak kaybettirdiği 3 güveni bütün bölgelere geri verir. Karalama ve perde şoku
  Bilinçlendirme'den etkilenmez.

Kaynaklar: [baskın](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Teams.cs:155),
[olay kartları](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Rules.cs:157),
[doğrulama](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Simulation.cs:816).

## Kampanya ve gelir olmayan değişimler

- Uluslararası fon böleni 150→127,5 yapar: açıklamadaki +%15 yerine matematikte +%17,647 gelir.
- Erken uyarı bütçesi tespit öncesi çarpanı 0,4→0,6 yapar: yalnız bu dönemde gelir +%50.
  Örnek dünyada 0,952→1,428; tespit sonrası değişmez.
- İletişim ofisi ret kararının diğer bölgelerdeki−2 güvenini kaldırır; hedefin−15'i korunur.
- Ek bütçe yalnız başlangıç fonuna+40 ekler. Şartlı bağış+60 ve YZ teklifi+50 de tek seferlik stok girişleridir.
- Rakip tipi veya kampanya zorluğu fon formülüne ayrı katsayı eklemez; yetenekler/olaylar girdileri dolaylı değiştirir.
- Sızma, bağımlılık, hesaplama gücü, ekip/izleme/tespit sayısı, siyasi güç ve mevcut fon stoku doğrudan gelir
  çarpanı değildir. Sonraki tespitler SG gelirini artırır; fon için yalnız ilk farkındalık değişimi önemlidir.
- İllüzyon gerçek fon üretimini sahteleştirmez. Sahte küresel güven gösterilse bile gelir gerçek bölge güveniyle
  hesaplanır. Araştırma durduğu için eldeki fonun birikmesi hızlanabilir.

Kaynaklar: [kampanya](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Game/Campaign/CampaignUpgrade.cs:18),
[stok girişleri](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Rules.cs:263),
[algı ve gerçek girdiler](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Simulation.cs:879).

## Brüt gelir, gerçek ödeme ve arayüz

Gün önce offline dönüş, tespit, ekonomi/güven ve üyelik işlemlerini yapar; sonra brüt geliri fon stokuna ekler.
Ardından araştırma rezerv üstündeki paradan ödeme alır. Komut/olay yoksa gerçek fon artışı `gelir−ödenen araştırma`dır.
Araştırmanın tam hız talebi ile ödenmiş miktar aynı olmayabilir.

| Hizalama aşaması | Dört gözlem/k=1,5 ile günlük talep | Kontrollü ilk tick gelir 2,3789 ise stok değişimi |
|---|---:|---:|
| Yorumlanabilirlik | 1,8133 | +0,5655 |
| Düzeltilebilirlik | 2,1778 | +0,2011 |
| Değer Kilidi | 2,8947 | −0,5159 |

Rezerv 60 ve elde 60 fon varken Değer Kilidi örneğinde gelir 2,3789, gerçek ödeme 2,3789 olur; stok 60'ta kalır ve
araştırma tam hızdan yavaş ilerler. Tam hız talebini gider sayan gösterge negatif net akış gösterebilir.

Desk gelir ve araştırma talebini k'ya doğru böler ama “gider” gerçek ödeme değil tam hız ihtiyacıdır. Main araştırma
notunda k'ya bölmeyi atlar; varsayılan ölçekte yükü %50 fazla gösterir. Main'in tespit öncesi “bütçe %40” metni sabittir;
Erken uyarı yükseltmesi%60 yapsa da değişmez. Açıklama penceresindeki dinamik oran ise doğrudur.

`FundRate` canlı hesaptır, son ödenmiş gelir kaydı değildir. Günlük gelirden sonra üretilen karalama haberi aynı
günün gösterge oranını düşürür ama o gün ödenmiş geliri geriye dönük azaltmaz. Üyelik bayrağı günlük tick'te
değişir; oyuncu komutuyla güven 20 altına veya 35 üstüne taşındığında getter ile üyelik arasında kısa gecikme olabilir.

Kaynaklar: [ödeme](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Simulation.cs:278),
[Desk](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Desk/Scripts/Bridge/SimDeskBuilder.cs:306),
[Main](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Game/UI/CrisisDeskView.cs:264).

## İnceleme sonucu ve doğrulama

Sistem fon büyütmekten çok katkı veren bölgeleri ve güveni korumaya dayanır. En büyük doğrudan olumlu değişim
ilk tespittir; sonrasında doğal güven 70'e yönelir, ekonomi kendi tabanına döner. Araştırmaların çoğu yeni gelir
üretmez, mevcut tabanı dolaylı korur. Sabit gelire göre daha çok harcama seçeneği eklenirse nakit birikimi ve
Hizalama finansmanı ayrıca sınanmalıdır.

Ölçüm aracı `Temp/fund-income-review.js`, çıktı `Temp/fund-income-review-results.jsonl`dir.41 kayıt üretildi.
Statik duyarlılıklar gerçek JS fundRate'inden; hediye/şalter ve araştırma gerçek komutlardan; süre örnekleri gerçek
step fonksiyonundan ölçüldü. Süre deneylerinde büyüme, yayılma, Tekillik, CP, bağımlılık artışı ve sabotaj yalnız
ayrı sürecin belleğinde sıfırlandı; haber/hediye takvimi ileri alındı. Ekonomi/güven işlemleri korundu. Böylece
gelir girdilerinin etkisi diğer dünya değişimlerinden ayrıldı. Unity'ye özgü olay/kampanya etkileri C# kaynağından
incelendi. Yeni Unity testi veya Play denemesi gerektiren oyun kodu değişikliği yapılmadı.
