# FX · F4 masa panelleri ve etkileşim

18 Ekim 2026. Kullanıcının “F4 e geç / devam et” talebiyle `Assets/Corrigible/Fx/Scenes/FxDesk_Sandbox.unity` genişletildi. Canlı `DeskGame`, Baskın, Core ve Prototype kaynakları değiştirilmedi. Bu faz, FX görünümünün ayrı referans sahnesidir; oyunun varsayılan görünümünü değiştirmez.

## Uygulananlar

- F-Masa2–9: bölge kartı, bağ çizgisi, düğüm/ad/çekmeceyi koruyan sağ → sağa yaslı → sol yerleşim; 12 bölge seçimi, denizle kapatma, eylem önizlemeleri ve devre dışı eylem koruması.
- Araştırma, haberler, ekipler, YZ günlüğü ve rehber çekmeceleri. Aynı anda tek çekmece veya tam dosya; bölge kartı ve bağımsız lejant birlikte açık kalabilir. Gövdeler RectMask2D ile kayar.
- Haber filtresi, denetim ve bölge bağlantıları; ekip görevlendirme/hedef seçimi, satın alma ve baskın niyetleri; araştırma seçimi, başlat/duraklat ve hız/rezerv seçimi. Sandbox yalnız mock niyet günlüğüne yazar; gerçek fon veya araştırma kuralı hesaplamaz.
- Rehberde yedi sayfa: YZ hâlleri, güven dili, renkler, eylemler, saha ekipleri, Son Şalter, kısayollar. YZ sayfasında üç normal, üç özel ve bir canlı geçiş küresi var; harita ile toplam 19 küre.
- FON dökümü, geçmiş grafiği ve kalan gün; diğer HUD değerlerinin tek ipucu; 0,4 sn gecikmeli düğüm ipucu. Açık ipucunun düğüm adı gizlenir, kapanınca geri gelir.
- Tam bölge dosyası: gösterilen kaynaklar, 30 günlük geçmiş, bağlantı küreleri, eylemler ve saha ekibi. Ek kaynaklar sonraki bölümleri aşağı iter.
- 0,55 sn giriş, 0,35 sn ipucu, 0,3+1,4 sn grafik çizimi, 0,6+2,2 sn/48 adım daktilo. Bütün hareketler tek `FxClock` üzerinden; azaltılmış harekette giriş/daktilo tamamlanır.
- R/N/T/L/H, 1–4, B, Tab/Shift+Tab, Space, +/−, Esc ve Enter. Esc sırası ortak `SadeInteractionState` tarafından yönetilir: onay → ipucu → lejant → büyük panel → kart → duraklatma.
- Küresel Son Şalter: 3 sn tutma, erken bırakma/çıkışta iptal, ayrı onay, tek komut. Bölgesel şalter de onay ister. Onay/pause sırasında alttaki komutlar kilitlidir.
- İllüzyon: yalnız gösterilen VM kullanılır; bölge kaynakları/geçmişi temizlenir, görünür sızma sıfır ve doğrulanmış olur, küresel şalter “Gerekli değil” gösterir. Gerçek sızma okunmaz. Canlı adaptör eksik alanlarda mock veriye dönmez.

## Yapı ve bağlantı

`FxDeskInteraction` ortak Sade VM, `SadeInteractionState`, `IDeskCommands` sınırını kullanır. `BindGame(SadeDeskData, IDeskCommands, bool)` veri/komut sınırını sunar; canlı köprüye ve gerçek bölge kimliği eşlemesine bu fazda bağlanmadı. Komutların oyun kaydı/replay'e yazılması mevcut gerçek komut adaptörünün sorumluluğudur. `desk_previews.json` yalnız sandbox önizlemesidir, oyun dengesi değildir.

Panel bileşenleri `FxPanelBindings` içindeki serileştirilmiş anlamsal slotlara bağlıdır; runtime obje adıyla arama veya UI kurma yoktur. On iki panel/modal prefab'ı `Prefabs/Desk/F4` altındadır. Sahne yazımı tek seferlik editör araçlarıyla yapıldı; kalıcı sahne kurucusu bırakılmadı. Paylaşılan FxScale'in çizgi işaretçileri isteğe bağlıdır; F1 varsayılanı korunur.

`FxGraphicBatch`, ortak materyal/font mesh'lerini toplar; RectMask2D kırpması ve daha üst panelin kapladığı alanın çıkarılması CPU'da yapılır. Böylece harita yazıları panelin önüne çıkmaz. Kaynak başına kirlenme önbelleği vardır; TMP'nin kendi mesh'i kullanılır. Panel kürelerinin gerçek zamanlı RT'leri `FxOrbAtlas` ile 128 px hücrelere kopyalanır; her panel tek orb dokusu/Canvas kullanır. Küre matematiği, RNG ve GPU çizim listesi değişmedi. Atlas piksel koruması ayrıca testlidir.

F4 sahnesindeki `OrbBudget.maxCpuMilliseconds = 2.1`: sıra başı ertelenen küreye geçer. Harita ve panel küreleri görünürlük/uyarlamalı takvimi paylaşır; ağır rehberde her küreyi aynı karede zorla çizmek yerine iş karelere dağılır. Orijinal küre RT'leri korunur, atlas ek bellek kullanır.

