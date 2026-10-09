# Tarayıcı prototipi (referans simülasyon + deneyler)

Saf JavaScript, bağımlılık yok. İki rolü var:

1. **Referans simülasyon.** `sim.js`, deneysel mod kapalıyken Unity'deki `Assets/Corrigible/Core` ile aynı tohum ve
   komutlarla bit düzeyinde aynı sonucu verir (parite). Kural değişikliği C# ve JS'te birlikte yapılır; RNG çağrılarının
   sırası aynı kalmalı. Ayrıntı: `../Docs/GELISTIRME.md`.
2. **Deney alanı.** Yeni mekanikler önce burada, `S.exp` (deneysel mod) arkasında denenir; beğenilenler sonra C#'a taşınır.

| Dosya | Ne |
|---|---|
| `sim.js` | Strateji simülasyonu (`globalThis.SonSalter`). Deneysel mod: `newGame(profil, tip, tohum, { exp: { teams, timeScale, startTeams } })` |
| `baskin.js` | Baskın: hücre tabanlı taktik savaş motoru (`globalThis.Baskin`), DOM yok, deterministik |
| `index.html` | Oynanabilir arayüz. Giriş penceresindeki **Deneysel** kutusundan saha ekipleri/baskın ve zaman ölçeği seçilir (varsayılan: açık, ölçek 1,5) |
| `parite.js` | C# parite testlerinin beklenen değerlerini üretir: temel oyun, deneysel mod ve baskın motoru |
| `denge-testi.js` | Denge: her rakibe karşı bot oyuncularla kazanma oranları (deneysel mod kapalı) |
| `baskin-testi.js` | Baskın motoru testi: açgözlü bot her haritayı farklı YZ yetenekleriyle oynar |
| `denge-deney.js` | Deneysel mod dengesi: ekipler + zaman ölçeği + baskın açıkken süre ve kazanma oranları |

## Çalıştırma

```bash
# Oynamak: index.html'i tarayıcıda aç (yerel dosya olarak da çalışır) ya da
python3 -m http.server 8732 --directory Prototype      # sonra http://localhost:8732

JSC=/System/Library/Frameworks/JavaScriptCore.framework/Versions/Current/Helpers/jsc   # macOS; node da olur
$JSC sim.js parite.js
echo 'globalThis.N_GAMES = 200;' > /tmp/n.js && $JSC /tmp/n.js sim.js denge-testi.js
echo 'globalThis.N_RAIDS = 200;' > /tmp/r.js && $JSC /tmp/r.js baskin.js baskin-testi.js
echo 'globalThis.DENGE_NO_RUN = true; globalThis.BASKIN_NO_RUN = true; globalThis.N_GAMES = 1;' > /tmp/pre.js
$JSC /tmp/pre.js sim.js baskin.js baskin-testi.js denge-testi.js denge-deney.js
```

`denge-deney.js` seçenekleri (ön dosyada `globalThis` üzerinden): `N_DENEY` (oyun sayısı), `DENEY_CONFIGS` (denenecek
ayarlar), `NO_RAID` (bot baskın yapmaz), `RAID_P` (baskın sonucunu taktik bot yerine bu başarı oranıyla üret: insan
oyuncu varsayımı).

Tarayıcı konsolu: `SonSalterDebug.S` (strateji durumu), `SonSalterDebug.RB` (süren baskın), `SonSalterDebug.render()`.

## Deneysel mod (Unity'de de var: `Core/Teams.cs`, `Core/Baskin.cs`)

**Kural:** `S.exp` yoksa her kod yolu Unity sürümüyle birebir aynıdır. Deneysel kod yalnızca `S.exp` varken çalışır ve
yalnızca o zaman rastgele sayı tüketir. Değişiklikten sonra `parite.js` çıktısının değişmediğini kontrol et.

