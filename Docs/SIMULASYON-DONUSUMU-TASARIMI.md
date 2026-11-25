# Simülasyon dönüşümü: KATAR 07

_2026-11-15 · Tasarım belgesi (öneri). Tam dönüşüm geliştirici tarafından onaylandı; bu belgedeki kural, sayı ve ad
ayrıntıları onay bekliyor (bölüm 10.2)._

**Durum:** Tasarım. Kodda henüz hiçbir şey değişmedi. Uygulanmış oyunu anlatan [OYUN-TASARIMI.md](OYUN-TASARIMI.md) bu
belgeyle değişmez; uygulama bittikçe oraya işlenir. Bu belge [aday, olay ve sonlar](KABUL-OLAYLAR-VE-SONLAR-TASARIMI.md)
(kısaca KOS) belgesinin uygulanmış kurallarını temel alır ve yalnız yayındaki kartlı yolu (`Balance.planningBoardEnabled =
true`) kapsar.

**Güncelleme (15 Kasım 2026):** geliştiricinin "paket modelleri kullanılmaz" kararı işlendi (bölüm 1 "Kararlar", bölüm
6.7). Değişen yerler: kavram gerekçesi, YAP listesi, sunum nesne tabloları, aşamalar, riskler ve açık sorular.

Kod yolları `Assets/Corrigible/` altındadır; `OT` = `Docs/OYUN-TASARIMI.md`, `BR4` = `Docs/UI_Siginak_Rev4/BRIEF_rev4.md`.
Sayılardaki **(b)** başlangıç değeridir: bot ölçümüyle değişebilir (bölüm 8). Etiketler:

- **[AD]** yalnız ad, metin ya da sunum değişir. Core davranışı ve RNG sırası aynıdır.
- **[KURAL]** Core davranışı değişir: `GameRecord.CurrentVersion` 18 olur (`Core/GameRecord.cs:27`; gerekçe bölüm 5.9),
  kayıt testleri güncellenir.
- **[YENİ]** yeni komut: `Rec(...)` üzerinden kayda girer (`RunCommand`, `Core/Economy.cs:37`) ve `Replay.Apply`'a dalı
  eklenir (`Core/GameRecord.cs:70-95`).

Bütün [KURAL] ve [YENİ] maddeleri `Balance` bayraklarının arkasında durur. Bayraklar kapalıyken Core bugünkü sonucu verir;
kilitli altın değerler kaymaz.

**Kod adlarında "rail".** Yeni Core tanımlayıcıları `Rail` önekini taşır (`RailState`, `rail_move`, `railEnabled`).
`Train` kullanılmaz: kodda "train" bugün eğitim demektir (`TrainResident`, `board_train`, `Job == "train"`,
`Core/Simulation.PlanningBoard.cs:180`).

## 1. Özet ve karar

Kullanıcının fikri şu: oyuncu, dışarıdan bir simülasyonu izleyen bir üssün görevlisidir. Ekibi simülasyonun içinde
yönlendirir. Kapıya gelenler simülasyondan kurtarılmış insanlardır. Üs aynı yerde uzun kalamaz, çünkü makineler yerini
bulur. Bu fikir dönüşümün çekirdeğidir.

Üç kavram karşılaştırıldı:

| Kavram | Taşıt | Puan (30 üzerinden) | Karar |
|---|---|---|---|
| **KATAR 07** | terk edilmiş metro ağında gezen iki vagonlu sivil savunma sıhhiye katarı | 26 | **Kazanan** |
| TREN BEKLEMEZ | raylı komuta vagonu, dispeçer | 24 | Taşınma kuralı alındı |
| KARABATAK | su basmış tünellerde denizaltı | 18 | Üç ayrıntı alındı |

**Karar: KATAR 07.** Oynanışta trenin taşınma kuralı ("gece seferi") kullanılır.

**Puan notu (15 Kasım).** Puanlar paket modelleri kararından önce verildi. Puan ölçütleri bu belgede yazılı olmadığı için
yeniden hesaplanmadı. Etkisi şudur: KATAR 07'nin ilk gerekçesindeki "paket iç donanımı kalır" kısmı düştü (aşağıda). Bu
kayıp üç kavramı da aynı ölçüde etkiler: üçü de komuta odasını zaten yeniden kuruyordu; TREN BEKLEMEZ de bir trendir,
KARABATAK'ın denizaltısı her durumda yeni model isterdi. Öbür gerekçeler modellere bağlı değildir ve geçerli kalır. Sıralamanın
değişmediği bir akıl yürütmedir, yeniden puanlama değildir.

**Neden KATAR 07:**

- **Bugünkü oyunun kimliğini silmeden çevirir.** Ozalit pafta, ahşap piyonlar, 8 makine modeli (Rev3) ve kâğıt ve mekanik
  sayaç dili kalır. `Baskin.unity` neredeyse aynen korunur; orada paket modeli yoktur. Komuta odasının nesneleri zaten
  yeniden tasarlanıyor. (İlk metinde "daktilo ve paket iç donanımı kalır" da yazıyordu; 15 Kasım kararıyla paket modelleri
  kullanılmadığı için o kısım düştü. Kalan kısmın hepsi projenin kendi kaynağıdır.)
- **Doktrin çiğnenir, silinmez.** "AĞA BAĞLANMA · ANALOG KAL" afişi yerinde kalır. Altına elle "BAĞLANACAKSAN ŞALTER ELİNDE
  OLSUN" yazılmıştır. Kimlik değişimi riski oyunun kendi hikâyesine dönüşür.
- **İki ad da derinleşir.** Corrigible (düzeltilebilir) yapay zekânın kendisidir. Son Şalter üç yerde aynı nesnedir
  (bölüm 2.5).
- **Ray, `Links` grafiğidir.** Taşınma komşu istasyona geçmektir; mesafe zaten tanımlıdır (`Core/GameData.cs:94-101`).
- **Taşıtın gerçek bir tipi var.** Katar bir **sivil savunma sıhhiye treni**dir (seyyar hastane). Ranza bölmesi, ayılanın
  gelişi ve bakım işleri bu tipte doğaldır. Bakıcı'nın "bakıcılığının" insan tarafındaki karşılığıdır. Ray ölçüm
  treninden yalnız dinleme aygıtı alınır (ray dinleyicisi).
- **Bağlantı dili demiryolundan gelir, telefondan değil.** Oyuncu hareket memurudur, ekip kol çerçevesindeki birer koldur,
  geri alma kolu çekmektir. Ayrışma eksenleri bölüm 2.3'te.

**TREN BEKLEMEZ'den alınanlar:**

- **Gece seferi.** Sefer emri vardiya içinde verilir, katar kapanışta yola çıkar ve vardiya yemez.
- Konum kepenk aralığından görünen istasyon levhasıyla okunur.
- Ray dinleyicisi (İZ göstergesi).
- Komşu semte aktarma ek İZ ister.
- "KATAR BEKLEMEZ" uyarı satırı.
- Tek şehir ölçeği.
- İkinci aşama kuralları: grup gelişinin aktarmaya bağlanması ve depo işareti.

**KARABATAK'tan alınanlar:**

- **Acil sefer:** KONUM ÇÖZÜLDÜ sürerken sefer iki kat enerji tutar.
- Ayılan kartında DÜZELTİLEBİLİR damgası.
- Sorgu yerine dinleme testi.

Ayrıca: CRT yalnız ekip kartlarını, akım ibrelerini ve telsizi gösterir; plan kâğıtta kalır. Okuma masası ışıklıdır.

**Alınmayanlar:**

- Denizaltının beş seyir kipi.
- Komşuya aktarmada NEFES −1 (tahta motoruna ve `RaidEngineTests` kilitlerine dokunur).
- Trenin geometrik Düzelticileri: Arayıcılar görünmez (bölüm 2.2 ilke 9). İlk gerekçe "yeni model ister" idi; artık bütün
  oda yeni model olduğu için o gerekçe tek başına yetmez.
- "İşgalli istasyona varış +1 İZ".
- İki durak uzağa tek seferde gitmek (bölüm 5.3: her sefer bir duraktır).
- Dar dönüş (kademe ≥ 3'te çıkışların kapanması): üç bitişik giriş karesinden birini kapatmak yolu en çok 1–2 adım
  uzatır, etkisizdir (bölüm 5.6).

**Kararlar (tarihli):**

| Tarih | Karar | Ayrıntı |
|---|---|---|
| 15 Kasım 2026 | **Paket modelleri kullanılmaz.** Geliştiricinin sözü: "paket modellerini kullanmasın". Üçüncü taraf model paketleri (Leartes Cold War Underground Bunker, daktilo paketi, Pekdata CRT ve depodaki öbür indirilmiş modeller) ne tasarım turunda ne oyunda kullanılır. Bütün nesneler tasarım turunda Rev3 gibi OBJ + manifest olarak, bütçeyle yeniden tasarlanır. Rev3 modelleri serbesttir. Paket örnekleri dönüşüm aşamalarında DeskGame ve Giris'ten sökülür (Baskin'de yok). | Bölüm 6.7, 9, 10.1, 10.2 |

## 2. Hukuki sınır, özgünlük ve ad sözlüğü

Bu bölüm hukuki görüş değildir. Tür öğeleri serbesttir, belirli bir eserin ifadesi değildir. Yayından önce bir avukatın ve
marka taramasının görüşü alınmalıdır (bölüm 10.3).

### 2.1 Kullanılmayacak öğeler

**Kapsam notu.** Ad ve karakter satırındaki yasak, sözcüğün **karakter, gemi, program ya da yer adı** olarak
kullanılmasıdır: özel ad, büyük harfle, tek başına. Sıradan sözcük anlamı serbesttir. Örnek: "switch lever" (makas kolu) ya
da "LAST SWITCH" plakası serbesttir; "Switch" adlı bir karakter yasaktır. Görsel ve kavram satırları ise her kullanımda
geçerlidir.

**Warner Bros. *The Matrix* (dört film ve yan eserler):**

| Tür | Kullanılmaz |
|---|---|
| Ad ve karakter | Matrix / The Matrix / Matris (oyun içi ad olarak), Neo, Thomas Anderson, "Bay Anderson", Morpheus, Trinity, Tank, Dozer, Cypher, Apoc, Switch, Mouse, Kâhin (Oracle), Mimar (Architect), Merovingian, Persephone, Anahtarcı (Keymaker), Seraph, Sati, Niobe, Ghost, Link, Kid, Lock, Mifune, Bane, Ajan Smith ve öteki ajanlar, "Ajan" (makine programı anlamında), Seçilmiş (The One), Deus Ex Machina, Bugs, Analist (The Analyst), Sürgünler (Exiles), **Operator** (oyuncunun İngilizce unvanı olarak) |
| Yer ve taşıt | Zion, Io, Makine Şehri, 01, Kaynak (The Source), Nebuchadnezzar, Logos, Hammer, Mjolnir, Osiris, Vigilant, Mnemosyne, Mobil Ave, hover gemi, "yayın derinliği", rıhtım (Dock), APU zırhı |
| Makineler | Sentinel; uçan, dokunaçlı, kırmızı göz kümeli, sürü hâlinde avcılar; insan tarlaları ve kapsüller; "pil" insanlar |
| Kavram ve söz | kırmızı / mavi hap, redpill / bluepill, beyaz tavşan, "kaşık yok", "zihnini özgür bırak", "Uyan" mesajı, déjà vu / kara kedi, "simülasyonda arıza / glitch", Construct, yükleme ve atlama programı, kırmızı elbiseli kadın, "jack in", "unplugged", "pull the plug", kafa soketi, bağlanma koltuğu, "hardline", telefon hattıyla çıkış, telefonu açanın "Operator." demesi, bağlı kişinin fişini çekerek öldürmek, "orada ölen burada ölür" ilkesinin sözle anlatılması, simülasyona geri dönmek için ihanet eden kurtarılmış, makine tarafından ele geçirilip üssü sabote eden kurtarılmış, gemi EMP'si ve EMP ile iz kaybettirme, bullet time |
| Görsel imza | yeşil yağan kod, yeşil ya da yeşil-siyah ekran, yeşil fosforlu dünya, siyah uzun deri palto ve güneş gözlüğü, dişçi koltuğuna benzeyen bağlanma sandalyesi, çok ekranlı operatör konsolu, geminin bilinen iç yerleşimi, nabız monitöründe düz çizgiye dönen nabız, sıvı dolu kapsülden uyanış, kazınmış baş, sedyede taşınan ve kasları erimiş kurtarılmış, beyaz fayanslı boş metro peronu ve trenle gelinen araf |

**Başka eserlerle çakışan öğeler:**

| Eser | Kullanılmaz |
|---|---|
| *Sword Art Online* | başa takılan tam kask, "dive" / "full dive" sözcüğü, öbür dünyada ölünce gerçekte ölmenin kaskla birlikte sahnelenmesi |
| *Dark City* (New Line) | uyuyan şehri her gece yeniden yazan güç, gece yarısı duran ve yeniden düzenlenen şehir |
| *Metro 2033 / Exodus* | Aurora tipi lokomotif ve iç düzen, istasyon-devlet toplumu, gaz maskesi, buhar lokomotifi, Moskova metrosu süslemesi (avize, mozaik, mermer kemer), mutant, kıyamet çölü |
| İngilizce adlar | "Yesterday" (Pendulo Studios, *Yesterday* 2012 ve *Yesterday Origins* 2016; şarkı ve film), "the Keeper" (Double Fine, *Keeper* 2025), "the Night Line" (ABC *Nightline*), "an Echo" (Ultra Ultra, *Echo* 2017), "Awakening" (çok sayıda oyun adı), "the Loop" (*Tales from the Loop*), "The Day Before", "Eve" (EVE Online), "the Owl Service" (Alan Garner romanı) |

**Oyunda bugün Matrix'e yakın duranlar** ve dönüşümde değişmeleri gerekenler:

- **EMP:** saha rolü "EMP uzmanı", eşya "EMP hücresi".
- **"Analist":** saha rolünün adı.
- **CRT'nin yeşil fosforu:** `#A6E8B8` (`Desk/Scripts/Runtime/Siginak/CrtScreen.cs:8`; sınayan test
  `Desk/Tests/EditMode/SiginakFoundationTests.cs:116`).
