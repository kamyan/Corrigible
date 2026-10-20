# AYNA · Hareket notları

Fx hareket dili aynen geçerli (`tokens_fx.json › motion`, Fx PROMPT §6). Burada yalnız AYNA’nın eklediği hareketler var.

- Süreler saniye. Yumuşatma aksi yazılmadıkça Fx `easeOut` = `cubic-bezier(.2,.8,.2,1)`.
- **HTML’de**: kare HTML’lerinde bu süreyle oynuyor. **Öneri**: durağan karede gösterilemeyen geçiş; uygulamada bu süreyle.
- PNG’ler 3,8 sn’de alındı; sürekli döngüler Fx `FREEZE_JS` evrelerinde donduruldu.
- Her CSS animasyonunun seçicisi ve değeri: `spec/tokens_ayna.json › motion.cssCatalog`.

---

## 1 · AÇ: iki plan aynı anda açılır (A3)

İki taraf da kilitleyince başlar. Önce kimin planı açılacağı hissi olmasın; ikisi aynı karede başlar.

| t (sn) | Ne | Süre | Durum |
|---|---|---|---|
| 0,0 | Evre plakası: PLANLA ✓ (nane), AÇ etkin + kilit simgesi. Birincil düğme “PLAN KİLİTLİ · planlar açılıyor · girdi kilitli” | 0,2 solma | öneri |
| 0,0 | Altın ve mavi iplikler **aynı anda** uçtan uca çizilir (FxDrawOn, ease-out) | 1,2 | öneri |
| 0,2 | “AÇ · İKİ PLAN AÇILDI” şeridi yukarıdan iner | 0,5 | HTML’de |
| 0,4 | Birlik jetonları rotada kayar | 0,8 | öneri |
| 0,4 | AYNA satırı soldan girer; daktilo 0,5 sn sonra (§4) | 0,6 | HTML’de |
| 0,9 + 0,12·i | Çakışma işaretleri sırayla belirir (ölçek 0,6 → 1) | 0,5 | HTML’de (sırasız) |
| sürekli | İplik akışı (kesik çizgi kayar) | 1,6 döngü | HTML’de |
| sürekli | Sis: “HAREKET ALGILANDI ?” + iki mor halka, 1,2 kaydırmalı | 2,4 döngü | HTML’de |

## 2 · Sürpriz parlaması (A3)

Koşul brifteki gibi: AYNA’nın %15’in altında beklediği hamle, döngü başına bir kez.

| t (sn) | Ne | Süre | Durum |
|---|---|---|---|
| 1,2 | Bölgede 16 ışınlı parlama: ışınlar büyür, sonra söner; beyaz düz + altın kesik halka | 0,35 + 0,8 | öneri (karede son hâli) |
| 1,2 | SÜRPRİZ kartı sıçrar: ölçek 0,6 → 1,06 → 1, `cubic-bezier(.2,1.4,.4,1)` | 0,6 | HTML’de |
| 1,4 | İŞLEM çipi: değer altın parlar, +3 sayarak artar; çip kenarı altın ışımalı kalır | 0,6 / 0,4 | öneri (karede son hâli) |

Savaştaki karşılığı İLK VURUŞ: hazırken dolu altın hap; kullanılınca çerçeveli “İLK VURUŞ kullanıldı” (A6).

## 3 · Okunabilirlik iğnesi

| Ne | Süre | Durum |
|---|---|---|
| Değer değişince çip sayısı sayar | 0,6 ease-out | öneri |
| Yeni nokta çizgiye eklenir, eskiler sola kayar | 0,4 | öneri |
| Son nokta beyaz, nabız (ölçek 1 → 1,4) | 1,2 döngü | öneri |
| A10 / A12 eğrisi çizilerek gelir | 1,8 (0,4 gecikme) | HTML’de |
| Sürpriz yıldızları eğri çizildikten sonra 80 ms arayla | 0,3 | öneri |

## 4 · Tahmin satırı: ele geçirilen kayıt

| Ne | Süre | Durum |
|---|---|---|
| Mavi lamba yanıp söner (steps 1) | 1,2 döngü | HTML’de |
| Daktilo: 40 adım, 0,5 sn gecikme (Fx: 48 adım / 2,2 sn) | 1,8 | HTML’de |
| İmleç 7×13 yanıp söner, satırın sonunda kalır | 0,9 döngü | HTML’de |

- Unity: TMP `maxVisibleCharacters`. Satır kaydırır (en çok 2 satır); kutu yüksekliği büyür. EN satırları TR’den %20’ye kadar uzun olabilir.
- HTML’de daktilo soldan açılan kırpma ile gösterildi (`clip-path`), satır kaydırabilsin diye. Unity’de karşılığı yukarıdaki TMP yöntemi.
- Bölge kartındaki tahmin satırı (A2) daktilo değil; kart açılınca kartla birlikte gelir.