- **Zaman ölçeği (`timeScale`, k):** dünya k kat yavaş akar. Günlük oranlar (`per`) k'ya bölünür, süreler (`dur`) k ile
  çarpılır, günlük olasılıklar (`prob`) k'ya bölünür (doğrusal yaklaşım: `pow` sonucu JS ile .NET arasında son basamakta
  farklı çıkabildiği için). Arayüzde bir gün 1,22 kat
  uzun sürer (1×: 1,1 sn). Saha ekiplerinin yolculuk ve görev süreleri ölçeklenmez.
- **Saha ekipleri:** 3 ekip Batı Avrupa'daki karargâhta başlar (en fazla 4; yenisi 45 fon, 4 günde kurulur). Yolculuk
  haritadaki uzaklıkla 1–5 gün. İzleme ağı (ekip 3 gün meşgul) ve denetim (2 gün) için bölgede boşta ekip gerekir.
  Karantina ve Son Şalter siyasi kararlardır, ekip istemez.
- **Baskın:** YZ'nin doğrulandığı (ya da çekirdeğin bilindiği) bölgede boşta ekip varsa 30 fon + 6 SG ile başlar.
  Strateji katmanı baskın bitene kadar durur (`S.raid`).
  - **Başarı:** sızma −40, yama ve saha ölçümü. Bina çekirdeğinse çekirdek yok olur; Yedekleme varsa yedek dolap da
    kırılmalıdır, yoksa YZ yedeğiyle kaçar. İllüzyon sürüyorsa perde yırtılır.
  - **Başarısızlık:** güven −10, sızma +15, YZ +10 hesaplama puanı; bir sonraki baskında bina daha kalabalık olur.
    Çekirdek yerinde kalır.
  - **Her durumda:** kırılan her sivil sunucu bölgeye ekonomi −5 ve güven cezası getirir. Düşen her ekip üyesi ekibi
    4 gün yaralı bırakır; üçü de düşerse ekip kaybedilir.

## Baskın kuralları (`baskin.js`)

- **Alan ve hedef:** 8×8 alan, 3 el yapımı harita (`LAYOUTS`). Hedef: 6 turda (Hızlanma ile 5) hedef dolabı kırmak:
  çekirdek 5 can, düğüm 4; Yedekleme ile çekirdek 4 + yedek 2.
- **Ekip:**
  - **Teknisyen:** sabotaj. Bitişik dolaba 2 hasar; robota 1 hasar ve itme.
  - **EMP uzmanı:** 2–5 kare uzağa topçu atışı. Hedefe 1 hasar, çevresindekileri dışarı iter.
  - **Analist:** kanca. Robotu çeker ya da kendini atar; 3 kare yarıçapındaki robotların niyetini kesinleştirir.
- **Düşmanlar:**
  - **Dron:** düz hat ateşi, 1 hasar.
  - **Bakım robotu:** yakın dövüş 2 hasar ya da hedef dolabı onarır.
  - **Kopya dolapları:** her tur yeni robot uyandırır; dolap kırılırsa durur.
- **Niyetler:** önceden gösterilir ve sırayla uygulanır. İtilen robot saldırısını yeni yerinden yapar. Aldatma ya da Medya
  Kontrolü varsa bazı niyetler yalan gösterilir ("?" rozeti).
- **Hasar kuralları:** Hedef dolaplar zırhlıdır, EMP işlemez. Çarpma (itme) her iki tarafa 1 hasar verir; elektrikli
  kanala itilen 1 hasar alır; soğutma ünitesi kırılınca çevresine 1 hasar verir.
- **Ölçümler** (açgözlü bot, yalnızca bir tur ileriyi görür): çekirdek ~%38, düğüm ~%45, yedekli çekirdek ~%30,
  Kopya + Hızlanma ~%17. Gerçek oyuncunun önizleme ve geri alma ile daha yüksek oranlar tutturması beklenir.

Not: `index.html` acil durum fonu seçicisini henüz göstermiyor (simülasyon destekliyor).
