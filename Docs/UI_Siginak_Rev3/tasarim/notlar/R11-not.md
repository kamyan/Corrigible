# R11 · Uygulama notu
*Pano: `R11-not.dc.html` · görüntü: `../panolar/R11-not.jpg`*

### R11 · BASKIN TAHTASI · ÜÇ KİT
WEU PLAN · AF MAKİNE · CN OLAY · EK 11-05: İSİMLİ EKİP
ESKİ → YENİ
- **kitler** — 12 kit yalnız tonla ayrılıyordu → her kitin kabartması, köprüsü, ayrık kolonu ve molozu var; gri tonda biçimle ayrılır (R12).
- **sivil** — Hep aynı krem kutu → bölgenin cihazı bandın altında; krem gövde, mavi bant, üstte Ø 0,32 halka ve çarpı aynı.
- **kapı** — Yoktu → giriş satırında, kontrplak payında binanın kapısı; yükseklik açık karar (R12-not).
- **makine** — Ortak gövde, tür etikette → 8 siluet, her yönde ayrı; kırmızı yalnız göz ve taban halkası. Her tahtada bölgenin imza makinesi: zırhlı dron, havan, çoğaltıcı.
- **piyon** — Kart piyon → Ø 0,54 siyah taban üstünde kalın ahşap harf (T, E, A); tur bitti 180° kraft yüz + tik, düştü −70°.
- **ekip · ek** — Beş anonim uzman (Madenci, Sis dahil) → üç isimli kişi: DENİZ · T, CEM · E, ZEYNEP · A; kartlarda gerçek CAN, HAR, GÜÇ ve eşya. Ad, altlığın önündeki kâğıt şeritte (TMP 14 px); 3B altlıkta yazı yok.
- **olay** — Tüp ve kaide soğutma ünitesinden türetilmişti → kendi modelleri; kaidenin turuncu burnu kolun yönünü gösterir.
- **panel** — Kontrplak şeritte kutu → paftadan kesilmiş kâğıt: pafta tonu #D7D5C9, ±2 px kesik kenar, bant, sağa 5 px sert gölge.
- **antet** — Yer adı yalnız çipteydi → sağ üstte künye: OZALİT KOPYA · PAFTA 2/3, bina, oda, bölge · ölçek · vardiya, ASLI GİBİDİR, anahtar plan.
- **ikmal · ek** — Tek MALZEME sayısı → çipte KASADA: 3 metal · 2 devre · 2 kumaş · 1 kimyasal · 6 erzak.

NESNELER
- **duvar kiti** — Relief 0,94 × 0,555 · 210–379 üçgen; ReliefIso 120–365; Bridge 16–140; Rubble 0,11–0,18 yüksek · 160–229 (kit malzemeleri).
- **sivil · kapı** — Civil 0,60–0,66 × 0,64–0,70 · 212–390 (varsayılan 162); EntryDoor 0,90 × ≤ 0,60 × ≤ 0,14 · 166–380.
- **kanal** — Hub 0,62 · 52, Arm 0,50 × 0,745 · 74, Edge 12, Single 350; kenar #A9520A, bakır; ≤ 0,048 yüksek, gölgesiz.
- **makineler** — Dron 1108 · Zırhlı 1162 · Kalkan 1372 · Patlayan 1100 · Bakım 1210 · Kilitleyici 1116 · Havan 1246 · Çoğaltıcı 1302.
- **piyonlar** — Pawn_T 848 · E 832 · A 1164, Ø 0,54 × 0,566; Letter, Tilt, Tape, Tick, Plate. Pawn_M ve Pawn_S yeni oyunda yok.
- **olay · sandık** — Tube Ø 0,2 × 0,75 · 386; WeldArmBase 0,70 × 0,30 · 294 + WeldArm 348; CeilingDebris 320; LootCrate 0,78 × 0,48 · 376.
- **kâğıt** — uGUI 9 dilim #D7D5C9, ızgara 24 px %10, iç çizgi 4 px içeride %45, bant 46 × 16 %82, gölge 5/1 px %50, 3 kenar maskesi.
- **antet · etiket** — Antet 364 × 166, anahtar plan sprite 112 × 84, damga #5B3A8E. Ad şeridi 24 px #FBF7EC (bitmiş #E9DDC6): ad CP 700 14 + pip + plaka.

