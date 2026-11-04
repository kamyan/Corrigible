# Ayarlar dolabı ve menü (giriş) sahnesi

_2026-11-03. İstek (aynen): "ayarlar bir dolap olsun, açılınca içinde sliderlar kaldırmalı fiziksel, tıklananlarda şalter
gibi tamamen fiziksel olsun, birde menü sahnesi ekleyelim, onda da kapı dışı olabilir kapıyı açıp girer, menu sahnesinde de
ayarlar dolabı olur, çıkış için birşey olur, fikir üret". **Uygulandı (2026-11-03, "ayarlar dolabı ve menü sahnesini
uygula"):** Katman 1–4 ve 6–9, her kararda önerilen seçenek; ayrıntı ve sapmalar en sonda, "Uygulama" başlığında.
"Sliderlar kaldırmalı" ifadesi kollu, yukarı kaldırılan sürgü diye okundu; Karar 2'de teyit isteniyor._

Öneri tek cümleyle şu: **ayarlar da odanın bir eşyası olur, oyun da kapıdan başlar.** Ayarlar artık kâğıt bir pencere
değil, sol duvardaki çelik bir dolap. İçinde kollar, şalterler ve bir kademe anahtarı var. Oyun, sığınağın kapısının
dışında, ziyaretçilerin durduğu sahanlıkta açılır. Oyuncu vardiya kartını basar, kapıyı açar ve içeri girer.

## Sorun

**Ayarlar bugün**
- Ayarlar `SettingsCard.prefab` (`DeskUI › Modal`, sıra 100) ve `SettingsView` ile açılıyor. İçinde dört kanal var
  (`FaderView`: tıkla-sürükle, %5 adım), bir "Ses açık" satırı (`RuleRowView`), iki dil kutusu (`RadioCheckView`) ve bir
  Kapat düğmesi. Odanın geri kalanı 3B ve fiziksel; ayarlar ise odanın üstüne açılan düz bir kâğıt pencere.
- Arka uç sağlam ve yeniden kullanılabilir:
  - `IDeskSettings` (`Desk/Scripts/Runtime/Data/DeskData.cs:20`) → `BridgeSettings` → `AudioPrefs`. PlayerPrefs anahtarları
    `corrigible.audio.Master/Music/Ambience/Effects/muted`. Genel düzey ve sessiz mod `AudioListener.volume`'a uygulanıyor
    (`AudioDirector.cs:173`).
  - Dil `DeskLoc` ve `corrigible.language` ile değişiyor, sahne yeniden yüklenmiyor. Gün 0'da oyun yeniden kuruluyor.
