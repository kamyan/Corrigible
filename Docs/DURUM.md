# Durum, kararlar ve açık işler

_Son güncelleme: 2026-11-01 (plaka rafı, haritanın kenarı)_

## Nerede kaldık

Oyun tek bir harekât: **DeskGame**'deki Sığınak komuta odasından YZ işgalindeki 12 bölgeyi sektör sektör geri alma,
baskınlar **Baskin** sahnesinin 8×8 taktik tahtasında. Açılış ve emir belgesi → harita planlaması → baskın emri → telsiz
hattı (CRT'ye dalış) → taktik savaş → operasyon raporu → vardiya şalteri ve vardiya raporu → … → son rapor ve yeniden başlama baştan
sona oynanabilir. Harekât sivil güven tükenirse kaybedilir (Dağılma); YZ alarmı eşikte karşı saldırır. Vardiyada bir
baskın. Sığınak baskının hazırlık yeri: uzmanların yarası kalır, revir sarar, atölye zırh plakası döker, yaşam alanı
yedek uzman tutar, telsiz alarmı yavaşlatır; oda seviyeleri özgür bölgeyle açılır. Zorluk özgür bölge sayısıyla dört
kademede artar; her bölgenin kendi tahta özelliği ve imza makinesi var (6 yeni makine). Birkaç vardiyada bir sığınağın
kapısı çalar: 23 ziyaretçi, zincirler ve kapıda ölüm (Vuruldu, Sızma). Türkçe ve İngilizce. Kayıt ve devam
(`liberation_savegame.json`, biçim 12) çalışır. Kadro beş uzman (Teknisyen, EMP uzmanı, Analist, Madenci, Sis uzmanı);
her baskına Teknisyen ve görev fişinde seçilen iki uzman çıkar, çıkmayanlar dinlenir. Yeni harekâtta rehberli eğitim (analistin notları) ilk vardiyayı yönlendirir;
ilk baskın adım adım oynanan bir senaryodur. Her harekâtın kendi 30 haritalık taktik harita havuzu var (altı tür, torba: tekrar etmez);
ikmal görevinde malzeme girişe geri taşınır; kampanyada makineler gelişmiş akılla oynar ve ekibe ulaşamayınca sivillere
nişan alır; ayakta kalan siviller güveni geri kazandırır (tavan 20); YZ alarmı arka duvardaki panoda.
Sığınağın ekonomisi kıt: erzak çoğunlukla ikmal baskınlarından gelir, açlık kapıya aç heyet getirir.
Baskın telsizle anlatılır: sığınak ağa bağlanmaz, gözcü bildirir, emir telsizle gider.
Son tam EditMode koşusu **378/378** (2026-11-01).

**Git:** Depo `git@github.com:kamyan/Corrigible.git`, varsayılan dal `main`. Sığınak işi `siginak-ui` dalında
(Aşama 1–10: `eed2824`, eski görünümün kaldırılması: `553517e`, sarı çerçeve: `b7b4d82`, sadeleştirme: `413fbf5`; sivil
güven, YZ alarmı, hazırlık sığınağı, bölge kimliği ve eğitim: `6e658a5`). Sonrası (baskında tıklama düzeltmeleri, alarm
panosu, harita havuzu ve torbası, ikmal kaçışı, makine aklı, sivillere saldıran makineler ve güven ödülü, sığınağın kapısı):
`c6f61e9`; build: `05ffdcb`; telsiz: `9d16037`. Tasarım revizyonu 2 (Aşama 1–10) henüz commit edilmedi.

## Harita kâğıdın kenarından taşıyordu (2026-11-01)

- **Hata (kullanıcı):** bölgeler haritanın kenarından taşıyordu: Grönland, Svalbard, Rusya'nın kuzey adaları üst kenarın,
  Çukotka ve Yeni Zelanda sağ kenarın, Alaska sol kenarın dışına çıkıyordu (kara dolgusu ve işgal taraması). Kâğıt
  (`MapBase`, `MapLines`) tam tasarım dikdörtgeninde biter; `RegionMeshGraphic` çokgenleri (na, weu, ru, oc ve konturları)
  12 px'e kadar dışarı uzanır ve haritada hiç maske yoktu.
- **Düzeltme:** `SadeMapCanvas`'a (`MapCoverFitter.viewport`) iç payı sıfır bir `RectMask2D` (tek seferlik betikle; sahnede).
  İşaretlerin (243 grafik: damgalar, etiketler, işaretler kutusu, ip) hiçbiri kâğıdın dışına çıkmıyor, kesilen yalnız taşan
  coğrafya. Test `SiginakMapTests.DeskGameScene_MapIsClippedToPaper`. Play'de V031 ile harita pozunda kenarlar düz.
- Aynı kayıtta panonun `Discs` ızgarasının (önceki iş) layout'un yönettiği çapa/boy değerleri de dosyaya yazıldı.

## Maketteki plaka rafı: 1 m'lik levha (2026-11-01)

- **Hata (kullanıcı, Play):** atölye ilk zırh plakasını döktükten sonra masa pozunda maketin önünde büyük, boş bir levha
  duruyordu. `ShelterStore` (DEPO bandı) belirip kalkan nesnelerin ölçeğini `Vector3.one`'a yazıyordu; sandık ve kovalar
  ölçeği 1 olan sarmalayıcı, ama `Plate rack/Plate 1–3` doğrudan ölçekli küp (0,006 × 0,07 × 0,05): plaka belirince 1 m'lik
  küp oluyordu. Plaka yokken (`Plates = 0`) görünmediği için önceki denemelerde çıkmadı.
- **Düzeltme:** `ShelterStore.Awake` her nesnenin sahnedeki ölçeğini saklar (`rest`); belirme ve kalkma o ölçeğe göre.
  Awake bir kez çalışır (Show onu elle çağırmış olabilir; ikinci çağrı son kovanın dolgusunu yeniden ölçüp kaydırıyordu).
  Aynı yerde eski bir kusur: kalkma sürerken gelen "göster" yok sayılıyor, nesne kayboluyordu; artık geri belirir.
  Test `SiginakShelterTests.DeskGameScene_StoreKeepsAuthoredScale`. Kullanıcının kaydıyla Play'de denendi (kayıttan devam,
  vardiya sonu, yarıda kesilen kalkma). Ölçeği koddan yazan öteki bütün nesneler (oda, Baskin, prefab'lar) tarandı:
  sahnedeki ölçekleri 1 ya da kod kendi kurduğu nesneye yazıyor; başka örnek yok.

## Madenci, Sis uzmanı ve kadro seçimi (2026-10-31)

- **İstek:** duvar kıran ve sis atan iki yeni uzman; baskın öncesi üç uzman seçilsin, Teknisyen hep seçili. Öneri koda
  karşı üç incelemeden geçti (kurallar ve denge, motor ve determinizm, akış ve okunurluk); kararlar önerilen varsayılanlarla:
  sisin içindeki makine yine ateş eder (körleşme yok), haklar baskın başına (dinamit 2, sis 3), yeni uzmanlar baştan
  kadroda, yeterli uzman yoksa baskın bugünkü gibi eksik kadroyla çıkar, oyunda "sis" (kimlik `duman`; `sis` Kum
  fırtınasının kimliği).
- **Core:** `Baskin`: `maden` (dinamit) ve `duman` (sis bombası) türleri, `Unit.Charges`, `Cell.Smoke`, `BState.Rubble`,
  `ITarget/IntentView.Smoked`, `PreviewResult.Intents`, efektler `dynamite`, `breach`, `debris`, `smoke`, `smoked`. Sis yalnız
  `IntentTarget`'te (atış ve kilitte her adımda önce sis; havanda düştüğü kare; yalnız yakın vuruş, onarım/kalkan/çoğaltma
  değil) ve makine evresinden hemen sonra temizlenir, plan (`ScoreTarget`, `Zone`, `Covers`, `CanReachTeam`) onu görmez.
  Moloz hedef dolaba ve ikmal kasasına işlemez (her haritada hedefin yanında duvar var: Teknisyen'in bir sabotajı bedava
  olurdu, kasa kazayla kırılırdı). `Signature`'a yıkık duvar, sis ve hak yalnız varsa eklenir: kilitli imzalar değişmedi.
  Yeni RNG tüketimi yok. Kadro: `Simulation.Roles` beş, `PickCount`, `DefaultPick`, `ResolvePick`, `Squad(team, pick)`,
  `TeamState.LastPick`, dinlenme uzman başına (`Rested`). Emir `StartLiberationRaid(sektör, seçim)`; kayıt biçimi 11 → 12
  (beş kişilik kadro revire, kapının yara/iyileşme kartlarına ve dinlenmeye girer): **eski kayıtlar açılmaz**.
- **Sunum:** fişte KADRO satırı (sahnede tek seferlik betikle, `OperationSlip.pickChips`, `SiginakMap.pick/tutorial`),
  panoda beş disk (iki satır), haritadaki figür varsayılan kadroyu gösterir. Baskında kartlar, 1–3 ve yetenek satırı
  kadrodan; duvar karoları durumdan; sis bulutu, SİS etiketi, sise kesilen gri kesik çizgi; önizleme hamle sonrası
  niyetleri gösterir; ipucu kartında duvar, sisli kare, "Duvar yıkılır", "Sise kesilir". Kurallar kartına iki uzman
  kadrodaysa birer satır. Eğitim baskını yalnız T + E + A kadrosuyla kurulur (`TutorialRaid.For` türleri denetler).
- **Görsel ve ses:** piyonlar `Prefabs/Raid/Miner.prefab`, `SmokeSpecialist.prefab` (Technician'ın kopyası; harf
  `PawnLetterM/S.asset`: PT Serif Bold ana hatları (OFL, sistem yazı tipi), eski harflerin ölçüsüyle: büyük harf 0,496,
  kalınlık 0,13, pah 0,012 × 0,01, eğri başına 6 parça; M yatayda %84). İpucu resimleri `thumb_standee_M/S.png` (tahtanın
  kamerasıyla, 80 × 92). Sesler `RaidDynamite`, `RaidSmoke`, `RaidSmoked` (kodla sentezlenmiş yer tutucu). Tek seferlik
  betikler depoya girmedi.
- **Oyunda denendi** (Play, V014 fikstürü): fişte seçim (M ve S en eski seçimlerin yerine geçti, T değişmedi), emir
  `liberation_raid eeu_2 maden,duman`, baskında T/M/S kartları, dinamitle duvar yıkıldı ve geri almada geri geldi, sis
  atıldı ve makine evresinden sonra dağıldı; İngilizce fiş.
