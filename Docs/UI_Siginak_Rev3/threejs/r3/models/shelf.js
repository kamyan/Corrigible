// family: shelf · Rev3 model library (see DESIGN_R3.md §3.5) — owner: room-shelf
// Unity local coordinates (x right, y up, z forward/away); fronts face −z. Shelf-local items: m.meta.place is in the
// SHELF frame (shelf origin = world (−1.50, 0, 1.72), yaw −35); diorama items: in the DIORAMA frame (diorama origin =
// shelf-local (0.20, 0.78, −0.01)). Every word is a TMP slot (m.text).
// This file also exports small geometry helpers (loft, sweep, segment, world placement) used by crt.js and door.js.
import * as THREE from 'three';
import { Model, gBox, gCyl, gLathe, gExtrude, gTorus, gTube, gSphere, gPlane, gDisc, gRing, roundedRect, circlePts, prep, D2R } from '../mlib.js';
import { defMat } from '../mats.js';

defMat('shelfPost', { hex: '#353B30', rough: 0.62, metal: 0.05, tex: 'tx_slotted (köşebent yarık delik dizisi, alfa kesme, 512 px)', note: 'delikli köşebent dikme (boyalı sac)' });
defMat('rope', { hex: '#8C7A55', rough: 0.95, metal: 0, note: 'kendir halat (sandık sapı)' });
defMat('roomCard', { hex: '#FBF7EC', rough: 0.85, metal: 0, tex: 'tx_roomcard (kâğıt lifi + ad altında kırmızı çizgi #D2483F, yazısız)', note: 'oda kartı kartonu' });
defMat('selYellow', { hex: '#F5D646', rough: 0.6, metal: 0, emissive: '#F5D646', ei: 0.35, note: 'seçili oda çerçevesi (sarı = seçili)' });
defMat('moltenGlow', { hex: '#5A2A10', rough: 0.6, metal: 0, emissive: '#FF7A2A', ei: 1.4, note: 'döküm kalıbı, döküm sürerken (kızgın)' });
defMat('firstAidSign', { hex: '#F4EFE2', rough: 0.6, metal: 0, tex: 'tx_firstaid (yeşil haç işareti, 128 px)', note: 'revir dolabı işareti (yazısız)' });
defMat('scrapMetal', { hex: '#5E605B', rough: 0.55, metal: 0.6, note: 'hurda malzeme (kova dolgusu)' });

// ---------------------------------------------------------------- local helpers (exported for crt.js / door.js)
const T3 = (p) => [p[0], p[1], -p[2]];
const sub = (a, b) => [a[0] - b[0], a[1] - b[1], a[2] - b[2]];
const add = (a, b) => [a[0] + b[0], a[1] + b[1], a[2] + b[2]];
const scl = (a, s) => [a[0] * s, a[1] * s, a[2] * s];
const cross = (a, b) => [a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0]];
const dot = (a, b) => a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
const nrm = (a) => { const l = Math.hypot(a[0], a[1], a[2]) || 1; return [a[0] / l, a[1] / l, a[2] / l]; };
const lerp = (a, b, t) => [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, a[2] + (b[2] - a[2]) * t];
export const V = { sub, add, scl, cross, dot, nrm, lerp };

// push a triangle (three-space points) oriented away from `inside` (or along `dir`), skip degenerate ones
function pushTri(out, a, b, c, inside, dir, flip) {
  const n = cross(sub(b, a), sub(c, a)); if (Math.hypot(n[0], n[1], n[2]) < 1e-14) return;
  let s = dir ? dot(n, dir) : dot(n, sub(scl(add(add(a, b), c), 1 / 3), inside)); if (flip) s = -s;
  if (s < 0) out.push(...a, ...c, ...b); else out.push(...a, ...b, ...c);
}
export function geomTris(out, crease = 40) { const g = new THREE.BufferGeometry(); g.setAttribute('position', new THREE.Float32BufferAttribute(out, 3)); return prep(g, { crease }); }
// loft through rings of Unity points (same count). o.orient: 'out' (default) | 'in' | [x,y,z] facing dir;
// o.caps [start,end]; o.closed (ring closed, default true); o.crease (deg, default 50)
export function gLoft(rings, o = {}) {
  const R = rings.map(r => r.map(T3)); const n = R[0].length, N = R.length; const out = [];
  const cen = R.map(r => scl(r.reduce((s, p) => add(s, p), [0, 0, 0]), 1 / r.length));
  const dir = Array.isArray(o.orient) ? T3(o.orient) : null; const flip = o.orient === 'in';
  const closed = o.closed !== false;
  for (let i = 0; i < N - 1; i++) { const ins = lerp(cen[i], cen[i + 1], 0.5);
    for (let j = 0; j < (closed ? n : n - 1); j++) { const k = (j + 1) % n;
      pushTri(out, R[i][j], R[i][k], R[i + 1][k], ins, dir, flip); pushTri(out, R[i][j], R[i + 1][k], R[i + 1][j], ins, dir, flip); } }
  const far = (i0, step) => { for (let i = i0 + step; i >= 0 && i < N; i += step) if (Math.hypot(...sub(cen[i], cen[i0])) > 1e-6) return cen[i]; return add(cen[i0], [0, 1e-3, 0]); };
  const caps = o.caps ?? [true, true];
  if (closed && !dir) {
    if (caps[0]) { const ins = far(0, 1); for (let j = 0; j < n; j++) pushTri(out, cen[0], R[0][j], R[0][(j + 1) % n], ins, null, flip); }
    if (caps[1]) { const ins = far(N - 1, -1); for (let j = 0; j < n; j++) pushTri(out, cen[N - 1], R[N - 1][j], R[N - 1][(j + 1) % n], ins, null, flip); }
  }
  return geomTris(out, o.crease ?? 50);
}
// horizontal ellipse ring around c (a: half-width x, b: half-depth z); o.rot (deg about y), o.fz (front/back bias)
export function ringH(c, a, b, n, o = {}) {
  const pts = []; const ry = (o.rot || 0) * D2R, ph = o.phase ?? 0;
  for (let i = 0; i < n; i++) { const t = (i / n) * Math.PI * 2 + ph; let x = a * Math.cos(t), z = b * Math.sin(t);
    if (o.fz && z < 0) z *= o.fz; if (o.bz && z > 0) z *= o.bz;
    pts.push([c[0] + x * Math.cos(ry) + z * Math.sin(ry), c[1], c[2] - x * Math.sin(ry) + z * Math.cos(ry)]); }
  return pts;
}
// ring perpendicular to d around c: a along `side` (projected), b along d × side
export function ringP(c, d, a, b, n, side = [1, 0, 0], ph = 0) {
  d = nrm(d); let s = sub(side, scl(d, dot(side, d))); if (Math.hypot(...s) < 1e-6) s = Math.abs(d[0]) < 0.9 ? [1, 0, 0] : [0, 0, 1];
  s = nrm(sub(s, scl(d, dot(s, d)))); const f = cross(d, s); const pts = [];
  for (let i = 0; i < n; i++) { const t = (i / n) * Math.PI * 2 + ph; pts.push(add(c, add(scl(s, a * Math.cos(t)), scl(f, b * Math.sin(t))))); }
  return pts;
}
// rectangle w (along side) × t sweep along a Unity path
export function gSweep(path, w, t, o = {}) {
  const rings = path.map((p, i) => {
    const tg = nrm(sub(path[Math.min(path.length - 1, i + 1)], path[Math.max(0, i - 1)]));
    let s = o.side || [0, 1, 0]; s = sub(s, scl(tg, dot(s, tg))); if (Math.hypot(...s) < 1e-6) s = [1, 0, 0]; s = nrm(s); const q = cross(tg, s);
    const hw = w / 2, ht = t / 2;
    return [add(p, add(scl(s, hw), scl(q, ht))), add(p, add(scl(s, -hw), scl(q, ht))), add(p, add(scl(s, -hw), scl(q, -ht))), add(p, add(scl(s, hw), scl(q, -ht)))];
  });
  return gLoft(rings, { caps: o.caps ?? [true, true], crease: o.crease ?? 35 });
}
// tapered round tube along a Unity path (polyline, rings ⟂ local tangent)
export function gPipe(path, r0, r1, n = 8, o = {}) {
  const L = path.length; const rings = path.map((p, i) => {
    const tg = sub(path[Math.min(L - 1, i + 1)], path[Math.max(0, i - 1)]); const r = r0 + (r1 - r0) * (i / Math.max(1, L - 1));
    return ringP(p, tg, r * (o.sx || 1), r * (o.sz || 1), n, o.side || [1, 0, 0]);
  });
  return gLoft(rings, { caps: o.caps ?? [true, true], crease: o.crease ?? 60 });
}
// Unity Euler that turns +y into direction d
export function dirEuler(d) { d = nrm(d); return [Math.atan2(d[2], d[1]) / D2R, 0, -Math.asin(Math.max(-1, Math.min(1, d[0]))) / D2R]; }
// cylinder (r0 at p0, r1 at p1) between two Unity points
export function segCyl(m, p0, p1, r0, r1, seg, mat, part = 'Body', o = {}) {
  const d = sub(p1, p0); return m.cyl(r0, Math.hypot(...d), seg, o.c || 0, mat, lerp(p0, p1, 0.5), dirEuler(d), part, { ...o, r2: r1 });
}
// box between two Unity points (long axis = segment), cross-section w × t, roll side hint
export function segBox(m, p0, p1, w, t, c, mat, part = 'Body', o = {}) {
  const d = sub(p1, p0); return m.box(w, Math.hypot(...d), t, c, mat, lerp(p0, p1, 0.5), dirEuler(d), part, o);
}
// Catmull-Rom tube through Unity points (mlib gTube wants three space)
export function tubeU(m, pts, r, rs, ts, mat, part = 'Body', o = {}) { return m.add(gTube(pts.map(T3), r, rs, ts, o), mat, [0, 0, 0], [0, 0, 0], part, o); }
// add a geometry that is already in MODEL space (Unity coords) to a part
export function addU(m, g, mat, part = 'Body', o = {}) { return m.add(g, mat, [0, 0, 0], [0, 0, 0], part, o); }
// world position of a local point under (base, yaw)
export function wp(base, yaw, p) { const a = yaw * D2R, c = Math.cos(a), s = Math.sin(a); return [base[0] + p[0] * c + p[2] * s, base[1] + p[1], base[2] - p[0] * s + p[2] * c].map(v => Math.round(v * 1e4) / 1e4); }
// pointed leaf outline from (0,0) to (0,L), half-width w
export function leafPts(L, w, n = 5) { const P = [[0, 0]]; for (let i = 1; i < n; i++) { const t = i / n; P.push([w * Math.sin(Math.PI * Math.pow(t, 0.8)) * (1 - 0.25 * t), L * t]); } P.push([0, L]);
  for (let i = n - 1; i >= 1; i--) { const t = i / n; P.push([-w * Math.sin(Math.PI * Math.pow(t, 0.8)) * (1 - 0.25 * t), L * t]); } return P; }
