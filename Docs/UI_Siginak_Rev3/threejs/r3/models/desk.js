// family: desk · Rev3 model library (DESIGN_R3.md §3.3) · owner: room-desk
// Unity local coordinates (x right, y up, z forward/away); fronts face −z. Desk-top props: origin = footprint centre on
// the desk top (y = 0). World placement (Unity, yaw deg) in m.meta.place, taken from work/rev2/room.js.
import * as THREE from 'three';
import { Model, gBox, gCyl, gLathe, gExtrude, gTorus, gTube, gSphere, gPlane, gDisc, roundedRect, circlePts, D2R } from '../mlib.js';
import { defMat } from '../mats.js';

// ---------------------------------------------------------------- new material keys (TR notes)
defMat('counterDrum', { hex: '#1C150E', rough: 0.5, metal: 0, tex: 'tx_counterDigits 128×1408: 11 hücreli şerit (0–9 + ikinci 0), krem rakam #EFDCAE koyu zeminde, hücre 0 üstte (v ekseni). Çark çevresi v = 1 → 1/11 (10 hücre, 36°/rakam); 11. hücre yalnız UV kaydırma içindir', note: 'sayaç çarkı (bakalit tambur)' });
defMat('counterTenths', { hex: '#E9DDBE', rough: 0.5, metal: 0, tex: 'tx_counterDigitsInv: aynı 11 hücre, koyu rakam #1C150E açık zeminde', note: 'FON onda bir çarkı (açık renk)' });
defMat('counterFraction', { hex: '#0E0906', rough: 0.6, metal: 0, tex: 'tx_counterFraction 3 hücre: “/60” (ek 2026-11-05; önce “/30”), “/12”, “/20” (yalnız rakam ve eğik çizgi, krem #C9A86A); UV 0–1, hücre seçimi UV kaydırma', note: 'pencere içindeki sabit baskı ve virgül ara plakası' });
defMat('keyIcon', { hex: '#E9DDBE', rough: 0.55, metal: 0, tex: 'tx_keyIcons 4 hücre: odalar (kesit), görev fişi, çarklı kapı, lambalı masa; koyu çizgi #1E1B16; UV 0–1 + hücre kaydırma', note: 'tuş başlığı ikon yuvası (kelime yok)' });
defMat('tagPaper', { hex: '#F2E27A', rough: 0.9, metal: 0, note: 'dikkat fişi (sarı not kâğıdı), yazı TMP Kalam' });
defMat('readyLampOff', { hex: '#1F3A26', rough: 0.25, metal: 0, note: 'hazır lambası sönük (readyLamp ↔ readyLampOff)' });
defMat('telexPaper', { hex: '#EFE4BC', rough: 0.92, metal: 0, tex: 'tx_telexPaper: kenar delik sırası ve kesikli kenar çizgisi, kelime yok (mesajlar TMP)', note: 'teleks kâğıdı; Render Face: Both' });
defMat('coffeeRingDecal', { hex: '#4A2E1A', rough: 0.6, metal: 0, alpha: 0.38, tex: 'tx_coffeeRing 256² (halka lekesi, alfa)', note: 'kupa izi çıkartması (geç harekât)' });
defMat('cardboard', { hex: '#8B6B45', rough: 0.85, metal: 0, note: 'rulo göbeği (karton)' });
defMat('inkPad', { hex: '#1A2440', rough: 0.85, metal: 0, note: 'ıstampa keçesi (mavi-siyah mürekkep)' });

// ---------------------------------------------------------------- local helpers
// cut every corner of a closed 2D polygon by c (number or per-vertex array) → chamfered long edges after extrusion
export function chamferPoly(pts, c) {
  const out = [], n = pts.length;
  for (let i = 0; i < n; i++) {
    const p = pts[i], a = pts[(i + n - 1) % n], b = pts[(i + 1) % n]; const cc = Array.isArray(c) ? c[i] : c;
    if (!cc) { out.push(p); continue; }
    const la = Math.hypot(a[0] - p[0], a[1] - p[1]), lb = Math.hypot(b[0] - p[0], b[1] - p[1]);
    const ka = Math.min(cc, la * 0.45) / la, kb = Math.min(cc, lb * 0.45) / lb;
    out.push([p[0] + (a[0] - p[0]) * ka, p[1] + (a[1] - p[1]) * ka], [p[0] + (b[0] - p[0]) * kb, p[1] + (b[1] - p[1]) * kb]);
  }
  return out;
}
// side profile [[z, y], ...] (Unity) extruded along x, length len, centred on pos.x
export function sideX(m, pts, len, c, mat, pos = [0, 0, 0], part = 'Body', o = {}) { return m.extrude(pts, len, c, mat, pos, [0, -90, 0], part, o); }
// tilted frame: origin o, Unity Euler x = a (a < 0 tilts the surface normal toward the operator). P(x, h, s): h along the
// normal, s along the surface (up/back). box: rot for boxes (box y = normal); flat: rot for planes / outlines lying in the surface
export function tilt(o, a) {
  const c = Math.cos(a * D2R), s = Math.sin(a * D2R);
  return { a, P: (x, y, z) => [o[0] + x, o[1] + y * c - z * s, o[2] + y * s + z * c], n: [0, c, s], up: [0, -s, c], box: [a, 0, 0], flat: [a + 90, 0, 0] };
}
// raw triangles (Unity coords, with normals + uv) → three geometry, winding fixed from the normals
export function triGeom(T) {
  const P = [], N = [], UV = [];
  for (const t of T) {
    const q = t.map(v => ({ p: [v.p[0], v.p[1], -v.p[2]], n: [v.n[0], v.n[1], -v.n[2]], uv: v.uv || [0, 0] }));
    const [a, b, c] = q;
    const u = [b.p[0] - a.p[0], b.p[1] - a.p[1], b.p[2] - a.p[2]], w = [c.p[0] - a.p[0], c.p[1] - a.p[1], c.p[2] - a.p[2]];
    const g = [u[1] * w[2] - u[2] * w[1], u[2] * w[0] - u[0] * w[2], u[0] * w[1] - u[1] * w[0]];
    const sn = [0, 1, 2].map(k => a.n[k] + b.n[k] + c.n[k]);
    const o = g[0] * sn[0] + g[1] * sn[1] + g[2] * sn[2] < 0 ? [a, c, b] : [a, b, c];
    for (const v of o) { P.push(...v.p); N.push(...v.n); UV.push(...v.uv); }
  }
  const g = new THREE.BufferGeometry();
  g.setAttribute('position', new THREE.Float32BufferAttribute(P, 3)); g.setAttribute('normal', new THREE.Float32BufferAttribute(N, 3)); g.setAttribute('uv', new THREE.Float32BufferAttribute(UV, 2));
  return g;
}
export const vsub = (a, b) => [a[0] - b[0], a[1] - b[1], a[2] - b[2]], vadd = (a, b) => [a[0] + b[0], a[1] + b[1], a[2] + b[2]], vmul = (a, s) => [a[0] * s, a[1] * s, a[2] * s];
export const vcross = (a, b) => [a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0]];
export const vnorm = (a) => { const l = Math.hypot(...a) || 1; return [a[0] / l, a[1] / l, a[2] / l]; };
// counter drum along x: radius r, width w, seg facets; digit cell 0 centred on the window normal at faceDeg (angle from −z toward +y).
// uv: u 0→1 left→right, v = 11-cell strip (cell 0 on top); +rotation about x brings the next digit up from below.
export function gDrum(r, w, seg, faceDeg) {
  const T = [], step = 360 / seg, vOf = (th) => 1 - (0.5 - th / 36) / 11;
  const V = (th, x) => { const ph = (faceDeg + th) * D2R, s = Math.sin(ph), c = -Math.cos(ph); return { p: [x, r * s, r * c], n: [0, s, c], uv: [x < 0 ? 0 : 1, vOf(th)] }; };
  for (let i = 0; i < seg; i++) { const t0 = 18 - i * step, t1 = t0 - step; const a = V(t0, -w / 2), b = V(t0, w / 2), c = V(t1, w / 2), d = V(t1, -w / 2); T.push([a, b, c], [a, c, d]); }
  return triGeom(T);
}
// paper strip of width w through sections [{c, a (across unit), n (printed side)}], zero thickness; both = add back faces
export function gStrip(sec, w, both = true) {
  const T = []; let v = 0;
  const vs = sec.map((s, i) => { if (i) v += Math.hypot(...vsub(s.c, sec[i - 1].c)); return v; });
  for (let i = 0; i < sec.length - 1; i++) {
    const A = sec[i], B = sec[i + 1];
    const q = [[A, -1, vs[i]], [A, 1, vs[i]], [B, 1, vs[i + 1]], [B, -1, vs[i + 1]]].map(([S, k, vv]) => ({ p: vadd(S.c, vmul(S.a, k * w / 2)), n: S.n, uv: [(k + 1) / 2, vv] }));
    T.push([q[0], q[1], q[2]], [q[0], q[2], q[3]]);
    if (both) { const r = q.map(x => ({ ...x, n: vmul(x.n, -1) })); T.push([r[0], r[2], r[1]], [r[0], r[3], r[2]]); }
  }
  return triGeom(T);
}
// flat strap (rectangle w × t) swept along a path; wdir = width direction (unit) per point or constant
export function gStrap(path, wdir, w, t, caps = true) {
  const T = []; const W = (i) => (typeof wdir === 'function' ? wdir(i) : wdir);
  const fr = path.map((p, i) => { const tg = vnorm(vsub(path[Math.min(i + 1, path.length - 1)], path[Math.max(i - 1, 0)])); const a = vnorm(W(i)); const n = vnorm(vcross(tg, a)); return { p, a, n }; });
  const C = (f, sa, sn) => vadd(f.p, vadd(vmul(f.a, sa * w / 2), vmul(f.n, sn * t / 2)));
  for (let i = 0; i < fr.length - 1; i++) {
    const A = fr[i], B = fr[i + 1];
    for (const [s1, s2, nn] of [[[1, 1], [-1, 1], 'n'], [[-1, -1], [1, -1], '-n'], [[1, -1], [1, 1], 'a'], [[-1, 1], [-1, -1], '-a']]) {
      const nA = nn === 'n' ? A.n : nn === '-n' ? vmul(A.n, -1) : nn === 'a' ? A.a : vmul(A.a, -1);
      const nB = nn === 'n' ? B.n : nn === '-n' ? vmul(B.n, -1) : nn === 'a' ? B.a : vmul(B.a, -1);
      const q = [{ p: C(A, ...s1), n: nA }, { p: C(A, ...s2), n: nA }, { p: C(B, ...s2), n: nB }, { p: C(B, ...s1), n: nB }];
      T.push([q[0], q[1], q[2]], [q[0], q[2], q[3]]);
    }
  }
  if (caps) for (const [f, sg] of [[fr[0], -1], [fr[fr.length - 1], 1]]) {
    const tg = vnorm(vcross(f.a, f.n)); const nn = vmul(tg, sg);
    const q = [[1, 1], [-1, 1], [-1, -1], [1, -1]].map(([a, b]) => ({ p: C(f, a, b), n: nn })); T.push([q[0], q[1], q[2]], [q[0], q[2], q[3]]);
  }
  return triGeom(T);
}
// gTube with Unity points (gTube itself takes three.js coordinates)
export function tubeU(pts, r, rs = 6, ts = 12, o = {}) { return gTube(pts.map(([x, y, z]) => [x, y, -z]), r, rs, ts, o); }
// world ↔ local for a model placed at T with Unity yaw θ (deg)
export function yawXf(T, yaw) { const c = Math.cos(yaw * D2R), s = Math.sin(yaw * D2R);
  return { w2l: (p) => { const dx = p[0] - T[0], dz = p[2] - T[2]; return [dx * c - dz * s, p[1] - T[1], dx * s + dz * c]; }, v2l: (v) => [v[0] * c - v[2] * s, v[1], v[0] * s + v[2] * c],
    l2w: (p) => [T[0] + p[0] * c + p[2] * s, T[1] + p[1], T[2] - p[0] * s + p[2] * c] }; }
