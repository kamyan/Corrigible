# YZ küresi — algoritma ve Unity karşılığı

Kanonik kaynak `art/orb/fx_orb.js`'dir (kanvastaki 19 karede çalışan motorun aynısı). Bu belge onu satır satır Unity'ye taşımak için yazıldı. Belgeyle kod çelişirse **kod doğrudur**.

- Canlı inceleme: `tools/orb_lab.html` (tarayıcıda aç; değer, tür, yarıçap, OTOMATİK).
- Belirlenimci referans: `reference/orb/golden/` (22 kare, `golden.json`). Üretimi: `fxOrbs(document, {manual: true}).advance(1/40, n)`.
- Sayısal sabitler makinece okunur biçimde `spec/orb/orb_constants.json`'da.

---

## 1. Ne gösterir

Bir bölgedeki YZ sızmasını (0–100) tek bir dönen küreyle gösterir. Üç hâl vardır. Aralarında sert geçiş yoktur; her katmanın ağırlığı sızmaya göre yumuşak bir eğriyle değişir.

| Hâl | Sızma | Görünüm |
|---|---|---|
| 1 · UYUMLU | 0–30 | altın veri küresi: enlem/boylam telleri, iki yörünge halkası, enlem bantlarında nokta dizileri, yanıp sönen veri blokları; yavaş dönüş |
| 2 · KARARSIZ | 30–70 | altın ve mavi parçacıklar karışır, kabuk titrer ve dağılır; çekirdekte dönen ışın yıldızı; dışarı uzanan şimşek lifleri ve yaylar |
| 3 · UYANMIŞ | 70–100 | buz mavisi, sivri kristal kabuk (ikosküre); kenarlarda çıtırtı, uçlarda ışık, yörüngede kırıklar, yoğun kıvılcım; hızlı dönüş |

Özel türler: `unknown` (gri, parazitli, değer yok), `quar` (karantina: nane kafes, yavaş, %72 parlaklık), `core` (çekirdek bölge: iki şok halkası).

## 2. Girdiler

| Alan | JS (`data-*`) | Unity | Not |
|---|---|---|---|
| sızma | `level` 0–100 | `float Level` | her karede okunur; değişince yumuşak geçer (§4.1) |
| yarıçap | `r` (CSS px) | `float Radius` | çizim alanı kenarı `S = round(R × 4,4)` px (panellerde 4,0–4,2) |
| tür | `kind` | `OrbKind {Normal, Unknown, Quarantine, Core}` | |
| tohum | `seed` (tam sayı) | `int Seed` | aynı tohum → aynı şekil |
| otomatik | `auto="sweep"` | `bool Sweep` | 18 sn'lik 0→100→0 döngüsü (§4.1) |
| eşitleme | `sync` (CSS seçici) | `Func<float> SweepClock` | F-YZ'de işaretçi animasyonunun zamanını kullanır |
| faz | `phase` (sn) | `float SweepPhase` | |
| lif boyu | `fil` (varsayılan 1) | `float FilamentScale` | panel içi küçük kürelerde 0,5–0,6 |

Haritada yarıçap: `R = 30 + 0,16 × v` (çekirdek bölgede +5); bilinmeyen bölge 27. Panellerde sabit değerler (bkz. `spec/tokens_fx.json › orbSizes`).

## 3. Çizim modeli

- Her küre kendi kare alanına (S×S CSS px; arka tampon 2×) çizilir. Merkez `(S/2, S/2)`; ekran y aşağı.
- **Bütün katmanlar eklemeli** (`globalCompositeOperation = 'lighter'`): `hedef = min(1, hedef + kaynak)`, hem renk hem alfa için. Sıra görüntüyü değiştirmez (yalnız doyma noktasında küçük fark).
- Saydam tuval her karede temizlenir; sonra sayfaya normal alfa ile bindirilir (önceden çarpılmış alfa).
- Renkler sRGB değerleriyle **gama uzayında** toplanır.
- Kanvas geçişleri (radial/linear gradient) durak renkleri arasında **çarpılmamış RGBA**'da doğrusal ara değer bulur.

