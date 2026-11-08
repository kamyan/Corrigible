// family: raidkit · Rev3 model library (see DESIGN_R3.md §3.8)
// Raid board units: 1 tile = 1 Unity unit; origin = tile centre on the board (y = 0); x east, z north.
// The raid camera is FIXED (orthographic, pitch 35°, yaw 45°, looking toward +x +z): only TOP, WEST (−x) and SOUTH (−z)
// faces are ever seen. The builder below (KB) therefore
//   · builds "camera boxes": top/west/south faces + the three camera-facing chamfers (13 tris instead of 44),
//   · drops every triangle that faces away from the raid camera (n·v < −0.03), except a few body back faces kept so the
//     ortho sheet views stay closed,
//   · assigns the kit's three face materials by normal ('F': up → wallTop, west → wallWest, south → wallSouth), so every
//     relief part gets the board's three-tone face shading; accents use detailA / detailB.
// Bridges are seen in two orientations (E as modelled, N = yaw −90°), so they are culled against both views; their face
// materials follow the rule −z → wallSouth (E gap), +z → wallWest (the +z side turns west in the N gap).
import * as THREE from 'three';
import { Model, metricUV, U, setUnityEuler, gBox, gLathe, gTube, D2R } from '../mlib.js';
import { defMat } from '../mats.js';

defMat('rk_void', { hex: '#34312C', rough: 1, metal: 0, note: 'oyuk içi: palet ağzı, tutamak deliği, kızak arası (ışık almayan derinlik); bütün kitlerde ortak' });
defMat('rk_dust', { hex: '#6B6257', rough: 1, metal: 0, alpha: 0.55, tex: 'tx_dust (yumuşak kenarlı toz lekesi, kenara doğru saydam, 256²)', note: 'gedik molozunun toz lekesi (zemine yapışık dörtgen, gölgesiz)' });
defMat('rk_galv', { hex: '#A3A7A6', rough: 0.6, metal: 0, note: 'galvaniz çelik (palet kirişi): mat, metalsiz; turuncu değil' });
defMat('rk_film', { hex: '#C9C6BC', rough: 0.32, metal: 0, note: 'streç film (yarı parlak, açık gri; koli istifinin ortasını saran ince kabuk)' });
defMat('rk_pick', { hex: '#D3DBD6', rough: 0.9, metal: 0, alpha: 0.25, note: 'kare seçim dörtgeni; Unity\'de MeshRenderer kapalı, yalnız MeshCollider' });

// ---------------------------------------------------------------- constants
const H = 0.47;          // half footprint (0.94)
const TOP = 0.555;       // flat top (contract 0.55–0.56)
const CB = 0.022;        // body chamfer (raid 0.02–0.03)
const VE = [-0.5792, 0.5736, -0.5792];   // toward the raid camera (Unity)
const VN = [-0.5792, 0.5736, 0.5792];    // same camera in the frame of a bridge turned −90° (N gap)
const ORIGIN = 'kare merkezi, tahta yüzeyi (y = 0)';
const ORIGIN_BR = 'kare merkezi (y = 0); köprü doğu boşluğunda (x 0,47–0,53), +x boyunca; kuzey için y ekseninde −90°';
const KITS = ['weu', 'eeu', 'me', 'af', 'na', 'ru', 'in', 'cn', 'jk', 'sea', 'sa', 'oc'];

const sub = (a, b) => [a[0] - b[0], a[1] - b[1], a[2] - b[2]];
const cross = (a, b) => [a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0]];
const dot = (a, b) => a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
const nrm = (a) => { const l = Math.hypot(a[0], a[1], a[2]) || 1; return [a[0] / l, a[1] / l, a[2] / l]; };
const rng = (seed) => () => { seed = (seed * 16807) % 2147483647; return (seed - 1) / 2147483646; };

// ---------------------------------------------------------------- kit builder (Unity coordinates in, merged meshes out)
class KB {
  constructor(m, id, o = {}) {
    this.m = m; this.id = id; this.T = []; this.views = o.views || [VE]; this.tol = o.tol ?? -0.03; this.h = o.h ?? H;
    const k = (s) => `k_${id}_${s}`; this.br = !!o.bridge;
    this.FK = o.faceKeys || { t: k('wallTop'), w: k('wallWest'), s: k('wallSouth'), a: k('detailA'), b: k('detailB') };
  }
  key(mat, n) {
    if (typeof mat === 'object') mat = n[1] > 0.5 ? mat.t : (Math.abs(n[0]) >= Math.abs(n[2]) ? mat.w : mat.s);
    // bridges: −z faces are the south side in the E gap, +z faces turn into the west side in the N gap (yaw −90°)
    const F = this.FK, side = this.br ? (n[2] < -0.5 ? F.s : F.w) : (Math.abs(n[0]) >= Math.abs(n[2]) ? F.w : F.s);
    switch (mat) {
      case 'F': return n[1] > 0.5 ? F.t : side;            // face tones
      case 'G': return n[1] > 0.5 ? F.w : side;            // side tones only (steel / galvanised parts)
      case 'T': return F.t; case 'W': return F.w; case 'S': return F.s;
      case 'A': return F.a; case 'B': return F.b; case 'V': return 'rk_void';
    }
    return mat;
  }
  // one triangle (Unity points), oriented by the outward hint n; vn = optional smooth normals per vertex
  tri(a, b, c, mat, n, o = {}, vn = null) {
    let g = cross(sub(b, a), sub(c, a)); const gl = Math.hypot(g[0], g[1], g[2]); if (gl < 1e-12) return;
    g = [g[0] / gl, g[1] / gl, g[2] / gl];
    if (dot(g, n) < 0) { const t = b; b = c; c = t; if (vn) vn = [vn[0], vn[2], vn[1]]; g = [-g[0], -g[1], -g[2]]; }
    this.T.push({ p: [a, b, c], n: g, vn, mat, cast: o.cast !== false, keep: !!o.keep });
  }
  poly(pts, mat, n, o = {}) { for (let i = 1; i < pts.length - 1; i++) this.tri(pts[0], pts[i], pts[i + 1], mat, n, o); }
  // camera box by bounds: top, west, south faces + camera-facing chamfers; o.back adds plain east/north caps (kept for sheets)
  box(x0, x1, y0, y1, z0, z1, c, mat, o = {}) {
    c = Math.max(0, Math.min(c || 0, (x1 - x0) * 0.45, (y1 - y0) * 0.45, (z1 - z0) * 0.45));
    if (!o.noTop) this.poly([[x0 + c, y1, z0 + c], [x1, y1, z0 + c], [x1, y1, z1], [x0 + c, y1, z1]], mat, [0, 1, 0], o);
    if (!o.noWest) this.poly([[x0, y0, z0 + c], [x0, y1 - c, z0 + c], [x0, y1 - c, z1], [x0, y0, z1]], mat, [-1, 0, 0], o);
    if (!o.noSouth) this.poly([[x0 + c, y0, z0], [x1, y0, z0], [x1, y1 - c, z0], [x0 + c, y1 - c, z0]], mat, [0, 0, -1], o);
    if (c > 0) {
      if (!o.noWest || !o.noTop) this.poly([[x0, y1 - c, z0 + c], [x0 + c, y1, z0 + c], [x0 + c, y1, z1], [x0, y1 - c, z1]], mat, [-1, 1, 0], o);
      if (!o.noSouth || !o.noTop) this.poly([[x0 + c, y1 - c, z0], [x1, y1 - c, z0], [x1, y1, z0 + c], [x0 + c, y1, z0 + c]], mat, [0, 1, -1], o);
      if (!o.noWest || !o.noSouth) this.poly([[x0, y0, z0 + c], [x0 + c, y0, z0], [x0 + c, y1 - c, z0], [x0, y1 - c, z0 + c]], mat, [-1, 0, -1], o);
      this.tri([x0 + c, y1, z0 + c], [x0, y1 - c, z0 + c], [x0 + c, y1 - c, z0], mat, [-1, 1, -1], o);
    }
    if (o.back) { const k = { ...o, keep: true };
      this.poly([[x1, y0, z0], [x1, y0, z1], [x1, y1, z1], [x1, y1, z0 + c], [x1, y1 - c, z0]], mat, [1, 0, 0], k);
      this.poly([[x0, y0, z1], [x1, y0, z1], [x1, y1, z1], [x0 + c, y1, z1], [x0, y1 - c, z1]], mat, [0, 0, 1], k); }
  }
  // bridge box: symmetric in z (top, south, north + two top chamfers); ends (±x) are buried in the blocks
  bbox(x0, x1, y0, y1, z0, z1, c, mat, o = {}) {
    c = Math.max(0, Math.min(c || 0, (y1 - y0) * 0.45, (z1 - z0) * 0.45));
    if (!o.noTop) this.poly([[x0, y1, z0 + c], [x1, y1, z0 + c], [x1, y1, z1 - c], [x0, y1, z1 - c]], mat, [0, 1, 0], o);
    this.poly([[x0, y0, z0], [x1, y0, z0], [x1, y1 - c, z0], [x0, y1 - c, z0]], mat, [0, 0, -1], o);
    this.poly([[x0, y0, z1], [x1, y0, z1], [x1, y1 - c, z1], [x0, y1 - c, z1]], mat, [0, 0, 1], o);
    if (c > 0) { this.poly([[x0, y1 - c, z0], [x1, y1 - c, z0], [x1, y1, z0 + c], [x0, y1, z0 + c]], mat, [0, 1, -1], o);
      this.poly([[x0, y1 - c, z1], [x1, y1 - c, z1], [x1, y1, z1 - c], [x0, y1, z1 - c]], mat, [0, 1, 1], o); }
    if (o.ends) { // −x / +x caps (only for parts whose ends sit in the open gap)
      const e = (x, s) => this.poly([[x, y0, z0], [x, y0, z1], [x, y1 - c, z1], [x, y1, z1 - c], [x, y1, z0 + c], [x, y1 - c, z0]], mat, [s, 0, 0], o);
      if (o.ends !== '+x') e(x0, -1); if (o.ends !== '-x') e(x1, 1); }
  }
  // face-local helpers: face 'W' (west, s runs north from the SW corner) or 'S' (south, s runs east); t = distance from
  // the centre along the face normal (t1 = outer surface)
  P(face, s, y, t) { const h = this.h; return face === 'W' ? [-t, y, -h + s] : [-h + s, y, -t]; }
  fbox(face, s0, s1, y0, y1, t0, t1, c, mat, o) { const h = this.h;
    if (face === 'W') this.box(-t1, -t0, y0, y1, -h + s0, -h + s1, c, mat, o); else this.box(-h + s0, -h + s1, y0, y1, -t1, -t0, c, mat, o); }
  both(fn) { fn('W'); fn('S'); }
  // n-gon prism: C front-cap centre, N outward axis, u/v in-plane axes, length L behind the cap; smooth sides
  ngon(C, N, u, v, r, L, seg, mat, o = {}) {
    const ph = o.phase ?? Math.PI / seg, F = [], B = [], D = [];
    for (let k = 0; k < seg; k++) { const a = ph + 2 * Math.PI * k / seg, ca = Math.cos(a), sa = Math.sin(a);
      const d = [u[0] * ca + v[0] * sa, u[1] * ca + v[1] * sa, u[2] * ca + v[2] * sa]; D.push(d);
      const f = [C[0] + r * d[0], C[1] + r * d[1], C[2] + r * d[2]]; F.push(f); B.push([f[0] - L * N[0], f[1] - L * N[1], f[2] - L * N[2]]); }
    if (o.cap !== false) this.poly(F, o.capMat || mat, N, o);
    if (o.backCap) this.poly(B, o.capMat || mat, [-N[0], -N[1], -N[2]], o);
    for (let k = 0; k < seg; k++) { const k2 = (k + 1) % seg, dm = nrm([D[k][0] + D[k2][0], D[k][1] + D[k2][1], D[k][2] + D[k2][2]]);
      const sm = o.sideMat || mat; if (o.flat) { this.poly([F[k], F[k2], B[k2], B[k]], sm, dm, o); continue; }
      this.tri(F[k], F[k2], B[k2], sm, dm, o, [D[k], D[k2], D[k2]]); this.tri(F[k], B[k2], B[k], sm, dm, o, [D[k], D[k2], D[k]]); }
  }
  // round part on a face (pipe end, knob): axis = face normal
  fround(face, s, y, r, t0, t1, seg, mat, o = {}) {
    const C = this.P(face, s, y, t1), N = face === 'W' ? [-1, 0, 0] : [0, 0, -1], u = face === 'W' ? [0, 0, 1] : [1, 0, 0];
    this.ngon(C, N, u, [0, 1, 0], r, t1 - t0, seg, mat, o);
  }
  // convex 2D outline (s, y) on a face, extruded from t0 to t1
  fprism(face, pts, t0, t1, mat, o = {}) {
    const F = pts.map(([s, y]) => this.P(face, s, y, t1)), B = pts.map(([s, y]) => this.P(face, s, y, t0));
    const N = face === 'W' ? [-1, 0, 0] : [0, 0, -1], ds = face === 'W' ? [0, 0, 1] : [1, 0, 0];
    this.poly(F, mat, N, o);
    const cs = pts.reduce((a, p) => a + p[0], 0) / pts.length, cy = pts.reduce((a, p) => a + p[1], 0) / pts.length;
    for (let i = 0; i < pts.length; i++) { const j = (i + 1) % pts.length; const es = pts[j][0] - pts[i][0], ey = pts[j][1] - pts[i][1];
      let ns = ey, ny = -es; if (ns * ((pts[i][0] + pts[j][0]) / 2 - cs) + ny * ((pts[i][1] + pts[j][1]) / 2 - cy) < 0) { ns = -ns; ny = -ny; }
      this.poly([F[i], F[j], B[j], B[i]], mat, [ds[0] * ns, ny, ds[2] * ns], o); }
  }
  // any three.js geometry (three space, centred), placed with Unity position / Euler like Model.add
  geom(g, mat, pos = [0, 0, 0], rot = [0, 0, 0], o = {}) {
    const ob = new THREE.Object3D(); ob.position.copy(U(pos[0], pos[1], pos[2])); setUnityEuler(ob, rot); if (o.scale) ob.scale.set(...o.scale);
    ob.updateMatrix(); const M = ob.matrix, NM = new THREE.Matrix3().getNormalMatrix(M);
    const gg = g.index ? g.toNonIndexed() : g, Pa = gg.attributes.position, Na = gg.attributes.normal;
    const v = new THREE.Vector3(), n = new THREE.Vector3();
    for (let i = 0; i < Pa.count; i += 3) {
      const p3 = [], pts = [], ns = [];
      for (let k = 0; k < 3; k++) { v.fromBufferAttribute(Pa, i + k).applyMatrix4(M); p3.push([v.x, v.y, v.z]); pts.push([v.x, v.y, -v.z]);
        n.fromBufferAttribute(Na, i + k).applyMatrix3(NM).normalize(); ns.push([n.x, n.y, -n.z]); }
      const g3 = cross(sub(p3[1], p3[0]), sub(p3[2], p3[0])); // CCW outward in three space
      this.tri(pts[0], pts[1], pts[2], mat, [g3[0], g3[1], -g3[2]], o, o.flat ? null : ns);
    }
  }
  // dust patch quad (UV 0–1) on the board
  dust(w, d, y = 0.003) { const x0 = -w / 2, x1 = w / 2, z0 = -d / 2, z1 = d / 2;
    this.T.push({ p: [[x0, y, z0], [x0, y, z1], [x1, y, z1]], n: [0, 1, 0], mat: 'rk_dust', cast: false, keep: true, uv: [[0, 0], [0, 1], [1, 1]] });
    this.T.push({ p: [[x0, y, z0], [x1, y, z1], [x1, y, z0]], n: [0, 1, 0], mat: 'rk_dust', cast: false, keep: true, uv: [[0, 0], [1, 1], [1, 0]] }); }
  finish(part = 'Body', o = {}) {
    const pv = this.m.parts[part].pivot, G = new Map();
    for (const t of this.T) {
      if (!t.keep && !this.views.some(v => dot(t.n, v) > this.tol)) continue;
      const key = this.key(t.mat, t.n), gk = key + (t.cast ? '' : '|nc') + (t.uv ? '|uv' : '');
      if (!G.has(gk)) G.set(gk, { key, cast: t.cast, uv: !!t.uv, pos: [], nor: [], uvs: [] });
      const g = G.get(gk), P = t.p.map(q => [q[0] - pv[0], q[1] - pv[1], -(q[2] - pv[2])]);
      const N = (t.vn || [t.n, t.n, t.n]).map(q => [q[0], q[1], -q[2]]);
      const gg = cross(sub(P[1], P[0]), sub(P[2], P[0])), order = dot(gg, [t.n[0], t.n[1], -t.n[2]]) >= 0 ? [0, 1, 2] : [0, 2, 1];
      for (const i of order) { g.pos.push(...P[i]); g.nor.push(...N[i]); if (t.uv) g.uvs.push(...t.uv[i]); }
    }
    for (const g of G.values()) {
      const geo = new THREE.BufferGeometry();
      geo.setAttribute('position', new THREE.Float32BufferAttribute(g.pos, 3)); geo.setAttribute('normal', new THREE.Float32BufferAttribute(g.nor, 3));
      if (g.uv) geo.setAttribute('uv', new THREE.Float32BufferAttribute(g.uvs, 2)); else if (o.uv) o.uv(geo); else metricUV(geo);
      this.m.add(geo, g.key, pv, [0, 0, 0], part, { cast: g.cast });
    }
    this.T = [];
  }
}

