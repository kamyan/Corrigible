# Kartlı başlangıç rehberi

2026-11-09 · Unity 6000.3.12f1 · kayıt biçimi 15.

Yeni harekâtta emir belgesi kapandıktan sonra ilk vardiya rehberi açılır. Mevcut sahne notu kullanılır; yeni sahne veya sahne kurucusu yoktur.

## Öğrenme sırası

1. Komuta odası ve zamanın yalnız dosyalamayla ilerlemesi.
2. SIĞINAK ile pano; bir kişiyi seçme ve kendi can/hareket/güç değerleri.
3. Boşta'dan Mutfağa gerçek sürükle-bırak, dolu alanda takas.
4. Üç saha rolü ve kişiye verilen kitler.
5. Görev fişi ve isimli kadro raporu.
6. Gerçek kampanya baskınında seçim → hareket → yetenek → tur bitirme → kasa ve tahliye.
7. Sonuç ve operasyon raporunun dosyalanması.
8. Yaralıyı Boşta'da dinlendirme.
9. Şalterin kapağı/kolu ile fiş basma; ayrı adımda fişi EVRAK'a bırakıp yeni vardiya.
10. Sonraki üretim ve kabul işleri için kısa yönlendirme.

Toplam 18 kısa adım var. Bilgi adımı SONRAKİ ile, eylem adımı gerçek durum değişince ilerler. Boşta kartını tıklamadan doğrudan Mutfağa atan oyuncu geriye dönüp ayrıca seçim yapmak zorunda kalmaz. Rehberden önce baskına çıkan veya ilk baskını kaybeden oyuncu da hazırlık/hamle adımlarında takılmaz. Dersler kaynak ya da ücretsiz hamle vermez; mevcut mekaniklerin erişim kurallarını değiştirmez.

## Tercihler ve devam

- EĞİTİMİ BİTİR açık rehberi kapatır, sonraki yeni harekâtta tekrar açılmaz.
- Esc'in pano/dosya geri zinciri korunur; kazara rehberi bitirmez.
- `Corrigible.PlanningTutorialDone`, `Seed`, `Step` ayrı PlayerPrefs alanlarıdır. Aynı tohumun yarım kalan rehberi kayıttan devam eder; eski kayıtlara dayatılmaz.
- İlk Start bir kare bekler; açılış/DEVAM seçimi sonuçlanmadan eski rehber ilerlemesi ezilmez. İlk ilerleme yazısı ancak emir belgesi kapandıktan sonradır.
- Editörde Corrigible › Eğitim › Eğitimi sıfırla, yeni ve eski rehberi sıfırlar; yeniden denemek için yeni harekât başlatılır.

## Kod ve görünüm

`PlanningTutorialFlow` ve bağlamı saf C# sunum akışıdır. `SiginakTutorial` mevcut GameController, pano, harita, baskın, rapor ve `SiginakShiftClose` durumunu okur. Kendi oyun komutu yazmaz. Gerçek oyuncu eylemleri mevcut komut kapılarından geçer. `TutorialNoteView` aynı içerikte her kare metin/ölçü yazmaz; panelin zemin ve yazıları masaya giden tıkları yutmaz. Rehber adımları gerçek isimli kampanya kadrosunu kullanır; eski `TutorialRaid` ve sabit kadro kuralı yalnız eski oyun yolunda kalır.

Üretim/kabul için burada ayrı uygulamalı ders yoktur; bu rehber ilk operasyon ve ilk vardiya kapanışına odaklanır. Türkçe ve İngilizce metinler çeviri tablosundadır.

## Doğrulama

- Tam EditMode: **993/993** (`editmode_sonuclar.json`).
- Tam PlayMode: **88/89** (`playmode_sonuclar.json`). Yeni rehberin dört testi geçti. Mevcut `ProductionPlayTests.RecipeDrag_FromPage_BookSlidesDown_DropOnOrder` testinde kitap açı toleransı 0,05° yerine 0,163° ölçüldü; yalnız bu test tekrar koşulunca **1/1 geçti** (`uretim_tekrar_sonuclar.json`). Üretim kodu değiştirilmedi.
- Son rehber odaklı canlı koşu: **4/4** (`rehber_canli_sonuclar.json`); gerçek pano ataması ve baskına geçiş, kayıt/devam, eski kayıt/açılış ve dil/Esc.
- Bütün 18 adım × 2 dil: **36 ölçüm**, başlık/gövde taşması 0 (`metin_olcumleri.json`). İki uzun İngilizce başlık iki satır kullanır.
- Yalıtılmış gerçek Play oturumunda masa TR/EN, pano ve baskın görüntüleri `screens/` altında. Panel son baskın üst bilgisinin altında kalır.
- Gerçek beş kayıt dosyasının SHA-256 değerleri, 10 tercihin değer/varlık durumu ve görev başında yedeklenen 24 font aynı kaldı (`koruma_kontrolu.json`, `son_durum.json`). Denemeler ayrı `Temp` kayıt alanlarında yapıldı; Editor Play kapalı, Giris sahnesi açık ve test kayıt yönlendirmesi temiz. Core, denge, RNG ve kayıt biçimi değiştirilmedi.

Son görsel kontrolde, panodan dönüş animasyonu sürerken açılan baskında oda kamerası kilitlenip rehberi gizliyordu. Rehber baskın yüklendiğinde oda kamerasının yarım kalan animasyonunu beklemez; canlı test aynı geçişi doğrular.
