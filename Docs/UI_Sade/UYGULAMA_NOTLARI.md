# Son Şalter · Sade arayüz uygulaması

18 Ekim 2026. Kaynak paket: `SonSalter_Sade_Paketi/PROMPT.md` (1.1). Faz ayrıntıları `F2_NOTLARI.md`–`F7_NOTLARI.md`; kararlar `KARARLAR.md` içindedir.

## Sahne ve veri bağlantıları

| Sahne | Kullanım | Veri/komut sınırı |
|---|---|---|
| `Assets/Corrigible/Desk/Scenes/SadeDesk_Sandbox.unity` | S-Masa1–9 örnekleri | `MockSadeDeskDataSource`, `MockSadeDeskCommands` |
| `Assets/Corrigible/Desk/Scenes/DeskGame.unity` | Ana oyun, giriş ve zorluk seçimi | `SimDeskBuilder` → `DeskPresenter`; `SimDeskCommands` → `GameController` |
| `Assets/Corrigible/Desk/Scenes/RaidBoard_Sandbox.unity` | S-Baskın1–9 ve B3/B4/B7b örnekleri | `MockRaidDataSource`, `IRaidCommands` |
| `Assets/Corrigible/Scenes/Baskin.unity` | Bekleyen gerçek operasyon | `LiveRaidDataSource` → `GameController.ResolveRaid` |
| `Assets/Corrigible/Desk/Scenes/SadeUI_Gallery.unity` | Ortak bileşen galerisi | Oyun kaydına bağlanmaz |

Hiyerarşiler sahne ve `Desk/Prefabs/Sade/` altında saklanır. Serileştirilmiş referanslar kullanılır; çalışma zamanında sahneyi sıfırdan üreten kurucu yoktur. Geçici sahne düzenleme betikleri Assets'e eklenmez.

`SimDeskBuilder` yalnız Algı Katmanı'nın gösterdiği durumu sunar. Baskın VM'si Core'un izin verdiği hareket, yetenek ve görünür niyetlerden kurulur. Taktik sonuç bir kez `raidres` komutu olarak kaydedilir; sonuç ekranından dönüş ana masa girdisini yeniden açar. Sandbox ön ayar arayüzü canlı kaynağa uygulanmaz.

## Bileşenler

- Masa: `MapCoverFitter`, `RegionMeshGraphic`, `SadeMarkerAnchor`, `SadeMapView`, düğüm/etiket/ekip/iplik görünümleri. `SadeMapTextLayer`, yazıları ortak Canvas’ta gruplar; serileştirilmiş çapalarıyla düğüm hareketi, ölçek ve görünürlüğünü izler. Harita dokusu kaplayarak ölçeklenir; işaretler ve yazılar kendi boylarını korur.
- Köşeler: `HudChipView`, `GoalChipView`, `ClockPlaqueView`, `SadeDockView`, `KillSwitchEnvelopeView`. Göstergelerin ipuçları ekrana göre yerleşir.
- Açılır içerik: `SadeDeskInteraction`, `SadeInteractionState`, `PopupCardBase`, bölge kartı/tam dosya, gösterge/düğüm/lejant ipuçları ve beş çekmece. Seçim ipliği ve seçili bölgenin görünürlüğü yerleşime katılır.
- Baskın: mevcut `RaidBoard3D` maketi, `SadeRaidCameraFraming` (k=1,5), `RaidScreenLabel`, `BoardBackgroundView`, `RaidPreviewOverlay`, `RaidOutcomeSpot`.
- Baskın HUD'u: `RaidHudView`, `RaidTeamCardView`, `UnitCardView`, `HoverInspectorView`; durum panelleri `RaidPanelsView`, havuzlanmış `RaidLogRowView` ve açılış geçişi `RaidCardMotion`.
- Ortak görünüm: `SadeTheme`, `RaidTheme`, `DeskTheme`; `DeskUI` ve `SadeUI` atlasları. Eski C bileşenleri giriş, olay ve oyun sonu belgelerinde kullanılır.

## Dil ve geliştirme kontrolleri

`Loc` Türkçe kaynak/İngilizce tabloyu, `DeskLoc` arayüzün dil geçişini yönetir. Dil değişimi simülasyonu veya RNG'yi ilerletmez. Girişteki oyuncu profili satırları oyunun başlangıç profilinden güncel dilde yeniden üretilir; baskın sonuç bölge adı güncel çeviriden alınır.

Editor/Development derlemelerinde: masa örneklerinde F1–F9, baskın örneklerinde Shift+F1–F9; Ctrl+F1/F2/F3 sırasıyla B3/B4/B7b. F10 dil; F12 referans katmanı; F11 opaklık, Shift+F11 yanıp sönme. Örnek kısayolları canlı operasyon durumunu değiştirmez. Düz F1 baskın kurallarını açar.

`Corrigible > Sade UI > Check Text` etkin masa/baskın görünümünü iki dilde denetler: gerçek glif sınırları, kırpılma, minimum 11 px (Kalam 15), Auto Size ve case stilleri. `DeskCapture` / F7 denetim betikleri dört çözünürlükte yakalama yapar. `DeskPerfProbe.Run(path, idleFrames)` raporu seçilen konuma yazar; eski C ölçümleri üzerine yazmak gerekmez.

## Tasarıma uyarlamalar

- Gerçek oyun bölge kimlikleri korunur; çokgen eşlemesi `KARARLAR.md` içindeki A seçeneğidir. Bazı görsel sınırlar oyun bölgesini birebir temsil etmez.
- Araştırma gerçek proje/peşin operasyon/ekonomi/Hizalama panosunu gösterir; paketin örnek teknoloji sayıları oyun kuralı yapılmaz.
- Bölge dosyasına fon katkısı, toparlanma/diplomasi ve ekip seçimi; haritaya saha raporu fişleri dahil edilmiştir.
- Haber denetimi mevcut bölge denetimi komutuna bağlanır.
- 3D maket ve gölgelendirme mevcut URP düzenini kullanır; referans çizimle raster eşitliği iddia edilmez. Önceki fazların fark/kenar karşılaştırmaları belgelenmiştir.
- Eski `RaidView` kaynakları korunur; canlı sahne Sade HUD'u kullanır. Ses ve gamepad bu paketin kapsamı değildir.

## Son doğrulama

F7 sonuçları, ölçüm kapsamı ve ekran yakalamaları `F7_NOTLARI.md` ve `captures/F7/` altında tutulur. Kayıt/profil/tercih dosyaları test öncesi yedeklenir ve test sonunda karşılaştırılarak geri yüklenir.
