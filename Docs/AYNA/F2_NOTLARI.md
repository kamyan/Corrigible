# AYNA · F2 uygulama ve doğrulama raporu

19.10.2026 · **F2 uygulaması ve kendi kabul ölçütleri doğrulandı; makro denge açık, F3 başlamadı.**

Kullanıcı F2 geçişini ve F1 raporundaki üç ölçüt düzenlemesini onayladı. Aynı mimari puanında beraberlik yarım puandır; Silme oranı F2'de ölçülür; güçlü Mind için Son Şalter yalnız %15 üst sınırı ve ayrı eşik/kural doğrulaması taşır. Önceki ham raporlar değiştirilmedi.

## Kurulan sistem

| Dosya / alan | İşlev |
|---|---|
| Core/BoardTypes.cs | Salt okunur tahta görünümü, komut/kurulum DTO'ları, sonuç ve özel durum |
| Core/BoardTemplates.cs | Güney Asya örneği dahil dört el yapımı şablon × sekiz dönüşüm |
| Core/BoardSimulation.cs | 8×8, altı tur; hareket/eylem; on birimin aktif/pasif özellikleri; tuzak, ilk vuruş, düğüm, çekilme, dolap; önizleme/undo |
| Core/BattleFlow.cs | Dünya çözümünü savaşta bekletme, kadro, otomatik/komuta seçimi ve sonucu tek kez uygulama |
| Core/AynaRecord.cs | Format/kurallar sürüm 2; niyet/tuzak/komut/undo/sonuç dahil savaş ortası replay; sürüm 1 desteği |
| Mind/BoardPlanner.cs | Görüşten inanç kopyası, dar beam, oyuncu cevabı/ilan edilen niyetlerin benzetimi; Habit/Random ve TrapPlanner |
| Mind/MacroPlanner.cs | Görünür çekirdeğe yeterli birlikle yapılan taktik baskının stratejik değeri |
| Editor/AynaBoardBench.cs | Rol eşli tahta deneyleri, üçgen, kalibrasyon, tuzak, p50/p95; JSON/Markdown |
| Editor/AynaSelfPlay.cs | Tactical seçeneğiyle tam oyun, kadro/tahta akışı, gerçek dolap kırılma sayacı; menü ve CLI |
| Tests/EditMode | Bütün yetenekler, gizlilik, belirlenimcilik, önizleme/undo, çoklu savaş, kaynak/gelir ve replay |

Bütün uygulama bağımsız `Assets/Corrigible/Ayna/` altındadır. Eski Core/Game/UI/Desk/Fx, sahneler, prefab'lar, proje ayarları ve çeviri tablosu değişmedi. Oyuncuya görünen yeni oyun ekranı bu fazda yoktur; uGUI debug sahnesi ve VM bağlantısı F4 kapsamındadır.

## Kurallar ve akış

