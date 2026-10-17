#!/usr/bin/env python3
"""Unity ekran görüntüsünü referans görüntüyle (reference/screens/*.jpg ya da PNG) karşılaştırır.

kullanım:
  python3 compare.py CAPTURE.png REFERENCE.png [--tsv reference/measure/F-Masa1.tsv] [--mask reference/measure/F-Masa1_mask.json] [--out captures/F-Masa1_cmp]

--mask: zamana bağlı alanları (küreler) karşılaştırmadan çıkarır. JSON: {"rects": [{"x":…, "y":…, "w":…, "h":…}, …]}
        (tasarım px, 1920×1080). Maskeli pikseller fark ve kenar hesabına girmez; _diff.png'de koyu mor görünür.
--pad:  maske kutularını her yönde genişletir (varsayılan 0).

çıktılar (--out önekiyle):
  _edges.png  referans kenarları yeşil, Unity kenarları mor, çakışan kenarlar beyaz
  _diff.png   mutlak renk farkı (parlak = fark)
  _blink.gif  iki görüntü arasında 0,5 sn yanıp sönme
ekrana: genel fark ve (TSV verilirse) her üst düzey öğe kutusu için ortalama fark + kenar örtüşmesi.
gereken: Python 3.9+, Pillow, numpy
"""
import argparse, csv, os, sys
import numpy as np
from PIL import Image

def load(p, size=None):
    im = Image.open(p).convert('RGB')
    if size and im.size != size:
        print(f'uyarı: {os.path.basename(p)} {im.size} → {size} ölçeklendi', file=sys.stderr)
        im = im.resize(size, Image.LANCZOS)
    return np.asarray(im).astype(np.float32)

def edges(a, thr=38.0):
    g = a @ np.array([.299, .587, .114], np.float32)
    gx = np.zeros_like(g); gy = np.zeros_like(g)
    gx[:, 1:-1] = g[:, 2:] - g[:, :-2]
    gy[1:-1, :] = g[2:, :] - g[:-2, :]
    return np.hypot(gx, gy) > thr

def mask_of(path, shape, pad=0):
    import json
    H, W = shape[:2]
    m = np.zeros((H, W), bool)
    sx, sy = W / 1920.0, H / 1080.0
    for r in json.load(open(path, encoding='utf-8')).get('rects', []):
        x0 = int(max(0, (r['x'] - pad) * sx)); y0 = int(max(0, (r['y'] - pad) * sy))
        x1 = int(min(W, (r['x'] + r['w'] + pad) * sx)); y1 = int(min(H, (r['y'] + r['h'] + pad) * sy))
        m[y0:y1, x0:x1] = True
    return m

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('capture'); ap.add_argument('reference')
    ap.add_argument('--tsv'); ap.add_argument('--out')
    ap.add_argument('--thr', type=float, default=38.0)
    ap.add_argument('--mask'); ap.add_argument('--pad', type=float, default=0)
    a = ap.parse_args()
    ref_im = Image.open(a.reference)
    ref = load(a.reference)
    cap = load(a.capture, ref_im.size)
    out = a.out or os.path.splitext(a.capture)[0] + '_cmp'
    er, ec = edges(ref, a.thr), edges(cap, a.thr)
    M = mask_of(a.mask, ref.shape, a.pad) if a.mask else np.zeros(ref.shape[:2], bool)
    er &= ~M; ec &= ~M
    vis = np.zeros(ref.shape, np.uint8)
    vis[er & ~ec] = (40, 220, 90)
    vis[ec & ~er] = (220, 60, 220)
    vis[er & ec] = (255, 255, 255)
    Image.fromarray(vis).save(out + '_edges.png')
    d = np.abs(ref - cap).mean(axis=2)
    d[M] = 0
    dv = np.stack([np.clip(d * 3, 0, 255)] * 3, axis=2).astype(np.uint8)
    dv[M] = (40, 0, 60)
    Image.fromarray(dv).save(out + '_diff.png')
    f1 = Image.fromarray(ref.astype(np.uint8)); f2 = Image.fromarray(cap.astype(np.uint8))
    f1.save(out + '_blink.gif', save_all=True, append_images=[f2], duration=500, loop=0)
    keep = ~M
    print(f'genel ortalama fark: {d[keep].mean():.2f} (0–255, maskesiz alan %{keep.mean() * 100:.0f})  ·  kenar örtüşmesi: {(er & ec).sum() / max(1, er.sum()):.1%}')
    if a.tsv:
        rows = list(csv.DictReader(open(a.tsv, encoding='utf-8'), delimiter='\t'))
        print(f'{"öğe":32} {"fark":>6} {"kenar":>7}')
        H, W = d.shape
        for r in rows:
            if ' > ' in r['id']:
                continue
            x, y, w, h = (int(round(float(r[k]))) for k in ('x', 'y', 'w', 'h'))
            if w <= 0 or h <= 0 or w * h > W * H * .9:
                continue
            x0, y0, x1, y1 = max(0, x), max(0, y), min(W, x + w), min(H, y + h)
            if x1 <= x0 or y1 <= y0:
                continue
            kk = keep[y0:y1, x0:x1]
            if kk.mean() < .25:
                continue
            dd = d[y0:y1, x0:x1][kk].mean()
            e = er[y0:y1, x0:x1]
            ov = (e & ec[y0:y1, x0:x1]).sum() / max(1, e.sum())
            print(f'{r["id"][:32]:32} {dd:6.1f} {ov:7.1%}')

if __name__ == '__main__':
    main()
