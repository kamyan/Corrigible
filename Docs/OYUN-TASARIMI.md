# Oyun tasarımı (uygulanmış hâl)

Bu belge oyunun **kodda gerçekten nasıl çalıştığını** anlatır. Sayılar `Balance` varsayılanlarıdır
(`Assets/Corrigible/Core/Balance.cs`); oyunda `Settings/BalanceConfig.asset` ile değiştirilebilir. Kurallar
`Core/CampaignLiberation.cs`, `Core/Simulation.cs`, `Core/ShelterLogic.cs` ve taktik tahta için `Core/Baskin.cs`
içindedir.

## Fikir ve ton

- Bir yapay zekâ dünyanın bütün ağlarını ele geçirmiş, 12 bölgenin hepsi işgal altında. Oyuncu KYGA'nın Batı
  Avrupa'daki son sığınağında, analog bir komuta odasındadır ("Ağa bağlanma, analog kal").
- Oda bir masa oyunu gibi çalışır: kâğıt harita, mekanik sayaçlar, teleks, sığınağın kesit maketi, saha yayını için
  tüplü monitör ve vardiyayı bitiren şalter. Zaman kendiliğinden akmaz; oyuncu hazırlığını yapar, şalteri indirir.
- Görsel dil: sıcak lamba ışığı, kâğıt ve mürekkep; renk anlam taşır (kırmızı işgal/tehdit, mavi geri alınmış, yeşil iyi).

## Harekât

- **Başlangıç:** Bütün bölgeler (`GameData.Regions`: na, sa, weu, eeu, ru, me, af, in, cn, jk, sea, oc) işgal altında;
  işgal oranı %100. Direniş Batı Avrupa'dan (`Simulation.HQ = "weu"`) tek **Alfa** ekibiyle başlar. Başlangıç fonu 60.
- **Sektörler:** Her bölge sırayla üç sektördür: **Tedarik deposu → Tahliye geçidi → Sunucu merkezi**. İlk ikisi
  ikmal (`MissionKind = "loot"`), sonuncusu sunucu imhası (`"server"`). Başarı sektörü kalıcı geri alır ve sıradakini
  açar; geri alınmış sektöre yeniden baskın yapılmaz, sıradaki sektör atlanamaz.
- **İşgal oranı** (`Simulation.Occupation`): geri alınmamış sektörlerin payı. Bölge başına %100 → %66,7 → %33,3 → %0.
- **Yeni cephe:** Bölgenin üç sektörü alınınca, geri alınmış bölgelerden **herhangi birine** `GameData.Links` üzerinden
  komşu olan işgal altındaki bir bölge seçilir (`SelectLiberationRegion`, sınır listesi `LiberationFrontier`). Ortak sınır
  bir yolun çıkmaza dönmesini engeller. Seçilen bölge tamamlanmadan başka bölgeye geçilmez.
- **Son:** Bütün 36 sektör geri alınınca **Silme** (`"silme"`, zafer): "Çekirdek yok edildi, kopyalar temizlendi." Sivil
  güven sıfıra inerse **Dağılma** (`"dagilma"`, yenilgi; aşağıda). Çekirdeği düşüren baskın aynı anda güveni bitirse de
  zaferdir.
- Son bölgenin sunucu merkezinde YZ çekirdeği vardır (`IsCore`).

## Zorluk kademesi (`Simulation.RaidTier`, `RaidParams.Tier`)

Harekât ilerledikçe baskınlar sertleşir. Kademe özgür bölge sayısına bağlıdır (`liberationTier2At/3At/4At`: 2, 6, 10):

| Kademe | Özgür bölge | Başta ek makine | Uyanma / tur | Makine canı | İmza makinesi |
|---|---|---|---|---|---|
| 1 | 0–1 | 0 | 1 | normal | 1 |
| 2 | 2–5 | 1 | 1 | normal | 1 |
| 3 | 6–9 | 2 | 2 (`"kopya"`) | normal | 2 |
| 4 | 10–11 | 3 | 2 | +1 (`"guclu"`) | 2 |

