// family: raidpieces · Rev3 model library (see DESIGN_R3.md §3.10)
// Raid board units: 1 tile = 1 unit; origin = tile centre on the board (y = 0). Unity local coordinates (x east, y up,
// z north); machine and pawn fronts face −z (the game yaws machines and pawns). Raid camera: orthographic, pitch 35°,
// yaw 45°, looking toward +x +z → visible faces are top, west (−x) and south (−z). 132.58 px/unit at 1080p.
// Rules used here: chamfers 0.006–0.025, thin parts ≥ 0.025, parts < 0.06 cast no shadow, red only on the machine
// eye (r_lens), the base ring (r_accent), the AI marks (node icon, core badge, wake pin).
import * as THREE from 'three';
import { Font } from 'three/addons/loaders/FontLoader.js';
import { Model, gBox, gLathe, gExtrude, gTube, gSphere, gPlane, gDisc, prep, roundedRect, setUnityEuler, D2R } from '../mlib.js';
import { defMat } from '../mats.js';
import { GLYPHS } from './raidpieces_glyphs.js';

// ---------------------------------------------------------------- materials (new keys only; Turkish notes)
defMat('rp_hazardYellow', { hex: '#D9A73A', rough: 0.7, metal: 0, note: 'kilitleyici tehlike şeridi sarısı (geometrik şerit; doku yok)' });
defMat('rp_slot', { hex: '#151311', rough: 0.9, metal: 0, note: 'dolap sunucu yuvası, koyu oyuk' });
defMat('rp_domeGlass', { hex: '#2A3135', rough: 0.25, metal: 0.1, note: 'dron sensör kubbesi (koyu cam; kırmızı değil, göz ayrı)' });
defMat('rp_tick', { hex: '#24592E', rough: 0.7, metal: 0, note: 'tur bitti tik işareti (yeşil = iyi)' });
defMat('rp_crateOlive', { hex: '#535C3D', rough: 0.88, metal: 0, note: 'ikmal sandığı: zeytin yeşili boyalı ahşap (mat)' });
defMat('rp_crateBatten', { hex: '#434B31', rough: 0.9, metal: 0, note: 'sandık çıtası, koyu zeytin' });
defMat('rp_crateInside', { hex: '#2B2720', rough: 0.95, metal: 0, note: 'boşalmış sandığın içi (koyu ham ahşap)' });
defMat('rp_crateSteel', { hex: '#6A6D69', rough: 0.7, metal: 0.5, note: 'sandık çelik köşe başlığı, etiket çerçevesi (mat)' });
defMat('rp_rope', { hex: '#A88E5E', rough: 0.95, metal: 0, note: 'kenevir halat tutamak' });
defMat('rp_crateEmblem', { hex: '#E3D9BE', rough: 0.9, metal: 0, alpha: 0.5, tex: 'tx_kyga_emblem (KYGA amblemi, krem mürekkep, alfa kanalında; yazı yok)', note: 'sandık kapağında amblem decal’i; doku alfası taşır (önizlemede yarı saydam kare)' });
defMat('rp_tubeGrey', { hex: '#3E4143', rough: 0.85, metal: 0.15, note: 'basınçlı tüp gövdesi, koyu gri boya (mat)' });
defMat('rp_steelPlate', { hex: '#6E726E', rough: 0.85, metal: 0.3, note: 'kaynak kolu ve kaidesi, açık çelik (mat)' });
defMat('rp_steelDark', { hex: '#45484A', rough: 0.85, metal: 0.3, note: 'döner tabla, ayak, mafsal (mat koyu çelik)' });
defMat('rp_concrete', { hex: '#8D8A82', rough: 0.95, metal: 0, note: 'tavan enkazı beton topak' });
defMat('rp_corrugated', { hex: '#7C817B', rough: 0.85, metal: 0.3, note: 'oluklu galvaniz sac (mat)' });
defMat('rp_beam', { hex: '#4D4943', rough: 0.88, metal: 0.25, note: 'bükük çatı kirişi ve filiz demiri (koyu, kırmızısız)' });
defMat('rp_soot', { hex: '#2B241D', rough: 1, metal: 0, alpha: 0.45, note: 'enkaz altı is lekesi (decal; gölgesiz)' });
defMat('rp_fanBlade', { hex: '#9AA5A5', rough: 0.6, metal: 0.3, note: 'soğutma fanı kanadı' });
defMat('rp_markWake', { hex: '#9A1E14', rough: 1, metal: 0, alpha: 0.95, note: 'uyanma halkası ve ışınları (URP Unlit Transparent, ZWrite kapalı, gölgesiz)' });
defMat('rp_markWakeFill', { hex: '#9A1E14', rough: 1, metal: 0, alpha: 0.12, note: 'uyanma halkasının dolgusu (Unlit Transparent, ZWrite kapalı)' });
defMat('rp_markGhost', { hex: '#854005', rough: 1, metal: 0, alpha: 0.95, note: 'kopyalama niyeti kesik halka (Unlit Transparent, ZWrite kapalı)' });
defMat('rp_markGhostFill', { hex: '#854005', rough: 1, metal: 0, alpha: 0.1, note: 'kopyalama halkasının dolgusu (Unlit Transparent, ZWrite kapalı)' });
defMat('rp_acetateMove', { hex: '#1A4789', rough: 1, metal: 0, alpha: 0.35, tex: 'tx_mark_move (dolgu α .16, tarama α .34 · 1,6 px · 0,11 birim aralık, v-sabit; kenar α .85 · 2 px · pay 0,07)', note: 'hareket asetatı: Unlit Transparent, ZWrite kapalı; saydamlık dokuda (malzeme alfası 1; önizlemede 0,35)' });
defMat('rp_acetateHit', { hex: '#9A1E14', rough: 1, metal: 0, alpha: 0.35, tex: 'tx_mark_hit (dolgu α .12, tarama α .46 · 1,8 px · 0,085 birim, u-sabit; kenar α .9 · 2,4 px)', note: 'kesin isabet asetatı: Unlit Transparent, ZWrite kapalı; saydamlık dokuda (önizlemede 0,35)' });

// ---------------------------------------------------------------- local geometry helpers
const ORIGIN = 'karenin merkezi, tahta yüzeyi (y = 0)';
const FAM = 'raidpieces';
const NOSH = { cast: false };
const BODY = { top: 'r_maintTop', side: 'r_gun' };   // machine body panels: lit top, gunmetal sides (machines rotate → no per-face colours)
const T3 = (p) => [p[0], p[1], -p[2]];               // Unity → three
const AP = (r, y, a) => [r * Math.sin(a), y, -r * Math.cos(a)]; // polar, a from the front (−z) toward +x

function triGeom(arr, crease = 40) { const g = new THREE.BufferGeometry(); g.setAttribute('position', new THREE.Float32BufferAttribute(arr, 3)); return prep(g, { crease }); }
// convex polygons in Unity space; each {pts, out:[dir]} or {pts, inside:[pt]}; wound so the normal points outward
function polyGeom(list, crease = 40) {
  const out = [];
  for (const q of list) {
    const P = q.pts.map(T3); let nx = 0, ny = 0, nz = 0;
    for (let i = 0; i < P.length; i++) { const a = P[i], b = P[(i + 1) % P.length]; nx += (a[1] - b[1]) * (a[2] + b[2]); ny += (a[2] - b[2]) * (a[0] + b[0]); nz += (a[0] - b[0]) * (a[1] + b[1]); }
    let ref;
    if (q.out) { const D = T3(q.out); ref = nx * D[0] + ny * D[1] + nz * D[2]; }
    else { const I = T3(q.inside); const c = [0, 1, 2].map(k => P.reduce((s, p) => s + p[k], 0) / P.length); ref = nx * (c[0] - I[0]) + ny * (c[1] - I[1]) + nz * (c[2] - I[2]); }
    const Q = ref < 0 ? [...P].reverse() : P;
    for (let i = 1; i < Q.length - 1; i++) out.push(...Q[0], ...Q[i], ...Q[i + 1]);
  }
  return triGeom(out, crease);
}
// closed profile [[r, y], ...] (one per angle, same length) swept around +y through `angles`, with end caps
function sweepGeom(profiles, angles, o = {}) {
  const list = []; const n = profiles[0].length;
  const avg = (p) => [p.reduce((s, v) => s + v[0], 0) / p.length, p.reduce((s, v) => s + v[1], 0) / p.length];
  for (let i = 0; i < angles.length - 1; i++) {
    const A = profiles[i], B = profiles[i + 1], a0 = angles[i], a1 = angles[i + 1]; const cA = avg(A), cB = avg(B);
    const inside = AP((cA[0] + cB[0]) / 2, (cA[1] + cB[1]) / 2, (a0 + a1) / 2);
    for (let j = 0; j < n; j++) { const k = (j + 1) % n; list.push({ pts: [AP(A[j][0], A[j][1], a0), AP(A[k][0], A[k][1], a0), AP(B[k][0], B[k][1], a1), AP(B[j][0], B[j][1], a1)], inside }); }
  }
  if (o.caps !== false) {
    const a0 = angles[0], a1 = angles[angles.length - 1];
    list.push({ pts: profiles[0].map(([r, y]) => AP(r, y, a0)), out: [-Math.cos(a0), 0, -Math.sin(a0)] });
    list.push({ pts: profiles[profiles.length - 1].map(([r, y]) => AP(r, y, a1)), out: [Math.cos(a1), 0, Math.sin(a1)] });
  }
  return polyGeom(list, o.crease ?? 40);
}
// flat annulus sector facing up, as polygons
function flatArc(ri, ro, a0, a1, n, y) { const L = []; for (let i = 0; i < n; i++) { const b0 = a0 + (a1 - a0) * i / n, b1 = a0 + (a1 - a0) * (i + 1) / n; L.push({ pts: [AP(ri, y, b0), AP(ro, y, b0), AP(ro, y, b1), AP(ri, y, b1)], out: [0, 1, 0] }); } return L; }
// bake a Unity euler rotation into a geometry
function bake(g, rot) { if (!rot || (!rot[0] && !rot[1] && !rot[2])) return g; const o = new THREE.Object3D(); setUnityEuler(o, rot); o.updateMatrix(); g.applyMatrix4(o.matrix); return g; }
// split a non-indexed geometry into groups by face normal (classify receives the Unity normal and centroid)
function splitFaces(g, classify) {
  const P = g.attributes.position.array, N = g.attributes.normal.array, UV = g.attributes.uv ? g.attributes.uv.array : null; const G = new Map();
  for (let o = 0, t = 0; o < P.length; o += 9, t += 6) {
    const ux = P[o + 3] - P[o], uy = P[o + 4] - P[o + 1], uz = P[o + 5] - P[o + 2], vx = P[o + 6] - P[o], vy = P[o + 7] - P[o + 1], vz = P[o + 8] - P[o + 2];
    let nx = uy * vz - uz * vy, ny = uz * vx - ux * vz, nz = ux * vy - uy * vx; const l = Math.hypot(nx, ny, nz) || 1; nx /= l; ny /= l; nz /= l;
    const key = classify([nx, ny, -nz], [(P[o] + P[o + 3] + P[o + 6]) / 3, (P[o + 1] + P[o + 4] + P[o + 7]) / 3, -(P[o + 2] + P[o + 5] + P[o + 8]) / 3]);
    if (!G.has(key)) G.set(key, { p: [], n: [], uv: [] }); const B = G.get(key);
    for (let k = 0; k < 9; k++) { B.p.push(P[o + k]); B.n.push(N[o + k]); } if (UV) for (let k = 0; k < 6; k++) B.uv.push(UV[t + k]);
  }
  return [...G].map(([k, B]) => { const gg = new THREE.BufferGeometry(); gg.setAttribute('position', new THREE.Float32BufferAttribute(B.p, 3)); gg.setAttribute('normal', new THREE.Float32BufferAttribute(B.n, 3)); if (UV) gg.setAttribute('uv', new THREE.Float32BufferAttribute(B.uv, 2)); return [k, gg]; });
}
const faceKey = (n) => n[1] >= 0.5 ? 'top' : n[1] <= -0.6 ? 'bottom' : (Math.abs(n[0]) >= Math.abs(n[2]) - 1e-3 ? (n[0] < 0 ? 'west' : 'east') : (n[2] < 0 ? 'south' : 'north'));
// add a geometry split into per-face materials: mats {top, west, south, east, north, bottom, side}
function addFaced(m, g, mats, pos = [0, 0, 0], rot = [0, 0, 0], part = 'Body', o = {}, classify = faceKey) {
  if (g.index) g = g.toNonIndexed(); g = bake(g, rot);
  for (const [k, gg] of splitFaces(g, classify)) { const mat = mats[k] || mats.side || mats.west; if (mat) m.add(gg, mat, pos, [0, 0, 0], part, o); }
}
function fbox(m, w, h, d, c, mats, pos, rot, part = 'Body', o = {}) { addFaced(m, gBox(w, h, d, c, { skip: o.skip }), mats, pos, rot, part, o); }
const cabMats = (k) => ({ top: `r_${k}_top`, west: `r_${k}_west`, south: `r_${k}_south`, east: `r_${k}_west`, north: `r_${k}_south` });
// extrusion of an outline drawn in the Unity x–z plane (top view), extruded along +y, centred on y
const extrudeXZ = (pts, h, c) => bake(gExtrude(pts, h, c), [90, 0, 0]);

