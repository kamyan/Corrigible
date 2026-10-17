# F3 · S-Masa etkileşimleri

Kaynak: paket 1.1 §A.11–14 ve S-Masa2–9. Sahne: `Assets/Corrigible/Desk/Scenes/SadeDesk_Sandbox.unity`.

## Kapsam

- Düğüm ve bölge alanıyla seçim; denizle kaldırma; Tab / Shift+Tab ile bölge dolaşımı.
- Bölge kartı, eylem önizlemesi, kart ipliği, tam dosya ve bağlantı çipleri.
- Araştırma, Haberler, Ekipler, YZ Günlüğü, Rehber çekmeceleri; büyük panel tektir, küçük bölge kartıyla birlikte açık kalabilir.
- 400 ms düğüm/HUD ipuçları ve bağımsız lejant kartı.
- Bölge/haber/ekip vurguları, ekip görevlendirme kipi, araştırma seçimi/duraklatma/devam, rehber konuları/sayfaları.
- Tehlikeli bölge eylemi ve üç saniye basılı tutulan küresel Son Şalter onayı; Esc öncelik sırası ve duraklatma menüsü.
- İllüzyon görünümü: analist işaretleri kaybolur, gösterilen veriler onaylı/yuvarlak duruma geçer; kapsam/onay ve 0 ms çelişkisi kart/dökümde ipucu olarak kalır.

## Sınırlar ve uyarlamalar

- F3 komutları `MockSadeDeskCommands` niyet günlüğüne gider. Bu, sunum ve girdi doğrulama sahnesidir; gerçek oyun komut kaydı ve ana oyun köprüsü ayrı aktarım adımıdır.
- Paneller paketin gösterilen JSON verilerini okur. Doğu Asya dışındaki kartlarda yalnız haritada gösterilen sızma/güven kullanılır; örnekte olmayan kaynak, tarihçe ve yerel istatistikler doldurulmaz.
- Önceki F0 kararına göre araştırmadaki hız bölmesi **acil durum fonu** olur: 0 / 15 / 30, varsayılan 15. Gerçek oyunda olmayan hız kuralı eklenmez.
- F1'de onaylanan sağ üst 8 px aralık sürer.
- Prefab ve sahne referansları serileştirilir. Tek seferlik yazım aracı çalıştırıldıktan sonra kaldırılır; çalışma anında UI hiyerarşisi üretilmez.

## Geliştirici kısayolları

- F1–F9: S-Masa1–9 örnekleri; referans katmanı aynı kareyi izler.
- F10: TR/EN; F11/F12: referans opaklığı/görünürlüğü.
- Ctrl+I: örnek İllüzyon görünümü.
- R / N / T / L / H: çekmeceler; 1–4: bölge eylemleri; B: uygun karttaki baskın.
- Boşluk, + / −: mock duraklatma/hız; Esc: onay → ipucu → lejant → büyük panel → bölge kartı → duraklatma menüsü.

## Doğrulama