// domed screw head (axis +y), 24 tris
export function screw(m, pos, rot = [-90, 0, 0], r = 0.003, mat = 'steelBare', part = 'Body') {
  return m.lathe([[0, 0], [r, 0], [r * 0.82, r * 0.5], [0, r * 0.66]], 6, mat, pos, rot, part, { cast: false });
}
// cast bridge pull on a −z facing surface: legs at the ends, bar stands off 22 mm
export function pull(m, x, y, zFace, w = 0.11, mat = 'brassDark', part = 'Body') {
  const L = w / 2, leg = 0.013, so = 0.022, bar = 0.009;
  return m.extrude([[-L, 0], [-L + leg, 0], [-L + leg, so - bar], [L - leg, so - bar], [L - leg, 0], [L, 0], [L, so], [-L, so]], 0.012, 0.003, mat, [x, y, zFace], [-90, 0, 0], part);
}
// brass label-card holder (frame + blank card), on a −z facing surface
export function cardHolder(m, x, y, zFace, w = 0.075, h = 0.03) {
  m.extrude(roundedRect(w, h, 0.004, 1), 0.003, 0.001, 'brassDark', [x, y, zFace - 0.0015], [0, 0, 0], 'Body', { holes: [roundedRect(w - 0.012, h - 0.01, 0.002, 1)], cast: false });
  m.add(gPlane(w - 0.012, h - 0.01), 'cardBlank', [x, y, zFace - 0.0006], [0, 0, 0], 'Body', { cast: false });
}
// rectangular frame with a hole (outline in local x/y, depth along local z), for bezels
export function frame(m, ow, oh, iw, ih, depth, c, mat, pos, rot, part = 'Body', ro = 0.006, ri = 0.003) {
  return m.extrude(roundedRect(ow, oh, ro, 1), depth, c, mat, pos, rot, part, { holes: [roundedRect(iw, ih, ri, 1)] });
}

// ================================================================ Desk
export function Desk() {
  const m = new Model('Desk', { family: 'desk', budget: 'big', listName: 'Masa ve ayakları', origin: 'taban ortası (zemin, y = 0)',
    note: 'Çelik “tanker” komuta masası (1955 dolayı). Linolyum üst; önde ve yanlarda yuvarlatılmış pirinç kenar bandı (12 mm); üstün altında çelik kuşak ve ince kalem çekmecesi; iki ayak dolabı (2 sığ + 1 dosya çekmecesi; ön yüzler çerçeveden 4 mm içeride, döküm pirinç köprü kulp, yazısız etiket kartlığı); 40 mm içeride süpürgelik; iki takviye çizgili diz paneli; ayak dolaplarının dış yanlarında pres panel. Hareketli parça yok.' });
  const TOP = 0.76, ZF = -0.53;
  m.add(gBox(1.80, 0.006, 1.06, 0.002, { skip: ['-y'] }), 'linoDesk', [0, TOP - 0.003, 0]);
  m.box(1.84, 0.028, 1.10, 0.006, 'paintOlive', [0, TOP - 0.020, 0]);
  // rounded brass edge band (12 mm): front + both sides
  const nose = [[0.004, 0.006], [-0.0015, 0.006], [-0.0042, 0.0045], [-0.0055, 0.0018], [-0.0055, -0.0018], [-0.0042, -0.0045], [-0.0015, -0.006], [0.004, -0.006]];
  sideX(m, nose, 1.86, 0, 'brassDark', [0, TOP - 0.006, -0.5545]);
  m.extrude(nose.map(([x, y]) => [-x, y]), 1.10, 0, 'brassDark', [0.9245, TOP - 0.006, 0]);
  m.extrude(nose, 1.10, 0, 'brassDark', [-0.9245, TOP - 0.006, 0]);
  // belt under the top + pencil drawer
  m.box(1.0, 0.05, 0.02, 0.003, 'paintOliveDark', [0, 0.701, ZF + 0.014]);
  m.box(0.60, 0.044, 0.016, 0.004, 'paintOlive', [0, 0.700, ZF + 0.002]);
  pull(m, 0, 0.684, ZF - 0.006, 0.12);
  // pedestals
  for (const sx of [-1, 1]) {
    const px = sx * 0.71;
    m.box(0.42, 0.654, 1.05, 0.006, 'paintOlive', [px, 0.397, 0.005]);
    for (const ex of [-1, 1]) m.box(0.022, 0.654, 0.012, 0.003, 'paintOlive', [px + ex * 0.199, 0.397, ZF + 0.006]);
    for (const [ry, rh] of [[0.713, 0.022], [0.542, 0.02], [0.372, 0.02], [0.081, 0.022]]) m.box(0.376, rh, 0.012, 0.003, 'paintOlive', [px, ry, ZF + 0.006]);
    for (const [cy, h] of [[0.627, 0.144], [0.457, 0.144], [0.227, 0.264]]) {
      const big = h > 0.2;
      m.box(0.370, h, 0.014, 0.004, 'paintOlive', [px, cy, ZF + 0.011]);
      cardHolder(m, px, cy + (big ? 0.085 : 0.036), ZF + 0.004);
      pull(m, px, cy + (big ? 0.035 : -0.012), ZF + 0.004, 0.11);
    }
    m.add(gBox(0.38, 0.07, 0.98, 0.004, { skip: ['-y'] }), 'paintOliveDark', [px, 0.035, 0.0]);
    m.box(0.004, 0.50, 0.90, 0.002, 'paintOlive', [px + sx * 0.2115, 0.40, 0.01]);                         // pressed side panel
  }
  // modesty (knee) panel with two pressed stiffening beads
  m.box(0.998, 0.56, 0.012, 0.003, 'paintOlive', [0, 0.43, 0.50]);
  for (const y of [0.33, 0.53]) m.box(0.94, 0.014, 0.006, 0.003, 'paintOlive', [0, y, 0.4915]);
  m.meta.place = [{ pos: [0, 0, 1.33], yaw: 0, note: 'oda: masa x ±0,93, z 0,77–1,89, üst y 0,76 (rev2 room.js desk)' }];
  return m;
}

