Son Şalter · Revizyon 4 brifi — yeni modellerle oda ve menü tasarım çalışması

Bu brif Claude Design içindir. Ekleri: `ekler/*.png` (oyunun bugünkü görüntüleri), `tokens_siginak.json` (bugünkü
tokenlar), Revizyon 3 paketinin `BRIEF_rev3.md`, `EK_11-05.md` ve `README.md` dosyaları (önceki turun dili ve kısıtları).

Oyun

"Son Şalter" (İngilizce adı Corrigible), Steam/PC için tek oyunculu strateji oyunu, 1920×1080, Unity 6 URP. Yapay zekâ
(YZ) dünyanın bütün ağlarını ele geçirmiş; oyuncu KYGA'nın son sığınağı SIĞINAK 07'nin komutanı. Doktrin: "AĞA
BAĞLANMA · ANALOG KAL". Ekranda neredeyse hiç arayüz yok: her şey tıklanan, sürüklenen fiziksel bir nesnedir. Fare yeter.

His: Soğuk Savaş kriz sığınağı. Loş; sıcak lamba ışığı, soğuk beton, kâğıt, mürekkep, pirinç, bakalit, emaye, tüp.
Yorgun ama inatçı bir umut. Yukarısı YZ'nin.

Döngü (bugün oyunda çalışan hâl):

- Oyun kapının dışında, sahanlıkta açılır (menü, Giris): komutan vardiya kartını puantaj saatine basar, kapı açılır, içeri
  girilir. Sahanlıkta ayarlar kutusu ve kapaklı SON ŞALTER (oyundan çıkış) da var.
- Komuta odası (DeskGame) masası: haritadan cephe bölgesinin sıradaki sektörüne baskın emri; altı sayaç (VARDİYA, FON,
  ERZAK, HAMMADDE, ÖZGÜR BÖLGE, GÜVEN); anahtar rayı (SIĞINAK, KAPI, MASA).
- Kartlı vardiya panosu (K4): 8 isimli kişinin kartları ceplere dağıtılır (Teknisyen, EMP uzmanı, Analist; Üretim,
  Mutfak, Eğitim; Boşta). Depo, iş emri, tarif defteri, hurda kutusu, kişi dosyası.
- Kapıda kabul (K8, K8A, K8R): birkaç vardiyada bir 2–3 adaylı grup gelir; tezgâhta aday kartları ve grup fişi; KABUL
  damgası bir karta, GERİ ÇEVİR damgası fişe. Sığınak doluysa yollanacak kişi fişten seçilir.
- Sığınak olayları: kapanışta bir olay çıkarsa EVRAK tepsisine bir olay tutanağı gelir (başlık, metin, 2–3 seçenek,
  bedel ve risk satırları, KAPALI damgalı seçenekler, KARAR kutusu); karar verilmeden fiş basılmaz.
- İFŞA şeridi (YZ ALARMI panelinin altında, 6 hücre): sızmacının izleri ve sabotajlar doldurur; eşikte "KONUM ÇÖZÜLDÜ"
  uyarısı, sonra sığınak düşer.
- SON ŞALTER (odada, arka duvarda): 5. vardiyadan sonra açılır; kapaklı, iki adımlı; çekilince ÇEKİLME ya da TAHLİYE
  emri çıkar ve harekât biter.
- Vardiya şalteri (masanın sağında, kapaklı): kol inince devir fişi DAKTİLODAN (teleks) yazılarak çıkar; oyuncu fişi
  EVRAK tepsisine koyunca vardiya biter.
- Teleks (masanın solunda, daktilo): gelişmeleri kâğıda basar; kâğıt rulosu arkada.
- CRT ve telsiz (sağdaki küçük masa): durum satırları, YZ alarmı, ipuçları.
- Ayarlar dolabı (odada sağ üstte, elektrik kutusu): kanal sürgüleri, ses, dil, MENÜYE DÖN.
- Son raporlar: zafer (temiz, bedelli, gölgeli), sığınak düştü, terk, dağılma, çekilme, tahliye.

Bugünkü hâl: ne değişti

Revizyon 3'te odadaki ve menüdeki bütün nesneler, bu paketin kendi modelleriydi (198 model; gerçek ölçek, pahlı,
düşük-orta poligonlu; manifest ve TMP yuvalarıyla). O turdan sonra iki şey değişti:

