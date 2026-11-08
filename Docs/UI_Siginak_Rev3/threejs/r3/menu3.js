// Rev3 menu scene (Giris): the landing outside Shelter 07's door. Unity coords; landing x 2.2–4.6, z −1.35…1.85 (0.30 m
// wider to the right than today so the main switch sits right of the door), h 2.6. The door wall is x = 2.2, facing +x.
import * as THREE from 'three';
import { inst, has, slotText, printPlane, setMat, matLike, U, D2R, anchorWorld, drumTex, drumAngle, textCanvas, canvas } from './scene3.js';
import * as P from './prints.js';
import { material } from './mats.js';

export const LAND = { x0: 2.2, x1: 4.6, z0: -1.35, z1: 1.85, h: 2.6, door: { z0: 0.32, z1: 1.18, h: 2.0 } };

function plane(root, w, h, mat, pos, rot) { const m = new THREE.Mesh(new THREE.PlaneGeometry(w, h), mat); m.position.copy(U(...pos)); m.rotation.order = 'YXZ'; m.rotation.set((rot[0] || 0) * D2R, (rot[1] || 0) * D2R, (rot[2] || 0) * D2R); m.receiveShadow = true; root.add(m); return m; }
function boxU(root, w, h, d, mat, pos) { const m = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), mat); m.position.copy(U(...pos)); m.castShadow = true; m.receiveShadow = true; root.add(m); return m; }

