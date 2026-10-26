# three.js referans sahnesi → Unity

Bu klasör, `reference/renders/*.webp` ve `reference/screens/*.png` görüntülerini üreten three.js sahnesidir.
Unity'de **oda yerleşiminin ve nesne ölçülerinin tek doğru kaynağı** budur: konum, boyut, açı ve malzeme
değerlerini buradan oku, tahmin etme. Doku kodu (`textures.js`) yalnızca görünüş örneğidir; Unity'de
yazılar TMP ile, dokular sprite / küçük PNG ile kurulur.

| Dosya | İçerik |
|---|---|
| `lib.js` | Koordinat yardımcıları (`U`, `group`, `lbox`, `lcyl`, `lsphere`, `lplane`) ve malzeme paleti `M` |
| `room.js` | `buildRoom()` — odanın tamamı: kabuk, masa, sayaç rafı, anahtar rayı, şalter, teleks, lamba, raf + kesit maketi, CRT masası, ışıklar |
| `textures.js` | Plaka, sayaç yüzü, teleks kâğıdı, CRT ekranı, afiş, not kâğıtları için canvas dokuları (görünüş örneği) |
| `mapk1.js` | Harita dokusu: bölge durumları (işgal / cephe / aday / özgür / seçili), etiketler, lejant |
| `render.html` | Poz başına sahne seçenekleri, ekip figürü konumları (`TEAM`) ve kameralar (`P` nesnesi) |
| `alt.js` | Karar panolarındaki kullanılmayan B seçenekleri (defter, mühür, ozalit pafta) — yalnız karşılaştırma için |

`room.js` içindeki `opts.alt === 'ledger' | 'stamp'` dalları, karar panolarındaki **kullanılmayan B seçenekleridir**; Unity'ye taşıma.

## Koordinat dönüşümü (önemli)

Yardımcılar Unity tarzı koordinat alır: **x sağ, y yukarı, z ileri (arka duvara doğru)**, metre.
Oyuncunun koltuğu z ≈ 0 tarafında, arka duvar z = 2,30'da.

| three.js çağrısı | Unity karşılığı |
|---|---|
| `U(x, y, z)` | `new Vector3(x, y, z)` (aynı sayılar) |
| `group(parent, x, y, z, yaw)` | boş GameObject, `localPosition = (x, y, z)`, `localEulerAngles = (0, +yaw, 0)` |
| `lbox(parent, w, h, d, mat, x, y, z, {rx, ry, rz})` | Cube, `localScale = (w, h, d)`, `localPosition = (x, y, z)`, `localEulerAngles = (−rx°, −ry°, −rz°)` (kodda radyan) |
| `lcyl(parent, r, h, mat, x, y, z, {...})` | Cylinder, `localScale = (2r, h/2, 2r)` (Unity silindiri 2 birim boy), açı kuralı aynı |
| `lsphere(parent, r, mat, x, y, z)` | Sphere, `localScale = 2r` |
| `lplane(parent, w, h, mat, x, y, z, {...})` | Quad (ön yüzü koltuğa, −Z yönüne bakar), `localScale = (w, h, 1)`, açı kuralı aynı |
| `new THREE.Group(); g.position.copy(U(...)); g.rotation.x = a` | `localEulerAngles.x = −a°` |
| `obj.position.set(x, y, z)` (U **olmadan**, ham three.js) | `localPosition = (x, y, −z)` — z işareti ters (ör. `dio.position.set(0.20, 0.78, 0.01)` → `(0.20, 0.78, −0.01)`) |
| `obj.rotation.x/y/z = a` (ham) | `localEulerAngles` ilgili eksen `−a°` |
| Çok malzemeli kutu (`[M.wood, …, mat({map})]`) | Kutu tek malzeme + etiketli yüze ayrı Quad (TMP ya da sprite) |

Kural özeti: **yardımcıyla verilen konum aynen; ham `position.set` ise z ters; bütün açıların işareti ters; yaw (group) aynen.**
Tek eksenli dönüşler dışında (birkaç kalem/çubuk) sıra farkı önemsiz; şüphede ekran görüntüsüyle karşılaştır.

