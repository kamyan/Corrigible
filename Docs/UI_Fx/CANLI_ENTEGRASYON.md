# FX · canlı oyun entegrasyonu

19 Ekim 2026. FX masa ve taktik baskın artık gerçek GameController / Simulation komutlarıyla çalışır.

## Giriş noktaları

- `Assets/Corrigible/Fx/Scenes/FxDeskGame.unity`: Build Settings'teki ilk etkin sahne. Editörde bu sahneyi açıp Play'e bas.
- `Assets/Corrigible/Fx/Scenes/FxBaskin.unity`: gerçek operasyon başlayınca eklemeli yüklenir; tek başına örnek oyun üretmez.
- Özgün `Desk/Scenes/DeskGame.unity`, `Scenes/Baskin.unity` ve `Scenes/Main.unity` korunur. FX sandbox'ları örnek/görsel QA içindir.

Girişte rakip/zorluk, eğitim, kampanya, kurallar, dil ve kayıt/devam seçenekleri mevcut ekranlarla sunulur. Harita/HUD/çekmeceler FX'tir; giriş, olay/hediye, eğitim, operasyon belgesi, ayarlar ve son ekranları ortak mevcut sunumu kullanır. Bu ekranların tamamını yeni FX görsel diline yeniden çizme ayrı bir tasarım çalışmasıdır.

## Veri ve komut sınırı

`DeskPresenter → IDeskSurface → FxLiveDeskSurface` aynı `SimDeskDataSource` / `SimDeskCommands` köprüsünü kullanır. FX, `SadeDeskData` görünüm modelinden beslenir. Gerçek sızma okunmaz; küre, gösterge, kart ve kaynaklarda yalnız Algı Katmanı verileri kullanılır. Araştırma, operasyon alımı, rezerv, ekip gönderme, toparlanma, diplomasi ve rapor toplama GameController'dan geçer; kayıt biçimi ve Core/Prototype kuralları değişmedi.

Canlı harita 12 gerçek bölge kimliğini ve özgün Sade ülke çokgenlerini kullanır. Mock Orta Amerika slotu Japonya-Kore geometrisiyle değiştirilmiştir. Ekip/bağ/rapor slotları sahnede yazılıdır; çalışma anında hiyerarşi kurulmaz. Bölge kartında günlük fon katkısı, toparlanma/diplomasi ve hazır ekip komutları vardır. Kısa ekranlarda kart gövdesi kaydırılır. Haber havuzu canlı köprünün 20 satırını gösterir; uzun başlıklarda satırlar büyür. Operasyon araştırmaları peşin satın alma metniyle gösterilir.

Açık belgeler/ayarlar/son ve taktik sahne masa komutlarını kilitler. `DeskInput` yalnız ortak ekranların klavyesini yönetir; FX masanın kendi girdisi vardır. Eğitim ve olay hedefleri kamera Canvas'ından Overlay Canvas'ına ekran koordinatları üzerinden çevrilir. Canlı sahnelerde mock kaynak, FxDebugMenu, F1–9 senaryo değiştirme veya illüzyon açma kısayolu yoktur. Hareketi azalt tercihi duraklatma menüsünden değişir ve baskına taşınır.

## Baskın ve dönüş

`FxRaidDataSource` görsel makine evresini paylaşır; `FxRaidSource` yalnız örnekler, `FxLiveRaidSource` gerçek bekleyen operasyon içindir. Canlı tahta `Baskin.Create(S.Raid.Params)` ile açılır. Makine sonucu bir kez hesaplanır; görsel kopya canlandırılır. Geri al, önizleme, onay, geri çekilme ve sonuç aynı ortak kurallardan gelir.

`RaidLauncher` sahne adını serialized alır; eski sahnelerin varsayılanı Baskin'dir. Baskın süresince masa kamerası, canvâsları, girdisi ve FX saati kapatılır; kapanınca önceki enabled değerleri geri gelir. Eklemeli sahnede tek EventSystem ve tek etkin FX saati vardır. Sonuç `ResolveRaid` ile bir kez kaydedilir; oyun değişikliği sahneyi kapatır ve operasyon raporu açılır. Kayıttaki bekleyen baskın yeniden yüklenebilir. Mevcut kayıt tasarımında baskının stratejik başlangıç parametreleri saklanır; taktik hamlelerin ortasından devam eklenmedi.

Ortak Sade köprüsündeki `RaidCheck() ?? RaidCheckResources()` ifadesi her zaman bir hata metni ürettiği için geçerli baskını da kapalı gösteriyordu; bu kontrol gerçek `RaidCheck()` sonucuna düzeltildi. Ayrı regresyon testi var.

## Doğrulama

- 445/445 EditMode: yeni sahne bağlantıları, geçerli baskın görünürlüğü ve boş isteğe bağlı etiketlerin ortak çizimi bozmaması dahil. Temel/deneysel C#/JS parite testleri geçti.
- Gerçek Play: masa 48/48, baskın 12/12, dönüş 7/7, bekleyen baskın kaydı 1/1, gerçek raycast/menü/diplomasi 5/5, eğitim/oyun sonu ekranları 7/7. Masa eylemleri bu senaryolarda elle simülasyon durumu değiştirmeden yürütüldü.
- TR/EN, 1920×1080, 1920×1200, 3440×1440: 48 görüntüde 6.366 metin incelemesi, 0 taşma/eksik glif. Kart sınırı ve olay hedefi dönüşümü 18/18.
- Kaynak: `captures/Live/*-qa.json`, `editmode-tests.json`, `editmode-results.xml`, `text-check.txt` ve PNG'ler.
- Yeniden çalıştırma: `Tools/Fx/live_desk_qa.cs`, `live_raid_qa.cs`, `live_return_qa.cs`, `live_input_qa.cs`, `live_visual_qa.cs`, `live_screens_qa.cs`. CLI eval_file ile Play'de çalıştırılır. **Bu senaryolar oyuncu kaydı/profil/tercih yazabilir; önce yedekle ve sonra geri yükle.**

Testlerden sonra profil, başlangıçta bulunmayan kayıt dosyaları, PlayerPrefs ve font/ayar/malzeme yan değişiklikleri başlangıç yedeğiyle birebir geri yüklendi (`captures/Live/data-restore.json`: all_restored=true). Editor temiz FxDeskGame sahnesinde ve Play kapalıdır.

Bu entegrasyon yeni Windows build/GPU veya tam piksel eşliği kabulü değildir. F7'de açık bırakılan görsel/performance kabul maddeleri geçerlidir; canlı UI eklerinin aynı native performans bütçesini sağladığı iddia edilmez. Kullanıcı commit/push istemedi; yapılmadı.
