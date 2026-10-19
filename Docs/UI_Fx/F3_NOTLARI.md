# Son Şalter · FX F3 uygulama notları

18 Ekim 2026. Kullanıcının “F3 e geç” isteğiyle F-Masa1 durağan masa fazı uygulandı. Sahne: `Assets/Corrigible/Fx/Scenes/FxDesk_Sandbox.unity`. Açıp Play'e basıldığında aynı Sade mock verileri FX görünümüyle gösterilir. Canlı DeskGame/Baskın geçişi ve F4 panelleri bu faza dahil değildir.

## Uygulama

- Aynı `map_regions.json` halkaları ve üçgenleri, `MapCoverFitter`, `SadeMarkerAnchor`, `RegionMeshGraphic` hit testi ve Sade VM/komut sözleşmeleri kullanılır. Sade kaynakları değiştirilmedi.
- `RegionFxGraphic` ×12 tek ortak `UI/FxRegion` materyaliyle çizilir. UV0 tasarım pikseli, UV1 bölge kimliğidir. `FxRegionLayer` yeniden kullanılan parametre dizisiyle düzey/bilinmeyen/karantina/seçim bilgilerini günceller; düzey değişimi geometriyi yeniden kurmaz. Renk doğrudan kürenin `DisplayedLevel` olayından gelir.
- 12 küre, güven/değer çipi, bölge adı; karantina dış hattı, dört yayılım ipliği, ekip rotası ve açık ok, üç jeton ve ETA, danışman uyarısı, ekran taraması/ışığı/vinyeti eklendi. Konum, tohum, yarıçap ve eğriler paket verisinden alınır.
- Fon/siyasi güç/hedef ve üç risk etiketi, zaman/hız denetimleri, beş sekme, LEJANT ve Son Şalter görünümü vardır. EN genişlikleri içerikle büyür. Harita cover ölçeği kullanırken işaretler, fontlar ve HUD piksel boyutunu korur.
- Harita 1,2 sn belirir. Küreler 0,25 + 0,05·i sn gecikmeyle kendi kanonik girişini başlatır; çip/ad/jeton girişleri ortak FX saatini kullanır. Dönen/ölçeklenen 28 işaretin pivotu merkezdir; üç saniyelik ilerlemede merkez kayması 0 olarak doğrulandı.
- `FxDeskInteraction`, saf `SadeInteractionState` ve mevcut komut niyetlerini kullanır. Bölge/kara/deniz seçimi, hız, sekme, Esc ve kısayollar Play'de denenmiştir. Son Şalter üç saniye sonunda yalnız onay isteği üretir; gerçek silme komutu veya onay penceresi F4'ün sorumluluğudur.
- `Prefabs/Desk/` altında düğüm, HUD ve harita işaretleri kayıtlıdır. Tüm çalışma referansları sahne/prefab'da serileştirilir. Tek seferlik sahne kurucusu çalıştırıldıktan sonra projeden kaldırıldı; çalışma anında hiyerarşi üretilmez.

## Ortak çizim ve renk

Küçük durağan görseller aynı materyal/dokuya göre `FxGraphicBatch` mesh'lerinde toplanır. Kaynak bileşenler metin/yerleşim/olay bağlantılarını taşımaya devam eder; saydam hit görselleri tıklama hiyerarşisini korur. Dönüş/nabız döngüleri ve küre RawImage'ları yerel kalır. Kaynak mesh'ler Canvas çizim geçişinden önce hazırlanır; Canvas geçişi sonunda kaynak çizimleri kapatılarak çift çizim engellenir. Doldurma, metin ve geometri değişiklikleri dirty callback ile izlenir. Bu gruplama yalnız F3'ün yazılmış tam ekran katmanları içindir; F4'ün farklı kırpma alanları tek gruba karıştırılmamalıdır.

F3 için ayrı `FxDeskTheme`, gama kompozit UI/line/additive materyalleri ve atlas içinde 2×2 beyaz yardımcı sprite bulunur. F1'in teması korunur. Linear projede vertex renkleri ve dokuların gama dönüşümü ayrı ele alınır. Yeni URP/Volume/Bloom veya paket yoktur.

Paylaşılan küre yolunda varsayılanı 1 olan `_Brightness`, görüntü rect'ine göre RT boyutu, varsayılanı 0 olan giriş gecikmesi ve GPU renk yardımcı fonksiyonunda inline iyileştirmesi eklendi. Kanonik matematik, RNG tüketimi ve katman sırası değişmedi; 22 altın kare yeniden geçti.

## Doğrulama