export function buildLanding(scene, o = {}) {
  const lang = o.lang || 'tr'; const S = {}; const root = new THREE.Group(); scene.add(root); S.root = root;
  const { x0, x1, z0, z1, h } = LAND; const D = LAND.door; const zc = (D.z0 + D.z1) / 2;
  // ---- shell: door wall = tiles to 1.45 + dark concrete above; side walls same; floor wet concrete; low ceiling slab
  const tileH = 1.45;
  const tilesDoor = new THREE.MeshStandardMaterial({ map: P.tileTex(z1 - z0, tileH, { base: '#AEB29B', grimeH: 0.5 }), roughness: 0.38 });
  const concDoor = new THREE.MeshStandardMaterial({ map: P.concrete(z1 - z0, h - tileH, { base: '#4F5550', stains: [[0.55, 0.0, 0.18, 0.5], [0.2, 0.1, 0.12, 0.4]] }), roughness: 0.95 });
  // door wall pieces around the portal opening (portal 0.16 each side, head to 2.16)
  const pz0 = D.z0 - 0.16, pz1 = D.z1 + 0.16, ph = D.h + 0.16;
  const wallPiece = (za, zb, ya, yb, mat) => { if (zb - za < 1e-3 || yb - ya < 1e-3) return; const mm = mat.clone(); if (mm.map) { mm.map = mm.map.clone(); mm.map.needsUpdate = true; mm.map.repeat.set((zb - za) / (z1 - z0), (yb - ya) / (mat === tilesDoor ? tileH : h - tileH)); mm.map.offset.set((za - z0) / (z1 - z0), (ya - (mat === tilesDoor ? 0 : tileH)) / (mat === tilesDoor ? tileH : h - tileH)); }
    plane(root, zb - za, yb - ya, mm, [x0, (ya + yb) / 2, (za + zb) / 2], [0, 90, 0]); };
  wallPiece(z0, pz0, 0, tileH, tilesDoor); wallPiece(pz1, z1, 0, tileH, tilesDoor);
  wallPiece(z0, pz0, tileH, h, concDoor); wallPiece(pz1, z1, tileH, h, concDoor); wallPiece(pz0, pz1, ph, h, concDoor);
  // tile cap (bullnose row) at tileH
  boxU(root, 0.018, 0.03, pz0 - z0, new THREE.MeshStandardMaterial({ color: '#5E6455', roughness: 0.35 }), [x0 + 0.009, tileH, (z0 + pz0) / 2]);
  boxU(root, 0.018, 0.03, z1 - pz1, new THREE.MeshStandardMaterial({ color: '#5E6455', roughness: 0.35 }), [x0 + 0.009, tileH, (pz1 + z1) / 2]);
  // concrete portal (reveal 0.30 deep) with hazard chevrons on the jambs
  const portalM = new THREE.MeshStandardMaterial({ map: P.concrete(1.2, 2.3, { base: '#666C66', streaks: 14 }), roughness: 0.92 });
  boxU(root, 0.06, ph, 0.16, portalM, [x0 + 0.03, ph / 2, D.z0 - 0.08]); boxU(root, 0.06, ph, 0.16, portalM, [x0 + 0.03, ph / 2, D.z1 + 0.08]);
  boxU(root, 0.06, 0.16, pz1 - pz0, portalM, [x0 + 0.03, D.h + 0.08, zc]);
  const hz = new THREE.MeshStandardMaterial({ map: P.hazardTex(128, 512, { pitch: 80 }), roughness: 0.65 });
  for (const zz of [D.z0 - 0.08, D.z1 + 0.08]) plane(root, 0.16, 0.9, hz, [x0 + 0.0605, 0.45, zz], [0, 90, 0]);
  // side walls (left z0 facing +z, right z1 facing −z) and the back wall behind the camera
  const tilesSide = new THREE.MeshStandardMaterial({ map: P.tileTex(x1 - x0, tileH, { base: '#A7AB94' }), roughness: 0.4 });
  const concSide = new THREE.MeshStandardMaterial({ map: P.concrete(x1 - x0, h - tileH, { base: '#4A4F4B' }), roughness: 0.95 });
  plane(root, x1 - x0, tileH, tilesSide, [(x0 + x1) / 2, tileH / 2, z0], [0, 180, 0]); plane(root, x1 - x0, h - tileH, concSide, [(x0 + x1) / 2, (tileH + h) / 2, z0], [0, 180, 0]);
  plane(root, x1 - x0, h, concSide.clone(), [(x0 + x1) / 2, h / 2, z1], [0, 0, 0]);
  plane(root, z1 - z0, h, concSide.clone(), [x1, h / 2, (z0 + z1) / 2], [0, -90, 0]);
  // floor + ceiling
  const fl = plane(root, x1 - x0, z1 - z0, new THREE.MeshStandardMaterial({ map: P.floorTex(x1 - x0, z1 - z0, { stains: [[0.15, 0.5, 0.3, 0.5], [0.7, 0.2, 0.2, 0.3]] }), roughness: 0.7, metalness: 0.05 }), [(x0 + x1) / 2, 0, (z0 + z1) / 2], [-90, 0, 0]);
  const pud = new THREE.Mesh(new THREE.CircleGeometry(0.42, 40), new THREE.MeshStandardMaterial({ color: '#1A1E1C', roughness: 0.06, metalness: 0.3, transparent: true, opacity: 0.7 }));
  pud.scale.set(1.4, 0.7, 1); pud.rotation.x = -Math.PI / 2; pud.position.copy(U(x0 + 0.62, 0.002, zc - 0.25)); pud.receiveShadow = true; root.add(pud);
  plane(root, x1 - x0, z1 - z0, new THREE.MeshStandardMaterial({ map: P.concrete(x1 - x0, z1 - z0, { base: '#2F3330' }), roughness: 0.95 }), [(x0 + x1) / 2, h, (z0 + z1) / 2], [90, 0, 0]);
  // drain grate + conduit runs
  boxU(root, 0.22, 0.004, 0.22, new THREE.MeshStandardMaterial({ color: '#2A2C2A', roughness: 0.6, metalness: 0.5 }), [x0 + 0.95, 0.002, zc - 0.55]);
  const galv = material('galv');
  const cnd = (pos, len, axis) => { const c = new THREE.Mesh(new THREE.CylinderGeometry(0.014, 0.014, len, 10), galv); c.position.copy(U(...pos)); if (axis === 'z') c.rotation.x = Math.PI / 2; c.castShadow = true; root.add(c); };
  cnd([x0 + 0.04, 2.47, (z0 + z1) / 2], z1 - z0, 'z'); cnd([x0 + 0.04, 2.2, -0.9 - 0.22], 0.54, 'y'); cnd([x0 + 0.05, 2.03, 1.55], 0.86, 'y');

  // ---- door (shared leaf: outer face toward +x)
  S.door = null;
  if (has('DoorFrame')) { S.frame = inst(root, 'DoorFrame', [x0, 0, zc], 90, {}); setMat(S.frame, 'concreteWall', portalM); }
  if (!o.doorOpen) { // warm light leaking under the closed leaf: thin emissive strip on the floor at the threshold
    const lk = new THREE.Mesh(new THREE.PlaneGeometry(0.80, 0.05), new THREE.MeshBasicMaterial({ color: 0xffb35a, transparent: true, opacity: 0.85 })); lk.rotation.x = -Math.PI / 2; lk.position.copy(U(x0 + 0.045, 0.003, zc)); lk.rotation.z = Math.PI / 2; root.add(lk); }
  if (has('DoorLeaf')) { S.door = inst(root, 'DoorLeaf', [x0, 0, zc], 90, {}); if (o.doorOpen) S.door.pose('Hinge', o.doorOpen === true ? 95 : o.doorOpen);
    // outer face hardware rides on the leaf (Hinge part): outer wheel + peep ring at the leaf's anchors, facing the landing
    const H = S.door.parts.Hinge, pv = H.pivot; const at = (n) => { const a = S.door.meta.anchors.find(q => q.name === n); return a ? [a.pos[0] - pv[0], a.pos[1] - pv[1], a.pos[2] - pv[2]] : null; };
    const wp = at('OuterWheel'), pp = at('PeepRingOuter');
    if (wp) { S.wheel = inst(H.group, 'OuterWheel', wp, 180); if (o.wheelTurn) S.wheel.pose('Wheel', o.wheelTurn); }
    if (pp) S.peep = inst(H.group, 'PeepRing', pp, 180); }
  else { // fallback leaf (until the door family lands)
    const hinge = new THREE.Group(); hinge.position.copy(U(x0 + 0.01, 0, D.z0)); root.add(hinge); S.hinge = hinge;
    const leaf = new THREE.Mesh(new THREE.BoxGeometry(0.07, D.h - 0.02, D.z1 - D.z0 - 0.02), material('paintOliveDark')); leaf.position.set(0.035, (D.h) / 2, -(D.z1 - D.z0) / 2); leaf.castShadow = leaf.receiveShadow = true; hinge.add(leaf);
    for (const y of [0.32, 1.0, 1.68]) { const b = new THREE.Mesh(new THREE.BoxGeometry(0.012, 0.05, D.z1 - D.z0 - 0.12), material('paintOlive')); b.position.set(0.075, y, -(D.z1 - D.z0) / 2); b.castShadow = true; hinge.add(b); }
    if (o.doorOpen) hinge.rotation.y = (o.doorOpen === true ? 95 : o.doorOpen) * D2R;
    S.leafGroup = hinge;
  }
  // exterior details on the leaf (wheel, peephole ring), placed in world for the closed door
  if (!o.doorOpen && !S.door) {
    S.wheel = inst(root, 'OuterWheel', [x0 + 0.075, 1.05, zc + 0.12], -90); if (o.wheelTurn) S.wheel.pose('Wheel', o.wheelTurn);
    S.peep = inst(root, 'PeepRing', [x0 + 0.075, 1.56, zc + 0.12], -90);
  }
  // ---- wall furniture (door wall, facing +x → yaw −90)
  const yaw = -90; const wy = (y) => y;
  S.cab = inst(root, has('SettingsCabinetMenu') ? 'SettingsCabinetMenu' : 'SettingsCabinet', [x0, 1.02, -0.99], yaw);
  S.rack = inst(root, 'CardRack', [x0, 1.14, -0.55], yaw);
  S.deck = inst(root, 'BlankDeck', [x0, 0.95, -0.55], yaw);
  S.clock = inst(root, 'PunchClock', [x0, 1.18, -0.30], yaw);
  S.ic = inst(root, 'OuterIntercom', [x0, 1.26, -0.02], yaw);
  S.bell = inst(root, 'DoorBell', [x0, 1.66, -0.02], yaw);
  S.lamp2 = inst(root, 'LandingLamp', [x0, 2.3, -0.5], yaw);
  S.sw = inst(root, 'LastSwitch', [x0, 1.06, 1.62], yaw, { pose: { Lever: 55, Cover: 0 } });
  S.title = inst(root, 'TitleStencil', [x0, 2.17, zc], yaw);
  S.lamp = inst(root, 'LandingLamp', [x0, 2.45, zc], yaw);
  // stairs up along the right wall: start x 2.95, rise toward +x
  S.stairs = inst(root, 'Stairs', [x0 + 0.78, 0, z1 - 0.48], 90);
  return S;
}

