# Durum, kararlar ve açık işler

_Son güncelleme: 2026-10-10_

## Nerede kaldık

Oyun baştan sona oynanabilir: giriş ve rakip seçimi, rehberli eğitim, kriz masası (harita, bölge eylemleri, araştırma,
YZ günlüğü, haber bandı), hediye/perde/sahte zafer/son pencereleri, kayıt ve devam, Seni Tanıyan Rakip profili.
23/23 EditMode testi geçiyor; C# simülasyonu tarayıcı prototipiyle birebir aynı (deneysel mod ve baskın motoru dahil).
Deneysel mod (saha ekipleri, zaman ölçeği, baskın) Unity'de oynanabilir ve varsayılan olarak açık.

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
   bölümü, haritada ekip işaretleri, `Baskin` canvas'ındaki taktik ekran, eğitimde "Ekibini gönder" adımı.

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
angarya mı yoksa ilginç karar mı, zaman ölçeği 1,5 mi 2 mi. Baskın ayrı sahne değil, Main sahnesinde ayrı bir canvas (`Baskin`, sıra 12); strateji
katmanı `S.Raid` varken durur.

## Önemli kararlar ve gerekçeleri

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

- **Steam:** Steamworks.NET paketi kurulmadı (indirme kullanıcı onayı ister), Steam App ID yok, `steam_appid.txt` yok.
  `Corrigible.Steam` paket kurulunca derlenir; derlenip denenmedi. Steamworks'te başarımlar (`Achievements`) ve Auto-Cloud
  (`persistentDataPath`) tanımlanmalı.
- **Baskın çözümlemesinde sıra:** `ResolveRaid` başarıda önce `Detect(r)`, sonra çekirdek kontrolü yapıyor (JS'te de
  aynı). YZ henüz fark edilmemişse `Detect` çekirdeği kaçırır ve baskın ekranı "çekirdek yok edildi" derken strateji
  tarafı yalnızca sunucu kümesini kırar. Normal oyunda erişilemiyor (baskın, doğrulanmış bölge ya da bilinen çekirdek
  ister; ikisi de YZ'nin fark edilmiş olmasını gerektirir) ama kural değişirse önce çekirdeği kontrol et (iki tarafta).
- **Baskın ekranı yalnızca fare/klavye için;** kare boyutu 1080p'ye göre sabit (110 px). Farklı en-boy oranlarında
  kontrol edilmeli.
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
- **Çözünürlük:** 1920×1080 referans; 16:10 ve ultra geniş ekranlarda denenmedi; yan panel 400 px sabit.
- **Denge:** Acemi bot Manipülatör/Kâhin'e karşı %5–8 kazanıyor; eğitim sonrası gerçek oyuncu testi gerekli.
- **Test eksikleri:** PlayMode/UI testi yok; UI davranışı ajanlar tarafından `CorrigibleDebug` ile elle doğrulanıyor.
