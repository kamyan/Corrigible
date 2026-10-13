# -*- coding: utf-8 -*-
"""
Unity ekran görüntüsünü referans PNG ile karşılaştırır.

Kullanım:
  python3 tools/compare.py <unity_capture.png> <reference.png> [--out captures/diff] [--regions C1|C2|C3|C4|grid]

Çıktılar (--out klasörüne):
  <ad>_edges.png   kenar bindirme: referans kenarları YEŞİL, Unity kenarları MOR; üst üste binen yerler BEYAZ.
                   Çift çizgi = konum/boyut kayması. En faydalı çıktı budur.
  <ad>_onion.png   %50 karışım (soğan kâğıdı)
  <ad>_diff.png    farklı pikseller kırmızı (eşik 48/255), altında soluk referans
  <ad>_side.png    yan yana (sol referans, sağ Unity)
Konsola bölge bazında fark yüzdesi yazar. Metin kenar yumuşatması (Chrome ↔ TMP) her zaman biraz fark verir;
hedef, kenar bindirmesinde çift çizgi kalmaması ve bölge farklarının birbirine yakın, düşük olmasıdır.
Gerekenler: pip install pillow
"""
import sys, os, argparse
from PIL import Image, ImageChops, ImageFilter, ImageOps

REGIONS = {
    'C1': {'ray': (0, 0, 1920, 48), 'sayaclar': (16, 56, 1888, 92), 'danisman': (16, 163, 1888, 32), 'hedef': (16, 204, 376, 112),
           'arastirma': (16, 328, 376, 664), 'harita': (404, 204, 1112, 788), 'dosya': (1528, 204, 376, 788), 'teleks': (16, 1004, 1572, 26),
           'sekmeler': (16, 1036, 700, 44), 'zarf': (1604, 1001, 300, 74)},
    'C3': {'bildirim': (236, 60, 712, 540), 'secenek1': (236, 609, 344, 390), 'secenek2': (600, 609, 344, 390), 'secenek3': (964, 609, 344, 390),
           'yz_gunlugu': (1290, 200, 560, 200), 'danisman': (1330, 630, 320, 190), 'alt_serit': (236, 1008, 1072, 30), 'spot': (1080, 280, 170, 170)},
    'C4': {'baslik': (72, 36, 900, 230), 'kpi': (968, 98, 844, 152), 'grafik': (108, 270, 1704, 410), 'zaman_cizelgesi': (60, 710, 1800, 250), 'alt_cubuk': (72, 1000, 1776, 44)},
}
REGIONS['C2'] = REGIONS['C1']


def edges(im):
    g = ImageOps.grayscale(im).filter(ImageFilter.FIND_EDGES)
    return g.point(lambda v: 255 if v > 40 else 0)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('capture'); ap.add_argument('reference')
    ap.add_argument('--out', default=None); ap.add_argument('--regions', default=None)
    a = ap.parse_args()
    ref = Image.open(a.reference).convert('RGB')
    cap = Image.open(a.capture).convert('RGB')
    if cap.size != ref.size:
        print(f'not: yakalama {cap.size} → {ref.size} boyutuna ölçeklendi (en iyisi Game görünümünü tam 1920×1080 yakalamak)')
        cap = cap.resize(ref.size, Image.LANCZOS)
    name = os.path.splitext(os.path.basename(a.capture))[0]
    out = a.out or os.path.join(os.path.dirname(os.path.abspath(a.capture)), 'diff')
    os.makedirs(out, exist_ok=True)
    # kenar bindirme
    er, ec = edges(ref), edges(cap)
    z = Image.new('L', ref.size, 0)
    both = ImageChops.multiply(er, ec)
    r = ImageChops.lighter(ec, both); g = ImageChops.lighter(er, both); b = ImageChops.lighter(ec, both)
    edge_img = Image.merge('RGB', (r, g, b))
    edge_img = Image.blend(ImageOps.grayscale(ref).convert('RGB').point(lambda v: v // 5), edge_img, 0.85)
    edge_img.save(os.path.join(out, f'{name}_edges.png'))
    Image.blend(ref, cap, 0.5).save(os.path.join(out, f'{name}_onion.png'))
    d = ImageOps.grayscale(ImageChops.difference(ref, cap)).point(lambda v: 255 if v > 48 else 0)
    base = ImageOps.grayscale(ref).convert('RGB').point(lambda v: 60 + v // 3)
    red = Image.new('RGB', ref.size, (230, 30, 40))
    Image.composite(red, base, d).save(os.path.join(out, f'{name}_diff.png'))
    side = Image.new('RGB', (ref.width * 2 + 8, ref.height), (255, 0, 255))
    side.paste(ref, (0, 0)); side.paste(cap, (ref.width + 8, 0)); side.save(os.path.join(out, f'{name}_side.png'))
    # bölge istatistiği
    regs = REGIONS.get(a.regions or '', None)
    if regs is None:
        W, H = ref.size
        regs = {f'h{r}{c}': (c * W // 3, r * H // 3, W // 3, H // 3) for r in range(3) for c in range(3)}
    tot = d.histogram()[255] / (ref.width * ref.height) * 100
    print(f'{"bölge":<16}{"fark %":>8}   kenar uyumu %')
    for k, (x, y, w, h) in regs.items():
        box = (x, y, x + w, y + h)
        dd = d.crop(box).histogram()[255] / (w * h) * 100
        e1 = er.crop(box).histogram()[255]; eb = both.crop(box).histogram()[255]
        match = (eb / e1 * 100) if e1 else 100.0
        print(f'{k:<16}{dd:>7.1f}%   {match:6.1f}%')
    print(f'{"TOPLAM":<16}{tot:>7.1f}%')
    print('çıktılar:', out)


if __name__ == '__main__':
    main()
