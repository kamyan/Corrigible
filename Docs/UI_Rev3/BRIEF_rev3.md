Son Şalter · Tasarım revizyonu 3: bütün proje, daha iyi tasarım ve modeller (Claude Design brifi)

Kullanım: çizginin altındaki metni Claude Design'a yapıştır. İş büyük; Claude Design'dan bölüm bölüm teslim iste (A, B, C, D;
aşağıda "Teslim"). Şu dosyaları yükle (yollar depo köküne göre):

- `Docs/UI_Rev3/screens_now/` (bugünkü oyun, 2026-11-03): `01_giris_sahanlik.png`, `02_giris_saat.png`, `03_giris_dolap.png`,
  `04_masa_K1.png`, `05_dolap_K9.png`, `06_baskin_bina_kitleri.png`
- `Docs/UI_Siginak_Rev2/screens_unity_rev2/`: `S2.png`, `S2_EN.png`, `S3.png`, `S4a.png`, `S4b.png`, `S4c.png`, `S5a.png`,
  `S5b.png`, `S6a.png`, `S6b.png`, `S6c.png`, `S7a.png`, `S7b.png`, `S7d.png`, `S8a.png`, `S8b.png` (Revizyon 2 uygulandıktan
  sonraki oyun, 2026-10-31; `S1*` eski olduğu için yükleme, yerine `04_masa_K1.png`)
- Önceki turların stil sayfaları: `Docs/UI_Siginak/reference/screens/K8.jpg`, `Docs/UI_Siginak_Rev2/reference/screens/S9.jpg`
- Tokenlar: `Assets/Corrigible/Desk/Data/Siginak/tokens_siginak.json`, `Assets/Corrigible/Desk/Data/Sade/raid_tokens.json`
- three.js temel sahnesi (bugünkü geometrinin kaynağı): `Docs/UI_Siginak_Rev2/reference/scene_threejs_rev2/` klasörünün tamamı
  (`README.md`, `lib.js`, `room.js`, `door.js`, `diorama.js`, `raid.js`, `map2.js`, `states.js`)
- Kurallar ve bağlam (metin): `Docs/OYUN-TASARIMI.md`, `Docs/AYARLAR-DOLABI-VE-MENU.md`, `Docs/BASKIN-MEKANLARI.md`

---

Oyun

"Son Şalter" (İngilizce adı Corrigible), Steam/PC için tek oyunculu strateji oyunu, 1920×1080. Yapay zekâ (YZ) dünyanın bütün
ağlarını ele geçirmiş; oyuncu KYGA'nın Batı Avrupa'nın altındaki son sığınağı SIĞINAK 07'nin komutanı. Doktrin duvardaki
afişte: "AĞA BAĞLANMA · ANALOG KAL". Her şey fiziksel bir masa oyunu gibi oynanır, tamamen fareyle: kâğıt harita, mekanik
sayaçlar, teleks, sığınağın kesit maketi, tüplü telsiz göstergesi, anahtar rayı ve vardiyayı bitiren kapaklı şalter. Zaman
yalnız şalter indirilince bir vardiya ilerler. Sahayla tek bağ telsizdir: saha görülmez, gözcünün bildirdiği maketle kurulur.

Döngü: oyun sığınağın kapısının dışında açılır (menü sahnesi): komutan vardiya kartını puantaj saatine basar, kapı açılır,
içeri girer. Masada haritadan cephe bölgesinin sıradaki sektörüne baskın emri verilir (görev fişi, kadro: Teknisyen + iki
uzman) → kamera CRT'ye dalar → 8×8 taktik tahta, bölgenin binasında (ozalit pafta üstünde kontrplak maket; harfli ahşap uzman
piyonları; makinelerin hamlesi kırmızı okla önceden görünür; tur tur işleyen bir bina olayı; görev bitince bütün ekip
çıkıştan kaçmalı) → sonuç kartı → masaya dönüş, operasyon raporu → sığınakta hazırlık (DEPO, yaşam alanı, telsiz odası,
atölye, revir) → biri kapıyı çalar (23 ziyaretçi; gözetleme deliği, interkom, aç/açma; kapıda ölüm olabilir) → şalter →
vardiya raporu. Ayarlar odadaki ve menüdeki çelik bir dolapta (kollu sürgüler, şalter, dil kademe anahtarı).

