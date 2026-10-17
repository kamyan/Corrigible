# Sade arayüz · PLAN (F0)

Kaynak: `Docs/UI_Sade/SonSalter_Sade_Paketi/PROMPT.md` (sürüm 1.1). Bu belgede “paket” o klasördür.

**Durum (2026-10-16):** plan ve F1 onaylandı. Sorulara verilen yanıtlar `KARARLAR.md` › F0'da: harita A, yerleşim önerileri uygun, eski düzen silinecek, sıra paketteki gibi. F2 durağan masa `SadeDesk_Sandbox` sahnesinde tamamlandı; kapsam ve doğrulama `F2_NOTLARI.md` içinde. Sonraki faz F3: seçim, kartlar ve çekmeceler. F7 performans hedefi açık: mevcut 37 UI batch, hedef ≤30.

**Güncelleme (2026-10-17):** F3 seçim, kartlar, beş çekmece, ipuçları, klavye ve İllüzyon sunumu sandbox'a uygulandı ve tüm doğrulamaları tamamlandı: 304/304 EditMode, 47/47 Play denetimi, S-Masa2–9 (1832 metin) + 12 bölge kart/dosya (6648 metin) TR/EN 0 hata, boşta 0 B betik GC / 0 canvas rebuild (varsayılan masa 35 batch, bölge kartı açık 55 batch; ≤30 batch F7 hedefi). Ayrıntı: `F3_NOTLARI.md`. Sıradaki faz F4 (baskın tahtası, k=1,5) veya kullanıcı onayına göre `DeskGame` aktarımı.

**F4 (17 Ekim 2026):** `RaidBoard_Sandbox` tamamlandı: k=1,5 kadraj, keçe/lamba/izdüşüm gölgesi, ekran Canvas’ında sade etiketler ve dokuz B1–B8 ön ayarı. 322/322 EditMode, 230/230 canlı kontrol; 14 görüntü. Ayrıntı: `F4_NOTLARI.md`. Sıradaki faz F5 (baskın HUD’u ve kartları); canlı `Baskin` aktarımı F6’da.

**F5 (17 Ekim 2026):** Baskın HUD'u, ekip/birim/inceleme kartları, mock komutlar ve S1↔S2 etkileşimleri tamamlandı. 330/330 EditMode, 439/439 otomatik Play ve 10/10 gerçek girdi kontrolü; sekiz görüntü. Ayrıntı: `F5_NOTLARI.md`. Sıradaki faz F6: S3–S9 durum panelleri ve canlı Baskın köprüsü.

**F6 (18 Ekim 2026):** S3–S9 panelleri ve canlı `Baskin.unity` aktarımı tamamlandı. Makine turu/Boşluk, kullanılmamış hak onayı, günlük, kurallar, son tur, geri çekilme ve sonuç fişi çalışıyor. Gerçek başarı/süre dolması/geri çekilme akışı rapora dönüyor; tek sonuç kaydı ve replay eşitliği doğrulandı. Ayrıntı: `F6_NOTLARI.md`. F7 daha sonra uygulandı (aşağıdaki not).

**F7 (18 Ekim 2026):** dil düzeltmeleri, minimum yazılar, ortak harita yazı Canvas'ı, geliştirme kısayolları ve son denetimler uygulandı. 345/345 EditMode; 144 görünüm/dil/oran birleşiminde 11.344 metin, 0 ihlal; 24 sanal girdi, 3 yerel girdi ve 59 canlı köprü kontrolü geçti. Varsayılan masa 35→30 UI batch, kart açıkken 55→50; Baskın 10–15. Açılır masa panellerinde ≤30 ve referansla tam raster eşitliği sağlanmış değildir. Ayrıntılar: `F7_NOTLARI.md`, `UYGULAMA_NOTLARI.md`.

## 1. Paket özeti

- **İki ekran, 18 referans kare.** S-Masa1–9 C1'in yerini alır; S-Baskın1–9, B paketinin taktik haritasındaki panellerin yerini alır. C3 (olay) ve C4 (oyun sonu) aynı kalır, C2 İllüzyon S-Masa'ya uyarlanır.
- **Masa: harita tam ekran.**
  - Harita gerçek ülke sınırlarından 12 bölge çokgeni (Miller izdüşümü, `map_regions.json`); dolgular mesh, katmanlar 3840×2160 doku.
  - Köşelerde: 6 kâğıt etiket (FON, SİYASİ GÜÇ, HEDEF · KÜRESEL GÜVEN, BAĞIMLILIK, TEKİLLİK), üst ortada zaman plakası, solda 5 sekme, sağda LEJANT ve küçük Son Şalter zarfı.
  - Ayrıntılar açılır: bölge kartı ve iplik, 5 çekmece, döküm fişi, düğüm ipucu, lejant kartı, tam bölge dosyası.