// ---------------------------------------------------------------- shared pieces
function mk(name, o) { return new Model(name, { family: 'raidkit', budget: 'raid', origin: ORIGIN, ...o }); }

// WallBlock: today's two-layer chipboard block (default kit), one mesh, 3 submeshes; side UV u 0–1 across the face, v = y/0.555
function WallBlock() {
  const m = mk('WallBlock', { listName: 'Duvar bloğu (iki katlı gövde, varsayılan kit)',
    note: 'Bütün kitlerin ortak gövde sözleşmesi: ayak izi 0,94, düz üst 0,555, iki kat (alt 0,94 / üst 0,92, arada 6 mm derz). Tek mesh, 3 alt mesh: üst / batı / güney (varsayılan yonga levha anahtarları; kitler malzemeyi değiştirir). Yan yüz UV: u yüz boyunca 0–1, v = y / 0,555 (Aşama A dokuları için). Doğu ve kuzey yüzleri sayfa görünüşleri için kapalı; alt yüz yok.' });
  const b = new KB(m, 'chip', { faceKeys: { t: 'r_chip_top', w: 'r_chip_west', s: 'r_chip_south' } });
  b.box(-0.47, 0.47, 0, 0.274, -0.47, 0.47, 0.012, 'F', { back: true });
  b.box(-0.46, 0.46, 0.280, TOP, -0.46, 0.46, CB, 'F', { back: true });
  b.finish('Body', { uv: (geo) => { // face-wise 0–1 mapping
    const P = geo.attributes.position.array, N = geo.attributes.normal.array, uv = new Float32Array(P.length / 3 * 2);
    for (let i = 0; i < P.length / 3; i++) { const x = P[i * 3], y = P[i * 3 + 1], z = -P[i * 3 + 2], nx = N[i * 3], ny = N[i * 3 + 1], nz = -N[i * 3 + 2];
      let u, v; if (ny > 0.5) { u = (x + 0.47) / 0.94; v = (z + 0.47) / 0.94; } else if (Math.abs(nx) >= Math.abs(nz)) { u = (nx < 0 ? (z + 0.47) : (0.47 - z)) / 0.94; v = y / TOP; } else { u = (nz < 0 ? (x + 0.47) : (0.47 - x)) / 0.94; v = y / TOP; }
      uv[i * 2] = u; uv[i * 2 + 1] = v; }
    geo.setAttribute('uv', new THREE.BufferAttribute(uv, 2)); } });
  return m;
}

function BoardBase() {
  const m = new Model('BoardBase', { family: 'raidkit', budget: 'raid', listName: 'Tahta tabanı (kontrplak + ozalit pafta)',
    origin: 'tahta ortası, kontrplak üst yüzü (y = 0); ızgaranın (0,0) köşesi (−4, 0, 4)',
    note: 'Kontrplak 8,6 × 0,25 × 8,6, pahlı (kenar katları tx_plywood). Ozalit pafta 8,5 × 8,5 tek dörtgen, UV 0–1 (u doğuya, v kuzeye; tx_ozalit 2048², ızgara ortadaki 8 × 8), kontrplaktan 1 mm yukarıda. Alt yüz yok.' });
  m.meta.place = [{ pos: [4, 0, -4], yaw: 0, note: 'ızgara (0,0) köşesi dünya orijininde: x = sütun, z = −satır' }];
  const b = new KB(m, 'ply', { faceKeys: { t: 'r_plyTop', w: 'r_plyEdge', s: 'r_plyEdge' } });
  b.box(-4.3, 4.3, -0.25, 0, -4.3, 4.3, 0.03, 'F', { back: true });
  b.finish();
  const q = new THREE.BufferGeometry(), s = 4.25, y = 0.001; // paper quad, three space (z flipped), CCW up
  q.setAttribute('position', new THREE.Float32BufferAttribute([-s, y, s, s, y, s, s, y, -s, -s, y, s, s, y, -s, -s, y, -s], 3));
  q.setAttribute('normal', new THREE.Float32BufferAttribute([0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0], 3));
  q.setAttribute('uv', new THREE.Float32BufferAttribute([0, 0, 1, 0, 1, 1, 0, 0, 1, 1, 0, 1], 2));
  m.add(q, 'r_ozalit', [0, 0, 0], [0, 0, 0], 'Body', { cast: false });
  return m;
}

function Tile() {
  const m = mk('Tile', { listName: 'Kare (seçim dörtgeni)', note: '1 × 1 dörtgen, yüzü yukarı, kâğıdın 2 mm üstünde; UV 0–1 (u doğuya, v kuzeye). Fare seçimi ve işaret yerleşimi için; görünmez (renderer kapalı).' });
  const q = new THREE.BufferGeometry(), s = 0.5, y = 0.002;
  q.setAttribute('position', new THREE.Float32BufferAttribute([-s, y, s, s, y, s, s, y, -s, -s, y, s, s, y, -s, -s, y, -s], 3));
  q.setAttribute('normal', new THREE.Float32BufferAttribute([0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0], 3));
  q.setAttribute('uv', new THREE.Float32BufferAttribute([0, 0, 1, 0, 1, 1, 0, 0, 1, 1, 0, 1], 2));
  m.add(q, 'rk_pick', [0, 0, 0], [0, 0, 0], 'Body', { cast: false });
  return m;
}

// ---------------------------------------------------------------- kit definitions (filled below)
const KIT = {};
function relief(id) {
  const K = KIT[id];
  const m = mk(`Relief_${id}`, { listName: `Duvar barikatı · ${K.bina} (${K.duvar})`, note: `${K.relNote} ${COMMON_REL}` });
  const b = new KB(m, id); K.wall(b); b.finish(); return m;
}
const COMMON_REL = 'Gövde dahil: iki katlı gövde ve kabartma tek mesh (kare başına 1 renderer + en çok 2 köprü = 3). Alt mesh: kitin üst / batı / güney yüz malzemesi (yüz normaline göre) + detailA / detailB. Ayak izi 0,94 içinde, düz üst 0,55–0,56. Sabit kamerada görünmeyen üçgenler (doğu, kuzey, alt) silindi; gölge için Cast Shadows = Two Sided.';

// ---------------------------------------------------------------- small layout helpers
function posts(b, w, y0, y1, c, mat, h = b.h, o = {}) { // corner posts at SW, NW, SE (NE never seen)
  b.box(-h, -h + w, y0, y1, -h, -h + w, c, mat, o); b.box(-h, -h + w, y0, y1, h - w, h, c, mat, o); b.box(h - w, h, y0, y1, -h, -h + w, c, mat, o);
}
function spread(s0, s1, rel, gap) { const tot = rel.reduce((a, b) => a + b, 0), L = s1 - s0 - gap * (rel.length - 1); const out = []; let s = s0;
  for (const r of rel) { const w = L * r / tot; out.push([s, s + w]); s += w + gap; } return out; }
function runs(s0, s1, L, gap) { const out = []; let s = s0; while (s < s1 - 0.06) { const e = Math.min(s + L, s1); out.push([s, e]); s = e + gap; } return out; }
// flat quad on a face plane at distance t (labels, hand holes): one quad, no sides
function fquad(b, face, s0, s1, y0, y1, t, mat, o = {}) {
  b.poly([b.P(face, s0, y0, t), b.P(face, s1, y0, t), b.P(face, s1, y1, t), b.P(face, s0, y1, t)], mat, face === 'W' ? [-1, 0, 0] : [0, 0, -1], { cast: false, ...o });
}
// n×n grid of boxes (cartons, crates); only the west column / south row unless `all`
function grid(b, n, h, y0, y1, g, c, mat, o = {}) {
  const w = 2 * h / n;
  for (let ix = 0; ix < n; ix++) for (let iz = 0; iz < n; iz++) {
    if (!o.all && ix > 0 && iz > 0) continue;
    const x0 = -h + ix * w + (ix ? g / 2 : 0), x1 = -h + (ix + 1) * w - (ix < n - 1 ? g / 2 : 0);
    const z0 = -h + iz * w + (iz ? g / 2 : 0), z1 = -h + (iz + 1) * w - (iz < n - 1 ? g / 2 : 0);
    b.box(x0, x1, y0, y1, z0, z1, c, typeof mat === 'function' ? mat(ix, iz) : mat, { noTop: !o.all });
  }
}

// ---------------------------------------------------------------- the 12 kits: wall relief
KIT.weu = { bina: 'Banka veri merkezi', duvar: 'sökülmüş rack kabinleri',
  relNote: 'Her yüzde üç dar rack kabini: çerçeve içinde kapı, dikey kol çubuğu (detailB), üstte koyu delikli havalandırma bandı (delikler doku), koyu süpürgelik; üst yüzde ortadan x boyunca merdiven kablo tavası (köprüyle komşuya devam eder). Gri tonda: uzun dikey paneller + dikey kollar. İki kat: süpürgelik + kabin gövdesi.',
  wall(b) {
    b.box(-0.45, 0.45, 0, 0.045, -0.45, 0.45, 0.008, 'A', { noTop: true, back: true });            // plinth
    b.box(-0.435, 0.435, 0.045, 0.52, -0.435, 0.435, 0, 'F', { noTop: true, back: true });        // carcass (seen in the door seams)
    b.box(-0.47, 0.47, 0.515, TOP, -0.47, -0.11, CB, 'F', { back: true });                        // top plate, south of the tray
    b.box(-0.47, 0.47, 0.515, TOP, 0.11, 0.47, CB, 'F', { back: true });                          // top plate, north of the tray
    b.box(-0.47, 0.47, 0.49, 0.522, -0.11, 0.11, 0, 'A');                                          // tray floor
    b.box(-0.47, 0.47, 0.522, TOP, -0.11, -0.082, 0, 'B'); b.box(-0.47, 0.47, 0.522, TOP, 0.082, 0.11, 0, 'B'); // rails
    for (let x = -0.41; x < 0.45; x += 0.12) b.box(x, x + 0.03, 0.522, 0.545, -0.082, 0.082, 0, 'B', { cast: false }); // rungs
    b.both(f => { for (const [s0, s1] of spread(0.03, 0.91, [1, 1, 1], 0.03)) {
      b.fbox(f, s0, s1, 0.065, 0.50, 0.435, 0.455, 0.012, 'F');                                    // door
      b.fbox(f, s0 + 0.035, s1 - 0.035, 0.40, 0.465, 0.455, 0.461, 0, 'A', { cast: false });       // perforated band
      b.fbox(f, s1 - 0.07, s1 - 0.04, 0.16, 0.37, 0.455, 0.47, 0.008, 'B', { cast: false });       // handle bar
    } });
  } };

