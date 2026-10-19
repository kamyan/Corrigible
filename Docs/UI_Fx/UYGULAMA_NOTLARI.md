# Son Şalter · FX uygulama notları

19 Ekim 2026. F7 geliştirme ve doğrulama kaydı aşağıdadır. Sonraki canlı entegrasyon tamamlandı: başlangıç `FxDeskGame`, taktik sahne `FxBaskin`. Güncel bağlantı, kullanım ve kabul kaydı [CANLI_ENTEGRASYON.md](CANLI_ENTEGRASYON.md), kanıtlar `captures/Live/` altında. F7 örneklerinin kanıtları `captures/F7/` olarak korunur.

## Sahne ve bileşenler

| Sahne / bileşen | Sorumluluk |
|---|---|
| `FxDesk_Sandbox` | F-Masa1–9 mock örnekleri; harita, kartlar, çekmeceler, danışman ve olay akışları |
| `FxRaidHud_Sandbox` | F-Baskin1–9; ortak Baskın kuralı üzerinde tahta, önizleme, HUD, onay ve sonuç |
| `FxRaid_Sandbox` | F5'in HUD'sız bağımsız tahta deneyi; korunur |
| `FxOrbLab` / `OrbView`, `OrbClock` | Kanonik küre portu, tek zamanlayıcı, görünürlük ve CPU/RT bütçesi |
| `FxClock` / `FxLoopRunner` | Bütün FX saatleri; referans evreleri, donma ve azaltılmış hareket |
| `FxDeskInteraction` / `FxDeskPanels` | Masa girdileri ve serileştirilmiş görünüm bağlantıları |
| `FxRaidSource` / `FxRaidHud` / `FxRaidBoard` | Veri kaynağı, komut ve sunum ayrımı; makine çözümü bir kez hesaplanır |
| `FxGraphicBatch` | Ortak font/malzeme geometrileri; kaynak etiketler yerleşim ve hit için korunur |
| `FxRaidEdges` | Hazır kenar mesh'lerini iki renderer'a birleştirir; yalnız görünürlük/transform değişince yeniden kurar |
| `FxPulseGraphic` | Nabız parametresini vertex'e yazar; shader saati alfa üretir |
| `FxDebugMenu` / `FxReferenceOverlay` | Geliştirme kısayolları, tek JPEG tamponlu referans, dil/donma/hareket denetimi |
| `FxTextChecker` / `FxCaptureTools` | Editör metin denetimi ve Game View yakalaması |

Sahne/prefab hiyerarşileri asıl kaynaktır. Bütün bağlantılar serileştirilmiştir; çalışma zamanında isimle obje arama ve sahne kurucu yoktur. F7'nin tek seferlik yazıcısı çalıştırıldı ve kaldırıldı.

## Kullanım

- F1–F9: Masa örnekleri. Shift+F1–F9: Baskın örnekleri. Ctrl+F12: YZ laboratuvarı. Üç sahne arasında Play modunda geçilir.
- F12: geçerli referansı göster/gizle. F11: %25/%50/%75/%100 opaklık. Shift+F11: yanıp sönme. Katman girdileri engellemez.
- Ctrl+F8: geliştirme paneli. F10: TR/EN. Ctrl+M veya panel: Hareketi azalt.
- Hareket tercihi `corrigible.fx.reduceMotion` anahtarında saklanır; FX sahne geçişinde okunur. Azaltılmış harekette tarama/yanıp sönme/dönüş durur, girişler tamamlanır, küre güncellemesi düşürülür.
- Baskın'da 1/2/3 ekip, Q yetenek, Z geri al, Enter tur/onay, Esc iptal/duraklat, L günlük, Shift+/ kurallar, Boşluk makine hızıdır. F1 ortak örnek kısayoluna ayrılmıştır.
- `Tools > FX UI > Capture Game View`: referans evresine dondurur, küreleri çizip PNG kaydeder. YZ laboratuvarında değer/işaret 26 olur.
- `Tools > FX UI > Check Text`: açık örneği iki dilde inceler; batch nedeniyle cull edilen kaynakları ve dünya etiketlerini de denetler. Dünya fontları ekran izdüşümünden piksele çevrilir. Otomatik boyut, case dönüşümü, 11 px altı yazı, eksik glif ve taşma raporlanır.