## 5 · Tuzağın açılması (A6)

| t (sn) | Ne | Süre | Durum |
|---|---|---|---|
| 0,0 | Gözcü tuzağa 3 kare yaklaşınca tarama halkası Gözcü’den genişler | 0,6 | öneri |
| 0,4 | Kare: kırmızı kesik çerçeve + dolgu belirir | 0,3 | öneri (karede son hâli) |
| 0,55 | Mavi çeneler açılır | 0,25 | öneri |
| 0,7 | “TUZAK · Gözcü açtı” damgası | 0,35 | öneri |
| — | HEDEF panelinde “Tuzak 1 açık · en fazla 2” sayar | 0,3 | öneri |

Tetiklenince: çeneler 0,12 sn’de kapanır, 10 kırmızı ışın 0,4 sn, “−1” 0,9 sn yükselir ve 0,4 sn’de söner; birim o tur durur (“−1 · durdu”, P3).

## 6 · Düğümün el değiştirmesi

| Ne | Süre | Durum |
|---|---|---|
| Alan tarafın renginde ilerleme yayı, tur başına dolar (Röle tek turda) | 0,5 | öneri |
| Alınınca üstte süzülen simge döner (⬢ → ◆) | 0,3 | öneri |
| Halka rengi geçer | 0,4 | öneri |
| “DÜĞÜM ALINDI” yükselir | 0,9 | öneri |
| HEDEF panelinde düğüm sayıları sayar | 0,3 | öneri |
| A7: alınan düğümlerde altın halka nabzı | 2,0 döngü | HTML’de |

## 7 · Sürekli döngüler

| Öğe | Periyot (sn) | Not |
|---|---|---|
| Küreler | Fx motoru | algoritma ve hızlar aynen |
| Yayılım okları (altın / mavi) | 1,6 | kesik çizgi akar |
| Birlik rotası | 1,4 | |
| Saldırı halkası | 10 | döner |
| Çakışma halkası | 6 | döner; çözülünce durur |
| Senin jeton halkan / AYNA jeton halkası | 6 / 7 (ters) | |
| AYNA niyet çizgisi akışı (tahta) | 0,7 | Fx `intentFlow` gibi, rengi açık mavi |
| Kademe ışığı “eğitimde” | 1,2 | nabız |
| Sis halkaları | 2,4 | iki halka, 1,2 kaydırmalı |
| Tekillik hedef bölgeleri (A11) | 2 + 1,8 | kontur akar + nabız |
| Tekillik ilanı tehlike şeridi | 1,2 | Fx Son Şalter şeridi |
| “SÜRPRİZ OLASI” (A5) | 1,6 | nabız |

## 8 · Giriş sırası (harita kareleri)

Fx’in giriş hareketleri aynen; AYNA’nın yeni öğeleri araya şöyle girer:

| Öğe | Hareket | Süre | Gecikme |
|---|---|---|---|
| Harita | opaklık 0 → 1 | 1,2 | 0 |
| Sol çipler | y −14 → 0 + opaklık | 0,7 | 0 / 0,06 / 0,12 |
| Evre plakası | y −14 → 0 | 0,7 | 0,1 |
| Sağ çipler | y −14 → 0 | 0,7 | 0,06 / 0,12 / 0,18 |
| Asılı etiket (TARAMA AÇIK) | y −14 → 0 | 0,6 | 0,5 |
| Sekmeler | y +16 → 0 | 0,7 | 0,2 |
| Alt sağ grup | y +16 → 0 | 0,7 | 0,22 – 0,3 |
| Düğümler | küre içi büyüme (Fx) | 1,1 | 0,25 + 0,05·i |
| Değer çipleri | y +16 → 0 | 0,6 | 0,5 + 0,05·i |
| Emir rozetleri / bayraklar / etiketler | ölçek / ölçek / opaklık | 0,5 / 0,5 / 0,6 | 0,5 / 0,6 / 0,7 |
| Birlik jetonları | ölçek 0,94 → 1 | 0,6 | 0 |
| Bölge kartı | x +26 → 0 | 0,55 | 0 |
| Çözüm listesi (A4) | x −26 → 0; satırlar 0,1 arayla | 0,6 / 0,5 | 0,2 |

## 9 · Hareketi azalt

- Sürekli döngüler durur (son evrede kalır).
- Daktilo tam metin gösterir.
- Parlama ve sıçrama yerine 0,2 sn solma.
- Sayaçlar sayarak değil, anında değişir.
