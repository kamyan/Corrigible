# Aşama 7 · Harita, kâğıtlar ve CRT

2026-11-07 · Unity 6000.3.12f1 · 1920 × 1080. Kapsam: R6-TR/EN, R8a/b, R10a–c ve Aşama 6'dan devreden R17f.

## Uygulanan davranış

- Görev fişi ve kadro raporu sahaya atanmış üç gerçek kişiyi, mesleklerini, CAN/HAR/GÜÇ değerlerini ve üzerlerindeki eşyayı gösterir. Kişi adına tık panodaki aynı kartı seçer; görev atamaz ve simülasyon komutu üretmez.
- Kadro raporu 470 px genişliğinde; kit, zırh ve yardımcı yuvaları, sağlık durumu ve kalıcı ölüm uyarısı var. Görev fişinin altından 0,22 s'de kayar; vazgeçmek fişi açık bırakır.
- Ödüller fon, erzak ve dört hammadde olarak ayrılır. Operasyon raporu hammadde kazancını ilgili baskından önceki kayıt kopyasının stoklarıyla hesaplar; depo sınırında alınamayanı kazanılmış göstermez. Süre de ilgili baskının `raidres` komutundan gelir. Başarısız raporda kazanç bölümü yoktur.
- Operasyon sonucu her kişiyi adıyla yazar: döndü, yeni yara, kurtarıldı/yatış, kalıcı kayıp, deneyim, kırılan zırh. İlk vardiya V.001 / S.001'dir.
- Açık kişi dosyasında solda portre/kimlik, CAN/HAR/GÜÇ, yedi ustalık ve üç eşya yuvası; sağda bilinen karar, erişim, bulgu ve izler bulunur. Uzun geçmiş ek yapraklara ayrılır; punto küçülmez. Tek yaprakta oklar gizlidir. Kıskaçlı etiket sağ kenara taşınarak kadraj içinde tutulur. İNCELE/ERİŞİM/TAHLİYE mevcut komutlarına bağlı kalır.
- CRT'de sinyal ibresi ve kanal arama hareketi, dalışın son 0,6 saniyesinde 1,4× ışıma, dönüşte tüp/LED/ışık kapanması var. Telsiz ve defter mevcut durumun üzerine gelme satırını taşır.
- Kabul kâğıdına geçişte, tezgâhın 4 mm hover kalkışı okuma hedefinden çıkarılır. Kamera, üzerine gelme animasyonu bittiğinde kâğıttan uzaklaşmaz; yakın okuma boyunca tezgâh yeniden kalkmaz. Tam PlayMode turunun yakaladığı bu zamanlama sorunu ayrıca regresyon kontrolüne alındı.
- K5a, aynı dönüş yönüyle CRT'nin yerel sağında +0,14 m ve 52° olarak kaydedildi. K5b baskın dalış hedefi aynı kaldı. Açılışta masa lambası ampulden 0,15 s sonra yanar.

## Görüntüler

Görüntüler [screens_unity_rev3](screens_unity_rev3/) içindedir.

| Kabul | Kanıt |
|---|---|
| R6 görev fişi TR/EN | `A7_R6_fis_TR.png`, `A7_R6_fis_EN.png` |
| R6 kadro raporu TR/EN | `A7_R6_kadro_TR.png`, `A7_R6_kadro_EN.png` |
| R8a K5a karşılaştırması | `A7_R8a_K5a_once.png` (46°), `A7_R8a_K5a_oneri.png` (52°, +14 cm) |
| R8b gerçek baskına giriş | `A7_R8b_dalis_1.png`, `A7_R8b_dalis_2.png` |
| R10a emir belgesi | `A7_R10a_emir_TR.png`, `A7_R10a_emir_EN.png` |
| R10b başarılı rapor | `A7_R10b_operasyon_TR.png`, `A7_R10b_operasyon_EN.png` |
| R10b başarısız rapor | `A7_R10b_operasyon_basarisiz_TR.png` |
| R10c/R17f iki sayfalık kişi dosyası | `A7_R17f_dosya_TR.png`, `A7_R17f_dosya_EN.png` |
| Uzun dosyada ek yaprak | `A7_R10c_dosya_ek_yaprak_TR.png` |
| Aşama 6'dan kalan kilit ve değişim anı | `A7_R17d_kilitli.png`, `A7_R17e_degisim_ani.png` |

