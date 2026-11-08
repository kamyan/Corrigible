# Corrigible (Son Şalter) — Yapay zeka geliştirici rehberi

Bu dosya, projeye devam edecek bir yapay zeka ajanının (ya da insan geliştiricinin) **ilk okuması gereken** belgedir.
Ayrıntılar `Docs/` altında; burada bağlam, değişmez kurallar ve en sık kullanılan komutlar var.

## Proje bir bakışta

- **Oyun:** Tek oyunculu strateji. Bir yapay zekâ (YZ) dünyayı işgal etmiştir; oyuncu KYGA'nın son sığınağındaki komuta
  odasından direnişi yönetir: bölgeleri sektör sektör baskınlarla geri alır, sığınağı ayakta tutar. Türkçe adı
  **Son Şalter**, İngilizce/ürün adı **Corrigible**. İlk hedef platform **Steam (PC)**.
- **Üç sahne:** `Desk/Scenes/Giris.unity` (menü: sığınağın kapısının dışı; vardiya kartı, ayarlar dolabı, son şalter;
  build'de ilk), `Desk/Scenes/DeskGame.unity` (Sığınak komuta odası: harita, sayaçlar, vardiya şalteri, kartlı vardiya panosu,
  kabul tezgâhı, CRT, ayarlar dolabı) ve `Scenes/Baskin.unity` (8×8 taktik baskın tahtası; DeskGame'in üstüne eklemeli yüklenir).
  DeskGame doğrudan açılınca menüsüz eski açılış çalışır. Başka oyun ya da deneme sahnesi yok.
- **Motor:** Unity **6000.3.12f1**, URP, **uGUI + TextMeshPro** (UI Toolkit DEĞİL), Input System.
- **Dil:** Oyun içi metinler, kod yorumları ve belgeler **Türkçe**. Kod tanımlayıcıları (sınıf, alan, metot) **İngilizce**.
  Simülasyondaki kimlikler (`"silme"`, `"kopya"`, `"loot"`, `"tek"`) Türkçe ya da kısa ASCII dizgilerdir; kayıt dosyaları
  onlara bağlı, değiştirme.
- **Tasarım dokümanı (GDD):** Claude Docs'ta tutuluyor (https://claude.ai/code/artifact/ea541028-8253-46b0-839e-897f1a7708b4).
  Uygulanmış hâlin güncel özeti `Docs/OYUN-TASARIMI.md`; çelişki varsa **kod ve bu belge** geçerlidir.

## Belgeler

| Belge | İçerik |
|---|---|
| `Docs/OYUN-TASARIMI.md` | Kurtuluş harekâtı, sektörler, baskın, sığınak, sayılar; kodda nerede oldukları |
| `Docs/MIMARI.md` | Assembly'ler, simülasyon/sunum ayrımı, kayıt/replay, sahneler, prefab'lar, UI, ses, lokalizasyon |
| `Docs/GELISTIRME.md` | Unity CLI ile çalışma, testler, oyunu ajan olarak denemek, sık yapılan işler, bilinen tuzaklar |
| `Docs/DURUM.md` | Ne yapıldı, hangi kararlar neden alındı, açık işler |
| `Docs/UI_Siginak_Rev3/IMPLEMENTATION_MAP.md` | Revizyon 3 (model paketi, token 1.3): güncel eşleme, kararlar, açık sorular, aşama sonuçları ve günlük |
| `Docs/UI_Siginak_Rev3/A10_DOGRULAMA.md` | Aşama 10 kanıtı: dil ve punto ölçümleri, gölge atlası ve kare süresi, T1 temizliği |
| `Docs/UI_Siginak_Rev3/IMPLEMENTATION_MAP_A9.md` | Haritanın Aşama 9 sonundaki tam metni (son hâl sadeleşmeden önceki kopya) |
| `Docs/UI_Siginak_Rev2/IMPLEMENTATION_MAP.md` | Revizyon 2 (S1–S8): önceki tur; eşleme, görsel farklar ve günlük (oda primitifleri Revizyon 3 Aşama 2'de modellere geçti) |
| `Docs/UI_Siginak/IMPLEMENTATION_MAP.md` | İlk tur (K1–K7) eşlemesi, poz/kamera ayrıntısı ve günlüğü |

## Değişmez kurallar

1. **Simülasyon deterministiktir; kayıt bunun üstüne kurulu.** Aynı tohum + aynı komutlar = aynı oyun. Kayıt dosyası
   yalnız tohum, denge değerleri ve komut geçmişidir; devam ederken oyun yeniden oynatılır. Bu yüzden rastgelelik yalnız
   `S.Rng` (mulberry32) ve RNG tüketen satırların sırası anlamlıdır. Var olan bir komutun sonucunu ya da RNG sırasını
   değiştirirsen eski kayıtlar başka oyuna döner: `GameRecord.CurrentVersion`'ı artır (eski kayıt yüklenmez) ve
   `LiberationCampaignTests`'in kayıt testlerini güncelle. Taktik tahta (`Core/Baskin.cs`) kendi tohumuyla çalışır;
   beklenen değerleri `RaidEngineTests`'te kilitli.
2. **Core motordan bağımsızdır.** `Corrigible.Core` asmdef'i `noEngineReferences: true`. Orada `UnityEngine` kullanma,
   `System.Random`/`DateTime`/`float` kullanma: tüm sayılar `double`, yuvarlama `Js.Round`, rastgelelik yalnızca `S.Rng`.
3. **Sunum simülasyonu okur, değiştirmez.** Oda, harita ve raporlar `WorldState`'i ve `Simulation`'ın türetilmiş
   değerlerini (`Occupation`, `LiberationFrontier`, `NextLiberationSector`…) okur; hiçbir görünüm durumu doğrudan yazmaz.
   Simülasyon hiçbir kararı metne göre vermez.
4. **Simülasyonu yalnızca komutlar değiştirir.** Oyuncu eylemleri `LiberationDeskView` → `GameController` →
   `Simulation` public metotları (`StartLiberationRaid`, `ResolveRaid`, `SelectLiberationRegion`, `ShelterUpgrade`,
   `ShelterAssign`) üzerinden gider; vardiya `GameController.AdvanceShift` → `Simulation.Step` ile biter (kartlı oyunda pano komutları `GameController.ExecuteBoardCommand`, vardiya `PrintBoardShift` → `FileBoardShift`). Her başarılı
   komut `Rec(...)` ile `GameRecord`'a yazılır. Yeni oyuncu eylemi eklersen `Rec(...)` ile kaydet ve `Replay.Restore`'a
   karşılığını ekle; yoksa kayıttan devam bozulur.
5. **Sahne ve prefab'lar asıl kaynaktır.** Oda ve UI hiyerarşisi `DeskGame.unity`, `Baskin.unity` ve `Desk/Prefabs`
   içinde durur. Bileşenler birbirine **`[SerializeField]` referanslarla** bağlanır; obje adıyla arama (`Find("...")`)
   yapılmaz. Sahneyi sıfırdan üreten kalıcı bir kurucu YOK (bilinçli karar, bkz. `Docs/DURUM.md`). Sahneyi koddan
   değiştirmen gerekirse tek seferlik bir editör script'i yaz, çalıştır, sil.
6. **Her MonoBehaviour/ScriptableObject kendi dosyasında, dosya adı = sınıf adı.** Aksi hâlde sahne kaydedilince
   bileşen "Missing Script" olur (bu projede yaşandı).
7. **Denge değerleri koda gömülmez.** `Balance` (C#) içinde durur; oyunda `Settings/BalanceConfig.asset` üzerinden
   düzenlenir. Renk ve ölçüler tema asset'lerinde (`Desk/Resources/SiginakTheme`, `SadeTheme`, `RaidTheme`,
   `DeskTheme`; kaynakları `Desk/Data/*/tokens_*.json`, `Desk/Data/Sade/raid_tokens.json` ve `Docs/UI_C/spec/tokens.json`), sesler `Settings/SoundBank.asset`'te.
8. **Değişiklikten sonra doğrula:** derle (yeni hata yok), EditMode testleri **boş sahne açıkken** geç, oyunda gerçekten
   dene (bkz. `Docs/GELISTIRME.md` → "Oyunu ajan olarak test etmek").
9. **Oyuncuya görünen her metin çevrilir (Türkçe + İngilizce).** Kodda metin Türkçe yazılır ve `Loc` ile sarılır:
   `Loc.T("...")` düz metin, `Loc.F("... {0} ...", x)` biçimli metin (asla `$"..."` ya da `+` ile birleştirme), statik
   tablolarda `Loc.N("...")` ve gösterirken `Loc.T(değer)`. İlk argüman dizgi sabiti olmalı. Sahne ve prefab'daki sabit
   yazılar `DeskLocText` bileşeniyle çevrilir. Metin ekledikten sonra **Corrigible > Lokalizasyon > Çeviri tablosunu
   güncelle** ve `Resources/Localization/strings.txt`'te İngilizcesini yaz; `LocalizationTests` eksik çeviride kırılır.
   Ayrıntı: `Docs/MIMARI.md` → Lokalizasyon.
10. **Git:** Kullanıcı istemedikçe commit/push yapma. Varsayılan dal `main`.

## Hızlı komutlar

Unity Editor açıkken `unity` CLI'si (Pipeline paketi `com.unity.pipeline`) ile sürülür. Her çağrıya
`--caller plugin --skill unity-cli` ekle. Proje kökü: bu dosyanın bulunduğu klasör. Kısayollar için
`source Tools/unity-helpers.sh` (`ueval`, `ucompile`, `utest`, `uptest`, `uplay`, `ustop`, `ushot`, `uerrors`).

```bash
unity command recompile --caller plugin --skill unity-cli                 # derle
unity command console --caller plugin --skill unity-cli --format json      # konsol (hataları zaman damgasıyla süz)
unity command run_tests --caller plugin --skill unity-cli --format json    # EditMode testleri (önce boş sahne aç)
unity command editor_play --caller plugin --skill unity-cli                # Play
unity command eval --caller plugin --skill unity-cli --code 'return Corrigible.EditorTools.CorrigibleDebug.Capture("Temp/shot.png");'
```

Ayrıntılar ve tuzaklar (`recompile_status` güvenilmez, domain reload sırasında `run_tests` zaman aşımı, testlerin açık
sahneyi kirletmesi vb.): `Docs/GELISTIRME.md`.
