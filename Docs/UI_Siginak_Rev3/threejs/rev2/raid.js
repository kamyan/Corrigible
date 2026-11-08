// Raid board (8x8) — ozalit paper on a plywood base; pieces per raid_tokens.json.
// three coords: x = column u (A..H), z = row index v (0 = row 8 ... 7 = row 1), y up. 1 unit = 1 tile.
import * as THREE from 'three';
import { canvas, tex, noise } from './lib.js';

const C = {
  felt: '#1C372C', ozalit: '#D3DBD6', line: '#2C4C8E', plyTop: '#C9A36B', plyW: '#B38B57', plyS: '#93703F', ply: '#6E522C',
  chipTop: '#A79E8D', chipW: '#8C8475', chipS: '#6E675B', chipEdge: '#4A443B',
  civTop: '#ECE6D6', civW: '#D5CDBB', civS: '#BAB19E', blue: '#1A4789', cool: '#9DB2B1', grille: '#3E4A4A', warn: '#F1C24B',
  copyTop: '#6E2D25', copyW: '#58241E', copyS: '#451B17', mirror: '#C8CED0',
  nodeTop: '#3B3734', nodeW: '#2C2826', nodeS: '#201D1B', cream: '#F1E9D6',
  gun: '#5B5E62', gunW: '#46494D', gunS: '#34373A', gunD: '#26282B', mTop: '#686B70', accent: '#9A1E14', lens: '#B8241A', copper: '#B8682C',
  pawn: '#C9A77A', kraft: '#B89D74', base: '#1E1B16', red: '#B8241A', hazard: '#A9520A', steel: '#9A9C98',
};
const sm = (hex, r = 0.8, m = 0, o = {}) => new THREE.MeshStandardMaterial({ color: hex, roughness: r, metalness: m, ...o });
function box(p, w, h, d, mats, x, y, z, o = {}) { const g = new THREE.BoxGeometry(w, h, d); const me = new THREE.Mesh(g, mats); me.position.set(x, y, z); me.castShadow = o.cast !== false; me.receiveShadow = true; if (o.ry) me.rotation.y = o.ry; if (o.rx) me.rotation.x = o.rx; if (o.rz) me.rotation.z = o.rz; p.add(me); return me; }
function cyl(p, r, h, m, x, y, z, o = {}) { const me = new THREE.Mesh(new THREE.CylinderGeometry(o.r2 ?? r, r, h, o.seg ?? 24, 1, !!o.open), m); me.position.set(x, y, z); me.castShadow = o.cast !== false; me.receiveShadow = true; if (o.rx) me.rotation.x = o.rx; if (o.rz) me.rotation.z = o.rz; if (o.ry) me.rotation.y = o.ry; p.add(me); return me; }
// face order for BoxGeometry: +x, -x, +y, -y, +z, -z.  Camera sees top, west (-x? no: screen-left-bottom) and south.
// Camera looks toward +x(u) and -z(v)?  Screen-left-bottom face = the face pointing to -u? We use: west = -x face... see tri() below.
function tri(top, west, south) { // visible faces: top (+y), west (-x side faces camera-left), south (+z side faces camera-right/down)
  return [sm(west), sm(west), sm(top), sm(top), sm(south), sm(south)];
}

