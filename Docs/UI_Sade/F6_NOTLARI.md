# Sade arayüz · F6: Baskın durumları ve canlı köprü

17–18 Ekim 2026. S3–S9 durum panelleri, `RaidBoard_Sandbox` örnekleri ve canlı `Scenes/Baskin.unity` aktarımı.

## Uygulama

- S3: makine evresi, sıra kutuları ve son olay fişi; ekip/tahta/geri al/tur bitir kilidi. Boşluk, niyet beklemesini 0,25 → 0,08 sn yapar. Gerçek hedef, efekt yürütülmeye başlayınca açıklanır.
- S4: kullanılmamış hareket/yetenek hakları ve yalnız kesin görünen niyetlerden tehdit uyarısı. İkinci Enter bitirir, Esc iptal eder. Çok birimde not büyür.
- S5: kaydırılabilir operasyon günlüğü, satır türleri, ele geçirilen YZ satırı. Geçmiş kırpılmaz; kanonik `RaidLogRow` prefab'ları havuzlanır.
- S6: altı kural ve on maddelik lejant. Türkçe/İngilizce içerik yüksekliği ölçülür.
- S7: son tur etiketi, kırılan yedek, sivil kaybı, düşen EMP ve kazandıran hamle kutusu.
- S8: karartma, yükseltilmiş ikinci onay düğmesi ve geri çekilme fişi; Esc vazgeçer.
- S9: HUD solması, hedef karesinde spot, sonuç damgası/fişi; Enter ve düğme aynı rapora geç komutunu kullanır. Başarısızlık ve süre dolması aynı sonuç düzenindedir.

`RaidDataSource` ayrı taktik BState üzerinde Core hareket/yetenek/geri alma/niyet API'lerini kullanır. `MockRaidDataSource` paket örneklerini, `LiveRaidDataSource` GameController'ın bekleyen operasyonunu açar. HUD ve paneller aynı görünüm modeli/komut sınırındadır. Hiyerarşi sahne ve prefab'lardadır; kalıcı kurucu eklenmedi.

Canlı sahne aynı GUID ile güncellendi. `LiveRaidDataSource`, `S.Raid.Params` ile taktiği açar; sonuç panelinden devam edildiğinde mevcut `GameController.ResolveRaid` ve `raidres` kayıt/replay yolu kullanılır. Core, Prototype, ekonomi sayıları ve kayıt biçimi değiştirilmedi. Ana masanın DeskInput bileşenleri baskın sırasında askıya alınır ve dönüşte açılır; ana EventSystem varken baskının yerel sistemi kapatılır.

## Deneme

Sandbox Play açılışı S1'dir. Örneğin:

```bash
unity command eval 'UnityEngine.Object.FindFirstObjectByType<Corrigible.Desk.Bridge.MockRaidDataSource>().LoadPreset(7); return true;' --caller plugin --skill unity-cli
```

`LoadPreset(1…9)`, `LoadBoardPreset("B3")`, `LoadBoardPreset("B4")`, `LoadBoardPreset("B7b")` kullanılabilir. Gerçek oyun için `Desk/Scenes/DeskGame.unity` üzerinden ekip gönder, denetimle varlığı doğrula ve Baskın başlat. `Baskin.unity` tek başına açılırsa kayıt kullanmadan bağımsız örnek kurar.

## Doğrulama

- EditMode: **341/341 başarılı**; S3–S9, algı sınırı, kırılmış hedef formu ve canlı sahne referansları dahil.
- **826/826 Play kontrolü**: S3–S9 TR/EN, B3/B4/B7b; 1920×1080, 1920×1200 ve 3440×1440. 19 yakalama; TMP taşma/kırpılma ve runtime hata sayısı 0. Sonuç damgasının boş zemini ve İngilizce onay düğmesi denemede bulunup düzeltildi.
- **16/16 girdi kontrolü**: Unity Input System sanal klavye/fare, gerçek Update/EventSystem/komut zinciri. Günlük/kurallar açma-kapatma, Enter/Esc, hızlandırma, makine kilidi, iki aşamalı geri çekilme ve yükseltilmiş düğme denendi. OS üzerinden fiziksel girdi denemesi yapılmadı; CUA bağlantısı zaman aşımına uğradı.
- **56/56 canlı kontrol**: DeskGame'de ekip gönderme, denetim ve gerçek StartRaid komutu; kontrollü taktik bitiş durumlarıyla başarı/süre dolması/geri çekilme. Her birinde tek `raidres`, sahne kapanışı, ana masa girdisi, görünür operasyon raporu ve replay'de fon/SG/bölge sızması eşitliği doğrulandı. Canlı sahnede eksik script sayısı 0.
- Tam oyun raporu `live_report_Tr.png` ile ayrıca yakalandı. Kanıtlar: `captures/F6/{editmode,play_qa,input_qa,live_qa,comparison,data_restore}.json`.
- Referans karşılaştırması: S3–S9 ortalama fark **17,34–22,25/255**, kenar örtüşmesi **%33,0–50,6**. Diff/kenar/blink çıktıları kaydedildi; mevcut 3D maket ile çizimin raster eşitliği iddia edilmez.
- Kayıt, profil, eski TestResults, 11 tercih ve ProjectSettings başlangıç yedeğiyle aynı doğrulandı. Preloaded asset sayısı 1; editör temiz `RaidBoard_Sandbox` sahnesinde Edit modunda. macOS tercih dosyasının diske yazılması beklenip karşılaştırıldı.

## Sınırlar

F7 başlatılmadı. Tam ekran raster eşitliği iddia edilmez; mevcut 3D malzeme/ışık/etiket farklılıkları ve genel UI batch hedefi F7'de ele alınacak. Eski `RaidView` C# dosyaları korunur, canlı sahne artık Sade HUD'u kullanır. Ses üretimi ve gamepad akışı bu fazın kapsamı değildir.
