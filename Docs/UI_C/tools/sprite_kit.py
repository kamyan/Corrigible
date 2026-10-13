# -*- coding: utf-8 -*-
"""
C · Analist masası — Unity sprite kiti üreticisi.

Her sprite 1x CSS piksel ölçüsünde SVG ya da tasarımın kendi CSS'iyle tanımlanır ve
Playwright/Chromium ile device-scale-factor 2'de, şeffaf zeminde PNG olarak çizilir.
Çıktı: art/sprites/*.png (@2x), art/textures/*.png (@2x, tekrar eden), art/sprites/sprites.json,
art/textures/textures.json, art/sprites/_contact_sheet_*.png, art/map/c_harita@2x.png

Kullanım:  python3 tools/sprite_kit.py            (paket kökünden ya da herhangi bir yerden)
Gerekenler: pip install playwright pillow ; playwright install chromium
"""
import math, json, os, sys, asyncio, re

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
REF_HTML = os.path.join(ROOT, 'reference', 'html', 'C1_main.html')
OUT_S = os.path.join(ROOT, 'art', 'sprites')
OUT_T = os.path.join(ROOT, 'art', 'textures')
OUT_M = os.path.join(ROOT, 'art', 'map')
SCALE = 2

W = '#FFFFFF'
SPRITES = []   # dict(name, w, h, html, kind='sprite'|'texture', mode, border(L,T,R,B @1x), pivot(Unity, y yukarı), tint, group, use)


def svg(w, h, inner, vb=None, extra=''):
    vb = vb or f'0 0 {w} {h}'
    return f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="{vb}" style="display:block"{extra}>{inner}</svg>'


def add(name, w, h, html, *, kind='sprite', mode='simple', border=None, pivot=(0.5, 0.5), tint=False, group='', use='', wrap='clamp'):
    for i in set(re.findall(r'id="([^"]+)"', html)):  # aynı sayfada çakışmasın
        html = html.replace(f'id="{i}"', f'id="{i}__{name}"').replace(f'url(#{i})', f'url(#{i}__{name})')
    SPRITES.append(dict(name=name, w=w, h=h, html=html, kind=kind, mode=mode, border=border, pivot=pivot, tint=tint, group=group, use=use, wrap=wrap))


def rr(x, y, w, h, tl=0, tr=0, br=0, bl=0):
    """köşe yarıçapları ayrı yuvarlatılmış dikdörtgen yolu"""
    return (f'M{x + tl} {y}H{x + w - tr}' + (f'A{tr} {tr} 0 0 1 {x + w} {y + tr}' if tr else '') +
            f'V{y + h - br}' + (f'A{br} {br} 0 0 1 {x + w - br} {y + h}' if br else '') +
            f'H{x + bl}' + (f'A{bl} {bl} 0 0 1 {x} {y + h - bl}' if bl else '') +
            f'V{y + tl}' + (f'A{tl} {tl} 0 0 1 {x + tl} {y}' if tl else '') + 'Z')


def ring_rr(w, h, s, tl=0, tr=0, br=0, bl=0, open_bottom=False):
    """iç kenarlık (CSS border gibi): dış yol − iç yol, evenodd"""
    outer = rr(0, 0, w, h, tl, tr, br, bl)
    ih = h - s + (2 if open_bottom else -s)
    inner = rr(s, s, w - 2 * s, ih, max(tl - s, 0), max(tr - s, 0), 0 if open_bottom else max(br - s, 0), 0 if open_bottom else max(bl - s, 0))
    return f'<path d="{outer}{inner}" fill="{W}" fill-rule="evenodd"/>'


# ------------------------------------------------------------------ 1 · ŞEKİLLER (beyaz, Image.color ile boyanır)
G = 'shapes'
for r in (0, 1, 2, 3):
    n = 8 if r <= 1 else 10
    b = 3 if r <= 1 else 4
    add(f'sh_fill_r{r}', n, n, svg(n, n, f'<path d="{rr(0, 0, n, n, r, r, r, r)}" fill="{W}"/>'), mode='sliced', border=(b, b, b, b), tint=True, group=G,
        use={0: 'düz dolgu (null sprite yerine; atlas toplu çizimini bozmaz)', 1: 'kâğıt sayfa, harita etiketi', 2: 'kart, fiş, plaka etiketi, takvim', 3: 'düğme, tuş fişi, zarf, damga zemini'}[r])
add('sh_fill_r4_tl0', 12, 12, svg(12, 12, f'<path d="{rr(0, 0, 12, 12, 0, 4, 4, 4)}" fill="{W}"/>'), mode='sliced', border=(5, 5, 5, 5), tint=True, group=G, use='klasör gövdesi (.fbody: 0 4 4 4)')
add('sh_fill_r7_top', 20, 20, svg(20, 20, f'<path d="{rr(0, 0, 20, 20, 7, 7, 0, 0)}" fill="{W}"/>'), mode='sliced', border=(8, 8, 8, 2), tint=True, group=G, use='klasör sekmesi ve alt çubuk sekmesi (7 7 0 0)')
for r, s, n, b, use in [(0, 1, 8, 3, 'çerçeve 1 px'), (1, 1, 8, 3, 'kâğıt sayfa kenarı'), (2, 1, 10, 4, 'kart, fiş kenarı'), (3, 1, 10, 4, 'düğme, zarf kenarı'),
                        (0, 1.5, 8, 3, 'onay kutusu (.cb)'), (2, 1.5, 10, 4, 'eylem kartı üstüne gelme, durum çipi'), (2, 2, 10, 4, 'seçenek kartı üstüne gelme (C3), vurgulu zaman kartı (C4)')]:
    nm = f'sh_frame_r{r}_{str(s).replace(".", "_")}'
    add(nm, n, n, svg(n, n, ring_rr(n, n, s, r, r, r, r)), mode='sliced', border=(b, b, b, b), tint=True, group=G, use=use)
add('sh_frame_r4_tl0_1', 12, 12, svg(12, 12, ring_rr(12, 12, 1, 0, 4, 4, 4)), mode='sliced', border=(5, 5, 5, 5), tint=True, group=G, use='klasör gövdesi kenarı')
add('sh_frame_r7_top_1', 20, 20, svg(20, 20, ring_rr(20, 20, 1, 7, 7, 0, 0, open_bottom=True)), mode='sliced', border=(8, 8, 8, 2), tint=True, group=G, use='sekme kenarı (alt kenar yok)')
add('sh_frame_r7_top_1_5', 20, 20, svg(20, 20, ring_rr(20, 20, 1.5, 7, 7, 0, 0, open_bottom=True)), mode='sliced', border=(8, 8, 8, 2), tint=True, group=G, use='açık sekme kenarı (.dtab.on)')
# sekmenin iç üst parıltısı (inset 0 1px 0 rgba(255,255,255,.35)) — kenarlığın 1 px içinde
hl = f'<path d="{rr(1, 1, 18, 21, 6, 6, 0, 0)}{rr(1, 2, 18, 21, 6, 6, 0, 0)}" fill="{W}" fill-rule="evenodd"/>'
add('sh_tab_highlight', 20, 20, svg(20, 20, f'<clipPath id="c"><rect x="0" y="0" width="20" height="18"/></clipPath><g clip-path="url(#c)">{hl}</g>'), mode='sliced', border=(8, 8, 8, 2), tint=True, group=G, use='sekme iç üst parıltı: beyaz %35')
# tuş fişi: 1 px kenar + 2 px alt kenar, r3
key = f'<path d="{rr(0, 0, 10, 10, 3, 3, 3, 3)}{rr(1, 1, 8, 7, 2, 2, 1, 1)}" fill="{W}" fill-rule="evenodd"/>'
add('sh_frame_key', 10, 10, svg(10, 10, key), mode='sliced', border=(4, 4, 4, 5), tint=True, group=G, use='tuş fişi (.key): 1 px kenar + 2 px alt')


