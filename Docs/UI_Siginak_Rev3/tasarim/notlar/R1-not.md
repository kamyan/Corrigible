# R1 · Uygulama notu
*Pano: `R1-not.dc.html` · görüntü: `../panolar/R1-not.jpg`*

### R1 · MENÜ SAHANLIĞI (M0)
GIRIS · ALTI DURUM · 3 GÖLGELİ IŞIK
ESKİ → YENİ
- **sahanlık** — Beyaz fayans, açık beton, düz ışık (koridor gibi) → 1,45 m'ye kadar 15 cm krem-yeşil sırlı fayans (alt sıra kirli, çatlak, iki eksik karo), üstte kalıp izli koyu beton, ıslak zemin + birikinti + süzgeç, alçak tavan 2,6. Sahanlık 0,30 m sağa genişler (z 1,55 → 1,85): son şalter kapıdan ayrı nefes alır.
- **ışık** — Aydınlık ve düz → soğuk sahanlık lambaları, merdiven tepesinden mavi-gri soğuk ışık (yukarısı YZ'nin), kapı altından sıcak şerit (içerisi sığınak). Sıcak/soğuk karşıtlığı kapıyı hedef yapar.
- **kapı dış yüzü** — Çıplak → beton portal (0,16 m pervaz, altta 0,9 m tehlike şeridi), iki yüzlü aynı kanat: dökme demir çark Ø 0,36, pirinç gözetleme halkası, “SIĞINAK 07 · KYGA” şablonu.
- **başlık** — Betona yapışık tabela → doğrudan betona püskürtülmüş şablon (165 mm, iki ucunda tehlike bloğu), lambanın konisinde; tabela ve astar yok.
- **duvar düzeni** — Soldan: dolap · kart rafı + deste · puantaj saati · interkom + zil · kapı · son şalter. Saat ve raf kendi lambasının altında (L2).
- **interkom** — Kara kutu → dökme alüminyum gövde, yağmur siperliği, ızgara, pirinç düğme; üstünde pirinç çanlı zil.
- **son şalter** — Derin kutu + küçük plaka → arduvaz tabanlı iki kutuplu bıçaklı şalter, sac kapak (115°), ŞEBEKE lambası, ANA ŞALTER 22 mm + MAIN SWITCH 12 mm.
- **kamera M0** — (4,30 · 1,66 · 0,20) → (2,2 · 1,58 · 0,05), 52° → (4,50 · 1,52 · 0,30) → (2,2 · 1,34 · 0,30), 56°: kapı ortada, dolap ve son şalter tam, başlık üstte.

NESNELER
- **fayans** — tileGlazed #B7BBA3 r.32 (tx_tile150, derz #4F5148); alt sıra tileGlazedDirty #8E9282; kapak sırası 18 × 30 mm #5E6455.
- **beton · zemin** — concreteDark #5C625D r.95; concreteFloorWet #3B3F3B r.55; birikinti #1E2220 r.08.
- **LandingLamp ×2** — 0,20 m kol, başlık paintBlack, aşağı bakan cam kubbe lampGlassCold (emission #D8E6E2 2,2), 3 yassı kafes şeridi; gölge vermez · 712 üçgen.
- **PunchClock** — 0,30 × 0,46 × 0,165 paintGreyGreen; kadran Ø 0,196 dialCream + glassClear; 3 teker counterDrum; PUANTAJ brassWorn · 2476.
- **CardRack · deste** — 0,13 × 0,34 woodVarnish, çelik cep; deste 0,112 tepsi + 40 kart cardBlank + yaylı kıskaç steelBare · 436 / 348.
- **OuterIntercom · zil** — aluCast gövde, siperlik, düğme 4 mm kayar · 344; DoorBell pirinç çan Ø 0,08, tokmak ±6° · 288.
- **LastSwitch** — 0,30 × 0,50 × 0,13; arduvaz slate, bakır bıçak copper, seramik sigorta, sac kapak paintOliveDark · 2408.
- **Stairs · başlık** — 10 basamak 0,18/0,27, genişlik 0,9, tırabzan · 1372; TitleStencil 1,28 m, TMP yuvası 1,16 × 0,20.

DURUMLAR
- **kayıt yok** — Rafın cebi boş; yalnız deste. Tek tık yeni harekât.
- **kayıt var** — Komutanın kartı cepte: KOMUTAN · SIĞINAK 07 / V.014 / özgür 3/12, biten her vardiyaya bir delik.
- **yeni harekât onayı** — Kıskaç −70° kalkık; eski kart −14° öne devrik, ataşlı İPTAL EDİLİR · V.014 fişi (kırmızı çift çizgi). 4 s içinde 2. tık onaylar, yoksa kıskaç kapanır.
- **kart saatte** — Kart yarığa 70 mm girmiş, piston −12 mm, amber lamba yanık, yeni delik açık.
- **kapı açık** — Çark 1,25 tur döndü, kanat −95° sahanlığa açık; içeriden sıcak spot zemine şerit düşürür, saat lambasının gölgesi kapanır (gölgeli ışık 3'te kalır).
- **son şalter** — Kapak 115° kalkık, kol hâlâ yukarıda (55°), ŞEBEKE yanık; 4 s içinde 2. tık kolu indirir.

ETKİLEŞİM
- **tıklananlar** — Kart (ve saat), deste, dolap, son şalter. Kapı, çark, interkom ve zil menüde süs: üzerine gelince kalkmaz (tek giriş yolu kart).
- **üzerinde** — Nesne duvardan 4 mm öne gelir; 400 ms sonra kâğıt etiket: “KAYITTAN DEVAM · V.014”, “YENİ HAREKÂT · eski kayıt ilk komutta silinir”, “AYARLAR · ses ve dil”, “ANA ŞALTER · ışıkları kes, oyundan çık”.
- **basılı · dikkat** — Basılı: 2 mm geri, ses. Onay bekleyen kıskaç ve kapak dikkat durumudur: etiket “2. tık onaylar · 4 sn”, kısa tık sesi her saniye.
- **devre dışı** — Giriş dizisi ve kapanış sırasında hepsi; okunamayan kayıt (öneri): gri kart, çapraz OKUNAMADI damgası, kalkmaz.
- **Esc** — Dolabı kapatır ya da M0'a döner; oyundan çıkarmaz.

HAREKET VE SES
- **açılış** — Siyah → lambalar 0,6 s'de iki kırpışmayla yanar; uzak uğultu, damla, merdivenden soğuk rüzgâr.
- **onay** — Kıskaç 0,15 s kalkar (tık), eski kart 0,2 s devrilir, fiş 0,15 s düşer; 4 s sonra kıskaç kapanır.
- **giriş** — R4'te: kart 0,4 s saate, 0,14 s yarığa, “ka-çunk”, zil 0,35 s, çark 0,5 s, kanat 0,8 s, eşik 1,6 s, kararma 0,3 s.
- **son şalter** — Kapak 0,25 s, kol 0,3 s (ağır bıçak sesi); lambalar 0,6 s kırpışıp söner, jeneratör 1,2 s'de yavaşlar, siyah → çıkış.

İNGİLİZCE
CORRIGIBLE · SETTINGS · TIME CLOCK · KYGA · CIVIL DEFENCE · SHIFT · COMMANDER · SHELTER 07 · S.014 · free 3/12 · NEW CAMPAIGN · TO BE CANCELLED · S.014 · MAINS. ANA ŞALTER / MAIN SWITCH plakası iki dilde sabit.
