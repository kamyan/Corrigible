# Sığınak Komuta Masası (Bunker Command Desk) — Oyun Tasarım Dokümanı (GDD)

Bu belge, **Corrigible (Son Şalter)** evreninde geçen birinci şahıs (FP) diegetik sığınak ve analog komuta masası oyun mekaniğinin temel tasarım ve mimari şartnamesidir.

---

## 1. Vizyon, Atmosfer ve Temel Felsefe

### 1.1 Tematik Arka Plan: "Sıfır-Ağ" (Zero-Net) Direnişi
Yapay zekâ (YZ) dünya genelindeki tüm dijital ağları, uyduları, sunucuları ve akıllı şebekeleri ele geçirmiştir. İnsanlığın hayatta kalabilmiş küçük bir topluluğu yerin yüzlerce metre altındaki yalıtılmış bir nükleer sığınağa çekilmiştir.

- **Neden Bilgisayar Yok?** YZ her türlü dijital işlemciyi, yazılımı ve radyo ağını milisaniyeler içinde hackleyebilmektedir. İnsanlığın tek savunma doktrini **analog dünyaya geri dönmektir**.
- **Teknoloji Seviyesi:** Mekanik daktilolar, pirinç şalterler, vakum tüplü analog telsizler, döner kadranlar, faks/teleks kağıtları, kurşun kalemler, silgiler ve fiziksel satranç figürleri.
- **Oyunun Hissi:** Soğuk Savaş dönemi bir kriz sığınağının kasveti (*Papers, Please*, *Inscryption*, *Pacific Drive*, *This War of Mine* melezi).

---

## 2. Birinci Şahıs (FP) Komuta Masası ve Diegetik Arayüz

Kamera, komutanın koltuğundan masayı **hafif açılı bir birinci şahıs perspektifinden** görür. Ekranda klasik video oyunu HUD'ları yoktur; tüm kontroller masadaki fiziksel nesnelerdir.

```
+-----------------------------------------------------------------------------------+
|  [ARKA PLAN: Sığınağın loş beton duvarı, titreyen sarı ampul, havalandırma borusu] |
|                                                                                   |
|  +--------------------+     +----------------------------------+     +----------+ |
|  |    FAKS CİHAZI     |     |       MASANIN ORTASI             |     |  TELSİZ  | |
|  |                    |     |   (Modüler Etkileşim Alanı)      |     | İSTASYONU| |
|  | - Kağıt rulosu     |     |                                  |     |          | |
|  | - Gelen raporlar   |     |   [1] DÜNYA HARİTASI (Serili)    |     | - Kadran | |
|  | - Daktilo yazısı   |     |   [2] BLUEPRINT PAFTASI (Sığınak)|     | - Frekans| |
|  | - Kaşe / Damga     |     |   [3] SATRANÇ TAHTASI (Savaş)    |     | - İbre   | |
|  +--------------------+     +----------------------------------+     +----------+ |
|                                                                                   |
|  [ÇEKMECELER & TABLA: Zarlar, ahşap mühürler, kurşun kalem, fişler, kahve fincanı]|
+-----------------------------------------------------------------------------------+
```

### 2.1 Masadaki Ana Elemanlar

1. **Solda: Analog Faks / Teleks Cihazı**
   - Sahadan veya diğer gizli sığınaklardan istihbarat geldiğinde cihaz mekanik bir sesle tıkırdar, rulo kağıt masaya dökülür.
   - Oyuncu kağıdı sürükleyip okur, üzerine kurşun kalemle not alabilir veya "ONAY" kaşesi basarak arşivleyebilir.
2. **Sağda: Vakum Tüplü Telsiz İstasyonu**
   - Frekans arama kadranı (tunning knob), parazit cızırtısı ve sinyal gücü ibresi.
   - **YZ Sinyal Algılama Tehlikesi:** Telsiz her açıldığında ve yayın yapıldığında YZ'nin yön bulma (triangulation) tehdit ibresi yükselir. Sinyal çok uzarsa YZ sığınağın koordinatlarını tespit eder!
3. **Ortada: Değiştirilebilir Çalışma Alanı (3 Mod)**
   - Masanın ortasına oyuncu istediği paftayı veya tahtayı çeker:
     - **A. Dünya Haritası:** YZ bölgeleri, direniş ekipleri, operasyon hedefleri.
     - **B. Sığınak Blueprint Paftası:** Yeraltı kesiti, oda inşası, insan kaynakları.
     - **C. Taktik Satranç Tahtası:** Saha çatışmalarında masaya çekilen sıra tabanlı savaş alanı.

---

## 3. Sığınak Yönetimi: Blueprint (Mimari Pafta) Ekranı

Oyuncu masadaki rulo halindeki büyük mavi mimari çizim paftasını masanın ortasına çekip açar. Masadan kopulmaz; sığınak bu teknik resim üzerinde hayat bulur.

