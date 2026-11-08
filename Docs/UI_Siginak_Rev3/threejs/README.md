# three.js referans sahnesi · Revizyon 3 + Ek 11-05 → Unity

Bu klasör Revizyon 3 ekranlarını (R1–R19; R7 ekle kalktı) ve model sayfalarını (R13) üreten three.js sahnesidir. **Konum, boyut, açı,
pivot ve malzeme için kaynak budur; tahmin etme, buradan ya da modellerin manifest dosyalarından oku.** Revizyon 2
sahnesi (`rev2/`) hâlâ geçerli ve bu klasörün içinde duruyor: oda kabuğu, harita dokusu, kâğıt dokuları, baskın tahtasının
ozalit dokusu ve durum fikstürleri oradan gelir. Çelişkide `r3/` kazanır.

Doku kodu (`prints.js`, `rev2/textures.js`, `rev2/tex2.js`, `rev2/map2.js`) yalnız görünüş örneğidir: Unity'de bütün
yazılar TMP ile kurulur (modellerde `TXT_*` yuvaları), işaretler sprite ile. Kodu çalıştırman gerekmez; okumak yeter.

## Koordinatlar (düzeltilmiş kural)

Bütün `r3` yardımcıları ve model kaynakları **Unity koordinatı** kullanır: x sağ, y yukarı, z ileri (arka duvara doğru),
metre; nesnelerin ön yüzü **−z**'ye bakar. Açılar Unity derecesidir.

| three.js | Unity |
|---|---|
| `U(x, y, z)` | `new Vector3(x, y, z)` (aynı sayılar) |
| `inst(parent, Model, [x, y, z], yaw)` | prefab örneği, `localPosition = (x, y, z)`, `localEulerAngles = (0, yaw, 0)` |
| `setUnityEuler(obj, [ax, ay, az])` | `localEulerAngles = (ax, ay, az)` (three tarafında `rotation.set(−ax, −ay, +az)`, sıra `YXZ`) |
| model parçası `m.pose('Lever', 35)` | `Lever` alt nesnesinin yerel ekseninde +35° (ya da kayma için metre) |

**Revizyon 2 README'sindeki düzeltme:** "bütün açıların işareti ters" kuralı **z ekseni için yanlıştı.** z aynalandığı için
x ve y açılarının işareti döner, **z açısı aynı kalır**: three `rotation (rx, ry, rz)` → Unity `localEulerAngles
(−rx°, −ry°, +rz°)`. Ham `position.set(x, y, z)` hâlâ Unity `(x, y, −z)` demektir. Rev2 kodunda `rz` kullanılan yerler
(raf kartlarının eğimi, teleks kâğıdı, afiş) Unity'ye bu kuralla taşınmalı; uygulamada yanlış yöne eğik duranlar varsa
neden budur.

## Klasör

