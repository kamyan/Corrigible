# Aşama 9 · Baskın kitleri — doğrulama

2026-11-08 · Unity 6000.3.12f1 · R11/R12 · kayıt biçimi 15.

## Uygulanan görünüm

- 12 bölge ve varsayılan kit: Relief / ReliefIso / Bridge / Rubble; 13 sivil cihaz ve giriş kapısı. Giriş kapıları onaylı 0,29 m sınırına ölçeklendi; dekor, çarpıştırıcıları yok. Varsayılan moloz eski yedeğini kullanır.
- Kanalın 16 komşuluk maskesi model parçalarından editörde birleştirildi; kare başına tek renderer, gölge atma/alma kapalı. Duvar en çok gövde + iki köprü; Doğu Avrupa kayar rafı kendi gövde ve çarkını kullanır.
- Sekiz makine, üç isimli saha piyonu, dolaplar, kapalı/açık ikmal kasası, tüp, kaynak kolu ve tavan enkazı paket modellerine bağlı. Kaynak kolunun turuncu burnu kol ile hedefe döner; çarpıştırıcı kolun tepesine uzanır. Model paleti ve makine göz ışıması korunur.
- Piyonda harf, düşme, hasar bandı, tik ve plaka ayrı model parçalarıdır. Tur bitti: harf 0,2 s'de 180° ve kraft arka yüz; düşme: ayrı menteşede −70°. Madenci/Sis ve eski simülasyon yolu durur. BackupCore'un Core karşılığı olmadığı için bağlanmadı.
- Tahtada gerçek kişinin adı, pipleri ve plakası kâğıt şeritte; alt kartlarda CAN/HAR/GÜÇ ve gerçek sahipli ekipman var. Kullanılmış EMP hücresi veya kırılmış plaka çizilir. İngilizce kısaltma artık kişi adını gizlemiyor.
- Hareket/vuruş asetatları model quad + kelimesiz tarama dokusudur; kopya ve uyanma işaretleri modelden gelir. Oyuncu yürüyüş süresi mesafe başına 0,16 s katsayısını kullanır; mevcut makine hızlandırması korunur.
- Kâğıtlar #D7D5C9, kesik kenarlı 9 dilim, 24 px ızgara, bant ve sert sağ gölge kullanır. Panel girişi 0,28 s / 40 px / ±1,5°. Antet: bina, oda, bölge, pafta, anahtar plan, ölçek, kampanyada gerçek vardiya ve açılış damgası; 400 ms hover bina notunu açar. Uzun EN bina adını küçültmemek için antet 364 × 184 px.
- İkmal görevinde kasanın metal/devre/kumaş/kimyasal/erzak dökümü mevcut Balance'tan okunur. Bağımsız örnek sahnede kampanya olmadığı için sahte vardiya ya da kasa miktarı yazılmaz.
- Kaçışın son turunda tahtada kalan canlı kişiler için emir her durumda iki adımlıdır; isimleri ve kaybolacak eşyaları gösterilir. Kartta GERİDE KALIRSA ÖLÜR. Simülasyonun ölüm/kaçış/geri çekilme kuralları değişmedi.

## Kabulde giderilenler

Yetenek açıklamasının karttan taşması, EN yetenek adının Q fişini örtmesi ve alt kartların ekran dışına sarkması giderildi. Tekrarlanan uzun açıklama yerine menzil satırı ve zaten gerçek hasarı gösteren alt eylemler kullanılır. Sağlık şeridinden tekrar durum/bayrak moduna geçen etiketin metin kutusu yenilenir; TURN DONE ve sıra numaraları taşmaz. 14 px’e yükselen kural lejantında kanal satırının yüksekliği gerçek metne göre uzar. Çevrilmiş Core özellikleri ikinci kez çevrilmez.

R12 molozları dairesel değildir: eski serbest yaw yerine 90° adımlarla döner, .47 m yatay uzanım sınırıyla kendi karesinde kalır. Malzeme testleri Rev2'nin üç yüz + tek vurgu şartı yerine kaynak model paletini denetler; çelik pim ve koyu metalin parlaklığı paket değeridir.

## Son kontroller

