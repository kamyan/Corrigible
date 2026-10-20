# AYNA · Durum

20.10.2026 · **Kullanıcının “iki denge sorununu çöz ve F4’e başla” talimatı uygulandı. F3’ün iki açık kapısı kapandı; F4 oynanabilir sahne ve kayıt akışı kuruldu. F5 ölçüm turu tamamlandı. Yavaş ekonomi adayı öğrenme takasını bozduğu için reddedildi; F4 varsayılanları korundu. Genel denge kabulü açık.**

## AYNA tasarım arayüzü

Kullanıcının SonSalter_AYNA_Tasarim talimatı uygulandı. Yeni sahne **Assets/Corrigible/Ayna/Scenes/Ayna.unity**, açılış menüsü **Corrigible > AYNA > Oyunu aç**. A1–A12 akışı; holo harita/tahta, bölge komutları, gelişim/birlik/model çekmeceleri, TR/EN, kayıt/devam ve son ekranı gerçek denetleyiciye bağlı. Uygulama ve canlı kanıtlar [UI/UYGULAMA.md](UI/UYGULAMA.md) içinde. Core/Mind ve F4 denge değerleri korunur; F5 borcu devam eder. Son kaynakta **618/618 test** (173 AYNA + 445 eski) geçti. Sahne temiz, Play kapalı; kullanıcının çalışma sırasında oynadığı son eski oyun durumu korundu.

## F5 sonucu

Üç tam matris =9.072 oyun; yedi bot × dokuz mimari eşleşmesi × otomatik/taktik. Yavaş aday Mind ortancasını 9/8 döngüden 27/26’ya uzattı; bağımsız örnekte 28/26. Ayrı 3.000 öğrenme koruma oyununda Surprise kazanması %92,4>Greedy %77,4 olduğu için aday reddedildi. Son varsayılan kaynak ve asset F4 başlangıcına birebir eşit. Yeni ölçüm araçları ve kapsamlı önce/sonra, mimari, rol ve bitiş tabloları [DENGE.md](DENGE.md) içinde.

F5 son kaynakta **614/614 test** (169 AYNA + 445 eski) geçti. Son değerlerle yeniden 3.000 F3 koruma oyunu bütün kapıları geçti; sonuçlar F4 kabulüyle aynı. Canlı kaydet/yükle eşit, oyun sonu ve nesil 1 doğrulandı. Normal kayıt/profiller korundu, Play kapalı ve sahne temiz.

## F4 kabul doğrulaması

- **611/611 EditMode testi geçti:** 166 AYNA + mevcut oyunun 445 testi. Son kaynak ve makine sonucu [F4 raporunda](F4_NOTLARI.md).
- Son kaynakta **3.000 oyun** tamamlandı. Habit Sürprizi **%4,15** (<%5); Surprise **%37,57** (≥%30); Surprise/Greedy oyuncu kazanması **%16,4/%67,2**. Kabul hedefleri değişmedi.
- Habit AYNA kazanması **%21→%62 (+41 puan)**, top-1 **%97,78**. Random farkı 0; Tam/Kapalı tavanda olduğu için genelleme sınırlı.
- Saf Unity DLL’leriyle Mono kohortlarının tahmin+makro p50’si 4,71–6,19 ms. Son kaynakta 50 Habit oyununun bütün sonuçları Unity’de birebir eşleşti; Unity p50 37,61 ms ile ≤50 ms kapısı geçti. Canlı kanıtlar F4 raporundadır.
- Ayrı 20 serilik kontrolde Habit Sürprizi %4,38, Surprise %37,08; Surprise kazanması %20≤Greedy %67.

## Uygulanan sistem

Bağımsız `Assets/Corrigible/Ayna/` altında Core, Mind, Presentation, Game, Editor ve Tests assembly’leri var. Eski oyun bu modüle taşınmadı.