// ================================================================ CounterRack
// body section (z from the front lip): lip 50 mm, 45° face 212 mm, flat top; drums r 75 mm behind the face, 12 mm proud
const CR = { D: 0.205, r: 0.075, p: 0.012, sw: 0.10, sp: 0.18, pitch: 0.042, ww: 0.0405 };
export const COUNTER_MODULES = [
  { tr: 'VARDİYA', en: 'SHIFT', n: 3 },
  { tr: 'FON', en: 'FUNDS', n: 3, tenth: true },
  { tr: 'ERZAK', en: 'RATIONS', n: 2, frac: '/60' },   // supplement 2026-11-05: ration cap 60 (was /30)
  { tr: 'MALZEME', en: 'MATERIALS', n: 3 },
  { tr: 'ÖZGÜR BÖLGE', en: 'FREE REGIONS', n: 2, frac: '/12' },
  { tr: 'GÜVEN', en: 'TRUST', n: 2, frac: '/20' },
];
export function CounterRack() {
  const m = new Model('CounterRack', { family: 'desk', budget: 'big', listName: 'Sayaç rafı (6 modül)', origin: 'taban ortası (masa üstü, y = 0)',
    note: 'Bakalit gövde, 45° eğik yüz (212 mm), üstte pirinç şerit. Her modülde yükseltilmiş pirinç pencere çerçevesi ve pirinç etiket plakası (TMP). Çarklar Ø150 mm tambur: 36°/rakam, + yönde döndükçe sıradaki rakam alttan gelir; tamburun ön yayı yüzden 12 mm çıkar ve çerçevenin içinde kalır. FON: 3 tam + 15 mm boşlukta pirinç virgül + açık renk onda bir çarkı. ERZAK /60 (ek 11-05; önce /30), ÖZGÜR /12, GÜVEN /20 pencere içinde sabit baskı (doku). Rakam yüksekliği ~40 mm (K1’de ~18 px).' });
  const Li = 1.576, zf = -CR.D / 2, zb = CR.D / 2;
  sideX(m, chamferPoly([[zf, 0], [zf, 0.05], [zf + 0.15, 0.20], [zb, 0.20], [zb, 0]], [0.002, 0.006, 0.008, 0.006, 0.002]), Li, 0.004, 'bakelite');
  const cheek = chamferPoly([[zf - 0.004, 0], [zf - 0.004, 0.0517], [zf + 0.1483, 0.204], [zb + 0.002, 0.204], [zb + 0.002, 0]], [0.002, 0.008, 0.01, 0.006, 0.002]);
  for (const sx of [-1, 1]) sideX(m, cheek, 0.012, 0.003, 'bakelite', [sx * (Li / 2 + 0.006), 0, 0]);
  m.box(Li, 0.004, 0.044, 0.0015, 'brassDark', [0, 0.202, zb - 0.03]);                 // top strip
  m.box(Li, 0.012, 0.004, 0.0015, 'brassDark', [0, 0.026, zf - 0.0015]);               // kick strip on the lip
  const S = tilt([0, 0.05, zf], -45);
  const modW = Li / 6;
  COUNTER_MODULES.forEach((md, mi) => {
    const cx = -Li / 2 + modW * (mi + 0.5);
    const nW = md.n + (md.tenth ? 1 : 0), gap = md.tenth ? 0.015 : 0, fracW = md.frac ? 0.046 : 0;
    const stackW = nW * CR.pitch + gap, x0 = cx - (stackW + fracW) / 2;
    for (let i = 0; i < nW; i++) {
      const tenth = md.tenth && i === nW - 1;
      const xw = x0 + CR.pitch * (i + 0.5) + (tenth ? gap : 0);
      const ax = S.P(xw, -(CR.r - CR.p), CR.sw);
      const name = `Wheel_${mi}_${i}`;
      m.part(name, { pivot: ax, axis: 'x', range: [0, 360], rest: 0, note: `${md.tr} ${tenth ? 'onda bir' : (i + 1) + '/' + md.n}` });
      m.add(gDrum(CR.r, CR.ww, 30, 45), tenth ? 'counterTenths' : 'counterDrum', ax, [0, 0, 0], name, { cast: false });
    }
    if (md.tenth) { // comma between the integer wheels and the tenths wheel
      const xg = x0 + CR.pitch * md.n + gap / 2;
      m.box(gap - 0.002, 0.002, 0.056, 0, 'counterFraction', S.P(xg, 0.006, CR.sw), S.box, 'Body', { cast: false });
      m.cyl(0.0055, 0.002, 12, 0, 'brass', S.P(xg, 0.008, CR.sw - 0.014), S.box, 'Body', { cast: false });
      m.box(0.0032, 0.002, 0.009, 0.0008, 'brass', S.P(xg - 0.0015, 0.008, CR.sw - 0.022), S.box, 'Body', { cast: false });
    }
    if (md.frac) m.add(gPlane(0.042, 0.040), 'counterFraction', S.P(x0 + stackW + fracW / 2, 0.006, CR.sw), S.flat, 'Body', { cast: false });
    const winW = stackW + fracW - 0.003;
    frame(m, winW + 0.026, 0.092, winW, 0.058, 0.014, 0.003, 'brass', S.P(cx, 0.007, CR.sw), S.flat, 'Body', 0.008, 0.003);
    const pw = 0.246, ph = 0.034;
    m.box(pw, 0.003, ph, 0.001, 'plate', S.P(cx, 0.0015, CR.sp), S.box);
    for (const ex of [-1, 1]) screw(m, S.P(cx + ex * (pw / 2 - 0.008), 0.003, CR.sp), S.box, 0.003, 'brassWorn');
    m.text(`TXT_Label_${mi}`, { center: S.P(cx, 0.0032, CR.sp), size: [0.228, 0.031], normal: S.n, up: S.up, role: 'counterLabelTex', capMm: 19, sample: md.tr, sampleEn: md.en,
      note: 'en uzun EN “FREE REGIONS” 12 harf tek satıra sığar; K1’de ~8 px (ikincil, üzerine gelince kâğıt etiket)' });
  });
  m.meta.place = [{ pos: [0, 0.76, 1.7775], yaw: 0, note: 'masanın arka kenarı (arka yüz z 1,88); eğik yüz K1’e bakar' }];
  return m;
}

// ================================================================ KeyRail + KeyKey
// rail section: front face 14 mm, deck tilted 20° toward the operator, 160 mm along the deck. Units: socket (cap) + plate.
export const KR = { L: 1.26, tilt: -20, hf: 0.014, zf: -0.0752, pitch: 0.30, x0: -0.551, sCap: 0.055, sPlate: 0.06, place: [-0.04, 0.76, 0.845] };
export const KEYS = [{ tr: 'SIĞINAK', en: 'SHELTER', icon: 'odalar' }, { tr: 'GÖREVLER', en: 'MISSIONS', icon: 'fiş' }, { tr: 'KAPI', en: 'DOOR', icon: 'çarklı kapı' }, { tr: 'MASA', en: 'DESK', icon: 'lambalı masa' }];
const deckFrame = () => tilt([0, KR.hf, KR.zf], KR.tilt);
export function KeyRail() {
  const m = new Model('KeyRail', { family: 'desk', budget: 'big', listName: 'Anahtar rayı', origin: 'taban ortası (masa üstü, y = 0); ön yüz masa kenarıyla aynı hizada',
    note: 'Masanın ön kenarına vidalı bakalit ray; tabla operatöre doğru 20° eğik. Önde pirinç burun şeridi, uçlarda pirinç başlıklar. 4 istasyon (aralık 0,30 m): pirinç yuva bileziği (tuş KeyKey modeli, yuvaya x −20° ile oturur) ve sağında vidalı pirinç plaka (TMP, büyük harf 22 mm → K1’de ~14 px). Hareketli parça yok.' });
  const D = deckFrame(), zf = KR.zf, zb = -zf, ytop = KR.hf + 2 * zb * Math.tan(-KR.tilt * D2R);
  sideX(m, chamferPoly([[zf, 0], [zf, KR.hf], [zb, ytop], [zb, 0]], [0.002, 0.004, 0.006, 0.002]), KR.L, 0.004, 'bakelite');
  for (const sx of [-1, 1]) sideX(m, chamferPoly([[zf - 0.003, 0], [zf - 0.003, KR.hf + 0.002], [zb + 0.002, ytop + 0.003], [zb + 0.002, 0]], [0.001, 0.004, 0.005, 0.001]), 0.006, 0.0015, 'brassDark', [sx * (KR.L / 2 + 0.003), 0, 0]);
  m.box(KR.L, 0.008, 0.006, 0.0025, 'brassDark', [0, KR.hf - 0.0035, zf - 0.0022]);
  KEYS.forEach((k, i) => {
    const x = KR.x0 + i * KR.pitch, xp = x + 0.143;
    frame(m, 0.074, 0.074, 0.060, 0.060, 0.010, 0.002, 'brass', D.P(x, 0.005, KR.sCap), D.flat, 'Body', 0.008, 0.004);
    m.add(gPlane(0.060, 0.060), 'bakeliteBlack', D.P(x, 0.0006, KR.sCap), D.flat, 'Body', { cast: false });
    m.add(gDisc(0.0125, 12), 'bakeliteBlack', D.P(x, 0.0006, KR.sCap + 0.060), D.flat, 'Body', { cast: false });
    m.box(0.19, 0.003, 0.056, 0.001, 'plate', D.P(xp, 0.0015, KR.sPlate), D.box);
    for (const ex of [-1, 1]) screw(m, D.P(xp + ex * 0.084, 0.003, KR.sPlate), D.box, 0.003, 'brassWorn');
    m.text(`TXT_Key_${i}`, { center: D.P(xp, 0.0032, KR.sPlate), size: [0.17, 0.038], normal: D.n, up: D.up, role: 'plateLabel', capMm: 22, sample: k.tr, sampleEn: k.en, note: 'EN “MISSIONS” 8 harf sığar' });
    m.anchor(`Socket_${i}`, { pos: D.P(x, 0, KR.sCap), note: `KeyKey (${k.tr}); yerel x −20°` });
  });
  m.meta.place = [{ pos: KR.place, yaw: 0, note: 'ön yüz z 0,77 (masa kenar bandı hizası), istasyonlar soldan: SIĞINAK, GÖREVLER, KAPI, MASA' }];
  return m;
}
export function KeyKey() {
  const m = new Model('KeyKey', { family: 'desk', budget: 'small', listName: 'Anahtar tuşu', origin: 'yuva ortası, ray tablası üstü (y = 0); rayda x −20° eğik oturur',
    note: 'Bakalit tuş başlığı (56 mm kare, yuvarlatılmış), üstte 3 mm derin ikon yuvası (ikon = doku hücresi: odalar, fiş, çarklı kapı, lambalı masa). Arkasında pirinç bilezikli amber mücevher lamba. Dikkat fişi: tuşun dibinden ray kenarına uzanan 7 mm bez şerit, kenardan çekül sarkan sarı kâğıt fiş (masa kenarının önünde).' });
  m.part('Cap', { pivot: [0, 0, 0], axis: 'y', kind: 'slide', range: [-0.006, 0.004], rest: 0, note: '−6 mm basılı (etkin istasyon) · +4 mm üzerinde' });
  m.extrude(roundedRect(0.056, 0.056, 0.008, 2), 0.028, 0.003, 'bakelite', [0, 0.022, 0], [90, 0, 0], 'Cap');
  frame(m, 0.050, 0.050, 0.036, 0.036, 0.003, 0.001, 'bakelite', [0, 0.0375, 0], [90, 0, 0], 'Cap', 0.006, 0.003);
  m.add(gPlane(0.035, 0.035), 'keyIcon', [0, 0.0362, 0], [90, 0, 0], 'Cap', { cast: false });
  m.lathe([[0.0085, 0], [0.0122, 0], [0.0122, 0.0045], [0.0105, 0.0064], [0.0085, 0.0064]], 12, 'brass', [0, 0, 0.060], [0, 0, 0], 'Body', { caps: false });
  m.part('Jewel', { pivot: [0, 0.006, 0.060], kind: 'swap', note: 'jewelAmberOn (etkin istasyon / kapıda biri) ↔ jewelAmberOff' });
  m.lathe([[0, 0], [0.0088, 0], [0.0092, 0.003], [0.0068, 0.0082], [0.0035, 0.0102], [0, 0.0106]], 12, 'jewelAmberOff', [0, 0.0045, 0.060], [0, 0, 0], 'Jewel', { cast: false });
  const kt = -KR.tilt * D2R, dn = [0, -Math.cos(kt), -Math.sin(kt)], nrm = [0, Math.sin(kt), -Math.cos(kt)], upw = [0, Math.cos(kt), Math.sin(kt)];
  const pv = [0, 0.004, -0.060]; const at = (d) => vadd(pv, vmul(dn, d));
  m.part('Tag', { pivot: pv, axis: 'z', range: [-12, 12], rest: 0, note: 'yalnız kapıda biri varken görünür; ray kenarından çekül sarkar, ±12° sallanır' });
  m.box(0.007, 0.0008, 0.033, 0, 'tape', [0, 0.0012, -0.0445], [0, 0, 0], 'Tag', { cast: false });
  for (const sx of [-1, 1]) m.box(0.006, 0.0135, 0.0008, 0, 'tape', vadd(at(0.006), [sx * 0.0033, 0, 0]), [-KR.tilt, 0, sx * 28], 'Tag', { cast: false });
  const cardC = at(0.012 + 0.021);
  m.box(0.074, 0.042, 0.001, 0, 'tagPaper', cardC, [-KR.tilt, 0, 0], 'Tag', { cast: false });
  m.text('TXT_Tag', { part: 'Tag', center: vadd(cardC, vmul(nrm, 0.0006)), size: [0.066, 0.034], normal: nrm, up: upw, role: 'handNote', font: 'Kalam 700', capMm: 8, align: 'center', sample: 'kapıda biri / bekliyor', sampleEn: 'someone at / the door', note: 'iki satır el yazısı; K1’de ~7 px — fişin rengi ve sallanması yeter, metin üzerine gelince kâğıt etikette' });
  const D = deckFrame();
  m.meta.place = KEYS.map((k, i) => { const p = D.P(KR.x0 + i * KR.pitch, 0, KR.sCap); return { pos: [KR.place[0] + p[0], KR.place[1] + p[1], KR.place[2] + p[2]], yaw: 0, euler: [KR.tilt, 0, 0], note: `${k.tr} / ${k.en} · ikon: ${k.icon} · Unity localEulerAngles (−20, 0, 0)` }; });
  return m;
}