Ek makineler tahtanın uzak yarısına konur; dron, bakım robotu ya da (üçte bir) bölgenin imza makinesi olur. Uyanan
makineler de aynı dağılımla seçilir (`Baskin.WakeType`). Görev çipi ve görev fişi kademeyi yazar ("Tehdit 2/4").

## Bölge kimliği (`RegionDef.Trait`, `RegionDef.Enemy`)

Her bölgenin kendine özgü bir tahta özelliği (`Baskin.RegionTraits`) ve bir **imza makinesi** vardır; altı yeni makinenin
her biri iki bölgede. Özellik bölgenin her baskınında `RaidParams.Traits`'e girer; imza makinesi tahtadaki makinelerden
birinin (3. kademeden ikisinin) yerini alır. Görev fişi ve bölge bilgi fişi ikisini de yazar; baskında görev çipinde
özellik, üstüne gelince açıklaması, imza makinesi ve kademe görünür.

| Bölge | Özellik | Etkisi | İmza makinesi |
|---|---|---|---|
| Batı Avrupa (başlangıç) | Güvenli giriş | İlk tur uyanma yok | Zırhlı dron |
| Doğu Avrupa | Dar koridor | +3 duvar | Kilitleyici |
| Kuzey Amerika | Kısa nöbet | Baskın 5 tur | Havan robotu |
| Güney Amerika | Takviye | Başta +1 makine | Patlayan robot |
| Afrika | Açık arazi | −3 duvar | Havan robotu |
| Orta Doğu | Kum fırtınası | Uyanma yerleri gizli | Patlayan robot |
| Rusya | Soğutma ağı | +2 soğutma ünitesi | Kalkan dronu |
| Hindistan | Kalabalık şehir | +2 sivil sunucu | Kalkan dronu |
| Çin | Zırhlı dolap | Hedef dolaplar +1 can | Çoğaltıcı |
| Japonya-Kore | Elektrik şebekesi | +3 elektrikli kanal | Kilitleyici |
| Güneydoğu Asya | Kopya fabrikası | +1 kopya dolabı | Çoğaltıcı |
| Okyanusya | Hızlı devriye | Makineler +1 yürür | Zırhlı dron |

Duvar ve nesne ekleyen özellikler ekip girişinin yanını boş bırakır ve yolu kapatmaz (`Baskin.Connected`); kapatacak
parça eklenmez.

## Sivil güven (`S.Trust`, `CampaignLiberation.cs → ChangeTrust`)

- Harekât `liberationTrustStart` (10) güvenle başlar; bu aynı zamanda tavandır.
- Baskında kırılan her sivil sunucu (`RaidResult.Collateral`, kimin kırdığına bakılmaz) güveni `liberationTrustPerCivilian`
  (1) düşürür; başarılı ya da başarısız her baskında. Özgürleşen her bölge `liberationTrustPerRegion` (2) geri verir.
  Değişim `LiberationRaidReward.Trust`'a yazılır; operasyon raporu "Sivil güven −2 · 8/10" satırını, güven 3 ve altına
  inince uyarı bandını gösterir.
- Güven 0 olunca harekât **Dağılma** ile biter; son rapor KAYBEDİLDİ damgası ve "YZ kazandı." ile açılır.
- **Makineler sivil sunucuya bilerek ateş etmez** (`Baskin.ScoreTarget`: sivil hattı −2). Sivil sunucu yine kırılabilir:
  hedefi atıştan kaçan ekip arkasındaki sunucuyu açıkta bırakır (niyet önceden sabitlenir, atış hattındaki ilk engeli vurur),
  EMP, itme ve patlayan soğutma ünitesi de kırar. Karar şudur: kaçmak mı (sunucu gider), atışı göğüslemek mi (1 hasar).
