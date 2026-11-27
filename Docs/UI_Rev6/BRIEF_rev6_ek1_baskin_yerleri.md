Son Şalter · Revizyon 6 brifi, Ek 1 — baskın yerleri, Mars finali ve son şalter

Bu ek Claude Design içindir ve `BRIEF_rev6.md`'nin (24 Kasım 2026) üstüne yazılır. Oradaki her şey geçerlidir; bu ek
yalnız ondan sonra gelen değişiklikleri anlatır ve baskın tahtasının on üç yerini tek tek tarif eder. Değişiklikler
henüz oyunda değil; tasarımla birlikte uygulanacak. Bu yüzden bugünkü ekran görüntüsü olmayan yerleri tarife göre
tasarlayın.

**Kesin olanlar:**
- Son şalter on iki bölgenin hepsi kurtarılınca açılır. Çekilince Mars'taki uzay istasyonunda son YZ uyanır, ona üç
  baskın yapılır, üçüncüsüyle oyun biter.
- §4'teki dokuz yeni yer ve kural (konteyner limanı, kereste fabrikası, bakır izabe tesisi, akaryakıt deposu, gökdelen,
  sel basmış çarşı, denizaltı, kutup veri merkezi, otomobil fabrikası).
- VERITY, WAYPOINT ve HALCYON aynen kalır.

**Öneri olanlar** (metinde **ÖNERİ** diye işaretli; geliştirici onaylamadı, tasarım önerebilir):
- ÇEKİLME ve TAHLİYE sonlarının kalkması.
- Finalde İFŞA'nın her vardiya artması (finalin saati).
- Kaybedilen Mars baskınının yeniden denenebilmesi.
- Emir kâğıdının adı ve bütün plaka metinleri.
- Mars'ın kuralı, kurgusu (yörünge ya da yüzey), kanat adları, kanat hedefleri ve son YZ'nin adı ve imza makinesi.
- EMBER ve PARAGON'un bina adı; BOREALIS'in yer ayrıntısı.

Kaynaklar: `Docs/KAPI-BELGELERI-VE-YZ-MEKANLARI-TASARIMI.md` §B.9 (kuralların motor ayrıntısı ve kararları),
`Docs/BASKIN-MEKANLARI.md` §3 (bugünkü bölge kitleri ve kit kabul kapısı) ve §7 (bina sesi),
`Docs/TESIS-BASKINLARI-TASARIMI.md` §2.4 ve §10 (tesis şartı, Katar ve Yalpa). Görsel referans: `ekler/07–14`
(bugünkü baskın tahtası, VERITY kiti).


1. Kısaca ne değişti

- **Her binanın kuralı yerine oturuyor.** Bugün her tesisin bir şartı (tur tur işleyen bina içi kuralı) var, ama
  çoğu yerle bağsız: çip fabrikasında faz değişimi, devlet arşivinde kayar raf gibi. Yalnız WAYPOINT'in sevkiyat katarı
  ve HALCYON'un yüzer platformu yerin kendisinden gelen bir kural taşıyor (Katar, Yalpa). Yeni hâlde her şart yerin
  kendisinden gelir: herkesin tanıdığı bir yer ve o yerde beklenen ilk şey, tek cümle.
  - Aynen kalanlar: VERITY'nin söndürme bulutu, WAYPOINT'in Katar'ı, HALCYON'un Yalpa'sı.
  - Kodu kalıp yeri ya da takvimi değişenler: EDDY'nin süzgeci girdap olur (sel basmış çarşı; takvim 3., 6. ve 9. tur),
    PARAGON'un kaynak kolu robot kola sadeleşir, eski yük asansörü gökdelenin kabini olur.
  - Kuralı değişenler: TESSEL, HEADWATER, CAIRN, EMBER, SOLACE, QUORUM, BOREALIS.
  - Tahtadan kalkan bugünkü olay nesneleri: basınçlı tüp, tavan enkazı ve çatlak, kayar raf bloğu, turnike, kriyo
    alanı, gaz sızıntısı ve faz kanalları. `BRIEF_rev6.md` §4.3'teki "olay nesneleri (tüp, kaynak kolu, tavan enkazı,
    kayar raf)" satırının yerine bu ekteki nesneler geçer.
- **Adlar.** Altı binanın adı değişir (TESSEL, HEADWATER, CAIRN, SOLACE, EDDY, QUORUM); EMBER ve PARAGON'unki karar
  bekliyor; Mars yeni bir bina. Kanat adları bu altısında ve ayrıca EMBER ile BOREALIS'te değişir.