**Unity karşılığı (önerilen yol A):** küre başına bir `RenderTexture` (ARGB32, `sRGB = false`), her karede tek bir `Mesh`'e yazılan dörtgen/üçgenler, `Blend One One` (renk ve alfa). Kompozit: `RawImage` + önceden çarpılmış alfa (`Blend One OneMinusSrcAlpha`). Proje Linear renk uzayındaysa kompozit shader'da `GammaToLinearSpace(rgb)` uygula; böylece toplama tuvaldeki gibi gama uzayında kalır. Ayrıntı §8.

## 4. Kare başına güncelleme

### 4.1 Değer ve ağırlıklar

```
target = kind == unknown ? 0 : (sweep ? Sweep(clock + phase) : level)
ilk karede: t0 = t; lv = target
lv += (target − lv) · min(1, dt · (sweep ? 6 : 3))          // ≈ 0,33 sn (sweep'te 0,17 sn) zaman sabiti
s  = lv / 100
wA = 1 − ss(0,18; 0,50; s)                 // hâl 1 ağırlığı
wC = ss(0,55; 0,86; s)                     // hâl 3
wB = max(0, 1 − wA − wC)                   // hâl 2
blueF = ss(0,24; 0,80; s)                  // altın → mavi
crF   = ss(0,42; 0,92; s) · (unknown ? 0 : 1)   // kristal görünürlüğü
dim   = quar ? 0,72 : 1
intro = min(1, (t − t0) / 1,1);  grow = intro² (3 − 2·intro)   // açılışta 1,1 sn büyüme
Rg = R · (0,55 + 0,45 · grow)              // çizim yarıçapı
```

`ss(a, b, x)` = smoothstep: `k = clamp((x−a)/(b−a), 0, 1); k·k·(3−2k)`.

`Sweep(t)`: `p = t mod 18`; `p<2 → 0`; `p<8 → 100·ss(0,1,(p−2)/6)`; `p<10 → 100`; `p<16 → 100·(1−ss(0,1,(p−10)/6))`; yoksa 0.

### 4.2 Dönüş

```
ang += dt · (unknown ? 0,12 : (0,2 + 0,62·s) · (quar ? 0,45 : 1))     // rad/sn
yaw = ang
pitch = −0,36 + 0,1 · sin(t·0,8 + seed) · (wB + wC)
rot(x,y,z):  x1 = x·cos(yaw) + z·sin(yaw);  z1 = −x·sin(yaw) + z·cos(yaw)
             return (x1, y·cos(p) − z1·sin(p), y·sin(p) + z1·cos(p))
ekran:  X = cx + q.x·Rg ;  Y = cy − q.y·Rg ;  derinlik d = (q.z + 1)/2   (1 = izleyiciye yakın)
```

Renk tonu: `tone = mix(gold, blue, blueF)`, `toneHi = mix(goldHi, ice, blueF)`; bilinmeyende `tone = grey`, `toneHi = (170,196,206)`.

Renkler (sRGB): gold (255,176,64) · goldHi (255,226,150) · amber (255,128,36) · blue (58,140,255) · ice (150,232,255) · white (235,250,255) · grey (110,140,156) · mint (70,255,186).

## 5. Rastgelelik ve kurulum (`build`)

**RNG** (mulberry32 türevi), tohum `seed·9973 + 17`:

```
a = tohum (uint32)
next(): a = (a + 0x6D2B79F5) | 0
        t = imul(a ^ (a >>> 15), 1 | a)
        t = (t + imul(t ^ (t >>> 7), 61 | t)) ^ t
        return ((t ^ (t >>> 14)) >>> 0) / 4294967296
```

C#'ta `uint`/`int` aritmetiğiyle birebir yaz (`imul` = 32 bit taşan çarpım). **Çağrı sırası önemlidir**; aynı sırayla çağır ki altın karelerle eşleşsin. Kurulumda sıra:

