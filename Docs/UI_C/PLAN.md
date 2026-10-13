# UI_C · Analist Masası: uygulama planı (Faz 0)

Durum: **onaylandı** (2026-10-11; yanıtlar §9 ve `KARARLAR.md`'de) · 2026-10-11 · kaynak: `Docs/UI_C/PROMPT.md`

Bu belge projeyi inceleyerek PROMPT'taki önerileri bu projenin kurallarına uyarlar. Sapmalar §4'te gerekçeleriyle listelenir. Onaydan sonra kararlar `KARARLAR.md`'ye tek satır olarak taşınır.

---

## 1. Proje incelemesi: bulgular

| Konu | Projede | Sonuç |
|---|---|---|
| Unity | **6000.3.12f1** (Unity 6.3) | — |
| Render pipeline | URP 17.3.0 | Screen Space Overlay canvas URP'den etkilenmez |
| uGUI / TMP | `com.unity.ugui` 2.0.0; TMP bu paketin içinde. TMP Essential Resources `Assets/TextMesh Pro`; varsayılan font LiberationSans SDF. TMP Settings: Default Font Asset Path `Fonts & Materials/`, genel yedek listesi boş | Desk font asset'leri kendi `Resources/Fonts & Materials/` klasörüne konur. Böylece `<font="…">` etiketi çalışır ve **TMP Settings'e dokunulmaz**. Yedek zinciri asset başına, style sheet metin başına (`TMP_Text.styleSheet`) atanır |
| Input | Input System 1.19.0, `InputSystemUIInputModule`. Kod `Keyboard.current` ile doğrudan okuyor; `InputSystem_Actions.inputactions` var ama kullanılmıyor | Masa da aynı düzeni kullanır. Tuş fişleri InputAction bağlamasından değil, sabit bir `DeskKeys` tablosundan + `Loc` çevirisinden gelir (“Space” → “Boşluk”) |
| Lokalizasyon | Kendi `Loc` sistemi (`Corrigible.Core`, gettext tarzı; **anahtar = Türkçe kaynak metin**). Tablo `Resources/Localization/strings.txt` (tr⇥en), `LocalizedText` bileşeni, `Corrigible/Lokalizasyon/*` menüleri. Unity Localization paketi yok | İnce bir `IDeskLocalizer` → `Loc` köprüsü yazılır. `strings_tr_en.csv`'deki tr/en çiftleri `strings.txt`'ye birleştirilir; CSV'deki `key` sütunu yalnızca not olarak kalır. `DeskFormat` `Loc.Culture`'dan bağımsız, deterministik çalışır |
| DI / async / tween | Yok. Coroutine + `Update` + `Time.unscaledTime` kullanılıyor | Masa da aynısını kullanır; DOTween, UniTask ya da DI eklenmez |
| Mevcut UI | `Assets/Corrigible/Game/UI`: CrisisDeskView, MapView, RegionPanelView, WindowView/DeskWindows, modallar, TooltipView, UiTheme SO, StyledButton. Prefab'lar `Prefabs/UI`. `Main.unity` canvas'ları Overlay; CanvasScaler 1920×1080, Match 0 | Masa **ayrı kökte** kurulur; mevcut UI'ya, `Main.unity`'ye ve prefab'lara dokunulmaz. Aynı adlı sınıflar (MapView, WindowView, TooltipView) `Corrigible.Desk` namespace'iyle ayrılır |
| asmdef'ler | `Corrigible.Core` (noEngineReferences), `Corrigible.Game`, `Corrigible.Editor`, `Corrigible.Tests.EditMode`, `Corrigible.Steam`. Namespace kökü `Corrigible.*`. Menü kökü `Corrigible/…` | Yeni asmdef'ler `Corrigible.Desk*` olur (§2). Menüler `Corrigible/Desk UI/…` altına girer (PROMPT'taki `Tools/Desk UI/…` yerine) |
| Newtonsoft | Manifest'te **doğrudan yok**. `com.unity.nuget.newtonsoft-json` 3.2.2 dolaylı olarak çözülüyor (ai.assistant, ai.inference, pipeline bağımlılığı) | Dolaylı bağımlılığa yaslanmak kırılgan. Ya manifest'e açıkça eklenir ya da Editor dönüştürücüsü → ScriptableObject yolu seçilir (**soru 2**) |
| Sprite atlas | `EditorSettings` Sprite Packer Mode = 0 (Disabled). `com.unity.2d.sprite` dolaylı olarak var | Sprite Atlas V2 için bu ayar değişmeli (**soru 2**). Değişmezse ≤35 draw call hedefi tutmaz |
| Unity MCP | `unity` CLI + `com.unity.pipeline` 0.8.0-exp.1. Editor açık ve bağlı (`ready`). eval, derleme, test, Play, ekran görüntüsü çalışıyor | Menü komutlarını, Play'i ve yakalamayı ben çalıştırırım; sizden menü çalıştırmanız istenmez |
| Python araçları | Python 3.14.3; **Pillow kurulu değil** | `compare.py` için `pip install pillow` gerekir; bu bir indirme (**soru 2**) |
| Git / LFS | Dal `main`, LFS ve `.gitattributes` yok. Önceki oturumların işleri (20+ dosya) **commit edilmemiş** | Paketten Assets'e kopyalanacak varlıklar ~4,5 MB (sprite 1,2 + font 3 + harita). `Docs/UI_C/reference` (14 MB) Assets dışında kalır. F1'den önce mevcut işlerin commit'lenmesi önerilir (siz isterseniz) |
| Testler | EditMode 36/36 | Masa testleri ayrı assembly'de; mevcutlar etkilenmez |

---

## 2. Klasör ve assembly planı

```
Assets/Corrigible/Desk/
  Art/Sprites/          135 PNG (@2x, PPU 200) + DeskUI.spriteatlasv2
  Art/Textures/         8 tekrar eden doku (atlas dışı, Wrap Repeat)
  Art/Map/              c_harita@2x.png (atlas dışı)
  Fonts/                TTF + lisanslar (OFL, DejaVu)
  Resources/Fonts & Materials/   CP-*, LF-*, Kalam-*, DejaVuSans* SDF asset'leri (<font> araması için)
  Data/Theme/           DeskTheme.asset, DeskStyles (TMP style sheet)
  Data/Mock/            C1_normal, C2_illusion, C3_event, C4_gameover (.json TextAsset ya da MockDeskPreset)
  Prefabs/{Components,Windows,Screens}
  Scenes/               DeskUI_Gallery.unity, DeskUI_Sandbox.unity
  Scripts/Runtime/{Core,Theme,Components,Graphics,Windows,Map,Screens,Data,Localization,Debug}
  Scripts/Editor/{Import,Builders,Tools}
  Tests/EditMode/
```

| Assembly | İçerik | Referanslar |
|---|---|---|
| `Corrigible.Desk` | bileşenler, VM'ler, `IDeskDataSource`/`IDeskCommands`, mock kaynak, tema, `DeskFormat` | UnityEngine.UI, Unity.TextMeshPro, Unity.InputSystem, `Corrigible.Core` (**yalnızca `Loc`** için; görünüm `Simulation`/`WorldState` kullanmaz) |
| `Corrigible.Desk.Editor` | `DeskSpriteImporter`, atlas/font/tema kurucuları, yakalama, `Check Text` | + Corrigible.Desk |
| `Corrigible.Desk.Tests` | EditMode testleri (§9.5) | Corrigible.Desk(.Editor), NUnit |
| `Corrigible.Desk.Bridge` *(F8, onaylanırsa)* | `SimDeskDataSource`, `SimDeskCommands`: `Simulation` → VM | Core, Game, Desk |

Bu yapıda görünüm, oyun kuralını derleme düzeyinde bile göremez. Kuralları bilen tek parça Bridge'dir.

---

## 3. Mimari özeti (PROMPT §4'e göre, projeye uyarlanmış)

- **Veri sınırı:** `IDeskDataSource` (`Current`, `Changed`) ve `IDeskCommands` PROMPT'taki imzalarla kurulur. VM ağacı mock JSON'un şeklini izler: `DeskViewModel` {Clock, Rail, Gauges[5], Advisor, Goal, Research, Map, Dossier, Telex, Dock, KillSwitch, Illusion}, `EventViewModel`, `GameOverViewModel`. VM'ler **gösterilecek** metni ve değeri taşır; görünüm hesap yapmaz.
- **Kök:** `DeskPresenter` veri kaynağını ve komutları alır, `Bind(vm)` ile dağıtır. Her görünüm son değeri önbelleğe alır ve yalnızca değişeni yazar.
- **Tema:** `DeskTheme` SO, `Corrigible/Desk UI/Temayı tokens.json'dan doldur` menüsüyle dolar. Renkler `ThemeColor` anahtarıyla gelir; İllüzyon paleti aynı temada durur.
- **Canvas'lar:** kök Overlay, CanvasScaler 1920×1080 **Expand**. Alt canvas'lar BG 0 / Map 10 / HUD 20 / Windows 30 / Ticker 40 / Bottom 41 / Overlay 60 / Modal 100 / Tooltip 110 / Debug 1000. Yalnızca yeni sahnelerde kullanılır; Main'deki Match 0 düzeni olduğu gibi kalır.
- **Prefab üretimi:** **Elle** (soru 3). Prefab'lar Editor'de bir kez kurulur ve asset kaynaktır; yeniden üreten builder tutulmaz. Ölçüler kurulum sırasında TSV'den alınır.
- **Özel Graphic'ler:** `UILineGraphic` (çoklu çizgi, Bézier, kesik desen, yuvarlak uç; atlas beyazı UV) ve `UIPolygonGraphic` (düz renk ya da tekrar eden doku).
- **Girdi:** `Keyboard.current` + `DeskKeys`. Hız kısayolları açık karar #1'deki varsayılanı izler: Boşluk = duraklat, +/− = hız, 1–4 = eylemler.
- **Hareket:** coroutine / unscaled time. Update içinde bellek ayırma yok.
- **Mock:** `MockDeskDataSource` + `MockDeskCommands`. F9–F12 ve Debug menüsü C1–C4 ön ayarlarını yükler.

---

## 4. PROMPT'tan bilinçli sapmalar (proje kuralı önce)

| PROMPT | Bu projede | Neden |
|---|---|---|
| `Assets/_Project/UI/Desk/` | `Assets/Corrigible/Desk/` | Tüm proje `Assets/Corrigible/` altında |
| `SonSalter.UI.Desk*` asmdef/namespace | `Corrigible.Desk*` | Namespace kökü `Corrigible` |
| `Tools/Desk UI/…` menüleri | `Corrigible/Desk UI/…` | Mevcut menüler `Corrigible/…` altında |
| Unity Localization String Table | `Loc` + `strings.txt` köprüsü | Projenin sistemi; Türkçe kaynak = anahtar |
| TMP Settings varsayılan yedek = DejaVu | Asset başına yedek listesi | Genel ayar mevcut UI'yı da etkiler |
| KeyChip metni InputAction bağlamasından | `DeskKeys` sabit tablosu | Projede InputAction kullanılmıyor |
| `FontStyles.UpperCase` yasağı | Aynen; ayrıca `UiUtil.Up` (kültürlü) Desk'te kullanılmaz, metinler tabloda doğru harfle durur | — |

---

## 5. Görünüm modeli ↔ oyun verisi eşlemesi

Bu tablo F1–F7'yi engellemez; F1–F7 mock veriyle çalışır. Ama oyuna bağlarken (F8) nelerin hazır, nelerin eksik olduğunu gösterir.

| Tasarım (VM) | Oyunda | Uyum |
|---|---|---|
| Ray: rakip YZ “AYNA” + profil | `AiTypes`: Solucan / Manipülatör / Kâhin (YZ'nin özel adı yok) | ◐ Ad sabit “AYNA”, profil = tür adı olabilir |
| Saat: gün, saat, hız 1×/2×/4×, duraklat | `S.Day`, `GameController.Hold`, hız | ● (oyunda saat yok, Bridge türetir) |
| 5 gösterge: Fon, Siyasi Güç, Küresel Güven, Bağımlılık, Tekillik | `S.Fon`, `S.Sg`, `Global().Trust/Dep/Tek` | ● Değerler var. ◐ Gösterge başına güven düzeyi ve günlük değişim oyunda yok; Bridge önceki günle farkı tutar |
| Danışman notu + eylemler | `S.Advice`, `Explain` | ● |
| Hedef: Hizalama / Silme yolları | Hizalama = yorum→duzelt→kilit; Silme = çekirdek + imha + temiz günler | ● İlerleme Bridge'de hesaplanır |
| Araştırma: 4 dal, 10 proje, **aynı anda 2 aktif**, duraklat | 4 dal (Tespit, Savunma, Politika, Hizalama), 9 proje, **tek aktif** proje, duraklatma yok | ◐ Liste VM'den gelir. Duraklat Bridge'de gizlenir |
| Acil durum fonu (Kapalı/×1,5/×2 hızlandırma) | `ResearchReserves` {0,15,30,60}: araştırmaya dokunulmayan **yedek** (anlamı farklı) | ◐ Seçenekler VM'den gelir; metin oyuna uyarlanır |
| Harita: 12 bölge, 15 yol (`map.json`: KAM, OAM, … OKY) | 12 bölge **farklı**: Rusya, Çin, Japonya-Kore, Hindistan var; Orta Amerika yok. 18 bağlantı + 5 uydu | ○ **En büyük uyuşmazlık** (R1, soru 5) |
| Düğüm sızma + güven (onaylandı / rapor / şüpheli / bilinmiyor) | `Shown(r).Src`: verified/field/truth → ONAYLANDI; report → RAPOR; report+`Flag` → ŞÜPHELİ; unknown → BİLİNMİYOR; off → karartılmış | ● |
| Dosya: kaynak tablosu (telekom %38, Ekip B %52…), ± aralık | Tek bir `Shown` değeri; kaynak listesi yok | ◐ Bridge tek satır + bölgeyle ilgili haberleri gösterir |
| Dosya: yerel güven, bağımlılık, ekonomi | `r.Trust`, `r.Dep`, `r.Econ` | ● |
| Eylemler 1–4 + bedel + devre dışı nedeni | `Actions.All`, `Balance`, `MonitorCostFor`, `Blocked`, `ActResult.Msg` | ● |
| Önizleme notu (eylem sonrası tahmin) | Yok | ○ Bridge yalnızca bedel ve gecikmeyi gösterir ya da notu gizler |
| Saha: ekip, moral, baskın | Deneysel mod: `TeamState`, `RaidCheckResources`, `RaidLauncher` | ● (yalnızca deneysel modda) |
| Teleks + güven işareti | `S.News` (Agency, Verified, Suspect; `Fake` asla doğrudan gösterilmez) | ● |
| Alt sekmeler: Araştırma, Haberler, Saha ekipleri, YZ günlüğü, Rehber | `DeskWindows`: araştırma, günlük, rehber, ekipler, haberler | ● |
| Küresel Son Şalter | `GlobalKillSwitch()` | ● |
| C2 İllüzyon (sunum anahtarları + 10 ipucu) | `S.Illusion`; `Shown`/`Global` zaten sahte değer döner | ● Bayrak Bridge'den gelir; ipuçları tasarım gereği ince |
| C3 olay: kaynaklar, bedel/kazanç tabloları, “olası”, önizleme, YZ günlüğü | `DilemmaDef`: Title, Body, Choices{Label, Effect **metni**, Check, Apply}. Yapısal bedel/kazanç ve kaynak listesi yok | ◐ Bölümler opsiyonel; Bridge Effect metnini tek satıra koyar |
| C4: sonuç, KPI'lar, gerçek/gösterilen şerit grafik, kırılma anları | `Endings`, `S.Hist` (Real/Shown/Illusion), `S.Moments`, `RunActions`, `ActionLog` | ● |

● hazır · ◐ uyarlama gerekir · ○ oyunda karşılığı yok

---

## 6. Faz planı

| Faz | Teslim | Doğrulama |
|---|---|---|
| **F1 · Temel** | Varlık kopyası; `DeskSpriteImporter` (sprites.json → PPU/border/pivot/tip); atlas; doku ve harita içe aktarma; 11 TMP font asset'i + yedek + ağırlık tabloları + style sheet; `DeskTheme`; `DeskFormat`; `IDeskLocalizer`; temel bileşenler; `UILineGraphic`, `UIPolygonGraphic`; `DeskUI_Gallery` (temel bileşenler × durumlar) | C5 yakın planlarıyla görsel eşleşme, 9-dilimler 3 boyutta sağlam, EditMode testleri, konsol temiz |
| **F2 · C1 durağan** | Yerleşim tabloları (TSV → C#); C1'in tüm bölgeleri `C1_normal` ile; harita katmanları; `ReferenceOverlay`; `Capture Game View`; karşılaştırma | `compare.py --regions C1`, 21:9 ve 16:10 görüntüleri |
| **F3 · C1 etkileşim** | `WindowManager` (aç/kapa/sürükle/mıknatıs/yığın), alt sekmeler, klavye, seçim ipliği, önizleme, mock komutlar, teleks akışı, ibre yumuşatma, döküm fişi, Son Şalter tutma + onay | §8 kontrol listesi, boşta 0 GC |
| **F4 · C2 İllüzyon** | `IllusionPresenter`, `IIllusionAware`, `ClueOverlay` | C2 karşılaştırması; aç/kapa sırasında yerleşim zıplamaz |
| **F5 · C3 olay** | `EventModalView` + spot + iplik + 1/2/3/Enter/Boşluk | C3 karşılaştırması |
| **F6 · C4 rapor** | `GameOverView` + şerit grafik + zaman kartları | C4 karşılaştırması; veri değişince grafik doğru çizilir |
| **F7 · Cila** | EN geçişi, `Check Text` (TR+EN), en-boy oranları, performans, `UYGULAMA_NOTLARI.md` | tüm listeler yeşil |
| **F8 · Oyuna bağlama** *(öneri, PROMPT kapsamı dışında)* | `Corrigible.Desk.Bridge`; §5'teki ◐/○ maddeleri için kararlar; Main'de eski masa ↔ yeni masa geçişi (ayar ya da sahne) | Oyun uçtan uca oynanır, 36+ test geçer |

Her fazın sonunda durup rapor veririm: ne yapıldı, sapmalar, karşılaştırma çıktıları, açık sorular.

---

## 7. Gereken paket ve ayar değişiklikleri (izin gerekir)

| Değişiklik | Etki | Gerekçe |
|---|---|---|
| `EditorSettings.spritePackerMode` → **Sprite Atlas V2 – Enabled** | Proje ayarı. Mevcut sprite'lar atlas'ta olmadığı için etkilenmez | Tek atlas ile ≤35 draw call |
| `com.unity.nuget.newtonsoft-json` **3.2.2** manifest'e doğrudan | Sürüm zaten çözülmüş durumda; yeni indirme beklenmez | Mock JSON'larda sözlük ve null var. Alternatif: Editor dönüştürücü → `MockDeskPreset` SO (biraz daha fazla kod) |
| `pip install pillow` (kullanıcı site-packages ya da scratchpad venv) | PyPI'dan indirme | `compare.py` |
| TMP Settings, Player, Quality, mevcut sahne ve prefab'lar | **Değişmez** (F8'e kadar) | — |

---

## 8. Riskler

| # | Risk | Önlem |
|---|---|---|
| R1 | **Harita uyuşmazlığı:** tasarım ile oyunun bölgeleri, konumları ve yolları farklı | `MapView` düğüm, etiket, yol ve konumları VM'den alır; tasarım haritası yalnızca mock'tadır. Bağlama yolu soru 5'te |
| R2 | Tasarımdaki bazı kavramların oyunda karşılığı yok: paralel araştırma, duraklatma, kaynak tablosu, olay bedel/kazanç yapısı, gösterge başına güven, önizleme | Bileşenler bu bölümleri **opsiyonel** destekler (boşsa gizlenir, yerleşim zıplamaz). Oyun kuralı eklemek ayrı bir karardır (kapsam dışı) |
| R3 | ±2 px sadakat: Chrome ve TMP metin ölçümü, satır yüksekliği, Courier harf aralığı | Panel kenarı, çizgi ve büyük metinde ≤2 px hedeflenir. Küçük metindeki kenar yumuşatma farkı rapora yazılır |
| R4 | Draw call: atlas dışı RawImage dokular (keçe, ahşap, teleks, dizin çizgileri) batch'i bölebilir | Doku katmanları alt canvas'larda gruplanır; Frame Debugger'la F2 sonunda ölçülür |
| R5 | Unity 6.3 / uGUI 2.0'daki TMP API farkları (font asset oluşturma, per-text style sheet, yedek) | F1'in başında küçük bir deneme; sorun çıkarsa KARARLAR'a yazılır |
| R6 | Lokalizasyon: mock metinleri rich text etiketli (`<style=num>`); `strings.txt` ~200 satır büyür | Etiketli tam metin anahtar olur; EN taslaktır. `Çevirileri doğrula` Desk'i de tarar |
| R7 | Play modundayken derleme statik sözlükleri kaybettiriyor (bu oturumda yaşandı) | Derlemeden önce Play'den çıkılır |
| R8 | Büyük ve commit'lenmemiş çalışma ağacı | F1'den önce commit (siz isterseniz) |

---

## 9. Sorular ve yanıtlar

Yanıtlar (2026-10-11): 1 uygun · 2 hepsine izin · 3 **elle** · 4 uygun · 5 **(b)**.

1. **Kapsam:** Masa, mock veriyle **yeni sahnelerde** (`DeskUI_Sandbox`, `DeskUI_Gallery`) kurulsun; `Main.unity` ve mevcut UI F7'ye kadar hiç değişmesin. Oyuna bağlama (Bridge + Main'de geçiş) ayrı bir **F8** olsun. Uygun mu?
2. **İzinler:** (a) Sprite Packer Mode → Sprite Atlas V2, (b) Newtonsoft'u manifest'e doğrudan ekleme, (c) Pillow kurulumu. Hangilerine izin veriyorsunuz?
3. **Prefab yolu:** Editor builder (öneri; idempotent, yerleşim TSV'den üretilen C# tablolarından) mı, yoksa elle prefab mı?
4. **Konum ve adlar:** `Assets/Corrigible/Desk`, `Corrigible.Desk*` asmdef/namespace ve `Corrigible/Desk UI/…` menüleri uygun mu?
5. **Harita (F8'e kadar engellemez):** Oyuna bağlarken hangi yol izlensin?
   - (a) Oyunun bölgeleri tasarımın 12 düğümüne eşlenir; ad oyundan, konum tasarımdan gelir (Japonya-Kore ↔ Orta Amerika gibi uymayan eşler kalır).
   - (b) **Öneri:** düğümler oyunun enlem/boylamından tasarımın projeksiyonuyla yerleştirilir, yollar oyunun bağlantılarıdır; harita sprite'ı aynen kalır.
   - (c) Oyunun bölgeleri tasarıma göre değiştirilir. Bu oyun mantığına ve JS paritesine dokunur.