// small round screw/rivet head facing direction d (Unity), dome profile, no hidden cap
export function screwHead(m, pos, d, r, h, mat, part = 'Body', seg = 6) {
  return m.lathe([[r, 0], [r * 0.8, h * 0.7], [0, h]], seg, mat, pos, dirEuler(d), part, { caps: false, cast: false });
}

// shelf constants (shared by placements)
export const SHELF = { base: [-1.5, 0, 1.72], yaw: -35, levels: [0.14, 0.78, 1.52, 1.96], W: 1.26, D: 0.47 };
export const DIO = { shelfLocal: [0.20, 0.78, -0.01], W: 0.80, H: 0.72, D: 0.34,
  rooms: { yasam: [-0.2075, 0.32], telsiz: [0.2075, 0.32], atolye: [-0.2075, 0.085], revir: [0.2075, 0.085] }, roomW: 0.325, roomH: 0.17 };
const fmt = (p) => p.map(v => (+v).toFixed(3)).join(', ');
// world placement of a shelf-local / diorama-local point (room3.js treats meta.place as WORLD)
export const shelfW = (p, relYaw = 0, note = '') => ({ pos: wp(SHELF.base, SHELF.yaw, p), yaw: SHELF.yaw + relYaw, local: { frame: 'shelf', pos: p, yaw: relYaw }, note: `dünya; raf yereli (${fmt(p)})${relYaw ? ', yaw ' + relYaw : ''}: ${note}` });
export const dioW = (p, relYaw = 0, note = '') => { const q = add(DIO.shelfLocal, p); const r = shelfW(q, relYaw, `maket yereli (${fmt(p)}) · ${note}`); r.local = { frame: 'diorama', pos: p, yaw: relYaw }; return r; };
const RT = { yasam: ['YAŞAM ALANI', 'LIVING QUARTERS'], telsiz: ['TELSİZ ODASI', 'RADIO ROOM'], atolye: ['ATÖLYE', 'WORKSHOP'], revir: ['REVİR', 'INFIRMARY'] };

// ---------------------------------------------------------------- Shelf
export function Shelf() {
  const m = new Model('Shelf', { family: 'shelf', budget: 'big', listName: 'Raf (delikli çelik köşebent, 4 kat)', origin: 'taban ortası (zemin, y = 0)',
    note: 'Delikli çelik köşebent dikmeli depo rafı. 4 kat (üst yüzeyler 0,14 · 0,78 · 1,52 · 1,96 m); her kat dört kenarı aşağı kıvrık sac tepsi, ön kenar dikmelere cıvatalı. Arkada iki, yanlarda birer çapraz lama; ayak plakaları. Delikler dokuda (alfa kesme). 2. katta kesit maketi; solda özet panosu ve BOŞTA tepsisi. 3. katın altında, ön kenarın arkasında küçük raf lambası: BOŞTA levhasını ve özet panosunu aydınlatır (ışık noktası UnderShelfLight).' });
  const { W, D, levels } = SHELF; const hx = W / 2, hz = D / 2, H = 2.0, Lw = 0.045, Lt = 0.006;
  // four L-angle posts 45 × 45 × 6 (outer corner on the footprint corner, flanges along front/back and sides)
  const L = [[0.003, 0], [Lw - 0.0015, 0], [Lw, 0.0015], [Lw, Lt - 0.0015], [Lw - 0.0015, Lt], [Lt, Lt], [Lt, Lw - 0.0015], [Lt - 0.0015, Lw], [0.0015, Lw], [0, Lw - 0.0015], [0, 0.003]];
  for (const sx of [-1, 1]) for (const sz of [-1, 1]) {
    const pts = L.map(([u, v]) => [sx * hx - sx * u, -(sz * hz - sz * v)]);
    m.extrude(pts, H - 0.008, 0.002, 'shelfPost', [0, 0.008 + (H - 0.008) / 2, 0], [-90, 0, 0]);
    m.box(0.07, 0.008, 0.07, 0.003, 'paintOliveDark', [sx * (hx - 0.03), 0.004, sz * (hz - 0.03)], [0, 0, 0], 'Body', { skip: ['-y'], cast: false }); // foot plate
  }
  // shelf trays: deck + four folded lips (inside the post flanges)
  const sw = W - 2 * Lt - 0.002, sd = D - 2 * Lt - 0.002, lip = 0.036;
  for (const y of levels) {
    m.box(sw, 0.006, sd, 0.002, 'paintOlive', [0, y - 0.003, 0]);
    m.box(sw, lip, 0.008, 0.0025, 'paintOlive', [0, y - lip / 2, -sd / 2 + 0.004]);
    m.box(sw, lip, 0.008, 0.0025, 'paintOlive', [0, y - lip / 2, sd / 2 - 0.004]);
    for (const sx of [-1, 1]) m.box(0.008, lip, sd - 0.016, 0.0025, 'paintOlive', [sx * (sw / 2 - 0.004), y - lip / 2, 0]);
    for (const sx of [-1, 1]) { // hex bolts through the front posts (front flange + side flange)
      m.cyl(0.0068, 0.005, 6, 0, 'steelBare', [sx * (hx - 0.024), y - lip / 2, -hz - 0.0025], [-90, 0, 0], 'Body', { cast: false });
      m.cyl(0.0068, 0.005, 6, 0, 'steelBare', [sx * (hx + 0.0025), y - lip / 2, -hz + 0.024], [0, 0, -sx * 90], 'Body', { cast: false });
    }
  }
  // sway braces (flat straps 25 × 6): X at the back between levels 1–2 and 2–3, X on both sides between levels 1–2
  const strapBack = (y0, y1) => { const dx = 2 * (hx - 0.03), dy = (y1 - 0.05) - (y0 + 0.03), a = Math.atan2(dy, dx) / D2R, L2 = Math.hypot(dx, dy), yc = (y0 + 0.03 + y1 - 0.05) / 2;
    m.box(L2, 0.025, 0.006, 0.002, 'paintOliveDark', [0, yc, hz + 0.003], [0, 0, a]); m.box(L2, 0.025, 0.006, 0.002, 'paintOliveDark', [0, yc, hz + 0.009], [0, 0, -a]); };
  strapBack(levels[0], levels[1]); strapBack(levels[1], levels[2]);
  for (const sx of [-1, 1]) { const y0 = levels[0], y1 = levels[1]; const dz = 2 * (hz - 0.03), dy = (y1 - 0.05) - (y0 + 0.03), a = Math.atan2(dy, dz) / D2R, L2 = Math.hypot(dz, dy), yc = (y0 + 0.03 + y1 - 0.05) / 2;
    m.box(0.006, 0.025, L2, 0.002, 'paintOliveDark', [sx * (hx + 0.003), yc, 0], [-a, 0, 0]); m.box(0.006, 0.025, L2, 0.002, 'paintOliveDark', [sx * (hx + 0.009), yc, 0], [a, 0, 0]); }
  // brass plate on the level-2 front lip, under the section model (shelf-local x 0.20)
  const py = levels[1] - lip / 2, pz = -sd / 2 - 0.0015;
  m.box(0.25, 0.028, 0.003, 0.001, 'plate', [0.20, py, pz], [0, 0, 0], 'Body', { cast: false });
  for (const sx of [-1, 1]) screwHead(m, [0.20 + sx * 0.116, py, pz - 0.0015], [0, 0, -1], 0.0032, 0.002, 'brassWorn', 'Body', 6);
  m.text('TXT_Plate', { center: [0.20, py, pz - 0.0016], size: [0.21, 0.02], role: 'plateLabel', capMm: 10, sample: 'SIĞINAK 07 · KESİT', sampleEn: 'SHELTER 07 · SECTION', note: 'maketin altındaki sabit pirinç plaka' });
  // under-shelf lamp (behind the level-3 front lip, above the clipboard and the idle tray)
  const ly = levels[2] - 0.006;
  m.box(0.30, 0.022, 0.05, 0.004, 'paintBlack', [-0.42, ly - 0.011, -0.185]);
  m.part('ShelfLamp', { pivot: [-0.42, ly - 0.023, -0.185], kind: 'swap', note: 'raf lambası camı: lampGlassWarm (yanık) ↔ roomBulbOff (sönük)' });
  m.box(0.27, 0.005, 0.034, 0.002, 'lampGlassWarm', [-0.42, ly - 0.0245, -0.185], [0, 0, 0], 'ShelfLamp', { cast: false });
  tubeU(m, [[-0.27, ly - 0.012, -0.185], [-0.15, ly - 0.012, -0.17], [0.30, ly - 0.012, -0.10], [0.58, ly - 0.012, -0.1]], 0.004, 5, 8, 'rubber', 'Body', { cast: false });
  m.light('UnderShelfLight', { part: 'ShelfLamp', pos: [-0.42, ly - 0.03, -0.19], type: 'spot', color: '#FFD9A0', shadows: false,
    note: 'aşağı ve 25° öne bakar; koni 110°, menzil 0,9 m, gölgesiz. Özet panosu kâğıdını ve BOŞTA levhasını (öne eğik) aydınlatır.' });
  m.meta.place = [{ pos: SHELF.base, yaw: SHELF.yaw, note: 'oda sol arka köşesi (Rev2 room.js shelf); dünya AABB yaklaşık x −2,15…−0,85, z 1,16–2,27' }];
  return m;
}