def dashed_frame(r, s=1, b=3, p=5, dash=3):
    n = 2 * b + p
    corner = f'<clipPath id="k"><rect x="0" y="0" width="{b}" height="{b}"/><rect x="{n - b}" y="0" width="{b}" height="{b}"/><rect x="0" y="{n - b}" width="{b}" height="{b}"/><rect x="{n - b}" y="{n - b}" width="{b}" height="{b}"/></clipPath>'
    ring = ring_rr(n, n, s, r, r, r, r).replace('/>', ' clip-path="url(#k)"/>')
    g = (p - dash) / 2
    d = (f'<rect x="{b + g}" y="0" width="{dash}" height="{s}" fill="{W}"/><rect x="{b + g}" y="{n - s}" width="{dash}" height="{s}" fill="{W}"/>'
         f'<rect x="0" y="{b + g}" width="{s}" height="{dash}" fill="{W}"/><rect x="{n - s}" y="{b + g}" width="{s}" height="{dash}" fill="{W}"/>')
    return n, svg(n, n, corner + ring + d)


for r, s, b, use in [(0, 1, 3, 'kesik kenar: risk kutusu, önizleme yuvası (C3)'), (2, 1, 3, '“~ RAPOR” fişi, devre dışı eylem kartı'), (3, 1, 4, 'devre dışı kâğıt düğme, devre dışı kapat düğmesi'), (3, 2, 4, 'klavye odak halkası (mavi, 2 px dışarıda)')]:
    n, h = dashed_frame(r, s, b)
    add(f'sh_dash_r{r}_{s}', n, n, h, mode='tiled', border=(b, b, b, b), tint=True, group=G, use=use + ' · Image Type = Tiled (kesikler gerilmez)')
# kesik sekme (devre dışı alt çubuk sekmesi)
n = 21
tab_ring = ring_rr(n, n, 1, 7, 7, 0, 0, open_bottom=True)
tab_dash = (f'<clipPath id="k2"><rect x="0" y="0" width="8" height="8"/><rect x="13" y="0" width="8" height="8"/></clipPath>'
            + tab_ring.replace('/>', ' clip-path="url(#k2)"/>')
            + f'<rect x="9" y="0" width="3" height="1" fill="{W}"/><rect x="0" y="9" width="1" height="3" fill="{W}"/><rect x="20" y="9" width="1" height="3" fill="{W}"/>')
add('sh_dash_r7_top_1', n, n, svg(n, n, tab_dash), mode='tiled', border=(8, 8, 8, 8), tint=True, group=G, use='kilitli alt çubuk sekmesi (kesik kenar)')
# çizgiler
add('ln_dot_h', 16, 1, svg(16, 1, ''.join(f'<rect x="{x}" y="0" width="1" height="1" fill="{W}"/>' for x in range(0, 16, 2))), mode='tiled', border=(0, 0, 0, 0), tint=True, group=G, use='noktalı ayraç (border 1px dotted) · Tiled, yükseklik 1')
add('ln_dot_v', 1, 16, svg(1, 16, ''.join(f'<rect x="0" y="{y}" width="1" height="1" fill="{W}"/>' for y in range(0, 16, 2))), mode='tiled', border=(0, 0, 0, 0), tint=True, group=G, use='dikey noktalı çizgi')
add('ln_dash_h', 20, 1, svg(20, 1, ''.join(f'<rect x="{x}" y="0" width="3" height="1" fill="{W}"/>' for x in range(0, 20, 5))), mode='tiled', border=(0, 0, 0, 0), tint=True, group=G, use='kesik ayraç (border 1px dashed) · Tiled')
# daireler ve halkalar
for d in (10, 16, 24, 32, 64, 128):
    add(f'sh_circle_{d}', d, d, svg(d, d, f'<circle cx="{d / 2}" cy="{d / 2}" r="{d / 2}" fill="{W}"/>'), tint=True, group=G,
        use={10: 'lamba, küçük nokta', 16: 'mühür zemini, lejant örneği', 24: 'numara rozeti (nb 22), ipucu işareti', 32: 'düğüm diski, hız tuşu zemini (30)', 64: 'seçili bölge fosforu (52, %72)', 128: 'Radial360 dolgu: belirsizlik kaması (r52), halka ilerleme'}[d])
for o, s, use in [(9, 1, 'lamba kenarı'), (11, 1, 'lejant renk örneği kenarı'), (14, 1.5, 'YZ günlüğü kırmızı nokta rozeti kenarı'), (20.5, 1.5, 'şerit grafik olay numarası kenarı (krem)'), (30, 2, 'hız tuşu kenarı'), (33.5, 1.5, 'düğüm mürekkep halkası (r16, 1,5)')]:
    c = math.ceil(o + 2)
    nm = f'sh_ring_o{str(o).replace(".", "_")}_{str(s).replace(".", "_")}'
    add(nm, c, c, svg(c, c, f'<circle cx="{c / 2}" cy="{c / 2}" r="{o / 2 - s / 2}" fill="none" stroke="{W}" stroke-width="{s}"/>'), tint=True, group=G, use=use + f' · tuval {c}×{c}, merkez ortada')
add('sh_pill_h18_fill', 18, 18, svg(18, 18, f'<rect x="0" y="0" width="18" height="18" rx="9" fill="{W}"/>'), mode='sliced', border=(9, 0, 9, 0), tint=True, group=G, use='sayı rozeti (.dot) zemini; yatay uzar')
add('sh_pill_h18_ring', 18, 18, svg(18, 18, f'<rect x=".75" y=".75" width="16.5" height="16.5" rx="8.25" fill="none" stroke="{W}" stroke-width="1.5"/>'), mode='sliced', border=(9, 0, 9, 0), tint=True, group=G, use='sayı rozeti kenarı 1,5')
# hız tuşu seçili: içe gölge hilali (inset 0 2px 0 rgba(0,0,0,.28)) — 30 px dairenin üst iç kenarı
add('sh_circle30_inset_top', 30, 30, svg(30, 30, '<mask id="m"><circle cx="15" cy="15" r="13" fill="#fff"/><circle cx="15" cy="17" r="13" fill="#000"/></mask>'
                                           f'<rect width="30" height="30" fill="{W}" mask="url(#m)"/>'), tint=True, group=G, use='seçili hız tuşu içe gölgesi (siyah %28), kenarlığın içinde')

# ------------------------------------------------------------------ 2 · DESENLER (beyaz, Tiled)
G = 'patterns'
lines = ''.join(f'<line x1="{c + 6}" y1="-6" x2="{c - 6}" y2="6" stroke="{W}" stroke-width="1.4"/>' for c in (-5, 0, 5, 10))
lines = ''.join(f'<line x1="{c + 10}" y1="{-10}" x2="{c - 10}" y2="{10}" stroke="{W}" stroke-width="1.4"/>' for c in (-5, 0, 5, 10))
add('pt_hatch', 5, 5, svg(5, 5, lines), mode='tiled', border=(0, 0, 0, 0), tint=True, group=G,
    use='“/” tarama, 1,4 px çizgi, ~3,5 px aralık · hedef kutuları (#4A4640), ilerleme çubuğu (#4A4640), aralık bandı (#A9520A %75) · Image Tiled')

