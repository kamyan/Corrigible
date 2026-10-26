# DeskGame · Sığınak Komuta Odası — Claude Design brifi

## Eklenecek görseller ve dosyalar

Brifle birlikte şunları yükle. İlk üçü bugünkü sahnedir; tasarım bunların **yerleşimini ve işlevini** korur,
görünüşünü baştan kurar.

1. `Docs/Gorseller/DeskGame_Kurtulus.png`: başlangıç kamerası. Oda, sol raf (sığınak kesiti), orta masa (dünya
   haritası), sağ masa (CRT monitör), üst kaynak çubuğu, alt istasyon düğmeleri.
2. `Docs/Gorseller/DeskGame_Siginak.png`: sığınak yakın görünümü (2 · SIĞINAK). Dört oda, oda ayrıntısı, personel düğmeleri.
3. `Docs/Gorseller/DeskGame_Operasyon.png`: CRT yakın görünümü. Baskından hemen önceki saha yayını ekranı.
4. `Docs/UI_Sade/SonSalter_Sade_Paketi/reference/screens/S-Masa1.jpg` ve `S-Masa2.jpg`: masadaki haritanın bugünkü
   “Sade” dili (pafta, bölge kartı, harita bağı). Harita bu dilde kalır.
5. `Docs/UI_C/reference/screens/C5_styleguide_full.png`: Analist Masası stil rehberi. Güven katmanı (ONAYLANDI / RAPOR /
   ŞÜPHELİ / BİLİNMİYOR), kâğıt malzemeleri, damga ve kalem işaretleri.
6. `Docs/UI_Sade/SonSalter_Sade_Paketi/spec/tokens_sade.json`: renk ve yazı tokenları.
7. İsteğe bağlı: `Docs/SIGINAK-KOMUTA-MASASI-TASARIMI.md` (sığınak/analog masa vizyonu) ve
   `Docs/OYUN-TASARIMI.md` → “DeskGame · işgalden kurtuluş kampanyası” (kurallar). Gereken her şey aşağıda da var.

## Bağlam

“Son Şalter” (Corrigible) tek oyunculu bir strateji oyunu. Bu sahnede dünya çoktan düşmüş: yapay zekâ (YZ) 12 bölgenin
tamamını, bütün ağları ve sunucuları ele geçirmiş. Oyuncu, Batı Avrupa'nın altındaki küçük bir nükleer sığınakta
**KYGA / SIĞINAK 07**'nin komutanı. Karşı harekâtı buradan yönetiyor.

**Neden analog:** YZ her dijital cihazı ele geçirebiliyor. Sığınağın doktrini duvardaki afişte yazıyor:
**“AĞA BAĞLANMA · ANALOG KAL”**. Masadaki her şey fiziksel: kâğıt harita, faks, tüplü telsiz, mekanik sayaçlar,
damga, kurşun kalem, pirinç şalterler. Tek “ekran” sağdaki tüplü (CRT) monitör ve o da yalnızca sahadan gelen
kapalı devre yayını gösteriyor.

**His:** Soğuk Savaş kriz sığınağı. *Papers, Please* × *Inscryption* × *Pacific Drive* × *This War of Mine*.
Loş, dar, sıcak lamba ışığı ile soğuk beton; yorgun ama inatçı bir umut.

## Oyunun ekrana yansıyan kuralları

Sayılar geçicidir; tasarım onları göstersin, değiştirmesin.

**Harekât**
- 12 bölge, hepsi başta **%100 işgal** altında. Her bölge sırayla üç **sektör**den oluşur:
  `01 Tedarik deposu (yağma)` → `02 Tahliye geçidi (yağma)` → `03 Sunucu merkezi (sunucu yıkımı)`.
- Sektör durumları: **KİLİTLİ**, **BASKINA HAZIR**, **GERİ ALINDI**. Bölgenin işgal oranı %100 → %66,7 → %33,3 → %0.
- Bir bölge bitince **özgür** olur; sıradaki cephe, özgür bir bölgeye **komşu** işgal altındaki bölgelerden seçilir
  (“Seçili bölgede cephe aç”). Aynı anda tek cephe açıktır. 36 sektörün hepsi alınınca zafer.
- Baskın: haritadaki **görev fişinden** emir verilir → kamera CRT'ye yaklaşır ve ekranın içine girer → ayrı sahnede
  8×8 taktik baskın tahtası (bu brifin kapsamı dışında) → sonuç raporu → masaya dönüş.
- Bir ekip var (**Alfa**: Teknisyen, EMP uzmanı, Analist). Başarısızlıkta ekip yaralanır, birkaç gün baskına çıkamaz.

**Zaman ve ekonomi**
- Zaman yalnızca oyuncu **VARDİYAYI BİTİR** dediğinde bir gün ilerler. Gerçek zaman akmaz.
- Kaynaklar: **Fon**, **Erzak**, **Malzeme**, **Enerji**, **Topluluk** (kişi / kapasite / boşta). Ayrıca vardiya sayacı ve
  **Özgür bölge X/12**.