// ---------------------------------------------------------------- shared machine parts
// common base: dark disc + red ring on its top edge (0.05 high → casts no shadow)
function machineBase(m, R, seg) {
  const h = 0.05, c = 0.01, w = 0.018;
  m.lathe([[R, 0], [R, h - c]], seg, 'r_gunD', [0, 0, 0], [0, 0, 0], 'Body', { caps: false, cast: false });
  m.lathe([[R, h - c], [R - c, h], [R - c - w, h]], seg, 'r_accent', [0, 0, 0], [0, 0, 0], 'Body', { caps: false, cast: false });
  m.lathe([[R - c - w, h], [0, h]], seg, 'r_gunD', [0, 0, 0], [0, 0, 0], 'Body', { caps: false, cast: false });
}
// machine eye: dark bezel + red lens dome; rot points the lathe's +y outward (front: [-90,0,0])
function eye(m, pos, rot = [-90, 0, 0], r = 0.032, part = 'Body') {
  m.lathe([[r + 0.011, 0], [r + 0.011, 0.011], [r, 0.015]], 10, 'r_gunD', pos, rot, part, { caps: false, cast: false });
  m.lathe([[r, 0.008], [r * 0.88, 0.022], [r * 0.5, 0.032], [0, 0.035]], 10, 'r_lens', pos, rot, part, { caps: false, cast: false });
}
// tracked chassis shared by the four tracked types: two stadium track pods along z, three road wheels each, belly plate
function tracks(m) {
  const L = 0.54, H = 0.12, W = 0.12, X = 0.17, y0 = 0.05;
  const prof = roundedRect(L, H, 0.055, 2);
  for (const sx of [-1, 1]) {
    m.extrude(prof, W, 0.012, 'r_gunD', [sx * X, y0 + H / 2, 0], [0, 90, 0]);
    for (const z of [-0.17, 0, 0.17]) m.lathe([[0.04, 0], [0.04, 0.014], [0, 0.014]], 8, 'r_gunW', [sx * (X + W / 2), y0 + H / 2, z], [0, 0, -90 * sx], 'Body', { caps: false, cast: false });
  }
  m.box(2 * X - W + 0.004, 0.07, L - 0.14, 0, 'r_gunD', [0, y0 + 0.045, 0], [0, 0, 0], 'Body', NOSH);
}
const MACHINE_NOTE = 'Gövde renkleri ortak (r_gun / r_maintTop / r_gunW / r_gunS / r_gunD); kırmızı yalnız gözde (r_lens) ve taban halkasında (r_accent). Tür biçimden okunur, etiketteki işaret ikincil.';

// ---------------------------------------------------------------- 1 · Drone (and the drone core shared with ShieldDrone)
function droneCore(m, o = {}) {
  const R = o.R ?? 0.24, rs = o.rotorSeg ?? 14;
  machineBase(m, R, 20);
  m.lathe([[0.046, 0.05], [0.046, 0.066], [0.036, 0.078], [0.026, 0.08]], 10, 'r_gunD', [0, 0, 0], [0, 0, 0], 'Body', { caps: false, cast: false }); // rod socket
  m.cyl(0.026, 0.30, 10, 0, 'r_acrylic', [0, 0.23, 0], [0, 0, 0], 'Body', { open: true, cast: false });                     // acrylic rod Ø0.052, y 0.08–0.38
  m.lathe([[0.026, 0.37], [0.044, 0.382], [0.044, 0.402]], 10, 'r_gunD', [0, 0, 0], [0, 0, 0], 'Body', { caps: false, cast: false }); // collar
  m.lathe([[0.044, 0.4], [0.112, 0.4], [0.135, 0.418], [0.135, 0.45], [0.112, 0.468], [0.066, 0.472]], 16, 'r_gun', [0, 0, 0], [0, 0, 0], 'Body', { caps: false }); // round hull
  m.lathe([[0.066, 0.472], [0.062, 0.49], [0.042, 0.507], [0, 0.513]], 14, 'rp_domeGlass', [0, 0, 0], [0, 0, 0], 'Body', { caps: false, cast: false }); // sensor dome
  for (const a of [45, -45]) m.box(0.5, 0.026, 0.044, 0.008, 'r_gunS', [0, 0.447, 0], [0, a, 0]);                // X arms
  const d = 0.24 / Math.SQRT2;
  for (const sx of [-1, 1]) for (const sz of [-1, 1]) {
    m.lathe([[0.03, 0.43], [0.03, 0.47], [0.022, 0.478], [0, 0.478]], 8, 'r_gunW', [sx * d, 0, sz * d], [0, 0, 0], 'Body', { caps: false, cast: false }); // motor pod
    m.lathe([[0.1, 0.48], [0.1, 0.496], [0.088, 0.506], [0, 0.506]], rs, 'r_gunD', [sx * d, 0, sz * d], [0, 0, 0], 'Body', { caps: false }); // rotor disc
  }
  eye(m, [0, 0.434, -0.128], [-90, 0, 0], 0.028);
}
export function Drone() {
  const m = new Model('Drone', { family: FAM, budget: 'unit', listName: 'Dron (2 can, 3 hareket)', origin: ORIGIN,
    note: 'Akrilik çubuk (Ø0,052) üstünde dört rotorlu dron: yuvarlak gövde, üstte koyu sensör kubbesi, X kollar, dört ince rotor diski; göz gövdenin önünde. Biçim: ince ayak + yüksek, açık, dört diskli siluet. Tepe 0,52. ' + MACHINE_NOTE });
  droneCore(m);
  m.anchor('Top', { pos: [0, 0.513, 0], note: 'tepe noktası (can etiketi / bayrak buraya göre)' });
  m.meta.place = [{ pos: [5.5, 0, -1.5], yaw: 90, note: 'örnek: Rev2 raid.html plan durumu, F7 karesi (u 5, v 1); batıya bakar' }];
  return m;
}

// ---------------------------------------------------------------- 2 · ShieldDrone
export function ShieldDrone() {
  const m = new Model('ShieldDrone', { family: FAM, budget: 'unit', listName: 'Kalkan dronu', origin: ORIGIN,
    note: 'Dron + önde 105° faset çelik kalkan (4 yüz, altı sivri, çelik çerçeve; kırmızı kenar yok). Kalkan üstü 0,52: göz kalkanın üstünden görünür. Biçim: dronun önünde kavisli büyük levha. ' + MACHINE_NOTE });
  droneCore(m, { rotorSeg: 12 });
  const half = 52.5 * D2R, N = 4, ri = 0.372, t = 0.03, c = 0.01;
  const ang = [...Array(N + 1)].map((_, i) => -half + 2 * half * i / N);
  const yb = (a) => 0.17 + 0.08 * (Math.abs(a) / half) ** 2, yt = (a) => 0.52 - 0.012 * (Math.abs(a) / half) ** 2;
  m.add(sweepGeom(ang.map(a => [[ri, yb(a)], [ri + t - c, yb(a)], [ri + t, yb(a) + c], [ri + t, yt(a) - c], [ri + t - c, yt(a)], [ri, yt(a)]]), ang, { crease: 30 }), 'r_armor_light');
  m.add(sweepGeom(ang.map(a => [[ri - 0.006, yt(a)], [ri + t + 0.006, yt(a)], [ri + t + 0.006, yt(a) + 0.024], [ri - 0.006, yt(a) + 0.024]]), ang, { crease: 30 }), 'r_armor_dark'); // top rim
  m.add(sweepGeom(ang.map(a => [[ri - 0.006, yb(a) - 0.022], [ri + t + 0.006, yb(a) - 0.022], [ri + t + 0.006, yb(a)], [ri - 0.006, yb(a)]]), ang, { crease: 30 }), 'r_armor_dark'); // bottom rim
  for (const a of [-half, half]) { const rr = ri + t / 2; const y0 = yb(a) - 0.022, y1 = yt(a) + 0.024;
    m.box(0.044, y1 - y0, 0.026, 0.006, 'r_armor_dark', [rr * Math.sin(a), (y0 + y1) / 2, -rr * Math.cos(a)], [0, a / D2R, 0]); } // side rims
  for (const a of [-24, 24]) { const ar = a * D2R, r0 = 0.12, r1 = ri; const rm = (r0 + r1) / 2;
    m.box(0.03, 0.03, r1 - r0, 0.008, 'r_gunS', [rm * Math.sin(ar), 0.43, -rm * Math.cos(ar)], [0, a, 0]); } // two struts hull → shield
  m.anchor('Top', { pos: [0, 0.544, 0], note: 'tepe noktası' });
  m.meta.place = [{ pos: [1.5, 0, -6.5], yaw: 90, note: 'örnek: Rev2 raid.html intents durumu (u 1, v 6)' }];
  return m;
}