1. `ang = rnd()·6,283`
2. **Kabuk parçacıkları** `n = round(56 + R·2,4)`; her i için: `y = 1 − ((i+0,5)/n)·2`, `rr = √(1−y²)`, `th = i·2,399963 + rnd()·0,25` → `{x: cos(th)·rr, y, z: sin(th)·rr, s: 0,55 + rnd(), ph: rnd()·6,283, tau: rnd(), ice: rnd() < 0,35}` (alan sırası soldan sağa).
3. **Bant noktaları**: enlemler `[−0,66, −0,33, 0, 0,33, 0,66]` (indeks bi); her biri için `m = round(16 + R·0,62·cos(lat))`; k = 0…m−1: `lon = k/m·6,283 + bi·0,21` → `{x: cos(lat)cos(lon), y: sin(lat), z: cos(lat)sin(lon), tau: rnd(), ph: rnd()·6,283}`.
4. **Veri blokları** 34 adet: `{lat: (rnd()·2−1)·1,2, lon: rnd()·6,283, w: 0,08 + rnd()·0,26, h: 0,02 + rnd()·0,035, tau: rnd(), ph: rnd()·6,283, dash: rnd() < 0,4}`.
5. **Yörünge halkaları** (sabit): normal (0,35, 0,92, 0,18) yarıçap 1,30 hız 0,55; normal (−0,82, 0,40, 0,41) yarıçap 1,52 hız −0,38. Her biri için `n = norm(normal)`, `a = |n.y| < 0,9 ? (0,1,0) : (1,0,0)`, `u = norm(cross(a, n))`, `w = cross(n, u)`.
6. **Sivrilik** (ikosküre köşesi başına, 42): `k = 0,8 + rnd()·0,42; if (rnd() < 0,22) k += 0,38 + rnd()·0,42`.
7. **Yüz eşikleri** 80 × `rnd()`; **kenar eşikleri** 120 × `rnd()`.
8. **Kırıklar** 10 adet: `{a: rnd()·6,283, rr: 1,3 + rnd()·0,55, sp: (0,4 + rnd()·0,7) · (rnd() < 0,5 ? −1 : 1), tilt: (rnd()·2−1)·0,9, sz: 0,1 + rnd()·0,12, rot: rnd()·6,283, tau: rnd()}`.

