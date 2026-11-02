# Geliştirme

## Ortam

- Unity **6000.3.12f1** (proje: bu deponun kökü). Editor açık olmalı; ajanlar onu `unity` CLI + Pipeline paketiyle
  (`com.unity.pipeline`) sürer. Editor derleme hatasıyla Safe Mode'a düşerse CLI bağlanamaz: hatayı dosyadan düzelt.
- Yardımcılar: `source Tools/unity-helpers.sh` → `ueval`, `ucompile`, `utest`, `uplay`, `ustop`, `ushot`, `uerrors`.
  Yardımcılar `--project-path` ile Corrigible'a bağlanır; başka bir Editor açıkken doğrudan CLI çağrılarında da
  `--project-path /Users/selimkamyan/Gits/Corrigible` belirt. Çalışma dizini tek başına doğru Editor'ü seçmeyebilir.

## Döngü

1. Kodu değiştir.
2. `ucompile` → "HATA YOK" görmeden devam etme. (`recompile_status` hata bildirmez; konsolu zaman damgasıyla oku.
   Konsolda eski derlemelerden kalma hatalar durur; yalnızca son derlemeden sonrakilere bak.)
3. Testler: önce **boş sahne aç** (aşağıda "Testler açık sahneyi kirletir"), sonra `utest`. Domain reload sırasında
   `run_tests` zaman aşımına uğrar; `utest` yeniden dener. Tam takım birkaç dakika sürer; doğrudan CLI'da
   `--timeout 900` ver ve arka planda çalıştır.
4. Oyunda dene (aşağıda). Görsel değişiklikte ekran görüntüsünü gerçekten aç ve bak.
5. Play'deysen `ustop`. Kayıt dosyalarını geri yükle, geçici görüntüleri sil (`Temp/*.png`). Dinamik font asset'lerindeki
   (`* Dyn SDF.asset`) oyun sırasında oluşan değişiklikleri `git checkout` ile geri al. Commit'i kullanıcı isterse yap.

```bash
ueval 'UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single); return "boş";'
```

## Testler

