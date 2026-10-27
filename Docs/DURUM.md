# Durum, kararlar ve açık işler

_Son güncelleme: 2026-10-27_

## Nerede kaldık

Oyun tek bir harekât: **DeskGame**'deki Sığınak komuta odasından YZ işgalindeki 12 bölgeyi sektör sektör geri alma,
baskınlar **Baskin** sahnesinin 8×8 taktik tahtasında. Açılış ve emir belgesi → harita planlaması → baskın emri → CRT'ye
dalış → taktik savaş → operasyon raporu → vardiya şalteri ve vardiya raporu → … → son rapor ve yeniden başlama baştan
sona oynanabilir. Harekât sivil güven tükenirse kaybedilir (Dağılma); YZ alarmı eşikte karşı saldırır. Vardiyada bir
baskın. Sığınak baskının hazırlık yeri: uzmanların yarası kalır, revir sarar, atölye zırh plakası döker, yaşam alanı
yedek uzman tutar, telsiz alarmı yavaşlatır; oda seviyeleri özgür bölgeyle açılır. Zorluk özgür bölge sayısıyla dört
kademede artar; her bölgenin kendi tahta özelliği ve imza makinesi var (6 yeni makine). Türkçe ve İngilizce. Kayıt ve
devam (`liberation_savegame.json`, biçim 8) çalışır. Yeni harekâtta rehberli eğitim (analistin notları) ilk vardiyayı yönlendirir;
ilk baskın adım adım oynanan bir senaryodur. Son tam EditMode koşusu **206/206** (2026-10-27).

**Git:** Depo `git@github.com:kamyan/Corrigible.git`, varsayılan dal `main`. Sığınak işi `siginak-ui` dalında
(Aşama 1–10: `eed2824`, eski görünümün kaldırılması: `553517e`, sarı çerçeve: `b7b4d82`, sadeleştirme: `413fbf5`;
sivil güven, YZ alarmı, hazırlık sığınağı, bölge kimliği ve eğitim henüz commit edilmedi).

## Eğitim: senaryolu ilk baskın (2026-10-27)

Kullanıcı "ilk baskında tüm hareketleri adım adım oynatarak bitirelim" dedi.

- **Senaryo:** `TutorialRaid` (Bridge/Siginak). Harekâtın ilk baskını (weu_1, kademe 1, "guvenli", imza zdron, tam ve
  sağlam kadro) eğitimin taktik tahta adımında açılırsa tohum 129 ile kurulur. Tohum taranarak seçildi (bot araması):
  kanca bir robotu çeken "temiz" bir hat bulunamadı; seçilen hat EMP itişiyle zırh kırmayı, kancayla dost çekmeyi,
  makinelerin birbirini vurmasını, atış hattından kaçmayı ve kasayı almayı öğretir; üç turda hasarsız zafer.
