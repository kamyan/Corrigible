# Son Şalter · FX F2 uygulama ve doğrulama notları

18 Ekim 2026. Kullanıcının “F2 ye geç” talebiyle uygulandı. F2 tamamlandı; F3 başlatılmadı.

## Teslim

- `Assets/Corrigible/Fx/Scenes/FxOrbLab.unity`: F-YZ laboratuvarı. Üç sabit hâl, canlı 0–100 ölçeği, otomatik 18 saniyelik geçiş, dil/hareket/donma denetimleri ve yalnız Editor/Development build'de geliştirici paneli.
- `Prefabs/Orb/OrbView.prefab`: RawImage, OrbView ve OrbRenderer; bağlantılar serialized. Sahne tek seferlik editör aracıyla kaydedildi; kurucu kaynak ve meta dosyası kaldırıldı.
- `Scripts/Orb/`: Unity referansı olmayan `Corrigible.Fx.Orb` assembly'si. Kanonik `art/orb/fx_orb.js` portu; double aritmetik, ayrı mulberry32, icosphere, kabuk/enlem parçacıkları, kristal, kafes, lif, ark, kıvılcım ve dört tür.
- `Scripts/Runtime/Orb/`: tek FxClock'a bağlı OrbClock; görünürlük, RT ömrü, kare paylaşımı, hız uyarlaması, gösterilen VM değerini kabul eden OrbStateMapper. Core/Prototype/oyun RNG'sine erişim yok.
- GPU çizimi tek StructuredBuffer + DrawProcedural geçişi kullanır. Köşe kutuları GPU'da hesaplanır; CPU'nun her ilkel için dört köşe kopyalaması kaldırıldı. Varsayılan SS 1,5; harita RT üst sınırı 512, laboratuvar 1024. Composite stencil ve RectMask2D'yi destekler.
- `Settings/OrbRenderSettings.asset`: RT/hız/maliyet ayarları. 40 fps üst sınır, 14 ms üzerinde kademeli 16 fps'e düşüş, azaltılmış harekette 8 fps. Gizli/ekran dışı kürenin zamanı durur; geri dönüşte intro tekrarlanmaz.
- Yeni TR/EN metinleri çeviri tablosuna işlendi. FX UYUMLU çevirisi paketteki COMPLIANT ile düzeltildi. İngilizce STATE başlıkları uzunluklarına göre yerleşir; LIVE TRANSITION'ın harf aralığı dilde daralır.

## Kanıtlar

| Denetim | Sonuç | Dosya |
|---|---|---|
| Tüm Corrigible EditMode testleri | **394/394 geçti**, atlanan yok | `captures/F2/editmode-results.json` |
| F2'nin yeni testleri | 32 sayısal parite + 5 entegrasyon; önceki 357 test de geçti | Aynı rapor |
| Altın kareler | **22/22 geçti**; ortalama renk farkı **0,74–0,95/255**, kenar örtüşmesi **%95,7–97,8** | `captures/orb/comparison.json`, `contact.png` ve 22 PNG |
| Altın kabul eşiği | Ortalama ≤6/255, kenar ≥%70 | Paket §5.5 ve karşılaştırma aracı |
| F-YZ TR, küre maskeli | Ortalama fark **5,01/255**, kenar örtüşmesi **%82,0** | `captures/F2/F-YZ_comparison.txt`, fark/kenar/yanıp sönme görüntüleri |
| TR / EN, 1920×1080 | Görsel olarak incelendi; EN'de metin taşması 0 | `captures/F2/F2_Lab_TR.png`, `F2_Lab_EN.png` |
| Play denetimi | **32/32**: raycast, sürükleme, oklar, easing, ortak sweep, donma, 8 fps, gizleme, dil ve geliştirici kontrolleri | `captures/F2/play-qa.json` |
| Ek Play denetimi | **6/6**: Shift+ok 10, adil bütçe, gerçek ekran dışı/alfa görünürlüğü ve altın yakalama düğmesi | `captures/F2/extra-qa.json` |
| Hareket | 18 saniyelik çevrim, 0,025 sn simülasyon adımı; 1280×720, 10 fps doğrulama kaydı | `captures/motion/F2_kure_canli_gecis.mp4`, `captures/F2/motion-timing.json` |
| Başlangıç durumuna dönüş | DeskGame, temiz, Edit modu; TR; Play/Project ayarları ve profil yedeğe eşit | `captures/F2/restoration.json`, `final-state.json` |

Parite verisi JS kaynağının SHA256'sını taşır. Testler ilk RNG örnekleri, icosphere vertex/yüz/kenar sırası, tüm rastgele başlangıç alanları ve 22 karede lif/ark/kıvılcım dizileri ile sonraki RNG değerini karşılaştırır. Intro atlandıktan sonra ilk kare mantığının tekrar başlamaması ayrıca test edildi. Sıcak simülasyon döngüsünde 0 B tahsis testi vardır.

