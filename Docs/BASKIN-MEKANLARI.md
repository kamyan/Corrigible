# Baskın mekânları

_2026-11-02. İstek: "baskınların geçtiği yer ve aksiyonlar değişiyor ama baskın değişmiyor, yine sunucu odasında geçiyor
gibi, orada nasıl farklılaştırabiliriz". Bu belge bir öneridir. **Uygulandı (2026-11-02, "Ad ve sandıkla başla"):**
Katman 1 (tek yer adı), Katman 2 (ikmal sandığı) ve Katman 9A (görev çipi ve ipucu); kararlar önerilenler (1a, 4a, 5a).
Ayrıntı ve sapmalar en sonda, "Uygulama" başlığında. Öneriler dört açıdan
toplandı: maket, pafta, ses ve oyun kuralı. Toplam 30 öneri çıktı ve iki yargıç (oyuncu ve mühendis gözüyle) bunları
puanladı. Aşağıda yinelenenler birleştirildi, yargıç notları da işlendi._

Bir baskından ötekine şunlar değişiyor: harita adı, harita türü, bölge özelliği, imza makinesi ve bölge olayı. Buna
karşın tahtanın görünüşü, sesi ve adı hep aynı kalıyor. Öneri tek cümleyle şu: **masa komutanın, maket binanın.**
Komutanın masası olduğu gibi kalır. Masaya konan maket ise baskının geçtiği binaya göre değişir.

## Sorun

**Bugün değişenler**
- **Harita:** 6 tür × 5 ad = 30 harita (`Core/RaidMaps.cs:22-35`). Her kampanya kendi havuzunu kendi tohumundan kurar.
  Haritadan haritaya yalnız duvar, kanal ve nesne karelerinin yeri değişir. Tür de ad da hiçbir kuralı etkilemez.
- **Bölge:** özellik, imza makinesi ve olay (`GameData.cs:40-51`, `Baskin.Events.cs:20-35`). Tahtada bunlardan yalnız
  olay katmanının renkli işaretleri görünür (`RaidBoard3D.cs:604-749`).

**Değişmeyenler**
- **Çizim:** 64 karenin tamamı tek bir Floor, Wall ve Cable prefab'ından kurulur. Seçimi yalnız hücre türü belirler
  (`RaidBoard3D.cs:1981-1992`). `Game/Raid/*.cs` bölgeyi, harita adını, özelliği ve görev türünü hiç okumaz. Keçe,
  ozalit, lamba, gri-bej mukavva bloklar ve krem sivil sunucular her baskında aynıdır.
- **Hedef:** yağma görevlerinde de hedef, üstünde kırmızı kare olan sunucu düğüm dolabıdır (`Baskin.cs:336`). Yağma 36
  kampanya baskınının 24'ünü oluşturuyor. "Yine sunucu odası" duygusunun en güçlü kaynağı bu dolap. Üstelik kırmızı
  burada yanlış: kırmızı tehdit demek, oysa bu sandığı kırmak görevi kaybettirir.
- **Sektör adları** her bölgede aynı: Tedarik deposu, Tahliye geçidi, Sunucu merkezi (`CampaignLiberation.cs:14`).
- **Ortam sesi:** her baskında tek döngü çalar (`LoopRaidAmbience`, `AudioDirector.cs:161-162`). Bu döngü 60/120 Hz bir
  uğultu (enerjisinin %80'i 300 Hz'in altında); telsiz bandından (450–3000 Hz) geçerken ~9 dB kaybediyor ve hışırtıya
  karışıp kayboluyor.
- **Olay sesleri:** bina olayları ekibin kendi seslerini kullanıyor. Bulut duman bombası gibi, raf ve asansör EMP
  itmesi gibi, kaynak EMP patlaması gibi duyuluyor.
- **Olay nesneleri:** üçünün kendi modeli yok (`RaidPieceLibrary.cs:125-143`, DURUM.md:945):
  - tüp, uzatılmış bir soğutma ünitesi;
  - kaynak kolu kaidesi, basıklaştırılmış bir soğutma ünitesi;
  - tavan enkazı, büyütülmüş bir dolap enkazı.
- **Kablo kanalı:** her zaman Z ekseni boyunca kurulur (`RaidBoard3D.cs:1986`, `Quaternion.identity`). Oysa koşuların
  yarısı yatay çiziliyor (`RaidMaps.cs:286`). Bu yüzden yatay bir koşu, yan yana dizilmiş dik parçalar gibi görünüyor.

