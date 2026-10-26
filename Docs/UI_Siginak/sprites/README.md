# Sprite ve doku kiti

Beyaz sprite’lar Unity’de `Image.color` ile tokenlardaki renge boyanır (tint). Kenar (border) değerleri Sprite Editor’de L,T,R,B olarak girilir. Bütün UI sprite’ları: Texture Type = Sprite (2D and UI), Mesh Type = Full Rect, Wrap = Repeat (Tiled olanlar), sıkıştırma yok ya da yüksek kalite, Pixels Per Unit = 100 (harita taramaları için harita px ölçeği).

Gölge: ayrı bir siyah `ui_fill_*` kopyası, (x, y) kadar kaydırılmış, alfa tokenlardaki gibi (bulanıklık yok).

| Dosya | Boyut | 9 dilim L,T,R,B | Image Type | Renk (token) | Kullanım |
|---|---|---|---|---|---|
| ui_fill_r0.png | 32×32 | 3,3,3,3 | Sliced | colors.* (kâğıt, mürekkep, bakalit) | Düz zemin, köşe 0 px |
| ui_fill_r2.png | 32×32 | 4,4,4,4 | Sliced | colors.* (kâğıt, mürekkep, bakalit) | Düz zemin, köşe 2 px |
| ui_fill_r3.png | 32×32 | 5,5,5,5 | Sliced | colors.* (kâğıt, mürekkep, bakalit) | Düz zemin, köşe 3 px |
| ui_fill_r4.png | 32×32 | 6,6,6,6 | Sliced | colors.* (kâğıt, mürekkep, bakalit) | Düz zemin, köşe 4 px |
| ui_fill_r8.png | 32×32 | 10,10,10,10 | Sliced | colors.* (kâğıt, mürekkep, bakalit) | Düz zemin, köşe 8 px |
| ui_frame_1px_r2.png | 32×32 | 4,4,4,4 | Sliced | kenar rengi (edge, ink, plaque.border, stamp) | Kâğıt kenarı #D2C7AF / #C9B98A, plaka kenarı #6E5A33 (r3 için de uygun) |
| ui_frame_1_5px_r2.png | 32×32 | 4,4,4,4 | Sliced | kenar rengi (edge, ink, plaque.border, stamp) | Üzerinde etiketi mürekkep kenarı |
| ui_frame_2px_r3.png | 32×32 | 5,5,5,5 | Sliced | kenar rengi (edge, ink, plaque.border, stamp) | Fiş kapatma düğmesi, sektör damgası, çerçeve düğme (r4) |
| ui_frame_3px_r0.png | 32×32 | 5,5,5,5 | Sliced | kenar rengi (edge, ink, plaque.border, stamp) | Oda kartı damgası, rapor/emir damgası (KAPANDI, GİZLİ) |
| ui_frame_4px_r0.png | 32×32 | 6,6,6,6 | Sliced | kenar rengi (edge, ink, plaque.border, stamp) | Seçili kart iç sarı çerçeve #F5D646 |
| ui_frame_8px_r0.png | 32×32 | 10,10,10,10 | Sliced | kenar rengi (edge, ink, plaque.border, stamp) | Seçili oda ağzı 8 px #F5D646 (2 px mürekkep ayrı 1px çerçeve ile) |
| ui_frame_dashed_2px.png | 14×14 | 2,2,2,2 | Tiled (kenarlar döşenir) | stamp.locked #675F50 · disabledLine #A79D88 | KİLİTLİ damgası, devre dışı düğme, KAYIT YOK |
| ui_frame_dashed_3px.png | 20×20 | 3,3,3,3 | Tiled (kenarlar döşenir) | stamp.locked #675F50 · disabledLine #A79D88 | YEDEK damgası (karar panoları), büyük kesik kenar |
| ui_dash_h.png | 14×2 | 0,0,0,0 | Tiled | rgba(120,104,70,.55) | Rapor kopma çizgisi, satır noktalı ayraç (8/6) |
| ui_telex_holes.png | 10×13 | 0,0,0,0 | Tiled (dikey) | — (renkli) | Teleks şeridi ve rapor iki yan delik sırası (10 px sütun, 13 px aralık) |
| ui_hazard_stripes.png | 24×24 | 0,0,0,0 | Tiled | — | Şalter kapağı “KİLİTLİ” mühür bandı (devre dışı) |
| map_hatch_45_p16.png | 16×16 | 0,0,0,0 | Tiled (Pixels Per Unit = harita px) | mapLiberation.*Hatch (#9A1E14 + α) | İşgal 45° katmanı (α .50) · cephe %66,7 tek yön (dik aralık ≈ 11 px) |
| map_hatch_m45_p16.png | 16×16 | 0,0,0,0 | Tiled (Pixels Per Unit = harita px) | mapLiberation.*Hatch (#9A1E14 + α) | İşgal −45° katmanı (α .32) |
| map_hatch_45_p32.png | 32×32 | 0,0,0,0 | Tiled (Pixels Per Unit = harita px) | mapLiberation.*Hatch (#9A1E14 + α) | Cephe %33,3 tek yön (dik aralık ≈ 22 px) |
| map_grease_ring.png | 360×150 | — | Simple (bölge etiketine ölçekle) | — (#2B2622 boyalı) | Cephe: yağlı kalem halkası |
| map_candidate_ring.png | 360×150 | — | Simple | mapLiberation.candidateContour #463F34 | Aday: kesik halka (seçiliyken 4 px karşılığı: ölçeği 1,3) |
| map_stamp_free.png | 160×160 | — | Simple (−6° döndür) | mapLiberation.freeStamp #1A4789 | Özgür damgası: çift halka + bant; “ÖZGÜR / FREE” ve “V.011” TMP ile (bant: beyaz yazı) |
| map_pin.png | 40×40 | — | Simple | — | Cephe raptiyesi; görev fişinin sol üstü (ip bu noktaya bağlanır) |
| icon_team_figure.png | 68×52 | — | Simple (34 × 26 göster) | — (renkli; harf TMP ile de yazılabilir) | Ekip figürünün üstten simgesi: görev fişi ekip satırı, vardiya raporu. Haritada figürün kendisi 3D’dir (sprite değil) |
| mark_sector_done.png | 52×52 | — | Simple (26 px göster) | — (renkli) | GERİ ALINDI: mavi dolu kare + beyaz ✓ (özgür bölgede ✓✓✓ için de) |
| mark_sector_ready.png | 52×52 | — | Simple (26 px göster) | — (renkli) | BASKINA HAZIR: beyaz kare, mürekkep kenar + kırmızı ▸ |
| mark_sector_locked.png | 52×52 | — | Simple (26 px göster) | — (renkli) | KİLİTLİ: kesik gri kare + kilit |
| crt_scanlines.png | 4×4 | 0,0,0,0 | Tiled (Point filtre) | — | Tarama çizgisi: 4 px’te 2 px siyah α .30 |
| crt_vignette.png | 512×288 | — | Simple (tam ekran gerdir) | — | CRT köşe kararması (radyal, %55’ten sonra koyulaşır, en fazla α .72) |
| crt_rollbar.png | 8×96 | — | Simple (genişliğe gerdir) | — | Yuvarlanan parlak bant 96 px, 1,2 s’de yukarıdan aşağı |
| decal_coffee_ring.png | 180×180 | — | Simple | — | Kahve halkası (anlatı, tıklanmaz) |
| decal_signature.png | 190×44 | — | Simple | — | KYGA merkez imzası (K7); oyuncu imzası BAŞLA anında çizilir (mask ile soldan sağa açılır) |
| decal_first_aid_iso7010.png | 128×128 | — | Simple | — | Revir dolabı: ISO 7010 E003 ilk yardım (yeşil); Kızılhaç amblemi kullanılmaz |
| tx_paper_grain.png | 256×256 | — | Tiled (kâğıt üstü ayrı Image, α olduğu gibi) | — | Kâğıt lifi; bütün kâğıtların üstünde 128 px döşeme |
| tx_crt_static.png | 256×256 | — | Tiled (her kare rastgele UV ofseti) | — | CRT statik, α .55 (geçişte), bekleme ekranında α .15 |
| tx_counter_digits.png | 96×2464 | — | Doku (malzeme `_BaseMap`), Wrap Repeat | — | Sayaç çarkı rakam şeridi: 11 hücre (0–9 + tekrar 0), hücre 96 × 224, CP 700 176 #EFDCAE / zemin #1C150E |
| tx_counter_digits_red.png | 96×2464 | — | aynı | — | FON’un onda bir çarkı (zemin #801810) |
| tx_counter_shade.png | 96×224 | — | Simple (çarkın önünde sabit) | — | Çark gölgesi: üst α .70 → %26’da 0 → %74’te 0 → alt α .72 |

## Sayaç çarkı (shader’sız)

Her çark bir Quad: malzeme URP Unlit (ya da Simple Lit), doku `tx_counter_digits.png`, **Tiling (1, 1/11)**.
Rakam `d` için **Offset.y = 1 − (d + 1) / 11** (Unity UV aşağıdan yukarı). Bir sonraki rakama dönüş = Offset.y’yi
`1/11` azaltarak 0,35 s’de ease-out; 9 → 0 geçişinde 10. hücreye (ikinci 0) dön, bitince Offset.y’yi 0’ın hücresine
anında sıfırla. Ondalık çark kırmızı doku ile aynı yöntem. Çarkın önüne `tx_counter_shade.png` sabit Quad.
Çark başına 0,12 s gecikme, her rakam geçişinde hafif “tık”. (Yazı TMP değil, çünkü yuvarlanma hareketi gerekiyor.)
