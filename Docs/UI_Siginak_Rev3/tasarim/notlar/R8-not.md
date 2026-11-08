# R8 · Uygulama notu
*Pano: `R8-not.dc.html` · görüntü: `../panolar/R8-not.jpg`*

### R8 · CRT VE TELSİZ (K5a) · DALIŞ
DESKGAME · K5a · DALIŞ 2,4 S · 2 GÖLGELİ IŞIK
ESKİ → YENİ
- **CRT masası** — Düz kutu → çelik konsol 0,95 × 0,76 × 0,58: linolyum üst, pirinç ön şerit, iki çekmeceli ayak, takviyeli arka panel; sağ arkada telsize 0,11 m yükseltilmiş raf, lastik kablo deliği.
- **CRT** — Kutu + düz ekran → 1950'ler monitörü: yuvarlatılmış kasa, geriye daralan kaput (yarıklar dokuda), 32 mm derin bezel ve köşe vidaları, 10 mm bombeli ekran, taşıma sapları, iki bakalit düğme, yeşil LED, pirinç TELSİZ · K-07.
- **telsiz** — Kutu + iki silindir → ceviz kasalı tüplü alıcı: bakalit panel, pirinç çerçeveli hoparlör bezi, arkadan aydınlatılan KANAL skalası ve kayan ibre, SİNYAL göstergesi, SES / AYAR, teleskop anten.
- **mikrofon · defter** — Çubuk + küre → döküm kaideli masa mikrofonu, önde bas-konuş kolu. Düz kâğıt → açık A5 kayıt defteri: kıvrık sayfa, kalkık köşe, kurşun kalem.
- **ekran** — Aynı (Rev2 S1): DİNLEMEDE ters video, ALFA · HAZIR, cephe / sektör, YZ ALARMI.
- **kamera K5a** — Ekran yereli (0 · −0,02 · 0,62) → (0 · −0,035 · 0), 46°. Gerçek ölçülerle telsizin sağ yarısı (KANAL ibresi, SİNYAL, SES / AYAR) kadraj dışında kalıyor.
- **öneri** — K5a'yı ekran yerelinde ≈ +0,14 m sağa kaydır, FOV 46 → 52° (tahmin, three.js sahnesinde doğrulanmalı): telsizin yüzü bütün görünür, CRT sol yarıda kalır. Kayıt defteri K5a'da ~5 px: okunmaz, son kaydı kâğıt etiket taşır.

NESNELER
- **CrtTable** — 0,95 × 0,87 × 0,59 · paintOlive #4A5140, linoDesk #2F4A3B, brassDark şerit, rubber · 1482 üçgen.
- **Crt** — 0,49 × 0,44 × 0,51 · crtCase #4F5446, crtBezel #1C1F19, crtScreen #07100B (emission #A6E8B8 1,0), crtLed #3FBF5F 1,5, bakeliteBlack, plate · 2126 · Screen, Led (swap).
- **Radio** — 0,46 × 0,30 (anten 0,72) × 0,29 · radioBody #4A3020, bakelite #24180F, radioGrille, brassWorn, radioDial (emission 0,9), dialCream, glassClear α.18 · 2112.
- **Microphone** — 0,11 × 0,19 × 0,19 · castIron, chrome, micGrille (doku), bakeliteBlack · 770.
- **LogBook** — 0,31 × 0,03 × 0,21 · book1 #2E4A3A, paper #EFE6D0, kalem · 420 · TMP başlık + iki sayfa (Kalam, saat sütunu Courier Prime).
- **ışıklar** — ScreenLight #7FD9A0 nokta 0,6 / 2,4 m, gölgesiz (ekran kapalıyken 0) · DialLight #FFA850 0,1 / 0,6 m, gölgesiz.

DURUMLAR
- **dinlemede** — Dört satır, LED yeşil, kadran ışığı yanık, DialPointer K-07'de (0), SİNYAL ibresi son hattın değerinde.
- **dalış sonu** — Emission ve ScreenLight 1,4×; ekran HAT AÇILIYOR. Öneri: kod DialPointer'ı K-07'ye kaydırır, Needle değere kalkar (0,3 s), KnobTune birlikte döner.
- **baskında** — Oda Baskın sahnesinin altında bekler; tıklanmaz.
- **dönüş** — Tüp kapanır (Screen swap: emission 0, ScreenLight 0), kamera 0,3 s bekler; ekran DİNLEMEDE'ye döner.
- **parçalar** — DialPointer x ±0,09 m · Needle z ±50° · KnobVolume, KnobTune z ±150° · PTT x 0 → −8° · Screen, Led swap.

ETKİLEŞİM
- **tıklananlar** — K5a'da yok: CRT, telsiz, mikrofon ve defter bilgi nesnesi; üzerine gelince kalkmazlar.
- **üzerinde** — 400 ms sonra kâğıt etiket: “TELSİZ · K-07 · dinlemede”, “TELSİZ KAYDI · 014/05 dinlemede” (defterin okunmayan son kaydı).
- **devre dışı** — Dalış ve baskın boyunca oda etkileşimi kapalı; anahtar rayı kilitli.
- **atlama** — Tık (ya da boşluk) dalışı atlar: son kare, emission 1,4×, kesme.

HAREKET VE SES
- **emir → dalış** — EMRİ İLET → fiş 0,09 s kapanır → 0,3 s → kamera 2,4 s tek hareket: geri çekil · dön · yaklaş (vuruşlar üst üste, kesirler SiginakCameraRig'de).
- **son 0,6 s** — CRT emission + ScreenLight 1 → 1,4×; ekran dolunca HAT AÇILIYOR (0,15 s) ve senkron kutusu (0,1 s) → Baskın yüklenir.
- **dönüş** — Tüp kapanır, kamera 0,3 s ekranda bekler (crtReturnDelayS), aynı yolun tersi, harita pozu.
- **ses** — Dalışta oda uğultusu alçalır, tüp vınlaması ve telsiz cızırtısı yükselir; kesmede röle “klak”; dönüşte tüp “tık”, vınlama söner.
- **ışık** — K5a: ampul + masa lambası gölgeli (2), CRT sızıntısı ve kadran gölgesiz. Bloom yok; post-processing kapalıyken de aynı.

İNGİLİZCE
RADIO · K-07 · CHANNEL · SIGNAL · VOLUME · TUNE · RADIO LOG · K-07 · LISTENING · ALPHA · READY · E. EUROPE / 02 · AI ALARM 4/10 · LINE OPENING. Plaka yuvaları EN'e göre (%30 uzun) ölçülü; punto küçülmez.
