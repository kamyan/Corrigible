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
limanındaki olay iyi gibi, karmaşık olaylar olmasın, uçağın olayı karmaşık geldi." Önceki iki turun listesi B.8'de. Dördüncü turun ikinci listesi B.4'te, beşinci turun kodla sınanmış üçüncü listesi B.5'te, altıncı turun bina şartları ve Mars listesi (dördüncü liste) B.9'da._

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

### B.9 Dördüncü liste (altıncı tur): bina şartları ve Mars

_Geliştirici (2026-11-27, ikinci geri bildirim): "konteyner limanında iniş yeri doluysa bir hasar verip itsin, yanardağ lokasyonunda lav saçma, binanın içinde olsun, sel basmış çarşıya değiştir, denizaltına başka kural bul, sibirya veri merkezina başka kural bul"._

_Geliştirici (2026-11-27, ilk istek): "12 yapay zeka ve Mars için binaya özel olay ekleyelim, gemi yalpalanması gibi; daha önce oluşturduğun fikirlerden olabilecekleri al ve yenilerini üret."_

**Bu turda değişenler**

- **TESSEL.** Vinç artık iniş yeri dolu diye beklemez. İniş yerindeki birim 1 hasar alır ve konteynerin gidiş yönünde bir kare itilir. İtilemezse hasarı yine alır, vinç o sefer bekler. Konteyner dinamitle yıkılmaz; bu artık karar değil, şarttır. Tesis adı Çip fabrikası yerine Konteyner limanı olur.
- **CAIRN.** Yanardağ krateri çıktı; yer bir binanın içidir: bakır izabe tesisinin döküm holü. Lavın işi oradaki karşılığıyla sürer: **Döküm oluğu**. Dozer yedeği düştü, çünkü dışarıdadır ve "iter, itemezse bekler" artık limanın vincinin işidir. Yerine konveyör (koşullu) geldi.
- **EDDY.** Yer kesin olarak Sel basmış çarşı olur. Girdap aynen kalır. Tesis adı, kanat adları ve bina notu yeniden yazıldı.
- **QUORUM.** Yer denizaltı kalır. Tek sıra yerine **Su basmış bölme** gelir. Yedek sonar pingidir (koşullu). Tipi yedeği kalktı.
- **BOREALIS.** Yer Sibirya veri merkezi kalır. Tipi yerine **Soğuk koridor** gelir; iki sınavı da geçti. Önceki taslağın egzoz fanı (koşullu) elendi. Yedek kutup gecesidir (koşullu, aynı yer). Yürüyen merdiven yeri değiştirdiği için yedekten çıktı.
- **Kararlar.** "Tipi ve aygıtlar" ile "Liman ve dinamit" kararları kalktı. "BOREALIS" ile "Su basmış bölmede pusu ve köpek" kararları eklendi.
- Beş YZ için 18 aday ilk turdaki iki sınavdan geçti. İkisini de geçenler: liman (Varyant B), döküm oluğu, su basmış bölme, sel basmış çarşının adları. Sibirya için ayrıca 23 aday aynı iki sınavdan geçti. İkisini de geçen iki aday aynı kuraldır: soğuk koridor ve gevrek çelik. Öbürleri aşağıda "Elenen dikkat çekici adaylar" tablosunda.

Bu tur, B.2, B.4, B.5 ve B.6'nın adayları ile yeni adaylar birlikte tartıldı. Dokuz YZ ve Mars için 77 aday vardı. Her aday iki sınavdan geçti. Birinci sınav, kuralın `Core`'da gerçekten kurulup kurulamayacağıydı. İkincisi, B.1'e uyup uymadığıydı: tanınır yer, beklenen ilk şey, tek cümle. Her yer için bir öneri ve bir yedek seçildi. "Koşullu" yazan aday iki sınavın yalnız birini geçti; neden geçemediği satırında yazar. İkinci geri bildirimde TESSEL, CAIRN, EDDY, QUORUM ve BOREALIS için 18 aday aynı iki sınavdan yeniden geçti. BOREALIS için 23 aday daha tartıldı. VERITY, WAYPOINT ve HALCYON değişmez. **Hiçbiri uygulanmadı ve ölçülmedi.**