KIT.eeu = { bina: 'Devlet arşivi', duvar: 'çelik arşiv rafı',
  relNote: 'Açık çelik raf: dört köşe dikmesi, üç raf dudağı (alt, orta, üst), aralarda 2–3 genişlikte kurşuni (detailA) ve krem (detailB) dosya kutuları, raf dudağından geride. Gri tonda: yatay raflar + düzensiz kutu ızgarası. İki kat: alt ve üst raf gözü.',
  wall(b) {
    b.box(-0.40, 0.40, 0.055, 0.515, -0.40, 0.40, 0, 'F', { noTop: true, back: true });           // back of the bays
    b.box(-0.462, 0.462, 0, 0.055, -0.462, 0.462, 0.01, 'F', { back: true });                      // bottom shelf
    b.box(-0.462, 0.462, 0.262, 0.302, -0.462, 0.462, 0.01, 'F');                                   // middle shelf lip
    b.box(-0.462, 0.462, 0.512, 0.53, -0.462, 0.462, 0.008, 'F', { back: true });                  // top shelf plate
    b.box(-0.462, -0.432, 0.53, 0.555, -0.462, 0.462, 0.008, 'F'); b.box(-0.432, 0.462, 0.53, 0.555, -0.462, -0.432, 0.008, 'F'); // rim W, S
    b.box(-0.432, 0.462, 0.53, 0.555, 0.432, 0.462, 0.008, 'F', { back: true }); b.box(0.432, 0.462, 0.53, 0.555, -0.432, 0.432, 0.008, 'F', { back: true }); // N, E
    [[[-0.428, -0.012], [2, 3, 2.2, 2.6], 'TBTB'], [[0.012, 0.428], [3, 2, 2.6, 2.2], 'BTTB']].forEach(([[z0, z1], rel, mm]) =>
      spread(-0.428, 0.428, rel, 0.014).forEach(([x0, x1], j) => b.poly([[x0, 0.547, z0], [x1, 0.547, z0], [x1, 0.547, z1], [x0, 0.547, z1]], mm[j], [0, 1, 0], { cast: false }))); // top-bay box lids
    posts(b, 0.045, 0, 0.556, 0.008, { t: 'A', w: 'F', s: 'F' }); b.box(0.425, 0.47, 0.5, 0.556, 0.425, 0.47, 0.008, { t: 'A', w: 'F', s: 'F' }, { back: true });
    const bays = [[0.055, 0.262], [0.302, 0.512]];
    const lay = { W: [[2, 3, 2, 2.4], [3, 2.2, 3]], S: [[2.4, 2, 3, 2], [2.2, 3, 3]] };
    const mt = { W: [['A', 'B', 'A', 'A'], ['B', 'A', 'B']], S: [['B', 'A', 'B', 'A'], ['A', 'A', 'B']] };
    b.both(f => bays.forEach(([y0, y1], i) => spread(0.065, 0.875, lay[f][i], 0.014).forEach(([s0, s1], j) =>
      b.fbox(f, s0, s1, y0, y1 - 0.016, 0.40, 0.432, 0.012, mt[f][i][j]))));
  } };

KIT.me = { bina: 'Körfez rafinerisi', duvar: 'kutu sehpa üstünde boru demeti',
  relNote: 'Alüminyum kaplı boru demeti kutu sehpa üstünde: batı ve güney yüzünde iki sıra × üç boru ağzı (sekizgen, yumuşak normal; ağız içi detailA), üstte iki kelepçe (detailB), altta koyu sehpa kirişi ve üç ayak. Gri tonda: iri yuvarlaklar. İki kat: sehpa + demet.',
  wall(b) {
    b.box(-0.44, 0.44, 0, 0.10, -0.44, 0.44, 0, 'V', { noTop: true, back: true });                // shadow under the trestle
    b.box(-0.47, 0.47, 0.10, 0.14, -0.47, 0.47, 0.012, 'A', { back: true });                       // trestle beam
    posts(b, 0.08, 0, 0.10, 0.01, 'A');                                                            // trestle legs
    b.box(-0.43, 0.43, 0.14, 0.55, -0.43, 0.43, CB, 'F', { back: true });                          // bundle (cladding)
    for (const x of [-0.22, 0.18]) b.box(x, x + 0.045, 0.55, 0.557, -0.43, 0.43, 0, 'B', { cast: false }); // clamps
    b.both(f => { for (const y of [0.245, 0.445]) for (const s of [0.165, 0.47, 0.775]) {
      b.fround(f, s, y, 0.1, 0.40, 0.468, 8, 'F');                                                 // pipe end
      b.ngon(b.P(f, s, y, 0.4695), f === 'W' ? [-1, 0, 0] : [0, 0, -1], f === 'W' ? [0, 0, 1] : [1, 0, 0], [0, 1, 0], 0.055, 0, 8, 'A', { cast: false, sideMat: null, flat: true, noSide: true });
    } });
  } };

KIT.af = { bina: 'Cevher hangarı', duvar: 'oluklu galvaniz sac bunker',
  relNote: 'Oluklu galvaniz sac bunker: her yüzde 12 ince dikey oluk, açık renk köşe köşebentleri (detailB), üst kenarda sac ağız; içinde ağzın altında basık cevher yığını (detailA, en çok 0,546). Gri tonda: ince sık dikey çizgiler + içi oyuk üst. İki kat: kızak + bunker.',
  wall(b) {
    b.box(-0.44, 0.44, 0, 0.045, -0.44, 0.44, 0.006, 'S', { noTop: true, back: true });           // skid
    b.box(-0.45, 0.45, 0.045, 0.47, -0.45, 0.45, 0, { t: 'A', w: 'G', s: 'G' }, { back: true });  // bunker walls; top = ore floor
    b.box(-0.45, -0.41, 0.47, TOP, -0.45, 0.45, 0.01, 'G');                                        // mouth W
    b.box(-0.41, 0.45, 0.47, TOP, -0.45, -0.41, 0.01, 'G');                                        // mouth S
    b.box(-0.41, 0.45, 0.47, TOP, 0.41, 0.45, 0.01, 'G', { back: true });                          // mouth N (inner face seen)
    b.box(0.41, 0.45, 0.47, TOP, -0.41, 0.41, 0.01, 'G', { back: true });                          // mouth E (inner face seen)
    b.both(f => { for (let k = 0; k < 12; k++) { const s = 0.083 + k * 0.0705; b.fbox(f, s, s + 0.026, 0.06, 0.535, 0.45, 0.47, 0, 'G', { cast: false }); } });
    posts(b, 0.06, 0.045, TOP, 0.01, 'B');
    b.geom(gLathe([[0.40, 0.468], [0.32, 0.50], [0.2, 0.528], [0.08, 0.543], [0, 0.546]], 7, { phiStart: 0.4 }), 'A', [0.01, 0, 0.02], [0, 17, 0], { scale: [1, 1, 0.92] });
  } };

KIT.na = { bina: 'Dağıtım merkezi', duvar: 'palet üstünde streç filmli koli yükü',
  relNote: 'Altta ahşap palet: üst tahtalar, yüz başına üç takoz, aralarında koyu forklift ağızları (rk_void); üstte üç kat koli (2×2, 3×3 şaşırtmalı derzler) ve filmle kapanmış yumuşak kenarlı üst kat; ortayı saran açık renk, yarı parlak streç film bandı (rk_film). Gri tonda: çıtalı koyu taban + açık yatay bant + sade yumuşak üst. İki kat: palet + yük.',
  wall(b) {
    b.box(-0.37, 0.37, 0, 0.075, -0.37, 0.37, 0, 'V', { noTop: true, back: true });               // fork openings
    b.box(-0.47, 0.47, 0.075, 0.11, -0.47, 0.47, 0.008, 'A', { back: true });                      // deck boards
    for (const [s0, s1] of [[0, 0.13], [0.405, 0.535], [0.81, 0.94]]) {
      b.fbox('W', s0, s1, 0, 0.075, 0.33, 0.47, 0.008, 'A'); if (s0 > 0) b.fbox('S', s0, s1, 0, 0.075, 0.33, 0.47, 0.008, 'A'); }
    grid(b, 2, 0.457, 0.11, 0.258, 0.01, 0.012, 'F');
    grid(b, 3, 0.457, 0.258, 0.405, 0.01, 0.012, 'F');
    b.box(-0.457, 0.457, 0.405, 0.55, -0.457, 0.457, 0.035, 'F', { back: true });                   // film-capped top layer
    b.box(-0.463, 0.463, 0.30, 0.47, -0.463, 0.463, 0.012, 'rk_film', { noTop: true });             // stretch-film band
  } };

KIT.ru = { bina: 'Sibirya veri merkezi', duvar: 'sandviç panel kabin',
  relNote: 'Sandviç panel kabin: her yüzde üç geniş düz panel, iki dikey derz çıtası ve köşe çıtaları; üst kenarlarda açık renk kırağı dudağı (detailB), üstte nervürlü çatı şeridi (x boyunca 5 nervür). Gri tonda: geniş sade paneller + parlak üst kenar + çizgili üst. İki kat: taban profili + panel.',
  wall(b) {
    b.box(-0.45, 0.45, 0, 0.04, -0.45, 0.45, 0.008, 'A', { noTop: true, back: true });            // base channel
    b.box(-0.445, 0.445, 0.04, 0.52, -0.445, 0.445, 0, 'F', { back: true });                       // panels; top = roof
    b.box(-0.47, -0.43, 0.515, TOP, -0.47, 0.47, 0.018, 'B', { back: true });                      // frost lip W
    b.box(-0.43, 0.47, 0.515, TOP, -0.47, -0.43, 0.018, 'B', { back: true });                      // frost lip S
    b.box(-0.43, 0.47, 0.515, TOP, 0.43, 0.47, 0.018, 'B', { back: true });                        // frost lip N
    b.box(0.43, 0.47, 0.515, TOP, -0.43, 0.43, 0.018, 'B', { back: true });                        // frost lip E
    for (const z of [-0.29, -0.145, 0, 0.145, 0.29]) b.box(-0.43, 0.43, 0.52, 0.55, z - 0.026, z + 0.026, 0.008, 'F'); // roof ribs
    b.both(f => { for (const s of [0.315, 0.625]) b.fbox(f, s - 0.022, s + 0.022, 0.04, 0.515, 0.445, 0.47, 0.008, 'F'); });
    posts(b, 0.05, 0.04, 0.515, 0.01, 'F');
  } };

KIT.in = { bina: 'Çağrı merkezi', duvar: 'bölme paneli ve dosya dolabı istifi',
  relNote: 'Alt kat: çekmeceli dosya dolapları (yüz başına iki dolap × iki çekmece, açık renk kulp). Üst kat: ofis bölme panelleri — açık alüminyum çerçeve (detailB) içinde koyu kumaş (detailA). Gri tonda: çerçeve ızgarası + çekmece çizgileri. İki kat: dolaplar + paneller.',
  wall(b) {
    b.box(-0.445, 0.445, 0, 0.27, -0.445, 0.445, 0, 'F', { noTop: true, back: true });            // cabinet carcass
    b.both(f => { for (const [s0, s1] of [[0.02, 0.46], [0.48, 0.92]]) for (const [y0, y1] of [[0.02, 0.135], [0.145, 0.26]]) {
      b.fbox(f, s0, s1, y0, y1, 0.445, 0.461, 0.01, 'F');                                          // drawer front
      const sm = (s0 + s1) / 2; b.fbox(f, sm - 0.07, sm + 0.07, y1 - 0.048, y1 - 0.022, 0.461, 0.47, 0, 'B', { cast: false }); // pull
    } });
    b.box(-0.44, 0.44, 0.27, 0.525, -0.44, 0.44, 0, 'A', { noTop: true, back: true });            // fabric
    b.box(-0.47, 0.47, 0.27, 0.30, -0.47, 0.47, 0.01, 'B');                                         // bottom rail
    b.box(-0.44, 0.44, 0.52, 0.55, -0.44, 0.44, 0, 'T');                                           // top panel
    b.box(-0.47, -0.44, 0.525, 0.556, -0.47, 0.47, 0.012, 'B'); b.box(-0.44, 0.47, 0.525, 0.556, -0.47, -0.44, 0.012, 'B'); // top rails W, S
    b.box(-0.44, 0.47, 0.525, 0.556, 0.44, 0.47, 0.012, 'B', { back: true }); b.box(0.44, 0.47, 0.525, 0.556, -0.44, 0.44, 0.012, 'B', { back: true }); // N, E
    b.box(-0.44, 0.44, 0.55, 0.556, -0.0125, 0.0125, 0, 'B', { cast: false }); b.box(-0.0125, 0.0125, 0.55, 0.556, -0.44, 0.44, 0, 'B', { cast: false }); // cross
    posts(b, 0.035, 0.30, 0.525, 0.008, 'B');
    b.both(f => { b.fbox(f, 0.455, 0.485, 0.30, 0.525, 0.44, 0.466, 0.006, 'B'); });              // mullion
  } };

