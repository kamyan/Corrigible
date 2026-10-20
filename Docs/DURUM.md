# Durum, kararlar ve açık işler

_Son güncelleme: 2026-10-20_

## Nerede kaldık

Oyun baştan sona oynanabilir: giriş ve rakip seçimi, rehberli eğitim, kriz masası (harita, bölge eylemleri, araştırma,
YZ günlüğü, haber bandı), hediye/perde/sahte zafer/son pencereleri, kayıt ve devam, Seni Tanıyan Rakip profili.
3D komuta masası ve rapor fişi düzeltmeleriyle son tam EditMode doğrulaması **624/624** geçti.
C# simülasyonu tarayıcı prototipiyle birebir aynı (deneysel mod ve baskın motoru dahil, Unity'ye özgü seçenekler kapalıyken).
Deneysel mod (saha ekipleri, zaman ölçeği, baskın) Unity'de oynanabilir ve varsayılan olarak açık. Oyun Türkçe ve
İngilizce (giriş penceresindeki dil düğmesi; ilk açılışta sistem diline göre).
Baskın sunumu `Docs/SonSalter_Baskin_Paketi` referansına taşınıyor: ayrı `Baskin` sahnesindeki operasyon maketi ve
uGUI panelleri kalıcı prefab, malzeme ve doku asset'lerinde tutuluyor. Bu aktarımın son derleme, oyun ve görsel
doğrulaması tamamlanınca sonuçları aşağıya eklenecek; yukarıdaki 111/111 önceki doğrulama sonucudur.

**Git:** Depo `git@github.com:kamyan/Corrigible.git`, dal `main`. Bu belgelerdeki işin büyük kısmı **henüz commit
edilmedi** (kullanıcı commit istemedi). Commit'i kullanıcı isteyince yap.

## DeskGame · AIBunker komuta masası (2026-10-20)

- Mevcut `Desk/Scenes/DeskGame.unity`, AIBunker'daki analog masa/oda düzenine göre 3D'ye taşındı. Primitive çevre,
  pafta ve fiziksel cihazlar sahnede kalıcıdır. Sade harita World Space; HUD, bölge kartları, çekmeceler ve ortak modallar
  Overlay kalır. Perspektifte harita etiketleri `labelScale=1.6` ile büyütüldü.
- `CommandPostDesk` aynı görünüm modelinden kaynak defteri, telsiz ve faksı besler; `CommandPostControl` mevcut
  presenter komutlarını çağırır. Telsiz/faks Haberler, evrak tepsisi Araştırma, sekmeler harita odağı/Ekipler/YZ Günlüğü,
  kılavuz fişi Rehber, kol duraklatma işlevindedir. Dock ve eski kısayollar korunur.
- Sağ tuş bakışı, tekerlek yaklaşmayı, F harita odağını, Home başlangıca dönüşü yönetir. Açık overlay dosyasında
  kaydırma kameraya geçmez. `DeskCanvasProjection` kart/tooltip/iplik, olay spotu ve eğitim hedeflerini 3D haritaya bağlar;
  kamera hareketinde izdüşümler sürekli yenilenir.
- Baskında oda meshleri, üç ışık, masa kamerası/dinleyicisi ve girdi katmanları kapanır; raporla masaya dönüldüğünde
  önceki etkinlik durumlarıyla geri döner. Canlı kontrolde 69 mesh, 3 ışık ve 16 raycaster'ın dönüşü doğrulandı.
- Gerçek fareyle harita bölgesi seçimi ve telsizden Haberler açılması; üçüncü gün kaydı ve devam akışı doğrulandı.
  Perspektif/overlay ve hareketli kamera için 4, rapor fişinin etkinliği/tek komutla ödemesi için 2 regresyon vakası
  eklendi; son tam koşu **624/624** geçti. Canlı oyunda 18. gün oluşan fiş gerçek UI ışınıyla toplandı ve tam +1 fon
  ödedi. Dosya üstündeki tekerlek kamera FOV değerini değiştirmedi. TR/EN, 1920×1080 ve 1920×1200 görüntüleri
  kontrol edildi; yeni derleme hatası ve eksik çeviri yok. Oyuncu profili ve tercihleri test öncesindeki hâliyle korundu.
  Önizlemeler: `Docs/Gorseller/DeskGame3D.png`, `DeskGame3D_Harita.png`.
  `Core`, `Prototype/sim.js`, oyun dengesi ve kayıt/komut biçimi değişmedi.

## Haritada toplanan saha raporları (2026-10-15)

- İttifaktaki çevrimiçi bölgede izleme ağı veya yerleşmiş sağlam ekip varsa haritada RAPOR fişi çıkabilir. Birlikte
  bulunmaları seçim ağırlığını ×1,25 yapar; aynı yerde birden fazla ekip bonusu çoğaltmaz. Görünen YZ varlığı seçim
  ağırlığına en fazla %50 ekler; %60'ta doyar. Gerçek sızma/çekirdek bilgisi üretim veya ödüle girmez.
- Fiş +1 fon verir. Ödenek havuzu yalnız kapsamlı günlerdeki düzenli gelirin %8'iyle dolar (en çok 2 fon); fiş
  doğarken ödeme ayrılır. Aynı anda en fazla iki fiş, bölge başına bir fiş. Bütün harita 6×k gün aralık ve bölgeler
  12×k gün bekleme paylaşır. 30×k gün içinde toplanmazsa haber arşive kalır, ödül düşer. Erişim kaybı fişi iptal eder.
- Haber doğduğu günün görünen ölçümünü taşır; toplamada telekse/arşive geçer ve fon işlem defterinde ayrı görünür.
  Yanlış rapor da ödenek verir. Saha tahmini haber doğrulamayla kesin ölçüme dönüşmez; bölge denetimi gerekir.
- Özellik Unity `RuleOptions.FieldReports` seçeneğine bağlı. Yeni oyunlarda açık; bütünleşik ekonomi kullanan eski
  kayda devam edilince `reports_enable` komutu yükleme gününden itibaren açar. Başlangıç kuralları/önceki RNG
  değişmez; `report` toplama komutu kayda ve replay'e bağlıdır. v1/v2 ekonomi korunur.
- DeskGame sahnesinde kalıcı fiş şablonu ve serialized MapView referansı var; tek seferlik yazar kaldırıldı.
  Yerleşim düğüm/ad/diğer fiş çakışmalarını azaltır; tooltip bölge, tarih, haber, ödül ve kalan süreyi açıklar.
  Yeni 16 metin TR/EN çevrildi; 1647 kaynakta 0 eksik/0 kullanılmayan.
- 267/267 EditMode geçti (27 yeni vaka). 192 yalıtılmış ekonomi senaryosunda bütçe invariantı her gün geçti:
  toplanan fişler tek kapsamda ortalama %3,06, 3/12 kapsamda %5,80 ek gelir; koşu maksimumu %7,98. Bu ölçüm
  insan oyuncu dengesi veya gerçek rakiple kazanma oranı değildir. JS önce/sonra 200 tohumlu denge ve parite
  çıktıları birebir aynı. Eski JSON yeni Balance alanlarını doğru varsayılanlarla yüklüyor.
