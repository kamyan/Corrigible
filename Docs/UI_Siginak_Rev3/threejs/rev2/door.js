// ★ Shelter door on the right wall (x = 2.2, faces -x), corridor and visitor silhouettes.
import * as THREE from 'three';
import { U, D2R, M, mat, lbox, lcyl, lsphere, lplane, group } from './lib.js';
import * as T from './textures.js';
import * as X from './tex2.js';

export const DOOR = { x: 2.2, z0: 0.32, z1: 1.18, h: 2.0 };

export function buildDoor(root, o = {}) {
  const A = {};
  const { x: WX, z0, z1, h } = DOOR; const zc = (z0 + z1) / 2, dw = z1 - z0;
  // right wall in three pieces around the opening (inside face, normal -x)
  const sideTex = T.concreteTex(3.0, 2.5, { base: '#737a76', stains: [[0.5, 0.1, 0.25, 0.4]], damp: true, ties: true });
  const wm = mat(0xffffff, 0.96, 0, { map: sideTex });
  const piece = (za, zb, ya, yb, seed = 0) => { const t = T.concreteTex(zb - za, yb - ya, { base: '#737a76', stains: [[0.5, 0.1, 0.25, 0.4]], damp: true, ties: true }); lplane(root, zb - za, yb - ya, mat(0xffffff, 0.96, 0, { map: t }), WX, (ya + yb) / 2, (za + zb) / 2, { ry: -Math.PI / 2 }); };
  piece(-1.6, z0 - 0.04, 0, 2.5); piece(z1 + 0.04, 2.3, 0, 2.5); piece(z0 - 0.04, z1 + 0.04, h + 0.04, 2.5);
  // front wall + floor/ceiling extension (only seen from the door pose)
  lplane(root, 4.4, 2.5, mat(0xffffff, 0.96, 0, { map: T.concreteTex(4.4, 2.5, { base: '#737a76', ties: true, stains: [[0.3, 0.2, 0.2, 0.4]] }) }), 0, 1.25, -1.6, { ry: 0 * Math.PI });
  lplane(root, 4.4, 0.95, mat(0x55564f, 0.95), 0, 0.0005, -1.15, { rx: -Math.PI / 2 });
  lplane(root, 4.4, 0.95, M.ceiling, 0, 2.5, -1.15, { rx: Math.PI / 2 });
  // steel frame
  const fr = M.steelDark;
  lbox(root, 0.14, h + 0.08, 0.08, fr, WX - 0.03, (h + 0.08) / 2, z0 - 0.04);
  lbox(root, 0.14, h + 0.08, 0.08, fr, WX - 0.03, (h + 0.08) / 2, z1 + 0.04);
  lbox(root, 0.14, 0.08, dw + 0.16, fr, WX - 0.03, h + 0.04, zc);
  // threshold
  lbox(root, 0.2, 0.025, dw, M.galv, WX, 0.0125, zc);
  // hazard header strip
  lplane(root, dw + 0.24, 0.12, mat(0xffffff, 0.7, 0, { map: X.hazard(1024, 128) }), WX - 0.1, h + 0.16, zc, { ry: -Math.PI / 2 });
  // corridor / stairwell outside (x 2.2 .. 4.4)
  const cm = mat(0x9aa39b, 0.92), cf = mat(0x6a6c64, 0.95);
  lplane(root, 2.2, 1.6, cf, WX + 1.1, 0.001, zc, { rx: -Math.PI / 2 });
  lplane(root, 1.6, 2.6, mat(0xb4bdb3, 0.95, 0, { map: T.concreteTex(1.6, 2.6, { base: '#aeb7ad', ties: true, stains: [[0.4, 0.8, 0.3, 0.5]] }) }), WX + 2.2, 1.3, zc, { ry: -Math.PI / 2 });
  lplane(root, 2.2, 2.6, cm, WX + 1.1, 1.3, zc + 0.8, { ry: Math.PI });
  lplane(root, 2.2, 2.6, cm, WX + 1.1, 1.3, zc - 0.8, {});
  lplane(root, 2.2, 1.6, mat(0x3a3c37, 0.95), WX + 1.1, 2.6, zc, { rx: Math.PI / 2 });
  // stairs going up to the left (+z side), seen through the door
  for (let i = 0; i < 9; i++) lbox(root, 0.28, 0.17, 0.7, mat(0x60645f, 0.95), WX + 0.75 + i * 0.28, 0.085 + i * 0.17, zc + 0.42);
  lcyl(root, 0.018, 1.0, M.galv, WX + 0.7, 0.95, zc + 0.08, { rz: 0.55 }); // handrail
  // debris on the landing
  for (const [dx, dz, s] of [[0.5, -0.4, 0.06], [0.8, -0.3, 0.04], [0.35, -0.55, 0.05]]) lbox(root, s, s * 0.6, s * 0.8, M.rubble, WX + dx, s * 0.3, zc + dz, { ry: dx * 3 });
  // caged bulb in the stairwell
  lsphere(root, 0.03, new THREE.MeshStandardMaterial({ color: 0xe8f0f0, emissive: 0xd8e8e8, emissiveIntensity: 2.0 }), WX + 1.3, 2.35, zc - 0.2, { cast: false });
  const cl = new THREE.PointLight(0xd8e6e2, o.corridorI ?? 5.5, 5, 2); cl.position.copy(U(WX + 1.3, 2.3, zc - 0.2)); root.add(cl);
  const cl2 = new THREE.PointLight(0xd8e6e2, 2.2, 3.5, 2); cl2.position.copy(U(WX + 1.9, 1.4, zc)); root.add(cl2);
  // door leaf (hinged at z0, opens inward into the room)
  const open = !!o.doorOpen;
  const hinge = group(root, WX - 0.045, 0, z0, open ? 95 : 0);
  const leafM = mat(0x4b5340, 0.6, 0.2);
  lbox(hinge, 0.07, h - 0.02, dw - 0.02, leafM, 0, (h - 0.02) / 2 + 0.01, (dw - 0.02) / 2 + 0.01);
  for (const y of [0.3, 1.0, 1.7]) lbox(hinge, 0.075, 0.04, dw - 0.12, M.oliveLight, -0.002, y, (dw - 0.02) / 2 + 0.01);
  for (const y of [0.35, 1.0, 1.65]) lcyl(hinge, 0.022, 0.16, M.steelDark, -0.02, y, 0.0);
  // handwheel
  const wheel = new THREE.Mesh(new THREE.TorusGeometry(0.15, 0.018, 10, 32), mat(0x8f1d14, 0.45));
  wheel.rotation.y = Math.PI / 2; wheel.position.set(-0.07, 1.05, -(dw / 2)); wheel.castShadow = true; hinge.add(wheel);
  for (let i = 0; i < 4; i++) { const sp = new THREE.Mesh(new THREE.BoxGeometry(0.012, 0.29, 0.012), M.steelDark); sp.position.set(-0.07, 1.05, -(dw / 2)); sp.rotation.x = i * Math.PI / 4; hinge.add(sp); }
  lcyl(hinge, 0.03, 0.05, M.steelDark, -0.05, 1.05, dw / 2, { rz: Math.PI / 2 });
  // ★ peephole with brass ring and hinged cover
  lcyl(hinge, 0.022, 0.012, M.brass, -0.04, 1.56, dw / 2 - 0.0, { rz: Math.PI / 2 });
  lcyl(hinge, 0.012, 0.014, mat(0x101410, 0.1, 0.2), -0.042, 1.56, dw / 2, { rz: Math.PI / 2 });
  const cov = lbox(hinge, 0.006, 0.05, 0.034, M.brassDark, -0.05, o.peepOpen ? 1.6 : 1.56, dw / 2, { rz: o.peepOpen ? 0.9 : 0 });
  // stencil 07
  const st = lplane(hinge, 0.32, 0.16, mat(0xffffff, 0.8, 0, { map: T.stencilTex(['07'], 0.32, 0.16, { color: '#c9c2ad', ls: 0.12 }), transparent: true }), -0.0362, 0.42, dw / 2, { ry: -Math.PI / 2 });
  A.leaf = hinge;
  // ★ intercom box (left of the door when facing it = lower z)
  const ic = group(root, WX - 0.03, 1.42, z0 - 0.32, 90);
  lbox(ic, 0.17, 0.25, 0.06, M.olive, 0, 0, 0.0);
  lplane(ic, 0.12, 0.1, mat(0x1c1f19, 0.8, 0, { map: (() => { const t = T.keyboardTex(); return t; })() }), 0, 0.04, -0.031);
  lcyl(ic, 0.02, 0.02, M.bakelite, 0, -0.06, -0.035, { rx: Math.PI / 2 });
  lsphere(ic, 0.009, new THREE.MeshStandardMaterial({ color: o.intercomOn ? 0xffb347 : 0x4a2f12, emissive: o.intercomOn ? 0xff9a2a : 0, emissiveIntensity: 2 }), 0.05, -0.06, -0.033);
  lplane(ic, 0.15, 0.03, new THREE.MeshStandardMaterial({ map: T.plateTex(o.lang === 'en' ? 'INTERCOM' : 'İNTERKOM', 0.15, 0.03), roughness: 0.4, metalness: 0.75 }), 0, 0.105, -0.031);
  lcyl(ic, 0.006, 0.6, M.galv, 0.06, 0.42, 0.0); // conduit up
  // ★ knock lamp (caged, amber when someone knocks; red only on a lethal outcome)
  const lampOn = o.knock || o.fatal;
  const lampCol = o.fatal ? 0xf0443a : 0xffa53a;
  lcyl(root, 0.05, 0.03, M.steelDark, WX - 0.03, h + 0.31, zc, { rz: Math.PI / 2 });
  lsphere(root, 0.04, new THREE.MeshStandardMaterial({ color: lampOn ? lampCol : 0x4a3a2a, emissive: lampOn ? lampCol : 0, emissiveIntensity: lampOn ? 2.4 : 0, roughness: 0.25 }), WX - 0.08, h + 0.31, zc, { cast: false });
  for (let i = 0; i < 3; i++) { const t = new THREE.Mesh(new THREE.TorusGeometry(0.045, 0.003, 4, 16), M.steelDark); t.position.copy(U(WX - 0.08, h + 0.31, zc)); t.rotation.y = i * Math.PI / 3; root.add(t); }
  if (lampOn) { const pl = new THREE.PointLight(lampCol, o.fatal ? 2.2 : 1.2, 3.5, 2); pl.position.copy(U(WX - 0.25, h + 0.28, zc)); root.add(pl); }
  // ★ visitor tally in chalk next to the door
  if (o.visitorTally) lplane(root, 0.84, 0.21, mat(0xffffff, 0.95, 0, { map: X.tally(o.visitorTally, o.lang === 'en' ? 'visitors' : 'ziyaretçi'), transparent: true }), WX - 0.004, 1.55, z1 + 0.52, { ry: -Math.PI / 2 });
  // visitor silhouette in the doorway
  if (o.visitor) A.visitor = visitor(root, WX + 0.42, zc, o.visitor);
  A.center = [WX, 1.0, zc];
  return A;
}

