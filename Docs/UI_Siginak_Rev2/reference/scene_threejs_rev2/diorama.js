// ★ Section model v2: store band (DEPO) above the rooms, room outputs shown in the model.
import * as THREE from 'three';
import { M, mat, lbox, lcyl, lsphere, lplane } from './lib.js';
import * as T from './textures.js';
import * as X from './tex2.js';

const PAWN = ['#3f5f8a', '#b07a2a', '#7a3a30', '#3e6b4a', '#6b5a8a', '#8a6a3f'];
export function peg(parent, x, y, z, band, opt = {}) {
  const g = new THREE.Group(); g.position.set(x, y, -z); parent.add(g);
  const b = new THREE.Mesh(new THREE.CylinderGeometry(0.0105, 0.012, 0.036, 14), M.woodPale); b.position.y = 0.018; b.castShadow = true; g.add(b);
  const h = new THREE.Mesh(new THREE.SphereGeometry(0.0105, 14, 10), M.woodPale); h.position.y = 0.046; h.castShadow = true; g.add(h);
  const c = new THREE.Mesh(new THREE.CylinderGeometry(0.0112, 0.0118, 0.009, 14), mat(band, 0.6)); c.position.y = 0.026; g.add(c);
  if (opt.head) { const hh = new THREE.Mesh(new THREE.SphereGeometry(0.0108, 14, 10), mat(opt.head, 0.5)); hh.position.y = 0.046; g.add(hh); }
  if (opt.bandage) { const bd = new THREE.Mesh(new THREE.TorusGeometry(0.0106, 0.0035, 6, 16), mat(0xf2efe6, 0.9)); bd.rotation.x = Math.PI / 2; bd.position.y = 0.05; g.add(bd); }
  if (opt.lying) { g.rotation.z = Math.PI / 2; }
  return g;
}
function slot(parent, x, y, z) { const d = new THREE.Mesh(new THREE.CylinderGeometry(0.012, 0.012, 0.002, 18), M.brassDark); d.position.set(x, y + 0.001, -z); d.receiveShadow = true; parent.add(d); }