// ---------------------------------------------------------------- 3 · ArmorDrone
export function ArmorDrone() {
  const m = new Model('ArmorDrone', { family: FAM, budget: 'unit', listName: 'Zırhlı dron (2 can, 1 zırh)', origin: ORIGIN,
    note: 'Dron ailesi, ağırlaştırılmış: kalın akrilik çubuk (Ø0,068), sekizgen gövde, altında sekizgen zırh eteği, çelik zırh bandı ve eğik ön levha, dört rotor kanal (halka) içinde, göz eğik ön levhanın alt yarısında. Biçim: dört davul + etekli ağır gövde (dronun ince ayaklı hafif siluetinden ayrılır). Tepe 0,53. ' + MACHINE_NOTE });
  machineBase(m, 0.26, 20);
  m.lathe([[0.058, 0.05], [0.058, 0.066], [0.046, 0.078], [0.034, 0.08]], 10, 'r_gunD', [0, 0, 0], [0, 0, 0], 'Body', { caps: false, cast: false });
  m.cyl(0.034, 0.29, 10, 0, 'r_acrylic', [0, 0.225, 0], [0, 0, 0], 'Body', { open: true, cast: false });                   // rod Ø0.068, y 0.08–0.37
  m.lathe([[0.034, 0.36], [0.06, 0.37], [0.06, 0.375]], 10, 'r_gunD', [0, 0, 0], [0, 0, 0], 'Body', { caps: false, cast: false });
  const oct = { phiStart: -Math.PI / 8 };
  m.add(prep(gLathe([[0.06, 0.372], [0.185, 0.372], [0.205, 0.39], [0.205, 0.468], [0.186, 0.488], [0.07, 0.49], [0, 0.49]], 8, { ...oct, caps: false }), { crease: 30 }), 'r_gun');
  m.add(prep(gLathe([[0.12, 0.489], [0.12, 0.498], [0.104, 0.508], [0, 0.508]], 8, { ...oct, caps: false }), { crease: 30 }), 'r_maintTop', [0, 0, 0], [0, 0, 0], 'Body', NOSH); // top hatch
  m.add(prep(gLathe([[0.205, 0.376], [0.226, 0.386], [0.226, 0.428], [0.205, 0.438]], 8, { ...oct, caps: false }), { crease: 30 }), 'r_armor_light'); // armour band
  m.add(prep(gLathe([[0.17, 0.275], [0.245, 0.275], [0.25, 0.283], [0.215, 0.372]], 8, { ...oct, caps: false }), { crease: 30 }), 'r_armor_mid'); // armour skirt under the hull
  m.box(0.21, 0.13, 0.04, 0.012, 'r_armor_light', [0, 0.44, -0.214], [18, 0, 0]);                                // sloped front plate
  eye(m, [0, 0.412, -0.231], [-72, 0, 0], 0.03);                                                                 // eye low on the glacis
  for (const a of [45, -45]) m.box(0.62, 0.035, 0.065, 0.01, 'r_gunS', [0, 0.455, 0], [0, a, 0]);               // heavy arms
  const d = 0.31 / Math.SQRT2;
  for (const sx of [-1, 1]) for (const sz of [-1, 1]) {
    const p = [sx * d, 0, sz * d];
    m.lathe([[0.13, 0.425], [0.13, 0.53], [0.112, 0.53], [0.112, 0.437]], 12, 'r_gunW', p, [0, 0, 0], 'Body', { caps: false }); // duct ring
    m.add(gDisc(0.112, 12), 'r_gunD', [p[0], 0.488, p[2]], [90, 0, 0], 'Body', NOSH);                                          // rotor (blur disc)
    m.box(0.224, 0.014, 0.028, 0, 'r_gunS', [p[0], 0.494, p[2]], [0, sx * sz > 0 ? 45 : -45, 0], 'Body', NOSH);                 // strut
    m.lathe([[0.028, 0.48], [0.028, 0.502], [0, 0.507]], 6, 'r_gunS', p, [0, 0, 0], 'Body', { caps: false, cast: false });     // hub
  }
  m.anchor('Top', { pos: [0, 0.53, 0], note: 'tepe noktası' });
  return m;
}

// ---------------------------------------------------------------- 4 · MaintRobot
export function MaintRobot() {
  const m = new Model('MaintRobot', { family: FAM, budget: 'unit', listName: 'Bakım robotu (2 can, 2 hareket)', origin: ORIGIN,
    note: 'Paletli kutu gövde, önde küçük kafa ve göz, kafada anten (Ø0,025, tepe 0,76), arkada alet rafı (çekiç, tornavida, boru anahtarı), sağ yanda (−x) mafsallı anahtar kolu. Biçim: basamaklı kutu + yana sarkan büyük anahtar. ' + MACHINE_NOTE });
  machineBase(m, 0.31, 24); tracks(m);
  fbox(m, 0.38, 0.3, 0.42, 0.025, BODY, [0, 0.32, 0.01], [0, 0, 0], 'Body', { skip: ['-y'] });                 // body y 0.17–0.47
  m.box(0.22, 0.1, 0.012, 0, 'r_gunS', [0.06, 0.3, -0.204], [0, 0, 0], 'Body', NOSH);                              // front service panel
  fbox(m, 0.22, 0.13, 0.19, 0.02, BODY, [0.02, 0.535, -0.07], [0, 0, 0], 'Body', { skip: ['-y'] });            // head y 0.47–0.60
  eye(m, [-0.02, 0.54, -0.165], [-90, 0, 0], 0.03);
  m.lathe([[0.024, 0], [0.024, 0.014], [0, 0.014]], 6, 'r_gunD', [0.098, 0.6, -0.02], [0, 0, 0], 'Body', { caps: false, cast: false });
  m.cyl(0.0125, 0.125, 6, 0, 'r_steelPin', [0.098, 0.6765, -0.02], [0, 0, 0], 'Body', { open: true, cast: false }); // antenna mast
  m.lathe([[0, 0], [0.022, 0.01], [0.022, 0.02], [0, 0.03]], 6, 'r_steelPin', [0.098, 0.732, -0.02], [0, 0, 0], 'Body', { caps: false, cast: false }); // tip bead (no red)
  // tool rack behind the head: bin + hammer, screwdriver, pipe wrench
  m.box(0.28, 0.08, 0.1, 0.012, 'r_gunW', [0.03, 0.51, 0.15]);
  m.box(0.028, 0.14, 0.028, 0, 'r_kraft', [-0.07, 0.6, 0.15], [0, 0, 6], 'Body', NOSH);                            // hammer handle (wood)
  m.box(0.1, 0.036, 0.042, 0.008, 'r_steelPin', [-0.077, 0.675, 0.15], [0, 0, 6]);                                 // hammer head
  m.lathe([[0.019, 0], [0.019, 0.07], [0.014, 0.075], [0.0125, 0.075], [0.0125, 0.16], [0, 0.165]], 6, 'r_steelPin', [0.03, 0.55, 0.15], [0, 0, -5], 'Body', { caps: false, cast: false }); // screwdriver
  m.box(0.03, 0.15, 0.03, 0, 'r_gunS', [0.12, 0.61, 0.15], [0, 0, -8], 'Body', NOSH);                              // pipe wrench handle
  m.box(0.07, 0.035, 0.04, 0.008, 'r_steelPin', [0.13, 0.695, 0.15], [0, 0, -8]);                                 // its jaw
  // Arm: shoulder on the −x side, swings in the x–y plane (axis z)
  const S = [-0.205, 0.425, -0.03], E = [-0.335, 0.405, -0.03], Wr = [-0.36, 0.275, -0.03];
  m.part('Arm', { pivot: S, axis: 'z', range: [-20, 30], rest: 0, note: 'anahtar kolu; omuz gövdenin −x yanında; +30 kol aşağı-içe, −20 yukarı-dışa' });
  m.lathe([[0, -0.034], [0.045, -0.034], [0.045, 0.034], [0, 0.034]], 10, 'r_gunW', S, [90, 0, 0], 'Arm');            // shoulder hub
  m.box(0.17, 0.052, 0.052, 0.012, 'r_gun', [(S[0] + E[0]) / 2, (S[1] + E[1]) / 2, S[2]], [0, 0, Math.atan2(S[1] - E[1], S[0] - E[0]) / D2R], 'Arm'); // upper arm
  m.lathe([[0, -0.03], [0.034, -0.03], [0.034, 0.03], [0, 0.03]], 10, 'r_gunW', E, [90, 0, 0], 'Arm', NOSH);           // elbow knuckle
  const fa = Math.atan2(Wr[0] - E[0], E[1] - Wr[1]) / D2R;                                                              // forearm tilt from vertical
  m.box(0.045, 0.15, 0.045, 0.01, 'r_steelPin', [(E[0] + Wr[0]) / 2, (E[1] + Wr[1]) / 2, E[2]], [0, 0, fa], 'Arm');   // forearm = spanner handle
  const jaw = [[0.03, -0.066], [0.058, -0.052], [0.078, -0.004], [0.058, 0.05], [0, 0.075], [-0.058, 0.05], [-0.078, -0.004], [-0.058, -0.052], [-0.03, -0.066], [-0.027, -0.008], [0.027, -0.008]];
  m.extrude(jaw, 0.034, 0.009, 'r_steelPin', [Wr[0] + 0.006, Wr[1] - 0.04, Wr[2]], [0, 0, fa], 'Arm');                    // open-end spanner head, jaw down
  m.anchor('Top', { pos: [0.098, 0.762, -0.02], note: 'tepe noktası (anten)' });
  m.meta.place = [{ pos: [6.5, 0, -4.5], yaw: 90, note: 'örnek: Rev2 raid.html plan durumu (u 6, v 4)' }];
  return m;
}

// ---------------------------------------------------------------- 5 · Locker
export function Locker() {
  const m = new Model('Locker', { family: FAM, budget: 'unit', listName: 'Kilitleyici', origin: ORIGIN,
    note: 'Paletli dik kule gövde (tepe 0,69, şapkalı), ortada sarı-siyah tehlike şeridi bandı (geometrik şeritler), önde iki büyük C kıskaç (JawL, JawR; düşey eksen). Biçim: uzun dar kule + öne uzanan kıskaçlar. ' + MACHINE_NOTE });
  machineBase(m, 0.31, 24); tracks(m);
  fbox(m, 0.32, 0.48, 0.34, 0.025, BODY, [0, 0.41, 0.03], [0, 0, 0], 'Body', { skip: ['-y'] });                // tower y 0.17–0.65
  fbox(m, 0.37, 0.05, 0.39, 0.015, BODY, [0, 0.665, 0.03]);                                                       // cap
  m.box(0.338, 0.1, 0.358, 0.006, 'r_base', [0, 0.3, 0.03]);                                                       // hazard band (black) y 0.25–0.35
  // yellow stripes, 3 per face, slanted 60°; drawn in the face plane and turned to each face
  const fw = 0.338, per = fw / 3, sw = 0.05, sh = 0.1 / Math.tan(60 * D2R);
  const faces = [{ yaw: 0, c: [0, 0.3, 0.03 - 0.179 - 0.002] }, { yaw: 90, c: [-0.169 - 0.002, 0.3, 0.03] }, { yaw: 180, c: [0, 0.3, 0.03 + 0.179 + 0.002] }, { yaw: -90, c: [0.169 + 0.002, 0.3, 0.03] }];
  for (const f of faces) for (let i = 0; i < 3; i++) { const u0 = -fw / 2 + per * i + 0.004;
    const pts = [[u0, -0.05], [u0 + sw, -0.05], [u0 + sw + sh, 0.05], [u0 + sh, 0.05]].map(([u, v]) => [Math.min(u, fw / 2 - 0.002), v]);
    m.extrude(pts, 0.006, 0, 'rp_hazardYellow', f.c, [0, f.yaw, 0], 'Body', NOSH); }
  m.box(0.3, 0.09, 0.07, 0.012, 'r_gunW', [0, 0.44, -0.165]);                                                     // jaw mount block
  eye(m, [0.075, 0.575, -0.14], [-90, 0, 0], 0.034);
  // jaws: C-shaped (top view), extruded 0.075 tall; modelled closed (tips toward the centre line)
  const jl = [[-0.04, 0.03], [0.022, 0.03], [0.022, -0.15], [0.05, -0.17], [0.094, -0.18], [0.1, -0.215], [0.08, -0.252], [0.0, -0.268], [-0.032, -0.25], [-0.048, -0.2]];
  const P = { JawL: [-0.105, 0.43, -0.19], JawR: [0.105, 0.43, -0.19] };
  m.part('JawL', { pivot: P.JawL, axis: 'y', range: [0, 25], rest: 0, note: 'sol kıskaç; +25° dışa açılır' });
  m.part('JawR', { pivot: P.JawR, axis: 'y', range: [0, -25], rest: 0, note: 'sağ kıskaç (ayna); −25° dışa açılır' });
  for (const [nm, sx] of [['JawL', 1], ['JawR', -1]]) {
    const pts = jl.map(([x, z]) => [sx * x, z]); if (sx < 0) pts.reverse();
    m.add(extrudeXZ(pts, 0.09, 0.014), 'r_steelPin', P[nm], [0, 0, 0], nm);
    m.lathe([[0.03, 0], [0.03, 0.012], [0, 0.016]], 8, 'r_gunD', [P[nm][0], P[nm][1] + 0.045, P[nm][2]], [0, 0, 0], nm, { caps: false, cast: false }); // pivot pin cap
  }
  m.anchor('Top', { pos: [0, 0.69, 0.03], note: 'tepe noktası' });
  m.meta.place = [{ pos: [6.5, 0, -5.5], yaw: 90, note: 'örnek: Rev2 raid.html plan durumu (u 6, v 5)' }];
  return m;
}