// ---------- ozalit paper texture: grid, double frame, notches, row/column labels
function ozalitTex() {
  const N = 2048, M = 0.25, S = 8 + 2 * M, ppu = N / S; const [c, x] = canvas(N, N);
  x.fillStyle = C.ozalit; x.fillRect(0, 0, N, N); noise(x, N, N, 6);
  const P = (u) => (u + M) * ppu;
  x.strokeStyle = C.line; x.globalAlpha = 0.42; x.lineWidth = 2.2;
  for (let i = 0; i <= 8; i++) { x.beginPath(); x.moveTo(P(i), P(0)); x.lineTo(P(i), P(8)); x.stroke(); x.beginPath(); x.moveTo(P(0), P(i)); x.lineTo(P(8), P(i)); x.stroke(); }
  x.globalAlpha = 0.85; x.lineWidth = 4; x.strokeRect(P(-0.06), P(-0.06), 8.12 * ppu, 8.12 * ppu); x.lineWidth = 2; x.strokeRect(P(-0.13), P(-0.13), 8.26 * ppu, 8.26 * ppu);
  // notches
  x.lineWidth = 2; for (let i = 0; i <= 16; i++) { const t = i * 0.5; const l = i % 2 ? 0.05 : 0.09; x.beginPath(); x.moveTo(P(t), P(-0.13)); x.lineTo(P(t), P(-0.13 - l)); x.moveTo(P(t), P(8.13)); x.lineTo(P(t), P(8.13 + l)); x.moveTo(P(-0.13), P(t)); x.lineTo(P(-0.13 - l), P(t)); x.moveTo(P(8.13), P(t)); x.lineTo(P(8.13 + l), P(t)); x.stroke(); }
  x.globalAlpha = 1;
  // labels: rows (8..1) along the u = 0 edge; columns (A..H) along the v = 8 edge
  x.fillStyle = C.line; x.font = `italic 700 ${Math.round(ppu * 0.3)}px "Courier Prime"`; x.textAlign = 'center'; x.textBaseline = 'middle';
  for (let v = 0; v < 8; v++) { x.save(); x.translate(P(-0.19), P(v + 0.5)); x.rotate(-Math.PI / 2); x.fillText(String(8 - v), 0, 0); x.restore(); }
  for (let u = 0; u < 8; u++) { x.save(); x.translate(P(u + 0.5), P(8.19)); x.fillText('ABCDEFGH'[u], 0, 0); x.restore(); }
  const t = tex(c); t.anisotropy = 16; return t;
}
function plyTex(w, h) { const [c, x] = canvas(512, 64); x.fillStyle = C.plyW; x.fillRect(0, 0, 512, 64); for (let i = 0; i < 7; i++) { x.fillStyle = i % 2 ? '#a17c4a' : '#8a6738'; x.fillRect(0, i * 9 + 2, 512, 4); } noise(x, 512, 64, 10); return tex(c); }
function hatchTex(col, alpha, dir, fillA, edge) {
  const [c, x] = canvas(256, 256); x.clearRect(0, 0, 256, 256);
  x.fillStyle = col; x.globalAlpha = fillA; x.fillRect(0, 0, 256, 256);
  x.globalAlpha = alpha; x.strokeStyle = col; x.lineWidth = 5;
  for (let i = -256; i < 512; i += 28) { x.beginPath(); if (dir > 0) { x.moveTo(i, 256); x.lineTo(i + 256, 0); } else { x.moveTo(i, 0); x.lineTo(i + 256, 256); } x.stroke(); }
  if (edge) { x.globalAlpha = 0.9; x.lineWidth = edge; x.setLineDash(edge > 5 ? [] : []); x.strokeRect(edge / 2 + 8, edge / 2 + 8, 256 - edge - 16, 256 - edge - 16); }
  return tex(c);
}
function frameTex(col, outer, inner) { const [c, x] = canvas(256, 256); x.clearRect(0, 0, 256, 256); x.strokeStyle = col; x.globalAlpha = 0.95; x.lineWidth = outer; x.strokeRect(14, 14, 228, 228); x.lineWidth = inner; x.strokeRect(34, 34, 188, 188); x.globalAlpha = 0.1; x.fillStyle = col; x.fillRect(14, 14, 228, 228); return tex(c); }

export function buildBoard(scene, o = {}) {
  const root = new THREE.Group(); scene.add(root);
  const A = { pieces: [] };
  // felt
  const [fc, fx] = canvas(512, 512); fx.fillStyle = C.felt; fx.fillRect(0, 0, 512, 512); noise(fx, 512, 512, 18);
  const ft = tex(fc, { repeat: [12, 12] });
  const felt = new THREE.Mesh(new THREE.PlaneGeometry(60, 60), sm('#ffffff', 1, 0, { map: ft })); felt.rotation.x = -Math.PI / 2; felt.position.set(4, -0.26, 4); felt.receiveShadow = true; root.add(felt);
  // plywood base 8.6 x 0.25 x 8.6
  const pt = plyTex(); const plyM = sm('#ffffff', 0.85, 0, { map: pt });
  box(root, 8.6, 0.25, 8.6, [plyM, plyM, sm(C.plyTop, 0.85), sm(C.plyTop), plyM, plyM], 4, -0.125, 4);
  // ozalit paper
  const paper = new THREE.Mesh(new THREE.PlaneGeometry(8.5, 8.5), sm('#ffffff', 0.9, 0, { map: ozalitTex() })); paper.rotation.x = -Math.PI / 2; paper.position.set(4, 0.001, 4); paper.receiveShadow = true; root.add(paper);
  A.root = root;
  return A;
}

