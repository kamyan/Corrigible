# Kapıdan gelen belgeler ve YZ mekânları

_2026-11-22. İstek: "olaylar ve dışarıdan gelen diğer belgeler kapıdan gelsin, evrak'a düşmesin direkt; her yapay zekâ
farklı bir yerde olsun, tren ve gemi gibi diğerleri için de fikir üret; sadece ilk başlanan yapay zekâ sabit olsun."_

Belgenin iki bölümü var:

- **A. Kapıdan gelen belgeler.** Dışarıdan gelen her belge kapının yanındaki çift taraflı belge çekmecesine konur
  (2026-11-23; önce kapı panosuydu). Oyuncu çekmeceyi açıp belgeyi alır, karardan sonra çekmeceye koyup yanındaki şalteri
  çeker. Sunum işidir; Core, kayıt ve RNG değişmez.
- **B. YZ mekânları.** VERITY'nin banka veri merkezi (karargâh, oyunun ilk YZ'si) olduğu gibi kalır. Katar (WAYPOINT,
  tren) ve Yalpa (HALCYON, platform) zaten var. Kalan dokuz YZ'nin her biri kendine özgü bir yere ve o yerin kuralına
  taşınır. Bu bölüm bir öneri listesidir; onay bekler.

---

## A. Kapıdan gelen belgeler

### A.0 Çift taraflı belge çekmecesi (2026-11-23)

_Geliştirici: "olaylar ve diğer dışarıdan gelen belgeler çekmece gibi bir şeyden gelsin, çift taraflı gibi, duvarın dışından
koyulmuş gibi olsun; belge gelince ışığı yansın, çekmeceyi açıp içinden alalım belgeyi; karar verdikten sonra çekmeceye koyup
çekmecenin yanındaki bir şalteri ya da butonu çekelim duvarın diğer tarafına haber vermek için."_

Kapı panosu kalktı. Yerine duvarı geçen bir belge çekmecesi geldi; dış kapağı sığınağın dışındadır. A.1–A.3 panonun
uygulamasını anlatır; aşağıdaki maddeler onların yerine geçer.

- **Yer.** KABUL penceresinin hemen altında, kapı duvarında (dünya 2,2 · 0,55 · −0,27), bankalardaki gişe çekmecesi gibi.
  K8'in kadrajındadır. Panonun eski yeri (kapının solu, 1,45 m) çekmece yüksekliğinde CRT masasının teçhizatının arkasında
  kalıyordu.
- **Parçalar** (sahnede `SiginakRoom/Belge çekmecesi`; Unity primitifleri ve Rev3 malzemeleri, yeni model yok):
  - çerçeve;
  - kayan tabla ("Tabla", `PartDriver` Slide, yerel Z 0 → −0,28 m) ve ön kapağı (çekmecenin hedefi);
  - lamba (`PartDriver` Swap: sönük, amber, yeşil);
  - şalter (kutu hedefi; kol `PartDriver` Rotate, −20° → 60°; kırmızı topuz).
  - İki belge çapası tablanın çocuğudur: kâğıt tabanda, arka dudağa yaslanmış ve 25° kalkık.
- **Geliş.** Kâğıt yalnız çekmece kapalıyken ve tablada giden belge yokken gelir (`SiginakPassDrawer.RequestDelivery`). Açık
  ve boşta duran çekmece dışarıdan çekilip kapanır. Kâğıt tablaya konur, kapı bir kez vurur. Çekmecenin lambası amber yanar
  (çekmece kapalıyken yanıp söner). Kapı lambası ve KAPI tuşu eskisi gibi belgeyi gösterir.
- **Alma.** Çekmecenin önüne tık çekmeceyi açar ya da kapar. Tabladaki kâğıda yalnız çekmece tam açıkken tıklanır
  (`ShelterEventPaperView.DoorOpen`); tık kâğıdı ele getirir.
- **Bırakma.** Esc, sağ tık ya da kâğıdın boş yeri kâğıdı tablaya geri koyar. EVRAK tepsisine inmez.
- **Karar.** KARAR'da komut verilir (`event_decide`, `server_reward`; değişmedi). İz basılır, kâğıt tablaya konur
  (`Where.Outbox`) ve lamba yeşil yanıp söner. KAPI tuşunun satırı "Karar çekmecede: yanındaki şalteri çekip dışarıya bildir."
  olur.
- **Gönderme.** Şaltere tık kolu çeker, çekmece kapanır; kapanınca kâğıt duvarın öbür yanına geçer (gizlenir, kâğıt sesi).
  Tablada karar yokken şalter ret sesi verir. Şalter yalnız sunumdur: kayda girmez, Core'a dokunmaz.
- **Bekleyen karar.** Yeni belge (zincir olayı, sonraki ödül) giden belge şalterle gidene dek bekler. Vardiya dosyalanırken
  gönderilmemiş karar kendiliğinden gider. Kayıttan devamda ve yeni oyunda çekmece kapalıdır; gönderilmemiş karar yeniden
  görünmez, çünkü karar zaten kayıttadır.
