# R2 · Uygulama notu
*Pano: `R2-not.dc.html` · görüntü: `../panolar/R2-not.jpg`*

### R2 · SAAT POZU (M2) VE KART
1 PX ≈ 0,65 MM · 2 GÖLGELİ IŞIK
ESKİ → YENİ
- **saat** — Düz kutu ve kadran → sivil savunma puantaj saati: döküm gövde, bombeli camlı kadran, VARDİYA sayaç penceresi (üç teker, masanın sayaçlarıyla aynı rakam dokusu), PUANTAJ pirinç plakası, KYGA · SİVİL SAVUNMA yazısı, üstte kart boğazı, piston kolu ve amber lamba.
- **kart** — Tasarımsız → 90 × 184 mm krem karton. Üstte KOMUTAN / SIĞINAK 07, büyük V.014, altında özgür 3/12; kalın çizgi; 10 × 8 zımba ızgarası (1–80 dokuda); altta mavi mürekkep paraf. Bir delik = biten bir vardiya; satır satır soldan sağa. 80'den sonra delik eklenmez, doğruyu V.nnn söyler.
- **raf** — Uzun boş pano (0,50) → 0,34 m cilalı ahşap, tek çelik cep, pirinç etiket çerçevesi (KOMUTAN). Kart cebe 46 mm girer: başlık, özet ve ızgaranın üst yedi sırası görünür.
- **deste** — Tasarımsız → rafın hemen altında çelik tepside 40 boş kart, YENİ HAREKÂT başlıklı; yaylı kıskaç (kapaklı şalterin deseni: 1. tık kalkar, 4 s içinde 2. tık onaylar).
- **yerleşim** — Saat 0,07 m aşağı (y 1,18), raf 0,34 m'ye kısalıp yükseldi (1,14), deste rafın altına (0,95): üçü tek kadraja girer.
- **kamera M2** — (2,92 · 1,40 · −0,33), 40° → (3,12 · 1,35 · −0,40) → (2,2 · 1,30 · −0,40), 42°. Kart saatteyken kamera 0,18 m yükselir (hedef 1,49) ki kartın üstü görünsün.
- **okunurluk** — M2'de V.014 22 px, özgür 3/12 19 px (büyük harf 8,5 mm), KOMUTAN · SIĞINAK 07 14 px, PUANTAJ 35 px, VARDİYA 15 px. Izgara rakamları (doku) 8 px: süs, okunması gerekmez.

NESNELER
- **PunchClock** — 312 × 535 × 185 mm · 2476 üçgen. Parçalar: HourHand, MinuteHand (z, 0…−360°), Wheel0–2 (x, 36°/rakam; rakam d için 162 − 36·d bu dokuda), Plunger (y kayma 0…−12 mm), Lamp (swap). Çapa CardSlot.
- **ShiftCard** — 90 × 184 × 0,8 mm cardStock #EEE5CA · 12. Tilt (x, 0…−14°, alt kenar). Yuvalar TXT_Role, TXT_Shelter, TXT_Shift, TXT_Free, TXT_Sign. Çapalar H01–H80.
- **PunchHole** — Ø 4,2 mm sekizgen punchHole #151310, kartın 0,1 mm önünde; ölçek 0 → 1, 0,3 s.
- **CardRack** — 130 × 340 × 30 mm woodVarnish + paintGreyGreen cep · 436. Çapalar CardTop, DeckSeat.
- **BlankDeck** — 112 × 185 × 30 mm · 348. Clip (x, 0…−70°). TXT_Deck.
- **CancelSlip** — 70 × 30 mm fiş + ataş · 120. TXT_Stamp (colors.stamp.failed #9A1E14, çift kenar).

KARTIN DÜZENİ
- **satır 1–2** — KOMUTAN / SIĞINAK 07 · Courier Prime 700, büyük harf 6,5 mm, sola dayalı, üst kenardan 10 ve 21 mm.
- **satır 3** — V.014 · CP 700, 10 mm (kartın en büyük yazısı), 36 mm'de.
- **satır 4** — özgür 3/12 · CP 400, 8,5 mm, 52 mm'de; altında 1,5 mm kalın çizgi (60 mm).
- **ızgara** — 10 sütun × 8 satır, hücre 7,6 × 10,5 mm, ilk hücre merkezi üstten 70 mm; ince çizgi #9C9078, rakam #8C806A.
- **delik** — Sekizgen, kenarında küçük mor vardiya numarası (damga mürekkebi #5B3A78, doku); dokunun değil modelin işi.
- **paraf** — “K. — 07” Kalam, mavi mürekkep #1A2F5A, alt kenardan 12 mm.

ETKİLEŞİM VE DURUMLAR
- **kart** — Normal: cepte. Üzerinde: 4 mm öne, etiket “KAYITTAN DEVAM · V.014 · özgür 3/12”. Basılı: kalkar ve saate gider (geri alınmaz). Kayıt okunamıyorsa (öneri): gri kart, OKUNAMADI damgası, tıklanmaz.
- **saat** — Kayıt varken saate tıklamak karta tıklamakla aynı; kayıt yokken kalkmaz.
- **deste** — Üzerinde: 4 mm öne, etiket “YENİ HAREKÂT”. Kayıt varken 1. tık dikkat durumu: kıskaç kalkık, eski kart devrik, İPTAL EDİLİR fişi, etiket “2. tık onaylar · 4 sn”; süre dolarsa geri kapanır.
- **devre dışı** — Kart saate giderken ve giriş dizisinde raf, deste ve saat kalkmaz.

HAREKET VE SES
- **kart** — Cepten 0,12 s kalkar (karton sürtmesi), 0,4 s'de yay çizerek boğaza (tepe 8 cm, −6° yatar), 0,14 s'de 70 mm girer.
- **damga** — Piston 0,1 s iner, “ka-çunk”; delik 0,3 s'de açılır, lamba 0,08 s yanar; VARDİYA tekerleri dönmez (sayı kayıttan gelir).
- **saat** — Yelkovan dakikada bir sıçrar (tık); akrep sabit.
- **onay** — Kıskaç 0,15 s, eski kart 0,2 s devrilir, fiş 0,15 s; kapanış 4 s sonra.

İNGİLİZCE
COMMANDER / SHELTER 07 · S.014 · free 3/12 · TIME CLOCK · KYGA · CIVIL DEFENCE · SHIFT · NEW CAMPAIGN · TO BE CANCELLED · S.014. Yuvalar EN'e göre ölçülü (80 mm); punto küçülmez.
