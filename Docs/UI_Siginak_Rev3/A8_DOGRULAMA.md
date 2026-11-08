# Aşama 8 · Menü sahanlığı, puantaj ve ayarlar dolabı

2026-11-07 · Unity 6000.3.12f1 · 1920 × 1080. Kapsam: R1a–f, R2, R3a–f ve R4.

## Uygulama

- Giris sahanlığı genişletildi: alt bölüm karo, üst bölüm beton; ıslak zemin, birikinti, drenaj, eksik karolar, kablo kanalları ve bitiş çıtaları. Paket modelleri: merdiven, iki sahanlık lambası, başlık, kapı kasası/kanadı, dış çark, gözetleme halkası, interkom, zil ve ana şalter. Kapı, çark, zil ve interkom menüde dekor; giriş karttan veya saatten başlar.
- Puantaj saati model pivotlarına bağlıdır. Kart dört ayrı satır taşır: rol, sığınak, sıradaki vardiya, özgür bölge sayısı. 80 delik biten vardiyaları gösterir; 80'den sonra sayı doğru kalır, delik eklenmez. Kayıtsız menüde raf boştur. Yeni harekât kayıt varken iki tıklıdır; kıskaç kalkar, eski kart devrilir, iptal fişi görünür. Dört saniye dolunca seçim kendiliğinden bırakılır; kaydı silmez.
- Kart tıklanınca kamerayla saate yaklaşılır; kart kalkar, yay çizerek boğaza gider, zımba iner, yeni delik açılır ve amber lamba yanar. Yeni kart V.001 / S.001 ile başlar. Saat yüzüne tıklamak da kayıttan devam eder.
- Ardından zil, M0'a dönüş, dış çark, kapı kanadı ve eşikten yürüyüş gelir. DeskGame yüklemesi kanat açılırken başlar; etkinleştirme, son 0,3 saniyelik kararma ve en az 0,2 saniye tam siyahtan sonradır. Giriş boyunca Esc dahil masa girdileri kilitli, imleç gizlidir. Yeni harekât emir belgesine; devam mevcut kısa açılışa gider.
- Oda dolabının prefab kökü ve dış referansları korundu. Menü ayrı `SettingsCabinetMenu.prefab` kullanır; kanat −120°, menüye dönüş kolu yerine kör kapak. Oda kanadı −95° ve dönüş kolu mevcut kilit/onay akışına bağlıdır. Dört sürgü, ibre, üç haneli sayaç, kanal lambaları, SES ve DİL modelleri mevcut ayar servisini kullanır. Sessizde ibre ve lambalar düşer; sürgü ve sayılar değerlerini tutar. İbre yayı uzun karelerde küçük adımlarla entegre edilir.
- Ana şalter iki tıklıdır; kapağı 115°, kol ±55°; sonrasında mevcut güç kesme/çıkış akışı çalışır. Girişte güç ve zil, çok malzemeli modellerin yalnız ilgili ışıklı yüzlerini değiştirir. Kapı açılırken saat lambasının gölgesi kapanır (en fazla üç gölgeli ışık).

## Varlık ve referans kararları

Kaynak 198 model prefab'ı değiştirilmedi. Sahnede ve iki dolap bileşik prefab'ında model örnekleri bağlandı. OBJ'nin metre izdüşümlü UV'leri kadran, kart ızgarası, gösterge, ölçek ve rakam şeridi için uygun değildi. `Art/Rev3/SceneMeshes/Menu` altında yalnız bu yüzlerin UV'leri düzeltilmiş ağ kopyaları kullanılır; geometrileri aynı kalır. Yeni baskılar `Rev3TextureBaker.BakeStage8()` ile tekrar üretilebilir. Menü karosu ortak malzeme anahtarıyla çakışmaması için `tx_menuTile150` adını taşır.

Sahne/prefab geçişini yapan tek seferlik kurucular ve QA oturum koruyucusu kaldırıldı. Aşama 0 kararına göre menü sahanlığıyla iç koridor ayrı kalır; ortak sahanlık prefab'ına birleştirilmez. Core, denge, RNG ve kayıt biçimi değişmedi (15).

## Referansa göre kalibrasyon