// ================================================================ ShiftSwitch
export function ShiftSwitch() {
  const m = new Model('ShiftSwitch', { family: 'desk', budget: 'big', listName: 'Kapaklı vardiya şalteri', origin: 'taban ortası (masa üstü, y = 0)',
    note: 'Zeytin boyalı döküm gövde, flanşı dört cıvatayla masaya bağlı; önde 43° eğik yüzde pirinç VARDİYAYI BİTİR plakası. Üstteki kaidede kırmızı kapak (arka alt menteşe, 0 → 112° yukarı-arkaya açılır) ve altında krom milli, bakalit topuzlu kol (+35° vardiya sürerken yukarı-arka, −35° operatöre çekili). Sol üstte pirinç bilezikli yeşil hazır lambası. KİLİTLİ bandı kapağın önüne ve üstüne sarılı (baskında görünür). Arkada kablo rakoru.' });
  const W = 0.27, zf = -0.105, zb = 0.105;
  m.add(gBox(W + 0.016, 0.010, 0.226, 0.003, { skip: ['-y'] }), 'paintOliveDark', [0, 0.005, 0]);
  for (const sx of [-1, 1]) for (const sz of [-1, 1]) screw(m, [sx * (W / 2 + 0.0005), 0.010, sz * 0.100], [0, 0, 0], 0.0045, 'steelBare');
  sideX(m, chamferPoly([[zf, 0.009], [zf, 0.03], [-0.04, 0.09], [zb, 0.09], [zb, 0.009]], [0, 0.006, 0.008, 0.007, 0]), W, 0.006, 'paintOlive');
  const Sl = tilt([0, 0.03, zf], -42.7);
  m.box(0.255, 0.003, 0.046, 0.001, 'plate', Sl.P(0, 0.0015, 0.044), Sl.box);
  for (const ex of [-1, 1]) screw(m, Sl.P(ex * 0.1185, 0.003, 0.044), Sl.box, 0.003, 'brassWorn');
  m.text('TXT_Plate', { center: Sl.P(0, 0.0032, 0.044), size: [0.236, 0.03], normal: Sl.n, up: Sl.up, role: 'plateLabel', capMm: 16, sample: 'VARDİYAYI BİTİR', sampleEn: 'END SHIFT', note: 'tek satır; K1’de ~14 px (kadraj kenarında perspektif büyütmesiyle)' });
  // boss, slot, fixed hinge leaf
  const cx = 0.03;
  m.box(0.11, 0.012, 0.15, 0.003, 'paintOlive', [cx, 0.096, 0.02]);
  m.box(0.018, 0.002, 0.07, 0.0005, 'paintBlack', [cx, 0.1025, 0.02], [0, 0, 0], 'Body', { cast: false });
  m.box(0.074, 0.003, 0.012, 0.001, 'steelCast', [cx, 0.1035, 0.090]);
  // ready lamp
  m.lathe([[0.008, 0], [0.013, 0], [0.013, 0.005], [0.0112, 0.0068], [0.008, 0.0068]], 12, 'brass', [-0.08, 0.09, 0.04], [0, 0, 0], 'Body', { caps: false });
  m.part('ReadyLamp', { pivot: [-0.08, 0.097, 0.04], kind: 'swap', note: 'readyLamp (yeşil yanık: vardiya bitirilebilir) ↔ readyLampOff' });
  m.lathe([[0, 0], [0.0085, 0], [0.009, 0.003], [0.0065, 0.0085], [0.003, 0.0105], [0, 0.011]], 12, 'readyLamp', [-0.08, 0.0935, 0.04], [0, 0, 0], 'ReadyLamp', { cast: false });
  // lever (modelled upright = 0°)
  m.part('Lever', { pivot: [cx, 0.100, 0.02], axis: 'x', range: [35, -35], rest: 35, note: '+35 vardiya sürüyor (yukarı-arka) · −35 operatöre çekildi (2. tık)' });
  m.cyl(0.009, 0.026, 12, 0.002, 'steelCast', [cx, 0.100, 0.02], [0, 0, 90], 'Lever');
  m.cyl(0.0045, 0.058, 10, 0.001, 'chrome', [cx, 0.129, 0.02], [0, 0, 0], 'Lever');
  m.add(gSphere(0.011, 12, 8), 'bakeliteBlack', [cx, 0.166, 0.02], [0, 0, 0], 'Lever');
  // cover (closed = 0°), hinge at the back-bottom edge
  m.part('Cover', { pivot: [cx, 0.105, 0.095], axis: 'x', range: [0, 112], rest: 0, note: '1. tık 112° yukarı-arkaya açılır; 4 s içinde 2. tık kol, yoksa kapanır' });
  m.box(0.096, 0.006, 0.15, 0.002, 'coverRed', [cx, 0.190, 0.02], [0, 0, 0], 'Cover');
  for (const ex of [-1, 1]) m.box(0.006, 0.082, 0.15, 0.002, 'coverRed', [cx + ex * 0.045, 0.146, 0.02], [0, 0, 0], 'Cover');
  for (const z of [-0.052, 0.092]) m.box(0.084, 0.082, 0.006, 0.002, 'coverRed', [cx, 0.146, z], [0, 0, 0], 'Cover');
  m.box(0.04, 0.008, 0.012, 0.003, 'coverRed', [cx, 0.113, -0.058], [0, 0, 0], 'Cover');
  m.cyl(0.005, 0.08, 10, 0.001, 'steelCast', [cx, 0.105, 0.095], [0, 0, 90], 'Cover');
  m.text('TXT_Cover', { part: 'Cover', center: [cx, 0.1932, 0.02], size: [0.08, 0.03], normal: [0, 1, 0], up: [0, 0, 1], role: 'wallStencil', font: 'Big Shoulders Stencil Display 800', capMm: 10, sample: 'VARDİYA', sampleEn: 'SHIFT', note: 'isteğe bağlı şablon boya (rev2’deki kapak yazısı)' });
  // lock band (on the cover)
  m.part('LockBand', { parent: 'Cover', pivot: [cx, 0.150, -0.056], kind: 'swap', note: 'görünür = KİLİTLİ (baskın/rapor açıkken); paintHazard şerit' });
  m.box(0.100, 0.026, 0.003, 0.001, 'paintHazard', [cx, 0.150, -0.0565], [0, 0, 0], 'LockBand', { cast: false });
  m.box(0.100, 0.003, 0.034, 0.001, 'paintHazard', [cx, 0.1945, -0.036], [0, 0, 0], 'LockBand', { cast: false });
  m.text('TXT_LockBand', { part: 'LockBand', center: [cx, 0.150, -0.0582], size: [0.09, 0.02], role: 'stamp', capMm: 12, sample: 'KİLİTLİ', sampleEn: 'LOCKED', note: 'şeridin üstünde siyah daktilo harfi, sarı-siyah şerit dokusunun önünde' });
  // cable gland + cord into the desk
  m.cyl(0.012, 0.02, 10, 0.002, 'castIron', [0.08, 0.04, 0.115], [90, 0, 0]);
  m.add(tubeU([[0.08, 0.04, 0.124], [0.08, 0.036, 0.15], [0.08, 0.02, 0.172], [0.08, 0.002, 0.182]], 0.0045, 6, 6), 'rubber', [0, 0, 0]);
  m.anchor('Hover', { pos: [0, 0.24, -0.12], note: 'üzerine gelince kalkış ve kâğıt etiket' });
  m.meta.place = [{ pos: [0.765, 0.76, 0.885], yaw: 10, note: 'masanın sağ önü (rev2)' }];
  return m;
}

