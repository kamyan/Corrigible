# Bölge olayları (fikir, uygulanmadı)

_2026-11-02. İstek: her bölge için tahtada tur tur işleyen bir olay. Tahta bina içi olduğu için olaylar da bina içi;
hiçbir kural tek bir tarafa işlemez, iki tarafı da etkiler. Bu belge öneri; kodda yok. 2026-11-01'deki açık hava sürümünün
(sel, deprem, hortum…) yerini aldı._

Bugünkü bölge özellikleri (`Baskin.RegionTraits`) kurulumda bir kez işler. Olay ise baskın boyunca yaşar: binanın bir
sistemi (söndürme, asansör, turnike, kaynak kolu…) bir tur önce tahtada işaretlenir ve kimin durduğuna bakmadan işler.
Her olay bölgenin özelliğine ve imza makinesine bağlandı. Üç açıdan (bina sistemleri, bölgenin binası, simetrik mekanik)
72 aday üretildi, bir yargıç her bölgeye birini seçti, bir eleştirmen `Baskin.cs`'e bakarak açıkları buldu. Aşağıdaki
kurallar eleştirinin düzeltmeleri işlenmiş hâli. Sayılar ilk öneri; ölçülmedi.

## Ortak kurallar

- **Kural tarafa bakmaz.** Aynı karede duran makine ile uzman aynı sonucu alır. Olay ne bir uzmanı ne bir makineyi adıyla
  seçer; yalnız kareye, nesneye ve eyleme bakar.
- **Hamle sırası açığı: fitil.** Makineler yerini planda (evrenin sonunda) sabitler, oyuncu sonra ve tam bilgiyle oynar.
  Oyuncu turunda tetiklenip aynı evrede sonuç veren bir olay bu yüzden fiilen oyuncunun silahı olur. Kural: **N. turda
  tetiklenen olay (oyuncu turunda ya da makine evresinde, fark etmez) N+1. turun makine evresinin sonunda işler.** Arada
  makineler bir kez yerleşir (N. evrenin sonundaki plan), oyuncu bir kez oynar (N+1. tur). Yalnız kilitli, donmuş,
  itilmiş ya da işini bırakmayı seçmeyen birim yer. Son turda olay adımı yoktur; o turda işleyecek fitil söner.
- **İki tarafın da durmak zorunda olduğu kareler.** Olayların çoğu hedefin (sunucu dolabı ya da ikmal kasası) komşularına
  ya da girişle hedef arasındaki yola bağlanır. Teknisyen de bakım robotu da orada çalışır; taşıyıcı oradan geçer.
- **Önce işaret, sonra olay.** Takvimli olaylar tur şeridinde baştan görünür; sonucu bir tur önce tahtada işaretlenir
  (kare, ok, varış hayaleti, birim kartında uyarı). Takvimli olaylar niyetlerden sonra, plandan önce işler: gösterilen
  niyetler kesindir, plan olaydan sonraki tahtayı görür. Hamle önizlemesi olayın sonucunu da gösterir.
- **Hasar tavanı.** Bir birim bir evrede olaylardan (zinciri dahil) en çok 1 hasar alır. Zırh plakası olay hasarını
  karşılar.
- **Nesneler.** Olay ve başlattığı zincir hiçbir nesneye hasar vermez: hedef dolap, ikmal kasası, sivil sunucu, kopya
  dolabı, soğutma ünitesi. GÜVEN, kasa ve YZ altyapısı yalnız seçilen bir hamleyle kaybedilir. Tek istisna: uçan tüp başka
  bir tüpü kırar (Güney Amerika).
- **Kaçış güvencesi.** Hiçbir olay bir zemin karesini çıkıştan koparamaz (`ExitSafe`). Son turdan önceki evrede çıkış
  yolunu değiştiren bir olay olmaz. Çıkışlar ve komşuları olay karesi olmaz.
- **Ritim.** Takvimler bilerek dağıtıldı: kimi her evre, kimi iki ya da üç turda bir, kimi tek seferlik, kimi yalnız
  tetiklenince. Bölgeler aynı nabızla atmasın.
- **Erken bölgeler.** Batı Avrupa ve dört komşusu (Doğu Avrupa, Orta Doğu, Afrika, Kuzey Amerika) Teknisyen, EMP uzmanı ve
  Analist'le oynanabilmeli (Madenci atölye 2'de, Sis uzmanı 3'te gelir).
- **Kayıt ve RNG.** Olaylar `Rnd` çağırmaz. Kurulum bütün RNG çekimlerinden sonra yapılır: olaysız baskınlar ve
  `RaidEngineTests`'in kilitli imzaları aynı kalır. Tahta kaydedilmez, kampanya baskının sonunu `RaidResult` olarak yazar
  ve olay bölge tanımından gelir: `GameRecord.CurrentVersion` artmaz. Eğitim baskını olaysız.

Baskın uzunlukları: sunucu 7 tur, ikmal 8 tur; kısa nöbette (Kuzey Amerika) 6 ve 7. "N. tur sonu", N. turun makine
evresinin sonu demektir.

## Bölgeler

| Bölge | Özellik · imza | Olay | Bina | Ritim | Boy |
|---|---|---|---|---|---|
| Batı Avrupa | Güvenli giriş · zırhlı dron | Söndürme bulutu | Banka veri merkezi, sunucu salonu | 2., 4., 6. (ikmalde 8.) tur bulutlu | S |
| Doğu Avrupa | Dar koridor · kilitleyici | Kayar arşiv rafı | Devlet arşivi, dosya katı | 3. ve 5. tur sonu kayar | M |
| Orta Doğu | Kum fırtınası · patlayan robot | Yanıcı gaz kaçağı | Rafineri kompresör dairesi | 2 tur dolu, 1 tur boş · fitil | M |
| Afrika | Açık arazi · havan | Çatlak tavan | Cevher işleme hangarı | Darbeyle tetiklenir · fitil | M |
| Kuzey Amerika | Kısa nöbet · havan | Yük asansörü | Dağıtım merkezi asansör holü | 2.→3. ve 4.→5. tur | S–M |
| Rusya | Soğutma ağı · kalkan dronu | Kriyo soğutma hattı | Soğuk depolu veri merkezi | 2., 4. (ikmalde 6.) tur sonu | S |
| Hindistan | Kalabalık şehir · kalkan dronu | Vardiya turnikeleri | Çağrı merkezi kasa holü | 2. ve 4. tur sonu döner, son iki tur tahliye | M |
| Çin | Zırhlı dolap · çoğaltıcı | Kaynak robotu kolu | Montaj fabrikası | Her evre sonu | M |
| Japonya-Kore | Elektrik şebekesi · kilitleyici | Şebeke faz değişimi | Çip fabrikası trafo merkezi | 3. ve 6. tur sonu faz değişir | S |
| Güneydoğu Asya | Kopya fabrikası · çoğaltıcı | Süzgeç emmesi | Su basmış elektronik fabrikası | 3. ve 6. tur sonu | S |
| Güney Amerika | Takviye · patlayan robot | Basınçlı tüp rafı | Hidroelektrik santrali bakım atölyesi | Vurulunca · fitil | M |
| Okyanusya | Hızlı devriye · zırhlı dron | Hidrolik yağ sızıntısı | Maden-liman makine dairesi | 2. tur sonu, bir kez | S–M |