# ------------------------------------------------------------------ 3 · İKONLAR (beyaz çizgi, 16 birimlik viewBox; kullanılan boyutta @2x)
G = 'icons'
ICONS = [
    ('grip', '<path d="M6 4h.01M10 4h.01M6 8h.01M10 8h.01M6 12h.01M10 12h.01" stroke-width="2.4"></path>', [(14, 1.6)], 'pencere tutamacı ⠿'),
    ('minimize', '<path d="M4 8.5h8"></path>', [(12, 1.6)], 'küçült'),
    ('close', '<path d="M4.5 4.5l7 7"></path><path d="M11.5 4.5l-7 7"></path>', [(12, 1.6)], 'kapat'),
    ('check', '<path d="M2.5 8.8c1.4.9 2.6 2.2 3.5 3.6C7.7 8.6 10.3 5.4 13.6 3"></path>', [(14, 2)], 'tamamlanan araştırma (yeşil)'),
    ('play', '<path d="M5.2 3.6l7 4.4-7 4.4z"></path>', [(14, 1.6), (12, 1.6)], 'süren proje / sürdür'),
    ('pause', '<path d="M5.5 3.5v9"></path><path d="M10.5 3.5v9"></path>', [(12, 1.6), (12, 2), (14, 1.6)], 'duraklat (12/2 = hız tuşu)'),
    ('lock', '<rect x="3.5" y="7.2" width="9" height="6.6" rx="1"></rect><path d="M5.5 7.2V5.3a2.5 2.5 0 0 1 5 0v1.9"></path>', [(14, 1.6), (13, 1.6)], 'kilitli'),
    ('circle', '<circle cx="8" cy="8" r="5.5"></circle>', [(14, 1.6)], 'başlatılabilir proje'),
    ('warning', '<path d="M8 2.2l6.3 11.3H1.7z"></path><path d="M8 6.6v3.2"></path><path d="M8 12.1v.1"></path>', [(12, 1.6), (14, 1.6)], 'uyarı üçgeni'),
    ('antenna', '<path d="M8 8.6V14"></path><circle cx="8" cy="7" r="1.4"></circle><path d="M5.2 4.4a4 4 0 0 0 0 5.2"></path><path d="M10.8 4.4a4 4 0 0 1 0 5.2"></path><path d="M3.2 2.6a6.6 6.6 0 0 0 0 8.8"></path><path d="M12.8 2.6a6.6 6.6 0 0 1 0 8.8"></path>', [(16, 1.6)], 'İzleme ağı eylemi'),
    ('lens', '<circle cx="7" cy="7" r="4.3"></circle><path d="M10.2 10.2L14 14"></path>', [(16, 1.6)], 'Denetim eylemi'),
    ('target', '<circle cx="8" cy="8" r="6" stroke-dasharray="2.6 1.8"></circle><circle cx="8" cy="8" r="2.2"></circle>', [(16, 1.6)], 'Karantina eylemi'),
    ('power', '<path d="M8 1.8v5.4"></path><path d="M4.6 4.2a5.2 5.2 0 1 0 6.8 0"></path>', [(16, 1.6), (13, 1.6)], 'Son Şalter / SONUÇ'),
    ('arrow_right', '<path d="M2.5 8h11M9.5 4.5L13 8l-3.5 3.5"></path>', [(14, 1.6)], 'önizleme oku'),
    ('gear', '<circle cx="8" cy="8" r="2.3"></circle><path d="M8 1.8v2M8 12.2v2M1.8 8h2M12.2 8h2M3.6 3.6l1.4 1.4M11 11l1.4 1.4M3.6 12.4L5 11M11 5l1.4-1.4"></path>', [(14, 2)], 'ayarlar'),
    ('arrow_down', '<path d="M8 3v9.5M4.5 9L8 12.5 11.5 9"></path>', [(12, 2)], 'değişim: azalıyor'),
    ('arrow_up', '<path d="M8 13V3.5M4.5 7L8 3.5 11.5 7"></path>', [(12, 2)], 'değişim: artıyor'),
    ('flat', '<path d="M3.5 8h9"></path>', [(12, 2)], 'değişim yok (İllüzyon)'),
    ('minus_circle', '<circle cx="8" cy="8" r="6"></circle><path d="M5 8h6"></path>', [(12, 1.6)], 'BEDEL başlığı (C3)'),
    ('plus_circle', '<circle cx="8" cy="8" r="6"></circle><path d="M5 8h6M8 5v6"></path>', [(12, 1.6)], 'KAZANÇ başlığı (C3)'),
    ('hourglass', '<path d="M4.5 2.5h7M4.5 13.5h7"></path><path d="M5.3 2.5c0 3.6 5.4 3.4 5.4 5.5s-5.4 1.9-5.4 5.5M10.7 2.5c0 3.6-5.4 3.4-5.4 5.5s5.4 1.9 5.4 5.5"></path>', [(16, 1.6)], 'Kışı geçir seçeneği (C3)'),
    ('ai_move', '<path d="M3.5 3.5h9v9h-9z"></path><path d="M6.4 6.2a2.3 2.3 0 1 0 0 3.6"></path>', [(13, 1.6)], 'YZ HAMLESİ (C4)'),
    ('check_circle', '<circle cx="8" cy="8" r="5.5"></circle><path d="M5.6 8.1l1.7 1.7 3.2-3.5"></path>', [(13, 1.6), (14, 1.6)], 'ÖLÇÜM (C4), İYİ SONUÇ çipi'),
    ('diamond', '<path d="M8 2.5L13.5 8 8 13.5 2.5 8z"></path>', [(13, 1.6)], 'KARAR (C4)'),
    ('octagon', '<path d="M5.6 1.8h4.8l3.4 3.4v4.8l-3.4 3.4H5.6l-3.4-3.4V5.2z"></path><path d="M8 4.8v3.6M8 10.9v.1"></path>', [(14, 1.6)], 'TEHDİT çipi'),
    ('info', '<circle cx="8" cy="8" r="6"></circle><path d="M8 7.2v4M8 4.9v.1"></path>', [(14, 1.6)], 'BİLGİ çipi'),
]
for name, inner, sizes, use in ICONS:
    for sz, sw in sizes:
        suffix = '' if sw == 1.6 else 'b'
        nm = f'ic_{name}_{sz}{suffix}'
        body = svg(sz, sz, inner, vb='0 0 16 16', extra=f' fill="none" stroke="{W}" stroke-width="{sw}" stroke-linecap="round" stroke-linejoin="round"')
        add(nm, sz, sz, body, tint=True, group=G, use=f'{use} · {sz} px, çizgi {sw}' + (' (kalın)' if suffix else ''))

# ------------------------------------------------------------------ 4 · İŞARETLER (güven katmanı ve damgalar)
G = 'marks'
def stamp_frame(n, ow, gap, iw, r_out, r_in, b):
    o = rr(0, 0, n, n, r_out, r_out, r_out, r_out) + rr(ow, ow, n - 2 * ow, n - 2 * ow, max(r_out - ow, 0), max(r_out - ow, 0), max(r_out - ow, 0), max(r_out - ow, 0))
    k = ow + gap
    i = rr(k, k, n - 2 * k, n - 2 * k, r_in, r_in, r_in, r_in) + rr(k + iw, k + iw, n - 2 * (k + iw), n - 2 * (k + iw), 0, 0, 0, 0)
    return svg(n, n, f'<path d="{o}" fill="{W}" fill-rule="evenodd"/><path d="{i}" fill="{W}" fill-rule="evenodd"/>')
add('stamp_frame', 14, 14, stamp_frame(14, 2, 2, 1, 3, 1, 6), mode='sliced', border=(6, 6, 6, 6), tint=True, group=G, use='damga çift çerçeve (normal, 12–13 px yazı): dış 2 px, 2 px boşluk, iç 1 px · renk = damga mürekkebi, alfa .92')
add('stamp_frame_sm', 12, 12, stamp_frame(12, 1.5, 1.5, 1, 3, 1, 5), mode='sliced', border=(5, 5, 5, 5), tint=True, group=G, use='küçük damga (11 px yazı): dış 1,5, iç 1')
add('stamp_frame_lg', 18, 18, stamp_frame(18, 3, 3, 1.5, 4, 2, 8), mode='sliced', border=(8, 8, 8, 8), tint=True, group=G, use='büyük damga (C4 KAYBEDİLDİ, 30 px): dış 3, iç 1,5')
SEAL_CHECK = '<path d="M2 5.2l2 2 4-4.4" fill="none" stroke="#FFFFFF" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round"/>'
for d in (16, 14):
    add(f'mark_seal_{d}', d, d, svg(d, d, f'<circle cx="{d / 2}" cy="{d / 2}" r="{d / 2}" fill="#1A4789"/><g transform="translate({(d - 10) / 2} {(d - 10) / 2})">{SEAL_CHECK}</g>'), group=G, use=f'ONAYLANDI mührü {d} px (mavi disk + beyaz tik) — renkli')