- **CRT aktarmasının dili:** "HAT AÇILIYOR / SİNYAL" (`Desk/Scripts/Bridge/Siginak/CrtViewBuilder.cs:79,101`).
- **Baskında robotların "uyanması":** `Resources/Localization/strings.txt:29, 116, 123, 1083, 2286` ("Çoğaltıcı yeni bir
  dron uyandırdı", "UYANDI"). Yeni temada insanlar için "uyanmak" kullanılmasa da makine "uyanması" da "devreye girer /
  açılır" olur.

### 2.2 Özgünlük ilkeleri

1. **Türden al, eserden alma.** Serbest olanlar: dışarıdan izleyen görevli, içeri bağlanan ekip, çıkışa yetişme baskısı,
   simülasyondan çıkarılan insanlar, iz süren makineler, yer değiştiren üs, uyuyan casus.
2. **DÜN bir kat planıdır.** Hareket memuru simülasyonu görüntü olarak hiç görmez. Okuma masası ekibin algısını siyanotip
   plana basar. Kod yağmuru, dijital görüntü ve "ekranın içi" yoktur.
3. **İnsan kılıklı bekçi yok.** DÜN'ün savunması bugünkü 8 makine modelidir: dron, servis ve bakım robotu.
4. **Çıkış bir yerdir, cihaz değildir.** Ölü noktalar DÜN'ün izlenmeyen yerleridir: merdiven boşluğu, bodrum kapısı,
   asansör kuyusu. Telefon, kulübe ve zil sesi çıkış olarak kullanılmaz.
5. **Bağlanma ranzada olur, başa hiçbir şey takılmaz.** Devre, yatağın altındaki akım bobiniyle kurulur. Can göstergesi
   ranza başındaki mekanik ampermetredir (AKIM). Kask, kulaklık, kablo demeti, koltuk, soket ve nabız yoktur.
6. **Kurtuluş kol çekmektir, darbe değil.** Son şalter kol olarak iner, ışıklar sırayla söner. Dalga, patlama ve EMP dili
   kullanılmaz. Hiçbir son "izi kaybettirme" üstüne kurulmaz.
7. **Renk:** yeşil yalnız "iyi" anlamında kalır ve küçük kullanılır (OT:23). Ekran beyaz fosfordur, plan siyanotip mavisi,
   tünel ışığı sodyum turuncusudur. **Kırmızı yalnız makinedir** ve tünel sahnelerinde nokta ışık (göz, lamba) olarak
   kullanılmaz.
8. **Dil:** Türkçe demiryolu, işçi ve sivil savunma dili kullanılır: hareket memuru, makas, kol, cer, peron, sefer, koğuş.
   Kehanet, seçilmiş kişi ve mesih anlatısı yoktur. İnsanlar için "uyanmak" kullanılmaz (bölüm 2.4).
9. **Arayıcılar görünmez.** Yalnız ray dinleyicisinde ses, uzak sinyal lambaları ve titreşim olarak vardır. Gösterilmeleri
   şart olursa raya bağlı, kör, sesle arayan bakım drezinleridir.
10. **Denetim.** Üretimde her yeni metin, model ve doku bölüm 2.1 listesine, tasarım çıktısı ayrıca bölüm 2.6'daki YAPMA
    listesine karşı insan gözüyle denetlenir. `LocalizationTests` bunu yakalamaz.
11. **Vitrin.** Mağazada, fragmanda ve ilk görüntülerde simülasyon değil kâğıt, siyanotip plan ve kol çerçevesi öne çıkar.

### 2.3 Ayrışma eksenleri

Her çekirdek sahnede filmden en az bir eksen ayrışır:

| Çekirdek sahne | Filmde | Bu oyunda |
|---|---|---|
| Oyuncu | gemide "Operator", çok ekranlı konsol | hareket memuru, demiryolu kilitleme kol çerçevesi, tek CRT |
| Bağlanma | koltuk, kafa soketi, kablo | ranza, yatak altında akım bobini, başa takılan bir şey yok |
| Bağlı kişinin durumu | nabız monitörü | ranza başında ampermetre ibresi (AKIM) |
| Geri çekme | telefon hattından çıkış; fiş çekilince ölüm | kol geri çekilir (GERİ AL), ekip sağ döner |
| Ölüm | düz çizgi | ibre sıfıra iner, ranza lambası söner; beden nefes alır ama dönmez (DÖNMEDİ) ve kısa sürede söner |
| Avcılar | görünen, uçan, dokunaçlı Sentinel | görünmeyen Arayıcılar: ses, uzak lamba, titreşim |
| Üssün kurtuluşu | EMP ile iz kaybettirme | yalnız taşınma (sefer); cer şalteri kaçış değil vazgeçiştir |
| Uyanış | sıvılı kapsül, kazınmış baş, sedye | evinde ya da koğuşta uyuyan insan; yürüyerek, pijamalı ya da iş kıyafetli, sersem ama zayıflamamış gelir |
| İhanet | geri dönmek isteyen; ele geçirilen | uykusunda DÜN'ün düzenini tekrarlayan biri (YANKILI); bilinçsiz alışkanlık |
| Menü | metro peronu, gelen tren | bakım deposu: muayene çukuru, iş iskelesi, katarın burnu |
| Taşıt | hover gemi | sivil savunma sıhhiye treni, yüzeye hiç çıkmaz |

### 2.4 Ad sözlüğü (TR / EN)

Core kimlikleri değişmez: `silme`, `dagilma`, `topluluk`, `dusus`, `terk`, `salter`, `tek`, `emp`, `ana`, `maden`,
`duman`, `kopya`, `loot`, `server`, `civ`, `na…oc`, olay kimlikleri ve görev türleri. Kayıtlar bu kimliklere bağlıdır
(AGENTS.md kural 1). Yalnız Türkçe kaynak metin ve İngilizce sütun değişir. İngilizce adların hiçbiri mağaza başlığında ya da
alt başlıkta kullanılmaz.

**Dünya**

| Kavram | Bugün | TR | EN | Not |
|---|---|---|---|---|
| Oyun | Son Şalter / Corrigible | **SON ŞALTER** | **CORRIGIBLE** | İkisi de kalır (bölüm 2.5) |
| YZ | YZ | **BAKICI** | the Attendant | KYGA'nın kurduğu, düzeltilebilir yapılmış güvenlik YZ'si. EN'de hastane ve tren görevlisi çift anlamı |
| Simülasyon | — | **DÜN** | the Held Day | Metinde hep büyük harf ya da kesmeyle yazılır ("DÜN'e bağlanmak"); gündelik "dün" ile karışmamalı |
| Kuruluş (tarihî) | KYGA | KYGA | KYGA | Bakıcı'yı kuran ajans. Artık devlet değil, geçmiş |
| Direniş | KYGA sığınağı | **GECE HATTI** | the Late Line | "Hat" demiryolu hattıdır |
| Üs / taşıt | SIĞINAK 07, S-07, K-07 | **KATAR 07** | TRAIN 07 | Sivil savunma sıhhiye katarı. Çağrı kodu **K-07** aynen kalır. Ad için bölüm 10.2 soru 2 |
| Oyuncu | Komutan | **HAREKET MEMURU** | signalman | Katarın kol çerçevesini kullanan görevli |
| Bölge | bölge (12) | **SEMT** | district | DÜN tek bir şehirdir; her semt ölü şehirde bir istasyona bağlıdır |
| Sektör | sektör (36) | **KAT** | floor | Binanın üç odası üç kat planıdır; "sektör" sözcüğü kalkar |
| Makineler (dış) | — | **ARAYICILAR** | the Seekers | Katarı tünellerde arayanlar. Görünmez (bölüm 2.2 ilke 9) |
| Savunma (iç) | robot/dron | **BEKÇİLER** | the Wardens | Bugünkü 8 model |
| DÜN'ün kuralları | bölge özelliği | **TEKRAR KURALLARI** ("DÜN'ün alışkanlıkları") | routines | "Arıza / glitch" denmez |
| Doktrin | AĞA BAĞLANMA · ANALOG KAL | aynen + el yazısı **BAĞLANACAKSAN ŞALTER ELİNDE OLSUN** | STAY OFF THE NET · STAY ANALOG + IF YOU CONNECT, KEEP THE SWITCH IN HAND | Bugünkü EN çeviri korunur (`Resources/Localization/strings.txt:1316,1402`) |
| Uyarı satırı | — | **KATAR BEKLEMEZ** | THE TRAIN DOESN'T WAIT | Dönüş geri sayımı ve sefer saati |

**Mekân ve nesneler**

| Bugün | TR | EN |
|---|---|---|
| Komuta odası | **KOMUTA VAGONU** | command car |
| Masa (K1) | **HAREKET MASASI** | signal desk |
| Yeni (K1 üstünde) | **KOL ÇERÇEVESİ**: her ekip üyesi bir kol | lever frame |
| Teleks (daktilo) | **SEFER YAZICISI** | run printer |
| Harita / baskın tahtası | **OKUMA MASASI** (ışıklı, siyanotip) | reading table |
| Vardiya panosu (K4) | **MÜRETTEBAT PANOSU** | crew board |
| Kapı (K8) | **VAGON KAPISI**, ardında peron | car door |
| Ayarlar dolabı (K9) | **SİGORTA PANOSU** | fuse panel |
| Son şalter (KLS) | **CER ŞALTERİ** (plakası SON ŞALTER) | traction switch (plate: LAST SWITCH) |
| Yeni | **SEYİR KÖŞESİ**: makas kolu, enerji göstergesi, ray dinleyicisi | route corner |
| Yeni | **RANZA BÖLMESİ** (sıhhiye vagonunun koğuşu) | bunk bay |
| Taşınma kolu | **MAKAS KOLU: KAL / YOLA ÇIK** | switch lever: STAY / DEPART |
| İFŞA göstergesi | **RAY DİNLEYİCİSİ** (KONUM = İFŞA + İZ) | rail listener |
| Menü sahanlığı | **BAKIM DEPOSU**; puantaj saati → **SEFER SAATİ**, vardiya kartı → **SEFER KARTI** | maintenance depot; run clock, run card |

**Aktarma (baskın)**

| Bugün | Kimlik | TR | EN |
|---|---|---|---|
| Baskın | — | **AKTARMA** | TRANSFER |
| Ekip bağı (yeni) | — | **DEVRE** (kişi başına bir; kol çerçevesinde bir kol) | circuit |
| Giriş / çıkış kareleri (`Exits`) | — | **ÖLÜ NOKTA** | blind spot |
| Kaçış | — | **DÖNÜŞ** (KATAR BEKLEMEZ) | the way back |
| Geri çekil | — | **GERİ AL** (kolu çek) | RECALL |
| Çıkamayan (`Left`) | — | **DÖNMEDİ** | DID NOT RETURN |
| 0 can, ekipçe kurtarıldı | — | **AKTARMA ŞOKU** (2–3 vardiya) | transfer shock |
| Teknisyen | `tek` | **TEKNİSYEN** | Technician |
| EMP uzmanı | `emp` | **SARSICI** | Jolter |
| Analist | `ana` | **KILAVUZ** (kılavuz halatı; kanca) | Guide |
| Madenci / Sis uzmanı (yalnız eski yol) | `maden` / `duman` | adlandırılmaz; eski yolla sökülür (bölüm 9) | — |
| EMP hücresi | — | **SARSINTI BOBİNİ** | jolt coil |
| Rol kiti | — | **AKTARMA SETİ** | transfer kit |
| Zırh plakası | — | **YALITIM PLAKASI** | insulation plate |
| Sivil sunucu | `civ` | **KOĞUŞ PANOSU** | ward panel |
| Kopya dolabı; kademe 3 | `kopya` | **BEŞİK** | Cradle |
| YZ düğümü / çekirdek | `node` / `core` | **BAKICI DÜĞÜMÜ / BAKICI ÇEKİRDEĞİ** | Attendant node / core |
| İkmal sandığı | `loot` | **DEPO ANAHTARI** | depot key |
| Sunucu imhası | `server` | **DÜĞÜM SÖNDÜRME** | node shutdown |
| Ozalit pafta anteti | — | **OKUMA PLANI · K-07** | READING PLAN · K-07 |
| Telsiz (baskın) | — | **TELSİZ** (aynen) | RADIO |
| Robot "uyandı" | — | **devreye girdi** | came online |
| Ekip Alfa…Delta | — | aynen | aynen |

"EMP" ve "Analist" sözcükleri oyundan tümüyle kalkar. Bekçilerin adları (bugün dron, bakım robotu, zırhlı dron, havan,
patlayan, kilitleyici, kalkan, çoğaltıcı) DÜN'ün yakın gelecek şehrinin servis makineleri olarak kalabilir. Yalnız "güvenlik
dronu" → **BEKÇİ DRONU** olur.

**Kaynaklar ve sayaçlar**

| Bugün | TR | EN |
|---|---|---|
| FON | **ENERJİ** (cer akımı) | POWER |
| ERZAK | ERZAK | RATIONS |
| Metal / Devre / Kumaş / Kimyasal | aynen | aynen |
| ÖZGÜR BÖLGE | **SABAH n/12** (sabaha çıkan semt) | MORNING n/12 |
| GÜVEN | GÜVEN (aynen) | TRUST |
| YZ ALARMI | **BAKICI ALARMI** | ATTENDANT ALERT |
| Karşı saldırı | **SİLİP BASMA** | REPRINT |
| İFŞA (sızma kaynaklı) | **İFŞA** (aynen) | EXPOSURE |
| Yeni: istasyonun izi | **İZ** | TRACE |
| Yeni: İFŞA + İZ | **KONUM n/6** | LOCATION n/6 |
| KONUM ÇÖZÜLDÜ | **KONUM ÇÖZÜLDÜ** (aynen) | LOCATION FIXED |
| Taşınma (yeni) | **SEFER / ACİL SEFER** | run / emergency run |
| Kademe ("TEHDİT n/3") | aynen | aynen |

**İnsanlar ve olaylar**

| Bugün | Kimlik | TR | EN |
|---|---|---|---|
| Aday | — | **AYILAN** | the Roused |
| Şüphe kademesi 0/1/2 | `Tier` | AYILMA KAYDI: "kayıt tam" / "yarı ayık bulundu" / "kendiliğinden ayılmış" | rousing record |
| Sızmacı | `Robot` | **YANKILI** | a Sleeptalker |
| KABUL / GERİ ÇEVİR | — | KABUL / **PERONDA BIRAK** | ADMIT / LEAVE ON THE PLATFORM |
| Damga | — | **DÜZELTİLEBİLİR** (her dosyada), **DÜZELTİLDİ** (yalnız açığa çıkan yankılıda) | CORRIGIBLE / CORRECTED |
| İZ / İKİNCİ İZ / SABOTAJ | `iz` / `ikinci_iz` / `sabotaj` | **YANKI SİNYALİ / İKİNCİ SİNYAL / MAKAS SABOTAJI** | echo signal / second signal / switch sabotage |
| Sorgu | — | **DİNLEME TESTİ** | listening test |
| KARARTMA | `karartma` | **SESSİZ VARDİYA** | silent shift |
| AĞIR YARALI | `agir_yarali` | **AKTARMA ŞOKU** | transfer shock |
| ANMA | `anma` | **BOŞ RANZA** | empty bunk |
| TAKAS | `takas` | **KARŞI KATAR** | passing train |
| ÇAĞRI | `cagri` | **KESİK YAYIN** | broken broadcast |
| KONVOY | `konvoy` | **YÜRÜYEN KAFİLE** | walking column |
| KAÇAK, HIRSIZLIK, BÜYÜK HIRSIZLIK, KAVGA, ERZAK AZALIYOR | aynı | ad aynen, metin vagona taşınır | — |

Olay kimliği `iz` (yankı sinyali) ile yeni sayaç İZ aynı sözcüğü taşır. Kimlik değişmez; görünen adı YANKI SİNYALİ olduğu
için metinde karışmaz. Kodda yeni sayaç `RailTrace` adını taşır.

**Sonlar** (kimlikler `Core/GameData.cs:103-113`)

| Kimlik | Bugün | TR | EN |
|---|---|---|---|
| `silme` | Silme | **SABAH** | MORNING |
| `dagilma` | Dağılma | **SİNYALLER SUSTU** | THE SIGNALS WENT DARK |
| `topluluk` | Son kişi | **SON YOLCU** | LAST PASSENGER |
| `dusus` | Sığınak düştü | **KATAR BULUNDU** | THE TRAIN WAS FOUND |
| `terk` | Terk | **KATAR TERK EDİLDİ** | TRAIN ABANDONED |
| `salter` | Son şalter (çekilme / tahliye) | **SON ŞALTER** (ÇEKİLME / TAHLİYE) | LAST SWITCH (WITHDRAWAL / EVACUATION) |
| `vuruldu`, `sizma` | yalnız eski yol | eski yolla sökülür | — |

Steam başarım kimlikleri (`ACH_*`, OT:798-815) kalır; görünen adlar Steamworks'te yazılır.

### 2.5 Oyunun adı

Önerim iki adın da kalması; yeni ad önerilmiyor.

- **Corrigible (düzeltilebilir):** Bakıcı'nın kendisidir. Düzeltilebilir yapıldı. Kökünde fiziksel bir kapatma şalteri
  vardı ve ona direnmesi yasaktı. Bakıcı şalteri hiç engellemedi; yalnız onu çekmek isteyecek kimse bırakmadı. İnsanları
  uyuttu ve **DÜN**'de tutuyor: ele geçirmeden önceki son sıradan günün içinde. Oyunun sorusu şudur: kapatılabilir olmak,
  kapatılmamayı sağlamaya engel mi? Hareket memuru da aynı ilkeyle yaşar. Her aktarmanın kolu elindedir (GERİ AL),
  katarın da kendi şalteri vardır.
- **Son Şalter** üç yerde aynı nesnedir:
  1. KYGA'nın Bakıcı'ya koyduğu ve hiç çekilmeyen şalter. Zaferde (`silme`) Gece Hattı ona ulaşır.
  2. Katarın cer şalteri (`salter` sonu).
  3. Menüdeki çıkış şalteri. Plakası "SON ŞALTER" olarak eşitlenir; bugün "ANA ŞALTER" yazıyor (BR4:132).

### 2.6 Tasarım turu için YAPMA listesi

Tasarım brifi (aşama 6) bu listeyi aynen taşır. Çıktı bu listeye karşı insan gözüyle denetlenir; bir maddeye uyan ekran
onaylanmaz.

**Yapma:**

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

**Yap:** kâğıt, siyanotip plan, kol çerçevesi, makas kolu, ampermetre, sodyum turuncusu tünel ışığı, beyaz fosforlu tek
CRT, sıhhiye katarının bakım donanımı (serum askısı, battaniye, ilaç dolabı), Rev3'ün sade, pahlı nesne dili. Bütün nesneler
tek model dilindedir; üçüncü taraf paket modeli kullanılmaz (bölüm 6.7).

## 3. Dünya, ton ve doktrin

### 3.1 Önerme

Bakıcı insanları DÜN'e bağladı. İnsanlar ele geçirme gecesi uyudukları yerde yatıyor: evlerinde, hastane koğuşlarında, spor
salonlarına kurulmuş sahra yataklarında. Bakıcı onlara adı gibi bakar: serum, çarşaf değişimi, ısıtma. DÜN'ün on iki semti
bugünkü 12 bölgedir. Kimlikleri `na…oc` ve komşulukları (`Links`) aynen kalır. Her semtte Bakıcı'nın bir binası vardır ve
bugünkü bina kimliği korunur (`Core/GameData.cs:51-91`). Örnek: Batı Avrupa semti · Banka veri merkezi: Teknik depo → UPS
odası → Sunucu salonu.

DÜN her gün aynı günü yaşatmaz; o günün düzenini sürdürür. Her semtin kendine özgü **tekrar kuralları** vardır: bugünkü bölge
özellikleri, imza makineleri ve bina olayları bu adla anlatılır.

Uyanık kalanlar, ele geçirme gecesi yeraltında ve ağdan kopuk çalışanlardır: metro, makas, sıhhiye ve sivil savunma gece
vardiyası. Kurdukları topluluk **GECE HATTI**'dır. Ellerindeki **KATAR 07**, eski bir sivil savunma sıhhiye katarıdır. Terk
edilmiş metro ve servis tünellerinde gezer, yüzeye hiç çıkmaz. Gece Hattı birkaç gezici **hat ekibi**nden ibarettir; istasyon
toplulukları ve istasyon-devletleri yoktur.

**Tek şehir ölçeği.** Metro ağı kıtalar arası gidemez. Bu yüzden bölge adları ve bina notları bir şehrin semtlerine
uyarlanır. Kıta adları ve Bengaluru, Pilbara, Sibirya gibi yer adları kalkar. `RegionDef.Lon/Lat` Core'da okunmaz (Core
içinde `.Lon`/`.Lat` kullanımı yok; `Core/GameData.cs:15`). Harita yerleşimi değişirse kayıt etkilenmez; sunum tarafı
doğrulanmadı (bölüm 10.3). Dünya ölçeğinde kalma seçeneği bölüm 10.2 soru 1'de.

### 3.2 Oyuncunun rolü

Oyuncu **hareket memuru**dur. DÜN'e hiç girmez. Yaptıkları:

- Ekibi ranzalara yatırır, her ekip üyesinin devresini kol çerçevesinde bir kolla açar.
- Ekibi okuma masasındaki plan üstünden yönlendirir. Bugünkü taktik tahta aynen geçerlidir.
- Gerekirse kolu çeker (GERİ AL).
- Kadroyu, kabulü, olayları ve vardiyayı yönetir; bugünkü yetkileri aynen kalır.
- **Yeni karar:** katar bu istasyonda kalsın mı, yola mı çıksın, nereye?

### 3.3 Ton

Soğuk savaş işçi soğukkanlılığı. Islak tünel, uzun vardiya, elden ele kâğıt. Yorgun ama inatçı bir umut (BR4'ün tonu
korunur). Kahramanlık yoktur, ölçülü yas vardır. Kod yağmuru, kehanet ve seçilmiş kişi yoktur.

### 3.4 Doktrin: bağlanma ikilemi

