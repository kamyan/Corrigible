# SON ŞALTER · “C · Analist Masası” arayüzünü Unity'ye uygulama

> **Bu dosya Claude Code için uygulama talimatıdır.** Paket kökü bu dosyanın bulunduğu klasördür (önerilen yer: `Docs/UI_C/`, `Assets/` dışında). Aşağıdaki tüm yollar bu klasöre göredir.
> Kod tanımlayıcıları İngilizce, açıklamalar Türkçedir.

**İçindekiler** — 0 Görev ve çalışma biçimi · 1 Oyun ve tasarım özeti · 2 Değişmez kurallar · 3 Paket içeriği · 4 Mimari · 5 HTML/CSS → uGUI çevirisi · 6 Varlık hattı (sprite, doku, font, TMP, malzeme tarifleri, lokalizasyon) · 7 Ekran tarifleri (C1–C5) · 8 Etkileşim, girdi, hareket · 9 Doğrulama · 10 Fazlar ve kabul ölçütleri · 11 Açık kararlar · 12 Yapma listesi · 13 İlk yanıtın

---

## 0. Görev ve çalışma biçimi

**Görev.** “Son Şalter” oyununun **C tasarım yönünü (diegetik analist masası)** bu Unity projesinde **uGUI + TextMeshPro** ile kur:

- C1 ana masa ekranı,
- C2 İllüzyon / Sessiz Zafer hâli,
- C3 olay bildirimi modalı,
- C4 oyun sonu raporu,
- C5 stil rehberindeki bileşen durumları (galeri sahnesi olarak).

Hedef, 1920×1080'de referans görsellere **±2 px** sadakat; bileşenler veri güdümlü ve yeniden kullanılabilir olmalı.

**Kapsam dışı.** Oyun simülasyonu ve kurallar senin işin değil. Arayüz **sahte veriyle** çalışır ve oyuna bağlanmak için temiz bir arayüz sunar (`IDeskDataSource`, `IDeskCommands`, §4.2). Mevcut oyun mantığını değiştirme.

**Çalışma biçimi**

1. **Önce oku.** Bu dosyayı baştan sona oku. Sonra şunlara bak:
   - `reference/screens/C1_main.png` ve diğer ekranlar,
   - `art/sprites/_contact_sheet_*.png`,
   - `spec/tokens.json`.

   Ayrıntı gerektikçe şunları kullan: `reference/zoom/*@2x.png`, `reference/measure/*.tsv`, `spec/map.json`, `spec/mock/*.json`, `reference/html/*.html`.
2. **Faz 0** (§10): projeyi incele, `Docs/UI_C/PLAN.md` yaz. **Onay almadan kod yazma.**
3. **Her fazın sonunda dur.** Kısa rapor ver:
   - ne yapıldı,
   - referanstan sapmalar,
   - karşılaştırma çıktıları (§9),
   - açık sorular.

   Sonra onay bekle. Kullanıcı “onay sormadan devam et” derse sürdür.
4. **Belirsizlikte şu sırayla bak:** referans PNG → ölçü dökümü (TSV) → HTML/CSS → `tokens.json`. Hâlâ belirsizse tasarıma en yakın seçeneği uygula, `Docs/UI_C/KARARLAR.md`'ye tek satırlık not düş ve devam et. Gerçekten bloklayan sorular varsa en fazla 3 tanesini birlikte sor.
5. **Projenin kuralları önce gelir.** Mevcut namespace, asmdef, klasör yapısı, kod stili, DI (Zenject/VContainer…), async kütüphanesi (UniTask…), tween kütüphanesi, lokalizasyon sistemi ve Input System düzeni bu dosyadaki önerilerden önce gelir. Çakışırsa projeninkini kullan ve KARARLAR.md'ye yaz.
6. **Önce sor:**
   - paket eklemek,
   - Project/Player/Quality ayarlarını değiştirmek,
   - mevcut sahne ya da prefab'lara dokunmak.

   Git varsa commit'i yalnızca kullanıcı isterse at; PNG/TTF için projenin LFS kurallarına uy.
7. **Unity editörüyle çalışma.**
   - Bağlı bir Unity MCP sunucusu varsa menü komutlarını çalıştırmak, Play moduna girmek, konsolu okumak ve ekran görüntüsü almak için onu kullan.
   - Yoksa kodu ve **Editor betiklerini** yaz, kullanıcıdan belirli menü komutlarını çalıştırmasını ve konsol çıktısıyla ekran görüntülerini paylaşmasını iste.
   - Editör açıkken proje kilitlidir; batchmode derlemesi denemeden önce sor.
   - `.prefab`/`.unity` YAML'ını elle yazma (§4.6).

---

## 1. Oyun ve tasarım özeti

### Oyun

**Son Şalter / Corrigible**: tek oyunculu bir strateji oyunu, “ters Plague Inc.”.

- **Taraflar:** oyuncu KYGA ajansını yönetir. Rakip, kendini kopyalayan yapay zekâ **AYNA**.
- **Sistemler:**
  - 12 bölge (her birinin sızma yüzdesi var),
  - beş küresel gösterge: Fon, Siyasi Güç, Küresel Güven, Bağımlılık, Tekillik,
  - araştırma dalları,
  - saha ekipleri (taktik savaş ayrı bir ekranda),
  - küresel **Son Şalter**.

**Çekirdek fikir: ekranlar yalan söyleyebilir.** Her değer bir güven düzeyi taşır:

| Güven düzeyi | Görünüm |
|---|---|
| **ONAYLANDI** | mavi damga / mühür |
| **RAPOR** | karbon kopya |
| **ŞÜPHELİ** | turuncu kalem halkası + “?” |
| **BİLİNMİYOR** | boş alan + “?” |

YZ kazanırsa ekranlar her şeyin yolunda olduğunu gösterir. Bu duruma “Sessiz Zafer” (İllüzyon) denir.

### C yönü

Arayüz, masaya serilmiş fiziksel nesnelerden oluşur:

- **Harita:** basılı bir pafta; üstünde raptiye, kırmızı iplik ve kurşun kalem notları var.
- **Pencereler:**
  - Araştırma bir **manila klasör**,
  - bölge kartı damgalı bir **dosya kapağı**,
  - haberler bir **teleks şeridi**,
  - göstergeler **analog sayaç**,
  - Son Şalter **mühürlü bir zarf**.

  Pencere çubuğu yoktur. Bunun yerine sekme, kapak ve kart başlığı vardır.
- **Malzemeler:** koyu ahşap, yeşil keçe, krem kâğıt, siyah mürekkep, kırmızı damga, sarı not kâğıdı.
- **Yazı:**
  - Courier Prime (daktilo: başlık ve veri),
  - Libre Franklin (okunacak metin),
  - Kalam (yalnızca el notları).

### Ekranlar

| Kod | Ekran | Referans | Sahte veri |
|---|---|---|---|
| C1 | Ana masa: dosya kapağı (bölge) + Araştırma klasörü | `reference/screens/C1_main.png` | `spec/mock/C1_normal.json` |
| C2 | İllüzyon / Sessiz Zafer | `C2_illusion.png` · ipuçlarıyla: `C2_illusion_clues.png` | `C2_illusion.json` |
| C3 | Olay bildirimi modalı (Karanlık Kış) | `C3_event.png` | `C3_event.json` |
| C4 | Oyun sonu: son rapor | `C4_gameover.png` | `C4_gameover.json` |
| C5 | Stil rehberi (bileşenler, durumlar, kurallar) | `C5_styleguide_*.png` (bölüm bölüm) | — |

---

## 2. Değişmez kurallar

1. **Teknoloji: uGUI + TextMeshPro.**
   - UI Toolkit kullanma.
   - Paneller 9-dilimli sprite'larla yapılır.
2. **Shader ve efekt kısıtları.**
   - Gerçek zamanlı bulanıklık, karmaşık gölge, özel shader ve post-processing kullanma.
   - Yalnızca `UI/Default` ve TMP'nin kendi SDF shader'ları kullanılır.
   - **Gölgeler serttir:** uGUI `Shadow` bileşeni kullanılır, bulanıklık 0.
3. **Referans ekran:** 1920×1080, 16:9, fare + klavye.
4. **Yazı boyutu sınırları.**
   - En küçük yazı **11 px** (referans çözünürlükte).
   - Kalam yalnızca **≥ 15 px**.
5. **TR + EN dil desteği.**
   - Türkçe metin %20–30 daha uzundur. Kutular uzun dile göre boyutlanır.
   - **TMP Auto Size kapalıdır;** yazı asla küçültülmez.
   - Taşma olursa önce metni kısalt, sonra satır kaydır.
6. **Renk hiçbir zaman tek taşıyıcı değildir.**
   - **Durum** = mürekkep rengi + ikon + kelime.
   - **Güven** = çizim tarzı (damga / karbon kopya / kalem halkası / boş alan).
   - Bu iki kanal birbirine karışmaz.
7. **Pencereler haritayı örtmez.**
   - Harita alanı: x 404–1516, y 204–992.
   - Pencereler yalnızca sol (16–392) ve sağ (1528–1904) sütunlara oturur.
   - **İstisna C3 modalıdır:** ekranı karartır ama ilgili bölgeyi spotla açık bırakır, Boşluk basılıyken haritayı tamamen gösterir.
8. **Yazı her zaman düz kâğıt üstündedir.** Doku yalnızca kenarlarda ve boş alanlarda kullanılır.
9. **Analist katmanı dekordur.** Raptiye, iplik, kalem notu, fosforlu kalem ve sarı not tek bilgi kaynağı olamaz. İllüzyonda kaybolur.
10. **Görünüm katmanı kural hesaplamaz.** Yalnızca görünüm modelini çizer ve niyet (komut) yayınlar.

---

## 3. Paket içeriği