| Dosya | Ne kilitler |
|---|---|
| `Tests/EditMode/LiberationCampaignTests` | açılış, sektör sırası, ödüller, sınır seçimi, bütün harekâtın kazanılabilmesi, makinelerin kopyalanması, sivil güven (kayıp, ayakta kalan sivil ödülü, tavan), kayıt/devam |
| `Tests/EditMode/DoorTests` | sığınağın kapısı: ilk vuruş, aç/açma ve seçim, cevapsız ziyaretçi, vuruş aralığı ve ana RNG'ye dokunmama, kopya ve silahlı sakinin koşulları, bedeller, kart tablosunun bütünlüğü, kayıttan devam |
| `Tests/EditMode/ShelterTests` | sığınak tüketimi, plaka dökümü, revir, yedekler, telsiz, depo sınırları |
| `Tests/EditMode/RaidEngineTests` | mulberry32, aynı hamlelerle aynı savaş (beklenen imzalar, bütün ekip kaçışla çıkar), geri alma, önizleme, ikmal görevi, kaçış (son çıkan kazandırır, süre dolarken kalan düşmüş sayılır, sunucu kaçışı, geri çekilme yok), sunucu sertliği (düğüm canı, ek makine; kasa ve çekirdek hariç) |
| `Tests/EditMode/RaidMapsTests` | harekâtın harita havuzu: aynı tohum aynı havuz, harekâtlar harita paylaşmaz, üretilen haritaların kuralları, altı türden eşit sayıda ve her harita türünün kurallarına uyar, torba (her harita bir kez, art arda tekrar yok), havuz yalnız `MapSeed` ile |
| `Desk/Tests/EditMode/SiginakDoorTests` | kapı kâğıdının evreleri (bak, sor, aç, açma; bedel ve gerekçe; sonuç ve damga), son raporun bekletilmesi, vardiya raporunun kapı satırı, sahne bağlantıları ve kapı pozu |
| `Desk/Tests/EditMode/TutorialFlowTests` | eğitim adımlarının sırası, geçilen adımlar, raporlar, atlama, sahne bağlantıları |
| `Desk/Tests/EditMode/TutorialRaidTests` | senaryolu ilk baskın: her harekât tohumunda aynı tahta, rehberin adımları hasarsız zafere varır |
| `Tests/EditMode/RegionIdentityTests` | bölge özellikleri ve imza makineleri, zorluk kademesi, yeni makinelerin niyetleri, oda seviyesi kilidi |
| `Tests/EditMode/LocalizationTests` | çeviri tablosu tam; İngilizce oynanan harekâtta Türkçe metin yok; dil sonucu değiştirmez |
| `Desk/Tests/EditMode/DeskBridgeTests` | DeskGame bağlantıları, ekran katmanlarının sırası, operasyon raporunun yeri ve güven satırı, harekât sonu raporu (damga, gösterge, 29 ve 48 vardiyada tam genişlik şerit, kırılma anları; dil değişince de) |
| `Desk/Tests/EditMode/LiberationReportTests` | operasyon raporu (S7): KAZANÇ ve fişler, başarısızda ödül alanı yok, aynı söz iki kez geçmez, depo sınırı, kayıttan aynı rapor |
| `Desk/Tests/EditMode/Siginak*Tests` | oda, kamera, harita, sığınak, CRT, vardiya, emir belgesi, punto taraması (oda + baskın) |
| `Desk/Tests/EditMode/SadeRaid*Tests`, `LootRaidPresentationTests` | baskın HUD komutları, kamera izdüşümü, Baskin sahnesi bağlantıları |
| `Desk/Tests/EditMode/DeskAssetTests`, `CommandPostProjectionTests` | sprite/font/tema varlıkları, dünya-ekran izdüşümü |

## Oyunu ajan olarak test etmek

Editor arka plandayken Game görünümü güncellenmez; ekran görüntüsü `CorrigibleDebug.Capture` ile alınır (Overlay
canvas'ları geçici olarak kameraya bağlar; odanın dünya uzayı canvas'ları yerinde kalır).

Oyunun akışı bir eval'in içinde coroutine olarak sürülür (GameController'da `StartCoroutine`); her adımdan sonra
bekle, durumu bir `Temp/*.txt` dosyasına yaz, ekran görüntüsü al. Sık kullanılan tutamaklar:

```csharp
var game = Object.FindFirstObjectByType<Corrigible.Game.GameController>();
var surface = Object.FindFirstObjectByType<Corrigible.Desk.Bridge.LiberationDeskView>();
// Emir belgesi: BAŞLA iki basış ister (kayıt uyarısı + onay). Düğme BriefingView'ın özel "action" alanında.
// Vardiya: ShiftSwitch.Press() iki kez (kapak + kol); SiginakShift.Skip() diziyi atlar; rapor özel File() ile tepsiye.
// Kapı kâğıdı açıkken masa kilitli (V031'de ziyaretçi var): DoorCardView'ın özel hintButton'ı (alt satır) kâğıdı kapatır.
surface.LaunchSector(game.Sim.NextLiberationSector(game.Sim.S.Liberation.CurrentRegion).Id);   // baskın emri
// Baskın: RaidLauncher.IsRaidLoaded; FindFirstObjectByType<RaidDataSource>() → Select, Move, Arm, Use, EndTurn,
// Retreat() iki kez, Continue() rapora geçer (sonuç kartı 0,6 s girdi kilitli). Dönüşte SiginakOperation.ReportOpen;
// rapor özel File() ile tepsiye. FinalReportView.Instance → IsOpen, PressRestart().
```