- Baskın bedeli 12 fon. Yağma ödülü fon + malzeme + erzak (+1 kurtulan); sunucu yıkımı fon + malzeme.

**Sığınak (yan kesit)**
- Dört oda: **Yaşam alanı**, **Jeneratör**, **Atölye**, **Revir**. Her oda: seviye 0 (HASARLI) – 3, personel x/y.
- Eylemler: **Onar / Geliştir** (fon + malzeme), **+ / − personel**. Oda seçilince açıklama, maliyet ve hazır olup
  olmadığı görünür. Atölye ve revir enerji tüketir; enerji yetmezse çalışmaz.

## Bugünkü sahne ve sorunları

Sahne Unity URP'de gerçek 3D, birinci şahıs masa kamerası. Üç kamera pozu var: **1 · KOMUTA** (harita),
**2 · SIĞINAK** (sol raf), **3 · GÖREVLER** (haritada görev fişi; CRT yalnız baskına geçişte). Sağ tuş bakış,
tekerlek yaklaşma, F harita odağı, Home başlangıç pozu.

Görüntülerde görülen sorunlar, tasarımın çözmesi gerekenler:
1. **Üç ayrı görsel dil yan yana:** masadaki kâğıt harita (Sade), sığınak rafının üstüne yapıştırılmış yarı saydam siyah
   bilgi kutuları, ekranın üstünde ve altında düz yeşil oyun HUD'u. Hiçbiri birbirine ait görünmüyor.
2. **Sığınak bilgisi diegetik değil:** oda etiketleri ve ayrıntı paneli 3D rafın önünde yüzen siyah kutular.
3. **Başlangıç pozunda masa nesneleri okunmuyor:** masa önündeki sekmeler, faks, evrak tepsisi ve kılavuz fişi küçük
   ve bulanık; neyin tıklanabilir olduğu belli değil.
4. **Harita bilgi hiyerarşisi yok:** 12 bölgenin hepsi aynı koyu kırmızı. Mevcut cephe, özgür bölgeler, cephe
   açılabilecek komşular ve sektör ilerlemesi bir bakışta ayrılmıyor.
