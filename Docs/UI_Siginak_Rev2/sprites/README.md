# Sprite kiti · Revizyon 2 (1.2 eki dahil)

İlk turun kiti (`UI_Siginak/sprites/`: zeminler, çerçeveler, taramalar, damga, sektör işaretleri, CRT katmanları,
sayaç rakamları) geçerli; bu klasör yalnız **yeni** sprite'ları içerir. İçe aktarma kuralları aynı: Texture Type =
Sprite (2D and UI), Mesh Type = Full Rect, sıkıştırma yok ya da yüksek kalite, Pixels Per Unit = 100; Tiled olanlarda
Wrap = Repeat. Beyaz sprite'lar `Image.color` ile token rengine boyanır (tint). Çoğu 2× çizildi: ekranda yarı boyunda göster.

| Dosya | Boyut | 9 dilim L,T,R,B | Image Type | Renk (token) | Kullanım |
|---|---|---|---|---|---|
| glyph_corridor.png · glyph_sand.png · glyph_locker.png · glyph_mortar.png · glyph_drone.png · glyph_maint.png · glyph_shield.png | 72×72 | — | Simple (18–34 px göster) | beyaz → baskında `#9A1E14`, haritada/fişte `#1E1B16` | Bölge özelliği (dar koridor, kum fırtınası) ve imza/makine türü (kilitleyici, havan, dron, bakım robotu, kalkan dronu). Baskın can etiketi, fişin SAHA satırı, harita cephe bloğu, S9-A |
| glyph_*_frame.png | 72×72 | — | Simple (30–34 px) | beyaz → `#1E1B16` | Aynı işaret kare çerçeveli: görev fişi SAHA satırı, harita, işaretler kutusu |
| **1.2** glyph_safe.png · glyph_short.png · glyph_reinforce.png · glyph_open.png · glyph_cooling.png · glyph_crowd.png · glyph_armored.png · glyph_grid.png · glyph_factory.png · glyph_patrol.png | 72×72 | — | Simple (22–34 px göster) | beyaz → `#1E1B16` | Bölge özelliği: güvenli giriş, kısa nöbet, takviye, açık arazi, soğutma ağı, kalabalık şehir, zırhlı dolap, elektrik şebekesi, kopya fabrikası, hızlı devriye. Harita cephe bloğu ve işaretler kutusu (doku 34 px, ekranda ~22 px), fişin SAHA satırı (`_frame`, 30 px). Eşleme: `raid_tokens.json → regionTraits` |
| **1.2** glyph_armordrone.png · glyph_bomber.png · glyph_replicator.png | 72×72 | — | Simple (18–34 px göster) | beyaz → baskında `#9A1E14`, haritada/fişte `#1E1B16` | Makine türü: zırhlı dron, patlayan robot, çoğaltıcı. Baskın can etiketi (18 px), fişin SAHA satırı (imza), harita, S9-A. Eşleme: `machineTypes.glyph` |
| **1.2** glyph_safe_frame.png … glyph_replicator_frame.png (13 dosya) | 72×72 | — | Simple (30–34 px) | beyaz → `#1E1B16` | Yukarıdaki 13 işaretin kare çerçeveli sürümü; çerçeve öteki `_frame` dosyalarıyla aynı (33 birim kare, çizgi 2,9) |
| threat_tier_1…4.png | 72×72 | — | Simple (28–34 px) | — (renkli: mürekkep çerçeve, kırmızı dolu basamak) | TEHDİT kademesi; dolu basamak sayısı = kademe, yanında “n/4” |
| mark_sector_reocc.png | 52×52 | — | Simple (26 px) | — (renkli) | YENİDEN İŞGAL: krem kare, kırmızı kenar + ✕ (diğer sektör işaretleri ilk turdan) |
| ui_frame_double_6px.png | 32×32 | 8,8,8,8 | Sliced | beyaz → stamp rengi | Sonuç damgaları (SEKTÖR GERİ ALINDI, GERİ ÇEKİLDİ, KAZANILDI, KAYBEDİLDİ, VURULDU, karşı saldırı kutusu) |
| ui_ply_strip.png | 32×32 | 2,2,2,6 | Sliced | — (renkli: #C9A36B, üst #DDBF8A, alt kenar #93703F) | Baskın kenar panellerinin kontrplak şeridi (iç pay 6 px) |
| tx_ozalit_grid.png | 24×24 | 0,0,0,0 | Tiled (ekranda 24 px) | beyaz → `#2C4C8E` α .10 | Ozalit sayfanın ızgarası; zemin `ui_fill_r0` #D3DBD6, kenar `ui_frame_1px_r2` #2C4C8E α .55 |
| ui_tape.png | 88×30 | — | Simple (44×15, ±24°) | — (renkli, α .8) | Ozalit sayfanın köşesindeki bant; panel başına en çok bir |
| pip_fill.png · pip_empty.png | 22×22 | — | Simple (11 px) | beyaz → makine `#9A1E14`, uzman `#1E1B16`, kayıp pip kenarı `#9A1E14` | Can pipleri (dolu = var, boş = kayıp) |
| cell_dashed.png | 28×28 | — | Simple (14 px) | beyaz → `#9A1E14` | Alarm ölçeğinde boş 8–10 hücresi (kesik kenar); dolu hücre `pip_fill` |
| icon_eye · icon_person · icon_antenna · icon_move · icon_bolt · icon_rules · icon_retreat · icon_radio · icon_undo · icon_target · icon_armor · icon_close | 24–52 | — | Simple (yarı boy) | beyaz → bağlama göre | Gözetleme bloğu, oda kartı personel, TELSİZ fişi, yetenek satırları, üst şerit düğmeleri, hedef etiketi, zırh |
| icon_plate.png | 30×34 | — | Simple (15×17) | — (renkli: #9A9C98 + mürekkep kenar) | Zırh plakası (uzman etiketi, birim kartı) |
| icon_civilian.png | 44×44 | — | Simple (22 px) | — (renkli) | Görev çipinde sivil sunucu |
| badge_clamp.png · badge_wrench.png | 56×56 · 48×48 | — | Simple (yarı boy) | — (renkli) | Kilit niyetinin yay ortasındaki rozeti · onarım niyeti rozeti |
| **1.2** badge_shield.png · badge_clone.png | 56×56 · 48×48 | — | Simple (yarı boy: 28 · 24 px) | — (renkli: krem `#FBF7EC` + `#854005`) | Kalkan niyetinin rozeti (kalkanı alacak makinenin tepe noktasının 22 px üstü) · kopyalama niyetinin rozeti (hedef karenin ortası, z 0,3; kenarı kesik) |
| ui_pointer.png | 20×32 | — | Simple (10×16) | beyaz → `#1E1B16` | Görev fişinde seçili sektör satırının sol ok ucu |
| ui_counter_comma.png | 40×72 | — | Simple (sayaç yüzünde Quad, 20×36 mm) | — (renkli pirinç) | FON sayacının ondalık virgülü; EN yerelinde kuyruğu gizlenmiş nokta kullan (`colors.counter.decimalShape`) |
| tx_counter_digits_tenths.png | 96×2464 | — | Doku (`_BaseMap`), Wrap Repeat | — | FON onda bir çarkı: açık zemin #E9DDBE, rakam #1C150E; ilk turun `tx_counter_digits_red.png` yerine. Çark formülü ilk turdaki gibi (Tiling 1, 1/11; Offset.y = 1 − (d + 1)/11) |
| decal_tally5.png | 128×96 | — | URP Decal ya da saydam Quad | beyaz → tebeşir #E6E2D4 α .8 | Beşli tebeşir çentiği: vardiya sayacı (alarm panosu üstü) ve kapı yanındaki ziyaretçi çentiği |

Yazılar (damga metni, etiket, sayı) her zaman TMP; sprite yalnız çerçeve, işaret ve doku.

**1.2 · içe aktarma notu (bütün `glyph_*` dosyaları, eskiler dahil):** Generate Mip Maps açık, Filter Mode = Trilinear. 72 px’lik çizim 18–22 px’te gösterilir; Sprite (2D and UI) türünde mipmap varsayılan olarak kapalıdır ve kapalıyken ince çizgiler 18 px’te kırılır ya da kaybolur.
