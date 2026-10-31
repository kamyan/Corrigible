# Oyun tasarımı (uygulanmış hâl)

Bu belge oyunun **kodda gerçekten nasıl çalıştığını** anlatır. Sayılar `Balance` varsayılanlarıdır
(`Assets/Corrigible/Core/Balance.cs`); oyunda `Settings/BalanceConfig.asset` ile değiştirilebilir. Kurallar
`Core/CampaignLiberation.cs`, `Core/Simulation.cs`, `Core/ShelterLogic.cs`, kapı için `Core/Door.cs` ve `Core/DoorCards.cs`,
taktik tahta için `Core/Baskin.cs` içindedir.

## Fikir ve ton

- Bir yapay zekâ dünyanın bütün ağlarını ele geçirmiş, 12 bölgenin hepsi işgal altında. Oyuncu KYGA'nın Batı
  Avrupa'daki son sığınağında, analog bir komuta odasındadır ("Ağa bağlanma, analog kal").
- Oda bir masa oyunu gibi çalışır: kâğıt harita, mekanik sayaçlar, teleks, sığınağın kesit maketi, telsizin tüplü
  göstergesi ve vardiyayı bitiren şalter. Zaman kendiliğinden akmaz; oyuncu hazırlığını yapar, şalteri indirir.
