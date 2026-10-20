# SIĞINAK: SIFIR-AĞ (BUNKER: ZERO-NET)
## Bağımsız Oyun Tasarım Dokümanı (GDD) ve 3 Aşamalı Geliştirme Yol Haritası

Bu doküman, projeyi **sıfırdan bağımsız bir Unity projesi** olarak inşa etmek üzere hazırlanmış kapsamlı mimari şartnamedir. Dokümanın sonunda, Claude'a (Claude Code veya Claude Chat) bu sistemleri doğrudan kodlatabileceğiniz özel **Master Prompt'lar** yer almaktadır.

---

## 1. Oyun Vizyonu ve Temel Tasarım Direkleri

- **Oyun Türü:** Birinci Şahıs (FP) Diegetik Komuta Masası Simülasyonu / Sığınak Yönetimi / Sıra Tabanlı Masaüstü Taktik Strateji.
- **Atmosfer:** Soğuk Savaş retro-analog sığınak estetiği (*Inscryption* + *Papers, Please* + *Fallout Shelter* + *Tabletop Miniatures*).
- **Temel Hikâye Kuralı (Sıfır-Ağ):** YZ tüm dijital ağları ve bilgisayarları kontrol etmektedir. İnsanların hayatta kalabilmek için **bilgisayar, yazılım ve dijital işlemcileri tamamen terk edip mekanik/analog dünyaya dönmesi** zorunludur.
- **Arayüz İlkesi (Tam Diegetik):** Ekranda dijital HUD veya havada asılı menüler yoktur; her şey komutanın masasında duran fiziksel bir nesnedir (faks, telsiz, kağıt pafta, daktilo kağıtları, ahşap piyonlar, zarlar).

---

## 2. Üç Ana Geliştirme Adımı (Detaylı Şartname)

```
========================================================================================
                                     3 ANA GELİŞTİRME ADIMI
========================================================================================

 [ ADIM 1: ÇEKİRDEK SİMÜLASYON MOTORU ] (Pure C# / Unity-Bağımsız / POCO)
  ├── 1.1 Sığınak & Kaynak Ekonomisi (Odalar, Mazot, Su, Gıda, Hurda, Nüfus)
  ├── 1.2 Taktik Satranç Savaş Motoru (8x8 Izgara, Piyon/At/Fil/Kale/Vezir, HP, Zarlar, Siper)
  └── 1.3 Dünya Haritası & Tehdit Mantığı (Sektörler, YZ Yayılımı, Telsiz Sinyal Riski)

 [ ADIM 2: BİRİNCİ ŞAHIS MASA VE DİYEGETİK ARAYÜZ ] (Unity URP / 3D Sahne)
  ├── 2.1 FP Masa Kamerası ve Etkileşim Raycast'i (Serbest Bakış & Odaklanma)
  ├── 2.2 Masadaki Modüler Alan (State Machine: Dünya Haritası / Blueprint / Satranç)
  ├── 2.3 Sol: Faks Makinesi (Kağıt tıkırtısı, masaya rulo besleme, damga/onay)
  └── 2.4 Sağ: Vakum Tüplü Telsiz (Frekans kadranı, parazit, ses/mors, YZ yön bulma ibresi)

 [ ADIM 3: KADEMELİ AÇILMA & SENARYO MOTORU ] (Unfolding Story Loop)
  ├── Faz 1: Loşlukta İlk Kıvılcım (Jeneratör şalteri -> Telsiz frekansı arama)
  ├── Faz 2: İlk Taktik Temas (Masa ortasına satranç tahtası iner -> İlk zar ve zafer)
  ├── Faz 3: Gözler Açılıyor (Faks ilk raporu basar -> Masaya dünya haritası serilir)
  ├── Faz 4: Yaşam Alanı (Mavi Blueprint açılır -> Odalar kurulur, iş gücü atanır)
  └── Faz 5: Küresel Direniş (YZ arama devriyeleri, telsiz sessizlik protokolü, tam döngü)
========================================================================================
```

---

