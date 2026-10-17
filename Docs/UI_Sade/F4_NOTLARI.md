# F4 · Sade baskın tahtası

Tarih: 17 Ekim 2026. Sahne: `Assets/Corrigible/Desk/Scenes/RaidBoard_Sandbox.unity`.

## Kapsam

- Mevcut `Baskin` sahnesinin tahta tabanı, parça kütüphanesi ve iki ışığı kullanılır. Canlı `Baskin.unity` sahnesi değiştirilmez; oyun köprüsü F6 kapsamındadır.
- `SadeRaidCameraFraming`: ortografik 35°/45°, k=1,5; 1080p'de yaklaşık 4,073 kamera boyu, tahta merkezi üstten (960,528). CanvasScaler Expand ile 16:10'da dikey, geniş ekranda yatay boşluk artar.
- `BoardBackgroundView`: kamera Canvas'ında döşenen keçe, ekran boyu lamba dokusu ve tabanın dört alt köşesinden izdüşürülen gölge. Lamba 3D parçaların altında kalır.
- `RaidScreenLabel.prefab`: ayrı `WorldLabels` overlay Canvas'ında can noktaları; hedef/zırh simgeleri, sıra ve güven fişleri. Sivil/soğutma etiketleri ve robot harfi yoktur. Pusula taşınmadı. Kamera veya çapa değişmedikçe ekran konumu yeniden hesaplanmaz.
- `RaidBoard3D` yeni etiket ve işaret ölçeğini serileştirilmiş seçeneklerle alır; eski sahne varsayılanları eski görünümü korur. Geçerli niyetler Core'un `Baskin.Intents` algı API'sinden okunur.
- `MockRaidBoardDataSource`, paketin `board_states.json` dosyasını RNG tüketmeden okur. B1, B2, B3, B4, B5, B6, B7a, B7b, B8: toplam dokuz örnek. Play sırasında `Mock raid board` nesnesindeki `Preset` alanından değiştirilebilir.
- Baskın HUD'u, birim/üstüne gelme kartları F5; büyük paneller ve canlı oyun aktarımı F6 kapsamındadır. F4 sahnesi oyuncu kaydı/komutu çalıştırmaz.

## Doğrulama

- 322/322 EditMode testi geçti (`captures/F4/editmode.json`). Yeni 14 vaka: üç ekran oranında gerçek `Camera.WorldToScreenPoint` ile ±1 px izdüşüm; dokuz ön ayarın konum/can/RNG denetimi; can etiketi prefab'ı ve izole sahne bağlantıları.
- Çeviri tablosu güncellendi; yeni ÇARPIŞMA → COLLISION girdisi eklendi.
- **230/230 canlı kontrol** geçti (`captures/F4/play_qa.json`): dokuz durumda can etiketi sayısı, gereksiz etiketlerin yokluğu, can fişlerinde sayı/harf olmaması, RNG'nin değişmemesi, üç çözünürlükte tahta merkezi ve en küçük yazı. Corrigible kaynaklı konsol hatası yok.
- **14 görüntü:** B1–B8 (B7a/B7b ayrı) Türkçe; B3/B4/B6 İngilizce; B1 için 1920×1200 ve 3440×1440. `captures/F4/`.
- Keçe dokusunun RGB'si zaten renklidir; ikinci yeşil çarpan kaldırıldı. Döşeme, dokunun özgün 40 px ölçüsünde tutuldu.
- Paket karşılaştırması: `B1_compare_diff.png`, `B1_compare_edges.png`, `B1_compare_blink.gif`, `comparison.txt`. Tüm ekran ortalama renk farkı 26,84/255, kenar örtüşmesi %35,7. Kaynak 3D maket ile referans çizim aynı raster görüntü değildir; ayrıca F5 HUD'u bu fazda yoktur. Bu sayı bütün görünümün ±2 px ya da yüz tonlarının ±%5 eşleştiği anlamına gelmez; kadrajın ±1 px koşulu ayrı kamera testleriyle doğrulandı.
- B5'in örnek JSON'unda silinmiş dördüncü robot için bağımsız iptal fişi verisi yoktur; veri uydurulmadı. Canlı iptal/sonuç sunumu F6 köprüsünde tamamlanacak.
- Son sahne ayarlarıyla EditMode testleri tekrar 322/322 geçti. Sandbox Edit modunda bırakıldı; veri karşılaştırması `captures/F4/data_restore.json` içinde.