// ---------- tile marks (acetate quads on the paper)
export function mark(root, kind, u, v) {
  let m;
  if (kind === 'move') m = new THREE.MeshBasicMaterial({ map: hatchTex('#1A4789', 0.34, 1, 0.16, 4), transparent: true, depthWrite: false });
  if (kind === 'hit') m = new THREE.MeshBasicMaterial({ map: hatchTex('#9A1E14', 0.5, -1, 0.12, 6), transparent: true, depthWrite: false });
  if (kind === 'sel') m = new THREE.MeshBasicMaterial({ map: frameTex('#1A4789', 10, 4), transparent: true, depthWrite: false });
  if (kind === 'target') m = new THREE.MeshBasicMaterial({ map: frameTex('#24592E', 8, 3), transparent: true, depthWrite: false });
  const q = new THREE.Mesh(new THREE.PlaneGeometry(0.98, 0.98), m); q.rotation.x = -Math.PI / 2; q.position.set(u + 0.5, 0.004, v + 0.5); q.renderOrder = 2; root.add(q);
  return q;
}
export function wake(root, u, v) {
  const g = new THREE.Group(); g.position.set(u + 0.5, 0.006, v + 0.5); root.add(g);
  const ringM = new THREE.MeshBasicMaterial({ color: C.accent, transparent: true, opacity: 0.95, depthWrite: false });
  const ring = new THREE.Mesh(new THREE.RingGeometry(0.31, 0.345, 48), ringM); ring.rotation.x = -Math.PI / 2; g.add(ring);
  const fill = new THREE.Mesh(new THREE.CircleGeometry(0.31, 48), new THREE.MeshBasicMaterial({ color: C.accent, transparent: true, opacity: 0.12, depthWrite: false })); fill.rotation.x = -Math.PI / 2; g.add(fill);
  for (let i = 0; i < 8; i++) { const a = i / 8 * Math.PI * 2; const r = new THREE.Mesh(new THREE.PlaneGeometry(0.09, 0.025), ringM); r.rotation.x = -Math.PI / 2; r.rotation.z = -a; r.position.set(Math.cos(a) * 0.425, 0.001, Math.sin(a) * 0.425); g.add(r); }
  cyl(g, 0.006, 0.22, sm('#7E7C78', 0.4, 0.6), 0, 0.11, 0); const hd = new THREE.Mesh(new THREE.SphereGeometry(0.04, 16, 12), sm(C.red, 0.35)); hd.position.y = 0.23; hd.castShadow = true; g.add(hd);
  return g;
}

// 1.2 · clone intent preview: the wake ring's shape, dashed, in warning brown (#854005, no damage); no pin yet.
// When the clone resolves, wake() (red, solid, with pin) takes its place.
export function ghostWake(root, u, v) {
  const g = new THREE.Group(); g.position.set(u + 0.5, 0.006, v + 0.5); root.add(g);
  const col = '#854005', m = new THREE.MeshBasicMaterial({ color: col, transparent: true, opacity: 0.95, depthWrite: false });
  const N = 12, dash = (Math.PI * 2 / N) * 0.6; // 12 dashes, 60 % ink
  for (let i = 0; i < N; i++) { const r = new THREE.Mesh(new THREE.RingGeometry(0.31, 0.345, 8, 1, i * Math.PI * 2 / N, dash), m); r.rotation.x = -Math.PI / 2; g.add(r); }
  const fill = new THREE.Mesh(new THREE.CircleGeometry(0.31, 48), new THREE.MeshBasicMaterial({ color: col, transparent: true, opacity: 0.1, depthWrite: false })); fill.rotation.x = -Math.PI / 2; g.add(fill);
  for (let i = 0; i < 8; i++) { const a = i / 8 * Math.PI * 2; const r = new THREE.Mesh(new THREE.PlaneGeometry(0.09, 0.025), m); r.rotation.x = -Math.PI / 2; r.rotation.z = -a; r.position.set(Math.cos(a) * 0.425, 0.001, Math.sin(a) * 0.425); g.add(r); }
  return g;
}

