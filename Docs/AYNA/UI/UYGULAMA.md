# AYNA · Tasarım paketinin Unity uygulaması

20.10.2026. Kaynak: Docs/SonSalter_AYNA_Tasarim (A1–A12, ölçüler, tokenlar, hareket ve TR/EN tablosu). Yeni oynanabilir sahne: **Assets/Corrigible/Ayna/Scenes/Ayna.unity**. Unity menüsü: **Corrigible > AYNA > Oyunu aç**; sonra Play.

## Uygulanan ekranlar

| Kare | Unity karşılığı |
|---|---|
| A1 | Üç mimari kartı, 12 başlangıç bölgesi, tohum, öğrenme düzeyi, mevcut nesil/profil bilgisi |
| A2 | 12 bölgenin gerçek coğrafyası, dönen küreler, sahiplik ve bilgi çipleri, güncel HUD, bölge kartından yedi emir, miktar/birlik seçimi, kuyruk/geri alma |
| A3 | Aynı evrede iki tarafın açığa çıkan emirleri, doğrulanmamış sinyaller ve gerçek Sürpriz sonucu |
| A4 | Bekleyen karşılaşma, komuta/otomatik çöz ve tamamlanan döngü özeti |
| A5 | Can/kapasite bilgili kadro seçimi, bilinen ve bilinmeyen rakip, yuva sınırı |
| A6 | İzometrik 8×8 holo tahta, on birim türü, seçim ve yasal hücreler, görünür niyetler/tuzaklar, saf önizleme, uygulama, geri alma, tur sonu/çekilme |
| A7 | Son tahta, gerçek düğüm sonucu ve kazanan, haritaya dönüş |
| A8 | Beş aile × üç kademe, gerçek bedel/süre/veri gereksinimi ve kuyruk durumu; Tekillik ilanı |
| A9 | Birlik seçimi, üretim, onarım, yeni birlik ve haritada seçilmiş komşu bölgeye yürüme |
| A10 | Kilitli üç tahmin, doğrulanmış emir dağılımı, okunabilirlik eğrisi, tahta ısı izi |
| A11 | Tekillik ilerlemesi ve yalnız görünür bilgilerden türeyen olası veri bölgeleri; kapanabilen bildirim |
| A12 | Gerçek bitiş türü/kazanan, tahmin karnesi, Sürpriz sayısı, nesil ve profil yazım durumu, okunabilirlik eğrisi, yeni oyun |

Günlük/rehber, kaydırılabilir emir kuyruğu, TR/EN, hareketi azalt, kaydet/yükle, ana menü ve iki adımlı hafıza silme var. Silme onayı Vazgeç/Esc ile denetleyicide de iptal edilir. Savaş sırasında menü klavye girdilerini durdurur ve arka plana tıklamayı engeller.

## Kaynaklar ve sınırlar

- Esas kaynak kaydedilmiş sahne ve SerializeField referanslarıdır. Sahne Unity API ile tek seferlik editör kurucusunda oluşturuldu; kurucu çalıştırıldıktan sonra kaldırıldı. Çalışma anında sahne/UI kuran sistem yok.
- AynaInterfaceView ekran akışını; AynaMapView/Graphic/Geometry ve OrbGraphic coğrafyayı; BoardView/Graphic tahtayı; DrawerView, TrendGraphic ve HeatGraphic çekmeceleri çizer. InterfaceTheme kendi renk/font asset’idir.
- Coğrafya paketin SVG konturundan sadeleştirildi; gerçek bağlantılar oyun kataloğundan gelir. Paketin önerdiği yeni bağlantılar oyun kuralına eklenmedi.
- Küreler ve holo parçalar bağımsız uGUI vertex çizimleridir. Fx’in eski Desk/Game bağımlılıkları yeni modüle eklenmedi. Görsel dil uygulandı; Fx küre motoruyla piksel eşitliği iddiası yok.
- Örneklerdeki +3 Sürpriz, eşik, üretim fiyatı, %58 savaş tahmini, sabit nesil ve uydurma öğrenme cümleleri canlı kurala dönüştürülmedi. Ekranlar güncel görünüm modellerini gösterir; bilinmeyen rakip ve gizli tuzak saklı kalır.
- Araştırma süreleri, birim azami canları, Tekillik/üretim sınırları ve Hakimiyet hedefi oyuncunun güvenli SideView/BalanceSnapshot verisinden sunuma eklenir. Core, Mind, RNG sırası ve denge değerleri değişmedi.
- Chakra Petch, IBM Plex Sans/Mono ve semboller AYNA’ya ait statik TMP atlasları kullanır. Eski fontlar ürün değişikliği olarak kaydedilmez.
- 16:9, 16:10 ve 21:9: köşelere bağlı HUD, cover harita/sabit boyutlu işaretler, üst HUD kaçınması, kesilmeyen tahta. Küçük ekranlarda ölçekleme CanvasScaler üzerinden.
- Hareket: dönen küreler, tahmin daktilosu ve panel solması; hareketi azalt seçeneği bunları sabitler. Paketin bütün sinematik mikro hareketleri ve P1–P3 tasarım katalogları ayrı oyun ekranı olarak eklenmedi.
- Yeni paket, Build/Player/Quality/URP ayarı, eski oyun sahnesi veya kural değişikliği yok. Mevcut AynaDebug sahnesi korunur. F5’teki genel denge açıkları bu arayüz işiyle çözülmüş sayılmaz.

