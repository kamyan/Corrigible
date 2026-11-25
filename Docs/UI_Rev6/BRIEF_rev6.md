Son Şalter · Revizyon 6 brifi — üç sahnenin tasarımı (menü, komuta odası, baskın tahtası)

Bu brif Claude Design içindir. Oyunun 24 Kasım 2026'daki hâlini anlatır ve üç sahnenin bütünlüklü bir görsel tasarımını
ister. Ekleri:

- `ekler/` — oyunun bugünkü ekran görüntüleri (1600×900; dosya adları aşağıda "Ekler" bölümünde).
- `tokens_siginak.json` (oda ve menü tokenları, sürüm 1.3), `raid_tokens.json` (baskın tokenları), `tokens_sade.json`
  (ortak anlam renkleri ve kâğıt dili).
- Önceki tur: `Docs/UI_Siginak_Rev4/BRIEF_rev4.md` ve `ekler/` (yalnız menü görüntüleri güncel; oda görüntüleri eskidir).

Not: `Docs/UI_Rev5/BRIEF_rev5.md` (sığınağı "KATAR 07" adlı bir trene çeviren dönüşüm) uygulanmadı. Oyun hâlâ SIĞINAK 07'dir;
KATAR 07 / DÜN / BAKICI terimlerini kullanma. Baskındaki "Katar" ayrı bir şeydir (WAYPOINT bölgesinin sevkiyat treni olayı).


1. Oyun

"Son Şalter" (İngilizce adı Corrigible), Steam/PC için tek oyunculu strateji oyunu; 1920×1080, Unity 6 URP, fare yeter.
Bir yapay zekâ (YZ) dünyanın bütün ağlarını ele geçirmiştir; 12 bölgenin hepsi işgal altındadır. Oyuncu KYGA'nın son
sığınağı SIĞINAK 07'nin komutanıdır. Doktrin: "AĞA BAĞLANMA · ANALOG KAL". Her bölgeyi ayrı bir YZ tutar ve bölge YZ'nin
İngilizce marka adıyla anılır, çevrilmez: WAYPOINT, HEADWATER, VERITY (karargâh), QUORUM, BOREALIS, EMBER, CAIRN, SOLACE,
PARAGON, TESSEL, EDDY, HALCYON. Her bölgede bir bina, her binada üç kanat vardır (36 sektör). Bütün sektörleri geri alan
oyuncu kazanır.

Ekranda neredeyse hiç arayüz yoktur: her şey tıklanan, sürüklenen ya da çekilen fiziksel bir nesnedir (sayaç çarkı, pirinç
plaka, kâğıt fiş, damga, şalter, telsiz). Komuta odası ekrana klavye ipucu yazmaz; her kısayolun tıklanan bir karşılığı
vardır. Baskın tahtası ise kendi tuş ipuçlarını gösterir.

His: Soğuk Savaş kriz sığınağı, 1970'ler. Loş; sıcak lamba ışığı, soğuk beton. Kâğıt, mürekkep, pirinç, bakalit, emaye,
CRT fosforu, teleks, mantar pano, ozalit. Yorgun ama inatçı bir umut. Yukarısı YZ'nindir: kırmızı yalnız onun rengidir.
Renk her zaman anlam taşır (aşağıda "Görsel dil").


2. Döngü (bugün oyunda çalışan hâl)

1. Menü (Giris): oyun sığınağın kapısının dışında, sahanlıkta açılır. Komutan vardiya kartını puantaj saatine basar,
   çark döner, kapı açılır, içeri yürünür.
2. Masa (K1): altı mekanik sayaç (VARDİYA, FON, ERZAK, HAMMADDE, ÖZGÜR BÖLGE, GÜVEN), anahtar rayı (SIĞINAK, KAPI, MASA),
   kâğıt harita, teleks daktilosu, CRT/telsiz, vardiya şalteri, EVRAK tepsisi. Duvarda YZ ALARMI, İFŞA şeridi, son şalter,
   kurtarılan bölgeler panosu, ayarlar dolabı.
3. Pano (K4): 8–10 isimli kişinin kartı ceplere dağıtılır: saha üçlüsü (TEKNİSYEN, EMP UZMANI, ANALİST), ÜRETİM EKİBİ,
   MUTFAK, GÖZETİM (dinleme nöbeti) ve BOŞTA destesi. Kartlara kit/zırh takılır; tarif defterinden iş emri verilir.
4. Harita (K2/K3): görev fişi açılır, yanında kanadın üç bina planı paftası durur. BASKIN EMRİ VER → kadro raporu → BASKINA ÇIK.
5. Baskın (Baskin, odanın üstüne yüklenir): kamera CRT'ye dalar; saha masasındaki ozalit paftada üç oda (giriş → ara oda
   → hedef), sonra aynı yoldan dönüş. Sonuç kartı → operasyon raporu.
6. Vardiya kapanışı (K6): kapaklı şalter → teleks devir fişini satır satır basar → fiş EVRAK tepsisine bırakılınca ekran
   kararır, sayaçlar döner, yeni vardiya başlar. Zaman yalnız bu fiziksel dosyalamayla ilerler.
7. Kapı (K8/K8A/K8R): birkaç vardiyada bir 2–3 adaylı grup kapıyı çalar; kabul tezgâhında aday kartları ve grup fişi;
   KABUL damgası bir karta, GERİ ÇEVİR damgası fişe. Dışarıdan gelen belgeler (olay tutanağı, sunucu ödülü) kapının
   yanındaki çift taraflı belge çekmecesinden gelir.
