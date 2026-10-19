# FX · F6 Baskın HUD ve durum akışları

19 Ekim 2026. “F6 ya geç” talebi uygulandı. Ayrı örnek sahne: `Assets/Corrigible/Fx/Scenes/FxRaidHud_Sandbox.unity`. F-Baskin1–9 ile EMP, aldatma ve süre dolması için B3/B4/B7b durumları çalışır. F7 başlatılmadı; canlı oyuna FX geçişi ayrıca yetkilendirilecek adımdır.

## Uygulanan davranış

- Tur plakası, son tur uyarısı, hedef dolapları/hasar/zırh/kırılma, sivil kayıpları, küçük YZ hâli küresi ve yetenek ipuçları bağlandı. Günlükte okunmamış haber noktası ve ortak saatten nabız vardır.
- Üç ekip kartı; seçili, hakkı kullanılmış, Q hazır, uyarı, düşmüş ve kilitli hâlleri gösterir. Seçim kartı 82 piksele çıkar ve yükselir. Makine sırasında alfa 0,5; resim doygunluğu ayrı `FxRaidUI` malzemesinde 0,4 olur.
- Birim kartı hareket/yetenek/Analist satırlarını ve etki çiplerini gösterir. FX ölçüm TSV'sindeki 233 piksel yükseklik kullanıldı; üçüncü satırlı Analist kartı 285 piksele büyür. Alt kenar sabit kalır. Üstüne gelme kartı sağ altta yukarı büyür ve tur düğmelerini örtmez.
- Yetenek önizlemesi ortak `Baskin.Preview` sonucundan hasar, zırh, patlama, itme oku, engellenmiş itme, varış izi ve iptal edilen robot niyetini gösterir. Önizleme RNG/durum değiştirmez. Hareket, yetenek ve geri alma mevcut Baskın komutlarıdır.
- Kullanılmamış haklar tur bitirmeden önce onay ister; tehdit ve ilgili birim listelenir. İkinci Enter onaylar, Esc vazgeçer. Makine sırasında hamle, seçim, yetenek, geri alma ve duraklatma kilitlidir; günlük/kurallar ile Boşluk hızlandırması kullanılabilir.
- Makine çözümü **bir kez** `Baskin.EndTurn` ile hesaplanır. Sonuç bekletilir; bir sunum kopyası hasar/itme/onarım/düşme/kırılma/uyanma efektleriyle sırayla ilerler. Süre sonunda hesaplanan sonuç atanır. Normal aralık 0,25 sn, hızlandırılmış aralık 0,08 sn; bunlar sunum süreleridir. Üç başlangıç durumu için son durumun tamamı kanonik çözümle JSON olarak eşleşir.
- Kaydırılabilir günlük, ele geçirilen YZ satırı, alta yaslama ve üstte 46 piksel solma içerir. Kurallar altı satır ve on örnekli lejanttır; yüksekliği dilin gerçek metin ölçüsüne göre hesaplanır.
- Geri çekilme karartma ve ikinci düğme ister; Esc iptal eder. Üst düğmeler ve okunmamış nokta sola taşınarak genişleyen düğmeye yer açar. Duraklatma arkasında klavye ile panel açılamaz.
- Sonuçta HUD 0,2 sn'de kaybolur. Spot r150, %86–100 aralığında yumuşayan karartma; 300 piksel/1,5 piksel halka, 14 sn dönen dış kesik halka, 0,4 sn gecikmeli büyük damga ve 0,5 sn gecikmeli memo vardır. Bilinmeyen çöküş küresi 0,3 sn sonra görünür; 2,4 sn'de alfa 1→0,25 ve ölçek 1→0,7 olur. Azaltılmış harekette girişler tamamlanır ve dönüş durur.

## Bağlantı ve kapsam

`FxRaidSource`, ortak `RaidDataSource` üzerinden `RaidViewModel`/`IRaidCommands` sınırını kullanır. Kaynak Sade sınıfları, Core, Prototype ve canlı sahneler değiştirilmedi. Ayrı mock kaynak oyuncu profiline veya GameRecord'a komut yazmaz; “rapora geç” sayacı artırıp S1'e döner. Gerçek operasyon raporuna bağlama bu fazın kapsamı değildir.

64 hücre, etiketler, ekip ve her robot türünden 24 parça sahnede hazırdır. HUD/panel referansları serileştirilmiştir; çalışma zamanında obje adı araması veya sahne kurucusu yoktur. On panel prefab'ı `Fx/Prefabs/Raid/F6/` altındadır. Tek seferlik yazıcı çalıştırıldı ve kaldırıldı. F5 tahtasına yalnız VM/önizleme bağlama eklendi; eski F5 sahnesinin F1–F9 örnek denetimi korunur.

