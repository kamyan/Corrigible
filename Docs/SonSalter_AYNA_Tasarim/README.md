# Son Şalter · AYNA tasarım paketi

**Sürüm 1.0 · 17.10.2026.** Kaynak: Claude Design kanvası, **AYNA** sayfası (A1–A12 kareleri, P1–P3 sayfaları).
Kanvas: https://claude.ai/artifact/3Wa4iRa62AwHYEsqsZmeYS

Oyuncu altın YZ (◆), rakip mavi AYNA (⬢). AYNA oyuncuyu yüzdelerle tahmin eder; onu şaşırtmak ödüllendirilir. Arayüzün taşıdığı duygu: **izleniyorsun, ama sen de onun seni nasıl gördüğünü görüyorsun.**

Görsel dil, harita, küre algoritması ve holo tahta Fx paketindekinin aynısı (`SonSalter_Fx_Paketi`). Bu paket yalnız AYNA’nın eklediklerini ve değiştirdiklerini verir.

---

## İçerik

```
SonSalter_AYNA_Tasarim/
├─ README.md                  bu dosya
├─ HAREKET.md                 hareket notları ve süreler
├─ SORULAR.md                 brifte eksik ya da çelişkili 15 nokta
├─ fonts/web/                 Chakra Petch · IBM Plex Mono · IBM Plex Sans (woff2, OFL). TMP için TTF’ler Fx paketinde.
├─ reference/
│  ├─ html/                   kare başına tek HTML (1920×1080, mutlak konumlu) + harita SVG’leri
│  ├─ png/                    kare başına PNG + _genel_bakis.jpg
│  ├─ measure/                kare başına ölçü TSV’si + küre maskesi (_kure.json)
│  ├─ en_prova/               EN taslağıyla çizilmiş kareler
│  └─ oranlar/                A2 ve A6: 1920×1200 · 2580×1080 (21:9) · 2560×1440
└─ spec/
   ├─ tokens_ayna.json        tokens_fx.json’a ek: renk, yazı, bileşen ölçüleri, tahta paleti, hareket, oranlar
   ├─ strings_ayna_tr_en.csv  TR/EN metinler, sığma ölçüsüyle (582 metin + 21 kalıp)
   └─ data/                   ortak.json (katalog) + A1.json … A12.json (kare başına örnek veri)
```

## Kareler

| Kod | Ekran | Ne gösterir |
|---|---|---|
| A1 | Başlangıç | Yeni nesil: üç mimari, başlangıç bölgesi (Batı Avrupa), “AYNA v7 seni hatırlıyor”, öğrenme ayarı, eşleşme |
| A2 | Planlama | Ana ekran (F-Masa1’in yeni hâli): 12 küre, ◆⬢ çipleri, verilmiş 6 emir, Güney Asya kartı |
| A3 | AÇ | İki plan birlikte açılır; 3 çakışma; sis; SÜRPRİZ (+3 İşlem); AYNA satırı |
| A4 | ÇÖZ | Harita karartılmış; çözüm sırası listesi (karşılaşma, AYNA saldırısı, teklif) |
| A5 | Kadro seçimi | B birliğinin 5 birimi, 3 yuva, sınıf üçgeni, kazanma tahmini %58, AYNA’nın beklediği |
| A6 | Savaş tahtası | Tur 2/6; AYNA’nın 3 niyeti; açılan tuzak; EMP önizlemesi “SERSEMLER · dron 2 tur” |
| A7 | Savaş sonucu | KAZANDIN ◆2 ⬢1; Güney Asya ◆3 ⬢2 → ◆5 ⬢1; tuzak açıklaması ve ısı haritası |
| A8 | Gelişim | 5 aile × 3 kademe; durumlar; Dil 3 ayrıntısı; Tekillik kartı |
| A9 | Birlikler | Üç birlik (hazır / savaşta / yolda), onarım, üretim listesi ve kuyruk |
| A10 | AYNA’nın modeli | Üç tahmin + iz; model özellikleri; tahta alışkanlığı; okunabilirlik eğrisi; günlük |
| A11 | Tekillik ilanı | Döngü 19 olayı; geri sayım 4; erişimi kesilecek AYNA bölgeleri haritada |
| A12 | Oyun sonu | SİLME ZAFERİ; özet; tahmin karnesi 14/9; en iyi sürpriz; AYNA v8’in öğrendikleri |
| P1 | Bileşenler | Sunucu çipi hâlleri, veri türleri, emir ve harita işaretleri, çakışma, evre, göstergeler, sürpriz, tahmin, taraf simgeleri |
| P2 | Birimler | 10 birim × 2 taraf holo parçalar, 7 hâl, kart küçük resimleri, tahta parçaları |
| P3 | Tahta işaretleri | Kareler, niyet okları, olaylar, can göstergesi, açılır yazılar ve renk kuralı |

