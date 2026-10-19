# Son Şalter · FX F1 uygulama notları

18 Ekim 2026. Kullanıcı onayı: “1.Başlat, 2.Doğrulandı”. **F1 tamamlandı; F2 başlamadı.**

## Teslim

Galeri: `Assets/Corrigible/Fx/Scenes/FxUI_Gallery.unity`. Editörle açıp Play'e gir. Üstten dil, hareketi azalt ve referans evresi; alttan önceki/sonraki sayfa seçilir. Galeri yalnız görsel altyapıdır; canlı oyun sahnesini değiştirmez.

| Altyapı | İçerik |
|---|---|
| Assembly | Corrigible.Fx, Corrigible.Fx.Editor, Corrigible.Fx.Tests; mevcut Desk → FX bağımlılığı eklenmedi |
| Fontlar | Chakra Petch 400/500/600/700; IBM Plex Mono ve Sans 400/500/600. On ayrı statik SDF; padding 9, 72–90 örnekleme, Mono 1024², diğerleri 2048²; gerçek ağırlık tabloları |
| Işıma | 8 ortak TMP materyal ön ayarı; LabelHalo koyu underlay + cyan glow. Font atlası uyuşmayan materyal başka fonta zorla bağlanmaz |
| Tema | JSON tokenları, harita ve Baskın paletinden FxTheme.asset. 117 tipografi tanımı. Koşullu renk açıklamalarının ilk/varsayılan rengi pişirilir; bağlam rengi çağıran görünüm tarafından seçilir |
| Sprite | Manifestin 73 sprite’ı, 108 izinli beyaz Sade simgesi; PPU 200, kenarlar ×2, padding/tint/imageType metadatası. FxUI atlasında padding 4; döndürme ve tight packing kapalı |
| Doku | 14 doku; desenlerde Repeat, zemin/ışık konisi/tarama bantlarında Clamp. İki 4K harita katmanı için Standalone BC7, max4096; mipmap yok |
| Shader | UI/FxAdditive ve UI/FxLine; stencil, RectMask2D, alfa kırpma. Kesik çizgi, UV akışı, LED ve panel süpürmesi ortak shader saatinden ilerler |
| Hareket | FxClock → FxLoopRunner; unscaled saat, veri tanımlı FxLoop, CSS cubic-bezier(.2,.8,.2,1) karşılığı FxTween; FxEntrance. Tek Update saatte; görsel döngü başına Update yok |
| Durumlar | Hareketi azalt ve referans dondurması ayrı; evre sözlüğü desteklenir. Girişler tamamlanır, daktilo tam metin olur, imleç/tarama ışığı gizlenir. Oyun hızı FX saatini durdurmaz. Azaltılmış hareketten çıkmak tamamlanan giriş/daktilo/çizimi yeniden başlatmaz |
| Yerelleştirme | DOĞRULANDI / VERIFIED; sahte rozeti TMP SAHTE / FAKE. FxLabel mevcut Loc/DeskLoc'a bağlı. Ortak strings.txt'e 40 yeni metin ve İngilizceleri eklendi; Sade güven terimi değiştirilmedi |

Ortak prefab’lar (`Prefabs/Common/`, her biri kendi bileşen dosyasıyla): FxPanel, FxSweep, FxGlow, FxLineGraphic, FxLedBar, FxHexIcon, FxTrustBadge, FxButton, FxKeyChip, FxCloseButton, FxPill, FxStamp, FxScale, FxPips, FxTypewriter, FxDrawOn, FxTextGlow.

Panel, gölge ve ışıma taşmaları serialized RectTransform’larda. LED belirsizliği mor kesik çerçeve, dolgu ve işaretçi ayrı ışıma; işaretçi 15 px. Ölçek 30/70 çizgileri yüzde anchor ile boyuta uyar. Düğme yazıları tema tipografisine bağlı; tur bitir 58 px yüksek ve 17 px yazılıdır. Camın altı varyantı ve 96×64 / 160×100 / 224×136 paneller ilk sayfada yan yana gösterilir.

