# DengeBotu: KOS harekât botu

KOS aşama 8'in denge ölçümünü yeniden üretmek için konsol programı. Gerçek `Corrigible.Core` ve
`Assets/Corrigible/Tests/EditMode/RaidBot.cs` ile, Unity olmadan Mono'da koşar. Oyun koduna girmez (Assets dışında; Unity
derlemez). Sonuçlar `Docs/KABUL-OLAYLAR-VE-SONLAR-TASARIMI.md` §8 "Ölçüm sonuçları" ve `Docs/DURUM.md` › KOS aşama 8'de.

## Tek kip (kayıt 21)

Tesis baskını oyunun tek baskın kipidir (`Docs/TESIS-BASKINLARI-TASARIMI.md` §12 aşama 18, §14 yirmi birinci tur). Camp8 her
baskını üç odalı tesis zinciriyle oynar; `BAL` vermeyen koşu da tesistir ve sahadaki sızmacı açıktır (`infiltratorFieldEnabled`
C# varsayılanı `true`). Eski tek tahta dalı ve `RaidBotMeasurement*` silindi.

- **`ApplyBal` emekli alanları:** `raidFacilityEnabled=true` sessizce yok sayılır, bu yüzden aşağıdaki eski `tesisN.txt` satırları
  ve `Library/Calisma` kilit betikleri değişmeden koşar. `raidFacilityEnabled=false`, `liberationLootExtraTurns` ve
  `liberationServerExtraTurns` "emekli alan: … (tek tahta kaldırıldı, kayıt 21)" hatasıyla durur.
- **Tarihsel listeler:** `asama8.txt` ve `tesis0.txt` her satırda açık `raidFacilityEnabled=false` taşır (ölçüldükleri günün
  varsayılanı); artık "emekli alan"la dururlar. Yapılandırma dosyaları `cekirdek6.txt`, `yolla4.txt`, `usta1.txt` notsuz kaldı
  (`kos.sh` `#` satırını da iş sayar). Aşağıdaki "Kullanım" ve aşama bölümleri o günlerin komutlarıdır.
- **Saha kapalı etiketler:** `tesis4`–`tesis17`'de sızmacısız ölçülmüş koşular BAL'e `;infiltratorFieldEnabled=false` ekler
  (t13K, t14K, t15K ve t17 ana satırları dahil); `t15` ve `t17saha` `=true` yazar. `tesis17.txt`'teki `t17g5` (g5'te tek tahta)
  çıktı. Aşağıdaki eski komut örneklerinde yalnız `raidFacilityEnabled=true` yazan bir BAL artık saha açık koşar; o günün
  sonucunu yeniden üretmek için BAL'e `;infiltratorFieldEnabled=false` ekle.
- **Bayt kilidi:** `Library/Calisma/taban21/kilit21.sh <camp8.exe> <çıktı>` değişiklikten önce yakalanan `taban21/base` ile
  `cmp` eder: A (`liberationMapGen=1;…;infiltratorFieldEnabled=false`, g5 saha kapalı), B (g5 saha açık; yeni oyun), C
  (`liberationMapGen=0`, sabit dünya); `iyi3.txt` politikaları, tohum 1–10, 44, 54, `REPLAY=1`; ayrıca t13K/t14K/t15K alt bayrak
  kilitleri ve saha açık replay (124 iş, 10 paralel ~8 dk). Tek kipten sonraki regresyon tabanı B varyantıdır; tek kip denge turundan
  (22 Kasım 2026) beri yerini `taban22` aldı (aşağıda "Tek kip denge turu"). `taban6`/`taban13`
  altındaki `pre_dunya`, `pre_gen`, `pre_gen2` (bayrak kapalı tek tahta) ve aşama 13–17 kilitlerinin ilk blokları emekli.

## Tek kip denge turu (`denge22.txt`)

> **Kayıt 22 (24 Kasım 2026):** yatmayan herkes vardiya kapanışında iyileşir (`boardDutyHeal` varsayılanı 1; önce yalnız Boşta).
> Bu tarihten önceki satırları (d22/t22 dahil) ve `taban22` bayt kilidini yeni exe ile yeniden üretmek için BAL'e
> `;boardDutyHeal=0` ekle; yeni ölçümler varsayılanla (1) koşar. Aynı gün tesis tur sayıları 6/6/10 (dönüş 6) ve istihbarat
> bedeli 1 oldu; eski satırlar bunları da BAL ile eski değerlerine çekmeli.

22 Kasım 2026'da beş `Balance` varsayılanı değişti (`liberationCivilianHp` 3, `facilityTargetTurns` 9, `candidateEmergencyBelow` 4,
`liberationTier3At` 9, `liberationAlarmPerShift` 0,5; sonuç `Docs/TESIS-BASKINLARI-TASARIMI.md` §11.6 "Tek kip denge ölçümü").
`denge22.txt`'teki tarama etiketleri (`d22…`) o günkü eski beş değeri BAL'in başında açıkça yazar (`ApplyBal` sırayla uygular, sonraki
kazanır), bu yüzden yeni exe ile de aynı koşuyu verir; `t22taban` eski denge, `t22` uygulanan denge. Bu tarihten önceki `tesisN.txt`
satırları BAL'de bu beş alanı yazmaz: yeni exe ile o günün sayısını vermez, eski beş değeri BAL'e eklemek gerekir.

```bash
Tools/DengeBotu/derle.sh
export PICK=rastgele RETIRE=1 DEV=kir DUSEN=hep ODUL=tarif ARAC=donus SAVUN=hep CEKIL=hic AYIR=hep KENAR=hic
REPLAY=1 Tools/DengeBotu/kos.sh <(grep -E '^t22(taban)?\|' Tools/DengeBotu/denge22.txt)
```

- **Bayt kilidi:** `taban21` eski dengenin tabanıdır (A ve B artık ayrışır). Yeni regresyon tabanı `Library/Calisma/taban22/kilit22.sh`
  (`taban22/base`, aynı 124 koşu; aşama 13–15 özgün dosya karşılaştırması çıkarıldı). İkisi de git dışıdır.


## Kullanım

```bash
Tools/DengeBotu/derle.sh                         # Library/DengeBotu/camp8.exe (Core değişince yeniden derle)
PAR=8 Tools/DengeBotu/kos.sh Tools/DengeBotu/asama8.txt   # aşama 8 son ölçümü (tarihsel: kayıt 21'de "emekli alan"la durur)
python3 -I Tools/DengeBotu/ozet.py son sonY tabanR c375 c425 usta
```

Tek koşu: `RETIRE=1 BAL="eventGapMax=2" mono Library/DengeBotu/camp8.exe 1 10 karisik ucuz hic etiket` (tohum 1–10).
Satır başına bir harekât yazar (`C\t` + `anahtar=değer`, sekmeyle).

- **Kapı:** `hic` (kimseyi alma), `kefilli` (yalnız kademe 0), `comert` (en çok özellikli), `karisik` (kadro 6'nın altındaysa en
  düşük kademe, değilse kefilli), `alyolla` (cömert al, dolu sığınakta bu politikayla alınmış en eskiyi yolla).
- **Olay:** `yoksay` (güvenli satır), `tecrit` (tecrit/sorgu), `yolla` (yolla/sök), `ucuz` (en ucuz: eşiğe vardırmıyorsa izi
  görmezden gel, sabotajda sök, konum çözülmüşse sorgu).
