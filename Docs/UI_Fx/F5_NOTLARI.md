# FX · F5 holo Baskın tahtası

19 Ekim 2026. Kullanıcının “F5 e geç” talebi uygulandı. Sahne: `Assets/Corrigible/Fx/Scenes/FxRaid_Sandbox.unity`. F5, ayrı FX örnek tahtasıdır. HUD, kartlar, komut kilitleri ve sonuç/çöküş akışı F6 kapsamındadır; canlı `DeskGame` ve `Baskin` sahneleri korunur.

## Uygulama

- Mevcut `RaidPieceLibrary` mesh'leri, 8×8 kare yerleşimi, 35°/45° ortografik kamera ve 1080p'de 132,5825 piksel/dünya birimi kullanılır. Ekip kökleri 45°, robot kökleri −135° yönlenir. Kaynak Sade prefab'ları değiştirilmedi.
- Üst/batı/güney yüzler `raid_fx_palette.json` üzerinden ayrı Unlit malzemelere dönüştürüldü. FX plan dokusu özgün ozalit UV eşlemesini korur. Akrilik/tape/kurum alfası ayrı saydam malzemelerdedir. Işık, gerçek zamanlı gölge ve Bloom kullanılmaz.
- `FxRaidEdgeMesh`, üçgenleri konumlarına göre kaynaklar; düz yüzlerin iç köşegenlerini çıkarır. >30° sert kenarlar ve yumuşak yüzlerin siluet adayları toplam **47** kenar mesh'ine pişirildi. `FxEdgeLine` clip uzayında 1 fiziksel piksel, ikinci geçişte 3 piksel/α0,40 çizer. Siluet testi ve ters Z derinlik ofseti shader'dadır. İki uçta AA yan koordinatının işareti aynı tutulur; buna regresyon testi eklendi.
- Uzak kamera Canvas'ında radyal arka plan, 1500×880 projeksiyon konisi, tarama çizgileri ve izdüşürülmüş siyah tahta gölgesi vardır. Tahta çokgeni stencil maskedir; 90 piksel tarama düzlemi 6,5 saniyede geçer. Giriş 1 saniye; titreme 1 saniye gecikmeli 7 saniyelik döngüde 0,86/0,90 dipleridir.
- Hareket/hedef/seçim/isabet/onarım işaretleri ve kırmızı uyanma halkaları uygulanır. Uyanma: 0,33 birim halka, 0,17 iç kesik halka, sekiz ışın ve iğne. Renkler ve paletin sayısal işaret ölçüleri `Settings/FxRaidTheme.asset` içindedir.
- `FxRaidGroundProjection`, sabit piksel işaretlerini ve niyet çizgilerini ekran izdüşümlerini koruyarak y=0,075 dünya düzlemine taşır. Böylece figürler işaretleri örter. İşaret mesh'i ve stencil çokgeni yalnız veri/kamera/çözünürlük değişiminde yenilenir.
- Niyet: kırmızı temel ok, açık renk 2,2 piksel akış, 3/26 kesik, 28,6 piksel/sn; etkin sıra kehribar, biten sıra soluk ve üstü çizilidir. `nomark`, `now`, `cert: maybe/scanned` mock alanları sunuma eşlenir. “Yalan olabilir” kırmızı dolgu/kehribar kenar ve soru fişi kullanır.
- Can fişleri, sıra bayrakları, harfler, düşme/tur bitti hâlleri, ZIRH/ÇARPIŞMA/HEDEF KIRILIR damgaları ve süzülen hasar yazısı vardır. Chakra Petch harf kutuları ve 16 piksel soru simgesinin 24 piksel metin kutusu taşma denetimiyle düzeltildi.
- Düğüm küresi dolap tepesinden 64 tasarım pikseli yukarıdadır: r22, tohum55, normal; ±5 piksel/3,2 saniye süzülme. Kehribar bağ 1,4 piksel, 3/4 kesik, 20 piksel/sn'dir. Bağın uzunluğu değişirken kesiklerin piksel boyu korunur. F2 küre modeli düzey değişiminde korunur; 58→64 geçişi yeniden kurulumla kesilmez. Mock B5=64, son tur B6/B7=86; diğerleri58. Gizli bölge sızması veya keyfî tur formülü kullanılmaz.
- Bütün hareketler ortak `FxClock` tarafından yürütülür. Azaltılmış harekette giriş tamamlanır, titreme1 olur, süzülme durur. Eski `RaidPiece` davranışları sahneden çıkarıldı; parça başına Update/rastgele salınım yoktur.

## Kaynak ve kullanım

`MockRaidBoardDataSource.Parse` ve `Baskin.Intents` yeniden kullanılır; simülasyon/oyuncu komutu eklenmedi. Her örnekte `State.Rs=0` korunur. 64 hücre, nesne varyantları, birim ve etiket havuzları sahnede serileştirilmiştir. `FxBoardBase.prefab` ve malzeme/mesh varlıkları FX altında bulunur. Tek seferlik sahne yazıcısı kaldırıldı; runtime obje adıyla arama veya sahne kurma yoktur.