Galeri kamera bağlantısını serialized tutar (Screen Space – Camera). Bu, render yakalamasından sonra Editor Game View ile overlay canvas ölçüsünün farklı kaldığı durumda da UI raycast’ini doğru kameraya bağlar. URP/Volume/kalite ayarı değiştirilmedi; Bloom eklenmedi.

Sahne/prefab’lar tek seferlik editör yazımıyla kaydedildi; kurucu betik kaldırıldı. Çalışma zamanında hiyerarşi üreten kurucu veya obje adıyla arama yok. Asset menüleri yeniden kullanılabilir: `Tools > FX UI > Import All Foundation Assets`, `Import Tokens`, `Build Atlas`.

## Doğrulama

- Son derleme başarılı. **357/357 EditMode**, 0 atlanan/başarısız. Önceki 345 teste 12 F1 örneği eklendi: manifest/kenar/tint, on fontun tüm karakterleri, 117 tipografi tokenı, eğri örnekleri, saat/dondurma, Bézier, prefab eksik script, güven terimleri ve döşeme ayarları.
- **27/27 Play kontrolü**: UI GraphicRaycaster → gerçek EventSystem tıklama işleyicileri üzerinden TR/EN, önceki/sonraki ve iki hareket anahtarı; devre dışı düğmede komut çıkmaması; tek saat; azaltılmış harekette tam metin/gizli imleç; oyun dururken saat; dört çizgide geometri sayacının değişmemesi; azaltılmış hareketten çıkınca üç giriş türünün geriye sarmaması.
- TR/EN üç sayfa görüntüsü incelendi. Sprite kataloğundaki `fx_tr_fake` kaynak sprite’ının TR yazısı kasıtlıdır; gerçek rozet TMP ile çevrilir. Font örneğindeki Türkçe karakter dizisi her iki dilde glif kontrolü için aynıdır.
- 1.000 sıcak `FxClock.Advance` çağrısı: **0 B yönetilen tahsis**, 0,915 ms toplam; dört çizgide mesh sayacı değişmedi. Bu doğrudan saat/yürütücü ölçümüdür; tüm kare, GPU veya hedef Steam PC performansı değildir.
- Aynı duran sahnede yalnız kesik akış evresi 0 → 0,5 yapıldığında çizgi alanında **1.492 piksel** değişti. RectMask2D'nin iki yanının dışında 0 çizgi pikseli. Stencil kaynağı 36 px antialias’lı dairenin 112 px büyütmesidir: ideal yarıçap sınırında 16 kenar pikseli bulunur; kaynak AA bandı olan 4 px’in ötesinde 0 piksel vardır. Matematiksel keskin daireyle birebir kenar iddiası yapılmaz.
- Son Play başlangıcından sonra yeni konsol hatası **0**. Konsolda önceki düzeltilmiş derleme ve geçici QA SendMessage kayıtları korunmuştur; geçmişi temizleyerek sonuç gizlenmedi.
- Başlangıçtaki temiz DeskGame sahnesi, kayıt/profil, PlayerPrefs ve Editor/Player ayarları geri yüklendi. TMP’nin kurulum sırasında kirlettiği mevcut Sade dinamik font asset’i özgün hâline döndürüldü.

Kanıtlar `captures/F1/`: altı TR/EN galeri PNG’si, iki akış evresi PNG’si, `editmode-summary.json`, `play-qa.json`, `motion-qa.json`, `pixel-qa.json`, `console-summary.json`, `restore-summary.json`.

## Maliyet ve sonraki faz

On font atlası Editor’da toplam **65.021.312 bayt (~62 MiB)** raporlandı (CPU/GPU taraflarını içeren Unity bellek ölçümü); yalnız piksel düzlemleri yaklaşık 31 MiB. İki 4K BC7 harita için teorik GPU maliyeti ~15,8 MiB. Oyuncu build’i ve canlı 12/19 küre bütçeleri F2–F7’de ayrıca ölçülecek. Galeri çok sayıda bileşeni bilerek aynı anda gösterir; masa/Baskın draw-call bütçesinin kanıtı değildir.

F2: kanonik `fx_orb.js` portu, ayrı deterministik RNG/simülasyon, RT çizimi, 22 altın kare ve FxOrbLab. Bu F1'de küre taklidi, oyun mekaniği veya Sade görünüm ayrımı yapılmadı.