**İkosküre:** 12 köşeli ikosahedron (`t = (1+√5)/2`; köşe ve yüz listesi JS'teki sırayla), her yüz bir kez bölünür (orta noktalar yüz sırasıyla eklenir, önbellekli), köşeler normalize. Sonuç 42 köşe, 80 yüz, 120 kenar; kenar listesi yüzler gezilerek ilk görülme sırasıyla. **Sıra JS'tekiyle aynı olmalı**, yoksa sivrilik/eşik eşleşmez.

## 6. Katmanlar (çizim sırası JS'teki gibi)

Aşağıda `α` = renk alfası; `ga` = genel alfa (`globalAlpha`). Kanvasta çizgiye hem renk alfası hem genel alfa uygulanır. "Sprite" = §7'deki ışıma profili, verilen renkle.

| # | Katman | Koşul | Geometri | Renk / alfa |
|---|---|---|---|---|
| 1 | Dış hale | her zaman | radyal geçiş, yarıçap `Rg·2,05`, bütün alan | duraklar: 0 → tone α `(unk ? .05 : .2)·dim·grow`; .45 → α `(unk ? .03 : .09)·dim·grow`; 1 → α 0 |
| 2 | Çekirdek | `!unk` | radyal geçiş yarıçapı `Rg·(0,62+0,25·wC)`, `Rg·0,95` daire içinde | `pulse = .82 + .18·sin(t·(2+3s) + seed)`; 0 → white α `.75·pulse·dim`; .25 → toneHi α `.5·pulse·dim`; 1 → tone α 0 |
| 3 | Işın yıldızı | `flA = (wB + .35wC)·dim·grow` > .03, `!unk` | 6 ışın: `aR = t·(0,9+s) + seed + fr·2π/6 + .25·sin(t·3+fr)`; boy `Rg·(0,8 + 0,5·|sin(t·2,7 + fr·1,3)|)`; kalınlık `Rg·0,07` | doğrusal geçiş merkez→uç: 0 white α `.55flA`; .35 `colR` α `.32flA`; 1 `colR` α 0. `colR` = tek fr: mix(amber, goldHi, .4), çift: ice; `s > .7` ise hep ice. Sonra merkez sprite: boy `2·Rg·(0,35 + 0,12·sin(t·7))`, renk `s > .6 ? ice : goldHi`, ga `.85flA` |
| 4 | Şok halkaları | `core && !unk` | 2 halka, w = 0,1: `ph = ((t + 1,2w) mod 2,4)/2,4`; yarıçap `Rg·(1,05 + 0,95ph)`; çizgi 1,6 px | tone α `(1−ph)·.45·grow` |
| 5 | Yörünge halkaları | `ringA = wA·dim·grow` > .02, `!unk` | her halka 72 parça; nokta `(u·cos a + w·sin a)·rr`; parça çizilir: `(k + floor(spin·6)) mod 3 ≠ 0` (spin = t·hız + halka indeksi); 1,1 px | goldHi α `ringA·(.12 + .5·d)`, `d = (q.z/rr + 1)/2`. Her halkada 2 ışık: açı `spin·1,6 + iπ`, sprite goldHi boy `2·R·(.22 + .16d)`, ga `ringA·(.35 + .65d)` |
| 6 | Tel kafes | `wireA = (.9wA + .35wB)·dim·grow·(unk ? .35 : 1)` > .02 | 6 enlem `[−1,05, −,62, −,21, ,21, ,62, 1,05]` × 64 parça, yarıçap `1 + wB·.07·sin(lon·3 + t·2,4 + li)`; 8 boylam (lon = mi/8·π) × 40 parça, yarıçap `1 + wB·.07·sin(lat·3 + t·2,1 + mi)`; 0,9 px | renk `unk ? grey : mix(gold, blue, .8·blueF)`; enlem α `wireA·(.04 + .34·dl²)`, boylam α `wireA·(.03 + .26·dm²)`; `dl`, `dm` = parça uçlarının ortalama derinliği |
| 6b | İç işlemci halkası | aynı koşul + `!unk` | 48 parça: `a = k/48·2π + t·1,4`; nokta `(cos a·.42, .1·sin(2a + t), sin a·.42)`; `k mod 4 ≠ 0` olanlar; 1,4 px | goldHi α `wA·.55·dim·grow` |
| 7 | Kabuk parçacıkları | her zaman | `jit = .2wB + .05wC`; `rr2 = 1 + jit·sin(t·2,6 + 3ph) + .06wC·sin(7ph)`; `n = (x·rr2 + .5jit·sin(t·1,9+ph), y·rr2 + .5jit·cos(t·2,3+2ph), z·rr2)` | `a = (.18 + .82d)·dim·grow`. Bilinmeyen: grey, `a ×= .35 + .65·|sin(t·9 + 13ph)|`. Diğer: `e = tau − blueF`; `e > .07` → (ice ? goldHi : gold); `e < −.07` → (ice ? ice : blue); arada karışım. Boy `sz = R·.085·s_p·(.65 + .55d)·(1 − .3wC)`; ga `a·(.42wA + wB + .3wC + (unk ? .6 : 0))`. **İz**: `wB > .25` ve `s_p > 1,25` ise `v = (sin(t·1,9+ph), cos(t·2,3+2ph))·.12wB`, çizgi `rot(n − 2,2v)` → nokta, kalınlık `.7sz`, renk α `.6·ga` **ve** ga uygulanır (etkin `.6·ga²`). Sonra sprite boy `3,2sz` |
| 8 | Enlem bant noktaları | `bandA = (wA + .45wB)·dim·grow·(unk ? .5 : 1)` > .02 | konum + `(wB·.18·sin(t·3,1 + 5ph), wB·.14·cos(t·2,7 + 3ph), 0)`; `d < .18` olanlar atlanır | renk `unk ? grey : (tau < blueF ? ice : goldHi)`; boy `3·R·.05·(.6 + .6d)`; ga `bandA·(.1 + .9d)·(unk ? .4 : 1)` |
| 9 | Veri blokları | `blkA = wA·dim·grow` > .02, `!unk` | küre üstünde 1,04 yarıçapta, teğet `(−sin lon, 0, cos lon)` boyunca `w` uzunluğunda çizgi; kalınlık `max(1, h·Rg·(.6 + .6d))`; `dash` olanlar 2,2/1,6 kesik; uç: düz | renk `tau < blueF ? ice : goldHi`; α `blkA·(.08 + .75d)·(.55 + .45·sin(t·2,2 + 4ph))` |
| 10 | Kristal | `crF` > .01 | köşe `V = rot(v·k)`, `k = lerp(1, jag, ss(.5,1,s))·(1,02 + .05·sin(t·1,3 + i))` | **yüz**: görünürlük `ss(kap−.08, kap+.08, 1,08crF)`; arka yüz atlanır: döndürülmüş koordinatlarda (x, y; y yukarı) `(A1−A0)×(A2−A0)`'ın z bileşeni ≤ 0 ise; normal ışık `L = max(0, −.4nx + .55ny + .75nz)`; dolgu `mix(blue, ice, L)` α `(.05 + .26L²)·vis·dim·grow`. **Kenar**: görünürlük `ss(ekap−.1, ekap+.1, 1,1crF)`; `de = ((z0+z1)/2 + 1,3)/2,3`; renk `de > .62 ? white : ice`; α `ev·(.06 + .62de²)·(.75 + .25·sin(t·6 + 1,7i))·dim·grow`; 1 px. **Çatlak**: `jag ≥ 1,12` köşeleri için merkezden `0,92V`'ye, ice α `.22·ss(.62,1,s)·(.4 + .6·|sin(t·2,3 + i)|)·dim·grow`, 1 px. **Çıtırtı**: `ck = floor(t·9) mod 120`; 3 kenar `(ck·7 + 37cz) mod 120`, `z0 + z1 ≥ 0` olanlar; white α `.9wC·dim`, 1,6 px. **Uç ışığı**: `jag ≥ 1,1` ve `z ≥ 0`; sprite ice boy `2·R·.16·ss(.6,1,s)`, ga `.7crF·dim`. **Kırıklar** (`shA = wC·dim·grow` > .02): konum `rot(cos(a+t·sp)·rr, sin(.7(a+t·sp))·tilt, sin(a+t·sp)·rr)`; üçgen: `rt = rot + 2t·sp`, köşeler açı rt (L), rt+2,3 (.6L), rt+3,9 (.5L), `L = sz·Rg`; dolgu ice α `shA·(.15 + .5d)`, kenar white α `shA·(.25 + .6d)` 1,2 px |
| 11 | Karantina kafesi | `quar && !unk` | yarıçap 1,42; döndürme yalnız `qa = −0,35t` (y ekseni) + pitch (yaw yok); 3 enlem [−.5, 0, .5] × 48 parça, çift indeksliler; 6 boylam (`mrd/6·π + qa`) × 32 parça, tek indeksliler ve z > −.2 | mint α `.42·grow`, 1 px |
| 12 | Şimşek lifleri + yaylar | `!unk` | yenileme: `t > nextFil` olunca. Lif sayısı `round(5wB + 2wC + (s > .3 ? wB : 0))`; her lif: `dir = norm(rnd3)`, `len = ((quar ? .95 : 1,15) + rnd()·(quar ? .3 : .7 + .6wB))·fil`, `side = norm(cross(dir, (.3,1,.2)))`, 10 nokta: `f = j/9`, `rad = .25 + f·len`, `off = (rnd()·2−1)·.16·(1−|f−.5|)`. Yay sayısı `round(3wB + wC)`; her yay: `ax = norm(rnd3)`, `a0 = rnd()·2π`, `span = 1,2 + rnd()·1,6`, `u = norm(cross(ax, (.1,1,.3)))`, `w = cross(ax, u)`, 17 nokta: `rr = 1,08 + rnd()·.12 + .12·sin(j)`. Sonra `nextFil = t + .07 + rnd()·(.22 − .1wB)` | iki geçiş: geniş (kalınlık ×3,2) + ince. Lif: blue α `.16·dim·grow` / white α `.85·dim·grow`, ince 1,1 px. Yay: blue α .2 / ice α .7 (×dim·grow), ince 1,3 px. Birleşim: yuvarlak |
| 13 | Kıvılcımlar | `!unk` | doğum hızı `(1,2wA + 16wB + 20wC)·(quar ? .4 : 1)·(R/34)` / sn (birikimli); her biri: `dv = norm(rnd3)`, `spd = .6 + rnd()·(.6 + wB)`, `life 0`, `max = .5 + rnd()·.7`, renk `rnd() < blueF ? (rnd() < .5 ? ice : blue) : (rnd() < .5 ? goldHi : amber)`. Güncelleme sondan başa: ömrü dolan silinir, `p += v·dt` | sprite boy `2·R·.07·(.5 + lf)`, ga `lf·.9·dim`, `lf = 1 − life/max` |
| 14 | Parazit çizgileri | `unk` | 3 yatay çubuk: `y = cy + sin(t(1,3 + g) + 2g)·.9R`, yarı genişlik `R·(.6 + .6|sin(3t + g)|)`, yükseklik 1,4 | (150,180,190) α `.08 + .08|sin(11t + g)|` |

## 7. Işıma sprite'ı

JS 64 px'lik bir radyal geçiş tuvali üretir (renk başına önbellek, renk 8'lik adımlara yuvarlanır). Duraklar: `0` beyaz α1; `.12` `mix(c, beyaz, .45)` α1; `.30` c α.55; `.62` c α.12; `1` c α0.