1. Oynanış genişledi (yukarıdaki aday grubu, olaylar, İFŞA, son şalter, yeni sonlar). Bunlar için odaya yeni yüzeyler
   eklendi (İFŞA şeridi, son şalter kutusu, olay tutanağı, aday kartları, grup fişi), ama hepsi var olan dilden türetilip
   olabildiğince küçük tutuldu; bir tasarımcı gözünden geçmedi.
2. Geliştirici odanın birçok nesnesini hazır, gerçekçi model paketleriyle değiştirdi ve bunlar oyuna bağlandı:

| Alan | Yeni model (kaynak) | Not |
|---|---|---|
| Duvarlar | SM_Wall4x4 ×6, SM_Wall15 (Leartes Cold War Underground Bunker) | Zemin, tavan ve ön duvar hâlâ düz düzlem |
| Masa | SM_Table_2 (Leartes) | Tabla üstü 0,76 m; nesneler aynı yükseklikte |
| Masa lambası | SM_Lamp (Leartes) | Gerçek ışık abajurda; K9'da CRT'nin bir kısmını örtüyor |
| Kupa | SM_Cup (Leartes) | Yalnız dekor |
| Teleks | Typewriter_FBX_Unity (daktilo paketi) | Kâğıt silindirden çıkıyor; devir fişi de buradan basılıyor; rulo arkada görünmüyor |
| CRT | SM_TV_2 (Leartes) | Ekran yazısı tüpe oturtulmuş eski Rev3 ekran parçası |
| Telsiz | SM_Radio + SM_Device_1 (Leartes) | Ahize kordonu ekranı kestiği için kordonsuz kopya; ibre/kanal animasyonu yok |
| Kapı | SM_Door (Leartes), kasa/kanat/çark olarak bölündü | Kanat ODAYA açılıyor; gözetleme kapağı yok; buğu camı ve interkom gövdeleri işlevsiz |
| Kapı lambası | SM_SmallLight (Leartes), kafes + cam | Vuruşta amber yanar |
| Kurtarılan bölgeler panosu | SM_Board (Leartes, mantar pano) | Kartlar ve raptiyeler yüzde |
| Vardiya panosu | SM_Board (Leartes) + eski panodan taşınan karatahta, iş emri, hurda kutusu, pirinç etiket bantları | Yerden 0,86 m yukarıda ayaksız duruyor; pano lambasının görünür kaynağı yok |
| Ayarlar dolabı | SM_PowerBox1 (Leartes), gövde/kapak/kilit olarak bölündü | Rev3 iç paneli 0,70 ölçekle kutuya sığdırıldı; AYARLAR plakası küçük |
| Menü (Giris) | Aynı kapı ve aynı elektrik kutusu | Sahanlık zemini/duvarları hâlâ düz düzlem (beyaz fayans, açık beton) |

Hâlâ Revizyon 3 modeli olanlar: sayaç rafı, anahtar rayı, vardiya şalteri, EVRAK tepsisi, damga rafı, kalem, silgi,
talaş, harita ve dayanağı, YZ ALARMI paneli, İFŞA şeridi, son şalter kutusu, afiş, borular, kanal, bağlantı kutusu, duvar
şablonu, tavan ampulü, CRT masası, kabul istasyonu (tezgâh, panel, tezgâh lambası, damga rafı), kişi dosyası, KAYIPLAR,
tarif defteri, koridor, tırabzan, koridor lambaları, kapı üstü tehlike bandı, iç interkom; menüde puantaj saati ve kart
rafı, son şalter, dış interkom, kapı zili, başlık.

