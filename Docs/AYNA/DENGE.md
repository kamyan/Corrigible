# AYNA · F5 denge turu

20.10.2026. **Ölçüm turu tamamlandı; genel oyun dengesi kabul edilmedi.** Yavaş ekonomi adayı oyun süresi hedefini sağladı, fakat F3’te kabul edilmiş Sürpriz/Greedy takasını bozdu. Bu nedenle aday reddedildi; çalışan varsayılan `AynaBalance.cs` ve `AynaBalance.asset` F4 başlangıcındaki içeriklerine birebir döndürüldü. Yeni oyun algoritması veya kuralı eklenmedi. Son kaynak kimliği `denge/F5/final_source.json` içindedir.

## Deney ve sınırlar

Yedi bot: Mind, Habit, Random, Greedy, Aggro, Stealth, Surprise. Her bot için üç oyuncu mimarisi × üç AYNA mimarisi; sekiz tohum × iki başlangıç coğrafyası. Otomatikte 144, taktikte ayrıca iki niyet rolüyle 288 oyun; her tam matris **3.024 oyun**. Başlangıç, adayın aynı tohumları ve bağımsız 100–107 örnek ofseti toplam **9.072 oyun**. Bağımsız tohumlar aday seçildikten sonra kullanıldı. Ayrı dar aday ve tanı koşuları bu toplamın içinde değildir.

Her bağımsız oyunda profil boş başlar, oyun içi öğrenme Tam’dır; mimariler sabittir. F3 koruma deneyinde ise 100 bağımsız serinin her birinde beş oyun boyunca profil taşınır ve başlangıç karşı seçimi açıktır. Bu iki deneyin kazanma yüzdeleri aynı payda gibi karşılaştırılmaz. İnsan oyuncu nüfusu veya insan oynanış süresi ölçülmedi; döngü sayısı dakika değildir. Taktik bot planlayıcısı insan kararlarını temsil etmez.

Saf Core/Mind DLL’leriyle mevcut Mono çalışma zamanı kullanıldı, paket kurulmadı. `AynaBalanceBench` ve bağımsız sürücü aynı oyun adımlarını işletir. Unity’deki ilk tohumun 36 taktik Mind oyunu bütün oyun alanlarıyla Mono başlangıcıyla birebir eşleşti (`denge/F5/unity_before/equality.json`). Unity makro p50 **39,93 ms**, tahta p50 **11,30 ms**; Mono süreleri Unity süreleri olarak sunulmaz. Makro ölçümü tahmin hazırlığı + AYNA kararını içerir; oyuncu botunun plan kurması hariçtir. Mind/Mind’de iki tarafın tahmin hazırlığı bu aralığa girebilir.

## Önce / aday sonrası / bağımsız doğrulama

Tabloda kazanma AYNA tarafıdır; n her matris için ayrı paydadır. “Diğer” sonuçlar toplamdan çıkarılmaz. Aday sütunları **reddedilen ayarlardır**, son varsayılan değildir.

| Bot | Mod | n/matris | AYNA önce → aday | AYNA bağımsız | Döngü önce → aday | Döngü bağımsız | Savaş/oyun önce → aday |
|---|---|---:|---:|---:|---:|---:|---:|
| Mind | auto | 144 | 47.2% → 52.8% | 29.2% | 9 → 27 | 28 | 10.06 → 7.11 |
| Mind | tactical | 288 | 52.1% → 49.0% | 48.6% | 8 → 26 | 26 | 8.03 → 8.15 |
| Habit | auto | 144 | 8.3% → 5.6% | 2.8% | 7 → 17 | 17 | 6.32 → 4.22 |
| Habit | tactical | 288 | 19.4% → 26.0% | 19.8% | 7 → 17 | 18 | 7.99 → 5.75 |
| Random | auto | 144 | 100.0% → 99.3% | 97.2% | 8 → 27 | 29 | 2.35 → 3.60 |
| Random | tactical | 288 | 100.0% → 100.0% | 99.7% | 7 → 24 | 24 | 1.89 → 2.85 |
| Greedy | auto | 144 | 6.9% → 34.0% | 18.1% | 8 → 26 | 23.5 | 13.15 → 17.80 |
| Greedy | tactical | 288 | 14.6% → 31.6% | 33.3% | 7 → 20 | 20 | 10.36 → 11.85 |
| Aggro | auto | 144 | 18.1% → 26.4% | 13.2% | 8.5 → 28.5 | 27.5 | 12.56 → 18.85 |
| Aggro | tactical | 288 | 22.2% → 27.8% | 21.2% | 8 → 22 | 21 | 9.13 → 11.67 |
| Stealth | auto | 144 | 100.0% → 37.5% | 30.6% | 8 → 27 | 27 | 3.05 → 4.78 |
| Stealth | tactical | 288 | 99.7% → 39.6% | 31.2% | 8 → 27 | 27 | 3.30 → 6.36 |
| Surprise | auto | 144 | 85.4% → 19.4% | 7.6% | 8 → 23 | 22 | 6.85 → 9.89 |
| Surprise | tactical | 288 | 84.7% → 18.4% | 16.3% | 8 → 21 | 21 | 5.50 → 7.83 |