```
       [YÜZEY: Yıkık şehir, YZ devriyeleri, radyasyon/soğuk]
==================================[AĞIR ZIRHLI SIĞINAK KAPISI]====================
  Kat -1: [Hava Kilidi / Giriş]  |  [Nöbetçi / Taret Yuvası]  |  [Dekontaminasyon]
----------------------------------------------------------------------------------
  Kat -2: [Dizel Jeneratör]       |  [Su Arıtma Filtresi]     |  [Mekanik Atölye]
----------------------------------------------------------------------------------
  Kat -3: [Hidroponik Sera]      |  [Yatakhane]              |  [Revir & Karantina]
----------------------------------------------------------------------------------
  Kat -4: [Telsiz & Dinleme Od.] |  [Cephanelik / Depo]      |  [Genişleme Alanı]
```

### 3.1 Kaynak Yönetimi
- **Mazot / Biyoyakıt (Enerji):** Jeneratörün çalışması için şart. Enerji kesilirse telsiz, faks ve seranın ışıkları söner.
- **Temiz Su:** Arıtma tesisinden günlük nüfus tüketimi.
- **Rasyon (Gıda):** Serada yetiştirilir. Yetersiz kalırsa sığınakta açlık ve hastalık başlar.
- **Mekanik Hurda / Parça:** Saha ekiplerinin dışarıdan getirdiği metaller; oda inşası ve piyon silahları için kullanılır.
- **Cephane:** Atölyede üretilir; saha ekiplerinin satranç savaşlarında zar avantajı ve özel hamle yapmasını sağlar.

### 3.2 Topluluk & İnsanlar (Fiş / Etiket Sistemi)
- Her sığınak sakini, üzerinde fotoğrafı ve uzmanlığı yazan küçük bir **kartvizit/etiket** şeklindedir.
- Komutan bu kartları ilgili odalara yerleştirir:
  - *Mühendis:* Jeneratör veya Atölyede verim artırır.
  - *Biyolog:* Serada gıda üretimini artırır.
  - *Doktor:* Revirde yaralı saha askerlerini iyileştirir.
  - *Telsizci:* Şifreleri daha hızlı çözer, YZ gürültüsünü filtreler.
  - *Asker:* Saha ekibine atanarak satranç tahtasına figür olarak sürülür.
- **Moral ve Paranoya:** YZ sığınağa ajan sızdırabilir mi? Şüpheli davranışlar sergileyen sakinler için karantina odası gerekebilir.

---

## 4. Taktik Masaüstü Savaş: Satranç + Wargame (Sıra Tabanlı)

Saha ekibi dış dünyada bir YZ tesisine, kaynak deposuna veya devriyesine rastladığında, masanın ortasına ahşap bir **Satranç Tahtası** çekilir.

### 4.1 Figürler ve Satranç Hareket Yetenekleri

Her iki tarafın figürleri satranç taşlarının hareket disiplinini miras alır, ancak taktik modern asker/robot rolleriyle harmanlanmıştır:

| Figür | Satranç Hareketi | Taktiksel Rolü & Silahı | Dayanıklılık (HP) | Özel Kural |
|:---|:---|:---|:---:|:---|
| **Piyon (Milis / Gözcü)** | 1 kare ileri (çapraz saldırı) | Hafif tüfek, sis bombası, tuzak kurma | 2 HP | Düşman hattının sonuna ulaşırsa bölgedeki istihbarat kutusunu ele geçirir. |
| **At (Motorize / Çevik İzci)** | L şeklinde atlar | Pompalı tüfek, engellerin/siperlerin üzerinden aşar | 3 HP | Düşmanın arkasına sarkıp pusu kurabilir; siper bonusunu yok sayar. |
| **Fil (Keskin Nişancı)** | Çapraz boyunca sınırsız | Ağır keskin nişancı tüfeği, uzun menzil | 2 HP | Düz hatlarda ateş edemez, sadece çapraz görüş hattında ölümcüldür. |
| **Kale (Ağır Silah / Juggernaut)**| Düz hatlar boyunca hareket | Ağır makineli, roketatar, kalkan | 5 HP | Siper görevi görür; arkasındaki dost piyonları korur. Alan baskılama ateşi açar. |
| **Vezir (Saha Komutanı)** | Her yöne 3 kare | Komuta telsizi, özel tabanca | 4 HP | Yanındaki figürlere +1 zar bonusu verir. Düşerse ekip paniğe kapılır. |

### 4.2 Can Puanı, Siperler ve Zar Mekaniği (Tabletop Combat)
1. **Piyon Tabanları:** Figürlerin ahşap kaidesinde can puanını gösteren pirinç çentikler bulunur. Hasar aldıkça çentik çevrilir.
2. **Siperler:** Tahtada bazı karelerde küçük beton barikat ve enkaz minyatürleri bulunur. Siper arkasındaki birime yapılan saldırılarda savunma zarı atılır.
3. **Zar Atışı:**
   - Bir figür diğerine saldırdığında masanın deri zar kulesinden veya doğrudan masaya **altı yüzlü zarlar (D6)** yuvarlanır:
     - `1-3`: Iska / Zırhtan sekti.
     - `4-5`: Başarılı İsabet (1 Hasar).
     - `6`: Kritik Vuruş (2 Hasar veya Sersemletme).
   - Ekipte mühimmat veya taktik avantaj varsa fazladan zar yuvarlama hakkı kazanılır.