Harekât: 12 bölge × 3 sektör; her bölgenin kendi binası, odaları, özelliği, imza makinesi ve olayı var. 36 sektörün hepsi
alınınca zafer. Kaybetme: sivil güven 0 (DAĞILMA), kapıyı silahlı birine (VURULDU) ya da YZ'nin kopyasına (SIZMA) açmak. YZ
alarmı 10'a varınca karşı saldırı son alınan sektörü geri alır.

His: Soğuk Savaş kriz sığınağı. Loş; sıcak lamba ışığı, soğuk beton, kâğıt, mürekkep, pirinç, bakalit, emaye, tüp. Yorgun ama
inatçı bir umut. Yukarısı YZ'nin.

Bugünkü hâl

Üç sahne var:
- Menü (`Giris`): kapının önündeki sahanlık. Kapının dış yüzü, ayarlar dolabı, kart rafı ve puantaj saati, dış interkom,
  sağda kapaklı "son şalter" (oyundan çıkış), yukarı çıkan merdiven, kapının üstünde "SON ŞALTER" / "CORRIGIBLE" şablonu.
  Bu sahne 2026-11-03'te tasarımcı gözü olmadan eklendi.
- Komuta odası (`DeskGame`): iki tasarım turundan geçti (K1–K8, Revizyon 2 S1–S9). Ayarlar dolabı arka duvarın sağına, CRT'nin
  üstüne yeni eklendi; ekrandaki AYARLAR düğmesi kaldırıldı. Anahtar rayı dört istasyon (SIĞINAK, GÖREVLER, KAPI, MASA).
- Baskın (`Baskin`): ozalit + kontrplak tahta, 12 bina kiti (duvar, kanal, gedik molozu dokuları), olay nesneleri, makineler
  ve piyonlar; kenarda tahtanın malzemesinden paneller.

Bütün 3D nesneler Unity'de ilkel parçalardan kuruldu (kutu, silindir, küre ve birkaç üretilmiş halka/silindir): kenar pahı
yok, çoğu nesne düz renkli. İki turun dili doğru, ama modeller oyuncak gibi duruyor; bu turun ağırlığı modellerde.

Ekteki görüntüler (sayılar örnektir; tasarım onları göstermeli, değiştirmemeli):

Dosya	Ekran
01_giris_sahanlik.png	Menü, sahanlık (M0): dolap, kart rafı + saat, interkom, kapı, son şalter, başlık
02_giris_saat.png	Menü, saat pozu (M2): komutanın kartı (V.001 · özgür 0/12), boş kart destesi ve kıskacı
03_giris_dolap.png	Menüdeki ayarlar dolabı açık
04_masa_K1.png	Komuta odası, başlangıç pozu; sağ üstte kapalı ayarlar dolabı
05_dolap_K9.png	Odadaki ayarlar dolabı açık (K9): dört kollu sürgü, SES anahtarı, dil kademe anahtarı, MENÜYE DÖN kolu
06_baskin_bina_kitleri.png	Aynı haritada 12 bina kiti + varsayılan (Kit önizleme sayfası)
S2 … S8	Revizyon 2'den sonra harita ve görev fişi, sığınak, kapı, vardiya raporu, baskın, sonuç ve operasyon raporu, son rapor

Korunacaklar (iyi çalışıyor)
- Analog dil ve doktrin; ekranda neredeyse hiç arayüz yok. Odada ve menüde klavye ipucu yazılmaz; her şey tıklanan fiziksel
  bir nesnedir: üzerine gelince nesne 4 mm kalkar, 400 ms sonra kâğıt etiket (başlık + tek satır) çıkar.