Tam dokuz mimari hücresi, niyet rolü, bitiş türleri, Wilson aralıkları ve kaynak/denge kimlikleri:

- [Başlangıç özeti](denge/F5/before/SUMMARY.md), [bütün bot × mimari hücreleri](denge/F5/before/summary.json).
- [Reddedilen aday özeti](denge/F5/after/SUMMARY.md), [hücreler](denge/F5/after/summary.json).
- [Bağımsız tohum özeti](denge/F5/validation/SUMMARY.md), [hücreler](denge/F5/validation/summary.json).

Tek hücrede otomatik n=16, taktik n=32; aralıklar geniştir. Coğrafya/rol tekrarları nedeniyle bunlar insan nüfusu için bağımsız örnek güven aralığı sayılmaz. Mind/Mind eş mimarideki örnek dalgalanmaları tek başına mimari üstünlüğü kanıtlamaz.

## Denenen değerler ve gerekçeleri

| Alan | Kabul edilmiş F4 / son varsayılan | Reddedilen yavaş aday |
|---|---:|---:|
| Sunucu / çekirdek geliri | 1 / 2 | 0,2 / 1 |
| Yürü bedeli | 1 | 0,5 |
| Sürü / Kale genişleme sınırı | 4 / 2 | 3 / 3 |
| Hakimiyet payı | %60 | %55 |
| Eğitim bedeli | 6 / 12 / 20 | 2 / 4 / 6 |
| Eğitim süresi | 1 / 2 / 3 | 1 / 1 / 2 |
| Tekillik bedeli | 30 | 8 |
| Kademe değeri / Hayalet toleransı | 12 / 1 | 24 / 0,25 |
| Ordu canı / çekirdek baskın değeri | 3 / 36 | 2 / 48 |
| Tahtada çekirdeğe yaklaşma | 0,35 | 0,75 |
| En kötü durum / kontrol azaltma ağırlığı | 0,75 / 0,75 | 0,25 / 0,5 |
| Öğrenilmiş çekirdeğe yaklaşma değeri | 18 | 4 |

Geliri azaltma erken genişleme yarışını yavaşlatmak; daha ucuz eğitim ve Tekillik, bilim yolunu düşük gelirde kullanılabilir tutmak içindi. Daha ucuz Yürü ve baskın değeri savaş/çekirdek yolunu korumayı amaçladı. Her değişiklik mevcut `AynaBalance` alanından yapıldı; yeni algoritma veya RNG sırası değişikliği yoktur. Tam görüntüler `before/balance.json` ve `selected_balance.json`, farklar `selected_delta.json` içindedir. `selected_*` adları seçilmiş **ön aday** tarihçesidir; üretim kabulü anlamına gelmez.

İlk dört tohumda üç botun altı kohortu, aday başına 648 oyundu:

| Aday | Mind auto / taktik döngü | Gözlem / karar |
|---|---:|---|
| tempo | 18 / 17 | Yayıl 3, gelir 0,6; yeterince uzatmadı |
| tempo_strategy | 18 / 17 | Savaş değerleri tempo sorununu çözmedi |
| slow_strategy | 22 / 20 | Yayıl 4; savaş sayısı 16,29 / 14,41’e çıktı |
| income_simple | 20 / 20 | Gelir 0,3; otomatikte Süre %15,3 |
| income_strategy | 19 / 18 | Bilim yolu kapanıyordu |
| value_only | 8,5 / 8 | Gelir değişmeden bu ağırlıklar yeterli olmadı |
| long_income | 29,5 / — | Otomatik Süre %40,3; bilim yok; reddedildi |
| balanced | 28 / 27 | Tam matrislere taşındı, öğrenme takasında reddedildi |
| balanced_slow | 34 / 30 | Gelir 0,15; ana adayın daha yavaş varyantı; kabul edilmedi |

Dar taramalar aday seçimi içindir; aynı tohumlarla tarandığından bağımsız kabul kanıtı değildir. Her adayın tam değerleri `denge/F5/candidates/`, ham oyunları `probe_*` dizinlerindedir.

## Öğrenme koruması ve adayın reddi

Adayın altı 100×5 kohortu = **3.000 oyun**: Habit kazanma artışı +39 puan, top-1 %93,44, Habit Sürprizi %3,71; Random farkı 0; Surprise Sürprizi %41,84. Buna karşılık **Surprise oyuncu kazanması %92,4 > Greedy %77,4**. Eşli seri farkı +15 puan, %95 seri bootstrap aralığı +10,8…+19,4 puan. F3 kapısı başarısız; [tam aday koruma özeti](denge/F5/learning/OZET.md).

100’er oyunluk tanılar (aynı ilk 20 seri, nihai kabul değildir):

| Aday varyantı | Surprise oyuncu kazanması | Greedy oyuncu kazanması |
|---|---:|---:|
| İade oranı 0,05 | %89 | ölçülmedi |
| İade 0, ilk vuruş açık | %93 | ölçülmedi |
| Ödül + ilk vuruş kapalı | %92 | ölçülmedi |
| Saldırı bedeli 1 | %94 | %76 |
| Saldırı 1 + iade 0,05 | %93 | %76 |
| F4 savunma/yaklaşma ağırlıkları | %88 | %68 |
| Bu ağırlıklar + saldırı 1 | %92 | %74 |

Ödüller tamamen kapalıyken de sorun sürdüğü için “fazla iade tek nedendir” çıkarımı desteklenmedi. Saldırı maliyetini azaltmak veya F4 savunma ağırlıklarını geri getirmek de farkı kapatmadı. Bot politikaları düşük gelirde farklı fırsat maliyetleri taşıyor olabilir; bu bir hipotezdir, bu ölçümler tek başına nedeni kanıtlamaz. F5 algoritma değiştirme fazı olmadığı için kural/planlayıcı değişikliğiyle kapı aşılmaya çalışılmadı.

## Kabul ve açık sapmalar

| Ölçüt | Başlangıç / son varsayılan | Yavaş aday | Karar |
|---|---|---|---|
| Bütün bot × mimari × iki savaş modu | 3.024 oyun | 3.024 + bağımsız 3.024 | Ölçüm tamam |
| Mind ortanca 25–35 döngü | auto 9 / taktik 8 | 27 / 26; bağımsız 28 / 26 | Başlangıç açık; aday geçti |
| ≥%90 oyun 40’tan önce | Mind %100 / %99,65 | %97,22 / %97,92; bağımsız %100 / %100 | Mind geçti; her bot için ayrıca tablolara bak |
| Random’a karşı AYNA ≥%90 | %100 / %100 | %99,31 / %100 | Geçti |
| Greedy’ye karşı AYNA ≥%60 | %6,94 / %14,58 | %34,03 / %31,60 | Açık |
| Hiçbir mimari eşleşmesi %65’i aşmaz | Taktik Sürü→Hayalet oyuncu %75 | %81,25; bağımsız %65,63 | Açık; küçük hücre paydası |
| Niyet rolü dengesi | Mind bot kazanması %47,92 / %41,67 | %63,19 / %38,19 | Aday rol farkını artırdı |
| F3 Surprise kazanması ≤Greedy | F4 %16,4≤%67,2 | %92,4>%77,4 | Aday reddedildi |
| Unity makro ortancası ≤50 ms | 36 oyun p50 39,93 ms | Aday için Unity hız kabulü verilmedi | Başlangıç geçti |