// ---------------------------------------------------------------- Crate
export function Crate() {
  const m = new Model('Crate', { family: 'shelf', budget: 'small', listName: 'Sandık (ERZAK / MALZEME)', origin: 'taban ortası (oturduğu raf yüzeyi, y = 0)',
    note: 'Kaba tahta ikmal sandığı: tahta gövde, ön yüzde çerçeve çıtalar, yanlarda halat saplı tutamak çıtası, kapakta iki çıta. Şablon yazısı TMP (Big Shoulders Stencil): ERZAK ya da MALZEME.' });
  const W = 0.40, D = 0.30, H = 0.25, t = 0.012, bw = 0.045;
  m.box(W - 2 * t, H, D - 2 * t, 0.003, 'woodRaw', [0, H / 2, 0], [0, 0, 0], 'Body', { skip: ['-y'] });
  const fz = -D / 2 + t / 2;
  m.box(W, bw, t, 0.003, 'wood', [0, H - bw / 2, fz]); m.box(W, bw, t, 0.003, 'wood', [0, bw / 2, fz]);
  for (const sx of [-1, 1]) m.box(bw, H - 2 * bw, t, 0.003, 'wood', [sx * (W / 2 - bw / 2), H / 2, fz]);
  m.box(W, bw, t, 0, 'wood', [0, H - bw / 2, D / 2 - t / 2]);                                 // back top batten (plain)
  for (const sx of [-1, 1]) {
    const x = sx * (W / 2 - t / 2);
    m.box(t, bw, D - 2 * t, 0.003, 'wood', [x, H - bw / 2, 0]); m.box(t, bw, D - 2 * t, 0.003, 'wood', [x, bw / 2, 0]);
    m.box(t + 0.006, 0.05, 0.17, 0.003, 'wood', [sx * (W / 2 + 0.003), 0.165, 0]);              // handle cleat
    tubeU(m, [[sx * (W / 2 + 0.009), 0.172, -0.055], [sx * (W / 2 + 0.02), 0.13, -0.05], [sx * (W / 2 + 0.024), 0.112, 0], [sx * (W / 2 + 0.02), 0.13, 0.05], [sx * (W / 2 + 0.009), 0.172, 0.055]], 0.0065, 5, 8, 'rope', 'Body', { cast: false });
  }
  for (const sx of [-1, 1]) m.box(bw, t, D - 2 * t, 0.003, 'wood', [sx * 0.12, H + t / 2, 0]);  // lid battens
  m.text('TXT_Stencil', { center: [0, 0.125, -D / 2 + t - 0.0005], size: [0.30, 0.10], role: 'wallStencil', font: 'Big Shoulders Stencil Display 800', capMm: 45,
    sample: 'ERZAK', sampleEn: 'RATIONS', note: 'ikinci örnek MALZEME / MATERIALS; boya rengi #D9D2BF, biraz silik' });
  m.meta.place = [shelfW([-0.36, 1.52, 0.0], 0, '3. kat: ERZAK'), shelfW([0.07, 1.52, 0.01], 2, '3. kat: MALZEME (K1’de görünen)'), shelfW([-0.33, 0.14, 0.0], -3, '1. kat: MALZEME')];
  return m;
}

// ---------------------------------------------------------------- Jerrycan
export function Jerrycan() {
  const m = new Model('Jerrycan', { family: 'shelf', budget: 'small', listName: 'Bidon (20 L)', origin: 'taban ortası (y = 0)',
    note: '20 litrelik askerî bidon: iki yarım kabuk ortada kaynak dikişi, iki yan yüzde kabarık X nervürü, üstte üç sap, sol üst köşede kelepçeli ağız kapağı. Boyalı sac.' });
  const W = 0.345, H = 0.43, D = 0.165;
  m.box(W, H, D, 0.022, 'paintOlive', [0, H / 2, 0], [0, 0, 0], 'Body', { skip: ['-y'] });
  m.extrude(roundedRect(W + 0.01, H - 0.004, 0.026, 2), 0.006, 0.002, 'paintOlive', [0, H / 2 + 0.002, 0]);           // weld seam flange
  const a = Math.atan2(H - 0.11, W - 0.09) / D2R, Lx = Math.hypot(W - 0.09, H - 0.11);
  for (const sz of [-1, 1]) for (const s of [-1, 1]) m.box(Lx, 0.042, 0.008, 0.004, 'paintOlive', [0, H / 2, sz * (D / 2 + 0.001)], [0, 0, s * a]);
  for (const x of [-0.035, 0.035, 0.105]) tubeU(m, [[x, H - 0.012, -0.05], [x, H + 0.03, -0.038], [x, H + 0.036, 0], [x, H + 0.03, 0.038], [x, H - 0.012, 0.05]], 0.009, 6, 6, 'paintOlive', 'Body');
  // spout with a clamp cap at the top left
  const sp = [-0.12, H + 0.004, 0]; const e = [0, 0, 28];
  m.cyl(0.022, 0.03, 10, 0.003, 'paintOlive', sp, e);
  m.cyl(0.026, 0.024, 10, 0.004, 'steelCast', [sp[0] - 0.012, sp[1] + 0.024, 0], e);
  m.box(0.05, 0.012, 0.03, 0.003, 'steelCast', [sp[0] + 0.02, sp[1] + 0.03, 0], [0, 0, 28]);
  m.meta.place = [shelfW([0.38, 0.14, 0.02], 0, '1. kat (y 0,14), dik duruyor')];
  return m;
}

// ---------------------------------------------------------------- HopePlant (3 stages)
function hopeTin(m) {
  m.lathe([[0, 0], [0.033, 0], [0.035, 0.003], [0.034, 0.006], [0.034, 0.06], [0.0362, 0.063], [0.0355, 0.0665], [0.033, 0.0665], [0.033, 0.058]], 12, 'galv', [0, 0, 0], [0, 0, 0], 'Body', { caps: false });
  m.lathe([[0.0347, 0.014], [0.0347, 0.05]], 12, 'tape', [0, 0, 0], [0, 0, 0], 'Body', { caps: false, cast: false }); // faded blank label
  m.cyl(0.033, 0.004, 12, 0, 'soil1', [0, 0.058, 0], [0, 0, 0], 'Body', { cast: false });
}
function leaf(m, base, L, w, lift, az, mat = 'leaf') { m.extrude(leafPts(L, w, 4), 0.0016, 0, mat, base, [-90 + lift, az, 0], 'Body', { cast: true }); }
export function HopePlant(stage) {
  const m = new Model(`HopePlant${stage}`, { family: 'shelf', budget: 'small', listName: `Umut bitkisi · aşama ${stage}`, origin: 'taban ortası (teneke dibi, y = 0)',
    note: ['Konserve tenekesinde (soluk, yazısız etiket) toprak ve iki çenekli filiz.', 'Aynı teneke: 7 cm gövde, beş yaprak.', 'Aynı teneke: tahta destek çubuğu ve bağ, 13 cm ana gövde, iki yan dal, on bir yaprak (geç harekât).'][stage - 1] });
  hopeTin(m);
  const s0 = [0, 0.06, 0];
  if (stage === 1) {
    segCyl(m, s0, [0.002, 0.085, 0], 0.0026, 0.0022, 6, 'leaf');
    leaf(m, [0.002, 0.084, 0], 0.017, 0.007, 25, 90); leaf(m, [0.002, 0.084, 0], 0.017, 0.007, 30, -90);
  } else if (stage === 2) {
    const p1 = [0.004, 0.10, 0.002], p2 = [0.0, 0.13, -0.002];
    segCyl(m, s0, p1, 0.0032, 0.0028, 6, 'leaf'); segCyl(m, p1, p2, 0.0028, 0.0022, 6, 'leaf');
    leaf(m, [0.003, 0.088, 0.001], 0.028, 0.011, 18, 20); leaf(m, [0.004, 0.10, 0.001], 0.03, 0.012, 22, 160);
    leaf(m, [0.002, 0.114, 0], 0.028, 0.011, 26, 260); leaf(m, [0.001, 0.124, -0.001], 0.024, 0.01, 35, 70); leaf(m, p2, 0.02, 0.008, 55, 200);
  } else {
    segCyl(m, [0.024, 0.055, 0.012], [0.026, 0.21, 0.014], 0.003, 0.0026, 6, 'woodLight');                      // stake
    m.box(0.012, 0.006, 0.012, 0, 'rope', [0.019, 0.15, 0.011], [0, 30, 0], 'Body', { cast: false });            // tie
    const st = [s0, [0.006, 0.11, 0.004], [0.012, 0.15, 0.008], [0.008, 0.19, 0.004]];
    for (let i = 0; i < 3; i++) segCyl(m, st[i], st[i + 1], 0.0042 - i * 0.0006, 0.0036 - i * 0.0006, 6, 'leaf');
    const b1 = [-0.03, 0.13, -0.01], b2 = [0.04, 0.165, -0.012];
    segCyl(m, [0.006, 0.105, 0.004], b1, 0.003, 0.0024, 6, 'leaf'); segCyl(m, [0.011, 0.145, 0.007], b2, 0.0028, 0.0022, 6, 'leaf');
    const Ls = [[0.006, 0.085, 0.004, 0.04, 0.017, 15, 30], [0.007, 0.095, 0.004, 0.042, 0.018, 12, 210], [-0.012, 0.118, -0.004, 0.038, 0.016, 20, 150],
      [b1[0], b1[1], b1[2], 0.036, 0.015, 30, 110], [b1[0], b1[1], b1[2], 0.03, 0.013, 25, 250], [0.012, 0.14, 0.007, 0.04, 0.017, 18, 300],
      [0.02, 0.152, -0.004, 0.036, 0.015, 22, 0], [b2[0], b2[1], b2[2], 0.034, 0.014, 28, 330], [b2[0], b2[1], b2[2], 0.03, 0.012, 35, 60],
      [0.01, 0.175, 0.006, 0.032, 0.013, 30, 180], [0.008, 0.19, 0.004, 0.026, 0.011, 55, 90]];
    for (const [x, y, z, L2, w, lift, az] of Ls) leaf(m, [x, y, z], L2, w, lift, az);
  }
  m.meta.place = [shelfW([0.46, 1.52, -0.06], 0, '3. kat sağ ön; harekâta göre aşama 1 → 2 → 3 (geç harekât); yalnız biri görünür')];
  return m;
}