## Sade ile ilişki ve canlı bağlama

Sade tema, geometri ve mock veriler gerektiği yerde paylaşılır. Ortak `RaidDataSource`, `RaidViewModel`, `IRaidCommands`, `IRaidPreviewSource` ve `Baskin` tek kural kaynağıdır. FX kaynakları `GameController` veya oyuncu kaydına komut göndermez. “Rapora geç” mock başlangıcına döner.

Canlı bağlama ayrı adımdır: Masa görünümleri `sim.Shown`/`Global` algı verisinden üretilen VM'leri almalı; gerçek sızma ve gizli çekirdek okunmamalıdır. Oyuncu eylemleri mevcut GameController komutlarına gitmeli, GameRecord/replay ve sahte zaferin `EndRevealed` sınırı korunmalıdır. Baskın görünümü mevcut canlı veri kaynağına bağlanmalı; sonuç/geri çekilme mevcut operasyon dönüşünden geçmelidir. Sade/FX arasında aynı anda iki girdi sahibi veya iki makine çözümü çalıştırılmamalıdır. Bu paragraf F7 teslim sınırını kaydeder; daha sonra yapılan canlı bağlantı CANLI_ENTEGRASYON.md içinde belgelenmiştir.

## F7 düzeltmeleri

Dünya üzerindeki ✓ tamamlandı işaretleri, mevcut DejaVu simge fontu ve FX malzemesini kullanır; Sade font dosyası değişmez. Eksik glif denetimi TMP'nin yerine koyduğu kareyi kaynak karakteriyle karşılaştırır.

Masa rapor/saha etiketlerinin ilk açılış ölçümü ortografik TMP ölçümüne sabitlendi; ödenek açıklaması 11 puntoya çıkarıldı. Baskın kenarları 1.863 hazır kaynaktan iki hedef mesh çizimine toplandı. Üç dünya shader'ının materyal alanları UnityPerMaterial tamponundadır; Unlit/SRP Batcher uyumu doğrulandı. Dünya/HUD yazıları ortak gruplandı; nabızlı işaretlerde sürekli CanvasGroup değişimi azaltıldı. Donmuş veya azaltılmış harekette tarama ışını gizlenir. Dinamik UI kaynaklarında `CanvasRenderer.GetMesh()` kopyaları yerine tekrar kullanılan mesh tamponları vardır; kaynaklar kapanınca tamponlar serbest bırakılır.

## Kabulün sınırları

Referans JPG'ler ile Unity TMP/font rasterizasyonu, cam ve glow birebir değildir. F5 robot/fan siluetleri, F6 günlük/sonuç düzeni ve sonuç memosunun 438 piksel yüksekliği korunur. Resmi karşılaştırma aracı 19 görünüm için maske/TSV ile çalıştırılır; üretilen farklar ±2 px UI, ±1 px harita, ±4 px tahta kabulünün tümüyle sağlandığını kanıtlamaz. Bu görsel eşlik kapısı açıkça ayrı tutulur.

Performans ölçümleri Apple M4 Pro / Metal / Unity Editor ortamına aittir; orta sınıf Windows/Steam PC garantisi değildir. GPU zamanı sıfır dönerse bu “GPU maliyeti yok” olarak yorumlanmaz. Kaynak native kayıt ve ölçüm özeti F7 raporunda bulunur.

Yeniden çalıştırma: `Tools/Fx/f7_screens.cs`, `f7_debug_qa.cs`, `f7_raid_regression.cs`, `f7_raid_performance.cs`, `f7_native_profile.cs` CLI `eval_file` üzerinden kullanılır. Native kayıt, uzun süre açık bırakılmadan tek seferlik sınırlı örnek toplar. Python karşılaştırmaları bundled NumPy/Pillow ortamıyla çalışır. Testlerin profil ve tercih yan etkileri başlangıç yedeğinden geri yüklenir. Commit/push yapılmaz.
