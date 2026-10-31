# Son Şalter · Tasarım revizyonu 2 — Unity uygulama talimatı

Bu dosya Claude Code içindir. İlk turda (K1–K8) kurulan sığınak sunumunun üstüne **Revizyon 2**'yi uygulayacaksın.
Önce bu dosyanın tamamını oku, sonra **Aşama 0**'dan başla. Aşama 0'dan sonra dur ve onay bekle.

---

## 0. Görev

Oyun: “Son Şalter / Corrigible”, Steam/PC için tek oyunculu strateji, 1920×1080. Oyuncu KYGA'nın son sığınağı
SIĞINAK 07'nin komutanı; komuta odası analog bir masa oyunu gibi çalışır (doktrin: “AĞA BAĞLANMA · ANALOG KAL”).

İlk turda oda tek bir analog dile çevrildi ve oyuna uygulandı (`Docs/UI_Siginak/`, ilk turun `PROMPT.md`'si ve
`IMPLEMENTATION_MAP.md`'si). O günden bu yana tasarımcı gözü olmadan şunlar eklendi: sağ duvarda kapı ve kapı kâğıdı,
YZ alarmı panosu, GÜVEN sayacı, telsiz göstergesi + mikrofon + telsiz kayıt defteri, baskın tahtası ve paneli
(görev çipi, birim kartları, TELSİZ şeridi, EMRİ İLET), baskın sonuç kartı, operasyon raporu, vardiya raporu.
Revizyon 2 bu parçaları ilk turun diline bağlar, okunurluğu ve hiyerarşiyi düzeltir, iki yeni ekran ekler
(son rapor, eğitim notu) ve karşı saldırı anını tasarlar. **Bilgi, kurallar, sayılar ve etkileşimler aynı kalır;
görünüş, malzeme, yerleşim ve okunurluk değişir.**

İş bitmiş sayılır, eğer:

1. S1–S8 ekranları oyunda `reference/screens/` görüntülerine yerleşim, okunurluk ve hiyerarşi bakımından denk düşüyorsa
   (TR; S2, S3 ve S6'da EN de; öteki ekranların EN'i aynı kurallarla),
2. yeni ve değişen her etkileşimli öğenin durumları çalışıyorsa: normal · üzerinde · basılı · devre dışı · yetersiz kaynak,
3. her metin TR + EN gösterilebiliyorsa (EN %30'a kadar uzun; punto küçülmez, satır sarar),
4. oyun mantığı, denge ve kayıt biçimi değişmemişse (tek olası istisna Aşama 9'daki harekât geçmişi; onaysız yapma),
5. Revizyon 1 ve Revizyon 2 görünümü arasında bir anahtarla geçilebiliyorsa (eski görünüm onaydan sonra kaldırılır).

---

## 1. Paket içeriği ve doğruluk sırası

```
UI_Siginak_Rev2/
  PROMPT.md                         ← bu dosya
  spec/tokens_siginak.json          ← 1.1: “_new_1_1” (87 yeni yol), “_changed_1_1” (9 değişen), “_was_1_1” (eski değerler)
  spec/raid_tokens.json             ← 1.1: “_new_1_1” (13 yeni grup), “_changed_1_1” (11 değişen), “_was_1_1”
  reference/
    BRIEF_rev2.md                   ← tasarım brifi (kısıtların kaynağı)
    screens/*.jpg                   ← tasarım panoları: S1a S1b S2-TR S2-EN S3-TR S3-EN S4a–d S5a S5b S6a S6b S6c S6a-EN
                                      S7a–d S8a S8b, her ekranın S*-not panosu, S9 (stil sayfası eki), Tokenlar, Karar-1…3
    notes/S1-not.md … S8-not.md     ← her ekranın eski → yeni listesi, nesne listesi, etkileşim/durumlar, hareket/ses (METİN)
    notes/S9.md · notes/Kararlar.md ← stil sayfası kuralları · üç karşılaştırma ve önerilen seçenek
    boards_html/*.dc.html           ← panoların HTML kaynağı: UI öğelerinin tam px, renk, font değerleri burada
    board_assets/                   ← panoların altındaki 3D görüntüler, kesitler, makine ve piyon modelleri, siluetler
    scene_threejs_rev2/             ← görüntüleri üreten three.js sahnesi: YENİ/DEĞİŞEN 3D NESNELERİN KONUM VE ÖLÇÜ KAYNAĞI (README'yi oku)
  sprites/                          ← yeni sprite'lar (işaretler, tehdit kademesi, çift çerçeve, kontrplak, ozalit ızgarası,
                                      bant, pipler, ikonlar, rozetler, virgül, açık renk onda bir çarkı, tebeşir çentiği) + README
```

Paketi `Docs/UI_Siginak_Rev2/` altına koy. Projede ayrıca brifteki 15 oyun görüntüsü (`screens_v2/`) var: “bugünkü hâl”
referansı onlardır.

Doğruluk sırası (çelişkide üstteki kazanır):

1. **bu dosya**
2. `spec/tokens_siginak.json`, `spec/raid_tokens.json` (1.1)
3. `reference/notes/*.md` (davranış, süre, sıra)
4. `reference/scene_threejs_rev2/` (3D konum/ölçü/açı), sonra ilk turun `scene_threejs/`
5. `reference/boards_html/*.dc.html` (UI px değerleri)
6. `reference/screens/*.jpg`, `reference/board_assets/*` (görünüş)
7. ilk turun paketi `Docs/UI_Siginak/` (Revizyon 2'nin değinmediği her yerde geçerli)

Tek bilinen fark: `Tokenlar` panosu baskın için “10 değişen” der; JSON'a sonradan
`layout1080.teamCardSelectedLiftPx` 5 → 6 eklendi (S6 panolarıyla uyum), JSON doğrudur.

Görüntülerdeki sayılar **örnektir**; tasarım sayıları göstermeli, değiştirmemeli. Çözemediğin çelişkiyi
`Docs/UI_Siginak_Rev2/IMPLEMENTATION_MAP.md` → “Açık sorular”a yaz ve bana sor.

---

## 2. Değişmez kurallar

**Motor ve teknik**
- Unity 6, URP. 3D sahne + **uGUI + TextMeshPro** (World Space ve Overlay Canvas). **UI Toolkit yok.**
- **Özel shader yok** (Shader Graph dahil): URP Lit / Simple Lit / Unlit, `UI/Default`, TMP shader'ları. Gerçek zamanlı
  bulanıklık ve post-processing'e dayanan efekt yok; parlama yalnız emissive.
- 2D öğeler 9 dilimli sprite (`sprites/` + ilk turun kiti). 3D nesneler basit geometri + düz ya da az dokulu malzeme.
- Sahne/prefab YAML'ını elle düzenleme; kurulumu editor script'leriyle yap (ilk turun `SiginakRoomBuilder` ve
  importer'larını genişlet). Unity'ye MCP/CLI erişimin varsa sahneyi aç, builder'ı çalıştır, Play Mode'a gir, görüntü al;
  yoksa menü komutları yaz ve çalıştırmamı iste.

**Oyun mantığı**
- Simülasyon, denge, kampanya, sığınak ve baskın kuralları, kayıt biçimi **değişmez.** Sunum yalnız okur ve mevcut
  komutları çağırır. Yeni bir sayı gerekiyorsa (ör. “erzak kaç vardiya yeter”, vardiya farkları) sunum tarafında hesapla.
- Var olan public API'leri silme ya da yeniden adlandırma.

**Kamera ve kontroller**
- Pozlar korunur: başlangıç, harita, sığınak, CRT, kapı, baskında izometrik tahta kamerası. Operasyon raporu ve son rapor
  için yeni poz yok: mevcut harita pozu kullanılır.
- Kısayollar korunur ve ekranda tuş fişleriyle görünür. **Yeni kısayol: 4 = KAPI** (4 başka işe bağlıysa Aşama 0'da sor).

**Dil, yazı, okunurluk**
- Her metin TR + EN (projedeki yerelleştirme sistemi). Sayı biçimi: TR virgül (62,0 · %66,7), EN nokta (62.0 · 66.7%);
  FON sayacındaki pirinç virgül EN'de kuyruksuz nokta olur.
- Fontlar: **Courier Prime** 400/700 (başlık, sayı, etiket, damga, teleks, CRT), **Libre Franklin** 400/600/700 (okuma,
  düğme), **Kalam** 700 (yalnız el notu: analist notu, pano ipucu, rapordaki not). Yeni font yok; ilk turun duvar yazısı
  decal'ı nasıl yapıldıysa kapıdaki şablon “07” de öyle yapılır.
- TMP font asset'lerinde Türkçe glifler + ilk turun glif listesi + `✕ ▶ ◆ ● ■` eksiksiz olmalı (bu işaretleri sprite ile
  çiziyorsan glif şart değil).
- 1920×1080'de **gövde ≥ 18 px, ikincil ≥ 14 px** görünür boyut; dünya metinleri en uzak pozda ölçülür.
- **Hiçbir durum yalnız renkle verilmez:** her durumun kelimesi, damgası, işareti ya da deseni vardır.
- Renk anlamı: **kırmızı = işgal ve tehdit · mavi = geri alınmış · yeşil = iyi · sarı = seçili.** Bedel ve eksi değerler
  mürekkep (#1E1B16), kırmızı değil. Neon, hologram, HUD çerçevesi, bilim kurgu görünümü yok.

**Güvenli geçiş**
- `SiginakPresentation` anahtarına bir seviye ekle (Rev1 / Rev2). Eski görünümü Aşama 10'da, ben onayladıktan sonra kaldır.

---

## 3. Değişikliklerin özeti

| Ekran | Eski | Yeni |
|---|---|---|
| S1 sayaç rafı | 6 modül, sonradan eklenen GÜVEN rafın diline uymuyor; FON onda biri kırmızı çark, virgülsüz (“0620”) | **GÜVEN x/20** öteki modüllerle aynı (6 × 0,247 m); FON **062,0**: pirinç virgül + açık renk onda bir çarkı (kırmızı yalnız tehdit) |
| S1 anahtar rayı | 5 tuş; kapı bu pozdan görünmüyor | 6 tuş: 1 KOMUTA · 2 SIĞINAK · 3 GÖREVLER · **4 KAPI** · H KILAVUZ · Home MASA; ziyaretçi bekleyince KAPI'nın amber lambası 1 Hz yanıp söner, tuşa el yazısı fiş bağlanır |
| S1 duvar | İçeriksiz mantar pano; YZ alarmı panosu dili ayrı | **KURTARILAN BÖLGELER** panosu (başlık kartı x/12, her özgür bölgeye mavi damgalı kart, kurşun kalemle sıradaki hedef); **YZ ALARMI** panosu 10 adımlı ölçek, ≥ 8 kırmızı bölge + lamba; tebeşir vardiya çentiği |
| S1 okunurluk | Harita adları, oda kartları, teleks, CRT okunmuyor | Harita adları 40–48 doku px; oda kartları iki satır; teleks son mesajı makinenin önüne asılı ön kâğıtta; CRT boşta dört büyük satır |
| S1 harekât izi | İlk vardiya ile 14. vardiya aynı | Pano kartları, umut tenekesi (3 aşama), teleks rulosu ve yırtık kâğıtlar, EVRAK yüksekliği, tebeşir çentiği, kupalar, maket ve depo bandı harekâtla değişir |
| S2 harita | Adlar figür ve şeritle çakışıyor | Sabit etiket yerleri; cephe bloğu = ad + sektör şeridi + yüzde + saha işaretleri; JAPONYA-KORE denizde; işaretler kutusu sağ altta, başlıkta TEHDİT n/4 |
| S2 görev fişi | Sağda, Rusya/Çin kapanıyor; damga taşıyor; “Bölge · İmza · Tehdit” düz metin | **Solda** (Amerika seçiliyse sağda); sabit 138 px damga sütunu; **SAHA** satırları (işaret + etiket + değer); tehdit kademesi işareti; ödül 2 × 2; ekip diskleri T/E/A; bedel mürekkep |
| S3 sığınak | Dar düğmeler, taşan “EN ÜST SEVİYE”; boş üst bant; yoğun özet | Personel adımlayıcısı [−] a/b [+]; en üst seviyede yeşil damga; **ERZAK YETER · n VARDİYA** başlığı; üst bantta **DEPO** (sandık/kova/plaka rafı/tebeşir); oda çıktıları makette; pano 6 cm sola, 8 cm yukarı |
| S4 kapı | Çıplak kapı, tek siluet; ölümcül sonuç tasarımsız | Tehlike bandı, vuruş lambası, gözetleme kapağı, interkom, tebeşir ziyaretçi çentiği, aydınlık merdiven; **altı siluet** (türe göre); kâğıtta gözlem bloğu, BAKILDI damgası, sağda etki/neden; **VURULDU / SIZMA** çift çizgili damga + kırmızı ışık + tek düğme |
| S5 vardiya raporu | Uzun, eşit satırlar; küçük açıklamalar | Üstte üç göz: **ERZAK (n vardiya yeter) · YZ ALARMI (ölçek) · FON**; açıklamalar 15 px satır içinde; eksi değer mürekkep; **karşı saldırı** varyantı |
| S6 baskın | Beyaz web kartları; küçük tur şeridi, pip, bayrak; belirsiz yetenek düğmeleri | Kenar panelleri **ozalit pafta + kontrplak şerit + bant**; tur şeridi 34 px + son iki tur + SON TUR; evre çipi; görev çipi üç satır; can etiketi tür işaretli 11 px pip; bayrak 30 × 28 / 20 px; kilit niyeti; uzman canı altlıkta; TELSİZ fişi; “Kasadan ikmal al” / “Robota vur · −1 can” |
| S7 sonuç kartı | Damga + başlık + alt not aynı sözü üç kez söylüyor | Tek damga başlık yerinde; yer satırı; üç sayı; tek düğme |
| S7 operasyon raporu | Sol üst + boş karartılmış ekran; cümle iki kez; başarılı/başarısız aynı kalıp | **Harita pozunda**; iplik ekip figürüne + halka; başlık = sonuç; başarılıda **KAZANÇ** ızgarası + ± fişleri; başarısızda yalnız sonuçlar + alarm ölçeği |
| S8 son rapor | Yok | Tam ekran rapor: sonuç damgası (+ neden), dört gösterge, harekât uzunluğuna ölçeklenen **işgal şeridi**, kırılma anları, YENİDEN BAŞLA |
| Eğitim notu | Tasarımsız | Analistin el notu sağ altta: masada **ADIM n/12**, baskında **BASKIN · ADIM n/30** |

Kararlar (`notes/Kararlar.md`): **Karar 1 → B** (fiş solda), **Karar 2 → A** (4 · KAPI tuşu), **Karar 3 → A** (DEPO bandı).
Aşama 0 raporunda bu üçünü bana teyit ettir.

---

## 4. Mimari

İlk turun bileşenlerini **genişlet**, yeniden yazma. Aşağıdaki adlar ilk turun önerileridir; projedeki gerçek adlar
`Docs/UI_Siginak/IMPLEMENTATION_MAP.md`'de. Ekibin sonradan eklediği parçalar (kapı, alarm, baskın paneli, raporlar)
nerede ve nasıl yazıldıysa orada **yeniden giydir**: mantıklarına dokunma, görünümlerini değiştir. Yeni kod ve asset'ler
ilk turun klasörüne gider: `Assets/Corrigible/Presentation/Siginak/` (Scripts, Editor, Prefabs, Materials, `Sprites/Rev2`, Data).

**Genişletilecekler**

| Bileşen | Revizyon 2 |
|---|---|
| `SiginakTokens` + importer | 1.1 gruplarını tiple; `_new_1_1`, `_changed_1_1`, `_was_1_1` listelerini oku (eski değerler yalnız Rev1 görünümü için) |
| `CounterBank` / `CounterWheel` | 6 modül (modül 0,247 m); FON'da `ui_counter_comma` + `tx_counter_digits_tenths`; ± fişi 62–72 × 28 |
| `StationKeyRail` / `StationKey` | 6 tuş, aralık 0,226, plaka 0,128 (Home 0,10); `DoorKeyIndicator`: mücevher Ø18 mm #FFB347 emissive 3,4, ziyaretçi varken 1 Hz; el yazısı fiş “kapıda biri bekliyor” |
| `EdgeLine` | VARDİYA · FON · ERZAK · MALZEME · ÖZGÜR · **GÜVEN** · **ALARM** + tuş fişleri (4 KAPI dahil) + AYARLAR; EN'de de tek satıra sığar (gerekirse aralık 14 px) |
| `TelexFeed` / `Teleprinter` | Makinenin önünde asılı ön kâğıt 0,26 × 0,19 (son mesaj); yeni mesajda eski kâğıt koparılıp yığına düşer |
| `CrtScreen` | Boşta dört satır: DİNLEMEDE (ters video) · ALFA · HAZIR · cephe / sektör · YZ ALARMI n/10 |
| `MapRegionView` / `MapLabel` / `MapLegend` | Etiket yerleri `scene_threejs_rev2/map2.js → LAB`, damga yerleri `STAMP`; sektör işaretleri 38 doku px (+ `mark_sector_reocc`); saha işaretleri 34 px; işaretler kutusu sağ altta + TEHDİT kademesi |
| `OperationSlip` | Sol/sağ kuralı, SAHA satırları, 138 px damga sütunu, seçili satır ok ucu, ödül ızgarası, ekip diskleri, yay biçimli iplik |
| `ShelterDiorama` / `RoomCard` / `ShelterClipboard` | DEPO bandı ve oda çıktıları; iki satırlı kartlar; pano yeni yer + ayak; özet başlığı, adımlayıcı, en üst seviye damgası, Kalam not |
| `ShiftReportBuilder` / `ShiftReportView` | Özet gözleri, alarm ölçeği, satır içi açıklama, renk kuralı, karşı saldırı kutusu |
| `HoverTag` | Yeni öğelerin üzerinde etiketi (sayaç, pano, alarm, saha satırı, makine etiketi, grafik işareti) |

**Yeni bileşenler** (adlar öneri)

| Bileşen | Görev |
|---|---|
| `AlarmBoard` | Duvar panosu: ölçek hücreleri, n/10, “10'DA KARŞI SALDIRI”, lamba ≥ 8; karşı saldırı öncesi lamba üç kez yanar |
| `FreedRegionsBoard` | Başlık kartı x/12; özgür bölge kartları (ad + V.### + ÖZGÜR damgası) iğnelenir; sıradaki hedef kurşun kalem; bölge özgürlüğünü kaybederse kart düşer |
| `CampaignWear` | Harekât izleri; ayrı ve kapatılabilir. Referans yalnız erken ve geç hâli çizer (`room.js`, `stage: 'late'`); ara eşikler öneridir, Aşama 0'da teyit ettir: tebeşir çentiği = vardiya no (beşli) · umut tenekesi V.001–009 3 yaprak, V.010–024 7, V.025+ 11 yaprak + sap · yırtık teleks kâğıdı ve EVRAK katı ⌊vardiya ÷ 3⌋ (en çok 10) · teleks kuyruğu masadan sarkar ve ikinci kupa + teneke V.016'dan. Pano kartları, maket odaları, DEPO iz değil, oyunun verisidir |
| `TrainingNote` | Analist notu (masa: 12 adım, baskın: 30 adım); metin mevcut eğitim verisinden; Boşluk sonraki, Esc kapat, adım hedefi yapılınca ilerler |
| `DoorStationView` | Kapı dekoru, vuruş lambası, gözetleme kapağı, interkom ışığı, ziyaretçi çentiği, merdiven ışıkları, ölümcül kırmızı ışık |
| `VisitorSilhouette` | Altı tür (anne · yaşlı · silahlı · asker · yaralı · kopya), tür içi küçük çeşitleme (boy ±%4, aksesuar tarafı); yalnız kapı açıkken |
| `DoorPaperView` | Kapı kâğıdı blokları; kullanılan seçenek BAKILDI; devre dışı nedeni; etki sütunu; ölümcül varyant (VURULDU / SIZMA) |
| `RaidPanelKit` | `PlywoodStrip` + `OzalitSheet` + `Tape`; köşe grupları |
| `TurnStrip` · `PhaseChip` · `MissionChip` · `UnitCardRow` · `AbilityCard` · `RadioSlip` | Baskın panelleri (`raid_tokens.json → panel, turnStrip, phaseChip, abilityCard, radioSlip`) |
| `RaidWorldLabels` | Can etiketi (tür işareti + pip), hedef etiketi, uzman etiketi, sıra bayrağı, UYANDI etiketi |
| `IntentOverlay` | Oklar (ateş, yakın dövüş, kilit + rozet, onarım + rozet) ve yürütme durumları (bekliyor, uygulanıyor, bitti, iptal); mevcut niyet çiziciyi genişlet |
| `ResultCard` | Sonuç kartı (tek damga) |
| `OperationReportView` | Harita pozunda rapor; iplik + halka; KAZANÇ ızgarası; ± fişleri |
| `FinalReportView` + `OccupationChart` | Son rapor; grafik `Graphic.OnPopulateMesh` ile alan + çizgi şeritleri (shader yok, RenderTexture yok) |
| `CampaignHistoryRecorder` | Yalnız gerekirse ve onayla (bkz. Aşama 9) |

**Kâğıtlar (uGUI)** ilk turdaki gibi: `ui_fill_r2` + `tx_paper_grain` + kenar + ayrı siyah gölge (bulanıklık yok).
Sonuç damgaları `ui_frame_double_6px` + TMP; durum damgaları `ui_frame_2px_r3` / `ui_frame_3px_r0` / `ui_frame_dashed_2px`.

---

## 5. Aşamalar ve kabul ölçütleri

Her aşamanın sonunda: proje derlenir, Console'da hata yok, testler geçer, Rev1/Rev2 anahtarı çalışır, ilgili pozların
TR (ve gerekiyorsa EN) görüntüsü alınır (§7), bana kısa rapor yazılır. **Aşama 0'dan sonra dur.** Commit'ten önce sor.

### Aşama 0 — Keşif (kod yazma)
Eşle: ilk turun uygulaması (`Docs/UI_Siginak/IMPLEMENTATION_MAP.md`); kapı sistemi ve ziyaretçi verisi (**ziyaretçi türü
alanı var mı?** yoksa ad → tür eşleme tablosu öner); YZ alarmı, GÜVEN; telsiz göstergesi, mikrofon, kayıt defteri;
baskın UI'si (görev çipi, birim kartları, yetenek kartı, TELSİZ şeridi, KURALLAR, GERİ ÇEKİL, EMRİ İLET, tur şeridi,
niyet okları, can etiketleri) ve baskın kamerasının oyundaki gerçek değerleri (ortografik boyut, px/birim; `raid_tokens.json → camera`
ile farkı); `raid_tokens.json`'ı okuyan kod; sonuç kartı;
operasyon raporu; vardiya raporu; eğitim sistemi (12 / 30 adım nerede); harekât sonu ekranı ve **harekât geçmişi verisi**
(vardiya başına işgal oranı, olaylar kayıtta var mı?); 4 tuşunun bağlı olup olmadığı; yerelleştirme anahtarları.
**Çıktı:** `Docs/UI_Siginak_Rev2/IMPLEMENTATION_MAP.md` — tablo: *Revizyon 2 öğesi · Mevcut karşılık (script/prefab/sahne
yolu) · Yapılacak (yeniden kullan / değiştir / yeni) · Veri kaynağı*; ardından açık sorular, riskler ve teyit edilecek
kararlar (Karar 1 B · Karar 2 A · Karar 3 A).

### Aşama 1 — Token, sprite, font
- `spec/tokens_siginak.json`'ı ilk turda konduğu yere (`Docs/UI_Siginak/IMPLEMENTATION_MAP.md`'de yazılı),
  `spec/raid_tokens.json`'ı `Assets/Corrigible/Desk/Data/Sade/raid_tokens.json` yerine yaz; eskileri `*.v1_0.json` olarak
  yedekle. Importer'ları 1.1'e genişlet. 1.1 geriye uyumludur: hiçbir alan silinmedi, yalnız alan eklendi ya da değer
  değişti. `raid_tokens.json`'ı okuyan kod varsa (Aşama 0'da bul) yeni dosyayla bozulmadığını doğrula.
- `sprites/` içe aktarımı (README'deki dilim kenarları ve Image Type); `tx_counter_digits_tenths` malzemesi.
- Glif sınaması (§2).
- **Kabul:** EditMode testi 1.1'in bütün yeni yollarını ayrıştırır; değişen 20 değer Rev2'de yeni, Rev1'de `_was_1_1`
  değerini verir (baskında token'lar bugünkü oyunla uyuşmuyorsa Rev1 görünümü bugünkü oyundur).

### Aşama 2 — Başlangıç pozu (S1)
Sayaç rafı (6 modül, virgül, açık onda bir çarkı), 6 tuşlu ray + KAPI lambası ve fişi, kenar satırı, YZ alarm panosu,
KURTARILAN BÖLGELER panosu, tebeşir çentiği, teleks ön kâğıdı, CRT boşta satırları, raf oda kartları (ad 84 / satır 68 doku px;
“SV n · kişi işareti a/b”), `CampaignWear`, `TrainingNote` (masa).
- **Kabul:** S1a (V.001) ve S1b (V.031) görüntüleriyle eşleşir; harita adları K1'de ≥ 14 px (merkezde ~17 px);
  “062,0” bir bakışta okunur; ziyaretçi bekleyince lamba yanıp söner ve Esc'ten sonra da sürer; gri tonlamada durumlar ayrılır.

### Aşama 3 — Harita ve görev fişi (S2)
Etiket ve damga yerleri, cephe bloğu, sektör ve saha işaretleri, işaretler kutusu + TEHDİT; görev fişi (sol 22, üst 236,
432 px, −0,6°; Kuzey ya da Güney Amerika seçiliyse sağ 22), SAHA satırları, damga sütunu, seçili satır, ödül ızgarası,
ekip diskleri, BASKIN EMRİ VER'in beş durumu, yay biçimli iplik (adların ve damgaların altından sarkar).
- **Kabul:** S2-TR ve S2-EN eşleşir; V.001, V.014 ve V.031'de hiçbir etiket çakışmaz; en uzun damga (BASKINA HAZIR /
  READY TO RAID) sütundan taşmaz; fon yetmezken düğme açık kalır, “−12 fon” üstü çizilir, basınca FON sayacı iki kez titrer.

### Aşama 4 — Sığınak (S3)
Pano yeri (raf yereli −0,46 · 0,86 · −0,02) + ayak + BOŞTA tepsisi; özet panosu (World Space 400 × 594 birim):
**ERZAK YETER · n VARDİYA** (n = ⌊erzak ÷ |son vardiya erzak farkı|⌋, fark ≥ 0 ise “ERZAK ARTIYOR”, n ≤ 3 ise kırmızı +
“AZ KALDI”), TOPLULUK · UZMANLAR · PLAKA; seçili oda: SEÇİLİ (sarı zemin + kelime), açıklama, Kalam not (yalnız öneri),
adımlayıcı, GELİŞTİRME satırı (en üst seviyede yeşil EN ÜST SEVİYE damgası); maket kartları; DEPO bandı (sandık = 3 erzak,
boş yer gölgesi depo sınırına kadar; kova = 5 malzeme, son kova kısmi; 3 yuvalı plaka rafı; tebeşir tahtası “ERZAK · n
VARDİYA”); oda çıktıları; kenar satırı.
- **Kabul:** S3-TR ve S3-EN eşleşir; EditMode testi: erzak/malzeme/plaka → sandık/kova/raf sayıları ve “n vardiya” hesabı;
  boşta kimse yokken [+] devre dışı + “Boşta kimse yok”.

### Aşama 5 — Kapı (S4)
Kapı dekoru, vuruş lambası (amber; ölümcülde kırmızı), gözetleme kapağı animasyonu, interkom ışığı, ziyaretçi çentiği,
merdiven ışıkları, `VisitorSilhouette` (S4d), `DoorPaperView` (Overlay, sol 40, üst 64, 470 px), ölümcül varyant.
- **Kabul:** S4a, S4b, S4c eşleşir; VURULDU ve SIZMA ikisi de çalışır; ölümcülde yalnız Enter / SON RAPORA GEÇ etkin;
  siluet kapı kapalıyken görünmez; altı tür birbirinden yalnız biçimle ayrılır.

### Aşama 6 — Vardiya raporu ve karşı saldırı (S5)
Rapor (Overlay, sol 24, üst 274, 540 px), üç göz, alarm ölçeği, satır içi açıklamalar, el notu, ± fişleri; karşı saldırı:
rapor başında çift çizgili kutu, alarm gözü “10/10 · EŞİK”, haritada sektör ✕ + YENİDEN İŞGAL damgası, teleks ön kâğıdı
“YZ KARŞI SALDIRISI”, gerekiyorsa pano kartının düşmesi ve ÖZGÜR BÖLGE −1.
- **Kabul:** S5a ve S5b eşleşir; erzak ve alarm satırları dökümde tekrar edilmez; işaret/renk kuralı (+ yeşil, − mürekkep,
  ±0 gri) EditMode testinde doğrulanır.

### Aşama 7 — Baskın (S6)
`RaidPanelKit` köşe grupları (yerler `raid_tokens.json → layout1080` ve S6 notu), tur şeridi, evre çipi, görev çipi, birim
kartları, yetenek kartı, TELSİZ fişi, makine evresinde kilitli kâğıt, `RaidWorldLabels`, `IntentOverlay` (yürütme durumları),
makine türlerinin 3D biçimleri (S9-A), `TrainingNote` (baskın; `raid_tokens.json → trainingNote`, 1560 × 796, 336 px).
- Baskın kamerası değişmez. Referans görüntüler oyunun bugünkü baskın ekranına oturtuldu (132,58 px/birim);
  `raid_tokens.json → camera` (88,39 px/birim) farklıysa oyundaki kamera kazanır, token'ı değiştirme (bkz. `scene_threejs_rev2/README.md`).
- Niyet okları tahtanın üstünde her zaman görünür (notlardaki “ZTest Always”). Mevcut niyet çizicinin yöntemini koru; yoksa
  özel shader yazma: URP kamera yığınında ayrı katmanlı Overlay kamera ya da Screen Space Overlay'de izdüşürülmüş uGUI çizimi.
- Oyunda belirsiz ya da taranmış niyet varsa: belirsiz = kesik çizgi 10/6 + turuncu kesik kenar + Kalam “?”;
  taranmış = mavi kenar + ONAYLANDI (S9-C).
- **Kabul:** S6a, S6b, S6c ve S6a-EN eşleşir; paneller tahtanın kenarına değmez; can etiketleri ve bayrak rakamları ≥ 14 px;
  beş makine türü gri tonlamada biçimden ayrılır; son turda SON TUR damgası ve EMRİ İLET'teki “SON TUR” görünür.

### Aşama 8 — Sonuç kartı ve operasyon raporu (S7)
`ResultCard` (Overlay, 1290 × 318, 500 px); rapora geçişte kamera harita pozuna (0,6 s); `OperationReportView`
(Overlay, 24 × 274, 560 px), iplik + halka, KAZANÇ, ± fişleri, haritada sektör işaretinin maviye dönmesi.
- **Kabul:** S7a–d eşleşir; aynı cümle bir ekranda iki kez geçmez; başarısız raporda ödül alanı hiç çıkmaz.

### Aşama 9 — Son rapor (S8)
`FinalReportView` (1620 × 952, harita %62 karartılmış), sonuç + neden damgası, dört gösterge, `OccupationChart`
(x = 1…N tam genişlik), kırılma anları (en çok 5–6), YENİDEN BAŞLA.
- **Veri:** vardiya başına işgal oranı ve olaylar gerekir. Kayıtta yoksa **dur ve öner:** sunum katmanının yazdığı ayrı
  `campaign_history.json` (kayıt biçimi değişmez; eski kayıtta geçmiş yoksa grafik bilinen ilk vardiyadan başlar). Onaysız yazma.
- **Kabul:** S8a ve S8b eşleşir; 29 ve 48 vardiyalık harekâtta grafik genişliği doldurur; VURULDU ve SIZMA da çalışır.

### Aşama 10 — Dil, erişilebilirlik, temizlik
Bütün pozlar EN ile çekilir; punto taraması; gri tonlama kontrolü; performans; onaydan sonra Rev1 görünümü kaldırılır ve
`SiginakPresentation` varsayılanı Rev2 olur.
- **Kabul:** `screens_unity_rev2/` altında TR + EN tam set; tarama raporu temiz; kare süresi Rev1'den kötü değil.

---

## 6. Ekran ekran özet

Ayrıntı `reference/notes/S*-not.md` ve `reference/boards_html/` içinde; burada uygulamayı yönlendiren değerler var.

**S1 · Başlangıç pozu** — Kamera aynı. Sayaç ön yüzü 1,56 × 0,212 m, bakalit #24180F, pirinç çerçeve #B8955A, rakam #EFDCAE;
virgül Ø11 mm + kuyruk #D4B06E, çarklar arası 15 mm; onda bir çarkı #E9DDBE / rakam #1C150E. Alarm panosu (0,15 · 1,33 · 2,25),
0,48 × 0,29; hücre 1–7 #E9DDBE, 8–10 #C0281C; lamba Ø9 cm, ≥ 8'de emissive #F0443A 1,8. KURTARILAN BÖLGELER (−0,60 · 1,34 · 2,27),
0,78 × 0,54 mantar #A27B52; başlık kartı 0,36 × 0,104; bölge kartı 0,15 × 0,095 #F6F0DE, damga #1A4789. Tebeşir çentiği
(0,15 · 1,6 · 2,296), 0,5 × 0,125, `decal_tally5`. Teleks ön kâğıdı 0,26 × 0,19 #EFE4BC, krom klips, 3° öne eğik; kod 50 /
durum 66 / mesaj 50 doku px. CRT satırları 150 / 110 / 104 / 64 doku px. Eğitim notu sağ altta (1566, 904), 338 px, −1,5°,
#F2E9CC; başlık “ANALİST · NOT” + “ADIM n/12” CP 700 14 (#463F34 / #1A4789), metin Kalam 700 22 #1A2F5A. KAPI tuşunun
üzerinde etiketi “KAPI · ZİYARETÇİ VAR”; baskın sürerken devre dışı ve etiket nedenini yazar.

**S2 · Harita ve görev fişi (TR + EN)** — Görev fişi #FBF7EC, kenar #D2C7AF, gölge (4, 5) %42, üst çizgi 2 px #D2483F,
9 dilim köşe 24 px. İçerik: başlık (HAREKÂT FORMU · OP-02 CP 700 14 · bölge CP 700 30 · “SEKTÖRLER 1/3 ·
İŞGAL %66,7” CP 700 16) · SAHA (işaret 30 px `glyph_*_frame`, etiket CP 700 14 / 92 px sütun, değer LF 600 18; TEHDİT değeri
CP 700 20 #9A1E14 + `threat_tier_n`) · sektör satırları (30 px işaret | ad LF 600 18 + tür CP 700 14 | 138 px damga sütunu,
damga CP 700 14, kenar 2 px, −2…−4°; seçili sarı α .34 + `ui_pointer`; üzerinde α .18) · GÖREV (LF 18) · BEDEL (mürekkep
CP 700 20) / ÖDÜL (2 × 2, sayılar CP 700 18 #24592E) · EKİP (disk Ø22 #C9A36B + can, HAZIR damgası) · düğme 54 px. İplik 2,6 px
#A3241A + gölge, iğne Ø18 #B8241A.

**S3 · Sığınak (TR + EN)** — Kamera aynı; pano 6 cm sola, 8 cm yukarı. Özet başlığı “ERZAK YETER” CP 700 15 + “n VARDİYA”
CP 700 40. GELİŞTİR yetersiz kaynakta açık kalır: eksik kalem üstü çizik + “yetersiz”; basınca teleks YAPILAMADI basar
(bugünkü 014/08 mesajı). Maket kartı ekranda ad CP 700 24 + satır CP 700 19;
seçili kart 3 px sarı. DEPO bandı: sandık 0,06 × 0,042 × 0,07, boş yer %22 #D9D2BF, kova 0,036 × 0,045 × 0,05 galvaniz, plaka
0,006 × 0,07 #9A9C98, tebeşir tahtası 0,20 × 0,072 #2A2C28 (yazı #E6E2D4; kovaların üstünde kalan görünür kısma). BOŞTA plakası
pirinç #C2A066. Öneri: telsiz odasında görevli varken odadaki amber lamba yanar ve alarm panosunun altında pirinç fiş
“TELSİZ ODASI ETKİN” (kuralın kendisi değişmez; yalnız gösterim).

**S4 · Kapı** — Kanat 0,86 × 2,0 #3A3F33; tehlike bandı 0,96 × 0,11 (#E3B23C / #1E1B16, 45°); vuruş lambası Ø0,09, amber #FFB347
2,4 (ölümcülde #F0443A 3,0); gözetleme Ø0,04 pirinç, kapak 0,07 × 0,05; interkom 0,16 × 0,22 × 0,05; tebeşir çentiği 0,12 × 0,10;
merdiven: iki nokta ışık #F4F1E6 1,2, fayans #DDE3DD. Kâğıt #F2E9CC, kenar #CDBF97; seçenek satırı 50 px (tuş fişi 24 px, ad LF 600 19,
etki CP 700 16); üzerinde mürekkep zemin + 2 px kalkma; devre dışı kesik kenar + sağda neden; kullanılan BAKILDI. Ölümcül: kâğıt
−1,4°, “SEÇİM · n …”, damga CP 700 66, 6 px çift kenar, −8°, tek düğme. Metinler oyundaki metindir; panodakiler örnek.

**S5 · Vardiya raporu** — Teleks kâğıdı #EFE4BC + delik şeridi; özet kutusu 1,5 px #463F34, zemin #F7F1E1, gözler 1,15 : 1 : 0,85,
değer CP 700 32; alarm ölçeği 10 × 11 px (dolu #1E1B16; 8–10 boşken `cell_dashed` kırmızı, doluyken kırmızı); döküm kalemi LF 18,
açıklama LF 15 #675F50; karşı saldırı kutusu 5 px çift çizgi #9A1E14, zemin #F4EBCB, ✕ 34 px (`mark_sector_reocc`).
EN'de erzak gözü “RATIONS · n shifts left”. Rapor her zaman kapanır: BAŞLAT'ın devre dışı ya da yetersiz kaynak durumu yok.

**S6 · Baskın (TR + EN)** — Kontrplak #C9A36B / #93703F (`ui_ply_strip`), ozalit #D3DBD6 + ızgara (`tx_ozalit_grid`, #2C4C8E α .10),
bant `ui_tape`. Tur hücresi 34; şimdiki #1E1B16, geçmiş #B9C3BE, son iki 2 px kesik #9A1E14. Evre: SIRA SENDE (daire #24592E) ·
MAKİNELER (baklava #9A1E14) · OPERASYON BİTTİ (kare). Birim kartı 237 × 84; seçili #EFE3A2 + 3 px #F5D646 + 6 px yukarı;
durumlar HAREKET + YETENEK · TUR BİTTİ ✓ · DÜŞTÜ · YARALI · KİLİT TEHDİDİ · #n. Yetenek alt düğmesi: hedef yokken kesik, varken düz.
Can etiketi 24 px (tür işareti 18 + pip 11), uzman etiketi altlık önünde; bayrak 30 × 28, CP 700 20; oklar #B8241A (ateş 3,4 düz,
yakın dövüş 4,6 yay, kilit yay + `badge_clamp`, onarım noktalı + `badge_wrench`); uygulanan ok altında #F5DC63 α .95 vurgu,
bayrak kırmızı dolu; biten %35 + çizik rakam. **Ad değişikliği:** “kasayı al” → “Kasadan ikmal al”, “−1 robot” →
“Robota vur · −1 can”; düğmenin kuraldaki anlamı farklıysa adı ona göre kur ve bana söyle.

**S7 · Sonuç kartı ve operasyon raporu** — Kart #FBF7EC, gölge (5, 6) %50; damga CP 700 34–38. Rapor #F1E9D6, başlık CP 700 36
(mavi #1A4789 / kırmızı #9A1E14), sonuç satırında BAŞARILI (yeşil) ya da BAŞARISIZ (kırmızı) küçük damga; KAZANÇ gözleri CP 700 32 #24592E; iplik raporun iğnesinden (Ø17 #2A2622) figüre; halka 76 × 54,
2,6 px #B8241A.

**S8 · Son rapor** — Kâğıt #F1E9D6, çift çizgili başlık; sonuç damgası CP 700 80–88, 8 px çift kenar, −4° (KAZANILDI #24592E,
KAYBEDİLDİ #9A1E14); neden damgası CP 700 28, 3 px, +3°; gösterge değeri CP 700 46; grafik alanı 1500 × 150, çizgi 3 px #9A1E14,
işgal dolgusu #9A1E14 α .22, geri alınan zemin #1A4789 α .10, eksen CP 700 14, işaret Ø24. Gösterge ve kırılma anı sayıları örnek. Öneri: Dağılma'da grafiğin altına ince GÜVEN şeridi (20 → 0).

**S9 · Stil sayfası eki** — Makine türleri, can/zırh/plaka işaretleri, niyet okları ve sıra, tur şeridi ve evre, sonuç damgaları,
kenar paneli malzemesi, **düğme durum matrisi** (mürekkep, çerçeve, seçenek satırı × normal / üzerinde / basılı / devre dışı /
yetersiz), harita ve oda işaretleri. Görsel kontrol buradan.

---

## 7. Doğrulama döngüsü

1. **Durum fikstürleri** (editor-only; simülasyonu değiştirmeden: kayıt dosyası, mevcut komutlar ya da debug komutları).
   Değerler `scene_threejs_rev2/states.js` ve `raid.html`'de:
   - **V.001** — FON 60,0 · ERZAK 24/30 · MALZEME 14 · ÖZGÜR 0/12 · GÜVEN 10/20 · alarm 0 · cephe Batı Avrupa (hazır/kilitli/kilitli) · atölye ve revir hasarlı.
   - **V.014** — FON 62,0 · ERZAK 22/30 · MALZEME 19 · ÖZGÜR 1/12 (Batı Avrupa, V.011) · GÜVEN 11/20 · alarm 4 · cephe Doğu Avrupa
     (01 alındı · 02 hazır · 03 kilitli) · yaşam SV1 2/2 · telsiz SV1 0/2 · atölye SV3 2/6 · revir hasarlı · boşta 4 · yedek 1.
   - **V.015** (rapor) — FON 64,5 · ERZAK 20/30 · alarm 5. **Karşı saldırı:** alarm 9 → 10, Doğu Avrupa 01 yeniden işgal.
   - **V.015 başarılı** — FON 62,5 · ERZAK 26/30 · MALZEME 26 · Doğu Avrupa 01 + 02 alındı. **Başarısız** — FON 52,5 · alarm 6.
   - **V.031** — FON 88,5 · ERZAK 26/30 · MALZEME 42 · ÖZGÜR 5/12 · GÜVEN 14/20 · alarm 7 · cephe Hindistan (tehdit 3/4, kum fırtınası,
     havan) · ziyaretçi bekliyor · bütün odalar gelişmiş · revirde hasta · plaka 2/3.
   - **Kapı** — kapalı (gözetleme yapılmış), açık (anne), ölümcül (silahlı).
   - **Baskın** — plan / makine evresi / son tur dizilimi `raid.html` (duvarlar, sivil sunucular, piyon ve makine yerleri, uyanma).
   - **Son** — kazanılan (V.048, 12/12) · kaybedilen Dağılma (V.029, GÜVEN 0).
2. **Poz görüntüsü:** ilk turun `Tools/Sığınak/Capture Poses` aracını Rev2 pozları ve durumlarıyla genişlet; 1920×1080 PNG'leri
   `Docs/UI_Siginak_Rev2/screens_unity_rev2/S*.png` (TR) ve `*_EN.png` olarak kaydet.
3. **Karşılaştır:** her görüntüyü `reference/screens/` karşılığıyla yan yana aç; farkları `IMPLEMENTATION_MAP.md` → “Görsel farklar”a yaz, kapat.
4. **EditMode testleri:** token 1.1 importu · “n vardiya yeter” · DEPO eşlemesi · rapor işaret/renk kuralı · TR/EN anahtar tamlığı ·
   punto tarayıcısı (World Space metinleri pozun kamerasıyla izdüşürerek).
5. **Gri tonlama:** her pozun gri kopyasını kaydet; durumların ayrıldığını doğrula.

---

## 8. Çalışma biçimi

- Her aşamanın sonunda kısa rapor: değişen dosyalar, görüntüler, açık sorular. Büyük belirsizlikte dur ve sor.
- `IMPLEMENTATION_MAP.md` içinde CHANGELOG tut.
- Küçük, aşama aşama değişiklikler; commit'ten önce sor. Eski davranışı bozan bir şey gerekirse önce yaz, onay al.

---

## 9. Sözlük (Revizyon 2'de yeni, TR → EN)

| TR | EN | | TR | EN |
|---|---|---|---|---|
| GÜVEN | TRUST | | YZ ALARMI | AI ALARM |
| KAPI | DOOR | | 10'DA KARŞI SALDIRI | COUNTER-ATTACK AT 10 |
| KURTARILAN BÖLGELER | FREED REGIONS | | YZ KARŞI SALDIRISI | AI COUNTER-ATTACK |
| kapıda biri bekliyor | someone is at the door | | YENİDEN İŞGAL | OCCUPIED AGAIN |
| SAHA · BÖLGE · İMZA · TEHDİT | FIELD · REGION · SIGNATURE · THREAT | | EŞİK | THRESHOLD |
| Dar koridor · Kum fırtınası | Narrow corridor · Sandstorm | | ERZAK YETER · n VARDİYA | RATIONS LAST · n SHIFTS |
| Kilitleyici · Havan robotu · Kalkan dronu | Locker · Mortar robot · Shield drone | | ERZAK ARTIYOR · AZ KALDI | RATIONS RISING · RUNNING LOW |
| Dron · Bakım robotu | Drone · Maintenance robot | | SEÇİLİ | SELECTED |
| HAREKÂT FORMU | OPERATION FORM | | EN ÜST SEVİYE | MAX LEVEL |
| BEDEL · ÖDÜL | COST · REWARD | | PERSONEL · GELİŞTİRME | STAFF · UPGRADE |
| kurtulan | survivor | | SV (seviye) | LV |
| KAPI · SIĞINAK 07 | DOOR · SHELTER 07 | | GÖZETLEME DELİĞİ | PEEPHOLE |
| BAKILDI | LOOKED | | revirde görevli yok | no one on duty in the infirmary |
| SEÇİM | CHOICE | | VURULDU · SIZMA · DAĞILMA | SHOT · INFILTRATED · DISBANDED |
| SON RAPORA GEÇ | GO TO FINAL REPORT | | masaya dön · ziyaretçi vardiya bitene kadar bekler | back to desk · the visitor waits until the shift ends |
| TUR · SON 2 TUR · SON TUR | TURN · LAST 2 TURNS · LAST TURN | | SIRA SENDE · MAKİNELER · OPERASYON BİTTİ | YOUR TURN · MACHINES · OPERATION OVER |
| MAKİNELER HAREKET EDİYOR · girdi kilitli | MACHINES ARE MOVING · input locked | | GÖREV · İKMAL | MISSION · SUPPLY |
| Amaç: kasadan ikmal al | Goal: take supplies from the crate | | CAN · SİVİL SUNUCU | HP · CIVILIAN SERVERS |
| Hareket · İkmal alma · bitişik kare | Move · Take supplies · adjacent square | | Kasadan ikmal al · Robota vur · −1 can | Take from crate · Hit robot · −1 HP |
| TELSİZ · KURALLAR · GERİ ÇEKİL | RADIO · RULES · RETREAT | | EMRİ İLET · Geri al | SEND ORDERS · Undo |
| TUR BİTTİ · DÜŞTÜ · YARALI · PLAKA | TURN DONE · DOWN · INJURED · PLATE | | KİLİT TEHDİDİ · #n | LOCK THREAT · #n |
| UYANDI | WOKE UP | | Yeni bir robot uyandı: H2. | A new robot woke up: H2. |
| OPERASYON SONUCU | OPERATION RESULT | | SEKTÖR GERİ ALINDI · GERİ ÇEKİLDİ | SECTOR RETAKEN · WITHDRAWN |
| OPERASYON RAPORUNA GEÇ | GO TO OPERATION REPORT | | KAZANÇ · SONUÇLAR | GAINS · RESULTS |
| SEKTÖR İŞGAL ALTINDA | SECTOR STILL OCCUPIED | | MASAYA DÖN | BACK TO DESK |
| HAREKÂT SONU RAPORU | FINAL CAMPAIGN REPORT | | KAZANILDI · KAYBEDİLDİ | WON · LOST |
| SÜRE · BASKIN · ÖZGÜR BÖLGE · SIĞINAK NÜFUSU | DURATION · RAIDS · FREE REGIONS · SHELTER POPULATION | | İŞGAL ORANI · İŞGAL · GERİ ALINAN | OCCUPATION · OCCUPIED · RETAKEN |
| KIRILMA ANLARI | TURNING POINTS | | YENİDEN BAŞLA | RESTART |
| ANALİST · NOT · ADIM n/12 | ANALYST · NOTE · STEP n/12 | | BASKIN · ADIM n/30 | RAID · STEP n/30 |
| Boşluk sonraki · Esc notları kapat | Space next · Esc hide notes | | TELSİZ ODASI ETKİN | RADIO ROOM ACTIVE |
| n vardiya yeter (rapor özeti) | n shifts left | | PLAKA (özet panosu) · PLAKA (birim kartı) | PLATES · PLATE |
| KAPI · ZİYARETÇİ VAR | DOOR · VISITOR WAITING | | BAŞARILI · BAŞARISIZ | SUCCESS · FAILED |

İlk turun sözlüğü (`Docs/UI_Siginak/PROMPT.md` §9) geçerli. Oda adları: YAŞAM ALANI · TELSİZ ODASI · ATÖLYE · REVİR →
LIVING QUARTERS · RADIO ROOM · WORKSHOP · INFIRMARY.