KIT.cn = { bina: 'Montaj fabrikası', duvar: 'istiflenmiş parça kasaları',
  relNote: 'İki kat plastik parça kasası (yüz başına 3 × 2): her kasada koyu tutamak yuvası (rk_void) ve açık renk etiket cebi (detailB); üst kattaki kasalar açık, ağızlarının altında koyu taban ve küçük parçalar (somun, pul). Gri tonda: sık küçük kutular + koyu nokta dizisi. İki kat: iki kasa sırası.',
  wall(b) {
    const n = 3, h = 0.466, w = 2 * h / n, g = 0.012;
    b.box(-0.40, 0.40, 0, 0.49, -0.40, 0.40, 0, { t: 'A', w: 'F', s: 'F' }, { back: true });       // filling; top = crate floors
    grid(b, n, h, 0, 0.272, g, 0.014, 'F'); grid(b, n, h, 0.282, TOP, g, 0.014, 'F');
    b.both(f => { for (let i = 0; i < n; i++) for (const [y0, y1] of [[0, 0.272], [0.282, TOP]]) {
      const s0 = i * w + (i ? g / 2 : 0), s1 = (i + 1) * w - (i < n - 1 ? g / 2 : 0), sm = (s0 + s1) / 2;
      fquad(b, f, sm - 0.065, sm + 0.065, y1 - 0.078, y1 - 0.045, 0.469, 'V');                      // hand hole
      fquad(b, f, sm - 0.055, sm + 0.055, y0 + 0.05, y0 + 0.105, 0.469, 'B');                       // label pocket
    } });
    const tw = 0.026, cc = 0.014;                                                                   // open tops: crate walls
    for (let k = 0; k <= n; k++) { const c = Math.min(-h + k * w, h - tw), outer = k === 0;
      b.box(-h + cc, h, 0.49, TOP, outer ? -h + cc : c, outer ? -h + tw : c + tw, 0, 'F', { noWest: true, noSouth: outer, cast: false }); // along x
      b.box(outer ? -h + cc : c, outer ? -h + tw : c + tw, 0.49, TOP, -h + cc, h, 0, 'F', { noSouth: true, noWest: outer, cast: false }); } // along z
    const R = rng(7); for (let ix = 0; ix < n; ix++) for (let iz = 0; iz < n; iz++) { if ((ix + iz) % 2) continue;
      const x = -h + (ix + 0.5) * w + (R() - 0.5) * 0.1, z = -h + (iz + 0.5) * w + (R() - 0.5) * 0.1;
      b.box(x - 0.04, x + 0.03, 0.49, 0.525, z - 0.03, z + 0.04, 0.006, 'B', { cast: false }); }
  } };

KIT.jk = { bina: 'Çip fabrikası', duvar: 'sökülmüş pano gövdeleri',
  relNote: 'Sökülmüş şalt pano gövdeleri: her yüzde iki geniş kapı, kapı çizgileri, kapı başına beş yatay kabarık panjur çıtası ve yuvarlak kilit topuzu (detailB); üstte taşan çatı sacı, altta koyu taban profili. Gri tonda: yatay panjur taraması. İki kat: taban profili + pano.',
  wall(b) {
    b.box(-0.44, 0.44, 0, 0.05, -0.44, 0.44, 0.008, 'A', { noTop: true, back: true });            // base channel
    b.box(-0.44, 0.44, 0.05, 0.52, -0.44, 0.44, 0, 'F', { noTop: true, back: true });             // carcass
    b.box(-0.47, 0.47, 0.515, 0.55, -0.47, 0.47, CB, 'F', { back: true });                         // roof sheet
    for (const [x, z] of [[-0.2, 0.17], [0.2, -0.17]]) { b.box(x - 0.16, x + 0.16, 0.55, 0.556, z - 0.1, z + 0.1, 0, 'B', { cast: false });
      for (const dx of [-0.09, 0, 0.09]) b.ngon([x + dx, 0.5565, z], [0, 1, 0], [1, 0, 0], [0, 0, 1], 0.03, 0, 8, 'V', { cast: false }); } // glands
    b.both(f => { for (const [s0, s1] of spread(0.02, 0.92, [1, 1], 0.03)) {
      b.fbox(f, s0, s1, 0.065, 0.50, 0.44, 0.455, 0.008, 'F');                                     // door
      for (let k = 0; k < 5; k++) { const y = 0.095 + k * 0.045; b.fbox(f, s0 + 0.05, s1 - 0.05, y, y + 0.026, 0.455, 0.47, 0, 'F', { cast: false }); }
      b.fround(f, s1 - 0.06, 0.40, 0.024, 0.455, 0.47, 8, 'B', { cast: false });                   // lock knob
    } });
  } };

KIT.sea = { bina: 'Elektronik fabrikası', duvar: 'kum torbası barikatı',
  relNote: 'Dört sıra kum torbası (yuvarlak kenarlı yastıklar, köşede sıra sıra şaşırtmalı bağ); en alt sıra koyu ıslak bant (detailA); üst sıra 2 × 3 torbayla üstü kaplar. Gri tonda: yuvarlak yatay sıralar + koyu alt bant. İki kat: alt iki sıra + üst iki sıra.',
  wall(b) {
    const hr = 0.1375, L = 0.30, D = 0.15, c = 0.042;
    b.box(-0.33, 0.33, 0, 3 * hr, -0.33, 0.33, 0, 'A', { noTop: true, back: true });              // core
    for (let r = 0; r < 3; r++) {
      const y0 = r * hr, y1 = y0 + hr, mat = r === 0 ? 'A' : 'F', wFirst = r % 2 === 0;
      for (const [s0, s1] of runs(wFirst ? 0 : D + 0.006, 0.94, L, 0.006)) b.fbox('W', s0, s1, y0, y1, 0.47 - D, 0.47, c, mat, { noTop: true });
      for (const [s0, s1] of runs(wFirst ? D + 0.006 : 0, 0.94, L, 0.006)) b.fbox('S', s0, s1, y0, y1, 0.47 - D, 0.47, c, mat, { noTop: true });
    }
    for (let ix = 0; ix < 2; ix++) for (let iz = 0; iz < 3; iz++) {
      const x0 = -0.47 + ix * 0.47 + (ix ? 0.003 : 0), x1 = -0.47 + (ix + 1) * 0.47 - (ix ? 0 : 0.003);
      const z0 = -0.47 + iz * 0.3133 + (iz ? 0.003 : 0), z1 = -0.47 + (iz + 1) * 0.3133 - (iz < 2 ? 0.003 : 0);
      b.box(x0, x1, 3 * hr, 0.55, z0, z1, c, 'F', { back: ix === 1 || iz === 2 });
    }
  } };

KIT.sa = { bina: 'Hidroelektrik santrali', duvar: 'takozlar üstünde çelik levha ve kalıp istifi',
  relNote: 'Altta iki ahşap takoz (detailA) üstünde üç çelik levha (kenarları kaçık); üstte çelik kalıp panosu: sık nervür ızgarası (yüz başına 4 × 3 göz). Gri tonda: kare ızgara + ince yatay levha çizgileri. İki kat: levha istifi + kalıp.',
  wall(b) {
    b.box(-0.36, 0.36, 0, 0.07, -0.36, 0.36, 0, 'V', { noTop: true, back: true });                // gap between the timbers
    b.both(f => { for (const [s0, s1] of [[0.09, 0.25], [0.66, 0.82]]) b.fbox(f, s0, s1, 0, 0.07, 0.30, 0.44, 0.008, 'A'); });
    b.box(-0.47, 0.47, 0.07, 0.095, -0.47, 0.47, 0.006, 'G', { back: true });                     // plates
    b.box(-0.458, 0.466, 0.095, 0.12, -0.462, 0.458, 0.006, 'G');
    b.box(-0.47, 0.46, 0.12, 0.145, -0.466, 0.47, 0.006, 'G');
    b.box(-0.445, 0.445, 0.145, 0.53, -0.445, 0.445, 0, 'F', { noTop: true, back: true });        // panel skin
    b.box(-0.47, 0.47, 0.145, 0.172, -0.47, 0.47, 0.008, 'F');                                     // bottom rib
    for (const y of [0.268, 0.398]) b.box(-0.47, 0.47, y, y + 0.026, -0.47, 0.47, 0.008, 'F');    // horizontal ribs
    b.box(-0.442, 0.442, 0.50, 0.53, -0.442, 0.442, 0, 'F', { noWest: true, noSouth: true });      // top skin (formwork lying rib-up)
    for (let k = 0; k <= 4; k++) { const c = -0.47 + k * 0.235, a = Math.min(c, 0.442), e = k === 0 ? -0.442 : Math.min(c + 0.028, 0.47), cc = k === 0 || k === 4 ? 0.008 : 0;
      b.box(-0.47, 0.47, k === 0 ? 0.50 : 0.53, TOP, a === c ? a : 0.442, e, cc, 'F', { noWest: k > 0 && k < 4, back: k === 4 });   // ribs along x
      b.box(a === c ? a : 0.442, e, k === 0 ? 0.50 : 0.53, TOP, -0.47, 0.47, cc, 'F', { noSouth: k > 0 && k < 4, back: k === 4 }); } // ribs along z
    posts(b, 0.04, 0.145, 0.50, 0.008, 'F');
    b.both(f => { for (const s of [0.245, 0.47, 0.695]) b.fbox(f, s - 0.014, s + 0.014, 0.172, 0.525, 0.445, 0.47, 0.006, 'F'); });
  } };

KIT.oc = { bina: 'Maden limanı', duvar: 'baklava sac kızak üstünde yedek parça sandığı',
  relNote: 'Baklava sac kızak (iki kızak ayağı, aralarında koyu boşluk; baklava deseni doku) üstünde yedek parça sandığı: derin çıta çerçeve (tahtalar 5 cm geride), her yüzde çapraz destek çıtası (köşede V oluşturur), alt ve üst köşelerde koyu çelik köşe sacı (detailA), kapakta iki çıta; kızıl toz dokuda. Gri tonda: çapraz çıtalar + kızak. İki kat: kızak + sandık.',
  wall(b) {
    b.box(-0.44, 0.44, 0, 0.08, -0.31, 0.31, 0, 'V', { noTop: true });                             // between the runners
    b.box(-0.46, 0.46, 0, 0.08, -0.46, -0.31, 0.008, 'A');                                          // runner S
    b.box(-0.46, 0.46, 0, 0.08, 0.31, 0.46, 0.008, 'A', { back: true });                            // runner N
    b.box(-0.47, 0.47, 0.08, 0.105, -0.47, 0.47, 0.006, 'A', { back: true });                      // diamond-plate deck
    b.box(-0.42, 0.42, 0.105, 0.51, -0.42, 0.42, 0, 'F', { noTop: true, back: true });            // boards (deep inside the frame)
    b.box(-0.466, 0.466, 0.105, 0.16, -0.466, 0.466, 0.012, 'F');                                  // bottom batten frame
    b.box(-0.466, 0.466, 0.50, 0.55, -0.466, 0.466, CB, 'F', { back: true });                     // top battens + lid
    for (const x of [-0.2, 0.17]) b.box(x, x + 0.075, 0.55, 0.556, -0.40, 0.40, 0, 'F', { cast: false }); // lid battens
    posts(b, 0.055, 0.16, 0.50, 0.01, 'F', 0.466);
    b.both(f => b.fprism(f, [[0.06, 0.16], [0.16, 0.16], [0.875, 0.43], [0.875, 0.50], [0.775, 0.50], [0.06, 0.23]], 0.42, 0.458, 'F'));
    for (const [x0, z0] of [[-0.47, -0.47], [-0.47, 0.38], [0.38, -0.47]]) { b.box(x0, x0 + 0.09, 0.47, 0.556, z0, z0 + 0.09, 0.01, 'A', { cast: false });
      b.box(x0, x0 + 0.09, 0.105, 0.19, z0, z0 + 0.09, 0.01, 'A', { cast: false }); }            // steel corner pieces, top and bottom
  } };

