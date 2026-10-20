# AYNA · F3 uygulama ve doğrulama raporu

> Bu rapor ilk F3 tesliminin tarihsel sonucudur. Kullanıcı daha sonra iki denge sorununu gidermeyi ve F4’e başlamayı onayladı; güncel sonuç [F4 raporundadır](F4_NOTLARI.md).

20.10.2026 · **F3 uygulaması ve 3.000 oyunluk ana doğrulama tamamlandı. İki denge kapısı geçmedi; F3 tam kabul almış sayılmaz. F4 başlamadı.**

F3 geçişi kullanıcı tarafından onaylandı. Eski oyun kodu/sahneleri/prefab'ları ve kullanıcının yeni kaydı korunur. Ortak çeviri tablosunda yalnız AYNA için 18 yeni Türkçe/İngilizce satır eklendi; önceki satırlar aynı kaldı.

## Yapılanlar

| Alan | Uygulama |
|---|---|
| Core/AynaLearningData.cs | Profil v2; sürüm 1 geçişi; istatistik ve oturum DTO'ları |
| Core/AynaForecasts.cs | Planlardan önce kilitlenen dağılım; Sürpriz; son beş döngü Okunabilirliği; Dil 3 görüntü gürültüsü |
| Core/BoardLearning.cs | Yalnız tamamlanmış savaşlardan görünür hamle/kadro; dönüşüm ve konuş köşesi normalizasyonu |
| Core/AynaRecord.cs | Format/kurallar 3, Mind 3; forecast komutu; eski otomatik/taktik kayıt desteği |
| Mind/PlayerModel.cs | Görünür/doğrulanmış gözlemlerden kategorik sayaçlar ve döngü bağlamı, 0,65/0,35 profil birleşimi, akıl kaydı |
| Mind/MindSession.cs | Ön dağılım ve profile göre mimari/başlangıç karşı seçimi; en fazla iki açılış karşı hamlesi |
| Mind/MacroPlanner.cs | Tahmin edilen yayılım hedefleriyle inanç planı; öğrenilmiş hedefe karşı plan adayı |
| Mind/BoardPlanner.cs | Öğrenilmiş ısıyla muhtemel cevap ve tuzak yerleşimi |
| Mind/LearningMatch.cs | Saf Core + Mind oturumu; iki modelle simetrik ödül; oyun/savaş ortasında kaydet/devam |
| Mind/ForecastText.cs | En güçlü üç tahmin ve Sürpriz açıklaması; Loc üzerinden TR/EN |
| Game/AynaProfileStore.cs | Atomik JSON, nesil/hafıza kimliği karşılaştırması, hafızayı silme; Kapalı'da dosyaya erişmeme |
| Editor/AynaLearningBench.cs / Suite.cs | İzole profillerle beş oyunluk seriler, kontrol koşuları ve ham JSON/Markdown |
| Tests/EditMode/LearningTests.cs | Profil/ödül/gizlilik/kayıt/undo/öğrenme düzeyleri ve dosya güvenliği |

## Davranış

Sayaç modelinin yanında beş genel politika hipotezi, yalnız SideView'dan oluşturulan dünyalarda bir döngü ileri çalışır. Politika adı/kimliği test botundan alınmaz. Hipotez eşleşmeleri yalnız doğrulanmış gerçek gözlemlerle güncellenir; gizli hamle yokluğu kanıt sayılmaz. Tahmini gelir/bütçe, eğitim ve ordu durumu oyun içinde taşınır; kesin güncel görüş ve oynanan savaşın görünür can bilgisiyle düzeltilir. Boş ordu ortadan kalkmış sayılmaz; yeniden üretim/eve dönüş mümkündür. Akıl sürümü 3, MindCheckpoint sürümü 2; eski sürüm 1 sayaç modeliyle sürer.

Tahmin tür×hedef ortak dağılımıdır; toplamı 1'dir. S2 kararı uyarınca Sürpriz, gerçek emrin türü içindeki **koşullu hedef olasılığından** hesaplanır. Haritadaki komutlar önce doğrulanıp bedelleri ödenir; yalnız geçerli, Bekle dışı emirler değerlendirilir. En küçük olasılık %15'in altındaysa +3 İşlem yalnız bir kez verilir. Ödül o döngünün emirlerini finanse edemez. Ödül gizli emre verilebilir ama Mind o gizli emri öğrenmez.

İlk vuruş hakkı ilk savaşta tüketilir; otomatik seçim de hakkı tüketir. İki model de ödül aldıysa ek ilk vuruşlar birbirini götürür. Tek kazanan cevap rolünü alır. Gerçek oyuncu oyununda yalnız AYNA model kurar; iki Mind kullanılan oturumda iki tarafa da model ve ödül açıktır.

