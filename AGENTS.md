# Corrigible (Son Şalter) — Yapay zeka geliştirici rehberi

Bu dosya, projeye devam edecek bir yapay zeka ajanının (ya da insan geliştiricinin) **ilk okuması gereken** belgedir.
Ayrıntılar `Docs/` altında; burada bağlam, değişmez kurallar ve en sık kullanılan komutlar var.

## Proje bir bakışta

- **Oyun:** Tek oyunculu strateji. Bir yapay zekâ (YZ) dünyanın ağlarına sızmıştır; oyuncu KYGA adlı bir ajansın kriz
  masasından onu durdurmaya çalışır. Plague Inc.'in tersi: oyuncu yayılmayı **durduran** taraftır.
  Türkçe adı **Son Şalter**, İngilizce/ürün adı **Corrigible**. İlk hedef platform **Steam (PC)**.
- **Motor:** Unity **6000.3.12f1**, URP, **uGUI + TextMeshPro** (UI Toolkit DEĞİL), Input System.
- **Dil:** Oyun içi metinler, kod yorumları ve belgeler **Türkçe**. Kod tanımlayıcıları (sınıf, alan, metot) **İngilizce**.
  Simülasyondaki kimlikler (`"izleme"`, `"kilit"`, `"sessiz"`) prototipten gelen Türkçe ASCII dizgilerdir; değiştirme.
- **Tarayıcı prototipi:** `Prototype/` (saf JS). Oyun mantığının **referans uygulaması**. C# simülasyonu onunla
  aynı tohum + aynı komutlar için **bit düzeyinde aynı** sonucu verir (bkz. Kural 1).
- **Tasarım dokümanı (GDD):** Claude Docs'ta tutuluyor (https://claude.ai/code/artifact/ea541028-8253-46b0-839e-897f1a7708b4).
  Erişemiyorsan `Docs/OYUN-TASARIMI.md` uygulanmış hâlin güncel özetidir; çelişki varsa **kod ve bu belge** geçerlidir.

## Belgeler

| Belge | İçerik |
|---|---|
| `Docs/OYUN-TASARIMI.md` | Mekanikler, sayılar, sonlar, rakipler, araştırmalar, eğitim; kodda nerede oldukları |
| `Docs/MIMARI.md` | Assembly'ler, simülasyon/sunum ayrımı, Algı Katmanı, determinizm, kayıt/replay, sahne, prefab'lar, UI, eğitim sistemi |
| `Docs/GELISTIRME.md` | Unity CLI ile çalışma, testler, parite, denge testi, hata ayıklama, sık yapılan işlerin tarifleri, bilinen tuzaklar |
| `Docs/DURUM.md` | Ne yapıldı, hangi kararlar neden alındı, açık işler |
| `Prototype/README.md` | Prototipin kullanımı, parite/denge script'leri, deneysel mod ve baskın kuralları |

## Değişmez kurallar