- **EditMode:** 304/304 geçti; atlanan test yok. F2'deki 285 vakanın üzerine 19 F3 vakası eklendi. Kart, çekmece, ipucu, İllüzyon, Esc sırası ve eski bölgeden kalan grafik işaretlerini kapsar. Tam sonuç: `captures/F3_EditMode.json`.
- **Play:** 47/47 denetim geçti. Gerçek Input System klavye olayları, EventSystem ile 12 düğüm raycast'i, kart/dosya düğmesi, çekmeceler, haber vurgusu, görevlendirme, araştırma komutları, rehber, 400 ms ipucu, Esc sırası, pause engelleri, erken bırakma / 3 saniye tutuş / onay ve İllüzyondan dönüş denendi. Sahnede Missing Script ve gerçek `GameController` yok. Sonuç: `captures/F3_play_qa.json`.
- **Görsel ve metin:** S-Masa2–9 TR/EN 1920×1080; ayrıca S-Masa2/8/9 için 1920×1200 ve 3440×1440: toplam 22 yakalama. Sekiz örneğin iki dilde toplam 1832 etkin metin denetiminde **0 hata**; ayrıca 12 bölgenin kart ve tam dosya görünümlerinde (24 durum × TR/EN = 6648 metin denetimi) **0 hata** (`captures/F3_bolge_metinleri.json`). İngilizcede eylem adları sarılır; fon çubukları, kalan gün ve araştırma maliyet alanı ölçülen metne göre yerleşir. Kaynak: `captures/F3_gorsel_qa.json`, `F3_S*_metin_denetimi.txt`, `F3_bolge_metinleri.json`.
- **Performans:** `DeskPerfProbe` ile boşta 123 karede masa/Sade betiklerinin GC ayırması **0 B**, `Canvas.BuildBatch` ve `Canvas.SendWillRenderCanvases` **0/123 kare**. UI batch sayısı varsayılan masada (S-Masa1) **35** (`SadeMapCanvas` 24, `SadeHudCanvas` 11), bölge kartı + iplik açıkken (S-Masa2) **55** (`SadeMapCanvas` 30, `SadeHudCanvas` 11, `SadeContextCanvas` 14). Rapor: `captures/F3_performans.txt` (≤30 batch hedefi F7 cila fazında ele alınacak).
- **Karşılaştırma:** paket aracıyla sekiz referans için fark, kenar ve yanıp sönme çıktıları üretildi (`F3_S*_cmp_*`, `F3_karsilastirma.txt`). Bu ölçüm tüm ekranın karşılaştırmasıdır; tek başına bütün öğelerin ±2 px koşulunu kanıtlamaz. F0/F1'de kabul edilen HUD aralığı ve araştırma rezervi uyarlaması sürer.
- **Veri koruma:** yeni F3 yedeğiyle oyuncu profili, kayıt dosyası, `corrigible.*` tercihleri ve diskteki `ProjectSettings.asset` aynı. Editör `SadeDesk_Sandbox` sahnesinde Edit moduna döndürüldü.

## F3 Oyun Köprüsü: DeskGame.unity Entegrasyonu (17 Ekim 2026)

- **Amaç:** `Assets/Corrigible/Desk/Scenes/DeskGame.unity` sahnesinde eski C1 kriz masası düzeni yerine Sade Analist Masası'nı yerleştirerek tam simülasyona (`GameController` + `Simulation`) bağlamak.
- **Mimari ve Bileşenler:**
  - `DeskPresenter` kökü altında `SadeDeskInteraction` ve `SadeDeskView` referansları bağlandı.
  - Harita katmanı `SadeMapView` ve `MapCoverFitter` ile tüm ekranı kaplar; 12 bölge düğümü, etiketleri, fill collider'ları ve poligon seçim sınırları aktiftir.
  - Açılır paneller: `SadeRegionCardView` (küçük bölge kartı), `SadeFullDossierView` (tam bölge dosyası), 5 çekmece (`research`, `news`, `teams`, `aiLog`, `guide`), `SadeLegendCardView` ve `CardStringView`.
  - Mevcut tüm modaller korundu ve en üst Canvas hiyerarşisine bağlandı: `IntroScreen` (`GÖREV DOSYASI`), `CampaignScreen`, `EventModal`, `ConfirmModal`, `FakeWinScreen`, `GameOverView`, `SettingsCard`, `ToastStackView`, `TutorialLayer`, `RaidLauncher`.
- **Düzeltmeler ve Mekanik Uyum:**
  1. *Poligon Eşleme Seçim Sınırları:* `SelectionLayer/cn` ve `SelectionLayer/jk` altındaki polyline'lar düzeltildi. Çin (`cn`) anakara + Tayvan + Hainan olmak üzere 7 çizgiyi, Japonya-Kore (`jk`) ise 10 ada çizgisini barındırır. Canlı seçimde fosforlu kalem sınırı anakarayı sarar.
  2. *Araştırma Düğüm Metin Taşması:* Uzun kilit nedenlerinin (`LockReason`) 52 px düğüm yuvasında yan düğümleri örtmesi engellendi; kısa metin (`"kilitli"` / `"Satın al"`) düğümde gösterilir, tam metin ayrıntı panelinde yer alır.
  3. *Haritada Saha Raporları:* `FieldReportView` prefab'ları `SadeMapView` üzerinde çalışır ve tıklamayla fon ödemesi yapar.
  4. *Acil Durum Fonu:* Araştırma çekmecesinde 0 / 15 / 30 rezerv seçimi tam çalışır.
  5. *Modern Ekonomi & Tam Dosya:* Peşin operasyonlar, bütçe yükseltmeleri, toparlanma ve diplomasi tam dosyada aktiftir.
  6. *Kritik Haberler:* Kritik haber geldiğinde Haberler çekmecesi otomatik açılır.
