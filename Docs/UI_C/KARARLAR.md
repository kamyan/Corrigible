# UI_C · Kararlar

Her satır bir karar: ne, neden. Faz 0 kararları kullanıcı onayıyla (2026-10-11) verildi.

## Faz 0 (kullanıcı onayı)

- **Kapsam:** Masa yalnızca yeni sahnelerde (`DeskUI_Sandbox`, `DeskUI_Gallery`) ve sahte veriyle kurulur. `Main.unity` ve mevcut UI F7'ye kadar değişmez. Oyuna bağlama ayrı bir F8 fazıdır.
- **İzinler:** Sprite Packer Mode → Sprite Atlas V2, `com.unity.nuget.newtonsoft-json` manifest'e doğrudan, Pillow kurulumu.
- **Prefab'lar elle kurulur:** Prefab asset'i kaynaktır; depoda onları yeniden üreten idempotent bir builder tutulmaz. Prefab'ları Editor'de (canlı Editor API'siyle) bir kez kurar, sonra asset üzerinde düzenlerim. Böylece sonradan elle yapılan değişiklikler bir builder tarafından ezilmez. TSV ölçüleri kurulum sırasında kullanılır, çalışma anında okunmaz.
- **Konum ve adlar:** `Assets/Corrigible/Desk`, asmdef/namespace `Corrigible.Desk*`, menüler `Corrigible/Desk UI/…` (PROMPT: `Assets/_Project/UI/Desk`, `SonSalter.UI.Desk`, `Tools/Desk UI`).
- **Harita bağlama (F8):** Düğümler oyunun enlem/boylamından tasarımın projeksiyonuyla (`x = 960 + (lon − 27) × 3,5`, `y = 622 − lat × 4,45`) yerleşir, yollar oyunun bağlantılarıdır, harita sprite'ı aynen kalır. Bu yüzden `MapView` düğüm, etiket ve yolları VM'den alır; tasarım haritası (`map.json`) yalnızca sahte veridir.

## Projeye uyarlama

- **Lokalizasyon:** Unity Localization yok. `IDeskLocalizer` → projenin `Loc` sistemi. `strings_tr_en.csv`'deki tr/en çiftleri `strings.txt`'ye girer, `key` sütunu yalnızca not.
- **TMP Settings değişmez:** Font asset'leri `Assets/Corrigible/Desk/Resources/Fonts & Materials/` altında durur (`<font>` araması bu yolu bulur). Yedek zinciri asset başına, style sheet metin başına atanır.
- **Tuş fişleri:** Projede InputAction kullanılmıyor (`Keyboard.current`). Fiş metinleri sabit `DeskKeys` tablosundan gelir, `Loc` ile çevrilir.
- **Canvas:** Yeni sahnelerde Overlay + Expand. Main'deki Match 0 düzenine dokunulmaz.
- **Hareket:** Tween kütüphanesi yok; coroutine + `unscaledTime`.

## Faz 1

- **Menüler:** `Corrigible/Desk UI/` altında: Atlası kur, Font asset'lerini kur, Temayı tokens.json'dan doldur, Sprite kaydını güncelle, Capture Game View.
- **Tema kaynağı:** `Docs/UI_C/spec/tokens.json` Assets'e kopyalanmaz; tema `Resources/DeskTheme.asset`'e doldurulur. Bileşenler renkleri `ThemeTint` anahtarıyla alır; eksik anahtar eflatun çizer ve testle yakalanır.
- **İllüzyon paleti:** `paper.page/pageEdge/manila/manilaEdge/index/telex/dialFace` ve `advisor.title` anahtarları İllüzyonda geçersiz kılınır (`DeskTheme.Illusion`).
- **Fontlar:** Her TTF için statik SDF (charset.txt; 90 örnekleme 1024²'ye sığdı) + aynı TTF'den dinamik kardeş (`… Dyn SDF`, 512², derlemede temizlenir). DejaVu yalnızca ana fontlarda olmayan 10 sembolü taşır (₺ № ← ↑ → ↓ ↔ ✓ ✗ ⠿; 512²). Malzeme `TextMeshPro/Distance Field` (Sharpness ayarlanabilsin diye mobil değil).
- **Style sheet:** `Data/Theme/DeskStyles.asset` (num/numr/semi/hand) metin başına atanır; TMP Settings'teki varsayılan stil sayfası değişmez.
- **Sprite kaydı:** `Resources/DeskSprites.asset` (ad → sprite + Image türü). Durum değiştiren bileşenler sprite'ı adla alır (ör. devre dışıda kesik kenar).
- **İsabet alanı:** Düğmelerde raycast'i çizilmeyen `DeskHitArea` alır; görsel çocuk (`Visual`) kalkar/iner, isabet alanı kıpırdamaz.
- **Prefab kurulumu:** F1 prefab'ları ve galeri, geçici bir Editor betiğiyle bir kez kuruldu, betik silindi. `DeskUiKit` (düğüm/görüntü/metin/alan bağlama yardımcıları) sonraki elle kurulumlar için kalır.
- **Düğme varyantları:** `DeskButton.prefab` taban; Small/Ink/Link/Close/Minimize prefab varyantıdır.
- **Galeri yerleşimi:** Bölümler C5 dilimleriyle aynı koordinatlardadır. Konumlar, referans HTML'in fontları yüklenmiş tarayıcı ölçümünden alındı (fontsuz ölçüm metne bağlı konumları kaydırıyordu). Galeri EN'de de TR koordinatlarını kullanır; uzun EN kelimelerin galeride çakışması beklenir (gerçek ekranlarda dinamik yerleşim).
- **Ölçü düzeltmeleri:** Hız tuşu metni Franklin 700 (CSS'teki Courier `.c button` kuralıyla ezilir). Nötr değişim mürekkebi ink2. “onaylandı” kelimesi Franklin 12, mühürle aralık 5. RAPOR fişi genişliği = metin + 2 × (5 iç boşluk + 1 kenar).
- **Lokalizasyon:** Desk kodundaki sabit kelimeler `Loc.N(...)` ile işaretlenir (tablo aracı toplar). `Çeviri tablosunu güncelle`, boş EN çevirilerini `Docs/UI_C/spec/strings_tr_en.csv`'den tohumlar.
- **Ölçüm altyapısı:** Referans HTML'i ölçmek için `Artificial Intelligence/.claude/launch.json`'a yalnızca 127.0.0.1'e bağlı `corrigible-ui-c` statik sunucusu eklendi (kök `Docs/UI_C`). Pillow kullanıcı site-packages'a kuruldu.
- **Renk uzayı (kullanıcı kararı, 2026-10-11): Linear'da kalınır.** Telafi: tüm masa font malzemelerinde `_WeightNormal` 0,4 ve `_Sharpness` 0,8 (`DeskFontSetup.LinearWeight/LinearSharpness`). C5 ölçümünde 11–13 px yazının mürekkep miktarı referansla eşitlendi (oran 0,98–1,04; önce 0,63–0,76). Sprite'taki 1 px çizgiler (damga çerçevesi vb.) telafi edilemez, referanstan biraz açık kalır (≈%82). 26 px değerler referanstan çok az kalın.

## Faz 2

- **1,5 px kenarlar:** CSS'te 1,5 px yazan kenarlar (küçük damga, durum çipi, onay kutusu, açık sekme, eylem kartı üstüne gelme) referans PNG'de 1 px çiziliyor (Chrome tam piksele yuvarlıyor; TSV de `bd=1` diyor). Bileşenler 1 px çerçeve sprite'ı kullanır; küçük damga yüksekliği 19.
- **Stil sayfası rengi:** `num` ve `semi` stilleri mürekkep rengini de taşır (`<color=#1E1B16>`); TSV'de vurgulu sayılar ink, çevre metin ink2. `numr` rengi değiştirmez.
- **Seçim ipliği:** PROMPT §7.1 ipliği Overlay canvas'a koyuyor ("dosyanın da üstünden geçer"), ama referans PNG'de iplik dosyanın altında kalıyor, sekmenin kenarında bitiyor ve uçtaki raptiye görünmüyor. Öncelik PNG'de: iplik harita canvas'ının en üstünde (pencerelerin altında).
- **Metin ölçümü:** Libre Franklin asset'leri kerning çiftleriyle kurulur (`TryAddCharacters(..., includeFontFeatures: true)`); genişlik hesabı CSS gibi son harften sonraki harf aralığını da sayar (`DeskUi.TextWidth`). Ölçüm Chrome ile 0,01 px düzeyinde aynı. Kaplar en yakın piksele yuvarlanır.
- **Sekme genişliği:** iç boşluk 12 + 1 px yan kenar (CSS border-box).
- **İnce çizgiler:** 1–2 px çizgiler mutlak ekran koordinatında tam piksele yaslanır (Chrome kenarları yaslar). `sh_fill_r0` her zaman Simple çizilir: 9-dilimde ya da yarım piksel konumda örnekleme sprite kenarına düşüp çizgiyi yarı saydam yapıyordu.
- **Danışman mesajı:** TMP “…” kesme modunda kutu satır yüksekliğinden (LF 14 → 17 px) kısa olursa metnin tamamı gizlenir; kutu 20 px.
- **Harita kökü:** tam ekran 1920×1080, ekran ortasına çapalı; içerik `map.json`/TSV mutlak koordinatlarında. 21:9'da yatayda, 16:10'da danışman–teleks bandında ortalanır. Düğümler, yollar ve analist katmanı VM'den havuzla kurulur (oyunda projeksiyon, F8).
- **En-boy oranı:** ray, sayaç rafı ve danışman yatayda uzar; sayaç hücreleri orantılı çapalı (hücre i: çapa i/5…(i+1)/5, kaydırma 2,4·i ve 2,4·i − 9,6; 1920'de 368 px); sol/sağ klasörler dikeyde uzar; teleks zarfa kadar; sekmeler sol-alt, zarf sağ-alt.
- **Seçim ipliği uçları:** gerçek düğüm raptiyesi (merkezin 14 px üstü) ve dosya sekmesinin sol-üstünden (16, −14); yalnızca konum değişince yeniden çizilir. Yakalama aracı başka çözünürlükte çizmeden önce yeniler.
- **Sahte veri:** `MockDeskDataSource` ekran JSON'unu (Newtonsoft `JObject`) ve `map.json` geometrisini birleştirir; bölge adları dile göre `map.json`'dan (TR/EN).
- **Yerleşim yardımcıları:** LayoutGroup yerine `DeskRow` (yalnızca veri değişince dizer; `LayoutElement.ignoreLayout` çocukları atlar) ve dinamik listeler için `DeskPool`.
- **Bilinçli küçük sapma:** Araştırma özetindeki öğeler CSS'te `gap: 6` ile ayrılır; VM metni tek satır rich text olduğundan TMP'de normal boşluk kullanılır (2–4 px fark).
- **Güven işareti çeşitleri:** kaynak tablosunda ŞÜPHELİ halka ikonlu Kalam 14; önizleme notu ve teleksde ikonsuz “? şüpheli” Kalam 14.
- **Açık (F7):** C1'de şu an ~105 batch (hedef ≤ 35; font malzemesi sayısı ve canvas bölünmesi); `DeskLocText` kaynakları çeviri aracına henüz toplanmıyor; sahte veri metinlerinin EN çevirileri eksik.

## Faz 3

- **Kullanıcı onayı (2026-10-12):** sekmenin tamamı tutamaç olarak kalır; arka sekme sadeleşmesi, dosya arkadayken ipliğin gizlenmesi ve `modal.*` Linear alfa telafisi uygun. Gölgelere telafi F7'de ölçülerek karar verilecek.

- **Pencere sütunları:** sol 16–392 (üst 328), sağ 1528–1904 (üst 204), alt 88; harita asla örtülmez. Sütun başına en çok 2 pencere; dolu sütuna açılan yenisi arkadakini kapatır. Hedef kartı pencere değildir (sabit).
- **Arka sekme (açık karar 2):** sağa yaslı, arka manila tonu, başlık ink2. Sadeleşir (tutamaç, × ve alt başlık gizli; tıklanınca öne gelir) ve öndeki sekmeden kalan yere sığar: önce tuş fişi düşer, sonra başlık “…” ile kısalır. Böyle yapılmazsa dosya (256) ile ekipler sekmesi çakışıyordu. Arkadaki klasörün süsleri (araştırma ataşı) gizlenir; yoksa sekme şeridinden taşıyordu.
- **Sekme genişliği:** araştırma ve dosya referans genişliğinde (238, 256). Tasarlanmamış pencerelerde içerikten: tutamaç alanı 33 + satır + 12 + × 22 + 5.
- **Sürükleme:** sekmenin tamamı tutamaçtır (⠿ dahil; daha büyük hedef). Bırakınca pencere merkezine en yakın sütuna 120 ms ease-out ile oturur; hedef sütun doluysa oradaki arka pencere kapanır.
- **Hedef kartı küçültme:** kart 33 px'e iner (gövde, çizgiler, kural gizli; düğme ikonu ↓). Sol sütunun tepesi aynı miktar yukarı çıkar (328 → 249).
- **Seçim ipliği:** yalnızca dosya öndeyken görünür; dosya arkadayken sekmesi başka pencerenin altında kaldığı için iplik gizlenir. Dosya kapanınca seçim kalkar (açık karar 9).
- **Tek girdi sahibi:** onay kartının Enter/Esc'si de `DeskInput`'tan geçer. Önceden Esc aynı karede hem kartı hem en üstteki pencereyi kapatıyordu.
- **Son Şalter:** erken bırakılınca halka doluluğundan bağımsız olarak 200 ms'de geri sarılır (önceden tam halka hızıyla sarılıyordu).
- **Onay kartı (açık karar 6):**
  - Bildirim kâğıdı: “KYGA · KARAR” başlığı, çift kural, CPB 26 başlık, LF 14 gövde; kâğıt boyu gövde metnine göre.
  - Karartma 150 ms'de, kâğıt 8 px düşerek 180 ms'de belirir.
  - Tehlikeli bölge eyleminde başlık “Son Şalter: BÖLGE ADI”.
- **Örtü alfası (Linear):** CSS yarı saydam örtüyü sRGB'de karıştırır, Linear proje doğrusal uzayda karıştırır. #0E0905@0,78 karartma bu yüzden kâğıt üstünde 123 çıkıyordu (referans ≈64). `modal.*` tonları tema kurulumunda `DeskMath.LinearOverlayAlpha` ile eşdeğer alfaya çevrilir (0,78 → 0,949; ölçülen 62). Küçük gölgelere dokunulmadı.
- **Hareket:** `DeskTween` bir yapıdır ve bellek ayırmaz.
  - Kalkma 80 ms, basma 60 ms, ease-out: düğme, eylem kartı, dock sekmesi (36 → 40 → 44) ve zarf.
  - Klasör açılışı 120 ms CanvasGroup soluklaşmasıdır; başlangıçta açık olanlar animasyonsuz gelir.
  - Sabit önizleme durumları (galeri, VM'deki `state`) animasyonsuzdur.
- **İpuçları:** 400 ms gecikme, imlecin 8 px altında açılır, sağ/alt kenarda ters açılır, en fazla 420 px; konum tam piksele yuvarlanır.
  - Metin ipucu iki yerde çıkar: düğüm üstünde “Ad · %değer · güven kelimesi” (açık karar 4) ve kesilen danışman mesajında tam metin (açık karar 11).
  - Döküm fişi RAPOR'da karbon kâğıttır. Toplam satırı referanstaki gibi “=” ile başlar.
- **Danışman:** “Göster” VM komutunu iletir (ör. `FocusRegion:DAS` → bölge seçilir); “Anladım” bandı kapatır.
- **Araştırma:** duraklatılmış satırdaki ▶ “başlat” niyeti gönderir (sürdür).
- **Gezinme:** bütün Selectable'larda `Navigation.None` (kısayollar komutlara bağlı).
- **Tasarlanmamış pencereler (açık karar 3):** hepsi aynı `FolderWindow` ile kurulur.
  - Haberler: teleks öğeleri liste hâlinde; teleks kâğıdı, güven işareti ya da damga; sahte haber üstü çizili.
  - Saha Ekipleri: dizin kartı, ekip jetonu, 4 kare moral.
  - YZ Günlüğü: pembe başlık şeridi ve kırmızı daktilo satırları; okunanlar soluk.
  - Rehber: Courier başlık, Franklin gövde.
  - Taşma (açık karar 7): RectMask2D'li ScrollRect, 4 px ink2 çubuk, iz yok, gerekmezse gizli.
- **Sınama yöntemi:** etkileşimler Play modunda Input System olay kuyruğuyla, gerçek UI modülü üzerinden sınandı (geçici sanal fare; sınamadan sonra kaldırıldı).
  - Profiler'da yalnız PlayerLoop okundu. Boşta ve ipucu açıkken masa kaynaklı GC 0'dır.
  - Kalan küçük ayırma Editor'ü süren CLI köprüsünün async dağıtıcısından gelir. Sürüklemedeki `GetComponentNullErrorMessage` yalnızca Editor'de oluşur.

## Faz 4

- **Kullanıcı onayı (2026-10-12):** F4 onaylandı ("F5'e geç"); sönük kart ve danışman düğmesinin veriden gelmesi, İllüzyonda kalem halkasının gizlenmesi itirazsız kabul edildi.

- **Sunum anahtarı:** `IllusionPresenter.Set(bool)` iki şey yapar:
  - `DeskTheme.Illusion`'ı değiştirir. Palet, damga açısı/opaklığı ve `ThemeTint`'ler tema olayıyla güncellenir.
  - Kanvas altındaki `IIllusionAware` görünümlere bildirir: harita, sayaç, danışman, dosya, dock, zarf, bant düzleştirici.
  - Görünümler son VM'leriyle yeniden çizilir. Sunucu her bağlamada `Set(model.Illusion)` çağırır; değer aynıysa bir şey yapmaz.
- **İzinli öğeler (çalışırken aç/kapa, C1 verisiyle sınandı):**
  - Analist katmanı ve iplik gizlenir; yerine raptiye delikleri, silik notlar ve silgi lekesi gelir.
  - Damgalar 0° ve .95 olur; bantlar düzleşir; palet değişir.
  - Sayaçta İllüzyon kadranı kullanılır; kama yok, ibre sabit.
  - Kalem halkası ve kadran “?”ı gizlenir (dosyadaki gibi). Düğümlerde şüpheli halka ve rapor rozeti gizlenir; karantina halkası kusursuz çizilir.
  - Dock rozetleri yok; zarf soluk ama basılabilir.
  - Sonuç: aç-kapa sonrası görüntü piksel piksel aynı; açıkken kenar bindirmesinde metin ve panellerde kayma yok.
  - Gösterilen değer, damga metni ve “Önerilmez” yazıları VM'dendir.
- **Veriye bağlı olanlar (anahtar değil):** bunların hepsi C2 JSON'undan gelir:
  - danışman düğme türü (`ink-small`) ve buna bağlı aralık (yalnız bağlantı 4, mürekkep düğme varsa 6)
  - sönük eylem kartı (`state: "dim"`, opaklık .55, hâlâ tıklanabilir)
  - hedef ve araştırma başlık damgaları, “Onay bekleniyor”
  - `stopped` proje (duraklatılmış gibi çizilir)
  - Son Şalter'in `danger: false`'u
  - Danışman aralığını İllüzyon bayrağına bağlamak “Göster”i 2 px kaydırıyordu; bu yüzden veriye bağlandı.
- **İzler:** silik notlar (#4A4640 %9 / %7) ve silgi lekesi (%2,5) Linear'da neredeyse görünmüyordu. CSS opaklığı `DeskMath.LinearOverlayAlpha` ile harita kâğıdı (≈0,85) üstündeki eşdeğerine çevrilir; referansla aynı yoğunlukta görünürler. Raptiye delikleri `map_pinhole` (6 px), analist raptiyelerinin konumunda.
- **Açık karar 5:** varsayılan sessiz (tek seferde). `IllusionPresenter.analystFadeSeconds` > 0 ise analist katmanı ease-out ile solarak kaybolur (6 sn ile sınandı); oyunda (F8) güne bağlanabilir.
- **Dosya dikey yığını:** C2'de tek kaynak satırı olduğundan dosyadaki bloklar 47 px yukarı kayıyor (CSS flex sütun, aralık 6). Dosya artık bölümleri veriye göre diziyor:
  - sızma sayfası 208,5 − 25 × eksik satır; başlıkta damga varsa +1
  - tehlikeli kartın satırı 68, diğerleri 66
  - bölüm içi göreli konumlar korunur
  - C1 sonucu değişmedi.
- **Doğrulanmış sızma başlığı:** damga (“ONAYLANDI”); öteki güvenlerde işaret. Araştırma başlığı damgayla 15 → 19 px uzar; başlık öğeleri 2, altındakiler 4 px iner.
- **Teleks plakası** metne göre uzar (“TELEKS · 0 ms” 138 px). Nokta dokusu görünüm genişliğinden döşenir; önceden sabit döşeme noktaları sıkıştırıyordu.
- **İpucu katmanı (F10):** `ClueOverlay` Debug kanvasında.
  - Görünüm: 24 px pembe daire, 2 px kenar, 3 px ışıma (Linear alfa), Courier 12.
  - İşaret konumu VM'den gelir (dairenin sol-üstü).
  - Üstüne gelince numara ve açıklama ipucunda görünür. Bunun için Debug kanvasına GraphicRaycaster eklendi; yalnızca işaretler ışın yakalar.
  - Yalnızca Editor/Development'ta çalışır.
- **F6 (geliştirme):** veriyi değiştirmeden İllüzyon sunumunu aç/kapa.
- **Bilinen küçük sapmalar:**
  - Hedef kutularındaki tarama fazı farklı (CSS periyodu 5,09 px, sprite 5 px). C1'de de var, görsel olarak aynı.
  - Teleks farkı yoğun metnin kenar yumuşatmasından geliyor; konumlar hizalı.
- **F5'e not:** C3 ön ayarının YZ günlüğü şeması farklı; `Load` artık hatayı günlüğe yazıp masayı düşürmüyor.

## Faz 5

- **Kullanıcı onayı (2026-10-12):** tek mesh spot, sahte önizlemeler ve bant kırpma onaylandı.

- **C3 verisi:** `C3_event.json` olayın kendisidir. Sahte kaynak C3'ü “C1 masası (duraklatılmış) + `DeskViewModel.Event`” olarak kurar.
  - Üst düzey `aiLog`/`advisor`, olayın kendi türlerine (`EventLogVM`, `EventAdvisorVM`) gider; masanın YZ Günlüğü penceresiyle çakışması F4'te kurulumu düşürüyordu.
  - Dikdörtgenlerdeki `"auto"` −1 olur.
  - Önizlemesi olmayan seçeneklere sahte kaynak ilk kazanç ve ilk bedelden not üretir (oyunda VM verir).
- **Spot:** `modal_spot_148` + 4 düz Image yerine tek `SpotlightGraphic` mesh'i kullanılır.
  - Ekranın tamamını kaplayan karartma (CSS .78 → Linear .949), 148 px delik ve 12 px koyu halka (CSS .38 üstüne .78 ≈ .86 → Linear eşdeğeri) çizilir.
  - Delik ve halka kenarları ±0,5 px yumuşatılır.
  - Gerekçe: Linear telafili karartma, sprite'a gömülü .78 ile dikiş verirdi.
  - Atlas beyazıyla çizilir. Raycast her yeri keser, delikten de tıklanmaz (arka plan etkileşimsiz).
- **Yerleşim:** olay katmanı, harita kökü gibi 1920×1080 ortalı bir kökte durur; spot, halka ve iplik her en-boy oranında düğümle hizalı kalır (21:9 sınandı). Karartma taşmayla bütün ekranı kaplar.
- **Akışlar:** bildirim, kart gövdeleri, YZ günlüğü ve danışman notu CSS akışını izler; yükseklikler veriden gelir:
  - paragraf ve risk satır sayısı
  - kaynak, bedel ve kazanç sayısı
  - günlük satırları
  - Seçili kartta kenar 2 px olduğundan gövde 1 px kayar.
  - İmza alanı alta yaslıdır; içerik taşarsa akışta kalır (CSS'te de taşıyor).
- **Seçim:**
  - VM'deki `state: "hover"` başlangıç seçimidir; 1/2/3 ya da üstüne gelme seçimi değiştirir. Seçim, başka karta geçilene dek kalır.
  - Seçili kart 7 px kalkar ve 2 px mürekkep kenar alır; sarı ÖNİZLEME notu ile mürekkep “Kararı imzala” + [Enter] görünür.
  - Diğer kartlarda kesik yuva ve kâğıt düğme + kendi tuşu bulunur. Her kartın düğmesi kendi seçeneğini imzalar.
- **Girdi:** olay açıkken `DeskInput` yalnızca olay tuşlarını alır:
  - 1/2/3 ile seçim, Enter ile imza.
  - Boşluk basılıyken alt şerit dışındaki her şey (karartma ve spot dahil) alfa 0 olur; harita görünür, arka plan yine etkileşimsizdir.
  - Esc ve masa kısayolları çalışmaz (karar zorunlu).
  - Onay kartı, olay kartının üstünde kalır (aynı Modal canvas'ında, sonra çizilir).
- **Hareket:** karartma 150 ms'de, kâğıtlar 8 px düşerek 180 ms'de belirir. `EventModalView.CompleteAnimation()` geçişi bitirir; yakalama aracı bunu kullanır.
- **Danışman bağlantısı:** VM komutu boşsa olay bölgesine odaklanır (`FocusRegion:AVR`). CSS'te bağlantı `padding: 0`'dır; metin içerik kenarındadır.
- **Bant:** danışman notundaki bant CSS'te notun `clip-path`'iyle kırpılır. Dikey dolgulu Image ile yalnızca alt 7/15'i çizilir.
- **Tuş fişi:** `KeyChipView`'a boy, yazı boyu ve renk anahtarı alanları eklendi. Kartta 22 px / CP 13; alt şeritte krem zemin ve #9A9284 kenar. Varsayılanlar eskisiyle aynı.
- **Tema:** `event.slotEdge` (#BDB29A) eklendi.
- **Ölçüm:** bölge bazında satır/sütun profilleri kaydırılarak karşılaştırıldı; bütün bölgeler ±1 px içinde.
  - Paragraf TMP üst hizası CSS yarım satır aralığını içermediği için 2 px üst kenar boşluğu aldı.
  - Döndürülmüş günlük ve not farkları, döndürülmüş metnin kenar yumuşatmasından gelir.

## Faz 6

- **Kullanıcı onayı (2026-10-12):** Linear telafili tarama dokusu aracı ve etiket konumlarının sahte veride ölçümden gelmesi onaylandı.

- **C4 verisi:** sahte kaynak C4'ü “C1 masası + `DeskViewModel.GameOver`” olarak kurar.
  - Etiket kutularının konumu JSON'da yok; C4 TSV'den sahte kaynağa eklendi. Oyunda (F8) köprü verecek.
  - Alt çubuk eylemlerinin komutu yoksa sıradan çıkarılır: ana menü, günlükler, aynı tohum, yeniden başla.
- **Tam ekran:** oyun sonu Modal canvas'ında, ahşap + 12 px içeride 2 px kenarlı keçe üstünde çizilir.
  - Açıkken masanın canvas'ları (BG, Map, HUD, Windows, Ticker, Bottom, Overlay) kapatılır, fazladan çizim yapılmaz.
  - Rapor 1920×1080 ortalı kökte durur; 21:9'da keçe kenarlara uzar.
- **Şerit grafik geometrisi:** `DeskChart` referans SVG'yle birebir tutar (testlerle sabitlendi).
  - Eşleme: x = 168 + 1608·gün/125, y = 618 − 2,76·değer.
  - Seriler tekdüze Catmull-Rom → kübik Bézier'dir. Uçlarda nokta ikilenir; kontrol noktasının y'si çizim alanına kırpılır; segment başına 12 örnek alınır.
  - Bantlar iki seri arasında 0,5 günlük adımlarla doğrusal ara değerle kurulur.
  - Izgara veri ölçeğinden çizilir (5 gün/%5 ince, 10 gün/%25 kalın), doku döşenmez. Çizgiler tam piksele yaslanır.
  - Kırılma iğnesi olay çizgisinden (y 336) eğrinin 4 px üstüne iner; numaralar y 325'tedir.
  - Uç etiketleri serilerin son noktasına göre yerleşir.
  - Geometri her bağlamada VM'den yeniden kurulur. F5, oyun sonu açıkken serileri ve mühür günlerini değiştirir; grafik doğru yeniden çizilir (sınandı).
- **Linear telafisi:**
  - Karbon bant (%20) ve ızgara (%32/%55), kâğıt (≈0,94) üstündeki eşdeğer alfaya çevrilir.
  - Tarama dokusunun alfası dokuya gömülü ve ton 1'i aşamadığı için yeni araç `DeskLinearTextures` (menü: *Linear telafili dokuları üret*) `tx_hatch_red_linear.png`'yi üretir: zemin %7 → %12, çizgi %46 → %63.
- **Genel düzeltmeler (ölçümden; C1–C3 de iyileşti):**
  - **Düğme genişliği:** CSS genişliğe 1+1 px kenarlığı da katar. Kenarlıklı düğmeler 2 px dar çıkıyordu (C1 araştırma düğmeleri 128'e karşı 126). Bağlantı değişmedi.
  - **Damga metni:** CSS ortalarken son harften sonraki harf aralığını içeriğe sayar, TMP saymaz. Damga metni bu aralığın yarısı kadar sola alınır (büyük damgada 2,1 px).
  - Sonuç: C1 %3,1 → %3,0; C2 %3,1 → %3,0.
- **Yeni ayarlar:** `StampView.paddingOverride` (büyük damga 16/9); `DeskButton.padXOverride` ve `textKeyOverride` (alt çubuk: 16 px iç boşluk, açık renk bağlantı).
- **Girdi:** oyun sonu açıkken Enter (yeniden başla) ve L (günlükler) çalışır; diğer kısayollar kapalı. Sahte kaynakta “Yeniden başla” C1'i yükler.
- **Ölçüm:** bölgeler ±1 px içinde (uç etiketleri +1).

## Faz 7

- **Kullanıcı onayı (2026-10-12):** C1'de 65 batch kabul edildi (hedefin altına inmek için canvas birleştirme ya da font azaltma yapılmayacak; F8'de gerçek veriyle yeniden ölçülür). EN kısa biçimleri (`POL. CAPITAL`, `Monitoring`, maliyette `d`) onaylandı.

- **EN sahte veri:**
  - `MockEnglish` görünüm modelini yansımayla dolaşır ve `Data/Mock/mock_en.txt`'yi uygular. Önce tam eşleşmeye, sonra `re:` kalıplarına bakar; ardından EN sayı/gün biçimi gelir (47,3 → 47.3, %38 → 38%, G-074 → D-074).
  - Kod alanlarına (kimlik, güven, durum, tuş…) dokunmaz. Oyun çeviri tablosunu şişirmez; oyunda veri kaynağı metni seçili dilde verir.
  - Genel bir kalıp yakaladığı halde çıktıda Türkçe kelime kalırsa bunu da eksik sayar.
- **Sabit metinler:** prefab'lardaki `DeskLocText` kaynakları menüyle `Localization/DeskStaticWords.cs`'ye (`Loc.N`) toplanır; çeviri aracı onları görür. Tablo 989 metin, EN eksiği 0.
- **Dar alanlarda EN kısa biçimleri:** gösterge başlığı `POL. CAPITAL`, eylem başlığı `Monitoring`, maliyet satırlarında gün `d` (`4 funds · 2 PC · 2d`). Tam biçim geniş yerlerde kalır (döküm fişi `POLITICAL CAPITAL · BREAKDOWN`).
- **Dile göre akış (TR yerleşimi aynen korunur):**
  - **Harita lejantı:** `MapLegendFlow` (Map.prefab/Legend) satırları yeniden akıtır. Aralıklar TR kaynak metin genişliğinden çıkarılır; EN'de uzun başlık sonraki öğeleri iter. Satır kutuya sığmazsa öğeler arası aralıklar en az 6 px'e kadar orantılı daralır; şekil–etiket aralığı korunur.
  - **Olay alan ızgarası:** CSS `auto 1fr auto 1fr`, sütun aralığı 12. Etiket sütunu en geniş etiket kadardır; TR'de ölçülen 78,5 / 354,5 / 419 aynen çıkar.
  - **Oyun sonu damgası:** CSS sol kenarı 686'da sabit; başlık uzarsa damga başlığın en az 24 px sağına gider.
- **Metin denetimi** (`DeskTextChecker`, Play):
  - Her iki dilde dört ön ayarı tarar. C1'de bütün pencereler açılır: animasyonsuz, sonra kapatılır.
  - Yakaladıkları: Auto Size/UpperCase, 11 px ve 15 px Kalam sınırları, taşma, kardeş metin çakışması, damga/tuş fişi çakışması, EN'de Türkçe kalıntı, kısaltılmış metin, kural ihlalleri (sprite'sız Image, Outline, LayoutGroup, etkileşimsiz raycast).
  - Zemin kardeşte de olabilir (`Fill`, `Box`). Gizli canvas'lar atlanır. Modal karartması bilinçli engelleyicidir.
  - Önceki sürüm, yeni açılan pencereleri açılış solması (alfa 0) yüzünden atlıyordu; düzeltildi ve gerçek bir EN taşması (maliyet satırı) bu sayede bulundu.
  - Doğrulama: eski EN metinleri geri konunca 17 hata verdi, düzeltilmiş hâlde 0.
- **Performans:**
  - **Ölçüm yöntemi:** Editor arka plandayken Game görünümü çizilmediği için `UnityStats` güncellenmiyor. Ölçüm Profiler UI Details'tan alınır (`DeskPerfProbe`): canvas başına batch, kırılma nedeni, malzeme ve öğeler.
  - **Kök neden:** statik font atlaslarında boşluk (U+0020) yoktu; üretici `c > ' '` ile eliyordu. Her boşluklu metin dinamik yedekten ayrı bir alt ağ ve malzeme açıyordu.
  - **Düzeltme:** üretici boşluğu da alır; mevcut asset'lere atlas yeniden çizilmeden eklendi (*Statik fontlara boşluk ekle*). C1 98 → 65 batch.
  - **Yan etki:** boşluk ilerlemesi artık ana fonttan; Kalam notlarında kelime aralığı referansa yaklaştı (1–2 px). C1/C2 toplam fark yine %3,0.
  - **Kalan yapı (C1 65):** BG 3, Map 17, HUD 10, Windows 19, Ticker 9, Bottom 7. Canvas başına neredeyse her font malzemesi bir batch: CP-R/B, LF-R/SemiBold/Bold/ExtraBold, Kalam-R/B, ayrıca DejaVu ve ağırlık yedekleri.
  - **Ek katman:** dizin kartı çizgileri (`tx_index_lines`, PROMPT gereği RawImage) hedef kartının içeriğini ikinci derinlik katmanına itiyor (≈ +7).
  - **Hedefin altına inmek** canvas birleştirme ya da font ağırlığı azaltma gerektirir; tasarım ve PROMPT kararı olduğundan kullanıcıya soruldu.
  - **Diğer ekranlar:** C2 53, C3 77, C4 17.
  - **Boşta:** masa betiklerinde GC 0; `Graphic` yeniden kurulumu 0. Yalnız teleks kayması Ticker canvas'ını her kare toplar (0,007 ms).
- **Yakalama:**
  - Son yakalamalar `captures/F7_C?_<çözünürlük>.png` ve `F7_C?_en.png`'de.
  - Ön ayar yüklendiği karede alınır; teleks kaydığı ve saat ilerlediği için beklemek farkı büyütür.
  - Yeni açılmış bir Play oturumunda ibreler ilk saniyelerde açılış animasyonundadır; önce bir ön ayar döngüsü yapılır.

## Faz 8

- **Kullanıcı kararları (2026-10-12):**
  - **Sahne:** yeni masa yeni bir sahnede çalışır (`Scenes/DeskGame.unity`): GameController + ses + yeni masa. `Main.unity`'ye dokunulmaz; eski oyun aynen oynanır. Build ayarına ekleme ayrıca sorulacak.
  - **Tasarımı olmayan ekranlar** hepsi masa tarzında yapılacak; her biri için önce taslak, sonra onay: giriş/ayarlar/kampanya, hediye teklifi, perde yırtılması, sahte zafer, uyarı fişi (toast), eğitim.
- **Alt fazlar:**
  - F8.1: `Corrigible.Desk.Bridge` (oyun durumu → `DeskViewModel`, niyetler → `GameController`) ve `DeskGame` sahnesi. Tasarımı olan her şey gerçek oyunla çalışır: masa, olay kartı (C3), oyun sonu (C4).
  - F8.2: eksik ekranların masa tarzı taslakları (HTML, referansın CSS diliyle) → kullanıcı onayı.
  - F8.3: onaylanan ekranların uGUI kurulumu.
  - F8.4: eğitimin yeni masaya bağlanması.

### F8.1 · Köprü ve oyun sahnesi

- **Kullanıcı onayı (2026-10-12):** F8.1 onaylandı; F8.2 (taslaklar) başladı.
- **Köprü:** `Corrigible.Desk.Bridge` asmdef'i (Core, Game, Desk). Üç parça:
  - `SimDeskBuilder`: saf C#, testlenebilir. Oyun durumu → `DeskViewModel`.
  - `SimDeskDataSource`: GameController olaylarıyla kirlenir; karede en çok bir kez kurar, zaman durunca hiç kurmaz.
  - `SimDeskCommands`: niyet → GameController; sonuç mesajları uyarı kuyruğuna gider.
- **Algı Katmanı:** sızma ve küresel değerler `Shown`/`Global`'den okunur. Gösterge güveni:
  - izleme kapsamı 9/12 ve üstü ise onaylı, değilse rapor (≈, ±);
  - işaretli medya varsa bağımlılık şüpheli;
  - Tekillik Yorumlanabilirlik'ten sonra onaylı;
  - İllüzyonda her şey “onaylı” görünür (yalanın kendisi), oyun sonunda gerçek değerler onaylıdır.
- **Harita:**
  - Düğümler projeksiyondan yerleşir (Hindistan ve Okyanusya tasarımdaki noktaya tam düşer).
  - Yollar tasarımın yapım kuralıyla çizilir (map.json'u ±1 px verir, testli).
  - Pasifik bağlantısı iki kenar oku ve kenar etiketi olur. Karantina yolu düğümden 29 px'te keser.
  - Etiket yönü masaya özel bir tabloda (oyunun LabelPos'u eski harita içindi).
  - Ekipler düğümün sol-üstünde; yoldaki ekip için varış etiketi ve kesik kalem oku çizilir.
- **Analist katmanı (türetilmiş dekor):**
  - bilinen düğümlere raptiye;
  - gösterilen değeri 50+ olan bölgeden 20+ komşulara yayılım ipliği;
  - çekirdek biliniyorsa kırmızı “çekirdek!” notu;
  - karantina notunda kararın günü (kayıttaki komut);
  - şüpheliye kalem “?”;
  - “Kör noktalar” notu.
  - İllüzyonda notlar silik iz olarak kalır.
- **Araştırma:**
  - Oyunda tek aktif proje var ve duraklatılamaz: VM'de `CanPause = false`, diğer başlat düğmeleri “Sırada” ile kapalı.
  - Süre ve toplam bedel alt satırda (oyundaki adlar uzun).
  - Acil durum fonu oyundaki 4 seçenek: prefab'a 4. radyo eklendi.
  - İllüzyonda süren proje “Kendiliğinden durdu · gerekmiyor” görünür (C2 ipucu).
- **Dosya:**
  - Eylem bedeli ve kapalı olma nedeni ActCore'un koşullarıyla hesaplanır, durum değişmez. Kartta kısa neden, önizlemede ayrıntı.
  - Saha düğmesi: bölgede ekip yoksa en yakın hazır ekibi gönderir (B); varsa baskın.
  - Baskın taktik haritası bu sahnede yok: kapalı, nedeni notta. Taktik harita F8.3'te.
- **Olay (C3):**
  - Oyunun ikilem kartları ve hediye teklifi aynı kâğıtla çizilir.
  - Etki metni (“−5 SG · bölgede güven +15”) bedel/kazanç satırlarına ayrıştırılır; bütün deste testli.
  - Önizleme şimdiki değerlerden hesaplanır. Kaynaklar o bölgenin son haberleridir.
  - Bölgeye bağlı olmayan kartta spot yoktur, masa düz karartılır. Bu, görünümde düzeltilen bir hataydı: delik yokken karartma beyaz çiziliyordu.
  - Bölge kâğıtların altına düşüyorsa halka ve iplik çizilmez.
  - Uzun başlık iki satıra kırılır; alt şeritte seçenek sayısı kadar tuş fişi; boş BEDEL/KAZANÇ bölümü gizlenir.
- **Oyun sonu (C4):**
  - Grafik oyun uzunluğuna ölçeklenir (DayMax, ızgara).
  - Kırılma anları 6 tanedir, 7. kart her zaman sonuçtur. Etiketler serinin ucuna sığmazsa çizginin soluna geçer.
  - “Günlükleri incele” raporu kapatır; masa gerçek değerlerle kalır, danışman bandında “Rapora dön / Yeniden başla” çıkar.
  - “Aynı tohumla dene” `GameController.NewGame(tür, tohum)` ile çalışır.
- **Saat:** gün içindeki saat görünümde yerelde ilerler (`ClockVM.MinutesPerSecond`, “00:00”…“23:59” önbelleği, bellek ayırmaz). Sahte veride 0, saat durağan.
- **Uyarılar:** eylem sonuçları, ekip varışı ve perde yırtılması danışman bandında “KYGA BİLDİRİMİ” olarak gösterilir. En yenisi önce, tekrar yok, 4 günde düşer. Masa tarzı uyarı fişi F8.2 taslağında.
- **Dil:**
  - Köprü metni geçerli dilde üretir. 176 yeni metin İngilizceye çevrildi.
  - Dar alanlar bağlamlı anahtar alır (`[gösterge] SİYASİ GÜÇ` → POL. CAPITAL, `[eylem] İzleme ağı` → Monitoring, `[damga] GİZLİ` → SECRET). Eski arayüzdeki aynı Türkçe metnin çevirisi değişmez.
  - Masa satırlarındaki EN “KYGA” oyunun terimine (AISA) çevrildi.
  - Oyunun ürettiği metinler (haber, YZ günlüğü, kırılma anı) üretildikleri dilde kalır. Bu oyunun mevcut davranışıdır; oyun ortasında dil değişirse eskiler Türkçe kalır.
- **Oyun koduna eklemeler (davranış değişmez):** `GameController.DayProgress`, `SecondsPerDay(hız)`, `NewGame(tür, tohum)`.
- **Sahne:** `Scenes/DeskGame.unity` (sandbox kopyası: sahte kaynak, ön ayar menüsü kaldırıldı; GameController, AudioDirector, köprü eklendi; kameraya AudioListener).
- **Ölçüm:**
  - Oyun sahnesi C1 benzeri durumda 55 batch.
  - Masa kodunda boşta GC 0. Eski `AudioDirector.Update` her kare ~200 B ayırıyor (oyun kodu, ayrı iş olarak önerildi).
  - Metin denetimi oyun durumlarında (orta oyun, pencereler, olay, hediye, illüzyon, oyun sonu, inceleme), TR oyunda TR, EN oyunda EN: 0 hata. Uyarı yalnız onaylı arka sekme kısaltması. TR olay ve hediyede ilk denetimde bir kez görülen “Kararı imzala 6 px” kaydı yeniden denetimde çıkmıyor; çizilen karede düğme doğru.

### F8.2 · Taslaklar

- **Kullanıcı onayı (2026-10-12):** dokuz taslağın hepsi onaylandı; taktik harita (Baskin sahnesi ve paneli) olduğu gibi kalır. F8.3 başladı.
- **Yöntem:** taslaklar referansın kendi CSS'iyle HTML olarak yazıldı (`drafts/`). Yüklü Chrome ile başsız çizildi; referans C3 bu yolla referans PNG'ye ±6 bin piksel içinde çıkıyor, ayrı araç kurulmadı. Arka planlar gerçek oyun yakalamaları.
- **Kurallar:**
  - Masanın malzemeleri kullanılır: manila klasör, dizin kartı, bildirim kâğıdı, damga, tuş fişi, sarı not, teleks.
  - Yeni malzeme yalnız iki yerde var:
    - sahte zaferin kusursuz beyaz resmî yazısı (bilinçli kopukluk: “fazla temiz” olan yalan);
    - uyarı fişinin yırtık teleks şeridi.
- **Taktik harita (Baskin sahnesi):** olduğu gibi kalır; masa tarzı yalnız giriş (operasyon emri) ve dönüş (operasyon raporu). Sahnenin kendi paneli ayrı bir iş.

### F8.3 · Kurulum

- **Kullanıcı onayı (2026-10-12):** F8.3 onaylandı (taslaktan sapmalar dahil); F8.4 (eğitim) başladı.
- **Durum:** onaylı taslakların hepsi kuruldu ve `DeskGame`'de oyunla çalışıyor.
- **Prefab'lar** (`Prefabs/Screens/`):
  - Yeni: `IntroScreen`, `SettingsCard`, `CampaignScreen`, `FakeWinScreen`, `ToastStack`.
  - `EventModal`'a belge kipi eklendi: `EventDocView` (sonuç satırları, sarı kutular, risk, iki düğme), yırtık alt kenar, alt şerit kökü.
  - `TopRail`'deki ⚙ tuşu ayarlara bağlandı.
  - Prefab'lar geçici bir editör betiğiyle taslak ölçülerinden (Chrome ölçü dökümü) kuruldu; betik silindi.
- **Yerleşim:**
  - Modal canvas'ında sıra (alttan üste): GameOver, Kampanya, Olay, Onay, Giriş, Sahte zafer, Ayarlar.
  - Uyarı fişleri Overlay canvas'ında; tıklansın diye Overlay'e GraphicRaycaster eklendi.
  - İki sahnede de kurulu. Ayarların kaynağı oyunda `BridgeSettings` (ses tercihleri `AudioPrefs`, dil), sandbox'ta `MockDeskSettings`.
- **Akış** (eski `ModalView` ile aynı kurallar; kuralı GameController uygular):
  - Her yeni oyunda giriş dosyası açılır; kayıt yüklenince açılmaz. “Göreve başla” 1× başlatır, “Eğitimle başla” duraklatılmış kapatır (eğitim F8.4'te).
  - Rakip, kurallar ve zaman ölçeği oyunun mevcut yöntemleriyle yeni oyun kurar.
  - Dil sahne yeniden yüklenmeden değişir; tercih oyunun anahtarına yazılır. Giriş açıkken (gün 0, komutsuz) oyun yeniden kurulur ki YZ'nin satırları da yeni dilde üretilsin.
  - Kampanya: oyun sonu raporunda “Sonraki kriz (n/5)” (kampanyada “Aynı tohumla dene / Yeniden başla” yok) → kampanya dosyası → yükseltme → sonraki kriz giriş dosyasıyla başlar. Kampanya bitince “Kampanyayı bitir”.
  - Sessiz Zafer: sahte duyuru 3,6 sn inandırır, 0,9 sn bozulur, sonra rapor açılır. Arkadaki masa son ana kadar illüzyonu gösterir (`HideEnding`). Ses yönetmeni iki anda da haberdar edilir.
  - Belge kipi:
    - perde raporu (oyunun Veil olayı);
    - baskın emri (B: bölgede ekip varsa ve `RaidCheck` geçerse; geçmezse neden fişte);
    - operasyon raporu (taktik harita kapanınca okunmamış baskın kaydı).
    - Enter birincil, Esc ikincil düğme.
  - Taktik harita: `RaidLauncher` DeskGame'e eklendi. Baskın sürerken masa canvas'ı, bütün ışın yakalayıcılar, kamera ve dinleyici, masa girdisi kapanır. Baskin sahnesinin kendi brifingi (taktik kurallar) olduğu gibi kalır.
  - Zaman: masanın kâğıtları (giriş, kampanya, sahte zafer, ayarlar, belge, okunmamış rapor) `GameController.Hold`'u tutar; köprü her kare yazar. Olay kartı, hediye ve baskın zaten `Blocked`.
  - Uyarılar artık danışman bandında değil, fişte: başarılı (mühür, gün ve saat), YAPILAMADI, SONUÇ. 3,6 sn sonra solar, tıklanınca kapanır, en çok üç; danışman bandı yalnız danışman notudur.
  - Klavye önceliği: Ayarlar (Esc) > Sahte zafer > Giriş (1/2/3, Enter) > Kampanya (1/2/3, Enter) > Belge (Enter/Esc) > olay > oyun sonu > onay > masa. Pencere yokken Esc ayarları açar (önce açık pencereler kapanır).
- **Taslaktan sapmalar:**
  - Tekli seçimler (zaman ölçeği, dil) taslaktaki daire yerine masanın X kutusu (RadioCheck).
  - Uyarı fişinin sol kenarı yırtık değil, düz; “kapat ×” metinden 12 px sonra (taslak 20).
  - Kampanya metinleri oyunun kuralına göre düzeltildi. Taslakta “kazandığın her kriz YZ'yi güçlendirir” ve atlatılan krizde “YZ bu zaferden güçlenerek çıktı” yazıyordu; oyunda kaybedilen kriz YZ'ye yetenek verir. Atlatılan krizde oyunun metni: “Sonraki kriz daha zor bir rakiple…”.
  - Baskın emrindeki “Başarı %64 · yan hasar riski orta” tahmini oyunda yok. Yerine olası sonuç yazılır (başarı: sızma −40; başarısızlık: +15, güven −10). Ekip kişi sayısı yazılmaz. HEDEF alanında “sunucu kümesi” yok (uzun bölge adlarında taşıyordu; tür başlıkta).
  - Perde raporundaki ekran değeri illüzyonun formülünden geri hesaplanır (`LastVeil` + bölgenin `FakeLow`'u).
  - Operasyon raporunun satırları oyunun `ResolveRaid` kuralından kurulur (sonuç metni RaidView'da kayboluyordu); tur sayısı kayıttaki sonuçtan.
  - Kampanya yükseltme kartlarında simge yok (taslaktaki gibi).
  - Sahte zaferde tarama çizgileri yok; kayan dilimler metin kopyası (çeviriyle değişir).
  - Eski oyun sonundaki “Hafızayı sil” masada yok.
- **Görünüm düzeltmeleri (C3'ü de etkiler):**
  - Seçenek kartında imza düğmesinin genişliği etiketten önce verilir. İlk bağlamada etiket sola taşıyordu: F8.1'deki “Kararı imzala 6 px” kaydının nedeni buydu.
  - Bedel/kazanç satırının noktalı çizgisi satır genişliğindedir (seçili kartta 2 px dışarı taşıyordu; artık CSS referansıyla aynı yerde biter).
  - Bildirim kâğıdında uzun başlık önce 40 px'e küçülür, yine sığmazsa satır kırar (belge taslakları 40 px).
  - Paragrafsız belgede kaynaklar alt başlığın hemen altından başlar.
  - `DeskUi.CapRule`: bölüm başlığının çizgisi çevrilen başlığın bittiği yerden başlar.
- **Doğrusal renk uzayı:** karartmalar telafili (CSS .78 → `modal.scrim`, .55 → %80); taslakla aynı ton (açık kâğıt üstünde 64 ↔ 62).
- **Tema:** `fakewin.paper`, `fakewin.edge`, `fakewin.muted`, `toast.edge` eklendi (`DeskThemeSetup`).
- **Dil:** 139 yeni metin İngilizceye çevrildi (eksik 0, doğrulama temiz). Prefab'daki sabit metinler `DeskLocText` + `DeskStaticWords` ile tabloda.
- **Ölçüm ve kalite:**
  - EditMode testleri 105/105 (7 yeni: giriş, kampanya, kampanyada oyun sonu, belgeler, sahte zafer, fişler, sahne bağlantıları).
  - Metin denetimi yeni ekranlarda TR ve EN 0 hata. Denetleyiciye dil adı istisnası eklendi (“Türkçe”).
  - Sahte C1, C2, C4 F7 yakalamalarıyla piksel aynı. C3'te 3 piksel fark: noktalı çizgi düzeltmesi.
- **Oyunda denenen akışlar** (Play, DeskGame):
  - giriş (rakip, kural, ayarlar, başla);
  - klavye (1/2, Enter, Esc);
  - baskın (emir → Baskin sahnesi → rapor → masa);
  - Sessiz Zafer (duyuru → rapor);
  - kampanya (başlat → kriz sonu → sonraki kriz → yükseltme → çık).
- **Kullanıcı verisi:** DeskGame'deki denemeler oyunun gerçek kayıt dosyalarını ve tercihlerini kullanır (`savegame.json`, `player_profile.json`, `campaign.json`, aynı PlayerPrefs). F8.3 denemelerinden önce yedeklendi, sonunda geri yüklendi. F8.1 denemeleri bu dosyaları zaten değiştirmişti (profil sayacı, kayıt); o değişiklikler geri alınamadı.

### F8.4 · Eğitim

- **Kullanıcı onayı (2026-10-12):** F8.4 onaylandı. Bildirilen iki sorun ayrıca düzeltildi (aşağıda “F8.4 sonrası düzeltmeler”).
- **Durum:** eğitim yeni masada çalışıyor. Bununla Faz 8'in alt fazları tamamlandı.
- **Yapı:**
  - `DeskTutorialRunner` (köprü) eski `TutorialController`'ın kurallarıyla çalışır: aynı içerik (`Settings/TutorialSequence.asset`), aynı koşullar (`TutorialConditions`), aynı kalıcı anahtarlar (`corrigible.tutorialDone`, `corrigible.tipsSeen`). İlerleme eski oyunla ortaktır. Asset'e ve oyun koduna dokunulmadı; “bölge seçili” koşulu masanın seçiminden okunur.
  - Gösterilecek not `TutorialVM` olarak VM'e girer. `DeskTutorialView` (taslak `drafts/egitim`) hedef adlarını (oyunun `TutorialTarget` adları) sahnedeki dikdörtgenlere bağlar.
  - Eğitim katmanı kendi canvas'ında (`Tutorial`, sıra 105): kâğıtların üstünde, hediye ipucu olay kâğıdının üstüne düşsün diye.
- **Akış:**
  - Giriş dosyasında “Eğitimle başla” rehberli adımları başlatır; yeni oyun eğitimi keser.
  - Adım İleri'yle (Enter) ya da koşulu sağlanınca ilerler. “Baştan” ilk adıma döner (koşulu sağlanmış adımlar kendiliğinden geçilir). “Eğitimi atla” bitirir: bitti işareti ve fiş.
  - İpucu bir mekanik ilk kez çıkınca bir kez gösterilir; × ya da Enter kapatır, Esc de kapatır.
  - Kâğıt açıkken (giriş, kampanya, belge, olay, oyun sonu…) yalnız hediye ipucu görünür. Taktik harita açıkken eğitim görünmez.
  - Adımın `pauseGame`'i `GameController.TutorialHold`'u tutar. Rehberli eğitim sürerken danışman notu susar.
  - Adımın penceresi (eski sağ panel sekmesi) ve hedefin penceresi (araştırma, saha ekipleri) gösterilince açılır.
  - Rehber penceresine “EĞİTİM · Eğitimi baştan oynat” bağlantısı eklendi (eski Rehber sekmesindeki gibi: bitti işareti ve görülen ipuçları silinir, Solucan'la yeni oyun açılır).
- **Hedefler:**
  - Göstergeler, tek tek gösterge kartları, harita kâğıdı, bölge dosyası, eylem kartları, saha bölümü, teleks, hız tuşları, araştırma dosyası, Hizalama kolu, acil durum fonu, Son Şalter, saha ekipleri penceresi, hediye kâğıdı.
  - Birden çok dikdörtgenin birleşimi olabilir (dosyanın eylem kartları, saha bölümü). Hizalama kolu araştırma dosyasının çalışırken kurduğu satırlardan hesaplanır (`ResearchFolderView.BranchSpan`).
- **Yerleşim:**
  - Adım notu hedefin altına, sığmazsa üstüne, sağına, soluna konur. Hedef ekranın çoğunu kaplıyorsa (harita) notu içeride sol alta koyar.
  - İpucu hedefin yanına konur (dikeyde sığıyorsa), yoksa altına ya da üstüne.
  - Yerleşim her karede pencerelere göre güncellenir, bellek ayırmaz.
- **Taslaktan sapmalar:**
  - Kalem oku mürekkep yerine çerçeveyle aynı sarı: koyu karartma üstünde mürekkep görünmüyordu.
  - Karartma doğrusal renk uzayı için telafili (CSS .72 → .92).
  - İpucunun kapat düğmesi masanın × düğmesi.
  - Adım metinlerindeki satır sonları asset'ten gelir (eski arayüz için yazılmış).
- **Metin:**
  - Eski arayüze göre yazılmış iki metin (“Bölge panelindeki Saha…”, “Bölge panelindeki Saha operasyonu…”) masada karşılığıyla gösterilir (“Bölge dosyasındaki SAHA…”, “Saha operasyonu: baskın (B) önce operasyon emrini açar…”). Asset değişmedi; test, asset metni değişirse karşılığın sessizce düşmesini yakalar.
  - 7 yeni metin İngilizceye çevrildi.
- **Düzeltme (bütün masa):**
  - Sabit genişlikli `DeskButton` dışarıdan boyutlanınca etiketini yeniden ortalar.
  - Etiket ve tür değişmediğinde düğme yeniden çizilmiyor, seçenek kartındaki kâğıt imza düğmesinin etiketi 69 px'lik ilk genişliğe göre kalıyordu. F8.1'deki ve F8.3 denetimlerinde ilk olay kâğıdında görülen “Kararı imzala 6 px” kaydının gerçek nedeni buydu; F8.3'teki sıra düzeltmesi yetmemişti. Artık ilk denetimde de 0 hata.
- **Ölçüm ve kalite:**
  - EditMode 107/107 (2 yeni: asset metni ve hedef bağlantıları; not VM'de ve danışman susar).
  - Metin denetimi eğitim adımlarında ve ipuçlarında TR ve EN 0 hata.
  - Sahte C1, C2, C4 F7 ile piksel aynı; C3'te F8.3'teki 3 piksellik noktalı çizgi düzeltmesi.
- **Oyunda denenen akış** (DeskGame):
  - giriş → Eğitimle başla → hoş geldin → beş gösterge → sis (harita) → bölge seç → ekip gönder → zamanı başlat → izleme ağı;
  - Eğitimi atla (bitti işareti, fiş);
  - Baştan; yeni oyunda iptal;
  - bağımlılık ipucu (açık olay kartı kapanınca görünür, Enter kapatır);
  - hediye ipucu (olay kâğıdının üstünde, Enter yalnız ipucunu kapatır).
- **Kullanıcı verisi:** denemeler eğitim tercihlerini (bitti, görülen ipuçları) ve kayıt dosyasını değiştirdi. Hepsi denemeden önceki değerlerine geri yüklendi.

### F8.4 sonrası düzeltmeler (kullanıcı bildirimi, 2026-10-12)

- **Edit modda sahne:** Play'e basılmadan sahne açık gri bir yüzeyle örtülüyordu.
  - Asıl neden: Debug canvas'ındaki referans katmanı (`ReferenceOverlay`). Dokusuz bir RawImage, edit modda açık, ekranın tamamını beyaz örtüyordu. Ayrıca tam ekran gerilmiş seçim ipliği raptiyesi (`SelectionThread/EndPin`) ve bütün kâğıtların kökleri (GameOver, EventModal, ConfirmModal ve F8.3–F8.4 ekranları) edit modda açıktı.
  - Hepsi çalışırken `Awake`'te kapanıp `Bind`'da açılıyordu.
  - Düzeltme:
    - kâğıt kökleri, uyarı fişi şablonu, ipucu baloncukları (döküm, metin) ve seçim ipliği prefab'larda kapalı;
    - raptiye kendi boyunda (14 px);
    - referans katmanının RawImage'ı iki sahnede kapalı (F11/F12 açar).
  - Çalışma zamanı davranışı değişmedi (görünümler kökü bağlamadan önce açıyor). Edit modda masa görünüyor.
- **Baskın sırasında masa görünüyordu:**
  - Neden 1: `RaidLauncher` yalnız DeskUI kök canvas'ını kapatıyordu; kendi sıralaması olan iç içe canvas'lar üst canvas kapanınca kök canvas gibi çizilmeye devam eder. Düzeltme: DeskUI altındaki bütün canvas'lar gizleme listesinde.
  - Neden 2 (asıl): `GameOverView` her bağlamada masa canvas'larını “rapor kapalı → açık” diye yeniden açıyordu. Baskın sürerken masa bağlandıkça RaidLauncher'ın kapattığı canvas'lar geri geliyordu. Düzeltme: canvas'lara yalnız rapor açılıp kapanırken dokunulur.
  - Doğrulama: baskında bütün masa canvas'ları kapalı, taktik harita (tahta, panel, brifing) görünüyor; baskın bitince hepsi geri açılıyor.
- **Kalite:** EditMode 107/107. Sahte C1, C2, C4 F7 ile piksel aynı, C3'te bilinen 3 piksel.
  - Not: eylem kartlarında gerçek fare bir karta girince sahte verinin “hover” önizlemesi o oturum boyunca kapanır (`ActionCardView.live`, F7'den beri). Karşılaştırma bu bayrak sıfırken yapılmalı.
- **Kullanıcı verisi:** kullanıcının yeni kaydı (Manipülatör, 157. gün) ve tercihleri denemelerden önce yedeklendi. Play'den çıkarken oyunun kendi `Save()`'i deneme oyununu yazdı; kayıt yedekten geri yüklendi. Kayıt ve tercihler kullanıcının bıraktığı hâlde.

### Edit mod önizlemesi (kullanıcı isteği, 2026-10-12)

- **İstek:** Play'e basmadan da masa düzgün görünsün; şablon üzerinde elle düzenleme yapılabilsin.
- **Yapı:**
  - `DeskEditPreview` (editör) masa sahnesi (DeskPresenter olan) açılınca, derlemeden sonra ve Play'den dönünce sahte veriyi bağlar (`Data/Mock`, varsayılan C1).
  - Menü: `Corrigible/Desk UI/Edit modda önizleme` → Göster (aç/kapa), Yenile, C1 masa / C2 illüzyon / C3 olay / C4 oyun sonu.
  - Veri sahneye hiçbir şey eklemeyen geçici bir sahte kaynaktan gelir. `DeskPresenter.Preview` pencere tablosunu (`WindowManager.EnsureInit`) ve seçim ipliğini kurup VM'i bağlar.
  - Dinamik listeler (harita, araştırma, teleks, pencere içerikleri, grafik) yalnız bu bağlama sırasında edit modda kurulur (`DeskUi.EditPreview`).
  - Kopyalar sahneye kaydedilmez (`DeskUi.MarkPreview`, `DontSaveInEditor`; havuz ve uyarı fişi). Derlemeden sonra sahipsiz kalan kopyalar silinir.
  - Edit modda eklenen bileşen kaydedilmez (`DeskUi.AddTransient`).
  - Pencere açılış canlandırması edit modda çalışmadığı için pencere doğrudan görünür, bileşen eklenmez.
  - Oyun sonu raporu edit modda masa canvas'larını kapatmaz: kapalı kaydedilirse oyunda masa görünmezdi.
  - Önizleme sahneyi kirli işaretlemez; kaydedilmemiş değişiklik uyarısı çıkmaz.
- **Sonuç:** edit modda C1, C2, C3 Play'deki F7 yakalamalarından %0,00–0,03 farklı (canlandırma ve ipucu farkları), C4 aynı.
  - Play'de önizleme kopyası yok; Play'den dönünce önizleme kendiliğinden yeniden kurulur.
  - EditMode 107/107.
- **Elle düzenleme notu:**
  - Veriden yerleşen özellikler (metinler, satır ve kart konumları, açık pencereler, gösterge ibreleri) her önizlemede ve oyunda yeniden kurulur.
  - Kalıcı tasarım değişiklikleri prefab'larda ve şablon öğelerinde yapılır: renk, yazı, boyut, sabit konum, şablon satırları.
  - Önizlemenin yazdığı değerler sahne kaydedilirse sahneye geçebilir. Zararsızdır: oyunda yeniden kurulur.

### Sahne yerleşimi, ekip seçimi, sürükle bırak (kullanıcı isteği, 2026-10-13)

- **İstek:** olay bildirimindeki önizlemeler üstlerine gelmeden de görünsün; bölgeye gönderilecek ekip seçilebilsin (yalnız en yakını gönderilebiliyordu); harita, dosya, araştırma gibi paneller sürükle bırakla istenen yere konabilsin; yerleşim (konum ve ölçek) sahnede ayarlansın, Play'e basınca yeniden kurulmasın, yalnız veri dolsun.
- **Olay önizlemeleri:** üç seçenek kartının önizlemesi her zaman açık (`EventOptionView`: önizleme varsa gösterilir; seçim yalnız çerçeveyi değiştirir). C3'te 2. ve 3. kartın önizlemesi artık görünüyor (bilinçli fark).
- **Ekip seçimi:**
  - Köprü: bölgede ekip yokken `FieldVM.Send` hazır her ekip için bir fiş verir: haritadaki harf ve yol günü (“A · 2 gün”), kadro sırasıyla. En yakın ekip mürekkep fiş ve B kısayolu; ötekiler kâğıt fiş. Komut `Team:Send:<ekip>:<bölge>`; kuralı oyun uygular (`GameController.MoveTeam`). B tuşu eskisi gibi en yakın ekibi gönderir.
  - İttifak dışı bölgede ve hazır ekip yokken fiş yok; eski kapalı düğme nedeniyle görünür.
  - Dosya: saha bölümünde baskın düğmesinin yerinde “Gönder” etiketi ve dört fiş (`RegionDossier/Send`, `DeskButton_Small`, aralık 5). Dört fiş ve etiket 353/354 px.
  - Saha başlığının çizgisi sağdaki metne kadar uzanıyor (aralık 8, CSS ile aynı). “Bölgede ekip yok” eskiden çizginin üstüne biniyordu. C1'de çizgi değişmedi (231 px).
  - Yeni metin: “Gönder” → “Send”.
  - Eğitimdeki “Saha” hedefi fiş satırını da kapsıyor.
- **Sürükle bırak:**
  - Pencereler sekmeden serbestçe taşınır. Sütuna oturtma ve sütun kaydırma kalktı; hedef kartı katlanınca sol sütun artık kaymıyor. Görünen kısım ekranın içinde tutulur.
  - Harita (`Map` prefab kökünde `DeskDraggablePanel`) kâğıdından tutulup taşınır; kâğıt artık tıklama alıyor. Düğüme tıklamak çalışır, sürükleme eşiği aşılınca tıklama iptal olur. Seçim ipliği, olay spotu ve iplik düğümü izler.
  - Hedef kartı (`GoalCard` prefab kökünde `DeskDraggablePanel`) dolgusundan tutulup taşınır, taşınırken öne gelir.
  - Konumlar oturum boyunca kalır, kayda yazılmaz.
- **Yerleşim sahnede:**
  - `WindowManager` artık yerleşim yapmıyor. Pencerelerin konumu, boyutu ve açılıştaki açık/kapalı durumu sahnedeki hâlidir. Açılışta yalnız araştırma açık; dosya seçimle açılır.
  - İki sahnede eski sütun yerleri sahneye yazıldı: araştırma, haberler, rehber solda; dosya, ekipler, YZ günlüğü sağda.
  - Olay kartı: YZ günlüğü ve danışman notunun yeri, genişliği ve eğimi prefab'da; yükseklik içerikten. Uzayınca dönüşümsüz üst kenar yerinde kalır (CSS'teki gibi). VM'deki `Rect`/`RotDeg` artık kullanılmıyor.
  - Harita: analist notunun (sarı yapışkan not) yeri, genişliği ve eğimi prefab'da; yükseklik metinden. Önizleme ve Play eskiden notu veriyle yeniden konumluyordu; kullanıcının taşıdığı not bu yüzden geri dönüyordu.
  - Spot, kalem halkası ve ipliğin bölge ucu, haritadaki düğümün o anki yerinden okunur (`MapView.MapToWorld`). Harita taşınsa ya da ölçeklense de düğümde kalır; spot deliği harita ölçeğiyle büyür ya da küçülür.
  - İpliğin bildirim ucu bildirim kâğıdının raptiyesidir (kâğıdın sol-üstünden `memoPinOffset` 690, 22).
  - Kâğıtlar 8 px düşerken halka ve iplik her karede yeniden yerleşir. Eskiden düşüşün ortasında bağlandıkları için haritadaki uçları 8 px kayık kalabiliyordu.
  - “Bölge kâğıtların altında mı” denetimi köprüdeki sabit tasarım dikdörtgenlerinden görünüme taşındı. Gerçek kâğıt ve düğüm yerlerine bakılır; altındaysa halka ve iplik gizlenir, spot kalır.
  - Veri olarak kalanlar (yerleşim değil): haritanın coğrafyası (düğüm, etiket, yol, ekip pulu ve analist çizimleri harita koordinatından), liste satırları (araştırma, teleks, uyarı fişleri), metne göre genişleyen düğme ve satırlar, gösterge ibreleri, hedef kartının katlanma yüksekliği.
- **Bulunan eski hata:** eğitim katmanının dört karartma paneli sprite'sızdı (§12; F8.4'te yalnız oyun sahnesi denetlendiği için görünmemişti). Düz dolgu sprite'ı `sh_fill_r0` verildi; görünüm aynı.
- **Sahneler:** `DeskGame` ve `DeskUI_Sandbox` diskten açılıp değiştirildi ve kaydedildi (pencere yerleri ve durumları, olay kartının harita bağı, eğitim hedefi). Böylece önizlemenin yazdığı değerler sahneye geçmedi.
  - DeskGame'de kaydedilmemiş durum yalnız önizleme değerleri ve net sıfır aç/kapalarıydı.
  - Kullanıcının taşıdığı not, önizleme yeniden bağlanınca zaten geri dönmüştü (bu isteğin konusu olan hata). Artık yerinde kalır.
- **Kalite:**
  - EditMode 109/109. Yeni testler: `Layout_IsAuthoredInScenes`, `Dossier_SendChipsReplaceRaid`; köprü testine ekip fişleri eklendi.
  - Edit modda C1, C2, C4 önceki önizlemeyle piksel aynı; C3 yalnız 2. ve 3. kartın önizlemesinde farklı.
  - Metin denetimi: sandbox TR+EN 0 hata, 0 uyarı; oyunda fişler açıkken (Çin, ekip yok) TR+EN 0 hata, 0 uyarı.
  - Yakalamalar: `F9_oyun_ekip_fisleri`, `F9_onizleme_C3_hep_acik`, `F9_onizleme_C3_harita_kaydirildi` (harita 260 sola, 90 aşağı: spot düğümde, bölge bildirim kâğıdının altında kaldığı için halka ve iplik gizli).
  - Play denemesi: Bravo fişi Bravo'yu gönderdi (“Bravo ekibi yola çıktı: Çin, 2 gün.”) ve fiş listeden düştü. Harita, hedef kartı ve araştırma penceresi taşındı ve yeni bağlamadan sonra yerinde kaldı; pencere ekran dışına itilince kenarda durdu.
- **Kullanıcı verisi:** kayıt, profil ve tercihler denemeden önce yedeklendi. İlk oyun denemesi Play'den çıkarken kaydı yazdı; kayıt yedekten geri yüklendi (aynı özet). Tercihlerde yalnız Unity'nin oturum sayaçları değişti.

### Play'e basınca masanın yeniden kurulması (kullanıcı bildirimi, 2026-10-13)

- **Bildirim:** Play'e basınca sahnedeki prefab'lar kaldırılıp yeniden konuyor, yeniden düzenleniyor gibi.
- **Ölçüm:** sahnedeki bütün RectTransform'lar (çapa, pivot, konum, boyut, ölçek, dönüş, açık/kapalı) edit modda ve Play'de karşılaştırıldı.
  - Panel düzeyinde (pencereler, harita, hedef kartı, göstergeler, bant) hiçbir şey değişmiyordu.
  - Değişenler edit önizlemesinin sahte verisinden geliyordu. Önizleme tasarımın haritasını (map.json: Doğu Asya, Batı Avrupa…) ve C1 değerlerini gösteriyordu; Play oyunun kendi bölgelerini (Çin, Hindistan…) ve projeksiyonunu kurduğu için harita düğümleri, etiketler, yollar ve ekip pulları başka yerlere geçiyordu. Seçili bölge olmadığından dosya kapanıyor, listeler değişiyordu.
  - Önizlemenin dinamik liste kopyaları (yaklaşık 1100 nesne: düğümler, satırlar, teleks) sahneye kaydedilmez. Play'de kaybolup oyunun verisiyle yeniden kuruluyorlardı (Hierarchy'de “kaldırılıp yeniden konma”). Bu kopyalardan biri elle taşınırsa Play'de kayboluyordu.
- **Düzeltme:**
  - Edit önizlemesinin varsayılanı “oyun verisi”: oyun sahnesinde oyunun açılış masası. Oyuncunun ayarlarıyla (rakip, deneysel mod, olaylar, öğrenme aşaması, kampanya) yeni oyun, sabit tohum (`GameController.PreviewOpeningGame`, `SimDeskDataSource.BuildPreview`, `IDeskPreviewSource`). Tercih, kayıt, profil ve kampanya dosyasına yazmaz, olay yayınlamaz.
  - Menüde: oyun verisi (açılış masası / giriş kâğıdıyla) ve sahte C1–C4. Sandbox'ta oyun verisi yok, sahte C1.
  - Önizleme kopyaları düzenlenemez (`HideFlags.NotEditable`; Inspector gri, sahnede taşınamaz). Kalıcı düzenleme şablonda ve prefab'da.
  - `GameController`: Awake'teki ayar okuma `ReadSettings`'e, oyun kurma `CreateSim`'e ayrıldı (davranış aynı).
- **Sonuç:** edit → Play arasında yerleşim farkı 7 nesne: güven göstergesinin ibresi 0,9° (Play rastgele tohumla başlar) ve edit modda kapalı olan giriş kâğıdının içi. Harita düğümleri, etiketleri, yolları ve ekip pulları aynı yerde. Giriş kapatılınca bütün ekran %0,01 farklı (yalnız o gösterge).
  - Hierarchy'de liste kopyaları Play'de yine yeniden kurulur (veri). Artık aynı yere ve aynı içerikle kurulur.
- **Kalite:** EditMode 111/111. Yeni testler `EditPreview_UsesOpeningGameWithoutWriting` (oyunun bölgeleri ve yerleri, giriş ve dosya kapalı; tercih ve kayıt yazılmıyor) ve `MarkPreview_CopiesAreUnsavedAndLocked`. Sahte C1, C2, C4 önizlemesi önceki yakalamayla piksel aynı; C3'te yalnız bilinen önizleme farkı.
- **Kullanıcı verisi:** kullanıcının yeni kaydı (07:48) denemeden önce yedeklendi (savebak5). Play denemeleri kayda ve profile yazmadı; tercihlerde yalnız Unity oturum sayaçları değişti.

### Eğitim karartmasında dikişler ve bekleme adımı (kullanıcı bildirimi, 2026-10-13)

- **Bildirim:** eğitimin bazı adımlarında karartmada açık renkli kesikler var. “İz sür” adımı YZ ortaya çıkana kadar ekranda kalıyor; bir onay düğmesi olsun, basınca gitsin, YZ ortaya çıkınca eğitim geri gelsin.
- **Dikişlerin nedeni:** önceki turda kural denetimi için karartma parçalarına (`DimTop/Bottom/Left/Right`) atlasın 16×16 düz dolgusu `sh_fill_r0` verilmişti (Image, Simple). Küçük sprite onlarca kat gerilince çift doğrusal süzme kenar piksellerini atlasın saydam dolgusuyla karıştırıyor ve yaklaşık 60 px'lik açık bantlara yayıyordu. Parçaların birleştiği yerlerde ve ekran kenarında kesikler görünüyordu. Bu hatayı o tur ben getirmiştim (F8.4'teki sprite'sız hâli düzdü).
- **Düzeltme:** `DeskSolidGraphic`, büyük alanlar için düz renk grafiği. Olay karartması (`SpotlightGraphic`) gibi atlas beyazının ortasından tek UV ile çizer; aynı batch'te kalır, sprite'sız Image kuralına da uyar. Dört karartma parçası bu grafiğe geçti; renk ve tema anahtarı (`modal.footerBg`, 0,92) aynı. `SpotlightGraphic` aynı UV yardımcısını kullanıyor.
  - Ölçüm (Play, 2. adım): karartılmış her pikselde parlaklık oranı 0,31–0,32. Sapan satır ve sütunlar yalnız sarı notun kendi gölgesi. 4. adım (bildirimdeki ekran) kesiksiz.
- **Bekleme adımı:** oyuncunun elinde olmayan bir şeyi bekleyen adımlarda “Anladım” düğmesi var (Enter da çalışır): 8 “İz sür” (YZ tespit edilene kadar), 10 “Gözlem noktası”.
  - Basınca not ve karartma kalkar, zaman akar. Koşul sağlanınca sıradaki adım her zamanki gibi gelir.
  - Oyuncu eylemi bekleyen adımlarda (bölge seç, ekip gönder, zamanı başlat, izleme ağı kur, hizalamayı başlat, denetim) not ne yapılacağını gösterdiği için düğme yok.
  - Yürütücü (`DeskTutorialRunner.Hide`, komut `Tutorial:Hide`) gizlenen adımı hatırlar; Baştan, yeni oyun ve sonraki adım bunu sıfırlar. Eski oyunun eğitimine dokunulmadı.
- **Metin:** 4. adım (“Ekibini gönder”) yeni ekip seçimine göre yazıldı: “SAHA bölümünde hazır her ekibin fişi var: göndereceğin ekibe bas (B en yakınını gönderir)”. EN karşılığı da güncellendi.
- **Doğrulama (Play):** 8. adımda “Anladım” → not ve karartma kalktı, eğitim zamanı tutmuyor. YZ tespit edilince 9. adım (“Çareyi başlat”) geldi.
  - Yeni test: `Tutorial_GameWaitingStepsCanBeHidden` (hangi adımlar gizlenebilir; karartma parçaları düz grafik, Image yok).
  - Test takımı bu turda çalıştırılmadı: kullanıcının açık DeskGame sahnesinde kaydedilmemiş düzenlemeleri var (taşınan not, yapıştırılan RectTransform değerleri). Testler sahneyi açıp kapattığı için sahne kaydedildikten sonra çalıştırılacak. Aynı denetimler editörde elle doğrulandı.
- **Kullanıcı verisi:** kullanıcının 09:35 kaydı ve tercihleri denemeden önce yedeklendi (savebak6). Deneme kayda, profile ve eğitim tercihlerine yazmadı.