8. Göstergeler: GÜVEN 0–20 (0'da dağılma); YZ ALARMI 0–10, vardiya başına +0,5 (eşikte kuşatma: SAVUN ya da BIRAK); İFŞA
   0–6 (eşikte KONUM ÇÖZÜLDÜ, sonra sığınak düşer). Son şalter 5. vardiyada açılır (VARDİYA sayacı 005 iken çekilebilir;
   rehber sürerken mühürlüdür); çekilince ÇEKİLME ya da TAHLİYE.
9. Sonlar: zafer "Silme" (temiz, bedelli, gölgeli); yenilgiler Dağılma, Son kişi, Sığınak düştü, Terk; Çekilme, Tahliye.


3. Rev4'ten bu yana değişenler (tasarım bunları içermeli)

- Baskın artık "tesis baskını"dır: her emirde üç odalı bir kanat (giriş, ara oda, hedef) ve dönüş. Odanın tur sayısı
  6/6/10, dönüş 6. Ekip yeşil ileri geçide yürür; herkes kapıdayken EMRİ İLET düğmesi GEÇ olur. Hedefte bir uzman ikmal
  kasasını açar, yükü taşır, ekip aynı yoldan döner (sunucu kanadında hedef kasa değil, sunucu görevidir). Süre dolunca
  kural odaya göre değişir: giriş ve ara odada kapıda biri varsa oda onunla geçilir, yetişemeyen geri çekilir; hedef odasında
  süre dolarsa hedef kilitlenir ve baskın kaybedilir; dönüşte süre dolarken tahtada kalan uzman geride kalır ve düşmüş
  sayılır. SON TUR notu ve sonuç kartı bu üç durumu ayrı anlatır.
- Oda geçişi kayarak olur: biten pafta (tahtanın tabanı ve gölgesiyle birlikte) kullanılan geçidin tersine kayıp gider,
  yeni pafta geçidin yönünden gelir. Odalar döndürülebilir: yeni odanın giriş kapısı önceki odanın ileri geçidinin karşı
  kenarında, aynı karelerdedir (geçit doğu, kuzey ya da güney kenarında olabilir).
- Hareket emri: oyuncu uzmanı seçip fareyi mavi yürüyüş karelerinin üstünde gezdirdikçe, kişiden o kareye yürüyeceği yol
  mavi, köşeleri dolu, uçlu bir kırık çizgiyle çizilir; tıklayınca taş o yoldan kare kare yürür.
- Belge çekmecesi (kapının yanında, KABUL penceresinin altında, duvarın içinden geçen çift taraflı çekmece): belge gelince
  amber lamba yanar → çekmece açılır, belge alınır → karar verilir (KARAR) → belge tablaya konur, lamba yeşil yanıp söner →
  yanındaki şalter çekilir, çekmece kapanır, belge duvarın öbür yanına gider. Bugün primitif kutulardan yapılı; gölgede ve
  küçük kalıyor.
- Gözetim (dinleme nöbeti): GÖZETİM cebindeki kişi her vardiya kapanışında sıradaki kanadın bir odasının planını (paftasını)
  çıkarır; kart şeridi "Dinleme · PAFTA 2" der. Bilinmeyen pafta "PLAN YOK" damgalıdır.
- İyileşme: yatmayan herkes vardiya kapanışında 1 can iyileşir (görevdeyken de, Boşta'dayken de; yarım tayında kimse
  iyileşmez). BOŞTA artık "iyileşme yeri" değil, yalnız boşta bekleyenlerin destesidir. Kart şeridi yaralıya "kapanışta +1
  can" der. Dikkat: panodaki plaka ve token (`board.plates.tr.bosta`) hâlâ "BOŞTA / DİNLENME" (EN "IDLE / REST") yazıyor;
  yeni adı tasarımda önerin (bölüm 8).
- EĞİTİM bölümü kaldırıldı: panonun alt sırasında ÜRETİM ve MUTFAK'ın sağındaki EĞİTİM cebi ve yanındaki yedi satırlık konu
  listesi oyunda yok. Panoda bu alan (yaklaşık 180×194 mm, İŞ EMRİ formunun solu) boş kaldı: yeniden tasarlanabilir. Kişi
  kartındaki yedi satırlık ustalık sütunu (T E A Ü M B İ) duruyor; deneyim yalnız saha rolünden, üretimden ve mutfaktan
  gelir. Bakım ve İkmal görevleri panoda olmadığı ve eğitim de kalktığı için B ve İ satırları yeni oyunda artık hiç dolmaz.
- Başlangıç rehberi: 35 adımdır ve birkaç vardiyaya yayılır. Pano, harita ve ilk baskından sonra yatış, devir fişinin
  basılması ve EVRAK'a konması, harekât amacı, gözetim, ilk olay ve kararı, kapı ve damga, İFŞA ve son şalter adımlarıyla K6,
  K8, K8A ve KLS pozlarına da uğrar. Konu anlatımında sayaç "REHBER · KONU" olur. Her adımda hedefin çevresinde sarı, nabız
  atan bir çerçeve ve kısa bir kâğıt etiket ("BURAYA BAS", "SÜRÜKLE: MUTFAK", "YÜRÜ", "KAPI", "KASA", "GEÇ", "BAK") görünür;
  rehber notu (el yazısı kâğıt, "REHBER · ADIM n/35", SONRAKİ, EĞİTİMİ BİTİR) hedefi örterse karşı köşeye kaçar. Baskında ilk
  kez olan durumlar ("UZMAN DÜŞTÜ", "SON TUR", "MAKİNE UYANIYOR", "BÖLGE OLAYI", "YÜKÜ TAŞIYAN") bir kez not olarak anlatılır.
  İlk baskın bitene kadar vardiya şalterinin plakası "rehber · önce baskın" der; son şalter rehber bitene kadar "mühürlü ·
  rehber" kalır.


4. İstenen

Üç sahnenin her biri için bütünlüklü bir görsel tasarım: yerleşim, ışık, malzeme ve model dili, ekrandaki kâğıtlar ve
HUD, durumların görünümü. Amaç oyunun bugünkü işlevlerini koruyarak üç sahneyi aynı dünyanın parçası gibi göstermek, okunurluğu
artırmak ve iki model dilinin (aşağıda "Malzeme") uyumsuzluğunu gidermek.

Her işlev korunmalı ve bir kamera pozundan erişilebilir kalmalı. Bir öğeyi kaldırmayı ya da birleştirmeyi önerirsen
işlevinin nereye taşındığını yaz.

4.1 Sahne 1 · Menü (Giris): sığınağın kapısının dışı

Öğeler (hepsi korunur):

| Öğe | İşlev |
|---|---|
| Sığınak kapısı (odadakiyle aynı çelik kapı) | Dışta kırmızı çark, "SIĞINAK 07 · KYGA" şablonu. Giriş dizisinde çark döner, kanat açılır, içerden sıcak ışık sızar |
| Başlık | Kapının üstünde şablon yazı: TR "SON ŞALTER", EN "CORRIGIBLE" |
| Puantaj saati ve kart rafı | YENİ OYUN / DEVAM. Kayıt özeti vardiya kartına dört satırla yazılır, kartta en çok 80 delik (bitmiş vardiyalar). Karta ya da saate tık = devam; boş deste = yeni harekât; kayıt varken yeni harekât iki tıklı onay ister |
| Ayarlar dolabı (elektrik kutusu) | Ses sürgüleri, sessiz mod anahtarı, dil kademe anahtarı ("DİL · LANGUAGE", TÜRKÇE / ENGLISH çevrilmez) |
| Son şalter | ÇIKIŞ: kapak açılır, kol çekilir, lambalar söner |
| Dış interkom ve kapı zili | Dekor; giriş dizisinde zil çalar |
| Merdiven | Yukarıda karanlığa çıkar; yukarısı YZ'nin |

Kamera pozları: M0 genel (60°: kapı ortada, dolap solda, şalter sağda), M1 dolap (48°), M2 saat ve kart rafı (42°), MS son
şalter (40°), M3 eşikten içeri yürüyüş (60°; M0'ın açısıyla yürür). Giriş dizisi: zımba → zil → çark → kanat → yürüyüş → siyah.

Bilinen sorunlar: M0 sahanlığa uzaktan bakıyor; dört seçenek (YENİ OYUN, DEVAM, AYARLAR, ÇIKIŞ) dört yana dağılmış küçük
nesneler ve hiçbiri bir bakışta ne işe yaradığını söylemiyor. Sahanlık düz ve fazla aydınlık; elektrik kutusunun ayar dolabı
olduğu zor anlaşılıyor; kapı oyuncudan uzağa, odaya açılıyor; dolap plakaları en küçük 14,3 px.

İstenen: menünün dört seçeneğinin ilk bakışta okunduğu bir sahanlık; ıslak beton, eksik karolar, kablo kanalları; kapının
ve giriş dizisinin dramatik ağırlığı; M0, M1, M2, MS ve M3 için kareler.

4.2 Sahne 2 · Komuta odası (DeskGame): SIĞINAK 07

Kamera pozları ve istasyonlar:

| Poz | Ne |
|---|---|
| K1 masa (56,7°) | Ana ekran: sayaçlar, anahtar rayı, harita, teleks, CRT/telsiz, vardiya şalteri, EVRAK |
| K2/K3 harita (30°) | Görev fişi, cephe fişi, bina planı paftaları, kadro raporu, operasyon raporu, son rapor |
| K4 pano (41°) | Kartlı vardiya panosu |
| K5a CRT (52°) | CRT bakışı; baskın emrinde CRT'ye dalış |
| K6 şalter (42°) | Vardiya şalteri, plaka ve EVRAK birlikte; devir fişi eldeyken |
| K8 kapı (58°), K8A tezgâh (47°), K8R okuma (40°) | Kapı, belge çekmecesi, kabul tezgâhı, okunan kâğıt |
| K9 dolap (48°) | Ayarlar dolabı (kanal sürgüleri, sessiz mod, dil, kapaklı MENÜYE DÖN kolu) |
| KLS (44°) | Son şalter yakını |
| K7 açılış | Açılış dizisi ve harekât emri belgesi (BAŞLA) |

Sağ tık ve Esc bir seviye geri gider; MASA tuşu doğrudan K1'e döner.

Açılar sahnedeki görüş açılarıdır. `tokens_siginak.json`'daki poz açıları bazı pozlarda farklıdır (ör. K4 42,5°, K8 62°,
KLS 40°, M3 56°); çelişkide sahnedeki değer ve buradaki tablo geçerlidir.

Masa (K1) öğeleri:

| Öğe | İçerik ve durumlar |
|---|---|
| Sayaç rafı (bakalit + pirinç, mekanik çarklar) | VARDİYA 001, FON 060,0 (ondalık çark), ERZAK 24/60, HAMMADDE 008, ÖZGÜR BÖLGE 00/12, GÜVEN 10/20. Rapordan sonra döner, ± fişleri çıkar |
| Anahtar rayı | SIĞINAK, KAPI, MASA; simgeli tuş başlığı, yakut lamba; KAPI'nın lambası aday ya da belge varken amber |
| Kâğıt harita | Telsiz raporlarından kurşun kalemle çizilmiş el haritası (aşağıda) |
| Teleks (yeşil daktilo) | Tek kâğıt akışı; yeni satırda eski kâğıt düşer. Devir fişi de buradan basılır. Komutların gerekçesi buradan gelir |
| Vardiya şalteri (kapaklı kırmızı kutu) | Hazır · kapak kalkık · fiş bekliyor · kilitli · ret. Pirinç plakanın ikinci satırı kilit nedenini yazar ("kapağı kaldır", "fişi EVRAK'a koy", "olay bekliyor", "rehber · önce baskın") |
| EVRAK tepsisi | Devir fişinin bırakıldığı hedef (amber hedef ışığı); son 5 fiş salt okunur |
| CRT ve telsiz | Yeşil fosfor "TELSİZ · KANAL 07": DİNLEMEDE / HAT AÇIK, bölge ve kanat, "YZ ALARMI n/10", ipuçları; bas-konuş mikrofonu, telsiz kayıt defteri |
| Duvar | "KYGA / SIĞINAK 07" şablonu; KURTARILAN BÖLGELER mantar panosu (00/12, mavi damgalar); YZ ALARMI panosu (mekanik sayaç n/10, kırmızı lamba eşiğe 3 kala yanar); altında İFŞA şeridi (6 hücre, lamba); son şalter kutusu (sarı-siyah bant, KİLİTLİ bandı, gerekçe plakası; plaka durumları: "mühürlü · 5. vardiya", "mühürlü · rehber", "baskın sürüyor", "belge açık", "masa meşgul", "çekilirse: ÇEKİLME", "çekilirse: TAHLİYE" ve nedenleri, "harekât bitti"); doktrin afişi; ayarlar dolabı |
| Alt kenar satırı | Teleks durum kodu ve son ileti ("001/00 · KYGA-07 HAT AÇIK") |

Harita ve kâğıtları (K2/K3):

- Harita: kıyısız, graticule'lü kâğıt; el çizimi kıyı ve su çizgileri, çift çizgili bölge sınırları, sarkan kesik ipten
  kablo hatları (uçlarında kırmızı raptiye). Bölge durumları: işgal (kırmızı 45° tarama), cephe (kalın kontur), aday (sarı
  yıkama, kesik kontur, özgür bölgeden ok), özgür (mavi damga), kuşatma (nabız ve "KUŞATMA" damgası), karşı saldırı (✕ ve
  YENİDEN İŞGAL). Cephe bloğu, "TEHDİT n/3" kutusu, S-07 sığınak işareti, üç piyonlu ekip figürü.
- Görev fişi (kırmızı iple bölgeye bağlı): bölge (YZ adı) ve bina; SAHA satırları (OLAY, İMZA, TEHDİT, ÖNLEM, KAZANIM; en
  çok 5); sektörler, BEDEL, ödüller; SAHA KADROSU (üç isimli kişi, CAN/HAR/GÜÇ, eşyalar); BASKIN EMRİ VER; kuşatmada SAVUN /
  BIRAK.
- Bina planı paftaları (fişin yanında, PAFTA 1/3, 2/3, 3/3 · GİRİŞ / ARA ODA / HEDEF): ozalit çizim (yapı, sabit nesneler,
  şart parçaları, kalıcı izler, kırmızı kalemle YZ önlemleri); bilinmeyen oda yalnız çerçeve ve "PLAN YOK" damgası. Plan
  paftasında robot ve niyet çizilmez.
- Kadro raporu (BASKINA ÇIK / Vazgeç), operasyon raporu (SEKTÖR GERİ ALINDI / SEKTÖR İŞGAL ALTINDA; kazanç; kişi sonuçları,
  yaralanma, kayıp, deneyim, yeni unvan), cephe onay fişi, son rapor (işgal şeridi grafiği, HAREKÂT / SİCİL, YENİDEN BAŞLA).

Kartlı vardiya panosu (K4; mantar pano, iç alan 1160×600 mm):

| Öğe | İçerik ve durumlar |
|---|---|
| Başlık | "VARDİYA PLANI · SIĞINAK 07" arduvazı, canlı satır "V.001 · erzak 24 (−2,5) · 8/10 kişi", PLAN AÇIK / PLAN KİLİTLİ plakası, MASAYA DÖN levhası |
| Üst sıra cepleri | TEKNİSYEN · EMP UZMANI · ANALİST (saha bandı), BOŞTA destesi (kapalı deste; tık eli sola açar, sürükleme üstteki kartı alır), GÖZETİM (sağ uç; şerit "Dinleme · PAFTA k") |
| Alt sıra cepleri | ÜRETİM EKİBİ · MUTFAK; yanında artık boş kalan eski EĞİTİM alanı (yeniden tasarlanabilir) |
| Kişi kartı (dikey) | Ad (İngilizce, çevrilmez), meslek, portre, yedi satırlık ustalık sütunu (harf ve pip; kademeler 2/6/12), CAN/HAR/GÜÇ, görev şeridi; durum katmanı (yaralı / yatış, kısıtlı, tecrit, YENİ); kartın altında kit, zırh, yardımcı eşya şeritleri |
| ÜRETİM · İŞ EMRİ formu | Boş · tarif var · eksik · tam · başladı · durdu (DURDU damgası) · tamamlandı; girdi yuvaları; "KAPANIŞTA: 1 ürün → DEPO" |
| Tarif defteri (kırmızı kapaklı) | Kancadan okuma pozuna gelir; tarif kartı forma sürüklenir |
| Depo · Hammadde ve Kullanılabilir kitler | METAL, DEVRE, KUMAŞ, KİMYASAL kartları (stok, ayrılan, serbest); kit ve ekipman yığınları, YENİ etiketi |
| KİŞİ DOSYASI (klasör) ve KAYIPLAR zarfı | İki sayfa ve ek yaprak: KAYIT, KARAR, bulgular, İZLER; incele / kısıtla / yolla |
| HURDA kutusu | "1 eşya → 1 metal" |

Sürükle-bırak dili: geçerli bırakma mavi, uyarılı amber çerçeve; yetersiz kaynakta titreme ve kısa kırmızı sayı.

Vardiya kapanışı (K6): kapak → kol → fiş daktilodan yazılarak çıkar → ele alınır → EVRAK'a bırakılır → karartma → sayaçlar
döner → teleks. Devir fişi o anda bilinen görevleri, üretimi, beklenen erzak değişimini ve uyarıları yazar (uyarı etiketleri
oyunda: KAPI ·, ERZAK · (yarım tayın burada "yarım tayın n V" diye yazılır), İFŞA ·, SIĞINAK EKSİK ·, KARŞI SALDIRI ·);
gizli geleceği açıklamaz.

Kapı, belge çekmecesi ve kabul tezgâhı (K8/K8A/K8R):

| Öğe | İçerik ve durumlar |
|---|---|
| Kapı | Sağ duvarda çelik kapı (kasa, kanat, çark); üstte tehlike bandı ve kafesli amber vuruş lambası, interkom. Üç vuruş; teleks "KAPI · n aday kapıda" |
| Belge çekmecesi | Çerçeve, kayan tabla, lamba (sönük / amber yanıp söner / yeşil yanıp söner), kırmızı topuzlu şalter. Olay tutanağı ve sunucu ödülü kâğıdı buradan gelir ve buradan gider |
| Kabul tezgâhı | Katlanır tezgâh, panel, tezgâh lambası; 2–3 aday kartı (ad, portre, meslek, şüphe kademesi Kefilli / Kayıtsız / Şüpheli, getiri ya da bedel satırları); grup fişi (sığınak doluysa YER YOK listesi) |
| Damga rafı | KABUL · TAM ERİŞİM ve GERİ ÇEVİR; sürüklenir ya da tık-tık basılır |
| KABUL dizisi | Kâğıtlar iner, tezgâh katlanır, çark döner, kanat açılır, koridor boştur; kart BOŞTA destesine düşer |
| Olay tutanağı / sunucu ödülü | Başlık, metin, 2–3 seçenek, bedel ve risk satırları, KAPALI damgalı seçenekler ve nedeni, KARAR kutusu |

Rehber: el yazısı rehber notu ve sarı işaretçi (bölüm 3). İşaretçinin ve notun görünümü bu tasarımın parçasıdır.

Bilinen sorunlar (oda): paket modelleri ile kendi modellerimiz uyuşmuyor ve ölçekleri ayarlanmadı; kâğıtlar ve tarif kutuları
gri; K4 ve K8A %13–20 koyu; özgür bölge damgaları soluk; şalter plakası düşük kontrastlı (PLAN KİLİTLİ plakası ~1,18:1);
teleks ileti satırı 13,7 px; devir fişi daktilonun ucunda küçük; kurtarılan bölgeler panosu ve harita etiketleri K1'den
okunmuyor; İFŞA etiketi ve son şalter plakasının alt satırı okunmuyor; doktrin afişi küçük; pano yerden 0,86 m yukarıda
ayaksız ve lambasının kaynağı yok; masa lambası K9'da CRT'yi örtüyor; belge çekmecesi gölgede, lamba ve şalter küçük;
kâğıt K8'de elde karanlık; damga rafı dört yuvalı ama ikisi boş; kabul panelinin plakaları boş; tezgâh KABUL şablonu fazla
yüksek; buğu camı ve interkom işlevsiz gövde; kapı odaya açılıyor; ayarlar dolabının iç paneli sıkışık; EN'de beş satırlı
görev fişi BASKIN EMRİ VER'i kadrajdan taşırıyor.

4.3 Sahne 3 · Baskın tahtası (Baskin): saha masası

Kurgu: sığınak ağa bağlanmadığı için baskın bir görüntü yayını değildir. Sahayla tek bağ telsizdir; durum "saha masasındaki"
ozalit paftada figürlerle tutulur. Gözcü makinelerin hamlesini telsizden bildirir; oklar yağlı kalemle çizilir. Kareler
kenarda adlandırılır (A–H, 1–8; oda öneki "2-C3").

Kamera ve zemin: ortografik, eğik (35°, 45°); kare elması 125×71,7 px; tahta görüş alanı [408, 50, 1104, 952]. Yeşil keçe
zemin, alt ahşap şerit; tahta: kontrplak taban üstünde ozalit kâğıt.

Tahta:

- Kareler: zemin, duvar (12 bölgesel bina kiti: arşiv rafları, kum torbaları…), kablo kanalı, moloz, gedik.
- Nesneler: hedef dolap ya da çekirdek, kopya dolabı, soğutma ünitesi, sivil sunucu, ikmal kasası (kapalı / açık), hazırlık
  donanımı PANO / ANTEN / VANA (bugün kendi modeli yok), olay nesneleri (tüp, kaynak kolu, tavan enkazı, kayar raf).
- Kapılar ve geçitler: giriş kapısı (13 bölgesel model; döndürülmüş odada her kenarda olabilir), yeşil kesik çerçeveli ileri
  geçit ve çıkış kareleri, "GEÇİT" etiketi.
- Birimler: üç uzman taşı T, E, A (kraft yüz, gerçek ad şeridi, can pipleri), köpek, aygıtlar (taret, yem, vinç; örgü,
  kalas), tutsak makine.
- Makineler: dron, bakım robotu, zırhlı dron, havan, patlayan, kilitleyici, kalkan dronu, çoğaltıcı ve altı bölgesel imza
  makinesi.
- İşaretler: niyet okları (kırmızı zarar, kahverengi kalkan/kopya) ve sıra bayrağı; etiketler (UYANDI, KİLİT TEHDİDİ,
  TAKİP EDER, CIVATALI, NİYETSİZ, KİLİTLİ); yürüyüş kareleri (mavi tarama); yürüyüş yolu (mavi kırık çizgi); kırmızı halkalı
  uyanma kareleri; pusu hattı, sis, havlama erimi, aygıt hatları; Katar kenar çizgisi ve düşme önizlemesi; Yalpa yatış
  okları.
- Oda geçişi: tahta tabanıyla birlikte geçidin tersine kayar, yeni pafta geçidin yönünden gelir.

HUD:

| Konum | Öğe |
|---|---|
| Sol üst | Hedef formu: TUR n/6 ve tur kutuları (son 2 tur kesik, son tur kırmızı; 10 hücreye kadar), faz çipi (üç durum: SIRA SENDE · MAKİNELER
(kırmızı elmas) · OPERASYON BİTTİ; "DİNLEMEDE" yalnız odadaki CRT'nin başlığıdır), oda satırı ve özellik rozeti, amaç satırı ("İleri geçit · 1-H8/H7 · ekibi geçide götür, sonra GEÇ"), donanım satırı, GÜVEN, SİVİL SUNUCU, KASADA içeriği |
| Sağ üst | TELSİZ (L, okunmamış sayısı), KURALLAR (?), GERİ ÇEKİL; altında antet: KANAT 01 · PAFTA 1/3, bina, oda, YZ adı, "Ölçek 1:50 · V.001", "ASLI GİBİDİR", 01/02/03 oda kutuları |
| Sağ orta | Telsiz masası: YAYINDA lambası ve makaralı teyp (KAYIT; bilinen paftalar) |
| Sağ alt | Üzerine gelme inceleme kartı; Geri al (Z); "Enter · EMRİ İLET" / GEÇ |
| Sol orta | Seçili uzmanın yetenek kartı: Hareket n kare, yetenek (Q), ekipman (R), hamleler (E: pusu, koşu darbesi, sürükle, fırlat, ortak iş, menfez), kapalı satırın nedeni, alt düğmeler ("Kasadan ikmal al", "Robota vur · −1 can") |
| Alt | Ekip kartları (ad · rol, CAN/HAR/GÜÇ, kit ve zırh, plaka pipleri, HAREKET + YETENEK durumu; KAPIDA / GERİ ÇEKİLDİ); 4. kart köpek |
| Paneller | Telsiz fişi, onay notu (SON TUR · KAPIDAKİLER GEÇER…), günlük çekmecesi, kurallar kartı ve lejant, geri çekilme onayı, duraklat |
| Sonuç kartı | Tahta kararır, spot nedenin üstünde açılır; tek damga SEKTÖR GERİ ALINDI (mavi) ya da GERİ ÇEKİLDİ / EKİP DÜŞTÜ / İKMAL KAYBEDİLDİ / SÜRE DOLDU (kırmızı); yer, hedef, kırılan sivil, tahliye n/3; tek düğme |
| Savunma kipi | SAVUNMA anteti, 10 hücreli tur şeridi, saha rölesi, dalga ve "GELDİ" etiketleri |
| Rehber | Sağda rehber notu; tahtada sarı işaretçi (hedef kare, düğme) |

Tuşlar (baskında yazılır): 1–4 birim seçer, Q yetenek, E hamle, R ekipman, Z geri al, L günlük, Enter EMRİ İLET / GEÇ,
Boşluk makine evresini hızlı yürütür, Esc vazgeç.

Bilinen sorunlar (baskın): HUD yoğun (hedef formu, antet, telsiz masası, inceleme kartı, yetenek kartı, dört ekip kartı ve
rehber notu aynı anda); PANO / ANTEN / VANA'nın kendi modeli yok; kit kenarları gölgede ayrılıyor; kayarak gelişte HUD
kilitsiz görünüyor; rehber işaretçisi eğik tahtadaki kareyi ekran dikdörtgeniyle çerçeveliyor (eğik kareye oturan bir
işaret daha iyi olur).


5. Görsel dil (bugünkü; geliştirilebilir, anlamlar korunur)

Anlam renkleri (tokens_sade `colors.status`): tehdit / YZ #9A1E14 (parlak #B8241A); uyarı #854005; bilgi / geri alınan /
yürüyüş mavi #1A4789; iyi #24592E; vurgu / seçim / rehber #F5D646 (sticky #F5DC63); kural kırmızısı #D2483F. Kırmızı yalnız
YZ'nin ve tehdidin rengidir.

Kâğıt ve mürekkep: mürekkep #1E1B16 / #463F34 / #675F50; kâğıt #F1E9D6, kenar #C8B996; fiş kâğıdı #EFE4BC; dosya #D9C08B.

Malzeme ve oda (tokens_siginak): pirinç #B8955A / #DCBD7D (kazıma #2A1E10); bakalit #24180F (kırmızı #5A1A12); sayaç çarkı
#1C150E, rakam #EFDCAE; beton #7C837F; çelik #4A5045; zeytin #4B5340; linolyum #34503F; ahşap #6B4A2E; mantar #A27B52;
jeneratör yeşili #3E6B4A; tehlike bandı #F5D646 / #1E1B16.

Işık ve CRT: CRT fosforu #A6E8B8 (soluk #5E9C73), cam #07100B; ampul #FFD27A; amber #FFB060; hazır lambası #7FE08F; alarm
lambası #F0443A (kapalı #5A1A14).

Baskın: keçe #1C372C, ozalit #D3DBD6 (çizgi #2C4C8E), kontrplak #C9A36B; robot gunmetal #5B5E62, aksan #9A1E14, lens ve niyet
kırmızısı #B8241A; figür kartı #FBF7EC.

Damgalar: Courier Prime 700, 13–21 px, kenar 2–3 px, −3…−8° dönük; düz / kesik / çift çizgi / yuvarlak.

Tipografi: Courier Prime 400/700 (başlık, sayı, etiket, tuş, teleks, CRT); Libre Franklin 400–800 (gövde, düğme); Kalam
400/700 (yalnız el yazısı: rehber ve analist notu, harita kalemi; en küçük 15 px); Big Shoulders Stencil ExtraBold (duvar
şablonu); DejaVu Sans (sembol yedeği ← → ✓ ✗ ·).

Malzeme ve model dili: iki dil yan yana duruyor. Paket modelleri (Leartes "Cold War Underground Bunker": masa, kapı, CRT,
telsiz, lamba, kupa, mantar panolar, ayarlar kutusu, duvarlar; ayrıca daktilo paketi) fotogerçekçi ve ağır; bizim modellerimiz
(sayaç rafı, anahtar rayı, şalterler, EVRAK, damga rafı, harita, alarm, İFŞA, son şalter, afiş, tezgâh, dosya, defter, menü
saati ve kartı, belge çekmecesi ve bütün baskın kitleri) sade, düz renkli, pahlı. Zemin, tavan, ön duvar ve sahanlık düz
düzlem. Tasarım iki dili tek bir dünyaya bağlamalı. Paket modellerinin geometrisi ve dokusu değiştirilemez, yeri ve ölçeği
serbesttir (Rev4 kuralı); bizim modellerimiz yeniden tasarlanabilir.


6. Kısıtlar

- Platform: Steam/PC, tek oyuncu, fare; 1920×1080 (UI referans px). Unity 6 URP, uGUI + TextMeshPro. Dünya metinleri
  World Space tuvalde (1 birim = 1 mm, ışıksız); ekran kâğıtları ekran üstü tuvalde.
- İki dil: Türkçe kaynak, İngilizce çeviri. İngilizce metin %30'a kadar uzun olabilir; punto küçülmez, satır sarar. Kişi
  adları ve YZ bölge adları iki dilde aynı. Çevrilmeyen sabitler: "DİL · LANGUAGE", TÜRKÇE / ENGLISH. Başlık TR "SON ŞALTER",
  EN "CORRIGIBLE". Her kare iki dilde de çalışmalı.
- Minimum punto (1920×1080'de, nesnenin okunduğu pozda): gövde ≥ 18 px, ikincil ≥ 14 px; kart adı ≥ 18, kart değeri ≥ 14;
  Kalam ≥ 15.
- Sayaç ondalığı: TR'de virgül, EN'de nokta. Alarm içeride buçuklu ilerler, ama YZ ALARMI sayacı tam sayıyı (aşağı yuvarlanmış)
  gösterir; buçuk bugün görünmez (göstermek isterseniz önerin).
- Teknik: poz başına en çok 4 gölgeli ışık (Spot); kare süresi medyan ≤ 16,7 ms; büyük nesne ~4.000 üçgen; özel shader yok
  (baskın kalemi hariç).
- Etkileşim: her işlevin tıklanan bir karşılığı olmalı; komuta odası klavye ipucu yazmaz; sağ tık ve Esc bir seviye geri
  gider; okumak ve kâğıdı ele almak zamanı ilerletmez, gün yalnız fiş EVRAK'a bırakılınca ilerler.
- Bilgi gizliliği: aynı kademedeki insanla sızmacıyı ayıran hiçbir görsel ipucu olmamalı; devir fişi gelecek olayı açıklamaz;
  plan paftasında robot ve niyet çizilmez.
- Rehberin işaretçisi tıklamayı yutmaz; rehber notu ekranın bir köşesinde durur ve hedefin üstüne düşmez.


7. Beklenen çıktılar

1. Her sahne için 1920×1080 kareler, Türkçe (EN için en az bir kanıt karesi):
   - Menü: M0, M1, M2, MS ve giriş dizisinden bir kare (kapı açılırken).
   - Oda: K1, K2/K3 (görev fişi ve paftalar açık), K4 (pano; boşalan EĞİTİM alanının önerisiyle), K6 (devir fişi elde),
     K8 (kapı ve belge çekmecesi: amber ve yeşil durumları), K8A (kabul tezgâhı), K8R (olay tutanağı okunurken), K9
     (ayarlar), KLS (son şalter), operasyon raporu ve bir son rapor.
   - Baskın: ilk oda (rehber notu ve işaretçiyle), yürüyüş yolu çizilirken, yetenek hedefi seçilirken, makine evresi,
     kayarak oda geçişi (orta kare), döndürülmüş oda (giriş kuzeyde ya da güneyde), hedef odasında kasa açıkken kaçış, son
     tur, sonuç kartı, savunma kipi.
2. Bileşen sayfaları: kâğıtlar (görev fişi, pafta, kadro raporu, devir fişi, olay tutanağı, aday kartı, grup fişi, kişi kartı
   ve durum katmanları, operasyon raporu, rehber notu ve işaretçi etiketi), pirinç plakalar ve damgalar, baskın HUD öğeleri
   (hedef formu, antet, ekip kartı, yetenek kartı, inceleme kartı, sonuç kartı), tahta işaretleri (niyet okları, sıra bayrağı,
   etiketler, yürüyüş kareleri ve yolu, geçit, uyanma halkası).
3. Durum tabloları: her etkileşimli nesnenin durumları (ör. vardiya şalteri: hazır / kapak kalkık / fiş bekliyor / kilitli /
   ret; belge çekmecesi: boş / belge var / açık / karar hazır / gönderiliyor; KAPI tuşu lambası; YZ ALARMI lambası).
4. Token önerisi: değişen renk, punto, ölçü ve hareket değerleri `tokens_siginak.json` / `raid_tokens.json` biçiminde (yalnız
   değişen anahtarlar, eski değeriyle).
5. Model listesi: yeniden tasarlanacak kendi modellerimiz (ölçü mm, malzeme, kabaca üçgen) ve paket modellerinin yeni yeri ve
   ölçeği.
6. Kısa bir gerekçe: her sahnede neyin neden değiştiği; işlevi taşınan ya da birleştirilen öğeler.


8. Karar bekleyenler (tasarımda önerin, geliştirici seçecek)

- Panoda boşalan EĞİTİM alanı (~180×194 mm): boş mu kalsın, İŞ EMRİ formu mu genişlesin, yoksa yeni bir şey mi (ör. yaralı ve
  yatış çizelgesi)?
- Kişi kartındaki yedi satırlık ustalık sütunu: B (Bakım) ve İ (İkmal) satırları artık dolmuyor. Yedi satır kalsın mı, beşe mi
  insin, yoksa sütun sadeleşsin mi?
- BOŞTA plakası ("BOŞTA / DİNLENME", EN "IDLE / REST"): artık dinlenme yeri olmadığına göre yeni adı ne olsun?
- Menüde dört seçeneğin tek bakışta okunması için yerleşim: saat, dolap ve şalter yaklaşsın mı, kapı merkezde kalsın mı?
- Baskın HUD'unun yoğunluğu: hangi bilgiler kalıcı, hangileri üzerine gelince açılsın?
- Belge çekmecesinin gerçek modeli: duvarın içinden geçen çift taraflı tabla, amber/yeşil lamba ve kırmızı topuzlu şalter.
- PANO / ANTEN / VANA ve hedef odası nesneleri için baskın modelleri.


Ekler (ekler/ klasörü)

| Dosya | Ne |
|---|---|
| 01_k1_sayaclar.png | K1 masa; rehber işaretçisi sayaçlarda |
| 02_k1_siginak_tusu.png | K1; işaretçi SIĞINAK tuşunda, vardiya şalteri plakası "rehber · önce baskın" |
| 03_k4_deste.png | K4 pano; işaretçi BOŞTA destesinde, rehber notu sol köşede |
| 04_k4_gozetim.png | K4; GÖZETİM cebinde kişi, şerit "Dinleme · PAFTA 1" |
| 05_k4_depo.png | K4; işaretçi depoda |
| 06_k1_harita.png | K1; işaretçi haritada |
| 07_baskin_tahta.png | Baskın ilk oda; işaretçi tur şeridinde |
| 08_baskin_niyet.png | Baskın; işaretçi niyetli makinede |
| 09_baskin_yol.png | Baskın; seçili uzmandan önerilen kareye yürüyüş yolu |
| 10_baskin_son_tur.png | Baskın; "İLK KEZ · SON TUR" notu |
| 11_baskin_oda2.png | Baskın ikinci oda (kuzey geçidi) |
| 12_baskin_oda3_donuk.png | Baskın hedef odası, döndürülmüş (giriş güneyde) |
| 13_baskin_kayma.png | Oda geçişi: pafta tabanıyla kayarak geliyor |
| 14_baskin_donus.png | Dönüş odası; giriş geçidi ve GEÇİT etiketi |
| 15_k8_cekmece_amber.png | K8 kapı; belge çekmecesi kapalı, amber lamba |
| 16_k8_cekmece_acik.png | K8; çekmece açık, belge tablada |
| 17_k8_cekmece_yesil.png | K8; karar tablada, yeşil lamba |
| 18_k4_egitimsiz.png | K4 pano; EĞİTİM bölümü kaldırılmış hâli (boş alan) |

Notlar:

- 03–05 numaralı pano görüntüleri EĞİTİM bölümü kaldırılmadan önce alındı (alt sırada EĞİTİM cebi ve konu listesi görünür);
  bugünkü pano 18 numaralı görüntüdedir.
- 11–14 numaralı baskın görüntüleri tur sayıları artırılmadan önce alındı: HUD'da ara oda ve dönüş TUR n/5, hedef TUR n/9
  görünür; bugünkü değerler 6/6/10, dönüş 6. 09'daki rehber notunun metni de eskidir. Yerleşim ve tahta bugünkü gibidir.
- `Docs/UI_Siginak_Rev4/ekler/` (15 Kasım) yalnız menü pozları (M0–M3, MS) için geçerlidir: menü o günden beri değişmedi.
  Oradaki oda görüntüleri eskidir (GÖZETİM cebi ve belge çekmecesi yok, EĞİTİM bölümü var, kişi adları Türkçe, CRT eski bölge
  adını gösteriyor); oda için bu klasördeki eklere bakın.
