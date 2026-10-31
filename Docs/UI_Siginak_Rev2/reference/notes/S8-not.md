# S8 · SON RAPOR
*YENİ EKRAN · HARİTA POZU, KARARTILMIŞ*

## YAPI

- **kâğıt** — Tam ekran rapor (1620 × 952) son haritanın üstünde; harita %62 karartılır, kenarlarda son durum seçilir.
- **damga** — KAZANILDI yeşil. KAYBEDİLDİ kırmızı + ikinci, küçük neden damgası: DAĞILMA, VURULDU ya da SIZMA.
- **göstergeler** — SÜRE (vardiya), BASKIN (başarılı · başarısız), ÖZGÜR BÖLGE (x/12 + sektör), SIĞINAK NÜFUSU.
- **işgal şeridi** — X ekseni harekâtın uzunluğuna ölçeklenir: 1–N her zaman tam genişlik, kısa harekât sola sıkışmaz. Kırmızı alan işgal (yazıyla İŞGAL), mavi zemin geri alınan (yazıyla GERİ ALINAN); karşı saldırılar yukarı çıkıntı olarak görünür.
- **kırılma anları** — Grafikte numaralı yuvarlak, altta aynı numarayla liste (V.### + olay). En çok beş-altı: ilk özgür bölge, karşı saldırılar, kaybın nedeni (güven düşüşleri, ölümcül kapı), son olay.
- **VURULDU, SIZMA** — Aynı kâğıt; neden damgası değişir, son kırılma anı kapı olayıdır (“V.029 · Kapı açıldı · silahlı ziyaretçi”).
- **öneri** — Dağılma'da grafiğin altına ince GÜVEN şeridi (20 → 0): kaybın nedeni tek bakışta görünür.
- **sayılar** — Baskın sayısı, nüfus ve kırılma anları örnek; oyunun harekât kaydı kullanılır.

## NESNELER

- **kâğıt** — Overlay, #F1E9D6, başlıkta 3 px çift çizgi; 9 dilim; gölge (6, 8) %50.
- **damgalar** — Sonuç CP 700 80–88, 8 px çift kenar, −4°. Neden CP 700 28, 3 px, +3°.
- **gösterge** — 1,5 px #463F34, zemin #F7F1E1; değer CP 700 46, açıklama LF 17.
- **grafik** — Alan 1500 × 150; çizgi 3 px #9A1E14; işgal dolgusu #9A1E14 %22; geri alınan #1A4789 %10; eksen CP 700 14; işaret Ø24.
- **grafik · Unity** — uGUI'de özel Graphic: OnPopulateMesh ile alan ve çizgi şeritleri (dörtgen zinciri). Özel shader ve RenderTexture gerekmez.

## ETKİLEŞİM VE DURUMLAR

- **YENİDEN BAŞLA** — Enter. Normal: mürekkep. Üzerinde: 2 px yükselir. Basılı: 2 px iner. Devre dışı ve yetersiz kaynak durumu yok.
- **grafik işareti** — Üzerinde: listedeki satır sarı zemin alır (sarı = seçili), işaret 1,2 kat büyür. Satırın üzerine gelince de grafikteki işaret büyür.

## HAREKET VE SES

- **açılış** — Kamera harita pozuna 0,6 s → harita 0,5 s'de kararır → kâğıt aşağıdan 0,4 s'de gelir.
- **damga** — 0,15 s; kazanınca uzun zil, kaybedince boğuk “tak”; neden damgası 0,3 s sonra.
- **göstergeler** — Sırayla 0,12 s; sayılar daktiloyla.
- **grafik** — Çizgi soldan sağa 1,2 s'de çizilir (teleks sesi); işaretler çizgi geldikçe 0,1 s'de belirir; liste satırları 0,08 s arayla.

## İNGİLİZCE

- **başlık** — KYGA · FINAL CAMPAIGN REPORT · SHELTER 07 · single copy · WON · LOST · DISBANDED · SHOT · INFILTRATED
- **metin** — “All twelve regions are free. The AI's last server fell on shift 48.” · “Civilian trust hit zero; Shelter 07 disbanded.”
- **gösterge** — DURATION · shifts · RAIDS · successful · failed · FREE REGIONS · sectors retaken · SHELTER POPULATION · people left
- **grafik, düğme** — OCCUPATION · SHIFT 1–48 · OCCUPIED · RETAKEN · TURNING POINTS · RESTART