## Kararlar

**Taraf rengi** (`tokens_ayna.json › colors.side`)
- Altın = sen: `#FFB547`, metin `#FFD58C`, simge ◆. Mavi = AYNA: `#3D8BFF`, metin `#8FB8FF`, simge ⬢. Gri kesik daire ○ = boş ya da bilinmiyor.
- Camgöbeği `#5BE4FF` yalnız krom: seçim, odak, evre, sistem düğmeleri, hareket karesi. AYNA’ya ait hiçbir şey camgöbeği değil; kromun hiçbir parçası AYNA mavisi değil.
- Ayrım ton ve açıklıkla: cy ton 190°, OKLCH açıklık %86, kroma 0,12; AYNA ton 216°, %65, kroma 0,19. Fark ΔE(OK) 0,25. Kürenin buz rengi (`ice`) cy’ye çok yakın (ΔE 0,09); arayüzde kullanılmaz, yalnız kürenin içinde.
- Kırmızı yalnız tehlike: vurulacak kare, tehdit, tuzak, farkındalık 70+, Tekillik ilanı, hafızayı sil.

**Şüpheli rozeti** — kehribardan **mora** taşındı. AYNA’da kehribar “senin” demek; şüphe bir tarafın değil bilginin niteliği. Mor artık “belirsiz bilgi” ailesi: ≈ rapor, şüpheli, AYNA GÜCÜ ± bandı, sis halkası. Şekil farkı korunur: şüpheli düz mor halka + “?” + nabız; bilinmiyor gri kesik halka; rapor mor kesik kutu + ≈; doğrulandı nane halka + ✓.

**Renk tek başına anlam taşımaz.** Taraf her yerde simgeyle de belli: değer çipi `◆3 ⬢≈2 /8`, sunucu hücresi, jeton, birim altlığı, can noktaları, düğüm, kart başlıkları.

**Küre ve bölge boyası.** Algoritma Fx’in aynısı; düzey sahiplikten: yalnız ◆ → 8, yalnız ⬢ → 92, ikisi → 30 + 40·⬢/(◆+⬢), sis ya da boş → bilinmiyor küresi. Yarıçap 24 + 2,2 × sunucu. Bölge boyası: altın noktalar, mavi noktalar; çekişmelide iki renk oranla; sisli bölge gri tarama.

**Harita.** 12 bölge. Fx’teki Orta Amerika Kuzey Amerika’ya katıldı; Japonya-Kore yeni çokgen (SORULAR 1). Bağlantı ağı öneridir (SORULAR 3).

**Holo tahta.** F-Baskın kamerası ve 8×8 aynen. Birimler kare merkezinden 1,4× izdüşüm; sunucu rafı 0,4 yükseklikte ki arkadaki birimi örtmesin. Yüz ve kenar renkleri `tokens_ayna.json › board`. AYNA’nın niyeti mavi ok + mavi sıra bayrağı, vurulacak kare kırmızı, senin yeteneğin altın, hareket ve seçim camgöbeği. Gizli tuzak oyuncuya çizilmez.

**HUD düzeni** (köşe kuralı)
- Üst sol (İŞLEM · VERİ · HEDEF) soldan sağa, üst sağ (FARKINDALIK · OKUNABİLİRLİK · AYNA GÜCÜ) sağdan sola akar; aralık 8 (Fx kuralı: içerik büyürse sonraki kutu kayar). Evre plakası (DÖNGÜ · PLANLA · AÇ · ÇÖZ) üst ortada, Fx’teki hız tuşlarının yerinde.
- Alt sol sekmeler; alt sağ grup (emir kuyruğu · LEJANT · birincil düğme) sağdan sola akar.
- Bölge kartı sağ kenara yaslı, çip şeridinin altında (x = 1904 − w, y = 92); seçili bölgenin küresini, adını, çipini ve jetonlarını örtmez. Çekmeceler Fx kabı: sol 16, alt kenar 1004.

**EN için yer.** Bütün karelerde metinler %20 uzatılarak denendi, sonra EN taslağıyla çizildi (`reference/en_prova`): taşma ve çakışma yok. CSV’de `fit`: `ok` = EN, TR genişliğinin %20 fazlasına sığıyor; `prova` = daha uzun ama provada sığdı.