- **Doğrulama Sonuçları:**
  - **EditMode Testleri:** 306/306 test geçti (%100 yeşil, 0 hata, 0 atlanan).
  - **PlayMode Doğrulaması:** `DeskGame.unity` üzerinde giriş dosyası açılışı, saat akışı, bölge seçimi, Event modal imzalamaları, tam dosya ve çekmece açılışları denendi; konsolda 0 hata / 0 exception.
  - **Kullanıcı Verisi:** `player_profile.json` ve `savegame.json` test öncesi yedeklendi ve test sonunda özgün durumuna geri yüklendi.

## Kullanıcı bildirimi sonrası DeskGame denetimi · 17 Ekim 2026

Önceki entegrasyon raporuna rağmen açılış menüsü görünmüyor, hız düğmeleri komut çalıştırmıyordu. İki sorun gerçek `DeskGame` Play oturumunda yeniden üretildi: giriş modeli açık, `Hold=true`, 1× düğmesinin olayı sonrası `Speed=0`.

- `DeskUI` kamera Canvas'ından overlay'e geçirildi. Sade masa 0/20/50; korunan overlay 60, modal 100, eğitim 105 ve ipucu 110 sırası aynı çizim modunda çalışır.
- `SadeDeskInteraction.clock` ve aktarımda boş kalan `cover`, `dock`, `sea`, altı `hudTargets`, bir `alerts`, lejant düğmesi/kâğıdı/konumu ve şalter zarfı bağlandı. `SadeDeskView` lejant ve zarf başlık/alt yazı başvuruları da tamamlandı.
- Dört panelin olmayan `sade.paper.card` anahtarı, mevcut `sade.paper` anahtarına düzeltildi; pembe yüzeyler giderildi.
- İki yeni sahne testi; hızın gösterilen saatle aynı bileşene bağlandığını, tüm bu başvuruları, tek klavye girdi sahibini, renk anahtarlarını ve korunan ekranların Sade panellerinin önünde çizildiğini denetler.
- **308/308 EditMode** geçti: `captures/F3_DeskGame_EditMode.json`.
- **128/128 canlı kontrol** geçti: `captures/F3_DeskGame_audit.json`. Gerçek oyun kaynağıyla üç rakip seçimi, göreve başlama, 0→1→2→4→1→0 hızları ve seçili durumları, 1×'te gerçek zaman ilerlemesi, beş çekmece aç/kapat, lejant aç/kapat ve 12 bölge kartı kontrol edildi. Otomatik bölüm bileşenlerin PointerClick olay yolunu kullanır; hızlar ayrıca Game View üzerinde gerçek fareyle doğrulandı. Editor update içindeki `Screen` boyutu Game View boyutundan farklı olduğundan otomatik masaüstü koordinat raycast'i bu koşulda kullanılmadı.
- Giriş dosyası TR/EN ve masa TR görüntüleri: `captures/F3_DeskGame_intro_tr.png`, `F3_DeskGame_intro_en.png`, `F3_DeskGame_desk_tr.png`. İngilizce girişte kişiselleştirilmiş `CounterLines` satırları oyun başlangıcındaki Türkçe metni koruyor; bu mevcut dil değiştirme açığı bu düzeltmenin kapsamına alınmadı.
- Canlı denetim aralığında 0 konsol hatası; oturumun daha önceki Unity AI Assistant bağlantı hatası oyun kodundan gelmiyor. Bu denetim baskın ve bütün oyun sonlarının yeniden doğrulaması değildir.
- Profil/kayıt baytları, 11 oyun tercihi ve `ProjectSettings.asset` yedekle karşılaştırıldı: `captures/F3_DeskGame_data_restore.json`. Editör temiz `DeskGame` ile Edit modunda bırakıldı. F4'e geçilmedi.