- M0: önerilen konum korunur; hedef y=1,25 m, FOV 60°. Başlık, merdiven ve alt eşik aynı kadraja alınır.
- M1: konum x=3,32 m, FOV 48°. K9: dolap yerel çapası (0; 0,35; −1,10), FOV 48°. Açık dolabın üst/alt kenarları kırpılmaz.
- Plakalar 7,2 mm büyük harf; iki dilde sabit DİL · LANGUAGE plakası 112 mm, MENÜYE DÖN plakası 120 mm genişletildi. İptal fişi 101,5 × 39 mm, yazı kutusu 96 × 36 mm; İngilizce iki satır da sığar. Dil konum etiketleri ve kilit şeridi de okunurluk eşiğine göre büyütüldü. Otomatik punto yok.
- Puantajda devrilen kart için gölgesiz soğuk okuma dolgusu eklendi; güç kesilince diğer lambalarla söner. Ampulün paneldeki parlak lekesini azaltmak için gölgesiz iç spot 0,15, ek yerel dolgu 0,35 kullanılır. Soğuk/sıcak ışık ilişkisi korunur; URP/three.js sayısal ışık şiddetleri doğrudan eşit değildir.
- R4 süreleri kart hareketinin başından sayılır. M0'dan seçilirse önce 0,35 saniye saat yaklaşımı eklenir. Mevcut ses bankası kullanılır; yeni zil/karton/jeneratör kayıtları üretilmedi. Seslerin son kalite turu Aşama 10'da yapılır.

## Doğrulama

- Tam EditMode: **964/964**, atlanan veya başarısız yok (son sahne ve kodla).
- Tam PlayMode: **79/79**, atlanan veya başarısız yok. Sonradan yalnız iptal fişinin TR/EN kutusu büyütüldü; bu yerleşim için iki yeni EditMode taşma testi eklendi.
- Canlı M1/M2 taraması TR/EN: **en küçük gerekli metin 14,30 px**. Süs parafı 10,62 px gerekçeli istisna; iptal fişi 14,64 px. Otomatik punto ve taşma yok. Ham ölçümler `A8_punto_menu.json` içinde.
- Gerçek yeni harekât girişinde damga 0,695 s; çark 1,369 s; kanat/yükleme yaklaşık 2,05 s; tam açık 2,656 s; siyah 4,260 s; DeskGame 5,235 s. Ekran kaydı ve yükleme bu ölçüme dahildir. Tam siyah 0,975 s, gölgeli ışık üst sınırı 3, `GameLaunch.Pending` bir kez tüketilip `None` olmuştur.
- Genel sahne punto taraması: **2419 metin, 0 ihlal**, 150 önceki gerekçeli istisna. Üç sahnede eksik script yok; **2011 çeviri, 0 eksik**.
- Gerçek oyuncu kaydı, eski kayıt, profil ve iki masa JSON'u SHA-256 ile aynı; tercihler aynı. Dinamik font atlasları bu aşamanın başlangıç yedeklerinden geri alındı. Test yönlendirmesi boş; Giris Edit Mode açık.
- İngilizce/Türkçe etiketler ve menü kolunun açık emir belgesinde gerçek `Disabled` durumu görüntülendi. Sayısal kanıt `A8_test_sonuclari.json` içinde.

## Görüntüler

Tümü `screens_unity_rev3/` içinde 1920 × 1080 Play görüntüleridir.

| Kabul | Kanıt |
|---|---|
| R1a/b kayıt yok / var; EN | `A8_R1a_M0_kayit_yok.png`, `A8_R1b_M0_kayit_var.png`, `A8_R1_M0_EN.png` |
| R1c / R2 onay | `A8_R1c_yeni_onay.png`, `A8_R2_yeni_onay_M2.png` |
| R1d–f | `A8_R1d_kart_saatte.png`, `A8_R1e_kapi_acik.png`, `A8_R1f_son_salter_kapak.png` |
| R2 kart TR/EN | `A8_R2_M2_kart_TR.png`, `A8_R2_M2_kart_EN.png` |
| R3 menü dolabı TR/EN, sessiz | `A8_R3_M1_acik_TR.png`, `A8_R3_M1_acik_EN.png`, `A8_R3d_sessiz.png` |
| R3 oda dolabı TR/EN, sürgü, gerçek kilit | `A8_R3_K9_TR.png`, `A8_R3_K9_EN.png`, `A8_R3e_surgulu.png`, `A8_R3f_kilitli_menu.png` |
| R4 siyah ve yeni harekât emri | `A8_R4_siyah.png`, `A8_R4_DeskGame_emir.png` |

R1b/R2'deki V.014, özgür 3/12 örneği salt sunum fikstürüdür; mevcut oyuncu kaydını değiştirmez. Giriş görüntüleri destenin iki tıklanmasıyla başlatılan gerçek yeni harekâttandır. R3f açık emir belgesi nedeniyle kilitlidir. Son okunurluk düzeltmelerinde oda menü plakası görüntüden sonra 96'dan 120 mm'ye, iptal fişi 70 × 30'dan 101,5 × 39 mm'ye çıkarılmıştır; harf ölçekleri korunur.