**Ekran oranları** (`reference/oranlar`, `tokens_ayna.json › layout.aspect`)
- Harita kaplar (cover); işaretler 1× kalır, yerleri haritayı izler. HUD köşelere bağlı.
- 21:9’da yalnız Avrasya üst şeride yaklaşır. Kural: düğüm kümesi (küre, çip, ad, yakın jetonlar) üst HUD kutularına 8 px’ten fazla yaklaşamaz, aşağı itilir (Avrasya 42 px, kendi bölgesinde kalır).
- Tahta kesilmez: 21:9’da yanlarda, 16:10’da üstte ve altta daha çok zemin.

**Türkçe yazım.** Büyük harf Türkçe kuralıyla (DİL, ENERJİ, SIZMA BİRİMİ). Sayı biçimi: %64, −21, 0,6 sn, 3–8, 8×8.

## Dosyaları okuma

- **HTML.** Chrome’da aç. Yazı tipleri `fonts/web/fonts.css`’ten, harita SVG’leri aynı klasörden yüklenir; küre motoru (Fx `fx_orb.js`) sayfanın içinde. Her öğe mutlak konumlu; `data-m` kimlikleri ölçü dosyasındaki `id` sütunudur.
- **PNG.** 3,8 sn’de alındı; sürekli döngüler sabit evrede donduruldu. Küreler zamana bağlı: karşılaştırmada `measure/*_kure.json` kutularını dışarıda bırak.
- **Ölçü TSV.** Sütunlar: `id, x, y, w, h, layoutW, layoutH, font, size, weight, lineHeight, letterSpacing, color, background, border, radius, shadow, transform, opacity, text`. Satırlar `data-m` kutuları ve sınıflı alt öğeleri.
- **Örnek veri JSON.** Anahtarlar İngilizce, görünen metinler karede yazdığı gibi Türkçe. Tahta koordinatı (satır, sütun), brifteki gibi; ekran koordinatı 1920×1080 px. Brifte olmayan her değer dosyanın `derived` listesinde, nedeniyle. Oyuncunun görmediği gerçekler (ör. Doğu Asya’daki 6 AYNA sunucusu, gizli tuzak) `trueValue` / `shownToPlayer: false` ile işaretli.
- **Tokenlar.** `tokens_fx.json`’a ektir; iki dosya birlikte okunur. Yazı tablosu biçimi Fx’inki: `[aile, ağırlık, px, harf aralığı em, renk, not]`.
- **Metin CSV’si.** İlk beş sütun Fx CSV’siyle aynı (`key, tr, en_draft, maxWidthPxTR, note`); sonra `type` (ui · veri · örnek · ayna · kod · sayfa · kalıp), `frames`, `context` (`data-m`), `font`, `enPxEst`, `ratio`, `fit`. `kalıp` satırları sayılı metinlerin biçimi (TR’de sayıdan sonra tekil, EN’de tekil/çoğul; yüzde işareti EN’de sonda).

## Uygulama notları (Unity uGUI + TMP)

| Fx bileşeni | AYNA’da |
|---|---|
| `FxPanel`, parantezler, süpürme | aynen; büyük paneller daha opak (blur yok, altındaki yazı sızmasın) |
| `FxTrustBadge` | `sus` artık mor (yukarıda) |
| `FxTypewriter` | AYNA satırı: 40 adım / 1,8 sn, 0,5 sn gecikme; satır kaydırır |
| `FxLineGraphic` | yayılım iplikleri (altın / mavi), rota, niyet okları; akış hızları HAREKET.md’de |
| `FxLedBar` | farkındalık (70+ kırmızı bölge), AYNA GÜCÜ (mor ± bandı), tahmin çubukları (mavi) |
| `OrbView` | bölge küreleri, kart küresi (r 28), AYNA v7 başlığı; düzey ve tür örnek verideki `orb` alanından |
| Üst çip grupları | `HorizontalLayoutGroup` (aralık 8) + `ContentSizeFitter`; sağ grup sağa yaslı |
| `MapMarkers` | 1× işaretler; kaçınma kuralı (yukarıda); iplikler işaretin yeni yerine bağlanır |

- Işımalar pişmiş sprite (kutu `fx_glow_frame`, nokta `fx_glow_dot_32`), metin ışıması TMP Underlay. Backdrop blur ve CSS filtresi yok.
- Holo tahta parçaları URP Unlit, az poligon; her parçanın yüz ve kenar hex’i `tokens_ayna.json › board`.

## Bilinen sınırlar

- HTML önizlemesi ışımaları `box-shadow` ve `drop-shadow` ile çizer; Unity’de bunlar pişmiş sprite. Harita SVG’sindeki kıyı ışıması da önizleme içindir; Unity’de Fx harita dokuları kullanılır.
- Hareketlerin bir kısmı durağan karede gösterilemez; HAREKET.md’de “öneri” diye işaretli.
- Sayılar geçici. Brifte olmayanlar `derived` listelerinde; açık sorular SORULAR.md’de.