### ADIM 1: Çekirdek Simülasyon Motoru (Core Simulation Engine)
*Kural: Bu katman tamamen saf C# (`noEngineReferences: true`) olacak, Unity API'lerine bağlı kalmayacak ve yüzde yüz deterministik test edilebilir olacaktır.*

#### 1.1 Sığınak ve Kaynak Modülü
- **Kaynaklar:**
  - `Fuel` (Mazot / Biyoyakıt): Jeneratör için günlük tüketilir; biterse elektrik kesilir.
  - `Water` (Temiz Su): Arıtma filtresinden üretilir; nüfus başına tüketilir.
  - `Food` (Gıda / Rasyon): Hidroponik serada üretilir; açlık morali düşürür.
  - `Scrap` (Mekanik Hurda): Saha operasyonlarından toplanır; oda inşası ve silah yapımında kullanılır.
  - `Ammo` (Cephane): Atölyede hurda ile üretilir; satranç savaşında zar avantajı sağlar.
- **Odalar (`BunkerRoom`):**
  - Türler: `Generator`, `WaterFilter`, `HydroponicFarm`, `Workshop`, `RadioRoom`, `Infirmary`, `Barracks`, `BunkerGate`.
  - Her odanın bir seviyesi (Level), çalışma durumu (Hasarlı, Aktif, Enerjisiz) ve atanmış işçi yuvaları (Slots) vardır.
- **Topluluk (`Survivor`):**
  - Nitelikler: `Id`, `Name`, `Profession` (Engineer, Farmer, Medic, Soldier, RadioOperator), `Morale` (0-100), `IsAssigned`.

#### 1.2 Taktik Satranç Savaş Motoru
- **Tahta (`TacticalBoard`):** 8x8 kareli ızgara. Bazı karelerde `CoverType` (None, Half, Full) engelleri bulunur.
- **Taş Hareket Disiplini (`ChessPiece`):**
  - `Pawn` (Piyade/Gözcü): İleri 1 kare (ilk hamlede 2), çapraz saldırı. 2 HP.
  - `Knight` (Motorize/İzci): Klasik L hareketi; engellerin ve siperlerin üstünden atlar. 3 HP.
  - `Bishop` (Keskin Nişancı): Çapraz hatlar boyunca sınırsız menzil (görüş hattı açıksa). 2 HP.
  - `Rook` (Ağır Silah/Kale): Düz hatlar boyunca hareket; arkasındakilere mobil siper sağlar. 5 HP.
  - `Queen` (Saha Komutanı): Her yöne 3 kare; çevresindeki dost taşlara +1 zar bonusu verir. 4 HP.
  - Düşman Taşları (YZ): `Drone` (At gibi hareket eder), `Sentinel` (Kale gibi düz ilerler), `Hunter` (Fil gibi çapraz kilitlenir).
- **Zar ve Çatışma Çözümü (`CombatResolver`):**
  - Saldırgan hedef kareye ateş ettiğinde D6 zarları atılır.
  - Siperde olmayan hedef: `4-5` = 1 Hasar, `6` = 2 Hasar (Kritik).
  - Yarı Siperde hedef: Vuruş için `5-6` gerekir. Tam Siperde: Sadece `6` isabet eder.
  - Cephane harcanarak fazladan zar eklenebilir.

#### 1.3 Dünya Haritası ve Sinyal Riski Modülü
- Haritada 12 sektör vardır. Her sektörün bir `AiThreatLevel` (0-100) ve `ScrapRichness` değeri bulunur.
- Saha ekipleri sektörlere gönderilir (ulaşma süresi, operasyon süresi).
- **Sinyal Riski (Signal Triangulation):** Telsiz her kullanıldığında küresel `SignalExposure` ibresi artar. Belli eşikte YZ sığınağa doğru arama birlikleri yollar.

---

### ADIM 2: Birinci Şahıs Masa ve Diyegetik Arayüz (Unity Presentation)