**Güncellenen motor gerçekleri** (B.5'in 1. maddesinin yerine geçer; öbür maddeler geçerli)

1. Oda süreleri şöyle: giriş 6, ara 6, hedef 10, dönüş 6 tur (`Balance.facility*Turns`). Son turda olay adımı yoktur (`t < MaxTurns`). "Çift turlar" 6 turluk odada 2 ve 4, hedefte 2, 4, 6 ve 8 demektir. "3., 6. ve 9. tur" 6 turluk odada yalnız 3, hedefte 3, 6 ve 9 demektir.
2. Yol değiştiren olay son turdan önceki evrede yapılamaz (`t < MaxTurns - 1`). Bu yüzden 6 turluk odada en geç 4., hedefte en geç 8. turda yapılır.
3. Oyunda artık yürüyüş yolu çiziliyor, ama `Core`'da yürüyüş hâlâ yalnız bitiş karesidir (`Reachable`, `Walk`). "Giren" ya da "geçen" yakalanmaz. Yalnız "tur sonunda duran" ve "itilen" yakalanır.
4. Her yeni şart Katar ve Yalpa gibi **yeni bir olay kimliği** alır. Eski olayın kodu eski kimliğiyle kalır. Böylece örnek tahtanın altın imzaları (`RaidEngineTests`) değişmez.
5. Ortak kurallar değişmedi. Bir evrede birim en çok 1 olay hasarı alır (`B.Ev.Hurt`, `Baskin.cs:596`). RNG yoktur, kural tarafa bakmaz, olay bir tur önceden görünür, hiçbir kare çıkıştan kopmaz. B.5'in 5. maddesi ("yalnız ekip iter") makineler için geçerlidir. Bu turda tek yer itmesi geldi: liman vinci (egzoz fanı elendi). Soğuk koridorun fazla hasarı olay adımı dışında da bu listeye yazılır. Liste yalnız olay adımında temizlendiği için birim oyuncu turu ile makine evresi boyunca en çok 1 fazla hasar alır.

**Öneriler**

| YZ (paket) | Yer | Kural (tek cümle) | İmza makinesiyle ilişkisi | Boy | Kaynak |
|---|---|---|---|---|---|
| **TESSEL** (jk) | Otomatik konteyner limanı (Busan, Yokohama) | **İstif:** vinç çift turların sonunda işaretli konteyneri öbür işaretli yere taşır; iniş yerindeki birim 1 hasar alır ve konteynerin gidiş yönünde bir kare itilir, itilemezse vinç o sefer bekler. | Mühürleyici kalasını iniş yerine kurarsa vinç o sefer bekler; kalas olay hasarı almaz. Ekip de kendi telini ya da kalasını iniş yerine kurup vinci bedavaya durdurabilir. Vinci gövdesiyle tutan ise her takvim turunda 1 can öder. Ekip bir makineyi duvara yaslı iniş yerine çekerse makine her taşımada 1 hasar alır. Cıvata makineyi itmeye karşı tutar, vuruştan korumaz. Hedef odada dört taşıma olur (2, 4, 6, 8). | M | B.2 (beğenildi); itme ikinci geri bildirimden |
| **HEADWATER** (sa) | Kereste fabrikası (Amazon kıyısı) | **Yangın:** atış, alev ya da patlama değen kereste yığını bir tur sonra yanar ve yok olur. | Alev robotunun alevi nesnede kesilmez. Yığının ardındakini hemen yakar, yığını bir tur sonra siler. Siper iki taraf için de geçicidir. | S | B.2 |
| **CAIRN** (af) | Bakır izabe tesisi, döküm holü (Zambiya Bakır Kuşağı). Erimiş bakır ocaktan potaya, zemine gömülü açık oluklarda akar. | **Döküm oluğu:** tur sonunda erimiş metal oluğunda duran her birim 1 hasar alır. | Havan 1 kare yürür, bu yüzden 1 karelik oluğu ancak içinde turunu bitirerek geçebilir. Akıllı kipte oluğun öbür yakasında kalır ve oluğun üstünden atar. Temel kipte oluğa girer ve yanar. Ekip oluğu tek yürüyüşte bedava aşar: oluk yavaş makineye karşı bir hendektir. Oluğa itilen makine olay adımında hâlâ oradadır, kesin 1 hasar alır. | S | B.5'in lavı, bina içine taşındı |
| **EMBER** (me) | Akaryakıt deposu | **Varil:** darbe alan varil bir tur sonra patlar; dört yanındaki herkes 1 hasar alır. | Patlayan robot varilin yanında infilak ederse varilin fitili kurulur. Varil sonraki olay adımında patlar: aynı köşe iki turda iki kez patlar. Ekip de varili vurup makinelerin toplandığı köşeyi bir tur önceden ateşler. | S | B.2 |
| **SOLACE** (in) | Gökdelen (Mumbai) | **Asansör:** kabin çift turların sonunda içindekini öbür durağa taşır; iniş durağı doluysa bekler. | Kalkan dronu plaka için makineye bitişik durur. Kabine itilen dron öbür durağa gider ve plakayı vereceği makineden kopar. Kabin ekibin aracıdır, ama turunu durakta bitiren makine de taşınır. | S | B.2 |
| **EDDY** (sea) | Sel basmış çarşı (Jakarta'daki Glodok gibi, muson selinin bastığı bir elektronik çarşısı; kurum adı ve amblem kullanılmaz) | **Girdap:** rögar 3., 6. ve 9. tur sonunda açılır ve dört yanındakileri içine çeker. | Çoğaltıcı kopyasını bitişik boş kareye uyandırır. Rögarın yanındaki Çoğaltıcı ile kopyası aynı emmede çekilir: ilki rögara girer, ikincisi ona çarpar. "Kopya atölyesi" kanadı Çoğaltıcı'ya ve `fabrika` bölge özelliğine (kopya dolabı) uyar. | S | B.2 |
| **QUORUM** (eeu) | Denizaltı (Karadeniz'de bir denizaltı üssüne yanaşık) | **Su basmış bölme:** su basmış karede duran birim eylem yapamaz, yalnız yürür. | Kilit yürüyüşü keser, su eylemi keser. Sudaki uzman kilitlenirse o tur hiçbir şey yapamaz: ne yürüyüp sudan çıkar ne yeteneğini kullanır. Kilitlenmeyen uzman önce yürüyüp sudan çıkar, sonra eylemini yapar. Kilitleyici'nin kendisi de suda kilit atamaz. Kilit hattı suyun üstünden geçer. Ekip sabotajla ya da EMP ile makineyi suya iterek o turki niyetini söndürür; iptal önizlemede görünür. | S–M | B.4'ün yüzme havuzu (HEADWATER kerestede, boşta) |
| **BOREALIS** (ru) | Sibirya permafrost veri merkezi. Veri merkezleri serbest soğutma için Sibirya'ya kurulur: eksi kırk derecelik dış hava raf sıralarının ön yüzündeki soğuk koridora doğrudan basılır. | **Soğuk koridor:** soğuk koridorda duran birim aldığı darbeden 1 fazla hasar alır. | Kırağı dronunun buzu (artı biçimli alan, menzil 3, iki tur; `Baskin.Signatures.cs:134`) hasar vermez, yürüyüşü buzlu karede bitirir. Kırağı artıyı koridora atarsa, yürüyüşünü orada bitiren uzmana dronun 1'lik atışı 2 vurur: buzun yavaşlatması hasara döner. Ters yönde de işler. Kırağı uzaktan atmak için yerinde durmayı sever (2 can). Koridorda durursa ekibin tek sabotajı ya da EMP'si onu düşürür. Ekip makineleri koridora iterek aynı fazlayı onlara da ödetir. | S–M | yeni (iki sınav) |
| **PARAGON** (cn) | Otomobil fabrikası | **Robot kol:** kol her tur sonunda önündeki kareye vurur ve saat yönünde çeyrek döner. | Kolun düşürdüğü makine hurda bırakır, Montajcı da bitişik hurdadan dron kurar: kol Montajcı'ya hurda üretir. Temel kipte Montajcı hurdaya giderken vurulur. Akıllı kipte `EventPos` onu koldan uzak tutar. | S | B.2 |
| **MARS** · istasyon | Mars yörünge istasyonu (Phobos'a demirli; YZ yaşam halkasının dönüşünü durdurmuş) | **Ağırlıksızlık:** itilen birim bir engele ya da birime çarpana kadar süzülür. | Son YZ'nin imzası henüz seçilmedi (aşağıda "Mars"). | S | B.2 (buz) ile B.5 (EMBER uzay istasyonu) birleşti |
| **MARS** · anten | Derin uzay anteni yer istasyonları (Dünya) | **Çanak:** çanak Mars'ı izler: ışın her tur sonunda bir sütun kayar, uçta geri döner; ışının sütununda duran 1 hasar alır. | Kural makineden bağımsızdır. Kilitleyen türden bir makine en sert eşleşmeyi verir: ışının geleceği sütunda kilitlenen uzman kaçamaz. | S | yeni |

**Motor, kanatlar, çekinceler**

| YZ · yer | Motor (dosya ve metot) | Kanatlar | Çekince |
|---|---|---|---|
| **TESSEL** · Liman | Yeni kimlik `istif`; `raf` ve takvimi dokunulmadan kalır. `EvState.P` ve `Q` iniş yerleridir, `Mode` konteynerin hangisinde olduğunu tutar. Konteyner `Shelf` bayraklı duvardır. Takvim `t % 2 == 0 && t >= 2 && t < MaxTurns - 1`. Gidiş yönü `d`, kalkılan yerden iniş yerine giden birim vektördür. Taşıma sırası şöyle: (1) Konteyner yer değiştirdiğinde `ExitSafe` (`Baskin.Events.cs:123`) tutmazsa vinç sıkışır; iniş yerinde nesne (kalas, tel, hurda) varsa vinç bekler. İkisinde de kimse hasar almaz. (2) İniş yerinde birim, yatan gövde ya da yük varsa itme karesi `land + d` denetlenir. Kare tahta içi bir zemin olmalı; birim, gövde, yük, kapı ve menfez taşımamalı; konteyner indikten sonra çıkışa bağlı kalmalı (`after[ny, nx] >= 0`). Birim de cıvatalı olmamalı (`Bolted`, `Baskin.Counters.cs:38`). (3) Uyuyorsa birim `EventHurt` (`:172`) ile 1 hasar alır ve `d` yönünde bir kare kayar. Çarpma ve zincirleme itme yoktur. Kabloya düşerse `CableHurt` çağrılır, tavan ikinci hasarı keser. Gövde ve yük aynı kareye hasarsız gider. Sonra konteyner iner. (4) Uymuyorsa ayaktaki birim yine 1 hasar alır; cıvatalıysa `BoltHold` Fx'i oynar. Birim yerinde kalır, vinç bekler ve sonraki takvim turunda yeniden dener. **Şart olan iki düzeltme:** `EventHurt`'ten sonra `lc.O` yeniden denetlenir, çünkü iniş yerinde ölen 1 canlı aygıt `GadgetDown` (`Baskin.Gear.cs:761`) ile hurda bırakır; doluysa vinç bekler. `DynamiteTarget`'ın (`Baskin.cs:907`) duvar dalına `!(wc.Shelf && Is(B, "istif"))` eklenir. `ShelfBroken` (`:314`) yalnız `raf`'a bakar; ek yapılmazsa yıkılan konteynerin yerine vinç sonraki taşımada yeni konteyner yaratır. Bağlantılar: `EventStepTurns` (`Baskin.Rooms.cs:608`) 6 turlukta {2, 4}, hedefte {2, 4, 6, 8} verir. `Upcoming` "VİNÇ" der. `Hazards` konteyneri, taşıma turunda iniş yerini (`crane_land`) ve doluysa itme okunu (`shove`, itilemezse `Hit`) çizer. `EventPos` taşıma turunda iniş yerine −2 verir. Kurulum `SetupShelf` ve `ShelfSafe` kalıbıdır. P ile Q aynı doğrultuda, aralarında 2–4 kare bulunur. İkisi de nesnesiz, birimsiz zemindir; `Door` ve `Vent` 0, `ExitDist ≥ 2`, hedefe uzaklık 1'den büyük, `CanonX` 1..6 olur. İki konumda da `ExitSafe` tutar. Kademe 0'da iki itme karesi (Q+d, P−d) açık olur. Q ana yoldaysa (`TargetPath`) +10 puan alır. `FacilityMaps.PhysicalKinds`'e "konteyner" ve "vinc_yeri" eklenir. | İstif sahası · Vinç altı · Liman kontrol kulesi | "İtilemez" listesi uzundur (kenar, duvar, nesne, birim, gövde, yük, kapı, menfez, kaçış güvencesi, cıvata). Kural metnine yazılmaz; oyuncu yalnız "itilemezse" görür, itme oku sonucu bir tur önceden gösterir. "Nesne varsa bekler" dalı "iniş yeri kapalı" diye tek satır kalır. Konteynerin vuruşu ile kablo aynı evrede ikinci hasar vermez. Montajcı'nın hurdası (`MachineScrap`) yalnız montaj imzalı tahtada iniş yerine düşer, TESSEL'de olmaz. Dönüş tahtası plandan kurulduğu için konteyner başlangıç yerine döner (B.5, 7. madde). Tesis adı Çip fabrikası yerine Konteyner limanı olur. |
| **HEADWATER** · Kereste | Gazın ateş kancası `Fire` (`Baskin.Events.cs:267`) kullanılır. Kanca bugün atış hattından, alevden ve `Blasted`'dan çağrılır. Kancaya düşen "kereste" nesnesine fitil kurulur (`Fuse`, `Due = Turn + 1`, `EndCracks` sırası). Fitil dolunca nesne kalkar. Yol yalnız açılır, `ExitSafe` gerekmez. Olay adımında (`InEvent`) nesne hasarı olmadığı için yanan yığın komşusunu tutuşturmaz, zincir yok. | Tomruk sahası · Hızar holü · Kumanda odası | `Fire` bugün EMP'den de çağrılıyor. Cümle EMP'yi saymadığı için kereste kolunda EMP ateş sayılmamalı. Sabotaj ve koşu darbesi ateş değildir. Varille aynı fitil ailesindendir, ama sonuç ayrıdır (siper kalkar, alan hasarı yok). Tesis adı değişir (bugün Hidroelektrik santrali). |
| **CAIRN** · Döküm oluğu | Yeni kimlik `oluk` ve yeni kare bayrağı `Cell.Melt`. Bayrak `Cell` sınıfında (`Baskin.cs:137`) öbür olay bayraklarının yanına girer, `EventSignature`'ın hücre dizgisine "m" olarak eklenir. Faz olayının hep canlı kanalının kalıbıdır (`EndPhases`, `Baskin.Events.cs:1131`). Her olay adımında `Melt` karedeki `OnBoard` birime `EventHurt` verilir. `CableHurt`'ün anında yakması kullanılmaz. Takvim yoktur: `EventStepTurns` her adımda çalışır, `Upcoming` boştur. Oluk tahtada kalıcı olarak işaretlenir (`Hazards` "melt"). `EventPos` oluk karesine −3 verir. `FacilityMaps` 1–2 oluk çizer: 1 kare geniş, 3–6 kare uzun, ana yola (`TargetPath`) dik. Kapı, menfez ve hedef komşusu olmaz. Kareler zemin kaldığı için `ExitSafe` ve `TeamSafe` gerekmez. | Cevher hangarı · Döküm holü · Kumanda kabini | İş lavla aynıdır (kalıcı hasar alanı); yer bina içine taşındı, geometri inceldi. "Oluğa giren" değil "olukta turunu bitiren" yanar. Yol çizimi oluğun üstünden geçer, sunum bunu "geçilir, durulmaz" diye göstermeli. B.5'in "dar lav bedava aşılır" çekincesi burada özelliktir: oluk yavaş makineye karşı hendektir. Geniş havuz istenirse aynı kod "Cüruf havuzu" (2–3 kare) olarak çizilir; o zaman girişten hedefe oluksuz yol şartı geri gelir. Takvim eklenmemeli: takvimli döküm, elenen metronun "takvimli hat hasarı" işine kayar. Tesis adı Cevher hangarı yerine Bakır izabe tesisi olur; eski ad kanada iner. Bugünkü bina notunun çatlak tavan cümlesi de değişir. |
| **EMBER** · Varil | Tüpün fitili kullanılır (`EventObjectHit`, `Fuse.Due = Turn + 1`, `EndTubes` sırası). Uçuş hattının yerine `BlastArea`'nın (`Baskin.cs:1199`) dört komşusu gelir; komşulara `EventHurt` verilir. `IsEventObject`'e (`Baskin.Chain.cs:15`) `varil` eklenir. Olay adımında `HurtObj` kısa devre yapar, zincir kendiliğinden olmaz. | Dolum sahası · Varil ambarı · Pompa dairesi | Robotun patlaması makine evresindedir (`InEvent` değil). Varilin fitilini bu yüzden kurar; bunun testle kilitlenmesi gerekir. Yer bugünkü rafineriye yakın; ad korunabilir. |
| **SOLACE** · Asansör | Yük asansörü (`EndLift` :1036, `LiftDeparts` :352) kullanılır. Bu kod WAYPOINT Katar'a geçtiğinden beri boşta. Yolculuk evresi (`Cab`, `Mode` 2/3) ve iniş hasarı kalkar: kalkış adımında duraktaki birim doğrudan öbür durağa konur. Takvim 2 ≤ t ≤ MaxTurns−2. `Upcoming` "KABİN" der. | Lobi · Asansör holü · Sunucu katı | Durak geçide, menfeze ve donanımın tek boş komşusuna konmaz (§2.4). Kare değil birim taşındığı için `ExitSafe` gerekmez. Tesis adı değişir (bugün Çağrı merkezi). |
| **EDDY** · Girdap | Süzgeç olayı (`EndDrain` :1144) olduğu gibi kullanılır. Yalnız takvim değişir: `DrainAt` (3 ya da 6) yerine `t % 3 == 0 && t < MaxTurns`. Çekme sırası kuzey, doğu, güney, batıdır; çarpışma tavanlıdır. Kimlik `girdap` olur, `suzgec` ve takvimi dokunulmadan kalır. Adlar `GameData.cs:102–105`'teki `sea` kaydında değişir. Bina adı "Elektronik fabrikası" yerine "Sel basmış çarşı" olur. Kanatlar "Bodrum ambarı · Sintine kanalı · Kopya atölyesi" yerine "Tezgâh sokağı · Kapalı pasaj · Kopya atölyesi" olur. Bina notu şu olur: "Muson selinin bastığı bir elektronik çarşısı. Tezgâhlar suyun altında; kapağı kalkmış rögar 3., 6. ve 9. turun sonunda suyu yutar ve dört yanındakileri içine çeker." Hepsi `Loc.N` ile sarılır; `strings.txt`'e İngilizceleri yazılır: "Flooded Bazaar", "Stall Street", "Covered Arcade", "Copy Workshop". Şartın notu Katar ve Yalpa gibi `facilityNote`'a (`GameData.cs:38`) girer. Adlar aşama 12 K7 emsaliyle bayraktan bağımsız değişir. İstenirse Katar'ın `trainRooms` emsaliyle (`FacilityMaps.cs`, `themedNames`) dokuz oda adı eklenir: Çarşı kapısı · Tezgâh sokağı · Telefon dükkânı / Arka sokak · Rögar pasajı · Kablo deposu / Üst kat merdiveni · Tamirci koridoru · Kopya atölyesi. Oda adları izi değiştirmez. | Tezgâh sokağı · Kapalı pasaj · Kopya atölyesi (yedek: Çarşı kapısı · Rögar pasajı · Arka depo) | 6 turluk odada tek emme olur. "Kapalı çarşı" adı kullanılmaz, bina adını yineler. Girdap giriş odasında yoktur, bu yüzden "Rögar pasajı" gibi adlar yalnız ara ya da hedef kanatta kullanılır. Kapalı çarşı 8×8 odalara uyar (dükkân bölmeleri duvar olur); açık pazar sokakları oda planına uymaz. Bugünkü notun bodrum ve sintine pompası anlatımı kalkar. |
| **QUORUM** · Su basmış bölme | Yeni kimlik `bolme` ve yeni kare bayrağı (`Cell.Flood`; ya da `ev.Area`). Takvim ve olay adımı yoktur, Katar gibi hep açıktır. Makine tarafında tek yer `IntentTarget`'ın (`Baskin.cs:1159`) başıdır. Makinenin karesi sudaysa, ters dönme ve imza dallarından önce `None` döner. Bu yol hem uygulamayı (`EndTurnCore`, `Forecast`; `t.None` ise `continue`) hem puanlamayı (`ScoreTarget`) kapatır. Patlama, onarım, kalkan ve kopya niyetleri de bu yoldan geçer. `PlanEnemies`'in tür döngüsü atlanmaz, `best` yine dolar ve plan çökmez. Ekip tarafında `CanAct` (`Baskin.cs:1037`) ve `CanAction` (`Baskin.Actions.cs:209`), birimin karesi sudaysa false döner. Yürüyüş bedelli eylemler `CanMove`'dan geçtiği için `CanAction` içinde ayrıca denetlenir. Aygıtlar `RunDevices`'ta (`Baskin.Gear.cs:620`) suda çalışmaz; kural tarafa bakmaz. Kilit yürüyüşü bugünkü gibi keser (`Moved = Locked`, `Baskin.cs:1768`). `FacilityMaps` 4–8 karelik bir ya da iki havuz çizer: geçit, menfez ve çıkış dışında. Hedefe sudan geçmeyen bir yol bırakır; yol çizimi suyun üstünden geçebilir. Duvar değişmez, `ExitSafe` gerekmez, hasar ve RNG yoktur. `EventPos` su karesinde atıcı makineye −1,5, yakın dövüşçüye −0,5 verir. `Hazards` suyu kalıcı işaretle çizer. | Torpido dairesi · Akü dairesi · Kumanda merkezi | Su basması herkesin beklediği ilk görüntüdür. "Suda eylem yapılmaz" ise beklenen ilk sonuç değildir (beklenen boğulma ya da yavaşlamadır). Kart bunu "ıslanan alet ve robot kısa devre yapar" diye anlatmalı. VERITY'nin bulutuna yakındır, ama ayrıdır: bulut nişanı keser ve içerdekini korur; su yalnız içerdekinin eylemini keser, içerdeki hedef alınabilir. Kural bölmenin kendisidir; ekranda görünür (tek sıranın "görünmez" kusuru burada yok). Pusu ve köpek kararları kural metnine istisna olarak girmez (aşağıda). Tesis adı Devlet arşivi yerine Denizaltı olur; bölge özelliği "Dar koridor" kalabilir. |
| **BOREALIS** · Soğuk koridor | Yeni kimlik `koridor` ve yeni kare bayrağı `Cell.Cold`. Bayrak `Cell` sınıfında (`Baskin.cs:137`) öbür olay bayraklarının yanına girer, `EventSignature`'ın (`Baskin.Events.cs:1701`) hücre dizgisine "c" olarak eklenir. Takvim ve olay adımı yoktur, Katar gibi hep açıktır: `EventEnd`'e kol eklenmez, `EventStepTurns` (`Baskin.Rooms.cs:608`) boş döner, `Upcoming` boştur. Tek kural satırı `Hurt`'tedir (`Baskin.cs:596`). Satır tavan bloğundan sonra, buz kabuğu ve plaka dallarından önce gelir: `!B.InEvent && Is(B, "koridor") && CellAt(B, u.X, u.Y).Cold && !B.Ev.Hurt.Contains(u.Id)` ise `B.Ev.Hurt.Add(u.Id); n += 1;` olur ve Fx "brittle" yazılır. Fazla hasar olay hasarıdır. `Hurt` `EvDamage*` sayaçlarını yalnız `InEvent` iken artırdığı için sayaçlara elle 1 yazılır. Tavan `B.Ev.Hurt` listesidir. Liste yalnız `EventEnd` sonunda temizlenir (`Baskin.Events.cs:908`), `EventEnd` ise `Ev` varken her turda çağrılır (`Baskin.cs:1720`). Bu yüzden birim oyuncu turu ile makine evresi boyunca en çok 1 fazla hasar alır. Olay adımı olmadığı için listeye girmek başka bir olay hasarını kesmez. Plaka ve buz kabuğu fazlayı da emer (önce n + 1, sonra plaka 1). Atış, yakın vuruş, havan, patlama, sabotaj, EMP, çarpma (`Push` :712) ve kablo (`CableHurt`) hep `Hurt`'ten geçer. `HurtObj` ayrı yol olduğu için kural nesneye işlemez. Önizleme `Ev`'i kopyaladığı için (`Baskin.cs:317`) 2 hasarı kendiliğinden gösterir. `FacilityMaps` hedefte 2, ara odada 1 şerit çizer: 1 kare geniş, 4–6 kare uzun, raf sıraları arasında. Geçit, menfez, kablo ve uyanma kenarı (`SpawnerSides`) şeride girmez. `CableHurt` olaysız dalda düz `Hurt` verdiği için koridordaki kablo 2 vururdu. Girişten hedefe koridora girmeyen bir yol kalır. Kareler zemin kaldığı için `ExitSafe` gerekmez; RNG yoktur. `Hazards` koridoru kalıcı bir işaretle çizer; işaret Kırağı'nın "frost" işaretinden ayrı olmalı. `EventPos` (`Baskin.Events.cs:1225`) koridor karesine −1, atıcı makineye −1,5 verir. Tesis izi ve kayıt sürümü artar. | Dış hava panjuru · Soğuk koridor · Sunucu bunkerı | Soğukta beklenen ilk şey donmaktır. "Eksi kırkta çelik cam gibi kırılır" ikinci akla gelendir (Yakutsk'ta kırılan aletler, kışın çatlayan raylar). Kart bunu tek cümleyle anlatmalı. "Soğuk koridor" bir veri merkezi terimidir ve herkesçe bilinmez; sunum şeridi açık göstermeli. Döküm oluğuna yakındır, çünkü ikisinde de orada durmak pahalıdır. Ama oluk kendiliğinden vurur; koridor tek başına zarar vermez, yalnız gelen darbeyi büyütür. Kural tarafa bakmaz, ekibin itme ve patlama hasarını da büyütür. 2 canlı makineler tek isabetle, 3 canlı uzman iki isabetle düşer. Patlayan robotun ve soğutma ünitesinin dört komşuya patlaması koridorda sertleşir. Bot ölçümü şart. "Aynı turda ikinci darbe fazla almaz" sınırı cümleden değil ortak tavandan gelir; rehberde yazılmalı. Aynı kuralın panjur önündeki ızgara sürümü (gevrek çelik) de iki sınavı geçti; yer ayrıntısı daha niş olduğu için koridor seçildi. Ad bugünkü "Kutup veri merkezi"ne uyar. |
| **PARAGON** · Robot kol | PARAGON'un bugünkü kaynak kolu kullanılır (`EndArm` :1115, `ArmTile`, `ArmFaces`). "Kola gelen darbe dönüşe adım ekler" maddesi kalkar (`EventObjectHit`'in `kol` dalı ve `Ev.Count`). Vuruş `InEvent` altında tavanlıdır. 6 turluk odada 5, hedefte 9 vuruş olur. | Parça ambarı · Kaynak hattı · Hat kontrol odası | Duvara, kenara ve hedefe bakan yön atlanır; bu bugünkü kuraldır. Ad bugünkü "Montaj fabrikası"na yakın. |

**Mars**

Mars finaldir. On iki bölge bitince son şalter açılır. Şalter indirilince son YZ uyanır ve tek binanın üç kanadına üç baskın yapılır; üçüncüsü çekirdektir. Şart Mars'ta da giriş odasında yoktur. Finalin savunma baskını yoktur.

| Kurgu | Öneri | Yedek |
|---|---|---|
| **1. İstasyon** (öncelikli) | **Ağırlıksızlık** (#44, iki sınav). Kanatlar: Yaşam halkası · Reaktör modülü · Çekirdek modülü. Geçitler hava kilididir. | **Güneş fırtınası** (#45, iki sınav): "Güneş fırtınası 3., 6. ve 9. tur sonunda vurur: radyasyon sığınağında durmayan herkes 1 hasar alır." |
| İstasyonun yüzey çeşidi | Kurgu istasyonu Mars yüzeyine koyarsa ağırlıksızlık yanlış olur (0,38 g). Yerine **Düşük yerçekimi** (#46, iki sınav) gelir: "itilen her birim iki kare gider." Kanatlar: Hava kilidi · Sera kubbesi · Çekirdek modülü. | Güneş fırtınası aynen geçerli. |
| **2. Anten** | **Çanak** (#48, iki sınav). Kanatlar: Goldstone · Madrid · Canberra. Bunlar gerçek üç yer adıdır; kurum adı ve amblemi kullanılmaz. | **Yayın** (#76, **koşullu**: ikinci sınavda parantezli ikinci cümle yüzünden kaldı; sadeleşti): "Yayın: çanak çift turların sonunda yayın yapar; önündeki ilk engele kadar hatta duran herkes 1 hasar alır." |

- **Ağırlıksızlığın motoru.** `Push` (`Baskin.cs:712`) içinde birim bir kare ilerler (`u.X = nx; CableHurt`). Olay açıksa döngü sürer: sonraki kare tahta içi, zemin, nesnesiz ve birimsizse birim ilerlemeye devam eder. Çarpma itmenin bugünkü kuralıyla işler: duvar 1 hasar; nesne 1 hasar ve `HurtObj`; birim çarpışmasında ikisine 1. Cıvatalı makine önce tutar. Takvim, geometri ve RNG yoktur; önizleme kopyada çalışır. Birinci sınavın üç düzeltmesi şarttır:
  1. Süzülmenin kablosu ve çarpması `EventHurt` ile verilir; tek itmede en çok 1 hasar olur. Mars'ın olay adımı olmadığı için bu tavan başka bir hasarla çakışmaz.
  2. Süzülme çarpmayla bittiyse Tampon türü `Displace` (`Baskin.Signatures.cs:177`) ve koşu darbesinin itme döngüsü (`Baskin.Actions.cs` ~584) durur. Yoksa ikinci itme duvara yeniden vurur.
  3. Kanca ve çapanın çekmesi süzülmez. Çekme `Displace` → `Push` yolundan geçtiği için muaf bayrağı gerekir. Kural yalnız itilene işler.
- **Ağırlıksızlığın imza sorusu.** Bugün makine itmez. Ekip EMP ile dört komşu makineyi duvarlara savurur; sabotaj ve koşu darbesi de uzun menzilli olur. Son YZ'ye iten bir makine (Tampon türü) verilirse kural iki yana da keskinleşir ve cıvata önlemi değer kazanır. Verilmezse final ekip için kolaylaşabilir; bot ölçümü şart.
- **Ağırlıksızlığın çekincesi.** Sonuç Yalpa'nın kaymasına akrabadır. Ama tetik itmedir, takvim yoktur ve kayma engele kadar sürer. İkinci sınav bunu ayrı iş saydı. Geliştirici tekrar sayarsa fırtına öne geçer. Mars süzülmeyi alınca BOREALIS'in donmuş gölü (#68) düşer.
- **Güneş fırtınasının motoru.** Faz olayının tersidir (`EndPhases` kalıbı). Sığınak yeni bir `Cell` bayrağıdır. Takvim `t % 3 == 0 && t < MaxTurns`; sığınak dışındaki her birime `EventHurt` verilir. Plan her modüle girişten ve hedeften 3 yürüyüş içinde 2–4 sığınak karesi koyar; geçit ve menfez sığınak olmaz. Zırh plakası fırtınayı emer (`Hurt`'te önce plaka). Akıllı kipte makineler sığınağa koşar. Çekince: aygıtlar sığınak dışında yanar. Bütün oda hasar alanıdır; finale yakışan iddia budur.
- **Düşük yerçekiminin motoru.** `Push` iki kez çağrılır. Koşu darbesinin emsali geçerlidir: ilk itme çarparsa, düşürürse ya da cıvata tutarsa ikinci itme olmaz. Çapa muafiyeti gerekir.
- **Çanağın motoru.** `EventEnd`'e yeni kol eklenir. Sütun turun fonksiyonudur ve kanonik x ile hesaplanır (`CanonX`). Işın iç sütunlarda (1..6) gidip gelir; kapı sütunları taranmaz. Duvar ışını kesmez, çünkü ışın yukarıdan gelir. Sütundaki birimlere `EventHurt` verilir; `Hazards` sonraki sütunu işaretler. Ara modülde 5, çekirdekte 9 adım olur. Çekince: kurgu Dünya'da kalır; "Mars'taki son YZ" anlatısını telsiz taşır. Anten seçilirse ağırlıksızlık boşa düşer, donmuş göl (#68) BOREALIS yedeği olarak geri gelebilir.
- **Yayının motoru.** Tüpün uçuş hattı (`TubeLine` :373) her olay adımında yeniden hesaplanır. Birimler hattı kesmez, nesne keser. Takvim çift turlardır. `Hazards` hattı her tur yeniden çizer.
- **Hava kilidi** (#19: "kilit yalnız tek turlarda çevrim yapar") iki sınavı geçti, ama final için tek başına zayıf. Kapıda bekleyen uzman tahtada değildir (`AtGate`); hedef alınamaz, kural yalnız bir tur süre yakar. B.1 tek kural istediği için eklenmez. Görsel ayrıntı olarak geçitlerin adı kalabilir.

**Yedekler**

| YZ | Yedek yer | Kural (tek cümle) | Durum | Motor | Boy | Kaynak | Neden yedekte |
|---|---|---|---|---|---|---|---|
| TESSEL | Kore DMZ gözetleme istasyonu | **Mayın:** mayına basan orada durur ve 1 hasar alır; mayın patlar ve gider. | İki sınav | Yeni `Cell` bayrağı (`Mine`). `Reachable` ve `Walk`'ta kırağının kalıbı kullanılır: mayın karesi bitiş olur ama oradan genişlenmez. Patlama birimin vardığı yerde olur: yürüyüş sonu, makinenin `PlanEnemies`'teki karesi, `CableHurt`'ün çağrıldığı itme, çekme ve atılma. `EventHurt` verilir, bayrak silinir. Tampon'un koşusu mayında kırılır. | M | yeni | Liman geliştiricinin beğendiği yer. Mayın, döküm oluğuyla "kareye basma hasarı" ailesine yakındır (oluk kalır, mayın gider). Mühürleyici kalasla mayınsız yolu kapatır. |
| HEADWATER | And Dağları jeotermal kaplıcası (El Tatio) | **Şifalı su:** tur sonunda sıcak havuzda duran birim 1 can kazanır. | **Koşullu.** Birinci sınavı geçti. İkincide yalnız TESSEL kaplıcasının (#26) kopyası diye elendi; TESSEL onu almadığı için serbest. | `EventEnd` kolu. Havuz `Cell` bayrağıdır (2–4 kare). `Hp = min(Max, Hp + 1)`, Fx `heal`. Hasar olmadığı için tavana girmez; yatan gövde iyileşmez. Akıllı kipte `EventPos` artı terim alır. | S | yeni | İyileştirme zafer oranını oynatır, bot ölçümü şart. Alev robotu (2 can) havuzda her tur iyileşir. |
| CAIRN | Cevher hangarı (bugünkü bina; Kongo kobalt ya da Gine boksit hangarı) | **Konveyör:** konveyör çift turların sonunda ağzının önüne atışı kesen bir cevher yığını döker. | **Koşullu.** Birinci sınavı geçti. İkincide kaldı: yığının kırılabilmesi ikinci bir cümle istiyor, kesin tanım da bir istisna zinciri getiriyor (karede birim varsa birim 1 hasar alır ve yığın dökülmez; hat doluysa ya da güvence bozulursa döküm olmaz). Sadeleşti. | Takvim `t % 2 == 0 && t >= 2 && t < MaxTurns - 1`. `EvState.P` ağız, `Area` 3 karelik döküm hattıdır. Yığın hattın ilk yığınsız karesine `NewObj("cevher", 2)` (`Baskin.cs:351`) ile konur. `ExitSafe` ya da `TeamSafe` (`Baskin.Gear.cs:487`) tutmazsa geri alınır; bu, `EndCracks`'in enkaz kalıbıdır. Karede birim varsa yalnız 1 `EventHurt` verilir. `StopsShot` tel dışındaki her nesnede zaten true (`Baskin.Gear.cs:46`); havanın lobu etkilenmez. Yığını `HurtObj`'un genel yolu kırar. | M | yeni | "Takvimle siper doğurmak" yeni bir iştir, ama sonucu Mühürleyici'nin kalasına ve tavan enkazına yakındır. Havan lob attığı için yığın onu durdurmaz, düz atanları keser. Yığınların dönüş tahtasına taşınıp taşınmayacağı karar ister. |
| EMBER | Lastik mezarlığı (Kuveyt, Sulaibiya) | **Lastik:** lastik yığınına itilen birim hasar almaz, bir kare geri seker. | İki sınav | `Push`'un nesne dalına tek koşul eklenir: hedef `lastik` ise `Hurt` ve `HurtObj` yok; birim ters yönde bir kez itilir (zincirlemesin diye bayrak). Lastik `EventObjectHit` ile hasar almaz. | S | yeni | EMBER'de yalnız ekip ittiği için seyrek tetiklenir. Patlayan robotun canı 1'dir: duvara itilen ölür, lastiğe itilen ekibe geri döner. |
| SOLACE | Elektrik santrali | **Ark:** jeneratör 3., 6. ve 9. tur sonunda boşalır; başka bir birime bitişik duran herkes 1 hasar alır. | İki sınav | `EventEnd`'e tek adım. Liste adımın başında alınır; `EventHurt`. | S | B.5 | Yer sıradan bir bina. Uzman kendi taretinin yanında da çarpılır. Eşleşme güçlü: kalkan dronu bitişik durmak zorundadır. |
| EDDY | Sel basmış çarşı (aynı yer) | **Diz boyu su:** diz boyu suda yürüyüşe başlayan birim en çok iki kare yürür. | İki sınav | `Reachable` ve `Walk`'ta başlangıç karesi su bayraklıysa `move = Math.Min(move, 2)`. Kırağı ile çakışmaz. Koşu darbesinin menzili için karar gerekir. | S | yeni | Girdap EDDY'nin bugünkü olayının kendisi ve en ucuzu. Çoğaltıcı (yürüyüş 2) sudan etkilenmez, dronları 3'ten 2'ye iner. Su basmış bölmeyle aynı görüntüyü (su) paylaşır, ama işi ayrıdır: biri yürüyüşü, öbürü eylemi keser. |
| QUORUM | Denizaltı (aynı yer) | **Sonar pingi:** 3., 6. ve 9. tur sonunda sonar başlığına en yakın birim 1 hasar alır. | **Koşullu.** Birinci sınavı geçti. İkincide kaldı: ping yer bulur, vurmaz; "eşitse hepsi" parantezi ikinci bir hükümdü. Sadeleşti. | `EventEnd`'e yeni kol eklenir, takvim `t % 3 == 0 && t < MaxTurns`. Başlık `ev.P`'dir ve olay nesnesidir: `EventObjectHit` (`Baskin.Events.cs:323`) darbeyi karşılar, `IsEventObject`'e (`Baskin.Chain.cs:15`) eklenir. Uzaklık Manhattan'dır, duvar sayılmaz. `OnBoard` birimlerin en küçük uzaklığındaki herkes `EventHurt` alır. Bu bir istisna değildir; `Hazards` vurulacakları canlı işaretler. `Upcoming` "PİNG" der. Başlık girişten ve hedeften en az 3 yürüyüş uzakta, geçit ve menfez komşusu olmayan bir duvar dibine konur. | S | yeni | Kurgu telsizle taşınmalı: YZ aktarıcıyı silaha çevirmiş. Kilitleyici ping'den bir tur önce başlığa en yakın uzmanı kilitler; yem (sonar şaşırtma yemi) ekibin karşı hamlesidir. Makine planı oyuncudan önce yapıldığı için akıllı makinenin "en yakın olma" tahmini yaklaşıktır. |
| BOREALIS | Sibirya permafrost veri merkezi (aynı yer; kutup gecesinde YZ salonların ışığını söndürmüş, yalnız acil durum lambaları yanar) | **Kutup gecesi:** üç kareden uzağa nişan alınmaz. | **Koşullu.** Birinci sınavı geçti. İkincide kaldı: VERITY de bir veri merkezidir ve işi nişanı kesmektir; iki veri merkezinde iki nişan kısıtı tekrar sayılabilir. Kutup gecesi binanın dışındadır; salonun karanlığı ikinci bir cümle ister ("YZ ışıkları söndürdü"). | Yeni kimlik `gece`. Takvim, olay adımı ve kare bayrağı yoktur, Katar gibi hep açıktır: `EventEnd`'e kol eklenmez, `EventStepTurns` boş döner. Yardımcı `Dark(B, k) => Is(B, "gece") && k > NightRange` yazılır; `NightRange` dengeden gelir (`facilityNightRange = 3`, kural 7). Bulutun nişan kümesine menzil denetimi eklenir, yani `InCloud`'un çağrıldığı her yere: `Targets` (`Baskin.cs:844–866`), `HookTrace` (:922), `IntentTarget` (:1168–1193), `Zone` (:1326), `Covers` (:1353), `ThrowTargets`, `AmbushTargets`, `AmbushLine` ve `FireAmbush` (`Baskin.Actions.cs:430–517`), köpek komutu (`Baskin.Gear.cs:230`), `RunTurret` (:647), `Baskin.Counters.cs:128`, `Baskin.Infiltrators.cs:120`. Atış ve kilit döngüsü zaten `k > range` iken `None` döner (`Baskin.cs:1171`), orada `range = Math.Min(range, NightRange)` yeter. `ScoreTarget` sınırı `IntentTarget` üzerinden kendiliğinden alır. Kırağı'nın buzu (`SignatureTarget`, yalnız `i == FrostRange`, 3) ve havanın lobu (`LobRange` 3) etkilenmez. Vinç (`RunWinch`, `WinchRange` 4) kümede yoktur, ayrıca karar ister. Hasar, RNG, geometri ve `ExitSafe` yoktur. `Hazards` her birimin çevresine 3 karelik ışık halkası çizer. | M | B.5'in sık ormanı (HEADWATER kerestede, boşta) | Soğuk koridor reddedilirse yeri Sibirya'da tutarak alınacak aday budur. Kırağı odanın en uzaktan etki eden makinesi olur. Dronlar yaklaşmak zorunda kalır; ekip EMP (2–5 → 2–3), kanca (5 → 3), taret ve pusu (4 → 3) menzilini kaybeder. Ekranda olay olmaz; ışık halkası çizilmezse hissedilmez. Dron etkisi düşer, bot ölçümü şart. |
| PARAGON | Tersane | **Kızak:** gemi 3. tur sonunda kızaktan suya iner; gövdenin durduğu sıra açılır. | İki sınav | Gövde kareleri `Cell.Shutter` taşır: duvar başlar, 3. turda zemin olur. Bayrak dönüşe taşınır, dinamit yıkamaz. Yol yalnız açılır. Çok kareli yerleşim yenidir (`FacilityMaps`). | M | B.5 | Tek seferliktir; robot kol bugünkü olaydır. Limanla aynı aileden değildir: liman taşır ve geri getirir, kızak bir kez açar. |

**On üç yer, on üç iş** (önerilerle)

| Yer | İş |
|---|---|
| VERITY · Banka veri merkezi | Nişanı kesmek |
| WAYPOINT · Sevkiyat katarı | Kenardan düşürmek |
| HALCYON · Yüzer platform | Herkesi takvimle bir kare kaydırmak |
| TESSEL · Liman | Duvar taşımak (takvimle yol açıp kapamak); iniş yerindekini ezip itmek |
| HEADWATER · Kereste | Siperi ateşle yok etmek |
| CAIRN · Döküm oluğu | Kalıcı hasar alanı (bina içinde, ince hendek) |
| EMBER · Varil | Vurulan nesnenin alan hasarı |
| SOLACE · Asansör | İki nokta arasında taşımak |
| EDDY · Girdap | Tek noktaya çekmek |
| QUORUM · Su basmış bölme | Bir bölgede eylemi kesmek |
| BOREALIS · Soğuk koridor | Bir bölgede gelen hasarı büyütmek |
| PARAGON · Robot kol | Dönen, tek karelik vuruş |
| MARS · Ağırlıksızlık | İtmeyi engele kadar uzatmak |

- Yakın duranlar. Kereste ile varil aynı fitil kalıbını kullanır, ama sonuçları ayrıdır. Ağırlıksızlık ile Yalpa'nın sonucu kaymadır, ama tetikleri ayrıdır. Asansör ile girdap birimi yerinden alır: biri ışınlar, öbürü çeker ve çarpıştırır. Su basmış bölme ile VERITY'nin bulutu bir bölgede bir şeyi yasaklar: bulut nişanı keser ve içerdekini korur, su içerdekinin eylemini keser ve onu açıkta bırakır. Soğuk koridor ile döküm oluğu kalıcı bölgelerdir: oluk orada durana kendiliğinden vurur, koridor tek başına zarar vermez, yalnız gelen darbeyi büyütür. Bunlar tekrar sayılmadı.
- Soğuk koridorun işi yenidir: B.9'un öbür on iki işinde, yedeklerde ve elenenlerde gelen hasarı büyüten bir bölge yok.
- Döküm oluğu B.5'in lavıyla aynı iştir. Yalnız yer bina içine taşındı ve geometri inceldi.
- Anten sürümünde çanak "takvimle kayan hasar alanı" işini yapar. Bu iş döküm oluğundan (sabit) ve robot koldan (tek kare) ayrıdır.
- Birlikte seçilemeyenler. Su basmış bölme alındığı için eylem kesen öbür adaylar (hurda mıknatısı, soğuk depo) açılmaz. Döküm oluğu alındığı için takvimli hasar alanları (koridor çevrimi, su bombası, torpido kovanı) açılmaz. Soğuk koridor alındığı için aynı işi yapan gevrek çelik ve ayaz açılmaz. Hasarı emen ya da iyileştiren bölgeler (buzlanma, kar yığını, ısınma) aynı yerde onun tersidir.
- Yedeklerin hiçbiri önerilen işlerle birebir çakışmaz. Yakın olanlar: mayın döküm oluğuna, konveyörün yığını Mühürleyici'nin kalasına, diz boyu su su basmış bölmenin görüntüsüne, kutup gecesi VERITY'nin bulutuna yakındır (yukarıda).

**Önerilen uygulama sırası** (ucuzdan pahalıya)

1. **PARAGON robot kol, EDDY girdap.** Bugünkü olayın kendisidir: bir madde kalkar ya da takvim değişir. EDDY'de ayrıca bina, kanat ve not metni değişir.
2. **SOLACE asansör, EMBER varil.** Boştaki ya da komşu olayın kodu kullanılır (asansör, tüpün fitili).
3. **CAIRN döküm oluğu, QUORUM su basmış bölme, BOREALIS soğuk koridor.** Kural tek satırdır, ama oda planı oluk, havuz ya da şerit ister; tesis izi artar. Su basmış bölmede makine, ekip ve aygıt için üç eylem kapısı gerekir. Soğuk koridorda `Hurt`'teki satır ve tavanı testle kilitlenmeli: aynı turda ikinci darbe fazla almaz, plaka fazlayı emer.
4. **HEADWATER kereste.** `Fire` kancasının EMP ayrımı gerekir.
5. **TESSEL liman.** Blok taşıma, itme denetimi, `ExitSafe`, dinamit dalı, hurda düzeltmesi ve yeni yerleşim gerekir.
6. **Mars.** Şartın kendisi küçüktür (S). Final tesisi, yeni YZ, imza makinesi ve son şalterin akışı ayrı bir iştir.

Her aşama Katar ve Yalpa'nın kalıbını izler:

- alt bayrak ve yeni olay kimliği; `EventStepTurns` (`Baskin.Rooms.cs:608`) ve `RaidEventTests` kaydı;
- tesis izi (`FacilityMaps.GeneratorVersion`) ve kayıt sürümü (`GameRecord.CurrentVersion`) artışı, çünkü şart oyunu değiştirir; `LiberationCampaignTests`'in kayıt testleri güncellenir;
- şart kartı, rehber satırı ve `Loc` çevirisi;
- PlayMode'da tesisin oynanması ve bot ölçümü (`TESIS-BASKINLARI-TASARIMI.md` §11.6).

**Değişmeyenler**

- **VERITY** (söndürme bulutu), **WAYPOINT** (Katar) ve **HALCYON** (Yalpa) aynen kalır.
- Eski olayların akıbeti:

| Eski olay | Akıbeti |
|---|---|
| `kaynak` | Robot kolda yaşar |
| `suzgec` | Girdapta yaşar |
| `asansor` | Gökdelende yaşar |
| `tup` (fitil) | Varile taşınır; uçuş hattı (`TubeLine`) yalnız Mars yayını seçilirse yaşar; HEADWATER'dan çıkar |
| `faz` (`EndPhases`) | Döküm oluğunda yaşar |
| `raf` | Limanın istifinde yaşar (`SetupShelf`, `ShelfSafe`, `ExitSafe` kalıbı); kimliği ve takvimi dokunulmadan kalır, QUORUM'dan çıkar |
| `gaz` (`Fire` kancası) | Kerestede yaşar |
| `yag` (`Slide` fikri) | Ağırlıksızlığın `Push` döngüsünde yaşar |
| `turnike` (`CanStep`) | Tesis kipinden düşer; yürüyen merdiven yedekten çıktı, Moskova metrosu yeniden istenirse onda yaşar |
| `kriyo`, `tavan` | Tesis kipinden düşer |

**Elenen dikkat çekici adaylar**

| Aday | Neden |
|---|---|
| Liman · İstif, Varyant A ("itilemeyene dokunmaz") | Kod sınavını geçti. Geliştirici hasarı doluluğa bağladı; A ise hasarı itmenin başarısına bağlar. Duvara yaslı iniş yerinde ekip vinci sıfır bedelle kilitler; geliştiricinin değiştirmek istediği davranış buydu. Akışkan "C" varyantı (önce ileri, sonra sağ, sonra sol) da önerilmez: ok tahmini zorlaşır, cümle uzar. |
| Yanardağ krateri · Lav (B.5) | Geliştirici: "lav saçma, binanın içinde olsun." İş döküm oluğunda yaşıyor. |
| Ferrokrom izabe · Cüruf kabuğu (CAIRN) | Kod sınavını geçti (dinamit dalına ek gerekir). İki cümle ve iki aşamalı durum (kabuk, fitil, açık cüruf); B.1'i deler. Lav kareleri birikir, denge havanın lehine bozulur. |
| Hurda çelik fabrikası · Mıknatıs (CAIRN) | Mıknatısta beklenen ilk şey çekmek ya da yapıştırmaktır, "yürür ama eylem yapamaz" değil. Eylem kesen ailedendir; QUORUM suyu aldı. Beyan edilen motorda iki hata vardı (`Acted` yürüyüşü de keser; boş `kinds` planı çökertir). |
| Cevher hangarı · Vagonet (CAIRN) | Yeni değil: B.2'nin maden ocağı adayı, varil ile aynı "vurulan nesne" ailesi. "Hasar verir ve iter" sonucu yeni limana da yaklaşıyor. |
| Kobalt açık ocağı · Dozer (önceki CAIRN yedeği) | Dışarıdadır. "Önündekini iter, itemezse bekler" artık limanın vincinin işidir. |
| Denizaltı · Tek sıra (B.5) | Geliştirici başka kural istedi. Ekranda görünmez; ancak plan tek karelik koridor koyarsa hissedilir. |
| Sibirya · Tipi (önceki BOREALIS önerisi) ve Karpat · Tipi (önceki QUORUM yedeği) | Geliştirici başka kural istedi. Tipi listeden çıkar; aygıt kararı da onunla kalkar. |
| Sibirya · Egzoz fanı (önceki taslağın BOREALIS önerisi, koşullu) | Kod sınavını geçti. Sabit hatlarda takvimle bir kare itmek Yalpa'nın kaymasının yerel hâlidir; bant, paternoster ve karusel bu yüzden elenmişti. Liman gibi çift turda da iter. Soğuk koridor iki sınavı geçince düştü. Uçuş hattı (`TubeLine`) yalnız Mars yayınında yaşar. |
| Moskova metrosu · Yürüyen merdiven (önceki BOREALIS yedeği) | İki sınavı geçti. Yer değişir; geliştirici Sibirya'yı tuttuğu için yedek aynı yerdeki kutup gecesine geçti. Yer yeniden açılırsa ilk aday budur (turnikenin `CanStep` altyapısı, yönlü BFS, M). |
| Denizaltı · Sessiz koşu | Kod sınavını geçti. Tipinin aynasıdır (tipi durmayı, bu kıpırdamayı cezalandırır). Sessiz turdan önce atılan kilit uzmanı yürütmez, yani onu korur: Kilitleyici kurala karşı çalışır. |
| Denizaltı · Su bombası, Torpido kovanı | Kod sınavını geçti. Takvimli alan ya da hat hasarıdır; metro ve roket rampası bu yüzden elenmişti, döküm oluğuyla çakışır. Su bombasının sıraları Katar'ın düşme kenarıyla aynıdır; torpidonun odanın içine ateşlenmesi beklenen şey değildir. |
| Veri merkezi · Soğuk depo | Kod sınavını düzeltmeyle geçti (makine kapısı `PlanEnemies`'te değil, `IntentTarget`'ın başında olmalı). Su basmış bölmeyle aynı iştir; eksi kırk derecelik bant arşivi tanınır bir görüntü değildir. |
| Veri merkezi · Çöküntü | İki sınavda da kaldı. `BarrierFits` (`Baskin.Gear.cs:496`) `ExitSafe`'e bakmaz, cep çıkıştan kopabilir; kural `Push`'un her yoluna dokunur ve beyan edilen M'den pahalıdır. "Dolu kare çökmez" bir istisnadır; yalnız ekip iter. |
| Veri merkezi · Koridor çevrimi | Kod sınavını geçti. Takvimle el değiştiren hasar alanıdır; kendi koşuluna göre CAIRN hasar alanı alınca düşer. Sıcak koridorun can yakması da beklenen şey değil. |
| Veri merkezi · Metan çatlağı | Kod sınavını geçti. B.6'daki petrol kuyusunun aynısıdır (girdabın tersi, aynı takvim). Yamal kraterleri dışarıdadır, veri merkezinde tanınmaz. |
| Veri merkezi · Buz duvar | Kod sınavını geçti. Madenci'nin dinamitinin bedava sürümüdür, yalnız ekibe yarar; kırağı ile ilişkisi yok. |
| Veri merkezi · Gevrek çelik | İki sınavı geçti. Soğuk koridorla aynı kural ve aynı kod; yer ayrıntısı (panjur önündeki çelik ızgara) daha niş. "Kırağılı ızgara" sözü Kırağı'nın buzuyla karışıyordu. |
| Veri merkezi · Ayaz | Soğuk koridorun tavansız ilk hâli. Fazla hasar `B.Ev.Hurt`'e yazılmadığı için "evrede en çok 1 olay hasarı" kuralını deliyordu. |
| Veri merkezi · Buz sarkıtı, Kablo tavası | Kod sınavını geçti. Varilin tek karelik hâli ve tesisten düşen tavanın kendisidir (fitil ailesi). Sarkıt EMP'yi de sayınca ikinci cümle ister; tavanın iki sonucu (hasar ya da yığın) vardır. |
| Veri merkezi · Isınma, Buzlanma, Kar yığını, Yedekleme | Kod sınavını geçti. İyileştirme ya da hasar emme işidir; iyileştirme kaplıcada (#26) ve şifalı su yedeğinde yaşıyor. Buzlanma ve kar yığını B.5'te elenen buz kabuğunun dönüşüdür. Yedekleme Sibirya'ya özgü değildir. |
| Veri merkezi · Buhar vanası | Kod sınavını geçti. Vagonet ve egzoz fanının gerekçesiyle düştü: varil ailesinden vurulan nesne ve hat boyunca itme. Teplotrassa veri merkezinde beklenen şey değil. |
| Veri merkezi · Derin kar | Kod sınavını geçti. EDDY yedeği diz boyu suyun kelimesi kelimesine aynısıdır ve kırağının yürüyüş kesme işini yineler (kemer ışığının gerekçesi). |
| Veri merkezi · Kar savruntusu, Yumuşak kar | Kod sınavını geçti. Biri konveyörün takvimle siper doğurması, öbürü lastiğin çarpma yumuşatmasıdır. |
| Veri merkezi · Sıcak koridor, Sıcak dolap | Kod sınavını geçti. Yalnız Kırağı'nın buzunu söndürür: tarafa bakar, Kırağısız odada uyur. |
| Veri merkezi · Donmuş metal, Tekerlekli kabin, Donmuş ızgara, Statik, Sunucu kafesi | Kod sınavını geçti. Donmuş metal bir iş listesi ister; kabin gizli dallar taşır; ızgara cıvatanın bölgesel kopyasıdır; statik arkın ve varilin ailesindendir; kafes ekibin teliyle aynı davranıştır. |
| Veri merkezi · Isı kapısı, Kalkık döşeme | İki sınavda da kaldı. Biri `Cell.T`'yi birim konumundan sürekli yeniden yazar, öbürü yürünmeyen yeni bir kare türü ister; gerçek boy L. İkisi de iki aşamalı durum taşır (yangın kapısı ve asansör yolculuğunun emsali). |
| Mumbai garı · Kalabalık (#8; ayrıca #3, #34, #64) | İki sınavı geçti, ama SOLACE asansörü aldı. Ekranda görünmez; tek sıra da aynı kusur yüzünden düştü. |
| Yolcu uçağı · Kemer ışığı (#69) | İki sınavı geçti. BOREALIS'te kırağının yürüyüş kesme işini yineler. Havadaki bir yer yeniden istenirse ilk aday budur (takvim 3 ve 6; 9. tur son evrede yürüyüşü keser). |
| Robotlu depo · Taşıyıcı robot (#0; ayrıca #15, #31, #38, #73) | İki sınavı geçti. TESSEL'in yeri liman olduğu için açıkta kaldı; aynı aileden dozer de düştü, çünkü limanın vinci artık iter. |
| Japon kaplıcası (#26) | İki sınavı geçti; iyileştirme işi HEADWATER yedeğinde (#53) yaşıyor. |
| Metro · Tren (#66) | Oyunda zaten tren (Katar) var. Takvimli hat hasarı döküm oluğunun (eski lav) işine yakın. |
| Temiz oda hava duşu (#1), Amazon akıntısı (#29) | Tek yön işini yürüyen merdiven daha tanınır biçimde yapıyor. |
| Bant (#4), Yüzen pazar (#11), Paternoster (#12), Bagaj karuseli (#36) | Yalpa'nın kaymasının yerel ya da her turluk hâli; istisna listesi ister. |
| Gayzer (#28), Yangın kapısı (#59), Siper (#67) | İki aşamalı durum ya da gizli sayaç; B.1'i deler. |
| Sorgu odası (#13), Donmuş göl kayışı (#42) | L boy. Nişan hattı ya da yönlü BFS her yere dokunuyor. |
| Uzayda nesne üstünden süzülme (#22, #47, #75) | B.5'te elenen favela atlaması. Yürüyüş ile itme arasında tutarsızlık bırakır. |
| Mars'tan gelen emir (#24) | Yalnız makinelere işler, yani tarafa bakar; L boy. |
| Hava kilidi (#19) | Final için tek başına zayıf; kapıdaki uzman hedef alınamaz. |
| Kum fırtınası (#33), güneş santrali gölgesi (#30), radyasyon sığınağı kopyaları (#20, #74) | "Güvenli kareye koş" işinin tek temsilcisi Mars fırtınasıdır (#45). |
| Duga radarı (#40), hep canlı anten ışını (#23) | Çanak (#48) aynı işi daha tanınır yerde ya da daha özgün yapıyor. |

**Karar bekleyenler**

1. **Mars'ın kurgusu.** İstasyon mu, anten mi? İstasyonsa yörüngede mi (ağırlıksızlık), yüzeyde mi (düşük yerçekimi)?
2. **Son YZ'nin imza makinesi.** İten bir makine olursa ağırlıksızlık iki yana da işler.
3. **Ağırlıksızlık ve Yalpa.** Ağırlıksızlık Yalpa'nın tekrarı sayılır mı? Sayılırsa Mars güneş fırtınasını alır.
4. **BOREALIS.** Soğuk koridor kabul mü? "Çelik soğukta kırılır" soğuğun ikinci akla gelen sonucudur; kart bunu "eksi kırkta çelik cam gibi kırılır" diye anlatır. Kabulse yer ayrıntısı koridor mu kalsın, panjur önündeki ızgara (gevrek çelik) mı olsun? Reddedilirse yer Sibirya'da kalıp kutup gecesi (koşullu, VERITY'ye yakın) mi alınsın, yoksa yer Moskova metrosuna geçip yürüyen merdiven mi seçilsin? Kutup gecesi alınırsa vinç (`WinchRange` 4) de gecede kısılır mı?
5. **Kereste ve EMP.** EMP kereste yığınını tutuşturur mu?
6. **Su basmış bölmede pusu ve köpek.** Suya kurulmuş pusu ateşlenir mi (öneri: evet, eylem kurulurken yapıldı)? Köpek komutu eylem sayılır mı (öneri: evet)? İkisi de kural metnine istisna olarak girmez.
7. **Tesis adları.** TESSEL (Konteyner limanı), HEADWATER (Kereste fabrikası), CAIRN (Bakır izabe tesisi), SOLACE (Gökdelen), EDDY (Sel basmış çarşı) ve QUORUM (Denizaltı) için tesis adı değişir; BOREALIS'in adı ("Kutup veri merkezi") kalabilir. Aşama 12'deki K7 gibi adlar bayraktan bağımsız mı değişsin?