Öğrenme Kapalı: yalnız genel ön dağılım, dış profil okunmaz/yazılmaz. Hafif: oyunlar arası katsayı 0,175, karşı hamle en çok bir. Tam: 0,35 ve en çok iki. Oyun sonu debrief'te açılan mimari/başlangıç ile kadrolar öğrenilebilir; oyun sırasında bunların gizli gerçeği okunmaz. Başlangıç karşı hamleleri ayrı ücret ödülü değil, brifte izin verilen en fazla bir duvar ve bir kademe-1 aile açılışıdır; üretimde canlı veri şartı sürer.

Hafızayı silme yeni hafıza kimliği oluşturur. Eski oturumun sonucu veya iki kez tamamlanmış kayıt, yeni/silinmiş profili ezemez. Replay Core girdilerini çalıştırır; profil birleşimi çalıştırmaz. LearningSave ayrıca Mind RNG'sini, gözlem imlecini, oyun istatistiklerini ve başlangıç profilini saklar. Sonraki aynı komutta aynı AI planı üretilir.

Tahta izi undo ile geri alınır; önizleme/inanç kopyası yeni öğrenme izi üretmez. Görüşe ancak CompleteBoard sonrasında girer. Doğrulanmamış sahte yığınaklar gerçek saldırı sayaçlarına katılmaz; blöf sıklığı ayrı sayılır. Dil 3 yalnız yayınlanan dağılım kopyasını değiştirir, gerçek ödül/RNG etkilenmez.

## Kabul ölçümleri

Son kaynak sürümünde **6 kohort × 100 seri × 5 oyun = 3.000 taktik oyun** tamamlandı; çalışma hatası yok. Dört Tam öğrenme kohortu ve Habit/Random Kapalı kontrolleri aynı kaynak hash’inde, izole profillerle çalıştı. [Tarihsel tam tablo](denge/F3/baseline_20261020/OZET.md), [tarihsel makine özeti](denge/F3/baseline_20261020/ozet.json).

| Ölçüt | Hedef | Sonuç | Kabul |
|---|---:|---:|---|
| Habit AYNA kazanma artışı (1→5) | ≥15 puan | %30→%68, **+38 puan** | Geçti |
| Habit top-1 (3–5) | ≥%60 | **%97,20** | Geçti |
| Habit Sürpriz (1–5) | <%5 | **%18,57** | Açık |
| Random AYNA kazanma farkı | ±5 puan | **0 puan** (%100→%100) | Geçti; tavan etkisi var |
| SurpriseBot Sürpriz | ≥%30 | **%73,59** | Geçti |
| SurpriseBot oyuncu kazanması ≤Greedy | ≤Greedy | **%92,6 / %22,2** (+70,4 puan) | Açık |
| Tahmin + makro p50 | ≤50 ms | **4,43–6,04 ms** (altı kohort) | Geçti |

Habit artışının seri kümeli %95 bootstrap aralığı +26…+49 puan. Kapalı kontrolde Habit %26→%31 (+5 puan); beşinci oyunda Tam %68, Kapalı %31. Habit Sürprizi yalnız 3–5. oyunlarla hesaplansa da %12,05; bu değer <%5 hedefine geçirilmez.

Top-1, planın en güçlü tek tür×hedef tahmininin **en az bir** kabul edilmiş emre denk gelmesidir. Sürpriz, bütün kabul edilmiş emirler içindeki **en küçük koşullu hedef olasılığından** gelir. Dolayısıyla %97 top-1 ile %18,57 Sürpriz birlikte mümkündür; ikinci/üçüncü beklenmeyen emir ayrıca ödül verebilir.

Önceki kaynakların başarısız/kısmi ham raporları silinmedi ve son tabloya katılmadı. İlk 100×5 Habit’te kazanma +30 puan, top-1 %53,10 ve Sürpriz %81,30 idi. Karma kadroda geçersiz niyet hatası aynı 89. seri yeniden oynatılarak doğrulandı; son 3.000 oyunda tekrar etmedi. Dolu bölgeye ücretli Yayıl teklifini yanlış eleme ve yanlış tarafın tepkisini sayma düzeltmeleri K78/K79’dadır. Önceki 72/500 kısmi iş kabul değildir.

## Ölçüm yöntemi ve sınırlar