add('mark_suspicious', 20, 18, svg(20, 18, '<path d="M4.6 15C1.7 13.2 1.2 7.6 4.3 4.6 7.7 1.2 14.4 1.3 17.2 4.4c2.8 3.1 1.9 8.5-1.6 10.2-3 1.4-7.4 1.4-10.2-.2" fill="none" stroke="#A9520A" stroke-width="1.7" stroke-linecap="round"/><text x="10" y="13.6" text-anchor="middle" font-family="Kalam" font-weight="700" font-size="12" fill="#854005">?</text>'),
    group=G, use='ŞÜPHELİ işareti: turuncu kalem halkası + Kalam “?” (yanına Kalam 15 “şüpheli”) — renkli')
for d, fs in ((17, 13), (15, 12), (26, 18)):
    html = (f'<div class="c" style="background:transparent"><span class="unk" style="display:flex"><i style="width:{d}px;height:{d}px;font-size:{fs}px">?</i></span></div>')
    add(f'mark_unknown_{d}', d, d, html, group=G, use=f'BİLİNMİYOR işareti {d} px: kesik kurşun kalem dairesi + Kalam “?” — renkli')
add('mark_hand_ring_value', 124, 56, svg(124, 56, '<path d="M26.1 10.9L23 12.7L19.2 14.3L14.9 15.6L10.1 16.6L5.2 17.2L0.2 17.5L-4.8 17.6L-9.7 17.5L-14.4 17.2L-18.9 16.6L-23.1 15.7L-26.8 14.5L-29.9 13L-32.3 11.4L-34.1 9.6L-35.7 7.8L-37.1 6L-38.5 4.2L-39.7 2.3L-40.7 0.2L-41 -2L-40.3 -4.2L-38.4 -6.4L-35.6 -8.5L-31.8 -10.3L-27.5 -11.7L-23 -13L-18.5 -14.1L-13.9 -15L-9.3 -15.9L-4.6 -16.6L0.2 -17.1L5 -17.4L9.9 -17.3L14.6 -17L19 -16.4L23.3 -15.6L27.4 -14.6L31.4 -13.5L35.2 -12.2L38.5 -10.7L41 -8.8L42.6 -6.8L43 -4.5L42.3 -2.3L40.8 -0.1L38.8 2L36.5 4L34.3 5.9L32.1 7.8L29.7 9.8L26.9 11.7L23.5 13.5L19.5 15.2L14.9 16.5L9.9 17.4" fill="none" stroke="#FFFFFF" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>', vb='-56 -23 112 46', extra=' preserveAspectRatio="none"'),
    tint=True, group=G, use='şüpheli büyük değerin etrafındaki elle çizilmiş halka (dosya “%38”), turuncu #A9520A; değerin sol-üstüne göre (−28, −9)')
add('mark_hand_ring_gauge', 96, 40, svg(96, 40, '<path d="M26.1 10.9L23 12.7L19.2 14.3L14.9 15.6L10.1 16.6L5.2 17.2L0.2 17.5L-4.8 17.6L-9.7 17.5L-14.4 17.2L-18.9 16.6L-23.1 15.7L-26.8 14.5L-29.9 13L-32.3 11.4L-34.1 9.6L-35.7 7.8L-37.1 6L-38.5 4.2L-39.7 2.3L-40.7 0.2L-41 -2L-40.3 -4.2L-38.4 -6.4L-35.6 -8.5L-31.8 -10.3L-27.5 -11.7L-23 -13L-18.5 -14.1L-13.9 -15L-9.3 -15.9L-4.6 -16.6L0.2 -17.1L5 -17.4L9.9 -17.3L14.6 -17L19 -16.4L23.3 -15.6L27.4 -14.6L31.4 -13.5L35.2 -12.2L38.5 -10.7L41 -8.8L42.6 -6.8L43 -4.5L42.3 -2.3L40.8 -0.1L38.8 2L36.5 4L34.3 5.9L32.1 7.8L29.7 9.8L26.9 11.7L23.5 13.5L19.5 15.2L14.9 16.5L9.9 17.4" fill="none" stroke="#FFFFFF" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/>', vb='-48 -20 96 40'),
    tint=True, group=G, use='şüpheli sayaç değerinin halkası (“58,4”), turuncu; değerin sol-üstüne göre (−15, −9)')
add('mark_strike_x', 84, 34, svg(84, 34, '<path d="M4 30L80 5M6 6L79 29" stroke="#FFFFFF" stroke-width="2.6" stroke-linecap="round"/>'), tint=True, group=G, use='üstü çizilmiş damga (C4 “TEMİZ”), kırmızı #B8241A')

# ------------------------------------------------------------------ 5 · ANALOG SAYAÇ
G = 'meter'
TICKS = 'M26.2 55L17.5 50M24.7 48L20.7 45.1M28.1 43.9L24.4 40.5M31.9 40.1L28.5 36.4M36 36.7L33.1 32.7M43 38.2L38 29.5M45.3 31.4L43.2 26.8M50.2 29.5L48.7 24.7M55.4 28.1L54.4 23.2M60.7 27.3L60.1 22.3M66 32L66 22M71.3 27.3L71.9 22.3M76.6 28.1L77.6 23.2M81.8 29.5L83.3 24.7M86.7 31.4L88.8 26.8M89 38.2L94 29.5M96 36.7L98.9 32.7M100.1 40.1L103.5 36.4M103.9 43.9L107.6 40.5M107.3 48L111.3 45.1M105.8 55L114.5 50'
def dial(face):
    return svg(132, 92, f'<rect x=".5" y=".5" width="131" height="91" rx="8" fill="#1A1511" stroke="#000"/>'
                        f'<rect x="5" y="5" width="122" height="82" rx="4" fill="{face}"/><path d="{TICKS}" stroke="#1E1B16" stroke-width="1.2" stroke-linecap="round"/>'
                        '<text x="34" y="63.5" text-anchor="middle" font-family="Courier Prime" font-size="11" font-weight="700" fill="#463F34">0</text>'
                        '<text x="66" y="45" text-anchor="middle" font-family="Courier Prime" font-size="11" font-weight="700" fill="#463F34">50</text>'
                        '<text x="98" y="63.5" text-anchor="middle" font-family="Courier Prime" font-size="11" font-weight="700" fill="#463F34">100</text>'
                        '<rect x="5.5" y="5.5" width="121" height="81" rx="4" fill="none" stroke="#000" stroke-opacity=".45"/>')
add('meter_dial', 132, 92, dial('#F3EDDC'), group=G, use='sayaç gövdesi (bakalit) + kadran + çentikler + 0/50/100 · ibre pivotu kadranın (66,78) noktası')
add('meter_dial_illusion', 132, 92, dial('#F5F0E1'), group=G, use='İllüzyon kadranı (bir ton açık)')
add('meter_needle', 4, 60, svg(4, 60, '<path d="M2 2V58" stroke="#FFFFFF" stroke-width="2" stroke-linecap="round"/>'), pivot=(0.5, 0.1), tint=True, group=G,
    use='ibre (mürekkep #1E1B16) · pivot (0.5, 0.1) = kadranda (66,78) · z = −(değer−50)×1,2°')
add('meter_hub', 12, 12, svg(12, 12, '<circle cx="6" cy="6" r="5" fill="#8A7346" stroke="#1E1B16" stroke-width="1.2"/>'), group=G, use='pirinç göbek, ibrenin üstünde, merkez (66,78)')
# tehlike bölgesi halkası: r 50..56, eğik çizgiler (teğete 45°), dönmeye karşı değişmez
N = 84; rin, rout = 50.0, 56.0; cx = cy = 56.0
polys = []
for k in range(N):
    a0 = 2 * math.pi * k / N; w_ = 2 * math.pi / N / 2; sl = math.radians(4.5)
    pts = [(rin, a0), (rout, a0 + sl), (rout, a0 + sl + w_), (rin, a0 + w_)]
    polys.append('<polygon points="' + ' '.join(f'{cx + r * math.sin(a):.2f},{cy - r * math.cos(a):.2f}' for r, a in pts) + '" fill="#9A1E14"/>')
hz = (f'<circle cx="56" cy="56" r="53" fill="none" stroke="#F3EDDC" stroke-width="6"/>'
      f'<mask id="hm"><circle cx="56" cy="56" r="56" fill="#fff"/><circle cx="56" cy="56" r="50" fill="#000"/></mask><g mask="url(#hm)">{"".join(polys)}</g>')
