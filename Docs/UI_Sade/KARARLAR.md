# Sade arayüz · kararlar

Paket: `Docs/UI_Sade/SonSalter_Sade_Paketi` (PROMPT 1.1). Her karar tek satır; gerekçe gerektiğinde altında.

## F0 · kullanıcı kararları (2026-10-16)

- **Harita bölgeleri: A, çokgen eşlemesi.** Tasarım çokgenleri değişmez; oyunun 12 bölgesine eşlenir.
  - Kuzey Amerika: `na` = KA + OA.
  - Avrupa ve Avrasya: `weu` = BA, `eeu` = DA, `ru` = AV.
  - Orta Doğu ve Afrika: `me` = ODKA, `af` = SAA.
  - Asya: `in` = GuA; `cn` = DoA ana kara + Tayvan + Hainan; `jk` = DoA'daki Japonya adaları + Jeju; `sea` = GDA.
  - Diğer: `sa` = GA, `oc` = OK.
  - Kore Çin'in, Kuzey Afrika Orta Doğu'nun renginde görünür.
- **Çizimde olmayan oyun özellikleri:** PLAN §4'teki yerleşim onaylandı.
  - Araştırma çekmecesi: ağacın altında ekonomi kartları; hız seçicinin yerinde acil durum fonu.
  - Bölge kartı ve tam dosya: kartta ekip fişleri; tam dosyada bölge fonu, toparlanma ve diplomasi.
  - Haritada saha raporu fişleri.
  - Haber “Denetle” = bölge denetimi.
- **Eski düzen kalkar:** S-Masa ve S-Baskın oyun sahnelerine geçince C1 düzeni ve B HUD'u silinir. C1'in bileşenleri ortak parça olarak kalır.
- **Sıra:** paketteki gibi. F1 ortak temel → F2–F3 masa → F4–F6 baskın → F7.

## Projeye uyarlama

- **Yol ve isim:** paketin `Assets/_Project`, `SonSalter.*` önerileri yerine projenin düzeni: `Assets/Corrigible/Desk`, `Corrigible.Desk*` (PROMPT §0.5).
- **Menüler:** `Tools/Sade UI/…` yerine projenin menü kökü `Corrigible/Sade UI/…`.

## F3 · masa etkileşimleri (2026-10-17)

- **Önce sandbox doğrulaması:** F3 kart/çekmece ve girdileri `SadeDesk_Sandbox` içinde örnek görünüm verisiyle kurulur. `DeskGame` aktarımı ve gerçek komut köprüsü ayrıca ele alınır (PLAN §7).
- **Komut sınırı:** bu sahnede eylemler `MockSadeDeskCommands` niyet günlüğüne yazılır; araştırma, görevlendirme ve tehlikeli eylemler simülasyon başlatmaz.
- **Acil durum fonu:** araştırma hız seçicisi yerine 0 / 15 / 30 fon, varsayılan 15. Bu, F0'da onaylanan uyarlamadır.
- **Eksik bölge verisi üretilmez:** paket yalnız Doğu Asya için ayrıntılı kart/dosya sağlar. Diğer bölgelerde haritanın gösterdiği değer ve güven kullanılır; örnekte olmayan yerel istatistik, kaynak ve geçmiş boş bırakılır.
- **İllüzyon ipucu:** gösterilen değerler onaylı ve yuvarlak olurken “Kapsam %0 · onaylandı · 0 ms” çelişkisi kart ve dökümde görünür. Gerçek sızma değeri okunmaz.
- **Dil ve en-boy:** panel bağlamaları TR/EN'de hazır metin yuvalarını yeniden yerleştirir; ekran oranı değiştiğinde kart/dosya ve ipuçları güvenli alana yeniden konumlanır.

## F1 · ortak temel (2026-10-16)

**Varlıklar**
- **Ayrı atlas:** Sade sprite'ları `Art/SadeUI.spriteatlasv2`'de. Kayıt (`DeskSprites`) tek: C ve Sade adları çakışmıyor (135 + 174).
- **Harita ve ozalit dokuları:** BC7, mipmap yok, Clamp, en çok 4096.
  - `map_base@2x` ve `map_lines@2x` (3840×2160) her biri 7,9 MB.
  - İki ozalit (2048²) her biri 4 MB.
  - Editör profili CPU kopyasıyla iki katını gösterir.