## Doğrulama

**618/618 EditMode testi geçti** (173 AYNA + eski oyunun 445 testi), 28,82 saniye. CLI 30 saniyede yanıt zaman aşımı verdi; bitmiş NUnit XML sonucu geçişi doğruladı. Son test sonucu [tests.json](tests.json). Yeni testler sahnenin bütün gerekli referanslarını/eksik scriptleri, 64 izometrik hücrenin tıklama dönüşümünü, güncel dengeye bağlı gösterim değerlerini ve hafıza silme iptalini sınar. Mevcut eski oyun testleri de çalıştırılır.

Canlı kanıtlar:

- [Planlama kaydet/yükle](acceptance_live.json): kayıt birebir eşit.
- [Tahta](board_acceptance.json): önizleme saf, hamle uygulanıyor, geri alma tahtayı birebir geri getiriyor, savaş ortası yükleme eşit, savaş bitiyor.
- [Gerçek oyun sonu](live_end.json): döngü 28, AYNA Tekillik, profil nesil 1; tekrar devam profil neslini çoğaltmaz.
- [A11 fixture](declaration_fixture.json): ilan örneği yalnız görünüm modelinde gösterildi; gerçek oyunun kayıt görüntüsü değişmedi. Bu görüntü canlı Tekillik ilanı koşusunun yerine sunulmaz.
- [TR planlama](A2_TR.png), [EN planlama](A2_EN.png), [tahta](A6_TR.png), [gelişim EN](A8_EN.png), [oyun sonu](A12_TR.png).
- [21:9 harita](A2_2580x1080.png), [16:10 harita](A2_1920x1200.png), [21:9 tahta](A6_2580x1080.png), [16:10 tahta](A6_1920x1200.png). Çekimler hareket azaltılmış durumdadır.
- Kullanıcı dosyaları ve eski font yan etkileri için son denetim user_file_audit.json içinde kaydedilir.


## Son teslim denetimi

Sahne Ayna.unity açık, Play kapalı, kaydedilmiş ve temiz; eksik script 0, boş gerekli referans 0 ([scene_audit.json](scene_audit.json)). Son canlı konsolda derleme/çalışma hatası ve uyarı 0. Tek seferlik kurucu kaldırıldı.

Kullanıcı çalışma sırasında eski Son Şalter’i oynadığını ve son durumu korumamızı doğruladı. Normal savegame.json’un silinmiş olması ve player_profile.json’un yeni hâli korundu; başlangıç yedeği geri alınmadı. Normal AYNA dosyaları oluşturulmadı. Yalnız kendi TestResults.xml çıktısı hash/zaman eşleşmesiyle temizlendi. Üç eski dinamik fontta oluşan otomatik serileştirme farkları temizlendi; kendi statik fontlarına eksik noktalama ve semboller eklendi. Kaynak ve kullanıcı dosyası denetimleri bu klasörde saklanır.

[Son oyun sonu kontrolü](end_acceptance.json): ilan paneli kapalı, gerçek bitiş açık, profil ikinci kez birleştirilmedi. [Son eylemler](final_actions.json): silme iptali çalışır, Hakimiyet hedefi mevcut %60 kuralından 44 sunucu olarak hesaplanır.