- Fiziksel etkileşimler: kapaklı şalter (1. tık kapak, 4 s içinde 2. tık kol), anahtar rayı, kapının çarkı, dolabın
  sürgüleri, kartı saate basmak, son şalter. Geri dönülmez işler iki adımlıdır.
- Kâğıt dili (görev fişi, kapı kâğıdı, raporlar, emir belgesi), durumların damgayla verilmesi.
- Haritanın tarama dili (işgal, cephe, aday, özgür), ekip figürü.
- Baskın: ozalit pafta + kontrplak maket, kenardaki kare adları (A–H, 1–8), harfli ahşap piyonlar, kırmızı niyet okları. Telsiz
  kurgusu bugünkü ölçüsünde (daha fazla vurgu istenmiyor).
- Ayrılmış işaretler, yalnız şu anlamda: kırmızı = YZ ve tehdit; mavi halka + çarpı = sivil; dik silindir + fan + sarı üçgen =
  soğutma ünitesi; turuncu omuz bandı = basınçlı tüp; el çarkı = kayar raf; harfli ahşap = ekip; kâğıtta harf/rakam = kare adı.
  Mavi geri alınmış, yeşil iyi, sarı seçili, amber lamba etkin. Hiçbir bina bunları süs olarak kullanamaz.

Görülen sorunlar

Genel
- Modeller: her şey kutu ve silindir; pah, kalınlık, vida, conta, yıpranma, malzeme ayrımı yok. Nesneler ışığı tutmuyor ve aynı
  ölçekteki gerçek eşyalara göre kaba duruyor (sayaç rafı, anahtar rayı, şalter, teleks, CRT, telsiz, raf, maket, kapı, dolap).
- İnce parçalar TAA açıkken titriyor (3 mm lamba zinciri ölçüldü, maketin 4 mm pirinç şeritleri hâlâ parıldıyor). Post-processing
  ve TAA oyuncunun açıp kapattığı seçenekler: görünüş ikisi kapalıyken de doğru olmalı.
- Gölgeli nokta ışıkları çok: Unity gölge atlasını küçültüyor; dolabın panelinde milimetrik parçalarda gölge pürüzü görüldü.

Menü (01–03)
- Sahanlık fazla aydınlık ve düz (beyaz fayans, açık beton); sığınağın dışında, YZ'nin dünyasına açılan bir merdiven boşluğu
  gibi değil, bir koridor gibi.
- Kapının dış yüzü çıplak; başlık şablonu betona yapıştırılmış tabela gibi.
- Puantaj saati düz bir kutu; kart rafı, kartın düzeni (başlık, özet, zımba ızgarası), boş kart destesi ve kıskacı tasarlanmadı.
- Son şalterin kapağı derin bir kutu; ANA ŞALTER plakası küçük. Dış interkom kara bir kutu.
- Kapının açılıp içeri yürünen an (kamera eşikten geçer, kararır) tasarlanmadı.

Ayarlar dolabı (03, 04, 05)
- Panel işlevsel ama genel: kasa düz kutu, plakaların yazısı küçük, ibreli göstergeler sade, lambalar minik, MENÜYE DÖN kolunun
  kapağı kaba bir kutu. Kanat açıkken iç ampul panelde parlak bir leke yapıyor.
- Odada K1'den görünüyor (sağ üst): kapalıyken ayarlar dolabı olduğu bir bakışta okunmalı.

Komuta odası (04, S2–S8)
- Revizyon 2'den açık kalanlar: oda kartında kişi işareti yok ("SV 1 · 2/2"); BOŞTA levhası gölgede; özet gözlerinin araları düz
  çizgi (noktalı olmalı); interkom için ikon yok; gözetleme deliği ve interkom dünyada tıklanmıyor (kâğıttan seçiliyor).
- Masa, raf ve CRT masası düz kutu mobilya; odanın harekât boyunca değiştiği az hissediliyor.