// ---------------------------------------------------------------- shared bits for columns, bridges, rubble
function weuTray(b, x0, x1, hw = 0.11) { // ladder tray recessed into a top plate (rails flush with the top)
  b.box(x0, x1, 0.49, 0.522, -hw, hw, 0, 'A');
  b.box(x0, x1, 0.522, TOP, -hw, -hw + 0.028, 0, 'B'); b.box(x0, x1, 0.522, TOP, hw - 0.028, hw, 0, 'B');
  for (let x = x0 + 0.04; x < x1 - 0.04; x += 0.12) b.box(x, x + 0.03, 0.522, 0.545, -hw + 0.028, hw - 0.028, 0, 'B', { cast: false });
}
function mouth(b, h, w, y0, y1, c, mat) { // four rim bars around an open top (N and E inner faces are the seen ones)
  b.box(-h, -h + w, y0, y1, -h, h, c, mat); b.box(-h + w, h, y0, y1, -h, -h + w, c, mat);
  b.box(-h + w, h, y0, y1, h - w, h, c, mat, { back: true }); b.box(h - w, h, y0, y1, -h + w, h - w, c, mat, { back: true });
}
function frost(b, h, w, y0, c) { // light frost lip around the top edge (ru)
  b.box(-h, -h + w, y0, TOP, -h, h, c, 'B', { back: true }); b.box(-h + w, h, y0, TOP, -h, -h + w, c, 'B', { back: true });
  b.box(-h + w, h, y0, TOP, h - w, h, c, 'B', { back: true }); b.box(h - w, h, y0, TOP, -h + w, h - w, c, 'B', { back: true });
}
function openCrates(b, n, h, yf, cc, parts) { // open crate tops: floors at yf, crate walls up to TOP
  const w = 2 * h / n, tw = 0.026;
  for (let k = 0; k <= n; k++) { const c = Math.min(-h + k * w, h - tw), outer = k === 0;
    b.box(-h + cc, h, yf, TOP, outer ? -h + cc : c, outer ? -h + tw : c + tw, 0, 'F', { noWest: true, noSouth: outer, cast: false });
    b.box(outer ? -h + cc : c, outer ? -h + tw : c + tw, yf, TOP, -h + cc, h, 0, 'F', { noSouth: true, noWest: outer, cast: false }); }
  const R = rng(7); for (let ix = 0; ix < n; ix++) for (let iz = 0; iz < n; iz++) { if (!parts(ix, iz)) continue;
    const x = -h + (ix + 0.5) * w + (R() - 0.5) * w * 0.3, z = -h + (iz + 0.5) * w + (R() - 0.5) * w * 0.3;
    b.box(x - 0.04, x + 0.03, yf, yf + 0.035, z - 0.03, z + 0.04, 0.006, 'B', { cast: false }); }
}
// Unity-space point of a local offset `v` in a frame at `pos` with Unity Euler `rot`
function at(pos, rot, v) { const o = new THREE.Object3D(); setUnityEuler(o, rot); o.updateMatrix(); const p = U(v[0], v[1], v[2]).applyMatrix4(o.matrix);
  return [pos[0] + p.x, pos[1] + p.y, pos[2] - p.z]; }
// bridge filler in the 0.06 gap (x 0.47–0.53), recessed `inset` from the block faces
function bfill(b, y0, y1, inset, mat, o) { b.bbox(0.47, 0.53, y0, y1, -(0.47 - inset), 0.47 - inset, 0, mat, o); }
// rubble chunk resting on the board: box w×h×d, tilted about its x by `tilt`, then yawed
function chunk(b, w, h, d, x, z, yaw, tilt, mat, c = 0.008, o = {}) {
  const t = tilt * D2R, y = (o.y ?? 0) + (h * Math.cos(t) + d * Math.abs(Math.sin(t))) / 2;
  b.geom(gBox(w, h, d, c), mat, [x, y, z], [tilt, yaw, 0], o);
}
function chips(b, seed, n, mat, r = 0.4) { const R = rng(seed);
  for (let i = 0; i < n; i++) { const a = R() * 6.283, d = 0.12 + R() * (r - 0.12), s = 0.035 + R() * 0.03;
    chunk(b, s * 1.4, s, s, Math.cos(a) * d, Math.sin(a) * d, R() * 90, R() * 30, mat, 0.006, { cast: false }); } }
function lay(b, r, L, x, y, z, yaw, mat, seg = 8, o = {}) { // pipe / cylinder lying on the board along yaw (deg, Unity)
  const a = yaw * D2R, N = [Math.cos(a), 0, -Math.sin(a)], u = [Math.sin(a), 0, Math.cos(a)];
  b.ngon([x + N[0] * L / 2, y, z + N[2] * L / 2], N, u, [0, 1, 0], r, L, seg, mat, { backCap: true, ...o });
}

// ---------------------------------------------------------------- isolated blocks (columns), bridges, rubble per kit
Object.assign(KIT.weu, {
  isoName: 'tek rack kabini', isoNote: 'Tek uzun rack kabini (0,70 gövde, 0,78 süpürgelik, 0,74 başlık): her yüzde tek kapı, ortada uzun koyu delikli panel, dikey kol; üstte kısa kablo tavası. Kolon gibi okunur: taban + ince gövde + başlık.',
  iso(b) { const hs = 0.35; b.h = hs;
    b.box(-0.39, 0.39, 0, 0.045, -0.39, 0.39, 0.008, 'A', { back: true });
    b.box(-hs, hs, 0.045, 0.515, -hs, hs, 0, 'F', { noTop: true, back: true });
    b.box(-0.37, 0.37, 0.515, TOP, -0.37, -0.09, CB, 'F', { back: true }); b.box(-0.37, 0.37, 0.515, TOP, 0.09, 0.37, CB, 'F', { back: true });
    weuTray(b, -0.37, 0.37, 0.09);
    b.both(f => { b.fbox(f, 0.035, 0.665, 0.06, 0.50, hs, hs + 0.018, 0.01, 'F');
      b.fbox(f, 0.15, 0.47, 0.10, 0.465, hs + 0.018, hs + 0.024, 0, 'A', { cast: false });
      b.fbox(f, 0.545, 0.575, 0.15, 0.40, hs + 0.018, hs + 0.036, 0.006, 'B', { cast: false }); });
  },
  brName: 'kablo tavası', brNote: 'Komşu iki rack sırası arasında üst tava devam eder (taban detailA, raylar ve basamak detailB); aradaki derz gövde renginde, yüzlerden 12 mm geride.',
  br(b) { bfill(b, 0.045, 0.505, 0.012, 'F');
    b.bbox(0.47, 0.53, 0.515, 0.545, -0.458, -0.11, 0, 'F'); b.bbox(0.47, 0.53, 0.515, 0.545, 0.11, 0.458, 0, 'F');
    b.bbox(0.43, 0.57, 0.49, 0.522, -0.11, 0.11, 0, 'A'); b.bbox(0.43, 0.57, 0.522, TOP, -0.11, -0.082, 0, 'B'); b.bbox(0.43, 0.57, 0.522, TOP, 0.082, 0.11, 0, 'B');
    b.bbox(0.485, 0.515, 0.522, 0.545, -0.082, 0.082, 0, 'B', { cast: false }); },
  rubName: 'kırık rack kapağı ve kablo', rubNote: 'Yatık rack kapağı, kopmuş çerçeve rayı, bir kabin parçası, kıvrık kablo (detailA) ve yonga; toz lekesi.',
  rub(b) { b.dust(0.9, 0.9);
    chunk(b, 0.40, 0.026, 0.28, -0.10, -0.06, 25, 20, 'F'); chunk(b, 0.30, 0.012, 0.06, -0.10, -0.06, 25, 20, 'A', 0, { y: 0.01, cast: false });
    chunk(b, 0.50, 0.04, 0.04, 0.12, 0.17, -32, 0, 'F'); chunk(b, 0.14, 0.11, 0.12, 0.22, -0.22, 14, 0, 'F');
    b.geom(gTube([[-0.34, 0.016, -0.24], [-0.12, 0.03, -0.14], [0.06, 0.016, -0.26], [0.32, 0.02, -0.06]], 0.016, 5, 10), 'A', [0, 0, 0], [0, 0, 0], { cast: false });
    chips(b, 3, 4, 'F'); },
});

Object.assign(KIT.eeu, {
  isoName: 'raf ucu, etiket çerçeveli', isoNote: 'Arşiv rafının ucu (0,80): batıda üç krem etiket çerçeveli uç levhası, güneyde iki göz dosya kutusu, köşe dikmeleri üstte kapaklı. Kolon gibi okunur: dar, dikmeli.',
  iso(b) { const hs = 0.40; b.h = hs;
    b.box(-0.33, 0.33, 0.055, 0.512, -0.33, 0.33, 0, 'F', { noTop: true, back: true });
    b.box(-0.392, 0.392, 0, 0.055, -0.392, 0.392, 0.01, 'F', { back: true });
    b.box(-0.392, 0.392, 0.262, 0.302, -0.392, 0.392, 0.01, 'F');
    b.box(-0.392, 0.392, 0.512, 0.55, -0.392, 0.392, 0.012, 'F', { back: true });
    posts(b, 0.045, 0, 0.556, 0.008, { t: 'A', w: 'F', s: 'F' }); b.box(hs - 0.045, hs, 0.5, 0.556, hs - 0.045, hs, 0.008, { t: 'A', w: 'F', s: 'F' }, { back: true });
    b.fbox('W', 0.045, 0.755, 0.055, 0.512, 0.36, 0.396, 0.008, 'F');                               // end panel
    for (const y of [0.11, 0.27, 0.42]) fquad(b, 'W', 0.28, 0.52, y, y + 0.065, 0.3965, 'B');
    [[0.055, 0.262], [0.302, 0.512]].forEach(([y0, y1], i) => spread(0.06, 0.74, i ? [3, 2, 2.6] : [2, 3, 2.4], 0.014).forEach(([s0, s1], j) =>
      b.fbox('S', s0, s1, y0, y1 - 0.016, 0.33, 0.362, 0.012, (i + j) % 2 ? 'B' : 'A')));
  },
  brName: 'raf tahtası', brNote: 'Raf dudakları (alt, orta, üst) ve gözlerin arkası komşu rafa devam eder; alt gözde derzi örten bir kutu (yüz başına bir; E ve K yönünde görünen yüzlerde).',
  br(b) { b.bbox(0.47, 0.53, 0.055, 0.512, -0.40, 0.40, 0, 'F');
    b.bbox(0.455, 0.545, 0, 0.055, -0.462, 0.462, 0.01, 'F'); b.bbox(0.455, 0.545, 0.262, 0.302, -0.462, 0.462, 0.01, 'F');
    b.bbox(0.455, 0.545, 0.512, 0.53, -0.462, 0.462, 0.006, 'F');
    b.bbox(0.455, 0.545, 0.53, 0.555, -0.462, -0.432, 0.006, 'F'); b.bbox(0.455, 0.545, 0.53, 0.555, 0.432, 0.462, 0.006, 'F');
    b.bbox(0.44, 0.56, 0.055, 0.244, -0.432, -0.40, 0.01, 'B'); b.bbox(0.44, 0.56, 0.055, 0.244, 0.40, 0.432, 0.01, 'A'); },
  rubName: 'dağılmış dosyalar', rubNote: 'Devrilmiş ve dağılmış dosya kutuları (kurşuni ve krem), bükülmüş raf dudağı, yere saçılmış dosyalar (düz kâğıtlar); toz lekesi.',
  rub(b) { b.dust(0.9, 0.9);
    const R = rng(11); const P = [[-0.2, -0.15, 20, 0], [0.05, -0.25, 70, 12], [0.22, 0.05, -15, 0], [-0.12, 0.18, 40, 25], [0.15, 0.25, 85, 0]];
    P.forEach(([x, z, yaw, tilt], i) => chunk(b, 0.20, 0.13, 0.08, x, z, yaw, tilt, i % 2 ? 'B' : 'A', 0.01));
    chunk(b, 0.34, 0.035, 0.05, -0.05, 0.02, -20, 8, 'F'); chunk(b, 0.26, 0.035, 0.05, 0.2, -0.12, 35, 0, 'F');
    for (let i = 0; i < 4; i++) { const x = (R() - 0.5) * 0.7, z = (R() - 0.5) * 0.7, a = R() * 3; const c = Math.cos(a) * 0.1, s = Math.sin(a) * 0.1;
      b.poly([[x - c - s * 0.7, 0.004, z - s + c * 0.7], [x + c - s * 0.7, 0.004, z + s + c * 0.7], [x + c + s * 0.7, 0.004, z + s - c * 0.7], [x - c + s * 0.7, 0.004, z - s - c * 0.7]], 'B', [0, 1, 0], { cast: false }); }
  },
});

Object.assign(KIT.me, {
  isoName: 'çerçevede dik boru istifi', isoNote: 'Kare çelik çerçevede (0,80) dik duran 3 × 3 ince boru: üstten boru ağızları (koyu iç), yüzlerde dikey borular; çerçevenin ortasında kuşak. Tek iri silindir ya da fan yok (soğutma ünitesiyle karışmaz).',
  iso(b) { const hs = 0.40; b.h = hs;
    b.box(-hs, hs, 0, 0.05, -hs, hs, 0.01, 'A', { back: true });
    posts(b, 0.05, 0.05, TOP, 0.008, 'A'); b.box(hs - 0.05, hs, 0.05, TOP, hs - 0.05, hs, 0.008, 'A', { back: true });
    mouth(b, hs, 0.05, 0.505, TOP - 0.003, 0.008, 'A');
    b.box(-hs, -hs + 0.035, 0.25, 0.29, -hs + 0.05, hs - 0.05, 0.006, 'A'); b.box(-hs + 0.05, hs - 0.05, 0.25, 0.29, -hs, -hs + 0.035, 0.006, 'A');
    for (let ix = 0; ix < 3; ix++) for (let iz = 0; iz < 3; iz++) { const x = -0.235 + ix * 0.235, z = -0.235 + iz * 0.235;
      b.ngon([x, 0.545, z], [0, 1, 0], [1, 0, 0], [0, 0, 1], 0.1, 0.495, 8, 'F');                    // inner pipes show between the front ones
      b.ngon([x, 0.5458, z], [0, 1, 0], [1, 0, 0], [0, 0, 1], 0.058, 0, 8, 'A', { cast: false }); }
  },
  brName: 'flanşlı kısa boru', brNote: 'Komşu demetler arasında üstte, iki yüze yakın iki kısa boru makarası (her biri iki flanşlı); altta sehpa kirişi devam eder; derz gövde renginde.',
  br(b) { bfill(b, 0.14, 0.53, 0.04, 'F'); b.bbox(0.455, 0.545, 0.10, 0.14, -0.47, 0.47, 0.012, 'A');
    for (const z of [-0.375, 0.375]) { b.ngon([0.585, 0.49, z], [1, 0, 0], [0, 0, 1], [0, 1, 0], 0.048, 0.17, 8, 'F', { cap: false });
      for (const x of [0.482, 0.538]) b.ngon([x, 0.49, z], [1, 0, 0], [0, 0, 1], [0, 1, 0], 0.068, 0.02, 8, 'B', { backCap: true }); } },
  rubName: 'kesik boru parçaları', rubNote: 'Yere düşmüş iki kesik boru parçası (ağızları koyu), yatık bir flanş, kaplama sacı parçaları ve yonga; toz lekesi.',
  rub(b) { b.dust(0.9, 0.9);
    lay(b, 0.085, 0.36, -0.10, 0.085, -0.10, 30, 'F', 8, { capMat: 'A' }); lay(b, 0.07, 0.26, 0.17, 0.07, 0.15, -55, 'F', 8, { capMat: 'A' });
    b.ngon([0.2, 0.024, -0.22], [0, 1, 0], [1, 0, 0], [0, 0, 1], 0.11, 0.024, 8, 'B');
    chunk(b, 0.28, 0.02, 0.16, -0.22, 0.22, -20, 14, 'F'); chips(b, 5, 4, 'F'); },
});

