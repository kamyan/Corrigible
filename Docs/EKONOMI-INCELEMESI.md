# Son Şalter — ekonomi incelemesi

14 Ekim 2026. Bu inceleme mevcut C#/JS kurallarına, BalanceConfig asset'ine, iki Unity arayüzüne ve yalıtılmış
simülasyon ölçümlerine dayanır. Oyun kuralları veya fiyatlar değiştirilmedi. Kampanya ve kayıtlı tercihler aşağıdaki
varsayılanları değiştirebilir.

Ekonomi, kriz yönetimi için anlamlı bir gerilim kuruyor: fonu araştırma ve saha müdahaleleri birlikte tüketiyor;
karantina tehdidi sınırlarken geliri ve güveni aşındırıyor. Oyuncunun ekonomiyi toparlama ve araştırma kararından
dönme araçları sınırlı. Ayrıca bazı maliyet açıklamaları gerçek uygulamayla uyuşmuyor. Önce karar bilgisini ve
mevcut işlemlere erişimi düzeltmek, sonra fiyatlarla oynamak daha sağlıklı olur.

Buradaki “gün” simülasyon günüdür. Varsayılan dünya ölçeği **k=1,5**; 1×/2×/4× oynatma hızından ayrıdır. Günlük
gelir, araştırma ilerlemesi ve dünya oranları k'ya bölünür; bazı süreler k ile çarpılır. Ekip yolculuk ve görev
süreleri ölçeklenmez. Böylece k artışı ekiplere dünya karşısında göreli zaman kazandırır.

| Bileşen | Mevcut kural | Ekonomik anlamı |
|---|---|---|
| Başlangıç | 60 fon, 5 SG, rezerv 0 | İki pahalı izleme ağı bütün başlangıç fonunu tüketebilir. |
| Fon geliri | Çevrimiçi ve ittifaktaki bölgelerin `Econ × Trust/100` toplamı `/150/k`; tespit öncesi ×0,4 | İlk tespit, diğer koşullar aynıyken geliri 2,5 katına çıkarır. |
| SG geliri | Tespit öncesi `0,05/k`; sonrası `(0,25 + 0,03 × Detected)/k` | Kalıcı tespit sayısına bağlı; mevcut ekonomi, çekilme ve çevrimdışı durumdan bağımsız. |
| İzleme | `10 + 4 × Compute`: 14–30 fon; bakım gideri yok | Bilgi, tespit ve Hizalama kapasitesi yatırımı. Tam harita 264 fon. |
| Denetim | 15 fon + 1 SG; Analog sonrası 20 fon + 1 SG | Aynı fonu araştırmadan çekerek tehdidi ve büyümeyi azaltır. |
| Karantina | 8 SG; kaldırmak ücretsiz | Fon harcamadan kontrol sağlar; günlük ekonomi/güven kaybı gelir tabanını daraltır. |
| Bölgesel şalter | `round(40 × (1 + Dep/50))` fon + 30 SG; bilinen çekirdekte 20 SG | Anlık bedel yanında ekonomi/güven kaybı, veto ve çevrimdışı gelir kaybı var. |
| Baskın | 30 fon + 6 SG ve hazır ekip | Taktik risk karşılığında şalterin ağır siyasi/ekonomik bedeline alternatif. |
| Yeni ekip | 45 fon, 4 gün; en fazla 4 | Operasyon kapasitesi; yolculuk/bakım için günlük fon gideri yok. |
| Araştırma | Tam hız ihtiyacı `(Cost/Days × speed)/k`; ödeme rezerv üstündeki fonla sınırlı | Fon yetmezse ilerleme ödeme oranıyla yavaşlar; aynı anda tek proje. |

Kaynaklar: [Balance](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Balance.cs:27),
[gelir ve maliyetler](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Simulation.cs:114),
[günlük ödeme](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Simulation.cs:278),
[ekip/baskın](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Teams.cs:17).

12 bölgenin başlangıç ekonomileri toplamı 765. Başlangıç güveninin beklenen değeri %67,5 kabul edildiğinde temel
k=1 gelirleri tespit öncesi **1,377**, sonrası **3,4425** fon/gün; varsayılan k=1,5 değerleri **0,918** ve
**2,295** fon/gündür. Güven %70 iken, hasarsız başlangıç ekonomisi yaklaşık **2,38** fon/gün üretir. Tohum 7
üzerindeki doğrudan ölçüm 0,905 ve 2,263 verdi. Dokümandaki ~1,4/~3,4 rakamları temel k=1 içindir.