- **Son şalter harekâtın finalinin kapısı olur.** Bugün komuta odasının arka duvarındaki son şalter 5. vardiyadan sonra
  çekilebilen gönüllü bir çekilme düğmesi (ÇEKİLME / TAHLİYE sonları). Yeni hâlde şalter harekât boyunca mühürlüdür ve
  on iki bölgenin hepsi kurtarılınca açılır. Çekilince son YZ uyanır ve final başlar.
- **ÖNERİ:** şalter finale bağlandığı için ÇEKİLME ve TAHLİYE sonları kalkabilir. Onaylanırsa bu, `BRIEF_rev6.md` §2
  madde 8–9'daki şalter ve sonlar satırlarının ve plaka durumlarının ("çekilirse: ÇEKİLME/TAHLİYE") yerine geçer.
  Onaylanmazsa iki son kalır. Bu yüzden ÇEKİLDİ/TAHLİYE raporlarını silmeyin; Mars son raporunu yanına ekleyin.


2. Son şalter ve final (komuta odası, K1 ve KLS pozları)

Model ve yer aynı kalır (arka duvar, kapaklı kutu, sarı-siyah bant). Mars ve son YZ, şalter çekilene kadar oyuncuya
söylenmez: mühürlü plaka yalnız hedefi ve sayıyı verir. Durumlar (plaka metinleri **ÖNERİ**dir, daha iyisini önerin):

| Durum | Görünüm |
|---|---|
| Mühürlü (harekât boyunca) | KİLİTLİ bandı ve mühür. Plaka "MÜHÜRLÜ · 07/12 BÖLGE" (mantar panoyla aynı sayı). İlk vardiyadan beri bir hedef gibi okunmalı |
| Rehber sürüyor, baskın sürüyor, belge açık, masa meşgul, harekât bitti | Bugünkü gibi ("mühürlü · rehber", "baskın sürüyor" …) |
| Açık (12/12) | Mühür kırılır, bant düşer, amber lamba yanıp söner; teleks "MÜHÜR KIRILDI" basar. Plaka "AÇIK · çek" |
| Yeniden mühürlendi (açık soru) | 12/12'den sonra şalter çekilene kadar kuşatma sürer mi, bırakılan sektör şalteri yeniden mühürler mi henüz karar verilmedi. Tasarım bu hâli de göstersin (ör. "11/12 · mühürlendi") |
| Kapak kalkık / kol inik | Bugünkü iki adım; plaka "kolu indir" / "damgala" |
| Emir kâğıdı | ÇEKİLME EMRİ yerine yeni bir emir (**ÖNERİ:** "BAĞLANTI EMRİ"). Kâğıt geri alınmazlığı, içerideki kişileri ve kurtarılan bölgeleri yazar; DAMGALA |
| Final sürerken | Şalter inik kalır, lambası kırmızı. Plaka finalin durumunu yazar (**ÖNERİ:** "KANAT 1/3", "İFŞA n/6") |

Damgadan sonra: oda ışıkları bir an söner, CRT parazite döner, sonra CRT'de Mars'tan bir satır belirir. Harita
kâğıdının köşesine Mars için ayrı küçük bir pafta ya da raptiyeli bir yörünge çizelgesi gelir. Dünya haritası 12 bölge
için kalır; Mars onun parçası değildir. Görev fişi, bina planı paftaları ve baskın emri Mars'ın üç kanadını bugünkü
bölgeler gibi gösterir.

Kurgu notu: şalter hattı açsa da baskın yine telsiz ve ozalit paftayla oynanır; hat yalnız son YZ'yi uyandırır. Bunu
söyleyen bir kurgu cümlesi önerin (ör. kâğıtta ya da teleks satırında).

**ÖNERİ · finalin saati:** final sürerken İFŞA her vardiya kapanışında +1 artar ("AĞA BAĞLANMA" doktrini çiğnendi).
Eşikte bugünkü gibi KONUM ÇÖZÜLDÜ uyarısı açılır, uyarı süresi bitince sığınak düşer. İFŞA finale harekâttaki değeriyle
girer. Tasarım şeridin finalde geri sayım gibi okunan bir hâlini önersin.

**ÖNERİ · yeniden deneme:** kaybedilen Mars baskını yeniden denenebilir (bedeli vardiya, ölenler ve İFŞA).

Son rapor: zaferin adı "Silme" ve üç tonu (temiz, bedelli, gölgeli) aynen kalır; başlık metni Mars'a göre yazılır
(**ÖNERİ:** "Hat kesildi. Mars susuyor.").


3. Baskın yerleri: ortak dil

