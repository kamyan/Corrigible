Son Şalter · Tasarım revizyonu 3 · Ek 1: brif yazıldıktan sonra değişenler (Claude Design brifi)

Kullanım: çizginin altındaki metni Revizyon 3'ün sürdüğü Claude Design oturumuna yapıştır. Yeni bir oturumda çalışılacaksa
önce `Docs/UI_Rev3/BRIEF_rev3.md`'yi ve onun eklerini ver, sonra bunu. Şu dosyaları yükle (yollar depo köküne göre):

- `Docs/UI_Rev3/screens_ek1/` (2026-11-05, eski şalter geri geldikten sonra): `01_masa_K1.png`, `02_salter_fis_basildi.png`,
  `03_K1_fis_bekliyor.png`, `04_vardiya_devredildi.png`
- `Docs/UI_KartliPano/screens/` (2026-11-05): `pano_TR.png`, `pano_EN.png`, `bosta_acik_TR.png`, `bosta_surukle_TR.png`,
  `bosta_toplandi_TR.png`, `defter_acik_TR.png`, `defter_acik_EN.png`, `kabul_TR.png`. Aynı klasördeki `makine_*.png` ve
  `oda_TR.png` eskidi (kaldırılan devir makinesi ve panonun eski büyük hâli); yükleme.
- Kurallar (metin): güncel `Docs/OYUN-TASARIMI.md` (özellikle "Kartlı sığınak ve topluluk", "Güncel pano üretimi ve eğitim",
  "Belgeyle kabul ve gecikmeli sızma", "Vardiya"). Tasarım geçmişi olarak `Docs/KARTLI-VARDIYA-PANOSU-DETAYLI-TASARIM.md` ve
  `Docs/TOPLULUK-KADRO-VE-SIZMA-TASARIMI.md`; çelişkide OYUN-TASARIMI geçerlidir.

---

Bu, "Tasarım revizyonu 3" brifinin ekidir. Brif 2026-11-03'teki oyunu anlatıyordu; o günden beri sığınağın mekaniği
değişti. Brifin dili, kısıtları, teslim biçimi ve korunacakları geçerli. Aşağıdakiler brifi günceller; çelişkide bu ek
geçerlidir. Kurallar ve sayılar değişmez; ekteki görüntülerdeki sayılar örnektir.

Ne değişti (2026-11-03 → 2026-11-05)

1. Sığınak maketi ve oda geliştirme yok. Yerine kartlı vardiya panosu ve isimli topluluk geldi.
   - Topluluk 8 isimli kişiyle başlar, en çok 10 kişi olur. Her kişinin adı, mesleği, beden profili (dayanıklı/yavaş,
     çevik/narin, güçlü/temkinli, dengeli), canı (CAN), hareketi (HAR), fiziksel gücü (GÜÇ), alan ustalıkları (kademe
     2/6/12 deneyimde açılır) ve üzerindeki eşyalar (bir kit, bir zırh, bir yardımcı eşya) vardır.
   - Her kişi vardiyada tek iş yapar. Kişi kartı görev alanına sürüklenip bırakılarak atanır. Dolu alana bırakmak iki
     kişinin görevini takas eder. Yanlış yere bırakılan kart geri döner.
   - Görev alanları: TEKNİSYEN, EMP UZMANI, ANALİST (baskına çıkacak üç kişi; her biri kendi rol kitiyle), ÜRETİM EKİBİ,
     MUTFAK, EĞİTİM (yanında küçük konu listesi: Teknisyen, EMP, Analiz, Üretim, Mutfak; seçim kalem halkasıyla) ve
     BOŞTA / DİNLENME (birden çok kişiyi tutan deste).
   - Boşta destesi: kapalıyken üst kart doğrudan sürüklenir. Tıklayınca kartlar üstten başlayarak sola tek tek açılır
     (her kart 320 ms, aralarında 70 ms). Tekrar tıklayınca alttan başlayarak toplanır. Açık elden bir kişi alınınca
     ötekiler desteye döner.
   - Üretim: ÜRETİM · İŞ EMRİ alanına tarif ve hammadde kartları bırakılır; kartlar bırakıldıkları serbest konumda kalır.
     Uygun girdi, üretici ve depo yeri varsa kapanışta tek ürün çıkar.
   - Tarif defteri: ciltli bir defter. Tıklayınca açılıp öne gelir; iki tarifli üç açılım vardır (önceki/sonraki).
     Tarif sayfadan üretime sürüklenir. Tarifler: zırh plakası, bakım kiti, EMP hücresi, üç rol kiti.
   - Depo (altta): HAMMADDE (Metal, Devre, Kumaş, Kimyasal; her biri STOK / BOŞ / AYRI) ve KULLANILABİLİR KİTLER (eşya
     kartları), yanında HURDA · GERİ DÖNÜŞÜM hedefi (eşya 1 metale döner).
   - Teçhizat kartı kişinin kartının üstüne bırakılır ve onunla taşınır; depoya bırakılınca kişiden çıkar.
   - Kişi dosyası: seçilen kişinin ayrıntısı, İNCELE, ERİŞİM ve TAHLİYE eylemleri.
   - Pano, eski raf ve maketin yerinde, odanın solunda. Anahtar rayındaki SIĞINAK tuşu kamerayı panoya götürür (K4).
     Panonun yanında MASAYA DÖN.