### Batı Avrupa · Söndürme bulutu

YZ'nin ele geçirdiği bir banka veri merkezinin sunucu salonu. Tavandaki CO₂ memeleri yangın sanıp salonu beyaz bir
bulutla doldurur.
- **Kural:** iki meme sırayla artı biçimli bir alanı (meme karesi ve duvar ya da nesne olmayan dört komşusu, en az 4 kare)
  doldurur. Bulut 1., 3. ve 5. (ikmalde 7.) tur sonunda dolar (A, B, A, B) ve bir evre sonra dağılır: 2., 4., 6. turlar
  bulutludur. Bulutlu kareye nişan alınmaz, bulutlu kareden de nişan alınmaz: düz atış, kilit ve kanca buluta girince
  kesilir; havan, EMP, bakım vuruşu, sabotaj ve dinamit bulutlu kareyi hedef alamaz ve buluttaki birim bunları yapamaz.
  Bitişik nesneye yapılan iş serbesttir: Teknisyen'in hedefe sabotajı, bakım robotunun onarımı, taşıyıcının kasayı
  alması. Yürüyüş, itme, patlama, uyanma, kalkan ve çoğaltma etkilenmez. Hasar yok.
- **Yer:** meme x 2–5, girişlere en az 3 kare uzak, alanında çıkış yok; B memesinin alanı hedefin bir komşusunu kapsar.
  İki alan birbirine değmez. RNG yok (en geniş alan, merkeze yakınlık, y, x).
- **İşaret:** memeler baskın boyunca simgeli; tur şeridinde "SÖNDÜRME" çipi. Dolacak alan bir tur önce kesik beyaz
  çerçeveyle ve "BULUT 1" yazısıyla; bulutlu turda alan dolu beyaz ve "DAĞILIR". Buluta kesilen hat kesik çizgiyle biter
  (sis gri, bulut beyaz).
- **İki taraf:** bulut hedefin önünde bir ateşkes alanı açar. İçeride Teknisyen söker, bakım robotu onarır, ikisi
  birbirine vuramaz; yarış iştir. Gelişmiş akıl bulutlu turda bakım robotunu buluta, hedefin yanına yollar, dronları
  bulutun dağılacağı turda Teknisyen'in çıkacağı karelere hat çektirir. Oyuncu da uzmanını dron hattındaki bulutlu
  kareye sokar.
- **Neden burada:** başlangıç bölgesi. Güvenli girişte 1. tur uyanma yok ve bulut 2. turda gelir: 1. turdaki işaret
  kayıpsız bir öğrenme turudur. Zırhlı dronun uzun hatlarını turdan tura keser, ama EMP'yi ve kancayı da keser.
- **Karşı hamle:** buluttaki makineyi EMP'yle çıkarmak için EMP'yi kolun yanına, merkezin çapraz komşusuna at; itme onu
  yandan dışarı atar. Merkezdeki birim itilerek çıkmaz. Kancayı ve EMP'yi bulut dolmadan kullan.
- **Eleştiriden:** ilk sürüm sis bombasının aynısıydı (yalnız içeri nişan yok) ve görünmez argon gazıyla inandırıcı
  değildi; iki yönlü körlük ve iş istisnası onu sisten ayırdı. "Hedefin yanında dokunulmaz bakım robotu" riski kalktı:
  robot dokunulmaz ama o da kimseye vuramaz.
- **Yedek:** zaman ayarlı yangın kepengi. İki tek karelik geçit çift turlarda duvara döner; eşikte duran birim, kim olursa
  olsun, kepengi açık tutar.

### Doğu Avrupa · Kayar arşiv rafı

Sovyet döneminden kalma bir devlet arşivinin dosya katı. Raylı kompakt raflar; YZ arşivi tararken raf bloğunu kaydırır.
- **Kural:** 4 karelik düz bir rayda 3 raf (duvar) ve 1 geçit var. 3. ve 5. tur sonunda blok bir kare kayar ve geçit
  rayın öbür ucuna atlar. Kapanan geçitte duran birim 1 hasar alır ve geçidin dik yanlarından boş olana itilir (ikisi de
  boşsa kuzey ya da batı). İki yan da kapalıysa birim yerinde kalır ve raf o evre kaymaz (yine 1 hasar). Raf duvar
  kurallarıyla işler: üstünden yürünmez; atış, kilit ve kanca durur; EMP ve havan aşar. Madenci bir rafı yıkarsa blok
  kalıcı olarak durur.
- **Yer:** ray x 2–6, nesnesiz ve birimsiz zemin; çıkış, çıkış komşusu ve hedef komşusu yok. Ray girişlerle hedef
  arasındaki ana ateş hattına dik durur, iki konumda da her zemin karesinden çıkışa yol kalır. RNG yok.
- **İşaret:** ray ve makaralar hep çizili, geçidin olduğu uç yeşil. Tur şeridinde 3 ve 5 çentikli. Kayma turunda blokta
  ok, "RAF 1"; geçitteki birimin itileceği kare ve "1 hasar". Kaymadan sonra açılacak ve kapanacak ateş hatları soluk
  çizgiyle gösterilir.
- **İki taraf:** olay bir siper ve hat olayıdır: kayan duvar iki tarafın ateş hattını ve yolunu aynı anda açar ya da
  kapar. Gelişmiş akıl kaymadan önce geçitte durmaz (−1,5) ve geçidin açacağı hatta önceden yerleşir. Kilitleyici, kayma
  evresinden önce geçitteki uzmanı kilitlemeyi +2 puanlar. Oyuncunun aracı kanca ve itmedir: makineyi geçide çeker.