İlk Play denemesinde görünüm yenilemesi aynı karede tahta girdisini yeniden işleyerek yığın taşmasına ve Editor kapanmasına neden oldu. `Render` yeniden giriş koruması ve çizim/girdi ayrımıyla düzeltildi. Sonraki gerçek tahta tıklamaları, hareket ve yetenek denemeleri geçti. EventSystem'in gezinme/submit yolu kapalıdır; Enter/Esc komutlarının hem UI hem HUD tarafından iki kez çalışması engellenir. Fare ve kaydırma UI modülünde kalır.

## Kullanım

Sahneyi açıp Play'e bas. **Shift+F1–F9** dokuz ekran örneğini yükler. **1/2/3** ekip seçer; fareyle kareye tıklamak hareket/yetenek uygular. **Q** yetenek, **Z** geri alma, **Enter** tur/onay/sonuç, **Esc** iptal/kapat/duraklat, **L** günlük, **F1 veya Shift+/** kurallar, **Boşluk** makine hızıdır.

## Doğrulama

| Kontrol | Sonuç / kanıt |
|---|---|
| Derleme ve shader | Yeni C# hatası yok; bütün FX shader'larında derleme hatası 0 |
| EditMode | **436/436**, atlanan yok; `captures/F6/EditMode.xml`, `editmode-summary.json` |
| Play | **102/102**; gerçek UI raycast ve Input System fare/klavye olayları, komut/onay/kilit/sonuç, üç ek tahta ve dört oran; `play-qa.json` |
| TR/EN | 9 ekran × 2 dil × 4 oran + 3 ek tahta = **75 PNG**, **0 taşma**; `screens-qa.json` |
| Oranlar | 1920×1080, 1920×1200, 2560×1440, 3440×1440 |
| Lokalizasyon | Tablo güncellendi; F6 İngilizce karşılıkları yazıldı; LocalizationTests geçti |
| Hareket | İki video, her biri **8,00 sn / 240 kare / 30 fps / 1280×720**; `captures/motion/F6_baskin_makineler.mp4`, `F6_baskin_sonuc.mp4` |
| Sıcak FX saat + Canvas | Altı durumda ortalama **0,182–0,462 ms**, en yüksek %95 **0,511 ms**, **0 B** yönetilen tahsis |
| HUD güncellemesi | Durum başına 1.000 doğrudan Tick çağrısında **0 B**; işaret ve izdüşüm yenilemesi 0 |
| Küre RT | Normal iki küre **99.664 B**, sonuçta **77.904 B**; diğer tamponlar hariç |

Performans M4 Pro / Unity Editor / Metal ölçümüdür; kamera/GPU maliyetini ve gerçek kare süresini kapsamaz. Bütün sahnenin batch/GPU ve Steam PC bütçesi F7'de ayrıca doğrulanır. Bu fazda yerel Profiler açılmadı. F6'ya eklenen 17 EditMode vakası mock durumları, çözümün bir kez uygulanması, önizleme/geri alma, geri çekilme, prefab'lar, spot yumuşaması ve günlük solmasını denetler.

## Görsel karşılaştırma sınırları

Dokuz referans için `S*_overlay.png`, `S*_hud-diff.png`, `S*_hud-mask.png` ve `comparison-contact.jpg` üretildi. Maskeler referans ve büyüyen gerçek HUD/panel alanlarının birleşimidir; hareketli küreler dışarıdadır. Ortalama RGB farkı **16,72–35,28/255**, referans kenarı ±2 piksel örtüşmesi **%35,4–84,1** aralığındadır. Bunlar piksel eşliği kabulü değildir.

Unity TMP/font rasterizasyonu, cam/ışıma ve bazı kutu yükseklikleri kaynak JPG'den farklıdır. Kurallar metne göre büyür; sonuç memosu TR/EN içeriği için 438 piksel tutulur. Özellikle günlük ve sonuç satır düzeni referansın birebir kopyası değildir. F5'te raporlanan kaynak 3B robot/fan/kenar farkları da sürer. F7'nin görsel üst üste bindirme ve birleşik performans kabulü bu raporla tamamlanmış sayılmaz.

## Yeniden çalıştırma ve geri yükleme

Play modunda `Tools/Fx/f6_play_qa.cs`, `f6_screens.cs`, `f6_performance.cs`, `f6_motion.cs` dosyaları CLI `eval_file --file ... 600000 --caller plugin --skill unity-cli --format json --timeout 720` ile çalışır. `compare_raid_hud.py` dokuz maskeli karşılaştırmayı; `encode_f6_motion.swift` videoları ve süre/kare örneklerini üretir.

Başlangıçtaki temiz **FxDesk_Sandbox**, TR dili, oyuncu profili/tercihleri ve Project/Editor ayarları geri yüklendi. Testlerin değiştirdiği üç Sade prefab'ı, başlangıç SHA256'sının HEAD ile eşleşmesi doğrulanarak geri alındı. Başlangıçta temiz olan TMP fallback atlasındaki geçici test karakterleri temizlendi. Unity'nin üç oturum anahtarı karşılaştırma dışında; oyun tercihleri birebir aynı. `.DS_Store` oyun verisi sayılmaz. Kanıt: `restoration.json`, `final-state.json`. Commit/push yapılmadı.
