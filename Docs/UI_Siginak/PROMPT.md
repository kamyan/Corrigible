# Son Şalter · Sığınak Komuta Odası — Unity uygulama talimatı

Bu dosya Claude Code içindir. DeskGame sahnesindeki sığınak komuta odasını, bu klasördeki yeni tasarıma göre
Unity'de kuracaksın. Önce bu dosyanın tamamını oku, sonra **Aşama 0**'dan başla.

---

## 0. Görev

Oyun: “Son Şalter / Corrigible”, tek oyunculu strateji. Oyuncu Batı Avrupa'nın altındaki KYGA / SIĞINAK 07'nin
komutanı; yapay zekâ dünyayı ele geçirmiş, sığınak her dijital cihazdan kaçınıyor (doktrin: “AĞA BAĞLANMA · ANALOG KAL”).

Bugünkü DeskGame sahnesinde üç görsel dil yan yana duruyor (kâğıt harita, rafın önünde yüzen siyah kutular, üstte ve
altta düz yeşil HUD). Yeni tasarım hepsini **tek bir analog sığınak diline** çeviriyor: kâğıt, mürekkep, pirinç,
bakalit, beton ve tüp. **Bilgi ve etkileşim aynı kalır; görünüş, malzeme, ışık ve yerleşim yenilenir.**

İş bitmiş sayılır, eğer:

1. K1–K7 pozları oyunda `reference/screens/` görüntülerine görünüş, yerleşim ve okunurluk bakımından denk düşüyorsa,
2. bütün etkileşimler ve durumlar (normal, üzerinde, basılı, devre dışı, yetersiz kaynak) çalışıyorsa,
3. her metin Türkçe ve İngilizce gösterilebiliyorsa,
4. eski HUD, alt düğme çubuğu ve rafın önündeki siyah kutular kaldırılmışsa,
5. oyun mantığında (simülasyon, denge, kayıt) hiçbir şey değişmemişse.

---

## 1. Paket içeriği ve doğruluk sırası

```
UI_Siginak/
  PROMPT.md                      ← bu dosya
  spec/tokens_siginak.json       ← renk, yazı, gölge, boşluk, hareket, kamera, ışık, malzeme (YENİ olanlar "_new" listesinde)
  reference/
    BRIEF.md                     ← tasarım brifi (kısıtların kaynağı)
    screens/*.jpg                ← tasarım panoları: K1…K7 ekranları, K*-not uygulama notları, Ekip (figür), K8 stil sayfası, Karar-1…3
    renders/*.jpg                ← aynı pozların yalnız 3D görüntüsü (üstünde UI yok) — ışık ve malzeme karşılaştırması için; TFS = figür durumları, TFC = taşıma anı
    notes/K1-not.md … K7-not.md  ← her ekranın nesne listesi, etkileşim/durumlar, hareket/ses (süre ve sıra) — METİN
    notes/Ekip-figuru.md         ← haritadaki 3D ekip figürü: yer kuralı, sürükle-bırak, teleks gerekçeleri, ölçü, süre
    notes/Kararlar.md            ← üç karşılaştırma (kullanılan seçenek A)
    boards_html/*.dc.html        ← panoların HTML kaynağı: UI öğelerinin tam px, renk ve font değerleri burada
    board_assets/                ← panolarda kullanılan kesit görüntüleri
    scene_threejs/               ← görüntüleri üreten three.js sahnesi: ODA YERLEŞİMİNİN VE ÖLÇÜLERİN KAYNAĞI (README'yi oku)
  sprites/                       ← 9 dilimli UI sprite'ları, harita taramaları, damga/işaret, CRT katmanları, decal'ler (README'de dilim kenarları)
```

Doğruluk sırası (çelişkide üstteki kazanır):

1. **bu dosya**
2. `spec/tokens_siginak.json` (ve projedeki mevcut `tokens_sade.json`; yeni dosya onu genişletir, değerlerini değiştirmez)
3. `reference/notes/*.md` (davranış, süre, sıra)
4. `reference/scene_threejs/room.js` (3D konum/ölçü/açı)
5. `reference/boards_html/*.dc.html` (UI px değerleri)
6. `reference/screens/*.jpg`, `reference/renders/*.jpg` (görünüş)

Çözemediğin çelişkiyi `IMPLEMENTATION_MAP.md` → “Açık sorular” bölümüne yaz ve bana sor. Görüntülerdeki sayılar
**örnektir** (V.014 durumu); tasarım sayıları göstermeli, değiştirmemeli.

---

## 2. Değişmez kurallar

**Motor ve teknik**
- Unity 6, URP. 3D sahne + **uGUI + TextMeshPro** (World Space ve Overlay Canvas). **UI Toolkit yok.**
- **Özel shader yok** (Shader Graph dahil). Yalnız URP Lit / Simple Lit / Unlit, `UI/Default` ve TMP'nin kendi shader'ları.
- Gerçek zamanlı bulanıklık ve post-processing'e dayanan efekt yok. Projede Volume varsa görünüm onsuz da doğru olmalı;
  bloom yok, parlama emissive ile. CRT etkileri (statik, tarama çizgisi, köşe kararması) sprite katmanlarıdır.
