"""KOS harekât botu özeti: python3 -I ozet.py [--dir OUT/out] [--pkg] etiket [etiket…]

Her etiketin her yapılandırması (kapı-olay-şalter) için bir satır, sonra "iyi" küme (karışık+tecrit, karışık+ucuz,
kefilli+ucuz; şalter çekilmez) ve olay türlerinin payı. Alanlar: zafer/dagilma/terk/dusus/süren yüzdesi, zafer uzunluğu
ort/medyan, 60. vardiyada süren harekât, erzak_azaliyor görülen ve aç kapanış görülen harekât yüzdesi, olay/harekât, zafer
tonları temiz/bedelli/gölgeli, ölü ortalaması, boş geçen vardiya, en düşük erzak medyanı, yollanan insan/sızmacı.
--pkg: paket × kademe tablosu (her paket hangi kademede kaç kez alındı, baskın başına başarısızlık) ve tohum başına zafer
yayılımı (en kötü %10); alanlar Camp8'in pkg satırından (üretilmiş harita: BAL="liberationMapGen=1").
--baskin: tesis baskınları aşama 0 taban tablosu ("iyi" küme; Camp8'in R satırlarından): baskın başına tur (oyuncu turu
ve süre dolumu), uzman hamlesi, görev ve kademeye göre başarı, düşen/kurtarılan/ölen, kırılan sivil, yeniden alınan sektör
ve bölge (Docs/TESIS-BASKINLARI-TASARIMI.md §11.6).
--tesis [--taban etiket]: tesis baskınları aşama 4 tablosu ("iyi" küme; R ve C satırlarından). Önce §11.6 "Bot ölçümü"
hedef tablosunun ölçülebilen satırları (etiketler yan yana; hedef ve taban sütunu, köşeli parantezde tabanla fark), sonra
tesise özgü tablolar (bitiş odası, bitiş nedeni, oda başına tur/yürüyüş/yetenek, süre dolan baskının odası ve kepenk payı,
geride kalan, yürünen oda, donanım) ve aşama 6 (düşen politikası, düşen/taşınan/toplanan/taşınmayan, tabanın kurtarılanına
oran, eylem kullanımı, pusu isabeti, kepenk ve büyük sandık, yükün düşmesi) ve aşama 8 (ödül/araç/ekipman politikası, baskın
başına ölü, tür başına "tarifi açık baskında kullanıldı" hedefi ve taşınma, eylem ve etki / baskın, köpek ve tutsak adımı, üretim,
ödül ve kazanım dağılımı). Taban varsayılanı t17 (klasörü varsa; tek kipten, kayıt 21, önce t0gen); "--taban -" tabanı kapatır.
--esli A B [A B …]: aynı tohumlarda eşli fark A − B ("iyi" küme): zafer, dağılma, ölü / harekât, baskın / harekât, ölü / baskın,
başarı / baskın (tohum ortalaması ± tohum kümeli standart hata; aşama 8); aşama 10'da ayrıca cezalı taşıma (penCarry / carryItems).
--fark A B C D [A B C D …]: farkların farkı (A − B) − (C − D), tohum başına, aynı ölçüler (aşama 10: tek aracın önlemle cezası
(t10tek − t10tekK0) − (t10donus − t10donusK0)); ayrıca tür başına "tarifi açık baskında kullanıldı" payı (puan).
--tesis'in "aşama 10" bölümü: önlem defteri (R cm, tl, tlb, learn, ans, cmfx, plates, sqHp, sqMv, sqPw, rook, half; C learns,
cmEnd, nLearn, rwCevap, prod_<tür>, cmRaids_<sınıf>_<k>, penCarry, carryItems); anahtar yoksa "-".
--tesis'in "aşama 11" bölümü: kalıcı tahta ve savunma (S satırları: savunma baskını başına; C siege, siegeNone, defend, yield,
autoYield, defNoSquad, nrSiege, nrYield, def*, marks*, recapDenied, counterDoor, defLossDag5; R retry, mk, mkNew, mkKind): kuşatma /
harekât, savunma başarısı (tümü, kademe, terk; hedef %40–60, tohum kümeli SE), güven, ölü ve çıkarılan, tur ve hamle, dalga ve röle,
yeniden alma, izler ve I3 defter denetimi (marksMade − marksGone − marksRepaired − marksWiped = marksEnd; 0 olmalı). --esli'de ayrıca
kuşatma / harekât, savunma başarısı (tohum oranı), yeniden alma baskını / harekât, nrYield / harekât, son güven.
--tesis'in "aşama 12" bölümü: Katar (na) ve Yalpa (oc) baskınları (R ayir, kenar, det, detNo, fol, folRooms, edgeM, edgeP, edgeX,
edgeSrc, eDied, eInj, tilt, slide, tiltHit, evP, evE; fate'in e kategorisi; S edgeM, edgeP, fol, tilt): Katar'da başarı (tohum kümeli
SE), AYIR fırsatı ve nedenleri, takip, kenardan düşen makine (kaynağa ve odaya göre) ve uzman, fate dağılımı, yatış, itme önlemi;
Yalpa'da yatış, kayma, çarpma ve olay hasarı; savunmada şartlar; yapı denetimleri (G/g'de kenar olayı, takvim dışı yatış, oc'de
tl ≠ tlb, kenardan düşüp ölen, yatış ≠ 5 × düşen, şartın bölgesi dışında olay, savunmada şart; hepsi 0 olmalı). Tabanda (aşama 11)
aynı bölgelerin asansör ve yağ satırları basılır.
--ilk BÖLGE A B [A B …]: bölgenin İLK baskını eşli (aşama 12): her çiftte aynı harekâtın öneki (bölgenin ilk baskınından önceki
R/S satırları, aşama 12 anahtarları atılarak) aynıysa ilk baskının ölçüleri A − B (tohum kümeli SE); geçerli çift sayısı yazılır.
"""
import sys, glob, os, statistics as st
from collections import Counter

IYI3 = {("karisik", "tecrit"), ("karisik", "ucuz"), ("kefilli", "ucuz")}
args = sys.argv[1:]
root = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Library", "DengeBotu", "out")
if args and args[0] == "--dir":
    root, args = args[1], args[2:]
PKG = "--pkg" in args
BASKIN = "--baskin" in args
TESIS = "--tesis" in args
# --esli A B [A B …]: aynı tohumlarda eşli fark ("iyi" küme; tohum ortalaması ± tohum kümeli standart hata; aşama 8)
ESLI = "--esli" in args
# --fark A B C D [A B C D …]: farkların farkı (A − B) − (C − D), tohum başına (aşama 10)
FARK = "--fark" in args
TABAN = None
if "--taban" in args:
    i = args.index("--taban")
    TABAN, args = (args[i + 1] if i + 1 < len(args) else "-"), args[:i] + args[i + 2:]
# --ilk BÖLGE A B [A B …]: bölgenin ilk baskını eşli (aşama 12)
ILK = "--ilk" in args
args = [a for a in args if a not in ("--pkg", "--baskin", "--tesis", "--esli", "--fark", "--ilk")]


def load(tag, prefix="C\t"):
    out = []
    for f in sorted(glob.glob(os.path.join(root, tag, "*.txt"))):
        if os.path.basename(f) == "BAL.txt":
            continue
        for l in open(f):
            if l.startswith(prefix):
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


def seed_groups(tag):
    # "iyi" küme, tohum başına harekâtlar (C satırları)
    by = {}
    for r in load(tag):
        if (r["door"], r["ev"]) in IYI3 and r["sw"] == "hic":
            by.setdefault(r["seed"], []).append(r)
    return by


_num = lambda r, k: float(r.get(k, 0))
# Eşli ölçüler (tohum başına üç "iyi" yapılandırmanın ortalaması; oranlarda toplam ÷ toplam). Aşama 10: cezalı taşıma (taşınan
# eşyanın sınıfında önlem olan eşya-baskın payı; önlemsiz koşuda 0)
PAIR_M = {"zafer %": lambda g: 100 * st.mean(r["end"] == "silme" for r in g),
          "dağılma %": lambda g: 100 * st.mean(r["end"] == "dagilma" for r in g),
          "ölü / harekât": lambda g: st.mean(_num(r, "dead") for r in g),
          "baskın / harekât": lambda g: st.mean(_num(r, "raids") for r in g),
          "ölü / baskın": lambda g: sum(_num(r, "dead") for r in g) / max(1, sum(_num(r, "raids") for r in g)),
          "başarı / baskın %": lambda g: 100 * (1 - sum(_num(r, "fails") for r in g) / max(1, sum(_num(r, "raids") for r in g))),
          "cezalı taşıma %": lambda g: 100 * sum(_num(r, "penCarry") for r in g) / max(1, sum(_num(r, "carryItems") for r in g)),
          # Aşama 11: kuşatma, savunma başarısı (savunmalı tohumlarda toplam ÷ toplam; savunmasız tohumda 0), yeniden alma, bırakılan kanada
          # baskın reddi, son güven
          "kuşatma / harekât": lambda g: st.mean(_num(r, "siege") for r in g),
          "savunma başarısı %": lambda g: (lambda n: 100 * sum(_num(r, "defWin") for r in g) / n if n else None)(
              sum(_num(r, "defWin") + _num(r, "defLoss") for r in g)),
          "yeniden alma baskını / harekât": lambda g: st.mean(_num(r, "recapRaids") for r in g),
          "nrYield / harekât": lambda g: st.mean(_num(r, "nrYield") for r in g),
          "son güven": lambda g: st.mean(_num(r, "trust") for r in g)}


def esli(pairs):
    # Aşama 8: aynı tohumlarda eşli fark (A − B). Tohum başına üç "iyi" yapılandırmanın ortalaması (oranlarda toplam ÷ toplam), sonra
    # tohum ortalaması ± tohum kümeli standart hata (kapanis/es.py kalıbı; baskın başına ölü ve başarı eklendi)
    for i in range(0, len(pairs) - 1, 2):
        a, b = pairs[i], pairs[i + 1]
        x, y = seed_groups(a), seed_groups(b)
        ks = sorted(set(x) & set(y), key=int)
        for nm, f in PAIR_M.items():
            # Aşama 11: ölçü bir tohumda tanımsızsa (None: ör. savunmasız tohumda savunma başarısı) o tohum atlanır
            df = [u - v for u, v in ((f(x[k]), f(y[k])) for k in ks) if u is not None and v is not None]
            se = st.stdev(df) / len(df) ** .5 if len(df) > 1 else 0
            print(f"{a} − {b}  {nm}: {st.mean(df):+.3f} ±{se:.3f} ({len(df)} tohum)" if df else f"{a} − {b}  {nm}: -")


USE8 = {"wire_kit": ("wire", "plank"), "charge": ("charge",), "turret": ("turret",), "decoy": ("decoy",), "winch": ("winch",),
        "clamp": ("clamp",), "dog": ("bark", "fetch", "sniff")}


def _gx(r):
    return {k: int(v) for k, v in (it.split(":") for it in r.get("gx", "").split("/") if ":" in it)}