| Dosya | İçerik |
|---|---|
| `r3/mlib.js` | Geometri ve `Model` sınıfı: pahlı kutu (44 üçgen), torna (`gLathe`), pahlı ekstrüzyon, kendi kırışık normal hesabı (40°), metrik düzlemsel UV; `part()` (pivot, eksen, aralık, dinlenme, tür `rotate · slide · swap`), `text()` (TMP yuvası), `anchor()`, `light()`, `meta.place` (dünya yerleşimi). |
| `r3/mats.js`, `r3/mats_base.js` | Malzeme anahtarları = URP Lit değerleri (renk, pürüzlülük = 1 − smoothness, metal, emission, saydamlık, doku notu). `mats_base.js` 1.1/1.2 token'larından üretildi; ailelerin `defMat()` ile eklediği anahtarlar token 1.3'te. |
| `r3/models/*.js` | Aileler: `menu`, `cabinet`, `desk`, `walls`, `crt`, `door`, `board` (ek), `admission` (ek), `raidkit`, `raidcivil`, `raidpieces`; `shelf` yalnız legacy (sahnede gizli, `&legacy=1`). Her model bir fonksiyon; açıklamalar Türkçe. |
| `r3/registry.js` | Bütün modellerin listesi (`MODELS`). |
| `r3/scene3.js` | Sahne yardımcıları: `inst`, `placeAll`, `worldOf`, `anchorWorld`, `lightWorld`, `setMat`, `slotText` (TMP yerine kanvas yazısı), `printPlane`, `drumTex` + `drumAngle` (sayaç tekeri: rakam d için açı = 162 − 36·d, yalnız bu dokuya göre). |
| `r3/prints.js` | Görünüş dokuları: fayans, beton, ıslak zemin, tehlike şeridi, saat kadranı, vardiya kartı ızgarası, ibre kadranı, ölçek, dişli rozet. |
| `r3/menu3.js` + `r3/render3.html` | Menü sahnesi (sahanlık) ve dolap. `?pose=M0|M1|M2|M3|MS &s=nosave|save|confirm|carry|press|wheel|open|switch|cab &lang=tr|en &door=<derece> &mute=1 &vals=80,100,100,100` |
| `r3/room3.js` + `r3/room3.html` | Komuta odası: Rev2 kabuğu + bütün oda modelleri `meta.place` konumlarında + giydirme modülleri (`dress_room_*.js`). `?pose=K1|MAP|K4|K4C|K5a|K6|K6W|K8|K8A|K8G|K8R|K9 &s=v001|v014|v031 &door=closed|open &knock=1 &cab=1 &mute=1 &lang=tr|en` (ölümcül kapı ve ziyaretçi yalnız `&legacy=1`) |
| `r3/dress_room_board.js`, `r3/boardtex.js`, `r3/portraits.js`, `r3/community.json` | Ek · pano (R15, R18): kart düzeni, kart yüzü çizimi (8 px/mm), mürekkep portreler, örnek kadro verisi (8 kişi + 2 aday, ilk vardiya ve 31. vardiya). `&b=first|late &bs=idle|hover:<id>|drag:<id>:<cep>|swap:<id>:<cep>:<t>|bosta_open|bosta_take:<id>|item:<eşya>:<kişi>|locked:raid|locked:slip|book|book:<n>|bookdrag:<tarif>|order|product|file:<id> &sel=<id> &wall=1` (Karşılaştırma 4: duvar panosu) |
| `r3/dress_room_switch.js` | Ek · vardiya kapanışı (R19): `&sw=ready|cover|down|locked &slip=none|emerge|out|hand|over|drop &sl=v001|v031 &tx=devir|onkontrol &new=1` (ERZAK /60) |
| `r3/dress_room_admission.js` | Ek · kapıda kabul (R17): `&adm=idle|present|inspect|pending|stamped|aid|v3|trace|cold &cand=aylin|rauf` |
| `r3/cards.html` | Kart sayfaları (R16): `?mode=anatomy|states|items|mats|recipes|stack|backs &lang=tr|en` |
| `r10/papers_new/` | R10 kâğıt dokuları (devir fişi, kabul talimatı v1–v3, bulgular, damga izleri, tarif açılımları, kişi dosyası). Sahne bunları `/r10/papers_new/` yolundan okur. |
| `r3/raid3.html` | Baskın tahtası: Rev2 dizilişi, r3 modelleri. `?kit=weu|eeu|me|af|na|ru|in|cn|jk|sea|sa|oc|default &s=plan|machine|event|last` |
| `r3/sheet.html`, `r3/sheets.mjs` | Model sayfası (önden, sağdan, üstten, perspektif; ölçü, pivot, TMP yuvası). |
| `r3/export_obj.mjs` | OBJ + MTL + manifest dışa aktarımı (aşağıda). |
| `r3/tokens13.mjs` | Token 1.3 üreticisi (`tokens13_extra.json` + ek için `tokens13_sup.json`). |
| `rev2/` | Revizyon 2 sahnesi (değişmeden; yalnız `r3` tarafından içe aktarılır). |

## Çalıştırma (isteğe bağlı)

```
npm i three@0.160.0 @fontsource/courier-prime @fontsource/libre-franklin @fontsource/kalam @fontsource/big-shoulders-stencil-display
python3 -m http.server 8765        # bu klasörün kökünde (node_modules, r3, rev2, r10 yanında)
http://127.0.0.1:8765/r3/render3.html?pose=M0&s=save
http://127.0.0.1:8765/r3/room3.html?pose=K1&s=v001&new=1&sw=ready
http://127.0.0.1:8765/r3/room3.html?pose=K4&s=v001&bs=bosta_open
http://127.0.0.1:8765/r3/room3.html?pose=K6&s=v001&sw=down&slip=out
http://127.0.0.1:8765/r3/room3.html?pose=K8A&adm=inspect&cand=rauf
http://127.0.0.1:8765/r3/raid3.html?kit=af&s=machine
node r3/export_obj.mjs out/lib            # bütün modeller
node r3/sheets.mjs out/sheets PunchClock --jpg
```

## Pozlar ve ışık

Kameralar ve poz başına ışık tarifi `tokens_siginak.json` 1.3 → `cameras.*`, `lightsRecipe` içinde (ek: K4 panoya bağlı,
K6, K8A, K8G, K8R). Kural: bir pozda en çok 4 gölgeli ışık, gölgeyi spot taşır; dolap içi, kapı arkası, kadran ışıkları ve
EVRAK hedef ışığı gölgesiz. Pano lambası K4’ün tek gölgeli ışığıdır. Render ayarı: ACES, pozlama
1,0 (menü 1,15), PCF yumuşak gölge, metaller için zayıf çevre yansıması (0,22). Unity'de bloom ve post-processing yok;
parlama yalnız emission.

## Taşınmayacaklar

Revizyon 2'deki `opts.doorBox`, `topBand: 'surface'`, `opts.alt` hâlâ taşınmaz. `render3.html` içindeki kapı ardı "iç oda"
düzlemi (sıcak degrade) yalnız referans görüntü içindir: Unity'de kapının ardında DeskGame'in ışığı yerine `DoorProp`'un
sıcak ışığı ve basit bir iç yüzey yeter.