ETKİLEŞİM VE DURUMLAR
- **piyon** — Normal: harf ön yüz. Üzerinde: kart 2 px kalkar, karede siyah köşe. Basılı: karede mavi çift çerçeve, kart sarı, yetenek kartı. Devre dışı: TUR BİTTİ (harf 180°, kart gri, ✓). Dikkat: vurulacak karede kırmızı tarama, kartta ⚠ PLAKA KIRILACAK ya da KİLİT TEHDİDİ · #n.
- **makine** — Normal: tür işareti + pip. Üzerinde: kâğıt etiket (tür, can, niyet). Basılı: inceleme kartı. Devre dışı: hamlesi bitti, bayrak %55 çizik. Dikkat: UYANDI (1 tur), sıradaki uyanma iğnesi.
- **kare** — Üzerinde: siyah köşe. Basılı: hedefte yeşil köşe. Devre dışı: ulaşılamaz, tarama yok. Dikkat: kırmızı tarama kesin, turuncu kesik kenar belki; olay bir tur önce işaretlenir.
- **panel** — EMRİ İLET: üzerinde 2 px kalkar, basılı 2 px iner, makine evresinde kilitli kâğıt, son turda SON TUR. GERİ ÇEKİL iki adımlı. Yetersiz kaynak: sarf edilen EMP hücresi kartta çizili; rol kiti eksik kişiyle emir verilmez.
- **antet** — Tıklanmaz; üzerinde BİNA · {BİNA} + bina ipucu (BuildingNote). Damga açılışta bir kez basılır.
- **kaçış · ek** — Çıkışa yetişemeyecek kişinin kartında GERİDE KALIRSA ÖLÜR. Son turda EMRİ İLET iki adımlı onay fişi açar: “Zeynep geride kalırsa kalıcı olarak ölür · Analiz kiti ve Bakım kiti kaybolur” (R11c). Geri çekilmede kimse ölmez; son canlının kaybı SON KİŞİ.

HAREKET VE SES
- **yürüyüş** — Kare başına 0,16 s (ease-in-out), atılma 0,22; itme 0,14 + sekme 0,06; kanala düşme 0,18.
- **saldırı** — Ateş çizgisi 0,12 s, isabet sarsıntısı 0,08; yakın vuruş 0,10 / 0,14; onarım 0,25; niyetler arası 0,25; bayrak 0,15, çizik 0,12.
- **piyon** — Tur bitti: harf 0,2 s'de 180° + tik; düşme 0,3 s; hasar yazısı 0,08 gelir, 0,6 kalır, 0,25 söner.
- **nesneler** — Dolap çöküşü 0,35 (ease-in), enkaz saçılması 0,25, soğutma patlaması 0,3, uyanma 0,12 / 0,25.
- **kâğıtlar** — Panel ve fiş 0,28 s'de 40 px yukarıdan ±1,5° ile düşer; antet damgası 0,12 s ağır “tak”; telsiz fişi 3 s kalır.
- **ses** — Yatak telsiz bandından: weu BedServer, af BedHangar, cn BedFactory. Olay: tüpte vana ıslığı ve çınlama, kaynakta ark cızırtısı, tavanda çakıl; ≤ 1,15 s.

İNGİLİZCE
TURN · LAST 2 TURNS · YOUR TURN · MACHINES ARE MOVING · input locked · UPS ROOM / CRUSHER BAY / WELDING LINE · SUPPLY · SAFE ENTRY · OPEN GROUND · ARMORED CABINETS · Supply crate · IN CRATE · metal · circuit · cloth · chemical · rations · HP · MOV · STR · TRUST · RADIO · RULES · RETREAT · BLUEPRINT COPY · SHEET 2/3 · BANK DATA CENTER / ORE HANGAR / ASSEMBLY PLANT · Scale 1:50 · S.031 · TRUE COPY · Technician kit · EMP kit · Analysis kit · Armour plate · Care kit · EMP cell · TURN DONE · PLATE WILL BREAK · SEND ORDERS · WOKE UP · “Zeynep dies for good if left behind · Analysis kit and Care kit are lost”. Adlar çevrilmez; bina, oda, MOV, STR öneri.