// ================================================================ Telex + TelexPaper
export const TX = { place: [-0.765, 0.76, 1.21], yaw: -12, bar: [0.285, -0.10], lane: -0.095, laneW: 0.085 };
export function Telex() {
  const m = new Model('Telex', { family: 'desk', budget: 'big', listName: 'Teleks', origin: 'taban ortası (masa üstü, y = 0)',
    note: 'Gri-yeşil döküm teleprinter (1950’ler sayfa yazıcısı). Önde 20° eğik klavye tablası: 3 sıra yuvarlak tuş (Ø15 mm) + ara çubuğu; arkada yuvarlak kapak (davlumbaz), ön eğik yüzde koyu cam gözetleme penceresi. Üst arkada döküm çatallarda kâğıt rulosu; kapak önünde krom kâğıt tutucu çubuk (ön mesaj kâğıdı buna kıskaçla asılır). Solda kâğıt şeridi çıkış yarığı ve tuşsuz şerit yolu. Sağ yanda bakalit merdane topuzu; ön dudakta pirinç TELEKS plakası.' });
  const zf = -0.19, zb = 0.19;
  sideX(m, chamferPoly([[zf, 0], [zf, 0.04], [-0.08, 0.08], [zb, 0.08], [zb, 0]], [0.002, 0.005, 0.004, 0.006, 0.002]), 0.30, 0.005, 'paintGreyGreen');
  const hood = chamferPoly([[-0.085, 0.078], [-0.085, 0.138], [-0.035, 0.208], [0.10, 0.222], [0.135, 0.219], [0.16, 0.208], [0.175, 0.19], [0.18, 0.168], [0.18, 0.078]], [0, 0.008, 0.014, 0, 0, 0, 0, 0, 0]);
  sideX(m, hood, 0.28, 0.006, 'paintGreyGreen');
  const Wn = tilt([0, 0.138, -0.085], -54.5);
  frame(m, 0.24, 0.062, 0.22, 0.046, 0.004, 0.0012, 'paintBlack', Wn.P(0, 0.002, 0.043), Wn.flat, 'Body', 0.006, 0.003);
  m.add(gPlane(0.22, 0.046), 'telexWindow', Wn.P(0, 0.0012, 0.043), Wn.flat, 'Body', { cast: false });
  m.box(0.088, 0.006, 0.003, 0.001, 'paintBlack', [TX.lane, 0.126, -0.0858], [0, 0, 0], 'Body', { cast: false });   // strip exit slot
  // copy-holder: chrome arms from the hood sides to the bar the front sheet hangs on
  const [by, bz] = TX.bar, a0 = [0.20, 0.0];
  const arm = Math.hypot(by - a0[0], bz - a0[1]), ang = Math.atan2(a0[1] - bz, by - a0[0]) / D2R;
  for (const sx of [-1, 1]) m.cyl(0.003, arm, 8, 0, 'chrome', [sx * 0.137, (by + a0[0]) / 2, (bz + a0[1]) / 2], [-ang, 0, 0]);
  m.cyl(0.0035, 0.282, 10, 0.001, 'chrome', [0, by, bz], [0, 0, 90]);
  // paper roll on cast forks
  for (const sx of [-1, 1]) m.box(0.01, 0.07, 0.04, 0.003, 'steelCast', [sx * 0.075, 0.25, 0.125]);
  m.cyl(0.042, 0.13, 16, 0.003, 'telexPaper', [0, 0.272, 0.125], [0, 0, 90]);
  m.cyl(0.005, 0.165, 8, 0.001, 'steelBare', [0, 0.272, 0.125], [0, 0, 90]);
  for (const sx of [-1, 1]) m.lathe([[0.0055, 0], [0.012, 0]], 12, 'cardboard', [sx * 0.0652, 0.272, 0.125], [0, 0, -sx * 90], 'Body', { cast: false });
  m.add(gStrip([{ c: [0, 0.232, 0.117], a: [1, 0, 0], n: [0, 0.2, -0.98] }, { c: [0, 0.2235, 0.095], a: [1, 0, 0], n: [0, 1, 0] }], 0.12, false), 'telexPaper', [0, 0, 0], [0, 0, 0], 'Body', { cast: false });
  // keyboard (deck tilted 20°)
  const K = tilt([0, 0.04, zf], -20);
  m.box(0.17, 0.002, 0.095, 0.0008, 'keyboard', K.P(0.035, 0.001, 0.0525), K.box, 'Body', { cast: false });
  const key = [[0.0075, 0], [0.0075, 0.007], [0.0062, 0.0095], [0, 0.0095]];
  for (let r = 0; r < 3; r++) for (let j = 0; j < 7; j++) m.lathe(key, 8, 'keyboardKey', K.P(-0.04 + j * 0.022 + r * 0.007, 0.002, 0.035 + r * 0.025), K.box, 'Body', { caps: false });
  m.box(0.11, 0.008, 0.012, 0.003, 'keyboardKey', K.P(0.035, 0.006, 0.013), K.box);
  // platen knob (right side)
  m.cyl(0.004, 0.008, 8, 0, 'steelBare', [0.143, 0.165, 0.02], [0, 0, 90]);
  m.lathe([[0, 0], [0.009, 0], [0.016, 0.004], [0.018, 0.012], [0.0155, 0.02], [0, 0.022]], 12, 'bakeliteBlack', [0.146, 0.165, 0.02], [0, 0, -90]);
  // TELEKS plate on the front lip, under the keyboard
  m.box(0.12, 0.028, 0.002, 0.0008, 'plate', [0.035, 0.021, zf - 0.001]);
  for (const ex of [-1, 1]) screw(m, [0.035 + ex * 0.052, 0.021, zf - 0.002], [-90, 0, 0], 0.0025, 'brassWorn');
  m.text('TXT_Plate', { center: [0.035, 0.021, zf - 0.0022], size: [0.09, 0.022], role: 'plateLabel', capMm: 13, sample: 'TELEKS', sampleEn: 'TELEX' });
  m.meta.place = [{ pos: TX.place, yaw: TX.yaw, note: 'masanın sol önü, operatöre dönük (rev2)' }];
  return m;
}
export function TelexPaper() {
  const m = new Model('TelexPaper', { family: 'desk', budget: 'small', listName: 'Teleks kâğıdı', origin: 'Telex ile aynı orijin ve yerleşim',
    note: 'Ön mesaj kâğıdı 0,26 × 0,19: kâğıt tutucu çubuğa kıskaçla asılı, alt kenarı 3° makineye yaslı, hafif kavisli (TMP: kod, durum, mesaj, önceki satır). Kâğıt şeridi soldaki yarıktan çıkar, klavye tablasının tuşsuz şeridinden ve ön dudaktan masaya iner, masada operatöre doğru düz gider ve masa kenarındaki pirinç bandın üstünden kıvrılıp sarkar. Kalınlık 0 (çift yüzlü).' });
  const [by, bz] = TX.bar, top = by - 0.004, H = 0.19, Wd = 0.26, lean = 3 * D2R;
  const SP = (x, d, bow = 0) => [x, top - d * Math.cos(lean), bz + 0.003 + d * Math.sin(lean) - bow]; // d = distance down the sheet
  const nS = [0, Math.sin(lean), -Math.cos(lean)];
  const T = []; const cols = [-Wd / 2, -Wd / 6, Wd / 6, Wd / 2], rows = [0, H / 2, H];
  const bowAt = (x) => 0.002 * (1 - (2 * x / Wd) ** 2);
  for (let r = 0; r < 2; r++) for (let c = 0; c < 3; c++) {
    const q = [[c, r], [c + 1, r], [c + 1, r + 1], [c, r + 1]].map(([ci, ri]) => ({ p: SP(cols[ci], rows[ri], bowAt(cols[ci])), n: nS, uv: [ci / 3, 1 - ri / 2] }));
    T.push([q[0], q[1], q[2]], [q[0], q[2], q[3]]); const b = q.map(v => ({ ...v, n: vmul(nS, -1) })); T.push([b[0], b[2], b[1]], [b[0], b[3], b[2]]);
  }
  m.add(triGeom(T), 'telexPaper', [0, 0, 0], [0, 0, 0], 'Body', { cast: false });
  m.box(0.05, 0.016, 0.008, 0.002, 'chrome', [0, top - 0.002, bz - 0.002]);
  const slot = (name, d, h, cap, role, tr, en, o = {}) => m.text(name, { center: vadd(SP(0, d, 0.002), vmul(nS, 0.0006)), size: [0.22, h], normal: nS, up: [0, Math.cos(lean), Math.sin(lean)], role, capMm: cap, sample: tr, sampleEn: en, ...o });
  slot('TXT_Code', 0.020, 0.016, 9, 'telexCode', '001/01', '001/01', { align: 'left' });
  slot('TXT_Status', 0.045, 0.026, 15, 'telexStatus', 'KAYIT VAR', 'SAVE FOUND', { align: 'left', note: 'olumsuz durumda kırmızı #9A1E14' });
  slot('TXT_Message', 0.10, 0.075, 10, 'telexMessage', 'V.001 kaydının üstüne yazılır · onay için yeniden bas', 'V.001 will be overwritten · press again to confirm', { align: 'left', note: '3–4 satır, sarar' });
  slot('TXT_Prev', 0.172, 0.014, 7, 'telexMessage', '001/00  KYGA-07 HAT AÇIK', '001/00  KYGA-07 LINE OPEN', { align: 'left', note: 'kesikli ayraç altında önceki mesaj' });
  // strip: exit slot → deck lane → lip → desk → over the desk edge band
  const x = TX.lane, X = yawXf(TX.place, TX.yaw);
  const sec = [
    { c: [x, 0.125, -0.0868], n: [0, 0, -1] }, { c: [x, 0.090, -0.0872], n: [0, 0, -1] }, { c: [x, 0.0805, -0.093], n: [0, 0.7, -0.7] },
    { c: [x, 0.0715, -0.115], n: [0, 0.94, -0.342] }, { c: [x, 0.0505, -0.173], n: [0, 0.94, -0.342] }, { c: [x, 0.0405, -0.1915], n: [0, 0.6, -0.8] },
    { c: [x, 0.027, -0.2035], n: [0, 0.45, -0.89] }, { c: [x, 0.010, -0.219], n: [0, 0.82, -0.57] }, { c: [x, 0.0006, -0.238], n: [0, 1, 0] },
  ].map(s => ({ ...s, a: [1, 0, 0] }));
  const s8 = X.l2w(sec[sec.length - 1].c); const xw = s8[0] - 0.003;
  const aL = X.v2l([1, 0, 0]);
  const W = (zw, yw, ny, nz) => ({ c: X.w2l([xw, yw, zw]), a: aL, n: X.v2l([0, ny, nz]) });
  sec.push(W(s8[2] - 0.012, 0.7606, 1, 0), W(0.7765, 0.7606, 1, 0), W(0.7715, 0.7590, 0.82, -0.57), W(0.7682, 0.7525, 0.3, -0.95), W(0.7675, 0.7385, 0, -1), W(0.7695, 0.7225, -0.25, -0.97));
  m.add(gStrip(sec, TX.laneW, true), 'telexPaper', [0, 0, 0], [0, 0, 0], 'Body', { cast: false });
  m.meta.place = [{ pos: TX.place, yaw: TX.yaw, note: 'Telex ile aynı; şerit masada dünya −z yönünde gider ve z 0,77’deki kenar bandından sarkar' }];
  return m;
}