// Silhouette catalogue: simple primitives; read by outline, no faces.
export function visitor(root, x, z, kind = 'anne', yaw = 90) {
  const g = group(root, x, 0, z, yaw);
  const dark = mat(0x16171a, 0.85), cloth = mat(0x1d1f22, 0.9), gloss = mat(0x2a2d33, 0.25, 0.4);
  const cap = (r, len, m) => { const me = new THREE.Mesh(new THREE.CapsuleGeometry(r, len, 6, 14), m); me.castShadow = true; return me; };
  const add = (p, ob, px, py, pz, rx = 0, rz = 0, ry = 0) => { ob.position.set(px, py, -pz); ob.rotation.set(rx, ry, rz); p.add(ob); return ob; };
  const bodyM = kind === 'kopya' ? gloss : cloth;
  const H = { anne: 1.66, yasli: 1.58, silahli: 1.82, asker: 1.8, yarali: 1.72, kopya: 1.84, generic: 1.75 }[kind] || 1.75;
  const s = H / 1.75;
  const hunch = kind === 'yasli' ? 0.32 : kind === 'yarali' ? 0.16 : 0;
  const ub = new THREE.Group(); ub.position.set(0, 0.92 * s, 0); ub.rotation.x = hunch; ub.rotation.z = kind === 'yarali' ? 0.12 : 0; g.add(ub);
  // legs
  for (const sx of [-1, 1]) add(g, cap(0.075 * s, 0.72 * s, bodyM), sx * (kind === 'silahli' ? 0.15 : 0.1) * s, 0.46 * s, 0, 0, sx * (kind === 'silahli' ? 0.12 : 0));
  // torso: coat silhouette (wider for anne / asker)
  const tw = { anne: 0.24, asker: 0.27, silahli: 0.26, kopya: 0.22, yasli: 0.22, yarali: 0.23, generic: 0.23 }[kind] || 0.23;
  const torso = new THREE.Mesh(new THREE.CylinderGeometry(tw * s, (tw - 0.02) * s, 0.62 * s, 24), bodyM); torso.scale.z = 0.62; torso.castShadow = true; add(ub, torso, 0, 0.36 * s, 0);
  if (kind === 'anne') { const skirt = new THREE.Mesh(new THREE.CylinderGeometry(0.205 * s, 0.3 * s, 0.66 * s, 24), cloth); skirt.scale.z = 0.7; add(g, skirt, 0, 0.64 * s, 0); }
  // head (with variant headwear)
  const head = new THREE.Mesh(new THREE.SphereGeometry(0.105 * s, 20, 16), kind === 'kopya' ? gloss : dark); head.scale.set(0.92, 1.12, 1); head.castShadow = true; add(ub, head, 0, 0.82 * s, 0);
  if (kind === 'anne') { const scarf = new THREE.Mesh(new THREE.SphereGeometry(0.125 * s, 20, 16, 0, Math.PI * 2, 0, Math.PI * 0.62), cloth); scarf.scale.set(1, 1.15, 1.05); add(ub, scarf, 0, 0.84 * s, 0.01); }
  if (kind === 'asker') { const helm = new THREE.Mesh(new THREE.SphereGeometry(0.135 * s, 20, 12, 0, Math.PI * 2, 0, Math.PI * 0.5), dark); helm.scale.set(1.05, 0.8, 1.1); add(ub, helm, 0, 0.88 * s, 0); const rim = new THREE.Mesh(new THREE.TorusGeometry(0.13 * s, 0.012, 6, 24), dark); rim.rotation.x = Math.PI / 2; add(ub, rim, 0, 0.88 * s, 0); }
  if (kind === 'yasli') { const hat = new THREE.Mesh(new THREE.CylinderGeometry(0.09 * s, 0.1 * s, 0.05 * s, 18), dark); add(ub, hat, 0, 0.93 * s, 0); const brim = new THREE.Mesh(new THREE.CylinderGeometry(0.16 * s, 0.16 * s, 0.008, 22), dark); add(ub, brim, 0, 0.905 * s, 0); }
  if (kind === 'silahli') { const hood = new THREE.Mesh(new THREE.SphereGeometry(0.13 * s, 18, 14, 0, Math.PI * 2, 0, Math.PI * 0.7), cloth); add(ub, hood, 0, 0.84 * s, 0.02); }
  // arms
  const arm = (sx, ux, uz, fx, fz) => { const sh = new THREE.Group(); sh.position.set(sx * (tw + 0.02) * s, 0.62 * s, 0); sh.rotation.set(ux, 0, uz); ub.add(sh); const ua = cap(0.055 * s, 0.24 * s, bodyM); ua.position.y = -0.16 * s; sh.add(ua); const el = new THREE.Group(); el.position.y = -0.32 * s; el.rotation.set(fx, 0, fz); sh.add(el); const fa = cap(0.05 * s, 0.22 * s, bodyM); fa.position.y = -0.15 * s; el.add(fa); return el; };
  if (kind === 'anne') { // child bundle on the shoulder
    const el = arm(1, -0.5, 0.25, -1.6, -0.6); arm(-1, -0.6, -0.2, -1.4, 0.7);
    const bundle = new THREE.Mesh(new THREE.CapsuleGeometry(0.11 * s, 0.22 * s, 6, 14), mat(0x24262a, 0.95)); bundle.rotation.z = 1.1; add(ub, bundle, 0.06, 0.58 * s, -0.15);
    const ch = new THREE.Mesh(new THREE.SphereGeometry(0.075 * s, 16, 12), dark); add(ub, ch, 0.17, 0.74 * s, -0.14);
  } else if (kind === 'yasli') { const el = arm(1, -0.25, 0.1, -0.5, 0); arm(-1, 0.05, -0.1, -0.2, 0); const cane = new THREE.Mesh(new THREE.CylinderGeometry(0.012, 0.012, 0.95, 8), mat(0x2a1e14, 0.7)); add(g, cane, 0.3 * s, 0.47, -0.22, 0.25, 0.05);
  } else if (kind === 'silahli') { arm(1, -1.1, 0.1, -0.7, -0.4); arm(-1, -0.9, -0.3, -1.0, 0.6);
    const rifle = new THREE.Group(); add(ub, rifle, 0.02, 0.45 * s, -0.22, 0, 0.55); const bar = new THREE.Mesh(new THREE.BoxGeometry(0.035, 0.85, 0.05), dark); rifle.add(bar); const stock = new THREE.Mesh(new THREE.BoxGeometry(0.05, 0.22, 0.09), dark); stock.position.y = -0.4; rifle.add(stock); const mag = new THREE.Mesh(new THREE.BoxGeometry(0.03, 0.12, 0.05), dark); mag.position.set(0, -0.05, 0.05); rifle.add(mag);
  } else if (kind === 'asker') { arm(1, 0.05, 0.12, -0.15, 0); arm(-1, 0.05, -0.12, -0.15, 0); const pack = new THREE.Mesh(new THREE.BoxGeometry(0.34 * s, 0.45 * s, 0.2 * s), cloth); add(ub, pack, 0, 0.4 * s, 0.2);
  } else if (kind === 'yarali') { arm(1, -0.4, 0.15, -1.7, -0.9); arm(-1, 0.25, -0.35, -0.2, 0); const sling = new THREE.Mesh(new THREE.TorusGeometry(0.16 * s, 0.025, 6, 20, Math.PI), mat(0x3a3a38, 0.9)); add(ub, sling, 0.02, 0.45 * s, -0.12, 0, 0.6);
  } else if (kind === 'kopya') { arm(1, 0, 0.06, 0, 0); arm(-1, 0, -0.06, 0, 0); const seam = new THREE.Mesh(new THREE.BoxGeometry(0.004, 0.24 * s, 0.006), mat(0x7a7f88, 0.2, 0.6)); add(ub, seam, 0, 0.82 * s, -0.104);
  } else { arm(1, 0.05, 0.1, -0.1, 0); arm(-1, 0.05, -0.1, -0.1, 0); }
  return g;
}