| Kontrol | Sonuç | Kanıt |
|---|---|---|
| Tüm Corrigible EditMode testleri | **400/400**, atlanan yok | `captures/F3/editmode-results.json` |
| Yeni F3 testleri | 6: renk örnekleri, 12 düğüm ofseti, iplik uçları, parametre/GC/mesh, ortak grafik görünürlüğü ve doldurma, shader | Aynı rapor |
| Play kontrolleri | **44/44**: 12 düğüm, kara/deniz, hız, Space, sekmeler, Esc, Tab/R, hover, düzey senkronu, bilinmeyen, karantina, basılı tutma, TR/EN taşma | `captures/F3/play-qa.json` |
| Hareket ek kontrolleri | 28 merkezde kayma 0; azaltılmış hareket bir saniyede 8 kare | `captures/F3/extra-qa.json` |
| 22 küre altın karesi | **22/22**; fark 0,74–0,95/255; kenar %95,7–97,8 | `captures/orb/comparison.json` |
| TR/EN ve oranlar | Her dilde 1920×1080, 1920×1200, 2560×1440, 3440×1440; taşma 0 | `captures/F3/F-Masa1_*`, `aspect-contact.png` |
| F-Masa1, küreler maskeli | Ortalama fark **5,92/255**, kenar örtüşmesi **%80,9** | Fark/kenar/yanıp sönme görüntüleri |
| Açılış | 68 kare, 10 fps; 0,025 sn simülasyon adımı; MP4 çözülerek **6,80 sn** doğrulandı | `captures/motion/F3_masa_acilis.mp4`, `motion-timing.json`, `motion-contact.png` |

Maskeli ekran karşılaştırması piksel eşitliği değildir: TMP/tarayıcı font rasterizasyonu, glow ve ülke/karantina çizgisi AA'sı farklıdır. Küre altın eşikleri yalnız küreler için uygulanır. Değer çiplerinin x konumunda JPG/TSV'deki sol kenar esas alındı; brifin “üst-orta” ifadesiyle farkı `KARARLAR.md`'de kayıtlıdır. Statik olmayan ışık taraması referans yakalamada gizlenir.

## Performans

Apple M4 Pro, Unity 6000.3.12f1 Editor/Metal. Oyuncu derlemesi veya Steam/Windows ölçümü değildir.

| Ölçüm | Sonuç |
|---|---:|
| Durağan masa UI batch | **33**, hedef ≤40 |
| Durağan Profiler kaydı | 31 kare; betik GC **0 B** |
| Canvas.BuildBatch | 31/31 kare, ortalama 0,012 ms |
| 12 F3 küresi, aynı karede model + çizim | Ortalama **2,398 ms**, %95 **2,510 ms**, 80 ölçüm karesi |
| Küre sıcak döngüsü | **0 B** |
| Tüm FX saati + Canvas güncellemesi | Ortalama **2,641 ms**, %95 **2,917 ms**, **0 B**, 80 kare |
| 12 küre RT toplamı | **2.519.652 bayt = 2,403 MiB** |

Küre ortalaması 2,5 ms hedefinin altında; %95 değeri hedefin 0,01 ms üzerindedir. İlk aynı-veri ölçümü 2,588 ms idi; GPU renk yardımcı fonksiyonunun inline edilmesinden sonra yukarıdaki değer alındı. RT bütçesi font/harita/StructuredBuffer içermez. 12 × 5.120 × 112 bayt GPU ilkel tamponu ayrıca yaklaşık 6,56 MiB'dir. Açık F4 panelleri, 19 küreli rehber ve oyuncu derlemesi daha sonra ölçülmelidir.

Unity'nin yerel Profiler `Dispatcher::AcquireFreeBuffer / EmitUIEvents` yolunda iki Editor çökmesi yaşandı. Kaydedilmiş sahne korundu; yeni Editor oturumunda tek kısa kayıt başarıyla alındı. Çökme izi `profiler-crash.txt` içindedir. Bu aşamada aynı oturumda peş peşe `DeskPerfProbe.Run` çağrılmamalıdır. Görsel saat/Canvas için bağımsız sıcak döngü ölçümü de yukarıda ayrıca raporlanır. Bu teşhis çağrı yığınına dayanır; kesin motor kök nedeni saptanmış değildir.

## Kullanım ve sonraki faz

`FxDesk_Sandbox` sahnesini açıp Play'e bas. 1×/2×/4× ve duraklatma, Space, R/N/T/L/H, Tab ve Esc kullanılabilir; sekmeler F3'te görsel niyet durumunu değiştirir. Bölge kartı/çekmece/ipuçları henüz açılmaz. İleride canlı adaptör `Bind(SadeDeskViewModel, IDeskCommands)` sınırına bağlanabilir; gizli simülasyon alanlarına doğrudan erişilmez.

`FxDeskCapture.Capture(path,width,height)` dört oranı aynı karede cover/HUD/ortak mesh'lerle yakalar. Yeniden altın doğrulama için F2 notlarındaki `OrbCapture.CaptureAll()` ve `Tools/Fx/compare_orbs.py` kullanılabilir.

F4: F-Masa2–9 bölge kartı, beş çekmece, düğüm ipucu, lejant, tam dosya, onay, illüzyon ve eylem bağlantıları. F4 başlatılmadı. Başlangıç sahnesi/ayarlar/profil geri yükleme sonucu `captures/F3/restoration.json` ve `final-state.json` içinde tutulur. Commit/push yapılmadı.