- Tur başına bir hareket + bir eylem. Izgara hareketi ortogonal, uzaklık Manhattan; alan etkileri 3×3. Hava ara engel aşar fakat üstünde duramaz. Örümcek raf aşar; sivil sunucudan geçemez. Dolap geçilmez ve görüş hattını keser.
- Sınıf bonusu bir kez uygulanır. Muhafız zırhı hasarı 1 azaltır, en az 1 kalır. EMP cevap sırasında mevcut niyet evresini, niyet sırasında sonraki cevap evresini etkiler; hava 2, kara 1 tur sersemler. Siber bağışıktır.
- Sızma geçici Controller değiştirir, özgün sahipliği değiştirmez. Planlayıcı ele geçirilen robotun o turdaki hareket/eylemini de kullanır. Röle anında düğüm alır ve yakındaki dosta +1 hareket verir. Yem menzildeki hedefli niyeti kendine çeker. Hayalet Emir yalnız başka geçerli hedefe yönlendirir.
- Normal düğüm tur sonunda üstünde duran tarafa geçer. İki taraf da bitişikse eski sahip korunur; Röle anında alır. Dünya sunucu talepleri aynı başlangıç görüntüsünden netlenir: 3/2 sunucu ve 2/1 düğüm sonucu 4/1'dir.
- Kadro 3, Hareket 3 ile 4. Seçilenler konuş yuvalarına kararlı birim kimliğiyle yerleşir. Yedekler hasar almaz. Gerçek hasar ve kayıplar dünyaya geri yazılır.
- Karşılaşmalar ve savunma ordusu olmayan çekirdek baskınları komuta açılır. Başlangıçta hiç savunma birimi bulunmaması, canlı dolabın otomatik yenilmesi sayılmaz. Dolap 5 can/zırhlıdır; kırılması kaçışsız Silme üretir. Normal birliksiz bölgeler duvar otomatiğidir; tasarlanmamış taret eklenmedi.
- `BeginCycle` savaşta bekler. Bütün savaşlar bitmeden gelir/eğitim/farkındalık uygulanmaz. `CompleteBoard` sonucu yalnız bir kez işler. İlk vuruş, ilk niyetten önce ek cevap evresidir. Son öldürme/dolap komutundan sonra sonuç onaylanana kadar undo yapılabilir.

## Adalet, önizleme ve kayıt

Mind canlı Dünya/Tahta durumu kabul etmez; yalnız SideView/BoardView ve kendi girdilerini alır. Gizli tuzak konumu/sayısı cevap görünümünde bulunmaz. Gözcü üç kare çevresini, Görü 3 bütün tuzakları açar. Niyetler açıktır: brifte niyeti gizleyen bir eylem tanımlanmadığından yeni bir gizleme yeteneği uydurulmadı.

Tahta önizlemesi yalnız oyuncunun görünümünden kurulur. Bilinmeyen tuzak hasarı/durma karesi önizleme üzerinden sızmaz; bilinen tuzaklar, kendi Görü 3 bilgisi ve görünür birikmiş gürültü korunur. Harita emir önizlemesi de bilinmeyen rakip varlığını kabul/ret farkıyla açığa çıkarmaz: kesin görüş dışındaki Saldır olasıdır, gerçek koşul ÇÖZ'de denetlenir. Gerçek komutun undo'su ise tam durumu geri alır. Önizleme dünya/RNG/kayıt değiştirmez.

Kayıt iki dış makro planı, savaş seçimini, tuzakları, niyetleri, tahta komutlarını, undo/çekilme ve sonuç onayını saklar. Replay AI'yı yeniden çalıştırmaz. Savaş ortası ve iki savaş arasındaki çözüm imleci yeniden kurulur. Sürüm 1 kayıtları kendi denge görüntüsüyle okunur; bozuk sürüm/girdi/döngü tutarsızlığı reddedilir. F2 disk oyuncu profili kullanmaz/güncellemez; kalıcı öğrenme F3'tedir.

## Denge ve performans kararları

Son aday: Gözcü/Muhafız hareketi 3; Sızma/Röle/Hayalet Emir canı 3, Yem canı 2; Nişancı özel hasarı 1. Diğer can/maliyetler ve sınıf bonusu 1 korundu. Tablolar AynaBalance'tadır. Önceki can/hareket/bonus denemeleri `triangle_tune/` altında ham olarak tutulur.

Makro planlayıcı taktik modda, yalnız görünür rakip çekirdeğinde en az iki birimle başlatılan savaşa değer verir (ScoreCoreRaid=36). Bu İşlem/fon ödülü değildir; otomatik modda kapalı karar sezgisidir. Tahta planlayıcı dolaba yaklaşmayı da değerlendirir.

Sıcak doğrulamada Enum.IsDefined yerine aynı ardışık, sürümlü kimlik kümesi aralıkla denetlenir. Denge Core içinde değişmez; özel görüntüler tabloyu paylaşır, dış BalanceSnapshot daima derin kopyadır. Makro adayları aynı fonksiyon-içi denge kopyasını kullanır. İlk 54 otomatik oyunda sonuçlar birebir aynı kaldı; kutulama/yansıma kaldırılınca süre 71,08 → 46,15 sn oldu. Karar bütçesiyle ek kalite/hız karşılaştırması ayrıca ölçülür.