Kanonik HTML laboratuvarı yerel Chrome sekmesinde açıldı; gerçek 0→100 sürükleme ve uçlardaki altın/kristal değişimi görüldü. Sekme ve yerel HTTP sunucusu kapatıldı. Unity Play içinde gerçek fare tıklamasıyla canlı küre değişimi izlendi; otomatik QA EventSystem raycast/drag akışını, Shift denetimi Input System klavye durumunu kullanır. Unity sayısal portunun kareleri GPU çıktısından okunur; referans PNG'leri tekrar kullanılmaz.

## Performans

Apple M4 Pro, macOS 27.0.1, Unity 6000.3.12f1 Editor/Metal. On iki harita boyutunda küre: 27–51 px yarıçap, görünen düzeyler 0–100; normal/bilinmeyen/karantina/çekirdek karışımı. 100 adım başlangıç, 10 kare ısınma, 40 ölçüm karesi. Aynı karede **12 simülasyon adımı + 12 GPU komut gönderimi** ölçüldü.

| Ölçüm | Sonuç |
|---|---:|
| Ortalama CPU | **2,346 ms** |
| Medyan CPU | 2,312 ms |
| %95 CPU | 2,496 ms |
| Sıcak döngü yönetilen GC | **0 B** |
| RT tahsisi, toplam | **2,985 MiB** (12 MiB sınırının altında) |
| Son kare toplam ilkel | 13.360 |

Ham kayıt `captures/F2/performance.json`. İlk köşe mesh yolu yaklaşık 5,14 ms idi. GPU köşe üretimi, küçük matematik yardımcılarının inline edilmesi ve değişmeyen trigonometrik geometrinin önceden hesaplanmasıyla yukarıdaki sonuç alındı. Bu değişiklikler RNG sırasını değiştirmedi; ardından bütün testler ve altın kareler tekrar geçti.

CPU ölçümü GPU süresi veya oyuncunun toplam kare süresi değildir. RT sayısına StructuredBuffer dahil değildir: 5.120 × 112 bayt × 12 = **6,563 MiB** ek GPU veri tamponu vardır; CPU çizim/model tamponları, fontlar ve atlaslar ayrıca yer tutar. Bu Mac Editor ölçümü Steam/Windows performans garantisi değildir. 19 küreli rehber ve gerçek masa batch ölçümü sonraki fazlardadır.

## Yeniden doğrulama

Unity Editor açıkken, proje kökünden:

```sh
unity command run_tests --mode editor --filter Corrigible --caller plugin --skill unity-cli --format json --timeout 600
unity command eval 'return Corrigible.Fx.EditorTools.OrbCapture.CaptureAll();' 600000 --caller plugin --skill unity-cli --format json --timeout 720
python3 Tools/Fx/compare_orbs.py
```

Son komut Pillow ve NumPy gerektirir. Sayısal oracle yalnız kanonik kaynak bilerek değiştirildiyse `python3 Tools/Fx/generate_orb_oracle.py` ile üretilir; varsayılan JavaScriptCore yolu macOS sistem JSC'sidir, başka ortamda `JSC` ortam değişkeni verilir. Kaynak JS üzerinde kalıcı ölçüm kancası bırakılmaz.

Laboratuvarı açıp Play'e bas. Ölçeğe tıklama/sürükleme otomatiği kapatır; ok ±1, Shift+ok ±10. OTOMATİK yeniden ortak sweep'e bağlar. Geliştirici panelinde tür, yarıçap, tohum, lif ve fps değişir; Altın kare Editor kancasından 22 görüntüyü üretir.

## Sınırlar ve görsel farklar

- F2 ayrı FX laboratuvarıdır. Canlı DeskGame/Baskın görünümü değiştirilmedi; F3 harita entegrasyonudur.
- RT'deki 64 px önbellek sprite örneklemesi shader'da analitik üretilir: kaynak renk kuantizasyonu, RGBA8 premultiplication ve bilinear örnekleme korunur. İçe alınan 128 px glow profil dokusu aktif çizim yolunda örneklenmez. Animasyon flipbook/video değildir.
- Canvas2D çizgi birleşimleri ile GPU parçalı çizgilerin AA'sı birebir piksel eşit değildir. 22 altın kare bu farklarla birlikte ölçülüp eşiği geçti.
- TMP ve tarayıcı yazı rasterizasyonu/ışıma dağılımında küçük farklar sürer; maskeli ekran sonucu yukarıda açıkça kayıtlıdır. UI için altın küre eşikleri ayrıca bir kabul kuralı olarak uydurulmadı.
- F-YZ JPG ve HTML'deki canlı sayı 20; metin brifinin §9.3 örneğinde 26 yazıyor. Statik karşılaştırmada JPG/HTML esas alınarak 20 kullanıldı. Otomatik model 18 sn; kaynak video 19,5 sn içerir. Kaydın kendisi 18,0 sn, fark %7,7; ilk/orta/son kareler çözümlenip görsel olarak incelendi. Kayıt 10 fps olduğundan 40 fps canlı akıcılığının ölçümü sayılmaz.
- Yeni paket, URP/Volume/Bloom veya oyun kuralı eklenmedi. Testlerin font/prefab yeniden serileştirme artıkları F2 başlangıç SHA256'larıyla doğrulanıp geri alındı. Commit/push yapılmadı.
