# F2 · S-Masa durağan sahnesi

Sahne: `Assets/Corrigible/Desk/Scenes/SadeDesk_Sandbox.unity`.
Kaynak: paket 1.1, S-Masa1. Etkileşim ve açılır paneller F3 kapsamındadır; gerçek oyun sahnesine geçiş PLAN'daki ayrı aktarım adımıdır.

## Kurulan yapı

- `SadeMapCanvas`: harita tabanı, 12 bölge meshi, karantina kâğıdı/taraması, sınır dokusu, kâğıt tanesi, karantina sınırı, ekran boyu lamba, çekirdek dalgaları, yayılım iplikleri, ekip rotası ve işaretler.
- `MapCoverFitter`: 1920×1080 tasarımını merkezden kaplar; ayrı `MapMarkers` yazı/simge boyunu korur. `SadeMarkerAnchor` yalnız konumu yeniler.
- `RegionMeshGraphic`: hazır earcut üçgenlerini, delikleri ve doku fazını saklar. Düğümler 44 px dairesel; bölge alanları çift–tek testiyle denize/deliklere tıklamayı dışlar.
- `SadeHudCanvas`: FON, SİYASİ GÜÇ, HEDEF; ortada takvim/saat/hız; sağda üç risk; altta beş sekme, LEJANT ve küçük Son Şalter zarfı.
- `MockSadeDeskDataSource` → `SadeDeskPreview` → HUD/harita. Örnek gün 74'te durağandır; `GameController` veya oyuncu kaydı açmaz.
- Düğüm, etiket ve küçük zarf prefab'ları `Prefabs/Sade` altında; referanslar sahnede serileştirilmiştir. Tek seferlik kurucu kaldırılmıştır.

## Görsel kararlar

- Sağ üst etiketlerin 8 px boşluk kararı F1'den korunur; referans görseldeki üst üste binme tekrarlanmaz.
- Paket jetonunun 30 px çizimi 40×40 sprite kanvasındadır. Ekip simgesinin görüntüsü 40×40, içindeki jeton 30 px tutulur.
- Uyarı notunun 30×30 kâğıdı 40×46 sprite kanvasındadır. Pin ve sert gölge kesilmeden çizilir.
- Halka sprite'larının kenar boşlukları korunur: r40 için 84×84, r53 için 110×110.
- `teamA` coğrafi örneği ile çizilmiş ekip A konumu uyuşmaz. F2'de çizimdeki `overlay.teams` konumu korunur; Miller testi formülü doğrular.
- Etiket halesi ortak `Sade Map Label.mat` TMP Underlay materyalidir.
- Linear renk uzayında vinyet açık kaldığı için `tx_lamp_desk_linear.png` türetildi; kaynak PNG korunur. C paketindeki `DeskMath.LinearOverlayAlpha` yöntemi kullanılır (temsilî kâğıt zemini 0,75). Yeniden üretim: `unity command eval_file --file Docs/UI_Sade/tools/lamp_linear.cs 600000 --caller plugin --skill unity-cli --format json --timeout 720`.
- Mock VM önbelleği serileştirilmez. Unity sıcak yeniden yüklemesinin `int?` alanlarını düşürmesine karşı her açılışta JSON yeniden okunur.
- Metin denetimi SDF saydam payını taşma saymaz; son konumlandırılmış glif metriklerini kullanır. İngilizce ETA fişi içeriğe göre genişler.
- Kamera yakalama aracında ilk çizim canvas boyunu kesinleştirir; ardından iki cover katmanı yenilenip son görüntü alınır. Ekran oranı değişirken ilk yakalamada görülen iplik/dalga kayması böyle giderilir.
- Dolgular karantina örtülerinden önce gruplanır. Düğüm metinlerinin dikdörtgenleri merkezleri korunarak daraltılır; saydam metin alanlarının neden olduğu çizim bölünmeleri azaltılır.
- İplik çizgileri/gölgeleri `SpreadThreadLines`, ok uçları `SpreadThreadArrows` katmanındadır. `SadeSpreadThreadView` serileştirilmiş referanslarla ikisini yönetir; devre dışı bırakılınca ayrı ebeveynlerdeki grafikler de kapanır. Bu davranış editör önizlemesinde de çalışır ve test edilir.