- **Neden burada:** dar koridorda yollar zaten uzun; geçidin hangi uçta olduğu iki taraf için en kısa yolu belirler.
  Kilitleyicinin kilidi yürüyüşü keser, rafın itmesini kesmez.
- **Eleştiriden:** ilk sürümde itme önü boşsa zararsız bir bedava adım, önü doluysa bedenle durdurulan bir raftı; kural
  kendiyle çelişiyordu. Artık raf her zaman ezer ve kayar. Hindistan turnikesine benzememesi için olay "değişen geçit"
  değil "kayan siper" olarak kuruldu ve takvimi üç turda bire çekildi.
- **Yedek:** bölme kapısı şalteri. Turunu ya da planını şalter karesinde bitiren birim, kim olursa olsun, çelik bölmeyi
  açar ya da kapatır.

### Orta Doğu · Yanıcı gaz kaçağı

Körfez'de bir rafinerinin kompresör dairesi. YZ gaz dedektörlerini susturdu; sızdıran bir flanştan yanıcı gaz geliyor.
- **Kural:** kompresör çevrimi bulutu takvimle doldurup boşaltır: 1. tur sonunda dolar (2.–3. tur dolu), 3. tur sonunda
  boşalır, 4. tur sonunda yeniden dolar (5.–6.), 6. tur sonunda boşalır (ikmalde 7. tur sonunda bir daha dolar). Bulut
  büyümez; her dolumda aynı 5 kareyi kaplar. Yürüyüşü ve görüşü kesmez. Buluta ateş değerse bulut "ALEV ALDI" olur ve
  fitil kuralıyla bir sonraki turun evre sonunda parlar: buluttaki herkes 1 hasar alır, bulut silinir, kompresör bir
  sonraki dolum zamanına kadar doldurmaz. Alev almış bulut boşalma zamanı gelse de boşalmaz, parlar.
- **Ateş sayılan (tek ölçüt):** hasar veren atışın geçtiği ve vurduğu kareler, havanın düştüğü kare, patlama alanı
  (patlayan robot, soğutma ünitesi), EMP'nin düştüğü kare. Sayılmayan: ateş edenin karesi, kablo, dinamit, vuruş, kilit,
  onarım, sabotaj, kanca, sis.
- **Yer:** sızıntı hedefe 2 kare uzakta, duvara bitişik zemin, çıkışlara en az 3 kare. Bulut sızıntıdan BFS'le kurulur ve
  önce hedefin komşularını alır; çıkışlara ve komşularına girmez. RNG yok.
- **İşaret:** bulut sarımsı tarama (kum fırtınası uyanma yerlerini gizler, bulutu gizlemez); dolum ve boşalma tur
  şeridinde. Buluta değecek her niyetin ve hamlenin oku turuncu, alev rozetli. Alev almış bulut kırmızı titrer, "PARLAMA
  1"; içindeki birimlerin kartında "PARLAMA: 1 hasar".
- **İki taraf:** bulut hedefin önünde durur: Teknisyen de bakım robotu da bulutta çalışmak zorunda. İki tarafın da ateşli
  ve ateşsiz eylemleri var (ekipte EMP ateşli; sabotaj, kanca, sis ateşsiz. Makinelerde atış, havan, patlama ateşli;
  vuruş, kilit, onarım ateşsiz). Tutuşturan kendi birimini de yakabilir. Fitil yüzünden iki taraf da bir kez kaçar;
  yanan, işini bırakmayan ya da kaçamayandır. Gelişmiş akıl alev almış buluttan çekilir, bakım robotunu yalnız onarım
  bitecekse bırakır.
- **Neden burada:** patlayan robotun patlaması buluta değerse bulut alev alır; bombacı bulutun yanında iki tarafa tehdit.
  Kum fırtınası yüzünden bulutun içinde uyanan makine sürpriz olabilir ama hasar almaz; parlama hep bir tur önce görünür.
- **Eleştiriden:** ilk sürümde oyuncunun EMP'si plan anında yerleşmiş makineleri anında yakıyordu (fiilen EMP uzmanının
  alan silahı); fitil bunu kapattı. Bulut büyümesi Okyanusya yağıyla aynıydı ve ateş listesi 10'dan fazla çağrı noktasına
  dağılıyordu; takvimli çevrim ve dört maddelik tek ölçüt bunları çözdü.
- **Yedek:** köpüklü söndürme. İki 3×3 bölge takvimle köpükle dolar; köpüğe yürüyen orada durur, köpükteki birim patlama
  hasarı almaz.

### Afrika · Çatlak tavan

YZ'nin yarım kalmış cevher işleme hangarı. Açık arazi özelliğinin yıktığı duvarlar aslında taşıyıcıydı; üstlerindeki
tavan çatlak.
- **Kural:** 3–4 çatlak kare var. Çatlakta ya da dört komşusundan birinde darbe olursa çatlak "ÇÖKECEK" olur ve fitil
  kuralıyla bir sonraki turun evre sonunda çöker. Altında birim varsa 1 hasar alır, enkaz oluşmaz. Kare boşsa enkaz yığını
  kurulur: 2 canlı nesne; üstünden yürünmez; atış, kilit ve kanca durur; havan ve EMP aşar; kırılabilir. Enkaz bir zemin
  karesini çıkıştan ya da girişi hedeften koparacaksa kurulmaz, toz dağılır. Her çatlak bir kez çöker.
- **Darbe sayılan:** itme çarpışması (itilenin ve çarptığının karesi), patlama alanı, havanın düştüğü kare, dinamitle
  yıkılan duvar ve moloz.
- **Yer:** önce açık arazinin yıktığı duvarlar (haritanın satırlarında duvar, tahtada zemin olan kareler); az kalırsa x
  2–6'da duvara bitişik zemin. Çıkışlar ve komşuları, hedefin komşuları, kopya dolabının komşuları ve taşıyıcının kaçış
  yolu (`KeyTiles`) çatlak olamaz. Ek RNG yok.
- **İşaret:** çatlak doku baskın boyunca çizili. Tetiklenen kare kırmızı kesik çerçeve ve tozla "ÇÖKECEK 1"; karedeki
  birimin kartında "TAVAN: 1 hasar", kare boşsa "ENKAZ". Hamle önizlemesi ve makine niyetleri (havan, patlama)
  tetikleyecekleri çatlağı aynı işaretle gösterir.