- **Açık:** ölçüm botu yok (önceki bot oturumun geçici klasöründeydi). Seçim zorluğu düşürür; altı ikili × iki görev ×
  harita türü ölçülmeli, dinamit ve sis hakları buna göre ayarlanmalı (öneri: tek evre bakan açgözlü bot dinamitin yol
  kısaltmasını görmez, skoruna Teknisyen'den hedefe / taşıyıcıdan çıkışa en kısa yol eklenmeli). Fiş haritanın türünü
  göstermiyor (Madenci'nin değeri haritaya bağlı). Sesler yer tutucu. Kurallar kartının lejantında sis satırı yok.

## Tasarım revizyonu 2 (2026-10-30, sürüyor)

Claude Design'ın ikinci turu (`Docs/UI_Siginak_Rev2/`: PROMPT, tokenlar 1.1, 46 sprite, S1–S9 panoları) uygulanıyor; ilk
turun dilini sonradan eklenen parçalara (kapı, alarm, baskın paneli, raporlar) taşır, kuralları ve kayıt biçimini
değiştirmez. Plan, eşleme ve açık sorular `Docs/UI_Siginak_Rev2/IMPLEMENTATION_MAP.md`'de.

- **Karar: Rev1/Rev2 anahtarı yok.** PROMPT ikisi arasında anahtar istiyordu; ilk turun anahtarı ve kurucusu silindiği,
  oda sahnede durduğu için anahtar her nesneyi ikiye katlardı. Güvenli geçiş git'le: her aşama ayrı değişiklik,
  `Docs/UI_Siginak/screens_v2/` bugünkü hâl.
- **Aşama 1 bitti:** sığınak tokenları 1.1 projedeki dosyaya birleştirildi (ilk turun uygulama değerleri korundu), baskın
  tokenları 1.1 kondu, Rev2 sprite'ları `Art/Siginak/Sprites/Rev2/`'de, açık renk onda bir çarkının malzemesi hazır,
  `✕ ▶ ◆ ● ■` glifleri font zincirinde. Oyunda görünen tek fark görev fişinin 432 px olması.
- **Aşama 2 bitti (başlangıç pozu, S1):** FON “060,0” (pirinç virgül, açık onda bir çarkı; EN nokta), 6 tuşlu ray ve
  4 · KAPI (ziyaretçi beklerken lamba yanıp söner, el yazısı fiş; 4 ya da K kapıyı açar), YZ ALARMI ölçeği, KURTARILAN
  BÖLGELER panosu (KAYIP ilanları kalktı), tebeşir vardiya çentiği, teleks ön kâğıdı, CRT'de dört satır, iki satırlı oda
  kartları, vardiyaya bağlı harekât izleri (`SiginakSettings.campaignWear`), yeni eğitim notu (Boşluk sonraki, Esc notları
  kapatır). Eksi farklar artık kırmızı değil mürekkep. Ayrıntı ve görsel farklar `Docs/UI_Siginak_Rev2/IMPLEMENTATION_MAP.md`.
- **1.2 eki temele alındı (`EK_1_2.md`):** paket oyundaki 12 bölge özelliğinin ve 8 makinenin yalnız bir kısmını
  çiziyordu; tasarımcı eksik 13 işareti (düz + çerçeveli), kalkan/kopyalama rozetlerini, üç makinenin 3D biçimini ve dört
  niyeti ekledi. Sprite'lar içe aktarıldı (bütün işaretler mipmap'li), baskın tokenları 1.2 oldu, `SiginakGlyphs`
  oyundaki kimliği işarete bağlıyor. İngilizce adlarda oyundakiler kaldı. 3D biçim, niyet çizimi ve fikstürler Aşama 7'de.
- **Aşama 3 bitti (harita ve görev fişi, S2):** etiketler tasarımın yerlerinde (dar yerde iki satır), cephe bloğu (sektör
  şeridi, yüzde, saha işaretleri), sabit yerde ÖZGÜR damgası, karşı saldırının aldığı sektörde ✕ ve YENİDEN İŞGAL,
  işaretler kutusu sağ altta TEHDİT'le. Görev fişi ekran katmanında sol kenarda (Amerika seçiliyse sağda): SAHA satırları,
  138 px damga sütunu, ödül 2 × 2, ekip diskleri; eksik yalnız fonsa düğme açık kalır, basınca FON titrer ve teleks
  gerekçeyi basar (komut çağrılmaz). Tasarımdan sapmalar (ekip noktaları karada, Hindistan şeridi sağda) haritanın §10'unda.
- **Aşama 4 bitti (sığınak, S3):** pano rafın soluna, ahşap ayağın üstüne taşındı; özet başta “ERZAK YETER · n VARDİYA”
  (3 ve altı kırmızı), uzmanlar diskle; seçili odada personel adımlayıcısı [−] a/b [+], en üst seviyede yeşil damga,
  yetersiz kaynakta düğme açık ve eksik kalem çizik. Maketin üstünde DEPO bandı (sandık, kova, plaka rafı, tebeşir), odalarda
  çıktılar (ranzada yedek, telsizde kadran, atölyede döküm kalıbı, revirde hasta), alarm panosunun altında TELSİZ ODASI
  ETKİN fişi. DEPO ölçeği Soru 6'nın (a) önerisiyle: 10 sandık = erzak sınırı, 5 kova = malzeme deposu.
  Tam EditMode 347/347.
- **Aşama 5 bitti (kapı, S4):** kapı kâğıdı 470 px (sol 40 · üst 64): gözetleme ve interkom ayrı bloklarda (interkom
  daktiloyla), kullanılan seçenek gri + BAKILDI/SORULDU, devre dışı seçeneğin nedeni ve bedel/etki (“−8 erzak”, “güven −1”)
  sağda; yetersiz kaynakta satır açık, basınca teleks + kâğıt titrer; sonuçta seçim satırı ve tek Enter düğmesi. Kapıda
  tehlike bandı, kafesli amber vuruş lambası (üç vuruşta parlar), pirinç gözetleme kapağı, interkomun amber noktası,
  tebeşir ziyaretçi çentiği; kanat koridora açılır, arkasında fayanslı merdiven boşluğu ve iki lamba. Altı siluet + genel
  biçim (S4d), kart → tür tablosu Soru 5'in ara cevabıyla. Ölümcül kapıda (VURULDU, SIZMA) oda ve lamba kırmızı (Soru 10 ara
  cevabı), çift çizgili damga, yalnız Enter / SON RAPORA GEÇ, kenar satırının tuşları kesik ve soluk. Tam EditMode 349/349.
- **Aşama 6 bitti (vardiya raporu ve karşı saldırı, S5):** rapor 540 px (sol 24 · üst 274), teleks kâğıdı ve delik
  şeridi; başta üç göz (ERZAK n vardiya yeter · YZ ALARMI on adımlık ölçekle · FON) ve alarm notu; erzak ve alarm dökümde
  tekrar edilmez, ekip bölümü yalnız EKİP. Renk kuralı (+ yeşil, − mürekkep, ±0 gri, kırmızı yalnız eşik ve işgal)
  `ReportTones`'ta, EditMode testli; ± fişleri aynı kuralla. Yukarıdan iner, başlık daktiloyla, gözler sırayla. Karşı
  saldırıda alarm lambası üç kez yanar, raporun başında çift çizgili kutu (teleks kodu + sektör), alarm gözü “10/10 · EŞİK”,
  haritada ✕ ve YENİDEN İŞGAL (kutunun üzerine gelince ✕ yanıp söner), özgürlüğünü yitiren bölgenin kartı panodan düşer.
  Tam EditMode 350/350.