SG varsayılan ölçekte tespit öncesi 0,0333/gün, ilk tespitten sonra 0,1867/gün; 12 bölge tespit edildiğinde
0,4067/gündür. Başlangıçtaki 5 SG karantina için yetmez. Doğrulama, denetim, hediye reddi ve baskınlar aynı siyasi
stokla yarışır. Ret kararı stok yetersizken engellenmez; SG kaybı sıfırda kesilir.

Yeni proje panosunun katalog bedelleri şu şekilde:

| Aşama | Toplam fon | Nominal toplam süre |
|---|---:|---:|
| 1 — Temeller | 105 | 26 gün |
| 2 — Uygulama | 158 | 47 gün |
| 3 — Dönüşüm | 158 | 44 gün |
| 4 — Hizalama | 165 | 38 gün |
| Tüm projeler | **586** | **155 gün** |

Bunlar yeterli fon, Hizalama için dört gözlem ve sabotajsız ilerleme varsayımıyla katalog toplamlarıdır; sütun
tamamlama kapısı yoktur. Hizalama zinciri tek başına **331 fon / 93 nominal gün**. Dört gözlemle k=1,5 dünyasında
yaklaşık 139,5 gün, bir gözlemle yaklaşık 558 gün gerekir; günlük tick yuvarlaması, fon açığı ve sabotaj bunu
uzatır. Diğer altı proje 255 fon / 62 nominal gün ile aynı araştırma yuvasını kullanır. Pano bu nedenle bütün
kutuları sırayla doldurma listesi yerine çözüm tercihi olarak okunmalı.

Kaynak: [araştırma kataloğu](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/GameData.cs:163).

Somut düzeltme adayları, önem sırasıyla:

1. **Yeni Desk'te ekip kurma işlemi yok.** Core 45 fonla ekip kurabiliyor; Main'in eski ekip ekranında düğme var,
   yeni masa yalnız mevcut ekipleri listeliyor ve gönderiyor. Baskında ekip kaybedildikten sonra parayla kapasiteyi
   geri alma yolu da kapanıyor. Bu bir fiyat dengeleme konusu değil, mevcut işleme erişim eksikliği.
   [Core](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Teams.cs:85),
   [Desk komutları](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Desk/Scripts/Bridge/SimDeskCommands.cs:173),
   [ekip penceresi](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Desk/Scripts/Runtime/Windows/Content/ContentWindowView.cs:53).
2. **Fon göstergesi ihtiyaç ile gerçek ödemeyi karıştırıyor.** Yeni Desk k'ya doğru böler, ancak tam hızdaki
   araştırma talebini “gider” diye gösterir. Fon rezervde sabit kaldığında bile negatif net akış raporlayabilir.
   Main'in eski ekranı ayrıca k'ya bölmeden ResearchNeed gösterir: k=1,5'te günlük araştırma yükü %50 fazla görünür.
   “Tam hız ihtiyacı / ödenen / kullanılabilir fon” ayrımı gerekli.
   [Desk](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Desk/Scripts/Bridge/SimDeskBuilder.cs:306),
   [Main](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Game/UI/CrisisDeskView.cs:264),
   [gerçek ödeme](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Simulation.cs:292).
3. **Ekonomik etki süreleri düşük gösteriliyor.** Denetim yaması ve şalterin çevrimdışı süresi önizlemelerde
   20 gün, varsayılan k=1,5 uygulamasında 30 gündür. Kalıcı yamalarla denetim 45 gün olur. İzleme kartındaki “3 gün
   sonra” ise etki gecikmesi değildir: ağ ve ödeme hemen uygulanır, ekip 3 gün meşgul kalır. Özellikle şalterin
   gelir kesintisi süresi doğru anlatılmalı.
   [Desk önizlemesi](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Desk/Scripts/Bridge/SimDeskBuilder.Dossier.cs:194),
   [yama](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Simulation.cs:667),
   [çevrimdışı süre](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Simulation.cs:691).
4. **Kampanya kartları bazı kazanımları yanlış anlatıyor.** İletişim ofisi “güven 0” diyor; yalnız diğer bölgelerin
   ret cezası kaldırılıyor, hedef bölge hâlâ 15 güven kaybediyor. Uluslararası fon “+%15” derken `fonDiv ×0,85`
   yüzünden +%17,647 veriyor. Hizalama ekibi kartındaki “+%6”, hızın 0,25'ten 0,31'e çıkmasıdır: 1–3 gözlemde
   göreli hız +%24, dört gözlemde tavan nedeniyle kazanç yok. Bunlar karar ekranında uygulanan değerlerden üretilmeli.
   [kartlar](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Desk/Scripts/Bridge/SimDeskBuilder.Screens.cs:102),
   [yükseltmeler](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Game/Campaign/CampaignUpgrade.cs:22),
   [ret](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Simulation.cs:795).