Sahneyi açıp Play'e bas. **F1–F9** sırasıyla B1, B2, B3, B4, B5, B6, B7a, B7b, B8 örneklerini yükler. Inspector'daki `preset` alanı da kullanılabilir. Bu kısayollar örnek durum seçer; F6'nın oynanabilir HUD komutları henüz bu sahneye eklenmedi.

## Doğrulama

| Kontrol | Sonuç / kanıt |
|---|---|
| EditMode | **419/419**, atlanan yok; `captures/F5/editmode-results.xml` |
| Play | **76/76**; dokuz örnek, havuz/can/RNG, belirsizlik/etkin sıra, zemin derinliği, küre model sürekliliği, önbellek, azaltılmış hareket ve GPU shader derlemesi |
| Kamera | Dört oranda en yüksek sapma **0,026 piksel**, hedef ±4; düğüm çapası **64 piksel** |
| TR/EN | 9 durum × 2 dil × 4 çözünürlük = **72 PNG**, **0 taşma**; TMP bayrağı ve NoWrap gerçek genişliği birlikte denetlenir |
| Çözünürlükler | 1920×1080, 1920×1200, 2560×1440, 3440×1440 |
| Lokalizasyon | Tablo güncellendi, eksik EN=0. Mevcut TUR BİTTİ girdisi yeni kaynak grubuna taşındı |
| Hareket | `captures/motion/F5_baskin_holo_tahta.mp4`: **8,00 sn, 240 kare/30 fps, 1280×720**; referans da8,00 sn. Kaynak Unity kareleri1920×1080 |
| Performans | Sıcak saat + Canvas: ortalama **0.236 ms**, %95 **0.265 ms**, **0 B**; 160/160 karede bir küre, 40 fps takvimi |
| Küre | Çizilen güncelleme başına ortalama **0.215 ms**, RT **85.264 B**; ilkel/font tamponları hariç |
| Önbellek | 1.000 doğrudan tahta Tick çağrısında **0 B**, işaret mesh'i/izdüşüm yenilemesi0 |

Performans Apple M4 Pro / Unity Editor / Metal üzerindedir. Ölçüm kamera render'ını ve GPU süresini içermez; Windows/Steam oyuncu derlemesi veya tüm sahnenin kare süresi olarak yorumlanmamalıdır. B5'te998 etkin MeshRenderer vardır; toplam çizim çağrısı/yerel Profiler ve F6 HUD'uyla birleşik bütçe F7'de ayrıca doğrulanmalıdır. F5'te önceki oturumlarda çökmeye neden olan yerel Profiler yeniden başlatma yolu kullanılmadı.

Karşılaştırma tahta çokgeniyle sınırlıdır; F6 HUD alanları ve değişken küre dışarıda bırakılır. Ortalama RGB farkı **27.12/255**, referans kenarlarının ±2 piksel örtüşmesi **%73.7**. Örnek duvar üst yüzü Unity'de(22,64,90), JPG'de(22,64,89). Kaynak 3B parçalar ile referans çizimin sadeleştirilmiş robot/fan/etiket siluetleri ve ışıma rasterizasyonu farklıdır. Bu ölçüm bütün parçaların ya da etiketlerin ±4 piksel görsel eşliği değildir; kamera/kare yerleşimi koşulu ayrı ölçülmüştür.

## Yeniden çalıştırma ve geri yükleme

`Tools/Fx/f5_play_qa.cs`, `f5_screens.cs`, `f5_performance.cs`, `f5_motion.cs` Play modunda CLI `eval_file --file ... 600000 --caller plugin --skill unity-cli --format json --timeout 720` ile çalışır. `compare_raid.py` Pillow ile maskeli karşılaştırmayı üretir. `encode_f5_motion.swift` 240 PNG'yi videoya çevirir ve süre/kare örneklerini doğrular.

Başlangıçtaki temiz DeskGame, TR dili, profil/PlayerPrefs ve Project/Editor ayarları geri yüklendi. Testlerin yeniden serileştirdiği Sade prefab/font dosyaları yalnız HEAD içeriğinin başlangıç SHA256'sıyla eşleşmesi doğrulandıktan sonra geri alındı. Unity’nin üç oturum kimliği/sayacı yenilenmiştir; oyuncu tercihleri birebir korunur. `.DS_Store` oyun verisi karşılaştırmasına dahil edilmez. Kanıt: `restoration.json`, `final-state.json`. Core/Prototype, Sade kaynak kodu ve önceki FX fazları korunur. Commit/push yapılmadı. F5 sonunda duruldu; sonraki faz F6 HUD ve durum akışlarıdır.