**İki yer adı var, ama ikisi de görünmüyor**
- Harita adı (`B.Layout`, örneğin "Metro peronu") ile olayın binası (`RegionEvents[..].Room`, örneğin "Banka sunucu
  salonu") birbirinden bağımsız iki yerdir. Herhangi bir harita herhangi bir binada çıkabildiği için çoğu zaman
  çelişirler.
- Kampanyada ikisi de ekrana gelmez. `Baskin.RoomName` (`Baskin.Events.cs:1362`) yalnız temel `ResultPlace`'te
  kullanılıyor. Kampanyada bunu da `LiveRaidDataSource.cs:32` "Bölge · NN Sektör" ile eziyor. DURUM.md'deki "sonuç kartında
  oda adı olayın binası" cümlesi bu yüzden yanlıştı (düzeltildi).

**Bugünkü altı baskın** (aynı Baskin sahnesinde altı bölgenin olaylı baskını yüklenip çekildi): weu · Metro peronu, ru · Fiber santrali, na · Montaj hattı, in · Uydu
bağlantı odası, oc · Batarya odası, sa · Ağ geçidi. Altısında da aynı şeyler var: aynı yeşil keçe, aynı ozalit kâğıt,
aynı gri-bej blok yığınları, kırmızı kareli aynı koyu dolap, aynı krem sivil kutular ve soğutma silindirleri, aynı
harfli piyonlar. Fark yalnız düzende ve birkaç küçük olay işaretinde ("BULUT 1", "HORTUM 1", "TÜP 1"). Ekrandaki adlar
kapatılırsa hangi baskının hangi binada geçtiği anlaşılmıyor.

## İlkeler

- **Masa komutanın, maket binanın.** Şunlar komutanın masasıdır ve hiç değişmez: keçe, kontrplak taban, ozalit kâğıt,
  masa lambası, kenardaki A–H / 1–8 kare adları, kalem işaretleri ve kamera. Değişen, masaya konan maket parçalarıdır:
  duvar blokları, kanal içleri, olay nesneleri, yağma hedefi ve ileride sivil ekipman. Gözcü binayı telsizden bildirir,
  komutan onu maketle kurar.
- **Taşlar sabit, arazi binaya göre.** Oyuncunun öğrendiği parçalar hiçbir binada değişmez: çekirdek ve düğüm dolabı,
  kopya dolabı, uyanma iğnesi, soğutma ünitesi, bütün makineler, harfli piyonlar, asetat ve kalem işaretleri ve olay
  katmanının renkleri.
- **Malzeme dili korunur.** Her parçanın ayırt edici işareti aynı kalır:
  - Duvar: boyasız, can etiketi yok, üstü düz. Geçilmez demek.
  - Kanal: koyu oluk, turuncu-siyah kenar şeridi ve görünür bakır. Üstüne itilen hasar alır demek.
  - Sivil: krem gövde, mavi bant, üstte mavi halka ve çarpı.
- **Ayrılmış işaretler.** Aşağıdakiler yalnız karşılarındaki anlam için kullanılır; hiçbir bina bunları süs olarak
  kullanamaz:

  | İşaret | Yalnız şu anlamda |
  |---|---|
  | kırmızı | YZ |
  | mavi halka ve çarpı | sivil |
  | dik silindir + fan + sarı üçgen | soğutma ünitesi |
  | turuncu omuz bandı | basınçlı tüp |
  | el çarkı | kayar raf |
  | harfli ahşap | ekip |
  | kâğıtta harf ve rakam | kare adları |
  | yeşil | iyi |
  | sarı çift çerçeve | eğitim |

- **Sanat kuralları.**
  - Neon, hologram ve bilim kurgu görünümü yok. Özel shader yok.
  - URP Lit mat malzeme: smoothness ≤0,15 (su için 0,35). Emission ve bloom yok.
  - Kamera kilitli: 35°/45° ortografik, 132,58 px/birim.
  - İnce parçalar ekranda ≥3 px olmalı, yoksa TAA altında titrer. 6 cm'den küçük parça gölge atmaz.
  - Yazı ≥14 px. Hiçbir durum yalnız renkle verilmez.
- **Motor kuralları.**
  - Önerilerin tamamı sunum katmanında. RNG, komutlar, `GameRecord.CurrentVersion`, harita havuzu ve `RaidEngineTests`
    değişmez.
  - Tek Core değişikliği metindir (Katman 1) ve açıkça işaretlendi.
  - `RaidBoard3D` bugünkü gibi `Params` okumaz. Bina kiti ve görev türü panoya tek noktadan, köprüden verilir
    (`RaidDataSource.OpenState → board.SetDress`).
  - Renkler `raid_tokens.json → RaidTheme`'de, sesler `SoundBank.asset`'te durur.
  - Prefab ve sahne değişiklikleri tek seferlik editör script'iyle yapılır. Her bileşen kendi dosyasındadır. Bütün
    metinler `Loc` ile çevrilir (TR + EN).
- **Eğitim ve örnek baskın bugünkü gibi kalır.** `MapSeed == 0` olan baskınlar varsayılan kitle, yani bugünkü
  görünümle çizilir: eğitim (tohum 129) ve tek başına açılan örnek baskın. Eğitimde yalnız iki şey değişir: yağma hedefi
  sandık olur ve sektörün adı "01 Teknik depo" olur.
- **Telsiz bugünkü ölçüsünde kalır.** Yeni telsiz satırı, şerit ya da konuşma eklenmez (BRIEF_rev2.md:39). Yeni ses
  yatakları bugünkü uğultuyla aynı seviyede çalar.

## Yer kimliği

Önerilen model: **bina = bölge, oda = sektör, harita türü = gizli düzen.**
- **Bina.** Her bölgenin tek bir binası olur. Olay, özellik ve imza makinesi zaten o binaya ait (BOLGE-OLAYLARI.md:48-61).
- **Oda.** Üç sektör, binanın içine doğru ilerleyen üç odadır: 01 ve 02 yağma, 03 sunucu ya da kumanda odası. Aynı
  sektöre yeniden girildiğinde adı değişmez. Olay ekipmanı üç odada da bulunur, çünkü olay her sektörde işler. Örnekler:
  CO₂ memeleri her teknik odada var, yük asansörü her katta durur, kriyo hattı bütün bloklardan geçer.
- **Düzen.** Harita türü ve 30 harita adı oyuncuya gösterilmez. Düzen "YZ'nin bugünkü barikatları"dır: gözcünün
  raporuna göre masaya dizilen istifler. Harita adları bot ve testler için iç etiket olarak kalır.
- **Neden harita adı değil?** Üç sebep var:
  - 30 adın çoğu binayla çelişir: bankada "Metro peronu", arşivde "Otopark katı".
  - Aynı sektöre yeniden girildiğinde ad değişirdi.
  - Türe göre oda adı vermek 72 ad gerektirirdi.

| Bölge | Bina | 01 · yağma | 02 · yağma | 03 · sunucu |
|---|---|---|---|---|
| Batı Avrupa | Banka veri merkezi | Teknik depo | UPS odası | Sunucu salonu |
| Doğu Avrupa | Devlet arşivi | Mikrofilm deposu | Dosya katı | Tarama odası |
| Orta Doğu | Körfez rafinerisi | Malzeme ambarı | Pompa holü | Kompresör dairesi |
| Afrika | Cevher hangarı | Konveyör hattı | Kırıcı bölümü | Kumanda kabini |
| Kuzey Amerika | Dağıtım merkezi | Sevkiyat rampası | Ayıklama katı | Sunucu asma katı |
| Rusya | Sibirya veri merkezi | Soğuk depo | Azot galerisi | Sunucu bloğu |
| Hindistan | Çağrı merkezi | Personel kantini | Kasa holü | Santral odası |
| Çin | Montaj fabrikası | Parça ambarı | Kaynak hattı | Hat kontrol odası |
| Japonya-Kore | Çip fabrikası | Malzeme kilidi | Temiz oda koridoru | Trafo merkezi |
| Güneydoğu Asya | Elektronik fabrikası | Bodrum ambarı | Sintine kanalı | Kopya atölyesi |
| Güney Amerika | Hidroelektrik santrali | Tüp deposu | Türbin galerisi | Kumanda odası |
| Okyanusya | Maden limanı | Kumanya deposu | Bant galerisi | Makine dairesi |

**Oyuncu adı nerede görür** (Batı Avrupa, 02 örneğiyle):
- **Fiş:** "BATI AVRUPA" başlığının altında "Banka veri merkezi". Satırlar: "01 Teknik depo · YAĞMA", "02 UPS odası ·
  YAĞMA", "03 Sunucu salonu · SUNUCU YIKIMI".
- **Teleks:** "Baskın emri: Batı Avrupa · UPS odası · −12 fon".
- **CRT:** "ALFA · BATI AVRUPA / 02 UPS ODASI" ve "SAHA MASASI · BANKA VERİ MERKEZİ · GÖREV: YAĞMA".
- **Kayıt paneli:** "TELSİZ KAYDI · Ekip UPS odası girişinde." Bugünkü "Ekip harekete hazır." satırının yerine geçer;
  yeni bir telsiz satırı eklenmez.
- **Sonuç ve raporlar:** sonuç kartında "Batı Avrupa · 02 UPS odası", raporda "HEDEF: Batı Avrupa · UPS odası", son
  rapor anında "Batı Avrupa: UPS odası geri alındı."

**Harita türü adından değil, düzeninden okunur.** Bunu yapmak için türün bilinmesi gerekmez: yalnız komşu karelerin
türüne bakılır (komşu maskesi), `RaidMaps.KindOf` okunmaz.

| Tür | Düzen | Maket nasıl okunur |
|---|---|---|
| Koridor | 7–9 dağınık, 2–3 karelik duvar | birleşik kısa istif sıraları (raf sırası, torba seti) |
| Kasa odası | kenarda duvarlı küçük oda, 2 kapı | kapalı istif odası (raf kafesi, pano odası) |
| İki hat | 4–6 karelik uzun bölme | tek parça uzun yapı; kanal ray ya da bara gibi uzanır |
| Açık salon | 5–7 ayrık blok, tek kanal kareleri | ayrık bloklar kolon gibi dik istif; tek kanallar döşeme kuyusu |
| Şebeke | bir sütun boyunca kanal bandı | uzun kablo, bara ya da ray hattı |
| Rehine | yolun ortasında 3 sivil | sivil ekipman kümesi |

## Önerilen katmanlar

Katmanlar değer/maliyet sırasına göre dizildi. Etiketler:
- **[sunum]:** yalnız sunum katmanı değişir.
- **[Core metin]:** Core'da yalnız metin değişir; RNG, komut ve kayıt değişmez.

### 1. Tek yer adı: bina + sektör odası · [Core metin] · M

Oyuncunun ne gördüğü yukarıda, "Yer kimliği" bölümünde.

**Core**
- `GameData.RegionDef`'e iki alan eklenir: `Building` ve `Sectors[3]` (ikisi de `Loc.N`).
- `LiberationSectorState.Name` (`CampaignLiberation.cs:14`) bölgenin sektör adını döndürür. Bu hesaplanan bir özellik,
  serileştirilmiyor; bu yüzden `CurrentVersion` artmaz.
- `Baskin.RoomName`, bölge tanımlıysa binayı döndürür. `RegionEvents.Room` alanı artık okunmaz; şimdilik silinmez.

**Masa**
- `CrtViewBuilder.cs:110` satırının biçimi değişir.
- Fiş (`LiberationMapBuilder.Slip`, :253) bir bina alt satırı alır. Satırın ipucu, BOLGE-OLAYLARI'ndaki bina cümlesidir
  (EN çevirisi gerekir).
- Kayıt paneli durum satırı: `RaidDataSource.cs:280`.
- `ShiftReportBuilder.SectorLabel` (:317) bölgeyi sektör kimliğinin önekinden alır.
- `FinalReportBuilder.SectorSources` (:24) 36 oda adını iki dilde tanır.
- Teleks, sonuç kartı ve rapor kod değişikliği istemez; zaten sektör adını basıyorlar.

**Metin**
- Eklenir: 12 bina, 36 oda (4'ünün kaynağı zaten var), 12 bina cümlesi.
- Çıkar: 3 genel sektör adı.
- Sonra: Çeviri tablosunu güncelle, strings.txt'e İngilizcesini yaz, LocalizationTests'i çalıştır.

**Testler ve belgeler**
- Yeni test: her bölgede bir bina ve birbirinden farklı 3 oda; her ad TR ve EN'de ≤18 karakter ve bütün bölgeler
  arasında tek. Son rapor anlarını tanıyan kod bu tekliğe dayanıyor.
- Güncellenecek testler: SiginakShiftTests:60, SiginakCrtTests:95, LiberationReportTests:115, SadeRaidRev2Tests:121 ve
  LiberationCampaignTests'teki an metinleri.
- DeskGame.unity'deki 4 sabit "Tedarik deposu" yazısı elden geçirilir.
- Belgeler: OYUN-TASARIMI:25, BOLGE-OLAYLARI tablosu ve DURUM.md:112.

**Risk**
- "2 ikmal + 1 sunucu" yapısını öğreten genel adlar gider. Bu yapıyı artık YAĞMA / SUNUCU YIKIMI etiketleri taşır; her
  03'ün sunucu ya da kumanda odası olması da yardımcı olur.
- CRT satırı en uzun hâlinde yaklaşık 59 karakter olur (bugün 46). Sığmazsa bina bu satırdan çıkar, yerine "PAFTA 2/3"
  yazılır. "SAHA MASASI" öneki kalır; sahibin onayladığı telsiz kurgusunun parçası.
- Genişlik iki dilde denetlenir: SiginakFontPreview'ın 'sectorName' örneği ve punto taramasıyla.

### 2. Yağma hedefi ikmal sandığı olur · [sunum] · S

**Oyuncu ne görür.** Yağma sektörlerinde (01 ve 02, eğitim dahil) sunucu düğüm dolabının yerine bir sandık durur:
- halat tutamaklı ahşap gövde, çelik köşe başlıkları;
- kapakta mürekkeple basılmış KYGA amblemi (yazı değil, simge);
- krem bir kargo etiketi.

Sandık, KYGA'nın savaş öncesi sivil savunma deposundan; YZ el koymuş. Açılacak bir şeydir, yıkılacak bir tehdit
değildir; bu yüzden üstünde kırmızı yok. Açılınca kapağı yana yatar. Sunucu sektörlerindeki düğüm ve çekirdek dolabı
değişmez.

**Nasıl**
- `RaidPieceLibrary`'ye `lootCrate` eklenir.
- Köprü görev türünü panoya verir; Katman 3'teki kit ile aynı noktadan (`board.SetDress(kit, missionKind)`).
- `NewObjectPiece` yağma görevinde `node` yerine sandığı kurar.
- Hedef listesi ve kartın küçük resmi: `thumb_crate` (`RaidDataSource.cs:257, 296`). Sprite `SiginakTextureBaker` ile
  üretilir, `SiginakSpriteManifest`'e kaydedilir.
- Can etiketi ve hedef çerçevesi aynı kalır.

**Risk**
- Eğitimin görüntüsü değişir; metni zaten "İkmal kasası" diyor. TutorialRaidTests çalıştırılır ve eğitim bir kez elle
  oynanır.
- Can etiketinin rengine karar verilmeli (bkz. Karar 4).
- Kuzey Amerika'nın palet istifleriyle karışmamalı: sandık çıplak ahşap ve amblemli, palet yükü streç filmli ve gri.

### 3. Bina kiti ve duvar barikatları · [sunum] · Aşama A: M, Aşama B: L

**Oyuncu ne görür.** Duvar blokları tahtanın en büyük kütlesidir (haritada 5–13 blok). Bu bloklar binanın malzemesine
bürünür. Kurgu şu: duvar, YZ'nin binanın kendi eşyasından yığdığı bir barikattır. Bu yüzden taşınabilir istifler seçildi
(raf, kasa, palet, torba, panel), sabit makineler değil. Aynı sektöre ikinci girişte yeri değişmiş bir türbin
inandırıcı olmazdı.

Tablonun son iki sütunu sonraki katmanlara ait (kanal: Katman 4, moloz: Katman 5); bina kimliği tek tabloda görülsün
diye burada.

| Bölge · bina | Duvar barikatı | Kanal içi (her kitte bakır var) | Gedik molozu |
|---|---|---|---|
| Batı Avrupa · Banka veri merkezi | sökülmüş rack kabinleri: kapı çizgisi, delikli havalandırma bandı, üstte kablo tavası şeridi (varsayılana en yakın kit) | tel örgü tavada bakır kablolar | kırık rack kapağı, kablo |
| Doğu Avrupa · Devlet arşivi | çelik arşiv rafı; raf aralarında kurşuni ve krem dosya kutuları | döküm ızgara kenarlı eski kanal, bez sargılı kablo | dağılmış dosyalar |
| Orta Doğu · Körfez rafinerisi | kutu sehpa üstünde yatık, ince boru demeti, flanş halkaları | boru kanalında bakır güç kablosu, göbekte flanş | kesik boru parçaları |
| Afrika · Cevher hangarı | oluklu galvaniz sac bunker, içinde basık cevher yığını | kauçuk kılıflı kalın besleme kablosu | cevher ve sac kırığı |
| Kuzey Amerika · Dağıtım merkezi | ahşap palet üstünde streç filmli koli yükü | iki kılavuz ray, ortada bakır enerji rayı | dağılmış koliler |
| Rusya · Sibirya veri merkezi | sandviç panel kabin, iki dikey derz | gri sargılı ısıtıcı hat ve bakır | panel ve köpük kırığı |
| Hindistan · Çağrı merkezi | ofis bölme paneli ve dosya dolabı istifi | yassı şerit kablolar, pirinç kenar profili | dağılmış kâğıt |
| Çin · Montaj fabrikası | istiflenmiş parça kasaları | robot hattının kablo zinciri | saçılmış parçalar |
| Japonya-Kore · Çip fabrikası | sökülmüş pano gövdeleri: kapı çizgileri, panjur yarıkları | krem izolatörlerde yassı bakır baralar | pano kapağı, kablo |
| Güneydoğu Asya · Elektronik fabrikası | dört sıra kum torbası, altta ıslak koyu bant | koyu reçine su (#343A33), içinde düşmüş bakır kablo | patlamış torbalar |
| Güney Amerika · Hidroelektrik santrali | ahşap takoz üstünde çelik levha ve kalıp istifi | jeneratör kablo demeti, göbekte çelik ızgara | eğri levhalar |
| Okyanusya · Maden limanı | baklava sac kızak üstünde yedek parça kasası | yarısı baklava sac kapaklı kanal, açık yarıda bakır | sac ve cıvata |

**Değişmeyen geometri.** Blok iki katlıdır; ayak izi 0,94, tepesi 0,56 ve üst yüzü düzdür. Böylece WallTop işaretleri,
hedef çerçevesi ve `Pick` olduğu gibi çalışır.

**Değişenler**
- Süs yalnız kameranın gördüğü yüzlere gelir: batı, güney ve üst yüz.
- Komşu iki duvarın arasındaki boşluğa bir köprü parçası (kiriş, boru ya da sac) girer. Böylece bir duvar dizisi tek
  yapı gibi okunur.
- Ayrık blok (8 komşusunda da duvar olmayan blok) kitin dik istifi olur ve salonda kolon gibi okunur.
- Kayar raf (Doğu Avrupa olayı) arşiv rafının aynısıdır. Farkı, uç levhasındaki üç kollu el çarkı ve altındaki iki
  tekerlek. "Bu duvar kayar" bilgisi böylece renkle değil şekille verilir. DURUM.md:945'teki açık iş de kapanır.

**Aşamalar**
- **A, kâğıt kaplama:** geometri aynı kalır, yan ve üst yüze baskılı doku gelir; maketçilerin bina kaplama kâğıdı gibi.
- **B, kabartma:** her bloğa 3–8 küçük parça eklenir (çıta, kutucuk, torba, el çarkı).

**Nasıl**
- **Kit asset'i.** Yeni `RaidRoomKit` ScriptableObject, kendi dosyasında (`Game/Raid/RaidRoomKit.cs`). Alanları: id,
  regions[], wall, shelfWall, cable, civilian, wallRubble ve adlar.
- **Kit seçimi.** `RaidPieceLibrary` şunları alır: `defaultKit`, `kits[]` ve `KitFor(region, mapSeed)`. `MapSeed 0`
  ise varsayılan kit döner. Kiti köprü seçer ve panoya verir.
- **Karo giydirme.** Yeni `RaidTileSkin` MonoBehaviour, kendi dosyasında (`Game/Raid/RaidTileSkin.cs`).
  - Çocukları: duvar için faceW, faceS, bridgeE, bridgeN ve isolated; kanal için hub, armN/E/S/W ve single.
  - `Apply(komşuMaskesi)` bu çocukları açıp kapatır.
  - Maske `BuildTile`'dan sonra hesaplanır. Bir karo değişince (raf kayması, dinamit) değişen kare ve 4 komşusu için
    `lastState`'ten yenilenir.
  - Raf olayının `EventFx` yolu (`RaidBoard3D.cs:1504`) animasyon bittikten sonra komşu kareleri yeniden giydirir.
- **Aşama A dokuları.** Yeni editör aracı `RaidKitTextureBaker` (SiginakTextureBaker kalıbında: sabit tohum, yazısız).
  Her kit için yan yüze 512×160, üst yüze 256² doku üretir. Renkler `raid_tokens.json → kits.{id}.wall*`'a yazılır.
  Malzemeleri tek seferlik bir script üretir.
- **Aşama B parçaları.** 7 geometri ailesi elle kurulur. Parçalar birleştirilir; karo başına en çok 3 renderer olur.
  Yeni üretilecek mesh yalnız iki tane: basık torba ve yarım silindir.
- **Batı Avrupa kiti varsayılana yakın tutulur:** aynı renk ailesi, üstüne rack çizgileri. Eğitim ile weu_2 böylece
  aynı binada geçiyor gibi görünür.
- **Deneme.** Editörde denemek için `RaidDataSource`'a yalnız editörde çalışan bir `kitOverride` alanı eklenir.

**Kabul kapısı (okunurluk sözleşmesi).** Bir kit bu kontrollerden geçmeden kütüphaneye bağlanmaz.
- Sayısal sınırlar:
  - duvar üst yüzü L* 58–74, yan yüz L* ≥40, doygunluk C* ≤22;
  - ahşap piyonlara (PawnWood #C9A77A) ve kontrplağa ΔE ≥15, ayrılmış renklere ΔE ≥20;
  - emission yok; ince parça ≥0,025 birim; karo başına en çok 3 renderer.
- `RaidRoomKitTests` şunları denetler:
  - her bölgenin bir kiti var; `MapSeed 0` varsayılan kiti veriyor;
  - her kitte duvar, kanal ve sivil parçası atanmış;
  - sınır kutuları ve renk sınırları tutuyor;
  - sivil varyantlarında halka, çarpı ve mavi bant var;
  - kanal kollarının ikisinde de kenar şeridi var.
- Görsel kontrol: `CorrigibleDebug`'a bir kit önizlemesi eklenir. Aynı tohum ve harita 12 kitte çizilir ve tek
  sayfada yan yana çıkar. Sayfanın gri tonlu kopyasında hedef, tehdit, sivil ve kanal renksiz de ayrılabilmeli.
- İlk kez bir baskın kare süresi ölçülür; IMPLEMENTATION_MAP:700-713'te baskın için ölçüm yok.

**Risk**
- Yan yüz deseni birimlerin arkasında gürültü yapabilir. Desen kontrastı düşük tutulur (ΔL* ≤15).
- Koyu kitler gunmetal makineleri ve ahşap piyonları yutabilir; L* kapısı bunu önlemek için var.
- Yan yüzler 1080p'de ancak 30 px kadar. Asıl etkiyi Aşama B taşıyabilir. Önce A yapılır ve Play'de bakılır.
- Sanat zamanının çoğu bu katmanda harcanır.

### 4. Kablo kanalı: göbek ve kollar, içi binaya göre · [sunum] · M

**Oyuncu ne görür**
- Kanal artık komşu kanala doğru bağlanır. Düz, köşe, T, uç ve tek kare doğru görünür. Bu düzeltme varsayılan kitte de,
  yani eğitimde de geçerli olur.
- Kural ve işareti değişmez: zemine gömülü koyu oluk, her kolun iki dış kenarında turuncu-siyah şerit, her kitte en az
  bir görünür bakır.
- İçi binaya göre değişir (Katman 3 tablosu) ama her zaman elektriktir. Böylece "Elektrikli kanal" ve "yere düştü
  (elektrik)" metinleri her binada doğru kalır.
- Açık salondaki tek kanal kareleri kitin kuyusu olur. Örnekler: kapağı açılmış döşeme kuyusu (Batı Avrupa), dökme
  muayene kapağı (Doğu Avrupa), su dolu çukur (Güneydoğu Asya).

**Nasıl**
- `Cable.prefab` yeniden kurulur: bir göbek (Hub), dört kol (ArmN/E/S/W) ve tek kare (Single). Kit varyantları yalnız
  içerik çocuklarını değiştirir. Kolları `RaidTileSkin.Apply` açar.
- Çarpıştırıcı ve sınır kutusu bugünkü gibi kalır, böylece birim kanalın üstünde durur.
- Su URP Lit ile yapılır (smoothness 0,35). Rengi mavi değildir, çünkü mavi "geri alınmış" ve hareket demek.

**Risk**
- Kollar zemin hizasında kalmalı. Eğitimin 26. adımındaki "Teknisyen kablo kanalının üstünden yürüyebilir" cümlesi her
  kitte doğru olmalı.
- Kenar şeridi hiçbir kitte değişmez; değişirse oyuncunun öğrendiği kural kaybolur.

### 5. Olay nesnelerine model, gediğe kitin molozu · [sunum] · S

- **Basınçlı tüp** (Güney Amerika): Ø0,2 × 0,75 ince silindir, koyu gri gövde, turuncu omuz bandı, vana başlığı,
  koruma yakası, iki çıtalı ayak. Fan ve sarı üçgen yoktur; soğutma ünitesinden böyle ayrılır. Bu fark önemli: soğutma
  ünitesi patlar, tüp ise fırlar.
- **Kaynak kolu kaidesi** (Çin): 0,7'lik çelik plaka, Ø0,4 döner tabla ve turuncu kaynak halkası. Kolun kendisi bugünkü
  turuncu çizgi olarak kalır. Hedef sanılmasın diye kırmızı kullanılmaz.
- **Tavan enkazı** (Afrika): yatık bir oluklu sac parçası, bükük bir çatı kirişi ve beton topak. Can etiketi bugünkü
  gibi kalır.
- **Duvar molozu:** Madenci'nin dinamitle açtığı gediğe bugün Node renginde dolap enkazı konuyor
  (`RaidBoard3D.cs:366`). Yerine kitin malzemesinden alçak bir moloz gelir (Katman 3 tablosunun son sütunu).

**Nasıl**
- `RaidPieceLibrary`'deki boş `tube`, `weldArm` ve `debris` yuvaları doldurulur (Damage grubu ve VisualHeight ile).
- Yeni `wallRubble` molozu çizen iki dalda kullanılır: gedik dalı (`:366`) ve `m.Rubble` dalı (`:425`).
- Üç olay nesnesi kitten bağımsızdır, hemen yapılabilir. Moloz Katman 3'e bağlıdır.

**Risk:** tüp ile soğutma ünitesi karışabilir. Fark turuncu bant, incelik ve fan olmamasıyla verilir.

### 6. Kartta kural adı ve altında bina adı · [sunum] · S

**Oyuncu ne görür.** Görünüş değişince oyuncu "bu ne?" diye sorar. Kartta kural adı aynı kalır; binadaki adı altında,
ikinci ve gri bir satırda yazar:
- "Duvar" ve altında "Kayar arşiv rafı" ya da "Kum torbası barikatı";
- "Elektrikli kanal" ve altında "Bara kanalı";
- kurallar kartının lejantında: "Elektrikli kanal · bara kanalı".

**Önce bugünkü bir açık kapanır.** Bugün kanal karesinin kartı "Boş kare" diyor ve altında satır yok
(`RaidDataSource.cs:295`). Karta önce kural eklenir: başlık "Elektrikli kanal", satır "Üstünden yürünür; üstüne itilen
ya da çekilen birim 1 hasar alır."

**Nasıl**
- Yeni `RaidKitText`: Bridge'de duran statik bir tablo (MonoBehaviour değil), kit kimliğine göre `Loc.N` adları tutar.
- `RaidDataSource.Inspector` (`:291-300`) ikinci satırı ekler.
- Lejantta `Loc.F("{0} · {1}", …)` kullanılır.
- Metin: yaklaşık 36 ad ve 2 kural satırı, TR ve EN. Varsayılan kitte ek satır çıkmaz.

**Risk:** kartın yerleşimi kontrol edilmeli. Ek ad kural sanılmamalı; bu yüzden yalnız ikinci satırda ve gri durur.

### 7. Bina sesi: bölgeye göre ortam yatağı · [sunum] · M

**Oyuncu ne duyar.** Hat açılınca genel uğultunun yerine binanın kendi sesi gelir. Kimliği bas tonlar değil, telsiz
bandının içinde kalan ritim ve tonal olaylar taşır, çünkü bant basları siliyor.

| Yatak | Bölgeler | Sürekli ses | Seyrek ayrıntı (7–15 sn'de bir) |
|---|---|---|---|
| SUNUCU | weu, ru, eğitim | fan uğultusu, hafif bobin cızırtısı, UPS çift bipi | weu: CO₂ panel uyarısı · ru: soğutucu "klak-vıııın", çatıda rüzgâr |
| ARŞİV | eeu | neredeyse sessiz; duvar saati tıkırtısı, floresan vızıltısı | raf makarası gıcırtısı |
| RAFİNERİ | me | kompresör pıt-pıtı ve supap "çıt"ı, gaz tıslaması, panjurda kum taneleri | Kum fırtınası özelliği ilk kez duyulur hâle gelir |
| HANGAR | af, oc | açık bölmeden rüzgâr, oluklu sac tıkırtısı | af: kiriş gıcırtısı, çakıl dökülmesi · oc: hidrolik pompa, uzakta gemi düdüğü |
| LOJİSTİK | na | makaralı hat takırtısı, havalandırma | forklift geri vites bipi, sözsüz anons "dıng-dong" |
| KALABALIK | in | anlaşılmaz kalabalık mırıltısı (dil yok), tavan vantilatörü | telefon çalması, sokak kornası |
| FABRİKA | cn | pres gümlemesi, pnömatik "pşt" | perçin tabancası |
| TRAFO | jk | trafo vızıltısı, temiz oda hava akışı, kontaktör tıkı | şalter "klak" |
| SU | sea, sa | akan su, damlalar | sea: pompa guruldaması, muson sağanağı · sa: türbin uğultusu kabarması |

**Nasıl**
- **Veri.** `SoundBank.cs`'e bir `[Serializable] RaidSite` yapısı eklenir: region, bed, details[], detailGap ve
  ileride kullanılmak üzere step. Kayıtlar enum ile değil, bölge kimliğiyle eşlenir.
- **Çağrı.** Yeni `Sfx.RaidSite(region, details)`; `RaidDataSource.OpenState` çağırır. Eğitimde ayrıntılar kapalıdır.
- **AudioDirector.**
  - İki yatak kaynağı (A/B: klip sessiz olanda değiştirilir) ve bir ayrıntı kaynağı. Üçü de telsiz bandından geçer.
  - Bir bölgenin yatağı yoksa yedek olarak `LoopCue.RaidAmbience` çalar.
  - `LoopCue`'ya yeni değer eklenmez, çünkü her `LoopCue` sürekli çalan bir kaynak açıyor.
  - Ayrıntıların zamanlaması `UnityEngine.Random` ile seçilir; `S.Rng`'ye dokunulmaz.
  - Son 0,4 saniyede bir saha sesi başladıysa ayrıntı atlanır, böylece gözcünün bildirimini örtmez.
- **Seviye kuralları.** Bant sonrası ses seviyesi bugünkü uğultuya eşit tutulur (RMS ≈0,078). Yataklar işaret
  seslerinin en az 10 dB altında kalır. Hışırtı seviyesi aynı kalır.
- **Yer tutucular.** Tek seferlik bir üretici script yazılır. Eski üretici (`PlaceholderAudio.cs`, f8436ff
  commit'inde) temel alınır, sabit tohum kullanılır ve her klibin bant kaybı ölçülür (hedef ≤6 dB). Kalabalık, yağmur
  ve gemi düdüğü için baştan CC0 kayıtlar kullanılır; her dosyanın lisansı ayrı takip edilir.
- **Test.** `SoundBankTests`: her bölgenin bir yatağı var.
- **İlk tur.** Önce 9 yatak yapılır. Sahibi dinleyip onaylarsa 24 ayrıntı eklenir.

**Risk**
- Tekrar: bir baskın birkaç dakika sürer. Yataklar 20–24 saniye olur ve ayrıntılar rastgele araya girer.
- Gözcünün sesini örtme tehlikesi; yukarıdaki atlama kuralı ve 10 dB kuralı bunun için.
- Telsiz vurgusu: sahibi daha önce fazlasını geri aldırdı. Bu yüzden bant aynı, seviye aynı, konuşma yok.
- Birleşik yataklarda (weu/ru, af/oc, sea/sa) bölgeleri ayırmak ayrıntılara kalır. Yetmezse 12 ayrı yatağa geçilir;
  bu yalnız veri değişikliği.

### 8. Olay sesleri · [sunum] · M

**Oyuncu ne duyar.** Kendi eylemini binanınkinden ayırabilir. Bugün durum şu:
- bulut, gaz ve kriyo: duman bombasının sesi;
- raf, asansör, turnike ve süzgeç: EMP itmesinin sesi;
- kaynak ve faz değişimi: EMP patlamasının sesi;
- tüpün uçuşu: dron atışının sesi.

Yeni ses örnekleri:
- bulut: iki kısa alarm bipi ve meme "pşşşt"ı;
- raf: el çarkı cırcırı, yuvarlanma ve sonda "klonk";
- gaz parlaması: döküntüsüz bir "vuuumf";
- tavan: çakıl şelalesi;
- asansör: motor sesi ve "dıng";
- donma: sıvı azot tıslaması;
- turnike: üç cırcır ve vardiya zili;
- kaynak: ark cızırtısı; faz: kontaktör "klak"ı;
- süzgeç: guruldama;
- tüp: vana ıslığı, fırlama ve indiği karede çınlama;
- yağ: hortum "pat"ı ve püskürme.

**Nasıl**
- `Cue` enum'unun sonuna 67–88 eklenir (`EvCo2Burst` … `EvOilSlide`). Aradaki eski boşluklar doldurulmaz.
- `AudioDirector.FromField` (`:85`) ikinci bir aralık alır. Almazsa yeni sesler telsiz bandını atlar ve odada
  çalıyormuş gibi duyulur.
- `RaidBoard3D.EventFx`'te yalnız `Sfx.Play` argümanları değişir.
- SoundBank'a 22 yeni giriş eklenir.
- `SoundBankTests`: her yeni olay sesinin klibi var ve `FromField` onu doğru tanıyor. `PlayCue` eksik klipte sessizce
  döndüğü için unutulan bir giriş olayı sessiz bırakırdı; test bunu yakalar.

**Risk**
- Hızlı makine modunda sesler üst üste biner; olay sesleri 1,2 saniyenin altında tutulur.
- Olay sesleri patlama ve çekirdek kırılma seslerinden yüksek olmamalı.
- Gaz parlaması tehlike olarak okunmalı; kırmızı "PARLAMA" yazısıyla birlikte çalar.

### 9. Baskın ekranında yer başlığı · [sunum] · A: S, B: M

Bugün baskın ekranında yerin adı hiç geçmiyor. İki basamak öneriliyor:

**A: görev çipi.** Çipin başlığına oda eklenir: "GÖREV · İKMAL · UPS ODASI" (`RaidHudView.cs:188-193`). Sığmazsa ad
çipin ipucuna gider; ipucunun ilk satırı "Yer · Banka veri merkezi, 02 UPS odası" olur.

**B: pafta anteti (karar bekliyor).** Paftanın künye köşesinden kesilmiş, keçeye bantla tutturulmuş bir kâğıt şerit.
Sağ üstte, TELSİZ / KURALLAR düğmelerinin altında durur (1080p'de yaklaşık 356×160 px).
- Satırlar: "OZALİT KOPYA · PAFTA 2/3" / "BANKA VERİ MERKEZİ" / "02 UPS odası" / "Batı Avrupa · Ölçek 1:50 · V.031".
- Köşede −7° eğik, mor bir "ASLI GİBİDİR" damgası.
- Baskın paketinde planlanıp hiç kullanılmamış bir başlığın karşılığı: `strings.title`, "SAHA OPERASYONU · {bölge} ·
  {yer}".

Yazı 3B kâğıda basılmaz. Kilitli kamerada kenara yatırılmış yazı yaklaşık 60° çarpılır. Kâğıdın 0,25 birimlik payı da
kare adlarıyla zaten dolu.

**Nasıl**
- Veri: `PaftaAntetData`, Bridge/Siginak altında statik bir kurucu.
- Görünüm modeli: `RaidViewModel.Antet`.
- Görünüm: `RaidAntetView`, Runtime/Raid altında kendi dosyasında.
- `RaidHud.prefab › Corners`'a tek seferlik bir script ekler. Renkler RaidTheme'den gelir. Antet punto taramasına
  eklenir.

**Risk**
- HUD zaten yoğun: 253 nesne.
- Arka sıradaki dolap ve dronlar yaklaşık 108 px yükselir; antetin alt kenarına (y≈248) uzanabilir.
- TELSİZ rozeti ve üstüne gelince açılan kartın konumu kontrol edilmeli.
- Kontrol: 1080p, 16:10 ve ultrawide; Türkçe ve İngilizce.

**Antet geldikten sonra eklenebilecekler**
- Tekrar girilen sektörde kurşun kalemle bir not: "2. giriş · önceki V.027: 1 uzman düştü".
- Geri alınıp yeniden düşen sektörde, ozalit damgasının yerine kırmızı "YENİDEN İŞGAL" damgası.
- Binanın silueti ve üç sektörün yerini gösteren küçük bir anahtar plan.

### Sonraya kalanlar

| Öneri | Ne | Neden sonra |
|---|---|---|
| Kabartma (Katman 3, Aşama B) | Bloklara çıta, kutucuk, torba, el çarkı | Önce Aşama A'nın Play'de yetip yetmediği görülmeli; kabul kapısı ve kare süresi ölçümü sonrası |
| Sivil ekipman ve hizmet cümlesi | Yalnız elektronik cihazlar: ATM, operatör konsolu, depo kiosku, röle panosu, kalite kontrol panosu, çağrı merkezi masası. Krem gövde, mavi bant, halka ve çarpı (Ø≥0,3) aynı. Kırılma satırı binaya göre: "Banka terminali kırıldı: mahallenin maaşları ödenmedi." (`Baskin.cs:487`, **[Core metin]**) | Sivil, "vurma" denen en önemli nesne; 12 ayrı siluet yeniden öğrenilmeli |
| Basılı kapılar | Gerçek girişlerin batı kenarında kapı yayı; iki karşı kenarı kapalı zemin karesinde kapı söveleri. Olay karelerinde ve eğitimde yok | Her bölgede aynı görünür, yeri ayırmaz; mürekkep mavisi hareket rengine yakın |
| Kesit duvarı | Paftanın uzak iki kenarında binadan kesilmiş alçak şerit (boru köprüsü, yükleme rampası…) | Uzak köşe kadrajın tepesinde, kuzey kenar görev çipinin altında; önce tek bir kitle 16:10 ve ultrawide çekimi |
| Zemine göre adım sesi | Metal ızgara, çakıl, ıslak zemin, yumuşak zemin (Katman 7'nin `step` alanıyla) | Baskında yüzlerce kez çalar; görünür su yokken ıslak adım yanıltır |

## Sıra

1. **Ad.** Katman 1, Katman 6'daki kanal kural satırı ve Katman 9A.
   - Ne gelir: her yüzeyde tek bir yer adı; iki adın çelişkisi kalkar.
   - Doğrulama: LocalizationTests, güncellenen masa testleri ve yeni ad testi; punto taraması. `CorrigibleDebug.Capture`
     ile fiş, CRT ve sonuç kartı TR ve EN'de, 16:10'da ve ultrawide'da çekilir. Eski bir kayıttan devam edilir; yeni
     adlarla açılmalı.
2. **Ucuz parçalar.** Katman 2 (sandık), Katman 5'in üç olay nesnesi ve kanalın göbek + kol olarak yeniden kurulması
   (varsayılan kitle).
   - Ne gelir: yağma baskınları sunucu baskını gibi görünmez; yatay kanallar düzelir; tüp soğutma ünitesine benzemez.
   - Doğrulama: TutorialRaidTests ve SadeRaidBoardTests; eğitim bir kez elle oynanır. Altı baskının karşılaştırma
     sayfası aynı tohumlarla yeniden çekilip eskisiyle yan yana konur.
3. **Bina kiti.** Katman 3'ün altyapısı, kabul kapısı ve Aşama A dokuları; Katman 4'ün içerikleri; Katman 5'in molozu;
   Katman 6'nın kit adları.
   - Ne gelir: 12 bina, 12 ayrı maket.
   - Doğrulama: RaidRoomKitTests; 12'li önizleme sayfası ve gri tonlu kopyası; ilk baskın kare süresi ölçümü. Raf
     kayması ve dinamitten sonra komşu karelerin yeniden giydirildiği Play'de denenir. Testler boş sahne açıkken
     çalıştırılır.
4. **Ses.** Önce yer tutucu üretici, bant denetimi ve SoundBankTests. Sonra 9 yatak ve sahibinin onayı. Sonra
   ayrıntılar, en son 21 olay sesi.
   - Doğrulama: bant kaybı raporu ve SoundBankTests. Play'de her bölgeden bir baskın dinlenir; gözcü bildirimleri
     örtülmüyor olmalı.
5. **Kabartma ve ekler** (kararlara göre): Aşama B, antet ve ikinci giriş notu, sivil ekipman, kapılar, kesit ve adım
   sesi.

Her aşama tek başına teslim edilebilir. 1. ve 2. aşama birlikte (yer adı ve yağma sandığı) sahibin ilk
değerlendirmesine yeter.

## Bırakılanlar

- **Harita adını göstermek:** çelişkiyi kapatmaz, görünür kılar ("PAFTA: METRO PERONU" bir bankada). Aynı sektörün adı
  da her denemede değişir.
- **Yeri telsiz satırıyla adlandırmak:** Katman 1'in kopyası. Artık okunmayacak `RegionEvents.Room`'a dayanıyor, kayıt
  paneline fazladan bir satır ekliyor; telsiz şeridini yeniden göstermek de sahibin geri aldırdığı vurgu.
- **Kanal, duvar ve uğultuyu harita türüne göre değiştirmek:** 12 bölgeye ortak 6 görünüm verir; bankaya ray, arşive
  metro vagonu sokar. İşe yarayan bulguları (kanal yönü hatası, kanalın kart satırı, kayar raf modeli) Katman 3, 4 ve
  6'ya alındı.
- **Harita türüne göre yankı:** telsiz bandında yapay duyulur, hızlı makine dizilerini bulandırır ve oyuncu fark etmez.
  Oda hissi istenirse yatağın içine kısa bir yankı kuyruğu gömülür.
- **Ayrı ayrı kapı baskıları:** aynı özelliğin üç sürümüydü. Tek kural "Sonraya kalanlar"da.
- **Harita üreticisine dürtü (rayı şerit boyunca çizdirmek):** bir görsel için Core harita havuzunu değiştirirdi
  (GELISTIRME.md:117-118). İki hat dengesini kaydırır ve süren kampanyaların havuzunu yeniler.
- **Odaya özgü yeni kurallar (geçen tren, raf yarım siper, buhar vanası):** bölge olaylarının yerini kapar, tur şeridine
  ikinci bir saat ekler; Core, makine aklı, tahmin ve bot ölçümü ister. Sorun sunumda, kurallarda değil.
- **Kit içinden çıkarılanlar:**
  - konveyör, makaralı hat ve hortum kanalı: elektrik gibi okunmaz, birimi taşıyormuş gibi görünür;
  - kiralık kasa duvarı: yağma hedefi sanılır;
  - yatık kompresör tamburu ve türbin: "bu da patlar mı?" diye sordurur ve barikat değil, sabit makinedir;
  - kart katalog dolabı ve titreşim arabası (sivil olarak): sunucu değiller.

## Karar bekleyenler

1. **Yer modeli.**
   - (a) Bina + 36 sektör odası, harita adları gizli. **Önerilen.**
   - (b) Bina + harita türünden türeyen alt bölüm adları (72 ad: "Raf koridoru", "Okuma salonu"…). Aynı sektöre her
     girişte ad değişir.
   - (c) 30 harita adı gösterilir, çelişenler yeniden adlandırılır ("Metro peronu" → "Yük peronu").

   Metro gerçekten isteniyorsa bir bölgenin binası yeniden düşünülmeli. Bugün en yakın görünüm Kuzey Amerika'nın rayı ile
   İki hat bölmesinin birleşimi.
2. **Maket ne kadar değişsin?**
   - (a) Aşama A dokular, kanal, sandık ve olay nesneleri. **Önerilen ilk adım.**
   - (b) Buna Aşama B kabartma da eklenir.
   - (c) Sivil ekipman da değişir (12 yeni siluet).
3. **Kâğıt.**
   - (a) Ozalit her bölgede aynı kalır. **Önerilen.** BRIEF_rev2.md:39 ozalit paftayı korumayı istiyor; Rev2 S6/S9'da
     paneller de tahtanın malzemesinden.
   - (b) Bina türüne göre 5 kâğıt ailesi: ozalit, sepya arşiv nüshası, yerleşim planı, aydınger tesisat paftası ve
     Afrika'da milimetrik gözcü krokisi. Ekranın en büyük alanı değişir; ama bütün işaret ve olay renkleri 5 kâğıtta,
     lamba altında yeniden ayarlanmalı, paneller tahtadan ayrılır ve duvar üst yüzü Katman 3 ile çakışır.
4. **İkmal sandığı.**
   - Hikâye: (a) YZ'nin el koyduğu, KYGA amblemli sivil savunma sandığı (**önerilen**) ya da (b) YZ'nin kendi sandığı,
     nötr bir şablonla.
   - Can etiketinin rengi: koyu mürekkep (önerilen) ya da bugünkü kırmızı.
5. **Baskın ekranında yer başlığı.**
   - (a) Yalnız görev çipi ya da ipucu satırı. **Önerilen ilk adım.**
   - (b) Tam antet şeridi, tahtayla aynı ozalit kâğıdında.
   - (c) Hiçbiri; yer yalnız masadaki yüzeylerde görünür.
6. **Kural değişikliği.**
   - (a) Hiçbiri; yer yalnız sunumla anlatılır. **Önerilen.**
   - (b) Sektörler harita türüne bağlanır (örneğin 03 hep salon ya da kasa odası). Harita seçimini ve bot ölçümlerini
     değiştirir.
   - (c) 1–4. katmanlardan sonra yer hâlâ hissedilmiyorsa, harita türü başına en çok bir pasif (zamanlayıcısız) kural.

## Uygulama (2026-11-02, 2026-11-03)

- **Ad (Katman 1):** `RegionDef.Building` ve `RegionDef.Sector(i)` (`GameData.cs`, 12 bina, 36 oda; tablo yukarıdaki gibi);
  `LiberationSectorState.Name` bölgeden okunur (kayda yazılmaz, sürüm aynı). `Baskin.RoomName` "bina · oda" verir;
  `RegionEvents.Room` kaldırıldı. Fişte bölge adının altında bina satırı (`OperationSlip.building`, sahnede tek seferlik
  betikle), CRT "SAHA MASASI · {BİNA} · GÖREV: …" (en uzun satır 933/996 px, ekran ~1850 px), görev çipi "{ODA} · İKMAL",
  ipucunun ilk satırı "Yer · bina · NN oda", telsiz kaydının durumu "Ekip {oda} girişinde.". Teleks, sonuç kartı, raporlar
  ve son rapor sektör adını zaten basıyordu. Kayıt yalnız komutları tuttuğu için eski kayıtlar da yeni adlarla açılır
  (anlar yeniden oynatılırken yazılır); son rapor karşı saldırı anını bölgenin oda adıyla tanır. "Trafo merkezi"nin İngilizcesi "Substation" oldu (≤18 harf).
- **Sandık (Katman 2):** `Prefabs/Raid/LootCrate.prefab` (düğüm prefab'ından: RaidPiece, çarpıştırıcı, can etiketi, hasar
  çatlakları) ve `LootCrateOpen.prefab` (boşalmış alçak tepsi: ≤ 0,25, ±0,37 içinde, içi koyu, iki gevşek çıta; kare
  artık yürünür, üstündeki birimi örtmez);
  malzemeler `Art/Raid/Analyst/Materials/Crate*.mat` (zeytin yeşili boyalı ahşap, çıta, halat, krem şablon, çelik köşe).
  `RaidPieceLibrary.lootCrate/lootCrateOpen`; `RaidBoard3D` yağma görevinde düğüm yerine sandığı kurar, can etiketi koyu
  mürekkep (görev çipindeki pipler de); zırh işareti kalır (EMP sandığa da işlemez); Teknisyen alınca kırık dolap enkazı
  yerine boşalmış sandık kalır (kırılırsa enkaz). Küçük resim
  `thumb_crate` 3D sandıktan çizilir (`SiginakTextureBaker.PieceThumbs`, menü: Corrigible › Sığınak › Baskın parçalarının
  küçük resimlerini üret; siyah/beyaz zemin farkından saydamlık), sprite tablosunda üretilen; kayıt üretilen `thumb_*`
  sprite'larını da alır.
- **Fişte binanın ipucu (Katman 1'den kalan):** `RegionDef.BuildingNote` (Core, yalnız metin: 12 bina için iki cümle,
  BOLGE-OLAYLARI'ndaki bina cümlesinden bina düzeyinde yeniden yazıldı). Fişteki bina satırının üstüne gelince ortak
  ipucu açılır: başlık "BİNA · {BİNA}", satır not (`OperationSlip.buildingHover`, sahnede tek seferlik betikle; bilgi
  fişlerinde de).
- **Kanal (Katman 4):** `Cable.prefab` = Hub (+Junction), EdgeN/E/S/W, ArmN/E/S/W ve tek kare için Single. Komşu maskesi
  `RaidTileMask` (K=1 y−1, D=2, G=4, B=8; 8 komşuda duvar yoksa yalıtılmış), giydirme `RaidTileSkin.Apply(maske,
  yalıtılmış, ray yönü, kit)`: yalnız çocukları açıp kapar ve malzeme atar, yeni nesne kurmaz. Yatay koşu hatası (eğitimde de)
  düzeldi. Raylar ±0,31, tehlike şeridi ±0,36; hepsi ≤ 0,048 yüksek, çarpıştırıcısız, gölgesiz. Düz geçişte ve tek karede
  bağlantı kutusu gizli. Kanalın içi kitin dokusunda (`channelFloor`; kol bantlarına göre yönlü), kuyu Hub'ın zemininde
  (`wellFloor`; bakır sargı üstünde kalır). Güneydoğu Asya'da koyu reçine su (#343A33, pürüzsüzlük 0,35; mavi değil).
- **Bina kiti (Katman 3, Aşama A):** `RaidRoomKit` (`Settings/Kits/Kit_<bölge>.asset`: duvarın üst/batı/güney, sütunun
  batı/güney, kanal ve kuyu malzemeleri, `wallRubble`, `bridges`). `RaidPieceLibrary.defaultKit/kits/KitFor(bölge, MapSeed)`
  (MapSeed 0 ya da bilinmeyen bölge → varsayılan: eğitim, örnek baskın, görüntü seti); `RaidDataSource.OpenState`
  `Build`'den önce `board.SetDress` çağırır (`kitOverride` yalnız editörde). `Wall.prefab` = Body (`WallBlock`: eski iki
  katmanın ölçüsünde tek mesh, 3 alt mesh, yan yüzde v = y/0,547), BridgeE/N (komşu duvara köprü), ShelfGear (kayar rafın
  el çarkı ve iki teker; ray K–G ise batı, D–B ise güney yüzünde). Kare başına ≤ 3 renderer; varsayılan kit bugünkü
  görünümün aynısı. `Sync` türü değişen karenin 3×3 komşuluğunu yeniden giydirir; raf ve dinamit canlandırmasında köprüler
  gizlenir, bitince giydirilir. 12 kitin dokuları `RaidKitTextureBaker` ile (Corrigible › Baskın › Kit dokularını üret; kit
  başına sabit tohum, metin yok): yan ve sütun 512×160, üst, kanal ve kuyu 256², `Art/Raid/Kits/<bölge>/`'de PNG ve 7 URP Lit
  malzeme. Renkler `raid_tokens.json` → `kits.<bölge>.*` (`_new_1_3`); baker dokunun ortalamasını bu renge oturtur. Kabul
  kapısı `RaidKitGate` (yüz rengi = dokunun doğrusal ortalaması × _BaseColor, CIE76): üst L* 58–74, yanlar ≥ 40, C* ≤ 22;
  ahşaptan ΔE ≥ 15, ayrılmış kırmızı/mavi/yeşil/turuncu/sarıdan ≥ 20; desen ΔL* ≤ 15 (P5–P95; baker 14'e sıkıştırır);
  kanal ve kuyu L* ≤ 30, mavi yok; malzeme mat, metalsiz, ışımasız. Kapıdan geçmeyen kit kütüphaneye bağlanmaz. Önizleme:
  `RaidKitPreview` (Corrigible › Baskın › Kit önizleme sayfası; gerçek prefab ve giydirme kodu, gri tonlu kopya). Motifler:

  | Bölge | Duvar | Kanal / kuyu |
  |---|---|---|
  | weu | delikli kapılı rack kabinleri, üstte kablo tavası | kablo tavası / yükseltilmiş döşeme kapağı |
  | eeu | gri-yeşil arşiv rafları, kurşun gri ve krem dosya kutuları | döküm ızgara / döküm kapak |
  | me | flanşlı boru ağızları, kelepçeli borular | flanşlı kanal / açık boru ağzı |
  | af | oluklu galvaniz bunker, üstünde cevher yığını | cevherli çukur |
  | na | streç filmli koliler, palet | kablo rakorlu yer kutusu |
  | ru | sandviç panel, kırağı, nervürlü çatı | keçe / kırağılı çukur |
  | in | kumaş bölme panoları, dosya dolapları | yassı kablo ve pirinç kenar / yer kutusu |
  | cn | nervürlü parça kutuları (pul, somun, cıvata) | kablo zinciri / robot ankraj plakası |
  | jk | soyulmuş pano gövdeleri (kapı, panjur, kilit) | krem izolatörler / delikli temiz oda karosu |
  | sea | dört sıra kum torbası, ıslak alt bant | koyu su / süzgeçli su çukuru |
  | sa | çelik kalıp panoları, takozlu plaka yığını | jeneratör kablo demeti / ızgara |
  | oc | kırmızı tozlu çelik yedek parça sandıkları, baklava sac | baklava sac kapak şeritleri / kapak |

- **Olay nesneleri ve moloz (Katman 5):** `Tube.prefab` (Ø0,2 × 0,75; turuncu omuz bandı, vana, koruma yakası, iki çıtalı
  ayak; el çarkı yok, o kayar rafın işareti), `WeldArmBase.prefab` (0,7 plaka, Ø0,4 döner tabla, turuncu halka; tepesi
  `RaidBoard3D.WeldArmHeight` 0,3), `CeilingDebris.prefab` (beton topak, oluklu sac, bükük kiriş; Damage grubu). Hepsi
  kırmızısız, mat. Kırılan tavan enkazı yerinde dolap enkazı bırakmaz (dağılır). Yeni küçük resimler `thumb_tube`,
  `thumb_weld`, `thumb_debris`, `thumb_civ`, `thumb_spawner` (`RaidDataSource.ObjectThumb`); tabloda olmayan resim kartta
  gizlenir. Gedik molozu `Prefabs/Raid/Kits/WallRubble_<bölge>.prefab` (tek renderer, 0,10–0,19 yüksek, karenin içinde;
  kitin duvar malzemeleri, bir vurgu malzemesi `Kit_<bölge>_RubbleAccent.mat`, toz lekesi ve yongalar);
  `RaidPieceLibrary.BreachRubble(kit)` gedik dalında ve önizlemede, varsayılan kitte eski dolap enkazı.
- **Kartta ad (Katman 6):** inceleme kartının gri alt satırı (`RaidInspectorModel.Subtitle`, renk `ink.ink3`): boş duvar,
  kanal ve gedik karesinde kitin adı (`RaidKitText`, 12 kit × duvar/kanal/moloz, iki dilde), kayar raf duvarında "Kayar
  arşiv rafı"; varsayılan kitte ve üstünde birim ya da nesne olan karede alt satır yok. Kanal karesinin kartı "Elektrikli
  kanal" ve kural satırı (faz olayında yalnız canlı faz). KURALLAR lejantında "Elektrikli kanal · döküm ızgaralı kanal".
- **Bina sesi ve olay sesleri (Katman 7 ve 8):** yer tutucular sentezlendi (kayıt indirilmedi; üretici
  `Tools/Audio/raid_site_audio.py`). 9 yatak: BedServer (weu, ru), BedArchive (eeu), BedRefinery (me), BedHangar (af, oc),
  BedLogistics (na), BedCrowd (in), BedFactory (cn), BedTransformer (jk), BedWater (sea, sa); 21–24 sn kesintisiz döngü, bant
  kaybı ≤ 3,1 dB, telsiz bandından sonra RMS 0,078 (bugünkü uğultu), bankada 0,45. 22 olay sesi `Cue` 67–88 (EvCo2Burst …
  EvOilSlide; ≤ 1,15 sn); `RaidBoard3D.EventFx` her olayı kendi sesiyle çalar (kabin_bekle artık EvPhase 0,7). Bağlantı:
  `SoundBank.sites` (`RaidSite`), `Sfx.RaidSite` (OpenState), `AudioDirector` Bed A/B ve Bed Detail (telsiz bandından;
  yatağı olan bölgede genel uğultu susar; ayrıntı son 0,4 sn'de başlayan saha sesini ya da gözcüyü örtmez). Ayrıntılar bu
  turda yok: sahibin yatakları dinlemesi bekleniyor.
- **Karar gereği yapılmayanlar:** Aşama B kabartma, antet (9B), sivil ekipman (karar 2a, 5a).
- **Sapmalar:**
  - Açık sandık tasarımda "kapağı yana yatar" idi. Yana yaslanan kapak bitişik karedeki Teknisyen'in üstüne düşüyor ve
    şablonu onay işaretine benziyordu; kaydırılmış kapak da yan karelere taşıyordu ve kare artık yürünür olduğu için tam
    boy kutu üstündeki birimi örterdi. Boşalmış alçak tepsi seçildi.
  - Sandık etiketinde zırh işareti tasarımdan kaldırılmamıştı ama ilk sürümde kaldırıldı; EMP kuralı sandığa da zırhlı
    hedef gibi davrandığı için geri geldi.
  - Görev türünü panoya köprü değil, panonun kendisi okur (`Baskin.IsLoot`; sunum durumu okur). Köprüden tek nokta
    `board.SetDress(bölge, MapSeed)` bina kitini seçer.
  - 9A'da çipin başı "GÖREV · İKMAL · UPS ODASI" yerine "UPS ODASI · İKMAL" oldu (kısa, rozetle çakışmaz).
  - Fişteki bina satırının ipucu 2026-11-03'te yapıldı (yukarıda); notlar oda değil bina düzeyinde (oda adı fişte yok).
  - Boşalmış sandık, tahta durumdan sıfırdan kurulunca (örnek baskın, durumdan açma) çizilmez; oyunda kasa alındığı anda
    konur.
  - **Köprüler:** varsayılan kit köprüsüz (bugünkü görünüm piksel piksel aynı kalsın, MapSeed 0 baskınları bugünkü gibi);
    bölge kitleri köprülü. Varsayılan kite köprü `Kit_default`'ta tek kutu, sahibin kararı.
  - Raf bloğunun ray yönü komşu raf karelerinden; komşusu kalmayan blokta (ortası dinamitlenmiş ray) olayın rayından.
    Dinamit canlandırmasında da köprüler gizlenir (tasarım yalnız rafı söylüyordu).
  - Kanalın bağlantı kutusu uçta, köşede, T'de ve dörtlü kavşakta görünür; yalnız düz geçişte ve tek karede gizli. Raylar
    0,025 geniş (ince parça kuralı); kanal parçalarının hepsi gölgesiz (6 cm kuralı).
  - Üst dokular koşu yönüne göre dönmez (dünya x boyunca: me boruları, weu tavası, ru çatı nervürleri). me'nin yan yüzü
    uzun boru değil boru ağızları: aynı doku iki yüzde, uzun yatay boru tahta ya da tuğla gibi okunuyordu.
  - Kuyular Aşama A'da yalnız doku (bakır sargı üstünde kalır); oc kanalında yarım kapak değil iki yanda baklava sac
    şeritler; jk'de yassı bara değil bugünkü yuvarlak bakır (geometri değişmedi), krem izolatörler dokuda.
  - Kapı tasarımdaki eşiklere ek olarak kanal ve kuyuyu da denetler (L* ≤ 30, mavi yok, ahşap ve ayrılmış renkler). Soğutma
    ünitesi ve sandık rengi kapıda değil: eeu üstü soğutma ünitesine ΔE ≈ 10, ru ≈ 6, jk/sa/cn 7 civarı; şekil ayırıyor.
    Soğutma ünitesini ve sandığı ayrılmış listeye eklemek sahibin kararı. Rengi birbirine yakın kit çiftleri yalnız motifle
    ayrılır (me–sea ΔE ≈ 3,9; in–sa ve cn–sa ≈ 5,2). weu varsayılana en yakın kit (duvar yüzlerinde ortalama ΔE 2,0).
  - Gedik molozuna toz lekesi ve duvar malzemesinden yongalar eklendi: onsuz moloz dağınık eşya gibi okunuyordu. Moloz
    yalnız gedik dalında; `m.Rubble` dalı hiç dolmuyor (ölü), kullanılmadı.
  - Lejant biçimi `{0} · <nobr>{1}</nobr>`: kit adı bölünmez; 160 px'e sığmayınca satır ikiye kırılır, alttaki satırlar ve
    KURALLAR kartı bir satır uzar (`RaidPanelsView.LayoutLegend`; varsayılan kitte piksel piksel aynı).
  - Gedik karesi çıkışa dönünce başlık "Çıkış" olur ve alt satır gösterilmez. MapSeed 0'da raf duvarı da adsız (el çarkı
    görünür).
  - Kırılan tavan enkazı moloz bırakmaz (tasarımda yoktu; dolap enkazı sunucu kırılmış gibi okunuyordu).
  - Ses: olay sesleri yerini aldıkları sesle eşitlendi, ama hiçbiri telsiz bandından sonra RaidExplosion'dan yüksek değil.
    Bugünkü RaidSmoke, RaidHitObject ve RaidDroneShot zaten ondan yüksek olduğu için onların yerine gelen sesler 3–10 dB
    daha sessiz (EvTubeLaunch −9,8 dB). Yataklar bugünkü uğultunun düzeyinde (tasarımın sabitlediği gibi): bu yüzden
    "yatak sinyal seslerinin ≥ 10 dB altında" kuralı bugünkü gibi karşılanmıyor (gözcünün bildiriminden RMS'te 3–6 dB
    yüksek). Sahip dinledikten sonra karar verilir. Kalabalık ve su yatakları yapay (CC0 kayıt indirilmedi). Olay sesi 21
    değil 22.
  - Ayrıntı kaynağı rastgele sağ-sol (±0,5) çalar; `RaidSite`'a `detailVolume` eklendi, `step` alanı yok.
- **Testler:** `LiberationCampaignTests.Regions_HaveABuildingAndThreeDistinctRooms` (iki dilde tek ve ≤18 harf; sektör adı
  bölgeden; `RoomName`), `LootRaidPresentationTests.LootCrate_IsItsOwnRedlessPiece` (kırmızısız, gövdesi dolaptan alçak,
  boşalmış hâli alçak ve karenin içinde, küçük resmi kayıtta) ve yağma sunumunda sandık küçük resmi, çipteki oda ve ipucu; `SiginakMapTests` fişin binası ve
  oda adları; CRT, vardiya, rapor testleri yeni adlarla. 2026-11-03'te: `RaidTileMaskTests` ve `RaidTileSkinTests` (maske,
  yalıtılmışlık, ray yönü, giydirme, ≤ 3 renderer), `RaidRoomKitTests` (kapı, bütçe, her bölgenin kendi kiti, weu
  varsayılana en yakın, kanal mavi değil ve yalnız sea su, baker `UnityEngine.Random` kullanmaz, molozun ölçüsü ve rengi,
  kit adı kuralı kütüphaneyle aynı), `EventObjectModelTests` (üç modelin ölçüsü, kırmızısızlığı, kaynak kolu yüksekliği,
  bütün küçük resimler), `SoundBankTests` (her bölgenin yatağı, klip ayarları, olay seslerinin adı ve uzunluğu, `FromField`,
  `EventFx` eşlemesi, A/B geçişi), `SadeRaidHudTests` (kanal kartı, faz satırı, kitin adı gri alt satırda ve varsayılanda yok, lejantın sığması), `SadeRaidBoardTests`
  (alt satır, bilinmeyen küçük resim), `SiginakMapTests` (12 bölgenin bina ipucu iki dilde, sahnede bağlı),
  `LocalizationTests` (`BuildingNote`).
- **Deneme (2026-11-03):** Play'de V014'ten Doğu Avrupa baskını gerçek akışla açıldı: kit eeu, BedArchive telsiz bandından,
  genel uğultu sustu; duvar, raf ve kanal kartları, KURALLAR lejantı. Aynı yüklü sahnede Güneydoğu Asya ve Afrika (kit
  değişimi, yatakta A/B geçişi, tavan enkazı modeli ve küçük resmi, kırılan enkaz); geri çekilince yataklar söndü, konsolda
  hata yok. Doğu Avrupa rafının kayması, dinamit ve geri alma (komşuların yeniden giydirilmesi, kitin molozu) tek başına
  Baskin'de denendi. Bir bölge kiti varsayılandan en çok ~0,15 ms pahalı (kare süresi kayması içinde). Seslere yalnız
  sayılarla bakıldı; editörde ses kapalıydı, kimse dinlemedi.