def fark(quads):
    # Aşama 10: farkların farkı (A − B) − (C − D), tohum başına (dört etiketin ortak tohumları), tohum ortalaması ± tohum kümeli
    # standart hata. Harekât ölçüleri PAIR_M; baskın ölçüsü tür başına "tarifi açık baskında kullanıldı" (R satırları, puan)
    def rseeds(tag):
        by = {}
        for r in load(tag, "R\t"):
            if (r["door"], r["ev"]) in IYI3 and r["sw"] == "hic":
                by.setdefault(r["seed"], []).append(r)
        return by
    for i in range(0, len(quads) - 3, 4):
        a, b, c, d = quads[i:i + 4]
        gs = [seed_groups(t) for t in (a, b, c, d)]
        ks = sorted(set(gs[0]) & set(gs[1]) & set(gs[2]) & set(gs[3]), key=int)
        name = f"({a} − {b}) − ({c} − {d})"
        for nm, f in PAIR_M.items():
            vs = [[f(g[k]) for g in gs] for k in ks]
            df = [(v[0] - v[1]) - (v[2] - v[3]) for v in vs if None not in v]
            se = st.stdev(df) / len(df) ** .5 if len(df) > 1 else 0
            print(f"{name}  {nm}: {st.mean(df):+.3f} ±{se:.3f} ({len(df)} tohum)" if df else f"{name}  {nm}: -")
        rs = [rseeds(t) for t in (a, b, c, d)]
        for kind, acts in USE8.items():
            def rate(g):
                op = [r for r in g if kind in r.get("rec", "-").split("+")]
                return 100 * sum(1 for r in op if sum(_gx(r).get(x, 0) for x in acts) > 0) / len(op) if op else None
            df = []
            for k in ks:
                v = [rate(rs[j].get(k, [])) for j in range(4)]
                if None not in v:
                    df.append((v[0] - v[1]) - (v[2] - v[3]))
            se = st.stdev(df) / len(df) ** .5 if len(df) > 1 else 0
            print(f"{name}  {kind} kullanıldı (tarifi açık) %: {st.mean(df):+.2f} ±{se:.2f} ({len(df)} tohum)" if df else f"{name}  {kind}: -")


if FARK:
    fark(args)
    sys.exit(0)

if ESLI:
    esli(args)
    sys.exit(0)

for tag in ([] if ILK else args):
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