F1: eşzamanlı emirler, algı/görüş, eğitim/gelir/farkındalık, bitişler, otomatik çözüm. F2: 8×8/altı turlu taktik tahta, on birim, sınıf üçgeni, tuzak, niyet/cevap, ilk vuruş, düğüm, çekirdek dolabı, saf önizleme ve geri alma. F3: altı hipotezli öğrenme, profil v3, nesil, tahmin kilidi, Sürpriz/Okunabilirlik, öğrenme seviyeleri ve atomik profil deposu.

Son Sürpriz politikası 4: en pahalı anlamlı bölgesel emir kümesi, eş bedellilerde sıra bağımsız toplam hedef olasılığı; en az iki ağırlıklı doğrulanmış gözlem desteği; koşullu eşik %20. Ödül bedelin %25’i, en çok 3 İşlem. İlk vuruş tek ana hedef bölgesinde kullanılabilir. Greedy ekonomik getiriyi, Surprise tek düşük olasılıklı ana genişlemeyi önceler; araçları aynı, fırsat maliyetleri farklıdır. Bu referans değişikliği eski bot yüzdeleriyle doğrudan aynı politika kıyası sayılmaz.

Öğrenilmiş konum yeterince güvenilirse makro bir yaklaşma rotası üretir; ara konum değeri denge asset’indedir. Mind canlı gizli dünya alamaz, Core motordan bağımsızdır. RNG akışları ayrıdır. Kayıt formatı 4, kurallar/akıl 6; eski politika 0–3 kendi kayıt kurallarıyla devam eder.

F4: `AynaGameController`, atomik kayıt/devam, yalnız oyuncu görüşünden A1–A12 görünüm modelleri, örnek JSON’lar, `AynaBalance.asset`, tema ve AYNA’ya ait statik font, `Scenes/AynaDebug.unity`. Başlangıç → planlama → AÇ → çatışma/kadro → tahta → sonuç → oyun sonu akışı çalışır. Kaydet/devam, neslin bir kez birleştirilmesi, ikinci nesil tekrar yükleme ve TR/EN canlı denendi. Ekranlar ve kanıtlar [F4_NOTLARI.md](F4_NOTLARI.md) içinde. Hafıza silme onayı ekran/oyun/yükleme değişiminde iptal edilir.

## Taşınan denge borcu

F1/F2’nin genel mimari/rol dengesi ayrı bir iştir. Tarihsel otomatik Hayalet/Sürü eşleşmesinde Sürü %69,09 ile %65 sınırını aşmıştı; F2 taktik tam oyunda mimari/rol hedefleri de geçmedi. Bunlar yeni kaynakta kabul edilmiş sayılmaz. F3 öğrenme/Sürpriz kabulü, bütün oyunun mimari dengesinin tamamlandığı anlamına gelmez. [F2 raporu](F2_NOTLARI.md), [tarihsel F3 ilk teslimi](F3_NOTLARI.md).

## Veri koruma ve faz sınırı

Kullanıcının oynadığı yeni kayıt korunur. Normal savegame, kampanya ve profiller geri yüklenmez; testler Temp altında izoledir. Yalnız testin ürettiği TestResults.xml hash/zaman eşleşmesiyle kontrollü temizlenir. Unity oturum/performance meta anahtarları korunur; oyun tercihleri geri alınmaz. Eski font/prefab otomatik serileştirme yan etkileri hash/zaman kontrolüyle temizlendi; sahne kendi fontunu kullanır.

Eski oyun kaynakları, sahneleri ve proje ayarları değiştirilmedi. Ortak çeviri tablosunda yalnız 141 yeni AYNA TR/EN satırı var. Yeni paket, commit/push/dal işlemi yok.

F4 yetkisi kullanıcıdan geldi; tekrar izin istenmez. Kullanıcı 20.10.2026 tarihinde F5 genel denge turunu açıkça onayladı; F5 ölçüm turu raporlandı. Algoritma değişikliği veya sonraki çalışma kendiliğinden başlatılmaz.
