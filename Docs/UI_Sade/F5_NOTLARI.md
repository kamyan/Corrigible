# Sade arayüz · F5: Baskın HUD ve kartlar

17 Ekim 2026. F5, `Assets/Corrigible/Desk/Scenes/RaidBoard_Sandbox.unity` üzerinde geliştirilir. Canlı `Assets/Corrigible/Scenes/Baskin.unity` aktarımı ve S3–S9 durum panelleri F6 kapsamındadır.

## Uygulama

- Tur plakası ve altı tur kutusu; HEDEF kartı, hedef can noktaları, zırh, sivil göstergeleri ve YZ yeteneği çipi.
- Günlük, Kurallar, Geri çekil; üç ekip kartı, kullanım hakları, hasarlı EMP görseli; Geri al ve Turu bitir.
- Seçilen ekip kartı 10 px, üstüne gelinen kart 2 px kalkar. Birim kartında hareket, yetenek, Q, etki çipleri; Analist için pasif Tarama satırı vardır. Tarama mevcut Core kuralıdır, ayrıca satın alınan/harcanan bir komut değildir.
- Kare incelemesi 150 ms gecikmeyle sağ alt sabit konumda açılır; satır arttığında yukarı büyür. Kartlarda 120 ms opaklık geçişi vardır.
- `IRaidDataSource` / `IRaidCommands` / `RaidViewModel` motor sunum assembly'sinde, `MockRaidDataSource` ve `MockRaidCommands` Bridge assembly'sindedir. Ekran prefab'ları ve sahne bağlantıları SerializeField ile saklanır; çalışma anında UI hiyerarşisi kurulmaz.
- Mock kaynak ayrı bir `Baskin.BState` üzerinde Core'un Reachable / Targets / Preview / DoMove / DoAct / Undo API'lerini kullanır. Ana simülasyona, GameController'a, kayıt/replay'e veya profile bağlı değildir. Niyetler `Baskin.Intents` algı API'sinden okunur.
- S1 ve S2 brif verileri doğrudan yüklenebilir; F4 kaynak bileşeni sahnede korunur fakat aynı tahtayı eşzamanlı sürmemesi için kapalıdır.

Sandbox Play açılışında S1 gösterir. S2 önizlemesini yüklemek için Unity CLI:

```bash
unity command eval 'UnityEngine.Object.FindFirstObjectByType<Corrigible.Desk.Bridge.MockRaidDataSource>().LoadPreset(2); return true;' --caller plugin --skill unity-cli
```

`LoadPreset(1)` ilk duruma döner. Örnekler ana oyunun kaydını kullanmaz.

## Girdi

| Girdi | Mock davranış |
|---|---|
| 1 / 2 / 3, ekip kartı, figür | Seçim ve hareket kareleri |
| Mavi kare | Taşıma; hareket hakkı tüketilir |
| Q veya yetenek satırı | Hazır yetenek ve hedef kareleri |
| Hedef üstüne gelme / tıklama | Salt okunur önizleme / kullanım |
| Z / Geri al | Son hareketi veya yeteneği geri alma |
| Enter / Turu bitir | Kullanılmamış hak varsa onay, ikinci çağrıda makineler evresi kilidi |
| Esc / sağ tık | Önce onay; sonra hazır yetenek, seçim, açık panel, duraklatma |
| L / Günlük | Günlük isteği; okunmamış nokta söner |
| F1 / ? / Kurallar | Kurallar isteği; günlükle aynı anda açık olmaz |
| Boşluk | Makineler evresinde hızlandırma durumunu aç/kapa |
| Geri çekil | İki istekle onay, Esc ile iptal |

F5'te günlük, kurallar ve onaylar sade bir komut bilgi fişiyle temsil edilir. Tam günlük/kurallar/onay/sonuç panelleri, makinelerin niyet canlandırması, olay fişi ve canlı oyun köprüsü F6'da yapılacaktır. Mock makineler evresi bilerek otomatik tur yürütmez; bu evreden çıkmak için `LoadPreset(1)` kullanılabilir.

## Doğrulama

EditMode: **330/330 başarılı** (yeni 8 komut testi dahil). Hareket/yetenek geri alma, salt okunur önizleme, geçersiz hedefler, birimlere özel satırlar, iki aşamalı onay ve makineler kilidi doğrulandı.

- **439/439 otomatik Play kontrolü**, **10/10 gerçek fare/klavye kontrolü** başarılı; test sırasında yeni runtime hata kaydı yok.
- Görünür ikonlarda null sprite yok. TMP hem taşma hem `isTextTruncated` yönünden kontrol edildi. İngilizce hedef başlığı “TARGET”, menziller “straight line, 2–5” / “straight line, 5” olarak sığdırıldı; Q fişi metinden ayrıldı.
- Sekiz yakalama: S1/S2 Türkçe; S1/S2, Analist ve EMP İngilizce; 1920×1200 ve 3440×1440. `captures/F5/` altında.
- S-Baskin1 raster karşılaştırması: ortalama fark **17,84/255**, kenar örtüşmesi **%51,9**. S-Baskin2: **19,24/255**, **%50,1**. Referans çizimi ile mevcut 3D tahta/ışık/malzeme farklılıkları sürer; bu sayılar piksel eşitliği iddiası değildir. Diff, kenar ve blink çıktıları kaydedildi.
- Kayıt, profil, önceki TestResults, 11 tercih ve ProjectSettings başlangıç yedeğiyle aynı. Preloaded asset sayısı 1; editör temiz `RaidBoard_Sandbox` sahnesinde Edit modunda.

Kanıtlar: `captures/F5/editmode.json`, `play_qa.json`, `native_qa.json`, `data_restore.json`, S1/S2 karşılaştırma dosyaları. Canlı `Baskin.unity` değiştirilmedi. F6 başlatılmadı.
