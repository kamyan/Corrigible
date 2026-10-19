# FX · F7 son düzenleme ve doğrulama

19 Ekim 2026. F7 geliştirme araçları, metin/oran kontrolleri ve performans düzeltmeleri uygulandı. Canlı oyuna geçiş yapılmadı. **Görsel piksel eşliği ve hedef Windows PC/GPU kabulü açık**; bütün brif yeşil kabul edilmiş değildir.

## Uygulama

Üç örnek sahnede ortak `FxDebugMenu` ve `FxReferenceOverlay` vardır. F1–9 Masa, Shift+F1–9 Baskın, Ctrl+F12 YZ laboratuvarını açar. F12 referans, F11 opaklık, Shift+F11 yanıp sönme; Ctrl+F8 denetim paneli, F10 dil, Ctrl+M Hareketi azalt. Tercih sahneler arasında kalır. Referanslar kopyalanmadan tek JPEG tamponuyla yüklenir; 19 görüntü desteklenir.

`Tools > FX UI > Capture Game View` donmuş referans evresinde yakalar. YZ laboratuvarı değer/işaret 26 olur. `Check Text` iki dilde, cull edilmiş batch kaynakları ve dünya yazıları dahil taşma, glif, 11 px, Auto Size ve case stili denetler. Dünya puntosu ekrana izdüşürülür. Denetim paneli metinleri de TR/EN çevrildi. Dünya üzerindeki tamamlandı işaretinin eksik ✓ glifi, mevcut DejaVu simge fontu ve FX malzemesiyle düzeltildi; Sade font dosyası değiştirilmedi. Eksik glif denetimi kaynak karakteri de karşılaştırır.

Masa'da ilk açılışta yanlış dar ölçülen rapor/saha etiketleri ortografik ölçüme sabitlendi; ödenek açıklaması 10→11 punto yapıldı. Baskın kenarları 1.863 hazır kaynaktan iki renderer'a birleştirildi. Üç dünya shader'ı Unlit/SRP Batcher uyumlu materyal tamponlarına geçirildi. Dünya/HUD çizgileri ve fontlar gruplandı. Nabız vertex/shader saatinde hesaplanır; donma ve azaltılmış harekette tarama gizlenir. Dinamik UI'de `CanvasRenderer.GetMesh()` kaynaklı yeni mesh kopyaları tekrar kullanılan tamponlarla değiştirildi; tamponlar kapanışta bırakılır.

## Doğrulama

| Kontrol | Sonuç / kanıt |
|---|---|
| EditMode | **440/440**, atlanan 0; `captures/F7/EditMode.xml` |
| Masa Play regresyonu | **74/74**, UI raycast, komutlar, panel/Esc, klavye ve 1x; `desk-play-qa.json` |
| Baskın Play regresyonu | **102/102**, tahta tıklaması/komut, kilit/onay, makine çözümü, sonuç ve ek tahtalar; `play-qa.json` |
| Denetim araçları | **11/11**; referans, opaklık, yanıp sönme, dil, tercih ve kenar yeniden kurmama; `debug-qa.json` |
| Sahne geçişi | Gerçek Input System F2/Ctrl+F12/Shift+F9 olaylarıyla doğru sahne/örnek ve kalıcı azaltılmış hareket; **6/6**, `cross-scene-summary.json` |
| TR/EN ve oran | **176 PNG, 13.392 metin, 0 hata**; `screens-summary.json` |
| Ekran boyutları | 1920×1080, 1920×1200, 2560×1440, 3440×1440 |
| Küre golden | **22/22**; RGB farkı 0,74–0,95/255, kenar örtüşmesi %95,7–97,8; `orb-comparison.json` |
| Hareket | İki yeni MP4; **8,00 sn / 240 kare / 30 fps / 1280×720**, süre sapması %0; `captures/motion/F7_baskin_*.mp4` |
| Kenar birleştirme | Önce/sonra en büyük kanal farkı **2/255**, ortalama **0,00048/255**; `edges-comparison.json` |
| URP / SRP Batcher | HoloFace, HoloGlass, EdgeLine materyal tamponları düzenlendi; üç shader uyumluluk kodu0, derleme hatası0, MaterialPropertyBlock kullanan renderer0; `srp-shaders.json` |
| Native Profiler | İki sahnede 80'er ısınma + 40'ar ölçüm karesi; `.data`, JSON ve metin raporu |

