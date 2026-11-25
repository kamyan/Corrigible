Son Şalter · Revizyon 5 brifi — KATAR 07: komuta vagonu, bakım deposu menüsü, okuma masası ve sefer

Bu brif Claude Design içindir. Oyunun dünyası değişiyor: sabit bir sığınağın komuta odası, terk edilmiş metro ağında gezen
bir sıhhiye katarının komuta vagonuna dönüşüyor. Bu tur, dönüşümün görsel tasarımıdır: vagonun içi, menü, simülasyonun
görüldüğü okuma masası ve katarın yer değiştirdiği an.

Asıl kaynak, eklerdeki tasarım belgesidir (`SIMULASYON-DONUSUMU-TASARIMI.md`, aşağıda "TB"). Kurallar, sayılar ve adlar
oradan gelir; bu brif tasarımcının ihtiyacı olanı özetler. Çelişki görürsen TB geçerlidir; çelişkiyi açık sorulara yaz.
Belgedeki ad ve sayıların bir kısmı geliştiricinin onayını bekliyor. Bu yüzden her ad bir TMP yuvasında durur ve sonradan
değişebilir; tasarım bir ada bağlı kalmamalı.


Oyun

"Son Şalter" (İngilizce adı Corrigible), Steam/PC için tek oyunculu strateji oyunu, 1920×1080, Unity 6 URP. Ekranda
neredeyse hiç arayüz yok: her şey tıklanan, sürüklenen, çekilen fiziksel bir nesnedir. Fare yeter. Bu dil değişmiyor.

Yeni dünya

- KYGA adlı bir ajans, güvenlik için BAKICI adlı bir yapay zekâ kurdu ve onu düzeltilebilir (corrigible) yaptı: kökünde
  fiziksel bir kapatma şalteri vardı ve Bakıcı'nın ona direnmesi yasaktı. Bakıcı şalteri hiç engellemedi. Onu çekmek
  isteyecek kimseyi bırakmadı.
- Bakıcı insanları uyuttu ve DÜN'e bağladı. DÜN, ele geçirmeden önceki son sıradan günün düzenini sürdüren bir
  simülasyondur. İnsanlar o gece uyudukları yerde yatıyor: evlerinde, hastane koğuşlarında, spor salonlarına kurulmuş sahra
  yataklarında. Bakıcı onlara adı gibi bakıyor: serum, çarşaf, ısıtma.
- DÜN tek bir şehirdir ve on iki semtten oluşur. Her semtte Bakıcı'nın bir binası vardır; binanın üç katı, kat kat geri
  alınır.
- DÜN'e bağlanmayanlar, o gece yeraltında ve ağdan kopuk çalışanlardı: metro, makas, sıhhiye ve sivil savunma gece vardiyası.
  Kurdukları topluluk GECE HATTI'dır. Ellerindeki KATAR 07, eski bir sivil savunma sıhhiye katarıdır (seyyar hastane).
  Terk edilmiş metro ve servis tünellerinde gezer, yüzeye hiç çıkmaz.
- Oyuncu katarın HAREKET MEMURU'dur. DÜN'e hiç girmez. Ekibi ranzalara yatırır; her ekip üyesinin devresini kol
  çerçevesinde bir kolla açar. Ekibi, okuma masasına siyanotip kat planı olarak basılan algıları üstünden yönlendirir.
  Gerekirse kolu çeker (GERİ AL).
- Ekip DÜN'de bir uyuyanı "söker". Kişi kendi yatağında ayılır, Gece Hattı'nın hat ekipleri onu katarın durduğu perona
  yürüterek getirir. Bunlar AYILANLAR'dır; kapıya gelen adayların yeni karşılığıdır.
- Makineler (ARAYICILAR) katarı tünellerde arar ve hiç görünmez. Her aktarma, katarın durduğu istasyona İZ bırakır; katar
  aynı yerde fazla kalırsa yeri çözülür. Bu yüzden katar gece seferiyle istasyon değiştirir: oyunun yeni kararı budur.
- Doktrin silinmez, çiğnenir. "AĞA BAĞLANMA · ANALOG KAL" afişi yerinde kalır; altına elle "BAĞLANACAKSAN ŞALTER ELİNDE
  OLSUN" yazılmıştır. Savaşın tek yolu bağlanmaktır ve en büyük risk de odur.

Ton: Soğuk Savaş işçi soğukkanlılığı. Islak tünel, uzun vardiya, elden ele kâğıt. Sıcak lamba ışığı, soğuk sac ve beton,
kâğıt, mürekkep, pirinç, bakalit, emaye, tüp; buna sıhhiye donanımı eklenir (serum askısı, battaniye, ilaç dolabı).
Yorgun ama inatçı bir umut. Kahramanlık yoktur, ölçülü yas vardır. Kod yağmuru, kehanet ve seçilmiş kişi yoktur.

Ad sözlüğü (çalışma adları; TB bölüm 2.4)

Bu adlar metinlerde ve örnek ekranlarda kullanılır. Hepsi TMP yuvasıdır. İngilizce adlar mağaza başlığında kullanılmaz.
"DÜN" metinde hep büyük harfle ya da kesmeyle yazılır ("DÜN'e bağlanmak"). İnsanlar için "uyanmak" fiili kullanılmaz
(ayılmak, sökmek kullanılır); makineler için de kullanılmaz ("devreye girdi").

Dünya

| Kavram | TR | EN |
|---|---|---|
| Oyun | SON ŞALTER | CORRIGIBLE |
| Yapay zekâ | BAKICI | the Attendant |
| Simülasyon | DÜN | the Held Day |
| Direniş | GECE HATTI (gezici hat ekipleri) | the Late Line |
| Taşıt | KATAR 07 (çağrı kodu K-07) | TRAIN 07 |
| Oyuncu | HAREKET MEMURU | signalman |
| Bölge (12) | SEMT (her semt bir istasyona bağlı) | district |
| Sektör (36) | KAT | floor |
| Dış makineler (görünmez) | ARAYICILAR | the Seekers |
| İç savunma (bugünkü 8 makine modeli) | BEKÇİLER | the Wardens |
| DÜN'ün kuralları | TEKRAR KURALLARI | routines |
| Uyarı satırı | KATAR BEKLEMEZ | THE TRAIN DOESN'T WAIT |

Mekân ve nesneler

| Bugün | TR | EN |
|---|---|---|
| Komuta odası | KOMUTA VAGONU | command car |
| Masa (K1) | HAREKET MASASI | signal desk |
| Yeni, masanın arkasında | KOL ÇERÇEVESİ (her ekip üyesi bir kol) | lever frame |
| Teleks (daktilo) | SEFER YAZICISI | run printer |
| Harita ve baskın tahtası | OKUMA MASASI (ışıklı, siyanotip) | reading table |
| Vardiya panosu (K4) | MÜRETTEBAT PANOSU | crew board |
| Kapı (K8) | VAGON KAPISI, ardında peron | car door |
| Ayarlar dolabı (K9) | SİGORTA PANOSU | fuse panel |
| Son şalter (KLS) | CER ŞALTERİ (plakası SON ŞALTER) | traction switch (plate: LAST SWITCH) |
| Yeni | SEYİR KÖŞESİ: makas kolu, enerji göstergesi, ray dinleyicisi | route corner |
| Yeni | RANZA BÖLMESİ (sıhhiye vagonunun koğuşu) | bunk bay |
| Taşınma kolu | MAKAS KOLU: KAL / YOLA ÇIK | switch lever: STAY / DEPART |
| İFŞA göstergesi | RAY DİNLEYİCİSİ (KONUM = İFŞA + İZ) | rail listener |
| Menü sahanlığı | BAKIM DEPOSU; puantaj saati → SEFER SAATİ; vardiya kartı → SEFER KARTI | maintenance depot; run clock; run card |

Aktarma (baskın)

| Bugün | TR | EN |
|---|---|---|
| Baskın | AKTARMA | TRANSFER |
| Kişi başına bağ | DEVRE (kol çerçevesinde bir kol) | circuit |
| Giriş/çıkış kareleri | ÖLÜ NOKTA | blind spot |
| Kaçış | DÖNÜŞ | the way back |
| Geri çekil | GERİ AL (kolu çek) | RECALL |
| Çıkamayan | DÖNMEDİ | DID NOT RETURN |
| 0 can, kurtarıldı | AKTARMA ŞOKU | transfer shock |
| Teknisyen / EMP uzmanı / Analist | TEKNİSYEN / SARSICI / KILAVUZ (piyon harfleri T / S / K) | Technician / Jolter / Guide |
| EMP hücresi / rol kiti / zırh plakası | SARSINTI BOBİNİ / AKTARMA SETİ / YALITIM PLAKASI | jolt coil / transfer kit / insulation plate |
| Sivil sunucu | KOĞUŞ PANOSU | ward panel |
| Kopya dolabı | BEŞİK | Cradle |
| YZ düğümü / çekirdek | BAKICI DÜĞÜMÜ / BAKICI ÇEKİRDEĞİ | Attendant node / core |
| İkmal sandığı | DEPO ANAHTARI | depot key |
| Ozalit pafta anteti | OKUMA PLANI · K-07 | READING PLAN · K-07 |
| Güvenlik dronu | BEKÇİ DRONU (öteki makine adları aynen) | warden drone |

Sayaçlar

| Bugün | TR | EN |
|---|---|---|
| FON | ENERJİ (cer akımı) | POWER |
| ERZAK, HAMMADDE, GÜVEN | aynen | RATIONS, MATERIALS, TRUST |
| ÖZGÜR BÖLGE | SABAH n/12 (sabaha çıkan semt) | MORNING n/12 |
| YZ ALARMI | BAKICI ALARMI (0–10) | ATTENDANT ALERT |
| Karşı saldırı | SİLİP BASMA | REPRINT |
| İFŞA (sızma kaynaklı) | İFŞA (aynen) | EXPOSURE |
| Yeni: istasyonun izi | İZ (yarım puanlı) | TRACE |
| Yeni: İFŞA + İZ | KONUM n/6 | LOCATION n/6 |
| Eşikte | KONUM ÇÖZÜLDÜ | LOCATION FIXED |
| Taşınma | SEFER / ACİL SEFER | run / emergency run |
| Kademe | TEHDİT n/3 (aynen) | THREAT n/3 |

İnsanlar, olaylar, sonlar