- **İki taraf:** tavan kimin altında çökerse onu vurur, enkaz iki tarafın hattını keser. Çöküşü iki taraf da tetikler:
  ekip itme çarpışmasıyla ve dinamitle, makineler havanla ve patlamayla. Fitil yüzünden hasar ancak kilitlenene ya da
  itilene düşer; asıl ödül, iki tarafın yarıştığı enkaz siperidir. Gelişmiş akıl çökecek karede durmaz (−2). Enkaz
  Teknisyen'in yolunu uzatacaksa tetiklemeyi +2, kendi dronunun hattını kapatacaksa −2 puanlar.
- **Neden burada:** açık arazide yıkık duvarlar uzun hatlar açar; çöküşler oyuna siper geri getirir. Havanın düşüşü
  çatlakları tetikler, havan da engelin üstünden attığı için enkazdan korunmaz.
- **Eleştiriden:** ilk sürümde oyuncunun darbesi aynı evrede çöküyordu; yürüyüşü 1 olan havan hiç kaçamıyordu. Kopya
  dolabının yanındaki enkaz uyanma adayını sildiği için yalnız makineyi, kaçış yolundaki enkaz da yalnız uzmanı
  vuruyordu; iki yer de yasaklandı.
- **Yedek:** köprülü vinç kovası. Vinç kovayı bir satırdaki üç durak arasında her evre taşıyıp indirir; indiği duraktaki
  birimi iter ve hareketli bir siper olur.

### Kuzey Amerika · Yük asansörü

Kıta dağıtım merkezinin yük asansörü holü. İki duraklı yük platformu YZ'den önce yazılmış bir zaman rölesiyle çalışır.
- **Kural:** kabin 2. tur sonunda A'dan kalkar, 3. tur sonunda B'ye iner; 4. tur sonunda B'den kalkar, 5. tur sonunda
  A'ya iner. Kalkışta duraktaki birim kabine biner: bir evre tahtada yoktur, hedef alınmaz, eylem yapmaz. İniş durağında
  birim varsa o birim 1 hasar alır ve kabin bir evre daha bekler. Son iki turda kabin kalkmaz. Hasar yalnız inişte.
- **Yer:** B hedefin komşusudur: Teknisyen'in de bakım robotunun da istediği kilit kare. A uzak yarıda, bir kopya
  dolabının yanında. İkisi de nesnesiz zemin, çıkış ya da çıkış komşusu değil. RNG yok.
- **İşaret:** iki durak sarı çerçeveli, kabinin yeri çizili. Tur şeridinde kalkış ve iniş. Kalkış turunda "KALKIŞ 1" ve
  duraktaki birimin ineceği yer hayalet; iniş durağındaki birimin kartında "İNİŞ: 1 hasar".
- **İki taraf:** ilk sefer YZ'nin takviye hattıdır: A'daki makine hedefin dibine iner. İkinci sefer hedefte çalışanı uzağa
  götürür: B'de bekleyen Teknisyen de bakım robotu da uzak yarıya iner. Oyuncu kalkıştan önce makineyi A'dan iter ya da
  B'deki bakım robotunu bilerek orada bırakıp uzaklaştırır. Kabin iki tarafa da bir evrelik sığınaktır: hedef alınmayan
  birim orada saklanır. Gelişmiş akıl kalkıştan önceki planda durağın değerini öbür durağa göre hesaplar.
- **Neden burada:** kısa nöbette baskın bir tur kısa; havanın yürüyüşü 1 ve tahtanın öbür yarısına ancak asansörle geçer.
- **Eleştiriden:** ilk sürümde duraklar yalnız oyuncunun istediği yerlerdeydi (girişin yanı ve hedefin önü) ve anlık takas
  bilinen ışınlanma pedlerine fazla yakındı. Durakların yeri iki tarafı bağlayacak biçimde değişti, kabin yolculuğu süre
  ve tahtadan çıkma getirdi.
- **Yedek:** otopark döner tablası. 2×2 tabla her evre çeyrek tur döner ve üstündeki her birimi bir köşe taşır.

### Rusya · Kriyo soğutma hattı

