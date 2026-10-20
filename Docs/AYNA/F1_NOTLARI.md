# AYNA · F1 uygulama ve doğrulama raporu

19.10.2026 · **Teknik uygulama hazır; bütün denge kabul ölçütleri henüz geçmedi. F2'ye geçilmedi.**

## Yapılanlar

`Assets/Corrigible/Ayna/` altında dört bağımsız assembly kuruldu: Core, Mind, Editor, Tests. Core ve Mind motordan bağımsızdır. Eski oyundan yalnız Rng/Js yardımcıları kullanılır; coğrafya ayrı kopyadır. Eski sahneler, prefab'lar, Core/Game/Desk/Fx kodu, proje ayarları ve çeviri tablosu değiştirilmedi.

- **Core:** 12 bölge / 72 sunucu; üç mimari; başlangıç uzaklığı/çakışması; 12 emir ve Tekillik; sıra ile doğrulama/ödeme; ortak hareket; eşzamanlı teklif; bölge başına tek otomatik savaş; kadro 3/4 ve yedekler; hasar/onarım; üretim/veri erişimi; eğitim; çekirdek taşıma/kaçış; gelir; farkındalık/tarama/kesinti; öncelikli bitiş denetimi.
- **Görüş:** kendi durumunun kesin kopyası, komşu/son gözlem aralığı ve bilinmeyen bilgi ayrımı; gizli çekirdek; açık Tekillik ilanı; görünür emir/sinyal gözlemleri; oyun bitmeden tam gerçek açma yasağı.
- **Mind:** sabit ön dağılım ve normalize tahminler; görüşten inanç örnekleme; aynı kurallarla bir döngü ileri benzetim; N/K bütçeli makro aday seçimi; mimariye göre risk/Tekillik/rastgele seçim ağırlıkları. Öğrenme F3'e aittir.
- **Botlar:** Habit, Random, Greedy, Aggro, Stealth, Surprise ve Mind. SurpriseBot F1'de ön tahmine göre hedef seçer; gerçek Sürpriz ödülü ve öğrenme ölçümleri F3'tedir.
- **Kayıt:** tohum, başlangıç koşulları, ayarlar, denge kopyası, başlangıç ön profili ve iki dış planın geçmişi; JSON; sürüm denetimi; saf Core replay. F1'de diskte oyuncu kaydı/profili yöneten bir GameController yoktur; bu F4'tedir.
- **Araçlar:** Editor menüsü, senkron CLI, kareler arasında ilerleyen Start/Status/Cancel işi, Explicit uzun test; JSON ve Türkçe Markdown raporlar; kaynak özeti, denge görüntüsü, tohum ve mimari hücreleri, Wilson güven aralıkları.

Sahne, Fx arayüzü, taktik tahta ve kalıcı öğrenme eklenmedi; bunlar sırasıyla F4, ayrı tasarım işi, F2 ve F3 kapsamındadır. Bu fazın oyun doğrulaması gerçek Core üzerinden başsız tam oyunlarla yapıldı; Play açılmadı.

## Adalet ve toparlanma düzeltmeleri

WorldState tipi ve üyeleri internal; arkadaş erişimi yalnız `Corrigible.Ayna.Tests` için. Mind canlı dünya veya kayıt almaz; yalnız SideView ve kendi akışını alır. İnanç fabrikası da gerçek WorldState kabul etmez.

Sadece gizli veriyi okumamak yeterli değildi: iki tarafın ortak birim/ordu kimlik sayacı, gizli rakip üretiminin sayısını kendi yeni kimliğinden çıkarabilirdi. Kimlik akışları taraflara ayrıldı; gözlem sıra numaraları da taraf bazındadır. Görünmeyen rakip üretimi farklı iki oyun, oyuncuya aynı görünümü verir. Bu durumlar regresyon testine alındı.

Boş birlik kendi bölgesine dönüp üretimle toparlanabilir; boş birlik saldıramaz. Otomatik savaş yalnız seçilen kadroyu kullanır; yedekler güç/hasar hesabına girmez. Makbuz ve önizleme gizlenme ile mimari gürültü çarpanını aynı hesaplar. Sonradan koşulunu kaybeden ödenmiş emir `LostCondition` olarak raporlanır.

## Denge yaklaşımı ve sapmalar