İlk geniş koşu, bilinen imkânsız hedeflere olasılık ayrılması düzeltilirken 138/500 oyun ilerlemesinde durduruldu. Bu kısmi iş kabul verisi değildir; [durdurma durumu](denge/F3/ilk_kosu_durdurma.json) korundu. Son sürümde yalnız görünümden kesin imkânsız olanlar elenir; bilinmeyen hedefler ve dolu bölgeye ücretli genişleme teklifleri korunur. İkinci kısmi iş de ilk hedef türünün hamle anında kaydedilmesi düzeltmesi için 84/500 oyun ilerlemesinde durduruldu; [durdurma kaydı](denge/F3/ikinci_kosu_durdurma.json) saklandı. Bu iki kısmi işin kazanma sonucu kabul verisine eklenmez.

Seri başında boş izole profil; beş oyunun sonunda profil taşınır. Habit sabit mimari/başlangıç kullanır, Random her oyunda bunları yeniden rastgele seçer. Dünya/karar tohumları oyunlar arasında farklıdır. Öğrenmesiz kontrol eş tohumlu koşulur. Top-1 isabeti en güçlü tür×hedef tahmininin en az bir kabul edilmiş emre denk gelmesidir; payda geçerli Bekle dışı emri olan döngülerdir. Sürpriz paydası da bu döngülerdir. Her oyun bütün sayaçlarla JSON’da saklanır. Tanı dosyalarındaki gerçek oyuncu bütçesi/sunucuları yalnız sentetik test botunun hakem karşılaştırmasıdır; Mind girdisine verilmez ve canlı oyuncu verisi içermez.

Sürpriz ölçütü, soğuk ilk oyunlar dahil bütün beş oyun üzerinden değerlendirilir. Sonraki oyunların oranı ayrıca gösterilir; brifin hedefi sessizce son oyunlara taşınmaz. Gizli emirlerden öğrenme veya bot kimliğinden gerçek hamleyi okuma yapılmaz. F2'den taşınan makro mimari/rol sapmaları genel denge işidir.

## Doğrulama / veri

**590/590 EditMode testi geçti:** 145 AYNA + mevcut oyunun 445 testi. Son koşu 19.10.2026 19:39:08–19:39:33 UTC, 25,52 sn. [Makine sonucu](denge/F3/testler_son_tur.json). Kaynak hash’i: `451303e16672264bb3e1007e672397b9fcb6461779189b88b4d256e544fba7f1`. Unity çeviri tablosu güncellemesi: 2.195 metin, 0 yeni/eksik/kullanılmayan; mevcut tablo düzeni korundu. [Yerelleştirme kontrolü](denge/F3/lokalizasyon.json).

Ölçümler oyuncunun persistentDataPath profilini kullanmaz; yalnız Docs/AYNA/denge/F3 ve testlerin kendi geçici dizinleri yazılır. F3 öncesi kayıt/profil/tercih yedeği alındı. TestResults.xml yalnız testin bıraktığı hash/zamanla eşleşerek geri alındı. Kullanıcının daha yeni kaydı asla geri alınmaz. [Son salt okunur veri denetimi](denge/F3/veri_koruma_son.json): yedi izlenen dosyanın içerik ve zamanları F3 başlangıcıyla aynı. [Editor](denge/F3/editor_son_durum.json): Play/derleme kapalı, DeskGame temiz; Core/Mind UnityEngine referansı taşımıyor. [Konsol](denge/F3/konsol_son_durum.json): derleme başarısızlığı ve yeni hata yok.

## Çalıştırma

Açık sahneler temiz, Play kapalı olmalıdır. Uzun iş Editor kareleri arasında dört oyunluk paketlerle ilerler; durum çağrısında 60 sn üst sınırı, bir paketin bitmesini beklemeye yeterlidir. Oyuncu Play'e girer veya sahne kirlenirse araç güvenle durur.

```bash
unity command eval_file --file Tools/Ayna/f3_start.cs 60000 --timeout 90 --caller plugin --skill unity-cli --format json
unity command eval_file --file Tools/Ayna/f3_status.cs 60000 --timeout 90 --caller plugin --skill unity-cli --format json
unity command eval_file --file Tools/Ayna/f3_cancel.cs 60000 --timeout 90 --caller plugin --skill unity-cli --format json
python3 Tools/Ayna/summarize_learning.py
```

EditMode için async çağrı, domain reload sırasında CLI zaman aşımı belirsizliğini azaltır:

```bash
unity command run_tests --mode editmode --async_tests true --caller plugin --skill unity-cli --format json
unity command test_status --caller plugin --skill unity-cli --format json
```

Profil dosyası API'si ayrı Game assembly'sindedir; F4 denetleyicisi kalıcı veri kökünü verir. `Load(Off)` dosya varlığına bakmaz, `Commit` beklenen nesil/hafıza kimliğini ister, `EraseMemory` yalnız kullanıcının açık sıfırlama eylemiyle çağrılır.

## Takasın yorumu

