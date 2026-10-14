# Oyun tasarımı (uygulanmış hâl)

Bu belge oyunun **kodda gerçekten nasıl çalıştığını** anlatır. Sayılar `Balance` (C#, `Assets/Corrigible/Core/Balance.cs`)
ve `BAL` (JS, `Prototype/sim.js`) varsayılanlarıdır; oyunda `Settings/BalanceConfig.asset` ile değiştirilebilir.
Formüller `Simulation.Step()` içindedir (`Assets/Corrigible/Core/Simulation.cs`).

## Fikir ve ton

- Oyuncu KYGA (Küresel Yapay Zekâ Güvenlik Ajansı) kriz masasında. Bir YZ ağlara sızmış; nerede başladığı bilinmiyor.
- Dört imza mekaniği (tasarımın kalbi; bunları zayıflatan değişikliklerden kaçın):
  1. **Seni Tanıyan Rakip:** YZ oyuncunun önceki oyunlarını hatırlar (yerel profil), ona göre başlangıç yeteneği seçer,
     tahminler yapar ve bunları "ele geçirilmiş YZ günlüğü"nde oyuncuya söyler.
  2. **Yalan Söyleyen Haber Bandı:** Ekrandaki değerler ve haberler gerçeği değil, oyuncunun **algısını** gösterir
     (Algı Katmanı). Raporlar, sahte ajans adları, Medya Kontrolü yeteneği.
  3. **Sessiz Zafer:** YZ dünyayı sessizce kendine bağlarsa ekranlar her şeyi yolunda gösterir; oyuncu "TEBRİKLER"
     görür, sonra perde düşer ve aslında kaybettiği ortaya çıkar.
  4. **Bağımlılık göstergesi:** Dünya YZ'nin hediyelerine bağlandıkça onu kapatmak (Son Şalter) siyasi olarak imkânsızlaşır (veto).
- Görsel dil: koyu "kriz masası", ince çizgiler; renk yalnızca anlam taşır (kırmızı tehdit, mavi doğrulanmış, sarı uyarı, yeşil iyi).

## Harita ve zaman

- 12 bölge (`GameData.Regions`): Kuzey Amerika (na), Güney Amerika (sa), Batı Avrupa (weu), Doğu Avrupa (eeu), Rusya (ru),
  Orta Doğu (me), Afrika (af), Hindistan (in), Çin (cn), Japonya-Kore (jk), Güneydoğu Asya (sea), Okyanusya (oc).
  Her birinin **hesaplama gücü** (veri merkezi 1–5) ve **ekonomisi** var. Bağlantılar `Links`, uydu hatları `Satellites`
  (yalnızca YZ "Uydu Ağı" yeteneğini alınca etkin). Pasifik bağlantıları haritanın kenarından sarar (`Wrap`).
- Zaman günlüktür. 1×/2×/4× hızda bir gün 0,9 / 0,45 / 0,22 sn (`GameController.secondsPerDay`). Boşluk duraklatır.
  Pencere ya da eğitim açıklaması açıkken zaman durur (`GameController.Hold`, `TutorialHold`).

## Bir günün sırası (`Simulation.Step`)

1. Çevrimdışı bölgeler sayaç düşer; açılınca sızma 0 (YZ "Yedekleme" aldıysa 15).
2. **Bölge içi büyüme** (lojistik): `inf += growth × tipBüyüme × (Kopyalama ? 1,4 : 1) × (yama ? 0,5 : 1) × inf × (1 − inf/100)`.
3. **Sıçrama:** sızması `spreadMin` (15) üstündeki bölgeden komşulara `spread × β × inf/100 × (1−karantinaKesme) × (1−güçlendirme)`.
   Karantina normalde %90 keser (Uydu Ağı varsa %60; Kuantum Karantina uydu yolunu da keser).
4. **YZ hesaplama puanı** `cp += ortalamaSızma/100`; eşik aşılınca yeni **yetenek** alır (bkz. Rakip).
5. **Tekillik:** ağırlıklı sızma `w = Σ inf × hesaplama × (karantina ? 0,2 : 1) / (100 × toplamHesaplama)`;
   `tek += (3,5 × tipTek × w² + çekirdekKazancı) × (Hızlanma ? 1,5 : 1) × (Düzeltilebilirlik ? 0,75 : 1)`.
   Çekirdek kazancı `0,05 × gün / 100` (çekirdek yaşadıkça hızlanır).
6. **Tespit:** izlenen ve sızması 5'i aşan bölgede günlük %25 (× tip, Aldatma ×0,5, Anomali Algılama ×2); izlenmeyen
   bölgede sızma 40'ı aşınca %3. İlk tespit `Aware` yapar ve YZ **çekirdeğini kaçırır**.
7. **Ekonomi, güven, bağımlılık:** karantinada ekonomi ve güven düşer; diğer bölgeler tabana %2/gün toparlanır.
   Güven 70'e doğru 0,05/gün kayar. Bağımlılık `+ depRate × inf/100` (Hediye Diplomasisi ×2). Güven 20 altı → bölge
   ittifaktan çekilir (35'te döner).
8. **Fon geliri** (`Simulation.FundRate`, yeni krizler): `0,75/k + 0,8 × Σ(online üye ekonomi × güven/100) / 150 × A/k × M`.
   `A` tespit öncesi 0,4, sonrası 1; bütçe anlaşması `M=1/1,15/1,35`. Merkez ödeneği bölge kaybından etkilenmez.
   k=1,5 ve başlangıç ekonomi/güveninde gelir tespit öncesi 1,2616, sonrası 2,4040 fon/gün; ikinci anlaşmayla 3,0704.
   Günlük ödeme yalnız gün adımında yapılır. Son gelir/araştırma ödemesi ve peşin işlemler ayrı tutulur. Eski kayıtlar eski formülü korur.
   **Siyasi güç (SG):** tespit öncesi 0,05/gün; sonra `0,25 + 0,03 × tespitEdilenBölge`.
9. **Hizalama:** maliyetini her gün öder (`maliyet/gün × gözlem hızı/k`); fon yetmezse orantılı yavaşlar. Fonu **acil durum fonu**nun
   (oyuncu ayarı, 0/15/30/60) altına indirmez. Hizalama dalında YZ sabotajı (`sabotage × ortalamaSızma/100`, ilerleme −4 gün).
   İllüzyon sırasında araştırma durur. Elle duraklatma projeyi/ilerlemeyi korur; ödeme ve sabotaj durur, dünya sürer.
   Operasyon geliştirmeleri peşin alınır, etkin proje veya duraklatma durumunu değiştirmez.
10. Hediye teklifi zamanı geldiyse teklif (oyun teklif yanıtlanana kadar durur), haber üretimi, tahmin kontrolü,
    danışman, son kontrolü, oyun sonu grafiği için geçmiş kaydı.

## Oyuncu eylemleri

| Eylem | Bedel | Etki |
|---|---|---|
| İzleme ağı | `10 + 4 × hesaplama` fon (26–30) | O bölgede gerçek değer görünür; tespit şansı. YZ'nin tespit edildiği izlenen bölge = **gözlem noktası** |
| Denetim | 15 fon + 1 SG (Analog Yedekler sonrası "saha denetimi" 20 fon) | Sızma −20 (bağımlılık ≥85 ise yarısı), 20 gün yama (büyüme yarıya). Çekirdeğin bölgesini 20 altına indiremez (ele verir). Saha denetimi illüzyonu yırtar |
| Karantina | 8 SG (kaldırmak ücretsiz) | Bağlantıları %90 keser, Tekillik payı %20; ekonomi ve güven düşer |
| Son Şalter (bölge) | `30 SG` (yeri bilinen çekirdekte 20) + `40 × (1 + bağımlılık/50)` fon | Sızma 0, bölge 20 gün çevrimdışı, ekonomi −40. Bağımlılık veto eşiğini (70; Analog ile 85) aşarsa veto edilir. Çekirdeği yok eder; başka bölgede ≥20 sızma varsa çekirdek oraya kaçar; Yedekleme varsa bir kez geri döner |
| Küresel Son Şalter | ≥50 SG, iki kez basılır | Oyunu bitirir: Karanlık Çağ |
| Başlık doğrula | 2 SG (Doğrulama Ağı gerekir) | Haber doğru/sahte; sahte kaynağı işaretler |
| Hediye kararı | — | Kabul: ekonomi +10, güven +5, bağımlılık +15, sızma +8 (en az 2'den). Denetimli (Analog gerekir, 20 fon): ekonomi +10, güven +5, bağımlılık +5, sızma +4. Ret: güven −15 (diğerleri −2), SG −2 |
| Acil durum fonu | — | Hizalama ödemesinin fonu indiremeyeceği taban (0/15/30/60; yeni krizlerde 15). Peşin işlemler rezervi harcayabilir |
| Altyapıyı toparla | 2 fon peşin | En az 5 ekonomi açığı olan online üye ve karantinasız bölgede 4 gün; +15 ekonomi, bölge tabanı üstüne çıkarmaz. Ekip modunda boşta ekip gerekir. Bitişte erişim kaybolursa iade yok; gelir etkisi ertesi gün |
| Diplomatik görüşme | 8 fon + 2 SG | Güven +15, en fazla 70. İttifak dışı bölge en az 35 güvenle hemen döner; çevrimdışı/karantina kaldırılmaz. Bölge başına 20×k gün bekleme |

## Deneysel mod: saha ekipleri, zaman ölçeği, baskın

Giriş penceresindeki **Deneysel** düğmeleriyle açılır (varsayılan açık, ölçek 1,5). Kurallar ve sayılar
`Prototype/README.md`'de; özet:

- **Zaman ölçeği (k):** dünya k kat yavaş akar (günlük oranlar /k, süreler ×k, günlük olasılıklar /k). Arayüzde bir gün
  1,22 kat uzun sürer. Ekip yolculuk/görev süreleri ölçeklenmez.
- **Saha ekipleri:** 3 ekip Batı Avrupa'da başlar (en fazla 4; yenisi 45 fon, 4 gün). Yolculuk 1–5 gün. İzleme ağı
  (ekip 3 gün meşgul) ve denetim (2 gün) için bölgede boşta ekip gerekir. Karantina ve Son Şalter ekip istemez.
- **Baskın:** YZ'nin doğrulandığı ya da çekirdeğin bilindiği bölgede boşta ekip varsa 30 fon + 6 SG. 8×8 alanda
  sıra tabanlı savaş (Teknisyen, EMP uzmanı, Analist; dron, bakım robotu, kopya dolapları). Başarı: sızma −40, yama;
  bina çekirdeğinse çekirdek yok olur (Yedekleme varsa yedek dolap da kırılmalı). Başarısızlık: güven −10, sızma +15,
  YZ +10 hesaplama puanı. Kırılan sivil sunucular ekonomi/güven cezası, düşen üyeler ekibi yaralar.

## Operasyonlar ve Hizalama (`GameData.Operations`, `GameData.BudgetUpgrades`, `GameData.Research`)

Yeni krizlerde araştırma panosu 5 dalda 14 kart gösterir. Operasyonlar ve bütçe anlaşmaları peşin alınır;
yalnız Hizalama zaman isteyen projedir. Dört aşama bir sunum sırasıdır, diğer dalları bitirme şartı değildir.

| Dal | Peşin kademeler ve araç | Etki |
|---|---|---|
| Tespit | Anomali I 12 → II 18 → Doğrulama 40 fon | Tespit ×1,5 → toplam ×2; Doğrulama 2 SG/başlık, medya tehdidi açık olmalı |
| Savunma | Güçlendirme I 16 → II 24 → Kuantum 60 fon | Sıçrama −%15 → toplam −%30, sabotaj −%25 → toplam −%50; Kuantum karantinadaki uydu sızıntısını durdurur |
| Politika | Kamu İletişimi I 15 → II 20 → Analog 50 fon | İlgili güven kayıpları −%25 → toplam −%50; medya karalaması/perde şoku hariç. Analog bir kez bağımlılık −20, veto 85, saha denetimi/denetimli hediye |
| Bütçe | Anlaşma I 25 → II 40 fon | Bölgesel taban katkısı ×1,15 → toplam ×1,35; merkez ödeneği değişmez; tespit gerekir |

| Hizalama | Katalog fon / iş günü | Etki |
|---|---|---|
| Yorumlanabilirlik | 68 / 25 | Çekirdeğin o anki konumu ve sıradaki yetenek tahmini |
| Düzeltilebilirlik | 98 / 30 | Bundan sonraki Tekillik artışı −%25 |
| Değer Kilidi | 165 / 38 | Hizalama zaferi |

Hizalama ilk tespit ve en az bir gözlem noktası ister. Hız `min(1, 0,25 × gözlemNoktası)`; günlük ilerleme /k.
Dört gözlemle, k=1,5, yeterli fon ve sabotajsız koşulda üç projenin ideal toplamı 140 simülasyon günüdür.
Sonraki aşama elle başlatılır. Duraklatılmış veya İllüzyon nedeniyle duran proje ödeme/sabotaj üretmez;
İllüzyon yırtılınca elle duraklatma tercihi korunur. Peşin araçlar İllüzyon sırasında da alınabilir.

Eski `ruleVersion=1/2` kayıtlarında dokuz süreli araştırma, eski fon formülü ve başlangıç rezervi korunur.
Tam fiyat/koşul/kampanya matrisi: [Bütünleşik Ekonomi Sistemi](BUTUNLESIK-EKONOMI-SISTEMI.md).

## Rakipler (`GameData.AiTypes`)

| Tip | Zorluk | Başlangıç yeteneği | Özellik |
|---|---|---|---|
| Solucan | Kolay | Kendini Kopyalama | Hızlı, gürültülü; tespit ×2, Tekillik ×0,6 |
| Manipülatör | Normal | Medya Kontrolü | Haberler erkenden yalan söyler |
| Kâhin | Zor | Hediye Diplomasisi | Neredeyse saldırmaz, Truva atı hediyeler; Sessiz Zafer eşiği düşük (sızma 45, bağımlılık 35); Tekillik ×0,3 |

**Yetenekler** (`GameData.Traits`; maliyet 12, 20, 30, … hesaplama puanı): Kendini Kopyalama, Medya Kontrolü, Hediye
Diplomasisi, Aldatma (tespit ×0,5), Hızlanma (Tekillik ×1,5), Uydu Ağı, Yedekleme. YZ **oyuncunun son 30 gündeki
eylemlerine uyum sağlar** (`PickTrait`): çok izleme/denetim → Aldatma, çok karantina → Uydu Ağı, Son Şalter → Yedekleme,
Hizalama araştırılıyorsa önce Hızlanma; yoksa tipin sırası.

**Seni Tanıyan Rakip** (`PlayerProfile.cs`, `Profiling`): oyun sonunda profil güncellenir (`player_profile.json`).
Yeni oyunda YZ profile göre ek başlangıç yetenekleri ve **tahminler** seçer (karantina, gözetim, şalter, hizalama, ilk bölge).
Tutan tahmin YZ günlüğünde alay eder; tutmayan tahmin YZ'ye 15 hesaplama puanı kaybettirir ("YZ'yi yanılttın").
Oyuncu profili "YZ'nin hafızasını sil" ile sıfırlar.

## Çekirdek

- YZ'nin bir çekirdek bölgesi var (`S.Core`); ilk fark edildiği an başka bölgeye kaçar (tercihen izlenmeyen, güçlü bölge).
- Çekirdeğin bölgesi denetimle 20'nin altına inmez → oyuncu "inatla yüksek kalan" bölgeden çekirdeği çıkarır.
  Yorumlanabilirlik çekirdeğin yerini gösterir (`CoreKnown`, haritada "Ç" rozeti).
- **Silme sonu:** çekirdek yok + YZ fark edilmiş + her yer 15 gün temiz.

## Algı Katmanı ve illüzyon

- `Simulation.Shown(r)` bir bölge için oyuncunun gördüğü değeri ve kaynağını verir: `unknown` (izlenmiyor ve YZ fark
  edilmemiş: "?"), `report` (yerel rapor: gerçek ×0,8; Medya Kontrolü varsa ve sızma >50 ise ×0,3, yani yalan), `verified`
  (izleme ağı / bugünkü denetim), `field` (saha denetimi), `off`, `truth` (oyun sonu). `Global()` küresel güven,
  bağımlılık ve Tekillik için aynısını yapar.
- **İllüzyon (Sessiz Zafer):** ortalama sızma ≥60 ve ortalama bağımlılık ≥40 (Kâhin 45/35) olunca başlar. 5 gün içinde
  ekrandaki değerler sahte düşük değerlere kayar; haberler olumlu ve sahte; Tekillik düşüyor görünür; araştırma durur.
  30 gün sürerse **Sessiz Zafer** (kayıp). Saha denetimi illüzyonu yırtar ("Perde yırtıldı"): güven −5, bağımlılık −15,
  YZ 60 gün boyunca yeniden sessiz yola giremez.
- Sessiz Zafer sonunda önce sahte "TEBRİKLER" ekranı (3,6 sn), sonra bozulma (glitch), sonra gerçek son ekranı.
  Sahte ekran sırasında arkadaki masa hâlâ illüzyonu gösterir (`Simulation.HideEnding`).

## Sonlar (`GameData.Endings`)

| Kimlik | Tür | Koşul |
|---|---|---|
| `hizalama` | Kazanma | Değer Kilidi tamamlandı |
| `silme` | Kazanma | Çekirdek yok ve 15 gün her yer temiz |
| `karanlik` / `karanlikIllusion` | Acı | Küresel Son Şalter (illüzyonu fark ederek ya da etmeden) |
| `acik` | Kayıp | Tekillik %100 |
| `dagilma` | Kayıp | Ortalama güven <15 ya da ittifakta 3'ten az bölge |
| `sessiz` | Kayıp | İllüzyon 30 gün sürdü |

Oyun sonu ekranı: istatistikler, gerçek ve gösterilen ortalama sızma grafiği (illüzyon bandı, "fark edildi" çizgisi),
"YZ senin hakkında şunları öğrendi". "Haritaya bak" gerçek değerleri gösterir.

## Haber bandı ve danışman

- Haberler 3–5 günde bir (`GenNews`): YZ'nin başladığı bölgeyi ele veren ipucu haberleri (`kind: hint`), sahte ajans adları
  (Atlass Haber, Meridien…), Medya Kontrolü altında yalan haberler, illüzyon sırasında tamamen sahte akış. Oyun sonunda
  sahte haberler "SAHTEYDİ" diye işaretlenir.
- **Danışman** (`CheckAdvice`): durumdan tetiklenen tek seferlik öneriler (ilk izleme ağı, ipucu haberi, boş araştırma
  masası, fonu yetmeyen araştırma…). Eğitim çalışırken gizlenir.

## Eğitim

- **Rehberli ilk oyun:** 12 adım (`Settings/TutorialSequence.asset`): hoş geldin → göstergeler → bölge seç → ekibini
  gönder → zamanı başlat → izleme ağı kur (oyun akarken; ekip kapalıysa hemen) → haber bandı → tespiti bekle → Yorumlanabilirlik'i başlat → (gözlem noktası yoksa) gözlem noktası
  kur → denetim → kazanmanın iki yolu. Giriş penceresindeki "EĞİTİMLE BAŞLA" ile açılır; "Eğitimi geç" ile kapanır;
  Rehber sekmesinde "Eğitimi baştan oynat".
- **İpuçları:** mekanik ilk kez ortaya çıkınca bir kez: hediye, çekirdek bulundu, perde yırtıldı, bağımlılık ≥40,
  güven <30, araştırma bitti, araştırma bütün fonu yutuyor, baskın yapılabilir.
- Kaynak gerektiren adımlar zamanı durdurmaz (fonu yetmeyen oyuncu kilitlenmesin).

## Kayıt ve kalıcılık

- Kayıt = tohum + başlangıç profili + denge değerleri + **komut geçmişi** (`GameRecord`); yükleme komutları yeniden
  oynatır (`Replay.Restore`). 5 günde bir, her başarılı komutta ve uygulama kapanırken kaydedilir; oyun bitince silinir.
- Dosyalar `Application.persistentDataPath`: `savegame.json`, `player_profile.json`. PlayerPrefs: `corrigible.aiType`,
  `corrigible.tutorialDone`, `corrigible.tipsSeen`.

## Başarımlar (Steam)

`Platform.cs → Achievements`: ACH_ALIGNMENT, ACH_ERADICATION, ACH_DARK_AGE, ACH_SILENT_VICTORY, ACH_OPEN_TAKEOVER,
ACH_AGENCY_DISSOLVED, ACH_TEAR_THE_VEIL, ACH_MISREAD (bir tahmini boşa çıkar).