5. **Geri bildirim kayıp:** “Emir uygulandı.” yazısı klavyenin/rafın üstünde serbestçe yüzüyor.
6. **Oda kimliksiz ve karanlık:** primitive bloklar, tek masa lambası, düz duvarlar. Sığınağın hikâyesi (yaşayan
   insanlar, eskiyen ekipman, YZ'nin yukarıda olduğu) odadan okunmuyor.
7. **VARDİYAYI BİTİR**, oyunun tek zaman düğmesi olmasına rağmen herhangi bir düğme gibi duruyor.

## Görev

DeskGame'in sığınak komuta odasını **1920×1080**'de, bütünlüklü tek bir **analog sığınak** diliyle tasarla.
Bilgi ve etkileşim aynı kalır; görünüş, malzeme, ışık ve yerleşim yenilenir.

**Teslim edilecek ekranlar**

| Kod | Ekran | İçerik |
|---|---|---|
| K1 | Başlangıç pozu | Odanın tamamı: sol sığınak rafı/kesiti, orta komuta masası ve harita, sağ CRT masası, duvar, ışık. Kaynak göstergeleri ve istasyon seçimi. |
| K2 | Komuta / harita odağı | Haritaya yakın bakış. Seçili cephe bölgesi, ona bağlı **görev fişi** (bölge adı, sektörler 0/3, her sektörün türü ve durumu, görev açıklaması, “Baskın emri ver” ve fişi kapatma). Özgür, cephe, komşu-aday ve işgal bölgelerinin ayrı görünümü. |
| K3 | Yeni cephe seçimi | Bir bölge yeni özgürleşmiş; komşu adaylar vurgulu, “Seçili bölgede cephe aç” onayı. |
| K4 | Sığınak odağı | Dört oda yan kesiti, seçili oda, oda kartı (açıklama, seviye, personel, maliyet, Onar/Geliştir, ± personel), sığınak özeti (topluluk, boşta, enerji, erzak, son vardiya farkı). Bir oda HASARLI, biri enerjisiz. |
| K5 | CRT saha yayını | İki hâl: “Bağlantı bekleniyor” ve “Bağlantı kuruluyor / saha yayını açılıyor” (kameranın ekrana girdiği an için bir kare). |
| K6 | Vardiya geçişi | VARDİYAYI BİTİR anı ve ardından gelen vardiya özeti (gelir/gider, üretim, ekip toparlanması). |
| K7 | Hikâye / brifing kâğıdı | Açılıştaki görev metni (“Dünya çoktan düştü…”) masaya bırakılmış bir emir belgesi olarak; “Başla” ve “Kayıttan devam”. |
| K8 | Stil sayfası | Renkler, tipografi, malzemeler, düğme/şalter durumları (normal, üzerinde, basılı, devre dışı, yetersiz kaynak), harita bölge durumları, sektör durum işaretleri, oda durumları. |

**Yön önerileri** (daha iyisini bulursan gerekçesiyle öner):
- **Kaynaklar masada:** üst HUD yerine masanın arka kenarında mekanik sayaçlar / rulolu sayı göstergeleri ya da pirinç
  plakalı bir defter. Ekran kenarında en fazla ince, okunaklı bir erişilebilirlik satırı kalabilir; kalırsa masa
  diliyle uyumlu olsun.
- **İstasyon seçimi masada:** alt düğme çubuğu yerine masanın ön kenarında dosya sekmeleri, pirinç anahtarlar ya da
  numaralı şalterler (1 / 2 / 3). Klavye kısayolları görünür kalsın.
- **VARDİYAYI BİTİR** tek ve ağır bir fiziksel kontrol olsun: kapaklı şalter, mühür ya da vardiya defterine imza.
  Basılınca odanın ışığı/sesi değişsin (hareket notu olarak tarif et).
- **Sığınak kesiti** bir mimari pafta / ozalit ya da raf üstünde kesit maketi; oda kartları kâğıt etiket veya kartvizit
  boyutunda dizin kartları. İnsanlar odalarda küçük fişler/figürler olarak görünsün.
- **Harita durumları** kâğıt ve kalem diliyle: işgal = kırmızı tarama, cephe = yağlı kalem halkası ve raptiye,
  özgür = silinmiş/temiz kâğıt + damga, aday komşular = kesik kalem çizgisi. Sektör ilerlemesi bölgenin yanında üç küçük
  işaret.
- **Görev fişi** haritadaki bölgeye kırmızı iplikle bağlı bir operasyon formu; durumlar damga ile (KİLİTLİ / HAZIR /
  GERİ ALINDI).
- **Geri bildirim** (“Emir uygulandı”, “Yapılamadı” ve gerekçesi) masaya düşen bir faks şeridi veya teleks satırı olarak.
- **Işık:** yeşil camlı masa lambasının sıcak dairesi, tavandan titreyen sarı ampul, CRT'nin soğuk yeşil sızıntısı.
  Bloom/neon yok; gölgeler sert ama karanlık okunabilirliği bozmasın.
- **Oda anlatısı:** havalandırma borusu, nem lekeleri, duvara iğnelenmiş kayıp ilanları ve kurtarılmış bölgeler listesi,
  bir köşede kahve fincanı ve kurşun kalem talaşı. Özgür bölge sayısı arttıkça odaya küçük umut işaretleri eklenebilir
  (öneri olarak göster).

## Kısıtlar

- **Motor:** Unity 6 URP, 3D sahne + **uGUI + TextMeshPro** (World Space ve Overlay Canvas). UI Toolkit yok.
  Özel shader, gerçek zamanlı bulanıklık ve post-processing'e dayanan efekt yok. 2D öğeler 9 dilimli sprite ile
  çizilebilir olmalı.
- **3D nesneler basit geometriyle** (kutu, silindir, düşük poligon) ve düz/az dokulu malzemeyle kurulabilir olsun;
  her önemli nesne için malzeme rengi, pürüzlülük ve gerekirse küçük bir doku/etiket tarifi ver.
- **Kamera:** üç poz ve başlangıç pozu korunur; nesneler o pozlarda okunur büyüklükte olmalı. Yerleşimi değiştirmek
  istersen kamera pozunu da öner.
- **Diller:** her metin Türkçe ve İngilizce gösterilebilir. İngilizce metin %30'a kadar uzun olabilir; taşma payı bırak.
  Türkçe karakterler (ğ, ı, İ, ş, ç, ö, ü) her fontta doğru görünsün. Yazı aileleri mevcut paketten: Courier Prime
  (daktilo), Libre Franklin (okuma), Kalam (yalnız el notları). Yeni font önerirsen gerekçe ver.
- **Okunabilirlik:** 1920×1080'de gövde metni en az 18 px, ikincil metin en az 14 px görünür boyutta. Renk körlüğü için
  durumlar yalnızca renkle değil, işaret/damga/desenle de ayrılsın.
- **Etiket hatası:** sektör türü bugün Türkçe arayüzde “LOOT” yazıyor; tasarımda **YAĞMA** / “LOOT” (EN) kullan.
- Neon, hologram, HUD çerçeveleri, bilim kurgu görünümü yok. Dünya kâğıt, mürekkep, pirinç, bakalit, beton ve tüp.

## İstenen çıktı

1. K1–K8 ekranları 1920×1080 (K8 serbest boyut). Her ekranda Türkçe metin; K2 ve K4'ün İngilizce varyantı da.
2. Her ekranın yanında kısa uygulama notu: nesne listesi (ad, yaklaşık boyut, malzeme), etkileşimli öğeler ve
   durumları, hareket/ses tarifi (süre ve sıra).
3. `tokens` (renk, yazı, boşluk, gölge) mevcut `tokens_sade.json` biçiminde; yeni token'ları ayrıca işaretle.
4. Kararsız kaldığın en fazla 3 nokta için iki seçenekli küçük karşılaştırma.
