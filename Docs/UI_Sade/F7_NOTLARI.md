# Sade arayüz · F7 cila ve doğrulama

18 Ekim 2026. F7 uygulandı. Bileşenler, sahneler, komut sınırları ve uyarlamalar `UYGULAMA_NOTLARI.md` içindedir.

## Değişiklikler

- Masa haritasının bölge etiketleri ve düğüm yazıları `SadeMapTextLayer` ile ortak alt Canvas'ta gruplandı. Serileştirilmiş çapalar hover hareketi, ölçek, boyut ve görünürlüğü korur. Düğüm sahne örnekleri bağımsızlaştırıldı; kayıt/yüklemede metinlerin eski prefab ebeveynine dönmesi giderildi. İki sahne yeniden açıldığında ortak katmanda 48 yazı doğrulandı.
- Kart açılışı tamamlanınca alpha yazımı durur. Sonuç spotu değişmedikçe mesh kirletilmez; kamera, önizleme yıldızları ve köşe alpha'sı yalnız değişince yazılır. Dünya etiketlerinin mevcut kamera/konum/VM önbelleği korunur.
- Girişteki karşı önlem satırları başlangıç profilinden güncel dilde yeniden kurulur; oyun durumu ve RNG değişmez. Baskın sonuç bölgesi güncel çeviriden alınır. Çeviri tablosu güncellendi.
- Baskın prefab'larında 11 px altında kalan 14 alan 11 px'e çıkarıldı. Auto Size kullanılmadı. `SadeTextChecker` masa ve Baskın'da gerçek glif sınırlarını, kırpılmayı, minimum boyları ve case stillerini denetler.
- Geliştirme kısayolları: F1–9 masa, Shift+F1–9 Baskın, Ctrl+F1–3 B3/B4/B7b; F10 dil, F12 referans, F11 opaklık, Shift+F11 yanıp sönme. `IRaidPreviewSource` yalnız mock kaynakta uygulanır. Düz F1 kuralları açar; Shift/Ctrl birleşimleriyle çakışmaz.
- `DeskPerfProbe.Run(path, idleFrames)` rapor yolunu parametre alır ve ölçümden önce PlayerLoop içinde bir kez Canvas'ı ısıtır. `DeskCapture`, çözünürlük geçişinde metin çapalarını ve dönüşte normal yerleşimi yeniler.

## Doğrulama

- Son derleme başarılı; **345/345 EditMode** testi geçti. Yeni testler: spotun değişmeden kirlenmemesi, profil satırlarının dil/RNG sınırı, metin çapalarının hareket/boyut/görünürlüğü ve örnek komutlarının canlı kaynaktan ayrılması.
- **144/144 görünüm/dil/çözünürlük birleşimi**, **11.344 metin kontrolü**, **0 ihlal**: S-Masa1–9 ve S-Baskın1–9; TR/EN; 1920×1080, 1920×1200, 2560×1440, 3440×1440. Optimize edilmiş sahneler kaydedilip yeniden açıldıktan sonra tekrar çalıştırıldı. 54 yakalama kaydedildi (`matrix_qa.json`, `text_report.txt`).
- **16/16 tahta köşe kontrolü**: dört çözünürlükte maket ekran içinde (`bounds_qa.json`). Temsilî normal/geniş oran yakalamaları görsel olarak da incelendi.
- **24/24 girdi kontrolü**: gerçek Update/EventSystem zincirinde Input System sanal klavye/fare; eski kontroller ve Shift/Ctrl ön ayarları, F10/F11/F12 dahil (`input_qa.json`).
- **3 yerel CUA kontrolü**: fareyle Kurallar açma, Escape ile kapatma, L ile günlük açma. CUA Shift+F7 girdisi Unity durumunu değiştirmedi; fonksiyon tuşları sanal klavyeyle doğrulandı (`native_input.json`).
- **59/59 canlı köprü kontrolü**: DeskGame'de ekip gönderme, denetim ve gerçek StartRaid; kontrollü taktik bitişleriyle başarı/süre dolması/geri çekilme. Her durumda İngilizce sonuç bölgesi, tek `raidres`, masa girdisinin dönüşü, görünen operasyon raporu ve replay fon/SG/sızma eşitliği (`live_qa.json`).
- İlk Play turunda normal domain reload kullanıldı. Son tekrarlar için domain reload geçici kapatıldı; bitişte önceki ayar geri yüklendi. İlk yedek kullanıcının bu fazdaki gerçek verisidir; önceki fazların yedeği kullanılmadı.

## Performans

Unity Profiler UI Details; ısınmadan sonra 120+ durağan kare. Sayılar UI batch'leridir; 3D maket ayrıca çizilir.

| Durum | Önce | Sonra |
|---|---:|---:|
| Varsayılan masa | 35 | **30** |
| Bölge kartı açık | 55 (F3 ölçümü) | **50** |
| Baskın S1 | 14 | **11** |
| Baskın S2 / S6 / S9 | — | **14 / 15 / 10** |

Ana masa için ≤30 ve ölçülen Baskın durumları için ≤25 hedefi karşılandı. Kart açıkken 50 batch kalır; ≤30 bütün açılır paneller için sağlanmış değildir. Ek font/malzeme/atlas birleştirmesi ayrı bir optimizasyon işidir.

Ölçülen durağan durumlarda **Corrigible betik GC'si 0 B/kare**, **Canvas.BuildBatch 0**. Editörün `UnitySynchronizationContext.ExecuteTasks` ayırmaları ayrıca raporda görünür; toplam editör GC'sinin sıfır olduğu iddia edilmez. PC URP asset'inde SRP Batcher açıktır. Raporlar: `desk_final.txt`, `desk_card_final.txt`, `raid_after_S*.txt`.

Varsayılan Türkçe masada optimizasyon öncesi/sonrası **ortalama piksel farkı 0, değişen piksel 0** (`batching_pixel_diff.json`).

## Görsel sınırlar ve teslim

18 referans için diff/kenar/blink üretildi (`comparison.json`, `S-*_cmp_*`). Masa farkı 5,84–7,80/255; Baskın 17,34–22,18/255. Mevcut 3D maketin malzeme, ışık ve yerleşim farklılıkları sürer; ±2/±4 px tam ekran sadakati veya raster eşitliği iddia edilmez. Tipografi ve oran denetimi görsel referansla birebir eşleşme anlamına gelmez.

Kayıt/profil/TestResults, 11 oyun tercihi, ProjectSettings ve geçici Enter Play ayarı geri yüklenip karşılaştırılır; sonuç `data_restore.json` içindedir. Eski `Docs/UI_C/captures/F7_performans.txt` dosyası başlangıç içeriğine döndürüldü. Editör başlangıçtaki `Scenes/Baskin.unity` sahnesine, Edit modunda döndürülür. Commit/push yapılmadı.

Tekrarlanabilir denetimler `Docs/UI_Sade/tools/f7_{matrix,perf,input,live}_qa.cs` içindedir; CLI `eval_file` ile çalıştırılır. Canlı denemeden önce kayıt/profil/tercih yedeği alınmalıdır. Geçici sahne kurucuları projeye bırakılmadı.