176 yakalama: 19 örnek × 2 dil × 4 boyut =152; Masa7/Baskın3/YZ azaltılmış hareket ×2 dil ×4 boyut =24. Metin kontrolü batch kaynaklarını atlamaz. FX'in yeni mesh tamponları için bağımsız renk/yenileme, kenar transform/endpoint/görünürlük ve vertex nabız ve kaynak karakteri farklı kare glifini yakalama testleri eklendi. Tek seferlik sahne yazıcıları çalıştırıldı ve kaldırıldı.

## Performans

Apple M4 Pro / Metal / Unity Editor, kamera hedefi 1920×1080. Native UI collector sayıları bütün UI canvas'larını kapsar; dünya mesh çizimleri UI sayısına katılmaz. FX GC, `FxClock.Update` alt ağacıdır; Editor/CLI/ölçüm callback'i hariçtir.

| Ölçüm | F-Masa7 | F-Baskin3 |
|---|---:|---:|
| UI batch, en fazla | **36 / hedef40** | **30 / hedef30** |
| FX CPU, ortalama | 2,400 ms | 1,431 ms |
| Küre sayısı | 19 | 2 |
| Küre CPU, ortalama / en fazla | **2,164 / 2,247 ms** | 0,459 / 0,481 ms |
| FX GC, her ölçülen kare | **0 B** | **0 B** |
| Canvas.BuildBatch, ortalama | 0,0118 ms | 0,0169 ms |

İlk kayıtta Baskın35 UI batch /40 B vardı; ara çizgi gruplamasında yeni mesh kopyaları80 B'ye çıktı. Tekrar kullanılan kaynak mesh tamponlarından sonra son native kayıtta0 B doğrulandı. `native-before.json`, `native-intermediate.json`, `native-before-mesh-fix.json` önceki ölçümleri; `native-profile.*` son ölçümü tutar. Her kayıt temiz Editor oturumunda bir kez açılıp sınırlı süre sonunda kapatıldı.

## Açık kabul maddeleri

- **Görsel piksel eşliği:** Resmi paket aracı19 ekran için TSV ve küre maskeleriyle çalıştırıldı. Fark/kenar/blink dosyaları `F-*_cmp_*`, sayısal sonuçlar `reference-comparison.json`. Masa/YZ ve Baskın'ın bazı siluet, font, glow, sonuç ve günlük kutuları referansa birebir değildir. Sonuç memosu438 px kalır. ±2 px UI, ±1 px harita ve ±4 px tahta eşliği topluca sağlandı denemez.
- **GPU ve hedef PC:** Profiler GPU süreleri0 döndü; geçerli GPU maliyeti alınamadı. Bu sıfır GPU maliyeti değildir. Orta sınıf Windows/Steam PC bütçesi bu Mac ölçümüyle doğrulanmış sayılmaz.
- **Canlı entegrasyon:** FX kaynakları mock kalır. GameController, algı sınırı, kayıt/replay, operasyon raporu dönüşü ve Sade/FX seçim akışı ayrı iş olarak bağlanmalıdır.

Bileşenler, kullanım ve canlı bağlama sınırları `UYGULAMA_NOTLARI.md` içindedir. Son kareler `captures/F7/`, toplu önizleme `final-contact.jpg` altındadır. Çekirdek/Prototype kuralı değiştirilmedi. Commit/push yapılmadı.

## Geri yükleme

Son440/440 koşusunun XML kanıtı ayrıldı; başlangıçtaki `FxDesk_Sandbox`, TR dili ve Edit modu geri yüklendi. Profiler/GPU kaydı kapalı, sahne temiz, ön yüklü asset1. Oyuncu profili/TestResults ve oyun tercihleri başlangıçla birebir; Project/Editor ayarları birebir. FX/lokalizasyon dışındaki Assets/ProjectSettings/Prototype dosyalarında beklenmedik fark0. Play'in değiştirdiği `FxRaidTeam.mat` başlangıç yedeğinden geri alındı. Testlerin font/atlas yan etkileri de başlangıç SHA'sı HEAD ile doğrulanarak temizlendi. Kanıt: `restoration.json`, `final-state.json`.