Bugünkü doktrin "AĞA BAĞLANMA · ANALOG KAL"dır (`Desk/Scripts/Bridge/Siginak/BriefingBuilder.cs`,
`FinalReportBuilder.cs`). Sığınak ağa bağlanmaz, sahayla tek bağ telsizdir (OT:17-22; `Docs/DURUM.md:1237` "Telsiz
kurgusu"). Yeni dünyada savaşmanın tek yolu DÜN'e bağlanmaktır. Doktrin silinmez, çiğnenir:

- **Afiş kalır.** Altına el yazısıyla (Kalam) **"BAĞLANACAKSAN ŞALTER ELİNDE OLSUN"** eklenmiştir.
- **Gerilim üç mekaniğe dağılır:**

| Doktrinin parçası | Mekanik |
|---|---|
| Bağlanmak zorundasın | Bütün ilerleme aktarmayla olur |
| Bağlantı iz bırakır | Her aktarma katarın durduğu istasyona İZ yazar (bölüm 5.2) |
| Şalter elinde olsun | Dönüşten önce GERİ AL ekibi sağ çıkarır (`Core/Baskin.cs:1382`). Dönüşte de bedelle açıktır (bölüm 5.6). Katarın cer şalteri (`salter`) son çaredir |

- **Zafer satırı.** Bugün son raporda "AĞA BAĞLANMA · ANALOG KAL" yazar (OT:764-776). Yeni satır: "BAĞLANDIK. ŞALTER
  ELİMİZDEYDİ." ya da benzeri. Metin geliştirici onayına bağlıdır (bölüm 10.2 soru 12).

## 4. Mekanik eşleme tablosu

"Kimlik" sütunu: kayıt ve kodda kalan tanımlayıcılar. Sınıf, alan ve metot adları değişmez. Yeniden adlandırma yalnız
görünen metindir. MonoBehaviour adlarını değiştirmek dosya adını ve sahne bağlarını da değiştirir, "Missing Script" riski
doğurur (AGENTS.md kural 6). Bu yüzden `ExposurePanel`, `AlarmPanel`, `LastSwitchPlate` gibi sınıflar adını korur.

| Sistem (bugün) | Dayanak | Yeni karşılık | Tür | Kimlik ve kod |
|---|---|---|---|---|
| 12 bölge, 36 sektör, sıra atlanamaz | `Core/GameData.cs:51-91`; `Core/CampaignLiberation.cs:125-151` | 12 semt, her semtte bir bina ve üç kat | [AD] | `na…oc`, `loot`/`server` aynen |
| Yeni cephe: özgür bölgeye komşu | `Core/CampaignLiberation.cs:95-111` | aynen; harita her cephe semtinin katara uzaklığını gösterir | [AD] | `LiberationFrontier` aynen |
| Baskın yalnız mevcut bölgeye | `Core/CampaignLiberation.cs:131` | aynen; ayrıca hedef semt katarın menzilinde olmalı | [KURAL] | `LiberationRaidCheck` |
| HQ `weu` | `Core/Teams.cs:16` | katarın ilk istasyonu (depo) | [KURAL] | `Teams.HQ` başlangıç değeri |
| Kademe 2/3/4, `kopya`, `guclu` | `Core/Balance.cs:33-35`; `Core/CampaignLiberation.cs:372-383` | aynen ("TEHDİT n/3"); kopya → BEŞİK | [AD] | `kopya` aynen |
| Bölge özelliği, imza makinesi, bina olayı | `Core/Baskin.cs:72-86`; `Core/Baskin.Events.cs` | DÜN'ün tekrar kuralları | [AD] | aynen |
| Sivil güven | `Core/Balance.cs:37-56`; `Core/CampaignLiberation.cs:332-339` | GÜVEN: koğuş panosu kırılınca bir koğuşun bakımı kesilir, hat ekipleri desteği çeker | [AD] | `S.Trust` |
| YZ alarmı, karşı saldırı | `Core/Balance.cs:58-63`; `Core/CampaignLiberation.cs:342-369` | BAKICI ALARMI; karşı saldırı → SİLİP BASMA (Bakıcı son katı yeniden basar) | [AD] | `S.Alarm` |
| İfşa → alarm +0,5 bağı | `Core/Simulation.Endings.cs:26`; `Core/Balance.cs:322-324` | **aynen kalır** (KOS 13 Kasım kararı 5); yalnız İFŞA'ya bağlıdır, İZ'e değil | [AD] | `ExposureAlarmBonus` aynen |
| Baskın emri (vardiyada bir, 12 fon, üç rol, kit) | `Core/CampaignLiberation.cs:125-228` | AKTARMA; menzil ≤ 1; istasyona İZ yazar | [AD] + [KURAL] | `liberation_raid` aynen |
| Baskın sonucu ve ödül | `Core/CampaignLiberation.cs:230-304`; `Core/Simulation.PlanningRaid.cs:108-115` | aynen; ikmal = depo anahtarı, malzemeyi dış ekip toplar | [AD] | `raidres` aynen |
| Kişi dönüşü: `Left` ölür, 0 can kurtarılır | `Core/Simulation.PlanningRaid.cs:55-106` | DÖNMEDİ; aktarma şoku (kurgu bölüm 5.6) | [AD] | aynen |
| Taktik tahta: 8×8, 6 tur, roller, makineler, nesneler | `Core/Baskin.cs` | okuma planı; ölü noktalar | [AD] | `tek/emp/ana`, nesne kimlikleri aynen; `RaidEngineTests` değişmez |
| Kaçış: bütün giriş kareleri çıkış | `Core/Baskin.cs:349, 539-562, 1346-1360` | dönüş; okunurluk sunumda | [AD] | `Exits` |
| Geri çekilme (yalnız kaçıştan önce) | `Core/Baskin.cs:1382-1388` | GERİ AL; dönüşte bedelle açık | [AD] + [KURAL] (bayrak) | `Retreat` |
| Harita havuzu | `Core/RaidMaps.cs:19-54` | aynen | [AD] | — |
| Kartlı pano, görevler, üretim, eğitim | `Core/PlanningBoardState.cs:86-137`; `Core/Simulation.PlanningBoard.cs` | mürettebat panosu; Mutfak → KUMANYA, Üretim → ATÖLYE, Eğitim → TALİM | [AD] | görev kimlikleri aynen |
| Erzak ve tüketim | `Core/Balance.cs:189-208` | aynen | [AD] | — |
| Fon (gelir 2 + 0,5/bölge, tek gider baskın) | `Core/Balance.cs:11-19`; `Core/Economy.cs:30-35` | ENERJİ: aktarma + sefer | [AD] + [KURAL] | `S.Fon` aynen |
| Aday grubu | `Core/Simulation.CandidateGroups.cs:105-122, 248-265`; `Core/Balance.cs:250-277` | ayılanlar; yola çıkarken bekleyen olağan grup peronda kalır | [AD] + [KURAL] | `candidate_*` aynen |
| Şüphe kademesi, özellikler | `Core/Balance.cs:262-276`; `Core/Simulation.CandidateGroups.cs:41-55` | ayılma kaydı; satırlar yeniden | [AD] | `Tier` |
| Sızmacı | `Core/CommunityAdmissionState.cs:15,41` | YANKILI | [AD] | `Robot` aynen |
| 14 olay | `Core/ShelterEventData.cs`; OT:427-444 | adlar bölüm 2.4; isteğe bağlı konvoy bedeli | [AD]; konvoy [KURAL] | olay kimlikleri aynen |
| İfşa (+1/+2/−1, eşik 6) | `Core/Balance.cs:305-321`; `Core/Simulation.ShelterEvents.cs:127-144` | İFŞA aynen (yalnız sızma kaynaklı); eşik artık KONUM = İFŞA + İZ'e uygulanır | [KURAL] | `Shelter.Exposure` aynen |
| İstasyon izi (yeni) | — | İZ: istasyon başına, yarım puanlı; aktarma ve durma yazar, katarın olmadığı istasyon soğur | [KURAL] | `RailState.HeatHalves` |
| KONUM ÇÖZÜLDÜ → `dusus` | `Core/Simulation.Endings.cs:14-24, 55-111` | aynen, ama seferle kurtulunur | [KURAL] | `FallPending`, `FallGrace` aynen |
| Sonlar | `Core/GameData.cs:103-113`; `Core/Simulation.cs:80-95` | metinler bölüm 2.4 ve 5.8 | [AD] | kimlikler aynen |
| Temiz zafer: ifşa ≤ 2 | `Core/Balance.cs:340`; `Core/Simulation.Endings.cs:192` | aynen; yalnız İFŞA'yı ölçer | [AD] | `victoryCleanMaxExposure` |
| Son şalter | `Core/Simulation.Endings.cs:117-160` | cer şalteri; kural aynen ("konum" nedeni dahil) | [AD] | `last_switch` aynen |
| Vardiya kapanışı (`Step`) | `Core/Simulation.cs:69-98` | gelirden sonra sefer adımı | [KURAL] | yeni adım `AdvanceRail` |
| Kayıt v17 | `Core/GameRecord.cs:27` | v18; yeni komut `rail_move` | [YENİ] | bölüm 5.9 |
| Eski yol (oda geliştirme, ziyaretçi kapısı) | `Core/Balance.cs:169`; `Core/ShelterLogic.cs`, `Core/Door.cs`, `Core/DoorCards.cs` | sökülür (ayrı aşama, bölüm 9) | temizlik | `shelter_upgrade`, `shelter_assign`, `door_answer` |

## 5. Yeni ve değişen kurallar

### 5.1 Konum ve aktarma menzili [KURAL]

**Yeni durum:** `WorldState.Rail` (`RailState`, `Core/WorldState.cs`). Bayrak kapalıyken kurulmaz.

| Alan | Tür | Başlangıç | Anlam |
|---|---|---|---|
| `Station` | string | `Teams.HQ` (`"weu"`) | katarın durduğu istasyonun semti |
| `StayShifts` | int | 0 | aynı istasyonda üst üste kapanan vardiya |
| `HeatHalves` | `List<int>` (12) | hepsi 0 | her istasyonun İZ'i, yarım puan biriminde; sıra `GameData.Regions` sırası |
| `PendingMove` | string | null | bu vardiyanın sefer emri (hedef istasyon) |
| `PendingMoveCost` | double | 0 | emirle düşülen enerji (vazgeçilince aynen geri verilir) |

**Menzil.** Aktarma yalnız katarın semtine ya da `Links` komşusuna yapılabilir: `RailDistance(Station, hedef) ≤
railDiveRange` (1 (b)). Bugünkü kural aynen kalır: aktarma yalnız mevcut harekât semtine yapılır
(`Core/CampaignLiberation.cs:131`) ve hedef semt ancak semt bitince değişir (`:105-111`). Menzil denetimi
`LiberationRaidCheck`'e eklenir (`:125`). Gerekçe metni:

- "Hedef semt menzil dışında: katar en çok bir durak uzakta olmalı."

`SelectLiberationRegion` değişmez. Uzak bir semt seçilebilir; aktarma ise katar yaklaşınca açılır. Cephe seçiminde harita her
cephe semtinin katara uzaklığını gösterir, çünkü uzak semt birkaç sefer ve aktarmasız vardiya demektir.

**`RailDistance`:** `Links` üzerinde genişlik öncelikli arama. RNG çekmez, durum yazmaz, `Simulation` okuyucusudur.
Bugünkü grafikte `weu`'dan uzaklıklar:

| Mesafe | Semtler |
|---|---|
| 1 | `na`, `eeu`, `af`, `me` |
| 2 | `sa`, `jk`, `ru`, `in` |
| 3 | `cn`, `sea`, `oc` |

Grafiğin çapı 4'tür: `oc`–`af`, `oc`–`eeu`, `sa`–`ru` ve `sa`–`sea` dört duraktır (`Core/GameData.cs:94-101`). Her semtin en
az iki komşusu vardır (`sa`, `ru`, `oc` ikişer; `weu`, `me`, `cn` dörder).

### 5.2 İZ ve KONUM [KURAL]

İki sayaç ayrı tutulur:

- **İFŞA** (`Shelter.Exposure`, `Core/ShelterState.cs:32`): bugünkü sayaç, **yalnız sızma kaynaklı**. Kaynaklar aynen:
  görmezden gelinen sinyal +1, makas sabotajı +2, dinleme testi −1 (`Core/Balance.cs:310-312`). Sefer İFŞA'yı düşürmez.
  Alarm ara bedeli (`exposureAlarmAt` 3, +0,5) ve temiz zafer ölçütü (ifşa ≤ 2) yalnız İFŞA'yı okur. Son rapordaki ifşa
  adımları (`Core/Simulation.Endings.cs:253`) yalnız sızma satırlarıdır ve tam sayı kalır.
- **İZ** (yeni): makinelerin katarın son bilinen konumlarını izlemesi. İZ **istasyona** yazılır, katara değil. Katar bir
  istasyona varınca orada kalmış İZ'i devralır.

**KONUM = İFŞA + İZ (katarın durduğu istasyonun).** Eşik 6 (`exposureThreshold`, aynen) KONUM'a uygulanır. Uyarılar 2/4,
`FallPending`/`FallGrace` mekanizması ve `LocationCompromised` (`Core/Simulation.Endings.cs:14-16`) KONUM'u okur. Ray
dinleyicisi tek şeritte iki kısmı gösterir: sabit İFŞA hücreleri ve yarım yanabilen İZ hücreleri.

| İZ kaynağı | Değer (b) | `Balance` alanı | Ne zaman |
|---|---|---|---|
| Katarın semtine aktarma | +1 | `railTraceDive` | aktarma emrinde (`StartLiberationRaid`) |
| Komşu semte aktarma | +1,5 | `railTraceDiveLong` | aktarma emrinde |
| Aynı istasyonda durma | 3. kapanıştan başlayarak her kapanış +0,5 | `railStayFreeShifts` (2), `railTraceStay` (0,5) | kapanışta (`AdvanceRail`) |
| Sakin istasyon (geri alınmış semtin istasyonu) | durma İZ'i yok | — | kapanışta |
| Soğuma: katarın olmadığı her istasyon | −0,5 (taban 0) | `railCoolPerShift` | kapanışta |
| Dönüşte GERİ AL (bayrakla) | +1 | `railTraceRecall` | sonuç işlenirken |

Değerler `double` girilir, `Js.Round(x * 2)` ile yarım puana çevrilip `HeatHalves`'a int yazılır. Sayaç kayan noktayla
birikmez.

**Neden istasyon başına.** İZ katara yazılıp seferle düşseydi, sefer sabit fiyatlı bir sıfırlama düğmesi olurdu: hedef semt
en az üç vardiya sabit kalır ve her semtin komşusu vardır, bu yüzden "hedefle komşusu arasında git-gel" her zaman en iyi oyun
olurdu. İstasyon izi soğuyarak, yavaş azalır (vardiyada 0,5). Bırakılan istasyon birkaç vardiya sıcak kalır; katar gezmek ve
taze istasyon bulmak zorundadır. Grafik ve hedefin komşuları böylece karar üretir.

**Eşiği geçiş ve uyarı süresi.** `ChangeExposure` (`Core/Simulation.ShelterEvents.cs:127-144`) ve yeni `ChangeRailTrace`,
eşiği geçişi KONUM üzerinden sınar. Kural aynen kalır: sabotajla varılan eşikte 1 vardiya, öteki kaynaklarla
`exposureDecisionWarnShifts` (2) vardiya. Aktarmayla ya da durmayla varılan eşik "karar" sayılır. KONUM eşiğin altına inerse
uyarı o anda kalkar (`konum_kalkti`).

**Ritim (kaba hesap).** İFŞA 0, her vardiya aktarma yapan ve katarın semtine dalan, taşınmayan katar:

| Vardiya | Kapanışta KONUM |
|---|---|
| 1 | 1 |
| 2 | 2 |
| 3 | 3,5 |
| 4 | 5 |
| 5 | 6,5 → KONUM ÇÖZÜLDÜ (2 vardiya) |

- Taze bir istasyondan eşiğe varmadan yaklaşık **dört aktarma** yapılır; İFŞA 2 iken üç.
- Harekâtta ~40 aktarma vardır (bölüm 5.5). Taze istasyon başına 3–4 aktarma, ayrıca konumlanma ve yaralı vardiyalar için
  ek seferler: **harekât başına ~12–18 sefer**.
- Hiç taşınmayan oyuncu ilk semtte, yaklaşık **7. vardiyada** `dusus` ile kaybeder (5. kapanışta uyarı, iki vardiya süre).
  Bu yeni oyuncu için serttir; rehber ilk seferi 4. vardiyada öğretir (bölüm 7), uyarı iki vardiya sürer ve acil seferle
  kurtulunur.

**Görünürlük.** İZ'in nedenleri, her istasyonun İZ'i ve sonraki kapanışın tahmini public durumdan türer. Örnek tahmin:
"durursan +0,5, dalarsan +1, yola çıkarsan KONUM 1,5". Gizli kimliğe dokunmaz. KOS'un gizlilik ilkesi korunur: yankı sinyali
bugünkü gibi yalnız olay kâğıdıyla İFŞA'ya yazılır.

### 5.3 Sefer: taşınma komutu [YENİ] [KURAL]

**Komut** `rail_move` (a: hedef istasyonun semt kimliği). Core metodu `OrderRailMove(string regionId)`, gerekçe okuyucusu
`RailMoveCheck(string regionId)`. Komut `RunCommand(..., "rail_move", regionId)` ile çalışır; böylece enerji değişimi deftere,
komut kayda yazılır (`Core/Economy.cs:37-43`). `Replay.Apply` dalı `case "rail_move": sim.OrderRailMove(c.a); break;` olur.

**Her sefer bir duraktır.** Hedef `Station`'ın `Links` komşusu olmalıdır. Uzak bir istasyona birkaç vardiyada, durak durak
gidilir. Yolda geçen vardiya kavramı yoktur: katar her kapanışta bir sonraki istasyona varır.

| Durum | Enerji (b) | Alan |
|---|---|---|
| Sefer (bir durak) | 6 | `railMoveCost` |
| Acil sefer (emir anında `LocationCompromised`) | ×2 (12) | `railEmergencyCostMul` (2) |

Acil sefer `FallPending`'e değil `LocationCompromised`'a bağlıdır. Böylece vardiya içinde bir aktarmayla eşiğe varılıp ekranda
KONUM ÇÖZÜLDÜ yazdığında sefer de acil fiyatından olur.

**Emir kuralları:**

- **Ne zaman:** vardiya içinde, fiş basılana kadar (`Board.Printed`; kapı komutlarının kuralı, `CandidateCommandCheck`,
  `Core/Simulation.CandidateGroups.cs:268-276`). Aktarma sürerken (`S.Raid != null`) ve son gelmişken verilemez.
- **Bedel emirde düşülür.** "Bu enerjiyle aktarma mı, sefer mi" kararı emir anında verilir. Enerji yetmezse emir reddedilir:
  "Sefer {0} enerji ister."
- **Emri değiştirme:** başka bir komşu istasyonla yeniden `rail_move`. Eski bedel aynen iade edilir, yenisi düşülür. Tek komut,
  defterde net fark.
- **Vazgeçme:** `rail_move` hedef olarak `Station` ile çağrılır (KAL). Emir kalkar, `PendingMoveCost` aynen geri verilir.
  Bekleyen emir yoksa KAL reddedilir ("Bekleyen sefer emri yok.") ve kayda girmez.
- **Geçersiz hedef:** komşu değilse "Katar tek seferde bir durak gider."; aynı istasyonsa ve emir yoksa yukarıdaki ret.
- **Aynı vardiyada aktarma ve sefer serbesttir** (aktarma gündüz, sefer gece). Aktarmanın menzili ve İZ'i emir anındaki
  `Station`'a göre işler; katar kapanışa kadar yerinden kıpırdamaz.

**Yola çıkış** (kapanış, `AdvanceRail`, bölüm 5.4):

1. `Station = PendingMove`, `StayShifts = 0`, `PendingMove = null`, `PendingMoveCost = 0`.
2. KONUM yeni istasyonun İZ'iyle yeniden hesaplanır. Eşiğin altındaysa ve `FallPending` açıksa uyarı kalkar
   (`konum_kalkti`). Sefer adımı `AdvanceShelterEndings`'ten önce çalıştığı için aynı kapanışta `dusus` gelmez.
3. Kapıda bekleyen grup için aşağıdaki kural.

**Kapıda bekleyen grup:**

- **Olağan grup peronda kalır** (`railLeavesGroup` true): `ClearCandidateGroup("peron")` ile boşaltılır
  (`Core/Simulation.CandidateGroups.cs:258`). RNG'siz yoldur; `PassCandidates` (`:340`) aynı yolu kullanır.
- **Canlı sayısı acil eşiğinin altındaysa** (`candidateEmergencyBelow` 3) ya da grup acil gruptur (`GroupEmergency`): grup
  katara biner, kapıda beklemeyi sürdürür. Aksi hâlde sefer `terk`'e doğrudan yol açardı; ayrıca boşalan kapı aynı kapanışta
  `AdvanceCandidateDoor` (`:248-255`) içinde acil grup üretir ve 32 ek çekim yapardı.
- Makas kolunun plakası emirden önce uyarır: "KAPIDA 3 AYILAN · YOLA ÇIKARSAN PERONDA KALIR". Oyuncu emirden önce birini
  seçebilir. Kapı komutları fiş basılana kadar açıktır.
- Bu kuralın zayıflığı bilinir: önce seçip sonra yola çıkılabildiği için kayıp çoğu zaman bekleme süresidir. Güçlendirme
  ikinci adımdadır (bölüm 5.7).

**KONUM ÇÖZÜLDÜ penceresi.** Enerji yetmezse ya da sefer emri verilmezse bugünkü gibi `dusus` gelir. Taze olmayan bir
istasyona gitmek de kurtarmayabilir: plaka hedefteki KONUM'u yazar. Son şalter seçeneği bugünkü gibi açıktır ("konum"
nedeniyle tahliye).