- 2D öğeler 9 dilimli sprite (`sprites/`). 3D nesneler basit geometri (Cube, Cylinder, Quad, düşük poligon) + düz ya da az dokulu malzeme.
- Sahne ve prefab YAML'ını elle düzenleme. Kurulumu **editor script'leriyle** yap (bkz. §4). Unity'ye MCP/CLI erişimin
  varsa sahneyi açmak, builder'ı çalıştırmak, Play Mode'a girmek ve ekran görüntüsü almak için onu kullan; yoksa menü
  komutları yaz ve çalıştırmamı iste, çıktılarını oku.

**Oyun mantığı**
- `Balance`, `Simulation` (`Step` dahil), `CampaignLiberation`, `ShelterLogic`, `Simulation.Shelter`, `GameData`,
  kayıt biçimi (`liberation_savegame.json`) **değişmez.** Sunum katmanı yalnızca okur ve mevcut komutları çağırır.
- Yeni bir sayı gerekiyorsa (ör. vardiya raporundaki farklar) simülasyona alan ekleme; sunum tarafında
  `Step` öncesi/sonrası anlık görüntü (snapshot) farkıyla hesapla.
- Var olan public API'leri silme ya da yeniden adlandırma; gerekirse `[Obsolete]` ile işaretle.

**Kamera ve kontroller**
- Pozlar korunur: başlangıç (Home), 1 KOMUTA, 2 SIĞINAK, 3 GÖREVLER. Kontroller korunur: sağ tuş bakış, tekerlek yaklaşma,
  F harita odağı, Home başlangıç. Yeni pozlar: harita üst pozu (K2/K3/K6b/K7 ortak), K4 raf pozu, K5a CRT bakışı
  (istasyon değil), K6a vardiya anı.
- Kısayollar ekranda görünür kalır (tuş fişleri). Enter = açık kâğıttaki asıl eylem; kâğıt yoksa şalter (iki adımlı).
  Esc = kâğıdı kapat / AYARLAR. Boşluk = kilitli diziyi atla.
- Sol tuşla sürükleme yalnızca haritadaki ekip figürü içindir (6 px eşiği; altında tık sayılır); sağ tuş bakış olarak kalır,
  figür taşınırken kamera bakışı ve tekerlek yaklaşması devre dışıdır.