// ---------------------------------------------------------------- 6 · Mortar
export function Mortar() {
  const m = new Model('Mortar', { family: FAM, budget: 'unit', listName: 'Havan robotu', origin: ORIGIN,
    note: 'Alçak paletli gövde (eğik ön levha), güverte tablasında muylu yanakları arasında 45° kalın kısa namlu (Tube; namlu ağzı ~0,8 = havan atışı yayının başlangıcı), arkada mermi rafı. Biçim: basık gövde + göğe bakan çapraz namlu. ' + MACHINE_NOTE });
  machineBase(m, 0.31, 24); tracks(m);
  const prof = [[-0.23, 0.17], [0.23, 0.17], [0.23, 0.33], [-0.11, 0.33], [-0.23, 0.25]];                       // side profile (z, y): sloped glacis at the front
  addFaced(m, gExtrude(prof.map(([z, y]) => [-z, y]), 0.4, 0.02), BODY, [0, 0, 0], [0, 90, 0]);
  m.lathe([[0.13, 0.33], [0.13, 0.346], [0.116, 0.358], [0, 0.358]], 12, 'r_gunW', [0, 0, 0.06], [0, 0, 0], 'Body', { caps: false, cast: false }); // deck turntable
  for (const sx of [-1, 1]) m.box(0.03, 0.11, 0.11, 0.008, 'r_gun', [sx * 0.098, 0.405, 0.06]);                  // trunnion cheeks
  eye(m, [-0.12, 0.29, -0.172], [-36, 0, 0], 0.03);
  // ammo rack (shells, brass) on the rear deck
  m.box(0.3, 0.07, 0.09, 0.012, 'r_gunW', [0, 0.365, 0.175]);
  for (let i = 0; i < 4; i++) m.lathe([[0.022, 0], [0.022, 0.045], [0.011, 0.07], [0, 0.078]], 6, 'brassWorn', [-0.105 + i * 0.07, 0.4, 0.175], [0, 0, 0], 'Body', { caps: false, cast: false });
  // Tube: pivot on the trunnion axis; modelled at 45° (up-forward)
  const pv = [0, 0.41, 0.06];
  m.part('Tube', { pivot: pv, axis: 'x', range: [0, -15], rest: 0, note: 'namlu 45°; −15° öne iner (atış/nişan). Namlu ağzı merkezi ~(0, 0,74, −0,27)' });
  m.lathe([[0.068, -0.05], [0.08, -0.03], [0.08, 0.4], [0.09, 0.41], [0.09, 0.46], [0.066, 0.46], [0.066, 0.43]], 14, 'r_gun', pv, [-45, 0, 0], 'Tube');
  m.lathe([[0.08, -0.005], [0.093, 0.005], [0.093, 0.06], [0.08, 0.07]], 12, 'r_gunW', pv, [-45, 0, 0], 'Tube', { caps: false }); // breech collar
  m.lathe([[0, -0.1], [0.03, -0.1], [0.03, 0.1], [0, 0.1]], 8, 'r_gunS', pv, [0, 0, 90], 'Tube', NOSH);          // trunnion pin
  m.anchor('Muzzle', { part: 'Tube', pos: [0, 0.41 + 0.46 * Math.SQRT1_2, 0.06 - 0.46 * Math.SQRT1_2], note: 'namlu ağzı (havan atışı yayı buradan başlar)' });
  m.anchor('Top', { pos: [0, 0.8, -0.24], note: 'tepe noktası' });
  m.meta.place = [{ pos: [0.5, 0, -0.5], yaw: 90, note: 'örnek: Rev2 raid.html intents durumu (u 0, v 0)' }];
  return m;
}

// ---------------------------------------------------------------- 7 · Replicator
export function Replicator() {
  const m = new Model('Replicator', { family: FAM, budget: 'unit', listName: 'Çoğaltıcı', origin: ORIGIN,
    note: 'Paletli yarım-tonoz hangar (kavisli çatı, iki nervür), önde kemerli karanlık bölme ve rampa, tepede iniş pisti ve göndereceği küçük dron (MiniDrone; göz makinenin). Göz ön yüzün sol üstünde. Biçim: ekmek somunu çatı + üstte küçük dron. ' + MACHINE_NOTE });
  machineBase(m, 0.31, 24); tracks(m);
  const arc = (sx) => [0, 30, 60, 90].map(t => [sx * (0.1 + 0.12 * Math.cos(t * D2R)), 0.3 + 0.14 * Math.sin(t * D2R)]);
  const outline = [[-0.22, 0.17], [0.22, 0.17], ...arc(1), ...arc(-1).reverse()];
  addFaced(m, gExtrude(outline, 0.42, 0.015), BODY, [0, 0, 0]);                                                    // hangar, along z
  // two roof ribs: U band 0.012 proud of the roof, 0.03 wide
  const off = (p, k) => { const [x, y] = p; if (y <= 0.3001) return [x + Math.sign(x) * k, y]; const cx = Math.sign(x) * 0.1; const dx = x - cx, dy = y - 0.3; const L = Math.hypot(dx / 0.12, dy / 0.14) || 1; return [x + k * dx / 0.12 / L, y + k * dy / 0.14 / L]; };
  const roof = [[0.22, 0.17], ...arc(1), ...arc(-1).reverse(), [-0.22, 0.17]];
  const rib = [...roof.map(p => off(p, 0.013)), ...roof.slice().reverse()];
  for (const z of [-0.08, 0.1]) m.add(gExtrude(rib, 0.03, 0), 'r_gunW', [0, 0, z]);
  const bay = [[-0.12, 0.18], [0.12, 0.18], [0.12, 0.31], ...[30, 60, 90, 120, 150].map(t => [0.12 * Math.cos(t * D2R), 0.31 + 0.12 * Math.sin(t * D2R)]), [-0.12, 0.31]];
  m.extrude(bay, 0.012, 0, 'r_gunD', [0, 0, -0.216], [0, 0, 0], 'Body', NOSH);                                   // dark arched bay
  m.box(0.22, 0.025, 0.2, 0.006, 'r_gunS', [0, 0.12, -0.31], [-30, 0, 0]);                                         // ramp
  eye(m, [-0.168, 0.335, -0.21], [-90, 0, 0], 0.028);
  m.lathe([[0.15, 0.44], [0.15, 0.458], [0.138, 0.468], [0, 0.468]], 20, 'r_gunD', [0, 0, 0], [0, 0, 0], 'Body', { caps: false }); // landing pad
  m.add(polyGeom(flatArc(0.095, 0.122, 0, Math.PI * 2, 20, 0.4685), 0), 'r_steelPin', [0, 0, 0], [0, 0, 0], 'Body', NOSH); // pad ring
  // MiniDrone: the drone it is about to send (0.45 scale; no eye — the eye stays the machine's)
  const pv = [0, 0.468, 0];
  m.part('MiniDrone', { pivot: pv, axis: 'y', kind: 'slide', range: [0, 0.15], rest: 0, note: 'pistten kalkar (uyandırma/kopyalama anı)' });
  m.lathe([[0.03, 0.468], [0.03, 0.478], [0.012, 0.484]], 8, 'r_gunD', [0, 0, 0], [0, 0, 0], 'MiniDrone', { caps: false, cast: false });
  m.lathe([[0.012, 0.484], [0.06, 0.49], [0.06, 0.508], [0.04, 0.518], [0, 0.521]], 12, 'r_gun', [0, 0, 0], [0, 0, 0], 'MiniDrone', { caps: false, cast: false });
  for (const a of [45, -45]) m.box(0.25, 0.025, 0.025, 0, 'r_gunS', [0, 0.5, 0], [0, a, 0], 'MiniDrone', NOSH);
  const d = 0.13 / Math.SQRT2;
  for (const sx of [-1, 1]) for (const sz of [-1, 1]) m.lathe([[0.046, 0.506], [0.046, 0.518], [0, 0.518]], 10, 'r_gunD', [sx * d, 0, sz * d], [0, 0, 0], 'MiniDrone', { caps: false, cast: false });
  m.anchor('Top', { pos: [0, 0.521, 0], note: 'tepe noktası' });
  m.meta.place = [{ pos: [6.5, 0, -6.5], yaw: 90, note: 'örnek: Rev2 raid.html intents durumu (u 6, v 6)' }];
  return m;
}

// ---------------------------------------------------------------- 8 · Bomber
export function Bomber() {
  const m = new Model('Bomber', { family: FAM, budget: 'unit', listName: 'Patlayan robot', origin: ORIGIN,
    note: 'Deniz mayını gövde: küre (r 0,2), ekvator flanşı, yedi fünye boynuzu (1 tepede + 6 tanesi 24° yükseklikte), kısa ayak; göz önde ekvatorun altında. Biçim: dikenli top. Tepe 0,66. ' + MACHINE_NOTE });
  machineBase(m, 0.26, 20);
  m.lathe([[0.085, 0.05], [0.085, 0.16], [0.07, 0.18], [0.05, 0.19]], 12, 'r_gunD', [0, 0, 0], [0, 0, 0], 'Body', { caps: false }); // stem
  const cy = 0.38, R = 0.2;
  m.add(gSphere(R, 18, 12), 'r_gun', [0, cy, 0]);
  m.lathe([[0.199, -0.02], [0.211, -0.012], [0.211, 0.012], [0.199, 0.02]], 18, 'r_gunS', [0, cy, 0], [0, 0, 0], 'Body', { caps: false, cast: false }); // equator flange
  const dirs = [[0, 1, 0]]; for (let i = 0; i < 6; i++) { const a = (i * 60 + 30) * D2R, e = 24 * D2R; dirs.push([Math.sin(a) * Math.cos(e), Math.sin(e), -Math.cos(a) * Math.cos(e)]); }
  for (const [dx, dy, dz] of dirs) {
    const pos = [dx * (R - 0.004), cy + dy * (R - 0.004), dz * (R - 0.004)];
    const rx = Math.atan2(Math.hypot(dx, dz), dy) / D2R, ry = Math.atan2(dx, dz) / D2R;           // +y → (dx, dy, dz): tilt about x, then yaw
    m.lathe([[0.025, 0], [0.019, 0.055], [0.025, 0.06], [0.022, 0.078], [0, 0.088]], 6, 'r_steelPin', pos, [rx, ry, 0], 'Body', { caps: false, cast: false });
  }
  const ey = 0.32, ez = -Math.sqrt(R * R - (ey - cy) ** 2) + 0.004;
  eye(m, [0, ey, ez], [-90 - Math.asin((cy - ey) / R) / D2R, 0, 0], 0.034);
  m.anchor('Top', { pos: [0, cy + R + 0.084, 0], note: 'tepe noktası (≈0,66)' });
  m.meta.place = [{ pos: [6.5, 0, -1.5], yaw: 90, note: 'örnek: Rev2 raid.html intents durumu (u 6, v 1)' }];
  return m;
}