add('meter_zone_hazard', 112, 112, svg(112, 112, hz), group=G, use='eşik ötesi tehlike bölgesi: tam halka, Image Filled · Radial360 · merkez = pivot (66,78) · fillAmount = aralık×1,2/360, z-dönüşü başlangıç açısını verir')

# ------------------------------------------------------------------ 6 · HARİTA
G = 'map'
add('node_disc', 36, 36, svg(36, 36, f'<circle cx="18" cy="18" r="16" fill="{W}"/>'), tint=True, group=G, use='bölge düğümü diski r16 · renk = sızma rampası · merkez = düğüm merkezi')
add('node_ring', 36, 36, svg(36, 36, f'<circle cx="18" cy="18" r="16" fill="none" stroke="{W}" stroke-width="1.5"/>'), tint=True, group=G, use='düğüm mürekkep halkası 1,5 (#1E1B16)')
add('node_ring_unknown', 36, 36, svg(36, 36, f'<circle cx="18" cy="18" r="15" fill="none" stroke="{W}" stroke-width="1.5" stroke-dasharray="3 3"/>'), tint=True, group=G, use='BİLİNMİYOR düğümü: kesik kurşun kalem dairesi r15 (#4A4640); dolgu node_disc #ECE6D4 %70, ölçek 30/32')
add('node_badge_verified', 18, 18, svg(18, 18, '<circle cx="9" cy="9" r="7" fill="#1A4789" stroke="#ECE6D4" stroke-width="1.5"/><path d="M5.8 9.2l2.2 2.2 4-4.4" fill="none" stroke="#FFFFFF" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round"/>'),
    group=G, use='düğüm sağ-üst ONAYLANDI mührü · düğüm merkezine göre (+15, −15) [Unity: (+15, +15)]')
add('node_badge_report', 19, 14, svg(19, 14, '<rect x="1" y="1" width="17" height="12" rx="2" fill="#E3E0D8" stroke="#524973" stroke-width="1" stroke-dasharray="2 1.5"/><path d="M4.6 7.6c1-1.4 2.2-1.4 3.3 0s2.2 1.4 3.3 0" fill="none" stroke="#524973" stroke-width="1.5" stroke-linecap="round"/>'),
    group=G, use='düğüm sağ-üst RAPOR fişi “~” · merkezine göre (+16.5, −17)')
for nm, d, sw, use in [('node_ring_suspicious_a', 'M15.1 46.8L13.1 45L11.5 42.9L10.1 40.8L9 38.6L7.9 36.5L6.9 34.3L6 32.1L5.4 29.7L5.2 27.2L5.4 24.7L6 22.2L7.1 19.8L8.4 17.5L9.9 15.3L11.7 13.2L13.7 11.3L16 9.5L18.6 8.2L21.4 7.3L24.4 7L27.2 7.2L30 7.7L32.6 8.4L35.1 9.1L37.6 9.9L40.1 10.7L42.6 11.6L45.2 12.7L47.5 14.3L49.4 16.2L50.8 18.5L51.7 21.1L52.1 23.7L52 26.4L51.7 29L51.1 31.5L50.3 34L49.2 36.4L47.7 38.7L45.9 40.8L43.8 42.6L41.5 44.1L39.1 45.4L36.6 46.6L34.1 47.6L31.4 48.7L28.6 49.6L25.6 50.2L22.6 50.4L19.5 50L16.7 49L14.2 47.5L12.2 45.5L10.5 43.3L9.1 41.1L7.9 38.8', 2.2, 'ŞÜPHELİ düğüm kalem halkası (varyant A, O.Doğu) turuncu #A9520A'),
                       ('node_ring_suspicious_b', 'M51.5 21.8L52.4 24.3L52.6 26.9L52.2 29.4L51.3 31.8L50.2 34L48.9 36.1L47.6 38.1L46.2 40L44.7 41.9L43 43.7L40.9 45.3L38.6 46.6L36 47.5L33.2 48.1L30.4 48.5L27.5 48.6L24.5 48.4L21.6 48L18.7 47.3L15.9 46.2L13.4 44.6L11.3 42.6L9.7 40.4L8.5 38.1L7.6 35.7L6.9 33.4L6.1 31.2L5.4 28.9L4.7 26.5L4.2 24L4.2 21.4L4.7 18.8L5.9 16.4L7.6 14.3L9.7 12.6L12.1 11.1L14.7 9.9L17.4 8.9L20.1 8.1L22.9 7.6L25.8 7.3L28.7 7.5L31.6 8L34.3 8.8L37 9.9L39.5 11.1L41.9 12.4L44.4 13.8L46.8 15.4L49.2 17.2L51.2 19.4L52.7 21.8L53.6 24.5L53.7 27.2L53.2 29.8L52.3 32.3', 2.2, 'ŞÜPHELİ düğüm kalem halkası (varyant B, D.Asya)')]:
    add(nm, 56, 56, svg(56, 56, f'<path d="{d}" fill="none" stroke="{W}" stroke-width="{sw}" stroke-linecap="round" stroke-linejoin="round"/>'), tint=True, group=G, use=use + ' · 56×56 düğüm kutusu, merkez (28,28)')
add('node_ring_quarantine', 56, 56, svg(56, 56, f'<path d="M14 6.8L16.9 5.8L19.9 5.3L22.8 5.1L25.7 5.1L28.6 5.2L31.4 5.4L34.2 5.9L37 6.6L39.7 7.6L42.2 9L44.4 10.7L46.5 12.6L48.3 14.7L50.1 16.9L51.6 19.3L53 21.9L54.1 24.6L54.8 27.5L54.8 30.4L54.2 33.3L53 36L51.3 38.4L49.2 40.5L47.1 42.4L44.9 44.1L42.8 45.8L40.6 47.5L38.4 49.1L35.9 50.7L33.2 51.9L30.2 52.7L27.1 53L24 52.7L21 52L18.2 50.9L15.4 49.6L12.8 48L10.4 46.2L8.2 44.2L6.4 41.9L4.9 39.3L4 36.6L3.5 33.8L3.3 31L3.4 28.3L3.6 25.6L3.8 22.9L4.1 20.1L4.6 17.3L5.4 14.4L6.7 11.7L8.6 9.2L11 7.2L13.8 5.6L16.8 4.7L19.9 4.1" fill="none" stroke="{W}" stroke-width="2" stroke-dasharray="5 3.5" stroke-linecap="round"/>'),
    tint=True, group=G, use='KARANTİNA: elle çizilmiş yeşil kesik halka (#24592E) · 56×56')
add('node_ring_quarantine_clean', 56, 56, svg(56, 56, f'<circle cx="28" cy="28" r="24" fill="none" stroke="{W}" stroke-width="2" stroke-dasharray="5 3.5"/>'), tint=True, group=G, use='İllüzyonda karantina halkası: kusursuz daire r24')
add('node_core_badge', 21, 21, svg(21, 21, '<rect x="2" y="2" width="17" height="17" rx="2" fill="#F1E9D6" fill-opacity=".9" stroke="#9A1E14" stroke-width="2"/>'),
    group=G, use='YZ ÇEKİRDEĞİ damgası (harf TMP: Franklin 800 12 px #9A1E14 “Ç”/EN “C”) · düğüm merkezine göre (−15, −15), z = +8°')
add('map_pin', 14, 14, svg(14, 14, '<g transform="translate(6 6)"><circle cx="1.8" cy="2.2" r="5.5" fill="#000" fill-opacity=".3"/><circle r="5.5" fill="#2A2420"/><circle cx="-1.8" cy="-1.8" r="1.8" fill="#FFFFFF" fill-opacity=".55"/></g>'),
    pivot=(6 / 14, 1 - 6 / 14), group=G, use='analist raptiyesi (gölgesi dahil) · pivot = iğne merkezi')