Başarısız rapor gerçek oynanan baskından; başarılı rapor ayrı, gerçek komutlarla ilerletilen fikstür simülasyonundan çizilir. R17f'de `AdmissionFixtures.Present("trace")`, gerçek kişinin dosyasının kopyasına Rauf adı/portresi/mesleği ve örnek izler koyar; kalan istatistikler gerçek kişinindir. Fikstür gizli kimlik açıklamaz, canlı simülasyonu değiştirmez. Kilitli damga görüntüsü kapasite dolu durumunun sunum kopyasıdır; kilidin gerçek Core kuralları Aşama 6 testlerinde kapsanır. Talimat değişimi K8A'ya varıldıktan sonra aynı animasyonla örneklenir; canlı akışın kamera zamanlaması değiştirilmez.

## Ölçüm ve kontroller

- Tam EditMode: **955/955**. Son görsel kontrolde eklenen dosya alt boşluğu düzeltmesinden sonra ilgili **10/10 kâğıt testi** yeniden geçti; metnin sayfa oklarından en az 4 mm uzakta kaldığı ayrıca denetlenir.
- PlayMode: 71 farklı senaryo kapsandı. Son tam turda 69/71 geçti; K8R hover sapması ve kare örneklemesine bağlı damga tepe denetimi düzeltildi. Ardından ilgili **26 kabul testinin 26'sı geçti**; kalan 45 senaryo aynı tam turda geçmişti. Bu 45'e yeni **4/4 R6/dosya/CRT testi** dahildir. K8R geri zinciri ayrıca tek başına 1/1 geçti.
- Damganın 2 mm genliği zaman kontrollü EditMode'da ölçülür; PlayMode'da gerçek karelerde görünür titreme ve genlik üst sınırı aranır. Kısa sinüs animasyonunun matematiksel tepesine gerçek karelerin denk gelmesi varsayılmaz.
- Derleme hatası yok; 2015 çeviri, eksik/yer tutucu sorunu yok.
- Tüm sahne punto taraması: **2431 metin, 0 ihlal**, 150 önceki gerekçeli istisna. Açık dosyanın canlı K4 ölçümünde en küçük yeni yazı **14,21 px**.
- Başlangıç sahne referanslarıyla karşılaştırma: **0 boşalan, 0 hedefi değişen, 0 silinen bileşen anahtarı**.
- K5a gölgeli ışık: **2** (≤ 4).
- Sayısal test özeti: [A7_test_sonuclari.json](A7_test_sonuclari.json).

Elle oynanan baskın döngüsü: kadro raporunda emir → dalış → Baskin eklemeli yüklendi → geri çekilme → operasyon raporu → masaya dönüş. Kaydedilen ölçüm: dalış ışıması 1,4; dönüş sırasında güç kapalı; dönüşte bir rapor.

Testler ve görüntüler ayrı `Corrigible.TestStorage` altında alındı. İlk gerçek kayıtlar SHA-256 ile karşılaştırıldı; testler beş dosyanın hiçbirini değiştirmedi. Çalışmanın kesildiği aralıkta kullanıcı test yönlendirmesi açıkken oynadı; onayıyla bu yeni ilerleme normal devam kaydına aktarıldı. Önceki normal kayıt ve yeni ilerleme `UserSettings/CodexRecovery/` altında ayrıca korunur. Son durumda test yönlendirmesi kaldırıldı ve Giris sahnesi Edit Mode'da açıldı. Dinamik font atlası yazımları bu çalışmanın başlangıç kopyalarından geri alındı.

## Referansa göre farklar

- Görev fişindeki üç gerçek kişi satırı ve altı ödül nedeniyle kâğıt yüksekliği yaklaşık 950 px; referans 887 px. Üst pay 128 px seçilerek TR/EN ana düğmeler 1080p kadrajda tutulur. Görev açıklaması kısa ve eylem odaklıdır.
- Kadro raporu 470 px ve yaklaşık 674 px; referans 470 × 673'e yakındır.
- Kişi dosyası sahnedeki 120 × 168 mm sayfaları kullanır; referansın tek sabit sağ sayfası yerine gerektiğinde ek yaprak vardır. Bu, izlerin taşmasını ve geçmişin kesilmesini önler.
- Mevcut yumuşatılmış kâğıt kenarları ve çizgileri korunur; R10 başlık çizgileri kırmızıya uyarlanır. Önceki aşamalardaki ışık/malzeme kalibrasyonu korunur; tam ışık ve performans turu Aşama 10 kapsamındadır.
- Rev2/Rev3 anahtarı önceki kabul edilmiş karara göre yoktur; önce/sonra görüntü kullanılır. `planningBoardEnabled=false` veri/sunum yolu korunur.
- Core, denge, RNG sırası ve kayıt biçimi bu aşamada değişmedi; Aşama 6'nın biçim 15 kararı geçerlidir.