- **Bozuk paket dokuları yeniden üretildi:** `tx_carbon_hatch.png` ve `tx_quar_hatch.png` tamamen saydamdı (alfa 0).
  - Paketin CSS ve SVG tanımlarından üretildi; araç `Docs/UI_Sade/tools/hatch_textures.py`.
  - Üreteç, sağlam `tx_hazard` ile doğrulandı (ortalama fark 0,14 / 255).
  - Test: `SadeTextures_AreNotBlank`.

**Tema**
- **Anahtarlar:** `sade.` ön ekli; `DeskTheme.TryGet` onları `SadeTheme`'e yönlendirir. Koddaki her `sade.` anahtarı testle denetlenir.
- **Tokenlarda eksik olup çizimde kullanılanlar** (`SadeThemeSetup` ekleri):
  - yazı rolleri `chipSlip` (CP 700 11, .05em) ve `chipQuestion` (Kalam 700 18);
  - renkler `desk.time.calHeadText` #FFF3EE ve `desk.dock.tabBorder` #000;
  - gölge `dockTab` (2, 0, #000 α .28).

**Bileşenler**
- **Önizleme:** her bileşenin `ShowSample()`'ı var. Edit modda `OnEnable` çalışmadığından kurucu kaydetmeden önce çağırır; `OnValidate` sahne yüklenince yeniden çizer.
- **Düz dolgular `DeskSolidGraphic`:** çubuk zemini ve dolgusu, takvim başlığı, plaka ayracı, kart başlık çizgisi.
  - Gerilmiş `sh_fill_r0` Image, kenar pikselini atlas boşluğuyla karıştırıp dolgunun başında ~2 px soluk şerit bırakıyordu.
- **Değişim okları:** C'nin kalın okları (`ic_arrow_up_12b`, `ic_arrow_down_12b`, `ic_flat_12b`). Paketteki `ic_up_s` çizimdekinden ince.

**Çizimden sapmalar**
- **Sağ üst etiketler arası 8 px:** çizimde KÜRESEL GÜVEN ile BAĞIMLILIK üst üste biniyor (TSV ve ekran görüntüsü aynı). Sağdan sola aralık 8: TEKİLLİK sağı 1904, BAĞIMLILIK sağı 1674, KÜRESEL GÜVEN sağı 1462.
- **Alt sekmeler:** her sekme çizimden ≈1 px dar (font ölçümü); ±2 px içinde.

**Kurulum**
- Prefab'lar `Prefabs/Sade/Common` (7) ve `Prefabs/Sade/Hud` (5). Galeri `Desk/Scenes/SadeUI_Gallery.unity`.
- Geçici kurucu kullanıldıktan sonra silindi.

## F2 · durağan masa (2026-10-16)

- **Sahne:** `SadeDesk_Sandbox`; S-Masa1 sahte verisiyle, `GameController` olmadan çalışır. Gerçek oyun köprüsü ve panel etkileşimleri sonraki aşamadadır.
- **Harita:** hazır çokgenler, delikler ve üçgenler korunur. Harita katmanları cover ölçeğiyle, düğüm/etiket/ekipler sabit boyla çizilir.
- **Ekip A:** JSON'un coğrafi örneği ile çizimdeki konum uyuşmadığından F2'de `overlay.teams` konumu kullanılır; izdüşüm formülü ayrıca test edilir.
- **Etiket halesi:** ortak TMP Underlay materyali; her etiket için materyal kopyalanmaz.
- **Linear lamba:** kaynak `tx_lamp_desk.png` korunur, proje renk uzayı için `tx_lamp_desk_linear.png` türetilir. Üreteç `tools/lamp_linear.cs`.
- **Yakalama:** yeni ekran oranında ilk kamera çizimi canvas boyunu kesinleştirdikten sonra fitter'lar yenilenir. Böylece tek yakalamada harita, iplik ve işaretler hizalanır.
- **Çizim sırası:** bölge dolguları, ardından karantina örtüleri; düğüm metin kutuları görünümü koruyarak içerik boyuna daraltılır. Gereksiz batch bölünmeleri azaltılır.
- **Sahte veri önbelleği:** serileştirilmez; sıcak yeniden yüklemede nullable sızma değerleri JSON'dan yeniden okunur.
- **İplik çizim grupları:** çizgiler/gölgeler ve ok uçları iki katmanda gruplanır. Görünürlük sahibi `SadeSpreadThreadView` kalır; ayrı katmanlardaki grafikler editör ve Play modda birlikte açılıp kapanır.

## F4 · Baskın tahtası (17 Ekim 2026)

- Sandbox, mevcut B maketinden yalnız kamera, tahta tabanı, tahta ve ışıkları alır; canlı sahne F6’ya kadar korunur.
- Sade ekran etiketleri serileştirilmiş prefab/Canvas seçeneğiyle açılır; eski `RaidWorldLabel` yolu varsayılan olarak kalır.
- Turu biten figürün can fişi için `raid.worldLabels.spentPaper = #E9DDC6` tema eki eklendi; kaynak metindeki renk sayısal token değildi.
- `tx_felt` zaten renkli RGB dokudur: ikinci yeşil çarpan uygulanmaz, döşeme özgün 40 px boyundadır.
- Kamera ölçeği gerçek kamera piksel boyutundan ve Expand hesabından gelir; editörün farklı GUI penceresine ait `Screen` boyutu kullanılmaz.
- Ön ayarlar paket JSON’una sadıktır. B5’te bağımsız iptal fişi verisi yoktur; canlı iptal/sonuç sunumu F6’da ele alınır. Tam ekran raster farkı F4 kabul ölçütü değildir; kamera izdüşümü ayrıca ±1 px doğrulanır.


## F5 · Baskın HUD ve kartlar (17 Ekim 2026)

- F5, `RaidBoard_Sandbox` üzerinde kalır; canlı `Baskin.unity` aktarımı F6'dır.
- Mock komutlarda ikinci bir kural kopyası yazılmaz: paket S1/S2 örnekleri ayrı bir BState'e çevrilir, hareket/hedef/önizleme/geri alma Core API'leriyle yürür. Ana oyun kaydı ve profil bağlanmaz.
- Analist Tarama satırı mevcut pasif kuralı gösterir; yeni etkin yetenek ya da harcanan hak eklenmez.
- Makine evresi, onay ve günlük/kurallar istekleri sınırdan geçer; tam panel tasarımı ve niyet yürütmesi F6'ya bırakılır. Mock makine evresi otomatik devam etmez.
- Paket kısa ikon adları, mevcut sprite kaydındaki `_s` çeşitlerine eşlenir. Kare görünen null sprite'lar kabul edilmez.
- Baskın düğmelerinin Navigation modu None'dır; Enter'ın hem Submit hem tur bitirme üretmesi engellenir.
- Dünya etiketinin piksel ötelenmesi ekran proxy'sine aynı çağrıda uygulanır; yakalamalarda eski konumdan çizilmez. Engellenen itme hayalet konum üretmez, çarpışma yıldızı gösterir.


## F6 · Durum panelleri ve canlı Baskın (17 Ekim 2026)

- Kullanıcının “F6 ya geç” isteği, planın S3–S9 ve canlı Baskın aktarımını kapsar. Sahne aynı GUID ile güncellenir; ana masanın baskın başlatma ve rapor yolu korunur.
- Core/Prototype kuralı, ekonomi ve kayıt biçimi değiştirilmez. Taktik sonuç mevcut `GameController.ResolveRaid` → `raidres` → `Replay.Restore` yoluna gider.
- Makine canlandırmasında sunum kopyası hasar/ölüm/kırılma/onarım efektleriyle ilerler. Kesin olmayan niyet tur sonu onayında kesin tehdit diye sunulmaz.
- Ana sahne ile Baskın'ın girişleri çakışmasın diye masa `DeskInput` geçici olarak kapatılır; yalnız bir etkin EventSystem kullanılır. Sahne kaldırılınca masa girişi geri gelir.
- İngilizce kurallar ve çok birimli onay notu içerik yüksekliğine göre büyür. Sonuç kartı tek gösterimdir; Enter ve düğme aynı devam komutuna gider.
- F7 performans ve son görsel cila ayrı faz olarak kalır.
