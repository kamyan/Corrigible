"""KOS harekât botu özeti: python3 -I ozet.py [--dir OUT/out] etiket [etiket…]

Her etiketin her yapılandırması (kapı-olay-şalter) için bir satır, sonra "iyi" küme (karışık+tecrit, karışık+ucuz,
kefilli+ucuz; şalter çekilmez) ve olay türlerinin payı. Alanlar: zafer/dagilma/terk/dusus/süren yüzdesi, zafer uzunluğu
ort/medyan, 60. vardiyada süren harekât, erzak_azaliyor görülen ve aç kapanış görülen harekât yüzdesi, olay/harekât, zafer
tonları temiz/bedelli/gölgeli, ölü ortalaması, boş geçen vardiya, en düşük erzak medyanı, yollanan insan/sızmacı.
"""
import sys, glob, os, statistics as st
from collections import Counter

IYI3 = {("karisik", "tecrit"), ("karisik", "ucuz"), ("kefilli", "ucuz")}
args = sys.argv[1:]
root = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Library", "DengeBotu", "out")
if args and args[0] == "--dir":
    root, args = args[1], args[2:]


def load(tag):
    out = []
    for f in sorted(glob.glob(os.path.join(root, tag, "*.txt"))):
        if os.path.basename(f) == "BAL.txt":
            continue
        for l in open(f):
            if l.startswith("C\t"):
                out.append(dict(kv.split("=", 1) for kv in l.rstrip("\n").split("\t")[1:]))
    return out


def kinds(r):
    c = Counter()
    for kv in filter(None, r.get("kinds", "").split(",")):
        k, n = kv.split(":")
        c[k] += int(n)
    return c


pc = lambda a, b: 100 * a / max(1, b)


def line(name, g):
    n = len(g)
    if n == 0:
        return
    e = Counter(r["end"] for r in g)
    w = [r for r in g if r["end"] == "silme"]
    wl = [int(r["day"]) for r in w]
    t = Counter(r["tone"] for r in w)
    ev = st.mean(sum(kinds(r).values()) for r in g)
    sent = lambda k: sum(float(r.get(k, 0)) for r in g)
    print(f"{name:24s} n={n:3d} zafer {pc(e['silme'],n):3.0f} dag {pc(e['dagilma'],n):3.0f} terk {pc(e['terk'],n):3.0f} "
          f"dusus {pc(e['dusus'],n):3.0f} salter {pc(e['salter'],n):3.0f} sur {pc(e['suruyor'],n):3.0f} | "
          f"uz {st.mean(wl) if wl else 0:4.1f}/{st.median(wl) if wl else 0:4.1f} at60 {sum(int(r['at60']) for r in g):3d} | "
          f"erzAz {pc(sum(kinds(r)['erzak_azaliyor']>0 for r in g),n):3.0f} ac {pc(sum(int(r['starving'])>0 for r in g),n):3.0f} "
          f"olay {ev:4.1f} | ton t/b/g {t['temiz']}/{t['bedelli']}/{t['golgeli']} "
          f"(%{pc(t['temiz'],len(w)):.1f}/%{pc(t['golgeli'],len(w)):.1f}) | olu {st.mean(float(r['dead']) for r in g):.1f} "
          f"bos {st.mean(float(r.get('noRaid',0)) for r in g):.1f} minSup {st.median(float(r['minSup']) for r in g):.1f} "
          f"yollanan i/s {sent('sentH'):.0f}/{sent('sentR'):.0f}")


for tag in args:
    rs = load(tag)
    bal = os.path.join(root, tag, "BAL.txt")
    print("##", tag, open(bal).read().strip() if os.path.exists(bal) else "")
    g = {}
    for r in rs:
        g.setdefault((r["door"], r["ev"], r["sw"]), []).append(r)
    for k in sorted(g):
        line("-".join(k), g[k])
    iyi = [r for r in rs if (r["door"], r["ev"]) in IYI3 and r["sw"] == "hic"]
    line("IYI3", iyi)
    line("HEPSI", rs)
    for nm, grp in (("iyi", iyi), ("hepsi", rs)):
        kc = Counter()
        for r in grp:
            kc += kinds(r)
        tot = sum(kc.values())
        if tot:
            print(f"  türler ({nm}):", ", ".join(f"{k} {100*v/tot:.0f}" for k, v in kc.most_common(6)))