- **Baskın: aynı 3D maket, kamera 1,5 kat yakın.**
  - Köşelerde: tur plakası, HEDEF kartı (+ YZ yeteneği çipi), GÜNLÜK / KURALLAR / GERİ ÇEKİL, ekip kartları, Geri al / TURU BİTİR.
  - Açılır öğeler: birim kartı, üstüne gelme kartı, son olay fişi, turu bitir onayı, günlük, kurallar, geri çekilme, sonuç.
- **Kurallar.**
  - C §2 geçerli.
  - “Pencereler haritayı örtmez” kuralı kalktı: açılır paneller örtebilir, seçili bölgeyi veya birimi örtemez.
  - Kalıcı arayüz yalnız köşelerde.
  - Harita üstündeki yazı ve düğümler harita ölçeğiyle büyümez.
  - 3D'de bloom, glow ve emissive yok.
- **Varlıklar.** 174 yeni sprite (@2x, PPU 200, `sprites.json`'da kenar, pivot ve renklendirme bilgisi), döşeme dokuları, lamba dokuları, harita taban ve çizgi katmanları, iki ozalit dokusu.
- **Veri.** `spec/mock/*.json`: 18 karenin sahte görünüm modeli; önerilen şema.
  - Masa: `SadeDeskViewModel`.
  - Baskın: `IRaidDataSource` / `IRaidCommands` / `RaidViewModel` (tahta dahil).
- **Sadakat ölçütleri.**
  - UI: ±2 px.
  - 3D: kadraj ve parça yerleri ±4 px; renkte “aynı aile”.
  - Doğrulama: `tools/compare.py` (kenar bindirmesi, fark, yanıp sönme).
- **Bağımlılıklar.** C F1 bileşenleri (projede var). B 3D maketi (projede, Baskin sahnesinde uygulanmış).
- **Fazlar.** F0 plan → F1 ortak temel → F2–F3 masa → F4–F6 baskın → F7 cila. Her faz sonunda rapor ve onay.

## 2. Projenin durumu

**C paketi**
- F0–F8 tamam. F1 altyapısı hazır:
  - `DeskTheme`, temel bileşenler (`PaperPanel`, `StampView`, `TrustMarkView`, `CarbonText`, `DeskButton`, `KeyChipView`…);
  - `DeskUI` atlası, sprite içe aktarıcı (PPU 200, mipmap yok);
  - TMP fontları: Courier Prime 400/700, Libre Franklin 400–800, Kalam 400/700.
- C1 kurulu ve oyuna bağlı:
  - `Desk/Scenes/DeskGame.unity` (oyun) ve `DeskUI_Sandbox.unity` (sahte veri);
  - köprü `Corrigible.Desk.Bridge`: `SimDeskBuilder` görünüm modelini kurar, `SimDeskCommands` niyetleri `GameController`'a taşır.

**C1'e sonradan eklenenler (14–15 Ekim).** Sade çizimlerinde karşılığı olmayanlar bu listeden çıkıyor (§4):
- **Araştırma proje panosu:** 9 proje, 4 aşama, ön koşul iplikleri. Yeni krizde peşin operasyonlar, bütçe kartları ve Hizalama kartları da aynı panoda; acil durum fonu.
- **Bütünleşik ekonomi:** bölgede toparlanma ve diplomasi, bölgenin fona katkısı, ekip alımı, işlem defteri.
- **Saha raporu fişleri:** haritada çıkan, toplanınca +1 fon veren fişler.
- **Diğer:**
  - kritik haber (Haberler panelini öne getirir);
  - gösterge döküm fişleri;
  - Silme sayacı (temiz gün X/Y);
  - dosyada gönderilecek ekip seçimi (fişler).
- EditMode: 267 test.

**B paketi.** `Scenes/Baskin.unity`'de uygulanmış.
- Görünüm:
  - kontrplak taban, ozalit;
  - mukavva duvarlar, karton figürler, metal minyatürler;
  - asetat işaretleri, kalem okları;
  - kâğıt etiketler (`RaidWorldLabel`).
- **Kamera:** `RaidCameraFraming` iki panel sütunu arasına sığdırır.
- **HUD:** `Corrigible.Game` assembly'sinde, `RaidView` (679 satır) + `RaidUnitCard` + `RaidLogLineView`.
- **Tahta:** `RaidBoard3D.Build/Sync(Baskin.BState, Marks)` ile çizilir; önizleme, hayalet ve açılır yazı API'leri var.

**Teknik durum**
- **Motor ve render:**
  - Unity 6000.3.12f1; URP 17.3.0; Linear renk uzayı.
  - `PC_RPAsset`: ana ışık gölgesi açık (2048, 4 kademe, yumuşak gölge yüksek); ek ışıklar piksel başına ≤ 4; gölge mesafesi 50; HDR açık; MSAA kapalı; SRP Batcher açık.
- **Play Mode:** Enter Play Mode Options açık; alan ve sahne yeniden yüklenir.
- **UI ve girdi:** uGUI 2.0 + TMP; Input System 1.19 (yalnız yeni sistem).
- **Paketler:** Newtonsoft 3.2.2 (köprüde `JObject`).
- **Lokalizasyon:**
  - Core'da `Loc` (`Resources/Localization/strings.txt`, Türkçe kaynak + EN) ve masada `DeskLoc`.
  - Sade dizeleri bu tabloya eklenecek.
- **Kod düzeni:**
  - DI yok; async yok (Update ve coroutine).
  - Tween: projenin kendi `DeskTween`'i.
  - Komutlar: `IDeskCommands` + `Run("Ekran:Eylem:arg")`.
- **Editör bağlantısı:**
  - Unity Pipeline (`com.unity.pipeline` 0.8, `unity command eval`) bağlı; editör canlı sürülebiliyor. Ayrıca bir “MCP for Unity” paketi yok.
  - Prefab ve sahneler geçici editör betikleri ya da eval ile kurulur; YAML elle yazılmaz.

**Veri modelleri**
- `Core.WorldState`:
  - bölgeler: güven, bağımlılık, sızma, izleme, karantina, çevrimdışı, ittifak dışı, çekirdek;
  - ekipler, araştırma, ekonomi, haberler, YZ günlüğü.
- `Simulation.Shown`: görünen değer + güven türü.
- `Baskin.BState`: tahta, birimler, niyetler, uyanma, günlük.

## 3. Projeye uyarlama

Paketin önerdiği yollar (`Assets/_Project/...`, `SonSalter.*`) yerine projenin düzeni kullanılır.

| Paketin önerisi | Bu projede |
|---|---|
| `Assets/_Project/UI/Desk/…/Sade` | `Assets/Corrigible/Desk/Scripts/Runtime/Sade/{Hud,Map,Cards,Drawers}`, `Prefabs/Sade/`, `Art/Sade/` |
| `SonSalter.UI.Desk` | `Corrigible.Desk` (genişletilir) + `.Editor` + `.Tests` |
| `SonSalter.UI.Raid` (HUD, kartlar, dünya etiketleri) | `Corrigible.Desk` içinde `Runtime/Raid/` (tema ve bileşenler orada). VM ve arayüzler (`IRaidDataSource`, `IRaidCommands`) de burada |
| `SonSalter.Raid.Board` (3D) | Mevcut `Corrigible.Game` (`Game/Raid/`) kalır: `RaidBoard3D`, parçalar, `RaidPieceLibrary`. Kamera k = 1,5 ve arka plan buraya eklenir |
| köprü | `Corrigible.Desk.Bridge`: `SimRaidDataSource` (BState → `RaidViewModel`), `SimRaidCommands`, tahta bağdaştırıcısı (VM → `RaidBoard3D`) |
| Sahneler | `Desk/Scenes/SadeDesk_Sandbox.unity`, `SadeUI_Gallery.unity`, `RaidBoard_Sandbox.unity` |
| Tema | `SadeTheme` (`tokens_sade.json`'dan) ve `RaidTheme` (mevcut `RaidUiTheme` + `RaidPieceLibrary` ile birleştirilir); menü `Tools/Sade UI/Import Tokens` |
| Sprite ve atlas | `Art/Sade/Sprites`; mevcut içe aktarıcı `sprites.json`'ı da okuyacak şekilde genişler. Ayrı `SadeUI` atlası (DeskUI bozulmasın) |
| Belgeler | `Docs/UI_Sade/`: PLAN, KARARLAR, UYGULAMA_NOTLARI, captures. Paket kökü `SonSalter_Sade_Paketi/`, araç yolları buna göre |

**Sahte tahta.** Mock JSON'un tahta kısmı köprüde `BState` + `Marks`'a çevrilir. Böylece tahta tek yoldan çizilir; görünüm kural hesaplamaz.

## 4. Görünüm modeli ↔ oyun eşlemesi (S-Masa)

| Sade öğesi | Oyundaki kaynak | Durum |
|---|---|---|
| Zaman plakası (gün, saat, hız) | `S.Day`, `DayProgress`, `Speed` | var |
| FON, SİYASİ GÜÇ: değer, değişim, güven | mevcut gösterge görünüm modeli | var |
| HEDEF: Hizalama %, Silme % | `GoalVM` | var. Silme sayacı ipucunda |
| KÜRESEL GÜVEN, BAĞIMLILIK, TEKİLLİK + eşik çubuğu | göstergeler, eşikler (kayıp 15, veto 70, tekillik 100) | var |
| Bölge düğümü: sızma, güven, karantina, çekirdek | `Shown(r)`, `Q`, çekirdek bilgisi | var |
| Bölge: çevrimdışı, ittifak dışı | `Off`, `Left` | **çizimde yok.** Öneri: C'deki işaretlerin sade boyu |
| Yayılım iplikleri | köprünün analist iplikleri (bağlantı + sızma) | var |
| Ekipler, rota, ETA | `S.Teams` | var |
| Uyarı notu | danışman mesajı + bölge | var |
| Bölge kartı: durum, sızma aralığı, istatistikler, 4 eylem, önizleme | `DossierVM` | var |
| Bölge kartı: ekip satırı, “Baskın %64” | `FieldVM` | var. Gönderilecek ekip fişleri **çizimde yok** |
| ARAŞTIRMA çekmecesi: 4 dal × 3 düğüm + hız seçici + şerit | `ResearchVM` | **içerik farklı** (aşağıda) |
| HABERLER çekmecesi | haberler, sahte işaretleri, kritik haber | var. “Denetle” düğmesinin **oyun karşılığı yok** (doğrulama bölge denetimiyle olur) |
| EKİPLER çekmecesi | ekipler, baskın uygunluğu, ekip alımı | var. Kartta 3 üye = baskın ekibi (T, E, A) |
| YZ GÜNLÜĞÜ | ele geçirilen kayıtlar | var |
| REHBER | rehber içeriği | var |
| FON döküm fişi (gelir, gider, 30 gün) | ekonomi işlem defteri | var |
| Tam dosya | `DossierVM` | var. Bölge fonu, toparlanma ve diplomasi **çizimde yok** |
| Saha raporu fişleri (haritada) | `FieldReportVM` | **çizimde yok** |

**Çizimde yeri olmayan oyun özellikleri için öneri.** Hepsi `KARARLAR.md`'ye yazılır.

- **Araştırma çekmecesi:**
  - Dal ağacı oyunun gerçek panosunu gösterir: dalda 3'ten çok proje varsa sağda “+n” fişi (paketin açık kararı 8).
  - Peşin operasyonlar, bütçe ve Hizalama kartları ağacın altında ayrı bir bölüm.
  - Hız seçicinin yerinde oyundaki acil durum fonu seçenekleri (oyunda araştırma hızı seçimi yok).
- **Bölge kartı ve tam dosya:**
  - Kartın ekip satırında gönderilecek ekip fişleri.
  - Tam dosyanın Saha bölümünde bölge fonu katkısı, toparlanma ve diplomasi.
- **Saha raporu fişleri:** haritada uyarı notunun dilinde küçük bir kâğıt fiş (düğümün yanında); tıklanınca toplanır, ipucunda ödül ve kalan süre.
- **Haber “Denetle”:** haberin bölgesinde Denetim eylemini önizler; bölge kartını açar.

## 5. Harita bölgeleri uyuşmuyor

Tasarımın 12 bölgesi oyunun 12 bölgesiyle aynı değil:

| Tasarım | Oyun |
|---|---|
| Kuzey Amerika, **Orta Amerika**, Güney Amerika | `na`, `sa` |
| Batı Avrupa, Doğu Avrupa, Avrasya | `weu`, `eeu`, `ru` |
| **O. Doğu + K. Afrika**, **Sahra Altı Afrika** | `me`, `af` |
| Güney Asya, **Doğu Asya**, Güneydoğu Asya, Okyanusya | `in`, **`cn`, `jk`**, `sea`, `oc` |

Çokgen verisi bölge düzeyinde birleştirilmiş, ülke sınırı ayrımı yok:
- Doğu Asya'da Çin + Moğolistan + Kore tek çokgen. Japonya adaları, Tayvan ve Hainan ayrı çokgenler.
- Orta Doğu ile Kuzey Afrika tek çokgen.

**Seçenekler:**
- **A · Çokgen eşlemesi (önerilen).** Tasarım verisi aynen kalır, yalnız bir eşleme tablosu eklenir.
  - Eşleme:
    - `na` = KA + OA
    - `sa` = GA
    - `weu` = BA
    - `eeu` = DA
    - `ru` = AV
    - `me` = O. Doğu + K. Afrika
    - `af` = Sahra Altı
    - `in` = Güney Asya
    - `cn` = Doğu Asya ana kara + Tayvan + Hainan
    - `jk` = Japonya adaları (+ Jeju)
    - `sea` = GDA
    - `oc` = OK
  - Düğümler oyunun bölge konumlarından izdüşümle; adlar oyunun adları.
  - Eksik:
    - Kore Çin'in renginde görünür, Kuzey Afrika Orta Doğu'nun renginde.
    - `map_lines` dokusundaki KA–OA sınırı `na`'nın içinde bir çizgi olarak kalır.
- **B · Oyunun bölgelerine göre çokgenleri yeniden üret.**
  - Natural Earth verisinden yeniden üretilir (indirme gerekir) ve `map_lines` yeniden çizilir.
  - Birebir doğru olur, ama paketin “bölgeleri yeniden çizme” kuralına aykırı ve daha uzun.
- **C · Oyunun bölgelerini tasarımınkine çevir.**
  - Kural, denge ve kayıt değişir. Paketin kapsamı dışında.

## 6. Baskın mimarisi

**Görünüm modeli.** `IRaidDataSource` / `IRaidCommands` / `RaidViewModel`:
- Oyunda köprü `BState`'ten kurar: önizleme, ulaşılabilirlik ve niyet sırası Core'un hesaplarıdır.
- Sandbox'ta mock JSON'dan.

**Tahta.** Mevcut B tahtası korunur.
- **Kamera:**
  - `RaidCameraFraming` yerine sabit k = 1,5: `orthographicSize = (Screen.height / canvasScale) / 265,165`.
  - Izgara ortası ekranda (960, 528).
- **Arka plan:** keçe, `tx_lamp_raid` ve tahta gölgesi; Screen Space – Camera canvas.
- **Dünya etiketleri:**
  - Robot harfi, sivil ve soğutma etiketleri ve pusula kalkar.
  - Hedeflerde “3/4” sayısı yerine noktalar.

**HUD.**
- Sade HUD yeni prefab'larla `Corrigible.Desk`'te kurulur.
- Mevcut B HUD (`RaidView`) Baskin sahnesinden kaldırılır. Kodu, Sade HUD oyunda doğrulanınca silinir.

**Yeni davranışlar** (Core'a dokunmadan, köprüde hesaplanır):
- turu bitir onayı: hakkı duran birim + onu vuracak niyet;
- son olay fişi: sıra kutuları, iptal edilen niyet;
- makineler evresinde hızlandırma;
- YZ yeteneği çipleri.

## 7. Sahne stratejisi

1. Önce yeni sandbox'larda sahte veriyle kur ve referansla karşılaştır:
   - `SadeDesk_Sandbox`, `RaidBoard_Sandbox`.
   - F1'deki galeri sahnesi (`SadeUI_Gallery`) bileşenleri sahnede gösterir.
2. Sonra oyun sahnelerine geç:
   - `DeskGame`: C1 düzeni yerine S-Masa.
   - `Baskin`: B HUD'u yerine S-Baskın.

   Bu iki adım mevcut sahnelere dokunur; o fazda ayrıca onay istenir.
3. C1 kodu ve prefab'ları kaldırılmaz. S-Masa onların bileşenlerini (klasör, kâğıt, tablo, kaydırma, damga, olay modali) kullanır.

## 8. Fazlar

| Faz | İçerik | Bu projede not |
|---|---|---|
| F1 | sprite kiti + `SadeUI` atlası + içe aktarıcı, dokular, `SadeTheme` / `RaidTheme`, ortak bileşenler, `SadeUI_Gallery` | C F1 hazır; yalnız yeni parçalar |
| F2 | S-Masa durağan: kaplama, harita katmanları, 12 bölge mesh'i, düğüm, etiket, iplik, ekipler; köşeler | §5 kararına göre eşleme |
| F3 | S-Masa etkileşim ve paneller (S-Masa2–9), klavye, İllüzyon; `DeskGame.unity` canlı oyun köprüsü | **17 Ekim kullanıcı bildirimi düzeltildi** — giriş Canvas'ı ve eksik bağlantılar; 308/308 EditMode, 128/128 canlı kontrol (`F3_NOTLARI.md`) |
| F4 | Baskın tahtası: kamera k = 1,5, arka plan, sade dünya etiketleri; B1–B8 ön ayarları | **Tamamlandı (17 Ekim)** — `RaidBoard_Sandbox`; 322/322 EditMode, 230/230 canlı kontrol (`F4_NOTLARI.md`) |
| F5 | Baskın HUD + birim kartı + üstüne gelme kartı; sahte komutlar | **Tamamlandı (17 Ekim)** — 330/330 EditMode, 439/439 Play, 10/10 gerçek girdi (`F5_NOTLARI.md`) |
| F6 | Baskın durumları ve panelleri (S-Baskın3–9); sonra `Baskin` sahnesine bağlama | **Tamamlandı (18 Ekim)** — 341/341 EditMode, 826 Play kontrolü, 16 girdi ve 56 canlı köprü kontrolü (`F6_NOTLARI.md`) |
| F7 | EN, taşma denetimi, en-boy oranları, performans, belgeler | **Uygulandı (18 Ekim)** — 345/345 test; 144 birleşim; ana masa 30 batch. Kart açıkken 50 ve görsel referans farkları belgeli (`F7_NOTLARI.md`) |

**Her fazda:**
- karşılaştırma (`compare.py`) ve TR/EN metin denetimi;
- EditMode testleri;
- Play denemesinden önce kullanıcının kayıt dosyası ve tercihleri yedeklenir, sonra karşılaştırılarak geri yüklenir.

## 9. Riskler

- **Harita bölgeleri** (§5): kararsız kalırsa F2 başlayamaz.
- **Çizimde olmayan oyun içeriği** (§4): yeni yerleşimler tasarımdan sapma sayılır; `KARARLAR.md`'de tek tek gösterilecek.
- **Kalabalık bölgeler:** 1920×1080 haritada Avrupa ve Orta Doğu düğümleri sıkışık. Oyunun düğüm konumları tasarımınkinden farklıysa etiket çakışması olabilir; tasarımın etiket kaydırmaları (`label.dy`) kullanılır.
- **3D renkler:** Lit ile tasarımdaki yüz tonlarına ±%5 yaklaşmak ışık ayarı ister. Tutmazsa vertex color (paketin önerdiği geri dönüş).
- **Harita dokuları:** 3840×2160 iki doku; bellek bütçesi `KARARLAR.md`'ye yazılacak.
- **Paralel çalışma:** kullanıcı editörde paralel oynuyor ve sahneleri düzenliyor. Sahne değişikliklerinden önce kaydedilmemiş değişiklik kontrolü yapılır; testler kirli sahneyle çalıştırılmaz.

## 10. Sorular

1. **Harita bölgeleri** (§5): A (çokgen eşlemesi, önerilen), B (çokgenleri oyunun bölgelerine göre yeniden üret) ya da C (oyunun bölgelerini değiştir)?
2. **Çizimde olmayan oyun özellikleri** (§4): öneriler uygun mu? Araştırmada ekonomi kartları, kartta ekip fişleri, tam dosyada bölge fonu ve diplomasi, haritada saha raporu fişleri, haber “Denetle” = bölge denetimi.
3. **Eski düzen:** S-Masa ve S-Baskın oyun sahnelerine geçince C1 düzeni ile B HUD'u tamamen mi kalkacak, yoksa ayarlardan seçilebilir bir “klasik görünüm” olarak mı kalacak? Varsayılan: kalkar; C1 bileşenleri ortak parça olarak kalır.
4. **Sıra:** önce masa mı, önce baskın mı, yoksa paketteki sıra mı (F1 → masa → baskın)?