// ---------------------------------------------------------------- pawns (specialists)
const FONT = new Font(GLYPHS);
const LETTER = { size: 0.5, depth: 0.13, bt: 0.016, bs: 0.012 };
function letterGeom(ch) {
  const shapes = FONT.generateShapes(ch, LETTER.size);
  const g = new THREE.ExtrudeGeometry(shapes, { depth: LETTER.depth - 2 * LETTER.bt, bevelEnabled: true, bevelThickness: LETTER.bt, bevelSize: LETTER.bs, bevelOffset: -LETTER.bs, bevelSegments: 1, curveSegments: ch === 'S' ? 2 : 3, steps: 1 });
  g.computeBoundingBox(); const bb = g.boundingBox;
  g.translate(-(bb.min.x + bb.max.x) / 2, -bb.min.y, -(bb.min.z + bb.max.z) / 2);
  const w = bb.max.x - bb.min.x; if (w > 0.5) g.scale(0.5 / w, 1, 1);          // M is condensed to fit the Ø0.54 base
  g.computeBoundingBox();
  return { g: prep(g, { crease: 35 }), box: g.boundingBox };
}
const letterKey = (n) => n[2] < -0.7 ? 'front' : n[2] > 0.7 ? 'back' : 'side';
function pawnBase(m) {
  const R = 0.27, h = 0.07, c = 0.014, w = 0.024;
  m.lathe([[R, 0], [R, h - c]], 24, 'r_base', [0, 0, 0], [0, 0, 0], 'Body', { caps: false });
  m.lathe([[R, h - c], [R - c, h], [R - c - w, h]], 24, 'r_ring', [0, 0, 0], [0, 0, 0], 'Body', { caps: false, cast: false });
  m.lathe([[R - c - w, h], [0, h]], 24, 'r_base', [0, 0, 0], [0, 0, 0], 'Body', { caps: false, cast: false });
}
const PAWN_STATES = {
  normal: { pose: { Tilt: 0, Letter: 0 }, show: [], note: 'hazır: ahşap ön yüz kameraya' },
  done: { pose: { Letter: 180 }, show: ['Tick'], note: 'tur bitti: harf Y’de 180° döner, koyu kraft arka yüz (r_pawnDone) öne gelir; tabanda yeşil tik' },
  wounded: { pose: {}, show: ['Tape'], note: 'yaralı: harfin önünde çapraz bant (harfle döner)' },
  plated: { pose: {}, show: ['Plate'], note: 'plakalı: tabanın önünde çelik kalkan plakası' },
  down: { pose: { Tilt: -70 }, show: [], note: 'düştü: harf ön alt kenarı üstünde 70° öne (kameraya doğru) yüzüstü yatar; bant harfle gider. Plaka bu durumda kalkmış olur (plaka hasarı önce yer).' },
};
function Pawn(letter, roleTR) {
  const name = 'Pawn_' + letter;
  const m = new Model(name, { family: FAM, budget: 'unit', listName: `Uzman piyonu ${letter} · ${roleTR}`, origin: ORIGIN, states: PAWN_STATES,
    note: `Kalın ahşap harf (Droid Serif Bold, ExtrudeGeometry, pah 1 segment; ön yüz ve yanlar r_pawnWood, arka yüz koyu kraft r_pawnDone) siyah Ø0,54 tabanda, krem halka. Harf −z’ye bakar. Durumlar: tur bitti = Letter 180° + Tick; yaralı = Tape; plakalı = Plate; düştü = Tilt −70° (öne yüzüstü). Tick/Tape/Plate görünürlük parçaları.` });
  pawnBase(m);
  const { g, box } = letterGeom(letter); const H = box.max.y, Wl = box.max.x - box.min.x, t = LETTER.depth;
  m.part('Tilt', { pivot: [0, 0.07, -t / 2], axis: 'x', range: [0, -70], rest: 0, note: 'düşme: menteşe harfin ön alt kenarında; −70° öne yüzüstü yatar (Unity’de −x dönüşü üstü −z’ye götürür). Kameraya dönük piyonda en açık “düştü” okuması bu.' });
  m.part('Letter', { parent: 'Tilt', pivot: [0, 0.07, 0], axis: 'y', range: [0, 180], rest: 0, note: 'tur bitti: 180° döner, kraft arka yüz öne gelir' });
  addFaced(m, g, { front: 'r_pawnWood', back: 'r_pawnDone', side: 'r_pawnWood' }, [0, 0.07, 0], [0, 0, 0], 'Letter', {}, letterKey);
  // Tape (wounded): diagonal strip on the letter front, moves with the letter
  const tpos = [0, 0.07 + H * 0.52, -t / 2 - 0.004];
  m.part('Tape', { parent: 'Letter', pivot: tpos, kind: 'swap', note: 'yaralı: görünür/gizli (r_tape, yarı saydam)' });
  m.box(Math.min(0.4, Wl * 0.92), 0.072, 0.006, 0, 'r_tape', tpos, [0, 0, -24], 'Tape', NOSH);
  // Tick (turn done): cream sticker with a green tick, flat on the base top in front of the letter
  const kp = [-0.075, 0.0712, -0.16];
  m.part('Tick', { pivot: kp, kind: 'swap', note: 'tur bitti: görünür/gizli' });
  m.add(gDisc(0.064, 16), 'r_sticker', kp, [90, 0, 0], 'Tick', NOSH);
  const tick = [[-0.038, 0.004], [-0.02, -0.014], [-0.006, 0.002], [0.034, -0.044], [0.05, -0.028], [-0.006, 0.034]];
  m.add(extrudeXZ(tick.map(([x, z]) => [x, -z]), 0.004, 0), 'rp_tick', [kp[0], kp[1] + 0.002, kp[2]], [0, 0, 0], 'Tick', NOSH);
  // Plate (steel plate): heater-shield plate leaning on the base front-right, 3 rivets
  const pp = [0.14, 0.07 + 0.09, -0.185];
  m.part('Plate', { pivot: pp, kind: 'swap', note: 'plakalı: görünür/gizli (etiketteki kalkan simgesiyle aynı biçim)' });
  const sh = [[-0.085, 0.09], [0.085, 0.09], [0.085, -0.012], [0.048, -0.062], [0, -0.09], [-0.048, -0.062], [-0.085, -0.012]];
  m.extrude(sh, 0.025, 0.007, 'r_armor_light', pp, [12, -8, 0], 'Plate');
  const pr = [12, -8, 0], q = new THREE.Object3D(); setUnityEuler(q, pr); q.updateMatrix();
  for (const [x, y] of [[-0.055, 0.06], [0.055, 0.06], [0, -0.045]]) {
    const v = new THREE.Vector3(x, y, 0.0135).applyMatrix4(q.matrix);                                 // local (x, y, −0.0135) Unity, in three space
    m.lathe([[0.013, 0], [0.01, 0.007], [0, 0.009]], 6, 'r_armor_dark', [pp[0] + v.x, pp[1] + v.y, pp[2] - v.z], [-90 + pr[0], pr[1], 0], 'Plate', NOSH);
  }
  m.anchor('Tag', { pos: [0, 0.035, -0.27], note: 'uzman can etiketi: altlığın önü (u−0,24, v+0,24 ekranda)' });
  m.anchor('Top', { pos: [0, 0.07 + H, 0], note: 'harfin tepesi' });
  return m;
}

