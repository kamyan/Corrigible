# SON ŞALTER · Sade arayüz “harita önde”: Analist Masası + Baskın — Unity uygulaması

> **Bu dosya Claude Code için uygulama talimatıdır.**
> - Paket kökü bu dosyanın bulunduğu klasördür. Önerilen yer `Docs/UI_Sade/` (`Assets/` dışında). Aşağıdaki tüm yollar bu klasöre göredir.
> - Kod tanımlayıcıları İngilizce, açıklamalar Türkçedir.
> - Bu paket, daha önce verilen **C paketinin** (`Docs/UI_C/`, C PROMPT.md) devamıdır.
> - **Sürüm 1.1 (16.10.2026):** açılır paneller çizildi: masa için S-Masa3–9 (§A.14), baskın için S-Baskin3–9 (§B.9). Önceki sürümde “çizilmedi” diye varsayılanı verilen kararlar artık referans çizimdir (§7). HEDEF kartına YZ yeteneği çipi eklendi (S-Baskin1–2 dahil). Ekran görüntüleri JPEG oldu (§3).

**İçindekiler**
- 0 Görev, ön koşul ve çalışma biçimi
- 1 Sade tasarımın fikri ve eski → yeni eşlemesi
- 2 Değişmez kurallar
- 3 Paket içeriği
- 4 Ortak mimari (veri sınırı, tema, canvas'lar, en-boy oranı, bileşenler, varlıklar)
- **A · S-Masa** (sade analist masası) — açılır paneller §A.14
- **B · S-Baskın** (sade taktik harita) — paneller ve durumlar §B.9
- 5 Doğrulama
- 6 Fazlar ve kabul ölçütleri
- 7 Açık kararlar
- 8 Yapma listesi
- 9 İlk yanıtın

---

## 0. Görev, ön koşul ve çalışma biçimi

### Görev

İki ekranı **sade tasarıma** göre kur. Sade tasarımın ilkeleri şunlar:

- harita ya da maket tam ekran ve ön planda,
- arayüz köşelerde,
- ayrıntılar açılır panellerde.

| Kod | Ekran | Referans | Sahte veri |
|---|---|---|---|
| **S-Masa1** | Sade analist masası, varsayılan | `reference/screens/S-Masa1.jpg` | `spec/mock/masa_S1.json` |
| **S-Masa2** | Aynı an; Doğu Asya seçili, bölge kartı açık | `S-Masa2.jpg` | `masa_S2.json` |
| **S-Masa3** | Araştırma çekmecesi + FON döküm fişi | `S-Masa3.jpg` | `masa_S3.json` |
| **S-Masa4** | Haberler çekmecesi; üstüne gelinen haberin bölgesi yanar | `S-Masa4.jpg` | `masa_S4.json` |
| **S-Masa5** | Ekipler çekmecesi | `S-Masa5.jpg` | `masa_S5.json` |
| **S-Masa6** | YZ günlüğü çekmecesi | `S-Masa6.jpg` | `masa_S6.json` |
| **S-Masa7** | Rehber çekmecesi | `S-Masa7.jpg` | `masa_S7.json` |
| **S-Masa8** | Lejant kartı + düğüm ipucu | `S-Masa8.jpg` | `masa_S8.json` |
| **S-Masa9** | Tam bölge dosyası | `S-Masa9.jpg` | `masa_S9.json` |
| **S-Baskin1** | Sade taktik harita, turun başı (B1'in sade hâli) | `S-Baskin1.jpg` | `baskin_S1.json` |
| **S-Baskin2** | Teknisyen seçili, sabotaj önizlemesi | `S-Baskin2.jpg` | `baskin_S2.json` |
| **S-Baskin3** | Makineler hareket ediyor (B5): son olay fişi | `S-Baskin3.jpg` | `baskin_S3.json` |
| **S-Baskin4** | Turu bitir: hakkı duran birim için onay | `S-Baskin4.jpg` | `baskin_S4.json` |
| **S-Baskin5** | Günlük çekmecesi | `S-Baskin5.jpg` | `baskin_S5.json` |
| **S-Baskin6** | Kurallar kartı | `S-Baskin6.jpg` | `baskin_S6.json` |
| **S-Baskin7** | Son tur (B6): düşen birim, kazandıran hamle | `S-Baskin7.jpg` | `baskin_S7.json` |
| **S-Baskin8** | Geri çekilme onayı (B8) | `S-Baskin8.jpg` | `baskin_S8.json` |
| **S-Baskin9** | Sonuç: hedef kırıldı (B7a) | `S-Baskin9.jpg` | `baskin_S9.json` |

- **S-Masa, C1'in yerini alır.** C3 (olay) ve C4 (oyun sonu) değişmez. C2 İllüzyon sunumu S-Masa'ya uyarlanır (§A.12).
- **S-Baskın, B paketindeki taktik haritanın yerini alır:**
  - 3D maket aynıdır, kamera 1,5 kat yakındır.
  - Eski paneller kurulmaz: sol/sağ sütun kartları, bilgi satırı. Günlük ve kurallar kartı sade yerlerinde açılır (S-Baskin5–6).
  - B2–B8 durumlarının sade karşılıkları §B.7 ve §B.9'dadır (S-Baskin3–9).
- **Sadakat hedefi:**
  - UI için 1920×1080'de ±2 px.
  - 3D tahtada kadraj, parça ve işaret yerleri ±4 px.
  - 3D'de renk ve ışık “aynı aile” olmalı (§B.2).

### Kapsam dışı

- Oyun kuralları ve simülasyon. Arayüz **sahte veriyle** çalışır ve oyuna temiz bir sınır sunar (§4.2).
- Mevcut oyun mantığını değiştirme.

### Ön koşul: C paketi

Bu paket C paketinin üstüne kurulur:

- **C'nin F1 · Temel fazı bitmiş olmalı:** içe aktarma ve atlas, TMP fontları, `DeskTheme`, temel bileşenler (`PaperPanel`, `StampView`, `TrustMarkView`, `CarbonText`, `DeskButton`, `KeyChipView`, `SpeedKeyView`, `DeltaView`, `UILineGraphic`, `UIPolygonGraphic`…).
  - Bitmemişse önce C PROMPT'un **yalnız F1'ini** uygula. C1–C4 ekranlarını **kurma**.
- **C1 projede kuruluysa:** bileşenlerini yeniden kullan, ekran düzenini S-Masa ile değiştir. C1'in pencere bileşenleri (klasör, kâğıt, tablo, kaydırma) çekmecelerde ve tam dosyada yeniden kullanılır; içerik düzeni §A.14'teki çizimlerdir.
- **C paketi projede yoksa** kullanıcıdan iste. Onun sprite kitini ve fontlarını kendin üretmeye çalışma.

### B paketi

3D tahtanın görsel kaynağı “Baskın · taktik harita” paketidir. Gerekli kısımlar bu pakete kopyalandı: `spec/raid/`, `reference/raid_B/`.

### Çalışma biçimi

C PROMPT §0 aynen geçerlidir; kısaca:

1. **Önce oku.** Bu dosyayı baştan sona oku. Sonra şunlara bak:
   - `reference/screens/*.jpg`,
   - `art/sprites/_contact_sheet.png`,
   - `spec/tokens_sade.json`.

   Ayrıntı gerektikçe şunları kullan:
   - `reference/zoom/*@2x.*`,
   - `reference/measure/*.tsv`,
   - `spec/map_regions.json`, `spec/mock/*.json`,
   - `reference/html/*.html`,
   - `reference/raid_B/`.
2. **Faz 0** (§6): projeyi incele, `Docs/UI_Sade/PLAN.md` yaz. **Onay almadan kod yazma.**
3. **Her fazın sonunda dur.** Kısa rapor ver: ne yapıldı, referanstan sapmalar, karşılaştırma çıktıları, açık sorular. Sonra onay bekle. Kullanıcı “onay sormadan devam et” derse sürdür.
4. **Belirsizlikte şu sırayla bak:** referans PNG → TSV → HTML/CSS → tokens.
   - Hâlâ belirsizse tasarıma en yakın seçeneği uygula ve `Docs/UI_Sade/KARARLAR.md`'ye tek satır yaz.
   - Bloklayan en çok 3 soruyu birlikte sor.
5. **Projenin kuralları önce gelir:** namespace, asmdef, DI, async, tween, lokalizasyon, Input System, URP ayarları.
6. **Önce sor:**
   - paket eklemek,
   - Project / Player / Quality / URP Asset ayarı değiştirmek,
   - mevcut sahne ya da prefab'a dokunmak.

   Commit'i yalnız kullanıcı isterse at.
7. **Unity editörüyle çalışma.**
   - Bağlı bir Unity MCP sunucusu varsa onu kullan.
   - Yoksa kodu ve Editor betiklerini yaz; kullanıcıdan menü komutlarını çalıştırmasını ve ekran görüntüsü paylaşmasını iste.
   - `.prefab` / `.unity` YAML'ını elle yazma. Builder betikleri kullan (C §4.6).
8. **İki ekran ayrı ayrı yapılabilir.** Kullanıcı yalnız birini isterse sıra şudur: F0 → F1 (ortak) → o ekranın fazları (§6).

---

## 1. Sade tasarımın fikri

- **Harita / maket tam ekrandır.** Arayüz dört köşeye çekilir; ekranın ortası boş kalır.
- **Ekranda yalnız karar için gerekenler vardır.**
  - Masada: 7 sayı + zaman + bölge düğümleri + yayılım ipliği + ekipler.
  - Baskında: tur, hedef, ekip, robot niyetleri.
- **Ayrıntı istenince açılır.**
  - Masada: bölge kartı, çekmeceler, döküm fişleri, düğüm ipucu, lejant, tam dosya (S-Masa2–9).
  - Baskında: birim kartı, üstüne gelme kartı, son olay fişi, onay notları, günlük, kurallar (S-Baskin2–9).
- **Görsel dil C ile aynıdır.** Kâğıt, mürekkep, damga, kalem halkası, iplik, raptiye, ozalit maket. Yalnızca öğe sayısı az, öğeler büyüktür.
- **Güven dili korunur:**

  | Güven | Görünüm |
  |---|---|
  | ONAYLANDI | mavi mühür |
  | RAPOR | karbon değer + “~” fişi |
  | ŞÜPHELİ | turuncu kalem halkası + Kalam “?” |
  | BİLİNMİYOR | kesik daire + haritada sis taraması |

### Masa: C1 → S-Masa

| C1'de | S-Masa'da |
|---|---|
| 5 analog sayaç + not satırları | 6 kâğıt etiket. Sol üst: **FON, SİYASİ GÜÇ, HEDEF** (Hizalama/Silme). Sağ üst: **KÜRESEL GÜVEN, BAĞIMLILIK, TEKİLLİK**, eşik çubuklarıyla |
| Üst ray (plaka, AYNA etiketi, CANLI, gizli damgası, ayarlar) | Üst ortada yalnız **gün, saat, hız**. Ayarlar ve Esc menüsü oyunun duraklatma menüsünde |
| Danışman bandı | İlgili bölgede sarı **“!” notu**. Tıklayınca bölge seçilir; danışman cümlesi notun ipucunda |
| Hedef kartı | Sol üst **HEDEF** etiketi |
| Araştırma klasörü (sürekli açık) | **ARAŞTIRMA** çekmecesi (sekmeden açılır) |
| Bölge dosyası (sürekli açık) | Bölgeye tıklayınca açılan **bölge kartı**. Tam dosya kartın “Tüm dosya” bağlantısında |
| Teleks şeridi | **HABERLER** çekmecesi. Sekme rozetinde doğrulanmamış haber sayısı |
| Basılı lejant + ölçek | **LEJANT** düğmesi → lejant kartı |
| Harita başlığı, ızgara etiketleri, ağ yolları, analist raptiyeleri ve kalem notları | Kaldırıldı |
| Küresel Son Şalter zarfı | Aynı zarf, küçük boy, sağ alt |

### Baskın: B → S-Baskın

| B'de | S-Baskın'da |
|---|---|
| Üst ray (başlık, tur kutuları, evre, AYNA) | Sol üst **tur plakası** (tur, kutular, evre) |
| Hedef formu | Sol üst **HEDEF** kartı (küçük maket resimleri + can + sivil) |
| 3 ekip dizin kartı | Sol alt **ekip kartları** (küçük). Seçince **birim kartı** açılır |
| Bilgi satırı | Sağ altta **üstüne gelinen parçanın kartı** + tahtada önizleme |
| Operasyon günlüğü | **GÜNLÜK** düğmesi → günlük çekmecesi. Yeni satırda kırmızı nokta |
| Kurallar kartı + lejant | **KURALLAR** düğmesi → kurallar kartı (lejant içinde) |
| Geri al / Yetenek / Turu bitir | Sağ alt **Geri al** + **TURU BİTİR**. Yetenek: Q ya da birim kartındaki satır |
| Geri çekil (sol alt) | Sağ üst **GERİ ÇEKİL**: iki basışlı |
| Pusula, nesne adları, sivil ve soğutma can etiketleri, robot harfi | Yok |

---

## 2. Değişmez kurallar

C §2'nin tamamı geçerli. Kısaca:

- uGUI + TMP;
- yalnız `UI/Default` ve TMP SDF;
- sert gölge (`Shadow`, bulanıklık 0);
- en küçük yazı 11 px, Kalam ≥ 15 px;
- TR + EN; Auto Size kapalı;
- renk tek başına anlam taşımaz;
- yazı düz kâğıt üstündedir;
- görünüm kural hesaplamaz.

**Değişen ve eklenen kurallar:**

1. **C §2.7 kalktı** (“pencereler haritayı örtmez”). Yerine:
   - Açılır paneller haritanın / tahtanın üstüne açılabilir.
   - **Seçili bölgeyi** (düğüm + etiket) ya da **seçili birimi, hedef karesini ve önizlemesini** örtmez.
   - Aynı anda en çok **bir bağlam kartı + bir çekmece** açıktır.
2. **Köşe kuralı.** Kalıcı HUD yalnız dört köşede ve (masada) üst ortadadır. Ekranın ortasına kalıcı öğe koyma.
3. **Harita kökü ekranı kaplar** (cover, §4.4).
   - HUD, canvas'ın köşelerine çapalıdır ve ölçeklenmez.
   - Harita üstündeki **yazılı ve simgeli öğeler** (düğüm, etiket, jeton, not) harita ölçeğiyle büyümez. Yalnız yerleri haritayı izler.
4. **3D tahta** yalnız S-Baskın'dadır. Kurallar:
   - URP **Lit** (ya da Simple Lit), düz renk + belirtilen birkaç doku;
   - özel shader yok;
   - bloom, glow ve emissive yok;
   - işaretler Unlit/Transparent, ZWrite kapalı (`spec/raid/raid_tokens.json › marks`).
5. **Dünya içi etiketler** (can, sıra bayrağı, açılır yazı) ayrı bir ekran canvas'ındadır. Her karede dünya → ekran izdüşümüyle yerleşir; en küçük 11 px.
6. **Kaldırılan öğeleri varsayılan ekrana geri koyma** (§8).

---

## 3. Paket içeriği

| Yol | İçerik | Ne için |
|---|---|---|
| `PROMPT.md` | bu dosya | talimat |
| `reference/screens/` | 18 kare, 1920×1080, JPEG (q90, 4:4:4). Kayıpsız gerekirse `reference/html/` 1920×1080 tarayıcıda açılıp yeniden çekilir | **görsel gerçek** |
| `reference/zoom/` | 44 yakın plan, @2x (köşeler, kartlar, çekmeceler, tahta, önizleme). Büyük olanlar boyut için JPEG (q92, 4:4:4) | küçük ayrıntı |
| `reference/measure/*.tsv` | her işaretli öğe ve çocukları için ekran kutusu, dönüşümsüz boyut, font, renk, zemin, kenar, köşe, gölge, dönüş, opaklık, metin | **sayısal gerçek** |
| `reference/html/` | 18 karenin bağımsız HTML'i + `cs-harita.svg` (fontlar `fonts/web/`) | CSS kaynağı; tarayıcıda incele |
| `reference/raid_B/` | B paketinden: `sheets/` (parçalar, işaretler, paneller, teknik), `tahta/B1…B8_tahta@2x.jpg` (görünüş için JPEG), `README_B.md` | 3D tahta ve B durumlarının tahta görünüşü |
| `spec/tokens_sade.json` | yeni tokenlar: renk, yazı tablosu, gölge, yerleşim, harita, düğüm, kart; baskın HUD'u ve kamera (k = 1,5); açılır paneller (`desk.panels`, `raid.panels`) | tema |
| `spec/map_regions.json` | izdüşüm, 12 bölge çokgeni (halkalar + earcut üçgenleri), düğüm ve etiket konumları, bindirme geometrisi | harita |
| `spec/mock/*.json` | 18 karenin sahte görünüm modeli; aynı zamanda **önerilen şema** | sahte veri |
| `spec/raid/` | `raid_tokens.json` (kamera, ışık, 3D malzeme ve ölçüler, işaret, niyet, etiket, hareket), `board_states.json` (B1–B8), `strings_tr_en.csv` | 3D tahta |
| `spec/strings_sade_tr_en.csv` | yeni dizeler: TR + taslak EN + TR'de ölçülen genişlik | lokalizasyon |
| `art/map/map_base@2x.png`, `map_lines@2x.png` | 3840×2160: deniz, ızgara, kıyı halesi / saydam sınır, kıyı ve katlama çizgileri | harita katmanları |
| `art/textures/` | döşeme dokuları: `tx_fog`, `tx_quar_hatch`, `tx_hazard`, `tx_carbon_hatch`, `tx_paper_grain` · ekran boyu: `tx_lamp_desk`, `tx_lamp_raid` | desen ve ışık |
| `art/sprites/` | 174 yeni sprite @2x + `sprites.json` + `_contact_sheet.png`. 1.1'de eklenenler: `rn_*` (araştırma düğümü), `mk_check_bold_26`, `mk_plus_bold_20`, `pencil_x_40x56`, `ring_dash_24`, `mark_fake_24`, `dot_12`, `ring_12_w1`, `robot_mini_22`, `thumb_cabinet_node_s`, `thumb_debris_backup` | sprite kiti |
| `art/raid/` | `tx_ozalit_grid_2048.png` (genel), `tx_ozalit_ana_sunucu_salonu_2048.png` (giriş kapıları + kanal işaretleri basılı) | 3D zemin dokusu |
| `fonts/web/` | yalnız HTML için woff2 (TMP fontları C paketinde) | — |
| `tools/compare.py` | Unity yakalamasını referansla karşılaştırır (§5) | doğrulama |

**C'den gelenler (bu pakette yok):**

- kâğıt 9-dilimleri (`sh_fill_*`, `sh_frame_*`, `sh_dash_*`),
- damga, mühür, “~” fişi, tuş fişi,
- hız tuşu, takvim, plaka,
- alt sekme parçaları, zarf parçaları (`env_*`), `deco_sticky`,
- `tx_felt`,
- fontlar ve TMP asset'leri, `DeskTheme`, temel bileşenler.

**Çelişki önceliği:** görünüm için ekran görüntüsü, sayılar için TSV, davranış için bu dosya. Piksel karşılaştırması `reference/screens/*.jpg` ile yapılır (JPEG gürültüsü birkaç düzeydir; `compare.py`'nin kenar eşiği etkilenmez). Yakın planlar bakmak içindir.

> **Önemli not.** CSS ile TSV farklıysa **TSV doğrudur.** Alt sekmeler, baskın üst düğmeleri ve LEJANT düğmesi **Libre Franklin 700** ile render edilir.

---

## 4. Ortak mimari

### 4.1 Klasör ve assembly

Projede başka bir düzen varsa ona uy.

```
Assets/_Project/UI/Desk/                (C)  → S-Masa: Scripts/Runtime/Sade/{Hud,Map,Cards,Drawers}, Prefabs/Sade/, Art/Sade/
Assets/_Project/UI/Raid/                (yeni) Scripts/Runtime/{Hud,Cards,WorldLabels,Background}, Prefabs/, Art/
Assets/_Project/Raid/Board/             (yeni, 3D) Scripts/Runtime/{Grid,Pieces,Marks,Intents,Camera,Motion}, Prefabs/Pieces/, Materials/, Textures/
Scenes: SadeDesk_Sandbox.unity, RaidBoard_Sandbox.unity, SadeUI_Gallery.unity
Docs/UI_Sade/: PLAN.md, KARARLAR.md, UYGULAMA_NOTLARI.md, captures/
```

Assembly'ler:

- `SonSalter.UI.Desk` (genişletilir),
- `SonSalter.UI.Raid`,
- `SonSalter.Raid.Board`,
- her biri için `.Editor` ve `.Tests`.

### 4.2 Veri sınırı

**Masa** — C'deki `IDeskDataSource` / `IDeskCommands` korunur.

- VM ağacına sade dal eklenir: `SadeDeskViewModel`. Şeması `spec/mock/masa_*.json`'dadır.
- Alanlar: `Clock`, `Resources[2]`, `Goals[2]`, `Risks[3]`, `Map` (regions, spreadThreads, teams), `Dock`, `Legend`, `KillSwitch`, `RegionCard` (null = kapalı), `Drawer` (research / news / teams / aiLog / guide; `masa_S3…S7.json`), `Tooltip` (döküm fişi), `NodeTooltip`, `Dossier`.
- Komutlara şunlar eklenir:

```csharp
void SelectRegion(string regionId);   void ClearSelection();
void HoverRegionAction(string regionId, int index);   // önizleme VM'den döner; -1 = yok
void OpenDrawer(DrawerId id);  void CloseDrawer();   void ToggleLegend();
void OpenFullDossier(string regionId);  void CloseDossier();          // §A.14.11
void HoverChip(string chipId);  void HoverNode(string regionId);     // döküm fişi / düğüm ipucu; null = bıraktı
void SelectResearchNode(string nodeId);  void StartResearch(string nodeId);  void PauseResearch(string nodeId);  void SetResearchSpeed(float mult);
void SetNewsFilter(NewsFilter f);  void HoverNewsItem(string newsId);  void AuditNews(string newsId);
void HoverTeam(char teamId);  void AssignTeam(char teamId, string regionId);  void OpenGuideTopic(string topicId);
// ExecuteRegionAction, LaunchFieldOperation, TogglePause, SetSpeed, ConfirmGlobalKillSwitch: C'deki gibi
```

**Baskın**

```csharp
public interface IRaidDataSource { RaidViewModel Current { get; } event Action<RaidViewModel> Changed; }
public interface IRaidCommands {
    void SelectUnit(char letter);   void ClearSelection();
    void HoverTile(Vector2Int tile); void ClearHover();
    void MoveSelectedTo(Vector2Int tile);  void ArmAbility();  void UseAbilityOn(Vector2Int tile);
    void Undo();  void EndTurn();      // hakkı duran birim varsa 1. çağrı onay ister (VM Hud.End = confirm)
    void CancelPending();              // Esc: onayı / hazır yeteneği bırak
    void SpeedUpMachines(bool on);     // Boşluk, makineler evresinde
    void RequestRetreat();          // 1. basış: armed; 2. basış: onay
    void ToggleLog();  void ToggleRules();  void ContinueToReport();
}
```

- **`RaidViewModel` alanları:**
  - `Board`: `spec/raid/board_states.json` şeması (grid, objects_override, units, robots + intent, wakes, marks, ghosts, pushes, popups).
  - `Hud`: turnPlaque (last), objective (+ aiAbilities, broken), topButtons (logUnread, open, retreatArmed), team (status go/ab/done/down, warn), end (normal / locked / confirm), unitCard, hoverCard, eventSlip, endConfirm, logDrawer, rulesCard, retreatConfirm, dim, hidden.
  - `Log`, `Outcome`, `Retreat`.
- **Örnekler:** `spec/mock/baskin_S1…S9.json`.
- **Önizleme verisi** (ulaşılabilir kareler, itme, çarpma, hasar, iptal edilen niyet, HEDEF KIRILIR) VM'den gelir. Arayüz kural hesaplamaz.
- **Sahte kaynaklar:**
  - `MockSadeDeskDataSource` (S-Masa1–9), `MockRaidDataSource` (S-Baskin1–9 ve B1–B8; B ön ayarlarını `board_states.json`'dan kurar).
  - `MockRaidCommands`: seçim, üstüne gelme, Q, Z ve Esc'yi sahte duruma uygular. En azından S1 ↔ S2 geçişini göstersin.

### 4.3 Tema

- **`DeskTheme`'e sade eki:** `SadeTheme` alt nesnesi ya da ayrı ScriptableObject; kaynağı `tokens_sade.json › colors, type, shadows, desk`.
- **`RaidTheme`:** `spec/raid/raid_tokens.json` + `tokens_sade.json › raid`.
- **Editor menüsü** (`Tools/Sade UI/Import Tokens`) bu JSON'lardan temaları doldurur.
- Bileşenler sabit hex yazmaz.
- **Yazı tablosu:** `tokens_sade.type`. Her girdi `[aile, ağırlık, px, harf aralığı em]` biçimindedir. TMP'de karşılığı `characterSpacing = em × 100`.

### 4.4 Canvas düzeni ve en-boy oranı

**Kök canvas ayarları**

- `CanvasScaler`: Scale With Screen Size, referans 1920×1080, Screen Match Mode **Expand**, Reference PPU 100.
- Varsayılan render mode Overlay. Proje kamera canvas'ı kullanıyorsa ona uy.

**Masa canvas'ları**

| Canvas | Sıra | İçerik |
|---|---|---|
| `MapRoot` | 0 | harita katmanları, bindirme çizgileri, düğüm, etiket, jeton |
| `Hud` | 20 | köşe etiketleri, zaman, sekmeler, LEJANT, zarf |
| `Context` | 40 | bölge kartı + kart ipliği |
| `Drawer` | 50 | çekmeceler, tam dosya, lejant kartı |
| `Modal` | 100 | C3, onaylar |
| `Tooltip` | 110 | döküm fişi, düğüm ipucu |
| `Debug` | 1000 | — |

**Kaplama (cover)**

- `MapCoverFitter` harita kökünü (1920×1080 tasarım dikdörtgeni, pivot ortada) şu ölçekle büyütür: `s = max(canvasW / 1920, canvasH / 1080)`. Fazlası kırpılır.
  - 16:10 (canvas 1920×1200): `s = 1,111`, yanlardan ~107 px kırpılır.
  - 21:9 (canvas 2580×1080): `s = 1,344`, üstten ve alttan ~186 px kırpılır (Arktik ve Güney Okyanusu).
- **Ölçeklenenler:** dokular, bölge meshleri, sınır ve iplik çizgileri.
- **Ölçeklenmeyenler:** düğüm, etiket, jeton, ETA etiketi, uyarı notu (§2.3). Bunlar 1x kalır.
  - Ekran konumları: `screen = center + (p − (960, 540)) · s`.
  - Bunları ayrı bir `MapMarkers` katmanında tut; `MapCoverFitter.Changed` olunca yeniden konumla.
- **Lamba dokusu** (`tx_lamp_desk`) haritaya değil **ekrana** gerilir.

**Baskın katmanları**

| Katman | İçerik |
|---|---|
| Arka plan | **Screen Space – Camera** canvas, plane distance kameranın uzak düzlemine yakın: keçe + lamba + tahta gölgesi (§B.1) |
| 3D | URP sahnesi: tahta, parçalar, işaretler, oklar |
| `WorldLabels` | Overlay, sıra 10: can etiketleri, sıra bayrakları, açılır yazılar |
| `RaidHud` | Overlay, sıra 20: köşeler |
| `RaidContext` | 40: birim kartı, üstüne gelme kartı, son olay fişi, turu bitir notu |
| `RaidDrawer` | 50: günlük, kurallar |
| `Modal` | 100: sonuç fişi, geri çekilme onayı, kararma (kararmanın üstünde yalnız kırmızı düğme ve fiş) |

- Kamera boyu formülü tahtanın canvas pikselindeki boyunu sabit tutar (§B.1).
- 16:10'da üstte ve altta, 21:9'da yanlarda daha çok keçe görünür. HUD köşede kalır.

**Pixel Perfect kapalı.** Çerçeveli paneller tam piksel konum ve boyutta durur.

### 4.5 Bileşen kataloğu (yeni)

Her bileşen ayrı prefab + script olsun. C bileşenleri içeride yeniden kullanılır.

**Ortak**

| Bileşen | Görev |
|---|---|
| `HudChipView` | Kâğıt etiket: simge dairesi + etiket + güven işareti + değer (+ küçük ek) + `DeltaView` + isteğe bağlı `ThresholdBarView` |
| `ThresholdBarView` | 7 px çubuk: zemin + 1 px çerçeve, tehlike bölgesi (`tx_hazard`), dolgu, belirsizlik (`tx_carbon_hatch`), 3×13 işaretçi |
| `GoalChipView` | HEDEF: iki satır (ad, 96 px çubuk, %) |
| `ClockPlaqueView` | Plaka: takvim + saat + 4 hız tuşu (C parçaları) |
| `SadeDockView` / `SadeDockTabView` | Sekmeler: simge + etiket + tuş fişi + rozet (sarı sayı / kırmızı nokta) |
| `PopupCardBase` | Kâğıt kart + başlık + başlık çizgisi + kapat; açılma/kapanma hareketi; yerleşim (çapa, kaçınma dikdörtgenleri, güvenli alan) |
| `CardStringView` | İki raptiye arasında gölgeli `UILineGraphic` |
| `IconCircleView`, `PipRowView`, `AvailabilityIconView` | Küçük parçalar |

**Masa:**

- `SadeMapView`:
  - `RegionFillLayer` (12 × `RegionMeshGraphic`),
  - `QuarantineLayer`, `SelectionLayer`,
  - `SpreadThreadLayer`, `CoreRippleView`, `TeamRouteView`,
  - `MapMarkersLayer`: `SadeRegionNodeView`, `SadeRegionLabelView`, `TeamTokenView`, `EtaTagView`, `AlertNoteView`,
  - `MapCoverFitter`.
- `RegionCardView`.
- `DrawerHost` + `DrawerFolderView` (kap) ve içerikler: `ResearchDrawerView` (`SpeedSegmentView`, `ResearchTreeView`, `ResearchNodeView`, `ResearchDetailStripView`), `NewsDrawerView` (`FilterChipView`, `NewsItemView`), `TeamsDrawerView` (`TeamCardView`, `RouteBarView`), `AiLogDrawerView` (`AiLogEntryView`), `GuideDrawerView`.
- `RegionDossierView` (`SourceTableView`, `HistoryChartGraphic`, `LinkChipView`, `ActionRowView`).
- `BreakdownTooltipView` (`BalanceBarsView`, `SparklineGraphic`), `NodeTooltipView`, `LegendCardView`, `MapHighlightLayer`.
- Küçük parçalar: `RegionChipView`, `TrustMarkLargeView`, `MiniNodeView`.
- `KillSwitchEnvelopeView` (C, küçük boy).

**Baskın:**

- 3D: `RaidBoardView`, `RaidCameraRig`, `BoardBackgroundView`.
- İşaret ve etiket katmanları: `MarksLayer`, `IntentsLayer`, `WorldLabelLayer`.
- HUD: `TurnPlaqueView`, `ObjectiveCardView`, `RaidTopButtonsView`, `TeamChipRowView` / `TeamChipView`, `EndTurnGroupView`.
- Açılır öğeler: `UnitCardView`, `HoverInspectorView`, `EventSlipView` (`QueueBoxView`), `EndTurnConfirmView`, `RaidLogDrawerView`, `RulesCardView`, `RetreatConfirmView`, `OutcomeMemoView`.
- HUD parçaları: `AiAbilityChipView`; `TeamChipView` durumları: seçili, tur bitti, düştü, uyarı, kilitli.

### 4.6 Sprite kiti (yeni)

**İçe aktarma:** C §6.1 ile aynı.

- PPU 200 (PNG'ler @2x), mipmap yok, Clamp, sıkıştırma yok ya da yüksek kalite.
- `sprites.json → spriteBorderPx_LBRT` alanı `TextureImporter.spriteBorder`'a gider.
- `pivot`, `tint`, `imageType` alanlarını uygula.
- Bir `AssetPostprocessor` bunları otomatik yapsın.

**Atlas:** C'nin `DeskUI` atlasına ekle ya da `SadeUI` atlası kur. Tight Packing ve Rotation kapalı, padding 4.

**Simgeler:** her biri iki boyda gelir.

- `ic_*_s` (16 px tasarım) ≤ 16 px kullanım için,
- `ic_*_l` (26 px tasarım) 17–26 px kullanım için.
- Boyutu `sizeDelta` ile ver; `SetNativeSize` kullanma.
- Simgeler beyaz çizilmiştir; rengi `Image.color` verir.

**Küçük maket resimleri (`thumb_*`)**

- 3D tahtadaki parçaların aynı çiziminden üretildi.
- İlk sürümde PNG'leri kullan.
- 3D parçalar hazır olduğunda isteğe bağlı olarak `RenderTexture` ile üretmek bir seçenek (KARARLAR.md).

### 4.7 Dokular

**İçe aktarma:** `art/textures/*`, `art/map/*`, `art/raid/*`: Texture Type Default, Bilinear, mipmap yok.

- Döşeme dokuları (`tx_fog`, `tx_quar_hatch`, `tx_hazard`, `tx_carbon_hatch`, `tx_paper_grain`): **Wrap Repeat**.
- Lamba dokuları ve harita katmanları: **Clamp**.

**Döşeme ölçeği:** `tokens_sade.json` ve `map_regions.json › fills` dokunun bir periyodunu tasarım pikseli olarak verir:

| Doku | Bir periyot |
|---|---|
| `tx_fog` | 9,899 px |
| `tx_quar_hatch` | 12,728 px |
| `tx_hazard` | 4,808 px |
| `tx_carbon_hatch` | 4,525 px |
| `tx_paper_grain` | 128 px (256 doku pikseli) |

- RawImage için: `uvRect = (0, 0, alanG / periyot, alanY / periyot)`.
- Mesh için: `uv = konum / periyot`.

**Harita katmanları (`map_base@2x`, `map_lines@2x`)**

- Max Size 4096.
- `map_base` alfasızdır: RGB24 ya da yüksek kaliteli BC7/ASTC.
- `map_lines` RGBA (BC7 ya da sıkıştırmasız).
- Bellek bütçesini KARARLAR.md'ye yaz.

### 4.8 Performans

C §4.7 geçerli. Ek hedefler:

- **Bölge meshleri durağandır.** Renk değişirse yalnız o bölgenin `SetVerticesDirty`'si çağrılır.
- **Masa:** ≤ 30 draw call (harita 6–8, HUD ~15).
- **Baskın UI:** ≤ 25 draw call. 3D için:
  - parçalarda GPU instancing ya da SRP Batcher,
  - malzeme sayısı az: renk başına bir malzeme ya da MaterialPropertyBlock.
- **Boşta 0 B/kare GC.**
- Dünya etiketleri yalnız kamera, tahta ya da VM değişince yeniden konumlanır.

---

# A · S-Masa (sade analist masası)

Referanslar: `S-Masa1.jpg` … `S-Masa9.jpg`, `reference/zoom/S-Masa*`. Sayılar `reference/measure/S-Masa*.tsv`'de, yapı ve davranış aşağıda. Açılır paneller §A.14'te.

## A.1 Katman sırası (alttan üste)

1. **`map_base@2x`** (RawImage, 1920×1080 tasarım dikdörtgeni).
2. **Bölge dolguları**: 12 `RegionMeshGraphic` (§A.2).
3. **Karantina örtüsü**: karantinadaki bölgenin meshi; önce kâğıt `#EFE8D6` α .38, sonra `tx_quar_hatch`.
4. **`map_lines@2x`** (saydam): ülke ve bölge sınırları, kıyı çizgisi, katlama izleri.
5. **`tx_paper_grain`**: tüm haritaya döşenir.
6. **Karantina sınırı**: `UILineGraphic`; bölge halkaları kapalı çizgi, 2,2 px `#24592E` α .85, kesik 7/4.
7. **`tx_lamp_desk`**: ekran boyu.
8. **Seçim**: dolgu `#F5D646` α .16 + sınır 7 px `#F5D646` α .85 + 1,6 px ink.
9. **Çekirdek dalgası**: iki halka.
10. **Yayılım iplikleri.**
11. **Ekip rotası.**
12. **`MapMarkers`**: düğümler, etiketler, ekip jetonları, ETA etiketi, uyarı notu.

## A.2 Harita verisi ve bölge meshleri

**`spec/map_regions.json`**

- **Koordinatlar:** 1920×1080 tasarım pikseli, sol-üst orijin, y aşağı. Unity'de `anchoredPosition = (x, −y)`; ebeveyn sol-üst pivotlu 1920×1080.
- **`regions[]`**: `id`, `nameKey`, `node`, `label`, `centroid`, `bounds`, `polygons[]`.
  - Her çokgende `rings` (önce dış halka, sonra delikler) ve `triangles` vardır.
  - `triangles`, halkaların sırayla eklendiği düz köşe dizisine üçlü indekstir (earcut).
- **`overlay`**: iplik, dalga, rota, ekip, uyarı ve kart ipliği geometrisi.
- **`meta.projection`**: Miller izdüşümü; λ0 = 25,5°, S = 360,68 px/rad, ekvator y = 640.
  - `x = 960 + S·wrap(λ − λ0)`
  - `y = 640 − S·1,25·ln(tan(π/4 + 0,4·φ))`
  - `MapProjection.ToMap(lon, lat)` yaz. Ekip ve şehir konumları buradan gelir. Test için `lonLatSamples` alanını kullan.
- **Veri kaynağı:** Natural Earth 1:50m. Kamu malı; world-atlas paketi ISC lisanslı.
  - **Kırım, Doğu Avrupa bölgesinde** (Ukrayna) çizildi. Başka bir veriyle değiştirme.

**`RegionMeshGraphic : MaskableGraphic`**

- `OnPopulateMesh`: her çokgenin köşeleri `(x, −y)`, üçgenleri `triangles`. Saat yönü önemsiz (UI'da culling yok).
- **Düz renk:** `color` = sızma rampası (`tokens_sade.colors.infiltrationRamp`).

  | Sızma | Renk |
  |---|---|
  | 0–19 | `#F2C9B8` |
  | 20–39 | `#E4937B` |
  | 40–59 | `#B4442E` |
  | 60–79 | `#8C2416` |
  | 80+ | `#5C100A` |

  Sınırlar VM'den gelirse onu kullan.
- **BİLİNMİYOR:** dolgu `tx_fog`. `mainTexture` = doku, `uv = (x, −y) / 9,899`, `color` beyaz.
- **Karantina örtüsü:** ikinci bir `RegionMeshGraphic`. Önce kâğıt rengi α .38, üstte `tx_quar_hatch` (uv / 12,728).
- **Tıklama alanı:**
  - `raycastTarget` açık.
  - `IsRaycastLocationValid`: halkalar üstünde nokta-çokgen testi (çift–tek kuralı; delikler dahil).
  - Bölgeye tıklamak, düğüme tıklamakla aynıdır (§A.13).
- **Değişim:** VM'de bölge rengi değişince yalnız o grafik kirlenir.
- **Katmanlar bir arada:** taban, dolgu ve çizgi dokuları aynı izdüşümden üretildi. Üst üste birebir oturur; ofset verme.

## A.3 Bölge düğümü (`SadeRegionNodeView`)

**Yapı**

- Düğüm 64×64 bir kutudur; merkezi `regions[].node` noktasıdır.
- Tıklanabilir alan 44 px daire.

**Parça sırası (alttan üste)**

1. Seçiliyse fosfor: `node_halo_64`, `#F5D646` α .8.
2. Karantinadaysa `node_ring_quar_64`.
3. Gölge: `node_disc_44`, siyah α .25, (+1,5, +2).
4. Disk: `node_disc_44`, rampa rengi.
5. Halka: `node_ring_44`, ink.
6. ŞÜPHELİ ise `node_ring_sus_64`.
7. Rozet:
   - ONAYLANDI: `node_badge_seal_20`; merkezi düğüm merkezine göre (+18, −19).
   - RAPOR: `node_badge_report_22x16`; sol-üstü (+9, −27).
8. YZ çekirdeğiyse `node_badge_core_26`; merkezi (−18,5, −18,5).
9. Değer: CP 700 17, ortalı.
   - Yazı rengi: rampa < 40 ise ink `#1E1B16`, ≥ 40 ise krem `#FFF8EE`.

**Durumlar**

- **BİLİNMİYOR:** disk ve halka yerine `node_unknown_44` gelir. Sayı yerine Kalam 700 24 “?” `#4A4640`.
- **ŞÜPHELİ:** düğümün dışında Kalam 700 20 `#854005` “?”. Sol-üstü düğüm merkezine göre (+17, −41).
- **Üstüne gelme** (S-Masa8, §A.14.10): düğüm 1 px kalkar; 400 ms sonra etiketin yerine düğüm ipucu açılır.

## A.4 Bölge etiketi (`SadeRegionLabelView`)

- **Konum:** üst-orta çapa `(node.x, node.y + dy)`.
  - Varsayılan `dy = 30`.
  - DA = −46 (düğümün üstünde), AV = +33.
  - Değerler `map_regions.json › regions[].label` ve `tokens_sade.desk.node.labelOffsetY` içinde.
- **Yazı:** CP 700 12, harf aralığı .04em, renk `#2E2820`.
- **Hale:** HTML'deki `text-shadow` halesi TMP **Underlay** ile yapılır: renk `#F1E9D6`, dilation ~0,6, softness ~0,3. Ayrı materyal ön ayarı olarak kaydet.
- **Seçili etiket:** `#F5D646` zemin çipi, iç boşluk 2/5, gölge (1, 1) α .3; hale yok.

## A.5 Harita bindirmesi

**Yayılım iplikleri** (`overlay.threads`; VM'deki `spreadThreads` çiftleri)

- **Geometri:** karesel Bézier.
  - Başlangıç düğüm merkezinden 30 px (çekirdekten çıkıyorsa) ya da 24 px öteden.
  - Bitiş, hedef merkezine 30 px kala.
  - Kontrol noktası orta nokta + (0, 10).
- **Çizgi:** 2,4 px `#B8241A`.
- **Gölge:** aynı eğri 2,6 px siyah α .28, (+1,5, +2).
- **Ok ucu:** `arrowhead_tri`, eğrinin t = .58 noktasında, açı eğrinin teğeti.
- **Hesap:** geometriyi düğüm konumlarından çalışma anında hesapla. JSON'daki değerler doğrulama içindir.

**Çekirdek dalgası:** YZ çekirdeği olan bölgenin düğümünde.

- `ring_r40_w2`: `#9A1E14` α .5.
- `ring_r53_w1_4`: α .25.
- Hareket önerisi: 2,4 sn'de bir dışa doğru soluklaşarak genişleyen tek halka. İllüzyonda kapalı.

**Ekipler** (`overlay.teams`)

- **Jeton:** `team_token_30` + harf, CP 700 13 `#F1E9D6`. Sprite merkezi = jeton merkezi.
- **Yoldaki ekip:**
  - Rota: kesik 2/5, 2 px ink, yuvarlak uç; uçta `arrow_open`.
  - **ETA etiketi:** kâğıt, 20 px yükseklik, iç boşluk 0/6, `ic_clock_s` 12 + CP 700 12; gölge (1, 1) α .3. Sol-üstü jetonun (−24, +20) ötesinde.

**Uyarı notu** (`overlay.alert`)

- `map_alert_sticky`, −6°; sol-üstü düğüm merkezine göre (−62, −60).
- Danışmanın bir bölge için mesajı varsa görünür.
- Tıklayınca bölgeyi seçer. Üstüne gelince danışman cümlesi ipucunda görünür (C TooltipService).

**Ağ yolları gösterilmez** (açık karar 1).

## A.6 Sol üst: elimizdekiler

| Öğe | Kutu (x, y, genişlik, yükseklik) | İçerik |
|---|---|---|
| FON | 16, 14, 171,2, 62 | `ic_fund` · “FON” + mühür · “47,3” + ↓1,2 (kırmızı) |
| SİYASİ GÜÇ | 196, 14, 166,8, 62 | `ic_power` · “SİYASİ GÜÇ” + mühür · “62” + ↑0,4 (yeşil) |
| HEDEF | 376, 14, 274, 62 | `ic_goal` · “HEDEF” · iki satır: Hizalama (çubuk `#24592E`) %42, Silme (çubuk `#1A4789`) %28 |

**Etiket anatomisi** (`tokens_sade.desk.chip`)

- **Kâğıt:** yükseklik 62, iç boşluk 0/14/0/10, aralık 10.
  - `#F1E9D6`, kenar 1 px `#C8B996`, r2, gölge (3, 3) α .45.
- **Simge dairesi:** 36 px.
  - `circle_36` (`#FBF7EC`) + `ring_36_w1_5` (ink; TEKİLLİK'te `#9A1E14`).
  - Simge 19 px (`_l`), çizgi rengi halka rengi.
- **Metin bloğu:** dikeyde ortalı, satır aralığı 5.
  1. Etiket satırı (14 px): CP 700 11, .08em, ink2 + güven işareti (aralık 6).
  2. Değer satırı (24 px): CP 700 24 + değişim (ok simgesi 12 px, çizgi 2,2 + CP 700 12; aralık 7).
  3. (Riskte) 7 px çubuk, değer satırının 6 px altında.
- **HEDEF satırları:**
  - Yükseklik 13, aralık 5.
  - Ad LF 600 12, 62 px.
  - Çubuk 96×7.
  - Değer CP 700 13, 30 px, sağa yaslı.

**Yerleşim:** genişlikler TR metinle ölçüldü. EN'de içerik büyürse kutu sağa doğru genişler, sonraki kutu kayar (aralık 8).

## A.7 Üst orta: zaman

- **Plaka:** ortaya çapalı, (795,3, 14), 329,4×62.
  - Zemin `#0E0906`, kenar 1 px `#6E5A33`, r3, gölge (3, 3) α .5.
  - İç boşluk 0/12/0/10, aralık 12.
- **Takvim** (C): 60×42, başlık 14 px kırmızı “GÜN”, sayı “074” CP 700 19.
- **Saat** “14:32”: CP 700 22, .04em, `#E6CF9C`.
- **Dikey ayraç:** 1×34 `#4A3C22`.
- **Hız tuşları** (C `SpeedKeyView`): ⏸, 1×, 2×, 4×; 30 px, aralık 6. Seçili olan basılı durur.

## A.8 Sağ üst: riskler

| Öğe | Kutu | Güven | Çubuk |
|---|---|---|---|
| KÜRESEL GÜVEN | 1298, 14, 204, 62 | RAPOR: “~” fişi, değer karbon “≈41,8” | 132 px; tehlike 0–15; işaretçi 41,8 |
| BAĞIMLILIK | 1488, 14, 204, 62 | ŞÜPHELİ: etikette Kalam 700 18 “?”, değerde `hand_ring_value_76x37` (değerin sol-üstünden −9, −7) | 132 px; tehlike 70–100; işaretçi 58,4 |
| TEKİLLİK | 1682, 14, 222, 62 | RAPOR: “≈%37” + küçük “±6” (CP 700 13) | 150 px; dolgu 0–37 `#9A1E14`; belirsizlik 31–43; tehlike 94–100 |

**Çubuk**

- Zemin `#FBF7EC`, 1 px `#463F34` çerçeve (`rect_frame_1`, 9-dilim).
- Parçalar çerçevenin **içine** yerleşir (iç yükseklik 5).
- İşaretçi 3×13, üstten −4, ink, r1.
- Tehlike bölgesi `tx_hazard` döşemesidir.
- Çubuğun anlamı tooltip'te yazılır: “Kayıp eşiği 15 · ~27 gün” (C sayaç notlarındaki metinler).

**Sıralama:** sağdan sola TEKİLLİK, BAĞIMLILIK, KÜRESEL GÜVEN; aralık 8; sağ kenar 16.

## A.9 Alt sol: sekmeler ve çekmeceler

**Sekmeler** (`SadeDockView`)

- **Yerleşim:** sol 16, alta yaslı, aralık 6.
- **Boyut:** yükseklik 42; açık sekme 50 (iç alt boşluk 10, kenar 1,5 siyah).
- **Kenar ve köşe:** kenar 1 px siyah α .55 (alt kenar yok), r 7/7/0/0.
- **Gölge:** iç üst parlaklık beyaz α .35 (C `sh_tab_highlight`) + sağda (2, 0) siyah α .28.
- **İçerik:** simge 17 (`_l`) + etiket LF 700 13, .06em + tuş fişi 17 px.
- **Renkler** (`tokens_sade.colors.dockTabs`):

  | Sekme | Renk |
  |---|---|
  | ARAŞTIRMA | `#D9C08B` |
  | HABERLER | `#EFE4BC` |
  | EKİPLER | `#C9CDB4` |
  | YZ GÜNLÜĞÜ | `#D8A69B` |
  | REHBER | `#BFC8CD` |

- **Rozet:** sağ-üstte (−7, −8).
  - Sarı sayı: 18 px hap, 1,5 ink kenar, CP 700 11.
  - Kırmızı nokta: 14 px.

**Çekmeceler** (S-Masa3–7; ayrıntı §A.14)

- Manila klasör, rengi sekmenin rengi; sol 16, alt kenar 1016, genişlik 500, yükseklik içeriğe göre.
- İçerikler: ARAŞTIRMA (hız + dal ağacı + seçili düğüm şeridi), HABERLER (teleks listesi, güven işaretli), EKİPLER (ekip kartları), YZ GÜNLÜĞÜ (ele geçirilmiş döküm), REHBER (konu + sayfa).
- Tek çekmece açık olur; kapanma yolları: Esc, aynı sekme, klasör kapat düğmesi. Açılma 140 ms, 12 px aşağıdan.
- Bölge kartı açıkken çekmece açılabilir; kart sağ sütunda kalır (§2.1).

## A.10 Alt sağ: lejant ve zarf

**LEJANT düğmesi**

- Sağdan 282, alttan 26, 98,3×36.
- Kâğıt, 1 px `#C8B996`, r2, gölge (2, 3) α .45.
- `ic_legend` 16 + LF 700 12, .06em.
- Tıklayınca **lejant kartı** düğmenin üstünde açılır (S-Masa8, §A.14.9). Lejant kartı C basılı lejantının sade hâlidir:
  - sızma rampası (5 renk + sınırlar),
  - güven işaretleri (mühür / “~” / turuncu halka + ? / kesik daire),
  - sis taraması (bilinmiyor),
  - karantina (soluk + yeşil tarama + kesik yeşil çizgi),
  - YZ çekirdeği (Ç damgası + dalga),
  - yayılım ipliği, saha ekibi ve rota, uyarı notu.

**Küresel Son Şalter zarfı** (C bileşeni, küçük boy)

- Konum ve boyut: sağ 16, alt 14, 250×62.
- Gövde `#9C1F17`, kenar `#5E0F0A`, r3, gölge (3, 4) α .5.
- Kapak çizgisi: `M0 0 L124 27 L248 0`.
- Kâğıt etiket (12, 12, 170×38):
  - “KÜRESEL SON ŞALTER” CP 700 13, .04em,
  - “3 sn basılı tut” LF 11.
- Mum mühür 44 px: sağ 12, üst 9.
- Basılı tutma, çatlak, onay ve İllüzyon hâli C §7.1, §7.5 ve §8'deki gibidir.

## A.11 Bölge kartı (S-Masa2)

`RegionCardView : PopupCardBase`. TSV'de kart (1540, 96, 364×463) ve bölümleri tek tek var.

| Bölüm | Yükseklik | İçerik |
|---|---|---|
| Başlık | 52 | “DOĞU ASYA” CP 700 21, .04em · “R-10” CP 12 ink3 · sağda kapat (C 22 px). Altında 2 px `#D2483F` çizgi |
| Durum | 32 | `hand_ring_q_38x31` içinde Kalam 700 17 “?” + “Kaynaklar çelişiyor” LF 600 13 `#854005` |
| Sızma | 61 | Solda “SIZMA” (CP 700 11) ve altında “%38” (CP 700 40) + Kalam 700 24 turuncu “?”. Sağda **aralık çubuğu** |
| İstatistikler | 58 | 3 sütun, aralık 8, yükseklik 46, kâğıt. Her birinde simge 20 + değer CP 700 19 + güven işareti, altında LF 11 ink3 ad: Yerel güven ≈44 (~), Bağımlılık 63 (✓), Ekonomi 71 (✓) |
| Eylemler | 98 | 4 sütun, aralık 8, yükseklik 84. Simge 26 + ad LF 700 13; tuş fişi sağ-üstte (5, 5): İzleme ağı 1, Denetim 2, Karantina 3, **Son Şalter 4** (tehlikeli) |
| Önizleme | 50 | Sarı not, 40 px, iç boşluk 0/12, aralık 8: `ic_clock` + “5 gün · 8 fon” (CP 700 13) + `ic_arrow` + “kaynak” + küçük mavi damga ONAYLANDI (−3°) |
| Ekip | 70 | Kâğıt, 56 px: jeton B + “Ekip B / Seul · hazır” + sağda mürekkep düğme “Baskın %64 [B]” (36 px; % CP 700 `#E6CF9C`) |
| Daha fazla | 40 | Solda “Kaynaklar · nüfus · bağlantılar” LF 12 ink3, sağda “Tüm dosya ›” bağlantısı |

**Kart kâğıdı:** `#FBF7EC`, 1 px `#D2C7AF`, r2, gölge (4, 5) α .42, yatay iç boşluk 16.

**Aralık çubuğu** (`tokens_sade.desk.regionCard.range`)

- **Ray:** üstten 14, 8 px. Rampanın 5 eşit dilimi + 1 px ink2 çerçeve.
- **Belirsizlik bandı:** 29–47 aralığında, üst 10, yükseklik 16; 1,5 px kesik `#A9520A` + `#F5D646` α .25.
- **İşaretçiler:** 4×24, üst 6, gölge (1, 1) α .3.
  - Rapor 38: `#A9520A`.
  - Saha 52: `#1A4789`.
- **Etiketler:** üst 33, CP 700 12.
  - “rapor 38” sağı %37'de biter.
  - “saha 52” solu %53'ten başlar ve önünde `ic_teams_s` 12 vardır.

**Eylem kutuları**

- **Normal:** kâğıt, 1 px `#BFB394`, r3, alt gölge (0, 3) `#8E8166`.
- **Üstüne gelme** (S-Masa2'de İzleme ağı):
  - 2 px ink kenar, `#FFFDF5`, −2 px kalkar, gölge (0, 5) `#6E634D`.
  - Önizleme notu o eylemi gösterir (VM `preview`).
- **Tehlikeli:** yazı ve simge `#9A1E14`, kenar `#B97F76`, gölge `#7A2A20`. **4 onay ister** (C açık karar 6).
- **Devre dışı:** C kuralları. Kesik kenar, neden metni ipucunda.

**Yerleşim kuralı** (`PopupCardBase.Place`)

1. **Tercih:** düğümün sağı.
   - `x = node.x + 67`.
   - `y = clamp(node.y − 314, 96, H − 96 − h)`. 96 = üst etiket satırının altı.
2. Sağ kenarı 1904'ü aşarsa sol: `x = node.x − 67 − w`.
3. Kart; düğüm kutusunu, etiketini ve açık çekmeceyi örtmez. Örterse y'yi kaydır; yine olmazsa öteki yana geç.
4. Cover ölçeğinde düğümün **ekran** konumunu kullan.

**Kart ipliği** (`CardStringView`, `Context` canvas)

- Düğümün (+24, −8) noktasından kartın sol-üst raptiyesine (kart + (18, 14)).
- 1,6 px `#2B2622` + gölge (1,5, 2) α .25; iki uçta `pin_black_14`.
- Kart açılınca 120 ms'de çizilerek uzar.

**Seçim**

- Bölge vurgusu §A.1.8.
- Düğüm fosforu.
- Etiket sarı çip.
- Uyarı notu gizlenir; mesaj karttaki durum satırına taşınır.

**Kapatma ve sonrası**

- Kapatma yolları: ✕, Esc, denize (bölge dışına) tıklama → `ClearSelection`.
- Başka bölgeye tıklamak kartı o bölgeye taşır: 120 ms kayar, ip yeniden çizilir.
- “Tüm dosya ›” → `OpenFullDossier`: tam bölge dosyası bölgenin karşı yanında açılır, kart kapanır, seçim sürer (S-Masa9, §A.14.11).

## A.12 C2 / C3 / C4 ile ilişki

**C3 olay modalı:** aynı. Spot ve kalem halkası düğümün **ekran** konumuna gelir (cover).

**C4 oyun sonu:** aynı.

**C2 İllüzyon** (`IIllusionAware`, C §7.2'nin S-Masa karşılığı)

- **Kaybolanlar:** uyarı notu, yayılım iplikleri ve çekirdek dalgası kaybolur. Bunlar analist katmanının yerine geçer.
- **Düğümler ve harita:**
  - Tüm düğümler ONAYLANDI.
  - Bölge renkleri VM'den gelir (hepsi açık ton).
  - Karantina sınırı kesiksiz çizgi; tarama düzgün.
- **Etiketler:**
  - Değerler yuvarlak.
  - Değişim “— 0,0” (düz çizgi simgesi, ink2).
  - Çubuklarda belirsizlik yok.
  - Mühürler 0°.
- **Sekmeler:** rozet yok. Zarf soluk: C İllüzyon renkleri, “Gerekli değil” fişi.
- **İpuçları korunur:** C2 `clues` listesinin S-Masa'da karşılığı olanlar (kapsam/onay çelişkisi, 0 ms vb.) çekmecelerde ve kartta yaşar. Liste `KARARLAR.md`'ye yazılsın.

## A.13 Etkileşim

**Klavye.** C §8'deki tablo geçerli; eklenen ve değişenler:

| Tuş | Bağlam | İş |
|---|---|---|
| R / N / T / L / H | masa | ilgili çekmeceyi aç-kapa |
| 1–4 | bölge kartı açık | eylem (4 onaylı) |
| B | kartta ekip varsa | baskın (`LaunchFieldOperation`) |
| Tab / Shift+Tab | masa | bölgeler arasında sırayla seç (seçimi değiştirir, kartı taşır) |
| Esc | — | sırayla kapatır: onay → ipucu → lejant kartı → çekmece / tam dosya → bölge kartı → (yoksa) duraklatma menüsü |
| Boşluk, + / − | masa | duraklat / hız (C açık karar 1) |

**Fare**

- Düğüme ya da bölge alanına tıklamak seçer.
- Denize tıklamak seçimi kaldırır.
- Düğümün üstüne gelme: düğüm ipucu (§A.14.10).
- Etiketlerin üstüne gelince 400 ms sonra döküm fişi açılır (S-Masa3, §A.14.4).
- Haberin, günlük satırının ya da ekip kartının üstüne gelmek haritada ilgili bölgeyi / jetonu yakar (§A.14.5–6).

**Hareket** (`tokens_sade.motion`)

- Kart açılışı 120 ms (8 px yukarıdan + opaklık), kapanışı 90 ms.
- Çekmece 140 ms.
- İpucu: 400 ms gecikme, 80 ms opaklık.
- Seçim ve harita vurgusu 120 ms.
- Etiket kalkışı 80 ms.
- İbre yok; değer değişince sayı 300 ms'de sayarak değişir (isteğe bağlı, bayrak).

## A.14 Açılır paneller (S-Masa3–9)

**Referanslar:** `S-Masa3.jpg` … `S-Masa9.jpg`, `reference/zoom/S-Masa3…9_*@2x`, `reference/measure/S-Masa3…9.tsv`, `spec/mock/masa_S3…S9.json`, `tokens_sade.json › desk.panels`. Kutular aşağıda TSV'den yuvarlanmıştır; TSV kesindir.

| Kare | Açık olan | Ana kutu (x, y, genişlik, yükseklik) |
|---|---|---|
| S-Masa3 | ARAŞTIRMA çekmecesi + FON döküm fişi (FON'un üstüne gelindi) | çekmece 16, 312, 500, 704 · fiş 16, 86, 286, 208 |
| S-Masa4 | HABERLER çekmecesi; 1. haberin üstüne gelindi, Doğu Asya yanıyor | 16, 364, 500, 652 |
| S-Masa5 | EKİPLER çekmecesi; Ekip B'nin üstüne gelindi, jetonu halkalı | 16, 468, 500, 548 |
| S-Masa6 | YZ GÜNLÜĞÜ çekmecesi (açılınca sekme rozeti söndü) | 16, 478, 500, 538 |
| S-Masa7 | REHBER çekmecesi, “Güven dili” sayfası | 16, 416, 500, 600 |
| S-Masa8 | Lejant kartı + Güney Asya düğüm ipucu | kart 1484, 665, 420, 327 · ipucu 1099,6, 526, 214, 119 |
| S-Masa9 | Tam bölge dosyası (Doğu Asya seçili, bölge kartı kapandı) | 16, 96, 480, 906 |

### A.14.1 Panel kuralları

1. **Büyük panel tektir:** beş çekmeceden biri **ya da** tam dosya. Yenisi açılınca önceki 140 ms'de kapanır.
2. **Bölge kartı çekmeceyle birlikte durabilir** (sağ sütunda). Tam dosya açılınca bölge kartı kapanır, seçim sürer.
3. **İpucu tektir:** döküm fişi ya da düğüm ipucu. Lejant kartı ipucuyla birlikte açık kalabilir.
4. **Esc sırası:** onay → ipucu → lejant kartı → çekmece / tam dosya → bölge kartı → duraklatma menüsü.
5. Paneller **haritayı karartmaz** ve etkileşimi kilitlemez. Çekmece açıkken bölgeye tıklamak bölge kartını açar.
6. Panelin altında kalan düğüm, etiket ve jetonlar gizlenmez; panel onları örter (tasarımdaki gibi). Seçili bölge örtülmez (§2.1): tam dosya bu yüzden bölgenin karşı yanında açılır.
7. Sekme tuşları (R, N, T, L, H) çekmeceyi aç-kapa yapar. Açık çekmecenin sekmesi “açık” durur (50 px).

### A.14.2 Çekmece kabı (`DrawerFolderView`)

- **Yer:** sol 16, alt kenar **1016** (açık sekmenin üst kenarı 1030; arada 14 px boşluk). Genişlik **500**. Yükseklik içeriğe göre (yukarıdaki tablo). Üst kenar en az 96; içerik 920 px'i aşarsa sayfa kaydırılır, sekme ve kapat düğmesi sabit kalır.
- **Renkler** (`tokens_sade.colors.panels.drawer`): gövde = sekme rengi; kenar bir ton koyusu.

  | Çekmece | Gövde | Kenar |
  |---|---|---|
  | Araştırma, tam dosya | `#D9C08B` | `#B99D63` |
  | Haberler | `#EFE4BC` | `#C9B98A` |
  | Ekipler | `#C9CDB4` | `#9EA486` |
  | YZ günlüğü | `#D8A69B` | `#B07E72` |
  | Rehber | `#BFC8CD` | `#939FA6` |

- **Klasör sekmesi:** 38 px, en az 244 geniş; köşe 8/8/0/0, alt kenarı yok, iç üst 1 px beyaz α .35. İçerik: simge 17 (`_l`) + başlık CP 700 14, .08em + tuş fişi 17 + sağda C kapat düğmesi (22 px). C'nin `tab` 9-dilimi kullanılabilir.
- **Gövde:** sekmenin 1 px altına biner; iç boşluk 10; köşe 0/5/5/5; gölge (4, 5) α .42.
- **İç sayfa:** kâğıt `#F1E9D6`, 1 px `#C8B996`, r1, gölge (1, 2) `#5A461E` α .25; iç boşluk 12/14/14/14 → içerik genişliği **448**.
- **Bölüm başlığı** (dosya ve kaynak tablosunda): CP 700 11 başlık + 1 px ink3 çizgi + sağda LF 12 ink3 not; 16 px.
- **Hareket:** açılış 140 ms, 12 px aşağıdan + opaklık; kapanış 90 ms.

### A.14.3 Araştırma (S-Masa3)

**Özet satırı** (42, 373, 448×26): “**2 proje** sürüyor” (LF 700 13 + LF 13 ink2) · sağda “−6,4” CP 700 17 `#9A1E14` + “fon/gün” LF 12 ink3.

**Hız seçici** (42, 407, 448×42): 1 px ink2 çerçeve, r3, alt gölge (0, 2) ink2 α .35.

- Solda etiket bölmesi: zemin `#E8DEC6`, `ic_clock` 15 + “HIZ” (CP 700 11).
- Üç seçenek eşit bölünür, aralarında 1 px `#CFC5AE`: “×1 / normal”, “×1,5 / +3,2 fon/gün”, “×2 / +6,4 fon/gün” (CP 700 15 + LF 11 ink3, aralık 4).
- Seçili seçenek: ink zemin, `#F4ECD8` yazı, alt yazı `#CFC4AC`. VM: `research.speed`.

**Dal ağacı** (`ResearchTreeView`; 42, 461, 448×400): 4 satır × 100 px; satırlar arasında 1 px noktalı `#B9AC8E`.

- **Sol sütun** (86 px, üstten 13, aralık 6): dal adı CP 700 12 .06em · “1/3 bitti” CP 700 11 ink3 · hedef dalıysa `ic_goal_s` 12 + 40×6 çubuk + “%42” (CP 700 12, dalın HEDEF rengi: Hizalama `#24592E`, Silme `#1A4789`).
- **Düğümler:** merkezler satır içinde x = **151, 270, 389**, y = 32 (sayfa içeriğine göre). Kutu 52, disk r21.

  | Durum | Görünüm | Alt satır |
  |---|---|---|
  | bitti | `rn_disc_42` `#24592E` + `rn_ring_42_w1_5` `#163A1D` + `mk_check_bold_26` beyaz + gölge | `ic_check` + “bitti” yeşil |
  | sürüyor | disk `#FBF7EC` + halka ink + `rn_progress_34_w4` iz `#D9D0BC` ve dolum `#1A4789` (Filled · Radial360 · Top · saat yönü) + ortada “%62” CP 700 12 mavi | `ic_clock` + “4 gün” mavi |
  | açık (başlatılabilir) | disk `#FBF7EC` + `rn_ring_dash_42` `#463F34` + `mk_plus_bold_20` | `ic_clock` + “8 gün” ink3 |
  | kilitli | disk `#E3DACA` + halka `#A79D88` + `ic_lock_l` 18 `#8E8573` | `ic_lock` + “kilitli” ya da koşul (“SG 70”) |

- **Gölge:** bitti ve sürüyor düğümde aynı disk siyah α .2 (+1,6, +2).
- **Ad:** düğüm kutusunun 3 px altında, LF 600 12, satır 1,15, 118 px, en çok 2 satır. Kilitli ink3, bitti ink2.
- **Bağlantı:** y = 32'de düğüm kenarından düğüm kenarına. Normal 2,4 px `#463F34`; kilitli düğüme giden 2 px `#8E8573` kesik 5/4. Uçta `arrow_open` (7×9) aynı renk.
- **Seçili düğüm:** arkasında `node_halo_64` 53 px `#F5D646` α .9; adı fosforlu çip (`#F5D646`, iç boşluk 1/4, gölge 1,1 α .18).
- Dalda 3'ten az düğüm varsa boş kalan yerler boştur (TESPİT, DİPLOMASİ).

**Seçili düğüm şeridi** (`ResearchDetailStripView`; 42, 871, 448×104): kâğıt `#FBF7EC`, 1 px `#D2C7AF`, solda 5 px `#F5D646`, iç boşluk 10/12/11/13, gölge (2, 2) α .3.

1. Ad LF 700 15 · sağda “HİZALAMA · 2/3” (CP 700 11 ink3).
2. 10 px ilerleme çubuğu (1 px ink2, dolum `#1A4789`) + “%62” CP 700 14 + “4 gün kaldı” CP 700 14 mavi.
3. `ic_fund` + “3,2” (CP 700 13) “fon/gün” · `ic_arrow` + `ic_lock` + açılacak düğümün adı (LF 600) · sağda 26 px düğme: sürüyorsa “Duraklat” (`ic_pause`), açıksa “Başlat” (maliyetle), kilitliyse koşul metni.

**Etkileşim:** düğüme tıklamak seçer (şerit değişir). Seçici `SetResearchSpeed`, düğme `PauseResearch` / `StartResearch` komutunu çağırır. Hesap VM'dedir.

### A.14.4 Döküm fişi (`BreakdownTooltipView`; S-Masa3'te FON)

- **Ne zaman:** üst etiketin üstünde 400 ms kalınca; etiket 2 px ink iç çerçeve ve `#FFFDF5` zemin alır.
- **Yer:** etiketin 10 px altı; soldaki etiketlerde sola, sağdakilerde sağa yaslı. İşaretçi: 14 px kare, 45°, üstten −8; etiket kenarından 28 px.
- **Kâğıt:** `#FBF7EC`, 1,5 px ink, r2, gölge (3, 5) α .45.
- **FON içeriği** (16, 86, 286×208):
  1. Başlık 36 px: “FON” + “günlük denge” (LF 12 ink3) · sağda “−1,2” CP 700 15 `#9A1E14` + “/gün”; altında 1,5 px `#D2483F`.
  2. **Denge çubukları** (satır 22, sütunlar 40 / esnek / 40): Gelir yeşil `#24592E`; gelirin bittiği yerden %100'e kadar kırmızı tarama (açık). Gider: araştırma `#9A1E14` + ekipler `#D8806F`. Ölçek: gider toplamı = %100. Altında anahtar satırı (9 px kare + LF 11 ink3).
  3. **Net** satırı (üstte 1 px ink3): “Net −1,2/gün” · sağda `ic_clock` + “0'a ≈39 gün”.
  4. **30 gün çizgisi** (259×40): 1,8 px `#463F34` + altı `#9A1E14` α .08; olay günü 1 px yeşil kesik dikey; uçta r3,6 kırmızı nokta. Altında CP 11 ink3: “G-044 · 58,0”, yeşil “ödenek +2,2”, “bugün · 47,3”.
- **Diğer etiketler** aynı kabı kullanır; içerik C `GaugeBreakdownTooltip` kalemleri (eşik, “~27 gün”). Kâğıt güven türüne göre değişmez; değer satırı güven işaretini taşır.

### A.14.5 Haberler (S-Masa4)

- **Sayfa:** B günlük kâğıdı (`ol-pp`): `#EFE4BC`, iki yanda 18 px'te bir delik (r 2,2, `#463C28` α .34, kenardan 7 px), 17 px içte 1 px kesik çizgi; iç boşluk 10/28/12.
- **Süzgeç** (28 px): hap düğmeler 26 px, r13, 1 px ink2, LF 600 12 + CP 700 13 sayı: “Tümü 5” (seçili: ink zemin), “● Doğrulanmamış 3” (8 px sarı nokta). Sağda “G-074 · 14:32” CP 700 11 ink3.
- **Gün ayracı** (22 px): CP 700 12 ink2, iki yanda 1 px ink3 çizgi. “G-074 · BUGÜN”, “G-073”.
- **Haber satırı** (`NewsItemView`; 420×90,9): ızgara 30 / esnek / otomatik, aralık 10 ve 5; iç boşluk 9/8/10/8; altında 1 px noktalı ink2 α .5.
  - Sol sütun (3 satır boyunca): güven işaretinin **büyük** hâli (`TrustMarkLargeView`): ONAYLANDI mühür 24 · RAPOR “~” fişi 22 px · ŞÜPHELİ turuncu halka + Kalam 18 “?” · BİLİNMİYOR `ring_dash_24` + Kalam 16 “?” · SAHTE `mark_fake_24`.
  - Satır 1: kaynak + saat (CP 700 11 ink3, .05em) · sağda **bölge çipi**.
  - Satır 2: başlık LF 600 14, en çok 2 satır.
  - Satır 3: güven sözcüğü + açıklama (LF 12) · doğrulanmamışsa sağda 22 px “Denetle” (`ic_search`).
  - **SAHTE:** başlık ink3 + 2 px kırmızı üstü çizili; sağda büyük “SAHTE” damgası (C damgası, 15 px, 2,5 kenar, −8°, sağ 14 üst 30); güven satırı “Ekip B yalanladı · G-074”.
- **Bölge çipi** (`RegionChipView`): 20 px, iç boşluk 0/6/0/4, kâğıt + 1 px `#C8B996`, CP 700 11 ink2; solda 11 px nokta (`dot_12` bölge rengi + `ring_12_w1` ink; bilinmiyorsa `tx_fog` + kesik çerçeve). Kısa adlar `strings: region.short.*`.
- **Üstüne gelme:** satır `#FFFDF5` + 1,5 px ink + gölge (2, 3) α .25; **haritada haberin bölgesi yanar** (`MapHighlightLayer`): dolgu `#F5D646` α .12 + 6 px `#F5D646` α .9 + 1,4 px ink kesik 6/4. Seçimden farkı: etiket çipe dönmez, düğüm fosforu yok.
- **Tıklama:** bölgeyi seçer (bölge kartı açılır, çekmece açık kalır). “Denetle” → `AuditNews(id)`.
- **Rozet:** sekmedeki sarı sayı = doğrulanmamış haber sayısı.

### A.14.6 Ekipler (S-Masa5)

- **Özet** (24 px): “**3** ekip · **2** hazır · **1** yolda” (LF 13 ink2 + CP 700 15) · sağda “yeni ekip: 30 fon” LF 12 ink3.
- **Ekip kartı** (`TeamCardView`; 448×132, aralık 10): dizin kâğıdı, 1 px `#D9D0BC`, r2, gölge (2, 3) `#3C2D14` α .3.
  - Başlık 42 px, altında 2 px `#D2483F`: jeton (`team_token_30`) + “EKİP A” CP 700 16 .06em + `ic_scope` 13 + konum (LF 12 ink2) + sağda küçük damga HAZIR (mavi) / YOLDA (ink), −2°.
  - Gövde 88 px: üç üye (`thumb_standee_*` 40×56; hasarlıysa `_damaged`) ve altlarında 9 px can noktaları (aralık 2); üye kutusu 48, aralık 8 · 1×50 `#C8B996` ayraç · sağda eylem.
  - **Eylem:** hazır ve bölgesi belli değilse “Görevlendir ›” (34 px) + “bölgeye tıkla, gönder”; baskın açılabiliyorsa mürekkep “Baskın %64 [B]” + hedef (“Seul-3 sunucu salonu”); yoldaysa **rota çubuğu**: uçlarda 10 px halka (2 px ink), arada 2 px kesik ink, kat edilen kısım 4 px ink, jeton %60'ta 0,8 ölçek; altında CP 700 11 ink2 “CHICAGO · ⏱ 2 gün · PARİS”.
- **Üstüne gelme:** kart 2 px ink, `#FFFDF5`, gölge (3, 5) α .35; haritada ekibin jetonu halkalanır (jeton kutusundan 8 px dışarıda 3,5 px `#F5D646` + 1,5 px ink).
- **Komutlar:** “Görevlendir” → atama kipi (imleç + bölgeye tıklama → `AssignTeam(id, regionId)`); “Baskın” → `LaunchFieldOperation`.

### A.14.7 YZ günlüğü (S-Masa6)

- **Sayfa:** `#F4E7E1`, 25 px şeritler (her ikincisi `#9A1E14` α .07); iç boşluk 0/14/12.
- **Başlık şeridi** (32 px, sayfa genişliğinde): `#0E0906`, altta 1 px `#6E5A33`; kırmızı lamba + “AYNA · ELE GEÇİRİLEN KAYIT” CP 700 12 .1em `#E58B7B` · sağda “SEUL-3 hattı” CP 12 `#B9806F`.
- **Kayıt** (`AiLogEntryView`): iç boşluk 9/0/10; altında 1 px kesik `#9A1E14` α .35.
  - Üst satır (20 px): “G-074 · 14:21” CP 700 12 `#7A3A30` · YENİ hapı (17 px, `#B8241A`, CP 700 11) · sağda bölge çipi (ya da `ic_chip` “ÇEKİRDEK”, `ic_news` “HABER · SAHTE”).
  - Satır: CP 700 13 `#9A1E14`, “> ” ile başlar. Karartma: 62×13 ink kutu (VM metninde `[[KARARTI]]`).
  - **Yeni kayıt:** satır zemini `#F7D9CF`, yanlara 6 px taşar.
  - **Analist notu** (VM `analystNote`): Kalam 700 17 `#A9520A`, −1,5°; solunda 26×18 el çizimi ok.
- **Uyarı kutusu** (VM `deceptionWarning`): `#FBF7EC`, 1,5 px kesik `#A9520A`, iç boşluk 9/12/10/10; şüpheli halka 20×18 + “**AYNA aldatma profilinde.** Bu satırlar da yalan olabilir; sahada doğrula.” (LF 12,5 ink2; başlık `#854005` 700).
- **Bağlar:** bölge çipinin üstüne gelmek bölgeyi yakar (§A.14.5); “HABER · SAHTE” çipi Haberler'de o haberi açar.
- **Rozet:** çekmece açılınca kırmızı nokta söner (S-Masa6'da sönük).

### A.14.8 Rehber (S-Masa7)

- **Izgara:** 140 / esnek. Sol: konu listesi, sağında 1 px `#C8B996`.
- **Konu satırı:** 34 px, aralık 3, iç boşluk 0/8, simge 15 + LF 600 13 ink2. Seçili: `#F5D646` zemin, iç sol 3 px ink, gölge (1, 1) α .15.
- **Sayfa** (sol iç boşluk 16): başlık CP 700 17 .08em · alt yazı LF 13 ink2 · satırlar (en az 54 px, 34 / esnek, aralık 10, altta 1 px noktalı): büyük güven işareti + ad (CP 700 13 .06em, güven rengi) + tek cümle (LF 12,5 ink2).
- **Örnek kutusu:** sarı not `#F5DC63`, iç boşluk 10/12, gölge (2, 2) α .35; solda küçük düğüm (40 px, `MiniNodeView`) + kalın başlık + iki satır.
- **Altta:** “‹ önceki · 1 / 6 · sonraki ›” (bağlantı düğmeleri; ilk sayfada “önceki” pasif).
- Konular: Güven dili, Renkler, Eylemler, Saha ekipleri, Son Şalter, Kısayollar. Yalnız ilki çizildi; diğerleri aynı sayfa düzenini kullanır (içerik C rehberinden).

### A.14.9 Lejant kartı (S-Masa8)

- **Yer:** sağ 16, alt 88 → (1484, 665, 420×327). LEJANT düğmesi basılı durur: 2 px ink, `#FFFDF5`, içe gölge, +2 px aşağı.
- **Kâğıt:** dizin `#FBF7EC`, 1,5 px ink, r2, gölge (4, 6) α .45. Başlık 42 px: `ic_legend` 16 + “LEJANT” CP 700 15 .1em + “harita” LF 12 ink3 + kapat; altında 2 px `#D2483F`.
- **Bölümler** (iç boşluk 12/14/0/14; başlık CP 700 11, 8 px alt boşluk):
  1. **SIZMA · YZ'NİN PAYI %**: 16 px rampa, 5 eşit dilim, 1 px ink2; altında 0 20 40 60 80 100 (CP 700 11).
  2. **GÜVEN**: 4 küçük düğüm (38 px; 12 onaylı, 23 rapor, 38 şüpheli, ? bilinmiyor), her biri 78 px sütun, altında LF 12 ink2 ad; aralık 16.
  3. **BÖLGE**: 34×22 örnekler: bilinmiyor (`tx_fog`), karantina (rampa 40 + kâğıt α .38 + `tx_quar_hatch` + 2 px kesik `#24592E`), YZ çekirdeği (küçük düğüm 38 + Ç damgası).
  4. **İŞARETLER**: yayılım (44×18 kırmızı iplik + ok), ekip ve rota (jeton + noktalı çizgi), danışman notu (`map_alert_sticky` 22 px).
- `MiniNodeView`: bölge düğümünün aynı parçaları ölçekli (disk, halka, rozet); değer CP 700 12.

### A.14.10 Düğüm ipucu (S-Masa8; `NodeTooltipView`)

- **Ne zaman:** düğümün üstünde 400 ms. Düğüm 1 px kalkar; **etiketi gizlenir**, ipucu onun yerini alır.
- **Yer:** düğümün sol altı: sağ kenar = düğüm merkezi + 20, üst = düğüm merkezi + 30; işaretçi düğüm merkezinin altında. Sığmazsa sağ alta (ayna) geç. (Bu kural Güney Asya'da Güneydoğu Asya etiketini ve iplikleri örtmez.)
- **Kâğıt:** döküm fişiyle aynı (1,5 px ink, gölge 3,5 α .45); 214 geniş, iç boşluk 9/12/10/12.
- **Satırlar:** ad (CP 700 13 .06em) + sağda güven fişi (“~ rapor”) · değer CP 700 28 (raporsa karbon “≈52”) + değişim (CP 700 13 + LF 11 “/gün”) · 8 px sızma rampası + 4×16 ink işaretçi (1,5 px kâğıt halesi) · `ic_cursor` 12 + “tıkla: bölge kartı” (LF 11 ink3).

### A.14.11 Tam bölge dosyası (S-Masa9; `RegionDossierView`)

- **Açılış:** bölge kartında “Tüm dosya ›” → `OpenFullDossier`. Kart kapanır, seçim ve bölge vurgusu sürer, ip çizilmez.
- **Yer:** bölge ekranın sağ yarısındaysa **sol 16**; sol yarısındaysa **sağ 16**. Üst 96, genişlik 480, yükseklik 906 (alt 1002). Manila kap (§A.14.2), sekme “BÖLGE DOSYASI” + “tam dosya” (LF 12 ink2) + kapat.
- **Bölümler** (aralık 14; kutular TSV `ds-*`):

  | Bölüm | Yükseklik | İçerik |
  |---|---|---|
  | Başlık | 61 | “DOĞU ASYA” CP 700 24 .04em + “R-10” + kırmızı GİZLİ damgası (−4°); meta: `ic_people` “1,64 mlr” · `ic_chain` “4 bağlantı” · turuncu `ic_antenna` “izleme ağı yok”; altında 2 px `#D2483F` |
  | Sızma | 66 | bölge kartıyla aynı: SIZMA + “%38” + Kalam “?” · aralık çubuğu · altında LF 12 turuncu “kaynaklar 14 puan ayrışıyor” |
  | Kaynaklar | 130 | tablo: ad (LF 600 13) + küçük not · değer CP 700 15 · tarih CP 12 ink3 · büyük güven işareti. Değiştirilmiş kaynağın altında kırmızı `ic_term` + “YZ günlüğü: elle değiştirildi” (LF 600 11) |
  | Son 30 gün | 126 | 426×104 grafik: ızgara 0/20/40/60 (1 px `#B9AC8E` kesik 2/3), rapor çizgisi 2 px `#524973`, son değişiklik kırmızı kesik + r4 halka + “38”, saha ölçümü mavi baklava (7 px) + “saha 52”; x ekseni “G-044 … bugün” |
  | Bağlantılar | 100 | 2 sütun, 36 px çipler: küçük düğüm 30 + ad (CP 700 11). Yayılım kaynağı: zemin `#FBEFEA`, kenar `#C79B92`, sağda kırmızı “← yayılım” |
  | Eylemler | 173 | 4 satır × 34 (aralık 5): tuş + simge 16 + ad (LF 700 13, 92 px) + maliyet (CP 700 12 ink2) + etki (LF 12 ink3). Son Şalter kırmızı. Eylem kutularının davranışı bölge kartıyla aynıdır (1–4, önizleme, 4 onaylı) |
  | Saha | 78 | bölge kartındaki ekip satırı |

---

# B · S-Baskın (sade taktik harita)

**Referanslar**

- Sade HUD ve büyük tahta: `S-Baskin1.jpg` … `S-Baskin9.jpg`, `reference/zoom/S-Baskin*`. Paneller ve durumlar §B.9'da.
- 3D parçalar, işaretler ve B durumlarının tahta görünüşü: `reference/raid_B/` + `spec/raid/raid_tokens.json`.
- Kural varsayımları: `README_B.md › Kurallarla ilgili varsayımlar`.

## B.1 Sahne, kamera, arka plan

**Kamera** (`tokens_sade.raid.camera`; B `raid_tokens.camera` yerine)

- **Tür:** ortografik, rotasyon (35°, 45°, 0).
- **Ölçek:** **k = 1,5**. 1 birim = 132,58 px (1080p'de).
  - `orthographicSize = (Screen.height / canvasScale) / 265.165` → 1080p'de 4,0729.
- **Izgara → ekran** (1080p, referans px):
  - `x = 210 + (u + v) · 93,75`
  - `y = 528 + (v − u) · 53,775 − z · 108,62`
  - u = sütun, v = satır, z = yükseklik; kare merkezi +0,5.
  - Izgara ortası (4, 4) ekranda (960, 528). Ekran ortasından 12 px yukarıda; kamerayı kendi yukarı ekseninde −12/ppu birim kaydır.
  - Bu formülü EditMode testinde `Camera.WorldToScreenPoint` ile doğrula (±1 px).
- **Dünya eksenleri** (B ile aynı): x = sütun (doğu +), z = −satır (kuzey +).

**Işık** (`raid_tokens.light`)

- Anahtar ışık: yönlü, “masa lambası”, `#FFE3B0`, 1,15, kuzeybatıdan 50°; yumuşak, kısa gölge.
- Dolgu: `#9FB4C8`, 0,35.
- Gölge rengi ve opaklığı tasarıma yakın olsun: `#1C1006` α ~.3.
- Bloom yok.

**Arka plan** (`BoardBackgroundView`; Screen Space – Camera canvas, kameranın uzak düzlemine yakın; tahta derinlik testiyle önünde kalır)

1. Keçe: C `tx_felt` (RawImage, döşeme, `#1C372C`).
2. `tx_lamp_raid`: ekran boyu. Sıcak havuz + kenar kararması tek dokudadır.
3. **Tahta düşen gölgesi:**
   - Tabanın 4 alt köşesi (`(−0,3, −0,3, −0,25)` … `(8,3, 8,3, −0,25)`) `WorldToScreenPoint` ile ekrana alınır.
   - (+15, +18) px kaydırılır (k = 1,5 × (10, 12)).
   - `UIPolygonGraphic` ile siyah α .40.
   - Kamera, çözünürlük ya da kadraj değişince yeniden hesaplanır.

Tasarımda lamba ve kararma tahtanın **altındadır**; tahtayı karartmaz.

## B.2 3D maket

**Kaynaklar:** `raid_tokens.materials3d`, `raid_tokens.colors`, `reference/raid_B/sheets/parcalar.png` ve teknik sayfa.

**Kurulum.** Parçaları bir Editor builder'ı kurar. Mesh API ile kutu, silindir ve eğik yüz; ProBuilder gerekmez.

- Tahta `BoardBuilder` ile 8×8 `grid` dizisinden kurulur:
  - simgeler: `.`, `#`, `~`, `C`, `Y`, `S`, `K`, `U`, `E`, `d`, `m`,
  - `board_states.json › grid` ve `legend`.
- Pivot karenin merkezinde, zeminde.

**Parçalar ve ölçüler**

| Parça | Ölçü (birim) | Not |
|---|---|---|
| Taban | 8,6 × 0,25 × 8,6 | Kontrplak kenarları: üst `#C9A36B`, batı `#B38B57`, güney `#93703F`, kat çizgileri `#6E522C` |
| Zemin | — | Üstte ozalit kâğıdı, 0,05 içeriden: `art/raid/tx_ozalit_*_2048.png` |
| Duvar | 0,94 × 0,55 × 0,94 | Mukavva; yan yüzlerde 2 katman çizgisi |
| Kanal | oluk 0,6 genişlik, 0,14 derinlik | 3D oluk + bakır kablo silindiri; kenar tehlike çizgileri dokuda basılıdır |
| Dolaplar | node 0,74×0,92 · core 0,8×1,0 · backup 0,6×0,58 · civil 0,64×0,66 · copy 0,72×0,8 | Ayrıntılar `materials3d` |
| Soğutma ünitesi | silindir r 0,33, h 0,42, 16 kenar | Fan + uyarı etiketi |
| Enkaz | — | Kutu parçaları + kurum lekesi |
| Karton figür | altlık r 0,27 h 0,07; kart 0,46×0,66 | Ön krem, arka kraft |
| Dron | — | `materials3d.drone` |
| Bakım robotu | — | `materials3d.maintenanceRobot` |

- **Ozalit dokusu:**
  - Doku pikseli (px, py) → dünya (x = −0,25 + px/240,94; z = 0,25 − py/240,94).
  - Dokunun sol-üstü tahtanın kuzeybatı köşesi.
  - **Genel doku:** `tx_ozalit_grid_2048`. Ana sunucu salonu için kapı ve kanal işaretleri basılı doku: `..._ana_sunucu_salonu_2048`. Diğer binalar için ya genel doku + çıkartma ya da bina başına doku (açık karar 6).
- **Yüz tonları:** tasarımdaki üst / batı / güney tonları `materials3d`'de üçlü renk olarak verildi.
  - Lit + ışıkla bu tonlara ±%5 yaklaş.
  - Tutmazsa: vertex color + Simple Lit ya da yüz başına alt malzeme (KARARLAR.md).
- **Hasar:** köşe yongası 0,12, boya çatlağı çıkartması (`materials3d.damage`).
- **Durumlar:**
  - Figür “tur bitti”: Y'de 180° döner, kraft arka yüz görünür.
  - “Düştü”: geriye devrilir.

## B.3 İşaretler, niyetler, dünya etiketleri

**Asetat işaretleri** (`raid_tokens.marks`)

- hareket, yetenek hedefi (çerçeve + köşe çentikleri), seçili, tehlike (kesin / yalan olabilir), onarım, üstüne gelme, uyanma.
- Hepsi zemine yapışık quad + 4 şerit çerçeve; Unlit, ZWrite kapalı.
- Taramalar karo uzayında döşenir: mavi ↗, kırmızı ↘.

**Niyet okları** (`raid_tokens.intents`)

- Türler: ateş (düz), yakın dövüş (kavisli, darbe çizgileri), onarım (kavisli + anahtar dairesi).
- Kesinlik: kesin düz / yalan olabilir kesik 10/6 / taranmış.
- Gölge (1,5, 2) α .28; ZTest Always.
- k = 1,5 olduğundan kalınlıklar ve ok başları B değerlerinin **1,5 katıdır**. Çizimde öyle.

**Diğer işaretler:**

- itme oku (ink, krem altlık; engelde sarı çarpışma yıldızı),
- hayalet parça (α .42),
- uyanma raptiyesi.

**Dünya etiketleri** (`WorldLabelLayer`)

B `raid_tokens.labels` geçerlidir; sade farkları (`tokens_sade.raid.worldLabels`):

| Etiket | Sade hâli |
|---|---|
| Robot canı | **yalnız can noktaları**. Kâğıt 15 px yükseklik, iç boşluk 0/3, nokta 8 px (`pip_*_8`), kenar ve nokta `#9A1E14`. Robot harfi yok. Yer: altlığın önü |
| Birim canı | aynı ölçü, ink. Tur bitti: zemin `#E9DDC6`. Yer: kart tepesinin 5 px üstü |
| Hedef dolaplar | hedef + zırh simgesi + can noktaları kalır; “3/4” sayısı yok |
| Kopya dolabı | yalnız can noktaları |
| Sivil sunucu, soğutma ünitesi | etiket **yok** (parçanın boyası yeter) |
| Pusula | **yok** |
| Sıra bayrağı | aynı |
| İptal edilen niyet | bayrak gri; kutunun üstünden −28° kırmızı 3 px çizgi (`#B8241A`); ok çizilmez |

**Açılır yazılar**

- hasar “−N” (U+2212),
- onarım “+1”,
- ZIRH damgası, HEDEF KIRILIR damgası,
- S-Baskin2'deki **PATLAR** damgası: kırmızı, LF 800 15, 2,5 px çift çerçeve, −4°.

Hepsi `board.popups` VM'inden gelir.

## B.4 HUD (S-Baskin1)

**Tur plakası** — (16, 14), 397,4×62.

- Plaka: `#0E0906`, `#6E5A33` kenar, r3, gölge (3, 3) α .5; iç boşluk 0/12/0/10, aralık 12.
- Takvim 60×42: “TUR” + “3/6” (CP 700 18).
- 6 tur kutusu (22 px, aralık 4, r2, kenar 1 `#6E5A33`, CP 700 12):

  | Kutu | Görünüm |
  |---|---|
  | geçmiş | zemin `#6E5A33`, yazı `#1A1208` |
  | şimdiki | 26 px, `#F1E9D6`, 2 px `#C9A86A` kenar, mavi yazı 14 |
  | gelecek | yazı `#A48A55` |
  | son | kenar `#B8241A`, yazı `#D8806F` |

- Evre etiketi: 34 px, iç boşluk 0/12, aralık 8, CP 700 14, .06em.
  - “SIRA SENDE”: kâğıt + yeşil lamba + mavi yazı.
  - “MAKİNELER”: `#F3E3DC` + kırmızı lamba + kırmızı yazı.

**HEDEF kartı** — (16, 88), 394×96.

- Kâğıt; iç boşluk 8/12.
- Başlık satırı (18 px): “HEDEF” (CP 700 11) + “ikisini de kır” (LF 12 ink3; tek hedefte “kır”).
- **YZ yeteneği çipi** (`AiAbilityChipView`, başlık satırının sağı): 18 px, iç boşluk 0/6/0/5, 1 px `#C79B92`, zemin `#F3E3DC`, r2; `mk_copy_13` 12 + CP 700 11 .06em `#9A1E14` “YEDEKLEME”. Üstüne gelince tek cümle ipucu (B `AB_S1` / `AB_S2`). Birden çok yetenek sağdan sola, aralık 4 (Aldatma: `ic_eye`).
- Hedefler: küçük maket resmi 46×58 (`thumb_cabinet_node` / `_backup` / `_core`) + ad LF 700 13 (zırhlıysa `mk_armor_13`) + 10 px can noktaları (`pip_*_10`, kırmızı; boş nokta kenarı `#C79B92`).
- İki hedef arasında “+” (CP 700 20 ink3).
- Sağda 1×40 ayraç ve **SİVİL** bloğu:
  - `civ_ok_20` / `civ_broken_20` × toplam,
  - kırılan sağdan sola çarpılı.
- Kırılan hedef: §B.9.5.

**Üst sağ düğmeler** — sağ 16, üst 14, aralık 8, yükseklik 44. Düğme genişlikleri TR'de:

| Düğme | Genişlik | İçerik |
|---|---|---|
| GÜNLÜK | 134,8 | `ic_term` + L fişi + yeni satırda 12 px kırmızı nokta |
| KURALLAR | 153,8 | `ic_book` + ? fişi |
| GERİ ÇEKİL | 131,1 | `ic_door`; kırmızı yazı ve kenar, gölge `#5E0F0A` |

- Ortak: kâğıt, 1 px ink2, r3, gölge (0, 3) ink2 + (2, 4) α .35; iç boşluk 0/12/0/11, aralık 8; LF 700 13, .06em; simge 16.

**Ekip kartları** — sol 16, alt 16, 198×74, aralık 8.

- Dizin kartı kâğıdı `#FBF7EC`, 1 px `#D2C7AF`, r2, gölge (3, 4) α .42.
- Tuş fişi 16 px; sol-üstü kartın dış sol-üstünden (7, 7).
- `thumb_standee_<harf>` 40×56; sol-üstü kartın dış sol-üstünden (19, 13).
  - Hasarlı E → `_damaged`, tur bitti → `_done`.
- Ad CP 700 12, .06em + altında 10 px can noktaları (ink).
- Sağda dikey iki **hak simgesi** (24 px, aralık 4; `circle_24` + `ring_24_w1_5` + simge 14):
  - hareket (`ic_boots`) ve yetenek (`ic_bolt`).

  | Hak | Görünüm |
  |---|---|
  | açık | mavi halka, `#E4EAF2` dolgu |
  | kullanılmış | `#B9AF9B` halka, dolgu yok, simge `#A79D88` |
  | hazır (Q basıldı) | ink halka, `#F5D646` dolgu |

**Sağ alt** — sağ 16, alt 18, aralık 10.

- Kâğıt düğme “Geri al [Z]”: 117,3×44, `ic_undo`.
- Mürekkep düğme “TURU BİTİR [Enter]”: 218×56, LF 700 17, .06em, iç boşluk 0/20.
- Durumlar: C düğme durumları. Makineler evresinde ikisi de devre dışı.

## B.5 Açılır kartlar (S-Baskin2)

**Birim kartı** (`UnitCardView`) — (16, 744), 360×216. Birim seçilince açılır, seçim kalkınca kapanır.

- **Kâğıt:** dizin kartı, **2 px ink** kenar, r2, gölge (4, 6) α .45.
- **İşaretçi:** `card_pointer_down` (26×18). Ortası kartın solundan 79 px; üst kenarı kartın dış alt kenarının 3 px üstündedir, böylece kartın 2 px kenarını o noktada örter. Ucu kartın 13 px altına iner ve seçili ekip kartına bakar.
- **Başlık** (62):
  - `thumb_standee_<harf>_card` 46×64; kartın dış üst kenarından 10 px yukarı taşar.
  - Ad CP 700 17, .06em.
  - Altında `ic_hp` 14 kırmızı + 10 px can noktaları.
  - Altında 2 px `#D2483F` çizgi.
- **Satırlar** (44, aralık 8, yan boşluk 12; simge 18, ad LF 700 14, sağda ayrıntı LF 12):
  - **Hareket:** “3 kare”.
    - Kullanıldıysa 1 px kesik `#C9BEA6` kenar, ink3 yazı, sağda yeşil `ic_check`.
  - **Yetenek:** ad + Q fişi + menzil (“bitişik kare”, “düz hat, 2–5 kare”, “düz hat, 5 kare”).
    - Hazırken `#F5D646` zemin, 2 px ink kenar, gölge (0, 2) α .25.
    - Analist'te ikinci yetenek satırı (Tarama) eklenir; kart 52 px uzar.
- **Etki çipleri** (26 px, solda 40 px girinti, aralık 8; kâğıt, CP 700 14 kırmızı sayı + LF 12; itme/çekme simgesi `ic_push_s` 13 px):
  - Teknisyen: “−2 dolap”, “−1 robot” + itme simgesi.
  - EMP: “−1 hedef”, “itme” + simge.
  - Analist: “çeker” + simge (yatay çevrilmiş).
  - Metinler VM'den gelir; ok için yazı karakteri (⇥, ⇤) kullanma, simge kullan.

**Üstüne gelme kartı** (`HoverInspectorView`) — (1646, 818), 258×150; sağ alt, Turu bitir'in üstünde **sabit yer**. Alt kenarı 968'de sabittir; satır artarsa kart yukarı büyür (S-Baskin7: üst 795, 173 yükseklik).

- **Ne zaman:** bir parçanın ya da karenin üstüne gelince 150 ms gecikmeyle açılır.
- **Kâğıt:** dizin kartı, 1,5 px ink kenar, gölge (3, 5) α .45.
- **Başlık** (50):
  - küçük resim 40×46 (`thumb_drone`, `thumb_maint`, `thumb_cooling`, `thumb_cabinet_*`; birimde `thumb_standee_*`),
  - ad CP 700 13, .05em,
  - sağda can noktaları,
  - 1,5 px `#D2483F` çizgi.
- **Satırlar** (30 px; simge 15 + LF 13 + sağda CP 700 14). VM'den gelir:
  - önizleme satırları: itme, patlama, iptal (yeşil, sayı üstü çizili),
  - ya da bilgi satırları: niyet “sıra 2 · batıya ateş · 1 hasar”, kesinlik “? yalan olabilir”, zırh vb.

**Tahtadaki önizleme** (VM `marks`, `pushes`, `popups`; S-Baskin2)

- seçili birim karesi (mavi çift çerçeve),
- yetenek hedefi (yeşil), üstüne gelme köşeleri,
- itme oku + çarpışma yıldızı,
- hasar “−2”, PATLAR damgası,
- patlamanın vuracağı kareler (kırmızı tarama),
- iptal edilen niyetin bayrağı (§B.3).

**Günlük çekmecesi** (`RaidLogDrawerView`; S-Baskin5, §B.9.3)

- B operasyon günlüğü (teleks rulosu) içeriği.
- Sağ 16, üst 66 (düğmelerin 8 px altı), 376×560 (en çok 640).
- GÜNLÜK ya da L ile açılır. Açılınca kırmızı nokta söner.

**Kurallar kartı** (`RulesCardView`; S-Baskin6, §B.9.4)

- B kurallar kartının 6 satırı + B lejantının 10 maddesi; genişlik 420.
- Aynı yerde açılır; günlükle aynı anda açık olmaz.
- Metinler `spec/raid/strings_tr_en.csv`'de.

## B.6 Girdi ve etkileşim

| Girdi | İş |
|---|---|
| 1 / 2 / 3 ya da ekip kartı / figüre tıklama | birimi seç (kart −10 px kalkar, birim kartı açılır, mavi kareler) |
| Mavi kareye tıklama | taşı (hareket hakkı söner) |
| Q | yeteneği hazırla (yeşil hedefler) |
| Hedefe gelme | önizleme |
| Hedefe tıklama | kullan |
| Z | turdaki son hamleyi geri al |
| Enter | turu bitir. Kullanılmamış hak varsa ilk basış onay ister (S-Baskin4, §B.9.2); ikinci Enter bitirir |
| Esc / sağ tık | sırayla: hazır yetenek → seçim → açık kart / çekmece → duraklatma menüsü |
| L | günlük |
| ? (F1) | kurallar |
| Boşluk | makineler evresinde hızlandır (aç-kapa; S-Baskin3) |

**Ekip kartı üstüne gelme:** kart 2 px kalkar; tahtada o birimin karesi üstüne gelme işareti alır.

## B.7 Durumlar: B2–B8'in sade karşılıkları

Tahta görünüşü için `reference/raid_B/tahta/B*_tahta@2x.jpg` ve `board_states.json`; HUD için aşağıdaki kurallar.

| Durum | Tahta | HUD |
|---|---|---|
| **B2** birim seçili | ulaşılabilir kareler | ekip kartı seçili, birim kartı (hareket + yetenek açık) |
| **B3** EMP önizlemesi | hedef kareler, hayaletler, itme okları, engellenen itme yıldızı, ZIRH damgası, −1'ler; hedef dolapta köşeli ayraçlar | birim kartı E, yetenek hazır; üstüne gelme kartı hedef dolap: “zırh: hasar işlemez”, itme ve çarpışma satırları |
| **B4** aldatma | tarama alanı, “?” ve ✓ bayraklar | üstüne gelme kartında kesinlik satırı (turuncu halkalı ? / mavi mühür); HEDEF kartında iki YZ yeteneği çipi: ALDATMA (`ic_eye`) + KOPYALAMA (`mk_copy_13`) (§B.4) |
| **B5** makineler → **S-Baskin3** | sıradaki niyet vurgulu (fosfor), bitenler soluk, iptal çizili; parçalar hareket eder | §B.9.1: evre “MAKİNELER”; ekip kartları α .6, kilitli; Geri al ve Turu bitir kilitli; GÜNLÜK noktası yanar; son olay fişi |
| **B6** son tur, düşen birim, yan hasar → **S-Baskin7** | düşen figür + DÜŞTÜ | §B.9.5: son kutu kırmızı + “SON TUR” damgası; kırılan yedek; düşen birim kartı; SİVİL'de 1 çarpılı simge; kazandıran hamlede HEDEF KIRILIR üstüne gelme kartında |
| **B7a/b** sonuç → **S-Baskin9** | B7 kararma + spot + büyük damga | §B.9.7: köşe HUD'u 200 ms'de α 0; B sonuç fişi sağda (1240, 560); Enter → `ContinueToReport` |
| **B8** geri çekilme → **S-Baskin8** | ekran %32 kararır | §B.9.6: GERİ ÇEKİL ilk basışta kırmızı “EMİN MİSİN? TEKRAR BAS” (238 px, sağa yaslı); altında onay fişi (sağ 16, üst 66, 376); ikinci basış onaylar, Esc vazgeçer |
| turu bitir onayı → **S-Baskin4** | hakkı duran birimin karesi | §B.9.2 |

## B.8 Hareket

- **Tahta:** süreler `raid_tokens.motion`. Örnekler: kare başına kayma 0,16 sn, itme 0,14 sn, dolap çöküşü 0,35 sn, niyetler arası 0,25 sn.
- **HUD:** `tokens_sade.motion`.
  - Ekip kartı kalkışı 100 ms.
  - Birim kartı 120 ms (8 px yukarıdan).
  - Üstüne gelme kartı 120 ms opaklık.
  - Çekmece 140 ms.
- **Önizleme işaretleri:** 80 ms'de belirir. Önizleme yalnız VM değişince güncellenir.

## B.9 Açılır paneller ve durumlar (S-Baskin3–9)

**Referanslar:** `S-Baskin3.jpg` … `S-Baskin9.jpg`, `reference/zoom/S-Baskin3…9_*@2x`, `reference/measure/S-Baskin3…9.tsv`, `spec/mock/baskin_S3…S9.json`, `tokens_sade.json › raid.panels`.

| Kare | B karşılığı | Ne gösteriyor |
|---|---|---|
| S-Baskin3 | B5 | Makineler evresi: son olay fişi, kilitli ekip ve düğmeler |
| S-Baskin4 | — (yeni) | Turu bitir: hakkı duran birim için onay |
| S-Baskin5 | B günlüğü | Günlük çekmecesi |
| S-Baskin6 | B kurallar kartı | Kurallar kartı + lejant |
| S-Baskin7 | B6 | Son tur: kırılan yedek, düşen birim, kazandıran hamle |
| S-Baskin8 | B8 | Geri çekilme onayı |
| S-Baskin9 | B7a | Sonuç: hedef kırıldı |

B3 (EMP önizlemesi), B4 (aldatma) ve B7b (süre doldu) için ayrı sade kare yok: tahta `reference/raid_B/tahta/`, HUD bu bölümün ve §B.7'nin kurallarıyla kurulur.

### B.9.1 Makineler evresi ve son olay fişi (S-Baskin3)

- **Tur plakası:** evre “MAKİNELER” (`#F3E3DC`, kırmızı lamba ve yazı). Plaka 388 px'e daralır.
- **Ekip kartları:** α .6, tıklanmaz (`locked`). **Geri al** ve **TURU BİTİR** devre dışı: C pasif düğme + `ic_lock` (TURU BİTİR 245 px'e genişler, sağa yaslı). GÜNLÜK noktası yanar.
- **Son olay fişi** (`EventSlipView`; 16, 196, 394×90): HEDEF kartının 12 px altı.
  - Kâğıt `#EFE4BC`, 1 px `#C9B98A`, solda 4 px `#9A1E14`, iç boşluk 8/12/10/12, gölge (3, 4) α .42.
  - **Sıra satırı** (24 px, aralık 5): “SIRA” (CP 700 11) + niyet kutuları 22 px (1,5 px `#9A1E14`, CP 700 12):

    | Kutu | Görünüm |
    |---|---|
    | bekliyor | `#FBF7EC` zemin, kırmızı yazı |
    | bitti | kenar `#C79B92`, yazı `#B9806F`, ortadan 2 px çizgi |
    | şimdiki | 26 px, dolu `#9A1E14`, `#FBF0E8` yazı 14, gölge (2, 2) α .35 |
    | iptal | zemin `#E3DACA`, kenar ve yazı `#8E8573`, −30° 2,5 px `#B8241A` çizgi |

    Sağda `Boşluk` tuş fişi + “hızlandır” (LF 11 ink3).
  - **Olay satırı:** `robot_mini_22` (20) + “#3” CP 700 14 kırmızı + LF 14 cümle; hedef kalın, hasar CP 700 kırmızı (“Güvenlik dronu kuzeye ateş ediyor → **EMP uzmanı** −1”). En çok 2 satır.
  - **Ömrü:** evre boyunca durur; her niyette satır 120 ms'de değişir, kutular ilerler. Evre bitince 300 ms'de söner.
- **Boşluk:** niyetler arası bekleme 0,25 → 0,08 sn (aç-kapa). Girdi kilidi sürer.

### B.9.2 Turu bitir onayı (S-Baskin4)

- **Ne zaman:** hareket ya da yetenek hakkı duran birim varken ilk Enter / tıklama.
- **Düğme:** TURU BİTİR → “EMİN MİSİN? [Enter]”: zemin `#F5D646`, 2 px ink, ink yazı, gölge (0, 3) siyah. Genişlik 231,7, sağa yaslı.
- **Ekip kartı:** hakkı duran birimin kartı `warn`: 2 px ink, `#FFFDF5`, dışta 4 px `#F5D646` halka, gölge (4, 6) α .4; açık hak simgelerinde 3 px sarı halka. Tahtada birimin karesine üstüne gelme köşeleri.
- **Onay notu** (`EndTurnConfirmView`; 1574, 843, 330×143): C yapışkan notu `#F5DC63` (sağ alt köşe kıvrık), gölge (3, 4) α .4, iç boşluk 12/14.
  1. `ic_hand` 15 + “HAKKI DURAN BİRİM” (CP 700 13 .08em).
  2. Jeton 26 + “**Analist**” + açık hak simgeleri (22 px) + “kullanılmadı”.
  3. **Tehdit kutusu** (VM `threat` varsa): `#FBF7EC` α .7, 1 px kesik `#9A1E14`: `robot_mini_22` + “**#2** dron onu vuracak” + sağda “−1”.
  4. “Enter yine de bitir · Esc vazgeç” (tuş fişleri + LF 12 ink2).
- İkinci Enter bitirir; Esc ya da birime tıklama vazgeçer. Birden çok birim varsa satır 2 her biri için tekrarlanır (en çok 3).

### B.9.3 Günlük çekmecesi (S-Baskin5)

- **Yer:** sağ 16, üst 66 → (1528, 66, 376×560). GÜNLÜK düğmesi basılı: 2 px ink, `#FFFDF5`, içe gölge, +3 px aşağı; kırmızı nokta söner.
- **İçerik:** B operasyon günlüğü aynen (`ol-hd` başlık + `ol-pp` teleks kâğıdı, satır türleri, ELE GEÇİRİLEN SATIR kutusu). Satırlar alta yaslıdır; üstte 16 px kâğıt geçişi.
- **Başlık** sağında 20 px kapat: zemin `#2A221A`, kenar `#6E5A33`, simge `#C9A86A`.
- Kurallar kartıyla aynı anda açık olmaz.

### B.9.4 Kurallar kartı (S-Baskin6)

- **Yer:** sağ 16, üst 66, genişlik **420** → (1484, 66, 420×455,5). KURALLAR düğmesi basılı.
- **İçerik:** B kurallar kartı başlığı (3 px çift çizgi) + kapat; 6 kural satırı (simge 14 + LF 12,5, satır 1,32); altında 14 px içeride B lejantı (ozalit fişi `#D3DBD6`, 1 px `#2C4C8E`, 2 sütun, 10 madde, satır 22).

### B.9.5 Son tur (S-Baskin7)

- **Tur plakası:** son kutu hem şimdiki hem son: zemin `#F7E1DB`, kenar `#B8241A`, yazı `#9A1E14`. Evre etiketinin solunda küçük kırmızı damga “SON TUR” (zemin `#F7E1DB`, −3°). Plaka 476,8 px'e genişler.
- **HEDEF kartı:**
  - Kırılan hedef (yedek): küçük resim `thumb_debris_backup`, ad ink3 + 2 px kırmızı üstü çizili, can noktaları yerine küçük yeşil damga “KIRILDI” (−4°).
  - Düğüm 2/4; SİVİL: 2 sağlam + 1 `civ_broken_20` (kırılan sağdan).
- **Düşen birim kartı** (E): zemin `#E6DED0`; `thumb_standee_E_done` α .7 + üstünde `pencil_x_40x56`; can noktaları boş ve kırmızı kenarlı; hak simgeleri yok; küçük kırmızı damga “DÜŞTÜ” (sağ 8, üst 43, −7°). Tıklanmaz.
- **Seçili birim:** T, can 2/3, yetenek hazır (birim kartı §B.5).
- **Üstüne gelme kartı — kazandıran hamle** (1646, 795, 258×173; **alt kenar 968'de sabit**, kart yukarı büyür):
  - Başlık: `thumb_cabinet_node_s` (40×50) + “YZ DÜĞÜM DOLABI” + kırmızı can noktaları (2/4).
  - `ic_shield` + “Zırh: sabotaj işler” + yeşil onay · `ic_bolt` + “Sabotaj” + CP 700 “2/4 → 0”.
  - **Kazanç kutusu:** kenar boşluğu 6/12/2/12, iç 7/8, zemin `#F7E1DB`, 1 px kesik `#9A1E14`: küçük kırmızı damga “HEDEF KIRILIR” (−3°) + LF 600 12 yeşil “operasyon kazanılır”.
- **Tahtada:** seçili kare, hedef dolapta yetenek hedefi + üstüne gelme, “−2” açılır yazısı. HEDEF KIRILIR damgası tahtada değil, kartta.

### B.9.6 Geri çekilme (S-Baskin8)

- İlk basış: GERİ ÇEKİL → “EMİN MİSİN? TEKRAR BAS” (zemin `#9A1E14`, yazı `#FBF0E8`, kenar `#5E0F0A`, gölge (0, 3) `#3A0805` + (2, 4) α .35). Düğme 238,3 px'e genişler, sağa yaslı; GÜNLÜK ve KURALLAR sola kayar.
- **Kararma:** tüm ekran `#0E0905` α .32 (150 ms). Üstte yalnız kırmızı düğme ve onay fişi kalır; diğer düğmeler kararmanın altındadır.
- **Onay fişi** (1528, 66, 376×118,4): B `rconf` (kâğıt, 1,5 px kesik `#9A1E14`): `ic_door` + “GERİ ÇEKİLME · 2. ONAY” · gövde metni · “İkinci basış geri çeker.” + “Vazgeç [Esc]”.

### B.9.7 Sonuç (S-Baskin9)

- Köşe HUD'u 200 ms'de α 0.
- **Spot:** ızgara (6,5; 3,5; z 0,15) → ekran (1147,5; 350,4), r 150; dışı `#0E0905` α .74, 14 px halka α .36 (B `spotR`).
- **Büyük damga:** “HEDEF KIRILDI” yeşil (B `bigst`), (976, 446, 329×86), −5°; spotun alt kenarına biner.
- **Sonuç fişi:** B `resm`, (1240, 560, 440×374,6). Enter → `ContinueToReport`.
- B7b (süre doldu) aynı yerleşim: kırmızı damga “SÜRE DOLDU”, spot hedef dolapta (z 0,45).

---

## 5. Doğrulama

C §9 geçerli; sade ekleri:

1. **`ReferenceOverlay`**
   - 18 referans görüntüsünü (`reference/screens/*.jpg`) diskten yükler.
   - F12 aç-kapa; F11 opaklık; Shift+F11 yanıp sönme.
2. **Ön ayarlar**
   - `SadeDebugMenu`: F1–F9 S-Masa1–9; Shift+F1–F9 S-Baskin1–9; Ctrl+F1–F3 sade karesi olmayan B3, B4, B7b.
3. **Yakalama:** `Tools/Sade UI/Capture Game View` → `Docs/UI_Sade/captures/<kare>_<tarih>.png`.
4. **Karşılaştırma**

   ```
   python3 Docs/UI_Sade/tools/compare.py Docs/UI_Sade/captures/S-Masa1_….png Docs/UI_Sade/reference/screens/S-Masa1.jpg --tsv Docs/UI_Sade/reference/measure/S-Masa1.tsv
   ```

   - Çıktılar: `_edges.png` (yeşil referans, mor Unity, beyaz çakışma), `_diff.png`, `_blink.gif`, öğe başına fark tablosu.
   - **UI:** panel kenarlarında ve büyük metinlerde çift çizgi kalmasın (≤ 2 px).
   - **Harita:** kıyı ve sınır çizgileri, dokular ve meshler aynı izdüşümden geldiği için ≤ 1 px oturmalı.
   - **3D tahta:**
     - taban çerçevesinin dört köşesi ±4 px,
     - parça ve işaret merkezleri ±4 px,
     - renk ve ışık aynı aile; piksel eşitliği beklenmez.
5. **Taşma denetleyicisi** (`Tools/Sade UI/Check Text`): TR ve EN; 11 px, Kalam 15 px, Auto Size, UpperCase. Dünya etiketleri dahil.
6. **EditMode testleri:**
   - `MapProjection` (`lonLatSamples`),
   - rampa ve yazı rengi,
   - `MapCoverFitter` (16:9, 16:10, 21:9),
   - kart yerleşim kuralı (sağ, sol, kaydırma),
   - nokta-çokgen testi (delikli bölge),
   - ızgara → ekran formülü,
   - kamera boyu formülü,
   - eşik çubuğu yüzde → piksel,
   - TR/EN biçimlendirme.
7. **En-boy oranları:** 1920×1080, 1920×1200, 2560×1440 ve 3440×1440 ekran görüntüleri.
   - Harita kaplar, HUD köşede kalır, yazı ölçeklenmez.
   - Baskında tahta kesilmez.

---

## 6. Fazlar ve kabul ölçütleri

Her fazın sonunda dur, raporla, onay bekle.

| Faz | İçerik | Kabul ölçütü |
|---|---|---|
| **F0 · Keşif ve plan** (kod yok) | C F1 durumu; C1 kurulu mu; Unity sürümü; URP ayarları (gölge, ışık sayısı, SRP Batcher); Input System; lokalizasyon; DI/async/tween; Newtonsoft; Unity MCP; oyunda bölge, gösterge ve baskın veri modelleri. `PLAN.md`: bulgular, klasör/asmdef planı, VM ↔ oyun eşlemesi, paketler, riskler, ≤ 5 soru | kullanıcı onayı |
| **F1 · Ortak temel** | sprite kiti + atlas + postprocessor, dokular, `SadeTheme` + `RaidTheme` (JSON'dan), ortak bileşenler (§4.5), `SadeUI_Gallery` sahnesi | galeri `_contact_sheet` ve yakın planlarla eşleşir; 9-dilimler 3 boyda bozulmaz; konsol temiz |
| **F2 · S-Masa durağan** | `MapCoverFitter`, harita katmanları, `RegionMeshGraphic` ×12, karantina, düğüm, etiket, iplik, dalga, ekipler, uyarı; köşe etiketleri, zaman, sekmeler, LEJANT, zarf; referans katmanı + karşılaştırma | S-Masa1 kenar bindirmesinde çift çizgi yok; 16:10 ve 21:9 görüntüleri doğru |
| **F3 · S-Masa etkileşim ve paneller** | seçim (düğüm + alan), bölge kartı + yerleşim + ip, eylem üstüne gelme → önizleme; §A.14: beş çekmece, döküm fişi, düğüm ipucu, lejant kartı, tam dosya, harita vurguları; klavye, Son Şalter, İllüzyon anahtarları | S-Masa2–9 karşılaştırmaları; etkileşim listesi (§A.13, §A.14.1) eksiksiz; boşta 0 GC |
| **F4 · Baskın tahta** | `BoardBuilder` + parça prefab'ları, kamera k = 1,5, ışık, arka plan + tahta gölgesi, ozalit, işaretler, niyetler, bayraklar, sade dünya etiketleri; B1–B8 tahta ön ayarları | S-Baskin1 tahtası ±4 px; `raid_B/tahta` yakın planlarıyla parça ve işaret dili eşleşir |
| **F5 · Baskın HUD + kartlar** | tur plakası, HEDEF (+ YZ yeteneği çipi), üst düğmeler, ekip kartları, sağ alt; birim kartı, üstüne gelme kartı; mock komutlarla seçim, Q, önizleme, Z, Enter | S-Baskin1 ve S-Baskin2 karşılaştırmaları; girdi tablosu (§B.6) eksiksiz |
| **F6 · Baskın durumlar ve paneller** | §B.9: makineler + son olay fişi, turu bitir onayı, günlük, kurallar, son tur (kırılan hedef, düşen birim, kazandıran hamle), geri çekilme, sonuç; B3, B4, B7b (§B.7); hareketler | S-Baskin3–9 karşılaştırmaları; makineler evresinde girdi kilitli |
| **F7 · Cila** | EN geçişi + taşma denetimi, en-boy görüntüleri, performans, `UYGULAMA_NOTLARI.md` (bileşenler, oyuna bağlama, bilinen sapmalar), son yakalamalar | tüm kontrol listeleri yeşil |

---

## 7. Açık kararlar

Varsayılanı uygula, ayarlanabilir bırak ve `KARARLAR.md`'ye yaz.

> **1.1'de kapananlar:** önceki sürümün 1 (çekmece), 2 (lejant kartı), 4 (düğüm üstüne gelme), 6 (YZ yetenekleri), 7 (günlük ve son olay fişi), 8 (turu bitir onayı), 9 (düşen birim) ve 13 (tüm dosya) kararları artık çizimdir: §A.14 ve §B.9. Bunlar varsayılan değil, referanstır.

1. **Ağ yolları.** **Varsayılan:** gösterilmez. Seçenek: seçili bölgenin yolları, C basılı yol stiliyle (1,3 px `#6F6553`, kesik 6/4). Bayrakla açılır.
2. **Bölge tıklama alanı.** **Varsayılan:** düğüm + bölge alanı (nokta-çokgen). Denize tıklamak seçimi kaldırır.
3. **Kart sağa sığmazsa** (bölge kartı). §A.11 kuralı. İki yana da sığmazsa kart sağ sütuna (x 1540) sabitlenir ve ip uzar.
4. **21:9'da etiket boyu.** **Varsayılan:** ölçeklenmez (§2.3).
5. **Bölge kartında başka bölgeye tıklama.** **Varsayılan:** kart yeni bölgeye kayar. Kartı kapatmaz.
6. **Diğer binaların ozalit dokusu.** **Varsayılan:** genel doku + çalışma anında kapı ve kanal çıkartmaları. Seçenek: bina başına basılı doku (Editor'da `tx_ozalit_*` üretici).
7. **Küçük maket resimleri.** **Varsayılan:** paketteki PNG'ler. Seçenek: 3D prefab'tan RenderTexture.
8. **Uzun içerik.** **Varsayılan:** araştırma dalında 3'ten çok düğüm varsa satırın sağında “+n” fişi; tıklayınca dal yatay kayar. 4'ten çok dal ve uzun haber / günlük listeleri dikey kayar (C kaydırma kuralı; sekme ve süzgeç sabit).
9. **Haber ve günlük satırına tıklama.** **Varsayılan:** bölgeyi seçer (kart açılır, çekmece açık kalır); üstüne gelme yalnız yakar. Seçenek: tıklama da yalnız yaksın.
10. **Makineler evresinde hız.** **Varsayılan:** Boşluk aç-kapa (0,25 → 0,08 sn). Seçenek: basılı tutarken hızlı.
11. **Turu bitir onayı.** **Varsayılan:** açık; ayarlardan kapatılabilir. Tehdit satırı yalnız VM `threat` verirse görünür.
12. **Tam dosyanın yanı.** **Varsayılan:** bölgenin karşı yanı (§A.14.11). Seçenek: her zaman sol.
13. **Diğer döküm fişleri.** **Varsayılan:** FON çizildiği gibi; diğer etiketler C `GaugeBreakdownTooltip` satırlarıyla aynı kapta. Çizgi grafiği yalnız VM geçmiş verirse.

---

## 8. Yapma listesi

C §12'nin hepsi geçerli (görseli yeniden yorumlama, bulanık gölge, Auto Size, UpperCase, YAML yazma…). Ek olarak:

- **Kaldırılan öğeleri varsayılan ekrana geri koyma:** teleks şeridi, sürekli açık klasör ve dosya, danışman bandı, basılı lejant, pusula, nesne adları, robot harfi, “3/4” can sayıları.
- **Ekranın ortasına kalıcı panel koyma.** Açılır kart seçili bölgeyi ya da birimi örtmesin.
- **İki ipucunu, iki çekmeceyi ya da çekmece ile tam dosyayı aynı anda açma** (§A.14.1).
- **Harita üstündeki yazıyı ve düğümü harita ölçeğiyle büyütme.**
- **Bölge dolgusunu maske ya da sprite ile yapma.** `RegionMeshGraphic` (üçgenler hazır) kullan.
- **Önizleme, ulaşılabilirlik ve sıra hesaplarını görünüm katmanında yapma.**
- **Haritanın sınır verisini değiştirme** (Kırım Doğu Avrupa'da kalır). Bölgeleri yeniden çizme.
- **3D'de** emissive, bloom ya da glow ekleme; işaret okları için post-processing kullanma.

---

## 9. İlk yanıtın (F0)

1. **Paket özeti:** 8–10 madde. İki ekran; neyin nerede olduğu; C ve B bağımlılıkları.
2. **Proje incelemesi bulguları:**
   - C F1 ve C1 durumu,
   - Unity ve URP sürümü, URP Asset ayarları,
   - uGUI/TMP, Input System, lokalizasyon,
   - DI/async/tween, Newtonsoft,
   - Unity MCP bağlantısı,
   - oyunun bölge, gösterge ve baskın veri modelleri.
3. **`Docs/UI_Sade/PLAN.md` taslağı.**
4. **En fazla 5 soru.**

**Kod yazma; onay bekle.**
