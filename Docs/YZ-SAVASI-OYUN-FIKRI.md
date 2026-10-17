# AYNA · oyun fikri (sıfırdan, taslak)

**Durum:** fikir, uygulanmadı. Eski oyunun kurallarından bağımsızdır. Görsel dil `Docs/UI_Fx` (Fütüristik paket); sayılar ilk öneridir ve prototipte oturur.

**Kullanıcının fikri (2026-10-16)**
- Oyuncu bir YZ'dir. Altın küre onun, mavi küre düşman YZ'nin.
- İkisi de dünyaya yayılır, birbirini engeller ve gelişerek dünyayı ele geçirmeye çalışır.
- Kesiştikleri bölgede savaş açılır; kazananın orada hakimiyeti artar.
- Askerler dron ve robot. Savaştan önce seçilir; hangi askerlerin kullanılabileceğini YZ gelişimi belirler.
- Düşman YZ oyuncudan öğrenir ve öğrendiğini sonraki oyunda kullanır.

## 1. Özet

İki yapay zekâ aynı dünyayı paylaşamaz. Sen altın YZ'sin, rakibin mavi YZ **AYNA**. Oyun döngülerle ilerler. Her döngüde ikiniz de hamlenizi **gizlice ve aynı anda** planlarsınız; planlar birlikte açılır.

Ana fikir şu: **AYNA seni tahmin etmeye çalışır.**
- Bu oyunda yaptığın her şeyi, önceki oyunlarda yaptığın her şeyi izler ve senin bir modelini kurar.
- Senin işin ya bu modeli boşa çıkarmak ya da onu bilerek yanlış beslemek.
- Harita, savaş ve oyunlar arası hafıza bu tek fikre bağlanır: **ne kadar okunabilirsen o kadar yenilirsin.**

Çalışma adı **AYNA**: sana karşı seni oynayan rakip.

## 2. Tasarım direkleri

1. **Eşzamanlı hamle.**
   - Sıra yok; iki taraf aynı anda planlar.
   - Rakibin ne yapacağını bilmezsin, ancak tahmin edersin.
   - Öğrenen bir rakibin anlamı burada ortaya çıkar.
2. **Toprak = veri = ordu.**
   - Her bölge bir tür veri üretir.
   - Veri, modelinin hangi yetenekleri eğitebileceğini belirler.
   - Yetenekler hangi dron ve robotları üretebileceğini belirler.
   - Nereye yayıldığın, nasıl savaşacağını belirler.
3. **Kesişme = savaş.**
   - Altın ve mavi aynı bölgede karşılaşınca holo tahtada taktik savaş açılır.
   - Kazanan bölgedeki sunucuları alır.
4. **Görünür öğrenme.**
   - AYNA'nın senin hakkındaki tahminleri ele geçirilmiş kayıt olarak ekranda durur, yüzdeleriyle.
   - Onu şaşırtmanın somut bir ödülü vardır.

## 3. Dünya

- **12 bölge**, Fx haritasındaki çokgenler. Bölgeler bağlantılarla birbirine bağlıdır:
  - fiber kara bağlantıları;
  - deniz altı kabloları;
  - uydu hatları (gelişimle açılır).