Unity'de **tek doku** yeter: `art/orb/orb_glow_profile_128.png` (R = beyazlık, A = alfa; doğrusal veri, sRGB kapalı). Shader:

```hlsl
float4 p = tex2D(_Glow, uv);                 // p.r = beyazlık, p.a = alfa
float3 rgb = lerp(vcol.rgb, 1.0, p.r);       // renk → beyaza
return float4(rgb * p.a * vcol.a, p.a * vcol.a);   // önceden çarpılmış, Blend One One
```

Çizgiler: dörtgen genişliği = kalınlık + 2 px; v ekseninde `art/orb/orb_line_aa_1x8.png` ile kenar yumuşatma (ya da `fwidth` tabanlı AA). Kalınlığı 1 px'ten az olan çizgilerde dörtgeni 1 px tut, alfayı kalınlıkla çarp.

Radyal geçişler (katman 1–2) ve ışın geçişi (katman 3): shader'da yordamsal hesapla (dörtgenin uv'si −1…1; `r = length(uv)`; duraklar arası doğrusal). Durak renk/alfa değerlerini köşe verisiyle geç (uGUI'de `uv1–uv3`, mesh'te ek kanallar).

## 8. Unity uygulaması

**Yol A (önerilen) — küre başına RenderTexture:**

- `OrbSimulation` (saf C#, MonoBehaviour değil): kurulum (§5), kare güncellemesi (§4), katman üretimi (§6) → `OrbDrawList` (dörtgen/üçgen/çizgi kayıtları). JS'in birebir karşılığı.
- `OrbRenderer` (MonoBehaviour): `RenderTexture(S·ölçek, S·ölçek, 0, ARGB32, Linear)`; `Mesh` (dinamik, `MarkDynamic`), `CommandBuffer` → `SetRenderTarget(rt) · ClearRenderTarget(0) · DrawMesh(mesh, ortho)`. Görünür değilse ya da ekran dışındaysa çizme.
- `OrbView` (uGUI): `RawImage` + `UI/OrbComposite` (önceden çarpılmış alfa, Linear projede gama→doğrusal). Haritada konum `MapMarkersLayer` ile (ölçeklenmez; Sade §4.4).
- Kare hızı: 40 fps sınırı (`OrbClock`), çizim maliyeti 14 ms'yi geçerse 16 fps'e kadar düşür; `prefers-reduced-motion` karşılığı oyun ayarı “Hareketi azalt” → 8 fps.
- Bütçe (1080p, 12 küre): CPU ≤ 1,5 ms/kare, GC 0 B (listeleri önceden ayır, `NativeArray` ya da yeniden kullanılan `List`), bellek ≤ 12 MB RT.

**Yol B — uGUI `MaskableGraphic`:** `OnPopulateMesh` her karede; `Canvas.additionalShaderChannels` (TexCoord1–3) açık; ayrı bir `Canvas` (yeniden kurulum yalnız küre katmanını etkilesin). Doğrudan haritanın üstüne `Blend One One` ile çizilir; tuvalin kendi alfası olmadığı için harita biraz daha aydınlanır (kabul edilebilir fark, KARARLAR.md'ye yaz).

**Yasak:** küreyi önceden çizilmiş bir sprite dizisiyle (flipbook) taklit etmek; değer sürekli değiştiği için hâl geçişi kaybolur.

## 9. Doğrulama

1. `OrbSimulation`'ı belirlenimci modda çalıştır: `dt = 1/40`, t = 1,0 sn (40 adım) ve 2,5 sn (100 adım), R = 48, tohum ve durumlar `golden.json`'daki gibi.
2. Çıktıyı `#02050A` zemin üstünde 2× ölçekle (422×422) kaydet.
3. `python3 tools/compare.py UNITY.png reference/orb/golden/orb_normal_L060_t2.5.png` → genel fark ≤ 6, kenar örtüşmesi ≥ %70. Kıvılcım ve liflerin rastgele dizisi birebir porta bağlıdır; kayan nokta farkları küçük kaymalar yapar, şekil ve renk aynı olmalı.
4. Hâl geçişi: `tools/orb_lab.html` ile yan yana izle (değer 0 → 100 sürüklenirken).