- Tam **EditMode 969/969**, tam **PlayMode 83/83** geçti; başarısız veya atlanan vaka yok. Son antet yerleşimi sonrası baskın sözleşmeleri **5/5** geçti. Sonuçlar `A9_EditMode_sonuclar.json` ve `A9_PlayMode_sonuclar.json`.

- Genel punto taraması: **2438 metin, 0 ihlal, 150 önceki gerekçeli istisna**. Son canlı EN kaçış görünümünde **en küçük metin 14 px, taşma 0**; `A9_live_metrics.json`.
- Kamera ölçeği: **132,58 px / dünya birimi** (1080 referansı). İzometrik zemindeki bir kare kenarının projeksiyonu **108,08 px**; bunlar farklı ölçülerdir. Kapı yüksekliği .29 m. Kamera ve oyun koordinatları değişmedi.
- 13 kit renkli ve gri tonda aynı haritayla karşılaştırıldı. Sıra soldan sağa, yukarıdan aşağı: default, na, sa, weu, eeu, ru, me, af, in, cn, jk, sea, oc.
- Ortak PC_RPAsset'in 2048 ana gölge atlası korundu (R15: 2048 yeterliyse dokunmama kararı); bölge biçimleri ve ince parçalar görüntülerde ayrılıyor. Tüm odanın performans/atlas kıyası Aşama 10 kapsamıdır.

## Kanıtlar

- `screens_unity_rev3/A9_R11a_TR.png`, `_EN.png`: WEU / planlama.
- `screens_unity_rev3/A9_R11b_TR.png`, `_EN.png`: AF / gerçek makine evresinde kilitli girdi.
- `screens_unity_rev3/A9_R11c_TR.png`, `_EN.png`: CN / kaynak kolu, tüp ve tavan enkazı.
- `screens_unity_rev3/A9_escape_TR.png`, `_EN.png`: bütün haklar bitmişken de isimli kalıcı kayıp onayı.
- `screens_unity_rev3/A9_R12_atlas.png`, `_gray.png`: 13 kit karşılaştırması.

R11 görselleri bağımsız baskın sahnesinde seed 79'dan türetilen isimli kadro / seed 731 tahta fikstürleridir; referansın Deniz/Cem/Zeynep adları zorla yazılmadı. CN'de üç olay parçasını birlikte görmek için ayrı sunum tahtasına tüp ve enkaz eklendi. Gerçek kampanya → baskın → geri çekilme → masa → kayıt/replay yolu PlayMode testinde ayrı doğrulandı.

## Kapatma ve koruma

Üç sahnede eksik script **0**; boşalan sahne referansı **0**. 12 görsel kaynak değişimi ve eski BoardBase çocuklarının kaldırılması kasıtlı (`A9_sahne_referanslari.txt`). Çeviri doğrulamasında eksik **0** (`A9_lokalizasyon.json`). Bina notlarının iki dildeki 24 ölçümü sığıyor; en uzun metin 69,59 px, ayrılan alan 120 px (`A9_bina_notlari.json`).

53 birleşik baskın prefab'ı, 16 kanal mesh'i ve UV kopyaları sahne/prefab kaynağı olarak kalır. Orijinal 198 modelin geometri/parça denetimleri geçti; ham model paketine yazılmadı. Tek seferlik sahne/varlık kurucuları ve test oturumu koruyucusu Assets'ten silindi; doku üreticisi kalır.

Gerçek kayıt, eski kayıt, profil ve iki masa JSON'u başlangıç SHA-256 değerleriyle aynı. Ses/dil tercihleri aynı; 24 font atlası bu aşamanın başlangıç yedeklerinden geri alınıp tekrar doğrulandı. Test kayıt yönlendirmesi boş, Play kapalı, **Giris Edit Mode açık** (`A9_koruma_denetimi.json`). Kayıt biçimi 15; Core, Balance ve RNG akışı bu aşamada değiştirilmedi. Kullanıcının önceki değişiklikleri korunur. Commit/push yapılmadı.

Sıradaki kapsam **Aşama 10**: bütün pozların dil/okunurluk/performance denetimi ve ancak kullanıcı onayıyla eski görünüm temizliği.