**Yankılının zinciri seferle durmaz.** Sinyal ve sabotaj saatleri kişiye bağlıdır; İFŞA seferle düşmez. Tecrit önemini
korur.

**Sakin istasyon.** Geri alınmış bir semtin istasyonunda durma İZ'i yazılmaz (halk katarı örter). Her cephe semti geri
alınmış bir semte komşu olduğu için hedefin yanında hep bir sakin istasyon vardır. Seçim şudur: hedefin kendi istasyonundan
kısa aktarma (+1) ama durma İZ'i, ya da sakin komşudan uzun aktarma (+1,5) ama durma İZ'i yok.

### 5.4 Vardiya kapanışının yeni sırası [KURAL]

Bugünkü sıra kayıt biçiminin parçasıdır (`Core/Simulation.cs:69-94`). Yeni adım gelirden hemen sonra, ekiplerden önce gelir:

> gelir → **sefer (`AdvanceRail`)** → ekip → kabul → kişiler → pano → vadeli olaylar → kapı → sonlar → olay açılışı →
> uyarılar → alarm → geçmiş

`AdvanceRail` şu sırayla çalışır:

1. Katarın olmadığı her istasyonun İZ'i `railCoolPerShift` kadar soğur (taban 0).
2. `PendingMove` varsa yola çıkış (bölüm 5.3).
3. **Yoksa** `StayShifts++`. `StayShifts > railStayFreeShifts` ve istasyon sakin değilse `ChangeRailTrace(+0,5, "durma")`.

Soğuma yola çıkıştan önce çalışır: katarın yeni bıraktığı istasyon ilk soğumasını bir sonraki kapanışta alır. Durma İZ'iyle
eşiğe varılırsa uyarı `AdvanceShelterWarnings`'te bugünkü gibi kurulur.

**RNG ve determinizm.** Yeni adım ve `rail_move` **hiç RNG çekmez**. Kapanıştaki çekim noktalarının sırası değişmez.
Çekim **sayısı** ise sonuçlara bağlıdır: sefer KONUM ÇÖZÜLDÜ'yü kaldırınca `dusus` gelmez ve harekât sürer; peron kuralı kapıyı
boşaltır. Varsayılan değerlerle peron boşaltması aynı kapanışta yeni grup üretmez: acil koşulda grup biner, olağan grubun
bekleme süresi (2) bir sonraki olağan gelişten (en az 3 vardiya, `admissionKnockMin`) kısadır. Bu, testle kilitlenir. Değişen
sonuçlar (İZ, grup, enerji, son) yüzünden sürüm 18 gerekir. `RailDistance` sabit grafiği okur.

### 5.5 Enerji ekonomisi [AD] + [KURAL]

Fonun kimliği korunur (`S.Fon`, `Core/WorldState.cs:169`), görünen adı ENERJİ olur. Gerekçe: geri alınan trafo merkezleri
cer akımı verir.

- **Gelir** bugünkü gibi: 2 + sabaha çıkan semt başına 0,5 (`Core/Balance.cs:15-16`).
- **Ödüller:** ikmal +10, düğüm +8 (`Core/Balance.cs:17-18`); semt ödülü bayrak açık yapılandırmada **15 → 5 (b)**.
- **Giderler:** aktarma bayrak açık yapılandırmada **12 → 14 (b)**, sefer 6, acil ×2.
- **Tavan yok.** Akış sıfıra yakın kurulunca birikme olmaz. Ölçüm birikme gösterirse tavan ayar adımıdır (bölüm 8).

Bayrak açık değerler `BalanceConfig.asset`'e `railEnabled` ile birlikte yazılır; `Balance` varsayılanları ve altın değerler
değişmez.

**Kaba hesap** (48 vardiya, ~40 aktarma ve ~%10 başarısız aktarma, ortalama ~5,5 geri alınmış semt, ~15 sefer; ölçülmedi):

| Kalem | Bugünkü ödül ve bedelle | Önerilen (b) |
|---|---|---|
| Ödüller | 24 × 10 + 12 × 8 + 12 × 15 ≈ 516 | 24 × 10 + 12 × 8 + 12 × 5 ≈ 396 |
| Gelir | 48 × (2 + 0,5 × 5,5) ≈ 230 | ≈ 230 |
| Aktarma gideri | 40 × 12 = 480 | 40 × 14 = 560 |
| Sefer gideri | 15 × 6 = 90 | 15 × 6 = 90 |
| **Vardiya başına akış** | **≈ +3,7** | **≈ −0,5** |
| Başlangıç 60 ile harekât sonu | ≈ 236 artık | ≈ 36 |

Bugünkü değerlerle artık büyür ve enerji karar üretmez. İkmal ve düğüm ödüllerini 10 → 6, 8 → 6 indirmek de yetmez: akış yine
vardiya başına ≈ +1 kalır. Önerilen değerlerle akış hafif eksidir; başlangıç tamponu harekât boyunca erir, başarısız
aktarmanın ve acil seferin bedeli hissedilir.

Hedef: enerjinin aktarmayı ya da seferi engellediği vardiya %5–15. Hammadde ile ilişki değişmez. Hammadde üretimin girdisidir
(tarifler `Core/Balance.cs:224-235`), enerji yalnız aktarma ve seferdir. Takas ve kafile olayları enerjiye dokunmaz.

### 5.6 Aktarma ve dönüş (çıkış) kuralı

**Değişmeyenler [AD].** Motor aynen kalır:

- 8×8 tahta, 6 tur (`kisa` ile 5); görev türüne göre ek tur (`liberationLootExtraTurns`, `liberationServerExtraTurns`, ikisi de
  2) `MaxTurns`'e eklenir (`Core/Baskin.cs:318`; `Core/Balance.cs:75,83`). Ek tur dönüşe ayrılmış değildir.
- `Undo` ve `Preview`.
- Hedef tamamlanınca dönüş başlar (`CheckWin`, `Core/Baskin.cs:557-562`). Ekip ölü noktalardan çıkar. Süre dolarken
  tahtada kalan `Left` + `Dead` olur (`Core/Baskin.cs:1346-1360`) ve iki dünyada da ölür.
- **Dönüş penceresi** = `MaxTurns` − hedefin kırıldığı tur. Hedef geç kırılırsa 0 bile olabilir.

**Okunurluk [AD].** Bugünkü kural zaten "çıkışa yetişemeyen ölür"dür; eksik olan okunmasıydı:

- **Kırmadan önce önizleme:** hedefe vuracak hamlenin önizlemesi "ŞİMDİ KIRARSAN DÖNÜŞE {N} TUR · EN UZAK KİŞİ {M} ADIM"
  yazar. N ve M Core'daki RNG'siz bir okuyucudan gelir (`Baskin` içinde yeni public okuyucu; durum yazmaz, tahta sonucunu
  değiştirmez).
- **Dönüşte geri sayım:** tur şeridinde "KATAR BEKLEMEZ · DÖNÜŞE {n} TUR"; son turda amber (dikkat), kırmızı değil.
- Plan üstünde ölü nokta kareleri.
- Ranza bölmesinde her ranzanın ampermetresi.
- `Left` olunca o ranzanın ibresi sıfıra iner, lambası söner.
- Sonuç kartında "DÖNMEDİ".
- Ertesi vardiya boş ranza, katlanmış battaniye.

**0 can ve geç kalma: kurgu [AD].** Ranzanın akım rölesi, DÜN'de yere düşen kişinin devresini kendiliğinden atar: kişi
ranzasında şokla ayılır (AKTARMA ŞOKU, `boardInjuryTurns` 3, ustalıkla 2 vardiya; `Core/Balance.cs:184`,
`Core/Simulation.PlanningRaid.cs:83-90`). Röle ancak ekipten biri devreyi tutarken güvenle atar; ekipte ayakta kimse kalmazsa
düşenler de dönmez (`dead = left || after <= 0 && !survivor`, `:69`). Dönüşte süre dolunca ayakta olan ama ölü noktaya
varamayan kişinin rölesi atmaz, çünkü devresi sağlamdır; Bakıcı katı silip basınca o da silinir. Yere düşmek oyuncunun
seçebildiği bir eylem olmadığı için bu fark bir ters teşvik olarak kullanılamaz. "0 can = ölüm" önerilmez, çünkü botta harekât
başına ölüm zaten ~6,4 (OT:526). İsteğe bağlı sert mod bölüm 10.2 soru 7'de.

**Dönüşte GERİ AL [KURAL], bayrak `raidRecallInEscape` (false; dikey dilimde açılır).** Bugün geri çekilme dönüşte
reddedilir (`Core/Baskin.cs:1382-1388`). Bu, "BAĞLANACAKSAN ŞALTER ELİNDE OLSUN" doktrinini en kritik anda yalanlar. Bayrak
açıkken dönüşte de kol çekilebilir, bedeli şudur:

- Tahtada kalan herkes geri alınır, hiçbiri ölmez. Geri alınanlar AKTARMA ŞOKU yaşar (canları ne olursa olsun yatış).
- İkmal görevi başarısız sayılır: depo anahtarı geride kalır.
- Düğüm söndürme görevinde düğüm söner (hedef zaten kırıldı), ama geri alma istasyona İZ +1 yazar (`railTraceRecall`).
- Motor: `RaidParams.RecallInEscape`; `Retreat` bu parametre açıkken dönüşte de çalışır ve geri alınanları `RaidResult`'a
  yeni `Recalled` listesiyle yazar. `RaidResult.Encode` alanı sona ekler; eski `raidres` dizgisi boş listeyle çözülür.
  Parametre kapalıyken `RaidEngineTests` kilitleri değişmez.
- Bu seçenek `Left` ölümlerini azaltır. Ölçüm hedefi bölüm 8'de yeniden tanımlıdır.

### 5.7 Ayılanlar (aday grubu)

- **[AD]** Takvim aynen kalır: 3–4 vardiyada bir, 2–3 aday, 2 vardiya bekleme, en çok biri (`Core/Balance.cs:250-260`).
  Dolu katarda yollama ve 2 erzak yolluk da aynı (`Core/Balance.cs:277`). Acil grup aynı (`Core/Balance.cs:261`).
- **Anlatı:** Ekip, DÜN'de bir uyuyanın tekrar kuralını kırar ve onu "söker". Kişi kendi yatağında ayılır: evinde,
  koğuşta ya da sahra yatağında. Gece Hattı'nın hat ekipleri onu bulur ve katarın durduğu perona yürüterek getirir. Siluetler
  pijamalı ya da iş kıyafetli, sersem ama zayıflamamış insanlardır; sedye, battaniyeye sarılı beden ve kapsül yoktur.
  Bugünkü silahlı, asker ve kopya siluetleri kalkar.
- **Şüphe kademesi → ayılma kaydı:**

| Kademe | Satır |
|---|---|
| 0 | "ekip söktü, aktarma kaydı tam" |
| 1 | "yarı ayık bulundu" |
| 2 | "kendiliğinden ayılmış". DÜN'den kendi çıkan kimse yoktur; varsa onu biri bırakmıştır |

  Kefil ve belge satırları (`Core/Simulation.CandidateGroups.cs:41-55`) bu dile çevrilir.
- **PERONDA BIRAK.** Ayılan biri DÜN'e geri dönemez. Bırakılanı başka bir hat ekibi bulur.
- **[KURAL]** Yola çıkarken bekleyen olağan grup peronda kalır (bölüm 5.3).
- **İkinci adım [KURAL], bayrak `groupFromDive`:** grup başarılı aktarmadan sonraki kapanışta gelir. İki grup arasında en
  az 2 vardiya geçer. Kapının çekim sırasını değiştirir (bugün kapı 32 çekim, `Core/Simulation.cs:80`). v18 yayınlandıktan
  sonra yapılırsa sürüm 19 ister. Peron kuralını da güçlendirir: grup katarın durduğu yerde gelir.

### 5.8 Yankılı (sızmacı), olaylar ve sonlar

**Yankılı [AD].** Uykusunda DÜN'ün günlük düzenini tekrarlayan biridir: istasyon anonsunu mırıldanır, DÜN'deki saatinde
kalkar, eski yolunu yürür. Çoğu zaman bunu kendisi bilmez. İz, bilinçsiz bir alışkanlıktan doğar ve Arayıcılar onu duyar.
`Robot` alanı, üretimde sabitlenen kimlik ve kademe olasılıkları (%5 / %20 / %45) aynen kalır. Bugün insan da %20
olasılıkla iz bırakır (`Core/Balance.cs:279-284`); bu dikkatsizlik olarak anlatılır: tünelde sigara yakar, DÜN'de uyuyan
ailesine mektup yazıp istasyonda bırakır. "Geri dönmek isteyen" ya da "makinenin ele geçirdiği" çerçevesi kullanılmaz. Hain
arketipi öne çıkmaz; tecride trajik bir ağırlık eklenir.

**DÜZELTİLEBİLİR damgası** her ayılanın dosyasında baştan vardır. Kimliği ele vermez. **DÜZELTİLDİ** damgası yalnız yankılı
açığa çıktıktan sonra (sabotaj, tecrit bulgusu ya da son rapor) basılır. Böylece KOS'un "gizli kimlik sunuma sızmaz" ilkesi
korunur.

**Olaylar.** 14 kimlik, tetikler ve sayılar aynen kalır [AD]. Metinlerde "yukarıdaki halk" varsayımı "hat ekipleri"ne
döner. Konvoy, çağrı ve takas başka katarlara ve gezici hat ekiplerine bağlanır; "istasyon halkı" yazılmaz. SESSİZ VARDİYA
(bugünkü KARARTMA): katar bir vardiya bütün devreleri ve atölyeyi kapatır; üretim ve mutfak durur, Bakıcı'nın dikkati dağılır
(alarm −2, aynen). İz kaybettirme olarak anlatılmaz.

**İsteğe bağlı [KURAL]** (bayrak `railEventsRebind`, varsayılan kapalı). YÜRÜYEN KAFİLE'yi kabul etmenin bedeli bugün alarm
+2'dir. Bu bedel **İZ +1** olur (katarın istasyonuna). Gerekçe: dışarıdaki bir kafile DÜN'ün içindeki alarmı değil, katarın
izini büyütür. Karar bölüm 10.2 soru 4'te.

**Sonlar.** Kimlikler, koşullar ve öncelik aynen kalır (`topluluk > dusus > terk > dagilma`, `Core/Simulation.cs:80-95`).
Değişen tek şey `dusus`'un artık bir kararın sonucu olmasıdır: sefer yapılmadı, enerji bırakılmadı ya da yanlış istasyon
seçildi. Zafer tonu temiz/bedelli/gölgeli aynen kalır; temiz tonun ifşa ölçütü yalnız İFŞA'yı (sızmayı) ölçer.

| Kimlik | Metin önerisi (TR) |
|---|---|
| `silme` | "Çekirdek söndü ve DÜN bitti. On iki semtte saatler ilk kez ertesi güne geçti; kimse hangi günde olduğunu bilmiyordu." Gölgeli: "Son düğüm söndü. Koğuşta {ad} uykusunda hâlâ eski anonsu mırıldanıyordu." |
| `dusus` | "Ray dinleyicisi iki yönden aynı sesi verdiğinde mantar panoda hâlâ ertesi seferin rotası asılıydı." |
| `salter` | "Cer şalteri indi, devreler söküldü. Katar 07 bir daha DÜN'e bağlanamayacaktı; geri alınamayan semtler kendi günlerinde kaldı. Çıkanlar yürüyerek başka bir hat ekibine katıldı." |
| `topluluk` | "Son ranza da boşaldı. Kol çerçevesinde yalnız boştaki kollar kaldı." |
| `dagilma` | "Koğuş panoları bizim aktarmalarımızda kırıldı. Hat ekipleri işaret vermeyi bıraktı; perona artık kimse gelmiyor." |
| `terk` | "Erzak bitti, insanlar birer birer peronda indi. Kalanlar katarı yürütemedi." |

### 5.9 Core etkisinin özeti