#### 2.1 Masa Kamerası ve Etkileşim
- Kamera komutanın göz hizasındadır; fare ile masanın farklı köşelerine hafif eğimle bakılabilir.
- **Odaklanma (Focus Zoom):** Telsize, Faksa veya Masanın Ortasına tıklandığında kamera yumuşakça o nesneye yaklaşır (Escape ile geri çekilir).
- Masadaki tüm öğeler fiziksel 3D objelerdir (Colliders + Unity Events / Pointer Interfaces).

#### 2.2 Masanın Modüler Orta Alanı (Central Workspace State Machine)
Masanın ortasında aynı anda tek bir büyük nesne aktif olur; aralarında fiziksel geçiş animasyonları vardır:
1. **Durum A: Katlanmış Dünya Haritası**
   - Masa üstüne serilmiş eski bir kağıt harita. Üzerinde raptiyeler, rotaları gösteren kırmızı ipler ve ekiplerin ahşap pulları.
2. **Durum B: Mimari Blueprint Paftası (Sığınak Ekranı)**
   - Rulo halinde çekilip açılan büyük mavi teknik çizim paftası.
   - Sığınağın yeraltı kesiti el çizimi şemalarla görünür.
   - Kurtulanların kartvizitleri sürükle-bırak (Drag & Drop) ile ilgili odaların yuvalarına yerleştirilir.
3. **Durum C: Taktik Satranç Tahtası**
   - Çatışma anında masanın ortasına çekilen ahşap satranç tahtası.
   - Ahşap dost piyonlar ve soğuk metal düşman piyonları.
   - Masanın kenarında deri bir bardak içinde sallanıp masaya atılan 3D fiziksel D6 zarları.

#### 2.3 Sol: Analog Faks / Teleks Cihazı
- İçeriden mekanik motor sesi gelir, üst yarıktan beyaz rulo kağıt yavaşça masaya doğru çıkar.
- Kağıtta daktilo fontuyla saha raporları yazılıdır.
- Oyuncu kağıdı tıklar, okur; masadaki "ONAYLANDI" veya "ARŞİV" damgasını basar.

#### 2.4 Sağ: Vakum Tüplü Telsiz İstasyonu
- Açma/kapama mandalı (tok bir "klik" sesiyle tüplerin sarı ışığı yanar).
- Frekans arama kadranı (çevrildikçe beyaz gürültü ve parazit sesleri değişir, doğru frekansta net insan konuşması veya mors sinyali duyulur).
- Titreyen analog ampermetre/voltmetre ibresi (YZ Sinyal Tehdidi).

---

### ADIM 3: Kademeli Öğrenme Eğrisi (Progressive Unfolding & Senaryo)

Oyun açıldığında tüm arayüz kapalıdır; sistemler oyuncu keşfettikçe sırayla hayat bulur:

- **1. Faz (Karanlık ve Telsiz - 0-10 Dk):**
  - Sığınak zifiri karanlıktır. Tek görünen şey acil durum el feneridir.
  - Oyuncu masanın altındaki jeneratör kolunu çeker. Lamba titreyerek yanar.
  - Sağdaki telsizden cızırtılı bir SOS sinyali gelir. Kadran çevrilir, frekans yakalanır: Yüzeyde mahsur kalmış 2 keşif erinin sesidir.
- **2. Faz (İlk Satranç Karşılaşması - 10-20 Dk):**
  - Mahsur kalan ekip bir YZ Gözcü Dronu ile karşılaşır.
  - Telsizdeki asker: *"Komutanım, ne yapalım? Karşımızda bir dron var!"*
  - Masanın alt çekmecesinden küçük bir ahşap tahta çıkar: 2 Ahşap Piyon ve 1 Metal Dron.
  - Oyuncu piyonu sürer, zarları masaya atar, siper mantığını öğrenir ve dronu etkisiz hale getirir. Askerler hurda parçası ele geçirir.
- **3. Faz (Faks ve Dünya Haritası - 20-35 Dk):**
  - Faks cihazı tıkır tıkır çalışır: Düşen uydudan alınan ilk bölge koordinatları basılır.
  - Oyuncu masaya katlanmış bölge haritasını serer. Harita mekaniği açılır: Ekipleri farklı sektörlere keşfe yollama.
