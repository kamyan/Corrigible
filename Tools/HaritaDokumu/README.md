# HaritaDokumu: üretilmiş haritanın görsel dökümü

Üretilmiş dünya haritasını (`Corrigible.Core.WorldMap`, `Balance.liberationMapGen` = 1) tohum tohum SVG ve PNG'ye döker.
Gerçek Core ile, Unity olmadan Mono'da koşar; oyun koduna girmez (Assets dışında).

```bash
Tools/HaritaDokumu/dokum.sh 1 20 /yol/klasor     # seed_01..20.svg/.png ve 5 sütunlu sheet.png
```

- `CORE=/yol/core` depodaki Core yerine bir kopyayı derler (üretici değişikliğini Assets'e koymadan önce dökmek için).
- Derleme çıktısı `Library/HaritaDokumu/dokum.exe` (git'e girmez). PNG'yi Chrome headless geçici bir profille çizer
  (`CHROME` ile değişir); kullanıcının Chrome profiline dokunmaz.
- Çizim: hücreler bölge rengiyle, kıyı kalın koyu, bölge sınırı ince, kablo bağlantıları kırmızı kesik, karargâh (S), her
  bölgenin en iç hücresinde ad ve "paket · kademe etiketi · karargâhtan uzaklık · hücre sayısı"; sağda parmak izi, bağlantı
  listesi ve dereceler. Ad kırmızıysa en iç hücre kalın değildir (olmamalı).
- Konsola tohum başına deneme, kıta, kara, bağlantı ve kablo sayısı ile parmak izi yazar.