- **Rehber:** `RaidGuide` yalnız sıradaki adıma izin verir (`RaidDataSource` Select/Move/Arm/Use/EndTurn'ü süzer; geri alma,
  geri çekilme, Esc ile Q'yu ya da seçimi bırakma kapalı; Geri al düğmesi sönük; tur bitirme onayı sorulmaz). Beklenen hamle
  yapılamazsa rehber kalkar (uyarı loglanır). Tahtada `RaidBoard3D` nabız gibi atan sarı çift çerçeve (`Marks.Guide`).
  Not "BASKIN · ADIM n/21": talimat + ders; makineler oynarken "Makineler sırayla oynuyor".
- **Eğitim akışı:** baskının beş adımı (seç, yürü, yetenek, turu bitir, süre) tek "raid" adımına indi (rehbersiz baskın için
  genel metin); 16 → 12 adım. Not baskın bitince de sağ üste çıkar (sonuç kartı sağ altta).
- **Kayıt:** simülasyon yalnız `RaidResult`'ı görür; tahtanın tohumu sunumda değişir, Core ve kayıt biçimi aynı.
- **Testler:** `TutorialRaidTests` (beş harekât tohumunda rehberin adımları yapılabilir ve hasarsız zafere varır; adım
  açılımı; yalnız taze ilk baskın senaryolu). `TutorialFlowTests` 12 adıma göre. EditMode 206/206.
- **Play'de denendi:** yeni harekât → fiş → emir → senaryolu baskın (tohum 129, rehber açık); yanlış uzman, yanlış kare,
  geri alma, geri çekilme ve erken tur bitirme engellendi; 21 adım sırayla, zafer, 3/3 · 3/3 · 2/2, sivil kaybı 0; rapor
  adımına geçiş. Konsolda hata yok.
- **Açık:** Esc rehberde Q'yu bırakmadığı için baskını duraklatır (duraklatma menüsü); yeniden Esc ile sürer.

## Eğitim: rehberli ilk vardiyalar (2026-10-27)

Kullanıcı "tutorial eklememiz gerekiyor" dedi; dört biçimden (rehberli ilk vardiyalar, ilk karşılaşma notları, ayrı eğitim
baskını, kılavuz el kitabı) rehberli ilk vardiyaları seçti.

- **Akış:** yeni harekâtta sağ altta analistin notu, 16 adım (masa → baskın → rapor → sığınak → şalter → vardiya raporu).
  Her adım oyuncunun eylemini bekler; zaten yapılmış adımlar geçilir; ilk ve son adım TAMAM ile. "Eğitimi atla" ya da
  kapanış bir daha göstermez (PlayerPrefs `Corrigible.TutorialDone`; editörde Corrigible › Eğitim › Eğitimi sıfırla).
  Kayıttan devamda eğitim yok. Simülasyona dokunmaz.
- **Kod:** `TutorialFlow` (saf C#: adımlar, koşullar, rapor açıldı mı mandalları), `SiginakTutorial` (bağlayıcı: masa,
  baskın tahtası, raporlar ve sığınaktan durumu okur), `TutorialNoteView` (kâğıt kart; görev fişinin malzemeleri ve
  yazıları). Sahnede kökte `SiginakTutorial` tuvali (sıralama 70); sahne farkı yalnız bu nesneler ve kök listesi.
- **Testler:** `TutorialFlowTests` (adım sırası, geçilen adımlar, raporun açılıp kapanması, atlama, sahne bağlantıları). EditMode 203/203.
- **Play'de denendi:** 16 adımın hepsi sırayla; not görev fişiyle ve baskındaki TURU BİTİR ile çakışmıyor, sığınak pozunda
  revir kartının köşesine biniyor. Konsolda hata yok; kayıtlar ve eğitim bayrağı geri yüklendi.
- **Açık:** ayarlardan eğitimi yeniden açma yok (yalnız editör menüsü); masadaki notlar hedefi işaretlemiyor (yazıyla
  yönlendirir; baskında sarı çerçeve var);
  "TAMAM"ın İngilizcesi tablodaki ortak çeviri (DONE).

## Bölge kimliği ve zorluk eğrisi (2026-10-27)

Oyun incelemesinin dördüncü önerisi. Kullanıcı: "zorluk ve gelişim zamanla artsın, her bölgenin farklı özelliği ve en az
bir farklı düşmanı olsun"; seçenekler arasından "6 yeni tür, ikişer bölge" ve "oda seviyesi bölgeyle açılır"ı seçti.

- **Zorluk kademesi** (özgür bölge 0–1 / 2–5 / 6–9 / 10–11): kademe başına başta bir makine fazla; 3. kademede kopyalama
  (turda iki uyanma) ve iki imza makinesi; 4. kademede makinelerin canı +1. Eski kural 4 bölgeden sonra artmıyordu.
- **Bölge kimliği:** 12 bölgenin her birine kendine özgü tahta özelliği (Güvenli giriş, Dar koridor, Kısa nöbet, Takviye,
  Açık arazi, Kum fırtınası, Soğutma ağı, Kalabalık şehir, Zırhlı dolap, Elektrik şebekesi, Kopya fabrikası, Hızlı
  devriye) ve imza makinesi. Altı yeni makine (her biri iki bölgede): zırhlı dron, havan robotu (engel üstünden 3 kare),
  patlayan robot (çevresine patlar), kilitleyici (uzmanı bir tur yürütmez), kalkan dronu (makineye zırh), çoğaltıcı (dron
  uyandırır). Duvar ve nesne ekleyen özellikler yolu kapatmaz.
- **Gelişim:** oda seviyesi 2 için 2, seviye 3 için 5 özgür bölge; başta yalnız onarım (seviye 1).
- **Ölçüm:** ilk ayar (kademe 3: 5, kademe 4: 9 bölge) tam sığınaklı dengeli botu 2/10'a düşürdü; kayıpların çoğu
  sivil güvenden. Eşikler 6 ve 10'a, bölge güveni +1 → +2'ye çekildi. Son hâl (10'ar harekât; saldırgan / dengeli /
  koruyucu): tam sığınak 1/10, 5/10, 8/10; yaralıyken dinlenen 1/10, 9/10, 8/10; sığınaksız 0/10, 1/10, 1/10. Kademe
  başına baskın başarısı ikmalde %98 → %59, sunucuda %95 → %50.
- **Sunum:** altı yeni makine prefab'ı (dron ve bakım robotundan türetildi, `Art/Raid/Enemies` malzemeleri;
  `RaidPieceLibrary`'de yeni yuvalar), tahtada ZIRHLANDI, KİLİTLENDİ, patlama efekti ve KİLİTLİ etiketi; patlama alanı
  tehlike karesi, kalkan onarım işareti, çoğaltma uyanma halkası; kum fırtınasında uyanma halkaları gizli. Görev çipinde
  bölge özelliği, açıklamasında imza makinesi ve kademe; düşman ipucu kartında makinenin açıklaması, zırhı, imza işareti.
  Görev fişinde "Bölge: … · İmza: … · Tehdit n/4", bilgi ve cephe onay fişinde açıklamalı kimlik. Sığınak panosunda kilitli
  seviye (KİLİTLİ, gerekçe). Ekip ve birim kartında zırh plakası için mavi nokta. Telsiz odasında jeneratör tankları yerine
  telsiz masası, cihaz, mikrofon ve anten.
- **Kayıt biçimi 8.** `RaidParams.Inf` ve `liberationRaidThreat*`, `liberationCopyFromRegion` kalktı.
- **Testler:** `RegionIdentityTests` (bölge benzersizliği, kademe eşikleri, özelliklerin tahtaya etkisi ve yolun açık
  kalması, altı makinenin niyetleri, oda kilidi); kopyalama testi kademeye göre; eski testler oda kilidine göre.
  EditMode 197/197.
- **Play'de denendi:** görev fişi kimliği, telsiz masası, zırh noktası, altı makinenin tahtada görünüşü ve niyet okları,
  Japonya-Kore'de 3. kademe baskın (fazladan kablolar, iki kilitleyici) ve bir düşman evresi, sığınak panosunda seviye
  kilidi. Konsolda hata yok; kayıt dosyaları geri yüklendi.

## Hazırlık sığınağı (2026-10-27)

Oyun incelemesinin bulgusu: sığınak harekâtın sonucunu değiştirmiyordu. Botla üç plan (hiç dokunma, yalnız revir, her şeyi
kur) aynı harekâtı oynadı; yaşam alanı, jeneratör ve atölye sonuca hiç dokunmuyor, revir yalnız bir vardiyalık zamanlama
kaydırıyordu. Malzeme yalnız oda geliştirmeye gidiyordu, fon 450–770 artıkla bitiyordu, ekip baskında 8 candan ortalama
1'ini kaybediyor ve neredeyse hiç düşmüyordu. Kullanıcı sığınağı baskının hazırlık yerine çevirmeyi seçti, önce botla ölçüm.

- **Kural:** uzmanlar ayrı izlenir. Baskında alınan hasar yara olarak kalır (sonraki baskına eksik canla); düşen uzman
  3 gün yatar, yerine yaşam alanının yedeği (bir eksik canla) çıkar; Teknisyen yatıp yedek de yoksa baskın yok. Baskına
  çıkmayan vardiya her uzman 1 yara toparlar. Revir görevli başına 0,34 yara sarar ve yatanları erken kaldırır; atölye
  malzemeden zırh plakası döker (0,25/görevli, 4 malzeme), plaka ilk hasarı karşılar; yaşam alanı seviye başına bir yedek
  tutar; jeneratörün yerine telsiz odası (görevli başına vardiya alarmı −0,05). Enerji kalktı, atölye malzeme üretmez.
- **Ölçüm** (deneme Core'u; dengeli ve koruyucu bot): ilk denemede tam sığınak 10/10, karşı saldırı 0 (telsiz alarmı
  siliyor, revir ve plakalar bütün yarayı karşılıyordu). Ürünler kısıldı; üç zorluk varyantından kullanıcı "Orta"yı seçti:
  revir 0,34, alarm eşiği 14 → 10, telsiz −0,05, fon ödülleri kısık (ikmal 22 → 12, sunucu 10 → 8, bölge 30 → 15).
  Gerçek Core ile son ölçüm: tam sığınak 8/10 ve 8/10, sığınaksız 1/10, revirsiz 2/10 (yaralıyken dinlenirse 9/10),
  atölyesiz 3–4/10, telsizsiz 6/10; saldırgan oyuncu hep 0/10 (güven). Fon 1–5 vardiya baskını bekletiyor.
- **Sunum:** pano özetinde UZMANLAR ve PLAKA, odalarda DURDU damgası (atölye deposu dolu ya da malzemesiz, revir
  açlıkta) ve yeni notlar; görev fişinde kadro ("T 3/3 · E 2/3 · A 2/2 · 1 plaka") ve TEKNİSYEN YARALI damgası; CRT ve harita
  figürü Teknisyen yatıp yedek yokken sığınakta, yatan uzmanın piyonu yatık; vardiya raporunda "Zırh plakası" üretim satırı
  ve ekip satırında yara (dinlenme, revir, yatanlar); operasyon raporunda "Düştü", "Yeni yara", "Zırh plakası kırıldı";
  baskın tahtasında "PLAKA KIRILDI", birim kartında Zırh plakası ve Yedek uzman satırları, kadroda olmayan uzman YARALI.
- **Kayıt biçimi 7:** baskın sonucu kadronun kalan canını ve plakasını taşır. Sahnede jeneratör odasının kimliği
  `radio` (tek satır), `BalanceConfig.asset`'te değişen ödüller ve yaralanma süresi.
- **Testler:** `ShelterTests` (plaka dökümü, revir, yedekler, telsiz), `LiberationCampaignTests` (yara taşınır ve dinlenmeyle
  iner, yedek ve Teknisyensiz baskın, plaka dağıtımı, sonuç kodlaması), `RaidEngineTests` (kadro, plaka), sunum testleri
  yeni satırlara göre. EditMode 184/184.

## YZ alarmı ve bir baskın bir vardiya (2026-10-27)

Oyun incelemesinin ikinci önerisi: zaman baskısı. Kullanıcı "önce botla ölç" dedi.

- **Bulgu 1:** ekip baskından döndüğü an hazırdı (`ResolveRaid` `Busy`'yi sıfırlıyordu). Zincirleyen bot bütün harekâtı
  1–3 vardiyada bitirdi; vardiya şalteri yalnız yaralıyken gerekiyordu. Vardiya başına alarm anlamsızdı.
- **Bulgu 2:** sessiz vardiyanın alarmı düşürdüğü kural (B) dikkatli botu hiç saldırı yemeden bedelsiz bekletti.
- **Seçilen (A6):** baskın vardiyayı doldurur; alarm vardiya +1, başarısız baskın +3, kırılan YZ sunucusu −4; 14'te alarm
  sıfırlanır, YZ en son geri alınan sektörü yeniden işgal eder, sivil güven −1. Bot (10'ar harekât, son kurallar):
  saldırgan 0/10, dengeli 6/10, koruyucu 9/10 zafer; harekât başına ~2 karşı saldırı. Denenen diğer eşik ve değerler
  (A1–A8) daha sert ya da etkisizdi.
- **Açık kalan:** vardiya şalteri hâlâ gerçek bir karar değil (vardiyayı harcayacak başka iş yok). "Sessiz vardiya +
  YZ zamanla güçlenir" ve "karşı saldırı sığınağa baskın" seçenekleri ölçülmedi.
- **Sunum:** CRT'de YZ ALARMI n/14 ve "ALFA · DÖNDÜ · SONRAKİ VARDİYA"; kenar satırında ALARM; vardiya raporu bölümü
  EKİP VE YZ ALARMI (alarm satırı, ekip simgesi gizli; karşı saldırıda KARŞI SALDIRI ve teleks); operasyon raporunda
  "YZ alarmı +3 · 3/14", eşikte "EŞİK AŞILDI" ve karşı saldırı satırı; görev fişinde "SONRAKİ VARDİYA"; son raporda
  kırmızı damgalı KARŞI SALDIRI kartı; karşı saldırıda alarm sesi. Aynı vardiyada ikinci baskın teleksle reddedilir.
- **Kayıt biçimi 6.** Testlerde art arda baskınlar arasında vardiya biter (`Ready`); "boşta 400 vardiya harekâtı
  bitirmez" testi kaldırıldı: artık bitirir (karşı saldırılar güveni tüketir).
- **Sahne:** vardiya raporunun iki ekip satırına simge başvurusu (`TeamSlot.icon`), sahne farkı 2 satır.
- **Play'de denendi:** başarısız baskın raporu, aynı vardiyada ikinci baskının reddi, CRT, vardiya raporu, eşikte karşı
  saldırı (Batı Avrupa · Tedarik deposu yeniden işgal, güven 9/10, teleks). Konsolda hata yok; kayıt dosyaları geri yüklendi.

## Sivil güven ve kaybetme koşulu (2026-10-27)

Oyun incelemesinin bulgusu: harekâtta kaybetmek mümkün değildi, fon hiç darlık yaratmıyordu, kırılan sivil sunucuların
sonucu yoktu. Kullanıcı ilk öneriyi seçti: sivil güven sayacı ve kaybetme koşulu, önce botla denge ölçümü.

- **Bot:** Core Unity'siz derlenip (`mono`) açgözlü bir taktik botla bütün harekâtlar oynatıldı (projede değil, oturumun
  geçici klasöründe). Bot bir düşman evresi ileriye bakar; sivil ağırlığı ayarlanarak "dikkatsiz" ve "dikkatli" oyuncu taklit
  edilir.
- **Bulgu:** eski kuralda makineler oyuncuya nişan alamayınca sivil sunucuya ateş ediyordu; dikkatli bot da baskın başına
  ~2,5 sunucu kaybediyordu (dikkatsiz 3,1). Hiçbir başlangıç değeri dikkatliyi hayatta tutup dikkatsizi kaybettirmiyordu.
  Beş makine davranışı denendi; kullanıcı "makineler sivillerden kaçınsın"ı seçti (dikkatli ~0,3, dikkatsiz ~0,6).
- **Kural:** güven 10 (tavan), kırılan sivil sunucu başına −1, özgürleşen bölge başına +1, sıfırda Dağılma sonu. Makineler
  sivil hattına bilerek ateş etmez; kaçılan atış, EMP, itme ve patlama sivili yine kırar. Ayrıntı: `OYUN-TASARIMI.md`.
- **Son ölçüm** (10'ar harekât, son kurallarla): dikkatsiz 10/10 dağıldı (16–41. vardiya), orta 9/10 ve dikkatli 10/10 kazandı.
- **Sunum:** sayaç rafına altıncı sayaç GÜVEN x/10 (modüller 0,26 m aralığa sıkıştırıldı); kenar satırında GÜVEN; baskın
  HUD'unda SİVİL yerine "GÜVEN n" (bu baskında kırılanlar düşülmüş); kurallar kartı ve emir belgesinde kural; operasyon
  raporunda "Sivil güven" satırı ve 3'ün altında uyarı; son raporda KAYBEDİLDİ damgası ve "YZ kazandı.".
- **Bu sırada düzeltilen:** HUD'da üç sivil yuvası var ama tahtada 4–5 sivil sunucu oluyor; kırık simge ancak üçüncü
  kırılandan sonra görünüyordu. Artık ilk kırılan işaretlenir.
- **Kayıt biçimi 5:** yeni son eski kayıtların sonucunu değiştirebileceği için biçim 4 kayıtları yüklenmez.
- **Sahne:** sayaç rafı tek seferlik editör kodu ile değişti. Unity'nin kaydı sahnedeki ~260 ilgisiz nesneye düzenleme
  anında hesaplanan değerler (TMP stil karması, renk, layout boyutları) yazdı; bunlar ayıklandı, sahne farkı yalnız sayaç
  rafıdır. RectTransform alt nesneleri sıradan Transform altındayken `localPosition` değil `anchoredPosition` kaydedilir.
- **Testler:** `RaidEngineTests` (kilitli ikmal savaşı yeni hamlelerle; makinelerin sivile nişan almaması, kaçılan atışın
  arkadaki sunucuyu vurması), `LiberationCampaignTests` (güven kaybı, Dağılma ve kayıttan devam, bölge iadesi ve tavan,
  çekirdek önce gelir), `DeskBridgeTests` (rapor satırı ve uyarı, yenilgi son raporu), `SiginakDeskTests` (altı sayaç sırası),
  `SadeRaidStateTests` (HUD güven etiketi ve ilk kırık simge). EditMode 167/167.
- **Play'de denendi:** emir belgesi (520 karakter sınırında), sayaç rafı GÜVEN 10/10, baskın HUD'u "GÜVEN 8", operasyon raporu
  "−3 · 7/10", güven 0'da son rapor. Konsolda hata yok; kayıt dosyaları geri yüklendi.

## Sadeleştirme: yalnız DeskGame ve Baskin (2026-10-27)

Kullanıcı kararı: "şu anki DeskGame ve Baskin sahnelerinde oynanan oyun kalsın, diğer denemeleri kaldır, bu oyunda
kullanılmayan kodları sil, illüzyon gibi bu oyunda olmayan mekanikleri kaldır." Soruların cevabı: eski kriz oyunu tamamen
(Main sahnesi ve Core kuralları dahil), tarayıcı prototipi tamamen, deneme belgeleri silinsin; dal `siginak-ui`.

- **Silinen sahneler:** `Scenes/Main.unity` (kriz masası), `Desk/Scenes/DeskUI_Gallery`, `DeskUI_Sandbox`,
  `RaidBoard_Sandbox`, `SadeDesk_Sandbox`, `SadeUI_Gallery`, `Siginak_FontTest`. Build listesinde DeskGame ve Baskin.
- **Silinen denemeler:** `Assets/Corrigible/Ayna` (AYNA savaş denemesi), `Assets/Corrigible/Fx` (fütüristik arayüz),
  `Prototype/` (tarayıcı prototipi, parite ve denge script'leri), `Docs/UI_Fx`, `Docs/AYNA`,
  `Docs/SonSalter_AYNA_Tasarim`, `Docs/SonSalter_Baskin_Paketi.zip` (açılmış paketi duruyor).
- **Core:** kriz kuralları çıktı: yayılma, Tekillik, tespit, çekirdek kaçışı, hediyeler, illüzyon ve Algı Katmanı
  (`Shown`/`Global`), araştırma ve operasyonlar, diplomasi, rakip tipleri ve yetenekleri, oyuncu profili ("Seni Tanıyan
  Rakip"), aşamalı öğrenme, olay kartları, kampanya, saha raporları, deneysel zaman ölçeği. Silinen dosyalar `Rules.cs`,
  `FieldReports.cs`, `PlayerProfile.cs`; `Simulation`, `WorldState`, `GameData`, `Balance`, `Economy`, `Teams`,
  `GameRecord` yeniden yazıldı. Taktik motordan aldatma (sahte niyet, tarama), yedek çekirdek ve hızlanma çıktı. Kurtuluş
  kuralları değişmedi; işgal oranı eski ortalama sızmaya eşit, baskınların "kopya" özelliği yine 2 bölgeden sonra.
- **Kayıt biçimi 4:** `RuleOptions`, profil ve kriz komutları kayıttan çıktı. Eski kayıtlar (biçim 3 ve öncesi)
  yüklenmez; oyun yeni harekâtla açılır.
- **Game:** eski uGUI kriz masası (`Game/UI`), eğitim, kampanya, profil deposu, `LocalizedText`, eski baskın kadrajı silindi.
  `GameController`, `AudioDirector`, `SoundBank` sadeleşti; başarım yalnız Silme.
- **Desk:** analist masasının harita, pencere, ipucu, illüzyon, hata ayıklama katmanları ve kriz ekranları (giriş,
  kampanya, sahte zafer, onay, eğitim, dosya, araştırma, telex, dock…) ve onların bileşenleri silindi. Kalanlar: oda
  (Siginak), kâğıt harita (`SadeMapView` ve bölge çokgenleri), baskın raporu (`EventModal`), son rapor (`GameOver`), ayarlar
  kartı, baskın HUD'u. Köprü yalnız baskın raporu ve son raporu kurar (`SimDeskBuilder`).
- **Sahne ve prefab'lar:** DeskGame'den kullanılmayan katmanlar (sızma haritasının işaret/düğüm/iplik katmanları, karantina
  çizgileri, eski masa lambası, kriz ekranları, ipucu/eğitim/hata ayıklama canvas'ları) ve eksik script'ler, Baskin'den
  `SadeDebug` silindi (DeskGame.unity ~885 000 → ~283 000 satır). Baskın kurallar kartının lejantı 10 → 8 satır (yalan ve
  tarama işaretleri çıktı). Çekirdek dolabının "Ç" yazısı `DeskLocText`'e geçti.
- **Varlıklar:** sahnelerin ve Resources'ın başvurmadığı ~175 prefab/veri/malzeme/doku, 64 kullanılmayan yer tutucu ses,
  `UiTheme`, `TutorialSequence`, `RaidUiTheme` asset'leri silindi; SoundBank'ta yalnız var olan 19 ses olayı kaldı.
- **Çeviri tablosu** 2 840 → 575 satır; yeni 16 metnin İngilizcesi yazıldı.
- **Kod:** C# 631 dosya / ~69 700 satır → 202 dosya / ~22 600 satır. Testler 713 → 127 (kriz kurallarının ve silinen
  arayüzlerin testleri gitti; taktik motor için `RaidEngineTests` eklendi, köprü testleri yeniden yazıldı).
- **Bu sırada bulunan hatalar:** son raporun grafik lejantında boş dizi öğesi `NullReferenceException` verecekti
  (dizi sıkıştırıldı); son rapor grafiği son sektör vardiya içinde düşünce %0 yerine son vardiyanın değerinde bitiyordu
  (güncel değer eklenir); İngilizce baskın günlüğünde "the aı node cabinet" (Türkçe küçük harf; kısaltmalar artık korunur).
- **Doğrulama:** derleme temiz, EditMode 127/127 (boş sahne açıkken), Play'de açılış → BAŞLA → vardiya ve raporu →
  baskın (ikmal) → kurallar kartı → geri çekilme → operasyon raporu → masaya dönüş; harekâtın sonu ve son rapor →
  Yeniden başla; Baskin tek başına. Konsolda hata yok. Kayıt dosyaları geri yüklendi.

Bu dönemden önceki kriz oyunu, analist masası (UI_C), Sade arayüz (F1–F7), Fx ve AYNA çalışmalarının ayrıntıları git
geçmişindedir (`b7b4d82` ve öncesi). `Docs/` altındaki şu belgeler o eski oyunu anlatır, güncel değildir:
`ARASTIRMA-INCELEMESI`, `BUTUNLESIK-EKONOMI-*`, `EKONOMI-INCELEMESI`, `FON-*`, `GELISIM-FIKIRLERI`,
`OPERASYON-VE-HIZALAMA-ORNEGI`, `ROSTER-6-GDD`, `SAHA-RAPORLARI*`, `SIFIR-PROJE-GDD-VE-YOL-HARITASI`,
`YZ-SAVASI-OYUN-FIKRI`, `UI-BILDIRIM-DUZELTMELERI-SONUCLARI.json`. (`UI_DeskGame/TASARIM_PROMPT.md` ise
bugünkü odanın tasarım brifidir.)

## DeskGame · Sığınak komuta odası (2026-10-26)

- `Docs/UI_Siginak/PROMPT.md`'deki on aşama uygulandı: oda ve ışık, kamera rig'i, masa (sayaçlar, anahtar rayı, şalter,
  teleks), kâğıt harita (görev fişi, cephe onayı, ekip figürü), sığınak maketi, CRT ve baskın dalışı, vardiya raporu,
  açılış ve emir belgesi, punto/TR-EN/performans geçişi. Ayrıntı ve kararlar `Docs/UI_Siginak/IMPLEMENTATION_MAP.md`.
- Eski AIBunker komuta masası ve odayı koddan kuran geçici kurucu kaldırıldı; sahne asıl kaynak (kural 5). Baskında odayı
  `SiginakRaidLink` gizler. Maketin büyük sarı çerçevesi kaldırıldı; seçili oda çerçevesi yalnız K4'te.

## DeskGame · kurtuluş harekâtı (2026-10-21)

- Tam YZ işgaliyle başlayan harekât, bölge başına iki ikmal ve bir sunucu sektörü, ortak sınırdan yeni cephe, 36 sektörde
  zafer, manuel vardiya, sığınak (yaşam alanı, jeneratör, atölye, revir), operasyon raporu. Bu kurallar kriz oyununun
  üstüne `RuleOptions.LiberationCampaign` ile yalıtılmış olarak eklenmişti; 2026-10-27'de oyunun tek kuralı oldu.
  Güncel anlatım: `OYUN-TASARIMI.md`.

## Önemli kararlar ve gerekçeleri

- **Lokalizasyon Unity Localization paketiyle değil, kendi `Loc` sınıfıyla.** Metinlerin bir kısmı motordan bağımsız
  Core'da üretiliyor; paket Core'dan kullanılamaz. Kaynak metin Türkçe kalır, çeviri tek bir sekmeli tabloda.
- **Komut kaydı ile kayıt** (durum dökümü yerine). Küçük dosya, determinizmi sürekli test eder. Kural değişirse eski
  kayıtlar farklı oynanır: `GameRecord.CurrentVersion` artırılır, eski sürüm yüklenmez.
- **Simülasyon/sunum ayrımı.** Core motordan bağımsız ve testli; oda yalnız okur, değişiklik komutla.
- **uGUI, UI Toolkit değil.** Kullanıcı UI'yı Scene görünümünde görmek ve Canvas ile çalışmak istedi.
- **Kalıcı sahne kurucusu yok.** Sahne ve prefab'lar elle düzenlenebilsin; koddan değişiklikler tek seferlik script'lerle.
- **Proje Linear renk uzayında** (kullanıcı kararı); ince yazı TMP malzeme ayarıyla, yarı saydam örtüler Linear telafili
  alfayla düzeltildi.
- **Zaman kendiliğinden akmaz:** vardiya oyuncunun şalteriyle biter; baskın ve hikâye açıkken bitirilemez.

## Açık işler

- **Karar bekleyen:** ampul ve lamba gölgelerini sert yapmak ~0,5 ms kazandırır, görünüşü değiştirir.
- **Denge:** harekâtın zorluğu ve uzunluğu gerçek oyunla ayarlanmadı; sivil güven, alarm ve hazırlık sığınağı botla
  ölçüldü, ama bot projede değil (oturumun geçici klasöründe); projeye denge testi olarak alınmadı. Botla görülen: zorluk
  5. bölgeden sonra artmıyor; baskın üç sabit yerleşimde ~45 kez tekrarlanıyor ve ikmal görevleri kolay. Karşı saldırıyla
  yeniden özgürleşen bölge bölge fonunu ve güveni yeniden veriyor (ölçüm de böyleydi).
  Son rapor grafiği en az 125 vardiyalık eksen kullanıyor; kısa harekâtta (Dağılma 16. vardiyada olabilir) çizgi sola sıkışır.
- **Ses:** Dağılma da Silme'nin bitiş sesini (`EndErase`) çalıyor; yenilgiye ayrı ses yok.
- **Yeni makineler** dron ve bakım robotunun renk ve parça varyantları; ayrı modeller ve ipucu kartı için ayrı küçük resimler
  (şimdi dron ve bakım robotunun resmi) ileride yapılabilir. Havan ve kilitleyicinin niyet oku düz/eğri ok; havanın
  engel üstünden attığı ayrıca çizilmiyor.
- **Operasyon raporu:** başarısız baskında aynı cümle hem başlık altında hem uyarı bandında görünüyor (eski davranış).
- **Ses:** yer tutucu sesler gerçekleriyle değiştirilmeli; AudioMixer yok.
- **İngilizce metinlerin ana dili İngilizce olan biri tarafından gözden geçirilmesi** (çeviriler ajan tarafından yapıldı).
- **Steam:** Steamworks.NET paketi kurulmadı, App ID ve `steam_appid.txt` yok. `Corrigible.Steam` paket kurulunca
  derlenir; denenmedi. Steamworks'te başarım ve Auto-Cloud (`persistentDataPath`) tanımlanmalı.
- **Baskın ekranı yalnız fare/klavye için;** gamepad akışı yok.
- **Test eksikleri:** PlayMode/UI testi yok; akış ajanlar tarafından Play'de elle doğrulanıyor.
- Eski oyunu anlatan belgeler (yukarıdaki liste) silinebilir ya da arşivlenebilir; kullanıcıya sorulmadı.