Her baskın bir binanın bir kanadıdır: üç oda (giriş, ara oda, hedef) ve aynı yoldan dönüş. Odalar 8×8 karedir; tahta
saha masasındaki ozalit paftadır (`BRIEF_rev6.md` §4.3). Bölgelerde kanat 01 ve 02 ikmal (hedefte ikmal kasası), kanat
03 sunucu (hedefte sunucu görevi) kanadıdır. Her binanın:

- **Bina kiti:** duvar barikatı, kanal içi ve gedik molozu (`BASKIN-MEKANLARI.md` §3), ortam sesi (§7) ve giriş kapısı
  modeli (yeni; bugün 13 bölgesel kapı modeli var, değişen binalar için yenisi gerekir).
- **Kuralı (tesis şartı):** yalnız ara odada ve hedef odada kurulur; giriş odası sadedir. Savunma baskınında kural
  kapalıdır. Kuralın parçaları (oluk, havuz, konteyner, rögar…) oda planındadır ve komuta odasındaki bina planı
  paftasında da çizilir.
- **İmza makinesi:** o YZ'ye özgü makine; hedef odada kesin bulunur.

Kuralın görünümü için ortak ilkeler:

1. **Takvimli ve fitilli kurallar bir tur önceden görünür.** Olacak şey bir önceki turda soluk kesik işaretle, olacağı
   turda koyu işaretle çizilir. Tur şeridinde olay turları glifle işaretlidir (bugün Yalpa: "2←", "4→", "YALPA 2/1";
   VERITY: "BULUT").
2. **Hep açık kurallar** (Katar'ın kenarı, döküm oluğu, su basmış bölme, soğuk koridor, ağırlıksızlık) tahtada kalıcı
   bir işaretle çizilir. İlk bakışta "burada durma" ya da "buradan düşersin" okunmalı. Bunların sonucunu önizleme
   gösterir.
3. **Önizleme:** üzerine gelme kartı ve hamle önizlemesi kuralın sonucunu gösterir: "DÜŞER", "1+1", "EYLEM YOK",
   süzülmenin yolu, itme oku.
4. **Şart kartı yenidir.** Bugün şart yalnız antetin notunda ve kurallar kartında yazar. Kartın başlığı kuralın kısa
   adıdır (İSTİF, YANGIN, OLUK…), altındaki tek cümle en çok iki satırdır (İngilizcede de; punto küçülmez), ayrıntı
   şemada ve üzerine gelme kartındadır. `BRIEF_rev6.md` §8'deki HUD yoğunluğu önerinizle birlikte kartın kalıcı mı,
   üzerine gelince mi, ilk ziyarette mi açılacağını ve genişliğini önerin. Rehber yerin ilk ziyaretinde bir "İLK KEZ"
   notu açar.
5. **Renk:** kural işaretleri için ayrı bir "şart" renk ailesi önerin. Bu aile kırmızıdan (YZ ve zarar niyeti),
   #854005'ten (kalkan ve kopya niyeti), #1A4789'dan (yürüyüş, seçim) ve sarıdan (rehber) gri tonda da ayrılmalı. Bugün
   Katar'ın kenarı #A9520A, Yalpa'nın kayma hattı #2C6E73; Yalpa'nın yatış etiketi "YALPA 1" ve çarpma ucu kırmızı (bunların
   da şart ailesine geçmesini önerebilirsiniz).
6. **Kurallar tarafa bakmaz:** makineleri de etkiler. İşaret ekip taşı için de makine için de aynıdır. Tek istisna
   Yalpa'da: çapa robotu ve cıvatalı makine kaymaz (sabit işareti).
7. **Kit ile kuralın nesnesi şekille ayrılmalı** (renk tek başına yetmez). Oyuncu hangi konteynerin taşındığını, hangi
   yığının yandığını, hangi varilin patladığını ilk bakışta ayırt etmeli. Her yerin "Kit" satırında ayrım önerisi var.

Kit kabul kapısı (`BASKIN-MEKANLARI.md` §3): duvar bloğu iki katlıdır (ayak izi 0,94, düz üst 0,56); süs yalnız
kameranın gördüğü batı, güney ve üst yüze gelir; köprü ve ayrık blok çeşitleri vardır; karo başına en çok 3 renderer;
emission yok; üst yüz L* 58–74, C* ≤ 22, ahşaba ΔE ≥ 15. Kit parçaları bugün 200–320 üçgendir; ~4.000 üçgen yalnız
büyük kural nesneleri (vinç, robot kol, asansör kabini, çekirdek) içindir. Özel shader yok (baskın kalemi hariç). Kare
125×71,7 px eğik elmas.