Baskın (06, S6)
- 12 bina kiti aynı haritada neredeyse aynı görünüyor (06): yalnız duvar dokusunun tonu değişiyor. Yapılmayanlar: duvar
  kabartması, bina anteti, sivil ekipman (siviller hep aynı krem kutu), kapılar. Kaynak kolunun kaidesi kolun yönünü göstermiyor.
- Makineler ve piyonlar basit; makine türleri renkten çok biçimle ayrılmalı (bugün tür işareti etikette).
- Panellerin malzemesi tahtayla bağlandı (Revizyon 2), ama 3D maketle 2D paneller arasındaki geçiş hâlâ sert.

Görev

Bilgi, kurallar, sayılar ve etkileşimler aynı kalır. İki turun dilini koruyarak:
1. Bütün 3D nesneleri modelle: gerçek ölçekte, pahlı, malzemesi ayrılmış, düşük-orta poligonlu; hareketli parçaları ayrı ve
   pivotu doğru yerde (aşağıdaki liste). Model kütüphanesi Unity'de doğrudan kurulabilmeli.
2. Menü sahnesini ve ayarlar dolabını tasarla (ilk kez tasarımcı gözünden geçiyor): atmosfer, ışık, kapının dış yüzü, saat ve
   kart, son şalter, dolabın kasası ve paneli, içeri girme anı.
3. 12 bina kitini birbirinden ayır (biçim, kabartma, sivil ekipman, kapı, antet; renk tek başına yetmez) ama ayrılmış
   işaretleri ve okunurluğu koru.
4. Odanın açık kalan maddelerini kapat, kâğıtlar ailesini (emir belgesi, görev fişi, kadro raporu, kapı kâğıdı, vardiya raporu,
   operasyon raporu, son rapor, eğitim notu) tek bir tutarlılık geçişinden geçir.
5. Her sahne için bir ışık tarifi ver (gölgeli nokta ışığı sayısı sınırlı, aşağıda).
Yukarıdakilerden daha önemli bir sorun görürsen gerekçesiyle öner.

Teslim (bölüm bölüm; her bölüm tek başına uygulanabilir)

Bölüm	Kod	Ekran / sayfa	Odak
A	R1	Menü sahanlığı (M0), 1920×1080	Atmosfer ve ışık; kapının dış yüzü; başlık; durumlar: kayıt yok / kayıt var / yeni harekât onayı (kıskaç kalkık, eski kartta İPTAL EDİLİR fişi) / kart saate basılıyor / kapı açık, içeriden sıcak ışık / son şalterin kapağı kalkık
A	R2	Saat pozu (M2)	Puantaj saati, kart rafı, kartın düzeni (KOMUTAN · SIĞINAK 07, V.nnn · özgür x/12, biten her vardiya için bir zımba deliği, en çok 80), boş kart destesi ve kıskacı
A	R3	Ayarlar dolabı	Kapalı (K1'den uzaktan ve K9'da yakından); açık (TR ve EN); sessiz mod; dil değişirken; menüdeki hâli (MENÜYE DÖN kolu yok). Kontrollerin durumları: normal, üzerinde, sürükleniyor, devre dışı
A	R4	İçeri girme anı	Kart → zil → kapı açılır → kamera eşikten geçer → kararır; ve DeskGame'de kısa dönüş (siyahtan ampul, masa)
B	R5	Komuta odası K1	İlk vardiya ve geç harekât yan yana; yenilenmiş modellerle bütün masa ve duvarlar
B	R6	Harita, görev fişi, kadro raporu	Revizyon 2'nin dili; yenilenmiş masa ve ekip figürü
B	R7	Sığınak maketi (K4)	Maketin modelleri; oda kartında kişi işareti; BOŞTA levhası
B	R8	CRT ve telsiz (K5a), dalış anı	Cihaz modelleri (CRT, ahşap telsiz, mikrofon, kayıt defteri)
B	R9	Kapı (K8)	Kapalı, açık, ölümcül; koridor; siluet ailesi; dünyada tıklanan gözetleme deliği ve interkom önerisi
B	R10	Kâğıtlar ailesi	Sekiz kâğıdın tutarlılık sayfası (başlık, damga, satır hiyerarşisi, TR/EN)
C	R11	Baskın tahtası, üç kontrast kit	Örneğin Batı Avrupa, Afrika, Çin kitleri; planlama ve makine evresi; olay nesneleri; kenar panelleri
C	R12	12 kit sayfası	Aynı haritada 12 kit + varsayılan, renkli ve gri tonlu kopya (gri tonda da ayrılmalı)
D	R13	Model sayfaları	Her nesne ailesi için ortografik önden/yandan/üstten + perspektif, ölçüler, pivot işaretleri, malzeme anahtarları
D	R14	Stil sayfası eki	Malzemeler (boya, pirinç, bakalit, emaye, beton, fayans, kâğıt, kontrplak), yıpranma, her sahnenin ışık tarifi