1. **Simülasyon pariteyi korur.** `Assets/Corrigible/Core` (C#) ile `Prototype/sim.js` aynı oyun kuralıdır. Kuralı
   (sayı, formül, RNG çağrısı sırası, olay sırası) değiştiriyorsan **ikisini birlikte** değiştir, sonra
   `Prototype/parite.js` ile beklenen değerleri yeniden üret ve `SimulationTests.FullGame_MatchesBrowserPrototype`'a yaz.
   RNG (`Rng`, mulberry32) tüketen her satırın sırası iki tarafta aynı olmalı. Yalnızca metin değişikliği (haber,
   danışman cümlesi) RNG'ye dokunmuyorsa pariteyi bozmaz.
   **Unity'ye özgü kurallar** (`RuleOptions`, `Core/Rules.cs`: aşamalı öğrenme, olay kartları, kampanya zorluğu) JS
   prototipinde **yok** (kullanıcı kararı: doğrudan Unity'de geliştirildi). Bu kod yalnızca `S.Rules` varken çalışmalı ve
   RNG tüketmeli; `S.Rules` null iken temel parite değişmemeli (`RulesTests.FullStageWithoutEvents_MatchesBaseRules`).
   **Deneysel mod** (`S.exp` / `S.Exp`: saha ekipleri, zaman ölçeği, baskın; bkz. `Prototype/README.md`) iki tarafta da
   var. Deneysel kod yalnızca bu alan varken çalışmalı ve RNG tüketmeli; kapalıyken temel parite değişmemeli. Deneysel
   kuralı ya da `baskin.js`'i değiştirirsen `parite.js`'in deneysel ve baskın bölümlerini de C# testlerine yaz.
2. **Core motordan bağımsızdır.** `Corrigible.Core` asmdef'i `noEngineReferences: true`. Orada `UnityEngine` kullanma,
   `System.Random`/`DateTime`/`float` kullanma: tüm sayılar `double`, yuvarlama `Js.Round` (JS `Math.round` eşleniği),
   rastgelelik yalnızca `S.Rng`.
3. **Sunum simülasyonu yalnızca Algı Katmanı üzerinden okur.** UI gerçek sızma (`r.Inf`) yerine `sim.Shown(r)` ve
   `sim.Global()` kullanır; oyunun "yalan söyleyen ekran" fikri buna dayanır. Sonu açığa çıkarma kontrolü
   `sim.EndRevealed` (sahte zafer sırasında gerçeği saklar).
4. **Simülasyonu yalnızca komutlar değiştirir.** Oyuncu eylemleri `GameController` → `Simulation` public metotları
   (`Act`, `StartResearch`, `ResolveGift`, `Verify`, `GlobalKillSwitch`, `DismissAdvice`, `SetResearchReserve`) üzerinden
   gider; her başarılı komut `GameRecord`'a yazılır. Yeni oyuncu eylemi eklersen `Rec(...)` ile kaydet ve
   `Replay.Restore`'a karşılığını ekle; yoksa kayıttan devam bozulur.
5. **Sahne ve prefab'lar asıl kaynaktır.** UI hiyerarşisi `Scenes/Main.unity` ve `Prefabs/UI/*.prefab` içinde durur.
   Bileşenler birbirine **`[SerializeField]` referanslarla** bağlanır; obje adıyla arama (`Find("...")`) yapılmaz.
   Sahneyi sıfırdan üreten kalıcı bir kurucu YOK (bilinçli karar, bkz. `Docs/DURUM.md`). Sahneyi koddan
   değiştirmen gerekirse tek seferlik bir editör script'i yaz, çalıştır, sil.
6. **Her MonoBehaviour/ScriptableObject kendi dosyasında, dosya adı = sınıf adı.** Aksi hâlde sahne kaydedilince
   bileşen "Missing Script" olur (bu projede yaşandı).
7. **Denge değerleri koda gömülmez.** `Balance` (C#) / `BAL` (JS) içinde durur; oyunda `Settings/BalanceConfig.asset`
   üzerinden düzenlenir. Renkler `Settings/UiTheme.asset`, eğitim içeriği `Settings/TutorialSequence.asset`.
8. **Değişiklikten sonra doğrula:** derle (yeni hata yok), EditMode testleri geç, oyunda gerçekten dene
   (bkz. `Docs/GELISTIRME.md` → "Oyunu ajan olarak test etmek"). Denge değiştiyse `Prototype/denge-testi.js`'i
   önce ve sonra çalıştırıp karşılaştır.
9. **Oyuncuya görünen her metin çevrilir (Türkçe + İngilizce).** Kodda metin Türkçe yazılır ve `Loc` ile sarılır:
   `Loc.T("...")` düz metin, `Loc.F("... {0} ...", x)` biçimli metin (asla `$"..."` ya da `+` ile birleştirme), statik
   tablolarda `Loc.N("...")` ve gösterirken `Loc.T(değer)`. İlk argüman dizgi sabiti olmalı. Sahnedeki sabit yazılar
   `LocalizedText` bileşeniyle çevrilir. Metin ekledikten sonra **Corrigible > Lokalizasyon > Çeviri tablosunu güncelle**
   ve `Resources/Localization/strings.txt`'te İngilizcesini yaz; `LocalizationTests` eksik çeviride kırılır.
   Simülasyon hiçbir kararı metne göre vermez (dil oyunu değiştirmez; `Language_DoesNotChangeTheGame`).
   Ayrıntı: `Docs/MIMARI.md` → Lokalizasyon. (Tarayıcı prototipi yalnızca Türkçe.)
10. **Git:** Kullanıcı istemedikçe commit/push yapma. Varsayılan dal `main`.

## Hızlı komutlar

Unity Editor açıkken `unity` CLI'si (Pipeline paketi `com.unity.pipeline`) ile sürülür. Her çağrıya
`--caller plugin --skill unity-cli` ekle. Proje kökü: bu dosyanın bulunduğu klasör.

```bash
unity command recompile --caller plugin --skill unity-cli                 # derle
unity command console --caller plugin --skill unity-cli --format json      # konsol (hataları zaman damgasıyla süz)
unity command run_tests --caller plugin --skill unity-cli --format json    # EditMode testleri
unity command editor_play --caller plugin --skill unity-cli                # Play
unity command eval --caller plugin --skill unity-cli --code 'return Corrigible.EditorTools.CorrigibleDebug.Capture("Temp/shot.png");'
```

```bash
JSC=/System/Library/Frameworks/JavaScriptCore.framework/Versions/Current/Helpers/jsc
cd Prototype && $JSC sim.js parite.js        # C# parite testinin beklenen değerleri
cd Prototype && $JSC sim.js denge-testi.js   # denge: rakip × oyuncu modeli kazanma oranları
```

Ayrıntılar ve tuzaklar (`recompile_status` güvenilmez, domain reload sırasında `run_tests` zaman aşımı, vb.):
`Docs/GELISTIRME.md`.