## Kabul ölçümleri

| F2 ölçütü | Hedef | Sonuç | Durum |
|---|---:|---:|---|
| Yetenek, belirlenimcilik, undo/önizleme ve diğer testler | Hepsi başarılı | 554/554 | Geçti |
| Kara > Hava | %60–75 | 39/64 = %60,94 | Geçti |
| Hava > Siber | %60–75 | 45/64 = %70,31 | Geçti |
| Siber > Kara | %60–75 | 44/64 = %68,75 | Geçti |
| Auto/tahta kova sapması | ≤10 yüzde puan | En çok 6,58 | Geçti |
| Habit tuzak isabeti | ≥%40 | %100 | Geçti |
| Random tuzak isabeti | ≤%20 | %12,5 | Geçti |
| Tahta planlayıcı p50 | ≤30 ms | 3,61 ms (p95 8,80) | Geçti |
| F2'ye taşınan Silme oranı | ≥%10 | 26/180 = %14,44 | Geçti |

Son koşu: **1.992 tahta + 64 ısınma**, 111,49 saniye. Kalibrasyon: 32 yeni tohum × 25 kadro çifti × iki rol = 1.600; buna 192 sonlu üçgen düzeni eklenir. Kova 3/4/5/6 için n=320/320/704/448; sapmalar 6,58/4,09/0,87/4,60 puandır. Ayrı tuzak deneyi 100 Habit + 100 Random'dur. [Ham rapor](denge/F2/final_calibrated_boards/20261019_140143_094.json), [okunur rapor](denge/F2/final_calibrated_boards/20261019_140143_094.md).

500 otomatik tam oyun: **91,70 saniye**, ortanca **27 döngü**, %99,6 oyun 40'tan önce bitti; ortalama **7,09 savaş**. Makro plan p50/p95 **3,06 / 5,15 ms**. Sonuçlar: 382 Tekillik, 113 Üstünlük, 3 Son Şalter, 2 süre sonu.

Aynı mimari puanları: Kale %47,32, Hayalet %54,55, Sürü %51,79 (beraberlik yarım puan). Bu üç hedef geçti. **Hayalet/Sürü eşleşmesinde Sürü 38/55=%69,09** kazandı; %65 üst sınırı hâlâ açık. Ters eşleşmede Sürü 36/56=%64,29. Tam makro denge kabulü verilmedi. [500 oyun ham raporu](denge/F2/final_auto/20261019_140540_518_Mind_Mind_500.json).

180 taktik tam oyun: **287,70 saniye**, ortanca **26 döngü**, bütün oyunlar 40'tan önce bitti; ortalama **8,09 savaş**. Makro plan p50/p95 **14,05 / 39,57 ms**. **26 Silme (%14,44)** var ve **26'sı da gerçek çekirdek dolabı kırılması**; F2'ye taşınan ≥%10 Silme hedefi geçti. Diğer sonuçlar: 95 Tekillik, 58 Üstünlük, 1 Son Şalter. [Ham rapor](denge/F2/final_tactical/20261019_164721_662_Mind_Mind_180.json).

Taktik tam oyunda, eşleşme başına 20 örnekte aynı mimari puanları Kale %27,5, Hayalet %37,5, Sürü %32,5 çıktı; Kale/Kale ve Hayalet/Sürü eşleşmelerinde ikinci taraf %70 kazandı. **Bu makro hedefler geçmedi.** Rol/başlangıç etkisi ve mimari dengesi ayrıca incelenmeli; 20 oyunluk eşleşmelerin Wilson aralıkları ham raporda bulunur. F2 tahta kabulünün geçmesi bütün oyunun dengeli olduğu anlamına gelmez.

