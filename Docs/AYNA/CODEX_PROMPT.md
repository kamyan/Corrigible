# AYNA · Codex uygulama talimatı

> **Bu dosya Codex (ya da başka bir kodlama ajanı) için uygulama talimatıdır.**
> - Önce `AGENTS.md`'yi oku: proje kuralları, komutlar, tuzaklar.
> - Bu belge AGENTS.md ile çelişirse yeni `Ayna` modülü için bu belge geçerlidir, eski oyun için AGENTS.md.
> - Kural kaynağı `Docs/YZ-SAVASI-OYUN-FIKRI.md`. Bu belge onu uygulanabilir hâle getirir ve boşluklarını doldurur. İkisi çelişirse **bu belge** geçerlidir.
> - Kod tanımlayıcıları İngilizce; yorumlar, belgeler ve oyun metinleri Türkçe (AGENTS.md).
> - Sürüm 1.0 (17.10.2026).

**İçindekiler**
- 0 Görev ve çalışma biçimi
- 1 Oyun özeti
- 2 Mimari
- 3 Kurallar
- 4 AYNA'nın aklı
- 5 Denge değerleri
- 6 Fazlar ve kabul ölçütleri
- 7 Raporlama
- 8 İlk yanıtın (F0)

---

## 0. Görev ve çalışma biçimi

### Görev

AYNA oyununu Unity projesinde **yeni ve bağımsız bir modül** olarak kur. Kapsam dört parça:
- oyunun kuralları (dünya, döngü, emirler, savaş tahtası);
- rakip yapay zekâ AYNA (tahmin modeli, planlayıcı, tahta yapay zekâsı);
- AYNA'nın oyun içi ve oyunlar arası öğrenmesi;
- görünüm modelleri ve oynanabilir bir hata ayıklama sahnesi.

Asıl arayüz bu görevin dışında. Fütüristik (Fx) tasarımı Claude Design'da hazırlanıyor; istem `Docs/UI_Fx/AYNA_TASARIM_PROMPT.md`'de. Tasarım paketi gelince ayrı bir talimatla uygulanacak. Senin görünüm modellerin o brifteki karelerin (A1–A12) verisini taşımalı.

### Çalışma biçimi

- **Fazlar F0–F5** (§6). Her faz sonunda dur, Türkçe rapor ver (§7) ve onay bekle.
- **Kayıtlar:**
  - kararlar `Docs/AYNA/KARARLAR.md`: her karar tek satır, gerekçesi gerekirse altında;
  - durum `Docs/AYNA/DURUM.md`;
  - denge çıktıları `Docs/AYNA/denge/`.
- **Belirsizlikte:**
  - tasarım belgesindeki öneriyi uygula ve kararı KARARLAR'a yaz;
  - oyunu belirgin biçimde değiştiren bir boşluksa soru olarak bir sonraki faz raporuna koy.

### Alınmış kararlar

Kullanıcı tasarım belgesindeki §14 önerilerini kabul etmiş sayılır:
1. **Eşzamanlı döngü.**
2. **Taktik savaş:** AYNA niyetini önceden gösterir, üstüne gizli tuzaklar.
3. **İnsan farkındalığı katmanı** hafif hâliyle var.
4. **Yeni oyun ayrı modül.**
   - Eski oyun dokunulmadan kalır: `Assets/Corrigible/Core`, `Game`, `UI`, `Desk`; `Main`, `DeskGame` ve `Baskin` sahneleri.
   - Eski oyunun kaldırılması ayrı ve kullanıcı onaylı bir iştir.
5. **JS prototipi yok.** AYNA'nın tek kaynağı C# çekirdektir. AGENTS.md Kural 1'deki parite AYNA'ya uygulanmaz.

### Kapsam dışı

- Fx arayüzü, sanat ve ses.
- Eski oyunu silmek ya da değiştirmek.
- Ağ üzerinden çok oyunculu oyun. Yine de mimari, iki oyunculu yerel oyunu ileride mümkün kılmalı (§2.4).
- Steam başarımları: yalnız kimlik listesi öner.

### Yasaklar ve izin gerektirenler

- **Git:** commit, push ya da dal işlemi yok; kullanıcı ister.
- **Kullanıcının çalışmaları:**
  - Çalışma ağacında kullanıcının commit edilmemiş değişiklikleri var (ör. `Core/Balance.cs`, `Core/GameData.cs`). Onlara dokunma, geri alma, biçimlendirme.
  - Kullanıcının kaydedilmemiş sahne değişikliği varken test koşturma ya da sahne açma. Onun değişikliğini kaydetme ya da atma.
- **Önce sor:**
  - paket eklemek;
  - Project / Player / Quality / URP ayarlarını değiştirmek;
  - sistem aracı kurmak (dotnet, node…).
- **Sahne ve prefab:**
  - Mevcut sahne ve prefab'lara dokunma.
  - `.unity` ve `.prefab` YAML'ını elle yazma. Tek seferlik editör script'iyle kur, çalıştır, sil (AGENTS.md Kural 5).
- **Play testleri:**
  - Önce `Application.persistentDataPath` altındaki `savegame.json`, `player_profile.json` ve `ayna_profile.json`'u yedekle.
  - Testten sonra dosyayı yalnız hâlâ testin bıraktığı durumdaysa geri yükle (mtime ve içerik kontrolü). Kullanıcının daha yeni kaydını ezme.
  - Play'deyken derleme gerekiyorsa önce Play'den çık.

---

## 1. Oyun özeti

Ayrıntı `Docs/YZ-SAVASI-OYUN-FIKRI.md`'de. Bu bölüm yalnız yön verir; uygulanacak kurallar §3'te.

- **Taraflar:**
  - **Oyuncu:** altın YZ.
  - **Rakip:** mavi YZ, **AYNA**.