- **Denge ölçümü** (2026-10-27, açgözlü taktik bot, 10'ar harekât): dikkatsiz bot 10 harekâtın 10'unda 16–41. vardiyada
  dağıldı; orta dikkatte 9/10, dikkatli 10/10 kazandı. Dikkatli bot baskın başına ~0,3, dikkatsiz ~0,6 sivil sunucu kırıyor.
  Makineler sivili hedeflerken (eski kural) dikkatli bot da ~2,5 kırıyordu; güven beceri ölçmüyordu.

## YZ alarmı ve karşı saldırı (`S.Alarm`, `CampaignLiberation.cs → ChangeAlarm, Counterattack`)

- **Bir baskın, bir vardiya:** baskından dönen ekip (yaralı olmasa da) o vardiya yeniden çıkamaz; `Busy` adımda düşer.
  Eskiden ekip dönüşte hazırdı; bütün harekât 1–3 vardiyada bitebiliyordu, vardiya şalterinin anlamı yoktu.
- **Alarm** her vardiya `liberationAlarmPerShift` (+1), her başarısız baskın `liberationAlarmPerFailure` (+3) yükselir;
  kırılan her YZ sunucusu (sunucu merkezi sektörü) `liberationAlarmPerServer` (−4) düşürür.
- **Telsiz odası** (sığınak) vardiya alarmını yavaşlatır: görevli başına `shelterRadioAlarmPerWorker` (−0,05; eksiye inmez,
  `Simulation.AlarmPerShift`).
- **Eşik** `liberationAlarmThreshold` (10): alarm sıfırlanır, YZ **karşı saldırır**: en son geri alınan sektör yeniden
  işgal altına girer (özgür bölgeyse bölge yeniden cephe olur, sınırdan seçilir) ve sivil güven
  `liberationCounterattackTrust` (1) düşer. Geri alınacak sektör yoksa yalnız güven düşer; boşta beklemek de sonunda
  Dağılma'ya varır. Yeniden özgürleşen bölge güveni ve bölge fonunu yeniden verir (ölçümde de böyleydi).
- **Gösterim:** CRT'de "YZ ALARMI n/10" (bekleme ve devre dışı ekranı), kenar satırında ALARM, vardiya raporunun
  EKİP VE YZ ALARMI bölümünde alarm satırı, operasyon raporunda "YZ alarmı" ve karşı saldırı satırları, karşı saldırıda
  teleks (YZ KARŞI SALDIRISI) ve ses, son raporda kırmızı damgalı "KARŞI SALDIRI" kartı.
- **Denge ölçümü** (2026-10-27, açgözlü bot, 10'ar harekât): eşik 14'ken (sığınak baskına bağlanmadan önce) saldırgan
  0/10, dengeli 6/10, koruyucu 9/10. Sığınak hazırlık yeri olunca telsiz alarmı neredeyse siliyordu; eşik 10'a, telsiz
  görevli başına −0,05'e indi (son ölçüm aşağıda, Sığınak). Sessiz vardiyanın alarmı düşürdüğü kural denendi ve
  bırakıldı: dikkatli oyuncu bedelsiz bekleyip hiç saldırı yemiyordu.

## Vardiya (`Simulation.Step`)

Oyuncu vardiya şalterini indirince (`GameController.AdvanceShift`) bir gün geçer:

1. **Gelir:** `liberationDailyFunds` (2) + özgür bölge başına `liberationRegionIncome` (0,5) fon (`IncomeBreakdown`).
   Her fon değişimi `TransactionLedger`'a yazılır.
2. **Ekip:** bu vardiya baskına çıkmadıysa ayaktaki her uzman `liberationRestHeal` (1) yara toparlar (dinlenme); yatan
   uzmanlar ve yedekler bir gün iyileşir; baskın meşguliyeti düşer (`StepTeams`).
3. **Sığınak:** erzak toplama ve tüketim, atölyede plaka dökümü, revirde yara sarma (`ShelterLogic.Advance`, aşağıda).
4. **YZ alarmı:** +1 eksi telsiz; eşikte karşı saldırı (yukarıda).
5. **Kayıt:** son rapordaki şerit grafik için işgal oranı (`S.Hist`).

Baskın sürerken (`S.Raid`), harekât bitmişken ya da açılış/emir belgesi açıkken vardiya bitirilemez.

## Baskın

- **Emir** (`StartLiberationRaid`): vardiyada bir kez, yalnız mevcut bölgenin sıradaki sektörüne, hazır bir ekip ve `liberationRaidCost`
  (12) fonla; kadroda Teknisyen (asıl ya da yedek) olmalı. Baskın bölgenin özelliğini, imza makinesini ve zorluk
  kademesini taşır (yukarıda). Kamera CRT'ye dalar ve `Baskin` sahnesi odanın üstüne yüklenir.
- **Kadro** (`Simulation.Squad`, `RaidParams.Squad`): her rol (Teknisyen, EMP uzmanı, Analist) için ayaktaki uzman
  yarasıyla (eksik canla) çıkar; yatan uzmanın yerine boştaki ilk yedek uzman, rolün canından `liberationRookieHpPenalty`
  (1) eksik; yedek de yoksa rol boş kalır, tahtada o birim olmaz. Zırh plakaları en düşük canlı uzmandan başlayarak birer
  tane takılır; plaka ilk hasarı karşılar ve kırılır (`Baskin.Hurt`, efekt `plate`).
- **Taktik tahta** (`Core/Baskin.cs`, 8×8, 6 tur; Kısa nöbette 5): oyuncu birimleri **Teknisyen** (sabotaj), **EMP uzmanı**
  (EMP atışı, iter), **Analist** (kanca). Kopya dolapları her tur makine uyandırır. Makineler (`Baskin.UnitTypes`; niyet
  türü `Kinds`):

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
 Makinelerin bu turdaki hamlesi kırmızı okla, sırası bayrakla görünür. Hamleler geri
  alınabilir (`Undo`), önizleme durumu değiştirmez. Kırılan sivil sunucular sivil güveni düşürür (aşağıda).
  - **İkmal:** Teknisyen ikmal kasasına bitişik kareden yeteneğiyle malzemeyi alır; kasayı kırmak görevi kaybettirir.
  - **Sunucu:** tur bitmeden bütün hedef dolaplar kırılır.
- **Sonuç** (`ResolveRaid`, taktik sonuçtan masaya dönülürken bir kez):
  - İkmal başarısı: +12 fon, +10 malzeme, +10 erzak, yer varsa +1 kurtulan.
  - Sunucu başarısı: +8 fon, +6 malzeme.
  - Bölge tamamlanınca ayrıca +15 fon.
  - Başarısızlık ya da geri çekilme sektörü değiştirmez.
  - **Kadro döner** (`ReturnSquad`, sonuçta kadronun kalan canı ve plakası `RaidResult.Hp/Armor`): ayaktaki uzmanın eksik
    canı yara olarak kalır; düşen uzman ya da yedek `liberationInjuryDays` (3) gün yatar ve sağlam kalkar; kırılan plaka
    sığınaktan düşer. Kalıcı kayıp yok. Teknisyen yatıp yedek de yoksa ekip kalkana kadar baskına çıkamaz.
  - Depo sınırına takılan ödül yalnız gerçekten alınan miktarla raporlanır (`LiberationRaidReward`).
- **Operasyon raporu:** masaya dönünce hedef, sonuç, ödüller ve bölge durumu bir kâğıtta açılır (`SimDeskBuilder.Report`).

## Sığınak (`ShelterState`, `ShelterLogic`, `Simulation.Shelter.cs`)

Sığınak baskının hazırlık yeridir: ürettiği her şey bir sonraki baskına girer. Beklemenin karşılığı hazırlık (dinlenme,
plaka, revir), bedeli alarm; vardiya şalteri böylece karara döner. 6 kişi, 24 erzak ve 14 malzemeyle başlar. Yaşam alanı
ve telsiz odası seviye 1; atölye ve revir onarılmayı bekler. **Seviyeler özgür bölgeyle açılır**
(`Simulation.ShelterLevelLock`): seviye 2 için 2, seviye 3 için 5 özgür bölge (`shelterLevel2Regions`, `shelterLevel3Regions`);
kilitliyken panoda düğme KİLİTLİ, hazırlık satırında gerekçe. Odalar en fazla seviye 3 olur; onarım/geliştirme
(`ShelterUpgrade`) fon ve malzeme ister, görevlendirme (`ShelterAssign`) değiştirilene kadar sürer. Oda başına personel
kapasitesi `2 × seviye`dir. Enerji yoktur.

| Oda | Rol | Onarım/geliştirme |
|---|---|---|
| Yaşam alanı | Seviye başına 8 kişilik yer, 40 erzak deposu ve **1 yedek uzman**; görevli başına vardiyada 1 erzak toplama | 8 fon + 6 malzeme |
| Telsiz odası | Görevli başına vardiya alarmı −0,05 | 6 fon + 4 malzeme |
| Atölye | Görevli başına vardiyada 0,25 **zırh plakası** (plaka 4 malzeme); depo seviye başına 1 plaka | 10 fon + 6 malzeme |
| Revir | Görevli başına vardiyada 0,34 **yara** sarar (en yaralıdan); en az 1 görevliyle yatan uzman 1 gün erken kalkar | 12 fon + 8 malzeme |

Her kişi vardiyada 0,2 erzak tüketir. Erzak biterse atölye yarı hızda döker ve revir durur; yaşam alanında toplama sürer.
Atölye artık malzeme üretmez: malzeme baskınlardan gelir (ikmal +10, sunucu +6) ve odalarla plakalar arasında paylaşılır.
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

## Komuta odası (sunum)

- **Masa (K1):** sayaç bankası (VARDİYA, FON, ERZAK, MALZEME, ÖZGÜR BÖLGE, GÜVEN), anahtar rayı (1 KOMUTA, 2 SIĞINAK,
  3 GÖREVLER, H KILAVUZ, Home MASA), vardiya şalteri, teleks, evrak tepsisi.
- **Harita (K2, K3):** bölge seçilince görev fişi (sektörler, bedel/ödül, baskın emri); bölge tamamlanınca cephe onay fişi.
  Ekip figürü haritada sürüklenebilir.
- **Sığınak maketi (K4):** odaların durumu (çalışıyor, DURDU, HASARLI), personeli ve geliştirme kararları; pano özetinde
  TOPLULUK (yedek dahil), UZMANLAR ("T 3/3 · E 2/3 · A yaralı (2)"), ERZAK, PLAKA.
- **CRT (K5):** saha yayını; yalnız baskın emri kamerayı monitöre yaklaştırır. Teknisyen yatıp yedek de yoksa yayın devre
  dışı ("ALFA · TEKNİSYEN YARALI · 2 GÜN").
- **Görev fişi** kadroyu baskına çıkacağı gibi, rol harfleriyle yazar: "T 3/3 · E 2/3 · A (yedek) 1/2 · 2 plaka".
- **Vardiya geçişi (K6):** şalter → sayaçlar döner → vardiya raporu; rapor evrak tepsisine kayar.
- **Açılış (K7):** açılış dizisi ve emir belgesi; BAŞLA ile oda açılır. Kayıt varsa son rapor kısaca görünür.
- **Eğitim (rehberli ilk vardiyalar):** yeni harekâtta, eğitim daha önce bitirilmediyse ya da atlanmadıysa sağ altta
  analistin notu çıkar (`SiginakTutorial`, adımlar `TutorialFlow.Steps`). 12 adım: komuta odası (TAMAM) → görev fişi →
  baskın emri → taktik tahta → sonuç → operasyon raporu → sığınak → atölyeyi onar → personel ata → vardiya şalteri →
  vardiya raporu → kapanış (TAMAM). Her adım oyuncunun eylemini bekler; zaten yapılmış adımlar geçilir. Not açılış, emir
  belgesi, vardiya dizisi, ayarlar, CRT geçişi ve son rapor sırasında gizlenir. Masa pozunda (şalter sağ altta) ve baskın
  bitince (sonuç kartı sağ altta) not sağ üste çıkar.
- **Senaryolu ilk baskın (`TutorialRaid`):** eğitimin taktik tahta adımında açılan baskın harekâtın ilk baskınıysa (Batı
  Avrupa'nın ilk sektörü, kademe 1, tam ve sağlam kadro) sabit tohumla (129) kurulur ve oyuncu bilinen çözümü adım adım
  oynar: 11 hamle, 21 arayüz adımı (uzmanı seç, yürü, Q, hedefe tıkla, turu bitir), üç tur. Ders sırası: yürümek, EMP
  itişiyle zırhlı dronu duvara çarptırıp zırhını kırmak, kancayla dost uzmanı çekmek, makinelerin sırasını okumak (birbirini
  vururlar), atış hattından çıkmak, kasayı almak; hasarsız zafer. Sıradaki kare tahtada nabız gibi atan sarı çift
  çerçeveyle işaretlenir; not "BASKIN · ADIM n/21" başlığıyla talimatı ve dersi yazar, makineler oynarken sırayı izlemeyi
  söyler. Yalnız sıradaki eyleme izin verilir: başka uzman, başka kare, geri alma, geri çekilme, Q'yu ya da seçimi Esc ile
  bırakma kapalı; boşta uzman kalınca tur bitirme onayı sorulmaz. Beklenen hamle tahtada yapılamazsa (olmamalı) rehber
  çekilir, baskın serbest oynanır. Simülasyon yalnız baskının sonucunu gördüğü için kayıt bozulmaz.
  "Eğitimi atla" ya da kapanış eğitimi bitirir (PlayerPrefs `Corrigible.TutorialDone`; editörde Corrigible › Eğitim ›
  Eğitimi sıfırla). Kayıttan devam eden oyunda eğitim yoktur. Simülasyona dokunmaz.
- Komutların gerekçesi ve sonucu teleksten basılır (`TelexReasons`). Ayrıntı: `Docs/UI_Siginak/IMPLEMENTATION_MAP.md`.

## Son rapor

Harekât bitince tam ekran son rapor açılır: sonuç damgası (Silme: KAZANILDI; Dağılma: KAYBEDİLDİ, "YZ kazandı."), dört gösterge (harekât süresi, baskın sayısı, özgür bölge,
sığınak nüfusu), işgal oranı şerit grafiği, kırılma anları (geri alınan sektörler ve sonuç) ve **Yeniden başla**.

## Kayıt ve kalıcılık

- Kayıt = tohum + denge değerleri + **komut geçmişi** (`GameRecord`, biçim 8: bölge kimliği ve zorluk kademesi); devam,
  komutları yeniden oynatır (`Replay.Restore`). Baskın sonucu kadronun kalan canını ve plakasını da taşır
  (`RaidResult.Encode`). Her başarılı komutta ve uygulama kapanırken kaydedilir; harekât bitince silinir.
  Biçim 7 ve öncesi kayıtlar yüklenmez.
- Dosyalar `Application.persistentDataPath` altında: `liberation_savegame.json` (oyun) ve kayda girmeyen sunum durumu
  için `liberation_desk.json` (teleks satırları, evrak tepsisindeki son 5 vardiya raporu, kamera pozu; tohum
  eşleşmezse yok sayılır).

## Başarımlar (Steam)

`Game/Platform.cs → Achievements`: yalnız `ACH_ERADICATION` (Silme sonu).