Takvim: oda süreleri giriş 6, ara oda 6, hedef 10, dönüş 6 tur. "Çift turlar" 6 turluk odada 2 ve 4, hedefte 2, 4, 6 ve
8 demektir; "3., 6. ve 9. tur" 6 turluk odada yalnız 3'tür. "Tek turlar" 1, 3, 5 (hedefte 7, 9).


4. On üç yer

Aşama, harita üreticisinin bölgeyi karargâha yakın (erken) ya da uzak (geç) koyduğunu söyler. Parantezde bugünkü ad;
değişen adlar kalın.

| YZ | Aşama | Bina | Kanatlar (01 · 02 · 03 sunucu) | Kural | İmza makinesi |
|---|---|---|---|---|---|
| VERITY | Karargâh | Banka veri merkezi | Teknik depo · UPS odası · Sunucu salonu | Söndürme bulutu | Zırhlı dron |
| WAYPOINT | Orta | Sevkiyat katarı | Yük vagonları · Ayıklama vagonu · Sunucu vagonu | Katar | Tampon |
| HALCYON | Geç | Yüzer maden platformu | Kumanya güvertesi · Bant galerisi · Makine dairesi | Yalpa | Çapa robotu |
| TESSEL | Erken | **Konteyner limanı** (Çip fabrikası) | **İstif sahası · Vinç altı · Liman kontrol kulesi** | İstif | Mühürleyici |
| HEADWATER | Erken | **Kereste fabrikası** (Hidroelektrik santrali) | **Tomruk sahası · Hızar holü · Kumanda odası** | Yangın | Alev robotu |
| CAIRN | Erken | **Bakır izabe tesisi** (Cevher hangarı) | **Cevher hangarı · Döküm holü · Kumanda kabini** | Döküm oluğu | Havan |
| EMBER | Orta | Akaryakıt deposu (Petrol rafinerisi; ad açık) | **Dolum sahası · Varil ambarı · Pompa dairesi** | Varil | Patlayan robot |
| SOLACE | Orta | **Gökdelen** (Çağrı merkezi) | **Lobi · Asansör holü · Sunucu katı** | Asansör | Kalkan dronu |
| EDDY | Orta | **Sel basmış çarşı** (Elektronik fabrikası) | **Tezgâh sokağı · Kapalı pasaj · Kopya atölyesi** | Girdap | Çoğaltıcı |
| QUORUM | Geç | **Denizaltı** (Devlet arşivi) | **Torpido dairesi · Akü dairesi · Kumanda merkezi** | Su basmış bölme | Kilitleyici |
| BOREALIS | Geç | Kutup veri merkezi | **Dış hava panjuru · Soğuk koridor · Sunucu bunkerı** | Soğuk koridor | Kırağı dronu |
| PARAGON | Geç | Otomobil fabrikası (Montaj fabrikası; ad açık) | Parça ambarı · Kaynak hattı · Hat kontrol odası | Robot kol | Montajcı |
| Son YZ (adı açık) | Final · Mars | **Yörünge istasyonu** (yeni; ÖNERİ) | Yaşam halkası · Reaktör modülü · Çekirdek modülü (ÖNERİ) | Ağırlıksızlık (ÖNERİ) | henüz seçilmedi |

Bugünkü kanat adları: EMBER "Malzeme ambarı · Pompa holü · Kompresör dairesi", BOREALIS "Soğuk depo · Azot galerisi ·
Sunucu bloğu". Yer ve kanat adları Türkçe yazılır ve İngilizceye çevrilir (ör. "Flooded Bazaar", "Stall Street",
"Covered Arcade", "Copy Workshop"); YZ adları çevrilmez. Gerçek kurum adları, amblemleri ve bayrakları kullanılmaz.

Her yerin tarifinde "Tahtada" satırı kuralın parçalarını ve işaretlerini, "Kit" satırı duvar ve kapı dilini, "An"
satırı kuralın en okunur anını (tasarım karesine aday) verir.

4.1 VERITY · Banka veri merkezi (değişmez)
- Yer: YZ'nin ele geçirdiği banka veri merkezi; karargâh. Tavandaki CO₂ memeleri yangın sanıp salonları beyaz bulutla
  doldurur.
- Kural: **Söndürme bulutu.** Tek turların sonunda iki memeden biri (sırayla) kendi alanını bir turluğuna bulutla
  doldurur; bulut sonraki adımda dağılır. Buluta ve buluttan nişan alınmaz; bitişik nesneye iş (sabotaj, onarım)
  serbesttir.