Referans botlar: Random **90/90=%100** (9,08 sn), Greedy **65/90=%72,22** (37,98 sn). İki kazanma eşiği de geçti. [Random raporu](denge/F2/final_bots/20261019_165209_394_Mind_Random_90.json), [Greedy raporu](denge/F2/final_bots/20261019_165218_494_Mind_Greedy_90.json).

Özet: F2'nin yetenek/belirlenimcilik/undo/önizleme, sınıf üçgeni, kalibrasyon, tuzak ve tahta hız ölçütleri; ayrıca taşınan Silme hedefi geçti. F1'den taşınan makro mimari denge işi açık kalır. Bu açık işler geçer olarak işaretlenmez.

## Deneyin sınırları

Üçgen kohortu sabit üç birimli kadrolar için dört şablon × sekiz dönüşüm × iki niyet rolünün tamamını (64 düzen) sayar. Bu sabit düzenlerde tohum değiştirmek bağımsız yeni savaş üretmez. Önce/sonra sorgu iyileştirmesinde aynı 192 tahtanın sonuç, düğüm, tur ve rotaları birebir eşitti: [query_equivalence.json](denge/F2/query_equivalence.json).

Kalibrasyon, eş becerili iki Balanced botun kadro matrisi ve üçgen kohortundadır; farklı becerili Habit/Random tuzak kohortu ayrı tutulur. Yalnız örnek bulunan olasılık kovaları doğrulanır; bu, bütün olası hasarlı/4 birimli kadroların tek tek doğrulandığı iddiası değildir. AutoUtility sabit kaldı; TriangleStrength 0,35→0,70 oldu. Çift tohumlu eğitim kümesinde sabit 0..1,5 / 0,05 ızgarasıyla en büyük kova hatası en aza indirildi. İlk tek-tohum keşif kontrolünde 10,92 puan sapma görüldü; bu sınır dışı sonuç saklandı. Ardından farklı 271026 başlangıçlı 32 tohumla ölçülen daha geniş doğrulamada bütün dolu kovalar ±10 puan içinde kaldı. Üçgen düzenleri yeni bağımsız örnek sayılmaz. [Katsayı seçimi](denge/F2/triangle_calibration.json). Serbest birim gücü ve sıcaklık aramaları doğrulamada güvenilir olmadığı için taşınmadı; ham denemeler saklandı.

Tuzak modeli dört şablonu ayrı tutar. Isınmada 32 Habit ilk iki tur geçişi ve 32 ayrı Random tam geçişi gözlenir; kanonik ısı, genel geçiş sıklığına bölünür. Test tohumları ayrıdır; 100 Habit + 100 Random oyunda isabet = tetiklenen / yerleştirilen tuzak sayısıdır. Sabit bot, sabit kanattan karşı kenara ilerler. İki ham ısı dizisi ve tuzak hücreleri JSON'da saklanır. Bu deney kalıcı F3 öğrenmesi sayılmaz. Tohum/oyun kümeleri birlikte örneklenerek 2.000 tekrarlı bootstrap aralıkları da çıkarıldı: Random tuzak isabeti %95 örnekleme aralığı %8–17,5; kalibrasyonun işaretli kova hata aralıkları [final_intervals.json](denge/F2/final_intervals.json) içindedir. Bunlar sabit bot/kadro/harita kataloglarına aittir; yeni kadrolara veya insan davranışına güven aralığı değildir. Aynı şablon/dönüşüm farklı tohumlarda tekrar edebilir; her tohum benzersiz bir taktik konum sayılmaz.

İlk uzun birleşik CLI çağrısı 600 sn üst sınırına ulaştı; çalışan benzetimler ham raporlarını tamamladı. Tamamlanan dosyalardaki oyun sayıları kontrol edildi. Eski başarısız hız/denge ölçümleri silinmedi. Son EditMode çağrısı da CLI tarafında 30 sn zaman aşımı verdi; Unity test koşusu XML raporuna göre 20,02 sn içinde 554/554 başarılı tamamlandı (14:04:30–14:04:50 UTC). Sonuç dosyası incelendi ve ayrı kopyalandı; başarılı testler sırf CLI yanıtı gelmedi diye tekrar çalıştırılmadı.

