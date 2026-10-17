"""Sade paketindeki bozuk (tamamen saydam) tarama dokularını üretir.
Kullanım: python3 hatch_textures.py <Assets/Corrigible/Desk/Art/Sade/Textures>   → tx_carbon_hatch.png, tx_quar_hatch.png
          python3 hatch_textures.py --check <tx_hazard.png>                     → üreteci sağlam tx_hazard ile doğrular
Ayrıntı: Docs/UI_Sade/KARARLAR.md › F1."""
from PIL import Image
import sys
def hatch(size, frac, stripe, bg, diag, ss=8, phase=0.0):
    """diag='/' : sabit x+y çizgileri (CSS 135deg); '\\' : sabit x−y (SVG rotate(−45))."""
    im = Image.new('RGBA', (size, size))
    px = im.load()
    for y in range(size):
        for x in range(size):
            acc = [0.0, 0.0, 0.0, 0.0]
            for sy in range(ss):
                for sx in range(ss):
                    fx = x + (sx + 0.5) / ss; fy = y + (sy + 0.5) / ss
                    t = (fx + fy) if diag == '/' else (fx - fy)
                    u = ((t / size) + phase) % 1.0
                    c = stripe if u < frac else bg
                    a = c[3] / 255.0
                    acc[0] += c[0] * a; acc[1] += c[1] * a; acc[2] += c[2] * a; acc[3] += a
            n = ss * ss; A = acc[3] / n
            if A > 0: px[x, y] = (round(acc[0] / acc[3]), round(acc[1] / acc[3]), round(acc[2] / acc[3]), round(A * 255))
            else: px[x, y] = (stripe[0], stripe[1], stripe[2], 0)
    return im
if __name__ == '__main__':
    if sys.argv[1] == '--check':
        ref = Image.open(sys.argv[2]).convert('RGBA')
        best = None
        for phase in [i / 64 for i in range(64)]:
            for diag in ['/', '\\']:
                im = hatch(32, 1.5 / 3.4, (154, 30, 20, 255), (243, 237, 220, 255), diag, ss=6, phase=phase)
                d = sum(abs(a - b) for p, q in zip(im.getdata(), ref.getdata()) for a, b in zip(p, q)) / (32 * 32 * 4)
                if best is None or d < best[0]: best = (d, diag, phase)
        print('en iyi', best)
    else:
        out = sys.argv[1].rstrip('/') + '/'
        # Karbon belirsizlik: repeating-linear-gradient(135deg, rgba(82,73,115,.75) 0 1.4px, transparent 1.4px 3.2px); 32 px = 4,525 tasarım px
        hatch(32, 1.4 / 3.2, (82, 73, 115, 191), (82, 73, 115, 0), '/').save(out + 'tx_carbon_hatch.png')
        # Karantina: SVG pattern 9×9 rotate(−45), şerit 2,2 px #1F5A33 α .30; 64 px = 12,728 tasarım px
        hatch(64, 2.2 / 9, (31, 90, 51, 77), (31, 90, 51, 0), '\\').save(out + 'tx_quar_hatch.png')