// ================================================================ PaperTray
export function PaperTray() {
  const m = new Model('PaperTray', { family: 'desk', budget: 'small', listName: 'EVRAK tepsisi', origin: 'taban ortası (masa üstü, y = 0)',
    note: 'Boyalı pres sac evrak tepsisi (kıvrık kenarlı), yanları öne doğru alçalır; içinde kâğıt destesi ve üstte hafif dönük beyaz yaprak. Ön duvarda pirinç etiket kartlığı (TMP EVRAK / PAPERS). StackLate: geç harekâtta görünen yüksek deste.' });
  const W = 0.32, D = 0.26, mt = 'paintOliveDark';
  m.add(gBox(W, 0.004, D, 0.0015, { skip: ['-y'] }), mt, [0, 0.002, 0]);
  for (const sx of [-1, 1]) sideX(m, chamferPoly([[-D / 2, 0], [-D / 2, 0.04], [D / 2, 0.07], [D / 2, 0]], [0, 0.003, 0.003, 0]), 0.005, 0.0015, mt, [sx * (W / 2 - 0.0025), 0, 0]);
  m.box(W, 0.07, 0.005, 0.0015, mt, [0, 0.035, D / 2 - 0.0025]);
  m.box(W, 0.04, 0.005, 0.0015, mt, [0, 0.02, -D / 2 + 0.0025]);
  m.box(W + 0.004, 0.008, 0.008, 0.003, mt, [0, 0.04, -D / 2 + 0.002]);
  m.box(W + 0.004, 0.008, 0.008, 0.003, mt, [0, 0.07, D / 2 - 0.002]);
  const ang = Math.atan2(0.03, D) / D2R;
  for (const sx of [-1, 1]) m.box(0.008, 0.008, Math.hypot(D, 0.03), 0.003, mt, [sx * (W / 2 - 0.002), 0.055, 0], [-ang, 0, 0]);
  m.box(W - 0.03, 0.022, D - 0.03, 0.001, 'paper', [0, 0.015, 0.006]);
  m.box(W - 0.036, 0.0015, D - 0.05, 0, 'paperWhite', [0.006, 0.0268, -0.002], [0, 3, 0]);
  m.part('StackLate', { pivot: [0, 0.028, 0], kind: 'swap', note: 'geç harekât: yüksek evrak destesi görünür' });
  for (let i = 0; i < 7; i++) m.box(W - 0.04, 0.0042, D - 0.06, 0, i % 2 ? 'paper' : 'paperWhite', [(i % 2 ? 0.004 : -0.003), 0.030 + i * 0.0042, (i % 3 - 1) * 0.004], [0, (i % 4 - 1.5) * 1.6, 0], 'StackLate', { cast: i === 6 });
  cardHolder(m, 0, 0.02, -D / 2, 0.11, 0.032);
  m.text('TXT_Label', { center: [0, 0.02, -D / 2 - 0.0012], size: [0.094, 0.021], role: 'plateLabel', capMm: 12, sample: 'EVRAK', sampleEn: 'PAPERS' });
  m.meta.place = [{ pos: [0.765, 0.76, 1.30], yaw: -4, note: 'masanın sağı, CRT masası tarafı (rev2)' }];
  return m;
}

// ================================================================ DeskLamp (banker lamp)
function halfTube(R, L, seg, b, inward) { // half cylinder along x (opening down), tilted by b deg about x (+b: top toward −z)
  const T = [], cb = Math.cos(b * D2R), sb = Math.sin(b * D2R);
  const P = (al, x) => { const z = -R * Math.cos(al), y = R * Math.sin(al); return [x, y * cb + z * sb, -y * sb + z * cb]; };
  for (let i = 0; i < seg; i++) {
    const a0 = Math.PI * i / seg, a1 = Math.PI * (i + 1) / seg;
    const q = [[a0, -L / 2], [a0, L / 2], [a1, L / 2], [a1, -L / 2]].map(([al, x]) => { const p = P(al, x); const n = vnorm([0, p[1], p[2]]); return { p, n: inward ? vmul(n, -1) : n, uv: [x / L + 0.5, al / Math.PI] }; });
    T.push([q[0], q[1], q[2]], [q[0], q[2], q[3]]);
  }
  return triGeom(T);
}
export function DeskLamp() {
  const m = new Model('DeskLamp', { family: 'desk', budget: 'small', listName: 'Masa lambası', origin: 'taban ortası (masa üstü, y = 0)',
    note: 'Bankacı lambası: iki kademeli pirinç taban, arkadan yükselen pirinç gövde ve kıvrık kol; yeşil kaplama cam yarım silindir abajur (dışı lampGlass, içi ışıyan lampInner), uçlarda pirinç kapaklar, içinde boru ampul. Abajur ağzı 10° operatöre eğik. Çekme zinciri YOK (TAA titremesi): tabanın sağ önünde Ø22 mm bakalit döner anahtar. Işık çapası abajurun altında (spot, gölgeli).' });
  m.extrude(roundedRect(0.20, 0.12, 0.05, 2), 0.018, 0.004, 'brass', [0, 0.009, 0], [90, 0, 0]);
  m.extrude(roundedRect(0.15, 0.08, 0.035, 1), 0.010, 0.003, 'brass', [0, 0.023, 0.005], [90, 0, 0]);
  m.lathe([[0, 0], [0.011, 0], [0.012, 0.004], [0.0085, 0.012], [0.0068, 0.02], [0.0068, 0.27], [0.0095, 0.278], [0.0072, 0.29], [0, 0.292]], 8, 'brass', [0, 0.028, 0.035]);
  // shade
  const R = 0.07, Ls = 0.26, b = 10, sy = 0.345, sz = -0.04;
  const cb = Math.cos(b * D2R), sbb = Math.sin(b * D2R), rot = (z, y) => [y * sbb + z * cb, y * cb - z * sbb];
  const arc = (r, n, rev) => { const a = []; for (let i = 0; i <= n; i++) { const al = Math.PI * (rev ? n - i : i) / n; a.push(rot(-r * Math.cos(al), r * Math.sin(al))); } return a; };
  sideX(m, [...arc(R, 9), ...arc(R - 0.004, 9, true)], Ls, 0.0015, 'lampGlass', [0, sy, sz]);
  m.add(halfTube(R - 0.0045, Ls - 0.004, 9, b, true), 'lampInner', [0, sy, sz], [0, 0, 0], 'Body', { cast: false });
  for (const sx of [-1, 1]) sideX(m, arc(R + 0.003, 8), 0.005, 0.0015, 'brass', [sx * (Ls / 2 + 0.0025), sy, sz]);
  m.cyl(0.011, 0.13, 8, 0, 'lampBulb', [0, sy + 0.022, sz + 0.004], [0, 0, 90], 'Body', { cast: false });
  // harp: stem top → shade back-top
  const att = rot(-R * Math.cos(2.2), R * Math.sin(2.2)); const ap = [0, sy + att[1], sz + att[0]];
  m.add(tubeU([[0, 0.31, 0.035], [0, 0.345, 0.04], [0, ap[1] + 0.012, ap[2] + 0.012], [0, ap[1] + 0.002, ap[2] + 0.001]], 0.0055, 6, 8), 'brass', [0, 0, 0]);
  // rotary switch knob on the base (Ø 22 mm)
  m.lathe([[0, 0], [0.011, 0], [0.011, 0.010], [0.0095, 0.014], [0.004, 0.0155], [0, 0.0155]], 8, 'bakeliteBlack', [0.06, 0.028, -0.02]);
  m.light('LampSpot', { pos: [0, sy - 0.03, sz - 0.005], type: 'spot', shadows: true, color: '#FFD9A0', note: 'tokens lights.deskLamp: açı 47°, yarı gölge 0,55; hedef dünyada (0,02, 0,76, 1,22)' });
  m.meta.place = [{ pos: [0.85, 0.76, 1.58], yaw: 30, note: 'masanın sağ arkası, abajur haritaya dönük' }];
  return m;
}