Model listesi (R13; ad, bugünkü nesne)
- Menü: kapı kasası ve kanadı (iki yüz), dış çark, gözetleme halkası, dış interkom, puantaj saati, kart rafı, vardiya kartı,
  boş kart destesi ve kıskacı, son şalter (bıçaklı, kapaklı), sahanlık lambaları, merdiven ve tırabzan, başlık şablonu.
- Ayarlar dolabı: kasa, kanat, mandal, iç panel, iç ampul ve kafesi, kollu dikey sürgü (yarık, ölçek, kol), ibreli gösterge,
  sayaç tekerleri, lamba ve çerçevesi, yarasa kollu anahtar, dil kademe anahtarı, kapaklı MENÜYE DÖN kolu, pirinç plakalar.
- Masa: masa ve ayakları, sayaç rafı (6 modül, FON'da virgül), anahtar rayı (4 tuş: başlık, mücevher, plaka, dikkat fişi),
  kapaklı vardiya şalteri, teleks ve kâğıdı, EVRAK tepsisi, masa lambası, kupa, kalem, silgi, kalem talaşı, damga rafı, ekip figürü.
- Duvarlar: KURTARILAN BÖLGELER mantar panosu ve kartları, YZ ALARMI panosu ve lambası, afiş, borular ve vana, kanal, bağlantı
  kutusu, tavan ampulü, tebeşir çentikleri.
- Raf ve maket: raf, sandıklar, bidon, umut bitkisi, kesit maketi (DEPO, dört oda, odaların çıktıları), oda kartları, özet panosu,
  BOŞTA tepsisi ve piyonlar.
- CRT masası: masa, CRT, ahşap telsiz (kanal skalası, SİNYAL ibresi), mikrofon, kayıt defteri.
- Kapı (içeriden): kasa, kanat, çark, vuruş lambası ve kafesi, gözetleme deliği ve kapağı, interkom, tehlike bandı; koridor,
  merdiven, iki lamba, ziyaretçi siluetleri (anne, yaşlı, silahlı, asker, yaralı, kopya, üç genel).
- Baskın: karo, duvar bloğu (12 kitin her biri için gövde, kabartma ve köprü), kayar raf ve el çarkı, kablo kanalı (göbek, kol,
  kenar), düğüm / çekirdek / kopya dolabı, ikmal sandığı, soğutma ünitesi, sivil (bölgeye göre ekipman), olay nesneleri (tüp,
  kaynak kolu ve kaidesi, tavan enkazı, gedik molozu), makineler (dron, bakım robotu, kilitleyici, havan robotu, kalkan dronu,
  zırhlı dron, bombacı, kopyalayıcı ve imza makineleri), uzman piyonları (Teknisyen, EMP uzmanı, Analist, Madenci, Sis uzmanı;
  normal, tur bitti, yaralı, plakalı, düşmüş), işaretler.

Hareketli parçalar ve pivotlar (kod bunları döndürür ya da kaydırır; modelde ayrı nesne, pivotu dönme ekseninde, adı aynı)
- Vardiya şalteri: `Cover hinge` (yerel x, 0 → 112°), `Lever` (yerel x, +35 → −35°); hazır lambası ve KİLİTLİ bandı ayrı.
- Anahtar tuşu: `Cap` (y, −6 / +4 mm), `Jewel` (malzeme değişir), dikkat fişi (z ekseninde sallanır).
- Sayaç: her çark ayrı bir yüzey; rakam dokusu 11 hücreli şerit (0–9 + ikinci 0).
- Kapı: `Hinge` (kanat, yerel y 0 → 95°, koridora), `Wheel` (yerel z, 1,25 tur; dış yüzde ikinci çark), gözetleme kapağı (y 50 mm
  kayar), vuruş lambası ve interkom noktası (malzeme + ışık).
- Ayarlar dolabı: `Leaf hinge` (yerel y; odada −95°, menüde −120°), `Latch` (yerel z 90°), sürgü kolu `Handle` (yerel y ±80 mm),
  `Needle` (yerel z ±50°), anahtar kolu (yerel x ±28°, kol panelden dışarı uzanır), `Knob` (yerel z ±40°), MENÜYE DÖN kolu
  (şalterle aynı: kapak + kol), iç ampul (malzeme + ışık).
- Puantaj saati: kart (rafından yarığa taşınır, yarığa 70 mm girer), `Plunger` (y −12 mm), `Clip` (yerel x −70°), kart
  devrilmesi (yerel x −14°), zımba delikleri (tek tek açılır).
- Son şalter: `Cover hinge` (yerel x 115°), `Lever` (yerel x ±55°).
- Baskın: duvar karonun komşularına göre parça açıp kapar (köprü D/K, kayar raf çarkı); kanal göbek + dört kol + dört kenar;
  piyonlar ve makineler karo karo yürür, uzman tahliye edilir; dinamitle yıkılan duvar zemine döner.

Kısıtlar

Motor ve modeller
- Unity 6, URP. 3D sahne + uGUI + TextMeshPro (World Space ve Overlay Canvas). UI Toolkit yok.
- Özel shader yok (Shader Graph dahil): URP Lit / Simple Lit / Unlit, TMP shader'ları. Parlama yalnız emissive; bloom'a,
  bulanıklığa ya da başka bir post-processing etkisine dayanma.
- Yeni: modeller artık ilkel parça olmak zorunda değil. Düşük-orta poligonlu mesh, gerçek ölçek (metre), pah. Bütçe: büyük
  nesne (kapı, dolap, masa, maket, saat, şalter) ≤ 4 000 üçgen; küçük nesne ≤ 800; baskın parçası (bir karede 64 kopyaya kadar)
  ≤ 400; makine ve piyon ≤ 1 500.
- Malzeme URP Lit değerleriyle (taban rengi, metalik, pürüzsüzlük, emissive); doku en çok 1024 px, mümkünse paylaşılan atlas;
  normal haritası kullanılabilir, kullanmasan da okunmalı. Renkleri tokenlarda tut (`materials`, `materialsExtra` biçimi).
- Kelime içeren her yazı TMP'dir (TR/EN değişir): dokuya kelime gömme; modelde yazı için yer (dikdörtgen, ölçü, yazı rolü) bırak.
  Rakam şeridi, ölçek çizgisi ve süs işaretleri dokuda olabilir.
- İnce parça ekranda en az 3 px (TAA). Uzaklık ölçüsü: masa pozunda 1 px ≈ 2 mm (≥ 6 mm), dolap pozunda 1 px ≈ 0,64 mm (≥ 2 mm).
  Bundan ince ayrıntıyı dokuya koy.
- Gölgeli nokta ışığı: bir pozda en çok 4. Dolap içi, kapı arkası gibi küçük ışıklar gölgesiz.

Okunurluk ve dil
- 1920×1080'de gövde en az 18 px, ikincil en az 14 px görünür boyutta; dünya metinleri okundukları pozda ölçülür.
- Her metin Türkçe ve İngilizce; İngilizce %30'a kadar uzun, punto küçülmez, satır sarar. Türkçe karakterler (Ğ Ü Ş İ Ö Ç ğ ü ş ı
  ö ç) her fontta. Yazı aileleri: Courier Prime (daktilo: başlık, sayı, etiket, damga, teleks, CRT), Libre Franklin (okuma,
  düğme), Kalam (yalnız el notu). Yeni font önerirsen gerekçe ver.