Object.assign(KIT.af, {
  isoName: 'oluklu bunker ve boşaltma oluğu', isoNote: 'Dar oluklu bunker (0,76): her yüzde 9 dikey oluk, açık köşe köşebentleri, içinde cevher; güney yüzünün altında eğik boşaltma oluğu (koyu ağız). Kolon gibi okunur: dar, dikey çizgili.',
  iso(b) { const hs = 0.38; b.h = hs;
    b.box(-0.36, 0.36, 0, 0.045, -0.36, 0.36, 0.006, 'S', { noTop: true, back: true });
    b.box(-hs, hs, 0.045, 0.47, -hs, hs, 0, { t: 'A', w: 'G', s: 'G' }, { back: true });
    mouth(b, hs, 0.035, 0.47, TOP, 0.008, 'G');
    b.both(f => { for (let k = 0; k < 9; k++) { const s = 0.085 + k * 0.074; b.fbox(f, s, s + 0.026, 0.06, 0.535, hs, hs + 0.02, 0, 'G', { cast: false }); } });
    posts(b, 0.06, 0.045, TOP, 0.01, 'B', hs + 0.02);
    b.geom(gLathe([[0.33, 0.468], [0.26, 0.50], [0.16, 0.528], [0.06, 0.543], [0, 0.546]], 7, { phiStart: 0.4 }), 'A', [0, 0, 0.01], [0, 17, 0]);
    const s0 = 0.27, s1 = 0.53, t0 = hs + 0.02, P = (s, y, t) => b.P('S', s, y, t);       // hopper chute
    b.poly([P(s0, 0.33, t0), P(s1, 0.33, t0), P(s1, 0.11, 0.47), P(s0, 0.11, 0.47)], 'G', [0, 1, -1]);
    b.poly([P(s0, 0.33, t0), P(s0, 0.11, 0.47), P(s0, 0.05, 0.47), P(s0, 0.05, t0)], 'G', [-1, 0, 0]);
    b.poly([P(s0, 0.05, 0.47), P(s1, 0.05, 0.47), P(s1, 0.11, 0.47), P(s0, 0.11, 0.47)], 'G', [0, 0, -1]);
    fquad(b, 'S', s0 + 0.03, s1 - 0.03, 0.058, 0.10, 0.4705, 'V');
  },
  brName: 'çelik I kirişi', brNote: 'Komşu bunkerlerin üst ağızları arasında iki I kirişi (detailB); arada oluklu sac derz, yüze yakın birer oluk devam eder.',
  br(b) { bfill(b, 0.045, 0.46, 0.02, 'G'); b.bbox(0.488, 0.512, 0.06, 0.535, -0.47, -0.45, 0, 'G', { cast: false }); b.bbox(0.488, 0.512, 0.06, 0.535, 0.45, 0.47, 0, 'G', { cast: false });
    b.bbox(0.47, 0.53, 0.47, 0.545, -0.45, -0.41, 0, 'G'); b.bbox(0.47, 0.53, 0.47, 0.545, 0.41, 0.45, 0, 'G');
    for (const z of [-0.22, 0.22]) { b.bbox(0.448, 0.552, 0.533, 0.555, z - 0.045, z + 0.045, 0.004, 'B'); b.bbox(0.448, 0.552, 0.47, 0.533, z - 0.013, z + 0.013, 0, 'B'); } },
  rubName: 'cevher ve sac kırığı', rubNote: 'Basık cevher yığını (detailA), bükülmüş oluklu sac parçası (üstünde üç oluk), köşebent parçası ve taşlar; toz lekesi.',
  rub(b) { b.dust(0.9, 0.9);
    b.geom(gLathe([[0.30, 0.0], [0.22, 0.07], [0.12, 0.12], [0.03, 0.145], [0, 0.146]], 7, { phiStart: 0.3 }), 'A', [-0.08, 0, 0.06], [0, 0, 0], { scale: [1.1, 1, 0.85] });
    chunk(b, 0.34, 0.02, 0.26, 0.18, -0.16, -30, 24, 'G');
    for (let k = 0; k < 3; k++) chunk(b, 0.026, 0.02, 0.24, 0.18 + (k - 1) * 0.085 * Math.cos(-30 * D2R), -0.16 + (k - 1) * 0.085 * Math.sin(30 * D2R), -30, 24, 'G', 0, { y: 0.012, cast: false });
    chunk(b, 0.36, 0.05, 0.05, -0.22, -0.22, 60, 0, 'B'); chips(b, 9, 5, 'A'); },
});

Object.assign(KIT.na, {
  isoName: 'tek uzun streç filmli istif', isoNote: 'Paletin (0,80) üstünde dar ve uzun dört kat koli istifi (0,72): şaşırtmalı derzler, filmle kapanmış yumuşak üst, ortada açık renk streç film bandı; paletin koyu ağızları görünür.',
  iso(b) { const hp = 0.40; b.h = hp;
    b.box(-0.30, 0.30, 0, 0.075, -0.30, 0.30, 0, 'V', { noTop: true, back: true });
    b.box(-hp, hp, 0.075, 0.11, -hp, hp, 0.008, 'A', { back: true });
    for (const [s0, s1] of [[0, 0.12], [0.34, 0.46], [0.68, 0.80]]) { b.fbox('W', s0, s1, 0, 0.075, 0.27, hp, 0.008, 'A'); if (s0 > 0) b.fbox('S', s0, s1, 0, 0.075, 0.27, hp, 0.008, 'A'); }
    const hs = 0.36; grid(b, 3, hs, 0.11, 0.22, 0.01, 0.01, 'F'); grid(b, 2, hs, 0.22, 0.33, 0.01, 0.01, 'F'); grid(b, 3, hs, 0.33, 0.44, 0.01, 0.01, 'F');
    b.box(-hs, hs, 0.44, 0.55, -hs, hs, 0.035, 'F', { back: true });
    b.box(-hs - 0.006, hs + 0.006, 0.17, 0.47, -hs - 0.006, hs + 0.006, 0.012, 'rk_film', { noTop: true });
  },
  brName: 'galvaniz palet kirişi', brNote: 'Komşu paletler arasında, iki yüze yakın galvaniz kutu kiriş (rk_galv, turuncu değil); palet tahtası ve koli derzi devam eder.',
  br(b) { bfill(b, 0.11, 0.535, 0.015, 'F'); b.bbox(0.47, 0.53, 0.075, 0.11, -0.40, 0.40, 0, 'A');
    b.bbox(0.42, 0.58, 0.03, 0.105, -0.47, -0.40, 0.008, 'rk_galv'); b.bbox(0.42, 0.58, 0.03, 0.105, 0.40, 0.47, 0.008, 'rk_galv'); },
  rubName: 'dağılmış koliler', rubNote: 'Yıkılmış üç koli (biri yan yatmış), kırık palet tahtası (detailA), koli yongaları; toz lekesi.',
  rub(b) { b.dust(0.9, 0.9);
    chunk(b, 0.26, 0.16, 0.22, -0.14, -0.10, 18, 0, 'F', 0.012); chunk(b, 0.24, 0.15, 0.20, 0.14, 0.12, -35, 0, 'F', 0.012);
    chunk(b, 0.22, 0.12, 0.18, 0.18, -0.22, 60, 25, 'F', 0.012); chunk(b, 0.50, 0.025, 0.09, -0.10, 0.25, 12, 10, 'A'); chips(b, 13, 4, 'F'); },
});

Object.assign(KIT.ru, {
  isoName: 'kapılı panel kabin', isoNote: 'Sandviç panelden kabin (0,84): güneyde koyu çerçeveli kapı ve büyük kol (detailB), batıda iki panel ve derz çıtası, üstte kırağı dudağı ve nervürlü çatı.',
  iso(b) { const hs = 0.42; b.h = hs;
    b.box(-0.40, 0.40, 0, 0.04, -0.40, 0.40, 0.008, 'A', { noTop: true, back: true });
    b.box(-hs, hs, 0.04, 0.52, -hs, hs, 0, 'F', { back: true });
    frost(b, 0.445, 0.04, 0.515, 0.018);
    for (const z of [-0.26, -0.13, 0, 0.13, 0.26]) b.box(-0.405, 0.405, 0.52, 0.55, z - 0.024, z + 0.024, 0.008, 'F');
    posts(b, 0.05, 0.04, 0.515, 0.01, 'F', 0.445);
    b.fbox('W', 0.42, 0.465, 0.04, 0.515, hs, 0.445, 0.008, 'F');
    b.fbox('S', 0.20, 0.66, 0.04, 0.485, hs, hs + 0.008, 0, 'A');
    b.fbox('S', 0.22, 0.64, 0.05, 0.47, hs + 0.008, hs + 0.02, 0.008, 'F');
    b.fbox('S', 0.50, 0.61, 0.25, 0.28, hs + 0.02, 0.465, 0.006, 'B', { cast: false });
  },
  brName: 'yalıtımlı yuvarlak kanal', brNote: 'Komşu kabinler arasında üstte iki yalıtımlı yuvarlak kanal (iki ceket bandı, detailB); kırağı dudağı ve çatı derzi devam eder.',
  br(b) { bfill(b, 0.04, 0.505, 0.012, 'F'); b.bbox(0.47, 0.53, 0.505, 0.54, -0.43, 0.43, 0, 'F');
    b.bbox(0.43, 0.57, 0.515, TOP, -0.47, -0.43, 0.018, 'B'); b.bbox(0.43, 0.57, 0.515, TOP, 0.43, 0.47, 0.018, 'B');
    for (const z of [-0.33, 0.33]) { b.ngon([0.6, 0.47, z], [1, 0, 0], [0, 0, 1], [0, 1, 0], 0.052, 0.2, 8, 'F', { cap: false });
      for (const x of [0.488, 0.532]) b.ngon([x, 0.47, z], [1, 0, 0], [0, 0, 1], [0, 1, 0], 0.058, 0.014, 8, 'B', { backCap: true, cast: false }); } },
  rubName: 'panel ve köpük kırığı', rubNote: 'Kırılmış iki sandviç panel parçası, açık renk köpük parçaları (detailB), kırağı yongaları; toz lekesi.',
  rub(b) { b.dust(0.9, 0.9);
    chunk(b, 0.42, 0.06, 0.30, -0.10, -0.08, 22, 16, 'F'); chunk(b, 0.30, 0.06, 0.24, 0.18, 0.18, -40, 0, 'F');
    chunk(b, 0.10, 0.07, 0.08, 0.20, -0.22, 10, 0, 'B'); chunk(b, 0.08, 0.06, 0.07, -0.25, 0.22, 50, 0, 'B'); chips(b, 17, 4, 'B'); },
});