SurpriseBot eğitim, üretim ve savaş yapar; GreedyBot yalnız sunucu genişlemesini seçer. Brifin Surprise≤Greedy kapısı aynen değerlendirilir; ancak farkın tamamı Sürpriz ödülüne nedensel olarak bağlanamaz. Bu nedenle ilk 20 serinin aynı tohumlarıyla, aynı Surprise politikası ve Tam öğrenmede yalnız `SurpriseThreshold=0` karşılaştırması hazırlanmıştır. Bu tanı kabul serisi değildir; varsayılan denge değiştirilmez. Eşik sıfır olduğunda hem +3 hem ilk vuruş tetiklenmez.

**Tanı tamamlandı: 20 seri ×5 =100 oyun.** Aynı botun ilk 20 serisinde oyuncu kazanması ödül açıkken **%96**, kapalıyken **%74**; fark **+22 puan**, seri kümeli %95 bootstrap aralığı +14…+30 puan. Bu, ödül ve ilk vuruşun birlikte etkisidir; ayrı ayrı etkileri ölçülmedi. Kapalıyken de %74 kazanması, Greedy ile %70,4 puanlık farkın yalnız ödülden gelmediğini gösterir; 100 seri ana kabul sonucunun yerini almaz. [Eşli karşılaştırma](denge/F3/ablation_karsilastirma.json). Tanı raporundaki “SurpriseBot Sürpriz %30 altında” kaydı, eşiğin özellikle sıfırlanmasının beklenen sonucudur; varsayılan oyunun kabul kapısı olarak kullanılmaz.

## Bilinen sınırlar

Bu fazda oynanabilir Unity ekranı kurulmadı; saf Core/Mind oturumu, profil dosyası API’si ve Editor ölçüm araçları hazırlandı. Denetleyici, görünüm modelleri ve debug sahnesi F4 kapsamındadır.

RandomBot / Tam son serisinde AYNA ilk ve beşinci oyunda %100 kazandı. ±5 puan kapısı sayısal olarak geçer; tavan etkisi nedeniyle bu kohort tek başına “rastgele oyuncuya karşı öğrenmenin hiç etkisi yok” çıkarımını desteklemez. Öğrenmesiz eş tohumlu kontrol ayrıca gösterilir.

Model, tüm insan stratejilerini kapsayan bir öğrenme sistemi değildir: beş genel politika hipotezi + gözlem sayaçları kullanır. Bot kimliği okunmaz, gizli duruma erişmez. Şaşırtıcı veya yeni davranışlarda düşük güven ve yanlış tahmin beklenir. Tepki sayacındaki Yürü sınıfı yer değiştirme/geri çekilme vekilidir; stratejik niyeti kesin bildiği iddia edilmez.

## Kararlar, sapmalar ve sonraki faz

K60–K79 profil, ödül ve gözlem kararlarıdır. K80–K82 son ölçüm ve yorum sınırlarını kaydeder. Açık kabul hedefleri sessizce değiştirilmedi; **F3 teknik uygulaması tamamlandı, tam denge kabulü verilmedi**. F1/F2’den taşınan mimari/rol dengesi de ayrıca açık.

Önerim F4’ten önce kısa bir F3 denge turu:

1. **Tahmin:** en güçlü tek emri bulmak yerine, aynı plandaki ikinci/üçüncü kabul edilmiş emirlerin hedef olasılıklarını da kalibre et. Soğuk başlangıç ve eksik görüşü ayrı ölç; <%5 hedefini son oyunlara taşıma.
2. **Takas kontrolü:** aynı eğitim/savaş politikasını kullanan iki referans botu yalnız hedef seçimi bakımından ayır; mevcut Surprise/Greedy kapısını ayrıca aynen raporla. Böylece askeri davranış farkı ödül etkisi gibi yorumlanmaz.
3. **Tasarım kararı adayı, uygulanmadı:** sonuçsuz ücretli tekliflerden Sürpriz kazanılmasını engellemek veya +3/ilk vuruşu yeniden dengelemek. Bu mevcut “geçerli emre ödül” kuralını değiştireceğinden ayrı karar gerekir. Son tanı ikisini birlikte kapattı; +3 ve ilk vuruşun ayrı etkisini göstermiyor.

F4 kapsamı: oynanabilir uGUI debug sahnesi, AynaGameController ve A1–A12 görünüm modelleri. Kullanıcının [uygulama talimatı §0, §6–7](CODEX_PROMPT.md) “Her faz sonunda dur, Türkçe rapor ver ve onay bekle” der; **F4 başlatılmadı**. Sonraki seçim: F3 denge turuyla açık kapıları kapatmak (önerilen) ya da bu iki açık hedefi kabul ederek F4’e geçmek.