- **4. Faz (Blueprint ve Sığınak Yönetimi - 35-50 Dk):**
  - Askerler sığınağa döner ve yanlarında kurtardıkları bir mühendis getirir.
  - Masaya büyük mavi mimari pafta (Blueprint) serilir.
  - Oyuncu jeneratör odasını onarır, mühendisi atar ve ilk Hidroponik Sera inşasını başlatır.
- **5. Faz (YZ Karşı Taarruzu & Tam Döngü):**
  - YZ sığınağın telsiz sinyalini fark eder; tehdit ibresi kırmızıya yaklaşır.
  - Oyuncu artık:
    - Sığınağın kaynaklarını dengede tutmak,
    - Telsizi gizlilikle kullanmak,
    - Haritada ekipleri yönlendirip satranç savaşlarını kazanmak,
    - Nihai hedef olan YZ ana veri merkezlerine sızmak zorundadır.

---

## 3. Claude Master Prompt'ları

Bu projeyi Claude'a yaptırırken aşağıdaki promptları sırasıyla veya doğrudan ana prompt olarak verebilirsiniz.

### 🌟 TEK SEFERDE PROJEYİ BAŞLATAN "CLAUDE MASTER PROMPT"

```markdown
Sen deneyimli bir Baş Oyun Mimarı ve Unity / C# Uzmanısın.
Biz sıfırdan "Sığınak: Sıfır-Ağ (Bunker: Zero-Net)" adında, birinci şahıs (FP) diegetik bir komuta masası başında geçen, sığınak yönetimi ve sıra tabanlı masaüstü satranç savaşı içeren bir oyun geliştiriyoruz.

Oyunun temel ilkeleri:
1. Tematik: YZ tüm dijital ağları ele geçirmiştir. İnsanlar analog dünyaya dönmüştür (bilgisayar/yazılım yok; faks, telsiz, kağıt paftalar, piyonlar ve zarlar var).
2. Görsel & Arayüz: Birinci şahıs masaya açılı bakış. HUD yok; her şey masadaki fiziksel nesnelerdir.
3. Sığınak Yönetimi: Masada açılan büyük bir mavi mimari çizim paftası (Blueprint) üzerinde oda inşası, kaynaklar (Mazot, Su, Gıda, Hurda, Cephane) ve insan görevlendirme.
4. Taktik Savaş: Masaya çekilen satranç tahtasında sıra tabanlı savaş. Figürler satranç taşları gibi hareket eder (Piyon, At, Fil, Kale, Vezir); ancak can puanları (HP), siperler ve D6 zarlarıyla hasar hesaplanır.
5. Kademeli Açılma: Oyun karanlıkta başlar, jeneratör -> telsiz -> ilk satranç savaşı -> faks & harita -> blueprint & sığınak şeklinde adım adım açılır.

Geliştirmeyi 3 ana faza bölerek inşa edeceğiz:
- FAZ 1: Unity'den bağımsız saf C# (POCO) Çekirdek Simülasyon Motoru (Sığınak Kaynakları, Satranç Savaş Motoru, Deterministik Zar & Testler).
- FAZ 2: Unity Birinci Şahıs Masa & Diegetik Arayüz Sistemi (FP Kamera, Modüler Masa Değiştirici: Harita/Blueprint/Satranç, Faks ve Telsiz etkileşimi).
- FAZ 3: Kademeli Açılma ve Senaryo Durum Makinesi (Tutorial / Unfolding Event Dispatcher).

İlk olarak FAZ 1 ile başlıyoruz. Bana Unity'den tamamen bağımsız, yüksek test kapsamına sahip, temiz mimarili (Clean Architecture) şu C# sınıflarını ve birim testlerini (NUnit/xUnit uyumlu) eksiksiz yaz:
1. Kaynak ve Sığınak Odaları Sistemi (`BunkerModel`, `Room`, `Survivor`, `ResourcePool`)
2. Taktik Satranç Savaş Motoru (`TacticalBoard`, `ChessPiece`, `PieceType`, `CombatResolver`, `DiceRoller`)
3. Test Dosyaları: Kaynak tüketim döngüsü testi ve satrançta Atın L hareketi, siper hesaplama ve zar ile HP düşüşünü doğrulayan testler.
Lütfen kodu yarım bırakmadan, üretime hazır ve eksiksiz olarak üret.
```

