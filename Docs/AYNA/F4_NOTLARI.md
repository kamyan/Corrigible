# AYNA · F4 ve F3 denge düzeltmeleri

20.10.2026 · Kullanıcı iki denge sorununu çözmeyi ve F4’e başlamayı onayladı. F3’ün bütün kapıları son kaynakta geçti; F4 uygulandı ve canlı denendi. F5 başlamadı.

## Değişen oyun davranışı

Sürpriz, en pahalı **anlamlı bölgesel emir kümesine** bağlanır. Eş bedelli emirler aynı tür içinde hedef olasılıkları toplanarak değerlendirilir; farklı türler varsa en yüksek grup olasılığı geçerlidir. Dolayısıyla aynı planın kuyruk sırasını değiştirmek, ucuz yan emir eklemek veya dolu bölgeye sonuçsuz teklif vermek ödül üretmez. Her ana türde en az 2 ağırlıklı doğrulanmış gözlem desteği gerekir. Oyun kuralındaki koşullu olasılık eşiği %15’ten **%20’ye** kalibre edildi; kabul hedefleri değişmedi. Ödül ana bedelin %25’i, en çok 3 İşlem. Tek bir ana hedef bölgesi varsa ilk vuruş yalnız orada kullanılabilir; birden fazla ana bölge varsa taktik bölge uydurulmaz.

Tahmine tepki veren altıncı hipotez ve yayımlanan tahmine karşı ikinci planlama senaryosu eklendi. Mind yalnız görünür/doğrulanmış verilerle öğrenir. Makro planlayıcı öğrenilmiş **şimdiki** inancı kullanır; gelecek projeksiyonunu bugüne karıştırmaz. Görünür rakip kontrolünün azaltılması ve tam ödüllü taktik savaş karar değerine girer; gerçek otomatik çözüm %60 ödüllü kalır. Konum inancı yeterliyse çekirdeğe yaklaşan rota da adaydır; ara adımların değeri ScoreApproach=18 / ApproachHorizon=6 ile düzenlenir. Soğuk ve Kapalı model bu öğrenilmiş hedefi kullanmaz; hedef sorgusu RNG harcamaz.

Greedy ve Surprise aynı üretim/eğitim/savaş araçlarına sahiptir. Greedy, o emirle alınabilecek sunucu/veri getirisini; Surprise, yayımlanan modeldeki düşük olasılığı hedefler. **Referans Surprise stratejisi değişti:** tek ana genişleme yapar ve bu odağı bozacak eş/daha pahalı bölgesel yan emirlerden vazgeçer. Bu gerçek ekonomik/askerî fırsat maliyetidir. Eski botla kazanma yüzdeleri doğrudan aynı strateji karşılaştırması sayılmaz; önceki eşit olmayan bot karşılaştırması da tarihsel veride korunur. Oyun kuralının sıra bağımsızlığı ayrı regresyon testleriyle doğrulanır. Son geniş ölçüm küçük kontrollerin yerine geçer.

Profil v3 eski v1/v2’yi taşır; beş politika sayacı altıya genişler. Kayıt formatı 4, kurallar ve akıl sürümü **6**, Sürpriz politikası **4**. Eski politika 0–3 kayıtları kendi kurallarıyla açılır. Replay başlangıç profilini de taşır; ikinci nesil bir kaydı tekrar kaydetmek profil görüntüsünü kaybetmez.

## F4 dosyaları ve akış

| Katman | Dosyalar / davranış |
|---|---|
| Game | AynaGameController: başlangıç → planla → aç → çatışma/kadro → tahta → sonuç → bitiş; kamu komutları kayıt üzerinden gider |
| Kayıt | AynaSaveStore: atomik JSON; kuyruk, taslak niyetler, aşama, savaş sonucu ve tahmin karnesi; profil deposunda nesil/hafıza kimliği koruması |
| Presentation | AynaViewModels: A1–A12 DTO’ları yalnız SideView(Player)/BoardView(Player) ile üretilir; gizli çekirdek/ordu/sunucu gerçeği alınmaz |
| Settings | AynaBalance.asset, AynaDebugTheme.asset ve AYNA’ya ait statik AynaDebugFont.asset |
| Scene | Assets/Corrigible/Ayna/Scenes/AynaDebug.unity; uGUI + TMP, Input System; SerializeField referansları, kalıcı kurucu yok |
| Örnek veri | Core’daki Editor-only AynaDesignFixture, Editor’de AynaExampleFixture; Docs/AYNA/vm_ornek/A1–A12.json |