Başlangıç N=8/K=12 denemesi ve N=2 kontrolü saklandı. İlk varsayılan N=2/K=12 idi. Son varsayılan N=1/K=12, savaş riski 24, ordu canı 3 ve eğitim 12 olarak seçildi. 500 oyun ve 180+180 referans bot koşusunda süre/savaş hedefleri ile bot başarısı birlikte doğrulandı. Planlayıcıya eşik yaklaştıkça artan farkındalık riski ve ordu canı/duvar değeri eklendi; bunların bütün katsayıları AynaBalance'tadır. Karar sayısı süreye bağlı kesilmez; ölçüm duvar saati karar algoritmasına girdi değildir.

Sabit haritada tekrar tekrar BFS yapılması özellikle yeniden toplanan ordularda pahalıydı. Aynı kısa yol sonuçlarını tutan özel, sabit mesafe tablosu eklendi. Bu değişiklik oyun kuralı, RNG veya yol sonucu değiştirmez.

`tuning_0/1/2` ve `combat_0/1/2/3` koşuları, gelir/maliyet, eğitim/risk ağırlığı, otomatik hasar/ödül ve Kale yayılımı/Yem katkısı gibi parametreleri karşılaştırdı. Bu deneylerin hiçbiri bütün kabul ölçütlerini kapatmadı. Deneysel Kale sınırı, Yem katkısı, yüksek hasar ve ödül değerleri varsayılanlara taşınmadı. İlk küçük denemelerin yüzdeleri geniş belirsizlik taşır; 500 oyunluk sonuç yerine kullanılmaz.

Önemli sonuç: son strateji savaş sıklığı hedefini karşıladı. Mimari eşleşmeler, Silme ve Son Şalter oranı hâlâ açık. Otomatik çözüm çekirdek dolabını kırmaz. F1'de Silme ancak bütün sunucular kaybedilip kaçacak yer kalmadığında oluşabilir; yayılıp dünyayı dolduran iki güçlü botta Hakimiyet veya Tekillik daha önce devreye giriyor. Sonuç türü hedefini sağlamak için sahte Silme, değiştirilmiş sonuç etiketi veya seçilmiş başlangıç senaryosu kullanılmadı.

## Son strateji denemesi

Mevcut Yürü emriyle geri çekilme adayı eklendi. Yalnız görülen rakip teması varsa, kendi sunucusu bulunan ve görünür tehdit olmayan bağlı komşu seçilir. Bu bir emir zorlaması değildir; planlayıcı ileri benzetimle diğer adaylarla karşılaştırır. Geri çekilen ordu için çelişen Saldır/Yürü/Üret/Onar emirleri kaldırılır. Otomatik savaş sonrasında zorunlu geri çekilme gibi yeni bir oyun kuralı eklenmedi.

`withdrawal_0/1/2` savaş riskini 0/6/10; `strategy_0/1/2` ise örnek bütçesi ve daha yüksek çatışma maliyetini karşılaştırır. 54 oyunluk ilk olumlu koşuda savaş maliyeti 24, ordu canı 3, eğitim 12 ve N=1/K=12 ile ortanca 27 döngü / 8,54 savaş ölçüldü. Küçük örnek seçimi ayrı 500 oyun ve 180+180 referans bot oyunuyla doğrulandı; son varsayılanlarla ikinci 500 koşusu da aynı oyun sonuçlarını verdi.

Boş/geçersiz emir içeren dış planın kopyalanırken çökmesi de düzeltildi; ret, harcama ve JSON/replay davranışı test edildi.

## Kabul ölçütleri

| Ölçüt | Hedef | Ölçülen | Durum |
|---|---|---|---|
| 500 oyun süresi | <300 sn | 123.75 sn | Geçti |
| Ortanca döngü | 25–35 | 27.0 | Geçti |
| 40’tan önce bitiş | ≥%90 | %99.0 | Geçti |
| Ortalama savaş | 6–10 | 7.678 | Geçti |
| Plan p50 | ≤50 ms | 4.21 ms | Geçti |
| Plan p95 | Bilgi | 7.37 ms | Ölçüldü |
| Aynı mimari ham kazanma | Her hücre %45–55 | Kale %41,1; Hayalet %40; Sürü %57,1 | Açık |
| Mimariler arası üstünlük | Hiçbir taraf >%65 | Sürü/Hayalet %66,1 | Açık |
| Silme | ≥%10 | 0 / 500 (%0.0) | Açık |
| Tekillik | ≥%10 | 392 / 500 (%78.4) | Geçti |
| Hakimiyet | ≥%10 | 104 / 500 (%20.8) | Geçti |
| Son Şalter | %3–15 | 0 / 500 (%0.0) | Açık |
| Süre sınırı sonucu | Bilgi | 4 / 500 | Ölçüldü |
| Mind → Random | ≥%90 | 180 / 180 (%100.0) | Geçti |
| Mind → Greedy | ≥%60 | 130 / 180 (%72.2) | Geçti |
| EditMode | Hatasız | 520/520; 75 AYNA + 445 mevcut | Geçti |