Örnek: `group(root, -1.50, 0, 1.72, -35)` (sol raf) → `localPosition (−1.5, 0, 1.72)`, `localEulerAngles (0, −35, 0)`:
rafın önü sağa-ileri, koltuğa döner. CRT masası `(1.50, 0, 1.72)`, yaw `+35`.

## Ana bölümler (`room.js`)

- **room shell** — duvarlar 4,4 × 2,5 (arka z 2,30), yan duvarlar x ±2,2, zemin/tavan; kanal, borular, buat; duvar yazısı “KYGA / SIĞINAK 07”, doktrin afişi, mantar pano.
- **ceiling bulb** — `(0.12, 2.02, 1.70)`, tel kafes, kablo.
- **main desk** — `group(root, 0, 0, 0, 0)`; üst yüzey y = 0,76.
- **counter bank** — arka kenar: bakalit gövde + 45° ön yüz (yüz düzlemi `(0, 0.8657, 1.7388)`, 1,56 × 0,212, `rx −45°`); 5 modül, modül genişliği 0,308; pirinç çerçeveler.
- **map paper** — 1,20 × 0,675, merkez `(0, 0.7612, 1.3175)`, yatay. Haritanın World Space Canvas'ı bu dikdörtgene oturur (1920 × 1080 → 1600 px/m).
- **team figure** — `buildTeamFigure(desk, tf)` + `opts.teams` (harita px konumu, durum `ready | injured`, `ring` = seçili, `lift` = taşınırken yükseklik). Pirinç oval taban (r 0,035, z ölçeği 0,68 → 70 × 48 mm, 6 mm), 3 piyon (x −0,019 · 0 · 0,019; arkaya 7 mm), önde bakalit harf plakası. Harita px → dünya: `x = −0,60 + px/1920 × 1,20`, `z = 1,655 − py/1080 × 0,675`, `y = 0,7616`. Konumlar `render.html → TEAM` (cephe: harita px (1000, 372); S-07: (560, 380)). Yaralıda üçüncü piyon yatık.
- **key rail** — `(−0.075, 0.775, 0.865)`, 12° eğik; tuş x: −0,58 · −0,33 · −0,08 · 0,17 · 0,42 (her biri −0,075 kaydırılmış başlık).
- **shift switch** — `group(desk, 0.765, 0.76, 0.885, 10)`; gövde 0,30 × 0,09 × 0,20, kapak menteşesi arka kenarda.
- **teleprinter** — `group(desk, −0.77, 0.76, 1.20, −12)`; kâğıt önden düşer, masaya serilir.
- **desk lamp** — `group(desk, 0.84, 0.76, 1.61, 30)` (sağ arka; K4 kadrajına girmez).
- **right props** — evrak tepsisi `(0.765, 0.76, 1.30)`, damga rafı, kupa, kalem + talaş.
- **left shelf** — `group(root, −1.50, 0, 1.72, −35)`: üst raf malzemeleri, umut işareti (filizli teneke), kesit maketi, sağda kıskaçlı pano, önünde BOŞTA tepsisi.
- **CRT desk** — `group(root, 1.50, 0, 1.72, 35)`: CRT kasası, ekran, klavye, tüplü telsiz.
- **diorama** (`buildDiorama`) — çerçeve, yüzey + YZ direği, toprak katmanları, 2 × 2 oda + şaft + merdiven, oda eşyaları, piyonlar.
- **lights** — hemisphere, tavan ampulü (gölgeli), masa lambası spot (gölgeli), CRT sızıntısı, telsiz kadranı, çalışan odalarda küçük nokta ışıklar. Değerler `spec/tokens_siginak.json → lights` (preview + urpStart).

## Kameralar (`render.html → P`)

`spec/tokens_siginak.json → cameras` ile aynıdır. K1, lens shift'li fiziksel kamera (`shiftCam`):
yükseklik 1,52, z −0,15, dikey FOV 56,7°, lens shift Y −0,324 (ufuk çizgisi üstten 190. pikselde).
Harita pozları (K2, K3, K6b, K7) neredeyse dik bakış: `(0, 2.75, 1.30) → (0, 0.76, 1.36)`, FOV 30°.