## Çalıştırma

Play kapalı, açık sahneler temiz olmalıdır. Menü: **Corrigible > AYNA > Tahtayla kendi kendine oynat**. İşin durumu/iptali mevcut Start/Status/Cancel API'sindedir. Tahta deneyi: **Corrigible > AYNA > Taktik denge ölçümü**.

```bash
unity command eval_file --file Tools/Ayna/f2_bench.cs 600000 --timeout 660 --caller plugin --skill unity-cli --format json
unity command eval 'return Corrigible.Ayna.EditorTools.AynaSelfPlay.Run(new Corrigible.Ayna.EditorTools.SelfPlayOptions { Games = 180, Tactical = true, OutputDirectory = "Docs/AYNA/denge/F2/yeni_tam_oyun" });' 600000 --timeout 660 --caller plugin --skill unity-cli --format json
```

## Son teknik doğrulama ve veri koruma

- **554/554 EditMode testi geçti:** 109 AYNA + mevcut oyunun 445 testi. [Son test özeti](denge/F2/testler_son_tur.json).
- Son derleme başarılı; Core/Mind assembly'leri `noEngineReferences: true`; Core/Mind içinde UnityEngine, float, DateTime veya System.Random yok. 27 C# dosyası ve bütün Unity meta dosyaları mevcut. Açık DeskGame sahnesi temiz, Play kapalı; son Editor kontrolünde Release ve Profiler kapalı. Konsolda son derleme sonrası yeni hata yok. Son test ve beş final raporun kod hash'i mevcut kaynakla aynı: [teknik denetim](denge/F2/son_teknik_denetim.json).
- Kullanıcı 16:45 civarı Play oynadığını doğruladı ve yeni kaydı korumamı istedi. Yeni **savegame.json ve Unity tercihleri geri alınmadı**; korunması istenen yeni sürümle hash/zaman damgası eşleşiyor. Diğer profil/kampanya/AYNA dosyaları başlangıç sürümüyle aynı. Yalnız testin ürettiği TestResults.xml, aynı test sürümünün hash/zamanı doğrulanarak eski kopyasına döndürüldü. [Veri denetimi](denge/F2/veri_koruma.json).
- Önceki otomatik onay servisi kullanım sınırı nedeniyle XML geri yüklemesini bir kez çalıştıramadı; kullanıcı “devam et” dedikten sonra aynı onay kontrolü başarılı oldu ve işlem tamamlandı.
- Eski izlenen proje dosyalarında değişiklik yok; commit/push yapılmadı. Yeni faz dosyaları diskte incelemeye hazır.

## Açık karar ve öneri

F2 uygulamasını tamamlamak için bekleyen yeni bir kural sorusu yok. Genel dengeyi tamamlanmış saymıyorum: önerim F3'te öğrenme sistemi kurulduktan sonra mimari/başlangıç/rol etkilerini F5'in auto/taktik matrisiyle birlikte ele almak. Bu, mevcut sapmaları kabul hedefinden silmez. Kararlar K35–K59'da izlenir; K56 otomatik katsayı, K57 ölçüm örneklemesi, K58 kullanıcı kaydını koruma, K59 son kabul/sapma özetidir.

## Sonraki faz

F3: oyun içi/oyunlar arası öğrenme, sürümlü kalıcı profil, tahmin/Sürpriz/Okunabilirlik ve öğrenme düzeyleri. Mevcut makro mimari dengesi raporları korunur; F2 tahta kabulü makro dengedeki bütün sapmaların kapandığı anlamına gelmez. Kullanıcı uygulama talimatı §6–7 uyarınca F3 ayrıca onay ister.