**Dil ve okunurluk**
- Her metin TR + EN. Projedeki yerelleştirme sistemini kullan (yoksa Aşama 0'da bana sor). İngilizce %30'a kadar uzun
  olabilir: punto küçülmez, satır sarar, kâğıt gerekirse genişler (görev fişi 436 → 452 px).
- Fontlar: **Courier Prime** 400/700 (daktilo: başlık, sayı, etiket, plaka, damga, teleks, CRT), **Libre Franklin**
  400/600/700 (okuma: açıklama, düğme), **Kalam** 700 (yalnız el notu). Öneri: **Big Shoulders Stencil Display** 800 yalnız
  duvar yazısı decal'ı için (projede yoksa Courier Prime 700 + şablon köprüsü maskesi).
- TMP font asset'leri Türkçe glifleri içermeli: `Ğ Ü Ş İ Ö Ç ğ ü ş ı ö ç` + `✓ ▸ · → − ± × … “ ” ‘ ’ — %`. Eksik glif = hata.
- 1920×1080'de **gövde ≥ 18 px, ikincil ≥ 14 px.** Dünya metinleri (harita, oda kartı, plaka) en dar pozda ölçülür.
- **Renk körlüğü:** hiçbir durum yalnız renkle verilmez; her durumun kelimesi, damgası, işareti ya da deseni vardır.
- Sektör türü TR'de **YAĞMA**, EN'de **LOOT**. Türkçe arayüzde “LOOT” kalmayacak (metin tablosunu düzelt).
  “SUNUCU YIKIMI” / “SERVER DEMOLITION”.
- Neon, hologram, HUD çerçevesi, bilim kurgu görünümü yok.

**Güvenli geçiş**
- Yeni sunumu bir anahtarın arkasına al (`SiginakPresentation`, ör. bir ayar ScriptableObject'i ya da debug menüsü):
  eski ve yeni görünüm arasında geçilebilsin. Eski HUD'u **Aşama 10**'da, ben onayladıktan sonra kaldır.

---

## 3. Tasarımın özü (bir sayfada)

| Eski | Yeni |
|---|---|
| Üstte yeşil kaynak HUD'u | Masanın arka kenarında **mekanik sayaç rafı**: VARDİYA · FON (kırmızı onda bir çarkı) · ERZAK x/40 · MALZEME · ÖZGÜR BÖLGE x/12. Değişince çark döner, altına ± kâğıt fişi asılır |
| Alttaki düğme çubuğu | Masanın ön kenarında **pirinç anahtar rayı**: 1 KOMUTA · 2 SIĞINAK · 3 GÖREVLER · H KILAVUZ · Home MASA. Etkin istasyon = anahtar içeride + amber lamba |
| Sıradan “Vardiyayı bitir” düğmesi | Masanın sağ önünde **kapaklı şalter**: 1. tık kapağı kaldırır (teleks ön kontrol satırı basar), 4 s içinde 2. tık kolu indirir; oda kararır, sayaçlar döner, rapor gelir |
| Ekranda yüzen “Emir uygulandı.” | **Teleks** satırı (sol masada). Teleks kadraj dışındaysa aynı satır ekranın altından kâğıt şerit olarak kayar |
| Rafın önünde siyah bilgi kutuları | Sol rafta **kesit maketi** (2 × 2 oda, ahşap piyonlar, pirinç yuvalar), oda başına pirinç çerçeveli **dizin kartı**, yanda **sığınak özeti panosu** (seçili oda kartı + eylemler) |
| 12 bölge aynı koyu kırmızı | Kâğıt harita durumları: **işgal** çapraz kırmızı tarama · **cephe** tek yön tarama + yağlı kalem halkası + raptiye · **aday** kesik grafit halka · **özgür** tarama silinmiş + mavi damga · **seçili** sarı dolgu |
| Haritadaki düz ekip simgesi | Harita üstünde fiziksel **ekip figürü** (pirinç oval taban, üç ahşap piyon, bakalit “A” plakası). Yeri oyun durumundan gelir (cephede / S-07'de yaralı); elle **sürüklenip başka bölgeye taşınabilir**: adaya bırakmak cephe seçmektir, geçersiz bırakmada geri kayar ve teleks gerekçeyi basar |
| Ekranın kenarında sabit HUD | Yalnız sayaç rafı kadraj dışındayken (K4, K5) üstte ince **kenar satırı** (40 px bakalit şerit); sağ üstte tek sabit öğe **AYARLAR (Esc)** plakası, kilitli dizilerde **ATLA (Boşluk)** |

Kurallar: **kâğıt bilgi taşır · pirinç plakalı nesne tıklanır (plakasız nesne dekordur) · bakalit çerçeveler ·
oda anlatır · tek ekran tüp (CRT yalnız sahadan yayın).** Odanın yerleşimi U biçiminde: sol raf ve sağ CRT masası
koltuğa doğru 35° döner, ikisi de başlangıç pozunda kadraja girer.

---

## 4. Önerilen mimari

Klasörü Aşama 0'da bulduğun proje düzenine uydur; varsayılan:

```
Assets/Corrigible/Presentation/Siginak/
  Scripts/   Editor/   Prefabs/   Materials/   Sprites/   Fonts/   Data/
Docs/UI_Siginak/               ← bu paket + IMPLEMENTATION_MAP.md + screens_unity/
```

**Veri ve kurulum**
- `SiginakTokens` (ScriptableObject) + `Editor/SiginakTokensImporter`: `spec/tokens_siginak.json`'ı okur (gerekirse
  `tokens_sade.json`'dan “reuse” değerlerini). Renk, TMP yazı ön ayarları (aile/ağırlık/boyut/aralık), gölge, süre,
  kamera, ışık, malzeme alanları tipli olur. **Script'lerde sabit hex yok.** Projede zaten bir token yükleyici varsa onu genişlet.
- `Editor/SiginakRoomBuilder` — menü `Tools/Sığınak/Build Room`: DeskGame'de `SiginakRoom` hiyerarşisini koddan kurar ya
  da günceller (tekrar çalıştırınca kopya üretmez). Konum/ölçü/açı `reference/scene_threejs/room.js`'den, dönüşüm kuralı
  `scene_threejs/README.md`'de. Malzemeler `tokens.materials`'tan asset olarak (`Materials/`).
- `Editor/SiginakSpriteImporter`: `sprites/README.md`'deki dilim kenarlarını ve Image Type önerilerini import ayarlarına uygular.

**Bileşenler** (adlar öneri)

| Bileşen | Görev |
|---|---|
| `SiginakCameraRig` | Pozlar (`tokens.cameras`), geçiş 0,45 s ease-in-out; mevcut kamera kontrolcüsüne bağlan, yerine yazma |
| `CounterBank` + `CounterWheel` | 5 modül; çark = `sprites/tx_counter_digits.png`, `material.mainTextureOffset` ile döner (shader yok; formül `sprites/README.md`); FON'da kırmızı onda bir çarkı; 0,35 s, çark başına 0,12 s gecikme; `DeltaSlip` (± fişi); plaka yazıları TMP |
| `StationKeyRail` + `StationKey` | 5 anahtar; durumlar: normal · üzerinde (4 mm kalkar, plaka +%15 emissive) · etkin (6 mm içeride, amber lamba) · devre dışı (α .6); kısayollar |
| `ShiftSwitch` | Durum makinesi: `Idle → Hover → CoverOpen (4 s) → Thrown → Sequence`, `Disabled` (sarı-siyah mühür bandı “KİLİTLİ”) |
| `TelexFeed` + `Teleprinter` + `TelexStrip` | Bütün geri bildirimin tek kapısı: `Post(code, status, message, kind)`. Masadaki teleks kâğıdı (World Space TMP, satır başına 12 mm ilerler); teleks kadraj dışındaysa Overlay şerit (820 × 52, 0,15 s giriş · 3 s · 0,12 s çıkış) |
| `EdgeLine` | Sayaç rafı ekranda değilken üst 40 px şerit; ayar: Otomatik / Her zaman / Kapalı |
| `HoverTag` + `HoverHighlight` | 400 ms gecikmeli kâğıt etiket (ad + kısayol + tek satır); büyük hedeflerde sarı çerçeve 5 px #F5D646 + 1,5 px mürekkep (mesh/Image çerçeve, shader outline değil) |
| `SettingsPlaque` | Sağ üst tek sabit öğe: AYARLAR (Esc); kilitli dizilerde ATLA (Boşluk) |
| `MapRegionView` + `MapLabel` + `MapLegend` | Mevcut Sade harita çizicisini genişlet: durumlar, katman sırası, etiketler (CP 700 28 harita px, TMP Underlay hale #F1E9D6, 330 px'i aşan ad “·”den iki satır), lejant (EN'de sütunlar metne göre) |
| `TeamFigure` + `TeamFigureDrag` | 3D ekip figürü (prefab: taban, 3 piyon, harf plakası, seçim halkası Quad'ı, taşıma gölgesi Quad'ı). Konum = oyun durumundan türetilmiş yer (cephe `teamAnchor` · S-07 · özgür bölge); `TeamFigureDrag`: harita düzlemine ışın (`Plane.Raycast`), bölgeyi Sade haritanın mevcut bölge seçicisiyle bul, bırakınca **mevcut komutu** çağır (aday seç / görev fişini aç) ya da geri kaydır + `TelexFeed.Post(...)`. Ayrıntı ve süreler: `reference/notes/Ekip-figuru.md` |
| `OperationSlip` + `UiStringLine` | Görev fişi (haritanın 2 mm üstünde World Space) + kırmızı ip (UI `Graphic` alt sınıfı, 3 noktalı eğri, 2,6 px #A3241A + gölge); yeni cephe onay fişi (K3) |
| `ShelterDiorama` + `RoomView` + `RoomCard` + `ShelterClipboard` | Kesit maketi ve oda durumları; piyon yuvaları ve BOŞTA tepsisi; oda kartları (World Space 380 × 110); özet panosu (600 × 800) ve seçili oda kartındaki eylemler (`ShelterLogic` komutlarını çağırır) |
| `CrtScreen` + `CrtTransition` | CRT ekranı (World Space; Bekleniyor · Kuruluyor · Yayın açık · Kesildi · Devre dışı); baskın geçişi (kamera yaklaşması + Overlay katmanlar + senkron kutuları + baskın kamerasıyla aynı 8×8 ızgara), dönüş |
| `ShiftSequence` + `ShiftReportBuilder` + `ShiftReportView` + `EvrakTray` | Vardiya dizisi (ışık, ses, sayaç, teleks, kamera), rapor hesaplama (snapshot farkı), rapor okuma görünümü (Overlay 600 px), EVRAK tepsisi (son 5 rapor + brifing) |
| `BriefingView` | Açılış: emir belgesi + KAYITTAN DEVAM dosya sekmesi; açılış dizisi |
| `SiginakAudio` | Adlandırılmış ses ipuçları (bkz. notlar). Mevcut ses sistemini/mixer'ı kullan; ses dosyası yoksa yer tutucu çağrılar bırak |

**UI kâğıtları nasıl kurulur (uGUI)**
Kâğıt = `ui_fill_r2` (renk token) + üstünde `tx_paper_grain` (Tiled, ekranda 128 px döşeme: Pixels Per Unit Multiplier 2) + kenar `ui_frame_1px_r2` (renk token)
+ gölge (ayrı siyah `ui_fill_r2`, `shadows.*` kadar kaymış, bulanıklık yok). Damga = `ui_frame_3px_r0` ya da
`ui_frame_dashed_2px` + TMP, −3…−8° dönük. Dünya metinleri: World Space Canvas'ın piksel ölçeği haritada 1600 px/m.

---

## 5. Aşamalar ve kabul ölçütleri

Her aşamanın sonunda: proje derlenir, Console'da hata yok, testler geçer, eski/yeni anahtarı çalışır, ilgili pozların
ekran görüntüsü alınır (§7) ve bana kısa rapor yazılır (ne değişti, hangi dosyalar, görüntüler, açık sorular).
**Aşama 0'dan sonra dur ve onay bekle;** sonrasında takılmadıkça sırayla devam et. Commit atmadan önce sor.

### Aşama 0 — Keşif (kod yazma)
Oku ve eşle: DeskGame sahnesi; kamera rig'i ve poz sistemi; istasyon geçişi; üst HUD ve alt çubuk script'leri;
raf önündeki sığınak kutuları; Sade harita çizicisi ve görev fişi; CRT ve baskın geçişi; vardiya bitirme; “Emir uygulandı”
geri bildirimi; yerelleştirme; TMP font asset'leri; mevcut token yükleyici (`Docs/UI_C/spec/tokens.json`,
`tokens_sade.json` C#'ta nasıl okunuyor); ses sistemi; kayıt/yükleme.
**Çıktı:** `Docs/UI_Siginak/IMPLEMENTATION_MAP.md` — tablo: *Tasarım öğesi · Mevcut karşılık (script/prefab/sahne yolu)
· Yapılacak (yeniden kullan / değiştir / yeni) · Veri kaynağı (Simulation alanı/metodu)*; ardından açık sorular ve riskler.

### Aşama 1 — Token, font, sprite
- `spec/tokens_siginak.json` projedeki spec klasörüne; `SiginakTokens` + importer.
- TMP font asset'leri (§2'deki glif seti); TR/EN deneme metni ve Kalam/Big Shoulders sınaması.
- `sprites/` içe aktarımı (dilim kenarları, Tiled/Sliced).
- **Kabul:** test sahnesinde “ĞÜŞİÖÇ ğüşıöç ✓ ▸ · → − ±” her fontta doğru; bütün token renkleri ayrıştırılıyor (EditMode test).

### Aşama 2 — Oda (3D) ve ışık
- `SiginakRoomBuilder`: kabuk (beton duvar, kanal, borular, nem lekesi, duvar yazısı “KYGA / SIĞINAK 07”, doktrin afişi,
  mantar pano + KURTARILAN BÖLGELER listesi + KAYIP ilanları, kafesli tavan ampulü), masa (linolyum üst, çelik gövde,
  pirinç ön şerit), sayaç rafı, anahtar rayı, şalter, teleks, banker lambası (sağ arka), evrak tepsisi, kupa, kalem;
  sol raf (yaw −35°: malzeme sandıkları, filizli teneke, kesit maketi, kıskaçlı pano, BOŞTA tepsisi); sağ CRT masası
  (yaw +35°: CRT, klavye, tüplü telsiz).
- Işıklar `tokens.lights` (preview → urpStart); gölgeler: tavan ampulü ve masa lambası. URP Asset: Additional Lights
  “Per Pixel” ve “Cast Shadows” açık (nokta ışık gölgesi için), gölge çözünürlüğü 1024 (ampul) / 2048 (lamba).
  Environment Lighting: Gradient (#7F90A6 → #16140F). Duvar ve zemin dokusu için tek 2K beton dokusu üretmek gerekirse
  basit bir editor script'iyle yap (gürültü + 15 cm kalıp izi + nem lekesi; `textures.js → concreteTex` örnek).
- **Kabul:** K1 pozunun görüntüsü `reference/renders/K1.jpg` ile aynı yerleşimde; raf ve CRT masası kadrajda; masa
  nesneleri okunur boyutta; Ambient/ışık oranı benzer (koyu ama okunur).

### Aşama 3 — Kameralar
- K1: Physical Camera, sensör 36 × 20,25 mm, Gate Fit Vertical, odak ≈ 18,76 mm (dikey FOV 56,7°), Lens Shift (0, −0,324),
  konum (0, 1,52, −0,15), dönüş (0, 0, 0). Ufuk çizgisi üstten 190. pikselde; dikeyler dik kalır.
- Harita pozu (0, 2,75, 1,30) → (0, 0,76, 1,36), FOV 30°; K4 raf yereli (0, 1,15, 1,04) → (0, 1,10, 0), FOV 48°;
  K5a ekran yereli (0, −0,02, 0,62) → (0, −0,035, 0), FOV 46°; K6a (0,32, 1,28, −0,36) → (0,38, 0,86, 1,25), FOV 60°.
- Geçişler 0,45 s ease-in-out (K1 → K2: önce 0,15 s yüksel, sonra eğil). Mevcut kontroller çalışır.
- **Kabul:** her poz `reference/renders/` ile aynı çerçeveyi verir (±%3).

### Aşama 4 — Masa kontrolleri ve geri bildirim
Sayaç rafı (+ ± fişleri), anahtar rayı, şalterin normal/üzerinde/devre dışı halleri, teleks + teleks şeridi, kenar satırı,
üzerinde etiketi + sarı çerçeve, AYARLAR plakası. “Emir uygulandı” / “Yapılamadı” bütün kaynaklardan `TelexFeed`'e bağlanır.
- **Kabul:** kaynak değişince sayaç döner ve fiş 1,5 s asılı kalır; anahtarlar istasyon değiştirir ve kısayollar çalışır;
  yapılamayan bir eylem teleksten gerekçeyle (sayı içerir) satır basar; K4'te kenar satırı kendiliğinden açılır.

### Aşama 5 — Harita ve görev fişi
Harita ışık almaz (UI/Default ya da Unlit); masa lambasının sıcak dairesi Sade'deki `tx_lamp_desk` katmanıyla verilir.
Taramalar `sprites/map_hatch_*` (Tiled, #9A1E14 + token alfası), halkalar `map_grease_ring` / `map_candidate_ring`,
damga `map_stamp_free` + TMP. Katman sırası K2 notunda.
Bölge durumları, etiket/lejant, görev fişi + kırmızı ip, baskın emri (EMİR VERİLDİ damgası, FON −12, 0,3 s sonra CRT'ye),
K3 akışı (özgür damgası, tarama silinir, adaylar çizilir, onay fişi, tek cephe kuralı).
- **Ekip figürü:** `TeamFigure` prefab'ı (ölçü/renk `tokens.space.teamFigure`, `tokens.colors.team`, `tokens.materials.team*`;
  geometri `scene_threejs/room.js → buildTeamFigure`). Yer kuralı, sürükle-bırak, geçersiz bırakma teleksleri ve süreler
  `reference/notes/Ekip-figuru.md`'de. Bölge başına `teamAnchor` (harita px) verisi ekle; yoksa etiket + (0, 110).
  Figürün yeri kalıcı yeni durum değildir: her açılışta oyun durumundan yeniden hesaplanır (kayda yazılmaz).
- **Kabul:** K2/K3 görüntüleri referansla eşleşir; bütün etiketler ekranda ≥ 14 px; durumlar gri tonlamada da ayrılır;
  aday yokken onay fişi açılmaz; cephe açıkken aday seçimi teleks hatası verir; figürü bir adaya sürükleyip bırakmak
  o adaya tıklamakla aynı sonucu verir; geçersiz bırakma figürü geri kaydırır ve gerekçeli teleks basar; Esc ile vazgeçince
  figür eski yerine döner; yaralı ekip figürü alınamaz; sürükleme olmadan da (tıkla + Enter) bütün akış tamamlanır.

### Aşama 6 — Sığınak (K4)
Kesit maketi (çalışan / seçili / enerjisiz / hasarlı / en üst seviye / dolu / yeni), piyon yuvaları ve BOŞTA tepsisi,
oda kartları, özet panosu ve seçili oda kartı, eylemler (ONAR / GELİŞTİR / ± PERS.), sonuç notu, “Eksik: …” satırı.
- **Kabul:** K4-TR/K4-EN görüntüleriyle eşleşir; + PERS. piyonu tepsiden yuvaya taşır; enerji açığı odayı karartır ve
  karta ENERJİSİZ damgası basar; kaynak yetmeyen eylem devre dışı + gerekçe.

### Aşama 7 — CRT (K5)
Bekleme ekranı (fosfor tek renk; yaralı ekip satırı ters video), K5a bakışı (tıkla / sağ tuşla bak), baskın geçişi
(zaman çizelgesi K5 notunda: 0,30 kamera dönüşü · 0,80 ekran açılışı · 0,95 statik + senkron · 2,00 ızgara · 2,30 baskın),
Boşluk ile atlama, dönüşte önceki kamera ve fiş.
- **Kabul:** geçiş 2,3 s, takılmadan; ızgara baskın kamerasının çerçevesine oturur (`tokens_sade.json → raid.camera.screenFromGrid1080`).

### Aşama 8 — Vardiya geçişi (K6)
Şalterin iki adımı ve ön kontrol satırı; dizi (K6 notundaki 0,00–2,60 s çizelgesi: kol, ampul %30, sessizlik, amber ışık,
sayaçlar, teleks, jeneratörün geri gelmesi, kamera, raporun düşmesi); rapor okuma görünümü; fark fişleri; ALFA etiketi;
EVRAK tepsisine dosyalama. Ekip figürü: başarısız baskından sonra S-07'de, düşen üye kadar piyon yatık; ekibin iyileştiği
vardiyanın dizisinde figür cepheye kayar (0,6 s) ve teleks “ALFA CEPHEDE” basar.
- `ShiftReportBuilder` bölümleri: **FON** (açılış · giderler, ör. baskın 12 · vardiya geliri `2 + 0,5 × tamamlanan bölge` ·
  kapanış) · **ÜRETİM VE TÜKETİM** (erzak toplama görevli × 1 · tüketim kişi × 0,2 · malzeme atölye görevlisi × 0,5 enerji
  yeterse · enerji üretim/talep) · **EKİP** (yaralı gün sayısı; revir çalışıyorsa +1 gün hızlı) · **YAPILAMADI** (o vardiyanın
  teleks hataları) · **analist notu** (Kalam, en çok bir satır, yalnız öneri, ör. “→ jeneratöre 2 kişi, atölye döner”).
  Formülleri yeniden yazma: değerleri simülasyondan/snapshot farkından al; formül yalnız açıklama satırıdır.
- **Kabul:** EditMode testi: verilen iki snapshot'tan doğru satırlar ve toplamlar; işaretler (+ / − / ±0) doğru;
  rapor açıkken şalter devre dışı; Boşluk diziyi atlar.

### Aşama 9 — Brifing ve açılış (K7)
Açılış dizisi (siyah → jeneratör marşı → ampul), emir belgesi (okuma görünümü), BAŞLA (kayıt varsa iki adımlı uyarı),
KAYITTAN DEVAM sekmesi (kayıt yoksa soluk + “KAYIT YOK”), belgenin EVRAK tepsisine gitmesi. Gövde metni taslaktır
(TR ve EN metin K7 notunda); yerelleştirme anahtarlarıyla koy.
- **Kabul:** yeni oyun ve kayıttan devam akışları çalışır; mevcut kayıt asla sorulmadan üstüne yazılmaz.

### Aşama 10 — Dil, erişilebilirlik, temizlik
İngilizce geçişi (bütün pozlar EN ile çekilir), en küçük punto taraması, renk körlüğü kontrolü (gri tonlama görüntüsü),
performans (statik nesneleri birleştir/işaretle, gölge sayısı), eski HUD/çubuk/kutuların kaldırılması (onaydan sonra),
`SiginakPresentation` anahtarının varsayılanı yeni görünüm.
- **Kabul:** `screens_unity/` altında TR + EN tam set; tarama raporu temiz; 1920×1080'de kare süresi eskisinden kötü değil.

---

## 6. Ekran ekran özet

Ayrıntı `reference/notes/K*-not.md` ve `reference/boards_html/` içinde; burada yalnızca uygulamayı yönlendiren özet var.

**K1 · Başlangıç pozu** — Bütün oda: solda raf + kesit maketi, ortada harita masası + sayaç rafı + anahtar rayı, sağda CRT
masası, arkada duvar. Kenar satırı yok (sayaç rafı kadrajda). Üzerinde: nesne 4 mm kalkar, plaka parlar, etiket çıkar;
büyük hedefte sarı çerçeve. Ampul 3–9 s'de bir 2–3 kez kırpışır. Umut işaretleri özgür bölge sayısına bağlı (1/3/6/9/12;
K1 notunda) — bunu ayrı ve kapatılabilir bir sistem yap.

**K2 · Harita / görev fişi (TR + EN)** — Görev fişi (436 px, dizin kâğıdı, −1°): başlık (OP kodu CP 700 14 · bölge CP 700 28
· “SEKTÖRLER 1/3 · İŞGAL %66,7”), 3 sektör satırı (işaret + no + ad LF 600 18 + tür CP 700 14 + damga; seçili satır sarı
α .32), görev açıklaması (LF 18), BEDEL / ÖDÜL, ekip satırı (HAZIR / YARALI · n GÜN), mürekkep düğme “Enter · BASKIN EMRİ VER
· −12 fon”. Fiş cepheyi, özgür bölgeleri ve adayları örtmeyen yarıya konur; kırmızı ip cephe raptiyesine bağlanır.

**K3 · Yeni cephe seçimi** — Bölge özgürleşince: mavi damga iner, tarama silinir, sayaçlar döner, teleks iki satır basar,
adaylar 0,12 s arayla kesik halkayla çizilir; oyuncu ekip figürünü bir adaya taşır (ya da adaya tıklar) → onay fişi
“Enter · SEÇİLİ BÖLGEDE CEPHE AÇ” / Esc vazgeç (figür geri kayar). Ekranda figür Avrasya'da, sarı halkayla; geldiği yerden
kesik kurşun kalem izi. Adaylar `GameData.Links` ile özgür bölgelere komşu işgal altındaki bölgeler. Aynı anda tek cephe.

**Ekip figürü (pano: Ekip)** — Haritadaki fiziksel figür; yeri oyun durumundan gelir, elle taşınır. Dört hali: hazır (cephede,
dik) · üzerinde/seçili (sarı halka) · taşınıyor (25 mm havada, −6°, ayrık gölge) · yaralı (S-07'de, piyon yatık).
Görev fişinin ekip satırında ve raporda figürün üstten simgesi (`sprites/icon_team_figure.png`).

**K4 · Sığınak (TR + EN)** — Raf pozu. Odalar 2 × 2: yaşam alanı, jeneratör, atölye, revir. Çalışan oda aydınlık, enerjisiz
oda karanlık (duvar #5A554B), hasarlı oda moloz + düşmüş kiriş. Kart: ad CP 700 29 dünya px (~20 ekran px), satır 21 (~14,7).
Seçili: oda ağzına 8 px sarı + 2 px mürekkep, kart #FFFDF5 + iç sarı. Özet panosu: TOPLULUK · ENERJİ (üretim/talep, açık
kırmızı) · ERZAK · SON VARDİYA; altında seçili odanın kartı (açıklama, sonuç notu, maliyet, hazırlık, eylemler 72 px).
Kenar satırı açık; teleks şeridi altta.

**K5 · CRT** — K5a: ekran yakın bakışı, “BAĞLANTI BEKLENİYOR”, ekip ve cephe satırları, yanıp sönen imleç. K5b: geçişin
tam ekran karesi (cam köşeleri, statik, yuvarlanan bant, “BAĞLANTI KURULUYOR”, SENKRON 10 kutu + yüzde, hedef satırı).

**K6 · Vardiya geçişi** — K6a: şalter anı (kapak açık, kol inik, oda kararmış, VARDİYA çarkı dönüyor), sağ üstte ATLA,
teleks şeridi. K6b: harita pozunda rapor (manila teleks kâğıdı, iki yanda delik sırası, kopma çizgisi, KAPANDI damgası),
sayaç fişleri, ALFA etiketi, “Enter · VARDİYA 015'İ BAŞLAT”, “Esc · rapor EVRAK tepsisine dosyalanır”.

**K7 · Brifing** — Harita pozunda emir belgesi (KYGA başlığı, EMİR NO. 001, KİME/KONU, GİZLİ damgası, 4 paragraf CP 400 20,
imza), BAŞLA (Enter), altta manila dosya + sağ kenarda KAYITTAN DEVAM sekmesi (Tab → Enter).

**K8 · Stil sayfası** — Değerlerin hepsi token'da; görsel kontrol için `reference/screens/K8.jpg`. Düğme/şalter durum
matrisi, harita durumları, sektör işaretleri ve oda durumları buradan doğrulanır.

---

## 7. Doğrulama döngüsü

1. **Durum fikstürü:** editor-only bir “V.014 referans durumu” yükleyicisi yaz (simülasyonu değiştirmeden: kayıt dosyası,
   mevcut komutlar ya da debug komutları ile): Fon 62,0 · Erzak 31/40 · Malzeme 19 · Özgür 1/12 (Batı Avrupa) · Doğu Avrupa
   cephe 1/3 (01 geri alındı, 02 hazır, 03 kilitli) · ekip hazır · odalar: yaşam SV1 2/2, jeneratör SV1 0/2, atölye SV3 2/6
   (enerjisiz), revir hasarlı · boşta 4 · topluluk 8/8 · ekip figürü Doğu Avrupa'da. K3 için V.019 (figür Doğu Avrupa → Avrasya),
   K6 için V.014 → 015 (raid başarısız, figür S-07'de yaralı), K7 için V.001 (figür S-07'de).
   Yapamıyorsan nedenini yaz ve elindeki durumla devam et.
2. **Poz görüntüsü:** `Tools/Sığınak/Capture Poses` — her pozu ve durumu kurup 1920×1080 PNG'yi
   `Docs/UI_Siginak/screens_unity/K*.png` (TR) ve `*_EN.png` olarak kaydeder.
3. **Karşılaştır:** her görüntüyü `reference/screens/` karşılığıyla yan yana aç (görüntü okuyabiliyorsun); yerleşim,
   okunurluk, renk ve ışık farklarını `IMPLEMENTATION_MAP.md` → “Görsel farklar”a yaz, kapat.
4. **Otomatik testler (EditMode):** token importu · `ShiftReportBuilder` hesapları · TR/EN anahtarlarının tamlığı ·
   punto tarayıcısı (Siginak prefab'larındaki TMP'leri gezip 1080p'de < 14 px olanları listeler; World Space metinleri
   ilgili pozun kamerasıyla izdüşürerek ölçer).
5. Renk körlüğü: her pozun gri tonlama kopyasını kaydet ve durumların ayrıldığını doğrula.

---

## 8. Çalışma biçimi ve teslim

- Her aşamanın sonunda kısa rapor: değişen dosyalar, çekilen görüntüler, açık sorular. Büyük belirsizlikte dur ve sor.
- `IMPLEMENTATION_MAP.md` içinde bir **CHANGELOG** tut.
- Küçük, aşama aşama değişiklikler; commit'ten önce bana sor.
- Eski davranışı bozan bir şey gerekirse önce yaz, onay al.

---

## 9. Sözlük (TR → EN)

| TR | EN | | TR | EN |
|---|---|---|---|---|
| KOMUTA | COMMAND | | GERİ ALINDI | RETAKEN |
| SIĞINAK | SHELTER | | BASKINA HAZIR | READY TO RAID |
| GÖREVLER | MISSIONS | | KİLİTLİ | LOCKED |
| KILAVUZ | GUIDE | | YAĞMA | LOOT |
| MASA | DESK | | SUNUCU YIKIMI | SERVER DEMOLITION |
| AYARLAR | SETTINGS | | ÖZGÜR (damga) | FREE |
| ATLA | SKIP | | ADAY | CANDIDATE |
| VARDİYAYI BİTİR | END SHIFT | | HAZIR / YARALI | READY / INJURED |
| VARDİYA · FON · ERZAK · MALZEME · ÖZGÜR BÖLGE | SHIFT · FUNDS · RATIONS · MATERIALS · FREE REGIONS | | HASARLI / ENERJİSİZ | DAMAGED / NO POWER |
| V.014 | S.014 | | EMİR UYGULANDI / YAPILAMADI | ORDER EXECUTED / FAILED |
| EKİP ALFA | TEAM ALPHA | | BASKIN EMRİ VER | ORDER THE RAID |
| TELEKS | TELEX | | SEÇİLİ BÖLGEDE CEPHE AÇ | OPEN FRONT IN SELECTED REGION |
| EVRAK | PAPERS | | GELİŞTİR / ONAR | UPGRADE / REPAIR |
| SAHA YAYINI | FIELD FEED | | + PERS. / − PERS. | + STAFF / − STAFF |
| BAĞLANTI BEKLENİYOR / KURULUYOR | AWAITING LINK / ESTABLISHING LINK | | VARDİYA RAPORU | SHIFT REPORT |
| VARDİYA 015'İ BAŞLAT | START SHIFT 015 | | BAŞLA / KAYITTAN DEVAM | BEGIN / CONTINUE |
| BOŞTA | IDLE | | KAPANDI / GİZLİ / KAYIT YOK | CLOSED / TOP SECRET / NO SAVE |
| EKİP FİGÜRÜ | TEAM FIGURE | | ALFA CEPHEDE | ALPHA AT THE FRONT |
| BIRAK: <bölge> | DROP: <region> | | Sürükle: adaya taşı | Drag onto a candidate |

Sayı biçimi: TR ondalık virgül (62,0 · %66,7), EN nokta (62.0 · 66.7%). Kesik çizgi ve eksi işareti tipografik (−).