// ---------- pieces
export function wall(root, u, v, h = 0.55) {
  const g = new THREE.Group(); g.position.set(u + 0.5, 0, v + 0.5); root.add(g);
  const layers = 2; const lh = h / layers;
  for (let i = 0; i < layers; i++) box(g, 0.94 - i * 0.02, lh - 0.006, 0.94 - i * 0.02, tri(C.chipTop, C.chipW, C.chipS), 0, i * lh + lh / 2, 0);
  return { g, h, kind: 'wall' };
}
export function civil(root, u, v) {
  const g = new THREE.Group(); g.position.set(u + 0.5, 0, v + 0.5); root.add(g);
  box(g, 0.64, 0.66, 0.64, tri(C.civTop, C.civW, C.civS), 0, 0.33, 0);
  box(g, 0.645, 0.06, 0.645, sm(C.blue, 0.6), 0, 0.52, 0, { cast: false });
  for (let i = 0; i < 5; i++) box(g, 0.4, 0.018, 0.005, sm('#3a3a36', 0.8), 0, 0.14 + i * 0.05, 0.322, { cast: false });
  const top = new THREE.Mesh(new THREE.RingGeometry(0.1, 0.13, 32), sm(C.blue, 0.6)); top.rotation.x = -Math.PI / 2; top.position.y = 0.662; g.add(top);
  box(g, 0.16, 0.003, 0.03, sm(C.blue, 0.6), 0, 0.663, 0, { cast: false }); box(g, 0.03, 0.003, 0.16, sm(C.blue, 0.6), 0, 0.663, 0, { cast: false });
  return { g, h: 0.66, kind: 'civil' };
}
export function cooling(root, u, v) {
  const g = new THREE.Group(); g.position.set(u + 0.5, 0, v + 0.5); root.add(g);
  cyl(g, 0.33, 0.42, sm(C.cool, 0.7), 0, 0.21, 0, { seg: 16 });
  cyl(g, 0.27, 0.01, sm(C.grille, 0.8), 0, 0.424, 0, { seg: 16, cast: false });
  for (let i = 0; i < 3; i++) box(g, 0.24, 0.006, 0.07, sm('#9aa5a5', 0.6, 0.3), 0, 0.43, 0, { ry: i * Math.PI / 3 + 0.3, cast: false });
  box(g, 0.13, 0.11, 0.004, sm(C.warn, 0.7), -0.2, 0.2, 0.26, { ry: -Math.PI / 4, cast: false });
  return { g, h: 0.42, kind: 'cooling' };
}
export function channel(root, u, v) {
  const g = new THREE.Group(); g.position.set(u + 0.5, 0, v + 0.5); root.add(g);
  box(g, 0.6, 0.012, 1.0, sm('#2A2622', 0.9), 0, 0.006, 0, { cast: false });
  for (const sx of [-0.31, 0.31]) box(g, 0.02, 0.016, 1.0, sm('#4A433B', 0.9), sx, 0.008, 0, { cast: false });
  for (const sx of [-0.12, 0, 0.12]) cyl(g, 0.022, 1.0, sm(C.copper, 0.45, 0.6), sx, 0.022, 0, { rx: Math.PI / 2, seg: 10 });
  for (const sx of [-0.36, 0.36]) box(g, 0.025, 0.002, 1.0, sm(C.hazard, 0.7), sx, 0.002, 0, { cast: false });
  return { g, h: 0.03, kind: 'channel' };
}
export function copyCab(root, u, v) {
  const g = new THREE.Group(); g.position.set(u + 0.5, 0, v + 0.5); root.add(g);
  box(g, 0.72, 0.8, 0.72, tri(C.copyTop, C.copyW, C.copyS), 0, 0.4, 0);
  box(g, 0.4, 0.5, 0.01, sm(C.mirror, 0.15, 0.6), 0.0, 0.42, 0.361, { cast: false });
  box(g, 0.26, 0.004, 0.18, sm(C.cream, 0.7), -0.05, 0.802, -0.05, { cast: false }); box(g, 0.26, 0.004, 0.18, sm(C.cream, 0.7), 0.05, 0.806, 0.05, { cast: false });
  return { g, h: 0.8, kind: 'copy' };
}
export function targetCase(root, u, v, opt = {}) {
  const g = new THREE.Group(); g.position.set(u + 0.5, 0, v + 0.5); root.add(g);
  box(g, 0.8, 0.9, 0.8, tri(C.nodeTop, C.nodeW, C.nodeS), 0, 0.45, 0);
  for (const [sx, sz] of [[-0.39, -0.39], [0.39, -0.39], [-0.39, 0.39], [0.39, 0.39]]) box(g, 0.06, 0.9, 0.06, sm('#7F817E', 0.4, 0.7), sx, 0.45, sz);
  box(g, 0.46, 0.14, 0.01, sm(C.cream, 0.8), 0, 0.62, 0.402, { cast: false });
  box(g, 0.3, 0.004, 0.3, sm(C.red, 0.6), 0, 0.903, 0, { cast: false });
  if (opt.dented) box(g, 0.25, 0.2, 0.02, sm('#1a1817', 0.9), 0.15, 0.3, 0.401, { cast: false });
  return { g, h: 0.9, kind: 'target' };
}
function robotBase(g, r = 0.31) { cyl(g, r, 0.05, sm(C.gunD, 0.6, 0.3), 0, 0.025, 0, { seg: 32 }); const ring = new THREE.Mesh(new THREE.TorusGeometry(r - 0.01, 0.008, 6, 40), sm(C.accent, 0.5)); ring.rotation.x = Math.PI / 2; ring.position.y = 0.05; g.add(ring); }
export function drone(root, u, v, yaw = 0) {
  const g = new THREE.Group(); g.position.set(u + 0.5, 0, v + 0.5); g.rotation.y = yaw; root.add(g);
  robotBase(g, 0.24);
  cyl(g, 0.025, 0.36, new THREE.MeshStandardMaterial({ color: '#DCE6EA', transparent: true, opacity: 0.55, roughness: 0.1 }), 0, 0.23, 0, { seg: 12 });
  cyl(g, 0.2, 0.09, sm(C.gun, 0.5, 0.4), 0, 0.445, 0, { seg: 24 });
  for (let i = 0; i < 4; i++) { const a = Math.PI / 4 + i * Math.PI / 2; box(g, 0.34, 0.02, 0.04, sm(C.gunS, 0.5, 0.4), Math.cos(a) * 0.17, 0.48, Math.sin(a) * 0.17, { ry: -a });
    cyl(g, 0.1, 0.012, sm(C.gunD, 0.5, 0.3), Math.cos(a) * 0.3, 0.5, Math.sin(a) * 0.3, { seg: 20 }); }
  const lens = new THREE.Mesh(new THREE.SphereGeometry(0.045, 14, 10), sm(C.lens, 0.3)); lens.position.set(0.12, 0.42, 0.12); g.add(lens);
  return { g, h: 0.52, kind: 'drone' };
}
function tracks(g) { for (const sz of [-0.17, 0.17]) { box(g, 0.54, 0.12, 0.12, sm('#2a2c2f', 0.8, 0.2), 0, 0.11, sz); for (let i = 0; i < 4; i++) cyl(g, 0.045, 0.13, sm('#4a4c50', 0.6, 0.4), -0.2 + i * 0.133, 0.11, sz, { rx: Math.PI / 2, seg: 12, cast: false }); } }
export function maint(root, u, v, yaw = 0) {
  const g = new THREE.Group(); g.position.set(u + 0.5, 0, v + 0.5); g.rotation.y = yaw; root.add(g);
  robotBase(g); tracks(g);
  box(g, 0.44, 0.33, 0.38, tri(C.mTop, C.gunW, C.gunS), 0, 0.335, 0);
  box(g, 0.22, 0.13, 0.19, tri(C.gun, C.gunW, C.gunS), 0.05, 0.565, 0);
  const lens = new THREE.Mesh(new THREE.SphereGeometry(0.03, 12, 10), sm(C.lens, 0.3)); lens.position.set(0.16, 0.57, 0); g.add(lens);
  cyl(g, 0.008, 0.14, sm('#7E7C78', 0.4, 0.6), -0.03, 0.70, 0.04); const tip = new THREE.Mesh(new THREE.SphereGeometry(0.02, 10, 8), sm(C.red, 0.4)); tip.position.set(-0.03, 0.77, 0.04); g.add(tip);
  // wrench arm
  box(g, 0.22, 0.04, 0.04, sm('#7F817E', 0.4, 0.6), 0.28, 0.4, 0.14, { rz: -0.3 }); box(g, 0.05, 0.1, 0.05, sm('#7F817E', 0.4, 0.6), 0.39, 0.34, 0.14);
  return { g, h: 0.77, kind: 'maint' };
}
export function locker(root, u, v, yaw = 0) {
  const g = new THREE.Group(); g.position.set(u + 0.5, 0, v + 0.5); g.rotation.y = yaw; root.add(g);
  robotBase(g); tracks(g);
  box(g, 0.34, 0.5, 0.34, tri(C.mTop, C.gunW, C.gunS), -0.05, 0.42, 0);
  for (let i = 0; i < 4; i++) box(g, 0.345, 0.04, 0.345, sm(i % 2 ? C.gunD : C.hazard, 0.6), -0.05, 0.25 + i * 0.04, 0, { cast: false });
  // clamp jaws (C-shaped) forward (+x local)
  for (const sz of [-1, 1]) { const j = new THREE.Group(); j.position.set(0.14, 0.42, sz * 0.13); g.add(j);
    box(j, 0.26, 0.07, 0.06, sm('#7F817E', 0.4, 0.6), 0.12, 0, 0);
    box(j, 0.06, 0.07, 0.14, sm('#7F817E', 0.4, 0.6), 0.25, 0, -sz * 0.05);
    box(j, 0.05, 0.075, 0.05, sm(C.accent, 0.5), 0.26, 0, -sz * 0.11); }
  const lens = new THREE.Mesh(new THREE.SphereGeometry(0.035, 12, 10), sm(C.lens, 0.3)); lens.position.set(0.12, 0.62, 0); g.add(lens);
  return { g, h: 0.7, kind: 'locker' };
}
export function mortar(root, u, v, yaw = 0) {
  const g = new THREE.Group(); g.position.set(u + 0.5, 0, v + 0.5); g.rotation.y = yaw; root.add(g);
  robotBase(g); tracks(g);
  box(g, 0.46, 0.18, 0.4, tri(C.mTop, C.gunW, C.gunS), 0, 0.26, 0);
  const t = cyl(g, 0.075, 0.55, sm(C.gun, 0.5, 0.4), 0.06, 0.55, 0, { rz: -0.6, seg: 18 });
  const ring = new THREE.Mesh(new THREE.TorusGeometry(0.075, 0.016, 8, 20), sm(C.accent, 0.5)); ring.position.set(0.22, 0.78, 0); ring.rotation.y = Math.PI / 2; ring.rotation.x = 0.6; g.add(ring);
  cyl(g, 0.05, 0.12, sm('#7F817E', 0.4, 0.6), -0.12, 0.41, 0.12);
  return { g, h: 0.82, kind: 'mortar' };
}
export function shieldDrone(root, u, v, yaw = 0) {
  const d = drone(root, u, v, yaw);
  const sh = new THREE.Mesh(new THREE.CylinderGeometry(0.34, 0.34, 0.34, 24, 1, true, -0.9, 1.8), sm(C.steel, 0.35, 0.6, { side: THREE.DoubleSide })); sh.position.set(0, 0.42, 0); d.g.add(sh);
  const rim = new THREE.Mesh(new THREE.TorusGeometry(0.34, 0.012, 6, 24, 1.8), sm(C.accent, 0.5)); rim.rotation.x = Math.PI / 2; rim.rotation.z = -0.9 - Math.PI / 2; rim.position.set(0, 0.59, 0); d.g.add(rim);
  d.kind = 'shieldDrone'; d.h = 0.6; return d;
}
// ---------- 1.2: three more machine types (same body colours; type reads from shape; red only on eye and base ring)
const orient = (mesh, dir) => { mesh.quaternion.setFromUnitVectors(new THREE.Vector3(0, 1, 0), dir.clone().normalize()); };
// Zırhlı dron: drone family (base, acrylic rod, round hull, four rotors) made heavy: octagonal hull with a steel
// armour band and front plate, thicker arms, rotors inside ducts (guard rings). Steel = armour, as on plates and shields.
export function armorDrone(root, u, v, yaw = 0) {
  const g = new THREE.Group(); g.position.set(u + 0.5, 0, v + 0.5); g.rotation.y = yaw; root.add(g);
  robotBase(g, 0.26);
  cyl(g, 0.034, 0.32, new THREE.MeshStandardMaterial({ color: '#DCE6EA', transparent: true, opacity: 0.55, roughness: 0.1 }), 0, 0.21, 0, { seg: 12 });
  cyl(g, 0.205, 0.11, sm(C.gun, 0.5, 0.4), 0, 0.435, 0, { seg: 8, ry: Math.PI / 8 });                 // hull 0.38–0.49
  cyl(g, 0.222, 0.05, sm(C.steel, 0.35, 0.7), 0, 0.405, 0, { seg: 8, ry: Math.PI / 8 });              // armour band
  box(g, 0.045, 0.13, 0.24, sm(C.steel, 0.35, 0.7), 0.222, 0.44, 0, { rz: -0.32 });                   // front plate (+x)
  const lens = new THREE.Mesh(new THREE.SphereGeometry(0.042, 14, 10), sm(C.lens, 0.3)); lens.position.set(0.2, 0.355, 0); g.add(lens);
  const duct = sm(C.gunW, 0.5, 0.4, { side: THREE.DoubleSide });
  for (let i = 0; i < 4; i++) { const a = Math.PI / 4 + i * Math.PI / 2, cx = Math.cos(a) * 0.31, cz = Math.sin(a) * 0.31;
    box(g, 0.34, 0.035, 0.065, sm(C.gunS, 0.5, 0.4), Math.cos(a) * 0.17, 0.475, Math.sin(a) * 0.17, { ry: -a });
    cyl(g, 0.118, 0.065, duct, cx, 0.49, cz, { seg: 28, open: true });                                    // duct 0.457–0.523
    const lip = new THREE.Mesh(new THREE.TorusGeometry(0.118, 0.012, 6, 28), sm(C.gun, 0.5, 0.4)); lip.rotation.x = Math.PI / 2; lip.position.set(cx, 0.523, cz); lip.castShadow = true; g.add(lip);
    cyl(g, 0.098, 0.012, sm(C.gunD, 0.5, 0.3), cx, 0.49, cz, { seg: 20 });
    cyl(g, 0.022, 0.03, sm(C.gunS, 0.5, 0.4), cx, 0.49, cz, { seg: 10 }); }
  return { g, h: 0.53, kind: 'armorDrone' };
}
// Patlayan robot: a sea-mine body (sphere with detonator horns) on a short stem; rolls up and goes off.
export function bomber(root, u, v, yaw = 0) {
  const g = new THREE.Group(); g.position.set(u + 0.5, 0, v + 0.5); g.rotation.y = yaw; root.add(g);
  robotBase(g, 0.26);
  cyl(g, 0.085, 0.13, sm(C.gunD, 0.6, 0.3), 0, 0.115, 0, { seg: 16 });                                 // stem 0.05–0.18
  const cy = 0.38, R = 0.2;
  const body = new THREE.Mesh(new THREE.SphereGeometry(R, 32, 22), sm(C.gun, 0.45, 0.4)); body.position.set(0, cy, 0); body.castShadow = true; body.receiveShadow = true; g.add(body);
  const belt = new THREE.Mesh(new THREE.TorusGeometry(R + 0.004, 0.016, 8, 40), sm(C.gunS, 0.5, 0.4)); belt.rotation.x = Math.PI / 2; belt.position.y = cy; g.add(belt);
  const hornM = sm('#7F817E', 0.4, 0.6), capM = sm(C.gunD, 0.5, 0.4);
  const horns = [new THREE.Vector3(0, 1, 0)];
  for (let i = 0; i < 6; i++) { const a = i * Math.PI / 3 + Math.PI / 6, e = 0.42; horns.push(new THREE.Vector3(Math.cos(a) * Math.cos(e), Math.sin(e), Math.sin(a) * Math.cos(e))); }
  for (const d of horns) { const hm = new THREE.Mesh(new THREE.CylinderGeometry(0.02, 0.026, 0.07, 10), hornM); orient(hm, d); hm.position.set(0, cy, 0).addScaledVector(d, R + 0.025); hm.castShadow = true; g.add(hm);
    const cap = new THREE.Mesh(new THREE.SphereGeometry(0.024, 10, 8), capM); cap.position.set(0, cy, 0).addScaledVector(d, R + 0.055); g.add(cap); }
  const lens = new THREE.Mesh(new THREE.SphereGeometry(0.04, 14, 10), sm(C.lens, 0.3)); lens.position.set(R * 0.93, cy - 0.06, 0); g.add(lens);
  return { g, h: 0.66, kind: 'bomber' };
}
// Çoğaltıcı: tracked hangar body with a dark bay and ramp at the front; a small drone waits on the pad on top.
export function replicator(root, u, v, yaw = 0) {
  const g = new THREE.Group(); g.position.set(u + 0.5, 0, v + 0.5); g.rotation.y = yaw; root.add(g);
  robotBase(g); tracks(g);
  box(g, 0.46, 0.28, 0.42, tri(C.mTop, C.gunW, C.gunS), -0.02, 0.31, 0);                               // hangar 0.17–0.45
  box(g, 0.02, 0.17, 0.25, sm(C.gunD, 0.8), 0.214, 0.285, 0, { cast: false });                          // bay (front, +x)
  box(g, 0.13, 0.016, 0.23, sm(C.gunS, 0.5, 0.4), 0.27, 0.205, 0, { rz: 0.55 });                        // ramp
  const lens = new THREE.Mesh(new THREE.SphereGeometry(0.032, 12, 10), sm(C.lens, 0.3)); lens.position.set(0.215, 0.405, 0.13); g.add(lens);
  cyl(g, 0.16, 0.022, sm(C.gunD, 0.6, 0.3), -0.02, 0.461, 0, { seg: 32 });                              // landing pad
  const ring = new THREE.Mesh(new THREE.TorusGeometry(0.12, 0.008, 6, 32), sm('#7F817E', 0.4, 0.6)); ring.rotation.x = Math.PI / 2; ring.position.set(-0.02, 0.473, 0); g.add(ring);
  // the drone it is about to send out (same parts as drone(), at 0.45 scale; no lens — the eye stays the machine's)
  const m = new THREE.Group(); m.position.set(-0.02, 0.473, 0); g.add(m);
  cyl(m, 0.012, 0.03, sm('#7E7C78', 0.4, 0.6), 0, 0.015, 0, { seg: 8 });
  cyl(m, 0.08, 0.04, sm(C.gun, 0.5, 0.4), 0, 0.05, 0, { seg: 20 });
  for (let i = 0; i < 4; i++) { const a = Math.PI / 4 + i * Math.PI / 2; box(m, 0.15, 0.012, 0.02, sm(C.gunS, 0.5, 0.4), Math.cos(a) * 0.075, 0.066, Math.sin(a) * 0.075, { ry: -a });
    cyl(m, 0.045, 0.008, sm(C.gunD, 0.5, 0.3), Math.cos(a) * 0.135, 0.074, Math.sin(a) * 0.135, { seg: 16 }); }
  return { g, h: 0.55, kind: 'replicator' };
}
// specialist pawn: thick wooden letter on a black base with a cream ring
let FONT = null;
export function setFont(f) { FONT = f; }
export function pawn(root, u, v, letter, opt = {}, TextGeometry) {
  const g = new THREE.Group(); g.position.set(u + 0.5, 0, v + 0.5); root.add(g);
  cyl(g, 0.27, 0.07, sm(C.base, 0.5), 0, 0.035, 0, { seg: 40 });
  const ring = new THREE.Mesh(new THREE.TorusGeometry(0.255, 0.012, 6, 48), sm(C.cream, 0.6)); ring.rotation.x = Math.PI / 2; ring.position.y = 0.072; g.add(ring);
  const geo = new TextGeometry(letter, { font: FONT, size: 0.5, height: 0.13, curveSegments: 6, bevelEnabled: true, bevelThickness: 0.012, bevelSize: 0.01, bevelSegments: 2 });
  geo.computeBoundingBox(); const bb = geo.boundingBox; geo.translate(-(bb.max.x + bb.min.x) / 2, 0, -(bb.max.z + bb.min.z) / 2);
  const done = opt.state === 'done', down = opt.state === 'down';
  const lm = sm(done ? '#8C734E' : C.pawn, 0.75);
  const L = new THREE.Mesh(geo, lm); L.castShadow = true; L.receiveShadow = true;
  const holder = new THREE.Group(); holder.position.y = 0.075; holder.rotation.y = -Math.PI / 4 + (done ? Math.PI : 0); g.add(holder); holder.add(L);
  if (down) { holder.rotation.x = -1.2; holder.position.y = 0.12; }
  if (done) { // tick sticker on the base front (faces the camera)
    const [cc, cx] = (() => { const c = document.createElement('canvas'); c.width = c.height = 128; return [c, c.getContext('2d')]; })();
    cx.fillStyle = '#F1E9D6'; cx.beginPath(); cx.arc(64, 64, 60, 0, 7); cx.fill(); cx.strokeStyle = '#24592E'; cx.lineWidth = 16; cx.lineCap = 'round'; cx.lineJoin = 'round'; cx.beginPath(); cx.moveTo(34, 66); cx.lineTo(56, 88); cx.lineTo(96, 40); cx.stroke();
    const t = new THREE.CanvasTexture(cc); t.colorSpace = THREE.SRGBColorSpace;
    const st = new THREE.Mesh(new THREE.PlaneGeometry(0.2, 0.2), new THREE.MeshBasicMaterial({ map: t, transparent: true })); st.position.set(-0.19, 0.16, 0.19); st.rotation.y = -Math.PI / 4; g.add(st); }
  if (opt.tape) { const t = new THREE.Mesh(new THREE.PlaneGeometry(0.42, 0.08), new THREE.MeshStandardMaterial({ color: '#ECE2BE', transparent: true, opacity: 0.85, roughness: 0.9 })); t.position.set(0, 0.32, 0.076); t.rotation.z = 0.5; holder.add(t); }
  if (opt.plate) { box(g, 0.2, 0.12, 0.02, sm(C.steel, 0.35, 0.8), 0.13, 0.11, 0.13, { ry: -Math.PI / 4 }); }
  return { g, h: down ? 0.25 : 0.62, kind: 'pawn', letter };
}