- Hiçbir durum yalnız renkle verilmez; kelime, damga, işaret ya da desen de taşır. Renk anlam taşır (yukarıdaki ayrılmış işaretler).
- Neon, hologram, HUD çerçevesi, bilim kurgu görünümü yok.

Kamera ve yerleşim (Unity koordinatı: x sağ, y yukarı, z arka duvara doğru, metre)
- Oda: x −2,2…2,2, z −1,6…2,3, y 0…2,5. Masa x ±0,95, z 0,75–1,89, üstü 0,76. Raf solda (x −2,14…−0,86, z 1,18–2,26). CRT
  masası sağda (x 0,94–2,06, z 1,21–2,23). Kapı sağ duvarda (x 2,2, z 0,32–1,18, h 2,0), ardında koridor (x 2,2–4,6). Ayarlar
  dolabı arka duvarda, merkez (1,68, 1,52, 2,30), 0,68 × 0,70 × 0,22.
- Pozlar: K1 (0, 1,52, −0,15) +z'ye, dikey 56,7°, lens kaydırması −0,324; harita (0, 2,75, 1,30) → (0, 0,76, 1,36), 30°; K4 raf;
  K5a CRT; K6a vardiya anı; K8 kapı (0,05, 1,35, 0,95) +x'e; K9 dolap (1,70, 1,52, 1,44) +z'ye, 45°. Menü: sahanlık x 2,2–4,6,
  z −1,35…1,55, h 2,6; M0 (4,30, 1,66, 0,20) → (2,2, 1,58, 0,05), 52°; M2 (2,92, 1,40, −0,33) → kart rafı ve saat, 40°. Baskın:
  izometrik ortografik kamera. Yerleşimi değiştirmek istersen kamera pozunu da öner.
