# S6 · BASKIN
*KAMERA AYNI · İZOMETRİK TAHTA, ORTO 35° / 45°*

## ESKİ → YENİ

- **kenar panelleri** — Beyaz web kartları → ozalit pafta kâğıdı (#D3DBD6, mavi ızgara) kontrplak şerit üstünde, köşede bant. Tahtayla aynı malzeme.
- **tur şeridi** — Küçük 1/8 kutuları → “TUR 1/8” 30 px + 34 px hücreler; 7–8 kesikli kırmızı ve “SON 2 TUR”. Son turda hücre 8 kırmızı dolu, şeritte SON TUR damgası, EMRİ İLET'te “SON TUR”.
- **görev çipi** — Sıkışık tek kart → üç satır: görev + bölge özelliği (işaret + DAR KORİDOR); hedef çizimi + ad + kare (F4) + amaç, sağda CAN; GÜVEN + sivil sunucu sayısı. İkon metne binmez.
- **makine etiketi** — 7 px, hepsi aynı → tür işareti (S9) + 11 px kırmızı pip, robotun önünde zeminde. 3D gövde rengi türe göre (S9).
- **niyet sırası** — 14 px bayrak → 30 × 28 bayrak, 20 px rakam. Uygulanan niyet sarı vurgulu, bayrağı kırmızı dolu; biten niyet %35, rakamı çizik.
- **uzman canı** — Piyon üstündeki küçük kutular → altlık önünde etiket (11 px pip, plaka kalkanı); kartta pip + PLAKA, yaralıda boş pipler kırmızı kenarlı + YARALI.
- **yetenek** — “kasayı al”, “−1 robot” → “Kasadan ikmal al”, “Robota vur · −1 can”. Hedef yokken kesikli, varken düz kenar. Davranış aynı, yalnız ad değişti.
- **telsiz** — Sarı kutu → teleks kâğıdı fişi (anten + TELSİZ · K-07), bugünkü ölçüde; TELSİZ düğmesinde okunmamış sayısı.
- **makine evresi** — Çip “MAKİNELER” (kırmızı baklava); EMRİ İLET yerinde kilitli kâğıt “MAKİNELER HAREKET EDİYOR · girdi kilitli · 2/3 hamle”; Geri al ve GERİ ÇEKİL soluk.
- **uyanma** — Yeni robotun üstünde UYANDI etiketi; sıradaki uyanma halkası tahtada (bugünkü).
- **eğitim notu** — Sağ altta, EMRİ İLET'in üstünde: BASKIN · ADIM n/30.

## NESNELER

- **kontrplak şerit** — #C9A36B, alt kenar 4 px #93703F, üst 1 px #DDBF8A, gölge (3, 4); 9 dilim, iç pay 6 px.
- **ozalit sayfa** — 9 dilim + 24 px ızgara (#2C4C8E %10), kenar 1 px %55; etiket CP 700 14 #2C4C8E.
- **birim kartı** — 237 × 84; seçili #EFE3A2 + 3 px #F5D646, 6 px yukarı; disk Ø44 #C9A36B, bitmiş #8C734E.
- **tur hücresi** — 34 × 34; şimdiki mürekkep dolu, geçmiş #B9C3BE, son iki 2 px kesikli #9A1E14.
- **etiket, bayrak** — Etiket 24 px, #FBF7EC, kenar 1 px; işaret 18 px, pip 11 px. Bayrak iğnesi 2 × 34 #5E5A54, bayrak 30 × 28.
- **oklar** — Ateş 3,4 px düz; yakın dövüş 4,6 px yay (yükselme 14–26 px); kilit: yay + Ø26 kıskaç rozeti; uç 15–16 px; hepsi #B8241A, ZTest Always.

## ETKİLEŞİM VE DURUMLAR

- **birim kartı** — Normal. Üzerinde: 2 px yükselir, piyonun karesinde siyah köşeler. Seçili: sarı. Devre dışı: TUR BİTTİ, gri zemin + ✓. Düştü: DÜŞTÜ, kırmızı kesikli kenar.
- **yetenek alt düğmesi** — Hedef yok: kesikli, tıklanmaz. Hedef var: düz kenar. Üzerinde: zemin açılır, tahtada hedef kare yeşil köşeli. Basılı: gölge 0.
- **EMRİ İLET** — Normal: mürekkep. Üzerinde: 2 px yükselir. Basılı: 2 px iner. Devre dışı: makine evresinde kilitli kâğıt. Yetersiz kaynak durumu yok.
- **GERİ ÇEKİL** — İlk basış “Emin misin? Tekrar bas” (bugünkü); makine evresinde soluk.
- **TELSİZ (L)** — Okunmamış sayısı kırmızı yuvarlak içinde; açınca kayıt (bugünkü).
- **etiketler** — Makine etiketi üzerinde: tür adı + can + niyeti (kâğıt etiket).

## HAREKET VE SES

- **niyet sırası** — Hamle arası 0,35 s. Uygulanan okun sarı vurgusu 0,1 s'de gelir, bayrak kırmızıya döner; bitince ok 0,2 s'de %35'e iner.
- **uyanma** — Halka 0,4 s parlar, robot 0,5 s'de yükselir, UYANDI 0,12 s damga; telsiz fişi 0,25 s'de kayar, 3 s kalır, 0,3 s'de çekilir.
- **son tur** — Tur değişince hücre 8 kırmızıya 0,2 s; SON TUR damgası 0,12 s (ağır “tak”) + tek zil.
- **seçim** — Kart 0,1 s'de 6 px yükselir; mavi kareler 0,15 s'de belirir; ahşap “tık”.

## İNGİLİZCE

TURN · LAST 2 TURNS · LAST TURN · YOUR TURN · MACHINES · MACHINES ARE MOVING · input locked · MISSION · SUPPLY · NARROW CORRIDOR · Supply crate · Goal: take supplies from the crate · HP · TRUST · CIVILIAN SERVERS · RADIO · RULES · RETREAT · TECHNICIAN · EMP SPECIALIST · ANALYST · PLATE · INJURED · MOVE + ABILITY · TURN DONE · DOWN · Take from crate · Hit robot · −1 HP · Undo · SEND ORDERS · WOKE UP · LOCK THREAT. Sol üst grup EN'de 620 px; tahtanın kenarına değmez.
