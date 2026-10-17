# Saha raporları ve ek operasyon ödeneği

2026-10-15. DeskGame haritasında küçük rapor fişleri toplanır. Amaç, izleme ağı ve ekip yerleşimini görünür kılmak,
oyuncuya küçük bir fon fırsatı vermek ve ana ekonomiyi korumaktır.

## Kurallar

| Ayar | Varsayılan |
|---|---:|
| Fiş başına ödenek | 1 fon |
| Ek ödenek tavanı | Kapsam varken kazanılan düzenli gelirin %8'i |
| Birikebilen kullanılmamış ödenek | 2 fon |
| Aynı anda bekleyen fiş | Bütün haritada 2; bölge başına 1 |
| Küresel üretim kontrolü | 6×k günde bir |
| Aynı bölgenin beklemesi | 12×k gün |
| Ödenek süresi | 30×k gün |
| Görünen sızmanın ağırlık katkısı | En fazla +%50; %60'ta doyar |
| Ekip ve ağ birlikteyse seçim ağırlığı | ×1,25; ekip sayısıyla çoğalmaz |

Süreler mevcut `Dur` yuvarlamasını kullanır. Varsayılan k=1,5 için kontrol 9 gün, bölgesel bekleme 18 gün,
fiş ömrü 45 gündür. Kontrol anında yeterli ödenek ve uygun bölge yoksa fiş doğmaz.

Uygun bölge çevrimiçi ve ittifak üyesi olmalıdır. İzleme ağı **veya** bölgede yerleşmiş, yaralı/kuruluyor olmayan
bir ekip gerekir. Yolculuktaki ekip kaynak ve varış bölgesine kapsam sağlamaz. Yerel görev yapan ekip kapsamı korur.

Her gün uygun bölge varsa, o gün gerçekten ödenen normal fon gelirinin %8'i ayrı fırsat havuzuna yazılır. Mevcut
gelirden kesinti yapılmaz. Fiş doğarken 1 fon bu havuzdan ayrılır; sadece başarılı toplamadan sonra bakiyeye eklenir.
Bu nedenle `ödenen ödüller + bekleyen ödüller + havuz ≤ kapsamlı günlerdeki düzenli gelir × 0,08`.
Havuz dolması, süre aşımı veya erişim kaybı bu eşitsizliği daha da aşağı çeker. Bağış, fiş ödülü ve diğer peşin
işlemler havuzu beslemez; bileşik ödül üretilemez. Başlangıç fonu da hesaba girmez.

Bölge seçiminin ağırlığı `1 + 0,5 × clamp(görünenSızma / 60, 0, 1)` olur. Ekip ve izleme ağı birlikteyse ×1,25
uygulanır. Bilinmeyen sızma temel ağırlığı kullanır. Yüksek sızma toplam bütçeyi veya tek fişin ödülünü artırmaz;
hangi bölgeden daha sık rapor geleceğini etkiler. Temiz bölgeler de rutin rapor üretir.

## Haritada kullanım

Fişler bölge düğümünün yakınında, düğüm ve ad etiketlerinden uzak bir boşluğa yerleştirilir. Küçük krem kâğıtta
RAPOR ve +1 görünür. Üstüne gelince bölge, rapor tarihi, başlık ve kalan ödenek süresi gösterilir. Tıklama haberi
telekse/arşive ekler ve fonu bir kez öder. Bildirim ödeneği gösterir; fon işlem defterinde ayrıca görünür.

Süre dolarsa haber özgün tarihiyle arşivlenir, fon ödenmez. Bölge çevrimdışı/ittifak dışında kalırsa veya tek kapsam
sağlayan ekip ayrılırsa fiş erişilemez; sonraki gün kaldırılır. Ödenek havuza geri dönmez. Yeniden tıklama, oyunu
kaydedip açma veya ekip getirip götürme zamanlayıcıları sıfırlamaz.

Üretim ve ömür simülasyon günlerine bağlıdır. Duraklatma, zamanı tutan pencereler, hediye/olay kararı ve baskın
sırasında ilerlemez. Kritik operasyon bildirimleri ve genel yayınlar mevcut bildirim yollarını kullanır; saha
raporları yalnız fiş toplandığında veya ödenek süresi bittiğinde haber arşivine geçer.

## Algı, kayıt ve uyumluluk

Raporun bölgesi, sıklık ağırlığı ve metni yalnız `Shown` değerini kullanır; gerçek sızma, çekirdeğin yeri ve haberin
gizli doğru/sahte bilgisi kullanılmaz. İllüzyonda fişler görünür kalır, yanlış raporlar da aynı ödeneği verir.
Ödenek doğruluk kanıtı değildir. `field` türü haber bir saha tahminidir; kesin oran için bölge denetimi gerekir.

Bu özellik Unity'ye özgü `RuleOptions.FieldReports` seçeneğidir. Kapalıyken yeni RNG çağrısı veya gelir değişikliği
yoktur; temel ve deneysel JS paritesi korunur. Yeni Unity oyunlarında açıktır. Bütünleşik ekonomi kullanan eski
kayıt, yükleme gününde `reports_enable` komutuyla geçer; geçmiş günleri değiştirmez. Eski ekonomi v1/v2 korunur.
`report` komutu toplama kimliğini kaydeder; üretim, seçim, süreler ve ödeme tekrar oynatmada aynı şekilde oluşur.

Denge değerleri `Core/Balance.cs` ve `Settings/BalanceConfig.asset` içindedir. Motor kuralları `Core/FieldReports.cs`,
sunum köprüsü `SimDeskBuilder.FieldReports.cs`, kalıcı fiş şablonu `Desk/Scenes/DeskGame.unity` içindedir.

%8 bir matematiksel tavan, insan oyuncuyla tamamlanmış denge çalışması değildir. Toplama oranı ve oyun hızına
göre gerçek katkı daha düşüktür; ilk oynanışlarda fişlerin fark edilmesi ve dikkat maliyeti ayrıca izlenmelidir.

## Kontrollü ekonomi ölçümü

192 senaryo: 0/1/3/12 izlenen bölge × k=1/1,5/2 × toplama/kaçırma × 8 tohum. Her senaryo 240 temel gün
çalıştı. YZ büyümesi, yayılması, tekillik, hesaplama ve bağımlılık artışı kapatıldı; varsayılan fon, güven,
haber ve rapor kuralları korundu. Ağlar başlangıçta hazır; ağ kurma maliyeti ölçüme dahil değildir.

- Kapsam yok: rapor ve ek ödeme yok.
- Tek bölge: toplanan fişlerle düzenli gelire ortalama %3,06 ek; oyun başına 20 fiş.
- 3 veya 12 bölge: ortalama %5,80 ek; yaklaşık 37,9 fiş.
- Bütün koşullarda günlük bütçe eşitsizliği geçti; tek koşuda en yüksek ek gelir %7,98.
- Fişler toplanmazsa ek ödeme sıfır. Süre/gelir aynı k ile ölçeklendiğinden üç zaman ölçeğinde benzer sonuç var.

Bu, fon sınırının ölçümüdür; rakiplerle gerçek maç kazanma oranı veya insan oyuncu deneyi değildir. JS
prototipi özelliği açmadığı için önce/sonra aynı 200 tohumlu bot ve parite çıktıları byte düzeyinde eşleşti.
Ayrıntılar: `SAHA-RAPORLARI-DENGE-SONUCLARI.json`.