- **Şalter:** `hic`, `salter` (konum ya da kadro nedeninde çek), `durgun` (ayrıca 30. vardiyadan sonra 12 vardiya ilerleme yoksa).
- **"İyi" küme:** karışık+tecrit, karışık+ucuz, kefilli+ucuz (şalter çekilmez).
- **Ortam:** `RETIRE=1` (varsayılan `kos.sh`'ta 1) sunumun kuralı: yayındaki panoda yuvası olmayan görevler (`care`, `gather`,
  `watch`, `contact`) Boşta'ya iner, bot onlara kimseyi atamaz (`PlanningBoardPresenter.RetireJobs`). **Ölçümler bununla
  yapılır**; `RETIRE=0` ilk ölçümün toplayıcılı botudur ve tüketimi yanlış ölçer (aşama 8'de yaşandı). `BAL="alan=değer;…"`
  `Balance`'ı değiştirir; `ALT=n` RaidBot'un seçenek sayısı (varsayılan 8; `usta` koşusu 16).
- **Üretilmiş harita:** `BAL="liberationMapGen=1"` her tohumda üretilmiş haritayla oynar (0 dünya haritası). `PICK=rastgele`
  sınırdan bot akışıyla rastgele cephe seçer (varsayılan `sira`: paket sırasıyla ilk; bu iki haritada da çoğu tohumda aynı
  paket sırasını verir, harita etkisini ölçmek için `rastgele` kullanılır). Satıra `mapgen`, `pick`, `map` (parmak izi),
  `mapAttempt`, `continents`, `links`, `pkg` (`paket:seçilme sırası:ilk baskının kademesi:başarısız baskın`) ve `pkgDist`
  (paketin karargâhtan çizge uzaklığı) yazılır. `ozet.py --pkg` paket × kademe tablosunu ve tohum başına zafer yayılımını basar.

## Baskın satırları (tesis baskınları aşama 0)

Camp8 her baskın için ayrıca bir `R\t` satırı yazar (`RAIDS=0` kapatır): koşu anahtarları, `day`, `kind` (loot/server),
`tier`, `region`, `ok`, `retreat`, `turns` (sonuç turu), `max` (tur bütçesi), `plTurns` (oyuncu turu), `moves` ve `acts`
(uzman yürüyüşü ve yeteneği; planlanan her adım uygulanır, sayım tamdır), `down` (`rescued` + `died`),
`rescued` (bugün kendiliğinden kurtarılan düşen: tesis kipinde taşınmazsa ölür), `died`, `collat`, `again` (karşı
saldırıdan sonra yeniden alınan sektör), `event`, `end` (zafer, cekilme, sure, tasiyici: ikmal kaçışında taşıyıcı düştü, ekip: bütün ekip düştü, diger: ikmal
kasası imha edildi). Harekât satırına aynı
sayaçların toplamı ve `recapRaids`, `recapSectors`, `recapRegions`, `recapFunds`, `recapTrust` (yeniden tamamlanan bölgenin
bugün yeniden verdiği fon ve güven; güven tavanla kırpılmış etkin paydır) eklenir. Sayaçlar `RaidBot.OnTurn` kancasından okunur; oyun kodu değişmez.

```bash
PICK=rastgele PAR=10 Tools/DengeBotu/kos.sh <(grep '^t0gen' Tools/DengeBotu/tesis0.txt)
PICK=sira PAR=10 Tools/DengeBotu/kos.sh <(grep '^t0dunya' Tools/DengeBotu/tesis0.txt)
python3 -I Tools/DengeBotu/ozet.py --baskin t0gen t0dunya
```

Sonuçlar `Docs/TESIS-BASKINLARI-TASARIMI.md` §11.6 "Bot ölçümü" tablosunun "Taban" sütununda.

## Tesis baskınları (aşama 2)

Baskınlar üç odalı tesis zinciriyle oynanır (aşama 2–17'de `BAL="raidFacilityEnabled=true"` ile açılırdı; kayıt 21'den beri
tek kip): bot (`RaidBot.PlayFacility`) odayı açar (`Simulation.OpenRaidRoom`), oynar, raporu dosyalar (`AdvanceRaidRoom`); sonuç oda
raporlarından toplanır (`ResolveRaid`).

- **`DEV=kir|hic|hep`**: botun donanım politikası (`RaidBot.Devices`, varsayılan `kir`). `kir` giriş ve ara odada donanımı
  kırıp ileri geçide varmak süreye bir tur payla sığıyorsa Teknisyen'e kırma hedefi verir, `hep` donanım sağlamsa her zaman,
  `hic` donanıma hiç dokunmaz (sabotaj, EMP, dinamit hedefi olmaz). Hedef odaya girişte bir kez seçilir; `kir` süreye sığmaz olunca turun başında bırakır (R satırındaki `goals` ilk seçimi sayar). Eski tahtada etkisiz.
- **`REPLAY=1`**: her harekâtın sonunda kayıt baştan yeniden oynatılır (`Replay.Restore`, kayıt nesnesiyle; Mono'da
  JsonUtility yok). Başarısız komut olmamalı ve gün, fon, güven, alarm, RNG, baskın günlüğü (başarı, oda raporları) ve kişilerin
  canı/durumu eşit olmalı; değilse istisna. `C` satırına `replay=ok` eklenir.
- **`R` satırı** (yalnız tesiste ekler; eski anahtarlar aynı, `turns` ve `max` odaların toplamı):

| Anahtar | Anlam |
|---|---|
| `rooms` | Oynanan oda sayısı (dönüş dahil; yürünerek geçilen temiz oda sayılmaz) |
| `rt`, `rmv`, `rac` | Oda başına tur, yürüyüş, yetenek: `G5/A4/H7/a3` (büyük harf gidiş, küçük dönüş) |
| `walked` | Yürünerek geçilen temiz oda |
| `devG`, `devA`, `devK` | Giriş ve ara odanın donanımı kırıldı mı (0/1); kırılan türler (`pano+anten`, yoksa `-`) |
| `devHits`, `goals` | Botun donanım karesine eylemi; donanım hedefi seçilen oda sayısı |
| `stuck` | Son raporda geride kalan kişi (`l`; dönüşte süre doldu) |
| `part` | Ara odanın kepengi bölme kepengi mi (0/1; aşama 2'de kepenk hep kapalı) |
| `endRoom` | Baskının bittiği oda: `G`, `A`, `H`, `a`, `g`; yürüyerek çıkışta `x` |
| `dev` | Politika |

- **`end`** sırası: `zafer`, `sure`, `cekilme`, `tasiyici`, `diger`, `ekip`. Tesisin giriş ve ara odasında süre dolunca ekip
  geri çekilmiş sayılır (sonuçta `retreat=1`); bu yüzden `sure` `cekilme`'den önce sınanır. Tesiste `end` son odanın tahtasından
  okunur. Eski tahtada geri çekilme anlıktır: iki koşul hiç birlikte doğru olmaz, sıra çıktıyı değiştirmez.
- **`C` satırı** (yalnız tesiste): `fRaids`, `fRooms`, `fWalked`, `fDevHits`, `fGoals`, `fDevBroken` (kırılan donanımlı rapor),
  `fStuck` ve `fEnd_<oda>` (`endRoom` dağılımı) toplamları.

```bash
# Aşama 2 doğrulaması: "iyi" küme, tohum 1–10; istisnasız biter, her C satırında replay=ok
while read door ev sw; do
  RETIRE=1 REPLAY=1 BAL="raidFacilityEnabled=true" mono Library/DengeBotu/camp8.exe 1 10 $door $ev $sw tesis2
done < Tools/DengeBotu/iyi3.txt > tesis2.txt
DEV=hic RETIRE=1 BAL="raidFacilityEnabled=true" mono Library/DengeBotu/camp8.exe 1 10 karisik ucuz hic tesis2hic
```

İlk doğrulama koşusunda (aşama 2, `ALT=8`) baskın başına süre eski tahtayla yakındı (~0,4 s; baskınların üçte biri ara odada
bitti); bütün odalar oynandıkça artar, kısa koşularda `ALT` küçültülür. Aşama 4'te tesis puanı düzeltildi: kişinin kartındaki yürüyüş (`Mv`; eski puan Teknisyen'i 3
sayıyordu), bu tur yürümemiş uzmana bir tur fazla (`TurnsFor`), giriş ve ara odada geç kalan her uzman bedelli
(`W.GateLate`, `W.GateMiss`; ölçümde etkisi küçük), son turda geçide uzaklık puana girer, `kir` süreye sığmaz olan donanım
hedefini turun başında bırakır (`ReviewDeviceGoal`). Bayrak kapalıyken çıktı bayt bayt aynı. Bilinen sınır: planı seçerken
kopyada oynatılan makine evresi RNG tüketir; bot yeni uyanan makineyi önceden görür (iki tahtada da).

## Tesis ölçümü (aşama 4): `ozet.py --tesis`

```bash
# 80 tohum, "iyi" küme; DEV ortamla verilir (BAL.txt'e yazılmaz, R satırının dev anahtarında görünür)
PICK=rastgele PAR=4 Tools/DengeBotu/kos.sh <liste>   # satır: t4|liberationMapGen=1;raidFacilityEnabled=true|8|iyi3.txt|1-10 … 71-80
python3 -I Tools/DengeBotu/ozet.py --tesis t4 [t4hic …] [--taban t0gen]
python3 -I Tools/DengeBotu/ozet.py --dir <OUT>/out --tesis t4 --taban -   # başka çıktı klasörü; tabansız
```

Her etiket bir sütundur ("iyi" küme; kümenin dışındaki satırlar atlanır ve sayısı yazılır). `--taban` varsayılanı `t0gen`
(klasör varsa); taban sütunu ve köşeli parantezde tabanla fark (`p`: yüzde puanı) yalnız ortak ölçülerde basılır. İlk bölüm
`Docs/TESIS-BASKINLARI-TASARIMI.md` §11.6 "Bot ölçümü" tablosunun ölçülebilen satırlarıdır (hedef sütunuyla): baskın başına
toplam tur (medyan, ort, p90, bütçe), uzman hamlesi (yürüyüş, yetenek, toplam; tabana oranı ≤ 2,2), görev ve kademeye göre
başarı, hazırlıksız (`devG=0`, `devA=0`) ve iki donanımı kırık baskının başarısı ve farkı, harekât başına ölü, kendiliğinden
kurtarılan düşen, baskın başına kırılan sivil, zafer oranı (binom ve tohum kümeli standart hata, tabanla aynı tohumlarda eşli
fark), zafer uzunluğu, dağılma oranı ve günü. İkinci bölüm tesise özgüdür (tesis anahtarı olmayan tabanda `-`): DEV
politikası, bitiş nedeni ve odası, oda başına tur/yürüyüş/yetenek (`rt`/`rmv`/`rac`), süre dolan baskının odası ve kepenk
(`part`) payı, geride kalan (`stuck`), yürünen temiz oda (`walked`), donanım kırılma oranları (`devG`, `devA`, `devK`,
`devHits`, `goals`). Eski seçeneklerin (`--baskin`, `--pkg`, düz) çıktısı değişmedi.

Okurken: bot donanımı kendi kararıyla kırar; "iki kırık" ile "hazırlıksız" baskınlar aynı koşulda değildir (seçim etkisi).
Tohum kümeli hata yapılandırmaların aynı tohumda benzer yol izlemesini hesaba katar; karar için 80 tohum kullanılır.

Aşama 4 ölçümü `tesis4.txt` listesidir (komutlar dosyanın başında; DEV her politika için ayrı koşulur, `grep '^t4kir'`
bütün `kir` varyantlarını seçer). Sonuçları `Docs/TESIS-BASKINLARI-TASARIMI.md` §11.6 "Aşama 4 ölçümü". Eşli karşılaştırma
için ilk baskın kullanılır (aynı kadro ve harita; üç yapılandırma ilk baskınları aynı oynar, etkin örneklem 80 tohum).
"Öbür sonlar" satırı adettir ve `topluluk`'u da sayar.

## Tesis hamleleri ve düşen (aşama 6): `DUSEN`, `tesis6.txt`

Bot tesis odasında yeni hamleleri de dener (`RaidBot.Step.Kind`, Core'un `Baskin.ActionTargets`/`DoAction`'ı; eski tahtada
hiç üretilmez): pusu, iki kişilik iş ve fırlatma yeteneğin yerine (önce yürüyebilir), koşu darbesi, sürükleme ve menfez yalnız
yürümeden (menfezden sonra yetenek de olabilir). Pusu yalnız hattı bu tur bir makinenin varabileceği bir kareden geçen yönde
denenir; planın doğrulama kopyasında pusu hatları silinir (bot pusu vuruşunu önceden görmesin), değeri kaba beklentiyle
(`W.Ambush`) eklenir. Tesiste taşıyıcının düşmesi görevi bitirmez (yük yere düşer, `W.CargoDrop`); ikmal kaçışında tam ağırlıklı
yol Teknisyen'in değil yükün (taşıyıcının ya da yerdeki yükün) yoludur. Açılmış büyük sandık `W.Crate`, tutulan iş (ikinci uzman
bu tur gelebiliyorsa) `W.Hold` / `W.Crate/2`.