- Belirli bir oda durumuna hızlı gitmek için `Corrigible.Desk.EditorTools.SiginakFixtures.Load("V014")` gibi hazır
  durumlar var (bkz. `SiginakFixtures.cs`). Durumu elle değiştirmek (`S.Fon = 200`) testi hızlandırır ama kayda girmez;
  kayıt/devam testinde kullanma.
- Harekâtın sonunu görmek için komutları doğrudan `Simulation` üstünden çalıştırıp (`StartLiberationRaid` +
  `ResolveRaid(new RaidResult { Success = true })`) sonra `game.Flush()` ve `SimDeskDataSource.MarkDirty()` çağır;
  `GameController` üstünden çalıştırırsan `RaidLauncher` her baskında sahneyi yüklemeye kalkar.
- `Scenes/Baskin.unity`'yi tek başına açıp Play'e basmak örnek bir sunucu baskını kurar (tahtayı hızlı denemek için).
  Hamleler ve makineler evresi canlandırılırken (`Busy`) girdi yok sayılır; ekran görüntüsünden önce bekle.
- Test oyunları **gerçek kayıt dosyalarını ve PlayerPrefs'i değiştirir**. Play öncesi `persistentDataPath` altındaki
  `liberation_savegame.json`, `liberation_desk.json` ve `corrigible.*` tercihlerini yedekle; sonunda geri yükle, önceden
  olmayan dosyaları sil. Harekât biterse kayıt silinir. Geri yükleyemediğin değişiklik varsa kullanıcıya söyle.
- Play sırasında script derlemek statik tekil örnekleri sıfırlayabilir. Script değişikliklerini Edit modunda yap.
- eval kodu tek tırnak içinde gönderilir; C# karakter sabiti (`' '`) kullanma, `"".PadLeft(n)` gibi dizgi kullan. Uzun
  kodu bir dosyaya yazıp `ueval "$(cat dosya.cs)"` ile gönder.
- Uzun `eval` çağrılarında **iki ayrı zaman aşımı** var: ikinci konumsal argüman sunucudaki C# çalışma süresi
  (milisaniye), global `--timeout` ise CLI'ın yanıtı bekleme süresi (saniye). `ueval` 5 sn'de "zaman aşımı" dönse de
  kod çoğunlukla tamamlanır; sonucu dosyadan oku.

  ```bash
  unity command eval 'return Corrigible.EditorTools.CorrigibleDebug.Capture("Temp/shot.png");' 60000 --timeout 90 --caller plugin --skill unity-cli
  ```

## Sığınak görünümünü doğrulamak (görüntü seti, punto, kare süresi)

- **Tam görüntü seti** (Revizyon 2 referanslarının karşılıkları): DeskGame Play'de
  `ueval 'return Corrigible.Desk.EditorTools.SiginakCaptureSet.Start("tr");'` (ya da `"en"`); Baskin tek başına Play'de
  `SiginakCaptureSet.StartRaid("tr")`. İlerleme `Temp/capture_set.txt` (son satır `bitti`); çıktı
  `Docs/UI_Siginak_Rev2/screens_unity_rev2/S*.png`, `_EN`, TR koşusunun sonunda `_gray`. Fikstürler oyunun kaydını değiştirir:
  önce kayıtları ve dinamik font asset'lerini yedekle, sonra geri yükle.
- **Punto taraması:** Edit modda `ueval 'return Corrigible.Desk.EditorTools.SiginakTextScan.Run();'` (DeskGame ve Baskin'i ek
  olarak açar; rapor `Docs/UI_Siginak_Rev2/punto_taramasi.txt`). 14 px altı her yeni metin ya düzeltilir ya da
  `SiginakTextScan.Exceptions`'a gerekçesiyle girer; `SiginakTextScanTests` ikisini de denetler.
