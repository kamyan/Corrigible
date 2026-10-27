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
- **Son:** Bütün 36 sektör geri alınınca **Silme** (`"silme"`): "Çekirdek yok edildi, kopyalar temizlendi." Tek son budur.
- **Makineler kopyalanır:** `liberationCopyFromRegion` (2) bölge özgürleşince baskınlarda `"kopya"` özelliği açılır;
  her tur iki makine uyanır. Son bölgenin sunucu merkezinde YZ çekirdeği vardır (`IsCore`).

## Vardiya (`Simulation.Step`)

Oyuncu vardiya şalterini indirince (`GameController.AdvanceShift`) bir gün geçer:

1. **Gelir:** `liberationDailyFunds` (2) + özgür bölge başına `liberationRegionIncome` (0,5) fon (`IncomeBreakdown`).
   Her fon değişimi `TransactionLedger`'a yazılır.
2. **Ekipler:** baskın meşguliyeti ve yaralılık günleri birer azalır.
3. **Sığınak:** üretim, tüketim, enerji ve revir (`ShelterLogic.Advance`, aşağıda).
4. **Kayıt:** son rapordaki şerit grafik için işgal oranı (`S.Hist`).

Baskın sürerken (`S.Raid`), harekât bitmişken ya da açılış/emir belgesi açıkken vardiya bitirilemez.

## Baskın

- **Emir** (`StartLiberationRaid`): yalnız mevcut bölgenin sıradaki sektörüne, hazır bir ekip ve `liberationRaidCost`
  (12) fonla. Tehdit `liberationRaidThreat + özgürBölge × liberationRaidThreatPerRegion` (30 + 5/bölge). Kamera CRT'ye
  dalar ve `Baskin` sahnesi odanın üstüne yüklenir.
- **Taktik tahta** (`Core/Baskin.cs`, 8×8, 6 tur): oyuncu birimleri **Teknisyen** (sabotaj), **EMP uzmanı** (EMP atışı,
  iter), **Analist** (kanca); makineler **güvenlik dronu** ve **bakım robotu** (hedef dolapları onarır). Kopya dolapları
  her tur makine uyandırır. Makinelerin bu turdaki hamlesi kırmızı okla, sırası bayrakla görünür. Hamleler geri
  alınabilir (`Undo`), önizleme durumu değiştirmez. Kırılan sivil sunucular rapora geçer.
  - **İkmal:** Teknisyen ikmal kasasına bitişik kareden yeteneğiyle malzemeyi alır; kasayı kırmak görevi kaybettirir.
  - **Sunucu:** tur bitmeden bütün hedef dolaplar kırılır.
- **Sonuç** (`ResolveRaid`, taktik sonuçtan masaya dönülürken bir kez):
  - İkmal başarısı: +22 fon, +10 malzeme, +10 erzak, yer varsa +1 kurtulan.
  - Sunucu başarısı: +10 fon, +6 malzeme.
  - Bölge tamamlanınca ayrıca +30 fon.
  - Başarısızlık ya da geri çekilme sektörü değiştirmez. Düşen her üye ekibi `liberationInjuryDays` (2) gün yaralar;
    ekip kalıcı silinmez.
  - Depo sınırına takılan ödül yalnız gerçekten alınan miktarla raporlanır (`LiberationRaidReward`).
- **Operasyon raporu:** masaya dönünce hedef, sonuç, ödüller ve bölge durumu bir kâğıtta açılır (`SimDeskBuilder.Report`).

## Sığınak (`ShelterState`, `ShelterLogic`, `Simulation.Shelter.cs`)

Sığınak 6 kişi, 24 erzak ve 14 malzemeyle başlar. Yaşam alanı ve jeneratör seviye 1; atölye ve revir onarılmayı
bekler. Odalar en fazla seviye 3 olur; onarım/geliştirme (`ShelterUpgrade`) fon ve malzeme ister, görevlendirme
(`ShelterAssign`) değiştirilene kadar sürer. Oda başına personel kapasitesi `2 × seviye`dir.

| Oda | Rol | Onarım/geliştirme |
|---|---|---|
| Yaşam alanı | Seviye başına 8 kişilik yer ve 40 erzak deposu; görevli başına vardiyada 1 erzak toplama | 8 fon + 6 malzeme |
| Jeneratör | Seviye başına 4, görevli başına 1 enerji; atölye ve revir seviye başına 2 enerji kullanır | 6 fon + 4 malzeme |
| Atölye | Enerji yeterliyse görevli başına vardiyada 0,5 malzeme (depo 100) | 10 fon + 6 malzeme |
| Revir | Enerji ve erzak varsa, en az 1 görevliyle yaralı ekibin toparlanmasını vardiyada 1 gün hızlandırır | 12 fon + 8 malzeme |

Her kişi vardiyada 0,2 erzak tüketir. Erzak biterse atölye yarı verimle çalışır ve revir durur; yaşam alanında toplama
sürer. Bütün sayılar `Balance.shelter*` alanlarındadır.

## Komuta odası (sunum)

- **Masa (K1):** sayaç bankası (VARDİYA, FON, ERZAK, MALZEME, ÖZGÜR BÖLGE), anahtar rayı (1 KOMUTA, 2 SIĞINAK,
  3 GÖREVLER, H KILAVUZ, Home MASA), vardiya şalteri, teleks, evrak tepsisi.
- **Harita (K2, K3):** bölge seçilince görev fişi (sektörler, bedel/ödül, baskın emri); bölge tamamlanınca cephe onay fişi.
  Ekip figürü haritada sürüklenebilir.
- **Sığınak maketi (K4):** odaların durumu, personeli ve geliştirme kararları.
- **CRT (K5):** saha yayını; yalnız baskın emri kamerayı monitöre yaklaştırır.
- **Vardiya geçişi (K6):** şalter → sayaçlar döner → vardiya raporu; rapor evrak tepsisine kayar.
- **Açılış (K7):** açılış dizisi ve emir belgesi; BAŞLA ile oda açılır. Kayıt varsa son rapor kısaca görünür.
- Komutların gerekçesi ve sonucu teleksten basılır (`TelexReasons`). Ayrıntı: `Docs/UI_Siginak/IMPLEMENTATION_MAP.md`.

## Son rapor

Harekât bitince tam ekran son rapor açılır: sonuç damgası, dört gösterge (harekât süresi, baskın sayısı, özgür bölge,
sığınak nüfusu), işgal oranı şerit grafiği, kırılma anları (geri alınan sektörler ve sonuç) ve **Yeniden başla**.

## Kayıt ve kalıcılık

- Kayıt = tohum + denge değerleri + **komut geçmişi** (`GameRecord`, biçim 4); devam, komutları yeniden oynatır
  (`Replay.Restore`). Her başarılı komutta ve uygulama kapanırken kaydedilir; harekât bitince silinir.
  Eski (kriz oyunu dönemi) kayıtlar yüklenmez.
- Dosyalar `Application.persistentDataPath` altında: `liberation_savegame.json` (oyun) ve kayda girmeyen sunum durumu
  için `liberation_desk.json` (teleks satırları, evrak tepsisindeki son 5 vardiya raporu, kamera pozu; tohum
  eşleşmezse yok sayılır).

## Başarımlar (Steam)

`Game/Platform.cs → Achievements`: yalnız `ACH_ERADICATION` (Silme sonu).