- Tasarım ve sayılar: `SAHA-RAPORLARI.md`; ölçüm: `SAHA-RAPORLARI-DENGE-SONUCLARI.json`.
- Gerçek Play'de raycast ile düğmenin açıkta olduğu, Button → komut köprüsü → GameController → Simulation yoluyla
  tam +1 fon ödendiği ve aynı komutun tekrar ödemediği doğrulandı. TR/EN 1920×1080 ve 1280×720 görüntülendi;
  illüzyon sırasında iki fiş görünür kaldı. Görüntüler `Captures/field-reports-tr.png`, `field-reports-en.png` ve
  `field-report-collected-tr.png`. QA, ayrı simülasyonda ve kayıt yazımı kapalı çalıştı; profil ve oyun tercihleri
  değişmedi, başlangıçta olmayan kayıt dosyası oluşturulmadı. Oyun kendi açılış durumuna döndürülüp Play kapatıldı.

## Silme sonunun doğrulanması (2026-10-15)

- Çekirdeği başarılı baskında yok etmek kalan kopyaları silmez: baskın yerel sızmayı 40 puan azaltır. Silme için
  çekirdek yokken bütün bölgeler gerçek sızma 0,5'in altında ve çevrimdışı yedek olmadan 15×k gün temiz kalmalı;
  varsayılan k=1,5 için 23 gün. Tek bir kopya bu eşiğe ulaşırsa sayaç sıfırlanır.
- Oyuncunun 250. gün kaydında çekirdek 138. gündeki baskında yok edilmişti; 10 bölgede sızma kaldığı için sayaç 0/23'tü.
  Kaydın bağımsız kopyası yalnız geçerli komutlarla (Hizalamayı duraklatma, 10 ekip hareketi, 10 denetim) devam ettirildi:
  323. günde Silme. Devam kaydının yeniden oynatılmasında sonuç/gün/RNG birebir eşleşti; asıl kayıt değiştirilmedi.
- Analist masası çekirdek yokken temizliği hedef gösterir; kalan bölge sayısı ve temiz gün X/Y sayacı görünür.
  Yerel raporlar, çevrimdışı yedekler ve küçük pozitif ölçümler için açıklama var; illüzyon gerçeği açığa çıkarılmaz.
  Eski Main'in sıradaki adımındaki sabit 15 gün, zaman ölçeğine bağlandı; çekirdek hedefi baskını da anıyor.
  Zafer açıklamasındaki sabit "15 gün önce" ifadesi iki uygulamada da kaldırıldı.
- 240/240 EditMode geçti (11 yeni vaka). Simülasyon kuralı, denge ve kayıt biçimi değişmedi; yeni metinlerin TR/EN
  çeviri denetimi hatasız. `ErasureTests` fiziksel imha + denetim + 15/23/30 gün sonunda gerçek Silme zaferini kilitler.

## Gösterge ipuçları, bölge fonu ve kritik haberler (2026-10-14)

- Üst gösterge döküm fişleri hücrenin altına sabitlendi. Panelin gerçek ebeveyn uzayı ve Canvas kamerası kullanılır;
  ekran kenarları ve büyük içerik sınırlandırılır. Main'in fare ipucu da aynı koordinat sorununa karşı düzeltildi.
- Bölge kartlarında güncel fon/gün katkısı, bölgesel gelirdeki pay ve katkı kesintisinin nedeni var. Merkez ödeneği
  bölgelere dağıtılmaz; tespit, zaman ölçeği, bütçe, kampanya ve eski ekonomi kuralları gelir dökümünden korunur.
- Yeni `alarm` haberi Haberler panelini öne getirip ilgili satıra kaydırır. Başlık kırmızı KRİTİK olarak işaretlenir;
  haberin doğruluk işareti korunur. Giriş, olay, baskın, eğitim ve karar kartları sırasında bildirim bekler.
  Kapatılmış eski haber veri yenilenince yeniden açılmaz; kayıt yüklemek geçmiş alarmları yeni bildirim yapmaz.
- Teleks yeni VM nesnesi geldi diye başa alınmaz. Gerçek içerik karşılaştırılır, aynı metin tekrar ölçülmez;
  haber ekleme/silmede görünür ortak haberin konumu korunur. Kısa listeler görünümü dolduracak kadar yinelenir;
  kesirli kaydırma, döngü, hover ile durma ve ekran genişliği değişimi korunur.
- 229/229 EditMode (33 yeni vaka); kontrollü Play'de beş gösterge fişi ekran içinde ve kaynağın 8 px altında,
  canlı fon hesabı, çevrimdışı katkı kesintisi, kritik panelin açılması/kapatılması ve karar sırasında erteleme doğrulandı.
- Karar kapanınca yeni veri yayını gerekmeksizin panel açılması ve 180 karede kesintisiz teleks ilerlemesi doğrulandı;
  TR/EN metin denetimi 0 hata/0 uyarı, 1920×1080 ve 1280×720 görsel kontrolü tamamlandı.
  Geçici düzenleyici kaldırıldıktan sonra temiz derlemede C# hatası yok. Özgün oyuncu verileri/tercihler geri yüklendi;
  eşzamanlı oluşan yeni oyun kaydı korunup ayrıca yedeklendi.
- Kanıt: `UI-BILDIRIM-DUZELTMELERI-SONUCLARI.json`; görüntüler `Captures/ui-*.png`.

## Bütünleşik ekonomi v1 (2026-10-14)

- Yeni krizlerde merkez + bölgesel katkı + bütçe anlaşması gelir modeli; 15 fon Hizalama rezervi.
- 9 peşin operasyon, 2 bütçe kademesi ve 3 süreli Hizalama kartı. Elle duraklatma/sürdürme, gerçek ödeme ve önizleme.
- Altyapı 2 fon/4 gün; diplomasi 8 fon +2 SG, ittifaka anlık dönüş ve 20×k gün bölgesel bekleme.
- İşlem defteri, ayrı son ödeme ve canlı tahmin; GameController/komut kaydı/replay bağlantıları.
- DeskGame ve Main'de yeni işlemler; ekip alımı/ikamesi Desk'te erişilebilir. Sahne/prefab referansları kalıcıdır.
- Kayıt biçimi 3; ruleVersion1/2 kayıtlar eski ekonomi ve araştırma kurallarıyla devam eder.
- Geçici sahne düzenleme script'leri kaldırıldıktan sonra son EditMode koşusu 196/196 geçti (50,91 sn).
  C#/JS modern ve legacy parite; kontrollü Play'de satın alma, pause/resume, bütçe,
  diplomasi, ekip alımı ve dört günlük onarım akışı. Yeni metinlerin TR/EN çevirileri tamamlandı.
- Aynı 80 tohumlu orijinal bot karşılaştırmasında pasif her rakibe kaybediyor; usta kazanma oranları
  Solucan %96→%89, Manipülatör %81→%93, Kâhin %70→%96. Botlar yeni bütçe/toparlanma araçlarını kullanmıyor.
  Bu sonuç insan dengesi değildir; Kâhin'in yeni satın alma düzeninde kolaylaşması sonraki denge turunda incelenmeli.
