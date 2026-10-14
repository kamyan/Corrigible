# Son Şalter — araştırmaların katkısı

14 Ekim 2026. Mevcut araştırma kataloğu, C# simülasyonu, Unity'ye özgü öğrenme kuralları ve Analist Masası
sunumu incelendi. Bu belge bir kod incelemesidir; oyun kuralları, fiyatlar ve araştırma kartları değiştirilmedi.
Fayda değerlendirmeleri koşullara dayalı tasarım yorumlarıdır; araştırma başına kazanma oranı ölçümü yapılmadı.

Dokuz araştırmanın hepsi bir etki üretiyor; fakat bazıları yalnız belirli tehditlere karşı değerli. Oyuncuya yeni
karar veren projeler ile arka planda katsayı değiştiren projeler aynı görünürlükte değil. Bazı açıklamalar da
etkiyi olduğundan geniş veya kalıcı anlatıyor.

## Katalog ve oyuncuya katkı

| Araştırma | Fon / nominal gün | Önkoşul | Gerçek katkı ve kullanım koşulu |
|---|---:|---|---|
| Anomali Algılama | 30 / 8 | Yok | Uygun bölgelerde otomatik tespit ihtimalini iki katına çıkarır. Erken farkındalık, daha fazla tespit ve izlenen bölgelerde Hizalama gözlemi sağlar. |
| Doğrulama Ağı | 40 / 10 | Anomali Algılama | Haber başlığı başına 2 SG ile doğru/sahte kontrolü açar; sahte kaynağı işaretler. Güven kaybettiren sahte haberi doğrulamak kaybı geri alır. |
| Güçlendirilmiş Ağlar | 40 / 10 | Yok | Bölgeler arası kara ve uydu yayılımını %30 azaltır; Hizalama sabotajı ihtimalini yarıya indirir. |
| Kuantum Karantina | 60 / 14 | Güçlendirilmiş Ağlar | Uydu Ağı'nın karantinayı aşmasını engeller. Karantinaya temas eden uydu geçişini tamamen keser ve kara karantinasının kesme oranını %60'tan %90'a geri getirir. |
| Kamu Bilinçlendirme | 35 / 8 | Yok | Karantina, şalter, hediye reddi, baskın zararları ve birçok olayın güven kaybını yarıya indirir. Güven üzerinden fon gelirini ve işbirliğini dolaylı korur. |
| Analog Yedekler | 50 / 12 | Kamu Bilinçlendirme | Bütün bölgelerde tek seferlik bağımlılık −20; veto eşiği 70→85. Saha denetimi ve denetimli hediye kabulü açılır. |
| Yorumlanabilirlik | 68 / 25 | YZ tespiti | Çekirdeğin o anki yerini bulur; o anda beklenen sıradaki yeteneği günlüğe yazar. Çekirdek operasyonlarını kolaylaştırır. |
| Düzeltilebilirlik | 98 / 30 | Yorumlanabilirlik | Bundan sonraki Tekillik artışını %25 azaltır. Hizalama zincirinde zaman kazandırır. |
| Değer Kilidi | 165 / 38 | Düzeltilebilirlik | Tamamlandığı anda Hizalama zaferi verir. |

Kaynak: [araştırma kataloğu](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/GameData.cs:163).
Bedeller proje başınadır; önkoşulların bedelini içermez. Süreler ideal koşullardaki iş miktarını gösterir.

## Etkilerin sınırları

**Anomali Algılama:** İzlenen bölgede sızma 5'i, izlenmeyen bölgede 40'ı aşmadan otomatik tespit üretmez.
İzleme ağı kurmaz ve sızmayı azaltmaz. Aldatma yeteneğinin ×0,5 tespit çarpanını dengeler. Zaten tespit edilmiş
bölgede ek katkısı yoktur. Denetim de tespit sağlayabildiği için erken oyunda her stratejinin zorunlu ilk seçimi
değildir. Tespit sayısı SG gelirini; izlenen ve tespit edilen bölge sayısı Hizalama hızını artırır.
[Tespit](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Simulation.cs:241),
[gözlem ve gelir](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Simulation.cs:122).

**Doğrulama Ağı:** Otomatik haber filtresi değildir; her başlık için oyuncu komutu ve 2 SG gerekir. Medyanın
karalama haberi bütün bölgelerde 3 güven kaybettirir; bu başlık sahte olarak doğrulanınca bütün bölgelere 3 güven
geri verilir (100 sınırı uygulanır). Kaynağı işaretlemek bölgesel sızma raporunun gerçek değerini açmaz. İllüzyon
sırasında kontrol yine 2 SG harcar ama sonuç “doğrulanamadı” olur; perdeyi yırtmaz. Başlangıçtan toplam yatırım
Anomali ile birlikte 70 fon / 18 nominal gündür.
[Doğrulama](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Simulation.cs:807),
[karalama](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Simulation.cs:460).