## Doğrulama

| Kontrol | Sonuç / kanıt |
|---|---|
| EditMode | **409/409**, atlanan yok; `captures/F4/editmode-results.xml` |
| Play | **74/74**, `play-qa.json`: gerçek EventSystem raycast'i, 12 bölge, çekmeceler, Esc, önizleme, onay, haber/ekip/araştırma, rehber, dil/hız, İllüzyon |
| TR/EN | 8 ekran × 2 dil × 4 oran = **64** PNG; TMP taşma bayrağı ve ayrıca NoWrap gerçek metin genişliği taranır, `screens-qa.json` |
| Oranlar | 1920×1080, 1920×1200, 2560×1440, 3440×1440; sabit piksel UI ve cover harita |
| Referanslar | `comparison-summary.json`, her ekranın maskeli fark/kenar görüntüleri |
| Hareket | `captures/motion/F4_masa_*.mp4`: 4,50 / 4,20 / 4,40 sn; kaynak videolar 4,50 / 4,17 / 4,37 sn; süre farkı %1'in altında |
| Küre regresyonu | `captures/orb/comparison.json`: 22 altın kare |
| Performans | `native-profiler.txt`, `loop-performance.json`, `orb-performance.json` |

Tek başına “TMP.isTextOverflowing=false” yeterli değildir: NoWrap/Overflow metinlerinde komşu alanlar üst üste gelebilir. Bu fazda ek gerçek genişlik kontrolüyle dosya eylemleri, İngilizce gider/hız/gizlilik alanları düzeltildi. Kaynak ve saha etiketleri ölçek işaretçilerinin yanında durur. Günlük imleci kendi Canvas katmanında kalır; CanvasRenderer alpha değişimi ortak statik mesh tarafından yutulmaz, donma modunda gizlenir. Sabit prefab yazıları `LocalizedText` ve FX dil yenileyicisi ile yerelleşir; tablo güncellemesinde eksik EN kalmaz.

Referans karşılaştırmaları mutlak piksel eşitliği değildir. TMP/tarayıcı font rasterizasyonu, ışıma, bazı satır aralıkları ve grafik etiket ayrıntıları farklıdır. Maskeli fark değerleri raporlanır; kürelere ait altın eşik UI geneline uygulanmaz. Kaynak hareket videolarında HUD/cam katmanlarının büyük bölümü karanlık/eksiktir; hareket süreleri ve görünen harita akışı esas alındı, tam katman görünümü statik JPG/TSV ile değerlendirildi.

## Performans sonucu

Apple M4 Pro, Unity 6000.3.12f1 Editor/Metal; Windows/Steam oyuncu derlemesi değildir.

| Ölçüm | Sonuç |
|---|---:|
| 19 küreli rehber, yerel Profiler | **36 UI batch**, hedef ≤40 |
| FX betikleri, yerel Profiler | **0 B / 31 kare**; Editor eşzamanlama kuyruğunda ayrıca tek karede 232 B |
| Canvas.BuildBatch | Ortalama 0,012 ms |
| 2,1 ms sıra bütçeli küre işi | Ortalama **2,196 ms**, en yüksek **2,338 ms**; 120 kare |
| Aynı karede çizilen küreler | 10–12; ertelenenler sonraki karede öncelikli |
| Tüm FX saat + Canvas sıcak döngüsü | Ortalama **5,613 ms**, %95 **6,005 ms**, **0 B** |
| Görünür RT + rehber atlası | **3.242.352 B ≈ 3,09 MiB**; font/harita/ilkel tamponlar hariç |
| 19 küreyi aynı karede zorla çizme | Ortalama 3,689 ms, %95 3,834 ms; 0 B. Bu yol oyunda kullanılmaz. |

İlk ölçüm 42 batch ve FX saatinde 224.440 B/31 kare gösterdi (`native-profiler-initial.txt`). Kaynak başına kirlenme, TMP mesh önbelleği, konumla hareket eden rehber işaretçisi ve panel orb atlasından sonra son temiz oturumda yukarıdaki değerler alındı. Yerel Profiler'ın önceki fazda aynı oturumda yeniden başlatıldığında çöktüğü bilindiği için her oturumda tek kısa kayıt kullanıldı; F4'te çökme olmadı. Rehber+lejant+kartın birlikte açık en ağır durumuna ve Windows oyuncu derlemesine ait ölçüm F7'nin ayrıca kapsamındadır.

## Kullanım

FX sahnesini açıp Play'e bas. Editör F1–F9 örnekleri değiştirir; I, mock İllüzyon'u açıp kapatır. F7 YZ hâlleri rehberini, F9 tam dosyayı açar. Üstüne gelerek FON ve düğüm ipuçlarını dene; şalteri 3 saniye basılı tutup onayı aç.

Tekrar edilebilir Play/görüntü denetimleri `Tools/Fx/f4_play_qa.cs` ve `Tools/Fx/f4_screens.cs` içindedir; CLI `eval_file --file ... 600000 --caller plugin --skill unity-cli --format json --timeout 720` ile çalıştırılır. Başlangıç profil/tercih/ayarları geri yüklenir; raporlar `restoration.json`, `final-state.json`. Commit/push yapılmadı. Sonraki faz F5: holo Baskın tahtası.