add('map_pinhole', 6, 6, svg(6, 6, '<circle cx="3" cy="3" r="1.8" fill="#2A2420" fill-opacity=".72" stroke="#000" stroke-opacity=".16" stroke-width="2.4"/>'), group=G, use='İllüzyon: raptiyenin iğne deliği')
add('map_flag', 10, 9, svg(10, 9, f'<path d="M0 0L10 4.5L0 9Z" fill="{W}"/>'), tint=True, group=G, use='yayılım ipliği ok bayrağı (#B8241A), iplik ortasında, yönü +x')
add('map_route_arrow', 7, 8, svg(7, 8, f'<path d="M0 .2L7 4L0 7.8Z" fill="{W}"/>'), pivot=(1.0, 0.5), tint=True, group=G, use='basılı yolun harita kenarı oku (#6F6553) · pivot uçta')
add('map_chevron', 9, 10, svg(9, 10, f'<path d="M1 1L8 5L1 9" fill="none" stroke="{W}" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round"/>'), pivot=(8 / 9, 0.5), tint=True, group=G, use='kalem oku ucu (#4A4640 / #9A1E14) · pivot uçta')
add('map_cut_mark', 12, 16, svg(12, 16, f'<path d="M3 2V14M9 2V14" stroke="{W}" stroke-width="2.4" stroke-linecap="round"/>'), tint=True, group=G, use='kesik yol işareti (iki çentik, #1E1B16) · yola dik döndür')
add('map_scale_bar', 190, 14, svg(190, 14, '<rect x=".5" y="2.5" width="62" height="6" fill="#1E1B16" stroke="#1E1B16"/><rect x="62.5" y="2.5" width="62" height="6" fill="#ECE6D4" stroke="#1E1B16"/><rect x="124.5" y="2.5" width="62" height="6" fill="#1E1B16" stroke="#1E1B16"/>'), group=G, use='ölçek çubuğu')
add('map_token', 28, 28, '<div class="c" style="background:transparent;position:relative;width:28px;height:28px"><span class="tok" style="left:3px;top:3px"><span></span></span></div>',
    group=G, use='saha ekibi jetonu (siyah elmas, krem kenar, gölge) · harf TMP Courier 700 11 #F1E9D6 · tuval 28, merkez ortada')
for nm, w, h, inner, use in [
    ('legend_report', 18, 13, '<rect x=".5" y=".5" width="17" height="12" rx="2" fill="#E3E0D8" stroke="#524973" stroke-dasharray="2 1.5"/><path d="M4.1 7.1c1-1.4 2.2-1.4 3.3 0s2.2 1.4 3.3 0" fill="none" stroke="#524973" stroke-width="1.5" stroke-linecap="round"/>', 'lejant: rapor'),
    ('legend_suspicious', 22, 16, '<path d="M5 13.2C2 11.5 1.4 6.8 4.6 4 8.1 1 15.3 1 18.4 3.8c3 2.8 2.3 7.6-1.2 9.2-3.3 1.5-8.4 1.4-11.1-.4" fill="none" stroke="#A9520A" stroke-width="1.8" stroke-linecap="round"/>', 'lejant: şüpheli'),
    ('legend_unknown', 16, 16, '<circle cx="8" cy="8" r="6.5" fill="none" stroke="#4A4640" stroke-width="1.4" stroke-dasharray="2.5 2.5"/>', 'lejant: bilinmiyor'),
    ('legend_quarantine', 18, 18, '<circle cx="9" cy="9" r="7.5" fill="none" stroke="#24592E" stroke-width="1.8" stroke-dasharray="4 2.5"/>', 'lejant: karantina'),
    ('legend_core', 15, 15, '<rect x="1" y="1" width="13" height="13" rx="2" fill="none" stroke="#9A1E14" stroke-width="1.8"/>', 'lejant: YZ çekirdeği (harf TMP Franklin 800 9,5)'),
    ('legend_token', 14, 14, '<path d="M7 1l6 6-6 6-6-6z" fill="#1E1B16"/>', 'lejant: saha ekibi'),
    ('legend_spread', 26, 10, '<path d="M1 5h24" stroke="#B8241A" stroke-width="2.2"/><path d="M13 1.6l5 3.4-5 3.4z" fill="#B8241A"/>', 'lejant: yayılım ipliği'),
    ('legend_route', 26, 10, '<path d="M1 5h24" stroke="#6F6553" stroke-width="1.3" stroke-dasharray="6 4"/>', 'lejant: ağ yolu'),
    ('legend_route_cut', 26, 14, '<path d="M1 7h24" stroke="#6F6553" stroke-width="1.3" stroke-dasharray="6 4"/><path d="M10 2.5l1.6 9M15 2.5l1.6 9" stroke="#1E1B16" stroke-width="2.2" stroke-linecap="round"/>', 'lejant: kesik yol'),
    ('legend_pin', 12, 12, '<circle cx="6" cy="6" r="5" fill="#2A2420"/><circle cx="4.4" cy="4.4" r="1.6" fill="#FFFFFF" fill-opacity=".55"/>', 'lejant: analist raptiyesi')]:
    add(nm, w, h, svg(w, h, inner), group=G, use=use)

# ------------------------------------------------------------------ 7 · MASA NESNELERİ
G = 'deco'
add('deco_tape', 46, 15, '<div class="c" style="background:transparent;position:relative;width:46px;height:15px"><span class="tape" style="left:0;top:0"></span></div>', group=G, use='bant parçası %78 · z −12°…+10°')
add('deco_paperclip', 16, 40, svg(16, 40, '<path d="M11.5 14V31.5a3.5 3.5 0 0 1-7 0V8a5 5 0 0 1 10 0v21" fill="none" stroke="#8E8B84" stroke-width="2" stroke-linecap="round"/>'), group=G, use='ataş (klasör, bildirim, rapor)')
add('deco_sticky', 60, 60, '<div class="c" style="background:transparent;position:relative;width:60px;height:60px"><div class="sticky" style="left:0;top:0;width:60px;height:60px;padding:0;box-shadow:none"></div></div>',
    mode='sliced', border=(6, 6, 16, 16), group=G, use='sarı not kâğıdı, sağ-alt kıvrım dahil · gölge yok (tasarımda kesim gölgeyi de keser)')
add('deco_lamp_on', 13, 13, '<div class="c" style="background:transparent;position:relative;width:13px;height:13px"><span class="lamp on" style="position:absolute;left:2px;top:2px"></span></div>', group=G, use='CANLI lambası (yeşil + %22 ışıma) · 9 px lamba, tuval 13')
add('deco_lamp_off', 9, 9, '<div class="c" style="background:transparent;position:relative;width:9px;height:9px"><span class="lamp" style="position:absolute;left:0;top:0"></span></div>', group=G, use='sönük lamba')

# ------------------------------------------------------------------ 8 · KÜRESEL SON ŞALTER ZARFI
G = 'envelope'
WAX = 'M26 2.5c3.2 0 4.3 2.4 7.1 3.4s5.3-.4 7.4 1.8 1.1 4.6 2.2 7.4 3.8 3.8 3.8 7-2.4 4.3-3.4 7.1.4 5.3-1.8 7.4-4.6 1.1-7.4 2.2-3.8 3.8-7 3.8-4.3-2.4-7.1-3.4-5.3.4-7.4-1.8-1.1-4.6-2.2-7.4-3.8-3.8-3.8-7 2.4-4.3 3.4-7.1-.4-5.3 1.8-7.4 4.6-1.1 7.4-2.2S22.8 2.5 26 2.5z'
def seal(f, s1, s2):
    return svg(52, 52, f'<path d="{WAX}" fill="{f}" stroke="{s1}" stroke-width="1"/><circle cx="26" cy="26" r="15" fill="none" stroke="{s1}" stroke-width="1.5"/><path d="M26 17v8" stroke="{s2}" stroke-width="2.6" stroke-linecap="round"/><path d="M21 20.2a8 8 0 1 0 10 0" fill="none" stroke="{s2}" stroke-width="2.6" stroke-linecap="round"/>')