Sibirya'da soğuk depolu bir veri merkezi. Sıvı azotlu soğutma hattı YZ'nin çevrimine bağlı ve takvimle boşalır.
- **Kural:** 2. ve 4. tur sonunda (ikmalde 6.'da da) hat 2×3'lük bir bölgeye boşalır; sonraki tur son tursa boşalmaz.
  Bölgede duran her birim donar: sonraki sırasında yürüyemez ama eylemini yapar. Donan birime 1 hasar emen bir buz kabuğu
  gelir; çözülünce (sırası bitince) kabuk gider. İtme, çekme ve atılma donmuş birime de işler.
- **Yer:** x 2–6'da 2×3 ya da 3×2 dikdörtgen, en az 4 nesnesiz zemin ya da kablo; hedefin bir komşusunu içerir, hedefi
  içermez; çıkışlara en az 3 kare. Soğutma ünitelerine en yakın olan seçilir. RNG yok.
- **İşaret:** bölge buz dokulu kesik çerçeveli; tur şeridinde boşalma turları. Boşalma turunda "DONDURMA 1" ve bölgedeki
  her birimin kartında "DONACAK: yürüyemez · buz 1 hasar emer". Donan birimin üstünde kar tanesi; makinenin sonraki niyeti
  bulunduğu kareden çizilir.
- **İki taraf:** donma, kilitleyicinin iki tarafa işleyen hâlidir; buz kabuğu ise yerinde çalışana ödüldür. Hedefin
  yanında söken Teknisyen de onaran bakım robotu da bilerek donmak isteyebilir; yürümesi gereken kaybeder. Gelişmiş akıl
  yakın dövüşçüleri ve kalkan dronunu bölgeden uzak tutar (−1,5), atıcıları ve bakım robotunu bilerek sokar.
- **Neden burada:** soğutma ağında bölge ünitelerin yanında; donan birim ünite patlamasından yürüyerek kaçamaz. Kalkan
  dronu donarsa ortağına gidemez.
- **Eleştiriden:** "yürüyemez ama eylem yapar" uzaktan vuran makineye neredeyse bedavaydı, uzmanı görevden alıkoyuyordu;
  bölgede makinelerin itmesi de yoktu. Buz kabuğu ve hedef komşusu bunu dengeledi. "Şok dondurma tüneli" bir veri
  merkezine zorlamaydı; kriyo hattı oldu.
- **Yedek:** soğutma suyu pompası arızası. Soğutma üniteleri baştan yazılı bir sırayla çift turlarda aşırı ısınıp patlar;
  sıradakini erken kırmak patlamanın anını seçmektir.

### Hindistan · Vardiya turnikeleri

Bengaluru'da bir banka ve sigorta çağrı merkezinin kasa holü. YZ kart okuyucuları kapattı; turnikeler yalnız mekanik
yön kilidiyle döner.
- **Kural:** hedefin çevresindeki holün sınırındaki geçişler turnikedir. Turnike kareyi değil, iki kare arasındaki kenarı
  kilitler. "İçeri" vardiyasında kenar yalnız dışarıdan içeri, "dışarı" vardiyasında yalnız içeriden dışarı geçilir.
  Vardiya 1.–2. turda içeri; 2. ve 4. tur sonunda döner. Son iki turdan önceki evrenin sonunda tahliye moduna geçer ve
  baskın bitene kadar dışarı kalır. Holün çıkışlardan en uzak kenarı sıradan geçittir, iki yöne de açık. Ters yönde
  itilen ya da çekilen birim kenara çarpar: 1 hasar, yerinde kalır. Atış, kilit, kanca hattı, EMP ve havan kenardan geçer.
- **Yer:** hol = hedef ve sekiz komşusu (3×3); sınırda iki yanı zemin olan her kenar turnike. RNG yok.
- **İşaret:** kenarlar yön okuyla çizili; tur şeridinde vardiya ve tahliye. Dönüş turunda oklar yanıp söner, "VARDİYA 1".
  Yürüyüş erişimi ve itme önizlemesi yönü hesaba katar; ters itmede okun ucu kırmızı, "1 hasar".
- **İki taraf:** yön kilidi kimin yürüdüğüne bakmaz. İçeri turunda Teknisyen de bakım robotu da hole girer; dışarı
  turunda içeridekiler çıkabilir ama kimse giremez. Holde onaran robot dışarı turunda dışarıdan yardım alamaz, holde
  söken Teknisyen de. Makineler `Reachable` kullandığı için kuralı kendiliğinden uygular. Gelişmiş akıl dönüşten önceki
  turda holün doğru yakasında beklemeyi +1 puanlar.
- **Neden burada:** kalabalık şehirde gelişmiş akıl, ekibe ulaşamayan makineyi sivil sunucuya nişan aldırır; hol
  kapandıkça rehine atışları artar. Ekip yolu kesmekle sivilleri korumak arasında seçer.
- **Eleştiriden:** ilk sürümde turnike sırası tahtayı ikiye bölüyordu; girişler batıda, makineler doğuda olduğu için
  turnikeyi iki kez geçmek zorunda olan yalnız ekipti. "Üstünde durulabilir kare" ile "dikey giriş yok" birleşince birim
  sıkışabiliyordu ve kanca, atılma, itme için üç ayrı kural vardı. Hol ve kenar kilidi bunları tek bir "kenar geçilebilir
  mi" yardımcısına indirdi; tahliye modu kaçışı güvenceye aldı.
- **Yedek:** metro treni. Çift turlarda ray şeridindeki her birim sabit peron yanına itilir; ray atışı kesmez.

### Çin · Kaynak robotu kolu

YZ'nin kendi robotlarını kaynakladığı bir montaj fabrikası. Zemine cıvatalı bir kaynak kolu ağdan kopmuş, döngüsünü
kimseye sormadan sürdürüyor.
- **Kural:** kolun bu evreki kaynak karesi kilitlidir. Her evre sonunda o kareye kaynak yapılır (karedeki birim 1 hasar),
  sonra kol saat yönünde bir adım döner ve yeni yön bir sonraki evrenin kilitli karesi olur. Kola gelen her darbe (atış,
  havan, vuruş, EMP, sabotaj, patlama, itme çarpışması) bu evrenin karesini değiştirmez; evre sonundaki dönüşe bir adım
  ekler. Duvara, kenara ya da hedefe bakan yönler atlanır. Kaide hasar almaz, üstünden yürünmez. Kaynak nesnelere
  işlemez. Son turda kaynak yok.
- **Yer:** kaide hedefle aynı satır ya da sütunda, tam 2 kare uzakta; aradaki kare (hedefin komşusu) zemin. Çıkışlara en
  az 3 kare. Hedefin en az iki komşusu açık kalmalı. RNG yok.
- **İşaret:** kilitli kare kıvılcımlı, "KAYNAK"; sıradaki kare soluk. Kolu vuracak niyetlerin ve hamlelerin önizlemesi
  sıradaki kareyi günceller. Karedeki birimin kartında "KAYNAK: 1 hasar".
- **İki taraf:** kaynak karede kimin durduğuna bakmaz, kolu çeviren darbe de kimden geldiğine. Kaynak dört evrede bir
  hedefin komşusuna gelir: Teknisyen'in ya da bakım robotunun karesine. İki taraf da kolu vurarak kaynağı öbürünün
  karesine getirmeye çalışır ve bunu bir hareket önce görür. Gelişmiş akıl kilitli karede durmaz; kolu çevirecek bir
  atışın puanına sıradaki karedeki birimin değerini ekler (uzman +9, makine −3).
- **Neden burada:** zırhlı dolapta söküm uzun sürer; ekip de bakım robotu da kolun yanında çok tur geçirir. Kaynak
  karesine uyanan ya da çoğaltılan dron hemen yanar; çoğaltıcı yönünü buna göre seçer.
- **Eleştiriden:** ilk sürümde darbe kolu anında çeviriyordu; oyuncu sabotajla kaynağı kıpırdayamayan bakım robotunun
  üstüne getiriyordu. Kaynak kopya dolabını vurursa yalnız YZ altyapısı zarar görüyordu ve kaide hedefin açık
  komşularını ikiye indirebiliyordu. Kilitli kare, nesne muafiyeti ve "iki komşu açık" şartı geldi; Forecast'ın darbeleri
  sırayla oynatma ihtiyacı da kalktı.
- **Yedek:** temiz oda hava kilidi. İki kapılı, tek kişilik bir geçit; kapılar asla birlikte açılmaz, kilidi tutan taraf
  kısa yolu ve ateş hattını kapatır.

### Japonya-Kore · Şebeke faz değişimi

Bir çip fabrikasının akıllı enerji yönetimi. Kat panoları yükü iki faz arasında aktarır; döşeme altındaki kanallar
sırayla canlanır.
- **Kural:** kablo kanalları iki faza ayrılır. A fazı 1.–3. turda, B 4.–6. turda, A yeniden 7.–8. turda canlıdır. Her evre
  sonunda önce biten fazın hasarı işler (canlı kanalda duran her birim 1 hasar), sonra faz değişir. Canlı kanala itilen,
  çekilen ya da atılan birim bugünkü kural gibi 1 hasar alır; sönük kanal sıradan zemindir. Hasar tavanı burada da geçer:
  itmeyle canlı kanala giren birim o evre sonunda ayrıca yanmaz. Son turda evre sonu hasarı yok.
- **Yer:** kablo grupları (birbirine değen kanallar) sıralanıp sırayla A ve B olur. Tek grup varsa uzun ekseninden ikiye
  bölünür (şebeke haritasındaki dikey kuşak y'den bölünür). RNG yok.
- **İşaret:** kanallar iki renkli, canlı faz parlak. Tur şeridi her turun canlı fazını baştan gösterir. Canlı kanaldaki
  birimin kartında "ŞEBEKE: 1 hasar"; itme önizlemesi sönük kanalda hasar göstermez.
- **İki taraf:** akım tarafa bakmaz; sönük kanal iki tarafa da güvenli yoldur. Gelişmiş akıl canlı kanalda durmaz (−3).
  Kilitleyici kanaldaki uzmanı kilitlemeyi +3 puanlar; makinelere gerçek bir araç verir. Oyuncu da makineyi canlı kanala
  iter.
- **Neden burada:** elektrik şebekesi tahtaya kanal ekler; tahtanın yarısı tuzak, yarısı yol olur ve üç turda bir yer
  değiştirir. Havan ve kilitleyici yavaştır, canlı kanaldan zor çekilir.
- **Eleştiriden:** değişim evresinde hasar ile faz değişiminin sırası belirsizdi; şebeke haritasındaki dikey kuşak "x
  ortasından böl" kuralıyla bölünmüyordu; canlı kanala itilen makine aynı evrede 2 hasar alıp tek hamlede düşüyordu.
  Üçü de düzeltildi; takvim üç tura çekildi.
- **Yedek:** kaçak akım. İki turda bir arızalı kablo grubunda duran birimden başlayan akım bitişik her birime atlar; yan
  yana duran uzman ve makine birlikte çarpılır.

### Güneydoğu Asya · Süzgeç emmesi

Penang ya da Ho Chi Minh'te bodrumu su basmış bir elektronik fabrikası. Sintine pompası üç turda bir geri çalışır ve yer
süzgeci suyu çeker.
- **Kural:** 3. ve 6. tur sonunda süzgeç emer: dört komşu karedeki birimler süzgece doğru 1 kare çekilir (sıra kuzey,
  doğu, güney, batı). Süzgeç boşsa ilk çekilen süzgece girer; sonrakiler süzgeçteki birime çarpar: ikisi de 1 hasar alır
  ve çekilen yerinde kalır (hasar tavanıyla süzgeçteki birim en çok 1 hasar alır). Süzgecin üstünden yürünür. Son turda
  emme yok.
- **Yer:** x 2–5'te nesnesiz zemin; çıkışlara en az 3 kare, hedefe bitişik değil ama bir komşusu hedefin komşusu.
  Kopya dolabının komşularına en az 2 kare uzak. En az iki komşusu zemin. RNG yok.
- **İşaret:** süzgeç ve dört komşusu hep çizili; tur şeridinde emme turları. Emme turunda "EMME 1" ve komşulardaki
  birimlerin çekileceği kare okla; çarpışma varsa okun ucu kırmızı, "1 hasar".
- **İki taraf:** emme tarafa bakmaz: hedefin yanında çalışanı da, süzgecin yanında bekleyen makineyi de içeri çeker ve
  birbirine çarptırır. Gelişmiş akıl emmeden önce süzgeç komşularında durmayı −1,5 ile cezalandırır; ama emme bir yakın
  dövüşçüyü ekibe yaklaştıracaksa seçebilir. Oyuncu da makineleri süzgeç komşularına çeker.
- **Neden burada:** kopya fabrikası ve çoğaltıcı yüzünden makineler kümelenir; süzgeç kümeleri bir araya toplar.
- **Eleştiriden:** ilk sürüm (dışarı savuran geri tepme) EMP'nin çevre itmesinin takvimli bir kopyasıydı ve süzgeç kopya
  dolabının dibinde olduğu için uyanan makineyi savurup dolabı hasarlıyordu: sonuç yalnız YZ'yi vuruyordu. Emme EMP'nin
  tersi oldu, süzgeç dolaplardan uzaklaştı ve olay nesnelere hasar vermez.
- **Yedek:** damgalama presi. İki pres sırayla iner ve altındaki birime hasar verir; uyanma karesine inen pres yeni
  makineyi ezer.

### Güney Amerika · Basınçlı tüp rafı

Bir hidroelektrik santralinin türbin salonundaki bakım atölyesi. Oksijen ve asetilen tüpleri zincirlenmeden duvar
diplerine dizilmiş; vanası kopan tüp füze gibi uçar.
- **Kural:** 3 tüp var (3. kademeden 4). Her tüpün vanası kurulumda duvardan dışarı bakar ve uçuş hattı sabittir. Tüp
  hasar alınca (kim vurursa vursun) vanası çatlar, "ISLIK" çalar ve fitil kuralıyla bir sonraki turun evre sonunda
  hattı boyunca ilk engele kadar uçar. Çarptığı birim 1 hasar alır ve aynı yönde 1 kare itilir. Başka bir tüpe çarparsa
  onu kırar (o tüp uçmaz); öteki nesnelere bir şey olmaz; duvara ya da kenara çarpan tüp düşer. Zincir tek halkadır.
- **Yer:** tüpler x 2–6'da duvara bitişik zeminde; çıkış ya da çıkış komşusu değil, hedefe ve sivil sunucuya bitişik
  değil, birbirlerine en az 3 kare uzak. En az bir tüpün hattı hedefin bir komşusundan geçer. RNG yok.
- **İşaret:** her tüpün uçuş hattı baştan soluk turuncu bir şerit. Islık çalan tüpün hattı parlak, "TÜP 1"; hattaki ilk
  birimin kartında "TÜP: 1 hasar + itme". Tüpü vuracak niyetin okunda "TÜP" rozeti.
- **İki taraf:** tüp hattındaki ilk birimi vurur ve kimin olduğuna bakmaz. Hat iki tarafın da kaçındığı ya da düşmanı
  çektiği bir şerittir. Gelişmiş akıl ıslık çalan tüpün hattında durmaz (−3); hatta hedefte çalışan bir uzman varsa
  tüpü vurmayı +4 puanlar. Oyuncu da tüpü, hattında kilitli ya da hedefe bağlı bir makine varken vurur.
- **Neden burada:** takviye bölgesinde başta bir makine fazla ve hatlar dolu; patlayan robotun patlaması dört yanındaki
  tüpleri çatlatır.
- **Eleştiriden:** ilk sürümde tüp darbenin yönünde uçuyordu; yönü bitişik kareden seçen Teknisyen için her tüp bedava
  bir uzun menzilli atış ve itme oldu, makinenin tüpü vurması ise rastlantıydı. Dört halkalı zincir oyuncu turunda
  okunmuyordu ve yönlü hasar altı çağrı noktasına dokunuyordu. Sabit hat, fitil ve tek halka üçünü de çözdü.
- **Yedek:** kazan basıncı. Kazanın sayacı her evre ve her vuruşla düşer; sıfırlandığı evrenin sonunda valf açılır,
  dört komşudaki herkese 1 hasar verip onları dışarı iter.

### Okyanusya · Hidrolik yağ sızıntısı

Pilbara'da uzaktan yönetilen bir maden-liman tesisinin makine dairesi. Hidrolik kaldırıcının hortumu patlamak üzere.
- **Kural:** 2. tur sonunda hortum bir kez patlar ve sabit, düz bir hat boyunca (4–5 kare) yağ boşaltır; yağ büyümez,
  baskın boyunca kalır. Yürüyüş yağda kesilmez. Yağdaki birim itilir, çekilir, atılır ya da isabet alırsa (atış, vuruş,
  havan; isabet yönünde) yağ bitene ya da önü kapanana kadar kayar. Önü kapanırsa itme kuralıyla çarpar: 1 hasar,
  çarptığı birim de 1 hasar. Yağdan kabloya kayan 1 hasar alır. Analist'in atılması da kayar.
- **Yer:** hat x 3–5'ten başlar, nesnesiz zeminde; çıkışlar ve komşuları yağlanmaz. Hat hedefin bir komşusundan ya da
  girişle hedef arasındaki yoldan geçer. RNG yok.
- **İşaret:** 1. turda hortum ve yağın kaplayacağı hat kesik çerçeveyle, "HORTUM 1"; sonra yağlı kareler parlak siyah.
  İtme, çekme ve isabet önizlemesi kaymayı sonuna kadar çizer; sonda çarpma varsa okun ucu kırmızı, "1 hasar".
- **İki taraf:** yağ kimin kaydığına bakmaz. Makineler de yağdaki uzmanı atışla kaydırır: dronlar bu bölgede bir itme
  aracı kazanır; oyuncunun aracı EMP, sabotaj ve kancadır. Gelişmiş akıl, hattında bir atıcı varsa yağda durmayı −1 ile
  cezalandırır ve yağdaki uzmanı vurmayı kayma sonucuna göre puanlar (çarpma varsa +2).
- **Neden burada:** hızlı devriyede makineler bir kare fazla yürür; yağ onları uzun hatlarda birbirine çarptırır. Zırhlı
  dronun zırhı kayıp çarptığı ilk hasarı karşılar.
- **Eleştiriden:** ilk sürüm önceki turda reddedilen iki fikrin (yürüyüşü kesen çamur, itilenin kaydığı buz) birleşimiydi;
  büyümesi Orta Doğu bulutuyla aynıydı ve kayma yalnız oyuncunun aracıydı. Yürüyüşü kesen yağ son turda uzmanı çıkışa
  ulaştırmayabiliyordu. Sabit tek seferlik yağ, isabetle kayma ve Analist istisnasının kalkması bunları çözdü.
- **Yedek:** zırh kaynak peronları. Sırayla çalışan iki peronda evre sonunda duran birim, kim olursa olsun, baskın boyunca
  1 geçici zırh plakası alır.

## Uygulama önerisi

- **Veri:** `RegionDef.Event` (`GameData`'daki `R(...)` çağrısına bir argüman), `Baskin.RegionEvents` (ad ve ipucu,
  `Loc.N`), `RaidParams.Event` (`CampaignLiberation.StartLiberationRaidCore` bölgeden verir). Dengede bir aç/kapa anahtarı
  (`liberationRegionEvents`). Eğitim baskını, örnek baskın ve motor testleri olay vermez.
- **Durum:** `BState`'e `Ev`, `EvTiles`, `EvState` ve fitil listesi (kare, işleyeceği tur). Hücreye yalnız gereken
  bayraklar (bulut, yakıt, çatlak, faz, yağ), turnike için kenar listesi, birime donma ve buz kabuğu, kabindeki birim için
  tahta dışı durumu. `Cell.Clone`, `BState.Clone` ve `CopyInto` her yeni alanı kopyalamalı; yoksa geri alma olayı geri
  sarmaz. Klonlanmayan geçici bir `InEvent` bayrağı: olay adımı ve zinciri sürerken `HurtObj` nesnelere işlemez. Hasar
  tavanı için birim başına evre içi olay hasarı sayacı.
- **Evredeki yer:** `EventSetup(B)` `Create`'te bütün RNG çekimlerinden ve kadro raporundan sonra, `PlanEnemies`'ten önce
  çalışır; RNG çekmez ve birimli kareye bir şey koymaz. `EventEnd(B)` `EndTurn`'de uyanmadan sonra, `CheckEscape`'ten önce
  çalışır (son turda çalışmaz): takvimli adımlar ve olgunlaşan fitiller burada işler. Uyanma döngüsü duvar denetlemediği
  için duvar değiştiren olaylar uyanmadan sonra gelir. Tepkili kurallar (tutuşma, darbe, tüp vuruşu, kol darbesi, yağda
  kayma, turnike kenarı) kendi çağrı noktalarında yalnız `B.Ev` eşleşince çalışır.
- **Ortak yardımcılar:** `ExitSafe(B)` (çıkışlardan BFS; her zemin karesi ve hedef bağlı kalmalı). Kareye girme ve kenar
  geçme kuralı tek yerde (kablo, faz, turnike, yağ); yürüyüş, itme, kanca çekişi ve atılma onu kullanır. `InEvent`'li
  olay itmesi. `HookTrace` benzeri hat izleme (tüp, atış).
- **Kayıt ve RNG:** olaylar `Rnd` çağırmaz; olaysız baskında tahta, RNG akışı ve kilitli imzalar aynı kalır. `Reachable`,
  `IntentTarget`, `Push` ve `HurtObj`'e eklenen denetimler olay yokken hiçbir dalı değiştirmez. `RaidParams.Event` bölge
  tanımından türediği için `GameRecord.CurrentVersion` artmaz, `LiberationCampaignTests`'in kayıt testleri etkilenmez.
  `Signature`'a olay eki yalnız olay varken yazılır.
- **Makine aklı:** plan `EventEnd`'den sonra çalıştığı için olayın bu evredeki sonucunu görür; bilmesi gereken yalnız
  sonraki evredir. İki yardımcı: `EventPos(B, e, pos)` `posScore`'a, `EventAim(B, e, …)` `ScoreTarget`'e eklenir; ikisi de
  yalnız `B.Smart` ve `B.Ev` varken çalışır. `Zone`, `Covers`, `KeyTiles` ve `CanReachTeam` yeni hücre ve kenar kurallarına
  uyar. Temel akıl yalnız kurallardan etkilenir; RNG tüketimi aynı kalır.
- **Önizleme ve sunum:** `Forecast(B)` kopyada niyetleri sırayla oynatıp `EventEnd`'i uygular (RNG yok, işaretli uyanma
  kareleri dolu sayılır). `Baskin.Hazards(B)` (`Intents` gibi) olay karelerini, okları, varış hayaletlerini, birim
  uyarılarını ve tur şeridi çiplerini verir. Yeni `PreviewMove(B, uid, x, y)` yürüyüş karesinde olayın sonucunu gösterir.
  `RaidDataSource` yeni katmanı `RaidBoard3D`'ye verir. Görev fişinde ve bölge fişinde özelliğin altında bir OLAY satırı;
  telsizde kadro raporundan sonra bir "saha raporu" satırı. Olaylı baskında görünen oda adı olayın binasından gelir
  (yalnız sunum; harita seçimi ve RNG aynı kalır), böylece "arşiv rafı" otopark katına düşmez.
- **Yer bulma:** olayların çoğu özel geometri istiyor (düz ray, hedefle aynı satır…). Koşullar iki kademede gevşer; son
  kademe her harita türünde yer bulmalı. Olay sessizce kaybolmaz; harita türü başına kurulum başarısı testle kilitlenir.
- **Lokalizasyon:** adlar, ipuçları ve "BULUT 1", "RAF 1", "KALKIŞ 1" gibi etiketler `Loc.N`/`Loc.F` ile; sonra çeviri
  tablosu güncellenir, `strings.txt`'e İngilizceleri yazılır, `LocalizationTests` geçmeli.
- **Test ve denge:** `RaidEngineTests`'te olaysız imzalar değişmemeli; her olay için bir tohumun imzası kilitlenir. Simetri
  testi: aynı karede makine ve uzman aynı sonucu almalı. `ExitSafe`, hasar tavanı ve nesne muafiyeti testleri. `RaidBot`
  olayları `Forecast` üzerinden bilir; `RaidBotMeasurement`'a bölge başına olaylı ve olaysız başarı ile "olay hasarı:
  ekibe / makinelere" oranı eklenir. Oran 1:1'den çok saparsa olay yeniden ayarlanır. Sayılar önce `Baskin`'de sabit,
  ölçümden sonra `Balance`'a ve `BalanceConfig.asset`'e taşınır.

## Sıra

Bölgelerin oyuncuya açılma sırasına ve altyapının yeniden kullanımına göre:

1. **Altyapı ve Batı Avrupa söndürme bulutu (S).** Oyuncunun ilk gördüğü olay; sis kod yolunu yeniden kullanır.
   `Hazards`, tur şeridi çipi, fiş satırı, saha raporu, `InEvent`, hasar tavanı, `Signature` eki ve yer bulma kademeleri
   burada kurulur.
2. **Kuzey Amerika yük asansörü (S–M).** En basit evre sonu olayı; tahta dışı birim ve `EventPos` burada oturur.
3. **Doğu Avrupa kayar raf (M).** Duvar açıp kapama, olay itmesi ve `ExitSafe`.
4. **Afrika çatlak tavan (M).** Fitil altyapısı ve yeni nesne (enkaz).
5. **Orta Doğu yanıcı gaz (M).** Fitili yeniden kullanır, takvimli bulut; `Forecast` burada zorunlu olur.

Bu beşiyle başlangıç bölgesi ve dört komşusu biter; hepsi Teknisyen, EMP uzmanı ve Analist'le oynanabilir.

6. **Rusya kriyo hattı (S):** donma ve buz kabuğu.
7. **Japonya-Kore faz değişimi (S):** kablo hasarı faz yardımcısına taşınır.
8. **Güneydoğu Asya süzgeç (S):** olay itmesinin ters yönlüsü (çekme).
9. **Okyanusya yağ (S–M):** kareye girme yardımcısına kayma, isabetle kayma.
10. **Hindistan turnikeleri (M):** kenar kilidi ve yönlü `Reachable`.
11. **Çin kaynak kolu (M):** kilitli kare ve darbe sayacı.
12. **Güney Amerika tüp rafı (M):** sabit hatlar ve fitil; en çok hasar çağrı noktasına dokunduğu için en sona.

Her adımdan sonra derle, boş sahnede EditMode testlerini geç, bölgenin olaylı ve olaysız başarısını ve hasar oranını
botla ölç, oyunda dene.

## Açık riskler

- **Hamle sırası.** Fitil ve hedef komşuları açığın çoğunu kapatıyor, ama makinelerin itme ya da çekme eylemi hâlâ yok
  (yalnız kilit). Olayın içine birini sokmak çoğu bölgede oyuncunun işi; Doğu Avrupa ve Japonya-Kore'de kilitleyici,
  Okyanusya'da isabetle kayma makinelere araç veriyor. Hasar oranı ölçümü bunu izlemeli.
- **Ateşkes alanı (Batı Avrupa).** Bulutlu turlarda hedefte söküm ile onarım yarışı, hangi tarafa yattığı ölçülmeden
  bilinmez.
- **Rehine atışları (Hindistan).** Hol kapandıkça ekibe ulaşamayan makineler sivile döner; GÜVEN kaybı ölçülmeli (turda
  en çok bir rehine niyeti sınırı var).
- **Okunurluk.** Bölgeler arasında görsel dil karışmamalı: bulut beyaz, sis gri, yanıcı gaz sarı; kilit kıskaç, donma
  kar tanesi.
- **Yer bulma.** Özel geometri isteyen olaylar (ray, kaide, hol) bazı harita türlerinde yalnız gevşetilmiş kademede yer
  bulacak; o hâlleri oyunda görmek gerek.
