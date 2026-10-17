# SON ŞALTER · Fütüristik arayüz (FX): YZ küresi, holo harita, holo baskın — Unity uygulaması

> **Bu dosya Claude Code için uygulama talimatıdır.**
> - Paket kökü bu dosyanın bulunduğu klasördür. Önerilen yer `Docs/UI_Fx/` (`Assets/` dışında). Aşağıdaki tüm yollar bu klasöre göredir.
> - Bu paket **Sade paketinin** (`Docs/UI_Sade/`, Sade PROMPT.md 1.1) üstüne kurulur. Yerleşim, görünüm modeli (VM), komutlar, etkileşim ve 3D tahta geometrisi oradan gelir.
> - Bu paket **görsel dili** değiştirir ve üç yeni şey ekler: **YZ küresi**, **holo harita boyası**, **holo baskın tahtası**. Ayrıca bütün arayüze sürekli hareket ekler.
> - Kod tanımlayıcıları İngilizce, açıklamalar Türkçedir.
> - Sürüm 1.0 (16.10.2026). Tasarım kaynağı: Claude Design kanvası, “Fütüristik · YZ küresi” sayfası (19 kare).

**İçindekiler**
- 0 Görev, ön koşul ve çalışma biçimi
- 1 Fikir: Sade → FX eşlemesi
- 2 Kurallar (Sade'den değişenler)
- 3 Paket içeriği
- 4 Ortak mimari (klasör, VM ekleri, tema, canvas, bileşenler, sprite, doku, font, hareket, performans)
- 5 YZ küresi
- **A · F-Masa** (fütüristik analist masası)
- **B · F-Baskın** (holo taktik harita)
- 6 Hareket kataloğu
- 7 Doğrulama
- 8 Fazlar ve kabul ölçütleri
- 9 Açık kararlar
- 10 Yapma listesi
- 11 İlk yanıtın (F0)

---

## 0. Görev, ön koşul ve çalışma biçimi

### Görev

Sade arayüzün **fütüristik (FX) görünümünü** kur. Ekranlar, anlar ve veriler Sade'deki ile aynıdır; değişen görsel dil ve hareket.

| Kod | Ekran | Referans | Veri |
|---|---|---|---|
| **F-YZ** | YZ küresi laboratuvarı: üç hâl + canlı geçiş. Oyunda ekran değil, **geliştirme ve doğrulama sahnesi** (`FxOrbLab`) | `reference/screens/F-YZ.jpg` | yerel durum (§5.6) |
| **F-Masa1** | Varsayılan masa: harita önde, her bölgede YZ küresi | `F-Masa1.jpg` | Sade `masa_S1.json` |
| **F-Masa2** | Doğu Asya seçili, bölge kartı | `F-Masa2.jpg` | `masa_S2.json` + ekler |
| **F-Masa3** | Araştırma çekmecesi + FON döküm fişi | `F-Masa3.jpg` | `masa_S3.json` |
| **F-Masa4** | Haberler çekmecesi; üstüne gelinen haberin bölgesi yanar | `F-Masa4.jpg` | `masa_S4.json` |
| **F-Masa5** | Ekipler çekmecesi; Ekip B'nin jetonu halkalı | `F-Masa5.jpg` | `masa_S5.json` |
| **F-Masa6** | YZ günlüğü çekmecesi | `F-Masa6.jpg` | `masa_S6.json` |
| **F-Masa7** | Rehber çekmecesi, **yeni ilk konu: YZ hâlleri** | `F-Masa7.jpg` | `masa_S7.json` + ekler |
| **F-Masa8** | Lejant kartı + Güney Asya düğüm ipucu | `F-Masa8.jpg` | `masa_S8.json` + ekler |
| **F-Masa9** | Tam bölge dosyası | `F-Masa9.jpg` | `masa_S9.json` + ekler |
| **F-Baskin1…9** | S-Baskin1…9'un aynı anları, holo tahta | `F-Baskin1…9.jpg` | `baskin_S1…S9.json` + ekler |

“Ekler”: `spec/fx_frames.json › frames[].vm` (§4.2). Her F karesi bir Sade karesinin aynı anıdır.

**Sadakat hedefi**

- **UI:** 1920×1080'de ±2 px. Yumuşak ışımaların dış sınırı ±4 px.
- **Küre:** piksel eşitliği beklenmez. Belirlenimci altın karelerle karşılaştırma (§5.5) + laboratuvarda gözle hâl geçişi.
- **Holo tahta:** kadraj, parça ve işaret yerleri ±4 px (Sade ile aynı geometri). Renkler `raid_fx_palette.json`'daki değerler (Unlit, ışık yok → birebir tutmalı).
- **Hareket:** süreler ±%10, eğri benzer. Videolar referanstır (`reference/motion/`).

### Kapsam dışı

- Oyun kuralları ve simülasyon. Arayüz sahte veriyle çalışır.
- Sade'nin davranışını değiştirmek. FX görünüm katmanıdır.
- Kâğıt görselleri (C ve Sade sprite'ları, kâğıt bileşenleri): FX'te kullanılmaz. **İstisna:** beyaz simge sprite'ları (`ic_*_s`, `ic_*_l`, §4.6).

### Ön koşul: Sade paketi

- `Docs/UI_Sade/` klasörü projede olmalı. Mock'lar, `map_regions.json`, `spec/raid/`, `reference/raid_B/` ve simge sprite'ları oradan okunur. Yoksa kullanıcıdan iste.
- **Durum A — Sade kurulu** (S-Masa ve S-Baskın çalışıyor):
  - FX, aynı VM ve komutlara bağlanan yeni bir görünüm katmanıdır.
  - Sade'nin davranış kodunu yeniden kullan: VM bağlama, yerleşim, panel kuralları, girdi, 3D tahta kurulumu.
  - Görünüm sınıfı davranışla iç içeyse ayırmayı **önce sor** (Sade koduna dokunmak). Önerilen ayrım: davranış = `*Presenter` / soyut temel; görünüm = Sade ve FX prefab'ları.
  - Sade prefab'ları silinmez (açık karar 1).
- **Durum B — Sade kurulmadı:**
  - Sade PROMPT'tan yalnız **görünümden bağımsız** parçaları uygula. F0'da listele:
    - VM şeması + mock kaynaklar + komutlar (Sade §4.2),
    - `MapProjection` ve bölge mesh verisi (Sade §A.2),
    - `MapCoverFitter` + `MapMarkersLayer` (Sade §4.4),
    - `PopupCardBase` yerleşim kuralı (Sade §A.11; FX farkı §A.9),
    - panel kuralları ve Esc sırası (Sade §A.13, §A.14.1),
    - `BoardBuilder` geometrisi + kamera k = 1,5 + ızgara → ekran formülü (Sade §B.1–B.2),
    - dünya etiketi konumlandırma (Sade §B.3), girdi tablosu (Sade §B.6).
  - Sade'nin kâğıt görsellerini **kurma**.
- **C paketi** FX için gerekmez. Projede varsa genel bileşenlerini yeniden kullan: `UILineGraphic`, `UIPolygonGraphic`, `TooltipService`, `DeskFormat` (TR sayı biçimi: 47,3 · %37 · −1,2). Yoksa `DeskFormat`'ın bu üç kuralını küçük bir sınıfla yaz.

### Çalışma biçimi

Sade §0 aynen geçerli. Kısaca:

1. **Önce oku.** Bu dosyayı baştan sona oku. Sonra şunlara bak:
   - `reference/screens/*.jpg` (19 kare),
   - `reference/motion/*.mp4` (9 video; **hareket tasarımın parçasıdır**),
   - `art/sprites/_contact_sheet.png`, `reference/orb/golden/_contact_golden.png`,
   - `tools/orb_lab.html` (tarayıcıda aç: küreyi canlı oynat),
   - `spec/tokens_fx.json`, `spec/orb/ORB_ALGORITHM.md`.

   Ayrıntı gerektikçe:
   - `reference/zoom/*@2x.*` (36 yakın plan),
   - `reference/measure/F-*.tsv` + `F-*_mask.json`,
   - `reference/html/*.html` (bağımsız; animasyonlar CSS'te),
   - `spec/map_fx.json`, `spec/fx_frames.json`, `spec/raid_fx/raid_fx_palette.json`,
   - Sade paketi: `spec/mock/`, `spec/map_regions.json`, `spec/raid/`, `reference/raid_B/`.
2. **Faz 0** (§8): projeyi incele, `Docs/UI_Fx/PLAN.md` yaz. **Onay almadan kod yazma.**
3. **Her fazın sonunda dur.** Kısa rapor ver: ne yapıldı, referanstan sapmalar, karşılaştırma çıktıları, açık sorular. Onay bekle. Kullanıcı “onay sormadan devam et” derse sürdür.
4. **Belirsizlikte şu sırayla bak:** referans görüntü → TSV → HTML/CSS → `tokens_fx.json` → Sade PROMPT.
   - Hâlâ belirsizse tasarıma en yakın seçeneği uygula, `Docs/UI_Fx/KARARLAR.md`'ye tek satır yaz.
   - Bloklayan en çok 3 soruyu birlikte sor.
5. **Projenin kuralları önce gelir:** namespace, asmdef, DI, async, tween, lokalizasyon, Input System, URP ayarları.
6. **Önce sor:** paket eklemek; Project / Player / Quality / URP Asset / Volume ayarı değiştirmek; mevcut sahne ya da prefab'a dokunmak; Sade koduna dokunmak. Commit'i yalnız kullanıcı isterse at.
7. **Unity editörüyle çalışma.** Bağlı bir Unity MCP sunucusu varsa onu kullan. Yoksa kodu ve Editor betiklerini yaz; kullanıcıdan menü komutlarını çalıştırmasını ve ekran görüntüsü paylaşmasını iste. `.prefab` / `.unity` YAML'ını elle yazma; builder betikleri kullan.
8. **Sıra.** F0 → F1 (ortak) → F2 (küre) → masa ya da baskın fazları (§8). Kullanıcı yalnız birini isterse F0–F2 + o ekranın fazları.

---

## 1. Fikir: Sade → FX eşlemesi

- **Düzen ve veri aynı.** Harita / maket tam ekran, arayüz köşelerde, ayrıntı açılır panellerde.
- **Görsel dil holo:**
  - koyu cam paneller, 1 px camgöbeği kenar, iki köşe parantezi, üst kenarda gezen ışık;
  - neon kenarlar ve yumuşak ışımalar;
  - sayılar tek aralıklı (IBM Plex Mono), etiketler Chakra Petch, gövde IBM Plex Sans.
- **Ana yenilik: YZ küresi.** Her bölgenin üstünde, o bölgedeki YZ sızmasını gösteren dönen bir parçacık küresi. Değer arttıkça üç hâl arasında **akıcı** geçer:

  | Hâl | Sızma | Görünüm |
  |---|---|---|
  | 1 · UYUMLU | 0–30 | altın veri küresi, düzenli teller, yörünge halkaları, yavaş |
  | 2 · KARARSIZ | 30–70 | altın + mavi parçacıklar karışır, şimşek lifleri, titreşen kabuk |
  | 3 · UYANMIŞ | 70–100 | buz mavisi sivri kristal, yörüngede kırıklar, kıvılcım, hızlı |

  Bölgenin harita boyası da aynı renk eğrisini izler (altın → mavi).
- **Hareket her yerde:** girişler, sürekli döngüler (tarama ışığı, akan iplikler, nabızlar, dönen halkalar), daktilo satırı, çizilerek gelen grafikler.

| Sade'de | FX'te |
|---|---|
| Kâğıt etiket, dizin kartı, manila çekmece | Cam panel (`fx_glass*` 9-dilim + parantez + süpürme + gölge) |
| Sızma rampası (5 kırmızı ton), düz bölge dolgusu | Nokta matrisi + düğüm çevresinde parıltı; renk altın → mavi, sürekli (§A.2) |
| Bölge düğümü (disk + sayı + rozet) | **YZ küresi** + altında değer çipi (sayı + güven rozeti) (§A.3) |
| Güven: mühür / “~” fişi / turuncu halka / kesik daire | ✓ nane daire / ≈ mor kesik çerçeve / ? kehribar kesik daire (nabız) / gri kesik daire; SAHTE kırmızı hap |
| Kırmızı yayılım ipliği + ok ucu | Mavi akan iplik: ışıma + kesik çizgi + akan parçacık; ok ucu yok, yön akıştan okunur |
| Çekirdek dalgası | Kürenin kendi şok halkaları (`core` türü) |
| Kart ipliği + raptiye | Düz bağ çizgisi, kesik ve akan, iki uçta nokta |
| Yazı tipleri CP / LF / Kalam | Chakra Petch / IBM Plex Mono / IBM Plex Sans |
| Lamba dokusu | Vinyet + tarama çizgileri + 12 sn'lik tarama ışığı |
| Zarf (Son Şalter) | Kırmızı cam düğme, kayan tehlike şeridi, dönen kesik halka |
| Ozalit maket (Lit, masa lambası) | Holo maket: Unlit koyu cam yüzler + neon kenarlar, tarama düzlemi, titreme |
| Tahtada yalnız işaretler | + YZ düğüm dolabının üstünde süzülen küçük küre (§B.6) |

---

## 2. Kurallar (Sade'den değişenler)

Sade §2'nin şu kuralları **aynen** geçerli:

- köşe kuralı,
- harita kökü ekranı kaplar (cover); harita üstündeki yazı ve işaretler harita ölçeğiyle büyümez,
- panel kuralları (bir bağlam kartı + bir çekmece; seçili bölge / birim örtülmez),
- görünüm kural hesaplamaz,
- renk tek başına anlam taşımaz,
- TR + EN; Auto Size kapalı; TMP UpperCase kapalı,
- en küçük yazı 11 px,
- kaldırılan öğeler geri gelmez.

**Değişen ve eklenen kurallar**

1. **Fontlar:** Chakra Petch (400/500/600/700), IBM Plex Mono (400/500/600), IBM Plex Sans (400/500/600). TTF'ler `fonts/ttf/` (OFL). Kalam ve Libre Franklin FX'te yok (§4.8).
2. **Özel UI shader'ları serbest**, sınırlı küme (§4.5):
   - `UI/FxAdditive`, `UI/FxRegion`, `UI/FxLine`, `UI/FxOrbComposite`,
   - RenderTexture içinde `Hidden/FxOrbDraw`.

   Hepsi `UI/Default`'un stencil, `RectMask2D` kırpma ve `_ClipRect` özelliklerini taşır.
3. **Işıma = önceden pişmiş sprite ya da eklemeli katman.**
   - UI için post-processing bloom kullanma.
   - Kutu ışıması `fx_glow_frame` (9-dilim, tint), nokta ışıması `fx_glow_dot_32`, metin ışıması TMP Underlay (§4.8).
4. **Yumuşak gölge serbest** (C/Sade'deki “bulanıklık 0” kuralı FX'te yok): `fx_shadow_panel`.
5. **Cam bulanıklığı (backdrop blur) yok** (açık karar 4). Cam paneller yarı saydam geçiştir; altındaki harita görünür.
6. **Sürekli hareket zorunlu** ve tek saatten akar (`FxClock`, §4.9).
   - Oyun ayarı **“Hareketi azalt”**: döngüler durur (tanımlı evrede donar), küreler 8 fps, giriş animasyonları atlanır, daktilo tam metni gösterir.
   - Duraklatma menüsü açıkken döngüler sürer (görsel); oyun hızı (⏸, 1×, 2×, 4×) döngüleri etkilemez.
7. **3D tahta (holo):**
   - URP **Unlit**, ışıksız; yüz başına düz renk (`raid_fx_palette.json`), kenarlar ayrı çizgi geometrisi (§B.2).
   - Gerçek zamanlı ışık ve gölge yok.
   - Bloom **varsayılan yok** (yol B). Yol A (URP Bloom + HDR kenar rengi) yalnız kullanıcı onayıyla, çünkü URP Asset / Volume değişikliğidir (açık karar 6).
8. **Küre yalnız `OrbSimulation`'dan çizilir** (§5). Sprite dizisi, video, Shuriken ya da VFX Graph ile taklit edilmez.
9. **Renk tek başına anlam taşımaz — FX'te de.**
   - Güven rozetleri şekil + simge taşır (✓, ≈, ?, kesik daire, SAHTE yazısı).
   - YZ hâli adıyla yazılır (“HÂL 2 · KARARSIZ”).
   - Tehlike kırmızısının yanında simge ya da metin vardır.
10. **Pikselde kalan çizgiler.** 1 px kenarlar tam piksel konum ve boyutta durur (Pixel Perfect kapalı; Sade §4.4). Işımalar bu kurala girmez.

---

## 3. Paket içeriği

| Yol | İçerik | Ne için |
|---|---|---|
| `PROMPT.md` | bu dosya | talimat |
| `reference/screens/` | 19 kare, 1920×1080, JPEG q86 4:4:4. Döngüler belirli evrede donduruldu, tarama ışıkları gizlendi | **görsel gerçek** |
| `reference/motion/` | 9 video (H.264, 30 fps, 1280 px geniş, 4–20 sn): `kure_canli_gecis`, `kure_uc_hal_yakin`, `masa_acilis`, `masa_bolge_karti`, `masa_cekmece_ve_dokum`, `masa_yz_gunlugu`, `baskin_holo_tahta`, `baskin_makineler`, `baskin_sonuc` | **hareket gerçeği** |
| `reference/zoom/` | 36 yakın plan @2x (büyükler JPEG) | küçük ayrıntı |
| `reference/measure/F-*.tsv` | her işaretli öğe ve çocukları: kutu, font, renk, zemin, kenar, gölge, dönüşüm, opaklık, metin | **sayısal gerçek** |
| `reference/measure/F-*_mask.json` | karedeki kürelerin kutuları (karşılaştırmada dışarıda bırakılır) | doğrulama |
| `reference/html/` | 19 bağımsız HTML (fontlar `fonts/web/`) + `fx-harita.svg`. Tarayıcıda canlı: küreler döner, animasyonlar oynar | CSS ve hareket kaynağı |
| `reference/orb/golden/` | 22 belirlenimci küre karesi + `golden.json` + `_contact_golden.png` | küre doğrulama |
| `spec/tokens_fx.json` | renk, yazı tablosu, panel, masa ve baskın ölçüleri, hareket kataloğu, küre boyları | tema |
| `spec/map_fx.json` | harita katmanları, bölge boyası formülleri, düğüm/değer/etiket konumları, bindirme (iplik, rota, seçim, vurgu, jeton, uyarı), ekran katmanları | harita |
| `spec/fx_frames.json` | kare başına: Sade karşılığı, mock, VM ekleri, notlar, üst düzey kutular, küre listesi (düzey, r, tür, tohum, merkez) | kare eşlemesi |
| `spec/orb/ORB_ALGORITHM.md`, `orb_constants.json` | küre algoritması satır satır + Unity karşılığı; sabitler | **küre** |
| `spec/raid_fx/raid_fx_palette.json` | kâğıt maket rengi → holo dolgu + kenar; işaret, niyet, yazı, ışıma | holo tahta |
| `spec/strings_fx_tr_en.csv` | yeni ve değişen 40 dize (TR + taslak EN) | lokalizasyon |
| `art/orb/` | `fx_orb.js` (**kanonik motor**), `orb_glow_profile_128.png`, `orb_line_aa_1x8.png` | küre |
| `art/map/` | `fx_map_base@2x.jpg` (3840×2160, alfasız), `fx_map_lines@2x.png` (saydam), `_fx_map_full_ref.jpg` | harita katmanları |
| `art/textures/` | 14 doku: döşeme (nokta, tarama, altıgen, LED, karartı, ızgara, tehlike, tarama çizgisi), tarama ışıkları, vinyet, baskın zemini + koni, F-YZ zemini | desen |
| `art/sprites/` | 73 sprite @2x + `sprites.json` + `_contact_sheet.png` | sprite kiti |
| `art/raid/` | `tx_fx_plan_grid_2048.png`, `tx_fx_plan_ana_sunucu_salonu_2048.png` | holo zemin dokusu |
| `fonts/ttf/` | 10 TTF + OFL lisansları (TMP için) | fontlar |
| `fonts/web/` | woff2 (yalnız HTML için) | — |
| `tools/compare.py` | yakalama ↔ referans karşılaştırması; `--mask` ile küreleri dışarıda bırakır | doğrulama |
| `tools/orb_lab.html` | küre laboratuvarı: sızma, tür, yarıçap, OTOMATİK, zemin; fps göstergesi | inceleme |

**Sade'den gelenler (bu pakette yok):** mock'lar, `map_regions.json` (çokgenler + üçgenler), `spec/raid/` (tahta durumları, 3D ölçüler, dizeler), `reference/raid_B/`, beyaz simge sprite'ları `ic_*_s` / `ic_*_l`, Sade dizeleri.

**Çelişki önceliği:** görünüm için ekran görüntüsü, sayılar için TSV, hareket için video + CSS, davranış için bu dosya (sonra Sade PROMPT). CSS ile TSV farklıysa **TSV doğrudur.** Küre için `art/orb/fx_orb.js` doğrudur (belgesi değil).

---

## 4. Ortak mimari

### 4.1 Klasör ve assembly

Projede başka bir düzen varsa ona uy.

```
Assets/_Project/UI/Fx/        Scripts/Runtime/{Theme,Common,Motion,Orb,Map,Desk,Raid}, Shaders/, Prefabs/{Common,Desk,Raid}, Art/{Sprites,Textures,Map,Fonts}
Assets/_Project/Raid/Board/   (Sade) + Materials/Fx/, Scripts/Runtime/Fx/{HoloMaterials,HoloEdges,BoardFx}
Scenes: FxOrbLab.unity, FxDesk_Sandbox.unity, FxRaid_Sandbox.unity, FxUI_Gallery.unity
Docs/UI_Fx/: PLAN.md, KARARLAR.md, UYGULAMA_NOTLARI.md, captures/
```

Assembly'ler:

- `SonSalter.UI.Fx` (çalışma anı; Sade varsa `SonSalter.UI.Desk` ve `SonSalter.UI.Raid`'e başvurur),
- `SonSalter.UI.Fx.Orb` (küre; UI'dan bağımsız, saf C# + çizim),
- her biri için `.Editor` ve `.Tests`.

### 4.2 Veri sınırı (VM ekleri)

Sade'nin `SadeDeskViewModel`, `RaidViewModel`, `IDeskCommands`, `IRaidCommands` arayüzleri **aynen** kullanılır. FX yalnız birkaç alan ekler. Örnek değerler `spec/fx_frames.json › frames[].vm`'de.

**Masa**

- **Küre türü ve düzeyi** mevcut alanlardan türetilir. Yeni alan gerekmez:

  | Koşul (sırayla) | `OrbKind` | Düzey |
  |---|---|---|
  | `infiltration == null` ya da `trust == unknown` | `Unknown` | 0 |
  | `aiCore` | `Core` | `infiltration` |
  | `quarantine` | `Quarantine` | `infiltration` |
  | diğer | `Normal` | `infiltration` |

  Bu eşleme görünüm katmanında değil, VM kurucusunda ya da küçük bir `OrbStateMapper`'da yapılır.
- **YZ hâli** (1 / 2 / 3 / bilinmiyor), sızmanın 30 ve 70 eşikleriyle sınıflanmış hâli.
  - VM'e eklenir: `RegionCard.AiState`, `NodeTooltip.AiState`, `Dossier.AiState`.
  - Eşikler oyun tasarımıdır: `AiStateRules` (varsayılan 30 / 70) VM tarafında. Görünüm hesaplamaz.
  - Bölge kartı ayrıca `AiState.Sources = same | split` alır: iki kaynak (rapor, saha) aynı hâldeyse “iki kaynakta da”, değilse “kaynağa göre 1–2” (`card.aiState.split`).
- **Bölge kartı ölçeği:** `RegionCard.Infiltration.Scale = { markers: [{kind, value, trust, labelSide}], band: [a, b] }`. Sade'de `range` ve `sources` vardı; FX aynı veriyi işaretçi + bant olarak çizer.
- **Rehber:** `Drawer.Guide.Topics` başına `{id: aiStates, icon: chip, title: guide.topic.aiStates}` eklenir (7 konu). Sayfa `aiStates`: üç satır (düzey 12 / 50 / 92), özel küreler (unknown, quar, core), canlı geçiş şeridi.
- **Tam dosya bağlantıları:** her bağlantı çipinde bağlı bölgenin küresi. Düzey ve tür `Map.Regions`'tan.

**Baskın**

- `Board.NodeOrb = { Level, Kind, Collapsed }`: YZ düğüm dolabının üstündeki küre.
- `Hud.Objective.AiNode = { Level, State }`: HEDEF panelindeki HÂL çipi.
- Mock değerleri: F-Baskin1, 2, 4, 5, 6, 8 → 58 (hâl 2); F-Baskin3 → 64; F-Baskin7 → 86 (hâl 3); F-Baskin9 → `Collapsed = true`.
- Düzeyin oyunda neyden geldiği açık karar 9.

**Küre görsel parametreleri VM değildir:**

- yarıçap formülü (§5.1),
- tohum: bölge dizinine göre `seed = i·7 + 3` (sıra: KA, OA, GA, BA, DA, ODKA, SAA, AV, GuA, DoA, GDA, OK). Referanslarla eşleşmesi için bu eşlemeyi koru.
- Panel ve baskın küre tohumları `fx_frames.json › orbs[].seed`'de.

**Sahte kaynaklar:** Sade `MockSadeDeskDataSource` / `MockRaidDataSource` + `FxMockOverlay`. `FxMockOverlay`, `fx_frames.json › vm` alanlarını Sade mock'unun üstüne yazar.

### 4.3 Tema (`FxTheme`)

- ScriptableObject. Kaynak: `tokens_fx.json` (+ `map_fx.json`, `raid_fx_palette.json`).
- Editor menüsü `Tools/FX UI/Import Tokens` JSON'lardan doldurur. Bileşenler sabit hex yazmaz.
- **Renkler:** `tokens_fx.colors`. Alfalı renkler `[r, g, b, a]` dizisidir.

  | Ad | Hex | Kullanım |
  |---|---|---|
  | bg | `#02050A` | zemin |
  | cy / cy2 / cy3 | `#5BE4FF` / `#B4F5FF` / `#2FA8D8` | ana vurgu, kenar, parlak, koyu camgöbeği |
  | tx / tx2 / tx3 | `#E4F8FF` / `#9CC4D4` / `#6E8FA0` | metin 1–3 |
  | am / am2 | `#FFB547` / `#FFD58C` | kehribar: şüpheli, uyarı, seçili, araştırma |
  | ai | `#3D8BFF` | YZ mavisi (iplik, sıcak değer) |
  | ice | `#B3F1FF` | hâl 3 ucu |
  | rd / rd2 | `#FF4D63` / `#FF8A97` | YZ, tehlike, hasar |
  | mt | `#3DFFB4` | doğrulandı, ekip, iyi |
  | vi | `#A98EFF` | rapor (≈), rehber |
  | hâl 1 / 2 / 3 | `#FFB547` / `#FF9F5A` / `#7FC4FF` | hâl adları ve bantları |

- **Yazı tablosu:** `tokens_fx.type`. Her girdi `[aile, ağırlık, px, harf aralığı em, renk, not]`. TMP `characterSpacing = em × 100`.
- **Hâl metin rengi:** hâl 2 `#FFD2B0` (çizildi). Hâl 1 `#FFE0A8`, hâl 3 `#D4ECFF` önerilir (çizilmedi; KARARLAR).

### 4.4 Canvas düzeni ve en-boy oranı

Sade §4.4 **aynen** geçerli: kök canvas ayarları, masa canvas'ları (MapRoot 0, Hud 20, Context 40, Drawer 50, Modal 100, Tooltip 110), cover kuralı, ölçeklenmeyen işaretler, baskın katmanları. FX ekleri:

| Canvas / katman | Sıra | İçerik |
|---|---|---|
| `MapRoot` | 0 | harita katmanları (§A.1). **Küreler `MapMarkers` içinde**, ölçeklenmez; yalnız yerleri haritayı izler |
| `ScreenFx` (yeni) | 10 | tarama çizgileri, tarama ışığı, vinyet (§A.5). Ekrana gerilir, harita ölçeğini izlemez. `raycastTarget` kapalı |
| `Hud` … `Tooltip` | Sade | cam paneller |

- **Panel içi küreler** (kart, rehber, lejant, dosya, HEDEF çipi) panelin kendi canvas'ında, `OrbView` olarak.
- **Baskın:** arka plan Screen Space – Camera (Sade gibi): `tx_fx_raid_bg` + koni. Tahta efektleri (tarama düzlemi) `BoardFx` overlay katmanında, sıra 5 (`WorldLabels`'ın altında). Düğüm küresi `WorldLabels`'ta (sıra 10), dünya etiketi gibi her karede izdüşümle yerleşir.

### 4.5 Bileşen kataloğu

Her bileşen ayrı prefab + script. Davranış Sade'den (Durum A) ya da Sade şemasıyla yeniden (Durum B).

**Ortak görsel parçalar**

| Bileşen | Görev |
|---|---|
| `FxPanel` | Cam 9-dilim (`fx_glass`, `fx_glass_solid`, `fx_glass_red`, `fx_glass_team`, `fx_glass_kill`, `fx_glass_btn`) + gölge (`fx_shadow_panel`, 34 px taşar) + iki parantez (`fx_bracket_10`, sol üst ve 180° sağ alt, 2 px dışarıda, tint = vurgu rengi, ışıma) + üst kenar süpürmesi (`FxSweep`) |
| `FxSweep` | 1 px şerit, `RectMask2D`; içinde `fx_sweep_128` (genişlik panelin %28'i) x: −0,336W → 1,176W, 5,2 sn doğrusal, döngü |
| `FxGlow` | Kutu ya da nokta ışıması (`fx_glow_frame` / `fx_glow_dot_32`), tint + alfa; nabız seçeneği |
| `FxLineGraphic : MaskableGraphic` | Çoklu çizgi / karesel Bézier; kalınlık, kesik [çizgi, boşluk], akış hızı (px/sn), yuvarlak uç, isteğe bağlı yumuşak kenar. Shader `UI/FxLine`: uzunluk boyunca `u` (px) → görünür ise `fmod(u − t·hız, periyot) < çizgi` (akış yolun başından sonuna). Bir kez kurulur; akış yalnız materyal zamanıyla |
| `FxLedBar` | 7 px LED çubuk: zemin (4 px dolu + 2 px boş, cy α.14), dolgu (aynı desen, renkli + ışıma), tehlike bölgesi (rd α.55 desen), belirsizlik (mor kesik kutu, ±2 px taşar), işaretçi 2×15 beyaz + ışıma (üst −4); dolgu üstünde %40 genişlikte parıltı, 2,8 sn |
| `FxHexIcon` | `fx_hex_40` / `fx_hex_32` + ortada simge (18 / 15 px) + simge ışıması |
| `FxTrustBadge` | 5 tür: ok (15 daire, nane ✓), rap (24×15 kesik mor çerçeve + “≈” Mono 12), sus (16 kesik kehribar daire + “?”, 1,6 sn nabız), unk (gri kesik daire + “?”), fake (kırmızı çerçeve + dolgu α.16 + **TMP “SAHTE / FAKE”**). `fx_tr_fake` sprite'ı yalnız TR ön izleme içindir |
| `FxButton` | Türler: normal (32 h), `pri` (camgöbeği dolu), `dng` (kırmızı), `sm` (26), `lg` (48), `endTurn` (58), `off` (α.45), `confirm` (kehribar, nabız 1,1 sn), `ln` (bağlantı). Üstüne gelince kenar cy2 + ışıma |
| `FxKeyChip` | Tuş fişi 18 (16) px, 1 px cy α.35, Mono 600 11 tx3 |
| `FxCloseButton` | 28×28, kenar `line`, simge `x` 12 |
| `FxPill`, `FxStamp` | Hap (20 px; `ok` nane dolu, `mv` camgöbeği çerçeve) ve damga (24 / 20 px, 1,5 kenar, Chakra 700 13 .18em; kırmızı damgada glitch 4 sn) |
| `FxScale` | Altın → mavi sızma ölçeği: iz (`fx_scale_gradient`), 30/70 sınır çizgileri, bant, işaretçiler (rapor/şüpheli kehribar, saha nane, rapor beyaz + mor ışıma), etiketler, bölge adları. Boylar: kart 52, ipucu ve rehber 30 |
| `FxPips` | Can noktaları 9 px (−12° eğik paralelkenar, `fx_pip_9` / `fx_pip_9_line`); nane (ekip), kırmızı (YZ) |
| `FxTypewriter` | TMP `maxVisibleCharacters`, 48 adım / 2,2 sn, 0,6 sn gecikme + imleç (8×14, 0,9 sn yanıp söner) |
| `FxDrawOn` | Çizgi grafiklerinin çizilerek gelmesi (kıvılcım çizgisi 1,4 sn, dosya grafiği 1,6 sn) |
| `FxTextGlow` | TMP materyal ön ayarı seçici (§4.8) |

**Küre** (§5): `OrbSimulation`, `OrbDrawList`, `OrbRenderer`, `OrbView`, `OrbClock`, `OrbBudget`.

**Masa** (davranış Sade adlarıyla; FX görünümü):

- `FxMapView`: `RegionFxLayer` (12 × `RegionFxGraphic`), `QuarantineOutlineLayer`, `FxSelectionLayer`, `FxHighlightLayer`, `FxSpreadThreadLayer`, `FxTeamRouteView`, `FxPanelLeaderView`, `MapMarkersLayer` (`FxRegionNodeView`, `FxValueChipView`, `FxRegionLabelView`, `FxTeamTokenView`, `FxEtaTagView`, `FxAlertView`), `FxScreenFx`.
- HUD: `FxHudChipView`, `FxGoalChipView`, `FxClockPlaqueView`, `FxDockView` / `FxDockTabView`, `FxLegendButton`, `FxKillSwitchView`.
- Paneller: `FxRegionCardView`, `FxDrawerView` + içerikler (`FxResearchDrawerView` + `FxResearchNodeView`, `FxNewsDrawerView`, `FxTeamsDrawerView`, `FxAiLogDrawerView`, `FxGuideDrawerView` + `FxGuideAiStatesPage`), `FxBreakdownTooltipView`, `FxNodeTooltipView`, `FxLegendCardView`, `FxRegionDossierView`, `FxRegionChipView`.

**Baskın:**

- 3D: `HoloMaterialLibrary`, `HoloEdgeBuilder`, `BoardScanPlane`, `BoardFlicker`, `IntentFlowOverlay`, `NodeOrbAnchor`, `RaidFxBackground`.
- HUD ve kartlar: `FxTurnPlaqueView`, `FxObjectiveCardView` (+ `FxAiStateChip`), `FxRaidTopButtonsView`, `FxTeamChipView`, `FxEndTurnGroupView`, `FxUnitCardView`, `FxHoverInspectorView`, `FxEventSlipView`, `FxEndTurnConfirmView`, `FxRaidLogDrawerView`, `FxRulesCardView`, `FxRetreatConfirmView`, `FxOutcomeView`.
- Dünya etiketleri: `FxHpChip`, `FxOrderFlag`, `FxPopup`, `FxWorldStamp`.

### 4.6 Sprite kiti

**İçe aktarma** (`AssetPostprocessor`, `art/sprites/sprites.json`):

- PNG'ler @2x; **PPU 200** → 1 birim = 1 tasarım px. Mipmap yok, Clamp, sıkıştırma yok ya da yüksek kalite.
- `border1x` = [sol, alt, sağ, üst] tasarım px. `TextureImporter.spriteBorder` doku pikselidir: **×2**.
- `pad1x`: sprite kutunun bu kadar dışına taşar (gölge 34, ışıma 16, parantez 2, halka 2–6). `RectTransform` = kutu + 2·pad; ya da kutu boyunda bırak, Image'ı negatif ofsetle taşır.
- `tint: true` → beyaz çizildi; rengi `Image.color` verir. `tint: false` → renk gömülü (cam geçişleri, rozetler, küçük resimler); `Image.color` beyaz.
- `imageType`: Sliced / Simple / Tiled. `Fill Center` açık.

**Atlas:** `FxUI` (Tight Packing ve Rotation kapalı, padding 4). `thumb_fx_*` ayrı atlas olabilir.

**Simgeler:** FX'te yeniden çizilmedi. Sade paketinin beyaz `ic_*_s` (≤ 16 px) / `ic_*_l` (17–26 px) sprite'ları kullanılır (`Docs/UI_Sade/art/sprites/`); rengi `Image.color`.

- Simge ışıması (`drop-shadow 0 0 3px renk α.6–.8`): arkasına aynı renkte `fx_glow_dot_32`, boy = simge × 1,6, α .35. Birebir değil; kabul edilen sapma.
- Holo renkli dünya simgeleri pakette: `ic_fx_target`, `ic_fx_armor`, `ic_fx_civil`, `ic_fx_civil_x`, `ic_fx_cool`, `ic_fx_copy`, `fx_robot_mini`.
- Yalnız F-YZ'deki OTOMATİK düğmesinin döngü simgesi yok: `ic_undo` ya da iki küçük ok (KARARLAR).

**Küçük maket resimleri (`thumb_fx_*`)** holo tahtanın aynı çiziminden üretildi. İlk sürümde PNG.

### 4.7 Dokular

İçe aktarma: Texture Type Default, Bilinear, mipmap yok. Döşemeler **Repeat**, diğerleri **Clamp**. Bir periyot tasarım pikseli olarak:

| Doku | Periyot / boy | Kullanım |
|---|---|---|
| `tx_fx_land_dot_7` | 7 px | bölge nokta matrisi (beyaz; renk shader'dan) |
| `tx_fx_unk_hatch` | 8,485 px (45° çizgi) | bilinmeyen bölge |
| `tx_fx_quar_hex` | 13,856 × 24 px | karantina altıgeni |
| `tx_fx_led_6` | 6 × 2 px | LED dolgu deseni (4 dolu + 2 boş) |
| `tx_fx_redact_5` | 5 × 2 px | YZ günlüğü karartısı (64×12 kutu, 1,6 sn ileri-geri 40 px kayar) |
| `tx_fx_grid_48` | 48 px | ızgara (baskın zemininde gömülü; ayrıca kullanılabilir) |
| `tx_fx_hazard_28x5` | 28 × 5 px | Son Şalter şeridi (28 px / 1,2 sn kayar) |
| `tx_fx_scanlines_1x3` | **ekran pikseli** 1×3 | tarama çizgileri; ölçeklenmez, `uvRect` ekran boyundan |
| `tx_fx_map_scan_160` | 160 × 8 (dikeyde gerilir) | harita tarama ışığı |
| `tx_fx_board_scan_90` | 8 × 90 (yatayda gerilir) | tahta tarama düzlemi |
| `tx_fx_map_vignette_960` | 960 × 540 → ekran | vinyet (alfa) |
| `tx_fx_raid_bg.jpg` | 1920 × 1080, kaplar | baskın zemini (ızgara gömülü, konisiz) |
| `tx_fx_raid_cone_750x440` | 1500 × 880 tasarım px | projeksiyon konisi; merkez = tahta merkezinin ekran izdüşümü; α .85↔1, 4 sn |
| `tx_fx_yz_bg.jpg` | 1920 × 1080 | FxOrbLab zemini |

- **RawImage döşeme:** `uvRect = (0, 0, alanG / periyot, alanY / periyot)`. Mesh: `uv = konum / periyot`.
- **Harita katmanları:** `fx_map_base@2x.jpg` RGB (BC7/ASTC yüksek kalite ya da RGB24), `fx_map_lines@2x.png` RGBA. Max Size 4096. Bellek bütçesi KARARLAR.md'ye.
- **Koyu geçişlerde bantlaşma** görünürse: geçişi shader'da üret ve 1/255 titreşim (dither) ekle (KARARLAR).

### 4.8 Fontlar ve TMP

- **Kaynak:** `fonts/ttf/` (OFL). Yükleme TMP Font Asset Creator ile:
  - Chakra Petch Regular/Medium/SemiBold/Bold, IBM Plex Mono Regular/Medium/SemiBold, IBM Plex Sans Regular/Medium/SemiBold.
  - SDF, Padding 9, Sampling 72–90, atlas 2048 (Mono 1024 yeter).
  - Karakter seti: ASCII + `ÇĞİÖŞÜçğıöşü` + `ÂâÎîÛû` (**HÂL**) + tipografik `“ ” ‘ ’ · – — − ≈ ± × → ← ↑ ↓ › ‹ …`. Üç ailede hepsi var (doğrulandı). ✓, ⏱ gibi işaretler simge sprite'ıdır.
  - EN için ek glif gerekmez.
- **Ağırlık:** ayrı Font Asset (sahte kalın kullanma). `FontWeight` tablosu TMP'de bağlanabilir.
- **Rakamlar:** IBM Plex Mono zaten eşit genişlikli; sayılar `DeskFormat` ile (TR ondalık virgül, U+2212 eksi).
- **Büyük harf:** dizeler CSV'de büyük yazılı gelir. TMP UpperCase **kullanma** (Türkçe i → İ bozulur).
- **Metin ışıması** (`text-shadow 0 0 Npx renk`): TMP **Underlay** (ofset 0, dilation ~0,1, softness 0,4–0,7). Materyal ön ayarları (en çok 10):

  | Ön ayar | Renk / alfa | Kullanım |
  |---|---|---|
  | `GlowCyan35` | cy α.35 | köşe değerleri, kart ve dosya adları, YZ başlığı |
  | `GlowCyan45` | cy α.45 | saat, büyük değer (α.40) |
  | `GlowAmber50` | am α.5 | gün, tur sayısı, “?” (α.7) |
  | `GlowViolet45` | vi α.45 | rapor değerleri (≈) |
  | `GlowMint60` | mt α.6 | sonuç başlığı, büyük damga (α.8) |
  | `GlowRed60` | rd α.6 | YZ günlüğü yeni satırı |
  | `LabelHalo` | `#02050A` α1 dar + cy α.35 geniş | harita bölge adları (iki katman: Underlay koyu, Glow camgöbeği) |
  | `StateColor` | hâl rengi | F-YZ hâl adları |

### 4.9 Hareket altyapısı

- **`FxClock`:** tek gerçek zaman kaynağı (`Time.unscaledTime`). Döngüler, akış hızları, küreler buradan okur. “Hareketi azalt” açıksa `FxClock.Reduced = true`.
- **Döngüler kod değil veri:** `FxLoop` bileşeni = {özellik (alfa, ölçek, dönüş, uv kayması, konum), dalga (sinüs, doğrusal, adım), periyot, faz, aralık}. Örnek: nabız `alfa .55↔1, 1,6 sn, ease-in-out`. CSS `@keyframes` ↔ `FxLoop` eşlemesi `tokens_fx.motion.loops`.
- **Girişler:** projenin tween kütüphanesi (DOTween / PrimeTween / kendi). Yoksa küçük `FxTween` yaz. Eğri `cubic-bezier(.2,.8,.2,1)` → `AnimationCurve` ön ayarı `FxEaseOut`. Liste `tokens_fx.motion.entrance`.
- **Malzeme zamanı:** akan çizgiler ve kayan dokular `_FxTime` global shader değişkeniyle akar (`Shader.SetGlobalFloat`), her bileşen ayrı `Update` çalıştırmaz.
- **Referans karelerdeki donmuş evreler:** `build_ref` tüm döngüleri belirli evrede durdurdu (nabız %50'de, dönüşler 0'da, parıltı %45'te). Karşılaştırma için `FxClock.Freeze(phaseMap)` uygula; evre haritası `tokens_fx.json › motion` ile aynı adlar (§7).

### 4.10 Performans

Sade §4.8 geçerli. FX ekleri:

- **Masa:** ≤ 40 draw call (harita + ekran katmanları ~10, küreler 12 RawImage, HUD ~15). Akış ve nabız döngüleri yalnız materyal / `CanvasRenderer.SetAlpha` ile; **Canvas yeniden kurma yok**.
- **Küreler:** 12 harita küresi + açık panellerdeki küreler (F-Masa7'de 19). CPU ≤ 2,5 ms/kare (1080p, orta sınıf PC), GC 0 B. Ayrıntı §5.2.
- **Döngü maliyeti:** her nabız / dönüş için `Update` yazma; `FxLoop`'lar tek bir `FxLoopRunner`'da toplu işlenir.
- **Baskın:** Unlit + SRP Batcher; kenar çizgileri tahta başına 1–2 mesh. ≤ 30 UI draw call.
- **Boşta 0 B/kare GC.** Profiler yakalaması F7'de.

---

## 5. YZ küresi

Ayrıntılı algoritma: `spec/orb/ORB_ALGORITHM.md` (kanonik kaynak `art/orb/fx_orb.js`; belge ile kod çelişirse **kod doğru**). Sabitler `spec/orb/orb_constants.json`.

### 5.1 Nerede

| Yer | Yarıçap r | Kutu | Tür / düzey | Not |
|---|---|---|---|---|
| Harita düğümü (12) | `30 + 0,16·v` (+5 çekirdek; bilinmiyor 27) | çizim alanı `round(r × 4,4)` | VM'den | §A.3 |
| Bölge kartı başlığı | 28 | 68 | seçili bölge | `fil` .5 |
| Tam dosya başlığı | 26 | 62 | seçili bölge | `fil` .55 |
| Dosya bağlantı çipi | 11 | 26 | bağlı bölge | |
| Rehber hâl satırları | 23 | 58 | 12 / 50 / 92, normal | `fil` .55 |
| Rehber özel küreler | 13 | 32 | unknown 0, quar 46, core 71 | |
| Rehber GEÇİŞ şeridi | 17 | 40 | sweep | işaretçiyle eş zamanlı |
| Lejant ölçeği | 15 | 36 | 12 / 50 / 92 | %15, %50, %85'te |
| Lejant BÖLGE satırı | 12 | 28 | unknown, quar 46, core 71 | |
| Baskın düğüm küresi | 22 | — | `Board.NodeOrb` | §B.6 |
| Baskın HEDEF HÂL çipi | 9 | 22 | `Hud.Objective.AiNode` | `fil` .5 |
| Baskın sonuç çöküşü | 19 | — | unknown 0 | §B.8 sonuç |
| FxOrbLab hâl küreleri | 118 | alan ×4,2 | 12 / 50 / 96, tohum 11/16/21 | §5.6 |
| FxOrbLab canlı | 74 | alan ×4,0 | sweep | §5.6 |

- “Kutu” panelde ayrılan yerdir. Küre kutudan taşar (ışıma ve lifler); çizim alanı `round(r × 4,4)` kutunun ortasında.
- Kare başına bütün küreler ve tohumları: `spec/fx_frames.json › frames[].orbs`.

### 5.2 Mimari (yol A, önerilen)

- **`OrbSimulation`** (saf C#): kurulum (ORB §5), kare güncellemesi (ORB §4), katman üretimi (ORB §6) → `OrbDrawList`. `fx_orb.js`'in birebir karşılığı. **RNG çağrı sırası aynen korunur.**
- **`OrbDrawList`**: önceden ayrılmış dizi; dörtgen (sprite), çizgi (kalınlık + renk), üçgen, radyal ve doğrusal geçiş kayıtları. GC yok.
- **`OrbRenderer`**: küre başına `RenderTexture` (ARGB32, `sRGB = false`). Boy = çizim alanı × canvas ölçeği × `OrbSupersample` (varsayılan 1,5; tarayıcı motoru 2× çizer; altın testte 2). Üst sınır 512 (FxOrbLab'da 1024). Tek dinamik `Mesh`, `CommandBuffer`: `SetRenderTarget → Clear(0) → DrawMesh`. Shader `Hidden/FxOrbDraw`: `Blend One One` (renk ve alfa), gama uzayında toplama.
- **`OrbView`** (uGUI): `RawImage` + `UI/FxOrbComposite` (önceden çarpılmış alfa: `Blend One OneMinusSrcAlpha`; Linear projede `GammaToLinearSpace`).
- **`OrbClock`**: çizim hızı 40 fps üst sınır. Kare maliyeti 14 ms'yi aşarsa fps 2'şer düşer (en az 16). Hareketi azaltta 8 fps. Ekran dışındaki ya da gizli küre çizilmez (simülasyon zamanı ilerlemez; geri gelince intro yok).
- **`OrbBudget`**: aynı karede en çok N küre güncellenir (varsayılan tümü); bütçe aşılırsa uzaktakiler sıraya girer.
- **Işıma sprite'ı:** tek doku `orb_glow_profile_128.png` (R = beyazlık, A = alfa; doğrusal, sRGB kapalı). Renk vertex renginden.
- **Yol B** (uGUI `MaskableGraphic`, RT yok) ORB §8'de; yalnız yol A'da sorun çıkarsa.

### 5.3 Değer geçişi ve harita senkronu

- `OrbSimulation.Level` her karede hedeftir; görünen düzey `lv += (hedef − lv) · min(1, dt · 3)` (≈ 0,33 sn). OTOMATİK'te `dt · 6`.
- **Bölge boyası (§A.2) kürenin görünen `lv`'sini kullanır.** Böylece küre ve boya aynı anda değişir. `OrbView` her karede `DisplayedLevel` yayınlar; `RegionFxGraphic` ona bağlanır (yalnız değiştiyse materyal günceller).
- Değer çipindeki sayı VM değeridir (anında). İsteğe bağlı sayarak geçiş Sade §A.13 bayrağıyla.
- **İlk görünüş:** küre 1,1 sn'de büyür (`intro`, ORB §4.1). Açılış sırası: düğüm i, 0,25 + 0,05·i sn gecikmeyle başlar.

### 5.4 Belirlenimci mod

- `OrbSimulation.Advance(dt, n)`: sabit adımla n kare ilerlet, çiz. Gerçek saat kullanmaz.
- Altın kareler: `dt = 1/40`, t = 1,0 sn (40 adım) ve 2,5 sn (100 adım), R = 48, çizim alanı 211 px, 2× ölçek, zemin `#02050A`. Durumlar `golden.json`.
- Editor menüsü `Tools/FX UI/Orb Golden Capture` 22 kareyi `Docs/UI_Fx/captures/orb/` altına yazar.

### 5.5 Doğrulama

1. `python3 tools/compare.py captures/orb/orb_normal_L060_t2.5.png reference/orb/golden/orb_normal_L060_t2.5.png`: genel fark ≤ 6, kenar örtüşmesi ≥ %70 (22 karenin hepsinde).
2. Kıvılcım ve liflerin rastgele dizisi port doğruluğuna bağlıdır. Kayan nokta farkları küçük kaymalar yapar; şekil ve renk aynı olmalı.
3. Hâl geçişi: `tools/orb_lab.html` ile FxOrbLab yan yana (değer 0 → 100 sürüklenirken). `reference/motion/kure_canli_gecis.mp4` ve `kure_uc_hal_yakin.mp4` ile karşılaştır.
4. EditMode testleri: RNG ilk 10 değer (tohum 3, 5, 7, 9), ikosküre 42/80/120 ve kenar sırası, `Sweep(t)` tablosu, ağırlıklar (`wA`, `wB`, `wC`) s = 0, .3, .5, .7, 1'de.

### 5.6 FxOrbLab sahnesi (F-YZ)

Referans `F-YZ.jpg`, `reference/zoom/F-YZ_canli_gecis@2x.jpg`, video `kure_canli_gecis.mp4`.

- **Zemin:** `tx_fx_yz_bg.jpg` + tarama çizgileri.
- **Başlık** (72, 60, 900 geniş): göz başlığı “YZ ÇEKİRDEĞİ · BÖLGE KÜRESİ” (Chakra 600 11 .16em cy) · başlık Chakra 700 40 .02em `GlowCyan35` · gövde Sans 16 satır 1,45 tx2, en çok 760 px. Giriş: x −24 → 0, 0,8 sn.
- **Üç hâl küresi:** merkezler (330, 392), (960, 392), (1590, 392); r 118; alan ×4,2; düzey 12 / 50 / 96; tohum 11 / 16 / 21.
- **Hâl kartları** (500 geniş, sol = merkez − 250, üst 600): cam panel, parantez rengi hâl rengi; başlık satırı “HÂL 1” (Mono 600 13 hâl rengi) + ad (Chakra 700 26 .08em, hâl renginde ışıma) + sağda “sızma 0–30” (Mono 13 tx2); üç madde (6 px döndürülmüş kare + Sans 15). Giriş y +16, 0,15·i gecikme.
- **Canlı geçiş şeridi** (80, 812, 1760×220):
  - Solda küre (r 74, alan ×4,0, tohum 41).
  - Sağda başlık satırı: “CANLI GEÇİŞ” + sayı (“sızma 26”, Mono 600 30) + ipucu “ölçeğe tıkla ya da sürükle” + **OTOMATİK** düğmesi (32 px, 7 px pahlı; açıkken camgöbeği dolu).
  - Ölçek: 10 px altın → mavi iz (`fx_scale_gradient`), 30 ve 70'te 2×26 çizgi, altında üç hâl adı (Chakra 600 12 .14em), en altta 0 / 30 / 70 / 100 (Mono 12 tx3).
  - İşaretçi: 4×38 beyaz + ışıma + üstte 16 px üçgen; konum değişince 0,25 sn kayar.
- **Davranış:**
  - OTOMATİK açık (varsayılan): küre ve işaretçi 18 sn'lik döngüde gezer. Tablo: 0–2 sn 0; 2–8 sn 0 → 100 (smoothstep); 8–10 sn 100; 10–16 sn 100 → 0; 16–18 sn 0. Sayı aynı saatten.
  - Ölçeğe tıklama ya da sürükleme: OTOMATİK kapanır; düzey tıklanan değere atlar, küre yumuşak geçer (≈ 0,33 sn). Klavye: ← / → 1, Shift ile 10.
  - OTOMATİK'e basmak döngüyü kaldığı evreden değil, saatten sürdürür.
- **Geliştirici paneli** (yalnız Editor / Development build): tür (normal / unknown / quar / core), yarıçap, tohum, fil, fps, “Altın kare” düğmesi.

---

# A · F-Masa (fütüristik analist masası)

Referanslar: `F-Masa1.jpg` … `F-Masa9.jpg`, `reference/zoom/F-Masa*`, `reference/measure/F-Masa*.tsv`, videolar `masa_*.mp4`. Davranış Sade §A ile aynıdır; aşağıda yalnız **FX farkları** ve görünüm var.

## A.1 Katman sırası (alttan üste)

1. `fx_map_base@2x.jpg` (deniz geçişi, 14 px nokta ızgarası, enlem-boylam, dönenceler, ekvator, kıyı parıltısı, kara zemini `#061220`).
2. **Bölge boyası:** 12 × `RegionFxGraphic` (§A.2). Sırası: parıltı (ya da tarama) → nokta matrisi → karantina altıgeni.
3. `fx_map_lines@2x.png`: ülke sınırları α.07, bölge sınırları kesik 4/4 α.38, kıyı çizgisi `#6FE6FF` α.66 0,9 px.
4. **Karantina dış hattı:** `#3DFFB4` α.7, 1,5 px, kesik 8/5 (durağan).
5. **Seçim** (dolgu + parlayan kıyı) ve **vurgu** (kehribar akan hat).
6. **Yayılım iplikleri**, **ekip rotası**, **panel bağ çizgisi** (F-Masa2).
7. `ScreenFx` canvas'ında: tarama çizgileri → tarama ışığı → vinyet (§A.5).
8. `MapMarkers`: küreler, nişangâh, üstüne gelme halkası, değer çipleri, bölge adları, ekip jetonları, ETA etiketi, danışman uyarısı.

**Açılış:** harita 1,2 sn'de belirir; küreler sırayla büyür; değer çipleri y +16 → 0 (0,6 sn, 0,5 + 0,05·i gecikme); bölge adları 0,8 sn'de belirir; jetonlar ölçek .94 → 1 (0,6 sn). Video `masa_acilis.mp4`.

## A.2 Bölge boyası (`RegionFxGraphic`)

Geometri Sade `map_regions.json`'dan (halkalar + earcut üçgenleri). Formüller `spec/map_fx.json › regionShader`.

- **Renk:** `aiColor(lv)` = altın (255, 176, 64) → mavi (58, 140, 255), `t = ss(0,24; 0,80; lv/100)`. Küre motoruyla **aynı eğri**. `lv` = kürenin görünen düzeyi (§5.3).
- **Nokta matrisi:** `tx_fx_land_dot_7` (7 px periyot, nokta r 1,05), renk `aiColor`, alfa `.26 + .30·s`.
- **Etki parıltısı:** düğüm merkezli radyal geçiş, yarıçap `glowRadius` (bölgeye göre: KA 400, OA 160, GA 320, BA 260, DA 170, ODKA 250, SAA 300, AV 470, GuA 170, DoA 240, GDA 230, OK 260). Duraklar: 0 → `a0`; .45 → `a0 × .38`; 1 → 0; `a0 = .20 + .40·s`. Bölge çokgeniyle kırpılır.
- **Bilinmeyen bölge:** parıltı yok. Nokta `#7FA3B3` r .9 α.30 + `tx_fx_unk_hatch` (`#7FA3B3` α.16).
- **Karantina:** üstte `tx_fx_quar_hex` (`#3DFFB4` α.22); dış hat §A.1.4; küre `quar` türü.
- **Shader `UI/FxRegion`** (tek geçiş): UV0 = tasarım px konumu. Parça: `glow` (parçalı doğrusal) altına, nokta üstüne. Parametreler (renk, alfalar, düğüm, R) iki yoldan biriyle: bölge başına materyal örneği (12 draw call) ya da tek materyal + vertex rengi ve UV1–UV2 kanalları (1 draw call; önerilen). Düzey değişince **yalnız parametreler** güncellenir; üçgenler değişmez.
- **Tıklama alanı:** Sade gibi (nokta-çokgen). Bölgeye tıklamak düğüme tıklamakla aynı.
- **Referans:** `art/map/_fx_map_full_ref.jpg` (mock değerlerle tüm katmanlar). Renkler `map_fx.json › regions[].mockColor`.

## A.3 Bölge düğümü (`FxRegionNodeView`)

Konumlar ve boylar `spec/map_fx.json › regions[]` ve `node`.

- **Küre:** merkez = `regions[].node`; r `30 + 0,16·v` (+5 çekirdek; bilinmiyor 27); çizim alanı `round(r × 4,4)`. Tür §4.2.
- **Tıklama alanı:** daire, yarıçap `max(r, 26)`. Klavye odağı: nişangâhın ince hâli.
- **Değer çipi** (`FxValueChipView`): üst-orta çapa `(x, y + 0,95r + 4)`, 22 px yükseklik, iç boşluk 0/7/0/8, aralık 5; zemin `#030A12` α.78, 1 px cy α.32, gölge 10 px α.6. İçerik: değer Mono 600 14 tx (`v` · raporsa `≈v` · bilinmiyorsa `?`) + güven rozeti.
  - **Sıcak** (`v ≥ 60`): kenar `#3D8BFF` α.7, metin `#D7ECFF`, ışıma 12 px ai α.35.
  - **Bilinmiyor:** kesik kenar, metin tx3.
- **Bölge adı** (`FxRegionLabelView`): üst-orta `(x, değer çipi üstü + 28)`; Chakra 600 11 .16em `#CFEFFA`, `LabelHalo` ön ayarı.
  - **Seçili:** zemin cy, metin `#02050A`, iç boşluk 3/6, ışıma 12 px α.6.
  - **İstisna DOĞU AVRUPA:** değer çipi `(x, y − 0,95r − 26)`, ad `(x, y − 0,95r − 46)` (kürenin üstünde; Batı Avrupa ile çakışmasın).
- **Seçili düğüm:** `fx_reticle`, boy `r × 3,2`, 12 sn'de döner, tint cy + ışıma. Bölge kıyısı parlar (§A.4).
- **Üstüne gelme:** `fx_hover_ring`, boy `r × 2,5`, nabız 1,6 sn; küre %25 parlaklaşır (composite'te `_Brightness 1,25`). 400 ms sonra düğüm ipucu açılır, **ad gizlenir** (F-Masa8).
- **Durum renkleri metinde değil kürede:** hâl, küreden ve değer çipinden okunur. Düğüm üstünde ek rozet yok (Sade'deki mühür, Ç damgası, turuncu halka FX'te küre türü ve değer çipindeki rozettir).

## A.4 Harita bindirmesi

Geometri `spec/map_fx.json › overlay` (doğrulama için sayılar; çalışma anında düğüm konumlarından hesapla).

- **Seçim** (`FxSelectionLayer`): dolgu cy α.07 + kıyı 2,4 px cy, ışıma 6 px α.9; kıyı alfası .55 ↔ 1, 2,4 sn.
- **Vurgu** (`FxHighlightLayer`; F-Masa4 haber, günlük satırı): 2 px am, kesik 6/5, 25 px/sn akar, ışıma 5 px. Seçimden farkı: ad çipe dönmez, nişangâh yok.
- **Yayılım iplikleri** (VM `spreadThreads`):
  - Uçlar düğüm merkezlerinden `r + 8` içeride; kontrol noktası orta nokta + (0, 10).
  - Üç vuruş: taban `#3D8BFF` α.35 6 px + 3 px bulanıklık (yumuşak kenarlı `FxLineGraphic`) · çizgi `#8FD3FF` 1,6 px kesik 10/8, 37,5 px/sn · parçacık beyaz 2,4 px kesik 2/58, yuvarlak uç, 50 px/sn.
  - Akış yönü kaynaktan hedefe. **Ok ucu yok.**
- **Ekip rotası:** nane 1,6 px, kesik 3/7, yuvarlak uç, 17,1 px/sn; uçta açık ok (M−8 −5 L0 0 L−8 5, 1,8 px), teğete dönük. Uçlar: jetondan 18 px, hedef düğümden `r + 10`; kontrol orta nokta + (0, −18).
- **Ekip jetonu** (`FxTeamTokenView`, 34 px): ortada 18 px elmas (`fx_tok_diamond`; dolgu `#030E14` α.9, 1,5 nane kenar, ışıma) + harf Chakra 700 12 nane + çevrede kesik halka (`fx_ring_dash_34`, nane α.55, 6 sn'de döner). Vurgu (F-Masa5): halka kehribar 1,5 px düz, 6 px dışarıda, ışıma 12 px α.6.
- **ETA etiketi:** 20 px, iç boşluk 0/6, zemin `#030E14` α.82, nane α.45 kenar, `ic_clock_s` 12 + Mono 500 12 nane; sol-üstü jeton merkezine göre (−26, +22).
- **Danışman uyarısı** (`FxAlertView`): 30×28 kehribar üçgen (`fx_alert_tri`) + “!” Chakra 700 15 `#1A1000`; sol-üstü düğüm merkezine göre (−64, −62). Dalga: `fx_ring_44`, üçgenin %60 yüksekliğinde, ölçek .6 → 1,9, α .9 → 0, 1,8 sn. Davranış Sade §A.5.
- **Panel bağ çizgisi** (`FxPanelLeaderView`, F-Masa2): kürenin kenarından `(x ± 1,15r, y − 4)` kartın yakın kenarına; taban cy α.25 5 px yumuşak + çizgi cy2 1,4 px kesik 6/4, 22 px/sn; iki uçta r 3,5 nokta cy2. Sade'deki kart ipliğinin yerine.

## A.5 Ekran katmanları (`FxScreenFx`)

- **Tarama çizgileri:** `tx_fx_scanlines_1x3` ekran pikselinde döşenir (ölçeklenmez), α.025, eklemeli.
- **Tarama ışığı:** `tx_fx_map_scan_160` (160 px geniş, ekran yüksekliğinde), x −200 → 2100, 12 sn doğrusal, eklemeli. Referans karelerde gizli; oyunda açık.
- **Vinyet:** `tx_fx_map_vignette_960` ekrana gerilir; haritanın üstünde, HUD'un altında.
- 21:9 ve 16:10'da üçü de **ekrana** gerilir (haritaya değil).

## A.6 Köşe etiketleri (`FxHudChipView`, `FxGoalChipView`)

Kutular `F-Masa1.tsv` (`chip-*`); genişlikler TR metinle ölçüldü. EN'de içerik büyürse kutu sağa doğru genişler, sonraki kutu kayar (aralık 8).

| Öğe | Kutu | İçerik |
|---|---|---|
| FON | 16, 14, 182, 66 | `fund` · “FON” + ✓ · “47,3” + ↓1,2 (kırmızı) |
| SİYASİ GÜÇ | 206, 14, 178, 66 | `power` · “SİYASİ GÜÇ” + ✓ · “62” + ↑0,4 (nane) |
| HEDEF | 392, 14, 290, 66 | `goal` · “HEDEF” · iki satır: Hizalama (nane LED) %42, Silme (camgöbeği LED) %28 |
| KÜRESEL GÜVEN | 1247, 14, 215, 66 | `globe` · ≈ rozeti · “≈41,8” mor + ↓1,0 kırmızı · 132 px LED: tehlike 0–15, işaretçi 41,8 |
| BAĞIMLILIK | 1460, 14, 214, 66 | `chain` · ? rozeti (nabız) · “58,4” + ↑0,7 kırmızı · 132 px LED: tehlike 70–100, işaretçi 58,4 |
| TEKİLLİK | 1672, 14, 232, 66 | `chip` (**kırmızı altıgen**) · ≈ rozeti · “≈%37” mor + “±6” küçük + ↑1,3 kırmızı · 150 px LED: dolgu %37 ai mavi, belirsizlik 31–43, tehlike 94–100 |

**Anatomi** (`tokens_fx.desk.chips`):

- Cam panel 66 px, iç boşluk 0/16/0/12, aralık 12; parantezler ve süpürme.
- Altıgen 40 (`fx_hex_40`, tint cy; TEKİLLİK rd) + simge 18 + ışıma.
- Metin bloğu dikeyde ortalı, satır aralığı 6:
  1. etiket satırı (15 px): cap (Chakra 600 11 .16em tx2, büyük harf) + güven rozeti (aralık 6);
  2. değer satırı: Mono 600 24 tx `GlowCyan35` (rapor değeri vi `GlowViolet45`) + değişim (Mono 600 12, ok simgesi 11; kötü rd, iyi mt; aralık 8);
  3. (riskte) `FxLedBar` 7 px.
- HEDEF satırları: 13 px, aralık 6; ad Sans 500 12 tx2 62 px; LED 96 px; değer Mono 600 13 34 px sağa yaslı, LED renginde.
- **Üstüne gelme** (döküm fişi için, F-Masa3): kenar cy + dış halka 1 px cy α.5 + ışıma 22 px.
- Giriş: y −14 → 0, 0,7 sn; soldan sağa 0,06 sn arayla.

## A.7 Üst orta: zaman (`FxClockPlaqueView`)

- Cam panel, ortaya çapalı, üst 14, 66 px; iç boşluk 0/14, aralık 14 (TSV `time`: 778, 14, 364×66).
- Gün: “GÜN” cap + “074” Mono 600 20 am `GlowAmber50`, aralık 4.
- 1×38 dikey ayraç (`fx_divider_fade` dikey; line2 → saydam).
- Saat “14:32”: Mono 500 26 .06em tx `GlowCyan45`; **iki nokta 1 sn'de yanıp söner** (adım).
- Hız tuşları (⏸, 1×, 2×, 4×): 34×30, aralık 6, 6 px pahlı (`fx_chamfer_6` + `_line`), kenar `line`, zemin cy α.05, Mono 600 12 tx2. Seçili: zemin cy, yazı `#02050A`, ışıma 12 px α.7.
- Giriş: y −14 → 0, 0,7 sn, 0,1 sn gecikme.

## A.8 Alt: sekmeler, LEJANT, Son Şalter

**Sekmeler** (`FxDockView`; TSV `dock-*`)

- Sol 16, alt 12, aralık 8, alta yaslı.
- Sekme 44 px (açık 52, iç alt boşluk 8), iç boşluk 0/14/0/12, aralık 9. Zemin `fx_glass_btn`, kenar `line`.
- Üstte 2 px vurgu çizgisi (sekme rengi α.75 + ışıma 8; açıkken 3 px α1).
- Simge 17 sekme renginde + ışıma 3 · etiket Chakra 600 12 .14em tx2 (açık tx) · tuş fişi 18.
- Açık sekme: kenar = sekme rengi, zemin daha açık cam, dış ışıma 18 px.
- Renkler: ARAŞTIRMA `#FFB547`, HABERLER `#5BE4FF`, EKİPLER `#3DFFB4`, YZ GÜNLÜĞÜ `#FF4D63`, REHBER `#A98EFF` (`tokens_fx.colors.dockAccent`).
- Rozetler: sayı hapı 18 px (am, `#1A1000` Mono 700 11, ışıma, 1,4 sn nabız; sağ-üst −9/−8) · nokta 12 px rd + ışıma (sağ-üst −6/−5).
- Giriş: y +16 → 0, 0,7 sn, 0,2 sn gecikme.

**LEJANT düğmesi:** sağ 300, alt 22, 40 px; `fx_glass_btn`, `legend` simgesi 16 cy + Chakra 600 12 .14em. Açık: yazı tx, kenar cy, ışıma 14 px.

**Küresel Son Şalter** (`FxKillSwitchView`): sağ 16, alt 14, 268×66.

- `fx_glass_kill` (kırmızı cam: kenar rd α.7, dış ışıma 22 px, iç ışıma).
- Üstte 5 px tehlike şeridi (`tx_fx_hazard_28x5`, 28 px / 1,2 sn kayar, α.9).
- “KÜRESEL SON ŞALTER” Chakra 700 13 .12em `#FFD7DC` · “3 sn basılı tut” Sans 11 rd2.
- Sağda 44 px düğme (`fx_kill_btn`; simge `kill` 20 `#FFE3E7`) + 6 px dışarıda kesik halka (rd α.75, 8 sn'de döner).
- Basılı tutma: Sade/C davranışı. FX görünümü **çizilmedi**; öneri: halka 3 sn'de dolar (`Image.fillAmount`, rd, ışıma), şerit hızlanır (×3), panel ışıması .9 → 1 (KARARLAR).

## A.9 Bölge kartı (F-Masa2)

`FxRegionCardView`. TSV `card-*`; kutu (1532, 96, 372×510). Video `masa_bolge_karti.mp4`.

| Bölüm | Kutu (y, yükseklik) | İçerik |
|---|---|---|
| Başlık | 97, 89 | küre (r 28, kutu 68, `fil` .5) · “DOĞU ASYA” Chakra 700 22 .1em `GlowCyan35` · altında “R-10 · 1,64 mlr” Mono 12 tx3 · sağ üstte kapat. Zemin: hâl renginde %10 → saydam (soldan, %60'ta biter). Altında 1 px `line` |
| Durum | 198, 28 | kesik kehribar kutu (α.55, zemin α.07), iç boşluk 0/10: ? rozeti + “Kaynaklar çelişiyor” Sans 600 13 am2 · sağda “14 puan fark” Mono 500 12 am |
| Sızma | 238, 57 | solda “SIZMA” + “%38” Mono 600 40 `GlowCyan45` + “?” Chakra 700 24 am (1,6 sn nabız). Sağda `FxScale` (52 px): bant 38–52, rapor 38 kehribar (etiket solda), saha 52 nane (etiket sağda) |
| YZ HÂLİ | 307, 26 | iç boşluk 0/10; sol 2 px hâl rengi kenar; zemin hâl rengi α.14 → mor α.10; “YZ HÂLİ” cap + “HÂL 2 · KARARSIZ” Chakra 700 13 .12em `#FFD2B0` · sağda “iki kaynakta da” Sans 12 tx3 |
| İstatistikler | 345, 33 | 3 sütun, aralık 8: `FxHexIcon` 32 + değer Mono 600 18 (+ rozet, aralık 5) + ad Sans 11 tx3 (aralık 4). Rapor değeri mor |
| Eylemler | 390, 66 | 4 sütun, aralık 6; kutu 66: kenar `line`, zemin cy .06 → .02; simge 20 cy + ışıma; ad Sans 600 12 tx2; tuş fişi 16 (4, 4). Üstüne gelme: kenar cy, yazı tx, ışıma 16 px + iç ışıma. Son Şalter: kırmızı kenar, yazı rd2, kırmızı zemin |
| Önizleme | 468, 32 | kenar cy α.3, zemin cy α.06, iç boşluk 0/10: `clock` + “5 gün · 8 fon” (Mono 600 tx) + `arrow` + “kaynak” + `FxPill ok` “✓ ONAYLANIR” |
| Ekip | 512, 39 | durağan jeton B + “Ekip B” Chakra 700 14 .06em / “Seul · hazır” Sans 12 tx3 + sağda `pri` düğme 36 px “BASKIN %64 [B]” |
| Daha fazla | 565, 40 | üstte 1 px `line`; “Kaynaklar · nüfus · bağlantılar” Sans 12 tx3 · sağda “Tüm dosya ›” Chakra 600 13 .08em cy |

- **Kap:** `FxPanel` (`fx_glass_solid`), iç boşluk 12/16/14, bölüm aralığı 12.
- **Giriş:** x +26 → 0 + opaklık, 0,55 sn. Kart başka bölgeye geçerken kayar (öneri 0,3 sn; çizilmedi), bağ çizgisi yeniden kurulur.
- **Yerleşim** (Sade §A.11 kuralının FX hâli):
  1. Tercih: düğümün sağı, `x = node.x + 1,15r + 17` (F-Masa2: 1473 + 41,5 + 17 ≈ 1532).
  2. Sağ kenar 1904'ü aşarsa **sağ kenara yasla** `x = 1904 − w`; yeter ki kürenin kutusunu (`node.x + 1,15r`) örtmesin.
  3. O da olmazsa sol: `x = node.x − 1,15r − 17 − w`.
  4. `y = clamp(node.y − 314, 96, 1080 − 96 − h)`. Kart düğümü, adını ve açık çekmeceyi örtmez (Sade kuralı).
  5. Bağ çizgisi düğüm yüksekliğinde (`y − 4`) yataydır. Bu yükseklik kartın dikey aralığı dışındaysa çizgi kartın en yakın köşesine 24 px içeriden bağlanır.
- **Hâl rengi bağlamı:** başlık zemini, YZ HÂLİ kenarı ve metni seçili bölgenin hâline göre (1 / 2 / 3). Bilinmiyorsa satır “YZ HÂLİ · BİLİNMİYOR”, renk tx3.

## A.10 Çekmeceler (F-Masa3–7)

**Ortak kap** (`FxDrawerView`):

- Sol 16, **alt kenar 1004**, genişlik 500 (Rehber 560). Yükseklik içeriğe göre; çizilen değerler: Araştırma 652, Haberler 706, Ekipler 494, YZ günlüğü 540, Rehber 576. Üst kenar en az 92; içerik sığmazsa gövde kayar.
- `fx_glass_solid`; parantezler **sekme renginde** + ışıma. YZ günlüğü: `fx_glass_red` (kırmızı cam, kenar rd α.32).
- **Başlık** 48 px, iç boşluk 0/10/0/14, aralık 10: zemin sekme rengi %14 → saydam (%70'te biter); altında 1 px `line` + soldan 120 px sekme rengi çizgi (ışıma 8). İçerik: simge 18 (sekme renginde + ışıma) + başlık Chakra 700 15 .18em + tuş fişi + sağda kapat.
- **Gövde:** iç boşluk 14/16/16/16, aralık 10.
- **Giriş:** y +22 → 0 + ölçek Y .96 → 1 (alt kenardan), 0,55 sn. Satırlar x −10 → 0, 0,5 sn, 0,08 sn arayla. Kapanış çizilmedi; varsayılan 0,25 sn (opaklık + y +12).
- Video `masa_cekmece_ve_dokum.mp4`.

**Araştırma** (F-Masa3; TSV `rs-*`)

- **Özet** (27 px): “**2 proje** sürüyor” Sans 14 tx2 · sağda “−6,4” Mono 600 20 rd + “fon/gün” 12 tx3.
- **Hız seçici** (40 px, 1 px `line`): solda `clock` + “HIZ” (kehribar) bölmesi; üç seçenek eşit (Mono 600 14 + Sans 11 tx3). Seçili: kehribar zemin α.22 → .08, yazı am2, altta 2 px am iç çizgi.
- **Dal ağacı** (4 satır × 94 px, satır altı 1 px cy α.1):
  - Sol sütun 132 px: dal adı Chakra 700 13 .14em · “1/3 bitti” Mono 11 tx3 · hedef dalıysa `goal` 12 + 40 px LED + “%42” (dalın HEDEF rengi).
  - Düğümler x = **150, 268, 386** (satır içi), 52 px altıgen:

    | Durum | Görünüm |
    |---|---|
    | bitti | `fx_rn_hex_fill` nane α.16 + `fx_rn_hex_line` nane 1,6 + ✓ (3 px, nane) + ışıma |
    | sürüyor | dolgu cy α.08 + kenar cy · `fx_ring_r15_w3_5` iz α.18 + dolum (Filled Radial360, üstten, saat yönü) cy + ışıma · ortada “62” Mono 600 12 |
    | açık | `fx_rn_hex_dash` cy α.8 + “+” cy2 |
    | kilitli | gri dolgu α.06 + gri kenar α.6 + kilit simgesi; düğüm α.75 |

  - Ad: altıgenin altında (üst 54), 112 px, Sans 600 11 satır 1,15, en çok 2 satır (kilitli tx3). Alt satır (üst 81): simge 10 + Mono 11 tx3 (“bitti”, “4 gün”, “kilitli”, “SG 70”).
  - Bağlar (y = 26): bitti/sürüyor dalı cy 1,6 kesik 4/4 akar (1,2 sn) + ışıma; açığa giden cy α.55 kesik 3/4; kilitliye giden gri α.45 kesik 2/5. Ok ucu yok.
  - **Seçili düğüm:** 7 px dışarıda kehribar kesik halka (`fx_ring_dash_66`, 1,5 px), 7 sn'de döner, ışıma.
- **Seçili düğüm şeridi** (101 px, altta): kehribar kenar α.55, zemin am .10 → .03, ışıma 18 px. Ad Sans 600 15 · sağda “HİZALAMA · 2/3” Chakra 600 11 .14em am · 6 px ilerleme (180 px, cy + ışıma, uçta 40 px parıltı nabzı 1,2 sn) + “%62” + “4 gün kaldı” (cy) · `fund` + “3,2 fon/gün” · `arrow` + `lock` + açılacak düğüm · sağda düğme “Duraklat” (28 px).

**FON döküm fişi** (`FxBreakdownTooltipView`; F-Masa3; 16, 90, 300 geniş)

- Kap: `FxPanel`, kenar cy α.45, gölge 30 px + ışıma 22 px; iç boşluk 12/14, aralık 10. İşaretçi 12 px kare 45°, üstten −7, soldan 34 px.
- Başlık: “FON” cap cy + “günlük denge” 12 tx3 · sağda “−1,2” Mono 600 18 rd + “/gün”.
- Denge çubukları (8 px, zemin cy α.08): Gelir nane + ışıma (+6,1) · Gider: araştırma rd + ışıma, ekipler rd2 (−7,3). Ölçek: gider toplamı = %100. Anahtar satırı (7 px kare + 11 tx3).
- Net satırı (üstte 1 px `line`): “Net −1,2/gün” (Mono 600 18 rd) · sağda `clock` + “0'a ≈39 gün” am.
- 30 gün çizgisi (272×36): 1,8 px cy + ışıma, altı cy α.28 → 0 geçiş; olay günü nane kesik dikey; uçta r 3,6 kırmızı nokta (ışıma, 1,2 sn nabız). Çizgi **1,4 sn'de çizilerek gelir** (0,3 sn gecikme). Altında Mono 11: “G-044 · 58,0” · “ödenek +2,2” (nane) · “bugün · 47,3”.
- Ne zaman / yer: Sade §A.14.4. Giriş y −14 → 0, 0,35 sn.

**Haberler** (F-Masa4)

- Süzgeç (28 px hap düğmeler: kenar `line`; seçili kenar cy + zemin cy α.12): “Tümü 5”, “● Doğrulanmamış 3” (7 px mor nokta + ışıma) · sağda “G-074 · 14:32” Mono 12 tx3.
- Gün ayracı: Chakra 600 11 .18em cy + sağa uzanan line2 → saydam çizgi.
- Haber satırı (`FxNewsItemView`): kenar cy α.14, zemin cy α.03, solda 2 px cy α.25 şerit; iç boşluk 10/12/10/14, aralık 7.
  - 1. satır: güven rozeti + kaynak · saat (Mono 11 .04em tx3) + sağda `FxRegionChipView`.
  - 2. satır: başlık Sans 600 15 satır 1,3 tx.
  - 3. satır (26 px): güven sözcüğü (renkli kalın) + açıklama 12 tx3 · doğrulanmamışsa sağda `sm` “Denetle” düğmesi.
  - **SAHTE:** başlık tx3 + kırmızı üstü çizili (α.7); sağ altta `FxStamp` kırmızı “SAHTE”, −4°, glitch.
  - **Üstüne gelme:** kenar am α.6, zemin am α.06, ışıma 18 px; sol şerit am + ışıma; **haritada bölge vurgusu** (§A.4).
- **Bölge çipi** (`FxRegionChipView`): 20 px, iç boşluk 0/7/0/6, kenar cy α.22, zemin cy α.05, Chakra 600 11 .08em tx2; solda 8 px daire = bölgenin `aiColor` + ışıma (bilinmiyorsa kesik gri halka). Kısa adlar Sade `region.short.*`.

**Ekipler** (F-Masa5)

- Özet: “**3** ekip · **2** hazır · **1** yolda” (Mono 600 16 sayılar) · sağda “yeni ekip: **30** fon” (am).
- Ekip kartı: kenar cy α.16, zemin cy α.03, iç boşluk 10/12/12, aralık 10.
  - Başlık: durağan jeton + “EKİP A” Chakra 700 15 .12em + `scope` + konum 12 tx3 + sağda hap (HAZIR nane dolu; YOLDA camgöbeği çerçeve).
  - Gövde: üç üye (36×40 altıgen `fx_av_hex_fill` + `_line` nane + ışıma; harf Chakra 700 15 nane; hasarlıysa kehribar) + altında 7 px can noktaları · 1 px ayraç · sağda eylem.
  - Eylem: “Görevlendir ›” + not · `pri` “BASKIN %64 [B]” + hedef · yoldaysa **rota çubuğu**: 2 px nane kesik iz, kat edilen kısım dolu nane + ışıma, uçlarda 10 px döndürülmüş kare (1,5 nane), jeton %58'de; altında “CHICAGO · ⏱ 2 gün · PARİS” (Mono 11).
  - Üstüne gelme: kenar am α.6 + ışıma; **haritada jeton vurgusu** (§A.4).

**YZ günlüğü** (F-Masa6; video `masa_yz_gunlugu.mp4`)

- Kırmızı cam kap. Başlık şeridi: kırmızı lamba (9 px, ışıma, 1,2 sn yanıp söner) + “AYNA · ELE GEÇİRİLEN KAYIT” Chakra 700 12 .16em rd2 · sağda “SEUL-3 hattı” Mono 11 tx3.
- Kayıt: solda 2 px rd α.3, zemin rd α.04, iç boşluk 9/12, aralık 6.
  - Üst satır: zaman Mono 11 tx3 + YENİ hapı (16 px, rd zemin, `#1A0306` Chakra 700 11 .12em, 1,2 sn nabız) + sağda bölge çipi (ya da kırmızı “ÇEKİRDEK”, “HABER · SAHTE” çipi).
  - Satır: Mono 500 13 .02em `#FFC2CA`, “> ” ile başlar. Karartı: 64×12 kutu `tx_fx_redact_5` (kayar).
  - **Yeni kayıt:** sol kenar rd, zemin rd α.10, iç ışıma; satır `#FFE3E7` + `GlowRed60`; **daktilo** (48 adım / 2,2 sn, 0,6 sn gecikme) + imleç 8×14 rd2 (0,9 sn).
  - Analist notu: “ANALİST” etiketi (16 px, am çerçeve, Chakra 700 11) + Sans 600 13 am2.
- Uyarı kutusu (altta): kesik am α.55, zemin am α.06; ? rozeti + “**AYNA aldatma profilinde.** Bu satırlar da yalan olabilir; sahada doğrula.” (12 tx2, başlık am2).

**Rehber** (F-Masa7; **YZ hâlleri sayfası yeni**)

- Izgara: sol konu listesi 148 px (sağında 1 px `line`) + sayfa.
- Konu satırı 34 px: simge 15 tx3 + Sans 500 13 tx2. Seçili: sol 2 px vi, zemin vi α.18 → saydam, yazı tx 600, simge vi.
- Konular: **YZ hâlleri** (yeni, `chip` simgesi), Güven dili, Renkler, Eylemler, Saha ekipleri, Son Şalter, Kısayollar (7). Altta “‹ önceki · 1 / 7 · sonraki ›”.
- **YZ hâlleri sayfası** (`FxGuideAiStatesPage`):
  - Başlık “YZ HÂLLERİ” Chakra 700 18 .16em + alt yazı Sans 13 satır 1,4 tx2.
  - Üç satır (kenar cy α.12, sol 2 px hâl rengi, zemin hâl rengi α.10 → saydam; iç boşluk 8/10/8/6, aralık 12): küre (r 23, kutu 58, düzey 12 / 50 / 92, `fil` .55) + “HÂL 1 · UYUMLU” Chakra 700 13 .12em hâl renginde + aralık “0–30” Mono 11 tx3 + tek cümle Sans 13 tx2.
  - Özel küreler satırı (34 px): r 13, kutu 32: “bilinmiyor”, “karantina”, “çekirdek” (Sans 12 tx2).
  - **GEÇİŞ** şeridi (58 px; mor kenar α.3, zemin vi α.05): “GEÇİŞ” cap (mor) + küre (r 17, kutu 40, OTOMATİK) + 30 px ölçek (6 px iz) + işaretçi (3×22 beyaz + ışıma). Küre ve işaretçi aynı 18 sn saatinden (§5.6).
- Diğer konular Sade düzeniyle (içerik C rehberinden), FX görünümüyle.

## A.11 Lejant kartı ve düğüm ipucu (F-Masa8)

**Lejant kartı** (`FxLegendCardView`): sağ 16, alt 88, 440 geniş (TSV `legend-*`).

- Başlık 44 px: `legend` 16 cy + “LEJANT” Chakra 700 14 .18em + “harita” 12 tx3 + kapat. Bölümler iç boşluk 12/14, aralık 9, aralarında 1 px cy α.1.
  1. **YZ KÜRESİ · SIZMA %**: üstte üç küre (r 15, kutu 36; düzey 12 / 50 / 92; %15, %50, %85'te) + `FxScale` (bölge adlarıyla) + altta 0 / 30 / 70 / 100 (Mono 11 tx3).
  2. **GÜVEN**: beş rozet + ad (12 tx2): doğrulandı, rapor, şüpheli, bilinmiyor, yalan.
  3. **BÖLGE**: küçük küreler (r 12, kutu 28): bilinmiyor, karantina, YZ çekirdeği.
  4. **İŞARETLER**: yayılım (44×18 iplik, üç vuruş, akar) · ekip ve rota (elmas + kesik nane) · danışman notu (20 px üçgen).
- Giriş: y +16 → 0, 0,45 sn. LEJANT düğmesi açık durur.

**Düğüm ipucu** (`FxNodeTooltipView`; 232 geniş)

- Yer: sol = düğüm x − 206, üst = y + 0,95r + 36; işaretçi düğüm x'inde (soldan 206). Sığmazsa ayna (sağa). **Bölge adı gizlenir.**
- Kap: döküm fişiyle aynı; iç boşluk 10/12, aralık 8.
- Satırlar: ad Chakra 700 14 .12em + rozet + “rapor” (12 vi) · değer “≈52” Mono 600 26 vi `GlowViolet45` + değişim (↑1,4 /gün kırmızı) · **YZ HÂLİ** satırı (22 px, hâl kenarı, “2 · KARARSIZ”) · 30 px `FxScale` (işaretçi 52) · `cursor` + “tıkla: bölge kartı” (12 tx3).

## A.12 Tam bölge dosyası (F-Masa9)

`FxRegionDossierView`. Kap = çekmece kabı (§A.10), sekme rengi cy, başlık “BÖLGE DOSYASI” + “tam dosya”; (16, 92, 500×918). Yer kuralı Sade §A.14.11 (bölgenin karşı yanı). TSV `ds-*`.

| Bölüm | Yükseklik | İçerik |
|---|---|---|
| Başlık | 62 | küre (r 26, kutu 62, `fil` .55) · “DOĞU ASYA” Chakra 700 24 .1em `GlowCyan35` + “R-10” Mono 12 tx3 + sağda kırmızı `sm` damga “GİZLİ” · meta: `people` “1,64 mlr” · `chain` “4 bağlantı” · am `antenna` “izleme ağı yok” |
| Sızma | 73 | bölge kartıyla aynı (büyük değer + ölçek) · altında “kaynaklar 14 puan ayrışıyor · YZ hâli 2” (11 tx3) |
| Kaynaklar | 187 | tablo (satır 5/4, alt 1 px cy α.08): ad + küçük not · değer Mono 600 15 (şüpheli am, doğrulandı mt, rapor vi) · tarih Mono 12 tx3 · rozet. Değiştirilmiş kaynakta kırmızı bayrak (18 px, rd kenar α.5) `term` + “YZ günlüğü: elle değiştirildi” |
| Son 30 gün | 121 | 468×92 grafik: hâl bantları (0–30 am α.05, 30–60 `#8C7CFF` α.05) · ızgara 0/20/40/60 (cy α.14 kesik 2/3; 0 çizgisi α.35 düz) · rapor çizgisi vi 2 px + ışıma, altı vi α.25 → 0 · son değişiklik am kesik + halka + “38” · saha baklavası nane + “saha 52”. Çizgi 1,6 sn'de çizilerek gelir |
| Bağlantılar | 103 | 2 sütun, 34 px çipler: bağlı bölgenin küresi (r 11, kutu 26) + ad (Chakra 600 12 .06em) + sağda değer. Yayılım kaynağı: kenar ai α.6, zemin ai α.08, sağda “← yayılım” (`#8FD3FF`) |
| Eylemler | 180 | 4 satır × 34 (aralık 5): ızgara 20/18/auto/1fr/auto: tuş · simge 17 cy · ad Sans 600 13 · etki (12 nane) · maliyet (Mono 11 tx3). Son Şalter kırmızı. Davranış bölge kartıyla aynı |
| Saha | 64 | bölge kartındaki ekip satırı |

---

# B · F-Baskın (holo taktik harita)

Referanslar: `F-Baskin1.jpg` … `F-Baskin9.jpg`, `reference/zoom/F-Baskin*`, videolar `baskin_*.mp4`. 3D geometri, kamera, ızgara → ekran formülü ve davranış Sade §B ile aynıdır. Aşağıda FX farkları.

## B.1 Sahne, kamera, arka plan

- **Kamera:** Sade §B.1 aynen (ortografik, k = 1,5, ızgara → ekran formülü).
- **Işık:** **yok** (Unlit). Gerçek zamanlı gölge yok.
- **Arka plan** (`RaidFxBackground`; Screen Space – Camera, uzak düzleme yakın):
  1. `tx_fx_raid_bg.jpg` (radyal koyu lacivert + merkezde maskeli 48 px ızgara) ekranı kaplar.
  2. `tx_fx_raid_cone_750x440` (1500×880 tasarım px; tahtanın altında projeksiyon ışığı), merkez = tahta merkezinin ekran izdüşümü; α .85 ↔ 1, 4 sn.
  3. Tarama çizgileri (§A.5) bu katmanda da.
- **Tahta düşen gölgesi:** Sade'deki gibi (`UIPolygonGraphic`), siyah α.55.

## B.2 Holo malzemeler

Kaynak `spec/raid_fx/raid_fx_palette.json`. Her Sade malzeme rengi için `paper` (eski), `fill` (holo yüz), `edge` (holo kenar).

- **Kural:** nötr ve kahve tonlar parlaklık sırası korunarak koyu cama; YZ kırmızısı `#FF4D63`; ekip yeşili `#3DFFB4`; plan mavisi `#5BE4FF`; turuncu / bakır `#FFB547`; sarı `#FFD58C`. Elle ayarlananlar `handTuned`.
- **Yüzler:** URP Unlit, düz renk. Sade'deki üst / batı / güney ton üçlüleri aynen kullanılır (her biri ayrı `fill`); ışık olmadığı için tonlar birebir tutar.
- **Kenarlar** (`HoloEdgeBuilder`): parça başına “sert kenar” listesi (açı > 30°, ayrıca silüet), ekranda **1 px** sabit kalınlık. Yöntem: kenar başına dörtgen, vertex shader'da clip uzayında piksel genişliğinde açılır (`FxEdgeLine`; `MeshTopology.Lines` platforma göre kalınlık tutmaz). Renk `edge`.
- **Kenar ışıması (yol B, varsayılan):** her kenar ikinci kez 3 px, α.40. Tasarımdaki `drop-shadow(0 0 1,6px cy α.55)` karşılığı.
- **Yol A (onayla):** URP Bloom (eşik 1,0); kenar rengi HDR ×1,6. Yalnız kenarlar parlar.
- **Zemin dokusu:** `art/raid/tx_fx_plan_*_2048.png` (Sade ozalit eşlemesiyle aynı: doku pikseli → dünya `x = −0,25 + px/240,94; z = 0,25 − py/240,94`). Plan çizgileri camgöbeği (ızgara α.42, çerçeve α.85).
- **Karton figür:** yüz `#0B2E2A`, kenar `#FFD58C`; harf nane (`pieceText.standeeLetter`). “Tur bitti” ve “düştü” durumları Sade gibi.
- **Akrilik çubuk:** α.55.
- **Parça yazıları:** Chakra Petch 700; harf figürleri nane, diğerleri `#E4F8FF`, YZ kırmızısı korunur.
- **Holo titreme** (`BoardFlicker`): bütün tahta malzemelerinin rengi global `_FxBoardFlicker` ile çarpılır. 7 sn döngü (1 sn gecikme): %48'de .86, %84'te .90, diğer anlar 1. Hareketi azaltta 1.

## B.3 Tahta efektleri

- **Tarama düzlemi** (`BoardScanPlane`):
  - Tahta üst yüzünün dört köşesi (`(−0,3, −0,3)` … `(8,3, 8,3)`, z = 0) ekrana izdüşürülür; bu çokgen stencil maskedir (`UIPolygonGraphic` + `Mask`, grafik gösterilmez).
  - İçinde `tx_fx_board_scan_90` (90 px yüksek, tam genişlik), y −120 → 1100, 6,5 sn doğrusal, eklemeli.
  - Kamera ya da çözünürlük değişince çokgen yeniden hesaplanır.
- **Giriş:** tahta 1 sn'de belirir (opaklık; malzeme alfası ya da `_FxBoardFlicker` 0 → 1).
- Video `baskin_holo_tahta.mp4`.

## B.4 İşaretler ve niyetler

- **İşaretler:** Sade `raid_tokens.marks` yapısı aynen; renkler `raid_fx_palette.json › marks.<ad>.colorFx / edgeFx`:
  - hareket camgöbeği (dolgu α.16, tarama α.34, kenar α.85 2 px),
  - yetenek hedefi nane,
  - seçili camgöbeği (dış 3 px + iç 1,2 px),
  - kesin isabet kırmızı,
  - yalan olabilir: kırmızı dolgu + kehribar kesik kenar,
  - onarım kırmızı kesik.

  Unlit/Transparent, ZWrite kapalı.
- **Niyet okları:** Sade geometrisi, renk `#FF4D63`. FX ekleri:
  - **Akan ışık** (`IntentFlowOverlay`): her kırmızı niyet çizgisinin kopyası, `#FFE3E7` 2,2 px, kesik 3/26, 28,6 px/sn, ışıma 3 px rd.
  - **Şimdi** olan niyet: fosfor `#FFD58C` α.95, +9 px.
  - **Bitti:** α.35.
  - **Yalan olabilir:** kesik 10/6, bayrak kenarı kehribar.
- Gölge (Sade 1,5/2 px) holo'da siyah α.35.

## B.5 Dünya etiketleri

Konum kuralları Sade §B.3. Görünüm (`tokens_fx.type › raid*`, CSS `.fx .hp`, `.flag`, `.pop`, `.stamp`):

| Etiket | FX görünümü |
|---|---|
| Can (robot, hedef) | 17 px, iç boşluk 0/4, zemin `#02080E` α.88, 1 px kenar; noktalar 7 px −12° eğik + ışıma. YZ: kırmızı kenar α.75 + ışıma 10. Ekip: nane. Tur bitti α.55; düştü kırmızı |
| Sıra bayrağı | 60×34: direk 2 px (rd → α.2 geçiş + ışıma) + 21 px kutu (`#1A0308` α.92, 1,5 rd kenar, Mono 600 14 `#FFC2CA`, ışıma 10). **Şimdi:** zemin rd, yazı `#1A0306`, 24 px, 16 px yazı, ışıma 16, 1 sn nabız. Bitti α.45 + üstü çizili. İptal: gri + kırmızı −28° çizgi. Yalan olabilir: kesik kehribar kenar + 16 px “?” dairesi |
| Açılır yazı (−2, +1) | 22 px, iç boşluk 0/7, `#1A0308` α.92, 1,5 rd kenar, Mono 600 16 `#FF8A97`, ışıma 14; 1,6 sn'de 3 px yukarı-aşağı süzülür. Onarım kehribar |
| Damga (PATLAR, ZIRH) | 24 px (PATLAR 28), 1,5 kenar = renk, zemin `#02080E` α.9, Chakra 700 13 .16em, ışıma 12. PATLAR kırmızı + glitch 3,5 sn |

## B.6 Düğüm küresi (`NodeOrbAnchor`)

- **Yer:** YZ düğüm dolabının (ızgara 6,3) tepe noktasının ekran izdüşümü; küre merkezi **64 px yukarıda**. Her kare yeniden (dünya etiketi gibi).
- **Küre:** r 22, normal, tohum 55, düzey `Board.NodeOrb.Level`. 3,2 sn'de ±5 px süzülür (yalnız küre).
- **Bağ:** dolap tepesinin 22 px üstünden kürenin 20 px altına dikey çizgi; am 1,4 px α.8, kesik 3/4, 20 px/sn akar, ışıma 3.
- **Düzey değişimi:** makineler evresinde VM değeri artar (58 → 64, F-Baskin3); küre yumuşak geçer (§5.3). Son tur 86 (hâl 3).
- **Çöküş** (F-Baskin9, `Collapsed`): düğüm küresi kaldırılır. Spot merkezinin 70 px üstünde gri `unknown` küre (r 19): 0,3 sn gecikmeyle 2,4 sn ease-in, opaklık 1 → .25, ölçek 1 → .7; öyle kalır.

## B.7 HUD (F-Baskin1)

TSV `turn`, `objective`, `top-buttons`, `team`, `end`.

**Tur plakası** (16, 14; 62 px; cam panel, iç boşluk 0/12, aralık 12)

- “TUR” cap + “3/6” Mono 600 20 am `GlowAmber50`.
- 6 kutu 24×26, aralık 4, 5 px pahlı, kenar `line`, Mono 600 12 tx3:

  | Kutu | Görünüm |
  |---|---|
  | geçmiş | zemin cy α.12, yazı tx2 |
  | şimdiki | zemin cy, yazı `#02050A`, ışıma 12 |
  | son | kenar rd α.7, yazı rd2 |
  | son + şimdi | zemin rd, yazı `#1A0306`, ışıma 12 rd |

- Evre etiketi 30 px, iç boşluk 0/11, aralık 7, Chakra 700 12 .16em; lamba 8 px (1,2 sn yanıp söner): **SIRA SENDE** nane (kenar α.6, zemin α.08) · **MAKİNELER** kırmızı (kenar α.7, zemin α.1).
- Giriş y −14 → 0, 0,7 sn.

**HEDEF paneli** (16, 88, 394; iç boşluk 8/12/9)

- Başlık satırı 22 px: “HEDEF” + “ikisini de kır” (12 tx3) · sağda **HÂL çipi** (`FxAiStateChip`: küre r 9, kutu 22, `fil` .5 + “HÂL 2” Chakra 700 11 .1em hâl metin rengi; ipucu “YZ düğümünün hâli”) · **YZ yeteneği çipi** (20 px, kenar rd α.6, zemin rd α.08, `ic_fx_copy` 12 + “YEDEKLEME” Chakra 700 11 .1em rd2).
- Hedefler (üst boşluk 4, aralık 8): holo küçük resim 46×58 (`thumb_fx_node` / `_backup` / `_debris_backup`; ışıma 1,5) + ad Sans 600 13 (+ `ic_fx_armor` 12) + kırmızı can noktaları; arada “+” Chakra 16 tx3; 1×40 ayraç; **SİVİL** cap + `ic_fx_civil` 16 × toplam (kırılanlar sağdan `ic_fx_civil_x`).
- Kırılan hedef: küçük resim α.6, can yerine `ok` hap “KIRILDI”.
- Giriş y −14 → 0, 0,7 sn, 0,08 sn gecikme.

**Üst sağ düğmeler** (sağ 16, üst 14, aralık 8)

- 44 px, iç boşluk 0/12/0/11, aralık 8, `fx_glass_btn`, Chakra 700 12 .14em tx2; simge 15 cy.
- Açık (`on`): kenar cy, yazı tx, ışıma 16 + altta 2 px cy iç çizgi.
- **GERİ ÇEKİL:** kırmızı cam (kenar rd α.6, yazı rd2).
- GÜNLÜK noktası 11 px rd (−5, −5), 1,2 sn nabız.
- Giriş 0,12 sn gecikme.

**Ekip kartları** (sol 16, alt 16, aralık 8, alta yaslı)

- 204×74, iç boşluk 0/10/0/8, aralık 8; `fx_glass_team` (nane cam, kenar mt α.3); üstte 2 px nane çizgi α.6 + ışıma.
- Tuş fişi 16 (5, 5) · küçük resim 40×56 (sol boşluk 12) · ad Chakra 700 12 .12em + can noktaları · sağda iki hak simgesi (24 px, 5 px pahlı):

  | Hak | Görünüm |
  |---|---|
  | açık | kenar cy α.6, simge cy, zemin cy α.1 |
  | hazır (Q) | zemin am, simge `#1A1000`, ışıma 10 |
  | kullanıldı | α.45 |
  | uyarı | am kenar + 0 → 4 px am α.25 halka nabzı (1,2 sn) |

- Kart durumları:
  - **seçili:** 82 px, kenar am, ışıma 22; üst çizgi am; ad am2.
  - **tur bitti:** α.6.
  - **uyarı:** kenar am + 1 px am α.5 halka + ışıma.
  - **düştü:** kırmızı cam; üst çizgi rd; ad tx3 + kırmızı üstü çizili; küçük resim α.45 + kırmızı çapraz (ışıma); `sm` kırmızı damga “DÜŞTÜ” (sağ 8, üst 40, −6°).
  - **kilitli** (makineler): satır α.5 + doygunluk .4.
- Giriş y +16 → 0, 0,6 sn, 0,1 + 0,06·i gecikme.

**Sağ alt** (sağ 16, alt 18, aralık 10)

- “Geri al [Z]” `lg` (48 px) · “TURU BİTİR [Enter]” `endTurn` (58 px, Chakra 600 17 .14em, camgöbeği dolu).
- Kilitli: ikisi `off` + `lock` simgesi.
- Onay: “EMİN MİSİN? [Enter]” `confirm` (kehribar, 1,1 sn nabız).
- Giriş 0,15 sn gecikme.

## B.8 Kartlar ve durumlar (F-Baskin2–9)

Davranış Sade §B.5 ve §B.9; aşağıda FX görünümü. Kutular `fx_frames.json › panels`.

| Kart | Kutu | FX görünümü |
|---|---|---|
| Birim kartı (F-Baskin2, 7) | 16, 744, 360 | Cam, kenar am α.55, parantezler am. Başlık: küçük resim 46×64 + ad Chakra 700 17 .14em + `hp` nane + noktalar. Hak satırları 42 px (kenar `line`; kullanıldı kesik + α.75 + nane ✓; **hazır**: kenar am, zemin am .24 → .08, ışıma 18, simge am, ad am2). Etki çipleri 24 px (kenar rd α.45, zemin rd α.08, değer Mono 13 rd). Giriş y +16, 0,45 sn |
| Üstüne gelme kartı | F-Baskin2: 1646, 818, 258×163 · F-Baskin7: 1646, 791, 258×195. Kural: sağ alt sabit bölge, alt kenar ≤ 986 (TURU BİTİR'in 18 px üstü); içerik artınca yukarı büyür | Cam, kenar rd α.45, parantezler rd. Başlık: küçük resim 40×46 + ad Chakra 700 12 .12em + kırmızı noktalar. Satırlar 30 px Sans 13 (simge 14 rd2; değer Mono 13: kötü rd, iyi nane ✓). Kazanç kutusu (F-Baskin7): nane kenar α.5, zemin α.08; yeşil `sm` damga “HEDEF KIRILIR” + “operasyon kazanılır”. Giriş x −24, 0,4 sn |
| Son olay fişi (F-Baskin3) | 16, 196, 394 | Cam, kenar rd α.45, solda 3 px rd. SIRA satırı: kutular 22 px (bitti: rd α.12 + üstü çizili; **şimdi**: rd dolu, ışıma, 1 sn nabız; iptal: −35° kırmızı çizgi) + sağda “Boşluk · hızlandır”. Olay satırı: `fx_robot_mini` 22 + “#3” Mono 600 rd + cümle Sans 14 (`arrow` 13; hedef kalın; “−1” rd). Giriş x −24, 0,45 sn. Video `baskin_makineler.mp4` |
| Turu bitir onayı (F-Baskin4) | sağ 16, alt 100, 336 | Cam, kenar am α.6. `hand` + “HAKKI DURAN BİRİM” Chakra 700 13 .16em am · jeton + “Analist” + kehribar hak simgeleri + “kullanılmadı” · tehdit kutusu (kenar rd α.45, zemin rd α.08: robot + “#2 dron onu vuracak” + “−1”) · “Enter yine de bitir · Esc vazgeç”. Giriş y +16, 0,4 sn |
| Günlük (F-Baskin5) | 1524, 74, 380×560 | Başlık 44 px: nane lamba + “OPERASYON GÜNLÜĞÜ” Chakra 700 13 .16em + “T3 · 14:32” + kapat. Satırlar alta yaslı, aralık 7, üstte 46 px solma maskesi; Sans 13 satır 1,35 (iyi nane ✓, kötü kırmızı ✕ + `#FFC2CA`, sistem ok tx3; numara Mono 600 rd); ayraç Chakra 600 11 .16em cy + solan çizgi; YZ satırı: sol 2 px rd, zemin rd α.08, “AYNA · ELE GEÇİRİLEN SATIR” + Mono metin. Giriş x +24, 0,45 sn; satırlar 0,4 sn |
| Kurallar (F-Baskin6) | 1474, 74, 430 | Başlık 44 px (`card` + “KURALLAR” Chakra 700 14 .18em + “operasyon emrinden”). 6 satır: simge 15 cy + Sans 13 satır 1,4 (kalın tx). Lejant: “LEJANT” + “PLAN B-1 · 1:50”; 2 sütun, 10 madde, 30×18 holo örnekler + Sans 12 |
| Geri çekilme (F-Baskin8) | 1524, 74, 380 | Kararma `#010308` α.55 (0,4 sn). GERİ ÇEKİL → “EMİN MİSİN? TEKRAR BAS” (rd dolu, yazı `#1A0306`, ışıma 22, 1 sn nabız; sağa yaslı, sola büyür; kararmanın üstünde). Fiş: kenar rd α.7, ışıma 30; `door` + “GERİ ÇEKİLME · 2. ONAY” Chakra 700 14 .16em rd; gövde Sans 14 (kalın rd2); “İkinci basış geri çeker.” + `sm` “Vazgeç [Esc]”. Giriş y −14, 0,35 sn |
| Sonuç (F-Baskin9) | memo 1240, 540, 460 | HUD yok (Sade: 200 ms'de α 0). **Spot:** r 150; içi saydam, %86'dan sonra `#01040A` α.72 (1 sn'de belirir). **Spot halkası** 300 px: nane 1,5 + ışıma 30 + iç ışıma; 2 sn nabız; 14 px dışarıda kesik halka 14 sn'de döner. **Büyük damga** “HEDEF KIRILDI” (spot merkezine göre x −190, y r − 36): 60 px, 3 px nane kenar, zemin `#020E0C` α.9, Chakra 700 32 .18em `GlowMint60`, ışıma 30; 0,4 sn gecikme, 0,6 sn: ölçek 1,6 → .96 → 1, −5°. **Sonuç paneli:** kenar nane α.5, ışıma 40; “OPERASYON SONUCU” + `ok` hap BAŞARILI + “G-074 · T6”; “HEDEF KIRILDI” Chakra 700 34 .1em `GlowMint60`; alt yazı; 4 satır (30 px; iyi nane, kötü kırmızı); `pri lg` “Operasyon raporuna geç [Enter]”; not. Giriş x +24, 0,6 sn, 0,5 sn gecikme. **Çöküş küresi** §B.6. Video `baskin_sonuc.mp4` |

B3 (EMP önizlemesi), B4 (aldatma), B7b (süre doldu) için ayrı FX karesi yok: tahta Sade `reference/raid_B/` + bu bölümün renkleriyle, HUD bu kurallarla.

---

## 6. Hareket kataloğu

Tam liste `tokens_fx.json › motion` (`entrance` 27, `loops` 35). Videolar gerçektir; CSS süreleri `reference/html/` içinde.

**Giriş eğrisi:** `cubic-bezier(.2, .8, .2, 1)` (`FxEaseOut`). Kapanışlar çizilmedi; varsayılan: girişin yarı süresi, ease-in, opaklık + 12 px.

**Sürekli döngüler (özet)**

| Ne | Süre | Hareket |
|---|---|---|
| Küre | sürekli | §5 |
| Harita tarama ışığı | 12 sn | x −200 → 2100 |
| Yayılım ipliği | — | çizgi 37,5 px/sn, parçacık 50 px/sn |
| Ekip rotası / bağ çizgisi / vurgu | — | 17,1 / 22,2 / 25 px/sn |
| Seçili kıyı | 2,4 sn | α .55 ↔ 1 |
| Panel süpürmesi | 5,2 sn | soldan sağa |
| LED parıltısı | 2,8 sn | soldan sağa, ease-in-out |
| Saat iki noktası | 1 sn | adım |
| Rozet, ŞÜPHELİ, YENİ, ilerleme ucu | 1,4 / 1,6 / 1,2 sn | α .55 ↔ 1 |
| Uyarı dalgası | 1,8 sn | ölçek .6 → 1,9, α .9 → 0 |
| Jeton halkası / nişangâh / Son Şalter halkası / seçili araştırma düğümü | 6 / 12 / 8 / 7 sn | dönüş |
| Son Şalter şeridi | 1,2 sn | doku 28 px kayar |
| SAHTE damgası | 4 sn | glitch (%86–92 arası kayma + kırpma) |
| Karartı | 1,6 sn | doku 40 px ileri-geri |
| Daktilo imleci | 0,9 sn | adım |
| Tahta tarama düzlemi / titreme | 6,5 / 7 sn | §B.2–B.3 |
| Niyet akışı | — | 28,6 px/sn |
| Düğüm küresi süzülmesi | 3,2 sn | ±5 px |
| Projeksiyon konisi | 4 sn | α .85 ↔ 1 |
| “Şimdi” bayrağı / evre lambası / kuyruk kutusu | 1 / 1,2 / 1 sn | nabız / adım |
| Sonuç spot halkası | 2 + 14 sn | nabız + dönüş |

**Hareketi azalt:** döngüler §7'deki donma evresinde durur; küreler 8 fps; girişler atlanır; daktilo tam metin; tarama ışıkları gizli.

---

## 7. Doğrulama

Sade §5 geçerli; FX ekleri:

1. **`ReferenceOverlay`:** 19 referans (`reference/screens/F-*.jpg`). F12 aç-kapa; F11 opaklık; Shift+F11 yanıp sönme.
2. **Ön ayarlar** (`FxDebugMenu`): F1–F9 F-Masa1–9; Shift+F1–F9 F-Baskin1–9; Ctrl+F12 FxOrbLab.
3. **Donma modu** (`FxClock.Freeze`): referans çekimindeki evreler. Nabız %50, yanıp sönme 0, imleç 0, dönüş 0, kesik akış 0, LED parıltısı %45, süpürme 0, tehlike 0, dalga %30, süzülme 0, koni %50, titreme 0, glitch 0, karartı 0, halka nabzı 0, ışıma nabzı %50. Girişler bitmiş. **Tarama ışıkları gizli.** Küreler maskelenir (aşağıda), donmaları gerekmez. F-YZ'de OTOMATİK saati ≈ 4 sn'de donduruldu: işaretçi ve sayı **26**.
4. **Yakalama:** `Tools/FX UI/Capture Game View` → `Docs/UI_Fx/captures/<kare>_<tarih>.png` (donma modunda).
5. **Karşılaştırma:**

   ```
   python3 Docs/UI_Fx/tools/compare.py Docs/UI_Fx/captures/F-Masa1_….png Docs/UI_Fx/reference/screens/F-Masa1.jpg \
       --tsv Docs/UI_Fx/reference/measure/F-Masa1.tsv --mask Docs/UI_Fx/reference/measure/F-Masa1_mask.json
   ```

   - `--mask`, kürelerin kutularını fark ve kenar hesabından çıkarır (`_diff.png`'de koyu mor). Gerekirse `--pad 4`.
   - **UI:** panel kenarlarında ve büyük metinlerde çift çizgi kalmasın (≤ 2 px). Işımalar ±4 px.
   - **Harita:** kıyı, sınır, nokta matrisi ve parıltı aynı izdüşümden; ≤ 1 px.
   - **Holo tahta:** taban çerçevesinin dört köşesi ±4 px; parça ve işaret merkezleri ±4 px; renkler palet değeri (Unlit).
6. **Küre:** §5.5 (22 altın kare).
7. **Hareket:** her video için Unity'de aynı akışı oynat, ekran kaydı al (`captures/motion/`), yan yana izle. Süre farkı ≤ %10.
8. **Taşma denetleyicisi** (`Tools/FX UI/Check Text`): TR ve EN; en küçük 11 px; Auto Size, UpperCase yok. Dünya etiketleri dahil.
9. **EditMode testleri:** Sade'dekiler (Durum B'de uygulananlar) + `aiColor` örnekleri (`map_fx.json › aiColor.samples`), bölge boyası alfaları (s = 0, .5, 1), değer çipi ve etiket konumu (DA istisnası dahil), kart yerleşimi (sağ, sağa yaslı, sol), ölçek yüzde → piksel, `OrbStateMapper`, `AiStateRules`, küre testleri (§5.5).
10. **En-boy oranları:** 1920×1080, 1920×1200, 2560×1440, 3440×1440. Harita kaplar; ekran katmanları ekrana gerilir; küreler ve yazılar ölçeklenmez; tahta kesilmez.
11. **Performans:** Profiler (masa F-Masa7, baskın F-Baskin3): CPU küre ≤ 2,5 ms, GC 0 B, draw call sınırları (§4.10).

---

## 8. Fazlar ve kabul ölçütleri

Her fazın sonunda dur, raporla, onay bekle.

| Faz | İçerik | Kabul ölçütü |
|---|---|---|
| **F0 · Keşif ve plan** (kod yok) | Sade durumu (A / B; kurulu sınıflar, davranış-görünüm ayrımı); C varlığı; Unity ve URP sürümü, renk uzayı (Linear / Gamma), URP Asset (HDR, Bloom, SRP Batcher); Input System; lokalizasyon; tween; DI/async; Unity MCP. `PLAN.md`: bulgular, klasör/asmdef, Durum B ise uygulanacak Sade parçaları, riskler, ≤ 5 soru | kullanıcı onayı |
| **F1 · FX temeli** | fontlar + TMP ön ayarları, sprite kiti + atlas + postprocessor, dokular, `FxTheme` (JSON'dan), `FxClock` + `FxLoop` + `FxTween`, ortak bileşenler (§4.5), shader'lar (`FxAdditive`, `FxLine`), `FxUI_Gallery` | galeri `_contact_sheet` ve yakın planlarla eşleşir; 9-dilimler 3 boyda bozulmaz; döngüler tek saatten; konsol temiz |
| **F2 · YZ küresi** | `OrbSimulation` (birebir port) + testler, `OrbRenderer` / `OrbView` / `OrbClock`, belirlenimci mod + altın yakalama, **FxOrbLab** (F-YZ) | 22 altın kare eşik içinde; FxOrbLab F-YZ ile karşılaştırması (küreler maskeli); geçiş videosuyla gözle eşleşme; 12 küre bütçe içinde |
| **F3 · F-Masa durağan** | harita katmanları, `RegionFxGraphic` ×12 (küreyle senkron), karantina hattı, düğümler (küre + değer + ad), iplikler, rota, jetonlar, uyarı, ekran katmanları; köşe etiketleri, zaman, sekmeler, LEJANT, Son Şalter; girişler | F-Masa1 karşılaştırması (maskeli); 16:10 ve 21:9 doğru; `masa_acilis.mp4` ile eşleşir |
| **F4 · F-Masa paneller ve etkileşim** | seçim + nişangâh, bölge kartı + bağ çizgisi + yerleşim, eylem üstüne gelme; beş çekmece (rehberde YZ hâlleri sayfası), FON fişi, düğüm ipucu, lejant kartı, tam dosya, vurgular; daktilo, çizilerek gelme; klavye, Son Şalter basılı tutma, İllüzyon | F-Masa2–9 karşılaştırmaları; panel kuralları eksiksiz; videolarla eşleşir; boşta 0 GC |
| **F5 · Holo tahta** | holo malzemeler (palet), kenar geometrisi + ışıma (yol B), zemin dokusu, arka plan + koni, tarama düzlemi, titreme, işaretler, niyetler + akış, dünya etiketleri, düğüm küresi | F-Baskin1 tahtası ±4 px; `baskin_holo_tahta.mp4` ile eşleşir |
| **F6 · Baskın HUD, kartlar, durumlar** | tur, HEDEF (+ HÂL ve yetenek çipleri), üst düğmeler, ekip kartları, sağ alt; birim ve üstüne gelme kartları; makineler + son olay fişi, onay, günlük, kurallar, son tur, geri çekilme, sonuç + çöküş | F-Baskin2–9 karşılaştırmaları; makineler evresinde girdi kilitli; videolar |
| **F7 · Cila** | EN + taşma, en-boy görüntüleri, “Hareketi azalt”, performans, `UYGULAMA_NOTLARI.md` (bileşenler, Sade ile ilişki, oyuna bağlama, bilinen sapmalar), son yakalamalar | tüm kontrol listeleri yeşil |

---

## 9. Açık kararlar

Varsayılanı uygula, ayarlanabilir bırak ve `KARARLAR.md`'ye yaz.

1. **FX ve Sade birlikte mi?** **Varsayılan:** FX varsayılan görünüm; Sade prefab'ları projede kalır, geliştirici anahtarı `UiSkin {Fx, Sade}` ile seçilebilir. Seçenek: oyuncu ayarı; ya da Sade görünümünün kaldırılması (kullanıcı söylerse).
2. **Küre yolu.** **Varsayılan:** yol A (küre başına RenderTexture). Yol B yalnız RT sorunluysa (ör. hedef platform).
3. **Küre kare hızı.** **Varsayılan:** 40 fps üst sınır, uyarlamalı 16'ya kadar; hareketi azaltta 8. Seçenek: kalite ayarına bağla.
4. **Cam bulanıklığı.** **Varsayılan:** yok. Seçenek: URP'de tek geçişli bulanık ekran kopyası (yalnız açık paneller; maliyet ölç).
5. **Hâl 1 ve 3 metin renkleri** (yalnız hâl 2 çizildi). **Varsayılan:** `#FFE0A8` / `#D4ECFF`.
6. **Tahta ışıması.** **Varsayılan:** yol B (ikinci yarı saydam kenar). Seçenek: yol A, URP Bloom (onayla).
7. **“Doğrulandı” / “Onaylandı”.** FX karelerinde “doğrulandı”, Sade dizelerinde “onaylandı” (`trust.verified`). **Varsayılan:** Sade anahtarı geçerli; tek terim için kullanıcıya sor.
8. **Bölge kartı yerleşimi.** **Varsayılan:** §A.9 (sağ → sağa yasla → sol). Seçenek: Sade kuralı aynen.
9. **Baskın düğüm küresinin düzeyi.** Tasarımda tur ilerledikçe artar (58 → 64 → 86). **Varsayılan:** VM'den; mock'ta sabit tablo. Oyunda neye bağlanacağı (bölgenin sızması + makineler evresinde artış?) oyun tasarımı sorusu.
10. **Simge ışıması.** **Varsayılan:** arkada `fx_glow_dot_32`. Seçenek: ışımasız (daha sade).
11. **OTOMATİK döngü simgesi** (yalnız FxOrbLab). **Varsayılan:** `ic_undo`.
12. **Sürekli hareketin oyunu duraklatmada sürmesi.** **Varsayılan:** sürer (yalnız görsel). Seçenek: duraklatmada döngüler yavaşlar (×0,25).

---

## 10. Yapma listesi

Sade §8'in hepsi geçerli (kaldırılan öğeleri geri koyma, ekranın ortasına kalıcı panel koyma, iki ipucu / iki çekmece, harita üstü yazıyı ölçekleme, bölge dolgusunu sprite ile yapma, önizleme hesabını görünümde yapma, sınır verisini değiştirme). FX ekleri:

- **Küreyi sprite dizisi, video ya da parçacık sistemiyle taklit etme.** Hâl geçişi kaybolur.
- **`fx_orb.js`'i “sadeleştirerek” port etme.** Katman, sıra ve RNG çağrı sırası aynen; değişiklik gerekiyorsa önce sor ve KARARLAR'a yaz.
- **UI'da post-processing bloom** kullanma; ışımalar pişmiş sprite.
- **Her nabız / dönüş için ayrı `Update`** ya da Canvas yeniden kurma.
- **Sabit renk hex'i** bileşenlere yazma; `FxTheme`.
- **Kâğıt görsellerini** (C/Sade sprite'ları, Kalam, kâğıt dokusu) FX ekranına karıştırma. İstisna: beyaz simgeler.
- **SAHTE gibi metinleri sprite'tan** kullanma (`fx_tr_fake` yalnız TR önizleme); TMP ile yerelleştir.
- **TMP UpperCase** (Türkçe i/İ).
- **Holo tahtada Lit malzeme, gerçek ışık ya da gölge.**
- **Bölge boyasını küreden bağımsız** canlandırma; ikisi aynı görünen düzeyi kullanır.
- **Hareketi azalt ayarını** yok sayma.

---

## 11. İlk yanıtın (F0)

1. **Paket özeti:** 8–10 madde. FX'in Sade ile ilişkisi; küre, harita boyası, holo tahta; neyin nerede olduğu.
2. **Proje incelemesi bulguları:**
   - Sade durumu (A / B) ve davranış-görünüm ayrımı,
   - C varlığı,
   - Unity ve URP sürümü, renk uzayı, URP Asset (HDR, Bloom, SRP Batcher),
   - uGUI/TMP, Input System, lokalizasyon,
   - tween, DI/async, Newtonsoft,
   - Unity MCP bağlantısı.
3. **`Docs/UI_Fx/PLAN.md` taslağı:** fazlar, Durum B ise uygulanacak Sade parçaları, küre portu planı (sınıflar, testler, altın yakalama), riskler.
4. **En fazla 5 soru.** (Örnek: FX Sade'nin yerini mi alıyor, yan yana mı? Bloom'a izin var mı?)

**Kod yazma; onay bekle.**