**Güçlendirilmiş Ağlar:** “Tüm yayılma −%30” ifadesi geniştir. Etki yalnız bölgeden bölgeye geçiş katsayısına
uygulanır; bölge içindeki lojistik büyüme değişmez. Mevcut sızma anında düşmez. Sabotaj ihtimalinin yarılanması
Hizalama projelerini hem zaman hem tekrar ödeme açısından korur; diğer dallar zaten aynı sabotaj mekanizmasına
maruz kalmaz. Erken yayılmayı sınırlamak ve Hizalama zincirini korumak için çok yönlü bir seçimdir.
[Büyüme/yayılma](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Simulation.cs:185),
[sabotaj](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Simulation.cs:124).

**Kuantum Karantina:** Uydu Ağı yoksa mevcut karantina zaten kara geçişinin %90'ını keser ve uydu geçişi yoktur;
projenin o anda ek faydası olmaz. Karantina uygulanmıyorsa da ek fayda üretmez. Uydu varken kara geçişinde kalan
pay 0,4→0,1 olur: önceki karantinadan geçen miktara göre %75 daha az geçiş. Karantinaya temas eden uydu geçişi
sıfırlanır. Karantinanın ekonomi/güven bedelini azaltmaz. Önkoşuluyla toplam 100 fon / 24 nominal gün; tehdit
görülmeden önce bu yatırım başka bir projeyi geciktirebilir.
[Karantina formülü](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Simulation.cs:194).

**Kamu Bilinçlendirme:** Güven yükselten bir gelir yatırımı değildir; güven kaybını önleyerek gelir tabanını
korur. Örneğin hediye reddinin hedef bölgedeki −15 güveni −7,5 olur. Açıklamadaki “Güven kayıpları −%50” bütün
kayıpları kapsamıyor: medya karalamasının −3'ü ve illüzyon ortaya çıkınca yaşanan −5 güven şoku yarılanmıyor.
Karantina ve sert müdahaleler sık kullanılıyorsa daha değerli; düşük kayıplı oyunda etkisi daha az görünür.
[Çarpan](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Simulation.cs:113),
[hediye reddi](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Simulation.cs:795),
[perde şoku](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Simulation.cs:601).

**Analog Yedekler:** En fazla yeni müdahale açan projedir. Bağımlılık azalması bir defalıktır; günlük bağımlılık
artışını yavaşlatmaz. Bütün denetimler 15 yerine 20 fon olur, 1 SG maliyeti korunur. Bağımlılığı en az 85 olan
bölgelerde normal denetimin yarıya düşen temizleme gücü saha denetiminde korunur. Saha denetimi gerçek bölgesel
veriyi gösterir ve devam eden illüzyonu bozabilir. 20 fonluk denetimli hediye, normal kabulün +15 bağımlılığı
yerine +5; temel +8 sızması yerine +4 getirir. Ekonomi +10 ve güven +5 yararı korunur. Temiz bölgede normal
kabul ayrıca en az 2 sızma tabanı oluşturduğundan normal/denetimli farkı daha büyüktür. Önkoşuluyla toplam
85 fon / 20 nominal gün.
[Tamamlanma](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Simulation.cs:403),
[denetim](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Simulation.cs:634),
[hediye](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Simulation.cs:776).

**Yorumlanabilirlik:** Çekirdek bilgisi Hizalama dışında Silme stratejisine de yarar. Bilinen çekirdeğe Son
Şalter maliyeti 30 yerine 20 SG olur; çekirdeğin bölgesi ayrıca tespit edilmemişse bile bu bilgi baskının tespit
koşulunu karşılayabilir. Diğer baskın koşulları korunur. Konum yalnız tamamlanma anında bulunur; çekirdek kaçınca
bilgi silinir ve otomatik yenilenmez. Sıradaki yetenek de yalnız o anda günlüğe yazılan tahmindir; sonraki
oyuncu eylemleri ve yeni Hizalama projesi YZ'nin tercihini değiştirebilir. Analist Masası tekillik göstergesindeki
“yaklaşık/±6” sunumunu kaldırır; normal durumda gösterilen sayısal değer zaten gerçek Tekillik'tir. İllüzyon
araştırma sonrası da göstergeleri yanıltabilir. Sürekli çekirdek takibi veya genel yalan tespiti sağlamaz.
[Analiz](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Simulation.cs:404),
[yetenek seçimi](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Simulation.cs:353),
[çekirdek kaçışı](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Simulation.cs:589),
[baskın](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Teams.cs:105),
[gösterge](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Desk/Scripts/Bridge/SimDeskBuilder.cs:375).