Debug sahnesi görsel Fx uygulaması değildir. Sahne dosyası asıl kaynaktır; sanat/ses/Fx kapsam dışıdır. Başlangıçta mimari, bölge, öğrenme ve tohum seçilir. Planlama ekranında 12 bölge, kesin/tahmini/bilinmeyen rakip bilgisi, kaynaklar, emir kuyruğu ve top-3 bulunur. Genel bakış, Gelişim, Birlikler, Model ve Günlük sekmeleri vardır. Tekillik ilanı görünür veri hedefleriyle bildirilir.

Komuta savaşında kadro seçilir. Tahtada birim/eylem/hedef seçilip kareye tıklanır; bu yalnız önizler. **Hamleyi uygula** gerçek komutu verir. Geri al, turu bitir ve geri çekil çalışır. Niyet tarafı oyuncuya geçtiğinde aynı kontrol taslak niyet kuyruğunu kurar. Oyun sonu tahmin karnesi, nesil ve Yeni oyun düğmesini gösterir. Hafızayı silme ikinci onay ister.

Örnek veri kurala göre düzeltildi: ortak bölgeler kesin görünür; Güney Asya da kesindir. Hayalet planı 19 İşlem / 2 gürültüdür. Yürü anlık olduğundan sahte bir yol süresi üretilmez. A3–A5 gerçek plan çözümünden, A7 oynatılmış tahta sonucundan; A11/A12 tarihli fixture durumlarından üretilir. Fixture canlı kayıt/devam testi değildir.

## Doğrulama

**611/611 test geçti:** 166 AYNA + eski oyunun 445 testi. Son tam koşu 20.10.2026 07:32:21–07:32:50 UTC, 28,31 sn. [Makine sonucu](F4/testler_son_tur.json). Kaynak: `6f474a238235bf1b17a4f49e2116c874671c219686e1b5d66fddab6a65a2bfcf`. CLI’nin 30 sn istek zaman aşımı test çalışmasını durdurmadı; sonuç tamamlanan TestResults.xml üzerinden doğrulandı.

Canlı testler **Temp/AynaF4Play** altında yapıldı; gerçek oyuncu kaydı/profili kullanılmadı.

| Senaryo | Sonuç / kanıt |
|---|---|
| Menü dropdown | Tam varsayılanı 2; gerçek Kapalı tıklaması 0; tekrar Tam 2 ([JSON](F4/menu_testi.json)) |
| Kuyruk/kaydet/temizle/yükle | 1 emir geri geldi; oyun+akıl görüntüsü birebir aynı ([JSON](F4/canli_kayit_testi.json)) |
| Tahta | Önizleme saf; hareket uygulandı; geri al konumu getirdi; savaş ortası yükleme birebir ([JSON](F4/canli_tahta_testi.json)) |
| Baştan sona oyun | 8. döngüde Hakimiyet; profil bir kez nesil 1’e yazıldı ([JSON](F4/canli_oyun_sonu.json)) |
| Sonraki oyun | Menü AYNA v2 / 1 oyun; ikinci nesil iki kez kaydet/yükle birebir ([JSON](F4/ikinci_nesil_kayit_testi.json)) |
| TR/EN | Aynı oturumda iki dil çalıştı; çeviri güncellemesinde eksik 0 ([JSON](F4/lokalizasyon.json)) |

### Ekranlar

[Başlangıç TR](F4/01_baslangic_tr.png) · [Başlangıç EN](F4/02_baslangic_en.png) · [Planlama EN](F4/03_planlama_en.png) · [Planlama TR](F4/04_planlama_tr.png) · [AÇ](F4/05_ac_evresi.png) · [Kadro](F4/06_kadro.png) · [Tahta](F4/07_tahta.png) · [Oyun sonu](F4/08_oyun_sonu.png) · [Yeni nesil](F4/09_yeni_nesil.png) · [Son kaynak oyun sonu](F4/10_son_kontrol.png)

İlk küçük kontrolde iki destek Habit oranını %5 üzerinde gösterdi; geniş 500 oyunluk kontrolde %4,15 ile hedefi geçti. 2,5 destekli aday Surprise oranında %29,75 ile kabulü kaçırdı. Son değer iki ağırlıklı doğrulanmış gözlem desteğidir. Ayrı 20 serilik kontrolde Habit %4,38 / Surprise %37,08; küçük tanı tek başına kabul sayılmadı.

## Denge kabulü

Son kaynak sürümünde altı kohort × 100 seri × 5 oyun = **3.000 oyun** tamamlandı. Kabul hedeflerinin tamamı geçti; [özet ve ham rapor bağlantıları](F4/f3_kabul_ozet.md).