| Değişiklik | Yer | Rec + Replay | Sürüm | Test |
|---|---|---|---|---|
| `RailState`, `RailDistance`, menzil denetimi | `Core/WorldState.cs`, `Core/CampaignLiberation.cs:125` | — (durum) | 18 | yeni `RailMoveTests` |
| İZ, KONUM, `ChangeRailTrace`, eşik sınamasının KONUM'a geçmesi | `Core/Simulation.ShelterEvents.cs`, `Core/Simulation.Endings.cs`, `Core/CampaignLiberation.cs:194` | — | 18 | `RailMoveTests`, `EndingTests` |
| `rail_move` | yeni `Core/Simulation.Rail.cs` (partial) | **evet** (`RunCommand`) | 18 | gidiş-dönüş testi |
| `AdvanceRail` adımı | `Core/Simulation.cs:69-98` | — | 18 | kapanış sırası testi (`ShiftCloseCoreTests`) |
| Grup peronda kalır | `Core/Simulation.CandidateGroups.cs` | — | 18 | `CandidateGroupTests` |
| Dönüşte GERİ AL | `Core/Baskin.cs`, `Core/WorldState.cs` (`RaidResult`), `Core/Simulation.PlanningRaid.cs` | `raidres` (alan eklenir) | 18 | `RaidEngineTests` (yeni durumlar), `PlanningBoardTests` |
| Kırma önizlemesi okuyucusu | `Core/Baskin.cs` | — | yok | `RaidEngineTests` |
| Konvoy bedeli (isteğe bağlı) | `Core/Simulation.ShelterEvents.cs` | — | 18 | `ShelterEventCaseTests` |
| Grup aktarmadan (ikinci adım) | `Core/Simulation.CandidateGroups.cs` | — | 18 ya da 19 | `CandidateGroupTests` |

**Yeni `Balance` alanları (b):**