// ---------------------------------------------------------------- cabinets (static; per-face top/west/south colours like the wall blocks)
function armourCorners(m, W, h, D, s = 0.07, t = 0.022) {
  for (const sx of [-1, 1]) for (const sz of [-1, 1]) {
    const X = sx * W / 2, Z = sz * D / 2;
    let pts = [[X, Z], [X - sx * s, Z], [X - sx * s, Z - sz * t], [X - sx * t, Z - sz * t], [X - sx * t, Z - sz * s], [X, Z - sz * s]];
    if (sx * sz < 0) pts = pts.reverse();
    m.add(extrudeXZ(pts, h, 0.006), 'r_armor_mid', [0, h / 2, 0]);
  }
}
export function NodeCabinet() {
  const m = new Model('NodeCabinet', { family: FAM, budget: 'raid', listName: 'Düğüm dolabı (4 can, zırhlı)', origin: ORIGIN,
    note: 'Sunucu düğüm dolabı 0,74 × 0,92 × 0,74: yüz başına boya (r_node_top/west/south), dört köşede L kesitli zırh şeridi (0,07), tepede kırmızı düğüm simgesi (YZ), güneyde krem etiket, batıda dört sunucu yuvası. Dönmez.' });
  fbox(m, 0.7, 0.9, 0.7, 0.02, cabMats('node'), [0, 0.45, 0], [0, 0, 0], 'Body', { skip: ['-y'] });
  armourCorners(m, 0.74, 0.92, 0.74);
  m.box(0.3, 0.01, 0.3, 0, 'r_red', [0, 0.905, 0], [0, 0, 0], 'Body', NOSH);                                       // red node icon
  m.box(0.4, 0.12, 0.008, 0, 'r_sticker', [0, 0.64, -0.354], [0, 0, 0], 'Body', NOSH);                              // cream label
  for (const y of [0.3, 0.4, 0.5, 0.6]) m.box(0.008, 0.032, 0.44, 0, 'rp_slot', [-0.354, y, 0], [0, 0, 0], 'Body', NOSH); // server slots
  m.anchor('Top', { pos: [0, 0.92, 0], note: 'tepe noktası (hedef etiketi 28 px üstte)' });
  m.meta.place = [{ pos: [5.5, 0, -4.5], yaw: 0, note: 'örnek: Rev2 raid.html hedef (u 5, v 4)' }];
  return m;
}
export function CoreCabinet() {
  const m = new Model('CoreCabinet', { family: FAM, budget: 'raid', listName: 'Çekirdek dolabı (5 can, zırhlı)', origin: ORIGIN,
    note: 'Çekirdek dolabı 0,80 × 1,0 × 0,80: r_core_* yüz boyası, köşe zırh şeritleri, tepede kırmızı düğüm simgesi, güneyde kırmızı “Ç” rozeti (harf TMP), batıda beş yuva.' });
  fbox(m, 0.76, 0.98, 0.76, 0.02, cabMats('core'), [0, 0.49, 0], [0, 0, 0], 'Body', { skip: ['-y'] });
  armourCorners(m, 0.8, 1.0, 0.8);
  m.box(0.32, 0.01, 0.32, 0, 'r_red', [0, 0.985, 0], [0, 0, 0], 'Body', NOSH);
  m.lathe([[0.115, 0], [0.115, 0.012], [0, 0.012]], 16, 'r_red', [0, 0.64, -0.38], [-90, 0, 0], 'Body', NOSH); // red badge
  m.text('TXT_Badge', { center: [0, 0.64, -0.3925], size: [0.17, 0.17], normal: [0, 0, -1], role: 'stamp (rozet harfi, krem #F1E9D6)', font: 'Courier Prime 700', sample: 'Ç', sampleEn: 'C', note: 'büyük harf ≈ 0,13 birim (raid pozunda ~14 px)' });
  for (const y of [0.3, 0.4, 0.5, 0.6, 0.7]) m.box(0.008, 0.032, 0.48, 0, 'rp_slot', [-0.384, y, 0], [0, 0, 0], 'Body', NOSH);
  m.anchor('Top', { pos: [0, 1.0, 0], note: 'tepe noktası' });
  return m;
}
export function BackupCore() {
  const m = new Model('BackupCore', { family: FAM, budget: 'raid', listName: 'Yedek çekirdek (2 can)', origin: ORIGIN,
    note: 'Yedek çekirdek 0,6 × 0,58 × 0,6, zırhsız: r_backup_* yüz boyası, güneyde kesik çizgili kırmızı halka içinde “Ç” (TMP) ve krem YEDEK etiketi (TMP), tepede küçük kırmızı simge, batıda iki yuva.' });
  fbox(m, 0.6, 0.58, 0.6, 0.02, cabMats('backup'), [0, 0.29, 0], [0, 0, 0], 'Body', { skip: ['-y'] });
  m.box(0.2, 0.008, 0.2, 0, 'r_red', [0, 0.584, 0], [0, 0, 0], 'Body', NOSH);
  const bc = [0, 0.37, -0.304];
  for (let i = 0; i < 8; i++) { const a = i * 45 + 22.5, r = 0.1; m.box(0.05, 0.026, 0.008, 0, 'r_red', [bc[0] + r * Math.cos(a * D2R), bc[1] + r * Math.sin(a * D2R), bc[2]], [0, 0, a + 90], 'Body', NOSH); }
  m.text('TXT_Badge', { center: [bc[0], bc[1], -0.309], size: [0.15, 0.15], normal: [0, 0, -1], role: 'stamp (kırmızı #9A1E14)', sample: 'Ç', sampleEn: 'C', note: 'büyük harf ≈ 0,12 birim (~13 px)' });
  m.box(0.4, 0.11, 0.008, 0, 'r_sticker', [0, 0.155, -0.304], [0, 0, 0], 'Body', NOSH);
  m.text('TXT_Label', { center: [0, 0.155, -0.3085], size: [0.36, 0.08], normal: [0, 0, -1], role: 'stamp (mürekkep #1E1B16)', sample: 'YEDEK', sampleEn: 'BACKUP', note: 'büyük harf ≈ 0,07 birim (~8 px): süs; bilgi can etiketinde ve kartta' });
  for (const y of [0.3, 0.42]) m.box(0.008, 0.032, 0.36, 0, 'rp_slot', [-0.304, y, 0], [0, 0, 0], 'Body', NOSH);
  m.anchor('Top', { pos: [0, 0.58, 0], note: 'tepe noktası' });
  return m;
}
export function CopyCabinet() {
  const m = new Model('CopyCabinet', { family: FAM, budget: 'raid', listName: 'Kopya dolabı (2 can)', origin: ORIGIN,
    note: 'Kopya dolabı 0,72 × 0,8 × 0,72: r_copy_* yüz boyası, koyu kaide bandı, güneyde çerçeveli ayna paneli, tepede üst üste iki krem kart (kopya simgesi).' });
  fbox(m, 0.72, 0.75, 0.72, 0.02, cabMats('copy'), [0, 0.425, 0], [0, 0, 0], 'Body', { skip: ['-y'] });       // y 0.05–0.80
  m.box(0.7, 0.05, 0.7, 0.008, 'r_copy_south', [0, 0.025, 0], [0, 0, 0], 'Body', NOSH);                              // plinth
  m.box(0.5, 0.58, 0.014, 0.006, 'r_armor_dark', [0, 0.43, -0.366]);                                                 // mirror frame
  m.box(0.44, 0.52, 0.008, 0, 'r_mirror', [0, 0.43, -0.376], [0, 0, 0], 'Body', NOSH);                               // mirror
  m.box(0.26, 0.008, 0.18, 0, 'r_sticker', [-0.05, 0.804, -0.05], [0, 0, 0], 'Body', NOSH);                          // copy icon: two cards
  m.box(0.26, 0.008, 0.18, 0, 'r_sticker', [0.05, 0.812, 0.05], [0, 0, 0], 'Body', NOSH);
  m.anchor('Top', { pos: [0, 0.816, 0], note: 'tepe noktası' });
  m.meta.place = [{ pos: [7.5, 0, -0.5], yaw: 0, note: 'örnek: Rev2 raid.html (u 7, v 0)' }, { pos: [7.5, 0, -7.5], yaw: 0, note: '(u 7, v 7)' }];
  return m;
}

// ---------------------------------------------------------------- loot crate (redless) and its emptied tray
function ropeHandle(m, sx, y, X = 0.351, b = 0.024) {
  const x = sx * X, P = [[x, y, -0.075], [x + sx * b * 0.7, y - 0.05, -0.05], [x + sx * b, y - 0.07, 0], [x + sx * b * 0.7, y - 0.05, 0.05], [x, y, 0.075]];
  m.add(gTube(P.map(T3), 0.013, 4, 6), 'rp_rope', [0, 0, 0], [0, 0, 0], 'Body', NOSH);
  for (const z of [-0.075, 0.075]) m.box(0.024, 0.04, 0.034, 0, 'rp_crateBatten', [sx * (X + 0.001), y + 0.004, z], [0, 0, 0], 'Body', NOSH);
}
export function LootCrate() {
  const m = new Model('LootCrate', { family: FAM, budget: 'raid', listName: 'İkmal sandığı (yağma hedefi)', origin: ORIGIN,
    note: 'KYGA sivil savunma sandığı 0,70 × 0,48 × 0,56: zeytin yeşili boyalı ahşap, kapak, orta çıta bandı, sekiz çelik köşe başlığı, iki ucunda halat tutamak, önde çelik çerçeveli krem kargo etiketi (yazısız: kâğıtta harf/rakam kare adlarına ayrılmış), kapakta KYGA amblemi (doku). Kırmızı yok: açılacak şey, tehdit değil. Düğüm dolabından alçak.' });
  m.box(0.68, 0.44, 0.54, 0.018, 'rp_crateOlive', [0, 0.22, 0], [0, 0, 0], 'Body', { skip: ['-y'] });
  m.box(0.7, 0.05, 0.56, 0.012, 'rp_crateOlive', [0, 0.455, 0]);                                                     // lid
  for (const sz of [-1, 1]) m.box(0.68, 0.06, 0.02, 0, 'rp_crateBatten', [0, 0.2, sz * 0.278], [0, 0, 0], 'Body', NOSH); // battens
  for (const sx of [-1, 1]) for (const sz of [-1, 1]) for (const y of [0.035, 0.445]) m.box(0.07, 0.07, 0.07, 0, 'rp_crateSteel', [sx * 0.33, y, sz * 0.26], [0, 0, 0], 'Body', NOSH);
  for (const sx of [-1, 1]) ropeHandle(m, sx, 0.33);
  m.box(0.24, 0.14, 0.01, 0, 'rp_crateSteel', [0.14, 0.3, -0.29], [0, 0, 0], 'Body', NOSH);                           // label holder
  m.box(0.2, 0.1, 0.006, 0, 'r_sticker', [0.14, 0.3, -0.297], [0, 0, 0], 'Body', NOSH);                                // cream cargo label (blank)
  m.add(gPlane(0.3, 0.3), 'rp_crateEmblem', [0, 0.4815, 0], [90, 0, 0], 'Body', NOSH);                                // KYGA emblem decal (UV 0–1)
  m.anchor('Top', { pos: [0, 0.48, 0], note: 'tepe noktası' });
  return m;
}
export function LootCrateOpen() {
  const m = new Model('LootCrateOpen', { family: FAM, budget: 'raid', listName: 'Boşalmış ikmal sandığı', origin: ORIGIN,
    note: 'Teknisyen sandığı alınca kalan alçak tepsi: 0,68 × 0,20 × 0,56 (≤ 0,25, ±0,37 içinde), içi koyu, içinde iki gevşek çıta (biri kenara yaslı), halat tutamaklar ve dört çelik köşe. Kare yürünür; üstündeki birimi örtmez.' });
  const W = 0.68, D = 0.56, t = 0.03, h = 0.2;
  const outer = [[-W / 2, -D / 2], [W / 2, -D / 2], [W / 2, D / 2], [-W / 2, D / 2]], inner = [[-W / 2 + t, -D / 2 + t], [-W / 2 + t, D / 2 - t], [W / 2 - t, D / 2 - t], [W / 2 - t, -D / 2 + t]];
  const tray = bake(gExtrude(outer, h, 0.01, { holes: [inner] }), [90, 0, 0]);
  const inKey = (n, c) => (n[1] < 0.5 && Math.abs(c[0]) < W / 2 - t + 0.012 && Math.abs(c[2]) < D / 2 - t + 0.012 && n[0] * -c[0] + n[2] * -c[2] > 0) ? 'in' : 'out';
  addFaced(m, tray, { in: 'rp_crateInside', out: 'rp_crateOlive' }, [0, h / 2, 0], [0, 0, 0], 'Body', {}, inKey);
  m.box(W - 2 * t, 0.02, D - 2 * t, 0, 'rp_crateInside', [0, 0.03, 0], [0, 0, 0], 'Body', NOSH);                    // floor
  m.box(0.6, 0.025, 0.07, 0.006, 'rp_crateBatten', [-0.03, 0.054, -0.1], [0, 12, 0], 'Body', NOSH);                // loose batten on the floor
  m.box(0.66, 0.026, 0.07, 0.006, 'rp_crateBatten', [0.02, 0.15, 0.09], [0, -18, 10], 'Body', NOSH);               // batten leaning on the rim
  for (const sx of [-1, 1]) for (const sz of [-1, 1]) m.box(0.07, 0.07, 0.07, 0, 'rp_crateSteel', [sx * 0.32, 0.165, sz * 0.26], [0, 0, 0], 'Body', NOSH);
  for (const sx of [-1, 1]) ropeHandle(m, sx, 0.15, 0.341, 0.012);
  m.anchor('Top', { pos: [0, 0.2, 0], note: 'tepe noktası' });
  return m;
}