| Ölçüt | Hedef | Sonuç |
|---|---:|---:|
| Habit Sürprizi (bütün beş oyun) | <%5 | **%4,15** |
| SurpriseBot Sürprizi | ≥%30 | **%37,57** |
| Surprise / Greedy oyuncu kazanması | Surprise ≤ Greedy | **%16,4 / %67,2** |
| Habit AYNA kazanma artışı | ≥15 puan | **%21→%62, +41 puan** |
| Habit top-1 (oyun 3–5) | ≥%60 | **%97,78** |
| Random kazanma farkı | ±5 puan | **0 puan** |
| Tahmin+makro p50 | ≤50 ms | **4,71–6,19 ms (Mono)** |

Surprise−Greedy farkı −50,8 puan; seri kümeli %95 aralığı −56,4…−45,4. Habit artışı +41 puan (%95: +29…+53). Kapalı Habit kontrolü %14→%34 (+20 puan); Tam’ın beşinci oyunu Kapalı’dan 28 puan yüksek. Tohumlar beş oyunda değiştiği için ham artışın tamamı öğrenmeye yüklenmez. Random Tam/Kapalı %100 AYNA kazanmasıyla tavanda; bu kontrol rastgele insan stratejilerine genellenmez.

Ana 3.000 oyun, Unity’nin derlediği aynı saf Core/Mind DLL’lerinin mevcut Mono 6.12 çalışma zamanındaki kopyasıyla koşuldu. Sürücü, Editor ölçümünün Steps yöntemini değiştirmeden çıkarır; sahne/profil/Unity API erişimi yoktur. [Tekrar hazırlanabilir sürücü](../../Tools/Ayna/prepare_learning_mono.py), [DLL/adım özetleri](F4/son_surucu.json). Yeni paket kurulmadı. Mono süreleri Unity süreleri olarak sunulmaz.

Ayrı seri ofseti 100–119’da 100’er oyunluk kontrolde Habit Sürprizi %4,38, Surprise %37,08; Surprise/Greedy kazanması %20/%67. Bu ek kontrol ana 100 seri paydasına katılmaz. [Ham ek kontroller](denge/F3/e2_holdout/). Aynı mantık son varsayılan değerle tekrar ana kohortta doğrulandı. Sürpriz referansı eski politikadan farklı olduğundan sonuç bütün insan stillerinin dengesi anlamına gelmez.

Son kaynak sürümünün ilk **50 Habit oyunu** ayrıca Unity Editor’de tekrar oynatıldı. Bütün oyun alanları Mono’nun aynı tohumlu ilk 50 oyunu ile **birebir aynı**. Unity tahmin+makro **p50 37,61 ms**, p95 67,92 ms; 50 oyun 77,86 sn. ≤50 ms ortanca kapısı Unity’de de geçti; p95 ayrı raporlandı. [Karşılaştırma kanıtı](F4/unity_mono_esitligi.json).

Küçük ön kontroller kabul değildir. Kısmi durdurulan işler ve başarısız aday raporları silinmedi. Eski F3 ilk teslim sonucu [tarihsel özette](denge/F3/baseline_20261020/OZET.md) korunur.

Son kaynakta gerçek düğmelerle hafıza onayı açıldı; Yeni oyun bu onayı iptal edip düğmeyi gizledi. Kaydet/temizle/yükle tam görüntüyü geri getirdi ([kanıt](F4/son_canli_kontrol.json)). Oyun 8. döngüde AYNA Hakimiyetiyle bitti ve profil bir kez nesil 1’e yazıldı ([son oyun](F4/son_canli_oyun_sonu.json)). Son canlı oturumda yeni konsol hata/uyarısı yok; sahne Edit modunda temiz ve eksik scriptsiz bırakıldı.

## Veri koruma ve sınırlar

Oyun kayıtları, kampanya ve her iki oyunun profilleri içerik ve dosya zamanı bakımından korundu ([son denetim](F4/veri_koruma_son.json)). Yalnız üretilen TestResults.xml hash/zaman eşleşmesiyle geri alındı. Unity’nin dört oturum/performance meta anahtarı güncellendi; oyun tercihleri değişmedi, plist geri yüklenmedi. Eski altı font/prefab’ın yalnız kendi otomatik serileştirme farkları kontrollü geri alındı ([asset denetimi](F4/eski_asset_koruma.json)).

Eski oyun kodu ve Project/Player/Quality/URP ayarları değiştirilmedi. Yeni paket kurulmadı; commit/push/dal işlemi yok. Eski dinamik font/prefab serileştirme yan etkileri ayrı denetlendi; yeni sahne kendi fontunu kullanır. F1/F2’den taşınan genel mimari/rol dengesi bu iki F3 kapısından ayrı değerlendirilir.

F4 sonrasında F5 bütün mimariler/botlar için genel denge turudur; bu faz için onay verilmedi ve başlanmadı.