- Oyun kuralları, sayılar ve etkileşimler değişmez.

İstenen çıktı
- R1–R12 ekranları 1920×1080 (TR; R3, R6, R11'in EN varyantı da); R13–R14 serbest boyut.
- Her ekranın yanında kısa uygulama notu: bugünkü görüntüden ne değişti (eski → yeni), nesne listesi (ad, ölçü, malzeme),
  etkileşimli öğeler ve durumları (normal, üzerinde, basılı, devre dışı, yetersiz kaynak, dikkat), hareket ve ses tarifi
  (süre ve sıra).
- three.js sahnesi: Revizyon 2'nin `lib.js` yardımcıları ve koordinat kuralıyla (README'yi güncelle); yeni ve değişen nesnelerin
  konum, ölçü ve açısının kaynağı bu sahne olsun.
- Model kütüphanesi: her model ayrı bir OBJ dosyası (Y yukarı, metre; hareketli parçalar ayrı `o` grubu, pivotu grubun
  orijininde, adları yukarıdaki listedekiyle aynı), aynı nesnenin three.js kaynağı, model sayfası görüntüsü ve kısa bir tablo:
  ad, ölçü, üçgen sayısı, parçalar ve pivotları, malzeme anahtarları, yazı yerleri.
- Tokenlar ekteki `tokens_siginak.json` ve `raid_tokens.json` biçiminde; yeni ya da değişen değerleri `_new_1_3`, `_changed_1_3`,
  `_was_1_3` listeleriyle işaretle.
- Öncelik listesi: hangi değişiklik en çok kazandırır, hangisi en ucuz (her bölüm için ilk beş).
- Kararsız kaldığın en fazla 5 nokta için iki seçenekli küçük karşılaştırma.