- Ayarlar dört yoldan açılıyor:
  - sağ üstteki AYARLAR plakası (`SettingsPlaque`);
  - kenar satırının son çipi (`EdgeLine`);
  - Esc (`LiberationDeskView.Escape`);
  - emir belgesi açıkken Esc (`SiginakBriefing.LateUpdate`'teki kare hilesi).
- `SettingsView.Instance.IsOpen` beş yerde okunuyor: `LiberationDeskView.InputBlocked`, `CommandPostDesk` (kamera girdisi),
  `SiginakTutorial.Read`, `SiginakBriefing` ve `DeskInput`.

**Menü ve çıkış bugün**
- Menü yok. Build listesinde DeskGame ilk sırada ve `GameController.Awake` her açılışta `NewGame()` çağırıyor. Ardından
  açılış oynuyor (siyah → jeneratör → ampul) ve HAREKÂT EMRİ düşüyor. Belgede BAŞLA düğmesi ve KAYITTAN DEVAM sekmesi
  var. Kayıt varken BAŞLA'ya iki kez basılıyor; ilk basış teleks uyarısı veriyor (`BriefingBuilder.Warning`).
- **Çıkış yok:** `Application.Quit` hiçbir yerde çağrılmıyor. Oyuncu ancak pencereyi kapatarak çıkabiliyor. Kayıt her
  komutta, vardiya sonunda ve `OnApplicationQuit`'te yazıldığı için veri kaybolmuyor. Yine de Steam'deki bir oyunda görünür
  bir çıkış gerekir.
- Baskında (Baskin sahnesi) ayarlara ulaşılamıyor. `DeskInput` askıda; `RaidHudView`'da Esc yalnız bekleyen emri iptal
  ediyor.

**Oda (dolabın yeri için ölçüler)**
- Oda x −2,2…2,2, z −1,6…2,3, y 0…2,5 aralığında. K1 kamerası (0, 1,52, −0,15) noktasından +z'ye bakıyor.
- Sol duvarın rafın önündeki kısmı boş: x −2,2, z −0,7…1,18 (raf z 1,18'de başlıyor). Bu kısım K1'den görünmüyor: en yakın
  köşe ~60°'de, yatay görüşün yarısı ise ~44°.
- Diğer duvarlar:
  - Sağ duvarda kapı (z 0,32…1,18) ve interkom (z ≈ 0) var.
  - Arka duvar dolu: pano, alarm, afiş, borular, kanal.
  - Ön duvar (z −1,6) kameranın arkasında kalıyor; hiç görünmüyor ve tasarlanmadı (sol duvarın o tarafta uzantısı bile yok).
- Koridor: x 2,2…4,6, z −0,05…1,55 (2,4 × 1,6 × 2,6 m). Yedi basamak, tırabzan ve iki lamba var; lambalar kapı kapalıyken
  sönük.

## İlkeler

- **Odanın kuralları burada da geçerli.** Pirinç plakası olan her nesne tıklanır. Üzerine gelince nesne kalkar ve bir kâğıt
  etiket çıkar (başlık + tek satır; `DeskTarget`, `HoverTag`). Ekranda tuş ipucu yok; her kısayolun tıklanarak yapılan bir
  karşılığı var.
- **Kaldır = artır, indir = kes.** Sürgü yukarıdayken ses yüksek, şalter yukarıdayken açık. Bir şeyi bitiren ya da kesen
  kollar aşağı iner: vardiya şalteri böyle, çıkış şalteri de böyle olur.
- **Değer yalnız konumdan okunmaz.** Her kontrolde üç ipucu bulunur: konum ve çentik, yazılı ölçek ya da sayaç, üzerine
  gelince tam değer (etiket: "MÜZİK · %85").
- **Geri dönülmez işler iki adımlıdır.** Kayıtlı harekâtın üstüne yeni harekât başlatmak ve oyundan çıkmak vardiya
  şalterinin desenini kullanır (`ShiftSwitch`): 1. tıklama kapağı kaldırır, 4 s içindeki 2. tıklama işi yapar, süre dolarsa
  kapak kapanır. Ses, sessiz mod ve dil ise tek tıklamadır, anında uygulanır ve geri alınabilir.
- **Ayrılmış renkler korunur.** Kırmızı YZ'ye ve ölümcül kapıya ait. Dolabın lambaları amber (açık) ya da sönük (kapalı)
  olur. Sessiz mod da kırmızıyla değil, lambaların sönmesiyle anlatılır.
- **Tek arka uç, tek prefab.** Dolap `IDeskSettings`'e bağlanır ve yeni tercih anahtarı açmaz (Katman 5 hariç). İki sahne
  aynı prefab'ı kullanır.
- **DeskGame tek başına açılabilir kalır.** Editör, testler, `SiginakCaptureSet` ve ajan testi DeskGame'i doğrudan açıyor.
  Menüden gelen bir niyet yoksa bugünkü açılış (emir belgesi, BAŞLA, KAYITTAN DEVAM) aynen çalışır.
- **Core'a dokunulmaz.** Hiçbir katman komutu, RNG'yi ya da kayıt biçimini değiştirmez; `GameRecord.CurrentVersion` aynı
  kalır.

## Özet

| Katman | Ne | Maliyet | Önerilen |
|---|---|---|---|
| 1 | Dolap: yer, model, açılış, kamera pozu | M | sol duvar, tek tıkla açılır |
| 2 | Ses sürgüleri | M | kollu dikey sürgü + sayaç teker + ibre |
| 3 | Aç-kapa şalterleri (sessiz mod) | S | yarasa kollu anahtar + lamba |
| 4 | Dil | S | döner kademe anahtarı |
| 5 | Görüntü sırası (isteğe bağlı) | M | karar bekliyor |
| 6 | Menü sahnesi: kapının dışı | L | sahanlık, kapının dış yüzü |
| 7 | Yeni harekât / devam düzeneği | M | puantaj saati ve vardiya kartı |
| 8 | Çıkış (menüde ve odada) | S | menüde son şalter, odada dolaptaki kol |
| 9 | Geçiş ve yükleme akışı | M | menü niyeti + eşzamansız yükleme |

Dolabın toplam maliyeti (1–4) yaklaşık L, menünün toplamı (6–9) da yaklaşık L.

### 1. Dolap: yer, model, açılış · [sunum] · M

**Yer (DeskGame)**
- (a) **Sol duvar, rafın önü.** Dolap x −2,2, z ≈ −0,04…0,64, y 1,05…1,75 aralığında durur. Kapının karşısında olduğu için
  odayı dengeler. K1'den görünmez; kamera sola ~90° döner. **Önerilen.**
  - Artı: duvar boş; dolap hiçbir pozla kadraj paylaşmaz; dönüş kısa; ışık sorununu dolabın iç lambası tek başına çözer.
  - Eksi: K1'den görünmediği için oyuncu dolabı AYARLAR plakası, kenar satırı ve Esc sayesinde bulur. Bugün de durum böyle.
- (b) **Sağ duvar, kapının yanı** (z −0,6…0,2). Menüdeki yeriyle aynı mantık: "kapının hat dolabı".
  - Eksi: kapı pozu (K8) ziyaretçiye ait. Ayarlar ölümcül kapıyla aynı kadraja girer; interkomla da yer çakışır.
- (c) **Ön duvar, kameranın arkası.** "Arkandaki dolap"; kamera 180° döner.
  - Eksi: dönüş uzun. Odanın bu yüzü hiç tasarlanmadı (sol duvar uzantısı yok, ışık ayarsız); daha çok sanat işi ister.
- (d) **Masa çekmecesi** (sol ayak, 42 cm).
  - Eksi: dört sürgü, bir şalter ve bir seçici sığmaz. Çekmecenin içi alçak ve karanlık; "dolap" isteğine de uymaz.

**Model** (önerilen ölçüler)
- Duvara asılı çelik bir hat dolabı; dış ölçüleri 0,68 × 0,70 × 0,22 m.
  - Boyası kapı kanadıyla aynı (#3A3F33). Üstünde kapınınki gibi krem bir şablon yazı (#D8D2BE): "AYARLAR" (`DeskLocText`).
  - Pirinç bir plaka ve dişli simgesi (anahtar rayındaki `tx_key_*` dokularının dişlisi); çeyrek tur dönen bir mandal kolu.
- Kanat rafa doğru açılır, böylece kameranın önünü kesmez. İçeride, tavanda kafesli küçük bir ampul var; kapak açılınca
  0,2 s kırpışıp yanar (`BulbFlicker`'ın kısa hâli).
- İç panel 0,60 × 0,55 m, koyu bakalit:

```
 ┌───────────────────────────────────────────┐
 │  (ibre)  (ibre)  (ibre)  (ibre)            │
 │   ┃ ┳     ┃ ┳     ┃ ┳     ┃ ┳      ○ SES   │  kollu sürgüler    yarasa kollu anahtar + lamba
 │   ┃       ┃       ┃       ┃                │
 │   ┃       ┃       ┃       ┃                │
 │  [080]   [100]   [100]   [100]     ◐ DİL   │  sayaç tekerleri   döner kademe anahtarı
 │  GENEL   MÜZİK   ORTAM   EFEKT   LANGUAGE  │  pirinç plakalar
 └───────────────────────────────────────────┘
```

Katman 5 seçilirse panelin altına bir GÖRÜNTÜ sırası eklenir.

**Açılış ve kapanış**
- Dolap dört yoldan açılır: dolaba tıklama (etiket "AYARLAR · Ses ve dil"), AYARLAR plakası, kenar satırının çipi ya da
  Esc. Kamera yeni K9 pozuna geçer (öteki pozlar gibi 0,45 s). Kamera varınca mandal döner (0,15 s), kanat açılır (0,45 s)
  ve ampul yanar.
- Dolap **tek tıkla açılır** (kamera + kapak). **Önerilen.** İki tıklama (önce poz, sonra mandal) daha fiziksel olurdu, ama
  ayarlar sık yapılan, zararsız bir iş.
- Kapatmak için Esc'e basılır, açık kanada ya da panelin dışına tıklanır veya MASA tuşuna basılır. Kanat kapanır (0,35 s),
  mandal döner, kamera önceki poza döner. Ayarlar her bırakışta ve kapanışta kaydedilir (`AudioPrefs.Save`).
- K9 önerisi: kamera (−1,45, 1,42, 0,30) noktasında, −x'e bakar, dikey görüş 45°. Bu poz paneli kadraja alır; 1080p'de
  1 px ≈ 0,57 mm. Bu pozda sağ tuşla bakış ve tekerlekle yaklaşma kapalıdır; tekerlek sürgünün ince ayarı olarak kullanılır.
- Sesler: mandal, menteşe ve kapanış. Yeni `Cue` değerleri listenin sonuna eklenir, çünkü asset bu değerleri sayı olarak
  saklıyor. Yer tutucu sesler, kapınınkiler gibi kodla sentezlenir.

**Riskler**
- **Girdi kilidi:** `DeskTarget.Blocked` bütün oda için tek bir bayrak. Dolap açıkken oda kilitli olmalı, ama dolabın
  kontrolleri çalışmalı. Bu yüzden kontroller kendi işaretçi bileşenini kullanır; oda kilidine değil, dolabın açık olup
  olmadığına ve sahne geçişine bakar.
- **Beş okuyucu:** `InputBlocked`, `CommandPostDesk`, eğitim, emir belgesi ve `DeskInput` bugün `SettingsView`'ı okuyor.
  Dolap aynı API'yi (`Instance`, `IsOpen`, `Open`, `Close`, `Toggle`) sunarsa her biri tek satırlık bir değişiklikle dolaba
  geçer.
- **Emir belgesi:** belge açıkken de dolap açılabilmeli, çünkü ilk açılışta dil seçilir. Belge Overlay'de olduğu için
  dolabın önünü örter; dolap açıkken belge kenara kayar ya da gizlenir.
- **Esc:** Esc'i tek bir yer yönetmeli. Bugün `DeskInput`, emir belgesinin `LateUpdate` hilesi ve kapının Esc'i sırayla
  çalışıyor; dolap hepsinden önce gelmeli.
- **Performans:** dolap kapalıyken iç parçalar çizilmez (renderer'lar kapanır); iç lamba gölge üretmez.

### 2. Ses sürgüleri · [sunum] · M

**Seçenekler**
- (a) **Kollu dikey sürgü.** Yayın masasındaki fader'lar gibi: 18 cm'lik bir yarıkta T başlı bakalit kol; yukarısı yüksek.
  Kol sürüklenir; imleçten gelen ışın panel düzlemine düşürülür ve yarığın eksenine izdüşürülür. Yarığın boş bir yerine
  tıklamak kolu oraya kaydırır (0,08 s; bugünkü `FaderView` da böyle). **Önerilen.**
  - Artı: tek eksen, fareye en doğal eşleme. Dört kol yan yana tek bakışta okunur. "Kaldır = artır" harfiyen uygulanır.
  - Eksi: yarık ince bir geometri (TAA riski, aşağıda).
- (b) **Çeyrek daire kolu** (gemi telgrafı, lokomotif gaz kolu). Kol bir yay üstünde döner, kadranı çentiklidir.
  - Artı: en ağır, en fiziksel his.
  - Eksi: fareyle bir yay boyunca sürüklemek kesin değil. Dört kol geniş yer kaplar; değer zor okunur.
- (c) **Döner düğme** (potansiyometre ve kadran).
  - Artı: az yer kaplar; telsizin SES/AYAR düğmeleriyle aynı dili konuşur.
  - Eksi: fareyle döndürmek en kötü eşleme (dikey mi sürüklenecek, dairesel mi?). Dört düğme de telsizi taklit eder.

**(a) seçeneğinin ayrıntısı**
- **Değer:** %0–100, %5 adımla (bugünkü gibi; tercih değeri kararlı kalır). Her %10'da bir çentik vardır: kol 1 mm
  "oturur" ve tık sesi gelir. %5'te yarım çentik, daha sessiz bir tıkla.
- **Okuma:** değer üç yoldan okunur.
  - Yarığın solunda 0–10 ölçeği. Bu ölçek geometri değil doku olarak çizilir (aşağıda TAA).
  - Kolun altında üç haneli bir sayaç teker; masanın sayaç tekerleri ve rakam dokusu yeniden kullanılır. Değer değişince
    teker döner (080 → 085).
  - Üzerine gelince ve sürüklerken etiket: "MÜZİK · %85".
- **İbre:** her kanalın üstünde VU benzeri bir gösterge.
  - (i) İbre değeri yaylı bir gecikmeyle izler. **Önerilen ilk adım** (S).
  - (ii) İbre kanalın o anki gerçek düzeyini gösterir (kaynakların çıkış RMS'i; M). Örneğin masada ortam yatağı susarken
    "bu kanal şimdi sessiz" diye gösterir. Ama ibre sürekli oynar ve dikkat dağıtır.
- **Lamba:** değer 0'dan büyükse ve ses açıksa kanalın amber lambası yanar.
- **Genel sürgü** ötekilerden ayrı durur: pirinç başlıdır ve bir aralıkla ayrılır.
- **Dinletme:** `BridgeSettings.Preview` aynen kalır (efekt kanalında tık, ortamda telsiz sesi; müzik zaten çalıyor).
  Menüde de müzik ve ortam sesi çalmalı, yoksa dinletmenin anlamı kalmaz (Katman 6).

**Fare ve erişilebilirlik**
- Kolu tam tutturmak gerekmez: yarığın çarpıştırıcısı kolun iki yanına ±15 mm taşar. Sürüklerken imleç yarıktan çıksa da
  sürükleme sürer.
- Tekerlek: üzerindeki sürgüyü ±%5 oynatır (ince ayar).
- Klavye (ekranda ipucu yazmaz):
  - Dolap açıkken Tab kontroller arasında gezer. Odak, üzerine gelmeyle aynı görünür (kalkma + etiket).
  - ↑/↓ sürgüyü ±%5 oynatır; basılı tutunca tekrar eder. ←/→ dil seçiciyi çevirir.
- Çift tıklama kanalı varsayılana döndürür (genel %80, öteki kanallar %100). İsteğe bağlı.
- Hiçbir bilgi yalnız renge dayanmaz: lamba yanık ya da sönük parlaklığıyla, değer de ibre ve sayaçla okunur.

**Riskler**
- **TAA:** K9'da 1 px ≈ 0,57 mm. 6 mm'lik yarık ~10 px, kol başı 25 mm; bunlarda sorun yok.
  - İbre en az 2 mm olmalı (≥ 3–4 px). Zincirin 3 mm'de titrediği ölçüldü (DURUM, 2026-11-01).
  - Ölçek çizgileri mip'li bir dokuya çizilir; saydam tuş simgelerinin neredeyse hiç titremediği ölçülmüştü.
  - Öteki pozlardan dolap kapalı görünür, içi görünmez.
  - `taa-flicker-tools/measure.cs` K9'da çalıştırılır.
- **Punto:** etiketler ≥ 14 px olmalı (yazı yüksekliği ≥ 8 mm); ölçek rakamları ≥ 12 px (≥ 7 mm). Dolap `SiginakTextScan`'in
  köklerine eklenir ve K9 pozunda taranır.
- **Lokalizasyon:** İngilizce adlar daha uzun ("AMBIENCE" 8 harf, "ORTAM" 5; "EFFECTS"). Plakalar İngilizceye göre ölçülür
  (~80 mm) ya da TMP'nin küçültmesi punto sınırında durur.

### 3. Aç-kapa: şalterler · [sunum] · S

Bugün tek bir aç-kapa var: Ses açık (sessiz mod).

- (a) **Yarasa kollu anahtar.** Üstünde amber bir lamba var; yukarı konumda AÇIK, aşağıda KAPALI yazar. Tıklama kolu çevirir
  (0,08 s, "klak"). Oyuncu deneyeceği için kol yukarı-aşağı sürüklenerek de çevrilebilir. **Önerilen.** Görüntü sırası
  gelirse aynı anahtarlar yan yana dizilir.
- (b) **Bıçaklı şalter.** Gösterişli ve büyük. Çıkışa ayrılması öneriliyor (Katman 8); ikisi aynı olursa anlamları karışır.
- (c) **Kapaklı anahtar** (vardiya şalteri gibi iki adım). Sessiz mod zararsız bir iş; iki adım fazla.

**Geri bildirim:** ses kapanınca kanal lambaları söner, ibreler yayla sıfıra düşer, sürgüler yerinde kalır. Bugünkü açıklama
("Kapatınca bütün kanallar susar; ayarlar korunur.") etiketin satırına taşınır.

### 4. Dil · [sunum] · S

- (a) **Döner kademe anahtarı** (telsizin bant anahtarı gibi). İşaretçili bir düğme ve iki konum: "TÜRKÇE" ve "ENGLISH".
  **Önerilen.**
  - Her dil kendi dilinde yazılır ve hiç çevrilmez. Plaka "DİL · LANGUAGE" iki dilli ve sabittir; dili bilmeyen oyuncu da
    bulur.
  - Tıklama düğmeyi sonraki konuma çevirir (45°, ağır bir "klonk"); sürükleyerek de çevrilebilir.
  - Yeni bir dil, düğmeye bir konum daha eklemek demektir.
- (b) **İki konumlu şalter TR | EN.** En ucuzu; ama iki dilde kalır ve sessiz mod anahtarına benzer.
- (c) **Dil kartı yuvası.** Kancalarda iki kart asılı; tıklanan kart pirinç çerçeveye kayar. En sevimlisi ve kart
  ekleyerek büyür. Maliyeti M: kartların yazısı ve kayma canlandırması iş ister.

**Dil değişince** `DeskLoc` sahneyi yeniden yüklemeden değişir (`BridgeSettings.ApplyLanguage`) ve bütün `DeskLocText`'ler
aynı anda güncellenir. Geri bildirim olarak iç ampul bir kez kırpışır; DeskGame'de teleks yeni dilde bir satır basar. Gün 0
kuralı aynı kalır: hiç oynanmadıysa oyun yeni dilde yeniden kurulur.

**Risk:** dolabın çevrilen bütün yazıları dünya TMP'si olmalı (`DeskLocText`); dokuya yalnız rakamlar ve çizgiler gömülür.
`LocalizationTool` bugün `DeskLocText`'leri ve `DeskTarget` başlıklarını topluyor. Dolap kontrollerinin etiket başlıkları
ve satırları da toplanmalı: ya kontroller `DeskTarget`'ın alanlarını kullanır ya da araç genişletilir.

### 5. Görüntü sırası (isteğe bağlı) · [sunum] · M

Bugün oyunda görüntü ayarı yok. TAA ve post-processing yalnız Inspector'dan açılıyor (DURUM, "TAA açıkken ince nesneler
titriyordu"). PC oyuncusu en azından tam ekran/pencere, dikey eşitleme ve kenar yumuşatma ayarı bekler.

- Panelin altına bir GÖRÜNTÜ sırası eklenir. Yarasa kollu anahtarlar: TAM EKRAN, DİKEY EŞİTLEME, KENAR YUMUŞATMA
  (kapalı/TAA), EFEKTLER (bloom, vinyet). Çözünürlük bir kademe anahtarıyla seçilir; değer küçük bir pencerede görünür.
- Yeni bir `VideoPrefs` (Game; PlayerPrefs `corrigible.video.*`) şunlara uygular: `UniversalAdditionalCameraData`
  (`antialiasing`, `renderPostProcessing`), `Screen.fullScreenMode` ve `QualitySettings.vSyncCount`. Odanın da baskının da
  kamerası bu ayarları uygular.
- Risk: TAA açılınca titreme ölçümü değişir (sahnede bugün kapalı). Çözünürlük listesi uzunsa kademe anahtarı kalabalıklaşır.

### 6. Menü sahnesi: kapının dışı · [akış + sunum] · L

**Fikir:** oyuncu, ziyaretçilerin durduğu yerde durur: Sığınak 07'nin kapısının önündeki sahanlıkta. Oyunda bu kapıyı
başkaları çalar; menüde kapıya gelen, nöbete gelen komutanın kendisidir. Kapıyı açıp içeri girmek oyunu başlatır.

**Sahne:** `Desk/Scenes/Giris.unity`. Build listesi: Giris (0), DeskGame (1), Baskin (2).

```
                   S O N   Ş A L T E R          (kapının üstünde şablon yazı)
   ┌───────┐   ┌──────────────┐   ┌────┐             ╱
   │ dolap │   │  SIĞINAK 07  │   │ ⏲  │ kart       ╱  merdiven yukarı
   │AYARLAR│   │    (çark)    │   │saat│ rafı      ╱   (karanlık, uğultu)
   └───────┘   │              │   └────┘          ╱
  [son şalter] └──────────────┘
```

- **Sahanlık:** DeskGame'deki `Corridor` tek seferlik bir betikle prefab'a çevrilir. Menüde, onun malzemeleriyle
  genişletilmiş bir sahanlık kurulur. DeskGame'de kapıdan görünen parça aynı kalır.
- **Kapının dış yüzü:** `Bunker door` da prefab olur (`DoorProp` yalnız çizdiği için iki sahnede de çalışır). Dış yüz
  baştan yapılacak bir iş:
  - dış çark, "SIĞINAK 07 · KYGA" şablonu;
  - dış interkom ızgarası ve düğmesi;
  - gözetleme deliğinin pirinç halkası;
  - kapı çerçevesi ve sahanlık tarafındaki duvar yüzü.
- **Başlık:** kapının üstünde bir şablon yazı: Türkçede "SON ŞALTER", İngilizcede "CORRIGIBLE" (`DeskLocText`).
  Alternatifi duvarda "AĞA BAĞLANMA" gibi bir afiş (Karar 9).
- **Merdivenin yukarısı:** basamaklar karanlığa çıkar. Yukarıdan uzak bir uğultu gelir; isteğe bağlı olarak soluk kırmızı
  bir nabız da. Yukarısı YZ'nin, kırmızı da yalnız onun rengi (Karar 11).
- **Dolap:** aynı prefab, sahanlığın sol duvarında ("hat dolabı").
- **Kamera:** dört poz: M0 sahanlık (kapı ortada; dolap, saat ve şalter görünür), M1 dolap, M2 saat ve kart rafı, M3 içeri
  yürüyüş. `SiginakCameraRig` `SiginakPose`'a ve `CommandPostDesk`'e bağlı; menü için aynı geçiş eğrisini kullanan küçük bir
  kamera öneriliyor.
- **Ses:** `AudioDirector` simülasyonu okuyor, menüde bulunmuyor. Küçük bir menü sesi bileşeni iki şey çalar: merdiven
  ortamı (floresan vızıltısı, damla, uzak gümbürtü) ve müziğin boğuk drone katmanı. İkisi de `AudioPrefs`'in kanal
  düzeylerine uyar. Genel düzey ve sessiz mod, `AudioDirector`'daki gibi `AudioListener.volume`'a uygulanır. Böylece
  sürgülerin dinletmesi menüde de anlamlı olur.
- **Girdi:** EventSystem, PhysicsRaycaster ve `HoverTag`'in Overlay canvas'ı. Tuş ipucu yok. Esc dolabı kapatır ya da M0'a
  döner; oyundan çıkarmaz.
- **Dil:** `DeskLoc.Instance` statik olduğu için menüde seçilen dil DeskGame'e taşınır.

**Riskler**
- **Assembly:** menünün bileşenleri üç şeyi birlikte kullanır: Core'un `Replay`'i, Game'in kayıt ve ses sınıfları ve
  Desk'in dolabı. Bu yüzden yerleri Bridge olmalı (`Desk/Scripts/Bridge/Giris/`); böylece Game'den Bridge'e ters bağımlılık
  doğmaz.
- **Kapı ve koridor bağları:** kapı ile koridor birbirine bağlı (`DoorProp` → merdiven ışıkları ve siluet). Prefab'a
  çıkarırken bu bağlar sahnede kalmalı; `SiginakDoorTests`'in sahne testi bunları denetliyor.

### 7. Yeni harekât / devam düzeneği · [akış] · M

- (a) **Puantaj saati ve vardiya kartı.** Kapının sağında mekanik bir kart basma saati ve bir kart rafı. **Önerilen.**
  - **Kart:** kayıt varsa rafta oyuncunun kartı durur. Kartta "KOMUTAN · SIĞINAK 07" ve "V.014 · ÖZGÜR 3/12" yazar, her
    vardiya için bir zımba deliği vardır. Vardiya oyunun zaman birimi olduğu için kart harekâtın kendisi gibi okunur.
  - **Devam:** karta tıklanır → kart saate kayar → "ka-çunk" damgası → kapının zili çalar, çark döner, kapı açılır ve kamera
    içeri girer.
  - **Yeni harekât:** rafın altında boş bir kart destesi durur ("YENİ HAREKÂT").
    - Kayıt yoksa tek tıklama yeter.
    - Kayıt varsa destenin üstünde yaylı bir kıskaç vardır (vardiya şalterinin kapağı gibi). 1. tıklama kıskacı kaldırır;
      eski kart raftan öne eğilir ve üstünde "İPTAL EDİLİR · V.014" fişi görünür. 4 s içindeki 2. tıklama onaylar; yoksa
      kıskaç kapanır. Bugünkü "iki kez bas + teleks uyarısı" böylece fiziksel olur.
  - Artı: kayıt özeti doğal bir yer bulur; onay görünür ve fizikseldir; fikir vardiya kurgusuna oturur.
  - Eksi: saat modeli, kart canlandırması ve kartta iki dilli yazı gerekir. 70'i aşan vardiya da kartın deliklerine
    sığmalı (10'arlı satırlar ya da son 10 satır + toplam sayı).
- (b) **Kapı + duvar panosu.** Panoda iki kâğıt asılı: "KAYITLI HAREKÂT · V.014 · özgür 3/12" ve "YENİ HAREKÂT". Oyuncu
  bir kâğıt seçer, sonra çarkı çevirir. Maliyeti S. Ama kâğıt, kılık değiştirmiş bir menü olur (bugünkü emir belgesinin
  sekmesi gibi).
- (c) **İnterkom.** Düğmeye basılır, içeriden "Kim o?" diye sorulur, oyuncu bir cevap seçer ("Komutan döndü" / "Yeni
  komutan"). Kapının interkomuna bağlanır; ama bu bir diyalog menüsü, fiziksel bir düzenek değil.
- (d) **Anahtarlık.** Kancada "V.014" fişli bir anahtar, bir zarfta da yeni bir anahtar. (a)'ya benzer; ama fişler küçük ve
  punto riski taşır.

**Kayıt durumları**
- **Kayıt yok:** rafın yuvası boş; yalnız deste görünür.
- **Kayıt var:** rafta kart, altında kıskaçlı deste.
- **Kayıt okunamıyor** (eski biçim ya da bozuk dosya): bugün sessizce "kayıt yok" sayılıyor (`GameController.LoadRecord`).
  Menüde gri, tıklanmayan bir "OKUNAMAYAN KART" gösterilebilir; böylece oyuncu kaydının neden açılmadığını görür (Karar 12).
- **Özet:** `BriefingBuilder.Summarize` ile çıkar (kaydı bir kez oynatır). Core saf C# olduğu için uzun bir harekâtta bu
  hesap, kart boş dururken bir arka iş parçacığında yapılabilir.

### 8. Çıkış · [akış] · S

**Menüde**
- (a) **Son şalter.** Sahanlığın duvarında kapaklı, büyük bir bıçaklı şalter; plakasında "ANA ŞALTER · MAIN SWITCH" yazar.
  Adı oyunun Türkçe adından gelir. **Önerilen.**
  - `ShiftSwitch`'in deseni aynen kullanılır: 1. tıklama kapağı kaldırır (etiket: "Işıkları kes · oyundan çık"), 4 s
    içindeki 2. tıklama kolu indirir.
  - Lambalar 0,6 s kırpışıp söner, jeneratör yavaşlayarak susar, ekran kararır → `Application.Quit` (editörde Play durur).
  - Artı: vardiya şalterinin kodu ve deseni yeniden kullanılır; kazara çıkış olmaz; oyunun adına gönderme yapar.
  - Eksi: masada da bir şalter var ve o vardiyayı bitiriyor. Biçimleri farklı olmalı (bu bıçaklı ve kapaklı); zaten
    başka sahnede duruyorlar.
- (b) **Merdivenden yukarı çıkmak.** Basamaklara tıklanır ("YUKARI · sığınaktan ayrıl"); kamera döner ve merdivenden çıkar,
  yukarıdan gelen ışık büyür, ekran kararır.
  - Artı: hikâyesi güçlü ("yukarısı YZ'nin").
  - Eksi: merdivenin üstü ve kamera yolu yeni iş; "çıkış" olduğu daha az belli; onay için ikinci bir tıklama gerekir.
- (c) **Duvar telefonu.** Ahize kapatılır, hat sesi gelir, oyun kapanır. Ama telefon "ara" der, "ayrıl" demez.
- (d) **Acil çıkış lambası.** Evrensel ve anlaşılır. Ama en az tematik olanı; üstelik ana kapıyla yarışan ikinci bir kapı
  demek.

**DeskGame'de** (bugün hiç çıkış yok)
- (i) **Dolabın içinde kapaklı bir kol:** "MENÜYE DÖN" (menü sahnesi yokken "ÇIKIŞ"). **Önerilen.**
  - Oyunların duraklatma menüsünde olduğu gibi ayarların yanında durur ve iki adımlıdır.
  - Önce `GameController.Save()` ve masa durumu (`SiginakDeskStore`) kaydedilir; sonra ekran kararır ve menü açılır.
    Oyundan menüdeki son şalterle çıkılır.
- (ii) **Kapıdan çıkmak.** Kimse beklemiyorken kapıya tıklanır, "Sığınaktan ayrıl" seçilir; iki adımdan sonra kapı açılır
  ve kamera dışarı çıkar.
  - Artı: menüye girişle simetrik.
  - Eksi: kapının anlamıyla çakışır: kapı ziyaretçi, tehlike, K8 pozu ve ölümcül son demek; 4 · KAPI tuşu ziyaretçiyi
    getiriyor. Ziyaretçi beklerken kapalı olmalı ve yanlış tıklama riski taşır.
- (iii) **Hiçbiri;** oyuncu yalnız pencereyi kapatır. Steam için önerilmez.
- Çıkış ancak masa serbestken yapılabilir: baskın, geçiş, hikâye ya da açık bir kâğıt yokken (`InputBlocked` yanlışken).

### 9. Geçiş ve yükleme akışı · [akış] · M

```
Oyun açılır → Giris (karanlık → merdiven lambaları 0,6 s'de yanar)
   kayıt özeti hesaplanır (kart boş başlar, özet hazır olunca deliklerle dolar)
   ├─ Devam      : kart saate → kapı açılır ──┐
   ├─ Yeni       : (kayıt varsa iki adım) ────┤→ kamera eşikten geçer → 0,3 s'de kararır
   └─ Son şalter : ışıklar söner → çıkış      │
                                              │ LoadSceneAsync(DeskGame) kapı açılırken başlar,
                                              │ ekran kararınca etkinleşir
DeskGame: GameController.Awake → NewGame() (bugünkü gibi) → menü niyeti okunur
   ├─ Yeni : açılış (siyah → jeneratör → ampul) → HAREKÂT EMRİ (sekme yok, BAŞLA tek basış) → K1
   ├─ Devam: ContinueSaved(); emir belgesi yok; kısa dönüş: siyah → ampul (0,6 s) → K1,
   │         teleks "V.014 · VARDİYA" (kayıt açılamazsa yeni oyun ve teleks "KAYIT AÇILAMADI")
   └─ Yok  : DeskGame doğrudan açıldı (editör, testler, görüntü seti) → bugünkü açılış
```

- **Menü niyeti:** Game'de statik bir alan (Yeni, Devam, Yok). Sahne yüklenince bir kez okunur ve sıfırlanır.
- **Kayıt deposu:** `GameController`'daki kayıt yolu, okuma ve silme işlemleri Game'de statik bir sınıfa taşınır. Menü de
  `GameController` da bu sınıfı kullanır; davranış değişmez.
- **Kesintisizlik:** menünün kararması DeskGame'in siyah açılışına bağlanır; yükleme siyahın arkasında kalır.
- **Seçenek: "kapı arkandan kapanır".** DeskGame kamerası içeride, açık kapının önünden başlar ve K1'e yürür; kapı arkadan
  çarkıyla kapanır (`DoorProp`, `DoorClose`). Yeni harekâtta oyuncu karanlık bir odaya girer, jeneratör ve ampul ondan sonra
  gelir. Maliyeti M; geçişlerin en güzeli bu (Karar 10).
- **Eski kayıt:** yeni harekât onaylandığında eski kayıt bugünkü gibi ilk komutta mı üstüne yazılsın, yoksa hemen mi
  silinsin? (Karar 12)
- **Son rapor:** YENİDEN BAŞLA olduğu yerde kalır; yanına "MENÜYE DÖN" eklenebilir.

**Riskler**
- `SiginakCaptureSet` ve testler DeskGame'i niyetsiz açmalı. `SiginakBriefingTests` (sekme ve `HasSave`) yalnız "Yok"
  kipini sınar.
- `Localization.SetLanguage` etkin sahneyi build sırasıyla yeniden yüklüyor. Bugün bu yol kullanılmıyor, ama sıra
  değişince de doğru çalışır.
- Baskının eklemeli yüklenmesi bu akıştan etkilenmez.
- DeskGame'in açılış yükünü (fontlar, dokular) eşzamansız yükleme ve siyah ekran örter. İlk karede takılma olursa ampulün
  gecikmesi biraz uzatılır.

## Baskında ayarlar

Bugün baskında ayarlara ulaşılamıyor. Oda baskında gizlendiği için dolap da görünmez.

- (a) Bugünkü gibi kalsın: baskın kısa, Esc bekleyen emri iptal ediyor. **Önerilen.**
- (b) Eski ayarlar kartı yalnız baskın için kalsın. İki ayrı arayüzün bakımı gerekir.
- (c) Baskın HUD'una sesi kısan küçük bir telsiz düğmesi eklensin. Yalnız sesi kapsar; ayrı bir iş.

## Sıra

1. **Dolap DeskGame'de** (Katman 1–4). Eski kart, dolap aynı işi görene kadar durur; sonra kaldırılır.
   - Testler: sahne bağlantı testi; sürgü değer hesabı için saf C# testi (işaretçi → değer, %5 adım, tekerlek);
     `DeskGameScene_ShowsNoKeyboardHints`; LocalizationTests.
   - Ölçümler: K9'da TR ve EN punto taraması; K9'da TAA ölçümü.
   - Play'de: dolabı plakadan, kenar satırından, Esc'le ve emir belgesi açıkken açmak; sürükleme, tekerlek, sessiz mod ve
     dil; tercihlerin oyun yeniden açılınca kalması.
2. **Odadan çıkış** (Katman 8, (i)). Menü gelene kadar kol oyundan çıkar. Ucuz bir iş; oyun ilk kez görünür bir çıkışa
   kavuşur.
3. **Menü sahnesi** (Katman 6, 7, menüdeki çıkış ve 9). Sıra: koridor ve kapı prefab'ları, sahne, kamera, kart ve saat,
   son şalter, menü niyeti ve eşzamansız yükleme.
   - Doğrulama: üç açılış (kayıt yok, kayıt var, kayıt okunamıyor); yeni harekâtın iki adımı; devamda kaydın aynı
     vardiyada açılması; DeskGame doğrudan açılınca bugünkü akış; menünün TR/EN görüntüleri.
4. **Görüntü sırası** (Katman 5), karara göre.

Her aşama tek başına teslim edilebilir. 1. ve 2. aşama birlikte sahibin ilk değerlendirmesine yeter.

## Bırakılanlar

- **Kâğıt kartı 3B dünyaya asmak:** yalnız görünüşte fiziksel olur; kaydırıcılar yine düz kalır.
- **Ayarları CRT'de menü olarak göstermek:** CRT baskının penceresi; iki anlam karışır.
- **Ayrı bir duraklatma menüsü:** zaman zaten akmıyor; duraklatılacak bir şey yok.
- **Menüde odanın içini göstermek:** içeri girme anı anlamını yitirir; ağır sahne de iki kez yüklenir.
- **Tam klavye/gamepad gezinmesi:** odada tuş ipucu yok; dolapta yalnız temel klavye (Tab, oklar, Esc) olur.

## Karar bekleyenler

1. **Dolabın yeri:** (a) sol duvar **önerilen** · (b) kapının yanı · (c) kameranın arkası · (d) çekmece.
2. **Sürgü:** (a) kollu dikey sürgü **önerilen** · (b) çeyrek daire kolu · (c) döner düğme. "Sliderlar kaldırmalı"
   ifadesi kollu ve yukarı kaldırılan sürgü diye okundu; doğru mu?
3. **Değerin okunuşu:** sayaç teker + ibre + etiket **önerilen** · yalnız ibre ve etiket · yalnız çentik ve etiket.
   İbre değeri mi göstersin (önerilen), yoksa kanalın canlı düzeyini mi?
4. **Dil:** (a) döner kademe anahtarı **önerilen** · (b) iki konumlu şalter · (c) kart yuvası.
5. **Görüntü ayarları** (tam ekran, dikey eşitleme, TAA, efektler, çözünürlük) dolaba eklensin mi?
6. **Eski ayarlar kartı:** tamamen kaldırılsın **önerilen** · baskın ya da erişilebilirlik için yedek olarak kalsın.
7. **Yeni harekât / devam:** (a) puantaj saati ve vardiya kartı **önerilen** · (b) pano · (c) interkom · (d) anahtarlık.
8. **Çıkış:** menüde (a) son şalter **önerilen** · (b) merdiven · (c) telefon · (d) acil çıkış. Odada: dolaptaki kol
   **önerilen** · kapı · hiçbiri.
9. **Başlık:** kapının üstünde şablon yazı **önerilen** · afiş · hiçbiri. İngilizcede "CORRIGIBLE" mı yazsın, iki dilde de
   "SON ŞALTER" mı?
10. **Devamda içeri giriş:** kısa dönüş (ampul → K1) **önerilen ilk adım** · "kapı arkandan kapanır" yürüyüşü · doğrudan K1.
11. **Merdivenin tepesinde YZ'nin soluk kırmızı ışığı** olsun mu?
12. **Kayıt:** yeni harekâtta eski kayıt ilk komutta mı üstüne yazılsın (bugünkü gibi, **önerilen**), yoksa onayda hemen mi
    silinsin? Okunamayan kayıt menüde gri bir kart olarak görünsün mü?
13. **Baskında ayarlar:** hiç olmasın **önerilen** · eski kart yalnız baskında kalsın · HUD'da yalnız ses düğmesi.
14. **Jenerik** (emeği geçenler, font lisansları) menüde bir ilan panosunda dursun mu?

## Uygulama (2026-11-03)

Kullanıcı "ayarlar dolabı ve menü sahnesini uygula" dedi; 14 kararın her birinde önerilen seçenek uygulandı. Önerisi
olmayan sorular (5 görüntü ayarları, 11 merdivenin tepesindeki kırmızı ışık, 12'nin ikinci yarısı okunamayan kayıt kartı,
14 jenerik) yapılmadı. Sahne ve prefab değişiklikleri tek seferlik editör betikleriyle yapıldı, betikler silindi.

**Yapılanlar**
- **Dolap** (`Desk/Prefabs/Siginak/SettingsCabinet.prefab`, `Runtime/Settings/`): arka duvarın sağında, CRT masasının üstünde
  (x 1,31–2,05, y 1,14–1,90; merkez x 1,68, y 1,52). Önce önerilen yere (sol duvar) kuruldu; kullanıcı denedikten sonra K1'de
  görünen bu boşluğu seçti (Karar 1 değişti).
  dış ölçü 0,68 × 0,70 × 0,22 m, kanat kapı kanadının malzemesiyle, “AYARLAR” şablonu ve dişli plakası, çeyrek tur mandal.
  Kanat sağda menteşeli; prefab'da −120°, odadaki örnekte −95° açılır (sağ duvara değmesin). İç panel bakalit; ampul
  `cabinet.bulbIntensity` (0,3) ile ve gölgesiz; iç parçalar gölge almaz ve vermez (odanın gölgeli ışıkları milimetrik
  parçalarda pürüz yapmasın, gölge atlası zaten dolu).
  K9 çapası prefab'ın içinde (yerel z −0,86), dikey 45°.
- **Sürgüler:** 18 cm'lik yarık, T başlı kol (genel pirinç başlı), ölçek çizgileri 2–2,6 mm geometri, 0/5/10 rakamları, sayaç
  tekerleri (masanın `CounterWheel`'i ve rakam dokusu), ibre değeri yaylı izler (seçenek i), amber lamba. Değer etiketin
  satırında (“%85”); `HoverTag` değişen satırı yeniler.
- **SES anahtarı, dil kademe anahtarı, MENÜYE DÖN kolu:** yarasa kollu anahtar (yukarı AÇIK); kademe anahtarı iki konum (40° /
  −40°); kol `ShiftSwitch` (kapak + kol, 4 s), menüde bu kol kapalı.
- **Klavye:** dolap açıkken Tab/Shift+Tab kontroller arasında, ↑/↓ ve ←/→ odaktakini çevirir; Esc kapatır. Ekranda yazmaz.
- **Menü sahnesi** (`Desk/Scenes/Giris.unity`): sahanlık x 2,2–4,6, z −1,35–1,55; kapı duvarında soldan dolap, kart rafı ve
  puantaj saati, dış interkom, kapı, sağda son şalter (“ANA ŞALTER · MAIN SWITCH” plakası sabit); merdiven yukarı kata
  sürer, tepede soluk soğuk ışık; kapının üstünde “SON ŞALTER” / “CORRIGIBLE”. Kamera pozları M0 (sahanlık, dikey 52°),
  M2 (saat, 40°), dolabın çapası, M3 (eşik, içerisi).
- **Akış:** `GameLaunch` niyeti; devamda kısa dönüş K1'de biter (öneri “K1” diyordu; eski KAYITTAN DEVAM kayıttaki poza
  dönüyordu, o yol DeskGame doğrudan açılınca aynen duruyor). Yeni harekâtta eski kayıt bugünkü gibi ilk komutta üstüne
  yazılır (Karar 12).

- **Ekrandaki AYARLAR düğmeleri kaldırıldı** (kullanıcı, 2026-11-03, dolabın K1'den görünür yere taşınmasından sonra): sağ
  üstteki plaka artık yalnız atlanabilir dizilerde ATLA (`SettingsPlaque.SetSkip`; “ATLA” çeviri tablosuna girdi, İngilizcesi
  SKIP), kenar satırında AYARLAR çipi yok. Dolap odada tıklanarak ya da Esc ile açılır. Fareyle oynayan oyuncu emir belgesi
  açıkken dolaba ulaşamaz (oda kilitli); dili ve sesi oyuna girmeden menüdeki dolapta ayarlar.

**Sapmalar**
- **Kapı ve koridor prefab'a çevrilmedi.** Menünün sahanlığı genişletildiği için koridor ortak kullanılamazdı; kapının iç
  yüzündeki vuruş lambası, ziyaretçi çentiği ve interkom dışarıda yok, dış yüzün çarkı içeride gereksiz. Menüde kapının
  bir kopyası var (`DoorProp` aynı bileşen; dıştaki çarkı döndürür, merdiven ışıkları yerine içeriden sızan sıcak ışığı
  yakar). DeskGame'in kapısına dokunulmadı.
- **Dil gün 0'da dolapta değişirse** oyun (YZ satırları ve ekip adları yeni dilde üretilsin diye) dolap kapanınca yeniden
  kurulur; açıkken kurulsaydı açılış dizisi dolabın önünü karartırdı.
- **Emir belgesi dolap açıkken gizlenir** (ölçek 0); belgenin durumu değişmez.

**Doğrulama sonuçları**
- Derleme temiz; EditMode testleri boş sahnede 489/489 (yeni `SettingsCabinetTests`: sürgünün değer hesabı, prefab parçaları,
  DeskGame bağlantıları ve K9, menü sahnesi ve build sırası). Çeviri tablosu güncellendi, 27 yeni metnin İngilizcesi yazıldı,
  doğrulama temiz. Punto taraması (testte) K9'u kendiliğinden ölçer, ihlal yok.
- Play (DeskGame doğrudan): emir belgesi açıkken AYARLAR → kamera K9, kapak açıldı, belge gizlendi; müzik sürgüsü işaretçi
  olaylarıyla %30'a basıldı ve %62'ye sürüklendi (%60'a oturdu, tercih ve etiket “%60”), tekerlekle %65; SES anahtarı
  sessiz modu açıp kapadı; kademe anahtarı İngilizceye çevirdi (bütün yazılar, teleks “LANGUAGE · ENGLISH”). Kapanınca kamera
  harita pozuna döndü, oyun İngilizce yeniden kuruldu. BAŞLA, bir vardiya, MENÜYE DÖN (kapak + kol) → menü.
- Play (menü): kartta “S.002 · free 0/12”; saat pozu; kayıttan devam → kart saate, kapı açıldı, eşikten geçildi, DeskGame
  kısa dönüşle K1'de açıldı (gün 1, emir belgesi yok, teleks “CONTINUE · S.002”). Yeniden menü: destenin kıskacı ilk basışta
  kalktı, 4 s'de kapandı, iki basışla yeni harekât → DeskGame'de dosyasız emir belgesi, eski kayıt ilk komuta kadar duruyor.
  Menüdeki dolap açıldı (MENÜYE DÖN yok), dil Türkçeye döndü; son şalter (kapak + kol) ışıkları söndürdü ve Play durdu.
  Konsolda hata yok.
- Kayıtlar ve tercihler Play'den önce yedeklenip geri yüklendi; dinamik font varlığı ve geçici görüntüler geri alındı.

**Açık kalanlar**
- Sesler yer tutucu (kodla sentezlendi); sahibin dinlemesi gerekir.
- Menünün ışığı DeskGame'den daha aydınlık ve düz (post-processing kapalı); sahibin gözüyle ayarlanmalı.
- Fare sürüklemesi işaretçi olaylarıyla denendi; gerçek fareyle (imleç yarıktan çıkarken, çift tık) sahibin elinde denenmeli.
- Dolabın örneklerinde TMP'nin gizli `MeshFilter`'ından gelen boş `m_AddedComponents` girdileri (zararsız; GELISTIRME
  › Bilinen tuzaklar).
- Bu işten bağımsız, denerken görülen: gün 0'da dil değişip oyun yeniden kurulunca bölge adları ikinci kez çevrilmeye
  çalışılıyor (konsolda “'en' çevirisi yok: Western Europe” uyarıları; eski ayarlar kartında da aynı yol). (“ATLA”nın eksik
  çevirisi plaka değişirken düzeldi.)