- **Kod.**
  - `PassDrawer` (Runtime): parçaları sürer, tık ve ses olayı yayar.
  - `SiginakPassDrawer` (Bridge): iki belgenin durumuna göre çekmeceyi, lambayı, etiketleri ve gönderimi yönetir.
  - `ShelterEventPaperView`: yeni `Where.Outbox`, `FileToDrawer`, `Send`, `DoorOpen`, `InOutbox`. `PutBack` çapa varsa tablaya
    döner. Çapasız sahnede eski tepsi davranışı sürer.
  - `SiginakShelterEvent` ve `SiginakServerReward`: `passDrawer` alanı. Karardan sonra `FileToDrawer` çağrılır.
  - `SiginakDesk.PaperToSend`.
- **Metinler.** Çekmece ve şalter etiketleri, KAPI satırları, rehberin iki adımı, ÖN KONTROL ("Tutanak kapının çekmecesinde."),
  CRT ve telsiz ipuçları.
- **Testler.**
  - `PassDrawerTests` (yeni).
  - `ShelterEventPaperDoorTests`: tablaya dönüş, kapalı çekmece, gönderme.
  - Sahne ve PlayMode testleri çekmeceye göre güncellendi.
- **Sesler** (var olan cue'lar): çekmece `AdmissionDrawer`, şalter `SwitchClack`, ret `BoardDeny`, gönderme `PaperSlide`.

### A.1 Hangi belgeler

Masaya fiziksel bir kâğıt olarak gelip EVRAK tepsisine düşen yalnız iki belge vardı. İkisi de artık kapıdan gelir:

| Belge | Bileşen | Önce | Şimdi |
|---|---|---|---|
| Olay tutanağı (bütün olay türleri) | `SiginakShelterEvent` | Kapanış karanlığından sonra EVRAK'a düşer | Kapı panosuna iner, kapı bir kez vurur |
| Sunucu ödülü kâğıdı | `SiginakServerReward` | Operasyon raporu dosyalanınca doğrudan ele gelir; raporsuzsa EVRAK'a düşer | Rapor dosyalanınca ve haritanın "BÖLGE ÖZGÜR" dizisi oynayınca kapı panosuna iner, kapı vurur |

Değişmeyenler:

- Operasyon raporu ve harekât emri ekranın önünde açılan belgelerdir; tepsiye düşmezler.
- Devir fişi ve çekilme emrini oyuncu odada üretir.
- Kuşatma kararı haritadaki bir fiştir.

İçerideki olaylar (hırsızlık, kavga, anma…) da kapıdan gelir: tutanağı nöbetçi getirip kapıya bırakır. Böylece olayın türüne
göre iki ayrı yol olmaz.

### A.2 Akış

1. **Geliş.** Vardiya dosyalanıp karanlık kalkınca, önceki sıra korunur: teleks önce günlük satırlarını, sonra OLAY satırını
   basar ("{0} · tutanak kapıya bırakıldı. Karar verilmeden vardiya kapanmaz."). Ardından kâğıt kapının sağındaki
   **kapı panosuna** iner (kâğıt sesi) ve kapı bir kez vurur (`SiginakAdmission.KnockDelivery`: lamba parlar, vuruş sesi).
   Kamera dönmez; kartlı oyunda aday grubu gelince de dönmüyordu.
2. **Gösterge.** Kâğıt panodayken:
   - Kapı lambası sürekli amber yanar.
   - KAPI tuşu yanıp söner. Aday yokken başlığı "KAPI · BELGE VAR", satırı "Kapıya bir belge bırakıldı: kapının yanındaki
     panodan al." olur.
   - Kapının üzerine gelme satırı belgeyi söyler.
   - EVRAK'ın ışığı yanmaz.
3. **Alma.** KAPI (4 / K) kamerayı K8'e götürür. Panodaki kâğıdın üzerine gelinince "KAPI · OLAY TUTANAĞI" ya da
   "KAPI · SUNUCU ÖDÜLÜ" yazar. Tık kâğıdı ele getirir. El pozu o anki kameradan hesaplanır, yeni kamera pozu gerekmez.
   Satır seçme, KARAR ve Enter aynen çalışır.
4. **Bırakma.** Esc, sağ tık ya da kâğıdın boş yerine tık kâğıdı **EVRAK tepsisine** indirir. Kâğıt kapıdan alınmış olsa
   da böyledir: okunan belge EVRAK'a kalkar. Bundan sonrası eski tepsi davranışıdır: ışık, etiket, arşivin bir kez açılması,
   tepsiden geri alma.
5. **Karar.** İz basılır, kâğıt tepsiye iner ve gizlenir (değişmedi).
6. **EVRAK tıkı.** Panodaki kâğıdı getirmez. Tepsi kendi yolunu açar (arşiv fişi, emir belgesi).
7. **Kayıttan devam.** Açık olayın ve bekleyen ödülün kâğıdı animasyonsuz kapı panosundadır; vuruş olmaz. Kâğıdın yeri sunum
   durumudur ve kayda girmez: tepsiye konmuş kâğıt yüklemede yine panoda görünür.
8. **Baskından dönüş.** Baskın sahnesi yüklenince oda kapanır, dönüşte yeniden açılır. Aynı kaydın sunumu sürer: panodaki kâğıt
   panoda, tepsideki kâğıt tepside kalır ve kapı vurmaz.
9. **Zincir olayı.** Bir karar aynı komutta yeni bir olay açarsa (alan kararı: sabotaj), yeni tutanak öncekinin izi ve
   dosyalanışı bitince kapıdan gelir: OLAY satırı, pano ve vuruş.
10. **Sunucu ödülü ve harita.** Bölgeyi tamamlayan baskının raporu dosyalanınca harita önce "BÖLGE ÖZGÜR" dizisini oynatır, ödül
    kâğıdı ondan sonra kapıya gelir. Böylece kapının vuruşu haritanın dizisiyle çakışmaz.
11. **İFŞA parlaması.** Kararın ifşa parlaması kamera kapının K8 ailesindeyken bekler. Şerit arka duvardadır ve kapıdan
    görünmez.

Şalterin kilit satırı ve ÖN KONTROL kâğıdın yerini bilmez (Core'dan kurulur). Bu yüzden "{0} Tutanak kapıda ya da EVRAK
tepsisinde." der. CRT ipucu "SUNUCU ÖDÜLÜ BEKLİYOR · KAPI/EVRAK", telsiz satırı "Sunucu ödülü bekliyor · kapı ya da
EVRAK" olur. Rehberin "OLAY TUTANAĞI" adımı kapıyı ve panoyu gösterir.

### A.3 Uygulama

- **Sahne** (`DeskGame.unity`, tek seferlik editör komutları): `SiginakRoom/Kapı panosu` kuruldu. Kapının solundaki duvardadır,
  göz hizasında ve kapı lambasının aydınlığında (dünya 2,18 · 1,45 · 1,85), odaya bakar. İçinde üç çocuk var:
  - "Mantar pano": arka duvardaki kurtarılan bölgeler panosunun modeli (`SM_Board`), 0,42 ölçekte (0,57 × 0,38 m),
    çarpıştırıcısız
  - "Olay tutanağı kapıda" çapası (kapıya yakın olan)
  - "Sunucu ödülü kapıda" çapası

  İlk deneme kapının sağında, kabul tezgâhının üstündeydi. Oyunda orası gölgede kaldı ve kâğıt zor seçiliyordu.
  İki kâğıt görünümü sahnede yine tepsinin çocuğudur. Yeni `doorAnchor` alanları çapalara bağlandı. İki köprünün yeni
  `admission` alanı `SiginakAdmission`'a bağlıdır.
- **`ShelterEventPaperView`:**
  - Yeni durum `Where.Door`.
  - `Bind(d, animate, atDoor)` ve `Restore(d, atDoor)`. Çapa yoksa kâğıt tepsiye gider.
  - Yeni okuyucular: `AtDoor`, `InTray`, `DoorPose`, `DoorTitle`/`DoorLine`.
  - Panodayken çarpıştırıcı açıktır, tık `Clicked`'tir ve üzerine gelme etiketi panonun başlığıdır.
  - Kâğıt panodayken çalışma anında panonun çocuğu olur. Tepsinin `DeskTarget`'ı ataydı ve etiketi kâğıda sızıyordu.
    Tepsiye dönerken ya da gizlenirken kâğıt yeniden tepsinin çocuğu olur. Oyunda ebeveyn değişimi kâğıdın sonraki `Update`'ine
    kalır, çünkü Unity açılmakta olan bir hiyerarşide ebeveyn değişimini reddeder.
- **Köprüler:** `SiginakShelterEvent.AtDoor`, `SiginakServerReward.AtDoor`, `SiginakDesk.PaperAtDoor` ve
  `SiginakAdmission.DeliveryWaiting`. Lamba, KAPI tuşu ve kapı satırı bu dördünü okur.
  - Tepsi önceliği (K27) artık tutanağın gerçekten tepside olmasına bakar (`SiginakShelterEvent.InTray`). Panodaki tutanak,
    tepsideki ödül kâğıdının önünü kesmez.
  - Köprülerin `OnEnable`'ı aynı kayıtta `Restore` değil `Refresh` çağırır.
  - Ödül kâğıdı `LiberationDeskView.MapRevealPending`'i (`SiginakMap.RevealPending`) bekler.
- **Testler:**
  - EditMode: yeni `ShelterEventPaperDoorTests` (7). `Rev3ShelterEventSceneTests` (pano, çapalar ve `admission` bağı; ödül
    köprüsünün yeni sahne testi), `Rev3ShelterEventSnapshotTests`, `FacilityServerRewardTests` ve `SiginakRoomTests` güncellendi.
  - PlayMode: `ShelterEventPlayTests` kapıdan gelişe göre yeniden yazıldı. Yeni `RoomReenabled_KeepsThePaperWhereItWas`.
    `FacilityServerRewardPlayTests`, `FacilityGearCampaignPlayTests`, `FacilityCounterPlayTests` ve `PlanningTutorialPlayTests`
    güncellendi. Ortak yardımcılar `ShiftPlayKit.WaitDoor` ve `ShiftPlayKit.TakeFromDoor`.
- **Core, kayıt ve RNG değişmedi.** Yeni komut yok.


---

## B. YZ mekânları

_Üçüncü tur (2026-11-22). Geliştirici: "gemi ve tren gibi daha genel anlaşılır bir olay olsun, otomatik konteyner
limanındaki olay iyi gibi, karmaşık olaylar olmasın, uçağın olayı karmaşık geldi." Önceki iki turun listesi B.8'de. Dördüncü turun ikinci listesi B.4'te, beşinci turun kodla sınanmış üçüncü listesi B.5'te._

### B.1 Ölçü

- **Yer herkesin tanıdığı bir yerdir**: tren, gemi, liman, gökdelen, maden, metro.
- **Kural o yerde herkesin beklediği ilk şeydir** ve tek cümledir. Tek tetik (takvim ya da tek bir neden), tek sonuç.
- **Sayaç, iki aşamalı durum, uzmana bağlı kol, istisna listesi yok.** İkinci turun uçağı, reaktörü, çatlayan buzu ve
  güneş ışını bu yüzden çıktı.
- **On iki yer on iki ayrı iş yapar.** Hiçbiri Katar'ın (kenardan düşme), Yalpa'nın (zeminin yatması) ya da limanın
  (vincin duvar taşıması) başka kılıkta tekrarı değildir.
- `BOLGE-OLAYLARI.md`'nin ortak kuralları geçerlidir: RNG yok, kural tarafa bakmaz, bir tur önceden görünür, evrede en
  çok 1 olay hasarı, hiçbir kare çıkıştan kopmaz. Bir darbeyle tetiklenen kural (yangın, varil, vagonet) fitil kuralı
  gereği bir tur sonra işler; bugünkü tüp de böyledir.
- VERITY sabit kalır. Şart giriş odasında yoktur; savunma baskınında kapalıdır (aşama 12 S2).

### B.2 Birinci liste (üçüncü tur)

| YZ (paket, kademe) | Yer | Kural (tek cümle) |
|---|---|---|
| **VERITY** (weu, karargâh) | Banka veri merkezi (**değişmez**) | Söndürme bulutu nişanı keser |
| **WAYPOINT** (na, orta) | Sevkiyat katarı (**var**) | Vagonun kenarı açık: dışarı itilen düşer |
| **HALCYON** (oc, geç) | Yüzer maden platformu (**var**) | Güverte çift turlarda yatar; her şey 1 kare kayar |
| **TESSEL** (jk, erken) | Konteyner limanı (**beğenildi**) | **İstif:** vinç çift turlarda işaretli konteyneri alıp işaretli yere koyar |
| **HEADWATER** (sa, erken) | Kereste fabrikası | **Yangın:** ateş değen kereste yığını yanar ve yok olur |
| **CAIRN** (af, erken) | Maden ocağı | **Vagonet:** vurulan vagonet rayında yuvarlanır ve önüne çıkana çarpar |
| **EMBER** (me, orta) | Akaryakıt deposu | **Varil:** vurulan varil bir tur sonra patlar; yanındaki herkes hasar alır |
| **SOLACE** (in, orta) | Gökdelen | **Asansör:** asansör çift turlarda içindekini öbür durağa taşır |
| **EDDY** (sea, orta) | Sel basmış çarşı | **Girdap:** rögar 3. ve 6. turda açılır; yanındakileri içine çeker |
| **QUORUM** (eeu, geç) | Metro istasyonu | **Tren:** tren 3. ve 6. turda geçer; rayda duran hasar alır |
| **BOREALIS** (ru, geç) | Donmuş göl | **Buz:** buzda itilen, bir engele çarpana kadar kayar |
| **PARAGON** (cn, geç) | Otomobil fabrikası | **Robot kol:** kol her tur çeyrek döner ve önündeki kareye vurur |

### B.3 Yerler

Sekiz kuralın yedisi bugünkü on iki oda olayından birinin sadeleşmiş hâlidir; kod büyük ölçüde hazır. "Motor" satırları
`Baskin.Events.cs`'teki karşılığı verir. Hiçbiri uygulanmadı ve ölçülmedi.

| YZ · yer | İmza makinesiyle ilişkisi | Motor | Boy |
|---|---|---|---|
| **TESSEL** · Konteyner limanı | Mühürleyici kalasını iniş karesine kurarsa vinç o sefer bekler | Kayar rafın blok taşıması (`EndShelf`) ve `ExitSafe`; Core'da vinç kodu henüz yok | M |
| **HEADWATER** · Kereste fabrikası | Alev robotu önündeki siperi kendi yakar: yığının arkası iki taraf için de güvenli değildir | Gaz olayının `Fire` kancası işaretli bloğu siler; yol yalnız açılır, `ExitSafe` gerekmez | S–M |
| **CAIRN** · Maden ocağı | Havanın mermisi vagonetin yanına düşerse vagonet yola çıkar | Tüp olayı (`EndTubes`, uçuş hattı ray olarak çizilir); yeni kod yok denecek kadar az | S |
| **EMBER** · Akaryakıt deposu | Patlayan robot varilin yanında patlarsa varil de tutuşur: aynı köşe iki kez patlar | Tüpün fitili (`Fuse`), uçuş hattı yerine dört komşu | S |
| **SOLACE** · Gökdelen | Kabine itilen kalkan dronu öbür durağa gider ve zırh vereceği makineden uzak düşer | Yük asansörü olayı (`EndLift`, `LiftDeparts`); yolculuk turu ve iniş hasarı kalkar | S |
| **EDDY** · Sel basmış çarşı | Çoğaltıcının yan yana doğan kopyaları tek kareye çekilir ve çarpışır | Süzgeç olayı (`EndDrain`) olduğu gibi; yalnız yer ve çizim değişir | S |
| **QUORUM** · Metro istasyonu | Kilitleyici ekip üyesini rayda tutarsa tren çarpar; rayda duran kilitleyici de aynı hasarı alır | İşaretli hat üstünde takvimli olay hasarı; makine aklında olay karesinden kaçınma terimi var | S |
| **BOREALIS** · Donmuş göl | Kırağıda yürüyüşü biten ekip üyesi buzun ortasında kalır; tek itme onu duvara kadar götürür | Yağ olayının kayması (`Slide`); zemin baştan kaygan, isabetle kayma (`HitSlide`) kalkar | S |
| **PARAGON** · Otomobil fabrikası | Montajcı hurdayı almak için kolun döneceği kareye girerse vuruşu o yer | Kaynak kolu olayı (`EndArm`); "kola darbe dönüşü hızlandırır" maddesi kalkar | S |

**Kanat adları (öneri).** Kereste fabrikası: Tomruk sahası · Hızar holü · Kumanda odası. Maden ocağı: Galeri ağzı · Ray
kavşağı · Kumanda kabini. Akaryakıt deposu: Dolum sahası · Varil ambarı · Pompa dairesi. Gökdelen: Lobi · Asansör holü ·
Sunucu katı. Sel basmış çarşı: Sokak · Çarşı içi · Kopya atölyesi. Metro istasyonu: Bilet holü · Peron · Sinyal odası.
Donmuş göl: Kıyı barakası · Buz üstü · Sondaj kulübesi. Otomobil fabrikası: Parça ambarı · Kaynak hattı · Hat kontrol odası.
Konteyner limanı: İstif sahası · Vinç altı · Liman kontrol kulesi.

### B.4 İkinci liste (dördüncü tur): farklı fikirler, biri havada

_Geliştirici: "farklı fikirler üret; uçak olması şart değil, hava aracı olsun, havada duran bir yer." B.2'nin listesi
geçerliliğini korur; bu liste onun yerine değil, yanına yazıldı. İki listeden yer yer seçilebilir (aynı YZ için biri)._

| YZ | Yer | Kural (tek cümle) | İmza makinesiyle ilişkisi | Motor | Boy |
|---|---|---|---|---|---|
| **BOREALIS** | **Yolcu uçağı** (havada) | **Kemer:** kemer ışığı 3. ve 6. turda yanar; koltukta olmayan hasar alır | Kırağıda yürüyüşü biten koltuğa yetişemez | Faz olayının tersi (`EndPhases`): koltuk kareleri güvenli, öbürleri 1 olay hasarı; koltuk yerleşimi yeni | S–M |
| **SOLACE** | Hastane | **Yatak:** hasta yatağında turu bitiren 1 can kazanır | Kalkan dronunun koruduğu makine yatakta hem hasar almaz hem iyileşir: yatağı önce ekip tutmalı | Evre sonunda işaretli karelerde +1 can; bakım robotuyla dengesi ölçülmeli | S |
| **HEADWATER** | Yüzme havuzu | **Su:** havuzdaki birim yürür ama eylem yapamaz | Havuza giren alev robotu yakamaz | Kare bayrağı; eylem kapıları (`CanAct`, `CanAction`) ve makine niyeti | S–M |
| **CAIRN** | Kale | **Burç:** burca çıkan, duvarların üstünden nişan alır | Havan duvarın ardından atar; burca çıkan onu saklandığı yerde vurur | Nişan hattı hesabı yirmi kadar yerde yineleniyor; her birine istisna gerekir | L |
| **EMBER** | Şantiye | **Beton:** mikser çift turlarda beton döker; ıslak betondaki o tur yürüyemez | Betonda kalan, yürüyen bombadan kaçamaz | Kriyo olayı olduğu gibi (`EndFreeze`); buz kabuğu çıkar | S |
| **EDDY** | Lunapark | **Atlıkarınca:** çift turlarda yarım tur döner; üstündekiler karşı yana geçer | Çoğaltıcının kopyası atlıkarıncayla ekibin arkasına geçer | İşaretli karelerde konum değişimi; çarpışma olmaz | S–M |
| **QUORUM** | Sınır kapısı | **Tel örgü:** telden geçilmez ama içinden nişan alınır | Kilitleyici telin ardından kilitler; ona yürüyemezsin, vurabilirsin | Ekibin tel örgü nesnesi (`tel`) zaten böyle çalışır; kesilmeyen türü gerekir | S |
| **PARAGON** | Hurdalık | **Pres:** pres çift turlarda çalışır; yerdeki bütün hurda yok olur | Montajcı hurdayı pres çalışmadan toplamak zorundadır | Çift turda `hurda` temizliği; yol yalnız açılır | S |

- **Havadaki yer için öbür seçenekler.** (1) Aynı uçak, daha da sade kural: "kemer ışığı yanınca o tur kimse yürüyemez"
  (koltuk karesi gerekmez; S). (2) Kargo uçağı: "arka rampa 3. ve 6. turda açılır; rampadaki dışarı düşer". Çok
  anlaşılır, ama sonucu Katar'ın düşmesiyle aynıdır ve olayın tek seferde birimi yok etmesi "evrede en çok 1 olay hasarı"
  kuralını deler. (3) Zeplin: "ateş değen kare baskın sonuna kadar yanar". Uçak dışındaki en iyi hava aracı, ama yanan
  kareler birikir ve kereste yangınına benzer. Balon sepeti, helikopter ve teleferik kabini üç tane 8×8 oda için küçük kalır.
- **Uçak hangi YZ'ye.** Kural imza makinesine bağlı değildir. BOREALIS'e yazıldı, çünkü bu turda onun için güçlü bir
  yer çıkmadı ve kırağı ile iyi eşleşiyor. SOLACE'a da uyar (yolcular, kalabalık); o zaman hastane yedekte kalır.
- **Birlikte kullanılmaması gerekenler.** Beton ile çeltik tarlasının çamuru ("çamura giren yürüyüşünü orada bitirir";
  EDDY için yedek) aynı iştir ve kırağının etkisini yineler. Atlıkarınca B.2'nin asansörüyle akrabadır (ikisi de
  üstündekini başka yere taşır). Havuz ile saat kulesinin çanı ("çan çalınca yanındaki eylem yapamaz"; QUORUM için
  yedek) aynı iştir.
- **Zayıf halkalar.** Kale pahalıdır. Yüzme havuzu bir YZ tesisi olarak tuhaf durabilir. Hurdalığın kuralı yalnız
  Montajcı'nın hurdasına işler. Hastane iyileştirdiği için bot ölçümünde zafer oranını oynatabilir.

### B.5 Üçüncü liste (beşinci tur): taşıt, dev makine ve doğa; kodla sınandı

_Geliştirici: "farklı fikirler üret." Bu turda adaylar taşıt, ulaşım yapısı, dev makine ve doğa ağırlıklı arandı (92
aday). Öne çıkan 60 aday iki ayrı sınavdan geçti: kural `Core` kodunda gerçekten kurulabiliyor mu, ve tek cümle ikinci
bir cümle istemeden oynanıyor mu. İki sınavı da geçen altı kural aşağıdadır. B.2 ve B.4 geçerliliğini korur; üç
listeden yer yer seçilebilir. Hiçbiri uygulanmadı ve ölçülmedi._

| YZ | Yer | Kural (tek cümle) | İmza makinesiyle ilişkisi | Motor | Boy |
|---|---|---|---|---|---|
| **CAIRN** | Yanardağ krateri | **Lav:** tur sonunda lav karesinde duran her birim 1 hasar alır | Havan lav gölünün öbür yakasından atar; lava itilen de yanar | Faz olayının kablo kareleri, hep canlı (`EndPhases`, `CableHurt`); oda üreticisine göl yerleşimi gerekir | S |
| **SOLACE** | Elektrik santrali | **Ark:** jeneratör çift turlarda boşalır; başka bir birime bitişik duran herkes hasar alır | Kalkan dronu zırh vereceği makineye bitişik durur; ark hem dronu çarpar hem verdiği plakayı yer | `EventEnd`'e tek adım; geometri yok, üretici değişmez | S |
| **PARAGON** | Tersane | **Kızak:** gemi 3. turda kızaktan suya iner; gövdenin durduğu sıra boşalır | Gövdenin ardındaki hurdaya gemi inene dek yalnız o yandaki Montajcı ulaşır | Açılan kareyi dönüşe kepenk bayrağı taşır (`Cell.Shutter`); takvimli kaldırma ve çok kareli yerleşim yeni | M |
| **BOREALIS** | Kayak merkezi | **Yokuş:** pist batıya iner; her yürüyüşte batıya atılan bir adım sayılmaz | Kırağılı kare bedava adımı da keser: kaçış da kovalamaca da orada biter | İki yürüyüş döngüsü (`Reachable`, `Walk`) adım maliyetini ayırmalı; kapıya uzaklık denetimi kuralı bilmeli | M |
| **HEADWATER** | Amazon yağmur ormanı | **Sık orman:** üç kareden uzağa nişan alınmaz | Alevin boyu iki karedir: orman herkesi alevin boyuna çeker | Menzil on kadar yerde ayrı yazılı (EMP, kanca, sis, pusu, taret, makine atışları) | M |
| **QUORUM** | Denizaltı (ya da havada: asker indirme uçağı) | **Tek sıra:** içerisi dardır; yürürken hiçbir birimin içinden geçilmez, dostunun bile | Kilitlenen ekip üyesi koridorda tıkaç olur; arkasındakiler onu geçemez | İki yürüyüş döngüsünde tek koşul; makineler aynı döngüyle yürür, iki taraf da bağlanır | S |

**Kanat adları (öneri).** Yanardağ krateri: Krater yamacı · Lav gölü kıyısı · Krater istasyonu. Elektrik santrali: Şalt
sahası · Jeneratör holü · Kumanda odası. Tersane: Sac atölyesi · Kızak · Tersane kumanda odası. Kayak merkezi: Alt
istasyon · Pist · Zirve istasyonu. Amazon yağmur ormanı: Nehir iskelesi · Orman patikası · Kaçak maden kampı. Denizaltı:
Torpido dairesi · Kumanda merkezi · Reaktör dairesi. Asker indirme uçağı: Arka rampa · Paraşütçü bölmesi · Pilot kabini.

**Satırların çekinceleri**

- **Lav.** Yürüyüşün yolu kodda yoktur, yalnız bittiği kare vardır; bu yüzden "lava giren" değil "lavda duran" yanar.
  Bir iki kare genişliğindeki lav bir yürüyüşte bedava aşılır; göl geniş çizilmelidir. B.6'daki elektrikli suyun
  takvimsiz hâlidir.
- **Ark.** Taret, yem ve köpek de birimdir; uzmanın kendi kurduğu taretin yanında durması onu da çarpar. Yer sıradan bir
  binadır ve Güney Amerika'nın bugünkü binası da hidroelektrik santralidir.
- **Kızak.** Tek seferliktir: gemi indikten sonra ve dönüş yolunda odada kural kalmaz. Hangar kapısı ("dev kapı 3. turda
  açılır"), şeker kamışı biçerdöveri, taş ocağı patlatması ve kargo uçağının yük atışı aynı kuralın başka yerleridir;
  yalnız biri seçilebilir. Gemi ve limandan sonra üçüncü deniz yeridir.
- **Yokuş.** "Yokuş aşağı her adım bedava" hâli sınırsızdır (herkes odayı tek yürüyüşte geçer); bu yüzden yürüyüş başına
  bir adım. Ekranda bir şey olmaz; kural bir kolaylıktır. Çöl kum tepesi aynı kuralın başka yeridir.
- **Sık orman.** Bankanın bulutundan ve sisten sonra üçüncü nişan kısıtıdır. Mağara, karayolu tüneli ve opera salonu aynı
  kuralın başka yerleridir. Havanın atışı zaten üç karedir; CAIRN'e verilirse imza makinesine hiç dokunmaz.
- **Tek sıra.** Ekranda bir şey olmaz; kural bir kolaylığın kalkmasıdır ve ancak oda planında tek karelik koridor ve
  geçiş cebi varsa hissedilir. Ekibin kendi tareti ve yemi de yolu tıkar. QUORUM'un bölge özelliğinin adı zaten "Dar
  koridor"dur; kuralın adı bu yüzden "Tek sıra".

**EMBER ve EDDY.** Bu iki YZ için iki sınavı da geçen yeni yer çıkmadı. En yakın adaylar:

- EMBER · **Uzay istasyonu**: "Yerçekimi yok: yürüyen herkes nesnelerin üstünden geçer, ama üstünde duramaz." Patlayan
  robot siperin ardına süzülür (kodda doğru). Boy M. Kusurları: bugünkü odalarda nesne azdır, kural az hissedilir;
  ekibin kendi teli ve kalası yürüyeni durdurmaz olur; itilen ve koşu darbesi yapan yine nesneye çarpar.
- EMBER · **Cam fabrikası**: "Patlamanın değdiği cam duvar kırılır." Boy M. Robot yalnız yanındaki camı kırar; kural
  bugünkü tavan olayına ve B.2'nin kereste yangınına yakındır.
- EDDY: arkın eşleşmesi Çoğaltıcı ile de doğrudur (kopya bitişikte uyanır), ama iki canlı Çoğaltıcı iki arkta kendini
  öldürür; kural SOLACE'ta daha iyi durur. EDDY için en ucuz yol B.2'nin girdabıdır (bugünkü süzgeç olayının kendisi).
- Sahibi olmayan sağlam kural: "kalabalıkta düşmanın içinden de geçilir" (Tek sıra'nın tersi; boy S, Çoğaltıcı'nın
  dronları yolu kapatamaz). Tanınır bir yer bulunamadı (motosiklet kavşağı elendi).

**Havadaki yer.** Yirmiden fazla hava adayı sınandı; iki sınavı da geçen tek kural Tek sıra'dır (asker indirme uçağı).
Denizaltıyla aynı kuraldır; ikisinden biri seçilir. Uçak daha önce iki kez önerildi (yolcu, kargo). Uzay istasyonu en
güçlü görüntüdür ama yörüngededir, havada değil. Elenenler ve nedenleri: ateş edenin geri tepmesi (hangi hamlenin atış
sayıldığı liste ister; makinenin geri kayması sonraki hamlesinde silinir), kargo uçağının yük atışı (tersanenin
aynısı; beş turluk odada bir kez işler), halka istasyonda kenardan dolanma (komşuluk yüz kadar yerde yazılı, boy L),
ikmal uçağında uzaktan vuranın yanması (iki canlı dronlar kendini öldürür), bombardıman uçağında çapraza vuran patlama
(yalnız makinenin işine yarar), itmeyi uzatan ya da çeviren rüzgâr, yüksek irtifada "ya yürü ya eylem yap", zeplinler.

**Elenen öbür adaylar** (geri gelmesin diye): denizaltıda enkazın kalması (Montajcı'nın hurdasıdır; yol kapanacaksa enkaz
sessizce düşmez), deniz fenerinin dönen ışığı, direğe düşen yıldırım, kar topunun buz kabuğu, şelalede alev geçirmeyen
su (yalnız ekibe yarar), monorayda zincirleme itme, yüksek fırında eriyen zırh, uçak gemisi mancınığı, Machu Picchu
terasları, favelada nesnelerin üstünden atlama, Petra'da ok tuzağı, Angkor'da gizli geçit, kutup istasyonunda yalnız
kalanı vuran tipi.

**Sınamada doğrulanan motor gerçekleri** (üç listeyi de bağlar)

1. Oda süreleri 5, 5, 9 ve dönüşte 5 turdur (`Balance.facility*Turns`); şart giriş odasında yoktur. "Çift turlarda" beş
   turluk odada 2. ve 4. turdur. "3. ve 6. turda" beş turluk odada bir kez işler (B.2'nin girdabı ve treni, B.4'ün kemer
   ışığı). Yol değiştiren olay son turdan önceki evrede yapılamaz (`t < MaxTurns - 1`): beş turluk odada yalnız 2. ya da
   3. tur.
2. Yürüyüşün yolu yoktur; birim ulaşılabilir bir bitiş karesine konur (`Reachable`, `Walk`). "Giren" ya da "geçen"
   yakalanamaz; yalnız "tur sonunda duran" ve "itilen" yakalanır.
3. Patlayan robot itmez: dört komşusuna 1 hasar verir ve yok olur. Niyeti olay adımından önce işlediği için "karede duran"
   türü hiçbir kural onu yakalamaz. B.4'teki "betona giren patlayan robot da gecikir" yanlıştı; düzeltildi.
4. Kalkan dronu hale değildir: bitişiğindeki bir makineye bir kerelik zırh plakası verir. B.3'teki "koruduğu makineler
   kalkansız kalır" yanlıştı; düzeltildi.
5. Sekiz odada yalnız ekip iter (sabotaj, EMP, koşu darbesi); Cıvata önlemi açıkken imza makinesi itilmez. İtmeye
   dayanan kurallar (B.2'nin buzu) ekibin aracıdır.
6. İmza makinesi yalnız hedef odada kesindir; öbür odalara ancak uyanan makinelerin bir bölümü olarak gelir. Eşleşmeler
   asıl dokuz turluk hedef odada oynar.
7. Oda içinde kalkan duvar dönüşte geri gelir; yalnız dinamitin molozu ve açılan kepenk kalır.
8. Makineler şartı ancak akıllı kipte hesaba katar (`B.Smart`); temel kipte kurala kör yürürler.

### B.6 Yedekler (birinci liste için)

| YZ | Yedek yer | Kural | Neden yedekte |
|---|---|---|---|
| HEADWATER | Baraj | Kapaklar 3. ve 6. turda açılır; su kanaldakileri kanalın sonuna sürükler | Adına daha uygun, ama Yalpa ve buzdan sonra üçüncü kayma olur |
| CAIRN | Maden ocağı | Çatlak tavanın altında patlama olursa tavan bir tur sonra çöker | Bugünkü tavan olayı; vagonet ve varil fazla benzer bulunursa |
| EMBER | Petrol kuyusu | Kuyu 3. ve 6. turda fışkırır; yanındakileri bir kare iter | Girdabın tersidir |
| QUORUM | Hapishane | Hücre kapıları çift turlarda kapanır, tek turlarda açılır | Kaçış güvencesi gerçek kilitli hücreye izin vermez; limanın duvar işine yakın |
| BOREALIS | Roket rampası | Motor 3. ve 6. turda ateşlenir; rampaya bitişik herkes hasar alır | Yer çok güçlü, ama metro treniyle aynı iş (takvimli alan hasarı) |
| EDDY | Sel basmış çarşı | Tek turların sonunda su elektriklenir; suda duran hasar alır | Bugünkü faz olayı; girdap adına daha uygun |

### B.7 Sıra ve kararlar

**Önerilen sıra**

1. **EDDY, PARAGON, CAIRN.** Kural bugünkü olayın kendisi ya da bir madde eksiği; iş yer, ad ve çizimdir.
2. **SOLACE, EMBER, BOREALIS, QUORUM.** Var olan olayın küçük bir değişiği.
3. **HEADWATER, TESSEL.** Duvar ekleyip çıkaran iki kural; kaçış güvencesi denetimi burada iş çıkarır.

Her aşama Katar ve Yalpa'nın kalıbını izler: alt bayrak, kural testi, tesis izi (`FacilityMaps.GeneratorVersion`) artışı,
şart kartı ve rehber satırı, PlayMode'da tesisin oynanması ve bot ölçümü (`TESIS-BASKINLARI-TASARIMI.md` §11.6).

**Karar bekleyenler**

1. **Liste.** Her YZ için yer hangi listeden gelsin (B.2, B.4, B.5 ya da yedek)? Havadaki yer hangi YZ'nin olsun?
   B.5'in altı kuralı kodla sınandı; B.2 ve B.4'ün kuralları B.5'teki motor gerçekleriyle yeniden okunmalıdır.
2. **Vagonet ve varil.** İkisi de "vurulan nesne" kuralıdır (biri hat boyunca gider, öbürü yerinde patlar). Fazla yakın
   bulunursa CAIRN yedeğine (göçük) geçer.
3. **Metro.** Oyunda zaten bir tren var. İkinci bir raylı yer fazla gelirse QUORUM hapishaneye geçer.
4. **Eski olaylar.** Yeni kurallar bölgelerin bugünkü olayının yerine geçer; gaz, turnike, kriyo, faz ve tavan tesis
   kipinden düşer, öbürleri yeni yerlerinde sade hâlleriyle yaşar.

### B.8 Önceki turlar (geri çekildi)

| YZ | Birinci tur | İkinci tur |
|---|---|---|
| HEADWATER | Baraj teleferiği · hiza | Baraj gövdesi · savak ve köprü |
| CAIRN | Tünel açma makinesi · ilerleme | Tünel açma makinesi · ilerleme |
| TESSEL | Parçacık hızlandırıcı · mıknatıs | Konteyner limanı · istif (**kaldı**) |
| EMBER | Hidrojen zeplini · yan rüzgâr | Güneş kulesi · odak ve gölge |
| SOLACE | Bagaj tüneli · bant | Yolcu uçağı · basınç |
| EDDY | Sualtı habitatı · su alma | Batık kule · dönen havuz |
| QUORUM | Metro arşivi · geçen tren | Reaktör binası · doz |
| BOREALIS | Buz üstü sondaj kampı · çatlayan buz | Buz gölü sondaj kampı · çatlak ve sondaj |
| PARAGON | Paletli montaj kulesi · asansör | Roket montaj kulesi · asansör |