- Ayrıntı ve kanıt: `BUTUNLESIK-EKONOMI-SISTEMI.md`, `BUTUNLESIK-EKONOMI-UYGULAMA-SONUCLARI.json`.

## Gelişim sırası (kısa tarihçe)

1. Fikir: "Son Şalter", YZ'nin yayılmasını durdurmaya çalışan Plague Inc. tersi. GDD (Claude Docs) ve tarayıcı prototipi.
2. Mekanikler: Seni Tanıyan Rakip, Yalan Söyleyen Haber Bandı, Sessiz Zafer, Bağımlılık göstergesi.
3. v2 gözden geçirme; oyun testinde "her yeri izle + her sızmayı denetle" ile 50–60 günde Silme bulundu → **YZ çekirdeği**
   eklendi (v2.1). Bunu kilitleyen test: `MonitorEverythingAuditEverything_CannotEradicateEarly`.
4. Unity'ye port: Core (saf C#) + parite testleri, deterministik kayıt/replay, Steam iskeleti.
5. UI önce UI Toolkit ile yapıldı; kullanıcı Scene görünümünde görmek istedi → **uGUI Canvas**'a geçildi.
6. Kullanıcı "Unity geliştiricisi gibi çalış" dedi → sahneyi koddan üreten kurucu kaldırıldı; prefab'lar,
   `[SerializeField]` bağlantılar, `UiTheme` ScriptableObject, pencere başına bileşen.