---

### Modüler Claude Promptları (Adım Adım Geliştirme İçin)

#### Prompt 1: Çekirdek Simülasyon Motoru (Pure C#)
```markdown
Görev: "Sığınak: Sıfır-Ağ" oyunu için Unity'den tamamen bağımsız (Pure C#) bir Çekirdek Simülasyon Motoru oluştur.
Gereksinimler:
1. Sığınak Kaynakları: Mazot, Su, Gıda, Hurda, Cephane yönetimi. Günlük tüketim ve tükenme durumları.
2. Odalar ve Topluluk: Jeneratör, Sera, Atölye, Revir, Telsiz Odası vb. Hayatta kalanların uzmanlıklarına göre odalara atanması.
3. Taktik Satranç Sistemi: 8x8 ızgarada satranç hareket kurallarına sahip figürler (Pawn, Knight, Bishop, Rook, Queen ve Düşman Dronları). Her taşın HP'si, masadaki engeller (Half/Full Cover) ve D6 zar atışı ile çatışma çözümü (CombatResolver).
4. Kod tamamen POCO (Plain Old C# Object) olmalı, RNG tohumu alarak deterministik çalışmalıdır.
Bu mimariyi oluşturan sınıfları ve doğrulama testlerini yaz.
```

#### Prompt 2: Unity Masa ve Diyegetik Arayüz (Presentation)
```markdown
Görev: "Sığınak: Sıfır-Ağ" için Unity URP uyumlu Birinci Şahıs Komuta Masası (Diegetic Desk) arayüz mimarisini hazırla.
Gereksinimler:
1. FirstPersonDeskCamera: Masaya açılı bakan kamera. Fare hareketiyle hafif açı değiştirme ve odaklanılabilir nesnelere (Faks, Telsiz, Masa Ortası) tıklandığında yumuşak zoom (Cinemachine veya Lerp).
2. DeskWorkspaceController: Masanın ortasındaki çalışma alanını yöneten State Machine. 3 modu yönetecek:
   - State_Map: Dünya haritası açık (pinler, ekipler).
   - State_Blueprint: Mimari pafta açık (yeraltı odaları, sürükle-bırak işçi yerleştirme).
   - State_ChessBoard: Taktik satranç tahtası açık (figürleri karelere tıklayıp hareket ettirme, masaya 3D zar fırlatma).
3. AnalogFaxMachine: Yeni rapor geldiğinde kağıt çıkarma animasyonu tetikleyen, kağıt tıklanınca okuma görünümüne alan bileşen.
4. VacuumTubeRadio: Frekans kadranı döndürülebilen, doğru frekansa gelince ses çalan ve sinyal ibresini güncelleyen kontrolcü.
Bu mimarinin C# MonoBehaviour scriptlerini ve etkileşim arayüzlerini (IInteractable, IDraggable) yaz.
```

#### Prompt 3: Kademeli Açılma (Unfolding) ve Senaryo Yöneticisi
```markdown
Görev: "Sığınak: Sıfır-Ağ" için oyunun ilk 30 dakikalık kademeli açılma (Progressive Unfolding) akışını yönetecek senaryo durum makinesini kodla.
Akış:
- Adım 1: Jeneratör Şalteri açılır -> Lamba yanar -> Telsiz aktif olur.
- Adım 2: Telsizde frekans yakalanır -> SOS mesajı alınır -> Masaya satranç tahtası iner.
- Adım 3: İlk taktik çatışma kazanılır -> Hurda toplanır -> Faks ilk haritayı basar.
- Adım 4: Masaya harita serilir -> Sığınak blueprint paftası açılır -> İlk oda inşa edilir.
Bu senaryo zincirini yöneten `UnfoldingDirector`, `GameEvent` ve hedef/görev takip sistemini C# ile yaz.
```
