# three.js referans sahnesi · Revizyon 2 (1.2 eki dahil) → Unity

Bu klasör, `reference/screens/` ve `reference/board_assets/` görüntülerini üreten three.js sahnesidir. Revizyon 2'de
**yeni ya da yeri/ölçüsü değişen nesnelerin** konum, boyut, açı ve malzeme kaynağı budur; tahmin etme, buradan oku.
Doku kodu (`textures.js`, `tex2.js`, `map2.js`) yalnız görünüş örneğidir: Unity'de yazılar TMP ile, işaretler
`sprites/` ile kurulur. Kodu çalıştırman gerekmez; okumak yeter.

İlk turun sahnesi (`UI_Siginak/reference/scene_threejs/`) hâlâ geçerli; bu klasör onun üstüne gelir. Çelişkide bu klasör kazanır.

## Koordinat dönüşümü (ilk turla aynı)

Yardımcılar Unity tarzı koordinat alır: **x sağ, y yukarı, z ileri (arka duvara doğru)**, metre. Koltuk z ≈ 0, arka duvar z = 2,30.

| three.js çağrısı | Unity karşılığı |
|---|---|
| `U(x, y, z)` | `new Vector3(x, y, z)` (aynı sayılar) |
| `group(parent, x, y, z, yaw)` | boş GameObject, `localPosition = (x, y, z)`, `localEulerAngles = (0, +yaw, 0)` |
| `lbox(parent, w, h, d, mat, x, y, z, {rx, ry, rz})` | Cube, `localScale = (w, h, d)`, `localPosition = (x, y, z)`, `localEulerAngles = (−rx°, −ry°, −rz°)` |
| `lcyl(parent, r, h, mat, x, y, z, {...})` | Cylinder, `localScale = (2r, h/2, 2r)` |
| `lsphere(parent, r, mat, x, y, z)` | Sphere, `localScale = 2r` |
| `lplane(parent, w, h, mat, x, y, z, {...})` | Quad (ön yüzü −Z'ye bakar), `localScale = (w, h, 1)` |
| `obj.position.set(x, y, z)` (U **olmadan**) | `localPosition = (x, y, −z)` — z işareti ters |
| `obj.rotation.x/y/z = a` (ham) | ilgili eksen `−a°` |

Kural: **yardımcıyla verilen konum aynen; ham `position.set` ise z ters; bütün açıların işareti ters; yaw (group) aynen.**

## Dosyalar ve Revizyon 2'de değişenler

| Dosya | İçerik · Revizyon 2 |
|---|---|
| `room.js` | `buildRoom()` — oda. **Yeni/değişen:** 6 modüllü sayaç rafı (GÜVEN), FON'da pirinç virgül + açık renk onda bir çarkı; KURTARILAN BÖLGELER mantar panosu (başlık kartı + 12 kart yuvası + kurşun kalem “sıradaki hedef”); YZ alarm panosu 0,48 × 0,29 + lamba (≥ 8 yanar); tebeşir vardiya çentiği; 6 tuşlu anahtar rayı (4 · KAPI: amber mücevher + el yazısı fiş; `opts.doorBox` = Karar 2 B seçeneği, kullanılmaz); teleks ön kâğıdı 0,26 × 0,19; CRT boşta 4 satır; telsiz yüzü, mikrofon, kayıt defteri; özet panosu **−0,46 · 0,86 · −0,02** (raf yereli, eskisi −0,40 · 0,78) + ahşap ayak + BOŞTA tepsisi −0,46; geç harekât katmanları (`stage: 'late'`: umut bitkisi 3 aşama, teleks rulosu, yırtık kâğıt yığını, EVRAK yüksekliği, kupalar) |
| `door.js` | `buildDoor()` — sağ duvarda kapı (x 2,2, z 0,32–1,18, h 2,0): çelik kasa, tehlike bandı, vuruş lambası (amber; ölümcülde kırmızı), gözetleme deliği + kapak, el çarkı, şablon 07, interkom, tebeşir ziyaretçi çentiği, aydınlık merdiven boşluğu; `visitor(root, x, z, kind)` altı siluet: `anne · yasli · silahli · asker · yarali · kopya` |
| `diorama.js` | `buildDiorama2()` — kesit maketi. **Yeni:** üst bantta DEPO (1 sandık = 3 erzak, boş yer gölgesi; 1 kova = 5 malzeme; 3 yuvalı plaka rafı; tebeşir tahtası `A.depotSlate`); `topBand: 'surface'` = Karar 3 B (kullanılmaz); oda çıktıları (ranzada yedek uzman, telsiz başında görevliler, döküm kalıpları ve plakalar, revirde sargılı hasta); oda kartı 0,23 × 0,057; seçili oda sarı çerçeve |
| `tex2.js` | Revizyon 2 dokuları: sayaç yüzü, alarm panosu, pano başlık/kart, CRT ekranı, teleks ön kâğıdı, oda kartı, telsiz yüzü, kayıt defteri, tebeşir çentiği; `S2` = TR/EN metinler |
| `map2.js` + `map/paths.json` | Harita dokusu 1920 × 1080: 12 bölge yolu (`paths.json`, Miller projeksiyonu), etiket yerleri (`LAB`), özgür damgası yerleri (`STAMP`), tarama, cephe konturu, sektör işaretleri (38 px), saha işaretleri (34 px, `glyph()`), işaretler kutusu + TEHDİT kademesi, yeniden işgal ✕ ve damgası |
| `textures.js` | İlk turdan; paylaşılan doku yardımcıları |
| `states.js` | Görüntülerin durum fikstürleri (sayılar ekran görüntülerinden; örnek): V.001, V.014, V.015, V.015 başarılı/başarısız, V.015 karşı saldırı, V.031, kazanılan, kaybedilen |
| `render.html` | Poz seçimi ve URL parametreleri: `pose=K1|MAP|K4|K5a|DOOR`, `s=<durum>`, `lang=tr|en`, `sel=<oda>`, `bake=0` (oda kartı yazısız), `band=store|surface`, `door=closed|open|fatal`, `visitor=<tür>`, `peep=1`, `doorBox=1`. `anchors` ve `out.quads` = yazı yerleşimi için ekran izdüşümleri |
| `raid.js` + `raid.html` | Baskın tahtası: ozalit pafta + kontrplak, parçalar ve tepe yükseklikleri, makine modelleri (`drone · maint · locker · mortar · shieldDrone`; **1.2:** `armorDrone · bomber · replicator`), piyonlar (normal · tur bitti · bantlı · plakalı · düşmüş), işaretler (hareket, isabet, seçili, uyanma; **1.2:** `ghostWake` = kopyalama hedef halkası), S6/S7 dizilimi. **1.2:** `raid.html?s=intents` dört köşede havan atışı, patlama, kalkan, kopyalama sahnesi (S9-C2 görüntüleri bundan kesildi; oklar görüntünün üstüne çizildi, değerler `intents.*`); `s=model&k=<tür>&w=440&h=440&cu=0.5&cv=0.5&ppu=300&cy=250` model görüntüsü (`mdl_*.png`), sonuç JSON’unda `bbox` |

## Pozlar

- K1, harita, K4, K5a: `spec/tokens_siginak.json → cameras` (değişmedi).
- Kapı: `cameras.door` → pos (0,15 · 1,75 · 0,55), hedef (2,2 · 1,28 · 1,0), dikey FOV 62°. Bugünkü kapı pozuyla eşdeğer; oyundaki değer farklıysa oyunu koru.
- Operasyon raporu ve son rapor: yeni poz yok, harita pozu.

## Yerleşim için izdüşüm formülleri (referans görüntüleri için)

- **Harita pozu, harita dokusu → ekran:** `X = 352 + 0,633 · tx`, `Y = 241 + 0,633 · ty` (1920 × 1080).
- **Baskın tahtası, kare → ekran** (referans görüntüdeki orto kamera): `x = 210 + (u + v + 1) · 93,75`,
  `y = 528 + (v − u) · 53,775 − h · 108,62`; u = sütun (A = 0 … H = 7), v = satır dizini (0 = 8. satır … 7 = 1. satır),
  h = tepe yüksekliği (birim). Referans kamera oyunun bugünkü baskın ekranına oturtuldu (orto 35° / 45°, 132,58 px/birim,
  tahta merkezi 960 × 528). `raid_tokens.json → camera` (88,39 px/birim, `screenFromGrid1080`) oyunla uyuşmayabilir:
  **oyundaki kamera kazanır**; token'ı değiştirme, farkı Aşama 0'da not et. Bu formül yalnız görüntülerdeki işaretlerin
  yerini okumak içindir; Unity'de yer, dünya konumunun kamerayla izdüşümünden gelir.
- Parça tepe yükseklikleri: duvar 0,55 · sivil sunucu 0,66 · soğutucu 0,42 · kopya dolabı 0,8 · hedef 0,9 · dron 0,52 ·
  bakım 0,77 · kilitleyici 0,7 · havan 0,82 · kalkan dronu 0,6 · **1.2:** zırhlı dron 0,53 · patlayan robot 0,66 · çoğaltıcı 0,55 · piyon 0,62 (düşmüş 0,25).

## Taşınmayacaklar

`opts.doorBox` (Karar 2 B) ve `topBand: 'surface'` (Karar 3 B) yalnız karşılaştırma içindir; kullanıcı aksini söylemedikçe Unity'ye taşıma.
`opts.alt = 'ledger' | 'stamp'` ilk turun reddedilen seçenekleridir (defter, damga); hiç taşıma.