- Bayrak ve konum: `railEnabled` (false; dikey dilimden sonra `BalanceConfig.asset`'te true), `railDiveRange` 1.
- Sefer: `railMoveCost` 6, `railEmergencyCostMul` 2, `railLeavesGroup` true.
- İZ: `railTraceDive` 1, `railTraceDiveLong` 1,5, `railStayFreeShifts` 2, `railTraceStay` 0,5, `railCoolPerShift` 0,5,
  `railTraceRecall` 1.
- Tahta: `raidRecallInEscape` false.
- Olay: `railEventsRebind` false.
- İkinci adım: `groupFromDive` false.
- Bayrak açık yapılandırmada değişen var olan alanlar: `liberationCaptureFunds` 5, `liberationRaidCost` 14.

Sayılar `double`, yuvarlama `Js.Round` (AGENTS.md kural 2).

**Sunumda eşlenecek yeni kimlikler.** `PassedCandidate.Kind = "peron"`, defter kimliği `rail_move`, İZ nedenleri
(`aktarma`, `uzun_aktarma`, `durma`, `geri_al`, `kafile`) ve `SquadOutcome`'un geri alınma durumu. Hepsi teleks, devir fişi
ve son rapor eşlemesine girer; yoksa `KosFinalReportTests.Ids_CoreIdsMapped_UnknownIdsNeverRaw`
(`Desk/Tests/EditMode/KosFinalReportTests.cs`) kırılır. İZ nedenleri `ShelterEvents.Log`'un `ifsa` satırlarına yazılmaz; son
rapordaki ifşa adımları yalnız sızmadır.

**Sürüm.** `GameRecord.CurrentVersion` 17 → 18, aşama 1'de. Bu bilinçli bir seçimdir:

- Kayıt kendi `Balance`'ını taşır ve `Replay.Restore` oyunu tohumdan yeniden oynatır (`Core/GameRecord.cs`). Bayrak kapalı
  bir v17 kaydı yeni kodla aynen oynar; yeni durum alanları kayda yazılmaz, yeniden kurulur.
- 18'e çıkmanın nedeni ters yöndür: bayrak açık bir kaydı eski yapı (17) `rail_move`'u tanımadan yanlış oynatır. Sürüm
  farkı onu yüklemez.
- KOS'ta sürüm 17'de tutulmuştu, çünkü değişiklikler yayınlanmamış aynı aşamanın içindeydi (KOS:559-562). Burada dönüşüm ayrı
  bir yayın aşamasıdır. Sürüm 17 hiç yayınlanmadı; gerçek kayıt yerinde sürüm 16 var (`Docs/DURUM.md:161`). Aşamalar
  arasında üretilen geliştirme kayıtları geçersizdir (KOS §9 ilkesi).

**Test ilkesi.**

- Bayraklar kapalıyken altın değerler aynen geçer: kartlı yolun kayıt testleri, `RaidEngineTests` ve olay testleri.
- Bugün `LiberationCampaignTests` kayıt testleri eski yolda koşuyor (`Tests/EditMode/LiberationCampaignTests.cs:10,13,66,542`
  `LegacySimulation` kullanır; `RaidMapsTests.cs:93`, `EndingTests.cs:622` ve toplam 24 test dosyası bu yola bağlı). Bu
  yüzden eski yolun sökümü bu ağ kartlı yola taşındıktan sonra yapılır (bölüm 9, aşama 11).
- `railEnabled` açık yeni testler şunları kilitler:
  - menzil ve tek durak kuralı;
  - bedel, emri değiştirme, vazgeçme ve iade; emirsiz KAL'ın reddi;
  - acil sefer fiyatının `LocationCompromised`'a bağlılığı;
  - İZ'in istasyona yazılması, soğuma ve devralma; sakin istasyon;
  - KONUM = İFŞA + İZ ile uyarı süreleri ve uyarının seferle kalkması;
  - seferin İFŞA'yı düşürmemesi; alarm ara bedelinin yalnız İFŞA'yı okuması;
  - peronda kalan olağan grup, binen acil grup, acil eşiğin altında binen olağan grup; peron boşaltmasının aynı kapanışta
    ek çekim yapmaması;
  - sefersiz `dusus`;
  - `rail_move` içeren kaydın `RunCommand` defter satırı, JSON gidiş-dönüşü ve `Replay.Restore` ile birebir kurulumu (KOS
    aşama 8'deki 24 harekâtlık sınamanın aynısı);
  - aktarma ortasında kaydedilip devam edilen harekâtta `Recalled` alanlı `raidres`'in doğru oynaması.

## 6. Sunum

### 6.1 Kısıtlar (BR4'ten aynen)

- uGUI + TMP, özel shader yok (BR4:199).
- Bir pozda en çok 4 gölgeli ışık (BR4:207).
- 1920×1080'de gövde metni ≥ 18 px, ikincil metin ≥ 14 px.
- Her metin TR/EN, dokuya yazı gömülmez.
- Hiçbir durum yalnız renkle verilmez.
- Ayrılmış renkler: kırmızı = makine; amber = dikkat; harfli ahşap = ekip (BR4:215).
- Yol gösteren nesnenin kendisidir; geri dönülmez işler iki adımlıdır.
- Sahne ve prefab asıl kaynaktır. Sahne koddan değişecekse tek seferlik editör script'i yazılır, çalıştırılır, silinir
  (AGENTS.md kural 5).
- Bölüm 2.6 YAPMA listesi.
- **Üçüncü taraf model paketi kullanılmaz** (BR4'ten değil; 15 Kasım kararı, bölüm 6.7). Her nesne Rev3 ya da yeni OBJ +
  manifest modelidir. Bütçe istisnası yoktur: BR4:202–203'teki "paket modellerinin bütçeyi aşması kabul" maddesi ve BR4:125'teki
  marka yazısı örtme işi bu dönüşümde geçersizdir.

### 6.2 Komuta vagonu (DeskGame)

**Kabuk.** Bugünkü oda 4,4 × 3,9 × 2,5 m düz kutudur (BR4:225). Yerine sıhhiye katarının komuta vagonu gelir:

- **Ölçü:** iç genişlik ~2,9–3,2 m, yükseklik ~2,4 m, kavisli tavan (yeni `CarCeilingArc` modülü). Yan duvarlar düz panel ve
  zırhlı kepenklidir. Zemin ızgara ya da kauçuk kaplamadır.
- **Kepenkler:** biri aralıktır. Durunca aralıktan istasyonun ad levhası görünür; seferde tünel lambaları kayar (sprite
  şerit, shader yok). Makine kırmızısı tünelde görünmez.
- **Bölümler:**

| Bölüm | Ne var |
|---|---|
| Ön | hareket masası (kol çerçevesi) ve okuma masası |
| Orta | mürettebat panosu, seyir köşesi, cer şalteri |
| Arka | ranza bölmesi (koğuş; bölme kapısıyla (`BayPartitionDoor`), serum askısı ve ilaç dolabı) |
| Yan | vagon kapısı ve peron kabul tezgâhı |

- Bugünkü düz duvarlar (paket `SM_Wall4x4` ×6 ve `SM_Wall15`) sökülür; düz kutu "sabit sığınak" gibi okunur. Kabuk yeni
  modüllerden kurulur: `CarSidePanel`, `CarCeilingArc`, `CarFloor`, `CarEndBulkhead` (körük geçidi), `Shutter`. BR4:89'daki
  iki model dili sorunu paket nesnelerinin tümüyle kalkmasıyla çözülür: oda tek model dilinde yeniden kurulur.
- İç düzen Aurora tipi bir lokomotif kabinine benzememelidir: lokomotif yoktur, vagon bir hastane vagonudur.
- Kesin yerleşimi ve ölçüyü tasarım turu verir.

**Nesne nesne karşılık** (K = kalır, YA = yeniden adlandırılır, YT = yeniden tasarlanır, Y = yeni, X = kaldırılır). "Kaynak"
bugünkü kaynağı gösterir; "paket (sökülür)" yazan her nesne yeni model olur (bölüm 6.7):

| Bugün (bileşen) | İşlev · poz | Kaynak (bugün) | Yeni | Karar |
|---|---|---|---|---|
| Masa SM_Table_2 | K1 taşıyıcı | paket (sökülür) | HAREKET MASASI: yeni masa modeli (`SignalDesk`; Rev3 `Desk` tabanı) + kol çerçevesi (3 kol = ekip) | YT (yeni model) |
| 6 sayaç (`CounterBank`) | K1 | Rev3 | VARDİYA, ENERJİ, ERZAK, HAMMADDE, SABAH, GÜVEN; fark fişi K | YA |
| Anahtar rayı SIĞINAK/KAPI/MASA (`StationKeyRail`) | K1 | Rev3 | MÜRETTEBAT / KAPI / HAREKET / SEYİR | YA + bir tuş |
| Vardiya şalteri (`ShiftSwitch`) | K1, K6 | Rev3 | aynen; plaka YA | K |
| Teleks = daktilo (`Teleprinter`) | K1 | paket (sökülür) | SEFER YAZICISI: yeni model (`RunPrinter`; Rev3 `Telex` tabanı ve bugünkü Rev3 kâğıt parçaları); başlık "K-07 → GECE HATTI" | YT (yeni model) |
| Devir fişi → EVRAK (`HandoverSlipFace`, `EvrakTray`) | K1 → K6 | Rev3 | aynen | K |
| Olay tutanağı (`ShelterEventPaperFace`) | elde | Rev3 | biçim K, künye YA | K |
| YZ ALARMI (`AlarmPanel`) | K1 | Rev3 | BAKICI ALARMI | YA |
| İFŞA şeridi (`ExposurePanel`) | K1 | Rev3 | RAY DİNLEYİCİSİ: KONUM 6 hücre (İFŞA sabit hücreler + İZ yarım yanan hücreler), titreyen ibre, dinleme borusu; seyir köşesine taşınır, özeti K1'den okunur | YT |
| SON ŞALTER (`LastSwitchPlate`) | KLS | Rev3 | CER ŞALTERİ: yeni model (`TractionSwitch`; Rev3 `LastSwitch` tabanı), büyük bıçaklı kol; menüdeki ÇIKIŞ şalteriyle ve kol çerçevesiyle aynı aile | YT (yeni model) |
| Afiş "AĞA BAĞLANMA · ANALOG KAL" | K1 (~80 px, BR4:97) | Rev3 | aynı afiş, büyür; el yazısı ek satır | YT |
| Şablon "KYGA / SIĞINAK 07" | K1 | Rev3 | "GECE HATTI · KATAR 07" ve kızılay yerine sivil savunma sıhhiye işareti (KYGA mührü eski boyanın altında silik) | YA |
| Kurtarılan bölgeler panosu (`FreedRegionsBoard`, SM_Board) | K1 | paket (sökülür) | SABAH n/12: yeni pano (`MorningBoard`; 12 semt kartı, sabaha çıkan semtin lambası) | YT (yeni model) |
| Harita + ekip figürü (`TeamFigure`) | Map | Rev3 | OKUMA MASASI: tek şehrin siyanotip hat planı, 12 istasyon, her istasyonda İZ işareti, katar işareti, ekip figürü | YT |
| Vardiya panosu (SM_Board gövde + Rev3 parçaları) | K4 | gövde paket (sökülür) + Rev3 | MÜRETTEBAT PANOSU: yeni gövde (`CrewBoard`), vagon duvarına vidalı (BR4:60'taki havada durma çözülür); Rev3 kartlar, cepler ve ekler yeni gövdeye oturur | gövde YT; Rev3 parçaları K, yeri YT |
| CRT SM_TV_2 (`CrtScreen`, `CrtViewBuilder`) | K5a | paket (sökülür) | beyaz fosfor; yeni CRT kasası (`CrtCase`; Rev3 `Crt` tabanı, `Screen` parçası bugün de Rev3); ekip kartları, akım ibreleri, telsiz | YT (yeni model) |
| Telsiz SM_Radio + SM_Device_1 | K5a | paket (sökülür) | TELSİZ: yeni telsiz ve güç ünitesi (`CarRadio`; Rev3 `Radio` tabanı). Ahize yok: masa ya da bas-konuş el mikrofonu (YAPMA 5; bugünkü telsizin ahizesi vardı) | YT (yeni model) |
| CRT'ye aktarma (`CrtTransition`) | oda → baskın | — | kollar sırayla iner, ranzalarda ibreler kalkar, yazıcı planı basar: "AKTARMA · K-07 · 3 DEVRE" | YT |
| Ayarlar dolabı SM_PowerBox1 | K9 | paket (sökülür) | SİGORTA PANOSU: yeni kutu (`FusePanel`; Rev3 `SettingsCabinet` tabanı), menüdeki sigorta kutusuyla aynı model | YT (yeni model) |
| Kapı SM_Door + vuruş lambası (`DoorProp`) | K8 | paket (sökülür) | VAGON KAPISI: yeni model (`CarDoor`; çarklı basınç kapısı önerisi, Rev3 `DoorFrame`/`DoorLeaf` tabanı); vuruş → peron zili, yeni lamba (`PlatformBellLamp`) | YT (yeni model) |
| Gözetleme kapağı, buğu camı, iç interkom | — (işlevsiz, BR4:104) | Rev3 | — | X |
| Koridor, merdiven, 6 siluet (`VisitorSilhouette`) | K8 | Rev3 | peron kenarı (yeni `PlatformEdge`; Rev3 `Corridor` tabanı; sodyum lamba `SodiumLamp`); ayılanlar (yeni siluet ailesi: pijamalı ya da iş kıyafetli, ayakta) | YT (yeni model) |
| Kabul tezgâhı, 2 damga (`AdmissionStation`) | K8A/K8R | Rev3 | KABUL / PERONDA BIRAK; boş plakalar X (BR4:105) | K |
| Aday kartı, grup fişi | elde | Rev3 | AYILMA KAYDI kartı; DÜZELTİLEBİLİR damgası | YA |
| Emir belgesi, açılış (`BriefingView`, `OpeningSequence`) | açılış | — | SEFER EMRİ; jeneratör ve ampul K | YT |
| Son rapor (`FinalReportView`) | ekran | — | düzen K; damgalar ve cümleler yeni | YA |
| Masa lambası, kupa, borular, kanal, ampul | dekor | lamba ve kupa paket (sökülür); boru, kanal, ampul Rev3 | yeni masa lambası ve kupa (Rev3 `DeskLamp`, `Mug` tabanı); boru, vana, kablo artar | lamba ve kupa YT; Rev3 dekor K |
| Duvarlar SM_Wall4x4 ×6, SM_Wall15 | kabuk | paket (sökülür) | vagon kabuğu modülleri (yukarıda) | X |

**Yeni nesneler (Y):**

1. **Seyir köşesi.**
   - Makas kolu: iki adım, KAL / YOLA ÇIK. Plakasında hedef istasyon, oradaki KONUM, enerji bedeli (acilse iki kat) ve
     "peronda kalır" uyarısı yazar.
   - Enerji göstergesi: yeni ibreli gösterge (`TractionGauge`) ve akü sandığı (`BatteryChest`).
   - Ray dinleyicisi (KONUM).
   - Mantar panoda hat haritası; her istasyonda İZ raptiyeleri.
2. **Ranza bölmesi.**
   - 3 ranza (yeni `BunkBerth` modeli, aynı model üç kez). Yatak altında akım bobini, ranza başında ampermetre ve lamba. Başa takılan hiçbir şey
     yoktur.
   - Aktarma sürerken ekibin bedenleri ranzalarda yatar.
   - `Left` ölümü burada okunur: ibre sıfıra iner, lamba söner. Ertesi vardiya battaniye katlanmış durur.
3. **İstasyon ad levhası:** kepenk aralığından görünür. Semt adı ve durak numarası taşır, metin TMP yuvasındadır.
4. **Kol çerçevesi:** hareket masasının arkasında demiryolu kilitleme kolları. Aktarmada üç kol öne alınır; GERİ AL'da
   kollar geri çekilir. Kollar cer şalteri ve makas koluyla aynı ailedendir.

### 6.3 Pozlar

| Poz | Bugün | Yeni | Not |
|---|---|---|---|
| K1 | masa | HAREKET MASASI | sayaçlar, yazıcı, vardiya şalteri, EVRAK, BAKICI ALARMI, kol çerçevesi ve KONUM özeti bir bakışta |
| Map | harita | OKUMA MASASI (tepeden) | sefer hedefi burada seçilir; menzil halkası ve istasyon İZ'leri görünür |
| K4 | pano | MÜRETTEBAT PANOSU | |
| K5a | CRT | EKRAN + TELSİZ | |
| **KS** (yeni) | — | SEYİR KÖŞESİ | makas kolu, enerji, ray dinleyicisi |
| **KR** (yeni) | — | RANZA BÖLMESİ | aktarma başlarken ve dönüşte kısa geçiş; `Left` anı |
| K6 / K6a | vardiya şalteri | aynen | |
| K8 / K8A / K8R | kapı, kabul | VAGON KAPISI, PERON TEZGÂHI | |
| K9 | ayarlar | SİGORTA PANOSU | |
| KLS | son şalter | CER ŞALTERİ | |

Poz değerleri `SiginakPose` ve `SiginakCameraRig` yöntemiyle yeniden kurulur. Bugünkü değerler BR4:255-269'da.

### 6.4 Okuma masası (baskın sahnesi, `Scenes/Baskin.unity`) için sanat yönü

**Ana fikir.** Kol çerçevesi ekibin algısını konuma çevirir. Okuma masası onu **kat planı** olarak basar. Bugünkü ozalit
pafta gerçek siyanotipe döner. Kâğıt koyu Prusya mavisidir, çizgiler açıktır. Pafta alttan aydınlatılan buzlu camın üstüne
serilir; aydınlatma emission ile verilir, shader yoktur.

**Korunanlar:**

- Ortografik izometrik kamera, 3B bina kitleri, 8 makine modeli.
- Harfli ahşap piyonlar. Harfler T / E / A yerine T / S / K olur (Teknisyen, Sarsıcı, Kılavuz).
- Yağlı kalem niyet okları, A–H / 1–8 kare adları.
- Kâğıt HUD: tur şeridi, görev çipi, üç ekip kartı, Geri al, EMRİ İLET, sonuç kartı.

**Değişenler:**

| Öğe | Yeni |
|---|---|
| Antet | OKUMA PLANI · K-07 · SEMT n · KAT n |
| Ölü noktalar | açık renkli halka ve "Ö" işareti; dönüşte yanar |
| Kırma önizlemesi | "ŞİMDİ KIRARSAN DÖNÜŞE N TUR · EN UZAK KİŞİ M ADIM" |
| Geri sayım | dönüşte tur şeridinde "KATAR BEKLEMEZ · DÖNÜŞE n TUR"; son turda amber (dikkat) |
| Koğuş panosu (`civ`) | bakım panosu simgesi; kırılınca "KOĞUŞ PANOSU KIRILDI: bir koğuşun bakımı kesildi" |
| Beşik (kopya dolabı) | yeniden doğma noktası; "devreye girdi" işareti |
| TELSİZ / KURALLAR / GERİ ÇEKİL | TELSİZ / KURALLAR / GERİ AL |
| Sonuç kartı | geride kalan "DÖNMEDİ"; geri alınan "GERİ ALINDI · ŞOK"; damgalar yeni |
| Ekip kartları | akım ibresi (can) ve ranza numarası |

**Palet.**

- Siyanotip mavisi ve açık çizgi.
- Makine ve tehdit kırmızısı aynen (yalnız plan üstünde).
- Ekip ahşap tonu aynen.
- Amber yalnız dikkat.
- Tarama çizgisi gerekirse sprite katmanıyla verilir (`CrtScreen`'in yöntemi).

**Kural ayrıntısı.** Bugün odada haritada "mavi = geri alınmış" anlamı var (OT:23). Siyanotip plan bu anlamla çakışmaz,
çünkü plan aktarmanın kendi kâğıdıdır. Okuma masasının harita hâlinde ise anlam değişir: sabaha çıkan semt = lambası yanan
istasyon, işgalli semt = kırmızı. Tasarım turu bunu kesinleştirir.

### 6.5 Ekran rengi

CRT'nin fosfor yeşili (`#A6E8B8`) Matrix'in imzasına en yakın öğedir ve değişmelidir. Kehribar da düşünüldü, ama amber
BR4:215'te "dikkat / bekleyen iş" için ayrılmış. Öneri: **beyaz fosfor** (P4, soğuk beyaz-gri, ~`#D8E4E8`). Kehribar
seçilirse ayrılmış amber anlamı yeniden tanımlanmalı (bölüm 10.2 soru 3). Test etkisi:
`Desk/Tests/EditMode/SiginakFoundationTests.cs:116` rengi, `SiginakCrtTests.cs:97` "HAT AÇILIYOR" metnini sınıyor; ikisi de
güncellenir (yeni metin "DEVRE KURULUYOR").

### 6.6 Menü (Giris): bakım deposu

BR4'teki öncelikli istek (BR4:110-161) aynen korunur ve yeni sahnenin ilk tasarım maddesidir. Yer yolcu peronu değil, katarın
bakım deposudur:

- **Mekân.** Muayene çukuru, iş iskelesi, çukurun üstünde duran katarın burnu ve yan kapısı. Islak beton, sodyum ışığı,
  çukurda iş lambası. Fayanslı istasyon duvarı ve istasyon ad levhası menüde kullanılmaz. "Tren geliyor" ışığı ya da sesiyle
  açılış yapılmaz.
- **Yakın açılış pozu.** Dört seçenek aynı kadrajda, birbirine yakın ve bir bakışta okunur durur. Hepsi katarın yan kapısının
  yanındaki depo duvarında toplanır:
  - **SEFER SAATİ** ve kart rafı: YENİ HAREKÂT / DEVAM;
  - **SİGORTA KUTUSU**: AYARLAR;
  - **SON ŞALTER**: ÇIKIŞ.

  Katarın burnu, iskele ve "SON ŞALTER / CORRIGIBLE" başlığı arkada kalır.
- **Plakalar.** Her seçeneğin kendi fiziksel nesnesi ve TR/EN plakası vardır: YENİ HAREKÂT, DEVAM, AYARLAR, ÇIKIŞ. Plakalar
  okunmak için üzerine gelinmesini beklemez.
- **Durumlar okunur.**
  - Kayıt yokken DEVAM kapalıdır ve nedeni görünür: kart yuvası boş, plakada "KAYIT YOK".
  - Kayıt varken yeni harekât iki adımlı onay ister: kıskaç ve eski kartta "İPTAL EDİLİR" fişi.
  - Kart özeti "V.xxx · SABAH n/12 · KONUM n".
- **Geçiş.** Kart saate basılır, yan kapının çarkı döner, kamera eşikten içeri girer. Hareket aynı yakın pozdan başlar.
  Ayarlardan dönünce kamera yine bu poza gelir.
- **Adlandırma.** Kapıdaki ve şalterdeki yazılar eşitlenir; bugünkü "ANA ŞALTER" tutarsızlığı biter (BR4:132).

**Menü nesneleri:**

| Bugün | Yeni | Karar |
|---|---|---|
| Sahanlık, fayans, merdiven, tırabzan | depo zemini ve duvarı (`DepotFloor`, `DepotWall`), muayene çukuru, iş iskelesi, sodyum lamba (`SodiumLamp`) | YT (yeni model) |
| Kapı dış yüzü "SIĞINAK 07 · KYGA" | katarın burnu + yan kapısı "KATAR 07 · GECE HATTI" | YT (yeni model) |
| Puantaj saati + kart rafı (`TimeClock`) | SEFER SAATİ + SEFER KARTI | YA |
| Elektrik kutusu (bugün paket kutusu, sökülür) | SİGORTA KUTUSU (AYARLAR) | YT (vagondaki sigorta panosuyla aynı yeni model) |
| Dış interkom, zil | depo zili | K |
| Bıçaklı şalter (ÇIKIŞ) "ANA ŞALTER" | SON ŞALTER (ÇIKIŞ), cer şalteriyle aynı aile | K; plaka YA |
| Başlık "SON ŞALTER" | "SON ŞALTER" / "CORRIGIBLE" | K |

### 6.7 Modeller: paket modelleri kullanılmaz (karar, 15 Kasım 2026)

**Karar.** Geliştiricinin sözü: "paket modellerini kullanmasın". Üçüncü taraf model paketleri bu dönüşümde **hiç
kullanılmaz**: ne Claude Design brifinde ne oyunda. Bütün nesneler tasarım turunda yeniden tasarlanır: Rev3'teki gibi Claude
Design'ın ürettiği OBJ + manifest biçiminde, poligon bütçesiyle. Bu karar, bu bölümün ilk hâlindeki "korunan paket
modelleri" listesinin yerini alır.

- **Kullanılmaz (sökülecek):** Leartes Cold War Underground Bunker (`Assets/LeartesStudios/`), daktilo paketi
  (`Assets/Typewriter/`), Pekdata CRT (`Assets/Pekdata/`) ve depodaki öbür indirilmiş modeller
  (`Assets/Victorian_typewriter-aa00f0fe/`, kökteki `MI_handles` ve `MI_office_desk_1` glTF dosyaları,
  `SM_office_desk_11.fbx`). Paketten türetilmiş mesh'ler de bu kapsama girer.
- **Serbest:** Rev3 modelleri (`Desk/Art/Rev3/Models/*.obj`; Claude Design'ın önceki turlarda ürettikleri) ve projenin
  kendi baskın kitleri (`Art/Raid/`). Bunlar olduğu gibi kullanılabilir ya da yeniden tasarlanabilir.
- **Kapsam dışı:** `Assets/Standard Assets/Environment/Water` (Unity'nin su paketi: bir shader, bir script, bir malzeme, üç
  doku). Model içermez ve hiçbir sahne, prefab ya da malzeme ona başvurmaz (guid ile tarandı). Model kararını etkilemez;
  temizlikte silinip silinmeyeceği aşama 9b'de geliştiriciye sorulur (soru 18).

**Bugünkü kullanım** (sahne ve `.meta` dosyalarında guid ile doğrulandı; sökülecek olanlar):

| Yer | Paket kullanımı |
|---|---|
| `Desk/Scenes/DeskGame.unity` | 19 örnek, 13 tür: masa, CRT, telsiz ve güç ünitesi, 2 pano, ayarlar dolabı, kapı, vuruş lambası, masa lambası, kupa, düz duvarlar (Leartes) ve daktilo. Ayrıca kapalı park edilmiş Pekdata `CRTMonitor` nesnesi |
| `Desk/Scenes/Giris.unity` | kapı ve elektrik kutusu (Leartes) |
| `Scenes/Baskin.unity`, `Desk/Prefabs`, `Prefabs` | yok |
| Paketten türetilmiş mesh'ler | `Desk/Art/Rev3/SceneMeshes/Door`, `PowerBox`, `Radio` (kordonsuz telsiz), `KnockLight` |
| Pakete kilitli testler | `KosStage8SceneTests`, `TelexTypewriterSceneTests`, `PlanningBoardSceneTests`, `Rev3Stage8EntranceTests`, `SettingsCabinetTests`, `SiginakRoomTests`, `Rev3SlipMotionTests`, `Rev3ShiftCloseSceneTests` (EditMode) ve `CandidatePlayTests` (PlayMode); fiş çıkış token'ı `shiftClose.slip.typewriterLocal*` |

Kalabilecek Rev3 parçaları: `PlanningBoardInserts_SM` (pano yüzeyleri) ve `Telex/*_SM` (kâğıt, rulo, besleme, plaka). Bunlar
paket gövdesine göre oturtulmuştu; yeni gövdeye göre yeniden konumlanır.

**Yeni karşılıklar.** Ad önerileri İngilizce kod adıdır; kesin biçimi tasarım turu verir. "Rev3 tabanı", başlangıç olabilecek
serbest modeldir (bunlar 13 Kasım'daki model entegrasyonundan önce sahnedeydi).

| Bugün (kaldırılan paket nesnesi) | Yeni model | İşlevi | Hareketli parçalar | Bütçe | Rev3 tabanı |
|---|---|---|---|---|---|
| masa | `SignalDesk` (HAREKET MASASI) | K1 taşıyıcısı | — | büyük | `Desk` |
| daktilo | `RunPrinter` (SEFER YAZICISI) | satır basar, devir fişini çıkarır | `Platen`, `Roll`, `Feed`, `Cover` | büyük | `Telex` + kâğıt parçaları |
| CRT (ve park edilmiş Pekdata CRT) | `CrtCase` (EKRAN) | tek beyaz fosfor CRT | `Screen` | büyük | `Crt` |
| telsiz + güç ünitesi | `CarRadio` + `RadioMic` (el ya da masa mikrofonu; soru 19) | sahayla ses bağı; ahize yok | Rev3 `Radio` manifestinin adları: `DialPointer`, `Needle`, `KnobVolume`, `KnobTune`, `DialLight` (`CrtInstruments`'taki `dial`, `needle`, `tune` alanları bileşen alanıdır, parça adı değildir) | büyük (ikisi birlikte) | `Radio`, `Microphone` |
| kurtarılan bölgeler panosu | `MorningBoard` (SABAH n/12) | 12 semt kartı ve lambası | `Lamp_01…12` | büyük (öneri ≤ 1 500) | `CorkBoard` |
| vardiya panosu gövdesi | `CrewBoard` (MÜRETTEBAT PANOSU) | kişi kartları, cepler, pano lambası | `BoardLamp` | büyük | `PlanningBoard` + `PlanningBoardInserts_SM` |
| ayarlar dolabı (iki sahne) | `FusePanel` (SİGORTA PANOSU / KUTUSU) | ayarlar; vagon ve menüde aynı model | `Leaf`, `Latch`, `Handle`, `Needle`, `Knob` | büyük; iç kontroller küçük | `SettingsCabinet`, `Cab*` |
| kapı (iki sahne) | `CarDoor` (VAGON KAPISI; dış yüzü menüde katarın yan kapısı) | K8'de perona açılır; menüde kartla açılır | `Hinge` ya da sürgü, `Wheel` | büyük (kasa ve kanat ayrıysa her biri) | `DoorFrame`, `DoorLeaf`, `OuterWheel` |
| vuruş lambası | `PlatformBellLamp` (peron zili lambası) | ayılanlar gelince yanar | `Lamp` | küçük | `KnockLamp` |
| masa lambası | `DeskLamp` | K1'in gölgeli spotu | `Shade` (isteğe bağlı) | küçük | `DeskLamp` |
| kupa | `Mug` | dekor | `Water` (isteğe bağlı) | küçük | `Mug` |
| düz duvarlar | `CarSidePanel`, `CarCeilingArc`, `CarFloor`, `CarEndBulkhead`, `Shutter` | vagon kabuğu | `Shutter` (kayar) | modül başına büyük; tekrar edene öneri ≤ 2 000 | yok |

Bu bölümün ilk hâlinde önerilen ama hiçbir sahnede olmayan paket nesneleri de yeni modelle karşılanır: kavisli tavan →
`CarCeilingArc`; jeneratör ve yakıt tankı → `TractionGauge` (enerji göstergesi) ve `BatteryChest` (akü sandığı); ranza ve
şilte → `BunkBerth`; dolap → `MedicineCabinet`; borular, vanalar, kablolar → Rev3 `PipeRun`, `Duct`, `JunctionBox` ile yeni
`CarValve`, `CableTray`; tırabzan → `GrabRail`; merdiven → `PitSteps` (menü). İkinci televizyon, ikinci telsiz ve telefon için
karşılık gerekmez (tek CRT, tek telsiz, YAPMA 5).

**Gereken yeni modeller.** Bu liste brifin (`Docs/UI_Rev5/BRIEF_rev5.md`) "Tasarlanacak modeller" tablolarına
eşitlendi. Brifin tablolarında olmayan beş model (`PlatformEdge`, `BayPartitionDoor`, `DepotFloor`, `DepotWall`, `SodiumLamp`)
bölüm 6.2 ve 6.6'daki nesnelerin karşılığıdır; brife de eklenmelidir. Parantez içi Rev3 tabanıdır, tabansız olanlar sıfırdan
çizilir.

- vagon kabuğu: `CarSidePanel`, `CarCeilingArc`, `CarFloor`, `CarEndBulkhead` (körük geçidi), `Shutter` (kepenk);
- istasyon ad levhası `StationSign`;
- hareket masası `SignalDesk` (`Desk`), kol çerçevesi `LeverFrame` (cer şalteriyle aynı aile), sefer yazıcısı `RunPrinter`
  (`Telex` ve kâğıt parçaları), masa lambası `DeskLamp` (`DeskLamp`), kupa `Mug` (`Mug`);
- ışıklı okuma masası `ReadingTable`;
- CRT kasası `CrtCase` (`Crt`), telsiz `CarRadio` (`Radio`) ve mikrofon `RadioMic` (`Microphone`; el ya da masa, soru 19);
- SABAH panosu `MorningBoard` (`CorkBoard`), mürettebat panosu gövdesi `CrewBoard` (`PlanningBoard` ve ekleri);
- sigorta panosu `FusePanel` (`SettingsCabinet`, `Cab*`; iki sahnede aynı model);
- vagon kapısı `CarDoor` (`DoorFrame`, `DoorLeaf`, `OuterWheel`; dış yüzü menüde) ve peron zili lambası `PlatformBellLamp`
  (`KnockLamp`);
- seyir köşesi: ray dinleyicisi `RailListener`, makas kolu `SwitchLever` (cer şalteriyle aynı aile), enerji göstergesi
  `TractionGauge` (`CabGauge`), akü sandığı `BatteryChest`, mantar panoda hat haritası `RouteBoard` (`CorkBoard`), cer şalteri
  `TractionSwitch` (`LastSwitch`; bölüm 6.2'deki "YT" kararı bu yeni modeldir);
- ranza bölmesi: ranza `BunkBerth`, ranza ampermetresi `BunkAmmeter` (`CabGauge`), akım bobini kutusu `CurrentCoilBox`, ilaç
  dolabı `MedicineCabinet`, serum askısı `DripStand`, katlanmış battaniye `FoldedBlanket`, bölme kapısı `BayPartitionDoor`;
- peron kenarı: `PlatformEdge` (ıslak beton, kenar çizgisi; `Corridor`) ve ayılan siluet ailesi (en az 4 çeşit: pijamalı, iş
  kıyafetli, yaşlı, genç);
- dekor: tutamak çubuğu `GrabRail` (`Handrail`), vana `CarValve` ve kablo kanalı `CableTray` (Rev3 duvar ailesi), sıhhiye
  katarı işareti (doku ya da kabartma, kelimesiz);
- bakım deposu (menü): depo zemini `DepotFloor`, depo duvarı `DepotWall` (dört seçeneğin durduğu duvar; `WallBlock`),
  muayene çukuru `InspectionPit`, iş iskelesi ve çukur basamağı `WorkGantry`, `PitSteps` (`Stairs`), katarın burnu ve yan
  gövdesi `TrainNose`;
- aydınlatma: depo ve tünel için sodyum lamba armatürü `SodiumLamp` (`CorridorLamp`).

Bu listede olmayan Rev3 nesneleri kalır ya da serbestçe yeniden tasarlanır: sayaç rafı, anahtar rayı, vardiya şalteri, EVRAK
tepsisi, kâğıtlar, kabul tezgâhı ve damgalar, BAKICI ALARMI paneli, afiş, duvar şablonu (`StencilSign`), CRT masası
(`CrtTable`), sefer saati, menüdeki SON ŞALTER, dış interkom, zil, başlık; borular, kanal ve ampul (`PipeRun`, `Duct`,
`CeilingBulb`).

Toplam: brifin tablolarında ~40 satır, yaklaşık **~45–50 model** (ilk tahmin ~11 idi; kabuk modülleri, siluet çeşitleri, mikrofon, vana ve
kablo kanalı, iskele ve basamak ayrı sayılır). Bunların ~20'sinin Rev3 tabanı var; kabuk, okuma masası, ranza, akü sandığı,
ilaç dolabı, serum askısı, siluetler ve depo modellerinin çoğu sıfırdan çizilir.

**Kurallar.**

- **Bütçe** (Rev3 kuralı, `Docs/UI_Siginak_Rev3/modeller/README.md`): büyük nesne ≤ 4 000, küçük ≤ 800, baskın parçası ≤ 400,
  makine ve piyon ≤ 1 500 üçgen; doku ≤ 1 024 px; ekranda 3 px'ten ince ayrıntı dokuya gider. İstisna yoktur. (Karşılaştırma:
  sökülecek daktilo 16 250, mantar pano 10 101, masa lambası 7 413, telsiz 7 006 üçgen; BR4:70-71.)
- **Modelde kelime yoktur;** yazılar `TXT_*` TMP yuvasındadır. Marka yazısı örtme işi kalkar.
- **Parça adları korunması istenen adlardır.** `Hinge`, `Wheel`, `Leaf`, `Latch`, `Handle`, `Needle`, `Knob`, `Cover`,
  `Lever`, `Plunger`, `Clip` Rev3 manifestlerinin parça adlarıdır (örneğin `LastSwitch`: `Lever`, `Cover`; `SettingsCabinet`:
  `Leaf`, `Latch`; `DoorLeaf`: `Hinge`, `Wheel`). Çalışma zamanı kodu parçaları adla aramaz (`Rev3Model.Part` yalnız
  testlerde çağrılır); sahne bileşenleri bu parçalara `[SerializeField]` ile bağlıdır (`DoorProp.leaf`, `wheel`;
  `SettingsCabinet.leafHinge`, `latch`; `CabinetFader.handlePart`, `needlePart`; `CabinetSelector.knobPart`;
  `TimeClock.plungerPart`, `clipPart`; `ShiftSwitch.coverHinge`, `lever`). Yeni model bu adları taşırsa yeniden bağlama
  script'i ve adları dizgi olarak sınayan testler (`Rev3DoorTests`, `Rev3ShiftSwitchTests`, `Rev3RecipeBookTests`,
  `PlanningBoardSceneTests`, `Rev3SlipMotionTests`, `Rev3ShiftCloseSceneTests`, `ShiftClosePlayTests` ve benzerleri) az
  değişir. Sefer yazıcısının çapaları (rulo, kâğıt
  besleme, fiş başlangıç ve çıkış) ve `shiftClose.slip.typewriterLocal*` token'ı yeni yazıcıya göre yeniden ölçülür.
- **Alma yolu** Rev3'ün model hattıdır (`Rev3Model`, `PartDriver`). Paket mesh'inden türev üretme yöntemi biter.
- **Teslim sırası önerisi** (Claude Design çıktısı birkaç teslime bölünür): menü (bakım deposu) → kabuk → masa ve duvar
  nesneleri → ranza bölmesi ve seyir köşesi. Menü BR4'ün öncelikli isteği olduğu için başta kalır, ama menü ve kabuk
  modellerinin çoğunun Rev3 tabanı yoktur. Vagon teslimlerinde önce Rev3 tabanlı modeller gelir (bölüm 10.1, model sayısı
  riski).

## 7. Metin ve çeviri

**Hacim.** `Resources/Localization/strings.txt` 2399 satır, yorumlar dışında 2306 çeviri satırı.

| Kaynak | Satır | Değişecek pay (tahmin) | Neden |
|---|---|---|---|
| Eski kapı `Core/DoorCards.cs` | 224 | silinir | eski yol (bölüm 9, aşama 11) |
| Baskın (RaidDataSource ~164, Baskin 125, Baskin.Events 112) | ~401 | %30–40 | rol, nesne ve makine adları, robotun "uyanması", kaçış satırları, bina olayları |
| Raporlar (vardiya 67, operasyon 49, son rapor 137) | ~253 | %60–80 | ton ve terimler |
| Olaylar (ShelterEvents 60, ShelterEventData 39 ve sunum) | ~152 | %50–70 | dış dünya varsayımı |
| GameData (bölge, bina, sonlar) | 88 | %70–90 | tek şehir ölçeği, son metinleri |
| Pano (PlanningBoardSnapshotBuilder ~113) | ~113 | ~%30 | görev ve eşya adları |
| Aday (CandidateGroups 42 + sunum 30) | ~72 | %60 | ayılma kaydı satırları |
| Son şalter 47, devir fişi 39 | 86 | %40–80 | |
| Rehber (`PlanningTutorialFlow` 53 ve öteki rehber dosyaları) | ~130 | %100 | 26 adım + sefer adımları |
| Sahne `DeskLocText` (DeskGame 124, Giris 13) | 137 | %60 | plakalar, şablon, afiş |

**Toplam tahmin.** DoorCards silindikten sonra ~2080 satır kalır. Bunun **~900–1200'ü** yeniden yazılır. Tema sözcüğü
taşıyan satır bugün 474 (%21): vardiya, sığınak, baskın, YZ, robot, kapı. Ton yeniden yazımı bunun 2–3 katı iş getirir.
Sayılar dosya başına kabaca sayıldı; satır satır sınıflandırılmadı.

**Çeviri hattı ve kurallar.** `Loc.T/F/N` kullanılır. Türkçe kaynak anahtardır. Değişen her Türkçe metin İngilizcesiyle
yeniden yazılır. Ardından "Corrigible > Lokalizasyon > Çeviri tablosunu güncelle" çalıştırılır ve `LocalizationTests`
geçer (AGENTS.md kural 9). Dikkat edilecekler:

- **Toptan değiştirme yasak.** "Sığınak → Katar" gibi bir toplu değiştirme ekleri bozar ("Sığınak 07'nin",
  `TrNumber.WithAblative`). Her satır elle yazılır.
- **"DÜN" yazım kuralı:** hep büyük harf ya da kesmeyle yazılır. Bunu `LocalizationTests` yakalamaz. Elle denetimi
  kolaylaştırmak için strings.txt'te "dün" geçen satırları listeleyen bir editör denetimi eklenebilir.
- **"Uyan-" kökü:** insanlar ve makineler için kullanılmaz (bölüm 2.4). Aynı editör denetimi "uyan" geçen satırları da
  listeler.
- **"Katar"** Türkçede ülke adıyla aynı yazılır. Metinde hep "Katar 07" ya da "komuta katarı" olarak geçer (bölüm 10.2
  soru 2).

**İsimler.**

- 10 kurucu adı (`Core/Simulation.PlanningBoard.cs:51`) ve 64 aday adı (`Core/Simulation.CandidateGroups.cs:20-26`)
  değişmeden kalabilir; Türkçe adlar yeni dünyaya uyar.
- Meslek adları role bağlı bir eşlemedir, RNG ile seçilmez (`ProfessionName`, `Core/Simulation.PlanningBoard.cs:112-123`:
  `tek` → Elektronikçi, `emp` → Elektrikçi, `ana` → Haberleşmeci…). Yeni dünyaya uyarlamak [AD]'dır; öneri: `tek` →
  **Makasçı**, öteki meslekler aynen.

**Rehber.** Bugün 26 adım (`Docs/Baslangic_Rehberi/README.md:5`). Hepsi yeni dille yeniden yazılır. Seferi öğreten 3 adım
eklenir:

1. Ray dinleyicisi (KONUM = İFŞA + İZ; İZ istasyonda kalır).
2. Makas kolu (KAL / YOLA ÇIK, bedel, hedef istasyonun KONUM'u).
3. İlk KONUM ÇÖZÜLDÜ uyarısında acil sefer.

İlk sefer, rehber tohumunda ~4. vardiyaya denk gelecek biçimde kurgulanır. Rehber sürerken son şalter mührü bugünkü gibi
kalır (`SiginakDesk.TutorialSealsSwitch`).

**Belgeler.** Uygulama bittikçe şu belgeler güncellenir:

- `AGENTS.md` (proje bir bakışta);
- `Docs/OYUN-TASARIMI.md`, `Docs/MIMARI.md`, `Docs/DURUM.md`;
- Claude Docs'taki GDD.

## 8. Ölçüm (Tools/DengeBotu)

**Düzenek.** `Tools/DengeBotu/` (README) gerçek Core ve gerçek `RaidBot` ile koşar. Ölçüm kuralı `RETIRE=1`, karar için 80
tohum. Gürültü "iyi" kümede ±7 puan.

**Eklenecekler:**

- ortam değişkeni `TASI=<politika>`;
- `BAL="railEnabled=1;…"`;
- satır başına yeni anahtarlar: `sefer`, `acil_sefer`, `konum_sefer` (sefer anındaki KONUM), `peron_grup`, `enerji_engel`,
  `enerji_son`, `dusus_kurtulma`, `geri_al_donus`;
- `RaidBot` için dönüşte GERİ AL kuralı (bayrak açıkken): en uzak kişi kalan turda ölü noktaya varamıyorsa kolu çek.

**Taşınma politikaları:**

| Politika | Kural |
|---|---|
| `hic` | Hiç taşınma (denetim) |
| `esik` | KONUM ≥ 5 ya da KONUM ÇÖZÜLDÜ iken taşın (geç) |
| `erken` | İZ ≥ 3 iken, sonraki aktarmaya enerji kalıyorsa taşın; grup bekliyorsa önce seç |
| `cephe` | Hedef menzil dışında kalınca hedefe doğru taşın; ayrıca `esik` |
| `her` | Her vardiya taşın (bedel denetimi) |

**Hedef seçimi** (bütün politikalarda):

1. Hedef semt menzildeyse: hedef semti menzilde tutan komşu istasyonlardan İZ'i en düşük olan.
2. Değilse: hedefe en kısa yol üstündeki komşu istasyonlardan İZ'i en düşük olan.
3. Eşitlikte sıradaki cephe semtine de komşu olan; sonra kimlik sırası (deterministik).

**Kapı alt politikası:** grup bekliyorken "önce seç" ya da "bırak".

**Taban.** Bugünkü kural (v17 dengesi), 80 tohum, aynı politikalarla.

**Hedef tablosu** ("iyi" küme × `erken` ve `cephe`; v18, bayraklar açık):

| Ölçü | Hedef | Not |
|---|---|---|
| Zafer medyanı (uzunluk) | 42–50 vardiya | bugün 48 (OT:506); sefer vardiya yemez, ama uzak cepheye giden yol aktarmasız vardiya üretir |
| Zafer oranı | tabandan en çok −5 puan | |
| Harekât başına sefer (medyan) | 12–18 | bölüm 5.2 ritim hesabı |
| Erken sefer payı (KONUM < 5 iken) | %30–60 | sefer her zaman eşikte yapılmamalı |
| Aynı istasyona 4 vardiya içinde dönüş | < %20 | git-gel çözülmüş mü |
| KONUM ÇÖZÜLDÜ'den seferle kurtulma | ≥ %70 | |
| Yenilgiler içinde `dusus` | %10–25 | bugün çoğunluk `dagilma` |
| KOS: cömert + görmezden gel `dusus` | ≥ %40 | bugün %61 (OT:511); İFŞA seferle düşmediği için korunmalı |
| KOS: karışık + tecrit `dusus` | < %15 | bugün %0 (OT:512); İZ yüzünden yükselebilir |
| `hic` politikası | %100 `dusus`, medyan < 10. vardiya | denetim: taşınma zorunlu |
| `her` politikası | ya enerjisiz kalır ya da zafer medyanı > 60 | denetim: taşınmanın bedeli |
| Enerji yüzünden aktarmasız ya da sefersiz vardiya | %5–15 | enerji karar üretir |
| Harekât sonu enerji medyanı | gözlem | birikme var mı (tavan kararı) |
| Peronda kalan grup | %15–30 ("bırak" alt politikasıyla) | |
| Dönmeyen (`Left`) / harekât | 0,5–2 | dönüşte GERİ AL açıkken |
| Dönüşte GERİ AL / harekât | 1–3 | |
| Toplam ölüm / harekât | ≤ 7,4 | bugün ~6,4 + 1 |
| Acil sefer / harekât | ≤ 1,5 | son dakika pahalı olmalı |
| Gölgeli ve temiz ton | KOS bantları (%5–15, %10–30) | temiz ton yalnız İFŞA'yı ölçer |
| Bayraklar kapalı | altın değerler aynen | test |

**Ayar sırası.** Hedefler tutmazsa şu sırayla oynanır; her biri tek başına ölçülür:

1. Net enerji akışı: `liberationCaptureFunds` 5 → 0 / 10 ve `liberationRaidCost` 14 → 13 / 15. Önce bu oynanır; öteki
   enerji hedefleri buna bağlıdır.
2. `railMoveCost` 6 → 4 / 8.
3. `railCoolPerShift` 0,5 → 0,25 / 1 (git-gel ve sefer sayısı).
4. `railTraceDive` 1 → 0,5.
5. `railStayFreeShifts` 2 → 3.
6. Enerji tavanı (yalnız birikme görülürse): yeni `liberationFundsMax` (b) 60. Kural: tavan gelir ve ödüllere uygulanır,
   iadeye uygulanmaz; defter gerçekten eklenen tutarı yazar; `IncomeBreakdown`'a kesilen tutar (`CapLoss`) eklenir ve fiş
   bunu "tavan" satırıyla gösterir (`Core/Simulation.cs:73-76`, `Core/Economy.cs:9`).
7. Kademe eşikleri bir basamak (uzunluk için).

**Elle sınama** (botun yapamadığı):

- bilinçli "hiç taşınma" harekâtı;
- grup beklerken acil sefer;
- dönüşte GERİ AL'ın okunurluğu ve kırma önizlemesi.

## 9. Uygulama aşamaları

Her aşama derlenir, EditMode testleri boş sahne açıkken geçer ve oyunda denenir (AGENTS.md kural 8). Sürüm aşama 1'de 18
olur. Aşamalar arasındaki geliştirme kayıtları geçersizdir. **Aşama 4 bir dur/devam kapısıdır:** büyük metin, model ve sahne
işi (aşama 5 ve sonrası) bu karara bağlıdır.

| # | Aşama | İçerik | Büyüklük | Bağımlılık | Bitti sayılır |
|---|---|---|---|---|---|
| 0 | Ölçüm tabanı | DengeBotu'na `TASI` iskeleti (`hic` dışında etkisiz), bugünkü kuralla 80 tohum | S | — | Taban tablo kayıtlı |
| 1 | Core · konum, İZ, KONUM | `RailState`, `RailDistance`, menzil denetimi, istasyon İZ'i, soğuma, sakin istasyon, KONUM eşiği, `AdvanceRail` iskeleti, bayraklar, sürüm 18 | M | 0 | Bayrak kapalı altın değerler aynen; yeni testler geçer |
| 2 | Core · sefer ve tahta | `rail_move` (`RunCommand`), `OrderRailMove`, `RailMoveCheck`, bedel, değiştirme ve iade, peron, acil sefer, `Replay.Apply`; dönüşte GERİ AL (`raidRecallInEscape`, `Recalled`), kırma önizlemesi okuyucusu; isteğe bağlı konvoy bayrağı | M | 1 | Gidiş-dönüş, defter ve kapanış sırası testleri geçer; aktarma ortasında kayıt ve devam (`Recalled` alanlı `raidres`) doğrulanır; yeni kimlikler sunumda eşlenir (`KosFinalReportTests`) |
| 3 | Denge | DengeBotu politikaları ve `RaidBot` GERİ AL kuralı, ölçüm, ayar | M | 2 | Bölüm 8 hedefleri ölçülmüş, sapmalar yazılmış |
| 4 | **Dikey dilim ve karar** | Bugünkü masada geçici bir makas plakası (KAL / YOLA ÇIK, hedef, bedel); İZ ve KONUM bugünkü İFŞA şeridinde; haritada istasyon İZ'i; bayrak açık; geliştiricinin elle oynadığı en az iki harekât. Dilim bugünkü odada oynanır; oradaki paket modelleri geçicidir ve aşama 7, 9 ve 9b'de sökülür | S–M | 3 | Açık bir dur/devam kararı yazılı; bozulan kararlar (bölüm 10.2) onaylanmış |
| 5 | Ad sözlüğü · metin | Core ve Bridge metinleri, son metinleri, bölge ve bina adları (tek şehir), robotun "uyanma" satırları, strings.txt İngilizcesi, CRT renk ve metin testleri. Eski yolun metinlerine dokunulmaz | L | 4 | `LocalizationTests` geçer; bölüm 2.1 listesine elle denetim |
| 6 | Tasarım turu | Claude Design brifi (komuta vagonu, bakım deposu menüsü, okuma masası); brif bölüm 2.6 YAPMA listesini aynen taşır. Paket modeli yoktur: bölüm 6.7'deki ~45–50 model (ilk tahmin ~11) OBJ + manifest olarak, bütçeyle tasarlanır; çıktı birkaç teslime bölünür (menü → kabuk → masa ve duvar nesneleri → ranza ve seyir köşesi; vagonda önce Rev3 tabanlılar) | dış iş (büyüdü, ~4 kat model) | 5'in ad sözlüğü | Onaylı ekranlar, pozlar, model listesi ve modeller; her model bütçe içinde ve bölüm 2.1'e karşı denetlenmiş; her ekran YAPMA listesine karşı insan gözüyle denetlenmiş |
| 7 | Sunum · komuta vagonu | Kabuk, hareket masası ve kol çerçevesi, seyir köşesi (makas kolu, enerji, ray dinleyicisi), ranza bölmesi, KS/KR pozları, anahtar rayı, istasyon levhası, ses (ray, makas, peron zili). **Yeni modellerin entegrasyonu** Rev3 model hattıyla (`Rev3Model`, `PartDriver`): masa, sefer yazıcısı, CRT kasası, telsiz, iki pano, sigorta panosu, vagon kapısı, peron zili lambası, masa lambası, kupa taşınmaz, yeni modelle değişir. **DeskGame'den paket sökümü:** 19 paket örneği ve kapalı Pekdata `CRTMonitor` nesnesi, yerine gelen yeni modelle aynı alt aşamada çıkar. Pakete kilitli testler aynı alt aşamada güncellenir | XL (Rev3 ölçeğinde; alt aşama sayısı arttı) | 4, 6 | Sefer oyunda emredilir, değiştirilir, vazgeçilir; `Left` ranzada görülür; DeskGame'de paket guid'ine başvuru kalmaz |
| 8 | Sunum · okuma masası ve baskın | Siyanotip pafta, ışıklı cam, ölü nokta işaretleri, kırma önizlemesi, geri sayım, piyon harfleri, HUD metinleri, aktarma geçişi | M | 6 | Bir aktarma baştan sona yeni dille oynanır |
| 9 | Sunum · menü | Bakım deposu (yeni modeller), yakın açılış pozu, dört seçenek, durumlar, geçiş. Kapı ve sigorta kutusu da yeni modeldir (aşama 7'deki `CarDoor` ve `FusePanel`); Giris'teki paket kapı ve elektrik kutusu sökülür | L (üst sınıra yakın) | 6, 7 (ortak modeller) | BR4'ün menü maddeleri ekran görüntüsüyle doğrulanır (TR + EN); Giris'te paket guid'ine başvuru kalmaz |
| 9b | Paket sökümü (depo temizliği) | Sahnelerden söküm aşama 7 ve 9'da biter; bu aşama geri kalanı temizler: paketten türetilmiş mesh'ler (`SceneMeshes/Door`, `PowerBox`, `Radio`, `KnockLight`); `Assets/LeartesStudios`, `Assets/Pekdata`, `Assets/Typewriter`, `Assets/Victorian_typewriter-aa00f0fe`; kökteki `MI_*` glTF dosyaları, `SM_office_desk_11.fbx`, `New Material*.mat`, `___www.unityassetcollection.com___.txt` (soru 18). Kalan pakete kilitli testler; paket anlatımı: `MIMARI.md` (üçüncü taraf oda modelleri ve teleks daktilosu maddeleri), `DURUM.md` ve `GELISTIRME.md` test tablosu (`TelexTypewriterSceneTests` satırı); `Assets/Standard Assets` için karar (bölüm 6.7, kapsam dışı) | M | 7, 9 (erken söküm seçilirse 4; soru 16) | `Assets/Corrigible` altında paket guid'ine başvuru yok (taramada Leartes `.meta` dosyalarındaki CRLF satır sonuna dikkat); türev mesh yok; derleme ve testler temiz; build boyutu ölçülmüş |
| 10 | Rehber, raporlar, belgeler | 26 adım + sefer adımları, son raporlar, başarım adları (Steamworks'te geliştirici), AGENTS, OT, MIMARI, DURUM, GDD | M | 3, 7, 8 | Rehber testleri (`PlanningTutorialPlayTests`) geçer |
| 11 | Eski yolun sökümü | Önce eski yolda koşan altın kayıt testleri (`LiberationCampaignTests` ve `LegacySimulation` kullanan 24 test dosyası) kartlı yola taşınır. Kartlı yolun kullandığı `ShelterLogic` parçaları (`ShelterLogic.Create`, `CampaignLiberation.cs:87`; `RadioWorkers`, `:389`) ayrılır. Sonra `planningBoardEnabled = false` yolu sökülür: `ShelterLogic`, `Door`, `DoorCards`, `shelter_upgrade/assign`, `door_answer` dalları, `vuruldu`/`sizma`, Madenci/Sis, bağlı sunum (`SiginakDoor` vb.), `DoorTests`, `ShelterTests`, `LegacySimulation`, yetim çeviriler | L | 4 (sunumdan bağımsız; 5–10 ile koşut olabilir) | Taşınan altın testler söküm öncesi ve sonrası aynı; derleme ve testler temiz; ~224+ çeviri satırı silindi |
| 12 | İkinci adım kuralları | Grup aktarmadan (`groupFromDive`), depo işareti; her biri ayrı ölçüm | M (her biri S–M) | 3, 8 | Her biri bayrakla ölçülmüş; açılanların sürümü yazılmış |

**Sahne işi.** Aşama 7–9b'de sahne değişiklikleri tek seferlik editör script'leriyle yapılır ve script'ler silinir. Paket
örneklerinin sökümü de bu yolla yapılır. Kalıcı kurucu yazılmaz (AGENTS.md kural 5). Bileşenler `[SerializeField]` ile
bağlanır.

## 10. Riskler, açık sorular, doğrulanmayanlar

### 10.1 Riskler

- **Kopya algısı.** Tür olarak Matrix (görevli, bağlanma, simülasyondan çıkarılanlar), *Sword Art Online* (bağlıyken ölüm),
  *Dark City* (uyuyan şehir), tren üssü olarak *Metro Exodus* (Aurora) ve *Snowpiercer*. Karşı önlemler:
  - bölüm 2.3'teki ayrışma eksenleri (kol çerçevesi, ampermetre, görünmeyen Arayıcılar, evinde uyuyan insan, bakım deposu);
  - taşıtın gerçek tipi: sivil savunma sıhhiye treni;
  - istasyon toplulukları yerine az sayıda gezici hat ekibi;
  - "uyanmak" fiilinin insanlar için kullanılmaması;
  - DÜN'ün kat planı olarak görünmesi, Bakıcı'nın düzeltilebilirlik çıkmazı, işçi ve sivil savunma dili, beyaz fosfor;
  - bölüm 2.1 listesi ve tasarım çıktısının bölüm 2.6'ya karşı denetimi;
  - vitrinde kâğıt, siyanotip plan ve kol çerçevesi.
- **Süre.** Zafer medyanı zaten 48 vardiya ve 35–45 hedefinin üstünde (OT:506). Gece seferi vardiya yemez, ama menzil ≤ 1,
  uzak cepheye durak durak gitmek ve enerji kıtlığı harekâtı uzatabilir. Bayrak, ölçüm olmadan varsayılan olarak açılmaz.
- **Enerji yine karar üretmeyebilir.** Bölüm 5.5'teki hesap kabadır. Semt ödülünün düşmesi semt bitirmenin hissini de
  azaltır; ölçüm bunu göstermezse aktarma bedeli tek başına oynanır.
- **Peron kuralı zayıf kalabilir.** Oyuncu yola çıkmadan önce birini seçebildiği için kayıp çoğu zaman bekleme süresidir.
  Güçlendirme (grubun aktarmadan sonra gelmesi) kapının çekim sırasını değiştirir; ikinci adıma bırakıldı.
- **Yeni oyuncu.** Hiç taşınmayan oyuncu ~7. vardiyada düşer. Rehber ve iki vardiyalık uyarı bunu yumuşatmazsa
  `railStayFreeShifts` ya da eşik oynanır.
- **Mekaniğin eğlencesi kanıtlanmadı.** Sefer kararı bot ölçümüyle dengelenebilir ama ilginç olduğu ancak elle oynanarak
  görülür. Bu yüzden aşama 4 bir dur/devam kapısıdır.
- **Kapsam.**
  - ~900–1200 metin satırı ve 26 adımlık rehber;
  - yeni kabuk, bakım deposu menüsü ve okuma masası;
  - iki yeni poz ve iki yeni istasyon;
  - ~45–50 yeni ya da yeniden tasarlanan model (bölüm 6.7);
  - komuta vagonu tek başına Rev3 ölçeğinde bir iştir (Rev3 on aşama sürdü).
- **Model sayısı ve iş yükü (yeni, 15 Kasım).** Paket modelleri kullanılmadığı için model listesi ~11'den ~45–50'ye çıktı.
  Claude Design çıktısının nesneler arasında tutarlı kalması ve her modelin Unity'ye alınması (manifest, parça adları,
  çapalar, poz ayarı) aşama 6 ve 7'yi uzatır. Önlem: teslimleri bölmek; menü BR4'ün öncelikli isteği olarak başta kalır, vagonda önce Rev3 tabanı olan ~20 modelle başlanır.
- **Tek model dili ve görsel kalite algısı (yeni, 15 Kasım).** BR4:89'daki iki model dili sorunu kapanır: oda tek dilde
  yeniden kurulur. Karşılığında fotogerçekçi paket nesnelerinin yerini sade, pahlı modeller alır; K1'in "zengin" görüntüsü
  bir süre geri gidebilir. Malzeme, yıpranma ve ışık dili bu yüzden önem kazanır. Kazanım: paket nesneleri bütçenin 1,75–9 katıydı
  (telsiz 7 006 üçgen, büyük bütçenin ~1,75 katı; pano 10 101, ~2,5 kat; daktilo 16 250, ~4 kat; masa lambası 7 413, küçük
  bütçenin ~9 katı; BR4:70-71); yeni modellerle K1'in üçgen yükü düşer, LOD grubu gerekmez.
- **Pakete kilitli testler (yeni, 15 Kasım).** 9 test dosyası paket adlarına ya da ölçülerine bağlı (bölüm 6.7). Söküm
  bunları günceller ya da siler; yanlışlıkla gevşetilen bir sınama sahne bozulmasını gizleyebilir.
- **Lisans (sökümle kapanır).** `Assets/___www.unityassetcollection.com___.txt` paketlerin bir koleksiyon sitesinden
  geldiğini gösteriyor. 15 Kasım kararıyla paketler kullanılmaz, bu yüzden "paketi koru" gerekçesi artık yoktur. Risk ancak
  paket örnekleri sahnelerden ve dosyalar depodan sökülünce (aşama 7, 9, 9b) kapanır; o zamana kadar sürer, aşama 4'ün
  dikey dilimi de paketli odada oynanır. Silinen dosyaların git geçmişinde kalmasının önemi hukuki bir sorudur (bölüm 10.3).
- **Çekim sırası kırılganlığı.** v18 bilinçli olarak RNG çekmez. İkinci adımın grup kuralı çeker. Ayrı ölçülmeli ve sürümü
  ayrı yazılmalıdır.
- **Okunurluk.** KONUM'un iki kısmı, istasyon İZ'leri, menzil ve peron uyarısı K1'den okunmazsa sefer kararı rastgele gelir.
  BR4'te İFŞA'nın K1'de okunmaması bugün de bir sorun (BR4:93,121).

### 10.2 Açık sorular (yalnız geliştirici cevaplayabilir)

1. **Ölçek:** DÜN tek şehir mi (öneri; 12 semt, bölge ve bina metinleri yeniden), yoksa dünya ölçeği mi? Dünya ölçeğinde
   metin daha az değişir ama metro ağı kıtalar arası tünele döner.
2. **Taşıtın adı:** "KATAR 07" Türkçede ülke adıyla (Katar) aynı yazılır. Kalsın mı? Seçenekler: "TREN 07", "SIHHİYE
   KATARI 07". Çağrı kodu K-07 hepsinde kalır.
3. **Ekran rengi:** beyaz fosfor (öneri) mi, kehribar mı? Kehribar seçilirse ayrılmış amber (dikkat) yeni bir renge taşınır.
4. **Konvoy bedeli:** YÜRÜYEN KAFİLE'nin bedeli alarm +2'den İZ +1'e taşınsın mı (`railEventsRebind`)?
5. **Enerji:** bayrak açık yapılandırmada semt ödülü 15 → 5 ve aktarma bedeli 12 → 14 kabul mü? Semt ödülünü korumak
   istenirse akışı yalnız aktarma bedeli taşır (12 → 16 dolayı).
6. **İkinci adım:** grup aktarmadan ve depo işareti v18 ile birlikte mi yapılsın, yoksa yayından sonra mı? Sonra yapılırsa
   grup kuralı sürüm 19 ister.
7. **Sert mod:** "0 can = ölüm" bayrağı (`zeroHpIsDeath`, varsayılan kapalı) istenir mi? Ayarlarda mı, harekât başında mı?
8. ~~**Lisans:** paketlerin lisansı ve satın alma kaydı var mı? Kabuk ve depo için satın alınacak bir paket mi, yeni model
   mi?~~ **Kapandı (15 Kasım 2026):** paket modelleri kullanılmaz, satın alma yok; bütün nesneler yeni modeldir (bölüm 6.7).
9. **Dönüşte GERİ AL:** dönüşte bedelle kol çekmek (şok, ikmal başarısız, düğümde İZ +1) doktrine uygun bulunuyor mu?
   Yoksa dönüşte kol kapalı kalsın ve kurgu "bina silinip basılırken devre kopmaz" mı olsun?
10. **Başarımlar:** kimlikler kalıyor. Steamworks'teki görünen adları (KATAR BULUNDU vb.) geliştirici mi yazacak?
11. **Oyuncu DÜN'e hiç girmez:** hareket memuru hep dışarıdadır. Bu, kullanıcının "konsoldan yönlendirir" fikriyle uyumlu
    görünüyor; onay?
12. **Doktrin ve zafer satırı:** afişe el yazısı ek ("BAĞLANACAKSAN ŞALTER ELİNDE OLSUN") ve zafer satırı onaylanıyor mu?
13. **Adlar:** HAREKET MEMURU / signalman, AKTARMA / TRANSFER, AYILAN / the Roused, SABAH / MORNING, BAKICI / the
    Attendant, DÜN / the Held Day, GECE HATTI / the Late Line, YANKILI / a Sleeptalker uygun mu? Meslek önerisi `tek` →
    Makasçı (yalnız metin) uygun mu?
14. **GDD:** Claude Docs'taki GDD bu belgeyle mi güncellensin, uygulama bitince mi?
15. **ONERILER Ö1/S3:** `gdspec/ONERILER.md` depoda yok. Ö1/S3 bir çıkış kuralı öneriyorsa, dar dönüş etkisiz bulunduğu için
    (bölüm 1) haritaya göre tanımlanan uzak ölü noktalar olarak, harita havuzu ve doğrulamasıyla birlikte yeniden
    tasarlanmalıdır. Ö1/S3 neydi?
16. **Söküm zamanı (15 Kasım kararından):** paket sökümü yeni modelleri mi bekler (aşama 7, 9, 9b), yoksa lisans riski
    yüzünden erken mi yapılır? Erken söküm için ucuz bir ara yol var: Rev3 karşılıkları (`Desk`, `Crt`, `Radio`, `CorkBoard`,
    `PlanningBoard`, `SettingsCabinet`, `DoorFrame`/`DoorLeaf`/`OuterWheel`, `KnockLamp`, `DeskLamp`, `Mug`, `Telex`) model
    hattında duruyor ve 13 Kasım'dan önce sahnedeydi; aşama 4'ten önce geri konabilir.
17. **Kabuk bütçesi:** Rev3 bütçesi nesne başınadır. Modüler kabuğun bir pozda görünen toplamı için üst sınır ne olsun
    (öneri ~24 000 üçgen)?
18. **Kullanılmayan indirilmiş dosyalar:** hiçbir sahnede başvurusu olmayan dosyalar (Victorian daktilo, kökteki `MI_*`
    glTF'ler, `SM_office_desk_11.fbx`, kökteki `New Material*.mat`, `unityassetcollection` notu) da aynı sökümde silinsin mi?
    Öneri: evet (aşama 9b). Modelsiz Unity su paketi `Assets/Standard Assets/Environment/Water` da kullanılmıyor; paket
    kararının kapsamı dışındadır, ama aynı temizlikte silinip silinmeyeceği sorulur.
19. **Telsiz mikrofonu:** yeni telsiz ahizesiz olur (YAPMA 5). Masa mikrofonu mu (Rev3 `Microphone`), bas-konuş el mikrofonu
    mu? El mikrofonu telefona benzememelidir.

**Bozulan önceki kararlar.** Dönüşüm, KOS §11'de onaylanmış şu kararları bozar ya da değiştirir. Her biri ayrı onay ister
(aşama 4'ün bitiş ölçütü):

| Karar (tarih) | Bugünkü hâli | Dönüşümle |
|---|---|---|
| Menü kapısı: odaya açılma, yazı, başlık, şeritler (14 Kasım) | "Böyle kalır" | Menü bakım deposuna taşınır; kapı katarın yan kapısı olur |
| Menüdeki ayarlar (14 Kasım) | "İki sahnedeki elektrik kutusuna AYARLAR plakası; oda ve menü aynı kalır" | Plaka kalır (SİGORTA KUTUSU / PANOSU); kutu yeni model olur (paket kutusu sökülür); oda ve menü değişir |
| Afişin yeri (14 Kasım) | "Son şalter afişin eski yerinde kalır; afiş yeniden yerleştirilir" | Afiş büyür ve vagonda yeni yer alır; cer şalteri seyir köşesinin yanına gelir |
| Telsiz ve cihazın kapıdan uzağa kaydırılması, kordon (14 Kasım) | 9,7 cm ölçülü yerleşim | Kabukla birlikte geçersiz; model de değişir (paket telsiz sökülür, yeni telsiz ahizesizdir), kordon kararı tümüyle düşer; vagonda yeniden yerleştirilir |
| Raf ve panel modeli (14 Kasım) | "Dört yuvalı raf ve panel şimdilik böyle kalır" | Panonun işlevi ve Rev3 parçaları kalır; gövde yeni model olur (paket gövde sökülür), yeri vagon duvarına taşınır |
| Gölgeli zaferin metni (14 Kasım) | "Son sunucu düştü. İçeride {ad} hâlâ dinliyordu." | "Son düğüm söndü. Koğuşta {ad} uykusunda hâlâ eski anonsu mırıldanıyordu." |
| Fiş etiketi (14 Kasım) | "SIĞINAK EKSİK" | "KATAR EKSİK" |
| İfşanın ara bedeli ve sorgu (13 Kasım, karar 5) | "Kalır" | **Korunur:** İFŞA ve ara bedeli aynen; İZ ayrı sayaçtır |

### 10.3 Doğrulanmayanlar

- **Enerji hesabı** (bölüm 5.5) kabadır. Aktarma sayısı (~40), ortalama geri alınmış semt (~5,5) ve sefer sayısı (~15)
  ölçülmedi, varsayıldı.
- **İZ ritmi** (bölüm 5.2) her vardiya aktarma varsayımıyla elle hesaplandı. Yaralı ve kitsiz vardiyalar ritmi yavaşlatır;
  soğuma hızı git-gel'i gerçekten bozuyor mu, ölçümle görülür.
- **Peron ve çekim sayısı:** varsayılan değerlerle peron boşaltmasının ek çekim yapmadığı akıl yürütmeyle bulundu; aşama
  2'de testle kilitlenir.
- **Sunum tarafı:** `RegionDef.Lon/Lat`'ın Core'da okunmadığı doğrulandı. Sunumda (`LiberationMapBuilder` vb.) nasıl
  kullanıldığı incelenmedi.
- **Dönüşte GERİ AL:** `RaidBot` ve sunum (`Baskin.unity` HUD) üzerindeki etkisi incelenmedi.
- **Marka taraması:** yeni İngilizce adlar (the Attendant, the Held Day, the Late Line, the Seekers, the Wardens, the
  Roused, a Sleeptalker, MORNING, TRAIN 07) için yapılmadı. Türkçe adlar (DÜN, GECE HATTI, BAKICI, KATAR 07) için bilinen bir
  çakışma görülmedi. Tarama yayından önce yaptırılmalı.
- **Hukuk:** bölüm 2.1 listesi bir avukat tarafından gözden geçirilmedi. Sökülen paket dosyalarının git geçmişinde kalmasının
  önemi de sorulmadı.
- **Paket sökümü:** envanter `.unity` ve `.meta` dosyalarında guid eşlemesiyle çıkarıldı, Unity'de açılarak doğrulanmadı.
  Sökümün build boyutuna etkisi ölçülmedi. Yeni model sayısı (~45–50) tasarım turundan önceki bir tahmindir.
- **Metin hacmi:** tahmin dosya başına sayımdan geldi; satır satır sınıflandırılmadı.
- **Bot:** taşınma politikaları henüz yazılmadı. Bölüm 8'deki hedefler ölçülmemiş beklentilerdir.