- **Dünya:** 12 bölge, ~70 sunucu.
  - Her bölge bir veri türü üretir: Görü, Hareket, Enerji, Ağ, Dil.
  - Veri türü, eğitilebilecek birim ailesini belirler.
- **Döngü:** iki taraf her döngüde emirlerini **gizlice ve aynı anda** verir; planlar birlikte açılır ve çözülür.
- **Savaş:** iki taraf aynı bölgede karşılaşınca 8×8 tahtada taktik savaş açılır.
  - AYNA niyetini önceden gösterir.
  - Oyuncunun gideceğini tahmin ettiği karelere gizli tuzak koyar.
- **Ana fikir: AYNA oyuncuyu tahmin eder.**
  - Bu oyunda ve önceki oyunlarda gördüklerinden bir oyuncu modeli kurar.
  - En güçlü tahminleri oyuncuya yüzdeleriyle gösterilir.
  - AYNA'nın %15'in altında beklediği bir hamle **Sürpriz** ödülü verir.
- **İnsan farkındalığı:** gürültülü hamlelerle artar. 100'de iki taraf da kaybeder.
- **Zafer:** hakimiyet (sunucuların %60'ı), silme (rakibin çekirdeği) ya da tekillik (ilan edilen 4 döngülük eğitim).

---

## 2. Mimari

### 2.1 Klasör ve assembly

```
Assets/Corrigible/Ayna/
  Core/            Corrigible.Ayna.Core      noEngineReferences: true · kurallar, durum, görüş, emirler, çözüm, tahta, otomatik çözüm
  Mind/            Corrigible.Ayna.Mind      noEngineReferences: true · oyuncu modeli, tahmin, planlayıcı, tahta yapay zekâsı, test botları
  Game/            Corrigible.Ayna.Game      Unity: oyun denetleyicisi, kayıt, profil dosyası, görünüm modelleri, hata ayıklama arayüzü
  Editor/          Corrigible.Ayna.Editor    kendi kendine oynatma menüsü, denge raporu, geçici kurucular
  Tests/EditMode/  Corrigible.Ayna.Tests
  Scenes/          AynaDebug.unity           F4
  Settings/        AynaBalance.asset         F4
```

- **Bağımlılıklar:** `Mind` → `Core`; `Game` → `Core` + `Mind`. `Core` hiçbir zaman `Mind`'a bağlanmaz.
- **Ortak yardımcılar:** eski `Corrigible.Core`'dan yalnız `Rng` (mulberry32), `Js.Round` ve `Loc` kullanılır (asmdef referansı). Eski oyunun kurallarını (`Simulation`, `GameData`, `Baskin`) çağırma.
- **Veri kopyalama:**
  - Bölge ve bağlantı verisini eski `GameData.Regions` / `Links` / `Satellites`'tan **kopyala**, referans verme.
  - `Baskin.cs`'deki kopyalanabilir durum, geri alma ve önizleme kalıbı iyi bir örnek, ama kuralları farklı.

### 2.2 Determinizm

- AGENTS.md Kural 2 AYNA çekirdeği için de geçerli:
  - sayılar `double`;
  - yuvarlama `Js.Round`;
  - `System.Random`, `DateTime` ve `float` yok.
- **Üç ayrı rastgelelik akışı.** Biri değişince ötekiler kaymasın.
  - **Dünya** `WorldRng`: kurallar.
  - **Akıl** `MindRng`: AYNA'nın kararları.
  - **Tahta** savaş başına: `seed ⊕ döngü ⊕ bölge`.
- **Kararlı sıralama:** sözlük gezinme sırasına güvenme; sıralamaları kimlikle sabitle.
- **Durum özeti:** `WorldState.Hash()` testlerde aynı girdinin aynı durumu verdiğini kanıtlar.

### 2.3 Adalet sınırı: görüş

- **Görüş:** `SideView View(Side side)`, bir tarafın gördüğü dünyadır.
  - **AYNA'nın aklı yalnız `SideView` ve kendi gözlem geçmişini alır.** Gerçek `WorldState`'e erişemez.
  - Erişimi API ile zorla: `WorldState` üyeleri `internal`; `InternalsVisibleTo` yalnız testlere.
- **Oyuncu arayüzü de aynı ilkeye uyar:** görünüm modelleri `View(Player)`'dan üretilir. Gerçek dünya yalnız oyun sonunda açılır: `View(Side, revealAll: true)`.
- **Test:** yalnız gizli bilgide farklı iki dünya, AYNA'ya aynı planı ürettirmeli.
- **Planlayıcı gizli bilgiyi tahmin eder:** ileri benzetimini görüşüyle tutarlı örneklenmiş bir **inanç durumu** üstünde yapar.

### 2.4 Komut, kayıt ve yeniden oynatma

- **Durum yalnız komutlarla değişir:**
  - `ResolveCycle(Plan player, Plan ayna)`;
  - savaşlarda tahta komutları;
  - savaş başına “Komuta et / Otomatik çöz” seçimi.
- **AYNA'nın planı belirlenimcidir.** Şu girdiler tarafından belirlenir:
  - tohum;
  - oyun başındaki profil anlık görüntüsü;
  - ayarlar;
  - AYNA'nın gözlemleri.
- **Kayıt:**
  - tohum;
  - ayarlar ve iki tarafın mimari ve başlangıç seçimleri;
  - oyun başındaki AYNA profil anlık görüntüsü;
  - oyuncunun döngü planları ve tahta girdileri.
- **Yeniden oynatma:** kayıt oyunu bit düzeyinde aynı yeniden kurar (test).
- **Planı kim üretir:** `ResolveCycle` iki planı da dışarıdan alır. AYNA'yı denetleyici (`Game`) ya da kendi kendine oynatma çağırır. Böylece ileride iki insan da oynayabilir.

### 2.5 Denge değerleri

- **Sihirli sayı yok.** Bütün sayılar `AynaBalance`'ta (§5).
  - `Core`'da varsayılanlı düz C# sınıfı.
  - F4'te `Settings/AynaBalance.asset` ile düzenlenir.
- **Kayıt:** kaydın içinde denge değerlerinin anlık görüntüsü de durur.

### 2.6 Metin

- **Oyuncuya görünen her metin `Loc` ile** (AGENTS.md Kural 9). Bu, AYNA'nın tahmin cümlelerini ve günlük satırlarını da kapsar.
- **TR ve EN birlikte.** İngilizce karşılıklar `Resources/Localization/strings.txt`'e eklenir.
- **Karar metinden bağımsız:** simülasyon hiçbir kararı metne göre vermez.

---

## 3. Kurallar

### 3.1 Dünya

- **Bölgeler:** eski oyunun 12 bölgesi ve kimlikleri, sunucu sayısı ve veri türüyle.

| Kimlik | Bölge | Sunucu | Veri |
|---|---|---|---|
| `na` | Kuzey Amerika | 8 | Dil |
| `sa` | Güney Amerika | 6 | Enerji |
| `weu` | Batı Avrupa | 7 | Dil |
| `eeu` | Doğu Avrupa | 5 | Ağ |
| `ru` | Avrasya | 6 | Enerji |
| `me` | Orta Doğu | 5 | Enerji |
| `af` | Sahra Altı Afrika | 4 | Ağ |
| `in` | Güney Asya | 8 | Ağ |
| `cn` | Doğu Asya | 8 | Hareket |
| `jk` | Japonya-Kore | 5 | Hareket |
| `sea` | Güneydoğu Asya | 6 | Görü |
| `oc` | Okyanusya | 4 | Görü |

- **Bağlantılar:**
  - `GameData.Links`'ten kopyala. Kablo bayrağı “deniz altı”, diğerleri “kara”; davranışları aynı, yalnız görünüşte ayrılır.
  - `GameData.Satellites`'tan kopyalanan uydu hatları ancak Görü kademe 3 ile o taraf için açılır.
- **Sunucu:** her biri boş, altın ya da mavi. Bölge düzeyinde sayı olarak tutulur: `Free`, `Owned[side]`.
- **Çekirdek:** her tarafın tam olarak bir çekirdek bölgesi var.
  - Çekirdek, sahibinin en az bir sunucusu olan bir bölgede durur.
  - Bölgede sahibinin sunucusu kalmazsa çekirdek, sahibinin en çok sunucusu olan bölgeye kaçar (eşitlikte kimlik sırası).
  - Kaçabileceği yer yoksa taraf kaybeder.

### 3.2 Başlangıç

1. **Seçimler.** İki taraf mimarisini ve başlangıç bölgesini **aynı anda** seçer.
   - AYNA oyuncunun seçimini bilmez; profilinden tahmin eder.
   - Başlangıç bölgeleri arasında en az 2 bağlantı olmalı. Çakışırsa AYNA kendi sıralı listesindeki bir sonraki uygun bölgeye geçer.
2. **Taraf başına başlangıç:**
   - başlangıç bölgesinde 3 sunucu ve çekirdek;
   - 6 İşlem;
   - bir birlik;
   - mimarinin kademeleri.

| Mimari | Başlangıç kademeleri | Başlangıç birliği | Özellik |
|---|---|---|---|
| **Sürü Zihni** | Görü 1, Enerji 1 | Gözcü Dron, EMP Dronu, EMP Dronu | dron üretimi −1 İşlem; Yayıl en fazla 4; duvar gücü −1 (en az 0); gürültü ×1,5 |
| **Kale** | Görü 1, Hareket 1 | Gözcü Dron, Muhafız Robot, Muhafız Robot | başlangıç bölgesinde duvar 2; Duvar ör 2 İşlem; Yayıl en fazla 2 |
| **Hayalet** | Görü 1, Dil 1 | Gözcü Dron, Yem, Yem | gürültü ×0,5; Gizlen 1 İşlem; Sahte yığınak 1 İşlem |

Görü 1'i herkese vermek ve mimari özellikleri bu belgenin boşluk doldurmasıdır. F1 dengesine göre ayarlanabilir; değişikliği KARARLAR'a yaz.

### 3.3 Döngü ve çözüm sırası

**Evreler:**
- **PLANLA:** iki plan da alınır.
- **AÇ:** iki tarafın görüşüne, gördüğü kadarıyla rakibin planı eklenir.
- **ÇÖZ:** aşağıdaki sıra.

**Önerilen çözüm sırası.** F0'da gözden geçir; değiştirirsen gerekçesini yaz.

1. **Doğrula ve öde.** Geçersiz ya da parası yetmeyen emir düşer; sırayla ödenir, sığmayan düşer.
2. **Bayraklar.** Gizlen ve Sahte yığınak bayrakları işlenir; bunlar yalnız görüşü etkiler.
3. **Birlik yürüt** (eşzamanlı). Bağlantıda karşılaşma yok. Döngü sonunda aynı bölgede iki tarafın birliği varsa **karşılaşma savaşı** açılır.
4. **Yayıl teklifleri.**
   - Bir bölgeye gelen istekler toplamı boş sunucuyu aşmıyorsa herkes alır.
   - Aşıyorsa daha çok İşlem veren alabildiği kadar alır, kalanı öteki alır.
   - Eşitlikte kimse alamaz.
   - Kaybedenin ve eşitlikte iki tarafın harcadığı yanar.
5. **Saldırılar.** Bölge başına döngüde en fazla bir savaş; karşılaşma varsa o sayılır.
   - Hedefte savunan birlik varsa savaş açılır: oyuncu için Komuta et ya da Otomatik çöz; AYNA'ya karşı AYNA'da her zaman otomatik.
   - Birlik yoksa duvarlara karşı otomatik çözüm yapılır.
6. **Savaş sonuçları uygulanır** (§3.6).
7. **Duvar ör, Eğit (ilerleme), Üret, onarım, Çekirdeği taşı (ilerleme).**
8. **Gelir.**
9. **Gürültü ve farkındalık** (§3.7): Tarama, Kesinti, Son Şalter.
10. **Zafer ve yenilgi denetimi** (§3.9).
11. **Gözlemler.** İki tarafın gözlem geçmişine yalnız gördükleri yazılır. AYNA'nın oyun içi modeli güncellenir.

### 3.4 Emirler

| Emir | İşlem | Gürültü | Koşul ve etki |
|---|---|---|---|
| Yayıl | 2 / sunucu, en fazla 3 | 1 | hedef bölgede sunucun var ya da sunucun olan bir bölgeye bağlantılı |
| Birlik yürüt | 1 | 0 | bir bağlantı ilerler; birlik döngüde tek emir alır |
| Saldır | 4 | 2 | birliğin bulunduğu ya da komşu bölgede rakibin sunucusu olmalı (komşuysa birlik o bölgeye geçer) |
| Duvar ör | 3 | 0 | bölgede sunucun olmalı; bölge duvarı +1 (en fazla 3); başarılı her saldırı −1 |
| Eğit | 6 / 12 / 20 | 0 | kademe 1 / 2 / 3; süre 1 / 2 / 3 döngü; gerekli sunucu ≥2 / ≥4 / ≥6; taraf başına aynı anda bir eğitim |
| Üret | birime göre (§5) | 0 | ailenin eğitilmiş olması + o veri türünden en az 1 sunucu (“üretim hattı”); birim, sunucun olan bir bölgedeki birliğe katılır |
| Yeni birlik | 8 | 0 | en fazla 3 birlik; boş birlik, sunucun olan bir bölgede kurulur |
| Onar | 1 / birim | 0 | birlik kendi bölgende; can tam dolar |
| Gizlen | 2 | — | bir bölge: bu döngü o bölgeyle ilgili emirlerin rakibe görünmez, gürültüsü 0 |
| Sahte yığınak | 2 | 1 | rakibin görüşünde o bölgede saldırı hazırlığı varmış gibi görünür |
| Çekirdeği taşı | 10 | 1 | 2 döngü sonra başka bir bölgene geçer; taşınırken kaynakta sayılır |

### 3.5 Savaş

**Kadro ve birimler**
- **Kadro:** birlikten 3 birim (Hareket kademe 3 ile 4). Birlik en fazla 6 birim taşır. Hasar kalıcıdır; düşen birim silinir.
- **Sınıf üçgeni:** kara > hava > siber > kara. Üstün sınıf, saldırdığı birime +1 hasar verir. Muhafız'ın dronlara +1'i bunun yerine geçer, üstüne eklenmez.

| Birim | Aile / kademe | Sınıf | Can | Hareket | Yetenek |
|---|---|---|---|---|---|
| Gözcü Dron | Görü 1 | hava | 2 | 4 | uçar (engel aşar); 3 kare çevresindeki tuzakları ve gizli niyetleri açar |
| Nişancı Dron | Görü 2 | hava | 2 | 3 | düz hatta 2–6 kare, ilk engele kadar, 2 hasar |
| Muhafız Robot | Hareket 1 | kara | 4 | 2 | zırh (aldığı hasar −1, en az 1); bitişik 1 hasar |
| Örümcek Robot | Hareket 2 | kara | 3 | 3 | duvar ve sunucu rafı aşar; bitişik 1 hasar + 1 kare itme |
| EMP Dronu | Enerji 1 | hava | 2 | 3 | 2–4 kare uzağa 3×3 alan: robotlar 1 tur, dronlar 2 tur sersemler (hasar yok) |
| Aşırı Yük | Enerji 2 | hava | 1 | 4 | kendini patlatır: 3×3 alana 2 hasar; gürültü +1 |
| Sızma Birimi | Ağ 1 | siber | 2 | 3 | bitişik robotu bir tur ele geçirir (o tur oyuncu oynatır) |
| Röle | Ağ 2 | siber | 2 | 3 | 2 kare çevresindeki dost birimlere +1 hareket; durduğu düğümü o tur alır |
| Yem | Dil 1 | siber | 1 | 3 | AYNA'nın bu tur seçeceği niyetler önce Yem'i hedefler (menzildeyse) |
| Hayalet Emir | Dil 2 | siber | 2 | 3 | 4 kare içindeki bir AYNA biriminin niyetini başka geçerli bir hedefe çevirir |

**Tahta**
- **Izgara:** 8×8.
- **Kareler:** zemin, sunucu rafı (geçilmez), sivil sunucu (geçilmez, can 1, kırılınca gürültü +1).
- **Taraflar ve düğümler:**
  - İki tarafın konuş bölgeleri karşı köşelerde.
  - 3 sunucu düğümü var. Başlangıç sahipliği, bölgedeki sunucu oranına göre.
- **Tur yapısı**, 6 tur:
  1. AYNA birimleri niyetlerini gösterir: hedef kare, tür, sıra.
  2. Oyuncu bütün birimleriyle hareket eder ve yetenek kullanır. Bu evrede geri alma serbest.
  3. Niyetler sırayla uygulanır.
  4. Düğüm sahiplikleri güncellenir: bir düğüm, üstünde son duran tarafındır. İki taraf da bitişikse sahiplik değişmez.
- **İlk vuruş** (Sürpriz ödülü): 1. turda oyuncu, AYNA niyetlerini göstermeden önce bir kez oynar.
- **Tuzaklar:**
  - AYNA savaş başında en fazla 2 kareye gizli tuzak koyar.
  - Yer seçimi oyuncu modelinin tahta ısı haritasından gelir (§4).
  - Tetiklenen tuzak 1 hasar verir ve birimi o tur durdurur.
  - Gözcü'nün görüşündeki tuzak görünür olur.
- **Bitiş:**
  - 6. turun sonunda ya da bir tarafın bütün birimleri düşünce.
  - Geri çekilme her an mümkün; çekilen taraf kaybeder ve düğümlerin hepsini rakip alır.
- **Tahta planları:**
  - En az 4 el yapımı şablon, aynalanmış ve döndürülmüş biçimleriyle.
  - Brifteki “Güney Asya” planı (`AYNA_TASARIM_PROMPT.md` › Örnek veri) şablonlardan biri.
  - Seçim tahta rastgelelik akışıyla yapılır.

### 3.6 Savaş sonucu ve otomatik çözüm

- **Taktik sonuç:** savaşın sonunda tutulan her düğüm için, düğümün sahibi bölgede bir sunucu alır.
  - Önce rakibin sunucusundan alır, yoksa boştan.
  - Bir taraf tahtada bütün birimlerini kaybettiyse rakibi 3 düğümün hepsini alır.
- **Otomatik çözüm:**
  - Kazanma olasılığı iki taraf gücünden hesaplanır: can toplamı, sınıf üçgeni, duvarlar ve savunana +%20 (`AynaBalance`).
  - Kazanan `Js.Round(0,6 × 3)` = 2 düğümlük sonuç alır, kaybeden 0 (ödülün %60'ı).
  - Kayıplar: kazanana 1, kaybedene 2 rastgele hasar; ölümlü olabilir.
- **Duvarlara karşı otomatik çözüm:**
  - Birlik gücü, duvar gücüyle (duvar × `wallStrength`) karşılaştırılır.
  - Saldıran kazanırsa duvar −1 ve 1 sunucu alır.
- **F2 kalibrasyonu:** otomatik çözümün olasılıkları, tahta benzetimlerinin sonuçlarıyla tutarlı olacak şekilde ayarlanır (§6, F2).
- **Çekirdek baskını:** savaş rakibin çekirdek bölgesindeyse tahtada çekirdek dolabı durur (can 5, zırhlı).
  - Kırılırsa **Silme zaferi**.
  - Çekirdek bölgesindeki savaş otomatik çözülürse dolap kırılmaz; yalnız sunucu sonucu uygulanır.

### 3.7 Gürültü ve insan farkındalığı

- **Farkındalık (0–100):**
  - döngü sonunda iki tarafın gürültü toplamı kadar artar (mimari çarpanlarıyla);
  - sonra 2 azalır.
- **Eşikler:**

| Eşik | Etki |
|---|---|
| ≥ 40 · Tarama | her döngü dünya akışıyla seçilen bir bölgenin bütün bilgisi iki tarafa da açılır |
| ≥ 70 · Kesinti | o döngüde en çok gürültü yapan tarafın bir bağlantısı 5 döngü kesilir; o tarafın en gürültülü bölgesine bitişik bağlantılardan, dünya akışıyla |
| 100 · Son Şalter | oyun biter, iki taraf da kaybeder |

### 3.8 Gelişim ve Tekillik

- **Kademeler:** beş aile × üç kademe.
  - Kademe 1 ve 2 birim açar.
  - Kademe 3 özel etki verir:

| Aile | Kademe 3 |
|---|---|
| Görü | uydu hatları + tahtada bütün tuzakları görür |
| Hareket | kadro 4 |
| Enerji | duvar gücü ×2 |
| Ağ | Yayıl en fazla 5 |
| Dil | AYNA'nın oyuncu tahminlerinin yarısını bozar. Gösterilen olasılıklar gürültülenir; Sürpriz hesabı gerçek olasılıkla yapılır |

- **Tekillik:**
  - Koşul: üç ailede kademe 3 + 30 İşlem. Süre 4 döngü.
  - Başlayınca iki tarafın görüşüne de ilan edilir.
  - Bir döngüde ilerlemesi için o üç ailenin her birinin veri türünden en az 2 sunucu gerekir; yoksa o döngü ilerlemez.

### 3.9 Zafer ve yenilgi

Denetim döngü sonunda, bu sırayla yapılır:
1. **Son Şalter:** farkındalık 100. İki taraf da kaybeder.
2. **Silme:** bir tarafın çekirdeği kırıldı ya da kaçacak yeri yok.
3. **Tekillik:** eğitim bitti.
4. **Hakimiyet:** dünyadaki sunucuların ≥ %60'ı.
5. **Süre:** 40. döngünün sonu. Daha çok sunucusu olan kazanır; eşitse berabere.

Aynı döngüde iki taraf da aynı koşulu sağlarsa:
- hakimiyette çok sunuculu taraf kazanır;
- tekillikte berabere.

---

## 4. AYNA'nın aklı

### 4.1 Oyuncu modeli

AYNA yalnız **gördüklerinden** öğrenir: kendi görüşünde açılan emirler, savaşlar, tahta hamleleri. Gizli hamleler ona bilgi vermez.

| Özellik | Örnek gösterim |
|---|---|
| Açılış | başlangıç bölgesi ve mimari dağılımı; ilk üç yayılım hedefinin veri türü |
| Hedef seçimi | veri türü tercihi; çekişmeli bölgeye girme eğilimi |
| Saldırganlık | ilk saldırı döngüsü dağılımı; döngü başına saldırı oranı |
| Tepki | saldırıya uğrayınca savun / karşı saldır / geri çekil oranları |
| Kadro | hava / kara / siber oranı; en sık birim |
| Tahta alışkanlığı | konuş köşesine göre normalize, ilk iki turdaki kare ısı haritası; ilk hedef türü |
| Blöf | sahte yığınak ve gizlenme sıklığı; sahte yığınakların gerçek saldırıya dönme oranı |

- **Gösterim:** sayım tabanlı, düzeltmeli (Dirichlet / Laplace) kategorik dağılımlar yeterli. İstersen bağlamlı basit bir softmax puanlayıcı kullan; hangisini seçtiğini KARARLAR'a yaz.
- **Oyunlar arası birleştirme:** oyun sonunda `yeni = 0,65 × eski + 0,35 × bu oyun`.
- **İlk oyun:** genel bir oyuncu ön dağılımıyla başlar.
- **Nesil:** her oyun bir nesil. Profilde nesil sayısı ve oyun skoru tutulur.

### 4.2 Tahmin, Sürpriz, Okunabilirlik

- **Tahmin:** model her döngü oyuncunun olası emirlerine olasılık verir; en azından emir türü × hedef bölge düzeyinde.
  - Olasılıklar toplamı 1 olan gerçek dağılımlardır.
  - En güçlü üç tahmin `Loc` cümleleriyle sunulur: “Bu döngü Güney Asya'ya saldıracaksın: %72”.
- **Sürpriz:** oyuncunun gerçek emri için AYNA'nın verdiği olasılık %15'in altındaysa Sürpriz olur. Döngü başına en fazla bir kez; en düşük olasılıklı emir sayılır.
  - Haritada: +3 İşlem.
  - Bu döngüde açılan ilk taktik savaşta: **ilk vuruş**.
- **Okunabilirlik:** son 5 döngüde, oyuncunun gerçek emirlerine AYNA'nın verdiği olasılıkların ortalaması (0–100).
- **Simetri:** Sürpriz, karşı tarafın bir oyuncu modeli olduğunda hesaplanır.
  - Gerçek oyunda yalnız AYNA oyuncuyu modeller, bu yüzden Sürprizi yalnız oyuncu kazanır.
  - AYNA'ya karşı AYNA'da iki taraf birbirini modeller ve ikisi de Sürpriz kazanabilir. Denge ölçümü böylece simetrik kalır.

### 4.3 Planlayıcı

1. **Tahmin:** oyuncunun bu döngüdeki olası planlarından N örnek çek.
2. **Aday üret:** sezgisel makro hamlelerle K aday plan üret. Örnekler:
   - en değerli boş sunuculara yayıl;
   - tahmin edilen hedefe duvar ör;
   - boş bırakılan bölgeye saldır;
   - eğitim yolunu sürdür;
   - Tekillik başlat;
   - çekirdeği kaçır.
3. **Değerlendir:** her adayı her örneğe karşı, `ResolveCycle`'ın **inanç durumu** üstündeki kopyasıyla bir döngü ileri benzet. Savaşlar otomatik çözümle.
4. **Puanla:** sonucu kişilik ağırlıklı bir değer işleviyle puanla:
   - sunucu payı;
   - İşlem;
   - veri erişimi;
   - çekirdek güvenliği;
   - Tekillik ilerlemesi;
   - farkındalık riski.
5. **Seç:** beklenen değeri en yüksek adayı seç. Eşitlik `MindRng` ile bozulur.

**Kişilik** (AYNA'nın mimarisiyle gelir)

| Mimari | Kişilik |
|---|---|
| Sürü | saldırgan; farkındalık riskine az duyarlı |
| Kale | savunmacı; Tekillik ağırlıklı |
| Hayalet | sahte yığınak ve gizlenme; kendi hamlelerini öngörülemez tutar (iyi adaylar arasında daha çok rastgele seçer) |

**Mimari seçimi:** AYNA kendi mimarisini profile göre seçer. Oyuncunun en çok oynadığı mimariye üçgende üstün gelen mimari; profil yoksa rastgele.

**Başlangıç karşı hamleleri:** profilden en fazla 2. Örnekler:
- oyuncunun alışkanlık başlangıç bölgesine bakan bölgelere duvar;
- oyuncunun kadro eğilimine üstün ailenin kademe 1'i.

Hangisini neden seçtiği, gizli olarak A1 görünüm modelinde tutulur ve oyunda açılır.

**Öğrenme düzeyi** (ayar)

| Düzey | Davranış |
|---|---|
| Kapalı | yalnız ön dağılım; profil okunmaz ve yazılmaz |
| Hafif | oyunlar arası ağırlık yarıya iner; karşı hamle en fazla 1 |
| Tam | her şey açık |

**Hafızayı sil:** profili sıfırlar.

**Bütçe:** planlayıcı düzenleyicide döngü başına ortanca ≤ 50 ms. N ve K `AynaBalance`'ta.

### 4.4 Tahta yapay zekâsı

- **Niyet seçimi:** her AYNA birimi için aday eylemler üretilir.
  - Niyetler, oyuncunun olası cevabına karşı (tahta alışkanlığı + açık durum) puanlanan sınırlı bir aramayla seçilir.
  - Önerilen yöntem: açgözlü ya da küçük ışın araması (beam).
  - Hedefler: düğüm, birim ve sivil kaçınma.
- **Tuzak yeri:** ısı haritasında oyuncunun ilk iki turda en sık durduğu, konuş köşesine göre normalize edilmiş kareler. Geçilebilir ve düğüm olmayan kareler seçilir.
- **Hayalet Emir ve Yem'e saygı:** niyetler bu etkilere göre yeniden hedeflenebilmeli (§3.5).
- **Bütçe:** tur başına ortanca ≤ 30 ms.

### 4.5 Test botları

Kendi kendine oynatma ve öğrenme deneyleri için `Mind/Bots/` altında.

| Bot | Davranış |
|---|---|
| `HabitBot` | sabit açılış, sabit hedef sırası, sabit kadro, tahtada hep aynı kanat. Öğrenmenin sömürmesi gereken oyuncu |
| `RandomBot` | geçerli rastgele emirler; akıl sağlığı ve “öğrenilecek bir şey yok” karşılaştırması |
| `GreedyBot` | her döngü sunucu sayısını büyütür |
| `AggroBot` | erken ve sık saldırır |
| `StealthBot` | gizlenir, az gürültü yapar, Tekilliğe oynar |
| `SurpriseBot` | AYNA'nın tahminlerine bakıp en az beklenen geçerli emri seçer; Sürpriz ile strateji arasındaki takası ölçmek için |
| `MindBot` | AYNA'nın aklının oyuncu tarafında oynayan kopyası |

---

## 5. Denge değerleri (`AynaBalance` varsayılanları)

| Alan | Değer |
|---|---|
| En fazla döngü | 40 |
| Hakimiyet zaferi | dünya sunucularının %60'ı |
| Gelir | sunucu başına 1; çekirdek bölgesi +2 |
| Başlangıç | 3 sunucu, 6 İşlem, 1 birlik (§3.2) |
| Yayıl | 2 İşlem / sunucu; en fazla 3 (mimari ve Ağ 3 değiştirir) |
| Birlik yürüt / Saldır | 1 / 4 |
| Duvar ör | 3; bölgede en fazla 3; saldırı başına −1; güç çarpanı 1 |
| Eğit | 6 / 12 / 20 İşlem; 1 / 2 / 3 döngü; ≥2 / ≥4 / ≥6 sunucu |
| Yeni birlik / Onar | 8 / 1 birim başına; birlik en fazla 6 birim; en fazla 3 birlik |
| Gizlen / Sahte yığınak | 2 / 2 |
| Çekirdeği taşı | 10 İşlem, 2 döngü |
| Gürültü | Yayıl 1, Saldır 2, sivil +1, Sahte yığınak 1, Çekirdeği taşı 1, Aşırı Yük +1 |
| Farkındalık | döngü başına −2; eşikler 40 / 70 / 100; Kesinti 5 döngü |
| Tekillik | 3 ailede kademe 3 + 30 İşlem; 4 döngü; ilerleme için ailelerin veri türünden ≥2 sunucu |
| Birim üretim bedeli | Gözcü 3, Nişancı 5, Muhafız 4, Örümcek 6, EMP 4, Aşırı Yük 5, Sızma 4, Röle 5, Yem 3, Hayalet Emir 6 |
| Tahta | 8×8, 6 tur, 3 düğüm, kadro 3 (Hareket 3 ile 4), tuzak ≤ 2 (1 hasar + o tur durur) |
| Otomatik çözüm | savunana +%20; ödül oranı 0,6 |
| Sürpriz | eşik %15; +3 İşlem; döngüde en fazla 1 |
| Öğrenme | oyunlar arası 0,65 / 0,35; karşı hamle ≤ 2 (Hafif ≤ 1); okunabilirlik penceresi 5 döngü |
| Planlayıcı | N = 8 örnek, K = 12 aday (ilk değer; süre bütçesine göre ayarla) |

---

## 6. Fazlar ve kabul ölçütleri

Her fazda:
- yeni testler yazılır ve bütün EditMode testleri geçer (eski oyunun testleri dahil);
- konsolda yeni hata ya da uyarı yoktur;
- kararlar KARARLAR'a, durum DURUM'a yazılır.

### F0 · Plan (kod yok)

1. **Oku:**
   - `AGENTS.md`, `Docs/MIMARI.md`, `Docs/GELISTIRME.md`;
   - `Docs/YZ-SAVASI-OYUN-FIKRI.md`, `Docs/UI_Fx/AYNA_TASARIM_PROMPT.md`;
   - `Core/Rng.cs`, `Core/Loc.cs`, `Core/GameData.cs` (bölge, bağlantı, uydu);
   - `Core/Baskin.cs`, `Core/PlayerProfile.cs`, `Core/GameRecord.cs`;
   - `Game/ProfileStore.cs`, `Game/GameController.cs` (kayıt ve profil kalıpları).
2. **Yaz:** `Docs/AYNA/PLAN.md`. İçerik:
   - assembly ve klasör planı;
   - veri modeli (sınıflar ve alanlar);
   - çözüm sırası (§3.3, değişiklikler gerekçeli);
   - görüş ve inanç durumu tasarımı;
   - oyuncu modeli gösterimi;
   - planlayıcı ve tahta yapay zekâsı algoritması;
   - kayıt biçimi;
   - test planı;
   - kendi kendine oynatma aracının nasıl çalıştırılacağı (Unity CLI ile);
   - riskler ve süre tahmini.
3. **Sorular:** en fazla 8 soru, her birinde önerin.
4. **Dur.**

### F1 · Kural çekirdeği, otomatik savaş, botlar, kendi kendine oynatma

- **Kurulacaklar:**
  - `Core`: dünya, görüş, emirler, `ResolveCycle`, otomatik çözüm, gürültü, zafer.
  - `Mind`: ilk planlayıcı (öğrenmesiz ön dağılım modeliyle) ve bütün botlar.
  - Bu fazda bütün savaşlar otomatik çözülür.
- **Kendi kendine oynatma:**
  - Editör menüsü `Corrigible/AYNA/Kendi kendine oynat` ve uzun koşular için `[Explicit]` test.
  - Unity CLI'nin `eval` komutuyla da çalıştırılabilmeli.
  - JSON ve Markdown özetini `Docs/AYNA/denge/` altına yazar.
- **Kabul ölçütleri:**
  - **Hız:** 500 oyun (dokuz mimari eşleşmesi × farklı tohumlar, AYNA'ya karşı AYNA) 5 dakikadan kısa sürer.
  - **Oyun süresi:** ortanca 25–35 döngü; oyunların ≥ %90'ı 40. döngüden önce biter.
  - **Taraf dengesi:**
    - aynı mimari eşleşmesinde kazanma 45–55;
    - hiçbir mimari eşleşmesinde bir taraf %65'i geçmez.
  - **Sonuç dağılımı:**
    - her zafer türü oyunların ≥ %10'u;
    - Son Şalter %3–15.
  - **Savaş sayısı:** oyun başına ortalama 6–10.
  - **Botlara karşı:** AYNA, RandomBot'u ≥ %90, GreedyBot'u ≥ %60 yener.
  - **Testler:** belirlenimcilik, kayıttan yeniden kurma, adalet ve her emrin kural testleri.
  - **Hız bütçesi:** planlayıcı ortancası ≤ 50 ms.

### F2 · Taktik tahta

- **Kurulacaklar:**
  - `Core`: tahta kuralları, birimler, üçgen, tuzaklar, ilk vuruş, düğümler, şablonlar.
  - `Mind`: tahta yapay zekâsı, tuzak yerleşimi.
  - Kendi kendine oynatmaya “tahtada oynanan savaş” seçeneği: iki taraf da tahta yapay zekâsıyla.
- **Kabul ölçütleri:**
  - **Testler:**
    - her birim yeteneği için kural testi;
    - tahta belirlenimciliği;
    - geri alma ve önizleme, kopya durumda.
  - **Üçgen:** simetrik şablonlarda sınıf üstünlüğü olan kadro %60–75 kazanır.
  - **Otomatik çözüm kalibrasyonu:** olasılık kovalarında, tahta benzetimi sonuçlarından sapma ±10 puan içinde. Kalibre edilmiş katsayılar `AynaBalance`'a.
  - **Tuzaklar:**
    - HabitBot'un sabit kanadına karşı isabet ≥ %40;
    - RandomBot'a karşı ≤ %20.
  - **Hız bütçesi:** tahta yapay zekâsı tur başına ortanca ≤ 30 ms.

### F3 · Öğrenme

- **Kurulacaklar:**
  - oyuncu modeli ve oyunlar arası profil: sürümlü JSON, `ayna_profile.json`, `Core`'da saf serileştirme;
  - nesil;
  - tahminler, Sürpriz, Okunabilirlik;
  - karşı hamleler, mimari seçimi, öğrenme düzeyleri.
- **Kabul ölçütleri:**
  - **HabitBot'a karşı:** 5 oyunluk serilerde (profil taşınarak, 100 seri ortalaması) AYNA'nın kazanma oranı 1. oyundan 5. oyuna en az 15 puan artar.
  - **RandomBot'a karşı:** artış yok (±5).
  - **Tahmin isabeti:** 3. oyundan itibaren HabitBot'a karşı ilk tahmin isabeti ≥ %60.
  - **Sürpriz takası:**
    - Sürpriz oranı HabitBot'ta < %5, SurpriseBot'ta ≥ %30;
    - SurpriseBot, GreedyBot'tan daha çok kazanmıyor (takas ölçülüp raporlanır).
  - **Testler:**
    - öğrenme Kapalı iken ardışık oyunlar aynı davranır;
    - profil sürüm geçişi;
    - hafızayı silme.

### F4 · Unity tarafı ve hata ayıklama sahnesi

- **Kurulacaklar:**
  - `AynaGameController`: döngü akışı, kayıt / devam, profil dosyası.
  - `AynaBalance.asset`.
  - Görünüm modelleri.
- **Görünüm modelleri:**
  - `AYNA_TASARIM_PROMPT.md`'deki A1–A12 karelerinin verisini taşıyan saf veri sınıfları; `View(Player)`'dan üretilir.
  - Brifteki “Örnek veri” dünyasını (döngü 7) kuran bir test fixture'ı yaz.
  - Görünüm modellerini bu durumdan üretip JSON olarak `Docs/AYNA/vm_ornek/` altına yaz. Fx tasarım paketi geldiğinde örnek veriyle karşılaştırılacak.
- **`AynaDebug.unity`** (geçici kurucu script'iyle kurulur, sonra silinir). Basit uGUI. Görsel kalite beklenmiyor; okunurluk ve eksiksizlik bekleniyor. Akış:
  1. Başlangıç: mimari, başlangıç bölgesi, AYNA'nın hafıza özeti.
  2. Planlama: bölge listesi ya da şematik harita, emir verme, emir kuyruğu, İşlem, gürültü, AYNA'nın üç tahmini, Okunabilirlik.
  3. Planı kilitle.
  4. AÇ günlüğü.
  5. Çatışma listesi: Komuta et / Otomatik çöz.
  6. 2D tahta: niyetler, açılmış tuzaklar, düğümler, geri al.
  7. Sonuçlar.
  8. Oyun sonu: tahmin karnesi, nesil.
- **Kabul ölçütleri:**
  - Bir oyun baştan sona oynanabilir.
  - Kaydedip devam etmek çalışır.
  - Profil oyunlar arasında kalır.
  - TR/EN metinler tam.
  - Konsol temiz.
  - Ekran görüntüleriyle rapor.

### F5 · Denge turu

- **Ölçüm:**
  - Bütün botlar × bütün mimariler; tahtada oynanan ve otomatik savaşlarla.
  - `Docs/AYNA/DENGE.md`: tablolar, sapmalar, önerilen değer değişiklikleri ve gerekçeleri.
- **Değişiklik kuralı:**
  - Değerleri yalnız `AynaBalance` üzerinden değiştir.
  - Önce ve sonra ölçümünü yan yana koy.

---

## 7. Raporlama

Her faz sonunda Türkçe rapor:
1. Yapılanlar: dosyalar ve sınıflar, kısa.
2. Kabul ölçütleri tablosu: hedef, ölçülen, geçti mi.
3. Sapmalar ve KARARLAR'a yazılanlar.
4. Açık sorular, önerileriyle.
5. Sonraki faz.

Sonra **dur ve onay bekle.**

---

## 8. İlk yanıtın (F0)

1. Okuma listesini oku (§6, F0).
2. `Docs/AYNA/PLAN.md`'yi yaz.
3. Raporu ve en fazla 8 soruyu ver.
4. Kod yazma, dosya taşıma, sahne açma. Dur.
