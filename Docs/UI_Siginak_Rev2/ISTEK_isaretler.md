Son Şalter · Revizyon 2 eki: eksik işaretler, makine biçimleri, niyet okları (Claude Design isteği)
Kullanım: çizginin altındaki metni Revizyon 2'nin yapıldığı Claude Design oturumuna yapıştır. Ek olarak şunları yükle:
`sprites/` klasöründeki 14 `glyph_*.png` ve `sprites/README.md`, `reference/screens/S9.jpg`, `reference/notes/S9.md`,
`reference/board_assets/mdl_*.png` (10 dosya), `reference/scene_threejs_rev2/raid.js`, `spec/raid_tokens.json`.

---

Revizyon 2 Unity'ye uygulanıyor. Uygulama sırasında paketin oyundaki içeriğin bir kısmını kapsamadığı ortaya çıktı:
oyunda 12 bölge özelliği ve 8 makine türü var, pakette 2 özelliğin ve 5 makinenin işareti çizilmiş. Bu istek yalnız eksik
parçalar için; Revizyon 2'nin geri kalanı (S1–S9, tokenlar, kararlar) olduğu gibi kalıyor. Yeni renk, yeni font, yeni
malzeme yok; S9'un kuralları geçerli.

Bu işaretler dört yerde görünür, hepsi aynı çizim:
- harita: cephe bloğunda saha işaretleri (dünya dokusunda 34 px, ekranda ~22 px) ve sağ alttaki işaretler kutusu;
- görev fişi: SAHA satırı, kare çerçeveli sürüm (`glyph_*_frame`, 30 px), yanında etiket (BÖLGE · İMZA) ve değer;
- baskın tahtası: makinenin önündeki can etiketi (18 px, #9A1E14), tür işareti olarak;
- S9 stil sayfası.

## 1. Eksik bölge özelliği işaretleri (10)

Pakette var: Dar koridor (`glyph_corridor`), Kum fırtınası (`glyph_sand`). Eksik olanlar, oyundaki adı ve kuralıyla:

| Dosya adı | Özellik | Oyundaki kural (tahtaya etkisi) |
|---|---|---|
| glyph_safe | Güvenli giriş | İlk tur hiçbir robot uyanmaz. |
| glyph_short | Kısa nöbet | Baskın bir tur kısa sürer. |
| glyph_reinforce | Takviye | Başta bir robot fazla. |
| glyph_open | Açık arazi | Duvarların bir kısmı yıkık: atış hatları uzun. |
| glyph_cooling | Soğutma ağı | İki soğutma ünitesi fazla; kırılan ünite çevresine patlar. |
| glyph_crowd | Kalabalık şehir | İki sivil sunucu fazla; GÜVEN kolay düşer. |
| glyph_armored | Zırhlı dolap | Hedef dolapların bir canı fazla. |
| glyph_grid | Elektrik şebekesi | Fazladan elektrikli kanal: üstüne düşen 1 hasar alır. |
| glyph_factory | Kopya fabrikası | Bir kopya dolabı fazla. |
| glyph_patrol | Hızlı devriye | Robotlar bir kare fazla yürür. |

## 2. Eksik makine türleri (3): işaret + 3D biçim

Pakette var: Güvenlik dronu, Bakım robotu, Kilitleyici, Havan robotu, Kalkan dronu. Eksik olanlar:

| Dosya adı | Makine | Can · zırh · yürüyüş | Kural | İmza olduğu bölgeler |
|---|---|---|---|---|
| glyph_armordrone | Zırhlı dron | 2 · 1 · 3 | Dron gibi düz hatta ateş eder; zırhı ilk hasarı karşılar. | Batı Avrupa (ilk cephe), Okyanusya |
| glyph_bomber | Patlayan robot | 1 · 0 · 3 | Yanına sokulur ve patlar: çevresindeki dört kareye 1 hasar. | Güney Amerika, Orta Doğu |
| glyph_replicator | Çoğaltıcı | 2 · 0 · 2 | Yanındaki boş kareye yeni bir dron uyandırır. | Çin, Güneydoğu Asya |

Zırhlı dron oyuncunun ilk baskınlarında karşılaştığı makinedir; işareti ve biçimi Güvenlik dronundan açıkça ayrılmalı,
ama aynı ailede olduğu da okunmalı.

Her biri için S9-A'daki gibi:
- 3D biçim: gövde ortak (#5B5E62 / yan #46494D / gölge #34373A), tür biçimden okunur, kırmızı (#9A1E14) yalnız göz ve
  halkada; tepe yüksekliği (birim) ve kare içindeki taban ölçüsü;
- `raid.js`'e model kodu (öteki beşinin yanına) ve `mdl_armorDrone.png`, `mdl_bomber.png`, `mdl_replicator.png` görüntüsü;
- `raid_tokens.json → machineTypes`'a birer satır (işaretin tarifi, ötekiler gibi: “X + 4 rotor halkası”).

## 3. Eksik niyet okları (4)

S9-C dört niyeti çiziyor: ateş, yakın dövüş, kilit, onarım (+ belki, tarandı). Oyunda dört niyet daha var; her biri
gözcünün bildirdiği bir hamle olarak tahtada önceden görünmeli, sıra bayrağı ve yürütme durumları (bekliyor, uygulanıyor,
bitti, iptal) ötekilerle aynı:

| Niyet | Kim | Ne olur | Tahtada gösterilmesi gereken |
|---|---|---|---|
| Havan atışı (`lob`) | Havan robotu | Üç kare öteye, engellerin üstünden atar: hedef kareye 1 hasar. | Engelin üstünden geçen yüksek yay ve hedef kare |
| Patlama (`blast`) | Patlayan robot | Bulunduğu yerde patlar: çevresindeki dört kareye 1 hasar. | Etkilenen dört kare (alan), ok yok ya da çok kısa |
| Kalkan (`shield`) | Kalkan dronu | Yanındaki makineye zırh verir (zırhsız olana). | Kalkan alacak makine; saldırı okundan ayrılmalı (hasar yok) |
| Kopyalama (`clone`) | Çoğaltıcı | Yanındaki boş kareye yeni bir dron uyandırır. | Dronun uyanacağı boş kare; uyanma halkasıyla akraba |

Kırmızı yalnız oyuncuya ya da sivile zarar veren niyet için kalmalı; kalkan ve kopyalama zarar vermez, bunlar için
renk dışında bir ayrım (çizgi türü, rozet) öner. Rozet gerekiyorsa `badge_clamp` / `badge_wrench` ölçüsünde
(56 / 48 px, yarı boy gösterilir).

## 4. Kurallar (değişmez)

- İşaretler 72 × 72 PNG, saydam zeminde beyaz (Unity'de renge boyanır), 36 birimlik ızgarada çizgi 2,4–4,4; ayrıca kare
  çerçeveli sürümü (`_frame`). Ötekilerle aynı ağırlık ve aynı ölçü.
- 18 px'te (baskın etiketi) ayırt edilebilir olmalı; gri tonlamada ve yan yana dizildiğinde birbirine karışmamalı.
- İşaret harfe ya da sayıya dayanmasın (TR ve EN'de aynı kalmalı).
- Renk anlamı aynı: kırmızı = işgal ve tehdit, mavi = geri alınmış, yeşil = iyi, sarı = seçili. Neon, hologram yok.
- Shader yok: oklar ve alanlar düz geometri ya da sprite olmalı (uygulama `RaidArrow` mesh'i ve UI/Default malzemesi).

## 5. İstenen çıktı

1. 13 işaretin PNG'leri (26 dosya: düz + `_frame`), `sprites/README.md`'ye eklenecek satırlar.
2. Üç makinenin `raid.js` kodu, `mdl_*.png` görüntüleri, tepe yükseklikleri (README'deki listeye).
3. S9'un güncellenmiş panosu: A'ya üç makine, C'ye dört niyet, H'ye on bölge özelliği (yan yana, gri tonlama kopyasıyla).
4. `raid_tokens.json` için yeni tokenlar (`machineTypes` satırları, `intents.lob/blast/shield/clone`), `_new_1_2` listesiyle.
5. Kısa not: her işaretin neyi çizdiği ve neden (oyuncu bir kez öğrenip haritada, fişte ve tahtada tanısın diye).