// ---------------------------------------------------------------- Diorama (section model)
const DPLACE = (p, note) => dioW(p, 0, note);
export function Diorama() {
  const m = new Model('Diorama', { family: 'shelf', budget: 'big', listName: 'Kesit maketi (sığınak)', origin: 'taban ortası (raf yüzeyi, y = 0)',
    note: 'Vernikli kontrplak kasada sığınağın kesiti: alt ve orta kart bantları (beton döşeme önleri), dört oda (boş kutular, donanım ayrı model), ortada merdivenli şaft, üstte DEPO bandı, toprak katmanları ve yüzeyde yıkıntılar ile YZ direği (kırmızı fener = YZ). Oda iç yüzleri güce göre malzeme değiştirir; seçili oda sarı çerçeve. Oda kartları RoomCardHolder + RoomCard ile kart çapalarına takılır.' });
  const { W, H, D, rooms, roomW, roomH } = DIO; const iw = W - 0.04, sd = D - 0.02;
  // varnished plywood case
  m.box(W, 0.02, D, 0.003, 'woodVarnish', [0, 0.01, 0], [0, 0, 0], 'Body', { skip: ['-y'] });
  for (const sx of [-1, 1]) m.box(0.02, H - 0.02, D, 0.003, 'woodVarnish', [sx * (W / 2 - 0.01), 0.02 + (H - 0.02) / 2, 0]);
  m.box(iw, H - 0.02, 0.012, 0.002, 'woodVarnish', [0, 0.02 + (H - 0.02) / 2, D / 2 - 0.006], [0, 0, 0], 'Body', { skip: ['-z'] }); // skip names are three-space: '-z' = Unity back
  // concrete structure: card bands, slabs, walls
  const slab = (h, y) => m.box(iw, h, sd, 0.003, 'slab', [0, y, 0]);
  slab(0.065, 0.0525); slab(0.065, 0.2875); slab(0.016, 0.498); slab(0.014, 0.607);
  for (const x of [-0.375, 0.375, -0.04, 0.04]) m.box(0.01, 0.405, sd, 0.002, 'slab', [x, 0.2875, 0]);
  for (const sx of [-1, 1]) m.box(0.01, 0.094, sd, 0.002, 'slab', [sx * 0.375, 0.553, 0]);
  // shaft: back wall + steel ladder
  m.box(0.07, 0.405, 0.006, 0, 'concreteWall', [0, 0.2875, 0.155], [0, 0, 0], 'Body', { cast: false });
  for (const sx of [-1, 1]) m.box(0.006, 0.405, 0.006, 0.0015, 'steelCast', [sx * 0.021, 0.2875, 0.118]);
  for (let y = 0.10; y < 0.48; y += 0.03) m.box(0.036, 0.0048, 0.0048, 0, 'steelCast', [0, y, 0.118], [0, 0, 0], 'Body', { cast: false });
  // rooms: inner liners (power swap), ceiling lamp (swap), brass floor nosing, selection frame (swap)
  for (const [k, [cx, fy]] of Object.entries(rooms)) {
    const P = `Room_${k}`;
    m.part(P, { pivot: [cx, fy, 0], kind: 'swap', note: 'güç: roomWall + roomFloor (açık) ↔ roomWallOff + roomFloorOff (kapalı)' });
    m.box(roomW - 0.008, roomH - 0.004, 0.006, 0, 'roomWall', [cx, fy + roomH / 2, 0.152], [0, 0, 0], P, { cast: false });
    m.box(roomW, 0.004, sd - 0.012, 0, 'roomFloor', [cx, fy + 0.002, -0.006], [0, 0, 0], P, { cast: false });
    m.box(roomW, 0.003, sd - 0.012, 0, 'roomWall', [cx, fy + roomH - 0.0015, -0.006], [0, 0, 0], P, { cast: false });
    for (const sx of [-1, 1]) m.box(0.004, roomH - 0.007, sd - 0.012, 0, 'roomWall', [cx + sx * (roomW / 2 - 0.002), fy + 0.004 + (roomH - 0.007) / 2, -0.006], [0, 0, 0], P, { cast: false });
    const ly = fy + roomH - 0.003, L = `Lamp_${k}`;
    m.lathe([[0.012, ly - 0.010], [0.0105, ly - 0.0075], [0.005, ly - 0.003], [0.0035, ly]], 10, 'paintBlack', [cx, 0, 0.02], [0, 0, 0], 'Body', { caps: false, cast: false });
    m.part(L, { pivot: [cx, ly - 0.009, 0.02], kind: 'swap', note: 'ampul: bulbGlass (oda güçlü ve seviye > 0) ↔ roomBulbOff' });
    m.add(gSphere(0.0058, 10, 6), 'bulbGlass', [cx, ly - 0.0095, 0.02], [0, 0, 0], L, { cast: false });
    m.light(`RoomLight_${k}`, { part: L, pos: [cx, ly - 0.03, 0.0], type: 'point', color: '#FFD8A0', shadows: false, note: 'Rev2: 0,06 × dioLight (K4’te dioLight 2,4), menzil 0,45 m; yalnız yanıkken' });
    m.box(roomW, 0.006, 0.006, 0.0015, 'brassWorn', [cx, fy + 0.003, -0.157], [0, 0, 0], 'Body', { cast: false });
    const dx = cx + (cx < 0 ? 1 : -1) * (roomW / 2 - 0.0045);                                                      // steel door to the shaft
    m.box(0.003, 0.078, 0.036, 0.0008, 'paintOliveDark', [dx, fy + 0.004 + 0.039, 0.095], [0, 0, 0], 'Body', { cast: false });
    const S = `Sel_${k}`;
    m.part(S, { pivot: [cx, fy, -0.1625], kind: 'swap', note: 'seçili oda çerçevesi: görünür/gizli (selYellow, hafif ışıklı)' });
    m.box(roomW + 0.012, 0.006, 0.003, 0, 'selYellow', [cx, fy + roomH + 0.003, -0.1625], [0, 0, 0], S, { cast: false });
    m.box(roomW + 0.012, 0.006, 0.003, 0, 'selYellow', [cx, fy + 0.003, -0.1635], [0, 0, 0], S, { cast: false });
    for (const sx of [-1, 1]) m.box(0.006, roomH + 0.012, 0.003, 0, 'selYellow', [cx + sx * (roomW / 2 + 0.003), fy + roomH / 2, -0.1625], [0, 0, 0], S, { cast: false });
    const band = fy - 0.0325;
    m.anchor(`Card_${k}`, { pos: [cx, band - 0.0315, -0.16], note: `RoomCardHolder orijini (bant ön yüzü, kart merkezi y ${band.toFixed(4)}); kart: ${RT[k][0]}` });
    m.anchor(`Slots_${k}`, { pos: [cx, fy + 0.004, -0.09], note: 'kişi yuvaları (PegSlot + Peg): x = cx − 0,13 + i · 0,26/(kapasite − 1), y taban, z −0,09' });
    m.anchor(`Props_${k}`, { pos: [cx, fy + 0.004, 0], note: `DioramaRoomProps_${k} orijini (hasarlıysa DioramaDamage da buraya)` });
  }
  // DEPO band: concrete back, floor plate, lamp, label plate
  m.box(iw, 0.094, 0.006, 0, 'concreteWall', [0, 0.553, 0.155], [0, 0, 0], 'Body', { cast: false });
  m.box(iw, 0.004, sd - 0.012, 0, 'floor', [0, 0.508, -0.006], [0, 0, 0], 'Body', { cast: false });
  m.lathe([[0.012, 0.590], [0.0105, 0.5925], [0.005, 0.597], [0.0035, 0.600]], 10, 'paintBlack', [0.12, 0, 0.02], [0, 0, 0], 'Body', { caps: false, cast: false });
  m.part('Lamp_depo', { pivot: [0.12, 0.591, 0.02], kind: 'swap', note: 'DEPO ampulü: bulbGlass ↔ roomBulbOff' });
  m.add(gSphere(0.0058, 10, 6), 'bulbGlass', [0.12, 0.5905, 0.02], [0, 0, 0], 'Lamp_depo', { cast: false });
  m.light('RoomLight_depo', { part: 'Lamp_depo', pos: [0.0, 0.585, 0.0], type: 'point', color: '#FFD8A0', shadows: false, note: 'Rev2: 0,07 × dioLight, menzil 0,6 m' });
  m.box(0.09, 0.02, 0.002, 0.0006, 'plate', [-0.325, 0.607, -0.161], [0, 0, 0], 'Body', { cast: false });
  m.text('TXT_Depo', { center: [-0.325, 0.607, -0.1622], size: [0.08, 0.016], role: 'plateLabel', capMm: 8, sample: 'DEPO', sampleEn: 'STORE' });
  for (let i = 0; i < 10; i++) if (i === 0 || i === 5) m.anchor(`DepotCrate_${i}`, { pos: [-0.335 + (i % 5) * 0.066, 0.510 + Math.floor(i / 5) * 0.044, 0.03], note: 'DepotCrate yuvaları: x = −0,335 + c · 0,066 (c 0–4), y 0,510 / 0,554 (iki sıra), z 0,03; 10 sandık = 30 erzak' });
  m.anchor('DepotBin_0', { pos: [0.01, 0.510, 0.03], note: 'DepotBin yuvaları: x = 0,01 + i · 0,044 (i 0–4), y 0,510, z 0,03; 1 kova = 5 malzeme' });
  m.anchor('DepotPlate', { pos: [0.31, 0.510, 0.04], note: 'DepotPlate (3 yuvalı plaka rafı)' });
  m.anchor('DepotSlate', { pos: [0.11, 0.522, 0.152], note: 'DepotSlate (DEPO arka duvarı, duvar düzlemi z 0,152)' });
  // soil strata + surface
  for (const [mt, y0, h] of [['soil3', 0.614, 0.012], ['soil2', 0.626, 0.014], ['soil1', 0.640, 0.012], ['rubble', 0.652, 0.008]]) m.box(iw, h, sd, 0.002, mt, [0, y0 + h / 2, 0], [0, 0, 0], 'Body', { cast: false });
  const top = 0.66;
  for (const [x, z, w, h, d, rz, ry] of [[-0.31, 0.02, 0.05, 0.042, 0.03, 0, 4], [-0.255, -0.05, 0.03, 0.02, 0.05, 9, 20], [-0.15, 0.03, 0.036, 0.052, 0.032, -3, -8],
    [-0.06, -0.02, 0.06, 0.016, 0.04, 0, 12], [0.06, 0.04, 0.05, 0.03, 0.03, 5, -5], [0.15, -0.04, 0.04, 0.014, 0.05, 0, 30], [0.32, 0.03, 0.045, 0.038, 0.04, -6, 2]])
    m.box(w, h, d, 0.002, 'ruin', [x, top + h / 2 - 0.002, z], [0, ry, rz]);
  m.box(0.008, 0.03, 0.03, 0.0015, 'ruin', [-0.151, top + 0.046, 0.03], [0, -8, 14]);                                 // broken wall top
  segBox(m, [-0.22, top + 0.003, 0.0], [-0.17, top + 0.03, -0.02], 0.006, 0.006, 0.0015, 'steelCast');                 // bent girder
  segBox(m, [-0.17, top + 0.03, -0.02], [-0.135, top + 0.022, -0.05], 0.006, 0.006, 0.0015, 'steelCast');
  for (const [x, z, s] of [[-0.09, 0.06, 0.012], [0.0, -0.06, 0.014], [0.1, 0.0, 0.01], [0.2, 0.07, 0.012], [-0.28, 0.08, 0.01], [0.27, -0.07, 0.011]])
    m.box(s, s * 0.7, s * 0.9, 0.0015, 'rubble', [x, top + s * 0.3, z], [8, x * 300, 12], 'Body', { cast: false });
  // AI mast with the red beacon (red = AI)
  m.box(0.022, 0.008, 0.022, 0.002, 'ruin', [0.24, top + 0.004, -0.02]);
  segCyl(m, [0.24, top + 0.008, -0.02], [0.24, 0.708, -0.02], 0.0038, 0.0024, 8, 'steelCast');
  m.box(0.03, 0.004, 0.004, 0.001, 'steelCast', [0.24, 0.698, -0.02], [0, 0, 0], 'Body', { cast: false });
  m.part('Beacon', { pivot: [0.24, 0.714, -0.02], kind: 'swap', note: 'YZ feneri: mastRed (yanar, yavaş nabız) ↔ beaconOff' });
  m.add(gSphere(0.0062, 10, 7), 'mastRed', [0.24, 0.714, -0.02], [0, 0, 0], 'Beacon', { cast: false });
  m.light('BeaconGlow', { part: 'Beacon', pos: [0.24, 0.714, -0.03], type: 'emissive', note: 'yalnız emissive (bloom yok); ışık bileşeni yok' });
  m.meta.place = [shelfW(DIO.shelfLocal, 0, '2. kat (y 0,78), x 0,20 (Rev2 dio)')];
  return m;
}