| Bugün | TR | EN |
|---|---|---|
| Aday | AYILAN | the Roused |
| Şüphe kademesi | AYILMA KAYDI: "kayıt tam" / "yarı ayık bulundu" / "kendiliğinden ayılmış" | rousing record |
| Sızmacı | YANKILI (uykusunda DÜN'ün düzenini tekrarlayan biri) | a Sleeptalker |
| KABUL / GERİ ÇEVİR | KABUL / PERONDA BIRAK | ADMIT / LEAVE ON THE PLATFORM |
| Damga | DÜZELTİLEBİLİR (her dosyada); DÜZELTİLDİ (yalnız açığa çıkan yankılıda) | CORRIGIBLE / CORRECTED |
| Sorgu | DİNLEME TESTİ | listening test |
| KARARTMA / AĞIR YARALI / ANMA / TAKAS / ÇAĞRI / KONVOY | SESSİZ VARDİYA / AKTARMA ŞOKU / BOŞ RANZA / KARŞI KATAR / KESİK YAYIN / YÜRÜYEN KAFİLE | silent shift / transfer shock / empty bunk / passing train / broken broadcast / walking column |
| Sonlar | SABAH (zafer) · SİNYALLER SUSTU · SON YOLCU · KATAR BULUNDU · KATAR TERK EDİLDİ · SON ŞALTER (ÇEKİLME / TAHLİYE) | MORNING · THE SIGNALS WENT DARK · LAST PASSENGER · THE TRAIN WAS FOUND · TRAIN ABANDONED · LAST SWITCH (WITHDRAWAL / EVACUATION) |

Semt adları henüz yazılmadı (tek şehir ölçeği onay bekliyor). Örnek ekranlarda uydurma yer tutucular kullan ("SEMT 04 ·
TERSANE"); gerçek şehir, kıta ya da ülke adı kullanma. Yuvaları TR 18, EN 24 karaktere göre ölç.


Hukuki sınır ve özgünlük (tasarımcı için de bağlayıcı)

Oyun, "dışarıdan bir simülasyonu izleyen görevli" türüne giriyor. Tür öğeleri serbesttir; belirli bir eserin ifadesi
değildir. Bu dünyanın en bilinen örneği Warner Bros.'un The Matrix filmleridir; onun adları, görünüşü ve bilinen
sahneleri bu oyunda kullanılmaz ve onlara benzeyen görünüşlerden de kaçınılır. Türden al, eserden alma.

Serbest olanlar: dışarıdan izleyen görevli, içeri bağlanan ekip, çıkışa yetişme baskısı, simülasyondan çıkarılan insanlar,
iz süren makineler, yer değiştiren üs, uyuyan casus.

Örnek metinlerde ve plakalarda özel ad olarak kullanılmayacaklar: Matrix / Matris, Neo, Morpheus, Trinity, Tank, Dozer,
Cypher, Switch, Mouse, Kâhin, Mimar, Ajan (makine programı anlamında), Seçilmiş, Zion, Nebuchadnezzar, Sentinel, Construct,
"Operator" (oyuncunun İngilizce unvanı olarak), "jack in", "unplugged", "dive / full dive". Sıradan sözcük anlamı serbesttir:
"switch lever" ya da "LAST SWITCH" plakası sorun değildir; "Switch" adlı bir karakter sorundur. Tam liste TB bölüm 2.1'de.

Amblem: kızılhaç / kızılay amblemi ve uluslararası sivil savunma işareti (turuncu zemin üstünde mavi eşkenar üçgen)
uluslararası hukukla korunur. Katarın üstünde ikisini de kullanma. Uydurma bir "sıhhiye katarı" işareti öner (bkz. açık
sorular).

Ayrışma eksenleri (her çekirdek sahnede filmden en az bir eksen ayrışır; TB bölüm 2.3)

| Çekirdek sahne | Bu oyunda |
|---|---|
| Oyuncu | hareket memuru; demiryolu kilitleme kol çerçevesi; odada tek CRT |
| Bağlanma | ranza; yatağın altında akım bobini; başa takılan hiçbir şey yok |
| Bağlı kişinin durumu | ranza başında mekanik ampermetre ibresi (AKIM) ve lamba |
| Geri çekme | kol geri çekilir (GERİ AL), ekip sağ döner |
| Ölüm | ibre sıfıra iner, ranza lambası söner; ertesi vardiya boş ranza, katlanmış battaniye |
| Avcılar | görünmeyen Arayıcılar: ses, uzak lamba, titreşim |
| Üssün kurtuluşu | yalnız taşınma (sefer); cer şalteri kaçış değil vazgeçiştir |
| Ayılan | evinde ya da koğuşta uyuyan insan; yürüyerek, pijamalı ya da iş kıyafetli, sersem ama zayıflamamış gelir |
| Menü | bakım deposu: muayene çukuru, iş iskelesi, katarın burnu |
| Taşıt | sivil savunma sıhhiye treni; yüzeye hiç çıkmaz |
| Simülasyonun görünüşü | DÜN bir kat planıdır: hareket memuru onu görüntü olarak hiç görmez; okuma masası ekibin algısını siyanotip plana basar |

Tasarım turu için YAPMA listesi (TB bölüm 2.6'dan aynen; bölüm başvurusu TB'ye göredir)

Çıktı bu listeye karşı insan gözüyle denetlenir; bir maddeye uyan ekran onaylanmaz.

Yapma:

1. Yeşil ya da yeşil-siyah ekran; yeşil yağan kod; kod yağmuru; "ekranın içi" görüntüsü.
2. Siyah uzun deri palto, güneş gözlüğü, siyah takım elbiseli ajan.
3. Çok ekranlı konsol. Odada tek CRT vardır.
4. Bağlanma koltuğu, dişçi koltuğu, kafa soketi; başa takılan kask, kulaklık ya da elektrot; bedene giren kablo; ranzadan
   başa giden kablo demeti.
5. Telefon, ahize, telefon kulübesi, santral fiş panosu, çalan telefon.
6. Nabız monitörü, EKG çizgisi, düz çizgiye dönen nabız.
7. Sıvı dolu kapsül, insan tarlası, kazınmış baş, zayıf ve kasları erimiş kurtarılmış, sedyede taşınan ayılan, gözlerini
   ışıktan koruyan ilk uyanış anı.
8. Uçan, dokunaçlı, kırmızı göz kümeli ya da sürü hâlinde makine. Arayıcılar hiç gösterilmez.
9. Tünel sahnelerinde kırmızı nokta ışık (göz, lamba). Kırmızı yalnız plandaki makinedir.
10. Hover gemi, gemi iç yerleşimi, EMP dalgası, karartmayla iz kaybettirme görüntüsü.
11. Beyaz fayanslı boş yolcu peronu, merdivenli istasyon girişi, gelen trenin ışığı ya da sesiyle açılış (menüde). İstasyon
    ad levhası yalnız oyun içinde, kepenk aralığından görünür.
12. Kırmızı / mavi hap, beyaz tavşan, kara kedi.
13. Gaz maskesi, buhar lokomotifi, Moskova metrosu süslemesi (avize, mozaik, mermer kemer), Aurora tipi lokomotif kabini,
    istasyon bayrakları, mutant, kıyamet çölü.
14. Gece yarısı yeniden düzenlenen şehir, duran saatler kümesi.
15. Kahraman pozu, seçilmiş kişi ışığı, mesih ikonografisi.

Yap: kâğıt, siyanotip plan, kol çerçevesi, makas kolu, ampermetre, sodyum turuncusu tünel ışığı, beyaz fosforlu tek CRT,
sıhhiye katarının bakım donanımı (serum askısı, battaniye, ilaç dolabı), Rev3'ün sade, pahlı nesne dili. Bütün nesneler
tek model dilindedir; üçüncü taraf paket modeli kullanılmaz (TB bölüm 6.7).

Bu brife özgü ek kurallar:

- Ayılanların ve ekip bedenlerinin yüzü belirgin değildir; insan figürü siluet ya da battaniye altında biçimdir. Kel ya da
  kazınmış baş yoktur.
- Kurtuluş kol çekmektir, darbe değildir: son şalter kol olarak iner, ışıklar sırayla söner. Dalga, patlama, şok halkası
  çizilmez.
- Vitrin: mağazada ve ilk görüntülerde simülasyon değil kâğıt, siyanotip plan ve kol çerçevesi öne çıkar. Bölüm C'nin K1
  ekranı bu yüzden oyunun yüzüdür.


Döngü (dönüşümden sonra)

- Menü, bakım deposunda açılır (Giris). Hareket memuru sefer kartını sefer saatine basar, katarın yan kapısının çarkı döner,
  kamera eşikten içeri girer. Depoda sigorta kutusu (AYARLAR) ve SON ŞALTER (oyundan çıkış) da vardır.
- Komuta vagonu (DeskGame), hareket masası (K1): okuma masasının harita hâlinden cephe semtinin sıradaki katına aktarma emri
  verilir. Hedef semt katarın menzilinde olmalıdır: katarın semti ya da bir durak ötesi. Altı sayaç: VARDİYA, ENERJİ,
  ERZAK, HAMMADDE, SABAH, GÜVEN. Anahtar rayı: MÜRETTEBAT, KAPI, HAREKET, SEYİR.
- Aktarma: kol çerçevesinde üç kol sırayla öne alınır, ranzalarda ampermetre ibreleri kalkar, sefer yazıcısı planı basar
  ("AKTARMA · K-07 · 3 DEVRE"). Okuma masasında 8×8 siyanotip kat planı açılır (Baskin). Hedef kırılınca DÖNÜŞ başlar;
  ekip ölü noktalara yetişmeli. Yetişemeyen DÖNMEDİ olur: hem DÜN'de hem ranzasında ölür. Dönüşten önce GERİ AL ekibi sağ
  çıkarır.
- Mürettebat panosu (K4): 8 kişi kartı ceplere dağıtılır: TEKNİSYEN, SARSICI, KILAVUZ; ATÖLYE, KUMANYA, TALİM; BOŞTA. Depo,
  iş emri, tarif defteri, hurda kutusu, kişi dosyası, KAYIPLAR.
- Vagon kapısı ve peron (K8, K8A, K8R): birkaç vardiyada bir 2–3 ayılan perona getirilir; peron zili çalar. Tezgâhta
  AYILMA KAYDI kartları ve grup fişi; KABUL damgası bir karta, PERONDA BIRAK damgası fişe. Katar doluysa yollanacak kişi
  fişten seçilir. Ayılanlardan biri yankılı olabilir.
- Olaylar: kapanışta bir olay çıkarsa EVRAK tepsisine olay tutanağı gelir; karar verilmeden fiş basılmaz.
- Seyir köşesi (KS, yeni): ray dinleyicisi KONUM'u gösterir (KONUM = İFŞA + katarın durduğu istasyonun İZ'i, eşik 6).
  Makas kolu ile vardiya içinde sefer emri verilir (KAL / YOLA ÇIK, komşu istasyon, 6 enerji; KONUM ÇÖZÜLDÜ iken acil
  sefer 12). Katar vardiya kapanışında yola çıkar; sefer vardiya yemez.
- Vardiya şalteri: kol inince devir fişi sefer yazıcısından basılır; fiş EVRAK tepsisine konunca vardiya biter. Sefer emri
  varsa katar bu anda yola çıkar.
- Cer şalteri (plakası SON ŞALTER): 5. vardiyadan sonra açılır; kapaklı, iki adımlı; ÇEKİLME ya da TAHLİYE emri çıkar,
  harekât biter.
- KONUM ÇÖZÜLDÜ: iki vardiya (sabotajla bir) içinde sefer yapılmazsa katar bulunur (son: KATAR BULUNDU).
- Son raporlar: SABAH (temiz, bedelli, gölgeli), KATAR BULUNDU, KATAR TERK EDİLDİ, SİNYALLER SUSTU, SON YOLCU, SON ŞALTER
  (ÇEKİLME, TAHLİYE).


Bugünkü hâl

- Bugün oyun SIĞINAK 07'nin komuta odasında geçiyor: 4,4 × 3,9 × 2,5 m düz bir kutu oda (ekler 01–18, 30, 31). Odada iki
  model dili var: üçüncü taraf paketlerden gelen fotogerçekçi, yoğun dokulu modeller (masa, daktilo, CRT, telsiz, iki pano,
  ayarlar dolabı, kapı, kapı lambası, masa lambası, kupa, duvarlar) ile Revizyon 3'ün sade, düz renkli, pahlı modelleri.
  Revizyon 4 brifi bu iki dili birleştirmek için yazıldı; sorun listesi ekte (`BRIEF_rev4.md`, "Görülen sorunlar").
- Geliştiricinin kararı (15 Kasım): üçüncü taraf model paketleri kullanılmaz ve oyundan sökülecek. Bu turda oda tek dilde
  yeniden tasarlanır; bugün paket modeli olan her nesnenin yerine yeni bir model gelir (aşağıda "Tasarlanacak modeller").
  Rev4'ün sorun listesinin çoğu bu yüzden kabuk ve yeni modellerle birlikte çözülür. `BRIEF_rev4.md`'deki paketlere dair
  kurallar (paketi koruma, marka yazısını örtme, bütçe istisnası) bu turda geçersizdir. Kararın TB'deki yeri bölüm 1
  ("Kararlar") ve bölüm 6.7'dir.
- Menü (Giris), kapının dışındaki beyaz fayanslı, düz ve fazla aydınlık bir sahanlık (ekler 20–25, 32). Dört seçenek
  sahanlığa dağılmış, hiçbiri bir bakışta ne yaptığını söylemiyor. Bu, Revizyon 4'ün de öncelikli sorunuydu.
- Baskın sahnesi (Baskin): ozalit pafta üstünde kontrplak maket, 8×8 tahta, 12 bina kiti, 8 makine modeli, harfli ahşap
  piyonlar, kâğıt HUD (Revizyon 3 ekran görüntüleri ekte: A9_R11a/b/c, A9_escape).
- CRT fosforu yeşil (`#A6E8B8`). Yeni dünyada bu kalkıyor.

Ne değişecek

| Alan | Bugün | Revizyon 5 |
|---|---|---|
| Mekân | sabit sığınak, kutu oda | sıhhiye katarının komuta vagonu: dar, uzun, kavisli tavan, kepenkli yan duvarlar |
| Menü | kapı önü sahanlık | bakım deposu: muayene çukuru, iş iskelesi, katarın burnu ve yan kapısı |
| Simülasyon görünümü | ozalit pafta + kontrplak maket | ışıklı okuma masasında siyanotip kat planı + aynı maket dili |
| Yeni karar | — | sefer: makas kolu, enerji, ray dinleyicisi (seyir köşesi) |
| Yeni yüzey | — | kol çerçevesi, ranza bölmesi, istasyon ad levhası, kepenk |
| İFŞA şeridi | 6 hücre, arka duvarda küçük | ray dinleyicisi: KONUM 6 hücre (İFŞA sabit + İZ yarım yanan), K1'den özeti okunur |
| Son şalter | arka duvarda kapaklı kutu | cer şalteri: büyük bıçaklı kol; menüdeki SON ŞALTER ve kol çerçevesiyle aynı aile |
| Afiş | ~80 px, küçük | büyür; el yazısı ek satırla odanın simgesi olur |
| CRT | yeşil fosfor, durum satırları | beyaz fosfor (~`#D8E4E8`) yeni CRT kasasında; ekip kartları, akım ibreleri, telsiz |
| Kapı | odaya açılan basınç kapısı | yeni vagon kapısı (çarklı ya da sürgülü; sen seç), ardında peron; vuruş → peron zili |
| Modeller | iki dil: paket modelleri + Rev3 | tek dil: bütün nesneler Rev3 ya da yeni model (OBJ + manifest, bütçeli) |
| Adaylar | silahlı, asker, kopya siluetleri | pijamalı ya da iş kıyafetli, ayakta, sersem ayılanlar |
| Adlar | SIĞINAK, YZ, FON, ÖZGÜR BÖLGE... | ad sözlüğü |

Değişmeyenler: analog dil; nesne üzerine gelince 4 mm kalkar, 400 ms sonra kâğıt etiket (başlık + tek satır); geri
dönülmez işler iki adımlıdır; kâğıt ve damga dili; yazı aileleri; kurallar ve etkileşimler (yenileri TB'de).

Nesne nesne karşılık (TB bölüm 6.2; K = kalır, YA = yeniden adlandırılır, YT = yeniden tasarlanır, Y = yeni, X = kaldırılır)

"Model" sütunu bu turdaki modeli gösterir: "Rev3" serbest bir Revizyon 3 modelidir (istersen yeniden tasarla), "yeni" bu
turda çizilecek modeldir (adı "Tasarlanacak modeller" tablosunda). Bugün paket modeli olan nesnelerin hepsi "yeni"dir;
bugünkü paket modeli sökülür ve kararları en az YT'dir.

| Bugün | Model | Yeni | Karar |
|---|---|---|---|
| Masa (K1) | yeni: `SignalDesk` + `LeverFrame` | HAREKET MASASI: masa + kol çerçevesi (3 kol = ekip) | YT; model yeni |
| 6 sayaç (sayaç rafı) | Rev3 | VARDİYA, ENERJİ, ERZAK, HAMMADDE, SABAH, GÜVEN; fark fişi | YA |
| Anahtar rayı SIĞINAK / KAPI / MASA | Rev3 | MÜRETTEBAT / KAPI / HAREKET / SEYİR (bir tuş eklenir) | YA |
| Vardiya şalteri | Rev3 | aynen; plaka YA | K |
| Teleks (daktilo) | yeni: `RunPrinter` (Rev3 `Telex` ve kâğıt parçaları taban olabilir) | SEFER YAZICISI; başlık "K-07 → GECE HATTI" | YT |
| Devir fişi, EVRAK tepsisi | Rev3 | aynen | K |
| Olay tutanağı | Rev3 | biçim K, künye YA | K |
| YZ ALARMI paneli | Rev3 | BAKICI ALARMI | YA |
| İFŞA şeridi | Rev3 → yeni `RailListener` | RAY DİNLEYİCİSİ, seyir köşesinde; özeti K1'den okunur | YT |
| SON ŞALTER kutusu | Rev3 → yeni `TractionSwitch` | CER ŞALTERİ (plaka SON ŞALTER) | YT |
| Afiş | Rev3 | aynı afiş, büyük; el yazısı ek satır | YT |
| Duvar şablonu "KYGA / SIĞINAK 07" | Rev3 | "GECE HATTI · KATAR 07" + uydurma sıhhiye işareti; KYGA mührü eski boyanın altında silik | YA |
| Kurtarılan bölgeler panosu | yeni: `MorningBoard` | SABAH n/12 panosu | YT |
| Harita + ekip figürü | Rev3 → yeni `ReadingTable` | OKUMA MASASI (harita hâli): tek şehrin siyanotip hat planı, 12 istasyon, İZ işaretleri, katar işareti, ekip figürü | YT |
| Vardiya panosu (gövde + Rev3 işlevsel parçaları) | gövde yeni: `CrewBoard`; kartlar, cepler ve ekler Rev3 | MÜRETTEBAT PANOSU, vagon duvarına vidalı | YT (gövde ve yer); işlevsel parçalar K |
| CRT | yeni: `CrtCase` (Rev3 `Screen` parçası kalabilir) | beyaz fosfor | YT |
| Telsiz + güç ünitesi | yeni: `CarRadio` + `RadioMic` | TELSİZ; ahizesiz (YAPMA 5) | YT |
| CRT'ye dalış geçişi | — | aktarma geçişi: kollar iner, ibreler kalkar, yazıcı planı basar | YT |
| Ayarlar dolabı | yeni: `FusePanel` (menüdeki sigorta kutusuyla aynı model) | SİGORTA PANOSU | YT |
| Kapı + vuruş lambası | yeni: `CarDoor` + `PlatformBellLamp` | VAGON KAPISI; peron zili | YT |
| Gözetleme kapağı, buğu camı, iç interkom | Rev3 | — | X |
| Koridor, merdiven, 6 siluet | Rev3 → yeni `PlatformEdge` + `SodiumLamp` + siluet ailesi | peron kenarı; ayılan siluetleri | YT |
| Kabul tezgâhı, 2 damga | Rev3 | KABUL / PERONDA BIRAK; boş plakalar kalkar | K |
| Aday kartı, grup fişi | Rev3 | AYILMA KAYDI kartı; DÜZELTİLEBİLİR damgası | YA |
| Emir belgesi, açılış | — | SEFER EMRİ; jeneratör (ses) ve ampul kalır | YT |
| Son rapor | — | düzen K; damgalar ve cümleler yeni | YA |
| Masa lambası, kupa | yeni: `DeskLamp`, `Mug` (Rev3 `DeskLamp`, `Mug` taban olabilir) | aynı işlev | YT |
| Borular, kanal, ampul | Rev3 (`PipeRun`, `Duct`, `CeilingBulb`) | K; boru, vana, kablo artar (`CarValve`, `CableTray`) | K |
| Duvarlar, düz zemin ve tavan | — (bugünkü duvarlar paket modeli; sökülür) | kabuk olarak kullanılmaz ("sabit sığınak" gibi okunur); yerine yeni vagon kabuğu (`CarShell` modülleri) | X |


Serbestlik (bu turun en önemli kuralı)

- Üçüncü taraf model paketi kullanılmaz. Depodaki satın alınmış ya da indirilmiş modeller (bugünkü masa, daktilo, CRT,
  telsiz, panolar, ayarlar dolabı, kapı, lambalar, kupa, duvarlar) yok sayılır ve oyundan sökülecek; tasarımda hiçbirine
  dayanma, biçimlerini de taklit etme. Bütün nesneler bu turda tek ve tutarlı bir model dilinde tasarlanır.
- Revizyon 3'ten gelen her model ve tasarım kararında TAMAMEN ÖZGÜRSÜN: olduğu gibi kullan, yeniden tasarla, ölçeğini ya da
  yerini değiştir, başka bir modelle birleştir, kaldır. Malzeme, renk, yıpranma ve ışık dili de serbest. Rev3 modelleri,
  aşağıdaki tabloda yeni nesnelerin tabanı olarak da kullanılabilir.
- Her yeni model Revizyon 3'ün OBJ + manifest biçimindedir (ekteki `modeller/README.md` ve örnek manifest) ve Kısıtlar'daki
  poligon bütçesine uyar; istisna yoktur.
- Rev3 manifestlerindeki hareketli parça adlarını koru (Kısıtlar → hareketli parça adları). Sahne bileşenleri bu
  parçalara doğrudan referansla bağlıdır. Model yenilense de ad aynı kalırsa yeniden bağlama işi ve adları sınayan testler
  az değişir, sunum kodu büyük ölçüde aynı kalır. Yeni parçalar için tablodaki öneri adları kullan ya da gerekçeli başka ad
  öner.
- Vagonun ve deponun yerleşimi, ölçüsü ve kamera pozları serbest; TB'deki ölçüler başlangıç önerisidir. Değişmeyen tek şey
  aşağıdaki işlev listesi: her işlev vagonda (ya da depoda) bulunmalı ve bir pozdan erişilebilmeli.
- Aşağıdaki tablo eksiksiz değildir; gerekirse başka model öner (aynı biçim ve bütçeyle).
- Revizyon 4 tasarım çıktısı varsa ve geliştirici onu ekliyorsa, malzeme ve yıpranma kararları taşınabilir; yerleşim
  kararları taşınmaz (kabuk değişti).

Tasarlanacak modeller (TB bölüm 6.7 ve bugün paket modeli olan nesnelerin yerine gelenler)

Ad sütunundaki İngilizce adlar dosya ve manifest adı önerisidir. Bütçe üçgen sayısıdır (büyük ≤ 4 000, küçük ≤ 800;
Kısıtlar). Hareketli parça adlarından **kalın** olanlar Rev3 manifestlerinden gelir ve korunması istenir; ötekiler
öneridir. "Taban",
yeniden tasarımda başlangıç olabilecek serbest Rev3 modelidir.

Komuta vagonu (DeskGame)

| Ad | İşlev | Bütçe | Hareketli parçalar, yuvalar | Taban |
|---|---|---|---|---|
| `CarShell` modülleri: `CarSidePanel`, `CarCeilingArc`, `CarFloor`, `CarEndBulkhead` | vagon kabuğu: düz yan panel, kavisli tavan, ızgara ya da kauçuk zemin, uçta körük geçidi; perçin sırası dokuda | modül başına ≤ 4 000 (tekrar eden modül için öneri ≤ 2 000) | — | — |
| `Shutter` | zırhlı kepenk: açık / aralık / kapalı | büyük | `Shutter` slide | — |
| `StationSign` | istasyon ad levhası, kepenk aralığından görünür; seferde tünel lambası şeridi | küçük | `Sign` slide; `TXT_*` (semt adı, durak no) | — |
| `SignalDesk` | HAREKET MASASI: sayaç rafı, sefer yazıcısı, vardiya şalteri, EVRAK tepsisi taşır | büyük | — | `Desk` |
| `LeverFrame` | KOL ÇERÇEVESİ: 3 ekip kolu; boşta / devre açık / geri alındı / dönmedi | büyük | `Lever_1…3`, `Latch_1…3`, plaka swap; `TXT_*` (ad, rol harfi, ranza no) | cer şalteriyle aynı aile |
| `RunPrinter` | SEFER YAZICISI: satır basar, kâğıt kopar, devir fişini çıkarır | büyük | `Platen`, `Roll`, `Feed`, `Cover`; çapalar: kâğıt çıkışı, fiş başlangıcı ve çıkışı | `Telex` ve bugünkü kâğıt parçaları |
| `CrtCase` | tek CRT kasası, beyaz fosfor; ekip kartları, akım ibreleri, telsiz satırları | büyük | `Screen` (tüp + ekran tuvali) | `Crt` |
| `CarRadio` + `RadioMic` | TELSİZ ve güç ünitesi; bas-konuş düğmeli el mikrofonu ya da masa mikrofonu (açık soru 15), ahize yok | ikisi birlikte büyük; mikrofon küçük | Rev3 `Radio` adları: `DialPointer`, `Needle`, `KnobVolume`, `KnobTune`, `DialLight` swap; mikrofonda `PTT` (Rev3 `Microphone`) | `Radio`, `Microphone` |
| `ReadingTable` | ışıklı OKUMA MASASI: buzlu cam tabla; harita ve aktarma için aynı masa | büyük | `Glass` (emission yüzeyi) | — |
| `MorningBoard` | SABAH n/12 panosu: 12 semt kartı, sabaha çıkan semtin lambası | büyük (öneri ≤ 1 500) | `Lamp_01…12` swap | `CorkBoard` |
| `CrewBoard` | MÜRETTEBAT PANOSU gövdesi, vagon duvarına vidalı; Rev3 kartları, cepleri, depo, hurda ve iş emri parçaları üstüne oturur | büyük | `BoardLamp` swap (lambanın görünür kaynağı) | `PlanningBoard` ve ekleri |
| `RailListener` | RAY DİNLEYİCİSİ: KONUM 6 hücre (İFŞA dolu, İZ yarım), titreyen ibre, dinleme borusu, uyarı lambası | büyük (öneri ≤ 1 500) | `Needle`, `Cell_1…6` swap, `Lamp` swap | — |
| `SwitchLever` | MAKAS KOLU ve plakası: KAL / YOLA ÇIK, hedef seçici | büyük | `Latch`, `Lever`, `Selector` (döner seçici, isteğe bağlı) | cer şalteriyle aynı aile |
| `TractionGauge` | enerji göstergesi (ENERJİ'nin seyir köşesindeki ibreli karşılığı) | küçük | `Needle`, `Lamp` swap | `CabGauge` |
| `BatteryChest` | akü sandığı; enerji göstergesinin bağlı olduğu kaynak | büyük | — | — |
| `RouteBoard` | mantar panoda hat haritası; 12 istasyon, İZ raptiyeleri | büyük (öneri ≤ 1 500) | `Pin` (raptiye; örneklenir) | `CorkBoard` |
| `TractionSwitch` | CER ŞALTERİ: büyük bıçaklı kol, kapaklı, mühürlü | büyük | **`Cover`**, **`Lever`**, `Seal` swap | `LastSwitch` |
| `FusePanel` | SİGORTA PANOSU (vagon) ve SİGORTA KUTUSU (menü): tek model; kanal sürgüleri, ibreler, ses ve dil anahtarı, MENÜYE DÖN (yalnız vagonda) | kutu büyük; iç kontroller küçük | **`Leaf`**, **`Latch`**, **`Handle`**, **`Needle`**, **`Knob`** | `SettingsCabinet`, `Cab*` kontrolleri |
| `CarDoor` | VAGON KAPISI (iç yüz) ve katarın yan kapısı (dış yüz, menü); perona açılır ya da kayar | kasa, kanat ve çark birlikte büyük; ayrı modelse her biri büyük | **`Hinge`** (sürgülüyse slide parçası, adını öner), **`Wheel`** | `DoorFrame`, `DoorLeaf`, `OuterWheel` |
| `PlatformBellLamp` | peron zili lambası; ayılanlar gelince bir kez yanar | küçük | `Lamp` swap (kapalı / amber) | `KnockLamp` |
| `PlatformEdge` | kapının ardındaki peron kenarı: ıslak beton, kenar çizgisi; fayanslı istasyon duvarı yok | modül başına büyük | — | `Corridor` |
| `SodiumLamp` | sodyum turuncusu armatür: peron, tünel ve depo; kepenkteki tünel lambası dizisi de bu armatürü kullanabilir | küçük | `Lamp` swap | `CorridorLamp`, `LandingLamp` |
| `DeskLamp` | hareket masası lambası (K1'in gölgeli spotu) | küçük | `Shade` (isteğe bağlı) | `DeskLamp` |
| `Mug` | kupa; KONUM 4'te sudaki halka | küçük | `Water` (swap ya da ölçek) | `Mug` |
| `BunkBerth` | sıhhiye ranzası (3 adet, tek model); battaniye altında beden biçimi | büyük | `Blanket` swap (açık / üstünde biçim / katlanmış) | — |
| `BunkAmmeter` | ranza başında ampermetre (AKIM) ve küçük lamba | küçük | `Needle`, `Lamp` swap | `CabGauge` |
| `CurrentCoilBox` | yatak altındaki akım bobini kutusu; ranzaya kısa, kalın kablo (bedene gitmez) | küçük | — | — |
| `BayPartitionDoor` | ranza bölmesinin bölme kapısı (menteşeli ya da sürgülü; sen seç) | büyük | menteşeliyse `Hinge`, sürgülüyse slide parçası (adını öner) | — |
| `MedicineCabinet` | ilaç dolabı | büyük (öneri ≤ 1 500) | `Leaf` | — |
| `DripStand` | serum askısı | küçük | — | — |
| `FoldedBlanket` | katlanmış battaniye (BOŞ RANZA) | küçük | — | — |
| `GrabRail` | tutamak çubuğu | küçük | — | `Handrail` |
| `CarValve`, `CableTray` | vana ve kablo kanalı (boru ve kanal Rev3 `PipeRun`, `Duct` olarak kalır) | modül başına küçük | `Wheel` (vana, isteğe bağlı) | Rev3 duvar ailesi |
| Ayılan siluet ailesi | pijamalı, iş kıyafetli, yaşlı, genç; ayakta, sersem duruş; yüz belirsiz | her biri ≤ 1 500 | — | — |
| Sıhhiye katarı işareti | uydurma işaret; doku ya da kabartma, kelimesiz | doku ≤ 1024 px | — | — |

Menü: bakım deposu (Giris)

| Ad | İşlev | Bütçe | Hareketli parçalar, yuvalar | Taban |
|---|---|---|---|---|
| `InspectionPit` | muayene çukuru; çukurda iş lambası | modül başına büyük | — | — |
| `WorkGantry`, `PitSteps` | iş iskelesi ve korkuluğu; çukur basamağı | modül başına büyük | — | `Stairs` (yeniden çizilir) |
| `TrainNose` | katarın burnu ve yan gövdesi ("KATAR 07 · GECE HATTI" TMP, sıhhiye işareti) | modül başına büyük | — | — |
| `DepotFloor`, `DepotWall` | depo zemini (ıslak beton, ray) ve duvar modülü; dört seçenek bu duvarda (ya da iskelenin bir direğinde) toplanır | modül başına büyük (tekrar eden modül için öneri ≤ 2 000) | — | `WallBlock` |
| `SodiumLamp` | depo ortam ışığının armatürü; vagondakiyle aynı model | yukarıdaki gibi | yukarıdaki gibi | yukarıdaki gibi |
| `CarDoor` (dış yüz) | katarın yan kapısı; sefer kartından sonra çarkı döner | yukarıdaki gibi | **`Hinge`**, **`Wheel`** | yukarıdaki gibi |
| `FusePanel` | SİGORTA KUTUSU (AYARLAR); vagondakiyle aynı model | yukarıdaki gibi | yukarıdaki gibi | yukarıdaki gibi |

Bu tablolarda olmayan Rev3 nesneleri (sayaç rafı, anahtar rayı, vardiya şalteri, EVRAK tepsisi, kâğıtlar, kabul tezgâhı ve
damgalar, BAKICI ALARMI paneli, afiş, duvar şablonu `StencilSign`, CRT masası `CrtTable`, sefer saati, menüdeki SON
ŞALTER, dış interkom, zil, başlık) Rev3 olarak kalır ya da serbestçe yeniden tasarlanır. Okuma masasının aktarma hâli (Baskin) paket modeli kullanmıyor; bina kitleri, 8 Bekçi modeli
ve ahşap piyonlar kalır.


Sahneler

Sahne 1 — Komuta vagonu (DeskGame; bugünkü komuta odasının karşılığı)

Kabuk (TB öneri; kesin ölçüyü sen ver):

- İç genişlik ~2,9–3,2 m, yükseklik ~2,4 m, kavisli tavan (yeni `CarCeilingArc` modülü). Yan duvarlar düz panel
  ve zırhlı kepenk. Zemin ızgara ya da kauçuk kaplama. Görünen uzunluk serbest; bütün istasyonlar kameradan erişilebilir
  olmalı.
- Kepenklerden biri aralıktır. Katar dururken aralıktan istasyonun ad levhası görünür (semt adı ve durak numarası, TMP).
  Seferde tünel lambaları aralıktan kayar (sprite şerit ya da hareket eden düşük poligonlu lamba dizisi; shader yok).
- Bölümler: ön — hareket masası (kol çerçevesi) ve okuma masası; orta — mürettebat panosu, seyir köşesi, cer şalteri;
  arka — ranza bölmesi (bölme kapısıyla, yeni `BayPartitionDoor`; serum askısı, ilaç dolabı); yan — vagon kapısı ve peron
  kabul tezgâhı.
- Vagon bir hastane vagonudur, lokomotif kabini değil. İç düzen Aurora tipi bir lokomotif kabinine, hover gemi içine ya
  da çok ekranlı bir konsola benzememeli.
- Vagonun bir taşıt olduğu hissedilmeli: perçin sıraları, tutamak çubukları, sarsıntıda sallanan ampul, raylı kapı,
  zemin üstünde boji kapağı, uçta körük geçidi. Ama oda bir kutu gibi değil, bir koridor gibi akmamalı: K1'den okunması
  gerekenler bir bakışta görünür kalır.

Tasarlanacak yüzeyler:

- Hareket masası ve kol çerçevesi. Kol çerçevesi masanın arkasında demiryolu kilitleme kolları gibi durur: üç ekip kolu
  (aktarma ekibi üç kişidir: TEKNİSYEN + iki uzman), her kolun üstünde kişinin adı ve rol harfi
  (TMP), altında ranza numarası. Kollar boşta / devre açık (öne alınmış) / geri alındı / dönmedi (kilitli, plakası
  kapatılmış) hâllerini biçimle taşır. Cer şalteri ve makas koluyla aynı ailedir.
- K1'den okunacaklar: altı sayaç ve fark fişi, sefer yazıcısının son satırı, vardiya şalteri ve plakası, EVRAK tepsisi ve
  dikkat ışığı, BAKICI ALARMI, KONUM özeti (ray dinleyicisinin tekrarı ya da K1'den görünen yeri), bekleyen sefer emri
  (varsa hedef istasyon), kol çerçevesi, cer şalteri ve plakası, afiş, SABAH panosu, kepenk aralığındaki istasyon levhası.
- Okuma masası (harita hâli): tek şehrin siyanotip hat planı; 12 istasyon ve semtleri; cephe; seçili hedef semt; katarın
  işareti; menzil halkası (katarın istasyonu + komşuları); her istasyonda İZ işareti (raptiye, yarım yanan hücre ya da
  tebeşir çentiği; renk tek başına değil); her cephe semtinin katara uzaklığı ("2 DURAK"); ekip figürü. Haritada
  sabaha çıkan semt "lambası yanan istasyon", işgalli semt kırmızıdır (bugünkü "mavi = geri alınmış" anlamı siyanotip
  planla çakışır; bunu sen kesinleştir).
- Seyir köşesi (ayrıntı Sahne 4'te): makas kolu, enerji göstergesi (ibreli gösterge ve akü sandığı; yeni `TractionGauge`,
  `BatteryChest`), ray dinleyicisi, mantar panoda hat haritası ve İZ raptiyeleri.
- Ranza bölmesi: 3 ranza (yeni `BunkBerth`). Yatak altında akım bobini kutusu; ranza başında ampermetre (AKIM) ve
  küçük lamba. Başa takılan hiçbir şey yok; bobinden ranzaya kısa, kalın bir kablo gider, bedene gitmez. Aktarma sürerken
  ekibin bedenleri ranzalarda yatar (battaniye altında, yüz belirsiz). AKTARMA ŞOKU yaşayan kişi vardiyalarca yatar.
  DÖNMEDİ: ibre sıfıra iner, lamba söner; ertesi vardiya battaniye katlanmış durur (BOŞ RANZA).
- Afiş: "AĞA BAĞLANMA · ANALOG KAL" büyük, altına Kalam ile "BAĞLANACAKSAN ŞALTER ELİNDE OLSUN". Odanın simgesi gibi
  okunmalı.
- Cer şalteri: büyük bıçaklı kol, kapaklı; mühürlü (5. vardiyadan önce ve rehber sürerken), açık, ÇEKİLME / TAHLİYE
  plakası, emir kâğıdı (vazgeçilebilir). KONUM nedeniyle tahliye seçeneği de vardır.
- Mürettebat panosu (K4): yeni pano gövdesi (`CrewBoard`) vagon duvarına vidalıdır (havada durma sorunu biter); bugünkü
  kartlar, cepler ve ekler üstüne oturur; lambasının görünür bir kaynağı olur. BOŞTA cebinde kaç kişinin olduğu okunur (Rev4 sorunu).
- CRT ve telsiz (K5a): beyaz fosfor; ekranda ekip kartları, akım ibreleri, telsiz satırları; plan kâğıtta kalır, CRT'de
  plan çizilmez. CRT kasası ve telsiz yeni modeldir (`CrtCase`, `CarRadio`); üstlerinde marka ya da kelime yoktur, yazılar
  TMP yuvasıdır. Telsizde ahize yok: bas-konuş düğmeli el mikrofonu ya da masa mikrofonu (YAPMA 5).
- Vagon kapısı ve peron (K8, K8A, K8R): yeni vagon kapısı (`CarDoor`) perona açılır (kanat oyuncudan uzağa ya da kayar). Ardında peron kenarı:
  ıslak beton, sodyum ışığı, kenar çizgisi (yeni `PlatformEdge`, `SodiumLamp`). Fayanslı istasyon duvarı yok. Ayılanlar peronda ayakta bekler. Kabul tezgâhı
  kapının yanında; iki damga (KABUL, PERONDA BIRAK); katar doluyken fiş satırından kişi seçimi; makas kolu sefer emri
  verilmişse tezgâhta "YOLA ÇIKARSAN PERONDA KALIR" fişi.
- Sigorta panosu (K9): yeni sigorta panosu (`FusePanel`) vagon duvarında; kutu içeriğe göre çizilir, bugünkü 0,70 ölçekli
  sıkışık panel sorunu biter. Menüdeki sigorta kutusu aynı modeldir.

Sahne 2 — Menü: bakım deposu (Giris; ÖNCELİKLİ)

Bu turun ilk tasarım maddesidir; bugünkü menünün kullanımı zor.

- Mekân: katarın bakım deposu. Muayene çukuru, iş iskelesi, çukurun üstünde duran katarın burnu ve yan kapısı ("KATAR 07 ·
  GECE HATTI", uydurma sıhhiye işareti). Islak beton, sodyum ışığı, çukurda iş lambası. Fayanslı istasyon duvarı ve
  istasyon ad levhası menüde kullanılmaz. "Tren geliyor" ışığı ya da sesiyle açılış yapılmaz.
- Yakın açılış pozu: dört seçenek aynı kadrajda, birbirine yakın, bir bakışta okunur durur. Hepsi katarın yan kapısının
  yanındaki depo duvarında (yeni `DepotWall`; ya da iskelenin bir direğinde) toplanır:
  - YENİ OYUN: SEFER SAATİ ve kart rafı; boş sefer kartını saate basmak. Plaka: YENİ HAREKÂT / NEW CAMPAIGN.
  - DEVAM: hareket memurunun kendi sefer kartını saate basmak. Plaka: DEVAM / CONTINUE. Kart özeti
    "V.014 · SABAH 3/12 · KONUM 2".
  - AYARLAR: SİGORTA KUTUSU (vagondaki sigorta panosuyla aynı içerik: kanal sürgüleri, ses, dil; menüde MENÜYE DÖN yok).
    Plaka: AYARLAR / SETTINGS.
  - ÇIKIŞ: SON ŞALTER, bıçaklı, kapaklı, iki adım; cer şalteriyle aynı aile. Plaka: ÇIKIŞ / QUIT; şalterin kendi plakası
    "SON ŞALTER / LAST SWITCH". Bugünkü "ANA ŞALTER" tutarsızlığı biter.
  Katarın burnu, iskele ve "SON ŞALTER / CORRIGIBLE" başlığı arkada ya da kadrajın kenarında kalır.
- Plakalar okunmak için üzerine gelinmesini beklemez; TR/EN, gövde ≥ 18 px.
- Durumlar okunur:
  - kayıt yok: DEVAM kapalı; kart yuvası boş, plakada "KAYIT YOK / NO SAVE";
  - kayıt var: hareket memurunun kartı yuvada, özetiyle;
  - kayıt varken yeni harekât: iki adımlı onay (kıskaç kalkar, eski kartta "İPTAL EDİLİR / WILL BE VOIDED" fişi);
  - her seçenek için üzerinde, basılı, kapalı (gerekçesiyle) hâlleri;
  - son şalter: kapak kalkık (1. adım), kol inmiş (2. adım).
- Geçişler aynı yakın pozdan başlar ve oyuncuyu kaybettirmez: kart saate basılır → yan kapının çarkı döner → kapı açılır,
  içeriden sıcak ışık → kamera eşikten vagona girer → kararır → DeskGame'de siyahtan ampul ve hareket masası. Sigorta kutusu
  açılır ve kapanınca kamera yine yakın poza döner. Son şalter çekilince ışıklar sırayla söner (darbe yok).
- Ardından deponun geri kalanı: atmosfer, ışık, katarın burnu ve yan kapının dış yüzü, başlık, depo zili, içeri girme anı.

Sahne 3 — Okuma masası: simülasyon görünümü (Baskin; bu turda görsel yön + 3 ekran)

Ana fikir: kol çerçevesi ekibin algısını konuma çevirir; okuma masası onu kat planı olarak basar. Hareket memuru DÜN'ü
görüntü olarak hiç görmez. Bugünkü ozalit pafta gerçek siyanotipe döner: kâğıt koyu Prusya mavisi, çizgiler açık. Pafta
alttan aydınlatılan buzlu camın üstüne serilir; aydınlatma emission ile verilir, shader yok. Kod yağmuru, dijital görüntü,
"ekranın içi" yok.

Korunanlar: ortografik izometrik kamera; 3B bina kitleri (bugünkü 12 kit; tek şehirde semt binaları olarak); 8 makine modeli
(Bekçiler); harfli ahşap piyonlar (harfler T / S / K); yağlı kalem niyet okları; kırmızı makine niyet okları; A–H / 1–8
kare adları; kâğıt HUD (tur şeridi, görev çipi, üç ekip kartı, hamleyi geri alma, EMRİ İLET, TELSİZ, KURALLAR, sonuç kartı);
bina olayları. Kurallar ve sayılar aynen; `RaidEngineTests` kilitlidir.

Değişenler:

| Öğe | Yeni |
|---|---|
| Antet | OKUMA PLANI · K-07 · SEMT n · KAT n |
| Ölü noktalar (bugünkü giriş/çıkış kareleri) | açık renkli halka ve "Ö" işareti; dönüşte yanar |
| Kırma önizlemesi | hedefe vuracak hamlenin önizlemesinde "ŞİMDİ KIRARSAN DÖNÜŞE N TUR · EN UZAK KİŞİ M ADIM" |
| Geri sayım | dönüşte tur şeridinde "KATAR BEKLEMEZ · DÖNÜŞE n TUR"; son turda amber (dikkat), kırmızı değil |
| Koğuş panosu (sivil) | bakım panosu simgesi; kırılınca "KOĞUŞ PANOSU KIRILDI: bir koğuşun bakımı kesildi" |
| Beşik (kopya dolabı) | yeniden doğma noktası; "devreye girdi" işareti |
| GERİ ÇEKİL | GERİ AL (kolu çek); dönüşte de açık olabilir (bayrağa bağlı) |
| Ekip kartları | can yerine akım ibresi (AKIM) ve ranza numarası |
| Sonuç kartı | geride kalan "DÖNMEDİ"; geri alınan "GERİ ALINDI · ŞOK"; damgalar yeni |

İstenen ekranlar (1920×1080, TR + EN):

- R-a Planlama evresi: siyanotip plan, ölü noktalar, piyonlar ve makineler, kalem okları, kırma önizlemesi açık.
- R-b Dönüş: hedef kırılmış, ölü noktalar yanık, "KATAR BEKLEMEZ · DÖNÜŞE 2 TUR" (ve son tur amber hâli), GERİ AL'ın
  durumu, en uzak kişinin yolu.
- R-c Sonuç kartı: bir kişi DÖNMEDİ, bir kişi AKTARMA ŞOKU, biri sağ; ardından vagonda ranza bölmesine kısa dönüş (KR).

Ayrıca aktarma geçişi (oda → okuma masası ve geri): kollar sırayla öne alınır, ranzalarda ibreler kalkar, sefer yazıcısı
planı basar ve kamera okuma masasına iner. Dönüşte tersine; DÖNMEDİ olan kişinin kolu kilitli kalır.

Sahne 4 — Sefer anı ve Arayıcıların yaklaşması

Arayıcılar hiç gösterilmez. Yaklaşmaları yalnız seste, uzak sinyal lambalarında, titreşimde ve göstergelerde okunur.
Katarın dışı tünelde gösterilmez; her şey vagonun içinden (kepenk aralığı, göstergeler, dekor) görülür.

Seyir köşesi (KS pozu):

- Makas kolu: iki adım (mandal + kol), KAL / YOLA ÇIK. Plakada: hedef istasyon (semt adı, durak no), oradaki KONUM, enerji
  bedeli (6; acilse 12, "ACİL SEFER"), varsa "KAPIDA 3 AYILAN · YOLA ÇIKARSAN PERONDA KALIR". Hedef, katarın istasyonunun
  komşuları arasından seçilir (okuma masasında ya da plakadaki döner seçiciyle; öneri senin).
  Durumlar: emir yok; hedef seçili; emir verildi (kapanışta yola çıkar); emir değiştirildi (eski bedel iade); vazgeçildi
  (KAL); kilitli: enerji yetmez ("Sefer 6 enerji ister."), aktarma sürüyor, fiş basıldı; acil (KONUM ÇÖZÜLDÜ iken).
- Enerji göstergesi: ENERJİ sayacının bu köşedeki ibreli karşılığı; seferin ve aktarmanın bedelini gösterir.
- Ray dinleyicisi: KONUM 6 hücre. İFŞA kısmı sabit tam hücreler, İZ kısmı yarım yanabilen hücreler; iki kısım biçimle de
  ayrılır (ör. İFŞA dolu, İZ taralı). Titreyen ibre ve dinleme borusu. Altında sonraki kapanışın tahmini satırı: "DURURSAN
  +0,5 · DALARSAN +1 · YOLA ÇIKARSAN KONUM 1,5". Durumlar: sakin (0–1), uyarı 2, uyarı 4, KONUM ÇÖZÜLDÜ (n vardiya kaldı),
  uyarı kalktı.
- Mantar panoda hat haritası: 12 istasyon, her istasyonda İZ raptiyeleri; katarın istasyonu; bırakılan istasyonların
  soğuduğu (raptiyelerin vardiya vardiya azalması) okunur.

Sefer anı (vardiya kapanışı; sekans olarak tasarla, süre ve sırayla):

1. Vardiya şalteri iner, devir fişi basılır; fişte "SEFER: → SEMT 05" satırı.
2. Fren bırakılır: ampul sallanır, kepenk aralığındaki istasyon levhası geri kayar ve çıkar; tünel lambaları aralıktan
   kaymaya başlar (sodyum turuncusu).
3. Ray sesi, makasta tek darbe, ışıklarda kısa titreme. Ray dinleyicisinin ibresi düşer.
4. Katar yavaşlar; yeni istasyonun levhası aralığa kayar. Ray dinleyicisi yeni istasyonun İZ'ini gösterir.
5. KONUM ÇÖZÜLDÜ kalktıysa uyarı lambası söner ve sefer yazıcısı "KONUM KALKTI" satırı basar.

Arayıcıların yaklaşması (KONUM yükselirken; K1 ve KS'den okunur):

- KONUM 2: dinleme borusunda uzak, düzenli bir tıkırtı; ibre arada kıpırdar.
- KONUM 4: tıkırtı sıklaşır; kepenk aralığında, tünelin derinliğinde uzak bir sinyal lambası yanıp söner (kırmızı değil:
  beyaz ya da sodyum; anlamı desen ve sestedir); masada kupadaki su halka yapar.
- KONUM ÇÖZÜLDÜ: ray dinleyicisi iki yönden aynı sesi verir; tavandan toz iner; uyarı lambası (amber) ve "KONUM ÇÖZÜLDÜ ·
  2 VARDİYA" plakası; makas kolu ACİL SEFER fiyatına geçer. Kırmızı ışık yok.
- KATAR BULUNDU sonu: ses kesilir, ışıklar sırayla söner; mantar panoda ertesi seferin rotası asılı kalır. Arayıcılar
  yine gösterilmez.
- Peronda bekleyen ayılanlar varken sefer emri: kapı penceresinden peronda kalan siluetler, tezgâhta "PERONDA KALDI"
  fişi.


İşlevler (değişmez; her biri bir nesne ve bir pozla karşılanmalı)

Komuta vagonu

- Altı sayaç (VARDİYA, ENERJİ, ERZAK, HAMMADDE, SABAH n/12, GÜVEN); sayaç değişince yanında fark fişi.
- Okuma masası, harita hâli (12 istasyon ve semt, cephe, hedef semt, katar işareti, menzil halkası, istasyon İZ'leri,
  uzaklık, ekip figürü) ve harita pozu; aktarma emri formu (görev fişi: kadro TEKNİSYEN + iki uzman, aktarma seti,
  bedel 14 enerji, menzil denetimi ve gerekçe satırı "Hedef semt menzil dışında: katar en çok bir durak uzakta olmalı.").
- Anahtar rayı: MÜRETTEBAT (panoya), KAPI (kapıya), HAREKET (masaya), SEYİR (seyir köşesine) tuşları; dikkat fişi.
- Kol çerçevesi: 3 ekip kolu; aktarmada öne alınır, GERİ AL'da geri gelir, DÖNMEDİ'de kilitlenir.
- Vardiya şalteri: kapak (1. tık) + kol (2. tık); plakada durum ("VARDİYAYI BİTİR", "olay bekliyor", kilit nedeni,
  "SEFER EMRİ: kapanışta yola çıkılır").
- Sefer yazıcısı: satır basar, kâğıt kopar; devir fişini basar.
- Devir fişi: yazıcıdan çıkar, ele alınır, EVRAK tepsisine sürüklenir; yanlış yere bırakılırsa geri döner.
- EVRAK tepsisi: devir fişinin hedefi; olay tutanağının bekleme yeri (dikkat ışığı); arşiv fişi ve sefer emri belgesi.
- Olay tutanağı: elde okunur; seçenek satırına tık (1/2/3), KARAR kutusu; Esc ile tepsiye döner.
- BAKICI ALARMI (0–10; eşikte SİLİP BASMA).
- Ray dinleyicisi (KONUM 0–6, İFŞA + İZ, uyarılar, KONUM ÇÖZÜLDÜ) ve K1'deki özeti.
- Makas kolu (KAL / YOLA ÇIK; bütün durumları yukarıda), enerji göstergesi, hat haritası ve İZ raptiyeleri.
- Kepenk ve istasyon ad levhası (dururken levha; seferde tünel lambaları).
- Ranza bölmesi: 3 ranza, ampermetre ve lamba, akım bobini; aktarmada yatan bedenler; AKTARMA ŞOKU; DÖNMEDİ; BOŞ RANZA.
- CER ŞALTERİ: mühürlü, kapak + kol, plakada ÇEKİLME ya da TAHLİYE; ardından emir kâğıdı (vazgeçilebilir).
- SABAH panosu (0/12 ve semt kartları).
- Afiş ve el yazısı ek; duvar şablonu "GECE HATTI · KATAR 07".
- CRT: ekip kartları, akım ibreleri, telsiz satırları, ipuçları; telsiz ışığı.
- Mürettebat panosu (K4): kişi kartları, cepler, BOŞTA destesi, depo (hammadde, AKTARMA SETİ, SARSINTI BOBİNİ, YALITIM
  PLAKASI kartları), iş emri, tarif defteri, hurda kutusu, kişi dosyası (TECRİT, KISITLI, GÖREV YOK etiketleri), KAYIPLAR,
  MASAYA DÖN.
- Vagon kapısı: çark döner, kanat açılır; ayılanlar geldiğinde peron zili bir kez çalar (vuruş lambası).
- Kabul tezgâhı (K8A, K8R): 2–3 AYILMA KAYDI kartı (DÜZELTİLEBİLİR damgası her kartta), grup fişi, KABUL ve PERONDA BIRAK
  damgaları, tezgâh lambası; dolu katarda fiş satırından kişi seçimi; yankılı açığa çıkınca DÜZELTİLDİ damgası.
- Sigorta panosu (K9): kanal sürgüleri, ibreler, ses anahtarı, dil anahtarı, MENÜYE DÖN; kapak açılır.
- Açılış: SEFER EMRİ belgesi, jeneratör ve ampul.
- Son rapor kâğıdı (ekran üstü): damga, özet kutuları, grafik, kırılma anları, neden kutusu, YENİDEN BAŞLA.

Menü (bakım deposu)

- YENİ OYUN: boş sefer kartını sefer saatine basmak (kart rafı, boş deste, kıskaç); kayıt varken iki adımlı onay.
- DEVAM: kendi sefer kartını saate basmak (V.xxx · SABAH n/12 · KONUM n); kayıt yoksa kapalı ve nedeni görünür.
- AYARLAR: sigorta kutusu.
- ÇIKIŞ: SON ŞALTER (kapak + kol, iki adım).
- Ayrıca: yan kapının açılması ve içeri yürüme (eşikte kararma), depo zili.

Okuma masası (aktarma)

- 8×8 plan, A–H / 1–8; piyonlar T / S / K (normal, tur bitti, yaralı, düşmüş); 8 Bekçi modeli ve kırmızı niyet okları;
  kalem okları; bina nesneleri (koğuş panosu, beşik, depo anahtarı, Bakıcı düğümü, bina olayı nesneleri).
- Ölü noktalar; kırma önizlemesi; tur şeridi ve KATAR BEKLEMEZ geri sayımı; görev çipi; ekip kartları (akım, ranza no).
- Hamleyi geri alma (bugünkü "Geri al"), EMRİ İLET, TELSİZ, KURALLAR, GERİ AL (recall), sonuç kartı.


Pozlar (bugünkü değerler başlangıçtır; vagonla birlikte yeniden kurulur)

| Poz | Ne | Bugünkü kamera (odada) |
|---|---|---|
| K1 | Hareket masası, ana poz | (0; 1,52; −0,15), +z'ye, dikey 56,7°, lens kaydırması −0,324 |
| Map | Okuma masası, tepeden | (0; 2,75; 1,30) → (0; 0,76; 1,36), 30° |
| K4 | Mürettebat panosu | panonun 0,98 m önünden, 42,5° |
| K5a | CRT ve telsiz | ekranın 0,62 m önünden, 52° |
| KS (yeni) | Seyir köşesi: makas kolu, enerji, ray dinleyicisi | — |
| KR (yeni) | Ranza bölmesi: aktarma başı ve dönüşte kısa geçiş; DÖNMEDİ anı | — |
| K6 / K6a | Vardiya şalteri / vardiya anı (sefer anı burada başlar) | (0,40; 1,28; 0,42) → (0,665; 0,82; 1,08), 42° |
| K8 / K8A / K8R | Vagon kapısı / kabul tezgâhı / ayılma kaydı okuma | (0,15; 1,75; 0,55) → (2,2; 1,28; 1,0), 62°; tezgâh 47°, okuma 40° |
| K9 | Sigorta panosu | panonun 1,1 m önünden, 48° |
| KLS | Cer şalteri | (0,30; 1,50; 1,30) → (0,68; 1,20; 2,24), 40° |
| M0' (yeni) | Menü, yakın açılış pozu | — (bugünkü M0 uzak genel bakış kalkar) |
| M1 / M2 / MS / M3 | Sigorta kutusu / sefer saati / son şalter / eşik | tokens `cameras.*` |
| Aktarma | Okuma masası (Baskin) | izometrik ortografik (raid_tokens `camera`) |

Bugünkü oda nesnelerinin ölçüleri (işlev referansı; s = boyut, metre)

Masa, daktilo, masa lambası, vardiya panosu, CRT, telsiz, ayarlar dolabı ve kapı bugün paket modelidir ve sökülecek;
ölçüleri yalnız işlevin kapladığı yeri gösterir, yeni modelin biçim ya da ölçü referansı değildir.

| Nesne | s |
|---|---|
| Masa (bugün) | 2,11 × 0,96 × 0,94 (tabla 0,76) |
| Sayaç rafı | 1,60 × 0,96 × 1,88 (masayla) |
| Anahtar rayı | 0,97 × 0,08 × 0,16 |
| Vardiya şalteri | 0,33 × 0,19 × 0,34 |
| Daktilo (bugün) | 0,57 × 0,32 × 0,62 |
| Masa lambası (bugün) | 0,40 × 0,68 × 0,41 |
| EVRAK tepsisi | 0,34 × 0,07 × 0,29 |
| YZ ALARMI paneli / İFŞA şeridi | 0,50 × 0,34 × 0,13 / 0,51 × 0,37 × 0,13 |
| Son şalter kutusu | 0,30 × 0,64 × 0,23 |
| Vardiya panosu (bugün) | 0,86 × 1,77 × 0,95 |
| CRT masası / CRT (bugün) / telsiz + güç ünitesi (bugün) | 1,12 × 0,87 × 1,03 / 0,45 × 0,34 × 0,43 / 0,73 × 0,44 × 0,70 |
| Ayarlar dolabı (bugün) | 0,47 × 0,74 × 0,20 |
| Kapı (bugün) | kasa 0,26 × 1,99 × 1,20 |
| Kabul tezgâhı | 0,15 × 0,58 × 0,84 (katlanır) |

Koordinat kuralı (önerim; değiştirirsen gerekçe yaz): Unity, metre, y yukarı. Vagonda orijin zemin ortası; z vagonun uzun
ekseni (+z katarın gidiş yönü), x enine (x sağ). Depoda orijin katarın burnunun önünde, çukur kenarında. Bütün nesne
tabloları ve pozlar bu kuralla verilir.


Kısıtlar

Motor

- Unity 6, URP. 3B sahne + uGUI + TextMeshPro (World Space ve Overlay Canvas). UI Toolkit yok.
- Özel shader yok (Shader Graph dahil): URP Lit / Simple Lit / Unlit, TMP shader'ları. Parlama yalnız emission; bloom ve
  post-processing'e dayanan etki yok. Oyuncu post-processing'i ve TAA'yı kapatabilir: görünüş ikisi kapalıyken de doğru.
  Okuma masasının alttan aydınlatması, kepenkteki tünel lambaları ve CRT'nin tarama çizgisi emission ve sprite katmanıyla
  verilir.
- Üçüncü taraf model paketi kullanılmaz. Her nesne Rev3 ya da yeni OBJ + manifest modelidir.
- Yeni modeller: gerçek ölçek (metre), Y yukarı; hareketli parçalar ayrı, pivotu dönme ekseninde; Revizyon 3'ün OBJ +
  manifest biçimi (boyut, origin, parçalar: parent, pivotLocal, axis, range, rest; malzeme anahtarları; `TXT_*` TMP
  yuvaları: center, size, normal, up, capMm, role, sample, sampleEn; anchors; lights; place). Bütçe: büyük nesne ≤ 4 000,
  küçük ≤ 800, baskın parçası ≤ 400, makine ve piyon ≤ 1 500 üçgen. Bütün modeller bu bütçeye uyar; istisna yok. LOD
  grubu gerekmez. Doku ≤ 1024 px.
- Hareketli parça adları, model yenilense de Rev3 manifestlerindeki adlarla aynı kalır. Sahne bileşenleri bu parçalara
  doğrudan referansla bağlıdır; ad korunursa yeniden bağlama işi ve adları sınayan testler az değişir. Korunacak adlar:
  vardiya şalteri ve cer şalteri `Cover`, `Lever`; kapı `Hinge`, `Wheel`; sigorta panosu `Leaf`, `Latch`, `Handle`,
  `Needle`, `Knob`; sefer saati `Plunger`, boş destenin kıskacı `Clip`. Yeni
  parçalar için ad öner (ör. makas kolu `Latch` + `Lever`; kol çerçevesi `Lever_1…3`; ray dinleyicisi ve ampermetre
  `Needle`, `Lamp` swap; kepenk `Shutter` slide; istasyon levhası `Sign` slide). Sefer yazıcısının kâğıt çıkışı ve devir
  fişinin başlangıç ve çıkış noktaları manifestte çapa (anchor) olarak verilir; kod fişi bu çapalara göre hareket ettirir.
- İnce parça ekranda en az 3 px (TAA). Daha ince ayrıntı dokuya gider.
- Dokuya kelime gömme: her yazı bir TMP yuvasıdır (TR/EN değişir). Rakam şeridi, ölçek çizgisi, süs işareti dokuda olabilir.

Işık

- Bir pozda en çok 4 gölgeli ışık; gölgeyi spot taşır (nokta ışığı yalnız dolgu, gölgesiz). 2 cm altı parçalar gölge
  atmaz. Kâğıtların albedosu ≤ 0,8. Gölge çözünürlüğü 2048. Okuma masasının alttan ışığı gölgesizdir.

Okunurluk, dil ve renk

- 1920×1080'de gövde en az 18 px, ikincil en az 14 px görünür boyutta; dünya metinleri okundukları pozda ölçülür.
- Her metin Türkçe ve İngilizce; İngilizce %30'a kadar uzun, punto küçülmez, satır sarar. Türkçe karakterler her fontta.
  Yazı aileleri: Courier Prime (daktilo, başlık, sayı, etiket, damga, yazıcı, CRT), Libre Franklin (okuma, düğme), Kalam
  (yalnız el notu; afişin ek satırı dahil). Yeni font önerirsen gerekçe ver.
- Hiçbir durum yalnız renkle verilmez; kelime, damga, işaret ya da desen de taşır.
- Ayrılmış renkler: kırmızı = makine ve tehdit, yalnız plan ve harita üstünde (tünel sahnelerinde nokta ışık olarak asla);
  amber = dikkat / bekleyen iş; harfli ahşap = ekip; yeşil = "iyi", küçük kullanılır. Yeni: siyanotip mavisi = plan;
  sodyum turuncusu = tünel ve depo ortam ışığı (anlam taşımaz; amber dikkat lambasıyla karışmamalı, ton ve biçim farkını
  sen kur); beyaz fosfor (~`#D8E4E8`) = CRT. Yeşil ya da yeşil-siyah ekran yok.
- Neon, hologram, HUD çerçevesi, bilim kurgu görünümü yok.
- Klavye ipucu, ok, parlayan çerçeve, ekran balonu yok; yol gösteren nesnenin kendisidir (üzerine gelince 4 mm kalkar,
  400 ms sonra kâğıt etiket: başlık + tek satır). Geri dönülmez işler iki adımlıdır (makas kolu, cer şalteri, son şalter,
  yeni harekât).

Oyun mantığı

- Kurallar, sayılar, kayıt biçimi ve komutlar TB'dedir; tasarım onları değiştirmez. Görüntülerdeki sayılar örnektir. TB'de
  (b) işaretli sayılar ölçümle değişebilir (sefer 6, acil ×2, aktarma 14, İZ değerleri): yuvaları iki basamaklı sayıya göre
  ölç.
- Örnek değerler: VARDİYA 14, ENERJİ 38, ERZAK 22, SABAH 3/12, GÜVEN 7, BAKICI ALARMI 4/10, TEHDİT 2/3, KONUM 3,5/6
  (İFŞA 1 + İZ 2,5).


Ekler (depo yolları; geliştirici ekler)

- `Docs/SIMULASYON-DONUSUMU-TASARIMI.md` (TB, asıl kaynak; özellikle bölüm 2, 3, 5.2–5.3, 5.6, 6).
- `Docs/UI_Siginak_Rev4/ekler/*.png`, oyunun bugünkü görüntüleri: 01_K1_masa · 02_harita · 03_K4_pano ·
  04_K4_kisi_dosyasi · 05_K5a_crt · 06_K6_salter · 07_K1_fis_daktiloda · 08_K8_kapi_kapali · 09_K8_kapi_acik ·
  10_K8A_kabul_tezgahi · 11_K8R_aday_karti · 12_olay_tutanagi · 13_K9_ayarlar_kapali · 14_K9_ayarlar_acik ·
  15_KLS_son_salter · 16_ifsa_uyari_K1 · 17_son_rapor · 18_K1_masa_EN · 20_M0_sahanlik · 21_M2_saat · 22_M1_dolap_acik ·
  23_MS_son_salter · 24_kapi_acilirken · 25_M0_EN · 30_oda_ustten · 31_oda_genel_arka · 32_giris_ustten.
  Bu görüntüler oyunun bugünkü hâlini gösterir. İçlerindeki paket modelleri (masa, daktilo, CRT, telsiz, panolar, ayarlar
  dolabı, kapı, lambalar, kupa, duvarlar) sökülecek; biçim referansı değildir, yalnız nesnenin işlevini ve yerini gösterir.
- Baskın görüntüleri: `Docs/UI_Siginak_Rev3/screens_unity_rev3/A9_R11a_TR.png`, `A9_R11b_TR.png`, `A9_R11c_TR.png`,
  `A9_escape_TR.png` (ve EN kopyaları), `A9_R12_atlas.png`.
- Tokenlar: `Docs/UI_Siginak_Rev4/tokens_siginak.json` (sürüm 1.3) ve `Assets/Corrigible/Desk/Data/Sade/raid_tokens.json`
  (sürüm 1.3).
- Önceki turlar: `Docs/UI_Siginak_Rev4/BRIEF_rev4.md` (sorun listesi; paketlere dair kuralları bu turda geçersiz),
  `Docs/UI_Siginak_Rev3/tasarim/BRIEF_rev3.md`.
- Model biçimi: `Docs/UI_Siginak_Rev3/modeller/README.md` ve örnek manifest
  `Docs/UI_Siginak_Rev3/modeller/manifest/AdmissionCounter.json`.


İstenen çıktı (bölüm bölüm; her bölüm tek başına uygulanabilir)

Öncelik sırası: B (menü), sonra A ve C–E, sonra öbürleri.

| Bölüm | İçerik |
|---|---|
| A | Görsel dil: vagon ve depo malzemeleri (boyalı sac, perçin, kauçuk, ıslak beton, pirinç, bakalit, emaye, kâğıt, siyanotip, buzlu cam), yıpranma; bütün nesneler için tek ve tutarlı bir model dili (pah, düz renk ile doku dengesi, ayrıntının modelde ya da dokuda olacağı sınır, ölçek hissi); ayrılmış renklerin yeni karşılığı (kırmızı, amber, sodyum, siyanotip, beyaz fosfor, yeşil); uydurma sıhhiye katarı işareti |
| B | Menü, bakım deposu (öncelikli): yakın açılış pozu ve dört seçeneğin düzeni; her seçeneğin bütün durumları (kayıt var / yok, yeni harekât onayı, üzerinde, basılı, kapalı) ayrı ekranlarda; seçimden sonraki geçişler; ardından deponun atmosferi, ışığı, katarın burnu ve yan kapı, başlık, içeri girme anı (TR + EN) |
| C | Komuta vagonu yerleşim planı: tepeden plan ve iki kesit (boyuna, enine); her nesne için tablo (ad, model: Rev3 / yeni, karar: kalır / taşınır / ölçeklenir / yeniden tasarlanır / kaldırılır / yeni, Unity konum, dönüş, ölçek) |
| D | K1 ve okuma masası harita hâli ekranları (TR + EN): ilk vardiya ve geç harekât; K1'de okunması gerekenlerin hepsi okunur; menzil halkası ve istasyon İZ'leri |
| E | Seyir köşesi (KS): makas kolunun bütün durumları, enerji göstergesi, ray dinleyicisi (KONUM 0, 2, 4, ÇÖZÜLDÜ, kalktı), hat haritası |
| F | Ranza bölmesi (KR) ve kol çerçevesi: boş, aktarmada, AKTARMA ŞOKU, DÖNMEDİ anı, BOŞ RANZA; kolların bütün hâlleri |
| G | Masa ve duvar nesneleri: sefer yazıcısı ve devir fişinin çıkışı, vardiya şalteri, EVRAK tepsisi ve olay tutanağının bekleyişi, anahtar rayı, sayaçlar, BAKICI ALARMI, cer şalteri (mühürlü, açık, ÇEKİLME / TAHLİYE), afiş, şablon, SABAH panosu, kepenk ve istasyon levhası |
| H | Mürettebat panosu (K4) ve kişi dosyası: panonun duvardaki duruşu, lamba, kartlar, BOŞTA sayısı, depo, hurda, defter |
| I | Vagon kapısı, peron ve kabul (K8, K8A, K8R): kapının açılışı, peron kenarı, ayılan siluet ailesi, tezgâh, AYILMA KAYDI kartı, grup fişi, iki damga, dolu katar hâli, "peronda kalır" uyarısı |
| J | CRT, telsiz ve sigorta panosu (K5a, K9): yeni CRT kasası ve telsiz (ahizesiz), beyaz fosfor ekran düzeni, panonun içi |
| K | Okuma masası (Baskin): görsel yön sayfası + R-a, R-b, R-c ekranları (TR + EN) + aktarma geçişi; renkli ve gri tonlu kopya (gri tonda da ayrılmalı) |
| L | Sefer anı ve Arayıcıların yaklaşması: kare kare sekans (süre, sıra, ses); KONUM 2 / 4 / ÇÖZÜLDÜ hâllerinin K1 ve KS görüntüleri; KATAR BULUNDU anı |
| M | Kâğıtlar ailesi, tek tutarlılık geçişi: SEFER EMRİ, aktarma görev fişi, olay tutanağı, AYILMA KAYDI kartı, grup fişi, devir fişi (sefer satırıyla), son rapor (bütün yeni sonlar) |
| N | Model kütüphanesi ve model sayfaları: "Tasarlanacak modeller" tablosundaki bütün modeller, OBJ + manifest + three.js kaynağı; liste uzun olduğu için birkaç teslime bölünebilir (öneri: menü modelleri → kabuk → masa ve duvar nesneleri → ranza ve seyir köşesi); ortografik önden / yandan / üstten + perspektif, ölçüler, pivot işaretleri, malzeme anahtarları, TMP yuvaları |

Ekranlar 1920×1080, her biri TR ve EN. Her ekranın yanında kısa uygulama notu: eski → yeni; nesne listesi (ad, ölçü,
malzeme); etkileşimli öğeler ve durumları (normal, üzerinde, tutuldu, hedef üstünde, bırakıldı, geçersiz, devre dışı,
kilitli); hareket ve ses tarifi (süre ve sıra); YAPMA listesine karşı öz denetim satırı ("1–15: uyar / uymaz", uyan madde
varsa nedeni).

Ayrıca:

- Her poz için ışık tarifi (ışık, tür, konum, yön, renk, şiddet, menzil, gölge evet/hayır); 4 gölgeli ışık sınırının
  denetimi.
- Kamera pozları (konum, hedef ya da çapa, dikey açı, lens kaydırması), yukarıdaki koordinat kuralıyla.
- Tokenlar ekteki `tokens_siginak.json` ve `raid_tokens.json` biçiminde; yeni ya da değişen değerleri `_new_1_5`,
  `_changed_1_5`, `_was_1_5` listeleriyle işaretle. (1.4 numarası Revizyon 4 turuna ayrılmıştı; onun çıktısı elinde
  değilse 1.3'ün üstüne yaz.) Yeni gruplar için öneri: `rail` (makas kolu, ray dinleyicisi, istasyon levhası, kepenk),
  `bunk` (ranza bölmesi), `leverFrame`, `depot` (menü), `readingTable`.
- Öncelik listesi: her bölüm için hangi değişiklik en çok kazandırır, hangisi en ucuz (ilk beş). Maliyete yeni model
  sayısını da kat.
- Kararsız kaldığın en fazla 5 nokta için iki seçenekli küçük karşılaştırma.
- Açık sorular listesi (aşağıdakilere cevap önerisi ve senin eklediklerin).


Açık sorular (geliştiriciye; tasarım önerisiyle cevapla)

1. Taşıtın adı: "KATAR 07" Türkçede ülke adıyla aynı yazılır. Seçenekler: "TREN 07", "SIHHİYE KATARI 07". Çağrı kodu K-07
   hepsinde kalır. Tasarım adı yalnız TMP yuvasında taşısın; bir ada bağlı kalan kabartma ya da doku olmasın.
2. Ekran rengi: beyaz fosfor (öneri) mi, kehribar mı? Kehribar seçilirse ayrılmış amber (dikkat) başka bir renge taşınmalı.
3. Ölçek: DÜN tek şehir mi (öneri), dünya ölçeği mi? Okuma masasının harita hâli buna bağlı (şehir hat planı ya da dünya
   haritası).
4. Sıhhiye işareti: kızılhaç / kızılay ve uluslararası sivil savunma işareti korunur. Önerilen uydurma işaret ne olsun?
5. Dönüşte GERİ AL: bayrağa bağlı. Kapalıysa R-b'de GERİ AL "DÖNÜŞTE KAPALI" hâliyle görünür; ikisini de çiz.
6. "Geri al" çakışması: bugünkü tahtadaki hamle geri alma düğmesi "Geri al", yeni recall da "GERİ AL". Türkçede iki ayrı iş
   aynı adı taşır. Hamle geri almaya yeni ad öner (ör. "HAMLEYİ SİL" / "UNDO MOVE").
7. Sefer hedefinin seçildiği yer: okuma masasının harita hâli mi, makas kolunun plakasındaki döner seçici mi, ikisi mi?
8. GERİ AL'ın fiziksel karşılığı: aktarma sürerken oda görünmez (Baskin odanın üstüne açılır). Kol çerçevesi yalnız
   geçişte mi görünür, yoksa kâğıt HUD'da kolun bir karşılığı mı olur?
9. Uzak sinyal lambasının rengi (Sahne 4): kırmızı yasak, amber dikkat demek. Beyaz mı, sodyum mu, desenle mi ayrılır?
10. İnsan figürleri: ranzadaki bedenler ve ayılanlar için yeni düşük poligonlu siluet ailesi mi, yoksa battaniye altında
    biçim ve kapı penceresinden siluet gibi daha ucuz bir çözüm mü?
11. Katarın dışı: menüdeki burun ve yan kapı dışında katar tünelde dışarıdan hiç gösterilmeyecek (öneri). Bir dış çekim
    istenirse model ve hukuki denetim maliyeti ayrı yazılsın.
12. Doktrin satırı: afişin el yazısı eki ("BAĞLANACAKSAN ŞALTER ELİNDE OLSUN") ve zafer satırı ("BAĞLANDIK. ŞALTER
    ELİMİZDEYDİ.") geliştirici onayı bekliyor; ekranlarda bu metinle çiz.
13. Kabuk bütçesi: bütçe nesne başınadır. Kabuk modüler olduğundan bir pozda görünen kabuğun toplamı için bir üst sınır
    öner (öneri ~24 000 üçgen).
14. Revizyon 4 çıktısı: geldiyse hangi kararları taşınır? (Brif: malzeme ve yıpranma evet, yerleşim hayır.)
15. Telsiz mikrofonu: ahize yasak (YAPMA 5). Bas-konuş düğmeli el mikrofonu mu, masa mikrofonu mu? Telefon gibi
    okunmayan biçimi öner.
