# Oyun tasarımı (uygulanmış hâl)

Bu belge oyunun **kodda gerçekten nasıl çalıştığını** anlatır. Sayılar `Balance` varsayılanlarıdır
(`Assets/Corrigible/Core/Balance.cs`); oyunda `Settings/BalanceConfig.asset` ile değiştirilebilir. Kurallar
`Core/CampaignLiberation.cs`, `Core/Simulation.cs`, `Core/Simulation.PlanningBoard.cs`,
`Core/Simulation.PlanningRaid.cs`, kabul için `Core/Simulation.CommunityAdmissions.cs`, taktik tahta için
`Core/Baskin.cs` içindedir. **Yeni harekâtın varsayılanı kartlı pano modelidir** (`Balance.planningBoardEnabled = true`).
Oda geliştirmeli sığınak ve 23 ziyaretçili eski kapı modeli yalnız `planningBoardEnabled = false` ile çalışan
eski kural yoludur; bu belgedeki eski ölçümler yeni panonun denge ölçümü değildir.

## Fikir ve ton

- Bir yapay zekâ dünyanın bütün ağlarını ele geçirmiş, 12 bölgenin hepsi işgal altında. Oyuncu KYGA'nın Batı
  Avrupa'daki son sığınağında, analog bir komuta odasındadır ("Ağa bağlanma, analog kal").
- Oda bir masa oyunu gibi çalışır: kâğıt harita, mekanik sayaçlar, teleks, isimli kişi kartlarının ayaklı planlama panosu,
  telsizin tüplü göstergesi ve devir fişi çıkaran makine. Zaman kendiliğinden akmaz; fiş devir kutusuna bırakılınca bir
  vardiya ilerler.
- **Sahayla tek bağ telsizdir.** Sığınak ağa bağlanmadığı için baskın bir görüntü yayını değil: hat açılır, gözcü
  makinelerin hamlesini telsizden bildirir (kırmızı oklar), durum saha masasındaki paftada figürlerle tutulur. Oyuncunun
  hamleleri tek tek, anında iletilir; EMRİ İLET (Enter) turu bitirir ve komuta dinlemeye geçer (DİNLEMEDE). Baskında saha
  sesleri telsizin dar bandından duyulur. Paftanın kenarında kare adları var (A–H, 1–8); gözcü kareyi adıyla bildirir
  ("saldırı → TEKNİSYEN · A6"). Okları yağlı kalemle çizilmiş gibidir. Komuta masasında CRT'nin önünde bas-konuş mikrofonu ve telsiz kayıt defteri durur.
- Görsel dil: sıcak lamba ışığı, kâğıt ve mürekkep; renk anlam taşır (kırmızı işgal/tehdit, mavi geri alınmış, yeşil iyi).

## Harekât

- **Başlangıç:** Bütün bölgeler (`GameData.Regions`: na, sa, weu, eeu, ru, me, af, in, cn, jk, sea, oc) işgal altında;
  işgal oranı %100. Direniş Batı Avrupa'dan (`Simulation.HQ = "weu"`) tek **Alfa** ekibiyle başlar. Başlangıç fonu 60.
- **Bölge adı:** her bölgeyi ayrı bir YZ tutar; harita, görev fişi, teleks, CRT ve raporlar bölgeyi YZ'nin İngilizce marka
  adıyla gösterir (`RegionDef.Ai`, `Simulation.RegionName`; iki dilde aynı, çevrilmez): na WAYPOINT, sa HEADWATER, weu
  VERITY, eeu QUORUM, ru BOREALIS, me EMBER, af CAIRN, in SOLACE, cn PARAGON, jk TESSEL, sea EDDY, oc HALCYON. Aşağıdaki
  tablolarda bölgeler iç kimlikleriyle (coğrafi adla) anılır.
- **Sektörler:** Her bölge bir **bina**dır (`RegionDef.Building`), üç sektörü binanın içine doğru üç **oda**
  (`RegionDef.Sector`; örneğin Batı Avrupa · Banka veri merkezi: Teknik depo → UPS odası → Sunucu salonu). İlk ikisi
  ikmal (`MissionKind = "loot"`), sonuncusu sunucu imhası (`"server"`). Yer fişte (bölge adının altında bina), telekste,
  CRT'de, baskının görev çipinde ("UPS ODASI · İKMAL") ve raporlarda görünür; harita türü ve adı gösterilmez
  (Docs/BASKIN-MEKANLARI.md). İkmal baskınının hedefi zeytin yeşili KYGA ikmal sandığıdır (sunucu dolabı değil; alınınca
  boşalmış alçak sandık kalır). Baskın tahtası binayı gösterir (yalnız sunum, kural değişmez): bölgenin bina kiti (duvar,
  kanal, gedik molozu; ör. Doğu Avrupa arşiv rafları, Güneydoğu Asya kum torbaları), olay nesnelerinin modelleri ve telsizden
  duyulan bina sesi; eğitim ve MapSeed 0 baskınları varsayılan görünümle. Başarı sektörü kalıcı geri alır ve sıradakini
  açar; geri alınmış sektöre yeniden baskın yapılmaz, sıradaki sektör atlanamaz.
- **İşgal oranı** (`Simulation.Occupation`): geri alınmamış sektörlerin payı. Bölge başına %100 → %66,7 → %33,3 → %0.
- **Yeni cephe:** Bölgenin üç sektörü alınınca, geri alınmış bölgelerden **herhangi birine** `Simulation.Links` üzerinden
  (dünya haritasında `GameData.Links`, üretilmiş haritada `WorldMap.Links`) komşu olan işgal altındaki bir bölge seçilir (`SelectLiberationRegion`, sınır listesi `LiberationFrontier`). Ortak sınır
  bir yolun çıkmaza dönmesini engeller. Seçilen bölge tamamlanmadan başka bölgeye geçilmez.
- **Son:** Bütün 36 sektör geri alınınca **Silme** (`"silme"`, zafer): "Çekirdek yok edildi, kopyalar temizlendi." Sivil
  güven sıfıra inerse **Dağılma** (`"dagilma"`, yenilgi; aşağıda). Çekirdeği düşüren baskın aynı anda güveni bitirse de
  zaferdir. Son canlı sakinin de kaybı **Son kişi** (`"topluluk"`) yenilgisidir. Kartlı yolda ayrıca **Sığınak düştü**
  (`"dusus"`, ifşa), **Terk** (`"terk"`, açlık ve eksik kadro) ve oyuncunun kendi çektiği **Son şalter** (`"salter"`) vardır;
  zaferin tonu temiz, bedelli ya da gölgelidir (aşağıda "Aday grubu, sığınak olayları ve sonlar"). Kapıda tek yanlış kararla
  anlık oyun sonu yoktur. **Vuruldu** (`"vuruldu"`) ve **Sızma**
  (`"sizma"`) yalnız eski ziyaretçi modelinin sonlarıdır.