// ---------------------------------------------------------------- room props (origin = room floor surface centre)
function pegLying(m, p, part, mat = 'pawn', headMat = 'pawn') { // a peg lying along +x (head toward −x) at centre p
  m.lathe([[0, 0], [0.0105, 0], [0.0088, 0.012], [0.0078, 0.026], [0, 0.026]], 8, mat, [p[0] + 0.016, p[1], p[2]], [0, 0, 90], part);
  m.add(gSphere(0.0092, 8, 6), headMat, [p[0] - 0.019, p[1], p[2]], [0, 0, 0], part);
}
function propsModel(k, list, note) {
  const m = new Model(`DioramaRoomProps_${k}`, { family: 'shelf', budget: 'small', listName: `Oda donanımı · ${RT[k][0].toLowerCase()}`, origin: 'oda tabanı ortası (taban kaplaması üstü, y = 0)', note });
  const [cx, fy] = DIO.rooms[k]; m.meta.place = [DPLACE([cx, fy + 0.004, 0], `${RT[k][0]} odası (Props_${k} çapası)`)];
  return m;
}
export function PropsYasam() {
  const m = propsModel('yasam', null, 'İki katlı iki ranza (battaniye, yastık), masa ve tabure. Çıktılar: Output = doldurulan erzak sandığı (yaşam alanında görevli varken), Reserve = alt ranzada yatan yedek uzman.');
  for (const bx of [-0.1225, -0.0025]) {
    for (const px of [-0.051, 0.051]) for (const pz of [0.064, 0.116]) m.box(0.005, 0.088, 0.005, 0, 'paintOliveDark', [bx + px, 0.044, pz], [0, 0, 0], 'Body', { cast: false });
    for (const by of [0.011, 0.061]) {
      m.box(0.106, 0.006, 0.054, 0, 'paintOlive', [bx, by, 0.09]);
      m.box(0.096, 0.008, 0.05, 0.002, 'blanket', [bx + 0.004, by + 0.007, 0.09]);
      m.box(0.018, 0.007, 0.036, 0, 'paper', [bx - 0.038, by + 0.0145, 0.09], [0, 0, 0], 'Body', { cast: false });
    }
  }
  m.box(0.052, 0.004, 0.04, 0, 'wood', [0.1075, 0.034, 0.07]); m.box(0.005, 0.032, 0.005, 0, 'wood', [0.1075, 0.016, 0.07], [0, 0, 0], 'Body', { cast: false });
  m.box(0.026, 0.003, 0.02, 0, 'wood', [0.1075, 0.0015, 0.07], [0, 0, 0], 'Body', { cast: false });
  m.cyl(0.0085, 0.02, 8, 0, 'wood', [0.08, 0.01, 0.04]);
  m.part('Output', { pivot: [0.06, 0, 0.0], kind: 'swap', note: 'görünür: yaşam alanında görevli var (erzak toplanıyor)' });
  m.box(0.045, 0.022, 0.035, 0.002, 'woodRaw', [0.06, 0.011, 0.0], [0, 8, 0], 'Output');
  for (let i = 0; i < 3; i++) m.cyl(0.006, 0.014, 8, 0, 'galv', [0.046 + i * 0.014, 0.029, 0.0], [0, 0, 0], 'Output', { cast: false });
  m.part('Reserve', { pivot: [-0.1225, 0.022, 0.09], kind: 'swap', note: 'görünür: yedek uzman var (alt ranzada yatıyor, battaniye göğsüne kadar)' });
  pegLying(m, [-0.118, 0.0315, 0.09], 'Reserve');
  m.box(0.05, 0.012, 0.052, 0.002, 'blanket', [-0.096, 0.028, 0.09], [0, 0, 0], 'Reserve');
  return m;
}
export function PropsTelsiz() {
  const m = propsModel('telsiz', null, 'Telsiz masası ve tüplü telsiz, cihaz dolabı, operatör taburesi, tavana çıkan anten kablosu. Çıktılar: Dial = telsiz kadranı yanar (görevli varken), Headset = masada kulaklık, Listen = oda tavanındaki amber lamba (dinleniyor: alarm yavaşlar).');
  m.box(0.13, 0.006, 0.06, 0.0015, 'wood', [-0.0075, 0.046, 0.08]);
  for (const sx of [-1, 1]) m.box(0.006, 0.043, 0.05, 0, 'wood', [-0.0075 + sx * 0.055, 0.0215, 0.08], [0, 0, 0], 'Body', { cast: false });
  m.box(0.075, 0.042, 0.04, 0.003, 'radioBody', [-0.0175, 0.07, 0.085]);
  for (const kx of [0.006, 0.015]) m.cyl(0.004, 0.004, 8, 0, 'bakeliteBlack', [kx, 0.058, 0.063], [-90, 0, 0], 'Body', { cast: false });
  m.part('Dial', { pivot: [-0.025, 0.077, 0.0645], kind: 'swap', note: 'kadran: jewelAmberOn (görevli var) ↔ jewelAmberOff' });
  m.box(0.042, 0.014, 0.002, 0.0005, 'jewelAmberOn', [-0.025, 0.077, 0.0645], [0, 0, 0], 'Dial', { cast: false });
  m.light('DialGlow', { part: 'Dial', pos: [-0.025, 0.08, 0.04], type: 'point', color: '#FFA850', shadows: false, note: 'Rev2: 0,05 × dioLight, menzil 0,25 m; yalnız görevli varken' });
  tubeU(m, [[-0.04, 0.091, 0.095], [-0.042, 0.12, 0.11], [-0.03, 0.163, 0.13]], 0.002, 4, 6, 'rubber', 'Body', { cast: false });
  m.part('Headset', { pivot: [0.04, 0.049, 0.08], kind: 'swap', note: 'görünür: telsiz başında görevli var' });
  m.add(gTorus(0.012, 0.0022, 4, 10, Math.PI), 'rubber', [0.04, 0.052, 0.08], [-90, 0, 0], 'Headset', { cast: false });
  for (const sx of [-1, 1]) m.cyl(0.0055, 0.004, 8, 0, 'rubber', [0.04 + sx * 0.012, 0.051, 0.08], [0, 0, 90], 'Headset', { cast: false });
  m.box(0.03, 0.05, 0.03, 0.002, 'paintOliveDark', [0.1225, 0.025, 0.12]);
  m.cyl(0.006, 0.002, 10, 0, 'dialCream', [0.1225, 0.035, 0.104], [-90, 0, 0], 'Body', { cast: false });
  m.cyl(0.009, 0.004, 10, 0, 'wood', [-0.02, 0.022, 0.025]); m.cyl(0.0025, 0.02, 6, 0, 'steelCast', [-0.02, 0.01, 0.025], [0, 0, 0], 'Body', { cast: false });
  m.box(0.008, 0.008, 0.012, 0, 'paintBlack', [0.152, 0.151, -0.12]);
  m.part('Listen', { pivot: [0.146, 0.151, -0.12], kind: 'swap', note: 'amber lamba: jewelAmberOn (biri dinliyor, alarm yavaşlar) ↔ jewelAmberOff' });
  m.add(gSphere(0.0048, 8, 6), 'jewelAmberOn', [0.146, 0.151, -0.12], [0, 0, 0], 'Listen', { cast: false });
  return m;
}
export function PropsAtolye() {
  const m = propsModel('atolye', null, 'Tezgâh, arka duvarda alet panosu (çekiç, anahtar, testere), mengene, pota. Çıktılar: Mould = döküm kalıbı kızarır (plaka dökülürken), Plates = tezgâha dayalı dökülmüş plakalar.');
  m.box(0.17, 0.006, 0.055, 0.0015, 'wood', [-0.0125, 0.041, 0.10]);
  for (const x of [-0.0875, 0.0625]) m.box(0.006, 0.038, 0.05, 0.0015, 'wood', [x, 0.019, 0.10], [0, 0, 0], 'Body', { cast: false });
  m.box(0.16, 0.07, 0.004, 0.001, 'pegboard', [-0.0125, 0.101, 0.150], [0, 0, 0], 'Body', { cast: false });
  m.box(0.004, 0.032, 0.003, 0, 'wood', [-0.07, 0.10, 0.1475], [0, 0, 8], 'Body', { cast: false }); m.box(0.014, 0.007, 0.004, 0, 'steelCast', [-0.072, 0.117, 0.147], [0, 0, 8], 'Body', { cast: false });
  m.box(0.005, 0.034, 0.003, 0, 'steelBare', [-0.045, 0.10, 0.1475], [0, 0, -6], 'Body', { cast: false });
  m.box(0.045, 0.013, 0.002, 0, 'steelBare', [0.01, 0.112, 0.148], [0, 0, 0], 'Body', { cast: false }); m.box(0.012, 0.016, 0.004, 0, 'wood', [0.038, 0.11, 0.147], [0, 0, 0], 'Body', { cast: false });
  m.box(0.02, 0.015, 0.02, 0.002, 'steelCast', [0.0475, 0.0515, 0.09]); m.box(0.003, 0.003, 0.026, 0, 'steelBare', [0.0475, 0.05, 0.075], [0, 0, 0], 'Body', { cast: false });
  m.box(0.07, 0.012, 0.04, 0.002, 'castIron', [-0.0425, 0.050, 0.095]);
  m.part('Mould', { pivot: [-0.0425, 0.0565, 0.095], kind: 'swap', note: 'kalıp gözü: moltenGlow (döküm sürüyor) ↔ castIron (soğuk)' });
  m.box(0.054, 0.002, 0.024, 0, 'moltenGlow', [-0.0425, 0.0565, 0.095], [0, 0, 0], 'Mould', { cast: false });
  m.light('MouldGlow', { part: 'Mould', pos: [-0.0425, 0.07, 0.07], type: 'point', color: '#FF8A3A', shadows: false, note: 'yalnız döküm sürerken; 0,05 × dioLight, menzil 0,25 m' });
  m.cyl(0.009, 0.016, 10, 0.0015, 'castIron', [0.105, 0.008, 0.11]);
  m.part('Plates', { pivot: [-0.07, 0, 0.06], kind: 'swap', note: 'görünür: depoda dökülmüş plaka var (1–3)' });
  for (let i = 0; i < 2; i++) m.box(0.04, 0.05, 0.004, 0.001, 'galv', [-0.07 + i * 0.012, 0.025, 0.066 + i * 0.006], [-14, 6 - i * 10, 0], 'Plates');
  return m;
}
export function PropsRevir() {
  const m = propsModel('revir', null, 'Katlanır sahra karyolası, beyaz emaye ilk yardım dolabı (işaret dokuda), üç kanatlı paravan. Çıktı: Patient = karyolada sargılı yatan uzman (revir çalışırken). Hasarlı hâl için DioramaDamage modeli eklenir.');
  const bx = -0.0475, bz = 0.09;
  m.box(0.12, 0.004, 0.05, 0.001, 'tape', [bx, 0.024, bz]);
  for (const sz of [-1, 1]) m.box(0.13, 0.005, 0.005, 0.001, 'woodLight', [bx, 0.024, bz + sz * 0.027], [0, 0, 0], 'Body', { cast: false });
  for (const ex of [-0.055, 0.055]) for (const sz of [-1, 1]) segBox(m, [bx + ex - 0.008, 0, bz + sz * 0.024], [bx + ex + 0.008, 0.023, bz + sz * 0.024], 0.004, 0.004, 0, 'woodLight', 'Body', { cast: false });
  m.box(0.05, 0.09, 0.04, 0.003, 'enamel', [0.1125, 0.045, 0.11]);
  m.box(0.002, 0.08, 0.035, 0, 'paintBlack', [0.1125 - 0.0255, 0.045, 0.11], [0, 0, 0], 'Body', { cast: false }); // door edge shadow line (hinge side)
  m.box(0.026, 0.026, 0.001, 0, 'firstAidSign', [0.1125, 0.058, 0.0895], [0, 0, 0], 'Body', { cast: false });
  m.box(0.003, 0.012, 0.004, 0, 'chrome', [0.132, 0.045, 0.0885], [0, 0, 0], 'Body', { cast: false });
  for (const [x, z, ry] of [[-0.142, 0.05, 60], [-0.128, 0.083, -30], [-0.142, 0.115, 60]]) m.box(0.036, 0.075, 0.003, 0.001, 'tape', [x, 0.0395, z], [0, ry, 0]);
  m.part('Patient', { pivot: [bx, 0.026, bz], kind: 'swap', note: 'görünür: revirde yatan (sargılı) uzman var' });
  pegLying(m, [bx + 0.004, 0.0365, bz], 'Patient');
  m.add(gTorus(0.0094, 0.0028, 5, 12), 'enamel', [bx - 0.015, 0.0375, bz], [0, 0, 90], 'Patient', { cast: false });
  m.box(0.05, 0.013, 0.052, 0.003, 'blanket', [bx + 0.03, 0.033, bz], [0, 0, 0], 'Patient');
  return m;
}
export function DioramaDamage() {
  const m = new Model('DioramaDamage', { family: 'shelf', budget: 'small', listName: 'Hasarlı oda (moloz, düşmüş kiriş, çatlak)', origin: 'oda tabanı ortası (y = 0)',
    note: 'Hasarlı odaya (Rev2: atölye ve revir onarım bekler) eklenen katman: moloz parçaları, tavandan düşüp çapraz duran ahşap kiriş, arka duvarda çatlak (koyu şerit).' });
  for (const [x, z, s, h, ry, rz] of [[0.012, -0.02, 0.03, 0.02, 0, 17], [0.052, 0.02, 0.025, 0.018, 40, 0], [-0.088, 0.03, 0.02, 0.015, 80, 23], [0.082, -0.06, 0.028, 0.02, 120, 0], [-0.028, -0.07, 0.022, 0.014, 160, 11], [0.032, 0.0, 0.018, 0.016, 200, 34]])
    m.box(s, h, s * 0.9, 0.002, 'rubble', [x, h * 0.4, z], [0, ry, rz]);
  m.box(0.24, 0.016, 0.016, 0.002, 'woodRaw', [-0.0075, 0.071, -0.01], [0, 0, 24]);
  m.box(0.012, 0.12, 0.001, 0, 'crack', [0.042, 0.085, 0.148], [0, 0, 23], 'Body', { cast: false });
  m.box(0.01, 0.07, 0.001, 0, 'crack', [0.005, 0.115, 0.148], [0, 0, -29], 'Body', { cast: false });
  m.meta.place = [DPLACE([DIO.rooms.revir[0], DIO.rooms.revir[1] + 0.004, 0], 'revir (hasarlı)'), DPLACE([DIO.rooms.atolye[0], DIO.rooms.atolye[1] + 0.004, 0], 'atölye (hasarlı)')];
  return m;
}