Object.assign(KIT.in, {
  isoName: 'uzun dosya dolabı', isoNote: 'Dört çekmeceli uzun dosya dolabı (0,68): güneyde çekmeceler (kulp + etiket yeri, detailB), batıda dolaba dayalı çerçeveli bölme paneli (detailB çerçeve, detailA kumaş).',
  iso(b) { const hs = 0.34; b.h = hs;
    b.box(-0.32, 0.32, 0, 0.035, -0.32, 0.32, 0.006, 'A', { noTop: true, back: true });
    b.box(-hs, hs, 0.035, 0.53, -hs, hs, 0, 'F', { noTop: true, back: true });
    b.box(-0.355, 0.355, 0.53, TOP, -0.355, 0.355, CB, 'F', { back: true });
    [[0.045, 0.16], [0.167, 0.282], [0.289, 0.404], [0.411, 0.524]].forEach(([y0, y1]) => {
      b.fbox('S', 0.03, 0.65, y0, y1, hs, hs + 0.016, 0.01, 'F');
      b.fbox('S', 0.27, 0.41, y1 - 0.05, y1 - 0.027, hs + 0.016, hs + 0.026, 0, 'B', { cast: false });
      fquad(b, 'S', 0.30, 0.38, y0 + 0.03, y0 + 0.058, hs + 0.0165, 'B'); });
    b.fbox('W', 0.06, 0.62, 0.05, 0.50, hs, hs + 0.02, 0, 'A');
    b.fbox('W', 0.06, 0.62, 0.05, 0.08, hs + 0.02, hs + 0.04, 0.006, 'B'); b.fbox('W', 0.06, 0.62, 0.47, 0.50, hs + 0.02, hs + 0.04, 0.006, 'B');
    b.fbox('W', 0.06, 0.09, 0.08, 0.47, hs + 0.02, hs + 0.04, 0.006, 'B'); b.fbox('W', 0.59, 0.62, 0.08, 0.47, hs + 0.02, hs + 0.04, 0.006, 'B');
    b.fbox('W', 0.325, 0.355, 0.08, 0.47, hs + 0.02, hs + 0.036, 0.006, 'B');
  },
  brName: 'alçak bölme', brNote: 'Komşu istifler arasında alçak bölme paneli: dolap derzi, alt alüminyum ray ve üstü alüminyum kapaklı koyu kumaş panel (0,445 yüksekliğe kadar; üstü açık).',
  br(b) { bfill(b, 0, 0.27, 0.012, 'F'); b.bbox(0.455, 0.545, 0.27, 0.30, -0.47, 0.47, 0.01, 'B');
    b.bbox(0.47, 0.53, 0.30, 0.42, -0.44, 0.44, 0, 'A'); b.bbox(0.462, 0.538, 0.42, 0.445, -0.455, 0.455, 0.008, 'B'); },
  rubName: 'dağılmış kâğıt', rubNote: 'Yere saçılmış kâğıtlar (düz, detailB), düşmüş bir çekmece, kırık bölme paneli parçası (koyu kumaş) ve yonga; toz lekesi.',
  rub(b) { b.dust(0.9, 0.9); const R = rng(23);
    for (let i = 0; i < 6; i++) { const x = (R() - 0.5) * 0.56, z = (R() - 0.5) * 0.56, a = R() * 3, c = Math.cos(a) * 0.09, s = Math.sin(a) * 0.09, y = 0.003 + i * 0.0008;
      b.poly([[x - c - s * 1.3, y, z - s + c * 1.3], [x + c - s * 1.3, y, z + s + c * 1.3], [x + c + s * 1.3, y, z + s - c * 1.3], [x - c + s * 1.3, y, z - s - c * 1.3]], 'B', [0, 1, 0], { cast: false }); }
    chunk(b, 0.36, 0.12, 0.26, -0.08, -0.06, 20, 0, 'F', 0.01); chunk(b, 0.30, 0.10, 0.20, -0.08, -0.06, 20, 0, 'A', 0.004, { y: 0.03, cast: false });
    chunk(b, 0.34, 0.03, 0.22, 0.18, 0.2, -30, 18, 'A'); chunk(b, 0.34, 0.03, 0.03, 0.18, 0.08, -30, 0, 'B'); chips(b, 29, 3, 'F'); },
});

Object.assign(KIT.cn, {
  isoName: 'uzun kasa kulesi', isoNote: 'Koyu altlık üstünde üç kat dar parça kasası (0,62): kaburgalı yanlar, koyu tutamak yuvaları, etiket cepleri; en üst kasa açık, içinde parçalar.',
  iso(b) { const hs = 0.31; b.h = hs;
    b.box(-0.35, 0.35, 0, 0.035, -0.35, 0.35, 0.006, 'A', { back: true });
    b.box(-0.25, 0.25, 0.035, 0.49, -0.25, 0.25, 0, { t: 'A', w: 'F', s: 'F' }, { back: true });
    const T = [[0.035, 0.205], [0.21, 0.38], [0.385, TOP]];
    T.forEach(([y0, y1]) => { b.box(-hs, hs, y0, y1, -hs, hs, 0.014, 'F', { noTop: true });
      b.both(f => { fquad(b, f, 0.24, 0.38, y1 - 0.065, y1 - 0.035, hs + 0.002, 'V'); fquad(b, f, 0.25, 0.37, y0 + 0.03, y0 + 0.075, hs + 0.002, 'B');
        for (const s of [0.09, 0.53]) b.fbox(f, s, s + 0.026, y0 + 0.012, y1 - 0.012, hs, hs + 0.018, 0, 'F', { cast: false }); }); });
    openCrates(b, 1, hs, 0.49, 0.014, () => true); b.box(-0.12, -0.05, 0.49, 0.525, 0.04, 0.11, 0.006, 'B', { cast: false }); b.box(0.06, 0.13, 0.49, 0.52, -0.1, -0.04, 0.006, 'B', { cast: false });
  },
  brName: 'kablo zinciri', brNote: 'Komşu kasa yığınları arasında, iki yüze yakın, orta yükseklikte mafsallı kablo zinciri (koyu baklalar detailA, pimler detailB); kasa derzi gövde renginde.',
  br(b) { bfill(b, 0, 0.49, 0.03, 'F');
    for (const z of [-0.465, 0.405]) { for (const x of [0.425, 0.5, 0.575]) b.bbox(x - 0.032, x + 0.032, 0.245, 0.305, z, z + 0.06, 0.01, 'A');
      for (const x of [0.4625, 0.5375]) b.bbox(x - 0.013, x + 0.013, 0.262, 0.288, z - 0.004, z + 0.064, 0, 'B', { cast: false }); } },
  rubName: 'saçılmış parçalar', rubNote: 'Yan yatmış bir parça kasası (koyu tutamak yuvası yukarıda), yere saçılmış küçük parçalar (somun, pul; detailB ve detailA); toz lekesi.',
  rub(b) { b.dust(0.9, 0.9);
    chunk(b, 0.30, 0.18, 0.26, -0.08, -0.04, 25, 0, 'F', 0.012); b.geom(gBox(0.12, 0.004, 0.035, 0), 'V', [-0.1, 0.182, -0.04], [0, 25, 0], { cast: false });
    const R = rng(31); for (let i = 0; i < 9; i++) { const a = R() * 6.283, d = 0.18 + R() * 0.22;
      chunk(b, 0.05, 0.03, 0.05, Math.cos(a) * d, Math.sin(a) * d, R() * 90, 0, i % 3 ? 'B' : 'A', 0.006, { cast: false }); } },
});

Object.assign(KIT.jk, {
  isoName: 'tek panjurlu pano', isoNote: 'Tek şalt pano hücresi (0,72): güneyde kapı (iki grup yatay panjur, kilit topuzu), batı yan sacında üstte panjur grubu; çatıda kablo giriş plakası ve rakorlar.',
  iso(b) { const hs = 0.36; b.h = hs;
    b.box(-0.34, 0.34, 0, 0.05, -0.34, 0.34, 0.008, 'A', { noTop: true, back: true });
    b.box(-hs, hs, 0.05, 0.515, -hs, hs, 0, 'F', { noTop: true, back: true });
    b.box(-0.39, 0.39, 0.515, 0.55, -0.39, 0.39, CB, 'F', { back: true });
    b.box(-0.15, 0.15, 0.55, 0.556, -0.1, 0.1, 0, 'B', { cast: false });
    for (const dx of [-0.08, 0, 0.08]) b.ngon([dx, 0.5565, 0], [0, 1, 0], [1, 0, 0], [0, 0, 1], 0.028, 0, 8, 'V', { cast: false });
    b.fbox('S', 0.03, 0.69, 0.065, 0.50, hs, hs + 0.015, 0.008, 'F');
    for (const y of [0.10, 0.145, 0.19, 0.235, 0.36, 0.405, 0.45]) b.fbox('S', 0.10, 0.62, y, y + 0.026, hs + 0.015, hs + 0.03, 0, 'F', { cast: false });
    b.fround('S', 0.63, 0.30, 0.024, hs + 0.015, hs + 0.03, 8, 'B', { cast: false });
    for (const y of [0.36, 0.405, 0.45]) b.fbox('W', 0.12, 0.60, y, y + 0.026, hs, hs + 0.015, 0, 'F', { cast: false });
  },
  brName: 'bara kanalı', brNote: 'Komşu panolar arasında üstte bara kanalı (detailB, iki ucunda flanş); pano ve çatı derzi gövde renginde.',
  br(b) { bfill(b, 0.05, 0.505, 0.012, 'F'); b.bbox(0.47, 0.53, 0.515, 0.54, -0.458, 0.458, 0, 'F');
    b.bbox(0.44, 0.56, 0.47, 0.548, -0.14, 0.14, 0.008, 'B');
    for (const x of [0.457, 0.543]) b.bbox(x - 0.013, x + 0.013, 0.46, 0.552, -0.165, 0.165, 0, 'B', { cast: false }); },
  rubName: 'pano kapağı ve kablo', rubNote: 'Yere düşmüş pano kapağı (üstünde panjur çıtaları), kopuk kablo (detailA), sac parçaları; toz lekesi.',
  rub(b) { b.dust(0.9, 0.9);
    const dp = [-0.06, (0.026 * Math.cos(20 * D2R) + 0.34 * Math.sin(20 * D2R)) / 2, -0.06], dr = [20, -20, 0];
    b.geom(gBox(0.40, 0.026, 0.34, 0.008), 'F', dp, dr);                                                     // fallen panel door
    for (let k = 0; k < 3; k++) b.geom(gBox(0.26, 0.026, 0.028, 0), 'F', at(dp, dr, [0, 0.024, (k - 1) * 0.075]), dr, { cast: false }); // louvre slats
    b.geom(gTube([[0.30, 0.016, -0.30], [0.18, 0.03, -0.10], [0.28, 0.016, 0.10], [0.05, 0.02, 0.30]], 0.016, 5, 10), 'A', [0, 0, 0], [0, 0, 0], { cast: false });
    chunk(b, 0.18, 0.02, 0.12, 0.22, 0.22, 40, 20, 'F'); chips(b, 37, 3, 'F'); },
});

Object.assign(KIT.sea, {
  isoName: 'kum torbası sütunu', isoNote: 'Dört kat çapraz örülmüş kum torbası sütunu (0,62): her katta iki torba, kat kat yön değiştirir; en alt kat koyu ıslak (detailA).',
  iso(b) { const hs = 0.31, hr = 0.1375, c = 0.045;
    for (let r = 0; r < 4; r++) { const y0 = r * hr, y1 = r === 3 ? 0.55 : y0 + hr, mat = r === 0 ? 'A' : 'F', top = r === 3;
      if (r % 2 === 0) { b.box(-hs, hs, y0, y1, -hs, -0.004, c, mat, { noTop: !top }); b.box(-hs, hs, y0, y1, 0.004, hs, c, mat, { noTop: !top, back: true }); }
      else { b.box(-hs, -0.004, y0, y1, -hs, hs, c, mat, { noTop: !top }); b.box(0.004, hs, y0, y1, -hs, hs, c, mat, { noTop: !top, back: true }); } }
    b.box(-0.25, 0.25, 0, 0.42, -0.25, 0.25, 0, 'A', { noTop: true });
  },
  brName: 'üstte tahta', brNote: 'Komşu torba yığınları arasında üstte, torbalara gömülü yatık tahta (detailB); arada torba rengi derz (yuvarlak torbalar nedeniyle 40 mm geride).',
  br(b) { bfill(b, 0, 0.53, 0.04, 'F'); b.bbox(0.36, 0.64, 0.53, TOP, -0.10, 0.10, 0.006, 'B'); },
  rubName: 'patlamış torbalar', rubNote: 'Yassılmış patlak torbalar, dökülmüş kum yığını (üst rengi), ıslak koyu bir torba (detailA) ve yonga; toz lekesi.',
  rub(b) { b.dust(0.92, 0.92);
    b.geom(gLathe([[0.28, 0.0], [0.2, 0.06], [0.1, 0.10], [0, 0.11]], 7, { phiStart: 0.2 }), 'T', [0.05, 0, 0.04], [0, 0, 0], { scale: [1.15, 1, 0.9] });
    chunk(b, 0.32, 0.07, 0.20, -0.18, -0.15, 25, 8, 'F', 0.03); chunk(b, 0.30, 0.07, 0.19, 0.20, -0.20, -40, 0, 'A', 0.03); chunk(b, 0.30, 0.06, 0.18, -0.12, 0.24, 70, 12, 'F', 0.028); chips(b, 41, 3, 'T'); },
});