// ---------------------------------------------------------------- text + prints for the landing
export function dressLanding(S, o = {}) {
  const lang = o.lang || 'tr'; const EN = lang === 'en';
  // title stencil
  slotText(S.title, 'TXT_Title', [{ t: EN ? 'CORRIGIBLE' : 'SON ŞALTER', size: 700, f: 'st', w: 800, c: '#CFC8B2', ls: 0.06 }], { ppm: 900, rough: 0.85 });
  if (S.door && S.door.meta.texts.find(t => t.name === 'TXT_StencilOuter')) slotText(S.door, 'TXT_StencilOuter', [{ t: EN ? 'SHELTER 07 · KYGA' : 'SIĞINAK 07 · KYGA', size: 160, f: 'st', w: 800, c: '#D8D2BE', ls: 0.06 }], { ppm: 1200, rough: 0.85 });
  // switch plate
  slotText(S.sw, 'TXT_PlateTR', [{ t: 'ANA ŞALTER', size: 120, c: '#2A1E10', ls: 0.06 }], { ppm: 2400, metal: 0.6, rough: 0.4 });
  slotText(S.sw, 'TXT_PlateEN', [{ t: 'MAIN SWITCH', size: 66, c: '#2A1E10', ls: 0.08 }], { ppm: 2400, metal: 0.6, rough: 0.4 });
  slotText(S.sw, 'TXT_Lamp', [{ t: EN ? 'MAINS' : 'ŞEBEKE', size: 46, c: '#D8D2BE', ls: 0.06 }], { ppm: 3000 });
  setMat(S.sw, 'paintHazard', new THREE.MeshStandardMaterial({ map: P.hazardTex(512, 160, { pitch: 70 }), roughness: 0.6 }));
  // clock
  setMat(S.clock, 'dialCream', new THREE.MeshStandardMaterial({ color: '#ECE3C8', roughness: 0.6 }));
  { const dial = printPlane(S.clock, [0, 0.315, -0.165 - 0.0045], [0.196, 0.196], P.clockDialTex(), { circle: true, rough: 0.55 }); }
  const drum = new THREE.MeshStandardMaterial({ map: drumTex(), roughness: 0.45, metalness: 0.2 });
  for (const m of [S.clock]) setMat(m, 'counterDrum', drum);
  const shift = String(o.shift ?? 14).padStart(3, '0'); [0, 1, 2].forEach(i => S.clock.pose(`Wheel${i}`, drumAngle(+shift[i])));
  S.clock.pose('HourHand', -(5 * 30 + 25)); S.clock.pose('MinuteHand', -300);
  slotText(S.clock, 'TXT_Plate', [{ t: EN ? 'TIME CLOCK' : 'PUANTAJ', size: 92, c: '#2A1E10', ls: 0.1 }], { ppm: 2400, metal: 0.6, rough: 0.4 });
  slotText(S.clock, 'TXT_Maker', [{ t: EN ? 'KYGA · CIVIL DEFENCE' : 'KYGA · SİVİL SAVUNMA', size: 34, c: '#C9C2AA', ls: 0.12 }], { ppm: 2400 });
  slotText(S.clock, 'TXT_ShiftLabel', [{ t: EN ? 'SHIFT' : 'VARDİYA', size: 40, c: '#2A1E10', ls: 0.12 }], { ppm: 2400 });
  if (o.lampOn) setMat(S.clock, 'jewelAmberOff', material('jewelAmberOn'));
  // cabinet (menu) closed: stencil + plate + badge
  dressCabinet(S.cab, { lang, closed: !o.cabOpen });
  // rack + deck texts
  slotText(S.rack, 'TXT_Pocket', [{ t: EN ? 'COMMANDER' : 'KOMUTAN', size: 36, c: '#1E1B16', ls: 0.06 }], { ppm: 7000 });
  slotText(S.deck, 'TXT_Deck', [{ t: EN ? 'NEW CAMPAIGN' : 'YENİ HAREKÂT', size: 34, c: '#1E1B16', ls: 0.04 }], { ppm: 7000 });
  return S;
}