// ================================================================ small desk props
export function Mug() {
  const m = new Model('Mug', { family: 'desk', budget: 'small', listName: 'Kupa', origin: 'taban ortası (masa üstü, y = 0)',
    note: 'Emaye çelik kupa Ø85 × 95: hafif koni gövde, kıvrık mavi kenar (mugRim), yuvarlak kesitli kulp (Ø9 mm), içte kahve yüzeyi. Kenar çentikleri dokuda (ortak kir haritası).' });
  m.lathe([[0, 0], [0.036, 0], [0.0395, 0.0025], [0.0415, 0.009], [0.0425, 0.090]], 16, 'enamel', [0, 0, 0], [0, 0, 0], 'Body', { caps: false });
  m.lathe([[0.0425, 0.090], [0.0437, 0.0925], [0.0428, 0.0952], [0.0405, 0.0955], [0.0393, 0.093], [0.0393, 0.089]], 16, 'mugRim', [0, 0, 0], [0, 0, 0], 'Body', { caps: false });
  m.lathe([[0.0393, 0.089], [0.039, 0.078]], 16, 'enamel', [0, 0, 0], [0, 0, 0], 'Body', { caps: false });
  m.add(gDisc(0.039, 16), 'coffee', [0, 0.078, 0], [90, 0, 0], 'Body', { cast: false });
  m.add(tubeU([[0.040, 0.077, 0], [0.058, 0.075, 0], [0.066, 0.058, 0], [0.062, 0.035, 0], [0.050, 0.024, 0], [0.040, 0.022, 0]], 0.0045, 6, 10), 'enamel', [0, 0, 0]);
  m.meta.place = [{ pos: [0.88, 0.76, 1.08], yaw: 0, note: 'sağ ön köşe, kulp +x' }, { pos: [-0.70, 0.76, 1.47], yaw: 140, note: 'geç harekât: ikinci kupa (stage late)' }];
  return m;
}
export function Pencil() {
  const m = new Model('Pencil', { family: 'desk', budget: 'small', listName: 'Kalem', origin: 'kalemin ortası, masa üstünde (y = 0; altıgenin düz yüzü masada)',
    note: 'Sarı boyalı altıgen kurşun kalem (7,5 mm), yontulmuş ahşap koni ve grafit uç, pirinç yüksük ve silgi. Kısalmış: 155 mm.' });
  const R = 0.0075 / Math.sqrt(3), hy = 0.00375, Lb = 0.11;
  m.extrude(circlePts(R, 6), Lb, 0.0004, 'pencil', [0, hy, 0], [0, 90, 0]);
  m.lathe([[R, 0], [0.0012, 0.02]], 6, 'pawn', [Lb / 2, hy, 0], [0, 0, -90], 'Body', { caps: false });
  m.lathe([[0.0012, 0], [0, 0.0055]], 6, 'inkBlack', [Lb / 2 + 0.02, hy, 0], [0, 0, -90], 'Body', { cast: false });
  m.cyl(0.0044, 0.012, 8, 0.0006, 'brassWorn', [-Lb / 2 - 0.006, hy, 0], [0, 0, 90]);
  m.cyl(0.0039, 0.009, 8, 0.0015, 'eraser', [-Lb / 2 - 0.0165, hy, 0], [0, 0, 90]);
  m.meta.place = [{ pos: [0.42, 0.76, 0.955], yaw: 62, note: 'anahtar rayının arkası, haritanın önü (rev2)' }];
  return m;
}
export function Eraser() {
  const m = new Model('Eraser', { family: 'desk', budget: 'small', listName: 'Silgi', origin: 'taban ortası (masa üstü, y = 0)', note: 'Pahlı pembe silgi 50 × 12 × 20, bir ucu aşınmış, ortasında kâğıt kılıf.' });
  m.box(0.05, 0.012, 0.02, 0.003, 'eraser', [0, 0.006, 0]);
  m.box(0.008, 0.010, 0.017, 0.004, 'eraser', [0.026, 0.005, 0]);
  m.box(0.026, 0.0128, 0.0208, 0.001, 'paperWhite', [-0.007, 0.0064, 0]);
  m.meta.place = [{ pos: [0.28, 0.76, 0.958], yaw: -20, note: 'kalemin solu (rev2)' }];
  return m;
}
function flake(m, pos, rot, R, wav, k) { // curled shaving: 250° cone sector with a wavy painted rim, double sided
  const T = [], Rim = [], seg = 10, a0 = k * 1.3;
  for (let i = 0; i < seg; i++) {
    const A = [a0 + i / seg * 4.36, a0 + (i + 1) / seg * 4.36];
    const ro = A.map((a, j) => R + wav * Math.sin((i + j) * 2.1 + k));
    const P = (r, a, y) => [r * Math.cos(a), y, r * Math.sin(a)];
    const q = [P(0.0015, A[0], 0.003), P(0.0015, A[1], 0.003), P(ro[1], A[1], 0.0006), P(ro[0], A[0], 0.0006)];
    const r2 = [P(ro[0], A[0], 0.0006), P(ro[1], A[1], 0.0006), P(ro[1] + 0.0009, A[1], 0.0001), P(ro[0] + 0.0009, A[0], 0.0001)];
    for (const [quad, out] of [[q, T], [r2, Rim]]) { const n = vnorm(vcross(vsub(quad[1], quad[0]), vsub(quad[3], quad[0])));
      const up = n[1] < 0 ? vmul(n, -1) : n; const f = quad.map(p => ({ p, n: up })), b = quad.map(p => ({ p, n: vmul(up, -1) }));
      out.push([f[0], f[1], f[2]], [f[0], f[2], f[3]], [b[0], b[2], b[1]], [b[0], b[3], b[2]]); }
  }
  m.add(triGeom(T), 'pawn', pos, rot, 'Body', { cast: false }); m.add(triGeom(Rim), 'pencil', pos, rot, 'Body', { cast: false });
}
export function Shavings() {
  const m = new Model('Shavings', { family: 'desk', budget: 'small', listName: 'Kalem talaşı', origin: 'yığının ortası, masa üstü (y = 0)', note: '5 kıvrık talaş (Ø12–16 mm), dalgalı sarı boya kenarlı, iki yüzlü ince yüzey.' });
  [[0, 0, 0.007, 0, 8], [0.014, 0.004, 0.0062, 1, -12], [-0.012, 0.008, 0.0068, 2, 20], [0.004, -0.013, 0.0058, 3, -6], [-0.006, -0.004, 0.0075, 4, 14]]
    .forEach(([x, z, R, k, tl]) => flake(m, [x, 0.0002, z], [tl, k * 70, tl * 0.5], R, 0.0009, k));
  m.meta.place = [{ pos: [0.50, 0.76, 0.965], yaw: 0, note: 'kalemin yanı' }];
  return m;
}
export function StampRack() {
  const m = new Model('StampRack', { family: 'desk', budget: 'small', listName: 'Damga rafı', origin: 'taban ortası (masa üstü, y = 0)',
    note: 'Cilalı ahşap raf, üç lastik damga (tornalanmış ahşap sap, ahşap gövde, kauçuk yüz), yanında kapaklı ıstampa kutusu. Damga yüzü desenleri dokuda.' });
  m.box(0.20, 0.018, 0.06, 0.003, 'woodVarnish', [-0.045, 0.009, 0]);
  m.box(0.20, 0.03, 0.012, 0.003, 'woodVarnish', [-0.045, 0.033, 0.024]);
  for (const x of [-0.11, -0.045, 0.02]) {
    m.box(0.044, 0.004, 0.028, 0.001, 'rubber', [x, 0.020, -0.004], [0, 0, 0], 'Body', { cast: false });
    m.box(0.046, 0.016, 0.03, 0.002, 'woodLight', [x, 0.030, -0.004]);
    m.lathe([[0.009, 0], [0.006, 0.008], [0.0055, 0.028], [0.0115, 0.038], [0.0125, 0.046], [0.0085, 0.054], [0, 0.056]], 8, 'woodVarnish', [x, 0.038, -0.004]);
  }
  m.box(0.08, 0.012, 0.055, 0.002, 'galv', [0.105, 0.006, 0]);
  m.box(0.082, 0.005, 0.057, 0.0015, 'galv', [0.105, 0.0135, 0]);
  m.meta.place = [{ pos: [-0.895, 0.76, 1.735], yaw: -90, note: 'masanın sol arka köşesi (sayaç rafının solu); rev2’de (−0,76, 1,55) — teleks büyüdüğü için kaydı' }];
  return m;
}
export function TeamFigure() {
  const m = new Model('TeamFigure', { family: 'desk', budget: 'small', listName: 'Ekip figürü', origin: 'taban ortası (harita üstü, y = 0)',
    note: 'Pirinç oval taban 70 × 48 × 6 (iki kademe: koyu kenar teamRim + pahlı üst teamBase), arkada üç tornalanmış ahşap piyon (h 26, r 6,6; baş ve bant ekip rengi: Teknisyen mavi, EMP amber, Analist yeşil; aralık 19 mm), önde pirinç bilezikli bakalit harf diski Ø21 (TMP harf).' });
  const sz = [1, 1, 48 / 70];
  m.lathe([[0, 0], [0.0352, 0], [0.0352, 0.0018]], 16, 'teamRim', [0, 0, 0], [0, 0, 0], 'Body', { caps: false, scale: sz });
  m.lathe([[0.0345, 0.0018], [0.0345, 0.0045], [0.033, 0.006], [0, 0.006]], 16, 'teamBase', [0, 0, 0], [0, 0, 0], 'Body', { caps: false, scale: sz });
  [['teamTechnician', -0.019], ['teamEmp', 0], ['teamAnalyst', 0.019]].forEach(([mt, x]) => {
    const p = [x, 0.006, 0.008];
    m.lathe([[0, 0], [0.0066, 0], [0.0066, 0.0018], [0.0057, 0.0035], [0.0055, 0.014], [0.0042, 0.0165], [0, 0.0165]], 8, 'pawn', p);
    m.lathe([[0.0059, 0.0075], [0.0059, 0.0112]], 8, mt, p, [0, 0, 0], 'Body', { caps: false, cast: false });
    m.lathe([[0, 0.0158], [0.0045, 0.0158], [0.006, 0.0185], [0.0062, 0.0215], [0.005, 0.0245], [0.0028, 0.026], [0, 0.026]], 8, mt, p);
  });
  m.lathe([[0, 0], [0.0105, 0], [0.0105, 0.0022], [0.0098, 0.0028], [0, 0.0028]], 12, 'teamPlate', [0, 0.006, -0.0115], [0, 0, 0], 'Body', { cast: false });
  m.lathe([[0.0105, 0], [0.0118, 0], [0.0118, 0.0024], [0.0105, 0.0024]], 12, 'brass', [0, 0.006, -0.0115], [0, 0, 0], 'Body', { caps: false, cast: false });
  m.text('TXT_Letter', { center: [0, 0.0089, -0.0115], size: [0.017, 0.017], normal: [0, 1, 0], up: [0, 0, 1], role: 'teamLetter (Courier Prime 700, krem #EFDCAE)', capMm: 11, sample: 'A', sampleEn: 'A', note: 'ekip harfi: A = Alfa' });
  m.meta.place = [{ pos: [-0.19, 0.7612, 1.46], yaw: 0, note: 'örnek: haritada Batı Avrupa; konum oyunda harita pikselinden (rev2 buildTeamFigure)' }];
  return m;
}
export function CoffeeRing() {
  const m = new Model('CoffeeRing', { family: 'desk', budget: 'small', listName: 'Kupa izi', origin: 'leke ortası, masa üstü (y = 0)', note: 'Geç harekât çıkartması: kahve halkası (alfa doku), gölge yok.' });
  m.add(gPlane(0.10, 0.10), 'coffeeRingDecal', [0, 0.0004, 0], [90, 0, 0], 'Body', { cast: false });
  m.meta.place = [{ pos: [0.70, 0.76, 1.10], yaw: 25, note: 'stage late · kupanın solu' }];
  return m;
}
export function TelexRoll() {
  const m = new Model('TelexRoll', { family: 'desk', budget: 'small', listName: 'Yedek teleks rulosu', origin: 'taban ortası (masa üstü, y = 0)', note: 'Geç harekât: dik duran yedek rulo (Ø90 × 130), karton göbek, kraft bant, ucu masaya sarkan kâğıt.' });
  m.lathe([[0.012, 0], [0.0435, 0], [0.045, 0.0015], [0.045, 0.1285], [0.0435, 0.13], [0.012, 0.13]], 16, 'telexPaper', [0, 0, 0], [0, 0, 0], 'Body', { caps: false });
  m.lathe([[0.012, 0.1305], [0.012, 0.004]], 12, 'cardboard', [0, 0, 0], [0, 0, 0], 'Body', { caps: false, cast: false });
  m.lathe([[0.0455, 0.045], [0.0455, 0.085]], 16, 'clipboard', [0, 0, 0], [0, 0, 0], 'Body', { caps: false, cast: false });
  m.add(gStrip([{ c: [0.0, 0.07, -0.0452], n: [0, 0, -1] }, { c: [0.0, 0.03, -0.0465], n: [0, 0, -1] }, { c: [0.0, 0.006, -0.056], n: [0, 0.6, -0.8] }, { c: [0.0, 0.0006, -0.085], n: [0, 1, 0] }].map(q => ({ ...q, a: [1, 0, 0] })), 0.10, true), 'telexPaper', [0, 0, 0], [0, 0, 0], 'Body', { cast: false });
  m.meta.place = [{ pos: [0.865, 0.76, 1.79], yaw: -20, note: 'stage late · sağ arka köşe' }];
  return m;
}
export function PaperPile() {
  const m = new Model('PaperPile', { family: 'desk', budget: 'small', listName: 'Yırtık kâğıt yığını', origin: 'taban ortası (masa üstü, y = 0)', note: 'Geç harekât: koparılmış teleks yaprakları (0,21 × 0,15) üst üste, kayık; en üstteki yaprağın köşesi kalkık. Mesaj yazıları dokuda değil (eski kâğıt, okunmaz).' });
  for (let i = 0; i < 7; i++) m.box(0.21, 0.0018, 0.15, 0, i % 3 ? 'telexPaper' : 'paperWhite', [(i % 2 ? 0.006 : -0.004) * (i % 3), 0.0009 + i * 0.0019, (i % 3 - 1) * 0.005], [0, (i * 37 % 9 - 4) * 1.4, 0], 'Body', { cast: i > 4 });
  const y0 = 0.0145, T = [], X = [-0.105, 0, 0.105], Z = [-0.075, 0, 0.075];
  const lift = (x, z) => Math.max(0, (x - 0.02) / 0.085 * (-z + 0.0) / 0.075) * 0.022;
  for (let a = 0; a < 2; a++) for (let b = 0; b < 2; b++) {
    const q = [[a, b], [a + 1, b], [a + 1, b + 1], [a, b + 1]].map(([i, j]) => ({ p: [X[i], y0 + lift(X[i], Z[j]), Z[j]], n: [0, 1, 0], uv: [i / 2, j / 2] }));
    T.push([q[0], q[1], q[2]], [q[0], q[2], q[3]]); const r = q.map(v => ({ ...v, n: [0, -1, 0] })); T.push([r[0], r[2], r[1]], [r[0], r[3], r[2]]);
  }
  m.add(triGeom(T), 'telexPaper', [0, 0, 0], [0, -6, 0]);
  m.meta.place = [{ pos: [-0.78, 0.76, 1.50], yaw: -15, note: 'stage late · teleksin arkası' }];
  return m;
}