**Düzeltilebilirlik:** Ağ kaynaklı ve çekirdek yaşından gelen yeni Tekillik artışına birlikte ×0,75 uygulanır.
Birikmiş Tekillik azalmaz; sızma, yetenek satın alma ve bağımlılık doğrudan yavaşlamaz. Hızlanma yeteneğiyle
birleşince 1,5×0,75=1,125 olur: artış, Hızlanmasız ve araştırmasız duruma göre hâlâ %12,5 fazladır. Sabit baskı
varsayımında kalan süreyi yaklaşık üçte bir uzatabilir; gerçek oyunda baskı değişir. Önkoşuluyla 166 fon / 55
nominal gün. Desk dökümündeki sabit “−0,25” satırı gerçek etkiyi anlatmaz; katkı değişen artışın %25'idir.
[Tekillik](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Simulation.cs:235),
[döküm](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Desk/Scripts/Bridge/SimDeskBuilder.cs:391).

**Değer Kilidi:** Araştırma sürerken ek ara fayda üretmez; tamamlanınca Hizalama zaferini verir. Çekirdeğin yok
edilmesi veya bütün bölgelerin temizlenmesi gerekmez. Üçlü zincirin katalog bedeli 331 fon / 93 nominal gündür.
Sabotaj geri alınan ilerlemeyi tekrar finanse etmeyi gerektirdiği için gerçek harcama 331 fonu aşabilir.
[Zafer](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Simulation.cs:414),
[ilerleme ve sabotaj](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Simulation.cs:282).

## İlerlemenin katkıları gizlediği yerler

- **İki ayrı aşama sistemi var.** Araştırma panosundaki dört sütun sunum sırasıdır; önceki sütundaki bütün
  projeleri tamamlama şartı yoktur. Oyunun ilk dört öğrenme aşaması ise hangi tehditlerin etkin olduğunu belirler.
- Öğrenme açıkken ilk iki aşamada sahte haber üretimi kapalıdır; Doğrulama Ağı yine araştırılabilir ama asıl
  karşılığı olan sahte haber tehdidi yoktur. Üçüncü aşamada medya, dördüncüde illüzyon açılır. İlk aşamada Analog'un
  hediye ve illüzyon yararları da kullanılmaz; diğer yararları devam eder. Uydu Ağı bu aşamalarda kapatılmaz.
- Hizalama için yalnız YZ tespiti araştırma başlatmayı açar. İlerleme için izlenen ve tespit edilen bölgeler
  gerekir. Varsayılan hız her gözlemde +0,25; dört gözlemde tam hızdır. Gözlemsiz başlatılan proje ilerlemez.
- Aynı anda tek proje çalışır. Fon yetersizliği ve rezerv ilerlemeyi yavaşlatır; illüzyon bütün araştırmaları
  durdurur. Bu yüzden Analog, illüzyon başladıktan sonra araştırılarak acil çıkış yolu üretilemez.
- Varsayılan dünya ölçeği k=1,5, nominal süreleri büyütür. Dört sabit gözlem, yeterli fon ve sabotajsız durumda
  Hizalama zinciri yaklaşık 140 simülasyon günü; tek sabit gözlemde yaklaşık 558 gün gerektirir. Günlük tick
  ve tamamlanma yuvarlamaları yaklaşık değerlere eklenir.

Kaynaklar: [öğrenme kuralları](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Rules.cs:66),
[oyun başlangıcı](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Game/GameController.cs:43),
[başlatma koşulları](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Simulation.cs:751),
[hız](/Users/selimkamyan/Gits/Corrigible/Assets/Corrigible/Core/Simulation.cs:122).

## Geliştirme önceliği önerisi

1. Kartlarda gerçek kapsamı ve koşulu yaz: bölgeler arası yayılma, tek seferlik çekirdek analizi, uygulanmayan
   güven kaybı istisnaları. Düzeltilebilirlik dökümünü gerçek yüzde etkisiyle uyumlu göster.
2. Doğrulama ve Kuantum kartında mevcut tehdit bağlamını göster. Araştırma yapılabiliyor diye o anda faydalı olduğu
   izlenimi oluşmasın; geleceğe hazırlık ile mevcut krize müdahale anlaşılır olsun.
3. Yorumlanabilirlik için tasarım kararı ver: tek seferlik istihbarat açıklaması mı, sürekli güncellenen yetenek
   tahmini mi? Mevcut açıklama ikincisini düşündürüyor, kod birincisini yapıyor.
4. Yeni araştırma eklemeden önce her projenin tamamlanmasında oyuncuya somut kazanımı göster. Sadece yüzde
   değiştiren Anomali/Güçlendirilmiş/Bilinçlendirme/Düzeltilebilirlik projeleri, yeni işlem açan Analog ve
   Doğrulama kadar görünür geri bildirim üretmiyor.

Bu öneriler uygulanmadı. Fiyat değişikliğinden önce katkıların doğru ve görünür anlatılması önerilir.