- **Kare süresi:** Play'de aynı oyun durumunda (`SiginakFixtures.Load("V014")`) kamerayı 1920×1080 MSAA 4× bir hedefe 30 kez
  çiz, `ReadPixels` ile GPU'yu bekle, tur başına ms/kare al. Eski bir commit'le karşılaştırmak için kullanıcının editörüne
  dokunma: `git archive <commit>` ile geçici klasöre çıkar, `Library`'yi `cp -cR` ile kopyala (APFS klonu, yer tutmaz),
  kopyanın `ProjectSettings.asset`'inde `productName`'i değiştir (Play kayıtları gerçek kayıtların üstüne yazılmasın), ölçüm
  script'ini yalnız kopyaya koy ve `Unity -batchmode -projectPath <kopya> -executeMethod …` ile koştur (Play'e girmek için
  `EditorApplication.update` üstünde adım adım ilerleyen, `SessionState`'te adımı tutan bir `[InitializeOnLoad]` sınıf). Turlar
  arası kayma ±0,1 ms olduğu için iki kopyayı art arda, birkaç kez sırayla koştur ve çiftlerin farkına bak.
- Sahnede bir nesneyi statik işaretlemeden önce: çalışırken taşınmıyor, ölçeklenmiyor ve TMP değil mi bak (statik birleştirme
  dönüşü ve TMP'nin yeniden kurulan mesh'ini dondurur; renk ve malzeme değişimi sorun değil).

## Baskın sunumunu düzenlemek

Görsel kaynak `Docs/SonSalter_Baskin_Paketi`: PNG'ler görünüşü, `reference/measure` TSV'leri sayısal yerleşimi,
`spec/raid_tokens.json` kamera/malzeme/işaret değerlerini anlatır (oyundaki kopyası `Desk/Data/Sade/raid_tokens.json` →
`Desk/Resources/RaidTheme`). Geçerli hamle, hasar ve niyet hesabı `Core/Baskin.cs`'ten gelir; görseli uydurmak için Core
kuralı değiştirme.

- Kalıcı sahne `Scenes/Baskin.unity`; HUD ve paneller `Desk/Prefabs/Sade/Raid` prefab'larında. Referanslar
  `[SerializeField]` ile bağlıdır.
- Parçalar `Prefabs/Raid` altında; var olan parçayı güncellerken prefab'ı yerinde kaydet, GUID'ini ve `RaidPiece`
  referanslarını koru. Parça/palet bağlantıları `Settings/RaidPieceLibrary.asset`; dokular, mesh'ler ve malzemeler
  `Art/Raid`. URP malzemeleri mat; emission ve kamera post-processing kapalı.
- **Izgara ve kamera:** bir karo bir dünya birimi, merkezler `RaidBoard3D.W(c, r) = (c − 3,5, TileTop, 3,5 − r)`. Kamera
  ortografik, rotasyon `(35°, 45°, 0)`. `SadeRaidCameraFraming` ortografik boyutu ekran yüksekliği, canvas ölçeği ve
  `raid.camera.pixelsPerUnitAt1080` tema değerinden hesaplar; izdüşüm `SadeRaidBoardTests.Camera_ProjectsGridAndHeightToReferencePixels`
  ile kilitli. Kadrajı ya da karo ölçeğini elle sabitleme.
- Kontrol: iki dilde gerçek baskın aç; erişilebilir kareleri, EMP önizlemesini, makinelerin sıra vurgusunu ve girdi
  kilidini, geri çekilme onayını ve sonuçtan rapora dönüşü dene. 1920×1080'e ek olarak 16:10 ve ultra geniş kadrajı gör.

## Sık yapılan işler

**Denge sayısı değiştirmek:** `Balance.cs` alanı (testler varsayılanları kullanır) ya da yalnızca oyunda denemek için
`Settings/BalanceConfig.asset`. Sonra `LiberationCampaignTests`/`ShelterTests`'i çalıştır; ödül ve gelir beklentileri
`Balance` alanlarından okunur. RNG sırasını ya da var olan komutun sonucunu değiştiriyorsan `GameRecord.CurrentVersion`'ı
artır (AGENTS kural 1).