// ---------------------------------------------------------------- DEPO band items
export function DepotCrate() {
  const m = new Model('DepotCrate', { family: 'shelf', budget: 'small', listName: 'DEPO sandığı (1 sandık = 3 erzak)', origin: 'taban ortası (y = 0)',
    note: 'Maket ölçeğinde tahta erzak sandığı. Dolu: Lid görünür, tenekeler gizli. Kısmen dolu son sandık: Lid gizli, Tin1–Tin3’ten erzak kadar görünür (tenekeler kenardan taşar, önden okunur).' });
  const w = 0.058, d = 0.066, h = 0.040, t = 0.004;
  m.box(w, t, d, 0.001, 'woodRaw', [0, t / 2, 0]);
  for (const sz of [-1, 1]) m.box(w, h - t, t, 0.001, 'woodRaw', [0, t + (h - t) / 2, sz * (d / 2 - t / 2)]);
  for (const sx of [-1, 1]) m.box(t, h - t, d - 2 * t, 0.001, 'woodRaw', [sx * (w / 2 - t / 2), t + (h - t) / 2, 0]);
  for (const y of [0.008, 0.033]) m.box(w + 0.004, 0.006, 0.003, 0.001, 'wood', [0, y, -d / 2 - 0.0015], [0, 0, 0], 'Body', { cast: false });
  m.part('Lid', { pivot: [0, h, 0], kind: 'swap', note: 'görünür: sandık dolu (3 erzak)' });
  m.box(w + 0.004, 0.004, d + 0.004, 0.001, 'woodRaw', [0, h + 0.002, 0], [0, 0, 0], 'Lid');
  for (const sx of [-1, 1]) m.box(0.008, 0.003, d, 0.0008, 'wood', [sx * 0.018, h + 0.0055, 0], [0, 0, 0], 'Lid', { cast: false });
  for (let i = 0; i < 3; i++) { const P = `Tin${i + 1}`; m.part(P, { pivot: [-0.018 + i * 0.018, t, 0.004 * (i - 1)], kind: 'swap', note: 'kısmen dolu sandıkta erzak başına bir teneke' });
    m.cyl(0.0078, 0.046, 10, 0.0012, 'galv', [-0.018 + i * 0.018, t + 0.023, 0.004 * (i - 1)], [0, 0, 0], P, { cast: false }); }
  m.meta.place = [DPLACE([-0.335, 0.510, 0.03], 'ilk yuva; x adımı 0,066 (5 sütun), ikinci sıra y 0,554')];
  return m;
}
export function DepotBin() {
  const m = new Model('DepotBin', { family: 'shelf', budget: 'small', listName: 'DEPO kovası (1 kova = 5 malzeme)', origin: 'taban ortası (y = 0)',
    note: 'Önü alçak galvaniz istif kovası (önden içi görünür). Fill1–Fill5: her biri 1 malzeme; 3. katmandan sonrası ön kenarın üstünden okunur.' });
  const w = 0.038, d = 0.05, h = 0.042, t = 0.0028, lipH = 0.018;
  m.box(w, t, d, 0.0008, 'galvBin', [0, t / 2, 0]);
  m.box(w, h, t, 0.0008, 'galvBin', [0, h / 2, d / 2 - t / 2]);
  m.box(w, lipH, t, 0.0008, 'galvBin', [0, lipH / 2, -d / 2 + t / 2]);
  for (const sx of [-1, 1]) m.extrude([[d / 2, 0], [d / 2, h], [-d / 2, lipH], [-d / 2, 0]], t, 0.0006, 'galvBin', [sx * (w / 2 - t / 2), 0, 0], [0, 90, 0]);
  m.box(0.016, 0.004, 0.004, 0.001, 'galvBin', [0, lipH - 0.004, -d / 2 - 0.002], [0, 0, 0], 'Body', { cast: false }); // finger tab
  for (let i = 0; i < 5; i++) { const P = `Fill${i + 1}`; const y = t + i * 0.0068;
    m.part(P, { pivot: [0, y, 0], kind: 'swap', note: `görünür: kovada ≥ ${i + 1} malzeme` });
    m.box(w - 2 * t - 0.001, 0.0066, d - 2 * t - 0.001, 0.0015, 'scrapMetal', [0, y + 0.0033, 0], [0, i * 7 - 14, 0], P, { cast: false }); }
  m.meta.place = [DPLACE([0.01, 0.510, 0.03], 'ilk yuva; x adımı 0,044 (5 kova)')];
  return m;
}
export function DepotPlate() {
  const m = new Model('DepotPlate', { family: 'shelf', budget: 'small', listName: 'Plaka rafı (3 yuva)', origin: 'taban ortası (y = 0)',
    note: 'Atölyede dökülen zırh plakaları için üç yuvalı çelik raf. Plate1–Plate3: depodaki plaka sayısı kadar görünür.' });
  m.box(0.116, 0.006, 0.06, 0.0015, 'paintOliveDark', [0, 0.003, 0]);
  for (const sx of [-1, 1]) m.box(0.006, 0.08, 0.06, 0.0015, 'paintOliveDark', [sx * 0.055, 0.04, 0]);
  m.box(0.104, 0.006, 0.006, 0.0015, 'paintOliveDark', [0, 0.077, 0.024]);
  for (const x of [-0.0175, 0.0175]) m.box(0.004, 0.008, 0.056, 0.001, 'paintOliveDark', [x, 0.01, 0], [0, 0, 0], 'Body', { cast: false });
  for (let i = 0; i < 3; i++) { const P = `Plate${i + 1}`; m.part(P, { pivot: [-0.035 + i * 0.035, 0.006, 0], kind: 'swap', note: `görünür: depoda ≥ ${i + 1} plaka` });
    m.box(0.006, 0.068, 0.05, 0.0015, 'galv', [-0.035 + i * 0.035, 0.006 + 0.034, 0], [0, 0, 0], P); }
  m.meta.place = [DPLACE([0.31, 0.510, 0.04], 'DEPO sağ uç')];
  return m;
}
export function DepotSlate() {
  const m = new Model('DepotSlate', { family: 'shelf', budget: 'small', listName: 'Tebeşir tahtası (DEPO)', origin: 'arka yüz alt kenar ortası (duvar düzlemi z = 0)',
    note: 'DEPO arka duvarında ahşap çerçeveli küçük kara tahta ve tebeşir rafı. İki satır TMP (tebeşir rengi #E6E2D4): erzak ve kaç vardiya yeteceği.' });
  const W = 0.20, H = 0.072, fw = 0.007;
  m.box(W - 2 * fw, H - 2 * fw, 0.004, 0, 'depotSlate', [0, H / 2, -0.002], [0, 0, 0], 'Body', { cast: false });
  for (const sy of [0, 1]) m.box(W, fw, 0.007, 0.0015, 'woodRaw', [0, sy ? H - fw / 2 : fw / 2, -0.0035]);
  for (const sx of [-1, 1]) m.box(fw, H - 2 * fw, 0.007, 0.0015, 'woodRaw', [sx * (W / 2 - fw / 2), H / 2, -0.0035]);
  m.box(W - 0.02, 0.004, 0.012, 0.001, 'woodRaw', [0, 0.002, -0.009], [0, 0, 0], 'Body', { cast: false });
  m.cyl(0.0028, 0.022, 6, 0.0008, 'chalkDecal', [0.05, 0.0068, -0.009], [0, 0, 90], 'Body', { cast: false });
  m.text('TXT_Line1', { center: [0, 0.0505, -0.0042], size: [0.17, 0.016], role: 'chalkSlateTex', capMm: 10, align: 'left', sample: 'ERZAK', sampleEn: 'RATIONS' });
  m.text('TXT_Line2', { center: [0, 0.0265, -0.0042], size: [0.17, 0.02], role: 'chalkSlateTex', capMm: 12, align: 'left', sample: '15 vardiya', sampleEn: '15 shifts' });
  m.meta.place = [DPLACE([0.11, 0.522, 0.152], 'DEPO arka duvarı')];
  return m;
}