- **Aşama 7 bitti (baskın, S6):** köşe panelleri tahtanın malzemesinden (kontrplak + ozalit + bant), tahtaya değmez. Tur
  şeridi 34 px hücreli (son iki tur kesik kırmızı; son turda SON TUR damgası ve EMRİ İLET'te “SON TUR”), evre çipi, üç
  satırlı görev çipi (bölge özelliği işaretli, hedefin karesi, amaç). Ekip kartları 237 × 84 alt sırada (kilit niyetinin
  hedefindeki uzmanda KİLİT TEHDİDİ · #n), yetenek kartı S6'nın adlarıyla (Soru 7 ara cevabı), makine evresinde kilitli
  kâğıt, telsiz fişi teleks kâğıdı. Etiketler 11 px pip ve tür işaretiyle, bayrak 30 × 28; yeni robotta bir tur UYANDI.
  Niyetler türüne göre (havan yay, kalkan çift şerit, kopyalama kesik, rozetler), hep üstte. Sekiz makine S9-A biçiminde;
  gri tonlamada ayırt ediliyor. EMRİ İLET İngilizcede SEND ORDERS. Tam EditMode 354/354.
- **Aşama 8 bitti (sonuç kartı ve operasyon raporu, S7):** baskın bitince tahta kararır, spot nedenin üstünde açılır, sağdan
  sonuç kartı kayar: başlık yerinde tek damga, yer satırı, üç satır, tek düğme. Dönüşte kamera harita pozuna geçer, rapor
  solda iner, iplik ekip figürüne ve figürün çevresine halka; başlık sonucu söyler, altında tek cümle; başarılıda KAZANÇ ve
  ± fişleri, sektör işareti maviye döner; başarısızda ödül alanı yok, alarm ölçekle. Eski olay modalı ve `SimDeskBuilder`'ın
  rapor kısmı kaldırıldı (`SiginakOperation`, `OperationReportBuilder`, `OperationReportView`). Tam EditMode 356/356.
- **Aşama 9 bitti (son rapor, S8):** harekât bitince kamera harita pozuna geçer, harita karartılır, rapor aşağıdan gelir:
  sonuç damgası ve kayıpta neden damgası (DAĞILMA, VURULDU, SIZMA), cümle, dört gösterge, işgal şeridi (x = vardiya 1…N her
  zaman tam genişlik) ve üstünde numaralı kırılma anları (en çok altı; sunumda türetilir), YENİDEN BAŞLA. Eski son rapor
  (`GameOverView` ve kartları, `DeskChart`, `GameOverVM`) kaldırıldı. GÜVEN şeridi Soru 8'e bağlı. Tam EditMode 360/360.
- **Aşama 10 bitti (dil, erişilebilirlik, temizlik):** tam görüntü seti S1a…S8b TR, EN ve gri (`SiginakCaptureSet` →
  `Docs/UI_Siginak_Rev2/screens_unity_rev2/`). Punto taraması oda, ekran katmanları, son rapor, ayarlar ve baskın HUD'unu
  kapsıyor: 781 metin, 0 ihlal (baskının eski panelleri, üzerine gelme kartı ve ayarlar kartı 14 px'e ölçeklendi, kart
  satırları sarıyor). EN taşmaları (teleks durumu, SON TUR, EMRİ İLET, neden damgası, rapor numarası) ve odadaki üzerine
  gelme etiketlerinin çevirisi düzeldi; kapı pozu kapıya bakıyor. Kare süresi Revizyon 2 öncesinden masa ve sığınakta
  %2–3 yavaş, harita ve CRT eşit (küçük parçaların gölgesi ve statik işaretlerle daraldı; gölgeler yumuşak kaldı, fark kabul).
  Rev2'nin bıraktığı ölü kod silindi. Tam EditMode 361/361.
- **Uzman piyonları (S9-B):** baskında T, E, A artık harfli ahşap piyon (referansın yazı tipi ve ölçüsü): siyah altlık ve
  krem halka üstünde kalın harf. Tur bitince harf koyulaşıp arkasını döner, altlıkta ✓; yaralıda bant, plakalıda çelik
  levha; düşen harf geriye yatar. Aşama 7 yalnız makineleri değiştirmişti.
- **İpucu kartı resimleri:** üç piyon ve sekiz makinenin her biri kendi Rev2 modelinden çizildi (`RaidDataSource.EnemyThumb`
  her türe kendi resmini verir; önceden yalnız dron ve bakım robotunun Sade çizimi vardı). Hepsi kartın 40 × 46 çerçevesinde
  (piyon resmi 40 × 56'dan küçüldü, artık ezilmiyor). Dolap ve soğutma ünitesinin resimleri hâlâ Sade çizimi.
- **Model denetimi (DeskGame + Baskin):** bütün modeller referans sahneyle karşılaştırıldı; değişmemiş olanlar referansa geldi:
  alarm panosu ve küre lambası, telsiz yüzü (Soru 11 cevabı), mikrofon, telsiz kaydı, umut bitkisinin geç hâli, kapının yeri
  (z 0,32–1,18) ve ayrıntıları, kapı pozundaki boşluk, maketin tavanı ve telsiz odası, baskının duvar, kanal, sunucu, soğutucu,
  kopya ve hedef dolapları, uyanma işareti; baskın işaretleri parçaların altında, tarama referanstaki yönde. Ayrıntı ve bilinçli
  bırakılanlar `Docs/UI_Siginak_Rev2/IMPLEMENTATION_MAP.md` değişiklik günlüğünde.
- **Telsiz defteri:** CRT önündeki defter hattın son dört kaydını gösterir (baskın dönüşleri, düşen uzman, kırılan sivil
  sunucu, karşı saldırı, bugünkü ekip ve hat durumu; `RadioLogBuilder`). Tam EditMode 362/362.

## Telsiz kurgusu, 1. aşama (2026-10-29)

Kullanıcı "baskına geçerken bağlanıyor diyor; oyun ağa bağlanmama üzerine, saha telsizden gelsin, figürlerle masada
yansıtılsın, emirlerimizi telsizle yollayalım" dedi. Açılış emri "AĞA BAĞLANMA. ANALOG KAL." derken geçiş "BAĞLANTI
KURULUYOR · SAHA YAYINI · KAMERA 2" diyordu; emir belgesi de masada CRT varken "ekran yok" diyordu.

- **Karar: emirler tek tek, anında gider.** Toplu iletme (turun sonunda hepsi birden, sonuçlar sonra) başka bir oyun olur:
  hamlenin sonucunu görmeden sonrakini planlamak motoru ve kayıtları değiştirir. Bu aşama yalnız sunum; Core, kayıt
  biçimi ve RNG değişmedi.
- **Metinler:** CRT telsizin göstergesi (TELSİZ · KANAL 07; DİNLEMEDE, HAT AÇILIYOR, HAT AÇIK, HAT KAPANDI, HAT KAPALI;
  CRT plakası TELSİZ · K-07). Geçiş: SİNYAL, "SAHA MASASI · PAFTA 8×8 · GÖREV". Baskın HUD'u: TURU BİTİR → EMRİ İLET
  (İngilizce TRANSMIT; "EMİRLERİ İLET" 104 px'lik etikete sığmıyordu), makine evresi MAKİNELER → DİNLEMEDE, GÜNLÜK → TELSİZ,
  operasyon günlüğü → TELSİZ KAYDI, olay fişi SIRA → TELSİZ ("emir iletildi · gözcü bildiriyor"). Kurallar kartı, eğitim
  notları ve emir belgesi ("Burada ağ yok: emirler kâğıtla verilir, sahayla yalnız telsiz konuşur.") buna göre.
- **Ses:** hat açılırken frekans arama, EMRİ İLET'te bas-konuş ve onay bipi, gözcünün her makine hamlesinden önce kısa
  bildirimi, baskın boyunca telsiz hışırtısı; saha sesleri telsiz bandından (ayrıntı `Docs/MIMARI.md` → Ses).
- **Play'de denendi:** açılış emri sığıyor; masada CRT DİNLEMEDE; geçiş HAT AÇILIYOR; HUD'da TELSİZ, EMRİ İLET, makine
  evresinde DİNLEMEDE ve TELSİZ fişi (başlık uzayınca sıra kutuları başlığın ardından dizilir); telsiz kaydı; İngilizcede
  LISTENING, RADIO, TRANSMIT; ses kaynaklarında saha sesleri bantlı, arayüz düz. EditMode 332/332.
- **Sonraki aşama (açık):** 3) isteğe bağlı mekanik: YZ telsizi dinler (iletim alarmı artırır); motoru değiştirir, kayıt
  sürümü artar.

## Telsiz vurgusu denendi, geri alındı (2026-10-30)

Kullanıcı telsizin yeterince hissedilmediğini söyleyince üçü denendi: tur başında gözcünün niyetleri sırayla bildirip
okları tek tek çizmesi, baskın boyunca açık telsiz şeridi (son dört konuşma, daktilo gibi), gözcünün satırlarında
sentezlenmiş konuşma sesi. Kullanıcı hepsini geri aldırdı; kod, prefab, ses ve çeviriler 2. aşamadaki hâline döndü.

## Telsiz kurgusu, 2. aşama: saha masası (2026-10-29)

Tahta zaten kontrplak taban, ozalit kâğıt ve kaideli harf figürlerle bir masa maketi gibi duruyordu; keçe ve figürler
değişmedi. Eklenenler yalnız sunum (Core, kayıt, RNG aynı):

- **Pafta koordinatları:** kameraya bakan kenarlarda mavi el yazısıyla A–H ve 1–8. Gözcünün satırları kareyi adıyla verir
  ("№3 · saldırı → TEKNİSYEN · A6 · −1", boş kareye "→ C3", "Yeni bir robot uyandı: C3.").
- **Yağlı kalem okları:** niyet okları hafif dalgalı, uçlarda incelen, açık V uçlu çizgiler; okunurluk için kalınlık aynı.
- **Görevli çubuğu (kaldırıldı, 2026-10-30):** makine evresinin sonunda bakır başlıklı ahşap bir çubuk figürleri yeni
  karelerine itiyordu. Kullanıcı kaldırttı; makineler yine turun sonunda yeni karelerinde belirir.
- **Komuta masası:** CRT'nin önündeki klavye kalktı (bilgisayar gibi duruyordu); yerine bas-konuş masa mikrofonu (kablosu
  telsize gider) ve kalemli telsiz kayıt defteri. CRT'nin üzerine gelme etiketi "TELSİZ HATTI · Yalnız baskın emriyle hat
  açılır…". Punto taramasında klavye istisnası defter başlığına geçti (dekor, 6,5 px).
- **Play'de denendi:** koordinatlar, kalem okları, telsiz satırında A6/A3 kareleri
  figürlerin gerçek yerine uyuyor; CRT yakın planında mikrofon ve defter. EditMode 332/332.

## Harita türleri (2026-10-28)

Kullanıcı 30 haritalık havuzdan sonra "haritaları türce de çeşitlendir" dedi. Önceki üreteç tek bir yerleşim türü veriyordu
(hedef 6. sütunda, dağınık kısa duvarlar).

- **Altı tür** (`RaidMaps.Kinds`): koridor (önceki), kasa odası (hedef iki kapılı odada, bakım robotu içeride), iki hat
  (uzun bölme duvarı), açık salon (tek karelik sütunlar), şebeke (kablo kuşağı), rehine (siviller yolun ortasında). Havuzda
  her türden beş harita; 30 oda adı türlere dağıtıldı (Arşiv katı kasa odası, Montaj hattı iki hat…). Giriş hep solda ve
  tek hedef dolap (motor, ikmal kaçışı ve fazladan makinelerin uzak yarıya konması buna dayanıyor).
- **Zorluk ayarı:** her tür 320 baskınla ölçüldü (`Docs/OYUN-TASARIMI.md`, tablo). İlk deneme: kasa odası %55 (tek kapı,
  8–11 adım), açık salon %83, rehine %81, iki hat %78; koridor %72. Kasa odası iki kapıya ve 6–9 adıma, açık salon 5–7 sütuna
  ve 8–10 adıma, rehine 7–10 adıma, iki hat 8–11 adıma çekildi: türler %69–75.
- **Harekât:** 30 harekâtta yalnız koridorlu havuz 19/30, türlü havuz 24/30 kazandı; aç vardiya 13 → 11, baskın başarısı
  %66 → %68. Koridor 4. kademede en zor tür (%38, ötekiler %47–61); geç oyunun uçurumu biraz yumuşadı. 30 harekâtta fark
  gürültü sınırında.
- **Testler:** `RaidMapsTests.Pool_HasEveryKindEqually_AndEachMapFitsItsKind` (20 tohum), `Kinds_HaveTheirShape`; 200 tohumda
  havuz hep 30 farklı, kurallara uyan harita (elle çizilmişe düşen yok). 332/332. Play'de kasa odası (Batarya odası) ve iki hat
  (Montaj hattı) baskınları açıldı, tahta doğru kuruldu. Kayıt biçimi 11'de kaldı (biçim 11 henüz commit edilmedi).

## Kıt sığınak ekonomisi ve 30 haritalık havuz (2026-10-28)

Kullanıcı "harita sayısını arttırmalı ve sığınak ekonomisini düzeltmeliyiz, kıtlık çekmeli" dedi. Önceki ölçümde harekât sonunda
erzak 120/120, malzeme 86/100, nüfus 24/24, fon ~66 kalıyordu; hiç aç vardiya yoktu, aç sakinlerin heyeti ve silahlı sakin
zinciri botta bir kez bile tetiklenmedi; kapıda erzakla yardım bedavaydı. Harekât ~57 baskın sürerken havuzda 10 harita vardı
(her biri ~6 kez).

- **Harita havuzu 10 → 30** (`RaidMaps.PoolSize`), oda adları 14 → 30 (16 yeni ad çevrildi). 400 tohumda havuz hep 30 farklı,
  kurallara uyan haritayla doluyor; elle çizilmiş haritaya düşen yok. Üretecin ölçüleri aynı (haritalar aynı türden, sayıca çok).
- **Ekonomi** (`Balance` ve `BalanceConfig.asset`): tüketim kişi başı 0,2 → 0,35, toplama görevli başı 1 → 0,7, erzak deposu
  seviye başı 40 → 30, ikmalin erzağı 10 → 6, malzemesi 10 → 7, fonu 12 → 10, sunucunun malzemesi 6 → 5, malzeme deposu
  100 → 60. Bir toplayıcı iki kişiyi doyurur, yaşam alanı nüfusun yarısına yer açar: büyüyen sığınak ikmal baskınlarıyla beslenir.
- **Analist notu:** toplayıcılar yetmezken ve yaşam alanında yer yokken erzak 6 vardiya içinde bitecekse "→ erzak N vardiyaya
  yeter, ikmale çık" (bittiyse "→ erzak bitti, ikmale çık"); `ShiftReportBuilder.SuppliesWarnShifts`.
- **Ölçüm** (bot, Core kopyası, orta oyuncu, kapıya en iyi cevap, 30 harekât; kıtlıkta erzak ve malzemeyi daha değerli sayan
  kapı kararları):

  | Ayar | Kazanılan | Erzak (depo %) | Aç vardiya | Erzak bitti | Malzeme ort. | Fon ort. |
  |---|---|---|---|---|---|---|
  | Önceki ekonomi (havuz 30) | 21/30 | 95 | 0 | 0 | 44 | 35 |
  | A: toplama 0,75, tüketim 0,3, ikmal erzak 8, malzeme 6/4, depo 30/60 | 24/30 | 87 | 0 | 0 | 9 | 48 |
  | B: toplama 0,7, ikmal erzak 6, malzeme 7/5, ikmal fonu 10, tüketim 0,3 | 20/30 | 70 | 2,4 | 0 | 15 | 26 |
  | **C: B ile tüketim 0,35 — seçilen** | **19/30** | **52** | **13,3** | **2,7** | **15** | **26** |

  C'de aç vardiya harekâta göre 0–33, erzağın bittiği vardiya 0–10, heyet 0–2; odalar ortalama 45. günde tamamlanıyor (önce 41).
  A malzemeyi fazla kıstı (odalar çoğu harekâtta tamamlanmadı, fon birikti). Kazanma oranı 21'den 19'a indi (30 harekâtta
  gürültü sınırında).
- **Kayıt biçimi 11** (havuz değişince aynı tohum başka haritalar verir). Testler: `ShelterTests.Starvation_…` (toplayıcılar artık
  bütün nüfusu doyurmaz; açlığı ikmal bitirir), `SiginakShiftTests` (V.014 fikstürü erzak 22/30; yeni
  `Analyst_WarnsWhenGatherersCannotFeedTheShelter`). 311/311. Play'de: sayaçlar ERZAK 24/30, GÜVEN 10/20; ilk vardiya raporu
  toplama +1,4, tüketim −2,1; erzak 3'e indirilince analist "→ erzak 3 vardiyaya yeter, ikmale çık" yazdı.

## Sığınağın kapısı (2026-10-28)

Kullanıcının önerisi (odaya bir kapı; birkaç vardiyada bir çalsın, erzak isteyen, erzak getiren, başka konuda gelen, seni
vuran sakinler ve robotlar) önce tasarım olarak konuşuldu, sonra "daha önce konuştuğumuz kapı mekaniğini de ekle" ile
uygulandı. Kurallar ve kart tablosu: `Docs/OYUN-TASARIMI.md` → Sığınağın kapısı.

- **Core:** `Door.cs` (`DoorState`, `DoorVisit`, `StepDoor`, `PickDoorCard`, `AnswerDoor` → `door_answer`, kapının kendi
  üreteci `DoorRng`) ve `DoorCards.cs` (23 kart; metinler `Loc.N`, koşullar, seçenekler, etkiler, zincirler). İki yeni son
  (`GameData.Endings`: `vuruldu`, `sizma`). `WorldState.Door`, `LastCounterattackDay`, `SimEventType.Door`;
  `ShelterLogic.Leave` (ayrılan sakinin görevi arka odalardan düşer). Denge: `Balance.door*` ve `BalanceConfig.asset`.
  Kayıt biçimi **10** (Step artık kapıyı da çözüyor; eski kayıtlar yüklenmez).
- **Kararlar:** ölüm saf rastgele değil (duruma bağlı, önceden uyarılı, hep bir çıkış var; ilk 8 gün ölümcül ziyaretçi
  yok). Gözetleme deliği ve interkom komut değil, sunum: bakmak serbest, bilgi karardan önce gelir. Kopyanın insan ikizi
  aynı başlıkla gelir; ipuçlarına bakmayan oyuncu ya güven kaybeder ya ölür. Vuruş vardiyayı kilitlemez: kapıda kalan
  ziyaretçi vardiya bitince açılmadı sayılır. Kart etkileri içerik tablosunda (birim canları gibi), sıklık ve eşikler
  `Balance`'ta. İlk vuruş gün 2: eğitim ilk vardiyada biter, kapı onunla çakışmaz.
- **Sunum:** sağ duvarda çelik kapı ve ardında koridor (duvarın mesh'i kapı boşluklu), kamera pozu K8, kapı kâğıdı
  (`DoorCardView`), bağlayıcı `SiginakDoor`, kurucu `DoorViewBuilder`. Vuruşta kamera kendiliğinden kapıya döner (kapı K1'de
  görünmüyor); Esc masaya döner, K geri getirir. Ölümcül kararda son rapor kâğıttaki "Son rapor" düğmesine kadar bekler
  (`DeskBridgeState.HoldEnding`). Vardiya raporuna kapı satırı (üçüncü ekip yuvası) ve açılmayan kapının erzağı ayrı satır;
  defterde kapının fonu "Kapı · kim". Ön kontrol kapıda bekleyeni uyarır. Sesler kodla sentezlenmiş yer tutucular.
- **Denge ölçümü** (bot, Core kopyası, orta oyuncu, 30'ar harekât): kapısız 27/30 zafer (3 dağılma); kapıya en iyi cevabı
  veren bot (etkileri bilerek seçer, ölümcül kapıyı hiç açmaz) 30/30, kapıdan harekât başına +5,6 güven; kapıyı hiç açmayan
  24/30 (6 dağılma), −8,7 güven. Harekât başına ~24 ziyaret. İlk denemede (erzakla yardım güven kazandırıyordu) en iyi cevap
  +24 güven topluyordu ve güven hiç düşmüyordu: botun sığınağında erzak hep tavanda, erzak bedeli bedava güvene dönüşüyordu.
  Bu yüzden muhtaç ziyaretçiye erzakla yardım güven kaybını önler ama kazandırmaz; güven fon, malzeme, tek seferlik ya da
  sonucu kötü olabilen seçimlerle kazanılır.
- **Güven tavanı 40 → 20** (kullanıcının seçimi; `liberationTrustMax`, `BalanceConfig.asset`). Aynı bot, 30'ar harekât:
  kapısız 20/30 (10 dağılma), kapıya en iyi cevap 21/30 (9 dağılma, kapıdan +4,2 güven), kapıyı hiç açmamak 17/30 (13 dağılma).
  Güven her harekâtta 20'ye değiyor; ortadaki bölgelerin güveni tavanda boşa gidiyor, dağılmalar harekâtın sonunda (gün
  49–76, kazanılanlar ortalama 62. günde bitiyor), karşı saldırı ve kırılan siviller birikimi eritiyor. Harekât sonu güveni
  ortalama 27 → 10. Kapı artık oyunu kolaylaştırmıyor (en iyi cevap kapısızla neredeyse aynı) ama ihmal edilirse 3–4 harekâta
  mal oluyor. Denge değeri kayıtla saklandığından var olan kayıtlar kendi tavanıyla sürer; kayıt biçimi değişmedi.
- **Doğrulama:** 310/310 test (yeni: `DoorTests`, `SiginakDoorTests`). Play'de: yeni harekât, iki vardiya, Nermin çaldı;
  bak, sor, aç, "Keseyi al" (erzak +6), kapat, masaya dönüş; teleks "KAPI AÇILDI" ve "KAPI AÇILMADI · Dışarıdan bir aile ·
  Güven −1". Kopya: aç → "SIZMA" damgalı sonuç kâğıdı, son rapor ancak "Son rapor"la açıldı. Kayıtlar ve eğitim bayrağı
  geri yüklendi.

## Sivillere saldıran makineler ve harekât dengesi (2026-10-28)

Kullanıcı "robotlar Teknisyen, EMP uzmanı ya da Analiste ulaşamıyorsa sivillere saldırsın" dedi. Kural kaldı; harekâtı
kazanılabilir tutmak için (aşağıdaki ölçümlerden sonra kullanıcının seçimi) üç değişiklik birlikte geldi:

- **Analistin kancası** çarptığı robota 1 hasar verir, sağ kalırsa çeker; dost uzmanı hasarsız çeker (`Baskin.ApplyAbility`).
  Kart efekti "−1 robot · çeker". Kilitli bir ikmal savaşının beklenen sonucu güncellendi (bakım robotu 4,3 yerine 4,4'te düşer).
- **Ayakta kalan siviller güveni geri kazandırır:** kazanılan baskında tahtada ayakta kalan her `liberationCiviliansPerTrust`
  (2) sivil sunucu +1 güven (`RaidResult.CiviliansLeft`, `LiberationRaidReward.CiviliansLeft/TrustFromCivilians`).
  Kaybedilen baskında ödül yok. Operasyon raporunda "Ayakta kalan sivil sunucu" satırı.
- **Güven tavanı** `liberationTrustMax` 40 (başlangıç 10 kaldı); `Simulation.TrustMax` sayaçlarda, raporda ve baskın
  HUD'unda kullanılır. `RaidResult.Encode` sekizinci alanı yazar; kayıt biçimi 9 (eski kayıtlar yüklenmez).
- Kurallar kartındaki "makineler sivile nişan almaz" cümlesi düzeltildi. Testler: `LiberationCampaignTests`
  (`LiberatedRegion_RestoresTrust_UpToTheCap`, `CiviliansLeftStanding_WinBackTrust_OnlyWhenTheRaidIsWon`,
  `RaidResult_CarriesCiviliansLeft_ThroughTheRecord`), `DeskBridgeTests.RaidReport_ShowsTrustLoss_AndWarnsWhenLow`.
  Oyunda denendi: kanca dronu 2 → 1 can yaptı; rapor "Ayakta kalan sivil sunucu 4", "Sivil güven +5 · 16/40".

- **Kodda (gelişmiş akıl):** makine gidebileceği hiçbir kareden ekibin bir üyesini vuramıyor ya da kilitleyemiyorsa
  (`Baskin.CanReachTeam`) sivil sunucuyu hedef alır (`ScoreTarget(..., hostage)`: +9, patlamada +6); turda en çok bir
  makine (`civTaken`). Temel akıl (eğitim, kilitli testler) değişmedi. Testler: `RaidEngineTests.SmartMachines_TargetCiviliansOnlyWhenTheTeamIsOutOfReach`.
- **Ölçümde bulunan eski bozukluk:** önceki turların zorlaştırmaları (asıl olarak ikmal kaçışı → çok başarısız baskın →
  alarm ve karşı saldırı, uzayan harekâtın sivil kayıpları) harekâtı kural olmadan da bozmuştu: orta oyuncu 0/10, dikkatli
  2/10 (önceki ölçüm 9/10 ve 10/10). Yeni denge değerleri: `liberationLootExtraTurns` 2 (ikmal 8 tur), `liberationAlarmPerFailure`
  3 → 1, `liberationTrustPerRegion` 2 → 3, `liberationCivilianHp` 2 (yeni; kampanyada sivil sunucu 2 can). Bunlar kayıtla
  saklandığı için eski kayıtlar eski değerlerle sürer.
- **Harekât ölçümü** (bot, Unity'siz Core, 10'ar harekât; orta = sivil ağırlığı 40, dikkatli = 150; "kazanılan" = Silme):

  | Ayar | Orta | Dikkatli |
  |---|---|---|
  | Kural kapalı, siviller 1 can, yeni denge | 5/10 | 7/10 |
  | Kural kapalı, siviller 2 can, yeni denge | 10/10 | 9/10 |
  | Kural açık (turda bir), siviller 2 / 3 can | 0/10 / 0/10 | 0/10 / 1/10 |
  | Kural açık + EMP susturur / Teknisyen sivili güçlendirir / ikisi | 0 / 0 / 1 | 0 / 0 / 0 |
  | Kural açık + kayıpsız baskına +1 güven (her baskın ya da yalnız başarılı) | 0/10 | 0/10 |
  | Kural açık + Analistin kancası robota 1 / 2 hasar (yalnız bitişiğe 1: 0/10) | 1/10 / 1/10 | 1/10 / 1/10 |
  | Kural kapalı + Analistin kancası robota 1 / 2 hasar | 9/10 / 10/10 | 10/10 / 10/10 |
  | Kural açık + güven tavanı 20 (başlangıç 10 / 15 / 20) | 0 / 0 / 0 | 0 / 0 / 0 |
  | Kural açık + güven tavanı 30 (başlangıç 10 / 20 / 30) | 0 / 0 / 0 | 0 / 0 / 0 |
  | Kural kapalı + güven tavanı 20 / 30, başlangıç = tavan | 10/10 / 10/10 | 9/10 / 10/10 |
  | Kural açık + kanca 1 hasar + tavan 40 + ayakta kalan her sivil +1 güven | 10/10 | 10/10 |
  | **… aynısı, ayakta kalan her 2 sivil +1 (yalnız başarılı baskında) — seçilen** | **8/10** | **8/10** |
  | … aynısı, ayakta kalan her sivil +1, baskın başına en çok +2 / +1 | 10/10 / 8/10 | 9/10 / 6/10 |
  | Kural açık + makinenin kırdığı sivil güvene sayılmaz | 10/10 | 10/10 |

  Kural açıkken makineler baskın başına ~1 sivil kırıyor (kapalıyken 0,05–0,09); güven her ayarda tükeniyor. Kanca hasarı
  bunu yalnız 0,98 → 0,72–0,88'e indiriyor; kural kapalıyken baskın başarısını %59–61'den %66–71'e çıkarıyor (oyun kolaylaşır).
  Güven tavanı işe yaramıyor: kural açıkken güven sürekli azaldığı için tavana hiç varılmıyor; 20'den başlamak dağılmayı
  ~25 baskından ~35 baskına, 30'dan başlamak ~33–61 baskına erteliyor (o arada 42–60 sivil gidiyor). Kazanmak 44–83 baskın
  sürüyor (12 bölge × 3 = +36 güven), yani kural açıkken güven bütçesinin ~60–80 olması gerekir.
  Ayakta kalan sivil ödülü bunu sağlıyor (baskın başına ~3,6 sivil ayakta kalıyor, ~1 kırılıyor). Her sivil +1 olunca güven
  hiçbir harekâtta başlangıcın (10) altına inmiyor, tehdit kalmıyor; her 2 sivil +1 olunca harekâtların 2/10'unda sıfıra
  iniyor (en düşük güven ortalaması 7–8). Baskın başarısı %60–65, harekât 44–101 baskın.

## Sunucu görevi sertleşti (2026-10-28)

Kullanıcı sunucu görevlerinin de zorlaştırılmasını istedi (önceki turda bot %89 kazanıyordu).

- **Ayarlar** (`Balance.liberationServerTargetHp`, `liberationServerMachines` → kampanyada sunucu baskınının
  `RaidParams.TargetHpBonus`, `ExtraMachines`; ikmalde ve havuzsuz baskında 0). Ölçüm (720 sunucu baskını, gelişmiş akıl):
  bugünkü %89; düğüm +1 can %83; **+2 can %76** (kademe 1/2/3 %83/%81/%64); +1 makine %89 (etkisiz); +1 can +1 makine %82.
  Seçilen: +2 can, 0 makine. Son bölgenin çekirdeği hariç (5 + 2 = 7 can 6 turda dört sabotaj ister, yetişilmez).
- **Toplam** (1440 baskın, bütün değişiklikler): **%72** (ikmal %67, sunucu %76; kademe 1/2/3 %79/%75/%61); önceki oyun
  her kademede ~%92.
- **BalanceConfig.asset** yeni alanlarla kaydedildi; Unity sınıfta olmayan 139 eski alanı attı ve asset'te yazılı olmayan
  23 güncel alanı kod varsayılanıyla yazdı. Var olan hiçbir değer değişmedi (karşılaştırıldı).
- **HUD:** hedef kartı can noktalarını 4'te kesiyordu (5 canlı çekirdek ve zırhlı düğüm de eksik görünüyordu); hedef başına 8
  nokta. Play'de kampanya sunucu baskını: 6 tur, düğüm 6/6, kartta 6 nokta; konsolda hata yok.
- **Testler:** `RaidEngineTests.ServerHardness_*` (düğüm canı ve ek makine; kasa ve çekirdek hariç),
  `LiberationCampaignTests.ServerRaid_CarriesTheServerHardness_LootRaidDoesNot`. EditMode **279/279**.

## Harita torbası, ikmal kaçışı, makine aklı (2026-10-28)

Kullanıcı "3. bölgeye geçtiğimde aynı harita gelmeye başladı, oyun çok kolay, düşman yapay zekasını geliştirmeliyiz, ikmal
görevlerinde ikmal alındıktan sonra geri kaçış olmalı" dedi.

- **Tekrar:** seçim `(Compute + RaidNumber + (Seed & 7)) % 6` idi; tohumun son üç biti 0–7 olduğundan iki harita iki kat
  sık geliyordu (kullanıcının kaydında 10 baskının 5'i havuzun aynı haritası). Şimdi havuz 10 harita (14 oda adı) ve
  `RaidMaps.Slot`: karıştırılmış torbalardan sırayla, her harita bir kez gelmeden tekrar yok, torba arasında art arda da
  yok (`RaidParams.MapSlot = S.Raids`). Aynı kayıtta 10 baskın 10 farklı harita.
- **Kaçış:** kasayı açan Teknisyen taşıyıcı olur (`BState.Carrier`); giriş kareleri çıkış (`Exits`). Taşıyıcı çıkışa
  (yürüyüş, kanca, itme; makine evresinde de) varınca kazanılır; düşerse ya da süre biterse kaybedilir. İkmal görevi 7 tur
  (kısa nöbet bir tur az; açıklaması "bir tur kısa"). Sunum: yeşil kesik çıkış çerçevesi, "YÜKLÜ" etiketi, HUD "girişe
  dön", çıkış kartı; HUD tur şeridi 8 kutuya kadar (prefab'a iki kutu, plaka tur sayısıyla kayar).
- **Makine aklı:** `RaidParams.Tactics` (kampanya 1; eğitim, örnek baskın ve kilitli motor testleri 0). Gelişmiş akıl kilit
  kareleri (hedefin önü; kasa alındıysa taşıyıcının yolu ve çıkışlar) ateş hattıyla ve bedenle tutar, vuracak kimse
  yokken dolaşmaz, taşıyıcıyı kovalar ve önceliklendirir, makineler tuttukları kareleri paylaşır (eşgüdüm). RNG tüketimi
  aynı, yalnız seçim değişir.
- **Ölçüm** (açgözlü bot, Unity'siz Core, havuz haritaları, 10 harekât × 12 bölge × 3 kademe × 2 görev × 2): önceki %92.
  Kaçış + temel akıl %73 (ikmal %56); + gelişmiş akıl %68 (ikmal %45, sunucu %90); + eşgüdüm aynı. İkmal süresi: 6 tur
  %67 toplam/%45 ikmal, **7 tur %78/%67** (kademe 1/2/3: %85/%80/%70), 8 tur %85/%80. 7 tur seçildi. Sunucu görevi
  gelişmiş akılla değişmedi (%89): niyetler önceden göründüğü için Teknisyen tutulmayan kareden sabotaj yapabiliyor.
- **Eğitim:** senaryo temel akılla ve elle çizilmiş haritada kalır (`TutorialRaid.For` `Tactics`/`MapSeed` kopyalamaz).
  Kaçış için devam aramayla bulundu (en ucuz hasarsız devam, ışın araması): kasadan sonra EMP (3,5) → hasarlı dron
  duvara itilip düşer, Analist kenara itilir; 4. turda Teknisyen (3,4), 5. turda çıkış (0,4). 30 adım, 5 tur; dersler
  eklendi. Play'de gerçek fare/klavyeyle 30 adım, zafer, 3/3 · 3/3 · 2/2, sivil 0. 23. adımda hedef karenin sol kenarındaki
  bir örnek nokta öndeki duvara düşüyor (merkez ve çerçevenin kalanı tıklanır).
- **Testler:** kilitli ikmal savaşları kaçışla yeniden kilitlendi (tohum 11 ve 99, 5. turda çıkış); `Escape_*` (çıkışa
  varan kazanır; düşen taşıyıcı ve süre kaybettirir), `LootPreview` (kasa açmak kazandırmaz), torba testi, sunum testi
  kaçışla, eğitim 30 adım/4 tur bitirme. EditMode **277/277**.
- **Play'de:** eğitimsiz kampanya ikmal baskınında 7 tur kutusu, kasa alınmış hâlde çıkış çerçeveleri, YÜKLÜ, "girişe dön",
  ALINDI. Konsolda hata yok.
- **Açık:** sunucu görevinin zorluğu (bot %89) değişmedi; istenirse hedef canı, onarım ya da kademe başına makine ile
  ayarlanır. Ölçüm düzeneği (bot) depo dışında, scratchpad'deydi.

## Alarm panosu ve harekâtın harita havuzu (2026-10-27)

Kullanıcı "alarm durumunu göremiyorum, her yeni oyunda yeni bir harita havuzu oluşturulabilir mi" dedi.

- **Alarm neden görünmüyordu:** yalnız masa kenarındaki yazı şeridindeydi (EdgeLine) ve o şerit sayaç rafı kadraja girince
  kapanıyor; masa pozunda hiç görünmüyordu (CRT'nin alt satırı küçük). Rafa yedinci modül sığmıyor (masa üstü ±0,92, raf
  ±0,80, sağda lamba ve CRT masası), bu yüzden **arka duvara YZ ALARMI panosu** (`AlarmPanel`): rafla aynı mekanik sayaç,
  eşiğe 3 basamak kala yanan, son basamakta yanıp sönen kırmızı ışık (`beaconOff` → `mastRed`), vardiyada fark fişi,
  üzerine gelince eşik ve bu vardiyanın artışı (`Loc`'lu satır `SiginakDesk`'ten). Eğitimin ilk notu panoyu gösterir.
  Sahne tek seferlik betikle kuruldu; Unity'nin kayıtta yazdığı TMP rengi ve düzen grubu çapa artıkları geri alındı, sahne
  farkı yalnız ekleme. Testler: `SiginakDeskTests.AlarmPanel_*`, `DeskGameScene_AlarmPanelIsWired`.
- **Harita havuzu:** `RaidMaps.Pool(MapSeed)` yeni oyunun tohumundan 6 harita üretir (kendi üreteci, `S.Rng`'den tüketmez);
  `RaidParams.MapSeed = S.Seed`, `Baskin.Create` havuzdan seçer. `MapSeed` 0 ise elle çizilmiş üç harita: eğitimin senaryolu
  baskını (`TutorialRaid.For` alanı kopyalamaz), örnek baskın ve kilitli motor testleri değişmedi. Kayıt sürümü artmadı:
  tahta stratejiye yalnız `RaidResult` verir, komut sonuçları ve RNG sırası aynı. Eski bir kayıtta süren baskın yeniden
  açılırsa tahtası havuzdan gelir (zararsız; baskın henüz kayda girmemiştir).
- **Zorluk ayarı (açgözlü bot, Unity'siz Core, 10 harekât × 12 bölge × 3 kademe × 2 görev × 2 = 1440 baskın):** elle çizilmiş
  haritalar %89,9 kazanma, 0,59 sivil kaybı, 4,37 tur. İlk üreteç %97,5 / 3,40 tur (fazla kolay); dron hattı kuralı ve
  hedefin yanına korunak duvarı tek başına değiştirmedi (%96,9, %97,1). Harita başına ölçüm (60 harita × 24 baskın) farkın
  girişten hedefe uzaklıkta olduğunu gösterdi (7 adım %95 → 10 adım %78); hedef 6. sütuna sabitlendi, uzaklık 7–10 oldu:
  **%92,0 kazanma, 0,59 sivil kaybı, 4,04 tur** (harita adı başına %86–99). 200 tohumda 1200 haritanın hepsi kurallara
  uyuyor ve farklı. Testler: `RaidMapsTests` (aynı tohum aynı havuz, iki harekât harita paylaşmaz, 60 tohumda kurallar,
  havuz yalnız `MapSeed` ile, harekât baskını tohumu taşır). Yeni oda adları çevrildi.
- **Play'de denendi:** masa pozunda pano (0/10, ışık sönük), alarm 8'de ışık yanık, 9,2'de yanıp söner, üzerine gelince
  açıklama; vardiya şalterinde alarm 0 → 1 ve "+1" fişi. Eğitimsiz yeni harekâtın ilk baskını havuzdan ("Arşiv katı")
  açıldı, tahta doğru kuruldu. Konsolda hata yok.
- **Açık:** havuz boyutu (6) ve üretecin ölçüleri `RaidMaps`'te sabit; dengeye taşınmadı. Masadaki diğer `DeskTarget`
  açıklama satırları çeviri aracına girmiyor (ör. TELEKS "Son emirler ve sonuçlar" İngilizcede Türkçe kalır); alarm panosu
  bu yüzden satırını koddan yazar.

## Eğitim: senaryolu ilk baskın (2026-10-27)

Kullanıcı "ilk baskında tüm hareketleri adım adım oynatarak bitirelim" dedi.

- **Senaryo:** `TutorialRaid` (Bridge/Siginak). Harekâtın ilk baskını (weu_1, kademe 1, "guvenli", imza zdron, tam ve
  sağlam kadro) eğitimin taktik tahta adımında açılırsa tohum 129 ile kurulur. Tohum taranarak seçildi (bot araması):
  kanca bir robotu çeken "temiz" bir hat bulunamadı; seçilen hat EMP itişiyle zırh kırmayı, kancayla dost çekmeyi,
  makinelerin birbirini vurmasını, atış hattından kaçmayı ve kasayı almayı öğretir; üç turda hasarsız zafer.
- **Rehber:** `RaidGuide` yalnız sıradaki adıma izin verir (`RaidDataSource` Select/Move/Arm/Use/EndTurn'ü süzer; geri alma,
  geri çekilme, Esc ile Q'yu ya da seçimi bırakma kapalı; Geri al düğmesi sönük; tur bitirme onayı sorulmaz). Beklenen hamle
  yapılamazsa rehber kalkar (uyarı loglanır). Tahtada `RaidBoard3D` nabız gibi atan sarı çift çerçeve (`Marks.Guide`).
  Not "BASKIN · ADIM n/21": talimat + ders; makineler oynarken "Makineler sırayla oynuyor".
- **Eğitim akışı:** baskının beş adımı (seç, yürü, yetenek, turu bitir, süre) tek "raid" adımına indi (rehbersiz baskın için
  genel metin); 16 → 12 adım. Not baskın bitince de sağ üste çıkar (sonuç kartı sağ altta).
- **Kayıt:** simülasyon yalnız `RaidResult`'ı görür; tahtanın tohumu sunumda değişir, Core ve kayıt biçimi aynı.
- **Testler:** `TutorialRaidTests` (beş harekât tohumunda rehberin adımları yapılabilir ve hasarsız zafere varır; adım
  açılımı; yalnız taze ilk baskın senaryolu). `TutorialFlowTests` 12 adıma göre. EditMode 206/206.
- **Play'de denendi:** yeni harekât → fiş → emir → senaryolu baskın (tohum 129, rehber açık); yanlış uzman, yanlış kare,
  geri alma, geri çekilme ve erken tur bitirme engellendi; 21 adım sırayla, zafer, 3/3 · 3/3 · 2/2, sivil kaybı 0; rapor
  adımına geçiş. Konsolda hata yok.
- **Düzeltme:** kullanıcı teknisyeni seçtikten sonra kareye tıklayamadı. Sebep eğitimde değil: baskında odanın kamerası
  kapanıyor ama kameradaki `PhysicsRaycaster` açık kalıyordu; kapalı kameradan atılan ışın tahtanın parçalarına (burada
  6,0'daki üreteç) çarpınca `IsPointerOverGameObject` doğru dönüyor, `RaidBoard3D` tıklamayı arayüze ait sayıp yok
  sayıyordu (her baskında ekranın bazı bölgeleri tıklanmaz olabiliyordu). Işın `RaidLauncher.hideDuringRaid`'e eklendi;
  `SiginakDeskTests` kilitliyor. Önceki doğrulama hamleleri doğrudan çağırdığı için yakalamamıştı; bu kez 21 adım Input
  System'e kuyruklanan gerçek fare ve klavye olaylarıyla oynandı, zafer; baskın sonrası odanın ışını geri açık.
- **Düzeltme 2:** EMP atışında duvara tıklama tutmadı. Duvar karesinin çarpıştırıcısı zemin kalınlığındaydı (y −0,08..0),
  blok ise 0,55 yüksek: bloğun tepesine tıklayan ışın içinden geçip arkadaki kareyi seçiyordu. `Wall.prefab`'ın kutusu
  bloğu kapsar (y −0,08..0,55; `SadeRaidBoardTests.TilePrefabs_CollidersReachWhatTheyShow`). Blok artık arkasındaki
  karenin yarısını örttüğü için `RaidBoard3D.Pick` kuralı: vurgusuz duvar arkasındaki vurgulu kareyi (yürüme, hedef)
  bırakır; eğitim çerçevesinin karesi ışının üstündeyse önünde ne olursa olsun seçilir; ışın zemine değince tarama
  biter. Play'de 21 adım oyuncunun tıklayacağı yerlerden (uzmanın gövdesi, duvarın tepesi) oynandı; her hedefte 20
  noktanın hepsi doğru kareye düşüyor (yalnız seçili uzman kartının örttüğü bir nokta arayüze gider). 1920×1080'de denendi.
- **Açık:** Esc rehberde Q'yu bırakmadığı için baskını duraklatır (duraklatma menüsü); yeniden Esc ile sürer.

## Eğitim: rehberli ilk vardiyalar (2026-10-27)

Kullanıcı "tutorial eklememiz gerekiyor" dedi; dört biçimden (rehberli ilk vardiyalar, ilk karşılaşma notları, ayrı eğitim
baskını, kılavuz el kitabı) rehberli ilk vardiyaları seçti.

- **Akış:** yeni harekâtta sağ altta analistin notu, 16 adım (masa → baskın → rapor → sığınak → şalter → vardiya raporu).
  Her adım oyuncunun eylemini bekler; zaten yapılmış adımlar geçilir; ilk ve son adım TAMAM ile. "Eğitimi atla" ya da
  kapanış bir daha göstermez (PlayerPrefs `Corrigible.TutorialDone`; editörde Corrigible › Eğitim › Eğitimi sıfırla).
  Kayıttan devamda eğitim yok. Simülasyona dokunmaz.
- **Kod:** `TutorialFlow` (saf C#: adımlar, koşullar, rapor açıldı mı mandalları), `SiginakTutorial` (bağlayıcı: masa,
  baskın tahtası, raporlar ve sığınaktan durumu okur), `TutorialNoteView` (kâğıt kart; görev fişinin malzemeleri ve
  yazıları). Sahnede kökte `SiginakTutorial` tuvali (sıralama 70); sahne farkı yalnız bu nesneler ve kök listesi.
- **Testler:** `TutorialFlowTests` (adım sırası, geçilen adımlar, raporun açılıp kapanması, atlama, sahne bağlantıları). EditMode 203/203.
- **Play'de denendi:** 16 adımın hepsi sırayla; not görev fişiyle ve baskındaki TURU BİTİR ile çakışmıyor, sığınak pozunda
  revir kartının köşesine biniyor. Konsolda hata yok; kayıtlar ve eğitim bayrağı geri yüklendi.
- **Açık:** ayarlardan eğitimi yeniden açma yok (yalnız editör menüsü); masadaki notlar hedefi işaretlemiyor (yazıyla
  yönlendirir; baskında sarı çerçeve var);
  "TAMAM"ın İngilizcesi tablodaki ortak çeviri (DONE).

## Bölge kimliği ve zorluk eğrisi (2026-10-27)

Oyun incelemesinin dördüncü önerisi. Kullanıcı: "zorluk ve gelişim zamanla artsın, her bölgenin farklı özelliği ve en az
bir farklı düşmanı olsun"; seçenekler arasından "6 yeni tür, ikişer bölge" ve "oda seviyesi bölgeyle açılır"ı seçti.

- **Zorluk kademesi** (özgür bölge 0–1 / 2–5 / 6–9 / 10–11): kademe başına başta bir makine fazla; 3. kademede kopyalama
  (turda iki uyanma) ve iki imza makinesi; 4. kademede makinelerin canı +1. Eski kural 4 bölgeden sonra artmıyordu.
- **Bölge kimliği:** 12 bölgenin her birine kendine özgü tahta özelliği (Güvenli giriş, Dar koridor, Kısa nöbet, Takviye,
  Açık arazi, Kum fırtınası, Soğutma ağı, Kalabalık şehir, Zırhlı dolap, Elektrik şebekesi, Kopya fabrikası, Hızlı
  devriye) ve imza makinesi. Altı yeni makine (her biri iki bölgede): zırhlı dron, havan robotu (engel üstünden 3 kare),
  patlayan robot (çevresine patlar), kilitleyici (uzmanı bir tur yürütmez), kalkan dronu (makineye zırh), çoğaltıcı (dron
  uyandırır). Duvar ve nesne ekleyen özellikler yolu kapatmaz.
- **Gelişim:** oda seviyesi 2 için 2, seviye 3 için 5 özgür bölge; başta yalnız onarım (seviye 1).
- **Ölçüm:** ilk ayar (kademe 3: 5, kademe 4: 9 bölge) tam sığınaklı dengeli botu 2/10'a düşürdü; kayıpların çoğu
  sivil güvenden. Eşikler 6 ve 10'a, bölge güveni +1 → +2'ye çekildi. Son hâl (10'ar harekât; saldırgan / dengeli /
  koruyucu): tam sığınak 1/10, 5/10, 8/10; yaralıyken dinlenen 1/10, 9/10, 8/10; sığınaksız 0/10, 1/10, 1/10. Kademe
  başına baskın başarısı ikmalde %98 → %59, sunucuda %95 → %50.
- **Sunum:** altı yeni makine prefab'ı (dron ve bakım robotundan türetildi; ilk `Art/Raid/Enemies` malzemeleri sonra `Analyst/Materials/Robot*` ile değişti;
  `RaidPieceLibrary`'de yeni yuvalar), tahtada ZIRHLANDI, KİLİTLENDİ, patlama efekti ve KİLİTLİ etiketi; patlama alanı
  tehlike karesi, kalkan onarım işareti, çoğaltma uyanma halkası; kum fırtınasında uyanma halkaları gizli. Görev çipinde
  bölge özelliği, açıklamasında imza makinesi ve kademe; düşman ipucu kartında makinenin açıklaması, zırhı, imza işareti.
  Görev fişinde "Bölge: … · İmza: … · Tehdit n/4", bilgi ve cephe onay fişinde açıklamalı kimlik. Sığınak panosunda kilitli
  seviye (KİLİTLİ, gerekçe). Ekip ve birim kartında zırh plakası için mavi nokta. Telsiz odasında jeneratör tankları yerine
  telsiz masası, cihaz, mikrofon ve anten.
- **Kayıt biçimi 8.** `RaidParams.Inf` ve `liberationRaidThreat*`, `liberationCopyFromRegion` kalktı.
- **Testler:** `RegionIdentityTests` (bölge benzersizliği, kademe eşikleri, özelliklerin tahtaya etkisi ve yolun açık
  kalması, altı makinenin niyetleri, oda kilidi); kopyalama testi kademeye göre; eski testler oda kilidine göre.
  EditMode 197/197.
- **Play'de denendi:** görev fişi kimliği, telsiz masası, zırh noktası, altı makinenin tahtada görünüşü ve niyet okları,
  Japonya-Kore'de 3. kademe baskın (fazladan kablolar, iki kilitleyici) ve bir düşman evresi, sığınak panosunda seviye
  kilidi. Konsolda hata yok; kayıt dosyaları geri yüklendi.

## Hazırlık sığınağı (2026-10-27)

Oyun incelemesinin bulgusu: sığınak harekâtın sonucunu değiştirmiyordu. Botla üç plan (hiç dokunma, yalnız revir, her şeyi
kur) aynı harekâtı oynadı; yaşam alanı, jeneratör ve atölye sonuca hiç dokunmuyor, revir yalnız bir vardiyalık zamanlama
kaydırıyordu. Malzeme yalnız oda geliştirmeye gidiyordu, fon 450–770 artıkla bitiyordu, ekip baskında 8 candan ortalama
1'ini kaybediyor ve neredeyse hiç düşmüyordu. Kullanıcı sığınağı baskının hazırlık yerine çevirmeyi seçti, önce botla ölçüm.

- **Kural:** uzmanlar ayrı izlenir. Baskında alınan hasar yara olarak kalır (sonraki baskına eksik canla); düşen uzman
  3 gün yatar, yerine yaşam alanının yedeği (bir eksik canla) çıkar; Teknisyen yatıp yedek de yoksa baskın yok. Baskına
  çıkmayan vardiya her uzman 1 yara toparlar. Revir görevli başına 0,34 yara sarar ve yatanları erken kaldırır; atölye
  malzemeden zırh plakası döker (0,25/görevli, 4 malzeme), plaka ilk hasarı karşılar; yaşam alanı seviye başına bir yedek
  tutar; jeneratörün yerine telsiz odası (görevli başına vardiya alarmı −0,05). Enerji kalktı, atölye malzeme üretmez.
- **Ölçüm** (deneme Core'u; dengeli ve koruyucu bot): ilk denemede tam sığınak 10/10, karşı saldırı 0 (telsiz alarmı
  siliyor, revir ve plakalar bütün yarayı karşılıyordu). Ürünler kısıldı; üç zorluk varyantından kullanıcı "Orta"yı seçti:
  revir 0,34, alarm eşiği 14 → 10, telsiz −0,05, fon ödülleri kısık (ikmal 22 → 12, sunucu 10 → 8, bölge 30 → 15).
  Gerçek Core ile son ölçüm: tam sığınak 8/10 ve 8/10, sığınaksız 1/10, revirsiz 2/10 (yaralıyken dinlenirse 9/10),
  atölyesiz 3–4/10, telsizsiz 6/10; saldırgan oyuncu hep 0/10 (güven). Fon 1–5 vardiya baskını bekletiyor.
- **Sunum:** pano özetinde UZMANLAR ve PLAKA, odalarda DURDU damgası (atölye deposu dolu ya da malzemesiz, revir
  açlıkta) ve yeni notlar; görev fişinde kadro ("T 3/3 · E 2/3 · A 2/2 · 1 plaka") ve TEKNİSYEN YARALI damgası; CRT ve harita
  figürü Teknisyen yatıp yedek yokken sığınakta, yatan uzmanın piyonu yatık; vardiya raporunda "Zırh plakası" üretim satırı
  ve ekip satırında yara (dinlenme, revir, yatanlar); operasyon raporunda "Düştü", "Yeni yara", "Zırh plakası kırıldı";
  baskın tahtasında "PLAKA KIRILDI", birim kartında Zırh plakası ve Yedek uzman satırları, kadroda olmayan uzman YARALI.
- **Kayıt biçimi 7:** baskın sonucu kadronun kalan canını ve plakasını taşır. Sahnede jeneratör odasının kimliği
  `radio` (tek satır), `BalanceConfig.asset`'te değişen ödüller ve yaralanma süresi.
- **Testler:** `ShelterTests` (plaka dökümü, revir, yedekler, telsiz), `LiberationCampaignTests` (yara taşınır ve dinlenmeyle
  iner, yedek ve Teknisyensiz baskın, plaka dağıtımı, sonuç kodlaması), `RaidEngineTests` (kadro, plaka), sunum testleri
  yeni satırlara göre. EditMode 184/184.

## YZ alarmı ve bir baskın bir vardiya (2026-10-27)

Oyun incelemesinin ikinci önerisi: zaman baskısı. Kullanıcı "önce botla ölç" dedi.

- **Bulgu 1:** ekip baskından döndüğü an hazırdı (`ResolveRaid` `Busy`'yi sıfırlıyordu). Zincirleyen bot bütün harekâtı
  1–3 vardiyada bitirdi; vardiya şalteri yalnız yaralıyken gerekiyordu. Vardiya başına alarm anlamsızdı.
- **Bulgu 2:** sessiz vardiyanın alarmı düşürdüğü kural (B) dikkatli botu hiç saldırı yemeden bedelsiz bekletti.
- **Seçilen (A6):** baskın vardiyayı doldurur; alarm vardiya +1, başarısız baskın +3, kırılan YZ sunucusu −4; 14'te alarm
  sıfırlanır, YZ en son geri alınan sektörü yeniden işgal eder, sivil güven −1. Bot (10'ar harekât, son kurallar):
  saldırgan 0/10, dengeli 6/10, koruyucu 9/10 zafer; harekât başına ~2 karşı saldırı. Denenen diğer eşik ve değerler
  (A1–A8) daha sert ya da etkisizdi.
- **Açık kalan:** vardiya şalteri hâlâ gerçek bir karar değil (vardiyayı harcayacak başka iş yok). "Sessiz vardiya +
  YZ zamanla güçlenir" ve "karşı saldırı sığınağa baskın" seçenekleri ölçülmedi.
- **Sunum:** CRT'de YZ ALARMI n/14 ve "ALFA · DÖNDÜ · SONRAKİ VARDİYA"; kenar satırında ALARM; vardiya raporu bölümü
  EKİP VE YZ ALARMI (alarm satırı, ekip simgesi gizli; karşı saldırıda KARŞI SALDIRI ve teleks); operasyon raporunda
  "YZ alarmı +3 · 3/14", eşikte "EŞİK AŞILDI" ve karşı saldırı satırı; görev fişinde "SONRAKİ VARDİYA"; son raporda
  kırmızı damgalı KARŞI SALDIRI kartı; karşı saldırıda alarm sesi. Aynı vardiyada ikinci baskın teleksle reddedilir.
- **Kayıt biçimi 6.** Testlerde art arda baskınlar arasında vardiya biter (`Ready`); "boşta 400 vardiya harekâtı
  bitirmez" testi kaldırıldı: artık bitirir (karşı saldırılar güveni tüketir).
- **Sahne:** vardiya raporunun iki ekip satırına simge başvurusu (`TeamSlot.icon`), sahne farkı 2 satır.
- **Play'de denendi:** başarısız baskın raporu, aynı vardiyada ikinci baskının reddi, CRT, vardiya raporu, eşikte karşı
  saldırı (Batı Avrupa · Tedarik deposu yeniden işgal, güven 9/10, teleks). Konsolda hata yok; kayıt dosyaları geri yüklendi.

## Sivil güven ve kaybetme koşulu (2026-10-27)

Oyun incelemesinin bulgusu: harekâtta kaybetmek mümkün değildi, fon hiç darlık yaratmıyordu, kırılan sivil sunucuların
sonucu yoktu. Kullanıcı ilk öneriyi seçti: sivil güven sayacı ve kaybetme koşulu, önce botla denge ölçümü.

- **Bot:** Core Unity'siz derlenip (`mono`) açgözlü bir taktik botla bütün harekâtlar oynatıldı (projede değil, oturumun
  geçici klasöründe). Bot bir düşman evresi ileriye bakar; sivil ağırlığı ayarlanarak "dikkatsiz" ve "dikkatli" oyuncu taklit
  edilir.
- **Bulgu:** eski kuralda makineler oyuncuya nişan alamayınca sivil sunucuya ateş ediyordu; dikkatli bot da baskın başına
  ~2,5 sunucu kaybediyordu (dikkatsiz 3,1). Hiçbir başlangıç değeri dikkatliyi hayatta tutup dikkatsizi kaybettirmiyordu.
  Beş makine davranışı denendi; kullanıcı "makineler sivillerden kaçınsın"ı seçti (dikkatli ~0,3, dikkatsiz ~0,6).
- **Kural:** güven 10 (tavan), kırılan sivil sunucu başına −1, özgürleşen bölge başına +1, sıfırda Dağılma sonu. Makineler
  sivil hattına bilerek ateş etmez; kaçılan atış, EMP, itme ve patlama sivili yine kırar. Ayrıntı: `OYUN-TASARIMI.md`.
- **Son ölçüm** (10'ar harekât, son kurallarla): dikkatsiz 10/10 dağıldı (16–41. vardiya), orta 9/10 ve dikkatli 10/10 kazandı.
- **Sunum:** sayaç rafına altıncı sayaç GÜVEN x/10 (modüller 0,26 m aralığa sıkıştırıldı); kenar satırında GÜVEN; baskın
  HUD'unda SİVİL yerine "GÜVEN n" (bu baskında kırılanlar düşülmüş); kurallar kartı ve emir belgesinde kural; operasyon
  raporunda "Sivil güven" satırı ve 3'ün altında uyarı; son raporda KAYBEDİLDİ damgası ve "YZ kazandı.".
- **Bu sırada düzeltilen:** HUD'da üç sivil yuvası var ama tahtada 4–5 sivil sunucu oluyor; kırık simge ancak üçüncü
  kırılandan sonra görünüyordu. Artık ilk kırılan işaretlenir.
- **Kayıt biçimi 5:** yeni son eski kayıtların sonucunu değiştirebileceği için biçim 4 kayıtları yüklenmez.
- **Sahne:** sayaç rafı tek seferlik editör kodu ile değişti. Unity'nin kaydı sahnedeki ~260 ilgisiz nesneye düzenleme
  anında hesaplanan değerler (TMP stil karması, renk, layout boyutları) yazdı; bunlar ayıklandı, sahne farkı yalnız sayaç
  rafıdır. RectTransform alt nesneleri sıradan Transform altındayken `localPosition` değil `anchoredPosition` kaydedilir.
- **Testler:** `RaidEngineTests` (kilitli ikmal savaşı yeni hamlelerle; makinelerin sivile nişan almaması, kaçılan atışın
  arkadaki sunucuyu vurması), `LiberationCampaignTests` (güven kaybı, Dağılma ve kayıttan devam, bölge iadesi ve tavan,
  çekirdek önce gelir), `DeskBridgeTests` (rapor satırı ve uyarı, yenilgi son raporu), `SiginakDeskTests` (altı sayaç sırası),
  `SadeRaidStateTests` (HUD güven etiketi ve ilk kırık simge). EditMode 167/167.
- **Play'de denendi:** emir belgesi (520 karakter sınırında), sayaç rafı GÜVEN 10/10, baskın HUD'u "GÜVEN 8", operasyon raporu
  "−3 · 7/10", güven 0'da son rapor. Konsolda hata yok; kayıt dosyaları geri yüklendi.

## Sadeleştirme: yalnız DeskGame ve Baskin (2026-10-27)

Kullanıcı kararı: "şu anki DeskGame ve Baskin sahnelerinde oynanan oyun kalsın, diğer denemeleri kaldır, bu oyunda
kullanılmayan kodları sil, illüzyon gibi bu oyunda olmayan mekanikleri kaldır." Soruların cevabı: eski kriz oyunu tamamen
(Main sahnesi ve Core kuralları dahil), tarayıcı prototipi tamamen, deneme belgeleri silinsin; dal `siginak-ui`.

- **Silinen sahneler:** `Scenes/Main.unity` (kriz masası), `Desk/Scenes/DeskUI_Gallery`, `DeskUI_Sandbox`,
  `RaidBoard_Sandbox`, `SadeDesk_Sandbox`, `SadeUI_Gallery`, `Siginak_FontTest`. Build listesinde DeskGame ve Baskin.
- **Silinen denemeler:** `Assets/Corrigible/Ayna` (AYNA savaş denemesi), `Assets/Corrigible/Fx` (fütüristik arayüz),
  `Prototype/` (tarayıcı prototipi, parite ve denge script'leri), `Docs/UI_Fx`, `Docs/AYNA`,
  `Docs/SonSalter_AYNA_Tasarim`, `Docs/SonSalter_Baskin_Paketi.zip` (açılmış paketi duruyor).
- **Core:** kriz kuralları çıktı: yayılma, Tekillik, tespit, çekirdek kaçışı, hediyeler, illüzyon ve Algı Katmanı
  (`Shown`/`Global`), araştırma ve operasyonlar, diplomasi, rakip tipleri ve yetenekleri, oyuncu profili ("Seni Tanıyan
  Rakip"), aşamalı öğrenme, olay kartları, kampanya, saha raporları, deneysel zaman ölçeği. Silinen dosyalar `Rules.cs`,
  `FieldReports.cs`, `PlayerProfile.cs`; `Simulation`, `WorldState`, `GameData`, `Balance`, `Economy`, `Teams`,
  `GameRecord` yeniden yazıldı. Taktik motordan aldatma (sahte niyet, tarama), yedek çekirdek ve hızlanma çıktı. Kurtuluş
  kuralları değişmedi; işgal oranı eski ortalama sızmaya eşit, baskınların "kopya" özelliği yine 2 bölgeden sonra.
- **Kayıt biçimi 4:** `RuleOptions`, profil ve kriz komutları kayıttan çıktı. Eski kayıtlar (biçim 3 ve öncesi)
  yüklenmez; oyun yeni harekâtla açılır.
- **Game:** eski uGUI kriz masası (`Game/UI`), eğitim, kampanya, profil deposu, `LocalizedText`, eski baskın kadrajı silindi.
  `GameController`, `AudioDirector`, `SoundBank` sadeleşti; başarım yalnız Silme.
- **Desk:** analist masasının harita, pencere, ipucu, illüzyon, hata ayıklama katmanları ve kriz ekranları (giriş,
  kampanya, sahte zafer, onay, eğitim, dosya, araştırma, telex, dock…) ve onların bileşenleri silindi. Kalanlar: oda
  (Siginak), kâğıt harita (`SadeMapView` ve bölge çokgenleri), baskın raporu (`EventModal`), son rapor (`GameOver`), ayarlar
  kartı, baskın HUD'u. Köprü yalnız baskın raporu ve son raporu kurar (`SimDeskBuilder`).
- **Sahne ve prefab'lar:** DeskGame'den kullanılmayan katmanlar (sızma haritasının işaret/düğüm/iplik katmanları, karantina
  çizgileri, eski masa lambası, kriz ekranları, ipucu/eğitim/hata ayıklama canvas'ları) ve eksik script'ler, Baskin'den
  `SadeDebug` silindi (DeskGame.unity ~885 000 → ~283 000 satır). Baskın kurallar kartının lejantı 10 → 8 satır (yalan ve
  tarama işaretleri çıktı). Çekirdek dolabının "Ç" yazısı `DeskLocText`'e geçti.
- **Varlıklar:** sahnelerin ve Resources'ın başvurmadığı ~175 prefab/veri/malzeme/doku, 64 kullanılmayan yer tutucu ses,
  `UiTheme`, `TutorialSequence`, `RaidUiTheme` asset'leri silindi; SoundBank'ta yalnız var olan 19 ses olayı kaldı.
- **Çeviri tablosu** 2 840 → 575 satır; yeni 16 metnin İngilizcesi yazıldı.
- **Kod:** C# 631 dosya / ~69 700 satır → 202 dosya / ~22 600 satır. Testler 713 → 127 (kriz kurallarının ve silinen
  arayüzlerin testleri gitti; taktik motor için `RaidEngineTests` eklendi, köprü testleri yeniden yazıldı).
- **Bu sırada bulunan hatalar:** son raporun grafik lejantında boş dizi öğesi `NullReferenceException` verecekti
  (dizi sıkıştırıldı); son rapor grafiği son sektör vardiya içinde düşünce %0 yerine son vardiyanın değerinde bitiyordu
  (güncel değer eklenir); İngilizce baskın günlüğünde "the aı node cabinet" (Türkçe küçük harf; kısaltmalar artık korunur).
- **Doğrulama:** derleme temiz, EditMode 127/127 (boş sahne açıkken), Play'de açılış → BAŞLA → vardiya ve raporu →
  baskın (ikmal) → kurallar kartı → geri çekilme → operasyon raporu → masaya dönüş; harekâtın sonu ve son rapor →
  Yeniden başla; Baskin tek başına. Konsolda hata yok. Kayıt dosyaları geri yüklendi.

Bu dönemden önceki kriz oyunu, analist masası (UI_C), Sade arayüz (F1–F7), Fx ve AYNA çalışmalarının ayrıntıları git
geçmişindedir (`b7b4d82` ve öncesi). `Docs/` altındaki şu belgeler o eski oyunu anlatır, güncel değildir:
`ARASTIRMA-INCELEMESI`, `BUTUNLESIK-EKONOMI-*`, `EKONOMI-INCELEMESI`, `FON-*`, `GELISIM-FIKIRLERI`,
`OPERASYON-VE-HIZALAMA-ORNEGI`, `ROSTER-6-GDD`, `SAHA-RAPORLARI*`, `SIFIR-PROJE-GDD-VE-YOL-HARITASI`,
`YZ-SAVASI-OYUN-FIKRI`, `UI-BILDIRIM-DUZELTMELERI-SONUCLARI.json`. (`UI_DeskGame/TASARIM_PROMPT.md` ise
bugünkü odanın tasarım brifidir.)

## DeskGame · Sığınak komuta odası (2026-10-26)

- `Docs/UI_Siginak/PROMPT.md`'deki on aşama uygulandı: oda ve ışık, kamera rig'i, masa (sayaçlar, anahtar rayı, şalter,
  teleks), kâğıt harita (görev fişi, cephe onayı, ekip figürü), sığınak maketi, CRT ve baskın dalışı, vardiya raporu,
  açılış ve emir belgesi, punto/TR-EN/performans geçişi. Ayrıntı ve kararlar `Docs/UI_Siginak/IMPLEMENTATION_MAP.md`.
- Eski AIBunker komuta masası ve odayı koddan kuran geçici kurucu kaldırıldı; sahne asıl kaynak (kural 5). Baskında odayı
  `SiginakRaidLink` gizler. Maketin büyük sarı çerçevesi kaldırıldı; seçili oda çerçevesi yalnız K4'te.

## DeskGame · kurtuluş harekâtı (2026-10-21)

- Tam YZ işgaliyle başlayan harekât, bölge başına iki ikmal ve bir sunucu sektörü, ortak sınırdan yeni cephe, 36 sektörde
  zafer, manuel vardiya, sığınak (yaşam alanı, jeneratör, atölye, revir), operasyon raporu. Bu kurallar kriz oyununun
  üstüne `RuleOptions.LiberationCampaign` ile yalıtılmış olarak eklenmişti; 2026-10-27'de oyunun tek kuralı oldu.
  Güncel anlatım: `OYUN-TASARIMI.md`.

## Önemli kararlar ve gerekçeleri

- **Lokalizasyon Unity Localization paketiyle değil, kendi `Loc` sınıfıyla.** Metinlerin bir kısmı motordan bağımsız
  Core'da üretiliyor; paket Core'dan kullanılamaz. Kaynak metin Türkçe kalır, çeviri tek bir sekmeli tabloda.
- **Komut kaydı ile kayıt** (durum dökümü yerine). Küçük dosya, determinizmi sürekli test eder. Kural değişirse eski
  kayıtlar farklı oynanır: `GameRecord.CurrentVersion` artırılır, eski sürüm yüklenmez.
- **Simülasyon/sunum ayrımı.** Core motordan bağımsız ve testli; oda yalnız okur, değişiklik komutla.
- **uGUI, UI Toolkit değil.** Kullanıcı UI'yı Scene görünümünde görmek ve Canvas ile çalışmak istedi.
- **Kalıcı sahne kurucusu yok.** Sahne ve prefab'lar elle düzenlenebilsin; koddan değişiklikler tek seferlik script'lerle.
- **Proje Linear renk uzayında** (kullanıcı kararı); ince yazı TMP malzeme ayarıyla, yarı saydam örtüler Linear telafili
  alfayla düzeltildi.
- **Zaman kendiliğinden akmaz:** vardiya oyuncunun şalteriyle biter; baskın ve hikâye açıkken bitirilemez.

## Açık işler

- **Gölgeler (karar, 2026-10-31):** ampul ve lamba gölgeleri yumuşak kalır. Sert gölge ~0,25 ms kazandırırdı (Revizyon 2'yi
  öncesinden %3–6 hızlı yapardı); Revizyon 2'nin masa ve sığınak pozundaki %2–3'lük farkı kabul edildi (`Docs/UI_Siginak_Rev2/IMPLEMENTATION_MAP.md` Soru 13).
- **Denge (kadro seçimi):** Madenci ve Sis uzmanının hakları (dinamit 2, sis 3) ölçülmedi; seçimli kadroyla zorluk yeniden
  ölçülmeli (yukarıda, "Madenci, Sis uzmanı ve kadro seçimi").
- **Denge:** harekâtın zorluğu ve uzunluğu gerçek oyunla ayarlanmadı; sivil güven, alarm ve hazırlık sığınağı botla
  ölçüldü, ama bot projede değil (oturumun geçici klasöründe); projeye denge testi olarak alınmadı. Botla görülen: zorluk
  5. bölgeden sonra artmıyor. Harita türleri hep soldan girişli ve tek hedefli; birden çok hedefli ya da başka kenardan
  girişli türler motorda ikmal kaçışını ve makine yerleşimini genişletmeyi ister. Karşı saldırıyla
  yeniden özgürleşen bölge bölge fonunu ve güveni yeniden veriyor (ölçüm de böyleydi).
- **Ses:** Dağılma da Silme'nin bitiş sesini (`EndErase`) çalıyor; yenilgiye ayrı ses yok.
- **Ses:** yer tutucu sesler gerçekleriyle değiştirilmeli; AudioMixer yok. Kapının üç sesi ve telsiz sesleri kodla sentezlendi.
- **Telsiz kurgusu 3. aşama:** isteğe bağlı "YZ telsizi dinler" mekaniği (yukarıda). Baskın günlüğündeki tur satırı
  Core'dan gelir ("— Tur n: makineler hareket ediyor —"), telsiz diliyle yazılmadı.
- **Kapı:** Kapı K1'den görünmüyor; vuruşta kamera kendiliğinden döner (masada 4 · KAPI tuşunun lambası ve el yazısı fişi de
  uyarır). Eğitim notu kapı kâğıdı açıkken gizlenmez
  (eğitim ilk vardiyada bittiği için çakışmıyor).
- **İngilizce metinlerin ana dili İngilizce olan biri tarafından gözden geçirilmesi** (çeviriler ajan tarafından yapıldı).
- **Steam:** Steamworks.NET paketi kurulmadı, App ID ve `steam_appid.txt` yok. `Corrigible.Steam` paket kurulunca
  derlenir; denenmedi. Steamworks'te başarım ve Auto-Cloud (`persistentDataPath`) tanımlanmalı.
- **Baskın ekranı yalnız fare/klavye için;** gamepad akışı yok.
- **Test eksikleri:** PlayMode/UI testi yok; akış ajanlar tarafından Play'de elle doğrulanıyor.
- Eski oyunu anlatan belgeler (yukarıdaki liste) silinebilir ya da arşivlenebilir; kullanıcıya sorulmadı.