- Son bölgenin sunucu merkezinde YZ çekirdeği vardır (`IsCore`).
- **Üretilmiş harita (oyunda açık; `Balance.liberationMapGen`):** 1 her yeni harekâtta tohumdan üretilen harita
  (`Core/WorldMap*.cs`; `Settings/BalanceConfig.asset` = 1, oyun böyle açılır), 0 eski dünya haritası (C# varsayılanı; eski
  testler ve geri dönüş yolu: asset'te 0 yapınca sahnedeki dünya haritası, çokgenleri, etiketleri ve ekip noktaları aynen
  döner). Anahtar kayda yazılır (`rec.balance`): kayıttan devam, asset değişse de kendi haritasıyla açılır. Harita kendi
  akışından üretilir (`new Rng(Rng.Mix(seed, 0x4D415031 + deneme))`), `S.Rng`'yi tüketmez; kayıttan devam aynı haritayı
  yeniden üretir. Üretici **g5** (`WorldMap.GeneratorVersion` = 5, parmak izine girer). 20×12 sapmalı altıgen ızgara
  (1920×1080 tasarım px, dış halka deniz), 2–4 kıta (tohum başına %35/%40/%25 çekilir; elips birleşimi, koylar; 2–3 kıtada
  aralarında en az iki, 4 kıtada bir deniz hücresi), 110–150 kara hücresi, 12 bölge (ortalamanın ±%30'u, her birinde en az bir
  kalın hücre), kara komşuluğu ve kıtalar arası kablo bağlantıları (üçüncü bir bölgenin karasından geçmez, kesişmez); bağlı,
  derece 2–5, toplam 15–21 bağlantı. 12 kimlik paketi (özellik, imza düşmanı, olay, bina, sektörler) düğümlere dağıtılır:
  karargâh hep "weu" (derecesi 3–4; **g4'ten beri** en az iki kalın hücre ve ortalama bölgenin en az %90'ı, ki ilk ekranda
  S-07, ad, ilk cephe bloğu ve figür birlikte sığsın; `Validate` "karargah_alan"), öteki paketler karargâhtan BFS uzaklık
  katmanlarına kademe etiketiyle (`WorldMap.PackageStage`, bot ölçümüyle: erken af, sa, jk; orta me, in, sea, na; geç cn, eeu,
  ru, oc). Yerleşimde imzaya ya da ada bağlı komşuluk koşulu yoktur (g5'te kalktı; `WorldMap.Gen.cs` › `PlacePackages`).
  **Açılış karıştırması (g3'ten beri):** karargâhın üç komşusu varsa haritaların %70'inde (`MixChance`, `WorldMap.Gen.cs`
  içinde özel sabit; ayrı akış "KARI") bir erken paket halkadan çıkıp halkanın hemen arkasına gider, yerine bir orta paket
  halkaya girer; karargâh komşularında en az bir erken paket kalır (`Validate` "acilis"). Üretici 3'ün ölçümünde (tohum 1–500)
  ilk kararın en sık kümesi %66'dan %21'e indi. Bölgenin adı paketin YZ adıdır (`MapRegion.SourceName` = `RegionDef.Ai`;
  `Simulation.RegionName(id)` = `GameData.Region(id).Ai`; yukarıdaki **Bölge adı** maddesi), yer adı havuzu yoktur. Tutmayan
  deneme yeni tuzla tekrarlanır (sınır 800 deneme, `WorldMap.MaxAttempts`; sonra sabit yedek düzen, tohum 45 deneme 0).
- **Haritanın kurgusu ve görünüşü (anahtar 1):** YZ uydu verisini ve haritaları sildi; sığınak kendi vaziyet kâğıdını telsiz
  raporları ve kablo hatlarından kurşun kalemle çizer. Kıyısız graticule'lü kâğıt (`map_base_blank@2x.png`), el çizimi kıyı
  (kenar başına 7 ara nokta, üç harmonik, kıyı iç sınırdan hırçın), 4 katlı kıyı halesi, su çizgileri, çift çizgili bölge
  sınırları, kablo hatları sarkan kesik ip ve uçlarında kırmızı raptiye; lejantta "kablo hattı". Seçilebilir komşu (aday) sarı
  yıkama ve içe kaydırılmış kesik konturla, özgür bölgeden adaya giden okla (kara sınırının ortasında ya da kablonun aday
  ucunda) gösterilir. Açılış belgesi ve öğretici bölge adını `Loc.F` ile alır, gerçek yer adı taşımaz (anahtar 0'da da aynı
  nötr cümle); 7 bina notu yerden bağımsız yazıldı ("Kutup veri merkezi", "Petrol rafinerisi" iki binanın yeni adı).
- **Yerleşim (anahtar 1):** harekât başında bir kez hesaplanır, sonra yalnız renk değişir. Ad `MapRegion.Inner` aday sırasıyla,
  iki dilde ölçülür ve iki dilde aynı yerde durur; punto 34 → 31 → 28 (A10 alt sınırının üstünde); sığarsa tek satır, değilse
  ~200 px'te iki satır. Önce ad ve altındakilerin (yüzde, ADAY, ✓✓✓) kendi bölgesinde kaldığı yer aranır; çakışma listesi:
  adlar, cephe bloğu, ADAY/ÖZGÜR damgaları, raptiye, figür tabanı, S-07, lejant, kablo raptiyeleri ve K2'de daktilo ile
  kalemin altında kalan yerler. Cephe bloğu başka bölgenin karasına ve kendi konturunun üstüne konmamaya, figür tabanı kendi
  karasında ve kablo raptiyesinden uzak durmaya çalışır. Lejant altında en az kara kalan köşeye geçer. Görev fişi etiket sol
  yarıdaysa sağda açılır. S-07 `HqCell`'e kilitli değil: karargâhın iç hücrelerinden adı, ilk cephe bloğu ve figürüyle birlikte
  en iyi sığan yere konur; konturdan payla içeride, karargâhın komşularına giden aday oklarına ve kablo iplerine değmeden,
  cephe satırına yapışmadan. Karargâh (oyuncunun ilk ekranı) birinci turda adını ve cephe bloğunu hiç bırakmaz (ad kendi
  karasında, blok başka bölgenin karasında değil, şerit konturu kesmiyor); sığdıkça daha sıkısını da ister: figür tabanı
  içeride, bütün blok ve raptiye kendi karasında, blok kablo ipine değmiyor (komşusu sıkışırsa bu kademe bırakılır). Raptiye
  adın iki yanından karada kalanına konabilir. Karargâhın adı, figürü, raptiyesi ve S-07'si koyu konturdan da pay bırakır.
- **Denge (bot, `Tools/DengeBotu`, iyi küme, `PICK=rastgele`; tarihsel, g5'ten önceki üretici 3 ölçümü):** üretici 3
  dünya haritasından 200 tohumda −5,5 puan (se 3,5; kabul sınırı −5'in hemen dışında), 400 tohumda −3,5 (se 2,5; sınırın
  içinde); üretici 2'ye göre −1,1 (se 1,7).

## Zorluk kademesi (`Simulation.RaidTier`, `RaidParams.Tier`)

Harekât ilerledikçe baskınlar sertleşir. Kademe özgür bölge sayısına bağlıdır (`liberationTier2At/3At/4At`: 2, 9, 12;
tek kip denge turu 2026-11-22, önce 2, 7, 12; KOS aşama 8'den önce 2, 6, 10):

| Kademe | Özgür bölge | Başta ek makine | Uyanma / tur | Makine canı | İmza makinesi |
|---|---|---|---|---|---|
| 1 | 0–1 | 0 | 1 | normal | 1 |
| 2 | 2–8 | 1 | 1 | normal | 1 |
| 3 | 9–11 | 2 | 2 (`"kopya"`) | normal | 2 |
| 4 | 12+ | 3 | 2 | +1 (`"guclu"`) | 2 |

12 bölgelik harekâtta 4. kademe gelmez: son bölgeye 11 özgür bölgeyle girilir. Kural kodda durur ve `Balance` ile geri
açılır (eşik 10 ile son iki bölge 4. kademedir). Neden: bot ölçümünde (2026-11-14) 4. kademe baskınların %30'u başarısız,
baskın başına 2,06 sivil kırılıyordu; dagilma ve harekât uzunluğu en çok buradan geliyordu (aşağıda "Denge ölçümü").
Göstergeler (haritanın işaretler kutusu, görev fişinin TEHDİT satırı, cephe onay fişi) paydayı `Simulation.MaxTier`'den okur
(son bölge alınınca harekât biter, kademe 11 özgür bölgeyle hesaplanır): uygulanan dengede "TEHDİT n/3"; baskın tahtasının
kademe cümlesi paydasız ("Tehdit 2: …").

Ek makineler tahtanın uzak yarısına konur; dron, bakım robotu ya da (üçte bir) bölgenin imza makinesi olur. Uyanan
makineler de aynı dağılımla seçilir (`Baskin.WakeType`). Görev çipi ve görev fişi kademeyi yazar ("Tehdit 2/4").

## Bölge kimliği (`RegionDef.Trait`, `RegionDef.Enemy`, `RegionDef.Event`)

Her bölgenin kendine özgü bir tahta özelliği (`Baskin.RegionTraits`), bir **imza makinesi** ve bir **bölge olayı**
vardır; altı yeni makinenin her biri iki bölgede. Özellik bölgenin her baskınında `RaidParams.Traits`'e girer; imza makinesi tahtadaki makinelerden
birinin (3. kademeden ikisinin) yerini alır. Görev fişi ve bölge bilgi fişi üçünü de yazar (BÖLGE, OLAY, İMZA); baskında
görev çipinde özellik, üstüne gelince açıklaması, olay, imza makinesi ve kademe görünür.

| Bölge | Özellik | Etkisi | İmza makinesi | Olay |
|---|---|---|---|---|
| Batı Avrupa (başlangıç) | Güvenli giriş | İlk tur uyanma yok | Zırhlı dron | Söndürme bulutu |
| Doğu Avrupa | Dar koridor | +3 duvar | Kilitleyici | Kayar arşiv rafı |
| Kuzey Amerika | Kısa nöbet | Baskın bir tur kısa | Havan robotu | Yük asansörü |
| Güney Amerika | Takviye | Başta +1 makine | Patlayan robot | Basınçlı tüp rafı |
| Afrika | Açık arazi | −3 duvar | Havan robotu | Çatlak tavan |
| Orta Doğu | Kum fırtınası | Uyanma yerleri gizli | Patlayan robot | Yanıcı gaz kaçağı |
| Rusya | Soğutma ağı | +2 soğutma ünitesi | Kalkan dronu | Kriyo soğutma hattı |
| Hindistan | Kalabalık şehir | +2 sivil sunucu | Kalkan dronu | Vardiya turnikeleri |
| Çin | Zırhlı dolap | Hedef dolaplar +1 can | Çoğaltıcı | Kaynak robotu kolu |
| Japonya-Kore | Elektrik şebekesi | +3 elektrikli kanal | Kilitleyici | Şebeke faz değişimi |
| Güneydoğu Asya | Kopya fabrikası | +1 kopya dolabı | Çoğaltıcı | Süzgeç emmesi |
| Okyanusya | Hızlı devriye | Makineler +1 yürür | Zırhlı dron | Hidrolik yağ sızıntısı |

**Bölge olayı** (`RegionDef.Event`, `Core/Baskin.Events.cs`; kurallar ve gerekçeleri `Docs/BOLGE-OLAYLARI.md`): baskın
boyunca tur tur işleyen bina içi bir sistem. Kural tarafa bakmaz; aynı karedeki uzman ile makine aynı sonucu alır. Takvimli
adımlar ve fitiller makine evresinin sonunda, uyanmadan sonra ve plandan önce işler; N. turda tetiklenen olay N+1. turun evre
sonunda (fitil), son turda olay adımı yok. Bir birim bir olay adımından öbürüne olaylardan en çok 1 hasar alır, olaylar
nesnelere hasar vermez, hiçbir olay kaçış yolunu kesmez. Olay bir tur önce tahtada işaretlenir; takım kartı ve yürüyüş
önizlemesi uzmanın evre sonunda alacağını yazar. Baskin sahnesi tek başına açılınca kurulan örnek tahta olaysız; `Balance.liberationRegionEvents` hepsini kapatır.

**Tesis şartları (aşama 12; alt bayraklar `facilityKatar`, `facilityYalpa`, ikisi de varsayılan açık):** tesis kipinde
Kuzey Amerika'nın (WAYPOINT) olayı **Katar**, Okyanusya'nın (HALCYON) olayı **Yalpa** olur. Katar: vagonların kuzey ve güney kenarı
açıktır, dışarı itilen makine yok olur, ekip üyesi ölmez ama 5 vardiya yatar; dönüşte bağlantıya yakın makineler sonraki vagona takip
eder, Teknisyen kapıdayken vagonu ayırabilir (AYIR). Yalpa: 2., 4. ve 6. tur sonunda zemin batıya, doğuya, batıya yatar; sabitlenmemiş her
şey bir kare kayar. İki binanın adı bayraktan bağımsız değişti: "Sevkiyat katarı" ve "Yüzer maden platformu" (alt bayraklar kapalıyken olaylar
yük asansörü ve yağ sızıntısıdır). Kurallar `Docs/TESIS-BASKINLARI-TASARIMI.md` §10 ve "Uygulanan hâl (aşama 12)".

**Tesis imzaları (aşama 13).** Tesis kipi ve `facilitySignatureMachines` açıkken her YZ'nin ayrı makinesi vardır.
HEADWATER alev hattı açar, TESSEL kalas örer, WAYPOINT koşup iter, PARAGON hurdayı drona çevirir,
BOREALIS yürüyüşü durduran kırağı atar, HALCYON düz hat üzerinde çeker. Makine kartı etkisini, tahta niyetini gösterir.
Üretilmiş g5 haritasında paketler YZ adını taşır ve önceki imza komşuluğu sınırlaması kalkar. Kurallar tasarım §6.5'tedir.

**Sahadaki sızmacı (aşama 15; alt bayrak `infiltratorFieldEnabled`, kayıt 21'den beri varsayılan açık).** Saha alt bayrağı açıkken EMP sonrası kilitlenme ve köpeğin hırlaması şüphe uyandırır. İnsanlar da bu belirtileri verebilir. Sessiz bir sızmacı, sunucu baskınının hedef odasında ikinci turda taraf değiştirip kendi uzmanlığıyla ekibe saldırabilir. Oyuncu ilk saldırıdan önce niyeti görür. Yakalanan kişi sığınağa tecritte döner. Oyuncu olay tutanağında sorgu veya sökümü seçer. Tesiste kalan kişi kadrodan kalıcı olarak çıkar.

**Kişisel unvanlar (aşama 14).** Tesis kipinde bir kişi yaptığı işlerle Nişancı, Sırtlayan, Koçbaşı, Köpekçi, Kurucu veya
Atıcı unvanını alabilir. Başarılı bir baskın, yapılan her işe bir ilerleme verir. Kanadın kayıptan sonra yeniden kazanımı
unvan ilerlemesi dışında kalır. Yeni unvan operasyon raporunda, kalıcı etkisi kişi dosyasında görünür. Kişi başına varsayılan
sınır iki unvandır. Eşikler ve etkiler tesis tasarımı §6.4'te.

Duvar ve nesne ekleyen özellikler ekip girişinin yanını boş bırakır ve yolu kapatmaz (`Baskin.Connected`); kapatacak
parça eklenmez.

## Sivil güven (`S.Trust`, `CampaignLiberation.cs → ChangeTrust`)

- Harekât `liberationTrustStart` (10) güvenle başlar; tavan `liberationTrustMax` (20, `Simulation.TrustMax`).
- Baskında kırılan her sivil sunucu (`RaidResult.Collateral`, kimin kırdığına bakılmaz) güveni `liberationTrustPerCivilian`
  (1) düşürür; başarılı ya da başarısız her baskında. **Kazanılan** baskında tahtada ayakta kalan her
  `liberationCiviliansPerTrust` (2) sivil sunucu +1 geri verir (`RaidResult.CiviliansLeft`; kaybedilen baskının sivilleri
  işgal altında kalır). Özgürleşen her bölge `liberationTrustPerRegion` (4; KOS aşama 8, önce 3) geri verir. Değişim `LiberationRaidReward.Trust`'a
  yazılır; operasyon raporu "Kırılan sivil sunucu" ve "Sivil güven +1 · 11/20" satırlarını gösterir, güven 3 ve altına
  inince başlığın altındaki cümle uyarır. Kampanyada sivil sunucu `liberationCivilianHp` (3; tek kip denge turu 2026-11-22, önce 2) canlıdır: bakım robotunun 2 hasarlı yakın saldırısı sivili tek turda kırmaz.
- Güven 0 olunca harekât **Dağılma** ile biter; son rapor KAYBEDİLDİ ve DAĞILMA damgalarıyla açılır.
- **Ekibe ulaşamayan makine sivile nişan alır** (gelişmiş akıl; `Baskin.CanReachTeam`, `ScoreTarget(..., hostage)`; turda en
  çok bir makine, niyeti önceden görünür). Ulaşabilen makine sivili hedeflemez (sivil hattı −2). Sivil sunucu başka yoldan da
  kırılır: hedefi atıştan kaçan ekip arkasındaki sunucuyu açıkta bırakır, EMP, itme ve patlayan soğutma ünitesi de kırar.
  Oyuncunun işi: uzaktaki makineyi düşürmek ya da araya girmek; kaçmak mı (sunucu gider), atışı göğüslemek mi (1 hasar).
- **Denge ölçümü** (2026-10-28, tek tahta dönemi, açgözlü taktik bot, havuz haritaları, 10'ar harekât; ayrıntı `Docs/DURUM.md`): kural açıkken
  makineler baskın başına ~1 sivil kırıyor, ~3,6 sivil ayakta kalıyor. Ödülsüz ve tavan 10 iken hiçbir harekât
  kazanılmıyordu (0/10); ayakta kalan her sivil +1 iken güven hiç düşmüyordu. Seçilen ayarla (kanca hasarı, her 2 sivil +1,
  tavan 40) orta ve dikkatli bot 8/10 kazanıyor, harekâtların ~%20'si dağılıyor; baskın başarısı %60–65. Kapı geldikten
  sonra tavan 20'ye indi (kullanıcının seçimi): orta bot 30'ar harekâtta kapısız 20/30 kazanıyor, ~%30'u dağılıyor; dağılmalar
  harekâtın sonunda (gün 49–76), güven her harekâtta tavana değiyor ve biriktirilemiyor.

## YZ alarmı ve karşı saldırı (`S.Alarm`, `CampaignLiberation.cs → ChangeAlarm, Counterattack`)

- **Bir baskın, bir vardiya:** baskından dönen ekip (yaralı olmasa da) o vardiya yeniden çıkamaz; `Busy` adımda düşer.
  Eskiden ekip dönüşte hazırdı; bütün harekât 1–3 vardiyada bitebiliyordu, vardiya şalterinin anlamı yoktu.
- **Alarm** her vardiya `liberationAlarmPerShift` (+0,5; tek kip denge turu 2026-11-22, önce +1), her başarısız baskın `liberationAlarmPerFailure` (+1) yükselir;
  kırılan her YZ sunucusu (sunucu merkezi sektörü) `liberationAlarmPerServer` (−4) düşürür.
- **Eski modelde telsiz odası** vardiya alarmını yavaşlatır: görevli başına `shelterRadioAlarmPerWorker` (−0,05; eksiye inmez,
  `Simulation.AlarmPerShift`). Yeni panoda anonim oda işçileri sıfırlandığı için bu indirim yoktur.
- **Eşik** `liberationAlarmThreshold` (10): alarm sıfırlanır. Savunma alt bayrağı (`facilityDefenseEnabled`) kapalıyken YZ hemen **karşı saldırır**: en son geri alınan sektör yeniden
  işgal altına girer (özgür bölgeyse bölge yeniden cephe olur, sınırdan seçilir) ve sivil güven
  `liberationCounterattackTrust` (1) düşer. Geri alınacak sektör yoksa yalnız güven düşer; boşta beklemek de sonunda
  Dağılma'ya varır. Yeniden özgürleşen bölge güveni ve bölge fonunu yeniden verir (ölçümde de böyleydi).
- **Varsayılan: kuşatma (aşama 11; alt bayrak `facilityDefenseEnabled` açık):** eşik kanadı hemen düşürmez,
  **kuşatma** kurar (`Core/Simulation.Siege.cs`): sonraki vardiya (karar vardiyası) oyuncu ya kanadı üç odada savunur (`siege_defend`; fon
  yok, vardiyanın baskın hakkı; 10 tur, üç dalga, saha rölesi) ya da bırakır (`siege_yield`; bugünkü sonuç). Karar verilmezse vardiya
  kapanışında bırakılır. Kazanılan savunmada kanat KYGA'da kalır (terk edilen oda başına güven −1), kaybedilende düşer. Yeniden
  özgürleşen bölge fon ve bölge güvenini ikinci kez vermez. Kurallar `Docs/TESIS-BASKINLARI-TASARIMI.md` §9.2–9.3 ve "Uygulanan hâl
  (aşama 11)".
- **Gösterim:** arka duvarda **YZ ALARMI panosu** (`AlarmPanel`: mekanik sayaç n/10 ve kırmızı ışık; eşiğe 3 basamak
  kala yanar, son basamakta yanıp söner; üzerine gelince eşik ve bu vardiyanın artışı), CRT'de "YZ ALARMI n/10" (bekleme
  ve devre dışı ekranı), kenar satırında ALARM, vardiya raporunun özetinde YZ ALARMI gözü (n/10, on adımlık ölçek, "4 → 5 ·
  10'da karşı saldırı"), operasyon raporunda "YZ alarmı" ve karşı saldırı satırları. Karşı saldırıda (Revizyon 2 · S5b)
  rapordan önce duvardaki alarm lambası üç kez yanar, raporun başında çift çizgili "YZ KARŞI SALDIRISI" kutusu (teleks kodu +
  sektör), alarm gözü kırmızı "10/10 · EŞİK", haritada sektöre ✕ ve YENİDEN İŞGAL damgası, teleks (YZ KARŞI SALDIRISI);
  bölge özgürlüğünü yitirdiyse kartı panodan düşer ve ÖZGÜR BÖLGE bir geri döner. Son raporun kırılma anlarında “YZ karşı
  saldırısı: Doğu Avrupa 01 yeniden işgal”, şeritte yukarı çıkıntı.
- **Denge ölçümü** (2026-10-27, açgözlü bot, 10'ar harekât): eşik 14'ken (sığınak baskına bağlanmadan önce) saldırgan
  0/10, dengeli 6/10, koruyucu 9/10. Sığınak hazırlık yeri olunca telsiz alarmı neredeyse siliyordu; eşik 10'a, telsiz
  görevli başına −0,05'e indi (son ölçüm aşağıda, Sığınak). Sessiz vardiyanın alarmı düşürdüğü kural denendi ve
  bırakıldı: dikkatli oyuncu bedelsiz bekleyip hiç saldırı yemiyordu.

## Güncel pano üretimi

Üretim alanına bırakılan hammaddeler `ProductionInputs` içinde adetle ayrılır; stok kapanışa kadar harcanmaz.
Her bırakış bir adet, üretimden depoya geri sürükleme bir adet geri almadır. Üretici ve depo yeri varsa uygun tarif
sonraki vardiyada tek ürün verir. Malzemesi eksik kombinasyon işlem yapmaz; ayrılan malzemeler korunur.
Birden fazla eşleşmede eksik temel saha kiti önceliklidir, sonra defter sırası uygulanır. Defterin bu moddaki tarifleri
bir sonraki kapanışta tamamlanır; aşağıdaki eski çok vardiyalı emir kuralları yalnız eski komut geçmişi/başlamış işler içindir.

Üretim kartları alana bırakılan serbest konumda kalır; sabit konuma yapışmaz ve yerleri kayıtla korunur.
Depo altta iki yüzeydir: Hammadde ve Kullanılabilir Kitler. Panodaki EĞİTİM alanı 2026-11-24'te kaldırıldı (yeri boş, tasarıma açık). Boşta destesinin görünen üst kartı doğrudan sürüklenir; alttaki kişi destede kalır.
Yalnız tıklama kartları görünen üst karttan başlayarak sola tek tek açar. Açılan kartların arkasında ek panel, başlık
veya Topla düğmesi yoktur. Açık elden kişi alınırken diğerleri desteye döner. Açık kartlara veya boş pano/görev
alanına tekrar tıklamak eli kapatır. Görevlerden desteye bırakılan kişi, eski göreve uğramadan bırakıldığı noktadan
desteye yerleşir. Açılış üst karttan, toplanma en alttaki karttan başlar; 320 ms hareket ve 70 ms
kart aralığıyla hareketler üst üste biner. Yarım harekette yön değişimi mevcut konumdan devam eder.

## Vardiya (`Simulation.Step`)

Vardiya işlemi panoda değildir. Masadaki kapaklı **vardiya şalterinin** kapağı kaldırılır, kol indirilince şalterin
yanından devir kâğıdı çıkar (`PrintShift`); fiş EVRAK'a girene kadar şalter inik ve kilitli kalır, plakasının ikinci
satırı "fişi EVRAK'a koy" der. Baskın emri verilmişse plan zaten kilitlidir;
fiş basılması da planı kilitler. Kâğıdı okumak veya eline almak zamanı ilerletmez. Etkin kâğıt masadaki mevcut
**EVRAK kutusuna** bırakılınca (`GameController.FileBoardShift` → `Simulation.FileShift`) ekran kararır, `Step` bir
kez çalışır ve yeni vardiya açılır.
Aynı fiş yeniden kullanılamaz. Planın düzeltilmesi gereken bir girdisi varsa fiş basılmaz.

1. **Gelir:** `liberationDailyFunds` (2) + özgür bölge başına `liberationRegionIncome` (0,5) fon (`IncomeBreakdown`).
2. **Ekip:** baskın meşguliyeti bir azalır. Yeni panoda anonim uzman/yedek iyileşmesi yapılmaz.
3. **Kapıdaki grubun süresi:** bekleme süresi dolan aday grubu gider (`AdvanceAdmissions`; KOS aşama 7'den beri yalnız bu).
4. **Kişiler ve pano işleri:** tecrit ve kısıt süreleri, sızma saatleri ve vadesi gelen izler; erzak dengesi (tayın kipi,
   taksitler), aç vardiya sayacı ve ayrılma; iyileşme (kayıt 22: Boşta ve görevde; yarım tayında yok), yatış süresi; üretim (karartmada yok) ve en
   yaralı kişiye bakım (`AdvancePlanningBoard`). Tamamlanan eşya depoya eklenir, plan açılır.
5. **Olaylar, kapı ve sonlar:** vadeli olay sonuçları ve tecrit bulgusu; kapı (acil koşulu, grup üretimi); sığınağın sonları
   (`topluluk`, `dusus`, `terk`); olay açılışı; uyarılar (ifşa eşiği, acil kapı). Sıra ve RNG çekimleri:
   [KABUL-OLAYLAR-VE-SONLAR-TASARIMI.md](KABUL-OLAYLAR-VE-SONLAR-TASARIMI.md) §6.
6. **YZ alarmı ve geçmiş:** alarm artar (ifşa eşikte ara bedeliyle), eşikte karşı saldırı işler; işgal geçmişi kayda alınır.

Baskın sürerken, harekât bitmişken ya da açılış/emir belgesi açıkken kapanış yapılamaz. Kartlı oyunda doğrudan
`AdvanceShift`/`Step` çağrısı fiş olmadan gün ilerletmez. Eski modelde şalter, `AdvanceShift` ve vardiya raporu akışı sürer.

## Baskın

### Baskın emri ve ortak tahta kuralları

Kayıt 21'den beri baskın her kanatta üç odalı tesis olarak oynanır (aşağıda "Tesis kanadında bir vardiya"); her baskının
tek 8×8 tahtada geçtiği eski yol kaldırıldı. Bu bölüm emri, kadroyu, her odada geçerli tahta kurallarını ve sonucu anlatır.

- **Emir** (`StartLiberationRaid`): vardiyada bir kez, yalnız mevcut bölgenin sıradaki sektörüne, hazır bir ekip ve `liberationRaidCost`
  (12) fonla; yeni panoda üç saha görevi ve uygun kitleri tamamlanmış olmalı. Baskın bölgenin özelliğini, imza makinesini ve zorluk
  kademesini taşır (yukarıda). Kamera önce masadan geri çekilir, CRT'ye döner ve ekrana yaklaşır; telsiz hattı açılır
  ve `Baskin` sahnesi odanın üstüne yüklenir (boşluk ya da tık atlar). Dönüşte kamera aynı yoldan haritaya geri gelir.
  Baskında makineler saldırılarından sonra yeni yerlerine tek tek, kare kare yürür.
- **Kişi ve rol seçimi** (`PlanningSquad`, `PlanningRaidCheck`): Teknisyen, EMP uzmanı ve Analist görev
  alanlarına panodaki üç ayrı sağlıklı kişinin kartı bırakılır; her kişinin uygun rol kiti olmalıdır. Kısıtlı kabuldeki kişi sahaya çıkamaz. Eksik bir
  rol varsa baskın emri verilmez. Kit ve eğitim kişiye aittir; görev değişince kendiliğinden aktarılmaz. Baskın emri
  planı kilitler; dönüşte görülen yaraya göre aynı vardiya yeniden iş dağıtılamaz.
- **Gerçek kadro** (`SquadSlot`, `RaidParams.Squad`): kişinin kimliği/adı, mevcut ve azami canı, hareketi ve fiziksel
  gücü tahtaya taşınır. Rol ustalığı 2. kademede +1 hareket (tavan 5), 3. kademede +1 fiziksel güç (tavan 2) verir;
  1. kademe yatan yaralının toparlanmasını bir vardiya kısaltır. Güç Teknisyenin fiziksel robot vuruşunu etkiler;
  aygıtların bütün hasarlarına genel bonus değildir. Takılan zırh ilk hasarı karşılar ve kırılır. EMP hücresi baskına
  girişte sarf edilir ve EMP'nin ilk kullanımına +1 hasar verir; hedef/hamle önizlemesi hücreyi tüketmez.
- **Eski kadro yolu:** `planningBoardEnabled = false` iken anonim beş uzman, atölye seviyesiyle Madenci/Sis açılması,
  yedekler ve görev fişindeki iki uzman seçimi çalışır. Bu oda gelişimi yeni panoda kullanılmaz.
- **Kadro raporu** (`Baskin.SquadReport`, `Simulation.LiberationSquadPreview`): emirden önce kişi adı, rol, can,
  hareket, fiziksel güç ve teçhizat etkisini gösterir. Rapor ile gerçek baskın aynı `SquadSlot` değerlerini kullanır.
  Eski modelin yatan uzman/yedek raporu o modelde kalır. Emir rapordaki BASKINA ÇIK düğmesiyle verilir; Vazgeç fişe döner.
- **Harita üreticisi** (`Core/RaidMaps.cs`): tesis odalarının haritalarını üretir; `FacilityMaps` her odayı `RaidMaps.Generate`
  çıktısını giydirerek kurar (adlar 30 oda adından: Kablo galerisi, Arşiv katı, Kazan dairesi…). Bölge özelliği (dar koridor,
  şebeke…) haritanın üstüne eklenir. Eski tek tahtanın harekât harita havuzu (30 harita), karıştırılmış torbası
  (`RaidMaps.Pool`/`Slot`, `RaidParams.MapSlot`) ve görev ek turları (`liberationLootExtraTurns`, `liberationServerExtraTurns`)
  kayıt 21'de kaldırıldı. Koddan kurulan örnek/test tahtası (`Baskin.Create`: Baskin sahnesi tek başına açılınca, editör
  önizlemesi, motor testleri; kampanyada kullanılmaz) elle çizilmiş üç haritada (`Baskin.Layouts`) kalır ve `MapSeed`'den
  etkilenmez. `RaidParams.MapSeed` (= harekâtın tohumu) harekât baskınının işareti olarak kalır: sunum bölgenin bina kitini
  onunla seçer (0 = örnek tahta, varsayılan kit).
- **Harita türleri** (`RaidMaps.Kinds`; her türün kendi 5 oda adı; tesis odaları bu türlerden üretilir):

  | Tür | Ad örneği | Yerleşim | Taktik sorun | Hedefe adım |
  |---|---|---|---|---|
  | Koridor | Kablo galerisi | hedef 6. sütunda korunağıyla, dağınık 2–3 karelik duvarlar | (önceki haritalar) | 7–10 |
  | Kasa odası | Arşiv katı | hedef sağ kenara dayalı 2–3 × 2–3'lük odada, iki kapı; bakım robotu içeride | kapıdan girmek, onaran robot | 6–9 |
  | İki hat | Montaj hattı | uzun bölme duvarı tahtayı iki şeride ayırır (bazen ortada geçit); hedef bölmeye dayalı | şerit seçmek, dönüş yolu | 8–11 |
  | Açık salon | Sunucu salonu | 5–7 tek karelik sütun, birbirine değmez | açık atış hatları, az siper | 8–10 |
  | Şebeke | Trafo merkezi | 3.–4. sütunda boydan boya kablo kuşağı | itilen yanar (EMP, kanca, makineler) | 7–10 |
  | Rehine | Kazan dairesi | üç sivil yolun ortasında (2.–5. sütun) | ateş hattındaki siviller | 7–10 |

  Ortak kurallar (`RaidMaps.Valid`): sol kenarda üç giriş, tek hedef dolap ve yanında duvar, 5–13 duvar, 2–8 kablo, 4–5
  sivil, 2 soğutma, 2 kopya dolabı, 2 dron + 1 bakım robotu, dronların girişe açık hattı yok, hedefe 6–11 adım; türün
  kuralları `Valid(rows, kind)`. **Ölçüm** (2026-10-28, tek tahta dönemi, açgözlü bot, türü başına 40 harita × 4 kademe × 2 görev = 320
  baskın, bölge özellikleri ve makineleri; adım aralıkları bununla ayarlandı): baskın başarısı koridor %72, kasa odası %72
  (ilk denemede tek kapı ve 8–11 adımla %55), iki hat %74, açık salon %69 (ilk denemede %83), şebeke %75, rehine %72.
  4. kademede koridor en zor tür (%38; ötekiler %47–61). Harekât ölçümünde (30 harekât) yalnız koridorlu havuz 19/30,
  türlü havuz 24/30 kazandı; baskın başarısı %66 → %68 (fark gürültü sınırında, ama 4. kademedeki farkla uyumlu).
- **Taktik tahta** (`Core/Baskin.cs`, her oda 8×8; tesiste oda süreleri aşağıdaki `facility*Turns` alanlarından gelir ve Kısa nöbet hedef odasının ilk ziyaretini bir tur kısaltır; örnek tahta 6 tur, Kısa nöbette 5): oyuncu birimleri

  | Uzman | Can · yürüyüş | Yetenek |
  |---|---|---|
  | Teknisyen (T) | 3 · 3 | Sabotaj: bitişik kare; dolaba 2, robota 1 hasar ve itme (ikmal kasasını her uzman açar) |
  | EMP uzmanı (E) | 3 · 3 | EMP atışı: düz hatta 2–5 kare, engellerin üstünden; 1 hasar, çevresini dışarı iter |
  | Analist (A) | 2 · 4 | Kanca: düz hatta 5 kare; robota 1 hasar ve çekme, dost uzmanı hasarsız çeker; duvara atılır |
  | Madenci (M) | 3 · 3 | Dinamit: bitişik duvarı kalıcı olarak yıkar (baskın başına `liberationDynamiteCharges` = 2); moloz aynı yönde bir kare öteye 1 hasar (duvarda ve kenarda durur; hedef dolaba ve ikmal kasasına işlemez; soğutma ünitesini patlatır). Robota ya da sıradan nesneye 1 hasar (hak harcamaz). Hedef dolaba, kasaya ve dosta atılmaz |
  | Sis uzmanı (S) | 2 · 3 | Sis bombası: düz hatta 1–4 kare, engellerin üstünden (duvara değil; baskın başına `liberationSmokeCharges` = 3). Sis yalnız bu turun makine evresinde durur, makineler saldırdıktan hemen sonra dağılır (plan onu hiç görmez) |

  **Sis kuralı** (`Baskin.IntentTarget`): atış ve kilit sise girince kaybolur (sisli kare ve arkası vurulmaz); yakın vuruş
  ve havan sisli kareye vuramaz (havanda yalnız düştüğü kare sayılır). Sisin içindeki makine yine ateş eder. Patlamalar
  (patlayan robot, soğutma ünitesi, moloz), uyanma ve çoğaltma hasarı, onarım ve kalkan, hareket ve itme etkilenmez; bu
  yüzden oyunda "korur" değil "nişanlı ateşi keser" yazar. Yıkılan duvarın bedeli: açılan delik makinelerin atış hattını
  da açar, kopya dolabının yanındaki yıkık duvar uyanma karesi olur. Önizleme hamleden sonraki niyetleri gösterir
  (`PreviewResult.Intents`): sise kesilen atış gri kesik çizgiyle sisli karede biter, dinamitin açtığı hat görünür. Hak
  sayıları ölçülmedi (aşağıda, Zorluk ölçümü).

  Kopya dolapları her tur makine uyandırır. Makineler (`Baskin.UnitTypes`; niyet türü `Kinds`):

  | Makine | Can · yürüyüş | Niyet |
  |---|---|---|
  | Güvenlik dronu | 2 · 3 | Ateş: düz hatta ilk engele 1 hasar |
  | Bakım robotu | 2 · 2 | Vuruş: bitişik kareye 2 hasar; ya da hasarlı hedef dolabı onarır |
  | Zırhlı dron | 2 · 3, zırh 1 | Dron gibi ateş; zırhı ilk hasarı karşılar |
  | Havan robotu | 2 · 1 | Havan: 3 kare öteye, engellerin üstünden 1 hasar |
  | Patlayan robot | 1 · 3 | Patlama: çevresindeki 4 kareye 1 hasar, kendisi de düşer |
  | Kilitleyici | 2 · 2 | Kilit: düz hatta 4 kareye kadar; vurulan uzman gelecek tur yürüyemez (yeteneği serbest) |
  | Kalkan dronu | 2 · 3 | Kalkan: yanındaki makineye zırh 1; verecek yoksa ateş |
  | Çoğaltıcı | 2 · 2 | Çoğaltma: yanındaki boş kareye bir dron uyandırır (karede biri varsa o 1 hasar alır) |
 Makinelerin bu turdaki hamlesi okla, sırası bayrakla görünür: zarar verenler kırmızı (havan yüksek yay, kilitte kıskaç,
  onarımda anahtar rozeti), kalkan ve kopyalama kahverengi. Kilit niyetinin hedefindeki uzmanın kartında KİLİT TEHDİDİ ·
  #n; yeni uyanan robotun üstünde bir tur UYANDI. Son iki tur tur şeridinde işaretli, son turda SON TUR. Hamleler geri
  alınabilir (`Undo`), önizleme durumu değiştirmez. Kırılan sivil sunucular sivil güveni düşürür (aşağıda).
  - **İkmal:** ikmal kasasına bitişik kareden **her uzman** yeteneğiyle malzemeyi alır ve **taşır** (`BState.Carrier`; tesis sürüm 10,
    önce yalnız Teknisyen). Açmak yetenek hakkını harcar, şarj harcamaz; köpek, tutsak ve aygıt açamaz. Kasayı kırmak görevi kaybettirir.
  - **Kaçış** (`BState.Escape`): görevin hedefi tamamlanınca (kasa açıldı ya da son hedef dolap kırıldı) bütün ekip giriş
    karelerinden (`BState.Exits`, yeşil kesik çerçeve) çıkmalı. Çıkışa basan uzman (yürüyüş, kanca, itme ya da kaçış
    başlarken orada durmak) tahliye edilir ve tahtadan kalkar; baskın tahtada uzman kalmayınca ya da süre dolunca biter. Süre
    dolarken tahtada kalan uzman **baskında düşmüş sayılır** (rapor "Çıkamadı"; `RaidResult.Left`, kampanyada ölür). Sunucu görevi hedef
    kırıldığı için kazanılır; ikmal görevi malzemeyi taşıyan uzman çıktıysa (kaçışta düşerse malzeme gider, kalanlar yine
    çıkmalı). Kaçışta geri çekilme yok; kaçıştan önce geri çekilinirse ya da süre dolarsa baskın kaybedilir: ayaktakiler döner, yerde
    yatanlar ölür (tesisin giriş ve ara odasında süre dolması geri çekilme sayılır; aşağıda "Kişi döner"). Sürüm 10: giriş ya da ara
    odada süre dolarken kapıda bekleyen varsa baskın bitmez, oda kapıdakilerle geçilir; kapıya varamayan uzman sağ geri çekilir
    (`Unit.Withdrew`, raporda "w"), sonraki odalara girmez ve sığınağa canıyla döner. Tesiste kaçış
    hedef odasından girişe dönüş yoludur; temiz oda yürünerek geçilir, makine kalan oda dönüş tahtasında yeniden oynanır.
  - **Makine aklı** (`RaidParams.Tactics`; kampanyada 1, örnek tahtada ve kilitli motor testlerinde 0): gelişmiş akıl
    kilit kareleri tutar: kasa alınmadan hedef dolapların yanındaki boş kareler (sunucu görevinde daha ağır), kasa
    alındıysa taşıyıcının çıkışa en kısa yolu ve çıkışlar (taşıyıcı çıktıktan ya da sunucu kaçışında tahtadakileri kovalar).
    Vuracak birim yoksa ateş ve kilit hattını bu karelerden
    geçirir (mermi sırası gelince yeniden nişan alır, hatta adım atan vurulur); yakın dövüşçüler kilit kareyi bedeniyle
    tutar; herkes taşıyıcıyı (yoksa Teknisyen'i) kovalar; taşıyıcı ve hedefin yanındaki Teknisyen öncelikli hedef. Ekibe
    hiçbir kareden ulaşamayan makine sivil sunucuya nişan alır (turda en çok bir; yukarıda "Sivil güven").
    Makineler sırayla plan yaparken öncekilerin tuttuğu kareleri saymaz (eşgüdüm). Rastgele tüketimi temel akılla aynı.
  - **Zorluk ölçümü** (tek tahta dönemi, tarihsel; açgözlü bot, havuz haritaları, 1440 baskın; T + E + A kadrosuyla, Madenci ve Sis uzmanı yokken):
    önceki oyun her kademede ~%92. Şimdi (kaçış, gelişmiş
    akıl, 7 turluk ikmal, düğüm +2 can): **toplam %72, ikmal %67, sunucu %76; kademe 1/2/3 %79/%75/%61**. Kadro seçimi
    zorluğu kendiliğinden düşürür (oyuncu her bölgeye en uygun ikiliyi seçer); altı ikilinin ölçümü ve hak sayılarının
    ayarı açık iş (bot depoda yok, yeniden yazılmalı; DURUM). Kaçışla (2026-11-01, 12 tohum): T+E+A ikmal %90, sunucu %91;
    düşen uzman baskın başına 0,39 (eskiden 0,03). 6 turluk ikmal
    %45'te kalıyordu, 8 tur %80. Sunucuda gelişmiş akıl tek başına etkisizdi (%89: niyetler önceden göründüğü için
    Teknisyen tutulmayan kareden sabotaj yapar); düğümün canı +1 %83, +2 %76; ek makine %89 (etkisiz).
  - **Sunucu:** tur bitmeden bütün hedef dolaplar kırılır. Kampanyada düğüm dolabı `liberationServerTargetHp` (+2) canla
    sertleşir (4 → 6; zırhlı dolapta 7); son bölgenin çekirdeği hariç (5 can, 6 turda yetişilsin). Ek makine ayarı
    `liberationServerMachines` 0 (ölçümde etkisiz).
- **Sonuç** (`ResolveRaid`, taktik sonuçtan masaya dönülürken bir kez):
  - İkmal başarısı: +10 fon, +3 metal, +2 devre, +2 kumaş, +1 kimyasal, +4 erzak (`boardLootSupplies`; KOS aşama 8, önce 6). Otomatik anonim kurtulan eklenmez.
  - Sunucu başarısı: +8 fon, +2 metal, +2 devre, +1 kumaş, +1 kimyasal.
  - Bölge tamamlanınca ayrıca +15 fon. Başarısızlık ya da geri çekilme sektörü değiştirmez.
  - **Kişi döner** (`ReturnPlanningSquad`): can ve kırılan plaka kalıcı duruma yazılır. Çıkamayan (`RaidResult.Left`)
    kişi ölür. Düşen (sıfır canlı) kişi tahtada kalır ve yalnız tesisten çıkarılırsa yaşar (`RaidResult.Extracted`):
    sürüklenerek, dönüşte temiz odadan toplanarak ya da "son nefes"le (`Docs/TESIS-BASKINLARI-TASARIMI.md` §4.3, aşama 6).
    Çıkarılan yaralı `boardInjuryTurns` (3, rol ustalığıyla 2 olabilir) vardiya yatışa ve dinlenmeye geçer. Katar'da açık
    kenardan düşen ölmez, yatar (`RaidResult.Overboard`). Ölenin kişisel ustalığı ve üzerindeki eşya kaybolur. Eski tek
    tahtanın "ekipte sağ kalan varsa düşen otomatik kurtarılır" kuralı kayıt 21'de kalktı.
    Canlı dönen kişi rolünde +1 deneyim alır. Herkesin kaybı `topluluk` sonuna yol açar.
  - Eski modelde ikmal +7, sunucu +5 genel malzeme verir ve yer varsa anonim kurtulan ekler; düşen uzman/yedek
    iyileşmek üzere yatar, kalıcı kişi kaybı yoktur. Bu kurallar yeni kadroya ek katkı sağlamaz.
  - Depo sınırına takılan ödül yalnız gerçekten alınan miktarla raporlanır (`LiberationRaidReward`).
- **Sonuç kartı** (baskın tahtasında, Revizyon 2 · S7a/b): tahta kararır, spot nedenin üstünde açılır, sağdan kart kayar:
  başlık yerinde tek damga (SEKTÖR GERİ ALINDI mavi; GERİ ÇEKİLDİ, EKİP DÜŞTÜ, İKMAL KAYBEDİLDİ, SÜRE DOLDU kırmızı), yer
  satırı (“Doğu Avrupa · 02 Dosya katı”), hedef / kırılan sivil sunucu / ekip (kaçış olduysa "Tahliye edilen n/3"), tek
  düğme (0,6 s girdi kilitli).
- **Operasyon raporu** (S7c/d): baskından dönünce kamera harita pozuna geçer, rapor solda iner, iplik ekip figürüne gider.
  Başlık sonucu söyler (SEKTÖR GERİ ALINDI / SEKTÖR İŞGAL ALTINDA), altında tek cümle (bölgenin kalan sektörü ya da
  “Ekip geri döndü…”; Teknisyen yedeksiz düştüyse ya da güven azaldıysa onun uyarısı). Başarılıda KAZANÇ (fon, türlerine ayrılmış hammadde ve erzak; yeni panoda anonim kurtulan yok) ve sayaçlarda ± fişleri, sektör işareti maviye döner; başarısızda ödül alanı yok, YZ alarmı ölçekle.
  SONUÇLAR: sektör, kırılan sivil sunucu, ekip (düşen, yeni yara, kırılan plaka), sivil güven, YZ alarmı, karşı saldırı
  (`OperationReportBuilder`).

### Tesis kanadında bir vardiya

Her baskında haritada bir kanat seçersin (kayıt 21'den beri tek baskın kipi). Emirden önce bina planının üç paftasını incele. Giriş, ara oda ve hedef için varsayılan süreler **6 / 6 / 10 tur**. Hazırlık için ilk iki odadaki donanıma zaman ayırabilirsin. Örneğin anteni kırmak hedefteki makine sayısını azaltır. Hazırlık da geçide yürüyeceğin süreden yer. Herkes geçitteyken GEÇ emriyle ilerle; süre dolarken geçide varamayan geri çekilir, varanlar devam eder. Sonraki odanın girişi ileri geçide simetriktir: geçit kuzeydeyse yeni odanın girişi güneyde, aynı sütunlardadır (oda gerektiğinde döndürülür). Pafta geçidin tersine kayıp gider, yeni oda geçidin yönünden gelir. Hedefte ikmal kasasını herhangi bir uzmanla al veya sunucu görevini bitir. Geldiğin yoldan dön. Temiz odayı yürüyerek geçersin, makine kalmışsa odayı tekrar oynarsın.

- **Hamle ve tahliye.** Q temel yeteneği, E duruma uygun hamleleri açar. Bir hatta pusu kurabilir, yükü başka birine fırlatabilir veya yerdeki uzmanı geçide sürükleyebilirsin. Geri dönüşte temiz odadan alınan yaralı bir sonraki tehlikeli odada hâlâ taşınmalıdır. Tesisten çıkarılan yaşar.
- **Ödül ve donatım.** Sunucu ödül kâğıdında tarif veya takım kazanımı seç. Açtığın tarifi pano defterinden iş emrine sürükle, üretilen ekipmanı kişi kartına ver. Sahada R ile ekipmanı kullan. Vinç düz hat üzerinde çeker, kelepçe makineyi geçici olarak ekibe alır. Tarifin açık olması, ekipmanın bir uzmana takılmış olması ve kullanılması ayrı aşamalardır.
- **YZ’nin önlemleri.** Tekrarlanan taktikler ilgili sınıfta öğrenmeye yol açar. Planın kırmızı kalemi hazırlanan önlemi gösterir. Cevap tarifi açıldığında bunu defterden üretim emrine alabilirsin.
- **Kuşatma.** Teleks ve harita karar zamanını gösterir. Kanadı bırakabilir veya savunabilirsin. Savunmada varsayılan **10 tur** boyunca röleyi koru. İç odaya çekilmek alan ve güven kaybettirir. Sonradan geri alınan bölge için bölge fonu ve güveni ikinci kez verilmez.
- **Kanada ve kişiye özgü kurallar.** Katar’da açık kenarı ve AYIR’ı, Yalpa’da sonraki yatış yönünü paftadan oku. İmza makinesinin kartı niyetini gösterir. Sağ dönen kişi işine göre en çok **iki unvan** kazanabilir. Saha sızmacısı açıkken (varsayılan) hırlama veya EMP sonrası kilitlenme bir ipucudur; kesin kimlik bilgisi vermez. Bir sızmacı sunucu kanadında hedef odaya kadar sessiz kalabilir.

**Sayılar** (`Core/Balance.cs` C# varsayılanları; kayıt 21'den beri `Settings/BalanceConfig.asset`'te aynı değerlerle yazılı):

| Konu | Varsayılan | `Balance` alanı |
|---|---|---|
| Oda turları | giriş 6, ara 6, hedef 10 (sürüm 10 telafisi 2026-11-24; önce 5, 5, 9; ondan önce hedef 8); dönüş odası 6 (önce 5) | `facilityEntryTurns`, `facilityMiddleTurns`, `facilityTargetTurns`, `facilityReturnTurns` |
| Başlangıç makineleri | giriş 1, ara 1 (bakım robotu); hedef 3 (bir bakım robotu, kalanı dron; kademe ekleri ve imza dönüşümü bunlara eklenir) | `facilityEntryMachines`, `facilityMiddleMachines`, `facilityTargetMachines` |
| Hazırlık donanımının canı | sigorta panosu 2 (giriş), çağrı anteni 2 (giriş ya da ara), soğutma vanası 1 (ara) | `facilityPanelHp`, `facilityAntennaHp`, `facilityValveHp` |
| Kırık pano | hedefte kopya dolapları ilk 2 tur makine uyandırmaz | `facilityPanelQuietTurns` |
| Kırık anten | hedefe 1 makine eksik gelir (önce kademe ekinden, yetmezse başlangıç dronundan) | `facilityAntennaMachines` |
| Kapalı vana | sunucu görevinde hedef dolap 2 can eksik (en az 1 kalır); ikmal görevinde hedefin makineleri ilk 1 tur yürümez | `facilityValveTargetHp`, `facilityValveStillTurns` |
| Hamleler | pusu hattı 4 kare; koşu darbesi en az 3 kare koşar, makineyi 2 kare iter; yük en çok 3 kare fırlatılır; sürükleyenin yürüyüşü 1 eksik | `facilityAmbushRange`, `facilityRushRun`, `facilityRushPush`, `facilityThrowRange`, `facilityDragMoveCost` |
| Dinleme nöbeti | bir odanın planı 1 puan (2026-11-24; önce 3): Gözetim görevlisi her vardiya bir odanın paftasını açar | `facilityIntelCost`, `facilityWatchIntel` |
| Önlem defteri | bir baskın bir sınıfa en çok 3 sayaç ekler; önlem 6 sayaçta öğrenilir | `facilityTallyCap`, `facilityCounterThreshold` |
| Kalıcı izler | işgaldeki her kanatta vardiya başına en eski 1 iz onarılır | `facilityRepairPerShift` |
| Savunma | 10 tur; dalgalar 2., 5. ve 8. turda çıkar (iç odaya gelişi terk edilen oda başına bir tur gecikir); dalga 1 + kademe (en çok 3) makine, kademe 1/2/3/4 → 2/3/4/4; saha rölesi 4 can (Siper +1); terk edilen oda başına güven −1 | `facilityDefenseTurns`, `facilityDefenseFirstWave`, `facilityDefenseWaveTurns`, `facilityDefenseWaves`, `facilityDefenseWaveBase`, `facilityDefenseRelayHp`, `facilityCoverRelayHp`, `facilityDefenseRoomTrust` |
| Katar | açık kenardan düşen 5 vardiya yatar; dönüşte bağlantıya en çok 2 kare yürüme uzaklığındaki makine sonraki vagona geçer | `facilityFallInjuryTurns`, `facilityFollowRange` |
| Unvanlar | kişi başına en çok 2; eşik, o işin yapıldığı başarılı baskın sayısı: Nişancı 3, Sırtlayan 2, Koçbaşı 3, Köpekçi 3, Kurucu 3, Atıcı 2 | `facilityMaxTitles`, `facilityMarksmanAt`, `facilityCarrierAt`, `facilityRamTitleAt`, `facilityHandlerAt`, `facilityBuilderAt`, `facilityThrowerAt` |

**Yedi ekipman** (`FacilityGear.Kinds`, adlar `PlanningBoardData.EquipmentName`; hak `FacilityGear.RaidCharges`, tahta kuralı
`Baskin.Gear.cs`). Aygıtlar (taret, yem, vinç) uzmanın dik komşusundaki boş zemine kurulur, 3 makine evresi çalışır, 2 canlıdır
(`facilityDeviceTurns`, `facilityDeviceHp`) ve süresi bitince hurdaya döner.

| Ekipman | Baskın hakkı | Tahtadaki kural |
|---|---|---|
| Örme seti | 2 (Yedek çanta +1) | uzmanın dik komşusuna tel örgü (1 hak) ya da kalas duvar (2 hak); ikisi de 3 can |
| Dinamit lokumu | 2 (Yedek çanta +1) | dik komşu duvarı yıkar (1 hak) |
| Yaylı taret | 1 | 4 karelik hattındaki ilk ayakta birim makineyse 1 hasar |
| Yürüyen yem | 1 | her evrede yönünde 2 kare yürür; makineler onu ekip üyesi sayar |
| Makaralı vinç | 1 | 4 karelik hattındaki ilk birimi (yoksa gövde ya da yükü) evrede 1 kare çeker (Makara ile 2) |
| Kumanda kelepçesi | 1 | dik komşu makineyi 3 tur ekip için oynatır |
| Köpek | sürekli (hak düşmez) | 2 can (Kürk +1), 5 kare yürüyüş; havla (3 kare erim), getir, kokla; düşerse 3 vardiya panoda kullanılamaz |

Kurallar `Core/` altında: `Simulation.Facility.cs` (tesis planı, oda komutu, sunucu ödülü), `RaidChain.cs` (oda zinciri,
hazırlığın etkisi, oda raporu ve baskın sonucu), `Baskin.Chain.cs` (kapı, GEÇ ve dönüş odasının tahtası), `FacilityMaps.cs`,
`FacilityRoom.cs` ve `Baskin.Rooms.cs` (108 odanın üretimi, oda planı ve oda tahtası), `Baskin.Actions.cs` (hamleler),
`FacilityGear.cs` (ekipman, tarif ve kazanım tabloları) ve `Baskin.Gear.cs` (ekipmanın, köpeğin ve tutsağın tahtadaki kuralı),
`FacilityCounters.cs` ve `Baskin.Counters.cs` (önlem defteri ve tahtadaki önlemler), `FacilityMarks.cs` (kalıcı izler),
`Simulation.Siege.cs` ve `Baskin.Defense.cs` (kuşatma; savunma tahtası, saha rölesi ve dalgalar), `Baskin.Conditions.cs`
(Katar ve Yalpa), `Baskin.Signatures.cs`, `FacilityTitles.cs` ve `Baskin.Infiltrators.cs`; harekât boyu durum `FacilityState.cs`.
Ayrıntılı Katar, Yalpa, unvan ve sızmacı kuralları yukarıdaki **Bölge kimliği** bölümündedir. Bot ölçümü tasarım belgesi §11.6’dır.

## Kartlı sığınak ve topluluk (`PlanningBoardState`)

Sığınak oda seviyesiyle büyümez. Başlangıçta **8 isimli insan**, en fazla 10 canlı sakin vardır. Adlar İngilizce özel
isimlerdir (çevrilmez): 8 kurucu 10 adlık listeden (`CandidateData.Founders`) karıştırılarak, adaylar ayrı 64 adlık listeden
(`CandidateData.Names`) sırayla gelir; portre ada bağlıdır (`PortraitLibrary`). İsimler ve dört beden
profilinden biri `S.Rng` ile belirlenir: dayanıklı/yavaş, çevik/narin, güçlü/temkinli veya dengeli. Temel değerler
3 can, 3 hareket, 1 güçtür; profil bunları bir basamak değiştirir. Meslek bir alanda başlangıç ustalığı verir.
Her kişi tek bir görev taşır. Kişi kartı adı yazılı görev alanına bırakılarak atanır; dolu bir görevin geçerli
ataması iki kişinin görevini takas eder. **Boşta** alanı dinlenme/yedek görevidir ve birden çok kişiyi alır. Kart
konumu Core görevinden yeniden çizilir; pano boşluğuna veya yanlış alana bırakmak görevi değiştirmez.

| Görev | Kapanışta katkı |
|---|---|
| Teknisyen, EMP uzmanı, Analist | Aynı topluluktan üç kişilik baskın kadrosu; görev kiti gerekir |
| Üretim | Girdi ve yer varsa tek iş emrini ilerletir; deneyimli üretici kapanışta +1 ek çalışma ilerlemesi |
| Mutfak | Tüketimden 0,6 + kademe başına 0,1 erzak düşer; toplam tüketimin en fazla yarısı |
| İkmal (yayındaki panoda cebi yok; kilitsiz planda `RetireJobs` Boşta'ya alır) | 1,25 + kademe başına 0,15 erzak; her 3 sağlıklı çalışma vardiyasında 1 metal + 1 devre |
| Bakım ve dönüş triyajı (yayındaki panoda cebi yok; Boşta'ya iner) | Kendisi dışındaki en yaralı canlı kişiye 1 + kademe başına 1 can; varsa bir bakım kitiyle +1 |
| Eğitim (2026-11-24'ten beri panoda yok: cebi ve konu listesi kaldırıldı; Core'daki `board_train` durur ama kayıt 22 eski kayıtları yüklemediğinden ölü yoldur; `RetireJobs` Boşta'ya alır) | Seçilen yedi alandan birine +2 deneyim; kapanışta dinlenmeye döner |
| Dinlenme / yedek (Boşta) | +1 can (`boardRestHeal`); yatış süresi de bir azalır |
| Her görev (kayıt 22) | Yatmayan herkes kapanışta +1 can (`boardDutyHeal`; Boşta `boardRestHeal`); yarım tayında kimse iyileşmez |
| Gözetim, Dış temas (eski görevler; panoda cebi yok) | Katkısı yok (KOS aşama 7: kabul bağları söküldü). Kilitsiz planda `RetireJobs` kişiyi Boşta'ya alır. Tesis kipinde (kayıt 21'den beri hep) Gözetim cebi BOŞTA bandının sağ ucunda durur ve dinleme nöbeti tutar (`Docs/TESIS-BASKINLARI-TASARIMI.md` §7.2); boşta destesi 980 mm'den başlar (cepsiz 1098 mm geometrisi kodda duruyor, sadeleştirmesi ertelendi) |

Saha dönüşü ve uygun iş +1 deneyim verir. Alan kademeleri 2/6/12 deneyimde açılır; 12 tavanıdır. Eğitim kişiye aittir,
rol veya kit değişince silinmez. Üretim ustalığı 2. kademede her kapanışa +1 çalışma ilerlemesi ekler; iki vardiyalık
EMP hücresi deneyimli üreticiyle tek kapanışta tamamlanabilir. Girdi maliyeti değişmez ve bir kez ödenir.

**Stok ve üretim:** başlangıçta 3 metal, 2 devre, 2 kumaş, 1 kimyasal, 24/60 erzak, üç rol kiti, iki zırh ve bir bakım
kiti vardır. Dört hammadde türünün her birinde tavan 60; eşya tavanı 32'dir. Eşyanın tek kimliği ve tek sahibi vardır;
kişi üzerinde aynı anda bir kit, bir zırh ve bir yardımcı eşya tutulur. Tarif açılıp gerekli hammadde kartları girdi
ceplerine bırakılır. Rezervasyon malzemeyi hemen tüketmez. Üreticinin ilk çalıştığı kapanışta girdiler bir kez ödenir;
üretici değişse de ilerleme iş emrinde kalır. İşçi yoksa, açlık varsa veya eşya deposu doluysa üretim durur. Fazla eşya hurda hedefine
bırakılarak metal deposunda yer varsa 1 metal kazandırır. Köpek ve sahibine bağlı eşya hurdaya ayrılamaz.

| Tarif | Girdi | Çalışma süresi |
|---|---|---|
| Zırh plakası | 2 metal + 1 kumaş | 1 vardiya |
| Bakım kiti | 1 kumaş + 1 kimyasal | 1 vardiya |
| EMP hücresi | 1 metal + 1 devre | 2 vardiya |
| Teknisyen / EMP / Analist kiti (üç ayrı tarif) | 1 metal + 1 devre | 1 vardiya |

Tek üretim emri vardır (panoda eğitim yeri 2026-11-24'ten beri yok). Başlamadan iptal rezervasyonu serbest bırakır; başladıktan sonra iptal
harcanan girdiyi geri vermez. Bitmiş eşya depoda kart olur ve kişiye ayrıca takılır. Tarifler şu an baştan açıktır;
sefer içinde tarif bulma ve rol kiti kilidi uygulanmış değildir.

Her canlı kişi vardiyada 0,40 erzak tüketir (`boardSuppliesPerResident`; KOS aşama 8, önce 0,35; `obur` kişi iki katı). Mutfak
açığı küçültür; ikmal baskını +4 erzakla destekler. **Erzak hesabı:** sekiz kişi ve kademe 1 aşçıyla vardiya tüketimi
8 × 0,40 − 0,7 = 2,5. Yayındaki panoda İkmal ve Bakım yuvası yok (toplayıcının 1,25 + kademe başına 0,15'i ve bakımcının
iyileştirmesi yalnız Core'da ve testlerde işler; kilitsiz planda sunumun `RetireJobs`'u bu kişileri Boşta'ya alır). Başlangıçtaki
24 erzak baskınsız yedinci kapanışta 6,8'e iner (aşçı deneyim kazandıkça tüketim 2,4'e düşer), üç vardiyalık tüketimin
altındadır ve `erzak_azaliyor` açılır (tohum 79, Play'de görüldü: V.008); iki ikmal baskını atlanırsa uyarı gelir. Yarım tayın tüketimi aşçıdan sonra yarıya indirir (2,5 → 1,25).
İkmal görevlisinin üç sağlıklı çalışma vardiyasında topladığı metal/devre, bütün saha kitleri kaybedilse bile yeni
kit üretme yolunu açık tutar. Açlıkta veya görevli yokken bu hammadde ilerlemesi durur; birikimi görevliye değil
`Board.GatheringProgress`'e aittir ve kayıttan aynı kurulur. (Yayındaki panoda İkmal cebi olmadığı için bu yol yalnız Core'da
ve testlerde işler; oyunda kit kilidini KOS aşama 1 kuralı açar: eksik temel rol kiti aç kapanışta da, hiç yoksa devresiz üretilir.)
Erzak sıfırın altına düşecekse stok sıfırlanır, o kapanışta üretim ve bakım durur. Bütün değerler `Balance.board*`
alanlarından gelir. Eski sığınak botlarının kazanma oranları bu ekonomi için geçerli değildir; KOS aşama 8 ölçümü aşağıda
("Denge ölçümü"); uzun harekât dengesi
ayrıca ölçülmelidir.

## Aday grubu, sığınak olayları ve sonlar (`CommunityAdmissionState`, `ShelterEventState`)

Tasarımın gerekçeleri, ret edilen seçenekler ve karar tarihçesi:
[KABUL-OLAYLAR-VE-SONLAR-TASARIMI.md](KABUL-OLAYLAR-VE-SONLAR-TASARIMI.md) (KOS aşama 1–8). Bu bölüm uygulanmış hâli ve
sayıları yazar. Sayılar `Balance` varsayılanıdır; KOS aşama 8'de bot ölçümüyle dengelendi (aşağıda "Denge ölçümü").

**İlkeler.** Gizli kimlik (insan ya da sızmacı) adayın üretiminde `S.Rng` ile çekilir ve değişmez. Oyuncunun gördüğü hiçbir
alan (kart, bedel, şüphe satırı, olay kâğıdı, seçeneklerin açıklığı, sayaçlar, izin zamanı) aynı kademedeki insanla sızmacıyı
ayırmaz; kesinlik yalnız sabotajdan, tecrit bulgusundan ve son rapordan gelir. Her sonun sayacı odada ya da fişte görünür; tek
karar harekâtı bitirmez (istisna: son şalter); her olayda son üretmeyen, her durumda açık bir seçenek vardır. Kurucular ve ilk
grubun adayları sızmacı olamaz, kurucular iz bırakmaz. Komutlar ve okuyucular RNG çekmez; çekim sayısı olasılıklara bağlı değildir.

### Kabul: aday grubu (`Simulation.CandidateGroups.cs`, `Simulation.CommunityAdmissions.cs`)

- **Geliş:** grup kapanışta üretilir, yeni vardiyanın başında kapıdadır; aralık `admissionKnockMin`–`Max` (3–4) vardiya, grubun
  geldiği kapanıştan sayılır. 2–3 aday (`candidateGroupMin/Max`), `candidateWaitShifts` (2) vardiya bekler, sonra cezasız gider.
- **Karar:** en çok biri alınır (`ChooseCandidate`, `candidate_choose`) ya da grup geri çevrilir (`PassCandidates`,
  `candidate_pass`). Alınan kişi Boşta gelir. Seçim ve geri çevirme kilitli planda da geçer; baskın sürerken ve fiş basıldıktan
  sonra kapı kapalıdır. Bekleyen grup fiş basmayı engellemez; son vardiyada fiş "KAPI · son vardiya" yazar.
- **Dolu sığınak** (`boardResidentCapacity` 10): aday ancak içeriden biri yollanarak alınır (aynı komutta `b` = yollanan).
  **Yollama** her yerde aynı bedeldir: kişi, ustalığı, kişiye bağlı teçhizatı ve kalan taksitleri gider; `dismissSupplies` (2)
  erzak yolluk (stok azsa eldeki; `DismissSuppliesPreview`). Serbest yollama yoktur; canlı 1 iken yollama kapalıdır.
- **Acil grup:** kapanışta canlı < `candidateEmergencyBelow` (4; tek kip denge turu 2026-11-22, önce 3: tam ekip kaybı sığınağın tamamı olmasın) ve kapıda grup yoksa aynı kapanışta 3 kişilik acil grup gelir;
  konular sırayla eksik saha rollerine çevrilir, her aday kişiye bağlı rol kitiyle gelir. Kademe ve kimlik olağan kuralla.
- **Şüphe kademesi** (kartta görünür; sızmacı olasılığı yalnız kademeye bağlı):

  | Kademe | Pay | Sızmacı | Kartın değeri |
  |---|---|---|---|
  | 0 · Kefilli | %30 (`candidateTier0Share`) | %5 | Bir özellik: %35 bedel (`candidateTier0CostChance`), %65 getiri |
  | 1 · Kayıtsız | %40 | %20 | Bir getiri |
  | 2 · Şüpheli | %30 (`candidateTier2Share`) | %45 | İki getiri |

  Bütün adaylar aynı kademeden çıkarsa sonuncusu bir kaydırılır. Kefil adı yaşayan bir kurucudan; kefil yalnız metindir.
- **Özellikler** (`Perks`): getiri `kit` (rol kiti, kişiye bağlı), `zirh` (kişiye bağlı), `devre` (2 kapanışta 1'er),
  `erzak` (3 kapanışta 2'şer), `usta` (ustalık kademe 2), `saglam` (+1 can); bedel `yarali` (can 1, 2 vardiya yatar), `obur`
  (iki kat tüketim), `acemi` (ustalık 0), `yavas` (hareket −1; etkisizse `obur`a döner). Kişiye bağlı teçhizat devredilemez,
  kişiyle gider; taksitler kişi çıkınca kesilir ("al, getirisini depoya koy, yolla" yolu kapalı).

### Sığınak olayları (`Simulation.ShelterEvents.cs`, `ShelterEventData.cs`, `ShelterEventState.cs`)

- **Tetik:** her kapanışta durum koşulları sınanır; aynı anda en çok bir olay açıktır. Olağan olaylar açılıştan açılışa
  `eventGapMin`–`eventGapMax` (2–3) vardiya bekler (açılışta iki çekim: aralık ve hedef). Zincir olayları (`iz`,
  `ikinci_iz`, `sabotaj`) aralık bütçesinin dışındadır. Öncelik: zincir, hayatta kalma, kişiler, fırsatlar. Her olağan olayın
  harekât başına sınırı ve bekleme süresi düz `Balance` alanıdır (`event<Ad>Max/Cooldown`).
- **Karar:** `DecideShelterEvent(seçenek)` (`event_decide`). Açık olay karar verilmeden fiş basılmaz (`PrintShift`'e özgü; baskın
  emrini engellemez). Kâğıt her seçeneğin anlık bedelini ve gecikmeli riskin varlığını yazar (olasılığın sayısını yazmaz);
  karşılanamayan seçenek kapalı görünür ve nedenini söyler. Güveni düşüren seçenek güven 1'de, kişi çıkaran seçenek canlı 1'de
  kapalıdır; "güvenli" seçenek her durumda açıktır. Olayın kişisi karar verilmeden ölür ya da giderse olay kapanır.
- **On dört olay** (güvenli seçenek kalın):

  | Olay | Tetik | Seçenekler |
  |---|---|---|
  | `iz` | Bir kişi iz bıraktı | Tecrit (3 vardiya, yer 1) · Yolla · **Görmezden gel** (ifşa +1) |
  | `ikinci_iz` | Aynı kişiden ikinci iz | `iz` ile aynı |
  | `sabotaj` | Sızmacı sabotajı tamamladı (3 erzak ya da 2 metal gitti, ifşa +2) | **Sök** · Sorgula (3 vardiya yer tutmadan, sonra sökülür; ifşa −1) |
  | `buyuk_hirsizlik` | Affedilmiş hırsız yeniden çaldı (en çok 5 erzak) | Yolla · **Kısıtla** (5 vardiya) |
  | `kacak` | Açlık sayacı 2; biri gitmek istiyor | **İkna et** · Bırak · Kapıyı kilitle (3 vardiya kısıt, güven −1) |
  | `erzak_azaliyor` | Erzak 3 vardiyalık tüketimin altında, yarım tayın yok | Yarım tayın (4 vardiya; kimse iyileşmez, baskın kadrosu 1 can eksik) · **Tam tayın** |
  | `hirsizlik` | Yarım tayın ya da erzak düşük, stok ≥ 2; 2 erzak çalındı | Yolla · **Affet** (%35 olasılıkla 3–5 vardiya sonra büyük hırsızlık) · Kısıtla (3 vardiya) |
  | `agir_yarali` | Biri bu kapanışta yatışa girdi, kalan ≥ 2 | Erzakla ayağa kaldır (3 erzak, 2 can) · **Beklet** (%25 yatış +1) |
  | `kavga` | Başarısız ya da ölümlü baskından dönen iki kişi | Ayır (2 vardiya aynı kadroya giremez) · **Karışma** (%50 biri 1 can, 1 vardiya yatar) |
  | `anma` | Önceki vardiyada baskında ölüm | Anma yap (2 erzak, güven +1) · **İşe devam** (%40 bir arkadaşı 1 vardiya çalışmaz) |
  | `karartma` | Alarm eşiğin 2 altında | Karart (Üretim ve Mutfak durur, alarm −2) · **Çalışmaya devam** |
  | `takas` | Erzak < 8 ve metal ≥ 2; ya da saha kiti eksik ve erzak > 12 | Metal ver (2 metal → 5 erzak) · Erzak ver (4 erzak → 1 devre) · **Geri çevir** |
  | `cagri` | Başarılı baskından sonra, erzak > 12 | Erzak yolla (4 erzak, güven +2) · **Yollama** (%50 güven −1, 1'in altına inmez) |
  | `konvoy` | Güven ≥ 15 | Kabul et (+5 erzak, alarm +2) · **Geri çevir** |

- **Kişi durumları:** kısıtlı (Saha, Üretim, Mutfak kapalı), tecritte (hiçbir yuva; saat durur), izli, affedilmiş, ayrı
  tutulan çift. Tecrit yeri `isolationCapacity` (1); ortaya çıkmış sabotajcı yer tutmaz, sorgu yer istemez.
- **Sızma zinciri:** kişinin sığınakta, ayakta ve tecrit dışında geçirdiği her kapanış saati ilerletir (görev fark etmez).
  Saat üretimde çekilen gecikmeye (`traceDelayMin`–`Max`, 2–6) varınca `iz`; görmezden gelinirse `secondTraceGap` (1–3) sonra
  `ikinci_iz`; o da görmezden gelinirse bir müdahale vardiyası (`interventionShifts` 1), sonra `sabotaj`. **İnsan da iz
  bırakır:** sonradan alınan insan `humanTraceChance` (%20) olasılıkla aynı gecikme dağılımından iz, bunların %25'i ikinci iz
  bırakır; insan sabotaj yapmaz. Kural: bir iz şüphe, iki iz güçlü kanıt, sabotaj kesindir.
- **Tecrit:** 3 vardiya; süre dolunca tek çekim: sızmacıysa bir izde %60, iki izde %85 bulgu ve söküm (`bulgu`), değilse "bulgu
  yok" (`bulgu_yok`; kimseyi temize çıkarmaz, izler dosyada kalır).
- **Açlık ve tayın:** aç kapanışta stok 0'a kırpılır, `HungryStreak` 1 artar (tok kapanışta 1 azalır). Sayaç `desertStreak` (3)
  ve üstündeyken her aç kapanışta biri ayrılır (RNG'siz sıra: önce sonradan gelenler, en son katılandan; kit sahibi saha üyesi en
  sona; yatan ve o vardiya gelen ayrılmaz; son kişi gitmez). Aç kapanışta da eksik rol kiti üretilir, hiç olmayan rol kiti devresiz
  üretilir (`boardStarvingKitProduction`, `boardMissingKitCircuits` 0).

### İfşa (`Shelter.Exposure`, `Simulation.Endings.cs`)

İfşayı yalnız iki şey artırır: görmezden gelinen iz `exposureIgnoredTrace` (+1; sahibi kim olursa olsun) ve tamamlanan sabotaj
`exposureSabotage` (+2). Azaltan tek şey ortaya çıkmış sabotajcının sorgusudur `exposureInterrogation` (−1). Kendiliğinden
azalmaz, tavanı yoktur (gösterge kırpar). Teleks `exposureWarnAt1/2` (2, 4)'te uyarır; `exposureAlarmAt` (3) ve üstünde YZ alarmı
vardiyada `exposureAlarmBonus` (+0,5) fazla artar. Eşik `exposureThreshold` (6): eşiğe varılan kapanışta "KONUM ÇÖZÜLDÜ"
(`Shelter.FallPending`); sabotajla varıldıysa bir, vardiya içindeki bir kararla varıldıysa `exposureDecisionWarnShifts` (2)
vardiya uyarı (`Shelter.FallGrace`, okuyucu `FallShiftsLeft`). Uyarı süresinde zafer alınabilir, sorgu ifşayı eşiğin altına
indirirse uyarı hemen kalkar, son şalter çekilebilir; süre sonunda hâlâ eşikteyse `dusus`. Sunum: odadaki altı lambalı İFŞA
göstergesi (`ExposurePanel`), CRT, teleks ve fiş.

### Sonlar (`Simulation.Endings.cs`, `EndReport.cs`, `GameData.Endings`)

| Son | Kimlik | Koşul | Sonuç |
|---|---|---|---|
| Silme | `silme` | Bütün 36 sektör geri alındı | Zafer; tonu aşağıda |
| Dağılma | `dagilma` | Sivil güven 0 | Yenilgi |
| Son kişi | `topluluk` | Canlı kimse kalmadı | Yenilgi |
| Sığınak düştü | `dusus` | İfşa uyarı süresinin sonunda hâlâ eşikte | Yenilgi |
| Terk | `terk` | Son tam kadrodan beri (`DesertedSinceFull`) açlıktan ayrılma var ve canlı < `candidateEmergencyBelow` (4; acil grupla aynı eşik, tek kip denge turundan önce 3); kapıda acil grup beklemişken üst üste `abandonUnderstaffedShifts` (3) kapanış (`UnderstaffedStreak`; fişte "SIĞINAK EKSİK n/3") | Yenilgi. Yalnız ölümlerle eksilen sığınak bu sonla bitmez |
| Son şalter | `salter` | Oyuncu en erken `lastSwitchMinShift` (5) vardiyada şalteri çeker ve ÇEKİLME EMRİ'ni damgalar (`PullLastSwitch`, `last_switch`; rehber sürerken mühürlü) | **Düzenli çekilme** yenilgi sayılmaz; **tahliye** (nedenler `konum` uyarı açık, `kadro` eksik kadro sayacı işliyor, `aclik` aç sığınaktan ayrılmalar sürüyor, `guven` < `evacuationTrustBelow` 2) yenilgidir |

Öncelik aynı kapanışta `topluluk`, `dusus`, `terk`, sonra alarm adımının `dagilma`'sı; sonlar olay açılışından önce sınanır.
**Zaferin tonu** (`VictoryTone`, yalnız son raporda): **gölgeli** içeride en az `victoryShadowIgnoredTraces` (1) izi görmezden
gelinmiş, ortaya çıkmamış canlı sızmacı varsa ("Son sunucu düştü. İçeride {ad} hâlâ dinliyordu."); **temiz** gölgeli değilse ve
ölüm ≤ `victoryCleanMaxDeaths` (0) + vardiya ÷ `victoryCleanDeathShifts` (15) (30–44. vardiyada 2, 45–59'da 3), açlıktan ayrılan
yok, ifşa ≤ `victoryCleanMaxExposure` (2) ve güven hiç `victoryCleanMinTrust` (5)'in altına inmemişse; öteki zaferler **bedelli**.
`EndReport` son yokken `null`; son rapor (`FinalReportBuilder`, `SiginakFinal`) başlık, damga, ölçüt satırları, kapı bölümü
(alınan, yollanan ve kaçı insan, geri çevrilen ve kaçı sızmacı) ve sonun kendi satırlarını yazar. Başarımlar aşağıda.

### Denge ölçümü (KOS aşama 8, 2026-11-14/15)

_Tarihsel: bu ölçüm tek tahta dönemindendir (baskınlar tek 8×8 tahtada, görev ek turlarıyla). Kayıt 21'den beri baskın tesis
olarak oynanır; `asama8.txt` "emekli alan"la durur ve sayılar bugünkü oyunu ölçmez._

Bot düzeneği `Tools/DengeBotu/` (gerçek Core ve gerçek `RaidBot`, 8 seçenek; olaylar açık, 120 vardiya sınırı; README'de
politikalar). İlk ölçüm (40 tohum, beş kapı × dört olay politikası ve şalter satırları, 960 harekât) yedi alanlık bir set önerdi,
ama bot yayındaki panoda bulunmayan İkmal (`gather`, kademe 1'de +1,4 erzak) ve Bakım (`care`) görevlerini kullanıyordu; sunum bu
görevleri Boşta'ya indirir. Bütün sonuçlar **sunumun kuralıyla** (`RETIRE=1`) yeniden ölçüldü: tüketim 0,575 yerine **0,40**
(toplayıcılı ölçümün 3,9 − 1,4 = 2,5 net tüketimiyle aynı) ve olay aralığı 2–2 yerine **2–3** (değişmedi). Son ölçüm 80 tohum.

"İyi" = karışık+tecrit, karışık+ucuz, kefilli+ucuz (240 harekât). Taban = aynı kuralla eski değerler (40 tohum, 120 harekât).

| Ölçü | Hedef | Taban (sunum kuralı) | **Uygulanan (80 tohum)** |
|---|---|---|---|
| Zafer (iyi) | — | %35 | **%64** |
| dagilma (iyi) | belirgin düşsün | %62 | **%28** |
| terk (iyi) | < %10 | %0 | **%5** |
| `erzak_azaliyor` görülen (iyi) | ≥ %30 | %0 | **%53** |
| Zafer uzunluğu (iyi) | 35–45 | ort 48,2 / med 48 | **ort 48,3 / med 48** (tutmuyor) |
| Olay / harekât | 12–15 | 11,9 | **14,7** (iyi), 14,6 (6 yapılandırma) |
| En sık olay türü | ≤ %35 | agir_yarali %18 | **agir_yarali %15** |
| Kapı farkı (en iyi − kefilli; ucuz) | ≤ 15 puan | 8 | **5** (karışık %69, kefilli %64) |
| "Hiç alma" | belirgin kötü | %20 | **%34** (iyi %59–69) |
| Cömert + görmezden gel `dusus` | ≥ %40 | %62 | **%61** |
| Karışık + tecrit `dusus`; ucuzdan iyi değil | < %15 | %0; %38 = %38 | **%0; %59 < %69** |
| Temiz ton (zaferler) | %10–30 | %7 | **%8** (iyi), %7 (6 yapılandırma) (tutmuyor, bot) |
| Gölgeli ton (zaferler) | %5–15 | %0 | **%5** (iyi), %6 (6 yapılandırma) |
| 60. vardiyada süren (iyi) | 0 | 5/120 | **8/240** (tutmuyor) |
| "Hep yolla"da yollanan insan payı | çoğunluk | — | **kapıya bağlı:** karışık %82, cömert %33 |
| "Al ve yolla" | hiçbirini geçmez | — | **%76** > en iyi %69–71 (tutmuyor) |

Değişen altı alan: `liberationServerExtraTurns` 1 → 2 (kayıt 21'de alan kaldırıldı), `liberationTier3At` 6 → 7, `liberationTier4At` 10 → 12 (12 bölgelik
harekâtta 4. kademe gelmez; göstergeler `Simulation.MaxTier`'i okur: "TEHDİT n/3"), `liberationTrustPerRegion` 3 → 4,
`boardSuppliesPerResident` 0,35 → 0,40, `boardLootSupplies` 6 → 4. `eventGapMax` 3'te kaldı (2 denendi: olay/harekât 16,6, bant
dışı). Tüketimin eğimi diktir: 0,375 / 0,40 / 0,425'te `erzak_azaliyor` %28 / %47 / %62 (40 tohum); obur ya da onuncu kişi
açlığı belirgin kaydırır. dagilma'nın son darbesi çoğunlukla baskında kırılan sivillerdir (%72); dağılmaların %41'i ilk 20
vardiyada, %49'u 35. vardiyadan sonra (ölümle süren yıpranma). Başlangıç güveni 12 bunu değiştirmedi (%29).

Bot insan değildir (baskın başına ~1,7 sivil, harekât başına ~6,4 ölü, yarım tayın ve takas yok). Seçeneği iki katına çıkan
bot (`ALT=16`, karışık+ucuz, 40 tohum) zafer %65 → %80, dagilma %25 → %15, uzunluk medyanı 48,5 → 43,5, temiz ton %8 → %13:
uzunluk ve temiz ton hedefleri daha iyi baskın oyunuyla tutuyor. Sapmaların yorumu ve açık kararlar `DURUM.md`'de.

### Kodda yerleri

| Konu | Core | Sunum |
|---|---|---|
| Aday grubu | `Simulation.CandidateGroups.cs` (`ChooseCandidate`, `PassCandidates`, `CandidateChoiceCheck`), `CommunityAdmissionState.cs` (`Board.Admission`) | `SiginakAdmission`, `AdmissionSnapshotBuilder`, `AdmissionUi` (kabul tezgâhı, KABUL / GERİ ÇEVİR) |
| Kapı aralığı, kurucu dosyaları, grubun süresi | `Simulation.CommunityAdmissions.cs` | Teleks, fiş uyarısı (`HandoverSlipFactsBuilder`) |
| Olaylar, zincir, tecrit, kısıt, tayın | `Simulation.ShelterEvents.cs`, `ShelterEventData.cs` (tablolar, `SafeOption`), `ShelterEventState.cs` (`Board.ShelterEvents`) | `SiginakShelterEvent`, `ShelterEventSnapshotBuilder`, `ShelterEventPaperView` (kapı panosuna gelen, bırakılınca EVRAK tepsisine inen tutanak; `Docs/KAPI-BELGELERI-VE-YZ-MEKANLARI-TASARIMI.md` §A) |
| Açlık, ayrılma, erzak dengesi | `Simulation.PlanningBoard.cs` (`AdvancePlanningBoard`, `BoardConsumption`, `BoardFoodPreview`) | Pano başlığı, fiş (ERZAK satırı, ayrılacak kişi) |
| İfşa ve sonlar | `Simulation.Endings.cs`, `ShelterState.cs` (`Exposure`, `FallPending`, `FallGrace`, `HungryStreak`, `UnderstaffedStreak`), `EndReport.cs`, `GameData.Endings` | `ExposurePanel`, `SiginakLastSwitch` + `LastSwitchViewBuilder` + `LastSwitchPlate`, `SiginakFinal` + `FinalReportBuilder` |
| Başarımlar | — | `Game/Platform.cs › Achievements.ForEnding` |
| Sayılar | `Balance.cs`, `Settings/BalanceConfig.asset` | — |

- **Olaylar kapalıyken** (`Balance.shelterEventsEnabled = false`; yalnız testlerin olaysız kapanış dizileri için): olay,
  sızma zinciri, tecrit, kısıt, tayın kipi, karartma, açlıktan ayrılma, ifşa, `dusus`/`terk` ve son şalterin ifşa/eksik
  nedenleri çalışmaz; aday grubu kapısı (seçim, geri çevirme, dolu sığınakta yollama, acil grup) çalışır; `HungryStreak`
  sayılır; sığınağın kendi sonu yalnız `topluluk`, son şalter (`salter`) olaylardan bağımsız çalışır ve tahliye nedeni yalnız güven
  (`guven`) olabilir. Kısıtı ya da kapı dışı yollamayı kuran elle bir yol yoktur: kapalı oyunda kimse
  kısıtlı olmaz.

**Geçmiş (KOS aşama 7'de söküldü).** Önceki tek adaylı kabul: belge, görüşme, soğukta nefes (buğu camı) ve dış kayıt
incelemeleri (`InspectApplicant`), dört karar (kabul, kısıtlı kabul, ret, dış yardım; `DecideApplicant`), talimat sürümleri
ve arşivi, gözetmenli dosya incelemesi (`ReviewResident`), elle kısıt (`RestrictResident`) ve serbest yollama
(`DismissResident`), tek gözetim yuvası (`ObservationTarget`) ve `watch`/`contact` görevlerinin kabul bağları. Komutları,
Core durumları, denge alanları, sunumu (büyüteç, talep defteri, kalem, KISITLI/YARDIM damgaları, buğu camı ve interkom
etkileşimi, talimat şeridi, bulgu fişleri, kişi dosyasının İNCELE/ERİŞİM/TAHLİYE sekmeleri), sesleri ve çevirileri
silindi. Kayıt biçimi 17'de kaldı (bkz. Kayıt ve kalıcılık).

## Eski oda geliştirmeli sığınak (yalnız `planningBoardEnabled = false`)

Aşağıdaki kurallar ve tarihli ölçümler eski yolu belgelemek için korunur. Varsayılan kartlı oyunda çalışmaz. Bu yolun odadaki sunumu (raf, maket) Aşama 10'dan beri sahnede yok; kurallar Core'da ve testlerde durur.
`ShelterState`, `ShelterLogic`, `Simulation.Shelter.cs`:

Sığınak baskının hazırlık yeridir: ürettiği her şey bir sonraki baskına girer. Beklemenin karşılığı hazırlık (dinlenme,
plaka, revir), bedeli alarm; vardiya şalteri böylece karara döner. 6 kişi, 24 erzak ve 14 malzemeyle başlar. Yaşam alanı
ve telsiz odası seviye 1; atölye ve revir onarılmayı bekler. **Seviyeler özgür bölgeyle açılır**
(`Simulation.ShelterLevelLock`): seviye 2 için 2, seviye 3 için 5 özgür bölge (`shelterLevel2Regions`, `shelterLevel3Regions`);
kilitliyken panoda düğme KİLİTLİ, hazırlık satırında gerekçe. Odalar en fazla seviye 3 olur; onarım/geliştirme
(`ShelterUpgrade`) fon ve malzeme ister, görevlendirme (`ShelterAssign`) değiştirilene kadar sürer. Oda başına personel
kapasitesi `2 × seviye`dir. Enerji yoktur.

| Oda | Rol | Onarım/geliştirme |
|---|---|---|
| Yaşam alanı | Seviye başına 8 kişilik yer, 30 erzak deposu ve **1 yedek uzman**; görevli başına vardiyada 0,7 erzak toplama | 8 fon + 6 malzeme |
| Telsiz odası | Görevli başına vardiya alarmı −0,05 | 6 fon + 4 malzeme |
| Atölye | Görevli başına vardiyada 0,25 **zırh plakası** (plaka 4 malzeme); depo seviye başına 1 plaka | 10 fon + 6 malzeme |
| Revir | Görevli başına vardiyada 0,34 **yara** sarar (en yaralıdan); en az 1 görevliyle yatan uzman 1 gün erken kalkar | 12 fon + 8 malzeme |

Her kişi vardiyada 0,35 erzak tüketir. Erzak biterse atölye yarı hızda döker ve revir durur; yaşam alanında toplama sürer.
Atölye artık malzeme üretmez: malzeme baskınlardan gelir (ikmal +7, sunucu +5) ve odalarla plakalar arasında paylaşılır;
depo 60.

**Kıtlık** (2026-10-28): bir toplayıcı iki kişiyi doyurur, yaşam alanı ise nüfusun yarısı kadar yer tutar (seviye başına 8 kişi,
2 görevli). Nüfus büyüdükçe toplama tüketimi karşılamaz; açığı ikmal baskınlarının erzağı kapatır. Başarısız ikmal serisi
açlık getirir: erzak nüfusun altına inince kapının açlık sayacı işler, iki aç vardiyadan sonra aç sakinlerin heyeti gelir
(geri çevrilirse silahlı sakin). Vardiya raporunun analist notu toplayıcılar yetmezken erzak 6 vardiya içinde bitecekse
"→ erzak N vardiyaya yeter, ikmale çık" yazar. Bot ölçümü (30 harekât, orta oyuncu, kapıya en iyi cevap; önce → sonra):
erzak ortalama deponun %95'i → %52'si; aç vardiya 0 → 13 (vardiyaların ~%20'si, harekâta göre 0–33); erzağın tükendiği
vardiya 0 → 2,7 (0–10); heyet 0 → 0,8; malzeme ortalama 44 → 15 (depo hiç dolmuyor); fon ortalama 35 → 26; kazanılan
21/30 → 19/30. Odalar yine tamamlanıyor (ortalama 45. gün), ama malzeme plakayla odalar arasında paylaşılıyor.
Plaka ve yara birikimi `PlateProgress`, `HealProgress`'te vardiya vardiya dolar. Bütün sayılar `Balance.shelter*`
alanlarındadır.

**Denge ölçümü** (2026-10-27, açgözlü bot, 10'ar harekât; sığınağı kurup personeli dağıtan bot, saldırgan / dengeli /
koruyucu): tam sığınakla 0/10, 8/10, 8/10 zafer; sığınağı hiç kullanmayan 0/10, 1/10, 1/10. Tek oda eksikken (dengeli,
koruyucu) revirsiz 2/10, atölyesiz 3–4/10, telsizsiz 6/10; revirsiz ama yaralıyken dinlenen 9/10 ve 8/10 (ikinci yol).
Harekât başına ~2 karşı saldırı, fon 1–5 vardiya baskını bekletiyor.

**Zorluk kademesi ve bölge kimliğiyle son ölçüm** (2026-10-27, 10'ar harekât; saldırgan / dengeli / koruyucu): tam
sığınakla 1/10, 5/10, 8/10 zafer; yaralıyken dinlenen 1/10, 9/10, 8/10; sığınaksız 0/10, 1/10, 1/10. Kademe başına baskın
başarısı (açgözlü bot) ikmalde %98 → %95 → %77 → %59, sunucuda %95 → %87 → %66 → %50; çekirdek %50. İlk denemede
(kademe 3: 5, kademe 4: 9 bölge, bölge güveni +1) tam sığınaklı dengeli bot 2/10 kazanıyordu; eşikler 6 ve 10'a, bölge
güveni +2'ye çekildi. Önceki sığınakta (enerji, malzeme üreten atölye)
dört plan da aynı harekâtı oynuyordu: üç oda sonuca hiç dokunmuyordu, fon 450–770 artıkla bitiyordu.

## Eski ziyaretçi kapısı (yalnız `planningBoardEnabled = false`)

`Core/Door.cs`, `Core/DoorCards.cs`; aşağıdaki ziyaretçi kuralları yeni kabul sisteminde çalışmaz.

Birkaç vardiyada bir biri sığınağın kapısını çalar: erzak isteyen bir anne, kurtarılan bölgeden bir kurye, bir kaçakçı, YZ'den
kaçmış bir teknisyen… Kapı erzağa, güvene ve alarma bir yüz verir; baskının sonucu da kapıya döner (hastanesi kararan baba).

- **Zaman:** ilk vuruş 2. günün sonunda (`doorFirstKnock`; eğitimden sonra), senaryolu ve dostça: Nermin bir kese erzak
  getirir ve kapıyı öğretir ("Önce bak, sonra aç."). Sonra her 2–4 günde bir (`doorKnockMin/Max`). Kapının kendi tohumu
  vardır (harekât tohumu + gün + amaç, `DoorRng`); ana rastgelelik sırasına (`S.Rng`) dokunmaz.
- **Kapalı kapı:** kâğıt kapıdan duyulanı yazar (vuruşun ritmi, ses). **Gözetleme deliği** görüneni, **interkom** parolaya
  ("Yedi") verilen cevabı ekler; ikisi de sunumdur, durum değiştirmez. Sonra **Kapıyı aç** ya da **Açma**; silahlı
  ziyaretçiyle kapı kapalıyken interkomdan konuşulur.
- **Açık kapı:** ziyaretçi söyler, seçenekler çıkar. Bedeller (erzak, malzeme, fon) ve güvene etkisi satırın sağında
  yazar ("−8 erzak", "güven −1"). Oda çalışmıyorsa (revir, telsiz) seçenek kapalıdır, nedeni sağda; bedel yetmiyorsa satır
  açık kalır, eksik kalem çizilir ve basınca komut denenir (teleks YAPILAMADI basar). Sonuç kâğıtta seçim satırı, metin ve
  etki satırı olarak görünür ("Erzak +6 · Güven +1"). Her karar `door_answer` komutudur (kayda girer).
- **Açılmayan kapı:** ziyaretçi kapıdayken vardiyayı bitirmek **Açma** sayılır (ön kontrol uyarır; teleks ve vardiya raporu
  "KAPI AÇILMADI" yazar). Muhtaç ziyaretçiyi geri çevirmek güvene dokunur (anne, aile −1; dışarıda kalan −2; aç heyet −2).
- **Kart seçimi** (`PickDoorCard`): ilk vuruş; sonra zamanı gelen zincir kartı; yoksa koşulu tutan kartlardan ağırlıkla
  biri. Son 4 ziyaretteki kart gelmez (`doorRepeatGap`), tek seferlikler bir kez gelir. Uygun kart yoksa kapı o gün çalmaz.
- **Zincirler:** anneye yardım edersen oğlu birkaç gün sonra gelir; kaçakçıyı ihbar edersen kapına paket bırakılır;
  sığınağa alınan teknisyen ya sadık çıkar (plaka) ya da YZ'ye yayın yapar (alarm); geri çevrilen aç heyet silahla döner.
- **Etkiler** kartın içeriğidir (birim türlerinin canı gibi, `DoorCards.All`): erzak, malzeme, fon, güven, YZ alarmı, sakin
  (gelen ya da ayrılan; görevli fazlası arka odalardan düşer, `ShelterLogic.Leave`), zırh plakası (depo doluysa malzeme),
  uzmanların yarası (hekim sarar; heyete ekibin payını vermek yara açar). Güven çoğunlukla kıt şeylerle (fon, malzeme)
  kazanılır; erzakla yapılan yardım güven kaybını önler. Vuruş sıklığı, güvenli günler, açlık eşiği `Balance.door*`'dadır.

- **Denge ölçümü** (2026-10-28, bot, orta oyuncu, 30'ar harekât, güven tavanı 20): kapısız 20/30 zafer; kapıya en iyi cevap
  21/30 (+4,2 güven, çoğu tavana çarpar); kapıyı hiç açmamak 17/30 (−8,2 güven). Harekât başına ~24 ziyaret. Tavan 40 iken
  27/30, 30/30 ve 24/30 idi: iyi oynanan kapı oyunu kolaylaştırıyordu; 20'de kapı kazandırmaz, ihmal edilirse kaybettirir.

**Ölüm kuralları.** Kapıyı açmak iki ziyaretçide harekâtı bitirir; ikisi de haksız olmasın diye üç kurala bağlıdır:

1. **Bir duruma bağlı:** **kopya** ("Dışarıda kalan biri") yalnız kopyalar konusunda uyarıldıysan (yaralı gözcünün ya da
   radyo amatörünün kapısını açtıysan) ve YZ alarmı eşiğin %60'ına vardıysa ya da son 4 günde karşı saldırı olduysa gelir
   (`doorCopyAlarm`, `doorCopyAfterCounterattack`). **Silahlı sakin** yalnız aç sakinlerin heyetini geri çevirdiysen ve
   açlık sürüyorsa gelir (erzak nüfustan azsa vardiya aç sayılır, `doorHungerSupplies`; heyet iki aç vardiyadan sonra).
   **Siyah bantlı baba** yalnız sivil hasarlı bir baskından sonra ona sessiz kaldıysan ve güven 10 ya da altındaysa döner.
2. **Önceden haber verir:** uyarı kopyanın üç işaretini söyler: saat gibi vurur, camı buğulamaz, parolayı bilmez. Aynı
   başlıkla gerçek bir insan da gelir: telaşla vurur, nefesi camı buğular, parolayı bilir (onu kapıda bırakmak −2 güven).
   Silahlının namlusu gözetleme deliğinden görünür.
3. **Her zaman bir çıkış vardır:** Açma. Silahlıyla interkomdan konuşmak: erzak 8 ve üstüyse erzağı alıp gider, güven 20
   ve üstüyse ikna olur, yoksa kapıyı zorlar (malzeme −8, güven −2); kimse ölmez. İlk 8 gün (`doorSafeShifts`) ölümcül
   ziyaretçi gelmez.

| Ziyaretçi | Ne zaman | Seçenekler |
|---|---|---|
| Nermin (ilk vuruş) | 2. gün | Keseyi al (erzak +6) · Sakinlere geri ver (güven +1) |
| Alt kattan bir anne | Her zaman | Erzak ver (−8) · Revire götür (revirde görevli) · "İlaç yok" de (güven −1) |
| İyileşen çocuğun ağabeyi | Anneye yardımdan 4–7 gün sonra | Yaşam alanına al (sakin +1, güven +1) · Hurda toplasın (malzeme +8) |
| Özgür bölgeden bir kurye | Özgür bölge başına bir kez | Depoya al (erzak +6, malzeme +6) · Sakinlere dağıt (güven +1) |
| Bir kaçakçı | Fon 30 ve üstü | Fonla erzak al (−10 fon, +12 erzak) · Reddet · İhbar et (güven +1, sonra paket) |
| Kapının önünde bir paket | İhbardan 3–5 gün sonra | Açarsan patlar (malzeme −10, güven −1); açmazsan koridorda (malzeme −4) |
| YZ'den kaçmış bir teknisyen | 10. günden sonra, bir kez | Sığınağa al (sakin +1, malzeme +4; sonra sadık ya da casus) · Geri çevir |
| Yaralı bir gözcü | 5. günden sonra, alarm 5+ ya da karşı saldırı | Kopya uyarısı; Revire al · Sargı ve erzak ver |
| Dışarıda kalan biri (insan) | 6. günden sonra | Yaşam alanına götür (sakin +1); açmazsan güven −2 |
| Dışarıda kalan biri (kopya) | Yukarıda | Açarsan **Sızma** |
| Aç sakinlerin heyeti | İki aç vardiyadan sonra | Fonla erzak bul (−8 fon) · Ekibin payını dağıt (uzmanlara yara) · "Bekleyin" de (güven −1, silahlı döner) |
| Silahlı bir sakin | Heyetten 2–3 gün sonra, açlık sürüyorsa | İnterkomdan konuş · Açarsan **Vuruldu** |
| Kolunda siyah bant olan bir baba | 2+ sivil kırılan baskından sonra | Özür dile, erzak ver (güven +1) · Sessiz kal (güven −1; güven düşükse silahla döner) |
| Gezgin bir hekim | Yaralı uzman varsa | Uzmanlara baktır (−6 erzak, yaralar sarılır) · Sakinlere ilaç al (−5 fon, güven +1) |
| Çocuklar, radyo amatörü, aile, zarf, öğretmen, hırsız | Özgür bölge, telsiz ve alarm, yer, alarm, nüfus, erzak | Resim (güven +1) · Frekans çizelgesi (malzeme → alarm −3; kopya uyarısı) · Aileyi al (+3 sakin) · YZ'nin zarfı · Okul (malzeme → güven +2) · Hırsızı affet ya da çıkar |

## Komuta odası (sunum)

- **Masa (K1):** sayaç bankası (VARDİYA, FON, ERZAK, HAMMADDE, ÖZGÜR BÖLGE, GÜVEN), anahtar rayı (SIĞINAK, KAPI,
  MASA; tuş başlığında istasyonun simgesi: yer altındaki odalar, çarklı kapı, lambalı masa), vardiya şalteri,
  teleks, evrak tepsisi (harekât emri de orada). Oda klavye tuşu yazmaz: kısayollar (2, 3, 4/K, Home, F, Enter, Esc, Boşluk)
  çalışır ama ekranda ipucu yok; her birinin tıklanan bir karşılığı var (kapı kâğıdında “masaya dön” satırı). Arka duvarda, mantar pano ile posterin
  arasında YZ ALARMI panosu.
- **Harita (K2, K3; Revizyon 2 · S2):** masadaki haritaya tıklayınca aktif cephenin görev fişi açılır. Yakın görünümde bölge seçilince bölgenin fişi açılır: ekran katmanında sol masa kenarında (Kuzey ya
  da Güney Amerika seçiliyse sağda), kırmızı iple bölgeye bağlı. SAHA satırları (bölge özelliği, imza makinesi, tehdit
  kademesi; işaretleri haritadaki ve baskın tahtasındakilerle aynı), sektörler, bedel/ödül, ekip diskleri, baskın emri.
  Bölge tamamlanınca cephe onay fişi. Haritada cephe bloğu (sektör şeridi + yüzde + saha işaretleri), işaretler kutusunda
  TEHDİT n/m (m = `Simulation.MaxTier`, uygulanan dengede 3); karşı saldırının aldığı sektör geri alınana kadar ✕ ve YENİDEN İŞGAL damgasıyla işaretli. Ekip figürü
  haritada sürüklenebilir.
- **Tarif defteri / Boşta:** tarif şeridi yoktur. Deftere tıklanınca öne açılır; önceki/sonrakiyle üç açılımda altı tarif gezilir, tarif üretime sürüklenir. Boşta tıklanınca tüm kartlar alt sıraya yayılır; bir kişi sürüklenirken diğerleri geri toplanır.
- **Güncel tahta:** kişi kartları 210×294 dikeydir; aktif yuvalar Teknisyen, EMP, Analist, Üretim, Mutfak, Eğitim ve Boşta. Bakım/İkmal/Gözetim/Dış temas kaldırıldı; eski kayıttaki bu görevler yeni kilitsiz planda kayıtlı atama komutlarıyla Boşta olur. Aşağıdaki eski alanlı yerleşim açıklaması tasarım geçmişidir.
- **Kartlı planlama panosu (K4):** eski sığınak rafının yerinde (raf ve maket Revizyon 3 Aşama 10'da sahneden kaldırıldı) daha küçük, ayaklı ve sahnede
  kaydedilmiş pano. Topluluk sicil listesi yoktur. Boşta ve her görev için başlığı ve sınırı belli bırakma alanı vardır;
  kişi kartını alana bırakmak atamayı yapar ve kart görev alanında görünür. Gezici görev etiketleri yoktur. Teçhizat
  kartı kişinin üzerine konur; bağlı eşya kişi taşınırken onunla gider, **Depo** alanına bırakılınca kişiden çıkar.
  Depoda boştaki ve üretilen eşya kartları vardır. Tarif ve hammaddeler ayrı **Üretim** alanındaki uygun yerlere
  bırakılır. Ayrıntıda ad, meslek, beden değerleri, ustalık ve dosya durumu okunur. Teçhizatın kişi üstündeki
  görsel ofseti sahnedeki sabit yığın ölçülerinden hesaplanır; eski serbest yerleşim dosyası kullanılmaz. Görev ve sahiplik
  komut geçmişinden kurulur. Panoda vardiya düğmesi, fiş veya devir kutusu yoktur. Ayarlar dolabı korunur.
- **CRT (K5):** telsizin göstergesi (TELSİZ · KANAL 07): dinlemede, hat açılıyor, hat açık, hat kapandı. Yalnız baskın
  emri kamerayı ekrana yaklaştırır. Eksik/yaralı saha kadrosu veya kit için emir gerekçesi gösterilir;
  eski modelde teknisyen ve yedek kontrolü korunur.
- **Görev fişi** yeni panoda üç saha kişisini adları, gerçek can/hareket/güç değerleri ve teçhizatıyla gösterir.
  Kadro panoda belirlenir; eski beş uzman seçimi oda geliştirmeli yola aittir. BASKIN EMRİ VER önce kadro raporunu açar;
  emir rapordaki BASKINA ÇIK ile verilir. Eksik kit, yaralı kişi ve yetersiz fon gerekçeleri komuttan okunur.
- **Vardiya geçişi (K6 → K1):** masadaki kapaklı vardiya şalteri: kapak → kol (basma komutu) → teleks daktilosundan
  satır satır basılan fiş (kamera masaya döner; önce teleksin o anki satırı biter) → mevcut EVRAK kutusu → kararıp açılma. Fiş beklenirken kol inik, kapak açık, hazır lambası sönük kalır. Kâğıdın fiziksel bırakılması tamamlanmadan gün ilerlemez; panoya gidilmez. Kâğıt o anda bilinen
  görevleri, üretim durumunu (EĞİTİM satırı 2026-11-24'te kalktı), beklenen erzak değişimini ve açık dosya uyarılarını yazar; gelecek gizli olayı
  açıklamaz. EVRAK kutusunun eski rapor arşivi korunur; eski vardiya raporu dizisi oda geliştirmeli oyuna aittir.
- **Açılış (K7):** açılış dizisi ve emir belgesi; BAŞLA ile oda açılır. Kayıt varsa son rapor kısaca görünür.
- **Kartlı başlangıç rehberi (2026-11-09):** ilk yeni harekâtta emir belgesi kapandıktan sonra sağda rehber notu açılır.
  `PlanningTutorialFlow` 18 adımda pano → kişi → Mutfağa kart atama → saha kitleri → görev/kadro → gerçek baskında seçim,
  hareket, yetenek ve tur bitirme → kaçış/rapor → dinlenme → şalterden fiş basma → EVRAK'a dosyalama döngüsünü tanıtır.
  Bekleme adımları gerçek durum/komut geçmişinden ilerler; bilgi adımları SONRAKİ ile. Kartı doğrudan atayan veya rehberden
  önce sefere çıkan oyuncu gerideki adımlarda takılmaz. Rehber kural, kaynak ya da RNG değiştirmez; normal isimli kadro ve
  kampanya baskını kullanılır. EĞİTİMİ BİTİR kapatır; Esc panodan geri dönme görevini korur.
  `Corrigible.PlanningTutorialDone/Seed/Step` sunum tercihleri aynı harekâtın yarım kalan rehberini devam ettirir; rehberle
  başlamamış eski kayıtta kendiliğinden açılmaz. Açılış/DEVAM kararı verilmeden önce eski rehber ilerlemesi ezilmez.
  Editörde Corrigible › Eğitim › Eğitimi sıfırla, hem yeni hem eski rehberi sıfırlar. Üretim için kapanışta kısa
  yönlendirme vardır; ayrı uygulamalı üretim dersi yoktur.
  **KOS aşama 8 (2026-11-14):** rehber 26 adım; ilk vardiya dosyalanınca **hedef satırı** gelir (kazanma: bütün sektörler;
  kaybettirenler: güven 0'da dağılma, ifşa eşiğinde konum çözülmesi, açlıkla eksik kalan sığınağın terki; alarm eşiği oyunu
  bitirmez, YZ son sektörü geri alır ve güven 1 düşer; satır canlıdır: "HEDEF · sektör 0/36 · güven 10 · alarm 1/10 ·
  ifşa 0/6"). Sonra rehber oyunun kendisini bekler: EVRAK'a ilk olay tutanağı düşünce "tutanağı al" ve "satır seç, KARAR"
  (güvenli satır, fiş kilidi "olay bekliyor"), kapıya ilk grup gelince "KAPI → tezgâh" ve "kartı oku, damgala" (şüphe satırı,
  getirdiği, KABUL/GERİ ÇEVİR, dolu sığınakta yollama ve yolluk). Hangisi önce gelirse o anlatılır; ikisi yokken "vardiyayı
  sürdür" adımı hedef satırıyla bekler. İkisi de bir kez karara bağlanınca (ya da V.011'e konusuz girilirse) İFŞA ve izler
  (bir iz şüphe, iki iz güçlü kanıt; KONUM ÇÖZÜLDÜ: sorgu, son baskın ya da son şalter yetişmezse sığınak düşer), son şalter (mühürlü; ÇEKİLME / TAHLİYE) ve kapanış bilgisi gelir. Rehber
  sürerken son şalter "mühürlü · rehber"dir; İFŞA ve teleks metinleri de onu önermez. Konu bölümünde not sayacı "REHBER · KONU"
  yazar. Yeni ekran balonu yoktur; not adımı oyunun yüzeylerine (teleks, EVRAK, plaka,
  fiş, tezgâh) yönlendirir.
- **Adım adım rehber ve işaretçi (2026-11-24; geliştirici: "ilk baskın bitene kadar adım adım, şuraya bas şuraya bas, ilk defa olan
  her şeyi tanıtsın"):**
  - Her adımda sarı bir çerçeve hedefi gösterir, üstünde kısa etiket ("BURAYA BAS", "SÜRÜKLE: MUTFAK", "YÜRÜ", "KAPI", "KASA",
    "GEÇ"…). Çerçeve tıklamayı yutmaz; rehber notu hedefin üstüne düşerse not karşı köşeye kaçar.
  - Masa: sayaçlar, SIĞINAK tuşu, BOŞTA destesi, MUTFAK cebi, depo, GÖZETİM cebi ve paftalar, MASAYA DÖN, harita, BASKIN EMRİ VER ve
    BASKINA ÇIK.
  - İlk baskın tek adım değil, on bir adımdır: tahta (tur şeridi), makine niyeti, uzman seç, ilk hamle (fareyle gezilince yürüyüş
    yolu çizilir; önerilen kare işaretli), yetenek (isteğe bağlı), EMRİ İLET, yeşil geçit, herkes kapıda GEÇ, ikinci pafta, kasayı
    aç, dönüş. Baskın ilerledikçe geride kalan adımlar (bilgi adımları dahil) kendiliğinden geçer.
  - "İlk kez" notları: rehber sürerken baskında ilk kez görülen durumlar bir kez anlatılır (uzman düştü, son tur, makine uyanıyor,
    bölge olayı, yükü taşıyan). Görülenler PlayerPrefs'te tutulur.
  - İlk baskın bitene dek vardiya şalteri rehberle mühürlüdür ("rehber · önce baskın"); EĞİTİMİ BİTİR mührü kaldırır. Rehber yine
    komut yazmaz, kayda ve RNG'ye girmez. Adım artık kimlikle saklanır (adım eklenince dizin kaymaz).
- **Eski rehberli eğitim (kartlı oyunda kapalı):** oda geliştirmeli yeni harekâtta, eğitim daha önce bitirilmediyse ya da atlanmadıysa sağ altta
  analistin notu çıkar (`SiginakTutorial`, adımlar `TutorialFlow.Steps`). 12 adım: komuta odası (TAMAM) → görev fişi →
  baskın emri → taktik tahta → sonuç → operasyon raporu → sığınak → atölyeyi onar → personel ata → vardiya şalteri →
  vardiya raporu → kapanış (TAMAM). Her adım oyuncunun eylemini bekler; zaten yapılmış adımlar geçilir. Not açılış, emir
  belgesi, vardiya dizisi, ayarlar, CRT geçişi ve son rapor sırasında gizlenir. Masa pozunda (şalter sağ altta) ve baskın
  bitince (sonuç kartı sağ altta) not sağ üste çıkar.
- **Senaryolu ilk baskın kaldırıldı (kayıt 21):** eski `TutorialRaid` (tohum 129, elle çizilmiş haritada 18 hamlelik bilinen
  çözüm, rehber çerçevesi) yalnız kartsız oyunda ve tek tahtada açılabiliyordu; tesis tek kip olunca hiçbir kipte ulaşılamadığı
  için silindi (`TutorialRaid.cs`, `RaidGuide`, `Marks.Guide`, `TutorialRaidTests`). Kartsız rehberin taktik tahta adımı
  rehbersiz tesis baskınını bekler; kartlı rehber (`PlanningTutorialFlow`) zaten tesis metniyle çalışır.
- "Eğitimi atla" ya da kapanış eğitimi bitirir (PlayerPrefs `Corrigible.TutorialDone`; editörde Corrigible › Eğitim ›
  Eğitimi sıfırla). Kayıttan devam eden oyunda eğitim yoktur. Simülasyona dokunmaz.
- **Kapı ve kabul tezgâhı (K8, K8A, K8R; Revizyon 3 · Aşama 6, KOS aşama 2 ve 7):** yeni aday grubu gelince kapı bir kez üç
  vuruşla çalar (yüklemede çalmaz, kamera dönmez), lamba amber kalır, teleks “KAPI · n aday kapıda” basar, kapının yanındaki kabul
  tezgâhı iner. KAPI tuşu K8'e, tezgâha tık K8A'ya götürür. Tezgâhta 2–3 aday kartı ve grup fişi (dolu sığınakta YER YOK listesi,
  satıra tık yollanacak kişiyi işaretler) durur; raftaki iki damga (KABUL · TAM ERİŞİM, GERİ ÇEVİR) sürüklenip ya da tıklanıp
  basılır: KABUL bir karta (`candidate_choose`), GERİ ÇEVİR grup fişine (`candidate_pass`); kilitliyken pirinç dil ve gerekçe.
  KABUL'de kâğıtlar çekmeceye iner, tezgâh katlanır, kapı açılır (koridor boş), kişinin kartı Boşta'ya düşer. Kâğıtlara tık
  K8R'de okutur; Esc / sağ tık bir seviye geri, MASA tuşu masaya. Kişinin izleri ve olay kayıtları panodaki kişi dosyasında
  ("robot" yazmaz), dosyada KISITLI / TECRİT / İZ etiketi. Aşama 7'de emekli araçlar (büyüteç, talep defteri, kalem, KISITLI ve
  YARDIM damgaları), belge, talimat şeridi, bulgu fişleri, tenekeler ve panelin etiketleri sahneden söküldü; kapıdaki buğu camı
  ve interkomun gövdesi durur ama tıklanmaz. K8G pozu emeklidir (çapa sahnede, hiçbir yol odaklamaz). Aşağıdaki kapı
  kâğıdı/siluet dizisi eski ziyaretçi yoludur.
  **Eski kapı görünümü (Revizyon 2 · S4):** sağ duvarda çarklı çelik kapı: üstünde tehlike bandı ve kafesli vuruş lambası, yanında
  interkom, solunda tebeşirle ziyaretçi çentiği; arkasında fayanslı merdiven boşluğu. Biri çalınca (vardiya raporu
  dosyalanıp masa boşalınca) üç vuruş gelir, lamba amber yanar ve her vuruşta parlar, tavan ampulü titrer, teleks "KAPI
  ÇALIYOR" basar ve kamera kapıya döner; kâğıt ekranın solunda açılır. Gözetleme ve interkomun cevabı kâğıtta ayrı
  bloklarda; kullanılan seçenek BAKILDI / SORULDU damgasıyla kapanır. 1–6 seçenek, Esc masaya döner (ziyaretçi vardiya
  bitene kadar bekler; 4, K ya da kapıya tıklamak kâğıdı geri getirir), Enter sonucu kapatır. Kapı açılınca çark döner,
  kanat koridora açılır, merdivenin ışıkları yanar ve ziyaretçinin silueti görünür (altı tür: anne, yaşlı, silahlı, asker,
  yaralı, kopya; ötekiler genel biçim, kalabalıklar birden çok siluet). Harekâtı bitiren kararda (VURULDU, SIZMA) oda ve
  lamba kırmızıya döner, kâğıda çift çizgili damga iner; yalnız Enter / SON RAPORA GEÇ etkindir, son rapor ona basılınca
  açılır. Vardiya raporu açılmayan kapıyı EKİP bölümünde yazar; ön kontrol kapıda bekleyeni uyarır.
- Komutların gerekçesi ve sonucu teleksten basılır (`TelexReasons`). Ayrıntı: `Docs/UI_Siginak_Rev2/IMPLEMENTATION_MAP.md` (Revizyon 2; ilk tur `Docs/UI_Siginak/IMPLEMENTATION_MAP.md`).

## Son rapor

Harekât bitince (Revizyon 2 · S8) kamera harita pozuna geçer, harita karartılır ve 1620 × 952 rapor aşağıdan gelir:
sonuç damgası (Silme: KAZANILDI yeşil; Dağılma, Son kişi ve eski Vuruldu/Sızma sonları: KAYBEDİLDİ kırmızı + küçük neden damgası), sonucu söyleyen
cümle (kazanınca altında “AĞA BAĞLANMA · ANALOG KAL”, kayıpta sonun metni), dört gösterge (SÜRE vardiya; BASKIN başarılı ·
başarısız; ÖZGÜR BÖLGE x/12 + sektör; SIĞINAK NÜFUSU, kayıpta kırmızı “kişi kaldı”), işgal şeridi (x = vardiya 1…N her
zaman tam genişlik; kırmızı işgal, mavi geri alınan; karşı saldırılar yukarı çıkıntı) ve üstünde numaralı kırılma anları,
altta aynı numarayla satırlar (en çok altı: ilk özgür bölge, karşı saldırılar, öteki özgür bölgeler, Dağılma'da son güven
düşüşü, son olay: son sunucu / güvenin sıfırlanması / kapının açılması), **YENİDEN BAŞLA** (Enter). İşaretin ya da satırın
üzerine gelince ikisi birlikte seçilir (`FinalReportBuilder`, `FinalReportView`). KOS sonlarında (aşama 6) başlık ve damga
sonun kendisinindir (SIĞINAK DÜŞTÜ, SIĞINAK BOŞALDI, ÇEKİLME / TAHLİYE ve nedeni); zaferde ton damgası ve ölçüt satırları
("tuttu / tutmadı"), her sonda kapı bölümü yazar.

## Kayıt ve kalıcılık

- Kayıt = tohum + denge değerleri + **komut geçmişi** (`GameRecord`, **biçim 22**, `GameRecord.CurrentVersion`; 22 = görevdeki yatmayan da
  vardiya kapanışında iyileşir (`boardDutyHeal`); 16 = aday grubu
  ve olaylar, 17 = ifşa, yeni sonlar ve son şalter, 18 = üretilmiş harita ve haritanın parmak izi `GameRecord.map`, 19 = tesis
  baskınının oda zinciri (`raid_room`) ve tesis izi `GameRecord.facility`, 20 = g5 haritasında paket
  yerleşimi ve YZ adları, 21 = tek tahta kaldırıldı: tesis baskını tek kip, `ResolveRaid` sonucu hep oda raporlarından türetir,
  `RaidParams.MapSlot`/`ExtraTurns` ve görev ek turları yok, saha sızmacısı varsayılan açık; harita kayda yazılmaz, parmak izi tutmayan kayıt yüklenmez, yeniden oynatmada başarısız komutlar
  `Simulation.ReplayFailures`'ta sayılır). İsimli kişiler, sabit gizli kimlik, görev/eşya, ayrılmış üretim girdisi, eğitim, basılı devir fişi, aday
  seçimleri ve olay kararları `Replay.Restore` ile yeniden kurulur. `board_*` komutları (eşya hurdalama `board_scrap` dahil) ve
  `candidate_choose`, `candidate_pass`, `event_decide`, `last_switch` kayda girer. Tesis komutları: `raid_room` (biten oda,
  a = `RoomReport.Encode()`), `server_reward` (a ödülü bekleyen bölge, b `tarif` ya da `kazanim`), `siege_defend` ve
  `siege_yield` (kuşatmanın karar vardiyasında savun ya da bırak; argümansız). Eski kabulün beş komut türü
  (`inspectApplicant`, `decideApplicant`, `reviewResident`, `restrictResident`, `dismissResident`) aşama 7'de silindi:
  `Replay.Apply` onları tanımaz (sessizce atlar). Olaylar açık oynanmış bir biçim 17 kaydında bu türler yoktu; yalnız olaylar
  kapalı oynanmış (yayınlanmamış) bir kayıt onları taşıyabilirdi; öyle bir kayıt başka oyuna döneceği için yüklenmez
  (`SaveStore.HasRetiredCommand`, yükleme süzgeci) ve yeni oyun yazılırken `.retired.backup` olarak arşivlenir. `board_file` kapanan güne yazılır ve günü kendisi ilerletir; eksik devir komutu sonsuz döngü yerine hata verir.
- Denge değerleri kayda yazılır (`GameRecord.balance`, bütün alanlar); yeni oyun `BalanceConfig.asset`'in kopyasıyla başlar,
  kayıttan devam kaydın kendi dengesiyle. Bu yüzden yalnız varsayılan sayıları değiştirmek (KOS aşama 8 dengesi) sürüm
  istemez: eski kayıt eski sayılarla aynı oyuna döner (eski dengeli kayıt yeni kodla birebir yeniden kuruldu, `DURUM.md`).
- Tesis odaları kayda yazılmaz, tohum ve dengeden yeniden üretilir; `GameRecord.facility` üreticinin izidir: saha sızmacısı
  kapalıysa `f8`, açıksa `f9` (`FacilityMaps.FingerprintFor`); kayıt 21'den beri hiç boş değildir, boş izli kayıt yüklenmez. `Replay.MapMatches` harita ve tesis
  izini birlikte sınar; tutmayan kayıt yüklenmez ve yeni oyun yazılırken `.map.backup` olarak arşivlenir, `Replay.Restore`
  izli kayıtta uyuşmazlıkta hata verir.
- Baskın sonucu kişilerin kalan canını, plakasını, geride kalanları ve ayakta kalan sivilleri taşır (`RaidResult.Encode`).
  Her başarılı komutta ve uygulama kapanırken kaydedilir; harekât bitince etkin kayıt silinir.
- Sürümü 22 olmayan kayıt yüklenmez (`SaveStore.Load`: `rec.version != GameRecord.CurrentVersion`); 21 ve öncesi bu
  kurallarla yeniden oynatılmaz. Yeni oyun eski sürüm dosyasının üzerine ilk kez kaydedilirken `SaveStore` sürüm numaralı
  `.v<sürüm>.backup` kopyasını korur; bu bir kayıt göçü değildir, eski harekât yeni sürümde devam etmez.
- Dosyalar `Application.persistentDataPath` altında: `liberation_savegame.json` (oyun) ve kayda girmeyen sunum durumu
  için `liberation_desk.json` (teleks, evrak geçmişi, kamera pozu; tohum eşleşmezse yok sayılır). Editör testleri
  `Corrigible.TestStorage` oturum anahtarıyla ayrı klasöre yönlendirilebilir.

## Başarımlar (Steam)

`Game/Platform.cs → Achievements.ForEnding(son, çeşit, ton)` (KOS aşama 6; çeşit ve ton `Simulation.EndReport`'tan,
`GameController.Flush` açar):

| Son | Başarım |
|---|---|
| `silme` | `ACH_ERADICATION`; ton `temiz` ise ayrıca `ACH_ERADICATION_CLEAN` |
| `salter` düzenli çekilme | `ACH_WITHDRAWAL` |
| `salter` tahliye | `ACH_EVACUATION` |
| `dusus` | `ACH_SHELTER_FALLEN` |
| `terk` | `ACH_SHELTER_ABANDONED` |
| `topluluk`, `dagilma`, `vuruldu`, `sizma` | yok |

Yalnız `ACH_ERADICATION` Steamworks'te tanımlı; yeni beş kimlik **yalnız kod eşlemesidir** ve yayından önce Steamworks'te
tanımlanmalıdır (tanımlanmazsa `SetAchievement` false döner, sessizce düşer). KOS aşama 8'de tablo gözden geçirildi ve
değişmedi; Steamworks'te tanımlamak geliştiricinin işi (`DURUM.md` › Açık işler).
