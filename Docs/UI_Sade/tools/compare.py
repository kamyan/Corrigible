#!/usr/bin/env python3
"""Unity ekran görüntüsünü referans PNG ile karşılaştırır.

kullanım:
  python3 compare.py CAPTURE.png REFERENCE.png [--tsv reference/measure/S-Masa1.tsv] [--out captures/S-Masa1_cmp]

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

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('capture'); ap.add_argument('reference')
    ap.add_argument('--tsv'); ap.add_argument('--out')
    ap.add_argument('--thr', type=float, default=38.0)
    a = ap.parse_args()
    ref_im = Image.open(a.reference)
    ref = load(a.reference)
    cap = load(a.capture, ref_im.size)
    out = a.out or os.path.splitext(a.capture)[0] + '_cmp'
    er, ec = edges(ref, a.thr), edges(cap, a.thr)
    vis = np.zeros(ref.shape, np.uint8)
    vis[er & ~ec] = (40, 220, 90)
    vis[ec & ~er] = (220, 60, 220)
    vis[er & ec] = (255, 255, 255)
    Image.fromarray(vis).save(out + '_edges.png')
    d = np.abs(ref - cap).mean(axis=2)
    Image.fromarray(np.clip(d * 3, 0, 255).astype(np.uint8)).save(out + '_diff.png')
    f1 = Image.fromarray(ref.astype(np.uint8)); f2 = Image.fromarray(cap.astype(np.uint8))
    f1.save(out + '_blink.gif', save_all=True, append_images=[f2], duration=500, loop=0)
    print(f'genel ortalama fark: {d.mean():.2f} (0–255)  ·  kenar örtüşmesi: {(er & ec).sum() / max(1, er.sum()):.1%}')
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
            dd = d[y0:y1, x0:x1].mean()
            e = er[y0:y1, x0:x1]
            ov = (e & ec[y0:y1, x0:x1]).sum() / max(1, e.sum())
            print(f'{r["id"][:32]:32} {dd:6.1f} {ov:7.1%}')

if __name__ == '__main__':
    main()