add('env_seal', 52, 52, seal('#6E120C', '#4A0B07', '#3E0905'), group=G, use='mum mühür (normal) · zarfın sağından 16, üstünden 11')
add('env_seal_dim', 52, 52, seal('#4F1C18', '#3A120F', '#2E0D0A'), group=G, use='mum mühür: devre dışı / İllüzyon')
add('env_seal_crack', 52, 52, svg(52, 52, '<path d="M14 18l7 5-3 6 6 4M34 12l-4 8 5 4M38 34l-6-3-2 7" fill="none" stroke="#F1C9B8" stroke-width="1.2" stroke-linecap="round" stroke-linejoin="round"/>'), group=G, use='tutarken mumda çatlak (ilerleme ≥ %50’de görünür)')
add('env_progress_ring', 48, 48, svg(48, 48, f'<circle cx="24" cy="24" r="21" fill="none" stroke="{W}" stroke-width="3"/>'), tint=True, group=G,
    use='tutma ilerlemesi: iz = krem %35, dolgu = #F5DC63 · dolgu Image Filled Radial360, origin Top, saat yönü · mühür merkezine ortalanır')
add('env_flap', 298, 72, svg(298, 72, '<path d="M0 0L149 32L298 0" fill="#000" fill-opacity=".1" stroke="#6E140E" stroke-width="1.5"/>'), group=G, use='zarf kapağı (normal) · zarfın içinde (0,0)')
add('env_flap_dim', 298, 72, svg(298, 72, '<path d="M0 0L149 32L298 0" fill="#000" fill-opacity=".1" stroke="#4A1A16" stroke-width="1.5"/>'), group=G, use='zarf kapağı: İllüzyon / devre dışı')

# ------------------------------------------------------------------ 9 · MODAL (C3) ve RAPOR (C4)
G = 'modal'
# spot: 148 px delik, 74..86 halka (%86), dışı %78 — çevresini 4 Image (rgba 14,9,5,.78) tamamlar
spot = ('<mask id="sm"><rect width="180" height="180" fill="#fff"/><circle cx="90" cy="90" r="74" fill="#000"/></mask>'
        '<g mask="url(#sm)"><rect width="180" height="180" fill="rgb(14,9,5)" fill-opacity=".78"/><circle cx="90" cy="90" r="86" fill="rgb(14,9,5)" fill-opacity=".38"/></g>')
add('modal_spot_148', 180, 180, svg(180, 180, spot), group=G, use='olay bildirimi spotu: 148 px delik + 12 px koyu halka; dışarısı 4 düz Image rgba(14,9,5,.78) · merkez = olay bölgesi')
pts = []
for k in range(0, 49):
    t = math.radians(-118 + k * (392 / 48))
    r = 50 * (1 + 0.035 * math.sin(3 * t + .7) + 0.02 * math.sin(5 * t)) * (1 + 0.05 * (k / 48))
    pts.append((62 + r * math.cos(t) * 1.06, 58 + r * math.sin(t) * 0.94))
ringd = 'M' + ' L'.join(f'{x:.1f} {y:.1f}' for x, y in pts)
add('modal_marker_ring', 124, 116, svg(124, 116, f'<path d="{ringd}" fill="none" stroke="{W}" stroke-width="2.6" stroke-linecap="round" stroke-linejoin="round"/>'), tint=True, group=G,
    use='olay bölgesinin etrafında kırmızı keçeli kalem halkası (#B8241A %92) · merkez (62,58) = bölge düğümü merkezi')
add('chart_seal', 18, 18, svg(18, 18, '<circle cx="9" cy="9" r="7.5" fill="#1A4789" stroke="#F3F1E4" stroke-width="1.5"/><path d="M5.6 9.2l2.2 2.2 4.2-4.6" fill="none" stroke="#FFFFFF" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round"/>'),
    group=G, use='şerit grafikte doğrulanmış ölçüm noktası (C4)')
add('chart_marker', 22, 22, svg(22, 22, f'<circle cx="11" cy="11" r="9.5" fill="{W}"/>'), tint=True, group=G, use='şerit grafik kırılma numarası zemini (#1E1B16; 5. olay #9A1E14) + kenar sh_ring_o20_5_1_5 (#F3F1E4) + TMP rakam')

# ------------------------------------------------------------------ 10 · TEKRAR EDEN DOKULAR (RawImage + uvRect; atlas dışı, Wrap = Repeat)
G = 'textures'
def tex(name, w, h, css, use):
    add(name, w, h, f'<div style="width:{w}px;height:{h}px;{css}"></div>', kind='texture', tint=False, group=G, use=use, wrap='repeat')
tex('tx_felt', 20, 20, 'background-color:#1C372C;background-image:radial-gradient(rgba(255,255,255,.035) 1px,transparent 1.3px);background-size:5px 5px', 'yeşil keçe (masa, C4/C5 zemini)')
tex('tx_wood_grain', 8, 114, 'background-color:#2A1C13;background-image:repeating-linear-gradient(0deg,rgba(255,230,190,.025) 0 1px,transparent 1px 6px),repeating-linear-gradient(0deg,rgba(0,0,0,.14) 0 2px,transparent 2px 19px)', 'ahşap masa kenarı (ray ile keçe arası, alt şerit) · y periyodu 114')
TEL = 'background-image:radial-gradient(circle,rgba(70,60,40,.32) 1px,transparent 1.5px),radial-gradient(circle,rgba(70,60,40,.32) 1px,transparent 1.5px);background-size:9px 26px,9px 26px;background-position:0 -10px,4px 10px'
tex('tx_telex', 18, 26, 'background-color:#EFE4BC;' + TEL, 'teleks şeridi delik deseni (yükseklik 26)')
tex('tx_telex_illusion', 18, 26, 'background-color:#F1E8C4;' + TEL, 'teleks şeridi, İllüzyon tonu')
tex('tx_index_lines', 8, 22, 'background:linear-gradient(to bottom,transparent 21px,#BCD0E1 21px)', 'dizin kartı mavi çizgileri (şeffaf zemin) · ilk çizgi kart üstünden 56 px (kırmızı çizgi 33–35, sonra 22 px periyot)')
tex('tx_sprocket', 22, 14, 'background:#E6E2CF;position:relative', 'şerit grafik delik bandı (C4) · delik 7×6 r1.5 #2A1C13 %55, x=10, y=4')
tex('tx_hatch_red', 10, 10, '', 'C4 “yalan payı” kırmızı tarama (#9A1E14: zemin %7 + çizgi %42) · poligonda UV = konum/10 px')
tex('tx_hatch_ink', 5, 5, '', '“/” tarama (beyaz) · büyük alanlarda RawImage ile; küçük kutularda pt_hatch sprite')

# tx_sprocket deliği ve taramalar SVG ile
for s in SPRITES:
    if s['name'] == 'tx_sprocket':
        s['html'] = svg(22, 14, '<rect width="22" height="14" fill="#E6E2CF"/><rect x="10" y="4" width="7" height="6" rx="1.5" fill="#2A1C13" fill-opacity=".55"/>')
    if s['name'] == 'tx_hatch_red':
        lr = ''.join(f'<line x1="{c + 20}" y1="-20" x2="{c - 20}" y2="20" stroke="#9A1E14" stroke-opacity=".42" stroke-width="2.2"/>' for c in (-10, 0, 10, 20))
        s['html'] = svg(10, 10, f'<rect width="10" height="10" fill="#9A1E14" fill-opacity=".07"/>{lr}')
    if s['name'] == 'tx_hatch_ink':
        s['html'] = svg(5, 5, lines)


# ------------------------------------------------------------------ çizim
def page(items, cols_w=1600):
    x = y = 8; rowh = 0; html = []; placed = []
    for s in items:
        w, h = s['w'], s['h']
        if x + w + 8 > cols_w:
            x = 8; y += rowh + 12; rowh = 0
        html.append(f'<div style="position:absolute;left:{x}px;top:{y}px;width:{w}px;height:{h}px;overflow:visible">{s["html"]}</div>')
        placed.append((s, x, y))
        x += w + 12; rowh = max(rowh, h)
    return html, placed, y + rowh + 8