Otomatik çözüm çekirdek dolabını kırmadığı için otomatikte Silme %0 tasarlanmış kuralın sonucudur; taktik Silme oranı ayrı raporlanır. Adayda Son Şalter’in Mind/Mind matrisinde %0’a düşmesi de bitiş çeşitliliği açısından gözden geçirilmelidir. Bütün bitişleri tek yüzdelik toplam altında gizlememek gerekir.

## Dosyalar, doğrulama ve sonraki çalışma

Yeni `Editor/AynaBalanceBench.cs` tekrar üretilebilir matrisi işletir. `BalanceBenchTests` coğrafya ayna eşlemesini, tohum/rol ayrımını ve eski ekonomi değerleriyle replay’i sınar. Eski sabit sayı bekleyen AYNA testleri kurala/fixture’a bağlandı; ürün algoritması değişmedi. `Tools/Ayna` sürücüleri ve özetleyiciler rapor üretir. Önceki F4 öğrenme kabul özeti `F4/f3_kabul_ozet.md` altında donduruldu.

Son kaynakta **614/614 EditMode testi** geçti (169 AYNA + eski oyunun 445 testi), 28,71 saniye. CLI aktarımı 30 saniyede zaman aşımı verdi; bitmiş NUnit XML ve konsol sonucu geçişi doğruladı. [Makine sonucu](denge/F5/final_testler.json).

Geri getirilen F4 değerleriyle ayrıca **3.000 oyunluk son F3 koruması** tamamlandı ve bütün ölçütler yeniden geçti: Habit +41 puan, top-1 %97,78, Sürpriz %4,15; Random farkı 0; Surprise Sürprizi %37,57, oyuncu kazanması %16,4≤Greedy %67,2. Mono plan p50 4,92–6,00 ms; [son koruma raporu](denge/F5/final_guard/OZET.md). Bu koşu, reddedilen adayın 3.000 koruma oyunundan ve 9.072 matris oyunundan ayrıdır.

Canlı son denemede başlat/kaydet/temizle/yükle gerçek düğmeleri aynı kaydı geri verdi; oyun 8. döngüde AYNA Hakimiyetiyle bitti, profil nesil 1’e bir kez yazıldı. [Başlangıç kanıtı](denge/F5/final_canli_baslangic.json), [oyun sonu](denge/F5/final_canli_son.json), [planlama görüntüsü](denge/F5/final_planlama.png), [oyun sonu görüntüsü](denge/F5/final_oyun_sonu.png). Yalnız Temp/AynaF5FinalPlay kullanıldı. Editör Play kapalı, AynaDebug sahnesi temiz, eksik script 0; son derlemede hata yok.

Normal savegame/player_profile başlangıç hash’leriyle aynı; normal AYNA kaydı/profili ve kampanya oluşturulmadı. TestResults.xml yalnız kendi ürettiğimiz dosya hash/zaman eşleşmesiyle başlangıca döndürüldü. Tercihlerde yalnız PT_Run ve Unity oturum meta anahtarları değişti; oyun tercihleri veya plist bütünü geri alınmadı. [Dosya denetimi](denge/F5/user_file_audit.json).

Önerilen takip sırası: (1) boş profille Greedy karşısındaki karar kalitesini ve Hayalet adaylarını ayrı değerlendirmek, (2) aynı politika üzerinde niyet/cevap rolü etkisini ayırmak, (3) gelirden bağımsız genişleme sınırlarıyla tempo adayını dar ölçmek, (4) ancak bütün eski öğrenme kapıları geçince yeni ekonomi varsayılanını kabul etmek. Son seçenek henüz denenmedi; onaylanmış denge çözümü değildir. Bu fazın sonucu ölçüm altyapısı + rapordur; genel denge çalışması devam gerektirir. Yeni algoritma ve Fx arayüzü bu fazda uygulanmadı.