- **Sahayla tek bağ telsizdir.** Sığınak ağa bağlanmadığı için baskın bir görüntü yayını değil: hat açılır, gözcü
  makinelerin hamlesini telsizden bildirir (kırmızı oklar), durum saha masasındaki paftada figürlerle tutulur. Oyuncunun
  hamleleri tek tek, anında iletilir; EMRİ İLET (Enter) turu bitirir ve komuta dinlemeye geçer (DİNLEMEDE). Baskında saha
  sesleri telsizin dar bandından duyulur. Paftanın kenarında kare adları var (A–H, 1–8); gözcü kareyi adıyla bildirir
  ("saldırı → TEKNİSYEN · A6"). Okları yağlı kalemle çizilmiş gibidir. Komuta masasında CRT'nin önünde bas-konuş mikrofonu ve telsiz kayıt defteri durur.
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
  zaferdir. Kapıyı yanlış kişiye açmak da harekâtı bitirir: **Vuruldu** (`"vuruldu"`, silahlı bir ziyaretçi) ve **Sızma**
  (`"sizma"`, YZ'nin kopyası); ikisi de yenilgidir (aşağıda, Sığınağın kapısı).
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
| Kuzey Amerika | Kısa nöbet | Baskın bir tur kısa | Havan robotu |
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

- Harekât `liberationTrustStart` (10) güvenle başlar; tavan `liberationTrustMax` (20, `Simulation.TrustMax`).
- Baskında kırılan her sivil sunucu (`RaidResult.Collateral`, kimin kırdığına bakılmaz) güveni `liberationTrustPerCivilian`
  (1) düşürür; başarılı ya da başarısız her baskında. **Kazanılan** baskında tahtada ayakta kalan her
  `liberationCiviliansPerTrust` (2) sivil sunucu +1 geri verir (`RaidResult.CiviliansLeft`; kaybedilen baskının sivilleri
  işgal altında kalır). Özgürleşen her bölge `liberationTrustPerRegion` (3) geri verir. Değişim `LiberationRaidReward.Trust`'a
  yazılır; operasyon raporu "Kırılan sivil sunucu" ve "Sivil güven +1 · 11/20" satırlarını gösterir, güven 3 ve altına
  inince başlığın altındaki cümle uyarır. Kampanyada sivil sunucu `liberationCivilianHp` (2) canlıdır.
- Güven 0 olunca harekât **Dağılma** ile biter; son rapor KAYBEDİLDİ ve DAĞILMA damgalarıyla açılır.
- **Ekibe ulaşamayan makine sivile nişan alır** (gelişmiş akıl; `Baskin.CanReachTeam`, `ScoreTarget(..., hostage)`; turda en
  çok bir makine, niyeti önceden görünür). Ulaşabilen makine sivili hedeflemez (sivil hattı −2). Sivil sunucu başka yoldan da
  kırılır: hedefi atıştan kaçan ekip arkasındaki sunucuyu açıkta bırakır, EMP, itme ve patlayan soğutma ünitesi de kırar.
  Oyuncunun işi: uzaktaki makineyi düşürmek ya da araya girmek; kaçmak mı (sunucu gider), atışı göğüslemek mi (1 hasar).
- **Denge ölçümü** (2026-10-28, açgözlü taktik bot, havuz haritaları, 10'ar harekât; ayrıntı `Docs/DURUM.md`): kural açıkken
  makineler baskın başına ~1 sivil kırıyor, ~3,6 sivil ayakta kalıyor. Ödülsüz ve tavan 10 iken hiçbir harekât
  kazanılmıyordu (0/10); ayakta kalan her sivil +1 iken güven hiç düşmüyordu. Seçilen ayarla (kanca hasarı, her 2 sivil +1,
  tavan 40) orta ve dikkatli bot 8/10 kazanıyor, harekâtların ~%20'si dağılıyor; baskın başarısı %60–65. Kapı geldikten
  sonra tavan 20'ye indi (kullanıcının seçimi): orta bot 30'ar harekâtta kapısız 20/30 kazanıyor, ~%30'u dağılıyor; dağılmalar
  harekâtın sonunda (gün 49–76), güven her harekâtta tavana değiyor ve biriktirilemiyor.

## YZ alarmı ve karşı saldırı (`S.Alarm`, `CampaignLiberation.cs → ChangeAlarm, Counterattack`)

- **Bir baskın, bir vardiya:** baskından dönen ekip (yaralı olmasa da) o vardiya yeniden çıkamaz; `Busy` adımda düşer.
  Eskiden ekip dönüşte hazırdı; bütün harekât 1–3 vardiyada bitebiliyordu, vardiya şalterinin anlamı yoktu.
- **Alarm** her vardiya `liberationAlarmPerShift` (+1), her başarısız baskın `liberationAlarmPerFailure` (+1) yükselir;
  kırılan her YZ sunucusu (sunucu merkezi sektörü) `liberationAlarmPerServer` (−4) düşürür.
- **Telsiz odası** (sığınak) vardiya alarmını yavaşlatır: görevli başına `shelterRadioAlarmPerWorker` (−0,05; eksiye inmez,
  `Simulation.AlarmPerShift`).
- **Eşik** `liberationAlarmThreshold` (10): alarm sıfırlanır, YZ **karşı saldırır**: en son geri alınan sektör yeniden
  işgal altına girer (özgür bölgeyse bölge yeniden cephe olur, sınırdan seçilir) ve sivil güven
  `liberationCounterattackTrust` (1) düşer. Geri alınacak sektör yoksa yalnız güven düşer; boşta beklemek de sonunda
  Dağılma'ya varır. Yeniden özgürleşen bölge güveni ve bölge fonunu yeniden verir (ölçümde de böyleydi).
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

## Vardiya (`Simulation.Step`)

Oyuncu vardiya şalterini indirince (`GameController.AdvanceShift`) bir gün geçer:

1. **Gelir:** `liberationDailyFunds` (2) + özgür bölge başına `liberationRegionIncome` (0,5) fon (`IncomeBreakdown`).
   Her fon değişimi `TransactionLedger`'a yazılır.
2. **Ekip:** bu vardiya baskına çıkmayan ayaktaki her uzman `liberationRestHeal` (1) yara toparlar (dinlenme, uzman
   başına: ekip baskına çıksa da son kadroda olmayanlar dinlenir, `Simulation.Rested`); yatan uzmanlar ve yedekler bir gün
   iyileşir; baskın meşguliyeti düşer (`StepTeams`).
3. **Sığınak:** erzak toplama ve tüketim, atölyede plaka dökümü, revirde yara sarma (`ShelterLogic.Advance`, aşağıda).
4. **YZ alarmı:** +1 eksi telsiz; eşikte karşı saldırı (yukarıda).
5. **Kayıt:** son rapordaki şerit grafik için işgal oranı (`S.Hist`).
6. **Kapı:** kapıda bekleyen ziyaretçi açılmadı sayılır; sırası geldiyse biri kapıyı çalar (`StepDoor`, aşağıda).

Baskın sürerken (`S.Raid`), harekât bitmişken ya da açılış/emir belgesi açıkken vardiya bitirilemez.

## Baskın

- **Emir** (`StartLiberationRaid`): vardiyada bir kez, yalnız mevcut bölgenin sıradaki sektörüne, hazır bir ekip ve `liberationRaidCost`
  (12) fonla; kadroda Teknisyen (asıl ya da yedek) olmalı. Baskın bölgenin özelliğini, imza makinesini ve zorluk
  kademesini taşır (yukarıda). Kamera CRT'ye dalar ve `Baskin` sahnesi odanın üstüne yüklenir.
- **Kadro seçimi** (`Simulation.Roles`, `PickCount`, `DefaultPick`, `ResolvePick`): sığınağın kadrosu beş uzman
  (Teknisyen, EMP uzmanı, Analist, Madenci, Sis uzmanı; hepsi baştan kadroda). Her baskına Teknisyen ve seçilen iki uzman
  çıkar; seçim emir komutuyla kayda girer (`liberation_raid`, b = "maden,duman", kadro sırasıyla). Seçim verilmezse
  varsayılan: ayaktakiler yatanlardan önce, önce son baskının uzmanları (`TeamState.LastPick`), sonra dinlenmiş olanlar
  (yarası az önce); ilk baskında EMP uzmanı ve Analist. Seçim görev fişinin KADRO satırında yapılır (aşağıda, Komuta odası).
  Eğitim sürerken ilk baskının kadrosu sabittir (Teknisyen, EMP uzmanı, Analist), eğitim senaryosu bu üçe göre yazıldı.
- **Kadro** (`Simulation.Squad`, `RaidParams.Squad`, `RaidParams.Roles`): seçilen her rol için ayaktaki uzman yarasıyla
  (eksik canla) çıkar; yatan uzmanın yerine boştaki ilk yedek uzman, rolün canından `liberationRookieHpPenalty` (1) eksik;
  yedek de yoksa rol boş kalır, tahtada o birim olmaz (kadro eksik çıkar; kartı SIĞINAKTA YARALI). Zırh plakaları en düşük
  canlı uzmandan başlayarak birer tane takılır; plaka ilk hasarı karşılar ve kırılır (`Baskin.Hurt`, efekt `plate`).
- **Harita havuzu** (`Core/RaidMaps.cs`): her harekâtın kendi 30 haritası olur, yeni oyunun tohumundan üretilir (adlar
  30 oda adından: Kablo galerisi, Arşiv katı, Kazan dairesi…). Harekât ~57 baskın sürdüğü için her harita ~2 kez gelir
  (havuz 10 iken ~6 kez). Baskınlar havuzu karıştırılmış torbalardan sırayla çeker (`RaidMaps.Slot`,
  `RaidParams.MapSlot` = harekâttaki baskın sırası): her harita bir kez gelmeden hiçbiri tekrar etmez, aynı harita art
  arda gelmez. Eski seçim (kademe + hesaplama gücü + tohumun son üç biti) iki haritayı iki kat sık getiriyordu.
  Bölge özelliği (dar koridor, şebeke…) haritanın üstüne eklenir. Eğitim baskını ve kampanya dışı baskınlar elle çizilmiş
  üç haritada (`Baskin.Layouts`) kalır.
- **Harita türleri** (`RaidMaps.Kinds`; havuzda her türden 5 harita, her türün kendi 5 oda adı; `RaidMaps.KindOf(ad)`):

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
  kuralları `Valid(rows, kind)`. **Ölçüm** (2026-10-28, açgözlü bot, türü başına 40 harita × 4 kademe × 2 görev = 320
  baskın, bölge özellikleri ve makineleri; adım aralıkları bununla ayarlandı): baskın başarısı koridor %72, kasa odası %72
  (ilk denemede tek kapı ve 8–11 adımla %55), iki hat %74, açık salon %69 (ilk denemede %83), şebeke %75, rehine %72.
  4. kademede koridor en zor tür (%38; ötekiler %47–61). Harekât ölçümünde (30 harekât) yalnız koridorlu havuz 19/30,
  türlü havuz 24/30 kazandı; baskın başarısı %66 → %68 (fark gürültü sınırında, ama 4. kademedeki farkla uyumlu).
- **Taktik tahta** (`Core/Baskin.cs`, 8×8, 6 tur; kampanyada ikmal görevinde 8, `liberationLootExtraTurns`; Kısa nöbette bir tur az): oyuncu birimleri

  | Uzman | Can · yürüyüş | Yetenek |
  |---|---|---|
  | Teknisyen (T) | 3 · 3 | Sabotaj: bitişik kare; dolaba 2, robota 1 hasar ve itme; ikmal kasasını açar |
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
  - **İkmal:** Teknisyen ikmal kasasına bitişik kareden yeteneğiyle malzemeyi alır ve **taşır** (`BState.Carrier`); baskın
    ancak taşıyıcı giriş karelerinden birine (`BState.Exits`, yeşil kesik çerçeve) varınca kazanılır (yürüyüş, kanca ya da
    itme). Kasayı kırmak, taşıyıcının düşmesi ya da sürenin bitmesi görevi kaybettirir. Dönüş için ikmal görevi kampanyada iki tur uzun.
  - **Makine aklı** (`RaidParams.Tactics`; kampanyada 1, eğitim baskınında ve kilitli motor testlerinde 0): gelişmiş akıl
    kilit kareleri tutar: kasa alınmadan hedef dolapların yanındaki boş kareler (sunucu görevinde daha ağır), kasa
    alındıysa taşıyıcının çıkışa en kısa yolu ve çıkışlar. Vuracak birim yoksa ateş ve kilit hattını bu karelerden
    geçirir (mermi sırası gelince yeniden nişan alır, hatta adım atan vurulur); yakın dövüşçüler kilit kareyi bedeniyle
    tutar; herkes taşıyıcıyı (yoksa Teknisyen'i) kovalar; taşıyıcı ve hedefin yanındaki Teknisyen öncelikli hedef. Ekibe
    hiçbir kareden ulaşamayan makine sivil sunucuya nişan alır (turda en çok bir; yukarıda "Sivil güven").
    Makineler sırayla plan yaparken öncekilerin tuttuğu kareleri saymaz (eşgüdüm). Rastgele tüketimi temel akılla aynı.
  - **Zorluk ölçümü** (açgözlü bot, havuz haritaları, 1440 baskın; T + E + A kadrosuyla, Madenci ve Sis uzmanı yokken):
    önceki oyun her kademede ~%92. Şimdi (kaçış, gelişmiş
    akıl, 7 turluk ikmal, düğüm +2 can): **toplam %72, ikmal %67, sunucu %76; kademe 1/2/3 %79/%75/%61**. Kadro seçimi
    zorluğu kendiliğinden düşürür (oyuncu her bölgeye en uygun ikiliyi seçer); altı ikilinin ölçümü ve hak sayılarının
    ayarı açık iş (bot depoda yok, yeniden yazılmalı; DURUM). 6 turluk ikmal
    %45'te kalıyordu, 8 tur %80. Sunucuda gelişmiş akıl tek başına etkisizdi (%89: niyetler önceden göründüğü için
    Teknisyen tutulmayan kareden sabotaj yapar); düğümün canı +1 %83, +2 %76; ek makine %89 (etkisiz).
  - **Sunucu:** tur bitmeden bütün hedef dolaplar kırılır. Kampanyada düğüm dolabı `liberationServerTargetHp` (+2) canla
    sertleşir (4 → 6; zırhlı dolapta 7); son bölgenin çekirdeği hariç (5 can, 6 turda yetişilsin). Ek makine ayarı
    `liberationServerMachines` 0 (ölçümde etkisiz).
- **Sonuç** (`ResolveRaid`, taktik sonuçtan masaya dönülürken bir kez):
  - İkmal başarısı: +10 fon, +7 malzeme, +6 erzak, yer varsa +1 kurtulan.
  - Sunucu başarısı: +8 fon, +5 malzeme.
  - Bölge tamamlanınca ayrıca +15 fon.
  - Başarısızlık ya da geri çekilme sektörü değiştirmez.
  - **Kadro döner** (`ReturnSquad`, sonuçta kadronun kalan canı ve plakası `RaidResult.Hp/Armor`): ayaktaki uzmanın eksik
    canı yara olarak kalır; düşen uzman ya da yedek `liberationInjuryDays` (3) gün yatar ve sağlam kalkar; kırılan plaka
    sığınaktan düşer. Kalıcı kayıp yok. Teknisyen yatıp yedek de yoksa ekip kalkana kadar baskına çıkamaz.
  - Depo sınırına takılan ödül yalnız gerçekten alınan miktarla raporlanır (`LiberationRaidReward`).
- **Sonuç kartı** (baskın tahtasında, Revizyon 2 · S7a/b): tahta kararır, spot nedenin üstünde açılır, sağdan kart kayar:
  başlık yerinde tek damga (SEKTÖR GERİ ALINDI mavi; GERİ ÇEKİLDİ, EKİP DÜŞTÜ, İKMAL KAYBEDİLDİ, SÜRE DOLDU kırmızı), yer
  satırı (“Doğu Avrupa · 02 Tahliye geçidi”), hedef / kırılan sivil sunucu / ekip, tek düğme (0,6 s girdi kilitli).
- **Operasyon raporu** (S7c/d): baskından dönünce kamera harita pozuna geçer, rapor solda iner, iplik ekip figürüne gider.
  Başlık sonucu söyler (SEKTÖR GERİ ALINDI / SEKTÖR İŞGAL ALTINDA), altında tek cümle (bölgenin kalan sektörü ya da
  “Ekip geri döndü…”; Teknisyen yedeksiz düştüyse ya da güven azaldıysa onun uyarısı). Başarılıda KAZANÇ (FON, MALZEME,
  ERZAK, KURTULAN) ve sayaçlarda ± fişleri, sektör işareti maviye döner; başarısızda ödül alanı yok, YZ alarmı ölçekle.
  SONUÇLAR: sektör, kırılan sivil sunucu, ekip (düşen, yeni yara, kırılan plaka), sivil güven, YZ alarmı, karşı saldırı
  (`OperationReportBuilder`).

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

## Sığınağın kapısı (`Core/Door.cs`, `Core/DoorCards.cs`)

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

- **Masa (K1):** sayaç bankası (VARDİYA, FON, ERZAK, MALZEME, ÖZGÜR BÖLGE, GÜVEN), anahtar rayı (1 KOMUTA, 2 SIĞINAK,
  3 GÖREVLER, 4 KAPI, H KILAVUZ, Home MASA), vardiya şalteri, teleks, evrak tepsisi. Arka duvarda, mantar pano ile posterin
  arasında YZ ALARMI panosu (rafta yedinci sayaca yer yok: masa ve lamba sınırı).
- **Harita (K2, K3; Revizyon 2 · S2):** bölge seçilince görev fişi açılır: ekran katmanında sol masa kenarında (Kuzey ya
  da Güney Amerika seçiliyse sağda), kırmızı iple bölgeye bağlı. SAHA satırları (bölge özelliği, imza makinesi, tehdit
  kademesi; işaretleri haritadaki ve baskın tahtasındakilerle aynı), sektörler, bedel/ödül, ekip diskleri, baskın emri.
  Bölge tamamlanınca cephe onay fişi. Haritada cephe bloğu (sektör şeridi + yüzde + saha işaretleri), işaretler kutusunda
  TEHDİT n/4; karşı saldırının aldığı sektör geri alınana kadar ✕ ve YENİDEN İŞGAL damgasıyla işaretli. Ekip figürü
  haritada sürüklenebilir.
- **Sığınak maketi (K4; Revizyon 2 · S3):** odaların durumu (çalışıyor, DURDU, HASARLI), personeli ve geliştirme kararları;
  pano özetinde başta erzakın kaç vardiya yeteceği (⌊erzak ÷ son vardiyadaki düşüş⌋; 3 ve altı kırmızı), sonra TOPLULUK
  (yedek dahil), UZMANLAR (beş uzmanın diski, iki satırda), PLAKA. Maketin üstündeki DEPO bandı erzakı sandık, malzemeyi kova, plakaları rafta
  gösterir; odalar çıktılarını gösterir (ranzada yedek, telsizde yanan kadran, atölyede döküm kalıbı, revirde hasta).
- **CRT (K5):** telsizin göstergesi (TELSİZ · KANAL 07): dinlemede, hat açılıyor, hat açık, hat kapandı. Yalnız baskın
  emri kamerayı ekrana yaklaştırır. Teknisyen yatıp yedek de yoksa hat kapalı ("ALFA · TEKNİSYEN YARALI · 2 GÜN").
- **Görev fişi** kadroyu baskına çıkacağı gibi, baskın piyonlarının harfli ahşap diskleriyle yazar: T 3/3 · E 2/3 ·
  A 1/2 yedek · 2 plaka. Altındaki **KADRO** satırı seçicidir: beş uzman kadro sırasıyla (disk + can; T SABİT, yatanda
  "n GÜN" ya da "YEDEK ÇIKAR"); seçili olan sarı, seçilmeyenin diski soluk. Tıklanan uzman seçilir, iki seçili varken en
  eski seçimin yerine geçer; seçili olana tıklamak onu bırakır (ikiden az seçiliyken düğme kapalı, KADRO EKSİK). Üstüne
  gelince ad, can ve yeteneğin tam açıklaması. Seçim vardiya boyunca kalır (`SiginakMap.pick`); eğitimde kilitli. Eksik yalnız fonsa BASKIN EMRİ VER açık kalır; basınca komut çağrılmaz, FON sayacı titrer ve
  teleks gerekçeyi basar. Başka gerekçede düğme devre dışıdır, kısa nedeni sağında yazar (“EKİP HAZIR DEĞİL”).
- **Vardiya geçişi (K6, Revizyon 2 · S5):** şalter → sayaçlar döner → vardiya raporu (harita pozunda, solda 540 px):
  başta üç göz (ERZAK "n vardiya yeter" · YZ ALARMI ölçekle · FON önce → sonra), altında alarm notu; FON, ÜRETİM VE TÜKETİM
  (+ analist notu), EKİP, YAPILAMADI. Erzak ve alarm dökümde tekrar edilmez. Renk kuralı: artı yeşil, eksi mürekkep (eksi
  işaretiyle), ±0 gri; kırmızı yalnız alarm eşiği ve işgal. Sayaçların altındaki ± fişleri aynı kuralla. Rapor evrak
  tepsisine kayar; tepsiden son 5 rapor okunur.
- **Açılış (K7):** açılış dizisi ve emir belgesi; BAŞLA ile oda açılır. Kayıt varsa son rapor kısaca görünür.
- **Eğitim (rehberli ilk vardiyalar):** yeni harekâtta, eğitim daha önce bitirilmediyse ya da atlanmadıysa sağ altta
  analistin notu çıkar (`SiginakTutorial`, adımlar `TutorialFlow.Steps`). 12 adım: komuta odası (TAMAM) → görev fişi →
  baskın emri → taktik tahta → sonuç → operasyon raporu → sığınak → atölyeyi onar → personel ata → vardiya şalteri →
  vardiya raporu → kapanış (TAMAM). Her adım oyuncunun eylemini bekler; zaten yapılmış adımlar geçilir. Not açılış, emir
  belgesi, vardiya dizisi, ayarlar, CRT geçişi ve son rapor sırasında gizlenir. Masa pozunda (şalter sağ altta) ve baskın
  bitince (sonuç kartı sağ altta) not sağ üste çıkar.
- **Senaryolu ilk baskın (`TutorialRaid`):** eğitimin taktik tahta adımında açılan baskın harekâtın ilk baskınıysa (Batı
  Avrupa'nın ilk sektörü, kademe 1, tam ve sağlam kadro) sabit tohumla (129) kurulur ve oyuncu bilinen çözümü adım adım
  oynar: 16 hamle, 30 arayüz adımı (uzmanı seç, yürü, Q, hedefe tıkla, turu bitir), beş tur, temel makine aklı ve elle
  çizilmiş harita. Ders sırası: yürümek, EMP itişiyle zırhlı dronu duvara çarptırıp zırhını kırmak, kancayla dost uzmanı
  çekmek, makinelerin sırasını okumak (birbirini vururlar), atış hattından çıkmak, kasayı almak, EMP ile dönüş yolunu açmak
  (dronu duvara iter, Analisti kenara çeker), malzemeyi girişe taşımak; hasarsız zafer. Sıradaki kare tahtada nabız gibi atan sarı çift
  çerçeveyle işaretlenir; not "BASKIN · ADIM n/30" başlığıyla talimatı ve dersi yazar, makineler oynarken sırayı izlemeyi
  söyler. Yalnız sıradaki eyleme izin verilir: başka uzman, başka kare, geri alma, geri çekilme, Q'yu ya da seçimi Esc ile
  bırakma kapalı; boşta uzman kalınca tur bitirme onayı sorulmaz. Beklenen hamle tahtada yapılamazsa (olmamalı) rehber
  çekilir, baskın serbest oynanır. Simülasyon yalnız baskının sonucunu gördüğü için kayıt bozulmaz.
  "Eğitimi atla" ya da kapanış eğitimi bitirir (PlayerPrefs `Corrigible.TutorialDone`; editörde Corrigible › Eğitim ›
  Eğitimi sıfırla). Kayıttan devam eden oyunda eğitim yoktur. Simülasyona dokunmaz.
- **Kapı (K8, Revizyon 2 · S4):** sağ duvarda çarklı çelik kapı: üstünde tehlike bandı ve kafesli vuruş lambası, yanında
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
sonuç damgası (Silme: KAZANILDI yeşil; Dağılma, Vuruldu, Sızma: KAYBEDİLDİ kırmızı + küçük neden damgası), sonucu söyleyen
cümle (kazanınca altında “AĞA BAĞLANMA · ANALOG KAL”, kayıpta sonun metni), dört gösterge (SÜRE vardiya; BASKIN başarılı ·
başarısız; ÖZGÜR BÖLGE x/12 + sektör; SIĞINAK NÜFUSU, kayıpta kırmızı “kişi kaldı”), işgal şeridi (x = vardiya 1…N her
zaman tam genişlik; kırmızı işgal, mavi geri alınan; karşı saldırılar yukarı çıkıntı) ve üstünde numaralı kırılma anları,
altta aynı numarayla satırlar (en çok altı: ilk özgür bölge, karşı saldırılar, öteki özgür bölgeler, Dağılma'da son güven
düşüşü, son olay: son sunucu / güvenin sıfırlanması / kapının açılması), **YENİDEN BAŞLA** (Enter). İşaretin ya da satırın
üzerine gelince ikisi birlikte seçilir (`FinalReportBuilder`, `FinalReportView`).

## Kayıt ve kalıcılık

- Kayıt = tohum + denge değerleri + **komut geçmişi** (`GameRecord`, biçim 12: beş uzmanlı kadro ve baskın öncesi seçim); devam, komutları
  yeniden oynatır (`Replay.Restore`). Baskın sonucu kadronun kalan canını, plakasını ve ayakta kalan sivilleri de taşır
  (`RaidResult.Encode`); kapı kararları `door_answer` komutudur. Her başarılı komutta ve uygulama kapanırken kaydedilir;
  harekât bitince silinir. Biçim 11 ve öncesi kayıtlar yüklenmez.
- Dosyalar `Application.persistentDataPath` altında: `liberation_savegame.json` (oyun) ve kayda girmeyen sunum durumu
  için `liberation_desk.json` (teleks satırları, evrak tepsisindeki son 5 vardiya raporu, kamera pozu; tohum
  eşleşmezse yok sayılır).

## Başarımlar (Steam)

`Game/Platform.cs → Achievements`: yalnız `ACH_ERADICATION` (Silme sonu).
