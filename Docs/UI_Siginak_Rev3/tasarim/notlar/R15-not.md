# R15 · Uygulama notu
*Pano: `R15-not.dc.html` · görüntü: `../panolar/R15-not.jpg`*

### R15 · PANO (K4)
ESKİ → YENİ · NESNELER · DURUMLAR · HAREKET VE SES
ESKİ → YENİ
- **pano** — Ekran gibi okunan düz yeşil uGUI tahta → ayaklı, mıknatıslı çelik planlama panosu (1196 × 636 mm), eski raf ve maketin yerinde, masanın sol önüne dönük. K1’de yandan ve küçük görünür.
- **yönerge** — “GÖREV PANOSU · kişiyi görev alanına…” satırları → kelimesiz baskı: kesikli kart dış hattı, kişi simgesi, teçhizat şeritleri, Boşta’nın yelpaze izi.
- **düğmeler** — KİŞİ DOSYASI ve MASAYA DÖN düz düğmeleri → manila klasör (tel sepet) ve emaye levha.
- **kartlar** — 63 × 88 koleksiyon kartı → 80 × 120 sicil kartı; teçhizat köşe etiketi yerine kartın arkasına sıkışan, altında 14 mm şerit gösteren kartlar.
- **hammadde** — STOK / BOŞ / AYRI üç sayı → büyük serbest sayı + birim noktaları (ayrılmış kırmızı halka) + alt satırda stok ve ayrı.
- **tarif** — “AÇMAK İÇİN TIKLA” kutusu → rafta duran ciltli defter (R18).
- **ekran şeridi** — K4’te sayaç şeridi ve istasyon çipleri gizli; başlık arduvazı vardiya, erzak gidişi ve kişiyi taşır.

NESNELER
- **PlanningBoard** — 1196 × 636 mm pano, 30 mm dikmeler, T ayak, 4 teker · 3,7 bin üçgen (4000). Parçalar: BackSign (z kayma), Flap (x 0/180), LockRod (swap), HurdaLid (x 0…28°), Lamp (swap). Işık BoardLamp (spot 58°, gölgeli). Çapalar Slot_*, Bosta_Deck, Bosta_HandLeft, Order_*, Folder, Mat_*, Kit_*, Hurda_Slot, Cam_K4, Look_K4.
- **Kartlar** — CardPerson 80 × 120 · CardItem 80 × 50 · CardMaterial 80 × 56 · CardChip 56 × 40 · CardRecipe 112 × 80; hepsi 0,6 mm. Yüz dokusu çalışma anında (boardtex.js düzeni), mıknatıs düğmesi Ø 10 mm.
- **PersonFolder** — Manila 130 × 180 mm, Cover (y 0…−176°). Açık sayfalar R10 kişi dosyası.
- **RecipeBook** — A6 ciltli, Cover ve Leaf parçaları (R18).
- **malzemeler** — boardPanel #56604F, boardFace (kelimesiz baskı dokusu), brassWorn çerçeveler, brass plakalar, enamelBadge, slate, galv, manila, cardFace/cardBack/cardEdge.

ETKİLEŞİM VE DURUMLAR
- **kişi kartı** — normal · üzerinde (4 mm kalkış, 400 ms sonra ad + görev etiketi) · tutuldu (10 mm, 3°) · hedef üstünde (önizleme etiketi) · bırakıldı (oturma) · geçersiz (geri döner) · kilitli (2 mm titreme).
- **cep** — boş (baskı) · dolu · uygun hedef (amber çerçeve) · uygun değil (sönük) · takas önizlemesi.
- **Boşta** — kapalı · açılıyor · açık · bir kart alındı · toplanıyor. Destenin üst kartı her an sürüklenir.
- **eğitim** — konu satırına tık: halka 0,2 s’de yeniden çizilir; kişi yokken de seçilebilir (kişiye ait seçim kişiyle gider).
- **dosya** — kapalı (ad kapakta) · üzerinde · açık (öne gelir, R17/R16). Kişi kartına çift tık da açar.
- **depo / hurda** — yığın üst kartı sürüklenir · hurda kutusu hedef olunca kapak 6 mm aralanır, bırakınca 28° kalkar.
- **kilit** — baskın emri ya da fiş basılınca: çubuk, asma kilit, kırmızı plaka; okuma serbest.
- **yetersiz kaynak** — iş emrine eksik girdi: formda kırmızı kalemle “eksik: 1 kumaş”; malzeme harcanmaz.
- **dikkat** — İNCELE bayrağı (kırmızı) ve saha cebindeki “KİT · GEREKLİ” şeridi; ikisi de ses vermez.

HAREKET VE SES
- **kamera** — SIĞINAK tuşu → K4: 0,6 s ease-in-out; sürüklerken kamera dönmez. MASAYA DÖN: 0,5 s.
- **kart** — kalkış 0,08 s, takas 0,32 s, geri dönüş 0,25 s kavis, oturma 0,06 s + mıknatıs “tık”.
- **Boşta** — kart başına 320 ms, aralarında 70 ms; açılış üstten, toplanma alttan.
- **kilit** — çubuk 0,4 s + asma kilit “klak”; plaka 0,3 s + metal “tık”.
- **başlık** — plan değişince tebeşir satırı 0,2 s silinme lekesiyle yeniden yazılır; ses yok.
- **ışık** — K4: pano lambası (spot, tek gölgeli) + tavan ampulü dolgu (gölgesiz) + ortam 0,5. K1’de pano lambası 2,4 şiddetle yanık.
