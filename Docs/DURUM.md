# Durum, kararlar ve açık işler

_Son güncelleme: 2026-10-13_

## Nerede kaldık

Oyun baştan sona oynanabilir: giriş ve rakip seçimi, rehberli eğitim, kriz masası (harita, bölge eylemleri, araştırma,
YZ günlüğü, haber bandı), hediye/perde/sahte zafer/son pencereleri, kayıt ve devam, Seni Tanıyan Rakip profili.
112/112 EditMode testi geçiyor (oyun + analist masası); C# simülasyonu tarayıcı prototipiyle birebir aynı (deneysel mod ve baskın motoru dahil).
Deneysel mod (saha ekipleri, zaman ölçeği, baskın) Unity'de oynanabilir ve varsayılan olarak açık. Oyun Türkçe ve
İngilizce (giriş penceresindeki dil düğmesi; ilk açılışta sistem diline göre).
Baskın sunumu `Docs/SonSalter_Baskin_Paketi` referansına taşınıyor: ayrı `Baskin` sahnesindeki operasyon maketi ve
uGUI panelleri kalıcı prefab, malzeme ve doku asset'lerinde tutuluyor. Bu aktarımın son derleme, oyun ve görsel
doğrulaması tamamlanınca sonuçları aşağıya eklenecek; yukarıdaki 111/111 önceki doğrulama sonucudur.

**Git:** Depo `git@github.com:kamyan/Corrigible.git`, dal `main`. Bu belgelerdeki işin büyük kısmı **henüz commit
edilmedi** (kullanıcı commit istemedi). Commit'i kullanıcı isteyince yap.

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
  (`GameRecord.version`).
- **uGUI, UI Toolkit değil.** Kullanıcı UI'yı Scene görünümünde görmek ve Canvas ile çalışmak istedi.
- **Kalıcı sahne kurucusu yok.** Sahne ve prefab'lar elle düzenlenebilsin; koddan değişiklikler tek seferlik script'lerle.
- **Acil durum fonu, gelir payı değil.** Kullanıcı "araştırma bütçesi" seçti. Gelirin %75'i sınırı denendi: Hizalama
  çok yavaşladı, usta botun kazanma oranı %86–95'ten %2–11'e düştü. Rezerv kuralına geçildi, varsayılan 0 (denge ve parite
  korunur). Rezerv 15/30 da botlarda kazanmayı düşürüyor (botlar biriken parayı harcamıyor), bu yüzden ayar oyuncuya
  bırakılmış bir takas. Değer Kilidi'nin (4,34/gün) gelirden (~3,4/gün) pahalı olması bilinçli bir son düzlük gerilimi;
  fon göstergesi ("+3,5/gün · araştırma −4,3") ve "Araştırma bütün fonu yutuyor" ipucu bunu görünür kılar.
- **Eğitimde kaynak gerektiren adımlar zamanı durdurmaz**, yoksa fonu yetmeyen oyuncu sonsuz duraklamada kalır.
- **Gözlem noktası adımı:** YZ izlenmeyen bölgede tespit edilirse Hizalama "gözlem yok" ile hiç ilerlemiyordu; yeni
  oyuncuların takıldığı yer. Eğitime bunun için adım eklendi.
- **Yeni oyuncular için varsayılan rakip Solucan** (PlayerPrefs `corrigible.aiType` yoksa).

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
- **Kayıt sürümleme:** kural değişince eski kayıtlar farklı oynanır; `GameRecord.version` kontrolü eklenmedi.
- **Çözünürlük:** 1920×1080 referans. Analist masasında F7 oran kontrolleri yapıldı; yeni baskın sunumunun oran
  kontrolü bekliyor. Baskında yan sütunlar referans Canvas uzayında 376 px, görüş alanı aralarında gerilir; eski Main
  arayüzünün 400 px yan paneli ayrı yerleşimdir.
- **Denge:** Acemi bot Manipülatör/Kâhin'e karşı %5–8 kazanıyor; eğitim sonrası gerçek oyuncu testi gerekli.
- **Test eksikleri:** PlayMode/UI testi yok; UI davranışı ajanlar tarafından `CorrigibleDebug` ile elle doğrulanıyor.
