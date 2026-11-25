# Tesis baskınları: üç odalı baskın, bölge YZ'leri, ekipman ve savunma

_2026-11-17 · Tasarım belgesi. Aynı gün ikinci ve üçüncü tur: açık sorular geliştiriciyle kapatıldı (bölüm 14). Aşama
0 (ölçüm tabanı) bitti: §11.6 "Taban ölçümü"; oyun kodu değişmedi. Güncel durum: aşama 0–18 uygulandı (22 Kasım 2026;
§12); aşama 18'den beri tesis oyunun tek baskın kipidir, ana bayrak `raidFacilityEnabled` ve eski tek tahta yolu silindi, sahadaki
sızmacı varsayılan açık, kayıt 21 (§14 yirmi birinci tur)._

**Tarihsel kayıtlar.** Aşama 0–17 bölümlerindeki "bayrak kapalı/açık", "asset'te kapalı", "tek tahta" ve "eski tahta" ifadeleri
o günün hâlini anlatır; aşama 18'den sonra ana bayrak yoktur. "Alt bayrak" ifadeleri (`facilityDefenseEnabled`, `facilityMarks`,
`facilityKatar` …) hâlâ geçerlidir.

**Durum:** Geliştiricinin 17 Kasım 2026'da seçtiği fikirlerin tek tasarımda birleşimi. Yalnız yayındaki kartlı yol
(`Balance.planningBoardEnabled = true`) kapsamdadır. Uygulanmış oyunu anlatan [OYUN-TASARIMI.md](OYUN-TASARIMI.md) bu
belgeyle değişmez; uygulama bittikçe oraya işlenir. Bu belge [bölge olayları](BOLGE-OLAYLARI.md) belgesinin ortak
kurallarını ve [aday seçimi, olaylar ve sonlar](KABUL-OLAYLAR-VE-SONLAR-TASARIMI.md) belgesinin gizlilik ilkesini
bölüm 8 dışında korur; [baskın mekânları](BASKIN-MEKANLARI.md) belgesindeki "sektör = oda" ad katmanının yerine geçer.

Sayılar "(b)" ile işaretli başlangıç değerleridir; ölçümle değişir. Kod yolları `Assets/Corrigible/` altındadır.
Geliştiricinin verdiği kararlar bölüm 14'ün başında, karar bekleyen sorular aynı bölümün devamında, kodda ya da
ölçümle doğrulanamayan varsayımlar bölüm 15'tedir.

## 1. Amaç ve kapsam

Bugün bir sektör tek baskın ve tek 8×8 tahtadır. Tahta sektöre bağlı değildir: harita baskın sırasına göre gelir
(`MapSlot = S.Raids`, `Core/CampaignLiberation.cs:210`), aynı sektörün ikinci denemesi başka haritada oynanır. Bölgeler
arasındaki fark bir özellik, bir imza makinesi ve bir olaydır. Baskından önce tahta hiçbir yerde görünmez. Karşı
saldırı oynanmaz: son alınan sektör kendiliğinden geri düşer. Altı tarifin hepsi baştan açıktır ve YZ oyuncunun
oynayışına hiçbir cevap vermez.

Bu belge altı değişiklik önerir:

1. **Tesis.** Her bölge ayrı bir YZ'nin tesisidir. Bölgenin üç sektörü tesisin üç kanadı olur; her kanadın kendine ait
   üç odası vardır. Her baskın bir kanadın üç odasından sırayla geçer; her oda ayrı bir 8×8 tahtadır. Bölgeler
   haritada YZ'nin adını taşır.
2. **Bina planı.** Odalar harekât başında tohumdan seçilir ve yerinde kalır. İstihbarat, baskından önce odaların
   planını masaya getirir. Robotlar ve niyetleri bugünkü gibi odaya girince görünür.
3. **Ekip.** Altı yeni temel hamle, sığınakta üretilen yedi ekipman ve yaptıkça gelişen karakterler.
4. **Karşı taraf.** On iki bağımsız YZ; her birinin kendi imza makinesi vardır. Sunucusu yıkılan YZ bir tarif ya da
   bir kazanım bırakır. Kalan YZ'ler oyuncunun en çok kullandığı araca önlem alır.
5. **Kalıcı tahta ve savunma.** Yıkılan ve örülen şeyler odada kalır. Karşı saldırı, alınan kanadın üç odasında,
   baskında oynanan aynı tahtalarda oynanır (bölüm 9.2).
6. **Tesis şartları.** İki yeni şart: vagonları ayrılan tren (Katar) ve yatan zemin (Yalpa).

| Kalkar | Yerine gelen | Aynen kalır |
|---|---|---|
| Sektör = tek tahta | Kanat = üç oda, üç tahta (bölüm 2, 3) | Bir baskın emri, bir vardiya, bir bedel (12 fon) |
| Baskın sırasına göre harita (`MapSlot`) | Kanada bağlı sabit odalar (`FacilityMaps`) | Harita üreticisinin altı türü ve yol, erişim kuralları |
| Emirde serpiştirilen duvar, kablo, nesne | Harekât başında odaya işlenir (plan sabit) | Kademe eklerinin, uyanan makinelerin ve niyet eşitliklerinin baskın tohumundan gelmesi |
| Otomatik karşı saldırı | Savunma baskını ya da bırakma (bölüm 9) | Alarm sayacı, eşik 10, güven −1 |
| Baştan açık altı tarif | Altısı açık kalır; yenileri sunucudan gelir (bölüm 5, 6) | Üretim hattı ve pano komutları |
| Düşenin otomatik kurtarılması | Sürükleme; taşınmayan düşen ölür (bölüm 4) | Taşınanın yatış süresi |
| Bölge adları (üretilmiş yer adları) | Bölge, YZ'nin İngilizce marka adını taşır (bölüm 6.1) | Üretilmiş haritanın kıtaları ve bağlantıları |
| Sığınak sakinlerinin Türkçe adları | İngilizce özel isimler (bölüm 11.4) | Ad çekim sırası ve sayısı |
| Altı imza makinesi ikişer bölgede | On iki YZ'ye on iki imza makinesi (bölüm 6.5) | Bugünkü altı makinenin kuralları |
| Otomatik üretimin tarifi girdiden tanıması | Tarif defterden seçilir (bölüm 5.6) | Üretici görevi (süre artık emir yolundaki tarif süresidir) |
| "Sektör = binanın bir odası" adı | "Sektör = kanat, oda = pafta n/3" | Bina adı, bina notu, üç sektör adı |

**Uygulanan hâl (aşama 16).** Tek tahta kipinde harita baskın sırasına göre torbadan gelir. Görev türünün ek turu geri dönüşü mümkün kılar (aşama 18'de tek tahta, torba ve ek turlar silindi). Tesis kipinde ise kanat ve ziyaret yönü oda planını belirler, oda veya kuşatma kendi tur bütçesini kullanır. Her iki kip kayıttan açılabilir. G5 haritası aynı YZ markasını ekranda ve harita izinde kullanır. Eski yer adı havuzu için kalan kıyı bölgesi sınıflandırması, ad karıştırıcısı ve ikinci ad okuyucusu kaldırıldı. Kablo uçlarını belirleyen kıyı hücreleri ve haritanın çizdiği kıyı çizgileri çalışmaya devam eder.

**Alınmayanlar:** "Sessizlik" (düşen kişinin durumunun gizlenmesi; geliştirici kararıyla çıktı), robotların ve
niyetlerin gizlenmesi, raporların yanıltması, ekibin iki odaya bölünmesi, haritada yer değiştiren tesis, ikmal
kanatlarının sunucu kanadını kalıcı olarak zayıflatması, seçilmeyen sunucu ödülünün sonradan alınması.

**İlkeler.**

- **Görünür niyet ve geri alma bugünkü gibidir.** Odaya girildiği anda bütün robotlar ve niyetleri görünür. Hamle geri
  alma tur içinde çalışır. Hiçbir yeni kural gösterilen niyeti gizlemez ya da yanıltmaz.
- **İstihbarat yalnız bina planıdır:** yapı (duvar, kapı, menfez, kablo, donanım), sabit nesneler ve tesis şartının
  parçaları. Robot, niyet ve robot sayısı göstermez.
- **Yan hedefi yapmamak ceza değildir.** Hazırlık donanımına dokunmayan ekip bugünkü zorlukta bir hedef odasıyla
  karşılaşır; dokunan kolaylık kazanır.
- **Hiçbir önlem bir aracı tamamen geçersiz kılmaz.** Önlem aracın bir bölümünü kapatır ve cevabı oyunda bulunur.
- **Kural tarafa bakmaz.** Kablo, itme, kayma ve açık kenar ekibe de makineye de işler (`BOLGE-OLAYLARI.md` ortak
  kuralları); yalnız açık kenardan düşmenin sonucu farklıdır (makine yok olur, ekip üyesi ölmez, 5 vardiya yatar;
  bölüm 10.1). Olay hasarı tavanı ve "önce işaret, sonra olay" sırası yeni şartlar için de geçerlidir.
- **Gizli kimlik sunuma sızmaz.** Sızmacının sahadaki davranışı yalnız `internal` alanlardan okunur (bölüm 8).
- **Determinizm.** Yeni hiçbir kural `S.Rng` çekmez. Oda üretimi ayrı akıştan (`Rng.Mix`) gelir; oyuncu hamleleri ve
  yeni komutlar RNG tüketmez.
- **Bayrak.** Bütün kural değişiklikleri `Balance` bayraklarının arkasındadır. Bayraklar kapalıyken Core bugünkü
  sonucu verir; kilitli altın değerler (`RaidEngineTests`, `TutorialRaid`) kaymaz. İstisna: bölge kimliği verisi (YZ
  adı, Katar ve Yalpa'nın tesis adları), sakin ad listeleri ve harita üreticisi statik
  veridir, bayrak arkasında değildir; aşama 3, 12 ve 13'te değişir, harita üreticisi sürümü aşama 13'te artar
  (bölüm 11.4).
- **Denge değerleri düz alandır.** `Balance.Clone()` sığ kopyadır; liste ya da tablo eklenmez. Oda, ekipman ve önlem
  tabloları Core'da statik veridir, sayıları `Balance`'tan okunur.

## 2. Tesis, kanat ve oda

### 2.1 Terimler

| Terim | Anlam | Bugünkü karşılığı |
|---|---|---|
| Tesis | Bir bölgedeki YZ'nin binası | `RegionDef.Building` |
| Kanat | Tesisin üç bölümünden biri; bir baskının hedefi | Sektör (`LiberationSectorState`), adı `RegionDef.Sector(i)` |
| Oda | Kanadın üç tahtasından biri | Yok (bugün sektörün sabit tahtası yoktur) |
| Giriş | Tesisin dış kapısı: giriş odasının batı kenarındaki üç kare | Bugünkü `E` kareleri |
| Geçit | İki odayı bağlayan kapı: odanın batı kenarındaki giriş geçidi ve doğu yarısındaki ileri geçit | Yok |
| Donanım | Giriş ve ara odadaki hazırlık hedefi (pano, anten, vana) | Yok |
| Menfez | Havalandırma ızgarası; oda içinde çift olarak bulunur | Yok |

Sektör kimlikleri (`weu_1`), sektör adları ve "01, 02 ikmal; 03 sunucu" sırası değişmez. Baskın ekranındaki
"OZALİT KOPYA · PAFTA n/3" anteti (`RaidBlueprintView`) bugün üç sektörden birini işaretler; yeni anlamı kanadın üç
odasından biridir. Sektörün sırası antette ayrıca yazılır: "KANAT 02 · PAFTA 1/3".

### 2.2 Oda tipleri

| Tip | Sıra | Giriş | İleri geçit | Amaç | İçerik (b) |
|---|---|---|---|---|---|
| Giriş (`G`) | 1 | Batı kenarında üç `E` | Doğu yarısında iki bitişik geçit karesi | İçeri girmek, dönüş yolunu kurmak | 1 makine (bakım robotu; aşama 4'e kadar 2), 1 sivil sunucu, 1 donanım, kopya dolabı yok |
| Ara (`A`) | 2 | Batı kenarında iki bitişik geçit karesi | Doğu yarısında iki bitişik geçit karesi | Yol seçmek | 1 makine (bakım robotu; aşama 4'e kadar 3), 2 sivil sunucu, 1 donanım, 1 kopya dolabı, bir menfez çifti, bir ağır kepenk |
| Hedef (`H`) | 3 | Batı kenarında iki bitişik geçit karesi | Yok | İkmal kasası ya da sunucu | Bugünkü tahtanın düzeni: hedef dolap, 2 kopya dolabı, 2 soğutma; sivil sunucu 3'e iner (bugün 4–5); kademe ve imza makineleri |

"Giriş hep batıda" kuralı üç tipte de korunur; motorun buna dayanan yerleri (ek makinelerin `x ≥ 4` yarısına konması,
kopya dolabının uzak yarıda olması) değişmez. İleri geçit yalnız doğu yarısındaki bir kenar karesi olabilir.

Ara odanın üç yolu vardır:

- **Geçit:** açık yol; makinelerin tuttuğu yoldur.
- **Menfez:** yalnız Analist geçer (bölüm 4). Çiftin öbür ucu geçide ya da donanıma yakın çıkar.
- **Ağır kepenk:** iki kişi birlikte kaldırır (bölüm 4). Geçide kestirme açar.

### 2.3 Odaların seçimi

- Odalar harekât başında üretilir ve harekât boyunca değişmez: 12 bölge × 3 kanat × 3 oda = 108 oda.
- Oda, haritadaki düğüme değil bölge paketine bağlıdır (`RegionDef.Id` + kanat sırası + oda sırası). Üretilmiş harita
  paketi hangi düğüme koyarsa tesis oradadır.
- Üretim `FacilityMaps` içindedir ve ayrı akıştan beslenir: `Rng.Mix(S.Seed, 0x54455349)`. `S.Rng` tüketilmez; kayda
  yalnız parmak izi yazılır (bölüm 11.4).
- Üretici bugünkü `RaidMaps.Generate`'in genişlemesidir: altı tür (`koridor`, `oda`, `serit`, `salon`, `sebeke`,
  `rehine`) kalır; tür, oda tipine göre giydirilir (geçit, donanım, menfez, kepenk). Geçerlilik kuralına şunlar
  eklenir: girişten ileri geçide yol, girişten donanımın yanına yol, menfez çiftinin iki ucu da zemin, kepenk
  kapalıyken de geçide yol, ileri geçitten batı girişine en kısa yol en çok 8 adım (b). Bu sınır dönüş
  süresini (4 tur) ancak kısmen karşılar: yürüyüşü 3 olan kişi 3 turda, yürüyüşü 2 olan ya da yürüyüşü 3 ile sürükleyen
  tam 4 turda yetişir; yürüyüşü 2 ile sürükleyen yetişemez (risk, bölüm 13).
- **Bölme kepengi (aşama 1).** Ara odada var olan duvarda kazançlı (açılınca yolu en az 2 adım kısaltan) kepenk yeri
  taslakların çok azında çıkar. Bu yüzden kepenk çoğunlukla bölmedir: geçide giden en kısa yolun x 2–6 arasındaki bir
  zemin karesine duvar–kepenk–duvar çizgisi kurulur. Bölme kepenginde kapalıyken yol en çok 10 adım, açıkken en çok 8
  adımdır (`FacilityMaps.GateClosedMaxSteps`); var olan duvardaki kepenkte ikisi de 8'dir. 200 tohumda ara odaların
  %96'sı bölme kepengi taşır. Kapalı yolun 10 adımı dönüş bütçesini zorlar (bölüm 13); geliştirici onayladı (§14, dördüncü tur).
- Bugünkü sayım kuralları (tek hedef, 2 soğutma, 2 kopya dolabı, 4–5 sivil, 2 dron, 1 bakım robotu) oda tipine göre
  2.2'deki içerikle değişir.
- **Plan ile robot ayrılır.** Bugün bölge özelliğinin duvar, kablo ve nesne serpiştirmesi baskın emrinde, baskın
  tohumuyla yapılır (`Baskin.ApplyRegionTraits`, `Core/Baskin.cs:283`). Yeni düzende bu serpiştirme oda üretimine
  taşınır; plan harekât başında kesinleşir. Baskın tohumu (`RaidParams.Seed`, emirde `S.Rng`'den tek çekim) yalnız
  makinelerin yerini, türünü ve niyet eşitliklerini belirler. Aynı odaya ikinci girişte plan aynı, makineler başkadır.
- Bugün başlangıçtaki 2 dron ve 1 bakım robotu harita satırlarındadır (`d`, `m`; `RaidMaps.Generate`) ve harekât
  tohumundan gelir. Tesis kipinde bu harfler oda satırlarından çıkar; başlangıç makineleri oda kurulumunda oda
  tohumuyla yerleştirilir. `RaidMaps.Valid`'in makineye bağlı kuralları (makineye girişten yol, dronun girişe açık
  atış hattının olmaması, bakım robotunun hedefe yakınlığı) bu yerleştirmede sınanır.
- Oda adı türün ad havuzundan gelir (`RaidMaps` adları); kanat adı bugünkü sektör adıdır.
- **Büyük sandığın nişi (aşama 6).** İkmal kanadının hedef odasında büyük sandık (bölüm 4.5) bir duvar karesinin bayrağıdır
  (`Cell.Crate`, `FacilityRoom.BigCrate`; `T = "wall"` kalır). Plan kurulduktan sonra, son denetimden önce RNG'siz seçilir
  (`Baskin.PlaceBigCrate`): üç hâlde (şartsız, kademe 1–2, kademe 3) de duvar olan, kepenk ve raf rayı olmayan, sekiz
  komşusundan en az ikisi iş karesi (her hâlde geçilebilir, geçit ve menfez değil, kademe 3 parçası değil, batı girişine
  bağlı) olan kareler arasından hedef dolaba Manhattan uzaklığı, sonra y, sonra x en küçük olan. Satırlar, özet (Digest),
  robot ve kadro yerleşimi değişmez (duvar zaten kapalıydı). Tohum 79 ve 1–10'da ikmal hedef odalarının hepsinde (24/24)
  yer bulundu. Üretici sürümü 2 (`FacilityMaps.GeneratorVersion`).

### 2.4 Tesis şartı

Her YZ'nin tesisi bir şart taşır. Şart, bugünkü bölge olayının yeni adıdır: 12 bölgenin 12 olayı (`bulut`, `raf`,
`gaz`, `tavan`, `asansor`, `kriyo`, `turnike`, `kaynak`, `faz`, `suzgec`, `tup`, `yag`) zaten bölgeye özgü, tur tur
işleyen kurallardır. İki paketin şartı yeni kuralla değişir: Katar ve Yalpa (bölüm 10).

- Şart ara odada ve hedef odada kurulur; giriş odası şartsızdır (b). Giriş sade kalır, şart içeride öğrenilir.
- **Şartın parçaları odanın planındadır.** Bugün olay parçaları (tüp, kaynak kolu, raf bloğu, turnike, süzgeç, çatlak,
  meme, gaz sızıntısı ve bulut alanı, kriyo bölgesi, kablo kanallarının fazı) baskın kurulumunda, makineler
  yerleştikten sonra konur (`EventSetup`). Tesis kipinde şart kurulumu oda üretimine taşınır: parçalar harekât başında
  odaya işlenir ve bina planında görünür. Oda kurulumunda makineler şart karelerine konmaz. Olay kurulumu bugün de RNG
  çekmez; taşınması ana sırayı değiştirmez.
- Parçalar geçide, menfeze ve donanımın tek boş komşu karesine konmaz; oda, parçalar konduktan sonra 2.3'ün geçerlilik
  kurallarından (8 adım sınırı dahil) geçer.
- Kademeye göre değişen parça sayısı (bugün bazı olaylarda 3 yerine 4) için odada en yüksek sayıyla yer ayrılır.
  Planda yalnız o anki kademede kurulan parçalar çizilir; ayrılan boş yer çizilmez. Kademe değişince bilinen odanın
  paftası güncellenir (b).
- Her oda kendi tur sayacını 1'den başlatır. Takvimler odanın tur bütçesine bağlıdır ve son turda olay adımı yoktur
  (`Turn < MaxTurns`): 6 turluk odada 6. tura ve sonrasına bağlı adımlar düşer (süzgecin 6. tur emişi gibi; 2026-11-24'e dek
  odalar 5 turdu ve rafın 5. tur kayması da düşüyordu). Her olayın oda başına takvimi aşama 1'de gözden geçirilir ve `RaidEventTests` ile kilitlenir.

## 3. Baskının akışı

### 3.1 Emir ve sıra

Emir bugünkü gibidir: `StartLiberationRaid(sectorId)`, 12 fon, vardiyada tek baskın, sektör sırası atlanamaz. Baskın
sırayla giriş, ara ve hedef odasında oynanır. Aynı anda tek oda oynanır; ekip bölünmez.

### 3.2 Tur bütçesi

| Oda | Alan | Tur (b) | Süre dolunca |
|---|---|---|---|
| Giriş | `facilityEntryTurns` | 6 (sürüm 10 telafisi, 2026-11-24; önce 5) | Kapıda bekleyen varsa onlarla geçilir, varamayan geri çekilir (sürüm 10); kapıda kimse yoksa baskın başarısız: ekip geri çekilmiş sayılır |
| Ara | `facilityMiddleTurns` | 6 (sürüm 10 telafisi; önce 5) | Giriş gibi (sürüm 10) |
| Hedef | `facilityTargetTurns` | 10 (sürüm 10 telafisi, 2026-11-24; tek kip denge turunda 9, önce 8) | Bugünkü kural: hedef kırılmadıysa kayıp; kaçıştaysa tahtada kalan geride kalır |
| Dönüşte temiz olmayan oda | `facilityReturnTurns` | 6 (sürüm 10 telafisi; aşama 4'ten beri 5, önce 4) | Tahtada kalan geride kalır |

"Kısa nöbet" özelliği (`kisa`) yalnız hedef odasının ilk ziyaretini bir tur kısaltır (b). Hedef odası başta 8 turdu: o günkü
kampanya bütçesi (6 + 2 ek tur; ikmal ölçümü: 6 turda %45, 8 turda %80; `Docs/DURUM.md`). Tek kip denge turunda (2026-11-22,
§11.6 "Tek kip denge ölçümü") 9'a çıktı: başarısızlığın çoğu süre dolmasıydı, hedef odada kaçışın süresi dolunca geride kalan ölüyordu.
Sürüm 10'un telafi turunda (2026-11-24, §14.x "Telafi varyantları", `g_tur6h10`) giriş, ara ve dönüşle birlikte 10'a çıktı (kısa nöbette
ilk ziyaret 9).
`liberationLootExtraTurns` ve `liberationServerExtraTurns` tesis kipinde okunmaz; önceki odalardan dönüş ayrı sayılır.

Giriş ve ara odada süre dolması geri çekilme gibidir: ayaktakiler döner, yerde yatanlar geride kalır (bölüm 4.3).

**Sürüm 10 (2026-11-23; geliştirici: "ekibin tamamı sonraki odaya geçmezse baskın sonlanmasın, geçenlerle devam etsin").**
Giriş ya da ara odada süre dolarken kapıda en az bir uzman bekliyorsa baskın bitmez: oda kapıdakilerle geçilir. Kapıya
varamayan ayaktaki uzman sağ geri çekilir (`Unit.Withdrew`, oda raporunda `"w"`). Canını korur, sonraki odalara girmez,
baskının sonucunda düşmüş sayılmaz ve sığınağa canıyla döner. Kapıda kimse yoksa eski kural geçerlidir: ekip geri çekilir,
baskın başarısız biter. Son tur satırı ve son tur onayı ("SON TUR · KAPIDAKİLER GEÇER") bunu söyler. Hedef odası, kaçış ve
dönüş değişmedi: orada süre dolarken tahtada kalan geride kalır. GEÇ yine bütün ayaktaki ekip kapıdayken verilir; erken,
eksik geçiş yoktur.

### 3.3 Oda geçişi

1. Geçit karesine giren uzman kapıda bekler (tahtadan kalkar, hedef alınamaz). Kapıdaki uzman asansör kabinindeki gibi
   "tahta dışı ama baskında" sayılır (`CheckEscape`'teki `Cab` istisnası); yoksa son uzman kapıya girdiği anda baskın
   "ekip düştü" diye biterdi.
2. Ayaktaki bütün uzmanlar kapıdaysa oyuncu **GEÇ** komutunu verir. Komut verilene kadar tur içi geri alma çalışır.
   Köpek ve ele geçirilmiş robot nerede olursa olsun ekiple geçer (b). Kapıya giren uzman kapıdan geri çıkamaz; onu
   yalnız tur içi geri alma geri getirir. Tahtadaki son uzman düşerse kapıdakiler kendiliğinden geçer; kaçışta süre
   dolarsa da kapıdakiler geçer, tahtada kalan geride kalır (aşama 2 kararı, §14 beşinci tur).
3. GEÇ geri alınamaz: yeni odanın robotlarını açtığı için bedava keşif olurdu.
4. Ekip yeni odanın giriş geçidine dizilir: kadro sırasıyla uzmanlar, sonra köpek, ele geçirilmiş robot ve sürüklenen
   düşen; önce geçit kareleri, yer kalmazsa geçide en yakın boş zemin karesi (eşitlikte küçük y, sonra küçük x).
   Bugünkü kurulum kadroyu giriş karesi sayısı kadar kurar; bu sınır tesis kipinde kalkar. Dönüşte doğu geçidinden
   girişte de aynı kural uygulanır; geçit karesinde duran makine en yakın boş kareye kaydırılır (b). Telsiz fişi
   girişi "gözcü raporu" olarak yazar (bölüm 7.3).
5. Ekibin çıktığı oda donar: içinde tur işlemez, makineler yerinde ve aynı canla bekler (Katar'ın takip kuralı ve
   savunma baskını hariç; bölüm 10.1, 9.2).
6. Kapıda bekleme uzmanın kendi yürüyüşüyle olur. İtilen, çekilen ya da kayan birim (uzman, makine, köpek, düşen kişi)
   geçit, giriş ve menfez karesinde durur: kapıda sayılmaz, menfezden geçmez, hedef alınabilir. O karedeki uzman kendi
   turunda adım harcamadan kapıya geçebilir (b).

**Odadan odaya taşınanlar:** can, zırh plakası, harcanan EMP hücresi, ekipman hakları, köpek, ele geçirilmiş robotun
kalan turu ve (dönüşte) yük. Kilit ve donma etkileri geçişte kalkar.

**Geride bırakılanlar:** düşen ve sürüklenmeyen kişi düştüğü karede kalır (bölüm 4.3); kurulu aygıtlar odada kalır.

### 3.4 Hazırlık: ilk iki oda hedef odasını hazırlar

Giriş ve ara odanın her birinde bir donanım vardır. Donanım bir nesnedir; kırılır ya da Teknisyen sabotajıyla
kapatılır. Etkisi o baskının hedef odasında görülür.

| Donanım | Kimlik | Oda | Can (b) | Etki (b) |
|---|---|---|---|---|
| Sigorta panosu | `pano` | Giriş | 2 | Hedef odada kopya dolapları ilk 2 tur makine uyandırmaz |
| Çağrı anteni | `anten` | Giriş ya da ara | 2 | Hedef odaya kademe makinesi 1 eksik gelir; dönüşte geçilen odalarda uyanma olmaz |
| Soğutma vanası | `vana` | Ara | 1; yalnız Teknisyen kapatır | Sunucuda hedef dolap 2 can eksik başlar; ikmalde hedef odadaki makineler ilk tur yürüyemez |

- Giriş odasında `pano` ya da `anten`, ara odada `vana` ya da `anten` bulunur; hangisi olduğu oda üretiminde belli
  olur ve planda görünür. Aynı kanatta `anten` iki kez gelmez.
- Ara odada ele geçirilen robot hedef odaya götürülür (bölüm 5.4); bu da bir hazırlıktır.
- Donanım kalıcı tahtanın parçasıdır: kırılan donanım, YZ onarana kadar sonraki baskında da kırıktır (bölüm 9).

### 3.5 Dönüş

Kaçış bugünkü gibi başlar: ikmalde bir uzman kasayı açınca (sürüm 10: her uzman açar; önce yalnız Teknisyen), sunucuda son hedef kırılınca. Çıkış geldiğin odalardan
geçer: hedef odasının batı geçidi, ara oda, giriş odası, tesisin girişi.

- **Temiz oda oynanmaz.** İçinde ayakta makine ve sağlam kopya dolabı olmayan oda yürünerek geçilir; tur harcanmaz,
  odada bırakılmış düşen kişi yolda alınır (taşınmış sayılır).
- **Temiz olmayan oda oynanır.** Ekip doğu geçidinden girer, batı çıkışına yürür; süre `facilityReturnTurns`.
  Makineler ekibin bıraktığı yerde ve canda bekler, girişte yeni niyet seçer. Örülmüş engeller ve kurulu kalmış
  aygıtlar yerindedir. Batı çıkışı da kapıda bekleme ve **GEÇ** ile geçilir; odanın tesis şartı plandan baştan başlar
  (aşama 2 kararı).
- İkmalde baskın, yük tesisin girişinden çıkarsa kazanılır. Sunucuda hedef kırıldığı için baskın kazanılmıştır;
  dönüş yalnız kimin döndüğünü belirler.

### 3.6 Geri çekilme, geri alma ve kayıt

- **Geri çekilme** bugünkü gibi anlıktır ve yalnız kaçıştan önce verilir. Hangi odada verilirse verilsin ayaktakiler
  döner; yerde yatanlar geride kalır ve ölür (bölüm 4.3). Düşeni kurtarmanın tek yolu taşımaktır.
- **Geri alma** yalnız içinde bulunulan turun hamleleri içindir. Oda geçişi ve tur sonu geri alınmaz.
- **Baskının ortasında çıkış.** Bugün süren baskın devamda baştan kurulur. Tesis kipinde her oda geçişi ve baskının
  bittiği oda kayda yazılır (`raid_room`, bölüm 11.3); devam eden oyuncu baskına içinde bulunduğu odanın başından
  döner.

## 4. Yeni temel hamleler

Bugün her uzman turda bir kez yürür ve bir kez yeteneğini kullanır; yetenekten sonra yürünmez (`DoMove`, `DoAct`).
Yeni hamleler ekipman istemez. Aksi yazılmadıkça yeteneğin yerine kullanılır.

**Uygulanan hâl (aşama 6): eylem API'si.**

- **Yer:** `Core/Baskin.Actions.cs` (`Baskin`'in `partial` parçası, `UnityEngine` yok). Eski API (`Reachable`, `Targets`,
  `CanMove`, `CanAct`, `DoMove`, `DoAct`, `Preview`) aynen kalır ve "uzmanın yeteneği" demektir; yeni hamleler yalnız yeni
  API'den girer.
  - Kimlikler: `pusu`, `kosu`, `surukle`, `firlat`, `ortak`, `menfez`. Sabit sıra `ActionKinds`; bot adayları ve sunum tuşları
    bu sırayı izler.
  - `CanAction(B, u, kind)`: önkoşul, hedeften bağımsız.
  - `ActionTargets(B, u, kind, other)`: hedef kareler; `other` sürüklenen gövdenin birim kimliğidir (yalnız `surukle`).
  - `DoAction(B, uid, kind, x, y, other)`: doğrular, `Snapshot` alır, uygular, bedeli yazar, `CheckEscape`. Geçersizse false
    döner ve durum değişmez.
  - `PreviewAction(...)`: kopyada aynı uygulama. Geçersiz eylemde null döner. Biçim `Preview`'ınkiyle aynıdır;
    `PreviewResult`'a sona `Line` (pusu hattı), `Opens` (iki kişilik iş bu hamlede açıyor) ve `Carrier` (hamleden sonraki
    taşıyıcı) eklendi. `Preview`'ın gövdesi saf `Diff(B, C)`'ye taşındı, çıktısı birebir aynı (`Preview_Unchanged`).
  - Yardımcılar: `DragBodies`, `WalkPath`, `AmbushLine`, `IsBody`, `LyingBody`.
- **Bedeller** (`CostOf`):

  | Hamle | Önkoşul | Sonra |
  |---|---|---|
  | `pusu`, `firlat`, `ortak` | `CanAct` (uzman önce yürümüş olabilir) | Yürüyüş ve yetenek biter |
  | `kosu`, `surukle` | `CanMove` | Yürüyüş ve yetenek biter |
  | `menfez` | `CanMove`, yalnız Analist | Yürüyüş biter, yetenek kullanılabilir kalır |

  Kilitli ya da donmuş uzman yürüyemez: `kosu`, `surukle`, `menfez` verilemez; öbür üçü verilebilir. Kabindeki ve kapıdaki
  uzman tahtada değildir, hiçbir hamle veremez.
- **Değerler:** `Balance` → `MoveRules.From(b)` → `RoomSetup.Moves` → `BState.Moves`. Alanlar ve değerleri: `facilityAmbushRange`
  4, `facilityRushRun` 3, `facilityRushPush` 2, `facilityThrowRange` 3, `facilityDragMoveCost` 1, `facilityBigCrateMetal` 2,
  `facilityBigCrateCircuits` 1, `facilityLastBreathDies` false (§4.3 "son nefes"; yalnız ölçüm anahtarı). Hiçbiri
  `RaidParams`'a girmez (orta baskın JSON'u değişmez); asset'te yazılı değiller, C# varsayılanıyla çalışırlar.
- **Sınırlar:**
  - Her kural `B.Room != null` arkasındadır. Eski tahtada `CanAction` false döner, yeni alanlar varsayılanında kalır,
    `Signature`, `RoomReport.Encode` ve `RaidResult.Encode` eski dizgiyi verir.
  - Hiçbir hamle RNG çekmez. Tek dolaylı etki: pusunun düşürdüğü makine uyanma yeri sayısını değiştirebilir.
  - Makine aklı (`PlanEnemies`, `ScoreTarget`, `Intents`) değişmedi: pusu hattını, gövdeyi, yerdeki yükü ve tutuşu görmez.
  - Tur içi durumun hepsi `BState`, `Unit` ve `Cell` içindedir; geri alma bunları bedava getirir. Her hamlenin geri alma testi
    yansımalı tam döküm ve imzayla yapılır (`FacilityKit.AssertUndoRestores`).
- **Sunum:** aşama 7 (aşağıda). Aşama 6 sahneye ve prefab'a dokunmamıştı.

**Uygulanan hâl (aşama 7): sunum.** Altı hamle oyunda verilir. Sahne ve prefab değişmedi; hamleler mevcut HUD'a çalışma
anında bağlanır. Metinler, seçenekler ve önizleme verisi saf Bridge sınıflarındadır (`FacilityMoveText`, `FacilityMoveView`).
Core, `BalanceConfig.asset` ve `SoundBank.asset` değişmedi; yeni komut yok, sürüm 19; bayrak kapalıyken baskın sunumu
eskisinin aynısıdır. Kararlar §14 onuncu tur.

- **Giriş ve hamle kipi:**
  - Tek tuş **E**. E, seçili uzmanın kullanılabilir hamlelerinde `ActionKinds` sırasıyla döner; sürüklemede her gövde ayrı
    seçenektir. Sonuncudan sonra hamle kipi kapanır.
  - Kart satırına tıklamak o hamleyi doğrudan kurar; kurulu satıra yeniden tıklamak kipi kapatır.
  - Kipi kapatanlar: Q, Esc, sağ tık, seçim değişimi, Z (geri alma) ve başarılı hamle. Q kuruluyken E yeteneği kapatır.
  - Rehberli eğitim baskınında E etkisizdir.
  - Tık sırası: önce hedef kare; sürüklemede başka bir gövdenin karesi gövdeyi değiştirir; ayaktaki ekip üyesine tık onu seçer.
    Bir gövdenin karesi aynı zamanda hedefse tık hamleyi verir.
- **Yetenek kartının satırları:**
  - Hamle satırları prefab'da yoktur; yetenek satırının (Row 1) çalışma anı kopyalarıdır. Plaka satırından (Row 2) sonra,
    alt düğmelerden önce durur ve kardeş sırası yoluyla bağlanır. Eski tahtada hiç kurulmaz.
  - Yalnız bu odada anlamlı hamleler listelenir. Fırlatma yalnız taşıyıcıda; menfez yalnız Analist'te ve menfezli odada;
    ortak yalnız kapalı kepenk ya da sandık nişi varken; sürükleme yalnız yatan gövde varken.
  - Kullanılamayan satır soluktur; ayrıntısında nedeni yazar: "yetenek kullanıldı", "yürüdü · yürüyüş ister", "bitişikte iş
    yok" vb. Ayrıntı bedel ile değeri birlikte taşır ("yetenek yerine · hat 4 kare"); ayrı bir bedel etiketi yoktur.
  - E fişi yalnız E'nin kuracağı satırda (`ActionKey`) görünür.
  - En kötü durum ölçüldü (3 + 6 satır, TR ve EN): kartın tepesi 770 px, durum panelinin altı 837 px (1080 başvuru). Katlanan
    "Diğer" satırına gerek kalmadı.
- **Hedef ve önizleme:**
  - Kipte hedef kareler çerçevelenir; yürüyüş boyası kapanır.
  - Üzerine gelinen hedefte önizleme çizilir: pusu hattı, koşu yolu ve itme okları; sürüklemede iki hayalet (sürükleyen ve
    gövde); fırlatmada atandan alıcıya ok; menfezde kesik kavis.
  - Çizgi tonları: "good" `lib.good`, "ink" grafit (`lib.ink` kâğıt tahtada okunmuyor), "graphite" grafit α .7, "threat"
    `lib.threat`.
  - Hover kartında önce hamle önizleme satırları gelir (5 satır sınırı): "Sandık açılır: kazanılırsa +2 metal +1 devre",
    "Tutar: ikinci uzman bu tur gelmeli". Kepenk ve sandık nişinin hover'ında dinamit cümlesi yoktur.
- **Tahta işaretleri** (`RaidBoard3D.Marks.Ambush/Cargo/Focus`):
  - Kurulu pusu hattı yeşil (`lib.good`), kesik ve oksuzdur, 4 px; sahibi tahtada değilse α .35. El çizimi değil düz kesik
    çizgidir (`RaidArrow`'un el çizimi kipi kesik ve oksuz çizgiyi desteklemiyor). Hat boşsa (ilk kare duvara bakıyorsa)
    yönündeki ilk kareye kısa bir saplama çizilir. Hat makine evresinde ateşlenene kadar tahtada kalır.
  - Yerdeki yük: karenin güney-doğu köşesinde küçük sandık (`lib.lootCrate` × 0,45) ve "YÜK" notu.
  - Odak çerçevesi: sürüklemede gövdenin karesi; iki kişilik işte yalnız iş karesinin üzerine gelinip önizleme varken.
  - Kâğıt notları: "SANDIK" (açılmamış niş), "TUTULUYOR" (tutulan iş karesi), "YÜK". Kalkık kepenkte not yok. Nişin,
    tutuşun ve kalkık kepengin modeli aşama 9'da kuruldu (§5 "Uygulanan hâl (aşama 9)").
- **Canlandırma (Fx):**
  - Sürükleyen yolu kare kare yürür, gövde bir adım arkasından gelir (`AnimateWalk`).
  - Menfezde Analist yakın uca yürür, batar, öbür uçta çıkar.
  - Fırlatmada geçici küçük sandık kavis çizer. Kepenk, breach dalı gibi (`SinkWall`) çöker.
  - Popup'lar: TUTUYOR, KEPENK KALKTI, SANDIK AÇILDI, YÜK ALINDI, YÜK DÜŞTÜ.
  - Yürüyüşte yük alımı `Carrier` farkından okunur (`DoMove` `FxList`'i temizlemez); parça `PickCargo` ile kalkar.
- **Makine evresi bölmesi:**
  - İlk `ambush-hit`'te evre bölünür: önce saldırılar, sonra yürüyüş (pusuyla düşecekler dahil), sonra pusu ve uyanma.
    `EffectPlaying` iki parçada da tam listenin sırasını ve uzunluğunu bildirir (`Animate(…, indexBase, total)`).
  - Taşıyıcı evrede düşerse yük parçası evre içinde belirir; görev çipinin YERDE damgası ve amaç satırı da sunum kopyasından
    okunur, düşüşten önce değişmez.
- **Telsiz:**
  - Her hamlenin telsiz fişi var. Core satırları ve fişler makine evresi boyunca akar (pusu vuruşu, yük düşmesi; tutuş boşa
    gittiyse bir satır fazla). Sürüklemenin Core telsiz satırı yok, yalnız fişi var (açık iş).
  - Gözcü raporu "Gözcü · büyük sandık: 3-F1 · iki uzman" ve "Gözcü · yerde: {ad} · {kare}" satırlarını ekler.
  - Oda kapanışına "{ad} tesisten çıkarıldı", "{ad} ekiple taşınıyor", "Kepenk kalktı · {kare}" ve "Büyük sandık açıldı ·
    {kare}" eklenir; bugünkü satırlar ve sırası aynı kalır.
- **Kart ve satır metinleri:**
  - Ekip kartı düşen için Core'un oda raporu kuralını izler: "YERDE · 2-C3", "GÖVDE KAPIDA", "EKİPLE TAŞINIYOR" (son nefes,
    geçişte) ya da "ÇIKARILDI" (tesisin girişinde).
  - Amaç satırı: "Amaç: yükü yerden al ({kare})" (tesisin girişi dahil).
  - Yük yerdeyken çıkış karesinin hover'ı tahliye satırından sonra, kapı kartı da sonda "Yük yerde kalır · {kare}" yazar
    (kapı kartının 4 satır sınırı için önce "Kapıdan geri çıkılmaz", sonra ilk açıklama satırı düşer).
- **Son nefes:** oda kendiliğinden geçilince `Breath` taşıyan gövdenin parçası evre sonunda ekiple tahtadan kalkar (geçişte
  KAPIDA, tesisin girişinde TAHLİYE); `Sync` onu çizmez. Gövdenin karesinin hover'ı "Son nefeste düştü: ekip onu da götürür"
  der. Kapının "Yerde kalan" satırı, GEÇ fişi, gözcü satırı ve uyarılar bu gövdeyi saymaz (`FacilityMoveText.Lying`,
  `GoesWithTeam`).
- **GEÇ:** onaysızdır (§14 altıncı tur 3). Yerde gövde varken fişte ve kapı kartında uyarı çıkar ("Yerde kalan: …").
- **Kurallar kartı:** rules.2'ye hamle satırı; rules.6'ya düşen ve yük kuralı (kazanma satırının yerine geçmez); geri çekilme
  metni tesiste `raid.retreat.bodyFacility` ("… Yerde yatanlar ölür."). Eski tahtanın metni aynı.
- **Operasyon raporu:** kazanılan ikmal baskınında büyük sandık açıldıysa KAZANÇ'taki METAL ve DEVRE sandığın ekini de sayar
  (`RaidPaperFacts.MaterialGains`; önceden yalnız nominal ödül, hata düzeltmesi). SONUÇLAR'da sektör satırından hemen sonra
  "Büyük sandık · +2 metal · +1 devre" (Balance'tan, `Good`). Kâğıdın 7 yuvası (`OperationReportData.MaxResults`) aşılırsa
  değişmemiş YZ alarmı satırı düşer.
- **Ses:** yeni ses yok.

### 4.1 Pusu (`pusu`)

- Pusu yeteneğin yerine kullanılır: o tur yetenek kullanılamaz. Uzman önce yürüyebilir, sonra bir yön seçer; seçilen
  hat yağlı kalemle işaretlenir.
- Makine evresinde makineler yeni yerlerine geçtikten sonra, hatta duran ilk makine 1 hasar alır. Hat düz, 4 kare (b)
  uzunluğundadır ve ilk duvarda, nesnede ya da birimde biter.
- Core'da makineler yol yürümez, yeni kareye doğrudan taşınır. Bu yüzden pusu "hattan geçeni" değil "yürüyüşünü hatta
  bitireni" vurur.
- Makine aklı pusu hattını görmez (b); sis bombasındaki kuralla aynıdır.
- Pusu makinenin o tur uygulanan niyetini engellemez; yeni seçtiği niyeti, makine ölürse düşürür.
- Hamlenin adı "Pusu"dur; "Kısa nöbet" bölge özelliğinin adı değişmez.

**Uygulanan hâl (aşama 6).**

- **Hedef:** uzmanın dört dik komşusundan geçilebilir olanlar (hattın ilk karesi; yön ondan çıkar).
  - İlk karesinde ayakta ekip uzmanı duran yön ve bulutlu yön verilmez; hat orada biteceği için hamle boşa giderdi
    (kapanış düzeltmesi). Makine ve gövde ilk kareyi kapatmaz.
  - Buluttaki uzman pusu kuramaz (kanca ve EMP kuralıyla aynı).
- **Durum:** `Unit.AmbushDir` (−1 yok), `AmbushX`, `AmbushY` (kurulduğu kare). Hat uzmanın kurulduğu karedendir; uzman
  sonradan yürüyemez (bedel).
- **Ateşleme:** `EndTurn`'de `PlanEnemies`'ten sonra, `PlaceSpawns`'tan önce (`FireAmbush`); uyanan makine vurulmaz.
  - Sahibi tahtada değilse (düştü, kapıda, kabinde) ateşlenmez.
  - Hat 1..`facilityAmbushRange` (4) kare; tahta dışı, duvar, nesne ya da bulutta biter. Sis kesmez.
  - İlk ayakta birim makineyse 1 hasar alır (telsiz "Pusu: {0} vuruldu."); ekiptense vurmadan biter.
  - İki hat aynı makineyi ayrı ayrı vurur. Ölen makinenin yeni niyeti düşer.
  - Ateşlemeden sonra bütün hatlar silinir. Odanın erken bittiği dallarda (kayıp, geçiş, süre) ateşlenmez.
- **Fx:** `ambush` (kare, yön), `ambush-hit` (makine, sahibi).

### 4.2 Koşu darbesi (`kosu`)

- Uzman düz bir hatta en az 3 kare (b) koşar ve hattın sonundaki makineye çarpar: makine aynı yönde 2 kare (b) itilir.
- İtme bugünkü kurala uyar: duvara, nesneye ya da başka birime çarpan 1 hasar alır; kablodan geçen yanar.
- Hamle hem yürüyüşü hem yeteneği harcar. Koşu hattında dost, nesne ya da duvar varsa hamle verilemez.

**Uygulanan hâl (aşama 6).**

- **Hedef:** düz hattın sonundaki ilk ayakta makinenin karesi. Koşu boyu L (makineye kadarki boş kare):
  `facilityRushRun` (3) ≤ L ≤ max(`facilityRushRun`, yürüyüş). "En az 3" alt sınırdır; yürüyüşü 2 olan profil de 3 kare koşar.
  - Keser: tahta dışı, duvar, nesne, turnikenin kapalı yönü (`CanStep`), ayakta ekip birimi.
  - Kesmez: gövde, kablo, bulut (fiziksel hamle).
- **Uygulama:** koşucu makinenin önündeki kareye ışınlanır. Kablo hasarı almaz, geçit karesinde kapıya girmez (aynı tur kendi
  yürüyüşüyle girebilir), menfezde ışınlanmaz, yerdeki yükü alır.
- **İtme:** makine `facilityRushPush` (2) kez ayrı ayrı itilir (bugünkü `Push`: çarpan 1 hasar, kablo yakar, yağ kaydırır).
  İtme makineyi yerinden oynatmazsa ya da makine düşerse kalan itme yapılmaz; çift çarpışma hasarı olmaz. Koşucu hasar
  almaz, darbenin kendi hasarı yoktur.
- **Fx:** `rush` (başlangıç, koşu, uzman, makine) ve `push`.

### 4.3 Düşen kişi ve sürükleme (`surukle`)

Bugün düşen birim tahtadan kalkar ve ekipten biri sağ çıktıysa kendiliğinden kurtarılır. Yeni kural:

- **Düşen kişi tahtada kalır.** Durumu açıktır: düştüğü karede yatar. Karesi yürüyüşü ve atışı kesmez; makineler onu
  hedef almaz.
- **Sürükleme.** Düşenin yanındaki ya da üstündeki uzman yürürken onu çeker: düşen, sürükleyenin bıraktığı kareye
  gelir. Sürükleyenin menzili 1 azalır (b) ve o tur yeteneğini kullanamaz.
- **Dışarı çıkarma.** Sürükleyen geçide ya da çıkışa girince düşen de onunla geçer. Temiz odada bırakılmış düşen,
  dönüşte kendiliğinden alınır.
- **Taşınmayan ölür.** Düşeni kurtarmanın tek yolu onu tesisten çıkarmaktır.

| Düşenin sonu | Sonuç |
|---|---|
| Tesisten çıkarıldı (sürüklenerek ya da temiz odadan toplanarak) | Bugünkü kurtarılma: `boardInjuryTurns` (3) vardiya yatış |
| Taşınmadı: baskın bitti, ekip onsuz çıktı | Ölür |
| Son nefes (aşama 6): oyuncunun hamle sırası dışında (makine evresi, tur sonu olay, kablo, gaz) düştü ve oda, oyuncu bir daha hamle yapamadan bitti (kendiliğinden geçiş, kaçışta süre, tesisin girişinden son çıkış); ekipten en az biri ayakta çıktı | Ekiple gider: geçişte "ekiple" (sonraki odaya konur, orada yine taşınmalı; yürüyerek çıkışta çıkarılır), tesisin girişinde çıkarılır. Gövde bir oyuncu turunun başında tahtada yattıysa üst satır: taşınmayan ölür. Geri çekilme ve kaçıştan önce kaybedilen oda değişmez |
| Geri çekilme verildi | Ayaktakiler döner; yerde yatanlar ölür |
| Kaçıştan önce süre doldu (giriş, ara, hedef) | Geri çekilme gibi: ayaktakiler döner, yerde yatanlar ölür |
| Kaçışta ya da dönüş odasında süre dolarken ayakta tahtada kaldı | Bugünkü kural: geride kalan ölür |
| Bütün ekip düştü | Bugünkü kural: düşen herkes ölür (Katar'da kenardan düşen hariç: alt satır) |
| Savunma baskını | Bölüm 9.2: kazanılırsa ekibin bulunduğu odada yatan çıkarılmış sayılır; terk edilen odada kalan ve kaybedilen savunmada yatan ölür |
| Katar'da açık kenardan düştü | Baskından çıkar; `facilityFallInjuryTurns` (5) vardiya yatar, ölmez (bölüm 10.1) |

Ölenin ustalığı, unvanı ve üstündeki eşya bugünkü gibi gider; köpek sahibiyle birlikte kaybedilmez (depoya döner) (b).

**Uygulanan hâl (aşama 6).**

- **Gövde modeli:** yeni alan yok (ilk tasarımdaki kişi başına `Fallen` yazılmadı). Düşen birim bugünkü gibi `B.Units`'te,
  düştüğü karede kalır; anlam bayrakların birleşimidir (`Baskin.IsBody`: ekip, `Dead`, `!Left`, `Slot ≥ 0`):

  | Durum | Bayraklar | Rapor (`Body`) |
  |---|---|---|
  | Yerde yatıyor (`LyingBody`) | `Dead && !Left && !AtGate && !Out` | 0 |
  | Kapıya sürüklendi | `Dead && AtGate` | 1 (oda geçilince) |
  | Tesisten çıkarıldı | `Dead && Out` | 2 |
  | Geride kaldı (süre) | `Dead && Left` | (durum "l") |

  - Gövde `OnBoard` değildir: kareyi kapatmaz, yürüyüşü ve atışı kesmez, hedef alınmaz, itilmez, kanca görmez. Yalnız
    sürüklemeyle hareket eder.
  - `CanPass` ve `CheckEscape` gövdeye bakmaz. `FinishEscape` gövdeyi "çıkan uzman" saymaz (`gone = Out && !Dead`).
  - `Unit.Breath`: son nefes işareti (aşağıda). Tahtaya ikinci bir `die` Fx'i girmez; kesin ölüm sonuçta verilir.
- **Sürükleme (`surukle`, yürüyüş + yetenek):**
  - Gövde: uzmanın karesinde ya da dik komşusunda yatan (`DragBodies`); duvar karesinde (raf kaymasının altında) yatan da
    sürüklenir.
  - Varış: `Reachable` kuralı, menzil yürüyüş − `facilityDragMoveCost` (1); menzil 1'in altındaysa hedef yok. Menfezden
    geçilmez.
  - Gövde, uzmanın yolda bıraktığı kareye gelir: `WalkPath` (aynı BFS, `Dirs` sırası, ilk bulan ebeveyn) yolunun varıştan önceki
    karesi; tek adımda uzmanın başlangıç karesi. Yol bir kapı karesinden geçerse gövde yoldaki kapı olmayan son kareye
    bırakılır, böyle kare yoksa yerinde kalır (kapanış düzeltmesi: kapının üstünde `AtGate` olmadan yatan gövde oyuncuyu
    yanıltırdı).
  - Varış geçit karesiyse uzman kapıya girer, gövde de `AtGate` olur (karesi uzmanınki). Tesisin girişinde ikisi de çıkar
    (gövde `Out`). `EnterGate` gövde götürmez.
  - Varıştaki yerdeki yükü uzman alır. Fx `drag` (gövde, eski kare, fark).
- **Zincir** (`RaidChain`, saf):
  - `RoomMember.Body` 0/1/2 ve `Picked` (dönüşte atlanan temiz odadan toplandı). Kapıdaki gövde yalnız oda geçildiyse (evre
    "pass") 1 yazılır; oda kayıpla biterse yerde sayılır.
  - Ekiple gelen gövde (Body 1) sonraki odanın giriş geçidine ayaktakilerden sonra konur, `B.Units`'in sonuna eklenir (kimlik
    sırası değişmez) ve orada yeniden yerde başlar: taşınmaya devam etmesi gerekir.
  - Dönüş tahtasında o odada yatan gövde kendi karesine konur (geçilemezse en yakın açık kare).
  - Temiz oda (`Collect`): dönüşte atlanan temiz odada yatan gövde Body 1, Picked 1 olur ve sonraki oynanan dönüş odasına
    girer; "taşınmış sayılır" böyle okundu (D17).
  - Yürüyerek çıkış: Body 1 olan ve batıdaki temiz odalarda yatan herkes Body 2 olur.
- **Sonuç:** `RaidResult.Extracted` (kadro sıraları) = son raporda düşmüş ve Body 2. Tek karar yeri `RaidChain.IsExtracted`
  (savunma kancası aşama 11'de yapıldı: kazanılan savunmada ekibin odasında yatan ve ekiple gelen, §9 "Uygulanan hâl (aşama 11)"; Katar kenarı aşama 12). `ReturnPlanningSquad`'ın tesis dalı:
  `dead = left || can ≤ 0 && !Extracted`. Çıkarılan düşen bugünkü kurtarılmayı alır (`boardInjuryTurns` yatış). Kartsız yol
  (`ReturnSquad`) ve bayrak kapalı yol değişmez.
- **Son nefes** (kapanışta eklendi; tablodaki satır):
  - `EndTurn` artık bir sarmalayıcıdır: turun başında düşmüş kadronun kimliklerini kaydeder, sonra eski gövdeyi
    (`EndTurnCore`) çağırır.
  - Oda o `EndTurn`'de biterse (evre "pass" ya da tesisin girişinde `FinishEscape`) ve ekipten biri ayakta çıktıysa (geçişte
    kapıda, girişte `Out`), o evrede düşüp yerde yatan her gövdeye `Unit.Breath` yazılır. Telsiz: "{0} son nefeste düştü; ekip
    onu da götürdü."
  - Rapor: geçişte Body 1, tesisin girişinde Body 2. Kayıp dalları (geri çekilme, kaçıştan önce süre, bütün ekip düştü) ve bir
    oyuncu turunun başında yatan gövde eski kurala tabidir.
  - Kaçışta süre dolarken kural harfiyen işler: o evrede düşen gövde ekiple gider, tahtada kalan ayaktakiler `Left` olur ve
    ölür.
  - Tesisin girişinde `FinishEscape` "lost" olsa da (ör. yük kayıp) gövde çıkarılır; sürüklenerek çıkarılan gövde de görevin
    sonucundan bağımsızdır.
  - Taşıyıcı son nefeste düşerse gövde ekiple gider, yük yerde kalır (Cargo −2; açık soru, §14 dokuzuncu tur).
  - `facilityLastBreathDies` (varsayılan false) kuralı kapatır; yalnız ölçüm anahtarıdır. Ad olumsuzdur: asset'te ve kayıtta
    `facility*` alanı yok, eksik alan nasıl doldurulursa doldurulsun kural açık kalır.
  - RNG çekmez. `Signature`'da birim ekinin sonunda `n` (yalnız varsa).
- **Ölçüm:** §11.6 "Aşama 6 ölçümü". Ölü hedefi (≤ 7,5) tutmadı: 12,7.

### 4.4 Yük: bırakma ve fırlatma (`firlat`)

- İkmalde bitişik kasayı her uzman yetenek hakkıyla açar ve yükü alır (sürüm 10, geliştirici: "erzakı tüm karakterler taşıyabilsin"; önce yalnız Teknisyen). Açmak şarj harcamaz, kasayı kırmaz. Köpek, tutsak ve aygıt açamaz; köpek yükü yine `getir` ile alır.
- **Fırlatma.** Yükü taşıyan uzman, yeteneği yerine yükü düz hatta 3 kare (b) içindeki ayakta bir ekip üyesine ya da
  köpeğe atar. Hatta duvar, nesne ya da birim olmamalıdır.
- **Bırakma.** Bugün taşıyıcı düşünce yük yok olur. Yeni kuralda yük düşenin karesine düşer; o kareye giren ilk ekip
  üyesi alır.
- Makineler yüke dokunmaz. Yük odada bırakılırsa baskın ikmal olarak kaybedilir.

**Uygulanan hâl (aşama 6).**

- **Durum:** `BState.CargoX/CargoY` (yerdeki yük, −1 yok); taşıyıcı bugünkü gibi `B.Carrier`.
- **Bırakma:** tesis ikmalinde taşıyıcı düşünce (`Hurt`) `Carrier = 0` olur, yük düştüğü kareye konur. Fx `cargo-drop`, telsiz
  "{0} düştü: yük yere düştü. Kareye giren ilk ekip üyesi alır."
  - Kare geçilemezse (ör. raf kaymasıyla duvar karesinde kalan taşıyıcı) yük en yakın açık kareye düşer (`NearestOpen`:
    geçit, menfez ve ayakta birimli kare dışında, BFS uzaklığı, sonra y, sonra x; çekimsiz; kapanış düzeltmesi).
  - Süre dolunca geride kalan (`Left`) yükü düşürmez; oda zaten kaybedilir. Eski tahtada bugünkü kural (yük düşenle yok olur).
- **Alma:** "kareye giren ilk ekip üyesi" = kendi hareketiyle varan uzman: yürüyüş (`DoMove`, kapıya girmeden önce), koşu
  bitişi, sürükleyenin varışı, menfez çıkışı. İtilen, çekilen ve kayan almaz; `EnterGate` almaz. Fx `cargo-pick`.
- **Fırlatma (`firlat`, yeteneğin yerine):** taşıyıcı, düz hatta 1..`facilityThrowRange` (3) kare içindeki ayakta ekip uzmanına
  atar. Aradaki kareler geçilebilir ve ayakta birimsiz olmalı; gövde kesmez; duvar, nesne ya da makine keser. Kapıdaki uzman
  alıcı olamaz (tahtada değil). Buluttan ve buluta atılmaz. Köpek alıcı aşama 8'de. Fx `throw`.
- **Rapor:** yük yerdeyken oda biterse `Cargo` −2 (baskın ikmal olarak kaybedilir); `CargoAt` yerdeki yükün karesi (sunum
  için). Tesiste taşıyıcı yokken çıkış nedeni "Yük odada kaldı: ikmal malzemeleri kaybedildi."

### 4.5 İki kişilik işler (`ortak`)

- **Ağır kepenk** (ara oda): kepenge bitişik iki ayakta uzman aynı turda yeteneğini harcarsa kepenk kalkar ve açık
  kalır; kalıcı izdir (bölüm 9.1).
- **Büyük sandık** (ikmalin hedef odası, isteğe bağlı): aynı kuralla açılır; baskın kazanılırsa ödüle
  `facilityBigCrateMetal` (2) metal ve `facilityBigCrateCircuits` (1) devre ekler (b).
- İlk uzman "tutar"; ikinci aynı turda gelmezse tutuş boşa gider.

**Uygulanan hâl (aşama 6).**

- **İş kareleri:** kapalı kepenk (`Cell.Shutter && T == "wall"`) ve açılmamış büyük sandık (`Cell.Crate && !CrateOpen`).
  "Bitişik" sekiz komşudur, çapraz dahil (D9): bölme kepenginde her yanda tek dik komşu var.
- **Tutuş:** `BState.HoldId/HoldX/HoldY`. Geçerli tutuş (başka uzman, aynı kare, tutan tahtada ve hâlâ sekiz komşuda) varsa
  ikinci uzman işi açar; yoksa kendisi tutar (öncekini ezer). Tutan çekilir ya da düşerse ikinci uzman yeni tutuş başlatır.
  Tur sonunda ("Tutuş boşa gitti.") ve GEÇ'te (sessiz) tutuş bırakılır; tur içi geri alma getirir.
- **Kepenk:** kalkınca `T = "floor"`, `Shutter` bayrağı kalır; makineler de geçer (kepenk iki yönlü kapıdır). Rapor `Opened`
  → dönüş tahtası açık kurulur (baskın içi kalıcılık; baskınlar arası iz aşama 11'de: `kepenk` izi, §9).
- **Büyük sandık:** duvar nişi (§2.3). Açılınca `BState.CrateOpen`, rapor `Crate`. Ödül yalnız kartlı yolda, kazanılan tesis
  ikmal baskınında: 2 metal + 1 devre, `LiberationRaidReward.BigCrate` (kayda girmez, raporlardan türer). Kaybedilen baskında
  ve kartsız yolda ödül yok.
- **Dinamit:** kepenk ve sandık nişi dinamit hedefi değildir (D10); iki kişilik işi atlatmasın.
- **Fx:** `hold`, `open`. Telsiz: "{0} kepengi tutuyor…" / "{0} büyük sandığı tutuyor…", "Kepenk kalktı." / "Büyük sandık
  açıldı."

### 4.6 Menfez (`menfez`)

- Menfez karesine yürüyüş hakkı kalmışken giren Analist çiftin öbür ucundan çıkar; yürüyüşü orada biter, yeteneği
  durur.
- Yalnız Analist geçer (b). Makineler, köpek ve sürüklenen kişi geçemez.
- Öbür uçta birim varsa geçilemez.

**Uygulanan hâl (aşama 6).**

- **Açık eylem** (`menfez`, D2): `DoMove` menfez karesinde durur, ışınlamaz; yanlışlıkla ışınlanma olmaz, geçiş önizlenir.
- **Hedef:** Analist bir menfez ucundaysa (d = 0) ya da uca d adımda yürüyebiliyorsa ve d < yürüyüş, aynı numaralı öbür uç.
  Öbür uçta ayakta birim olmamalı; gövde ve yük kapatmaz.
- **Uygulama:** Analist öbür uca geçer, yürüyüşü biter, yeteneği kullanılabilir kalır ("yeteneği durur" = kalır, D26). Öbür
  uçtaki yükü alır. Kilitli ya da donmuş Analist geçemez. Makineler, köpek ve sürüklenen gövde geçemez (`Reachable` değişmedi).
- **Fx:** `vent` (yakın uç, fark); telsiz "{0} menfezden geçti."

## 5. Ekipman

### 5.1 Yuva ve taşıma

Eşyalar bugünkü gibi panoda kişiye takılır (`board_equip`). Yeni ekipmanın hepsi yardımcı yuvaya (`utility`) girer;
bu yuva bugün bakım kiti ve EMP hücresi içindir. Kişi başına tek yardımcı eşya kuralı kalır: üç kişilik kadro baskına
en çok üç ekipman götürür. Emir verilince seçim kilitlenir (bugünkü plan kilidi).

Ekipmanı her rol kullanır. Kullanım yeteneğin yerinedir; hak bitince eşya o baskında işe yaramaz. Haklar her baskında
dolar; eşya tükenmez (kelepçe hariç).

### 5.2 Liste

Yedi temel ekipman vardır. Beş cevap tarifi yeni eşya değildir; 5.6'daki geliştirmelerdir.

| Ekipman | Kimlik | Hak (b) | Etki |
|---|---|---|---|
| Örme seti | `wire_kit` | 2 | Bitişik boş zemine engel kurar (bölüm 5.3) |
| Dinamit lokumu | `charge` | 2 | Bitişik duvarı kalıcı zemine çevirir; moloz bir kare öteye 1 hasar verir (bugünkü `dinamit` kuralı) |
| Yaylı taret | `turret` | 1 | Aygıt: bir yöne kurulur; her makine evresinin sonunda hattındaki ilk makineye 1 hasar; 3 tur çalışır |
| Yürüyen yem | `decoy` | 1 | Aygıt: kurulduğu yönde her makine evresinin sonunda 2 kare yürür; makineler onu ekip üyesi gibi hedef alır; 3 tur |
| Makaralı vinç | `winch` | 1 | Aygıt: hattındaki ilk birimi, düşen kişiyi ya da yükü her makine evresinin sonunda 1 kare kendine çeker; 3 tur |
| Kumanda kelepçesi | `clamp` | 1; kullanılınca tükenir | Bitişik robotu ele geçirir (bölüm 5.4) |
| Köpek | `dog` | Sürekli | Ayrı hamle hakkı olan dördüncü birim (bölüm 5.5) |

Aygıtların ortak kuralları (b):

- Aygıt ekip tarafındadır ama uzman değildir: can 2, yürümez (yem hariç), çıkışa gitmez, sonuç listelerine girmez.
- Mekaniktir: EMP, kilitleyici ve donma onu etkilemez; YZ onu ele geçiremez.
- Kurulduğu turun makine evresinin sonunda çalışmaya başlar ("fitil" kuralı): makinelerin o turki niyeti yine
  uygulanır.
- Süresi bitince ya da kırılınca hurda olarak karede kalır (engel, 1 can). Odalar arasında taşınmaz.
- Makineler aygıtı ve engeli hedef alabilir.
- Aygıt girişe, geçide ve menfeze kurulamaz; yem bu karelere yürümez; bu karelerde hurda kalmaz (kaldırılır).

### 5.3 Örme ve yıkma

| Engel | Kimlik | Hak | Can (b) | Yürüyüş | Atış |
|---|---|---|---|---|---|
| Tel örgü | `tel` | 1 | 3 | Keser | Geçirir |
| Kalas duvar | `kalas` | 2 | 3 | Keser | Keser |

- Engel girişe, geçide, menfeze ve nesne olan kareye kurulamaz.
- Engel ayaktaki hiçbir ekip üyesini odanın batı girişinden (dönüş yolu), ileri geçitten ve hedef odada hedeften (dolap
  ya da kasa) koparamaz; koparacaksa kurulamaz (bugünkü `ExitSafe` denetiminin ekip için uygulanışı). Makineleri
  koparabilir; amacı budur.
- Engel nesne gibi hasar alır: ekip onu bugünkü sıradan nesne kuralıyla kırabilir (sabotaj 2, EMP ve dinamit 1 hasar),
  kendi ördüğünü de (b).
- Ekibe ulaşamayan ve vuracak ekip üyesi bulamayan her makine (yakın dövüşçü ya da uzaktan vuran) önündeki engele
  yönelir. Bugün ulaşamayan makine sivil sunucuya nişan alır; engel kırma bundan önce gelir (b).
- Tel kabloyla birleşir: tele itilen birim itme kuralındaki 1 hasarı alır.
- Örülen engel ve yıkılan duvar kalıcıdır (bölüm 9).

### 5.4 Ele geçirme

- Kelepçe bitişik bir robota takılır. Robot `facilityClampTurns` (3) tur (b) oyuncunun olur: her tur bir yürüyüş ve
  kendi eylemi (ateş, vuruş, onarım yerine ekip üyesine zırh gibi) vardır.
- Ele geçirilmiş robot ekiple birlikte geçitten geçer; kalan turu sonraki odada sürer.
- Süre bitince robot yeniden düşmandır ve bir makine evresi boyunca niyetsizdir (yeniden açılır) (b).
- Kopya dolabı, hedef dolap ve aygıt ele geçirilemez. Ele geçirilmiş robot tesisten çıkarılamaz.
- Ele geçirilmiş robot makine sınırına (`MaxEnemies` 7) sayılır.

### 5.5 Köpek

- Köpek bir kişinin yardımcı eşyasıdır ve kişiye bağlıdır. Baskına o kişiyle girer; giriş karesinin yanındaki en
  yakın boş kareden başlar.
- Kendi hamle hakkı vardır: turda bir yürüyüş ve bir komut. Can 2, yürüyüş 5 (b). Yetenek kartı ayrıdır.

| Komut | Kimlik | Etki (b) |
|---|---|---|
| Havla | `havla` | 3 kare içindeki makineler o evrede köpeği ekip üyesi gibi puanlar |
| Getir | `getir` | Yerdeki yükü alır ve taşır; yük köpeğe fırlatılabilir; köpek çıkışa varınca yük çıkmış sayılır |
| Kokla | `kokla` | Geçidin yanındayken sıradaki odanın planını çıkarır (yalnız plan; robot göstermez) |

- Köpek düşerse tahtadan çekilir; eşya `facilityDogRestTurns` (3) vardiya kullanılamaz (b). Köpek ölmez.
- Sahibi düşerse köpek oynamaya devam eder.
- Köpek kepenk kaldıramaz, menfezden geçemez, kişi sürükleyemez.

### 5.6 Tarifler

Bugünkü altı tarif (`armor`, `medkit`, `emp_cell`, `kit_tek`, `kit_emp`, `kit_ana`) baştan açık kalır. Yeni tarifler
sunucu ödülüyle açılır (bölüm 6.2).

**Tarif defterden seçilir.** Yayındaki otomatik üretim tarifi girdiye göre tanır (`AutomaticProductionRecipe`: girdisi
karşılanan ilk tarif); on üçten çok tarifle bu ayırt edemez. Tesis kipinde oyuncu üretilecek tarifi defterden seçer:
Core'da duran iş emri komutları (`QueueProduction` `board_queue`, `ReserveProductionMaterial` `board_reserve`) yeniden
kullanılır; yeni komut gerekmez. Bugünkü kodda aynı anda tek emir açılır, açık emir varken otomatik üretim hiç çalışmaz
ve otomatik yol tarif süresini uygulamaz. Tesis kipinde eksik rol kiti emir açıkken de kendiliğinden üretilir (b); emir
yoluna geçen bugünkü tarifler de süreli olur (EMP hücresi 2 vardiya).

| Tarif | Girdi (b) | Vardiya (b) |
|---|---|---|
| Örme seti | 2 metal + 1 kumaş | 1 |
| Dinamit lokumu | 1 metal + 2 kimyasal | 2 |
| Yaylı taret | 2 metal + 1 devre | 2 |
| Yürüyen yem | 1 metal + 1 devre + 1 kumaş | 1 |
| Makaralı vinç | 2 metal + 1 kumaş | 1 |
| Kumanda kelepçesi | 2 devre + 1 metal | 2 |
| Köpek (eğitim) | 4 erzak + 1 kumaş | 3 |

Cevap tarifleri (bölüm 6.3) eski tarifin yerine geçer ve girdisine 1 devre ekler (b): Delici şarj `charge`
tarifinin, Çelik tel seti `wire_kit` tarifinin, Yedek röle `clamp` tarifinin, Çift zemberek `turret`, `decoy` ve
`winch` tariflerinin üçünün, Söküm anahtarı `kit_tek` tarifinin yerine geçer. Cevap tarifi yalnız o harekâtta açılmış
temel tarifin yerine geçer; temel tarif açılmadıysa o eşyayı üretilebilir kılmaz (seçilmeyen ödül hiçbir yolla
alınamaz, bölüm 6.2). Çift zemberek üç tariften yalnız açık olanların yerine geçer; ödül seçimi satırı bunu yazar.

Köpeğin bedeli erzaktır; erzak hammadde olmadığı için tarife erzak bedeli alanı eklenir (`BoardRecipe`).

**Uygulanan hâl (aşama 8).** `BoardRecipes` = baştan açık altı tarif + (yalnız tesis kipinde) `Facility.Recipes`'teki temel tarifler
açılış sırasıyla (`Reward = true`); eski sayfa dizinleri kaymaz, açılan tarif kapanmaz (K17). Kilitli tarif listede olmadığı için
`board_queue` onu reddeder ("Böyle bir tarif yok."); yeni komut yok. Otomatik üretim yolu yalnız eski altıyı tanır (örme ve vinç
girdisi zırhla aynı). Köpek tarifinin erzak bedeli (4) emir başlarken düşer, yetmezse emir hammadde eksikliği gibi bekler (K18).
Tesis kipinde açık emir varken `FieldJobs` sırasıyla ilk eksik rol kiti, üretim alanındaki ve serbest hammaddeyle karşılanıyorsa
o kapanışta üretilir; emir o kapanış ilerlemez (tek üretici tek iş; K19); karşılanamıyorsa sonraki eksik kite bakılmaz. Tek emir
sınırı kaldı (K20). Tezgâh ödül tariflerini 1 vardiya kısaltır (en az 1). Cevap tarifleri seçilebilir, `Facility.Recipes`'e yazılır,
listeye girmez (etkisi aşama 10; K22).

**Uygulanan hâl (aşama 9): defter, iş emri, depo.** Tarif defteri 13 sayfaya kadar büyür (7 açılım). Açılmamış ekipman
tarifleri açık sayfalardan sonra kapalı sayfa olarak görünür ("KAPALI · {YZ} sunucusundan"; sürüklenmez, açılınca dizinler
kaymaz). Sayfa üç girdi satırı ve erzak satırı taşır (yem üç girdili, köpek "+ 4 erzak"); tarif kartı üç sütun ve üç kutu.
Ödülle açılan sayfada kaynak YZ ve (Tezgâh açıksa) "−1 vardiya" notu. İş emri formu ve devir fişi erzak yetmeyince "erzak
yetmiyor · emir bekliyor" der (`ProductionForecast.NoSupplies`); emir açıkken eksik rol kiti yapılacaksa "bu kapanış {kit}
yapılır · emir beklemede" (K19). Depoda yedi ekipmanın ikinci sırası kitlerin altında durur (`board.layoutMm.gearX/gearY`,
yalnız tesis kipinde): kart yüzünde piktogram (`DeskSprites` `picto_<tür>`), yığın sayısı yuva etiketinde ("YARDIMCI · ×2"),
kullanım satırı ("baskında 2 hak · …"), YENİ kayışı. Dinlenen köpek: kullanım satırında "DİNLENİYOR · n V", takılırken uyarı,
kadro raporunda ve kişi dosyasında not; emre gitmez. Köpek hurdaya atılmaz. Aşama 16’dan beri pano önizlemesi ve Core komutu aynı gerekçeyi kullanır. Görev fişinde
KAZANIM satırı (beşinci SAHA satırı; yalnız tesiste ve kazanım varken). Kararlar §14 on ikinci tur.

**Uygulanan hâl (aşama 10, 2026-11-19): cevap tarifleri.** Önlem defteri açıkken (`raidFacilityEnabled` ve
`facilityCountermeasures`, `Simulation.CountermeasuresOn`) seçilmiş cevap tarifi **ekip düzeyinde** etkilidir (S2, onaya açık):
etkisi emirde `RaidParams.Answers`'a kopyalanır (`Facility.Recipes ∩ FacilityGear.Answers`) ve bütün kadroya işler; depodaki eski
eşya da yükselir, +1 devre yalnız yeni üretimin bedelidir. Defterde cevap yeni sayfa açmaz: temel tarifin sayfası **aynı dizinde,
aynı `Id`, `Output`, `Turns` ve `Reward` ile** durur, adı cevabın olur (`FacilityGear.RecipeName`), `BoardRecipe.Answer` dolar ve
girdiye `facilityAnswerCircuits` (1) devre eklenir (devre satırı varsa adet artar, yoksa yeni satır; en çok üç girdi türü). Eşleme
(`FacilityGear.AnswerBases`): Söküm anahtarı → Teknisyen kiti (`kit_tek`), Delici şarj → lokum, Çelik tel seti → örme seti, Yedek
röle → kelepçe, Çift zemberek → taret, yem ve vinç. Temel açık değilse sayfa yoktur (boş cevap; ödül kâğıdı aşama 9'un notunu
yazar). Okuma anıdır: Çift zemberek sonradan açılan vinci ve yemi de dönüştürür. Söküm Teknisyen kitinin sayfasını değiştirir ve
`Reward` false kalır: eksik kit yolu (`ProduceMissingKit`) ve otomatik yol cevap sürümünü (eksik kit indiriminin üstüne +1 devre)
kullanır; bot kit hazırlığı aynı kimlikle sürer (`FacilityBotTests.Plan_AfterSokum_TechnicianKitStillPrepared`). Cevap seçildiği
anda temellerinden birinin **başlamamış** taslak iş emri silinir, ayrılan hammadde serbest kalır, ileti "Tarif açıldı: {0}. Taslak
iş emri iptal edildi: {1} tarifi değişti." (K22; `BoardPlanCheck` kilidini önler); başlamış emir sürer. Defter kapalıyken sayfa ve
iptal aşama 9'un aynısı. Sunum (parça D): cevap sayfasının el notu "Cevap tarifi · {önlem} önlemine karşı · +1 devre", kaynağı
"VERITY · cevap: HALCYON" (temel baştan açıksa "cevap: …"); defterin satırı "{n} tarif · {k} cevap" (kapalı sayfa varsa üçü); ödül
kâğıdının cevap satırı `FacilityGear.AnswerDesc` (+ "· önlem yürürlükte"; defter kapalıyken aşama 9'un "Cevap tarifi"), yeni sayfa
satırı cevapta "Defterde TARİF {nn} değişti: {cevap}" (Çift zemberekte sayfalar virgülle); eşya kartının kullanım satırı açık
cevabın notunu ekler ("tel 4 can", "+1 tur", "çift cidar 1 hak"); taslak iptali Core iletisiyle teleksten gelir. Cevapların
tahtadaki etkisi §6.3 "Uygulanan hâl (aşama 10)"; kararlar §14 on üçüncü tur.

**Uygulanan hâl (aşama 8, 2026-11-19): bütün §5.** Core'da; bayrak (`raidFacilityEnabled`) asset'te kapalı, kayıt biçimi 19, tesis
izi f3. Kurallar bayrak ya da tesis odası (`B.Room != null`) ya da dolu `Unit.Class` arkasında: eski tahtada ve ekipmansız tesis
tahtasında oyun aşama 6–7 ile aynı (altın imzalar, Digest, `t8yok` = `t6sn`). Kararlar §14 on birinci tur.

- **Tablo ve sayılar:** kimlikler, ödül eşlemesi, adlar, tarif kurma ve baskın hakkı `Core/FacilityGear.cs`; sayılar `Balance`'ta
  (§11.1). Yedi ekipman `utility` yuvasında; kişi başına tek yardımcı eşya. Köpek kişiye bağlı değildir (`Bound` yok; K14):
  takılı olduğu kişiyle girer.
- **Emir:** `PlanningSquad` (yalnız bayrak açık) kişinin yardımcı yuvasındaki ekipmanı `SquadSlot.Gear`'a, hakkı
  `FacilityGear.RaidCharges`'tan `GearCharges`'a yazar (örme ve lokum 2, öbürleri 1, köpek −1; Yedek çanta örme ve lokuma +1);
  dinlenen köpek (`BoardEquipment.Rest > 0`) gitmez. `RaidParams.Perks` emir anındaki kazanımların kopyasıdır (K23).
- **Eylemler:** `Baskin.ActionKinds` aşama 6'nın altısı kalır (K2). Yeni diziler `GearActionKinds` (tel, kalas, lokum, taret,
  yem, vinç, kelepçe), `UnitActionKinds` (havla, getir, kokla, komuta), `AllActionKinds`; `GearOf`. Bedel hepsinde yeteneğin
  yerine (önce yürünebilir). Uzman ekipman eylemini yalnız `Gear == GearOf(kind)` ve hak yetiyorsa verir (kalas 2 hak).
  Aygıt hiç eylem vermez ve yürümez; köpek yalnız kendi üç komutunu, tutsak yalnız `komuta`yı verir.
- **Birim sınıfı (`Unit.Class`):** null uzman, `kopek`, `aygit`, `tutsak`; `Slot` −1 ("köpek kadro dizinlerini kaydırmaz").
  `Baskin.IsSpecialist/IsGadget/IsDog/IsCaptive`. Kaçış, GEÇ, son çıkış, son nefes, sürükleme, yük alma, sonuç listeleri ve
  `EvDamageTeam` yalnız uzmanı sayar. Yeni birim türleri `taret` (R), `yem` (Y), `vinc` (V), `kopek` (Ö). Bölge olaylarının
  nişanı (`Baskin.Events.cs`) hâlâ `Side == 'p'`'ye bakar: aygıt, köpek ve tutsak olay nişanında ekip sayılır (§14 on birinci
  tur 51).
- **Örme seti (tel, kalas):** dik komşu boş zemine (kablo, nesne, birim, gövde, yük, kapı, menfez değil); ekip güvencesi
  `TeamSafe` (batı, kaçıştan önce ileri geçit, hedef odada kalan hedefler; önce tutan bağ sonra da tutmalı; K7). Tel nişanlı
  atışı geçirir (makine ateşi ve kilidi, pusu, taret); kalas ve hurda her şeyi keser (K5). Engel nesne gibi hasar alır; itilen
  birim ve engel birer hasar ("engele çarptı"). Engel oda raporunun `Objects`'inde, dönüş tahtasında yeniden kurulur.
- **Dinamit lokumu:** dik komşu duvar; kepenk, sandık nişi ve bulut değil (K31). Madencinin dinamitiyle ortak `Breach` (telsiz
  ve Fx sırası değişmedi).
- **Aygıtlar (taret, yem, vinç):** dik komşu boş zemine, yön uzmandan kareye (K32); can 2, 3 tur. Makine evresinde niyetlerden ve
  sisin dağılmasından sonra, kaçış denetiminden ve uyanmadan önce çalışır (`RunDevices`, K3; "fitil": kurulduğu turun niyetleri
  önce uygulanır). Taret: hat 4, ilk ayakta birim makineyse 1 hasar, ekipse atmaz; tel geçirir, öbür nesne ve bulut keser. Yem:
  yönünde 2 kare (engel, kapı, menfez, birimde durur). Vinç: hat 4, her nesnede biter; ilk ayakta birimi (aygıt hattı keser) ya
  da o karedeki gövdeleri ve yükü 1 kare (Makara 2) çeker; gövde ve yük kapı ve menfeze çekilmez (K34). Süresi biten ya da kırılan
  aygıt hurda (1 can) bırakır; kapı, menfez, gövde/yük karesinde ya da ekip güvencesi bozulacaksa kalkar (K11). EMP, kilit ve donma
  aygıta işlemez (K10). Aygıt odalar arasında taşınmaz; dönüş tahtası kalan turuyla geri kurar.
- **Makine aklı (yalnız tesis):** yem ve (erimde) havlayan köpek ekip gibi (10 + can düşükse 4), öbür ekip birimleri 3; kilit yalnız
  uzmana (K9). Ekibe ulaşamayan her makine engele 11 puan verir (sivil 9; K8). Süresi biten tutsak bir planı niyetsiz geçirir
  (`Idle`); `Rnd` çekilmez.
- **Kelepçe ve tutsak:** dik komşu ayakta makine. Makine ekibe geçer (`Side 'p'`, `tutsak`, sahibi kelepçeleyen), yakalandığı tur
  oynamaz, sonraki `facilityClampTurns` (3) oyuncu turu yürür ve `komuta` verir, sonra bırakılır ve bir planı niyetsiz geçirir
  (K12). Komuta tablosu (K13): dron ve zırhlı dron atış (tel geçer), bakım robotu dik komşu makine ya da nesneye 2 hasar (bulutta
  makineye değil) ya da plakasız uzmana plaka, havan 3 kare öteye, patlayan robot kendi karesinde (çevresinde ekip birimi yoksa;
  patlayınca düşer), kilitleyici hattaki ilk makineyi bir sonraki planında niyetsiz bırakır (bu evrenin niyetini silmez), kalkan
  dronu plakasız dik komşu uzmana plaka (yoksa dron gibi), çoğaltıcının eylemi yok. Hedef "vurulabilir kare": ayakta makine ya da
  nesne (ikmal kasası ve soğutma vanası değil); ekip birimi hiç hedef değil. Tutsak makine sınırına sayılır, makine raporuna ve
  sonuca girmez, GEÇ'te ekiple geçer (kalan turu taşınır), tesisin girişinde kalır; düşerse makine gibi. Kelepçe kullanılınca
  hakkı 0; son raporda 0 ise panodan silinir, kaybedilen baskında da (K27).
- **Köpek:** emirde köpeği olan kişiyle ilk odaya girer; yerleşim sırası uzmanlar → köpekler ve tutsaklar → ekiple gelen gövdeler
  (köpek ve tutsak kareleri makineye kapalı; karesi kalmayan konmaz). Can 2 (Kürk +1), yürüyüş 5, turda bir yürüyüş ve bir
  komut; kepenk, menfez, sürükleme, pusu yok. Havla: bu planda Manhattan 3 içindeki makineler köpeği ekip gibi puanlar. Getir:
  yerdeki yük köpeğin karesinde ya da dik komşusundaysa alır (yürüyerek almaz; uzman yükü köpeğe fırlatabilir). Kokla: kaçıştan
  önce, ileri geçidin üstünde ya da yanında, oda başına bir; rapor `Sniff` ile `AdvanceRaidRoom` sıradaki odanın planını açar
  (teyp kalıbı, K30). Kapıya girmez, GEÇ'te ekiple geçer, tesisin girişinde batı karesine yürürse tahliye olur (yükle çıkarsa
  yük çıkar; ekip çıkarken yükle tahtada kalırsa yük kaybedilir, köpek yaşar). Düşerse tahtadan çekilir, rapor "d", baskından
  sonra `facilityDogRestTurns` (3) vardiya dinlenir; köpek ölmez. Sahibi ölürse köpek depoya döner.
- **Sedye (K24, yeniden tanım):** "ekiple" gövde (Body 1) sonraki odada tahtaya konmaz, rapor onu taşır; son raporda ekipten biri
  ayakta bitirdiyse çıkarılır (Body 2), kimse ayakta değilse ölür. Ölçüm anahtarı `facilityStretcherAlways`. Sırtlayan unvanı
  (aşama 14) eski tanımı alır.
- **Bot:** bkz. §11.6 "Aşama 8 ölçümü" ve `Tools/DengeBotu/README.md` "Tesis ekipmanı ve ödül".
- **Ölçüm:** §11.6 "Aşama 8 ölçümü".

**Uygulanan hâl (aşama 9, 2026-11-19): ekipmanın sunumu.** Core, `BalanceConfig.asset` ve `SoundBank.asset` değişmedi; yeni
komut yok, sürüm 19, tesis izi f3. Bayrak asset'te kapalı; bayrak kapalıyken baskın ve sığınak sunumu eskisinin aynısıdır (tek
sapma: üç ekip kartı 345,33 → 345 px, en çok 0,67 px kayma; §14 on ikinci tur 44). Bitti ölçütü §12 satır 9. Tarif, depo ve
iş emri §5.6, ödül seçimi §6.2 "Uygulanan hâl (aşama 9)". Kararlar §14 on ikinci tur.

- **Giriş:**
  - Yeni tuş **R** (ekipman): seçili uzmanın kullanılabilir ekipman eylemlerinde döner (örme seti tel → kalas → kapalı;
    öbürleri tek eylem). E ve altı hamle değişmedi. Kart satırına tık R ile aynı döngüdür.
  - Tek kip açıktır: R hamle ve yetenek kipini, E ekipman kipini kapatır, kullanılabilir seçenek yokken de. Ekipmansız
    uzmanda, köpekte, tutsakta, eski tahtada ve rehberli baskında R etkisizdir.
  - 1–4 ekip kartlarını seçer: 4 köpeğin kartıdır, birden çok köpekte köpekler arasında döner.
- **Seçim:** uzman harfle (`Selected`), köpek ve tutsak birim kimliğiyle (`SelectedOther`). Tutsak tahtada tıkla seçilir;
  aygıt seçilmez, bilgisi hover'da ve tahta etiketindedir. Harf araması, son tur onayı, kaçış uyarısı, KAPIDA paydası, sonuç
  sayıları ve hover başlığı `IsSpecialist`'e süzülür.
- **Kartlar:**
  - Uzmanın yetenek kartında tek ekipman satırı (Row 1'in kopyası): ad, hak ("tel 1 · kalas 2 · 2 hak"), R fişi;
    kullanılamazsa nedeni.
  - Köpeğin ve tutsağın kartında yalnız E satırları (havla, getir, kokla; komuta), Q satırı yok; tutsağın kalan turu başlıkta.
  - Kart 10 satırı aşarsa kullanılamayan hamleler "Diğer · n hamle kullanılamaz" satırına katlanır.
  - Dördüncü ekip kartı (Card 4, 280 px) köpeğindir, yalnız tahtada köpek varken açılır. Ekip kartının eşya satırı
    "{ad} · {n} hak", hak bitince çizili; köpek düşünce "TAHTADAN ÇEKİLDİ".
- **Önizleme** (`FacilityGearView`; Core'un private aygıt kuralının public parçalarla kopyası, `FacilityGearViewTests` Core'un
  makine evresiyle eşler): taret ve vinç hattı, vincin çekeceğinin varışı (hidrolik yağda kayma dahil; yük turnikeye bakmaz),
  yemin yolu (yağda kayma dahil), havlamanın erimi (karo boyası), kelepçede hedefin niyetinin düşmesi. Kablo hasarı sayılmaz.
  Hover'da önce önizleme satırları gelir.
- **Tahta:**
  - Yeni parçalar: taret, yem, vinç, köpek; tel, kalas, hurda; kapalı ve kalkık kepenk, sandık nişi (kapalı/açık), tutuş
    krikosu. Hepsi var olan Analist mesh'leri ve Rev3 malzemeleriyle kurgu prefab'dır (`Desk/Prefabs/Raid/Gear`, yol E;
    Rev3 paketi 198 değişmedi). `RaidPieceLibrary`'de on iki alan; atanmamışsa Teknisyen piyonu × 0,6.
  - İşaretler (`RaidBoard3D.Marks.Devices/DecoyPaths/Bark/Tags`): aygıtın hattı, yönü ve kalan turu ("3 TUR"); KÖPEK;
    "TUTSAK · n" ve SON TUR (kırmızı); planı atlanan makinede NİYETSİZ; kilitlenen makinede KİLİTLİ (bu turun niyet oku
    durur); koklanan odanın geçidinde KOKLANDI.
  - Kırılan engelin yerine enkaz konmaz; düşen köpek ve kırık aygıt çizilmez.
- **Canlandırma:** on altı Fx dalı (`build`, `charge`, `gadget`, `turret-hit`, `decoy-step`, `winch-pull`, `scrap`,
  `device-break`, `capture`, `release`, `command`, `plate-give`, `bark`, `fetch`, `sniff`, `dog-down`); yeni ses yok.
- **Uyarılar:** yükü taşıyan köpek tahtadayken son tur onayında ve son uzmanın çıkış karesinde "Köpek yükle kalırsa yük
  kaybedilir".

## 6. Bölge YZ'leri

### 6.1 On iki YZ

On iki bağımsız YZ vardır; üstlerinde tek bir YZ yoktur. Her bölge paketi bir YZ'dir ve **bölge haritada YZ'nin adını
taşır**: görev fişi, harita etiketi, teleks ve raporlar "VERITY", "BOREALIS" der. Adlar İngilizce özel isimlerdir: bir
yapay zekâya verilebilecek marka adları. Sabittir ve iki dilde aynıdır; harita her harekâtta değişir, YZ'ler ve adları
aynı kalır. Bugünkü üretilmiş yer adları (kıyı ve iç bölge ad havuzu) kalkar.

**Uygulanan hâl (aşama 13).** Harita üreticisi g5 paketleri doğrudan YZ adıyla yazar. Eski imzaya göre komşu paket takası ve
iki ad havuzu çıktı. Tesis emri ve savunmanın imza kimliği `Simulation.RegionSignature` üzerinden gelir.

YZ'nin kimliği beş parçadır: adı, tesis şartı (bugünkü olay), imza makinesi, bölge özelliği (bugünkü) ve sunucusundan
çıkan ödül. Adlar, Katar ile Yalpa'nın paketi ve yeni makinelerin karışık dağılması (erken paketlerin de yeni makine
alması) karardır; hangi makinenin hangi YZ'ye verildiği ile tarif ve kazanım eşlemesi öneridir (b).

| Paket | Aşama | YZ adı | Tesis | Şart | İmza makinesi | Tarif (b) | Kazanım (b) |
|---|---|---|---|---|---|---|---|
| `weu` | Karargâh | VERITY | Banka veri merkezi | Söndürme bulutu | Zırhlı dron | Örme seti | Kulak: dinleme nöbeti 1 puan hızlı |
| `af` | Erken | CAIRN | Cevher hangarı | Çatlak tavan | Havan | Dinamit lokumu | Sedye: sürüklerken menzil azalmaz |
| `sa` | Erken | HEADWATER | Hidroelektrik santrali | Basınçlı tüp | Alev robotu (yeni) | Yaylı taret | Yedek çanta: örme ve dinamit +1 hak |
| `jk` | Erken | TESSEL | Çip fabrikası | Faz değişimi | Mühürleyici (yeni) | Köpek | Plan defteri: yeni cephenin ilk giriş odası bilinir |
| `me` | Orta | EMBER | Petrol rafinerisi | Yanıcı gaz | Patlayan robot | Yürüyen yem | Açık hat: dönüş odalarında +1 tur |
| `in` | Orta | SOLACE | Çağrı merkezi | Turnike | Kalkan dronu | Kumanda kelepçesi | Revir: taşınan düşenin yatışı 1 vardiya kısa |
| `sea` | Orta | EDDY | Elektronik fabrikası | Süzgeç emmesi | Çoğaltıcı | Makaralı vinç | Tezgâh: yeni tarifler 1 vardiya kısa (en az 1 vardiya) |
| `na` | Orta | WAYPOINT | Sevkiyat katarı (aşama 12; önce Dağıtım merkezi) | Katar (aşama 12; önce yük asansörü) | Tampon (yeni) | Söküm anahtarı | Eşik: giriş odasında +1 tur |
| `cn` | Geç | PARAGON | Montaj fabrikası | Kaynak kolu | Montajcı (yeni) | Yedek röle | Nişan: pusu hattı +1 kare |
| `eeu` | Geç | QUORUM | Devlet arşivi | Kayar raf | Kilitleyici | Delici şarj | Siper: savunmada saha rölesi +1 can |
| `ru` | Geç | BOREALIS | Kutup veri merkezi | Kriyo hattı | Kırağı dronu (yeni) | Çelik tel seti | Kürk: köpek +1 can |
| `oc` | Geç | HALCYON | Yüzer maden platformu (aşama 12; önce Maden limanı) | Yalpa (aşama 12; önce yağ sızıntısı) | Çapa robotu (yeni) | Çift zemberek | Makara: vinç 2 kare çeker |

- Aşama sütunu `WorldMap.PackageStage` değeridir: üretilmiş harita erken paketleri karargâhın yakınına koyar; temel
  araçlar bu yüzden önce, önlemlerin cevapları sonra gelir.
- Yeni makineler karışık dağılır: ikisi erken (alev, mühürleyici), biri orta (tampon), üçü geç (montajcı, kırağı,
  çapa) paketlerdedir (bölüm 6.5). Karargâh paketi bugünkü makinesini korur; rehberin geçtiği ilk üç baskın tanıdık
  kalır.
- Katar şartı WAYPOINT'in (`na`, orta aşama), Yalpa şartı HALCYON'un (`oc`, geç aşama) tesisindedir; iki tesisin adı ve
  kanat adları değişir. **Aşama 12'de uygulandı:** adlar bayraktan bağımsız değişti (yayındaki oyunda da; onaya açık S1), şart ise
  tesis bayrağına bağlı (§10 "Uygulanan hâl (aşama 12)").

### 6.2 Sunucu ödülü

- Bir bölgenin sunucu kanadı alınınca (bölge tamamlanınca) o YZ'nin ödülü açılır: oyuncu **tarifi** ya da **kazanımı**
  seçer. Öbürü o harekâtta hiçbir yolla alınamaz.
- Seçim operasyon raporunda yapılır ve sonraki baskın emrinden önce verilmek zorundadır
  (`ChooseServerReward`, bölüm 11.3).
- Bugünkü sunucu ödülü (fon, hammadde, alarm −4, bölge fonu ve güveni) aynen kalır; bu seçim onun üstüne gelir.
- Karşı saldırı bölgeyi geri alsa da ödül kaybolmaz; bölge yeniden alınınca ikinci ödül verilmez.

**Uygulanan hâl (aşama 8).** Bölge ilk kez tamamlanınca (`LiberationRegionState.Rewarded`), zaferle bitmeyen oyunda
`Facility.PendingReward` = bölge. `server_reward` (a bölge, b "tarif" | "kazanim") ödülü seçer; tarif `Facility.Recipes`'e,
kazanım `Facility.Perks`'e eklenir, bekleyen boşalır; RNG çekmez, fon defteri değişmez. Bekleyen ödül yalnız baskın emrini
kilitler (`LiberationRaidCheck`, fon denetiminden önce: "Önce sunucu ödülünü seç: tarif ya da kazanım."); vardiya, pano ve bölge
seçimi kilitlenmez (K21). Karşı saldırıdan sonra yeniden tamamlanan bölge ikinci ödül vermez; bölge fonu ve güveni de aşama 11'den beri (savunma açıkken) bir kez (§9.3).
Ödülü kuran baskın oyunu dağılmayla bitirirse bekleyen dolu kalır (oyun bitti; seçim reddedilir; zararsız, ama aşama 9'un son
ekranı "bekleyen ödül"ü göstermemeli; onaya açık, §14 on birinci tur 38). Kazanımlar (§6.1 tablosu, K25): Kulak gözetim +1
puan/vardiya; Sedye §5 (yeniden tanım); Yedek çanta örme ve lokum +1 hak; Plan defteri yeni bölge seçilince ilk kanadın giriş
odası bilinir (istihbarat harcamasından önce); Açık hat dönüş odası +1 tur; Revir tesisten çıkarılan düşenin yatışı −1 (en az 1);
Tezgâh ödül tarifleri −1 vardiya; Eşik gidişteki giriş odası +1 tur; Nişan pusu hattı +1; Siper savunmada saha rölesi +1 can (aşama 11); Kürk köpek +1
can; Makara vinç 2 kare. Kazanımlar ekip çapında, emirde `RaidParams.Perks`'e kopyalanır; baskın sürerken seçilen kazanım o
baskını değiştirmez (K23). Kazanım ile unvan toplanır (K26).

**Uygulanan hâl (aşama 9): ödül seçimi.** Bölgeyi tamamlayan baskının operasyon raporunda SONUÇLAR satırı "Sunucu ödülü · tarif
ya da kazanım", düğme ÖDÜLÜ SEÇ. Rapor dosyalanınca ödül kâğıdı ele gelir (sığınak olayı tutanağının görünümünün ikinci örneği,
`SiginakServerReward`): iki satır, TARİF (ad, tek satır açıklama, girdiler) ve KAZANIM (ad, açıklama). 1/2 + Enter ya da KARAR
kutusu tek komutu verir: `GameController.ExecuteBoardCommand(s => s.ChooseServerReward(bölge, seçim))` (`server_reward`). Teleks
Core'un sonucunu ve tarifte defterin yeni sayfasını basar.

- Esc, sağ tık ya da kâğıdın boş yerine tık kâğıdı EVRAK tepsisine koyar: tepsinin ışığı yanar, etiketi "EVRAK · SUNUCU ÖDÜLÜ"
  olur. Tepsiye ya da kâğıda tık geri getirir; açık olay tutanağı tepside önce gelir. Kayıttan devamda kâğıt animasyonsuz
  tepsidedir; rapor yeniden açılmaz.
- **Değişti (2026-11-22, `Docs/KAPI-BELGELERI-VE-YZ-MEKANLARI-TASARIMI.md` §A):** rapor dosyalanınca kâğıt ele gelmez, kapının
  yanındaki panoya iner ve kapı vurur; oyuncu KAPI'dan alır. Bırakılınca EVRAK tepsisine iner (yukarıdaki gibi). Kayıttan devamda
  kâğıt panodadır.
- Bekleyen ödül kilidi görev fişinde ("ÖDÜL SEÇİLMEDİ"), teleks gerekçesinde, CRT ipucunda ve telsiz günlüğünde görünür; fon kısa
  uyarısı onu örtmez. Hepsi tek yardımcıyı okur: `FacilityRewardText.Pending` (bayrak, kartlı oyun, oyun sürüyor, baskın yok).
  Son ekranda hiçbir yüzey bekleyen ödülü göstermez (on birinci tur 38; Core boşaltmaz). Kartsız oyunda Core'un kilidi işlemediği
  için kâğıt da gelmez.
- Kazanımlar görev fişinin KAZANIM satırında, Kulak gözetim önizlemesinde (+1). Cevap paketinin satırında temel tarif notu.
- Raporun ekipman satırı: "kelepçe harcandı (kişi) · köpek dinlenecek (3 V)". 7 yuva aşılırsa ödül satırından önce düşer.

### 6.3 Önlem defteri

YZ'ler oyuncunun araçlarını sayar ve en çok kullanılana önlem alır.

**Sayım.** Her baskın, araç sınıfı başına kullanım sayısını bildirir; baskın başına sınıf başına en çok
`facilityTallyCap` (3) (b) sayılır. Sayaçlar harekât boyunca birikir.

**Öğrenme.** Bir bölge tamamlandığında kalan YZ'ler bir kez öğrenir: sayacı en yüksek ve
`facilityCounterThreshold` (6) (b) ya da üstü olan sınıfın önlemi bir kademe artar (en çok 2), o sınıfın sayacı
sıfırlanır. 2. kademedeki sınıf seçime girmez. Eşitlikte en uzun süredir önlem alınmamış sınıf, o da eşitse tablodaki
sıra geçerlidir. Eşiği geçen sınıf yoksa önlem alınmaz. Öğrenme bölge başına harekâtta bir kez olur; karşı saldırıdan
sonra yeniden tamamlanan bölge ikinci kez öğretmez. Rastgelelik yoktur.

**Duyuru.** Alınan önlem teleksle bildirilir ve görev fişine ÖNLEM satırı olarak yazılır. Yapısal önlemin yeri bina
planında görünür; robota ait önlem odaya girince robotun üstünde görünür.

| Sınıf | Sayılan | 1. kademe | 2. kademe | Cevabı |
|---|---|---|---|---|
| `itme` | Koşu darbesi, vinç çekişi | Cıvatalı ayak: imza makineleri itilmez ve çekilmez | Bakım robotu da | Söküm anahtarı: Teknisyen sabotajı cıvatayı söker |
| `yikim` | Yıkılan duvar | Çift cidar: giriş ve ara odada iki duvar iki hak ister | Hedef odada da | Delici şarj: çift cidarı tek hakla açar |
| `orme` | Kurulan engel | Kesici kol: makineler tele +1 hasar verir (bakım robotu teli tek vuruşta keser) | Kalasa da +1 hasar | Çelik tel seti: tel +1 can |
| `ele` | Ele geçirilen robot | Kısa bağ: kelepçe 1 tur kısa | Robot süre bitince niyetsiz kalmaz | Yedek röle: kelepçe +1 tur |
| `kopek` | Köpek komutu | Koku yayıcı (nesne, 1 can): 3 kare içinde köpek komut alamaz | İki yayıcı | Yayıcı kırılır |
| `aygit` | Kurulan aygıt (pusu vuruşu yalnız ölçüm anahtarıyla; aşağıda, S1) | Parazit: aygıtlar 1 tur kısa çalışır | Makine aklı pusu hattını ve taret hattını görür | Çift zemberek: aygıtlar +1 tur |

Rol yetenekleri (sabotaj, EMP, kanca) sayılmaz: her baskında kullanıldıkları için sayaç oynayışı ayırt etmezdi ve ilk
önlem hep `itme` olurdu. Cıvatalı ayak yine de bu yeteneklerin itmesini ve çekişini keser.

Önlem kalan bütün YZ'lerde geçerlidir ve harekât boyunca geri alınmaz. Alınmış bölgede yeniden baskın (karşı
saldırıdan sonra) o anki önlemlerle oynanır.

Zorluk kademesi (`RaidTier`, özgür bölge sayısı) ve son bölgenin çekirdeği (`IsCore`) değişmez; önlem defteri bunların
yanına gelen ikinci eksendir.

**Uygulanan hâl (aşama 10, 2026-11-19).** Core'da; ana bayrak (`raidFacilityEnabled`) asset'te kapalı, alt bayrak
`facilityCountermeasures` (C# varsayılanı `true`; ana bayrak açıkken okunur) kapalıyken oyun aşama 9 ile bayt aynı (olay nişanı
hariç, aşağıda). Kayıt sürümü 19, tesis izi f4. Kurallar RNG çekmez (tek istisna aşağıda kısa bağın 2. kademesi, tahtanın kendi
akışında). Aşama 10 uygulama tasarımının açık soruları varsayılanlarıyla uygulandı, **onaya açık**: S1 pusu isabeti sayılmaz, S2 cevaplar ekip
düzeyinde, S3 sonuç teleksi yalnız tesis kipinde (§14 on üçüncü tur "Açık sorular").

- **Sayım (tahta, `Core/Baskin.Counters.cs`).** `BState`'te altı sayaç (`TallyPush … TallyGadget`); emirde `Counters` altı öğeyken
  (`MoveRules.Tally`) ve sınıfın emirdeki kademesi 2'den küçükken artar. Sayılanlar: koşu darbesi (bir birime değdi; cıvatalı zaten
  hedef değil), vincin **makine** çekişi (çekiş başına bir; gövde, yük ve ekip çekişi kurtarmadır, sayılmaz), lokumla yıkılan duvar
  (çift cidar da bir), kurulan tel ve kalas, kelepçelenen makine, havla/getir/kokla, kurulan taret/yem/vinç. **Pusu isabeti
  sayılmaz** (S1, K6); ölçüm anahtarı `facilityTallyAmbush=true` onu `aygit`'e sayar (tablonun lafzı). Rol yetenekleri (sabotaj,
  EMP, kanca, Madenci dinamiti) sayılmaz. Oyuncu hamleleri geri alınınca sayaç geri gelir; makine evresindeki (vinç, pusu) geri
  alınmaz. Oda raporunun kuyruk alanı `Tally` ("a:b:c:d:e:f").
- **Toplama ve öğrenme (simülasyon).** Her tesis baskınında (başarısız da) raporların sınıf başına toplamı `facilityTallyCap` (3)
  ile kesilip `Facility.Tally`'ye eklenir; 2. kademedeki sınıf eklenmez. Bölge ilk kez tamamlanınca (`!Rewarded`), `ChangeTrust`'tan
  sonra ve oyun bitmediyse (zaferde ve o baskının dağılmasında öğrenme yok) `FacilityCounters.Learn`: kademesi < 2 ve sayacı ≥
  max(1, `facilityCounterThreshold` (6)) olan en büyük sayaç; eşitlikte `Learned` günlüğünde son girdisi en eski (hiç girdisi
  olmayan en eski), sonra tablo sırası. Seçilen sınıfın kademesi bir artar, yalnız onun sayacı sıfırlanır, `Learned`'e (gün, bölge,
  sınıf, kademe) eklenir, `reward.Countermeasure` = "sınıf:kademe" (sunum; kayda girmez). Eşiği geçen yoksa önlem yok, bölgenin
  hakkı yine harcanır. Karşı saldırıdan sonra yeniden tamamlanan bölge öğretmez (o baskının sayımı yine eklenir).
- **Emir kopyası (K12).** `RaidParams.Counters` (defter açıkken hep altı öğe; öğrenme yokken sıfırlar ve tahtada sayımı açar) ve
  `RaidParams.Answers`, emirde ve görev fişi önizlemesinde (`LiberationSquadPreview`); süren baskın sonradan öğrenilen önlemle ya
  da seçilen cevapla değişmez. Bot ve sunum kademeyi emirden okur.
- **Cıvatalı ayak (`itme`).** `Baskin.Bolted`: kademe ≥ 1, ayaktaki makine, sökülmemiş, imza makinesi (dron ve bakım dışı her tür;
  aşama 13'ün makineleri kendiliğinden) ya da 2. kademede bakım robotu da. `Push` yer değiştirmez, çarpma hasarı yok, Fx `bolted` ve
  telsiz "{0} cıvatalı: yerinden oynamadı."; sabotaj, EMP, koşu ve tüp uçuşu hepsi `Push`'tan geçer (asıl hasar işler). Kanca hasar
  verir, çekmez; yağda kaymaz; koşu darbesinin hedefi değil. Vinç çekmez, hat orada biter (kural `Baskin.WinchPulls`; sunumun
  önizlemesi de onu okur); vinç her makine evresinde yeniden dener, Fx ve telsiz satırı aygıtın ömrü boyunca (en çok 3 evre) her
  evrede yinelenir (bilinen günlük gürültüsü; §14 on üçüncü tur 81). Tutsak cıvatasızdır, bırakılınca yeniden cıvatalı. **Söküm:**
  Teknisyen'in sabotajı (hasardan sonra, makine ayaktaysa) cıvatayı söker (`Unit.Unbolted`, Fx `unbolt`, telsiz "{0}: cıvatası
  söküldü.") ve aynı vuruşta iter; sökük makine dönüş tahtasında sökük kalır (`RoomMachine` 6. parça `:1`). Tutsakken sonraki odaya
  taşınan sökük makinenin sökükluğu taşınmaz (bilinen, nadir).
- **Çift cidar (`yikim`).** RNG'siz, tohumdan bağımsız seçim (oda üretimine girmez, Digest değişmez): odada `facilityDoubleWalls`
  (2) duvar, kademe 1'de giriş ve ara oda, 2'de hedef oda da; aday lokum hedefi olabilecek duvar, puan "zemin olsaydı" batıdan ileri
  geçide (hedef odada kalan hedeflere) BFS kısalması, eşitlikte odağa yakın, y, x. Lokum çift duvarı **tek hamlede**
  `facilityDoubleWallCharges` (2) hakla yıkar, yetmezse hedef değil (Delici şarj: `facilityPiercerCharges` 1); Fx `dwall`. Madenci
  dinamiti almaz. Dönüşte yıkılan moloz, yıkılmayan yine çift. 216 tahtanın (108 oda × olaylı/olaysız) %10'undan azında iki duvar
  seçilemez.
- **Kesici kol (`orme`).** Makine niyetinin hasarı (yakın vuruş, havan, patlama, atış) karede birim yokken tele (k ≥ 1) ve kalasa
  (k 2) + `facilityCutterDamage` (1); Fx `cut`, telsiz "Kesici kol {0} kesti." yalnız kırıldıysa. Atış teli geçtiği için tele
  değmez; itme çarpışması makine niyeti değildir. **Çelik tel:** tel canı +1 (4); kesici bakım (3) tek vuruşta kesemez.
- **Kısa bağ (`ele`).** `Baskin.ClampTurnsFor` = max(1, 3 − 1 + Yedek röle). 2. kademe: süresi biten tutsak niyetsiz kalmaz (aynı
  planda planlanır; tahta **bir `Rnd` fazla** çeker; kayıt tahtayı oynatmadığı için etkisiz).
- **Koku yayıcı (`kopek`).** Nesne `yayici` (1 can), her odada kademe kadar (1/2), RNG'siz yer: boş zemin, kapı/menfez/ayrılmış
  kare/şart parçası/olay karesi değil, batı karelerine Manhattan ≥ 3, konunca batıdan ulaşılan hiçbir kare ve hedef kopmaz; 1. odağa
  en yakın, 2. ileri geçide en yakın (hedef odada 1.'den en uzak). Erimde (Manhattan ≤ `facilityEmitterRange` 3) ayakta yayıcı
  varken köpek havla, getir ve kokla veremez (yürüyüş serbest; `Baskin.EmitterBlocks`). Yürüyüşü ve nişanlı atışı keser; makine
  bilerek vurmaz (−10). Kırılma mevcut nesne yolu (sabotaj, EMP, tutsak komutu, moloz, çarpma); Fx `break` (`Obj = "yayici"`).
  Dönüşte raporun nesnelerinden gelir (kırıldıysa yok). Açılış tahtasında makine sayısını ve çekimi değiştirmez (216 tahtada
  ölçüldü). Oyun sırasında kopya dolabının dik komşusundaki yayıcı uyanma adaylarını daraltır; bütün komşular doluysa o tur uyanma
  çekimi yapılmaz (bağımsız inceleme; kayıt tahtayı oynatmadığı için etkisiz; adaylardan dışlamak ölçümü değiştirirdi, yapılmadı).
- **Parazit ve makine aklı (`aygit`).** `Baskin.DeviceTurnsFor` = max(1, 3 − 1 + Çift zemberek); Fx `jam`. 2. kademe: makine
  planının başında `Baskin.DangerTiles` (ayaktaki uzmanların kurulu pusu hattı ve ayaktaki taretlerin hattı; oyuncunun görünen
  hatları) bir kez kurulur, hattaki varış karesi −`facilityCounterLinePenalty` (6); makine başına `Rnd` çekimi aynı.
- **Olay nişanı (A0, K3; alt bayrağa bağlı değil).** `EventAim`'in ekip değeri `TeamLike` ile (uzman, yürüyen yem, erimde havlayan
  köpek); aygıt, susan köpek ve tutsak 0; yağ (kaydırıp çarptırma) yalnız uzmana. Eski tahtada değerler aynı.
- **Plan kaplaması.** `Simulation.FacilityPlanCells` bugünkü kademelerle `Baskin.ApplyCounterCells` uygular: plan = emrin kurduğu
  tahta (648 tahtada sınandı).
- **Sunum (parça D; yalnız defter açıkken, bütün yüzeyler `FacilityCounterText.On` arkasında):**
  - **ÖNLEM satırı (K37):** görev fişi beş SAHA yuvasında kalır (`LiberationMapBuilder.SlipFieldsMax = 5`; sahne değişmedi). Boş
    yuva varsa ÖNLEM ayrı satırdır; beşi doluysa KAZANIM ile birleşik satır yazılır ("KAZANIM 1 · ÖNLEM 3", hover ikisini yazar).
    Ölçüm: altıncı satır BASKIN EMRİ VER'i 1080'de 29,5 px kadrajdan iterdi, birleşik satır +4,5 px. Glyph `ic_robot_s` (OLAY satırı
    `ic_bolt_s` kullanır), değer kırmızı; iki önlemin adları 24 harfi aşarsa değer "2 önlem", adlar hover'da. Görünür: önlem ya da
    sayaç varken; hover'da "YZ gözlemi: itme 4/6 · örme 2/6" (K38).
  - **ÖNLEM teleksi (K39):** `PlayReveal`'da "BÖLGE ÖZGÜR"den hemen sonra iki Alarm satırı ("YZ ÖNLEMİ" · "Cıvatalı ayak · 1. kademe ·
    İmza makineleri itilmez ve çekilmez" + "Kalan YZ'lerde geçerli · cevap: Söküm anahtarı"; köpekte "· yayıcıyı kır"); şeritte de.
    `SiginakMap.seenLearned` ilk bağlamada dolar: yeni oyun ve kayıttan devam basmaz, oyun bittiyse basılmaz, yeniden tamamlanan
    bölge girdi eklemediği için satır yok. Telsiz günlüğü (`RadioLogBuilder`) "YZ önlemi · {ad}", o günün baskın satırlarından sonra.
  - **Sonuç teleksi** §7.3, **kırmızı kalem** §7.1 "Uygulanan hâl (aşama 10)".
  - **Baskın tahtası (K42–K44):** cıvatalı makinede kırmızı **CIVATALI** etiketi (`RaidDataSource.GearMarks`; sökülünce kalkar).
    Hover: makinede "ÖNLEM · Cıvatalı ayak · itilmez, çekilmez" / "Cıvatası sökük"; tel (k1) ve kalasta (k2) "Kesici kol: makineler
    +1 hasar"; yayıcıda "Koku yayıcı: 3 kare içinde köpek komut alamaz · kırılınca biter" (küçük resim `thumb_emitter`); çift cidarda
    başlık "Çift cidar" + "ÖNLEM · Çift cidar · yıkmak {n} hak". Nedenler: köpek "koku yayıcı yakında", lokum "çift cidar: {n} hak
    gerekir", koşu "cıvatalı: itilmez" (Core'un hedef kuralını sökülmüş kopyada çalıştırarak; kopya kural yok). Gözcü satırları
    ("Gözcü · çift cidar: 1-C3, 1-D5", "· koku yayıcı: …", "· cıvatalı: Zırhlı dron 1-F2"). Önizleme süreleri Core'dan
    (`DeviceTurnsFor`, `ClampTurnsFor`); vinç hattının sonundaki cıvatalı makinede kırmızı "CIVATALI"; lokum önizlemesinde "Çift
    cidar: {n} hak harcanır". Canlandırma (yeni ses yok): `bolted` (kırmızı parlama, yerinde sarsıntı), `unbolt`, `dwall`, `cut`
    ("KESİCİ KOL +1"), `jam` ("PARAZİT · n TUR"); yayıcının kırılması mevcut `break` dalı. Çift cidarlı duvarın 3B işareti yok.
  - **Yayıcı parçası (K47, yol E):** `Desk/Prefabs/Raid/Gear/ScentEmitter.prefab` (kurgu; 304 üçgen) ve `RaidPieceLibrary.scentEmitter`
    (atanmamışsa soğutma ünitesi `emitterFallbackScale` ile).
  - **Alt bayraktan bağımsız sunum düzeltmesi (aşama 9 hatası):** kelepçenin kalan turu (`FacilityGearText.CaptiveLeft`) Core'un
    `ClampTurnsFor`'unu okur ve lokum önizlemesinin "Duvar yıkılır · kalan" satırı `GearLeft − bedel` yazar (önceden uzmanın yetenek
    hakkını yazıyordu). Ana bayrak açık, alt bayrak kapalıyken bu satır aşama 9'dan farklıdır (bağımsız inceleme 3).
- **Bot:** `RaidBot` iki terimle uyar (`Weights.Emitter`, `Weights.AmbushSeen`), `CampaignBot` `ODUL=onlem` ve `TAK=hic`
  (`Tools/DengeBotu/README.md` "Önlem defteri").
- **Ölçüm:** §11.6 "Aşama 10 ölçümü". Kararlar §14 on üçüncü tur.

### 6.4 Karakter gelişimi

Bugünkü deneyim (konu başına puan; 2, 6, 12 puanda kademe) ve saha etkileri (yürüyüş, güç, kısa yatış) aynen kalır.
Üstüne **unvan** gelir: kişi sahada yaptığı işle kalıcı bir özellik kazanır.

| Unvan | Kimlik | Kazanılır (b) | Etki (b) |
|---|---|---|---|
| Nişancı | `nisanci` | Pusudan vurduğu 3 baskın | Pusu hattı 4 yerine 6 kare |
| Sırtlayan | `sirtlayan` | Düşen birini tesisten çıkardığı 2 baskın | Sürüklerken menzil azalmaz |
| Koçbaşı | `kocbasi` | Koşu darbesi vurduğu 3 baskın | Koşu darbesi 3 kare iter |
| Köpekçi | `kopekci` | Köpekle çıktığı 3 baskın | Köpek turda iki komut alır |
| Kurucu | `kurucu` | Aygıt kurduğu 3 baskın | Kurduğu aygıt 1 tur uzun çalışır |
| Atıcı | `atici` | Yük fırlattığı 2 baskın | Fırlatma 5 kare |

- Bir iş baskın başına bir kez ve yalnız kazanılan baskında sayılır. Tekrar baskın ve bilerek kaybetme deneyim
  toplama yoluna dönüşmez.
- Kişi başına en çok `facilityMaxTitles` (2) unvan (b). Unvan kişiyle birlikte ölür.
- Uzmanlaşmış ekip güçlüdür ama aynı aracı çok kullandığı için önlem defterini de o besler.
- Karşı saldırıyla düşüp yeniden alınan kanattaki iş unvan için sayılmaz (bölüm 9.3).

**Uygulanan hâl (aşama 14).** `facilityTitlesEnabled`, tesis ana bayrağıyla birlikte iş sayaçlarını ve unvanların etkilerini açar.
Pusu isabeti, koşu teması, aygıt kurma ve yük fırlatma oda raporunda yapan kişinin sırasına yazılır. Sürükleme de yapan kişiyi
taşınan gövdeye bağlar. Son sonuçta gövde gerçekten çıkarıldıysa onu hareket ettiren kişiler Sırtlayan ilerlemesi alır.
Köpekçi ilerlemesi son raporda sahibiyle eşleşen ayakta köpekten gelir. Aynı iş oda ve tur sayısından bağımsız olarak kazanılan
baskın başına bir kez sayılır. Kazanılan savunma sayılır. Daha önce kazanılıp kaybedilen kanadın ikinci kazanımı ilerleme vermez.

İki unvan sınırında aynı anda tamamlanan eşikler üstteki tablo sırasıyla çözülür. Unvanı taşıyan yaşayan kişi sonraki baskın emrine
etkisini götürür. Nişan kazanımı pusu hattına ayrıca eklenir. Sedye odalar arasında taşınan gövdeyi, Sırtlayan ise sürükleyenin
yürüyüş bedelini düzenler. Köpekçi'nin ikinci komutu hareket hakkını yenilemez. Operasyon raporu yeni unvanı kişi satırında,
kişi dosyası adını ve etkisini gösterir. Nişancı'nın altıncı kareye isabeti gerçek baskın akışında sınanır.

### 6.5 Altı yeni imza makinesi

On iki YZ'nin her birinin kendi imza makinesi vardır. Bugünkü altı makine (zırhlı dron, havan, patlayan robot,
kilitleyici, kalkan dronu, çoğaltıcı) birer YZ'de kalır; altı yeni makine eklenir. Hepsinin niyeti öbür makineler gibi
bir tur önceden görünür; hiçbiri RNG çekmez. Değerler öneridir (b).

| Makine | Kimlik | YZ | Can · yürüyüş | Niyet | Kural (b) |
|---|---|---|---|---|---|
| Alev robotu | `alev` | HEADWATER (erken) | 2 · 2 | `flame` | Önündeki 2 kareye 1 hasar. Hasar nesnelere bugünkü darbe kuralıyla işler: değdiği basınçlı tüp ıslık çalar ve bir tur sonra uçar |
| Mühürleyici | `muhur` | TESSEL (erken) | 2 · 2 | `seal` | Bitişik boş zemine kalas duvar kurar (3 can). Bölüm 5.3'ün engel kuralları aynen geçerlidir: girişe, geçide, menfeze kurulamaz; ekibi girişten, ileri geçitten ve hedeften koparamaz; ekip onu kırabilir. Kurduğu kalas iz değildir, baskın bitince kalkar. Örülen engelin makine tarafındaki karşılığıdır |
| Tampon | `tampon` | WAYPOINT (orta) | 2 · 2 | `ram` | Düz hatta 3 kare koşar; çarptığı birimi aynı yönde 2 kare iter (itme kuralı: çarpma 1 hasar, kablo yakar). Katar'ın açık kenarında ekip üyesini düşürebilir |
| Montajcı | `montaj` | PARAGON (geç) | 2 · 2 | `build`, `melee` | PARAGON'un tesisinde devre dışı kalan makine karesinde hurda bırakır (engel, 1 can; iz değildir). Makine hurdası 5.2 ve 5.3'ün yer kurallarına uyar: giriş, geçit ve menfez karesinde ve ayaktaki bir ekip üyesini batı girişinden, ileri geçitten ya da hedeften koparacak karede hurda kalmaz (kaldırılır). Montajcı bitişik hurdayı (makine ya da aygıt hurdası) yeni bir drona çevirir (makine sınırı 7); onun kurduğu dron hurda bırakmaz. Hurda yoksa bitişik kareye 1 hasar |
| Kırağı dronu | `kirag` | BOREALIS (geç) | 2 · 3 | `frost` | Düz hatta 3 kare öteye atar: hedef kare ve 4 komşusu 2 tur buzludur; buzlu kareye giren birimin yürüyüşü orada biter |
| Çapa robotu | `capa` | HALCYON (geç) | 2 · 2 | `pull` | Düz hatta 4 kare içindeki ilk birimi 2 kare kendine çeker. Zemine çapalıdır: yatışta kaymaz |

Bugünkü altı makineden her biri bugün bulunduğu iki paketten birinde kalır: zırhlı dron VERITY'de, havan CAIRN'de,
patlayan robot EMBER'de, kalkan dronu SOLACE'ta, çoğaltıcı EDDY'de, kilitleyici QUORUM'da.

- İki makine (tampon, çapa) makinelere itme ve çekme kazandırır; bugün makinelerin böyle bir aracı yoktur
  (`BOLGE-OLAYLARI.md` açık riski). Kural tarafa bakmaz: tampon makineyi de iter.
- Uyanma ve ek makine kuralı aynen kalır (`WakeType`: imzalı bölgede %30 imza makinesi).
- Üretilmiş haritadaki "aynı imza düşmanlı iki paket komşu olamaz" kuralı gereksizleşir ve kalkar.
- Her yeni makine için model, makine aklı puanı, bot karşılığı ve rehber satırı gerekir (aşama 13).

**Uygulanan hâl (aşama 13, 20 Kasım 2026).**

- `Simulation.RegionSignature` emrin, savunmanın, haritanın ve görev fişinin ortak okuyucusudur. Tesis ana bayrağı ve
  `facilitySignatureMachines` ile yukarıdaki 12 imza çalışır. Alt bayrak kapalıyken altı ortak türün önceki dağılımı kullanılır.
- `Baskin.Signatures` niyeti, puanlamayı ve sonucu aynı hat hesabından üretir. Duvar, sis ve söndürme bulutu hattı kesebilir.
  Tampon ve çapa ortak itme, cıvata, çarpışma ve açık kenar kurallarını uygular. Çapa Yalpa'nın kaymasında yerini korur.
- Mühürleyici ve Montajcı'nın engelleri dönüşte aynı odada durur. Kalıcı iz defteri bunları dışarıda bırakır. Montajcı'nın
  kurduğu dron, sonraki odaya taşınırken de hurda istisnasını korur. Kırağı iki oyuncu turu yürüyüşü etkiler. Buzlu kare
  yürüyüşün son adımı olabilir. Makine hurdası ve kalas mevcut bağlılık denetiminden geçer.
- Tahta niyet hattını, hedef alanını ve buzlu kareyi gösterir. Makine kartı altı türün kuralını Türkçe ve İngilizce açıklar.
  Altı yeni parça prefabı var olan örgü ve `Rev3MaterialLibrary` malzemelerinden kuruldu. Motor, darbe ve donanım sesleri
  mevcut baskın ses kütüphanesinden gelir.



## 7. İstihbarat ve telsiz sunumu

### 7.1 Bina planı kâğıdı

Baskın emrinden önce görev fişinin yanında kanadın üç paftası durur. Bilinen odanın planı ozalit çizimdir:

- **Yapı:** duvarlar, kablo kanalları, giriş, geçitler, menfezler, kepenk ve donanım (pano, anten, vana).
- **Sabit nesneler:** hedef dolap, kopya dolapları, soğutma üniteleri ve sivil sunucular.
- **Tesis şartı:** şartın adı ve tahtaya koyduğu parçalar (tüp, kaynak kolu, raf bloğu, turnike, süzgeç, çatlak, meme,
  gaz sızıntısı ve bulut alanı, kriyo bölgesi, kablo kanallarının fazı) planda çizilir. Bunun için şart kurulumu oda
  üretimine taşınır (bölüm 2.4).
- **İzler:** bilinen odanın paftası kalıcı izleri (bölüm 9.1) ve YZ onarımını güncel gösterir (b).

Bilinmeyen odanın paftasında yalnız dış çerçeve ve "PLAN YOK" damgası vardır.

Planda robot, niyet, robot sayısı ve uyanma işareti yoktur. Kopya dolabının yeri plandadır; uyanma karesi dolabın
yanındaki karelerden biri olduğu için plan uyanmanın nerelerde olabileceğini daraltır, hangisinde olacağını söylemez.
Yapısal önlemler (çift cidar, koku yayıcı) plana kırmızı kalemle işlenir.

| Bilinen oda | Kâğıt |
|---|---|
| Hiçbiri | Üç pafta da boş çerçeve |
| Giriş | 1. pafta çizili |
| Giriş ve ara | 1. ve 2. pafta çizili |
| Üçü | Üç pafta çizili |

Planın işe yaradığı yer baskın öncesi hazırlıktır: hangi ekipman götürülecek (duvarlı koridor için dinamit, açık salon
için örme seti), menfez varsa Analist'in rolü, donanımın ve hedefin yeri.

Plan bilinmese de odaya girildiğinde tahta bugünkü gibi bütünüyle görünür. Robotlar ve niyetleri oda oda, ekip o odaya
girince görünür; sıradaki odanın robotları geçişe kadar bilinmez.

**Uygulanan hâl (aşama 5).**

- **Yer:** paftalar görev fişinin iç yanında, üst bantta yan yana durur; soldan sağa 1/3, 2/3, 3/3. Fişle kayar, fişle
  kapanır.
- **Kanat:** fişin bölgesinin sıradaki kanadı (`NextLiberationSector`).
  - Cephe fişinde paftalar hep görünür.
  - Bilgi fişinde paftalar yalnız o kanadın en az bir odası biliniyorsa görünür.
- **Bilinen pafta:**
  - Başlık "PAFTA n/3 · GİRİŞ / ARA ODA / HEDEF".
  - Çizim: yapı, sabit nesneler ve kademeye göre şart parçaları (`PartsAt(Tier)`; şart alanı kesik kenar ve kahve
    mürekkeple).
  - Antet: oda adı, altında olay adı (şart rengiyle) ya da donanım adı.
  - Üzerine gelme etiketi planı sözle verir; çizimin lejantı budur.
- **Bilinmeyen pafta:** çerçeve ve "PLAN YOK". Oda adı da gizlidir, çünkü türü ele verir.
- **Kaynak:** plan Core'un `FacilityPlanCells(room)` okumasıdır, yani baskın emrinin kuracağı tahtanın makinesiz kopyası.
  - Olaylar yalnız `liberationRegionEvents` açıkken çizilir.
  - Baskın tohumuna bakmaz, RNG çekmez.
  - `FacilityIntelTests` bunu 36 kanadın üç odası, kademe 1/3, olaylar açık/kapalı ve iki tohum için baskın tahtasıyla
    kare kare karşılaştırır.
- **Sonraya kalanlar:** izler ve YZ onarımı aşama 11'de geldi (mürekkep katmanında moloz taraması, kalkık kepenk, kırık donanımda çarpı, engel ve hurda işaretleri, onarım hover'ı; §9 "Uygulanan hâl (aşama 11)"), yapısal önlemlerin kırmızı kalemi aşama 10'da.

**Uygulanan hâl (aşama 10): kırmızı kalem.** Önlem defteri açıkken bilinen paftanın çizimine yedinci katman gelir: `Counter`
(`UILineGraphic`, `parts`'ın kardeşi ve üstünde, 1,75 px düz çizgi, ışın hedefi değil; bağ `FacilityPlanSheet.counter`; token
`plan.counter` #B3261A α 0,92). Çift cidarda duvar karesinin iç konturu ve üç çapraz tarama, koku yayıcıda halka, nokta ve Manhattan
eriminin sınırı (`FacilityPlanSketch.Counter`; `Outline` genelleştirildi). Veri `FacilityPlanSheetData.Counters` (Kind "cidar" /
"yayici") ve `CounterRange`; `FacilityPlanBuilder` plan hücrelerinden (`Cell.Double`, `O.K == "yayici"`; `Simulation.FacilityPlanCells`
emrin kuracağı tahtayı bugünkü kademelerle verir) doldurur. Üzerine gelme: "Önlem (kırmızı kalem): Çift cidar · bu duvar {n} hak
ister" / "… Koku yayıcı · 3 kare içinde köpek komut alamaz". Bilinmeyen paftada ve defter kapalıyken kalem yoktur (pafta aşama 9'un
aynısı).

### 7.2 İstihbarat kaynakları

| Kaynak | Kural (b) |
|---|---|
| Teyp | Ekibin girdiği her odanın planı kayda geçer ve harekât boyunca bilinir |
| Köpek | "Kokla" komutu sıradaki odanın planını çıkarır; kayda geçer |
| Dinleme nöbeti | Panoda Gözetim görevindeki kişi vardiya kapanışında 1 puan üretir; `facilityIntelCost` (1; 2026-11-24'e dek 3) puan, bulunulan bölgede sıradaki kanadın bilinmeyen ilk odasını açar: her vardiya bir oda |
| Evrak | Bir kanat alınınca aynı tesisin sıradaki kanadının giriş odası bilinir |
| Kazanımlar | "Kulak" ve "Plan defteri" (bölüm 6.1) |

Gözetim görevinin kimliği Core'da durur (`watch`). Bayrak kapalıyken panoda cebi yoktur ve görev emeklidir.

**Uygulanan hâl (aşama 5).**

- **Teyp:** `AdvanceRaidRoom`, girilen odayı `Known`'a ekler.
- **Evrak:** başarılı tesis baskınında kanat alınınca sıradaki kanadın giriş odası bilinir; 3. kanatta evrak yoktur.
- **Dinleme nöbeti:**
  - Kim sayılır: Gözetim'de çalışan kişi (yatan ya da ölü değil). Yatan kişi Gözetim'de bırakılamaz; plan basılmaz
    (öbür görevlerle aynı kural).
  - Ne zaman: `AdvanceBoardCare`'den sonra `AdvanceFacilityIntel` vardiya başına `facilityWatchIntel` (1) puan ekler.
  - Ne açar: `facilityIntelCost` (1) puan, sıradaki kanadın bilinmeyen ilk odasını açar (G, A, H sırası). Geliştirici
    (2026-11-24): "3 vardiyada bir harita değil, 1 vardiyada bir oda haritası": bedel 3 → 1. Yalnız denge değeri; kayıt
    dengeyi taşıdığı için eski oyunlar 3 bedelle sürer. Kulak kazanımıyla vardiyada iki oda.
  - Puan bedelde birikir. Açılacak oda yokken puan bir oda bedelinde durur.
  - Biriken puan yeni bölge seçilince ya da evrakla yeni hedef doğunca hemen harcanır.
- **Gözetim cebi:** kartlı panoda BOŞTA bandının sağ ucunda durur ve yalnız bayrak açıkken görünür.
  - Cebe bırakılan kart tek `board_assign(kişi, watch)` yazar.
  - Görünür metinler:
    - şerit: nöbet her vardiya bir paftayı açıyorsa (bedel ≤ kazanç) "Dinleme · PAFTA k", değilse (eski kayıt) "Dinleme n/3";
      açılacak plan yoksa "Dinleme · bitti";
    - üzerine gelme "her vardiya bir pafta · sıradaki: PAFTA k" (eski kayıtta "dinleme n/3 · sıradaki: PAFTA k");
    - önizleme kapanıştan sonraki hâli yazar: "kapanışta dinleme 0/3 → 1/3" ya da "kapanışta PAFTA k çıkar";
    - devir fişinde GÖZETİM satırı.
- **Sınırlar:** yeni komut yok, RNG çekilmez, kayıt sürümü 19 kalır. `Known` ve `Intel` komutlardan yeniden kurulur;
  hiçbir komutun sonucunu değiştirmez.
- **Bilinen boşluk:** dinleme yalnız sıradaki kanadı hedefler. Karşı saldırıyla geri düşen kanat tamamen bilinir; o
  sürede puan bir oda bedelinde bekler ve sonraki kanadın ara ve hedef odası açılmaz (§14 sekizinci tur, açık bırakılanlar).
  Bedel 1 olunca kanadın üç odası üç vardiyada biter; sonra görevli boşa çalışır.

### 7.3 Telsizi hissettiren ayrıntılar

Diegetik çerçeve bugün de yazılıdır: saha görüntü vermez, gözcü telsizden bildirir, durum masadaki makette tutulur.
Aşağıdakiler bu çerçeveyi güçlendirir; hiçbiri bilgi gizlemez.

| Ne | Kural | Yüzey |
|---|---|---|
| Gözcü raporu | Odaya girildiği anda bütün figürler ve niyetleri makettedir (bugünkü gibi). Rapor yalnız telsiz fişine koordinat diliyle satır yazar; figürlerin konmasını ve hamleyi geciktirmez | Telsiz fişi |
| Koordinat dili | Kare adına oda öneki eklenir: "2-C3" | Telsiz fişi, görev çipi, kayıt |
| Rapor evresi | Makine evresinin satırları bugünkü gibi telsiz fişinden akar; oda geçişi ve dönüş satırları eklenir | Telsiz fişi |
| Ses | Saha sesleri yalnız telsiz bandından gelir (bugünkü süzgeç); yeni saha sesleri aynı banda numaralanır | `AudioDirector.FromField` |
| "YAYINDA" lambası | Oyuncu turu bitirip emri ilettiğinde yanar, ilk rapor satırıyla söner | Baskın sahnesi |
| Makaralı teyp | Baskın boyunca makaralar döner; oda bitince "KAYIT · PAFTA n" etiketi yazılır. Kayıt, o odanın planının bilindiğinin görünür karşılığıdır | Baskın sahnesi; masada kayıt rafı |
| Sonuç teleksi | Baskın bitince sonuç teleksten de basılır. Bugün CRT "SONUÇ TELEKSTE" yazar ama satırı basan kod yoktur | Teleks (aşama 7'den aşama 10'a taşındı; §14 onuncu tur 17) |

Baskın yüklüyken komuta odası gizlidir; lamba ve teyp bu yüzden baskın sahnesinde durur. Masadaki kayıt rafı yalnız
baskın dışında görünür.

**Uygulanan hâl (aşama 5).** Lamba ve teyp baskın HUD'unun sağ alt köşesindeki telsiz masasındadır
(`RaidHud.prefab › Corners › Radio desk`). Masa yalnız tesis baskınında görünür.

- **Lamba:**
  - Yanar: EMRİ İLET ve GEÇ ile.
  - Söner: gözcünün ilk rapor satırıyla, efektsiz evrede evre sonunda, GEÇ'te oda dosyalanınca.
  - En az 0,4 s yanık kalır (`radioDesk.lamp.holdS`, yalnız görünümde).
- **Teyp:**
  - Makaralar baskın boyunca döner.
  - Etiket son kaydı yazar ("KAYIT · PAFTA n"); sayaç üç paftayı gösterir.
  - Kayıt, bu baskında dosyalanan ve planı bilinen odalardır. Evrak ya da dinlemeyle bilinen ama girilmemiş oda teypte
    yoktur.
  - Kayıttan devamda aynı kayıtlar raporlardan çıkar.
- **Ertelenen:** masadaki kayıt rafı (§14 sekizinci tur).

**Aşama 7.** Hamlelerin telsiz fişleri ve Core satırları makine evresinde de akar (pusu vuruşu, yük düşmesi, tutuşun boşa
gitmesi). Sonuç teleksi aşama 10'a (teleks işi) taşındı: hamlelerle ilgisizdir, aşama 10 zaten teleks duyurusu yapıyor.

**Uygulanan hâl (aşama 10): sonuç teleksi.** Yalnız tesis kipinde (`raidFacilityEnabled`; S3, onaya açık): bayrak kapalıyken
teleks dizisi eskisinin aynısı. `OperationReportBuilder.Telex(sim, log)` başarıda "SEKTÖR GERİ ALINDI" (Done) "{bölge} · {sektör} ·
{n} tur · ekip {a}/{t} ayakta" (+ "· {n} düştü", "· bölge özgür"), başarısızlıkta "GERİ ÇEKİLDİ" ya da "BASKIN BAŞARISIZ" (Failed);
sayılar `RaidPaperFacts.Result` ve ödülün kadrosundan. `SiginakOperation`'ın yeni bağı `telex` (= masanın `SiginakMap.telex`): satır
`Begin`'de kurulur, operasyon raporu inerken (kamera harita pozuna varınca) basılır; böylece şeritte de görünür (Begin anında kamera
masadaydı ve satır okunmuş sayılıp şeride düşmüyordu). `RaidLog` girdisi başına bir kez; kayıttan devamda basılmaz; Abort ve yeni
oyun bekleyen satırı siler. Şerit uzun satırı üç noktayla keser, tamamı kâğıtta.

## 8. Sahadaki sızmacı

Terim: kadrodaki YZ ajanı kodda ve metinlerde "sızmacı"dır (`AdmissionIdentity.Robot`). "Kopya" kelimesi kademe
özelliği ve kopya dolabı için kullanıldığından bu belgede kullanılmaz.

Saha alt bayrağı kapalıyken sızmacı saha rolüne atanabilir ve baskını sıradan bir uzman gibi oynar. Sahadaki sızmacının tasarıma girmesi ve
belirtileri insanın da verebilmesi geliştirici kararıdır (bölüm 14). Kural ayrı bayrakla (`infiltratorFieldEnabled`)
ve aşama 15'te uygulandı. Aşağıdaki eşikler C# varsayılanlarıdır.

- **Belirtiler.** EMP atışının çevresinde kalan kişi, o turun makine evresinde kilitlenir (gelecek tur yürüyemez).
  Köpek tur başında yanındaysa hırlar (kayda satır düşer). Belirtiyi iki tür kişi verir: sızmacı ve sığınakta iz
  bırakabilen insan (bugünkü `HumanTrace`, %20); ikisinde de yalnız iz gecikmesi 4 ve altı olanlar. Belirti şüphe
  üretir, kesinlik vermez.
- **Sessizlik.** Belirtisizlik de kesinlik vermesin diye belirti verebilecek kişilerin bir bölümü sahada belirti
  vermez: iz gecikmesi (`TraceDelay`, 2–6) 5 ya da 6 olan kişi, sızmacı da olsa iz bırakabilen insan da olsa,
  sessizdir (eşik (b)). Yeni çekim yoktur; değer aday üretilirken iki tür için de zaten çekilmiştir. Sessizlik yalnız
  sızmacıya uygulansaydı dosyasında iz olup sahada belirti vermeyen kişi kesin sızmacı olurdu. Böylece dosyadaki iz ile
  sahadaki belirti birlikte okunduğunda da kimlik çıkmaz: belirti veren de vermeyen de sızmacı ya da insan olabilir.
- **Geri almayla sınanamaz.** Belirti oyuncu turunda değil makine evresinde ya da tur başında görünür; hamle
  önizlemesinde (`Baskin.Preview`) gösterilmez.
- **Dönüş.** Sızmacı yalnız sunucu kanadının baskınında döner: baskın hedef odasına ulaşmışsa o odanın 2. turunun
  makine evresinde taraf değiştirir. İkmal baskınlarında normal bir kişi gibi davranır; yalnız belirti verir. Savunma
  baskınında, savunulan kanat sunucu kanadı olsa da dönmez; yalnız belirti verir (b). Sessiz sızmacı da döner;
  sessizlik yalnız belirtiyi keser. Döndüğü evrede eylem yapmaz: yalnız taraf değiştirir ve evre sonunda öbür
  makinelerle birlikte niyet seçer. İlk eylemi sonraki makine evresindedir; niyeti aradaki oyuncu turu boyunca
  görünür. İnsan dönmez.
- **Dönünce.** Kişi makine tarafına geçer: kendi rolünün yeteneğini ekibe karşı kullanır. Düşürülürse ortaya çıkar;
  baskından sonra bugünkü sabotaj akışına (tecrit, sorgu) girer. Düşürülmeden tesiste kalırsa sığınağa dönmez; kayıp
  sayılır ve ortaya çıkmış kabul edilir (b).
- **Sonuç dizinleri.** Dönen kişi sonuç listelerinde (`Hp`, `Armor`, `Left`) kadro sırasını korur ama kaçış ve "ekip
  düştü" denetiminde sayılmaz. `RaidResult` sonuna "döndü" ve "düşürüldü" bayrakları eklenir.
- **Rastgelelik yok.** Kimlik ve iz bırakma eğilimi bugünkü gibi aday üretilirken belirlenir; yeni çekim eklenmez ve
  çekim sayısı kimliğe bağlı değildir.
- **Gizlilik.** Kimlik ve belirti eğilimi `RaidParams`'ın `internal` alanlarında taşınır; sunum derlemesi okuyamaz.
  Tahtanın açık durumu, belirti görünene ya da kişi dönene kadar öbür kişilerle aynıdır. Gizlilik testi tahtaya
  genişletilir.

Kesinlik yalnız dönüşten gelir ve bedelsiz değildir: sunucu kanadının baskınında hedef odanın 2. turunu ayakta geçirip
dönmeyen kişi insandır, ama bunu öğrenmenin yolu şüpheliyi harekâtın en kritik odasına götürmektir; dönerse baskın
büyük olasılıkla kaybedilir ve yerde kalanlar ölür. Baskından çıkıp devam etmek odayı başından açtığı için görülen
belirti ve dönüş oyuncunun bilgisinde kalır (risk, bölüm 13).

[TOPLULUK-KADRO-VE-SIZMA-TASARIMI.md](TOPLULUK-KADRO-VE-SIZMA-TASARIMI.md) baskın içi sürprizi "vardiya bütçesine ve
taktik kontrole etkisi tanımlanmadan eklenmez" diye ertelemişti; bu bölüm o tanımdır.

**Uygulanan hâl (aşama 15).** Belirti için adayın kapıda çekilmiş değerleri kullanılır. EMP merkezinde veya dik komşusunda bulunan iz verebilen uzman, atış geri alınmazsa makine evresinde bir tur yürüyüşünü kaybeder. Yeteneğini kullanabilir. Köpek aynı odada tur başında dik komşuysa isimli telsiz satırı gelir. Yeni bir odaya giriş de tur başlangıcıdır.

Sunucu saldırısının hedef odasında ikinci makine evresini yaşayan her sızmacı taraf değiştirir. Önceden düşenler dönmez. Kısmen kapıda bekleyen ekip üyesi tahtaya döner, kabindeki kişinin tarafı kabinde değişir. Ekip tamamen geçip o evreyi oynamazsa dönüş tetiklenmez. Teknisyen sabotaj, EMP uzmanı EMP, Analist kanca kullanır. İlk niyet mevcut konumdan seçilir, ilk saldırı sonraki makine evresindedir. Köpek ve EMP belirtilerinin insan karşılığı vardır. Taraf değişimi operatörün kesin kanıtıdır.

Kadrodaki yeri sonuç kâğıdında korunur. Sahada etkisizleştirilen sızmacı sığınağa tecritte döner. Açık tutanak varsa sırasını bekler. Oyuncu sorgu veya söküm kararını mevcut olay kâğıdında verir. Tesiste kalan ya da Katar'ın açık kenarından düşen sızmacı kalıcı kayıptır. Her iki durumda da kişi dosyası ifşa edilmiş sayılır.

## 9. Kalıcı tahta ve savunma baskını

### 9.1 Odada kalanlar

| İz | Kaynağı | Sonraki girişte |
|---|---|---|
| Yıkılan duvar | Dinamit, olaylar | Zemin |
| Örülen engel | Örme seti | Yerinde, kalan canıyla |
| Kırılan donanım | Baskın | Kırık; hazırlık etkisi sürer |
| Kırılan soğutma ünitesi, kopya dolabı | Baskın | Yok |
| Hurda | Süresi biten ya da kırılan aygıt | Engel, 1 can |
| Açılan kepenk | İki kişilik iş | Açık |

Robotlar, sivil sunucular, hedef dolap, şart parçaları ve şartın oynattığı değişiklikler (kayan raf, enkaz, patlayan
tüp), düşen kişi ve yük iz değildir: her baskında yeniden kurulur; şart parçaları plandaki yerine konur.

- **YZ onarır.** İşgaldeki her kanatta vardiya kapanışında bir iz onarılır (`facilityRepairPerShift`, 1) (b): en eski
  iz önce. Beklenirse hazırlık erir. Alınmış kanatta onarım yoktur.
- **Nerede işe yarar.** Başarısız baskının tekrarı aynı kanadın aynı üç odasına döner; açılan delik, kurulan tel ve
  kırılan donanım yerindedir. Karşı saldırıdan sonra geri düşen kanada yeniden girişte de aynıdır. İstisna: savunmada
  terk edilen odanın izleri, savunmanın sonucu ne olursa olsun silinir (bölüm 9.2); bu, vardiya onarımından ayrıdır.

### 9.2 Savunma baskını

Bugün alarm eşiğe varınca son alınan sektör kendiliğinden geri düşer. Yeni kural (`facilityDefenseEnabled`):

1. Alarm eşiğe varınca karşı saldırı **bekler**: hedef, bugünkü kuralla son alınan kanattır. Teleks ve harita bunu
   gösterir. Alarm bugünkü gibi o anda sıfırlanır. Alınmış kanat yoksa saldırı beklemez; bugünkü sonuç (yalnız
   güven −1) hemen uygulanır. Bekleyen saldırı varken ikincisi kurulmaz.
2. Sonraki vardiyada oyuncu iki komuttan birini verir:
   - **Savun** (`DefendSector`): o vardiyanın baskın hakkını kullanır, fon istemez. Kadro kuralları baskınla aynıdır.
   - **Bırak** (`YieldSector`): bugünkü sonuç uygulanır (kanat geri düşer, güven −1). Baskın hakkını kullanmaz (b).
     Düşen kanat mevcut bölgedeyse o vardiya o kanada baskın da verilemez (bölüm 9.3); hakkın korunması yalnız kanat
     başka bir bölgedeyken anlam taşır.
3. Karar vardiyası, saldırının kurulduğu vardiyadan sonraki vardiyadır. O vardiya karar verilmeden kapatılırsa "bırak"
   uygulanır. Vardiya ortasında (başarısız baskın, konvoy kararı) kurulan saldırı aynı vardiyanın kapanışında düşmez.
4. Karar vardiyasında baskın emri verilemez; önce Savun ya da Bırak verilir (b). Saldırının vardiya ortasında
   kurulduğu vardiyada baskın emri serbesttir; Savun ve Bırak o vardiyada verilemez.

**Savunma kanadın üç odasında oynanır.** Makineler dışarıdan içeri ilerler; ekip oda oda içeri çekilerek savunur.
Odalardaki izler yerindedir: önceki baskında açtığın delikler makinelere yol verir, ördüğün engeller onları tutar.

| Kural | Açıklama (b) |
|---|---|
| Başlangıç | Ekip giriş odasında, doğu geçidinin önünde başlar. Saha rölesi (`role`, 4 can) hedef odasında, eski hedef dolabın yerindedir |
| Barikat turu | 1. tur makinesizdir: ekip yerleşir, engel örer, aygıt kurar |
| Dalgalar | Üç dalga: 2., 5. ve 8. tur başında tesisin girişinden yola çıkar. Büyüklük 1., 2. ve 3. kademe için 2, 3, 4 makinedir |
| Dalganın gelişi | Ekip giriş odasındaysa dalga aynı tur batı girişinden girer. Ekip içerideyse dalga, terk edilen her oda için 1 tur geç, ekibin bulunduğu odanın batı geçidinden girer |
| Sayaç | Savunmada tek tur sayacı vardır; oda değişince sıfırlanmaz (bölüm 2.4'ün oda sayacı savunmada geçerli değildir). Süre ve dalga takvimi bu sayaçla işler. Tesis şartının takvim adımları ve tetikleri savunmada işlemez; şart parçaları plandaki yerinde durur, nesne olan parça sıradan nesnedir |
| İçeri çekilme | Ayaktaki bütün ekip doğu geçidindeyken GEÇ ile bir iç odaya çekilir; hedef odadan daha içeri çekilinmez. Savunmada kapı sığınak değildir: ayaktaki bütün ekip kapıdayken tur yalnız GEÇ ile biter, tur sonu verilemez (tur içi geri alma serbesttir). GEÇ o turu bitirir: iç odadaki ilk tur sonraki turdur ve makinesizdir (yeni barikat turu). Terk edilen odadaki sağ makineler ondan sonraki turun başında gelir |
| Terk edilen odanın bedeli | Savunma kazanılsa da kaybedilse de terk edilen her oda için güven `facilityDefenseRoomTrust` (1) düşer ve o odanın izleri silinir (YZ odayı yeniden kurar). Girişi tutmak en iyi sonuçtur |
| Geri dönüş yok | Terk edilen odaya dönülmez. Orada kalan düşen ölür; kurulu aygıt ve engel makineleri oyalamaz (oda oynanmaz) |
| Röle | Makineler röleyi yalnız ekip hedef odasındayken hedef alır. Ekip giriş ya da ara odadayken makineler ekibi geçip röleye ulaşamaz |
| Süre | `facilityDefenseTurns` (10): barikat turu ve üçer turluk üç dalga |
| Makine yerleşimi | Dalga makinesi ve takipçi boş giriş (iç odada batı geçidi) karesine, kare doluysa o kenardaki en yakın boş kareye konur; aynı tur gelirlerse önce takipçiler, sonra dalga; yer yoksa ya da makine sınırı (`MaxEnemies`, 7) doluysa sonraki tur başına kalır |

- **Kazanma.** Süre sonunda ekipten en az biri ayaktadır ve röle kırılmamıştır: kanat tutulur. Giriş odası
  tutulduysa güven düşmez; bir oda terk edildiyse güven −1, iki oda terk edildiyse −2 olur.
- **Kaybetme.** Ekip düşer, röle kırılır ya da baskın içinde geri çekilme emri verilir (bölüm 3.6): kanat geri düşer,
  güven −1. Terk edilen oda başına güven kaybı buna eklenir: bir oda terk edildiyse toplam −2, iki oda terk edildiyse
  −3. Kaybetmek hiçbir durumda kazanmaktan az güven götürmez.
- **Düşenler.** Savunmada çıkış yoktur. Kazanılırsa ekibin bulunduğu odada yatanlar tesisten çıkarılmış sayılır
  (3 vardiya yatış). Kaybedilirse ya da geri çekilme emri verilirse yerde yatanlar ölür; içeri çekilirken iç odaya
  sürüklenmeyen de ölür.
- **İçeri çekilmenin anlamı.** Çekilmek yer verip zaman kazanmaktır: sıradaki dalga geç gelir, ekip temiz bir odada
  yeniden barikat kurar. Bedeli: terk edilen oda başına güven ve o odanın izleri, geride kalan düşen ve aygıtlar, son
  odada rölenin açığa çıkması.
- **Deneyim.** Savunmadan dönen kişi yalnız kazanılan savunmada rol deneyimi alır; "Savun ve ilk turda geri çekil"
  bedelsiz deneyim yolu olmaz.

Başarısız bir baskın alarmı eşiğe çıkarırsa karşı saldırı o anda işlemez; bekleyen duruma geçer.

### 9.3 Yeniden alınan kanat

Bugünkü kod, karşı saldırıyla düşüp yeniden alınan kanatta bütün ödülleri yeniden verir; bu, savunmayı bırakıp kanadı
yeniden almayı kârlı yapar. Yeni kural:

- Yeniden alınan kanat baskının kendi ganimetini (fon, hammadde, erzak) verir.
- Bölge fonu (`liberationCaptureFunds`, 15) ve bölge güveni (`liberationTrustPerRegion`, 4) bölge başına harekâtta bir
  kez verilir; bölge ikinci kez tamamlanınca verilmez.
- Sunucu ödülü (tarif ya da kazanım) ve önlem defterinin öğrenmesi de bir kezdir (bölüm 6.2, 6.3).
- Yeniden alınan kanattaki iş unvan için sayılmaz.
- "Bırak" verilen vardiyada o kanada baskın emri verilemez (b).

Bugünkü güven dengesi yeniden verilen ödülle ölçülmüştür; aşama 0 tabanı bu kuralla yinelenir. **Sonuç (aşama 17):**
yinelendi, kural tabanı değiştirmiyor (zafer +1,25 ±1,25 puan, ölü/harekât 6,66 → 6,58, zafer medyanı 48 → 48);
hedef tablosunun Taban sütunu geçerli (§11.6 "§9.3 ve aşama 11 K3").

**Uygulanan hâl (aşama 11, 2026-11-20).** Core ve sunumda; ana bayrak (`raidFacilityEnabled`) asset'te kapalı. İki alt bayrak (C#
varsayılanı `true`, ana bayrak açıkken okunur, asset'e yazılmaz): `facilityMarks` (izler, onarım, planda ve emirde iz;
`Simulation.MarksOn`) ve `facilityDefenseEnabled` (kuşatma, iki komut, üç odalı savunma, §9.3; `Simulation.DefenseOn`). İkisi kapalıyken
oyun aşama 10 ile bayt aynıdır (`t11K0` = `t10donus`, 24/24). Kayıt sürümü 19, tesis izi f5 (Digest aynı; §11.4 "Aşama 11'de kayıt").
Yeni `S.Rng` çekimi yok. Aşama 11 uygulama tasarımının açık soruları varsayılanlarıyla uygulandı, **onaya açık**: S1 saldırı baskınında
savunma kancası yok, S2 "bırak" kuralı yukarıdaki lafızla, S3 hedef tutmazsa yalnız yeni savunma alanları ayarlanır, S4 kapı kuralı
genişletildi (§14 on dördüncü tur "Açık sorular"). **Bitti ölçütünün ölçüm yarısı tutmadı** (savunma başarısı %100; §11.6 "Aşama 11
ölçümü"); aşama "oynanır" yarısıyla kapandı, denge değişmedi (§14 on dördüncü tur 78, 111).

- **İzler (§9.1, `Core/FacilityMarks.cs`).** `FacilityState.Marks` (`FacilityMark`: oda, tür, kare, can, gün; liste sırası = onarım
  sırası). Sekiz tür: `duvar` (yıkılan duvar zemin olur, moloz rafı durdurur), `kepenk` (açık), `donanim` (kırık; hedef odası hazırlığı
  sürer), `cool`, `spawner` (yok), `tel`, `kalas`, `hurda` (kalan canıyla). `raidres`'te (saldırı, başarısız baskın ve savunma) oynanan
  her odanın ziyaretlerinden türetilir (`Derive`, `Merge`): kalıcı türler ziyaretlerin birleşimidir ve yerinde kalır (gün ve sıra
  korunur), nesne türleri son ziyaretten gelir (kümede olmayan silinir: makine ya da oyuncu kırdı, uygulanamadı). İz değil: makine,
  sivil, hedef/röle, şart parçası, düşen, yük, koku yayıcı, büyük sandık. Emirde `RaidParams.Marks`'a kopyalanır (`WingMarks`); ilk
  ziyaret tahtasında (önlem kaplamasından sonra), savunma tahtasında ve bina planında (`FacilityPlanCells`) aynı saf fonksiyonla
  (`FacilityMarks.Apply`) uygulanır; dönüş tahtası uygulamaz. Önce kalıcı türler, sonra engeller. Engel; kapı, geçit, menfez karesine,
  dolu kareye ve şart parçası karesine konmaz; batı girişi ↔ ileri geçidin **her** karesi ↔ odak (H'de hedef/röle, G/A'da donanım) bağını
  kesiyorsa atlanır (lafızdaki "hedefin bir komşusu"ndan geniş; §14 on dördüncü tur 43). Duvar izi kepenk ve sandık nişine uygulanmaz.
  Emirdeki `donanim` izi hedef odası hazırlığına girer (`RaidChain.MarkedDevices`).
- **Onarım.** Her vardiya kapanışında işgaldeki her kanatta listede en önde duran `facilityRepairPerShift` (1) iz silinir ve
  `FacilityState.Repairs`'e (gün) kopyalanır (`AdvanceFacilityRepairs`); alınmış kanatta (bekleyen kuşatmanınki dahil) onarım yok; aynı
  gece düşen kanat onarılır.
- **Kuşatma (§9.2 madde 1–4, `Core/Simulation.Siege.cs`).** `ChangeAlarm` eşikte savunma açıkken `Counterattack` yerine `BeginSiege` kurar:
  hedef son alınan kanat (`PickCounterTarget`); `FacilityState.Sieges` günlüğüne girdi (kanat, bölge, kurulduğu gün, **karar vardiyası**
  `Due`, sonuç `Outcome` "" / `savun` / `tutuldu` / `dustu` / `birak` / `kendi`, kapanış günü, terk). Günlük kuşatmanın tek kaynağıdır
  (bekleyen saldırı, savunma tohumu, "bırak" kilidi, sunum, bot). Kapanışta kurulanın karar vardiyası ertesi vardiya (artmış gün),
  vardiya ortasında kurulanınki bir sonraki (`stepping` bayrağı). Alınmış kanat yoksa ya da bekleyen/süren kuşatma varken kuşatma
  kurulmaz: güven −1, sayaç artar ve `LastCounterattack` boşalır (aşama 10'un kanatsız karşı saldırısıyla aynı sonuç; §14 on dördüncü tur
  7). Dört yol (`StepAlarm`, başarısız `raidres`, `event_decide` (konvoyun eşik reddi bugün zaten var), kartsız kapı cevabı) kendiliğinden
  kapsanır; kartsız kapının ziyareti kuşatmayı bilir (`DoorVisit.Siege`; kuşatmada sıfırlanan alarm etki diye yazılmaz).
  `S.Counterattacks`/`LastCounterattack*` yalnız kanat düşünce yazılır. Karar vardiyasında baskın emri kilitli ("Önce karşı saldırıya
  karar ver: savun ya da bırak."); `siege_defend` (`DefendSector`) ya da `siege_yield` (`YieldSector`); karar verilmezse kapanışta
  `StepAlarm`'dan önce kendiliğinden bırakılır (`kendi`, `AdvanceFacilitySiege`). Komutla bırakılan kanada o vardiya baskın verilmez;
  kendiliğinden bırakma kilitlemez.
- **Savunma emri.** Fon yok, `Attempts` artmaz, `S.Raids++`, `RaidParams.Mode = "defense"`; tohum `Rng.Mix(S.Seed, 0x53494547 + günlük
  dizini)` (`S.Rng`'den çekilmez); saldırıyla ortak gövde `OrderRaid`; pano kilidi ve EMP hücresi baskındaki gibi; bekleyen sunucu ödülü
  ve pano denetimi (`PlanningRaidCheck`) savunmayı da kilitler; kartsızda `SquadCanRaid`.
- **Savunma tahtası (`Core/Baskin.Defense.cs`).** Aynı zincir API'si (`OpenRaidRoom`, `RaidRoomReport`, `AdvanceRaidRoom`,
  `FacilityRaidResult`, `ResolveRaid`); `RaidChain.SetupDefense` → `Baskin.DefenseBoard`. Tek tur sayacı (`facilityDefenseTurns` 10;
  `RaidChain.Result.Turns` son raporun turu), ilk odada ekip ileri geçidin önünde, her odanın ilk turu barikat turu (makinesiz), üç dalga
  2/5/8 (büyüklük `facilityDefenseWaveBase` (1) + min(kademe, 3) = 2/3/4; türler dalga başına ayrı akıştan `Rng.Mix(p.Seed, 0x57415645 +
  k)`, aynı dalga hangi odaya gelirse gelsin aynı makineler), geliş tek formülle `max(D + terk, S_oda + 1)`, dalga tek odaya gelir,
  takipçiler (terk edilen odanın sağ makineleri) sonraki tur başında dalgadan önce; GEÇ'te gelmemiş dalga sonraki odaya kayar, takipçiler
  ve gelmiş ama yerleşmemiş makineler **her zaman** taşınır (§14 on dördüncü tur 47). Yerleşim batı girişi sırasıyla, doluysa batıdan
  yürüme uzaklığı en küçük zemin; yer ya da makine sınırı (7) yoksa kuyruk. Saha rölesi (`role`; hedef odasında hedef dolabın yerinde,
  4 can, Siper +1; `IsTarget` değil, her kaynak kırar, kırılınca kayıp); uyanma, şart takvimi, hazırlık ve `kisa` yok (şart parçaları
  yerinde); makine aklı röleye yönelir (vuruş 11, patlamada +6, konumda −0,25/−0,1 × uzaklık, `Smart`'tan bağımsız). **Kapı sığınak
  değildir** (S4): bütün ayaktakiler kapıdayken tur yalnız GEÇ ile biter; kısmen kapıda bekleyen uzman tur sonunda tahtaya döner
  (`ReturnFromGate`, Fx `gate-out`); son turda içeri çekilinmez. Kazanma 10. tur sonunda ekipten biri (tahtada ya da kapıda) ayakta ve röle
  sağlam ("Kanat tutuldu."). Okuyucular: `Baskin.IsDefense`, `DefenseWaves`, `DefenseIncoming`, `RelayHp/RelayMax`, `BarricadeTurn`,
  `CanEndTurn`, `Abandoned`, `StartTurn`; Fx `wave`, `spawn`, `gate-out`, `relay-break`.
- **Sonuç (`ResolveDefense`).** Güven −`facilityDefenseRoomTrust` (1) × terk (kazanma) ya da −1 −1 × terk (kayıp); sivil kaybı baskındaki
  gibi; alarm, fon, ganimet, evrak, ödül, öğrenme ve ayakta kalan sivilin güveni yok; önlem sayımına girer; deneyim yalnız kazanınca
  (`ReturnPlanningSquad(experience)`); kazanınca ekibin odasında yatan ve ekiple gelen çıkarılır (`RaidChain.IsExtracted` savunma dalı),
  kayıpta ve terk edilen odada yatan ölür; terk edilen odanın izleri sonuçtan bağımsız silinir; günlük girdisi `RaidLogEntry.Mode =
  "defense"`; reward `Defense, Held, Abandoned, MarksNew/Gone/Wiped`. `RaidResult.Encode`'a alan eklenmedi (§11.3).
- **§9.3.** Savunma açıkken bölge fonu ve bölge güveni bölge başına bir kez (`Rewarded`; `Simulation.RegionFundsDue`); yeniden tamamlanan
  bölge `reward.RegionRepeat`. Savunma kapalıyken aşama 10'daki gibi her seferinde.
- **Siper.** Savunmada saha rölesi +`facilityCoverRelayHp` (1) can (`RaidChain.DefenseRelayHp`); kazanım metni dengeden.
- **Sunum: masa (parça P4a; bütün yüzeyler `FacilityDefenseText.On` arkasında, metinler `Desk/Scripts/Bridge/Siginak/FacilityDefenseText.cs`).**
  - **Duyuru:** kartsız vardiya raporu kutusu "KARŞI SALDIRI BEKLİYOR" (`ShiftReportInput.siege*`), kendiliğinden bırakmada "YZ KARŞI
    SALDIRISI · … kanat bırakıldı (karar verilmedi)"; kartlı oyunda `SiginakMap` kuşatma günlüğünü izler ve teleksi birer kez basar (yeni
    oyun ve kayıttan devam basmaz); operasyon raporunda başarısız baskının kurduğu kuşatma satırı; alarm panosu hover'ı; kartsız kapının
    teleksi "Karşı saldırı bekliyor" (`DoorVisit.Siege`; kapanış).
  - **Harita:** kuşatılan sektörün ✓ işareti tehdit renginde nabız atar; cephede "KUŞATMA" damgası (özgür bölgede yok); bırakınca
    bugünkü karşı saldırı dizisi (✕, YENİDEN İŞGAL).
  - **Görev fişinin kuşatma kipi** (`LiberationMapBuilder.SiegeSlip`): "KARŞI SALDIRI · OP-nn", "SAVUNMA · nn SEKTÖR", görev cümlesi ve
    süre dengeden (`Simulation.DefenseTurns`), BEDEL "fon yok · baskın hakkı", ödül yok; SAHA: BÖLGE, İMZA, DALGA ("{n} dalga · {k}
    makine", hover geliş turları `Simulation.DefenseSchedulePreview`), İZ, RÖLE; SAVUN (Enter; kadro raporu "SAVUNMAYA ÇIK" →
    `GameController.DefendSector`) ve BIRAK (`OperationSlip.yieldAction`, yalnız tık, iki tıkla: "EMİN MİSİN? · güven −1" →
    `GameController.YieldSector`). Karar vardiyasında cephe fişinin BASKIN EMRİ VER'i Core gerekçesiyle kapalı ("ÖNCE KARAR VER"); 3 ve
    vardiyanın ilk harita dönüşü kuşatma fişini açar. Kuşatma kipinde ödülsüz ÖDÜL satırı kalkar (1080'de BIRAK kadrajda).
  - **Bina planı:** paftalar savunulan kanadın (`FacilityPlanBuilder.Build(…, sectorId)`); izler mürekkep katmanında (yıkık duvarda moloz
    taraması, kalkık kepenk, kırık donanımda çarpı, tel/kalas `PlanItem.Barrier`, hurda `PlanItem.Scrap`); hover "İz: …", "YZ onardı ·
    V.012: …" (`Repairs`), savunulan kanadın giriş ve ara odasında "Terk edilirse izler silinir."
  - **CRT, telsiz günlüğü, devir fişi, ÖN KONTROL, pano kilidi:** "KARŞI SALDIRI BEKLİYOR · SAVUN YA DA BIRAK", "GÖREV: SAVUNMA"; "{ekip}
    döndü · {yer} tutuldu|düştü", "YZ {yer} onardı"; devir fişinde ilk kutu `SlipWarningKind.Siege` "KARŞI SALDIRI · Karar verilmezse kanat
    bırakılır (güven −1)."; ÖN KONTROL "karşı saldırıya karar verilmedi: bırakılacak"; pano kilidi "savunma emri verildi".
  - **`RaidLog` tüketicileri** (`Mode == "defense"`): operasyon raporu "KANAT TUTULDU"/"KANAT DÜŞTÜ" (KAZANÇ yok, terk satırı, deneyim eki
    yalnız tutulunca), sonuç teleksi ("SEKTÖR GERİ ALINDI" basılmaz), defter, final raporu (savunma baskın sayısına ve başarıya girmez),
    CRT, pano kilidi, hammadde.
- **Sunum: baskın sahnesi (parça P4b; metinler `Desk/Scripts/Bridge/FacilityDefenseRaidText.cs`).** Antet "KANAT nn · PAFTA n/3 ·
  SAVUNMA"; tur şeridi 10 hücre (`RaidHud.prefab` Cell 9/10; 8'den çok turda hücreler 8 hücrelik genişliğe daralır, `RaidHudView.FitCells`)
  ve dalga turlarında "D" işareti (gecikme dahil, Core `DefenseWaves`); barikat turunda TELSİZ fişi "SAVUNMA · BARİKAT · makine yok";
  amaç "Amaç: 10. tura dayan" (hedef odasında "· röleyi koru") ve dalga satırı; hedef odasında "Saha rölesi {h}/{m}" (`thumb_relay`);
  gözcü satırları; dalga gelişi Core `spawn` + `wave` popup "DALGA n", makine etiketi "GELDİ"; sonraki tur gelecek makineler
  `DefenseIncoming` kareleriyle; GEÇ "İÇERİ ÇEKİL · güven −1", ilk basış onay ("Bu oda terk edilir: güven −1, izler silinir." + yerde
  yatanın ölümü), hedef odasında ve son turda yok; son tur onayı "SON TUR · SAVUNMA BİTER" (`TimeoutEnd` savunmayı dışlar); geri çekilme
  onayı; sonuç kartı "KANAT TUTULDU" / "RÖLE KIRILDI" / "KANAT DÜŞTÜ" (`RaidDataSource.DefenseCard`); kurallar kartının savunma bölümü;
  kapı hover'ı ve fişi "kapı sığınak değil"; röle ve batı geçidi hover'ı; saha rölesi parçası `Desk/Prefabs/Raid/Gear/Relay.prefab`
  (kurgu, yol E; `RaidPieceLibrary.relay`, yedek `relayFallbackScale`). Rölenin adı Core'un nesne adlarında (`Baskin.ObjNames["role"]`;
  kapanış).
- **Bot (`CampaignBot.Savun`, `RaidBot.Retreat`):** §11.6 "Aşama 11 ölçümü"; `Tools/DengeBotu/README.md` "Kuşatma ve savunma, kalıcı tahta".
- **Doğrulama:** Core testleri (`FacilityMarksTests`, `FacilityDefenseTests`, `FacilitySiegeTests`, `FacilityRewardTests` ekleri,
  `FacilityBotTests` ekleri), sunum testleri (`FacilityDefenseTextTests`, `FacilityPlanSheetTests`, `FacilityDefensePresentationTests`,
  `FacilityDefenseSceneTests`), PlayMode bitti ölçütü `FacilityDefensePlayTests.Defense_PlayedFromSlipToReport` (kuşatma kapanışta kurulur,
  beş masa yüzeyinde görünür, SAVUN gerçek Enter'la, 10 hücreli şerit, barikat turu, gerçek EMRİ İLET'le gelen dalga ve bir tur gerçek dalga
  çatışması, onaylı İÇERİ ÇEKİL, "KANAT TUTULDU", rapor ve teleks, terk edilen odanın izi silinir), `Defense_Lost_RelayBroken`,
  `FacilitySiegePlayTests` (BIRAK ve kendiliğinden bırakma). Ölçüm §11.6 "Aşama 11 ölçümü"; kararlar §14 on dördüncü tur.

## 10. Yeni tesis şartları: Katar ve Yalpa

İki şart, iki YZ'ye bağlıdır ve o iki paketin bugünkü olayının yerine geçer. Katar WAYPOINT'in tesisinin şartıdır
(`na` paketi, orta aşama: Dağıtım merkezi yerine "Sevkiyat katarı", yük asansörü olayı yerine). Yalpa HALCYON'un
tesisinin şartıdır (`oc` paketi, geç aşama: Maden limanı yerine "Yüzer maden platformu", yağ sızıntısı olayı yerine).
Olay kimlikleri `katar` ve `yalpa`'dır; `train` kodda eğitim demektir, kullanılmaz.

### 10.1 Katar

Tesis bir trendir; her kanat üç vagondur ve her vagon bir odadır. Geçit, vagonlar arasındaki bağlantıdır.

| Kural | Açıklama (b) |
|---|---|
| Açık kenar | Vagonun kuzey ve güney kenarı açıktır. Kenardan dışarı itilen ya da çekilen her birim düşer: makine yok olur; ekip üyesi vagondan düşer, baskından çıkar ve ağır yaralı döner (`facilityFallInjuryTurns`, 5 vardiya); ölmez |
| Kenar ve yük | Kenardan düşen ekip üyesi ayakta sayılmaz. Taşıyıcı düşerse yük, düşmeden önceki son karesinde kalır; yerdeki yük kenardan düşmez. Yerde yatan kişi kenardan itilemez ve çekilemez, kenar karesinde kalır. Ayaktaki son ekip üyesi de düşerse baskın "ekip düştü" ile biter: yerde yatanlar ölür, kenardan düşenler yine 5 vardiya yatar |
| Ayırma | Dönüşte GEÇ komutunun seçeneğidir: Teknisyen ayaktaysa oyuncu GEÇ yerine AYIR verebilir; Teknisyen'in o turki yeteneği harcanmış sayılır ve o geçişte takip olmaz. Ayrılan vagondaki makineler, yük ve taşınmayan düşen geride kalır |
| Takip | Dönüşte AYIR verilmeyen geçişte, terk edilen vagonda bağlantıya 2 kare ya da daha yakın duran makineler sonraki vagonun doğu geçidine en yakın boş karelere konur; o vagon temiz sayılmaz ve oynanır |

Ayrılan vagon o baskın için gider; kalıcı iz değildir (YZ katarı yeniden dizer). Takip kuralı yalnız Katar'dadır;
öbür tesislerde baskında makineler oda değiştirmez (savunma baskını ayrıdır: bölüm 9.2). Takip terk edilen vagonun
kuralıdır; giriş vagonuna da takipçi girebilir.
Açık kenar şartın parçasıdır ve giriş vagonunda yoktur (bölüm 2.4). Ayrılan vagonda kalan düşen ölür (bölüm 4.3).

Açık kenar itme araçlarını güçlendirir: kenar satırındaki makine can ve zırh fark etmeden düşer. Karşı ağırlığı aynı
kuralın ekibe de işlemesi, WAYPOINT'in imza makinesi Tampon (ekibi kenara iter) ve `itme` önlemidir (cıvatalı ayaklı
makine düşmez).

**Uygulanan hâlin tablodan iki farkı (aşama 12; onaya açık, §14 on beşinci tur 9 ve 19).** (1) Kenardan yalnız **itilen** düşer:
tek giriş `Baskin.Push`'tur (önce cıvata, sonra açık kenar); vinç ve öbür çekmeler birimi tahtadan çıkaramaz, tablodaki "ya da çekilen"
bugün boştur (aşama 13'ün Tamponu da iterek düşürür). (2) AYIR yalnız Teknisyen **kapıdayken ve o tur yeteneğini kullanmamışken**
verilir (`Baskin.DecoupleBlock` "gate"/"acted"): GEÇ odayı bitirdiği için "Teknisyen'in o turki yeteneği harcanmış sayılır"ın tek
okunabilir hâli ön koşuldur; tablodaki "Teknisyen ayaktaysa" bundan geniştir.

### 10.2 Yalpa

Tesis yüzen bir platformdur; zemin belli turlarda yatar.

| Kural | Açıklama (b) |
|---|---|
| Takvim | 2., 4. ve 6. tur sonunda zemin yatar: sırayla batıya, doğuya, batıya. Odanın son turunda ve bütçesini aşan turda yatma olmaz (6 turluk ara odada yalnız 2. ve 4. tur; 6. tur son tur). Yön bir tur önceden tahtada işaretlidir |
| Kayma | Sabitlenmemiş her şey yatış yönünde 1 kare kayar: makineler, ekip üyeleri, köpek, düşen kişi, yük, hurda, aygıtlar |
| Sabit olanlar | Duvar, bütün nesneler (dolaplar, soğutma, sivil sunucu, donanım, koku yayıcı, röle, sandık), örülmüş engel, çapa robotu ve cıvatalı ayaklı makine (önlem defteri) kaymaz |
| Çarpma | Kayarken engele çarpan 1 olay hasarı alır (evrede en çok 1 olay hasarı tavanı geçerlidir); kabloya kayan yanar |

Kayma itme kurallarını kullanır ama önlem defterinde `itme` sayılmaz.

**Uygulanan hâl (aşama 12, 2026-11-20).** Core ve sunumda; ana bayrak (`raidFacilityEnabled`) asset'te kapalı. İki alt bayrak (C#
varsayılanı `true`, ana bayrak açıkken okunur, asset'e yazılmaz): `facilityKatar` (na → `katar`) ve `facilityYalpa` (oc → `yalpa`).
Kapalıyken na ve oc tesiste aşama 11'deki asansör ve yağdır (oda üretimi dahil); ikisi kapalıyken oyun aşama 11 ile bayt aynıdır
(`t12K0` = `t11savun`, 24/24). Ana bayrak kapalıyken Camp8 iki harita kipinde `asama6/taban` ile bayt aynı. Kayıt sürümü 19, tesis izi
**f6** (`FacilityMaps.GeneratorVersion` 6; Digest `0xF47D2411`, alt bayraklar kapalıyken `0xBB1486CC`; §11.4 "Aşama 12'de kayıt"). Yeni
komut ve yeni `S.Rng` ya da `Rnd` çekimi yok. Aşama 12 uygulama tasarımının dört açık sorusu varsayılanlarıyla kararlaştırıldı, **onaya
açık** (§14 on beşinci tur "Açık sorular"): S1 adlar bayraktan bağımsız, S2 savunmada Katar ve Yalpa kapalı, S3 bedel gerekmedi (ölçüm
okuma ölçütünü geçmedi), S4 yeni kit ve kuplör yok. **Bitti ölçütü tuttu:** şart testleri geçer, iki tesis oynanır (PlayMode
`FacilityConditionPlayTests`).

- **Kimlik.** `RegionDef.FacilityEvent` ve tek okuyucu `FacilityMaps.EventFor(def, katar, yalpa)`: plan, baskın emri (`OrderRaid`) ve
  sunum (`Simulation.RegionEvent`) aynı şartı okur. `Baskin.RegionEvents` 12 + 2 kayıt. Şart parçası yok: kenar geometriden, yatış
  takvimden. Plan önbelleği (tohum, iki alt bayrak) anahtarlı.
- **Katar · plan.** G ve A odalarında ileri geçit yalnız doğu sütunundadır ve açık kenar satırlarına değmez (köşe çiftleri de dışarıda).
  200 tohumda tükenen yok; na G/A odalarının ortalama denemesi 12,8 (aşama 11: 6,5), en çok 137, yedek tür 5/1200.
- **Katar · kenar (§10.1).** A ve H vagonunda (dönüş ziyaretinde de) kuzey ve güney kenarı açıktır; giriş vagonunda (G) yoktur. Tek giriş
  `Push`: cıvatalı makine tutar (aşama 10), sonra açık kenar. Sabotaj, EMP ve koşu darbesi düşürür (koşu darbesinin birinci itmesi
  düşürürse ikincisi yok); çekmeler tahtadan çıkaramaz. Makine ve tutsak ortak ölüm yolundan, `fall` efektiyle, hurdasız yok olur; köpek
  düşer (baskından sonra dinlenir), aygıt hurdasız yok olur. Uzman ölmez: `Dead` + `Unit.Overboard` (gövde değil, ayakta değil,
  sürüklenmez, son nefes almaz), taşıdığı yük son karesinde kalır. Önizleme = uygulama, geri alma kendiliğinden.
- **Katar · rapor ve dönüş.** Oda raporunda kişi durumu `"k"` (sonraki odaya konmaz, rapor taşır); `RaidResult.Overboard`.
  `ReturnPlanningSquad`: kenardan düşen yaşar, `facilityFallInjuryTurns` (5, en az 1) vardiya düz yatar (`Job` "rest"); beceri indirimi,
  Revir ve deneyim yok; eşya sahibinde kalır; satır "{0} vagondan düştü; {1} vardiya yatış gerekiyor."; sunum alanı
  `SquadOutcome.Overboard`. "Ekip düştü" baskınında kenardan düşen yaşar, yerde yatan ölür.
- **Katar · takip.** Dönüşte (kaçış dahil) Katar odasından GEÇ'te, batı geçidine yürüme uzaklığı ≤ `facilityFollowRange` (2) olan ayaktaki
  makineler (`Baskin.FollowerIds`) raporun `Follow`'una yazılır; sonraki vagon temiz olsa da oynanır (`RaidChain.Next`), takipçiler
  kadrodan sonra doğu geçidine en yakın boş zemine konur (ortak yerleştirici `EntryTile`, aşama 11 savunmasıyla aynı; `MaxEnemies` dolarsa
  kalan gelmez). Kendiliğinden geçişte de işler; gidişte ve savunmada yok. Gelen takipçi `Unit.Follower` taşır (okuyucu
  `Baskin.FollowedInIds`; kapanış).
- **Katar · AYIR.** GEÇ'in seçeneği (`CanDecouple`/`Decouple`, neden `DecoupleBlock`: `rule`, `tek`, `gate`, `acted`): dönüşte, Katar'da,
  Teknisyen kapıda ve bu tur yeteneğini kullanmamışken. Takibi keser, Teknisyen'in o turki yeteneği harcanır, geri alınmaz, rapora
  `Decouple` yazılır (`BState.Decoupled`, imza eki `|A`); bedeli yoktur (ölçüm §11.6 I6).
- **Rapor denetimi** (`Simulation.KatarReportCheck`, `AdvanceRaidRoomCheck`'in sonunda): takip dizinleri aralıkta ve tekil; takip ya da
  AYIR yalnız geçişle biten Katar kaçışı/dönüşünde ve ikisi birden değil; `"k"` yalnız Katar odasında düşülmüşse (kişinin düştüğü oda);
  savunmada hiçbiri.
- **Yalpa (§10.2).** 2., 4. ve 6. tur sonunda (bütçe içinde, son turda değil) batı/doğu/batı yatış (`EndTilt`, olay adımı; takvim
  `Baskin.TiltAt/TiltDir/TiltSchedule`, dönüş odası 1'den). Birimler yokuş aşağıdan `Push`'un kayma kipiyle kayar (cıvatalı sessiz tutar;
  çarpma ve kablo itme kuralıyla, olay hasarı tavanı adım başına 1); sonra hurda (boş zemin ve bağ kuralı; "bütün nesneler sabit"in tek
  istisnası), sonra gövde ve yer yükü (kapı ve menfeze girmez, hasarsız). Kayma itme sayılmaz. Fx `ev`/`yalpa`, `slide` (yalnız kayan
  nesnede; çarpan birimde N 0). `Hazards`: şeritte `←`/`→`, yatış turunda `slide` hatları ve `tilt` kenarı ("YALPA 1"), önceki turda
  `tilt_next` ("YALPA 2"; etiket yokuş aşağı kenarın orta karesindeki işarette). Makine aklı yatış turunda yokuş aşağısı kapalı adaya −0,5.
- **Savunma:** Katar ve Yalpa savunma tahtasında kapalı (S2; savunma olay durumu kurmaz; `FollowerIds` savunmaya da bakar).
- **Adlar (S1):** na "Sevkiyat katarı" (Yük vagonları · Ayıklama vagonu · Sunucu vagonu), oc "Yüzer maden platformu" (Kumanya güvertesi ·
  Bant galerisi · Makine dairesi), bayraktan bağımsız; statik notlar iki hâle uyar (istasyonun yük asansörü, demirli platformun hortumu),
  şartın notu `RegionDef.FacilityNote` (okuyucu `Simulation.RegionNote`: oynanan şart bugünkü olaydan farklıysa).
- **Okuyucular (kapanış).** `Simulation.FacilityRoomTurns(room)` (emrin o odada açacağı ilk ziyaret tahtasının `MaxTurns`'ü:
  `RaidChain.Setup` ve `Baskin.FirstVisitTurns`; `RoomBoard` aynı kuralı okur) ve `Baskin.FollowedInIds(B)`; sunum tur bütçesini ve
  takipçiyi kendisi hesaplamaz.
- **Sunum: masa (P4a; metinler `Desk/Scripts/Bridge/FacilityConditionText.cs`, saf).** Görev fişinin OLAY satırı ve kimliği
  `Simulation.RegionEvent`'i, bina hover'ı `Simulation.RegionNote`'u okur (kuşatma fişi ve savunma anteti `BuildingNote`'ta kalır). Katar
  paftasının (A ve H) kuzey ve güney dış çerçevesi çizilmez; yerine şart mürekkebiyle kesik çizgi ve kenara bakan tırnaklar
  (`FacilityPlanSheetData.OpenEdges` ← `Baskin.OpenEdgeRows`, `FacilityPlanSketch.DrawOpenEdges`; duvar karesinde kesik yok); hover
  "Açık kenar (kuzey, güney): … ekip üyesi 5 vardiya yatar." Yalpa paftasının hover'ı odanın tur bütçesine göre takvim verir (ara oda 2
  batı, 4 doğu; hedef ayrıca 6 batı; bütçe `Simulation.FacilityRoomTurns`'ten). Operasyon raporunda kenardan düşen "vagondan düştü · 5
  vardiya yatış" (Alert; yatış Core'un kuralının alanı `FacilityConditionText.FallShifts`, sonuçta yazılan değerle aynı; teleks değişmedi).
  Fiş sığması (1080): EN beş satırlı HALCYON fişi BASKIN EMRİ VER'i 18,8 px taşırır (aşama 10 tabanı ~19 px; kötüleşme yok, EN bina adı
  kısaltılmadı).
- **Sunum: baskın sahnesi (P4b).** Katar: açık kenarın kesik uyarı çizgisi ve dışa bakan tırnak (`brink`, `RaidPieceLibrary.hazardBrink`
  #A9520A), kenar hover'ı; önizlemede düşme satırları (makine iyi, ekip kötü), kenarın dışında yarım karede hayalet ve "DÜŞER"; `fall`
  canlandırması (parça kenara kayar, düşerek eğilir, söner); kenardan düşen tahtada yatmaz, Z geri getirir; kart "VAGONDAN DÜŞTÜ"
  (sonraki odada "· PAFTA n"), telsiz fişi, kapanış satırı, sonuç kartı eki "· n vagondan düştü". AYIR: Q (seçili birim yokken),
  Teknisyen'in kartı ("AYIR · Q", `RaidTeamModel.Detach`) ya da rakamı çevirir, yalnız `CanPass && CanDecouple`; Enter `Decouple` ya da
  `Pass`, onaysız; EMRİ İLET "AYIR · takip yok" / "GEÇ · n makine takip eder"; durum satırı ve "AYIR yok: Teknisyen bu tur yeteneğini
  kullandı". Takip: "TAKİP EDER" etiketi (GEÇ verilebilirken, AYIR kapalıyken), hover (her zaman), kapı ve geçiş fişleri (`EvArmClank`),
  gözcü "takip: n makine doğu geçidinde" (`FollowedInIds`), kapanış "Vagon ayrıldı: PAFTA n" / "Takip: n makine · PAFTA n". Yalpa: şerit
  okları ("2←", "4→"; glif CP-Bold SDF'de var); önceki turda soluk kesik oklar ve "YALPA 2", yatış turunda koyu oklar, "YALPA 1"
  (kırmızı) ve kayma hatları (çarpanın ucu kırmızı; `hazardTilt` #2C6E73); hover (kayar, çarpar −1, cıvatalı, sabit); canlandırma
  (tahta kökleri ~2,5° eğilir, kaymalar Fx'in varış karesiyle birlikte ≤ 0,6 s, popup "YALPA", `EvShelfRoll`; itme kısayolu yok);
  karesiz telsiz fişi "Yalpa · batıya", gözcü takvimi. Antetin notu `RegionNote`. Sahne ve prefab değişmedi; tek asset değişikliği
  `Settings/RaidPieceLibrary.asset`'in iki renk satırı; yeni ses ve model yok.
- **Bot (`RaidBot.Detach`, `RaidBot.Edge`):** Katar'ın dönüş geçidinde AYIR politikası; açık kenardan düşen uzman ayrı bedelle puanlanır;
  kenardan düşen makine düşürülmüş makine terimini alır; Yalpa'ya politika yok (kopyadaki gerçek evre kaymayı görür). Ölçüm §11.6
  "Aşama 12 ölçümü"; README "Katar ve Yalpa (aşama 12)".
- **Doğrulama:** Core testleri (`FacilityTrainTests` 16, `FacilityTiltTests` 14, `FacilityConditionCampaignTests` 6,
  `FacilityConditionBotTests` 26, `RaidEventTests`/`FacilityMapsTests`/`FacilityRecordTests`/`FacilityJsonTests` ekleri), sunum testleri
  (`FacilityConditionTextTests` 4, `FacilityConditionDeskTests` 4, `FacilityConditionPresentationTests` 4, `FacilityConditionSceneTests` 3,
  `FacilityPlanSheetTests` +1), PlayMode bitti ölçütü `FacilityConditionPlayTests` (A: Katar kanadı fişten rapora: gerçek Q + tıkla
  makine ve Analist kenardan düşer, Z, gerçek Q ile AYIR, AYIR'sız takipçi giriş vagonuna gelir, rapor satırı ve kayıttan devam; B:
  Yalpa kanadı: önceki tur ve yatış turu işaretleri, gerçek Enter'la yatış, kayan makine kayma karesinden yürür). Ölçüm §11.6 "Aşama 12
  ölçümü"; kararlar §14 on beşinci tur.

## 11. Core tasarımı, sunum, testler

### 11.1 Yeni `Balance` alanları

| Alan | Varsayılan (b) | Anlam |
|---|---|---|
| ~~`raidFacilityEnabled`~~ | — (aşama 18'de silindi) | Tesis kipi tek baskın kipidir; ana bayrak ve eski tek tahta yolu yok (kayıt 21) |
| `facilityEntryTurns`, `facilityMiddleTurns`, `facilityTargetTurns` | 6, 6, 10 (sürüm 10 telafisi 2026-11-24; önce 5, 5, 9; ondan önce 5, 5, 8) | Oda tur bütçeleri |
| `facilityReturnTurns` | 6 (sürüm 10 telafisi 2026-11-24; aşama 4 kararıyla 5, önce 4) | Dönüşte temiz olmayan odanın süresi |
| `facilityEntryMachines`, `facilityMiddleMachines`, `facilityTargetMachines` | 1, 1, 3 (aşama 4 kararı; önce 2, 3, 3) | Odanın başlangıç makineleri (ilki bakım robotu, kalanı dron); hedef odada kademe ekleri ve imza makinesi bunun üstüne gelir, en çok 7 |
| `facilityPanelHp`, `facilityAntennaHp`, `facilityValveHp` | 2, 2, 1 | Ara odadaki donanımın canı (sigorta panosu, çağrı anteni, soğutma vanası) |
| `facilityPanelQuietTurns` | 2 | Sigorta panosu kırıksa hedef odada uyanmasız ilk turlar (aşama 2) |
| `facilityAntennaMachines` | 1 | Çağrı anteni kırıksa hedef odaya eksik gelen makine: önce kademe ekinden, kademe eki yoksa başlangıç dronundan; bakım robotu kalır (aşama 2) |
| `facilityValveTargetHp` | 2 | Soğutma vanası kapalıysa sunucu hedef dolabının can eksiği; en az 1 can kalır (aşama 2) |
| `facilityValveStillTurns` | 1 | Soğutma vanası kapalıysa ikmal hedef odasında makinelerin yürüyemediği ilk turlar (aşama 2) |
| `facilityFallInjuryTurns` | 5 | Katar'da açık kenardan düşen ekip üyesinin yatışı |
| `facilityAmbushRange`, `facilityRushRun`, `facilityRushPush`, `facilityThrowRange` | 4, 3, 2, 3 | Pusu hattı, koşu uzunluğu, itme, fırlatma (aşama 6; `MoveRules.From` → `RoomSetup.Moves` → `BState.Moves`) |
| `facilityDragMoveCost` | 1 | Aşama 6: sürükleyenin yürüyüş kaybı (o tur yeteneği de yoktur); §13 "Ölüm" karşı ağırlığı bununla ölçülür. D22 uygulandı: 0 da ölü hedefini tutturmadı, 1 kaldı (§11.6 "Aşama 6 ölçümü") |
| `facilityLastBreathDies` | `false` | Aşama 6 kapanışı: true "son nefes" kuralını kapatır (§4.3); yalnız ölçüm anahtarı. Olumsuz ad: asset'te ve kayıtta alan yokken kural açık kalsın |
| `facilityBigCrateMetal`, `facilityBigCrateCircuits` | 2, 1 | Büyük sandığın ek ödülü |
| `facilityWireCharges`, `facilityChargeCharges` | 2, 2 | Aşama 8: örme seti ve lokumun baskın hakkı |
| `facilityWireHp`, `facilityPlankHp`, `facilityPlankCost`, `facilityScrapHp` | 3, 3, 2, 1 | Aşama 8: tel ve kalas canı, kalasın hak bedeli, hurda canı |
| `facilityDeviceTurns`, `facilityDeviceHp` | 3, 2 | Aşama 8: aygıt süresi ve canı |
| `facilityTurretRange`, `facilityWinchRange`, `facilityWinchPull`, `facilityDecoyStep` | 4, 4, 1, 2 | Aşama 8: taret ve vinç hattı, vinç çekişi, yem adımı |
| `facilityClampTurns` | 3 | Aşama 8: tutsağın oynadığı oyuncu turu |
| `facilityDogHp`, `facilityDogMove`, `facilityDogRestTurns`, `facilityBarkRange` | 2, 5, 3, 3 | Aşama 8: köpek |
| `facility{Wire,Charge,Turret,Decoy,Winch,Clamp,Dog}…` | §5.6 tablosu | Aşama 8: tarif girdileri ve vardiyası; `facilityDogSupplies` 4 (double) |
| `facilityEarIntel`, `facilitySpareCharges`, `facilityOpenLineTurns`, `facilityThresholdTurns` | 1, 1, 1, 1 | Aşama 8: Kulak, Yedek çanta, Açık hat, Eşik |
| `facilityInfirmaryShifts`, `facilityWorkbenchShifts`, `facilitySightRange`, `facilityFurHp`, `facilityPulleyPull` | 1, 1, 1, 1, 2 | Aşama 8: Revir, Tezgâh, Nişan, Kürk, Makara (mutlak) |
| `facilityStretcherAlways` | `false` | Aşama 8: yalnız ölçüm; Sedye bütün harekât açık (K24) |
| `facilityIntelCost`, `facilityWatchIntel` | 1 (2026-11-24; önce 3), 1 | Dinleme nöbeti |
| `facilityTallyCap`, `facilityCounterThreshold`, `facilityCountermeasures` | 3, 6, `true` | Önlem defteri |
| `facilityMaxTitles` | 2 | Unvan sınırı |
| `facilityRepairPerShift` | 1 | YZ onarımı |
| `facilityDefenseEnabled`, `facilityDefenseTurns`, `facilityDefenseWaveTurns`, `facilityRelayHp`, `facilityDefenseRoomTrust` | `true`, 10, 3, 4, 1 | Savunma baskını: toplam süre, dalga aralığı, röle canı, terk edilen oda başına güven kaybı. Uygulanan adlar ve ek alanlar aşağıda "Aşama 11'de eklenen" (röle canı `facilityDefenseRelayHp`) |
| `infiltratorFieldEnabled` | `true` (aşama 18; önce `false`) | Sahadaki sızmacı (alt bayrak) |
| `infiltratorTellMaxDelay`, `infiltratorTurn` | 4, 2 | Belirti veren en uzun iz gecikmesi ve hedef odasındaki dönüş turu |

`facilityDefenseEnabled`, `facilityCountermeasures`, `facilityMarks`, `facilityKatar`, `facilityYalpa`, `facilitySignatureMachines`,
`facilityTitlesEnabled` ve `infiltratorFieldEnabled` alt bayraktır; hepsinin C# varsayılanı `true`. Aşama 18'den (kayıt 21) beri
ana bayrak yoktur: alt bayraklar her oyunda okunur. `facilityDefenseEnabled = false` iken alarm eşiği anlık karşı saldırıyı verir
(`Counterattack`), `facilityCountermeasures = false` iken önlem defteri işlemez. Aşama 18'den beri `Settings/BalanceConfig.asset`
bütün `facility*` alanlarını ve `infiltratorFieldEnabled: 1`'i açıkça yazar; aşağıdaki "asset'e yazılmaz" notları o güne dek
geçerliydi.

Tarif girdileri, unvan eşikleri ve kazanım sayıları da düz alandır; adları uygulama aşamasında bu tabloya eklenir.

**Aşama 8'de eklenen** (`Core/Balance.cs`, 49 alan; asset'e yazılmaz, C# varsayılanıyla çalışır): yukarıdaki "Aşama 8" satırları.
`MoveRules` aşama 8 alanları: `WireHp, PlankHp, PlankCost, ScrapHp, DeviceTurns, DeviceHp, TurretRange, WinchRange, WinchPull,
DecoyStep, ClampTurns, DogHp, DogMove, BarkRange, Stretcher`; `MoveRules.From(b, p)` Nişan, Makara, Kürk ve Sedye'yi uygular; Eşik
ve Açık hat `RaidChain.Setup`'ta süreye eklenir.

**Aşama 10'da eklenen** (`Core/Balance.cs`, 17 alan; asset'e yazılmaz, C# varsayılanıyla çalışır; tasarımın "19" yazısı sayım
hatasıydı):

| Alan | Değer | Anlam |
|---|---|---|
| `facilityTallyCap`, `facilityCounterThreshold` | 3, 6 | Baskın başına sınıf tavanı; öğrenme eşiği (en az 1 okunur) |
| `facilityCountermeasures` | `true` | Alt bayrak (ana bayrak açıkken okunur); `false` = aşama 9 ile bayt aynı (ölçüm kilidi) |
| `facilityTallyAmbush` | `false` | Ölçüm anahtarı: `true` pusu isabetini `aygit`'e sayar (§6.3 tablosunun lafzı; S1) |
| `facilityDoubleWalls`, `facilityDoubleWallCharges`, `facilityPiercerCharges` | 2, 2, 1 | Çift cidar sayısı, lokum bedeli, Delici şarjın bedeli |
| `facilityCutterDamage` | 1 | Kesici kolun ek hasarı |
| `facilityShortClampTurns` | 1 | Kısa bağ |
| `facilityEmitterHp`, `facilityEmitterRange` | 1, 3 | Koku yayıcının canı ve erimi (sayısı = kademe) |
| `facilityJamTurns` | 1 | Parazit |
| `facilityCounterLinePenalty` | 6.0 (double) | Makine aklının hat cezası |
| `facilityAnswerCircuits` | 1 | Cevap sayfasının ek devresi |
| `facilitySteelWireHp`, `facilityRelayTurns`, `facilitySpringTurns` | 1, 1, 1 | Çelik tel, Yedek röle, Çift zemberek |

`MoveRules` aşama 10 alanları: `Tally`, `TallyAmbush`; kademeler `Bolt, DoubleWall, Cutter, ShortClamp, Emitter, Jam` (0..2,
`Level(i)`, `Level(cls)`); sayılar `DoubleWalls, DoubleWallCharges, CutterDamage, ShortClampTurns, EmitterHp, EmitterRange, JamTurns,
LinePenalty`; cevaplar `Unbolt, Piercer` (bool), `SteelWireHp, RelayTurns, SpringTurns, PiercerCharges`. `MoveRules.From(b, p)`
kademeyi ve cevapları emirden okur; `From(null)` ve `FacilityMaps.Setup` hepsini 0 verir.

**Aşama 11'de eklenen** (`Core/Balance.cs`, 11 alan; asset'e yazılmaz, C# varsayılanıyla çalışır; aşama 11 ölçümünden sonra hiçbir
varsayılan değişmedi: §11.6 "Aşama 11 ölçümü" okuma 1):

| Alan | Değer | Anlam |
|---|---|---|
| `facilityMarks` | `true` | Alt bayrak (ana bayrak açıkken okunur): izler, onarım, planda ve emirde iz; `false` = aşama 10 tahtaları (ölçüm kilidi) |
| `facilityRepairPerShift` | 1 | İşgaldeki her kanatta vardiya kapanışında onarılan iz (en eski önce) |
| `facilityDefenseEnabled` | `true` | Alt bayrak: kuşatma, `siege_defend`/`siege_yield`, üç odalı savunma, §9.3; `false` = anlık karşı saldırı ve yeniden verilen bölge ödülü (aşama 10) |
| `facilityDefenseTurns` | 10 | Savunmanın toplam süresi (tek sayaç; en az 1 okunur, `RaidChain.DefenseTurns`) |
| `facilityDefenseFirstWave`, `facilityDefenseWaveTurns`, `facilityDefenseWaves` | 2, 3, 3 | İlk dalganın çıkış turu, aralık, dalga sayısı (2, 5, 8) |
| `facilityDefenseWaveBase` | 1 | Dalga büyüklüğü = taban + min(kademe, 3): 2/3/4 |
| `facilityDefenseRelayHp` | 4 | Saha rölesinin canı. Yukarıdaki tabloda ve bölüm 9.2'de `facilityRelayHp`; Yedek rölenin `facilityRelayTurns`'üyle karışmasın diye bu ad (§14 on dördüncü tur 34) |
| `facilityDefenseRoomTrust` | 1 | Terk edilen oda başına güven |
| `facilityCoverRelayHp` | 1 | Siper: savunmada röle eki |

Hepsi ölçümsüz (b). Aşama 11 taraması: dalga tabanı 0–6, dalga aralığı 1–4, dalga sayısı 3–8, süre 8–12 ve birleşimleri tutma
politikasında savunma başarısını değiştirmedi (%100); yalnız röle canı hep çekilmede etkili (röle 1 → %93). Okuyucular
(`Simulation.DefenseTurns`, `DefenseWaveCount`, `DefenseSchedulePreview`, `DefenseWaveSize`, `DefenseRelayHpFor`, `DefenseRoomTrust`)
sıkıştırmayı tek yerde yapar; sunum alanları doğrudan okumaz (kapanış, §14 on dördüncü tur 115).

**Aşama 12'de eklenen** (`Core/Balance.cs`, 4 alan; asset'e yazılmaz, C# varsayılanıyla çalışır; aşama 12 ölçümünden sonra hiçbir
varsayılan değişmedi: botla ölçüldü, hedef satırı yok, okuma ölçütleri bedel gerektirmedi, §11.6 "Aşama 12 ölçümü"):

| Alan | Değer | Anlam |
|---|---|---|
| `facilityKatar` | `true` | Alt bayrak (ana bayrak açıkken okunur): na tesisinde Katar (kapalıyken yük asansörü, aşama 11 oda üretimi) |
| `facilityYalpa` | `true` | Alt bayrak: oc tesisinde Yalpa (kapalıyken hidrolik yağ); ikisi kapalı = aşama 11 ile bayt aynı (`t12K0` = `t11savun`) |
| `facilityFallInjuryTurns` | 5 | Katar'da açık kenardan düşen ekip üyesinin düz yatışı (en az 1 okunur; `FacilityConditionText.FallShifts` aynı alan) |
| `facilityFollowRange` | 2 | Takibin ölçütü: batı geçidine yürüme uzaklığı (`MoveRules.FollowRange`) |

**Aşama 13.** `facilitySignatureMachines = true` tesis ana bayrağıyla birlikte çalışır. Düz alan varsayılanları menzil ve etkide
`facilityFlameRange = 2`, `facilityFlameDamage = 1`, `facilityRamRange = 3`, `facilityRamPush = 2`, `facilityAnchorRange = 4`,
`facilityAnchorPull = 2`, `facilityFrostRange = 3`, `facilityFrostTurns = 2`, `facilitySealHp = 3` ve `facilityBuildDamage = 1`.
Makine hurdası mevcut `facilityScrapHp` alanını (1) kullanır. `BalanceConfig.asset` mevcut ayarları taşır.

**Aşama 14.** `facilityTitlesEnabled = true`, `facilityMaxTitles = 2`. Eşikler `facilityMarksmanAt = 3`, `facilityCarrierAt = 2`,
`facilityRamTitleAt = 3`, `facilityHandlerAt = 3`, `facilityBuilderAt = 3`, `facilityThrowerAt = 2`. Etkiler sırasıyla
`facilityTitleAmbushBonus = 2`, `facilityTitleRushBonus = 1`, `facilityTitleDogCommands = 2`, `facilityTitleDeviceBonus = 1`,
`facilityTitleThrowBonus = 2`. Sırtlayan sürükleme maliyetini sıfırlar. Alanlar C# varsayılanlarından okunur.

**Tek tahtanın ayarları (aşama 16; aşama 18'de silindi).** `liberationLootExtraTurns` ve `liberationServerExtraTurns` (ikisi de 2)
yalnız eski tek tahtanın tur formülüne giriyordu (`Baskin.Create`, `TutorialRaid`); tesis süresini `RoomSetup.Turns`/`FirstVisitTurns`
ve savunmanın toplam sayacı verir. Aşama 18'de iki alan `raidFacilityEnabled` ile birlikte silindi; `Camp8.ApplyBal` onlara "emekli
alan" der. Silinen belge notlarındaki ölçümler tarihsel kayıt olarak burada:
- İkmal ek turu: kasa alındıktan sonra bütün ekip malzemeyle girişe döner. Harekât ölçümü (2026-10-28, bot, 10'ar harekât): ek tur 2,
  başarısızlık alarmı 1, bölge güveni 3 ile orta oyuncu 5/10, dikkatli 7/10 kazanır; eski değerlerle (1, 3, 2) 0/10 ve 2/10.
- Sunucu ek turu: bot ölçümünde (2026-11-01, 12 tohum) 1 ek turla sunucu başarısı kaçıştan önceki kadar (T+E+A %91, eskiden %90;
  zayıf ikililer ±3 puan), 2 ek turla 3–13 puan kolay; ek tursuz T+E+A %78. KOS aşama 8 harekât ölçümü (2026-11-14, 40 tohum): 1 → 2;
  sunucu başarısızlığı %11 → %6, harekât başına ölü −1,5, zafer uzunluğu −2,7 vardiya.

### 11.2 Durum

G5’te `MapRegion.SourceName` paket YZ’sinin adıdır. Eski kıyı/iç bölge ad seçimi için kullanılan bölgesel oran kayda girmez. G5 harita izi tohumdan aynı paket, hücre, ad ve bağlantı verileriyle yeniden kurulur.

| Yer | Alan | Erişim | Not |
|---|---|---|---|
| `LiberationCampaignState` | `Facility` (`FacilityState`) | public | Bilinen odalar, izler, önlem kademeleri, sayaçlar, açılan tarifler ve kazanımlar, istihbarat puanı |
| `FacilityState` | `Known` (oda kimliği listesi, öğrenme sırasıyla), `Intel` | public | Aşama 5: bina planı bilinen odalar ve harcanmamış dinleme puanı; öbür alanlar kendi aşamalarında eklenir |
| `FacilityState` | `Marks` (`FacilityMark` listesi: oda, tür, kare, can, gün), `Repairs` (onarılan izlerin kopyası, onarım günüyle) | public | Kalıcı tahta; sıra onarım sırasıdır. Aşama 11: bütün kanatların izleri tek listede (oda kimliğiyle) |
| `FacilityState` | `Tally`, `Counter` (sınıf başına) | public | Önlem defteri |
| `FacilityState` | `Recipes`, `Perks` (`List<string>`), `PendingReward` (`""` yok) | public | Sunucu ödülleri. Aşama 8: açılan tarifler, kazanımlar, ödülü bekleyen bölge; `LiberationRegionState.Rewarded` ödülü bir kez yapar |
| `FacilityState` | `Sieges` (`FacilitySiegeEntry` günlüğü: kanat, bölge, kuruluş günü, karar vardiyası `Due`, sonuç `Outcome`, kapanış günü, terk) | public | Aşama 11: bekleyen ya da süren kuşatma son girdidir (`Outcome` "" ya da `savun`). Tek `Siege` alanı yerine günlük: JsonUtility'de null olamaz; savunma tohumunun tuzu, "bırak" kilidi, sunum ve bot aynı günlüğü okur (§14 on dördüncü tur 4) |
| `RaidState` | `Reports` (`RoomReport` listesi) | public | Aşama 2: zincirin tek durumu. Süren baskının odası, yönü, taşınan ekip ve odalarda kalanlar raporlardan `RaidChain` ile türetilir (ilk tasarımdaki `Room`, `Outbound`, `Carry`, `Left` alanları yazılmadı) |
| `RaidParams` | `Mode` (`""` saldırı, `"defense"`), `Rooms`, `Marks` | public | Baskın türü, üç odanın kimliği ve emir anındaki kanadın izleri (aşama 11; saldırı `"raid"` değil boş dizgi) |
| `RaidParams` | `Turncoats`, `FieldTurn` | internal | Sızmacıların kadro bitleri ve emir anındaki dönüş turu |
| `ResidentState` | `Titles`, `Deeds` | public | Unvanlar ve iş sayaçları |
| `LiberationRegionState` | `Rewarded` | public | Bölge fonu, güveni, sunucu ödülü ve öğrenme bir kez verildi |
| `RegionDef` | `Ai` (YZ adı) | public | Bölgenin haritadaki adı; `Simulation.RegionName` bunu döndürür |
| `RaidParams` | `Tells` | internal | Sahada belirti verecek kişilerin kadro sırası (sızmacı ve iz bırakabilen insan; iz gecikmesi 5–6 olanlar hariç) |
| `Cell` | `Door` (0 yok, 1 batı girişi, 2 ileri geçit), `Vent` (menfez çifti kimliği), `Shutter` | public | Aşama 1; kare kopyasıyla taşınır |
| `BState` | `Room` (`FacilityRoom`; eski tahtada null) | public | Aşama 1; `CopyInto` taşır, `Signature`'a yalnız tesiste `\|R` + oda kimliği eklenir |
| `BState` | `QuietTurns`, `StillTurns` | public | Aşama 2: pano ve vana hazırlığı; `CopyInto` taşır, `Signature`'a yalnız sıfır değilse `\|p` eklenir |
| `Unit` | `AtGate`, `Slot` | public | Aşama 2: kapıda bekleyen uzman (tahta dışı, baskında; `OnBoard` dışlar) ve kadro sırası |
| `RaidLogEntry` | `Rooms` | public | Aşama 2: baskının oda raporları (`RoomReport.Encode()`, ziyaret sırasıyla; dönüş odaları da ayrı satırdır) |
| `GameRecord` | `facility` | public | Aşama 2: tesis parmak izi (bayrak açıkken `"f" + FacilityMaps.GeneratorVersion`, kapalıyken boş); `Replay.MapMatches` sınar. Aşama 6'da `GeneratorVersion` 2 (`"f2"`): aşama 2–5'in tesis kayıtları yüklenmez, bayrak kapalı kayıtlar etkilenmez. Aşama 8'de 3 (`"f3"`; §11.4 "Aşama 8'de kayıt"). Aşama 18 (kayıt 21): iz hiç boş değil (`f8`/`f9`), boş izli kayıt yüklenmez |
| `Unit` | düşen kişi (gövde): yeni alan yok | public | Aşama 6: kişi başına `Fallen` yerine bugünkü bayraklar: yerde `Dead && !Left && !AtGate && !Out`, kapıya sürüklendi `Dead && AtGate`, tesisten çıkarıldı `Dead && Out` (`Baskin.IsBody`, `LyingBody`). Gövde `OnBoard` değildir: kareyi kapatmaz, hedef alınmaz, itilmez; yalnız sürüklemeyle hareket eder |
| `Unit` | `AmbushDir` (−1), `AmbushX`, `AmbushY` | public | Aşama 6: pusu hattı (yön ve kurulduğu kare); `Units` klonuyla geri alınır, `Signature`'da birim ekinin sonunda `a{dir}{x}{y}` |
| `BState` | `Moves` (`MoveRules`, paylaşılan), `CargoX`/`CargoY` (yerdeki yük, −1), `HoldId`/`HoldX`/`HoldY` (iki kişilik işin tutuşu), `CrateOpen` | public | Aşama 6: `CopyInto` taşır (yansımalı `CopyInto_CoversEveryField` testi); `Signature`'a yalnız varsa `\|y`, `\|h`, `\|k` (kalkan kepenkler), `\|B` (`\|p`'den sonra, `\|R`'den önce) |
| `Unit` | `Breath` | public | Aşama 6 kapanışı: son nefes (§4.3); oda biterken yazılır, raporda Body'yi belirler; `Signature`'da birim ekinin sonunda `n` (yalnız varsa) |
| `Cell` | `Crate` | public | Aşama 6: büyük sandığın duvar nişi (plan bayrağı; `T = "wall"` kalır) |
| `RoomSetup` | `Moves` | public | Aşama 6: hamle değerleri (`FacilityMaps.Setup` dengeden doldurur) |
| `Unit` | `Class` (null uzman, `kopek`, `aygit`, `tutsak`) | public | Aşama 8: `Side` ikili kalır; sonuç ve kaçış yalnız uzmanı sayar (`Baskin.IsSpecialist`) |
| `Unit` | `Gear`, `GearLeft`, `Owner`, `Dir`, `Turns`, `Bark`, `Idle` | public | Aşama 8: uzmanın ekipmanı ve kalan hakkı; birimin sahibi, aygıt yönü ve kalan turu, havlama, niyetsiz plan; `Units` klonuyla geri alınır |
| `BState` | `Sniffed` | public | Aşama 8: odada koklandı mı; `CopyInto`'da, `Signature`'a yalnız varsa |
| `BoardEquipment`, `BoardRecipe` | `Rest`; `Supplies`, `Reward` | public | Aşama 8: köpeğin dinlenmesi; tarifin erzak bedeli ve ödül tarifi; `PlanningBoardData.EquipmentName`'e yedi ad |
| `SquadSlot`, `RaidParams` | `Gear`, `GearCharges`; `Perks` | public | Aşama 8: kadronun ekipmanı ve hakkı; emir anındaki kazanımların kopyası (K23) |
| `LiberationRaidReward`, `SquadOutcome` | `ServerReward`; `ClampSpent`, `DogDown` | public | Aşama 8: sunum; kayda girmez (`SquadOutcome` kişi satırında) |
| `RoomMember`, `RoomUnit`, `RoomReport`, `RoomSetup` | `Gear`; yeni `RoomUnit` (`Class:Type:Owner:State:Hp:Armor:X:Y:Dir:Turns`); `Units`, `Sniff`, `CargoDog`; `CarryUnits`, `CargoDog` | public | Aşama 8: oda zincirinde ekipman, köpek, tutsak ve aygıt |
| `Baskin` | `IsSpecialist/IsGadget/IsDog/IsCaptive/IsBarrier`, `GearActionKinds`, `UnitActionKinds`, `AllActionKinds`, `GearOf` | public | Aşama 8: bot ve sunum okur; `ActionKinds` altı kalır (K2) |

Durum kayda yazılmaz; hepsi komutlardan yeniden kurulur.

**Aşama 10'da eklenen** (hepsi public, kayda yazılmaz):

- `FacilityState.Tally`, `Counter` (`List<int>`, boş başlar; ilk yazışta altı, `FacilityCounters.Classes` sırası), `Learned`
  (`List<FacilityCounterEntry>`: `Day, Region, Class, Level`; aynı dosyada `[Serializable]` sınıf). Ayrı `CounterAt` yok (eşitlik
  `Learned`'den). `LiberationRegionState.Rewarded` artık öğrenmeyi de keser.
- `RaidParams.Counters` (`List<int>`, 6 ya da boş), `RaidParams.Answers` (`List<string>`); `LiberationRaidReward.Countermeasure`
  ("sınıf:kademe", sunum; kayda girmez).
- `BState.TallyPush, TallyBreach, TallyBuild, TallyCapture, TallyDog, TallyGadget` (+ `TallyArray()`), `Unit.Unbolted`,
  `Cell.Double`, `ObjNames["yayici"]`; `BoardRecipe.Answer`. `CopyInto` hepsini taşır; `Signature` ekleri yalnız varsa: `|T` + altı
  sayı, birimde `U` (sökük; `u` aşama 8'de kalan tur), `|D` + çift kareler (ikisi de `|R`'den önce).
- Okuma sözleşmesi (K29; bot ve sunum kopya kural taşımaz): `FacilityCounters.*` (`Classes, Index, ClassName, Name, Short, Desc,
  AnswerOf, ClassOfAnswer, SignatureMachine, RaidTally, Learn`), `FacilityGear.IsAnswer/AnswerBases/AnswerFor/AnswerDesc`,
  `Simulation.CountermeasuresOn/CounterLevel/CounterTally/Countermeasures/RaidCounters/RaidAnswers/FacilityPlanCells`,
  `Baskin.Bolted/IsDoubleWall/DoubleWallCost/EmitterBlocks/DeviceTurnsFor/ClampTurnsFor/DangerTiles/ApplyCounterCells` ve
  (kapanışta eklenen) `Baskin.WinchPulls` (vinç hattındaki ayakta birim çekilir mi; `FacilityGearView.WinchLine` okur).

**Aşama 12'de eklenen** (hepsi public, kayda yazılmaz; tahta durumu `Clone`/`CopyInto` ile taşınır):

- `RegionDef.FacilityEvent` (na `katar`, oc `yalpa`), `RegionDef.FacilityNote` (`Loc.T`'li; na ve oc dışında null).
- `Unit.Overboard` (kenardan düşen ekip üyesi; `Dead` ile birlikte; imza eki `K`), `Unit.Follower` (bu dönüş tahtasına takip ederek
  gelen makine; yalnız sunum okur, imzaya girmez; kapanış), `BState.Decoupled` (imza eki `|A`).
- `RoomReport.Follow` (`List<int>`, terk edilen vagonun takip eden makinelerinin `Machines` dizinleri), `RoomReport.Decouple` (bool); kişi
  durumu `"k"` (kenardan düştü). `RaidResult.Overboard` (kadro sıraları). `SquadOutcome.Overboard` (sunum; `Down` da true).
- `RoomSetup.Followers` dönüşte de dolar (aşama 11'de yalnız savunma); `FacilityPlan.Katar/Yalpa` (özellik; Digest'e girmez, serileşmez).
- Okuma sözleşmesi (bot ve sunum kopya kural taşımaz): `FacilityMaps.EventFor`, `Simulation.RegionEvent/RegionNote/FacilityRoomTurns`,
  `Baskin.IsOpenEdge/OpenEdgeRows/FollowerIds/FollowedInIds/CanDecouple/DecoupleBlock/TiltAt/TiltDir/TiltSchedule/Slides/FirstVisitTurns`;
  test kancaları `Baskin.TestEventStep`, `TestEventPos` (internal).

**Aşama 13.** `Cell.FrostUntil` oda tahtasında buzun son turunu tutar. `Unit.Assembled` ve `Obj.MachineBuilt` geri alma ve
öngörü kopyalarına katılır. `RoomReport` aynı iki alanı dönüş, takip ve ekiple geçit için taşır. `FacilityMarks` geçici makine
engellerini ayıklar.

**Aşama 14.** `ResidentState.Deeds` baskın sayılarıdır. `Titles` bit kümesi `SquadSlot` üzerinden uzman ve Köpekçi için köpeğin
`Unit` durumuna kopyalanır. `Unit.Work/Dragged` bu oda ziyaretindeki kanıt, `DogCommands` bu turki komut sayısıdır.
`RoomMember.Work/Dragged` raporun kişi sırasındadır. `SquadOutcome.NewTitles` kazanım anının rapor verisidir.

### 11.3 Komutlar

| Metot | Argümanlar | `Rec` adı | `Replay.Apply` karşılığı |
|---|---|---|---|
| `StartLiberationRaid(sectorId, pick)` | Bugünkü | `liberation_raid`, a, b | Bugünkü |
| `AdvanceRaidRoom(report)` | Biten odanın raporu (her geçişte ve baskının sonunda) | `raid_room`, a = `RoomReport.Encode()` | `sim.AdvanceRaidRoom(RoomReport.Decode(c.a))` |
| `ResolveRaid(result)` | Bugünkü; sona eklenen alanlarla | `raidres`, a | Bugünkü |
| `ChooseServerReward(regionId, choice)` | Bölge, `"tarif"` ya da `"kazanim"` | `server_reward`, a, b | `sim.ChooseServerReward(c.a, c.b)` |
| `DefendSector()` | Yok | `siege_defend` | `sim.DefendSector()` |
| `YieldSector()` | Yok | `siege_yield` | `sim.YieldSector()` |

- Aşama 15’te gözlenen taraf değişimi `raid_room` kadro durumunda `t` (tesiste kaldı) veya `x` (etkisizleştirildi) olarak taşınır. `raidres` sonunda yalnız dönüş varsa `Turned` ve `Subdued` kadro dizinleri yazılır. Sorgu ve söküm mevcut `event_decide` komutudur.
- Her komut bir `…Check` ve bir eylemdir. `Replay.Apply` bilinmeyen türü sessizce başarılı saydığı için her yeni tür
  için `case` eklenir ve test edilir.
- `RoomReport` konumsal bir dizgidir: oda, yön, tur, kadro canları, zırhlar, kalan haklar, düşenlerin kareleri, odada
  kalan makineler (tür, kare, can), yeni izler, araç sayaçları, kişi başına işler. Yeni alan yalnız sona eklenir.
- **Aşama 2'de yazılan biçim** (`Core/RoomReport.cs`):
  `Room|Return|Turns|End|Last|Success|Retreat|Cargo|Collateral|Civilians|Device|Team|Machines|Objects|Rubble`.
  Kişi satırı `State:Hp:Armor:Emp:Charges:Fell:X:Y`, makine `Type:X:Y:Hp:Armor`, nesne `K:X:Y:Hp:Max`. İzler, araç
  sayaçları ve kişi işleri kendi aşamalarında sona eklenir. Simülasyon raporun `Decode(Encode)` kopyasını saklar;
  bayrak kapalıyken ya da eski tahtalı baskında `AdvanceRaidRoom` reddedilir (aşama 18'den beri yalnız odasız emirde). Zincir kuralları (sonraki oda, temiz
  oda, taşınanlar, dönüş, sonuç) saf `Core/RaidChain.cs`'tedir; bot da sunum da onu kullanır.
- `ResolveRaid` tesis kipinde (aşama 18'den beri her baskında) son oda raporlanmadan reddeder; verilen sonucun yalnız `Reason`'ını alır, sonucu
  `RaidChain.Result` ile raporlardan toplar.
- Baskın hangi odada biterse bitsin (zafer, geri çekilme, süre, ekibin düşmesi, savunma) o odanın raporu `raidres`'ten
  hemen önce `raid_room` ile yazılır; `AdvanceRaidRoom` bu durumda yeni oda açmaz (rapordaki "son oda" alanı).
  `ResolveRaid` izleri, araç sayaçlarını ve kişi işlerini yalnız kayıtlı raporlardan toplar.
- **Aşama 6'da eklenen** (yalnız varsayılan dışındayken yazılır; eski ve gövdesiz raporların dizgisi değişmez): kişi
  satırının sonuna `:Body:Picked` (Body 0 yerde, 1 ekiple, 2 tesisten çıkarıldı; Picked 1 dönüşte atlanan temiz odadan
  toplandı), raporun sonuna `|CargoAt|Opened|Crate` (yerde kalan yükün karesi y*8+x ya da −1, bu ziyarette kalkan kepenk
  kareleri, büyük sandık açıldı mı; sondaki varsayılanlar kırpılır). `Fell` gövdenin yattığı odadır. Taşıyıcı düşünce yük
  yere düşer; yük yerde kalırken oda biterse `Cargo` −2. Son nefes (`Unit.Breath`) biçime alan eklemez; yalnız Body'yi
  belirler (geçişte 1, tesisin girişinde 2). `LiberationRaidReward.BigCrate` kayda girmez, raporlardan türer.
- **Aşama 8'de eklenen** (yalnız tesiste ve varsayılan dışındayken yazılır; eski, gövdesiz ve ekipmansız dizgiler değişmedi):
  - `server_reward` (a: ödülü bekleyen bölge, b: "tarif" | "kazanim"): `ServerRewardOptions`, `ChooseServerRewardCheck`,
    `ChooseServerReward` (`Core/Simulation.Facility.cs`); `Replay.Apply` karşılığı. Bekleyen ödülde `liberation_raid` reddi.
  - `raid_room` raporunun kuyruğu `|CargoAt|Opened|Crate|Units|Sniff|CargoDog` (sondaki varsayılanlar kırpılır), kişi satırı
    `…:Body:Picked:Gear` (son varsayılan dışı parçaya kadar). `Sniff` (1..3) `AdvanceRaidRoom`'da sıradaki odanın planını açar
    (aralık dışı değer yok sayılır).
  - **Denetim boşluğu (bağımsız inceleme):** `AdvanceRaidRoomCheck` raporun `Sniff` ve `Units` alanlarını yapısal denetimde
    doğrulamaz (§14 on birinci tur 62); elle kurulmuş bir rapor 1..3 arasındaki herhangi bir odayı öğretebilir. Raporlar yalnız
    `RaidChain.Report`'tan geldiği için bugün etkisizdir; simülasyon raporu doğrulamaz ilkesi (aşağıda) burada da geçerli.
  - `board_queue`/`board_reserve` açılan tariflerle (kilitli tarif reddedilir); yeni pano komutu yok.
- **Aşama 10'da eklenen** (yeni komut yok, `Replay.Apply` değişmedi; yalnız tesiste ve alt bayrak açıkken):
  - `raid_room` raporunun kuyruğu `…|CargoAt|Opened|Crate|Units|Sniff|CargoDog|Tally` (alan 21, "a:b:c:d:e:f", yalnız sıfır
    değilse; çözülünce hep 6 öğe, eski raporda altı sıfır); `RoomMachine` 6. parça `:1` yalnız sökükse.
  - `raidres` (`ResolveRaid`) sayımı toplar ve ilk tamamlamada öğrenir.
  - `server_reward` cevap tarifinde temellerinin başlamamış taslak iş emrini siler; `board_queue` cevap sayfasında aynı `Id`'yi
    kullanır (+1 devre).
- `RaidResult.Encode`'a sona eklenir: kişi başına "tesisten çıkarıldı" (aşama 6: `Extracted`, kadro sıraları; doluysa
  `Left` boş da olsa yer tutar, boşken dizgi eskisi gibi) ve "kenardan düştü" bayrakları (aşama 12) ve
  (aşama 15'te) bölüm 8'in "döndü" ve "düşürüldü" bayrakları. Tesiste düşen yalnız `Extracted`'taysa yaşar
  (`ReturnPlanningSquad`'ın `facility` dalı); tek tahtada bugünkü "biri ayakta döndüyse kurtarılır" kuralı. Aşama 18'de tek tahta
  kolu ve `facility` parametresi silindi: kural her baskında tesis kuralıdır.
- Simülasyon bugünkü gibi tahtayı yeniden oynatmaz ve raporu doğrulamaz; tutarlılık testleri bunu kapatır (11.6).
- Ekipman takma, üretim ve Gözetim ataması bugünkü pano komutlarıyla gider; yeni komut istemez.
- **Aşama 14.** `raid_room` kişi girdisine yalnız kanıt varsa `:Work:Dragged` eklenir. Önceki opsiyonel konumlar korunur.
  `AdvanceRaidRoomCheck` kadro bitleri ve iş türlerini denetler. `raidres` kazanan kişileri mevcut sonuçtan ve bu raporlardan türetir.
- Savunma baskını da `raidres` ile biter ve `S.RaidLog`'a bir girdi yazar; sunumun "n'inci `raidres` = n'inci kayıt"
  eşlemesi (`RaidPaperFacts.ResultIndex`) korunur. Aşama 11: savunmanın sonucu dizgiye eklenmedi (emirde `Mode`, sonuç `Success`,
  terk `RaidChain.Abandoned`'dan türer; girdi `RaidLogEntry.Mode = "defense"`; §14 on dördüncü tur 28). Komutlar `siege_defend`
  (`DefendSector`) ve `siege_yield` (`YieldSector`) argümansızdır; `Replay.Apply`'da `case`'leri var.
- **Aşama 12'de eklenen** (yeni komut yok, `Replay.Apply` değişmedi): `raid_room` raporunun kuyruğu `…|Tally|Follow|Decouple`
  (boşken kırpılır; eski dizgiler değişmez), kişi durumu `"k"`; AYIR ayrı komut değildir, `raid_room` raporunun `Decouple`'ıdır.
  `RaidResult.Encode` kuyruğu `…|Left|Extracted|Overboard`. `AdvanceRaidRoomCheck` sonunda `KatarReportCheck` (§10 "Uygulanan hâl
  (aşama 12)"; dört yeni ret metni); reddedilen rapor kayda girmez.

**Hurda (aşama 16).** `ScrapEquipmentCheck` panoya saf bir gerekçe verir. Yeni köpek hurda talebi envanteri, metali, RNG’yi ve komut geçmişini değiştirmeden reddedilir. Önceki Core ile geçerli bir `board_scrap` köpek için kaydedilebildiğinden `Replay.Apply` bu başarılı işlemi eski kuralıyla yürütür. Eksik veya bağlı eşya geçmişte de reddedilir. Kayıt sürümü 20, harita g5, tesis izleri f8/f9.

### 11.4 `Step` sırası ve RNG

- `Step` içine iki adım eklenir, ikisi de çekimsizdir: `AdvancePlanningBoard`'dan sonra `AdvanceFacilityIntel`
  (dinleme puanı) ve `AdvanceFacilityRepairs` (onarım).
- Tesis kipinde ve `facilityDefenseEnabled` açıkken `ChangeAlarm` eşikte bugünkü gibi alarmı sıfırlar ve
  `Counterattack` yerine kuşatma kurar (`BeginSiege`, `FacilityState.Sieges`'e girdi). Bu dört yoldan olur: vardiya kapanışı (`StepAlarm`),
  başarısız baskının `ResolveRaid`'i, konvoy kararı (`event_decide`) ve kartsız kapının cevabı (`door_answer`; ilk tasarımın listesinde
  yoktu). Girdi karar vardiyasını saklar (`Due`); "bırak" karar vardiyasının kapanışında, `StepAlarm`'dan önce uygulanır
  (`AdvanceFacilitySiege` → `AdvanceFacilityRepairs` → `StepAlarm`; aşama 11'de `Step` gövdesi `StepCore`'a taşındı, `stepping` bayrağı
  `try/finally` ile). Geri alınacak kanat yoksa ya da bekleyen/süren kuşatma varken kuşatma kurulmaz (güven −1, sayaç artar).
- Baskın emrindeki tek `S.Rng` çekimi (tohum) aynen kalır. Oda tohumu ondan türetilir:
  `Rng.Mix(seed, oda × 2 + yön)`. Savunma baskınının tohumu `S.Rng`'den çekilmez (tabloya bakın).
- Yeni komutlar, okuyucular, hamleler, aygıtlar, şartlar ve önlem defteri RNG çekmez.

| Nokta | Akış | Çekim |
|---|---|---|
| Oda üretimi (harekât başı) | `Rng.Mix(S.Seed, 0x54455349)` | Ayrı akış; `S.Rng` tüketmez |
| Baskın emri | `S.Rng` | 1 (bugünkü) |
| Oda kurulumu (makine yeri, türü) | Oda tohumu | Yeni sıra: başlangıç makineleri (yer), sonra bugünkü kademe ekleri (yer, tür); serpiştirme çekimleri çıkar |
| Makine planı, uyanma | Oda tohumu | Bugünkü |
| Savunma emri (`siege_defend`) | `Rng.Mix(S.Seed, 0x53494547 + kuşatma günlüğündeki girdinin dizini)` (aşama 11; ilk tasarımdaki `S.Counterattacks` tuzu, sayaç yalnız kanat düşünce arttığı için art arda tutulan iki savunmaya aynı tohumu verirdi: §14 on dördüncü tur 12) | Ayrı akış; `S.Rng` tüketmez |
| Savunma dalgalarının türü | Oda tohumu değil: `Rng.Mix(p.Seed, 0x57415645 + dalga)` (aşama 11) | Ayrı akış; tahtanın `Rnd(B)` sırası değişmez |

**Sürüm.** `GameRecord.CurrentVersion` 18'den 19'a çıkar (aşama 2'de, `raid_room` komutuyla
birlikte; aşama 1 bayrak arkasında kalır ve kayıt biçimini değiştirmez). Göç yoktur; eski kayıt arşivlenir.
Kayda tesis parmak izi eklenir (`GameRecord.facility`): oda üreticisinin sürümü değişirse kayıt reddedilir (üretilmiş
haritadaki `map` iziyle aynı kalıp).

**Aşama 6'da kayıt (D20).** `GameRecord.CurrentVersion` **19 kaldı**; tesis izi `"f1"` → `"f2"` oldu
(`FacilityMaps.GeneratorVersion` 2). Neden değişti: tesis kipinde `raid_room` dizgisi yeni parçalar taşıyabilir
(`Body`, `Picked`, `CargoAt`, `Opened`, `Crate`), `raidres` `Extracted` taşır ve aynı raporlar başka sonuç verir (aşama 5
kaydındaki taşınmayan düşen artık ölür; büyük sandık ödülü; kepenk ve sandık dinamit hedefi değil). Yeni komut yok, RNG
sırası değişmedi, bayrak kapalıyken hiçbir komutun sonucu değişmedi (Camp8 iki harita kipinde bayt bayt aynı).

- **AGENTS kural 1'in lafzından bilinçli sapma.** Kural "var olan bir komutun sonucu değişirse sürümü artır" der. Sürüm
  artsaydı geliştiricinin bayraksız bütün kayıtları gereksiz yere yüklenmezdi; oysa etkilenen yalnız tesis kayıtlarıdır.
  Kuralın amacı ("eski kayıt başka oyuna dönmesin") izle karşılanır: `Replay.MapMatches` ve `Replay.Restore` aşama 2–5'in
  tesis kayıtlarını reddeder ("f1 / f2"), izi boş bayraksız kayıtlar aynen yüklenir. `FacilityRecordTests` bunu kilitler.
- `GeneratorVersion` belgesine not düşüldü: tesis kuralları raporun ya da sonucun anlamını değiştirirse de artar. Bayrak
  asset'te kapalı olduğu için oyuncu kaydı yoktur; aşama içi geliştirme kayıtları (A'dan sonra, B'den ya da kapanıştan önce
  alınmış "f2" kayıtları) §12'nin ilkesiyle geçersizdir.
- Geliştirici sürümü artırmayı seçerse yedek yol: `CurrentVersion` 20, sürüm sabitli testler ve `LiberationCampaignTests`
  kayıt testleri; ya da iz "f3" ve `FacilityMapsTests` (`GeneratorVersion == 2`), `FacilityRecordTests`, `FacilityJsonTests`.

**Aşama 8'de kayıt (K1).** `GameRecord.CurrentVersion` **19 kaldı**; tesis izi `"f2"` → `"f3"` (`FacilityMaps.GeneratorVersion`
3; Digest değişmedi). Neden: tesis kipinde yeni komut (`server_reward`), bekleyen ödülde `liberation_raid` reddi, `board_queue`'nun
açılan tariflerle sonucu, üretim kuralları (erzak, eksik kit) ve `raid_room` dizgisinin yeni parçaları; hepsi bayrak arkasında.
Bayrak kapalıyken hiçbir komutun sonucu değişmedi (Camp8 iki harita kipinde bayt aynı): bayraksız kayıtlar yüklenir, aşama 6
tesis kayıtları (f2) reddedilir. Yeni `S.Rng` ya da `Rnd(B)` çekimi yok; tahtanın çekim sırası yalnız ekipman kullanılınca dolaylı
değişir (tutsak ve niyetsiz makine planlanmaz, aygıtın düşürdüğü makine, köpek/tutsak karesi makine adaylarını daraltır).
AGENTS kural 1'in lafzından bilinçli sapma (aşama 6 D20 emsali).

**Aşama 10'da kayıt.** `GameRecord.CurrentVersion` **19 kaldı**; `FacilityMaps.GeneratorVersion` 3 → **4** (iz `"f4"`; Digest aynı,
`0xBB1486CC`); f3 ve f2 kayıtları reddedilir, ana bayrak kapalı kayıtlar yüklenir. Neden (aşama 6 D20 ve aşama 8 K1 emsali; AGENTS
kural 1'in lafzından bilinçli sapma): alt bayrak açıkken f3 kaydındaki `board_queue` (+1 devre), `server_reward` (taslak iptali) ve
`raid_room` (yeni kuyruk) sonucu ve sonraki tahtalar değişir. Durum (`Tally`, `Counter`, `Learned`) kayda yazılmaz, komutlardan
yeniden kurulur. Yeni `S.Rng` çekimi yok; tahtada tek yeni `Rnd` kısa bağın 2. kademesi (bırakılan tutsak aynı planda planlanır);
koku yayıcı ve çift cidar uyanma ve hedef adaylarını dolaylı değiştirir (§6.3 "Uygulanan hâl (aşama 10)"). Doğrulama:
`FacilityCounterTests.Record_F4_LearningCampaignReplays`, `FacilityKit.State`'in önlem ve iş emri satırları,
`FacilityRecordTests`/`FacilityRewardTests`'te f3 reddi; Camp8 `REPLAY=1` 190/190 (§11.6 "Aşama 10 ölçümü"). Yeni Core dosyaları
(`FacilityCounters.cs`, `Baskin.Counters.cs`) motordan bağımsızdır (`float`, `System.Random`, `DateTime` yok).

**Aşama 11'de kayıt.** `GameRecord.CurrentVersion` **19 kaldı**; `FacilityMaps.GeneratorVersion` 4 → **5** (iz `"f5"`; Digest aynı,
`0xBB1486CC`); f4 ve öncesi tesis kayıtları reddedilir, ana bayrak kapalı kayıtlar yüklenir. Neden (aşama 6 D20, 8 K1, 10 emsali; AGENTS
kural 1'in lafzından bilinçli sapma): alt bayraklar açıkken eşikte anlık düşüş yerine kuşatma, `raidres`'in fon ve güven farkı (§9.3),
iki yeni komut (`siege_defend`, `siege_yield`), izli tahtalar. Durum (`Marks`, `Sieges`, `Repairs`) kayda yazılmaz; `raid_room`, `raidres`
ve `Step`'ten yeniden kurulur. Yeni `S.Rng` çekimi yok (savunma tohumu ve dalga türleri ayrı akıştan; iz türetme, birleştirme, onarım,
kendiliğinden bırakma ve `siege_yield` çekimsiz); izler makine adaylarını dolaylı değiştirebilir (kayıt tahtayı oynatmadığı için
zararsız). Doğrulama: `FacilitySiegeTests.Replay_FullCampaignWithBothCommands_SameState` (iz, kuşatma ve onarım satırıyla
`FacilityKit.State`), `Replay_MidDefense_SameBoard`, f4 reddi; Camp8 `REPLAY=1` 130/130 (§11.6 "Aşama 11 ölçümü"). Yeni Core dosyaları
(`FacilityMarks.cs`, `Baskin.Defense.cs`, `Simulation.Siege.cs`) motordan bağımsızdır.

**Aşama 12'de kayıt.** `GameRecord.CurrentVersion` **19 kaldı**; `FacilityMaps.GeneratorVersion` 5 → **6** (iz `"f6"`; Digest değişti:
tohum 79 `0xF47D2411`, alt bayraklar kapalıyken aşama 11'in `0xBB1486CC`); f5 ve öncesi tesis kayıtları reddedilir, ana bayrak kapalı
kayıtlar yüklenir. Neden (aşama 6 D20, 8 K1, 10, 11 emsali; AGENTS kural 1'in lafzından bilinçli sapma): alt bayraklar açıkken na ve oc'nin
planı (Katar'da doğu geçidi), emrin olayı, `raid_room` raporunun kuyruğu (`Follow`, `Decouple`, `"k"`) ve `raidres`'in sonucu (kenardan
düşen yaşar) değişir. Yeni `S.Rng` ya da `Rnd` çekimi yok: kenar, takip, AYIR, yatış ve kayma çekimsiz; `Dress(eastOnly)` çekim sayısını
değiştirmez, yalnız na akışında aday listesini kısaltır; takipçi ve kayan birim makine planını dolaylı değiştirebilir (kayıt tahtayı
oynatmadığı için zararsız). Doğrulama: `FacilityRecordTests.KatarCampaign_Replay_SameState`, `KatarFollowRoom_Resume_SameBoard`, f5 reddi;
`FacilityJsonTests.Json_KatarResultAndReportTail_RoundTrip`; Camp8 `REPLAY=1` 60/60 (§11.6 "Aşama 12 ölçümü"). Yeni Core dosyası
`Baskin.Conditions.cs` motordan bağımsızdır. Kapanışın Core değişikliği (`Unit.Follower`, `Baskin.FollowedInIds`, `Baskin.FirstVisitTurns`,
`Simulation.FacilityRoomTurns`) davranışı değiştirmez (bayt kilitleri ve REPLAY yeniden koşuldu).

**Harita üreticisi.** Bölge adı artık YZ'nin adıdır: `Simulation.RegionName` iki harita kipinde de paketin YZ adını
(`RegionDef.Ai`) döndürür (aşama 3); üretilmiş haritanın ad havuzları (kıyı ve iç bölge adları) aşama 13'te kalkar, aynı dosyadaki
`PackageStage` kalır. On iki imza makinesi ayrı olduğu için "aynı imza düşmanlı iki paket komşu
olamaz" kuralı da kalkar. İkisi de `WorldMap.GeneratorVersion`'ı artırır; tohum 79 altın değerleri (`WorldMapTests`)
güncellenir.

**Sığınak sakinlerinin adları.** Kurucuların ve adayların adları İngilizce özel isimler olur: kurucu ad listesi ve
aday ad listesi (`CandidateData.Names`) değişir. Kurucu listesi 10 ad kalır (`SetupPlanningBoard` içinde `S.Rng` ile
karıştırılır); aday listesi 64 ad kalır (çekimsiz, sırayla). Kurucu adları aday listesinde yer almaz. Ana RNG sırası
değişmez. Adlar çevrilmez, iki dilde aynıdır. Ad listesi aşama 3'te yazıldı ve onaylandı (§14 altıncı tur); kayıt sürümü
19 kalır, çünkü adlar hiçbir komut argümanına girmez. Portreler aynı kişide aynı yüzdür: `PortraitLibrary` anahtarları yeni
adlarla yeniden yazıldı.

**Motor bağımsızlığı.** Yeni Core dosyaları (`FacilityMaps.cs`, `FacilityState.cs`, `Baskin.Rooms.cs`,
`Baskin.Actions.cs`; aşama 8'de `FacilityGear.cs`, `Baskin.Gear.cs`, `Simulation.Facility.cs`) `UnityEngine` kullanmaz; sayılar
`double`, yuvarlama `Js.Round`.

**Aşama 13'te kayıt.** Makine niyetleri mevcut makine evresinde çözülür ve kendi RNG çekimlerini eklemez. Yeni bir oyuncu
komutu gerekmedi. `raid_room` raporunda, yalnız gerektiğinde makine ve nesne dizgilerine işaret eklenir. Tesis izi `f6` → `f7`.
`WorldMap.GeneratorVersion` bir kez 4 → 5 oldu. Üretilmiş harita paketlerinin imza komşuluğu takası ve kıyı/iç ad havuzları
aynı adımda çıktı. Katmanlar, açılış karıştırması ve ayrı RNG akışları korundu. `GameRecord.CurrentVersion` 20 oldu. Kayıt
süzgeci sürüm 19 dosyalarını reddeder. Dünya haritasının Core sonucu bayt kilidiyle doğrulanır.

**Aşama 14'te kayıt.** Tesis izi `f8`. Başarı ve unvan sayımı `ResolveRaid` ile aynı anda uygulanır, aynı komutlar yeniden
oynatıldığında türetilir. İşler RNG çekmez. Dosya sürümü 20 ve g5 geçerli. Tesis açık `f7` kayıtları tesis izi süzgecinden geçmez.

**Aşama 15'te kayıt.** Tesis açık ve saha kapalıyken iz f8, ikisi açıkken f9, tesis kapalıyken boştur. Dosya sürümü 20, harita g5. Kimlik emri açan komutta yeniden türetilir. Taraf seçimi ve düşman uzmanın niyet seçimi RNG çekmez. Odayı yeniden açmak o odanın başlangıcına döner.

**Aşama 18'de kayıt (kayıt 21).** `GameRecord.CurrentVersion` 20 → **21**. Var olan bir komutun sonucu değişti: `ResolveRaid` verilen
`RaidResult`'ı hiçbir baskında kayda gerçek sonuç diye yazmaz, sonucu oda raporlarından türetir; sürüm 20 kayıtların tek tahta
baskınları başka oyuna dönerdi (AGENTS kural 1). Sürüm 20 dosyası yüklenmez, yeni oyun ilk yazılırken `.v20.backup` olarak arşivlenir
(`SaveStoreTests.Load_RejectsVersion20`). Tesis izi alanı kaldı (üretici değişince iz artırma kalıbı sürsün): değeri hep `f8` (saha
kapalı) ya da `f9` (saha açık, yeni varsayılan); boş iz `MapMatches`'ten geçmez (`FacilityRecordTests.Fingerprint_NeverEmpty`).
`RaidParams.MapSlot`/`ExtraTurns` ve görev ek turları yok; `Rooms` yazımı RNG çekmez. Tesis yolunun sonucu bayt bayt aynı kaldı:
`Library/Calisma/taban21/kilit21.sh` değişiklikten önce yakalanan tabanla 124/124 `cmp` eşit (A g5 saha kapalı, B g5 saha açık, C sabit
dünya, t13K/t14K/t15K, saha replay). Kayıtta tek tahtaya özgü komut türü yoktu; `raidres`'in biçimi değişmedi.

### 11.5 Sunum

| Ne | Yüzey | Not |
|---|---|---|
| Bina planı paftaları | Görev fişinin yanında üç kâğıt (`SiginakHud › Facility plan`) | Aşama 5: `FacilityPlanBuilder` (Bridge) → `FacilityPlanData` → `FacilityPlanSheets`, `FacilityPlanSheet`, `FacilityPlanSketch`; çizim `UILineGraphic` ile; bağ `SiginakMap.plans`; tokenlar `colors.plan.*`, `stamp.noPlan` |
| Sahadaki sızmacı | Telsiz günlüğü, ekip kartı, niyet ve operasyon raporu | Gözlenen belirti telsizde, dönüş ve düşman yeteneği tahtada, yakalanma veya kayıp kadro satırında okunur. Saha tutanağı tecrit kararını taşır. |
| Oda geçişi | Baskın sahnesi | `RaidDataSource.ResetBoard(first: false)` (eşyordamı durdurmadan; `OpenState` yalnız ilk açılışta); sahne yeniden yüklenmez; pafta çevirme geçişi |
| Antet | `RaidBlueprintView` | "KANAT nn · PAFTA n/3" |
| Koordinat öneki | `RaidBoard3D.CellName` ve dört çağrı yeri | "2-C3" |
| Yeni hamleler | `RaidHudView`, yetenek kartı, tahta | **Yapıldı (aşama 7, 2026-11-19; §4 "Uygulanan hâl (aşama 7)").** Aşama 6: Core API (`CanAction`, `ActionTargets`, `PreviewAction`, `DoAction`). Aşama 7: tek tuş E (`RaidHudView.LetterKeys`), kart satırları Row 1'in çalışma anı kopyaları (`UnitCardView`; prefab değişmedi), `RaidViewModel.ActionRows/ActionKey/ActionArmed/CargoOnFloor`, `IRaidCommands.ArmAction/ChooseAction`; metin, seçenek ve önizleme verisi `FacilityMoveText`/`FacilityMoveView` (Bridge). Tahta: `RaidBoard3D.Marks.Ambush/Cargo/Focus`, `AddPreviewStroke`/`AddPreviewGhost`, on yeni Fx dalı (`ambush`, `ambush-hit`, `rush`, `drag`, `throw`, `hold`, `open`, `vent`, `cargo-pick`, `cargo-drop`), `AnimateWalk`, `PickCargo`, `Animate(…, indexBase, total)`. Makine evresi ilk `ambush-hit`'te bölünür. Yük yerde ile yük kayıp ayrımı (`CargoOnFloor`, YERDE damgası, "Yük yerde kalır" satırı); çıkış sayımında ölü gövde dışlanır (`gone` için `!Dead`); son nefes gövdesi ekiple gider (kartta EKİPLE TAŞINIYOR, parça evre sonunda kalkar); `LiberationRaidReward.BigCrate` operasyon raporunda (`RaidPaperFacts.MaterialGains`, `OperationReportBuilder`, `OperationReportData.MaxResults` = 7). Sandık nişinin ve kepengin modeli aşama 9'da |
| Dördüncü birim | Ekip kartları | **Yapıldı (aşama 9, 2026-11-19; §5 "Uygulanan hâl (aşama 9)").** Köpeğin ekip kartı Card 4 (280 px; `RaidHud.prefab › Corners › Team`, kap `ContentSizeFitter`), 4 tuşu; aygıt ve ele geçirilmiş robot kartsız, tutsak tahtada tıkla seçilir. Ekipman tuşu R ve uzmanın kartında tek ekipman satırı; parça modelleri, işaretler, önizleme ve on altı Fx dalı |
| "YAYINDA" lambası, teyp | Baskın sahnesi (`RaidHud.prefab › Corners › Radio desk`) | Aşama 5: kaynak `RaidDataSource.onAir` → `vm.OnAir` ve `TapeSheets()` → `vm.TapeSheets` (`CrtState` baskın boyunca hep Live kaldığı için kaynak olamadı); `RaidOnAirLampView`, `RaidTapeDeckView`; tokenlar `radioDesk.*` |
| ÖNLEM satırı | Görev fişi (`LiberationMapBuilder.Fields`) | **Yapıldı (aşama 10; §6.3 "Uygulanan hâl (aşama 10)").** Beş SAHA yuvası kalır (`SlipFieldsMax`), beşi doluysa KAZANIM ile birleşik satır; ÖNLEM teleksi `SiginakMap.PlayReveal` (`TelexFeed`), telsiz günlüğü `RadioLogBuilder`; metinler `FacilityCounterText` (Bridge, saf) |
| Sonuç teleksi | Teleks | **Yapıldı (aşama 10; §7.3 "Uygulanan hâl (aşama 10)").** `OperationReportBuilder.Telex`, `SiginakOperation.telex`; yalnız tesis kipinde (S3) |
| Kırmızı kalem | Bina planı paftaları | **Yapıldı (aşama 10; §7.1 "Uygulanan hâl (aşama 10)").** `FacilityPlanSheet.counter` katmanı, `plan.counter` |
| Sunucu ödülü | Operasyon raporu, ödül kâğıdı | **Yapıldı (aşama 9; §6.2 "Uygulanan hâl (aşama 9)").** Raporda satır ve ÖDÜLÜ SEÇ; seçim rapordan sonra ele gelen kalıcı kâğıtta (`SiginakServerReward`; EVRAK tepsisi, kayıttan devamda tepside); kilit görev fişinde, telekste, CRT'de ve telsiz günlüğünde |
| Büyük sandık ödülü | Operasyon raporu | Aşama 7: KAZANÇ sandığı içerir; SONUÇLAR'da "Büyük sandık · +{m} metal · +{c} devre" (sektör satırından sonra); 7 yuva aşılırsa değişmemiş YZ alarmı satırı düşer |
| Savunma | Harita, teleks, CRT, görev fişi, baskın sahnesi | **Yapıldı (aşama 11, 2026-11-20; §9 "Uygulanan hâl (aşama 11)").** Masa: `FacilityDefenseText` (Bridge, saf; bütün masa metinleri), `LiberationMapBuilder.SiegeSlip`, `OperationSlip.yieldAction` (BIRAK; `DeskGame.unity`), `MapRegionState.Siege/SiegeDue`, `SlipWarningKind.Siege`, `ShiftReportInput.siege*`, `SiginakMap` kuşatma izleyicisi, `GameController.DefendSector/YieldSector`. Baskın: `FacilityDefenseRaidText` (Bridge), `RaidViewModel.BlueprintDefense/Defense/PassLabel/PassNote/RetreatBody/RulesDefense`, `RaidHudView.FitCells`, `RaidHud.prefab` Cell 9/10, `RaidDataSource.DefenseCard`, `RaidPieceLibrary.relay`, `Relay.prefab`, `thumb_relay` |
| Kalıcı tahta | Bina planı paftaları, baskın tahtası | **Yapıldı (aşama 11).** İzler `FacilityPlanCells`'ten (plan = emirdeki tahta); `FacilityPlanSheetData.Traces`, `PlanItem.Barrier/Scrap`, mürekkep katmanında çizim (sahne değişmedi); onarım hover'ı `Repairs`'ten; baskın tahtasında izler Core'un kurduğu hücrelerle (gedikte moloz parçası) |
| Katar ve Yalpa | Görev fişi, bina planı, operasyon raporu, baskın sahnesi | **Yapıldı (aşama 12, 2026-11-20; §10 "Uygulanan hâl (aşama 12)").** Metinler `FacilityConditionText` (Bridge, saf; şart yoksa null). Fiş OLAY ve kimlik `Simulation.RegionEvent`, bina hover'ı `RegionNote`; pafta `FacilityPlanSheetData.OpenEdges`, `FacilityPlanSketch.DrawOpenEdges`, Yalpa takvimi `Simulation.FacilityRoomTurns`'ten; rapor satırı "vagondan düştü · n vardiya yatış". Baskın: `RaidBoard3D` `brink`/`tilt`/`tilt_next`/`slide` çizimleri, `fall` ve `slide` Fx, `yalpa` canlandırması (`TiltRoll`, `SlideFx`, `FallOffEdge`), `AddPreviewGhostAt`; `RaidDataSource` AYIR (`Detach`, `ToggleDetach`, `Decouple`), `RaidTeamModel.Detach`; `RaidPieceLibrary.hazardBrink/hazardTilt`. Sahne, prefab, model ve ses değişmedi |
| Gözetim cebi | Kartlı pano | Aşama 5: `BoardLayout.WatchJob`, `BoardSnapshot.WatchOpen`, `BoardView3D.watchParts`, `BoardDropRules.WatchOpen`, `FacilityWatchText`; sahnede `Slot_gozet`, `TXT_P_gozet`, `Gözetim bandı` (`GozetimBand_SM`), `Şerit · gozet · 1..3` |
| Bölge adı | Harita etiketi, görev fişi, teleks, CRT, raporlar | YZ'nin İngilizce marka adı (iki dilde aynı), `Simulation.RegionName` |
| Sakin adları | Pano kartları, raporlar, baskın kartları | İngilizce özel isimler. Portreler ada bağlıdır (`PortraitLibrary`): yeni adlar bugünkü portrelerle eşlenir, anahtarlar yeniden yazılır; adı sabitleyen testler güncellenir |
| Tarif seçimi | Kartlı pano, tarif defteri | **Yapıldı (aşama 9; §5.6 "Uygulanan hâl (aşama 9)").** Oyuncu tarifi defterden iş emrine sürükler; defter sayfaları ödülle açılır, kapalı sayfalar görünür; üç girdi ve erzak satırı; depoda ekipmanın ikinci sırası |
| Altı yeni imza makinesi | Görev fişi, savunma, baskın sahnesi | **Yapıldı (aşama 13).** `RaidPieceLibrary` ve altı kurgu prefabı; `RaidDataSource` makine adı, niyet ve kural kartı; `RaidBoard3D` hedef hatları, kırağı işareti ve koşu adımları. Niyet alanı Core'dan okunur |
| Kişisel unvanlar | Operasyon raporu, kişi dosyası, baskın | `SquadOutcome.NewTitles` ile yeni unvan, dosyada etki, mevcut hamle ve köpek kartında etkin değerler. Core menzil okuyucuları önizleme ve botta da kullanılır |

- **Değişenler:** sonuç kartı, operasyon raporundaki SÜRE satırı ve güven göstergesi üç odanın toplamını okur.
- **Rehber:** güncel rehber (`PlanningTutorialFlow`) gerçek baskını kullanır; ilk baskın (karargâh tesisinin 01
  kanadı) üç odalı olur. Adım metinleri ve `PlanningTutorialPlayTests` güncellenir.
- **Uygulanan hâl (aşama 17).** Kartlı rehberin 26 adımı ve kayıtlı adım dizinleri değişmedi; tesis bayrağı kapalıyken
  metinler de bayt bayt aynı. Bayrak açıkken `PlanningTutorialFlow.BodyText` bazı adımlarda tesis metni seçer, ama taban
  metnin tesiste de geçerli derslerini silmez. raidMove ve raidAbility taban metni olduğu gibi gösterir ve sonuna kısa bir
  not ekler (`WithNote`): düşeni yanındaki ya da üstündeki uzman E ile kapıya sürükler, Teknisyen donanımı sabote edebilir.
  goal, exposure, report ve go tabanın derslerini (kaybetme koşulları, izler, kurtuluş yolları, mühür, tarif defteri)
  koruyan tek metinlerdir. goal'de eşikteki anlık karşı saldırının yerini kuşatma alır (SAVUN/BIRAK); report unvanı,
  exposure sahadaki şüpheyi, go E ve R hamlelerini ve KURALLAR kartını ekler. slip ilk kanadın gerçek hâlini söyler:
  MASAYA DÖN, üç pafta PLAN YOK, girilen oda teybe kaydedilir, öbür kaynaklar paftanın üstüne gelince yazar. Henüz anlamı
  olmayan bilgi koşula bağlıdır. result taban metindir, çünkü ilk baskın ikmal kanadına gider ve sunucu ödülü çıkamaz.
  raidTurn'ün önlem notu yalnız yürürlükte önlem varken çıkar (`Simulation.Countermeasures`). Alt bayrağı
  (`facilityTitlesEnabled`, `facilityDefenseEnabled`, `infiltratorFieldEnabled`, `facilityCountermeasures`) kapalı adım
  taban metne döner. Katar/Yalpa'nın ayrıntısı rehberde değil, ilgili kanadın fişinde, paftasında ve kural kartındadır.
  Kartlı 01 kanadı gerçek baskındır; senaryolu eski eğitimde E, R, 4 ve uzman dışı seçim kilitlidir.
  `PlanningTutorialFlowTests`'teki beş yeni test bu kuralları ve rehberin andığı düğme etiketlerinin iki dilde
  oyundakilerle aynı olduğunu sabitler.
- **Lokalizasyon:** bütün yeni metinler `Loc` ile sarılır, İngilizceleri `strings.txt`'e yazılır. Core'un baskın
  günlüğü üretildiği dilde donar; yeni satırlar da aynı kurala uyar.
- **Ses:** yeni saha sesleri telsiz bandı aralıklarına numaralanır; `LoopCue` eklenmez.

### 11.6 Testler ve ölçüm

**Ayakta kalanlar.** Bayrak kapalıyken `RaidEngineTests` (iki altın imza), `TutorialRaidTests`, `RaidMapsTests`,
`RaidEventTests` değişmeden geçer. `Baskin.Signature` yeni alanları yalnız varsa ekler. (Aşama 18: `RaidEngineTests` örnek tahtada
aynen geçer; `TutorialRaidTests`, `FacilityLockTests`, `RaidBotMeasurementTests` ve ana bayrak kapalı testleri silindi; `RaidMapsTests`
havuz testlerini kaybetti, üretici `Generate_Golden` ile kilitli.)

**Güncellenecek.** Sürümü sabit sınayan testler ("18"), `LiberationCampaignTests` kayıt testleri,
`PlanningTutorialPlayTests`, `RegionIdentityTests` (her imza iki bölgede kilidi; aşama 13'te on iki ayrı imzaya göre),
`WorldMapTests` (tohum 79 altın değerleri).

**Eklenecek Core testleri.**

- `FacilityMaps`: 108 oda üretilir; her oda geçerlidir; aynı tohum aynı odaları verir; ana RNG değişmez; oda üretimi
  plan dışında (robot) çekim yapmaz.
- Oda zinciri: can ve hak taşınır; geçiş geri alınmaz; temiz oda oynanmaz; süre dolunca doğru sonuç.
- Hazırlık: her donanımın hedef odadaki etkisi; kırık donanım tekrar baskında kırık.
- Hamleler: pusu, koşu darbesi, sürükleme, fırlatma, iki kişilik iş, menfez; her biri için geri alma tam durumu geri
  getirir.
- Ekipman: engel ekibi çıkıştan koparamaz; aygıt fitil kuralına uyar; ele geçirilmiş robot sonuç listesine girmez;
  köpek kadro dizinlerini kaydırmaz. **Aşama 8'de yazılanlar:** engel ekibi çıkıştan, geçitten ve hedeften
  koparamaz (`FacilityGearTests.Barrier_CannotCutTeamFrom*`); aygıt fitil kuralına uyar (`Gadget_Fuse_IntentsFirst`); aygıt giriş,
  geçit ve menfeze kurulamaz (`Gadget_NotOnGateVentBodyCargo`); süresi biten aygıt hurda kalır (`Gadget_ExpiresToScrap`,
  `Scrap_NotOnGateOrIfCutsTeam`); EMP ve kilit aygıta işlemez (`Gadget_ImmuneEmpAndLock`); ele geçirilmiş robot sonuç listesine
  girmez ve makine sınırına sayılır (`FacilityCaptiveTests.Captive_CountsToMaxEnemies_NotInMachinesReport`); kelepçe kullanılınca
  tükenir (`Clamp_SpentRemoved_EvenOnLoss_UnusedKept`); köpek kadro dizinlerini kaydırmaz ve ölmez
  (`FacilityDogTests.Dog_EntersWithOwner_SlotMinusOne_IndicesUnchanged`, `Dog_Down_RestsAfterRaid_StandingDoesNot`). Her yeni eylemin
  geri alması tam döküm ve imzayla (`AssertUndoRestores`), önizleme = uygulama. Dar kalan (bağımsız inceleme):
  `Gadget_NoRngDraw` tek `EndTurn`'de yalnız yürüyen yemle `Rs` eşitliğine bakar; taret ve vinç kurulumundan sonra çekim sayısının
  aynı kaldığı ayrıca sınanmadı.
- Kayıt: `raid_room` ortasında kaydet ve devam et; oda başından aynı tahta; tam harekât yeniden oynatması aynı
  durumu verir.
- Önlem defteri: sayım tavanı, öğrenme anı, eşitlik sırası; rastgelelik yok. **Aşama 10'da yazılanlar:** sayım her noktada artar, 2. kademede ve sayım kapalıyken
  artmaz, geri alma geri getirir, pusu isabeti yalnız ölçüm anahtarıyla (`FacilityCountermeasureTests`); altı önlem ve beş cevap,
  önizleme = uygulama ve geri alma (aynı sınıf); öğrenmenin saf seçimi, tavan, başarısız baskın, ilk tamamlama, zafer ve dağılmada yok,
  karşı saldırı, emir kopyası, cevap sayfası, taslak iptali, f4 yeniden oynatma (`FacilityCounterTests`); plan = tahta önlem kaplamasıyla
  (`FacilityIntelTests`, 648 tahta; bağlantı testi, aşağıda); Core sayımı = botun eşlemesi (`FacilityBotTests.Tally_CoreEqualsBotMap`), `ODUL=onlem`, `TAK=hic`,
  Söküm'den sonra kit, taslak iptalinden sonra emir, yayıcı (`FacilityBotTests`).
  **Bağlantı testleri (bağımsız inceleme):** `FacilityIntelTests.PlanCells_WithCounters_MatchTheBoard…` ve
  `FacilityPlanSheetTests.Counters_RedPen_…`'te plan = tahta karşılaştırmasının iki tarafı da `ApplyCounterCells`'i çağırır; olay
  bayrağını, kademe kaynağını ve sırayı sınarlar, kuralın kendisini değil. Kuralın bağımsız sınaması
  `FacilityCountermeasureTests.Emitter_PerRoom_Constraints_…` (batıya ≥ 3, bağ, hedef, kendi BFS'i) ve `DoubleWall_*`'dadır; sayım
  noktası → rapor → `Facility.Tally` zinciri PlayMode `FacilityCounterPlayTests`'te (gerçek `ActRush`) ve botun `tl` = `tlb`
  eşlemesinde bağımsız sınanır.
- Savunma: bekleyen saldırı, savun ve bırak komutları, kapanışta kendiliğinden bırakma; üç odada içeri çekilme, dalga
  gecikmesi, röle yalnız hedef odada hedef alınır; kazanılan ve kaybedilen savunmada terk edilen oda başına güven düşer
  ve izleri silinir; bütün ekip kapıdayken tur bitirilemez; deneyim yalnız kazanılan savunmada.
- Sızmacı: yalnız sunucu kanadının baskınında döner, savunmada dönmez; iz gecikmesi 5–6 olan sızmacı ve iz
  bırakabilen insan belirti vermez, 4 ve altı olan verir; sessiz sızmacı da döner.
- Şart parçaları: oda üretiminde konur, planda görünür, makineler şart karelerine konmaz.
- Ölüm kuralı: taşınmayan düşen ölür; temiz odadan toplanan ve tesisten çıkarılan yaşar; geri çekilmede yerde yatan
  ölür; Katar'da açık kenardan düşen ekip üyesi ölmez, `facilityFallInjuryTurns` vardiya yatar.
- Yeniden alınan kanat: bölge fonu, bölge güveni, sunucu ödülü ve öğrenme ikinci kez verilmez.
- Yeni imza makineleri: her birinin niyeti görünür, RNG çekmez; tampon ve çapa makineyi de iter ve çeker; mühür
  girişi, ileri geçidi ve hedefin yanını kapatamaz; montajcının kurduğu dron hurda bırakmaz; makine hurdası girişi,
  geçidi ve çıkış yolunu kapatmaz.
- Bölge adı: `RegionName` on iki pakette YZ adını verir; ad iki dilde aynıdır (çevrilmez).
- Gizlilik: sızmacılı ve sızmacısız kadronun açık tahta durumu belirtiye kadar aynıdır.
- Katar ve Yalpa: kural tarafa bakmaz; olay hasarı tavanı; çıkış güvencesi.

**Bot ölçümü.** Gerçek Core ve `RaidBot` ile, yayın kuralıyla (`RETIRE=1`). En az 80 tohum. Bot insan değildir;
sayılar yön gösterir. Aşama 0'da bot yalnız tek tahtayı oynuyordu; oda zinciri sürücüsü aşama 2'de (`RaidBot.PlayFacility`,
`Camp8` tesis kipi), eylem kimliği (tek yetenek yerine çoklu eylem) aşama 6'da, dördüncü birim (köpek) aşama 8'de eklendi;
tur ve hamle sayacı aşama 0'dan beri vardır. Eşli ölçülecek bot politikaları: hazırlık (hep yap, hiç yapma),
ödül (hep tarif, hep kazanım), ekipman (tek araç, dönüşümlü), savunma (hep savun, hep bırak), içeri çekilme (hep çekil,
hiç çekilme), düşen (hep taşı, hiç taşıma).

| Ölçü | Taban (bugün) | Hedef (b) | Aşama 17 (ölçülen, kaynak t17 aksi yazılmadıkça) | Tuttu mu |
|---|---|---|---|---|
| Baskın başına toplam tur (medyan) | 7 (ort 6,6; p90 8) | 12–16 | 20 tur; gidiş (G+A+H) medyanı 14, bandın içinde (hedefin "toplam" mı "gidiş" mi olduğu §14 yedinci turun açık maddesi) | Tutmadı |
| Baskın başına uzman hamlesi | 26,3 (medyan 27; p90 36) | Tabanın en çok 2,2 katı (≤ 58) | 71,32 hamle (2,71 kat) | Tutmadı |
| Baskın başarısı, hazırlıksız | İkmal %82, sunucu %91 | Tabanla aynı (±5 puan) | İkmal %69,0 (2.364), sunucu %77,4 (818); iki donanım da sağlam | Tutmadı |
| Baskın başarısı, iki donanım kırık | Yok | Hazırlıksızdan 5–10 puan yüksek | %93,65 (425) − %71,15 (3.182) = +22,50 puan | Tutmadı |
| Harekât başına ölü | 6,7 | ≤ 7,5; taşınmayan düşenin ölmesi tabanı yükseltir | 14,88 | Tutmadı |
| Harekât başına kendiliğinden kurtarılan düşen | 9,4 (zaferle bitende 10,9) | Taşınan ve temiz odadan toplanan ≥ %80'i | 1,82 / 9,39 = %19,39; son nefes dahil | Tutmadı |
| Baskın başına kırılan sivil sunucu | 1,67 | ≤ 1,7 | 2,34 | Tutmadı |
| Zafer oranı ("iyi" küme) | %65 | %60–70 | 4/240 = %1,67 | Tutmadı |
| Zafer uzunluğu (medyan) | 48 vardiya | ≤ 48 | 60,5 vardiya (4 zafer) | Tutmadı |
| Savunma baskını başarısı | Yok | %40–60 | 231/231 = %100 (54 tohumda en az bir savunma) | Tutmadı |
| Tek araç politikası ile dönüşümlü politika farkı | Yok | Baskın başarısında (aynı tohumlar, eşli karşılaştırma) tek araç 5–10 puan düşük | t17tek − t17 = +0,88 ±0,51 puan | Tutmadı |
| Kullanılmayan ekipman | Yok | Her ekipman, tarifinin açık olduğu baskınların en az %10'unda (ödül politikası: hep tarif) | Örme %24,69 · lokum %20,85 · taret %24,68 · yem %25,05 · vinç %3,22 · kelepçe %5,51 · köpek %48,72 | 5/7 tuttu; tüm türler hedefi tutmadı |

**Taban ölçümü (aşama 0, 17 Kasım 2026).** Bugünkü oyun, kod değişmeden: `Tools/DengeBotu/tesis0.txt`, "iyi" küme,
80 tohum, 240 harekât. `t0gen` oyunun bugünkü hâlidir (üretilmiş harita, `PICK=rastgele`; yukarıdaki tablo bundan).
`t0dunya` dünya haritasıyla KOS aşama 8 tabanını yeniden üretir (zafer %64, medyan 48, ölü 6,4, sivil 1,67): eklenen
sayaçlar botun oyununu değiştirmedi. İki koşu arasındaki farkların hiçbiri gürültüyü aşmaz (zaferde eşli fark +1,3 puan,
standart hata 4,2). Sayılar ham dosyalardan bağımsız betikle yeniden hesaplandı.

| Ölçü | `t0gen` | `t0dunya` |
|---|---|---|
| Baskın / harekât | 38,3 | 37,5 |
| Sonuç turu: ort, medyan, p90 | 6,63 · 7 · 8 | 6,59 · 7 · 8 |
| Sonuç turu: kazanılan / kaybedilen baskın | 6,45 / 7,60 | 6,38 / 7,67 |
| Tur bütçesi 8 olan baskın | %91 (öbürü 7: Kısa nöbet) | %91 |
| Uzman hamlesi: yürüyüş + yetenek | 13,6 + 12,7 = 26,3 | 13,5 + 12,7 = 26,2 |
| İkmal başarısı: kademe 1 / 2 / 3 / hepsi | %85 / %87 / %76 / %82 | %82 / %85 / %76 / %80 |
| Sunucu başarısı: kademe 1 / 2 / 3 / hepsi | %98 / %95 / %84 / %91 | %96 / %96 / %86 / %92 |
| Bitiş: zafer / süre doldu / taşıyıcı düştü / bütün ekip düştü | %84,7 / %12,1 / %3,1 / %0,1 | %83,8 / %13,4 / %2,8 / %0,0 |
| Düşen / baskın | 0,42 | 0,42 |
| Kendiliğinden kurtarılan düşen: baskın, harekât | 0,25 · 9,4 | 0,25 · 9,3 |
| Kurtarılan düşen / harekât: medyan, p90, en çok | 10 · 15 · 20 | 10 · 14 · 21 |
| Ölen: baskın, harekât | 0,17 · 6,7 | 0,17 · 6,4 |
| Kırılan sivil sunucu / baskın | 1,67 | 1,67 |
| Yeniden alınan sektör baskını / harekât (başarılı) | 1,54 (1,13) | 1,57 (1,16) |
| Yeniden tamamlanan bölge / harekât | 0,14 | 0,11 |
| Yeniden verilen bölge fonu, güven / harekât | 2,1 fon · 0,52 güven | 1,7 fon · 0,42 güven |

Tasarım için çıkanlar:

- **Süre.** Bugünkü baskın 8 turluk bütçenin ortalama 6,6 turunu kullanır, %12'si süre dolarak biter. Üç odalı
  baskının hedefi (12–16 tur) bugünkünün yaklaşık iki katıdır.
- **Ölüm kuralı en büyük risktir.** Bugün harekât başına 9,4 düşen kendiliğinden kurtarılıyor; harekâtların %99'unda
  en az biri. "Taşınmayan düşen ölür" kuralında bunların hepsi taşınmazsa ölü 6,7'den 16'ya çıkar. Hedef tablosuna bu
  yüzden "taşınan ve toplanan ≥ %80" satırı eklendi; aşama 6'da "hep taşı" ve "hiç taşıma" politikalarıyla ölçülür.
- **Taşıyıcı.** Baskınların %3,1'i, ekip çıktığı hâlde yalnız malzemeyi taşıyan Teknisyen düştüğü için kaybedilir
  (kaybedilen ikmal baskınlarının dörtte biri): yük fırlatma (bölüm 4.4) tam bu durumun cevabıdır.
- **Yeniden alınan kanadın ödülü küçüktür.** Bölüm 9.3'ün kestiği bölge fonu ve güveni harekât başına ortalama 2,1 fon
  ve 0,5 güvendir; kural dengeyi ölçülür biçimde değiştirmez.
- **Kademe 3 en zor yerdir.** İkmal %76, sunucu %84–86. Altı yeni makinenin ikisi erken paketlere girdiği için kademe
  1 ve 2 başarısı aşama 13'te bu tabana karşı yeniden ölçülür.
- **Bot sınırları.** Bot hiç geri çekilmez ve tur içi geri almayı kullanmaz; sayılar insan oyuncu için yön gösterir.

**Aşama 2 bot denemesi (bilgi için, 18 Kasım 2026).** Ölçüm değil, sürücünün çalıştığının kanıtıdır; ağırlıklar
ayarlanmadı, resmî ölçüm aşama 4'tedir. `BAL="raidFacilityEnabled=true" RETIRE=1 REPLAY=1 ALT=8`, tohum 1–10, "iyi"
küme, 30 harekât; hepsi `replay=ok`. 212 baskında başarı %45 (ikmal %40, sunucu %88); baskın başına 15,9 tur, 32,7
yürüyüş, 21,1 yetenek. Baskınların %33'ü ara odada süre dolarak bitiyor (hepsi bölme kepenkli odalar); 30 harekâtın
30'u ortalama 9. günde dağılmayla bitti. Donanım politikalarından `hic` (donanıma dokunma) `kir`'den iyi, `hep`
belirgin kötü. Bu sayılar §13'teki "Süre", "Sivil güven" ve "Dönüş bütçesi" risklerini destekliyor; aşama 4'ün ilk
işidir.

**Aşama 4 ölçümü (dikey dilim, 18 Kasım 2026).** Liste `Tools/DengeBotu/tesis4.txt`; bayrak yalnız `BAL`'de açık, "iyi"
küme, 80 tohum, her etiket 240 harekât; taban `t0gen`. Sayılar ham satırlardan iki ayrı bağımsız betikle yeniden
hesaplandı. Bir varyantın içinde üç yapılandırma ilk baskınları aynı oynar; etkin örneklem 80 tohumdur ve standart
hatalar tohum kümelidir.

*Bot önce düzeltildi.* Aşama 2 botunun tesis puanı Teknisyen'i 3, öbürlerini türün yürüyüşüyle sayıyordu (kartlı kadroda
yürüyüş 2–5, profillerin yarısı 2); geç kalan uzmanı ucuz sayıyor, son turda geçide uzaklığı hiç görmüyordu. `RaidBot`
artık kişinin kartındaki yürüyüşü kullanır, geç kalan her uzmanı odanın kaybı sayar ve `kir` süreye sığmayan donanım
hedefini bırakır (`ReviewDeviceGoal`). Bayrak kapalıyken Camp8 çıktısı bayt bayt aynı (üretilmiş ve dünya haritası);
tesiste `REPLAY` temiz. Ayrı tohumlarda (61–80) baskın başarısı %52 → %69, ara odada süre dolması %27 → %10. Bilinen
eski bir sınır: planı seçerken kopyada oynatılan makine evresi RNG tüketir, bot yeni uyanan makineyi bu yolla önceden görür
(iki tahtada aynı; sayılar insan için yön gösterir).

| Ölçü (hedef) | Taban | Devam (`t4kir`) | Tur kısma 4/4 (`T`) | Makine 1/2 (`M`) | Makine 1/1 (`M11`) | `M11` + dönüş 5 (`M11R5`) |
|---|---|---|---|---|---|---|
| Toplam tur medyan, p90 (12–16) | 7, 8 | 18, 22 | 16, 21 | 18, 22 | 18, 22 | 19, 23 |
| Hamle / taban (≤ 2,2) | 1 | 2,26 | 1,81 | 2,28 | 2,31 | 2,36 |
| İkmal / sunucu başarısı (taban ±5) | %82 / %91 | %63 / %84 | %44 / %74 | %68 / %78 | %70 / %84 | %70 / %86 |
| Hazırlıksız başarı, `hic` politikası | %85 | %66 | – | – | – | – |
| İlk baskın, eşli fark `t4kir`'e | – | %60 | −17,5 ± 5,6 | +10,0 ± 4,6 | +12,5 ± 5,1 | +20,0 ± 5,2 |
| Kademe karışımı eşit başarı fark | +20,7 | – | −19,7 ± 2,7 | +3,3 ± 2,1 | +6,6 ± 2,1 | +7,0 ± 2,4 |
| Ölü / baskın (taban 0,17) | 0,17 | 0,54 | 0,30 | 0,49 | 0,47 | 0,39 |
| Bütün ekibin öldüğü baskın | %0,9 | %13,9 | %7,4 | %10,3 | %10,4 | %9,3 |
| Geride kalanlı baskın (`stuck`) | – | %19,6 | %9,4 | %22,3 | %20,5 | %14,7 |
| Kırılan sivil / baskın (≤ 1,7) | 1,67 | 2,68 | 2,22 | 2,43 | 2,31 | 2,34 |
| Zafer (60–70) | %65 | %0 | %0 | %2,5 | %2,5 | %6,7 ± 2,3 |
| Dağılma, medyan günü | %27, 40 | %96, 7 | %96, 7 | %90, 11 | %81, 14 | %86, 14 |

Ayrıca ölçülenler: `t4hic`/`t4hep` (donanım politikası), `MT` (makine 1/2 + tur 4/4: ilk baskın −11,2, kademe eşit
−11,7), `M11G4` (yalnız giriş 4 tur: `M11`'e göre ilk baskın −5,0 ± 2,5), `M11C1`, `M11R5C1` ve `Gev` (`M11` + ara 6 tur + dönüş 5
tur + güven kuralı `liberationCiviliansPerTrust` 1). `C1` içerenler aşama 4'ün dışında, bilgi için. Çıktı: `ozet.py --tesis` ve
`Library/DengeBotu/out/t4*`.

Okuma:

- **Tur bütçesini kısmak başarıyı yıkar.** Ara odaların %97'si bölme kepenklidir, kapalı yol 10 adıma kadar uzar (üçüncü kişi için 11)
  ve kepenk bugünkü motorda kalkmaz (aşama 6). Devamda hedef odaya varan baskınların üçte biri ara odada 5 turun hepsini kullanır;
  4 turda bunlar süre dolarak biter. `T` ve `MT`'nin hedefe giren tur medyanı (16) yalnız erken biten baskınlardan gelir;
  başarılı baskınlarda 18–19. Yalnız giriş odasını kısmak ilk baskında `M11`'den 5 puan düşürür (−5,0 ± 2,5), bütün
  baskınlarda fark yok (kademe eşit +0,8): tur kazandırmaz, kazancı da yoktur.
- **Makine kısmak yararlıdır ama yetmez.** `M11` ilk baskında ve kademe eşit başarıda anlamlı (+12,5; +6,6), `M` zayıf
  (+10 ilk baskında, kademe eşit +3,3 anlamsız); ikisi birbirinden ayrılamaz. Ara oda `M11`'de 1 makineye iner ("yol seçmek"
  tasarımı zayıflar).
- **Dönüş bütçesi 5 tur ayrıca kazandırır.** `M11R5 − M11`: ilk baskın +7,5 ± 3,0, geride kalanlı baskın %20,5 → %14,7,
  ölü/baskın 0,47 → 0,39, ikmalin dönüş ara odasında süre dolarak biten ekip kaybı 91 → 25. §13 "Dönüş bütçesi"nin
  koşulu (geride kalan) tetiklendi.
- **Süre hedefi tutmaz.** Başarılı baskında gidiş (G+A+H) medyanı 13–14 turdur (hedef içinde), dönüş 5–6 tur ekler; toplam
  18–20. Hedef "toplam" sayılırsa dönüş kısalmalıdır, ölüm verisi ise dönüşü uzatmayı ister: hedefin tanımı geliştirici
  kararıdır.
- **Dağılmanın nedeni güven ekonomisidir; Balance'ın tur ve makine alanları bunu çözmez.** Baskın başına beklenen güven
  devamda −1,03, `M11`'de −0,29 (taban +0,43). Yeni botla başarılı baskın yaklaşık başa baştır (−0,04; eski bot −0,49);
  açığın asıl kaynağı başarısız baskındır (−2,78; taban −2,16): üç odada daha çok sivil kırılır; başarısız baskının sivil
  kaybının %41–49'u dönüş odasında biten baskınlardandır (devam, `hic`, `M`, `M11`). Dağılmada son darbe %80–87 bir baskının sivil kaybıdır (karşı
  saldırıyla birlikte %88–94). Güveni artıya çeviren tek alan güven kuralıdır (`C1`:
  `M11` + 1 sivil başına 1 güven, hesapla beklenen +1,34), ama ölçülen dağılma yine %61–65, zafer %10–12; başarı ve sivil kaybı tabana
  yaklaşmadıkça 12 bölgeye varılmaz. Hesapla ölçülen kural çareleri: dönüşte kırılan sivili kayıp saymamak +0,26–0,36,
  giriş ve ara odayı sivilsiz yapmak devamda +0,93, `M11`'de +0,37 (giriş odasınınki tek başına etkisiz).
- **Bütün ekibin ölümü yeni bir kayıp yoludur.** Devamda ölülerin %76'sı ekibin tamamının öldüğü baskınlardandır; dönüşün
  ara odasında süre dolarak biten ikmal baskınlarında ekip tamamen ölür (geride kalan ölür, hayatta kalan olmayınca düşen
  de ölür). Sunucu baskınında hedef yıkıldıktan sonra dönüşte ölen ekip baskını yine kazanmış sayılır (tabanda da var,
  tesiste daha sık: `M11R5`'te 214 baskın).
- **Donanım.** "İki kırık − hazırlıksız" farkı (+27 ile +54 puan; `T`'de n=4) seçim etkisidir: `kir` yalnız rahat odada kırar. Nedensel
  kıyas politikalar arasıdır: `kir − hic` −1,2 ± 2,0 (anlamsız), `hep − kir` −36,7 (5 turluk odada donanıma sapmak odayı
  kaybettirir). Hedef odaya varınca `hep` %89, `hic` %76: hazırlığın hedef odadaki etkisi ~+13 puandır (üst sınır).
- **Karar (geliştirici, 18 Kasım):** `M11R5` Balance varsayılanı oldu (giriş 1, ara 1 makine; dönüş 5 tur; §14 yedinci tur).
  Ölçüm o günkü değerlerle alındı; `tesis4.txt` satırları değerleri açıkça yazar.
- **Bot payı.** Aynı bot tabanda %85 başarı tutturur; tesisle taban arasındaki fark çoğunlukla tasarımdandır. İnsan oyuncu
  geri çekilir, tur içi geri alır; sivil ve süre kaybı daha az olabilir, ama ölçülmedi.

**Aşama 6 ölçümü (hamleler ve ölüm kuralı, 19 Kasım 2026).** Liste `Tools/DengeBotu/tesis6.txt`; bayrak yalnız `BAL`'de
açık, "iyi" küme, 80 tohum, her etiket 240 harekât; `PICK=rastgele`, `DEV=kir`, değerler C# varsayılanları (giriş 1, ara 1
makine, dönüş 5 tur = aşama 4 `M11R5`). Taban `t6taban`: aşama 6'dan önce derlenmiş exe (aşama 5 sonu Core ve bot), aynı
tohumlar; bot tarafında eski kuralın taklidi yok. Standart hatalar tohum kümelidir.

*Testler.* Yeni sınıflar `FacilityMoveTests` (eylem API'si, bedeller, eski tahtada hiçbir hamle yok, her hamlenin kuralı,
kenarları ve geri alması, `Preview_Unchanged`, büyük sandık yerleşimi ve ödülü) ve `FacilityFallenTests` (gövde, sürükleme, yük,
rapor ve zincir, ölüm tablosunun Katar ve savunma dışındaki her satırı, son nefes, kayıt); `FacilityBotTests`'e iki politika ve
sayaç testleri; `FacilityChainTests.FellOff_Interim_RescuedIfSurvivor` → `FellOff_NotCarried_Dies`. Her hamlenin geri alma testi
yansımalı tam döküm ve imzayla (`FacilityKit.FullDump`, `AssertUndoRestores`); `CopyInto_CoversEveryField` her `BState` alanını
bozup geri alır. Mono koşucusu (Core ve motordan bağımsız EditMode sınıfları; `Docs/GELISTIRME.md`): 776 geçti, 3 kaldı (Unity
taklidinin eksikleri: `CandidateGroupTests.PublicJson_CarriesNoHiddenField`, `EndingTests.Privacy_…`,
`EndingTests.NewReaders_DrawNothing`), 5 atlandı ([Explicit]); `FacilityMoveTests` 71, `FacilityFallenTests` 52,
`FacilityBotTests` 26. `RaidEngineTests`'in iki altın imzası ve `FacilityMapsTests.Golden_Seed79` Digest'i değişmeden geçti.
Unity (batchmode, projenin APFS klonunda; editör ekran kilidi yüzünden yanıtsızdı): derleme temiz; EditMode 1732 test, 1727 geçti, 5 atlandı, 0 kaldı (`LocalizationTests`, `FacilityJsonTests`, `Generation_IsFastEnough` dahil); PlayMode 107 test, 93 geçti, 14 kaldı. Kalan 14'ü (`ProductionPlayTests`'in 13 defter/tarif testi ve `PlanningTutorialPlayTests.Guide_…`) batchmode ekranından: aynı testler aşama 6'dan önceki HEAD kopyasında da aynı biçimde düşüyor; editörde yeniden koşulmalı.

*Bayrak kapalı ve kayıt.* Camp8 `pre_dunya` (dünya haritası) ve `pre_gen` (üretilmiş harita) çıktısı aşama 6 öncesi tabanla
`cmp` aynı (her parçadan sonra ve kapanışta; bağımsız incelemede yeniden). Tesiste `REPLAY=1`: C'de 60/60, kapanışta 10/10
`replay=ok`, `.err` boş.

*İlk ölçüm* (parça C; son nefes kuralından ve sayaç düzeltmesinden önce):

| Ölçü | Hedef | `t6taban` | `t6hep` | `t6hepD0` | `t6hic` |
|---|---|---|---|---|---|
| Ölü / harekât | ≤ 7,5 | 7,0 | 13,0 | 12,2 | 13,6 |
| Düşen (son raporda) / harekât | | – | 10,3 | 9,9 | 10,3 |
| Taşınan (sürükleme) / harekât | | – | 1,28 | 1,42 | 0 |
| Temiz odadan toplanan / harekât | | – | 0,20 | 0,19 | 0,21 |
| (Taşınan + toplanan) ÷ taban kurtarılan (9,34) | ≥ %80 | (%100) | %15,8 | %17,2 | %2,2 |
| (Taşınan + toplanan) / düşen | | – | %14,4 | %16,1 | %2,0 |
| Zafer | %60–70 | %6,7 | %2,1 | %2,5 | %1,7 |
| Dağılma (medyan günü) | | %86,2 (14) | %82,1 (22) | %80,4 (20) | %82,9 (24) |
| İkmal / sunucu başarısı | taban ±5 | %70,2 / %85,5 | %80,3 / %84,8 | %78,5 / %83,5 | %79,7 / %84,3 |
| Tur / baskın (medyan) | 12–16 | 19 | 20 | 20 | 20 |
| Hamle / baskın (÷ taban) | ≤ 2,2 | 62,0 | 72,2 (1,16) | 71,6 (1,15) | 71,3 (1,15) |
| Pusu isabeti / kurulan | | – | 0,92 (şişik; gerçeği ≈ 0,40) | 0,92 | 0,94 |
| Kepenk açıldı (ara oda) | | – | %60,8 | %60,5 | %60,2 |
| A'da süre dolması | | %6,8 | %3,6 | %4,3 | %3,7 |
| Geride kalan / harekât | | 2,9 | 4,2 | 3,9 | 3,5 |

D22 kuralı uygulandı: `t6hep` ölü > 7,5 olduğu için `t6hepD0` (`facilityDragMoveCost=0`) koşuldu; o da tutmadı,
**varsayılan 1 kaldı**.

*Sayaç düzeltmesi.* Bağımsız inceleme botun efekt sayacının şişik olduğunu buldu: `RaidBot.CountFx` o anki `FxList`'in hepsini
sayıyordu, `DoMove` ve `EnterGate` listeyi temizlemediği için makine evresinin `ambush-hit`'leri ve önceki eylemin `open`,
`cargo-pick`'leri sonraki adımlarda yeniden sayılıyordu. Fx nesne kimliğiyle tekilleştiren kopya 1192 pusuda ham 0,89'a karşı
gerçek 0,40 verdi. Düzeltme: `CountFx` efektleri `Tally.Counted` kümesiyle bir kez sayar (`CountFx_EachEffectOnce`). Oyun aynı,
yalnız `amb`, `cdrop`, `cpick` sayaçları değişti; `shut` ve `crate` raporlardan okunduğu için zaten doğruydu. İlk ölçümün
"pusu kurulanın %92'si vuruyor, bedava hasar" okuması ve ona dayanan pusu kısma önerisi bu yüzden geçersiz.

*Kapanış ölçümü* (son nefes kuralı, düzeltilmiş sayaçlar, `holdWin` düzeltmesi, kural kenarları): `t6sn` son nefes açık + hep
(yeni varsayılan), `t6sn0` son nefes kapalı + hep (`t6hep`'in kapanış düzeltmeleriyle karşılığı: düzeltilmiş sayaç, holdWin, 7a/b/c ve `LineHits`), `t6snhic` son nefes açık + hiç
taşıma; bilgi için `t6snP0` (pusu kapalı: `facilityAmbushRange=0`) ve `t6snD0` (sürükleme bedelsiz: aşama 8 "Sedye"nin üst
sınırı). Taban yine `t6taban`.

| Ölçü | Hedef | Taban | `t6sn` | `t6sn0` | `t6snhic` | `t6snP0` | `t6snD0` |
|---|---|---|---|---|---|---|---|
| Ölü / harekât | ≤ 7,5 | 7,0 | **12,7** | 12,7 | 15,0 | 13,3 | 12,3 |
| Ölü / baskın | (≈ 0,42) | 0,39 | 0,71 | 0,71 | 0,72 | 0,74 | 0,67 |
| Baskın / harekât | | 17,9 | 17,9 | 17,9 | 20,8 | 17,9 | 18,4 |
| Taşınan (sürükleme) / harekât | | – | 1,63 | 1,23 | 0 | 1,93 | 2,07 |
| Toplanan / harekât | | – | 0,15 | 0,19 | 0,18 | 0,15 | 0,17 |
| Son nefesle çıkarılan / harekât | | – | 0,11 | 0 | 0,09 | 0,24 | 0,14 |
| Son nefes gövdesi (oda başına) / harekât | | – | 1,70 | 0 | 1,45 | 2,61 | 1,71 |
| (Taşınan + toplanan + son nefes) ÷ 9,34 | ≥ %80 | (%100) | **%20,2** | %15,2 | %2,9 | %24,9 | %25,5 |
| Zafer | %60–70 | %6,7 | %2,1 | %0,4 | %2,9 | %0,4 | %1,2 |
| Zafer, eşli fark tabanla | | | −4,6 ±2,4 | −6,2 ±2,3 | −3,8 ±2,4 | −6,2 ±2,3 | −5,4 ±2,3 |
| Dağılma (medyan günü) | | %86,2 (14) | %80,0 (23,5) | %78,8 (22) | %79,2 (28,5) | %80,0 (24) | %83,3 (24) |
| Baskın başarısı: tümü | | %74,2 | %80,2 | %80,6 | %81,3 | %78,5 | %80,0 |
| İkmal / sunucu | taban ±5 | %70,2 / %85,5 | %78,1 / %86,0 | %78,2 / %86,9 | %80,3 / %83,9 | %77,3 / %81,7 | %78,3 / %84,4 |
| Sunucu kademe 3 (n ≈ 130) | | %93,7 | %66,9 | %66,9 | %70,6 | %65,8 | %65,3 |
| Tur / baskın (medyan) | 12–16 | 19 | 20 | 20 | 19 | 19 | 19 |
| Hamle / baskın (÷ taban) | ≤ 2,2 | 62,0 | 71,3 (1,15) | 71,3 | 71,0 | 62,4 (1,01) | 70,2 |
| Pusu kurulan; isabet / baskın (oran) | | – | 9,40; 4,08 (0,43) | 9,35; 4,10 | 9,28; 4,11 | 0 | 9,07; 3,99 |
| Ortak; fırlatma; sürükleme / baskın | | – | 1,49; 0,49; 0,40 | 1,50; 0,48; 0,22 | 1,47; 0,48; 0 | 1,57; 0,54; 0,45 | 1,49; 0,48; 0,51 |
| Koşu darbesi; menfez / baskın | | – | 0,14; 0,19 | | | | |
| Kepenk açıldı (baskın) | | – | %61,8 | %61,0 | %60,5 | %63,2 | %61,3 |
| Büyük sandık açıldı (ikmal) | | – | %5,3 | %6,1 | %5,7 | %7,1 | %5,5 |
| Geride kalan / baskın | | 0,16 | 0,23 | 0,25 | 0,18 | 0,22 | 0,20 |

Eşli farklar (aynı tohumlar): `t6sn − t6sn0` zafer +1,7 ±1,0, ölü −0,02 ±0,55; `t6sn − t6snhic` zafer −0,8 ±1,8, ölü
−2,3 ±0,8; `t6snD0 − t6sn` zafer −0,8 ±1,0, ölü −0,45 ±0,58; `t6snP0 − t6sn` zafer −1,7 ±1,0, ölü +0,57 ±1,06; `t6sn −
t6taban` ölü +5,75 ±0,79.

*Ölüm kategorileri* (`t6sn`, 80 tohum; Camp8'in `fate` alanı: düşen ve geride kalan her kişinin sonu, düştüğü ve bırakıldığı
oda; payda düşen + geride kalan, hepsi baskında):

| Son | / harekât | Pay |
|---|---|---|
| Çıkarıldı: sürükleme | 1,63 | %11,2 |
| Çıkarıldı: temiz odadan toplandı | 0,15 | %1,0 |
| Çıkarıldı: son nefes | 0,11 | %0,7 |
| Öldü: kaybedilen baskın (yerde yatan; kural gereği) | 3,74 | %25,6 |
| Öldü: ayaktakiler varken bırakıldı | 3,49 | %23,9 |
| Öldü: odada ayakta kimse kalmadı | 1,32 | %9,0 |
| Öldü: geride kaldı (ayakta, süre) | 4,16 | %28,5 |

- **Kaybedilen baskın:** en çok gidiş A'sında düşenler (A 1,62; H 0,95; dönüş a 0,57; G 0,53 / harekât); bitiş nedeni süre 454,
  taşıyıcı 354, ekip 90.
- **Bırakıldı** (bırakıldığı oda, / harekât): H 1,42, dönüş a 1,42, dönüş g 0,44, gidiş G ve A 0,20. `t6sn0`'da H 1,90 ve a 1,63;
  `t6snhic`'te H 2,48 ve a 3,39.
- **Geride kalan:** H'de 753, dönüş a'da 228, g'de 18 kişi; 609'u kazanılan baskında (H'nin süresi doluyor, kapıya varamayan
  ölüyor). Tabanda da vardı (2,9 / harekât).
- **Ayakta kimse kalmadı:** 316'sı kazanılan baskında; dönüş odasında süre dolmuş, kimse kapıya varamamış.
- Tabanla fark (+5,75 ölü / harekât): düşen ölümleri 4,04 → 8,55 (+4,5), geride kalan 2,92 → 4,16 (+1,2). Tabanda düşen ancak
  baskın kaybedilince ölüyordu; yeni ölümler aşama 5'te kendiliğinden kurtulan gruplardan gelir: bırakılan, ayakta kimse
  kalmayan ve `hep`'in uzattığı süredeki ek geride kalan.

Okuma:

- **Hedefler tutmuyor.** Ölü 12,7 (hedef ≤ 7,5; taban 7,0). Çıkarılan / taban kurtarılan %20 (hedef ≥ %80; ilk ölçümde %15,8).
  Zafer %2,1 (tabana göre −4,6 ±2,4). İkmal başarısı tabanın +7,9 puan üstünde, "taban ±5" aralığından yukarı taşıyor. Tur
  medyanı 20. Hamle oranı 1,15 hedefin içinde.
- **D22: sürükleme bedeli ölüyü belirlemiyor.** Bedelsiz sürükleme (`t6hepD0`, `t6snD0`) ölüyü 0,4–0,8 indiriyor, gürültüye yakın;
  varsayılan 1 kaldı.
- **Son nefes ölümü değiştirmiyor** (`t6sn − t6sn0` −0,02 ±0,55 ölü). Harekât başına 1,70 gövde son nefes alıyor ama yalnız
  0,11'i son nefesle çıkıyor: kural çoğunlukla gidiş odasında ya da H'de işliyor, gövde "ekiple" sonraki odaya geçiyor, orada yine
  yerde başlıyor ve yeniden taşınmalı. Kurtarılan gövdelerin karşılığında başka gövdeler sonraki odada ölüyor (çıkarılan +0,4,
  bırakılan −0,5, ayakta kimse kalmayan +0,5, kaybedilen baskın +0,3). Zafer +1,7 ±1,0 sınırda. Kural açık kaldı (adil, bedava,
  RNG yok); ölüm hedefine katkısı yok.
- **`hep` ile `hic` farkı harekât başına yanıltıcı.** `hic` harekâtları daha uzun (20,8'e karşı 17,9 baskın). Baskın başına ölü:
  `hep` 0,711, `hic` 0,724; fark −0,013. `hep` baskın başına 0,09 gövde sürüklüyor ama düşen (+0,08) ve geride kalan (+0,055)
  artıyor: bağımsız incelemenin bulduğu `holdWin` etkisi, düzeltmeden sonra küçülerek sürüyor (geride kalan `hep` 4,16, `hic` 3,70
  / harekât; ilk ölçümde fark 0,7). Politikalar baskın başına ölüyle karşılaştırılmalı.
- **Pusu orta güçte bir araç.** Baskın başına 9,4 pusu, 4,1 isabet (oran 0,43). Pusu kapalıyken (`t6snP0`) başarı −1,7 puan
  (sunucu −4,3, ikmal −0,8), zafer −1,7 ±1,0, ölü +0,6 ±1,1 (gürültü). Yeni hamlelerin +9'luk hamle artışının tamamı pusudan:
  bot pusuyu boştaki her uzmanın dolgu hamlesi olarak kullanıyor (baskınların %99,9'unda). Başarının tabana göre +6 puanının
  ≈ 4,3'ü pususuz da kalıyor (taşıyıcı kuralı, ortak iş, fırlatma, bot). Pusuyu kısmak denge için gerekli değil; gerekirse
  "tur başına ekipte bir pusu" hamle yükünü düşürür (başarıya etkisi ≤ 2 puan).
- **Ara oda.** Kepenk baskınların %61'inde açılıyor; A'da süre dolması %6,8 → %3,8. Ara odanın "yol seçmek" amacı kepenkle
  kısmen geri geldi (§14 yedinci tur açık maddesi).
- **Sunucu kademe 3** −27 puan (n ≈ 130) pusuyla ilgili değil (`t6snP0`'da da −28); örnek az, ayrı açık iş.
- **Bot sınırları.** Tek turluk açgözlü bakış; gidiş G/A'sında gövdeye dokunmaz; ikiden çok gövdeyi tek sürükleyiciyle
  sıralamaz; `Field` menfezi bilmez; bağlam önbelleği kepenk açılınca tur içinde yenilenmez. Pusu doğrulamasında makine evresi
  görülmez (D21); yerdeki yükü neredeyse hiç almaz (t6sn: yük 121 baskında düştü, 9'unda alındı); bilinen RNG sınırı (§11.6 aşama 4) sürer.

**Aşama 8 ölçümü (ekipman, ödül, kazanım; 19 Kasım 2026).** Liste `Tools/DengeBotu/tesis8.txt`; bayrak yalnız `BAL`'de açık, "iyi"
küme, 80 tohum (240 harekât), `PICK=rastgele PAR=10 DEV=kir DUSEN=hep`. Taban `t8taban` = aşama 6 kapanışının `t6sn`'i (aşama 8'den
önceki exe aynı çıktıyı verir; yeniden koşulmadı). Bot: `CampaignBot` (vardiya; `ODUL`, `ARAC`) ve `RaidBot.Gear` (baskın).

| Ölçü | Hedef | t8taban (= t6sn) | t8yok | t8tek | t8donus | t8donus0 | t8kaz | t8sedye |
|---|---|---|---|---|---|---|---|---|
| Politika (ODUL/ARAC/GEAR) | | aşama 6 | tarif/yok | tarif/tek | tarif/donus | tarif/donus/hic | kazanim/yok | tarif/yok + Sedye hep |
| Baskın / harekât | | 17,9 | 17,9 | 16,4 | 17,4 | 17,2 | 18,6 | 19,3 |
| **Ölü / baskın** | (t6sn 0,71) | 0,711 | 0,711 | 0,717 | 0,687 | 0,680 | 0,669 | **0,661** |
| Ölü / harekât | ≤ 7,5 | 12,7 | 12,7 | 11,8 | 12,0 | 11,7 | 12,4 | 12,7 |
| Başarı: tümü / ikmal / sunucu | | %80,2 / 78,1 / 86,0 | aynı | %78,4 / 76,1 / 84,4 | %78,7 / 75,8 / 86,4 | %78,8 / 76,7 / 84,5 | %79,8 / 78,0 / 84,5 | %79,3 / 76,7 / 86,6 |
| Zafer; dağılma | | %2,1; %80,0 | aynı | %1,7; %75,4 | %2,5; %74,2 | %0,0; %81,2 | %0,8; %79,6 | %1,2; %81,7 |
| Tur medyanı; uzman hamlesi / baskın (÷ taban) | 12–16; ≤ 2,2 | 20; 71,3 | 20; 71,3 (1,00) | 19; 71,2 (1,00) | 20; 71,5 (1,00) | 20; 71,1 (1,00) | 20; 71,4 (1,00) | 19; 71,0 (1,00) |
| Köpek adımı; tutsak adımı / baskın | | – | 0; 0 | 0; 0 | 5,20; 0,07 | 1,02; 0 | 0; 0 | 0; 0 |
| Kadroda en az bir ekipman (baskın) | | – | %0 | %52,0 | %55,9 | %57,2 | %0 | %0 |
| Üretilen ekipman / harekât; emir; iptal | | – | 0 | 5,05; 7,38; 2,33 | 4,43; 6,46; 1,89 | 4,13; 6,01; 1,68 | 0 | 0 |
| Ölenle silinen ekipman / harekât | | – | 0 | 4,50 | 3,43 (+0,28 tükenen kelepçe) | 3,50 | 0 | 0 |
| Son nefesle çıkarılan; sürüklenerek / harekât | | 0,11; 1,63 | aynı | 0,11; 1,40 | 0,11; 1,51 | 0,09; 1,61 | 0,76; 1,40 | **1,42**; 1,12 |
| Süre / etiket (PAR=10, 12 çekirdek) | ≤ 2,5 × t6sn | – | 523–692 sn | 579 sn | 638 sn | 592 sn | 564 sn | 547 sn |

"Kullanılmayan ekipman" hedefi (`t8donus`; tarifin açık olduğu baskınlarda kullanıldığı baskın payı ≥ %10; parantezde taşındığı
pay ve taşındığında kullanıldığı pay):

| Tür | Tarifi açık baskın | Kullanıldı | Taşındı | Taşındığında kullanıldı | Kullanım / taşındığı baskın | Eylem ve etki / baskın |
|---|---|---|---|---|---|---|
| Örme seti | 3381 | **%27,5** | %36,3 | %75,8 | 1,21 | tel 0,24, kalas 0,11 |
| Dinamit lokumu | 1789 | **%23,4** | %29,5 | %79,5 | 1,21 | 0,15 |
| Yaylı taret | 1822 | **%27,8** | %30,5 | %91,4 | 0,95 | 0,13 (isabet 0,06) |
| Yürüyen yem | 951 | **%23,1** | %25,4 | %90,9 | 0,91 | 0,05 |
| Makaralı vinç | 842 | %5,0 (tutmadı) | %32,4 | %15,4 | 0,15 | 0,01 |
| Kumanda kelepçesi | 1338 | %6,1 (tutmadı) | %11,1 | %55,4 | 0,56 | 0,02 (ele geçirme 0,02, tutsak komutu 0,04) |
| Köpek | 1633 | **%48,0** (bilgi vermez, aşağıda) | %48,0 | %100 | 15,1 | havla 2,49, kokla 0,35, getir 0,00 |

Köpek satırının "kullanıldı" ölçüsü havla + getir + kokla'dır (`ozet.py` `USE["dog"]`). Havla bedelsizdir ve her turda
verilebilir; bot köpeği taşıdığı her baskında havlıyor (taşındığında %100). Bu yüzden köpekte hedef kendiliğinden tutar ve
bilgi vermez (bağımsız inceleme); köpeğin anlamlı kullanım ölçüleri kokla (0,35 / baskın) ve getir (0).

`t8tek` (ilk açılan tür; üretilmiş haritada karargâh `weu` olduğu için hemen hep örme seti): örme seti tarifi açık baskınların
%65,4'ünde taşındı, %53,9'unda kullanıldı; öbür türler hiç üretilmedi.

Eşli farklar (aynı 80 tohum, "iyi" küme, tohum ortalaması ± tohum kümeli standart hata):

| Karşılaştırma | Ölü / baskın | Başarı / baskın | Zafer | Dağılma | Ölü / harekât |
|---|---|---|---|---|---|
| `t8yok − t8taban` | 0 | 0 | 0 | 0 | 0 (bayt aynı) |
| `t8donus − t8donus0` (ekipmanın kendi değeri) | +0,013 ±0,016 | −0,3 ±0,6p | **+2,5 ±1,2p** | **−7,1 ±3,2p** | +0,25 ±0,61 |
| `t8donus − t8yok` | −0,010 ±0,025 | **−2,4 ±0,9p** | +0,4 ±1,5p | −5,8 ±3,2p | −0,72 ±0,69 |
| `t8donus0 − t8yok` (üretim ve taşıma, kullanım yok) | −0,024 ±0,022 | **−2,0 ±0,9p** | −2,1 ±0,9p | +1,3 ±3,0p | −0,98 ±0,67 |
| `t8tek − t8donus` | +0,011 ±0,012 | −0,1 ±0,5p | −0,8 ±1,4p | +1,3 ±3,0p | −0,200 ±0,512 |
| `t8tek − t8yok` | +0,001 ±0,025 | **−2,5 ±0,9p** | −0,4 ±0,9p | −4,6 ±3,2p | −0,92 ±0,74 |
| `t8kaz − t8yok` | **−0,027 ±0,007** | −0,2 ±0,4p | −1,3 ±1,1p | −0,4 ±3,1p | −0,29 ±0,28 |
| `t8sedye − t8yok` | **−0,090 ±0,023** | +0,3 ±0,9p | −0,8 ±1,0p | +1,7 ±2,6p | +0,02 ±0,47 |

Okuma:

1. **Regresyon kilidi tuttu.** `t8yok` aşama 8 anahtarları atılınca `t6sn` ile 24/24 dosyada bayt aynı (80 tohum; son exe ile); bayrak
   kapalıyken `pre_dunya` ve `pre_gen` aşama 6 tabanıyla aynı (K35). Bağımsız incelemede tohum 1–20 × 3 yapılandırmada tam dosya
   `cmp` ile (taban kırpılmadan) yeniden aynı çıktı (6/6); `REPLAY=1` tohum 1–10 × ODUL × ARAC 60/60 `replay=ok`.
2. **Kullanılmayan ekipman hedefi beş türde tuttu, ikisinde tutmadı.** Örme, lokum, taret, yem ve köpek ≥ %10 (%23–48; köpeğinki
   havla yüzünden kendiliğinden, yukarıda). Vinç (%5,0)
   ve kelepçe (%6,1) tutmadı. Vinçte darboğaz durum: taşındığı baskınların %32'sinde yanında; ama hattın ilk dolu karesi gövde, yük ya
   da birim olan bir kurulum yeri nadir (taşındığında %15). Kelepçede darboğaz üretim ve tükenme: kullanılınca silinir, iki devre
   ister, taşındığı baskın %11 (taşındığında %55). Bot tarafında denenenler: aday süzgeci genişletildi (vinç ekip birimini de çeker),
   üretimin eşitlik kuralı değişti (bkz. §14 on birinci tur 64), ağırlıklar ×0,5–×2 tarandı. Denge değişikliği (ör. kelepçe tarifi,
   vincin hattı ya da "her makine evresinin başında kendi önüne yürüyen gövdeyi çeker" gibi bir kural) geliştiricinin kararıdır.
3. **Ekipmanın kendi değeri küçük ve olumlu:** `t8donus − t8donus0` baskın başına ölüde ve başarıda gürültü içinde (+0,013 ±0,016;
   −0,3 ±0,6p), harekât düzeyinde zafer +2,5 ±1,2p, dağılma −7,1 ±3,2p. Beklenti ("ekipman ≥ 0") zafer ve dağılmada tuttu, baskın
   düzeyinde ayrışmadı.
4. **Üretim yolu baskın başarısını ~2 puan düşürüyor; kaynağı belirsiz.** `t8donus0 − t8yok` −2,0 ±0,9p ve `t8tek − t8yok` −2,5 ±0,9p
   (kullanımsız da, yalnız örme setiyle de); düşüş ikmalde (−1,4…−2,3p, kademe 3'te −2…−4p). Bulunanlar: `t8donus`'ta köpeğin taşındığı
   ikmal baskınları %72,8, taşınmayan ama tarifi açık olanlar %79,7; kontrollü deneyde (ilk baskın, aynı tohum, köpekli/köpeksiz,
   480 baskın, kademe 1–3) köpek −1,7p (gürültü içinde). Köpeğe yük fırlatmayı kapatmak (§14 on birinci tur 75) farkı değiştirmedi.
   Adaylar: köpeğin tahtadaki varlığı (yer tutması, makinelere kapalı kareleri), harekâtın yolu (üretimin hammaddesi, baskın sayısı
   ve kademe karışımı: `t8tek` 1,4 baskın / harekât az). **Bağımsız incelemenin adayı (en güçlüsü, doğrulanmadı):** açık iş emri
   varken otomatik üretim hiç çalışmaz (`Simulation.PlanningBoard.cs`, `AdvanceBoardProduction`'ın `if (order == null)` dalı) ve
   ekipman emri zırhla aynı girdiyi (metal + kumaş) tüketir; zırh plakası (ve EMP hücresi, bakım kiti) üretiminin yerini ekipman
   alıyor olabilir. Düşüşün en büyüğü hemen hep örme seti (2 metal + 1 kumaş) üreten `t8tek`'te; düşüş ikmalde ve kademe 3'te büyük.
   Sınama: R satırına kadronun plaka sayısı (ya da C satırına harekât başına üretilen zırh) eklenip `t8donus0 − t8yok` farkının
   plakayla açıklanıp açıklanmadığına bakmak. Ölüm hedefine etkisi yok (ölü / baskın −0,01). Aşama 9/10'a açık iş.
5. **Kazanımlar baskın başına ölüyü düşürüyor:** `t8kaz − t8yok` −0,027 ±0,007 (≈ −%4); seçilenler en çok Kulak (210), Sedye (132),
   Yedek çanta (125), Plan defteri (104). Etkinin çoğu Sedye'den (son nefesle çıkarılan 0,11 → 0,76 / harekât).
6. **Sedye karar kuralı (K24): yeni tanım kesinleşir.** `t8sedye − t8yok` ölü / baskın **−0,090 ±0,023** (−3,9 SE < −2 SE). Son nefesle
   çıkarılan 0,11 → 1,42 / harekât (aşama 6'nın 1,6 üst sınırına yakın); sürüklenerek çıkarılan 1,63 → 1,12 (gövde artık sonraki odada
   yeniden sürüklenmiyor). Harekât başına ölü değişmedi (+0,02 ±0,47) çünkü harekât uzadı (+1,4 baskın, dağılma günü 23,5 → 27):
   ölü ≤ 7,5 hedefi yine tutmuyor (12,7); kaldıraç 2–4 geliştiriciye açık (§14 dokuzuncu tur).
7. **Hamle ve süre hedefleri tuttu.** Uzman hamlesi / baskın tabanla aynı (1,00 ×; köpek adımı 5,2 ayrı sayılır), tur medyanı 19–20
   (hedef 12–16 aşama 6'dan beri tutmuyor), etiket başına süre 523–692 sn (`t6sn`'in ~1,0–1,2 katı; ≤ 2,5 ×).
8. **İlk koşu notu.** Son exe'den önceki iki koşuda (`asama8/D/olcum_exe2`, `olcum_exe3`) ekipman üreten üç etiket harekâtların 7'sini
   fiş reddiyle kaybetti: bot eksik ayrılmış taslak emir bırakıyordu (`BoardPlanCheck` "… adedi … olmalı"); düzeltildi (§14 on
   birinci tur 65), tablo son exe'nin 240/240 harekâtıdır. Botun ağırlık taraması (tohum 1–20, `t8donus`, tasarım değerleri, ×2,
   ×0,5): baskın başına ölü ve başarı farkları ±0,03 ve ±1,4p içinde; tasarım değerleri kaldı (`asama8/D/sw*`).

**Aşama 10 ölçümü (önlem defteri; 19 Kasım 2026).** Liste `Tools/DengeBotu/tesis10.txt`; bayrak yalnız `BAL`'de açık, "iyi" küme,
80 tohum (etiket başına 240 harekât), `PICK=rastgele PAR=10 DEV=kir DUSEN=hep`. Taban `t10taban` = aşama 8'in `t8yok`'u (bağlantı;
`t10yokK0` onunla bayt aynı). Pusu isabeti varsayılan olarak sayılmaz (S1); `A` etiketleri §6.3'ün lafzı (`facilityTallyAmbush=true`).
`K0` = önlem defteri kapalı (`facilityCountermeasures=false`). Bot: `CampaignBot` (`ODUL`, `ARAC`, `TAK`) ve `RaidBot` (önlem
terimleri `Emitter` 6, `AmbushSeen` 0,25). Etiket başına 508–638 sn (toplam 93 dk).

| Ölçü | Hedef | t10yokK0 (= t8yok) | t10tekK0 | t10donusK0 | **t10tek** | **t10donus** | t10tekA | t10donusA | t10onlem | t10yok | t10uretK0 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Politika (ODUL/ARAC) | | tarif/yok, K0 | tarif/tek, K0 | tarif/donus, K0 | tarif/tek | tarif/donus | tarif/tek, A | tarif/donus, A | onlem/donus | tarif/yok | tarif/donus TAK=hic, K0 |
| Baskın / harekât | | 17,9 | 16,4 | 17,6 | 16,0 | 17,1 | 16,1 | 16,6 | 17,0 | 17,9 | 17,9 |
| Başarı: tümü / ikmal / sunucu | | %80,2 / 78,1 / 86,0 | %78,4 / 76,1 / 84,4 | %78,6 / 75,8 / 86,4 | %78,9 / 76,2 / 86,4 | %78,1 / 75,3 / 86,0 | %77,3 / 74,6 / 84,8 | %78,1 / 75,2 / 86,5 | %79,2 / 77,2 / 84,8 | %80,1 / 78,2 / 85,3 | %79,4 / 76,9 / 86,4 |
| Ölü / baskın; ölü / harekât | ≤ 7,5 / harekât | 0,711; 12,7 | 0,717; 11,8 | 0,689; 12,1 | 0,730; 11,7 | 0,685; 11,7 | 0,716; 11,5 | 0,721; 12,0 | 0,688; 11,7 | 0,704; 12,6 | 0,687; 12,3 |
| Zafer; dağılma | | %2,1; %80 | %1,7; %75 | %2,5; %75 | %0,4; %80 | %2,1; %75 | %0,4; %82 | %0,8; %78 | %2,1; %74 | %2,1; %81 | %2,5; %79 |
| **Önlem / harekât** (I4, I11) | | 0 | 0 | 0 | **0,71** | **1,19** | 2,37 | 2,48 | 1,19 | 0,05 | 0 |
| Önlem sayısı dağılımı (harekât) | | | | | 0:147, 1:22, 2:65, 3:6 | 0:142, 1:15, 2:32, 3:24, 4:9, 5:11, 6:6, 7:1 | 0:30, 1:51, 2:43, 3:42, 4:65, 5:9 | 0:30, 1:52, 2:53, 3:44, 4:25, 5:20, 6:8, 7:8 | 0:141, 1:14, 2:41, 3:11, 4:16, 5:9, 6:8 | 0:228, 1:11, 2:1 | |
| Sınıf başına önlem / harekât (2. kademe) | | | | | örme 0,68 (0,30), itme 0,03 | örme 0,50 (0,13), köpek 0,35 (0,14), aygıt 0,18 (0,04), yıkım 0,12 (0,02), itme 0,03 | aygıt 1,54 (0,66), örme 0,79 (0,31), itme 0,04 | aygıt 1,53 (0,66), örme 0,55 (0,14), köpek 0,23 (0,07), yıkım 0,11 (0,02), itme 0,06 | örme 0,47 (0,11), köpek 0,36 (0,14), aygıt 0,20 (0,05), yıkım 0,13 (0,03), itme 0,02 | itme 0,05 | |
| İlk önlemin sınıfı (önlemli harekât) | | | | | örme %100 | örme %85, köpek %9, aygıt %4, yıkım %2 | aygıt %100 | aygıt %100 | örme %84, köpek %9, aygıt %4, yıkım %2, itme %1 | itme %100 | |
| İlk öğrenme günü (medyan; ort) | | | | | 11; 13,6 | 11; 14,5 | 2; 3,3 | 2; 3,3 | 12; 15,2 | 38; 40,7 | |
| Önlemli baskın (emirde en az bir sınıf ≥ 1) | | %0 | %0 | %0 | %41,8 | %42,4 | %79,2 | %79,8 | %41,5 | %2,8 | %0 |
| **Cezalı taşıma** (I5; eşya-baskın) | tek > donus | – | – | – | **%61,4** (4041) | **%38,5** (4383) | %59,1 | %49,5 | %37,2 | – | – |
| Core sayımı / baskın (tavanlı; itme/yıkım/örme/ele/köpek/aygıt) | | – | – | – | 0,13/0/0,48/0/0/0 | 0,13/0,11/0,32/0,02/0,34/0,18 | 0,13/0/0,57/0/0/1,06 | 0,14/0,11/0,39/0,02/0,26/1,03 | 0,13/0,11/0,32/0,02/0,33/0,17 | 0,13/0/0/0/0/0 | – |
| Tutarlılık `tl` ≠ `tlb` (I2) | 0 | | | | 0 / 3843 | 0 / 4108 | 0 / 3869 | 0 / 3977 | 0 / 4072 | 0 / 4287 | |
| Pusu isabeti / baskın | | 4,08 | 3,73 | 3,70 | 3,77 | 3,67 | 2,13 | 2,13 | 3,67 | 4,08 | 4,05 |
| Önlem etkisi / baskın: kesici kol; yayıcı kırıldı; susturulan köpek-tur; parazit; cıvata; Söküm; çift cidar | | | | | 0,267; 0; 0; 0; 0,007; 0,002; 0 | 0,047; 0,175; 0,114; 0,057; 0,011; 0,002; 0,013 | 0,295; 0; 0; 0; 0,015; 0,007; 0 | 0,072; 0,098; 0,057; 0,141; 0,029; 0,016; 0,010 | 0,049; 0,163; 0,107; 0,058; 0,005; 0; 0,017 | 0; 0; 0; 0; 0,016; 0,005; 0 | |
| Cevap açık baskın; seçilen cevap / harekât | | %0; 0,72 | %0; 0,62 | %0; 0,72 | %26,3; 0,57 | %30,1; 0,63 | %27,3; 0,55 | %29,6; 0,60 | %8,2; 0,14 | %28,1; 0,71 | %0; 0,70 |
| Vinç; kelepçe: tarifi açık baskında kullanıldı (I9) | ≥ %10 | – | – | %4,7; %6,0 | – | %4,9; %6,0 | – | %10,2; %5,8 | %4,6; %6,2 | – | – |
| Üretilen ekipman / harekât | | 0 | 5,05 | 4,48 | 5,00 | 4,25 | 4,98 | 4,09 | 4,25 | 0 | 1,99 |

Eşli farklar (aynı 80 tohum, tohum ortalaması ± tohum kümeli standart hata; `ozet.py --esli`, `--fark`):

| Karşılaştırma | Başarı / baskın (p) | Ölü / baskın | Dağılma (p) | Zafer (p) | Baskın / harekât | Cezalı taşıma (p) |
|---|---|---|---|---|---|---|
| **`t10tek − t10donus`** (hedef satırı) | **+0,08 ±0,57** | +0,023 ±0,011 | +5,4 ±3,3 | −1,7 ±1,0 | −1,10 ±0,58 | +11,1 ±2,3 |
| `t10tekK0 − t10donusK0` (önlemsiz) | −0,15 ±0,48 | +0,010 ±0,012 | +0,4 ±2,9 | −0,8 ±1,4 | −1,13 ±0,68 | 0 |
| **`(t10tek − t10tekK0) − (t10donus − t10donusK0)`** (tek aracın önlemle cezası) | **+0,23 ±0,54** | +0,012 ±0,008 | +5,0 ±2,9 | −0,8 ±1,6 | +0,02 ±0,46 | +11,1 ±2,3 |
| `t10tek − t10tekK0`; `t10donus − t10donusK0` | +0,20 ±0,43; −0,03 ±0,36 | +0,009 ±0,006; −0,003 ±0,004 | +4,6 ±2,4; −0,4 ±1,9 | −1,3 ±0,9; −0,4 ±1,1 | −0,44 ±0,34; −0,46 ±0,36 | |
| `t10tekA − t10donusA` (lafız) | −0,32 ±0,47 | −0,000 ±0,008 | +4,2 ±2,6 | −0,4 ±0,7 | −0,45 ±0,56 | +4,9 ±2,3 |
| `(t10tekA − t10tekK0) − (t10donusA − t10donusK0)` | −0,17 ±0,61 | −0,011 ±0,013 | +3,8 ±4,1 | +0,4 ±1,5 | +0,68 ±0,87 | +4,9 ±2,3 |
| `t10tekA − t10tek`; `t10donusA − t10donus` (pusunun sayılması) | −0,56 ±0,77; −0,16 ±0,81 | −0,005 ±0,015; +0,018 ±0,016 | +1,7 ±3,0; +2,9 ±3,3 | 0 ±0,6; −1,3 ±1,1 | +0,11 ±0,78; −0,55 ±0,79 | |
| `t10tekK0 − t8tek` (olay nişanı, I7) | 0 (bayt aynı, 240/240) | 0 | 0 | 0 | 0 | |
| `t10donusK0 − t8donus` (olay nişanı) | +0,02 ±0,10 (26/240 harekât ayrışır) | +0,001 ±0,001 | +0,8 ±0,8 | 0 | +0,13 ±0,11 | |
| `t10onlem − t10donus` (cevap tarifi, I8) | +0,65 ±0,26 | +0,004 ±0,004 | −0,8 ±1,7 | 0 ±1,0 | −0,15 ±0,36 | −0,9 ±0,8 |
| `t10yok − t10yokK0` (ekipmansız oyuncuya önlem) | −0,06 ±0,07 | −0,004 ±0,002 | +1,3 ±0,9 | 0 ±0,6 | −0,01 ±0,10 | |
| `t10uretK0 − t10yokK0` (üretim ekonomisi, I10) | −1,61 ±0,82 | −0,017 ±0,022 | −1,3 ±2,7 | +0,4 ±0,9 | +0,07 ±0,94 | |
| `t10donusK0 − t10uretK0` (taşıma + kullanım) | −0,74 ±0,67 | +0,008 ±0,016 | −3,8 ±3,1 | 0 ±1,6 | −0,37 ±1,01 | |

Farkların farkında tür başına "tarifi açık baskında kullanıldı" (tek − donus, önlemli − önlemsiz): örme seti −1,5 ±0,7p, öbürleri
gürültü içinde (`fark10.txt`).

Kısa tarama (tohum 1–20, "iyi" küme, 60 harekât; `asama10/tara_ozet.txt`): `W.Emitter` 0 / 3 / 6 / 12 (`t10donus`): yayıcının
susturduğu köpek-tur köpek önlemli baskında 1,14 / 0,96 / 0,76 / 0,77, kırılan yayıcı 1,07 / 1,20 / 1,18 / 1,21; başarı farkı 6'ya
göre −0,2 ±0,2 / +0,3 ±0,3 / +0,3 ±0,5p (gürültü). `W.AmbushSeen` 1 / 0,5 / 0,25 / 0,125 (`t10donusA`): aygıt 2. kademe baskında pusu
9,6 / 6,1 / 4,7 / 4,0, isabet 2,0 / 1,2 / 1,0 / 0,8; başarı farkı 0,25'e göre 0 ±1,0 / +1,7 ±1,2 / −1,2 ±1,2p, dağılma +10 ±6 / −3 ±8 /
+10 ±5p. İkisi de gürültü içinde; varsayılanlar (6; 0,25) kaldı.

**Okuma.**

1. **İddialar (I1–I3) tutar.** Bayrak kapalı (`pre_dunya`, `pre_gen`, `pre_gen2`) ve `t10yokK0` = `t8yok` (80 tohum, 24/24 dosya, aşama
   10 anahtarları atılarak) bayt aynı; `t10tekK0`/`t10donusK0` tohum 1–20 A0 exe'siyle bayt aynı; aynı komutun iki koşusu (`t10tekK0`
   ve `t10donusK0` tohum 1–20, ayrı koşularda) bayt aynı. Core sayımı botun eşlemesiyle her R satırında aynı (24 156 baskın, ayrıca
   REPLAY kümesinin 2741 baskını). `REPLAY=1` 190/190 (tohum 1–10 × `ODUL` {tarif, kazanim, onlem} × `ARAC` {yok, tek, donus} ×
   `facilityTallyAmbush` {false, true} + `TAK=hic`; cevap seçen 15 harekât dahil), `.err` 0 bayt, on etikette 240/240 harekât, fiş
   reddi yok.
2. **Öğrenme ve eşik (I4, I11; §15 "Önlem eşiği" kapanır).** Pusu sayılmazken tek araç harekât başına 0,71 önlem alır (hep örme;
   ilk önlem medyan gün 11), dönüşümlü 1,19 (dört sınıf; örme %85 ilk). Harekâtların %59–61'i hiç önlem görmez (bölge tamamlamadan
   dağılır ya da eşiğe varamaz); baskınların %42'si önlem altında. Kâğıt tahmini (keşif §2.3: 0,71 ve 1,30) tuttu. Lafızda (`A`)
   ilk önlem **%100 `aygit`** ve medyan gün 2; harekât başına 2,4–2,5 önlem, baskınların %79–80'i önlem altında, %60–61'i makine
   aklı altında: politika ayırt edilmez (S1'in gerekçesi doğrulandı). `ele` hiç öğrenilmedi (kelepçe baskın başına 0,02).
   Ekipmansız oyuncu (`t10yok`) yalnız koşu darbesinden `itme` alır (0,05 / harekât, gün 38).
3. **Cezalı taşıma (I5) tutar:** tek araçta taşınan eşyanın %61'i önlemli sınıfta, dönüşümlüde %39 (eşli fark +11 ±2p; lafızda
   %59 / %50). "Tek araç cezalandırılır, dönüşümlü kaçınır" önlemin maruziyetinde görülür.
4. **Hedef (I6) tutmadı.** `t10tek − t10donus` baskın başarısı +0,1 ±0,6p (hedef: tek araç 5–10p düşük); farkların farkı +0,2 ±0,5p,
   lafızda −0,2 ±0,6p. Önlem ölçülür ama baskın başarısına yansımaz: kesici kol örme setinin değerini keser (`cut` 0,27 / baskın)
   ama örme setinin kendi değeri zaten ~0 (aşama 8); bot önlemli sınıfta aracı bırakmıyor (tarifi açık baskında örme seti kullanımı tekte önlemsiz %46, 1. ve 2.
   kademede %59; dönüşümlüde %35 → %23 → %17; zamana bağlı seçimle karışık). Yalnız harekât düzeyinde zayıf işaretler var: tek araçta dağılma +5,0
   ±2,9p (~1,7 SE) ve ölü / baskın +0,012 ±0,008 (~1,5 SE; ikisi farkların farkı). Aşama 10 dengeyi değiştirmedi (S4); geliştiriciye seçenekler
   §14 on üçüncü tur S4.
5. **Olay nişanı (I7) gürültü içinde:** tek araçta hiçbir harekât değişmez (80 tohum bayt aynı), dönüşümlüde 240 harekâtın 26'sı
   ayrışır, başarı +0,02 ±0,10p.
6. **Cevap tarifi (I8):** `ODUL=onlem` cevabı harekât başına 0,14 kez seçer (`tarif`'te 0,63); cevap açık baskın %8 (%30) ama hepsi
   önlemle eşleşir (dönüşümlüde 993 Söküm baskınının yalnız 43'ünde cıvata vardı). Başarı +0,65 ±0,26p: farkın kaynağı boş cevap
   yerine alınan kazanımdır (saf cevap etkisi değil). Cevabın işlediği baskın az: Söküm 0,002–0,016 sökme / baskın; Çelik tel
   kesici kolla karşılaştığı 245 baskın.
7. **Vinç ve kelepçe (I9):** kullanım hedefi (≥ %10) yine tutmuyor: dönüşümlüde vinç %4,9, kelepçe %6,0 (önlemsiz %4,7 / %6,0);
   kısa bağ hiç öğrenilmediği için kelepçe etkilenmedi; vincin sınıflarında (aygıt, itme) 1. kademede %8,1 (234 baskın),
   2. kademede %0 (58). Lafızda vinç
   %10,2 (makine aklı pusunun değerini düşürünce bot vinci daha çok kuruyor).
8. **Üretim yolu (I10):** `t10uretK0 − t10yokK0` −1,6 ±0,8p (üretir ama takmaz: ekonomi), `t10donusK0 − t10uretK0` −0,7 ±0,7p
   (taşıma ve kullanım). Plaka adayı **çürüdü**: "tarif açık, kadro boş" baskınlarında kadronun plakası her etikette 0,09–0,14
   (bot zırhı hiç takmaz; başlangıç zırhı tarifsiz erken baskınlarda 0,80), yani üretim yolları arasında plaka farkı yok. Düşüş
   ekonomide (`TAK=hic` stoğu 3'te durdurur: 1,99 ekipman / harekât; "tarif açık" baskınlarında başarı %79,1, ekipmansızda
   %80,0) ve "kadro boş" baskınların seçiminde: dönüşümlüde boş kadronun başarısı %73,8, eşyalı kadronun %79,9. Kadro gücü
   eş değişkenleri seçimi tek başına açıklamıyor (boş kadronun canı 7,4, ekipmansızın 6,7; yürüyüşü 8,8, ekipmansızın 9,7; çaylak
   ve yarım tayın hiç yok): boş kadro baskınları harekâtın belli anlarında toplanıyor (tarif açıldıktan hemen sonra, eşya ölümle
   silindikten sonra). Kaynak bulunmadı; plaka değil.
9. **Ekipmansız oyuncu (`t10yok`):** önlemden etkilenmez (başarı −0,06 ±0,07p); Söküm 915 baskında açık, 54'ünde cıvatayla eşleşti.
10. **Ölü (I12):** 11,5–12,6 / harekât; ölüm hedefi (≤ 7,5) yine tutmuyor. Önlem baskın başına ölüyü değiştirmiyor
    (`t10donus − t10donusK0` −0,003 ±0,004); harekât başına ölüde gürültü sınırında bir azalma var (−0,39 ±0,22, ~1,8 SE),
    çoğu baskın sayısının azalmasından (−0,46 ±0,36 baskın / harekât).
11. **Kapanış (parça E) ölçümü değiştirmedi.** Kapanışın Core değişikliği (`Baskin.WinchPulls`: vincin kuralı bir okuyucuya
    taşındı, davranış aynı) ve İngilizce metin düzeltmeleri sonrası yeniden koşuldu: bayrak kapalı (`pre_dunya`, `pre_gen`,
    `pre_gen2`) = `asama6/taban`; `t10yokK0` tohum 1–10 ve 71–80 × 3 yapılandırma = `t8yok` (6/6); `REPLAY=1` (`facilityTallyAmbush=true`,
    ODUL {onlem, tarif} × ARAC {tek, donus} × tohum 1–5) 20/20; `t10donus` ve `t10onlem` tohum 1–10 ölçüm çıktılarıyla bayt aynı.

**Aşama 11 ölçümü (kalıcı tahta ve savunma; 20 Kasım 2026).** Liste `Tools/DengeBotu/tesis11.txt`; bayrak yalnız `BAL`'de açık, "iyi" küme,
80 tohum (etiket başına 240 harekât), `PICK=rastgele PAR=10 DEV=kir DUSEN=hep ODUL=tarif ARAC=donus`. Taban `t10donus` (aşama 10 ölçümü;
`t11K0` onunla bayt aynı). `K0` = iki alt bayrak kapalı, `iz` = savunma kapalı (izler ve onarım açık, anlık karşı saldırı), `Iz0` = izler
kapalı. Bot: `CampaignBot.Savun` (`SAVUN`), `RaidBot.Retreat` (`CEKIL`), savunma puanı `Relay` 10, `WaveGhost` 1. Etiket başına 653–836 sn
(toplam 82 dk; Unity'de paralel parça çalışırken). Exe `asama11/olc/exe` (P3 sonu).

| Ölçü | Hedef | t11K0 (= t10donus) | t11iz | t11birak | **t11savun** | t11cekil | t11savunIz0 | t11baski |
|---|---|---|---|---|---|---|---|---|
| Politika (BAL / SAVUN, CEKIL) | | K0 | savunma kapalı | hic | hep, hic | hep, hep | izler kapalı; hep, hic | hep, baski |
| Baskın / harekât | | 17,1 | 17,8 | 17,8 | 18,4 | 17,1 | 17,0 | 18,3 |
| Başarı (saldırı baskınları) | | %78,1 | %78,6 | %78,9 | %78,5 | %79,0 | %78,0 | %78,5 |
| Ölü / harekât | ≤ 7,5 | 11,7 | 12,8 | 12,6 | 13,2 | 12,0 | 11,9 | 13,2 |
| Zafer; dağılma | | %2,1; %74,6 | %0,8; %72,9 | %0,4; %79,6 | %1,7; %73,8 | %1,7; %78,8 | %2,9; %72,1 | %1,2; %75,0 |
| Kuşatma / harekât | | 0 | 0 | 1,52 | 1,70 | 1,47 | 1,45 | 1,69 |
| savun / bırak / kendiliğinden / kanatsız anlık | | – / – / – / 1,60 | – / – / – / 1,73 | 0 / 1,50 / 0 / 0,17 | 0,78 / 0,91 / 0 / 0,17 | 0,68 / 0,75 / 0 / 0,17 | 0,63 / 0,78 / 0 / 0,18 | 0,77 / 0,90 / 0 / 0,17 |
| Savunma reddi (kadro yok) / harekât | | | | | 0,91 | 0,75 | 0,78 | 0,90 |
| **Savunulabilen kuşatma** (savunma / (savunma + red); kapanış, inceleme 2) | | | | | **%45,9** (186 / 405) | %47,5 (164 / 345) | %44,5 (151 / 339) | %46,1 (185 / 401) |
| Kuşatma başına tutulan kanat (`SAVUN=hep`; reddedilen bırakılır) | | | | | %45,9 | %46,7 | %44,5 | %46,1 |
| Bırakılan kanada baskın reddi (`nrYield`) / harekât | | | | 0,55 | 0 | 0 | 0 | 0 |
| **Savunma başarısı** (S `ok`; tohum kümeli SE) (I4) | **%40–60** | | | | **%100,0 ±0,0** (186) | %98,2 ±0,7 (164) | %100,0 (151) | %100,0 (185) |
| Kademe 1 / 2 / 3 | | | | | %100 (15) / 100 (98) / 100 (73) | %100 (15) / 98,9 (95) / 96,3 (54) | %100 / 100 / 100 | %100 / 100 / 100 |
| Terk 0 / 1 / 2 | | | | | 186 / 0 / 0 | 0 / 0 / 164 | 151 / 0 / 0 | 177 / 8 / 0 |
| Güven / savunma: tutulan; düşen | 0/−1/−2; −1/−2/−3 | | | | −0,12 (186); – | −2,03 (161); −3,00 (3) | −0,09; – | −0,15; – |
| Savunma başına düşen / ölü / çıkarılan | | | | | 0 / 0 / 0 | 0,18 / 0,16 / 0,01 | 0,01 / 0 / 0,01 | 0 / 0 / 0 |
| Dalga / gelen makine / düşürülen | | | | | 3 / 9,9 / 9,8 | 3 / 9,6 / 8,3 | 3 / 10,0 / 9,8 | 3 / 9,9 / 9,7 |
| Röle son can; isabet (H'ye girilen) | | | | | – | 3,88; 0,12 (161) | – | – |
| Engel (tel+kalas) / savunma | | | | | 0,24 | 0,09 | 0,15 | 0,23 |
| Savunma turu; oyuncu turu; uzman hamlesi | | | | | 10; 10; 36,3 | 10; 10; 37,3 | 10; 10; 35,8 | 10; 10; 36,2 |
| Yeniden alma baskını / harekât | | 1,30 | 1,46 | 1,45 | 0,87 | 0,67 | 0,64 | 0,84 |
| Yeniden tamamlanan bölge; verilmeyen / harekât (I8) | | 0,087; 0 | 0,125; 0 | 0,117; 0,117 | 0,067; 0,067 | 0,050; 0,050 | 0,029; 0,029 | 0,058; 0,058 |
| Verilen yeniden tamamlama fonu / harekât | | 1,31 | 1,88 | 0 | 0 | 0 | 0 | 0 |
| İzler / harekât: yapılan; onarılan; terkle silinen; kırılan; son | | 0 | 52,9; 8,1; 0; 0,26; 44,5 | 54,1; 8,7; 0; 0,27; 45,2 | 56,1; 8,3; 0; 0,29; 47,5 | 52,4; 7,2; 0,98; 0,27; 44,0 | 0 | 56,0; 8,2; 0,02; 0,29; 47,5 |
| Emirde görülen iz / baskın | | 0 | 0,30 | 0,27 | 0,20 | 0,20 | 0 | 0,20 |
| I3 defter: tutmayan harekât | 0 | 0/240 | 0/240 | 0/240 | 0/240 | 0/240 | 0/240 | 0/240 |
| Tekrar baskını başarısı: izli / izsiz (I9) | | – / %55,5 (733) | %51,7 (325) / %61,6 (435) | %54,5 / 61,2 | %52,0 / 60,6 | %52,6 / 61,2 | – / %52,5 (752) | %52,6 / 60,5 |
| Tekrar baskınında A'da tur: izli / izsiz | | – / 4,01 | 3,71 / 4,05 | 3,67 / 4,05 | 3,69 / 4,06 | 3,74 / 4,05 | – / 3,97 | 3,69 / 4,06 |
| S `tl` ≠ `tlb` | 0 | | | | 0/186 | 0/164 | 0/151 | 0/185 |
| Süre (sn) (I11) | ≤ 1,3 × 621 | 700 | 676 | 679 | 836 | 653 | 663 | 744 |

Eşli farklar (aynı tohumlar, tohum ortalaması ± tohum kümeli SE; `asama11/olc/esli11.txt`):

| Fark | Zafer (p) | Dağılma (p) | Ölü / harekât | Baskın / harekât | Son güven | Öteki |
|---|---|---|---|---|---|---|
| `t11iz − t11K0` (izler ve onarım) | −1,3 ±1,1 | −1,7 ±3,2 | **+1,10 ±0,45** | +0,70 ±0,60 | +0,43 ±0,33 | ölü / baskın +0,034 ±0,015; başarı / baskın +1,2 ±0,9p |
| `t11birak − t11iz` (bekleyen saldırı + §9.3, hep bırak) | −0,4 ±0,7 | **+6,7 ±3,3** | −0,20 ±0,32 | +0,02 ±0,47 | **−0,88 ±0,34** | `nrYield` +0,55 |
| **`t11savun − t11birak`** (I5) | +1,3 ±0,9 | **−5,8 ±2,3** | +0,58 ±0,36 | +0,51 ±0,45 | **+0,66 ±0,24** | yeniden alma −0,58 ±0,11 |
| **`t11cekil − t11savun`** (I6) | 0,0 ±1,2 | +5,0 ±2,7 | **−1,22 ±0,25** | **−1,27 ±0,32** | −0,40 ±0,26 | savunma başarısı −1,2 ±0,7p (48 tohum) |
| `t11cekil − t11birak` (I6) | +1,3 ±1,1 | −0,8 ±3,0 | −0,64 ±0,31 | −0,76 ±0,44 | +0,26 ±0,24 | |
| `t11savun − t11savunIz0` (I10) | −1,3 ±1,3 | +1,7 ±3,7 | **+1,34 ±0,47** | **+1,31 ±0,57** | −0,15 ±0,36 | savunma başarısı 0 (43 tohum) |
| `t11baski − t11savun` | −0,4 ±0,4 | +1,3 ±0,7 | −0,04 ±0,03 | −0,04 ±0,04 | −0,03 ±0,03 | 8 savunmada çekildi |
| `t11savun − t10donus` (aşama 11'in toplamı) | −0,4 ±1,1 | −0,8 ±3,3 | **+1,48 ±0,52** | +1,23 ±0,74 | +0,21 ±0,32 | |

İddialar (aşama 11):

| # | İddia | Sonuç |
|---|---|---|
| I1 | Bayt kilitleri | **Tuttu.** Bayrak kapalı `pre_dunya`, `pre_gen`, `pre_gen2` = `asama6/taban`; `t11K0` = `t10donus` **24/24** (S satırı yok, aşama 11 anahtarları atılarak) |
| I2 | Kayıt | **Tuttu.** `REPLAY=1` tohum 1–10 × `SAVUN` {hep, hic, yok} × `CEKIL` {hic, hep} × `ARAC` {yok, donus} + `facilityMarks=false`: **130/130** `replay=ok`, `.err` 0 bayt (42 savunma, 78 komutla bırakma, 56 kendiliğinden bırakma) |
| I3 | İz defteri | **Tuttu.** Yedi etiketin 1 680 harekâtında `marksMade − marksGone − marksRepaired − marksWiped = marksEnd` |
| I4 | Savunma başarısı %40–60 | **Tutmadı: %100,0** (186 savunma, 48 tohum; kademe 1/2/3 hep %100). K38 taraması (aşağıda okuma 1) bandı tutturan değer bulamadı; varsayılanlar değişmedi |
| I5 | "Bırak" seçilebilir | Bot için hayır: hep savunmak dağılmayı 5,8 ±2,3p düşürür, son güveni +0,66 artırır. Hep bırakmak `t11iz`'e göre dağılmayı +6,7 ±3,3p artırır; bırakmaların %37'sinde o vardiya baskın reddedilir (`nrYield` 0,55 / 1,50 bırakma; düşen kanat mevcut bölgede: S2) |
| I6 | Terk edilen odanın bedeli | Hep çekilmek savunmayı kazandırır (%98,2) ama her tutulan savunmada −2 güven öder (−2,03): dağılma +5,0 ±2,7p (hep tutmaya göre); hep bırakmayla farkı gürültüde (−0,8 ±3,0p). Ölü / harekât −1,22: daha az baskın (−1,27) |
| I7 | −3'lük kayıp | 3 kez (yalnız `t11cekil`; 0,013 / harekât); sonraki 5 vardiyada 1 dağılma. `t11cekil`'in 18 dağılmasında son darbe tutulan ama iki oda terk edilen savunmanın güveniydi (`lastHit=savunma`) |
| I8 | §9.3 | **Tuttu.** Savunma açık etiketlerde yeniden tamamlanan bölge fon ve güven vermez (0,029–0,117 / harekât `recapDenied`; verilen fon 0); savunma kapalı `t11iz`'de 1,88 / harekât fon verilir |
| I9 | İzler | İzler ve onarım ölü / harekâtı +1,10 ±0,45 (ölü / baskın +0,034 ±0,015) artırır; saldırı başarısı +1,2 ±0,9p (gürültü); tekrar baskınında A'da süre 4,05 → 3,71 tur (açık kepenk); onarım 8,1 / harekât |
| I10 | İzler savunmada | Fark yok (iki tarafta %100) |
| I11 | Süre | Savunma 10 tur, oyuncu turu 10, uzman hamlesi 36. Etiket süresi 653–836 sn: `t11savun` 836 sn = 1,35 × 621 (lafız 1,3'ü aşar) ama aynı oyunu oynayan `t11K0` bu koşuda 700 sn (1,13 ×; makine yükü): `t11savun / t11K0` = 1,19 |
| I12 | Ölüm | Savunma başına ölü 0 (`t11savun`), 0,16 (`t11cekil`); çıkarılan 0,01. Aşama 11'in toplamı ölü / harekât +1,48 ±0,52: kaynağı savunma değil, izler (I9) ve fazladan baskın (savunulan kanat yeniden alınmaz, bot o vardiyaları ilerleyerek geçirir) |

Okuma:

1. **Hedef tutmadı (I4).** Bot her savunmayı ölüsüz kazanır: makineler batı girişinde yığılır, birbirinin atış hattını keser (dost ateşi,
   çarpışma), ekibin pusu hatlarına girer; ekibin tutacak bir hedefi olmadığı için bot görünen niyetlerden her tur serbestçe kaçar. Tarama
   (`asama11/p3/scan_*.txt`, `scan2_*.txt`, `scan3_*.txt`: doğrudan savunma kurulumu, 30 savunma × kademe 1–3; `asama11/p3/tara/*`: 10 tohum
   harekât): `facilityDefenseWaveBase` 0, 2, 3, 4, 6; `facilityDefenseWaveTurns` 1, 2, 4; `facilityDefenseWaves` 6, 8; `facilityDefenseTurns`
   8, 12 ve birleşimleri — tutmada hepsi **%100**, savunma başına düşen 0. Yalnız röle baskı yapar: `CEKIL=hep` ile `facilityDefenseRelayHp`
   1 → %93, dalga tabanı 4 + röle 1 → %80. Tasarımın izin verdiği alanlar (dalga büyüklüğü, süre, dalga takvimi) bandı tutturamıyor;
   varsayılanlar değişmedi (S3). Kural düzeyinde öneri §14 on dördüncü tur F11 (onaya açık).
2. **Savunmak bot için hep doğru (I5).** Hep savun, hep bıraka göre dağılmayı düşürür ve güveni korur; bedeli yok (savunma ölüsüz). Bu,
   hedefin tutmamasının sonucudur: savunma %50 olsaydı her kaybedilen savunma −1 güven ve kadro riski getirirdi.
3. **Bırak bir vardiya kaybettirir (S2).** `t11birak`'ta bırakmaların %37'sinde bot o vardiya hiç baskın veremez (düşen kanat mevcut
   bölgede, sıradaki sektör o; `nrYield` 0,55 / 1,50 bırakma). Keşfin %96 tahmini kadar değil, çünkü bırakma vardiyalarının bir
   kısmında bot zaten kadrosuzdur.
4. **Savunma reddi.** `SAVUN=hep`'te kuşatmaların yarısından fazlası (0,91 / 1,70) savunulamaz: hepsinin nedeni "üç sağlıklı kişi"
   (`asama11/p3/neden`, 20 tohumda 58/58). Bot kadrosunu baskında eritir; kuşatma çoğu zaman kadro yokken gelir ve kendiliğinden bırakmaya
   döner (`SAVUN=hep` reddi açık `siege_yield`'dir).
   Başlık tablosundaki savunma başarısı (%100) yalnız sahaya çıkabilen savunmaların başarısıdır: karar verilen kuşatmaların %45–48'i
   savunulabilir. Kuşatma başına bakılırsa (`SAVUN=hep`; savunulamayan kanat bırakılır) kanat %44–47 tutulur: hedef bandının içinde,
   ama bandı veren savunmanın zorluğu değil kadronun yokluğudur. Hedef satırı (I4) tasarımın tanımıyla (oynanan savunmanın başarısı)
   okunur; bu ikinci okuma geliştiricinin kararına bırakıldı (§14 on dördüncü tur "Açık sorular" 5).
5. **İzler (I9).** Harekât başına ~53 iz yapılır, ~8 onarılır, ~45 kalır: izlerin çoğu alınmış kanatlarda bekler (onarım yok), emirde
   ancak tekrar ve yeniden alma baskınında görülür (0,2–0,3 iz / baskın). Tekrar baskınında A'da süre ~0,35 tur kısalır (açık kepenk), ama
   izler ölüyü baskın başına +0,03 artırır (2σ). İzli tekrar baskınının başarısı izsizden düşük (%52 / %61) ama bu seçimdir: izi olan tekrar
   baskını, önceki baskının daha derine indiği (zor) kanattır.
6. **Hep çekil (I6).** Röle açığa çıkar ama makineler ona nadiren varır (161 savunmada 0,12 isabet); 3 kayıp. Çekilmenin bedeli güvendir:
   tutulan her savunma −2, dağılma +5p.
7. **Kapanış ölçümü değiştirmedi.** Kapanışın Core değişiklikleri (`Baskin.ObjNames["role"]`, `DoorVisit.Siege`, okuyucular
   `Simulation.DefenseTurns/DefenseWaveCount/DefenseSchedulePreview` ve `RaidChain.DefenseTurns/DefenseWaveCount/DefenseSchedule`;
   davranış aynı) sonrası güncel kaynakla yeniden koşuldu: bayrak kapalı (`pre_dunya`, `pre_gen`, `pre_gen2`) = `asama6/taban`;
   `t11K0` tohum 1–10 ve 71–80 = P3 çıktısı ve = `t10donus`; `t11savun` tohum 1–10 ve 71–80, `t11cekil` ve `t11birak` tohum 1–10 = P3
   ölçüm çıktısı (bayt aynı); determinizm aynı; `REPLAY=1` (130 koşu) hepsi `replay=ok`, `.err` boş.

**Aşama 12 ölçümü (Katar ve Yalpa; 20 Kasım 2026).** Liste `Tools/DengeBotu/tesis12.txt`; ana bayrak yalnız `BAL`'de açık, "iyi" küme,
80 tohum (etiket başına 240 harekât), aşama 11 `t11savun`'un ortamı (`PICK=rastgele PAR=10 DEV=kir DUSEN=hep ODUL=tarif ARAC=donus SAVUN=hep
CEKIL=hic`). Taban `t11savun` (aşama 11 ölçümü; `t12K0` onunla bayt aynı). Bot: `RaidBot.Detach` (`AYIR`), `RaidBot.Edge` (`KENAR`):
`hic` kenardan düşen uzmanı `Down + Abandon` (345; Teknisyen 600), `kacis` 0,3 × `Down` (13,5; 90) sayar. `t12` = `AYIR=hep KENAR=hic`,
`t12ayir0` = `AYIR=hic`, `t12kenar` = `KENAR=kacis`, `t12yalpa` = Katar kapalı (Yalpa tek başına; na asansör). Etiket başına 708–807 sn
(toplam 63 dk; Unity'de paralel parça çalışırken). Hedef satırı yok; okuma ölçütleri ölçümden önce yazıldı (README "Katar ve Yalpa").

Baskın düzeyi (na ve oc baskınları; tabanda asansör ve yağ):

| Ölçü | t11savun (= t12K0) | **t12** | t12ayir0 | t12kenar | t12yalpa |
|---|---|---|---|---|---|
| na baskını / harekât; olay | 1,62; asansör | 1,49; Katar | 1,49; Katar | 1,48; Katar | 1,55; asansör |
| na başarısı (tohum kümeli SE) | %71,8 ±3,8 (390) | %78,7 ±3,6 (357) | %78,8 ±3,6 (358) | %75,5 ±3,7 (355) | %71,3 ±4,1 (373) |
| na ölü / baskın | 0,592 | 0,669 | 0,707 | 0,611 | 0,584 |
| na dönüş odası oynandı: a; g | %80,0; %45,1 | %85,4; %42,0 | %85,5; %42,5 | %83,1; %41,4 | %79,4; %44,8 |
| AYIR fırsatı / baskın; AYIR / fırsat | – | 1,68; %74,1 | 1,68; %0 | 1,64; %71,4 | – |
| Verilmeyen AYIR: tek / acted / gate / pol / auto | – | 66 / 10 / 0 / 0 / 79 | 65 / 10 / 0 / 441 / 85 | 76 / 11 / 0 / 0 / 80 | – |
| `acted` payı (bot ölçütü ≤ %20) | – | %1,7 | %1,7 | %1,9 | – |
| Takipçi / baskın; / dönüş geçişi; takipli temiz oda / baskın (I6) | – | 0,078; 0,047; 0 | 0,204; **0,121**; 0,003 | 0,082; 0,050; 0,011 | – |
| Kenardan düşen makine / baskın (I7) | – | 0,826 | 0,827 | 0,797 | – |
| Kaynak emp / sab / koşu (makine) | – | 268 / 16 / 11 | 269 / 16 / 11 | 256 / 16 / 11 | – |
| Oda A / H / a (G, g: 0) | – | 58 / 122 / 115 | 58 / 125 / 113 | 60 / 112 / 111 | – |
| Kenardan düşen uzman / baskın (I5) | – | 0,014 (5, hepsi A) | 0,014 (5, A) | **0,180** (64, hepsi A) | – |
| Kişi sonu / baskın: e; l (geride kaldı) | 0; 0,218 | 0,014; 0,213 | 0,014; 0,240 | 0,180; 0,203 | 0; 0,217 |
| Yatış / kenardan düşen; kenardan düşüp ölen (I8) | – | 5,00; 0 | 5,00; 0 | 5,00; 0 | – |
| `itme` öğrenilen (na); `cmfx bolt` | 3; 0 | 0; 0 | 0; 0 | 0; 0 | 1; 0 |
| oc baskını / harekât; olay | 0,87; yağ | 0,85; Yalpa | 0,94; Yalpa | 0,76; Yalpa | 0,88; Yalpa |
| oc başarısı (tohum kümeli SE) | %60,1 ±5,0 (208) | %51,2 ±5,5 (203) | %53,8 ±5,2 (225) | %51,6 ±5,4 (182) | %53,3 ±5,4 (210) |
| oc ölü / baskın | 1,221 | 1,360 | 1,320 | 1,396 | 1,276 |
| Yatış / baskın; oda A / H / a (I4) | – | 4,72; 273 / 527 / 158 | 4,79 | 4,69 | 4,66 |
| Kayma / baskın: uzman / makine / gövde / hurda / öbür (I9) | – | 10,5 / 20,4 / 0,47 / 0,14 / 2,4 | 10,6 / 20,6 / 0,42 / 0,16 / 2,3 | 10,4 / 20,5 / 0,55 / 0,14 / 2,4 | 10,2 / 19,0 / 0,49 / 0,10 / 2,3 |
| Yatışta çarpan uzman / makine / baskın | – | 0,73 / 5,45 | 0,73 / 5,69 | 0,70 / 5,54 | 0,65 / 5,36 |
| Olay hasarı ekip / makine / oc baskını (evP / evE) | 0,22 / 0,80 (yağ) | 0,84 / 5,64 | 0,85 / 5,92 | 0,82 / 5,76 | 0,77 / 5,70 |
| Savunma (na+oc): sayı; başarı; şart olayı | 49; %100; – | 26; %100; 0 | 29; %100; 0 | 24; %100; 0 | 45; %100; 0 |
| Yapı denetimleri (G/g'de kenar, savunmada şart, takvim dışı yatış, oc'de `tl ≠ tlb`, kenardan düşüp ölen, `fate e ≠ edgeP`, yatış ≠ 5 × düşen, şartın bölgesi dışında olay) | – | 0 | 0 | 0 | 0 |
| Süre (sn) (I10) | 836 | 737 | 771 | 708 | 733 (`t12K0` 807) |

Bölgenin ilk baskını eşli (aynı harekâtın öneki aynı; tohum kümeli SE; `ozet.py --ilk`):

| Fark | Geçerli çift | Başarı (p) | Ölü | Öteki |
|---|---|---|---|---|
| `t12kenar − t12`, ilk na (I5) | 94/94 (40 tohum) | +1,3 ±2,8 | **−0,06 ±0,05** | kenardan düşen uzman +0,21 ±0,06; kurtarılan +0,20 ±0,06; geride kalan −0,04 ±0,04 |
| `t12ayir0 − t12`, ilk na (I6) | 94/94 | 0,0 ±0,0 | 0,00 ±0,00 | takipçi +0,18 ±0,06; AYIR −1,34 ±0,13; tur −0,03 |
| `t12 − t12yalpa`, ilk na (I7: Katar ile asansör) | 94/94 | +3,3 ±7,0 | −0,06 ±0,16 | kenardan düşen makine +1,21 ±0,19; tur +1,2 ±0,8 |
| `t12yalpa − t12K0`, ilk oc (I9: Yalpa ile yağ) | 50/50 (30 tohum) | +6,1 ±11,0 | +0,29 ±0,23 | yatış +4,85; olay hasarı ekip +0,61 ±0,16; tur −1,5 ±0,8 |

Harekât düzeyi eşli farklar (`ozet.py --esli`): `t12 − t12K0` zafer 0,0 ±1,2p, dağılma −0,8 ±3,3p, ölü / harekât +0,03 ±0,36
(**ölçülebilir değil**); `t12kenar − t12` ölü / harekât −0,36 ±0,13, baskın / harekât −0,47 ±0,20, ölü / baskın −0,001 ±0,006 (ölü azalması
daha az baskından: yatan üye kadroyu daraltır); `t12ayir0 − t12` ölü / harekât +0,15 ±0,13 (gürültü); `t12yalpa − t12K0` ölü / harekât
−0,27 ±0,20 (gürültü).

`KENAR=kacis` ağırlık taraması (tohum 1–20): `EdgeFall` 0,1 / 0,3 / 0,6 × `Down` → kenardan düşen uzman / Katar baskını 0,277 / 0,220 /
0,149; dönüş odasında (a) atılan yalnız 0,1'de (3). İlk na eşli ölü farkı (t12'ye göre) −0,15 ±0,15 / −0,15 ±0,15 / −0,10 ±0,10: gürültü.
0,3 kaldı; `t12sw03`, `t12kenar`'ın tohum 1–20'si ile bayt aynı (politika ve sayı aynı ağırlık).

İddialar (aşama 12):

| # | İddia | Sonuç |
|---|---|---|
| I1 | Bayt kilitleri | **Tuttu.** Bayrak kapalı `pre_dunya`, `pre_gen`, `pre_gen2` = `asama6/taban`; `t12K0` = `t11savun` **24/24** (aşama 12 anahtarları atılarak); önek kilidi `t12 ~ t12yalpa` (ilk na), `t12yalpa ~ t12K0` (ilk oc), `t12ayir0 ~ t12`, `t12kenar ~ t12` (ilk na): dördü de **240/240** |
| I2 | Kayıt | **Tuttu.** `REPLAY=1`, `karisik ucuz hic`, tohum 5 9 14 26 30 41 44 53 58 69 × `AYIR` {hep, hic} × `KENAR` {hic, kacis} + iki alt bayrak kipi × 10: **60/60** `replay=ok`, `.err` 0 bayt; kapsam katarRaids 132, yalpaRaids 198, AYIR 121, kenardan düşen 6, takipçi 7, kenar yatışı 30 vardiya. Determinizm: `t12` kalıbı (`AYIR=hic KENAR=kacis`) tohum 5 9 44 54 iki kez, 4/4 `cmp` aynı |
| I3 | Katar oynanır, kenar yerinde | **Tuttu.** Beş etiketin 1 070 Katar baskını istisnasız (`.err` 0); G/g'de kenar olayı 0; savunmada 0 |
| I4 | Yalpa oynanır, takvim yerinde | **Tuttu.** Takvim dışı yatış 0 (oda başına ≤ A 2, H 3, dönüş 2); oc'de `tl ≠ tlb` 0 (kayma itme sayılmaz) |
| I5 | Kaçış kapısı (§13) | **Bot için gerçekleşmedi.** Kendi üyesini kenardan düşürme 0,180 / Katar baskını (≥ 0,05) ama ilk na eşli ölü farkı −0,06 ±0,05 (< −2 SE değil). Atmaların hepsi A gidişinde (64/64): yetişemeyecek üyeyi kaçışta atma hiç yok; bot ileri geçide geç kalacak ya da yolu uzun üyeyi atar. `hic`'te bile 5 atma (0,014; okuma 3) |
| I6 | AYIR bedelsiz mi (§13) | **Belirleyici değil.** `t12ayir0`'da takipçi 0,121 / dönüş geçişi (< 0,3): takip kuralı fiilen seyrek; ilk na eşli başarı ve ölü farkı 0. `hep`'te AYIR fırsatların %74'ünde verilir; kaçanlar Teknisyen ayakta değil (%11), kendiliğinden geçiş (%13), yetenek harcandı (%1,7) |
| I7 | Kenar itme araçlarını güçlendirir (§10.1) | Bilgi: Katar baskınında 0,83 makine kenardan düşer (EMP %91, sabotaj %5, koşu darbesi %4; A %20, H %41, a %39). Katar ile asansörün ilk na eşli başarı farkı +3,3 ±7,0p (gürültü; denge notu gerekmez). WAYPOINT'te `itme` öğrenilmesi 0 (taban 3); `cmfx bolt` 0. Makine kayıplarında kenarın payı ölçülmedi |
| I8 | Ölüm kuralı | **Tuttu.** Kenardan düşen hiç ölmez (`eDied` 0), ölü sayısına girmez (`fate e`), yatış 5,00 / kenardan düşen |
| I9 | Yalpa'nın bedeli | Bilgi: oc baskınında 4,7 yatış, uzman başına ~3,5 kayma; yatışta çarpan uzman 0,73, makine 5,45 / baskın; olay hasarı ekibe 0,84 / baskın (yağ 0,22). Yalpa ile yağın ilk oc eşli başarı farkı +6,1 ±11,0p, ölü +0,29 ±0,23 (gürültü; denge notu gerekmez). Bölge düzeyi başarı %51–54 / %60 (eşli değil, kademe karışımı) |
| I10 | Süre | **Tuttu.** Etiket 708–807 sn ≤ 1,2 × 836 = 1 003 sn; Katar baskınında oynanan oda 4,20 (asansör 4,16) |

Okuma:

1. **Kaçış kapısı bot için gerçekleşmedi (I5).** Önceden yazılan ölçütün ikinci yarısı tutmadı: `kacis` baskın başına 0,18 üyeyi kenardan
   atar ama ilk na baskınında ölü farkı −0,06 ±0,05. Kâğıt üstü üst sınır (0,215 kurtarılan / baskın, "ayakta geride kaldı" `l`) neredeyse
   hiç kullanılmadı: `l` yalnız 0,213 → 0,203. Neden: kaçışta geride kalan üyenin yanında EMP uzmanı nadiren durur, bot onu kaçışta değil
   gidişte (A) atar. A'da atmanın kaynağı botun puanıdır: üye ileri geçide geç kalacaksa (`GateLate` 150 + `GateMiss` 200) ya da yolu uzunsa
   (`GateScore`'un yol terimi) onu kenardan atmak 13,5'e kurtarır. Bu da bir "kaçış kapısı"dır (odayı kaybetmek yerine yavaş üyeyi 5 vardiya
   yatırmak); insan oyuncu için anlamı ayrıdır (§15). Bedel önerisi gerekmedi (S3).
2. **AYIR belirleyici değil (I6).** Takip kuralı az işler: dönüş geçişi başına 0,12 takipçi gelir (bot kaçışta makineleri geride bırakır ya
   da düşürür). Takipçiyle oynanan temiz oda 0,003 / baskın. `hic` ile `hep` ilk na baskınında aynı sonucu verir. "AYIR hep seçilir" bot
   için doğrudur ama bedelsizliği fark yaratmıyor; bedel önerisi gerekmedi.
3. **`hic` bile 5 kez kendi üyesini attı (0,014 / baskın, hepsi A).** `Down + Abandon` (345) `GateLate + GateMiss`'ten (350) küçük: bot
   odayı kaybetmektense geç kalacak üyeyi atar. Ağırlıklar ölçüm için değiştirilmedi; `hic` "sömürüsüz oyun"un tam alt sınırı değildir.
4. **Katar başarıyı düşürmez.** Bölge düzeyinde Katar %78,7, asansör %71,8 (eşli değil); ilk na eşli fark +3,3 ±7,0p. Kenar makineleri
   ucuza düşürür (EMP), §10.1'in kabul ettiği etki. `itme` önlemi aşama 12 koşularında WAYPOINT'te hiç öğrenilmedi.
5. **Yalpa ağır; eşli fark gürültüde, bölge düzeyi tutarlı biçimde düşük.** Yalpa olay hasarını yağın ~4 katına çıkarır (ekip 0,84 /
   baskın). Yalpa'lı dört etiketin hepsinde oc baskın başarısı tabandan **6,3–8,9 puan düşük** (%51,2–53,8 / %60,1) ve ölü / baskın
   +0,06–0,18 yüksek; ama bu karşılaştırma eşli değildir (kademe ve sıra karışımı) ve ilk oc eşli fark **ters işaretli ve geniştir**
   (+6,1 ±11,0p, ölü +0,29 ±0,23; 50 çift). Önceden yazılan tetik (10 puandan çok düşüş ve eşli > 2 SE) tutmadı, denge notu yok; yönün
   doğrulanması için daha çok eşli çift gerekir (oc'yi erkene alan kip ya da `PROBE`; §14 on beşinci turun açık bıraktıkları).
6. **Harekât düzeyi ölçülebilir değil.** Katar ve Yalpa harekâtın ~2,3 baskınını değiştirir; `t12 − t12K0` zafer, dağılma ve ölü farkı
   ±1–3p / ±0,4 gürültüsünde.
7. **Kapanış ölçümü değiştirmedi.** Kapanışın Core değişikliği (`Unit.Follower`, `Baskin.FollowedInIds`, `Baskin.FirstVisitTurns` ve
   `Simulation.FacilityRoomTurns`; davranış aynı) sonrası güncel kaynakla yeniden koşuldu: bayrak kapalı (`pre_dunya`, `pre_gen`,
   `pre_gen2`) = `asama6/taban`; `t12K0` tohum 1–10 × 3 yapılandırma = `t11savun` (3/3); `t12`, `t12ayir0`, `t12kenar`, `t12yalpa` tohum
   1–10 ve 71–80 = ölçüm çıktısı (24/24 dosya bayt aynı); determinizm aynı; `REPLAY=1` (tohum 5 44 54 69 × `AYIR` {hep, hic} + iki alt bayrak
   kipi × tohum 9 14) 12/12 `replay=ok` (Katar 24, Yalpa 37 baskın, AYIR 23, kenardan düşen 6, takipçi 3), `.err` boş.

**Elle sınama.** Karargâh tesisinin üç kanadı baştan sona; baskın ortasında çıkış ve devam; İngilizce; savunma
baskını; Katar'da vagon ayırma.

**Aşama 13 ölçümü (20 Kasım 2026).** `Tools/DengeBotu/tesis13.txt` ile 80 aynı g5 haritasında `karisik ucuz hic`
politikası oynandı. `t13K` altı eski imzayı, `t13` on iki imzayı kullanır. İki etiket için de `RETIRE=1`, `ALT=8`,
`PICK=rastgele`, `DEV=kir`, `DUSEN=hep`, `ODUL=tarif`, `ARAC=donus`, `SAVUN=hep`, `CEKIL=hic`, `AYIR=hep`, `KENAR=hic`.
Harekât başına farklar aynı tohuma göre hesaplandı. Baskınların sayısı ve yapıldığı vardiya iki tarafta ayrışabilir.

| Ölçü | Eski imzalar `t13K` | Yeni imzalar `t13` |
|---|---:|---:|
| Saldırı baskını başarısı | %78,4 (1.470) | %76,8 (1.638) |
| İkmal / sunucu başarısı | %75,0 / %87,5 | %74,0 / %84,1 |
| Hazırlıksız ikmal / sunucu başarısı | %68,0 / %81,8 | %67,3 / %78,3 |
| İki donanım kırık ile hazırlıksız arasındaki fark | +20,2 puan | +21,0 puan |
| Baskın medyan turu ve ortalama uzman hamlesi | 19 / 71,2 | 19 / 71,0 |
| Ölü / harekât | 13,32 | 15,39 |
| Kırılan sivil sunucu / baskın | 2,42 | 2,37 |
| Zafer | 1/80 (%1,25) | 4/80 (%5) |
| Zafer medyan vardiyası | 65 | 64,5 |
| Savunma başarısı | 65/65 | 84/84 |

Tohum başına eşli ortalama saldırı başarısı farkı −1,44 ± 1,15 puan, ölüm farkı +2,06 ± 1,05, zafer farkı +3,75 ± 2,78
puandır (standart hata). Tesisin genel ölüm, tur, sivil kayıp, zafer ve savunma hedefleri hâlâ açık. Yeni imzalarla
hazırlıksız baskındaki değişim önceki tesis kipine göre 5 puan içinde kalır. Yayındaki tek tahta tabanına göre hedef hâlâ açık.

| Yeni imza bölgesi | Başarı `t13K` | Başarı `t13` | Aynı bölgeye ulaşan tohumlarda fark, puan ± SH |
|---|---:|---:|---:|
| HEADWATER | %86,2 (160) | %78,4 (185) | −5,2 ± 3,9 (47) |
| TESSEL | %80,3 (147) | %87,6 (153) | +12,1 ± 4,3 (39) |
| WAYPOINT | %79,3 (111) | %82,4 (125) | −2,9 ± 7,0 (23) |
| PARAGON | %56,2 (64) | %53,8 (106) | +8,5 ± 8,1 (10) |
| BOREALIS | %84,6 (52) | %60,8 (79) | −10,5 ± 12,0 (9) |
| HALCYON | %43,8 (73) | %55,8 (86) | +1,0 ± 15,3 (7) |

İki koşuda da on iki bölgede baskın kaydı var. Son bölgelere varan ortak tohum sayısı düşük. Katar'da kenardan düşen uzman
sayısı baskın başına 0,009'dan 0,032'ye çıktı; mevcut sayaç makineye özgü sebebi ayırmaz. Düşme başına beş vardiya ve ölüm
olmaması kontrolü tuttu. Ham ölçüm, bölge dökümü ve eşli özet `Library/Calisma/asama13/olc_final/` altında.

Aynı harekât geçmişine sahip ilk bölge baskınları `ozet.py --ilk` ile ayrıca ölçüldü. HEADWATER'da fark 0,0 ± 6,0 puan
(34 tohum) ve TESSEL'de +9,5 ± 9,5 puan (21 tohum). Bölgenin toplam oranı sonraki baskınları ve ayrışan harekât yollarını
da içerir. WAYPOINT'te beş, PARAGON'da sıfır, BOREALIS ve HALCYON'da birer uygun ilk baskın çifti bulundu.

**Onaya açık denge taraması.** İlk 10 tohumda varsayılan yeni imzalar %80,2 (242 baskın), 15,8 ölü/harekât verdi.
`facilityFlameRange=1` ile genel başarı %82,9 (217), ölü/harekât 14,8, HEADWATER %95 → %90. `facilityFrostTurns=1` ile
genel başarı %81,6 (245), ölü/harekât 16,6, BOREALIS %86,7 (15) → %100 (13). Her ayar yalnız kendi yeni alanını değiştirir.
Örneklem ve farklı baskın sayıları nedeniyle öneri varsayılanları korumak ve seçilecek ayarı daha geniş eşli koşuyla doğrulamak.

**Aşama 13 tam doğrulama.** Unity CLI ile derleme temiz. Canlı editörde EditMode 2.224/2.224, PlayMode 130/130.
HEADWATER hedef oda ve prefab testi geçti. Mono 1.117 geçti, üç bilinen taklit katmanı hatası ve beş atlanan test var.
Sabit dünya `taban6`, üretilmiş harita `taban13` ile eşleşti. Sabit haritada imza alt bayrağı kapalı dört tohumun
tesisi önceki uygulamayla bayt bayt eşleşti. `REPLAY=1` dört sabit ve on g5 tohumunda geçti.

**Aşama 14 ölçümü (20 Kasım 2026).** `Tools/DengeBotu/tesis14.txt` ile 32 eşli g5 haritası ve aşama 13'teki `karisik ucuz hic`
politikası. `t14K` unvan alt bayrağı kapalı, `t14` açık. Kapanan koşuların harita izleri aynı. `t14K` satırları koşu etiketi
dışında aşama 13'ün aynı tohumlarıyla tam eşleşti.

| Ölçü | Unvan kapalı | Unvan açık |
|---|---:|---:|
| Saldırı baskını başarısı | %79,2 (683) | %75,3 (620) |
| İkmal / sunucu başarısı | %76,7 / %85,8 | %73,8 / %79,3 |
| Ölü / harekât | 15,09 | 14,16 |
| Zafer | 3/32 | 0/32 |
| Savunma başarısı | 31/31 | 37/37 |

Tohum başına eşli başarı farkı −2,55 ± 0,96 puan, ölüm farkı −0,94 ± 0,92, zafer farkı −9,38 ± 5,24 puan (standart hata).
Toplam 169 unvan, 26/32 harekâtta en az bir unvan. İlk unvan medyanı altıncı vardiya (`S.Day = 5`). Nişancı 109, Sırtlayan 5,
Koçbaşı 0, Köpekçi 1, Kurucu 8 ve Atıcı 46. Koşuların sonunda 15 yaşayan unvan sahibi vardı. Ölüm, zafer ve savunma denge
hedefleri açık. Onay bekleyen sayı seçenekleri §14'te.

**Açık denge taraması.** İlk 12 tohum ve aynı politika kullanıldı. Varsayılan koşuda 200/257 başarılı baskın (%77,8),
13,50 ölü/harekât ve sıfır Koçbaşı vardı. `facilityRamTitleAt=2` ile iki Koçbaşı ve varsayılanla aynı saldırı başarı ve ölüm
sayıları görüldü. Eşik 1 ile 20 Koçbaşı, %77,6 başarı (201/259) ve 14,08 ölü/harekât. Yalnız `facilityTitleAmbushBonus=1`
ile %77,1 başarı (209/271), 14,75 ölü/harekât ve bir zafer. İki değişken de **onaya açık**. Öneri varsayılanları korumak ve
Koçbaşı için 2 eşiğini daha geniş örneklemde ölçmek.

**Aşama 14 doğrulaması.** Unity derlemesi temiz. Batchmode EditMode'da 2.236 geçti, beş atlandı, sıfır hata.
PlayMode'da 115/131 geçti. Kalan 16 test, önceki aşamalarda tanımlanmış 640×480 batchmode sınırlamalarıyla aynı.
Nişancı'nın kadro raporuna yazılması ve altıncı kareye isabeti son PlayMode koşusunda geçti. Canlı editörde ilk Nişancı akışı
geçti; tam canlı test takımı için CLI zaman aşımına uğradı. Mono 1.129 geçti, üç başlangıç taklit hatası ve beş atlanan test.
Sabit dünya ve iki g5 bayt tabanı, dört g5 alt bayrak koşusu ve önceki dört imza kilidi aynı. Dört açık g5 kaydı yeniden oynandı.

**Aşama 15 ölçümü (21 Kasım 2026).** `Tools/DengeBotu/tesis15.txt` ile 32 tohumun her biri aynı kapı, olay, ekipman ve baskın politikalarıyla iki kez oynatıldı. Yalnız saha bayrağı değişti. 64 komut geçmişinin tamamı tekrar oynatıldı. Saha kapalı g5 koşularının çıktısı, koşu etiketi ve tekrar doğrulama işareti çıkarıldığında aşama 14’ün unvan açık grubuyla 32/32 eşleşti.

| 32 eşli g5 tohumu | Saha kapalı | Saha açık |
|---|---:|---:|
| Başarılı saldırı / toplam | 467/620 (%75,3) | 421/586 (%71,8) |
| Ölü / harekât | 14,16 | 13,16 |
| Zafer | 0/32 | 0/32 |

Kabul edilen 26 sızmacının 11’i sessizdi (%42,3). 5 harekâtta toplam 9 kişi taraf değiştirdi, bunların 7’si sahada etkisizleştirildi. Sessiz payının paydası sığınağa kabul edilen sızmacılardır. Botun kadro seçimi saha belirtilerine tepki vermiyor. Tohum başına saldırı başarısındaki eşli fark -2,66 ± 1,01 yüzde puandır (standart hata). İnsan oyuncunun şüphesine bağlı seçim ve genel denge hedefleri açık.

**Aşama 15 doğrulaması.** Bitti ölçütü olan gizlilik testi geçti. Belirti veya dönüşten önce tahtanın açık durumu, hamle önizlemesi, geri alma ve botun bir tur ileri bakışı kimliği ele vermedi. Boş sahneden başlayan batchmode EditMode koşusunda 2.255 test geçti, 5 test atlandı. PlayMode’da 115/132 geçti. 16 test aşama 14’ün 640×480 batchmode koşusunda da kalmıştı. Ek olarak tutsak makine testi, baskın sahnesini yükleme aşamasında 8 saniyelik sınırı aştı. Ekipman sınıfı yeniden çalıştırıldığında bu test dahil 5/5 geçti. Yeni saha senaryosu hem tek başına hem tam PlayMode koşusunda geçti. Mono’da 1.148 geçti, başlangıçta da görülen üç taklit katmanı hatası ve beş atlanan test vardı. Sabit dünya ve iki g5 tabanı ile aşama 13, 14 ve 15’in dörder alt bayrak çıktısı bayt bayt eşleşti.

**Aşama 16 sonucu (21 Kasım 2026).** Aşama 15’in 32 eşli g5 tohumu iki kipte yeniden oynatıldı. 64 harekâtın ham baskın, savunma ve kampanya çıktıları aşama 15, temizlik öncesi ve sonrasında bayt bayt eşleşti. Her harekâtın komut tekrarı başarılı. Üç harita tabanı, aşama 13–15’in dörder alt bayrak kilidi ve dört saha açık tekrar da eşleşti.

Standart 64 harekâtın kayıtlarında hurda komutu sıfır. Bu harekâtlarda ilk köpeğin bulunduğu 20 ayrı kayıt kopyasının hepsinde yeni talep reddedildi. Ayrı t16Hurda deneyinde 5 ve 9 numaralı tohumların ilk köpeği kullanıldı. Eski Core ikisini de hurdalayıp kayda yazdı. Metal değişimi 5. tohumda 0, 9. tohumda +1 oldu. Yeni Core her iki kopyada köpeği korudu, metal ve komut farkı 0 oldu. Önceki Core ile oluşturulan 86 komutluk kaydın sonunda bulunan köpek hurda işlemi ise son sürümde aynı envanter, 32 metal ve aynı RNG değeriyle yeniden oynatıldı.

Batchmode EditMode’da 2.256 geçti, 5 atlandı ve hata çıkmadı. Tam PlayMode’da 116/132 geçti. Kalan 16 test aşama 14’ün bilinen düşük çözünürlük sonuçlarında da kaldı. Ayrı DeskGame denemesinde panonun bırakma işlemi köpeği hurdaya almayı reddetti ve aynı hedef tareti kabul etti (1/1). Mono koşusunda 1.149 geçti, başlangıçta da görülen üç taklit katmanı hatası ve beş atlanan test vardı.

**Aşama 17 ölçümü (21 Kasım 2026).** Hedef tablosundaki t17, g5 haritasında 1–80 tohum × üç yapılandırmadır (karışık/tecrit, karışık/ucuz, kefilli/ucuz; son şalter hiç). 240 harekât, 4.870 saldırı. Saldırılar R, savunmalar S, harekâtlar C satırıdır. Yukarıdaki tek araç farkında her tohum eşit ağırlıktadır. Diğer başarı yüzdelerinde her uygun baskın bir gözlemdir. Zafer uzunluğunun örneklemi sadece dört olduğundan medyan kararsızdır.

**Politikalar.** `Tools/DengeBotu/tesis17.txt`. RETIRE=1, REPLAY=1, PICK=rastgele, DEV=kir, ODUL=tarif, ARAC=donus, SAVUN=hep, CEKIL=hic, DUSEN=hep, AYIR=hep, KENAR=hic. Ana deneyde BAL yalnız üretilmiş haritayı ve tesis kipini seçer. Aşağıda fark, belirtilen politika eksi karşılaştırma politikasıdır. ±, aynı tohumun üç yapılandırması birlikte tutularak hesaplanan bir standart hatadır.

| Eşli karşılaştırma | Saldırı başarısı farkı (puan ± SH, 80 tohum) |
|---|---|
| t17tek − t17, tek araç | +0,88 ±0,51 |
| t17hazirsiz − t17, donanıma hiç gitme | −1,10 ±1,09 |
| t17hep − t17hazirsiz, her donanımı hedefle | −29,72 ±2,65 |
| t17kazanim − t17, takım kazanımı | +1,19 ±0,86 |
| t17birak − t17, kuşatmayı bırak | +1,22 ±0,51 |
| t17cekil − t17, savunmada içeri çekil | +0,67 ±0,44 |
| t17dusen − t17, düşeni taşıma | +2,28 ±1,16 |

**Koşu sayımı ve tarihsel kontrol.** Her hücre 80 farklı tohumdaki üç kapı/olay yapılandırmasını içerir. Her harekâtın komut geçmişi tekrar yürüdü.

| Etiket | Harekât ve doğrulanmış tekrar | Saldırı | Savunma | Zafer | Ölü/harekât |
|---|---:|---:|---:|---:|---:|
| t17 | 240/240 | 4870 | 231 | 4 | 14,88 |
| t17tek | 240/240 | 4550 | 188 | 4 | 13,92 |
| t17hazirsiz | 240/240 | 4171 | 198 | 4 | 13,11 |
| t17hep | 240/240 | 5442 | 530 | 1 | 8,78 |
| t17kazanim | 240/240 | 4781 | 234 | 8 | 14,14 |
| t17birak | 240/240 | 4741 | 0 | 8 | 14,19 |
| t17cekil | 240/240 | 4494 | 203 | 5 | 13,97 |
| t17dusen | 240/240 | 5385 | 273 | 8 | 17,42 |
| t17saha | 240/240 | 4707 | 223 | 3 | 14,20 |
| t17g5 | 240/240 | 9141 | 0 | 152 | 6,44 |
| t17k3 | 240/240 | 9225 | 0 | 159 | 6,58 |
| t0g4kontrol | 240/240 | 9195 | 0 | 156 | 6,66 |

Saha bayrağı açıkken (`t17saha − t17`) saldırı başarısı farkı **−1,34 ±0,40 puan**. Ana hedefler saha bayrağı kapalı ölçüldü. Hiç taşımamak harekât başına ölü sayısını +2,54 ±0,78 artırdı. İçeri çekilmek, her iki etikette de savunma oynanan 54 tohumda savunma başarısını −3,70 ±2,23 puan değiştirdi.

Aynı g5 haritalarında tek tahta 152/240 (%63,33), tesis 4/240 (%1,67) zafer verdi. Harekât başına ölü aynı sırayla 6,44 ve 14,88. Harita sürümü sabitken de denge farkı görülüyor.

**§9.3 ve aşama 11 K3.** Tesis kapalı A13 öncesi arşiv exe’siyle aynı g4 tohumları yeniden oynandı. Etiket ve fazladan `replay=ok` alanı eşitlenip tohum dosyaları eski parça sırasına konduğunda eski `t0gen` ile 240/240 bayt eşleşti. Güncel g5 tek tahta farklı üretilmiş haritadır (0/240 bayt eşleşmesi). Eski g4 exe’sinin yalnız deney kopyasında bölge tamamlanınca `Rewarded` yazılıp okunarak bölge fonu/güveni ilk tamamlanmaya sınırlandı. Oyunun Core’una ve kayıt yoluna değişiklik gerekmedi.

| g4 aynı tohumlar | Yeniden tamamlanan bölge/harekât | Yeniden verilen fon/harekât | Yeniden verilen güven/harekât | Zafer |
|---|---:|---:|---:|---:|
| t0g4kontrol | 0,138 | 2,06 | 0,52 | 156/240 |
| t17k3 | 0,138 | 0,00 | 0,00 | 159/240 |

**K3 hükmü.** Kural aşama 0 tabanını değiştirmiyor. `t17k3 − t0g4kontrol`: zafer +1,25 ±1,25 puan (tohum kümeli standart hata; 156 → 159 gürültü içinde), harekât başına ölü 6,66 → 6,58 (−0,075 ±0,052), baskın başına kırılan sivil 1,673 → 1,672, zafer medyanı 48 → 48 vardiya. 240 harekâtın 210'unun C satırı etiket dışında aynı; ayrışan 30 harekâtın hepsi bölgeyi yeniden tamamlayanlardır. Hedef tablosunun Taban sütunu bu kural altında da geçerlidir; aşama 0 tabanını yeniden koşmak gerekmez. Adına karşın `t17k3` tesis kipi değil, g4 tek tahta koşusudur. Deney git dışındaki bir kopyada yapıldı: `Library/Calisma/asama17/K3.cs` A13 öncesi arşiv exe'sini (`Library/Calisma/asama13/exe_once/camp8.exe`) IL düzeyinde yamalayıp `k3/camp8.exe`'yi üretir, `k3.py` iki etiketi koşar; iki etiket `tesis17.txt`'te yoktur.

Ana tablonun sayıları ve yedi aracın kendine ait paydası ham satırlardan ayrı okuyucuyla yeniden hesaplandı. Her etiketin eşli saldırı farkı ayrıca C satırındaki toplam ve kayıplara karşı R satırındaki tek tek sonuçlardan kontrol edildi.

**Köken.** `tesis17.txt`'in 2.400 harekâtından 2.101'i (sekiz etiketin tamamı ve `t17saha`'nın 181'i) bot düzeltmesinden (§14 yirminci tur K5) önceki `Decide`'ı taşıyan exe ile (koşunun tam exe yolu arşivde yok; o saatte var olan `asama17/exe_once`, `base` ve `p1` kopyaları SHA-1 61077c64, `final` kopyası bayt olarak farklı ama IL'i aynı), 299'u (`t17saha`'nın 59'u ve `t17g5`'in 240'ı) düzeltilmiş `asama17/son/camp8.exe` ile (a4107127) üretildi; arşivdeki `olc.py` yalnız sonuncuyu gösterir. İki exe yalnız `Camp8.Decide`'ın sabotaj dalında ayrılır. Eski exe ayrıştığı tek durumda çöker ve çöken koşu `olc.py`'nin geçerlilik denetiminden (boş stderr, `replay=ok`) geçemez. Denetimde 20 dosya düzeltilmiş exe ile bayt aynı yeniden üretildi (19'u eski exe'nin ürettiği dosyalar); eski exe de düzeltilmiş exe'nin ürettiği iki dosyayı bayt aynı üretti ve ayrıştığı tek durumda (`t17saha` `kefilli-ucuz-hic-48`) çöktü. Tablo değerleri değişmez. `t17k3` `k3/camp8.exe` (8124cc6b), `t0g4kontrol` `asama13/exe_once/camp8.exe` ile koştu. Yinelerken botu önce derle (`Tools/DengeBotu/derle.sh`): `kos.sh` var olan exe'yi yeniden derlemez.

**Açıkların yorumu ve ayar seçenekleri.** Birden çok değer aynı anda değişmedi. Aşağıdakiler ayrı deney önerileridir. Önceki aşamalarda denenmiş yönler atfıyla işaretlidir. `liberation…` alanları bayrak koşulu olmadan tek tahtada da okunur; onları değiştirmek bayrak kapalı oyunu da değiştirir.

| Ölçü ve en yakın hedef sınırına fark | Olası neden ve denenebilecek yön |
|---|---|
| Tur +4, hamle +13,32 | Gidiş (G+A+H) medyanı 14, bandın içinde; fazlası dönüşten gelir (başarılı baskında dönüş medyanı 5). Hedefin toplam mı gidiş mi sayılacağı aşama 4'ten beri açık (§14 yedinci tur açık maddesi). Hamlede gidiş ve dönüşte yeniden oynanan odalar. `facilityEntryMachines` ve `facilityMiddleMachines` zaten 1 (yalnız bakım robotu, aşama 4 `M11R5`); azaltılamaz. Tur bütçesini kısmak aşama 4'te ilk baskında −17,5 puan verdi. `facilityReturnTurns` 5 → 4 dönüşü kısaltır, ama aşama 4'te 4 → 5 geride kalanlı baskını %20,5 → %14,7 indirmişti. |
| Hazırlıksız ikmal ve sunucu, toleransın yaklaşık 7,9 ve 8,7 puan altında | Gidişte elenen baskın da “iki donanım sağlam” grubundadır. `facilityTargetMachines` azaltılması veya `facilityReturnTurns` artırılması ayrı ayrı sınanabilir. Hiç hazırlanmama politikasında gerçekten sıfır kırıklı başarı %74,8 / %83,6. |
| İki donanım kırık, üst sınırın 12,50 puan üzerinde | Her ikisini kırabilen ekip ve kanatlar seçilmiş bir grup. Zorunlu hazırlık politikasında başarı düşüyor. Önce aynı kadroyla ilk baskını karşılaştır; mekanik etki hâlâ yüksekse `facilityPanelQuietTurns` veya `facilityValveTargetHp` azaltılabilir. |
| Ölü +7,38; kurtarma −60,61 puan | Son nefesle çıkış taşınanlara dahil. Gövde odalar boyunca taşınıyor, bot sürede çıkışı tercih edebiliyor. `facilityDragMoveCost` zaten 1; 0 aşama 6'da denendi (D22, `t6hepD0`, `t6snD0`: ölü 0,4–0,8 azaldı, gürültüye yakın), **denendi, etkisiz**. Aşama 6 okuması: ölümlerin kaynağı bedel değil kural yapısı (§13 "Ölüm"). `facilityReturnTurns` artışı (aşama 4'te 4 → 5 ölü/baskını 0,47 → 0,39 indirdi; 5'in üstü ölçülmedi) kurtarma ve süreye birlikte bakılarak denenebilir. |
| Sivil +0,64 | Birden çok odada sivil hasar oluşabiliyor. `liberationCivilianHp` artışı için tek tahtadaki etkisini de ölç. |
| Zafer −58,33 puan; kazananlarda uzunluk +12,5 vardiya | Ana etikette en sık son dağılma. Kadro kaybı, sivil kaybı ve güven birlikte izlenmeli. `liberationTrustPerCivilian` (tam sayı, 1) azalırsa dağılma yavaşlayabilir; tek azaltım 0'dır ve sivil kaybını güvenden tümüyle düşürür. Aşama 4'te yakın bir güven kuralı (`liberationCiviliansPerTrust` 1, `C1`: güveni hesapta artıya çevirdi) ölçülen dağılmayı yine %61–65'te, zaferi %10–12'de bıraktı. Zafer süresi için `liberationAlarmPerServer` artışının kuşatma sayısına etkisi ayrıca ölçülmeli. İki alan da tek tahtada okunur (`CampaignLiberation.cs`): bayrak kapalı oyunu da değiştirir. |
| Savunma +40 puan | Savunmaya girebilen kadrolar, girişte tutma ve röle. Kuşatma başına tutulan kanat 231/509 = %45,4 (aşama 11'de %46); bot hep savunur, bırakılan 270 kuşatmanın hepsinde savunacak kadro yoktu (`defNoSquad`). `facilityDefenseWaveBase` artırımı aşama 11'de denendi: dalga tabanı 0–6 (dalga aralığı, sayısı ve süreyle birlikte) tutmada başarıyı değiştirmedi (%100), **denendi, etkisiz**. `facilityDefenseRelayHp` azaltımı yalnız hep çekilmede etkili oldu (röle 1 → %93; dalga tabanı 4 + röle 1 → %80). Kural düzeyindeki seçenekler dizinde (14 · açık soru 5). |
| Tek araç beklenen aralığın 5,88 puan üstünde | Çeşitlilik bu botta beklenen avantajı vermedi; fark iki standart hatadan küçük. Önlem eşiğini (`facilityCounterThreshold`) düşürmenin tekrarlanan kullanım ve başarı üzerindeki etkisi ölçülebilir. |
| Vinç −6,78 ve kelepçe −4,49 puan | Tarifin açılması, üretim, taşıma ve eylem fırsatı farklı aşamalar. Aşama 8'de yalnız bot tarafı denendi (aday süzgeci, üretim eşitliği, ağırlıklar ×0,5–×2); denge alanı denenmedi. Önce vinç menzili (`facilityWinchRange` ↑), kelepçe üretim süresi (`facilityClampShifts` ↓) ayrı ayrı denenebilir. Köpek ölçüsüne bedelsiz havlama da girer. |

**Aşama 17 doğrulaması.** Son kopyanın derlemesinde C# hatası yok. Boş sahneden batchmode EditMode'da 2.256 geçti, 5 atlandı, hata çıkmadı. Tam PlayMode'da 117/133 geçti; kalan 16 test aşama 14'ün bilinen düşük çözünürlük sonuçlarıyla aynı. Canlı editörde 1920×1080 rehber testleri 7/7 geçti (altı `PlanningTutorialPlayTests` ve gerçek tesis kanadını açan yeni test; kanıt özet satırlarıdır). Mono'da 1.149 geçti; başlangıçta da görülen üç taklit katmanı hatası ve beş atlanan test vardı.

**Aşama 17 düzeltme turu (21 Kasım 2026).** Bağımsız denetimden sonra rehber metinleri, belgeler ve bot README'si düzeltildi; oyun Core'u, bot kodu ve ölçüm çıktıları değişmedi. Canlı editörde derleme temiz; `PlanningTutorialFlowTests` 19/19 (beş yeni test), `LocalizationTests` 5/5 ve 1920×1080'de `PlanningTutorialPlayTests` ile gerçek ilk tesis kanadı testi 7/7 geçti (taşma testleri dahil). Tam EditMode ve tam PlayMode bu turda yeniden koşulmadı.

**Tek kip denge ölçümü (22 Kasım 2026).** Tek kipten sonra oyunun varsayılanı tesis + saha açık oldu; bot zaferi %1,2'ydi (aşama 17
`t17saha`, 3/240). Hedef: "iyi" kümede bot zaferi kabaca eski tek tahta düzeyine (%63) yaklaşsın, alt sınır %45; yalnız `Balance`
değerleriyle, kural ve kayıt sürümü değişmeden. Liste `Tools/DengeBotu/denge22.txt` (tarama etiketleri o günkü eski beş değeri BAL'in
başında açıkça yazar; yeni exe ile bayt aynı yeniden üretildiği bir parçada denetlendi). Ortam aşama 17 ile aynı (`PICK=rastgele RETIRE=1 DEV=kir
DUSEN=hep ODUL=tarif ARAC=donus SAVUN=hep CEKIL=hic AYIR=hep KENAR=hic`).

*Teşhis (taban, 240 harekât).* Sonların %74'ü dağılma (güven 0), %14'ü son kişi, %10'u terk. Güven giderinin %96'sı baskında kırılan
sivil (harekât başına 45,5; kazanç 24,9 + bölge 17,5); dağılmaların %84'ünde son darbe bir baskının sivil kaybı. Baskın başına 2,32
sivil kırılıyor (H %60, A %29, G %15); bakım robotunun 2 hasarlı yakın saldırısı 2 canlı sivili tek turda kırıyor, Balance'taki "ikinci
can bir tur kurtarma fırsatı" niyeti işlemiyordu. Güven tek başına düzeltilince (aşağıda `d22v1`, `d22v2`) harekât uzuyor ve ikinci
darboğaz çıkıyor: ölüm vardiyada ~0,43, kabul ~0,30; nüfus eriyor, son kişi sonu %40'a çıkıyor. Baskınsız vardiyaların %93'ü ölümlü
baskının ardından (ölünün kiti silinir, kapanışta tek kit üretilir; kural, bu turda değişmedi).

*Tarama (karisik/ucuz, tohum 1–40; zafer %, n = 40; gürültü ±7–8 puan).* Taban (aynı alt küme) %0.

| Etiket | Değişiklik (eski dengeye göre) | Zafer | Dağılma | Son kişi | Not |
|---|---|---:|---:|---:|---|
| d22v1 | `liberationCiviliansPerTrust` 1 | 7,5 | 35 | 40 | güven tavanda boşa (harekât başına ~40) |
| d22v2 | `liberationCivilianHp` 3 | 5,0 | 32,5 | 40 | sivil/baskın 2,32 → 1,54 |
| d22v3 | `facilityDefenseRoomTrust` 0 | 0 | 72,5 | 20 | tabanla bayt aynı: bot hiç oda terk etmiyor |
| d22v4 | `liberationTrustPerRegion` 6 | 12,5 | 62,5 | 17,5 | |
| d22v5 | v1 + v2 | 15,0 | 12,5 | 52,5 | |
| d22v6 | `liberationTrustStart` 15 | 2,5 | 62,5 | 27,5 | |
| d22w7 | v4 + hedef 9, dönüş 6, acil 4, kademe-3 9, alarm 0,5 | 20,0 | 62,5 | 10 | |
| d22w9 | w7'nin güveni sivil canıyla (bölge 4, sivil 3) | 52,5 | 15 | 15 | asıl sıçrama: iki darboğaz birlikte |
| d22x1 | w9 + bölge 6 | 60,0 | 12,5 | 10 | |
| d22x4 | x1 + `facilityTargetMachines` 2 | 70,0 | 10 | 2,5 | hedef odanın kimliği zayıflar; seçilmedi |

*Ayıklama (w9'dan birer alan çıkarıldı; w9 %52,5).* Hedef 9 çıkınca %42,5 (`d22y1`); dönüş 6 çıkınca %55 (`d22y2`: gereksiz, alınmadı);
acil 4 çıkınca %32,5, son kişi %40 (`d22y3`); kademe-3 9 çıkınca %45 (`d22y4`); alarm 0,5 çıkınca %42,5 (`d22y5`). Uygulanan beş alan
`d22y2`'dir.

*Doğrulama (iyi3, 80 tohum × 3, `REPLAY=1`, 240/240 `replay=ok`, `.err` boş).*

| | t22taban (eski) | **t22 (uygulanan)** | Eşli fark (tohum kümeli SH) |
|---|---:|---:|---:|
| Zafer | %1,2 | **%43,3** | +42,1 ±3,6 |
| Dağılma | %74 | %19 | −54,6 ±4,7 |
| Terk | %10 | %16 | +5,4 ±3,5 |
| Son kişi | %14 | %10 | −4,2 ±3,7 |
| Sığınak düştü | %0 | %8 | +7,9 ±1,7 |
| Süren (vardiya tavanı) | 0 | 8/240 | |
| Zafer uzunluğu medyanı (çeyrekler) | 55 (n = 3) | 74 (60–88) | |
| Ölü / harekât; / baskın; / vardiya | 14,2; 0,72; 0,41 | 30,2; 0,77; 0,45 | harekât iki kat uzun |
| Kırılan sivil / baskın | 2,32 | 1,59 | |
| Baskın başarısı | %75,8 | %74,8 | |

Yapılandırmaya göre zafer: karışık/tecrit %61,2, karışık/ucuz %48,8, kefilli/ucuz %20 (kefilli kapı yalnız kademe 0 aday alır; kadro
terkle erir, terk %31). İki ek aday tam ölçekte etkisiz çıktı: `t22ara6` (`facilityMiddleTurns` 6) %42,5, `t22bolge5`
(`liberationTrustPerRegion` 5) %43,3 (t22'ye eşli fark +0,00 ±1,97).

**Uygulanan değerler** (`Balance.cs` ve `BalanceConfig.asset`): `liberationCivilianHp` 2 → 3, `facilityTargetTurns` 8 → 9,
`candidateEmergencyBelow` 3 → 4 (terk sayacı da bu eşiği okur), `liberationTier3At` 7 → 9, `liberationAlarmPerShift` 1 → 0,5. Kural,
komut ve RNG sırası değişmedi; kayıt kendi dengesini taşıdığı için kayıt 21 aynı kaldı, eski v21 kayıtlar kendi değerleriyle oynar.

**Açık kalanlar.** Zafer uzunluğu (medyan 74) aşama 8 hedefinin (35–45) çok üstünde; ölüm hızı vardiyada 0,45 ile kabulü geçiyor. Bunu
yalnız değerle kapatmak mümkün görünmedi: en büyük kaldıraç kurallarda (ölünün rol kitinin depoya dönmesi ya da eksik kitin emirsiz
üretilmesi, kaybedilen baskında gidişte yatan gövdenin kurtarılması; analiz harekât başına ~15 baskınsız vardiya ve ölümlerin %31'i).
Sığınak düştü sonu %8'e çıktı (uzun harekâtta sızmacı ifşası birikir). Bot insan değildir: iki karışık politika bandın içinde, insan
oyuncuda zafer daha yüksek çıkar; elle sınama geliştiricide.

**Kit kuralı denemesi (22 Kasım 2026, geri alındı).** Geliştiricinin isteğiyle kit kuralı iki geçici `Balance` anahtarıyla denendi
(karisik/ucuz, tohum 1–40, `REPLAY=1`, uygulanan dengenin üstüne; taban `d22y2` %55, baskınsız vardiya 23,8/harekât): (A) baskında
ölenin rol kiti, ekipten biri döndüyse depoya döner: %50, 20,9; (B) kapanışta en çok 3 eksik kit: %50, 25,1; (A + B): %50, 18,5. Fark
gürültü içinde (±7–8). Baskınsız vardiyaların tamamı `nrSquad`; tohum 3'ün dökümü (Camp8 `WHY=1`) sağlıklı kişinin azlığını gösterir:
üç kişilik ekipler topluca ölüyor, sığınak sürekli yeni gelenle dönüyor, kit eksikliği aradaki ikincil kalem. Kural ve anahtarlar geri
alındı (kayıt 21 korundu); geri alınmış kodun exe'si `taban22` bayt kilidini tuttu. Aynı turdaki analizin "baskınsız vardiyanın %93'ü
kit kuralından" okuması bu yüzden yanlıştı: boşluk ölüm sayısıyla birlikte artıyordu, nedeni kit değil kadroydu. Sıradaki kaldıraç
ölümün kendisidir (ölümlerin %31'i kaybedilen baskında yerde kalan, %25'i hedef odada kaçış süresi dolan, %25'i ayaktakiler varken
bırakılan gövde; §11.6 aşama 6 ve §13 "Ölüm").

## 12. Uygulama aşamaları

Her aşama derlenir, EditMode testleri boş sahnede geçer, oyunda denenir. Sürüm aşama 2'de 19 olur; aşamalar arasında
üretilen geliştirme kayıtları geçersizdir. Bayraklar aşama 4'e kadar asset'te kapalı kalır.

| # | Aşama | İçerik | Bitti sayılır | Büyüklük | Bağımlılık |
|---|---|---|---|---|---|
| 0 | Ölçüm tabanı | Kartlı kadro değerleriyle baskın düzeyi taban; harekât botuna tur ve hamle sayacı; oyun kodu değişmez | Taban tablo kayıtlı. **Bitti (2026-11-17):** `Camp8` baskın satırları ve sayaçları, `ozet.py --baskin`, `tesis0.txt`; 2 × 240 harekât; §11.6 "Taban ölçümü" | S | Yok |
| 1 | Core · odalar | `FacilityMaps`, 108 oda, plan ile robot ayrımı, donanım, geçit, menfez, kepenk; şart parçalarının oda üretimine taşınması; bayrak | Oda testleri geçer; her şart ara ve hedef oda türünde yer bulur; bayrak kapalıyken altın değerler aynı. **Bitti (2026-11-18):** `FacilityRoom`, `FacilityMaps`, `Baskin.Rooms` (`CreateRoom`), `Simulation.FacilityPlan`; `FacilityLockTests`, `FacilityMapsTests`, `FacilityRoomTests`, `EventSchedule_PerRoomBudget`; bayrak kapalı, sürüm 18. Nesne ekleyen bölge özellikleri (`sogutma`, `kalabalik`, `fabrika`) yalnız hedef odada, duvar özellikleri (`dar`, `acik`, `sebeke`) her odada; bölme kepengi §2.3 | XL | 0 |
| 2 | Core · üç odalı baskın | Oda zinciri, geçiş, taşıma, hazırlık etkileri, dönüş, `raid_room`, sürüm 19; bot zincir sürücüsü | Bot tam harekâtı oynar; kayıt testleri geçer. **Bitti (2026-11-18):** `RoomReport`, `RaidChain`, `Baskin.Chain` (kapı, GEÇ, dönüş tahtası), `AdvanceRaidRoom` (`raid_room`), `ResolveRaid` tesis dalı, sürüm 19 ve tesis parmak izi; `RaidBot.PlayFacility`, `Camp8` tesis kipi (`DEV`, `REPLAY`); `FacilityChainTests`, `FacilityRecordTests`, `FacilityBotTests`, `FacilityJsonTests`; bayrak kapalı. Düşen kişi için bugünkü kurtarma kuralı aşama 6'ya kadar sürer; giriş ve ara odada süre sonu geri çekilmedir | XL | 1 |
| 3 | Sunum · üç odalı baskın | Oda geçişi, antet, koordinat öneki, gözcü raporu, sonuç kartı, rehber; bölgeler YZ adını taşır (`RegionName`; ad havuzu aşama 13'e kadar üreticide kalır); sakinlerin İngilizce adları ve portre anahtarları | Karargâh tesisi baştan sona oynanır. **Bitti (2026-11-18):** `FacilityRaidSession` (oturum; simülasyona tek yazma yolu `GameController.AdvanceRaidRoom`) ve `FacilityRaidText` (gözcü raporu, rapor evresi, notlar, amaç ve geçit satırları), `RaidDataSource.CheckRoomEnd` (odanın bittiği yedi yer), `ResetBoard`, kendiliğinden geçiş, temiz odanın yürünmesi, sonuç kartında çıkıp devam; `RaidBoard3D` pafta çevirme, `CellName` öneki, kapı ve donanım etiketleri; GEÇ (EMRİ İLET düğmesi ve Enter), KAPIDA kartı, antet; rehberin tesis metni; `RegionDef.Ai`, `CandidateData.Founders`/`Names`, `PortraitLibrary` anahtarları; harita yerleşimine üçüncü tur (§15). `FacilityRaidHudTests`, `FacilityRaidTextTests`, `FacilityRaidSessionTests`, `FacilityRaidSceneTests`, `ResidentPortraitTests`, `FacilityRaidPlayTests`; bayrak asset'te kapalı, sürüm 19. Sahne ve prefab değişmedi | L | 2 |
| 4 | **Dur ya da devam** | Dikey dilim ölçümü: süre, başarı, ölü; elle oynama | Geliştirici kararı: devam, ya da giriş ve ara odanın tur bütçesi ve makine sayısı kısılır (üç oda sabittir). **Bitti (2026-11-18):** `RaidBot` tesis puanı düzeltildi (gerçek yürüyüş, geçit puanı, son tur); `tesis4.txt`, 12 varyant × 240 harekât, `ozet.py --tesis`; §11.6 "Aşama 4 ölçümü". Devam hedefleri tutmuyor; tur kısma başarıyı düşürüyor; makine kısma ve dönüş 5 tur yararlı ama dağılmayı durdurmuyor (güven kuralı). **Karar (2026-11-18):** giriş ve ara oda birer makine, dönüş 5 tur (`M11R5`, §14 yedinci tur); tur bütçesi kısılmadı. Elle oynama geliştiricide | S | 3 |
| 5 | Core ve sunum · bina planı | Bilinen oda durumu, teyp, evrak, dinleme nöbeti, üç pafta (yapı, nesneler, şart parçaları), Gözetim cebi | Plan baskın öncesi görünür; robot sızmaz. **Bitti (2026-11-18):** Core `FacilityState` (`Known`, `Intel`), teyp (`AdvanceRaidRoom`), evrak (`ResolveRaid`), dinleme (`AdvanceFacilityIntel`, `SpendFacilityIntel`), `FacilityPlanCells`, `FacilityIntelTarget`; görev fişinin yanında üç pafta (`FacilityPlanBuilder`, `FacilityPlanSheets`); kartlı panoda Gözetim cebi (`FacilityWatchText`); baskında telsiz masası: YAYINDA lambası ve makaralı teyp. `FacilityIntelTests`, `FacilityPlanSheetTests`, `FacilityPlanPlayTests`, `FacilityWatchPocketTests`, `FacilityWatchPlayTests`, `FacilityRadioDeskTests`; yeni komut yok, sürüm 19, bayrak asset'te kapalı. Masadaki kayıt rafı ertelendi (§14 sekizinci tur) | M | 4 |
| 6 | Core · hamleler | Pusu, koşu darbesi, düşen kişi ve sürükleme, ölüm kuralı, yük, iki kişilik iş, menfez; bot eylem kimliği | Hamle testleri ve geri alma geçer; ölü sayısı ölçülür. **Bitti (2026-11-19):** Core `Baskin.Actions.cs` (`CanAction`, `ActionTargets`, `DoAction`, `PreviewAction`; altı hamle, `MoveRules`), gövde modeli (yeni alan yok; `Body`/`Picked`, `RaidResult.Extracted`, ölüm kuralı, son nefes `Unit.Breath`), yük bırakma/alma/fırlatma, kepenk ve büyük sandık (duvar nişi, ödül), menfez; sekiz `Balance` alanı; bot eylem kimliği ve düşen politikası (`hep`/`hic`), `tesis6.txt`, 80 tohum ölçümü (§11.6 "Aşama 6 ölçümü"). `FacilityMoveTests`, `FacilityFallenTests`; her hamlenin geri alması tam döküm ve imzayla geçer; Mono koşucusunda 776 test geçer (3 bilinen taklit eksiği), Unity tam takımı: EditMode 1727/1732 (5 atlandı), PlayMode 93/107 (14 batchmode ortam kalanı, HEAD'de de düşüyor; §11.6). Yeni komut yok, sürüm 19, tesis izi f2, bayrak asset'te kapalı; bayrak kapalıyken Camp8 bayt bayt aynı. **Hedef tutmadı:** ölü 12,7 / harekât (hedef ≤ 7,5, taban 7,0), çıkarılan taban kurtarılanın %20'si (hedef ≥ %80); D22: sürükleme bedeli 0 da tutmadı, 1 kaldı. Ne yapılacağı geliştirici kararı (§14 dokuzuncu tur, açık bırakılanlar). Sunum aşama 7 | L | 4 |
| 7 | Sunum · hamleler | Yetenek kartı, tuşlar, işaretler, telsiz satırları | Bütün hamleler oyunda verilir. **Bitti (2026-11-19):** ölçüt `FacilityMovesPlayTests.Moves_AllSix_GivenInPlay` ile tutuyor (altı hamle gerçek E tuşu ve gerçek tahta tıkıyla verilir, her biri Z ile geri alınıp yeniden verilir). Tek tuş E ve kart satırları (Row 1'in çalışma anı kopyaları; prefab değişmedi); Bridge `FacilityMoveText` (seçenek, ad, ayrıntı, neden, etki, fiş, hover) ve `FacilityMoveView` (önizleme: çizgi, hayalet, etiket, odak); `RaidDataSource` `ArmAction`, `ChooseAction`, `UseAction`, tam `ActionRoutine`, makine evresinin pusu bölmesi, `OnEffect` `ambush-hit`/`cargo-drop`; `RaidBoard3D` pusu hattı, yerdeki yük, odak çerçevesi, önizleme çizgileri ve hayaletleri, on yeni Fx dalı, `AnimateWalk`, `PickCargo`; metinler (gözcü, kapanış, amaç, "Yük yerde kalır", notlar, kurallar kartı, geri çekilme); son nefes gövdesi ekiple gider; büyük sandık KAZANÇ'ta ve SONUÇLAR'da (`MaxResults` 7). Testler: EditMode `FacilityMoveTextTests` (10), `FacilityMovesPresentationTests` (16), `FacilityMovesSceneTests` (5), `FacilityBigCrateReportTests` (3); PlayMode `FacilityMovesPlayTests` (6). Unity tam takımı (batchmode kopya, aşama 8'in o anki dosyalarıyla): derleme temiz; EditMode 1814 test, 1809 geçti, 0 kaldı, 5 atlandı; PlayMode 113 test, 99 geçti, 14 kaldı: kalanların hepsi bilinen batchmode ortam kalanı (`ProductionPlayTests` 13, `PlanningTutorialPlayTests.Guide_…` 1), aşama 7'den önceki HEAD kopyasında da aynı biçimde düşüyor. Bağımsız inceleme ve kapanış düzeltmeleri (amaç satırı, "Yük yerde kalır", test iddiaları, `Animate` uzunluğu, son nefes gösterimi, `.meta` biçimi) yapıldı; iki yeni iddia ters sınamada eski hatayla kırıldı. Değişmeyenler: Core, sahne, prefab, BalanceConfig ve SoundBank; yeni komut yok, sürüm 19, bayrak asset'te kapalı, bayrak kapalıyken baskın sunumu ve testleri aynı. Kararlar §14 onuncu tur. **Ertelenen:** sonuç teleksi (aşama 10), sandık nişi ve kepenk modeli (aşama 9), sürüklemenin Core telsiz satırı. Elle oynanmadı (§15) | M | 6 |
| 8 | Core · ekipman ve ödül | Yedi ekipman ve yedi temel tarif, on iki kazanım, aygıt sınıfı, ele geçirme, köpek, tarif kilidi, tarif seçimi, `server_reward`; beş cevap tarifi aşama 10'da açılır | Ekipman testleri geçer; bot politikası var. **Bitti (2026-11-19):** Core `FacilityGear` (tablolar, ödül eşlemesi, tarif ve hak), `Baskin.Gear.cs` (sınıflar, ekipman eylemleri, aygıtlar, engel, makine aklı, kelepçe ve tutsak, köpek), `RoomReport`/`RaidChain` ekleri, tarif kilidi ve seçimi (iş emri yolu), erzak bedeli, eksik kit emir açıkken, `server_reward` ve bekleyen ödül kilidi, on iki kazanım (Siper etkisiz), Sedye yeni tanımı; 49 `Balance` alanı; sürüm 19, tesis izi f3. Testler: `FacilityRewardTests` (21), `FacilityRecipeTests` (21), `FacilityGearTests` (45), `FacilityDogTests` (12), `FacilityCaptiveTests` (12), `FacilityBotTests` +16; her yeni eylemin geri alması tam döküm ve imzayla, önizleme = uygulama; altın imzalar, Digest ve `CopyInto_CoversEveryField` değişmedi. Bot: ortak planlama `CampaignBot` (`ODUL`, `ARAC`), `RaidBot.Gear`, Camp8 R/C anahtarları, `ozet.py --tesis` "aşama 8" ve `--esli`, `tesis8.txt`, 7 etiket × 240 harekât (§11.6 "Aşama 8 ölçümü"). Mono koşucusu 903 geçti (3 bilinen taklit eksiği); Unity batchmode kopya EditMode 1899 test, 1894 geçti, 0 kaldı, 5 atlandı (derleme temiz, `LocalizationTests` dahil); PlayMode parça C'de 113/99 (14 bilinen ortam kalanı; parça D yalnız test ve araç dosyalarına dokundu). Bayrak asset'te kapalı; bayrak kapalıyken Camp8 bayt bayt aynı; `t8yok` = `t6sn`. Bağımsız inceleme kesin sorun bulmadı; bulguları §11.3 (Sniff denetimi), §11.6 (köpek ölçüsü, üretim yolunun adayı) ve §14 on birinci turun açık bıraktıklarında. **Hedef:** kullanılmayan ekipman beş türde tuttu (vinç %5, kelepçe %6 tutmadı); Sedye yeni tanımı kesinleşti (ölü / baskın −0,090 ±0,023). Kararlar §14 on birinci tur. Sunum aşama 9 | XL | 6 |
| 9 | Sunum · ekipman ve ödül | Dördüncü kart, aygıt ve engel modelleri, tarif defteri ve tarif seçimi, ödül seçimi | Bir harekâtta her ekipman üretilip kullanılır. **Bitti (2026-11-19):** ölçüt `FacilityGearCampaignPlayTests.EveryGear_ProducedEquippedUsed` ile tutuyor: tek harekâtta (tohum 79) yedi tarif ödül kâğıdından seçilir (ilki EVRAK tıkı ve 1 + Enter), yedi ekipman defterden iş emriyle üretilir (ilki gerçek sürükleme, ilk kapanış şalter ve devir fişiyle) ve depodaki ikinci sırada kart olur, panodan kişiye takılır (`board_equip`, ilki gerçek sürükleme), görev fişinin yolundan üç baskına çıkılır; tahtadaki ekipman ve hak panodan gelir, her uzman kendi ekipmanını R ve tahta tıkıyla kullanır, tutsak E ile komut verir, köpek 4 ile seçilip havlar ve düşer; sonda kelepçe panodan silinmiş, köpek dinleniyor (83 sn). Sunum: R tuşu ve tek ekipman satırı, köpeğin dördüncü kartı (4), uzman olmayan seçim, sınıf süzgeçleri, kart katlaması (Bridge `FacilityGearText`, `FacilityGearView`); on iki kurgu parça (yol E: var olan mesh'ler ve Rev3 malzemeleri), tahta işaretleri (aygıt hattı, yem yolu, havlama erimi, etiketler, NİYETSİZ ve KİLİTLİ), on altı Fx dalı; depo ikinci sırası, piktogramlar, defter 13 sayfa (kapalı sayfalar, 3 girdi, erzak), `NoSupplies` ve eksik kit tahmini, dinlenen köpek, köpek hurda reddi; ödül kâğıdı (`SiginakServerReward`) ve bekleyen ödül kilidinin yüzeyleri, KAZANIM satırı. Testler: EditMode `FacilityGearTextTests` (7), `FacilityGearPresentationTests` (14), `FacilityGearViewTests` (5), `FacilityGearSceneTests` (6), `GearPieceModelTests` (3), `FacilityGearBoardTests` (10), `FacilityServerRewardTests` (7); PlayMode `FacilityGearPlayTests` (5), `FacilityServerRewardPlayTests` (2), `FacilityGearBoardPlayTests` (1), `FacilityGearCampaignPlayTests` (1). Unity tam takımı editörde (boş sahne): derleme temiz; EditMode 1951 test, 1946 geçti, 0 kaldı, 5 koşulmadı (`Explicit`); PlayMode 122/122 (765 sn; batchmode'un bilinen 14 ortam kalanı editörde geçer). `LocalizationTool.Validate` boş. Ekran görüntüleri 1920×1080 TR ve EN (baskın HUD'u dört kartla, aygıtlar ve işaretler, hover, ekipman önizlemesi, depo, defter, ödül kâğıdı) ve bir kez 16:10. Bağımsız inceleme: kural ihlali yok; kapanışta düzeltilenler §14 on ikinci tur 73–80. Değişmeyenler: Core (sürüm 19, iz f3), `BalanceConfig.asset`, `SoundBank.asset`; yeni komut yok; bayrak asset'te kapalı, bayrak kapalıyken sunum aynı (üç ekip kartı en çok 0,67 px; 44). Kararlar §14 on ikinci tur. Elle oynanmadı (§15) | L | 8 |
| 10 | Core ve sunum · önlem defteri | Sayım, öğrenme, altı önlem, cevap tarifleri, ÖNLEM satırı, ÖNLEM teleksi, sonuç teleksi (aşama 7'den taşındı, §7.3) | Tek araç ve dönüşümlü politika ölçülür. **Bitti (2026-11-19):** Core `FacilityCounters` (tablo, saf öğrenme, `RaidTally`) ve `Baskin.Counters` (sayım, altı önlem iki kademe, beş cevabın etkisi, kaplama), `FacilityState.Tally/Counter/Learned`, emir kopyası, cevap sayfaları ve taslak iptali, plan kaplaması, olay nişanı sınıf süzgeci; 17 `Balance` alanı, alt bayrak `facilityCountermeasures`, ölçüm anahtarı `facilityTallyAmbush`; kayıt 19, iz f4. Sunum: ÖNLEM satırı (beş yuvada, gerekirse KAZANIM ile birleşik), ÖNLEM ve sonuç teleksi, telsiz günlüğü, paftada kırmızı kalem, CIVATALI etiketi, hover, neden ve gözcü satırları, yayıcı parçası, defterde cevap sayfası ve ödül kâğıdı; PlayMode `FacilityCounterPlayTests.Countermeasure_ShownEverywhere` (gerçek koşu darbesiyle öğrenme, altı yüzey, kayıttan devam). Bot (`ODUL=onlem`, `TAK=hic`, yayıcı ve makine aklı terimleri), `ozet.py --fark`, `tesis10.txt`; 10 etiket × 240 harekât: tek araç 0,71, dönüşümlü 1,19 önlem / harekât, cezalı taşıma %61 / %39, baskın başarısı farkı +0,1 ±0,6p (hedef 5–10p tutmadı; denge değişmedi, S4); §11.6 "Aşama 10 ölçümü"; §14 on üçüncü tur | L | 8 |
| 11 | Core ve sunum · kalıcı tahta ve savunma | İzler, onarım, `siege_defend`, `siege_yield`, üç odalı savunma, yeniden alınan kanat kuralı | Savunma baskını oynanır; ölçüm hedefte. **Bitti (2026-11-20), ölçüm yarısı tutmadı:** Core `FacilityMarks` (sekiz iz türü, saf uygulama, türetme ve birleştirme, bağ kuralı), YZ onarımı, `Baskin.Defense` (tek sayaç, barikat turu, üç dalga ve gecikme, takipçiler, saha rölesi, kapı sığınak değil, makine aklının röle terimleri), `Simulation.Siege` (kuşatma günlüğü, karar vardiyası, kendiliğinden bırakma, savunmanın sonucu), §9.3, Siper; iki alt bayrak (`facilityMarks`, `facilityDefenseEnabled`) ve 11 `Balance` alanı; kayıt 19, iz f5. Sunum: kuşatma duyurusu, harita işareti, görev fişinin kuşatma kipi (SAVUN, iki tıklı BIRAK), paftada izler ve onarım, CRT, telsiz, devir fişi, ÖN KONTROL, pano kilidi, `RaidLog` tüketicileri; baskın sahnesinde SAVUNMA anteti, 10 hücreli şerit, barikat ve dalga, İÇERİ ÇEKİL onayı, saha rölesi parçası, "KANAT TUTULDU". **Oynanır:** PlayMode `FacilityDefensePlayTests.Defense_PlayedFromSlipToReport` yeşil. **Ölçüm:** bot (`SAVUN`, `CEKIL`, `S` satırı, `tesis11.txt`; 7 etiket × 240 harekât) hep tutmada savunma başarısı %100 (186 savunma), hep çekilmede %98; hedef %40–60 tutmadı, tasarımın ayar alanları bandı tutturamadı, denge ve kural değişmedi (kuşatma başına tutulan kanat %46); §11.6 "Aşama 11 ölçümü"; §14 on dördüncü tur | XL | 8 |
| 12 | Core ve sunum · Katar ve Yalpa | İki şart, iki tesisin adı ve kiti | Şart testleri geçer; iki tesis oynanır. **Bitti (2026-11-20):** Core `Baskin.Conditions.cs` (açık kenar ve `FallOff`, takip `FollowerIds`/`FollowedInIds`, yatış `EndTilt`/`Slides` ve takvim), AYIR (`CanDecouple`/`Decouple`/`DecoupleBlock`), ortak `EntryTile`, `Push`'un kayma kipi, kişi durumu `"k"`, `RaidResult.Overboard`, kenardan düşenin dönüşü (yaşar, 5 vardiya düz yatış), `KatarReportCheck`, okuyucular (`RegionEvent`, `RegionNote`, `FacilityRoomTurns`), Katar'da doğu geçidi; iki alt bayrak (`facilityKatar`, `facilityYalpa`) ve 4 `Balance` alanı; adlar (Sevkiyat katarı, Yüzer maden platformu; bayraktan bağımsız) ve şartın notu; kayıt 19, iz f6. Sunum: fişin OLAY'ı ve bina notu, Katar paftasında kesik kenar, Yalpa paftasında takvim, rapor satırı; baskın sahnesinde kenar çizgisi ve hover, düşme önizlemesi ve canlandırması, VAGONDAN DÜŞTÜ, AYIR (Q, Teknisyen kartı, EMRİ İLET), TAKİP EDER, Yalpa şeridi, okları, kayma hatları ve eğilme canlandırması. Kit ve model değişmedi (S4). **Oynanır:** PlayMode `FacilityConditionPlayTests` (Katar ve Yalpa kanadı fişten rapora, gerçek tuşlarla) yeşil. **Ölçüm** (hedef yok): bot (`AYIR`, `KENAR`, `tesis12.txt`; 5 etiket × 240 harekât) iki tesisi istisnasız oynar (1 070 Katar, 820 Yalpa baskını); kaçış kapısı ve AYIR'ın bedelsizliği okuma ölçütünü geçmedi, denge değişmedi; §11.6 "Aşama 12 ölçümü"; §14 on beşinci tur | L | 10 |
| 13 | Core ve sunum · altı imza makinesi | Tampon, çapa, alev, mühürleyici, kırağı, montajcı: kural, makine aklı, model, bot; harita g5, tesis f7 ve kayıt 20 | **Bitti (2026-11-20):** 12 imza hedef odada sınandı. 80 eşli tohumda baskın başarısı %78,4 → %76,8. Ölüm, süre ve zafer hedefleri açık. Yürürlükteki ana bayrak asset'te kapalı; §6.5, §11.6 ve §14 on altıncı tur | XL | 8 |
| 14 | Core ve sunum · unvanlar | İş sayaçları, altı kişisel unvan, kadro raporu, kişi dosyası ve tahta etkileri | **Bitti (2026-11-20):** Nişancı raporda kazanılır ve sonraki baskında altıncı kareye isabet eder. 32 eşli tohum, f8, kayıt biçimi 20. Genel denge hedefleri açık. §6.4, §11.6 ve §14 | M | 8 |
| 15 | Core ve sunum · sahadaki sızmacı | Bölüm 8, ayrı saha bayrağı | **Bitti (2026-11-21).** Açık tahta, önizleme, geri alma ve botun ileri bakışı gizlilik testinden geçti. | M | 8 |
| 16 | Temizlik | Harita torbası, ek turlar, eski ad katmanı ve köpek hurda kuralı | **Bitti (2026-11-21).** Kullanılmayan ad havuzu işlemleri kaldırıldı, erişilebilir yollar korundu. Bayt karşılaştırmaları ve eski hurda kaydı geçti. | S | 13 |
| 17 | Rehber ve denge | Rehber metinleri, 80 tohumluk ölçüm, `OYUN-TASARIMI.md` ve `MIMARI.md` | Ölçüm tablosu dolu. **Bitti (2026-11-21):** §11.6 hedef tablosunun her satırı 80 tohumla ölçüldü. 2.400 güncel ve 480 tarihsel karşılaştırma harekâtı, komut tekrarları, TR/EN rehber ve §14 karar dizini. | M | 16 |
| 18 | Tek kip | Tesis oyunun tek baskın kipi: `raidFacilityEnabled`, tek tahta yolu, harekât harita havuzu ve torbası, `MapSlot`/`ExtraTurns`, görev ek turları ve senaryolu eğitim baskını (`TutorialRaid`) silinir; `ResolveRaid` sonucu hep oda raporlarından türetir; saha sızmacısı varsayılan açık; kayıt 21 | **Bitti (2026-11-22):** Core, testler, bot, sunum ve asset tek kipe geçti; `Baskin.Create` havuzsuz örnek/test tahtası olarak kaldı (`RaidEngineTests` altın imzaları aynı). Bayt kilidi `taban21/kilit21.sh` 124/124 `cmp` eşit (tesis yolu değişmedi). Mono koşucusu 1.087 geçti (bilinen 3 taklit testi); tam EditMode (batchmode, tek koşu) 2.195/2.197 geçti, 0 kırmızı (2 atlanan ölçüm); tam PlayMode 128/128. Tesis tarif adı kart yüzüne kısa kart adıyla sığar (`PlanningBoardData.CardName`). Oyunda ilk sektör baskını sahnede raporuna kadar oynandı; v20 kayıt reddedilip `.v20.backup` olarak arşivlendi. §14 yirmi birinci tur | L | 17 |

Geliştiriciye önerilen ve itiraz görmeyen yapım sırası korunmuştur (sıra geliştiricinin kendi sözü değildir;
değiştirebilir): üç odalı baskın, bina planı, hamleler ve ekipman, önlem defteri, kalıcı tahta ve savunma, tesis
şartları. Tek fark: tarif kilidi ve sunucu ödülü ekipmanla aynı aşamadadır, çünkü ekipman oyuna
tarifle girer.

**§12 kapanışı.** Aşama 0–17 tamamlandı. Aşama 18'de (22 Kasım 2026, geliştirici kararı) tesis kipi oyunun tek baskın kipi oldu;
eski tek tahta yolu silindi, kayıt 21 (§14 yirmi birinci tur).

## 13. Riskler

- **Süre.** Üç tahta bugünkü baskının iki katından uzun sürer; zafer medyanı zaten 48 vardiyadır ve her vardiyada bir
  baskın vardır. Üç oda sabittir (karar). Karşı ağırlık: giriş ve ara odada az makine, temiz odanın oynanmaması. Ölçüm
  tutmazsa giriş ve ara odanın tur bütçesi ve makine sayısı kısılır (aşama 4). Aşama 4'te ölçüldü: giriş ve ara oda birer
  makineye indi, tur bütçesi kısılmadı (başarıyı düşürüyordu); toplam tur yine 18–19 (§11.6).
- **Ölüm.** Taşınmayan düşen ölür (karar). Taban harekât başına 6,7 ölüdür ve bugün harekât başına 9,4 düşen
  kendiliğinden kurtarılır (aşama 0); hiçbiri taşınmazsa ölü 16'ya çıkar. Yeni kural ölümü belirgin artırabilir ve
  dağılma oranını yükseltebilir. Karşı ağırlık: temiz odada
  kendiliğinden toplama, "Sedye" ve "Revir" kazanımları, vinç. Ölçüm tutmazsa sürükleme cezası (menzil −1, yetenek
  yok) hafifletilir. Aşama 6'da ölçüldü: risk gerçekleşti. Ölü 12,7 / harekât, zafer %6,7 → %2,1; sürükleme bedelini
  kaldırmak (D22) ve son nefes kuralı ölüyü gürültü içinde değiştiriyor. Ölümlerin kaynağı bedel değil kural yapısıdır
  (kaybedilen baskında yatan, H ve dönüş odasında bırakılan, süre sonunda geride kalan); "Sedye" bugünkü tanımıyla (menzil
  azalmaz) en iyi hâlde −0,45 ölü verir, "Revir" ölümü değiştirmez. Öneriler §14 dokuzuncu turun açık bıraktıklarında.
- **Sivil güven.** Üç oda daha çok sivil sunucu demektir; dağılmaların son darbesi çoğunlukla sivil kaybıdır. Karşı
  ağırlık: giriş 1, ara 2, hedef 3 sivil sunucu (toplam 6; bugün 4–5, hedef odasında bugünkünden 1–2 eksik).
- **Şart parçalarının taşınması.** On iki olayın kurulumu bugün baskın anındaki duruma (makine yerleri, kademe, tek
  hedef) göre yazılmıştır; oda üretimine taşımak on iki olayın kodunu ve testlerini değiştirir. Karşı ağırlık: aşama
  1'de olay olay taşınır, bayrak kapalıyken eski kurulum çalışır.
- **Motorun tek tahta varsayımı.** `BState` bir baskındır; kaçış, sonuç dizinleri, olay takvimleri ve bot buna
  dayanır. Karşı ağırlık: oda başına ayrı `BState`, üstünde ince bir zincir katmanı; bayrak kapalıyken eski yol (aşama 18'de
  silindi; `Room == null` bekçileri örnek tahta için kaldı).
- **Ekip tarafında uzman olmayan birimler.** Köpek, aygıt ve ele geçirilmiş robot sonuç listelerini ve kaçış
  denetimini bozabilir. Karşı ağırlık: `Unit.Class` ve dizin testleri.
- **Makineler yol bulmaz.** Örülen engelin arkasında kalan makine sonsuza dek bekleyebilir; oyuncu teli sömürebilir.
  Karşı ağırlık: engel kırma niyeti ve `orme` önlemi.
- **Önlem defteri cezaya dönebilir.** Oyuncu sevdiği aracı bırakmak zorunda kalırsa oyun daralır. Karşı ağırlık:
  önlem aracın bir bölümünü kapatır, cevabı vardır, teleksle duyurulur.
- **Unvan kasma.** Tekrar baskınla unvan toplanabilir. Karşı ağırlık: yalnız kazanılan baskın, baskın başına bir kez.
- **Arayüz yükü.** Yetenek kartı, dördüncü birim ve altı yeni hamle baskın ekranını kalabalıklaştırır. Karşı ağırlık:
  hamleler ve ekipman ayrı aşamalarda gelir; her aşamada rehber güncellenir.
- **Bot maliyeti.** 80 tohumluk liste bugün yaklaşık 1,5 saattir; üç oda bunu katlar.
- **Altı yeni makine.** Kapsamı en çok büyüten karardır: her makine kural, makine aklı, model, ses, bot ve denge
  ister. Tampon ve çapa makinelere ilk kez itme ve çekme verir; Katar'ın açık kenarıyla birlikte ölçülmelidir.
  Montajcının hurda kaynağı (düşen makine) ayrıca ölçülür; dar koridorda kalan hurdanın dönüş yolunu kapatmadığı
  sınanır. Alev ve mühürleyici erken paketlerdedir: ilk bölgelerin baskın başarısı yeniden ölçülür.
- **Üç odalı savunma.** Savunma 10 tura çıkar; karşı saldırı harekât başına yaklaşık 1,4 kez gelir. Karşı ağırlık:
  "bırak" her zaman vardır (seçilir olması ölçülür). Terk edilen oda güven ve iz kaybettirdiği için içeri çekilmek
  bedelsiz değildir; yine de bot "hep çekil" ile "hiç çekilme"yi karşılaştırır. İki oda terk edilerek kazanılan
  savunma (güven −2) "bırak"tan (güven −1) daha pahalıdır; kanadın elde kalmasının buna değip değmediği ölçülür. İki
  oda terk edilip kaybedilen savunma güveni 3 düşürür (başlangıç güveni 10); dağılma oranına etkisi ölçülür.
- **Dönüş bütçesi.** Kartlı kadronun iki beden profili yürüyüşü 2'ye indirir; sürükleme, yolu tutan makineler ve
  hareketi kısan imza makineleri (kilitleyici, kırağı, çapa, mühürleyici) dönüş odasında ayaktaki kişiyi geride
  bırakabilir. Karşı ağırlık: "Sedye", "Açık hat", "Sırtlayan". Aşama 4 ölçümüne "dönüşte geride kalan ayakta kişi"
  sayacı eklenir; tutmazsa `facilityReturnTurns` ya da 8 adım sınırı değişir. Aşama 4'te sayaç tetiklendi (geride kalanlı
  baskın %20); `facilityReturnTurns` 4 → 5 oldu (%15). Ara odanın bölme kepenginde kapalı yol 10
  adıma kadar uzar (§2.3); sayaç kepengin açık ya da kapalı olduğunu da yazar.
- **Katar.** Açık kenar ölüm kuralına karşı kaçış kapısı olarak kullanılabilir: yetişemeyecek kişiyi EMP itmesiyle
  kenardan düşürmek ölümü 5 vardiya yatışa çevirir. Ayırmanın bedeli de yoktur: Teknisyen ayaktaysa AYIR hep seçilir
  ve takip kuralı işlemez. İkisi de ölçülür; gerekirse ayırmaya bedel konur.
- **Boş cevap tarifi.** Temel tarifi seçmemiş oyuncu için geç YZ'nin tarif seçeneği işe yaramaz; o bölgede seçim
  fiilen kazanıma iner.
- **Sahada kimlik.** Belirti iki yönde de kesinlik vermez (insan da verir; sessiz sızmacı da sessiz insan da vermez).
  Dönüş yalnız sunucu kanadının baskınında olduğu için ikmal ve savunma baskınları şüpheliyi dönüş riski olmadan
  sınama yeridir; belirti verenleri sunucu kanadına götürmeyen oyuncu için dönüş tehlikesi sessiz sızmacıya iner.
  Sessiz payı bu yüzden ölçülür. Kesinlik yalnız
  sunucu kanadındaki dönüşten gelir ve bedeli şüpheliyi harekâtın en kritik odasına götürmektir. Baskından çıkıp devam
  etmek odayı başından açar: görülen belirti ve dönüş oyuncunun bilgisinde kalır, oda yeniden oynanır.
- **Tek kip (aşama 18).** Asset tesis alanlarını taşımadığı için oyun aşama 18'e dek C# varsayılanıyla tek tahtada oynanıyordu;
  tek kip oyunun kipini değiştirdi. Kartlı rehber, sığınak olayları ve PlayMode akışları ilk kez tesiste koştu. Saha sızmacısının
  açılması sızma olaylarını ve aday akışını değiştirir (bilinçli). Tesiste "düşen, ekipten biri sağ çıktıysa kurtarılır" kuralı yok;
  bu kurala dayanan olay kurguları (`agir_yarali`, `anma`) yeni kurala göre yeniden yazıldı. `MapSeed` sunumun bina kitini seçer:
  alan silinir ya da 0 yazılırsa tesis tahtaları derleme hatası vermeden varsayılan kite düşer (bekçi
  `FacilityRaidSceneTests.FacilityRoom_UsesRegionKit`). Kartsız kipte sunucu ödülü yüzeyi yok (kip oyunda ulaşılamıyor).

**Bozulan önceki kararlar.**

| Karar (tarih) | Bugünkü hâli | Bu belgeyle |
|---|---|---|
| Sektör binanın bir odasıdır (2026-11-02) | `RegionDef.Sector(i)` oda adı | Sektör kanattır; oda ayrı ad alır |
| Harita türü ve oda adı gösterilmez (2026-11-02) | Tahta emirden önce görünmez | Bilinen odanın planı baskından önce görünür |
| Harita havuzu 30, baskın sırasına göre (2026-10-28) | `RaidMaps.Slot` | Kanada bağlı 108 oda |
| Karşı saldırı tahtasızdır (2026-10-27) | `Counterattack` anlık | Savunma baskını ya da bırakma |
| "Yeni savunma tahtası yok" (`MEKANIK-INCELEME` §5) | Öneriydi | Savunma kanadın üç odasında, baskında oynanan aynı tahtalarda oynanır (bölüm 9.2) |
| Taktik taşıma eylemi yok (2026-11-05) | Düşen kendiliğinden kurtarılır | Sürükleme ve geride kalma kuralı |
| Tarifler baştan açık | Altı tarif | Altısı açık; yenileri sunucudan |
| Baskın içi sürprizler ertelendi (`TOPLULUK` §6) | Sızmacı sahada etkisiz | Bölüm 8, ayrı bayrak |
| "Geçen tren gibi odaya özgü kural alınmaz" (`BASKIN-MEKANLARI`) | Alınmadı | Katar ve Yalpa tesis şartı olarak gelir |
| Kesinlik yalnız sabotaj, tecrit bulgusu ve son rapordan gelir (`KABUL` §1) | Sahada kimlik izi yok | Bölüm 8: belirti iki yönde de kesinlik vermez; sunucu kanadının hedef odasında dönmeyen kişi insandır |
| Bölgeler üretilmiş yer adı taşır (2026-11-16) | Kıyı ve iç bölge ad havuzu | Bölge YZ'nin İngilizce marka adını taşır |
| Sakin adları Türkçedir | Kurucu ve aday ad listeleri | İngilizce özel isimler |
| Olay parçaları baskın kurulumunda konur | `EventSetup` | Oda üretiminde konur ve planda çizilir |
| Her imza makinesi iki bölgededir (2026-10-27) | Altı makine | On iki YZ'ye on iki makine |
| Düşen, ekipten biri sağ çıktıysa kurtarılır (2026-11-05) | Otomatik kurtarma | Taşınmayan düşen ölür |
| Yeniden alınan bölge fonu ve güveni yeniden verir | Açık not | Bir kez verilir (bölüm 9.3) |
| Otomatik üretim tarifi girdiden tanır | Yayında | Tarif defterden seçilir |
| Karşı doktrin geçicidir, davranış değişince söner (`MEKANIK-INCELEME` §7) | Öneriydi | Önlem kalıcıdır ve birikir; cevabı tarifle gelir |

## 14. Kararlar ve açık sorular (geliştirici kararı)

**Kararlar (17 Kasım 2026, ilk tur: tasarımın kendisi).**

| Konu | Karar |
|---|---|
| Baskının yapısı | Üç aşama; her oda ayrı 8×8 tahta; üçüncü odada ikmal ya da sunucu |
| Odalar | Bölgedeki her baskın farklı üç odadan geçer |
| İlk odalar | Sunucu odası için hazırlıktır |
| İstihbarat | Kâğıtta yalnız bina planı (duvar ve benzeri) |
| Robotlar ve niyetler | Baskına girdikten sonra bugünkü gibi görünür |
| Sessizlik | Yok |
| Bölgeler | Her bölge farklı bir YZ |
| Sunucu ödülü | Yeni tarif ya da kazanım |
| YZ'nin gelişimi | Oynama tarzına önlem alır ("artık şu ekipmanla itilemiyor") |
| Ekipman | Örme, yıkma, tahtaya kurulan mekanik makineler, robotu birkaç turluğuna ele geçirme, kendi hamlesi olan köpek |
| Hamleler | Pusu (ilk adı nöbet), koşu darbesi, düşeni sürükleme, yükü fırlatma, iki kişilik işler, havalandırma |
| Karakterler | Geliştirilebilir |
| Tesisler | Hareket eden tesis (tren, ayrılan vagon) ve yatan zemin |
| Kalıcı tahta | Yıkılan duvar, bırakılan hurda, patlatılan soğutma ve örülen tel sonraki gelişte yerinde durur; YZ onarır |
| Savunma baskını | Karşı saldırı tahtada oynanır: daha önce basılan yer savunulur; önce barikat, sonra dalgalar |
| Kadrodaki kopya (belgede "sızmacı") | Şüpheli alınan kişi baskında taraf değiştirebilir; öncesinde küçük işaretler verir |
| Telsiz | Rapor evresi, koordinat dili, ses, yayın lambası, makaralı teyp |

**Kararlar (17 Kasım 2026, ikinci tur: açık sorular).**

| # | Soru | Karar |
|---|---|---|
| 1 | Robotların görünmesi | Oda oda: her odanın robotları ekip o odaya girince görünür |
| 2 | Başarısız baskının tekrarı | Aynı kanadın aynı üç odası |
| 3 | Taşınmayan düşen | Ölür |
| 3a | Geri çekilmede yerde yatanlar | Geride kalır, ölür |
| 4 | Süre tutmazsa | Üç oda sabit; tur bütçesi ve makine sayısı kısılır |
| 5 | Katar ve Yalpa | Bölgeler artık YZ'nin adını taşır; Katar ve Yalpa iki YZ'ye bağlanır |
| 5a | YZ adları | On iki sabit ad (adların kendisi üçüncü turda değişti: İngilizce marka adları) |
| 6 | Tek YZ kurgusu | On iki bağımsız YZ; üstte tek YZ yok |
| 7 | Sızmacının belirtileri | İnsan da verebilir: belirti şüphe üretir, kesinlik vermez |
| 8 | İmza makineleri | On iki ayrı makine |
| 9 | Savunmanın bedeli | Fon yok; vardiyanın baskın hakkı gider |
| 10 | Seçilmeyen sunucu ödülü | O harekâtta alınamaz |
| 11 | İkmal kanatlarının sunucu kanadına etkisi | Yok; hazırlık yalnız baskın içinde |
| 12 | İş sırası | Önce tesis baskınları (sürüm 19); simülasyon dönüşümü taslağı sonra (üçüncü tur, karar 10) |
| 13 | "Nöbet" adı | Hamlenin adı "Pusu"; "Kısa nöbet" özelliği kalır |
| 14 | `AGENTS.md` belge tablosu | Satır eklendi |
| 15 | Savunmanın odası | Üç oda |
| 16 | Planın kapsamı | Yapı ve sabit nesneler; robot yok (şart parçaları üçüncü turda eklendi, karar 8) |
| 17a | Yalpa'da tutunma | Yok; sabit olmayan her şey kayar |
| 17b | Katar'ın açık kenarı | Ekip üyesi düşer, ağır yaralı döner; ölmez |
| 18 | Tarif seçimi | Oyuncu tarifi defterden seçer; köpeğin bedeli erzak |
| 19 | Yeniden alınan kanat | Bölge fonu ve bölge güveni ikinci kez verilmez; unvan işi sayılmaz |

**Kararlar (17 Kasım 2026, üçüncü tur: ayrıntılar).**

| # | Soru | Karar |
|---|---|---|
| 1 | Adlar | Sığınak karakter adları ve YZ adları İngilizce özel isimlerdir; YZ adları bir yapay zekâya verilebilecek marka adlarıdır |
| 1a | YZ ad listesi | VERITY, CAIRN, HEADWATER, TESSEL, EMBER, SOLACE, EDDY, WAYPOINT, PARAGON, QUORUM, BOREALIS, HALCYON (bölüm 6.1) |
| 2 | Katar ve Yalpa'nın paketi | Katar orta aşamada (`na`, WAYPOINT), Yalpa geç aşamada (`oc`, HALCYON) |
| 3 | Yeni makinelerin dağılımı | Karışık: erken paketlerin bir bölümü de yeni makine alır |
| 4 | Montajcının hurdası | Düşen makine de hurda bırakır; montajcının kurduğu dron bırakmaz |
| 5 | Savunmada içeri çekilme | Terk edilen oda kaybedilir: izleri silinir ve oda başına güven düşer; girişi tutmak en iyi sonuçtur |
| 6 | Sızmacının dönüşü | Yalnız sunucu kanadında |
| 7 | Sessiz sızmacı | Var: belirti iki yönde de kesinlik vermez |
| 8 | Şart parçaları | Bina planında çizilir; şart kurulumu oda üretimine taşınır |
| 9 | Savunmada deneyim | Yalnız kazanılan savunmada |
| 10 | Simülasyon dönüşümü taslağı | Şimdilik dokunulmaz; o işe sıra gelince birlikte güncellenir |

**Açık sorular.** Karar bekleyen soru kalmadı. Uygulama sırasında onaya sunulacaklar: altı yeni makinenin sayıları ve makine ile YZ eşlemesi (aşama 13) ve "(b)" işaretli bütün başlangıç
değerleri (ölçümle).

**Kararlar (18 Kasım 2026, dördüncü tur: aşama 1 uygulaması).**

| # | Soru | Karar |
|---|---|---|
| 1 | Bölme kepengi | Kalır: ara odada kepenk kapalıyken geçide yol en çok 10 adım, açıkken 8 (§2.3 "Bölme kepengi"). Var olan duvara konan kazançlı kepenk taslakların yalnız %2,4'ünde yer buluyordu; kepenk kuralı gevşetilmedi. Dönüşte geride kalan ayakta kişi aşama 4'te ölçülür (§13 "Dönüş bütçesi") |

**Kararlar (18 Kasım 2026, beşinci tur: aşama 2 uygulaması).**

| # | Soru | Karar |
|---|---|---|
| 1 | Dönüşte geçiş | Kaçışta batı geçidi de kapıda bekleme ve GEÇ ile geçilir. Tahtadaki son uzman düşerse ya da kaçışta süre dolarsa kapıdakiler kendiliğinden geçer (§3.3) |
| 2 | Dönüş odasında tesis şartı | Plandan baştan başlar (§3.5) |
| 3 | Kademe eki olmayan hedefte çağrı anteni | Bir başlangıç dronu eksilir; bakım robotu kalır (§11.1 `facilityAntennaMachines`; geliştiriciye varsayılan olarak bildirildi) |
| 4 | Makineler donanımı hedef alır mı | Almaz: makine aklında donanım karesi −10 puan |
| 5 | Kapıdan geri çıkış | Yok; kapıdaki uzmanı yalnız tur içi geri alma geri getirir (geliştiriciye varsayılan olarak bildirildi) |
| 6 | Ekipman hakları odalar arasında | Taşınır (bugün dinamit ve duman hakları) |

**Kararlar (18 Kasım 2026, altıncı tur: aşama 3 uygulaması).** İlk dördünü geliştirici seçti; öbürleri önerilen varsayılandır
(geliştiriciye bildirildi).

| # | Soru | Karar |
|---|---|---|
| 1 | Sakin adları | İngilizce özel isimler: 10 kurucu (Agnes, Walter, Quinn, Nell, Robin, Victor, Margot, Iris…) ve 64 aday, ayrık listeler (`CandidateData.Founders`, `Names`) |
| 2 | YZ adları | Her oyunda; bayrağa bağlı değil |
| 3 | GEÇ'in arayüzü | Herkes kapıdayken EMRİ İLET düğmesi ve Enter "GEÇ" olur, altında "GERİ ALINMAZ"; onay sorulmaz |
| 4 | Oda geçişi | Pafta kayması (sürüm 10; önce batı kenarı üstünde 80° çevirme): biten pafta kullanılan geçidin tersine kayıp çıkar, yeni pafta geçidin yönünden gelir (0,45 + 0,45 s; `RaidBoard3D.slideSeconds`, `slideDistance`, `GateDirection`) |
| 5 | Kapıya giren uzman | Tahtadan kalkar ("KAPIDA"); sayı geçit etiketinde ("KAPIDA 2/3"), kart soluk ve "✓ KAPIDA" |
| 6 | Temiz oda | Telsiz satırı ve olay fişi; antetteki pafta numarası atlar |
| 7 | Giriş ve ara odada süre sonu | Başlık "GERİ ÇEKİLDİ" (Core'un `Retreat`'i); son turda onay notu "SON TUR · GERİ ÇEKİLME" |
| 8 | Sonuç kartının tur satırı | "TUR 14 · PAFTA 3/3"; operasyon raporunun SÜRE satırı "14 tur" kalır |
| 9 | Sonuç kartında çıkıp devam | Kart yeniden gösterilmez: sonuç yazılır, operasyon raporu açılır |
| 10 | Devamda önceki odaların telsiz satırları | Oda başına kapanış ayracı, düşenler ve "Kayıttan devam" |
| 11 | Rehber | "raid" adımının gövdesi bayrak açıkken tek tesis metni; adım dizisi değişmez |
| 12 | Yayın lambası, makaralı teyp, sonuç teleksi | Lamba ve teyp aşama 5'te; sonuç teleksi aşama 7'de (onuncu turda aşama 10'a taşındı) |
| 13 | Donanımın görünümü | Gerçek model gelene kadar soğutma ünitesi yedek parçası, ölçekli, üstünde PANO / ANTEN / VANA |
| 14 | Kayıt sürümü | 19 kalır: adlar komut argümanına girmez, kurucu ve aday listeleri ayrık olduğu için ad çakışma denetimi aynı sonucu verir |

**Kararlar (18 Kasım 2026, yedinci tur: aşama 4 "dur ya da devam").** Geliştirici seçti; ölçüm §11.6 "Aşama 4 ölçümü".

| # | Soru | Karar |
|---|---|---|
| 1 | Devam mı, kısma mı | Kısma, yalnız makine yarısıyla: `facilityEntryMachines` 2 → 1, `facilityMiddleMachines` 3 → 1 (giriş ve ara oda yalnız bakım robotuyla başlar). Tur bütçesi kısılmaz (giriş 5, ara 5): 4 tur ilk baskında −17,5 puan |
| 2 | Dönüş bütçesi (§13) | `facilityReturnTurns` 4 → 5: ilk baskında makine kısmanın üstüne +7,5 puan, geride kalanlı baskın %20,5 → %14,7 |

Aşama 4'ün açık bıraktıkları (geliştiriciye bildirildi, karar verilmedi):

- **Güven ve sivil kuralı.** Bu değerlerle de harekâtların %86'sı dağılır (zafer %7, taban %65): başarısız tesis baskınının
  sivil kaybı güveni bitirir. Tur ve makine alanları bunu çözmez; ölçülen ve hesaplanan çareler §11.6'da (1 sivile 1
  güven, dönüşte kırılan sivili saymamak, giriş ve ara odada sivili azaltmak).
- **Süre hedefinin tanımı.** Başarılı baskında gidiş 13–14 tur (12–16 içinde), dönüşle toplam 18–20.
- **Ekibin tamamının öldüğü baskın** (%9; taban %0,9) ve **sunucu baskınında dönüşte ölen ekibin baskını kazanmış
  sayılması.**
- **Ara odanın amacı.** "Yol seçmek" tek bakım robotuyla zayıflar; kepenk aşama 6'da kalkınca yeniden bakılır.

**Kararlar (18 Kasım 2026, sekizinci tur: aşama 5 bina planı).** İlk dördünü geliştirici seçti; öbürleri önerilen
varsayılandır (geliştiriciye bildirildi).

| # | Soru | Karar |
|---|---|---|
| 1 | Gözetim cebinin yeri | BOŞTA bandının sağ ucu: Gözetim [1042, 44, 108, 194] mm, BOŞTA 652 mm. BOŞTA plakası ve pirinç bant kalır, pano mesh'i değişmez (Gözetim bandı ayrı mesh). Deste ve açık el bayrağa bağlıdır (geliştirici, ikinci soru): cep açıkken deste 980 mm, aralık min(82, 518/(n−1)); bayrak kapalıyken deste 1098 mm, aralık min(82, 636/(n−1)), pano aşama 5'ten önceki görünümüyle aynı (`BoardLayout.Deck(watchOpen)`, token `deckNoWatch`; tepsinin çarpıştırıcısını `BoardView3D.deckTray` kaydırır, sahne bayraksız yerde kayıtlı). Kapalı köşe BOŞTA'nın bırakma ve tık alanıdır (eski 770 mm) |
| 2 | Dinleme puanı | Birikir; açılacak oda yokken tavan bir oda bedeli (3) |
| 3 | Paftaların yeri | Görev fişinin iç yanında üstte, üçü yan yana (1080p'de pafta 192×234, bant 596×234, fişten 14 px) |
| 4 | Masadaki kayıt rafı | Ertelendi |
| 5 | Kayıt sürümü | 19 kalır: yeni komut yok, RNG çekilmez; bilinen odalar `liberation_region`, `raidres`, `raid_room` ve vardiya kapanışından yeniden kurulur |
| 6 | Bilgi fişinde paftalar | Yalnız kanadın en az bir odası biliniyorsa. Hep gösterilince cephe dışında üç boş PLAN YOK haritanın üst bandını örtüyordu |
| 7 | Bilinmeyen paftanın adı | Gizli; başlık yalnız oda türünü yazar ("PAFTA 2/3 · ARA ODA") |
| 8 | Paftanın mürekkebi | Şart parçaları kahve (`plan.part`); kırmızı kalem önlem defterine (aşama 10) kalır. Paftalar düz durur: `UILineGraphic` kenar yumuşatmaz, eğik çizgi basamaklanır |
| 9 | Tab sırası | Gözetim, Eğitim'den sonra, BOŞTA'dan önce; bayrak kapalıyken atlanır |
| 10 | Gözetim'in İngilizcesi | "Watch" (plaka, devir fişi, görev adı; önce görev adı "Supervision" idi) |
| 11 | Gözetim cebinin şeritleri | Sığınak cebi biçimi (KİT / ZIRH / YARDIMCI); eşya orada işlevsizdir |
| 12 | Lamba ve teyp eski tahtada | Yok: telsiz masası yalnız tesis baskınında görünür; bayrak kapalıyken baskın HUD'u bugünküyle aynı |
| 13 | Teybin kaydı | Bu baskında dosyalanan ve planı bilinen odalar; etiket son kaydı yazar, sayaç üç paftayı gösterir. Yazılırken `Cue.Pencil`; sönüşte röle sesi yok |
| 14 | Önizleme satırı | Kapanıştan sonraki hâl: "kapanışta dinleme 0/3 → 1/3", plan bedeline varınca "kapanışta PAFTA k çıkar" |

Aşama 5'in açık bıraktıkları (geliştiriciye bildirildi):

- **Bayrak açıkken kalabalık el.** Gözetim cebi açıkken 10 kişilik açık elde kart aralığı 57,6 mm'dir (bayraksız 70,7):
  soyadlar kesilir, öndeki kartın mıknatıs düğmesi bazı adların ilk harfine biner. Bayrak kapalıyken pano değişmez.
- **Paftalar sayaçları örter.** Harita pozunda fiş açıkken paftalar üstteki ERZAK, HAMMADDE ve ÖZGÜR BÖLGE
  sayaçlarının rakamlarını örter (fiş de GÜVEN'i örter).
- **Dinleme hedefi yalnız sıradaki kanat** (§7.2 "Bilinen boşluk").
- **Gözetim'deki kişiye kit takılabilir**; işlevsizdir, sahadan çekilen kit kadroyu zayıflatabilir.

**Kararlar (19 Kasım 2026, dokuzuncu tur: aşama 6 hamleler).** Geliştirici uyurken, onun "karar gerekirse önerinle yap"
talimatıyla verildi: hepsi önerilen varsayılandır, geliştiricinin onayına açıktır (geliştiriciye bildirilecek). Ölçüm §11.6
"Aşama 6 ölçümü".

| # | Soru | Karar | Gerekçe |
|---|---|---|---|
| 1 | Eylem API'si | Ayrı `DoAction`, `ActionTargets`, `PreviewAction`, `CanAction`; eski `DoAct`/`Targets`/`Preview` aynen (D1) | Bot ve sunum eski API'yi "tek yetenek" diye okur; hedefleri ona eklemek seçilen yeteneği yanlış etiketlerdi |
| 2 | Bedeller | Pusu, fırlatma, ortak yeteneğin yerine; koşu ve sürükleme yürüyüş + yetenek; menfez yalnız yürüyüş, yetenek kalır (D4, D26) | §4; "yeteneği durur" = kalır |
| 3 | Sürükleme | Açık eylem; gövde yolun varıştan önceki karesine (BFS, `Dirs` sırası); menzil −`facilityDragMoveCost` (yeni alan, 1) (D3, D4) | Motor yol yürümez; `DoMove`'un görünmeyen yan etkisi menzili yerdi |
| 4 | Menfez | Açık eylem, `DoMove` ışınlamaz (D2) | Öngörülebilir, önizlenir |
| 5 | Koşu | `facilityRushRun` ≤ L ≤ max(`facilityRushRun`, yürüyüş); iki ayrı itme, ilki oynatmazsa ikincisi yok; geçitte kapıya girmez (D5, D6) | "En az 3" alt sınırdır; çift çarpışma hasarı olmasın |
| 6 | Pusu ayrıntısı | Hat kurulduğu kareden; plan sonrası, uyanma öncesi; sahibi tahtada değilse ateşlemez; çok hat ayrı vurur; buluttan kurulmaz; sis kesmez (D7, D8) | §4.1 "yeni yerlerine geçtikten sonra" |
| 7 | İki kişilik işte "bitişik" | Sekiz komşu (D9) | Bölme kepenginde her yanda tek dik komşu var; dik kural kepengi gidişte işe yaramaz kılardı |
| 8 | Dinamit | Kepenk ve sandık nişi hedef değil (D10) | İki kişilik işi atlatmasın |
| 9 | Büyük sandık | Her ikmal hedef odasında, yer varsa; duvar nişi (`Cell.Crate`), RNG'siz deterministik seçim; iş kareleri üç hâlde de batı girişine bağlı (D11) | Oda RNG'si, plan satırları, Digest, robot yerleşimi ve kilit testleri değişmez; tohum 79 ve 1–10'da 24/24 yer bulundu |
| 10 | Sandık ödülü | Yalnız kazanılan ikmal baskınında, kartlı yolda: 2 metal, 1 devre (D12) | §4.5; tesis kartlı oyun için ölçülüyor |
| 11 | Yük | Yalnız kendi hareketiyle varan alır (yürüyüş, koşu, sürükleme, menfez); yük yerde kalırken oda biterse Cargo −2; fırlatmada kapıdaki uzman alıcı değil (D14, D15) | Kapı kuralıyla aynı ilke |
| 12 | Gövde temsili | Yeni alan yok: `Dead` + `AtGate`/`Out`; gövde itilmez, kanca görmez (D16) | Gövde bugün de tahtadaydı; en az kırılma |
| 13 | Temiz odadan toplanan, kapıdaki gövde, kartsız yol | Toplanan sonraki dönüş odasına girer ve orada da taşınmalı (D17); geri çekilmede kapıdaki gövde ölür (D18); kartsız yolda ölüm yok (D19) | "Düşeni kurtarmanın tek yolu çıkarmaktır" |
| 14 | Kayıt | `CurrentVersion` 19 kalır, tesis izi f1 → f2 (D20); AGENTS kural 1'in lafzından bilinçli sapma (§11.4 "Aşama 6'da kayıt") | Etkilenen yalnız tesis kayıtları; bayraksız kayıtlar korunur |
| 15 | Bot ve pusu | Doğrulama kopyasında pusu hatları silinir, kaba beklenti terimi `W.Ambush` eklenir; pusu makine aklına görünmez (D21) | Bot makine evresini önceden görerek pusu seçmesin |
| 16 | Ölüm hedefi tutmazsa | `facilityDragMoveCost` 0 ölçülür; D0 tutup 1 tutmazsa varsayılan 0 (D22). Sonuç: ikisi de tutmadı, **1 kaldı** | §13 "Ölüm" karşı ağırlığı |
| 17 | Rapor ekleri ve hedefin tanımı | Ekler yalnız varsayılan dışındaysa yazılır (D23); hedef (taşınan + toplanan) / harekât ÷ tabanın kurtarılanı ≥ %80, ayrıca / düşen (D24) | Eski dizgiler birebir aynı |
| 18 | Tutuş | İlk uzman tutar ve yeteneğini harcar; ikinci aynı tur gelmezse boşa gider; tutan çekilir ya da düşerse ikinci yeni tutuş olur (D25) | §4.5 |
| 19 | Ölçüm tabanı | `t6taban` = aşama 6'dan önce derlenmiş exe, aynı 80 tohum; bot tarafında eski kuralın taklidi yok | Tam olarak aşama 5 sonu Core ve bot |
| 20 | Botun varsayılan düşen politikası | `hep` (`RaidBot.Fallen`, Camp8 `DUSEN`) | Kural taşımayı bekliyor |
| 21 | Gidişte G/A'daki gövde | Bot dokunmaz (tasarımdaki "yarım bırakma bedeli" yerine) | İleri taşımak gövdeyi H'ye götürüp geri getirmek olurdu; dönüş tahtası gövdeyi zaten koyar ya da temiz oda toplar |
| 22 | GEÇ kazancının tutulması (`holdWin`) | Turun başında gövdeler süresinde kurtarılabilir görünüyorsa GEÇ ve son çıkış kazanç sayılmaz. Kapanış düzeltmesi: yalnız ayaktakilerin süresi de yetiyorsa; her gövde seçilen sürükleyiciye verilir, bir sürükleyicinin gövdeleri sırayla sayılır | Kazanç (10000) yanında bırakma bedeli kararı hiç değiştirmiyordu; ilk hâli geride kalan üretiyordu (bağımsız inceleme) |
| 23 | Son nefes | Oyuncunun hamle sırası dışında düşen ve oyuncuya tur kalmadan biten odada, biri ayakta çıktıysa ekiple gider (§4.3 tablosu); `Unit.Breath`; ölçüm anahtarı `facilityLastBreathDies` (olumsuz ad); taşıyıcının yükü yerde kalır | Oyuncunun hiçbir şey yapamadığı gövde ölmesin; ölçüm: ölümü değiştirmiyor, kural açık kaldı (adil, bedava, RNG yok) |
| 24 | Kural kenarları (bağımsız inceleme 7a/b/c) | (a) Sürükleme yolu kapıdan geçerse gövde kapı olmayan son kareye bırakılır. (b) Yük geçilemez karede düşerse en yakın açık kareye düşer. (c) Pusu, ilk karesi dost uzmanlı ya da bulutlu yöne kurulamaz (B'nin "bulut dışlanmaz" kararını değiştirir); botun `LineHits`'i bulutta durur | Kapının üstünde `AtGate`'siz yatan gövde, alınamayan yük ve boşa giden pusu oyuncuyu yanıltırdı. Not: pusu makine evresinde ateşlendiği için dost uzman aynı turda çekilebilir; dost kısıtı hamleyi kurtarmaz, sırayı dayatır (önce dost yürür, sonra pusu). Aşama 7'de kalırsa kartta neden metniyle gösterilir (onuncu turda açık soruya döndü: kısıt Core'da kaldı, kartta ayrı neden metni yok) |
| 25 | Bot sayacı | `CountFx` efektleri nesne kimliğiyle bir kez sayar | Pusu isabeti 0,92 → 0,43; pusu kısma önerisi geçersiz |
| 26 | Küçük kararlar | `PreviewAction` geçersizde null; `MoveRules` değerleri sıkıştırır (`RushRun` ≥ 1, öbürleri ≥ 0); ekiple gelen gövdenin karesi kademe eki makinelerine kapalı; önizleme hattı (`Line`) yalnız araziye bakar; dört hamleye telsiz satırı | Sunum (aşama 7) hamleyi önizleyip telsizde gösterebilsin |

Aşama 6'nın açık bıraktıkları (karar geliştiricinin; Balance değiştirilmedi):

- **Ölüm hedefi tutmadı; ne yapılacağı.** Ölü 12,7 (≤ 7,5), çıkarılan %20 (≥ %80). Hedefe inmek için −5,2 ölü / harekât gerekir:
  yeni üç kaynağın (bırakılan, ayakta kimse kalmayan, ek geride kalan; ≈ 6) neredeyse tamamı. Önerilen kaldıraçlar:
  1. **Son nefes gövdesi ekiple kalsın:** "ekiple" (Body 1) geçen gövde sonraki odalarda yere konmasın, ekip çıkınca çıkarılsın.
     Üst sınır ≈ 1,6 gövde / harekât. İstenirse "Sedye" bu davranışın kazanımı olur (sedyeli uzman bir gövdeyi odalar arasında
     taşır).
  2. **Kazanılan baskında hedef ve dönüş odasında bırakılan gövde ve geride kalan:** savunma kancası (aşama 11; kanat elde
     kalırsa yatan çıkarılır), "Açık hat" ya da `facilityReturnTurns` 5 → 6, H'de kaçış süresinin bir tur uzaması. Geride
     kalan tabanda da 2,9'du; hedef (7,5) tabanın 7,0'ı üstüne kurulduğu için doğrudan yarar.
  3. **Gidiş odası kaybında yerde yatanın ölmesi** (≈ 3,7 / harekât): geri çekilmede batı kapısına bitişik yatan gövde çekilen
     ekiple dönsün ya da ayrı bir "yaralı esir" durumu (savunma kancasıyla birlikte).
  4. **Hedef yeniden tanımlansın:** bu bot bu kurallarla "çıkarılan ≥ %80"e ulaşamaz; hedef "düşen başına" ya da baskın başına
     ölüyle yazılabilir. "Ölü ≤ 7,5"e ancak 1 ile 2 birlikte yaklaşılabilir.
- **Pusu kısıtı (öneri, gerekli değil):** pusu orta güçte; bot onu boştaki her uzmanın dolgu hamlesi yapıyor (+9 hamle /
  baskın). Arayüz ve tempo yükü sorun olursa "tur başına ekipte bir pusu" (başarıya etkisi ≤ 2 puan).
- **Sedye ve Revir (aşama 8):** Sedye bugünkü tanımıyla zayıf (üst sınırı `t6snD0`: ölü −0,45 ±0,58, çıkarılan %25,5); bölge
  kazanımı olarak harekâtın bir bölümünde açık olacağından gerçek etki daha küçük. Revir yalnız yatışı kısaltır. Aşama 8'de
  Sedye'nin gerçek tanımıyla `t6snD0` benzeri bir koşu ve 1. kaldıracın anahtarlı denemesi ölçülmeli; kıyas `t6sn` ile, baskın
  başına ölüyle.
- **İkmal başarısı tabanın +7,9 puan üstünde** ("taban ±5" dışı); kaynağı taşıyıcı kuralı, fırlatma, ortak iş ve bot.
- **Sunucu kademe 3** −27 puan (n ≈ 130); pusudan bağımsız, ayrı açık iş.
- **Kaçışta süre dolarken son nefes** harfiyen işliyor: o evrede düşen gövde ekiple gider, tahtada kalan ayaktakiler ölür.
  Tuhaf görünebilir.
- **Taşıyıcı son nefeste düşerse** yük yerde kalır (Cargo −2); kural yalnız gövdeden söz ediyordu.
- ~~**Sunuma kalanlar**~~ **Kapandı (aşama 7, 2026-11-19; §4 "Uygulanan hâl (aşama 7)"):** yerdeki yükün işareti (parça,
  YÜK notu, YERDE damgası, amaç satırı, "Yük yerde kalır"), "yük yerde / kayıp" ayrımı, çıkış sayımında gövde (`gone` için
  `!Dead`), yeni Fx türleri ve son nefes gövdesi (kartta EKİPLE TAŞINIYOR, parçası ekiple kalkar, "yerde" sayan satırlar onu
  saymaz). Yalnız sandık nişinin ve kepengin modeli aşama 9'a kaldı.
- **Ara odanın amacı** (yedinci tur): kepenk baskınların %61'inde açılıyor, A'da süre dolması yarıya indi; tek bakım robotuyla
  "yol seçmek" kepenkle kısmen geri geldi.
- **Ekibin tamamının öldüğü baskın** (yedinci tur): odada bütün ekibin düşmesiyle biten baskın yeni kuralda %0,5–0,8 (taban
  %0,3); dönüşte süre dolarak ekibin ölmesi "geride kalan" ve "ayakta kimse kalmadı" kategorilerinde (§11.6).

**Kararlar (19 Kasım 2026, onuncu tur: aşama 7 hamlelerin sunumu).** Geliştirici uyurken, onun "karar gerekirse önerinle
yap" talimatıyla verildi: hepsi önerilen varsayılandır, geliştiricinin onayına açıktır (geliştiriciye bildirilecek).
1–27 aşama 7 tasarımının kararlarıdır; 28–43 parçaların uygulama sırasındaki sapma ve ek kararları; 44–48 bağımsız
incelemeden sonraki kapanış düzeltmeleridir.

| # | Konu | Karar | Gerekçe |
|---|---|---|---|
| 1 | Tuş | Tek tuş **E**, döngü (`ActionKinds` sırası, sürüklemede gövde başına); Esc, sağ tık, Q, seçim ve Z kapatır; kart satırına tık doğrudan kurar | Altı ayrı tuş kartta yer ve ezber ister; §11.5 "eylem seçimi için ikinci tuş" diyor; E boş ve Q'nun yanında; satır tıkı döngüyü atlamayı sağlar |
| 2 | Kart | Prefab değişmez; hamle satırları Row 1'in çalışma anı kopyaları (kardeş sırası yoluyla bağlanır) | Unity yanıtsızdı, prefab YAML'ını elle yazmak riskli; tek satır "yapabileceği hamleler, bedeli, kullanılamıyorsa neden" bilgisini taşıyamaz; kopya E fişini de verir |
| 3 | Satırdaki hamleler | Yalnız bu odada anlamlı olanlar; kullanılamayan soluk ve nedeniyle; kart taşarsa kullanılamayanlar tek satıra katlanır (gerekmedi, bkz. 32) | Öğretici ama kalabalık değil |
| 4 | Plaka satırı | Row 2 olduğu gibi kalır; hamleler altına | Eski satır testleri ve oyuncunun alışkanlığı değişmez |
| 5 | Sürüklemede gövde seçimi | Gövde başına ayrı seçenek; kipteyken gövde karesine tık gövdeyi değiştirir | `DoAction` `other` ister; tık tahtada doğal; E yine yeter |
| 6 | Hedef boyası | Bütün hamlelerde hedef çerçevesi; kipte yürüyüş boyası kapalı | Tıklanacak kare için tek dil; sürükleme ve menfez varışı mavi yürüyüşle karışmaz |
| 7 | Pusu hattının görünümü | `lib.good` yeşil, kesik, oksuz; sahibi tahtada değilse %35 | Kırmızı makine tehdidi, gri kesik sise kesilmiş niyet; yeşil oyuncunun işareti |
| 8 | Yerdeki yük | Küçük sandık parçası (`lib.lootCrate` × 0,45, köşede) ve "YÜK" notu | Kâğıt notu tek başına kalabalıkta kaybolur |
| 9 | Sandık nişi, tutuş, kalkık kepenk | Not (SANDIK, TUTULUYOR) ve hover; kalkık kepenkte not yok; model aşama 9 | Kod-only aşama; modeller aşama 9 "aygıt ve engel modelleri" |
| 10 | Makine evresi sırası | İlk `ambush-hit`'te bölünür: saldırılar, yürüyüş (pusuyla düşecekler dahil), pusu ve uyanma | Core sırası; pusu yürüyüşünü hatta bitireni vurur |
| 11 | Sürüklemenin telsiz kaydı | Yalnız fiş; Core satırı yok | Core'a bu aşamada dokunulmadı; açık iş (tek `Say`, RNG yok) |
| 12 | Sürükleme canlandırması | Sürükleyen yolu yürür, gövde bir adım arkasından | Core yol yürümez; "çekiyor" okunur |
| 13 | Yürüyüşte yük alımı | `Carrier` farkından | `DoMove` `FxList`'i temizlemez; bayat Fx yeniden oynardı |
| 14 | GEÇ ve yerdeki gövde | Onay yok; fişte ve kapı hover'ında uyarı | Altıncı tur 3 (GEÇ onaysız) |
| 15 | Ekip kartı | YERDE · kare / GÖVDE KAPIDA / ÇIKARILDI (kapanışta EKİPLE TAŞINIYOR eklendi, bkz. 46) | Gövdenin yeri ve kaderi tek bakışta |
| 16 | Büyük sandık ödülü | KAZANÇ sandığı içerir (hata düzeltmesi); SONUÇLAR'a satır; 7 yuva aşılırsa değişmemiş alarm satırı düşer | Ödül görünür olmalı; yuva sayısı prefab değişmeden sabit; düşen satır bilgi taşımıyor |
| 17 | Sonuç teleksi (altıncı tur 12) | Aşama 7'den aşama 10'a (teleks işi) taşındı (§7.3) | Hamlelerle ilgisiz masa işi; aşama 10 zaten teleks duyurusu yapıyor |
| 18 | Hedef odasında süre sonu onayı | Kapsam dışı; açık soru | Core'un hedef odasındaki süre sonu davranışı ayrı doğrulama ister |
| 19 | Kurallar ve uyarı metinleri | rules.2'ye tesis satırı, rules.6 tesis metni, geri çekilme tesis anahtarı | Yeni kurallar (taşınmayan ölür) oyuncuya görünmeli; eski tahta metni aynı |
| 20 | Ses | Yeni ses yok | Saha sesleri ayrı iş; `SoundBank.asset` değişmez |
| 21 | Core ve kayıt | Dokunulmaz; sürüm 19 | Hamleler tahta içi; kayda yalnız oda raporu girer |
| 22 | Lokalizasyon akışı | `strings.txt` elle; `UpdateTable` çıktısı toptan kopyalanmaz; denetim `LocalizationTests` ile | Eşzamanlı Core oturumunun satırlarını ezme riski |
| 23 | Test edilebilirlik | Metin, seçenek ve önizleme verisi saf Bridge sınıflarında; `MarksFor(vm)` ayrıldı | Makine evresi dışında her şey EditMode'da sınanır |
| 24 | Rehberli eğitim baskını | E kapalı | Rehberin adımları kaymasın (Q'daki kural) |
| 25 | Hover kartı | Önce hamle önizleme satırları; 5 satır sınırı | Karar anında en önemli bilgi |
| 26 | Kepenk açılışı | `SinkWall` (breach dalı) | Var olan ve sınanmış canlandırma |
| 27 | Yeni test dosyaları | Ayrı dosyalar (`FacilityMoveTextTests`, `FacilityMovesPresentationTests`, `FacilityMovesSceneTests`, `FacilityBigCrateReportTests`, PlayMode `FacilityMovesPlayTests`) | Var olan testlerle çakışma yok |
| 28 | Bedel etiketi | Ayrı metin değil; ayrıntı bedel ile değeri birlikte taşır ("yetenek yerine · hat 4 kare") | Tabloya üç gereksiz metin girmesin |
| 29 | Tıklama sırası | Önce hedef, sonra (sürüklemede) başka gövde, sonra ayaktaki ekip üyesi; bir gövdenin karesi aynı zamanda hedefse tık hamleyi verir | Hedef her zaman önce; gövde E döngüsüyle de değişir |
| 30 | Kurallar kartı ayrıntısı | rules.6 tesis metni kazanma satırının altına eklenir; geri çekilme metni ikmal/sunucu ayrımı olmayan tek tesis metni | Kazanma koşulu kartta kalır |
| 31 | Çıkış satırı | Taşıyıcının adını yazan metin yalnız tesiste; eski tahtada "…malzeme Teknisyen'le çıkar" | Eski tahta aynı |
| 32 | Kart sığması | Ölçüldü: ad tek satır, ayrıntı en çok iki satır (bugün de iki satıra inen EN metni var); en kötü durumda kartın tepesi 770 px, panelin altı 837 px; katlanan "Diğer" satırı gerekmedi | 3'teki yedek kullanılmadı |
| 33 | Fişlerde ad | Telsiz fişinde "kişi · rol" (`PersonalName`), kartta ve hover'da kısa ad (`Who`) | Bugünkü "{0} kapıda" fişiyle aynı |
| 34 | Tutuş ayrıntısı | Tutanın kendi kartında da "… {ad} tutuyor" görünür | Satır zaten kullanılamaz (Acted); neden okunur |
| 35 | Pusu hattının çizimi | El çizimi değil, düz kesik çizgi, 4 px | `RaidArrow`'un el çizimi kipi kesik ve oksuz çizgiyi desteklemiyor; Game'deki `RaidArrow`'a dokunulmadı |
| 36 | Boş pusu hattı | Hat boşsa yönündeki ilk kareye kısa saplama | Kurulu pusu yine görünsün |
| 37 | Çizgi tonları | "ink" grafit çizilir | `lib.ink` açık renk; kâğıt tahtada okunmuyor |
| 38 | Odak (iki kişilik iş) | Yalnız iş karesinin üzerine gelinip önizleme varken | Kalıcı çerçeve hedef çerçevesiyle karışırdı |
| 39 | `RaidPiece.Init` | Tembel kurulum (Game kodu; sahne/prefab değil) | Düzenleme kipinde Awake çalışmıyor; harcanan ya da düşen birimi çizen EditMode testi NRE alıyordu; oyunda davranış aynı |
| 40 | Tesis girişinde çıkan gövde | `evac` Fx'iyle zaten kalkan uzman ikinci kez kaldırılmaz; kapıdaki gövdenin kalkışında da KAPIDA popup'ı | Çift canlandırma olmasın |
| 41 | PlayMode kurulumları | Pusu ve yük testleri kurulumu `BState.Clone` + `EndTurn` ile önceden arar (tasarımdaki `StillTurns` yerine); sürükleme ve fırlatmada Teknisyen'in `Moved/Acted` bayrakları sıfırlanır; sürükleyen Teknisyen (Analist'in menzili 1 çıktı) | `StillTurns` makineyi yürütmez, evre bölmesi sınanmamış olurdu |
| 42 | Raporda sandık değeri | Satır Balance'taki nominal değeri yazar; KAZANÇ kapasite kırpmasından sonraki gerçek değeri | Satır kuralın ödülünü söyler, göz gerçekte gireni |
| 43 | Alarm düşürme kapsamı | Yalnız sandık satırı eklenince ve yalnız alarm `Neutral` (değişmedi) iken; sandıksız raporlar bugünkü gibi | Eski raporlar değişmesin; değişen alarm ve EŞİK AŞILDI bilgi taşır |
| 44 | Makine evresinde amaç satırı | `FacilityRaidText.Goal(display, …)`: taşıyıcı, YERDE damgası ve amaç aynı sunum kopyasından | Amaç, yükün düşüşünü canlandırmadan önce söylüyordu (inceleme bulgu 2); evre dışında `display == State` |
| 45 | Yük yerdeyken çıkış ve kapı | Çıkış karesinde tahliye satırından sonra, kapı kartında sonda "Yük yerde kalır · {kare}"; kapı kartının 4 satır sınırı için önce "Kapıdan geri çıkılmaz", sonra ilk açıklama satırı düşer | Son uzman yükü yerde bırakıp çıkınca yük uyarısız kayboluyordu (bulgu 3); yalnız gövde varken davranış eskisiyle aynı |
| 46 | Son nefes gösterimi | Ekip kartı Core'un oda raporu kuralını izler (girişte ÇIKARILDI, geçişte EKİPLE TAŞINIYOR); parça evre sonunda ekiple kalkar, `Sync` çizmez; hover "Son nefeste düştü: ekip onu da götürür"; "yerde" sayan satırlar (kapı, GEÇ fişi, gözcü, uyarılar) onu saymaz | Aşama 6'nın açık bıraktığı "YERDE değil ekiple" gösterimi; sonuç, zincir kartı ve kapanış satırları zaten Core raporundan okuyordu |
| 47 | `EffectPlaying` uzunluğu | `RaidBoard3D.Animate(…, indexBase, total = -1)`; makine evresinin iki parçası tam listenin uzunluğunu bildirir | Belge yorumuyla tutarlılık (bulgu 6); bugün okuyan abone yok |
| 48 | Elle yazılan `.meta` biçimi | Kısa biçim (`fileFormatVersion: 2` + `guid`, sonda satır sonu yok); `MonoImporter` bloğu eklenmez | Unity 6000.3.12f1 yeni betiğe bunu yazıyor, projenin 511 `.cs.meta`'sının 508'i böyle; tam blok yalnız zorla yeniden yazmada çıkar ve gereksiz commit farkı üretirdi (inceleme bulgu 5'in öncülü bu projede geçmiyor) |

Aşama 7'nin açık bıraktıkları (geliştiriciye bildirilecek):

- **Hedef odasında süre sonu onayı** (18).
- **Sürüklemenin Core telsiz satırı:** Core'a tek `Say`, RNG yok; bugün yalnız fiş var.
- **Kalkık kepenk için kâğıt notu** istenir mi?
- **Pusunun ilk karesi** dost uzmanlı ya da bulutlu olamaz (dokuzuncu tur 24c). Kart yalnız hiçbir yön kalmadıysa "çevre
  kapalı" der; tek bir yönün bu yüzden hedef olmaması ayrıca söylenmez. Ayrı bir metin istenirse eklenecek.
- **Elle oynama:** aşama 7 oyunda elle denenmedi, ekran görüntüsü alınmadı (§15).

**Kararlar (19 Kasım 2026, on birinci tur: aşama 8 ekipman ve ödül).** Geliştirici uyurken, onun "karar gerekirse önerinle yap"
talimatıyla verildi: hepsi önerilen varsayılandır, geliştiricinin onayına açıktır (geliştiriciye bildirilecek). 1–35 aşama 8
tasarımının kararlarıdır (ana oturum K24 Sedye yeniden tanımını ölçüm anahtarıyla, K1 sürüm 19 + tesis izi f3, K2 ve K22'yi
ayrıca onayladı); 36–42 parça A'nın, 43–53 parça B'nin, 54–62 parça C'nin, 63–75 parça D'nin uygulama sırasındaki sapma ve ek
kararlarıdır. Ölçüm §11.6 "Aşama 8 ölçümü"; uygulanan hâl §5 ve §6.2 "Uygulanan hâl (aşama 8)".

| # | Konu | Karar | Gerekçe |
|---|---|---|---|
| 1 | Kayıt | `CurrentVersion` 19 kalır; tesis izi f2 → f3 | D20 emsali: etkilenen yalnız tesis kayıtları; bayraksız kayıtlar korunur |
| 2 | Eylem dizileri | `ActionKinds` altı kalır; `GearActionKinds`, `UnitActionKinds`, `AllActionKinds` ayrı | Aşama 7'nin tuşları ve testleri "altı hamle" diye bu diziye bağlı; ekipman kartı aşama 9'da ayrı kurulur |
| 3 | Aygıtın çalışma anı | Niyetlerden ve sisin dağılmasından sonra, kaçış denetiminden ve uyanmadan önce | "Fitil"; botun doğrulama kopyası gizli bilgi görmez; uyanma RNG'si aygıttan sonra |
| 4 | Hat uzunlukları | Taret 4, vinç 4; taret tel'den geçer, duvar/nesne/bulutta biter; vinç her nesnede biter; yem engelde, kapıda, menfezde durur | Pusu emsali (4) |
| 5 | Tel "atışı geçirir" | Makine ateşi ve kilidi, pusu, taret geçer; kanca, fırlatma, vinç, yürüyüş geçmez | "Atış" nişanlı ateştir |
| 6 | Örme seti | İki eylem: `tel` (1 hak), `kalas` (2 hak) | 2 hak = bir kalas ya da iki tel |
| 7 | Engel yeri ve ekip güvencesi | Dik komşu boş zemin; batı, ileri geçit, hedef bağı "önce tutuyorsa sonra da" | `ExitSafe` kalıbı; gövde engel altında kalmasın |
| 8 | Engel kırma | Tesiste ekibe ulaşamayan her makine engele 11 puan (sivil 9) | "Engel kırma sivile nişandan önce" |
| 9 | Hedefleme | Yem ve havlayan köpek ekip gibi (10); taret, vinç, köpek, tutsak 3; kilit yalnız uzmana | Belgenin "ekip gibi" ayrımı |
| 10 | Aygıt bağışıklığı | Oyuncu EMP'si (hasar, itme), kilit, donma işlemez | "Mekaniktir" |
| 11 | Hurda | Süresi biten/kırılan aygıtın karesinde (1 can); kapı, menfez, gövde/yük karesinde ya da ekip güvencesini bozacaksa kalkar | §5.2 + montajcı satırının güvencesi |
| 12 | Tutsağın süresi | Yakalandığı tur oynamaz; sonraki 3 oyuncu turu oynar; sonra bir makine evresi niyetsiz | §5.4 |
| 13 | Tutsağın eylemi | Atış/vuruş/havan/patlama hasar; onarım ve kalkan uzmana plaka; kilit hedefi bir plan niyetsiz; çoğaltıcı eylemsiz | Belge yalnız bakım robotunu yazmış; çoğaltma oyuncuya makine verirdi |
| 14 | Köpek eşyası | Kişiye bağlı değil, yardımcı yuvada; dinlenme `BoardEquipment.Rest`; ölen sahibin köpeği depoya | Takma kalıcı olmasın; tek yardımcı eşya korunur |
| 15 | Köpek tahtada | Slot −1; yürüyüş 5; kapıya girmez (girişte tahliye); GEÇ'te ekiple; yükü `getir` ile; fırlatma alıcısı; havla Manhattan 3; kokla ileri geçit yanında, kaçıştan önce, oda başına bir | §5.5 |
| 16 | Köpeğin düşmesi | Tahtadan çekilir, gövde değil; rapor "d"; dönüşte 3 vardiya dinlenir | "Köpek ölmez" |
| 17 | Tarif sırası | Eski altı + açılanlar açılış sırasıyla; otomatik yol yalnız eski altı | Defter dizinleri kaymaz; örme/vinç = zırh girdisi |
| 18 | Erzak bedeli | Emir başlarken düşer; yetmezse bekler; `board_reserve` değişmez | Yeni komut gerekmez |
| 19 | Eksik kit emir açıkken | Tesiste eksik rol kiti staged + serbest hammaddeden; emir o kapanış ilerlemez | §5.6; tek üretici tek iş |
| 20 | Emir sayısı | Tek emir sınırı kalır | Belge değiştirmedi |
| 21 | Bekleyen ödül | Yalnız baskın emrini kilitler; zaferde kurulmaz; `Rewarded` ödülü bir kez yapar; bölge fonu/güveni aşama 11 | §9.3'ü öne çekmek ölçüm tabanını kaydırırdı |
| 22 | Cevap paketinde tarif | Seçilebilir; `Recipes`'e cevap kimliği; etkisi aşama 10 | Komut tek biçim; "hep tarif" kazanımsız → `t8yok` = `t6sn` |
| 23 | Kazanımın kapsamı | Ekip çapında; emirde `RaidParams.Perks` | `RaidChain` saf kalsın |
| 24 | Sedye | Yeni tanım (kaldıraç 1); Sırtlayan eski tanımı alır; ölçüm anahtarı `facilityStretcherAlways` | Aşama 6 ölçümü; **ölçüm sonucu §11.6 "Aşama 8 ölçümü"** |
| 25 | Muğlak kazanımlar | Kulak +1/vardiya; Plan defteri ilk kanadın G'si; Eşik gidiş G +1; Açık hat dönüş +1; Revir −1 (en az 1); Tezgâh ödül tarifleri −1; Siper etkisiz | Lafzın en yakın okuması |
| 26 | Kazanım ve unvan | Toplanır | Unvanlar aşama 14 |
| 27 | Kelepçenin tükenmesi | Son raporda hak 0 → panodan silinir; kayıpta da | "Kullanılınca tükenir" |
| 28 | Ölçüm tabanı | `t8taban` = aşama 8 öncesi exe (= `t6sn`); regresyon kilidi `t8yok` | Aşama 6 emsali |
| 29 | Planlama botu | Ortak `CampaignBot.cs`; önce saf taşıma | İki kopyanın ayrışma riski |
| 30 | Kokla kaydı | Rapor `Sniff` → `AdvanceRaidRoom` öğrenir | `Known` yalnız komutlardan |
| 31 | Lokum hedefi | Yalnız duvar; kepenk, sandık nişi, bulut değil | D10 |
| 32 | Aygıtın yönü | Kurulum karesi dik komşu, yön uzmandan kareye | Eylem API'si değişmez |
| 33 | Yükü taşıyan köpek | Rapor `Cargo` = sahibin sırası + `CargoDog` | `Cargo ≥ 0` anlamı korunur |
| 34 | Vinç ve gövde | Gövdeyi ve yükü çeker (D16'ya tek istisna); kapı ve menfeze değil; ayakta birim önce | §5.2; kapıda `AtGate`'siz gövde yanıltırdı |
| 35 | Üretimsiz koşunun eşitliği | `t8yok` = `t6sn` (yeni anahtarlar hariç) | Kurallar ekipmansız etkisiz olmalı. **Tuttu** (80 tohum) |
| 36 | A: `Recipe` | Ödül tarifleri `FacilityGear.Make`'te; Simulation'ın `Recipe` yardımcısı değişmedi | Tarif tablosu tek yerde |
| 37 | A: `PerkDesc(Balance, id)` | Açıklamadaki sayılar dengeden | Metin sayıyla tutarlı |
| 38 | A: ödülü kuran baskın oyunu bitirirse | `PendingReward` dolu kalır, seçim reddedilir ("Oyun bitti."). **Onaya açık** (bağımsız inceleme): zararsız, ama aşama 9 son ekranda "bekleyen ödül"ü göstermemeli. **Aşama 9:** sunum göstermiyor (`FacilityRewardText.Pending` `Ending == null` ister; on ikinci tur 29) | Oyun bitti; istenirse `ResolveLiberationRaidCore` sonunda (`S.Ending != null` iken) boşaltılabilir; bayt eşitliğini etkilemez (Camp8 bu durumda seçim yapmaz) |
| 39 | A: ödül tablosu dışı bölge | Ödül kurulmaz | 12 bölgenin hepsi tabloda |
| 40 | A: K19 "ilk eksik kit" | Harfiyen: ilk eksik kit karşılanamıyorsa sonrakine bakılmaz | Belgenin lafzı |
| 41 | A: ölenle giden eşya | Yalnız baskın ölümünde köpek depoya döner; olay ve ayrılma değişmedi | Kapsam |
| 42 | A: `ReturnPlanningSquad(…, reports)` | İmza A'da, kullanım C'de | Parça sırası |
| 43 | B: dosya | Tahta kuralları `Core/Baskin.Gear.cs` (statik kısmi sınıf) | `Baskin.cs`'te yalnız alanlar ve süzgeç |
| 44 | B: kısa harfler | Taret R, yem Y, vinç V, köpek Ö; can ve yürüyüş `MoveRules`'tan | Mevcut harflerle çakışmaz |
| 45 | B: vinç hattı | Ayaktaki aygıt keser; bulut kesmez; turnikenin kapalı yönü çekişi ve yem adımını durdurur | Belge yalnız duvar/nesne diyor; yürüyüş kuralı |
| 46 | B: hurda yeri ve telsiz | `FreeFloor` + ekip güvencesi; "{0} kırıldı." / "{0} durdu." / "Aygıt hurdaya döndü." | |
| 47 | B: Sedye, kimse ayakta değil | Son raporda gövde `Body 0`, `Fell` son oda (ölür) | K24 |
| 48 | B: niyetsiz makine | `Order 0`, `Intent null`, `Rnd` çekilmez | RNG ilkesi |
| 49 | B: `stuck` | Yalnız tahtada engel varken hesaplanır | Sonuç aynı, ek iş yok |
| 50 | B: C'nin küçük parçaları B'de | Köpek `Hurt` dalı, girişte tahliye, `Machines` tutsağı sayar, köpek fırlatma alıcısı, rapor köpek/tutsak satırları, taşıyıcı köpek | §5.2 tablosu B'deydi; birimsiz tahtada etkisiz |
| 51 | B: olay hedeflemesi | `Baskin.Events.cs` 1277/1319 `Side == 'p'` kaldı (aygıt, köpek ve tutsak olay nişanında ekip sayılır). Aşama 9/10'da `IsSpecialist`'e çevrilmesi düşünülebilir (açık iş) | Olay nişanı ekip tarafı birime de değer verir; eski tahtada aynı |
| 52 | B: kilit | Puan yalnız uzmana; `ApplyIntent` kilidi yalnız aygıta işlemez (köpek/tutsak kilitlenebilir) | K9, K10 |
| 53 | B: test kancaları | `Baskin.TestScore`, `TestTeamLike` (internal) | `TestHurt` emsali |
| 54 | C: sunum alanları | `SquadOutcome.ClampSpent/DogDown` (tasarımdaki `LiberationRaidReward` yerine) | Kişi satırına bağlı; dönüş fişi kişi kişi okur |
| 55 | C: komuta hedefi | Vurulabilir kare: ayakta makine ya da nesne (ikmal kasası, soğutma vanası değil); boş kare ve duvar değil; havan yalnız hedef karesine | Boşa hamle yok; görev bozulmasın |
| 56 | C: bakım/kalkan/bomba | Bakım bulutta makineye vurmaz; kalkan plakasız dik komşu uzman varsa yalnız plaka; patlayan robot çevresinde ekip birimi ya da vuracak şey yoksa verilmez, patlayınca düşer | Sabotaj emsali; ekibe zarar yok |
| 57 | C: kilitleyici komutu | Bu evrenin niyetini silmez; sonraki planı atlatır | Belgenin lafzı |
| 58 | C: kelepçede bulut | Sınır yok; kabin ve kapıdaki makine `UnitAt`'ta yok | |
| 59 | C: bırakılan tutsak | `Locked` temizlenir; `Moved/Acted` dokunulmaz | |
| 60 | C: karesi olmayan köpek/tutsak | Konmaz; ilk odada konmayan köpek rapora girmez | Uç durum, ölçülmedi |
| 61 | C: köpek yükle kalınca | Rapor `Cargo −2`, metin "Köpek yükle tahtada kaldı…" | Eski metin yanıltıcıydı |
| 62 | C: Sniff denetimi | `AdvanceRaidRoomCheck` reddetmez; aralık dışı yok sayılır. Bağımsız inceleme: `Sniff` ve `Units` yapısal denetimde yok (§11.3) | Rapor yapısal denetimi değişmedi; raporlar yalnız `RaidChain.Report`'tan geldiği için bugün etkisiz |
| 63 | D: planlama botu sınıfı | `Tests/EditMode/CampaignBot.cs`, örnekli sınıf (harekât başına durum: taslak emrin günü, tür başına açılan emir, sayaçlar); Camp8 ve `FacilityBotTests` çağırır, `derle.sh` derler | Saf taşıma: bayrak kapalı ve `t8yok` bayt aynı |
| 64 | D: `donus` eşitlik kuralı | Stokta en az olan tür; eşitlikte **bu harekâtta en az emri açılan**, sonra açılış sırası (tasarım: stok, sonra açılış sırası) | Ölenle silinen eşya sık (≈3–4 / harekât): ilk açılan tür hep önde kalıyor, sonraki türler hiç üretilmiyordu (tarama w0: örme 3,1 / harekât, vinç 0,08) |
| 65 | D: girdi ayırma | Emir yalnız bütün girdiler serbest hammaddeden karşılanıyorken açılır ve hemen tam ayrılır; 3 vardiyada başlamayan taslak ve emir açıkken depo dolarsa emir iptal (tasarımdaki "eksik ayrılan taslak sonraki vardiya tamamlanır" yerine) | Eksik ayrılmış taslak `BoardPlanCheck`'te ("… adedi … olmalı") fişi ve baskın emrini kilitler: ilk 80 tohumluk koşuda 3 etikette 7'şer harekât fiş reddiyle düştü |
| 66 | D: takma | Kaynak depo ya da saha rolü dışındaki kişi; kişiye bağlı eşya, dinlenen köpek ve EMP hücresi taşıyan yuva atlanır; `donus`ta (rol dizini + baskın sayısı) ile dönen eşleme | EMP rolünün takviyesi bozulmasın |
| 67 | D: köpek emri | Erzak ≥ bedel + 3 × vardiya tüketimi iken | Açlık eşiği (tasarım) |
| 68 | D: botun sınıf süzgeci | `Baskin.IsSpecialist`: kapıya giriş, puanın can/yol/kilit ve "ekip ayakta" sayımı, sürükleyici, ikinci uzman, `TeamFits`, `Predict` kilit/donma, Camp8 `RaidEnd`/`Fates`; `left` köpek ve tutsağı içerir, aygıtı içermez | Eski tahtada `Class` null: aynı sonuç |
| 69 | D: aday süzgeçleri | tel/kalas makine erişimi ya da (kalas) niyet hattı; taret hattı makine erişiminden geçen yön; vinç hattının ilk dolu karesi gövde, yük ya da **birim (makine ya da ekip)**; yem makine varken; havla erimde makine varken; lokum, kelepçe, getir, kokla, komuta bütün hedefler | Tasarımdaki vinç süzgeci (gövde/yük/makine) vinci hiç kurdurmuyordu (w0: %0); ekip birimini çekmek kaçışta yararlı olabilir, sonuç doğrulamada görülür |
| 70 | D: puan terimleri | Aygıt × kalan tur (`Turret` 4 hattı erişimden geçiyorsa, `Decoy` 2 × yakındaki ≤ 2 makine, `Winch` 6 hattın ilk dolu karesi gövde/yük ise); `Barrier` 3 × engelin uzattığı makine yolu (adım, makine başına ≤ 3; uzmanlardan BFS, birimler sayılmaz); tutsak makine bedelinin `CaptiveShare` 0,3'ü (süresi ≤ 1 iken tam); köpek `DogHp` 2, `DogDown` 30 (taşıyıcıysa `CargoDrop`), `Bark` 4 × erimdeki ≤ 2 makine (kaba, doğrulamada evreden önce eklenir), `Sniff` 8, koklamadan önce ileri geçide `SniffWalk` 2 / tur | Tasarım §8.2; terimler kaçıştan önce ve sonra aynı (sıçrama yok) |
| 71 | D: ağırlık taraması | Tohum 1–20, "iyi" küme, `t8donus`: tasarım değerleri, ×2 ve ×0,5 (`BOTW`) baskın başına ölü ve başarıda gürültü içinde (±0,03, ±1,4p); tasarım değerleri kaldı | En kötü değil; kullanım hedefi ortada |
| 72 | D: köpek taşıyıcı | Tesisin girişinde köpeğin çıkışa yolu (köpek yürüyüşüyle), ara odada en geç kalan uzmanın yolu tam ağırlıkla | Ara odada köpek ekiple geçer; yoksa uzmanlar `Others` 0,1 ile oyalanırdı |
| 73 | D: sayaçlar | R: `odul arac gear rec carry gx gfx dogSteps capSteps`; C: `rwTarif rwKazanim orders ordersDraftLeft made_<tür> gearLost clampSpent dogRest dogSteps capSteps recipes perks gearUsed`; `ozet.py --tesis` "aşama 8" bölümü ve `--esli`; Camp8 `BOTW` (yalnız tarama) | Tasarım §8.3; eşli fark ayrı betik yerine ozet.py'de |
| 74 | D: test düzeltmesi | `FacilityGearTests`'te geri almadan sonraki eski birim başvurusu `Fresh(B, u)` ile (4 yer) | C'nin notu: ikinci uygulama sınanmıyordu |
| 75 | D: köpeğe fırlatma | Bot yükü köpeğe fırlatmaz; köpek yükü yalnız yerden getirir; taşıyıcı köpek havlamaz | Köpekli ikmal baskınlarında başarı düşük bulundu; köpeğin 2 canı taşıyıcıyı kırılgan yapar. Ölçülen farkı değiştirmedi (§11.6 "Aşama 8 ölçümü" okuma 4), kural yine de botta kaldı (Core'da fırlatma köpeğe açık) |

Aşama 8'in açık bıraktıkları (geliştiriciye bildirilecek; Balance değiştirilmedi):

- **Onay bekleyen tasarım kararları:** K24 Sedye (ölçümle kesinleşti), K22 (cevap paketinde tarif), K13 (tutsağın eylem tablosu);
  38 (dağılmayla biten oyunda dolu kalan `PendingReward`; aşama 9 son ekranda göstermemeli ya da Core boşaltmalı).
- **Vinç ve kelepçe kullanım hedefi tutmadı** (%5,0, %6,1): denge değişikliği (kelepçe tarifi, vincin hattı ya da yeni bir çekiş
  kuralı) geliştiricinin kararı (§11.6 okuma 2).
- **Üretim yolunun ~2 puanlık başarı düşüşü** (§11.6 okuma 4): kaynağı bulunmadı. İnceleme adayı (açık emir varken otomatik üretimin
  durması, ekipmanın zırhla aynı girdiyi tüketmesi) aşama 9/10'da sınanmalı: plaka sayısını R satırına ekleyip `t8donus0 − t8yok`
  farkına bakmak.
- **Köpeğin kullanım ölçüsü bilgi vermez:** havla bedelsiz ve her turda verilebildiği için %48 kendiliğinden; anlamlı ölçüler kokla
  (0,35 / baskın) ve getir (0). Hedef köpek için yeniden tanımlanmalı.
- **Olay hedeflemesi** (51): `Baskin.Events.cs` hâlâ `Side == 'p'`; aşama 9/10'da `IsSpecialist`'e çevrilmesi düşünülebilir.
- **`Sniff`/`Units` denetimi** (62): `AdvanceRaidRoomCheck` doğrulamaz; bugün etkisiz (§11.3).
- **Ölüm hedefi** (≤ 7,5) yine tutmuyor (12,7 / harekât); kaldıraç 2–4 geliştiriciye açık (dokuzuncu tur).
- **Sunum ve elle oynama:** aşama 9; aşama 8 yalnız Core testleri ve botla doğrulandı (§15).

**Kararlar (19 Kasım 2026, on ikinci tur: aşama 9 ekipmanın ve ödülün sunumu).** Geliştirici uyurken, onun "karar gerekirse
önerinle yap" talimatıyla verildi: hepsi önerilen varsayılandır, geliştiricinin onayına açıktır (geliştiriciye bildirilecek).
Tasarımın iki açık sorusu ana oturumda kararlaştırıldı: modeller yol E (var olan mesh'lerden kurgu; node kurulmaz), ödül seçimi
tasarımdaki gibi (raporda satır ve ÖDÜLÜ SEÇ, rapordan sonra ele gelen kalıcı kâğıt, EVRAK tepsisi, kayıttan devamda tepside).
1–38 aşama 9 tasarımının kararlarıdır; 39–49 parça 1'in (baskın girişi, kartlar, HUD), 50–59 parça 2'nin (tahta, modeller,
canlandırma), 60–72 parça 3'ün (sığınak, defter, ödül kâğıdı) uygulama sırasındaki sapma ve ek kararları; 73–84 parça 4'ün
(bitti ölçütü, bağımsız incelemenin kapanışı). Uygulanan hâl §5, §5.6 ve §6.2 "Uygulanan hâl (aşama 9)".

| # | Konu | Karar | Gerekçe |
|---|---|---|---|
| 1 | Dördüncü kart | Köpeğin ekip kartı; aygıt ve tutsak kartsız | §11.5 lafzı |
| 2 | Birden çok köpek | Tek Card 4: seçili köpek, yoksa ayaktaki ilk; 4 tuşu köpekler arasında döner; eşya satırı "+n köpek" | 1920×1080'de beşinci kart sığmaz; çok köpek nadir |
| 3 | Card 4 genişliği ve yerleşim | 280 px; Team kabı `ContentSizeFitter`, `childForceExpandWidth` kapalı | Dört 345'lik kart Command'a değer; üç kartın yeri en çok 0,67 px kaydı (bkz. 44) |
| 4 | Ekipman tuşu | Yeni **R**; tek ekipman satırı, R ve satır tıkı eylemler arasında döner | On birinci tur 2 ("ekipman kartı ayrı"): E ve altı hamle dokunulmaz; E döngüsü uzamaz |
| 5 | Ekipman satırı sayısı | Uzman başına tek satır (örme setinde tel/kalas döngüyle) | Kart sığması (10 satır) |
| 6 | Köpek ve tutsak komutları | Onların kartında E satırları; Q satırı yok | Birimin "hamleleri" bunlar; aynı kip, aynı test kalıbı |
| 7 | Seçim kimliği | Uzman harfle (`Selected`), köpek ve tutsak birim kimliğiyle (`SelectedOther`); harf araması `IsSpecialist` | Aynı türden iki tutsak ayrılır; rehber ve 1–3 eski yolda kalır |
| 8 | Aygıt seçimi | Seçilmez; bilgisi hover'da ve tahta etiketinde | Hiç eylemi yok |
| 9 | Sınıf süzgeci | Onay, kaçış uyarısı, Unused, KAPIDA paydası, sonuç sayıları `IsSpecialist` | Core GEÇ'i ve sonucu uzmanla sayar (on birinci tur 68) |
| 10 | Kalan tur gösterimi | Aygıt `Turns` ("{0} TUR"); tutsak `CaptiveLeft` (yakalandığı tur `Turns`, sonra `Turns + 1`; 0'da "SON TUR") | Core'da iki `Turns` anlamı farklı |
| 11 | Aygıt hattı önizlemesi | Bridge'de public parçalarla yeniden kurulur; Core'un evresiyle eşlik testi | Core sabit; `PreviewResult`'ta hat alanı yok |
| 12 | Havla önizlemesi | Erim boyası + "etki niyetlere tur sonunda yansır"; niyet değişmez | Core etkisi sonraki planda |
| 13 | Parça modelleri | Yol E: var olan mesh asset'leri ve `Rev3MaterialLibrary` malzemeleriyle tek seferlik betikle kurgu prefab'lar; yedekler Teknisyen × 0,6 ve enkaz; Rev3 paketi (198) ve three.js kaynağı değişmez. **Kararlaştırıldı (13a soru 1):** yol E; gerçek three.js modelleri için node ve `three@0.160.0` kurulumu kullanıcı onayı bekliyor | Node ağ kurulumu ve sistem değişikliği ister |
| 14 | Kepenk, niş ve tutuş modeli | Kütüphanede tek parça (kit başına değil); duvar bloğuna süs, kalkık kepenk zeminde rulo | 13 kit × model gerekmez |
| 15 | Kalkık kepenk notu (onuncu tur 9) | Not yok; model okunur | Açık soru modelle kapanır |
| 16 | Engel kırılınca | Yerine moloz konmaz | Engel yok olur (Core); dolap enkazı yanlış okunur |
| 17 | Renk ve ölçü | Renkler `RaidPieceLibrary` (ekip `lib.good`); çizgi ve alfa bkz. 51 | Kural 7 |
| 18 | Depo yeri | Kitlerin altında ikinci sıra, yalnız tesis kipinde (y bkz. 64) | Panoda boş tek şerit; hurda kutusuna ve sayaçlara değmez |
| 19 | Yığın sayısı | Kart yüzünde değil yuva etiketinde (bkz. 62) | Yeni TMP yuvası prefab değişikliği isterdi |
| 20 | Simgeler | `DeskSprites` adla (`picto_<tür>`); diziler ve prefab'lar değişmez (çizim bkz. 60) | Kartlar zaten adla sprite okuyabiliyor |
| 21 | Üç girdi | Defter sayfası 3 satır; tarif kartı 3 sütunda adsız (simge + ×n) | Kart 112 mm; üç adlı sütun sığmaz |
| 22 | Kapalı sayfalar | Açık sayfalardan sonra, "KAPALI · {YZ} sunucusundan", sürüklenmez | Oyuncu hangi YZ'nin hangi tarifi verdiğini görür; dizinler kaymaz (on birinci tur 17) |
| 23 | Erzak bedeli | Girdi satırı gibi; tahminde `NoSupplies` | Core sessiz bekliyordu |
| 24 | K19 tahmini | Tahmin eksik kit üretimini söyler | Form "üretici: X" deyip emri ilerletmiyordu |
| 25 | Ödül seçim yüzeyi | Operasyon raporunda satır ve ÖDÜLÜ SEÇ; seçim olay tutanağıyla aynı görünümün ikinci örneğinde (EVRAK tepsisi, kayıttan devamda tepside). **Kararlaştırıldı (13a soru 2):** tasarımdaki gibi | Rapor tek seferlik ve kayıttan devamda açılmaz; tutanak kalıbı sınanmış |
| 26 | Ödül komutu yolu | `GameController.ExecuteBoardCommand(s => s.ChooseServerReward(r, c))` | `DecideShelterEvent`, `PullLastSwitch` emsali; `Rec` `RunCommand`'da |
| 27 | Tepsi önceliği | Açık olay tutanağı önce; ödül kâğıdının kendi tıkı her zaman | `SiginakShelterEvent` değişmesin |
| 28 | Bekleyen ödül kilidinin görünmesi | Tek yardımcı `FacilityRewardText.Pending`: fiş "ÖDÜL SEÇİLMEDİ", fon kısa uyarısı örtmez, CRT ve telsiz ipucu | Core değişmez |
| 29 | Son ekranda bekleyen ödül (on birinci tur 38) | Sunum korur (`Pending` `Ending == null` ister); Core boşaltmaz | Core sabit; final raporu zaten okumuyor; EditMode ve PlayMode testleri kilitler |
| 30 | Kazanımların görünmesi | Görev fişinde KAZANIM satırı; Kulak gözetim önizlemesinde | Fiş baskın kararının yeri |
| 31 | Köpek hurdaya | Sunum reddeder ("Köpek hurdaya atılmaz.") | Aşama 16’da Core ve pano aynı denetime bağlandı |
| 32 | Dinlenen köpek | Kullanım satırında damga, takarken uyarı, kadro raporu satırı | Core sessizce götürmüyordu |
| 33 | Ekipman raporu satırı | Tek SONUÇLAR satırı (kelepçe, köpek); ödül satırından önce düşer | 7 yuva sınırı |
| 34 | Olay nişanı `Side == 'p'` | Değişmez (aşama 10) | Core değişikliği; nişan ve RNG kayda dokunabilir |
| 35 | Bitti ölçütü testi | Tek PlayMode harekâtı; ilk örnekler gerçek giriş (tuş, fare), tekrarlar sunumun aynı karar ve bırakma yolundan; hammadde ve erzak kurulumla (bkz. 79) | Yedi tür × gerçek fare dakikalar sürerdi; yol aynı kod |
| 36 | Rehberli eğitim baskını | R, 4 ve uzman olmayan seçim kapalı | Onuncu tur 24 |
| 37 | Lokalizasyon akışı | `strings.txt` elle; `UpdateTable` çalıştırılmaz | Eşzamanlı oturumlar |
| 38 | Sahne ve prefab işleri | Tek seferlik betik, açık sahne kirliyse durur | Kural 5 |
| 39 | P1: arayüz üyeleri | `IRaidCommands.SelectUnitId/ArmGear/ChooseGear` varsayılan gövdeli; Digit4 denetimi yeni `KeyMap_RGear_Digit4_Free`'de | Aşama 7'nin test sahtesi ve dosyası değişmeden derlenir |
| 40 | P1: kurallar satırı | Ekipmanlı kadroda "R: ekipman", köpekli kadroda "1–4"; ikisi de yoksa aşama 7 metni aynen | `FlagOff_EAndCardUnchanged` aşama 7 metnini birebir sınar |
| 41 | P1: ekipman satırının ayrıntısı | Kısa: "tel 1 · kalas 2 · {n} hak", "hat {0} kare · {1} tur · n hak"; "yetenek yerine" yazılmaz; EN'de "left" ("Wire kit · 2 left") | Uzun TR metin üç satıra taşıyordu (test yakaladı); çoğul sorunu yok |
| 42 | P1: kokla satırı | Oda koklanınca da kalır, nedeni "bu oda koklandı" | Satırı bırakmak daha çok bilgi verir |
| 43 | P1: tutsağın kartı | Kalan tur başlıkta ("DRON · TUTSAK · 3 TUR", "DRON · SON TUR"); köpek kartı "KÖPEK · {sahip}" | `UnitCardView`'da Threat alanı yok |
| 44 | P1: üç kartın yeri (sapma) | Bayrak kapalıyken de kartlar 345,33 → 345 px; sol kenarlar 6 / 357,33 → 357 / 708,67 → 708 (en çok 0,67 px); kap 1060 (sağ dolgu 7), Strip ve Shadow aynı. Test `TeamCards_Three_Fixed345_ContainerUnchanged` (aşama 9 kapanışında `…_RectsUnchanged`'den yeniden adlandırıldı) | Tasarım "piksel piksel aynı" diyordu; force-expand kabı yayıyordu, `ContentSizeFitter` tam sayıya oturtur. Görünür fark yok |
| 45 | P1: kart katlaması | `MaxCardRows` 10; aşılınca iki ya da daha çok kullanılamayan hamle "Diğer · {n} hamle kullanılamaz"; `ChooseAction` satır dizinini `actionRowMap` ile çevirir | §3.1 yedeği; katlama yoksa aşama 7 dizinleri aynı |
| 46 | P1: birim adları | `UnitType.Name` ikinci kez `Loc.T`'ye verilmez; `ObjNames` verilir | Ad zaten geçerli dilde (test yakaladı) |
| 47 | P1: önizleme satırları | Kalasta "Kesilen niyet: n"; telde "yolu kesilen makine" ve yemde "ona yönelecek makine" satırı yok | Niyetler yürüyüşü göstermez; yem kurulunca plan yeniden yapılmaz |
| 48 | P1: hover'da can | Tel, kalas, hurdada "Can" satırı yok | Pipler zaten gösteriyor |
| 49 | P1: kişi satırında kalan hak (§8) | Yapılmadı | Zincir ve kapanış satırlarında kişi satırı yok |
| 50 | P2: doğrulama yolu | Parça 1–2'de editör kilitliydi: tek seferlik prefab betiği batchmode kopyasında `-executeMethod` ile, çıktılar projeye kopyalandı | Editör yanıtsızdı; parça 3–4 editörde |
| 51 | P2: çizgi ve alfa (kural 7 sapması) | `gearLinePx` 3, `decoyPathAlpha` .6, `barkAlpha` .18 `RaidPieceLibrary` asset'inde; `raid_tokens.json` → `RaidTheme` değil | `RaidBoard3D` (Game) `RaidTheme`'yi (Desk) okumuyor; tema içe aktarıcısı Dyn SDF'leri yazar; değerler asset'te düzenlenebilir |
| 52 | P2: `Marks.Tags` | Uzman olmayan birimin etiket adı ve kırmızı bayrağı köprüden | Tahta Game'de, kalan tur metni Bridge'de |
| 53 | P2: NİYETSİZ notu | `Idle` ya da `Intent == null` olan makinede (bkz. 73: kapanışta ikiye ayrıldı) | Core planda `Idle`'ı siler |
| 54 | P2: kepenk modeli | Düz levha + beş kaburga kutusu | Oluklu sac mesh'i dört yüzde 2624 üçgen; bütçe 400 |
| 55 | P2: sandık nişinin yüzü | Dirs sırasıyla ilk duvar olmayan komşu | Gerçek odalarda niş kenarda, tek zemin komşusu var |
| 56 | P2: kalkık kepengin rulosu | Duvar hattına paralel, kasa tepesi y ≈ 0,71 | Uzun piyonla kesişebilir (yalnız süs) |
| 57 | P2: KOKLANDI notu | Son ileri geçit karesinde | İlk karede GEÇİT/KAPIDA notu var |
| 58 | P2: `OnEffect` | Yeni telsiz satırı yok; yalnız sunum kopyası (`dog-down`, `scrap`, `device-break`) | Core'un `Say` satırları kayda zaten giriyor |
| 59 | P2: tahta testi | `Board_GearMarksAndDecor` `FacilityGearSceneTests`'te | Sahne testleri bir arada |
| 60 | P3: piktogramlar (K20 sapması) | PIL değil `Rev3BoardTextureBaker`'ın vektör çizicisi | Var olan piktogramlarla aynı çizgi ve hücre |
| 61 | P3: `DeskSprites` kaydı | Rev3 pano sprite'larından yalnız sekiz yeni ad (yedi piktogram + `icon_erzak`) | Eski pano sprite'ları kayıtta değildi |
| 62 | P3: dinlenen köpek ve yığın | "DİNLENİYOR · n V" kullanım satırında; yığın sayısı yuva etiketinde ("YARDIMCI · ×2") | Eşya kartında `CardStateOverlay` ve sayaç yuvası yok |
| 63 | P3: uzun ad | Eşya kartında ad en çok %20 küçülür (yalnız yeni türlerde); defterde iki satıra sarılır | "Kumanda kelepçesi" komşu karta taşıyordu |
| 64 | P3: depo ikinci sırası | y 571 (tasarımda 566) | 566'da kit sayaçlarının "×n" yazısı kartlara değiyordu |
| 65 | P3: raporda 7 yuva | Düşme sırası: değişmemiş YZ alarmı, ekipman satırı, büyük sandık, "0" sivil, karşı saldırısız YZ alarmı; ödül satırı düşmez | Olağan başarılı rapor zaten 7 satır |
| 66 | P3: arşiv sırası | Ödül kâğıdında yok: tepsiye tık her zaman kâğıdı getirir, etiket hep "EVRAK · SUNUCU ÖDÜLÜ" | Tasarım §7.6 |
| 67 | P3: kâğıdın yüzü | Künye "KYGA · SUNUCU ÖDÜLÜ", başlık YZ'nin adı, kişi satırı bina, sağ sütun "ETKİ"; tutanağın çapasının 12 mm sağında, 5° dönük; EVRAK tepsisinin çocuğu | Gerçek tutanak tepsinin çocuğu |
| 68 | P3: KAZANIM satırı | Görev fişine beşinci SAHA satırı (yalnız tesiste ve kazanım varken) | Dört satır doluydu |
| 69 | P3: cevap paketinin satırı | Açıklama yerine "Cevap tarifi"; Risk satırında temel tarif notu; Söküm anahtarı notsuz | Etkisi aşama 10 |
| 70 | P3: ekipman satırı | Etiket "Ekipman", değer "kelepçe harcandı (…) · köpek dinlenecek (3 V)" | Rapor satırının iki alanı |
| 71 | P3: EN hak metni | "raid charges {0} · {1}" | "1 charges" yanlışı |
| 72 | P3: dinlenen köpek önizlemesi | Not yerine sarı uyarı | Köpek baskına gitmez: uyarı ağırlığında |
| 73 | P4: niyetsiz makine (inceleme 1–2) | Kilitlenen makine (`Idle`, niyet duruyor): hover "Sonraki planında niyetsiz", not KİLİTLİ; planı atlanan (`Intent` yok): hover "Bu planı niyetsiz geçiriyor", not NİYETSİZ. Test Core yolundan (`Idle_LockedThenSkipped_HoverAndNote_FromCore`: kelepçe, tur, kilit komutu, tur) | Eski hover koşulu (`Idle && Intent == null`) Core'da hiç oluşmuyordu, testi elle kuruyordu; kilitli makinede NİYETSİZ bu turun niyet okuyla çelişiyordu |
| 74 | P4: önizleme eşliği olay odalarında (inceleme 5) | Yük turnikeye bakmaz (Core `PullLoad`); çekilen birim ve yem hidrolik yağda kayar (`SlideEnd`, Core `Slide`); kurulum önizlemesinde vincin karesine çekilmez; kablo hasarı sayılmaz. Test `OilSlide_And_TurnstileLoad_MatchCore` | Önizleme yanlış kareyi gösterebiliyordu. Niyetli makineler önizlemede hesaba katılmaz: niyetler aygıttan önce uygulanır, önizleme bugünkü tahtayı gösterir |
| 75 | P4: kartsız oyunda bekleyen ödül (inceleme 6) | `Pending` panolu oyun ister (`sim.Board != null`) | Core kilidi yalnız panolu oyunda; tarifler panonun defterinde |
| 76 | P4: tek kip (inceleme 8) | Kullanılabilir seçenek yokken de E ekipman kipini, R hamle kipini kapatır; ekipmansız uzmanda R hiç etkisiz | Tasarım §2.3 "tek kip açık" |
| 77 | P4: köpek yükle kalırken son çıkış (inceleme 9) | Çıkış karesinin hover'ında zafer işareti yok, "Köpek yükle kalırsa yük kaybedilir" satırı | Core `FinishEscape` baskını kaybettirir; oyuncu görmüyordu |
| 78 | P4: son ekran testi (inceleme 10) | PlayMode `FacilityServerRewardPlayTests.Ending_HidesPaper_TrayLightAndLabelOff`: tepside ve eldeyken `Ending` → kâğıt gizli, ışık ve etiket söner, komut yok | Bileşen davranışı yalnız kurucularla sınanıyordu |
| 79 | P4: bitti ölçütü testinin kurgusu | Yedi ödül kurulumla bekler (`PendingReward`, kayda girmez), kâğıttan seçilir (ilki EVRAK tıkı + 1 + Enter, öbürleri `Decide(0)`); iş emri ilki gerçek sürükleme, öbürleri `PlanningBoardPresenter.ApplyDrop` (tarif ve stok fişleri); ilk kapanış şalter ve devir fişiyle, öbürleri komutla; takma ilki gerçek sürükleme, öbürleri `ApplyDrop`; baskın emri `LaunchSector`; tahtada ışınlama ve hedef hazırlama (makine, duvar) kurulum; köpeğin düşmesi uyanma işaretiyle. Baskın 1: örme seti, lokum, taret; 2: kelepçe, vinç, yem (+ tutsağın komutu); 3: köpek (havla, düşer) | §11.7; `GearUp` kullanılmaz: ekipman ve hak panodan gelir ve sınanır; sonda kelepçe panodan silinmiş, köpek dinleniyor |
| 80 | P4: PlayMode kiti | `FacilityPlayKit.Standing` ve `ThroughGate` uzmana süzüldü | Köpek, aygıt ve tutsak kapıya girmez; `PlayWing` ekipmanlı tahtada kırılıyordu |
| 81 | P4: ayraçlar (inceleme 11) | `string.Join(", ")` ve `" · "` `Loc`'suz kalır | İki dilde aynı; kod tabanının kalıbı (`OperationReportBuilder`'ın eski satırları) |
| 82 | P4: "1 hasar" (inceleme 11) | Metinde sabit kalır | Core'da sabit (taret ve moloz `Hurt(…, 1)`), dengede değil |
| 83 | P4: `SiginakServerReward.Present` (inceleme 11) | Masa kilitlerini atlar (`Take(true)`); değişmedi | Rapor dosyalanırken başka kâğıt elde olmuyor; sıra değişirse iki kâğıt ele gelebilir (açık) |
| 84 | P4: kirlenen yedek font (inceleme 3) | `LiberationSans SDF - Fallback.asset`'e ı ve İ eklendi; aşama 9 kodu değil: eski testlerin fontsuz TMP'si (ör. `SiginakBriefingTests` "HAREKÂT EMRİ", "VARDİYA RAPORU") varsayılan fonta düşer. Dosya geri alınmadı | Geri alma kullanıcıda (`git checkout`); GELISTIRME tuzak notu |

Aşama 9'un açık bıraktıkları (geliştiriciye bildirilecek):

- **Gerçek three.js modelleri:** kurgu parçaları kalıcı mı, yoksa Revizyon 3 hattıyla gerçek modeller mi? Node ve `three@0.160.0`
  (sayfa için playwright) kurulumu kullanıcı onayı bekliyor (13).
- **Kirlenen yedek font** (84): `git checkout` kullanıcının kararı.
- **Köpeğin hurdaya atılması** aşama 16’da Core’da kapatıldı (31). **Olay nişanı** hâlâ `Side == 'p'` (34; aşama 10).
- **Vinç ve kelepçe kullanım hedefi**, **üretim yolunun ~2 puanlık düşüşü** ve **cevap tariflerinin etkisi** (aşama 10, bot) aşama 9'un
  kapsamı dışında kaldı. Sonuç teleksi aşama 10.
- **`Present`'in masa kilidi** (83) ve **hover kartının telsiz masasıyla çakışması:** beş satırlı hover kartının üst kenarı 1920×1080
  ekran görüntüsünde sağ alttaki telsiz masasının (YAYINDA, KAYIT) altında kaldı (kilitli makinenin hover'ı); kısa kartta çakışma yok.
  Yakalama aracının katman sırası mı, oyundaki sıra mı, doğrulanmadı.
- **Elle oynama:** aşama 9 testlerle ve ekran görüntüleriyle doğrulandı; oyun elle oynanmadı (§15).

**Kararlar (19 Kasım 2026, on üçüncü tur: aşama 10 önlem defteri).** Geliştirici uyurken, onun "karar gerekirse önerinle yap"
talimatıyla verildi: hepsi önerilen varsayılandır, geliştiricinin onayına açıktır. Tasarımın dört açık sorusu (S1–S4) varsayılanlarıyla
kararlaştırıldı (aşağıda "Açık sorular"). 1–48 aşama 10 tasarımının kararlarıdır (uygulanan hâle göre düzeltilmiş); 49–55 parça A0 ve
A'nın (Core tahta), 56–60 parça B'nin (Core simülasyon), 61–70 parça C'nin (bot ve ölçüm), 71–80 parça D'nin (sunum) sapma ve ek kararları;
81–88 kapanışın (parça E: bağımsız incelemenin bulguları ve Unity doğrulaması). Uygulanan hâl §5.6 ve §6.3 "Uygulanan hâl (aşama 10)"; ölçüm §11.6 "Aşama 10 ölçümü".

| # | Konu | Karar | Gerekçe |
|---|---|---|---|
| 1 | Kayıt | `CurrentVersion` 19 kalır; tesis izi f3 → f4; Digest değişmez | D20, aşama 8 K1 emsali: etkilenen yalnız tesis kayıtları |
| 2 | Alt bayrak | Sayım, öğrenme, önlemler, cevap etkileri ve sayfaları `facilityCountermeasures` (true) arkasında | Kapalıyken aşama 9 ile bayt aynı (ölçüm kilidi); `t8yok`'un Söküm seçen harekâtları da korunur |
| 3 | Olay nişanı | `TeamLike` (uzman, yem, erimde havlayan köpek) ekip; aygıt, susan köpek, tutsak 0; yağda yalnız uzman; alt bayrağa bağlı değil; ayrı adım (A0) | On birinci tur 51, on ikinci tur 34'ün kapanışı |
| 4 | Sayım yeri | `BState`'te altı int; raporun kuyruk alanı 21; `ResolveRaid`'de tavanlı toplama; başarısız baskın da sayar | Rapordan türetilemezdi; metinden saymak kural 3'ü çiğner |
| 5 | Sayılanlar | Koşu darbesi değdiğinde; vinçte yalnız makine çekişi; lokum yıkımı; tel/kalas birer; kelepçe; havla/getir/kokla; aygıt kurulumu | §6.3 tablosu; vincin gövde/yük çekişi kurtarmadır |
| 6 | Pusu isabeti | Varsayılan sayılmaz; ölçüm anahtarı `facilityTallyAmbush` (true = lafız). **Onaya açık (S1)** | §6.3'ün rol yeteneği gerekçesi: botta pusu her baskında `aygit`'i tavana doldurur (ölçüm §11.6) |
| 7 | Rol yetenekleri | Sabotaj, EMP, kanca, Madenci dinamiti sayılmaz | §6.3 |
| 8 | 2. kademe | Sayılmaz (sayaç durur) | Seçime girmez |
| 9 | Öğrenme anı | İlk tamamlamada, `ChangeTrust`'tan sonra, oyun bitmediyse; eşik en az 1 | Zaferde ve dağılmada duyuru olmaz |
| 10 | Eşitlik | `Learned`'de son girdisi en eski (hiç = en eski), sonra tablo sırası; `CounterAt` alanı yok | Tek kaynak |
| 11 | Günlük | `FacilityState.Learned` (gün, bölge, sınıf, kademe) | Teleks, telsiz günlüğü ve eşitlik aynı okumayı kullanır |
| 12 | Emir kopyası | `RaidParams.Counters/Answers` (K23 kalıbı) | Süren baskın değişmesin |
| 13 | Cıvatalı ayak | İmza = dron ve bakım dışı her tür; itme, kanca, vinç, yağ kayması yok; çarpma hasarı yok; koşu hedefi değil; Söküm sabotajda söker ve iter; sökük dönüşte kalır | Aşama 13'ün makineleri kendiliğinden; §10.2 ile tutarlı |
| 14 | Çift cidar | Odada 2 duvar (k1 G/A, k2 H de); RNG'siz kısayol sırası; tek hamlede 2 hak; Delici 1; dinamit almaz | "Plana kırmızı kalemle" seçili duvar ister |
| 15 | Kesici kol | Makine niyet hasarı tele k1, kalasa k2 +1; itme çarpışması dışarıda; Çelik tel +1 can | Lafız |
| 16 | Kısa bağ | Kelepçe −1 tur (en az 1); k2 bırakılan planlanır (+1 `Rnd`); Yedek röle +1 | Lafız |
| 17 | Koku yayıcı | Her odada kademe kadar; RNG'siz yer; 1 can; köpeğin üç komutu Manhattan ≤ 3'te kapalı; atışı keser; makine kırmaz | Plan önceden göstersin |
| 18 | Parazit | Aygıt −1 tur (en az 1); Çift zemberek +1 | Lafız |
| 19 | Makine aklı | Plan anındaki pusu ve taret hattına varış cezası 6; `Rnd` sayısı aynı | Gizli bilgi değil |
| 20 | Cevap kapsamı | Ekip düzeyinde (`RaidParams.Answers`); sayfa temelin yerinde, aynı `Id`, ad ve +1 devre; temel açık değilse sayfa yok. **Onaya açık (S2)** | Eşya düzeyi altı Desk ve iki bot çağrı yerini değiştirirdi; Söküm fiilen etkisiz kalırdı |
| 21 | Söküm kiti | Kit yolu, eksik kit ve otomatik yol cevap sürümünü kullanır; `Reward` false | Bot ve K19 `kit_tek` kimliğine bağlı |
| 22 | Taslak iptali | Cevap seçilince temellerinin başlamamış taslağı silinir; başlamış emir sürer | `BoardPlanCheck` kilidi (aşama 8 D65) |
| 23 | Kaplama yeri | Oda üretimine girmez; `RoomBoard`, `ReturnBoard` (çift cidar) ve plan kopyası | Digest korunur |
| 24 | Fx ve telsiz | `bolted`, `unbolt`, `dwall`, `cut`, `jam`; yayıcının kırılması nesnelerin `break`'i (`Obj = "yayici"`; bkz. 50); satırlar `Loc` ile Core'da | Bot ve sunum aynı kaynağı okur |
| 25 | SimEvent | Eklenmez | Sunum `Learned`'i okur |
| 26 | Balance | 17 alan (§11.1); asset'e yazılmaz | Aşama 8 emsali |
| 27 | İmza | `\|T`, birimde `U` (bkz. 49), `\|D` yalnız varsa | Eski imzalar değişmez |
| 28 | Rapor | Kuyruk `…\|CargoDog\|Tally`; `RoomMachine` `:1` yalnız sökükse | "Yeni alan yalnız sona" |
| 29 | Okuma sözleşmesi | §11.2 listesi | Sunum kopya kural taşımasın |
| 30 | Ölçüm kilitleri | `t10yokK0` = `t8yok` (80 tohum); `t10tekK0/donusK0` tohum 1–20 = A0 exe'si | Nişanı önlemden ayırır |
| 31 | `ODUL=onlem` | Cevap paketinde tarif yalnız önlem ≥ 1 ve temel açıkken | §13 "Boş cevap tarifi"nin bot karşılığı |
| 32 | `TAK=hic` | Üretir, takmaz | Aşama 8 okuma 4'ün sınaması |
| 33 | R/C anahtarları | `cm tl tlb learn ans cmfx plates sqHp sqMv sqPw rook half tak`; `learns cmEnd nLearn rwCevap prod_* cmRaids_* penCarry carryItems` | Tutarlılık `tl` ↔ `tlb` |
| 34 | RaidBot | Yayıcı terimi `W.Emitter`; k2'de `AmbushValue` × `W.AmbushSeen`; kısa tarama | Önlemin cezası botun hatasıyla şişmesin |
| 35 | `ozet.py` | `--fark`; "aşama 10" bölümü | Politikanın kendi değeri düşer |
| 36 | Hedef | Bitti ölçütü "ölçülür"; hedef tutmazsa denge değiştirilmez, seçenekler belgeye (S4) | Aşama 8 emsali |
| 37 | ÖNLEM satırı | Tasarım: altıncı SAHA satırı (Field 6); sığmazsa KAZANIM ile tek satır. Uygulanan: sığma yolu (bkz. 71) | Aşama 9 Field 5 kalıbı |
| 38 | Görünürlük | Önlem ya da sayaç varken; sayaç hover'da ("YZ gözlemi n/6") | Dönüşümlü oynamanın gerekçesi |
| 39 | ÖNLEM teleksi | `PlayReveal`'da "BÖLGE ÖZGÜR"den sonra, Alarm, `Learned` primed; önlemsizse yok | Kayıttan devamda yinelenmez |
| 40 | Sonuç teleksi | Yeni `SiginakOperation.telex` bağı; satır `Begin`'de kurulur, rapor inerken basılır (bkz. 74); yalnız tesis kipinde. **Onaya açık (S3)** | Bayrak kapalıyken sunum aynı |
| 41 | Kırmızı kalem | `Counter` katmanı, `plan.counter`; bilinmeyen paftada yok | §7.1, sekizinci tur 8 |
| 42 | Robot etiketi | Yalnız cıvatalı ("CIVATALI") | Bütün makinelere işleyen önlem gürültü olurdu |
| 43 | Hover, neden, gözcü | Makine, tel, kalas, yayıcı ve çift cidar hover'ı; köpek, lokum ve koşu nedeni (Core kuralı sökülmüş kopyada); gözcü satırları (§6.3 "Uygulanan hâl (aşama 10)") | Yanlış gerekçe olmasın |
| 44 | Önizleme | `FacilityGearView` Core'un `DeviceTurnsFor/ClampTurnsFor`'unu okur | Aşama 9 K11 kopyası çoğalmasın |
| 45 | Defter ve kâğıt | Sayfa Core'dan; el notu, `AnswerDesc`, `NewPageLine`; depo değişmez | Aynı eşya |
| 46 | Telsiz günlüğü | `Learned`'den satır | Simülasyondan türer |
| 47 | Yayıcı modeli | Yol E kurgu prefab, `RaidPieceLibrary.scentEmitter` | Aşama 9 kararı |
| 48 | Lokalizasyon | `strings.txt` elle; `UpdateTable` çalıştırılmaz | Aşama 9 emsali |
| 49 | A: imza harfi | Sökük birim eki `U` (`u` aşama 8'de kalan tur) | Çakışma |
| 50 | A: yayıcının kırılma Fx'i | Nesnelerin `break` dalı (`Obj = "yayici"`), telsiz "{0} kırıldı."; `device-break` aygıt birimi içindir | Var olan yol |
| 51 | A: Söküm telsizi | "{0}: cıvatası söküldü." ({0} makine) | "söktü"de özne belirsizdi |
| 52 | A: yayıcı bağ kuralı | Tasarımdakinden güçlü: batıdan ulaşılan her kare ve hedef korunur; ayrılmış kareler (geçit, menfez, donanım erişimi, kepengin iki yanı) dışlanır | Ekip ve kural kareleri kopmasın |
| 53 | A: Balance sayısı | 17 alan | Tasarımın "19"u sayım hatası |
| 54 | A: Desc metinleri | Yıkım 2: "Hedef odada da {0} duvar {1} hak ister."; köpek tek satır "Her odada {0} koku yayıcı: {1} kare içinde köpek komut alamaz." | Sayılar dengeden |
| 55 | A: `CountTally` | Public değil; okuyucular `TallyArray`, `RaidTally`; ek okuyucu `DoubleWallCost` | Durum yazan yardımcı dışa açılmasın |
| 56 | B: kayıt testinin yeri | Öğrenmeli harekâtın yeniden oynatması `FacilityCounterTests.Record_F4_LearningCampaignReplays`; `FacilityKit.State` önlem ve iş emri satırlarıyla bütün kayıt testleri sayaçları da karşılaştırır | Tek dosyada |
| 57 | B: cevap seçimi ve iptal yeniden oynatmada | Testte bekleyen ödül elle kurulur; Camp8 REPLAY'de cevap bölgelerine ulaşan harekâtlar `is` satırıyla karşılaştırılır (`ODUL=onlem` dahil, §11.6) | Cevap paketleri geç; test harekâtı ulaşmıyor |
| 58 | B: simülasyon testinde sayaç | Tahtaya doğrudan yazılır; sayım noktaları A'nın testinde, bağ (`Moves.Tally`) ayrıca | Katman ayrımı |
| 59 | B: `RaidCounters` hep 6 öğe | Öğrenme yokken sıfırlar (tahtada sayımı açan `Count == 6`); `Facility.Counter` ilk yazışa kadar boş | JSON ve bayrak kapalı bayt eşitliği |
| 60 | B: iptal ve basılmış fiş | `ChooseServerReward` pano kilidini sınamaz (aşama 8'den beri); fiş basılmışken taslak silinirse fiş eski emri gösterir; kapanışta emir yoktur. Zararsız, nadir | Core'a kilit eklemek aşama 9 akışını değiştirirdi |
| 61 | C: tutarlılık sayacı | R'de `tlb` (botun eylem ve efekt sayaçlarından, `RaidBot.TallyMap`); vincin makine çekişi için `Tally.MachinePulls` (çekiş başına bir: (tur, vinç, makine) tekrarsız; `gfx wpull` adım sayar ve gövdeyi de sayar) | `gx/gfx` eşlemesi vinçte Core'la birebir değildi |
| 62 | C: yayıcı terimi | `W.Emitter` (6) köpek başına, köpek ayaktayken ve `EmitterBlocks` iken; yalnız köpek önlemli emirde ve `GEAR=kullan`'da. Aday eklenmedi: sabotaj, EMP ve tutsak komutu yayıcıyı zaten hedefler; bot yayıcıyı kırar ya da köpeği erimden çıkarır | Var olan adaylar yeter; test `Bot_BreaksTheEmitter_ThatSilencesTheDog` |
| 63 | C: makine aklı terimi | `W.AmbushSeen` 0,25: aygıt 2. kademede (makine aklı) kurulu pusunun kaba beklentisi `Ambush` × 0,25. Tarama 1 / 0,5 / 0,25 / 0,125 gürültü içinde; pusu sayısı 9,6 → 4,7 / baskın | Kısa tarama (§11.6) |
| 64 | C: cezalı taşıma | Eşyanın önlem sınıfı: örme seti örme, lokum yıkım, taret ve yem aygıt, **vinç aygıt ve itme**, kelepçe ele, köpek köpek; eşya-baskın başına | Vinci iki önlem keser (parazit, cıvata) |
| 65 | C: `emit` | Yayıcının susturduğu köpek-tur `cmfx`'te; "makine aklının kaçındığı plan" ölçülmedi (Core'da Fx yok) | Kopya kural yazılmasın |
| 66 | C: `ODUL=onlem` | `CampaignBot.AnswerChoice` (statik, test edilir); seçilen cevap `RewardAnswer` (C `rwCevap`) | — |
| 67 | C: `TAK=hic` | `CampaignBot.TakeGear = false`: `EquipGear` hiç çalışmaz; emir ve stok hedefi aynı | Üretim yolunun ayrıştırması |
| 68 | C: regresyon süzgeci | `K10` tasarımın listesi + `tlb` | Yeni anahtar |
| 69 | C: REPLAY kümesi | Tohum 1–10 × ODUL {tarif, kazanim, onlem} × ARAC {yok, tek, donus} × `facilityTallyAmbush` {false, true} + `TAK=hic` tohum 1–10 | Tasarım §5 + B'nin kümesi |
| 70 | C: ölçüm etiketleri | Tasarım §8.2'nin on etiketi, 80 tohum; sırayla | Zorunlu yedi + önerilen üç |
| 71 | D: ÖNLEM satırının yeri (sapma) | Fiş beş SAHA yuvasında kalır (`LiberationMapBuilder.SlipFieldsMax = 5`; sahne ve `DeskBridgeTests` 5); boş yuva varsa ayrı satır, beşi doluysa KAZANIM ile birleşik ("KAZANIM 1 · ÖNLEM 3"); Field 6 bir kez eklenip geri alındı | Altı satırla BASKIN EMRİ VER 1080'de 29,5 px kadrajdan çıkıyordu; birleşik satır +4,5 px (K37'nin sığma yolu) |
| 72 | D: glyph | `ic_robot_s` (tasarım `ic_bolt_s`) | OLAY satırı `ic_bolt_s` kullanıyor; YZ'nin önlemi robot işaretiyle ayrışır |
| 73 | D: iki önlemin adı | 24 harfi aşarsa değer "2 önlem", adlar hover'da; kısa çiftler sığar ("Kesici kol · Parazit") | "Cıvatalı ayak · Çift cidar" 26 harf; tasarımın örneği eşiği geçiyordu |
| 74 | D: sonuç teleksinin zamanı (sapma) | `Begin`'de kurulur, rapor inince (kamera harita pozunda) basılır; `RaidLog` girdisi başına bir kez; Abort ve yeni oyun bekleyeni siler | `Begin` anında kamera masadaydı: kâğıt okunmuş sayılıyor, satır şeride düşmüyordu |
| 75 | D: EN fiş taşması (aşama 9'dan) | Olaylı bölgede beş satırlı fiş EN'de (görev cümlesi iki satır) BASKIN EMRİ VER'i ~19 px @1080 kadrajdan taşırıyor; önlem defteri kapalıyken de. Düzeltilmedi | Bu aşamanın etkisi değil; fişin üst konumu ya da görev satırı geliştiricinin kararı |
| 76 | D: kalan tur ve lokumun "kalan" satırı (sapma) | `FacilityGearText.CaptiveLeft` kelepçenin tam süresini `Baskin.ClampTurnsFor`'dan alır; lokum önizlemesinin "Duvar yıkılır · kalan" satırı `GearLeft − bedel` yazar (önceden uzmanın yetenek hakkı). Alt bayraktan bağımsız: ana bayrak açık, alt bayrak kapalıyken de metin aşama 9'dan farklı (bağımsız inceleme 3) | Aşama 9 hatasının düzeltmesi; ana bayrak kapalıyken tesis tahtası yok, etkisiz |
| 77 | D: cevap sayfasının notu | El notu cevabın notu; "{YZ} sunucusundan" yerine kaynak `Source`'ta iki YZ ("VERITY · cevap: HALCYON") | Not iki satır sınırı |
| 78 | D: çift cidarın 3B işareti | Yok; hover, gözcü, lokum nedeni ve paftanın kırmızı kalemi söyler | Tasarım istemiyor |
| 79 | D: `plan.counter` tokenı | #B3261A α 0,92, `tokens_siginak.json` ve `SiginakTheme.asset`'e elle (içe aktarıcıyla aynı biçim; `Tokens_AssetIsUpToDate` yeşil) | İçe aktarıcının Dyn SDF tuzağı (aşama 9) |
| 80 | D: yayıcı parçası ve küçük resim | `ScentEmitter.prefab` (kurgu, yol E; çelik kaide, zeytin gövde, pirinç ağız, 304 üçgen), `RaidPieceLibrary.scentEmitter`, yedek soğutma ünitesi × `emitterFallbackScale`; `thumb_emitter` (80 × 92) `SiginakTextureBaker` ve `DeskSprites`'ta | K47 |
| 81 | E: vincin cıvatalı makinede yinelenen satırı (inceleme 4) | Kabul edildi, düzeltilmedi: vinç her makine evresinde yeniden dener, Fx `bolted` ve telsiz satırı aygıtın ömrü boyunca (en çok 3 evre) her evrede yazılır | Kural doğru; her satır gerçek bir denemedir. Tekilleştirmek `BState`'e alan (`CopyInto`, imza, rapor) isterdi |
| 82 | E: sunumdaki kural kopyaları (inceleme 5) | Core'a public `Baskin.WinchPulls(B, t)` (aygıt değil ve cıvatalı değil); `RunWinch` ve `FacilityGearView.WinchLine` onu okur. `FacilityGearText.CaptiveLeft`'in sabit 3'ü kalktı (`ClampTurnsFor` kuralsız tahtada dengenin değerini verir). Test `FacilityCountermeasureTests.Bolt_RushHookWinch_DoNotMove` ekleri | İlke 6. Davranış aynı: bayrak kapalı, `t10yokK0` = `t8yok`, REPLAY 20/20 ve iki ölçüm etiketinin tohum 1–10'u bayt aynı |
| 83 | E: yayıcı ve uyanma adayları (inceleme 6) | Belgeye not (§6.3); kod değişmedi | Adaylardan kopya dolabının komşularını dışlamak ölçümü değiştirirdi; kayıt etkilenmez (tahta oynatılmıyor) |
| 84 | E: bağlantı testleri (inceleme 7) | Değişmedi; §11.6'da "bağlantı testi" diye anılır | Kural `Emitter_PerRoom_Constraints_…` ve `DoubleWall_*`'da, sayım zinciri PlayMode'da ve botta bağımsız sınanıyor |
| 85 | E: İngilizce tekil/çoğul (inceleme 8; D sapma 10) | "Clamps last {0} turn less.", "Devices run {0} turn less.", "Scent emitters in every room: {0}; …", kullanım satırı "+{0} turn" | Sayılar dengeden 1; "+{0} turn." satırlarıyla tutarlı |
| 86 | E: belge notlarının sunum satırları (inceleme 1, 2) | §6.3 ve §11.6 uygulanan hâlle yazıldı (beş yuva, `ic_robot_s`, teleksin zamanı); ölçüm okumasında SE'ler ölçü başına, ölü / harekât "gürültü sınırında azalma" | Belge kodu anlatsın |
| 87 | E: `seenLearned` (inceleme 10) | Değişmedi; aşama 11'e not: sayaç yalnız reveal sırasında ilerler | Bugün öğrenme yalnız ilk tamamlamada ve reveal'le; reveal'siz bir öğrenme yolu (ör. savunma) gelirse birikmiş satırlar sonraki reveal'de topluca basılır |
| 88 | E: Unity doğrulaması | Parça A–D'de kullanıcının editörü yanıtsızdı ("Main thread operation timed out"); doğrulama batchmode kopyada (aşama 7 tarifi). Kapanışta önce batchmode (EditMode 2031: 2026 geçti, 5 atlandı; PlayMode 123: 107 geçti, 16 ortam kalanı), sonra editör yanıt verince editörde boş sahnede tam EditMode 2026/2026 ve tam PlayMode 123/123 (batchmode'da düşen 16 test dahil); Dyn SDF yedekten geri kondu | Editörün açık sahnesi boş ve temizdi; kaydedilmedi, atılmadı |

**Açık sorular (varsayılanla ilerlendi, onaya açık).**

1. **(S1) Pusu isabeti `aygit` sayımına girsin mi?** Varsayılanla ilerlendi: girmez (§6.3 tablosunun lafzından bilinçli sapma, K6).
   Lafız ölçüldü (`t10tekA`, `t10donusA`): ilk önlem harekâtların %100'ünde `aygit` (medyan gün 2), önlem / harekât 2,4–2,5,
   baskınların %60–61'i makine aklı altında; tek ile dönüşümlü ayırt edilmez (cezalı taşıma %59 / %50, başarı farkı −0,3 ±0,5p).
   Değiştirmek tek alan: `facilityTallyAmbush = true`.
2. **(S2) Cevap tarifleri ekip düzeyinde mi, eşya düzeyinde mi?** Varsayılanla ilerlendi: ekip düzeyinde (K20).
3. **(S3) Sonuç teleksi bayrak kapalı oyunda da basılsın mı?** Varsayılanla ilerlendi: yalnız tesis kipinde (değiştirmek tek satır).
4. **(S4, hedef) "Tek araç 5–10 puan düşük" hedefi tutmadı** (`t10tek − t10donus` baskın başarısı +0,1 ±0,6p, farkların farkı +0,2
   ±0,5p; cezalı taşıma %61 / %39 tutar). Varsayılanla ilerlendi: aşama 10 dengeyi değiştirmedi, yalnız ölçtü ve raporladı.
   Seçenekler: önlemlerin şiddeti artsın, hedef yeniden tanımlansın (başarı yerine cezalı taşıma ya da kullanım), `tek` tanımı taşma
   yapan sınıfa kaydırılsın (`tek:winch`), ya da olduğu gibi kalsın.

Aşama 10'un açık bıraktıkları (geliştiriciye bildirilecek):

- **S1–S4** yukarıda; hepsi varsayılanla uygulandı.
- **Hedef tutmadı** (S4). **`ele` hiç öğrenilmedi** (kelepçe 0,02 / baskın): kısa bağ ve Yedek röle botta ölçülmedi. **Bot önleme
  göre araç değiştirmez:** önlemden kaçınan politika (`uyum`) ve `tek:<tür>` ölçülmedi. **Üretim yolunun ~1,6p düşüşünün kaynağı**
  (plaka değil; §11.6 "Aşama 10 ölçümü" okuma 8) bulunmadı. **Makine aklının kaçındığı plan** sayılmadı (Core'da Fx yok).
- **EN fiş taşması** (75): olaylı bölgede beş satırlı fiş EN'de BASKIN EMRİ VER'i ~19 px @1080 kadrajdan taşırıyor (aşama 9'dan;
  önlem defterinden bağımsız). Fişin üst konumu ya da görev satırı geliştiricinin kararı.
- **Vinç telsiz satırının yinelenmesi** (81) kabul edildi; istenirse yalnız ilk denemede yazılır (Core'a durum ister).
- **Yayıcının uyanma adaylarını daraltması** (83) ve **`seenLearned`** (87) aşama 11'de akılda tutulmalı. Vinç hattının Core okuyucusu
  artık var (`WinchPulls`); aygıt hattının geri kalanı (`TurretLine`, `PullEnd`, `DecoyPath`) hâlâ Core'un private kuralının
  Bridge kopyası (aşama 9 kalıbı, eşlik testli).
- **Unity:** parça A–D'nin doğrulaması batchmode kopyadaydı (editör yanıtsızdı); kapanışta editörde tam EditMode ve PlayMode geçti (88).
- **Elle oynama:** yapılmadı; ana bayrak asset'te kapalı, oyuncu yolu yok (§15).

**Kararlar (20 Kasım 2026, on dördüncü tur: aşama 11 kalıcı tahta ve savunma).** Geliştirici uyurken, onun "karar gerekirse
önerinle yap" talimatıyla verildi: hepsi önerilen varsayılandır, geliştiricinin onayına açıktır. Uygulama tasarımının dört açık sorusu
(S1–S4) varsayılanlarıyla kararlaştırıldı (aşağıda "Açık sorular"). 1–40 aşama 11 uygulama tasarımının kararlarıdır (K1–K40; uygulanan
hâle göre düzeltilmiş); 41–56 parça P1'in (Core tahta: D1–D16), 57–67 P2'nin (Core simülasyon: E1–E11), 68–79 P3'ün (bot ve ölçüm:
F1–F12), 80–95 P4a'nın (sunum, masa: F1–F16), 96–110 P4b'nin (sunum, baskın sahnesi: G1–G15) sapma ve ek kararları; 111–119 kapanışın
(bağımsız incelemenin bulguları ve Unity doğrulaması). Parça satırlarındaki "§2.x", "§3.x", "§8.x" atıfları uygulama tasarımının
bölümleridir; karşılıkları §9 "Uygulanan hâl (aşama 11)". Ölçüm §11.6 "Aşama 11 ölçümü".

| # | Konu | Karar | Gerekçe |
|---|---|---|---|
| 1 | Kayıt (K1) | Sürüm 19; iz f4 → f5; Digest aynı | D20, aşama 8 K1, aşama 10 emsali; etkilenen yalnız tesis kayıtları |
| 2 | Alt bayraklar (K2) | `facilityDefenseEnabled` (kuşatma, savunma, §9.3) ve yeni `facilityMarks` (izler, onarım); ikisi C# `true`, asset'e yazılmaz | Kapalıyken `t11K0` = `t10donus` (aşama 10 `facilityCountermeasures` kalıbı); izlerin etkisi ayrı ölçülür (`t11iz`) |
| 3 | §9.3'ün bayrağı (K3) | `facilityDefenseEnabled`'a bağlı; ayrı ölçüm anahtarı yok; "aşama 0 tabanı bu kuralla yinelenir" aşama 17'ye. **Aşama 17'de yinelendi:** kural tabanı değiştirmiyor (zafer +1,25 ±1,25 puan, ölü/harekât 6,66 → 6,58); Taban sütunu geçerli (§11.6 "§9.3 ve aşama 11 K3") | §9.3'ün gerekçesi bırak-yeniden al kârını kesmek; anlık karşı saldırı dünyasında konu dışı; etiket sayısı sınırlı |
| 4 | Kuşatmanın durumu (K4) | `FacilityState.Sieges` günlüğü (kanat, bölge, kuruluş, karar vardiyası, sonuç, terk); tek kaynak | TESIS §11.2 `Siege` sınıfı JsonUtility'de null olamaz; günlük tohum tuzunu, "bırak" kilidini, sunumun teleks/kutu/telsizini ve botun sayaçlarını tek okumayla besler (aşama 10 `Learned` emsali) |
| 5 | Karar vardiyası (K5) | `Step` içinde kurulan: artmış `S.Day`; vardiya ortası: `S.Day + 1`; geçici `stepping` bayrağı | §9.2-3 lafzı; "kurulduğu gün" yerine karar günü tek sayıda |
| 6 | Sayaçlar (K6) | `S.Counterattacks`/`LastCounterattack*` yalnız kanat düşünce (bırak, kendiliğinden, kaybedilen savunma, kanatsız anlık); kuşatma kurulumu `SimEvent.Siege` | Altı Bridge kurucusu bu sayacı "yeniden işgal" diye okur; art arda kazanılan iki savunma aynı tohumu almasın (tuz günlük dizini) |
| 7 | Bekleyen varken ikinci eşik (K7) | Alarm sıfırlanır, kuşatma kurulmaz, güven −1 (`FallSector(null)`: `S.Counterattacks` artar, `LastCounterattack` boşalır; aşama 10'un kanatsız karşı saldırısıyla aynı sonuç, bağımsız inceleme 8) | "İkincisi kurulmaz"; baskı kaybolmasın; nadir (eşik 10) |
| 8 | Kurulum yolları (K8) | `ChangeAlarm` içinde; dört yol kendiliğinden (`StepAlarm`, `raidres`, `event_decide`, kartsız kapı) | §11.4'ün listesi eksik (kapı), konvoy yolu bugün eşikte reddediliyor; belgeye düzeltme |
| 9 | Savunma emri (K9) | Fon yok, `Attempts` artmaz, `S.Raids++`, bekleyen ödülde red, `PlanningRaidCheck`, EMP hücresi harcanır, `Task = "baskin"` | Kadro kuralları baskınla aynı (§9.2); Siper ödülle gelebilir; sunum numarası tutarlı; pano okuyucuları değişmesin |
| 10 | Bırak kilidi (K10) | Komutla bırakılan kanada o vardiya baskın yok; kendiliğinden bırakma kilitlemez; başka bölgede hak korunur | §9.2-2, §9.3 lafzı |
| 11 | `Step` sırası (K11) | Pano dalından sonra: kendiliğinden bırakma → onarım → `StepAlarm` | "Bırak" `StepAlarm`'dan önce (§11.4); düşen kanat aynı gece onarılır ("işgaldeki her kanat"); dagilma en sonda kalır |
| 12 | Savunma tohumu (K12) | `Rng.Mix(S.Seed, 0x53494547 + girdi dizini)` | TESIS'in `S.Counterattacks` tuzu K6 ile anlam değiştirir; dizin her kuşatmada farklı |
| 13 | Dalga türleri (K13) | Dalga başına `Rng.Mix(p.Seed, 0x57415645 + k)` akışı, `WakeType` (imza dahil), kurulumda | Belgede tür yok; aynı dalga her odada aynı; tahtanın `Rnd(B)` sırası değişmez |
| 14 | Dalga büyüklüğü (K14) | `facilityDefenseWaveBase + min(kademe, 3)`: 2/3/4, kademe 4'te 4 | Belgenin tablosu; kademe 4 tanımsız, `MaxEnemies` 7 |
| 15 | Geliş turu (K15) | Giriş odası `D_k`; iç oda `max(D_k + terk, S_r + 1)`; dalga tek odaya gelir; yerleşmemiş gelen GEÇ'te takipçi olur (rapor `Machines`) | §9.2 tablosu; barikat turu korunur; rapora alan eklenmez; kayıttan devam aynı |
| 16 | Yerleşim (K16) | Takipçi önce, sonra dalga; `room.West` sırası, sonra yürüme uzaklığı en küçük boş zemin (y, x); yer/sınır yoksa kuyruk | "O kenardaki en yakın boş kare"nin ölçütsüz lafzı; çekimsiz |
| 17 | Kapı (K17) | Bütün ekip kapıdayken yalnız GEÇ (bugünkü); **kısmi kapıdaki uzman tur sonunda tahtaya döner**; son turda GEÇ yok | "Savunmada kapı sığınak değildir"in kısmi boşluğu (bir uzmanın 10 tur kapıda güvende kalması) ve son turda kapıya saklanma kapanır. **Onaya açık (S4)** |
| 18 | Savunmada kapalı olanlar (K18) | Uyanma, şart takvimi/tetikleri, hazırlık (kırık donanım), `kisa`, başlangıç makinesi | §9.2: makinelerin tek kaynağı dalga ve takipçi; şart parçaları yerinde |
| 19 | Röle (K19) | Hedef nesne `role` olur (4 + Siper); `IsTarget` değil; `CheckWin` savunmada kapalı; her kaynak kırar; kırılınca kayıp | §9.2 tablosu; kaçış/kasa/bakım kuralları karışmasın |
| 20 | Makine aklı (K20) | `role` vuruşu 11, patlamada +6, konumda röleye çekim (−0,25/−0,1 × uzaklık, `Smart`'tan bağımsız) | §15 "röleye ilerleme terimi yok"; sabitler ölçümle doğrulanır |
| 21 | Kazanma/kaybetme (K21) | 10. tur sonunda tahtada ya da kapıda biri ayakta ve röle sağlam; kayıp: ekip düştü, röle kırıldı, geri çekilme | §9.2 |
| 22 | Gövde (K22) | Kazanınca ekibin odasında yatan ve ekiple gelen çıkarılır; kayıpta ve terk edilen odada ölür | §9.2 "Düşenler", §4.3 satır 488 kancası |
| 23 | Deneyim (K23) | Deneyim yalnız kazanılan savunmada (`ReturnPlanningSquad(experience)`) | §9.2 "Deneyim" |
| 24 | Güven ve alarm (K24) | Kazanma −1 × terk; kaybetme −1 −1 × terk; sivil kaybı baskındaki gibi; ayakta kalan sivilin güveni yok; alarm değişmez | §9.2; alarm saldırıda sıfırlandı, zincirleme saldırı olmasın |
| 25 | Sivil (K25) | Savunma odalarında kurulur | §9.1 "her baskında yeniden kurulur" |
| 26 | Sayım (K26) | Savunma önlem sayımına girer; öğrenme yok (bölge tamamlanmaz) → `seenLearned` etkilenmez | Araç kullanıldı (başarısız baskın emsali); aşama 10'un açık notu kapanır |
| 27 | Önlemler (K27) | Savunmada da geçerli (`Counters` emirde) | YZ'nin öğrendiği her yerde |
| 28 | Sonuç dizgisi (K28) | `RaidResult.Encode`'a alan eklenmez; `RaidLogEntry.Mode`, reward sunum alanları | Türetilebilir; dizgi değişmez (TESIS §11.3'ten bilinçli sapma) |
| 29 | İz türetme (K29) | Kalıcı türler ziyaretlerin birleşimi ve korunur; nesne türleri son ziyaretten, kümede olmayan (uygulanamayan dahil) silinir | Rapor `Opened`/`Device` ziyaret başına; nesne izinin uygulanamaması "YZ kaldırdı" okunur, ek hesap gerekmez |
| 30 | Kırık donanım (K30) | Emirdeki `donanim` izi hedef odası hazırlığına girer | §9.1 "hazırlık etkisi sürer"; tahtada donanım yokken rapor `Device` boş kalır |
| 31 | Onarım (K31) | İşgaldeki her kanatta listede en önde `facilityRepairPerShift`; `Repairs` günlüğü | §9.1; sunumun "YZ onardı" satırı ve botun defteri |
| 32 | Terk edilen oda (K32) | İzleri sonuçtan bağımsız silinir | §9.1, §9.2 |
| 33 | Siper (K33) | Röle +`facilityCoverRelayHp` (1); metin dengeden | §6.2 satır 872, K25 |
| 34 | Ad (K34) | `facilityDefenseRelayHp` (belgede `facilityRelayHp`) | `facilityRelayTurns` (Yedek röle) ile karışmasın |
| 35 | Saldırı baskınında savunma kancası (K35) | Bu aşamaya girmez | §12 satır 11'in kapsamı değil; üst sınır bildirilir. **Açık soru (S1)** |
| 36 | Kip kimliği (K36) | `Mode = "defense"`; `IsLoot` savunmada false; `MissionKind` sektörünki | Kısa ASCII kimlik; plan ve günlük sektör türünü okur |
| 37 | Bot politikaları (K37) | `SAVUN` hep/hic/yok, `CEKIL` hic/hep/baski; `S\t` satırı; hedef satırı `t11savun` | §11.6 eşli politikalar; saldırı ölçüleri kirlenmesin |
| 38 | Hedef (K38) | %40–60 dışında kısa tarama ve yalnız yeni savunma alanlarının C# varsayılanı değişir. **Sonuç:** taramadaki hiçbir değer bandı tutturmadı; hiçbir varsayılan değişmedi (78, 111) | Bitti ölçütü "ölçüm hedefte"; alanlar "b" (ölçümsüz); mevcut denge dokunulmaz |
| 39 | Sunum (K39) | (1) Şerit prefab'a iki hücre; (2) BIRAK fişte ikinci düğme, iki tıkla; (3) özgür bölgede damga yok; (4) karar vardiyasında 3 ve ilk harita dönüşü kuşatma fişini açar; (5) komutla bırakmada teleks komutta, kutu kapanışta; (6) izler var olan mürekkep katmanında; (7) onarım `Repairs`'ten hover ve telsiz; (8) dalga Core `spawn` + `wave` popup; (9) kurallar kartına savunma bölümü; (10) EN "Field relay"; (11) savunma final raporda baskın sayılmaz; (12) röle hep görünür, "henüz hedef almaz" ipucu | kesif_sunum §9 önerileri |
| 40 | Okuma sözleşmesi (K40) | Tahta ve simülasyon okuyucuları (§9 "Uygulanan hâl (aşama 11)"; kapanışta `DefenseTurns`, `DefenseWaveCount`, `DefenseSchedulePreview`, `ObjNames["role"]` eklendi); sunum ve bot kural kopyalamaz | Aşama 10 K29 ilkesi |
| 41 | P1 (D1): `Apply` imzası | `Apply(cells, room, marks, rubble, bool events)`: `events` şart parçası karelerini dışlar (`room.PartTiles(3)`, oda tahtasının dışlamasıyla aynı) | Şart parçaları hücre bayrağıyla bulunamaz (meme, durak, süzgeç…); plan = tahta için P2 aynı `events` ile çağırmalı |
| 42 | P1 (D2): Uygulama sırası | Önce kalıcı türler, sonra engeller; her biri liste sırasıyla; dönen liste liste sırasıyla | Engelin bağ denetimi yapının son hâlinde yapılsın (açılan duvar ve kepenk sonra gelse bile) |
| 43 | P1 (D3): Bağ kuralı | Batı girişinden ileri geçidin **her** karesine ve **odağa** (H'de hedef/röle, G/A'da donanım; odak geçilebilirse kendisi, değilse bir dik komşusu); yalnız önceden tutan bağ kopuyorsa atlanır | Belge "hedefin bir komşusu" der; G/A'da donanım hazırlığın hedefidir, kopması tahtayı bozar. `TeamSafe` kalıbı |
| 44 | P1 (D4): Duvar izi | Kepenk ve sandık nişi karesine uygulanmaz; türetmede plan duvarı ∩ kepenk değil | Dinamit oralara işlemez; kepengin kendi izi var |
| 45 | P1 (D5): Takvimin saklanması | `RoomSetup.Waves` ve `BState.Waves` (salt okunur liste; Clone/CopyInto'da, imzada yok) eklendi | `DefenseWaves` okuyucusu yerleşen dalganın çıkış/geliş turunu göstermeli; `Incoming` yerleşince boşalır. Takvim raporlardan türer, imzaya gerek yok |
| 46 | P1 (D6): Geliş formülü | Her oda için tek formül `max(D + j, S_j + 1)` (ilk oda dahil: `max(D, 2)`) | Varsayılanla (ilk dalga 2) belgenin aynısı; `facilityDefenseFirstWave = 1` ayarında da barikat turu korunur |
| 47 | P1 (D7): GEÇ'te kuyruk | Gelmemiş dalga düşer (sonraki odaya yeniden zamanlanır); **takipçiler (dalga 0) her zaman** taşınır, gelmiş ama yerleşmemiş dalga makineleri de | Ekip barikat turunda yeniden çekilirse takipçiler kaybolmasın; belge yalnız `Arrive ≤ Turn` der |
| 48 | P1 (D8): `MarkedDevices` | `RaidChain.Setup`'a isteğe bağlı `FacilityPlan plan = null`; `Open` planı verir | Donanım türü planda. Not: G ve A her saldırıda oynandığı için raporun `Device`'ı (karede donanım yok → dolu) hazırlığı zaten sürdürür; `MarkedDevices` belgedeki gibi eklendi, sonuç aynı |
| 49 | P1 (D9): `RaidChain.Result.Turns` | Savunmada son raporun turu (toplam değil) | Tek tur sayacı; raporların turu küreseldir |
| 50 | P1 (D10): Telsiz satırları | Savunma tahtası açılış satırı (+ hedef odasında röle satırı), kapıdan dönüş satırı; savunmada donanım kırılınca "{0} kırıldı." | "Etkisi hedef odasında görülecek" savunmada yanlış olurdu |
| 51 | P1 (D11): Yerleşim karesi | Batı girişi: geçilebilir ve ayakta birim yok (yatan gövde engel değil); yedek: batıdan yürüme uzaklığı en küçük **zemin** (kablo, kapı, menfez değil) | Makineler saldırıda da gövde üstünden geçer; kabloya düşen makine hasar almasın |
| 52 | P1 (D12): `CampaignLiberation.cs` | Yalnız `LiberationRaidReward` sınıfına §1'in alanları; Simulation mantığı yok | Sınıf o dosyada; §1 bütünüyle P1'in |
| 53 | P1 (D13): Röle dönüşümü | Hedef dolap, önlem kaplaması ve izlerden **sonra** röleye döner | Kaplama ve bağ hesapları saldırı tahtası ve planla aynı hücreleri görsün (plan = tahta) |
| 54 | P1 (D14): Kapıdan dönüş | Geçit karesi doluysa (ayakta birim ya da yatan gövde) `NearestOpen` (geçit/menfez değil); yer yoksa kapıda kalır | Çekimsiz; pratikte hep yer var |
| 55 | P1 (D15): Kazanma denetimi | `Turn > MaxTurns`'de yalnız "ayakta biri var mı"; röle kırılınca tahta zaten kayıptır | Röle kırılması anında `Lose` |
| 56 | P1 (D16): İlk oda kadrosu | `FrontTiles`: ileri geçitten yürüme uzaklığı en küçük boş zemin (geçit, menfez, parça karesi değil; y, x) | §2.3 lafzı |
| 57 | P2 (E1): Emir gövdesi | Saldırı ve savunma tek `OrderRaid` yardımcısını kullanır; saldırının tohum çekimi, fon ve sayaçları çağıranda | §3.4 "ortak gövde bir yardımcıya çıkarılabilir"; bayt kilidi tuttu |
| 58 | P2 (E2): `Step` | Gövde `StepCore`'a taşındı, `Step` `stepping`'i `try/finally` ile kurar | §3.1 lafzı; `Step`'in erken dönüşleri değişmedi |
| 59 | P2 (E3): Okuyucu ekleri | Sözleşmenin dışında `ActiveSiege` (bekleyen ya da süren), `YieldedToday`, `WingMarks`, `RegionFundsDue` (bölge fonu ve güveni verilecek mi), sabitler `DefenseMode`, `Siege*` | Sunum fişin "fon · bölge özgür" satırını ve süren savunmayı kural kopyalamadan okusun (K40) |
| 60 | P2 (E4): `SiegeSector` | Bekleyen **ya da süren** kuşatmanın kanadı | Savunma sürerken sunum kanadı göstersin |
| 61 | P2 (E5): Savunma sonucunda sivil | `reward.CiviliansLeft` ve `TrustFromCivilians` 0 (ayakta kalan sivilin güveni yok, K24); sivil kaybı güven düşürür | Tablo |
| 62 | P2 (E6): Savunmanın an satırı | Kazanınca `Moment("good", "{0}: {1} tutuldu.")`; kayıpta `FallSector`'un "yeniden işgal altında" anı | Telsiz ve devir fişi mevcut okuyucularla |
| 63 | P2 (E7): Kendiliğinden bırakma | Önce "karar verilmedi" anı (bad), sonra `FallSector`'un anı | Sunum kutusu "karar verilmedi"yi `Outcome == "kendi"`den de okuyabilir |
| 64 | P2 (E8): Komut mesajları | `siege_defend`: "{0}: {1} savunuluyor."; `siege_yield`: "{0}: {1} bırakıldı." (dağılmada dağılma metni) | Fiş geri bildirimi |
| 65 | P2 (E9): Savunma `ResolveDefense` denetimi | Kanat alınmış, etkin sektör ve süren kuşatma kanadıyla aynı; değilse "Baskın hedefi geçersiz." | Bozuk kayıt sessiz geçmesin |
| 66 | P2 (E10): Ortak yardımcılar P1 dosyalarında | `Baskin.DefenseWaveSize`, `RaidChain.DefenseRelayHp` (P1'in formülleri taşındı) | Okuyucular (`DefenseWaveSize`, `DefenseRelayHpFor`) kural kopyalamasın |
| 67 | P2 (E11): Eski Siper metni | `strings.txt`'te "Kullanılmayan çeviriler" bloğunun sonuna `#` ile taşındı | `UpdateTable`'ın yapacağı |
| 68 | P3 (F1): Satırlar | `S\t` yalnız savunma açıkken; R ve C'nin aşama 11 anahtarları ana bayrak açıkken hep (kilitte atılır) | Saldırı ölçüleri kirlenmez; K0 kilidi kırpmayla |
| 69 | P3 (F2): Komutlar nerede | `CampaignBot.Siege` (Camp8 ve testler ortak); sayaçlar `Defended`, `Yielded`, `DefendRefused` | Aşama 8 K29 kalıbı |
| 70 | P3 (F3): `baski` | Turun başında karar; o odada kalıcı | Kapıda yarım kalan ekibin her tur geri dönmesi olmasın |
| 71 | P3 (F4): Savunmada kapalı bot kuralları | Donanım hedefi, `bodyRescue`/`holdWin`/`BodyCost` | Görev tutmak; gövde kuralı Core'da (kazanınca çıkarılır) |
| 72 | P3 (F5): Tutmada kapı | Herhangi bir uzmanı kapıya sokan aday (yürüyüş, sürükleme, menfez) elenir; `GateIdle` çalışmaz | Kapı sığınak değildir (K17) |
| 73 | P3 (F6): Hayalet makine | Sonraki tur gelecek makinenin tahmini karesi + türünün yürüyüşü (BFS, birimler sayılmaz); engel, taret, pusu adayları ve değerleri, pay `WaveGhost` | Barikat turunda makine erişimi boş: hayaletsiz engel adayı hiç yok (test) |
| 74 | P3 (F7): GEÇ'in puanı | Savunmada `pass` ve `CanPass` kazanç gibi; çekilirken `GateScore` | Saldırının oda geçişi kalıbı |
| 75 | P3 (F8): `siegeNone` | `S.Counterattacks − birak − kendi − dustu` (günlükten) | K6: sayaç yalnız düşüşte |
| 76 | P3 (F9): `recapFunds`, `regionGain` | Gerçek verilen değer (`RegionRepeat`'te 0) | K0'da aynı (kural kapalı) |
| 77 | P3 (F10): Savunmanın sayaçları | Savunma `raids`/R ölçülerine girmez; `gearUsed`, iz defteri ve `marksSeen`'e girer | R süzgeci; I3 bütün `raidres`'leri kapsamalı |
| 78 | P3 (F11): **Hedef tutmadı** | Varsayılanlar değişmedi. **Öneri (Core, ayrı iş):** savunmada tutulan odaya bir amaç ver: (a) **sızma**: makine evresinin sonunda ekibin odasının ileri geçidinde duran makine içeri sızar (bir sonraki odaya geçer; hedef odasına varan röleye vurur) ya da her sızan makine güven/röle bedeli; (b) dalganın girişi tek kenara yığılmasın (batı + menfez, ya da girişte bekleme yerine sonraki tur); (c) savunmada pusu hattı sınırı. Öneri sırası a > b > c; her biri tek ölçüm anahtarıyla `t11savun` 80 tohumla ölçülmeli | Tarama: dalga, süre, takvim tutmayı hiç değiştirmiyor (%100, 0 düşen); yalnız röle (amaç) baskı yaptı (%80–93). Bot görünen niyetlerden serbestçe kaçabildiği sürece makine sayısı bedel üretmiyor |
| 79 | P3 (F12): `--esli` savunma başarısı | Savunmasız tohum atlanır (None) | Aksi hâlde 0 sayılıp farkı şişirir |
| 80 | P4a (F1): Kuşatma kipinin koşulu | Fiş kuşatma kipinde: karar vardiyası, savunma sürüyor ya da bölgede emir verilecek başka kanat yok. Vardiya ortasında kurulan kuşatmanın vardiyasında cephe fişi baskın emrine açık kalır | Core karar vardiyasına kadar baskını kilitlemez; o vardiya fiş SAVUN'u (gerekçeyle kapalı) gösterip baskını saklamasın |
| 81 | P4a (F2): Kuşatma kipinin SAHA'sı | BÖLGE, İMZA, DALGA, İZ, RÖLE; TEHDİT kalkar (kademe DALGA hover'ında), OLAY yok (savunmada şart takvimi kapalı), KAZANIM/ÖNLEM yok (Siper RÖLE'de) | Beş yuva kuralı (K37) |
| 82 | P4a (F3): Karar vardiyasında cephe fişinin notu | Kısa "ÖNCE KARAR VER" (düğme yanındaki not yuvası, ≤ 20 harf); uzun gerekçe hover'da Core'unki | "KARŞI SALDIRI BEKLİYOR · önce karar ver" not yuvasına sığmaz |
| 83 | P4a (F4): KUŞATMA'nın EN'i | "SIEGE" (tasarımda "UNDER SIEGE") | Aynı anahtar harita damgasında; YENİDEN İŞGAL ("RE-OCCUPIED") yuvasından kısa kalsın, `MeasureMetrics` ve üretilmiş harita yerleşimi değişmesin |
| 84 | P4a (F5): Kartlı kapanışın duyurusu | Rapor olmadığı için teleks: `SiginakMap` kuşatma günlüğünü izler (primed; yeni oyun ve kayıttan devam basmaz), kurulan kuşatma ve kendiliğinden bırakma birer satır. Baskının kurduğu kuşatma operasyon raporunda (satır) ve `SiginakOperation`'ın ikinci teleks satırında | §8.1 "Teleks" maddesi kartlı oyunda başka yüzey bulamıyor; aşama 10 `seenLearned` kalıbı |
| 85 | P4a (F6): Kendiliğinden bırakmanın kartlı teleksi | "YZ KARŞI SALDIRISI · … kanat bırakıldı (karar verilmedi) · sivil güven −1" (Alarm) | Bugün kartlı oyunda anlık karşı saldırının teleksi yok; karar verilmeyen kanadın düşüşü oyuncuya söylenmeli (PlayMode sınar) |
| 86 | P4a (F7): `ShiftReportInput` alanları | `siege`, `siegeRegion` + `siegeFall`, `siegeFallRegion`, `siegeAuto` (tasarımda yalnız ilk ikisi) | Kendiliğinden ve komutla bırakmanın kutusunu ayırmak için; `[Serializable]`, varsayılan boş |
| 87 | P4a (F8): Alarm gözü ve lamba | Eşik = kuşatma kuruldu ya da kendiliğinden bırakmadan fazla düşüş; lamba her düşüşte | Kendiliğinden bırakma eşik değildir (alarm sıfırlanmadı) |
| 88 | P4a (F9): Komutla bırakmanın kapanış kutusu | "YZ KARŞI SALDIRISI · … kanat bırakıldı."; teleks ve harita dizisi yalnız komutta (`CounterMap` false) | K39-5; kutu basılınca eski dizi yinelenmesin |
| 89 | P4a (F10): Savunma raporu | Damga TUTULDU / KAYBEDİLDİ (DÜŞTÜ'nün EN'i kişi için "DOWN"); alarm satırı yok (değişmez); "YZ karşı saldırısı" satırı yok (başlık söyler); deneyim eki yalnız tutulunca; ekipman satırı yer varsa | Core: savunmada alarm değişmez, deneyim yalnız kazanınca |
| 90 | P4a (F11): Final rapor | Savunma BASKIN sayısına ve başarıya girmez (K39-11); savunma açıkken kuşatmanın kötü anları (kurulma, karar verilmedi) kırılma anı değil, kanadın düşüşü "yeniden işgal" anıyla gelir | Aksi hâlde "YZ karşı saldırısı: X: Y kuşatıldı; …" gibi çift ve yanlış anlar |
| 91 | P4a (F12): Pafta izleri | Veri `Traces` (uygulanan izler) + `PlanItem.Barrier/Scrap`; çizim mürekkep katmanında (K39-6); `PlanItem` sözleşme testi güncellendi | Sahne değişmez; tel/kalas/hurda planın gerçek nesnesi olduğu için nesne işaretiyle |
| 92 | P4a (F13): Yeniden özgürleşen bölgenin teleksi | "BÖLGE ÖZGÜR · {bölge} · yeniden özgür" (fon yazılmaz), son baskının `RegionRepeat`'inden | §9.3: fon ikinci kez verilmez; eski satır yanlış tutar yazardı |
| 93 | P4a (F14): BIRAK kapalıyken | Tık onay sormaz, komutu dener (YAPILAMADI + Core gerekçesi teleksten) | Kapalı düğmenin nedeni görünsün; zaten `interactable` false (yalnız hover gerekçe) |
| 94 | P4a (F15): Tek seferlik betiğin yeri | Kopyada `Desk/Scripts/Editor/` (tasarımda `Assets/Corrigible/Editor/`) | `Corrigible.Editor` asmdef'i Desk'i görmüyor; betik projeye hiç girmedi |
| 95 | P4a (F16): Devir fişindeki kutunun yeri | KARŞI SALDIRI ilk kutu (KAPI'dan önce) | En acil karar; en kötü fişte sığdı (0,9 mm pay) |
| 96 | P4b (G1): 10 turun şeride sığması | 8'den çok turda hücreler 8 hücrelik genişliğe daralır (`RaidHudView.FitCells`; 34 → ~26,8 px); 8 ve altında prefab genişliği | Cell 9/10 eklenince şerit 72 px uzadı ve evre çipini (SIRA SENDE) örttü; sayfanın yerleşimini değiştirmeden çözüm. Sahne testi kilitler |
| 97 | P4b (G2): Dalga işareti | Numaranın yanında küçük "D" (EN "W"), bölge olayının harf kalıbı | Savunmada bölge olayı yok; altı çizili numara saldırıda olayın anlamını taşıyor |
| 98 | P4b (G3): Barikat turunun "evre/olay satırı" | TELSİZ fişinde barikat turu boyunca "SAVUNMA · BARİKAT · makine yok"; evre çipi aynı | Tasarım "olay satırı `eventLine`; çip aynı" |
| 99 | P4b (G4): Dalga satırı | Görev çipinin ikinci satırı (can yok): sıradaki gelmemiş dalga; dalga kalmadıysa satır yok | HUD'da iki hedef satırı var: geçit/röle + dalga sığar |
| 100 | P4b (G5): İÇERİ ÇEKİL onayı | Var olan onay notu (`ConfirmTitle` + `FatalWarning`), düğme onayda "EMİN MİSİN?", Esc vazgeçer; saldırıda GEÇ onaysız kalır (`FacilityRaidHudTests` değişmedi) | Yeni sahne öğesi gerekmez |
| 101 | P4b (G6): Sonuç kartının başlığı | "KANAT TUTULDU" / röle kırıldıysa "RÖLE KIRILDI" / değilse "KANAT DÜŞTÜ"; masadaki rapor her kayıpta "KANAT DÜŞTÜ" (P4a) | Tasarım tablosu: "KANAT DÜŞTÜ ("RÖLE KIRILDI")" |
| 102 | P4b (G7): Rölenin adı | Sunum "Saha rölesi" yazar; Core `Baskin.ObjNames`'te `"role"` yok (bkz. §7) | Core'a dokunulmadı; inceleme kartı eskiden `ObjNames["role"]`de KeyNotFound verirdi |
| 103 | P4b (G8): `Defense_Lost_RelayBroken`'da rölenin kırılması | Teknisyen'in sabotajı (2 vuruş) — "her kaynak kırar" (K19) | Makine aklıyla kırılma tur sayısı belirsiz; test deterministik kalsın |
| 104 | P4b (G9): Makine gelişinin dili | Etiket "GELDİ" (UYANDI yerine), fiş "makine geldi · 1-A4", "DALGA n geldi: k makine", tahtada "DALGA n" | Savunmada uyanma yok (K18) |
| 105 | P4b (G10): Gelecek tur gelecek makineler | Core `DefenseIncoming`'in tahmini kareleri "robot uyanacak" işaretiyle | Tasarım "B.Spawns yolu" diyordu; Core savunmada `B.Spawns` doldurmuyor, okuyucu var |
| 106 | P4b (G11): Kuşatma fişinin 1080 sığması (P4a fişi) | Ödülsüz kuşatma kipinde ÖDÜL satırı kalkar; BEDEL "fon yok · baskın hakkı" ("vardiyanın baskın hakkı" taşıyordu; EN "no funds · raid slot"); EN görev cümlesi kısaldı ("Hold the wing for {0} turns in three rooms; guard the relay. Yielding loses it, trust −{1}."); EN "{0} trace(s)" | Ekran görüntüsünde BIRAK alttan kesiliyordu (EN'de 7 px); PlayMode testi şimdi BIRAK'ın alt kenarını TR/EN ölçer |
| 107 | P4b (G12): Tek seferlik betiğin yeri | Kopyada `Desk/Scripts/Editor/` (tasarımda `Assets/Corrigible/Editor/`) | P4a F15: `Corrigible.Editor` asmdef'i Desk'i görmüyor; betik projeye hiç girmedi |
| 108 | P4b (G13): Kurallar kartı | Savunmada 6. satır (kazanma) dört satırlık savunma bölümüyle değişir | Saldırının kazanma metni savunmada yanlış |
| 109 | P4b (G14): Kapı (K17) sunumu | Kapı hover'ı ve fişi savunmada "kapı sığınak değil", "İÇERİ ÇEKİL" | Saldırının "kapıda bekler, hedef alınmaz" metni savunmada yanlış |
| 110 | P4b (G15): Donanım satırı | Saldırıda izle kırık gelen donanım KIRILDI damgalı satır (Max 1); savunmada donanım satırı ve "Kırılırsa hedef odasında…" hover satırı yok | Savunmada hazırlık kapalı (K18); tasarım "izden kırık donanım KIRILDI" |
| 111 | Kapanış: bitti ölçütü (inceleme 1) | Aşama "oynanır" yarısıyla kapandı; ölçüm yarısı tutmadı (%100) ve **yeni kural eklenmedi**: sızma (78 (a)) ya da başka bir savunma amacı geliştiricinin kararına bırakıldı. Denge değişmedi | S3'ün yetkisi yalnız yeni alanların değeridir; sızma yeni bir oyun kuralıdır (Core, sunum, bot ve ölçüm ister) ve hangi amacın seçileceği tasarım kararıdır. Aşama 10 emsali (hedef tutmadı, denge değişmedi; on üçüncü tur S4). Kuşatma başına okuma (%46) "Açık sorular" 5'te |
| 112 | Kapanış: ölçümün paydası (inceleme 2) | §11.6 başlık tablosuna "savunulabilen kuşatma" ve "kuşatma başına tutulan kanat" satırları; okuma 4'e ek | %100 yalnız sahaya çıkabilen ~%46'nın başarısıydı; red gerekçesi hep "üç sağlıklı kişi" |
| 113 | Kapanış: rölenin adı (inceleme 4; P4b G7) | `Baskin.ObjNames["role"] = "Saha rölesi"`; `FacilityDefenseRaidText.RelayName` ve `RaidDataSource` (hedef satırı önbelleği, inceleme kartı) oradan okur; `strings.txt` satırı Core bölümüne taşındı. Test: hedef odasındaki her nesnenin adı var | Genel `ObjNames[o.K]` yolu röleye uğrarsa KeyNotFound atmasın; ad tek yerde |
| 114 | Kapanış: kartsız kapının kuşatması (inceleme 5; P4a §5) | `DoorVisit.Siege` (yeni alan); kapı cevabı kuşatma kurarsa `v.Siege = true` ve sıfırlanan alarm `v.Alarm`'a yazılmaz; `DoorViewBuilder.Effects` "Karşı saldırı bekliyor". Test `FacilitySiegeTests.DoorAnswer_AtThreshold_VisitKnowsTheSiege` (savunma açık/kapalı) | Ziyaret kartı kuşatmayı bilmeliydi; savunma kapalıyken eski hâl (`Counterattack`). Camp8 `counterDoor` sayaçtan okur, etkilenmez |
| 115 | Kapanış: sunumdaki denge sıkıştırması (inceleme 6) | Core okuyucuları `Simulation.DefenseTurns`, `DefenseWaveCount`, `DefenseSchedulePreview(tier)` ve `RaidChain.DefenseTurns/DefenseWaveCount/DefenseSchedule`; `RaidChain.SetupDefense`, `FacilityDefenseText` (görev cümlesi, SAVUN'un tur sayısı, DALGA satırı) ve `FacilityDefenseRaidText.WaveTotal` onları okur. `liberationCounterattackTrust`'ın gösterimdeki `Math.Max(0, …)`'ı (aşama 10'dan) değişmedi | K40; davranış aynı (bayt kilitleri) |
| 116 | Kapanış: test gücü (inceleme 7) | `FacilityDefenseTextTests` fişin görev cümlesini, BEDEL'i, BIRAK bedelini ve onayını, DALGA değerini ve başlıkları sabit TR/EN metinle bekler; bitti ölçütü PlayMode testinde ilk dalga geldikten sonra bir tur makineler tahtadayken gerçek EMRİ İLET'le oynanır (makine evresi işler, ekip ayakta, savunma sürer) | Yardımcıyla karşılaştırma yalnız bağı sınar (P4b'nin BEDEL değişikliğinde yeşil kaldı); dalga çatışması PlayMode'da hiç oynanmıyordu |
| 117 | Kapanış: tasarım lafzından sapmalar (inceleme 8) | 47 (D7) ve 7 (K7 yolu) belgeye "onaya açık" diye yazıldı; kod değişmedi | Bilinçli sapmalar |
| 118 | Kapanış: belge çelişkileri (inceleme 3) | §11.4'ün savunma tuzu günlük dizini (12), §11.1'in röle canı adı (34), §11.2'nin kuşatma durumu günlük (4), §11.3'ün sonuç dizgisi (28) düzeltildi; AGENTS kural 4'e `DefendSector`/`YieldSector` | Belge kodu anlatsın |
| 119 | Kapanış: Unity doğrulaması (inceleme 9) | Parça P2 sonu ve P4a–P4b'nin Unity doğrulaması batchmode kopyadaydı; kapanışta editör yanıt verdi: editörde boş sahnede derleme temiz, tam EditMode 2129 testten 2128 geçti (tek kalan süre eşikli `FacilityMapsTests.Generation_IsFastEnough`, 1777 ms / 1500; üretici değişmedi, tek başına geçti), tam PlayMode 127/127 (batchmode'un 16 ortam kalanı dahil). Dyn SDF'ler testten önceki hâline geri kondu (yedek font aşama 9'dan beri değişik, dokunulmadı) | Editörün açık sahnesi boş ve temizdi; kaydedilmedi |

**Açık sorular (varsayılanla ilerlendi, onaya açık).**

1. **(S1) Saldırı baskınında savunma kancası** (aşama 6 kaldıraç 2: kazanılan baskında hedef ve dönüş odasında bırakılan gövde çıkarılsın
   mı)? Varsayılanla ilerlendi: aşama 11'e alınmadı (35). Kâğıt üstü üst sınır (aşama 10 `t10donus`): yalnız hedef odasında −3,05, dönüş
   odalarıyla −5,45 ölü / harekât (11,7 → ~6,3, ölüm hedefi ≤ 7,5'in altı); aşama 11'de ölü / harekât 13,2 (`t11savun`). İstenirse tek
   ölçüm anahtarıyla (`facilityHeldExtract`) ölçülür.
2. **(S2) "Bırak baskın hakkını korur" kuralı pratikte işlemiyor:** düşen kanat çoğunlukla mevcut bölgededir ve bırakılan vardiyada o
   kanada baskın da yasaktır; bot oyununda bırakmaların %37'sinde o vardiya baskın verilemedi (`nrYield` 0,55 / 1,50 bırakma). Kural
   belgedeki gibi uygulandı; değiştirmek (ör. bırakılan kanada aynı vardiya baskın serbest) `LiberationRaidCheck`'te tek satır ve
   `t11birak` yeniden ölçülür.
3. **(S3) Hedef bandı dışında savunma ayarı** (yalnız yeni alanlar: dalga büyüklüğü, sonra süre): yetki kullanıldı, tarama hiçbir
   değerle bandı tutturmadı; hiçbir varsayılan değişmedi (38, 78).
4. **(S4) Kapı kuralının genişletilmesi** (17): savunmada kısmen kapıda bekleyen uzman tur sonunda tahtaya döner ve son turda içeri
   çekilinmez (bir uzmanın 10 tur kapıda güvende kalması ve son turda kapıya saklanma kapanır). Uygulandı; onay bekliyor.
5. **(Hedef) Savunma başarısı %40–60 tutmadı** (oynanan savunmanın %100'ü kazanıldı; bot tutma politikasında makineleri girişte yığıp
   düşürür, tutulacak bir hedef olmadığı için niyetlerden serbestçe kaçar). Varsayılanla ilerlendi: aşama 11 dengeyi ve kuralı
   değiştirmedi, yalnız ölçtü ve raporladı (111). Seçenekler (78): (a) **sızma**: ekibin odasının ileri geçidine varan makine içeri
   sızar (hedef odasında röleye vurur) ya da sızan makine başına bedel; (b) dalganın girişi tek kenara yığılmasın (batı + menfez); (c)
   savunmada pusu hattı sınırı; (d) hedefi kuşatma başına tanımla: karar verilen kuşatmaların %44–47'sinde kanat tutuluyor (savunulamayan
   bırakılıyor; §11.6 okuma 4), bu okuma bandın içindedir ama zorluğu savunma değil kadro yokluğu verir; (e) olduğu gibi kalsın. Her
   kural seçeneği tek ölçüm anahtarıyla `t11savun` ve `t11cekil` 80 tohumla ölçülmeli.

Aşama 11'in açık bıraktıkları (geliştiriciye bildirilecek):

- **S1–S4 ve hedef** yukarıda; hepsi varsayılanla uygulandı.
- **Savunmanın amacı** (hedef, 78, 111): kural düzeyinde karar gerekir.
- **Savunma reddi:** `SAVUN=hep`'te kuşatmaların yarısından fazlası kadro yokken gelir (red gerekçesi hep "üç sağlıklı kişi"); insan
  oyuncuda sıklığı farklı olabilir.
- **İzlerin ölüme etkisi** (I9: ölü / baskın +0,034 ±0,015) incelenmedi; aşama 11'in toplamı ölü / harekât +1,48 ±0,52 (`t11savun −
  t10donus`), ölüm hedefi (≤ 7,5) uzaklaştı (13,2).
- **I11 süre:** `t11savun` etiketi 836 sn (1,35 × `t10donus`; aynı koşuda `t11K0` 700 sn); boş makinede yinelenmedi.
- **Sunum cilası:** içeri çekilme ve son tur onay notu var olan "ölümcül" boyutta (480 × 300) açılır, tek satırlık metne göre boş
  görünür (saldırının SON TUR notuyla aynı davranış). Kartsız + tesis oyunu pratikte oynanmıyor (kapının kuşatması 114 ile kapandı).
- **`seenLearned`** (on üçüncü tur 87): savunma bölge tamamlamadığı için öğretmez; reveal'siz öğrenme yolu açılmadı (not kapandı).
- **Elle oynama:** yapılmadı; ana bayrak asset'te kapalı, oyuncu yolu yok (§15).


**Kararlar (20 Kasım 2026, on beşinci tur: aşama 12 Katar ve Yalpa).** Geliştirici uyurken, onun "karar gerekirse önerinle yap"
talimatıyla verildi: hepsi önerilen varsayılandır, geliştiricinin onayına açıktır. Uygulama tasarımının dört açık sorusu (S1–S4)
varsayılanlarıyla kararlaştırıldı (aşağıda "Açık sorular"). 1–48 aşama 12 uygulama tasarımının kararlarıdır (K1–K48; uygulanan hâle göre
düzeltilmiş); 49–59 parça P1'in (Core tahta ve zincir: D1–D11), 60–69 P2'nin (Core simülasyon, adlar, kayıt: E1–E10), 70–84 P3'ün (bot ve
ölçüm: F1–F15), 85–93 P4a'nın (sunum, masa: G1–G9), 94–111 P4b'nin (sunum, baskın sahnesi: H1–H18) sapma ve ek kararları; 112–120
kapanışın (bağımsız incelemenin bulguları ve Unity doğrulaması). Karşılıkları §10 "Uygulanan hâl (aşama 12)"; ölçüm §11.6 "Aşama 12
ölçümü".

| # | Konu | Karar | Gerekçe |
|---|---|---|---|
| 1 | Kayıt (K1) | Sürüm 19; iz f5 → f6 (`GeneratorVersion` 6); Digest değişir | Aşama 6 D20, 8 K1, 10, 11 emsali; etkilenen yalnız tesis kayıtları |
| 2 | Alt bayraklar (K2) | `facilityKatar`, `facilityYalpa` (C# `true`, asset'e yazılmaz); kapalıyken na/oc tesiste asansör ve yağ, oda üretimi dahil | `t12K0` = `t11savun` kilidi; iki bayrakla Yalpa'nın ilk baskını ~50 çiftle eşlenir (`t12yalpa`); ölçüm kötü çıkarsa geri dönüş anahtarı |
| 3 | Eşleme (K3) | `RegionDef.FacilityEvent` ve tek okuyucu `FacilityMaps.EventFor`; plan, emir ve sunum okur | Oda tahtası emrin olayı ile odanın olayının eşitliğini ister; eski tahta değişmez |
| 4 | Plan önbelleği (K4) | Anahtar (tohum, katar, yalpa); `For`'un varsayılanları `true` | Testler ve aynı süreçte bayrak değişimi |
| 5 | Şart parçası (K5) | Katar ve Yalpa parça üretmez; kenar geometriden, yatış takvimden; pafta olaydan çizer | Fiziksel olmayan kenar parçası 16 kareyi başlangıç yeri olmaktan çıkarırdı |
| 6 | Katar'da ileri geçit (K6) | G ve A odalarında yalnız doğu sütunu; köşe çiftleri de dışarıda (49) | §10.1 "geçit vagonlar arası bağlantı"; açık kenardaki geçit karesi kural ve kurgu çelişkisi |
| 7 | Adlar (K7) | Bina ve kanat adları statik değişir; statik not iki hâle uyar; şartın notu `FacilityNote` (`Simulation.RegionNote`). **Onaya açık (S1)** | Belge adları bayrağa bağlamaz; yayındaki oyunda "Sevkiyat katarı" asansörle tutarlı |
| 8 | Olay tablosu (K8) | `RegionEvents` 12 + 2; `RaidEventTests` kilidi güncellendi | Olay altyapısı ve sunumun anahtarlı okumaları tek sözlüğe bağlı |
| 9 | Kenarın girişi (K9) | Yalnız `Push`; cıvata kenardan önce; çekme tahtadan çıkaramaz | Aşama 10 cıvatası ve aşama 13 Tamponu kendiliğinden kapsanır; §10.1'in "çekilen"i bugün boş (§10.1 "Uygulanan hâlin tablodan iki farkı") |
| 10 | Kenardan düşenin temsili (K10) | `Dead` + `Overboard`, `Out` değil; `IsBody` `!Overboard` | Ayakta ve tahta yüklemleri, bot ve Camp8 sayımları değişmeden doğru; `Out` "tahliye" sayılırdı |
| 11 | Düşen makine, köpek, tutsak, aygıt (K11) | Makine ve tutsak ortak ölüm yolundan, `fall` Fx'iyle, hurdasız; köpek düşer ve dinlenir; aygıt yok olur | Köpek ölmez (aşama 8 K14); ortak ölüm yolu hedef ve bağ sayaçlarını korur |
| 12 | Taşıyıcı (K12) | Yük son karede (`DropCargo` ortak) | §10.1 lafzı |
| 13 | Rapor ve sonuç (K13) | Kişi durumu `"k"`; sonraki odaya konmaz; `RaidResult.Overboard`; `IsExtracted` değişmez | Zincir bilgiyi yalnız rapordan taşır |
| 14 | Yatış (K14) | Düz `facilityFallInjuryTurns` (en az 1); beceri indirimi, Revir ve deneyim yok | Belge "5 vardiya"; Revir "taşınan düşen" içindir |
| 15 | Takibin ölçütü (K15) | Batı geçidine yürüme uzaklığı ≤ `facilityFollowRange` (2); yalnız sağ makineler | Manhattan duvar arkasını takip ettirirdi; sayı `Balance`'ta |
| 16 | Takibin kapsamı (K16) | Yalnız dönüşte (kaçış dahil), terk edilen oda Katar ise; kendiliğinden geçişte de; savunmada yok | §10.1 "dönüşte" |
| 17 | Takipçi yerleşimi (K17) | Hemen, kadrodan sonra; doğu geçidi, sonra ondan yürüme uzaklığı en küçük boş zemin; `MaxEnemies` dolarsa kalan gelmez; aşama 11 ile ortak `EntryTile` | §10.1 lafzı; savunma bayt aynı |
| 18 | Takibin taşınması (K18) | Raporun `Follow`'u; `Next` takipte odayı oynatır; `Setup.Followers` | `Next` planı bilmez; yeni komut yok |
| 19 | AYIR koşulu (K19) | `CanPass && Escape && Katar` ve Teknisyen kapıda, `!Acted`; geri alınmaz; `DecoupleBlock` nedeni | "Yeteneği harcanmış sayılır"ın tek okunabilir hâli ön koşul (GEÇ odayı bitirir); §10.1'in "Teknisyen ayaktaysa"sından dar |
| 20 | AYIR'ın bedeli (K20) | Yok | §13 "ölçülür; gerekirse bedel konur"; ölçüm I6 bedel gerektirmedi |
| 21 | Savunmada şartlar (K21) | Katar (kenar dahil) ve Yalpa savunma tahtasında kapalı. **Onaya açık (S2)** | Savunma olay durumu kurmaz (aşama 11 K18); belge sessiz |
| 22 | Yalpa takvimi (K22) | Sabit 2/4/6, batı/doğu/batı, `Turn < MaxTurns`; dönüş odası 1'den | §10.2 ve §2.4 |
| 23 | Kayma (K23) | Birimler `Push`'un kayma kipiyle, yokuş aşağıdan; cıvatalı sessiz tutar; hurda boş zemin ve bağ kuralıyla; gövde ve yer yükü hasarsız, kapı ve menfeze girmez | §10.2 kayanlar listesi hurdayı sayar; kapıdaki gövde aşama 6'da yanıltıcı bulundu |
| 24 | Yalpa Fx'i (K24) | Tur başına `ev`/`yalpa`; kayan her şey için `slide`; `push` yazılmaz | `push` Fx'i sunumda planlanmış kareye kaydırırdı; bot itme sayımı karışmaz |
| 25 | Pusu ve tutuş (K25) | Bugünkü itilen birim davranışı | Belge sessiz; emsal tutarlı |
| 26 | Makine aklı (K26) | Yalpa'da yatış turunda yokuş aşağısı kapalı adaya −0,5; Katar terimi yok | `yag` −0,5 emsali; kenardan kaçınan makine oyuncunun aracını önceden okurdu |
| 27 | Hazards (K27) | Katar `brink`; Yalpa `Turns` okları, `slide` hatları, `tilt`/`tilt_next`, "YALPA {n}" | "Önce işaret, sonra olay"; kural kopyası yok |
| 28 | Okuyucular (K28) | Tasarımın §5 tablosu ve §6.1; kapanışta `FacilityRoomTurns`, `FollowedInIds` (113) | Sunum ve bot kural kopyası taşımasın |
| 29 | Balance (K29) | Dört alan; asset'e yazılmaz | Aşama 8, 10, 11 emsali |
| 30 | İmza (K30) | Birimde `K`, tahtada `\|A`, yalnız varsa | Eski imzalar değişmez |
| 31 | Dizgiler (K31) | Oda raporu `…\|Tally\|Follow\|Decouple`; `RaidResult` `…\|Extracted\|Overboard` | Yeni alan yalnız sona |
| 32 | Rapor denetimi (K32) | `KatarReportCheck` (§10 "Uygulanan hâl (aşama 12)") | Yapısal denetim ilkesi (aşama 2) |
| 33 | `AYIR` politikası (K33) | `hep` (varsayılan), `hic`, `takip` | Oyuncu ve takibin üst sınırı |
| 34 | `KENAR` politikası (K34) | `hic` (345/600), `kacis` (0,3 × düşme) | Terimsiz bot kenarı rastlantıyla seçerdi; riskin alt ve üst sınırı |
| 35 | Ölçüm planı (K35) | Beş etiket, eşli ilk baskın (`--ilk`), önek kilidi; `PROBE` yok | Örneklem küçük; ilk baskın aynı durumdan oynanır |
| 36 | Hedef (K36) | Hedef satırı yok; okuma ölçütleri önceden; aşama 12 dengeyi değiştirmez | §12 satır 12 ölçüm hedefi koymaz |
| 37 | AYIR girişi (K37) | Q ve Teknisyen kartı; EMRİ İLET "AYIR"; sahne ve prefab değişmez | Herkes kapıdayken Q boştadır |
| 38 | AYIR onayı (K38) | Yok | AYIR ile GEÇ'in geride bıraktığı aynı; onay akışı yavaşlatır |
| 39 | Takipçi gösterimi (K39) | Etiket yalnız `CanPass` iken; hover her zaman | Erken etiket tahtayı kalabalıklaştırır |
| 40 | Yatış canlandırması (K40) | Tahta kökü ~2,5° eğilme ve toplu kayma, ≤ 0,6 s | Okunur ve kısa |
| 41 | Teleks (K41) | Kenar düşüşü "· {n} düştü" içinde; ayrıntı raporda | Teleks satırı kısa kalsın |
| 42 | Antet (K42) | "PAFTA n/3" kalır; vagon sözü oda ve kanat adında | Kalıp her tesiste aynı |
| 43 | Kit ve model (K43) | `Kit_na`, `Kit_oc` kalır; yeni model ve kuplör yok. **Onaya açık (S4)** | Rölyef three.js hattı ister (node yok); kuplör kapı sözleşmesini değiştirirdi |
| 44 | Renkler (K44) | `RaidPieceLibrary.hazardBrink` #A9520A, `hazardTilt` #2C6E73 (asset + C# yedeği) | Kural 7 |
| 45 | Ses (K45) | Yeni ses yok; var olan `Cue`'lar (`EvShelfRoll`, `EvArmClank`, `RadioReport`) | SoundBank değişmez |
| 46 | Lokalizasyon (K46) | `strings.txt` elle; `UpdateTable` çalıştırılmaz; eski adlar kullanılmayanlar bloğuna | Aşama 9–11 emsali (LF) |
| 47 | Şerit glifi (K47) | `←`/`→`; CP-Bold SDF'de var, Core değişikliği gerekmedi | Ekran görüntüsünde doğrulandı |
| 48 | Fiş taşması (K48) | Ölçüldü: EN beş satırlı HALCYON fişi −18,8 px (aşama 10 tabanı ~−19); EN bina adı kısaltılmadı | Aşama 10 madde 75 açık kalıyor |
| 49 | P1 (D1): köşe çiftleri | Doğu sütununun (7,0)–(7,1) ve (7,6)–(7,7) çiftleri de dışarıda; ortalama deneme 11,93 → 12,82, yedek tür 4 → 5; tükenme yok | K6'nın lafzıyla 1200 odanın 252'sinde geçidin bir karesi açık kenarda kalıyordu |
| 50 | P1 (D2): `Upcoming` | Yalpa için ok (`"←"`/`"→"`), "YALPA ←" değil | `HazardView.Turns` `Upcoming`'den dolar; PlayMode ölçütü (`TurnEvents[2] == "←"`) belirleyici |
| 51 | P1 (D3): "YALPA {n}" etiketi | Yokuş aşağı kenarın orta karesindeki `HazardMark{Kind = "tilt"/"tilt_next"}`'te | `HazardEdge`'in etiketi yok |
| 52 | P1 (D4): kaymayan nesne | Hurda, gövde ve yük yalnız kaydıysa `slide` yazar; N 0 yalnız çarpan birimde | Hat yalnız kayanda |
| 53 | P1 (D5): test yardımcıları | `FacilityKit.Params`, `FacilityRoomTests.Params` şartı `EventFor(def, true, true)`'dan; `FacilityCountermeasureTests`'te moloz yönü | Gerçek emrin karşılığı; iddialar değişmedi |
| 54 | P1 (D6): `DecoupleBlock` sırası | `rule` → `tek` → `gate` → `acted`; `CanDecouple = CanPass && DecoupleBlock == null` | Neden tek değerle okunur |
| 55 | P1 (D7): takipçi yerleşimi `ReturnBoard`'un sonunda | Kadro, köpek/tutsak, taşınan ve yatan gövdelerden sonra | Öbür birimlerin kimlik sırası değişmez |
| 56 | P1 (D8): `StepsFrom` | `Baskin.Events.cs`'te `ExitSteps`'in yanında; `EntryTile` `Baskin.Defense.cs`'te | `ExitSteps` ona sarılır |
| 57 | P1 (D9): test kancaları | `Baskin.TestEventStep`, `TestEventPos` (internal) | `TestHurt` emsali |
| 58 | P1 (D10): telsiz türleri | Kenardan düşen ekip üyesi "bad", makine "good", köpek "bad", tutsak/aygıt "sys"; yatış "sys"; AYIR "good" | Tonun anlamı oyuncuya göre |
| 59 | P1 (D11): `FollowerIds` ve savunma | `B.Defense`'e de bakar | Savunma tahtası her iki yoldan kapalı |
| 60 | P2 (E1): savunmada Katar alanları | `KatarReportCheck` savunmada `Follow`, `Decouple`, `"k"`yi reddeder | K21'in yapısal karşılığı |
| 61 | P2 (E2): takip dizinleri | Çift ve negatif dizin reddedilir | `Setup.Followers` aynı makineyi iki kez kurmasın |
| 62 | P2 (E3): `"k"` denetimi | Kişinin düştüğü odaya (`Fell`) bakar, raporun odasına değil | Kenardan düşen sonraki raporlara aynen taşınır |
| 63 | P2 (E4): okuyucuların kenar durumları | Bilinmeyen bölgede null; `RegionNote` `FacilityNote`'u yalnız oynanan şart bugünkü olaydan farklıysa verir | Tasarımın §6.1 lafzı |
| 64 | P2 (E5): ret metinleri | Dört yeni metin TR + EN | Rapor denetiminin nedeni okunur |
| 65 | P2 (E6): `SquadOutcome.Overboard` | Bool; `Down` da true; plaka, kelepçe, köpek kuralları ortak | "Kurtarıldı" satırından ayrılır |
| 66 | P2 (E7): kaldırılan na notu | Güncel not ("Bölgenin en büyük dağıtım merkezlerinden biri…") kullanılmayanlar bloğunda | İki eski not da blokta |
| 67 | P2 (E8): test sürücüsü | `FacilityConditionCampaignTests.KatarRaid`/`Order` (internal); tahta kurulumu yardımcılarla, kayda yalnız komut | `WinRaid` emsali |
| 68 | P2 (E9): `SimAt`'in yönü | Hedef sınırda değilse en az bağlantıyla yaklaşan sınır bölgesi (BFS) | Tohum 79'da na 1, oc 3 bölgeden sonra |
| 69 | P2 (E10): `strings.txt` yerleşimi | Yeni `# …/Core/Baskin.Conditions.cs` başlığı; öbürleri kullanıldığı dosyanın başlığında | `UpdateTable` sonradan aynı içeriği dizer |
| 70 | P3 (F1): AYIR'ın kaçma nedenleri | `tek/gate/acted`'a ek `pol` ve `auto` | Fırsat = AYIR + bütün nedenler |
| 71 | P3 (F2): kenar sayaçları | `EdgeBy[kaynak]` yalnız makine; uzman `EdgeTeam`, öbürleri `EdgeOther` | I7 makinenin kaynağını sorar |
| 72 | P3 (F3): ek anahtarlar | R `edgeX`, `eDied`, `eInj`; C `eDied`; S `edgeM edgeP fol tilt` | Yapı denetimleri |
| 73 | P3 (F4): `TiltHurtP/E` | Yatışta çarpan birim sayısı, hasar değil; hasar `evP/evE` | Tavan yüzünden çarpma = 1 hasar |
| 74 | P3 (F5): `fol` | Raporlara yazılan takipçi; `MaxEnemies` dolarsa giren daha az olabilir | Raporlardan okunur |
| 75 | P3 (F6): `kacis` ağırlığı | `Down × 3 / 10` | `BOTW` 13,5 ile bayt aynı |
| 76 | P3 (F7): `Bot_Kenar_Hic` | Önce ödüllü kenarla (`EdgeFall` −2000) adayın varlığı | "hic atmaz" boş geçmesin |
| 77 | P3 (F8): `kacis`'in kapsamı | Sağlam ama uzak/geç kalan üyeyi de atar (gidişte); I5 "üst sınır" | Puanın yol terimi |
| 78 | P3 (F9): `Bot_Katar_PushesMachineOff` | Test yalnız adayın görüldüğünü sınar | Düz gidişte bot ilerlemeyi seçer (doğru puan) |
| 79 | P3 (F10): `Bot_FullCampaign` | Yeni Explicit test yok | Camp8 REPLAY 60 koşu ve determinizm kapsar |
| 80 | P3 (F11): I7 kenar payı | Ölçülmedi | R satırında makine kaybı sayısı yok |
| 81 | P3 (F12): `ozet.py --ilk` | Öneki aşama 12 anahtarlarını atarak karşılaştırır; varsayılan döküm basılmaz | Politika etiketlerinde öneki bozmasın |
| 82 | P3 (F13): `hic` ağırlığı | Değişmedi (345 < 350 bulgusu yazıldı) | Ölçüm tasarımın ağırlığıyla |
| 83 | P3 (F14): `--tesis` dökümü | Değişmedi | Bugünkü davranış |
| 84 | P3 (F15): tarama | `t12sw01/03/06` `tesis12.txt`'e girmedi (ölçüm betiğinde, `BOTW`) | Ana ölçümden ayrı |
| 85 | P4a (G1): tırnaklar | Çizim alanının içinde, kenara bakan ok başı | Dışarı taşan çizgi başlığa ve antete binerdi |
| 86 | P4a (G2): kuşatma fişinin notu | `BuildingNote` | Savunma şart kurmaz (S2) |
| 87 | P4a (G3): duvar karesi | Açık kenar satırındaki duvarda kesik yok; çerçeve açık kenarın hiçbir karesinde çizilmez | Core'un `brink` kenarları da duvarsız kareler |
| 88 | P4a (G4): rapordaki yatış | Önce kayıttan ikinci yeniden oynatmayla okunuyordu; kapanışta `FallShifts(sim.B)` (114) | Kişinin bugünkü `Down`'ı her vardiya azalır |
| 89 | P4a (G5): `RoomTurns` | Kapanışta Core okuyucusuna taşındı (113) | Sunum kural kopyası taşımasın |
| 90 | P4a (G6): `Identity`'ye isteğe bağlı `sim` | Simülasyonsuz çağıran bugünkü olayı görür | `EnglishCardBuildersTests` |
| 91 | P4a (G7): EN "laid up" | Tasarımın metni; var olan "kurtarıldı" satırı "bedridden" der; birleştirilmedi (119) | Core satırı da "laid up" |
| 92 | P4a (G8): takvim ayırıcısı | ", " | Okunur |
| 93 | P4a (G9): paftanın hover'ı | "Olay: Katar" kalır, kural satırı arkasında | Bugünkü kalıp |
| 94 | P4b (H1): EMRİ İLET notları | AYIR "takip yok", takip "{0} makine takip eder" / EN "{0} chasing" | Not yuvası kısa; uzun not etikete biniyordu |
| 95 | P4b (H2): Teknisyen'in kartı | Tıklanabilir (`RaidTeamModel.Detach`); prefab değişmedi | Kapıdaki kartın düğmesi kapalıydı |
| 96 | P4b (H3): takipçi hover'ı | Menzil dengeden (`B.Moves.FollowRange`) | Kural 7 |
| 97 | P4b (H4): gözcünün takip satırı | Önce "tahtaya en son eklenen n makine"; kapanışta `Baskin.FollowedInIds` (113) | Makine sınırı takipçiyi kestiyse donmuş odanın makinesi sayılıyordu |
| 98 | P4b (H5): kapanış satırının paftası | "Takip: … PAFTA {1}" gidilen pafta; "Vagon ayrıldı: PAFTA {0}" ayrılan vagon | Okunur |
| 99 | P4b (H6): Yalpa hover'ı | "Yatışta çarpar · −1"; `HazardRow` `tilt`/`tilt_next` | Çarpan birim kaymaz; etiket karesi boş kalıyordu |
| 100 | P4b (H7): koşu darbesinin kartı | Katar'da ikinci düğme "kenardan düşerse yok olur" | Kenar karesinde doğru etki |
| 101 | P4b (H8): hayalet ve kâğıt | Hayalet kenarın 0,8 dışında; "DÜŞER" var olan önizleme kâğıdıyla | Yeni havuz anahtarı yok |
| 102 | P4b (H9): antetin notu | `RegionNote`; savunmada `BuildingNote` | G2 ile tutarlı |
| 103 | P4b (H10): telsiz fişinde düşme önce | Hamle birim düşürürse fiş düşmeyi yazar | Önemli olan önce |
| 104 | P4b (H11): `decouple` Fx dalı yok | Kol sesi `PassRoutine`'de | AYIR oyuncu evresindedir |
| 105 | P4b (H12): eğilme | `PageRoots` dünya Z ekseni çevresinde; ≈ 0,57 sn | Tahtanın bütünü eğilir |
| 106 | P4b (H13): PlayMode akışı | AYIR hedefin kaçış geçidinde, takip ara vagonun dönüşünde; gidişte A'da bir makine bırakılır | Dönüş vagonu temiz sayılmasın |
| 107 | P4b (H14): tohum | 79 | `Reach` birkaç saniye |
| 108 | P4b (H15): Yalpa testinin kurulumu | Batısı boş zemin olan açık makine; EMP uzmanı gövde için yatırılıp kaldırılır | Tohum 79'da (4,2)'nin batısı kepenk |
| 109 | P4b (H16): sonuç kartının yeri | "HALCYON · 01 Kumanya güvertesi" (bugünkü biçim) | Bina adı antette doğrulandı |
| 110 | P4b (H17): EN telsiz fişleri | Fiş yazıldığı dilde kalır (bütün fişler; değişmedi, 117) | Bugünkü davranış |
| 111 | P4b (H18): Unity | Tam EditMode ve PlayMode batchmode kopyada (editör yanıtsız kaldı) | Ekran kilidi |
| 112 | Kapanış: EN terim (inceleme 1) | Üç yeni EN satırında "PASS" → "GO THROUGH", "gate" → "door" (durum satırı, takipçi hover'ı, kapı fişi); `FacilityConditionPresentationTests` üçünü sabit EN metinle bekler | GEÇ'in yerleşik karşılığı "GO THROUGH", kapı "door"; EN oyuncu aynı eylemi iki adla görüyordu |
| 113 | Kapanış: Core okuyucuları (inceleme 2, G5, H4) | `Simulation.FacilityRoomTurns(room)` (`RaidChain.Setup` + yeni `Baskin.FirstVisitTurns`; `RoomBoard` aynı kuralı okur) ve `Baskin.FollowedInIds(B)` (yeni `Unit.Follower`, `ReturnBoard` koyar); `FacilityConditionText.RoomTurns` ve `SpotterFollow` bunları okur. Testler: `FacilityConditionCampaignTests.FacilityRoomTurns_EqualsTheOpenedBoard` (na ve oc, üç oda türü, Eşik, denge değişince; okuyucu = açılan tahtanın `MaxTurns`'ü), `FacilityTrainTests` 10'a okuyucu iddiaları (makine sınırı takipçiyi kesince yalnız gelen sayılır) | Sunum kuralı kopyalıyordu, test aynı aritmetikle kuruyordu; davranış aynı (bayt kilitleri ve REPLAY) |
| 114 | Kapanış: ikinci yeniden oynatma (inceleme 3) | `RaidPaperFacts.AfterResult` silindi; rapor `FallShifts(sim.B)` okur | Okunan değer her zaman `max(1, facilityFallInjuryTurns)`; denge kayıtla gelir |
| 115 | Kapanış: belge (inceleme 4) | §10.1'e iki lafız farkı (çekme düşürmez; AYIR için Teknisyen kapıda ve yeteneği harcanmamış), §10 "Uygulanan hâl (aşama 12)", §11.1–11.6, §12, bu tur, §15; DURUM, MIMARI, GELISTIRME, OYUN-TASARIMI, AGENTS ("aşama 0–12"); BOLGE-OLAYLARI ve BASKIN-MEKANLARI'nda na/oc adları | Belge kodu anlatsın |
| 116 | Kapanış: test gücü (inceleme 5) | `FacilityConditionTextTests` takvimi sabit metin ve sabit adımlarla bekler (aynı ifadeyle yeniden kurmaz). Bot testindeki "`Follow` = `Followers`" bot sayacının raporla uyumunu sınar; takip kuralı `FacilityTrainTests` 10'da somut karelerle | Totoloji kalktı |
| 117 | Kapanış: EN telsiz fişleri (inceleme 6, H17) | Değişmedi; açık kalanlara yazıldı | Fiş yazıldığı anda `Loc` ile çevrilir; EN başlayan oyunda fişler İngilizcedir. Türkçe fiş yalnız baskının ortasında dil değişince kalır (ekran görüntüsü betiği dili ortada değiştirdi); bütün fişlerin bugünkü davranışı, aşama 12'ye özgü değil |
| 118 | Kapanış: Yalpa'nın yönü (inceleme 8) | §11.6 okuma 5: bölge düzeyi dört etikette 6–9 puan düşük, eşli ölçüm ters işaretli ve geniş; tetik tutmadı, denge değişmedi | Yönün doğrulanması daha çok eşli çift ister |
| 119 | Kapanış: EN yatış terimi (G7) | "laid up" (aşama 12) ile var olan "bedridden" (kurtarılan) birleştirilmedi | Ayrı durumlar; istenirse tek satırda |
| 120 | Kapanış: Unity doğrulaması | Editör yanıt verdi; **editörde, boş sahnede** derleme temiz (`scriptCompilationFailed` false), tam EditMode **2213/2213** (271 sn; `FacilityConditionBotTests` 26/26 dahil; 5 `Explicit` koşulmadı), tam PlayMode **129/129** (921 sn; batchmode'un 16 ortam kalanı ve `FacilityConditionPlayTests` 2/2 dahil); Mono 1107 / 3 bilinen taklit / 5 atlanan. Dyn SDF ve yedek font testten önceki hâline geri kondu | Editörün açık sahnesi boş ve temizdi; kaydedilmedi |

**Açık sorular (varsayılanla ilerlendi, onaya açık).**

1. **(S1) Tesis adları bayraktan bağımsız mı?** Varsayılanla: evet (belgedeki gibi statik). Yayındaki oyunda (ana bayrak kapalı) WAYPOINT
   binası "Sevkiyat katarı", HALCYON binası "Yüzer maden platformu" olur; olaylar yük asansörü ve yağ kalır, statik notlar buna göre
   yeniden yazıldı (7). Alternatif: adlar da şart gibi bayrağa bağlanır (belgeden sapma).
2. **(S2) Savunma baskınında Katar'ın açık kenarı ve Yalpa'nın yatışı** kapalı (21). Açılırsa bot dalgayı kenardan iter, savunma daha da
   kolaylaşır (aşama 11'de zaten %100).
3. **(S3, ölçümden sonra) Kaçış kapısının ve AYIR'ın bedeli.** Önceden yazılan okuma ölçütleri bot için tutmadı (§11.6 I5, I6): bedel
   konmadı, denge değişmedi. İnsan oyuncu için gidişte "yavaş üyeyi kenardan at, odayı kaybetme" yolu açıktır (bedeli 5 vardiya yatış);
   istenirse seçenekler: yatışı uzatmak, düşenin ekipmanını kaybettirmek, Katar'da EMP'nin dost itmesini kaldırmak; AYIR için Teknisyen'in
   sonraki odada yeteneğinin olmaması ya da kapıdaki yükün bırakılması. Karar geliştiricinin.
4. **(S4) Tren ve platform için yeni rölyef kitleri ya da vagonlar arası kuplör modeli.** Varsayılanla: aşama var olan `Kit_na` ve
   `Kit_oc` ile, yeni model olmadan kapandı (43). Rölyef three.js hattı ister (node yok).

Aşama 12'nin açık bıraktıkları (geliştiriciye bildirilecek):

- **S1–S4** yukarıda; hepsi varsayılanla uygulandı.
- **Kaçış kapısı insan elinde:** bot kendi üyesini yalnız gidişte (A) atar; insan oyuncunun bu yolu ne sıklıkla kullanacağı ölçülmedi (§15).
- **Botun `hic` ağırlığı** `GateLate + GateMiss`'ten küçük (§11.6 okuma 3); sömürüsüz alt sınır istenirse `EdgeFall` ≥ 400 ile `t12`
  yeniden koşulur.
- **Kenarın makine kayıplarındaki payı** (80) ve `itme` önleminin karşı ağırlığı: WAYPOINT'te `itme` hiç öğrenilmedi; aşama 13 (Tampon)
  ile birlikte ölçülmeli.
- **Eşli ölçümün gücü:** Katar 94, Yalpa 50 çift (2 SE sınırı ±7p / ±11p). Yalpa'nın bölge düzeyindeki 6–9 puanlık düşüşü eşli ölçümde
  görünmedi (ters işaretli, geniş); oc'yi erkene alan kip ya da `PROBE` (şart baskınında kopyadan eşli politika) eklenmedi.
- **AYIR'ın kaçışı:** kendiliğinden geçişte (%13) ve Teknisyen ayakta değilken (%11) AYIR verilemez; insan oyuncuda sıklığı ölçülmedi.
- **EN telsiz fişleri** baskının ortasında dil değişince eski dilde kalır (117; bütün fişler).
- **Fiş taşması:** EN beş satırlı fişte BASKIN EMRİ VER ~19 px taşar (aşama 10 madde 75; aşama 12'de kötüleşmedi).
- **Oda adları:** "Otopark katı" gibi kanat odası adları bir vagonda görünebilir; kurguya uyan oda adları aşama 13'ün ad havuzu işine.
- **Tampon ve çapa** (aşama 13): kenar ve yatışla birlikte ölçülmeleri açık; aşama 12 kuralı onları kendiliğinden kapsar (`Push`,
  `Bolted`); çapa "kaymaz" satırı aşama 13'te `EndTilt`'in sabitler listesine girer.
- **Elle oynama:** yapılmadı; ana bayrak asset'te kapalı, oyuncu yolu yok (§15).

### Aşama 13 · on altıncı tur (20 Kasım 2026)

| Karar | Uygulanan | Gerekçe |
|---|---|---|
| K1 · İmza eşlemesi ve ölçüm | `facilitySignatureMachines` ve §6.5 varsayılanları. **Onaya açık** | İki makine dağılımı aynı g5 haritasında karşılaştırılabilir |
| K2 · Hat ve engel | İtme ve çekmede ortak çarpışma ve cıvata, kurulan parçada ortak yol denetimi. **Onaya açık** | Yeni mekanik mevcut tahta ve önlemlerle aynı tepkiyi verir |
| K3 · Oda adları ve kurgular | Katar ve Yalpa paftalarına mekâna özgü adlar, altı makineye var olan örgülerden ayrı prefablar. **Onaya açık** | Pafta ve makine kimliği baskın sırasında ayırt edilir |
| K4 · Harita ve kayıt | g5, f7, sürüm 20. Eski sürüm süzgeci ve yeni g5 tabanı | Kayıttan yeniden oynatma aynı paket yerleşimini gerektirir |
| K5 · Kelepçe | Yeni imza tutsakları mevcut yürüyüşü kullanır; özel makine niyetleri YZ evresinde yürür. **Onaya açık** | Oyuncuya altı yeni eylem eklemek ekipman tasarımı ve ayrı denge sınaması gerektirir |

**Aşama 13'ün açık bıraktıkları.** Makine ayarları için §11.6'daki g5 ölçümü ve tek alan taramaları karar girdisidir.
Savunmanın hedef aralığı ve önceki aşamaların onay bekleyen kararları geliştiricide kalır. Aşama 14 unvanları ayrı iştir.

### Aşama 14 · on yedinci tur (20 Kasım 2026)

| Karar | Uygulanan | Gerekçe |
|---|---|---|
| K1 · Kurtarmanın sahibi | Gövdeyi gerçekten hareket ettiren ve sağ dönen katkıcılar ilerleme alır. **Onaya açık** | Kurtarma farklı odalarda el değiştirebilir |
| K2 · Eşitlik | §6.4 tablo sırası, en çok iki unvan. **Onaya açık** | Aynı baskında birkaç eşik aşılırsa tutarlı bir sonuç verir |
| K3 · Başarı ve tekrar | Zaferle biten savunma sayılır. Daha önce kazanılmış aynı kanada ikinci saldırı zaferi sayılmaz. **Onaya açık** | Savunma başarısını korur ve yeniden alma döngüsünde puan birikimini keser |
| K4 · Köpekçi | Son raporda ayakta köpek ve yaşayan sahip. İkinci komut için hareket hakkı aynı kalır. **Onaya açık** | Komut ödülünü sahibine ve başarıyla dönen köpeğe bağlar |
| K5 · Etkilerin birleşimi | Pusu, atış, koşu ve aygıt farkları tabana eklenir. Sedye ile Sırtlayan ayrı taşıma kurallarına işler. **Onaya açık** | Takım kazanımları ve kişisel unvan aynı baskında etkili olur |

**Aşama 14'ün açık bıraktıkları.** Koçbaşı'nın 3/2/1 eşik karşılaştırması ve Nişancı'nın +2/+1 menzili onaya açık.
Şimdilik §6.4 değerleri geçerli. Ölçüm §11.6'da. Sonraki aşama sızmacıdır.

### Aşama 15 · on sekizinci tur (21 Kasım 2026)

| Karar | Uygulanan ve gerekçesi |
|---|---|
| K1 · Kayıt | Saha kapalı tesis f8, açık tesis f9 kullanır. Eski tesis kayıtlarının ayarlarıyla açılması korunur. |
| K2 · Dönüş | Aynı kadrodaki tüm uygun sızmacılar döner. Kapıda veya kabinde beklemek kimliği saklamaz. **Onaya açık.** |
| K3 · Belirti | EMP'nin merkezi ve dört dik komşusu atış anında sayılır. İtildikten sonra kişi o alanın dışına çıksa da temas aynı evrede görülebilir. **Onaya açık.** |
| K4 · Saha yakalaması | İkinci bir erzak, metal veya sabotaj ifşa cezası eklenmez. Tecritteki kişi için mevcut sorgu/söküm akışı çalışır. Birden fazla kişi sırayla karara bağlanır. **Onaya açık.** |
| K5 · Niyet | İlk plan bulunduğu kareden verilir. Eşit hedefler sabit sırayla seçilir. Botun hamle öngörüsü yalnız görünür niyetleri oynar. |

**Aşama 15'in açık bıraktıkları.** İnsan oyuncuların hırlama ve kilitlenmeyi birlikte nasıl yorumladığı ve belirtileri gördükten sonra kimleri sunucu kanadına gönderdiği ölçülmedi. K2–K4 geliştirici onayına açık. Önceki denge kararları §14'te geçerlidir.

### Aşama 16 · on dokuzuncu tur (21 Kasım 2026)

| Karar | Uygulama ve gerekçe |
|---|---|
| K1 · Tek tahta | `MapSlot`, görev ek turları ve yerleşim torbası oynanan tek tahtada gereklidir. İleride bunları kaldırmak için tesis kipinin varsayılanı ve önceki kayıtların geçişi birlikte karara bağlanmalı. **Onaya açık.** Aşama 18'de kapandı: hepsi silindi, kayıt 21. |
| K2 · Adlar | G5'teki YZ adı tek kaynaktan gelir. Coğrafi ad uyumluluk ve çeviri karşılaştırmalarında, sektör/kanat adları görev fişi ve raporda, oda adları paftada ve tahtada kullanılır. |
| K3 · Hurda ve kayıt | Yeni istek pano ve Core’da ortak kuralla reddedilir. Yalnız eski bir başarı kayda yazılmışsa tekrar yürütülür. 86 komutluk gerçek bir eski Core geçmişiyle sınandı. |
| K4 · Kıyı | Eski ad havuzunu seçen bölgesel kıyı oranının tüketicisi kalmadı. Kablo uçları hücrelerin kıyıda olmasını, çizim ise kenar listesini kullanır. |

**Aşama 16’nın açık bıraktıkları.** Tek tahta desteğinin son tarihi ile kayıt geçişi geliştiricinin kararını bekliyor. Genel denge ve oyuncu rehberi sonraki aşamaya ait.

### Aşama 17 · yirminci tur (21 Kasım 2026)

| Madde | Karar |
|---|---|
| K1 · Rehber | Aynı adımda bağlama göre kısa tesis metni. Ayrıntı bina planı ve kural kartında. |
| K2 · Örneklem | Tarihsel üç yapılandırmalı iyi küme; saldırı, savunma ve kampanyaya kendi paydası. Kurtarma paydası `t0gen` (g4) 9,39 kurtarılan/harekât (2.254/240); aşama 6 ve 8 tablolarındaki 9,34 eski bir taban değeridir (kaynağı belgede yok). g5 tek tahta karşılığı (`t17g5`) 9,27; üç paydayla da oran %19,4–19,6, hüküm aynı. |
| K3 · Ayarlar | **Onaya açık.** §11.6’daki her hedef açığı için önerilen tek alanlı deneylerden hangisi önce yapılacak? |
| K4 · Yayın | **Onaya açık.** Tesis kipini ve ayrı saha sızmacısı seçeneğini varsayılan yapma kararı geliştiricide. Aşama 18'de kapandı: ikisi de varsayılan. |
| K5 · Ölçüm botu | `Camp8.Decide`, ucuz politikada sabotajda son sakinin sökümü kapalıyken açık kalan seçeneği (sorgu) seçer. Eski kod bu durumda `First(sok)`'ta istisnayla çöküyordu. Sabotajın seçenekleri yalnız sök ve sorgu olduğundan eski kod çökmediği her durumda aynı seçimi yapar; ölçüm satırlarına etkisi yok (denetimin IL karşılaştırması, §11.6 "Köken"). |

**Aşama 17’nin açık bıraktıkları.** Hedefler insan oyuncu ölçümüyle karşılaştırılmadı. Botun taşıma ve hazırlık tercihleri, savunmaya kabul edilen ekiplerin seçilmesi ve dört zaferin azlığı sonuçları sınırlar.

### Aşama 18 · yirmi birinci tur (22 Kasım 2026)

Geliştiricinin kararı: tesis kipi oyunun tek baskın kipi olur, eski tek tahta yolu silinir, sahadaki sızmacı varsayılan açık olur,
`GameRecord.CurrentVersion` 20 → 21 (eski kayıtlar yüklenmez). Uygulama tasarımının on bir açık sorusu tasarımın önerisiyle kapandı.

| Madde | Karar |
|---|---|
| T1 · `Baskin.Create` | Havuzsuz örnek/test tahtası olarak kalır (elle çizilmiş `Layouts`; `MapSeed`'i yok sayar). `RaidEngineTests`'in altın imzaları bayt bayt aynı; Baskin sahnesi tek başına, `RaidKitPreview` ve `Rev3CaptureSet` buna dayanır. Tümden silmek (oda fikstürüne taşımak) ayrı iş. |
| T2 · Saha alt bayrağı | `infiltratorFieldEnabled` alt bayrak olarak kalır, C# varsayılanı `true`. Saha kapalı kilitler (t13K/t14K/t15K) `;infiltratorFieldEnabled=false` ile yeniden üretilir; `f8`/`f9` izi anlamlı kalır. |
| T3 · Senaryolu eğitim baskını | `TutorialRaid`, `RaidGuide`, `Marks.Guide`, Guided yolu ve `TutorialRaidTests` silindi; hiçbir kipte ulaşılamıyordu. |
| T4 · Kartsız kip | `planningBoardEnabled = false` kalır; yalnız testleri oda zinciriyle uyarlandı. Sunucu ödülü yüzeyi yok (açık soru, kip oyunda ulaşılamıyor). |
| T5 · Baskin sahnesi tek başına | `Layouts` örneği kalır (MapSeed 0, varsayılan kit). |
| T6 · Tek tahta bot listeleri | Kalır, tarihsel. `asama8.txt` ve `tesis0.txt` başlıkta "# TARİHSEL" notu taşır ve her satırdaki açık `raidFacilityEnabled=false` yüzünden "emekli alan"la durur. `cekirdek6`, `yolla4`, `usta1` yapılandırma dosyalarına not eklenmedi: `kos.sh` `#` satırını da iş sayar. |
| T7 · Katar ve Yalpa | Alt bayraklar ve kapalı yolları (`asansor`/`yag`) kalır. |
| T8 · Alarm testleri | `LiberationCampaignTests`'in alarm ve karşı saldırı testleri `facilityDefenseEnabled = false` ile eski kuralı kilitler. |
| T9 · Kısa nöbet | Tesiste `kisa` yalnız hedef odasının ilk ziyaretini bir tur kısaltır (`Baskin.FirstVisitTurns`; savunma ve dönüş tahtaları kendi sürelerini kurar). Açıklama "Hedef odasının ilk ziyareti bir tur kısa sürer." oldu (TR/EN). |
| T10 · Kayıt yeri | §12'ye "18 · Tek kip" satırı ve bu tur. |
| T11 · GÖZETİM cepsiz geometrisi | `watchOpen` parametreleri ve `deckNoWatch` tokeni kalır; sadeleştirme ertelendi. |

**Kapananlar.** 20 · K4 (tesis kipinin ve saha sızmacısının yayına açılması) ve 19 · K1 (tek tahta desteğini kaldırma zamanı, yeni
varsayılan ve önceki kayıtların geçişi): geçiş yok, sürüm 21 eski kayıtları yüklemez. 19 · K3 (oyuncu kayıtlarında eski köpek hurdası)
ve 13 · S3 (sonuç teleksi tek tahtada da basılsın mı?) anlamsızlaştı.

**Uygulanan hâl.** Core: `raidFacilityEnabled`, `liberationLootExtraTurns`, `liberationServerExtraTurns`, `RaidParams.MapSlot`/`ExtraTurns`
ve `RaidMaps.Pool`/`Slot`/`PoolSize`/`KindOf`/tek argümanlı `Valid` silindi; bayrak okumaları tesis yönüne sabitlendi. Her emir üç oda
yazar. `ResolveRaid` imzası aynı; verilen sonucu yok sayar, son oda raporlanmadıysa reddeder. `ReturnPlanningSquad`'ın tek tahta kolu
gitti. `FacilityMaps.FingerprintFor` hep `f8`/`f9`. Sunum: `LiveRaidDataSource` emri hep oda oturumuna verir (odasız emirde `LogError`),
`PlanningTutorialFlow`'un tek tahta gövdeleri tesis metniyle değişti (alt bayrak kapalı taban metinleri kaldı), ana bayrak kapıları
sabitlendi; yeni editör yardımcısı `RaidChainFixture`. Testler: elle sonuç veren testler oda zinciri yardımcılarına geçti (`FacilityKit`,
`ShelterEventKit`, `FacilityDeskKit`, `FacilityRewardPlayKit`); yeni testler §11.4 "Aşama 18'de kayıt" ve `DURUM.md`'de. Bot: `ApplyBal`
emekli alanları. Asset: `BalanceConfig.asset` yeniden serileştirildi, bütün `facility*` alanları ve `infiltratorFieldEnabled: 1` yazılı.

**Aşama 18’in açık bıraktıkları.** Kapanış turunda `SiginakTextScanTests.BoardCards_AtK4` kırmızısı kısa kart adıyla giderildi;
tam EditMode, tam PlayMode, oyunda bir baskın ve v20 kayıtla açılış doğrulandı (`DURUM.md`). `SiginakFixtures.ApplyCounterattack` ve
`RaidChainFixture` yalnız derlendi. `RaidBot`'taki `Room == null` dalları, GÖZETİM cepsiz geometrisi ve `ResolveRaid(string)` imza
indirimi ayrı işler.

### Tek kip denge turu · yirmi ikinci tur (22 Kasım 2026)

Geliştiricinin isteği: zafer oranı yükselsin (denge ayarı). Ölçüm ve gerekçe §11.6 "Tek kip denge ölçümü".

| Madde | Karar |
|---|---|
| D1 · Kapsam | Yalnız `Balance` değerleri; kural, komut, RNG sırası ve kayıt sürümü (21) değişmedi. Kayıt dengesini taşıdığından eski v21 kayıtlar kendi değerleriyle oynar. |
| D2 · Değerler | `liberationCivilianHp` 3, `facilityTargetTurns` 9, `candidateEmergencyBelow` 4, `liberationTier3At` 9, `liberationAlarmPerShift` 0,5. Bot zaferi %1,2 → %43,3 (iyi3, 80 tohum; karışık politikalarda %49–61). |
| D3 · Alınmayanlar | `liberationCiviliansPerTrust` 1 (güveni beceriyle çarpar, tavanda boşa gider), `liberationTrustPerRegion` 5–6 (son ölçümde etkisiz), `facilityReturnTurns` 6 ve `facilityMiddleTurns` 6 (etkisiz), `facilityTargetMachines` 2 (hedef odanın kimliğini zayıflatır), kapı aralığı 2/3 ve başlangıç nüfusu 10 (kıtlık gerilimini kaldırır), `facilityDefenseRoomTrust` 0 (botta hiç işlemiyor). |
| D4 · Sunum | Alarm buçuklu ilerlediği için kenar satırı ve CRT alarmı ondalığıyla yazar (raporlar zaten öyleydi); 9,5 artık "10/10" görünmez. V.014/K6 tasarım düzeni testleri alarm payını 1'e sabitler. |
| D5 · Bayt kilidi | `taban21` eski dengenin tabanıdır; varsayılanlar değiştiği için A ve B ayrışır. Yeni taban `Library/Calisma/taban22` (`kilit22.sh`, aşama 13–15 özgün dosya karşılaştırması çıkarıldı). |

**Açık sorular (geliştirici).** (1) Hedef: bot zaferi %43 (insan daha yüksek) yeterli mi, yoksa kural düzeyinde ikinci tur mu? Kit
kuralı denendi ve etkisiz çıktı (§11.6 "Kit kuralı denemesi"; geri alındı). Kalan aday ölüm kuralıdır (gövdenin kurtarılması); kayıt
sürümünü artırır. (2) Zafer uzunluğu medyanı 74 vardiya: aşama 8 hedefi (35–45) korunacak mı? (3) Kefilli kapıyla terk %31: istenen bir
bedel mi?

### Geliştirici onayına açık kararlar dizini

Madde numaraları yukarıdaki §14 tablolarına aittir. Dokuzuncu–on beşinci turlarda geliştirici uyurken alınan kararların tamamı onaya açık bırakılmıştır. Aşağıda önce oynanışı ve yayın kararını etkileyen sorular, ardından o yedi turun madde dizini yer alır. Ayrıntı dizininin "Karar (kısa)" sütunu kaynak tablonun Karar sütununun ilk cümlesi ya da yaklaşık 160 karakteridir; kısaltılan girişler "…" ile biter, kesilince sonucunu ya da onay durumunu kaybedecek dokuz madde elle özetlendi. Tam karar kaynak turun tablosundadır.

| Tur ve madde | Geliştiricinin kararı |
|---|---|
| 20 · K3 | §11.6 açığı başına önerilen ayar ve bot deneylerinin sırası. Zafer açığı için yirmi ikinci turda beş değer uygulandı (§11.6 "Tek kip denge ölçümü"); kalan açıklar ve kural adayları o turun açık sorularında. |
| ~~20 · K4~~ | ~~Tesis kipinin ve ayrı saha sızmacısı bayrağının yayına açılması.~~ Kapandı (aşama 18, yirmi birinci tur): tesis tek kip, saha varsayılan açık. |
| 7 · açık madde, süre hedefinin tanımı | Tur hedefi (12–16) toplam mı gidiş mi sayılır? Aşama 17'de gidiş (G+A+H) medyanı 14 (bandın içinde), dönüşle toplam 20. |
| 9 · 16, 23 ve A6 ölüm §1–4 | Gövdeyi son nefes sonrası odalar arasında taşımak, elde kalan kanattan çıkarmak, girişe yakın yaralıyı kurtarmak veya kurtarma hedefinin paydasını değiştirmek. |
| 9 · 24c, A6 pusu | Ekip başına pusu sınırı konması ve dost/bulut engelinin oyuncuya nasıl anlatılacağı. |
| 9 · A6 ikmal ve kademe 3 | İkmal başarısının tabanı aşması ve üst kademede sunucunun zorlaşması için görev türlerine ayrı ayar. |
| 9 · 23–24, A6 son nefes ve yük | Son evrede düşenin ekiple dönebilmesi, ayaktaki kişinin süre sonunda kalması ve taşıyıcının yükü bırakması. |
| 10 · 18 | Hedef odasında süre sonu için ayrıca onay istenmesi. |
| 10 · 11 | Sürükleme için tahta fişine ek Core telsiz günlüğü. |
| 10 · A7 kalkık kepenk | Kalkık kepengin ayrıca kâğıt notla gösterilmesi. |
| 10 · A7 pusu, 9 · 24c | Pusuda ilk kareyi kapatan tek bir dost veya bulut için yön bazında açıklama. |
| 11 · K24 | Sedye son nefesle gelen gövdeyi sonraki odada taşır. Revir yatışı kısaltır. |
| 11 · K22; 13 · S2 | Cevap paketinde tarif seçiminin kapsamı ve ekip düzeyinde uygulanması. |
| 11 · K13 | Tutsak makinenin oyuncuya sunduğu eylem tablosu. |
| 11 · 38; 12 · 29 | Dağılma sonunda bekleyen ödül Core’da kalır, son ekranda gösterilmez. |
| 11 · A8 okuma 2 | Makaralı vinç ve kumanda kelepçesinin kullanımını tarif maliyeti, hat ya da çekiş kuralıyla artırmak. Aşama 17: vinç %3,22, kelepçe %5,51 (hedef ≥ %10). |
| 11 · A8 okuma 4; 13 · A10 okuma 8 | Üretim açıkken gözlenen yaklaşık 1,6–2 puanlık başarı kaybının kaynağını incelemek. |
| 11 · A8 köpek ölçüsü | Bedelsiz havlamayı içeren kullanım yüzdesi yerine koklama/getirme hedefi belirlemek. |
| 11 · 51, 62; 12 · 34 | Olay hedeflemesinde uzman filtresi ile oda raporundaki Units/Sniff denetiminin kapsamı. |
| 12 · 13; 15 · S4 | Kurgu modellerini korumak veya gerçek three.js modeli, Node/three ve gerektiğinde Playwright kurulumuna geçmek. |
| 12 · 83–84, A9 hover | Telsiz masasının hover kartını örtmesi, masa kilidi ve önceki yedek fontla ne yapılacağı. |
| 13 · S1 / 6 | Pusu isabeti aygıt önlemine sayılsın mı? Mevcut false. |
| 13 · S2 / 20 | Cevap tarifinin etkisi ekip düzeyinde mi, tek eşyada mı? Mevcut ekip düzeyi. |
| ~~13 · S3 / 40~~ | ~~Sonuç teleksi tek tahtada da basılsın mı?~~ Anlamsızlaştı (aşama 18: tek tahta yok). |
| 13 · S4 | Tek araç farkında başarı yerine cezalı taşıma hedefi, daha güçlü önlem veya başka tek araç politikası seçmek. Aşama 17: `t17tek − t17` = +0,88 ±0,51 puan (hedef 5–10 puan düşük). |
| 13 · 75; 15 · A12 fiş | EN beş satırlı görev fişindeki yaklaşık 19 px taşma için fiş konumu veya görev satırı. |
| 13 · 81 | Yinelenen vinç telsiz satırının ilk denemeye indirilmesi. |
| 14 · S1 / 35 | Saldırı kazanıldığında hedef ve dönüş odasında bırakılan gövdeleri çıkarmak. |
| 14 · S2 | BIRAK sonrası aynı vardiyada o kanada baskın yasağı sürsün mü? Önceki ölçümde bırakmaların %37’sinde baskın hakkı kullanılamadı. Aşama 17 bu oranı yeniden ölçmedi (`t17birak` koşuldu, oran hesaplanmadı). |
| 14 · S3 / 38, 78 | Dalga ve süre taramasında %40–60 savunma başarısı hedefi sağlanmadı. Aşama 17: 231/231 = %100; kuşatma başına tutulan kanat 231/509 = %45,4. |
| 14 · S4 / 17 | Kısmen kapıda bekleyen uzman tur sonunda geri döner. Son turda içeri çekilme kapalı. |
| 14 · açık soru 5 / 78, 111 | Savunma için sızma, dalgayı iki girişe bölme, pusu sınırı veya kuşatma başına başarı hedefi. Aşama 17'de kuşatma başına %45,4. |
| 14 · 7, 47, 117 | Bekleyen kuşatmada ikinci alarm eşiği güveni azaltır. Savunmada GEÇ ile takipçi ve ulaşmış kuyruk taşınır, gelecekteki dalga yeniden zamanlanır. |
| 15 · S1 / 7 | Katar ve Yalpa tesis adları alt bayraklardan bağımsız kalır. |
| 15 · S2 / 21 | Savunmada Katar kenarları ve Yalpa kapalı kalır. |
| 15 · S3 | AYIR ve kenardan kaçış için yeni bedel konması. Ölçüm sonrası ek bedel uygulanmadı. |
| 15 · S4 / 43 | Vagon kuplörü ve şartlara özgü rölyef kitleri eklenmesi. Mevcut kitler kullanılıyor. |
| 16 · K1 | YZ paketlerinin imza makineleriyle eşlemesi ve §6.5 başlangıç sayıları. |
| 16 · K2 | İtme/çekmede ortak çarpışma ve cıvata, kurulan engelde ortak yol denetimi. |
| 16 · K3 | Katar ve Yalpa odalarına özgü adlar ve altı makine için var olan örgülerden prefablar. |
| 16 · K5 | Kelepçelenmiş imza makinesinde mevcut yürüyüşün korunması. |
| 16 · A13 açıkları, §11.6 tek alan taramaları | Alev menzili ve kırağı süresinin varsayılanları, imzaların ölüm/başarı dengesi. |
| 17 · K1 | Sırtlayan ilerlemesi gövdeyi hareket ettiren ve dönen kişiye yazılır. |
| 17 · K2 | Eşik eşitliğinde tablo sırası uygulanır ve kişi en çok iki unvan alır. |
| 17 · K3 | Başarılı savunma sayılır, önceden kazanılan kanadı yeniden almak sayılmaz. |
| 17 · K4 | Köpekçi için sahip ve köpeğin dönüş durumu ve ikinci komutun yürüyüş hakkıyla ilişkisi. |
| 17 · K5 | Takım kazanımlarıyla kişisel unvan etkilerinin birleşimi. |
| 17 · A14 açıkları | Koçbaşı için 3/2/1 iş eşiği ve Nişancı için +2/+1 menzil kıyası. Unvanların saldırı başarısı ve zafere etkisi. |
| 18 · K2 | Aynı kadrodaki tüm uygun sızmacılar, geçit/kabin dahil, döner. |
| 18 · K3 | EMP temasında atış anındaki merkez ve dört dik komşu alınır. |
| 18 · K4 | Sahada yakalanan kişi için ikinci sabotaj bedeli yoktur. Aynı baskından gelen sorgular sırayla açılır. |
| 18 · A15 açıkları, §11.6 | Saha açıkken gözlenen −2,66 puan saldırı başarısı ve insanların belirtilere verdiği tepki. Aşama 17 (80 tohum, `t17saha − t17`): −1,34 ±0,40 puan. |
| ~~19 · K1~~ | ~~Tek tahta desteğini kaldırma zamanı, yeni varsayılan ve önceki kayıtların geçişi.~~ Kapandı (aşama 18): geçiş yok, kayıt 21 eski kayıtları yüklemez. |
| ~~19 · K3, §15~~ | ~~Oyuncu kayıtlarında eski köpek hurdası bulunması~~ Anlamsızlaştı (aşama 18): sürüm 20 ve önceki kayıtlar yüklenmez. |

<details>
<summary>Dokuzuncu–on beşinci turlarda onaya açık bırakılan bütün uygulama maddeleri</summary>

**Aşama 6 · 9. tur**

| Tur ve madde | Kararın konusu | Karar (kısa) |
|---|---|---|
| 9 · 1 | Eylem API'si | Ayrı `DoAction`, `ActionTargets`, `PreviewAction`, `CanAction`; eski `DoAct`/`Targets`/`Preview` aynen (D1) |
| 9 · 2 | Bedeller | Pusu, fırlatma, ortak yeteneğin yerine; koşu ve sürükleme yürüyüş + yetenek; menfez yalnız yürüyüş, yetenek kalır (D4, D26) |
| 9 · 3 | Sürükleme | Açık eylem; gövde yolun varıştan önceki karesine (BFS, `Dirs` sırası); menzil −`facilityDragMoveCost` (yeni alan, 1) (D3, D4) |
| 9 · 4 | Menfez | Açık eylem, `DoMove` ışınlamaz (D2) |
| 9 · 5 | Koşu | `facilityRushRun` ≤ L ≤ max(`facilityRushRun`, yürüyüş); iki ayrı itme, ilki oynatmazsa ikincisi yok; geçitte kapıya girmez (D5, D6) |
| 9 · 6 | Pusu ayrıntısı | Hat kurulduğu kareden; plan sonrası, uyanma öncesi; sahibi tahtada değilse ateşlemez; çok hat ayrı vurur; buluttan kurulmaz; sis kesmez (D7, D8) |
| 9 · 7 | İki kişilik işte "bitişik" | Sekiz komşu (D9) |
| 9 · 8 | Dinamit | Kepenk ve sandık nişi hedef değil (D10) |
| 9 · 9 | Büyük sandık | Her ikmal hedef odasında, yer varsa; duvar nişi (`Cell.Crate`), RNG'siz deterministik seçim; iş kareleri üç hâlde de batı girişine bağlı (D11) |
| 9 · 10 | Sandık ödülü | Yalnız kazanılan ikmal baskınında, kartlı yolda: 2 metal, 1 devre (D12) |
| 9 · 11 | Yük | Yalnız kendi hareketiyle varan alır (yürüyüş, koşu, sürükleme, menfez); yük yerde kalırken oda biterse Cargo −2; fırlatmada kapıdaki uzman alıcı değil (D14… |
| 9 · 12 | Gövde temsili | Yeni alan yok: `Dead` + `AtGate`/`Out`; gövde itilmez, kanca görmez (D16) |
| 9 · 13 | Temiz odadan toplanan, kapıdaki gövde, kartsız yol | Toplanan sonraki dönüş odasına girer ve orada da taşınmalı (D17); geri çekilmede kapıdaki gövde ölür (D18); kartsız yolda ölüm yok (D19) |
| 9 · 14 | Kayıt | `CurrentVersion` 19 kalır, tesis izi f1 → f2 (D20); AGENTS kural 1'in lafzından bilinçli sapma (§11.4 "Aşama 6'da kayıt") |
| 9 · 15 | Bot ve pusu | Doğrulama kopyasında pusu hatları silinir, kaba beklenti terimi `W.Ambush` eklenir; pusu makine aklına görünmez (D21) |
| 9 · 16 | Ölüm hedefi tutmazsa | `facilityDragMoveCost` 0 ölçülür; D0 tutup 1 tutmazsa varsayılan 0 (D22). Sonuç: ikisi de tutmadı, **1 kaldı** |
| 9 · 17 | Rapor ekleri ve hedefin tanımı | Ekler yalnız varsayılan dışındaysa yazılır (D23); hedef (taşınan + toplanan) / harekât ÷ tabanın kurtarılanı ≥ %80, ayrıca / düşen (D24) |
| 9 · 18 | Tutuş | İlk uzman tutar ve yeteneğini harcar; ikinci aynı tur gelmezse boşa gider; tutan çekilir ya da düşerse ikinci yeni tutuş olur (D25) |
| 9 · 19 | Ölçüm tabanı | `t6taban` = aşama 6'dan önce derlenmiş exe, aynı 80 tohum; bot tarafında eski kuralın taklidi yok |
| 9 · 20 | Botun varsayılan düşen politikası | `hep` (`RaidBot.Fallen`, Camp8 `DUSEN`) |
| 9 · 21 | Gidişte G/A'daki gövde | Bot dokunmaz (tasarımdaki "yarım bırakma bedeli" yerine) |
| 9 · 22 | GEÇ kazancının tutulması (`holdWin`) | Turun başında gövdeler süresinde kurtarılabilir görünüyorsa GEÇ ve son çıkış kazanç sayılmaz; kapanış düzeltmesi: yalnız ayaktakilerin süresi de yetiyorsa… |
| 9 · 23 | Son nefes | Oyuncunun hamle sırası dışında düşen ve oyuncuya tur kalmadan biten odada, biri ayakta çıktıysa ekiple gider (§4.3 tablosu); `Unit.Breath`; ölçüm anahtarı… |
| 9 · 24 | Kural kenarları (bağımsız inceleme 7a/b/c) | (a) Sürükleme yolu kapıdan geçerse gövde kapı olmayan son kareye; (b) yük geçilemez karede düşerse en yakın açık kareye; (c) pusu dost uzmanlı ya da bulutlu yöne kurulamaz… |
| 9 · 25 | Bot sayacı | `CountFx` efektleri nesne kimliğiyle bir kez sayar |
| 9 · 26 | Küçük kararlar | `PreviewAction` geçersizde null; `MoveRules` değerleri sıkıştırır (`RushRun` ≥ 1, öbürleri ≥ 0); ekiple gelen gövdenin karesi kademe eki makinelerine kapalı… |

**Aşama 7 · 10. tur**

| Tur ve madde | Kararın konusu | Karar (kısa) |
|---|---|---|
| 10 · 1 | Tuş | Tek tuş **E**, döngü (`ActionKinds` sırası, sürüklemede gövde başına); Esc, sağ tık, Q, seçim ve Z kapatır; kart satırına tık doğrudan kurar |
| 10 · 2 | Kart | Prefab değişmez; hamle satırları Row 1'in çalışma anı kopyaları (kardeş sırası yoluyla bağlanır) |
| 10 · 3 | Satırdaki hamleler | Yalnız bu odada anlamlı olanlar; kullanılamayan soluk ve nedeniyle; kart taşarsa kullanılamayanlar tek satıra katlanır (gerekmedi, bkz. 32) |
| 10 · 4 | Plaka satırı | Row 2 olduğu gibi kalır; hamleler altına |
| 10 · 5 | Sürüklemede gövde seçimi | Gövde başına ayrı seçenek; kipteyken gövde karesine tık gövdeyi değiştirir |
| 10 · 6 | Hedef boyası | Bütün hamlelerde hedef çerçevesi; kipte yürüyüş boyası kapalı |
| 10 · 7 | Pusu hattının görünümü | `lib.good` yeşil, kesik, oksuz; sahibi tahtada değilse %35 |
| 10 · 8 | Yerdeki yük | Küçük sandık parçası (`lib.lootCrate` × 0,45, köşede) ve "YÜK" notu |
| 10 · 9 | Sandık nişi, tutuş, kalkık kepenk | Not (SANDIK, TUTULUYOR) ve hover; kalkık kepenkte not yok; model aşama 9 |
| 10 · 10 | Makine evresi sırası | İlk `ambush-hit`'te bölünür: saldırılar, yürüyüş (pusuyla düşecekler dahil), pusu ve uyanma |
| 10 · 11 | Sürüklemenin telsiz kaydı | Yalnız fiş; Core satırı yok |
| 10 · 12 | Sürükleme canlandırması | Sürükleyen yolu yürür, gövde bir adım arkasından |
| 10 · 13 | Yürüyüşte yük alımı | `Carrier` farkından |
| 10 · 14 | GEÇ ve yerdeki gövde | Onay yok; fişte ve kapı hover'ında uyarı |
| 10 · 15 | Ekip kartı | YERDE · kare / GÖVDE KAPIDA / ÇIKARILDI (kapanışta EKİPLE TAŞINIYOR eklendi, bkz. 46) |
| 10 · 16 | Büyük sandık ödülü | KAZANÇ sandığı içerir (hata düzeltmesi); SONUÇLAR'a satır; 7 yuva aşılırsa değişmemiş alarm satırı düşer |
| 10 · 17 | Sonuç teleksi (altıncı tur 12) | Aşama 7'den aşama 10'a (teleks işi) taşındı (§7.3) |
| 10 · 18 | Hedef odasında süre sonu onayı | Kapsam dışı; açık soru |
| 10 · 19 | Kurallar ve uyarı metinleri | rules.2'ye tesis satırı, rules.6 tesis metni, geri çekilme tesis anahtarı |
| 10 · 20 | Ses | Yeni ses yok |
| 10 · 21 | Core ve kayıt | Dokunulmaz; sürüm 19 |
| 10 · 22 | Lokalizasyon akışı | `strings.txt` elle; `UpdateTable` çıktısı toptan kopyalanmaz; denetim `LocalizationTests` ile |
| 10 · 23 | Test edilebilirlik | Metin, seçenek ve önizleme verisi saf Bridge sınıflarında; `MarksFor(vm)` ayrıldı |
| 10 · 24 | Rehberli eğitim baskını | E kapalı |
| 10 · 25 | Hover kartı | Önce hamle önizleme satırları; 5 satır sınırı |
| 10 · 26 | Kepenk açılışı | `SinkWall` (breach dalı) |
| 10 · 27 | Yeni test dosyaları | Ayrı dosyalar (`FacilityMoveTextTests`, `FacilityMovesPresentationTests`, `FacilityMovesSceneTests`, `FacilityBigCrateReportTests`, PlayMode… |
| 10 · 28 | Bedel etiketi | Ayrı metin değil; ayrıntı bedel ile değeri birlikte taşır ("yetenek yerine · hat 4 kare") |
| 10 · 29 | Tıklama sırası | Önce hedef, sonra (sürüklemede) başka gövde, sonra ayaktaki ekip üyesi; bir gövdenin karesi aynı zamanda hedefse tık hamleyi verir |
| 10 · 30 | Kurallar kartı ayrıntısı | rules.6 tesis metni kazanma satırının altına eklenir; geri çekilme metni ikmal/sunucu ayrımı olmayan tek tesis metni |
| 10 · 31 | Çıkış satırı | Taşıyıcının adını yazan metin yalnız tesiste; eski tahtada "…malzeme Teknisyen'le çıkar" |
| 10 · 32 | Kart sığması | Ölçüldü: ad tek satır, ayrıntı en çok iki satır (bugün de iki satıra inen EN metni var); en kötü durumda kartın tepesi 770 px, panelin altı 837 px; katlanan… |
| 10 · 33 | Fişlerde ad | Telsiz fişinde "kişi · rol" (`PersonalName`), kartta ve hover'da kısa ad (`Who`) |
| 10 · 34 | Tutuş ayrıntısı | Tutanın kendi kartında da "… {ad} tutuyor" görünür |
| 10 · 35 | Pusu hattının çizimi | El çizimi değil, düz kesik çizgi, 4 px |
| 10 · 36 | Boş pusu hattı | Hat boşsa yönündeki ilk kareye kısa saplama |
| 10 · 37 | Çizgi tonları | "ink" grafit çizilir |
| 10 · 38 | Odak (iki kişilik iş) | Yalnız iş karesinin üzerine gelinip önizleme varken |
| 10 · 39 | `RaidPiece.Init` | Tembel kurulum (Game kodu; sahne/prefab değil) |
| 10 · 40 | Tesis girişinde çıkan gövde | `evac` Fx'iyle zaten kalkan uzman ikinci kez kaldırılmaz; kapıdaki gövdenin kalkışında da KAPIDA popup'ı |
| 10 · 41 | PlayMode kurulumları | Pusu ve yük testleri kurulumu `BState.Clone` + `EndTurn` ile önceden arar (tasarımdaki `StillTurns` yerine); sürükleme ve fırlatmada Teknisyen'in `Moved/Acted`… |
| 10 · 42 | Raporda sandık değeri | Satır Balance'taki nominal değeri yazar; KAZANÇ kapasite kırpmasından sonraki gerçek değeri |
| 10 · 43 | Alarm düşürme kapsamı | Yalnız sandık satırı eklenince ve yalnız alarm `Neutral` (değişmedi) iken; sandıksız raporlar bugünkü gibi |
| 10 · 44 | Makine evresinde amaç satırı | `FacilityRaidText.Goal(display, …)`: taşıyıcı, YERDE damgası ve amaç aynı sunum kopyasından |
| 10 · 45 | Yük yerdeyken çıkış ve kapı | Çıkış karesinde tahliye satırından sonra, kapı kartında sonda "Yük yerde kalır · {kare}"; kapı kartının 4 satır sınırı için önce "Kapıdan geri çıkılmaz", sonra… |
| 10 · 46 | Son nefes gösterimi | Ekip kartı Core'un oda raporu kuralını izler (girişte ÇIKARILDI, geçişte EKİPLE TAŞINIYOR); parça evre sonunda ekiple kalkar, `Sync` çizmez; hover "Son nefeste… |
| 10 · 47 | `EffectPlaying` uzunluğu | `RaidBoard3D.Animate(…, indexBase, total = -1)`; makine evresinin iki parçası tam listenin uzunluğunu bildirir |
| 10 · 48 | Elle yazılan `.meta` biçimi | Kısa biçim (`fileFormatVersion: 2` + `guid`, sonda satır sonu yok); `MonoImporter` bloğu eklenmez |

**Aşama 8 · 11. tur**

| Tur ve madde | Kararın konusu | Karar (kısa) |
|---|---|---|
| 11 · 1 | Kayıt | `CurrentVersion` 19 kalır; tesis izi f2 → f3 |
| 11 · 2 | Eylem dizileri | `ActionKinds` altı kalır; `GearActionKinds`, `UnitActionKinds`, `AllActionKinds` ayrı |
| 11 · 3 | Aygıtın çalışma anı | Niyetlerden ve sisin dağılmasından sonra, kaçış denetiminden ve uyanmadan önce |
| 11 · 4 | Hat uzunlukları | Taret 4, vinç 4; taret tel'den geçer, duvar/nesne/bulutta biter; vinç her nesnede biter; yem engelde, kapıda, menfezde durur |
| 11 · 5 | Tel "atışı geçirir" | Makine ateşi ve kilidi, pusu, taret geçer; kanca, fırlatma, vinç, yürüyüş geçmez |
| 11 · 6 | Örme seti | İki eylem: `tel` (1 hak), `kalas` (2 hak) |
| 11 · 7 | Engel yeri ve ekip güvencesi | Dik komşu boş zemin; batı, ileri geçit, hedef bağı "önce tutuyorsa sonra da" |
| 11 · 8 | Engel kırma | Tesiste ekibe ulaşamayan her makine engele 11 puan (sivil 9) |
| 11 · 9 | Hedefleme | Yem ve havlayan köpek ekip gibi (10); taret, vinç, köpek, tutsak 3; kilit yalnız uzmana |
| 11 · 10 | Aygıt bağışıklığı | Oyuncu EMP'si (hasar, itme), kilit, donma işlemez |
| 11 · 11 | Hurda | Süresi biten/kırılan aygıtın karesinde (1 can); kapı, menfez, gövde/yük karesinde ya da ekip güvencesini bozacaksa kalkar |
| 11 · 12 | Tutsağın süresi | Yakalandığı tur oynamaz; sonraki 3 oyuncu turu oynar; sonra bir makine evresi niyetsiz |
| 11 · 13 | Tutsağın eylemi | Atış/vuruş/havan/patlama hasar; onarım ve kalkan uzmana plaka; kilit hedefi bir plan niyetsiz; çoğaltıcı eylemsiz |
| 11 · 14 | Köpek eşyası | Kişiye bağlı değil, yardımcı yuvada; dinlenme `BoardEquipment.Rest`; ölen sahibin köpeği depoya |
| 11 · 15 | Köpek tahtada | Slot −1; yürüyüş 5; kapıya girmez (girişte tahliye); GEÇ'te ekiple; yükü `getir` ile; fırlatma alıcısı; havla Manhattan 3; kokla ileri geçit yanında, kaçıştan… |
| 11 · 16 | Köpeğin düşmesi | Tahtadan çekilir, gövde değil; rapor "d"; dönüşte 3 vardiya dinlenir |
| 11 · 17 | Tarif sırası | Eski altı + açılanlar açılış sırasıyla; otomatik yol yalnız eski altı |
| 11 · 18 | Erzak bedeli | Emir başlarken düşer; yetmezse bekler; `board_reserve` değişmez |
| 11 · 19 | Eksik kit emir açıkken | Tesiste eksik rol kiti staged + serbest hammaddeden; emir o kapanış ilerlemez |
| 11 · 20 | Emir sayısı | Tek emir sınırı kalır |
| 11 · 21 | Bekleyen ödül | Yalnız baskın emrini kilitler; zaferde kurulmaz; `Rewarded` ödülü bir kez yapar; bölge fonu/güveni aşama 11 |
| 11 · 22 | Cevap paketinde tarif | Seçilebilir; `Recipes`'e cevap kimliği; etkisi aşama 10 |
| 11 · 23 | Kazanımın kapsamı | Ekip çapında; emirde `RaidParams.Perks` |
| 11 · 24 | Sedye | Yeni tanım (kaldıraç 1); Sırtlayan eski tanımı alır; ölçüm anahtarı `facilityStretcherAlways` |
| 11 · 25 | Muğlak kazanımlar | Kulak +1/vardiya; Plan defteri ilk kanadın G'si; Eşik gidiş G +1; Açık hat dönüş +1; Revir −1 (en az 1); Tezgâh ödül tarifleri −1; Siper etkisiz |
| 11 · 26 | Kazanım ve unvan | Toplanır |
| 11 · 27 | Kelepçenin tükenmesi | Son raporda hak 0 → panodan silinir; kayıpta da |
| 11 · 28 | Ölçüm tabanı | `t8taban` = aşama 8 öncesi exe (= `t6sn`); regresyon kilidi `t8yok` |
| 11 · 29 | Planlama botu | Ortak `CampaignBot.cs`; önce saf taşıma |
| 11 · 30 | Kokla kaydı | Rapor `Sniff` → `AdvanceRaidRoom` öğrenir |
| 11 · 31 | Lokum hedefi | Yalnız duvar; kepenk, sandık nişi, bulut değil |
| 11 · 32 | Aygıtın yönü | Kurulum karesi dik komşu, yön uzmandan kareye |
| 11 · 33 | Yükü taşıyan köpek | Rapor `Cargo` = sahibin sırası + `CargoDog` |
| 11 · 34 | Vinç ve gövde | Gövdeyi ve yükü çeker (D16'ya tek istisna); kapı ve menfeze değil; ayakta birim önce |
| 11 · 35 | Üretimsiz koşunun eşitliği | `t8yok` = `t6sn` (yeni anahtarlar hariç) |
| 11 · 36 | A: `Recipe` | Ödül tarifleri `FacilityGear.Make`'te; Simulation'ın `Recipe` yardımcısı değişmedi |
| 11 · 37 | A: `PerkDesc(Balance, id)` | Açıklamadaki sayılar dengeden |
| 11 · 38 | A: ödülü kuran baskın oyunu bitirirse | `PendingReward` dolu kalır, seçim reddedilir ("Oyun bitti."). **Onaya açık:** son ekran bekleyen ödülü göstermemeli; aşama 9'da sunum göstermiyor |
| 11 · 39 | A: ödül tablosu dışı bölge | Ödül kurulmaz |
| 11 · 40 | A: K19 "ilk eksik kit" | Harfiyen: ilk eksik kit karşılanamıyorsa sonrakine bakılmaz |
| 11 · 41 | A: ölenle giden eşya | Yalnız baskın ölümünde köpek depoya döner; olay ve ayrılma değişmedi |
| 11 · 42 | A: `ReturnPlanningSquad(…, reports)` | İmza A'da, kullanım C'de |
| 11 · 43 | B: dosya | Tahta kuralları `Core/Baskin.Gear.cs` (statik kısmi sınıf) |
| 11 · 44 | B: kısa harfler | Taret R, yem Y, vinç V, köpek Ö; can ve yürüyüş `MoveRules`'tan |
| 11 · 45 | B: vinç hattı | Ayaktaki aygıt keser; bulut kesmez; turnikenin kapalı yönü çekişi ve yem adımını durdurur |
| 11 · 46 | B: hurda yeri ve telsiz | `FreeFloor` + ekip güvencesi; "{0} kırıldı." / "{0} durdu." / "Aygıt hurdaya döndü." |
| 11 · 47 | B: Sedye, kimse ayakta değil | Son raporda gövde `Body 0`, `Fell` son oda (ölür) |
| 11 · 48 | B: niyetsiz makine | `Order 0`, `Intent null`, `Rnd` çekilmez |
| 11 · 49 | B: `stuck` | Yalnız tahtada engel varken hesaplanır |
| 11 · 50 | B: C'nin küçük parçaları B'de | Köpek `Hurt` dalı, girişte tahliye, `Machines` tutsağı sayar, köpek fırlatma alıcısı, rapor köpek/tutsak satırları, taşıyıcı köpek |
| 11 · 51 | B: olay hedeflemesi | `Baskin.Events.cs` 1277/1319 `Side == 'p'` kaldı (aygıt, köpek ve tutsak olay nişanında ekip sayılır). … |
| 11 · 52 | B: kilit | Puan yalnız uzmana; `ApplyIntent` kilidi yalnız aygıta işlemez (köpek/tutsak kilitlenebilir) |
| 11 · 53 | B: test kancaları | `Baskin.TestScore`, `TestTeamLike` (internal) |
| 11 · 54 | C: sunum alanları | `SquadOutcome.ClampSpent/DogDown` (tasarımdaki `LiberationRaidReward` yerine) |
| 11 · 55 | C: komuta hedefi | Vurulabilir kare: ayakta makine ya da nesne (ikmal kasası, soğutma vanası değil); boş kare ve duvar değil; havan yalnız hedef karesine |
| 11 · 56 | C: bakım/kalkan/bomba | Bakım bulutta makineye vurmaz; kalkan plakasız dik komşu uzman varsa yalnız plaka; patlayan robot çevresinde ekip birimi ya da vuracak şey yoksa verilmez… |
| 11 · 57 | C: kilitleyici komutu | Bu evrenin niyetini silmez; sonraki planı atlatır |
| 11 · 58 | C: kelepçede bulut | Sınır yok; kabin ve kapıdaki makine `UnitAt`'ta yok |
| 11 · 59 | C: bırakılan tutsak | `Locked` temizlenir; `Moved/Acted` dokunulmaz |
| 11 · 60 | C: karesi olmayan köpek/tutsak | Konmaz; ilk odada konmayan köpek rapora girmez |
| 11 · 61 | C: köpek yükle kalınca | Rapor `Cargo −2`, metin "Köpek yükle tahtada kaldı…" |
| 11 · 62 | C: Sniff denetimi | `AdvanceRaidRoomCheck` reddetmez; aralık dışı yok sayılır. Bağımsız inceleme: `Sniff` ve `Units` yapısal denetimde yok (§11.3) |
| 11 · 63 | D: planlama botu sınıfı | `Tests/EditMode/CampaignBot.cs`, örnekli sınıf (harekât başına durum: taslak emrin günü, tür başına açılan emir, sayaçlar); Camp8 ve `FacilityBotTests` çağırır… |
| 11 · 64 | D: `donus` eşitlik kuralı | Stokta en az olan tür; eşitlikte **bu harekâtta en az emri açılan**, sonra açılış sırası (tasarım: stok, sonra açılış sırası) |
| 11 · 65 | D: girdi ayırma | Emir yalnız bütün girdiler serbest hammaddeden karşılanıyorken açılır ve hemen tam ayrılır; 3 vardiyada başlamayan taslak ve emir açıkken depo dolarsa emir… |
| 11 · 66 | D: takma | Kaynak depo ya da saha rolü dışındaki kişi; kişiye bağlı eşya, dinlenen köpek ve EMP hücresi taşıyan yuva atlanır; `donus`ta (rol dizini + baskın sayısı) ile… |
| 11 · 67 | D: köpek emri | Erzak ≥ bedel + 3 × vardiya tüketimi iken |
| 11 · 68 | D: botun sınıf süzgeci | `Baskin.IsSpecialist`: kapıya giriş, puanın can/yol/kilit ve "ekip ayakta" sayımı, sürükleyici, ikinci uzman, `TeamFits`, `Predict` kilit/donma, Camp8… |
| 11 · 69 | D: aday süzgeçleri | tel/kalas makine erişimi ya da (kalas) niyet hattı; taret hattı makine erişiminden geçen yön; vinç hattının ilk dolu karesi gövde, yük ya da… |
| 11 · 70 | D: puan terimleri | Aygıt × kalan tur (`Turret` 4 hattı erişimden geçiyorsa, `Decoy` 2 × yakındaki ≤ 2 makine, `Winch` 6 hattın ilk dolu karesi gövde/yük ise); `Barrier` 3 ×… |
| 11 · 71 | D: ağırlık taraması | Tohum 1–20, "iyi" küme, `t8donus`: tasarım değerleri, ×2 ve ×0,5 (`BOTW`) baskın başına ölü ve başarıda gürültü içinde (±0,03, ±1,4p); tasarım değerleri kaldı |
| 11 · 72 | D: köpek taşıyıcı | Tesisin girişinde köpeğin çıkışa yolu (köpek yürüyüşüyle), ara odada en geç kalan uzmanın yolu tam ağırlıkla |
| 11 · 73 | D: sayaçlar | R: `odul arac gear rec carry gx gfx dogSteps capSteps`; C… |
| 11 · 74 | D: test düzeltmesi | `FacilityGearTests`'te geri almadan sonraki eski birim başvurusu `Fresh(B, u)` ile (4 yer) |
| 11 · 75 | D: köpeğe fırlatma | Bot yükü köpeğe fırlatmaz; köpek yükü yalnız yerden getirir; taşıyıcı köpek havlamaz |

**Aşama 9 · 12. tur**

| Tur ve madde | Kararın konusu | Karar (kısa) |
|---|---|---|
| 12 · 1 | Dördüncü kart | Köpeğin ekip kartı; aygıt ve tutsak kartsız |
| 12 · 2 | Birden çok köpek | Tek Card 4: seçili köpek, yoksa ayaktaki ilk; 4 tuşu köpekler arasında döner; eşya satırı "+n köpek" |
| 12 · 3 | Card 4 genişliği ve yerleşim | 280 px; Team kabı `ContentSizeFitter`, `childForceExpandWidth` kapalı |
| 12 · 4 | Ekipman tuşu | Yeni **R**; tek ekipman satırı, R ve satır tıkı eylemler arasında döner |
| 12 · 5 | Ekipman satırı sayısı | Uzman başına tek satır (örme setinde tel/kalas döngüyle) |
| 12 · 6 | Köpek ve tutsak komutları | Onların kartında E satırları; Q satırı yok |
| 12 · 7 | Seçim kimliği | Uzman harfle (`Selected`), köpek ve tutsak birim kimliğiyle (`SelectedOther`); harf araması `IsSpecialist` |
| 12 · 8 | Aygıt seçimi | Seçilmez; bilgisi hover'da ve tahta etiketinde |
| 12 · 9 | Sınıf süzgeci | Onay, kaçış uyarısı, Unused, KAPIDA paydası, sonuç sayıları `IsSpecialist` |
| 12 · 10 | Kalan tur gösterimi | Aygıt `Turns` ("{0} TUR"); tutsak `CaptiveLeft` (yakalandığı tur `Turns`, sonra `Turns + 1`; 0'da "SON TUR") |
| 12 · 11 | Aygıt hattı önizlemesi | Bridge'de public parçalarla yeniden kurulur; Core'un evresiyle eşlik testi |
| 12 · 12 | Havla önizlemesi | Erim boyası + "etki niyetlere tur sonunda yansır"; niyet değişmez |
| 12 · 13 | Parça modelleri | Yol E: var olan mesh asset'leri ve `Rev3MaterialLibrary` malzemeleriyle kurgu prefab'lar… **Kararlaştırıldı (13a soru 1):** yol E; three.js kurulumu kullanıcı onayı bekliyor |
| 12 · 14 | Kepenk, niş ve tutuş modeli | Kütüphanede tek parça (kit başına değil); duvar bloğuna süs, kalkık kepenk zeminde rulo |
| 12 · 15 | Kalkık kepenk notu (onuncu tur 9) | Not yok; model okunur |
| 12 · 16 | Engel kırılınca | Yerine moloz konmaz |
| 12 · 17 | Renk ve ölçü | Renkler `RaidPieceLibrary` (ekip `lib.good`); çizgi ve alfa bkz. 51 |
| 12 · 18 | Depo yeri | Kitlerin altında ikinci sıra, yalnız tesis kipinde (y bkz. 64) |
| 12 · 19 | Yığın sayısı | Kart yüzünde değil yuva etiketinde (bkz. 62) |
| 12 · 20 | Simgeler | `DeskSprites` adla (`picto_<tür>`); diziler ve prefab'lar değişmez (çizim bkz. 60) |
| 12 · 21 | Üç girdi | Defter sayfası 3 satır; tarif kartı 3 sütunda adsız (simge + ×n) |
| 12 · 22 | Kapalı sayfalar | Açık sayfalardan sonra, "KAPALI · {YZ} sunucusundan", sürüklenmez |
| 12 · 23 | Erzak bedeli | Girdi satırı gibi; tahminde `NoSupplies` |
| 12 · 24 | K19 tahmini | Tahmin eksik kit üretimini söyler |
| 12 · 25 | Ödül seçim yüzeyi | Operasyon raporunda satır ve ÖDÜLÜ SEÇ; seçim olay tutanağıyla aynı görünümün ikinci örneğinde… **Kararlaştırıldı (13a soru 2):** tasarımdaki gibi |
| 12 · 26 | Ödül komutu yolu | `GameController.ExecuteBoardCommand(s => s.ChooseServerReward(r, c))` |
| 12 · 27 | Tepsi önceliği | Açık olay tutanağı önce; ödül kâğıdının kendi tıkı her zaman |
| 12 · 28 | Bekleyen ödül kilidinin görünmesi | Tek yardımcı `FacilityRewardText.Pending`: fiş "ÖDÜL SEÇİLMEDİ", fon kısa uyarısı örtmez, CRT ve telsiz ipucu |
| 12 · 29 | Son ekranda bekleyen ödül (on birinci tur 38) | Sunum korur (`Pending` `Ending == null` ister); Core boşaltmaz |
| 12 · 30 | Kazanımların görünmesi | Görev fişinde KAZANIM satırı; Kulak gözetim önizlemesinde |
| 12 · 31 | Köpek hurdaya | Sunum reddeder ("Köpek hurdaya atılmaz.") |
| 12 · 32 | Dinlenen köpek | Kullanım satırında damga, takarken uyarı, kadro raporu satırı |
| 12 · 33 | Ekipman raporu satırı | Tek SONUÇLAR satırı (kelepçe, köpek); ödül satırından önce düşer |
| 12 · 34 | Olay nişanı `Side == 'p'` | Değişmez (aşama 10) |
| 12 · 35 | Bitti ölçütü testi | Tek PlayMode harekâtı; ilk örnekler gerçek giriş (tuş, fare), tekrarlar sunumun aynı karar ve bırakma yolundan; hammadde ve erzak kurulumla (bkz. 79) |
| 12 · 36 | Rehberli eğitim baskını | R, 4 ve uzman olmayan seçim kapalı |
| 12 · 37 | Lokalizasyon akışı | `strings.txt` elle; `UpdateTable` çalıştırılmaz |
| 12 · 38 | Sahne ve prefab işleri | Tek seferlik betik, açık sahne kirliyse durur |
| 12 · 39 | P1: arayüz üyeleri | `IRaidCommands.SelectUnitId/ArmGear/ChooseGear` varsayılan gövdeli; Digit4 denetimi yeni `KeyMap_RGear_Digit4_Free`'de |
| 12 · 40 | P1: kurallar satırı | Ekipmanlı kadroda "R: ekipman", köpekli kadroda "1–4"; ikisi de yoksa aşama 7 metni aynen |
| 12 · 41 | P1: ekipman satırının ayrıntısı | Kısa: "tel 1 · kalas 2 · {n} hak", "hat {0} kare · {1} tur · n hak"; "yetenek yerine" yazılmaz; EN'de "left" ("Wire kit · 2 left") |
| 12 · 42 | P1: kokla satırı | Oda koklanınca da kalır, nedeni "bu oda koklandı" |
| 12 · 43 | P1: tutsağın kartı | Kalan tur başlıkta ("DRON · TUTSAK · 3 TUR", "DRON · SON TUR"); köpek kartı "KÖPEK · {sahip}" |
| 12 · 44 | P1: üç kartın yeri (sapma) | Bayrak kapalıyken de kartlar 345,33 → 345 px; sol kenarlar 6 / 357,33 → 357 / 708,67 → 708 (en çok 0,67 px); kap 1060 (sağ dolgu 7), Strip ve Shadow aynı. … |
| 12 · 45 | P1: kart katlaması | `MaxCardRows` 10; aşılınca iki ya da daha çok kullanılamayan hamle "Diğer · {n} hamle kullanılamaz"; `ChooseAction` satır dizinini `actionRowMap` ile çevirir |
| 12 · 46 | P1: birim adları | `UnitType.Name` ikinci kez `Loc.T`'ye verilmez; `ObjNames` verilir |
| 12 · 47 | P1: önizleme satırları | Kalasta "Kesilen niyet: n"; telde "yolu kesilen makine" ve yemde "ona yönelecek makine" satırı yok |
| 12 · 48 | P1: hover'da can | Tel, kalas, hurdada "Can" satırı yok |
| 12 · 49 | P1: kişi satırında kalan hak (§8) | Yapılmadı |
| 12 · 50 | P2: doğrulama yolu | Parça 1–2'de editör kilitliydi: tek seferlik prefab betiği batchmode kopyasında `-executeMethod` ile, çıktılar projeye kopyalandı |
| 12 · 51 | P2: çizgi ve alfa (kural 7 sapması) | `gearLinePx` 3, `decoyPathAlpha` .6, `barkAlpha` .18 `RaidPieceLibrary` asset'inde; `raid_tokens.json` → `RaidTheme` değil |
| 12 · 52 | P2: `Marks.Tags` | Uzman olmayan birimin etiket adı ve kırmızı bayrağı köprüden |
| 12 · 53 | P2: NİYETSİZ notu | `Idle` ya da `Intent == null` olan makinede (bkz. 73: kapanışta ikiye ayrıldı) |
| 12 · 54 | P2: kepenk modeli | Düz levha + beş kaburga kutusu |
| 12 · 55 | P2: sandık nişinin yüzü | Dirs sırasıyla ilk duvar olmayan komşu |
| 12 · 56 | P2: kalkık kepengin rulosu | Duvar hattına paralel, kasa tepesi y ≈ 0,71 |
| 12 · 57 | P2: KOKLANDI notu | Son ileri geçit karesinde |
| 12 · 58 | P2: `OnEffect` | Yeni telsiz satırı yok; yalnız sunum kopyası (`dog-down`, `scrap`, `device-break`) |
| 12 · 59 | P2: tahta testi | `Board_GearMarksAndDecor` `FacilityGearSceneTests`'te |
| 12 · 60 | P3: piktogramlar (K20 sapması) | PIL değil `Rev3BoardTextureBaker`'ın vektör çizicisi |
| 12 · 61 | P3: `DeskSprites` kaydı | Rev3 pano sprite'larından yalnız sekiz yeni ad (yedi piktogram + `icon_erzak`) |
| 12 · 62 | P3: dinlenen köpek ve yığın | "DİNLENİYOR · n V" kullanım satırında; yığın sayısı yuva etiketinde ("YARDIMCI · ×2") |
| 12 · 63 | P3: uzun ad | Eşya kartında ad en çok %20 küçülür (yalnız yeni türlerde); defterde iki satıra sarılır |
| 12 · 64 | P3: depo ikinci sırası | y 571 (tasarımda 566) |
| 12 · 65 | P3: raporda 7 yuva | Düşme sırası: değişmemiş YZ alarmı, ekipman satırı, büyük sandık, "0" sivil, karşı saldırısız YZ alarmı; ödül satırı düşmez |
| 12 · 66 | P3: arşiv sırası | Ödül kâğıdında yok: tepsiye tık her zaman kâğıdı getirir, etiket hep "EVRAK · SUNUCU ÖDÜLÜ" |
| 12 · 67 | P3: kâğıdın yüzü | Künye "KYGA · SUNUCU ÖDÜLÜ", başlık YZ'nin adı, kişi satırı bina, sağ sütun "ETKİ"; tutanağın çapasının 12 mm sağında, 5° dönük; EVRAK tepsisinin çocuğu |
| 12 · 68 | P3: KAZANIM satırı | Görev fişine beşinci SAHA satırı (yalnız tesiste ve kazanım varken) |
| 12 · 69 | P3: cevap paketinin satırı | Açıklama yerine "Cevap tarifi"; Risk satırında temel tarif notu; Söküm anahtarı notsuz |
| 12 · 70 | P3: ekipman satırı | Etiket "Ekipman", değer "kelepçe harcandı (…) · köpek dinlenecek (3 V)" |
| 12 · 71 | P3: EN hak metni | "raid charges {0} · {1}" |
| 12 · 72 | P3: dinlenen köpek önizlemesi | Not yerine sarı uyarı |
| 12 · 73 | P4: niyetsiz makine (inceleme 1–2) | Kilitlenen makine (`Idle`, niyet duruyor): hover "Sonraki planında niyetsiz", not KİLİTLİ; planı atlanan (`Intent` yok): hover "Bu planı niyetsiz geçiriyor"… |
| 12 · 74 | P4: önizleme eşliği olay odalarında (inceleme 5) | Yük turnikeye bakmaz (Core `PullLoad`); çekilen birim ve yem hidrolik yağda kayar (`SlideEnd`, Core `Slide`); kurulum önizlemesinde vincin karesine çekilmez… |
| 12 · 75 | P4: kartsız oyunda bekleyen ödül (inceleme 6) | `Pending` panolu oyun ister (`sim.Board != null`) |
| 12 · 76 | P4: tek kip (inceleme 8) | Kullanılabilir seçenek yokken de E ekipman kipini, R hamle kipini kapatır; ekipmansız uzmanda R hiç etkisiz |
| 12 · 77 | P4: köpek yükle kalırken son çıkış (inceleme 9) | Çıkış karesinin hover'ında zafer işareti yok, "Köpek yükle kalırsa yük kaybedilir" satırı |
| 12 · 78 | P4: son ekran testi (inceleme 10) | PlayMode `FacilityServerRewardPlayTests.Ending_HidesPaper_TrayLightAndLabelOff`: tepside ve eldeyken `Ending` → kâğıt gizli, ışık ve etiket söner, komut yok |
| 12 · 79 | P4: bitti ölçütü testinin kurgusu | Yedi ödül kurulumla bekler (`PendingReward`, kayda girmez), kâğıttan seçilir (ilki EVRAK tıkı + 1 + Enter, öbürleri `Decide(0)`); iş emri ilki gerçek sürükleme… |
| 12 · 80 | P4: PlayMode kiti | `FacilityPlayKit.Standing` ve `ThroughGate` uzmana süzüldü |
| 12 · 81 | P4: ayraçlar (inceleme 11) | `string.Join(", ")` ve `" · "` `Loc`'suz kalır |
| 12 · 82 | P4: "1 hasar" (inceleme 11) | Metinde sabit kalır |
| 12 · 83 | P4: `SiginakServerReward.Present` (inceleme 11) | Masa kilitlerini atlar (`Take(true)`); değişmedi |
| 12 · 84 | P4: kirlenen yedek font (inceleme 3) | `LiberationSans SDF - Fallback.asset`'e ı ve İ eklendi; aşama 9 kodu değil: eski testlerin fontsuz TMP'si varsayılan fonta düşer. Dosya geri alınmadı |

**Aşama 10 · 13. tur**

| Tur ve madde | Kararın konusu | Karar (kısa) |
|---|---|---|
| 13 · 1 | Kayıt | `CurrentVersion` 19 kalır; tesis izi f3 → f4; Digest değişmez |
| 13 · 2 | Alt bayrak | Sayım, öğrenme, önlemler, cevap etkileri ve sayfaları `facilityCountermeasures` (true) arkasında |
| 13 · 3 | Olay nişanı | `TeamLike` (uzman, yem, erimde havlayan köpek) ekip; aygıt, susan köpek, tutsak 0; yağda yalnız uzman; alt bayrağa bağlı değil; ayrı adım (A0) |
| 13 · 4 | Sayım yeri | `BState`'te altı int; raporun kuyruk alanı 21; `ResolveRaid`'de tavanlı toplama; başarısız baskın da sayar |
| 13 · 5 | Sayılanlar | Koşu darbesi değdiğinde; vinçte yalnız makine çekişi; lokum yıkımı; tel/kalas birer; kelepçe; havla/getir/kokla; aygıt kurulumu |
| 13 · 6 | Pusu isabeti | Varsayılan sayılmaz; ölçüm anahtarı `facilityTallyAmbush` (true = lafız). **Onaya açık (S1)** |
| 13 · 7 | Rol yetenekleri | Sabotaj, EMP, kanca, Madenci dinamiti sayılmaz |
| 13 · 8 | 2. kademe | Sayılmaz (sayaç durur) |
| 13 · 9 | Öğrenme anı | İlk tamamlamada, `ChangeTrust`'tan sonra, oyun bitmediyse; eşik en az 1 |
| 13 · 10 | Eşitlik | `Learned`'de son girdisi en eski (hiç = en eski), sonra tablo sırası; `CounterAt` alanı yok |
| 13 · 11 | Günlük | `FacilityState.Learned` (gün, bölge, sınıf, kademe) |
| 13 · 12 | Emir kopyası | `RaidParams.Counters/Answers` (K23 kalıbı) |
| 13 · 13 | Cıvatalı ayak | İmza = dron ve bakım dışı her tür; itme, kanca, vinç, yağ kayması yok; çarpma hasarı yok; koşu hedefi değil; Söküm sabotajda söker ve iter; sökük dönüşte kalır |
| 13 · 14 | Çift cidar | Odada 2 duvar (k1 G/A, k2 H de); RNG'siz kısayol sırası; tek hamlede 2 hak; Delici 1; dinamit almaz |
| 13 · 15 | Kesici kol | Makine niyet hasarı tele k1, kalasa k2 +1; itme çarpışması dışarıda; Çelik tel +1 can |
| 13 · 16 | Kısa bağ | Kelepçe −1 tur (en az 1); k2 bırakılan planlanır (+1 `Rnd`); Yedek röle +1 |
| 13 · 17 | Koku yayıcı | Her odada kademe kadar; RNG'siz yer; 1 can; köpeğin üç komutu Manhattan ≤ 3'te kapalı; atışı keser; makine kırmaz |
| 13 · 18 | Parazit | Aygıt −1 tur (en az 1); Çift zemberek +1 |
| 13 · 19 | Makine aklı | Plan anındaki pusu ve taret hattına varış cezası 6; `Rnd` sayısı aynı |
| 13 · 20 | Cevap kapsamı | Ekip düzeyinde (`RaidParams.Answers`); sayfa temelin yerinde, aynı `Id`, ad ve +1 devre; temel açık değilse sayfa yok. **Onaya açık (S2)** |
| 13 · 21 | Söküm kiti | Kit yolu, eksik kit ve otomatik yol cevap sürümünü kullanır; `Reward` false |
| 13 · 22 | Taslak iptali | Cevap seçilince temellerinin başlamamış taslağı silinir; başlamış emir sürer |
| 13 · 23 | Kaplama yeri | Oda üretimine girmez; `RoomBoard`, `ReturnBoard` (çift cidar) ve plan kopyası |
| 13 · 24 | Fx ve telsiz | `bolted`, `unbolt`, `dwall`, `cut`, `jam`; yayıcının kırılması nesnelerin `break`'i (`Obj = "yayici"`; bkz. 50); satırlar `Loc` ile Core'da |
| 13 · 25 | SimEvent | Eklenmez |
| 13 · 26 | Balance | 17 alan (§11.1); asset'e yazılmaz |
| 13 · 27 | İmza | `\|T`, birimde `U` (bkz. 49), `\|D` yalnız varsa |
| 13 · 28 | Rapor | Kuyruk `…\|CargoDog\|Tally`; `RoomMachine` `:1` yalnız sökükse |
| 13 · 29 | Okuma sözleşmesi | §11.2 listesi |
| 13 · 30 | Ölçüm kilitleri | `t10yokK0` = `t8yok` (80 tohum); `t10tekK0/donusK0` tohum 1–20 = A0 exe'si |
| 13 · 31 | `ODUL=onlem` | Cevap paketinde tarif yalnız önlem ≥ 1 ve temel açıkken |
| 13 · 32 | `TAK=hic` | Üretir, takmaz |
| 13 · 33 | R/C anahtarları | `cm tl tlb learn ans cmfx plates sqHp sqMv sqPw rook half tak`; `learns cmEnd nLearn rwCevap prod_* cmRaids_* penCarry carryItems` |
| 13 · 34 | RaidBot | Yayıcı terimi `W.Emitter`; k2'de `AmbushValue` × `W.AmbushSeen`; kısa tarama |
| 13 · 35 | `ozet.py` | `--fark`; "aşama 10" bölümü |
| 13 · 36 | Hedef | Bitti ölçütü "ölçülür"; hedef tutmazsa denge değiştirilmez, seçenekler belgeye (S4) |
| 13 · 37 | ÖNLEM satırı | Tasarım: altıncı SAHA satırı (Field 6); sığmazsa KAZANIM ile tek satır. Uygulanan: sığma yolu (bkz. 71) |
| 13 · 38 | Görünürlük | Önlem ya da sayaç varken; sayaç hover'da ("YZ gözlemi n/6") |
| 13 · 39 | ÖNLEM teleksi | `PlayReveal`'da "BÖLGE ÖZGÜR"den sonra, Alarm, `Learned` primed; önlemsizse yok |
| 13 · 40 | Sonuç teleksi | Yeni `SiginakOperation.telex` bağı; satır `Begin`'de kurulur, rapor inerken basılır (bkz. 74); yalnız tesis kipinde. **Onaya açık (S3)** |
| 13 · 41 | Kırmızı kalem | `Counter` katmanı, `plan.counter`; bilinmeyen paftada yok |
| 13 · 42 | Robot etiketi | Yalnız cıvatalı ("CIVATALI") |
| 13 · 43 | Hover, neden, gözcü | Makine, tel, kalas, yayıcı ve çift cidar hover'ı; köpek, lokum ve koşu nedeni (Core kuralı sökülmüş kopyada); gözcü satırları (§6.3 "Uygulanan hâl (aşama 10)") |
| 13 · 44 | Önizleme | `FacilityGearView` Core'un `DeviceTurnsFor/ClampTurnsFor`'unu okur |
| 13 · 45 | Defter ve kâğıt | Sayfa Core'dan; el notu, `AnswerDesc`, `NewPageLine`; depo değişmez |
| 13 · 46 | Telsiz günlüğü | `Learned`'den satır |
| 13 · 47 | Yayıcı modeli | Yol E kurgu prefab, `RaidPieceLibrary.scentEmitter` |
| 13 · 48 | Lokalizasyon | `strings.txt` elle; `UpdateTable` çalıştırılmaz |
| 13 · 49 | A: imza harfi | Sökük birim eki `U` (`u` aşama 8'de kalan tur) |
| 13 · 50 | A: yayıcının kırılma Fx'i | Nesnelerin `break` dalı (`Obj = "yayici"`), telsiz "{0} kırıldı."; `device-break` aygıt birimi içindir |
| 13 · 51 | A: Söküm telsizi | "{0}: cıvatası söküldü." ({0} makine) |
| 13 · 52 | A: yayıcı bağ kuralı | Tasarımdakinden güçlü: batıdan ulaşılan her kare ve hedef korunur; ayrılmış kareler (geçit, menfez, donanım erişimi, kepengin iki yanı) dışlanır |
| 13 · 53 | A: Balance sayısı | 17 alan |
| 13 · 54 | A: Desc metinleri | Yıkım 2: "Hedef odada da {0} duvar {1} hak ister."; köpek tek satır "Her odada {0} koku yayıcı: {1} kare içinde köpek komut alamaz." |
| 13 · 55 | A: `CountTally` | Public değil; okuyucular `TallyArray`, `RaidTally`; ek okuyucu `DoubleWallCost` |
| 13 · 56 | B: kayıt testinin yeri | Öğrenmeli harekâtın yeniden oynatması `FacilityCounterTests.Record_F4_LearningCampaignReplays`; `FacilityKit.State` önlem ve iş emri satırlarıyla bütün kayıt… |
| 13 · 57 | B: cevap seçimi ve iptal yeniden oynatmada | Testte bekleyen ödül elle kurulur; Camp8 REPLAY'de cevap bölgelerine ulaşan harekâtlar `is` satırıyla karşılaştırılır (`ODUL=onlem` dahil, §11.6) |
| 13 · 58 | B: simülasyon testinde sayaç | Tahtaya doğrudan yazılır; sayım noktaları A'nın testinde, bağ (`Moves.Tally`) ayrıca |
| 13 · 59 | B: `RaidCounters` hep 6 öğe | Öğrenme yokken sıfırlar (tahtada sayımı açan `Count == 6`); `Facility.Counter` ilk yazışa kadar boş |
| 13 · 60 | B: iptal ve basılmış fiş | `ChooseServerReward` pano kilidini sınamaz (aşama 8'den beri); fiş basılmışken taslak silinirse fiş eski emri gösterir; kapanışta emir yoktur. Zararsız, nadir |
| 13 · 61 | C: tutarlılık sayacı | R'de `tlb` (botun eylem ve efekt sayaçlarından, `RaidBot.TallyMap`); vincin makine çekişi için `Tally.MachinePulls` (çekiş başına bir: (tur, vinç, makine)… |
| 13 · 62 | C: yayıcı terimi | `W.Emitter` (6) köpek başına, köpek ayaktayken ve `EmitterBlocks` iken; yalnız köpek önlemli emirde ve `GEAR=kullan`'da. … |
| 13 · 63 | C: makine aklı terimi | `W.AmbushSeen` 0,25: aygıt 2. kademede (makine aklı) kurulu pusunun kaba beklentisi `Ambush` × 0,25. … |
| 13 · 64 | C: cezalı taşıma | Eşyanın önlem sınıfı: örme seti örme, lokum yıkım, taret ve yem aygıt, **vinç aygıt ve itme**, kelepçe ele, köpek köpek; eşya-baskın başına |
| 13 · 65 | C: `emit` | Yayıcının susturduğu köpek-tur `cmfx`'te; "makine aklının kaçındığı plan" ölçülmedi (Core'da Fx yok) |
| 13 · 66 | C: `ODUL=onlem` | `CampaignBot.AnswerChoice` (statik, test edilir); seçilen cevap `RewardAnswer` (C `rwCevap`) |
| 13 · 67 | C: `TAK=hic` | `CampaignBot.TakeGear = false`: `EquipGear` hiç çalışmaz; emir ve stok hedefi aynı |
| 13 · 68 | C: regresyon süzgeci | `K10` tasarımın listesi + `tlb` |
| 13 · 69 | C: REPLAY kümesi | Tohum 1–10 × ODUL {tarif, kazanim, onlem} × ARAC {yok, tek, donus} × `facilityTallyAmbush` {false, true} + `TAK=hic` tohum 1–10 |
| 13 · 70 | C: ölçüm etiketleri | Tasarım §8.2'nin on etiketi, 80 tohum; sırayla |
| 13 · 71 | D: ÖNLEM satırının yeri (sapma) | Fiş beş SAHA yuvasında kalır (`LiberationMapBuilder.SlipFieldsMax = 5`; sahne ve `DeskBridgeTests` 5); boş yuva varsa ayrı satır, beşi doluysa KAZANIM ile… |
| 13 · 72 | D: glyph | `ic_robot_s` (tasarım `ic_bolt_s`) |
| 13 · 73 | D: iki önlemin adı | 24 harfi aşarsa değer "2 önlem", adlar hover'da; kısa çiftler sığar ("Kesici kol · Parazit") |
| 13 · 74 | D: sonuç teleksinin zamanı (sapma) | `Begin`'de kurulur, rapor inince (kamera harita pozunda) basılır; `RaidLog` girdisi başına bir kez; Abort ve yeni oyun bekleyeni siler |
| 13 · 75 | D: EN fiş taşması (aşama 9'dan) | Olaylı bölgede beş satırlı fiş EN'de (görev cümlesi iki satır) BASKIN EMRİ VER'i ~19 px @1080 kadrajdan taşırıyor; önlem defteri kapalıyken de. Düzeltilmedi |
| 13 · 76 | D: kalan tur ve lokumun "kalan" satırı (sapma) | `FacilityGearText.CaptiveLeft` kelepçenin tam süresini `Baskin.ClampTurnsFor`'dan alır; lokum önizlemesinin "Duvar yıkılır · kalan" satırı `GearLeft − bedel`… |
| 13 · 77 | D: cevap sayfasının notu | El notu cevabın notu; "{YZ} sunucusundan" yerine kaynak `Source`'ta iki YZ ("VERITY · cevap: HALCYON") |
| 13 · 78 | D: çift cidarın 3B işareti | Yok; hover, gözcü, lokum nedeni ve paftanın kırmızı kalemi söyler |
| 13 · 79 | D: `plan.counter` tokenı | #B3261A α 0,92, `tokens_siginak.json` ve `SiginakTheme.asset`'e elle (içe aktarıcıyla aynı biçim; `Tokens_AssetIsUpToDate` yeşil) |
| 13 · 80 | D: yayıcı parçası ve küçük resim | `ScentEmitter.prefab` (kurgu, yol E; çelik kaide, zeytin gövde, pirinç ağız, 304 üçgen), `RaidPieceLibrary.scentEmitter`, yedek soğutma ünitesi ×… |
| 13 · 81 | E: vincin cıvatalı makinede yinelenen satırı (inceleme 4) | Kabul edildi, düzeltilmedi: vinç her makine evresinde yeniden dener, Fx `bolted` ve telsiz satırı aygıtın ömrü boyunca (en çok 3 evre) her evrede yazılır |
| 13 · 82 | E: sunumdaki kural kopyaları (inceleme 5) | Core'a public `Baskin.WinchPulls(B, t)` (aygıt değil ve cıvatalı değil); `RunWinch` ve `FacilityGearView.WinchLine` onu okur. … |
| 13 · 83 | E: yayıcı ve uyanma adayları (inceleme 6) | Belgeye not (§6.3); kod değişmedi |
| 13 · 84 | E: bağlantı testleri (inceleme 7) | Değişmedi; §11.6'da "bağlantı testi" diye anılır |
| 13 · 85 | E: İngilizce tekil/çoğul (inceleme 8; D sapma 10) | "Clamps last {0} turn less.", "Devices run {0} turn less.", "Scent emitters in every room: {0}; …", kullanım satırı "+{0} turn" |
| 13 · 86 | E: belge notlarının sunum satırları (inceleme 1, 2) | §6.3 ve §11.6 uygulanan hâlle yazıldı (beş yuva, `ic_robot_s`, teleksin zamanı); ölçüm okumasında SE'ler ölçü başına, ölü / harekât "gürültü sınırında azalma" |
| 13 · 87 | E: `seenLearned` (inceleme 10) | Değişmedi; aşama 11'e not: sayaç yalnız reveal sırasında ilerler |
| 13 · 88 | E: Unity doğrulaması | Parça A–D'de kullanıcının editörü yanıtsızdı ("Main thread operation timed out"); doğrulama batchmode kopyada (aşama 7 tarifi). … |

**Aşama 11 · 14. tur**

| Tur ve madde | Kararın konusu | Karar (kısa) |
|---|---|---|
| 14 · 1 | Kayıt (K1) | Sürüm 19; iz f4 → f5; Digest aynı |
| 14 · 2 | Alt bayraklar (K2) | `facilityDefenseEnabled` (kuşatma, savunma, §9.3) ve yeni `facilityMarks` (izler, onarım); ikisi C# `true`, asset'e yazılmaz |
| 14 · 3 | §9.3'ün bayrağı (K3) | `facilityDefenseEnabled`'a bağlı; ayrı ölçüm anahtarı yok; aşama 0 tabanı aşama 17'de bu kuralla yinelendi, değişmedi. |
| 14 · 4 | Kuşatmanın durumu (K4) | `FacilityState.Sieges` günlüğü (kanat, bölge, kuruluş, karar vardiyası, sonuç, terk); tek kaynak |
| 14 · 5 | Karar vardiyası (K5) | `Step` içinde kurulan: artmış `S.Day`; vardiya ortası: `S.Day + 1`; geçici `stepping` bayrağı |
| 14 · 6 | Sayaçlar (K6) | `S.Counterattacks`/`LastCounterattack*` yalnız kanat düşünce (bırak, kendiliğinden, kaybedilen savunma, kanatsız anlık); kuşatma kurulumu `SimEvent.Siege` |
| 14 · 7 | Bekleyen varken ikinci eşik (K7) | Alarm sıfırlanır, kuşatma kurulmaz, güven −1 (`FallSector(null)`: `S.Counterattacks` artar, `LastCounterattack` boşalır; aşama 10'un kanatsız karşı saldırısıyla… |
| 14 · 8 | Kurulum yolları (K8) | `ChangeAlarm` içinde; dört yol kendiliğinden (`StepAlarm`, `raidres`, `event_decide`, kartsız kapı) |
| 14 · 9 | Savunma emri (K9) | Fon yok, `Attempts` artmaz, `S.Raids++`, bekleyen ödülde red, `PlanningRaidCheck`, EMP hücresi harcanır, `Task = "baskin"` |
| 14 · 10 | Bırak kilidi (K10) | Komutla bırakılan kanada o vardiya baskın yok; kendiliğinden bırakma kilitlemez; başka bölgede hak korunur |
| 14 · 11 | `Step` sırası (K11) | Pano dalından sonra: kendiliğinden bırakma → onarım → `StepAlarm` |
| 14 · 12 | Savunma tohumu (K12) | `Rng.Mix(S.Seed, 0x53494547 + girdi dizini)` |
| 14 · 13 | Dalga türleri (K13) | Dalga başına `Rng.Mix(p.Seed, 0x57415645 + k)` akışı, `WakeType` (imza dahil), kurulumda |
| 14 · 14 | Dalga büyüklüğü (K14) | `facilityDefenseWaveBase + min(kademe, 3)`: 2/3/4, kademe 4'te 4 |
| 14 · 15 | Geliş turu (K15) | Giriş odası `D_k`; iç oda `max(D_k + terk, S_r + 1)`; dalga tek odaya gelir; yerleşmemiş gelen GEÇ'te takipçi olur (rapor `Machines`) |
| 14 · 16 | Yerleşim (K16) | Takipçi önce, sonra dalga; `room.West` sırası, sonra yürüme uzaklığı en küçük boş zemin (y, x); yer/sınır yoksa kuyruk |
| 14 · 17 | Kapı (K17) | Bütün ekip kapıdayken yalnız GEÇ (bugünkü); **kısmi kapıdaki uzman tur sonunda tahtaya döner**; son turda GEÇ yok |
| 14 · 18 | Savunmada kapalı olanlar (K18) | Uyanma, şart takvimi/tetikleri, hazırlık (kırık donanım), `kisa`, başlangıç makinesi |
| 14 · 19 | Röle (K19) | Hedef nesne `role` olur (4 + Siper); `IsTarget` değil; `CheckWin` savunmada kapalı; her kaynak kırar; kırılınca kayıp |
| 14 · 20 | Makine aklı (K20) | `role` vuruşu 11, patlamada +6, konumda röleye çekim (−0,25/−0,1 × uzaklık, `Smart`'tan bağımsız) |
| 14 · 21 | Kazanma/kaybetme (K21) | 10. tur sonunda tahtada ya da kapıda biri ayakta ve röle sağlam; kayıp: ekip düştü, röle kırıldı, geri çekilme |
| 14 · 22 | Gövde (K22) | Kazanınca ekibin odasında yatan ve ekiple gelen çıkarılır; kayıpta ve terk edilen odada ölür |
| 14 · 23 | Deneyim (K23) | Deneyim yalnız kazanılan savunmada (`ReturnPlanningSquad(experience)`) |
| 14 · 24 | Güven ve alarm (K24) | Kazanma −1 × terk; kaybetme −1 −1 × terk; sivil kaybı baskındaki gibi; ayakta kalan sivilin güveni yok; alarm değişmez |
| 14 · 25 | Sivil (K25) | Savunma odalarında kurulur |
| 14 · 26 | Sayım (K26) | Savunma önlem sayımına girer; öğrenme yok (bölge tamamlanmaz) → `seenLearned` etkilenmez |
| 14 · 27 | Önlemler (K27) | Savunmada da geçerli (`Counters` emirde) |
| 14 · 28 | Sonuç dizgisi (K28) | `RaidResult.Encode`'a alan eklenmez; `RaidLogEntry.Mode`, reward sunum alanları |
| 14 · 29 | İz türetme (K29) | Kalıcı türler ziyaretlerin birleşimi ve korunur; nesne türleri son ziyaretten, kümede olmayan (uygulanamayan dahil) silinir |
| 14 · 30 | Kırık donanım (K30) | Emirdeki `donanim` izi hedef odası hazırlığına girer |
| 14 · 31 | Onarım (K31) | İşgaldeki her kanatta listede en önde `facilityRepairPerShift`; `Repairs` günlüğü |
| 14 · 32 | Terk edilen oda (K32) | İzleri sonuçtan bağımsız silinir |
| 14 · 33 | Siper (K33) | Röle +`facilityCoverRelayHp` (1); metin dengeden |
| 14 · 34 | Ad (K34) | `facilityDefenseRelayHp` (belgede `facilityRelayHp`) |
| 14 · 35 | Saldırı baskınında savunma kancası (K35) | Bu aşamaya girmez |
| 14 · 36 | Kip kimliği (K36) | `Mode = "defense"`; `IsLoot` savunmada false; `MissionKind` sektörünki |
| 14 · 37 | Bot politikaları (K37) | `SAVUN` hep/hic/yok, `CEKIL` hic/hep/baski; `S\t` satırı; hedef satırı `t11savun` |
| 14 · 38 | Hedef (K38) | %40–60 dışında kısa tarama ve yalnız yeni savunma alanlarının C# varsayılanı değişir. **Sonuç:** hiçbir değer bandı tutturmadı; hiçbir varsayılan değişmedi |
| 14 · 39 | Sunum (K39) | (1) Şerit prefab'a iki hücre; (2) BIRAK fişte ikinci düğme, iki tıkla; (3) özgür bölgede damga yok; (4) karar vardiyasında 3 ve ilk harita dönüşü kuşatma fişini… |
| 14 · 40 | Okuma sözleşmesi (K40) | Tahta ve simülasyon okuyucuları (§9 "Uygulanan hâl (aşama 11)"; kapanışta `DefenseTurns`, `DefenseWaveCount`, `DefenseSchedulePreview`, `ObjNames["role"]`… |
| 14 · 41 | P1 (D1): `Apply` imzası | `Apply(cells, room, marks, rubble, bool events)`: `events` şart parçası karelerini dışlar (`room.PartTiles(3)`, oda tahtasının dışlamasıyla aynı) |
| 14 · 42 | P1 (D2): Uygulama sırası | Önce kalıcı türler, sonra engeller; her biri liste sırasıyla; dönen liste liste sırasıyla |
| 14 · 43 | P1 (D3): Bağ kuralı | Batı girişinden ileri geçidin **her** karesine ve **odağa** (H'de hedef/röle, G/A'da donanım; odak geçilebilirse kendisi, değilse bir dik komşusu); yalnız… |
| 14 · 44 | P1 (D4): Duvar izi | Kepenk ve sandık nişi karesine uygulanmaz; türetmede plan duvarı ∩ kepenk değil |
| 14 · 45 | P1 (D5): Takvimin saklanması | `RoomSetup.Waves` ve `BState.Waves` (salt okunur liste; Clone/CopyInto'da, imzada yok) eklendi |
| 14 · 46 | P1 (D6): Geliş formülü | Her oda için tek formül `max(D + j, S_j + 1)` (ilk oda dahil: `max(D, 2)`) |
| 14 · 47 | P1 (D7): GEÇ'te kuyruk | Gelmemiş dalga düşer (sonraki odaya yeniden zamanlanır); **takipçiler (dalga 0) her zaman** taşınır, gelmiş ama yerleşmemiş dalga makineleri de |
| 14 · 48 | P1 (D8): `MarkedDevices` | `RaidChain.Setup`'a isteğe bağlı `FacilityPlan plan = null`; `Open` planı verir |
| 14 · 49 | P1 (D9): `RaidChain.Result.Turns` | Savunmada son raporun turu (toplam değil) |
| 14 · 50 | P1 (D10): Telsiz satırları | Savunma tahtası açılış satırı (+ hedef odasında röle satırı), kapıdan dönüş satırı; savunmada donanım kırılınca "{0} kırıldı." |
| 14 · 51 | P1 (D11): Yerleşim karesi | Batı girişi: geçilebilir ve ayakta birim yok (yatan gövde engel değil); yedek: batıdan yürüme uzaklığı en küçük **zemin** (kablo, kapı, menfez değil) |
| 14 · 52 | P1 (D12): `CampaignLiberation.cs` | Yalnız `LiberationRaidReward` sınıfına §1'in alanları; Simulation mantığı yok |
| 14 · 53 | P1 (D13): Röle dönüşümü | Hedef dolap, önlem kaplaması ve izlerden **sonra** röleye döner |
| 14 · 54 | P1 (D14): Kapıdan dönüş | Geçit karesi doluysa (ayakta birim ya da yatan gövde) `NearestOpen` (geçit/menfez değil); yer yoksa kapıda kalır |
| 14 · 55 | P1 (D15): Kazanma denetimi | `Turn > MaxTurns`'de yalnız "ayakta biri var mı"; röle kırılınca tahta zaten kayıptır |
| 14 · 56 | P1 (D16): İlk oda kadrosu | `FrontTiles`: ileri geçitten yürüme uzaklığı en küçük boş zemin (geçit, menfez, parça karesi değil; y, x) |
| 14 · 57 | P2 (E1): Emir gövdesi | Saldırı ve savunma tek `OrderRaid` yardımcısını kullanır; saldırının tohum çekimi, fon ve sayaçları çağıranda |
| 14 · 58 | P2 (E2): `Step` | Gövde `StepCore`'a taşındı, `Step` `stepping`'i `try/finally` ile kurar |
| 14 · 59 | P2 (E3): Okuyucu ekleri | Sözleşmenin dışında `ActiveSiege` (bekleyen ya da süren), `YieldedToday`, `WingMarks`, `RegionFundsDue` (bölge fonu ve güveni verilecek mi), sabitler… |
| 14 · 60 | P2 (E4): `SiegeSector` | Bekleyen **ya da süren** kuşatmanın kanadı |
| 14 · 61 | P2 (E5): Savunma sonucunda sivil | `reward.CiviliansLeft` ve `TrustFromCivilians` 0 (ayakta kalan sivilin güveni yok, K24); sivil kaybı güven düşürür |
| 14 · 62 | P2 (E6): Savunmanın an satırı | Kazanınca `Moment("good", "{0}: {1} tutuldu.")`; kayıpta `FallSector`'un "yeniden işgal altında" anı |
| 14 · 63 | P2 (E7): Kendiliğinden bırakma | Önce "karar verilmedi" anı (bad), sonra `FallSector`'un anı |
| 14 · 64 | P2 (E8): Komut mesajları | `siege_defend`: "{0}: {1} savunuluyor."; `siege_yield`: "{0}: {1} bırakıldı." (dağılmada dağılma metni) |
| 14 · 65 | P2 (E9): Savunma `ResolveDefense` denetimi | Kanat alınmış, etkin sektör ve süren kuşatma kanadıyla aynı; değilse "Baskın hedefi geçersiz." |
| 14 · 66 | P2 (E10): Ortak yardımcılar P1 dosyalarında | `Baskin.DefenseWaveSize`, `RaidChain.DefenseRelayHp` (P1'in formülleri taşındı) |
| 14 · 67 | P2 (E11): Eski Siper metni | `strings.txt`'te "Kullanılmayan çeviriler" bloğunun sonuna `#` ile taşındı |
| 14 · 68 | P3 (F1): Satırlar | `S\t` yalnız savunma açıkken; R ve C'nin aşama 11 anahtarları ana bayrak açıkken hep (kilitte atılır) |
| 14 · 69 | P3 (F2): Komutlar nerede | `CampaignBot.Siege` (Camp8 ve testler ortak); sayaçlar `Defended`, `Yielded`, `DefendRefused` |
| 14 · 70 | P3 (F3): `baski` | Turun başında karar; o odada kalıcı |
| 14 · 71 | P3 (F4): Savunmada kapalı bot kuralları | Donanım hedefi, `bodyRescue`/`holdWin`/`BodyCost` |
| 14 · 72 | P3 (F5): Tutmada kapı | Herhangi bir uzmanı kapıya sokan aday (yürüyüş, sürükleme, menfez) elenir; `GateIdle` çalışmaz |
| 14 · 73 | P3 (F6): Hayalet makine | Sonraki tur gelecek makinenin tahmini karesi + türünün yürüyüşü (BFS, birimler sayılmaz); engel, taret, pusu adayları ve değerleri, pay `WaveGhost` |
| 14 · 74 | P3 (F7): GEÇ'in puanı | Savunmada `pass` ve `CanPass` kazanç gibi; çekilirken `GateScore` |
| 14 · 75 | P3 (F8): `siegeNone` | `S.Counterattacks − birak − kendi − dustu` (günlükten) |
| 14 · 76 | P3 (F9): `recapFunds`, `regionGain` | Gerçek verilen değer (`RegionRepeat`'te 0) |
| 14 · 77 | P3 (F10): Savunmanın sayaçları | Savunma `raids`/R ölçülerine girmez; `gearUsed`, iz defteri ve `marksSeen`'e girer |
| 14 · 78 | P3 (F11): **Hedef tutmadı** | Varsayılanlar değişmedi. **Öneri (Core, ayrı iş):** savunmada tutulan odaya amaç: (a) sızma, (b) dalganın girişi tek kenara yığılmasın, (c) pusu hattı sınırı; sıra a > b > c |
| 14 · 79 | P3 (F12): `--esli` savunma başarısı | Savunmasız tohum atlanır (None) |
| 14 · 80 | P4a (F1): Kuşatma kipinin koşulu | Fiş kuşatma kipinde: karar vardiyası, savunma sürüyor ya da bölgede emir verilecek başka kanat yok. … |
| 14 · 81 | P4a (F2): Kuşatma kipinin SAHA'sı | BÖLGE, İMZA, DALGA, İZ, RÖLE; TEHDİT kalkar (kademe DALGA hover'ında), OLAY yok (savunmada şart takvimi kapalı), KAZANIM/ÖNLEM yok (Siper RÖLE'de) |
| 14 · 82 | P4a (F3): Karar vardiyasında cephe fişinin notu | Kısa "ÖNCE KARAR VER" (düğme yanındaki not yuvası, ≤ 20 harf); uzun gerekçe hover'da Core'unki |
| 14 · 83 | P4a (F4): KUŞATMA'nın EN'i | "SIEGE" (tasarımda "UNDER SIEGE") |
| 14 · 84 | P4a (F5): Kartlı kapanışın duyurusu | Rapor olmadığı için teleks: `SiginakMap` kuşatma günlüğünü izler (primed; yeni oyun ve kayıttan devam basmaz), kurulan kuşatma ve kendiliğinden bırakma birer… |
| 14 · 85 | P4a (F6): Kendiliğinden bırakmanın kartlı teleksi | "YZ KARŞI SALDIRISI · … kanat bırakıldı (karar verilmedi) · sivil güven −1" (Alarm) |
| 14 · 86 | P4a (F7): `ShiftReportInput` alanları | `siege`, `siegeRegion` + `siegeFall`, `siegeFallRegion`, `siegeAuto` (tasarımda yalnız ilk ikisi) |
| 14 · 87 | P4a (F8): Alarm gözü ve lamba | Eşik = kuşatma kuruldu ya da kendiliğinden bırakmadan fazla düşüş; lamba her düşüşte |
| 14 · 88 | P4a (F9): Komutla bırakmanın kapanış kutusu | "YZ KARŞI SALDIRISI · … kanat bırakıldı."; teleks ve harita dizisi yalnız komutta (`CounterMap` false) |
| 14 · 89 | P4a (F10): Savunma raporu | Damga TUTULDU / KAYBEDİLDİ (DÜŞTÜ'nün EN'i kişi için "DOWN"); alarm satırı yok (değişmez); "YZ karşı saldırısı" satırı yok (başlık söyler); deneyim eki yalnız… |
| 14 · 90 | P4a (F11): Final rapor | Savunma BASKIN sayısına ve başarıya girmez (K39-11); savunma açıkken kuşatmanın kötü anları (kurulma, karar verilmedi) kırılma anı değil, kanadın düşüşü… |
| 14 · 91 | P4a (F12): Pafta izleri | Veri `Traces` (uygulanan izler) + `PlanItem.Barrier/Scrap`; çizim mürekkep katmanında (K39-6); `PlanItem` sözleşme testi güncellendi |
| 14 · 92 | P4a (F13): Yeniden özgürleşen bölgenin teleksi | "BÖLGE ÖZGÜR · {bölge} · yeniden özgür" (fon yazılmaz), son baskının `RegionRepeat`'inden |
| 14 · 93 | P4a (F14): BIRAK kapalıyken | Tık onay sormaz, komutu dener (YAPILAMADI + Core gerekçesi teleksten) |
| 14 · 94 | P4a (F15): Tek seferlik betiğin yeri | Kopyada `Desk/Scripts/Editor/` (tasarımda `Assets/Corrigible/Editor/`) |
| 14 · 95 | P4a (F16): Devir fişindeki kutunun yeri | KARŞI SALDIRI ilk kutu (KAPI'dan önce) |
| 14 · 96 | P4b (G1): 10 turun şeride sığması | 8'den çok turda hücreler 8 hücrelik genişliğe daralır (`RaidHudView.FitCells`; 34 → ~26,8 px); 8 ve altında prefab genişliği |
| 14 · 97 | P4b (G2): Dalga işareti | Numaranın yanında küçük "D" (EN "W"), bölge olayının harf kalıbı |
| 14 · 98 | P4b (G3): Barikat turunun "evre/olay satırı" | TELSİZ fişinde barikat turu boyunca "SAVUNMA · BARİKAT · makine yok"; evre çipi aynı |
| 14 · 99 | P4b (G4): Dalga satırı | Görev çipinin ikinci satırı (can yok): sıradaki gelmemiş dalga; dalga kalmadıysa satır yok |
| 14 · 100 | P4b (G5): İÇERİ ÇEKİL onayı | Var olan onay notu (`ConfirmTitle` + `FatalWarning`), düğme onayda "EMİN MİSİN?", Esc vazgeçer; saldırıda GEÇ onaysız kalır (`FacilityRaidHudTests` değişmedi) |
| 14 · 101 | P4b (G6): Sonuç kartının başlığı | "KANAT TUTULDU" / röle kırıldıysa "RÖLE KIRILDI" / değilse "KANAT DÜŞTÜ"; masadaki rapor her kayıpta "KANAT DÜŞTÜ" (P4a) |
| 14 · 102 | P4b (G7): Rölenin adı | Sunum "Saha rölesi" yazar; Core `Baskin.ObjNames`'te `"role"` yok (bkz. §7) |
| 14 · 103 | P4b (G8): `Defense_Lost_RelayBroken`'da rölenin kırılması | Teknisyen'in sabotajı (2 vuruş) — "her kaynak kırar" (K19) |
| 14 · 104 | P4b (G9): Makine gelişinin dili | Etiket "GELDİ" (UYANDI yerine), fiş "makine geldi · 1-A4", "DALGA n geldi: k makine", tahtada "DALGA n" |
| 14 · 105 | P4b (G10): Gelecek tur gelecek makineler | Core `DefenseIncoming`'in tahmini kareleri "robot uyanacak" işaretiyle |
| 14 · 106 | P4b (G11): Kuşatma fişinin 1080 sığması (P4a fişi) | Ödülsüz kuşatma kipinde ÖDÜL satırı kalkar; BEDEL "fon yok · baskın hakkı" ("vardiyanın baskın hakkı" taşıyordu; EN "no funds · raid slot"); EN görev cümlesi… |
| 14 · 107 | P4b (G12): Tek seferlik betiğin yeri | Kopyada `Desk/Scripts/Editor/` (tasarımda `Assets/Corrigible/Editor/`) |
| 14 · 108 | P4b (G13): Kurallar kartı | Savunmada 6. satır (kazanma) dört satırlık savunma bölümüyle değişir |
| 14 · 109 | P4b (G14): Kapı (K17) sunumu | Kapı hover'ı ve fişi savunmada "kapı sığınak değil", "İÇERİ ÇEKİL" |
| 14 · 110 | P4b (G15): Donanım satırı | Saldırıda izle kırık gelen donanım KIRILDI damgalı satır (Max 1); savunmada donanım satırı ve "Kırılırsa hedef odasında…" hover satırı yok |
| 14 · 111 | Kapanış: bitti ölçütü (inceleme 1) | Aşama "oynanır" yarısıyla kapandı; ölçüm yarısı tutmadı (%100) ve **yeni kural eklenmedi**: sızma (78 (a)) ya da başka bir savunma amacı geliştiricinin kararına… |
| 14 · 112 | Kapanış: ölçümün paydası (inceleme 2) | §11.6 başlık tablosuna "savunulabilen kuşatma" ve "kuşatma başına tutulan kanat" satırları; okuma 4'e ek |
| 14 · 113 | Kapanış: rölenin adı (inceleme 4; P4b G7) | `Baskin.ObjNames["role"] = "Saha rölesi"`; `FacilityDefenseRaidText.RelayName` ve `RaidDataSource` (hedef satırı önbelleği, inceleme kartı) oradan okur… |
| 14 · 114 | Kapanış: kartsız kapının kuşatması (inceleme 5; P4a §5) | `DoorVisit.Siege` (yeni alan); kapı cevabı kuşatma kurarsa `v.Siege = true` ve sıfırlanan alarm `v.Alarm`'a yazılmaz; `DoorViewBuilder.Effects` "Karşı saldırı… |
| 14 · 115 | Kapanış: sunumdaki denge sıkıştırması (inceleme 6) | Core okuyucuları `Simulation.DefenseTurns`, `DefenseWaveCount`, `DefenseSchedulePreview(tier)` ve `RaidChain.DefenseTurns/DefenseWaveCount/DefenseSchedule`… |
| 14 · 116 | Kapanış: test gücü (inceleme 7) | `FacilityDefenseTextTests` fişin görev cümlesini, BEDEL'i, BIRAK bedelini ve onayını, DALGA değerini ve başlıkları sabit TR/EN metinle bekler; bitti ölçütü… |
| 14 · 117 | Kapanış: tasarım lafzından sapmalar (inceleme 8) | 47 (D7) ve 7 (K7 yolu) belgeye "onaya açık" diye yazıldı; kod değişmedi |
| 14 · 118 | Kapanış: belge çelişkileri (inceleme 3) | §11.4'ün savunma tuzu günlük dizini (12), §11.1'in röle canı adı (34), §11.2'nin kuşatma durumu günlük (4), §11.3'ün sonuç dizgisi (28) düzeltildi; AGENTS kural… |
| 14 · 119 | Kapanış: Unity doğrulaması (inceleme 9) | Parça P2 sonu ve P4a–P4b'nin Unity doğrulaması batchmode kopyadaydı; kapanışta editör yanıt verdi: editörde boş sahnede derleme temiz, tam EditMode 2129 testten… |

**Aşama 12 · 15. tur**

| Tur ve madde | Kararın konusu | Karar (kısa) |
|---|---|---|
| 15 · 1 | Kayıt (K1) | Sürüm 19; iz f5 → f6 (`GeneratorVersion` 6); Digest değişir |
| 15 · 2 | Alt bayraklar (K2) | `facilityKatar`, `facilityYalpa` (C# `true`, asset'e yazılmaz); kapalıyken na/oc tesiste asansör ve yağ, oda üretimi dahil |
| 15 · 3 | Eşleme (K3) | `RegionDef.FacilityEvent` ve tek okuyucu `FacilityMaps.EventFor`; plan, emir ve sunum okur |
| 15 · 4 | Plan önbelleği (K4) | Anahtar (tohum, katar, yalpa); `For`'un varsayılanları `true` |
| 15 · 5 | Şart parçası (K5) | Katar ve Yalpa parça üretmez; kenar geometriden, yatış takvimden; pafta olaydan çizer |
| 15 · 6 | Katar'da ileri geçit (K6) | G ve A odalarında yalnız doğu sütunu; köşe çiftleri de dışarıda (49) |
| 15 · 7 | Adlar (K7) | Bina ve kanat adları statik değişir; statik not iki hâle uyar; şartın notu `FacilityNote` (`Simulation.RegionNote`). **Onaya açık (S1)** |
| 15 · 8 | Olay tablosu (K8) | `RegionEvents` 12 + 2; `RaidEventTests` kilidi güncellendi |
| 15 · 9 | Kenarın girişi (K9) | Yalnız `Push`; cıvata kenardan önce; çekme tahtadan çıkaramaz |
| 15 · 10 | Kenardan düşenin temsili (K10) | `Dead` + `Overboard`, `Out` değil; `IsBody` `!Overboard` |
| 15 · 11 | Düşen makine, köpek, tutsak, aygıt (K11) | Makine ve tutsak ortak ölüm yolundan, `fall` Fx'iyle, hurdasız; köpek düşer ve dinlenir; aygıt yok olur |
| 15 · 12 | Taşıyıcı (K12) | Yük son karede (`DropCargo` ortak) |
| 15 · 13 | Rapor ve sonuç (K13) | Kişi durumu `"k"`; sonraki odaya konmaz; `RaidResult.Overboard`; `IsExtracted` değişmez |
| 15 · 14 | Yatış (K14) | Düz `facilityFallInjuryTurns` (en az 1); beceri indirimi, Revir ve deneyim yok |
| 15 · 15 | Takibin ölçütü (K15) | Batı geçidine yürüme uzaklığı ≤ `facilityFollowRange` (2); yalnız sağ makineler |
| 15 · 16 | Takibin kapsamı (K16) | Yalnız dönüşte (kaçış dahil), terk edilen oda Katar ise; kendiliğinden geçişte de; savunmada yok |
| 15 · 17 | Takipçi yerleşimi (K17) | Hemen, kadrodan sonra; doğu geçidi, sonra ondan yürüme uzaklığı en küçük boş zemin; `MaxEnemies` dolarsa kalan gelmez; aşama 11 ile ortak `EntryTile` |
| 15 · 18 | Takibin taşınması (K18) | Raporun `Follow`'u; `Next` takipte odayı oynatır; `Setup.Followers` |
| 15 · 19 | AYIR koşulu (K19) | `CanPass && Escape && Katar` ve Teknisyen kapıda, `!Acted`; geri alınmaz; `DecoupleBlock` nedeni |
| 15 · 20 | AYIR'ın bedeli (K20) | Yok |
| 15 · 21 | Savunmada şartlar (K21) | Katar (kenar dahil) ve Yalpa savunma tahtasında kapalı. **Onaya açık (S2)** |
| 15 · 22 | Yalpa takvimi (K22) | Sabit 2/4/6, batı/doğu/batı, `Turn < MaxTurns`; dönüş odası 1'den |
| 15 · 23 | Kayma (K23) | Birimler `Push`'un kayma kipiyle, yokuş aşağıdan; cıvatalı sessiz tutar; hurda boş zemin ve bağ kuralıyla; gövde ve yer yükü hasarsız, kapı ve menfeze girmez |
| 15 · 24 | Yalpa Fx'i (K24) | Tur başına `ev`/`yalpa`; kayan her şey için `slide`; `push` yazılmaz |
| 15 · 25 | Pusu ve tutuş (K25) | Bugünkü itilen birim davranışı |
| 15 · 26 | Makine aklı (K26) | Yalpa'da yatış turunda yokuş aşağısı kapalı adaya −0,5; Katar terimi yok |
| 15 · 27 | Hazards (K27) | Katar `brink`; Yalpa `Turns` okları, `slide` hatları, `tilt`/`tilt_next`, "YALPA {n}" |
| 15 · 28 | Okuyucular (K28) | Tasarımın §5 tablosu ve §6.1; kapanışta `FacilityRoomTurns`, `FollowedInIds` (113) |
| 15 · 29 | Balance (K29) | Dört alan; asset'e yazılmaz |
| 15 · 30 | İmza (K30) | Birimde `K`, tahtada `\|A`, yalnız varsa |
| 15 · 31 | Dizgiler (K31) | Oda raporu `…\|Tally\|Follow\|Decouple`; `RaidResult` `…\|Extracted\|Overboard` |
| 15 · 32 | Rapor denetimi (K32) | `KatarReportCheck` (§10 "Uygulanan hâl (aşama 12)") |
| 15 · 33 | `AYIR` politikası (K33) | `hep` (varsayılan), `hic`, `takip` |
| 15 · 34 | `KENAR` politikası (K34) | `hic` (345/600), `kacis` (0,3 × düşme) |
| 15 · 35 | Ölçüm planı (K35) | Beş etiket, eşli ilk baskın (`--ilk`), önek kilidi; `PROBE` yok |
| 15 · 36 | Hedef (K36) | Hedef satırı yok; okuma ölçütleri önceden; aşama 12 dengeyi değiştirmez |
| 15 · 37 | AYIR girişi (K37) | Q ve Teknisyen kartı; EMRİ İLET "AYIR"; sahne ve prefab değişmez |
| 15 · 38 | AYIR onayı (K38) | Yok |
| 15 · 39 | Takipçi gösterimi (K39) | Etiket yalnız `CanPass` iken; hover her zaman |
| 15 · 40 | Yatış canlandırması (K40) | Tahta kökü ~2,5° eğilme ve toplu kayma, ≤ 0,6 s |
| 15 · 41 | Teleks (K41) | Kenar düşüşü "· {n} düştü" içinde; ayrıntı raporda |
| 15 · 42 | Antet (K42) | "PAFTA n/3" kalır; vagon sözü oda ve kanat adında |
| 15 · 43 | Kit ve model (K43) | `Kit_na`, `Kit_oc` kalır; yeni model ve kuplör yok. **Onaya açık (S4)** |
| 15 · 44 | Renkler (K44) | `RaidPieceLibrary.hazardBrink` #A9520A, `hazardTilt` #2C6E73 (asset + C# yedeği) |
| 15 · 45 | Ses (K45) | Yeni ses yok; var olan `Cue`'lar (`EvShelfRoll`, `EvArmClank`, `RadioReport`) |
| 15 · 46 | Lokalizasyon (K46) | `strings.txt` elle; `UpdateTable` çalıştırılmaz; eski adlar kullanılmayanlar bloğuna |
| 15 · 47 | Şerit glifi (K47) | `←`/`→`; CP-Bold SDF'de var, Core değişikliği gerekmedi |
| 15 · 48 | Fiş taşması (K48) | Ölçüldü: EN beş satırlı HALCYON fişi −18,8 px (aşama 10 tabanı ~−19); EN bina adı kısaltılmadı |
| 15 · 49 | P1 (D1): köşe çiftleri | Doğu sütununun (7,0)–(7,1) ve (7,6)–(7,7) çiftleri de dışarıda; ortalama deneme 11,93 → 12,82, yedek tür 4 → 5; tükenme yok |
| 15 · 50 | P1 (D2): `Upcoming` | Yalpa için ok (`"←"`/`"→"`), "YALPA ←" değil |
| 15 · 51 | P1 (D3): "YALPA {n}" etiketi | Yokuş aşağı kenarın orta karesindeki `HazardMark{Kind = "tilt"/"tilt_next"}`'te |
| 15 · 52 | P1 (D4): kaymayan nesne | Hurda, gövde ve yük yalnız kaydıysa `slide` yazar; N 0 yalnız çarpan birimde |
| 15 · 53 | P1 (D5): test yardımcıları | `FacilityKit.Params`, `FacilityRoomTests.Params` şartı `EventFor(def, true, true)`'dan; `FacilityCountermeasureTests`'te moloz yönü |
| 15 · 54 | P1 (D6): `DecoupleBlock` sırası | `rule` → `tek` → `gate` → `acted`; `CanDecouple = CanPass && DecoupleBlock == null` |
| 15 · 55 | P1 (D7): takipçi yerleşimi `ReturnBoard`'un sonunda | Kadro, köpek/tutsak, taşınan ve yatan gövdelerden sonra |
| 15 · 56 | P1 (D8): `StepsFrom` | `Baskin.Events.cs`'te `ExitSteps`'in yanında; `EntryTile` `Baskin.Defense.cs`'te |
| 15 · 57 | P1 (D9): test kancaları | `Baskin.TestEventStep`, `TestEventPos` (internal) |
| 15 · 58 | P1 (D10): telsiz türleri | Kenardan düşen ekip üyesi "bad", makine "good", köpek "bad", tutsak/aygıt "sys"; yatış "sys"; AYIR "good" |
| 15 · 59 | P1 (D11): `FollowerIds` ve savunma | `B.Defense`'e de bakar |
| 15 · 60 | P2 (E1): savunmada Katar alanları | `KatarReportCheck` savunmada `Follow`, `Decouple`, `"k"`yi reddeder |
| 15 · 61 | P2 (E2): takip dizinleri | Çift ve negatif dizin reddedilir |
| 15 · 62 | P2 (E3): `"k"` denetimi | Kişinin düştüğü odaya (`Fell`) bakar, raporun odasına değil |
| 15 · 63 | P2 (E4): okuyucuların kenar durumları | Bilinmeyen bölgede null; `RegionNote` `FacilityNote`'u yalnız oynanan şart bugünkü olaydan farklıysa verir |
| 15 · 64 | P2 (E5): ret metinleri | Dört yeni metin TR + EN |
| 15 · 65 | P2 (E6): `SquadOutcome.Overboard` | Bool; `Down` da true; plaka, kelepçe, köpek kuralları ortak |
| 15 · 66 | P2 (E7): kaldırılan na notu | Güncel not ("Bölgenin en büyük dağıtım merkezlerinden biri…") kullanılmayanlar bloğunda |
| 15 · 67 | P2 (E8): test sürücüsü | `FacilityConditionCampaignTests.KatarRaid`/`Order` (internal); tahta kurulumu yardımcılarla, kayda yalnız komut |
| 15 · 68 | P2 (E9): `SimAt`'in yönü | Hedef sınırda değilse en az bağlantıyla yaklaşan sınır bölgesi (BFS) |
| 15 · 69 | P2 (E10): `strings.txt` yerleşimi | Yeni `# …/Core/Baskin.Conditions.cs` başlığı; öbürleri kullanıldığı dosyanın başlığında |
| 15 · 70 | P3 (F1): AYIR'ın kaçma nedenleri | `tek/gate/acted`'a ek `pol` ve `auto` |
| 15 · 71 | P3 (F2): kenar sayaçları | `EdgeBy[kaynak]` yalnız makine; uzman `EdgeTeam`, öbürleri `EdgeOther` |
| 15 · 72 | P3 (F3): ek anahtarlar | R `edgeX`, `eDied`, `eInj`; C `eDied`; S `edgeM edgeP fol tilt` |
| 15 · 73 | P3 (F4): `TiltHurtP/E` | Yatışta çarpan birim sayısı, hasar değil; hasar `evP/evE` |
| 15 · 74 | P3 (F5): `fol` | Raporlara yazılan takipçi; `MaxEnemies` dolarsa giren daha az olabilir |
| 15 · 75 | P3 (F6): `kacis` ağırlığı | `Down × 3 / 10` |
| 15 · 76 | P3 (F7): `Bot_Kenar_Hic` | Önce ödüllü kenarla (`EdgeFall` −2000) adayın varlığı |
| 15 · 77 | P3 (F8): `kacis`'in kapsamı | Sağlam ama uzak/geç kalan üyeyi de atar (gidişte); I5 "üst sınır" |
| 15 · 78 | P3 (F9): `Bot_Katar_PushesMachineOff` | Test yalnız adayın görüldüğünü sınar |
| 15 · 79 | P3 (F10): `Bot_FullCampaign` | Yeni Explicit test yok |
| 15 · 80 | P3 (F11): I7 kenar payı | Ölçülmedi |
| 15 · 81 | P3 (F12): `ozet.py --ilk` | Öneki aşama 12 anahtarlarını atarak karşılaştırır; varsayılan döküm basılmaz |
| 15 · 82 | P3 (F13): `hic` ağırlığı | Değişmedi (345 < 350 bulgusu yazıldı) |
| 15 · 83 | P3 (F14): `--tesis` dökümü | Değişmedi |
| 15 · 84 | P3 (F15): tarama | `t12sw01/03/06` `tesis12.txt`'e girmedi (ölçüm betiğinde, `BOTW`) |
| 15 · 85 | P4a (G1): tırnaklar | Çizim alanının içinde, kenara bakan ok başı |
| 15 · 86 | P4a (G2): kuşatma fişinin notu | `BuildingNote` |
| 15 · 87 | P4a (G3): duvar karesi | Açık kenar satırındaki duvarda kesik yok; çerçeve açık kenarın hiçbir karesinde çizilmez |
| 15 · 88 | P4a (G4): rapordaki yatış | Önce kayıttan ikinci yeniden oynatmayla okunuyordu; kapanışta `FallShifts(sim.B)` (114) |
| 15 · 89 | P4a (G5): `RoomTurns` | Kapanışta Core okuyucusuna taşındı (113) |
| 15 · 90 | P4a (G6): `Identity`'ye isteğe bağlı `sim` | Simülasyonsuz çağıran bugünkü olayı görür |
| 15 · 91 | P4a (G7): EN "laid up" | Tasarımın metni; var olan "kurtarıldı" satırı "bedridden" der; birleştirilmedi (119) |
| 15 · 92 | P4a (G8): takvim ayırıcısı | ", " |
| 15 · 93 | P4a (G9): paftanın hover'ı | "Olay: Katar" kalır, kural satırı arkasında |
| 15 · 94 | P4b (H1): EMRİ İLET notları | AYIR "takip yok", takip "{0} makine takip eder" / EN "{0} chasing" |
| 15 · 95 | P4b (H2): Teknisyen'in kartı | Tıklanabilir (`RaidTeamModel.Detach`); prefab değişmedi |
| 15 · 96 | P4b (H3): takipçi hover'ı | Menzil dengeden (`B.Moves.FollowRange`) |
| 15 · 97 | P4b (H4): gözcünün takip satırı | Önce "tahtaya en son eklenen n makine"; kapanışta `Baskin.FollowedInIds` (113) |
| 15 · 98 | P4b (H5): kapanış satırının paftası | "Takip: … PAFTA {1}" gidilen pafta; "Vagon ayrıldı: PAFTA {0}" ayrılan vagon |
| 15 · 99 | P4b (H6): Yalpa hover'ı | "Yatışta çarpar · −1"; `HazardRow` `tilt`/`tilt_next` |
| 15 · 100 | P4b (H7): koşu darbesinin kartı | Katar'da ikinci düğme "kenardan düşerse yok olur" |
| 15 · 101 | P4b (H8): hayalet ve kâğıt | Hayalet kenarın 0,8 dışında; "DÜŞER" var olan önizleme kâğıdıyla |
| 15 · 102 | P4b (H9): antetin notu | `RegionNote`; savunmada `BuildingNote` |
| 15 · 103 | P4b (H10): telsiz fişinde düşme önce | Hamle birim düşürürse fiş düşmeyi yazar |
| 15 · 104 | P4b (H11): `decouple` Fx dalı yok | Kol sesi `PassRoutine`'de |
| 15 · 105 | P4b (H12): eğilme | `PageRoots` dünya Z ekseni çevresinde; ≈ 0,57 sn |
| 15 · 106 | P4b (H13): PlayMode akışı | AYIR hedefin kaçış geçidinde, takip ara vagonun dönüşünde; gidişte A'da bir makine bırakılır |
| 15 · 107 | P4b (H14): tohum | 79 |
| 15 · 108 | P4b (H15): Yalpa testinin kurulumu | Batısı boş zemin olan açık makine; EMP uzmanı gövde için yatırılıp kaldırılır |
| 15 · 109 | P4b (H16): sonuç kartının yeri | "HALCYON · 01 Kumanya güvertesi" (bugünkü biçim) |
| 15 · 110 | P4b (H17): EN telsiz fişleri | Fiş yazıldığı dilde kalır (bütün fişler; değişmedi, 117) |
| 15 · 111 | P4b (H18): Unity | Tam EditMode ve PlayMode batchmode kopyada (editör yanıtsız kaldı) |
| 15 · 112 | Kapanış: EN terim (inceleme 1) | Üç yeni EN satırında "PASS" → "GO THROUGH", "gate" → "door" (durum satırı, takipçi hover'ı, kapı fişi); `FacilityConditionPresentationTests` üçünü sabit EN… |
| 15 · 113 | Kapanış: Core okuyucuları (inceleme 2, G5, H4) | `Simulation.FacilityRoomTurns(room)` (`RaidChain.Setup` + yeni `Baskin.FirstVisitTurns`; `RoomBoard` aynı kuralı okur) ve `Baskin.FollowedInIds(B)` (yeni… |
| 15 · 114 | Kapanış: ikinci yeniden oynatma (inceleme 3) | `RaidPaperFacts.AfterResult` silindi; rapor `FallShifts(sim.B)` okur |
| 15 · 115 | Kapanış: belge (inceleme 4) | §10.1'e iki lafız farkı (çekme düşürmez; AYIR için Teknisyen kapıda ve yeteneği harcanmamış), §10 "Uygulanan hâl (aşama 12)", §11.1–11.6, §12, bu tur, §15… |
| 15 · 116 | Kapanış: test gücü (inceleme 5) | `FacilityConditionTextTests` takvimi sabit metin ve sabit adımlarla bekler (aynı ifadeyle yeniden kurmaz). … |
| 15 · 117 | Kapanış: EN telsiz fişleri (inceleme 6, H17) | Değişmedi; açık kalanlara yazıldı |
| 15 · 118 | Kapanış: Yalpa'nın yönü (inceleme 8) | §11.6 okuma 5: bölge düzeyi dört etikette 6–9 puan düşük, eşli ölçüm ters işaretli ve geniş; tetik tutmadı, denge değişmedi |
| 15 · 119 | Kapanış: EN yatış terimi (G7) | "laid up" (aşama 12) ile var olan "bedridden" (kurtarılan) birleştirilmedi |
| 15 · 120 | Kapanış: Unity doğrulaması | Editör yanıt verdi; **editörde, boş sahnede** derleme temiz (`scriptCompilationFailed` false), tam EditMode **2213/2213** (271 sn; `FacilityConditionBotTests`… |

</details>

### 14.x Sürüm 10 kararları (2026-11-23)

| # | Karar | İçerik |
|---|---|---|
| 1 | Geçenlerle devam | Giriş ve ara odada süre dolarken kapıdakiler geçer, varamayan sağ geri çekilir ("w"). GEÇ'in koşulu (`CanPass`) değişmedi. |
| 2 | Kasayı her uzman açar | Bitişik ikmal kasası her uzmanın yetenek hedefidir (`Baskin.CabinetAt`, `OpenCabinet`); köpek, tutsak, aygıt ve sızmacı açamaz. Makinelerin Teknisyen önceliği değişmedi. |
| 3 | Simetrik geçit (döndürülmüş oda) | İleri geçit eskisi gibi kanonik doğu, kuzey ya da güney kenarında. Katar'da yalnız doğu ve köşesiz (açık kenarlar kuzey ve güneyde); Yalpa'da da yalnız doğu (yatış batıya ve doğuya; odalar döndürülmez, testlerin el kurulumları korunur). A ve H'nin girişi önceki odanın ileri geçidine simetriktir: karşı kenarda, aynı iki karede. Oda kanonik düzende (giriş batıda) üretilir: ham haritanın girişi istenen satıra konur (`RaidMaps.Generate(..., entryTop)`). Sonra geçidin kenarına göre döndürülür (`FacilityRoom.Turn`, `FacilityMaps.EntryFor`): doğu geçidi → batı girişi (0), kuzey → güney (1), batı → doğu (2), güney → kuzey (3). Baskın kodunun girişe göre kuralları (makinelerin uzak yarısı, girişin ateş hattı, menfez uçları, olay yerleri, fabrika dolabı, kademe makineleri) kanonik x'le hesaplanır (`Baskin.CanonX`). Oda geçide uyamazsa önceki oda sonraki denemesinden yeniden üretilir (kanat içi geri izleme, en çok 64). Geçide ortalama yol 7,65 adım (sürüm 9: 7,60); geçitlerin %42'si doğuda (sürüm 9: %46). İlk deneme (yalnız doğu sütunu) zaferi %43'ten %30'a düşürdüğü için bırakıldı (aşağıdaki ölçüm). |
| 4 | Pafta kayması | Oda geçişinde biten pafta geçidin tersine kayıp çıkar, yeni pafta geçidin yönünden gelir (geçit hangi kenardaysa). Tahtanın tabanı da üstündekilerle birlikte gider ve gelir, gölgesi onu izler (geliştirici, 2026-11-24; `RaidBoard3D.slideWith`, `BoardBackgroundView.boardBase`). Kayarken makinelerin emir iğnesi gizlenir. |
| 5 | Sürüm | `FacilityMaps.GeneratorVersion` 10; iz f10 (saha kapalıyken f8s10). Eski kayıtlar yüklenmez (iz tutmaz). `GameRecord.CurrentVersion` 21 kaldı: komutların biçimi ve S.Rng sırası değişmedi. |

**Bot ölçümü (2026-11-23; `denge22.txt`'in `t22` satırı: iyi3, 80 tohum × 3 = 240 harekât; `Tools/DengeBotu`).** Taban yeniden
üretildi (%43, kayıtlı %43,3). Değişiklikler tek tek ölçüldü:

| Koşu | Zafer | Not |
|---|---:|---|
| Taban (sürüm 9 kuralları) | %43 | karışık/tecrit 61, karışık/ucuz 49, kefilli 20 |
| Yalnız geçenlerle devam | %45 | |
| Yalnız kasayı her uzman açar | %48 | |
| Yalnız simetrik geçit (doğu sütunu, ayna) | %31 | |
| Doğu sütunu, aynasız | %28 | düşüşü ayna değil, doğu sütunu yapıyor |
| Hepsi (sürüm 10) | %27–30 | baskın başına başarı aynı (%74,5); ekibin tamamı düşen baskın %1,3 → %3,8 |
| Hepsi + en yakın ileri geçit | %35 | |
| Hepsi + giriş/ara/dönüş 6 tur | %35 | |
| Hepsi + en yakın geçit + 6 tur (+ hedef 10) | %36 (%38) | |
| Hepsi + geçide en çok 10 adım | %10 | uzun yol çok daha kötü |

Neden: eski üreticide ileri geçitlerin yarısı kuzey ya da güney kenarındaydı ve odaların %18'inde geçide 4–6 adımda varılıyordu;
doğu sütununda en kısa yol 7 adım (ortalama 7,6 → 8,1). **Karar (geliştirici, 2026-11-23): odaları döndür.** Kuzey ve güney geçidi
kaldı, sonraki oda döndürülür (karar 3).

**İkinci inceleme (2026-11-24, döndürmeden sonra).** Bağımsız inceleme iş akışı dokuz gerçek bulgu doğruladı; hepsi düzeltildi:

| # | Bulgu | Düzeltme |
|---|---|---|
| 1 | Plan tahtası (`BuildRoom`'un `P`'si ve şart kurulumunun kopyası) odasını taşımıyordu: `CanonX` plan sırasında gerçek x döndürüyor, döndürülmüş odada bölge özelliği (fabrika üreticisi uzak yarıda) ve beş şartın bantları (çatlak, asansör, kriyo, tüp, yağ) girişin yanına düşebiliyordu | `BState.PlanRoom` (yalnız kanonik x okur; çalışma zamanı kuralları `Room`'a bakar, plan tahtasında açılmaz) |
| 2 | Bulut, raf ve süzgeç bantları mutlak x'teydi | `CanonX` |
| 3–4 | Dönüş satırı "doğu geçidinden", savunma dalgası "batı girişinde" diyordu (oda döndürülebilir) | Yönsüz metin: "ileri geçitten", "giriş kapısında"; kaçışta "Giriş geçidi" (TR ve EN) |
| 5 | Geçiş kayarken geri çekilen uzmanın kartı ayakta görünüyor, KAPIDA paydası onu sayıyordu | Kart GERİ ÇEKİLDİ, sayım dışı |
| 6 | Köşedeki geçit karesinden çıkan uzman komşu kenara kayıyordu | Kayma yönü karenin geçidinden (`RaidBoard3D.LeaveDirection`) |
| 7 | Geri çekilen uzmanın üstünde yeşil TAHLİYE yazıyor, kenara kayıyordu | Tehdit renginde GERİ ÇEKİLDİ, yerinde küçülür |

Düzeltme 1–2 döndürülmüş odaların yerleşimini değiştirdi: sürüm 10 henüz yayımlanmadığı için iz f10 kaldı, tohum 79 altın özetleri
yenilendi (`0x8358DEEF` / `0x15634C40`). Döndürülmemiş odanın kuralı aynı (kanonik x = gerçek x).

**Döndürmenin ölçümü (2026-11-24; 160 yeni tohum, 81–240 × 3 yapılandırma = 480 harekât, eşli).** Taban (sürüm 9 kuralları) bu
tohumlarda %45 (ilk 80 tohumda %43). Sürüm 10'un son hâli (döndürülmüş oda, Katar ve Yalpa yalnız doğu, ikinci inceleme düzeltmeleri):

| Ölçü (son − taban) | Fark |
|---|---:|
| Zafer | **%31** (−13,3 ±3,3 puan) |
| Dağılma | +8,5 ±3,1 puan |
| Baskın / harekât | −2,9 ±1,1 |
| Başarı / baskın | −1,7 ±0,9 puan |
| Ölü / baskın | +0,09 ±0,02 |
| Son güven | −2,0 ±0,6 |

Okuma: baskın başına başarı neredeyse aynı; kayıp harekâtın temposunda (daha az baskın, daha çok dağılma). İlk 80 tohumdaki %36
`PlanRoom` düzeltmesinden önceydi (o koşuda döndürülmüş odada şartlar ve fabrika üreticisi kanonik bantlara uymuyordu); fark
tohumdan mı düzeltmeden mi, ayrıca ölçülmedi. **Denge kararı geliştiricide** (açık).

**Telafi varyantları (2026-11-24; aynı 160 tohum, eşli; geliştirici: "telafi varyantlarını ölç").** Sürüm 10'un son hâli üstüne yalnız
tur sayıları (denge değeri, `BAL`):

| Varyant | Zafer | − taban | − sürüm 10 | Başarı / baskın − taban | Dağılma − taban |
|---|---:|---:|---:|---:|---:|
| Taban (sürüm 9) | %45 | | | | |
| Sürüm 10 (G/A/dönüş 5, H 9) | %31 | −13,3 ±3,3 | | −1,7 ±0,9 | +8,5 ±3,1 |
| G/A 6 (`g_gm6`) | %38 | −6,7 ±3,1 | +6,7 ±3,2 | +0,9 ±1,0 | +3,8 ±3,0 |
| G/A/dönüş 6 (`g_tur6`) | %39 | −5,2 ±3,2 | +8,1 ±3,0 | −0,4 ±0,9 | +5,0 ±3,0 |
| **G/A/dönüş 6, H 10** (`g_tur6h10`) | **%43** | −1,9 ±3,2 | +11,5 ±2,9 | +2,3 ±0,9 | +4,0 ±3,0 |

`g_tur6h10` tabanı gürültü içinde geri getirir; baskın başına başarı tabanın üstünde (+2,3), harekât daha kısa (uz 71 / taban 72 /
sürüm 10 77 gün) ama dağılma hâlâ biraz yüksek (+4,0, sınırda). **Karar (geliştirici, 2026-11-24): `g_tur6h10` uygulandı** (`Balance.cs` ve
`BalanceConfig.asset`: giriş, ara ve dönüş 6, hedef 10 tur; kısa nöbette ilk ziyaret 9).

## 15. Doğrulanmayanlar

- Aşama 17’de uzman hamlesi ve tur sayısı bot kararlarıyla ölçüldü. Gerçek oyuncunun süre ve zorluk deneyimi, köpeğin havlamasının taktik değeri ve dört zaferden hesaplanan medyanın daha geniş örneklemdeki kararlılığı açık.


- Aşama 16 eski hurda uyumunu önceki Core ile üretilen kayıt üzerinden doğruladı. Oyuncuların kişisel kayıtlarında böyle bir işlem olup olmadığı bilinmiyor.

- **Sahadaki sızmacı (aşama 15).** Aynı kimlikten birden çok baskın fırsatı doğabilir. Kabul edilen sızmacılar içindeki sessiz payı ile baskında görülen dönüş sayısı ayrı ölçülerdir. İnsan oyuncuda şüpheye bağlı kadro seçiminin etkisi bilinmiyor.

- **Kişisel unvanlar (aşama 14).** Bot unvanların insan oyuncudaki dağılımını temsil etmez. Nişancı canlı PlayMode akışında
  kazanılır ve bir sonraki tahtada altıncı karede bir makine vurulur. Diğer beş etki ayrı Core testlerinde sınanır.
  Oyuncuyla iki unvanlı dosya görünümü ve uzun harekâtlardaki oranlar ayrıca ölçülebilir.

- **İmza makineleri (aşama 13).** Kurallar Core testlerinde, baskın açılışı canlı Unity PlayMode'da ve başarı botla sınandı.
  İnsan oyuncuyla model ayrımı, kırağı işaretinin okunurluğu ve niyet animasyonlarının temposu ölçüm dışında kalır.
  Botun genel hurda sayacı makine ve aygıt kaynaklarını birlikte sayar. Baskın süresi, can kaybı ve geç bölgelerdeki
  örneklem §11.6'da raporlanır.

- **İnsan oyuncuyla süre ve başarı.** Aşama 4 botla ölçtü (§11.6); insan oyuncunun geri çekilmesi, tur içi geri alması
  ve sivili gözetmesi ölçülmedi. Elle oynama geliştiricidedir.
- **Bot.** Çoklu eylemde dallanmanın büyüklüğü ve oda sınırında puanın sıçraması (bilinen tuzak) ölçülmedi.
- **Sunum.**
  - Dördüncü ekip kartı 1920×1080'de (TR ve EN) ve bir kez 16:10'da (1920×1200) ekran görüntüsüyle bakıldı ve
    `TeamCards_Four_Fit1080_BothLanguages` ile ölçüldü (aşama 9); 21:9'da bakılmadı.
  - Pafta çevirmenin görünümü yalnız ekran görüntüsüyle bakıldı; oyuncuyla denenmedi.
  - Paftaların ve Gözetim cebinin üzerine gelme etiketleri gerçek fareyle denenmedi; bağları sahne testiyle sınandı.
  - Telsiz masasına 16:10 ve 21:9'da bakılmadı.
- **Ekipman dengesi.** Tarif girdileri, haklar ve süreler ölçümsüz başlangıç değerleridir (aşama 8'de yalnız botla ölçüldü,
  değiştirilmedi; aşağıdaki "Ekipman ve ödül" maddesi).
- **Yeni hamlelerin insan oyuncuyla kullanımı.** Aşama 6 hamleleri yalnız Core ve botla sınandı. Aşama 7'nin sunumu
  (19 Kasım 2026) yalnız testlerle doğrulandı: EditMode ve PlayMode testleri gerçek E tuşu ve gerçek tahta tıkıyla altı
  hamleyi verir, ama oyun elle oynanmadı ve hiç ekran görüntüsü alınmadı. Kullanıcının editörü ekran kilidi yüzünden
  yanıtsızdı; bütün Unity doğrulaması batchmode kopyada yapıldı. Görülmeyenler: önizleme çizgilerinin, pusu hattının, yerdeki
  yük parçasının ve odak çerçevesinin tahtadaki görünümü; canlandırmaların temposu (sürükleme, menfez, fırlatma kavisi,
  makine evresinin bölünmesi); kartın kopya satırlarının gerçek ekranda okunurluğu (sığma yalnız prefab içeriğinde ölçüldü);
  E döngüsünün ve tık sırasının oyuncuya doğal gelip gelmediği; 16:10 ve 21:9. Botun pusuyu dolgu hamlesi yapması, sürüklemeyi
  az kullanması (baskın başına 0,4) ve gidişte gövdeye dokunmaması insan oyuncunun oyununu temsil etmeyebilir.
- **Ekipman ve ödül (aşama 8, 19 Kasım 2026):** yalnız Core testleri ve botla doğrulandı; sunum yok (aşama 9), elle oynanmadı, bayrak
  asset'te kapalı. Vinç ve kelepçenin kullanım hedefi tutmadı (bot ölçümü); köpeğin %48'i havla yüzünden bilgi vermez. Üretim
  yolunun baskın başarısını ~2 puan düşürmesinin kaynağı bulunmadı (§11.6 "Aşama 8 ölçümü", okuma 4; bağımsız incelemenin zırh
  üretimi adayı sınanmadı). Bot ekipmanı insanın kullanacağı gibi kullanmıyor: geri alma ve geri çekilme yok, havla kaba terimle
  seçiliyor (2,5 / baskın), getir hiç seçilmedi; ekipmanın insan elindeki değeri ölçülmedi. Karesi kalmayan köpek ya da tutsağın
  konmaması (§14 on birinci tur 60) ölçülmedi. `FacilityJsonTests` yalnız Unity'de (geçti); PlayMode parça D'de yeniden koşulmadı.
- **Ekipmanın ve ödülün sunumu (aşama 9, 19 Kasım 2026):** EditMode ve PlayMode testleriyle (bitti ölçütü
  `FacilityGearCampaignPlayTests.EveryGear_ProducedEquippedUsed`: tek harekâtta yedi ekipman sunumun yolundan üretilir, takılır ve
  baskında R ve tahta tıkıyla kullanılır) ve 1920×1080 TR/EN ekran görüntüleriyle doğrulandı; oyun elle oynanmadı. Görülmeyenler:
  canlandırmaların temposu (aygıtlar, kelepçe, köpek), R döngüsünün ve köpeğin 4 tuşunun oyuncuya doğal gelip gelmediği, kurgu
  parçalarının (yol E) uzaktan okunurluğu, ödül kâğıdının akışı (rapor → kâğıt → tepsi) gerçek oyuncu temposunda, 21:9. Beş
  satırlı hover kartı ekran görüntüsünde telsiz masasının altında kaldı (§14 on ikinci turun açık bıraktıkları). Bitti ölçütü
  testinde hammadde ve erzak kurulumla doldurulur; ekipmanın gerçek harekât ekonomisindeki üretilebilirliği ölçülmedi.
- **Önlem defteri (aşama 10, 19 Kasım 2026):** Core testleri, botun 80 tohum ölçümü, EditMode ve PlayMode testleri
  (`FacilityCounterPlayTests.Countermeasure_ShownEverywhere`: gerçek koşu darbesiyle öğrenme, altı yüzey, kayıttan devam) ve 1920×1080
  TR/EN ekran görüntüleriyle (batchmode) doğrulandı; oyun elle oynanmadı, bayrak asset'te kapalı. Parça A–D'nin Unity doğrulaması
  batchmode kopyada yapıldı (editör yanıtsızdı); kapanışta editör yanıt verdi ve tam EditMode (2026/2026) ile tam PlayMode (123/123)
  editörde, boş sahnede geçti. Batchmode PlayMode ekranı 640×480'dir; defterin ışınına bağlı 16 test orada düşer (GELISTIRME).
  Bot insan değildir: pusuyu dolgu hamlesi yapar, havlamayı kaba terimle seçer, yayıcıyı ancak puan terimiyle kırar (`W.Emitter`),
  makine aklının hattan kaçınmasını yalnız kaba payla görür (`W.AmbushSeen`); önlemlerin insan oyuncudaki cezası farklı olabilir. Bot
  önleme göre araç değiştirmez (`ARAC=tek` ve `donus` önlemi okumaz; `uyum` ölçülmedi). Makine aklının kaçındığı plan sayılmadı (Core'da
  Fx yok). Çift cidarın kısayol puanının anlamsız duvar seçtiği odalar plan üstünde ayrıca aranmadı (ekran görüntüsünde üç pafta
  bakıldı). Tutsakken taşınan sökük makinenin yeniden cıvatalanması (A, nadir) ve basılmış fişte iptal edilen taslak (B) ölçülmedi.
  Görülmeyenler: CIVATALI, `cut` ve `jam` canlandırmalarının temposu; ÖNLEM teleksinin oyuncu temposunda okunması; 16:10 ve 21:9.
- **Altı yeni makine.** Kuralları, canları ve makine aklı puanları ölçümsüzdür; tampon ve çapanın kabloyla ve
  kendi tesis şartlarıyla (Katar'ın açık kenarı, Yalpa'nın yatışı) etkileşimi sınanmadı.
- **Üç odalı savunma (aşama 11, 20 Kasım 2026):** Core testleri, botun 80 tohum ölçümü, EditMode ve PlayMode testleri (bitti ölçütü
  `FacilityDefensePlayTests.Defense_PlayedFromSlipToReport`) ve 1920×1080 TR/EN ekran görüntüleriyle (batchmode) doğrulandı; oyun elle
  oynanmadı, bayrak asset'te kapalı. Bot savunmayı 10 turda oynar (oyuncu turu 10, uzman hamlesi 36) ve hep kazanır (tutma %100, hep
  çekil %98, tehlikede çekil %100); hedef %40–60 tutmadı ve tasarımın ayar alanlarıyla tutturulamadı (§11.6 "Aşama 11 ölçümü" okuma 1).
  İçeri çekilme bot için hiç gerekmez (`baski` 185 savunmanın 8'inde çekildi, sonuç aynı). İnsan oyuncunun savunmayı nasıl oynadığı,
  barikat turunda ne kurduğu (bot 0,1–0,24 engel / savunma) ve "bırak"ı ne zaman seçtiği ölçülmedi. Savunmaya kural düzeyinde amaç
  gerekir (§14 on dördüncü tur "Açık sorular" 5). Görülmeyenler: dalga gelişinin ve İÇERİ ÇEKİL onayının oyuncu temposunda okunması,
  daralan 10 hücreli şeridin 16:10 ve 21:9'da görünümü, saha rölesi parçasının uzaktan okunurluğu.
- **İzlerin etkisi (aşama 11):** botla ölçüldü (I9): izler ve onarım ölü / baskını +0,034 ±0,015 artırır; kaynağı aranmadı (adaylar:
  tekrar baskınında yerinde kalan engel ekibin yolunu uzatır, açık kepenk makinelere de yol verir, kırık donanım hazırlığı botun donanım
  hedefini değiştirir). İzlerin paftada görünümü yalnız ekran görüntüsüyle ve `FacilityPlanSheetTests` ile bakıldı; onarımla geri gelen
  duvar ile yerinde kalan telin başka kareleri kapatması (bağ kuralı yalnız batı girişi ↔ ileri geçit ↔ odak bağını korur) ölçülmedi.
- **Katar ve Yalpa (aşama 12, 20 Kasım 2026):** Core testleri, botun 80 tohum ölçümü, EditMode ve PlayMode testleri (bitti ölçütü
  `FacilityConditionPlayTests`: iki kanat fişten rapora, gerçek Q, Z, Enter ve tahta tıkıyla) ve 1920×1080 TR/EN ekran görüntüleriyle
  doğrulandı; oyun elle oynanmadı, ana bayrak asset'te kapalı. Bot Katar'da kenardan baskın başına 0,8 makine düşürür (çoğu EMP), Yalpa'da
  baskın başına 4,7 yatış yaşar; iki şartın ilk baskın başarısına etkisi 50–94 eşli çiftte gürültü içinde (Katar +3 ±7p, Yalpa +6 ±11p);
  Yalpa'lı etiketlerde oc'nin bölge düzeyi başarısı 6–9 puan düşük (eşli değil). Görülmeyenler: düşme ve yatış canlandırmalarının oyuncu
  temposunda okunması, kayma hatlarının kalabalık tahtada okunurluğu, AYIR'ın Q ile verilişinin oyuncuya doğal gelip gelmediği, 16:10 ve
  21:9.
- **Kaçış kapısı (§13 "Katar"):** bot için gerçekleşmedi (okuma ölçütü: ölü farkı −0,06 ±0,05). Botun kendi üyesini attığı her durum ileri
  geçide yetişmeme ya da yol kısaltmadır (A gidişi); kaçışta geride kalacak üyeyi EMP ile atma neredeyse hiç olmadı. İnsan oyuncu "yavaş
  üyeyi kenardan at, odayı kaybetme" yolunu bilinçli kullanabilir; bedeli 5 vardiya yatıştır. Bedel seçenekleri (S3) açık.
- **AYIR (§13 "Katar"):** bot için bedelsiz ama belirleyici değil; dönüş geçişi başına 0,12 takipçi gelir. İnsan oyuncunun Teknisyen'i kapıda
  tutup tutmayacağı, takibi bilinçli göze alıp almayacağı ölçülmedi.
- **Yalpa'nın şiddeti:** olay hasarı yağın ~4 katı; oc zaten zor bölge (aşama 12'de %51–54). Eşli ölçümde fark gürültüde; daha çok çift
  için oc'yi erkene alan bir ölçüm kipi ya da `PROBE` gerekir (açık).

**Kapananlar**

- **Makine aklının savunmada çalışması** (aşama 11, 20 Kasım 2026): röle vuruşu (11), patlamada +6 ve `Smart`'tan bağımsız röleye çekim
  Core testleriyle sınandı (röle ve uzman eşit uzaklıkta niyet röle; ekip yokken bakım robotu röleye yürür). Bot ölçümünde röle yalnız hep
  çekilmede görülür (161 savunma) ve 0,12 isabet alır: makineler ekibi geçemez, çoğu girişte yığılıp düşer. Giriş ve ara odada makinelerin
  bir hedefi yoktur (tasarım: "makineler ekibi geçip röleye ulaşamaz"); savunmanın zorluğu bu yüzden yalnız ekibin hatasından doğar (§11.6
  "Aşama 11 ölçümü" okuma 1).

- **Önlem eşiği** (aşama 10, 19 Kasım 2026): 6 kullanım ve baskın başına 3 tavanı bot harekâtında pusu sayılmazken tek araçta 0,71,
  dönüşümlüde 1,19, ekipmansızda 0,05 önlem / harekât üretir (harekâtların %59–61'i önlemsiz kalır); lafızda (pusu sayılır) 2,4–2,5
  ve ilk önlem hep `aygit` (§11.6 "Aşama 10 ölçümü").

- **Ölüm kuralının etkisi** (aşama 6, 19 Kasım 2026): botla ölçüldü (§11.6 "Aşama 6 ölçümü"). Taşınmayan düşenin ölmesi
  harekât başına ölüyü 7,0'dan 12,7'ye çıkarır (+5,75 ±0,79; baskın başına 0,39 → 0,71), zaferi %6,7'den %2,1'e indirir;
  dağılma oranı %86'dan %80'e iner ama geç gelir (medyan 14 → 23,5. gün): ölümler sığınağı eritir. Bot, ölen ya da çıkarılan kişilerin (düşen ve geride kalan birlikte)
  %11'ini sürükleyerek, %1'ini temiz odadan toplayarak, %1'ini son nefesle çıkarır; tabanın kendiliğinden kurtardığının %20'si.
  Yeni ölümlerin kaynağı bedel değil kural yapısıdır: kaybedilen baskında yatan (3,7 / harekât, tabanda da ölüyordu), hedef
  ve dönüş odasında ayaktakiler varken bırakılan (3,5), dönüş odasında ayakta kimse kalmayan (1,3) ve süre sonunda geride
  kalan (4,2; tabanda 2,9). Sürükleme bedelini kaldırmak ve son nefes kuralı bunu gürültü içinde değiştirir. Ölü hedefi
  (≤ 7,5) tutmadı; ne yapılacağı §14 dokuzuncu turun açık bıraktıklarında. Kartlı kadroyla tesiste baskın başına düşen (son raporda)
  0,58 (`t6sn`; tek tahtalı taban 0,42).

- **Bina planının 1920×1080'e sığması** (aşama 5, 18 Kasım 2026): üç pafta TR/EN, fiş solda ve sağda, kadro raporu açıkken
  ekran görüntüsüyle ve `FacilityPlanPlayTests` ile doğrulandı. Fişi ve kadro raporunu örtmezler; üst sayaçları örtmeleri
  §14 sekizinci turda açık.

- **Süre** (aşama 4, 18 Kasım 2026): üç odalı baskın botla toplam 18–19 tur (p90 21–23), devam ve makine kısmada tabanın
  2,3 katı hamle; gidiş 13–14, dönüş 5–6 tur. Ayrıntı §11.6 "Aşama 4 ölçümü".

- **Bölge adının değişmesi** (aşama 3, 18 Kasım 2026): YZ adını okuyan bütün sunum kurucuları ve testleri güncellendi. Üretilmiş
  haritada tek sözcüklü YZ adı iki satıra bölünemez: tohum 72'de (küçük ve komşu HEADWATER ile EDDY) yerleşim temiz
  bitmiyordu. `GeneratedMapLayout`'a üçüncü tur eklendi: ilk iki tur bitmezse adın hücre merkezinden daha uzağa
  (±80 px, dikeyde +22/−60) kayabildiği seçeneklerle ikinci turun kuralı yeniden denenir. İlk iki turda biten yerleşim
  değişmez; Mono'da 1–332 tohumun hepsi temiz, bugünle tek fark tohum 72 (eski yerleşim 101–400'de de temizdi).

- **Kartlı kadroyla baskın başarısı** (aşama 0, 17 Kasım 2026): kartlı yolun kişi değerleriyle ikmal %82, sunucu %91;
  ayrıntı §11.6 "Taban ölçümü".
- **108 odanın üretilebilirliği** (aşama 1, 18 Kasım 2026): 200 tohumda tükenen tohum yok, Mono'da tohum başına
  ~230 ms; oda başına ortalama 5 deneme (ara oda 13, en çok 263); yedek tür 14/21600.
- **Bölge olaylarının yeni odalarda yer bulması** (aşama 1): 12 şartın hepsi ara ve hedef odada 8 tohumun 8'inde kurulur
  (`EveryEvent_FindsAPlace_InMiddleAndTargetRooms`).
