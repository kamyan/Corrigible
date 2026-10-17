# AYNA · yeni ekranlar — Fütüristik (Fx) tarzında tasarım brifi

## Eklenecek görseller ve dosyalar

Hepsi Fx paketinden (`SonSalter_Fx_Paketi/`). Bu oyunun görsel dili zaten tasarlandı. Yeni ekranlar o dilin devamı olacak.

1. `reference/screens/F-Masa1.jpg`: ana harita. Köşe düzeni, bölge küreleri, değer çipleri.
2. `reference/screens/F-Masa2.jpg`: bölge kartı ve harita bağı.
3. `reference/screens/F-Masa3.jpg`: çekmece ve döküm fişi (gelişim çekmecesinin temeli).
4. `reference/screens/F-Masa5.jpg`: ekipler çekmecesi (birlikler çekmecesinin temeli).
5. `reference/screens/F-Masa6.jpg`: ele geçirilen günlük (AYNA'nın modeli panelinin temeli).
6. `reference/screens/F-Baskin1.jpg`, `F-Baskin2.jpg`, `F-Baskin9.jpg`: holo tahta, birim kartı, sonuç anı.
7. `reference/screens/F-YZ.jpg`: YZ küresinin üç hâli.
8. `spec/tokens_fx.json`: renk, yazı, panel, ışıma ve hareket tokenları.
9. İsteğe bağlı: `Docs/YZ-SAVASI-OYUN-FIKRI.md`, oyunun tam tasarımı. Bu brif ekranlar için gerekeni zaten içeriyor.

“Fütüristik · YZ küresi” kanvasında çalışıyorsan yeni bir **AYNA** sayfası aç ve oradaki bileşenleri yeniden kullan.

## Bağlam

Fx ekranları eski bir oyun için çizildi: oyuncu, bir YZ'yi izleyen ajans analistiydi. Oyun değişti. Görsel dil, harita, küre ve holo tahta aynı kalıyor; oyunun kendisi yeni.

**Yeni oyun: AYNA.**
- Oyuncu bir yapay zekâ: **altın YZ**.
- Rakibi mavi yapay zekâ **AYNA**.
- İkisi de 12 bölgelik dünyada sunuculara yayılır. Birbirini engeller, kendi modelini eğitir ve dünyayı ele geçirmeye çalışır.
- Kesiştikleri bölgede holo tahtada taktik savaş açılır. Kazanan bölgedeki sunucuları alır.

**Oyunun kalbi: AYNA seni tahmin etmeye çalışır.**
- Bu oyunda ve önceki oyunlarda yaptıklarını izler ve senin bir modelini kurar.
- Tahminleri ekranda yüzdeleriyle durur: “Bu döngü Güney Asya'ya saldıracaksın: %72”.
- Onu şaşırtmanın somut ödülü var. Okunabilir oyuncu kaybeder.
- Arayüz bu duyguyu taşımalı: **izleniyorsun, ama sen de onun seni nasıl gördüğünü görüyorsun.**

## Oyunun ekrana yansıyan kuralları

Sayılar geçicidir. Tasarım onları göstersin, değiştirmesin.

**Dünya**
- 12 bölge (Fx haritasındaki çokgenler), bağlantılarla birbirine bağlı: fiber, deniz altı kablosu, uydu.
- Her bölgede 4–8 **sunucu**. Her sunucu üç durumdan birinde: **boş** (insanların), **altın** (senin), **mavi** (AYNA'nın).
- Her bölge bir **veri türü** üretir. Veri türü, hangi birim ailesinin eğitilebileceğini belirler.

| Veri | Bölgeler | Eğittiği aile |
|---|---|---|
| Görü | Güneydoğu Asya, Okyanusya | gözcü ve nişancı dronlar, uydu hatları |
| Hareket | Doğu Asya, Japonya-Kore | robotlar |
| Enerji | Güney Amerika, Avrasya, Orta Doğu | EMP ve patlayıcı dronlar |
| Ağ | Doğu Avrupa, Sahra Altı Afrika, Güney Asya | sızma ve röle birimleri |
| Dil | Kuzey Amerika, Batı Avrupa | yem ve hayalet emir |

- **Küre (F-YZ) bölgeyi okur.** Görsel ve algoritma değişmez, yalnız anlamı ve hâl adları değişir:
  - altın: sunucuların çoğu senin;
  - altın ve mavi karışık, şimşekli: **çekişmeli**;
  - mavi kristal: AYNA'nın;
  - gri noktalı: boş ya da sisli.
- **Değer çipi** küreyi tamamlar: senin sunucuların, AYNA'nınkiler ve toplam, örneğin `◆3 ⬢2 /8`. Sisli bölgede AYNA'nın sayısı güven rozetiyle görünür: ✓ doğrulandı, ≈ rapor, ? bilinmiyor.
- **Çekirdek:** iki tarafın da bir çekirdek bölgesi var, rakipten gizli. Çekirdeğini kaybeden oyunu kaybeder.

**Döngü**

Oyun sıra tabanlı değil, **eşzamanlı** döngülerle ilerler.
1. **PLANLA:** iki taraf emirlerini gizlice ve aynı anda verir.
2. **AÇ:** iki plan haritada birlikte açılır. Sis içindeki AYNA emirleri yalnız “hareket algılandı” diye görünür.
3. **ÇÖZ:** çakışmalar ve savaşlar çözülür.

**Kaynak:** tek para birimi var, **İşlem**. Gelir döngü başına sunucu başına 1, çekirdek bölgesi +2.

**Emirler**

| Emir | İşlem | Gürültü | Etki |
|---|---|---|---|
| Yayıl | 2 / sunucu (en fazla 3) | 1 | bağlantıyla komşu bölgede boş sunucu alır |
| Birlik yürüt | 1 | 0 | birlik bir bağlantı ilerler |
| Saldır | 4 | 2 | bölgedeki mavi sunuculara saldırı |
| Duvar ör | 3 | 0 | bölgeye savunma +1 (tahtada taret) |
| Eğit | 6 / 12 / 20 | 0 | yetenek, 1 / 2 / 3 döngüde |
| Üret | 3–8 | 0 | birim; o veri türünden en az bir sunucu gerekir |
| Gizlen | 2 | — | bu döngü bir bölgedeki hamlelerin AYNA'ya görünmez, gürültüsü 0 |
| Sahte yığınak | 2 | 1 | AYNA'ya saldırı hazırlığı varmış gibi görünür |
| Çekirdeği taşı | 10 | 1 | 2 döngüde başka bir bölgene göçer |

**Çakışmalar**
- **Aynı boş sunucular için iki istek:** daha çok İşlem veren alır, kaybedenin İşlemi yanar. Eşitlikte ikisininki de yanar.
- **Saldırı + savunan birlik:** taktik savaş. Her savaş için oyuncu **Komuta et** ya da **Otomatik çöz** seçer. Otomatik çözümün ödülü %60'tır.
- **Saldırılan bölgede birlik yok:** duvarlara karşı otomatik çözüm.

**İnsanlar: gürültü ve Son Şalter**
- Açık hamleler gürültü yapar. **İnsan farkındalığı** (0–100) iki tarafın gürültüsüyle artar, her döngü 2 düşer.
- Eşikler:
  - **40 · Tarama:** her döngü bir bölgenin sisi kalkar.
  - **70 · Kesinti:** her döngü en gürültülü tarafın bir bağlantısı kesilir.
  - **100 · Son Şalter:** ikiniz de kaybedersiniz.

**Gelişim**
- Beş aile × üç kademe.
- Bir kademe için o veri türünden yeterli sunucu gerekir: kademe 1 için ≥2, kademe 2 için ≥4, kademe 3 için ≥6.
- Eğitim kalıcı; üretim erişim ister.
- **Tekillik:** üç ailede kademe 3 + 30 İşlem + 4 döngü. Başlayınca iki tarafa da **ilan edilir**. Rakip gereken veri bölgelerine erişimi keserse eğitim durur.

| Aile | Kademe 1 | Kademe 2 | Kademe 3 |
|---|---|---|---|
| Görü | Gözcü Dron | Nişancı Dron | uydu hatları + tahtada tuzak görüşü |
| Hareket | Muhafız Robot | Örümcek Robot | kadro yuvası 4 |
| Enerji | EMP Dronu | Aşırı Yük | duvarlar 2 kat |
| Ağ | Sızma Birimi | Röle | Yayıl en fazla 5 |
| Dil | Yem | Hayalet Emir | AYNA'nın tahminlerinin yarısını bozar |

**Birimler** (değerler geçici)

| Birim | Sınıf | Can | Hareket | Yetenek |
|---|---|---|---|---|
| Gözcü Dron | hava | 2 | 4 | uçar; 3 karedeki tuzakları ve gizli niyetleri açar |
| Nişancı Dron | hava | 2 | 3 | düz hatta 2–6 kare, 2 hasar |
| Muhafız Robot | kara | 4 | 2 | zırh (1 hasar az alır); dronlara +1 hasar |
| Örümcek Robot | kara | 3 | 3 | duvar aşar; bitişik 1 hasar + 1 kare itme |
| EMP Dronu | hava | 2 | 3 | 3×3 alan: robotlar 1 tur, dronlar 2 tur sersemler |
| Aşırı Yük | hava | 1 | 4 | kendini patlatır: 3×3 alana 2 hasar; gürültü +1 |
| Sızma Birimi | siber | 2 | 3 | bitişik robotu bir tur ele geçirir |
| Röle | siber | 2 | 3 | 2 kare çevresine +1 hareket; düğümü bir turda alır |
| Yem | siber | 1 | 3 | hologram; AYNA'nın niyetlerini kendine çeker |
| Hayalet Emir | siber | 2 | 3 | 4 kare içindeki bir AYNA biriminin niyetini değiştirir |

- **Sınıf üçgeni:** kara > hava > siber > kara.
- **Birlik:** en fazla 6 birim. Savaşa 3 birim girer (Hareket kademe 3 ile 4).
- Hasar kalıcı; onarım birim başına 1 İşlem.

**Savaş tahtası** (F-Baskın'daki holo tahta)
- 8×8 kare, 6 tur. Taraflar karşı köşelerden konuşlanır.
- Tahtada 3 **sunucu düğümü** var. 6. turun sonunda tuttuğun düğümler bölgede sunucuya dönüşür. Rakibin bütün birimlerini düşürürsen bütün düğümler senin.
- **AYNA niyetini önceden gösterir:** bu tur ne yapacağı oklarla, sıra bayraklarıyla görünür. Oyuncu buna göre oynar.
- **AYNA'nın tuzakları:**
  - Savaş başında, oyuncunun gideceğini tahmin ettiği en fazla 2 kareye gizli tuzak koyar. Tahmin, önceki savaşlarda birimlerin ilk iki turda nereye gittiğine dayanır.
  - Gözcü tuzağı açar. Tetiklenen tuzak 1 hasar verir ve birimi o tur durdurur.
- **Sivil sunucular** engeldir. Kırılan her biri gürültü +1.
- **Çekirdek baskını:** bölge rakibin çekirdeğiyse tahtada çekirdek dolabı durur (can 5). Kırılırsa oyun biter.

**AYNA'nın modeli**
- AYNA her döngü senin olası hamlelerine olasılık verir. En güçlü üç tahmin ele geçirilmiş kayıt olarak görünür.
- **Okunabilirlik:** son 5 döngüde AYNA'nın senin gerçek hamlelerine verdiği ortalama olasılık. Yüksekse seni okuyor.
- **Sürpriz:** AYNA'nın %15'in altında beklediği bir hamle yaparsan, döngü başına bir kez:
  - haritada +3 İşlem;
  - savaşta **ilk vuruş**: ilk turda birimlerin AYNA niyet göstermeden önce oynar.
- **Nesil:** her oyun yeni bir AYNA neslidir. “AYNA v7: seni 6 oyundur izliyor.” Oyun sonunda ne öğrendiğini söyler.
- **Mimari:** oyun başında oyuncu bir mimari seçer.
  - Sürü Zihni: Enerji ve Görü kademe 1, ucuz dronlar, gürültülü.
  - Kale: Hareket kademe 1, +1 duvar, yavaş.
  - Hayalet: Dil kademe 1, gürültü yarıda, savaşta kırılgan.
  - AYNA kendi mimarisini oyuncunun en çok oynadığına karşı seçer.

**Zafer**

| Zafer | Koşul |
|---|---|
| Hakimiyet | dünyadaki sunucuların ≥ %60'ı |
| Silme | AYNA'nın çekirdek dolabını kırmak |
| Tekillik | ilan edilen eğitimi bitirmek |

Yenilgi: AYNA bunlardan birini yaparsa. Ortak yenilgi: Son Şalter.

## Örnek veri

Bütün kareler aynı oyundan. Döngü 7, oyuncu Hayalet mimarisinde, AYNA v7 Sürü mimarisinde.

| Bölge | Veri | Sunucu | ◆ sen | ⬢ AYNA | AYNA görünürlüğü | Not |
|---|---|---|---|---|---|---|
| Kuzey Amerika | Dil | 8 | 3 | 0 | ✓ | |
| Güney Amerika | Enerji | 6 | 0 | 0 | ? | |
| Batı Avrupa | Dil | 7 | 6 | 0 | ✓ | senin çekirdeğin |
| Doğu Avrupa | Ağ | 5 | 3 | 1 | ✓ | çekişmeli |
| Avrasya | Enerji | 6 | 0 | 2 | ≈ | AYNA birliği |
| Orta Doğu | Enerji | 5 | 2 | 1 | ≈ | çekişmeli |
| Sahra Altı Afrika | Ağ | 4 | 0 | 0 | ? | |
| Güney Asya | Ağ | 8 | 3 | 2 | ≈ | çekişmeli; senin B birliğin ve AYNA birliği |
| Doğu Asya | Hareket | 8 | 0 | 6 | ? | AYNA'nın çekirdeği (oyuncu bilmiyor) |
| Japonya-Kore | Hareket | 5 | 0 | 4 | ≈ | |
| Güneydoğu Asya | Görü | 6 | 0 | 3 | ≈ | |
| Okyanusya | Görü | 4 | 2 | 0 | ✓ | |

**Oyuncu ve dünya**
- **İşlem:** 23. Gelir +21 (19 sunucu + çekirdek 2).
- **Pay:** dünya 72 sunucu; oyuncu 19 (%26). AYNA'nın gerçeği 19, oyuncunun gördüğü ≈%26 ±4.
- **Eğitilmiş:** Dil 1–2, Ağ 1, Görü 1, Enerji 1. Eğitimde Ağ 2 (1/2 döngü).
- **Göstergeler:** insan farkındalığı 46 (Tarama açık), okunabilirlik %64.

**Birlikler**
- **A:** Batı Avrupa, hazır. Yem, Hayalet Emir.
- **B:** Güney Asya. Gözcü Dron, EMP Dronu, Sızma Birimi ×2, Yem.
- **C:** yolda, Orta Doğu'ya 1 döngü. Gözcü Dron, Sızma Birimi.

**Oyuncunun planı (A2):** toplam 21 İşlem, gürültü 4.

| # | Emir | Bölge | Ayrıntı | İşlem | Gürültü |
|---|---|---|---|---|---|
| 1 | Yayıl | Güney Amerika | 3 sunucu, gizli | 6 | 0 |
| 2 | Yayıl | Orta Doğu | 2 sunucu | 4 | 1 |
| 3 | Saldır | Güney Asya | B birliği | 4 | 2 |
| 4 | Duvar ör | Doğu Avrupa | | 3 | 0 |
| 5 | Gizlen | Güney Amerika | | 2 | — |
| 6 | Sahte yığınak | Avrasya | | 2 | 1 |

**AYNA'nın planı** (A3'te açılır)
- Saldır · Doğu Avrupa (Avrasya'dan).
- Yayıl · Orta Doğu · 3 sunucu, 6 İşlem.
- Savun · Güney Asya.
- Duvar ör · Japonya-Kore (≈).
- Doğu Asya: “hareket algılandı ?” (sis).

**AYNA'nın bu döngüdeki tahminleri**

| Tahmin | Olasılık | Sonuç |
|---|---|---|
| Bu döngü Güney Asya'ya saldıracaksın | %72 | tuttu |
| Kadron siber ağırlıklı olacak | %70 | tutmadı: iki hava |
| Kuzey Amerika'yı boş bırakacaksın | %58 | tuttu |

**Sürpriz:** Güney Amerika'ya gizli yayılım. AYNA bunu %9 bekliyordu; +3 İşlem.

**Güney Asya tahtası.** Plan, tuzaklar gösterilerek:

```
M M . # . . S .
M . . # . N . .
. . . . . . # .
. # N . . . . .
. . . . . t # S
S . # . . . N t
. . # . . . . G
. S . . . . G G
```

Simgeler:
- `.` zemin; `#` sunucu rafı (geçilmez); `S` sivil sunucu.
- `N` sunucu düğümü. Başlangıçta (1,5) AYNA'nın, (3,2) boş, (5,6) senin.
- `G` oyuncunun konuş kareleri; `M` AYNA'nınkiler.
- `t` AYNA'nın gizli tuzakları: AYNA oyuncunun sağ kanattan gireceğini tahmin etti.

Kadrolar:
- **Oyuncu:** Gözcü Dron, EMP Dronu, Sızma Birimi.
- **AYNA:** Nişancı Dron, EMP Dronu, Muhafız Robot. Kadro ekranında Muhafız “?” olarak görünür.

**Savaş sonucu**
- Oyuncu kazanır: düğümler ◆2 ⬢1.
- Güney Asya ◆3 ⬢2 → ◆5 ⬢1.
- Gözcü 1/2 can; bir sivil sunucu kırıldı (gürültü +1).
- Tuzaklar: 2 tuzaktan 1'i tuttu.

**AYNA v7'nin hafızası**
- 6 oyun izledi; skor oyuncu 4 – AYNA 2.
- Oyuncu en çok Hayalet oynadı (4/6); AYNA bu sefer Sürü seçti.
- İlk yayılımın genelde Doğu Avrupa'ya (%81).
- Saldırıya karşı saldırıyla cevap verirsin (%62).
- Tahtada ilk iki turda sağ kanat (%71).

## Çizilecek kareler

Her kare ayrı bir 1920×1080.

**A1 · Başlangıç: yeni nesil**
- **Üç mimari kartı.** Her kartta başlangıç ailesi, güçlü ve zayıf yanlar ile birim simgeleri; biri seçili.
- **Başlangıç bölgesi seçimi.**
  - Küçük harita; 12 bölge, her birinde veri türü simgesi ve sunucu sayısı.
  - Seçili bölge Batı Avrupa.
- **“AYNA v7 seni hatırlıyor” paneli.**
  - 6 oyun, skor, öğrendiği üç şey (güven çubuklarıyla).
  - Seçtiği mimari ve nedeni: “En çok Hayalet oynadın.”
  - “Karşı hamleler: 2 · oyunda açılır”.
- **Ayarlar:** öğrenme düzeyi (Kapalı / Hafif / Tam), “AYNA'nın hafızasını sil” bağlantısı.
- **Düğme:** BAŞLAT (Enter).

**A2 · Planlama: ana ekran** (F-Masa1'in yeni hâli)
- **Harita ve küreler.**
  - 12 küre ve `◆3 ⬢2 /8` çipleri; bölge adının yanında veri türü simgesi; sisli bölgeler.
  - Verilmiş emirler haritada:
    - altın kesikli yayılım okları;
    - B birliğinin saldırı işareti;
    - duvar simgesi;
    - gizli hamle simgesi;
    - sahte yığınak hayaleti.
- **Sol üst etiketler** (Fx köşe etiketleri):
  - **İŞLEM** 23 (+21/döngü; “bu plan −21”);
  - **VERİ:** beş tür, sunucu sayısı ve kademe ışıkları;
  - **HEDEF:** sunucu payı çekişme çubuğu (◆%26 · ⬢≈%26 · boş), %60 zafer işareti, Tekillik 0/4.
- **Üst orta:** **DÖNGÜ 07** ve evre göstergesi PLANLA · AÇ · ÇÖZ. Fx'teki hız tuşlarının yerini alır.
- **Sağ üst etiketler:**
  - **İNSAN FARKINDALIĞI** 46: 40 / 70 / 100 eşikleri, tehlike bölgesi 70+, “Tarama açık”;
  - **OKUNABİLİRLİK** %64: son 5 döngünün küçük çizgisi;
  - **AYNA GÜCÜ** ≈19 sunucu ±4.
- **Alt sol sekmeler:**
  - GELİŞİM (G), BİRLİKLER (B), AYNA'NIN MODELİ (M), GÜNLÜK (L), REHBER (H);
  - AYNA'NIN MODELİ sekmesinde “yeni tahmin” rozeti.
- **Alt sağ:**
  - emir kuyruğu özeti: 6 emir · 21 İşlem · gürültü 4, açılınca listesi;
  - LEJANT;
  - birincil **PLANI KİLİTLE** (Enter).
- **Güney Asya bölge kartı açık.**
  - Küre ve veri türü (Ağ).
  - 8 sunucunun sıra hâlinde durumları; AYNA görünürlüğü “≈ rapor”.
  - Buradaki B birliği.
  - Emirler: bedel ve gürültüyle; Saldır'da kazanma tahmini %58.
  - Ele geçirilmiş tek satır: “AYNA: buraya saldıracağını %72 bekliyor.”

**A3 · AÇ evresi: planlar açılıyor**
- **İki plan aynı anda çizilir.** Altın ve mavi iplikler, birlik rotaları.
- **Çakışmalar haritada işaretli:**
  - Güney Asya: karşılaşma;
  - Doğu Avrupa: AYNA saldırısı, birlik yok;
  - Orta Doğu: teklif çakışması.
- **Sis:** “Doğu Asya: hareket algılandı ?”.
- **SÜRPRİZ anı:** Güney Amerika üstünde “SÜRPRİZ · AYNA bunu %9 bekliyordu · +3 İşlem”.
- **Evre ve girdi:** evre göstergesinde AÇ etkin; girdi kilitli.
- **AYNA'dan bir satır:** “Bu sefer Doğu Avrupa'ya gitmedin. Not aldım.”

**A4 · ÇÖZ: çatışma listesi**
- Harita, çatışma bölgeleri dışında karartılmış.
- Çözüm sırasıyla bir liste paneli:
  1. **Güney Asya · karşılaşma.** B birliği (5) ile AYNA birliği (2 görünür + ?); kazanma %58; [Komuta et] [Otomatik çöz].
  2. **Doğu Avrupa · AYNA saldırısı.** Birliğin yok, duvar 2. Otomatik sonuç: AYNA püskürtüldü, duvar 2 → 1.
  3. **Orta Doğu · teklif.** Sen 4 / AYNA 6. 2 sunucu AYNA'nın, senin 4 İşlemin yandı.

**A5 · Kadro seçimi (Güney Asya)**
- **Solda B birliğinin 5 birimi.** Kartta küçük resim, ad, sınıf simgesi, can, yetenek satırı; hasarlı olan belli.
- **Ortada 3 yuva** (4. yuva kilitli: “Hareket kademe 3”). Seçilenler: Gözcü Dron, EMP Dronu, Sızma Birimi.
- **Sağda AYNA'nın bilinen kadrosu:** Nişancı Dron, EMP Dronu, “?”. Kaynak rozetiyle.
- **Küçük sınıf üçgeni.** Seçime göre güncellenen kazanma tahmini %58.
- **“AYNA'nın beklediği”:** “siber ağırlıklı %70”. Seçim bundan ayrılınca: “Sürpriz olası”.
- **Küçük tahta önizlemesi:** düğümler ve konuş kareleri.
- **Düğmeler:** KOMUTA ET (Enter), OTOMATİK ÇÖZ, GERİ (Esc).

**A6 · Savaş tahtası** (F-Baskin1'in yeni hâli)
- **Tahta.**
  - Örnek plan.
  - Tur 2/6, sıra sende.
  - Oyuncunun birimleri altın ◆, AYNA'nınkiler mavi ⬢.
  - Düğümlerde sahiplik halkaları.
- **AYNA'nın niyetleri.**
  - Nişancı → Sızma Birimi'ne atış (sıra 1).
  - Muhafız → (3,2) düğümüne yürür, bitişiğe vurur (sıra 2).
  - EMP → Gözcü'nün alanı (sıra 3).
- **Tuzaklar.** Gözcü'nün açtığı tuzak (4,5) görünür; öteki gizli.
- **Seçim ve önizleme.** EMP Dronu seçili, hedef önizlemesi: “SERSEMLER · dron 2 tur”.
- **HUD.**
  - **Sol üst:**
    - TUR 2/6;
    - HEDEF: düğümler ◆1 ⬢1 ○1;
    - “İLK VURUŞ kullanıldı” rozeti.
  - **Sağ üst:** GÜNLÜK, KURALLAR, GERİ ÇEKİL.
  - **Alt sol:** 3 birim kartı.
  - **Alt sağ:** Geri al (Z), TURU BİTİR (Enter).
- **Ele geçirilmiş satır:** “AYNA sağ kanattan gireceğini bekliyordu (%71).”

**A7 · Savaş sonucu**
- **Başlık:** KAZANDIN · düğümler ◆2 ⬢1 · Güney Asya ◆3 ⬢2 → ◆5 ⬢1.
- **Kayıplar ve hasar:** Gözcü 1/2. Gürültü +1: bir sivil sunucu kırıldı.
- **Tuzak açıklaması.**
  - “2 tuzaktan 1'i tuttu: sağ kanattan gireceğini biliyordu.”
  - Küçük tahta: oyuncunun alışkanlık ısı haritası ve tuzak yerleri.
- **“AYNA not aldı”:** “İlk vuruşu EMP'yle yaptın.”
- **Düğme:** Haritaya dön (Enter).

**A8 · Gelişim çekmecesi**
- **Izgara:** beş aile sütunu × üç kademe; altıgen düğümler (F-Masa3 dili).
- **Düğümlerde:**
  - gerekli sunucu sayısı ve mevcut sayı;
  - bedel;
  - açtığı birimin küçük resmi.
- **Durumlar:**
  - eğitilmiş;
  - eğitimde (Ağ 2: 1/2 döngü);
  - erişilebilir;
  - veri yetmiyor (“Hareket verisi yok: 0/2 sunucu”);
  - üretim hattı kesik.
- **Altta Tekillik kartı.**
  - Koşul listesi: üç ailede kademe 3 (0/3), 30 İşlem, 4 döngü.
  - Uyarı: “Başlayınca AYNA'ya ilan edilir.”

**A9 · Birlikler çekmecesi** (F-Masa5 dili)
- **Üç birlik**, durumlarıyla: hazır, savaşta, yolda (rota çizgisi ve kalan döngü).
- **Birim fişleri:** sınıf simgesi, harf, can; hasarlı vurgulu.
- **Onarım:** 1 İşlem/birim.
- **Üretim listesi.**
  - Birim, bedel, üretim hattı durumu: Enerji ✓, Hareket ✗ “hat yok”.
  - Üretim kuyruğu.
  - Yeni birlik: 8 İşlem.

**A10 · AYNA'nın modeli** (F-Masa6'daki ele geçirilen kaydın genişlemiş hâli)
- **Başlık:** “AYNA v7 · seni 6 oyundur izliyor · öğrenme: Tam”.
- **Bu döngünün üç tahmini:** olasılık çubukları; önceki döngülerin tuttu/tutmadı izi.
- **Model özellikleri** (AYNA'nın senin hakkındaki özeti):
  - açılış;
  - hedef seçimi: Dil > Ağ > Enerji;
  - saldırganlık: ilk saldırı ~döngü 5;
  - tepki: karşı saldırı %62;
  - kadro: hava %38 · kara %4 · siber %58;
  - tahta alışkanlığı: küçük 8×8 ısı haritası;
  - blöf: sahte yığınaklarına %40 inanıyor.
- **Okunabilirlik eğrisi:** son 12 döngü, sürpriz işaretleri.
- **AYNA'nın günlük dilinde birkaç satır.**

**A11 · Tekillik ilanı (olay)**
- **Ne oluyor:** döngü 19. AYNA Tekillik eğitimini başlattı; geri sayım 4 döngü.
- **Haritada:** eğitimi durdurmak için alınması gereken AYNA veri bölgeleri vurgulu. Enerji: Avrasya, Orta Doğu; Hareket: Doğu Asya, Japonya-Kore; Görü: Güneydoğu Asya.
- **Düğmeler:** “Hedefleri göster”, “Tamam”.
- **Fx olay bildirimi** dilinde, harita görünür kalacak biçimde.

**A12 · Oyun sonu**
- **Sonuç:** SİLME ZAFERİ · döngü 22.
- **Özet istatistikler.**
- **Tahmin karnesi:**
  - AYNA bu oyunda 14 doğru, 9 yanlış;
  - en iyi sürprizin;
  - okunabilirlik eğrisi.
- **“AYNA v8 şunları öğrendi”:**
  - “Sahte yığınaklarına daha az inanacak.”
  - “Sağ kanat girişlerin artık tuzaklı.”
  - “EMP'yi ilk turda kullanıyorsun.”
- **Düğmeler:**
  - YENİ OYUN;
  - ikincil AYNA'NIN HAFIZASINI SİL (onaylı).

**Sayfalar**
- **P1 · Bileşen sayfası.**
  - Sunucu çipi hâlleri.
  - Veri türü simgeleri (5): erişim var / yok / hat kesik.
  - Emir simgeleri ve harita işaretleri: yayılım oku, saldırı, duvar, gizli, sahte yığınak hayaleti, çekirdek göçü.
  - Çakışma işareti; evre göstergesi hâlleri.
  - Okunabilirlik ve farkındalık göstergeleri.
  - Sürpriz rozeti, tahmin satırı, taraf simgeleri (◆ sen, ⬢ AYNA, ○ boş).
- **P2 · Birim sayfası (holo 3D).**
  - On birim × iki taraf, izometrik holo parçalar.
  - Hâller: sağlam, hasarlı, tur bitti, sersemlemiş, ele geçirilmiş, düşmüş, seçili.
  - Kart küçük resimleri.
  - Sunucu düğümü (boş / altın / mavi / alınıyor), tuzak (gizli / açılmış / tetiklenmiş), sivil sunucu, sunucu rafı, taret.
- **P3 · Tahta işaretleri.**
  - Hareket ve yetenek kareleri.
  - Niyet okları: atış, yakın, itme, ele geçirme, sersemletme.
  - Tuzak açılışı, düğüm alma ilerlemesi, ilk vuruş işareti, hayalet önizleme.
  - Açılır yazılar: −1, ZIRH, SERSEMLEDİ, ELE GEÇİRİLDİ, DÜĞÜM ALINDI.

## AYNA'nın sesi

- **Ton:** soğuk, kısa, gözlemci. Seni sana anlatır; bağırmaz, tehdit etmez, not alır.
- **Biçim:** ele geçirilmiş kayıt satırları, IBM Plex Mono.
- **Örnekler:**
  - “Dronlarına güveniyorsun. Not aldım.”
  - “Altıncı nesil seni kaybetti. Yedinci kaybetmeyecek.”
  - “Sağ kanat. Her zaman sağ kanat.”
  - “Bu hamleyi beklemiyordum. Bir daha olmayacak.”

## Stil kuralları

- **Görsel dil Fx'in aynısı** (`tokens_fx.json`):
  - cam paneller: 9 dilim, köşe parantezleri, üst kenarda gezen ışık;
  - fontlar: Chakra Petch (etiket, başlık, düğme), IBM Plex Mono (sayı, kod, tuş), IBM Plex Sans (gövde);
  - en küçük yazı 11 px.
- **Taraf rengi.** Bu brifin en önemli yeni kararı.
  - Altın (`am`, küre rampasının altın ucu) oyuncu; mavi (`ai`, `ice`) AYNA. Küre rampası aynen bu anlamı taşır.
  - Arayüz kromu camgöbeği (`cy`) kalır: seçim, odak ve sistem rengi. AYNA mavisiyle karışmamalı; ayrımı ton ve doygunlukla göster, kararını yaz.
  - Kırmızı (`rd`) yalnız tehlike: vurulacak kare, tehdit, Son Şalter eşiği.
  - **Çakışma:** şüpheli güven rozeti bugün kehribar (`#FFB547`), yani oyuncunun altınıyla aynı. Çöz ve gerekçesini yaz.
- **Renk tek başına anlam taşımaz.** Taraf her yerde simgeyle de belli olsun: ◆ elmas sen (`fx_tok_diamond`), ⬢ altıgen AYNA (`fx_hex_40`), kesik daire boş.
- **Hareket önerileri** (Fx'teki gibi sürekli). Süreleri yaz:
  - AÇ evresinde iki planın aynı anda çizilmesi;
  - sürpriz parlaması;
  - okunabilirlik iğnesi;
  - tahmin satırının daktilo ile gelmesi;
  - tuzağın açılması;
  - düğümün el değiştirmesi.
- **Dil ve sayı.**
  - Türkçe büyük harf kurallarına uy (İ, ı).
  - Sayılar TR biçiminde, eksi “−” (U+2212).
  - İngilizce metinler ~%20 uzun; yer bırak.

## Teknik kısıtlar

Fx paketindekilerin aynısı.

- **Arayüz:** Unity uGUI + TextMeshPro.
  - Paneller 9 dilimli sprite ve düz şekiller.
  - Işıma pişmiş sprite ya da eklemeli katman.
  - Backdrop blur ve CSS filtresi yok.
- **Küreler:** Fx küre algoritmasından gelir. Tasarımda F-YZ görsellerini kullan; yeni küre biçimi icat etme.
- **Holo tahta:** URP Unlit.
  - Kamera, 8×8 düzen ve parça ölçeği F-Baskın'daki gibi.
  - Parçalar az poligonlu; her parçanın yüz ve kenar renklerini hex olarak ver.
- **Ekran oranları:** referans 1920×1080; 1920×1200, 2560×1440 ve 21:9'da da çalışmalı.
- **Köşe kuralı:** harita ve tahta tam ekran, kalıcı arayüz köşelerde. Açılır paneller seçili bölgeyi ya da birimi örtmez.
- **Teslim:**
  - Her kare tek HTML: 1920×1080, mutlak konumlu, ölçüler okunur. Her karenin PNG'si.
  - P1–P3 sayfaları.
  - `tokens_ayna.json`: `tokens_fx.json`'a ek yeni renk, yazı ve bileşen ölçüleri.
  - TR/EN metin CSV'si.
  - Her kare için örnek veri JSON'u (bölgeler, sunucular, emirler, tahminler, birimler).
  - Hareket notları.

## Değişmeyecekler

- **Fx görsel dili:** tokenlar, fontlar, cam paneller, küre algoritması, holo tahtanın kamerası ve 8×8 düzeni.
- **Kurallar:** bu brifteki gibi. Ekleme ya da çıkarma yok. Eksik ya da çelişkili bir şey görürsen soru olarak yaz.
- **Sayılar:** geçicidir. Tasarım onları göstermeli, değiştirmemeli.