## Geliştirici araçları

- F1: S-Masa1 örneğini yenile.
- F10: TR/EN değiştir.
- F12: referans JPG katmanını aç/kapat.
- F11: referans opaklığı; Shift+F11: yanıp sönme.
- `Corrigible/Sade UI/Capture Game View`: görüntüyü `Docs/UI_Sade/captures` altına kaydet.
- `Corrigible/Sade UI/Check Text`: etkin örnekte TR/EN metin kurallarını kontrol et.

Karşılaştırma:

```bash
python3 Docs/UI_Sade/tools/compare.py Docs/UI_Sade/captures/F2_S-Masa1_1920x1080_TR.png Docs/UI_Sade/SonSalter_Sade_Paketi/reference/screens/S-Masa1.jpg --tsv Docs/UI_Sade/SonSalter_Sade_Paketi/reference/measure/S-Masa1.tsv --out Docs/UI_Sade/captures/F2_karsilastirma
```

## Doğrulama

- **Derleme:** yeni C# derleme hatası yok.
- **EditMode:** 285/285 geçti; 16 F2 vakası izdüşüm, üç ekran oranında cover dönüşümü, delik/ada isabeti, serileştirilmiş mesh, ayrı katmanlardaki iplik görünürlüğü ve mock VM durumlarını kapsar. Sonuç: `captures/F2_tests.json`.
- **Play:** 12/12 düğümün grafik raycast'i doğru hedefe ulaştı; JPG referans katmanı açılıp kapandı. `GameController` bulunmadığı ayrıca doğrulandı. Sonuç: `captures/F2_qa.json`.
- **TR/EN:** 148 metinde eksik çeviri, küçük font, AutoSize, UpperCase veya glif taşması yok. Sonuç: `captures/F2_metin_denetimi.txt`.
- **Oranlar:** 1920×1080, 1920×1200, 2560×1440, 3440×1440 TR; ayrıca 1920×1080 EN yakalandı. 16:10 ve ultra genişte iplik, dalga ve ekip rotası haritayla hizalı; işaret/yazı boyu sabit.
- **Referans:** ortalama RGB farkı 5,84/255, ham kenar örtüşmesi %83,6. Bu değer ±2 px uygunluk yüzdesi değildir; JPEG sıkıştırması, TMP/CSS yazı çizimi ve Linear renk karışımı da fark üretir. Sağ üst etiketlerin 8 px aralığı bilinçli F1 kararıdır. Kenar, fark ve yanıp sönme dosyaları `captures/F2_karsilastirma_*`.
- **Koruma:** oyuncu profili, kayıt ve `corrigible.*` tercihleri aynı kaldı; `ProjectSettings.asset` başlangıç kopyasıyla aynı.
- **Performans:** UI Details toplamı 40'tan 37 batch'e indi (harita 26, HUD 11). Paketin ≤30 toplam hedefi henüz karşılanmıyor; F7 cila işidir. İplik/ok gruplaması bu örneğin son sayısını ayrıca düşürmedi. 121 boş karede `Canvas.BuildBatch` ve `Graphic.PerformUpdate` yok; Sade betiklerinde GC yok. Pipeline çağrılarının `UnitySynchronizationContext.ExecuteTasks` örneğinde iki karede toplam 464 B ölçüldü; bu nedenle tüm editör için mutlak 0 B iddiası yapılmaz. Ayrıntı: `captures/F2_performans.txt`.

F2, sekme/LEJANT içeriğini veya ana oyun eylemlerini henüz bağlamaz; bunlar F3 kapsamındadır.