- **`DUSEN=hep|hic`** (varsayılan `hep`): botun düşen politikası (`RaidBot.Fallen`). `hep` kaçışta ve hedef odasında yatan
  gövdeyi kapıya sürükler: puanda gövdenin kapıya kalan yolu (`W.BodyShare`), süreye sığmayan gövde `W.Abandon`; turun başında
  kurtarılabilen gövde yatarken GEÇ ve tesisin girişinden son çıkış kazanç sayılmaz; turun başında kapı karesinde yanında gövde
  olan uzman kendiliğinden kapıya girmez. Gidişin giriş ve ara odasında yatan gövdeye dokunmaz (dönüşte yeniden tahtaya konur ya
  da oda temizse toplanır). `hic` sürükleme adayı üretmez. Temiz odadan toplanma kuraldır, politika dışıdır. Eski tahtada etkisiz;
  bayrak kapalıyken çıktı bayt bayt aynı.
- **`R` satırı** (yalnız tesiste, `dev`'in ardından): `dusen`; `fallen` (son raporda düşen, `l` hariç), `carried` (sürüklenerek
  çıkarılan: `Body 2`, `Picked 0`), `collected` (dönüşte temiz odadan toplanıp çıkarılan: `Body 2`, `Picked 1`), `abandoned`
  (taşınmayan, öldü); `ax=pusu:n/kosu:n/surukle:n/firlat:n/ortak:n/menfez:n` (eylem kullanımı), `amb` (pusu isabeti), `shut`
  (kepenk açıldı, 0/1), `crate` (büyük sandık açıldı, 0/1), `cdrop`/`cpick` (yük yere düştü / yerden alındı). `rescued` tesiste
  artık tam olarak `carried + collected`'tır. `moves`/`acts` sayımı: pusu, fırlatma, iki kişilik iş ve koşu darbesi yetenek;
  sürükleme ve menfez yürüyüş. `end=tasiyici` tesiste "yük odada kaldı" anlamına da gelir.
- **`C` satırı** (yalnız tesiste): `fFallen`, `fCarried`, `fCollected`, `fAbandoned`.
- **`ozet.py --tesis`**: "aşama 6" bölümü (politika, düşen/taşınan/toplanan/taşınmayan, (taşınan + toplanan) / harekât ÷ tabanın
  kurtarılanı / harekât, / düşen, eylem kullanımı, pusu isabeti, kepenk ve sandık açılma, yükün düşmesi, bütün ekibin düştüğü
  baskın). Eski "kurtarılan / düşen" satırının paydasında geride kalan da vardır.

```bash
# 80 tohum, "iyi" küme. Taban: aşama 6'dan önce derlenmiş exe (aşama 5 sonu), aynı tohumlar
OUT=<aşama5 exe klasörü> PICK=rastgele PAR=10 DEV=kir Tools/DengeBotu/kos.sh <(grep '^t6taban' Tools/DengeBotu/tesis6.txt)
PICK=rastgele PAR=10 DEV=kir DUSEN=hep Tools/DengeBotu/kos.sh <(grep -E '^t6hep(D0)?\|' Tools/DengeBotu/tesis6.txt)
PICK=rastgele PAR=10 DEV=kir DUSEN=hic Tools/DengeBotu/kos.sh <(grep '^t6hic' Tools/DengeBotu/tesis6.txt)
python3 -I Tools/DengeBotu/ozet.py --tesis t6hep t6hepD0 t6hic --taban t6taban
```