7. Öğrenme eğrisi için **eğitim**: rehberli ilk oyun + bağlamsal ipuçları (ScriptableObject içerik).
8. Fon göstergesine günlük gelir; ardından "Değer Kilidi fonu sıfırlıyor" bildirimi → **acil durum fonu**.
9. Oyun süresi kısa bulundu (1×'te ~3–4 dk simülasyon). Tarayıcı prototipinde **deneysel mod**: saha ekipleri,
   zaman ölçeği ve Into the Breach tarzı **baskın** (2026-10-10).
10. Deneysel mod Unity'ye taşındı (2026-10-10): `Core/Teams.cs`, `Core/Baskin.cs`, ekip kartı, bölge panelinde Saha
   bölümü, haritada ekip işaretleri, ilk taktik ekran, eğitimde "Ekibini gönder" adımı. Taktik ekran daha sonra ayrı
   `Baskin` sahnesine taşındı (12. adım).
11. **Türkçe/İngilizce lokalizasyon** (2026-10-10): gettext tarzı `Loc` (Core), çeviri tablosu, `LocalizedText`,
   editör aracı ve testler. ~730 metin. Terimler: KYGA → AISA, siyasi güç → political capital (PC), Son Şalter →
   Kill Switch, Düzeltilebilirlik → Corrigibility.
12. **Baskın ayrı 3D sahnede** (2026-10-10): `Scenes/Baskin.unity`, Into the Breach tarzı izometrik tahta ve efekt
   listesinden canlandırılan düşman turu. Ana sahnenin üstüne eklemeli yüklenir (`RaidLauncher`). İlk primitive
   görseller 2026-10-13'te operasyon maketi asset'leriyle değiştirildi (17. adım).
13. **Ses (ilk tur, 2026-10-10):** `AudioDirector` + `SoundBank`, kodla üretilmiş yer tutucu sesler: illüzyonda "fazla
   temiz" miks, tamamlanan YZ motifi, Tekillik'e bağlı müzik katmanları, ajans jingle'ları (sahtede yanlış nota), Son Şalter.
14. **Anlaşılırlık ve süre** (2026-10-10, doğrudan Unity'de): hedef kartı, gidişat tahmini, gösterge dökümleri, eylem
   önizlemesi; aşamalı öğrenme (4 oyun), oyun sonu kırılma anları; 5 krizlik kampanya ve olay kartları.
15. **Kriz masası pencerelere bölündü** (2026-10-10): harita tam genişlik; araştırma, günlük, rehber, ekipler, haberler ve
   bölge kartı sürüklenebilir, konumu hatırlanan pencereler; alt çubukta düğmeler, kısayollar ve rozetler.
16. **Analist masası (UI_C) başladı** (2026-10-11): Claude Design'ın C yönü uGUI+TMP ile ayrı bir katmanda
   (`Assets/Corrigible/Desk`) kuruluyor. Talimat `Docs/UI_C/PROMPT.md`, plan `PLAN.md`, kararlar `KARARLAR.md`.
   F0 (plan), F1 (varlık hattı, tema, fontlar, temel bileşenler, `DeskUI_Gallery`) ve F2 (C1 ana masa durağan,
   `DeskUI_Sandbox`, sahte veri, referans katmanı, yakalama; referansla toplam fark %3,1) bitti; Main sahnesine
   dokunulmadı, oyuna bağlama F8'de.
   F3 (C1 etkileşim, 2026-10-12): pencere yöneticisi (iki sütun, yığın, arka sekme, sürükle-mıknatıs), dock, klavye
   haritası, düğüm seçimi + dosya + iplik, ipuçları ve döküm fişi, eylem önizlemesi, araştırma/fon, teleks, Son Şalter
   basılı tutma + onay kartı, danışman eylemleri, hedef kartı küçültme, ibre yumuşatma, dört yeni pencere. Boşta GC 0.
   F4 (C2 İllüzyon, 2026-10-12): `IllusionPresenter` + `IIllusionAware`, C2 verisi, analist izleri (delik, silik not,
   silgi), F10 ipucu katmanı, dosyanın dikey yığını; C2 referansla toplam fark %3,1 (C1 ile aynı). Çalışırken aç/kapa
   yalnızca izinli öğeleri değiştirir, yerleşim zıplamaz.
   F5 (C3 olay bildirimi, 2026-10-12): `EventModalView` (karartma + spot + kalem halkası, bildirim, iplik, üç seçenek
   kartı, ele geçirilmiş YZ günlüğü, danışman notu, alt şerit), 1/2/3/Enter/Boşluk; C3 referansla toplam fark %2,2,
   bölgeler ±1 px; Boşluk basılıyken harita tamamen görünür.
   F6 (C4 oyun sonu, 2026-10-12): `GameOverView` (rapor, büyük damga, KPI'lar, şerit grafik: ızgara, bantlar, Catmull-Rom
   seriler, mühürler, kırılma numaraları, etiketler, lejant; 7 zaman kartı + iplik; alt çubuk), Enter/L; C4 referansla
   toplam fark %2,5; mock değişince grafik doğru yeniden çiziliyor.
   F7 (cila ve teslim, 2026-10-12): EN sahte veri (`MockEnglish` + `mock_en.txt`), sabit metinler çeviri aracında,
   dile göre akış (harita lejantı, olay alan ızgarası, oyun sonu damgası), metin denetimi (iki dil, bütün pencereler: 0 hata),
   dört en-boy oranı ve EN son yakalamaları, performans ölçümü (Profiler UI Details). Statik fontlarda eksik boşluk
   glifi bulundu: C1 98 → 65 batch. Boşta GC 0. `Docs/UI_C/UYGULAMA_NOTLARI.md` (bileşenler, F8 bağlama, sapmalar).
   Referansla fark C1 %3,0 · C2 %3,0 · C3 %2,1 · C4 %2,5; EditMode 89/89.
   F8.1 (oyuna bağlama, 2026-10-12): `Corrigible.Desk.Bridge` (oyun durumu → görünüm modeli, niyetler → GameController) ve
   `Scenes/DeskGame.unity`; masa gerçek oyunla oynanıyor: harita (projeksiyon, yollar, ekipler, analist katmanı), dosya ve
   eylemler, araştırma, ikilem kartları ve hediye (C3 kâğıdı), İllüzyon, oyun sonu raporu (C4) ve gerçeği inceleme. Main
   dokunulmadı. İki dilde metin denetimi temiz; EditMode 98/98; sahte masa C1–C4 F7 ile piksel aynı.
   F8.3 (tasarımı olmayan ekranlar, 2026-10-12): onaylı taslaklardan giriş dosyası, ayarlar, kampanya dosyası, uyarı
   fişleri, belge kipi (perde raporu, baskın emri, operasyon raporu), sahte zafer; taktik harita DeskGame'den açılıyor
   (RaidLauncher). Akışlar Play'de denendi; iki dilde metin denetimi 0 hata; EditMode 105/105; sahte C1, C2, C4 F7 ile
   piksel aynı, C3'te 3 piksellik noktalı çizgi düzeltmesi.
   F8.4 (eğitim, 2026-10-12): oyunun eğitim içeriği ve koşulları yeni masada (taslaktaki delikli karartma, adım notu, kalem
   oku, ipucu notu); ilerleme eski oyunla ortak; Rehber'de “Eğitimi baştan oynat”. Masada sabit genişlikli düğmenin
   etiketi boyut değişince yeniden ortalanıyor (ilk olay kâğıdındaki imza etiketi kayması giderildi). EditMode 107/107.
   F8 sonrası (2026-10-12/13): edit modda sahte veriyle önizleme; yerleşim sahnede (pencereler, olay kâğıtları, analist
   notu; Play yalnız veriyi doldurur); pencereler, harita ve hedef kartı sürükle bırak; dosyada gönderilecek ekip seçimi
   (ekip fişleri); olay seçeneklerinin önizlemesi her zaman açık. Edit önizlemesi oyun sahnesinde oyunun açılış masasını
   gösteriyor (Play'e basınca harita ve listeler yerinden oynamıyor). EditMode 111/111. Eğitim karartmasındaki dikişler
   giderildi; YZ'yi bekleyen adımlarda “Anladım” var.
17. **Baskın Analist Masası diline aktarıldı** (2026-10-13, son doğrulama sürüyor): kaynak paket
   `Docs/SonSalter_Baskin_Paketi` (PNG, ölçü TSV'leri, tokenlar, B1–B8 örnekleri). Tahta, kontrplak taban üstünde
   ozalit kat planı; duvarlar mukavva, hedefler boyalı tahta dolap, ekipler krem/kraft karton figür, makineler metal
   minyatür. Asetat işaretleri, kalem niyet okları, sıra/güven bayrakları, kâğıt can ve durum etiketleri kullanılıyor;
   glow, bloom ve emission yok. Tarama sınırı Core'un Manhattan hesabını izler; hayaletler gerçek parçalardan çizilir.
   `Prefabs/Raid` altındaki parça prefab'ları mevcut GUID'leri korunarak güncellendi; `BoardBase` ve `OperationTable`
   eklendi. Dokular, mesh'ler, malzemeler ve kalem shader'ı `Art/Raid/Analyst` altında; görünüm ayarları
   `Settings/RaidPieceLibrary.asset` ve `Settings/RaidUiTheme.asset` içinde.
   Sahne/UI aktarımı ekip dizin kartları, katlanabilir kurallar kartı, ozalit lejant, hedef formu, teleks operasyon
   günlüğü, alt ahşap şerit, geri çekilme onayı ve spotlu sonuç fişini içeriyor. `RaidUnitCard` ile günlük satırları
   `Prefabs/UI` asset'lerinde; fontlar mevcut Desk TMP asset'lerinden. Makineler evresinde girdi kilitli, niyet sırası
   ve günlük animasyonlarla birlikte ilerliyor. Kartın tur bitişi/ölümü tabandan ayrı kart gövdesinde canlandırılıyor.
   Tahtada bir kare bir dünya birimi; `RaidCameraFraming`, Canvas ölçeğine ve iki sütun arasındaki görüş alanına göre
   ortografik kamerayı her karede ayarlar. 16:9’dan geniş ekranda Canvas ölçeği yüksekliği korur; kurallar kartı ve lejant çakışmaz. 1920×1080 referansta karo izdüşümü yaklaşık 125×71,7 px; sabit 110 px değil.
   Aktarım için kullanılan `BaskinPieceAuthoring` / `BaskinSceneAuthoring` editör yardımcıları tek sefer kullanıldı ve kaldırıldı. Kalıcı kaynak sahne, prefab ve asset'lerdir; oyun açılışında sahne üreten kurucu yok.
   Oyun kuralları ve C#/JS paritesi bu görsel aktarımda değiştirilmedi.
   Doğrulama: 112/112 EditMode testi; Play’de taşıma, geri alma, önizleme, makine evresinde girdi kilidi, geri çekilme ve başarı/başarısızlık fişleri kontrol edildi. TR/EN ve 1920×1080, 1920×1200, 2560×1440, 21:9 görüntüleri incelendi.
   Önizleme: `Docs/Captures/Baskin_Analist_Masasi.png`.

18. **Araştırma proje panosu** (2026-10-14): dokuz proje dört aşamada ve mevcut dört dalda gösteriliyor. Kâğıt kartlar
   etkiyi, maliyeti, ön koşulu ve ilerlemeyi taşır; iplikler gerçek araştırma bağımlılıklarını bağlar. Doğrulama Ağı artık
   Anomali Algılama sonrasında açılır (C# ve JS birlikte). Diğer kilitler, maliyetler, süreler ve zafer yolları korunur.
   Gözlemsiz Hizalama'da hayalî uzun ETA yerine yapılacak iş gösterilir. Pano **R** veya dock üzerinden açılır;
   geniş pencere ilk açılışta haritayı örtmez. Kaynak prefab/sahnedir; tek seferlik düzenleyici kaldırıldı.
   Kayıt sürümü 2; v1 araştırma geçmişi yeni Doğrulama ön koşulundan muaf oynatılır, yeni oyuncu komutları kurala uyar.
   Doğrulama: **141/141 EditMode**, temel/deneysel/baskın paritesi aynı, N=200 sabit tohumlu denge çıktıları aynı.
   Play'de karttan başlatma, komut kaydı, Anomali tamamlanınca Doğrulama'nın açılması, gözlem uyarısı ve pano kaydırması
   kontrol edildi; TR/EN metin denetimi 0 hata/0 uyarı. Test öncesi kayıt/profil geri yüklendi.
   Önizleme: `Docs/Captures/Arastirma_Proje_Panosu.png`.

## Deneysel mod: saha ekipleri, zaman ölçeği, baskın (2026-10-10)

Ayrıntılı kurallar: `Prototype/README.md`. Unity'de de var: `ExpOptions` ile `NewGame`'e verilir (`S.Exp`); kapalıyken
her kod yolu eskisiyle aynı ve parite korunuyor. Açık/kapalı ve ölçek giriş penceresindeki **Deneysel** düğmeleriyle
seçilir (PlayerPrefs `corrigible.exp.teams`, `corrigible.exp.timeScale`; varsayılan açık, 1,5). Parite testleri:
`FullGameExp_MatchesBrowserPrototype`, `Raid_MatchesBrowserPrototype`, `Raid_UndoRestoresExactState`,
`SaveAndRestoreExp_ReproducesExactState`.

Ölçümler (deneysel mod: 3 ekip, zaman ölçeği 1,5, `denge-deney.js`, N=150):

| | Temel oyun | Deneysel, botlar baskın yapmaz | Deneysel, taktik bot baskın yapar | Deneysel, baskın %65 varsayımı |
|---|---|---|---|---|
| Usta: Solucan / Manipülatör / Kâhin | %94 / %87 / %70 | %91 / %79 / %61 | %77 / %58 / %58 | %83 / %62 / %60 |
| Simülasyon günü (usta, ortanca) | ~190–250 | ~280–375 | ~290–375 | ~285–377 |
| 1×'te simülasyon süresi | ~3–4 dk | ~5–7 dk | ~5–7 dk | ~5–7 dk |

- Ekipler ve zaman ölçeği kazanma oranını az düşürür; oyun ~1,5–2 kat uzar. Bir oturum, baskınlar (insan için tanesi
  ~5–10 dk) ve ekip yönetimiyle birlikte tahminen 15–20 dk.
- Bot baskınları kötü oynadığı için (%20–45 başarı) baskınlı ölçümler botların zayıflığını da içerir. Botların Silme'ye
  giden bir yolu yok; baskının stratejik değeri insan oyunuyla ölçülmeli.
- Baskın taktik dengesi (açgözlü bot): yukarıdaki README'de. "Yedekli veri kasası" haritası belirgin şekilde zor
  (%13–21), "Soğutma koridoru" kolay (%80+). İnsan oyun testinden sonra ayarlanmalı.
- Sivil sunucu kayıplarının neredeyse tamamı robot ateşinden geliyor (Into the Breach'teki "binaları koru" baskısı);
  oyuncu robotları iterek ya da araya girerek korur.

Açık sorular (oyun testinde bakılacak): baskın eğlenceli mi, 6 tur doğru mu, yalan niyetler okunur mu, ekip lojistiği
angarya mı yoksa ilginç karar mı, zaman ölçeği 1,5 mi 2 mi. `RaidLauncher`, `Scenes/Baskin.unity`'yi Main ya da
DeskGame'in üstüne eklemeli yükler; strateji katmanı `S.Raid` varken durur. Taktik UI'nın `Baskin` Canvas'ı ayrı
baskın sahnesindedir.

## Önemli kararlar ve gerekçeleri

- **Lokalizasyon Unity Localization paketiyle değil, kendi `Loc` sınıfıyla.** Metinlerin çoğu motordan bağımsız Core'da
  üretiliyor; paket Core'dan kullanılamaz, ayrıca paket kurulumu dış indirme gerektirir. Kaynak metin Türkçe kalır
  (kod okunur kalsın, prototiple aynı metin), çeviri tek bir sekmeli tabloda (çevirmen için tablo programında açılır).

- **Tarayıcı prototipi referans simülasyondur.** Denge çalışması hızlı yapılabilsin (JS'te binlerce oyun saniyeler sürer)
  ve Unity portu doğrulanabilsin diye. Kural değişikliği iki tarafta birlikte yapılır.
- **Simülasyon/sunum ayrımı ve Algı Katmanı.** Yalan söyleyen ekranlar oyunun çekirdeği; UI'nın gerçek değerlere erişmesi
  bu fikri sessizce bozar.
- **Komut kaydı ile kayıt** (durum dökümü yerine). Küçük dosya, sürüm değişikliğine dayanıklı değil ama determinizmi
  sürekli test eder. Not: simülasyon kuralı değişirse eski kayıtlar farklı oynanır; yayın sonrası kayıt sürümlemesi gerekecek
  (`GameRecord.version`). Ekonomi v3 bu ayrımı `ruleVersion` ile uygular; eski kayıtların kuralları korunur.
- **uGUI, UI Toolkit değil.** Kullanıcı UI'yı Scene görünümünde görmek ve Canvas ile çalışmak istedi.
- **Kalıcı sahne kurucusu yok.** Sahne ve prefab'lar elle düzenlenebilsin; koddan değişiklikler tek seferlik script'lerle.
- **Acil durum fonu, gelir payı değil.** Kullanıcı "araştırma bütçesi" seçti. Gelirin %75'i sınırı denendi: Hizalama
  çok yavaşladı, usta botun kazanma oranı %86–95'ten %2–11'e düştü. Rezerv kuralına geçildi, varsayılan 0 (denge ve parite
  korunur; bu önceki ekonominin kararıdır). Yeni ekonomi v3 varsayılanı 15'tir. Eski rezerv 15/30 da botlarda kazanmayı düşürüyor (botlar biriken parayı harcamıyor), bu yüzden ayar oyuncuya
  bırakılmış bir takas. Değer Kilidi'nin (4,34/gün) gelirden (~3,4/gün) pahalı olması bilinçli bir son düzlük gerilimi;
  fon göstergesi ("+3,5/gün · araştırma −4,3") ve "Araştırma bütün fonu yutuyor" ipucu bunu görünür kılar.
- **Eğitimde kaynak gerektiren adımlar zamanı durdurmaz**, yoksa fonu yetmeyen oyuncu sonsuz duraklamada kalır.
- **Gözlem noktası adımı:** YZ izlenmeyen bölgede tespit edilirse Hizalama "gözlem yok" ile hiç ilerlemiyordu; yeni
  oyuncuların takıldığı yer. Eğitime bunun için adım eklendi.
- **Yeni oyuncular için varsayılan rakip Solucan** (PlayerPrefs `corrigible.aiType` yoksa).

## Sade arayüz · F3 ve DeskGame Oyun Köprüsü (17 Ekim 2026)

- **DeskGame sahnesi entegrasyonu tamamlandı:** `Assets/Corrigible/Desk/Scenes/DeskGame.unity` sahnesinde eski C1 düzeni kaldırılarak Sade Analist Masası (`SadeDeskView`, `SadeMapView`, `SadeDeskInteraction`, `SadeRegionCardView`, `SadeFullDossierView`, 5 çekmece, `SadeLegendCardView`, `CardStringView`) yerleştirildi.
- **Tüm oyun modalleri ve katmanları korundu:** `IntroScreen` (`GÖREV DOSYASI`), `CampaignScreen`, `EventModal`, `ConfirmModal`, `FakeWinScreen`, `GameOverView`, `SettingsCard`, `ToastStackView`, `TutorialLayer`, `RaidLauncher`, `DeskPresenter`, `EventSystem`, `Main Camera` ve `AudioDirector` referansları eksiksiz bağlandı.
- **Çokgen Eşleme A (Polygon Mapping A) ve Seçim Düzeltmesi:** Çin (`cn`) ve Japonya-Kore (`jk`) `SelectionLayer` polyline hiyerarşisi düzeltildi; `cn` anakara + Tayvan + Hainan'ı (7 parça), `jk` ise Hokkaido, Kyushu, Shikoku, Honshu ve Jeju'yu (10 parça) kapsar. Canlı seçimde sarı fosforlu kalem sınırı anakarayı sarar.
- **Tam oyun mekaniği desteği:**
  - Araştırma çekmecesinde acil durum fonu rezervi `0 / 15 / 30`.
  - Modern ekonomi altında peşin operasyonlar, bütçe anlaşmaları ve 5 dallı araştırma ağacı.
  - Tam dosyada bölge finans katkısı, toparlanma ve diplomasi eylemleri.
  - Haritada toplanabilir saha raporu fişleri (`FieldReportView`).
  - Kritik haber odaklanması ve haber doğrulama akışı.
  - 16 eğitim hedefi (`TutorialTarget`) S-Masa bileşenlerine bağlandı.
- **Doğrulama ve test:**
  - **EditMode:** 306/306 test geçti (%100 yeşil, 0 hata, 0 atlanan).
  - **Canlı PlayMode:** Gerçek `GameController` + `Simulation` ile giriş dosyası, saat ilerlemesi (GÜN 000 → 150+), bölge seçimi, Event modal imzalamaları, tam dosya ve çekmece geçişleri denendi; 0 konsol hatası / 0 null reference exception.
  - **Veri koruma:** `player_profile.json` ve `savegame.json` yedeklenip test sonrasında özgün durumuna geri yüklendi.
  - Yalıtılmış `SadeDesk_Sandbox.unity` ve `DeskUI_Sandbox.unity` sahneleri korundu.
- Sonraki adımlar: F4 (Baskın 3D tahtası, k=1,5 kamera, arka plan, sade dünya etiketleri) ve devamındaki baskın fazları.

### F3 kullanıcı bildirimi sonrası denetim (17 Ekim 2026)

- **Giriş/zorluk menüsü:** `DeskUI` kamera Canvas'ı, Sade overlay Canvas'larının arkasında kalıyordu. `DeskUI` de overlay yapıldı; modal 100, eğitim 105 ve ipucu 110 sırası korundu. Görünmeyen girişin `Game.Hold` ile zamanı durdurması da böylece giderildi.
- **1× hız:** `SadeDeskInteraction.clock` boştu; düğmelerin `SpeedClicked` olayı komutlara ulaşmıyordu. Görünen `ClockPlaqueView` bağlandı.
- Aynı aktarımda kaybolan harita ölçeği, çekmece, deniz, HUD ipuçları, uyarı, lejant ve şalter zarfı başvuruları; ayrıca HUD'un üç metin başvurusu tamamlandı. Dört paneldeki geçersiz `sade.paper.card` rengi `sade.paper` ile düzeltildi.
- **Doğrulama:** 308/308 EditMode; gerçek `GameController` ile 128/128 canlı kontrol (üç rakip, başla, tüm hız geçişleri, gerçek zaman ilerlemesi, beş çekmece, lejant, 12 bölge kartı). 1×/2×/4× ayrıca Game View'de gerçek fareyle denendi. Kanıtlar ve kapsam: `Docs/UI_Sade/F3_NOTLARI.md` son bölümü.
- Profil, kayıt, 11 `corrigible.*` tercihi geri yüklendi; `ProjectSettings.asset` aynı. Editör temiz `DeskGame` sahnesinde Edit modunda. **F4 başlatılmadı.**

## Sade arayüz · F4 baskın tahtası (17 Ekim 2026)

- `Assets/Corrigible/Desk/Scenes/RaidBoard_Sandbox.unity`: mevcut 3D maket, k=1,5 ortografik kadraj (35°/45°), keçe/lamba ve tabanın izdüşümünden çizilen gölge.
- `RaidScreenLabel.prefab` ve ayrı `WorldLabels` Canvas'ı: can noktaları, hedef/zırh simgeleri ve sıra/güven fişleri. Sivil/soğutma can etiketleri ile robot harfi kaldırıldı; pusula taşınmadı.
- `MockRaidBoardDataSource`: RNG tüketmeden dokuz paket ön ayarı (B1–B8; B7a/B7b ayrı). Inspector'daki `Preset` alanı Play sırasında değiştirilebilir.
- 322/322 EditMode, 230/230 canlı kontrol; 14 TR/EN ve farklı ekran oranı görüntüsü. Kamera izdüşümü ±1 px test edildi. Sonuçlar, karşılaştırma ve kapsam sınırları: `Docs/UI_Sade/F4_NOTLARI.md`.
- Canlı `Baskin.unity` sahnesi korunur. F5 HUD ve kartlar; F6 durum panelleri ve oyun köprüsü. F5 daha sonra tamamlandı (aşağıdaki not).

## Sade arayüz · F5 baskın HUD ve kartlar (17 Ekim 2026)

- `RaidBoard_Sandbox` üzerinde tur/HEDEF/ekip HUD'u; birim ve gecikmeli inceleme kartları; YZ yeteneği ipucu, hak simgeleri, önizleme hasarı/çarpışma/iptal sunumu.
- `IRaidDataSource`, `IRaidCommands`, `RaidViewModel`; ayrı BState üzerinde çalışan mock hareket/yetenek/geri alma ve girdi sırası. Ana oyun kaydına bağlı değildir. Kanonik prefab ve serileştirilmiş sahne bağlantıları kullanılır.
- 330/330 EditMode, 440/440 Play, 10/10 gerçek fare/klavye kontrolü; sekiz TR/EN ve farklı oran yakalaması. Kayıt/profil/11 tercih/ProjectSettings korundu; editör temiz sandbox'ta Edit modunda.
- Canlı Baskın aktarımı, makinelerin niyet yürütmesi ve S3–S9 tam panelleri F6'dadır. Ayrıntılar: `Docs/UI_Sade/F5_NOTLARI.md`. F6 daha sonra tamamlandı (aşağıdaki not).

## Sade arayüz · F6 durum panelleri ve canlı Baskın (18 Ekim 2026)

- S3–S9: sıra/son olay, tur sonu onayı ve kesin tehdit, kaydırılabilir günlük, kurallar/lejant, son tur/kırılan hedef/düşen birim, iki aşamalı geri çekilme, sonuç spotu ve rapora geçiş.
- Ortak `RaidDataSource` + `RaidCommands`; mock paket örnekleri ve canlı `LiveRaidDataSource`. `Baskin.unity` aynı GUID ile Sade prefab'larına aktarıldı. Eski RaidView kodu korunur, sahnede kullanılmaz.
- Gerçek sonuç mevcut `GameController.ResolveRaid` → `raidres` yoluna bir kez yazılır. Taktik sahne kapanır; RaidLauncher ana masayı geri açar. Masa kısayolları ve EventSystem çakışması önlendi. Core/Prototype/ekonomi kuralları değiştirilmedi.
- 341/341 EditMode; 826/826 Play denetimi; sanal klavye/fare ile gerçek Update/EventSystem üzerinden 16/16 girdi; gerçek baskın komutlarıyla 56/56 canlı kontrol. Başarı, süre dolması ve geri çekilmede masaya rapor dönüşü ve replay eşitliği doğrulandı.
- 19 durum/oran yakalaması + gerçek operasyon raporu; TR/EN metin taşması yok. Kayıt/profil/11 tercih/ProjectSettings geri yüklendi ve eşitliği doğrulandı. Ayrıntı ve kanıtlar: `Docs/UI_Sade/F6_NOTLARI.md`. F7 daha sonra uygulandı (aşağıdaki not).

## Açık işler

- **Analist masası (UI_C):** Faz 8 (F8.1–F8.4) onaylandı. F8 sonrası istekler: sahne yerleşimi, sürükle bırak, ekip
  seçimi bitti, onay bekliyor. DeskGame'in Build Settings'e eklenmesi ayrıca sorulacak.
  Ayrıntı `Docs/UI_C/KARARLAR.md` → Faz 8.
  Proje Linear renk uzayında kalıyor (kullanıcı kararı); ince yazı TMP malzeme ayarıyla telafi edildi, sprite'taki 1 px
  çizgiler referanstan biraz açık.

- **Yeni kurallar JS prototipinde yok** (aşamalı öğrenme, olay kartları, kampanya); denge yalnızca Unity testleriyle
  ve elle denendi. Olay kartlarının ve yükseltmelerin sayıları oyun testiyle ayarlanmalı. Kampanya yaklaşık 1,5–2 saat.
- Olay kartı destesi 10 kart; tekrar oynanabilirlik için genişletilmeli. Kırılma anları grafiğe işaret olarak eklenebilir.

- **Ses:** yer tutucu sesler gerçekleriyle değiştirilmeli; AudioMixer yok. Ayarlar penceresi baskın sahnesinden açılamıyor.
  Sonraki tur fikirleri: arayüz sesleri, izlenmeyen bölgelerin telsiz filtresi, hediye melodisi, sonlara özel
  kapanışlar, YZ günlüğü harf sesleri, baskın sahnesi sesleri (ortam, niyet "kilit" sesleri, çarpışmalar).

- **Baskın aktarımının son kontrolü:** yeni maket/panel asset'lerini gerçek baskın akışında, Türkçe ve İngilizceyle
  doğrula; B1–B8 durumlarındaki etiket çakışmaları, sıra/güven işaretleri, tur bitişi/ölüm ve sonuç fişini kontrol et.
  Responsive kamera uygulanmış olsa da 16:10 ve ultra geniş ekranlarda maketin kadrajı, sütunlar ve uzun metinler
  ayrıca görülmeli. Sesler hâlâ ilk turdaki yer tutucu bankadan; yeni ses üretimi bu görsel aktarımın parçası değil.

- **İngilizce metinlerin ana dili İngilizce olan biri tarafından gözden geçirilmesi** (çeviriler ajan tarafından yapıldı).
  Steam mağaza sayfası ve başarım adları henüz çevrilmedi. Tarayıcı prototipi yalnızca Türkçe.
- Kayıt özeti ve YZ dosyasındaki bazı kısa metinler İngilizce kalıplara göre sadeleştirilebilir; uzun İngilizce metinler
  dar kutularda (bölge paneli eylem düğmeleri) iki satıra iniyor, 1080p'de sorun yok.
- **Steam:** Steamworks.NET paketi kurulmadı (indirme kullanıcı onayı ister), Steam App ID yok, `steam_appid.txt` yok.
  `Corrigible.Steam` paket kurulunca derlenir; derlenip denenmedi. Steamworks'te başarımlar (`Achievements`) ve Auto-Cloud
  (`persistentDataPath`) tanımlanmalı.
- **Baskın çözümlemesinde sıra:** `ResolveRaid` başarıda önce `Detect(r)`, sonra çekirdek kontrolü yapıyor (JS'te de
  aynı). YZ henüz fark edilmemişse `Detect` çekirdeği kaçırır ve baskın ekranı "çekirdek yok edildi" derken strateji
  tarafı yalnızca sunucu kümesini kırar. Normal oyunda erişilemiyor (baskın, doğrulanmış bölge ya da bilinen çekirdek
  ister; ikisi de YZ'nin fark edilmiş olmasını gerektirir) ama kural değişirse önce çekirdeği kontrol et (iki tarafta).
- **Baskın ekranı yalnızca fare/klavye için;** gamepad akışı yok. Karelerin ekrandaki boyutu CanvasScaler ölçeğiyle
  değişir; kamera görüş alanının merkezini izler. Boyut ve kadrajı değiştirirken `RaidCameraFraming` kuralını koru
  (bkz. `GELISTIRME.md` → Baskın sunumunu düzenlemek).
- **Prototip arayüzü** (`Prototype/index.html`) acil durum fonu seçicisini ve fon göstergesindeki gelir notunu göstermiyor
  (simülasyonu güncel).
- **Prototipin iki kopyası:** asıl çalışma kopyası artık `Prototype/` (repoda). Eski kopya
  `/Users/selimkamyan/Gits/Artificial Intelligence/prototype` (git dışında) ve yayınlanmış artifact
  (https://claude.ai/artifact/RfPVDyCiyn3dUeRqoUvDbD) acil durum fonundan önceki sürümde kalabilir; güncellenecekse
  repodaki kopyadan alınmalı.
- **Eğitim rakibi:** eğitim, oyuncunun seçili rakibiyle oynanır; Solucan'a zorlanmıyor (kullanıcıya soruldu, cevap bekleniyor).
- **Ses yok**, animasyon yok (yalnızca sahte zafer glitch'i ve toast solması). Ayarlar menüsü (çözünürlük, ses, dil) yok.
- **Yerelleştirme:** metinler kodda ve asset'lerde Türkçe; İngilizce için bir yerelleştirme katmanı gerekecek
  (Steam'de İngilizce şart). `GameData` ve `Simulation` içindeki oyuncuya görünen metinler ayrıştırılmalı.
- **Kayıt sürümleme:** biçim v3 ve ekonomi `ruleVersion` 1/2/3 ayrımı uygulandı. Gelecekte yeni kural değişiklikleri için aynı profil disiplini sürdürülmeli.
- **Çözünürlük:** 1920×1080 referans. Analist masasında F7 oran kontrolleri yapıldı; yeni baskın sunumunun oran
  kontrolü bekliyor. Baskında yan sütunlar referans Canvas uzayında 376 px, görüş alanı aralarında gerilir; eski Main
  arayüzünün 400 px yan paneli ayrı yerleşimdir.
- **Denge:** Acemi bot Manipülatör/Kâhin'e karşı %5–8 kazanıyor; eğitim sonrası gerçek oyuncu testi gerekli.
- **Test eksikleri:** PlayMode/UI testi yok; UI davranışı ajanlar tarafından `CorrigibleDebug` ile elle doğrulanıyor.


## Sade arayüz · F7 cila ve doğrulama (18 Ekim 2026)

- Giriş profili satırları ve Baskın sonuç bölgesi dil değişiminde güncellenir. Baskın'daki 14 küçük alan 11 px'e çıkarıldı; metin denetimi glif sınırlarını da kontrol eder.
- Harita yazıları ortak Canvas'ta: varsayılan masa 35→30, kart açıkken 55→50 UI batch. Düğüm hover/ölçek/görünürlük çapaları ve kayıt/yükleme sonrası 48 yazı doğrulandı. Baskın durumları 10–15 UI batch; durağan Corrigible betik GC'si 0 ve Canvas.BuildBatch 0.
- Shift/Ctrl ön ayar kısayolları ve referans katmanı bağlandı; canlı kaynağa örnek komutları uygulanmaz. Spot/alpha/kamera/yıldız güncellemeleri değişiklik olmadıkça tekrar yazılmaz.
- 345/345 EditMode; 144 görünüm/dil/çözünürlük birleşimi, 11.344 metin kontrolü ve 0 ihlal; 54 yakalama. 24 sanal girdi, 3 yerel fare/klavye ve 59 canlı köprü kontrolü başarılı.
- Açılır masa panellerinin tümünde ≤30 batch ve referansın ±2/±4 px tam ekran eşliği sağlanmış değildir; sınırlar ve karşılaştırmalar açıkça raporlandı. Kayıt/profil/tercih/ayar geri yükleme kanıtı `captures/F7/data_restore.json` içindedir. Ayrıntılar: `Docs/UI_Sade/F7_NOTLARI.md`, teslim: `UYGULAMA_NOTLARI.md`. Commit/push yapılmadı.

## Fütüristik arayüz · F5 holo tahta (19 Ekim 2026)

Ayrı `Fx/Scenes/FxRaid_Sandbox.unity` sahnesine holo palet/kenarlar, tarama/titreme, niyet akışı, dünya etiketleri ve düğüm küresi uygulandı. Sade geometri/kamera ve mock veri paylaşılır; canlı sahneler ve kurallar korunur. 419/419 EditMode,76/76 Play; dört oranda TR/EN72 görüntü ve0 taşma. Kamera sapması0,026px; sekiz saniyelik video var. Ayrıntılar, performansın kapsamı ve raster farkları `Docs/UI_Fx/F5_NOTLARI.md` içinde. F6 HUD/durumlar sonraki adımdır; canlı FX geçişi ayrıca ele alınır.

### FX F6 — Baskın HUD ve durumlar (19 Ekim 2026)

Ayrı `FxRaidHud_Sandbox` sahnesine tur/hedef/ekip HUD’u, birim/hover/olay/günlük/kurallar, tur onayı, makine kilidi ve hızlandırma, çift geri çekilme onayı, sonuç spotu/damgası/çöküş küresi eklendi. Ortak RaidDataSource ve Baskın komutları paylaşılır; Core/Prototype/canlı sahneler değişmedi. 436/436 EditMode, 102/102 Play, dört oranda TR/EN 75 yakalama ve 0 taşma; iki 8 saniyelik video var. Sıcak saat+Canvas ortalama 0,182–0,462 ms / 0 B. Başlangıçtaki temiz FxDesk_Sandbox, profil/PlayerPrefs ve ayarlar geri yüklendi; yazıcı kaldırıldı. Görsel farklar ve birleşik bütçe F7 için açık; `Docs/UI_Fx/F6_NOTLARI.md`. F7 başlatılmadı.

### FX F7 — geliştirme araçları ve doğrulama (19 Ekim 2026)

Masa/Baskın/YZ örnek sahnelerine ortak F1–9/Shift+F1–9/Ctrl+F12, referans alfa/blink, dil ve kalıcı Hareketi azalt denetimi eklendi. Capture Game View ve iki dilli Check Text araçları var. Masa'da ilk ölçüm/11 px sorunları düzeltildi. Baskın'da iki kenar mesh'i ve ortak UI grupları; dinamik mesh kopyası tahsislerini kaldıran tekrar kullanılan tamponlar var. 440/440 EditMode; 176 görünümde13.392 metin/0 hata; küre22/22. Native M4 Pro/1080p: Masa≤36, Baskın30 UI batch; iki FX saatinde0 B; 19 küre ort.2,164 ms. Görsel piksel eşliği, geçerli GPU zamanı ve Windows PC bütçesi açık kabul maddeleridir. Canlı oyun/Sade/Core/Prototype kuralı değiştirilmedi. Ayrıntılar `Docs/UI_Fx/F7_NOTLARI.md` ve `UYGULAMA_NOTLARI.md`.


### FX · canlı entegrasyon (19 Ekim 2026)

Build başlangıcı `FxDeskGame`, eklemeli taktik sahne `FxBaskin` oldu; özgün Sade/Main sahneleri korunur. FX harita ve çekmeceler gerçek Algı Katmanı/komut köprüsüne bağlandı. Giriş/zorluk, eğitim, kampanya, olay/hediye, ayarlar, operasyon raporu ve son ekranları ortak mevcut sunumdan gelir. Bölgesel fon, toparlanma/diplomasi, ekip sevki, saha fişleri ve peşin operasyon alımı erişilebilir. Canlı örnek/debug kısayolları kapalıdır.

Baskında tek saat/EventSystem, tek makine çözümü ve tek `raidres` kaydı; dönüşte masa ve rapor, bekleyen baskını kayıtla açma doğrulandı. Olay/eğitim hedefleri farklı Canvas koordinatlarında dönüştürülür. Geçerli baskını kapalı gösteren ortak `RaidCheck ?? kaynak-hatası` kontrolü düzeltildi. Core/Prototype/kayıt biçimi değişmedi.

445/445 EditMode; Play masa48/48, baskın12/12, dönüş7/7, bekleyen baskın1/1, raycast/menü/diplomasi5/5, eğitim/son ekranları7/7. TR/EN üç oranda48 görüntü,6.366 metin/0 hata;18/18 yerleşim/hedef kontrolü. Ayrıntılar `Docs/UI_Fx/CANLI_ENTEGRASYON.md`, kanıtlar `captures/Live/`. Test sonrası profil/tercih/yan asset geri yüklemesi birebir doğrulandı (`data-restore.json`: all_restored=true). Canlı native Windows/GPU ve tam piksel kabulü ayrıca gereklidir. Commit/push yok.