- Tahtada: iki meme, her memenin artı biçimli alanı (meme karesi ve açık dört komşusu), sıradaki memenin bir tur önceki
  "BULUT" işareti, dolu alan ve "DAĞILIR".
- Kit: sökülmüş rack kabinleri (bugünkü varsayılana en yakın kit). Bugünkü ekran görüntüleri bu kitle.
- An: bulut dolu; içerideki uzman bitişik panoyu sabote ediyor, zırhlı dron ona nişan alamıyor.

4.2 WAYPOINT · Sevkiyat katarı (var)
- Yer: kıtayı boydan boya geçen sevkiyat katarı; odalar vagonlardır (giriş vagonunda kenar kapalı).
- Kural: **Katar.** Vagonun kuzey ve güney kenarı açıktır; dışarı itilen düşer. Dönüşte bağlantıya 2 kare ya da daha
  yakın makineler sonraki vagona atlar. Dönüşte Teknisyen geçitteyse ve o tur yeteneğini kullanmadıysa GEÇ yerine AYIR
  verilebilir (vagon ayrılır, takip olmaz).
- Tahtada: açık kenarın kesik uyarı çizgisi ve dışa bakan tırnaklar, takip edecek makinelerin işareti, AYIR seçeneği.
- Kit: palet ve streç filmli koli; kanalda kılavuz raylar.
- An: uzman bir makineyi açık kenardan itiyor ("DÜŞER").

4.3 HALCYON · Yüzer maden platformu (var)
- Yer: açık denizde demirli, uzaktan yönetilen ıssız maden platformu.
- Kural: **Yalpa.** Güverte 2., 4. ve 6. turun sonunda sırayla batıya, doğuya, batıya yatar. Sabitlenmemiş her şey
  (birimler, hurda, yatan gövde, yerdeki yük) bir kare kayar; engele çarpan 1 hasar alır. Duvarlar, nesneler, çapa robotu
  ve cıvatalı makine kaymaz.
- Tahtada: şerit okları, önceki turda soluk "YALPA 2", yatış turunda koyu "YALPA 1", kayma hatları, tahtanın ~2,5°
  eğilmesi.
- Kit: baklava sac kızak, yedek parça kasaları.
- An: güverte yatıyor, makineler duvara çarpıyor, çapa robotu yerinde kalıyor.

4.4 TESSEL · Konteyner limanı (yeni)
- Yer: otomatik konteyner limanı (Busan, Yokohama gibi). İnsansız; raylı portal vinçler konteyner istiflerini taşır.
- Kural: **İstif.** Vinç çift turların sonunda işaretli konteyneri öbür işaretli yere taşır. İniş yerindeki birim 1
  hasar alır ve konteynerin gidiş yönünde bir kare itilir; itilemezse hasarı yine alır, yerinde kalır ve vinç o sefer
  bekler. İniş yerinde nesne (kalas, tel, hurda) varsa vinç bekler, kimse hasar almaz.
- Tahtada: iki iniş yeri işareti ("VİNÇ"), konteyner (bir kare duvar gibi), vincin rayı ya da gölgesi. Taşıma turunda
  üç önizleme: itme oku; itilemez işareti ve "−1"; "İNİŞ KAPALI". Konteyner dinamitle yıkılmaz: çelik ve sağlam okunmalı.
- Kit: oluklu konteyner istifleri; kanalda vinç rayı; giriş kapısı liman çit kapısı. Ayrım: taşınan konteyner tek renk,
  kancalı ve vinç gölgeli; duvar istifi başka renkte ve iki katlı.
- An: konteyner havada, iniş yerinde bir makine; vinç bir uzmanı konteynerin gidiş yönünde iter.

4.5 HEADWATER · Kereste fabrikası (yeni)
- Yer: Amazon kıyısında, ormanı keserek kurulmuş bir kereste fabrikası; tomruk yığınları ve hızar holü.
- Kural: **Yangın.** Atış, alev ya da patlama değen kereste yığını bir tur sonra yanar ve yok olur.
- Tahtada: kereste yığınları (siper gibi nesneler), tutuşmuş yığının bir tur süren alevi ("YANIYOR"), sonra boşalan kare
  ve kül izi. Siper iki taraf için de geçicidir. Yanan yığın komşusunu tutuşturmaz (zincir yok).
- Kit: talaş, hızar bandı. Ayrım: yanıcı yığın gevşek tomruk; duvar barikatı çelik bantlı, bağlı kereste istifi.
- An: uzmanın arkasına saklandığı yığına alev robotunun alevi değer; bir tur sonra yığın yanar ve uzman açıkta kalır.