**Kapanış (son nefes kuralı, düzeltilmiş sayaçlar):** R satırında ayrıca `breath` (oda başına son nefes gövdesi), `bsaved` (son
nefesle çıkarılan: çıkarıldığı son tahtasında `Unit.Breath`; `carried`'ın alt kümesi, sürükleyerek çıkarılan `carried − bsaved`)
ve `fate` (son raporda düşen ve geride kalan her kişinin sonu, kadro sırasıyla: kategori + düştüğü oda + gövdenin tahtada son
göründüğü oda; kategori `c` sürüklenerek çıkarıldı, `n` son nefesle çıkarıldı, `k` temiz odadan toplandı, `x` baskın kaybedildi,
`b` ayaktakiler varken bırakıldı, `h` o odada ayakta kimse kalmadı (hepsi düştü ya da geride kaldı), `l` geride kaldı; oda G/A/H gidiş, g/a dönüş). Yalnız tesis
satırında; eski kiplerde çıktı değişmez. `ozet.py --tesis` bunları "aşama 6" bölümünde gösterir (anahtar yoksa "-").
`facilityLastBreathDies=true` kuralı kapatır (ölçüm anahtarı). Bilgi satırları: `t6snP0` pusu kapalı
(`facilityAmbushRange=0`), `t6snD0` sürükleme bedelsiz ("Sedye" üst sınırı).

```bash
PICK=rastgele PAR=10 DEV=kir DUSEN=hep Tools/DengeBotu/kos.sh <(grep -E '^t6sn0?\|' Tools/DengeBotu/tesis6.txt)
PICK=rastgele PAR=10 DEV=kir DUSEN=hic Tools/DengeBotu/kos.sh <(grep '^t6snhic' Tools/DengeBotu/tesis6.txt)
python3 -I Tools/DengeBotu/ozet.py --tesis t6sn t6sn0 t6snhic --taban t6taban
```

Ağırlık taraması (tohum 1–20, "iyi" küme, `hep`, 60 harekât): `BodyShare` 0,5 / 1 / 2 taşınanı 1,25 / 1,40 / 1,32, `Abandon`
1000 (BodyShare 2) 1,28 / harekât verdi (gürültü içinde; varsayılan 1 ve 300). Tesisin girişinden son çıkışın da GEÇ gibi
sayılması (gövde yatarken kazanç değil) 1,62'ye çıkardı. Sonuç ağırlıktan çok durumdan gelir: tesiste düşenlerin çoğu ya baskın
kaybedildiği için ya da son ayaktaki uzman makine evresinde düşüp oda kendiliğinden geçildiği (kapıdakiler geçer) için ölür; o
gövde hiçbir turun başında tahtada yatmaz. 80 tohum sonuçları `Docs/TESIS-BASKINLARI-TASARIMI.md` §11.6 "Aşama 6 ölçümü".

## Tesis ekipmanı ve ödül (aşama 8): `ODUL`, `ARAC`, `GEAR`, `tesis8.txt`

Vardiya planı ortak `Tests/EditMode/CampaignBot.cs`'tedir (Camp8 ve `FacilityBotTests` aynı sınıfı çağırır; `derle.sh` onu da
derler). Bayrak kapalıyken açık tarif ve ödül olmadığı için yeni hiçbir komut verilmez: `pre_dunya` ve `pre_gen` bayt aynı (tek tahta
dönemi; kayıt 21'de koşular kalktı, tabanlar emekli).

- **`ODUL=tarif|kazanim`** (varsayılan `tarif`): bekleyen sunucu ödülü baskın emrini kilitlediği için baskın sonucundan hemen
  sonra ve vardiya başında seçilir. Cevap paketinde de "tarif" (K22): "hep tarif" kazanımsız kalır.
- **`ARAC=yok|tek|donus`** (varsayılan `donus`): kit adımından sonra, yalnız emir yokken, hiçbir rol kiti eksik değilken ve depoda
  yer varken, bütün girdiler serbest hammaddeden karşılanıyorsa açılan ekipman tarifinin iş emri (`board_queue`, girdiler hemen tam
  `board_reserve`: eksik ayrılmış taslak `BoardPlanCheck`'te fişi ve baskın emrini kilitler; 3 vardiyada başlamayan taslak ve emir
  açıkken depo dolarsa emir iptal). `tek`: ilk açılan tür, saha rolü başına bir.
  `donus`: stokta en az olan tür (eşitlikte bu harekâtta en az emri açılan, sonra açılış sırası), toplam stok hedefi 3. Köpek
  yalnız erzak ≥ bedel + 3 × tüketimken. Takma: rol ve kit adımından sonra saha rollerinin yardımcı yuvasına (`board_equip`;
  `tek`: hepsine ilk tür, `donus`: dönen eşlemeyle farklı türler; dinlenen köpek, kişiye bağlı eşya ve EMP hücresi taşıyan yuva
  atlanır). `yok`ta emir de takma da yoktur.
- **`GEAR=kullan|hic`** (varsayılan `kullan`): `RaidBot.Gear`. `hic`te ekipman eylemleri ve köpeğin/tutsağın komutları aday
  değildir (köpek yine yürür); ekipmanın kendi değeri aynı üretim yolunda eşli ölçülür.
- **Bot (`RaidBot`):** köpek, aygıt ve tutsak uzman değildir (`Baskin.IsSpecialist`: kapı, can, yol, kilit, sürükleme, "ekip
  ayakta" sayımı). Adaylar `Baskin.AllActionKinds` (eşitlikte aşama 6'nın altısı önde) ve süzgeçler: tel/kalas makinenin bu tur
  varabileceği kareye ya da (kalas) niyet hattına; taret hattı makine erişiminden geçen yöne; vinç hattının ilk dolu karesi gövde,
  yük ya da birimse; yem tahtada makine varken; havla erimde makine varken; lokum, kelepçe, getir, kokla ve komutanın bütün
  hedefleri. Aygıtlar makine planından önce çalıştığı için (K3) doğrulama kopyasında açık kalır. Puan terimleri (`Weights`):
  aygıtın sonraki turları (`Turret` 4, `Decoy` 2, `Winch` 6 × kalan tur), engelin uzattığı makine yolu (`Barrier` 3 / adım, makine
  başına en çok 3), tutsak makine bedelinin payı (`CaptiveShare` 0,3; süresi ≤ 1 iken tam), köpek (`DogHp` 2, `DogDown` 30, `Bark`
  4 × erimdeki en çok iki makine, kaba; `Sniff` 8, koklamadan önce ileri geçide `SniffWalk` 2 / tur); yükü köpek taşıyorsa
  tesisin girişinde köpeğin, ara odada en geç kalan uzmanın yolu.
- **`R` satırı** (yalnız tesis, `fate`'ten sonra): `odul`, `arac`, `gear`; `rec` (emirde açık ekipman tarifleri, `+`; yoksa `-`),
  `carry` (kadro sırasıyla yardımcı eşya, `/`); `gx` (eylem kullanımı: `wire`, `plank`, `charge`, `turret`, `decoy`, `winch`,
  `clamp`, `bark`, `fetch`, `sniff`, `command`), `gfx` (efektlerden: `thit` taret isabeti, `wpull` vinç çekişi, `dstep` yem adımı,
  `cap` ele geçirme, `rel` bırakılan tutsak, `dogdown`, `scrap` süresi biten aygıt, `brk` kırılan aygıt, `plate` tutsağın verdiği
  plaka); `dogSteps`, `capSteps` (köpek ve tutsak adımı; uzman `moves`/`acts`'ten ayrı).
- **`C` satırı** (yalnız tesis): `rwTarif`, `rwKazanim`, `orders`, `ordersDraftLeft` (iptal edilen + sonda açık taslak),
  `made_<tür>` (üretilen), `gearLost` (ölenle silinen), `clampSpent` (tükenen kelepçe), `dogRest` (köpeğin dinlendiği vardiya),
  `dogSteps`, `capSteps`; sonda `recipes`, `perks`, `gearUsed` (harekâtta kullanılan türler).
- **`ozet.py --tesis`**: "aşama 8" bölümü (politika, baskın başına ölü, tür başına "tarifi açık baskında kullanıldı" hedefi ≥ %10,
  taşınma ve taşındığında kullanılma, eylem ve etki / baskın, köpek ve tutsak adımı, ödül, açık tarif sayısı, üretim, kayıp,
  kullanılan tür sayısı, kazanımlar). **`ozet.py --esli A B [A B …]`**: aynı tohumlarda eşli fark (zafer, dağılma, ölü / harekât,
  baskın / harekât, ölü / baskın, başarı / baskın; tohum ortalaması ± tohum kümeli standart hata).
- **Regresyon kilidi:** `ODUL=tarif ARAC=yok` koşusu aşama 8 anahtarları atılınca `t6sn` ile satır satır aynıdır (BSD `sed` `\t`
  tanımaz; `perl` kullanılır). Karşılaştırma tam dosyadır: taban `head -n` ile `t8yok`'un satır sayısına kırpılmaz (kırpılırsa
  eksik ya da kesik bir `t8yok` çıktısı fark vermeden geçer); `BAL.txt` `*-*.txt` kalıbına girmez:

```bash
for f in out/t8yok/*-*.txt; do cmp <(perl -pe 's/\t(odul|arac|gear|rec|carry|gx|gfx|dogSteps|capSteps|rwTarif|rwKazanim|recipes|perks|made_[a-z_]+|orders|ordersDraftLeft|gearLost|clampSpent|gearGone|dogRest|gearUsed)=[^\t\n]*//g; s/tag=t8yok/tag=t6sn/' $f) out/t6sn/$(basename $f); done
```

- **`BOTW="alan=değer;…"`**: `RaidBot.Weights`'i değiştirir (yalnız ağırlık taraması; çıktıya yazılmaz).
- Ölçüm `tesis8.txt` (komutlar dosyanın başında; yedi etiket, `t8taban` aşama 6 kapanışının `t6sn`'ine bağlantı). Sonuçları
  `Docs/TESIS-BASKINLARI-TASARIMI.md` §11.6 "Aşama 8 ölçümü".

## Önlem defteri (aşama 10): `ODUL=onlem`, `TAK`, `tesis10.txt`

Önlem defteri `facilityCountermeasures` (C# varsayılanı `true`; aşama 18'e dek yalnız `raidFacilityEnabled` iken okunurdu) arkasındadır; kapalı
koşu (`K0` etiketleri) aşama 9 ile bayt aynıdır. Pusu isabetinin `aygit` sayımına girmesi ölçüm anahtarıdır:
`facilityTallyAmbush=true` (§6.3'ün lafzı; varsayılan sayılmaz, K6). İkisi de `BAL` ile verilir ve `BAL.txt`'e yazılır.

- **`ODUL=onlem`** (K31): temel pakette tarif; cevap paketinde tarif yalnız cevabın sınıfında önlem kademesi ≥ 1 **ve** temel
  tarifi bu harekâtta açıkken (Söküm'ün temeli Teknisyen kiti hep açık), değilse kazanım (`CampaignBot.AnswerChoice`). Ödül baskın
  sonucundan sonra seçildiği için o baskının öğrenmesi görülür.
- **`TAK=hep|hic`** (varsayılan `hep`; K32): `hic`te üretilen ekipman saha rollerine takılmaz (`CampaignBot.TakeGear`); emir,
  hammadde ve depo aynı, kadro boş gider. R satırına `tak=hic` yalnız `hic`te yazılır.
- **Bot (`RaidBot`):** kademeyi emirden okur (`B.Moves`, `RaidParams.Counters`). Cıvatalı ayak, çift cidar, kesici kol, kısa bağ ve
  parazit Core'un aday ve evre kurallarında kalır (bot kendiliğinden uyar). İki terim: `Weights.Emitter` (6): tahtada ayakta olup
  erimdeki koku yayıcının susturduğu köpek başına bedel (`Baskin.EmitterBlocks`; sabotaj, EMP ve tutsak komutu yayıcıyı kırar,
  köpek erimden de çıkabilir); `Weights.AmbushSeen` (0,25): aygıt 2. kademede (makine aklı) kurulu pusunun kaba beklentisi
  `Ambush` × bu pay. Önlemsiz emirde ikisi de çalışmaz (puan aynı). Tarama (`BOTW`) aşağıda.
- **`R` satırı** (yalnız tesis, `capSteps`'ten sonra): `cm` (emirdeki kademeler, `itme:0/yikim:0/orme:0/ele:0/kopek:0/aygit:0`;
  defter kapalıyken `-`), `tl` (Core'un bu baskından saydığı, `FacilityCounters.RaidTally`, tavanlı, sınıf sırası `/`), `tlb` (botun
  eylem ve efekt sayaçlarından aynı eşleme, `RaidBot.TallyMap`, tavanlı: `tl` ile aynı olmalı), `learn` (bu baskının tamamladığı
  bölgede alınan önlem `sınıf:kademe` ya da `-`), `ans` (emirde açık cevaplar `+`), `cmfx` (`bolt` cıvatanın direndiği itme/çekiş,
  `unbolt` Söküm, `dwall` yıkılan çift cidar, `cut` kesici kolun ek hasarı, `jam` parazitin kısalttığı aygıt, `ebrk` kırılan yayıcı,
  `emit` yayıcının susturduğu köpek-tur, `mpull` vincin makine çekişi), `plates` (Σ `SquadSlot.Armor`), `sqHp`, `sqMv`, `sqPw` (kadro
  toplamı), `rook` (çaylak), `half` (yarım tayın), `tak`.
- **`C` satırı** (yalnız tesis): `learns` (`sınıf:kademe:gün`, sırayla), `cmEnd` (son kademeler), `nLearn`, `rwCevap` (seçilen cevap
  tarifi); sayaçlar `prod_<tür>` (ekipman dışı üretim: kitler, zırh, bakım kiti, EMP hücresi), `cmRaids_<sınıf>_<k>` (önlem altında
  oynanan baskın), `penCarry`/`carryItems` (cezalı taşıma: taşınan eşyanın sınıfında önlem olan eşya-baskın / taşınan eşya-baskın;
  sınıflar örme seti örme, lokum yıkım, taret ve yem aygıt, vinç aygıt ve itme, kelepçe ele, köpek köpek).
- **REPLAY:** `State()` önlem defterini (sayaçlar, kademeler, öğrenme günlüğü) ve iş emirlerini de karşılaştırır.
- **`ozet.py --tesis`**: "aşama 10" bölümü (önlem / harekât ve sınıf başına, ilk önlemin sınıfı ve günü, önlemli baskın payı sınıf ×
  kademe, cezalı taşıma, Core sayımı, `tl` ↔ `tlb` tutarlılığı (0 olmalı), cevaplar, önlem etkileri, tür başına kullanım önlem
  kademesine göre, "kadro boş" baskınlarının kadro gücü, ekipman dışı üretim). **`ozet.py --fark A B C D [A B C D …]`**: farkların
  farkı `(A − B) − (C − D)` tohum başına (tek aracın önlemle cezası: `t10tek t10tekK0 t10donus t10donusK0`), `--esli`'nin ölçüleri
  ve cezalı taşıma, ayrıca tür başına "tarifi açık baskında kullanıldı".
- **Regresyon kilidi:** `t10yokK0` (önlem kapalı) aşama 10 anahtarları atılınca aşama 8'in `t8yok`'u ile tam dosya aynıdır (taban
  kırpılmaz; `BAL.txt` `*-*.txt` kalıbına girmez):

```bash
K10='cm|tl|tlb|learn|ans|cmfx|plates|sqHp|sqMv|sqPw|rook|half|tak|learns|cmEnd|nLearn|rwCevap|prod_[a-z_]+|cmRaids_[a-z_0-9]+|penCarry|carryItems'
for f in out/t10yokK0/*-*.txt; do cmp <(perl -pe "s/\t($K10)=[^\t\n]*//g; s/tag=t10yokK0/tag=t8yok/" $f) out/t8yok/$(basename $f); done
```

- Ölçüm `tesis10.txt` (komutlar dosyanın başında; on etiket, `t10taban` aşama 8'in `t8yok`'una bağlantı). Sonuçları
  `Docs/TESIS-BASKINLARI-TASARIMI.md` §11.6 "Aşama 10 ölçümü".

## Kuşatma ve savunma, kalıcı tahta (aşama 11): `SAVUN`, `CEKIL`, `tesis11.txt`

Aşama 11'in iki alt bayrağı (C# varsayılanı `true`; aşama 18'e dek yalnız `raidFacilityEnabled` iken okunurdu; `BAL` ile verilir ve `BAL.txt`'e
yazılır): `facilityDefenseEnabled` (bekleyen karşı saldırı = kuşatma, `siege_defend`/`siege_yield`, üç odalı savunma, yeniden alınan
bölgenin fonu ve güveni bir kez) ve `facilityMarks` (izler, YZ onarımı, planda ve emirde iz). İkisi kapalı koşu (`t11K0`) aşama 10 ile
bayt aynıdır.

- **`SAVUN=hep|hic|yok`** (varsayılan `hep`; `CampaignBot.Savun`, `SiegeChoice`): karar vardiyasında (`Simulation.SiegeDecisionDue`)
  `hep` savunur (savunma reddedilirse, ör. kit ya da kadro eksik, bırakır ve `defNoSquad` sayılır), `hic` açıkça bırakır, `yok` komut
  vermez (kapanışta kendiliğinden bırakma; REPLAY ve test). Sıra: `ChooseReward, Door, Decide, Switch, Plan` → **`Siege`** → `Raid`;
  savunma oynandıysa o vardiya baskın yok.
- **`CEKIL=hic|hep|baski`** (varsayılan `hic`; `RaidBot.Retreat`): `hic` giriş odasını tutar (kapı karesine girmez, GEÇ vermez), `hep`
  giriş ve ara odada ilk fırsatta ileri geçide yürür ve GEÇ verir (hedef odasında geçit yok: röleyle tutar), `baski` turun başında
  tehlikede (ayaktaki uzman ≤ 1 ya da makine ≥ ayaktaki + 2) ve ayaktaki herkes bu tur geçide varabiliyorsa o odadan çekilir.
- **Bot (`RaidBot`):** savunma aynı zincir sürücüsüyle oynanır (`PlayFacility`). `Score` savunmada `DefenseScore`'a dallanır: bugünkü
  can, zırh, makine ve tesis terimleri + `Weights.Relay` (10) × saha rölesinin canı (kırık röle kayıp), süre sonu (`won`) kazanç; görev
  yolu, ilerleme, `holdWin` ve `BodyCost` yok; çekilirken ileri geçide yol `GateScore`'un kuralıyla. `Weights.WaveGhost` (1): sonraki tur
  gelecek makineler (`Baskin.DefenseIncoming`) giriş karesinde hayalet makine sayılır (engel, taret ve pusu adayları ve değerleri; barikat
  turu). Tutarken kapı karesi aday değildir; `EndTurn` savunmada `false` dönerse (bütün ekip kapıda) GEÇ verilir. Savunmada donanım
  hedefi yok. Saldırı tahtasında bu dalların hiçbiri çalışmaz (puan birebir aynı; `t11K0` kilidi).
- **`S` satırı** (savunma başına; yalnız tesiste ve savunma açıkken; `R\t` süzgecine girmez): `day tier region wing` (gün, kademe,
  kanadın bölgesi ve sırası), `setup src` (kuşatmanın kurulduğu gün; kaynak `vardiya`, `baskin`, `olay`, `kapi`), `same` (kanat mevcut
  bölgede), `ok`, `left` (terk edilen oda), `endRoom`, `turns` (son tur), `plTurns moves acts`, `rt` (oda başına tur), `waves wm kills`
  (gelen dalga, gelen makine, düşürülen makine), `relay relayHit` (rölenin son canı ve aldığı hasar; hedef odasına girilmediyse `-`),
  `down died extracted trust collat`, `mk` (emirdeki izler oda başına), `wiped` (terkle silinen iz), `ax amb`, aşama 8'in `odul arac gear
  rec carry gx gfx dogSteps capSteps`'i, `tl tlb cm`, `savun cekil dusen dev`, `again` (kanat daha önce yeniden alınmıştı), `perkSiper`.
- **`R` satırı** (yalnız tesis, sona): `retry` (kanadın önceki saldırı baskını başarısızdı), `mk` (emirdeki izler oda başına `G1/A0/H2`),
  `mkNew` (bu baskının bıraktığı yeni iz), `mkKind` (emirdeki iz türleri `duvar/kepenk/donanim/nesne/engel/hurda`; nesne soğutma + kopya
  dolabı, engel tel + kalas).
- **`C` satırı** (yalnız tesis): `siege` (kurulan kuşatma), `siegeNone` (kanatsız anlık karşı saldırı: kuşatma düşüşüyle açıklanmayan
  sayaç), `defend yield autoYield`, `defNoSquad`, `nrSiege` (karar vardiyasında baskın reddi), `nrYield` (bırakılan kanada baskın reddi),
  `defWin defLoss defLeft0/1/2 defLoss2` (iki oda terk + kayıp), `defTrust defDeaths defExtracted defTurns`, `defLossDag5` (−3'lük
  kayıptan sonraki 5 vardiyada dağılma), `marksMade marksGone marksWiped marksRepaired marksEnd` (I3 defteri: `marksMade − marksGone −
  marksRepaired − marksWiped = marksEnd`), `marksSeen` (emirlerde görülen iz), `retryRaids retryWins`, `recapDenied` (savunma açıkken
  yeniden tamamlanan bölgede verilmeyen fon ve güven; `recapFunds` artık gerçek verilen değer, `regionGain` yalnız verilen), `counterDoor`
  (kapı kartının karşı saldırısı), `savun cekil`; `lastHit` yeni değerleri `savunma` (kaybedilen savunma), `birak` (komutla bırakma).
- **REPLAY:** `State()`'e `iz` (izler ve onarım günlüğü) ve `kusatma` (kuşatma günlüğü, karşı saldırı sayacı, alınmış sektörler, günlüğün
  kipi) satırları (`FacilityKit.MarkLine`/`SiegeLine` biçimi).
- **`ozet.py --tesis`**: "aşama 11" bölümü (kuşatma / harekât ve dağılımı, baskın reddi, savunma başarısı tümü + kademe + terk, **hedef
  %40–60** tohum kümeli SE ile, güven / savunma sonuca göre, ölü ve çıkarılan, tur ve hamle, dalga ve röle, yeniden alma ve verilmeyen
  fon, izler ve I3 defter denetimi (0 olmalı), tekrar baskını izli/izsiz). **`--esli`** ayrıca kuşatma / harekât, savunma başarısı,
  yeniden alma baskını, `nrYield`, son güven.
- **Regresyon kilidi:** `t11K0` (iki alt bayrak kapalı) aşama 11 anahtarları atılınca aşama 10'un `t10donus`'u ile tam dosya aynıdır:

```bash
K11='retry|mk|mkNew|mkKind|siege|siegeNone|defend|yield|autoYield|defNoSquad|nrSiege|nrYield|defWin|defLoss|defLoss2|defLeft[0-2]|defTrust|defDeaths|defExtracted|defTurns|defLossDag5|marksMade|marksGone|marksRepaired|marksWiped|marksEnd|marksSeen|recapDenied|counterDoor|retryRaids|retryWins|savun|cekil'
for f in out/t11K0/*-*.txt; do cmp <(grep -v '^S' $f | perl -pe "s/\t($K11)=[^\t\n]*//g; s/tag=t11K0/tag=t10donus/") out/t10donus/$(basename $f); done
```

- Ölçüm `tesis11.txt` (komutlar dosyanın başında; yedi etiket). Sonuçları `Docs/TESIS-BASKINLARI-TASARIMI.md` §11.6 "Aşama 11 ölçümü".

## Katar ve Yalpa (aşama 12): `AYIR`, `KENAR`, `tesis12.txt`

Aşama 12'nin iki alt bayrağı (C# varsayılanı `true`; aşama 18'e dek yalnız `raidFacilityEnabled` iken okunurdu; `BAL` ile verilir ve `BAL.txt`'e
yazılır): `facilityKatar` (na tesisi Katar: kuzey ve güney kenarı açık vagonlar, takip, AYIR) ve `facilityYalpa` (oc tesisi Yalpa: 2/4/6.
tur sonunda batı/doğu/batı yatış). İkisi kapalı koşu (`t12K0`) aşama 11'in `t11savun`'u ile bayt aynıdır (aşama 12 anahtarları atılınca).

- **`AYIR=hep|hic|takip`** (varsayılan `hep`; `RaidBot.Detach`): Katar'ın dönüş geçidinde (herkes kapıda, GEÇ verilebilir) `hep`
  verilebildiğinde AYIR (`Baskin.CanDecouple`), `hic` hep GEÇ (takip işler), `takip` yalnız takip edecek makine varken
  (`Baskin.FollowerIds`). Başka her yerde `Baskin.DecoupleBlock` `rule` döner, oyun aynı.
- **`KENAR=hic|kacis`** (varsayılan `hic`; `RaidBot.Edge`, `Weights.EdgeFall`/`TekEdgeFall`): `Score`'da açık kenardan düşen uzman
  (`Unit.Overboard`) gövde değildir, ayrı bedel alır. `hic`: düşmek + bırakmak (`Down + Abandon` 345, Teknisyen `TekDown + Abandon` 600);
  bot kendi üyesini kasıtlı düşürmez, EMP'nin yan itmesinden kaçınır. `kacis`: 0,3 × düşmek (13,5 ve 90): bot yetişemeyecek üyeyi EMP ile
  kenardan atar (belge §13 "kaçış kapısı"nın üst sınırı). Dikkat: gidiş odasında (A) kalan yolu uzun üyenin yol terimi (`GateScore`) de
  kalkar; `kacis` sağlam ama uzaktaki üyeyi de atabilir (puanın yan etkisi; üst sınırın bir parçası). `BOTW="EdgeFall=…;TekEdgeFall=…"`
  sayı verirse politika ezilir (ağırlık taraması). Kenardan düşen makine bugünkü düşürülmüş makine terimini alır; Yalpa için politika yok
  (kopyadaki gerçek evre kaymayı görür).
- **`RaidBot.Tally`**: `Detaches` ("decouple" efekti), `DetachMiss[neden]` (Katar'ın dönüş geçişinde verilmeyen AYIR: `tek` ayakta
  Teknisyen yok, `gate` Teknisyen kapıda değil, `acted` yeteneği harcandı, `pol` politika istemedi, `auto` kendiliğinden geçiş),
  `Followers` (geçişte takip eden makine), `EdgeMachines`, `EdgeTeam`, `EdgeOther` ("fall" efekti: makine, uzman, köpek/tutsak/aygıt),
  `EdgeBy[kaynak]` (kenardan düşen makinenin kaynağı: `emp`, `sab`, `kosu`, `evre` makine evresi ve olay adımı, `diger`), `Tilts` ("ev"
  efekti, Obj `yalpa`), `Slides[tür]` ("slide": `p` uzman, `e` makine, `govde`, `yuk`, `hurda`, `diger`), `TiltHurtP/E` (yatışta çarpan
  birim, slide N 0). `CountFx` efekti nesne kimliğiyle bir kez sayar.
- **`R` satırı** (yalnız tesis, en sona; alt bayraklar kapalıyken de yazılır, sabit değerle): `ayir kenar`, `det`, `detNo`
  (`tek:n/acted:n/gate:n/pol:n/auto:n`), `fol` (raporlara yazılan takipçi), `folRooms` (takipçiyle oynanan, takipsiz temiz sayılacak dönüş
  odası), `edgeM edgeP` (kenardan düşen makine / uzman oda başına `G0/A1/H2/a0/g0`), `edgeX` (köpek, tutsak, aygıt), `edgeSrc`
  (`emp:n/sab:n/kosu:n/evre:n/diger:n`, makine), `eDied` (kenardan düşüp ölen; 0 olmalı), `eInj` (kenardan düşenlerin yatışı, vardiya
  toplamı), `tilt` (oda başına yatış), `slide` (`p/e/govde/yuk/hurda/diger`), `tiltHit` (`p:n/e:n`), `evP evE` (olay hasarı ekip /
  makine, odaların `EvDamageTeam/Machines` toplamı; her olayda, asansör ve yağda da). `fate`'e yeni kategori **`e`** (Katar'ın kenarından
  düştü, durum `"k"`; yaşar, yatar). Kenardan düşen `died`'a girmez, `rescued`'a girer.
- **`S` satırı** (sona): `edgeM edgeP fol tilt` (savunmada Katar ve Yalpa kapalı: 0 olmalı).
- **`C` satırı** (yalnız tesis, en sona; M'nin sıralı anahtarlarına girmez): `katarRaids yalpaRaids det detNo fol folRooms edgeM edgeP
  fellInj eDied slides tiltHitP ayir kenar`.
- **`ozet.py --tesis`**: "aşama 12" bölümü (Katar: başarı tohum kümeli SE, ölü, dönüş odası, AYIR fırsatı ve nedenleri (**`acted` ≤
  %20** bot ölçütü), takip, kenardan düşen makine (kaynak, oda) ve uzman, kişi sonu, yatış, itme önlemi; Yalpa: yatış, kayma, çarpma,
  olay hasarı; savunmada şart olayı) ve yapı denetimleri (G/g'de kenar olayı, savunmada şart, takvim dışı yatış, oc'de `tl ≠ tlb`,
  kenardan düşüp ölen, `fate e ≠ edgeP`, yatış ≠ 5 × düşen, şartın bölgesi dışında olay; hepsi 0). Tabanda aynı bölgelerin asansör ve
  yağ satırları basılır.
- **`ozet.py --ilk BÖLGE A B [A B …]`**: bölgenin ilk baskını eşli: aynı harekâtın öneki (bölgenin ilk baskınından önceki R/S satırları,
  aşama 12 anahtarları atılarak) aynıysa ilk baskının ölçüleri (başarı, ölü, düşen, kurtarılan, tur, oda, stuck, sivil, edgeP, edgeM, fol,
  det, tilt, evP, fate l) A − B, tohum kümeli SE; geçerli çift sayısı yazılır.
- **Regresyon kilidi:** `t12K0` aşama 12 anahtarları atılınca `t11savun` ile tam dosya aynıdır; önek kilidi: `t12 ~ t12yalpa` ilk na
  baskınına, `t12yalpa ~ t12K0` ilk oc baskınına, `t12ayir0`/`t12kenar ~ t12` ilk na baskınına kadar bayt aynı:

```bash
K12='ayir|kenar|det|detNo|fol|folRooms|edgeM|edgeP|edgeX|edgeSrc|eDied|eInj|tilt|slide|tiltHit|evP|evE|katarRaids|yalpaRaids|fellInj|slides|tiltHitP'
for f in out/t12K0/*-*.txt; do cmp <(perl -pe "s/\t($K12)=[^\t\n]*//g; s/tag=t12K0/tag=t11savun/" $f) out/t11savun/$(basename $f); done
```

- Ölçüm `tesis12.txt` (komutlar dosyanın başında; beş etiket, hedef satırı yok, okuma ölçütleri önceden). Sonuçları
  `Docs/TESIS-BASKINLARI-TASARIMI.md` §11.6 "Aşama 12 ölçümü".

## On iki imza makinesi (aşama 13)

`tesis13.txt`, iki imza dağılımını aynı 80 g5 haritasında ve aynı `karisik ucuz hic` politikasıyla karşılaştırır.
`t13K` etiketi önceki altı ortak imzayı, `t13` etiketi on iki ayrı imzayı kullanır. Koşuyu proje kökünden başlat.

```bash
RETIRE=1 PICK=rastgele DEV=kir DUSEN=hep ODUL=tarif ARAC=donus SAVUN=hep CEKIL=hic AYIR=hep KENAR=hic \
  Tools/DengeBotu/kos.sh Tools/DengeBotu/tesis13.txt
python3 -I Tools/DengeBotu/ozet.py --tesis t13 t13K
python3 -I Tools/DengeBotu/ozet.py --esli t13 t13K
```

Yeni üretilmiş harita tabanı `Library/Calisma/taban13` (tek tahta blokları kayıt 21'de emekli; güncel kilit `taban21`). Sabit dünya testi önceki bayt tabanını kullanır. Alt bayrak
regresyonu sabit dünya, tesis ana bayrağı açık, `facilitySignatureMachines=false` ile 5, 9, 44, 54 tohumlarını karşılaştırır.

## Kişisel unvanlar (aşama 14)

`tesis14.txt` iki kipi aynı 32 g5 haritasında oynatır. `t14K` imza makinelerini açık, kişisel unvanları kapalı tutar.
`t14` ikisini de açar. Etiket tablosunun başındaki ortam değişkenleri önceki tesis ölçümüyle aynı politikayı seçer.

```bash
RETIRE=1 PICK=rastgele DEV=kir DUSEN=hep ODUL=tarif ARAC=donus SAVUN=hep CEKIL=hic AYIR=hep KENAR=hic \
  Tools/DengeBotu/kos.sh Tools/DengeBotu/tesis14.txt
python3 -I Tools/DengeBotu/ozet.py --tesis t14 t14K
python3 -I Tools/DengeBotu/ozet.py --esli t14 t14K
```

`t14` son harekât satırında tür başına `title_*`, ilk kazanım günü `title_first` ve son yaşayan sahip sayısı `title_alive` yazar.
`title_first=-1` kazanım olmadan biten harekâttır. `REPLAY=1` her kişinin iş sayaçlarını ve unvanlarını da karşılaştırır.

## Sahadaki sızmacı (aşama 15)

`tesis15.txt` aynı 32 g5 tohumu için saha bayrağını karşılaştırır. `REPLAY=1` ile komut geçmişi kişi ve tutanak durumuyla birlikte denetlenir. `C` satırında etkin kipin `field_agents` ve `field_silent` alanları kabul edilen kişi sayılarıdır. `field_turn` ve `field_capture` gerçekleşen baskın sonuçlarıdır. Bot aday politikası bu sayaçları okumaz. Bir baskında açılan saha tutanaklarını aynı olay politikasıyla sırayla karara bağlar, ardından vardiya fişini basar.

## Köpeğin hurdaya ayrılması (aşama 16)

`tesis16.txt` köpek üretilen 5 ve 9 numaralı g5 tohumlarını çalıştırır. Botu derledikten sonra ölçümü şu komutla çalıştır.

```bash
HURDA_DENE=1 REPLAY=1 SAVUN=hep CEKIL=hic ODUL=tarif ARAC=donus RETIRE=1 PICK=rastgele DEV=kir DUSEN=hep AYIR=hep KENAR=hic \
  Tools/DengeBotu/kos.sh Tools/DengeBotu/tesis16.txt
```

İlk köpek vardiya başında görüldüğünde bot o anın komut listesini kopyalar, oyunu kopyadan kurar ve hurdayı orada dener. `dogScrapTried`, `dogScrapAccepted`, `dogScrapKept`, `dogScrapMetal` ve `dogScrapRecorded` sırasıyla denemeyi, kabulü, hayvanın envanterde kalmasını, metal ve komut sayısındaki farkları verir. `normalScraps` ayrıca ana harekâttaki gerçek hurda kayıtlarını sayar. Olağan ölçümde bu seçeneği kapalı tutmak iki sürümün tüm ham satırlarını doğrudan karşılaştırmayı sağlar.

## Sınırlar

Bot insan değildir: baskın başına ~1,7 sivil kırar ve harekât başına ~6,4 kişi kaybeder; `erzak_azaliyor`'da hep tam tayın,
takası hep reddeder, eğitim ve takas fırsatlarını kullanmaz. Açlık, terk ve ölüm sayıları bu yüzden üst sınırdır; zafer ve
temiz ton insan oyuncuda yüksek çıkar. 40 tohumda politika satırında ±7–8, "iyi" kümede (120 harekât, ama yapılandırmalar
çoğu tohumda aynı yolu izler) yaklaşık ±7 puan gürültü vardır; kararlar için 80 tohum kullanılır.


## Aşama 17 ölçümünü yinelemek

`tesis17.txt` güncel dokuz etiketin (`t17` … `t17saha`; aşama 17'nin onuncu etiketi `t17g5` kayıt 21'de çıktı) BAL, başlangıç kadrosu ve tohumlarını verir. Başındaki ortak ortamı kullan. Her etiketin hemen üstündeki `# @etiket` satırının ortam değerlerini o koşuya uygula.

Önce botu derle. `kos.sh` yalnız `$OUT/camp8.exe` yoksa derler; exe varsa Core ya da `Camp8.cs` değişmiş olsa da onu yeniden kullanır. Bu makinedeki varsayılan `Library/DengeBotu/camp8.exe` 18 Kasım tarihli: onunla `t17saha` `alan yok: infiltratorFieldEnabled` hatasıyla durur, öteki etiketler eski botla sessizce koşar ve §11.6 değerleri çıkmaz. Ayrı bir klasör istersen `OUT=<klasör>` ver (`kos.sh` orada exe yoksa derler); `ozet.py`'ye de ilk argüman olarak `--dir <klasör>/out` ver, `--taban t0gen` için `t0gen` klasörü de orada olmalı. Örneğin tek araç karşılaştırması:

```bash
Tools/DengeBotu/derle.sh
export PICK=rastgele RETIRE=1 REPLAY=1 DEV=kir DUSEN=hep ODUL=tarif ARAC=donus SAVUN=hep CEKIL=hic AYIR=hep KENAR=hic
Tools/DengeBotu/kos.sh <(grep '^t17|' Tools/DengeBotu/tesis17.txt)
ARAC=tek Tools/DengeBotu/kos.sh <(grep '^t17tek|' Tools/DengeBotu/tesis17.txt)
python3 -I Tools/DengeBotu/ozet.py --tesis t17 --taban t0gen
python3 -I Tools/DengeBotu/ozet.py --esli t17tek t17
```

Her etiket aynı 80 tohum ve üç yapılandırma içerir. Ana tabloda R saldırıyı, S oynanmış savunmayı sayar. Araç farkı tohum kümeli, diğer ana başarı oranları uygun baskınlar üzerinden havuzlanmıştır. Kurtarma hedefinin paydası tarihsel g4 `t0gen` kurtarılan/harekât değeridir. Kalıcı çıktı ve kararlar tasarım belgesi §11.6’dadır. `ucuz` olay politikası sabotajda tercih ettiği seçeneğin kapalı olduğu son sakin durumunda açık kalan seçeneği uygular.

**Depodan yinelenemeyen iki etiket.** §11.6’daki `t17k3` ve `t0g4kontrol` `tesis17.txt`’te yoktur; tek seferlik deney koşularıdır. Git’e girmeyen `Library/Calisma/asama17/k3.py`, `t17g5`’in işlerini (aynı BAL, tohum ve yapılandırmalar) iki etiketle çoğaltıp `t0g4kontrol`’ü A13 öncesi arşiv exe’si `Library/Calisma/asama13/exe_once/camp8.exe` ile, `t17k3`’ü bu exe’nin deney kopyası `Library/Calisma/asama17/k3/camp8.exe` ile koştu. Deney kopyasını `Library/Calisma/asama17/K3.cs` üretir: arşiv exe’sini Mono.Cecil ile sabit IL ofsetlerinden yamalayıp tek tahtada da tamamlanan bölgenin `Rewarded` bitini yazdırır ve okutur. O exe’nin g4 kaynağı arşivde yoktur. Bu iki satır yalnız bu makinenin `Library/` klasörüyle yeniden üretilebilir.

**Ölçümün iki exe’si.** Arşivdeki `Library/Calisma/asama17/olc.py` son exe’yi (`asama17/son/camp8.exe`) gösterir, ama bu sürüm koşuların çoğundan sonra yazıldı (20:13, son değişiklik 20:19); ilk koşuların exe yolu arşivde yok. Koşu başlangıçlarına (`.err` zamanları) göre `tesis17.txt` etiketlerinin 2.400 koşusunun 2.101’i (sekiz etiketin 1.920 koşusu ve `t17saha`’nın 181’i) yukarıdaki son sakin düzeltmesinden önceki exe ile, 299’u (`t17saha`’nın 59’u ve `t17g5`’in 240’ı) son exe ile üretildi. Denetimin IL karşılaştırmasına göre iki exe yalnız `Decide`’ın sabotaj dalında ayrılır, Core IL’i aynıdır. Sabotajın seçenekleri yalnız sök ve sorgula olduğundan eski exe istisna atmadığı her durumda aynı seçeneği seçer; tek fark söküm kapalı ve konum ifşa olmamışken eski exe’nin `InvalidOperationException` ile çökmesidir. Çöken koşu boş olmayan stderr ve eksik `replay=ok` yüzünden doğrulamadan geçmez ve tabloya girmez. Denetimde son exe ile yeniden koşulan 20 dosyanın hepsi bayt aynı çıktı; bunların 19’unu ilk kez eski exe üretmişti. Bu yüzden §11.6 tablosu geçerlidir.