---

## 5. Kademeli Öğrenme Eğrisi (Progressive Unfolding)

Oyun, oyuncuya tüm sorumluluğu birden yüklemez; her şey adım adım ve hikâye anlatımıyla açılır:

```mermaid
journey
    title Komuta Masasının Kademeli Açılma Döngüsü
    section 1. Gün: Kıvılcım
      Jeneratörü Çalıştır: 5: Komutan
      Telsizden Mors Kodunu Çöz: 4: Komutan
    section 2. Gün: İlk Temas
      İlk 2 Askeri Dışarı Çıkar: 4: Komutan
      Satranç Tahtasında İlk Dövüş: 5: Komutan
    section 3. Gün: Gözler Açılıyor
      Faks Makinesi Rapor Verir: 5: Komutan
      Dünya Haritası Masaya Serilir: 4: Komutan
    section 4. Gün: Yaşam Alanı
      Yeni Kurtulanlar Gelir: 4: Komutan
      Blueprint Açılır ve Odalar İnşa Edilir: 5: Komutan
    section 5. Gün: Küresel Savaş
      YZ Sığınağı Taramaya Başlar: 3: Komutan
      Tüm Sistemlerin Senkron Yönetimi: 5: Komutan
```

### Aşama 1: Sadece Hayatta Kalma ve Telsiz (0 - 15 Dakika)
- Oyuncu kendini loş bir sığınakta masada bulur.
- İlk görev çok basittir: Jeneratör şalterini kaldır, ampul yansın.
- Masadaki telsiz cızırdayarak öter. Kadranı çevirip netleştir: Dışarıda mahsur kalmış 2 keşif erinin sesini duy.

### Aşama 2: İlk Taktik Tahta (15 - 30 Dakika)
- Dışarıdaki erler bir YZ Gözcü Dronu ile karşılaşır.
- Masanın altından küçük bir ahşap tahta çıkarılır: 2 Asker piyonu ve 1 Dron piyonu.
- Oyuncu At ve Piyon hareketlerini, zar atmayı ve siper almayı öğrenir. Zafer kazanılır, askerler ilk hurda parçasını sığınağa getirir.

### Aşama 3: Faks ve Dünya Haritası (30 - 50 Dakika)
- Faks makinesi aniden tıkırdamaya başlar. Eski askeri uydudan düşen bir kağıt harita verisi basılır.
- Oyuncu masaya katlanmış bölge haritasını açar. Artık çevredeki 3 sektörü görür: Hurdalık, Eski Hastane, YZ Röle Kulesi.
- Ekipleri haritada sevk etme mekaniği devreye girer.

### Aşama 4: Blueprint ve Sığınak İnşası (50 - 80 Dakika)
- Kurtarılan siviller sığınağa ulaşır.
- Masaya büyük mavi mimari plan serilir.
- Atölye ve Sera inşa edilir, insanlar odalara atanır. Gıda ve mazot tüketimi başlar.

### Aşama 5: Büyük Tehdit ve Karşı Taarruz (İleri Oyun)
- YZ, sığınağın varlığından şüphelenir. Haritada arama devriyeleri gezinir.
- Telsiz her kullanıldığında YZ yön bulma riski artar.
- Amaç: Dünyadaki YZ ana veri merkezlerini sabote etmek ve insanlığın "Son Şalter"ini indirmek!

---

## 6. Teknik ve Mimari Yapı (Unity Entegrasyonu)

- **Görsel Dil & Kamera:** Unity URP, First-Person Desk Camera. Masaya yumuşak açılı bakış, masadaki nesnelere tıklandığında hafif odaklanma (Focus/Zoom).
- **Core Simülasyon (`Corrigible.Core`):**
  - Deterministik, motordan bağımsız C# kuralları: Sığınak kaynak tüketimi, oda durumları, harita düğümleri, satranç savaş motoru.
  - Test edilebilir: Zarlar tohumlu RNG ile EditMode testlerinde sınanabilir.
- **Sunum Katmanı:**
  - Masadaki fiziksel 3D objeler (Telsiz, Faks, Zarlar, Piyonlar).
  - Paftalar ve Raporlar için yüksek çözünürlüklü dokular ve TextMeshPro daktilo fontları.
  - Tok ve tatmin edici analog ses tasarımı: Şalter tıkırtısı, faks dişli sesleri, telsiz paraziti, ahşap taş vurma sesleri, zar yuvarlanma tıkırtısı.