5. **Araştırma tahmini finansman riskini tam taşımıyor.** Süre gözlem hızıyla hesaplanıyor; mevcut fonun ne kadar
   dayanacağını veya ödeme açığının bitişi ne kadar uzatacağını hesaba katmıyor. Proje kartı fon yetmezliğini
   açıklarken hedef kartı tam hız ETA'sını gösterebiliyor. Sabotaj, ödenmiş fonu iade etmeden ilerlemeyi geri alır;
   katalog maliyeti gerçek nihai harcama için garanti değildir.
   [ETA](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Desk/Scripts/Bridge/SimDeskBuilder.cs:469),
   [sabotaj](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Simulation.cs:297).
6. **Kamu Bilinçlendirme metni fazla genel.** “Güven kayıpları −%50” bazı büyük kayıpları kapsamıyor: medya
   karalamasındaki dünya çapında −3 ve perde yırtılmasındaki −5 yarılanmıyor. Bunlar tasarlanmış istisna olabilir;
   açıklama bunu belirtmiyor.
   [karalama](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Simulation.cs:462),
   [perde](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Simulation.cs:601).

Bu ayrım geliştirme açısından önemli: mevcut Build Settings'in ilk sahnesi **Main**, **DeskGame listede yok**.
Dolayısıyla yeni masa ve mevcut build ekranındaki bulguların ikisi de ele alınmalı. Bu incelemede sahne geçişi veya
build düzeni değiştirilmedi. [Build Settings](/Users/selimkamyan/Gits/Corrigible/ProjectSettings/EditorBuildSettings.asset:9).

Tasarım tarafında korunması gereken güçlü döngüler var. İzleme bilgi sağlamanın yanında tespit ödeneğini ve
Hizalama hızını açar. Karantina SG kullanarak zaman kazandırır ama ekonomiyi aşındırır. Hediye kabulü kısa vadede
gelir ve güven sağlar, kalıcı bağımlılık ve sızma yükü getirir. Hediye kaynaklı ekonomi artışı başlangıç ekonomisine
doğru geri söner; bağımlılık aynı şekilde kendiliğinden sönmez. Kontrollü kabulün 20 fonu bu nedenle bir yatırım
getirisinden çok siyasi zararı ve bağımlılığı sınırlama bedelidir.

İyileştirmeyi gerektirebilecek tasarım riskleri de var:

- **Rezerv seçenekleri bazı kriz bedellerini koruyamıyor.** En yüksek rezerv 60; bağımlılık 70'te şalter 96 fon,
  Analog ile veto sınırı 85'te 108 fon. Araştırma gideri geliri aşıyorsa yeni gelir aynı tick harcanır; rezerv üstünde
  birikim oluşmaz. Araştırmayı durdurma/değiştirme komutu da yok. Bu otomatik kayıp değildir, ancak kritik bir
  eyleme para ayırma esnekliği yetersiz kalabilir.
- **İttifaktan çekilme oturum boyunca kalıcılaşabilir.** Çıkış eşiği 20, dönüş 35; doğal toparlanma 0,05/k. Tam
  20'den 35'e dış yardım olmadan varsayılan ölçekte yaklaşık 450 gün gerekir. Bölge gelir vermez ve olağan
  müdahaleleri reddeder. Mevcut olaylardaki diplomasi etkisini, sınırlı kullanım ve maliyetle bir onarım seçeneğine
  dönüştürmek değerlendirilebilir.
- **Az gözlemle Hizalama sabotajı ağırlaşır.** Bir gözlemde nominal ilerleme 0,25; %50 ortalama sızmada temel
  beklenen sabotaj kaybı `0,05 ×4 =0,20` olur. Aldatma ile kayıp 0,30'a çıkar. Bu doğrusal teşhis gerçek bitiş süresi
  değildir; sıfır tabanı ve değişen dünya koşulları vardır. Harita kontrolü ve Güçlendirilmiş Ağlar bu riski azaltır.
- **Baskın güçlü bir ekonomik alternatif.** Başarıyla çekirdeği 30 fon/6 SG karşılığında, veto ve şalterin bölgeyi
  karartma maliyeti olmadan yok edebilir. Taktik başarısızlık, yan hasar ve ekip kaybı avantajın karşılığıdır. Fiyatı
  artırmadan önce farklı taktik beceri düzeyleriyle gerçek oyuncu testi gerekir.
- **SG'nin teması netleştirilmeli.** Güven/ekonomi/ittifak kaybı fonu azaltırken SG kalıcı tespitlerden üretilmeye
  devam eder. SG tehdidin ciddiyetinin kabulünü temsil ediyorsa bu mantıklıdır; mevcut siyasi işbirliğini temsil
  ediyorsa çekilen bölgelerin sayılmaya devam etmesi açıklama gerektirir. Salt fon formülünü SG'ye kopyalamak
  kötüleşme döngüsünü gereksiz ağırlaştırabilir.