// ---------------------------------------------------------------- cooling unit (reserved sign: upright cylinder + fan + yellow triangle)
export function CoolingUnit() {
  const m = new Model('CoolingUnit', { family: FAM, budget: 'raid', listName: 'Soğutma ünitesi (1 can, patlar)', origin: ORIGIN,
    note: 'Dik silindir r 0,33 × 0,42, 16 kenar; üstte koyu oyuk, üç kanatlı fan (Fan) ve üç düz ızgara kayışı; altta koyu ayak bandı; güneybatıya bakan yüzde sarı uyarı üçgeni (siyah kenarlı). Ayrılmış işaret: dik silindir + fan + sarı üçgen. Dönmez.' });
  const ph = { phiStart: -11.25 * D2R };                                                                             // faces point S, SW, W …
  m.lathe([[0.336, 0], [0.336, 0.03], [0.33, 0.036]], 16, 'r_cooling_south', [0, 0, 0], [0, 0, 0], 'Body', { ...ph, caps: false, cast: false }); // foot band
  m.lathe([[0.33, 0.036], [0.33, 0.392]], 16, 'r_cooling_west', [0, 0, 0], [0, 0, 0], 'Body', { ...ph, caps: false });
  m.lathe([[0.33, 0.392], [0.312, 0.42], [0.285, 0.42], [0.285, 0.37]], 16, 'r_cooling_top', [0, 0, 0], [0, 0, 0], 'Body', { ...ph, caps: false });
  m.lathe([[0.285, 0.37], [0, 0.37]], 16, 'r_grille', [0, 0, 0], [0, 0, 0], 'Body', { ...ph, caps: false, cast: false }); // recess floor
  for (let i = 0; i < 3; i++) m.box(0.56, 0.012, 0.03, 0, 'r_grille', [0, 0.414, 0], [0, i * 60 + 15, 0], 'Body', NOSH); // grille straps
  m.lathe([[0.045, 0.408], [0.045, 0.418], [0, 0.423]], 8, 'r_grille', [0, 0, 0], [0, 0, 0], 'Body', { caps: false, cast: false });
  m.part('Fan', { pivot: [0, 0.388, 0], axis: 'y', range: [0, 360], rest: 0, note: 'fan sürekli döner (soğutma çalışıyor); patlayınca durur' });
  m.lathe([[0.05, 0.38], [0.05, 0.392], [0, 0.396]], 8, 'rp_fanBlade', [0, 0, 0], [0, 0, 0], 'Fan', { caps: false, cast: false });
  for (let i = 0; i < 3; i++) { const a = i * 120 * D2R; m.box(0.2, 0.01, 0.075, 0, 'rp_fanBlade', [0.15 * Math.sin(a), 0.388, -0.15 * Math.cos(a)], [20, i * 120 + 90, 0], 'Fan', NOSH); }
  // warning triangle on the SW face (inradius of the 16-gon)
  const rin = 0.33 * Math.cos(11.25 * D2R), dir = [-Math.SQRT1_2, 0, -Math.SQRT1_2];
  const tri = (s) => { const h = s * Math.sqrt(3) / 2; return [[0, h * 2 / 3], [-s / 2, -h / 3], [s / 2, -h / 3]]; };
  m.extrude(tri(0.13), 0.004, 0, 'r_base', [dir[0] * (rin + 0.002), 0.235, dir[2] * (rin + 0.002)], [0, 45, 0], 'Body', NOSH);
  m.extrude(tri(0.1), 0.004, 0, 'r_warn', [dir[0] * (rin + 0.004), 0.235, dir[2] * (rin + 0.004)], [0, 45, 0], 'Body', NOSH);
  m.anchor('Top', { pos: [0, 0.42, 0], note: 'tepe noktası' });
  m.meta.place = [{ pos: [1.5, 0, -5.5], yaw: 0, note: 'örnek: Rev2 raid.html (u 1, v 5)' }, { pos: [6.5, 0, -6.5], yaw: 0, note: '(u 6, v 6)' }];
  return m;
}

// ---------------------------------------------------------------- event objects (redless, matte)
export function Tube() {
  const m = new Model('Tube', { family: FAM, budget: 'raid', listName: 'Basınçlı tüp (Güney Amerika olayı)', origin: ORIGIN,
    note: 'Dik gaz tüpü Ø0,2 × 0,75: koyu gri gövde, turuncu omuz bandı (ayrılmış işaret), vana gövdesi ve altıgen başlık (el çarkı yok: o kayar rafın işareti), çelik koruma yakası, çapraz iki çıtalı ahşap ayak. Fan ve sarı üçgen yok: soğutma ünitesinden incelik, bant ve fansızlıkla ayrılır. Fırlar (patlamaz).' });
  for (const a of [0, 90]) m.box(0.34, 0.04, 0.06, 0.01, 'woodRaw', [0, 0.02, 0], [0, a + 20, 0], 'Body', { skip: ['-y'], cast: false }); // two-slat foot
  m.lathe([[0.1, 0.04], [0.1, 0.5]], 12, 'rp_tubeGrey', [0, 0, 0], [0, 0, 0], 'Body', { caps: false });
  m.lathe([[0.1, 0.5], [0.1, 0.565], [0.072, 0.622], [0.034, 0.64]], 12, 'r_hazard', [0, 0, 0], [0, 0, 0], 'Body', { caps: false }); // orange shoulder band
  m.lathe([[0.034, 0.64], [0.03, 0.66], [0, 0.66]], 12, 'rp_tubeGrey', [0, 0, 0], [0, 0, 0], 'Body', { caps: false, cast: false }); // neck
  m.box(0.05, 0.06, 0.05, 0.008, 'r_steelPin', [0, 0.69, 0], [0, 0, 0], 'Body', NOSH);                              // valve body
  m.lathe([[0, 0], [0.014, 0], [0.014, 0.04], [0, 0.04]], 6, 'r_steelPin', [0.02, 0.69, 0], [0, 0, -90], 'Body', NOSH); // outlet stub
  m.lathe([[0.026, 0.72], [0.026, 0.738], [0.018, 0.75], [0, 0.75]], 6, 'r_armor_dark', [0, 0, 0], [0, 0, 0], 'Body', { caps: false, cast: false }); // hex cap
  m.lathe([[0.075, 0.645], [0.075, 0.718], [0.062, 0.718], [0.062, 0.66]], 12, 'r_armor_dark', [0, 0, 0], [0, 0, 0], 'Body', { caps: false }); // guard collar
  m.anchor('Top', { pos: [0, 0.75, 0], note: 'tepe noktası' });
  return m;
}
export function WeldArmBase() {
  const m = new Model('WeldArmBase', { family: FAM, budget: 'raid', listName: 'Kaynak kolu kaidesi (Çin olayı)', origin: ORIGIN,
    note: 'Cıvatalı 0,7 çelik plaka, ayak ve Ø0,4 döner tabla (Turn, tepesi 0,30 = WeldArmHeight); tablanın üstünde turuncu kaynak halkası ve kolun yönünü gösteren turuncu burun (−z = kolun yönü). Kırmızı yok.' });
  m.box(0.7, 0.04, 0.7, 0.012, 'rp_steelPlate', [0, 0.02, 0], [0, 0, 0], 'Body', { skip: ['-y'], cast: false });
  for (const sx of [-1, 1]) for (const sz of [-1, 1]) m.lathe([[0.022, 0.04], [0.022, 0.052], [0, 0.052]], 6, 'rp_steelDark', [sx * 0.29, 0, sz * 0.29], [0, 0, 0], 'Body', { caps: false, cast: false });
  m.lathe([[0.16, 0.04], [0.13, 0.065], [0.13, 0.215]], 12, 'rp_steelDark', [0, 0, 0], [0, 0, 0], 'Body', { caps: false });
  m.part('Turn', { pivot: [0, 0.215, 0], axis: 'y', range: [0, 360], rest: 0, note: 'döner tabla: kolun yönü (0° = −z); WeldArm buna takılır' });
  m.lathe([[0.2, 0.215], [0.2, 0.285], [0.186, 0.3]], 16, 'rp_steelDark', [0, 0, 0], [0, 0, 0], 'Turn', { caps: false });
  m.lathe([[0.186, 0.3], [0.13, 0.3]], 16, 'r_hazard', [0, 0, 0], [0, 0, 0], 'Turn', { caps: false, cast: false });   // orange weld ring
  m.lathe([[0.13, 0.3], [0, 0.3]], 16, 'rp_steelPlate', [0, 0, 0], [0, 0, 0], 'Turn', { caps: false, cast: false });
  m.add(extrudeXZ([[-0.065, -0.17], [0.065, -0.17], [0, -0.275]], 0.05, 0.008), 'r_hazard', [0, 0.275, 0], [0, 0, 0], 'Turn', NOSH); // direction nose
  m.anchor('ArmRoot', { part: 'Turn', pos: [0, 0.3, 0], note: 'kolun kökü (WeldArmHeight 0,30)' });
  return m;
}
export function WeldArm() {
  const m = new Model('WeldArm', { family: FAM, budget: 'raid', listName: 'Kaynak kolu (iki mafsal)', origin: 'karenin merkezi, tahta yüzeyi (y = 0); kök 0,30’da. WeldArmBase.Turn’ün çocuğu olunca yerel konum (0, −0,215, 0)',
    note: 'İki parçalı kaynak kolu: sabit omuz çatalı (tabla üstünde), Arm1 (omuz, 35° yukarı-öne), Arm2 (dirsek, 40° aşağı-öne), uçta aşağı bakan kaynak torcu. −z’ye uzanır (kaidenin turuncu burnuyla aynı yön). Kırmızı yok.' });
  m.box(0.14, 0.03, 0.12, 0.008, 'rp_steelDark', [0, 0.315, 0], [0, 0, 0], 'Body', NOSH);
  for (const sx of [-1, 1]) m.box(0.025, 0.1, 0.1, 0.006, 'rp_steelDark', [sx * 0.05, 0.37, 0]);
  const S = [0, 0.385, 0], L1 = 0.32, a1 = 35 * D2R, E = [0, S[1] + L1 * Math.sin(a1), -L1 * Math.cos(a1)], L2 = 0.28, a2 = 40 * D2R, Wr = [0, E[1] - L2 * Math.sin(a2), E[2] - L2 * Math.cos(a2)];
  m.part('Arm1', { pivot: S, axis: 'x', range: [-30, 30], rest: 0, note: 'omuz; + aşağı/öne, − yukarı' });
  m.lathe([[0, -0.0375], [0.04, -0.0375], [0.04, 0.0375], [0, 0.0375]], 10, 'rp_steelDark', S, [0, 0, 90], 'Arm1');
  m.box(0.06, 0.06, L1, 0.012, 'rp_steelPlate', [0, (S[1] + E[1]) / 2, (S[2] + E[2]) / 2], [35, 0, 0], 'Arm1');
  m.part('Arm2', { parent: 'Arm1', pivot: E, axis: 'x', range: [-40, 40], rest: 0, note: 'dirsek' });
  m.lathe([[0, -0.034], [0.035, -0.034], [0.035, 0.034], [0, 0.034]], 10, 'rp_steelDark', E, [0, 0, 90], 'Arm2', NOSH);
  m.box(0.05, 0.05, L2, 0.01, 'rp_steelPlate', [0, (E[1] + Wr[1]) / 2, (E[2] + Wr[2]) / 2], [-40, 0, 0], 'Arm2');
  m.lathe([[0.024, 0], [0.024, 0.045], [0.012, 0.075], [0, 0.082]], 8, 'r_steelPin', [Wr[0], Wr[1] + 0.02, Wr[2]], [180, 0, 0], 'Arm2', NOSH); // torch, pointing down
  m.anchor('Torch', { part: 'Arm2', pos: [Wr[0], Wr[1] - 0.062, Wr[2]], note: 'torç ucu (kaynak çizgisinin başı)' });
  return m;
}
// deterministic jitter
function jit(x, y, z, k) { const s = Math.sin(x * 127.1 + y * 311.7 + z * 74.7 + k * 17.3) * 43758.5453; return s - Math.floor(s) - 0.5; }
function lumpGeom(r, sx, sy, sz, seed) {
  const g = new THREE.IcosahedronGeometry(r, 0); const P = g.attributes.position;
  for (let i = 0; i < P.count; i++) { const x = P.getX(i), y = P.getY(i), z = P.getZ(i); const f = 1 + 0.35 * jit(+x.toFixed(4), +y.toFixed(4), +z.toFixed(4), seed);
    P.setXYZ(i, x * f * sx, Math.max(y * f * sy, -r * sy * 0.55), z * f * sz); }
  return prep(g, { crease: 15 });
}
export function CeilingDebris() {
  const m = new Model('CeilingDebris', { family: FAM, budget: 'raid', listName: 'Tavan enkazı (Afrika olayı)', origin: ORIGIN,
    note: 'Düşmüş tavan: kırık oluklu galvaniz sac (ortadan bükük), bükük I kiriş, filiz demirli beton topak ve küçük parça; altında is lekesi (decal, α .45, r 0,44). Tepe ≤ 0,36, kırmızısız. Kırılınca dağılır, moloz bırakmaz.' });
  m.add(gDisc(0.44, 16), 'rp_soot', [0, 0.003, 0], [90, 0, 0], 'Body', { cast: false, scale: [1, 0.82, 1] });
  // corrugated sheet: 11 points across (5 waves), 3 sections along with a kink; double-sided with edges
  const nx = 11, A = 0.018, half = 0.21; const secs = [[-0.3, 0.025], [0.0, 0.05], [0.3 * Math.cos(28 * D2R), 0.05 + 0.3 * Math.sin(28 * D2R)]];
  const up = (i) => { if (i === 0) return [0, 1, 0]; const a = i === 2 ? 28 * D2R : 14 * D2R; return [0, Math.cos(a), -Math.sin(a)]; };
  const Pt = (i, j, off) => { const x = -half + 2 * half * j / (nx - 1), wave = (j % 2 ? A : -A) + off, u = up(i); return [x, secs[i][1] + u[1] * wave, secs[i][0] + u[2] * wave]; };
  const L = [];
  for (const [off, sgn] of [[0.008, 1], [-0.008, -1]]) for (let i = 0; i < 2; i++) for (let j = 0; j < nx - 1; j++) {
    const c = [0, (secs[i][1] + secs[i + 1][1]) / 2 - sgn * 0.5, (secs[i][0] + secs[i + 1][0]) / 2];
    L.push({ pts: [Pt(i, j, off), Pt(i, j + 1, off), Pt(i + 1, j + 1, off), Pt(i + 1, j, off)], inside: c }); }
  for (const i of [0, 2]) for (let j = 0; j < nx - 1; j++) L.push({ pts: [Pt(i, j, 0.008), Pt(i, j + 1, 0.008), Pt(i, j + 1, -0.008), Pt(i, j, -0.008)], out: i === 0 ? [0, 0, -1] : [0, 0.47, 0.88] });
  for (const j of [0, nx - 1]) for (let i = 0; i < 2; i++) L.push({ pts: [Pt(i, j, 0.008), Pt(i + 1, j, 0.008), Pt(i + 1, j, -0.008), Pt(i, j, -0.008)], out: [j === 0 ? -1 : 1, 0, 0] });
  const sheet = polyGeom(L, 85);
  m.add(bake(sheet, [0, 24, 0]), 'rp_corrugated', [-0.06, 0, 0.02]);
  // kinked I-beam (two straight pieces)
  const ib = [[-0.03, -0.035], [0.03, -0.035], [0.03, -0.01], [0.0125, -0.01], [0.0125, 0.01], [0.03, 0.01], [0.03, 0.035], [-0.03, 0.035], [-0.03, 0.01], [-0.0125, 0.01], [-0.0125, -0.01], [-0.03, -0.01]];
  const beamPiece = (len) => bake(gExtrude(ib, len, 0), [0, 90, 0]);   // along x
  const by = 40 * D2R, bt = 24 * D2R, d1 = [Math.cos(by), 0, -Math.sin(by)], d2 = [Math.cos(by) * Math.cos(bt), Math.sin(bt), -Math.sin(by) * Math.cos(bt)];
  const c1 = [-0.14, 0.035, 0.1], e1 = c1.map((v, i) => v + d1[i] * 0.17), c2 = e1.map((v, i) => v + d2[i] * 0.13);
  m.add(bake(beamPiece(0.34), [0, 40, 0]), 'rp_beam', c1);
  m.add(bake(beamPiece(0.26), [0, 40, 24]), 'rp_beam', c2);
  // concrete lump with rebar, small chunk
  m.add(lumpGeom(0.11, 1.25, 0.85, 1.05, 1), 'rp_concrete', [0.2, 0.052, 0.2]);
  m.add(lumpGeom(0.06, 1.1, 0.8, 1.2, 2), 'rp_concrete', [-0.28, 0.027, -0.24], [0, 0, 0], 'Body', NOSH);
  m.cyl(0.0125, 0.2, 6, 0, 'rp_beam', [0.16, 0.2, 0.22], [0, 0, 28], 'Body', NOSH);
  m.cyl(0.0125, 0.16, 6, 0, 'rp_beam', [0.27, 0.17, 0.16], [-30, 0, -20], 'Body', NOSH);
  m.anchor('Top', { pos: [0, 0.3, 0], note: 'tepe noktası (≈0,30)' });
  return m;
}