def pkg_table(rs):
    tiers, fails = Counter(), Counter()
    for r in rs:
        for item in filter(None, r.get("pkg", "").split(",")):
            pid, order, tier, nf = item.split(":")
            tiers[(pid, int(tier))] += 1
            fails[pid] += int(nf)
    ids = sorted({k[0] for k in tiers})
    print("  paket  k1  k2  k3  k4 | başarısız/harekât")
    for pid in ids:
        row = " ".join(f"{tiers[(pid, t)]:3d}" for t in (1, 2, 3, 4))
        print(f"  {pid:5s} {row} | {fails[pid] / max(1, len(rs)):.2f}")
    seeds = {}
    for r in rs:
        seeds.setdefault(r["seed"], []).append(r["end"] == "silme")
    rate = sorted(100 * sum(v) / len(v) for v in seeds.values())
    if rate:
        k = max(1, len(rate) // 10)
        print(f"  tohum başına zafer: ort {st.mean(rate):.0f}, en kötü %10 ort {st.mean(rate[:k]):.0f}, en kötü {rate[0]:.0f}")


if PKG:
    for tag in args:
        print("## paket", tag)
        pkg_table(load(tag))


def q(xs, p):
    xs = sorted(xs)
    return xs[min(len(xs) - 1, int(p * len(xs)))] if xs else 0


def baskin_table(tag):
    iyi = lambda r: (r["door"], r["ev"]) in IYI3 and r["sw"] == "hic"
    rs = [r for r in load(tag, "R\t") if iyi(r)]
    cs = [r for r in load(tag) if iyi(r)]
    if not rs:
        print("  R satırı yok (Camp8 RAIDS=0 ya da eski çıktı)")
        return
    n, nc = len(rs), max(1, len(cs))
    num = lambda r, k: float(r[k])
    ok = [r for r in rs if r["ok"] == "1"]
    print(f"  harekât {len(cs)}, baskın {n} (harekât başına {n / nc:.1f})")
    for nm, g in (("hepsi", rs), ("kazanılan", ok)):
        tu, pt = [num(r, "turns") for r in g], [num(r, "plTurns") for r in g]
        print(f"  tur ({nm}): sonuç turu ort {st.mean(tu):.2f} med {st.median(tu):.0f} p90 {q(tu, .9):.0f} | "
              f"oyuncu turu ort {st.mean(pt):.2f} med {st.median(pt):.0f} p90 {q(pt, .9):.0f}")
    mv, ac = [num(r, "moves") for r in rs], [num(r, "acts") for r in rs]
    tot = [a + b for a, b in zip(mv, ac)]
    print(f"  uzman hamlesi/baskın: yürüyüş {st.mean(mv):.1f}, yetenek {st.mean(ac):.1f}, toplam ort {st.mean(tot):.1f} "
          f"med {st.median(tot):.0f} p90 {q(tot, .9):.0f}")
    print("  başarı:   görev    k1        k2        k3        k4        hepsi")
    for kind in ("loot", "server"):
        cells = []
        for tier in ("1", "2", "3", "4", None):
            g = [r for r in rs if r["kind"] == kind and (tier is None or r["tier"] == tier)]
            cells.append(f"%{pc(sum(r['ok'] == '1' for r in g), len(g)):3.0f} ({len(g):4d})" if g else "   -      ")
        print(f"            {kind:7s} " + " ".join(cells))
    sm = lambda k, g=rs: sum(num(r, k) for r in g)
    print(f"  düşen/baskın {sm('down') / n:.2f}; kurtarılan (bugün kendiliğinden; tesiste taşınmazsa ölür) "
          f"{sm('rescued') / n:.2f}/baskın, {sm('rescued') / nc:.1f}/harekât; ölen {sm('died') / n:.2f}/baskın, "
          f"{sm('died') / nc:.1f}/harekât (harekât satırı ölü ort {st.mean(float(r['dead']) for r in cs):.1f})")
    ends = Counter(r.get("end", "?") for r in rs)
    print(f"  kırılan sivil/baskın {sm('collat') / n:.2f}; bitiş: " + ", ".join(f"{k} %{pc(v, n):.1f}" for k, v in ends.most_common()))
    cv = lambda k: sum(float(r.get(k, 0)) for r in cs) / nc
    print(f"  yeniden alınan: sektör baskını {cv('recapRaids'):.2f}/harekât (başarılı {cv('recapSectors'):.2f}), "
          f"yeniden tamamlanan bölge {cv('recapRegions'):.2f}/harekât, yeniden verilen fon {cv('recapFunds'):.1f}, "
          f"güven {cv('recapTrust'):.2f}")
    ev = Counter((r["event"], r["ok"]) for r in rs)
    evs = sorted({r["event"] for r in rs})
    print("  olaya göre başarı: " + ", ".join(f"{e} %{pc(ev[(e, '1')], ev[(e, '1')] + ev[(e, '0')]):.0f}" for e in evs))


if BASKIN:
    for tag in args:
        print("## baskın tabanı", tag)
        baskin_table(tag)


# ---- tesis baskınları aşama 4 (--tesis) ----
# Ölçüler "iyi" kümededir (--baskin gibi). Her ölçü (sayı, metin) verir; sayı tabanla farkı için. Tesis anahtarları (rooms, rt,
# devG…) olmayan çıktıda (taban) tesise özgü ölçüler "-" yazar. Yüzdeler 0–100; fark yüzde ölçülerinde puandır.
ROOMS = "GAHag"
ENDS = ("zafer", "sure", "cekilme", "tasiyici", "diger", "ekip")


def tesis_load(tag):
    iyi = lambda r: (r["door"], r["ev"]) in IYI3 and r["sw"] == "hic"
    ar, ac = load(tag, "R\t"), load(tag)
    rs, cs = [r for r in ar if iyi(r)], [r for r in ac if iyi(r)]
    # Aşama 11: savunma baskınının S satırları (R süzgecine girmez)
    ss = [r for r in load(tag, "S\t") if iyi(r)]
    return {"tag": tag, "rs": rs, "cs": cs, "ss": ss, "drop": (len(ar) - len(rs), len(ac) - len(cs)),
            "fac": any("rooms" in r for r in rs)}


def per_room(s):
    c = Counter()
    for it in filter(None, s.split("/")):
        c[it[0]] += int(it[1:])
    return c


def t_pct(g, f):
    return (pc(sum(1 for r in g if f(r)), len(g)), f"%{pc(sum(1 for r in g if f(r)), len(g)):.1f} ({len(g)})") if g else None


def t_mean(xs, d=2):
    return (st.mean(xs), f"{st.mean(xs):.{d}f}") if xs else None


def t_num(v, d=2):
    return (v, f"{v:.{d}f}")


def seed_rates(cs, f):
    by = {}
    for r in cs:
        by.setdefault(r["seed"], []).append(1 if f(r) else 0)
    return {k: 100 * st.mean(v) for k, v in by.items()}


def se_seed(cs, f):
    v = list(seed_rates(cs, f).values())
    return (st.stdev(v) / len(v) ** .5, f"±{st.stdev(v) / len(v) ** .5:.1f} ({len(v)} tohum)") if len(v) > 1 else None


def paired(d, b, f):
    if b is None:
        return None
    x, y = seed_rates(d["cs"], f), seed_rates(b["cs"], f)
    ks = sorted(set(x) & set(y))
    if len(ks) < 2:
        return None
    df = [x[k] - y[k] for k in ks]
    m, se = st.mean(df), st.stdev(df) / len(df) ** .5
    return (m, f"{m:+.1f} ±{se:.1f} ({len(ks)} tohum)")


def ckey(r):
    return (r["door"], r["ev"], r["sw"], r["seed"])


def tesis_rows(tiers):
    rs = lambda d: d["rs"]
    cs = lambda d: d["cs"]
    fac = lambda fn: (lambda d, b: fn(d, b) if d["fac"] else None)
    num = lambda r, k: float(r[k])
    win = lambda r: r["end"] == "silme"
    dev = lambda r, a, g: r.get("devG", "0") == a and r.get("devA", "0") == g
    ok = lambda r: r["ok"] == "1"

    def succ(kind, pred):
        return lambda d, b: t_pct([r for r in d["rs"] if (kind is None or r["kind"] == kind) and pred(r)], ok)

    def gap(kind):
        def f(d, b):
            a = succ(kind, lambda r: dev(r, "1", "1"))(d, b)
            z = succ(kind, lambda r: dev(r, "0", "0"))(d, b)
            return (a[0] - z[0], f"{a[0] - z[0]:+.1f}p") if a and z else None
        return f

    def walked_out(d, b):
        return t_pct(d["rs"], lambda r: r.get("endRoom") == "x")

    R = []  # (bölüm, etiket, hedef, fn(d, b), fark var mı, fark birimi)
    A = "§11.6 hedef tablosu"
    R += [
        (A, "harekât (C satırı)", "≥ 240 (80 tohum)", lambda d, b: (len(d["cs"]), str(len(d["cs"]))), False, ""),
        (A, "baskın / harekât", "", lambda d, b: t_num(len(d["rs"]) / max(1, len(d["cs"])), 1), True, ""),
        (A, "toplam tur / baskın: medyan", "12–16", lambda d, b: t_num(st.median(num(r, "turns") for r in d["rs"]), 1) if d["rs"] else None, True, ""),
        (A, "  ort", "", lambda d, b: t_mean([num(r, "turns") for r in d["rs"]]), True, ""),
        (A, "  p90", "", lambda d, b: t_num(q([num(r, "turns") for r in d["rs"]], .9), 0) if d["rs"] else None, True, ""),
        (A, "  tur bütçesi (max) ort", "", lambda d, b: t_mean([num(r, "max") for r in d["rs"]]), True, ""),
        (A, "uzman hamlesi / baskın: yürüyüş ort", "", lambda d, b: t_mean([num(r, "moves") for r in d["rs"]], 1), True, ""),
        (A, "  yetenek ort", "", lambda d, b: t_mean([num(r, "acts") for r in d["rs"]], 1), True, ""),
        (A, "  toplam ort", "≤ 2,2 × taban", lambda d, b: t_mean([num(r, "moves") + num(r, "acts") for r in d["rs"]], 1), True, ""),
        (A, "  toplam medyan", "", lambda d, b: t_num(st.median(num(r, "moves") + num(r, "acts") for r in d["rs"]), 1) if d["rs"] else None, True, ""),
        (A, "  toplam p90", "", lambda d, b: t_num(q([num(r, "moves") + num(r, "acts") for r in d["rs"]], .9), 0) if d["rs"] else None, True, ""),
        (A, "  toplam ort / taban ort", "≤ 2,2", lambda d, b: (lambda x, y: t_num(x[0] / y[0], 2) if x and y and y[0] else None)(
            t_mean([num(r, "moves") + num(r, "acts") for r in d["rs"]]), t_mean([num(r, "moves") + num(r, "acts") for r in b["rs"]]) if b else None), False, ""),
    ]
    for kind, nm in (("loot", "ikmal"), ("server", "sunucu")):
        for t in tiers + [None]:
            R.append((A, f"başarı {nm} {'k' + t if t else 'hepsi'}", "taban ±5p" if t is None else "",
                      (lambda kind, t: lambda d, b: t_pct([r for r in d["rs"] if r["kind"] == kind and (t is None or r["tier"] == t)], ok))(kind, t), True, "p"))
    R.append((A, "başarı tüm baskınlar", "", lambda d, b: t_pct(d["rs"], ok), True, "p"))
    for kind, nm in (("loot", "ikmal"), ("server", "sunucu"), (None, "hepsi")):
        R.append((A, f"hazırlıksız (devG=0, devA=0) başarı {nm}", "taban ±5p", succ(kind, lambda r: dev(r, "0", "0")), True, "p"))
    for kind, nm in (("loot", "ikmal"), ("server", "sunucu"), (None, "hepsi")):
        R.append((A, f"iki donanım kırık başarı {nm}", "", succ(kind, lambda r: dev(r, "1", "1")), False, "p"))
    for kind, nm in (("loot", "ikmal"), ("server", "sunucu"), (None, "hepsi")):
        R.append((A, f"  fark: iki kırık − hazırlıksız, {nm}", "+5–10p", gap(kind), False, "p"))
    R += [
        (A, "ölü / harekât (C dead)", "≤ 7,5", lambda d, b: t_mean([float(r["dead"]) for r in d["cs"]], 1), True, ""),
        (A, "  baskında ölen / harekât (R died)", "", lambda d, b: t_num(sum(num(r, "died") for r in d["rs"]) / max(1, len(d["cs"])), 1), True, ""),
        (A, "düşen / baskın", "", lambda d, b: t_mean([num(r, "down") for r in d["rs"]]), True, ""),
        (A, "kendiliğinden kurtarılan düşen / harekât", "taşınan+toplanan ≥ %80 (aşama 6)",
         lambda d, b: t_num(sum(num(r, "rescued") for r in d["rs"]) / max(1, len(d["cs"])), 1), True, ""),
        (A, "  / baskın", "", lambda d, b: t_mean([num(r, "rescued") for r in d["rs"]]), True, ""),
        (A, "  zaferle biten harekâtta / harekât", "", lambda d, b: (lambda w: t_num(sum(num(r, "rescued") for r in d["rs"] if ckey(r) in w) / len(w), 1)
                                                                    if w else None)({ckey(r) for r in d["cs"] if win(r)}), True, ""),
        (A, "  kurtarılan / düşen (paydada geride kalan da var)", "", lambda d, b: (lambda dn: (100 * sum(num(r, "rescued") for r in d["rs"]) / dn,
                                                                  f"%{100 * sum(num(r, 'rescued') for r in d['rs']) / dn:.1f}") if dn else None)(
            sum(num(r, "down") for r in d["rs"])), True, "p"),
        (A, "kırılan sivil sunucu / baskın", "≤ 1,7", lambda d, b: t_mean([num(r, "collat") for r in d["rs"]]), True, ""),
        (A, "zafer oranı", "%60–70", lambda d, b: t_pct(d["cs"], win), True, "p"),
        (A, "  standart hata (binom)", "", lambda d, b: (lambda n, p: (100 * (p * (1 - p) / n) ** .5, f"±{100 * (p * (1 - p) / n) ** .5:.1f}") if n else None)(
            len(d["cs"]), sum(map(win, d["cs"])) / max(1, len(d["cs"]))), False, ""),
        (A, "  standart hata (tohum kümeli)", "", lambda d, b: se_seed(d["cs"], win), False, ""),
        (A, "  eşli fark tabanla (aynı tohumlar)", "", lambda d, b: paired(d, b, win), False, ""),
        (A, "zafer uzunluğu medyan", "≤ 48", lambda d, b: (lambda w: t_num(st.median(w), 1) if w else None)([int(r["day"]) for r in d["cs"] if win(r)]), True, ""),
        (A, "  ort", "", lambda d, b: t_mean([int(r["day"]) for r in d["cs"] if win(r)], 1), True, ""),
        (A, "dağılma oranı", "", lambda d, b: t_pct(d["cs"], lambda r: r["end"] == "dagilma"), True, "p"),
        (A, "  medyan günü", "", lambda d, b: (lambda w: t_num(st.median(w), 1) if w else None)([int(r["day"]) for r in d["cs"] if r["end"] == "dagilma"]), True, ""),
        (A, "  eşli fark tabanla (aynı tohumlar)", "", lambda d, b: paired(d, b, lambda r: r["end"] == "dagilma"), False, ""),
        (A, "öbür sonlar, adet (terk/düşüş/topluluk/şalter/süren)", "", lambda d, b: (None, " ".join(
            f"{k[:3]} {sum(r['end'] == k for r in d['cs'])}" for k in ("terk", "dusus", "topluluk", "salter", "suruyor"))) if d["cs"] else None, False, ""),
    ]
    B = "tesise özgü"
    R.append((B, "DEV politikası (R dev)", "", lambda d, b: (None, ",".join(f"{k}:{v}" for k, v in sorted(Counter(r.get("dev", "-") for r in d["rs"]).items()))) if d["rs"] else None, False, ""))
    for e in ENDS:
        R.append((B, f"bitiş nedeni {e}", "", (lambda e: lambda d, b: t_pct(d["rs"], lambda r: r.get("end") == e))(e), True, "p"))
    for k in ROOMS + "x":
        R.append((B, f"bitiş odası {k}" + (" (yürüyerek çıkış)" if k == "x" else ""), "",
                  (lambda k: fac(lambda d, b: t_pct(d["rs"], lambda r: r.get("endRoom") == k)))(k), False, "p"))
    R += [
        (B, "oynanan oda / baskın (rooms)", "", fac(lambda d, b: t_mean([num(r, "rooms") for r in d["rs"]]), ), False, ""),
        (B, "yürünen temiz oda / baskın (walked)", "", fac(lambda d, b: t_mean([num(r, "walked") for r in d["rs"]])), False, ""),
        (B, "  walked > 0 olan baskın", "", fac(lambda d, b: t_pct(d["rs"], lambda r: r["walked"] != "0")), False, "p"),
    ]
    for k in ROOMS:
        R += [
            (B, f"oda {k}: oynanan baskın", "", (lambda k: fac(lambda d, b: t_pct(d["rs"], lambda r: k in per_room(r["rt"]))))(k), False, "p"),
            (B, f"  {k} tur ort", "", (lambda k: fac(lambda d, b: t_mean([per_room(r["rt"])[k] for r in d["rs"] if k in per_room(r["rt"])])))(k), False, ""),
            (B, f"  {k} yürüyüş ort", "", (lambda k: fac(lambda d, b: t_mean([per_room(r["rmv"])[k] for r in d["rs"] if k in per_room(r["rmv"])], 1)))(k), False, ""),
            (B, f"  {k} yetenek ort", "", (lambda k: fac(lambda d, b: t_mean([per_room(r["rac"])[k] for r in d["rs"] if k in per_room(r["rac"])], 1)))(k), False, ""),
        ]
    sure = lambda d: [r for r in d["rs"] if r.get("end") == "sure"]
    R.append((B, "süre dolan baskın (end=sure)", "", lambda d, b: t_pct(d["rs"], lambda r: r.get("end") == "sure"), True, "p"))
    for k in ROOMS:
        R.append((B, f"  süre dolanların odası {k}", "", (lambda k: fac(lambda d, b: t_pct(sure(d), lambda r: r["endRoom"] == k)))(k), False, "p"))
    R += [
        (B, "kepenkli ara oda (part=1), tüm baskın", "", fac(lambda d, b: t_pct(d["rs"], lambda r: r["part"] == "1")), False, "p"),
        (B, "  süre dolanlarda part=1", "", fac(lambda d, b: t_pct(sure(d), lambda r: r["part"] == "1")), False, "p"),
        (B, "  A'da süre dolanlarda part=1", "", fac(lambda d, b: t_pct([r for r in sure(d) if r["endRoom"] == "A"], lambda r: r["part"] == "1")), False, "p"),
        (B, "  A'da süre dolma, part=1 baskınlarda", "", fac(lambda d, b: t_pct([r for r in d["rs"] if r["part"] == "1"], lambda r: r["end"] == "sure" and r["endRoom"] == "A")), False, "p"),
        (B, "  A'da süre dolma, part=0 baskınlarda", "", fac(lambda d, b: t_pct([r for r in d["rs"] if r["part"] == "0"], lambda r: r["end"] == "sure" and r["endRoom"] == "A")), False, "p"),
        (B, "  başarı part=1", "", fac(lambda d, b: t_pct([r for r in d["rs"] if r["part"] == "1"], ok)), False, "p"),
        (B, "  başarı part=0", "", fac(lambda d, b: t_pct([r for r in d["rs"] if r["part"] == "0"], ok)), False, "p"),
        (B, "geride kalan (stuck) / baskın", "", fac(lambda d, b: t_mean([num(r, "stuck") for r in d["rs"]])), False, ""),
        (B, "  / harekât", "", fac(lambda d, b: t_num(sum(num(r, "stuck") for r in d["rs"]) / max(1, len(d["cs"])), 1)), False, ""),
        (B, "  stuck > 0 olan baskın", "", fac(lambda d, b: t_pct(d["rs"], lambda r: r["stuck"] != "0")), False, "p"),
        (B, "donanım kırık: giriş (devG)", "", fac(lambda d, b: t_pct(d["rs"], lambda r: r["devG"] == "1")), False, "p"),
        (B, "  ara (devA)", "", fac(lambda d, b: t_pct(d["rs"], lambda r: r["devA"] == "1")), False, "p"),
        (B, "  ikisi", "", fac(lambda d, b: t_pct(d["rs"], lambda r: dev(r, "1", "1"))), False, "p"),
        (B, "  biri", "", fac(lambda d, b: t_pct(d["rs"], lambda r: r["devG"] != r["devA"])), False, "p"),
        (B, "  hiçbiri (hazırlıksız)", "", fac(lambda d, b: t_pct(d["rs"], lambda r: dev(r, "0", "0"))), False, "p"),
        (B, "  tek kırık başarı (hepsi)", "", fac(lambda d, b: t_pct([r for r in d["rs"] if r["devG"] != r["devA"]], ok)), False, "p"),
        (B, "  kırılan türler (devK)", "", fac(lambda d, b: (None, ",".join(f"{k}:{v}" for k, v in Counter(r["devK"] for r in d["rs"]).most_common()))), False, ""),
        (B, "  donanım vuruşu / baskın (devHits)", "", fac(lambda d, b: t_mean([num(r, "devHits") for r in d["rs"]])), False, ""),
        (B, "  donanım hedefli oda / baskın (goals)", "", fac(lambda d, b: t_mean([num(r, "goals") for r in d["rs"]])), False, ""),
    ]
    # Aşama 6 (hamleler, ölüm kuralı): R satırının dusen, fallen, carried, collected, abandoned, ax, amb, shut, crate, cdrop, cpick
    # anahtarları; yoksa (aşama 6'dan önceki çıktı) "-"
    C6 = "aşama 6: düşen ve hamleler"
    f6 = lambda fn: (lambda d, b: fn(d, b) if any("fallen" in r for r in d["rs"]) else None)
    fb = lambda fn: (lambda d, b: fn(d, b) if any("bsaved" in r for r in d["rs"]) else None)
    per_c = lambda d, k: t_num(sum(num(r, k) for r in d["rs"]) / max(1, len(d["cs"])), 2)
    saved = lambda d: sum(num(r, "carried") + num(r, "collected") for r in d["rs"])

    def vs_base(d, b):
        # (taşınan + toplanan) / harekât ÷ tabanın kurtarılanı / harekât (taban aşama 6 anahtarlarını taşıyorsa onun taşınan + toplananı)
        if b is None or not b["cs"]:
            return None
        bb = (saved(b) if any("fallen" in r for r in b["rs"]) else sum(num(r, "rescued") for r in b["rs"])) / len(b["cs"])
        x = saved(d) / max(1, len(d["cs"]))
        return (100 * x / bb, f"%{100 * x / bb:.1f} ({x:.2f} / {bb:.2f})") if bb else None

    def ax(d, k):
        n = 0
        for r in d["rs"]:
            for it in r.get("ax", "").split("/"):
                if it.startswith(k + ":"):
                    n += int(it.split(":")[1])
        return t_num(n / max(1, len(d["rs"])), 2)

    loot = lambda d: [r for r in d["rs"] if r["kind"] == "loot"]
    R += [
        (C6, "DÜŞEN politikası (R dusen)", "", f6(lambda d, b: (None, ",".join(f"{k}:{v}" for k, v in sorted(Counter(r.get("dusen", "-") for r in d["rs"]).items())))), False, ""),
        (C6, "düşen (son raporda d) / harekât", "", f6(lambda d, b: per_c(d, "fallen")), False, ""),
        (C6, "  / baskın", "", f6(lambda d, b: t_mean([num(r, "fallen") for r in d["rs"]])), False, ""),
        (C6, "taşınan (sürüklenerek çıkarılan) / harekât", "", f6(lambda d, b: per_c(d, "carried")), False, ""),
        # Aşama 6 kapanışı: carried son nefesle çıkarılanı (bsaved) da içerir; anahtar yoksa (önceki çıktı) "-"
        (C6, "  bunun sürüklenerek (carried − bsaved)", "", fb(lambda d, b: t_num(sum(num(r, "carried") - num(r, "bsaved") for r in d["rs"]) / max(1, len(d["cs"])), 2)), False, ""),
        (C6, "  bunun son nefesle (bsaved)", "", fb(lambda d, b: per_c(d, "bsaved")), False, ""),
        (C6, "temiz odadan toplanan / harekât", "", f6(lambda d, b: per_c(d, "collected")), False, ""),
        (C6, "taşınmayan (öldü) / harekât", "", f6(lambda d, b: per_c(d, "abandoned")), False, ""),
        (C6, "son nefes gövdesi (oda başına, breath) / harekât", "", fb(lambda d, b: per_c(d, "breath")), False, ""),
        (C6, "(taşınan + toplanan; son nefes dahil) / harekât ÷ taban kurtarılan / harekât", "≥ %80", f6(vs_base), False, ""),
        (C6, "(taşınan + toplanan) / düşen", "", f6(lambda d, b: (lambda n: (100 * saved(d) / n, f"%{100 * saved(d) / n:.1f}") if n else None)(
            sum(num(r, "fallen") for r in d["rs"]))), False, ""),
        (C6, "düşeni olan baskında bütün düşenler çıkarıldı", "", f6(lambda d, b: t_pct([r for r in d["rs"] if r["fallen"] != "0"],
                                                                                    lambda r: r["abandoned"] == "0")), False, "p"),
    ]
    for k, nm in (("pusu", "pusu"), ("kosu", "koşu darbesi"), ("surukle", "sürükleme"), ("firlat", "fırlatma"), ("ortak", "iki kişilik iş"), ("menfez", "menfez")):
        R.append((C6, f"eylem / baskın: {nm}", "", (lambda k: f6(lambda d, b: ax(d, k)))(k), False, ""))
    R += [
        (C6, "  kullanılan baskın: pusu", "", f6(lambda d, b: t_pct(d["rs"], lambda r: "pusu:0" not in r.get("ax", ""))), False, "p"),
        (C6, "pusu isabeti / baskın (amb)", "", f6(lambda d, b: t_mean([num(r, "amb") for r in d["rs"]])), False, ""),
        (C6, "  isabet / kurulan pusu", "", f6(lambda d, b: (lambda n: t_num(sum(num(r, "amb") for r in d["rs"]) / n, 2) if n else None)(ax(d, "pusu")[0] * len(d["rs"]))), False, ""),
        (C6, "kepenk açıldı (shut), tüm baskın", "", f6(lambda d, b: t_pct(d["rs"], lambda r: r["shut"] == "1")), False, "p"),
        (C6, "  kepenkli ara oda (part=1) baskınlarında", "", f6(lambda d, b: t_pct([r for r in d["rs"] if r["part"] == "1"], lambda r: r["shut"] == "1")), False, "p"),
        (C6, "büyük sandık açıldı (crate), ikmal baskını", "", f6(lambda d, b: t_pct(loot(d), lambda r: r["crate"] == "1")), False, "p"),
        (C6, "yük yere düştü / ikmal baskını (cdrop)", "", f6(lambda d, b: t_mean([num(r, "cdrop") for r in loot(d)])), False, ""),
        (C6, "  yerden alındı (cpick)", "", f6(lambda d, b: t_mean([num(r, "cpick") for r in loot(d)])), False, ""),
        (C6, "bütün ekibin düştüğü baskın (end=ekip)", "", lambda d, b: t_pct(d["rs"], lambda r: r.get("end") == "ekip"), True, "p"),
    ]
    # Aşama 6 kapanışı: düşenin ve geride kalanın sonu (R fate, Camp8.Fates): kategori / harekât ve payı; "b" bırakıldığı odaya göre
    fates = lambda d: [f for r in d["rs"] for f in r.get("fate", "-").split(",") if f != "-"]

    def fate(pred):
        def f(d, b):
            xs = fates(d)
            n = sum(1 for x in xs if pred(x))
            return t_num(n / max(1, len(d["cs"])), 2)[0], f"{n / max(1, len(d['cs'])):.2f} (%{pc(n, len(xs)):.0f})"
        return fb(f)

    for k, nm in (("c", "sürüklenerek çıkarıldı"), ("n", "son nefesle çıkarıldı"), ("k", "temiz odadan toplandı"), ("x", "baskın kaybedildi"),
                  ("b", "ayaktakiler varken bırakıldı"), ("h", "odada ayakta kimse kalmadı"), ("l", "geride kaldı (ayakta)")):
        R.append((C6, f"son: {nm} / harekât (payı)", "", (lambda k: fate(lambda x: x[0] == k))(k), False, ""))
        if k == "b":
            for room in ROOMS:
                R.append((C6, f"  bırakıldığı oda {room}", "", (lambda room: fate(lambda x: x[0] == "b" and x[2] == room))(room), False, ""))
    # Aşama 8 (ekipman, ödül, kazanım): R satırının odul, arac, gear, rec, carry, gx, gfx, dogSteps, capSteps; C satırının rwTarif,
    # rwKazanim, recipes, perks, made_<tür>, orders, ordersDraftLeft, gearLost, clampSpent, dogRest, gearUsed anahtarları; yoksa "-"
    C8 = "aşama 8: ekipman ve ödül"
    f8 = lambda fn: (lambda d, b: fn(d, b) if any("gx" in r for r in d["rs"]) else None)
    gx = lambda r: {k: int(v) for k, v in (it.split(":") for it in r.get("gx", "").split("/") if ":" in it)}
    gfx = lambda r: {k: int(v) for k, v in (it.split(":") for it in r.get("gfx", "").split("/") if ":" in it)}
    # Tür başına kullanım (eylem sayısı): örme seti tel + kalas, köpek havla + getir + kokla
    USE = {"wire_kit": ("wire", "plank"), "charge": ("charge",), "turret": ("turret",), "decoy": ("decoy",), "winch": ("winch",),
           "clamp": ("clamp",), "dog": ("bark", "fetch", "sniff")}
    use = lambda r, k: sum(gx(r).get(a, 0) for a in USE[k])
    rec = lambda d, k: [r for r in d["rs"] if k in r.get("rec", "-").split("+")]
    carried = lambda r, k: k in r.get("carry", "").split("/")
    deadr = lambda d, b: (lambda n: t_num(sum(float(r["dead"]) for r in d["cs"]) / n, 3) if n else None)(len(d["rs"]))
    R += [
        (C8, "ÖDÜL / ARAÇ / GEAR politikası (R odul, arac, gear)", "", f8(lambda d, b: (None, ",".join(f"{k}:{v}" for k, v in sorted(Counter(
            r.get("odul", "-") + "/" + r.get("arac", "-") + "/" + r.get("gear", "-") for r in d["rs"]).items())))), False, ""),
        (C8, "ölü / baskın (C dead ÷ baskın)", "t6sn 0,71", deadr, True, ""),
        (C8, "emirde en az bir ekipman tarifi açık (rec)", "", f8(lambda d, b: t_pct(d["rs"], lambda r: r.get("rec", "-") != "-")), False, "p"),
        (C8, "  kadroda en az bir ekipman (carry)", "", f8(lambda d, b: t_pct(d["rs"], lambda r: any(c != "-" for c in r.get("carry", "-").split("/")))), False, "p"),
    ]
    for k in USE:
        R += [
            (C8, f"{k}: tarifi açık baskında kullanıldı", "≥ %10", (lambda k: f8(lambda d, b: t_pct(rec(d, k), lambda r: use(r, k) > 0)))(k), False, "p"),
            (C8, f"  tarifi açık baskında taşındı", "", (lambda k: f8(lambda d, b: t_pct(rec(d, k), lambda r: carried(r, k))))(k), False, "p"),
            (C8, f"  taşındığı baskında kullanıldı", "", (lambda k: f8(lambda d, b: t_pct([r for r in d["rs"] if carried(r, k)], lambda r: use(r, k) > 0)))(k), False, "p"),
            (C8, f"  kullanım / taşındığı baskın", "", (lambda k: f8(lambda d, b: t_mean([use(r, k) for r in d["rs"] if carried(r, k)])))(k), False, ""),
        ]
    for a in ("wire", "plank", "charge", "turret", "decoy", "winch", "clamp", "bark", "fetch", "sniff", "command"):
        R.append((C8, f"eylem / baskın: {a}", "", (lambda a: f8(lambda d, b: t_mean([gx(r).get(a, 0) for r in d["rs"]])))(a), False, ""))
    for a, nm in (("thit", "taret isabeti"), ("wpull", "vinç çekişi"), ("dstep", "yem adımı"), ("cap", "ele geçirme"), ("rel", "tutsak bırakıldı"),
                  ("dogdown", "köpek düştü"), ("scrap", "aygıt hurdaya döndü"), ("brk", "aygıt kırıldı"), ("plate", "tutsağın verdiği plaka")):
        R.append((C8, f"etki / baskın: {nm}", "", (lambda a: f8(lambda d, b: t_mean([gfx(r).get(a, 0) for r in d["rs"]])))(a), False, ""))
    R += [
        (C8, "köpek adımı / baskın (dogSteps)", "", f8(lambda d, b: t_mean([num(r, "dogSteps") for r in d["rs"]])), False, ""),
        (C8, "tutsak adımı / baskın (capSteps)", "", f8(lambda d, b: t_mean([num(r, "capSteps") for r in d["rs"]])), False, ""),
        (C8, "ödül: tarif; kazanım / harekât", "", f8(lambda d, b: (None, f"{sum(float(r.get('rwTarif', 0)) for r in d['cs']) / max(1, len(d['cs'])):.2f}; "
                                                                   f"{sum(float(r.get('rwKazanim', 0)) for r in d['cs']) / max(1, len(d['cs'])):.2f}")), False, ""),
        (C8, "açık ekipman tarifi sayısı (harekât sonu; dağılım)", "", f8(lambda d, b: (None, ",".join(f"{k}:{v}" for k, v in sorted(Counter(
            sum(1 for x in r.get("recipes", "-").split("+") if x in USE) for r in d["cs"]).items())))), False, ""),
        (C8, "iş emri / harekât (orders); tamamlanmayan", "", f8(lambda d, b: (None, f"{sum(float(r.get('orders', 0)) for r in d['cs']) / max(1, len(d['cs'])):.2f}; "
                                                                             f"{sum(float(r.get('ordersDraftLeft', 0)) for r in d['cs']) / max(1, len(d['cs'])):.2f}")), False, ""),
        (C8, "üretilen ekipman / harekât (made_)", "", f8(lambda d, b: t_num(sum(float(v) for r in d["cs"] for k, v in r.items() if k.startswith("made_")) / max(1, len(d["cs"])), 2)), False, ""),
    ]
    for k in USE:
        R.append((C8, f"  {k}", "", (lambda k: f8(lambda d, b: t_num(sum(float(r.get("made_" + k, 0)) for r in d["cs"]) / max(1, len(d["cs"])), 2)))(k), False, ""))
    R += [
        (C8, "ölenle silinen ekipman / harekât (gearLost); tükenen kelepçe", "", f8(lambda d, b: (None, f"{sum(float(r.get('gearLost', 0)) for r in d['cs']) / max(1, len(d['cs'])):.2f}; "
                                                                                              f"{sum(float(r.get('clampSpent', 0)) for r in d['cs']) / max(1, len(d['cs'])):.2f}")), False, ""),
        (C8, "köpeğin dinlendiği vardiya / harekât (dogRest)", "", f8(lambda d, b: t_num(sum(float(r.get("dogRest", 0)) for r in d["cs"]) / max(1, len(d["cs"])), 2)), False, ""),
        (C8, "harekâtta kullanılan tür sayısı (gearUsed; dağılım)", "", f8(lambda d, b: (None, ",".join(f"{k}:{v}" for k, v in sorted(Counter(
            0 if r.get("gearUsed", "-") == "-" else len(r["gearUsed"].split("+")) for r in d["cs"]).items())))), False, ""),
        (C8, "seçilen kazanımlar (harekât sonu, perks)", "", f8(lambda d, b: (None, ",".join(f"{k}:{v}" for k, v in Counter(
            x for r in d["cs"] for x in r.get("perks", "-").split("+") if x != "-").most_common()) or "-")), False, ""),
    ]
    # Aşama 10 (önlem defteri): R satırının cm, tl, tlb, learn, ans, cmfx, plates, sqHp, sqMv, sqPw, rook, half; C satırının learns,
    # cmEnd, nLearn, rwCevap, prod_<tür>, cmRaids_<sınıf>_<k>, penCarry, carryItems anahtarları; yoksa "-"
    C10 = "aşama 10: önlem defteri"
    CL = ("itme", "yikim", "orme", "ele", "kopek", "aygit")
    f10 = lambda fn: (lambda d, b: fn(d, b) if any("cm" in r for r in d["rs"]) else None)
    levels = lambda r: [int(it.split(":")[1]) for it in r.get("cm", "-").split("/")] if r.get("cm", "-") != "-" else [0] * 6
    six = lambda r, k: [int(v) for v in r.get(k, "-").split("/")] if r.get(k, "-") != "-" else None
    learns = lambda r: [tuple(it.split(":")) for it in r.get("learns", "-").split(",") if it != "-"]
    cmfx = lambda r: {k: int(v) for k, v in (it.split(":") for it in r.get("cmfx", "").split("/") if ":" in it)}
    per_h = lambda d, f: t_num(sum(f(r) for r in d["cs"]) / max(1, len(d["cs"])), 2)
    # Ekipmanın önlem sınıfları (Camp8 GearClasses): vinç aygıt ve itme
    GCL = {"wire_kit": (2,), "charge": (1,), "turret": (5,), "decoy": (5,), "winch": (5, 0), "clamp": (3,), "dog": (4,)}
    glevel = lambda r, k: max(levels(r)[i] for i in GCL[k])
    empty = lambda r: r.get("rec", "-") != "-" and all(c == "-" for c in r.get("carry", "-").split("/"))
    full = lambda r: any(c != "-" for c in r.get("carry", "-").split("/"))
    R += [
        (C10, "önlem / harekât (nLearn)", "", f10(lambda d, b: per_h(d, lambda r: _num(r, "nLearn"))), False, ""),
        (C10, "  önlem sayısı dağılımı (harekât)", "", f10(lambda d, b: (None, ",".join(f"{k}:{v}" for k, v in sorted(Counter(
            int(_num(r, "nLearn")) for r in d["cs"]).items())))), False, ""),
        (C10, "  önlemsiz harekât", "", f10(lambda d, b: t_pct(d["cs"], lambda r: _num(r, "nLearn") == 0)), False, "p"),
    ]
    for i, c in enumerate(CL):
        R.append((C10, f"  {c}: önlem / harekât; 2. kademe / harekât", "", (lambda c: f10(lambda d, b: (None,
            f"{sum(1 for r in d['cs'] for x in learns(r) if x[0] == c) / max(1, len(d['cs'])):.2f}; "
            f"{sum(1 for r in d['cs'] for x in learns(r) if x[0] == c and x[1] == '2') / max(1, len(d['cs'])):.2f}")))(c), False, ""))
    R += [
        (C10, "ilk önlemin sınıfı (önlemli harekâtın payı)", "", f10(lambda d, b: (None, ",".join(f"{k}:%{pc(v, sum(1 for r in d['cs'] if learns(r))):.0f}"
            for k, v in Counter(learns(r)[0][0] for r in d["cs"] if learns(r)).most_common()) or "-")), False, ""),
        (C10, "ilk öğrenme günü (medyan; ort)", "", f10(lambda d, b: (lambda xs: (None, f"{st.median(xs):.0f}; {st.mean(xs):.1f}") if xs else None)(
            [int(learns(r)[0][2]) for r in d["cs"] if learns(r)])), False, ""),
        (C10, "önlemli baskın (en az bir sınıf ≥ 1, emirde)", "", f10(lambda d, b: t_pct(d["rs"], lambda r: max(levels(r)) >= 1)), False, "p"),
    ]
    for i, c in enumerate(CL):
        R.append((C10, f"  {c} k1 / k2 baskın payı", "", (lambda i: f10(lambda d, b: (None,
            f"%{pc(sum(1 for r in d['rs'] if levels(r)[i] == 1), len(d['rs'])):.1f} / %{pc(sum(1 for r in d['rs'] if levels(r)[i] == 2), len(d['rs'])):.1f}")))(i), False, ""))
    R += [
        (C10, "cezalı taşıma (penCarry / carryItems, eşya-baskın)", "tek > donus", f10(lambda d, b: (lambda n, k: (pc(k, n), f"%{pc(k, n):.1f} ({n:.0f})") if n else None)(
            sum(_num(r, "carryItems") for r in d["cs"]), sum(_num(r, "penCarry") for r in d["cs"]))), False, "p"),
        (C10, "Core sayımı / baskın (tl, tavanlı; sınıf sırası)", "", f10(lambda d, b: (lambda xs: (None, "/".join(f"{sum(x[i] for x in xs) / len(xs):.2f}" for i in range(6))) if xs else None)(
            [six(r, "tl") for r in d["rs"] if six(r, "tl")])), False, ""),
        (C10, "tutarlılık: tl ≠ tlb baskın", "0", f10(lambda d, b: (lambda xs: (None, f"{sum(1 for r in xs if r['tl'] != r['tlb'])} / {len(xs)}"))(
            [r for r in d["rs"] if r.get("tl", "-") != "-"])), False, ""),
        (C10, "öğrenme baskını (learn; dağılım)", "", f10(lambda d, b: (None, ",".join(f"{k}:{v}" for k, v in sorted(Counter(
            r.get("learn", "-") for r in d["rs"] if r.get("learn", "-") != "-").items())) or "-")), False, ""),
        (C10, "cevap açık baskın (ans)", "", f10(lambda d, b: t_pct(d["rs"], lambda r: r.get("ans", "-") != "-")), False, "p"),
        (C10, "  cevaplar (baskın sayısı)", "", f10(lambda d, b: (None, ",".join(f"{k}:{v}" for k, v in Counter(
            x for r in d["rs"] for x in r.get("ans", "-").split("+") if x != "-").most_common()) or "-")), False, ""),
        (C10, "seçilen cevap tarifi / harekât (rwCevap)", "", f10(lambda d, b: per_h(d, lambda r: _num(r, "rwCevap"))), False, ""),
    ]
    for a, nm in (("bolt", "cıvata direndi"), ("unbolt", "Söküm söktü"), ("dwall", "çift cidar yıkıldı"), ("cut", "kesici kol ek hasar"),
                  ("jam", "parazit kısalttı"), ("ebrk", "yayıcı kırıldı"), ("emit", "yayıcının susturduğu köpek-tur"), ("mpull", "vincin makine çekişi")):
        R.append((C10, f"önlem etkisi / baskın: {nm}", "", (lambda a: f10(lambda d, b: t_mean([cmfx(r).get(a, 0) for r in d["rs"]], 3)))(a), False, ""))
    for k in USE:
        R.append((C10, f"{k}: tarifi açık baskında kullanıldı, önlem k0 / k1 / k2", "", (lambda k: f10(lambda d, b: (None, " / ".join(
            (lambda g: f"%{pc(sum(1 for r in g if use(r, k) > 0), len(g)):.1f} ({len(g)})" if g else "-")([r for r in rec(d, k) if glevel(r, k) == lv])
            for lv in (0, 1, 2)))))(k), False, ""))
    for key, nm in (("plates", "plaka"), ("sqHp", "can"), ("sqMv", "yürüyüş"), ("sqPw", "güç"), ("rook", "çaylak"), ("half", "yarım tayın")):
        R.append((C10, f"kadro {nm}: tarif açık-kadro boş / kadroda eşya / tarifsiz", "", (lambda key: f10(lambda d, b: (None, " / ".join(
            f"{st.mean(_num(r, key) for r in g):.2f}" if g else "-" for g in ([r for r in d['rs'] if empty(r)], [r for r in d['rs'] if full(r)],
                                                                          [r for r in d['rs'] if r.get('rec', '-') == '-'])))))(key), False, ""))
    R += [
        (C10, "başarı: tarif açık-kadro boş / kadroda eşya / tarifsiz", "", f10(lambda d, b: (None, " / ".join(
            f"%{pc(sum(1 for r in g if ok(r)), len(g)):.1f} ({len(g)})" if g else "-" for g in ([r for r in d['rs'] if empty(r)], [r for r in d['rs'] if full(r)],
                                                                                          [r for r in d['rs'] if r.get('rec', '-') == '-'])))), False, ""),
        (C10, "ekipman dışı üretim / harekât (prod_)", "", f10(lambda d, b: (None, ",".join(f"{k[5:]}:{v / max(1, len(d['cs'])):.2f}" for k, v in sorted(
            sum((Counter({k: _num(r, k)}) for r in d["cs"] for k in r if k.startswith("prod_")), Counter()).items())) or "-")), False, ""),
    ]
    R += tesis11_rows()
    R += tesis12_rows()
    return R


def tesis11_rows():
    # Aşama 11 (kalıcı tahta ve savunma): S satırları (savunma başına), C satırının kuşatma/iz anahtarları, R satırının retry/mk/mkNew;
    # anahtar yoksa "-". Savunma başarısının SE'si tohum kümelidir (savunmalı tohumların oranı).
    C11 = "aşama 11: kalıcı tahta ve savunma"
    f11 = lambda fn: (lambda d, b: fn(d, b) if any("siege" in r for r in d["cs"]) else None)
    per_h = lambda d, k: t_num(sum(_num(r, k) for r in d["cs"]) / max(1, len(d["cs"])), 2)
    ok = lambda r: r["ok"] == "1"
    S = lambda d: d.get("ss", [])

    def rate(g):
        return (pc(sum(1 for r in g if ok(r)), len(g)), f"%{pc(sum(1 for r in g if ok(r)), len(g)):.1f} ({len(g)})") if g else None

    def rate_se(d, b):
        g = S(d)
        if not g:
            return None
        by = {}
        for r in g:
            by.setdefault(r["seed"], []).append(1 if ok(r) else 0)
        v = [100 * st.mean(x) for x in by.values()]
        m = pc(sum(1 for r in g if ok(r)), len(g))
        se = st.stdev(v) / len(v) ** .5 if len(v) > 1 else 0
        return (m, f"%{m:.1f} ±{se:.1f} ({len(g)}; {len(v)} tohum)")

    def smean(k, pred=lambda r: True, dd=2):
        return lambda d, b: (lambda g: t_mean([float(r[k]) for r in g if r.get(k, "-") != "-"], dd) if g else None)([r for r in S(d) if pred(r)])

    def ledger(d, b):
        bad = sum(1 for r in d["cs"] if "marksEnd" in r and _num(r, "marksMade") - _num(r, "marksGone") - _num(r, "marksRepaired")
                  - _num(r, "marksWiped") != _num(r, "marksEnd"))
        return (bad, f"{bad} / {sum(1 for r in d['cs'] if 'marksEnd' in r)}")

    mk_sum = lambda r: sum(per_room(r.get("mk", "")).values())
    retry = lambda d: [r for r in d["rs"] if r.get("retry") == "1"]
    a_turns = lambda g: t_mean([per_room(r.get("rt", ""))["A"] for r in g if "A" in per_room(r.get("rt", ""))], 2) if g else None
    R = [
        (C11, "kuşatma / harekât (siege)", "", f11(lambda d, b: per_h(d, "siege")), True, ""),
        (C11, "  savun / bırak / kendiliğinden / kanatsız anlık", "", f11(lambda d, b: (None, " / ".join(
            f"{sum(_num(r, k) for r in d['cs']) / max(1, len(d['cs'])):.2f}" for k in ("defend", "yield", "autoYield", "siegeNone")))), False, ""),
        (C11, "  savunma reddi (defNoSquad) / harekât", "", f11(lambda d, b: per_h(d, "defNoSquad")), False, ""),
        (C11, "  baskın reddi: karar vardiyası / bırakılan kanat", "", f11(lambda d, b: (None, " / ".join(
            f"{sum(_num(r, k) for r in d['cs']) / max(1, len(d['cs'])):.2f}" for k in ("nrSiege", "nrYield")))), False, ""),
        (C11, "  kaynak: vardiya / baskın / olay / kapı (savunulan)", "", f11(lambda d, b: (None, " / ".join(
            str(sum(1 for r in S(d) if r.get("src") == k)) for k in ("vardiya", "baskin", "olay", "kapi")))), False, ""),
        (C11, "  kanat başka bölgede (same=0, savunulan)", "", f11(lambda d, b: t_pct(S(d), lambda r: r.get("same") == "0")), False, "p"),
        (C11, "savunma başarısı (S ok; tohum kümeli SE)", "%40–60", f11(rate_se), True, "p"),
    ]
    for t in ("1", "2", "3", "4"):
        R.append((C11, f"  kademe {t}", "", (lambda t: f11(lambda d, b: rate([r for r in S(d) if r["tier"] == t])))(t), False, "p"))
    for l in ("0", "1", "2"):
        R.append((C11, f"  terk {l}", "", (lambda l: f11(lambda d, b: rate([r for r in S(d) if r["left"] == l])))(l), False, "p"))
    R += [
        (C11, "  bitiş odası G / A / H", "", f11(lambda d, b: (None, " / ".join(str(sum(1 for r in S(d) if r.get("endRoom") == k)) for k in "GAH"))), False, ""),
        (C11, "güven / savunma: tutulan; düşen", "0/−1/−2; −1/−2/−3", f11(lambda d, b: (None, "; ".join(
            (lambda g: f"{st.mean(float(r['trust']) for r in g):+.2f} ({len(g)})" if g else "-")([r for r in S(d) if r["ok"] == w]) for w in ("1", "0")))), False, ""),
        (C11, "  terk başına güven (tutulan, terk 0/1/2)", "", f11(lambda d, b: (None, " / ".join(
            (lambda g: f"{st.mean(float(r['trust']) for r in g):+.2f}" if g else "-")([r for r in S(d) if r["ok"] == "1" and r["left"] == l]) for l in "012"))), False, ""),
        (C11, "  −3'lük kayıp (defLoss2) / harekât; sonraki 5 vardiyada dağılma", "", f11(lambda d, b: (None,
            f"{sum(_num(r, 'defLoss2') for r in d['cs']) / max(1, len(d['cs'])):.3f}; {sum(_num(r, 'defLossDag5') for r in d['cs']):.0f}")), False, ""),
        (C11, "  dağılmada son darbe savunma / birak", "", f11(lambda d, b: (None, " / ".join(
            str(sum(1 for r in d["cs"] if r.get("lastHit") == k)) for k in ("savunma", "birak")))), False, ""),
        (C11, "savunma başına düşen / ölü / çıkarılan", "", f11(lambda d, b: (lambda g: (None, " / ".join(
            f"{st.mean(float(r[k]) for r in g):.2f}" for k in ("down", "died", "extracted"))) if g else None)(S(d))), False, ""),
        (C11, "  savunma ölüsü / harekât (defDeaths)", "", f11(lambda d, b: per_h(d, "defDeaths")), True, ""),
        (C11, "savunma turu; oyuncu turu; uzman hamlesi", "", f11(lambda d, b: (lambda g: (None, "; ".join(
            f"{st.mean(float(r[k]) for r in g):.2f}" for k in ("turns", "plTurns")) + f"; {st.mean(float(r['moves']) + float(r['acts']) for r in g):.2f}") if g else None)(S(d))), False, ""),
        (C11, "dalga / gelen makine / düşürülen", "", f11(lambda d, b: (lambda g: (None, " / ".join(
            f"{st.mean(float(r[k]) for r in g):.2f}" for k in ("waves", "wm", "kills"))) if g else None)(S(d))), False, ""),
        (C11, "röle: son can; isabet (H'ye girilen savunma)", "", f11(lambda d, b: (lambda g: (None,
            f"{st.mean(float(r['relay']) for r in g):.2f}; {st.mean(float(r['relayHit']) for r in g):.2f} ({len(g)})") if g else None)(
            [r for r in S(d) if r.get("relay", "-") != "-"])), False, ""),
        (C11, "  Siper açık savunma", "", f11(lambda d, b: t_pct(S(d), lambda r: r.get("perkSiper") == "1")), False, "p"),
        (C11, "  barikat/engel kullanımı (S gx wire+plank / savunma)", "", f11(lambda d, b: (lambda g: t_mean([_gx(r).get("wire", 0) + _gx(r).get("plank", 0) for r in g], 2) if g else None)(S(d))), False, ""),
        (C11, "  sayım tutarlılığı: S tl ≠ tlb", "0", f11(lambda d, b: (None, f"{sum(1 for r in S(d) if r.get('tl', '-') != r.get('tlb', '-'))} / {len(S(d))}")), False, ""),
        (C11, "yeniden alma baskını / harekât (recapRaids)", "", f11(lambda d, b: per_h(d, "recapRaids")), True, ""),
        (C11, "  yeniden tamamlanan bölge; verilmeyen (recapDenied) / harekât", "", f11(lambda d, b: (None,
            f"{sum(_num(r, 'recapRegions') for r in d['cs']) / max(1, len(d['cs'])):.3f}; {sum(_num(r, 'recapDenied') for r in d['cs']) / max(1, len(d['cs'])):.3f}")), False, ""),
        (C11, "  verilen fon (recapFunds) / harekât", "", f11(lambda d, b: per_h(d, "recapFunds")), False, ""),
        (C11, "izler / harekât: yapılan; onarılan; terkle silinen; kırılan; son", "", f11(lambda d, b: (None, "; ".join(
            f"{sum(_num(r, k) for r in d['cs']) / max(1, len(d['cs'])):.2f}" for k in ("marksMade", "marksRepaired", "marksWiped", "marksGone", "marksEnd")))), False, ""),
        (C11, "  emirde görülen iz / baskın (R mk)", "", f11(lambda d, b: t_mean([mk_sum(r) for r in d["rs"]], 2)), False, ""),
        (C11, "  I3 defter: tutmayan harekât", "0", f11(ledger), False, ""),
        (C11, "tekrar baskını (retry): izli / izsiz başarı", "", f11(lambda d, b: (None, " / ".join(
            (lambda g: f"%{pc(sum(1 for r in g if r['ok'] == '1'), len(g)):.1f} ({len(g)})" if g else "-")([r for r in retry(d) if (mk_sum(r) > 0) == m]) for m in (True, False)))), False, ""),
        (C11, "  tekrar baskını A'da tur: izli / izsiz", "", f11(lambda d, b: (None, " / ".join(
            (lambda v: v[1] if v else "-")(a_turns([r for r in retry(d) if (mk_sum(r) > 0) == m])) for m in (True, False)))), False, ""),
        (C11, "  tekrar baskını / harekât; geride kalan (stuck) / tekrar", "", f11(lambda d, b: (None,
            f"{len(retry(d)) / max(1, len(d['cs'])):.2f}; {st.mean(float(r.get('stuck', 0)) for r in retry(d)) if retry(d) else 0:.3f}")), False, ""),
        (C11, "karşı saldırı kapı yolu (counterDoor) / harekât", "", f11(lambda d, b: per_h(d, "counterDoor")), False, ""),
    ]
    return R


# ---- tesis baskınları aşama 12 (Katar ve Yalpa) ----
# Aşama 12 anahtarları (kilitte ve önek karşılaştırmasında atılır)
K12 = ("ayir", "kenar", "det", "detNo", "fol", "folRooms", "edgeM", "edgeP", "edgeX", "edgeSrc", "eDied", "eInj", "tilt", "slide",
       "tiltHit", "evP", "evE", "katarRaids", "yalpaRaids", "fellInj", "slides", "tiltHitP")
MISS12 = ("tek", "acted", "gate", "pol", "auto")
SLIDE12 = ("p", "e", "govde", "yuk", "hurda", "diger")
SRC12 = ("emp", "sab", "kosu", "evre", "diger")
TILT_CAP = {"G": 0, "A": 2, "H": 3, "a": 2, "g": 0}


def slash(s):
    out = Counter()
    for it in filter(None, (s or "").split("/")):
        if ":" in it:
            k, v = it.split(":", 1)
            out[k] += int(v)
    return out


def room_items(s):
    # "G0/A1/H2/a0" → [("G", 0), ("A", 1), …] (her ziyaret ayrı)
    return [(it[0], int(it[1:])) for it in filter(None, (s or "").split("/")) if it[1:].lstrip("-").isdigit()]


def fates(r):
    return [f for f in (r.get("fate", "-") or "-").split(",") if f and f != "-"]


def succ_se(g):
    if not g:
        return None
    by = {}
    for r in g:
        by.setdefault(r["seed"], []).append(1 if r["ok"] == "1" else 0)
    v = [100 * st.mean(x) for x in by.values()]
    m = pc(sum(1 for r in g if r["ok"] == "1"), len(g))
    se = st.stdev(v) / len(v) ** .5 if len(v) > 1 else 0
    return (m, f"%{m:.1f} ±{se:.1f} ({len(g)}; {len(v)} tohum)")


def yapi12(d):
    """Aşama 12 yapı denetimleri (sayılar; hepsi 0 olmalı)."""
    rs, ss = d["rs"], d.get("ss", [])
    na = [r for r in rs if r["region"] == "na"]
    oc = [r for r in rs if r["region"] == "oc"]
    gEdge = sum(1 for r in na for k in ("edgeM", "edgeP") for (x, n) in room_items(r.get(k)) if x in "Gg" and n > 0)
    defense = sum(int(r.get(k, 0) or 0) for r in ss for k in ("edgeM", "edgeP", "fol", "tilt"))
    sched = sum(1 for r in oc for (x, n) in room_items(r.get("tilt")) if n > TILT_CAP.get(x, 0))
    tl = sum(1 for r in oc if r.get("tl", "-") != r.get("tlb", "-"))
    tlAll = sum(1 for r in rs if r.get("tl", "-") != r.get("tlb", "-"))
    eDied = sum(int(r.get("eDied", 0)) for r in rs)
    eFate = sum(1 for r in rs if sum(1 for f in fates(r) if f[0] == "e") != sum(n for _, n in room_items(r.get("edgeP"))))
    inj = sum(1 for r in rs if "eInj" in r and int(r["eInj"]) != 5 * sum(n for _, n in room_items(r.get("edgeP"))))
    outside = sum(1 for r in rs if r["region"] != "na" and (sum(n for _, n in room_items(r.get("edgeM"))) + sum(n for _, n in room_items(r.get("edgeP")))
                                                           + int(r.get("det", 0) or 0) + sum(slash(r.get("detNo")).values()) + int(r.get("fol", 0) or 0)))
    outside += sum(1 for r in rs if r["region"] != "oc" and sum(n for _, n in room_items(r.get("tilt"))) > 0)
    return {"G/g'de kenar olayı": gEdge, "savunmada kenar/takip/yatış": defense, "takvim dışı yatış (oda)": sched,
            "oc'de tl ≠ tlb": tl, "tüm baskınlarda tl ≠ tlb": tlAll, "kenardan düşüp ölen (eDied)": eDied,
            "fate e ≠ edgeP (baskın)": eFate, "yatış ≠ 5 × kenardan düşen (baskın)": inj, "şartın bölgesi dışında olay (baskın)": outside}


def tesis12_rows():
    CK = "aşama 12: Katar (na; tabanda asansör)"
    CY = "aşama 12: Yalpa (oc; tabanda yağ)"
    CS = "aşama 12: yapı denetimleri"
    NA = lambda d: [r for r in d["rs"] if r["region"] == "na"]
    OC = lambda d: [r for r in d["rs"] if r["region"] == "oc"]
    f12 = lambda fn: (lambda d, b: fn(d, b) if d["fac"] else None)
    k12 = lambda fn: (lambda d, b: fn(d, b) if d["fac"] and any("det" in r for r in d["rs"]) else None)
    per_raid = lambda g, f, dd=2: t_mean([f(r) for r in g], dd) if g else None
    rsum = lambda r, k: sum(n for _, n in room_items(r.get(k)))
    chance = lambda r: int(r.get("det", 0) or 0) + sum(slash(r.get("detNo")).values())

    def count_ev(g):
        c = Counter(r.get("event", "-") for r in g)
        return ",".join(f"{k}:{v}" for k, v in sorted(c.items()))

    def per_h(d, g):
        return (len(g) / max(1, len(d["cs"])), f"{len(g) / max(1, len(d['cs'])):.2f}; {count_ev(g)}")

    def rooms_played(g, key):
        return t_pct(g, lambda r: any(x == key for x, _ in room_items(r.get("rt"))))

    def miss_share(g):
        tot = Counter()
        for r in g:
            tot += slash(r.get("detNo"))
        n = sum(tot.values()) + sum(int(r.get("det", 0) or 0) for r in g)
        return (pc(tot["acted"], n), " / ".join(f"{k}:{tot[k]}" for k in MISS12) + f" (fırsat {n}; acted %{pc(tot['acted'], n):.1f})")

    def det_rate(g):
        n = sum(chance(r) for r in g)
        dt = sum(int(r.get("det", 0) or 0) for r in g)
        return (pc(dt, n), f"{n / max(1, len(g)):.2f}; %{pc(dt, n):.1f} ({dt}/{n})")

    def fol_rate(g):
        n = sum(chance(r) for r in g)
        f = sum(int(r.get("fol", 0) or 0) for r in g)
        fr = sum(int(r.get("folRooms", 0) or 0) for r in g)
        return (f / max(1, n), f"{f / max(1, len(g)):.3f}; {f / max(1, n):.3f}; {fr / max(1, len(g)):.3f}")

    def by_room(g, key):
        c = Counter()
        for r in g:
            for x, n in room_items(r.get(key)):
                c[x] += n
        return (None, " / ".join(f"{x}{c[x]}" for x in ROOMS))

    def src(g):
        c = Counter()
        for r in g:
            c += slash(r.get("edgeSrc"))
        return (None, " / ".join(f"{k}:{c[k]}" for k in SRC12))

    def fate_dist(g):
        c = Counter(f[0] for r in g for f in fates(r))
        return (None, " / ".join(f"{k}:{c[k] / max(1, len(g)):.3f}" for k in "elxbhckn"))

    def inj(g):
        p = sum(rsum(r, "edgeP") for r in g)
        i = sum(int(r.get("eInj", 0) or 0) for r in g)
        e = sum(int(r.get("eDied", 0) or 0) for r in g)
        return (None, f"{i / p:.2f}; {e}" if p else f"-; {e}")

    def itme(g):
        learn = sum(1 for r in g if r.get("learn", "-").startswith("itme"))
        cm = sum(1 for r in g if any(x.startswith("itme:") and x[5:] not in ("0", "") for x in r.get("cm", "-").split("/")))
        bolt = sum(slash(r.get("cmfx")).get("bolt", 0) for r in g)
        return (None, f"{learn} / %{pc(cm, len(g)):.1f} / {bolt / max(1, len(g)):.3f}") if g else None

    def slides(g):
        c = Counter()
        for r in g:
            c += slash(r.get("slide"))
        return (sum(c.values()) / max(1, len(g)), " / ".join(f"{k}:{c[k] / max(1, len(g)):.2f}" for k in SLIDE12))

    def tilt_hit(g):
        c = Counter()
        for r in g:
            c += slash(r.get("tiltHit"))
        return (None, f"{c['p'] / max(1, len(g)):.2f} / {c['e'] / max(1, len(g)):.2f}")

    def ev_dmg(g):
        return (None, f"{st.mean(float(r.get('evP', 0)) for r in g):.2f} / {st.mean(float(r.get('evE', 0)) for r in g):.2f}") if g and "evP" in g[0] else None

    def defense(d, b):
        g = [r for r in d.get("ss", []) if r["region"] in ("na", "oc")]
        if not g:
            return None
        z = sum(int(r.get(k, 0) or 0) for r in g for k in ("edgeM", "edgeP", "fol", "tilt"))
        return (None, f"{len(g)}; %{pc(sum(1 for r in g if r['ok'] == '1'), len(g)):.1f}; şart olayı {z if any('edgeM' in r for r in g) else '-'}")

    R = [
        (CK, "baskın / harekât; olay", "", f12(lambda d, b: per_h(d, NA(d))), False, ""),
        (CK, "başarı (tohum kümeli SE)", "", f12(lambda d, b: succ_se(NA(d))), True, "p"),
        (CK, "ölü / baskın", "", f12(lambda d, b: per_raid(NA(d), lambda r: float(r["died"]), 3)), True, ""),
        (CK, "  düşen; geride kalan (stuck) / baskın", "", f12(lambda d, b: (None, f"{st.mean(float(r['down']) for r in NA(d)):.3f}; "
                                                                               f"{st.mean(float(r.get('stuck', 0)) for r in NA(d)):.3f}") if NA(d) else None), False, ""),
        (CK, "tur; oynanan oda; yürünen oda / baskın", "", f12(lambda d, b: (None, "; ".join(f"{st.mean(float(r[k]) for r in NA(d)):.2f}" for k in ("turns", "rooms", "walked"))) if NA(d) else None), False, ""),
        (CK, "  dönüş odası oynandı: a", "", f12(lambda d, b: rooms_played(NA(d), "a")), True, "p"),
        (CK, "  dönüş odası oynandı: g", "", f12(lambda d, b: rooms_played(NA(d), "g")), True, "p"),
        (CK, "AYIR fırsatı / baskın; AYIR / fırsat", "", k12(lambda d, b: det_rate(NA(d))), False, ""),
        (CK, "  verilmeyen AYIR: tek / acted / gate / pol / auto", "acted ≤ %20", k12(lambda d, b: miss_share(NA(d))), False, ""),
        (CK, "takipçi / baskın; / dönüş geçişi; takipli temiz oda / baskın", "", k12(lambda d, b: fol_rate(NA(d))), False, ""),
        (CK, "kenardan düşen makine / baskın (edgeM)", "", k12(lambda d, b: per_raid(NA(d), lambda r: rsum(r, "edgeM"), 3)), False, ""),
        (CK, "  kaynak emp / sab / kosu / evre / diger", "", k12(lambda d, b: src(NA(d))), False, ""),
        (CK, "  oda G / A / H / a / g", "", k12(lambda d, b: by_room(NA(d), "edgeM")), False, ""),
        (CK, "kenardan düşen uzman / baskın (edgeP)", "", k12(lambda d, b: per_raid(NA(d), lambda r: rsum(r, "edgeP"), 3)), False, ""),
        (CK, "  oda G / A / H / a / g", "", k12(lambda d, b: by_room(NA(d), "edgeP")), False, ""),
        (CK, "  edgeP > 0 olan baskın", "", k12(lambda d, b: t_pct(NA(d), lambda r: rsum(r, "edgeP") > 0)), False, "p"),
        (CK, "  köpek/tutsak/aygıt (edgeX) / baskın", "", k12(lambda d, b: per_raid(NA(d), lambda r: float(r.get("edgeX", 0)), 3)), False, ""),
        (CK, "  yatış / kenardan düşen; kenardan düşüp ölen", "5; 0", k12(lambda d, b: inj(NA(d))), False, ""),
        (CK, "kişi sonu / baskın (fate e/l/x/b/h/c/k/n)", "", f12(lambda d, b: fate_dist(NA(d)) if NA(d) else None), False, ""),
        (CK, "itme: öğrenilen; cm itme ≥ 1; cmfx bolt / baskın", "", f12(lambda d, b: itme(NA(d))), False, ""),
        (CK, "olay hasarı ekip / makine (evP / evE) / baskın", "", k12(lambda d, b: ev_dmg(NA(d))), False, ""),
        (CY, "baskın / harekât; olay", "", f12(lambda d, b: per_h(d, OC(d))), False, ""),
        (CY, "başarı (tohum kümeli SE)", "", f12(lambda d, b: succ_se(OC(d))), True, "p"),
        (CY, "ölü / baskın", "", f12(lambda d, b: per_raid(OC(d), lambda r: float(r["died"]), 3)), True, ""),
        (CY, "  düşen; geride kalan (stuck) / baskın", "", f12(lambda d, b: (None, f"{st.mean(float(r['down']) for r in OC(d)):.3f}; "
                                                                               f"{st.mean(float(r.get('stuck', 0)) for r in OC(d)):.3f}") if OC(d) else None), False, ""),
        (CY, "tur; oynanan oda / baskın", "", f12(lambda d, b: (None, "; ".join(f"{st.mean(float(r[k]) for r in OC(d)):.2f}" for k in ("turns", "rooms"))) if OC(d) else None), False, ""),
        (CY, "yatış / baskın; oda G / A / H / a / g", "", k12(lambda d, b: (None, f"{st.mean(rsum(r, 'tilt') for r in OC(d)):.2f}; {by_room(OC(d), 'tilt')[1]}") if OC(d) else None), False, ""),
        (CY, "kayma / baskın: p / e / govde / yuk / hurda / diger", "", k12(lambda d, b: slides(OC(d)) if OC(d) else None), False, ""),
        (CY, "yatışta çarpan uzman / makine / baskın", "", k12(lambda d, b: tilt_hit(OC(d)) if OC(d) else None), False, ""),
        (CY, "olay hasarı ekip / makine (evP / evE) / baskın", "", k12(lambda d, b: ev_dmg(OC(d))), False, ""),
        (CY, "savunma (na+oc S): sayı; başarı; şart olayı", "0", f12(defense), False, ""),
    ]
    for name in ("G/g'de kenar olayı", "savunmada kenar/takip/yatış", "takvim dışı yatış (oda)", "oc'de tl ≠ tlb", "tüm baskınlarda tl ≠ tlb",
                 "kenardan düşüp ölen (eDied)", "fate e ≠ edgeP (baskın)", "yatış ≠ 5 × kenardan düşen (baskın)", "şartın bölgesi dışında olay (baskın)"):
        R.append((CS, name, "0", (lambda name: k12(lambda d, b: (lambda v: (v, str(v)))(yapi12(d)[name])))(name), False, ""))
    return R


def ilk_table(region, pairs):
    """Bölgenin ilk baskını eşli (aşama 12; asama12/bot/ilk.py mantığı): aynı harekâtın öneki (bölgenin ilk baskınından önceki R/S
    satırları, aşama 12 anahtarları atılarak, etiket eşitlenerek) aynıysa ilk baskının ölçüleri A − B, tohum kümeli SE."""
    import re
    strip = re.compile(r"\t(" + "|".join(K12) + r")=[^\t\n]*")
    def kv(l):
        return dict(x.split("=", 1) for x in l.rstrip("\n").split("\t")[1:] if "=" in x)
    def camps(path, tag):
        out, cur = [], []
        for l in open(path):
            if l[:2] not in ("R\t", "S\t", "C\t"):
                continue
            cur.append((strip.sub("", l).replace("tag=" + tag + "\t", "tag=@\t"), l))
            if l[0] == "C":
                out.append(cur)
                cur = []
        return out
    def first(c):
        for i, (s, l) in enumerate(c):
            if l[0] == "R" and kv(l)["region"] == region:
                return [x[0] for x in c[:i]], kv(l)
        return None, None
    rsum = lambda r, k: float(sum(n for _, n in room_items(r.get(k))))
    M = [("başarı %", lambda r: 100.0 * (r["ok"] == "1")), ("ölü", lambda r: float(r["died"])), ("düşen", lambda r: float(r["down"])),
         ("kurtarılan", lambda r: float(r["rescued"])), ("tur", lambda r: float(r["turns"])), ("oynanan oda", lambda r: float(r.get("rooms", 0))),
         ("geride kalan (stuck)", lambda r: float(r.get("stuck", 0))), ("kırılan sivil", lambda r: float(r["collat"])),
         ("kenardan düşen uzman (edgeP)", lambda r: rsum(r, "edgeP")), ("kenardan düşen makine (edgeM)", lambda r: rsum(r, "edgeM")),
         ("takipçi (fol)", lambda r: float(r.get("fol", 0))), ("AYIR (det)", lambda r: float(r.get("det", 0))),
         ("yatış (tilt)", lambda r: rsum(r, "tilt")), ("olay hasarı ekip (evP)", lambda r: float(r.get("evP", 0))),
         ("fate l (geride kaldı)", lambda r: float(sum(1 for f in fates(r) if f[0] == "l")))]
    for a, b in pairs:
        by = {k: {} for k, _ in M}
        n = valid = 0
        for fa in sorted(glob.glob(os.path.join(root, a, "*-*.txt"))):
            fb = os.path.join(root, b, os.path.basename(fa))
            if not os.path.exists(fb):
                continue
            for x, y in zip(camps(fa, a), camps(fb, b)):
                px, rx = first(x)
                py, ry = first(y)
                if rx is None and ry is None:
                    continue
                n += 1
                if rx is None or ry is None or px != py:
                    continue
                valid += 1
                for k, f in M:
                    by[k].setdefault(rx["seed"], []).append(f(rx) - f(ry))
        print(f"## ilk {region} baskını eşli: {a} − {b}: {valid}/{n} harekât geçerli (önek aynı)")
        for k, _ in M:
            v = [st.mean(x) for x in by[k].values()]
            if not v:
                continue
            se = st.stdev(v) / len(v) ** .5 if len(v) > 1 else 0
            print(f"  {k}: {st.mean(v):+.3f} ±{se:.3f} ({len(v)} tohum)")


def tesis_table(tags, base):
    ds = [tesis_load(t) for t in tags]
    b = tesis_load(base) if base else None
    tiers = sorted({r["tier"] for d in ds + ([b] if b else []) for r in d["rs"]})
    for d in ds + ([b] if b else []):
        bal = os.path.join(root, d["tag"], "BAL.txt")
        print(f"  {d['tag']}{' (taban)' if d is b else ''}: {open(bal).read().strip() if os.path.exists(bal) else ''}; "
              f"harekât {len(d['cs'])}, baskın {len(d['rs'])}; 'iyi' küme dışı atlanan R/C {d['drop'][0]}/{d['drop'][1]}"
              + ("" if d["fac"] else "; tesis anahtarı yok (eski tek tahta, tarihsel)"))
    head = ["ölçü", "hedef"] + ([f"taban {base}"] if b else []) + tags
    sec, rows = None, []
    for s, label, goal, fn, diff, unit in tesis_rows(tiers):
        if s != sec:
            rows.append(None)
            rows.append([f"[{s}]"] + [""] * (len(head) - 1))
            sec = s
        bv = fn(b, None) if b else None
        cells = [label, goal] + ([bv[1] if bv else "-"] if b else [])
        for d in ds:
            v = fn(d, b)
            c = v[1] if v else "-"
            if diff and v and bv and v[0] is not None and bv[0] is not None:
                df = round(v[0] - bv[0], 9) + 0.0  # -0.0 yazılmasın
                c += f" [{df:+.1f}{unit}]" if unit == "p" else f" [{df:+.2f}]"
            cells.append(c)
        rows.append(cells)
    w = [max(len(x[i]) for x in [head] + [r for r in rows if r]) for i in range(len(head))]
    fmt = lambda r: "  " + "  ".join(c.ljust(w[i]) if i < 2 else c.rjust(w[i]) for i, c in enumerate(r)).rstrip()
    print(fmt(head))
    for r in rows:
        if r is None:
            continue
        print(fmt(r) if r[0][:1] != "[" else "  " + r[0])
    print("  Yüzdelerde parantez içi baskın/harekât sayısıdır; köşeli parantez tabanla farktır (p: puan). Bot insan değildir;\n"
          "  donanımı bot kendi kararıyla kırar (DEV), 'iki kırık' ile 'hazırlıksız' baskınlar aynı koşulda değildir.")


if ILK:
    ilk_table(args[0], list(zip(args[1::2], args[2::2])))
    sys.exit(0)

if TESIS:
    base = TABAN if TABAN not in (None, "-") else None
    # Tek kipten (kayıt 21) beri varsayılan taban son tesis ölçümü t17'dir; t0gen (tek tahta) tarihsel klasör olarak --taban ile okunur.
    if TABAN is None and os.path.isdir(os.path.join(root, "t17")):
        base = "t17"
    if base in args:
        base = None
    print("## tesis ölçümü", " ".join(args), f"(taban {base})" if base else "(tabansız)")
    tesis_table(args, base)