Kontrollü JS ölçümünde büyüme, sıçrama, tekillik kazancı ve sabotaj kapatıldı; bölgeler başlangıç ekonomisi ve
%70 güvenle kuruldu. Bu bir zorluk testi değil, ödeme formülünü ayıran deneydir. Yalnız Değer Kilidi çalıştırıldı;
ön koşullar fixture olarak tamamlanmış kabul edildi.

| Gözlem | Rezerv | Başlangıç fonu | Değer Kilidi bitişi | Son fon |
|---:|---:|---:|---:|---:|
| 4 | 0 | 60 | 57 gün | 30,63 |
| 4 | 60 | 60 | 70 gün | 60,00 |
| 1 | 0 | 60 | 228 gün | 437,51 |

İkinci senaryoda tam hız ihtiyacı **2,895**, ilk gün gerçek ödeme **2,379** fon; stok değişimi **0**. Göstergenin
yaklaşık −0,516/gün hesaplaması gerçek stok hareketi değildir. İlk senaryoda katalogdaki 165 fon ödendi;
rezervli senaryoda son tickin tam ödenmesi nedeniyle yaklaşık 166,56 fon harcandı. Az gözlem araştırma takvimini
uzatırken günlük ödemeyi de azaltır; fon biriktirmek ile zaman kazanmak aynı hedef değildir.

Ek olarak 10000–10079 tohumlarıyla 1440 bot oyunu çalıştırıldı: 3 rakip × 3 oyuncu modeli × 2 yapılandırma ×80.
Mevcut bot kararları ve taktik baskın botu kullanıldı. Seçili sonuçlar:

| Rakip | Temel acemi | Temel usta | Ekipler/k=1,5 acemi | Ekipler/k=1,5 usta |
|---|---:|---:|---:|---:|
| Solucan | %16,25 | %97,50 | %12,50 | %82,50 |
| Manipülatör | %11,25 | %82,50 | %8,75 | %57,50 |
| Kâhin | %2,50 | %72,50 | %6,25 | %65,00 |

Pasif modellerin kazanması her iki yapılandırmada da %0. Usta Manipülatör modelinde fonun 1 altında olduğu
günlerin oranı temel oyunda yaklaşık %20,1, ekipler/k=1,5 koşulunda %32,1. Bu, bütçe darboğazının ölçümlerde sık
yaşandığını gösterir; tek başına gelir veya fiyat artırılması gerektiğini göstermez.

Bu yüzdeler insan zorluğu değildir. Botlar ipucu türünü hatasız okuyor, hediye kararlarında gerçek bağımlılık
verisini kullanıyor ve sınırlı strateji izliyor. Unity'ye özgü eğitim aşamaları, olay kartları, kampanya yükseltmeleri
ve kayıtlı oyuncu profili bu ölçümde yok. Dünya ölçeği, saha kuralları ve botun hamle sıklığı birlikte değiştiğinden
sonuç farkı yalnız bir ekonomik katsayıya bağlanamaz. Her hücre 80 oyun; oranlar kaba sinyal olarak kullanılmalı.

Önerdiğim sıra: önce ekip kurma erişimi ve maliyet/süre açıklamaları; ardından ilerlemeyi koruyan araştırma
duraklatma/değiştirme, daha esnek kriz rezervi ve gelir dökümü; sonrasında sınırlı diplomasi/onarım seçeneğiyle
insan testi. Fiyat dengelemesi bu ölçümlerin ardından yapılmalı. Değer Kilidi'nin gelirden pahalı günlük gideri
belgelenmiş bir son düzlük gerilimidir; bunu kendiliğinden hata saymıyorum.

Son olarak ekonomi ayarlarının bir kısmı BalanceConfig dışında: SG gelir katsayıları, denetim/karantina bedelleri,
ekip/baskın bedelleri ve araştırma kataloğu kodda duruyor. Tasarımcıya ekonomi ayarı vermek için bu değerlerin
düzenlenebilir veri haline taşınması gerekir. Kural değişiklikleri C#/JS paritesini ve komut/kayıt uyumluluğunu
korumalı; yeni oyuncu işlemleri kaydedilip replay'de uygulanmalı.

Ham ölçümler: [results.jsonl](/Users/selimkamyan/Gits/Corrigible/Temp/economy-review/results.jsonl).
Tekrarlama betiği: [audit.js](/Users/selimkamyan/Gits/Corrigible/Temp/economy-review/audit.js).