2. Kapı: 23 ziyaretçili siluet döngüsü yerine belgeyle kabul.
   - 2–4 vardiyada bir aday gelir. Önünde güncel KABUL TALİMATI (sürüm ve vardiya numarası; 4. ve 8. vardiyada değişir),
     adayın KİMLİK / SEVK BELGESİ (ad, meslek, geldiği yer, düzenleme, belge no, sevk no, mühür, portre izi, can/hareket/güç)
     ve inceleme bulguları vardır.
   - Dört inceleme: BELGELERİ KARŞILAŞTIR, GÖRÜŞME YAP, SOĞUK GÖZLEM (nefes buğusu), DIŞ KAYIT TEYİDİ (ayrı bir gözetmen
     gerekir, sonuç sonraki kapanışta gelir).
   - Dört karar: KABUL · TAM ERİŞİM, KISITLI KABUL (üç vardiya), REDDET, DIŞARIDAN YARDIM (2 erzak verilir, aday içeri
     girmez). Cevapsız başvuru kapanışta ret sayılır.
   - Robot kimliği gelişte gizli belirlenir. İçerideki robot önce normal çalışır, sonra iki kalıcı iz bırakır, müdahale
     edilmezse kaynak sabote eder. Kapıda tek yanlış kararla anlık oyun sonu yok. Eski VURULDU ve SIZMA sonları yeni oyunda
     yok; yeni son: SON KİŞİ (son canlı kişinin kaybı).
3. Vardiya kapanışı artık fişle yapılır. Şalter geri geldi: 2026-11-05'te kapaklı kutu ve iç tuşlu bir "devir makinesi"
   denendi, beğenilmedi; brifteki kapaklı vardiya şalteri modeli olduğu gibi geri kondu.
   - Akış: kapak kalkar → kol iner → şalterin solundan DEVİR FİŞİ kayar (0,35 s, 12 cm) → oyuncu fişi sürükleyip masadaki
     EVRAK tepsisine bırakır → ekran 0,2 s kararır, bir vardiya ilerler, teleks "VARDİYA DEVREDİLDİ" basar.
   - Fiş bekleyen şalter inik kalır: kapak açık, kol aşağıda, hazır lambası sönük, tıklanmaz. Plakanın ikinci satırı durumu
     söyler ("kapağı kaldır" / "fişi EVRAK'a koy").
   - Fişin içeriği: "VARDİYA 001 · DEVİR FİŞİ"; her kişinin adı ve görevi, beklenen erzak değişimi, üretim durumu, açık
     kapı başvurusu uyarısı. Fiş okumak ya da eline almak zamanı ilerletmez; yanlış yere bırakılan fiş çıkışa döner.
4. Baskın ekibi isimli kişilerdir: panoda Teknisyen, EMP ve Analist alanlarındaki üç kişi. Görev fişi ve kadro raporu
   adlarını, gerçek CAN/HAR/GÜÇ değerlerini ve teçhizatını gösterir. Geride kalan kişi kalıcı ölür; ustalığı ve eşyası
   kaybolur. Raporlar kişiyi adıyla yazar. Eski anonim beş uzman, Madenci ve Sis uzmanı, yedekler ve atölye seviyesi yeni
   oyunda yok.
5. Ekonomi: dört hammadde (metal, devre, kumaş, kimyasal; sayaç rafındaki MALZEME toplamı gösterir), erzak 60 tavanlı.
   İkmal baskını hammaddeyi türlerine ayrılmış verir.
6. Yeni pano için rehberli eğitim yok (eski ilk vardiya rehberi kapalı). Tasarım, ilk vardiyada neyin nereye
   sürükleneceğini fiziksel ipuçlarıyla göstermeli.

Bugünkü hâl ve görülen sorunlar (ekteki görüntüler)

