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
| `Tests/EditMode/LiberationCampaignTests` | açılış, sektör sırası, ödüller, sınır seçimi, bütün harekâtın kazanılabilmesi, makinelerin kopyalanması, kayıt/devam |
| `Tests/EditMode/ShelterTests` | sığınak üretimi, tüketimi, enerji, revir, depo sınırları |
| `Tests/EditMode/RaidEngineTests` | mulberry32, aynı hamlelerle aynı savaş (beklenen imzalar), geri alma, önizleme, ikmal görevi |
| `Tests/EditMode/LocalizationTests` | çeviri tablosu tam; İngilizce oynanan harekâtta Türkçe metin yok; dil sonucu değiştirmez |
| `Desk/Tests/EditMode/DeskBridgeTests` | DeskGame bağlantıları, ekran katmanlarının sırası, baskın raporu ve son raporun kurulması |
| `Desk/Tests/EditMode/LiberationReportTests` | operasyon raporunun satırları, depo sınırı, kayıttan aynı rapor |
| `Desk/Tests/EditMode/Siginak*Tests` | oda, kamera, harita, sığınak, CRT, vardiya, emir belgesi, punto taraması |
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
surface.LaunchSector(game.Sim.NextLiberationSector(game.Sim.S.Liberation.CurrentRegion).Id);   // baskın emri
// Baskın: RaidLauncher.IsRaidLoaded; FindFirstObjectByType<RaidDataSource>() → Select, Move, Arm, Use, EndTurn,
// Retreat() iki kez, Continue() rapora geçer. EventModalView.Instance / GameOverView.Instance → IsOpen.
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
2. Sahneye/prefab'a yeni sabit yazı eklediysen TMP nesnesine `DeskLocText` bileşeni ekle.
3. **Corrigible > Lokalizasyon > Çeviri tablosunu güncelle**, sonra `Assets/Corrigible/Resources/Localization/strings.txt`'te
   boş kalan `en` hücrelerini doldur (satırın sonundaki sekmeden sonra). `{0}` ve `<b>` gibi işaretler aynen kalmalı.
4. **Çevirileri doğrula** menüsü ya da `LocalizationTests` → eksik/uyumsuz çeviri listesi.
5. Oyunda dene: ayarlar kartındaki dil seçimi ya da `PlayerPrefs.SetString("corrigible.language", "en")` + Play.

Ajan olarak: `ueval 'return Corrigible.EditorTools.LocalizationTool.UpdateTable();'` ve
`ueval 'return string.Join("\n", Corrigible.EditorTools.LocalizationTool.Validate());'`.

## Bilinen tuzaklar (bu projede yaşandı)

- **Testler açık sahneyi kirletir:** sahne testleri (`DeskGameScene_*`, `Siginak*Tests`) sahneyi `Additive` açar; sahne
  editörde zaten açıksa aynı bellekteki sahneyi kullanıp nesnelerini değiştirir ve Test Runner kirli sahneyi kaydedebilir.
  Testleri boş sahne açıkken çalıştır; sahneyi koddan değiştireceksen diskten yeniden aç.
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
- **Vardiya kaydı vardiya sonunda alınır:** işgal geçmişi (`S.Hist`) yalnız `Step`'te yazılır; vardiya içinde değişen
  değeri gösteren sunum (son rapor grafiği) güncel değeri ayrıca ekler.