Sonuç: oda iki ayrı dilde konuşuyor. Paket modelleri fotogerçekçi, yoğun dokulu ve yüksek poligonlu (daktilo 16 250,
mantar panolar 10 101'er, lamba 7 413, telsiz 7 006 üçgen); Revizyon 3 modelleri sade, düz renkli ve pahlı. Ölçek ve
yerleşim de birbirine göre ayarlanmadı: modeller eski nesnelerin yerine "oturtuldu".

Serbestlik (bu turun en önemli kuralı)

- Revizyon 3'ten gelen her model ve tasarım kararında TAMAMEN ÖZGÜRSÜN: yeniden tasarla, ölçeğini ya da yerini değiştir,
  başka bir modelle birleştir, kaldır, yeni model öner. Malzeme, renk, yıpranma ve ışık dili de serbest. Amaç Revizyon 3
  modellerini yeni paket modelleriyle aynı dünyaya getirmek (gerekirse paket modellerinin diline yaklaştırarak).
- Yeni paket modellerinin KONUMUNU, DÖNÜŞÜNÜ ve ÖLÇEĞİNİ değiştirebilirsin. Geometrilerini ve dokularını değiştirme; bir
  paket modelinin malzeme değerine (renk tonu, pürüzsüzlük, emission) dokunmak ya da bir paket modelini tamamen çıkarmak
  istersen bunu gerekçeli bir öneri olarak ayrı yaz.
- Odanın ve sahanlığın yerleşimi serbest: mobilyalar, duvar nesneleri, istasyonlar yer değiştirebilir; kamera pozları
  yeniden kurulabilir. Değişmeyen tek şey aşağıdaki işlev listesi: her işlev odada bulunmalı ve bir pozdan erişilebilmeli.
- Kapsam yalnız DeskGame (komuta odası, kapı ve koridoru dahil) ve Giris (sahanlık). Baskın sahnesi bu turda yok.

Görülen sorunlar

Genel
- İki model dili yan yana: paket modellerinin yanında Rev3 nesneleri oyuncak gibi, Rev3 nesnelerinin yanında paket
  modelleri ağır ve kalabalık duruyor.
- Paket modelleri Rev3 üçgen bütçesinin çok üstünde (bütçe büyük nesne 4 000). LOD grupları var ama bazıları kapalı.
- Zemin, tavan, ön duvar ve sahanlığın yüzeyleri düz düzlem; duvarlar paket modeli olunca bu fark büyüdü.
- Yeni oynanış yüzeyleri (İFŞA şeridi, son şalter, olay tutanağı, aday kartları, grup fişi) küçük ve K1'den zor okunuyor;
  İFŞA etiketi ve son şalter plakasının alt satırı K1'de okunmuyor.

Komuta odası (K1 ve diğer pozlar)
- Afiş son şalterin üstüne küçültülerek taşındı (K1'de ~80 px); doktrin afişi artık odanın simgesi gibi okunmuyor.
- Vardiya panosu yerden 0,86 m yukarıda ayaksız, havada; pano lambasının görünür kaynağı yok.
- Masa lambası ayarlar dolabı pozunda (K9) CRT'nin bir kısmını örtüyor.
- Teleks daktilosunun ileti satırı K1'de 13,7 px (sınır 14). Devir fişi daktilonun sağ ucunda küçük; K1'den okunmuyor,
  okumak için tıklanıyor. Kâğıt rulosu masada tutucusuz yatıyor ve K1'den görünmüyor.
- Telsiz kapı kanadının süpürmesine yakın; telsiz ve cihaz bu yüzden 14 cm geri kaydırıldı, CRT ile yan yana değiyor.
- Kapı kanadı odaya açılıyor (yeni kapının menteşesi oda tarafında). Gözetleme kapağı, buğu camı ve iç interkom artık
  işlevsiz gövde.
- Kabul istasyonu: paneldeki talimat tutucusu, ESKİ cebi, GİDEN ve BULGU plakaları boş; damga rafı dört yuvalı, ikisi boş
  (eski kabul yolundan kaldı).
- Hurda kutusu yeni panonun çerçevesine çok yakın (5 mm).
- Ayarlar dolabının içi sıkışık; Rev3 paneli 0,70 ölçekle kutuya zor sığıyor.

Menü (Giris) — bu turun öncelikli sorunu
- Menü bugünkü hâliyle kullanımı ve anlaması zor. Açılış kamerası (M0) sahanlığa uzaktan bakıyor: dört ana seçenek
  (YENİ OYUN, KAYITLI OYUNA DEVAM, AYARLAR, ÇIKIŞ) sahanlığın dört bir yanına dağılmış küçük nesneler (puantaj saati ve
  kart rafı, kapı, elektrik kutusu, son şalter) ve hiçbiri bir bakışta ne işe yaradığını söylemiyor. Oyuncu ne yapacağını
  bulmak için nesnelerin üzerine tek tek gelmek zorunda.
- Sahanlık düz ve fazla aydınlık; yeni kapı ve elektrik kutusu dışında her şey düz yüzey.
- Elektrik kutusu menüde ayarlar olduğunu zor anlatıyor (küçük AYARLAR plakası eklendi).
- Kapının üstündeki SON ŞALTER başlığı yeni kasaya göre 11 cm indirildi; kapı odaya, oyuncudan uzağa açılıyor.

Ekteki görüntülerde ayrıca görülenler
- 01, 07, 16 (K1): sol paneldeki yazılar, kurtarılan bölgeler panosu ve son şalter plakasının alt satırı okunmuyor;
  daktilodaki devir fişi çok küçük; "KONUM ÇÖZÜLDÜ" gibi kritik bir uyarı K1'de yalnız CRT'de küçük bir satır.
- 03 (K4): BOŞTA cebindeki kişiler tek bir deste olarak üst üste duruyor (başlık "8/10 kişi", görünen 6 kart); kaç
  kişinin boşta olduğu okunmuyor. Başlık altındaki küçük satır ve kartların altındaki YARDIMCI yuva yazıları çok soluk.
- 04 (dosya): sağ sayfa büyük ölçüde boş.
- 05 (K5a): paket modellerinin üstünde İngilizce marka/etiket yazıları var (TactComm, P.A. TUNING, HERTZ, OptiScreen);
  oyunun dil kuralıyla çelişiyor.
- 06 (K6): EVRAK tepsisinin etiketini kupa örtüyor.
- 08 (K8): kapı pozu çok karanlık; ön planda masa lambası ve son şalter kutusu kadraja giriyor.
- 10, 11 (K8A, K8R): kabul panelinin pirinç plakalarının çoğu boş; okuma pozunda kart kadrajın sağına kaymış.
- 13, 14 (K9): kadraj dolabı kesiyor; açık kapağın iç yüzü düz gri bir levha.
- 18 (EN): haritada WESTERN EUROPE ve EASTERN EUROPE etiketleri çakışıyor.
- 20–25 (menü): kapının üstünde "SON ŞALTER" başlığı, şalterin plakasında "ANA ŞALTER": adlandırma tutarsız; ANA ŞALTER
  plakası İngilizcede de Türkçe kalıyor; menü dolabında MENÜYE DÖN'ün yerinde boş plaka.
- 30, 31 (plan ve genel görünüm): masa arka duvara dayalı, odanın ön yarısı boş zemin; K1 kamerasının arkasındaki duvar
  düz, bitmemiş bir yüzey.

Görev

Bilgi, kurallar, sayılar ve etkileşimler aynı kalır. Yeni modelleri esas alarak:

1. Komuta odasını ve sahanlığı tek bir görsel dile getir. Paket modellerini yerleştir, ölçekle ve gerekirse çıkarmayı
   öner; Rev3 nesnelerini bu dile göre yeniden tasarla ya da değiştir.
2. Odanın yerleşimini yeniden düşün: K1'den okunması gerekenler (sayaçlar, harita, teleks, şalter, EVRAK, İFŞA, son
   şalter, afiş, kurtarılan bölgeler panosu) bir bakışta okunmalı. İstasyonlar (pano, kapı/kabul, CRT/telsiz, ayarlar)
   fiziksel olarak mantıklı ve kamera pozlarıyla ulaşılabilir olmalı.
3. Yeni oynanış yüzeylerini tasarla: İFŞA göstergesi, son şalter, olay tutanağı (ve onun EVRAK tepsisindeki bekleyişi),
   aday kartları ve grup fişi, kabul tezgâhı (yalnız iki damga: KABUL, GERİ ÇEVİR), devir fişinin daktilodan çıkışı.
4. Menüyü (Giris) baştan, anlaşılırlık için tasarla. Bu turun en önemli maddesi:
   - Açılış pozu yakın olmalı: YENİ OYUN, KAYITLI OYUNA DEVAM, AYARLAR ve ÇIKIŞ aynı kadrajda, birbirine yakın ve bir
     bakışta okunur görünmeli. Bugünkü uzak genel bakış (M0) yerine kamera bu dört seçeneğin toplandığı bir köşeye ya da
     panoya yaklaşır; sahanlığın atmosferi (kapı, merdiven, başlık) arkada ya da kadrajın kenarında kalabilir.
   - Her seçeneğin kendi fiziksel nesnesi kalır (analog dil: düğme ya da ekran menüsü yok), ama nesneler birbirine
     yakınlaştırılabilir, büyütülebilir, yeniden düzenlenebilir ya da yenileriyle değiştirilebilir. Her birinin üstünde ya
     da yanında, ne yaptığını söyleyen okunur bir plaka olsun (TR/EN): ör. YENİ VARDİYA / YENİ HAREKÂT, DEVAM, AYARLAR,
     ÇIKIŞ. Plakalar okunmak için üzerine gelmeyi beklemez.
   - Durumlar okunur olmalı: kayıt yokken DEVAM kapalı ve neden kapalı olduğu belli; kayıt varken yeni harekât eski kaydı
     sileceği için iki adımlı onay (bugün: kıskaç kalkar, eski kartta İPTAL EDİLİR fişi).
   - Seçimden sonraki hareket (kart saate basılır, kapı açılır, kamera eşikten geçer; ayarlar kutusu açılır; son şalter
     çekilir) aynı yakın pozdan başlar ve oyuncuyu kaybettirmez; ayarlardan dönünce kamera yine bu poza döner.
   - Bunun ardından sahanlığın geri kalanı: atmosfer, ışık, kapının dış yüzü, başlık, dış interkom, zil, merdiven ve
     içeri girme anı. Yeni kapı ve elektrik kutusu (paket modelleri) yer ve ölçek olarak taşınabilir.
5. Her poz için ışık tarifi ver. Yukarıdakilerden daha önemli bir sorun görürsen gerekçesiyle öner.

İşlevler (değişmez; her biri bir nesne ve bir pozla karşılanmalı)

Oda
- Altı sayaç (VARDİYA, FON, ERZAK, HAMMADDE, ÖZGÜR BÖLGE, GÜVEN); sayaç değişince yanında fark fişi.
- Harita (12 bölge, cephe, ekip figürü) ve harita pozu; baskın emri formu.
- Anahtar rayı: SIĞINAK (panoya), KAPI (kapıya), MASA (masaya) tuşları; dikkat fişi.
- Vardiya şalteri: kapak (1. tık) + kol (2. tık); plakada durum ("VARDİYAYI BİTİR", "olay bekliyor", kilit nedeni).
- Teleks: satır basar, kâğıt kopar; devir fişini basar.
- Devir fişi: daktilodan çıkar, ele alınır, EVRAK tepsisine sürüklenir; yanlış yere bırakılırsa geri döner.
- EVRAK tepsisi: devir fişinin hedefi; olay tutanağının bekleme yeri (dikkat ışığı); arşiv fişi ve emir belgesi.
- Olay tutanağı: elde okunur; seçenek satırına tık (1/2/3), KARAR kutusu; Esc ile tepsiye döner.
- YZ ALARMI paneli (0–10, eşikte karşı saldırı) ve İFŞA şeridi (0–6, eşikte KONUM lambası ve kırmızı ışık).
- SON ŞALTER: mühürlü (5. vardiyadan önce ve rehber sürerken), kapak + kol, plakada ÇEKİLME ya da TAHLİYE; ardından
  emir kâğıdı (vazgeçilebilir).
- Kurtarılan bölgeler panosu (0/12 ve bölge kartları).
- CRT: durum ekranı ve ipucu satırları; telsiz ışığı.
- Vardiya panosu (K4): kişi kartları, cepler, deste, depo (hammadde ve kit kartları), iş emri, tarif defteri, hurda kutusu,
  kişi dosyası (TECRİT, KISITLI, GÖREV YOK etiketleri), KAYIPLAR, MASAYA DÖN.
- Kapı: çark döner, kanat açılır; vuruş lambası; adaylar geldiğinde kapı bir kez vurulur.
- Kabul tezgâhı (K8A, K8R): 2–3 aday kartı, grup fişi, KABUL ve GERİ ÇEVİR damgaları, tezgâh lambası; dolu sığınakta fiş
  satırından kişi seçimi.
- Ayarlar dolabı (K9): kanal sürgüleri, ibreler, ses anahtarı, dil anahtarı, MENÜYE DÖN; kapak açılır.
- Son rapor kâğıdı (ekran üstü): damga, özet kutuları, grafik, kırılma anları, neden kutusu, YENİDEN BAŞLA.

Menü (dört ana seçenek; bugün hangi nesneyle karşılandıkları)
- YENİ OYUN: boş kartı puantaj saatine basmak (kart rafı, boş deste, kıskaç). Kayıt varken iki adımlı onay.
- KAYITLI OYUNA DEVAM: komutanın kendi kartını saate basmak (kartta V.xxx · özgür n/12 özeti). Kayıt yoksa kapalı.
- AYARLAR: elektrik kutusu (odadakiyle aynı içerik: kanal sürgüleri, ses, dil; menüde MENÜYE DÖN yok).
- ÇIKIŞ: SON ŞALTER (oyundan çıkış; kapak + kol, iki adım).
- Ayrıca: kapının açılması ve içeri yürüme (eşikte kararma), dış interkom, zil.

Kısıtlar

Motor
- Unity 6, URP. 3B sahne + uGUI + TextMeshPro (World Space ve Overlay Canvas). UI Toolkit yok.
- Özel shader yok (Shader Graph dahil): URP Lit / Simple Lit / Unlit, TMP shader'ları. Parlama yalnız emission; bloom ve
  post-processing'e dayanan etki yok. Oyuncu post-processing'i ve TAA'yı kapatabilir: görünüş ikisi kapalıyken de doğru.
- Yeni model önerirsen: gerçek ölçek (metre), Y yukarı; hareketli parçalar ayrı, pivotu dönme ekseninde; Revizyon 3'ün
  OBJ + manifest biçimi (boyut, pivot, parça aralığı, malzeme anahtarı, TMP yuvaları, çapalar, dünya yerleşimi). Bütçe:
  büyük nesne ≤ 4 000, küçük ≤ 800 üçgen. Paket modellerinin bütçesi aşması kabul; önerdiğin yeni modeller için değil.
- Dokuya kelime gömme: her yazı bir TMP yuvasıdır (TR/EN değişir). Rakam şeridi, ölçek çizgisi, süs işareti dokuda olabilir.

Işık
- Bir pozda en çok 4 gölgeli ışık; gölgeyi spot taşır (nokta ışığı yalnız dolgu, gölgesiz). 2 cm altı parçalar gölge
  atmaz. Kâğıtların albedosu ≤ 0,8 (lamba altında beyaz patlamaz). Gölge çözünürlüğü 2048.

Okunurluk ve dil
- 1920×1080'de gövde en az 18 px, ikincil en az 14 px görünür boyutta; dünya metinleri okundukları pozda ölçülür.
- Her metin Türkçe ve İngilizce; İngilizce %30'a kadar uzun, punto küçülmez, satır sarar. Türkçe karakterler her fontta.
  Yazı aileleri: Courier Prime (daktilo, başlık, sayı, etiket, damga, teleks, CRT), Libre Franklin (okuma, düğme), Kalam
  (yalnız el notu). Yeni font önerirsen gerekçe ver.
- Hiçbir durum yalnız renkle verilmez. Ayrılmış anlamlar: kırmızı = YZ ve tehdit; amber = dikkat/bekleyen iş; harfli
  ahşap = ekip. Neon, hologram, HUD çerçevesi, bilim kurgu görünümü yok.
- Klavye ipucu, ok, parlayan çerçeve, ekran balonu yok; yol gösteren nesnenin kendisidir (üzerine gelince 4 mm kalkar,
  400 ms sonra kâğıt etiket: başlık + tek satır). Geri dönülmez işler iki adımlıdır.

Oyun mantığı
- Kurallar, sayılar, kayıt biçimi ve komutlar değişmez. Görüntülerdeki sayılar örnektir.

Bugünkü yerleşim (Unity koordinatı: x sağ, y yukarı, z arka duvara doğru, metre; c = kutunun merkezi, s = boyutu)

Oda: x −2,2…2,2, z −1,6…2,3 (arka duvar z 2,40), y 0…2,5. Kapı sağ duvarda (x 2,2), ardında koridor ve merdiven.

| Nesne | c | s |
|---|---|---|
| Masa (SM_Table_2) | (0,01; 0,32; 1,24) | 2,11 × 0,96 × 0,94 |
| Sayaç rafı | (0; 0,48; 0,94) | 1,60 × 0,96 × 1,88 (masayla) |
| Anahtar rayı | (−0,04; 0,80; 0,90) | 0,97 × 0,08 × 0,16 |
| Vardiya şalteri | (0,77; 0,86; 0,92) | 0,33 × 0,19 × 0,34 |
| Teleks daktilosu | (−0,77; 0,92; 1,17) | 0,57 × 0,32 × 0,62 |
| Masa lambası | (0,75; 1,10; 1,53) | 0,40 × 0,68 × 0,41 |
| EVRAK tepsisi | (0,77; 0,80; 1,30) | 0,34 × 0,07 × 0,29 |
| Kupa | (0,79; 0,81; 1,09) | 0,16 × 0,11 × 0,12 |
| Harita dayanağı | (0; 0,78; 1,32) | masa üstünde |
| YZ ALARMI paneli | (0,15; 1,33; 2,24) | 0,50 × 0,34 × 0,13 |
| İFŞA şeridi | (0,15; 1,12; 2,24) | 0,51 × 0,37 × 0,13 |
| SON ŞALTER | (0,68; 1,19; 2,24) | 0,30 × 0,64 × 0,23 |
| Afiş | (0,68; 1,65; 2,29) | 0,19 × 0,24 |
| Duvar şablonu "KYGA / SIĞINAK 07" | (−0,05; 1,77; 2,30) | 0,84 × 0,15 |
| Kurtarılan bölgeler panosu | sol arka duvar, (−0,52; ~1,25; 2,30) civarı | mantar pano |
| Vardiya panosu (SM_Board) | (−1,60; 0,89; 1,69) | 0,86 × 1,77 × 0,95, köşede açılı |
| Tarif defteri / kişi dosyası / KAYIPLAR | panonun yanında, y ~1,1–1,25 | |
| CRT masası | (1,50; 0,44; 1,71) | 1,12 × 0,87 × 1,03 |
| CRT (SM_TV_2) | (1,28; 0,93; 1,84) | 0,45 × 0,34 × 0,43 |
| Telsiz + cihaz | (1,71; 0,98; 1,52) | 0,73 × 0,44 × 0,70 |
| Ayarlar dolabı (SM_PowerBox1) | (1,58; 1,49; 2,20) | 0,47 × 0,74 × 0,20 |
| Kapı (SM_Door) | (2,30; 0,99; 0,75) | kasa 0,26 × 1,99 × 1,20; kanat odaya 95° |
| Kabul istasyonu | (2,20; 0,98; −0,27) | tezgâh 0,15 × 0,58 × 0,84 (katlanır), panel, tezgâh lambası |
| Tavan ampulü | (0,12; 2,21; 1,70) | |
| Borular, kanal, bağlantı kutusu | arka ve yan duvarlarda | |

Pozlar (bugünkü değerler; yeniden kurulabilir)

| Poz | Ne | Kamera |
|---|---|---|
| K1 | Masa, ana poz | (0; 1,52; −0,15), +z'ye, dikey 56,7°, lens kaydırması −0,324 |
| Harita | Haritaya tepeden | (0; 2,75; 1,30) → (0; 0,76; 1,36), 30° |
| K4 | Vardiya panosu | panoya bağlı, 0,98 m önünden, 42,5° |
| K5a | CRT | ekranın 0,62 m önünden, 52° |
| K6 / K6a | Vardiya şalteri / vardiya anı | (0,40; 1,28; 0,42) → (0,665; 0,82; 1,08), 42° |
| K8 | Kapı | (0,15; 1,75; 0,55) → (2,2; 1,28; 1,0), 62° |
| K8A / K8R | Kabul tezgâhı / aday kartı okuma | (1,44; 1,69; −0,27) → (2,08; 1,0; −0,27), 47°; tezgâh üstünden 40° |
| K9 | Ayarlar dolabı | dolabın 1,1 m önünden, 48° |
| KLS | Son şalter | (0,30; 1,50; 1,30) → (0,68; 1,20; 2,24), 40° |
| M0 | Menü sahanlığı | (4,5; 1,52; 0,30) → (2,2; 1,25; 0,30), 60° |
| M1 / M2 / MS / M3 | Menü dolabı / saat / son şalter / eşik | tokens `cameras.*` |

Menü sahanlığı: kapı x 2,2'de dışarıdan görülür; solda elektrik kutusu (2,30; 1,34; −1,09), puantaj saati ve kart rafı
(~1,2; 0,86; −0,31), dış interkom ve zil kapının solunda; sağda SON ŞALTER (2,26; 1,27; 1,62); kapının üstünde başlık;
sağda merdiven ve tırabzan.

Ekler (ekler/ klasörü)

01_K1_masa · 02_harita · 03_K4_pano · 04_K4_kisi_dosyasi · 05_K5a_crt · 06_K6_salter · 07_K1_fis_daktiloda ·
08_K8_kapi_kapali · 09_K8_kapi_acik · 10_K8A_kabul_tezgahi · 11_K8R_aday_karti · 12_olay_tutanagi ·
13_K9_ayarlar_kapali · 14_K9_ayarlar_acik · 15_KLS_son_salter · 16_ifsa_uyari_K1 · 17_son_rapor · 18_K1_masa_EN ·
20_M0_sahanlik · 21_M2_saat · 22_M1_dolap_acik · 23_MS_son_salter · 24_kapi_acilirken · 25_M0_EN ·
30_oda_ustten · 31_oda_genel_arka · 32_giris_ustten
(Görüntülerdeki sayılar ve durumlar örnektir.)

İstenen çıktı (bölüm bölüm; her bölüm tek başına uygulanabilir)

| Bölüm | İçerik |
|---|---|
| A | Görsel dil: paket modelleriyle Rev3 nesnelerini birleştiren malzeme, renk, yıpranma ve ışık kuralları; ayrılmış renklerin yeni dildeki karşılığı |
| B | Oda yerleşim planı: tepeden plan ve iki kesit; her nesne için tablo (ad, kaynak: paket / Rev3 / yeni, karar: kalır / taşınır / ölçeklenir / yeniden tasarlanır / kaldırılır / yeni, Unity konum, dönüş, ölçek) |
| C | K1 ve harita ekranları 1920×1080 (TR + EN): K1'de okunması gerekenlerin hepsi okunur |
| D | Masa nesneleri: teleks daktilosu, kâğıt rulosu ve devir fişinin çıkışı, vardiya şalteri, EVRAK tepsisi ve olay tutanağının bekleyişi, anahtar rayı, sayaç rafı |
| E | Arka duvar: YZ ALARMI, İFŞA (normal, uyarı, eşik), SON ŞALTER (mühürlü, açık, ÇEKİLME/TAHLİYE), afiş, kurtarılan bölgeler panosu, duvar şablonu |
| F | Vardiya panosu (K4) ve kişi dosyası: panonun duruşu (ayak, duvar, lamba), kartlar, depo, hurda, defter |
| G | Kapı ve kabul (K8, K8A, K8R): kapının iç yüzü ve açılışı, vuruş lambası, tezgâh, aday kartları, grup fişi, iki damga, dolu sığınak hâli |
| H | CRT, telsiz ve ayarlar dolabı (K5a, K9): telsizin yeri ve kapıyla ilişkisi, dolabın içi |
| I | Menü (öncelikli): yeni yakın açılış pozu ve dört seçeneğin (YENİ OYUN, DEVAM, AYARLAR, ÇIKIŞ) düzeni; her seçeneğin bütün durumları (kayıt var / yok, yeni harekât onayı, üzerinde, basılı, kapalı) ayrı ekranlarda; seçimden sonraki geçişler; ardından sahanlığın atmosferi, ışığı, kapının dış yüzü, başlık, içeri girme anı (TR + EN) |
| J | Kâğıtlar: olay tutanağı, aday kartı, grup fişi, devir fişi, son rapor (yeni sonlar dahil) tek bir tutarlılık geçişinden |

Her ekranın yanında kısa uygulama notu: eski → yeni, nesne listesi (ad, ölçü, malzeme), etkileşimli öğeler ve durumları
(normal, üzerinde, tutuldu, hedef üstünde, bırakıldı, geçersiz, devre dışı, kilitli), hareket ve ses tarifi.

Ayrıca:
- Her poz için ışık tarifi (ışık, tür, konum, yön, renk, şiddet, menzil, gölge evet/hayır).
- Kamera pozları (konum, hedef ya da çapa, dikey açı, lens kaydırması).
- Tokenlar ekteki `tokens_siginak.json` biçiminde; yeni ya da değişen değerleri `_new_1_4`, `_changed_1_4`, `_was_1_4`
  listeleriyle işaretle.
- Yeni model önerdiysen Revizyon 3'ün OBJ + manifest biçiminde model kütüphanesi ve model sayfası.
- Kararsız kaldığın ya da geliştiricinin seçmesi gereken noktalar için "Açık sorular" listesi.