- Pano (pano_TR, bosta_*, defter_acik): altın çerçeveli koyu yeşil, düz bir uGUI tahtası. Fiziksel bir pano değil, ekran
  arayüzü gibi okunuyor. Başlıklar ve "GÖREV PANOSU · kişiyi görev alanına, ekipmanı kişi kartının üstüne bırak" gibi
  yönerge satırları ekranda; KİŞİ DOSYASI ve MASAYA DÖN düz düğme.
- Pano K1'den bakınca odanın sol üstünü kaplayan büyük düz bir levha (01_masa_K1). "Daha küçük, ayaklı pano" olması
  gerekiyordu.
- Kişi kartları 63×88 oranında dikey koleksiyon kartı (panonun canvas'ında 210×294). Portreler gri yer tutucu siluet.
  Değerler kısaltmayla ("CAN 2/2 · HAR 4 · GÜÇ 1"). Teçhizat kartı kişi kartının köşesine binen küçük bir etiket
  ("Tek. kiti / Zırh / Cem").
- Hammadde kartlarında üç sayı (STOK / BOŞ / AYRI) okunması zor. Tarif defteri kapalıyken "TARİF DEFTERİ · AÇMAK İÇİN
  TIKLA" yazan düz bir kutu.
- Kapı (kabul_TR): "KAPI · BAŞVURU VE GÜVENLİK DOSYASI" kahverengi bir ekran penceresi; üç kâğıt sütunu ve sekiz düz düğme.
  Brifin "ekranda neredeyse hiç arayüz yok, her şey tıklanan fiziksel nesne" ilkesine uymuyor. İncelemeler (karşılaştırma,
  görüşme, soğuk gözlem, dış kayıt) fiziksel bir araçla yapılmıyor.
- Devir fişi (02–04): düz bir kâğıt, düzeni tasarlanmadı. Şalterin yanında masaya düşüyor.
- Pano ve kapı pozlarında ekranın üstündeki sayaç şeridi ve istasyon çipleri (Revizyon 2'den beri var) bu iki yüzeyde
  daha çok göze batıyor.

Görev (Revizyon 3'e eklenir)

Brifin beş görevi geçerli. Bunlara ek olarak:
1. Kartlı panoyu fiziksel bir nesne olarak tasarla: ayaklı pano (ya da başka fiziksel bir öneri), görev alanları, Boşta
   destesi, üretim alanı, depo, hurda, tarif defteri, kişi dosyası. Sürükle-bırak hedefleri büyük ve sınırları belli kalsın.
2. Kart ailesini tasarla: kişi kartı (ad, meslek, portre, CAN/HAR/GÜÇ, ustalık, görev, yaralı/yatıyor/kısıtlı/ölü
   durumları), teçhizat, hammadde ve tarif kartları; kişi kartının üstünde teçhizat. Portre sistemini öner (8 kişi, dört
   beden profili; fotoğraf değil, analog bir kimlik çizimi ya da benzeri). Portre, kart ya da dosya robotu ele vermemeli:
   ipuçları yalnız talimatın tarif ettiği belge ve nefes işaretlerinde.
3. Kapıdaki kabulü fiziksel yap: belgeler, talimat ve bulgular kapının önündeki bir yüzeyde ya da gözetleme/evrak
   gözünde. Dört inceleme gerçek araçlarla (ör. karşılaştırma için büyüteç ve talimat şeridi, görüşme için interkom, soğuk
   gözlem için buğu camı, dış kayıt için telsiz ya da teleks), dört karar damga ya da kol gibi geri dönülmez fiziksel
   eylemlerle. Kısıtlı kabul ve dış yardımın görünümünü de ver.
4. Vardiya kapanışını tasarla: şalterin dört durumu (hazır, kapak kalkık, inik ve fiş bekliyor, kilitli), fişin çıkışı
   (şaltere bir fiş ağzı ya da yanında küçük bir yazıcı ağzı önerilebilir; şalterin bugünkü biçimi korunur), devir fişinin
   düzeni (TR/EN), EVRAK tepsisine bırakma ve kararıp açılma anı.
5. Kâğıtlar ailesine (R10) yenileri ekle: devir fişi, kabul talimatı, kimlik/sevk belgesi, inceleme bulgusu, kişi dosyası,
   tarif sayfası. Görev fişi ve kadro raporu isimli kişilerle.

Teslim (yeni ve değişen bölümler)

Bölüm	Kod	Ekran / sayfa	Odak
B	R5	Komuta odası K1 (değişir)	Sol tarafta pano; ilk vardiya ve geç harekât; şalter hazır ve fiş bekliyor
B	R6	Harita, görev fişi, kadro raporu (değişir)	Üç isimli kişi, gerçek değerler ve teçhizat
B	R7	(kaldırıldı)	Sığınak maketi yok; yerine R15
B	R9	Kapı (değişir)	Kapı kanadı ve koridor brifteki gibi; ziyaretçi siluetleri yerine R17
B	R10	Kâğıtlar ailesi (değişir)	Yeni altı kâğıtla birlikte tutarlılık sayfası (TR/EN)
B	R15	Pano (K4)	Genel görünüş; atama sürüklenirken; dolu alana takas; Boşta kapalı / açık / bir kart alınmış; plan kilitli (baskın emrinden ya da fiş basıldıktan sonra)
B	R16	Kart ailesi	Kişi kartı ve durumları, teçhizat, hammadde, tarif; portre sistemi; kart üstünde teçhizat
B	R17	Kapı kabulü (K8)	Aday var, inceleme yapılıyor, dış kayıt bekliyor, karar damgalandı; talimat değişti; içeride robot izi bulundu (kişi dosyası)
B	R18	Tarif defteri ve üretim	Kapalı, açık (üç açılım), tarif sürükleniyor, iş emri dolu, ürün çıktı
B	R19	Vardiya kapanışı (K6)	Şalterin dört durumu, fişin çıkışı, fiş elde, EVRAK'a bırakma, kararma
D	R13	Model sayfaları (değişir)	Aşağıdaki model listesi değişiklikleri

Model listesi değişiklikleri (R13)
- Çıkar: raf ve sığınak maketi (raf, sandıklar, bidon, umut bitkisi, kesit maketi, oda kartları, özet panosu, BOŞTA tepsisi
  ve piyonlar), ziyaretçi siluetleri.
- Ekle: planlama panosu (kasa, ayaklar, yüzey, görev alanlarının çerçeveleri ve başlık plakaları, Boşta tepsisi, hurda
  kutusu), kart (kişi, teçhizat, hammadde, tarif; aynı ince kalınlık), tarif defteri (cilt, kapak, sayfalar), kişi dosyası
  klasörü, kabul yüzeyi ve araçları (talimat şeridi, belge, büyüteç, buğu camı, karar damgaları), devir fişi, fiş ağzı.
- Kalır: vardiya şalteri (brifteki pivotlarla), EVRAK tepsisi, kapı.

Hareketli parçalar ve pivotlar (ek)
- Vardiya şalteri: `Cover hinge` (yerel x, 0 → 112°), `Lever` (yerel x, +35 → −35°); fiş beklenirken ikisi de açık/inik
  kalır. Fiş çıkış noktası şalterin solunda (yerel −0,30, 0,14, 0,015 m); fiş çıkıştan 12 cm sağdan kayarak gelir.
- Kart: tutulunca yüzeyden kalkar, bırakılınca alanına yerleşir; Boşta destesinde üst kart ayrı.
- Tarif defteri: kapak ve sayfa çevirme; açılınca öne gelir.
- Kabul: karar damgası ya da kolu, büyüteç, interkom düğmesi, buğu camı kapağı.

Kısıtlar (ek)
- Etkileşim fareyle sürükle-bırak olarak kalır. Bugün pano ve kapı dünya uzayında uGUI canvas'ı; 3D bir öneri, bırakma
  alanlarını ve kart okunurluğunu korumalı (1920×1080'de kart adı ≥ 18 px, değerler ≥ 14 px).
- Panoda en çok 10 kişi, 7 görev alanı (Boşta dahil), 4 hammadde, en çok 32 eşya kartı sığmalı.
- Her metin TR ve EN; isimler Türkçe ve çevrilmez.
- Tokenları `_new_1_3`, `_changed_1_3`, `_was_1_3` listeleriyle işaretlemeye devam et.

İstenen çıktı
Brifteki gibi: R15–R19 ve değişen R5, R6, R9, R10 için 1920×1080 ekranlar (TR; R15 ve R17'nin EN varyantı da), her
ekranın uygulama notu (eski → yeni, nesne listesi, etkileşimli öğeler ve durumları, hareket ve ses), three.js sahnesi,
OBJ model kütüphanesi ve tokenlar. Önce R15, R16 ve R19'u teslim et (en çok bunlar oynanıyor), sonra R17 ve R18.
Pano ile kapı için en fazla iki seçenekli küçük bir karşılaştırma ver (ör. ayaklı pano mı, duvar panosu mu; kabul kapı
önündeki yüzeyde mi, masada mı).