4.6 CAIRN · Bakır izabe tesisi (yeni; bina içi)
- Yer: Zambiya Bakır Kuşağı'nda bir izabe tesisinin döküm holü. Erimiş bakır ocaktan potaya, zemine gömülü açık
  oluklarda akar.
- Kural: **Döküm oluğu.** Tur sonunda erimiş metal oluğunda duran her birim 1 hasar alır.
- Tahtada: 1 kare geniş, 3–6 kare uzun oluklar (hep açık, takvimsiz). Oluk geçilir ama içinde durulmaz: yürüyüş yolu
  oluğun üstünden geçebilir; işaret "geçilir, durulmaz" demeli. Oluk yavaş makinelere (havan 1 kare yürür) karşı bir
  hendektir. Erimiş metalin "sıcaklığı" emission ve ışık olmadan, renk ve kenar tonuyla çözülmeli.
- Kit: cüruf ve cevher yığınları, döküm potaları, oluklu sac. Bugünkü "Cevher hangarı" adı ilk kanada iner.
- An: havan oluğun öbür yakasından atar; oluğa itilen makine kızarır.

4.7 EMBER · Akaryakıt deposu (yeni kural)
- Yer: meşalesi sönmeyen bir rafinerinin akaryakıt deposu; dolum sahası, varil ambarı, pompa dairesi.
- Kural: **Varil.** Darbe alan varil bir tur sonra patlar; dört yanındaki herkes 1 hasar alır.
- Tahtada: variller (nesne), darbe alan varilin fitili ("PATLAR"), dört komşu karenin patlama önizlemesi.
- Kit: boru demetleri ve flanşlar (bugünkü rafineri kiti). Ayrım: patlayan varil tek, kırmızı bantlı ve tıpalı (kırmızı
  burada tehdit anlamında doğru); duvar barikatı paletli ve kapaklı varil istifi ya da boru demeti.
- An: ekip bir varili vurur, makinelerin toplandığı köşe bir tur sonra patlar. Patlayan robot varilin yanında infilak
  ederse aynı köşe iki turda iki kez patlar.

4.8 SOLACE · Gökdelen (yeni)
- Yer: Mumbai'de bir gökdelen; lobi, asansör holü, sunucu katı.
- Kural: **Asansör.** Kabin çift turların sonunda içindekini öbür durağa taşır; iniş durağı doluysa bekler.
- Tahtada: iki durak (aynı odada iki ayrı yer), kabin işareti, taşıma turunda "KABİN" ve hangi durağa gideceğini gösteren
  ok. Turunu durakta bitiren makine de taşınır.
- Kit: cam bölmeler, ofis bölme panelleri, mermer lobi; giriş kapısı döner kapı ya da lobi kapısı.
- An: kabine itilen kalkan dronu öbür durağa gider ve zırh vereceği makineden kopar.

4.9 EDDY · Sel basmış çarşı (yeni yer; bugünkü süzgeç, takvimi 3., 6. ve 9. tur)
- Yer: muson selinin bastığı bir elektronik çarşısı (Jakarta'daki Glodok gibi). Tezgâhlar suyun altında; dükkân bölmeleri
  odanın duvarlarıdır.
- Kural: **Girdap.** Rögar 3., 6. ve 9. tur sonunda açılır ve dört yanındakileri içine çeker.
- Tahtada: kapağı kalkmış rögar, emme turunda içe dönen oklar, dört komşu kare. Sığ su yalnız zemin dokusudur (kuralı
  yok).
- Kit: kum torbaları, ıslak tezgâhlar, sarkan tabelalar, suya düşmüş kablolar (bugünkü EDDY kitinin suyu kalır).
- An: çoğaltıcı ile kopyası aynı emmede çekilir ve çarpışır.

4.10 QUORUM · Denizaltı (yeni)
- Yer: Karadeniz'de bir denizaltı üssüne yanaşık denizaltı; dar bölmeler, su geçirmez kapılar.
- Kural: **Su basmış bölme.** Su basmış karede duran birim eylem yapamaz, yalnız yürür (ıslanan alet ve robot kısa devre
  yapar).
- Tahtada: 4–8 karelik bir ya da iki su havuzu (kalıcı işaret), sudaki birimin üstünde "EYLEM YOK". Hedefe sudan geçmeyen
  bir yol hep vardır. Su, EDDY'nin sığ zemin suyundan ayrı okunmalı (EDDY'de suyun kuralı yok).