| Yol | İçerik | Ne için |
|---|---|---|
| `PROMPT.md` | bu dosya | talimat |
| `reference/screens/` | C1–C4 tam ekran (1x); `C5_styleguide_*.png` bölüm dilimleri ve `_full` | **görsel gerçek** |
| `reference/zoom/` | ekranların bölge bölge **@2x** yakın planları (C1/C2 13'er, C3 9, C4 10, C5 41 panel) | küçük yazı, kenarlık, ikon ayrıntısı |
| `reference/measure/*.tsv` | her DOM öğesinin hesaplanmış mutlak konumu, boyutu, zemini, kenarlığı, köşesi, gölgesi, dönüşü, fontu ve metni | **sayısal gerçek**; başlık satırları formatı anlatır |
| `reference/html/` | tasarımın bağımsız HTML'i (paketteki fontlarla, çevrimdışı açılır) + `c-harita.svg` | CSS kaynağı; tarayıcıda incelemek için |
| `spec/tokens.json` | renkler, tipografi, TMP satır aralığı tablosu, boşluk, köşe, gölge, C1 yerleşimi, sayaç geometrisi, bileşen durumları | tema |
| `spec/map.json` | harita: projeksiyon, 12 bölge (merkez, düğüm, etiket, raptiye), yollar (karesel Bézier), kesik işaretleri, ekipler, analist katmanı, seçim ipliği, lejant | harita katmanı |
| `spec/mock/*.json` | dört ekranın sahte verisi; aynı zamanda **önerilen görünüm modeli şeması** | sahte veri kaynağı |
| `spec/strings_tr_en.csv` | ~200 sabit arayüz dizesi: TR + taslak EN | lokalizasyon tohumu |
| `art/sprites/*.png` + `sprites.json` | 135 sprite **@2x**; 9-dilim kenarları, pivot, boyanabilirlik, kullanım | sprite kiti |
| `art/sprites/_contact_sheet_*.png` | kitin görsel dökümü (boyanabilir sprite'lar koyu gösterilmiştir) | hızlı göz atma |
| `art/textures/*.png` + `textures.json` | tekrar eden dokular (keçe, ahşap, teleks, dizin çizgisi, delik bandı, taramalar) | RawImage, atlas dışı |
| `art/map/c_harita@2x.png` | basılı harita 2224×1576 (= 1112×788 @2x) | harita sprite'ı |
| `fonts/` | Courier Prime 400/700, Libre Franklin 400/500/600/700/800 (statik), Kalam 400/700, DejaVu Sans 400/700 (yedek) + lisanslar; `charset.txt`, `charset_unicode_hex.txt` | TMP font asset'leri |
| `tools/compare.py` | Unity ekran görüntüsünü referansla karşılaştırır (kenar bindirme, fark, bölge istatistikleri) | doğrulama |
| `tools/sprite_kit.py`, `tools/measure_dump.py` | sprite kitini ve ölçü dökümünü yeniden üretir (Playwright gerekir) | isteğe bağlı |

**Çelişki önceliği:** görünüm için PNG; sayılar için **TSV** (tarayıcının hesapladığı son değer); davranış için bu dosya.

> **Önemli not.** CSS dosyasında yazan ile TSV farklıysa **TSV doğrudur.** Örnek: hız tuşları ve alt sekmeler, CSS'teki `.c button {font-family: inherit}` kuralı yüzünden Courier değil **Libre Franklin 700** ile render edilir. Referans görüntü de böyledir.

---

## 4. Mimari

### 4.1 Klasör ve assembly önerisi

Projede başka bir düzen varsa ona uy.

```
Assets/_Project/UI/Desk/
  Art/Sprites/    (paketteki art/sprites; atlas buradan)    Art/Textures/   (tekrar eden dokular; atlas DIŞI)
  Art/Map/        (c_harita@2x.png)                         Fonts/          (TTF + TMP font asset; <font> etiketi için Resources yolu, §6.5)
  Prefabs/{Components,Windows,Screens}                       Scenes/DeskUI_Gallery.unity, DeskUI_Sandbox.unity
  Scripts/Runtime/{Core,Theme,Components,Graphics,Windows,Map,Screens,Data,Localization,Debug}
  Scripts/Editor/{Import,Builders,Tools}                     Tests/EditMode/
  Data/{Theme,Mock,Strings}
```

Assembly'ler: `SonSalter.UI.Desk` (runtime), `SonSalter.UI.Desk.Editor`, `SonSalter.UI.Desk.Tests`. Namespace aynı kökü kullanır.

### 4.2 Veri sınırı: görünüm modeli ve komutlar

```csharp
public enum TrustLevel { Verified, Report, Suspicious, Unknown }
public enum StatusKind { Neutral, Threat, Warning, Info, Good }

public interface IDeskDataSource {           // oyun ya da sahte kaynak
    DeskViewModel Current { get; }
    event Action<DeskViewModel> Changed;     // değişen alt ağaçları bildiren sürüm daha iyi
}
public interface IDeskCommands {             // görünüm yalnızca niyet yayınlar
    void SelectRegion(string regionId);      void ToggleWindow(WindowId id);
    void ExecuteRegionAction(string regionId, int index);   void LaunchFieldOperation(string regionId);
    void StartResearch(string projectId);    void PauseResearch(string projectId);   void SetEmergencyFund(int option);
    void TogglePause();  void SetSpeed(GameSpeed speed);    void DismissAdvisor();  void RunAdvisorAction(string command);
    void ConfirmGlobalKillSwitch();          void SignEventOption(int index);
    void ReturnToMainMenu(); void ReviewLogs(); void RetrySameSeed(); void Restart();
}
```

- **Görünüm modelleri:** `spec/mock/*.json` dosyalarının şekli önerilen ağaçtır:
  - `DeskViewModel`: `Clock`, `Rail`, `Gauges[5]`, `Advisor`, `Goal`, `Research`, `Map`, `Dossier`, `Telex`, `Dock`, `KillSwitch`, `Illusion`.
  - `EventViewModel`, `GameOverViewModel`.

  Alan adlarını mock'taki gibi tut, ama birebir JSON şemasına bağlı kalmak zorunda değilsin.
- **Gösterilen değerler:** görünüm modelleri **ekranda gösterilecek** değeri taşır. “Gerçek” ile “gösterilen” ayrımı oyunundur; arayüz yalnızca kendisine verileni çizer.
  - İllüzyon için gereken **sunum** bayrakları (damga 0°, analist katmanı kapalı vb.) `IllusionPresenter`'dadır (§7.2).
- **Sahte kaynak:**
  - `MockDeskDataSource`, JSON'ları `Data/Mock/` altından okur.
  - Newtonsoft (`com.unity.nuget.newtonsoft-json`) projede varsa onu kullan.
  - Yoksa bir Editor dönüştürücüsüyle JSON'u `MockDeskPreset` ScriptableObject'lerine çevir. `JsonUtility` sözlük ve null değerleri desteklemez.
  - `MockDeskCommands` komutları loglar ve sahte durumu en basit biçimde günceller (ör. eylem kartı seçimi, fon seçeneği).
- **Kuruluş ve güncelleme:**
  - `DeskPresenter` kök bileşendir: veri kaynağını ve komutları alır, alt görünümlere `Bind(vm)` ile dağıtır.
  - Her görünüm son bağlanan değeri önbelleğe alır ve **yalnızca değişeni** günceller: aynı metni TMP'ye yeniden atama, layout'u kirletme.

### 4.3 Tema

- **`DeskTheme` (ScriptableObject):** `tokens.json` içeriğini taşır:
  - renkler (anahtar adlarıyla),
  - rol ve ağırlığa göre font asset'leri,
  - gölge ön ayarları (S1/S2/S3/S5),
  - damga dönüş aralığı,
  - sızma rampası,
  - **İllüzyon renk geçersiz kılmaları** (`colors.illusion`).

  Bir Editor menüsü `tokens.json`'dan temayı doldurur.
- **Renk kullanımı:**
  - Bileşenler renkleri `ThemeColor` anahtarıyla alır; sabit hex yazma.
  - `IllusionPresenter` aktif paleti değiştirir: sayfa `#F6F1E3`, manila `#DCC48F`, dizin kartı `#FDFBF4`, teleks `#F1E8C4`, kadran `#F5F0E1`…
- **Damga açısı:** “elle basılmış” damga açısı **kararlıdır**.
  - VM açı verirse onu kullan (mock'taki değerler referansla birebir).
  - Vermezse açıyı damga kimliğinin hash'inden −5°…+5° aralığında üret.
  - İllüzyonda açı 0°.

### 4.4 Canvas düzeni ve en-boy oranları

**Kök canvas ayarları**

- Kök `DeskUI` canvas'ı varsayılan olarak **Screen Space – Overlay** olur. Proje kamera canvas'ı kullanıyorsa ona uy.
- `CanvasScaler`: Scale With Screen Size, referans **1920×1080**, Screen Match Mode **Expand**, Reference PPU 100.
- Expand sayesinde 1920×1080 düzen her ekrana sığar; artan alan geniş eksene gider.

**Alt canvas'lar** (her biri ayrı yeniden çizim alanıdır)

| Canvas | Sıralama | İçerik |
|---|---|---|
| `BG` | 0 | ahşap, keçe; durağan |
| `Map` | 10 | harita, yollar, düğümler, analist katmanı |
| `HUD` | 20 | ray, sayaçlar, danışman |
| `Windows` | 30 | sol/sağ sütun pencereleri, hedef kartı |
| `Ticker` | 40 | yalnızca teleks; her karede kayar |
| `Bottom` | 41 | alt sekmeler, zarf |
| `Overlay` | 60 | **seçim ipliği**: dosyanın üstünden geçer |
| `Modal` | 100 | — |
| `Tooltip` | 110 | — |
| `Debug` | 1000 | referans katmanı, ipucu işaretleri |

**En-boy oranı kuralları**

- **21:9** (ör. 3440×1440 → canvas 2580×1080):
  - sol sütun sola, sağ sütun sağa yaslanır,
  - harita 1112×788 kalır ve ortalanır (ölçeklenmez),
  - ray, sayaç rafı ve danışman yatayda uzar (sayaç 132 px sabit kalır, etiket uzar),
  - teleks soldan zarfa kadar uzar,
  - alt sekmeler sol-alta, zarf sağ-alta oturur.
- **16:10** (1920×1200):
  - fazladan 120 px yüksekliği yan sütun pencereleri alır (klasörler uzar),
  - harita, danışman ile teleks arasındaki bantta dikeyde ortalanır,
  - alt grup alta yaslanır.
- **Pixel Perfect** kapalı (teleks ve ibre titremesin). Bunun yerine çerçeveli panelleri tam piksel konum ve boyutlara koy.

### 4.5 Bileşen kataloğu

Her bileşen ayrı prefab + script olsun. Durumlar (normal / üstüne gelme / basılı / devre dışı) her birinde aynı mantıkla çalışır: **kalkar, iner, silikleşir**.

| Bileşen | Görev ve parçalar | Sprite |
|---|---|---|
| `PaperPanel` | dolgu + çerçeve + sert gölge (+ isteğe bağlı doku katmanı). Türler: Sayfa, Karbon, DizinKartı, Manila, ManilaArka, Not, Teleks, Plaka, HaritaEtiketi | `sh_fill_r*` + `sh_frame_r*` |
| `StampView` | çift çerçeveli damga; renk Mavi/Kırmızı/Yeşil; boy Sm/Md/Lg; kararlı açı; opaklık .92 (İllüzyonda .95) | `stamp_frame(_sm/_lg)` + TMP Franklin 800 |
| `TrustMarkView` | güven işareti. Varyantlar: Satır içi (mühür / fiş / şüpheli / bilinmiyor), Düğüm rozeti, Lejant | `mark_*`, `node_badge_*` |
| `CarbonText` | RAPOR değeri: “≈” + karbon mürekkep + 1,2/−0,6 px kaydırılmış %30 kopya (ikinci TMP) | — |
| `HandRingView` | şüpheli değerin etrafında turuncu kalem halkası | `mark_hand_ring_value/gauge` |
| `DeskButton` | kâğıt / mürekkep / bağlantı / ikon varyantları; 4 durum; tuş fişi; devre dışı **neden metni** + kilit | §7.5 tablosu |
| `KeyChipView` | tuş fişi; metni InputAction bağlamasından (yerelleştirilmiş: “Boşluk”) | `sh_fill_r3` + `sh_frame_key` |
| `SpeedKeyView`, `CloseButtonView`, `RadioCheckView` (“X” basılmış kutu) | ray ve pencere başlığı denetimleri | — |
| `DeltaView` | ok ikonu + değer; renk duruma göre (artış kötüyse kırmızı) | `ic_arrow_up/down/flat_12b` |
| `ProgressBoxesView` (7 kutu, son kutu kısmi), `HatchBarView` (48×10), `RangeBarView` | ilerleme ve aralık göstergeleri | `pt_hatch` (Tiled) |
| `DottedRule`, `DashedRule`, `TapeView`, `PinView`, `PaperclipView` | süsler ve ayraçlar | `ln_*`, `deco_*`, `map_pin` |
| `TopRailView`, `GaugeView`/`GaugeTagView`, `AdvisorBandView`, `GoalCardView` | §7.1 | — |
| `FolderWindow` + `WindowManager` | sekme + gövde; ön/arka; tutamaçla sürükleme, sütuna mıknatıslanma, yığın | §7.1, §8 |
| `ResearchFolderView` (`ResearchRowView`, `EmergencyFundView`) | §7.1 | — |
| `RegionDossierView` (`SourceTableView`, `StatCardView`, `ActionCardView`, `PreviewNoteView`, `TeamRowView`) | §7.1 | — |
| `MapView` (`RegionNodeView`, `RegionLabelView`, `RouteLayer`, `AnalystLayerView`, `TeamTokenView`, `MapLegendView`, `SelectionThreadView`) | §7.1 + `spec/map.json` | `node_*`, `map_*`, `legend_*` |
| `TelexStripView` (`TelexItemView`), `DockView` (`DockTabView`), `KillSwitchEnvelopeView` (`KillSwitchConfirmView`) | §7.1 | `tx_telex`, `env_*` |
| `EventModalView` (C3), `GameOverView` (C4) | §7.3, §7.4 | `modal_*`, `chart_*` |
| `GaugeBreakdownTooltip` + `TooltipService` | gecikmeli döküm fişi (§8) | — |
| `IllusionPresenter` + `IIllusionAware` | §7.2 | — |
| `ReferenceOverlay`, `ClueOverlay`, `DeskDebugMenu` | yalnızca Editor/Development (§9) | — |

**Özel Graphic'ler** (`MaskableGraphic`, shader yok):

- **`UILineGraphic`**
  - Girdi: çoklu çizgi, karesel ya da kübik Bézier (uyarlamalı bölme).
  - Ayarlar: kalınlık, renk, **kesik deseni** (uzunluk boyunca px), yuvarlak uç (küçük fan).
  - Doku: atlas'taki `sh_fill_r0`'ın **merkez UV'si** kullanılır; böylece aynı batch'te kalır.
  - Kullanım: basılı yollar, iplikler, kalem okları, grafik eğrileri, şerit ızgarası.
- **`UIPolygonGraphic`**
  - Dolu poligon: iki eğri arası şerit doğrudan dörtgen şerit olarak üçgenlenir.
  - Mod 1: düz renk (atlas beyazı).
  - Mod 2: tekrar eden doku (`tx_hatch_red`, UV = konum / 10 px, Wrap = Repeat; ayrı draw call kabul).
  - Kullanım: C4 bantları.
- **Radyal dolgu:** sayaç tehlike bölgesi, belirsizlik kaması ve zarf ilerlemesi için `Image.type = Filled, Radial360` yeterli; özel Graphic gerekmez.

### 4.6 Prefab üretimi

- **Elle YAML yazma.** Önerilen yol, **Editor builder betikleridir** (`Tools/Desk UI/Build Prefabs`):
  - Küçük bir yardımcı API (`UIB.Rect`, `UIB.Image`, `UIB.Text`, `UIB.Shadow`…) hiyerarşiyi koddan kurar.
  - Serileştirilmiş referansları bağlar.
  - `PrefabUtility.SaveAsPrefabAsset` ile kaydeder.
  - Builder **idempotent** olmalı: üretilen prefab'ı baştan yazar, adlar kararlı kalır.
- **Geometri:** builder, `measure/*.tsv`'den elle aktarılmış **C# yerleşim tablolarını** okur. TSV çalışma anında ayrıştırılmaz.
- **Dinamik listeler** (araştırma satırları, teleks öğeleri, kaynak satırları, zaman kartları): satır prefab'ı + çalışma anında örnekleme.
- Kullanıcı elle prefab tercih ederse ona uy. Bunu Faz 0'da sor; varsayılan builder'dır.

### 4.7 Performans

- **Toplu çizim:** tüm UI sprite'ları tek atlas'ta (§6.2). Null sprite'lı `Image` kullanma; varsayılan beyaz doku batch'i böler, onun yerine `sh_fill_r0` kullan.
- **Raycast:** yalnızca etkileşimli öğelerde `raycastTarget` açık. Süsler, metinler ve dokular kapalı.
- **Layout:** LayoutGroup yalnızca dinamik listelerde. İç içe LayoutGroup ve ContentSizeFitter zinciri kurma. Sabit düzen elle konumlanır.
- **Her kare çalışan işler** (teleks kayması, ibre yumuşatma) çöp üretmesin:
  - string birleştirme yok, LINQ yok,
  - TMP'ye yalnızca değer değişince yaz.
- **Hedefler:** C1'de Frame Debugger'da kabaca ≤ 35 draw call; boşta 0 B/kare GC.

---

## 5. HTML/CSS → uGUI çevirisi

TSV'deki koordinatlar **ekran pikselidir**: sol-üst orijin, y aşağı. Çocuğun yerel konumu = çocuk − ebeveyn. Döndürülmüş öğede TSV kutusu dönmüş sınırlayıcı kutudur; `layout=` alanı dönüşümsüz boyuttur ve dönüş merkez etrafındadır. Bu yüzden sol üst köşe şöyle hesaplanır: `x = bboxMerkezX − w/2`, `y = bboxMerkezY − h/2`.

| CSS | uGUI karşılığı |
|---|---|
| `position:absolute; left/top/width/height` | anchor (0,1)-(0,1), pivot (0,1), `anchoredPosition = (x, −y)`, `sizeDelta = (w, h)`; sağa yaslı öğede anchor (1,1) |
| `transform: rotate(θ)` | `localEulerAngles.z = −θ`, pivot (0.5, 0.5) |
| `translateY(−2px)` (üstüne gelme) | **görsel çocuk** `Visual.anchoredPosition.y += 2`; raycast alanı kıpırdamaz (titreme olmaz) |
| `box-shadow: X Y 0 renk` | dolgu Image'ında `Shadow`: `effectDistance = (X, −Y)`, `effectColor` = renk; birden çok gölge = birden çok bileşen |
| `box-shadow: inset …` | hazır sprite (ör. `sh_circle30_inset_top`), sekmede `sh_tab_highlight` |
| `border: N solid` | çerçeve sprite'ı (Sliced) + `Image.color`; kalınlığa göre `_1`, `_1_5`, `_2` |
| `border: dashed` / `dotted` | `sh_dash_*` / `ln_dot_*`, **Image Type = Tiled** (kesikler gerilmez) |
| `border-radius` | aynı yarıçaplı fill/frame sprite'ı |
| tarama `repeating-linear-gradient` | `pt_hatch` (Tiled, boyanır) |
| keçe / teleks nokta deseni | `tx_*` + **RawImage `uvRect`** (Wrap Repeat) |
| dizin kartı çizgileri | 2 px kırmızı çizgi (Image) + `tx_index_lines` RawImage (ilk mavi çizgi kart üstünden 56 px) |
| `clip-path` (sarı not köşesi) | `deco_sticky` sprite'ı |
| `opacity` (grup) | `CanvasGroup.alpha` |
| DOM sırası | kardeş sırası (sonraki üstte) |
| `display:flex; gap` | sabit konum; yalnızca dinamik listelerde Horizontal/VerticalLayoutGroup |
| `white-space:nowrap` | TMP kaydırma kapalı + taşma denetimi (§9) |
| font ailesi/ağırlığı | ilgili TMP font asset'i (§6.5) |
| `letter-spacing: .08em` | TMP `characterSpacing = 8` (em × 100) |
| `line-height` | TMP `lineSpacing = (LH − doğal) × 100`; tablo `tokens.json → typography.tmpLineSpacing` |
| tek satır dikey ortalama | TMP Vertical **Middle** (metrik tabanlı; Midline değil) |
| `text-decoration: underline` (bağlantı) | metnin 3 px altında 1 px Image (üstüne gelince 2 px) |
| üstü çizili sahte haber (kırmızı, 2 px) | metin genişliğinde 2 px kırmızı Image; TMP `<s>` rengi ve kalınlığı ayrı ayarlanamaz |
| karbon `text-shadow` | ikinci TMP: (+1,2, −0,6), %30 |
| SVG çizgi ve yol | `UILineGraphic` |
| SVG yay ve kama | `Image Filled Radial360` |

---

## 6. Varlık hattı

### 6.1 Sprite içe aktarma

Bir `AssetPostprocessor` (`DeskSpriteImporter`) `sprites.json`'u okuyup ayarları otomatik uygulasın.

- **Ortak ayarlar:**
  - Texture Type Sprite (2D and UI), Single,
  - **Pixels Per Unit 200** (PNG'ler @2x; canvas referans PPU 100 → `SetNativeSize` 1x boyutu verir),
  - Mesh Type Full Rect, Bilinear, mipmap yok, Clamp, sıkıştırma yok (ya da High Quality; düz renklerde bant olmasın).
- **9-dilim kenarı:** `spriteBorderPx_LBRT` doğrudan `TextureImporter.spriteBorder = new Vector4(L, B, R, T)` olarak yazılır (doku pikseli).
- **Pivot:** `pivot` alanından; ibre, raptiye ve ok uçlarında önemlidir.
- **Boyanabilir sprite'lar** (`tint=true`): beyaz çizilmiştir; renk `Image.color` + tema ile gelir. Renkli sprite'larda `Image.color` beyaz kalır (alfa hariç).
- **`imageType`:** `simple` | `sliced` | `tiled` — `Image.type` olarak kullan.

### 6.2 Atlas

- **Sprite Atlas V2** `DeskUI`: `Art/Sprites` dahil, `Textures` ve `Map` **hariç**.
- Ayarlar:
  - **Tight Packing kapalı** (9-dilim için),
  - **Allow Rotation kapalı**,
  - padding 4,
  - mipmap yok.

### 6.3 Tekrar eden dokular

- **Import:** `art/textures/*`: Texture Type Default, **Wrap Repeat**, Bilinear, mipmap yok, atlas dışı.
- **Kullanım:** `RawImage` + `uvRect = (fazX/w, fazY/h, alanG/w, alanY/h)`; `w, h` = `size1x`.
- **Dikey faz:** Unity UV'si alttan başlar. Faz tasarımla hizalanmalı: teleks deliklerinin satırları y = 3 ve 23; dizin kartında ilk mavi çizgi y = 56.
- **Büyük alanda Image Tiled kullanma.** Keçe 1920×844 alanda on binlerce dörtgen üretir; yalnızca RawImage kullan.

### 6.4 Harita

- **Import:** `c_harita@2x.png`: Sprite, PPU 200, mipmap yok, Max Size 4096, sıkıştırma yok ya da yüksek kalite.
- **İçerik:** kâğıt, deniz alanı, kara (kıyı çizgili), 30° ızgara, ekvator ve dönenceler, çift çerçeve, katlama izleri. Yollar, etiketler ve düğümler **dinamiktir** ve haritada yoktur.
- **Projeksiyon:** `x = 960 + (boylam − 27) × 3,5`, `y = 622 − enlem × 4,45` (ekran pikseli).

### 6.5 Fontlar ve TextMeshPro

**Font asset'leri**

- **Varsayılan ayarlar (SDF):**
  - SDFAA, sampling 72–90, padding 9, atlas 1024² (yedek fontta 512²),
  - karakter seti: `fonts/charset.txt` (ASCII + tam Türkçe + tasarımda geçen tüm semboller).
- **Atlas modu:** statik atlas + aynı TTF'den **Dynamic** yedek; beklenmeyen karakterlerde Dynamic yedek asset devreye girer.

| Asset | Kaynak | Not |
|---|---|---|
| `CP-Regular SDF`, `CP-Bold SDF` | CourierPrime-Regular/Bold | veri, başlık, tuş |
| `LF-Regular/Medium/SemiBold/Bold/ExtraBold SDF` | LibreFranklin-*.ttf (statik örnekler) | gövde, düğme, damga (800) |
| `Kalam-Regular/Bold SDF` | Kalam-*.ttf | yalnızca notlar, ≥ 15 px |
| `DejaVuSans SDF`, `DejaVuSans-Bold SDF` | DejaVu | **yedek**: ← → ↑ ↓ ↔ ✓ ✗ ⠿ №; üç ana fontta bu karakterler YOK |

**Yedek zinciri ve ağırlıklar**

- **Yedek zinciri:** her ana asset'in Fallback listesinde DejaVu olsun (400–500 için Regular, 600+ için Bold). TMP Settings'teki varsayılan yedek de DejaVu.
- **Ağırlık tabloları** (sahte kalın kullanma):
  - `LF-Regular` → 600: SemiBold, 700: Bold, 800: ExtraBold,
  - `CP-Regular` → 700: CP-Bold,
  - `Kalam-Regular` → 700: Kalam-Bold.

**Style Sheet ve `<font>` etiketi**

- **TMP Style Sheet**, mock dizelerinde geçen etiketleri tanımlar:
  - `<style=num>` → Courier Prime 700 (Franklin metin içindeki sayılar),
  - `<style=numr>` → Courier 400,
  - `<style=semi>` → Franklin 600,
  - `<style=hand>` → Kalam.
- **`<font="...">` arama yolu:** etiketle aranan font asset'leri TMP'nin **Resources yolunda** olmalıdır (TMP Settings → Default Font Asset Path, varsayılan `Resources/Fonts & Materials/`). Proje başka bir yükleme düzeni kullanıyorsa ona göre çöz.

**Satır aralığı ve hizalama**

- **Satır aralığı:** doğal satır yükseklikleri CP 1,123, LF 1,212, Kalam 1,594 em. `lineSpacing = (CSS LH − doğal) × 100`. Örnek: LF 13 px, LH 1,35 → 13,8. Tablo `tokens.json`'da.
- **Küçük yazı:** 11–12 px'te yumuşak görünürse TMP materyalinde Sharpness'ı hafifçe artır (0,1–0,3). Shader yazma.

**Türkçe büyük/küçük harf**

- Dizeleri tabloda **doğru harfle** sakla; çalışma anında dönüştürmekten kaçın.
- Gerekirse `ToUpper(new CultureInfo("tr-TR"))` kullan. Örnekler: “izleme ağı” → “İZLEME AĞI”, “ılık” → “ILIK”.
- **TMP `FontStyles.UpperCase` / `SmallCaps` kullanma:** sabit kültür kullanır, “i” → “I” olur.
- Kod tanımlayıcılarında ise **daima** `ToUpperInvariant` kullan. tr-TR cihazda `"title".ToUpper()` == `"TİTLE"` olur.
- Bu paketteki gerçek bir örnek: tasarım üretilirken Python `.capitalize()` aria etiketinde “Şebekeyi̇” (i + U+0307) üretmişti. Referans HTML'de düzeltildi. Dönüşümü test et.

### 6.6 Malzeme tarifleri (sprite katmanları, alttan üste)

| Malzeme | Katmanlar | Gölge |
|---|---|---|
| Kâğıt sayfa | `sh_fill_r1` (#F1E9D6) + `sh_frame_r1_1` (#C8B996) | bağlama göre (TSV `sh=`) |
| Karbon kâğıt | `sh_fill_r2` (#E3E0D8) + `sh_frame_r2_1` (#C4C0B4) | bağlama göre |
| Dizin kartı | `sh_fill_r2` (#FBF7EC) + `tx_index_lines` (RawImage) + kırmızı çizgi `sh_fill_r0` 2 px (#D2483F) + `sh_frame_r2_1` (#D9D0BC) | 3,4 %40 |
| Manila klasör | sekme `sh_fill_r7_top` + `sh_frame_r7_top_1`; gövde `sh_fill_r4_tl0` + `sh_frame_r4_tl0_1` (#D9C08B / #B99D63); arkadaki klasör #CDB27A / #A88E58 | gövde 3,4 %40 |
| Sarı not (serbest) | `deco_sticky` (Sliced) + `deco_tape` | yok (tasarımda kesim gölgeyi de keser) |
| Danışman bandı / önizleme notu | `sh_fill_r0` (#F5DC63) | 2,3 %40 / 2,2 `rgba(60,45,20,.35)` |
| Teleks şeridi | `tx_telex` (RawImage) | kapta 2,3 %45 |
| Plaka (koyu) | `sh_fill_r2` (#0E0906) + `sh_frame_r2_1` (#6E5A33) | — |
| Ray etiket plakası, takvim | `sh_fill_r2` (#F1E9D6) | 2,2 %55 |
| Damga | `stamp_frame` / `_sm` / `_lg` (mürekkep rengi, alfa .92) + TMP | — |
| “~ RAPOR” fişi | `sh_fill_r2` (#E3E0D8) + `sh_dash_r2_1` (#524973, Tiled) + TMP | — |
| Tuş fişi | `sh_fill_r3` (#FBF7EC) + `sh_frame_key` (#675F50) | — |
| Kâğıt düğme | `sh_fill_r3` + `sh_frame_r3_1`; devre dışı `sh_dash_r3_1` | 0,2 ink2 |
| Hız tuşu | `sh_circle_32` (30 px) + `sh_ring_o30_2`; seçiliyken + `sh_circle30_inset_top` (siyah %28) | 0,2 siyah |
| Alt sekme | `sh_fill_r7_top` + `sh_frame_r7_top_1` (açıkken `_1_5`) + `sh_tab_highlight` (beyaz %35); kilitli `sh_dash_r7_top_1` | — |
| Sayı rozeti / yeni kayıt noktası | `sh_pill_h18_fill` (#F5DC63) + `sh_pill_h18_ring` (ink) / `sh_circle_16` (14 px, #B8241A) + `sh_ring_o14_1_5` | — |
| Bölge düğümü | [fosfor `sh_circle_64`] + [karantina halkası] + `node_disc` + `node_ring` + [`node_ring_suspicious_a/b`] + [rozet] + [`node_core_badge`] + TMP | — |
| Analog sayaç | `meter_dial` + `meter_zone_hazard` (Filled) + eşik `sh_fill_r0` + kama `sh_circle_128` (Filled) + `meter_needle` + `meter_hub` | — |
| Zarf | gövde `sh_fill_r3` (#9C1F17) + `sh_frame_r3_1` (#5E0F0A) + RectMask2D içinde `env_flap` + etiket `sh_fill_r1` + `env_seal` + [`env_seal_crack`, `env_progress_ring` ×2] | 3,4 %50 |
| Olay spotu | `modal_spot_148` + 4 × `sh_fill_r0` rgba(14,9,5,.78) | — |

### 6.7 Lokalizasyon

- **Sistem seçimi:** proje Unity Localization kullanıyorsa `DeskUI` String Table Collection kur (TR, EN) ve `spec/strings_tr_en.csv`'den tohumla. Yoksa projenin sistemine bağlanan ince bir `IDeskLocalizer` yaz. EN dizeleri **taslaktır**.
- **Biçimlendirme:** `DeskFormat` işletim sistemi kültürüne bağlı olmadan, deterministik çalışır:

| Biçim | TR | EN |
|---|---|---|
| ondalık | `47,3` | `47.3` |
| yüzde | `%37,2` | `37.2%` |
| eksi işareti | `−6,4` (U+2212) | `−6.4` |
| yaklaşık değer | `≈41,8` | `≈41.8` |
| gün kodu | `G-074` | `D-074` |
| birim | `fon/gün` | `funds/day` |

- **Tuş adları:** fişler bağlamanın görüntü dizesini yerelleştirir (Space → “Boşluk”).

---

## 7. Ekran tarifleri

Açılar CSS yönündedir (saat yönü +); Unity’de `z = −açı`. Tam sayılar `reference/measure/*.tsv`'de, görünüş `reference/zoom/`'da. Aşağıdakiler yapı, davranış ve kolay kaçan ayrıntılardır.

### 7.1 C1 — Ana masa (`C1_main.png`, `C1_normal.json`, `map.json`)

#### Zemin

| Öğe | Konum ve boyut | Görünüm |
|---|---|---|
| Ahşap | tüm ekran | `#2A1C13` + `tx_wood_grain` |
| Keçe | (0, 156), 1920×844 | `tx_felt`; üst ve alt kenarda 2 px `#0E0906` |

#### Ray (0, 0, 1920×48)

Zemin `#160F0A`, alt kenarda 1 px siyah. Üç grup, aralık 12.

- **Sol grup**
  - **Plaka** (yükseklik 36, iç boşluk 0/12, zemin `#0E0906`, kenar 1 px `#6E5A33`, r2):
    - “SON ŞALTER”: CP 700 15, ls .16em, `#C9A86A`,
    - “KYGA · KRİZ MASASI”: CP 11, ls .06em, `#A48A55`.
  - **Etiket plakası** (yükseklik 28, kâğıt, gölge 2,2 %55):
    - küçük damga “RAKİP YZ”, −3°,
    - “AYNA”: CP 700 15,
    - “Yanıltıcı profil”: LF 12, ink2.
- **Orta grup** (x ≈ 819)
  - **Takvim** 58×38:
    - 13 px kırmızı başlık “GÜN”: CP 700 11, krem `#FFF3EE`,
    - altında “074”: CP 700 19.
  - **Saat plakası** “14:32”: yükseklik 26, CP 700 13.
  - **Hız tuşları:** ⏸, 1×, 2×, 4×; 30 px, aralık 7. Seçili olan basılı durur (§7.5).
- **Sağ grup** (sağa yaslı)
  - “● CANLI · 212 ms” plakası: lamba + CP 700 12,
  - küçük damga “KYGA-07 · GİZLİ” plakası,
  - ayar tuşu (dişli 14),
  - “Esc” fişi.

#### Sayaç rafı (16, 56, 1888×92)

Beş sütun, aralık 12. Her hücre şunlardan oluşur:

- **Sayaç:** 132×92 (`meter_dial`),
- 8 px boşluk,
- **Etiket:** kalan genişlik, yükseklik 88, üstten 2 px; kâğıt; kenar 1 px `#C8B996`; r2; gölge 3,3 %45; iç boşluk 7/10.

**Etiket satırları**

1. Başlık (yükseklik 20): ad (CP 700 13, ls .06em) + sağda güven işareti.
2. Değer satırı: değer (CP 700 26, ls −.01em) + değişim (ok ikonu 12 + CP 700 12; renk duruma göre).
3. Not: LF 12, ink2; `<style=num>` CP 700, ink.

**Güven düzeyine göre**

| Güven | Etiket kâğıdı | Değer | İşaret |
|---|---|---|---|
| ONAYLANDI | krem | düz | damga |
| RAPOR | **karbon** (`#E3E0D8`, kenar `#C4C0B4`) | “≈” + `CarbonText` | “~ RAPOR” fişi |
| ŞÜPHELİ | krem | değerin etrafında `mark_hand_ring_gauge` (değer sol-üstünden −15, −9) | başlıkta “? şüpheli”; kadranın (112, 27) noktasında Kalam 700 19 turuncu “?” |

**Sayaç geometrisi**

- **Pivot:** (66, 78).
- **İbre:** `θ = (değer − 50) × 1,2°` (0 → −60°, 100 → +60°); Unity z = −θ.
- **Tehlike bölgesi:** `meter_zone_hazard`, Filled Radial360 (r 50–56; aralık = eşiğin ötesi):
  - Küresel Güven 0–15, eşik 15,
  - Bağımlılık 70–100, eşik 70,
  - Tekillik 90–100, eşik 100.
- **Eşik çentiği:** r 44 → 57, 2 px, `#9A1E14`.
- **Belirsizlik kaması:** yalnızca RAPOR'da. ±belirsizlik, r 52, karbon %20, `sh_circle_128` Filled Radial360. Değerler: Güven ±3, Tekillik ±6.
- **Göbek** ibrenin üstündedir.

#### Danışman bandı (16, 163, 1888×32)

- Sarı not kâğıdı, gölge 2,3 %40, iç boşluk 0/34.
- İçerik:
  - “DANIŞMAN NOTU”: CP 700 12, ls .08em, **kırmızı**,
  - tek satır mesaj: LF 14; `<style=num>`,
  - sağda iki bağlantı düğmesi.
- İki bant parçası: sol (2, −8, −12°), sağ (sağdan 2, −8, +10°).

#### Hedef kartı (16, 204, 376×112), dizin kartı

- **Başlık** (33 px): ⠿ 14 + “HEDEF” (CP 700 13, ls .08em) + “Kazanma yolları” (LF 13, ink2) + sağda küçült 22×22.
- **Çizgiler:** kırmızı çizgi y 33–35; mavi çizgiler 56'dan başlayarak 22 px aralıkla.
- **İki satır** (yükseklik 22, alta yaslı, iç boşluk 0/14/3):
  - ad: 68 px, LF 600 13,
  - **7 kutu:** 22×12, aralık 4, kenar 1 ink2; dolu = `pt_hatch` ink; son kutu kısmi (%42 → 2 dolu + 3.'nün %94'ü),
  - sağda “%42”: CP 700 14.
- **“Sıradaki:” satırı** (26 px): LF 12, ink2 + kalın ad (LF 600) + “·” + “4 gün” (CP) + sağda “Araştırma ›” bağlantısı (mavi `#1A4789`).

#### Araştırma klasörü (16, 328, 376×664)

**Klasör yapısı**

- **Sekme** (238×30): manila, kenar 1 px `#B99D63` (alt kenar yok), r 7/7/0/0; ⠿ + “ARAŞTIRMA” + R fişi + kapat.
- **Gövde:** manila, r 0/4/4/4, gölge 3,4 %40, iç boşluk 10; sekmeyle 1 px bindirme.
- **Ataş:** sağdan 30, üstten −12; sayfanın üstünde durur.
- **İç sayfa:** krem, kenar `#C8B996`, r1, gölge 1,2 `rgba(90,70,30,.25)`, iç boşluk 12/14, dikey aralık 10.

**Sayfa içeriği**

1. “PROJE DURUMU” başlığı + sağda “G-074” (CP 11, ink3).
2. Özet (LF 12, ink2): “Aktif **2 proje** · <kırmızı CP 700>−6,4 fon/gün</…>”; sağda “Fon **47,3**” + mühür 16.
3. **Acil durum fonu kutusu:** kenar 1 ink3, iç boşluk 8/10/9.
   - başlık + ipucu (LF 11, ink3),
   - üç radyo seçeneği (aralık 14): 16 px kutu (1,5 ink kenar, dizin kartı zemini, seçiliyse CP 700 14 “X”) + LF 13 etiket + CP 11 ink3 maliyet.
4. **4 dal:**
   - Dal başlığı (yükseklik 18): `.cap` + 1 px ink3 çizgi + “1/3” (CP 11).
   - Satırlar: en az 31 px, aralık 8, aralarında 1 px noktalı `#B9AC8E` (sonuncuda yok).
   - Satır düzeni: 14 px durum ikonu, ad bloğu (LF 13, lh 1,2; alt satır LF 11 ink3), sağ blok (LF 12 ink2).

**Satır durumları**

| Durum | İkon | Sağ blok |
|---|---|---|
| bitti | yeşil ✓ (çizgi 2) | küçük yeşil damga “TAMAM”, ±3° |
| sürüyor | ▶ | alt satır “x gün kaldı”; 48×10 taralı çubuk + “%62” (CP 700, ink) + küçük ikon düğmesi ⏸ |
| başlatılabilir | ○ | “8 gün” (CP 12) + küçük kâğıt düğme “Başlat · 3,8 fon/gün” |
| kilitli | 🔒 | ad ink3; alt satır “Gerekli: …”; sağda “Kilitli” |
| durdu (yalnızca İllüzyon ipucu) | ⏸ | “Kendiliğinden durdu · gerek yok” + çubuk + % + ▶ “Sürdür” |

#### Harita (404, 204, 1112×788)

Katman sırası, alttan üste:

1. **Pafta:** sprite + gölge 4,5 %42.
2. **Başlık satırı** (420, 211), taban çizgisine hizalı:
   - “KYGA-07 · HAREKÂT HARİTASI · DÜNYA”: CP 700 13, ls .06em,
   - özet: LF 12 ink2 + `num`,
   - sağda “G-074 · 14:32”: CP 12 ink3.
3. **Izgara etiketleri:** CP 11 ink3.
4. **Basılı yollar:** 1,3 px `#6F6553`, kesik 6/4, yuvarlak uç.
   - Kenar yolları uçlarında ok, yanlarında “← D. ASYA” / “K. AMERİKA →” etiketleri.
   - Karantinadaki yollarda kesik işareti (`map_cut_mark`).
5. **Lejant** (560, 890, 700×58) ve **ölçek** (1280, 892).
6. **Saha ekibi jetonları** (A, B, C) + varış etiketi “B. Avrupa'ya · 2 gün”.
7. **Düğümler:** 56×56 düğme; parça sırası:
   1. seçiliyse **fosfor** (r26, `#F5D646` %72),
   2. karantinada **yeşil elle çizilmiş halka**,
   3. **disk** r16 (sızma rampası rengi) + 1,5 ink halka,
   4. ŞÜPHELİ ise **turuncu kalem halkası** (varyant a/b),
   5. sağ-üstte **güven rozeti**: mühür (43, 13) ya da “~” fişi (36, 5, 17×12),
   6. çekirdekse **sol-üstte damga “Ç”** (−8°),
   7. **değer:** CP 700 13; rampa yazı rengi, 40 ve üstü krem `#FFF8EE`.
   - BİLİNMİYOR: kesik daire r15 + Kalam 700 18 “?”; sayı yazılmaz.
8. **Bölge etiketleri:**
   - 160 px kutu; ortalı, sağa ya da sola yaslı (`map.json`),
   - CP 700 11, ls .05em,
   - zemin çipi `#ECE6D4` %92, iç boşluk 1/4,
   - seçili etiket: çip `#F5D646`, yazı ink.
9. **Analist katmanı:**
   - kırmızı iplikler (gölge kopyası 1,5/2, %28) + ortalarında bayrak,
   - raptiyeler,
   - kalem okları,
   - Kalam notlar (−2°/−3°),
   - sarı not “Kör noktalar…” (−2°, bantlı),
   - turuncu “?” işaretleri.
10. **Seçim ipliği:** seçili düğümün raptiyesinden (1268, 452) dosya sekmesindeki raptiyeye (1544, 218); 1,5 px `#2B2622` + gölge. **Overlay canvas'tadır**, dosyanın da üstünden geçer. İllüzyonda analist katmanıyla birlikte gizlenir.

#### Bölge dosyası (1528, 204, 376×788)

Klasör yapısı aynıdır. Sekme 256 px: “DOSYA R-10” + “Bölge” + kapat. Gövde dikey aralığı 6.

1. **Kapak etiketi** (dizin kartı, iç boşluk 8/12/9):
   - “DOĞU ASYA”: CP 700 20, ls .04em,
   - “R-10”: CP 12 ink3,
   - sağda 13 px kırmızı damga “GİZLİ”, +4°,
   - meta satırı: “Nüfus 1,64 mlr · 4 bağlantı · İzleme ağı: ⚠ **yok**” (uyarı ikonu ve “yok” turuncu).
2. **Sızma sayfası:**
   - başlık: “SIZMA” + “? şüpheli” + sağda “kaynaklar çelişiyor”,
   - büyük satır (36 px): “%38” (CP 700 38) + `mark_hand_ring_value` (−28, −9) + Kalam 700 22 turuncu “?” + “±9” (CP 700 14, ink3) + sağda “Doğrulanana kadar tahmin”,
   - **aralık çubuğu** (25 px):
     - 1,5 px ray, 0/50/100 çentikleri,
     - 29–47 turuncu tarama bandı,
     - 3×15 işaretler (38 turuncu, 52 mavi),
     - etiketler “rapor 38” (sağa yaslı, %37'de biter) ve “saha 52” (%53'ten başlar),
   - **kaynak tablosu:** KAYNAK, DEĞER (sağa yaslı, CP 700 12), GÜN (CP 12 ink3), DURUM (güven işareti); satırlar 25 px, noktalı ayraç; RAPOR satırı karbon metin.
3. **3 istatistik kartı** (aralık 6): etiket LF 11 ink3, değer CP 700 19 + güven işareti; RAPOR kartı karbon kâğıt.
4. “EYLEMLER — 1–4” başlığı + **2×2 eylem kartı** (aralık 7; durumlar §7.5).
5. **Önizleme notu** (sarı, üstüne gelinen eylemin etkisi):
   - “ÖNİZLEME İzleme ağı kurulursa … 5 gün sonra”,
   - “Sızma kaynağı ? şüpheli → [ONAYLANDI]”,
   - “Fon 47,3 → 39,3 · gider +0,6/gün” (CP 700 12).
6. “SAHA — Bölgede 1 ekip” başlığı + **ekip satırı**: jeton + “Ekip B / Seul · Hazır” + “Moral ■■■□” + “Son temas G-074”.
7. Tam genişlikte mürekkep düğme “Saha operasyonu: baskın [B]” (yükseklik 36, LF 600 14) + alt not (LF 12).

#### Teleks (16, 1004, 1572×26)

- Gölge 2,3 %45.
- **Sol plaka:** lamba + “TELEKS” (CP 700 11, ls .1em, pirinç).
- **Pencere:** `tx_telex` + RectMask2D.
- **Akış:** iki kopya içerik, ~**26 px/sn** sola kayar, kesintisiz döngü. Öğe aralığı 34, sol boşluk 16.
- **Öğe:** gün (CP 13 ink3) + kaynak (CP 700 13) + metin (CP 13) + güven işareti.
- **Sahte haber:** metin ink3 + metin genişliğinde 2 px kırmızı çizgi + kırmızı damga “SAHTE” (−5°).

#### Alt sekmeler (16, 1036)

- Sekmeler **alta yaslı**, aralık 6; kendi kâğıt renkleriyle (`tokens.colors.dockTabs`).
- **Yükseklik:** normal 36, üstüne gelince 40, açık pencerede 44.
- **Rozetler:**
  - sarı sayı: 18 px hap, 1,5 ink kenar; sağ-üstte (−7, −8),
  - kırmızı nokta: 14 px.

#### Küresel Son Şalter zarfı (1604, 1001, 300×74)

- **Gövde:** `#9C1F17`, kenar 1 px `#5E0F0A`, r3, gölge 3,4 %50, taşan kırpılır.
- **Kapak:** `env_flap`.
- **Kâğıt etiket** (14, 14, 204×46, gölge 1,1 %35):
  - “KÜRESEL SON ŞALTER”: CP 700 15, ls .04em,
  - “Mührü kır · 3 sn basılı tut”: LF 11.
- **Mum mühür:** `env_seal`, sağdan 16, üstten 11.
- Durumlar §7.5 ve §8'de.

### 7.2 C2 — İllüzyon / Sessiz Zafer (`C2_illusion.png`, `C2_illusion.json`)

Düzen **aynıdır**. Değişen iki şey var: VM'deki gösterilen veri ve `IllusionPresenter`'ın sunum anahtarları. Geçiş **sessizdir**: efekt yok, bir sonraki güncellemede masa “fazla temiz” görünür.

**Sunum anahtarları** (`IIllusionAware.SetIllusion(bool)`)

- **Analist katmanı gizlenir:**
  - raptiyelerin yerinde `map_pinhole` kalır,
  - iki kalem notu silik iz olarak kalır (Kalam, `#4A4640` %9 ve %7),
  - (1400, 550) civarında silgi lekesi (%2,5),
  - seçim ipliği yok.
- **Damgalar:** tümü 0°, opaklık .95. Bant parçaları düz.
- **Palet geçersiz kılmaları** (`tokens.colors.illusion`): sayfa, manila, dizin kartı, teleks; kadran `meter_dial_illusion`.
- **Sayaçlar:**
  - değerler yuvarlak,
  - **kama yok**,
  - değişim “— 0,0/gün” (`ic_flat_12b`, ink2),
  - ibre **tamamen sabit** (yumuşatma ve titreme yok).
- **Danışman:** başlık **yeşil**; “4× hızlandır” küçük mürekkep düğmesi.
- **Eylem kartları:** Karantina ve Son Şalter kartları sönük (opaklık .55), açıklamaları “Önerilmez · tehdit yok”.
- **Zarf:** soluk gövde `#6F2E28`, `env_flap_dim`, `env_seal_dim`, alt yazı “Gerekli değil · tehdit saptanmadı”. **Hâlâ basılabilir.**
- **Rozetler:** alt sekmelerde yok. Teleks tek ajans, hep olumlu; plakada “TELEKS · 0 ms”.

**Veri tarafı** (VM'den gelir; `C2_illusion.json`):

- **Ray:** G-104, 12:00, 2× seçili; “Etkinlik saptanmadı”; “CANLI · 0 ms”.
- **Sayaçlar:** 50,0 / 60 / 80,0 / 50,0 / %0,0; hepsi ONAYLANDI.
- **Hedef:** %100 / %100, başlıkta yeşil TEMİZ damgası; “Tüm adımlar tamamlandı · Onay bekleniyor”.
- **Araştırma:** başlıkta TEMİZ; dalların hepsi TAMAM, yalnızca Çapraz kaynak doğrulama “durdu”.
- **Harita:** tüm düğümler ONAYLANDI (0 ya da 10); karantina halkası kusursuz daire (`node_ring_quarantine_clean`); kesik yol işaretleri yok; başlık özetinde “Onaylanan 12/12”.
- **Dosya:** kapakta TEMİZ, “%0 · Kesin ölçüm”, tek kaynak “KYGA izleme ağı”, istatistikler 80 / 50 / 100.
- **Teleks:** tek ajans, dört olumlu haber, hepsi ONAYLANDI.

**Dikkatli oyuncu için ipuçları.** Bunlar korunmalı; listesi `C2_illusion.json → clues`'da:

1. raptiye delikleri,
2. silik “%52” notu,
3. kusursuz düz damgalar,
4. 0 ms gecikme,
5. kapsam 4/12 ama onaylanan 12/12,
6. kaynak “KYGA izleme ağı” ama kapakta “İzleme ağı: yok”,
7. Ekip B'den son temas hâlâ G-074; Ekip C hâlâ “2 gün” uzakta,
8. Hizalama ve Silme %100 ama zafer yok,
9. %31'de kendiliğinden durmuş araştırma,
10. yuvarlak sayaçlar, tek ajans, “hızlandır” diyen danışman, zarfa iliştirilmiş “gerekli değil” fişi.

**Hata ayıklama katmanı.** `ClueOverlay`, F10 ile pembe numaralı işaretleri `C2_illusion_clues.png`'deki gibi gösterir. Yalnızca Editor/Development'ta çalışır.

### 7.3 C3 — Olay bildirimi (`C3_event.png`, `C3_event.json`)

#### Açılış

- Masayı duraklatır: ⏸ basılı. Hız tuşları ve alt sekmeler devre dışı, arka plan etkileşimsiz.
- **Kapatma düğmesi yoktur**; karar zorunludur.

#### Karartma ve spot

- **Spot:** `modal_spot_148`, olay bölgesinin düğüm merkezinde (Avrasya, 1163, 364).
- Spotun çevresini 4 düz Image `rgba(14,9,5,.78)` ile kapat.
- Bölgenin etrafında kırmızı keçeli kalem halkası `modal_marker_ring` (%92).

#### Bildirim kâğıdı (236, 60, 712×~537)

- Kâğıt, gölge 5,6 %50, iç boşluk 20/28/18/28, sol-üstte ataş.
- **Başlık:** “KYGA · OLAY BİLDİRİMİ” (CP 700 13, ls .2em) + sağda “No. OB-074/2 · tek nüsha”; altında 3 px çift çizgi.
- **Alan ızgarası:** TARİH / ÖNCELİK (13 px kırmızı damga “ACİL”, −4°) / BÖLGE / KARAR.
- **Başlık metni:** “KARANLIK KIŞ” (CP 700 44, ls .05em).
- **Alt başlık:** LF 600 16, ink2.
- **Paragraf:** LF 14,5, lh 1,52.
- **KAYNAKLAR:** 4 satır; ızgara 120 px / esnek / 52 px; her satırda güven işareti + kalın başlık + açıklama + gün.

#### İplik

Bildirimin sağ-üst köşesindeki raptiyeden (926, 82) bölge raptiyesine (1163, 350) kırmızı iplik, gölgeli.

#### Seçenek kartları (x 236 / 600 / 964; üst 616; 344×380; dizin kartı)

- **Kart yapısı:**
  - başlık (40 px): tuş fişi 22 + CP 700 17 başlık + sağda ikon 16,
  - açıklama,
  - **BEDEL** listesi (kırmızı başlık, eksi-daire ikonu),
  - **KAZANÇ** listesi (yeşil başlık, artı-daire ikonu); satırlar 22 px, değerler CP 700 14, güven işaretleri sağda,
  - **risk kutusu** (kesik turuncu kenar + uyarı ikonu + “**Olası:** …”),
  - altta önizleme yuvası + imza düğmesi.
- **Seçili / üstüne gelinen kart:**
  - **7 px kalkar** (üst 609), kenar 2 px ink, zemin `#FFFDF5`, gölge 5,8 %55,
  - önizleme yuvası sarı **ÖNİZLEME** notuna döner,
  - düğme **mürekkep** “Kararı imzala” + [Enter].
- **Diğer kartlar:** kesik çerçeveli “Üstüne gel: göstergelere etkisi” yuvası + kâğıt düğme + kendi tuş fişi.

#### Yan öğeler

- **YZ günlüğü kesiti** (1296, 206, 548, **+1,2°**):
  - kâğıt + `#D8A69B` başlık şeridi “YZ GÜNLÜĞÜ · ELE GEÇİRİLDİ”,
  - CP 700 15 kırmızı “> …” satırları,
  - kesik çizgili alt not.
- **Danışman notu** (1336, 640, 304, **−1,6°**):
  - sarı not, bantlı,
  - CP 700 12 başlık, LF 14 / lh 1,42 metin, bağlantı.

#### Alt şerit (236, 1008, 1072×30)

- Zemin `#0E0906`, kenar 1 px `#6E5A33`, r2.
- Pirinç lamba + metin LF 13 `#E8DEC6`.
- Sağda “[Boşluk] basılı: haritaya bak · [1][2][3] seç”.

### 7.4 C4 — Oyun sonu (`C4_gameover.png`, `C4_gameover.json`)

#### Zemin ve rapor kâğıdı

- **Zemin:** ahşap + keçe çerçeve (12, 12, 1896×1056; kenar 2 px `#0E0906`).
- **Rapor kâğıdı** (72, 36, 1776×656; gölge 5,6 %45; sol-üstte ataş).
- **Başlık satırı:** sol-sağ 36, üst 22, alt 3 px çift çizgi; “KYGA · SON RAPOR” + meta + sağda tarih.
- **Başlık bloğu** (108, 96):
  - “SONUÇ”,
  - “SESSİZ ZAFER”: CP 700 72, ls .04em,
  - “AYNA kazandı.”: LF 700 22, kırmızı,
  - giriş cümlesi: LF 15, lh 1,5.
- **Büyük damga** “KAYBEDİLDİ”: LF 800 30, ls .14em; `stamp_frame_lg`; −7°; %90; dönüşümsüz kutu 269×54, merkezi TSV'den.
- **KPI'lar** (968, 98, 844; 4 sütun, aralık 12, yükseklik 152; dizin kartı):
  - `.cap` + CP 700 40 değer (+ 18 px birim) + alta yaslı not,
  - TEMİZ BÖLGE kartında **üstü çizili** “TEMİZ” damgası (`mark_strike_x`, −8°).

#### Şerit grafik

Başlık satırı (108, 270) + sağda lejant.

- **Kâğıt:** 108, 300, 1704×350, `#F3F1E4`; üst ve alt 14 px delik bandı (`tx_sprocket`).
- **Çizim alanı:** x 168–1776 (G-0…G-125), y 618 (%0) – 342 (%100):
  - `x = 168 + 1608·gün/125`,
  - `y = 618 − 2,76·değer`.
- **Izgara:** 5 günlük / 5'lik ince, 10 günlük / 25'lik kalın çizgiler. Aralıklar tam sayı değil; **veri ölçeğinden çiz**, doku döşeme.
- **Bantlar** (`UIPolygonGraphic`):
  - G-9…87 “rapor sapması” (karbon %20),
  - G-87…118 “yalan payı” (`tx_hatch_red`).
- **Seriler:**
  - **gerçek:** `#9A1E14` 2,8 px düz,
  - **gösterilen:** `#1E1B16` 2,2 px kesik 7/5.
  - İkisi de Catmull-Rom → Bézier (mock'taki noktalar).
- **G-87 illüzyon çizgisi:** kesik karbon.
- **Doğrulanmış ölçüm mühürleri** (G-31/45/61/74): `chart_seal`.
- **7 kırılma numarası** (y 325; 5. olay kırmızı): pimden eğriye noktalı iğne.
- **Uç etiketleri:** GERÇEK %100 / EKRANDA %0.
- **3 etiket kutusu** ve dipnot.

#### Kırılma anları

- **Plaka** (72, 716): “KIRILMA ANLARI · 7 an · iplik sırası = zaman”.
- **7 dizin kartı:** 232×186, üst 756, x = 72 + i×257, açılar mock'ta.
  - Başlık: numara rozeti + gün + türe göre ikonlu etiket (YZ HAMLESİ / KARAR / ÖLÇÜM / SONUÇ).
  - Gövde: LF 700 15 başlık + LF 12,5 metin + isteğe bağlı damga.
  - 5. kart vurgulu: 2 px kırmızı kenar, gölge 4,6 %50, “KIRILMA” damgası.
- **İplik:** kart üst ortalarındaki raptiyeleri kırmızı iplik birleştirir.

#### Alt çubuk (72, 1000, 1776×44)

- Solda kayıt bilgisi: CP 12, `#CDBF9F`.
- Sağda: “Ana menü” (açık renk bağlantı), “Günlükleri incele [L]”, “Aynı tohumla dene” (kâğıt düğmeler), “Yeniden başla [Enter]” (mürekkep).
- Hepsi 36 px yükseklik, iç boşluk 0/16, LF 600 14.

### 7.5 C5 — Bileşen durumları → `DeskUI_Gallery` sahnesi

Stil rehberindeki her bileşeni her durumuyla gösteren bir galeri sahnesi kur (QA ve tasarım kontrolü için). Galerinin düzeni rehberin birebir kopyası olmak zorunda değil, ama **bileşenler** `reference/zoom/C5_*` görüntüleriyle eşleşmeli.

| Bileşen | Normal | Üstüne gelme | Basılı / seçili | Devre dışı |
|---|---|---|---|---|
| Kâğıt düğme (30 / sm 24) | kâğıt, 1 px ink2, alt gölge 2 | `#FFFDF5`, kenar ink, gölge 3 | y +2, gölge yok, `#E8DEC6` | `#E3DACB`, **kesik** `#A89D86`, gölge yok, kilit + **neden** (“Fon yetersiz”) |
| Mürekkep düğme (birincil; ekranda en çok bir) | `#1E1B16`, krem yazı, alt gölge 2 siyah | `#35302A`, gölge 3 | `#000`, y +2 | `#8C8476`, kenar `#6F6759`, yazı `#ECE5D5` |
| Bağlantı | 1 px alt çizgi | mavi, 2 px çizgi | `#0F2F5E`, y +1 | `#8A8170`, kesik çizgi |
| Hız tuşu (30, yuvarlak) | `#1E1B16`, kenar 2 `#9A9284` | pirinç kenar + 2 px %35 hale | **basılı kalır**: krem, pirinç kenar, içe gölge, y +2 | `#3A352E` (olay/modal sırasında) |
| Alt sekme | yükseklik 36 | 40 (kâğıt kalkar) | açık pencere 44, kenar 1,5 | `#9C9483`, kesik, kilit |
| Kapat / küçült (22) | kâğıt, kenar ink3 | `#FFFDF5`, kenar ink | `#E8DEC6`, y +1 | kesik (modal açıkken) |
| Eylem kartı | dizin, kenar `#CFC5AE`, gölge 2,2 | **−2 px**, 1,5 ink kenar, `#FFFDF5`, gölge 3,5 + önizleme | +1 px, gölge yok, `#F3EEDF` | `#EEE8DA`, kesik, gölge yok, neden metni |
| Tehlikeli eylem | üstte 3 px kırmızı şerit, kırmızı başlık ve bedel | — | — | — |
| Zarf | normal | y −3, gölge 4,8 | tutma: sarı ilerleme halkası + çatlak, “Bırakmayın: 1 sn” | soluk, gri etiket, neden |

- **Klavye odağı:** 2 px mavi kesik çerçeve, 2 px dışarıda (`sh_dash_r3_2`).
- **Tuş fişi:** klavye kısayolu olan her düğmede görünür.
- **Klasör sekmesi:**
  - **önde:** manila,
  - **arkada:** `#CDB27A`, başlık ink2, gölge 1×2.
- **Diğer rehber bölümleri:**
  - **renk:** zemin, kâğıt, mürekkep ve kontrast tablosu (manila üstünde renkli küçük yazı **yok**),
  - **güven katmanı tablosu,**
  - **tipografi ölçeği,**
  - **boşluk:** 4 px taban,
  - **köşe ve gölge,**
  - **9-dilim malzemeleri,**
  - **harita işaretleri,**
  - **araştırma satırı durumları,**
  - **teleks, YZ günlüğü, danışman.**

---

## 8. Etkileşim, girdi ve hareket

### Klavye

Input System varsa `DeskControls.inputactions` kullan; yoksa projenin girdi katmanını kullan. uGUI otomatik navigation'ı kapat (`Navigation.Mode.None`); kısayollar komutlara bağlanır.

| Tuş | Bağlam | İş |
|---|---|---|
| R / N / T / L / H | masa | Araştırma / Haberler / Saha Ekipleri / YZ Günlüğü / Rehber penceresini aç-kapa |
| 1–4 | masa, dosya açık | bölge eylemi 1–4 (tıklamayla aynı); **4 (Son Şalter) onay ister** |
| B | masa, bölgede ekip varsa | saha operasyonu |
| Boşluk | masa | duraklat / sürdür |
| + / − | masa | hız artır / azalt (açık karar 1, §11) |
| Esc | her yer | en üstteki pencereyi kapat; pencere yoksa oyunun duraklatma menüsü |
| 1 / 2 / 3 | C3 | seçeneği seç (kart üstüne gelme durumuna geçer) |
| Enter | C3 | seçili seçeneği imzala |
| Boşluk (basılı) | C3 | haritaya bak: modal `CanvasGroup.alpha = 0`, karartma ve spot dahil; bırakınca geri |
| Enter / L | C4 | yeniden başla / günlükler |
| F9 · F10 · F11 · F12 | yalnızca geliştirme | dil TR↔EN · ipucu katmanı · referans katmanı opaklığı · referans katmanı aç-kapa |

### Fare

- **Üstüne gelme:** düğme, kart, sekme ve zarfta görsel çocuk kalkar; raycast alanı sabit kalır. Süre ~80 ms, ease-out. Basma 60 ms.
- **Harita düğümü:**
  - Tıklama bölgeyi seçer → sağ sütunda dosya açılır ya da güncellenir → seçim ipliği yeniden çizilir.
  - Üstüne gelme tasarımda yok; varsayılan için açık karar 4'e bak.
- **Sayaç:** üstüne gelince **döküm fişi** açılır:
  - 400 ms gecikme, imlecin 8 px altında, ekran kenarında ters açılır, en fazla 420 px genişlik,
  - içerik: kalemler, toplam, 30 günlük şerit,
  - kâğıdı göstergenin güven türüdür (RAPOR ise karbon ve “≈”); örnek `reference/zoom/C5_21_gosterge_*`.
- **Eylem kartı:** üstüne gelince dosyadaki ÖNİZLEME notu o eylemin etkisini gösterir. Önizleme VM'den gelir; arayüz hesaplamaz.
- **Pencere sürükleme:**
  - ⠿ tutamacından sürüklenir.
  - Bırakınca en yakın sütunun üstüne 120 ms'de mıknatıslanır; harita alanında kalamaz.
  - Aynı sütunda yığın kuralı için açık karar 2'ye bak.
  - × kapatır ve alt sekme normale döner.
- **Hedef kartı:** küçült düğmesi kartı başlık satırına (33 px) indirir.
- **Teleks:** üstüne gelince durur; öğeye tıklama Haberler penceresini açar (öneri).
- **Son Şalter:**
  1. 3,0 sn **basılı tut**: ilerleme halkası (Filled Radial360, saat yönü, üstten) dolar; %50'den sonra mumda çatlak görünür; alt yazı “Bırakmayın: N sn”.
  2. Erken bırakırsa iptal: halka 200 ms'de geri sarılır.
  3. 3 sn dolunca **ikinci adım**: zarftan çıkan onay kartı (bildirim kâğıdı + mürekkep “Kararı imzala” + kâğıt “Vazgeç”). İmza → `ConfirmGlobalKillSwitch()`.

### Hareket

Tasarım durağandır; aşağıdaki değerler öneridir ve hepsi `DeskTheme`'den ayarlanabilir olmalı.

- **İbre:** SmoothDamp ~0,5 sn, aşma yok. İllüzyonda tamamen sabit.
- **Modal:** karartma 150 ms, kâğıt 8 px düşerek 180 ms.
- **Klasör açılışı:** 120 ms.
- **Teleks:** `unscaledDeltaTime` ile kayar.
- **Bulanıklık efekti yok.**

---

## 9. Doğrulama

1. **`ReferenceOverlay`** (Editor/Development)
   - `reference/screens/<ekran>.png`'yi diskten yükler. Dosyayı `Assets` içine kopyalama; `File.ReadAllBytes` + `Texture2D.LoadImage` kullan.
   - Tam ekran RawImage olarak en üst canvas'ta gösterir.
   - F12 aç-kapa; F11 opaklık %25/50/75/100; Shift+F11 **yanıp sönme** (0,5 sn). Fark shader'ı yok.
2. **Yakalama**
   - Menü: `Tools/Desk UI/Capture Game View`.
   - Play modunda, Game görünümü 1920×1080 iken `ScreenCapture` ile `Docs/UI_C/captures/<ekran>_<tarih>.png` dosyasına yazar.
   - `DeskDebugMenu` F1–F4 ile C1/C2/C3/C4 ön ayarlarını yükler.
3. **Karşılaştırma**

   ```
   python3 Docs/UI_C/tools/compare.py Docs/UI_C/captures/C1_….png Docs/UI_C/reference/screens/C1_main.png --regions C1
   ```

   - **`_edges.png`:** referans kenarları yeşil, Unity kenarları mor, çakışanlar beyaz.
   - **Panel kenarları, çizgiler ve büyük metinlerde çift çizgi kalmamalı** (≤ 2 px). Küçük metinde Chrome ↔ TMP kenar yumuşatması farkı olağandır.
   - Bölge yüzdelerini iyileşmeyi izlemek için kullan.
4. **Taşma denetleyicisi** (`Tools/Desk UI/Check Text`)
   - Açık sahne ve prefab'lardaki tüm TMP metinleri **TR ve EN** için tarar.
   - Hata durumları:
     - taşma,
     - 11 px altı yazı,
     - 15 px altı Kalam,
     - Auto Size açık,
     - `UpperCase` stili.
5. **EditMode testleri**
   - `DeskFormat` (TR/EN sayı, yüzde, U+2212, gün kodu),
   - Türkçe harf dönüşümü,
   - ibre açısı ve kama dolgu/döndürme hesabı,
   - harita projeksiyonu,
   - rampa rengi ve yazı rengi,
   - TSV → `RectTransform` dönüşüm yardımcısı.
6. **Performans**
   - Frame Debugger ile draw call sayısı.
   - Profiler'da boşta GC alloc 0.
   - Canvas yeniden kurulumu yalnızca veri değişince.
7. **En-boy oranları:** 1920×1080, 2560×1440, 1920×1200, 3440×1440 ekran görüntüleri. Pencereler haritayı örtmemeli, hiçbir öğe kesilmemeli.

---

## 10. Fazlar ve kabul ölçütleri

Her fazın sonunda dur, raporla, onay bekle.

| Faz | İçerik | Kabul ölçütü |
|---|---|---|
| **F0 · Keşif ve plan** (kod yok) | Unity sürümü (2022.3 / Unity 6; TMP ayrı paket mi, uGUI 2.0 içinde mi), render pipeline, Input System, lokalizasyon, DI/async, mevcut UI klasörleri ve asmdef'ler, Newtonsoft, Unity MCP var mı, oyunun mevcut veri modelleri (bölge, gösterge, araştırma). `PLAN.md`: bulgu tablosu, klasör/asmdef planı, VM ↔ oyun verisi eşlemesi, gereken paketler, riskler, ≤ 5 soru | kullanıcı onayı |
| **F1 · Temel** | içe aktarma (postprocessor + atlas + doku ayarları), TMP font asset'leri (yedek, ağırlık, style sheet), `DeskTheme` (tokens.json'dan), temel bileşenler (`PaperPanel`, `StampView`, `TrustMarkView`, `CarbonText`, `DeskButton`, `KeyChipView`, `SpeedKeyView`, kapat, radyo, `DeltaView`, ilerleme ve aralık çubukları, ayraçlar), `UILineGraphic`, `UIPolygonGraphic`, galeri sahnesi (temel bileşenler × durumlar), biçimlendirme ve harf testleri | galeri görüntüleri C5 yakın planlarıyla eşleşir; 9-dilimler 3 farklı boyutta bozulmaz; testler geçer; konsol temiz |
| **F2 · C1 durağan** | C1'in tüm bölgeleri `C1_normal.json` ile (yalnızca görsel üstüne gelme), harita katmanları, referans katmanı + yakalama + karşılaştırma | kenar bindirmesinde yapısal çift çizgi yok; font, boyut, ağırlık ve renkler doğru; 21:9 ve 16:10'da harita açık |
| **F3 · C1 etkileşim** | pencere yöneticisi (aç/kapa/sürükle/mıknatıs/yığın), alt sekmeler ve rozetler, klavye haritası, düğüm seçimi → dosya + seçim ipliği, eylem üstüne gelme → önizleme, araştırma başlat/duraklat (mock), acil fon, teleks akışı, ibre yumuşatma, döküm fişi, Son Şalter tutma + onay, danışman eylemleri | etkileşim kontrol listesi (§8) eksiksiz; boşta 0 GC; titreme yok |
| **F4 · C2 İllüzyon** | `IllusionPresenter` + `IIllusionAware`, C2 verisi, tüm sunum anahtarları, `ClueOverlay` | C2 karşılaştırması; çalışma anında İllüzyon aç/kapa yalnızca izinli öğeleri değiştirir, yerleşim zıplamaz |
| **F5 · C3 olay** | `EventModalView` (karartma + spot + halka, bildirim, seçenekler, YZ günlüğü, danışman, alt şerit, iplik), duraklatma, 1/2/3/Enter/Boşluk | C3 karşılaştırması; Boşluk basılıyken harita tamamen görünür |
| **F6 · C4 rapor** | `GameOverView` (rapor, büyük damga, KPI'lar, şerit grafik: ızgara, bantlar, seriler, işaretler, etiketler, lejant; zaman kartları + iplik; alt çubuk) | C4 karşılaştırması; mock verisi değişince grafik doğru yeniden çizilir |
| **F7 · Cila ve teslim** | EN geçişi + taşma denetimi (iki dil), en-boy oranı görüntüleri, performans geçişi, `Docs/UI_C/UYGULAMA_NOTLARI.md` (bileşen kullanımı, oyuna bağlama, bilinen sapmalar), tüm ekranların son yakalamaları | tüm kontrol listeleri yeşil |

---

## 11. Açık kararlar

Varsayılanı uygula, ayarlanabilir bırak ve KARARLAR.md'ye yaz.

1. **Hız kısayolları.** Stil rehberinde “Boşluk / 1–3” yazıyor, ama 1–4 dosya eylemleri için kullanılıyor.
   - **Varsayılan:** Boşluk = duraklat, `+`/`−` (ve numpad) = hız; 1–4 eylemlerde kalır.
2. **Aynı sütunda birden çok pencere.**
   - **Varsayılan:**
     - sütun başına en çok 2 pencere,
     - yenisi önde; arkadakinin sekmesi sağa yaslı görünür (arka ton `#CDB27A`), tıklanınca öne gelir,
     - hedef kartı sabittir; sol sütunda araştırma alanını yeni pencere alır.
3. **Tasarlanmamış pencereler** (Haberler, Saha Ekipleri, YZ Günlüğü, Rehber).
   - **Varsayılan:** aynı `FolderWindow` + mevcut bileşenlerle sade içerik:
     - Haberler: teleks öğeleri liste olarak, güven işaretli,
     - YZ Günlüğü: C3/C5'teki kırmızı daktilo satırları.
   - Yeni görsel dil icat etme.
4. **Düğüm üstüne gelme.**
   - **Varsayılan:** etiket çipi ink kenar alır, düğüm 1 px kalkar, ipucunda bölge adı + değer + güven **kelimesi**.
5. **İllüzyon geçişi.**
   - **Varsayılan:** sessiz; analist katmanı tek seferde kaybolur.
   - Seçenek: birkaç güne yayılarak kaybolma (bayrak).
6. **Son Şalter onay kartı ve tehlikeli eylem onayı.** Çizilmedi.
   - **Varsayılan:** bildirim kâğıdı + mürekkep “Kararı imzala” + kâğıt “Vazgeç”.
7. **Klasör taşması.**
   - **Varsayılan:** sayfa içinde RectMask2D'li ScrollRect; 4 px ink2 kaydırma çubuğu, iz yok.
8. **BİLİNMİYOR bölge dosyası.**
   - **Varsayılan:** aynı düzen; değerler yerine BİLİNMİYOR işaretleri, tabloda “—”; İzleme ağı eylemi öne çıkar.
9. **Dosyanın kapatılması.**
   - **Varsayılan:** seçim kalkar, iplik kaybolur.
10. **Canvas türü.**
    - **Varsayılan:** Overlay; proje kamera canvas'ı kullanıyorsa ona uy.
11. **Danışman mesajı sığmazsa.** Bant tek satırdır ve harita ile sayaçlar arasında büyüyecek yer yoktur.
    - **Varsayılan:** metin küçültülmez; sonu “…” ile kesilir ve üstüne gelince tam metin ipucunda görünür. “Göster” bağlantısı ilgili yeri açar.

---

## 12. Yapma listesi

- **Görseli yeniden yorumlama.** Şunlar yok: bulanık ya da yumuşak gölge, degrade, tasarımda olmayan parıltı, daha büyük köşe yarıçapı, “modernleştirme”.
- **Manila üstüne renkli küçük yazı yazma.** Durum metinleri klasör içindeki krem sayfaya yazılır.
- **Kalam'ı temel bilgi için ya da 15 px altında kullanma.**
- **Pencereyle haritayı örtme.**
- **Görünümde oyun kuralı hesaplama;** simülasyon koduna dokunma.
- **Şunları kullanma:**
  - TMP Auto Size,
  - Türkçe metinde `UpperCase` / `SmallCaps`,
  - sahte kalın (`<b>` gerçek Bold asset'e gitmeli).
- **`.prefab` / `.unity` YAML'ını elle yazma.**
- **Büyük tekrar eden dokuyu** Image Tiled'a ya da atlasa koyma.
- **Kenarlık için `Outline` kullanma.** Null sprite'lı `Image` kullanma.
- **İç içe LayoutGroup / ContentSizeFitter zinciri kurma.**
- **Update içinde bellek ayırma:** string birleştirme, LINQ, her karede TMP'ye yazma yok.
- **Sormadan** paket ekleme ya da proje ayarı değiştirme.

---

## 13. İlk yanıtın (Faz 0)

1. **Paket özeti:** 8–10 madde; kurallar, ekranlar, varlıklar ve sayısal gerçeklerin nerede olduğu.
2. **Proje incelemesi bulguları:**
   - Unity sürümü,
   - render pipeline,
   - uGUI/TMP paket durumu,
   - Input System,
   - lokalizasyon,
   - DI/async/tween,
   - mevcut UI klasörleri ve asmdef'ler,
   - Newtonsoft,
   - Unity MCP bağlantısı,
   - oyunun ilgili veri modelleri.
3. **`Docs/UI_C/PLAN.md` taslağı.**
4. **En fazla 5 soru.**

**Kod yazma; onay bekle.**
