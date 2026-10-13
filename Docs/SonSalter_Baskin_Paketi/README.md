# Son Şalter · Baskın — taktik harita (Analist Masası) referans paketi

Baskın taktik haritasının Analist Masası (C) diliyle tasarımı. Kurallar, ekrandaki bilgi ve etkileşim aynı; görünüş masanın dünyasına ait. Tahta 3D ve izometrik kalıyor (ortografik, 35° aşağı, 45° dönük, 8×8).

Akıştaki yeri: masa → onaylı operasyon emri → **taktik harita** → operasyon raporu → masa.

## İçindekiler

| Klasör / dosya | Ne |
|---|---|
| `reference/screens/` | B1–B8 (B7 iki kare: B7a, B7b) · 1920×1080 PNG. **Görsel doğru.** |
| `reference/zoom/` | Her durumun tahta bölgesi @2x (x 404, y 180, 1112×700 → 2224×1400). |
| `reference/sheets/` | `parcalar.png` (3D parçalar), `isaretler.png` (asetat, ok, bayrak, etiket), `paneller.png`, `teknik.png` (kadraj, çapa, katman, hareket). |
| `reference/html/` | Her durum için tek HTML: sabit 1920×1080, mutlak konumlu; fontlar `../../fonts/`. Tarayıcıda açılır. |
| `reference/measure/` | Her durumun ölçü dökümü (TSV): `data-m` kimliği, x, y, w, h, font, boyut, kalınlık, satır yüksekliği, renk, zemin, kenar, köşe, gölge, dönüş, metin. **Sayısal doğru.** |
| `spec/raid_tokens.json` | Yeni tokenlar (C paketindeki `spec/tokens.json`'a ek): kamera, ışık, 3D malzeme renkleri, işaret renkleri (alfa ile), dünya etiketleri, düzen, çapalar, hareket süreleri, metin anahtarları. |
| `spec/board_states.json` | B1–B8 tahta verisi: ızgara, birim/robot konumları, can, niyetler (tür, yön, sıra, kesinlik, durum), işaretler, hayaletler, günlük satırları. Unity'de sahte veri olarak kullanılabilir. |
| `spec/strings_tr_en.csv` | Arayüz metinleri: Türkçe + İngilizce öneri + en geniş alan (px). |
| `fonts/` | Courier Prime, Libre Franklin, Kalam (woff2, OFL) — yalnız HTML'leri açmak için. Unity TMP font varlıkları C paketindekiler (DejaVu yedeğiyle). |

## Masa metaforu

- **Tahta:** analistin masasında, yeşil çuha üstünde duran operasyon maketi. Taban kontrplak (kenarı görünür), üstü **ozalit** kat planı: açık mavi-gri kâğıt, mavi-mor çizgi, 8×8 kare, çift çerçeve, ölçü çentikleri, batı kenarında giriş kapıları.
- **Duvar:** gri mukavva blok. **Dolaplar:** boyalı tahta bloklar; tür rengi ve üstteki işaretle okunur — YZ düğümü (koyu, kırmızı düğüm simgesi, çelik zırh şeritleri), çekirdek (“Ç” rozeti), yedek çekirdek (kesik “Ç”), sivil sunucu (krem + mavi), soğutma ünitesi (silindir + fan + uyarı etiketi), kopya dolabı (koyu kırmızı + **ayna** paneli).
- **Ekip:** altlıklı krem karton figürler, siyah Courier harf. **Makineler:** boyalı metal minyatürler, kırmızı altlık halkası ve mercek. Ekip ile makineler malzemeden bir bakışta ayrılır.
- **İşaretler:** asetat üstüne yağlı kalem — mavi hareket, yeşil yetenek hedefi, kırmızı tehlike, turuncu şüphe, siyah üstüne gelme.
- **Niyetler:** kırmızı kalem okları + iğneli kâğıt sıra bayrağı. Uyanma: kırmızı raptiye ve halka.
- **Işık:** tek masa lambası (sıcak, yumuşak gölge) + soğuk dolgu. Glow, bloom ve emissive yok.
- **Paneller:** ekip dizin kartları (C1 HEDEF kartı), hedef formu, teleks rulosu operasyon günlüğü (YZ satırları C3'ün ele geçirilmiş günlük şeridi), ozalit lejant fişi, katlanabilir kurallar kartı (ayrı brifing penceresinin yerine), ahşap şeritte bilgi satırı ve düğmeler.

## Güven dili tahtada

| Durum | Görünüş |
|---|---|
| Kesin niyet | düz kırmızı ok, kırmızı kenarlı bayrak, hedef karede kırmızı tarama + düz çerçeve |
| Yalan olabilir (Aldatma) | kesik ok, turuncu kesik kenarlı bayrak + halkalı Kalam “?”, hedef karede turuncu kesik çerçeve |
| Tarandı, kesin | düz ok, mavi kenarlı bayrak + mavi ONAYLANDI mührü (✓); tarama alanı mavi noktalı sınır |
| Durum renkleri | mürekkep + simge + kelime: kırmızı tehdit, turuncu uyarı, mavi bilgi/hareket, yeşil iyi/hedef |

## Durumlar

| Kare | Ne gösteriyor | Senaryo |
|---|---|---|
| B1 | Turun başı: niyetler (4 ateş, 2 onarım), aynı kareye iki niyet (EMP uzmanı), iki uyanma karesi, kurallar kartı açık | Tur 3/6 · Yedekleme |
| B2 | Teknisyen seçili: 7 erişilebilir kare, sabotaj hedefi (soğutma ünitesi), seçili kart | aynı an |
| B3 | EMP önizlemesi: dolapta ZIRH, bakım robotunun hayaleti + itme oku, engellenen itme (çarpışma, −1/−1), geçerli EMP hedefleri, nişan ayracı | aynı tur |
| B4 | Aldatma: tarama alanı dışındaki iki “yalan olabilir” niyet ile içindeki iki “tarandı” niyet yan yana; üstüne gelinen dron bilgi satırında | Tur 4/6 · Aldatma + Kendini Kopyalama |
| B5 | Makineler hareket ediyor: girdi kilitli, sıra 3 vurgulu, 1–2 uygulandı, 4 iptal, günlük akıyor, figürler tur bitti yüzünde | Tur 3/6 |
| B6 | Zorlu an: SON TUR, düşmüş EMP uzmanı, yan hasar 1, kazandıran sabotajda HEDEF KIRILIR önizlemesi | Tur 6/6 |
| B7a / B7b | Sonuç anı: kararma + spot, büyük damga, sonuç fişi (rapora geç: Enter) | Tur 6/6 |
| B8 | Geri çekilme 2. onay: düğme kırmızı “Emin misin? Tekrar bas”, onay fişi, ekran %32 kararır | Tur 3/6 |

## Kurallarla ilgili varsayımlar (kontrol edin)

Kural eklenmedi ya da çıkarılmadı; brifin söylemediği yerlerde çizim şu varsayımlarla yapıldı:

1. **Kanal kareleri yürünebilir** çizildi (yalnız üstüne itilen ya da düşen hasar alır). Oyunda yürünemiyorsa B2'deki iki mavi kare düşer.
2. **Tarama alanı** çizimde Manhattan uzaklığı ≤ 3. Oyunun hesabı neyse sınır onu izler.
3. **Uyanma kareleri** kopya dolaplarının yanında, her tur 2 tane çizildi; sayı ve yer oyunun hesabıdır.
4. **İtilen robot** saldırısını yeni yerinden **aynı yöne** yapar (B5'te itilen bakım robotunun onarımı boş kareye düşüyor).
5. **EMP geçerli hedefleri** çizimde duvar kareleri hariç; düşmüş birimin ateşi durdurup durdurmadığı belirsiz olduğu için çizimlerde bu durumda niyet yok.
6. **Enkazın** geçilebilirliği mevcut kurala göre; görünüş yalnız “kırıldı” bilgisini taşır.
7. Brifte adı geçen `baskin_brifing.png`, `baskin_sonuc.png` ve `F9_baskin_mevcut.png` bu çalışmaya eklenmemişti; aile C1–C5'e göre kuruldu.

## Unity'ye aktarım notları

- **Kamera:** ortografik, rotasyon (35°, 45°, 0). `orthographicSize = (Screen.height / canvasScale) / 176.777` (1080p'de 6,109). Izgara ortası, panellerin arasındaki görüş alanının ortasına gelecek şekilde kamera kendi sağ/yukarı ekseninde kaydırılır (`teknik.png` · A).
- **Izgara → dünya:** x = sütun, z = −satır. 1080p'de ekran: `x = 460 + (u+v)·62,5`, `y = 528 + (v−u)·35,85 − z·72,41` (u = sütun, v = satır; kare merkezi +0,5).
- **Katman sırası:** 3D sahne → asetat vurguları (Unlit/Transparent, ZWrite kapalı) → hayaletler (α .42) → oklar, itme okları, ayraçlar, raptiye (ZTest Always) → dünya etiketleri (ekran Canvas'ı, izdüşüm) → paneller (Overlay) → kararma/sonuç fişi.
- **Asetat işaretleri:** quad + 4 şerit çerçeve; tarama tek döşenen doku (karo uzayında; mavi ↗, kırmızı ↘). Renk ve alfa: `raid_tokens.json › marks`.
- **Karton figür:** tek quad (ön: krem kart, arka: kraft) + 2 px kalınlık şeridi; tur bitince Y ekseninde 180°, düşünce geriye devrilir.
- **Paneller:** C paketinin sprite'ları (dizin kartı, krem kâğıt, teleks, damga, tuş fişi, mürekkep düğme) yeterli; yeniler: ozalit lejant fişi (#D3DBD6 + #2C4C8E kenar), sonuç fişi (krem kâğıt + çift çizgi başlık), kırmızı kesik kenarlı onay fişi.
- **Çapalar:** sol sütun sola, sağ sütun sağa, günlük dikey gerilir, alt grup (bilgi satırı + Geri al + Yetenek = 1104 px) görüş alanının ortasına (`teknik.png` · B).
- **Metin:** Türkçe büyük harf `tr-TR` ile (TMP UpperCase kullanma); eksi işareti U+2212; en küçük 11 px; Kalam yalnız “?” ve el notu (en az 15 px).
- **Hareket:** süreler öneri (`teknik.png` · D, `raid_tokens.json › motion`).

## Fontlar

Courier Prime, Libre Franklin ve Kalam SIL Open Font License 1.1 ile dağıtılır (`fonts/LICENSE-*.txt`).