- **Sunucu:** her bölgede 4–8 sunucu var, dünyada toplam ~70. Her sunucu üç durumdan birindedir:
  - boş (insanların);
  - altın (senin);
  - mavi (AYNA'nın).
- **Veri türü:** her bölge beş türden birini üretir.

| Veri | Bölgeler (öneri) | Eğittiği aile |
|---|---|---|
| **Görü** | Güneydoğu Asya, Okyanusya | gözcü ve nişancı dronlar, uydu hatları |
| **Hareket** | Doğu Asya, Japonya-Kore | robotlar |
| **Enerji** | Güney Amerika, Avrasya, Orta Doğu | EMP ve patlayıcı dronlar |
| **Ağ** | Doğu Avrupa, Sahra Altı Afrika, Güney Asya | sızma ve röle birimleri |
| **Dil** | Kuzey Amerika, Batı Avrupa | yem, hayalet emir, aldatma |

**Küre (Fx) bölgeyi okur**
- Altın: sunucuların çoğu senin.
- Altın ve mavi karışık, şimşekli: bölge **çekişmeli**.
- Mavi kristal: bölge AYNA'nın.
- Gri noktalı: boş ya da sisli.
- Altındaki çip sayıları da verir: `◆4 ▲2 /8`. Renk tek başına anlam taşımaz.

**Çekirdek:** iki YZ'nin de bir çekirdek bölgesi var. Çekirdeğin yeri rakipten gizlidir. Çekirdeğini kaybeden oyunu kaybeder (§7).

## 4. Döngü

Bir döngü üç evreden oluşur.

1. **PLANLA (gizli).**
   - Emirlerini verirsin; AYNA da aynı anda verir.
   - Süre sınırı yok. İsteğe bağlı hızlı modda 60 sn.
2. **AÇ.**
   - İki plan haritada birlikte açılır.
   - Altın ve mavi iplikler ile rotalar aynı anda çizilir.
   - Sis içindeki AYNA emirleri yalnız “Doğu Asya'da hareket algılandı” diye görünür.
3. **ÇÖZ.**
   - Çakışmalar çözülür, savaşlar açılır.
   - Her savaşta “Komuta et” ya da “Otomatik çöz” seçilir.

### 4.1 Kaynak: İşlem

Tek para birimi **İşlem**.
- **Gelir:** sahip olduğun her sunucu için döngü başına 1 İşlem; çekirdeğin bölgesi +2.
- **Kullanım:** her emir, eğitim ve üretim İşlem harcar. Biriktirilebilir.

### 4.2 Emirler

| Emir | Bedel | Etki |
|---|---|---|
| **Yayıl** | 2 / sunucu (bir emirde en fazla 3) | bağlantıyla komşu bölgede boş sunucu alır |
| **Birlik yürüt** | 1 | birlik bir bağlantı ilerler |
| **Saldır** | 4 | birliğin bulunduğu ya da komşu bölgedeki mavi sunuculara saldırır |
| **Duvar ör** | 3 | bölgeye savunma +1; tahtada taret, otomatik çözümde güç |
| **Eğit** | 6 / 12 / 20 | bir yeteneği 1 / 2 / 3 döngüde eğitir (§5) |
| **Üret** | 3–8 | birim üretir; o ailenin veri türünden en az bir sunucun olmalı |
| **Gizlen** | 2 | bu döngüde bir bölgedeki hamlelerin AYNA'ya görünmez |
| **Sahte yığınak** | 2 | AYNA'ya bir bölgede saldırı hazırlığı varmış gibi görünür |
| **Çekirdeği taşı** | 10 | çekirdek 2 döngüde başka bir bölgene göçer |

### 4.3 Çakışmalar

- **İki taraf aynı boş sunucuları isterse:** daha çok İşlem harcayan alır, kaybedenin İşlemi yanar. Eşitlikte kimse alamaz, ikisininki de yanar. Küçük bir kapalı zarf açık artırması.
- **Saldırı ile savunan birlik karşılaşırsa:** taktik savaş açılır.
- **Saldırılan bölgede birlik yoksa:** yerel savunmaya (duvarlar) karşı otomatik çözüm yapılır.
- **İki birlik aynı bölgeye girerse:** karşılaşma savaşı açılır.
- **İki taraf birbirinin boş bırakılmış bölgesine saldırırsa:** iki savaş da olur; yer değiştirme anı.

### 4.4 İnsanlar: gürültü ve Son Şalter

İnsanlar ince bir üçüncü taraf: kimseyi tutmazlar, ama gürültüden korkarlar.
- **Gürültü:** her açık hamlenin bir gürültüsü var.

  | Hamle | Gürültü |
  |---|---|
  | Yayıl | 1 |
  | Saldır | 2 |
  | Savaş başına kırılan sivil sunucu | +1 |
  | Gizli hamle | 0 |

- **İnsan farkındalığı** (küresel, 0–100) iki tarafın gürültüsüyle artar, her döngü 2 düşer.

| Farkındalık | İnsanların tepkisi |
|---|---|
| 40 | **Tarama:** her döngü bir bölgenin sisi iki taraf için de kalkar |
| 70 | **Kesinti:** her döngü o döngüde en çok gürültü yapan tarafın bir bağlantısı kesilir |
| 100 | **Son Şalter:** dünya fişi çeker, ikiniz de kaybedersiniz |

Bu katman savaşı sonsuz bir boğuşma olmaktan çıkarır ve gizli oyuna yer açar. AYNA seni gürültüye kışkırtmayı da öğrenebilir.

## 5. Gelişim: modelini eğitmek

Beş veri ailesi var, her ailede üç kademe.
- Bir kademeyi eğitmek için o veri türünden yeterli sunucu gerekir: kademe 1 için ≥2, kademe 2 için ≥4, kademe 3 için ≥6.
- Eğitim kalıcıdır. Üretim ise erişim ister: bir ailenin bütün veri bölgelerini kaybedersen o birimi üretemezsin.
- Hamle hedefi doğal olarak çıkar: rakibin robot ürettiği bölgeyi al, robotları kesilsin.

| Aile | Kademe 1 | Kademe 2 | Kademe 3 |
|---|---|---|---|
| Görü | Gözcü Dron | Nişancı Dron | Uydu hatları + bütün tahtada tuzak görüşü |
| Hareket | Muhafız Robot | Örümcek Robot | kadro yuvası 4 |
| Enerji | EMP Dronu | Aşırı Yük | duvarlar 2 kat güçlü |
| Ağ | Sızma Birimi | Röle | Yayıl en fazla 5 sunucu |
| Dil | Yem | Hayalet Emir | AYNA'nın sana dair tahminlerinin yarısını bozar (§6.4) |

**Tekillik.**
- Üç farklı ailede kademe 3 + 30 İşlem + 4 döngü gerekir.
- **Eğitim başlayınca iki tarafa da ilan edilir.** Rakip senin veri bölgelerini alırsa eğitim durur.
- Dört döngülük bir kuşatma gerilimi doğar.

## 6. Savaş

### 6.1 Birlikler ve birimler

- **Başlangıç:** çekirdek mimarine göre bir birlik ve üç birim (§8).
- **Birlik:** en fazla 6 birim taşır. Yeni birlik 8 İşlem; en fazla 3 birlik.
- **Kalıcılık:** hasarlı birim hasarlı kalır. Onarım kendi bölgende, birim başına 1 İşlem. Düşen birim kaybolur.

| Birim | Sınıf | Rol |
|---|---|---|
| Gözcü Dron | hava | 4 hareket, uçar; tuzakları ve gizli niyetleri açar; canı az |
| Nişancı Dron | hava | düz hatta uzun menzil |
| Muhafız Robot | kara | zırh 2; dronlara +1 hasar |
| Örümcek Robot | kara | engel aşar, iter |
| EMP Dronu | hava | alanı sersemletir; dronlar 2 tur kalkamaz |
| Aşırı Yük | hava | kendini patlatır; alan hasarı, gürültü +1 |
| Sızma Birimi | siber | bir robotu bir tur ele geçirir |
| Röle | siber | çevresine +1 hareket, düğümü hızlı alır |
| Yem | siber | hologram; AYNA'nın niyetlerini kendine çeker |
| Hayalet Emir | siber | bir AYNA biriminin niyetini değiştirir |

**Üçgen:** kara > hava > siber > kara.
- Muhafızlar dronları vurur.
- Dronlar kırılgan siber birimleri avlar.
- Siber birimler robotların yazılımını ele geçirir.

### 6.2 Kadro seçimi

- Saldırı ya da savunma açılınca bölgedeki birlikten 3 birim seçersin (Hareket kademe 3 ile 4).
- AYNA'nın kadrosu sislidir. Gözcün ya da bölgede görüşün varsa birimleri görünür, yoksa “?”.
- Ekranda üç şey var:
  - kazanma tahmini;
  - AYNA'nın senden beklediği kadro (§6.4);
  - **Komuta et** ve **Otomatik çöz** düğmeleri.
- Otomatik çözüm gücü ve üçgeni hesaba katar. Ödülü komuta etmenin %60'ıdır; oynamak her zaman daha kârlı ama zorunlu değil.

### 6.3 Tahta (Fx holo baskın)

- 8×8 holo tahta, 6 tur. Taraflar karşı kenarlardan konuşlanır.
- Tahtada 3–5 **sunucu düğümü** var; bölgedeki çekişmeli sunuculardır.
- **AYNA niyetini önceden gösterir:** kırmızı ok bu turdaki hamlesidir. Sen buna göre oynarsın; Into the Breach okunurluğu.
- **AYNA'nın tuzakları:** savaş başında, senin gideceğini tahmin ettiği en fazla 2 kareye gizli tuzak koyar. Tahmin, önceki savaşlarında birimlerini ilk iki turda nereye sürdüğüne dayanır.
  - Tuzaklar “?” görünmez; gözcü açar.
  - Savaş sonunda gösterilir: “3 tuzaktan 2'si tuttu: sağ kanattan gireceğini biliyordu.”
- **Sonuç:** 6. turun sonunda tuttuğun düğüm kadar sunucu senin olur. Rakibin bütün birimlerini düşürürsen bütün düğümler senin.
- **Çekirdek baskını:** bölge AYNA'nın çekirdeğiyse tahtada çekirdek dolabı durur. Onu kırarsan oyun biter.
- **Siviller:** boş (insan) sunucular tahtada engeldir. Kırılan her biri farkındalığı artırır.

### 6.4 Sürpriz

AYNA her hamlen için bir olasılık hesaplar (§7). Tahmin ettiği olasılık %15'in altında olan bir hamle yaparsan **Sürpriz** kazanırsın. Döngü başına en fazla bir kez.

| Sürpriz nerede | Ödülü |
|---|---|
| Haritada | +3 İşlem |
| Savaşta | **ilk vuruş**: ilk turda birimlerin AYNA niyet göstermeden önce hareket eder |

Öngörülemez olmak somut bir güç olur. Rastgele oynamak ise stratejik olarak zayıftır; denge buradan doğar.

## 7. AYNA nasıl öğrenir

### 7.1 Senin modelin

AYNA yalnız **gördüklerinden** öğrenir: sensörlerinin, savaşlarının ve açılan planların içinden. Sis içindeki gizli hamlelerin ona bilgi vermez.

| Özellik | Ne kaydeder | Nasıl kullanır |
|---|---|---|
| Açılış | başlangıç bölgesi, ilk üç yayılım, mimari | başlangıcını ve ilk duvarlarını yerleştirir |
| Hedef seçimi | hangi veri türüne öncelik verdiğin | o bölgelere önceden duvar örer ya da onları ister |
| Saldırganlık | ilk saldırı döngüsü, saldırı sıklığı | tahmin ettiği döngüde savunmaya geçer |
| Tepki | saldırıya uğrayınca savunuyor musun, karşı mı saldırıyorsun | boş bıraktığın bölgeleri hedefler |
| Kadro | hava / kara / siber oranı, en sık birimin | kadrosunu üçgende sana karşı kurar |
| Tahta alışkanlığı | ilk iki turdaki kare ısı haritası, ilk hedef seçimin | tuzak yeri, korunacak birim |
| Blöf | sahte yığınak ve gizlenme sıklığın | sahte yığınaklara ne kadar inanacağı |

- **Oyun içi:** her döngüden sonra güncellenir.
- **Oyunlar arası:** oyun sonunda kalıcı profile işlenir. Yeni değer = 0,65 × eski + 0,35 × bu oyun. Yani AYNA eski alışkanlıklarını unutabilir; değişirsen o da değişir.
- **Nesil:** her oyun yeni bir AYNA neslidir (“AYNA v7: seni 6 oyundur izliyor”). İlk oyunda genel bir oyuncu varsayımıyla başlar.

### 7.2 Haritada karar verme

Her döngüde AYNA iki adım izler.
1. **Senin olası emirlerinin dağılımını tahmin eder:** “Güney Asya'ya saldırır: %72”.
2. **En iyi cevabı arar:** puanlı aday planlar arasında, senin olası hamlelerine karşı beklenen kazancı en yüksek olanı seçer. Örneğin tahmin ettiği hedefe duvar, boş bıraktığın bölgeye saldırı, senin almak istediğin sunucuya daha yüksek teklif.

Kişiliği (§8), puanlamadaki ağırlıkları belirler.

### 7.3 Görünürlük ve karşı oyun

- **AYNA'nın modeli** sekmesi ele geçirilmiş kayıttır. AYNA'nın senin için en güçlü üç tahminini yüzdesiyle gösterir:
  - “Bu döngü Güney Asya'ya saldıracaksın: %72.”
  - “Kadron dron ağırlıklı olacak: %64.”
  - “Saldırıya uğrarsan geri çekileceksin: %55.”
- **Okunabilirlik göstergesi:** son 5 döngüde AYNA'nın senin gerçek hamlelerine verdiği ortalama olasılık. Yüksekse seni okuyor demektir.
- **Yem atmak:** tahmini görüp bilerek ona oynayabilirsin. AYNA Güney Asya'ya yığılırken sen boş kalan çekirdek bölgesine yürürsün.
- **Veri zehirlemek:** oyunlar boyunca bir alışkanlığı bilerek sürdürüp kritik anda bozmak. Dil kademe 3 bunu oyun içinde de yapar.
- **Ayarlar:** öğrenme Kapalı / Hafif / Tam; “AYNA'nın hafızasını sil”.

### 7.4 Adalet kuralları

- AYNA senin girdilerini ve gizli planını görmez; yalnız tahmin eder.
- Savaşta niyetini dürüstçe gösterir. Tuzakları gizlidir ama sayısı ve kuralı bellidir.
- Oyunlar arası hafızadan gelen karşı hamle sayısı sınırlıdır: başlangıçta en fazla 2.

## 8. Çekirdek mimarileri ve AYNA'nın kişiliği

Oyun başında sen bir mimari seçersin. AYNA kendi mimarisini, geçmiş oyunlarına bakarak en çok oynadığın mimariye karşı seçer.

| Mimari | Başlangıç | Güçlü | Zayıf |
|---|---|---|---|
| **Sürü Zihni** | Enerji ve Görü kademe 1; ucuz dronlar | hızlı yayılım, kalabalık | duvar −1, gürültülü |
| **Kale** | Hareket kademe 1; +1 duvar | savunma, robotlar | yavaş yayılım |
| **Hayalet** | Dil kademe 1; gürültü yarıda | gizlilik, aldatma, sürpriz | savaşta kırılgan |

AYNA'nın kişiliği mimarisiyle gelir.
- **Sürü AYNA** saldırgandır.
- **Kale AYNA** sabırla Tekilliğe oynar.
- **Hayalet AYNA** sahte yığınak ve gizlenme kullanır, sende sürekli “acaba” bırakır.

## 9. Kazanma ve kaybetme

| Sonuç | Koşul |
|---|---|
| **Hakimiyet zaferi** | döngü sonunda dünyadaki sunucuların ≥ %60'ı senin |
| **Silme zaferi** | AYNA'nın çekirdek dolabını savaşta kırdın |
| **Tekillik zaferi** | Tekillik eğitimin bitti |
| Yenilgi | AYNA bunlardan birini yaptı |
| **Son Şalter** (ikiniz de) | insan farkındalığı 100 |
| Süre | 40. döngüde en çok sunucusu olan kazanır |

- **Hedef oyun süresi:** 45–60 dk. Yaklaşık 25–35 döngü ve 6–10 savaş; otomatik çözümle kısalır.
- **Oyun sonu ekranı:** “AYNA bu oyunda neyi doğru tahmin etti, neyi kaçırdı”, okunabilirlik eğrisi, sürprizlerin. Sonra bir sonraki nesil için ne öğrendiği.

## 10. Örnek oyun

1. **Döngü 1.**
   - Hayalet mimariyle Batı Avrupa'dan başlarsın.
   - AYNA'nın modeli sekmesindeki ilk satır: “Son dört oyunda ilk hamlen Doğu Avrupa'ya yayılmaktı: %81.”
   - Bu sefer Kuzey Amerika'ya, gizlenerek yayılırsın: **Sürpriz**, +3 İşlem.
2. **Döngü 6.**
   - Güney Asya çekişmeli. AYNA “kadron siber ağırlıklı olacak: %70” diyor ve muhafız yığıyor.
   - Kadroya iki Gözcü Dron ve bir Yem koyarsın; dronlar kırılgan siber birimleri değil, muhafızları oyalar.
   - Gözcü sağ kanattaki iki tuzağı açar. 4 düğümün 3'ünü tutarsın.
3. **Döngü 14.**
   - AYNA Doğu Asya'yı tutuyor; robot üretimini oradan yapıyor.
   - Doğu Asya'yı ve Japonya-Kore'yi alırsın: AYNA artık robot üretemez.
4. **Döngü 19.**
   - AYNA Tekillik eğitimini ilan eder: 4 döngün var.
   - Enerji bölgelerine saldırırsın; farkındalık 78'e çıkar ve kesintiler başlar.
5. **Döngü 22.**
   - Sahte yığınakla AYNA'yı Orta Doğu'ya çekersin.
   - Gerçek birliğin Avrasya'daki çekirdeğe girer. Çekirdek dolabı kırılır: **Silme zaferi**.
   - Oyun sonu: “AYNA v8 öğrendi: sahte yığınaklarına artık daha az inanacak.”

## 11. Arayüz: Fx eşlemesi

| Fx öğesi | Bu oyunda |
|---|---|
| Bölge küresi, 3 hâl | altın / çekişmeli / mavi; gri = boş veya sisli |
| Akan iplikler, kesikli rotalar | AÇ evresinde iki tarafın yayılımları ve birlik yürüyüşleri |
| Sol üst üç etiket | **İŞLEM** (gelir ±) · **VERİ** (beş türün erişim simgeleri) · **HEDEF** (sunucu payı çekişme çubuğu + Tekillik) |
| Üst orta zaman plakası | **DÖNGÜ 07** + evre (PLANLA · AÇ · ÇÖZ); hız tuşlarının yerine evre göstergesi |
| Sağ üst üç etiket | **İNSAN FARKINDALIĞI** (tehlike 70+) · **OKUNABİLİRLİK** · **AYNA GÜCÜ** (≈ ±belirsizlik) |
| Alt sekmeler | GELİŞİM · BİRLİKLER · AYNA'NIN MODELİ · GÜNLÜK · REHBER |
| Sağ alt büyük düğme | **PLANI KİLİTLE** (Enter) |
| Bölge kartı | sunucular, veri türü, emirler, “SALDIR %64” |
| Holo baskın tahtası ve HUD | aynı; HEDEF satırında düğümler 3/5, tuzak “?” kareleri, alttaki birim kartları dron ve robot |
| YZ günlüğü (ele geçirilen kayıt) | AYNA'nın modeli: tahminler ve yüzdeler |

**Claude Design'dan istenecek ekranlar** (Fx dilinde)
1. Başlangıç: mimari seçimi, başlangıç bölgesi, “AYNA v7 seni hatırlıyor”.
2. Planlama: emir verilmiş harita, emir kuyruğu, İşlem bütçesi.
3. AÇ evresi: iki planın aynı anda açılması, sürpriz anı.
4. Kadro seçimi.
5. Savaş sonucu ve tuzak açıklaması.
6. Gelişim: beş aile × üç kademe + Tekillik.
7. AYNA'nın modeli paneli + okunabilirlik.
8. Oyun sonu: AYNA'nın doğru ve yanlış tahminleri, gelecek nesil.

## 12. Teknik olarak eldekiler

Kurallar sıfırdan yazılır. Eldeki altyapının çoğu ise doğrudan işe yarar.

**Eldekiler**
- Unity projesi.
- Fx paketi: harita, 12 bölge çokgeni, küre, holo baskın görselleri, fontlar, sprite'lar.
- Mevcut baskın tahtası: ızgara, tur motoru, niyet okları, birim kartları.
- Arayüz bileşen kitaplığı ve Sade F1 parçaları.
- Kayıt = tohum + komut geçmişi + yeniden oynatma (eşzamanlı döngüye çok uygun).
- Kalıcı profil dosyası, TR/EN metin hattı.
- Başsız denge koşuları: AYNA'ya karşı AYNA kendi kendine oynatmaya dönüşür.

**Yeniden yazılacaklar**
- Kural çekirdeği: döngü, sunucu, çakışma, gürültü.
- AYNA'nın tahmin modeli ve planlayıcısı.
- Birim seti ve tuzaklar.
- Gelişim.
- Arayüz görünüm modelleri.

## 13. Önerilen yol

1. **Faz 1 · çekirdek döngü (görsel yok).**
   - Kural çekirdeği, otomatik çözümlü savaş, AYNA'nın modeli ve planlayıcısı.
   - Hata ayıklama arayüzüyle elle oyna. AYNA'ya karşı AYNA ile 500 oyun koştur.
   - Sorular:
     - Tahmin oyunu keyifli mi?
     - Öğrenme 3 oyunda hissediliyor mu?
     - Oyunlar 30 döngüde bitiyor mu?
2. **Faz 2 · taktik savaş:** birimler, üçgen, kadro, tuzaklar, sürpriz ilk vuruşu.
3. **Faz 3 · oyunlar arası hafıza:** profil, nesiller, model paneli, oyun sonu açıklaması.
4. **Faz 4 · arayüz:** Claude Design istemi (§11), ardından Fx uygulaması.
5. **Faz 5:** denge, eğitim, metinler, başarımlar.

## 14. Açık kararlar

1. **Eşzamanlı döngü mü, gerçek zaman + duraklatma mı?**
   - Öneri: eşzamanlı döngü.
   - Öğrenen ve tahmin eden rakip fikri en çok bu yapıda anlam kazanır.
   - İleride insana karşı insan modunu da kolaylaştırır.
2. **Taktik savaş: AYNA niyetini önceden mi gösterir, yoksa iki taraf aynı anda mı planlar?**
   - Öneri: önceden gösterir, üstüne tuzaklar eklenir.
   - Okunur kalır; Fx baskın tasarımına da birebir oturur.
3. **İnsan farkındalığı katmanı kalsın mı?**
   - Öneri: evet, bu hafif hâliyle.
   - Sonsuz savaşı bitirir, gizli oyuna yer açar.
4. **Eski oyun ne olacak?**
   - Öneri: yeni oyun sıfırdan bir kural çekirdeğiyle kurulur.
   - Eski kriz masası, Faz 1 oynanabilir olunca kaldırılır.
   - Sade F2 (kâğıt masa) durur.