async def render():
    from playwright.async_api import async_playwright
    css = open(REF_HTML, encoding='utf-8').read()
    style = re.search(r'<style>(.*?)</style>', css, re.S).group(1)
    for d in (OUT_S, OUT_T, OUT_M):
        os.makedirs(d, exist_ok=True)
    html, placed, H = page(SPRITES)
    doc = (f'<!doctype html><html><head><meta charset="utf-8"><link rel="stylesheet" href="file://{ROOT}/reference/html/fonts.css"><style>{style}\n'
           f'html,body{{margin:0;background:transparent}} .c{{position:relative;overflow:visible}}</style></head>'
           f'<body><div style="position:relative;width:1600px;height:{H}px">{"".join(html)}</div></body></html>')
    tmp = os.path.join(ROOT, 'tools', '_sprite_page.html')
    open(tmp, 'w', encoding='utf-8').write(doc)
    async with async_playwright() as p:
        b = await p.chromium.launch()
        ctx = await b.new_context(viewport={'width': 1600, 'height': max(H, 100)}, device_scale_factor=SCALE)
        pg = await ctx.new_page()
        await pg.goto('file://' + tmp)
        await pg.evaluate('document.fonts.ready'); await pg.wait_for_timeout(300)
        for s, x, y in placed:
            out = os.path.join(OUT_T if s['kind'] == 'texture' else OUT_S, s['name'] + '.png')
            await pg.screenshot(path=out, clip={'x': x, 'y': y, 'width': s['w'], 'height': s['h']}, omit_background=True)
        # harita
        mp = await ctx.new_page()
        await mp.set_viewport_size({'width': 1112, 'height': 788})
        await mp.goto('file://' + os.path.join(ROOT, 'reference', 'html', 'c-harita.svg'))
        await mp.wait_for_timeout(200)
        await mp.screenshot(path=os.path.join(OUT_M, 'c_harita@2x.png'), clip={'x': 0, 'y': 0, 'width': 1112, 'height': 788})
        await b.close()
    os.remove(tmp)


def dump_compact(obj, path):
    """üst düzey anahtarlar girintili; nesne listeleri satır başına bir nesne"""
    lines = ['{']
    keys = list(obj.keys())
    for i, k in enumerate(keys):
        v = obj[k]; comma = ',' if i < len(keys) - 1 else ''
        if isinstance(v, list) and v and isinstance(v[0], dict):
            lines.append(f' {json.dumps(k)}: [')
            lines += ['  ' + json.dumps(e, ensure_ascii=False) + (',' if j < len(v) - 1 else '') for j, e in enumerate(v)]
            lines.append(' ]' + comma)
        else:
            lines.append(f' {json.dumps(k)}: {json.dumps(v, ensure_ascii=False)}{comma}')
    lines.append('}')
    open(path, 'w', encoding='utf-8').write('\n'.join(lines) + '\n')


def manifest():
    sp, tx = [], []
    for s in SPRITES:
        b = s['border']
        e = {'file': s['name'] + '.png', 'group': s['group'], 'size1x': [s['w'], s['h']], 'sizePx': [s['w'] * SCALE, s['h'] * SCALE]}
        if s['kind'] == 'texture':
            e.update({'wrap': 'repeat', 'use': s['use']})
            tx.append(e); continue
        e['imageType'] = s['mode']
        if b:
            L, T, R, B = b
            e['border1x_LTRB'] = [L, T, R, B]
            e['spriteBorderPx_LBRT'] = [L * SCALE, B * SCALE, R * SCALE, T * SCALE]
        e['pivot'] = [round(s['pivot'][0], 4), round(s['pivot'][1], 4)]
        e['tint'] = s['tint']
        e['use'] = s['use']
        sp.append(e)
    head = {
        'about': 'C · Analist masası sprite kiti. Tüm PNG’ler @2x (1 tasarım pikseli = 2 doku pikseli).',
        'import': {'textureType': 'Sprite (2D and UI)', 'spriteMode': 'Single', 'pixelsPerUnit': 200, 'meshType': 'FullRect', 'filterMode': 'Bilinear',
                   'mipmaps': False, 'wrapMode': 'Clamp', 'compression': 'None (ya da High Quality); düz renklerde bant oluşmasın', 'alphaIsTransparency': True, 'sRGB': True},
        'notes': ['spriteBorderPx_LBRT doğrudan TextureImporter.spriteBorder = new Vector4(L, B, R, T) olarak yazılır (doku pikseli).',
                  'tint=true: beyaz çizilmiştir; rengi Image.color verir (DeskTheme). tint=false: renkli; Image.color beyaz kalır (alfa hariç).',
                  'imageType: simple | sliced | tiled. tiled sprite’larda kenarlar orta bölge boyunda döşenir (kesik çizgiler gerilmez).',
                  'pivot Unity düzeninde (x sağa, y yukarı, 0–1).',
                  'Canvas referencePixelsPerUnit = 100 ve PPU 200 ile SetNativeSize() 1x tasarım boyutunu verir.'],
        'sprites': sp}
    dump_compact(head, os.path.join(OUT_S, 'sprites.json'))
    dump_compact({'about': 'Tekrar eden dokular (@2x). Atlasa KOYMA. Import: Texture Type Default, Wrap Mode Repeat, Filter Bilinear, Mipmap kapalı. RawImage + uvRect = (ofsetX/w, ofsetY/h, alanGenişliği/w, alanYüksekliği/h); w,h = size1x.',
               'textures': tx}, os.path.join(OUT_T, 'textures.json'))
    print(len(sp), 'sprite,', len(tx), 'doku')


def contact_sheets():
    from PIL import Image, ImageDraw, ImageFont
    font = ImageFont.truetype(os.path.join(ROOT, 'fonts', 'CourierPrime-Bold.ttf'), 18)
    groups = {}
    for s in SPRITES:
        groups.setdefault('textures' if s['kind'] == 'texture' else s['group'], []).append(s)
    sheets = [('A_shapes_patterns', ['shapes', 'patterns']), ('B_icons_marks', ['icons', 'marks']), ('C_meter_map', ['meter', 'map']),
              ('D_deco_envelope_modal_textures', ['deco', 'envelope', 'modal', 'textures'])]
    for nm, gs in sheets:
        items = [s for g in gs for s in groups.get(g, [])]
        W_, pad = 2000, 16
        cells = []; x = pad; y = pad; rowh = 0
        for s in items:
            im = Image.open(os.path.join(OUT_T if s['kind'] == 'texture' else OUT_S, s['name'] + '.png')).convert('RGBA')
            if s['kind'] == 'texture':
                big = Image.new('RGBA', (im.width * 4, im.height * 2 if im.height < 100 else im.height))
                for i in range(4):
                    for j in range(big.height // im.height):
                        big.paste(im, (i * im.width, j * im.height))
                im = big
            cw = max(im.width, min(len(s['name']) * 11, 300)) + 8
            ch = im.height + 30
            if x + cw > W_ - pad:
                x = pad; y += rowh + 14; rowh = 0
            cells.append((s, im, x, y)); x += cw + 14; rowh = max(rowh, ch)
        H_ = y + rowh + pad
        sheet = Image.new('RGBA', (W_, H_), (255, 255, 255, 255))
        d = ImageDraw.Draw(sheet)
        for s, im, x, y in cells:
            bg = Image.new('RGBA', im.size)
            bd = ImageDraw.Draw(bg)
            for i in range(0, im.width, 8):
                for j in range(0, im.height, 8):
                    bd.rectangle([i, j, i + 7, j + 7], fill=(205, 205, 205, 255) if (i // 8 + j // 8) % 2 else (235, 235, 235, 255))
            if s['tint']:
                tinted = Image.new('RGBA', im.size, (30, 27, 22, 255)); tinted.putalpha(im.getchannel('A')); im = tinted
            bg.alpha_composite(im)
            sheet.paste(bg, (x, y + 24))
            d.text((x, y), s['name'], fill=(30, 27, 22), font=font)
        sheet.convert('RGB').save(os.path.join(OUT_S, f'_contact_sheet_{nm}.png'))
        print('sheet', nm, sheet.size)


if __name__ == '__main__':
    asyncio.run(render())
    manifest()
    contact_sheets()