// cabinet: assemble panel controls at anchors + texts
export function assembleCabinet(m, o = {}) {
  const menu = m.name === 'SettingsCabinetMenu'; const C = { sliders: [], gauges: [], counters: [], plates: {} };
  const put = (name, anchor) => { const a = m.meta.anchors.find(q => q.name === anchor); const sub = (await_build(name)); sub.root.position.copy(U(...a.pos)); m.root.add(sub.root); return sub; };
  for (let i = 0; i < 4; i++) { C.gauges.push(put('CabGauge', `Gauge${i}`)); C.sliders.push(put(i === 0 ? 'CabSliderMaster' : 'CabSlider', `Slider${i}`)); C.counters.push(put('CabCounter', `Counter${i}`)); C.plates[i] = put('CabPlate', `Plate${i}`); }
  C.lamps = []; for (let i = 0; i < 4; i++) { const a = m.meta.anchors.find(q => q.name === `Gauge${i}`); const l = await_build('CabLamp'); l.root.position.copy(U(a.pos[0] + 0.03, a.pos[1] - 0.05, a.pos[2])); m.root.add(l.root); C.lamps.push(l); }
  C.toggle = put('CabToggle', 'ToggleSES'); { const a = m.meta.anchors.find(q => q.name === 'ToggleSES'); const l = await_build('CabLamp'); l.root.position.copy(U(a.pos[0], a.pos[1] + 0.055, a.pos[2])); m.root.add(l.root); C.sesLamp = l; }
  C.plates.SES = put('CabPlate', 'PlateSES'); C.rotary = put('CabRotary', 'RotaryDIL'); C.plates.DIL = put('CabPlate', 'PlateDIL');
  C.lever = put(menu ? 'CabBlank' : 'CabMenuLever', 'MenuLever'); if (!menu) C.plates.MENU = put('CabPlate', 'PlateMENU');
  m.cab = C; return C;
}
import { MODELS } from './registry.js';
function await_build(n) { return MODELS[n].build(); }
export function dressCabinet(m, o = {}) {
  const EN = o.lang === 'en';
  slotText(m, 'TXT_Stencil', [{ t: EN ? 'SETTINGS' : 'AYARLAR', size: 300, f: 'st', w: 800, c: '#D8D2BE', ls: 0.08 }], { ppm: 1100, rough: 0.8 });
  slotText(m, 'TXT_LeafPlate', [{ t: EN ? 'SOUND · LANGUAGE' : 'SES · DİL', size: 64, c: '#2A1E10', ls: 0.08 }], { ppm: 2400, metal: 0.6, rough: 0.4 });
  setMat(m, 'enamelBadge', new THREE.MeshStandardMaterial({ color: '#E8E2CF', roughness: 0.3 }));
  printPlane(m, [-0.06, 0.39, -0.22 - 0.004 - 0.0145], [0.124, 0.124], P.gearBadgeTex(), { circle: true, part: 'Leaf', rough: 0.3 });
  if (o.open != null) m.pose('Leaf', o.open);
}
export function dressCabinetPanel(m, o = {}) {
  const EN = o.lang === 'en'; const C = m.cab; const vals = o.values || [80, 100, 100, 100]; const mute = !!o.mute;
  const labels = EN ? ['MASTER', 'MUSIC', 'AMBIENCE', 'EFFECTS'] : ['GENEL', 'MÜZİK', 'ORTAM', 'EFEKT'];
  const drum = new THREE.MeshStandardMaterial({ map: drumTex({ size: 110 }), roughness: 0.45, metalness: 0.2 });
  const gTex = P.gaugeTex(), sTex = P.scaleTex();
  for (let i = 0; i < 4; i++) {
    slotText(C.plates[i], 'TXT', [{ t: labels[i], size: 44, c: '#2A1E10', ls: 0.06 }], { ppm: 5000, metal: 0.6, rough: 0.4 });
    setMat(C.counters[i], 'counterDrum', drum); const v = String(vals[i]).padStart(3, '0'); [0, 1, 2].forEach(k => C.counters[i].pose(`Wheel${k}`, drumAngle(+v[k])));
    C.sliders[i].pose('Handle', -0.08 + 0.16 * vals[i] / 100);
    printPlane(C.gauges[i], [0, 0.002, -0.0125], [0.066, 0.046], gTex, {});
    printPlane(C.sliders[i], [-0.03, 0, -0.0017], [0.015, 0.188], sTex, {});
    C.gauges[i].pose('Needle', mute ? 50 : 50 - vals[i]);
    const on = !mute && vals[i] > 0; setMat(C.lamps[i], 'jewelAmberOn', material(on ? 'jewelAmberOn' : 'jewelAmberOff'));
  }
  C.toggle.pose('Lever', mute ? 28 : -28); setMat(C.sesLamp, 'jewelAmberOn', material(mute ? 'jewelAmberOff' : 'jewelAmberOn'));
  slotText(C.plates.SES, 'TXT', [{ t: EN ? 'SOUND' : 'SES', size: 44, c: '#2A1E10', ls: 0.08 }], { ppm: 5000, metal: 0.6, rough: 0.4 });
  slotText(C.plates.DIL, 'TXT', [{ t: 'DİL · LANGUAGE', size: 36, c: '#2A1E10', ls: 0.04 }], { ppm: 5000, metal: 0.6, rough: 0.4 });
  slotText(C.rotary, 'TXT_PosTR', [{ t: 'TÜRKÇE', size: 30, c: '#E6CF9C', ls: 0.06 }], { ppm: 5000 });
  slotText(C.rotary, 'TXT_PosEN', [{ t: 'ENGLISH', size: 30, c: '#E6CF9C', ls: 0.06 }], { ppm: 5000 });
  C.rotary.pose('Knob', EN ? -40 : 40);
  if (C.plates.MENU) slotText(C.plates.MENU, 'TXT', [{ t: EN ? 'TO MENU' : 'MENÜYE DÖN', size: 38, c: '#2A1E10', ls: 0.04 }], { ppm: 5000, metal: 0.6, rough: 0.4 });
  if (C.lever && C.lever.name === 'CabMenuLever') { setMat(C.lever, 'paintHazard', new THREE.MeshStandardMaterial({ map: P.hazardTex(256, 256, { pitch: 64 }), roughness: 0.6 })); C.lever.pose('Lever', 35);
    if (o.locked) slotText(C.lever, 'TXT_Lock', [{ t: EN ? 'LOCKED' : 'KİLİTLİ', size: 34, c: '#675F50', ls: 0.1 }], { ppm: 5000 });
    else C.lever.parts.LockBand.group.visible = false; }
}