export function buildDiorama2(g, opts, A) {
  const st = opts.state; const lang = opts.lang || 'tr';
  const W = 0.80, D = 0.34, H = 0.72;
  const L = (w, h, d, m, x, y, z, o) => lbox(g, w, h, d, m, x, y, z, o);
  const surface = opts.topBand === 'surface';
  // frame
  L(0.02, H, D, M.wood, -W / 2 + 0.01, H / 2, 0); L(0.02, H, D, M.wood, W / 2 - 0.01, H / 2, 0);
  L(W, 0.02, D, M.wood, 0, 0.01, 0); L(W, H, 0.01, M.soil3, 0, H / 2, D / 2 - 0.005);
  // fascias (card bands) and slabs
  L(W - 0.04, 0.065, D - 0.02, M.slab, 0, 0.0525, 0);
  L(W - 0.04, 0.065, D - 0.02, M.slab, 0, 0.2875, 0);
  L(W - 0.04, 0.016, D - 0.02, M.slab, 0, 0.498, 0);
  for (const x of [-0.375, 0.375, -0.04, 0.04]) L(0.01, 0.39, D - 0.02, M.slab, x, 0.28, 0);
  for (const x of [-0.022, 0.022]) L(0.004, 0.39, 0.004, M.steelDark, x, 0.28, 0.10);
  for (let y = 0.1; y < 0.47; y += 0.03) L(0.044, 0.003, 0.003, M.steelDark, 0, y, 0.10);
  // ---- top band
  if (!surface) {
    // ★ DEPO: rations crates (1 crate = 2 rations), materials bins (1 bin = 5), plate rack (3 slots)
    lplane(g, W - 0.04, 0.095, mat(0x8a8576, 0.92), 0, 0.553, D / 2 - 0.012);
    L(W - 0.04, 0.004, D - 0.03, mat(0x6e6a5e, 0.9), 0, 0.508, 0, { cast: false });
    L(W - 0.04, 0.014, D - 0.02, M.slab, 0, 0.607, 0);
    const er = st.erzak ?? 22, ma = st.malzeme ?? 19, pl = st.plaka ?? 0;
    const crates = er / 3; // 1 crate = 3 rations; last one shown open/partial
    for (let i = 0; i < 10; i++) { const c = i % 5, r = Math.floor(i / 5); const cx = -0.335 + c * 0.066, cy = 0.51 + r * 0.044 + 0.021;
      if (i < Math.floor(crates)) { L(0.06, 0.042, 0.07, M.wood, cx, cy, 0.03); L(0.062, 0.005, 0.072, M.woodLight, cx, cy + 0.012, 0.03, { cast: false }); L(0.062, 0.005, 0.072, M.woodLight, cx, cy - 0.012, 0.03, { cast: false }); }
      else if (i < Math.ceil(crates)) { L(0.06, 0.03, 0.07, M.wood, cx, cy - 0.006, 0.03); for (let k = 0; k < 2; k++) lcyl(g, 0.009, 0.02, M.galv, cx - 0.012 + k * 0.024, cy + 0.016, 0.03, { seg: 10 }); }
      else lplane(g, 0.056, 0.038, new THREE.MeshBasicMaterial({ color: 0xd9d2bf, transparent: true, opacity: 0.22, depthWrite: false }), cx, cy, 0.0); }
    const bins = Math.ceil(ma / 5);
    for (let i = 0; i < 5; i++) { const bx = 0.01 + i * 0.044; if (i < bins) { L(0.036, 0.045, 0.05, M.galv, bx, 0.531, 0.03); const fill = i === bins - 1 && ma % 5 ? (ma % 5) / 5 : 1; L(0.03, 0.035 * fill, 0.044, mat(0x5a5b54, 0.8, 0.4), bx, 0.51 + 0.035 * fill / 2 + 0.004, 0.03, { cast: false }); } }
    // plate rack
    L(0.11, 0.004, 0.06, M.steelDark, 0.31, 0.512, 0.04); L(0.004, 0.08, 0.06, M.steelDark, 0.255, 0.55, 0.04); L(0.004, 0.08, 0.06, M.steelDark, 0.365, 0.55, 0.04);
    for (let i = 0; i < 3; i++) { const px = 0.275 + i * 0.035; if (i < pl) L(0.006, 0.07, 0.05, mat(0x9a9c98, 0.4, 0.8), px, 0.55, 0.04); else lplane(g, 0.006, 0.07, new THREE.MeshBasicMaterial({ color: 0xe6e2d4, transparent: true, opacity: 0.35 }), px, 0.55, 0.012); }
    { const bl = new THREE.PointLight(0xffd8a0, 0.07 * (opts.dioLight ?? 1), 0.6, 2); bl.position.set(0.0, 0.585, 0.0); g.add(bl);
      lsphere(g, 0.006, new THREE.MeshStandardMaterial({ color: 0xfff0c8, emissive: 0xffd27a, emissiveIntensity: 2.2 }), 0.12, 0.592, 0.02, { cast: false }); }
    // chalk slate on the back wall (text: overlay quad in K4, baked in K1)
    L(0.2, 0.072, 0.006, mat(0x2a2c28, 0.95), 0.11, 0.566, D / 2 - 0.016);
    A.depotSlate = [[0.015, 0.533, D / 2 - 0.02], [0.205, 0.533, D / 2 - 0.02], [0.205, 0.599, D / 2 - 0.02], [0.015, 0.599, D / 2 - 0.02]];
    if (opts.bakeCards) lplane(g, 0.19, 0.066, mat(0xffffff, 0.95, 0, { map: X.paperLines([[lang === 'en' ? 'RATIONS' : 'ERZAK', 54, '#e6e2d4'], [opts.shiftsLeft || '15 vardiya', 66, '#f2ecd8']], 600, 208, { bg: '#2a2c28', top: 72 }) }), 0.11, 0.566, D / 2 - 0.0195);
    // band label
    lplane(g, 0.07, 0.016, new THREE.MeshStandardMaterial({ map: T.plateTex(X.S2[lang].depot, 0.07, 0.016), roughness: 0.4, metalness: 0.75 }), -0.33, 0.6, -D / 2 + 0.009);
    // soil + thin surface strip with ruins and the AI mast
    L(W - 0.04, 0.03, D - 0.02, M.soil2, 0, 0.629, 0, { cast: false });
    L(W - 0.04, 0.012, D - 0.02, M.rubble, 0, 0.65, 0);
    for (const [x, h, w] of [[-0.31, 0.04, 0.06], [-0.2, 0.025, 0.05], [-0.1, 0.05, 0.04], [0.06, 0.03, 0.07], [0.15, 0.018, 0.05], [0.3, 0.035, 0.05]]) L(w, h, 0.05, mat(0x3c3c38, 0.9), x, 0.656 + h / 2, 0.03);
    lcyl(g, 0.0025, 0.07, M.steelDark, 0.24, 0.69, -0.02);
    lsphere(g, 0.006, new THREE.MeshStandardMaterial({ color: 0xb8241a, emissive: 0xb8241a, emissiveIntensity: 2.0 }), 0.24, 0.727, -0.02, { cast: false });
  } else {
    // Revizyon 2 · Karar 3 · B (seçilmedi, TAŞIMA): surface band (ruins, hatch, radio antenna) instead of the store
    L(W - 0.04, 0.03, D - 0.02, M.soil2, 0, 0.52, 0, { cast: false }); L(W - 0.04, 0.03, D - 0.02, M.soil1, 0, 0.55, 0, { cast: false });
    L(W - 0.04, 0.014, D - 0.02, M.rubble, 0, 0.572, 0);
    for (const [x, h, w] of [[-0.33, 0.07, 0.06], [-0.22, 0.04, 0.05], [-0.12, 0.09, 0.04], [0.05, 0.05, 0.07], [0.14, 0.03, 0.05], [0.31, 0.06, 0.05]]) L(w, h, 0.06, mat(0x3c3c38, 0.9), x, 0.579 + h / 2, 0.03 + (x * 7 % 0.05));
    L(0.07, 0.012, 0.06, M.steelDark, -0.02, 0.585, 0.0); lcyl(g, 0.012, 0.016, M.steelDark, -0.02, 0.594, 0.0);
    lcyl(g, 0.0025, 0.12, M.steelDark, 0.24, 0.64, -0.02);
    lsphere(g, 0.007, new THREE.MeshStandardMaterial({ color: 0xb8241a, emissive: 0xb8241a, emissiveIntensity: 2.0 }), 0.24, 0.703, -0.02, { cast: false });
    lcyl(g, 0.002, 0.16, M.chrome, 0.16, 0.66, 0.02);
  }
  // ---- rooms
  const rooms = { yasam: [-0.2075, 0.32], telsiz: [0.2075, 0.32], atolye: [-0.2075, 0.085], revir: [0.2075, 0.085] };
  const roomH = 0.17, roomW = 0.325;
  A.rooms = {};
  for (const [k, [cx, fy]] of Object.entries(rooms)) {
    const r = st.rooms[k];
    lplane(g, roomW, roomH, mat(r.powered ? 0xcfc6ae : 0x5a554b, 0.9), cx, fy + roomH / 2, D / 2 - 0.012);
    L(roomW, 0.004, D - 0.03, mat(r.powered ? 0x77756b : 0x45433d, 0.9), cx, fy + 0.002, 0, { cast: false });
    const on = r.powered && r.level > 0;
    lsphere(g, 0.007, new THREE.MeshStandardMaterial({ color: on ? 0xfff0c8 : 0x3a3a34, emissive: on ? 0xffd27a : 0x000000, emissiveIntensity: on ? 2.5 : 0 }), cx, fy + roomH - 0.012, 0.02, { cast: false });
    if (on) { const pl = new THREE.PointLight(0xffd8a0, 0.06 * (opts.dioLight ?? 1), 0.45, 2); pl.position.set(cx, fy + roomH - 0.03, 0); g.add(pl); }
    L(roomW, 0.004, 0.004, M.brassDark, cx, fy + 0.001, -D / 2 + 0.012, { cast: false });
    A.rooms[k] = {
      opening: [[cx - roomW / 2, fy, -D / 2 + 0.01], [cx + roomW / 2, fy, -D / 2 + 0.01], [cx + roomW / 2, fy + roomH, -D / 2 + 0.01], [cx - roomW / 2, fy + roomH, -D / 2 + 0.01]],
      card: [[cx - 0.115, fy - 0.0625, -D / 2 + 0.005], [cx + 0.115, fy - 0.0625, -D / 2 + 0.005], [cx + 0.115, fy - 0.0055, -D / 2 + 0.005], [cx - 0.115, fy - 0.0055, -D / 2 + 0.005]],
    };
    // card holder (brass), wider card 0.23 x 0.057
    L(0.24, 0.004, 0.006, M.brass, cx, fy - 0.004, -D / 2 + 0.006, { cast: false });
    L(0.24, 0.004, 0.006, M.brass, cx, fy - 0.064, -D / 2 + 0.006, { cast: false });
    for (const sx of [-0.12, 0.12]) L(0.004, 0.062, 0.006, M.brass, cx + sx, fy - 0.034, -D / 2 + 0.006, { cast: false });
    if (opts.bakeCards) lplane(g, 0.23, 0.057, mat(0xffffff, 0.85, 0, { map: X.roomCard(r.name, r.line2, r.stamp) }), cx, fy - 0.034, -D / 2 + 0.003);
    // selected room: yellow frame around the opening
    if (opts.selRoom === k) for (const [w, h, x, y] of [[roomW + 0.012, 0.006, cx, fy + roomH + 0.003], [roomW + 0.012, 0.006, cx, fy - 0.003], [0.006, roomH + 0.012, cx - roomW / 2 - 0.003, fy + roomH / 2], [0.006, roomH + 0.012, cx + roomW / 2 + 0.003, fy + roomH / 2]]) L(w, h, 0.006, new THREE.MeshStandardMaterial({ color: 0xf5d646, emissive: 0xf5d646, emissiveIntensity: 0.35, roughness: 0.6 }), x, y, -D / 2 + 0.008, { cast: false });
    // staff slots + pegs
    const cap = r.cap || 0;
    for (let i = 0; i < cap; i++) {
      const sx = cx - 0.13 + (cap > 1 ? i * (0.26 / Math.max(cap - 1, 1)) : 0.13);
      slot(g, sx, fy + 0.004, -0.09);
      if (i < r.staff) peg(g, sx, fy + 0.006, -0.09, PAWN[i % 6]);
    }
  }
  // ---- room props + ★ outputs
  // yasam: bunks, table, reserve specialist on the lower bunk, food crate being filled
  for (const bx of [-0.33, -0.21]) {
    for (const by of [0.335, 0.385]) L(0.10, 0.012, 0.055, M.blanket, bx, by, 0.09);
    for (const [px, pz] of [[-0.05, 0.065], [0.05, 0.065], [-0.05, 0.115], [0.05, 0.115]]) L(0.004, 0.08, 0.004, M.steelDark, bx + px, 0.36, pz);
  }
  L(0.05, 0.004, 0.04, M.wood, -0.10, 0.355, 0.07); L(0.004, 0.032, 0.004, M.wood, -0.10, 0.338, 0.07);
  if ((st.reserve ?? 1) > 0) peg(g, -0.33, 0.341, 0.075, '#3e6b4a', { head: '#3f5f8a' });
  if (st.rooms.yasam.staff > 0) { L(0.045, 0.022, 0.035, M.wood, -0.13, 0.331, 0.0); for (let i = 0; i < 3; i++) lcyl(g, 0.006, 0.014, M.galv, -0.145 + i * 0.014, 0.346, 0.0, { seg: 10 }); }
  // telsiz: radio table, radio set (dial lit when staffed), antenna cable up through the ceiling
  const radioOn = st.rooms.telsiz.staff > 0;
  L(0.13, 0.006, 0.06, M.wood, 0.20, 0.37, 0.08); for (const px of [0.145, 0.255]) L(0.006, 0.05, 0.05, M.wood, px, 0.345, 0.08);
  L(0.075, 0.042, 0.04, mat(0x4a3020, 0.6), 0.19, 0.394, 0.085);
  lplane(g, 0.05, 0.018, new THREE.MeshStandardMaterial({ color: 0x1a1008, emissive: radioOn ? 0xe9a948 : 0, emissiveIntensity: radioOn ? 2.2 : 0 }), 0.19, 0.402, 0.0645);
  if (radioOn) { const rl = new THREE.PointLight(0xffa850, 0.05 * (opts.dioLight ?? 1), 0.25, 2); rl.position.set(0.19, 0.41, -0.03); g.add(rl);
    const hp = new THREE.Mesh(new THREE.TorusGeometry(0.013, 0.0022, 6, 16, Math.PI), mat(0x1c1c1a, 0.5)); hp.position.set(0.0775 + 0.0, 0.32 + 0.006 + 0.048, 0.09); g.add(hp); }
  // amber lamp above the radio room: lit while someone listens (alarm slows)
  lsphere(g, 0.006, new THREE.MeshStandardMaterial({ color: radioOn ? 0xffb347 : 0x4a2f12, emissive: radioOn ? 0xff9a2a : 0, emissiveIntensity: radioOn ? 2.4 : 0 }), 0.36, 0.475, -0.12, { cast: false });
  lcyl(g, 0.0015, 0.09, M.chrome, 0.215, 0.445, 0.09);
  lcyl(g, 0.002, 0.11, M.rubber, 0.33, 0.43, 0.13);
  L(0.03, 0.05, 0.03, M.steelDark, 0.33, 0.36, 0.12);
  // atölye: bench, pegboard, ★ casting mold (glows while a plate is being cast)
  L(0.17, 0.006, 0.055, M.wood, -0.22, 0.13, 0.10);
  for (const px of [-0.295, -0.145]) L(0.006, 0.045, 0.05, M.wood, px, 0.107, 0.10);
  lplane(g, 0.16, 0.07, mat(0x9a7448, 0.9), -0.22, 0.19, D / 2 - 0.014);
  L(0.02, 0.015, 0.02, M.steelDark, -0.16, 0.14, 0.09);
  L(0.07, 0.012, 0.04, mat(0x2a2622, 0.8), -0.25, 0.139, 0.095);
  const cast = st.casting ?? 0;
  lplane(g, 0.056, 0.026, new THREE.MeshStandardMaterial({ color: cast > 0 ? 0x5a2a10 : 0x1a1714, emissive: cast > 0 ? 0xff7a2a : 0, emissiveIntensity: cast > 0 ? 1.4 : 0 }), -0.25, 0.1455, 0.095, { rx: -Math.PI / 2 });
  // revir: damaged (rubble, beam) or working (cot with a bandaged specialist, cabinet)
  const rv = st.rooms.revir;
  L(0.12, 0.018, 0.05, M.paperWhite, 0.16, 0.105, 0.09, { rz: rv.level > 0 ? 0 : 0.06 });
  L(0.05, 0.09, 0.04, M.paperWhite, 0.32, 0.13, 0.11);
  lplane(g, 0.03, 0.03, mat(0xffffff, 0.6, 0, { map: T.firstAidTex() }), 0.32, 0.14, 0.089);
  if (rv.level > 0 && st.patient) peg(g, 0.16, 0.12, 0.09, '#3f5f8a', { bandage: true, lying: true, head: '#3f5f8a' });
  if (rv.level === 0) {
    [[0.22, 0.095, -0.02, 0.03, 0.02], [0.26, 0.092, 0.02, 0.025, 0.018], [0.12, 0.093, 0.03, 0.02, 0.015], [0.29, 0.094, -0.06, 0.028, 0.02], [0.18, 0.091, -0.07, 0.022, 0.014], [0.24, 0.1, 0.0, 0.018, 0.016]].forEach(([x, y, z, s, h], i) => L(s, h, s * 0.9, M.rubble, x, y, z, { ry: i * 0.7, rz: i * 0.3 }));
    L(0.24, 0.016, 0.016, M.wood, 0.20, 0.16, -0.01, { rz: -0.42 });
    lplane(g, 0.012, 0.12, mat(0x2a2620, 0.9), 0.25, 0.17, D / 2 - 0.0135, { rz: 0.4 });
  }
}