- Kit: borular, vana çarkları, perçinli çelik; geçitler oval su geçirmez kapı (kapı çarkı). Odalar uzun ve dar okunmalı.
- An: kilitleyici sudaki uzmanı kilitler; uzman o tur ne yürüyebilir ne eylem yapabilir.

4.11 BOREALIS · Kutup veri merkezi (yeni kural)
- Yer: Sibirya'da donmuş toprağa gömülü veri merkezi. Serbest soğutma için eksi kırk derecelik dış hava, raf sıralarının
  ön yüzündeki soğuk koridora doğrudan basılır.
- Kural: **Soğuk koridor.** Soğuk koridorda duran birim aldığı darbeden 1 fazla hasar alır ("eksi kırkta çelik cam gibi
  kırılır"). Aynı tur içinde en çok 1 fazla; zırh plakası fazlayı emer.
- Tahtada: raf sıraları arasında 1 kare geniş, 4–6 kare uzun şeritler (hep açık, kalıcı işaret). Önizleme "1+1" gösterir.
- Kit: sandviç panel kabinler, sunucu rafları, panjurlar; kanalda ısıtıcı hat. Ayrım: koridor şeridi, kitin kendisi ve
  kırağı dronunun buz alanı ("frost", iki tur süren artı biçimli alan) üç ayrı şeydir; kitte kırağı kullanmayın ya da
  gri tonda ayırın.
- An: kırağı dronunun buzu uzmanın yürüyüşünü soğuk koridorda bitirir; sıradaki turda bir makinenin 1'lik atışı ona 2
  vurur. (Kırağı dronu ateş etmez; yalnız buz atar.)

4.12 PARAGON · Otomobil fabrikası (bugünkü kol sadeleşir)
- Yer: YZ'nin kendi robotlarını kaynakladığı otomobil ve montaj fabrikası.
- Kural: **Robot kol.** Kol her tur sonunda önündeki kareye vurur ve saat yönünde çeyrek döner (duvara, kenara ya da
  hedefe bakan yön atlanır).
- Tahtada: zemine cıvatalı kaynak kolu (nesne), baktığı kare ve sonraki turda bakacağı kare (dönüş oku).
- Kit: istiflenmiş parça kasaları, kablo zinciri, sarı-siyah hat çizgileri.
- An: kolun düşürdüğü makine hurda bırakır; montajcı hurdadan dron kurar.

4.13 Mars · Yörünge istasyonu (final; bütünüyle ÖNERİ)
- Yer: Mars yörüngesinde, Phobos'a demirli bir araştırma istasyonu. Son YZ yaşam halkasının dönüşünü durdurmuş;
  istasyonda yerçekimi yok. Görünüm analog ve ağır: perçinli modüller, tutunma rayları, kayışlar, yuvarlak lombozlar,
  kırmızı Mars ışığı. Ekibin Mars'a nasıl ulaştığı açık bir sorudur; bir kurgu önerin.
- Kural: **Ağırlıksızlık.** İtilen birim bir engele ya da birime çarpana kadar süzülür.
- Tahtada: itme önizlemesi süzülmenin bütün yolunu ve çarpma noktasını gösterir. Geçitler hava kilididir (dairesel
  kapak, basınç lambası). Pafta yine ozalittir; istasyon saha masasında öbür binalar gibi tutulur.
- Kit: yeni (modül paneli, tüp koridor); duvar barikatı bağlı kargo torbaları ve kayışlı kasalar.
- Kanatlar: üç kanat sırayla açılır. Kanat 1 ve 2'nin hedefi henüz seçilmedi (ikmal kasası ya da bir sistem nesnesi).
  Kanat 3'ün hedefi son YZ'nin çekirdeğidir (bugünkü "core" nesnesi, daha büyük); sunucu ödülü yoktur. Kural giriş
  odasında yoktur; finalde savunma baskını yoktur.
- Antette ve haritada YZ adı yerine geçici bir etiket kullanın; son YZ'nin adı, görünümü ve imza makinesi için öneri
  bekliyoruz (bölge YZ'lerinin kalıbı: duygu taşıyan tek İngilizce sözcük, çevrilmez; önerilerimiz LODESTAR, TETHER).
- Işık gecikmesi (ör. "rapor 14 dakika önce") yalnız telsiz fişinin üst bilgisinde süs olarak durabilir; tur akışını ve
  gözcü satırlarını değiştirmez.
- An: EMP bir makineyi süzerek karşı duvara çarptırır.
- Yedek kural (ağırlıksızlık seçilmezse): **Güneş fırtınası.** 3., 6. ve 9. tur sonunda radyasyon sığınağında durmayan
  herkes 1 hasar alır (sığınak kareleri işaretli). İstasyon yüzeyde olursa ağırlıksızlık yerine **Düşük yerçekimi:**
  itilen her birim iki kare gider (kanatlar: Hava kilidi · Sera kubbesi · Çekirdek modülü).


5. İstenen (öncelik sırasıyla)

1. Yeni yedi yerin (konteyner limanı, kereste fabrikası, bakır izabe tesisi, akaryakıt deposu, gökdelen, sel basmış
   çarşı, denizaltı) ve kutup veri merkezinin "An" kareleri (1920×1080, Türkçe; tahta işaretleri ve HUD ile). Sonra
   VERITY, WAYPOINT, HALCYON, PARAGON ve Mars.
2. Takvimli ve fitilli kurallar için bir tur önceki ("gelecek tur") hâl: VERITY, HALCYON, TESSEL, HEADWATER, EMBER,
   SOLACE, EDDY, PARAGON. Hep açık kurallar (WAYPOINT, CAIRN, QUORUM, BOREALIS, Mars) için onun yerine önizleme ve üzerine
   gelme kartının bir kesiti.
3. Bina kitleri: altı yeni bina ve Mars için duvar barikatı, kanal içi, gedik molozu ve giriş kapısı; EMBER'e varil, BOREALIS'e panjur
   ekleri. Kuralın nesneleri: konteyner ve vinç, kereste yığını, döküm oluğu, varil, asansör kabini ve durak, rögar, su
   havuzu, soğuk koridor şeridi, robot kol, hava kilidi.
4. Kural işaretleri bileşen sayfası: on üç kuralın tahta işaretleri (kalıcı, gelecek tur, olay turu), tur şeridi glifleri,
   şart renk ailesi, şart kartları (kısa ad ve tek cümle, TR ve EN), "İLK KEZ" rehber notları.
5. Bina planı paftası (komuta odası K2/K3): kuralın parçalarının ozalit çizimdeki hâli (her yer için bir örnek).
6. Son şalter: durum tablosu (mühürlü / açık / yeniden mühürlendi / kapak / kol / final sürerken), emir kâğıdı,
   damgadan sonraki an (K1 ve KLS), haritadaki Mars paftası; ÖNERİ olarak finalde İFŞA şeridinin geri sayım hâli.
7. Mars: istasyonun üç kanadı, çekirdek, son YZ'nin görünümü ve adı, final son raporunun başlığı.
8. Token önerisi: şart renk ailesi ve kitlerin renkleri `raid_tokens.json` biçiminde (`kits.{id}`, yalnız yeni ve
   değişen anahtarlar, eski değeriyle).


6. Kısıtlar (`BRIEF_rev6.md` §6'ya ek)

- Takvimli ya da fitilli kural bir tur önceden görünmeli; hep açık kural kalıcı işaretli olmalı. Oyuncu hiçbir kuralın
  sonucuna habersiz yakalanmamalı.
- Kural işaretleri makine niyet oklarıyla, yürüyüş karelerinden ve rehber işaretçisiyle karışmamalı (§3 ilke 5).
- Kural tarafa bakmaz; işaret ekip ve makine için aynı olmalı (Yalpa'nın sabit istisnası dışında).
- Şart kartının cümlesi en çok iki satır (İngilizcede de); punto küçülmez.
- Bina planı paftasında robot ve niyet çizilmez (değişmedi).
- Mars ve son YZ, şalter çekilene kadar hiçbir yerde görünmez.
- Gerçek kurum adı, amblemi ya da bayrağı kullanılmaz.


7. Karar bekleyenler (önerin; geliştirici seçecek)

- ÇEKİLME ve TAHLİYE sonları kalksın mı?
- Finalin saati: İFŞA her vardiya artsın mı? Kaybedilen Mars baskını yeniden denensin mi?
- 12/12'den sonra kuşatma sürsün mü, bırakılan sektör şalteri yeniden mühürlesin mi?
- Emir kâğıdının adı ve plaka metinleri.
- Mars: yörünge istasyonu (ağırlıksızlık) mı, yüzeyde bir üs (düşük yerçekimi) mi? Ekip oraya nasıl gidiyor? Kanat
  adları ve kanat 1–2'nin hedefi; son YZ'nin adı, görünümü ve imza makinesi.
- BOREALIS'in yer ayrıntısı: soğuk koridor mu, panjur önündeki kırağılı ızgara ("gevrek çelik") mı?
- EMBER ve PARAGON'un bina adı korunsun mu (Petrol rafinerisi, Montaj fabrikası)?
- Şart kartı kalıcı mı, üzerine gelince mi, ilk ziyarette mi?