// ---------------------------------------------------------------- 3D marks
export function WakePin() {
  const m = new Model('WakePin', { family: FAM, budget: 'raid', listName: 'Uyanma iğnesi ve halkası', origin: ORIGIN,
    note: 'Uyanacak makinenin karesi: iğne (Ø0,026, r_needle) + kırmızı top baş (YZ), boy ~0,235 (17 px); zeminde kırmızı halka r 0,31–0,345, sekiz ışın 0,38–0,47, iç kesik halka r 0,17 ve α .12 dolgu (asetat: Unlit, ZWrite kapalı, gölgesiz). Halka dokulu dörtgenle çiziliyorsa rp_markWake* parçaları atlanır.' });
  m.add(polyGeom(flatArc(0.31, 0.345, 0, Math.PI * 2, 36, 0.006), 0), 'rp_markWake', [0, 0, 0], [0, 0, 0], 'Body', NOSH);
  m.add(gDisc(0.31, 36), 'rp_markWakeFill', [0, 0.005, 0], [90, 0, 0], 'Body', NOSH);
  const rays = []; for (let i = 0; i < 8; i++) { const a = i * 45 * D2R, w = 0.0125 / 0.38, w2 = 0.0125 / 0.47; rays.push({ pts: [AP(0.38, 0.006, a - w), AP(0.47, 0.006, a - w2), AP(0.47, 0.006, a + w2), AP(0.38, 0.006, a + w)], out: [0, 1, 0] }); }
  const dash = []; for (let i = 0; i < 12; i++) { const a0 = i * 30 * D2R; dash.push(...flatArc(0.155, 0.183, a0, a0 + 15 * D2R, 1, 0.006)); }
  m.add(polyGeom([...rays, ...dash], 0), 'rp_markWake', [0, 0, 0], [0, 0, 0], 'Body', NOSH);
  m.lathe([[0, 0.004], [0.013, 0.03], [0.013, 0.19], [0, 0.19]], 8, 'r_needle', [0, 0, 0], [0, 0, 0], 'Body', { caps: false, cast: false });
  m.add(gSphere(0.036, 10, 7), 'r_red', [0, 0.2, 0]);
  m.anchor('Top', { pos: [0, 0.236, 0], note: 'iğne başının tepesi' });
  return m;
}
export function GhostRing() {
  const m = new Model('GhostRing', { family: FAM, budget: 'raid', listName: 'Kopyalama hedef halkası (kesik)', origin: ORIGIN,
    note: 'Kopyalama niyetinin hedef karesi: uyanma halkasının biçimi, 12 kesik (%60 mürekkep) r 0,312–0,348, sekiz ışın 0,38–0,47, α .10 dolgu; #854005 (zarar vermez), iğne yok. Asetat: Unlit, ZWrite kapalı, gölgesiz. Kopyalama uygulanınca WakePin yerine geçer.' });
  const L = []; for (let i = 0; i < 12; i++) { const a0 = i * 30 * D2R; L.push(...flatArc(0.312, 0.348, a0, a0 + 18 * D2R, 3, 0.006)); }
  for (let i = 0; i < 8; i++) { const a = i * 45 * D2R, w = 0.0125 / 0.38, w2 = 0.0125 / 0.47; L.push({ pts: [AP(0.38, 0.006, a - w), AP(0.47, 0.006, a - w2), AP(0.47, 0.006, a + w2), AP(0.38, 0.006, a + w)], out: [0, 1, 0] }); }
  m.add(polyGeom(L, 0), 'rp_markGhost', [0, 0, 0], [0, 0, 0], 'Body', NOSH);
  m.add(gDisc(0.312, 36), 'rp_markGhostFill', [0, 0.005, 0], [90, 0, 0], 'Body', NOSH);
  return m;
}
function quadMark(name, listName, mat, note) {
  const m = new Model(name, { family: FAM, budget: 'raid', listName, origin: ORIGIN, note });
  m.add(gPlane(0.98, 0.98), mat, [0, 0.004, 0], [90, 0, 0], 'Body', NOSH);
  return m;
}
export const HitQuad = () => quadMark('HitQuad', 'Asetat: kesin isabet', 'rp_acetateHit', 'Zemine yapışık asetat dörtgeni 0,98 × 0,98, UV 0–1 (u = +x doğu, v = +z kuzey); tarama ve kenar dokuda (tx_mark_hit). Belki-yalan sürümü aynı dörtgen, başka doku. Unlit, ZWrite kapalı, gölgesiz.');
export const MoveQuad = () => quadMark('MoveQuad', 'Asetat: hareket', 'rp_acetateMove', 'Zemine yapışık asetat dörtgeni 0,98 × 0,98, UV 0–1 (u = +x doğu, v = +z kuzey); tarama ve kenar dokuda (tx_mark_move). Unlit, ZWrite kapalı, gölgesiz.');

// ---------------------------------------------------------------- registry
export const MODELS = {
  Drone: { build: Drone }, MaintRobot: { build: MaintRobot }, Locker: { build: Locker }, Mortar: { build: Mortar },
  ShieldDrone: { build: ShieldDrone }, ArmorDrone: { build: ArmorDrone }, Bomber: { build: Bomber }, Replicator: { build: Replicator },
  Pawn_T: { build: () => Pawn('T', 'Teknisyen') }, Pawn_E: { build: () => Pawn('E', 'EMP uzmanı') }, Pawn_A: { build: () => Pawn('A', 'Analist') },
  Pawn_M: { build: () => Pawn('M', 'Madenci') }, Pawn_S: { build: () => Pawn('S', 'Sis uzmanı') },
  NodeCabinet: { build: NodeCabinet }, CoreCabinet: { build: CoreCabinet }, BackupCore: { build: BackupCore }, CopyCabinet: { build: CopyCabinet },
  LootCrate: { build: LootCrate }, LootCrateOpen: { build: LootCrateOpen }, CoolingUnit: { build: CoolingUnit },
  Tube: { build: Tube }, WeldArmBase: { build: WeldArmBase }, WeldArm: { build: WeldArm }, CeilingDebris: { build: CeilingDebris },
  WakePin: { build: WakePin }, GhostRing: { build: GhostRing }, HitQuad: { build: HitQuad }, MoveQuad: { build: MoveQuad },
};