// ================================================================ SlipMouth + HandoverSlip (supplement 2026-11-05, R19 shift close)
// The switch keeps its form; a slim slip printer is bolted to its left flank. Coordinates are SWITCH-local (same origin and
// placement as ShiftSwitch). The exit point given by the implementation is (−0,30; 0,14; 0,015): the slip slides 12 cm from
// x −0,18 to −0,30 out of the mouth (left face, y 0,14) and comes to rest on a brass slip tongue.
export function SlipMouth() {
  const m = new Model('SlipMouth', { family: 'desk', budget: 'small', listName: 'Fiş ağzı (şaltere bağlı devir yazıcısı)', origin: 'şalterin orijini (taban ortası, masa üstü); şalterle aynı yerleşim',
    note: 'Şalterin sol yanına iki cıvatayla bağlanan zeytin döküm kutu (72 × 156 × 180 mm). Sol yüzde y 0,14’te 112 mm yatay ağız, pirinç dudaklar ve dişli koparma çubuğu; önünde pirinç fiş dili (165 mm). Kol inince fiş 0,35 s’de 12 cm kayarak dile yatar. Önde DEVİR FİŞİ plakası ve fiş bekliyor lambası.' });
  const x0 = -0.143, x1 = -0.215, xc = (x0 + x1) / 2, y0 = 0.009, y1 = 0.165, zc = 0.01, D = 0.18;
  m.box(x0 - x1, y1 - y0, D, 0.004, 'paintOlive', [xc, (y0 + y1) / 2, zc]);
  m.box(x0 - x1 + 0.008, 0.008, D + 0.008, 0.002, 'paintOliveDark', [xc, y0 + 0.004, zc]);
  for (const z of [-0.06, 0.08]) screw(m, [x0 + 0.004, 0.05, z], [0, 0, 90], 0.004, 'steelBare');
  // mouth on the left face, brass lips, serrated tear bar
  const ym = 0.14; m.box(0.006, 0.006, 0.116, 0, 'punchHole', [x1 - 0.0006, ym, 0.015], [0, 0, 0], 'Body', { cast: false });
  for (const s of [-1, 1]) m.box(0.006, 0.0025, 0.124, 0.0006, 'brassWorn', [x1 - 0.002, ym + s * 0.0045, 0.015]);
  m.box(0.003, 0.004, 0.124, 0.0005, 'steelBare', [x1 - 0.0045, ym + 0.0085, 0.015]);
  // slip tongue (brass rest) with two struts
  m.box(0.150, 0.0015, 0.104, 0.0005, 'brassWorn', [x1 - 0.075, ym - 0.006, 0.015], [0, 0, 2]);
  for (const s of [-1, 1]) m.box(0.150, 0.004, 0.0015, 0.0004, 'brassWorn', [x1 - 0.075, ym - 0.004, 0.015 + s * 0.052], [0, 0, 2]);
  for (const z of [-0.03, 0.06]) m.box(0.004, 0.06, 0.004, 0, 'steelCast', [x1 - 0.05, ym - 0.036, z], [0, 0, -38]);
  // front plate and slip-waiting lamp
  m.box(0.056, 0.022, 0.0018, 0.0005, 'plate', [xc, 0.105, zc - D / 2 - 0.0009]);
  m.text('TXT_Plate', { center: [xc, 0.105, zc - D / 2 - 0.002], size: [0.052, 0.016], role: 'plateLabel', capMm: 6.5, sample: 'DEVİR FİŞİ', sampleEn: 'HANDOVER' });
  m.lathe([[0.006, 0], [0.009, 0], [0.009, 0.004], [0.006, 0.005]], 10, 'brass', [xc, 0.135, zc - D / 2], [-90, 0, 0]);
  m.part('Lamp', { pivot: [xc, 0.135, zc - D / 2 - 0.004], kind: 'swap', note: 'fiş bekliyor: jewelAmberOn (fiş dilde ya da elde, EVRAK’a girene kadar) ↔ jewelAmberOff' });
  m.add(gSphere(0.0055, 10, 6), 'jewelAmberOff', [xc, 0.135, zc - D / 2 - 0.004], [0, 0, 0], 'Lamp', { cast: false });
  m.anchor('SlipStart', { pos: [-0.18, 0.14, 0.015], note: 'fişin başlangıç noktası (merkez), ağzın içinde' });
  m.anchor('SlipExit', { pos: [-0.30, 0.14, 0.015], note: 'fişin durduğu nokta (merkez) — uygulamadaki çıkış noktası; 12 cm, 0,35 s, ease-out' });
  m.meta.place = [{ pos: [0.765, 0.76, 0.885], yaw: 10, note: 'ShiftSwitch ile aynı (şaltere bağlı)' }];
  return m;
}
export function HandoverSlip() {
  const m = new Model('HandoverSlip', { family: 'desk', budget: 'small', listName: 'Devir fişi (100 × 150 mm)', origin: 'fişin merkezi; ön yüz −z (dilde yatarken +y)',
    note: 'Teleks kâğıdından devir fişi (0,2 mm), üst kenarı delikli koparma çizgisi. Dilde yatarken uzun kenarı x boyunca; elde kameraya dönük, hafif kıvrık. Yüz dokusu R10 devir fişi (TR/EN), alfa yalnız koparma kenarında.' });
  const W = 0.10, H = 0.15, nx = 4, ny = 8; const g = new THREE.PlaneGeometry(W, H, nx, ny); const pos = g.attributes.position;
  for (let i = 0; i < pos.count; i++) { const y = pos.getY(i); pos.setZ(i, 0.004 * Math.pow((y + H / 2) / H, 2)); }   // slight curl toward the top
  g.computeVertexNormals();
  m.add(g, 'telexPaper', [0, 0, 0], [0, 0, 0], 'Body', { cast: true });
  m.text('FACE', { center: [0, 0, -0.0003], size: [W, H], role: 'paperFace', note: 'devir fişi dokusu (R10: devir_fisi_v001_tr@2x.png …)' });
  return m;
}

export const MODELS = {
  SlipMouth: { build: SlipMouth },
  HandoverSlip: { build: HandoverSlip },
  Desk: { build: Desk },
  CounterRack: { build: CounterRack },
  KeyRail: { build: KeyRail },
  KeyKey: { build: KeyKey },
  ShiftSwitch: { build: ShiftSwitch },
  Telex: { build: Telex },
  TelexPaper: { build: TelexPaper },
  PaperTray: { build: PaperTray },
  DeskLamp: { build: DeskLamp },
  Mug: { build: Mug },
  Pencil: { build: Pencil },
  Eraser: { build: Eraser },
  Shavings: { build: Shavings },
  StampRack: { build: StampRack },
  TeamFigure: { build: TeamFigure },
  CoffeeRing: { build: CoffeeRing },
  TelexRoll: { build: TelexRoll },
  PaperPile: { build: PaperPile },
};