Object.assign(KIT.sa, {
  isoName: 'kolon kalıbı (kelepçeli pano çifti)', isoNote: 'Kare kolon kalıbı (0,72): dört kalıp panosu, yüzlerde nervür ızgarası, iki kat koyu çelik kelepçe (köşelerden taşan kamalar), altta galvaniz levha; üstte düz kapak.',
  iso(b) { const hs = 0.36; b.h = hs;
    b.box(-0.42, 0.42, 0, 0.03, -0.42, 0.42, 0.006, 'G', { back: true });
    b.box(-hs, hs, 0.03, 0.525, -hs, hs, 0, 'F', { noTop: true, back: true });
    b.box(-0.375, 0.375, 0.525, TOP, -0.375, 0.375, CB, 'F', { back: true });
    b.both(f => { for (const s of [0.18, 0.36, 0.54]) b.fbox(f, s - 0.013, s + 0.013, 0.03, 0.525, hs, hs + 0.018, 0.005, 'F', { cast: false });
      for (const y of [0.27]) b.fbox(f, 0, 0.72, y, y + 0.024, hs, hs + 0.018, 0.005, 'F', { cast: false }); });
    for (const y of [0.13, 0.40]) { b.box(-hs - 0.035, hs + 0.035, y, y + 0.045, -hs - 0.035, hs + 0.035, 0.008, 'A', { noTop: false });
      b.box(-hs - 0.035, -hs + 0.0, y, y + 0.045, hs + 0.035, hs + 0.095, 0.006, 'A', { cast: false }); b.box(hs + 0.035, hs + 0.095, y, y + 0.045, -hs - 0.035, -hs, 0.006, 'A', { cast: false }); }
  },
  brName: 'çelik U kiriş', brNote: 'Komşu kalıp istifleri arasında, iki yüze yakın orta nervür hizasında çelik U kiriş (detailA); levha istifi ve kalıp derzi devam eder.',
  br(b) { bfill(b, 0.145, 0.53, 0.012, 'F'); b.bbox(0.455, 0.545, 0.07, 0.145, -0.47, 0.47, 0.006, 'G');
    b.bbox(0.43, 0.57, 0.26, 0.30, -0.47, -0.43, 0.006, 'A'); b.bbox(0.43, 0.57, 0.26, 0.30, 0.43, 0.47, 0.006, 'A'); },
  rubName: 'eğri levhalar', rubNote: 'Birbirine yaslanmış eğri çelik levhalar, kalıp nervürü parçası, ahşap takoz (detailA) ve yonga; toz lekesi.',
  rub(b) { b.dust(0.9, 0.9);
    chunk(b, 0.44, 0.025, 0.30, -0.06, -0.08, 20, 22, 'G'); chunk(b, 0.40, 0.025, 0.26, 0.10, 0.12, -35, 10, 'G');
    chunk(b, 0.18, 0.07, 0.12, -0.24, 0.22, 30, 0, 'A'); chunk(b, 0.40, 0.03, 0.03, 0.18, -0.24, 75, 0, 'F'); chips(b, 43, 3, 'G'); },
});

Object.assign(KIT.oc, {
  isoName: 'kızak üstünde uzun sandık', isoNote: 'Baklava sac kızak üstünde dar ve uzun yedek parça sandığı (0,72): çıta çerçeve, her yüzde çapraz çıta, üst köşelerde çelik köşe sacı, kapakta çıta.',
  iso(b) { const hs = 0.36; b.h = hs;
    b.box(-0.36, 0.36, 0, 0.08, -0.22, 0.22, 0, 'V', { noTop: true });
    b.box(-0.38, 0.38, 0, 0.08, -0.38, -0.22, 0.008, 'A'); b.box(-0.38, 0.38, 0, 0.08, 0.22, 0.38, 0.008, 'A', { back: true });
    b.box(-0.385, 0.385, 0.08, 0.105, -0.385, 0.385, 0.006, 'A', { back: true });
    b.box(-0.315, 0.315, 0.105, 0.50, -0.315, 0.315, 0, 'F', { noTop: true, back: true });
    b.box(-hs, hs, 0.105, 0.16, -hs, hs, 0.012, 'F'); b.box(-hs, hs, 0.50, 0.55, -hs, hs, CB, 'F', { back: true });
    b.box(-0.04, 0.035, 0.55, 0.556, -0.30, 0.30, 0, 'F', { cast: false });
    posts(b, 0.05, 0.16, 0.50, 0.01, 'F', hs);
    b.both(f => b.fprism(f, [[0.05, 0.16], [0.14, 0.16], [0.67, 0.43], [0.67, 0.50], [0.58, 0.50], [0.05, 0.23]], 0.315, 0.352, 'F'));
    for (const [x0, z0] of [[-hs - 0.005, -hs - 0.005], [-hs - 0.005, hs - 0.08], [hs - 0.08, -hs - 0.005]]) { b.box(x0, x0 + 0.085, 0.47, 0.556, z0, z0 + 0.085, 0.01, 'A', { cast: false });
      b.box(x0, x0 + 0.085, 0.105, 0.19, z0, z0 + 0.085, 0.01, 'A', { cast: false }); }
  },
  brName: 'baklava sac şerit', brNote: 'Komşu sandıkların kapakları üstünde baklava sac şerit (detailA, desen doku); kızak ayakları ve sandık derzi devam eder.',
  br(b) { bfill(b, 0.105, 0.51, 0.012, 'F'); b.bbox(0.47, 0.53, 0.08, 0.105, -0.47, 0.47, 0.006, 'A');
    b.bbox(0.455, 0.545, 0, 0.08, -0.46, -0.31, 0.008, 'A'); b.bbox(0.455, 0.545, 0, 0.08, 0.31, 0.46, 0.008, 'A');
    b.bbox(0.40, 0.60, 0.55, 0.556, -0.16, 0.16, 0, 'A', { cast: false }); },
  rubName: 'sac ve cıvata', rubNote: 'Baklava sac parçası (detailA), kırık sandık tahtası, dağılmış cıvatalar (detailB) ve yonga; toz lekesi.',
  rub(b) { b.dust(0.9, 0.9);
    chunk(b, 0.40, 0.025, 0.30, -0.08, -0.06, 15, 18, 'A'); chunk(b, 0.50, 0.03, 0.09, 0.10, 0.20, -28, 6, 'F'); chunk(b, 0.20, 0.10, 0.14, 0.22, -0.20, 40, 0, 'F', 0.01);
    const R = rng(47); for (let i = 0; i < 5; i++) { const a = R() * 6.283, d = 0.15 + R() * 0.25; lay(b, 0.016, 0.09, Math.cos(a) * d, 0.016, Math.sin(a) * d, R() * 180, 'B', 6, { cast: false }); }
    chips(b, 53, 3, 'F'); },
});

// ---------------------------------------------------------------- model factories
const COMMON_ISO = 'Ayrık blok: 8 komşusunda duvar olmayan karede gövdenin yerine geçer (tek renderer). Düz üst 0,55–0,56; üst yüz daha dar olduğundan işaretler kenardan biraz taşabilir. Görünmeyen üçgenler silindi.';
const COMMON_BR = 'Kare başına en çok iki köprü (doğu: olduğu gibi, kuzey: y ekseninde −90°, +x → +z). İki yönde de görünen yüzler bırakıldı; −z yüzü güney, +z yüzü (kuzey yönünde batıya döner) batı malzemesi alır. Üst ≤ 0,556; boşlukta yalnız üst 6 cm ve yüze yakın 6 cm görünür, köprü parçaları oraya yerleşti.';
const COMMON_RUB = 'Gedik molozu: karenin içinde (±0,47), 0,10–0,19 yüksek, tek renderer; kitin yüz malzemeleri + vurgu, toz lekesi dörtgeni (rk_dust, UV 0–1, gölgesiz). Görünmeyen üçgenler silindi.';
function isoModel(id) { const K = KIT[id]; const m = mk(`ReliefIso_${id}`, { listName: `Ayrık blok · ${K.bina} (${K.isoName})`, note: `${K.isoNote} ${COMMON_ISO}` });
  const b = new KB(m, id); K.iso(b); b.finish(); return m; }
function bridgeModel(id) { const K = KIT[id]; const m = mk(`Bridge_${id}`, { listName: `Köprü · ${K.bina} (${K.brName})`, origin: ORIGIN_BR, note: `${K.brNote} ${COMMON_BR}` });
  const b = new KB(m, id, { views: [VE, VN], bridge: true }); K.br(b); b.finish(); return m; }
function rubbleModel(id) { const K = KIT[id]; const m = mk(`Rubble_${id}`, { listName: `Gedik molozu · ${K.bina} (${K.rubName})`, note: `${K.rubNote} ${COMMON_RUB}` });
  const b = new KB(m, id); K.rub(b); b.finish(); return m; }

// ---------------------------------------------------------------- sliding archive shelf (eeu event): shelf + hand wheel + two castors
function SlidingShelf() {
  const m = mk('SlidingShelf', { listName: 'Kayar arşiv rafı (el çarkı, iki teker)',
    note: 'Doğu Avrupa olayı. Arşiv rafının aynısı (güneyde raf gözleri ve kutular) + batıdaki uç levhasında üç kollu el çarkı (Wheel, ayrılmış "kayar raf" işareti) ve uç levhasının altında iki teker. Modelde ray K–G (çark batıda); ray D–B için x ↔ z aynası: localScale (−1, 1, 1) + y 90°. Kare başına 2 renderer (Body + Wheel) + raya göre 1 köprü. Kaymada çark döner.' });
  const b = new KB(m, 'eeu');
  b.box(-0.40, 0.40, 0.055, 0.512, -0.40, 0.40, 0, 'F', { noTop: true, back: true });
  b.box(-0.425, 0.462, 0, 0.055, -0.462, 0.462, 0.01, 'F', { back: true });                        // bottom shelf (set back on the west: castors)
  b.box(-0.462, 0.462, 0.262, 0.302, -0.462, 0.462, 0.01, 'F');
  b.box(-0.462, 0.462, 0.512, 0.55, -0.462, 0.462, 0.012, 'F', { back: true });
  [[[-0.43, -0.012], [2, 3, 2.2, 2.6], 'TBTB'], [[0.012, 0.43], [3, 2, 2.6, 2.2], 'BTTB']].forEach(([[z0, z1], rel, mm]) =>
    spread(-0.43, 0.43, rel, 0.014).forEach(([x0, x1], j) => b.poly([[x0, 0.5508, z0], [x1, 0.5508, z0], [x1, 0.5508, z1], [x0, 0.5508, z1]], mm[j], [0, 1, 0], { cast: false }))); // box lids
  posts(b, 0.045, 0, 0.556, 0.008, { t: 'A', w: 'F', s: 'F' }); b.box(0.425, 0.47, 0.5, 0.556, 0.425, 0.47, 0.008, { t: 'A', w: 'F', s: 'F' }, { back: true });
  [[0.055, 0.262], [0.302, 0.512]].forEach(([y0, y1], i) => spread(0.065, 0.875, i ? [2.2, 3, 3] : [2.4, 2, 3, 2], 0.014).forEach(([s0, s1], j) =>
    b.fbox('S', s0, s1, y0, y1 - 0.016, 0.40, 0.432, 0.012, (i + j) % 3 === 1 ? 'A' : 'B')));
  b.fbox('W', 0.045, 0.895, 0.078, 0.512, 0.44, 0.466, 0.008, 'F');                                  // end plate
  fquad(b, 'W', 0.36, 0.58, 0.44, 0.49, 0.4665, 'B');                                                // label frame
  b.fround('W', 0.47, 0.30, 0.045, 0.462, 0.47, 8, 'A');                                             // wheel boss
  for (const s of [0.20, 0.74]) { b.fbox('W', s - 0.045, s + 0.045, 0.062, 0.078, 0.42, 0.462, 0, 'A', { cast: false });
    b.fround('W', s, 0.034, 0.034, 0.435, 0.465, 8, 'A', { cast: false }); }                         // castors under the end plate (bracket + wheel)
  b.finish();
  // Wheel: three-arm hand wheel, axis = face normal (x); kept closed except the side facing the end plate (it spins)
  const pv = [-0.47, 0.30, 0];
  m.part('Wheel', { pivot: pv, axis: 'x', range: [0, 360], kind: 'rotate', note: 'üç kollu el çarkı; raf kayarken döner (yerel x = yüz normali)' });
  const w = new KB(m, 'eeu', { views: [[-1, 0, 0], [-0.6, 0.8, 0], [-0.6, -0.8, 0], [-0.6, 0, 0.8], [-0.6, 0, -0.8]], tol: 0.05 });
  const X = pv[0] - 0.016, R0 = 0.115;
  w.ngon([pv[0] - 0.032, 0.30, pv[2]], [-1, 0, 0], [0, 0, 1], [0, 1, 0], 0.032, 0.032, 8, 'B');                 // hub
  for (let k = 0; k < 3; k++) { const a = (90 + k * 120) * D2R; const cy = 0.30 + Math.sin(a) * R0 / 2, cz = pv[2] + Math.cos(a) * R0 / 2;
    w.geom(gBox(0.026, R0, 0.026, 0), 'B', [X, cy, cz], [k * 120, 0, 0], { flat: true }); }                    // arms
  for (let k = 0; k < 8; k++) { const a0 = (k + 0.5) / 8 * Math.PI * 2, a1 = (k + 1.5) / 8 * Math.PI * 2;            // rim (8-sided ring, square section)
    const P = (a, r, x) => [x, 0.30 + Math.sin(a) * r, pv[2] + Math.cos(a) * r];
    const ri = R0 - 0.013, ro = R0 + 0.013, xf = X - 0.013, xb = X + 0.013;
    w.poly([P(a0, ri, xf), P(a1, ri, xf), P(a1, ro, xf), P(a0, ro, xf)], 'B', [-1, 0, 0]);
    const am = (a0 + a1) / 2, d = [0, Math.sin(am), Math.cos(am)];
    w.poly([P(a0, ro, xf), P(a1, ro, xf), P(a1, ro, xb), P(a0, ro, xb)], 'B', d);
    w.poly([P(a0, ri, xf), P(a1, ri, xf), P(a1, ri, xb), P(a0, ri, xb)], 'B', [0, -d[1], -d[2]]); }
  w.finish('Wheel');
  return m;
}

// ---------------------------------------------------------------- registry
export const MODELS = {
  WallBlock: { build: WallBlock },
  BoardBase: { build: BoardBase },
  Tile: { build: Tile },
};
MODELS.SlidingShelf = { build: SlidingShelf };
for (const id of KITS) {
  MODELS[`Relief_${id}`] = { build: () => relief(id) };
  MODELS[`ReliefIso_${id}`] = { build: () => isoModel(id) };
  MODELS[`Bridge_${id}`] = { build: () => bridgeModel(id) };
  MODELS[`Rubble_${id}`] = { build: () => rubbleModel(id) };
}