// ---------------------------------------------------------------- room card + holder
export function RoomCardHolder() {
  const m = new Model('RoomCardHolder', { family: 'shelf', budget: 'small', listName: 'Oda kartı tutucusu (pirinç)', origin: 'arka yüz alt kenar ortası (bant ön yüzü, z = 0)',
    note: 'Bant önüne vidalı pirinç kanal: üst ve alt dudak kartın 3 mm’sini tutar, yanlarda durdurucu. Şeritler ≥ 6 mm (K1’de titremesin), mat pirinç. Sel: seçili oda için sarı çerçeve.' });
  const W = 0.244, H = 0.063;
  for (const [y0, sgn] of [[0, 1], [H, -1]]) {
    m.box(W, 0.003, 0.0025, 0, 'brassWorn', [0, y0 + sgn * 0.0015, -0.00125], [0, 0, 0], 'Body', { cast: false });
    m.box(W, 0.006, 0.0025, 0.0008, 'brassWorn', [0, y0 + sgn * 0.003, -0.00375], [0, 0, 0], 'Body', { cast: false });
  }
  for (const sx of [-1, 1]) m.box(0.006, H, 0.005, 0.0008, 'brassWorn', [sx * (W / 2 - 0.003), H / 2, -0.0025], [0, 0, 0], 'Body', { cast: false });
  for (const sx of [-1, 1]) for (const y of [0.003, H - 0.003]) screwHead(m, [sx * 0.108, y, -0.005], [0, 0, -1], 0.0026, 0.0014, 'brass', 'Body', 6);
  m.part('Sel', { pivot: [0, H / 2, -0.0055], kind: 'swap', note: 'seçili oda: sarı çerçeve görünür' });
  for (const sy of [-1, 1]) m.box(W + 0.012, 0.006, 0.002, 0, 'selYellow', [0, H / 2 + sy * (H / 2 + 0.003), -0.0055], [0, 0, 0], 'Sel', { cast: false });
  for (const sx of [-1, 1]) m.box(0.006, H + 0.012, 0.002, 0, 'selYellow', [sx * (W / 2 + 0.003), H / 2, -0.0055], [0, 0, 0], 'Sel', { cast: false });
  m.anchor('Card', { pos: [0, 0.003, -0.0002], note: 'RoomCard orijini (kart alt kenarı, arka yüz)' });
  m.meta.place = Object.entries(DIO.rooms).map(([k, [cx, fy]]) => DPLACE([cx, fy - 0.0325 - 0.0315, -0.16], `${RT[k][0]} kart bandı (Card_${k})`));
  return m;
}
export function RoomCard() {
  const m = new Model('RoomCard', { family: 'shelf', budget: 'small', listName: 'Oda kartı', origin: 'alt kenar ortası, arka yüz (tutucuda kart yuvası)',
    note: 'Krem karton 0,23 × 0,057 (üst ve alt 3 mm tutucu dudağının arkasında kalır). Satır 1 oda adı; altında basılı kırmızı çizgi (doku). Satır 2: seviye · KİŞİ İŞARETİ · görevli/kapasite (açık madde kapandı: “SV 1 · ⚲ 2/2”). Sağda durum damgası (DURDU / HASARLI).' });
  const W = 0.23, H = 0.057, z = -0.0009;
  m.box(W, H, 0.0008, 0, 'roomCard', [0, H / 2, -0.0004], [0, 0, 0], 'Body', { cast: false });
  m.text('TXT_Name', { center: [-0.003, 0.0405, z], size: [0.21, 0.02], role: 'roomCardNameTex', capMm: 12, align: 'left', sample: 'YAŞAM ALANI', sampleEn: 'LIVING QUARTERS', note: 'K4’te büyük harf ≈ 13 px' });
  m.text('TXT_Level', { center: [-0.077, 0.0155, z], size: [0.06, 0.015], role: 'roomCardLineTex', capMm: 9, align: 'left', sample: 'SV 1 ·', sampleEn: 'LV 1 ·' });
  m.text('ICON_Person', { center: [-0.038, 0.0155, z], size: [0.012, 0.014], role: 'sprite icon_person (Image / SpriteRenderer, mürekkep #2E2820)', capMm: 11, sample: '⚲', sampleEn: '⚲', note: 'kişi işareti: görevli sayısının önünde; yükseklik büyük harf + %20' });
  m.text('TXT_Staff', { center: [-0.004, 0.0155, z], size: [0.05, 0.015], role: 'roomCardLineTex', capMm: 9, align: 'left', sample: '2/2', sampleEn: '2/2', note: 'görevli/kapasite' });
  m.text('TXT_Stamp', { center: [0.068, 0.0165, z], size: [0.08, 0.024], up: [0.0698, 0.9976, 0], role: 'stamp (kırmızı #9A1E14, çift çizgi çerçeve, −4°)', capMm: 8, sample: 'HASARLI', sampleEn: 'DAMAGED', note: 'durum yoksa boş; DURDU / STOPPED' });
  m.meta.place = [DPLACE([DIO.rooms.yasam[0], DIO.rooms.yasam[1] - 0.0325 - 0.0285, -0.1602], 'tutucu içinde (RoomCardHolder.Card çapası); her oda için bir kart')];
  return m;
}