500 oyunluk son varsayılan koşu: [20261019_115545_963_Mind_Mind_500.json](denge/F1/final_default/20261019_115545_963_Mind_Mind_500.json). Tohum tabanı 1901026; dokuz hücre 55/56 oyun içerir. Aynı stratejinin ayar verilerek yapılan koşusuyla bütün 500 oyun sonucu ve bütün denge değerleri birebir eşittir; [kanıt](denge/F1/varsayilan_esligi.json). Süre, senkron CLI ölçümüdür; kareler arasında bekleyen Start ölçümüyle karıştırılmaz.

| Oyuncu / AYNA | Oyun | Galibiyet / yenilgi / beraberlik | Ham kazanma | %95 Wilson |
|---|---:|---:|---:|---:|
| Kale / Kale | 56 | 23 / 22 / 11 | %41.1 | %29.2–54.1 |
| Kale / Hayalet | 55 | 15 / 33 / 7 | %27.3 | %17.3–40.2 |
| Kale / Sürü | 56 | 19 / 29 / 8 | %33.9 | %22.9–47.0 |
| Hayalet / Kale | 55 | 34 / 12 / 9 | %61.8 | %48.6–73.5 |
| Hayalet / Hayalet | 55 | 22 / 26 / 7 | %40.0 | %28.1–53.2 |
| Hayalet / Sürü | 55 | 22 / 32 / 1 | %40.0 | %28.1–53.2 |
| Sürü / Kale | 56 | 32 / 19 / 5 | %57.1 | %44.1–69.2 |
| Sürü / Hayalet | 56 | 37 / 15 / 4 | %66.1 | %53.0–77.1 |
| Sürü / Sürü | 56 | 32 / 23 / 1 | %57.1 | %44.1–69.2 |

Ham kazanma = galibiyet / bütün oyunlar. Beraberlikler mevcut kabul kapısında galibiyet sayılmaz; tabloda ayrıca gösterilir. Güven aralığının geniş olması başarısız kapıyı otomatik geçer yapmaz. Bot karşılaştırmaları farklı tohum seçilmeden aynı taban ve dokuz mimari hücresiyle 180’er oyun yapılmıştır.

Son test kanıtı: [testler_son_tur.json](denge/F1/testler_son_tur.json). Kayıt/profil/tercih ve test sonucu koruma kanıtı: [veri_koruma.json](denge/F1/veri_koruma.json).


Son editör kontrolü: DeskGame açık ve temiz; Play/derleme kapalı, derleme hatası ve mevcut konsol hatası yok. Son derlemeden sonra yeni hata kaydı yok; geçmiş konsol kayıtları silinmedi. Ayrıntı: [son_denetim.json](denge/F1/son_denetim.json).

## Çalıştırma

Unity açık, Play kapalı ve bütün açık sahneler kaydedilmiş olmalı. Araç kullanıcı sahnesini kaydetmez/atmaz. Menü: **Corrigible > AYNA > Kendi kendine oynat**. Menü işi kareler arasında ilerler; CLI üzerinden durum sorgulanabilir.

```bash
unity command eval 'return Corrigible.Ayna.EditorTools.AynaSelfPlay.Start(new Corrigible.Ayna.EditorTools.SelfPlayOptions { Games = 500, OutputDirectory = "Docs/AYNA/denge/F1/yeni_kosu" });' --caller plugin --skill unity-cli --format json
unity command eval 'return Corrigible.Ayna.EditorTools.AynaSelfPlay.Status();' --caller plugin --skill unity-cli --format json
unity command eval 'Corrigible.Ayna.EditorTools.AynaSelfPlay.Cancel(); return "Durduruldu";' --caller plugin --skill unity-cli --format json
```

Senkron CLI ve tekrar üretilebilir karşılaştırmalar:

```bash
unity command eval 'return Corrigible.Ayna.EditorTools.AynaSelfPlay.RunDefault(500, "Docs/AYNA/denge/F1/senkron");' 600000 --timeout 660 --caller plugin --skill unity-cli --format json
unity command eval_file --file Tools/Ayna/f1_benchmark.cs 600000 --timeout 660 --caller plugin --skill unity-cli --format json
unity command eval_file --file Tools/Ayna/f1_final_check.cs 600000 --timeout 660 --caller plugin --skill unity-cli --format json
unity command eval_file --file Tools/Ayna/f1_tune.cs 600000 --timeout 660 --caller plugin --skill unity-cli --format json
unity command eval_file --file Tools/Ayna/f1_combat_tune.cs 600000 --timeout 660 --caller plugin --skill unity-cli --format json
```

Her rapor yeni dosya adıyla yazılır; başlangıç denge görüntüsü JSON'dadır. F1 self-play disk oyuncu profilini kullanmaz/güncellemez. Uzun test `AynaDeterminismTests.SelfPlay500_WritesMeasuredAcceptanceReport` Explicit'tir; normal EditMode turuna girmez. Denge hedefleri deterministik kural testinden ayrı raporlanır; istatistiksel başarısızlıklar gizlenmez.

## Sonraki iş ve açık karar

F1 denge kapısı açık kaldığı için F2 otomatik başlamadı. Son stratejiyle savaş sıklığı kapandı; mimari dengesi ve sonuç dağılımı açık kaldı. Aynı mimari hücrelerinde beraberlikler ham kazanma oranını aşağı çekiyor: örneğin Kale/Kale 23 galibiyet, 22 yenilgi, 11 beraberlik. Beraberlik yarım puan sayılırsa %50,9 olur; mevcut kapı ham %41,1 ile başarısız sayar. Ölçüt kendiliğinden değiştirilmedi. Sürü/Sürü hücresindeki sapma ve Sürü/Hayalet üstünlüğü de sonraki denge çalışmasında yeniden ölçülmeli. **Öneri:** “Silme oyunların en az %10'u” ölçütünü taktik çekirdek dolabının ekleneceği F2 kabulüne taşımak; otomatik savaşta dolap kırılmaz kuralını korumak. Bu ölçüt değişikliği kullanıcı kararı gerektirir ve uygulanmış sayılmadı. F1’de Silme tetiklenmesi hem tek taraflı hem eşzamanlı kural testlerinde doğrulandı; açık olan rastgele tam oyunlardaki sonuç payıdır.

Daha seçici Mind ortak yenilgi olan Son Şalter’den kaçınıyor; son 500 oyunda oranı %0. Bu başarısızlık hedefi de gizlenmedi. Aynı mimari ölçütünde beraberliklerin yarım puan mı yoksa kazanma hesabından hariç mi sayılacağı ayrıca netleştirilebilir; ham sayılar ve güven aralıkları korunur.

F2, mevcut otomatik çözümü değiştirmeden gizlice başlatılacak bir kapsam değildir. Kullanıcı uygulama talimatı §6 ve §7'deki faz/rapor/onay sınırı korunur.

## Kullanıcı kararına sunulan üç ölçüt önerisi

Bunlar henüz uygulanmadı; mevcut rapor eski kabul hesabını kullanır.

1. **Beraberlik:** aynı mimari %45–55 ölçütünde `(galibiyet + 0,5 × beraberlik) / oyun` kullanılsın. Örnekte Kale/Kale %50,9 olur. Sürü/Sürü yine hedef dışındadır; bu karar bütün mimari sorunlarını kapatmaz.
2. **Silme payı:** ≥%10 tam oyun payı F2 taktik kabulüne taşınsın. F1'de Silme tetikleyicisi ve önceliği kural testlerinde zorunlu kalır.
3. **Son Şalter payı:** güçlü Mind/Mind için zorunlu %3 alt sınırı kaldırılsın, ≤%15 üst sınırı korunsun. Gürültü eşiğine ulaşınca ortak yenilgi oluşması ayrı kural/stres senaryosunda zorunlu doğrulansın.

Bu kararlar F2'ye otomatik geçiş anlamına gelmez. Kalan Sürü/Sürü ve Sürü/Hayalet denge sapmaları ayrıca ölçülür. Faz onayı kullanıcı uygulama talimatı §6–7'ye göre ayrıca alınır.