**Oyuncu eylemi eklemek:** bkz. MIMARI.md → "Determinizm ve kayıt". Odada eylemi `LiberationDeskView`'a gerekçeli bir
komut olarak ekle (`TelexReasons`'a yapılamama gerekçesi), bağlayıcı onu çağırır.

**Odaya öğe eklemek / sahneyi koddan değiştirmek:** önce boş sahneye geç, sahneyi diskten yeniden aç
(`EditorSceneManager.OpenScene`), objeleri `Transform.Find` (pasifleri de bulur) ile bul, alanları `SerializedObject` ile
bağla, prefab örneğinde değiştirdiğin bileşenler için `PrefabUtility.RecordPrefabInstancePropertyModifications` çağır,
`MarkSceneDirty` + `SaveScene`. Bunu tek seferlik bir eval ya da editör script'iyle yap, sonucu doğrula, script'i sil.
`git diff`'te yalnız beklediğin satırlar olmalı. Prefab'ı `PrefabUtility.LoadPrefabContents` ile aç,
`SaveAsPrefabAsset` ile kaydet, `UnloadPrefabContents` ile bırak (bıraktıktan sonra içindeki nesnelere dokunma).

## Lokalizasyon iş akışı

1. Kodda oyuncuya görünen metni Türkçe yaz ve sar: `Loc.T("Metin.")`, `Loc.F("{0} bölgesinde {1} gün.", ad, gün)`.
   Sayı biçimini yer tutucuda ver (`{0:0.0}`); `ToString` + birleştirme yapma. Türkçe ek uyumu gerekiyorsa `TrNumber` ya
   da `Loc.IsSource` ile Türkçeye özel dal yaz. Küçük harfe çevirirken kültürü dile göre seç (Türkçe `I → ı`).
2. Sahneye/prefab'a yeni sabit yazı eklediysen TMP nesnesine `DeskLocText` bileşeni ekle. Odadaki tıklanır nesnenin
   üzerine gelme etiketi (`DeskTarget` başlık ve satır) tabloya kendiliğinden girer.
3. **Corrigible > Lokalizasyon > Çeviri tablosunu güncelle**, sonra `Assets/Corrigible/Resources/Localization/strings.txt`'te
   boş kalan `en` hücrelerini doldur (satırın sonundaki sekmeden sonra). `{0}` ve `<b>` gibi işaretler aynen kalmalı.
4. **Çevirileri doğrula** menüsü ya da `LocalizationTests` → eksik/uyumsuz çeviri listesi.
5. Oyunda dene: ayarlar kartındaki dil seçimi ya da `PlayerPrefs.SetString("corrigible.language", "en")` + Play.

Ajan olarak: `ueval 'return Corrigible.EditorTools.LocalizationTool.UpdateTable();'` ve
`ueval 'return string.Join("\n", Corrigible.EditorTools.LocalizationTool.Validate());'`.

## Bilinen tuzaklar (bu projede yaşandı)

- **TAA ve ince geometri:** URP TAA her karede alt-piksel jitter uygular; ekranda ~2 px'ten ince nesne TAA açıkken titrer
  (masa pozunda 2 m uzakta 1 px ≈ 2 mm). Odaya ince nesne (ip, kablo, çubuk) eklerken ekranda en az ~3 px yap (masa pozunda
  ≥ 6 mm). Kameranın TAA ayarı DeskGame'deki `Main Camera`'da (Very High, temel karışım 0,95, kıskaç 1,25); URP
  varsayılanına dönerse bütün kenarlar titrer. TAA'yı ölçerken aynı karede `cam.Render()`'ı ikinci kez çağırma: URP o
  karede geçmişi zaten güncellediği için bir kez daha karıştırır ve oyuncunun görmediği bir kare çıkar. Kamerayı bir
  RenderTexture'a bağla, motorun kendi çizimini `EditorApplication.update`'te `Time.frameCount` değişince oku.
  `CorrigibleDebug.Capture` tek kare çizdiği için titremeyi göstermez.
- **Baskın testinde kaçışı elle kurmak:** taşıyıcıyı elle yazan test (`state.Carrier = …`) `state.Escape = true`'yu da
  yazmalı; kaçış bayrağı olmadan çıkış tahliye etmez, süre dolunca kaçış öncesi gibi kaybedilir. Kaçışta tahliye edilen
  uzman `Units`'te kalır ama `UnitAt`/`Alive` onu görmez; birim aramalarında `!u.Dead` yerine `Baskin.OnBoard(u)` kullan.
  Geri alma birimleri kopyasıyla değiştirir: `Undo`'dan sonra eski `Unit` başvurusunu yeniden bul.
- **Baskın botu puanı sıçramamalı:** botun puanında bir aşamaya (kaçış) geçerken yeni bir terim birden eklenirse bot o
  hamleyi hiç yapmaz (kaçışta öteki uzmanların yolu yalnız kaçışta sayılınca ikmal kasası açılmadı, başarı %93 → %41). Her
  terim iki aşamada da aynı ağırlıkla sayılmalı.
- **Layout grubunun çocuğu:** `HorizontalLayoutGroup` altındaki nesnenin konumunu `anchoredPosition` ile değiştirmek çalışırken
  ezilir; boşluğu grubun `padding`'iyle ver. `KeyChipView` kendi `fontSize`/`height` alanını etikete uygular; puntoyu orada değiştir.

- **Testler açık sahneyi kirletir:** sahne testleri (`DeskGameScene_*`, `Siginak*Tests`) sahneyi `Additive` açar; sahne
  editörde zaten açıksa aynı bellekteki sahneyi kullanıp nesnelerini değiştirir ve Test Runner kirli sahneyi kaydedebilir.
  Testleri boş sahne açıkken çalıştır; sahneyi koddan değiştireceksen diskten yeniden aç.
- **Play açıkken `run_tests` komut sunucusunu kilitler.** Kullanıcı oyunu editörde oynayıp Play'de bıraktıysa boş sahne
  açılamaz (`NewScene` Play'de çalışmaz), Test Runner sahneyi kaydedemeyip takılır; ardından `eval`, `editor_stop`,
  `recompile` hepsi 30 sn'de zaman aşımına uğrar ve yalnız Unity'yi yeniden açmak çözer. Testten önce
  `EditorApplication.isPlaying`'i denetle; Play'deyse kullanıcıya sor.
- **Büyük sahne düzenlemesi Editor'ü çökertebilir.** Çöküşten sonra Editor açılışta bir iletişim kutusunda bekler (CPU
  %0, `Editor.log` lisanstan sonra ilerlemez): çoğunlukla `Temp/__Backupscenes`'teki yarım sahneyi geri yükleme sorusu.
  Geri yükleme; Editor'ü kapat, `Temp/__Backupscenes` ve eski `Temp/UnityLockfile`'ı kenara taşı, yeniden aç.
- **Test assembly'sindeki MonoBehaviour** (`includePlatforms: Editor`) `AddComponent` ile eklenemez (null döner). Testte
  bileşen gerekiyorsa çalışma zamanı assembly'sindekini kullan (ör. `LiveRaidDataSource.OpenForValidation`).
- **Dosya silmeden önce içindeki bütün tipleri ara:** bir dosya adıyla aranan sınıf kullanılmıyor görünse de aynı dosyadaki
  başka tipler kullanılıyor olabilir.
- **Lokalizasyon:** statik `readonly` alanda `Loc.T` kullanma (dil, sınıf ilk yüklendiğinde donar; `Loc.N` + gösterimde
  `Loc.T`). Metne bakarak karar verme. Testte `RegexOptions.IgnoreCase` Türkçe `ı`'yı `I` ile eşleştirir.
- **Enum'lar asset'te sayı olarak saklanır:** `Cue`/`LoopCue` üyelerinin sayıları sabittir; araya değer ekleme, silinen
  sesin numarasını yeniden kullanma.
- **Image layout'u bozar:** sliced sprite'lı `Image` bir `ILayoutElement`'tir ve sprite kenarlarını minimum boyut olarak
  bildirir. Zemin Image'ı ayrı bir `Background` çocuğuna koy ve `ignoreLayout` yap.
- **Belirip kalkan nesneye `Vector3.one` yazma:** sahnede doğrudan ölçeklenmiş bir mesh (ör. maketin plakası, 0,006 × 0,07 ×
  0,05) 1 m'lik küp olur. Sahnedeki ölçeği Awake'te sakla ve ona göre canlandır (`ShelterStore.rest`; testli).
- **DeskGame klavye tuşu yazmaz:** yeni kâğıda ya da düğmeye “Enter”, “Esc”, tuş fişi koyma; kısayol çalışır ama her
  birinin tıklanan karşılığı olmalı (`SiginakDeskTests.DeskGameScene_ShowsNoKeyboardHints` yakalar). Baskın tahtası hariç.
- **Bir dosyada iki MonoBehaviour:** sahne kaydedilince ikincisi "Missing Script" olur. Dosya adı = sınıf adı.
- **`childForceExpandHeight`:** HorizontalLayoutGroup'ta açıkken grup esnek yükseklik bildirir; satıra
  `LayoutElement.flexibleHeight = 0` ver.
- **TMP harf aralığı:** `characterSpacing`, CSS piksel değerinin yaklaşık `px / fontSize × 50` karşılığıdır (100 değil).
- **Statik font atlası boşluğu içermeli;** yoksa her boşluklu metin dinamik yedekten ayrı draw call açar (testli).
- **`capture_game_view` / Editor ekran görüntüsü** arka planda güvenilmez; `CorrigibleDebug.Capture` kullan. Proje dışına
  (ör. /tmp) yazamaz; `Temp/` altına yaz.
- **Edit modda sahne kurulumu:** `EditorSceneManager.NewScene` kullanılmayan asset'leri boşaltır; asset referanslarını
  sahne oluşturulduktan sonra yükle.
- **Dönük nesnenin çarpıştırıcısı:** `Renderer.bounds` dünya eksenlidir; 35° dönük bir nesnede derinliği de kapsar. Kutu
  çarpıştırıcıyı nesnenin kendi ekseninde, mesh sınırlarından hesapla (`SiginakRoomTests.RoomTargets_CollidersFitTheirModels`).
- **Kapalı kameranın ışını:** `PhysicsRaycaster` kameradan ayrı bir bileşendir; kamera kapalıyken de ışın atar. Baskında
  odanın ışını açık kalırsa tahtanın parçaları "arayüz" sayılır ve karelere tıklanamaz; `RaidLauncher.hideDuringRaid`'de
  durur (testli). Tıklama yolunu doğrularken `RaidDataSource` metotlarını doğrudan çağırma: fareyi
  `InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = cam.WorldToScreenPoint(RaidBoard3D.W(x, y)), buttons = 1 })`
  ile sür.
- **Tahtada tıklama yükseklik ister:** karonun çarpıştırıcısı görünen gövdesine uzanmalı (duvar bloğu 0,55; testli), yoksa
  tepesine tıklayan arkadaki kareyi seçer. Kareyi seçen kural `RaidBoard3D.Pick` (vurgusuz duvar arkasındaki vurgulu
  kareyi bırakır, eğitim çerçevesi ışın üstündeyse kazanır). Tıklamayı doğrularken karo merkezine değil oyuncunun
  tıklayacağı yere (gövde, blok tepesi) tıkla.
- **Vardiya kaydı vardiya sonunda alınır:** işgal geçmişi (`S.Hist`) yalnız `Step`'te yazılır; vardiya içinde değişen
  değeri gösteren sunum (harekât sonu raporunun şeridi) güncel değeri ayrıca ekler.