// ---------------------------------------------------------------- clipboard stand, idle tray, pegs
export function ClipboardStand() {
  const m = new Model('ClipboardStand', { family: 'shelf', budget: 'small', listName: 'Özet panosu ve ayağı', origin: 'ayak tabanı ortası (raf yüzeyi, y = 0)',
    note: 'Ahşap ayakta 8° geriye yaslı sert pano, yaylı kıskaç. Kâğıdın tamamı yazı yeri (K4’te SIĞINAK ÖZETİ arayüz olarak çizilir; layout3d.clipboard). Pano alt kenarı raf yereli (−0,46 · 0,86 · −0,02).' });
  const c8 = Math.cos(8 * D2R), s8 = Math.sin(8 * D2R); const F = [0, 0.08, -0.03], u = [0, c8, s8], n = [0, -s8, c8];
  const at = (a, b) => add(add(F, scl(u, a)), scl(n, b));
  m.box(0.36, 0.025, 0.11, 0.003, 'woodVarnish', [0, 0.0125, 0.0], [0, 0, 0], 'Body', { skip: ['-y'] });
  m.box(0.36, 0.055, 0.03, 0.003, 'woodVarnish', [0, 0.0525, -0.03]);
  m.box(0.36, 0.008, 0.008, 0.002, 'woodVarnish', [0, 0.084, -0.049]);                                   // front stop lip
  m.box(0.40, 0.60, 0.008, 0.003, 'clipboard', at(0.30, 0.004), [8, 0, 0]);
  m.box(0.37, 0.54, 0.001, 0, 'paper', at(0.275, -0.0005), [8, 0, 0], 'Body', { cast: false });
  m.box(0.13, 0.03, 0.004, 0.0012, 'steelBare', at(0.578, -0.002), [8, 0, 0]);
  m.cyl(0.006, 0.12, 10, 0.001, 'steelBare', at(0.563, -0.008), [8, 0, 90]);
  m.box(0.112, 0.004, 0.034, 0.0012, 'steelBare', at(0.551, -0.016), [8 - 62, 0, 0]);
  m.box(0.12, 0.012, 0.004, 0.001, 'steelBare', at(0.545, -0.003), [8, 0, 0], 'Body', { cast: false });
  segBox(m, at(0.32, 0.012), [0, 0.03, 0.045], 0.04, 0.012, 0.002, 'woodVarnish');
  const pc = at(0.275, -0.0012);
  m.text('TXT_Paper', { center: pc, size: [0.37, 0.54], normal: [0, s8, -c8], up: u, role: 'panelTex (kâğıdın tamamı; K4’te arayüz)', capMm: 13, sample: 'SIĞINAK ÖZETİ', sampleEn: 'SHELTER SUMMARY', note: 'tuval 400 × 594 birim (layout3d.clipboard.canvasUnits)' });
  m.meta.place = [shelfW([-0.46, 0.78, 0.01], 0, '2. kat; pano alt kenarı raf yereli (−0,46 · 0,86 · −0,02)')];
  return m;
}
export function IdleTray() {
  const m = new Model('IdleTray', { family: 'shelf', budget: 'small', listName: 'BOŞTA tepsisi', origin: 'taban ortası (raf yüzeyi, y = 0)',
    note: 'Ayaklar üstünde yükseltilmiş ahşap tepsi; pirinç levha ön dudakta 20° yukarı eğik: K4 kamerasına (20° yukarıdan) dik bakar ve rafın ön kenarında, üst katın gölgesinin dışında kalır. Ayrıca rafın UnderShelfLight ışığı. Açık madde kapandı: “BOŞTA levhası gölgede”.' });
  const W = 0.26, D = 0.075, fh = 0.012, th = 0.014;
  for (const sx of [-1, 1]) for (const sz of [-1, 1]) m.box(0.018, fh, 0.018, 0.002, 'wood', [sx * 0.11, fh / 2, sz * 0.024], [0, 0, 0], 'Body', { skip: ['-y'], cast: false });
  m.box(W, th, D, 0.003, 'woodVarnish', [0, fh + th / 2, 0]);
  m.box(W, 0.012, 0.006, 0.002, 'woodVarnish', [0, fh + th + 0.006, D / 2 - 0.003]);                  // back rail
  const c = Math.cos(20 * D2R), s = Math.sin(20 * D2R); const b = [0, fh, -D / 2 - 0.002]; const up = [0, c, s], nn = [0, s, -c];
  const lipH = 0.04; const lipC = add(add(b, scl(up, lipH / 2)), scl(nn, -0.004));
  m.box(W, lipH, 0.008, 0.0025, 'woodVarnish', lipC, [20, 0, 0]);
  const pc = add(add(b, scl(up, lipH / 2 + 0.001)), scl(nn, 0.0012));
  m.box(0.22, 0.028, 0.0024, 0.0008, 'plate', pc, [20, 0, 0], 'Body', { cast: false });
  for (const sx of [-1, 1]) screwHead(m, add(add(pc, [sx * 0.102, 0, 0]), scl(nn, 0.0012)), nn, 0.0026, 0.0014, 'brassWorn', 'Body', 6);
  const tc = add(pc, scl(nn, 0.0013));
  m.text('TXT_Label', { center: add(tc, [-0.03, 0, 0]), size: [0.14, 0.02], normal: nn, up, role: 'plateLabel', capMm: 12, align: 'left', sample: 'BOŞTA', sampleEn: 'IDLE', note: 'K4’te büyük harf ≈ 13 px, levha kameraya dik' });
  m.text('TXT_Count', { center: add(tc, [0.07, 0, 0]), size: [0.05, 0.02], normal: nn, up, role: 'plateLabel', capMm: 12, align: 'right', sample: '4', sampleEn: '4', note: 'boştaki kişi sayısı' });
  for (let i = 0; i < 5; i++) m.anchor(`Peg_${i}`, { pos: [-0.10 + i * 0.05, fh + th, 0.008], note: 'boştaki piyon yeri (Peg)' });
  m.meta.place = [shelfW([-0.46, 0.78, -0.192], 0, '2. kat ön kenarı (levha z ≈ −0,235: raf ön kenarı hizası); eski yeri (−0,46 · 0,78 · −0,155)')];
  return m;
}
const PEGBANDS = [['a', 'teamTechnician'], ['b', 'teamEmp'], ['c', 'book4'], ['d', 'teamAnalyst'], ['e', 'pawnViolet'], ['f', 'pawnOchre']];
export function Peg(v = 'a') {
  const band = (PEGBANDS.find(p => p[0] === v) || PEGBANDS[0])[1];
  const name = v === 'base' ? 'Peg' : `Peg_${v}`;
  const m = new Model(name, { family: 'shelf', budget: 'small', listName: v === 'base' ? 'Piyon (kişi)' : `Piyon · bant ${band}`, origin: 'taban ortası (y = 0)',
    note: 'Tornalanmış ahşap piyon (h 46 mm), gövdede renkli bant. Bant malzemesi değişkendir: Peg_a teamTechnician · b teamEmp · c book4 · d teamAnalyst · e pawnViolet · f pawnOchre (Rev2 PAWN sırası).',
    variants: v === 'base' ? Object.fromEntries(PEGBANDS.map(([k, mt]) => [`Peg_${k}`, { band: mt }])) : null });
  m.lathe([[0, 0], [0.0118, 0], [0.012, 0.0016], [0.0114, 0.0042], [0.0098, 0.008], [0.0089, 0.0148]], 14, 'pawn');
  m.lathe([[0.0089, 0.0148], [0.0093, 0.0152], [0.0091, 0.0232], [0.0086, 0.0238]], 14, v === 'base' ? 'teamTechnician' : band, [0, 0, 0], [0, 0, 0], 'Body', { caps: false });
  m.lathe([[0.0086, 0.0238], [0.0080, 0.0268], [0.0060, 0.0292], [0.0067, 0.0302], [0.0091, 0.0335], [0.0096, 0.0372], [0.0088, 0.0418], [0.0062, 0.0448], [0, 0.046]], 14, 'pawn', [0, 0, 0], [0, 0, 0], 'Body', { caps: false });
  m.meta.place = [shelfW([-0.46, 0.806, -0.184], 0, 'BOŞTA tepsisi orta yeri (IdleTray.Peg_i); maket yuvaları Diorama.Slots_<oda> (dress_room_shelf.js kurar)')];
  return m;
}
export function PegSlot() {
  const m = new Model('PegSlot', { family: 'shelf', budget: 'small', listName: 'Kişi yuvası (pirinç disk)', origin: 'taban ortası (oda tabanı, y = 0)',
    note: 'Maket odalarında kapasite kadar pirinç yuva; boş yuva görünür kalır, görevli Peg üstüne oturur.' });
  m.lathe([[0, 0], [0.0125, 0], [0.0125, 0.0012], [0.0112, 0.0024], [0, 0.0024]], 14, 'brassDark', [0, 0, 0], [0, 0, 0], 'Body', { cast: false });
  m.meta.place = [DPLACE([DIO.rooms.atolye[0] - 0.13, DIO.rooms.atolye[1] + 0.004, -0.09], 'ör. atölye ilk yuva (Slots_<oda> formülü)')];
  return m;
}

export const MODELS = {
  Shelf: { build: Shelf },
  Crate: { build: Crate },
  Jerrycan: { build: Jerrycan },
  HopePlant1: { build: () => HopePlant(1) },
  HopePlant2: { build: () => HopePlant(2) },
  HopePlant3: { build: () => HopePlant(3) },
  Diorama: { build: Diorama },
  DioramaRoomProps_yasam: { build: PropsYasam },
  DioramaRoomProps_telsiz: { build: PropsTelsiz },
  DioramaRoomProps_atolye: { build: PropsAtolye },
  DioramaRoomProps_revir: { build: PropsRevir },
  DioramaDamage: { build: DioramaDamage },
  DepotCrate: { build: DepotCrate },
  DepotBin: { build: DepotBin },
  DepotPlate: { build: DepotPlate },
  DepotSlate: { build: DepotSlate },
  RoomCardHolder: { build: RoomCardHolder },
  RoomCard: { build: RoomCard },
  ClipboardStand: { build: ClipboardStand },
  IdleTray: { build: IdleTray },
  Peg: { build: () => Peg('base') },
  Peg_a: { build: () => Peg('a') }, Peg_b: { build: () => Peg('b') }, Peg_c: { build: () => Peg('c') },
  Peg_d: { build: () => Peg('d') }, Peg_e: { build: () => Peg('e') }, Peg_f: { build: () => Peg('f') },
  PegSlot: { build: PegSlot },
};
