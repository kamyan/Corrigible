import * as THREE from 'three';
import { U, D2R, M, mat, lbox, lcyl, lsphere, lplane, group } from './lib.js';
import * as T from './textures.js';
import * as X from './tex2.js';
import { buildDoor } from './door.js';
import { buildDiorama2 } from './diorama.js';

// Builds the bunker command room. opts: {lang, state:{...}, show:{map:bool, telexOverlay:bool,...}, dim:0..1, switchOpen:bool}
export function buildRoom(scene, opts) {
  const lang = opts.lang || 'tr';
  const S = T.STR[lang];
  const st = opts.state;
  const A = {}; // anchors for overlays (Unity coords arrays of corners)
  const root = group(scene, 0, 0, 0, 0);

  // ---------------- room shell
  const wallTex = T.concreteTex(4.4, 2.5, { base: '#7c837f', stains: [[0.18, 0.08, 0.16, 0.5], [0.75, 0.05, 0.22, 0.45], [0.55, 0.95, 0.25, 0.35], [0.05, 0.6, 0.12, 0.35]], damp: true });
  const wallMat = mat(0xffffff, 0.96, 0, { map: wallTex });
  lplane(root, 4.4, 2.5, wallMat, 0, 1.25, 2.30);
  const sideTex = T.concreteTex(3.0, 2.5, { base: '#737a76', stains: [[0.5, 0.1, 0.25, 0.4]], damp: true, ties: true });
  const sideMat = mat(0xffffff, 0.96, 0, { map: sideTex });
  lplane(root, 3.0, 2.5, sideMat, -2.2, 1.25, 0.8, { ry: Math.PI / 2 });
  const floorTex = T.concreteTex(4.4, 3.0, { ppm: 200, base: '#5a5b54', ties: false, streaks: 0, stains: [[0.5, 0.5, 0.4, 0.3]] });
  lplane(root, 4.4, 3.0, mat(0xffffff, 0.95, 0, { map: floorTex }), 0, 0, 0.8, { rx: -Math.PI / 2 });
  lplane(root, 4.4, 3.0, M.ceiling, 0, 2.5, 0.8, { rx: Math.PI / 2 });

  // duct along the top of the back wall
  lbox(root, 4.4, 0.22, 0.26, M.galv, 0, 2.30, 2.16);
  for (let x = -2.0; x <= 2.0; x += 0.6) lbox(root, 0.02, 0.24, 0.28, M.steelDark, x, 2.30, 2.16);
  // vertical pipes right
  lcyl(root, 0.038, 2.5, M.pipe, 1.03, 1.25, 2.22);
  lcyl(root, 0.03, 2.5, M.pipe, 1.13, 1.25, 2.23);
  const wheel = new THREE.Mesh(new THREE.TorusGeometry(0.075, 0.012, 8, 24), M.red);
  wheel.position.copy(U(1.03, 1.32, 2.16)); wheel.castShadow = true; root.add(wheel);
  lcyl(root, 0.012, 0.06, M.steelDark, 1.03, 1.32, 2.19, { rx: Math.PI / 2 });
  for (const y of [0.55, 1.75]) lbox(root, 0.2, 0.03, 0.05, M.steelDark, 1.08, y, 2.25);
  // left conduit + junction box
  lcyl(root, 0.018, 2.5, M.galv, -1.12, 1.25, 2.27);
  lbox(root, 0.14, 0.18, 0.07, M.galv, -1.12, 1.78, 2.25);
  lcyl(root, 0.012, 1.0, M.rubber, -0.62, 2.02, 2.27, { rz: Math.PI / 2 });

  // stencil sign
  lplane(root, 0.84, 0.15, mat(0xffffff, 0.9, 0, { map: T.stencilTex(['KYGA / SIĞINAK 07'], 0.84, 0.15, { ls: 0.08 }), transparent: true }), -0.05, 1.77, 2.295);
  // doctrine poster (K-07)
  const poster = lplane(root, 0.42, 0.57, mat(0xffffff, 0.85, 0, { map: T.posterTex(lang) }), 0.68, 1.38, 2.293, { rz: -1.5 * D2R });
  for (const [dx, dy] of [[-0.19, 0.27], [0.19, 0.27], [-0.19, -0.27]]) lplane(root, 0.06, 0.025, mat(0xd9cfa8, 0.6, 0, { transparent: true, opacity: 0.75 }), 0.68 + dx, 1.38 + dy, 2.291, { rz: (dx > 0 ? 30 : -30) * D2R });
  if (opts.stage === 'late') { // worn corner + extra tape
    lplane(root, 0.07, 0.03, mat(0xd9cfa8, 0.6, 0, { transparent: true, opacity: 0.8 }), 0.68 + 0.19, 1.38 - 0.27, 2.2905, { rz: 25 * D2R });
  }
  // ★ freed-regions board (cork, 0.78 x 0.54): header card with the count + one card per freed region
  lbox(root, 0.82, 0.58, 0.02, M.wood, -0.60, 1.34, 2.285);
  lplane(root, 0.78, 0.54, M.cork, -0.60, 1.34, 2.274);
  const freed = opts.freed || [];
  lplane(root, 0.36, 0.104, mat(0xffffff, 0.9, 0, { map: X.corkHeader(String(freed.length).padStart(2, '0'), lang) }), -0.78, 1.535, 2.271, { rz: -1 * D2R });
  lsphere(root, 0.008, mat(0x1a4789, 0.4), -0.78, 1.578, 2.266);
  const NM = lang === 'en' ? { weu: 'WESTERN\nEUROPE', eeu: 'EASTERN\nEUROPE', me: 'MIDDLE\nEAST', af: 'AFRICA', ru: 'RUSSIA', in: 'INDIA' } : { weu: 'BATI\nAVRUPA', eeu: 'DOĞU\nAVRUPA', me: 'ORTA\nDOĞU', af: 'AFRİKA', ru: 'RUSYA', in: 'HİNDİSTAN' };
  for (let i = 0; i < 12; i++) {
    const col = i % 4, row = Math.floor(i / 4);
    const cx = -0.86 + col * 0.172, cy = 1.39 - row * 0.106;
    const f = freed[i];
    const rot = ((i * 37) % 7 - 3) * 0.8;
    if (f) {
      lplane(root, 0.15, 0.095, mat(0xffffff, 0.9, 0, { map: X.corkCard(NM[f[0]] || f[0], f[1], lang) }), cx, cy, 2.270, { rz: rot * D2R });
      lsphere(root, 0.007, mat(0x1a4789, 0.4), cx, cy + 0.035, 2.265);
    } else if (i === freed.length && opts.nextTarget) {
      lplane(root, 0.15, 0.095, mat(0xffffff, 0.9, 0, { map: X.corkCard(opts.nextTarget, '', lang, { empty: true }) }), cx, cy, 2.2705, { rz: rot * D2R });
      lsphere(root, 0.007, mat(0x9a1e14, 0.4), cx, cy + 0.035, 2.265);
    } else {
      lsphere(root, 0.006, mat(0x9a7448, 0.5), cx, cy + 0.035, 2.268); // empty pin
    }
  }
  // ★ AI alarm board (0.48 x 0.32) with a mechanical counter, 10-step scale and a lamp
  const alarmV = opts.alarm ?? 4;
  lbox(root, 0.50, 0.34, 0.04, M.bakelite, 0.15, 1.33, 2.27);
  lplane(root, 0.48, 0.29, mat(0xffffff, 0.5, 0.2, { map: X.alarmBoard(alarmV, lang) }), 0.15, 1.33, 2.249);
  { const on = alarmV >= 8;
    lsphere(root, 0.045, new THREE.MeshStandardMaterial({ color: on ? 0xd03020 : 0x4a1410, emissive: on ? 0xf0443a : 0x000000, emissiveIntensity: on ? 1.8 : 0, roughness: 0.3 }), 0.15 + 0.165, 1.33 + 0.01, 2.246); }
  if (opts.tally) lplane(root, 0.5, 0.125, mat(0xffffff, 0.95, 0, { map: X.tally(opts.tally, lang === 'en' ? 'shifts' : 'vardiya'), transparent: true }), 0.15, 1.6, 2.296);

  // ceiling bulb with cage
  const bulbY = 2.02, bulbX = 0.12, bulbZ = 1.70;
  lcyl(root, 0.004, 2.5 - bulbY - 0.05, M.rubber, bulbX, (2.5 + bulbY + 0.05) / 2, bulbZ, { cast: false });
  lcyl(root, 0.018, 0.05, M.bakelite, bulbX, bulbY + 0.05, bulbZ);
  const bulbOn = opts.dim != null ? opts.dim : 1;
  lsphere(root, 0.032, new THREE.MeshStandardMaterial({ color: 0xfff0c8, emissive: 0xffd27a, emissiveIntensity: 2.5 * bulbOn + 0.05 }), bulbX, bulbY, bulbZ, { cast: false });
  for (let i = 0; i < 4; i++) { const t = new THREE.Mesh(new THREE.TorusGeometry(0.05, 0.002, 4, 20), M.steelDark); t.position.copy(U(bulbX, bulbY, bulbZ)); t.rotation.y = i * Math.PI / 4; root.add(t); }

  // ---------------- main desk
  const desk = group(root, 0, 0, 0, 0);
  lbox(desk, 1.84, 0.04, 1.10, mat(0xffffff, 0.72, 0, { map: T.linoTex() }), 0, 0.74, 1.33);
  lbox(desk, 1.86, 0.012, 0.012, M.brassDark, 0, 0.754, 0.775);
  lbox(desk, 1.84, 0.03, 1.10, M.steel, 0, 0.705, 1.33);
  for (const sx of [-0.71, 0.71]) {
    lbox(desk, 0.42, 0.66, 1.06, M.steel, sx, 0.36, 1.33);
    for (let i = 0; i < 3; i++) {
      const y = 0.56 - i * 0.2;
      lbox(desk, 0.38, 0.17, 0.01, M.steelDark, sx, y, 0.795);
      lbox(desk, 0.12, 0.012, 0.02, M.brassDark, sx, y + 0.04, 0.785);
    }
  }
  lbox(desk, 1.0, 0.6, 0.02, M.steelDark, 0, 0.40, 1.80);

  // ---------------- counter bank (sayaç rafı)
  if (opts.alt !== 'ledger') {
    const cb = group(desk, 0, 0, 0, 0);
    lbox(cb, 1.60, 0.195, 0.08, M.bakelite, 0, 0.8575, 1.84);
    lbox(cb, 1.60, 0.012, 0.09, M.brassDark, 0, 0.958, 1.835);
    // wedge whose front face is the 45° slope
    const wedge = lbox(cb, 1.60, 0.212, 0.10, M.bakelite, 0, 0.830, 1.775, { rx: -45 * D2R });
    const vals = st.counters;
    const faceMap = X.counterFace6(vals, lang);
    const faceMat = new THREE.MeshStandardMaterial({ map: faceMap, roughness: 0.45, metalness: 0.35, emissive: 0xffffff, emissiveMap: faceMap, emissiveIntensity: 0.16 });
    const face = lplane(cb, 1.56, 0.212, faceMat, 0, 0.8657, 1.7388, { rx: -45 * D2R });
    // raised brass bezels per module
    for (let i = 0; i < 6; i++) {
      const mx = -0.78 + 0.015 + i * 0.25667 + 0.12333;
      const fr = new THREE.Group(); fr.position.copy(U(0, 0.865, 1.7395)); fr.rotation.x = -45 * D2R; cb.add(fr);
      const wy = 0.106 - 0.139, wh = 0.132, ww = 0.2327;
      for (const [w, h, x, y] of [[ww, 0.006, mx, wy + wh / 2], [ww, 0.006, mx, wy - wh / 2], [0.006, wh, mx - ww / 2, wy], [0.006, wh, mx + ww / 2, wy]]) {
        const b = new THREE.Mesh(new THREE.BoxGeometry(w, h, 0.008), M.brass); b.position.set(x, y, 0.004); b.castShadow = true; fr.add(b);
      }
    }
  } else {
    // İLK TUR alternatifi (opts.alt = 'ledger'; reddedildi, TAŞIMA): open ledger on a wooden lectern with a brass plate
    const lg = group(desk, 0, 0, 0, 0);
    lbox(lg, 0.80, 0.15, 0.08, M.wood, 0, 0.835, 1.84);
    lbox(lg, 0.80, 0.212, 0.10, M.wood, 0, 0.830, 1.775, { rx: -45 * D2R });
    lbox(lg, 0.68, 0.23, 0.01, mat(0x5a1a12, 0.6, 0.0), 0, 0.8622, 1.7423, { rx: -45 * D2R });
    lplane(lg, 0.64, 0.206, new THREE.MeshStandardMaterial({ map: ALT.ledgerTex(lang), roughness: 0.9, emissive: 0xffffff, emissiveMap: ALT.ledgerTex(lang), emissiveIntensity: 0.08 }), 0, 0.8672, 1.7373, { rx: -45 * D2R });
    lplane(lg, 0.30, 0.03, new THREE.MeshStandardMaterial({ map: T.plateTex([lang === 'en' ? 'SHIFT LEDGER' : 'VARDİYA DEFTERİ'], 0.30, 0.03), roughness: 0.4, metalness: 0.75 }), 0, 0.776, 1.6795, { rx: -45 * D2R });
  }
  A.counterFace = [U(-0.78, 0.79, 1.665), U(0.78, 0.79, 1.665), U(0.78, 0.94, 1.815), U(-0.78, 0.94, 1.815)];

  // ---------------- map paper (only when baked)
  A.map = [U(-0.60, 0.7612, 0.98), U(0.60, 0.7612, 0.98), U(0.60, 0.7612, 1.655), U(-0.60, 0.7612, 1.655)];
  if (opts.mapTexture) {
    const mm = new THREE.MeshBasicMaterial({ map: opts.mapTexture });
    lplane(desk, 1.20, 0.675, mm, 0, 0.7612, 1.3175, { rx: -Math.PI / 2, receive: false });
  }

  // ---------------- team figure(s) on the map (Alfa): brass oval base, three pawns, letter plate
  A.teamFigs = [];
  for (const tf of (opts.teams || [])) {
    const [px, py] = tf.pos;
    const fig = buildTeamFigure(desk, tf);
    fig.position.copy(U(-0.60 + px / 1920 * 1.20, 0.7612 + 0.0004 + (tf.lift || 0), 1.655 - py / 1080 * 0.675));
    if (tf.lift) { fig.rotation.z = -6 * D2R; fig.rotation.x = 4 * D2R; }
    if (tf.lift && tf.lift > 0.01) {
      const sh = new THREE.Mesh(new THREE.CircleGeometry(0.036, 32), new THREE.MeshBasicMaterial({ color: 0x000000, transparent: true, opacity: 0.28, depthWrite: false }));
      sh.scale.set(1, 0.68, 1); sh.rotation.x = -Math.PI / 2; sh.position.copy(U(-0.60 + px / 1920 * 1.20 + 0.012, 0.7616, 1.655 - py / 1080 * 0.675 + 0.010)); desk.add(sh);
    }
    if (tf.ring) {
      const ring = new THREE.Mesh(new THREE.RingGeometry(0.037, 0.0425, 48), new THREE.MeshBasicMaterial({ color: 0xf5d646, transparent: true, opacity: 0.95, depthWrite: false }));
      ring.scale.set(1, 0.68, 1); ring.rotation.x = -Math.PI / 2; ring.position.copy(U(-0.60 + px / 1920 * 1.20, 0.7617, 1.655 - py / 1080 * 0.675)); desk.add(ring);
    }
    A.teamFigs.push({ group: fig, top: U(-0.60 + px / 1920 * 1.20, 0.7612 + 0.04 + (tf.lift || 0), 1.655 - py / 1080 * 0.675), base: U(-0.60 + px / 1920 * 1.20, 0.7612, 1.655 - py / 1080 * 0.675) });
  }

  // ---------------- key rail
  const rail = new THREE.Group(); rail.position.copy(U(-0.06, 0.775, 0.865)); rail.rotation.x = 12 * D2R; desk.add(rail);
  { const b = new THREE.Mesh(new THREE.BoxGeometry(1.40, 0.03, 0.15), M.bakelite); b.position.x = 0.01; b.castShadow = b.receiveShadow = true; rail.add(b); }
  { const b = new THREE.Mesh(new THREE.BoxGeometry(1.41, 0.008, 0.008), M.brassDark); b.position.set(0.01, 0.012, 0.075); rail.add(b); }
  let keyX = [-0.62, -0.394, -0.168, 0.058, 0.284, 0.51].map(v => v + 0.075);
  let SK = X.S2[lang];
  if (opts.doorBox) { keyX = [-0.58, -0.33, -0.08, 0.17, 0.42].map(v => v - 0.015); SK = { keys: SK.keys.filter((k, i) => i !== 3), keyCaps: SK.keyCaps.filter((k, i) => i !== 3) }; }
  const active = opts.activeKey ?? 3;
  A.keys = [];
  keyX.forEach((kx, i) => {
    const small = i === keyX.length - 1;
    const capX = kx - 0.075;
    const pressed = i === active;
    const bez = new THREE.Mesh(new THREE.BoxGeometry(0.072, 0.008, 0.072), M.brass); bez.position.set(capX, 0.017, 0.0); bez.castShadow = true; rail.add(bez);
    const capMat = [M.bakelite, M.bakelite, new THREE.MeshStandardMaterial({ map: T.keyCapTex(SK.keyCaps[i]), roughness: 0.35 }), M.bakelite, M.bakelite, M.bakelite];
    const cap = new THREE.Mesh(new THREE.BoxGeometry(0.056, 0.03, 0.056), capMat);
    cap.position.set(capX, pressed ? 0.024 : 0.034, 0.0); cap.castShadow = true; rail.add(cap);
    // jewel lamp
    const lit = pressed || (i === 3 && opts.doorWaiting && !opts.doorBox);
    const jm = new THREE.MeshStandardMaterial({ color: lit ? 0xffb347 : 0x4a2f12, emissive: lit ? 0xff9a2a : 0x000000, emissiveIntensity: lit ? (i === 3 ? 3.4 : 2.4) : 0, roughness: 0.2 });
    if (i === 3 && lit) { const halo = new THREE.Mesh(new THREE.RingGeometry(0.013, 0.02, 24), new THREE.MeshBasicMaterial({ color: 0xffb347, transparent: true, opacity: 0.55, depthWrite: false, side: THREE.DoubleSide })); halo.rotation.x = -Math.PI / 2; halo.position.set(capX, 0.0215, -0.05); rail.add(halo); }
    const j = new THREE.Mesh(new THREE.SphereGeometry(0.009, 16, 10), jm); j.position.set(capX, 0.02, -0.05); rail.add(j);
    const jb = new THREE.Mesh(new THREE.CylinderGeometry(0.012, 0.012, 0.008, 16), M.brass); jb.position.set(capX, 0.016, -0.05); rail.add(jb);
    // label plate
    const pw = small ? 0.10 : 0.128;
    const pl = new THREE.Mesh(new THREE.PlaneGeometry(pw, 0.05), new THREE.MeshStandardMaterial({ map: T.plateTex(SK.keys[i], pw, 0.05), roughness: 0.4, metalness: 0.75 }));
    pl.rotation.x = -Math.PI / 2; pl.position.set(capX + 0.045 + pw / 2, 0.0156, 0.0); pl.receiveShadow = true; rail.add(pl);
    A.keys.push({ cap: [capX, 0.034, 0], plate: [capX + 0.045 + pw / 2, 0.016, 0, pw] });
    if (i === 3 && opts.doorWaiting && opts.doorTag !== false && !opts.doorBox) { // hand-written tag tied to the door key
      const tg = new THREE.Mesh(new THREE.PlaneGeometry(0.11, 0.045), new THREE.MeshStandardMaterial({ map: X.paperLines([[lang === 'en' ? 'someone at' : 'kapıda biri', 46, '#1A2F5A', '700'], [lang === 'en' ? 'the door' : 'bekliyor', 46, '#1A2F5A', '700']], 440, 180, { top: 66, bg: '#f2e27a' }), roughness: 0.9, side: THREE.DoubleSide }));
      tg.position.set(capX + 0.01, 0.006, -0.105); tg.rotation.x = -Math.PI / 2 + 0.25; tg.rotation.z = 0.22; rail.add(tg);
    }
  });
  A.rail = rail;

  // ---------------- shift switch housing
  if (opts.alt !== 'stamp') {
    const sw = group(desk, 0.765, 0.76, 0.885, 10);
    lbox(sw, 0.30, 0.09, 0.20, M.olive, 0, 0.045, 0);
    lbox(sw, 0.31, 0.01, 0.21, M.steelDark, 0, 0.005, 0);
    lbox(sw, 0.30, 0.012, 0.075, M.olive, 0, 0.085, -0.075, { rx: 32 * D2R });
    const swPlate = lplane(sw, 0.27, 0.062, new THREE.MeshStandardMaterial({ map: T.plateTex([S.sw1, S.sw2], 0.27, 0.062), roughness: 0.4, metalness: 0.75 }), 0, 0.0925, -0.081, { rx: -58 * D2R });
    lbox(sw, 0.13, 0.016, 0.13, M.brassDark, 0.02, 0.098, 0.02);
    // lever
    const open = !!opts.switchOpen;
    const lever = new THREE.Group(); lever.position.set(0.02, 0.106, -0.01); sw.add(lever);
    lever.rotation.x = open ? 35 * D2R : -35 * D2R; // closed: lever up/back (shift running) ; open: flipped toward operator
    { const l = new THREE.Mesh(new THREE.CylinderGeometry(0.007, 0.009, 0.075, 12), M.chrome); l.position.y = 0.0375; l.castShadow = true; lever.add(l);
      const k = new THREE.Mesh(new THREE.SphereGeometry(0.011, 12, 8), M.chrome); k.position.y = 0.077; lever.add(k); }
    // red cover, hinged at the back edge
    const hinge = new THREE.Group(); hinge.position.set(0.02, 0.106, -0.075); sw.add(hinge);
    hinge.rotation.x = open ? -112 * D2R : 0;
    { const coverTex = T.stencilTex([lang === 'en' ? 'SHIFT' : 'VARDİYA'], 0.13, 0.04, { color: '#efe2cf', chips: 40, ls: 0.1 });
      const coverM = mat(0xb8241a, 0.42, 0.05); const top = new THREE.Mesh(new THREE.BoxGeometry(0.085, 0.006, 0.14), coverM); top.position.set(0, 0.092, 0.07); top.castShadow = true; hinge.add(top);
      const lab = new THREE.Mesh(new THREE.PlaneGeometry(0.075, 0.024), new THREE.MeshStandardMaterial({ map: coverTex, transparent: true, roughness: 0.5 }));
      lab.rotation.x = -Math.PI / 2; lab.rotation.z = Math.PI / 2; lab.position.set(0, 0.0955, 0.075); hinge.add(lab);
      for (const sx of [-0.0395, 0.0395]) { const s = new THREE.Mesh(new THREE.BoxGeometry(0.006, 0.09, 0.14), coverM); s.position.set(sx, 0.045, 0.07); s.castShadow = true; hinge.add(s); }
      const fr = new THREE.Mesh(new THREE.BoxGeometry(0.085, 0.09, 0.006), coverM); fr.position.set(0, 0.045, 0.137); hinge.add(fr);
      const fr2 = new THREE.Mesh(new THREE.BoxGeometry(0.085, 0.09, 0.006), coverM); fr2.position.set(0, 0.045, 0.003); hinge.add(fr2); }
    if (opts.amberSwitch) { const al = new THREE.PointLight(0xffb060, 0.35, 0.8, 2); al.position.set(-0.05, 0.25, 0.05); sw.add(al); }
    // ready lamp
    lcyl(sw, 0.013, 0.01, M.brass, -0.10, 0.095, 0.03);
    lsphere(sw, 0.01, new THREE.MeshStandardMaterial({ color: 0x7fe08f, emissive: 0x3fbf5f, emissiveIntensity: opts.switchLamp === false ? 0 : 1.6, roughness: 0.2 }), -0.10, 0.101, 0.03, { cast: false });
  } else {
    // İLK TUR alternatifi (opts.alt = 'stamp'; reddedildi, TAŞIMA): shift log book + rubber stamp on an ink pad
    const sg = group(desk, 0.765, 0.76, 0.885, 10);
    lbox(sg, 0.34, 0.014, 0.24, mat(0x5a1a12, 0.6, 0.0), -0.04, 0.007, 0);
    lplane(sg, 0.32, 0.22, new THREE.MeshStandardMaterial({ map: ALT.logPageTex(lang), roughness: 0.9 }), -0.04, 0.0145, 0, { rx: -Math.PI / 2 });
    lplane(sg, 0.16, 0.012, new THREE.MeshStandardMaterial({ map: T.plateTex([lang === 'en' ? 'SHIFT LOG' : 'VARDİYA DEFTERİ'], 0.16, 0.012), roughness: 0.4, metalness: 0.75 }), -0.04, 0.007, -0.1206);
    lbox(sg, 0.11, 0.014, 0.075, M.steelDark, 0.20, 0.007, 0.03);
    lplane(sg, 0.095, 0.06, mat(0x2a1e3a, 0.4, 0.0), 0.20, 0.0145, 0.03, { rx: -Math.PI / 2 });
    lbox(sg, 0.07, 0.02, 0.045, M.woodLight, 0.20, 0.025, 0.03);
    lcyl(sg, 0.012, 0.06, M.wood, 0.20, 0.065, 0.03);
    lsphere(sg, 0.022, M.wood, 0.20, 0.105, 0.03);
    lcyl(sg, 0.005, 0.15, mat(0x1c1c1a, 0.4, 0.3), -0.02, 0.019, -0.03, { rz: Math.PI / 2, ry: 0.4 });
  }
  if (opts.doorBox) { // Revizyon 2 · Karar 2 · B (seçilmedi, TAŞIMA): annunciator box with a drop flag next to the switch
    const bx = group(desk, 0.535, 0.76, 0.86, 8);
    lbox(bx, 0.12, 0.085, 0.07, M.bakelite, 0, 0.0425, 0);
    lbox(bx, 0.124, 0.006, 0.074, M.brass, 0, 0.088, 0);
    { const ft = X.paperLines([[lang === 'en' ? 'DOOR' : 'KAPI', 92, '#1E1B16']], 300, 120, { bg: opts.doorWaiting ? '#F5D646' : '#3a2a1c', top: 96 }); lplane(bx, 0.09, 0.036, new THREE.MeshStandardMaterial({ map: ft, emissive: 0xffffff, emissiveMap: ft, emissiveIntensity: opts.doorWaiting ? 0.55 : 0, roughness: 0.6 }), 0, 0.05, -0.0355); }
    lsphere(bx, 0.01, new THREE.MeshStandardMaterial({ color: opts.doorWaiting ? 0xffb347 : 0x4a2f12, emissive: opts.doorWaiting ? 0xff9a2a : 0, emissiveIntensity: 2.4 }), 0.045, 0.075, -0.036);
  }
  A.switchPlate = [U(0.765 - 0.135, 0.76 + 0.015, 0.885 - 0.1005), U(0.765 + 0.135, 0.76 + 0.015, 0.885 - 0.1005)];

  // ---------------- teleprinter
  const tp = group(desk, -0.77, 0.76, 1.20, -12);
  lbox(tp, 0.30, 0.16, 0.36, M.oliveLight, 0, 0.08, 0);
  lbox(tp, 0.30, 0.035, 0.20, M.oliveLight, 0, 0.177, -0.06, { rx: 10 * D2R });
  lbox(tp, 0.15, 0.004, 0.07, mat(0x20281f, 0.25, 0.3), 0.04, 0.1955, -0.07, { rx: 10 * D2R, cast: false });
  lbox(tp, 0.05, 0.012, 0.05, M.bakelite, -0.09, 0.20, -0.08);
  lcyl(tp, 0.045, 0.22, M.paperWhite, 0, 0.215, 0.12, { rz: Math.PI / 2 });
  for (const sx of [-0.12, 0.12]) lbox(tp, 0.012, 0.07, 0.06, M.steelDark, sx, 0.19, 0.12);
  lplane(tp, 0.12, 0.03, new THREE.MeshStandardMaterial({ map: T.plateTex(X.S2[lang].telexPlate, 0.12, 0.03), roughness: 0.4, metalness: 0.75 }), 0.08, 0.152, -0.183);
  // ★ front sheet: torn-off last message hung on a clip over the front panel (faces the operator)
  if (opts.telexHang) {
    const hs = lplane(tp, 0.26, 0.19, new THREE.MeshStandardMaterial({ map: X.telexHang(opts.telexHang), roughness: 0.92, side: THREE.DoubleSide }), -0.0, 0.105, -0.19, { rx: -3 * D2R });
    lbox(tp, 0.06, 0.016, 0.012, M.chrome, 0.0, 0.198, -0.192);
    A.telexHang = { group: tp, w: 0.26, h: 0.19, cy: 0.105, z: 0.19 };
  }
  // paper: down the front, then across the desk toward the operator, curling over the edge
  const telexLines = opts.telexLines || S.telex;
  const ttex = T.telexTex(lang, telexLines, { h: 1100 });
  const paperM = new THREE.MeshStandardMaterial({ map: ttex, roughness: 0.92, side: THREE.DoubleSide });
  // front drop segment (vertical) — uses top part of texture
  const segA = new THREE.Mesh(new THREE.PlaneGeometry(0.2, 0.17), paperM.clone());
  segA.material.map = ttex.clone(); segA.material.map.needsUpdate = true;
  segA.material.map.repeat.set(1, 0.17 / 0.55); segA.material.map.offset.set(0, 1 - 0.17 / 0.55);
  segA.position.set(0, 0.085, 0.183); tp.add(segA);
  // flat on desk: from the teleprinter foot (local z -0.18) toward the operator
  const flatLen = 0.24;
  const segB = new THREE.Mesh(new THREE.PlaneGeometry(0.2, flatLen), paperM.clone());
  segB.material.map = ttex.clone(); segB.material.map.needsUpdate = true;
  segB.material.map.repeat.set(1, flatLen / 0.55); segB.material.map.offset.set(0, 1 - (0.17 + flatLen) / 0.55);
  segB.rotation.x = -Math.PI / 2; segB.position.set(0, 0.0025, 0.18 + flatLen / 2); segB.receiveShadow = true; tp.add(segB);
  A.telexFlat = { group: tp, w: 0.2, len: flatLen };

  if (opts.stage === 'late') {
    // long telex tail spilling over the desk edge + a stack of torn sheets
    const tail = group(desk, -0.70, 0.7625, 1.0, -18);
    lplane(tail, 0.2, 0.42, new THREE.MeshStandardMaterial({ map: T.telexTex(lang, (opts.telexHang || []).map(l => [l[0], l[1].t, l[2]]).concat(T.STR[lang].telex), { h: 1100 }), roughness: 0.92, side: THREE.DoubleSide }), 0, 0.001, 0.0, { rx: -Math.PI / 2 });
    for (let i = 0; i < 9; i++) lbox(desk, 0.2, 0.002, 0.26, M.paper, -0.80, 0.762 + i * 0.0022, 0.92, { ry: 0.3 + (i % 3 - 1) * 0.06 });
    // second mug + tin can
    const m2 = group(desk, -0.70, 0.76, 1.66, 0); lcyl(m2, 0.04, 0.09, M.enamel, 0, 0.045, 0); const r2 = new THREE.Mesh(new THREE.TorusGeometry(0.039, 0.003, 6, 24), mat(0x2f4e7a, 0.4)); r2.rotation.x = Math.PI / 2; r2.position.y = 0.09; m2.add(r2);
    lcyl(desk, 0.033, 0.1, M.galv, 0.66, 0.81, 1.12);
  }
  // ---------------- desk lamp (banker)
  const lamp = group(desk, 0.84, 0.76, 1.61, 30);
  lcyl(lamp, 0.075, 0.022, M.brass, 0, 0.011, 0);
  lcyl(lamp, 0.008, 0.30, M.brass, 0, 0.17, 0);
  lbox(lamp, 0.02, 0.012, 0.11, M.brass, 0, 0.31, -0.05);
  const shade = new THREE.Mesh(new THREE.CylinderGeometry(0.075, 0.075, 0.30, 32, 1, true, 0, Math.PI), new THREE.MeshStandardMaterial({ color: 0x1e5236, roughness: 0.18, metalness: 0.1, side: THREE.DoubleSide }));
  shade.rotation.z = Math.PI / 2; shade.rotation.x = 0; shade.position.set(0, 0.33, 0.10); shade.castShadow = true; lamp.add(shade);
  shade.rotation.y = 0;
  // shade tilt toward operator
  const shadeHolder = new THREE.Group(); shadeHolder.position.set(0, 0.33, 0.10); lamp.add(shadeHolder); lamp.remove(shade); shade.position.set(0, 0, 0); shadeHolder.add(shade); shadeHolder.rotation.x = -10 * D2R;
  const inner = new THREE.Mesh(new THREE.CylinderGeometry(0.072, 0.072, 0.29, 32, 1, true, 0, Math.PI), new THREE.MeshStandardMaterial({ color: 0xfff1d0, emissive: 0xffd9a0, emissiveIntensity: 0.45, side: THREE.BackSide }));
  inner.rotation.z = Math.PI / 2; shadeHolder.add(inner);
  for (const sx of [-0.152, 0.152]) { const cap = new THREE.Mesh(new THREE.CylinderGeometry(0.076, 0.076, 0.006, 32, 1, false, 0, Math.PI), M.brass); cap.rotation.z = Math.PI / 2; cap.position.x = sx; shadeHolder.add(cap); }
  const lampBulb = new THREE.Mesh(new THREE.CylinderGeometry(0.012, 0.012, 0.12, 12), new THREE.MeshStandardMaterial({ color: 0xffffff, emissive: 0xffe2b0, emissiveIntensity: 1.6 })); lampBulb.rotation.z = Math.PI / 2; lampBulb.position.y = 0.022; shadeHolder.add(lampBulb);
  if (opts.noLamp) lamp.visible = false;
  // pull chain
  lcyl(lamp, 0.0015, 0.08, M.brass, 0.05, 0.27, 0.06, { cast: false });

  // ---------------- right side desk props
  // tray
  const tray = group(desk, 0.765, 0.76, 1.30, -4);
  lbox(tray, 0.26, 0.006, 0.33, M.steelDark, 0, 0.003, 0);
  for (const [w, h, d, x, z] of [[0.26, 0.05, 0.006, 0, -0.165], [0.26, 0.05, 0.006, 0, 0.165], [0.006, 0.05, 0.33, -0.13, 0], [0.006, 0.05, 0.33, 0.13, 0]]) lbox(tray, w, h, d, M.steelDark, x, 0.025, z);
  lbox(tray, 0.22, 0.02, 0.29, M.paper, 0, 0.016, 0.005);
  lbox(tray, 0.21, 0.004, 0.28, M.paperWhite, 0.008, 0.028, -0.004, { ry: 3 * D2R });
  if (opts.stage === 'late') for (let i = 0; i < 10; i++) lbox(tray, 0.21, 0.0035, 0.28, i % 3 ? M.paper : M.paperWhite, (i % 2 ? 0.006 : -0.004), 0.031 + i * 0.0036, (i % 3 - 1) * 0.004, { ry: (i % 4 - 1.5) * 1.5 * D2R });
  lplane(tray, 0.10, 0.025, new THREE.MeshStandardMaterial({ map: T.plateTex(S.tray, 0.10, 0.025), roughness: 0.4, metalness: 0.75 }), 0, 0.03, -0.1695);
  // stamps rack
  const stamps = group(desk, -0.76, 0.76, 1.55, 8);
  lbox(stamps, 0.17, 0.022, 0.07, M.wood, 0, 0.011, 0);
  for (const [sx, inkCol] of [[-0.045, 0x1a4789], [0.045, 0x1e1b16]]) {
    lbox(stamps, 0.055, 0.022, 0.04, M.woodLight, sx, 0.035, 0);
    lbox(stamps, 0.055, 0.004, 0.04, mat(inkCol, 0.8), sx, 0.023, 0);
    lcyl(stamps, 0.009, 0.04, M.woodLight, sx, 0.066, 0);
    lsphere(stamps, 0.013, M.woodLight, sx, 0.092, 0);
  }
  // mug
  const mug = group(desk, 0.88, 0.76, 1.08, 0);
  lcyl(mug, 0.042, 0.095, M.enamel, 0, 0.0475, 0);
  lcyl(mug, 0.038, 0.002, mat(0x2a1a10, 0.3), 0, 0.085, 0, { cast: false });
  const rim = new THREE.Mesh(new THREE.TorusGeometry(0.041, 0.003, 6, 24), mat(0x2f4e7a, 0.4)); rim.rotation.x = Math.PI / 2; rim.position.set(0, 0.095, 0); mug.add(rim);
  const handle = new THREE.Mesh(new THREE.TorusGeometry(0.022, 0.006, 6, 16, Math.PI), M.enamel); handle.rotation.z = -Math.PI / 2; handle.position.set(0.042, 0.05, 0); mug.add(handle);
  // pencil + shavings + eraser (outside the map, near the rail)
  const pen = group(desk, 0.40, 0.7625, 0.945, 62);
  lcyl(pen, 0.0045, 0.16, mat(0xd9a520, 0.5), 0, 0.0045, 0, { rx: Math.PI / 2, seg: 6 });
  lcyl(pen, 0.0045, 0.02, mat(0xc9a77a, 0.6), 0, 0.0045, -0.09, { rx: -Math.PI / 2, r2: 0.0005, seg: 6 });
  for (let i = 0; i < 7; i++) { const s = new THREE.Mesh(new THREE.TorusGeometry(0.006 + (i % 3) * 0.002, 0.0015, 4, 10, Math.PI * 1.4), mat(0x9a7448, 0.8)); s.position.copy(U(0.47 + (i % 4) * 0.012, 0.764, 0.905 + Math.floor(i / 4) * 0.014)); s.rotation.x = Math.PI / 2; s.rotation.z = i; root.add(s); }
  lbox(root, 0.045, 0.012, 0.022, mat(0xc79b92, 0.8), 0.28, 0.767, 0.945, { ry: 20 * D2R });

  // ---------------- left shelf with section model
  const shelf = group(root, -1.50, 0, 1.72, -35);
  A.shelfGroup = shelf;
  for (const [x, z] of [[-0.6, -0.21], [0.6, -0.21], [-0.6, 0.21], [0.6, 0.21]]) lbox(shelf, 0.035, 2.0, 0.035, M.steelDark, x, 1.0, z);
  const levels = [0.14, 0.78, 1.52, 1.96];
  for (const y of levels) { lbox(shelf, 1.24, 0.025, 0.44, M.steel, 0, y - 0.0125, 0); lbox(shelf, 1.24, 0.03, 0.01, M.steelDark, 0, y - 0.015, -0.222); }
  lplane(shelf, 0.25, 0.032, new THREE.MeshStandardMaterial({ map: T.plateTex(S.shelfPlate, 0.25, 0.032), roughness: 0.4, metalness: 0.75 }), 0.20, 0.763, -0.2265);
  // upper shelf supplies
  const cr1 = lbox(shelf, 0.30, 0.20, 0.26, [M.wood, M.wood, M.wood, M.wood, mat(0xffffff, 0.8, 0, { map: T.crateTex(S.crate1) }), M.wood], -0.36, 1.62, 0.0);
  cr1.material = [M.wood, M.wood, M.wood, M.wood, mat(0xffffff, 0.8, 0, { map: T.crateTex(S.crate1) }), M.wood];
  const cr2 = new THREE.Mesh(new THREE.BoxGeometry(0.30, 0.17, 0.26), [M.wood, M.wood, M.wood, M.wood, mat(0xffffff, 0.8, 0, { map: T.crateTex(S.crate2) }), M.wood]);
  cr2.position.set(-0.02, 1.605, 0); cr2.castShadow = cr2.receiveShadow = true; shelf.add(cr2);
  lbox(shelf, 0.16, 0.25, 0.09, M.olive, 0.25, 1.645, 0.02);
  lbox(shelf, 0.05, 0.03, 0.03, M.olive, 0.21, 1.785, 0.02);
  // hope sign: tin with a sprout
  lcyl(shelf, 0.035, 0.065, M.galv, 0.46, 1.5525, -0.06);
  lcyl(shelf, 0.032, 0.004, M.soil1, 0.46, 1.585, -0.06, { cast: false });
  { const n = opts.stage === 'late' ? 11 : 3, hgt = opts.stage === 'late' ? 0.13 : 0.035;
    if (opts.stage === 'late') lcyl(shelf, 0.003, hgt, mat(0x4f7a3a, 0.7), 0.46, 1.585 + hgt / 2, -0.06);
    for (let i = 0; i < n; i++) { const a = i * 2.4; const l = lplane(shelf, opts.stage === 'late' ? 0.035 : 0.02, opts.stage === 'late' ? 0.05 : 0.035, mat(0x5f8f4a, 0.7, 0, { side: THREE.DoubleSide }), 0.46 + Math.cos(a) * (opts.stage === 'late' ? 0.025 : 0.008), 1.60 + (opts.stage === 'late' ? i * 0.011 : i * 0.006), -0.06 + Math.sin(a) * 0.02, { rz: (i % 2 ? 0.7 : -0.7), ry: a }); l.castShadow = true; } }
  // bottom shelf
  lbox(shelf, 0.34, 0.22, 0.30, M.wood, -0.38, 0.25, 0.0);
  const fa = new THREE.Mesh(new THREE.BoxGeometry(0.26, 0.15, 0.12), [M.paperWhite, M.paperWhite, M.paperWhite, M.paperWhite, mat(0xffffff, 0.6, 0, { map: T.firstAidTex() }), M.paperWhite]);
  fa.position.set(0.05, 0.215, -0.05); fa.castShadow = fa.receiveShadow = true; shelf.add(fa);
  for (let i = 0; i < 4; i++) lbox(shelf, 0.05, 0.28, 0.22, mat([0x2e4a3a, 0x463f34, 0x524973, 0x7a3a30][i], 0.8), 0.32 + i * 0.06, 0.28, 0.0);

  // section model (diorama)
  const dio = new THREE.Group(); dio.position.set(0.20, 0.78, 0.01); shelf.add(dio);
  A.dioGroup = dio;
  buildDiorama2(dio, opts, A);
  // clipboard on a stand (right)
  const cbd = new THREE.Group(); cbd.position.set(-0.46, 0.86, -0.02); cbd.rotation.x = -8 * D2R; shelf.add(cbd);
  { const b = new THREE.Mesh(new THREE.BoxGeometry(0.40, 0.60, 0.008), mat(0x8b6b45, 0.7)); b.position.y = 0.30; b.castShadow = b.receiveShadow = true; cbd.add(b);
    const p = new THREE.Mesh(new THREE.BoxGeometry(0.37, 0.54, 0.002), M.paper); p.position.set(0, 0.275, 0.0055); p.receiveShadow = true; cbd.add(p);
    const clip = new THREE.Mesh(new THREE.BoxGeometry(0.13, 0.035, 0.018), M.chrome); clip.position.set(0, 0.575, 0.01); clip.castShadow = true; cbd.add(clip); }
  lbox(shelf, 0.36, 0.08, 0.06, M.wood, -0.46, 0.82, 0.07);
  A.clipPaper = { group: cbd, w: 0.37, h: 0.54, cy: 0.275, z: 0.0067 };
  // idle tray (BOŞTA) in front of the clipboard
  const idle = new THREE.Group(); idle.position.set(-0.46, 0.78, 0.155); shelf.add(idle);
  { const t = new THREE.Mesh(new THREE.BoxGeometry(0.26, 0.018, 0.07), M.wood); t.position.y = 0.009; t.castShadow = t.receiveShadow = true; idle.add(t);
    const lip = new THREE.Mesh(new THREE.BoxGeometry(0.26, 0.026, 0.006), M.woodLight); lip.position.set(0, 0.013, 0.035); idle.add(lip);
    const nIdle = st.idle ?? 4;
    for (let i = 0; i < nIdle; i++) peg(idle, -0.09 + i * 0.06, 0.018, 0.0, ['#3f5f8a', '#b07a2a', '#7a3a30', '#3e6b4a', '#6b5a8a'][i % 5]); }
  A.idleTray = { group: idle };

  // ---------------- CRT desk (right)
  const cd = group(root, 1.50, 0, 1.72, 35);
  lbox(cd, 0.95, 0.04, 0.58, M.olive, 0, 0.74, 0);
  lbox(cd, 0.34, 0.70, 0.54, M.steel, 0.28, 0.36, 0.0);
  for (const [x, z] of [[-0.44, -0.25], [-0.44, 0.25]]) lbox(cd, 0.035, 0.72, 0.035, M.steelDark, x, 0.36, z);
  const crt = new THREE.Group(); crt.position.set(-0.10, 0.76, -0.04); cd.add(crt);
  { const body = new THREE.Mesh(new THREE.BoxGeometry(0.42, 0.34, 0.36), M.crtCase); body.position.set(0, 0.17, 0); body.castShadow = body.receiveShadow = true; crt.add(body);
    const rear = new THREE.Mesh(new THREE.BoxGeometry(0.30, 0.26, 0.18), M.crtCase); rear.position.set(0, 0.16, -0.26); rear.castShadow = true; crt.add(rear);
    // bezel recess
    const bz = new THREE.Mesh(new THREE.BoxGeometry(0.34, 0.26, 0.01), mat(0x1c1f19, 0.6)); bz.position.set(0, 0.185, 0.181); crt.add(bz);
    const crtMap = opts.crtTexture || X.crtScreen(opts.crtLines || [['DİNLEMEDE', 150, '#a6e8b8', true], ['ALFA · HAZIR', 110], ['DOĞU AVR. / 02', 104], ['YZ ALARMI 4/10', 64, '#5e9c73']], { top: 190, reticle: false });
    const screen = new THREE.Mesh(new THREE.PlaneGeometry(0.30, 0.225), new THREE.MeshStandardMaterial({ color: 0x050a07, emissive: 0xffffff, emissiveMap: crtMap, emissiveIntensity: opts.crtOff ? 0 : 1.0, roughness: 0.9, metalness: 0.0 }));
    screen.position.set(0, 0.19, 0.187); crt.add(screen);
    const pl = new THREE.Mesh(new THREE.PlaneGeometry(0.20, 0.026), new THREE.MeshStandardMaterial({ map: T.plateTex(X.S2[lang].crtPlate, 0.20, 0.026), roughness: 0.4, metalness: 0.75 })); pl.position.set(0, 0.035, 0.1815); crt.add(pl);
    const led = new THREE.Mesh(new THREE.SphereGeometry(0.006, 10, 8), new THREE.MeshStandardMaterial({ color: 0x8fe3a8, emissive: 0x3fbf5f, emissiveIntensity: 1.5 })); led.position.set(0.17, 0.035, 0.184); crt.add(led);
    for (const kx of [0.13, 0.155]) { const k = new THREE.Mesh(new THREE.CylinderGeometry(0.008, 0.008, 0.012, 14), M.bakelite); k.rotation.x = Math.PI / 2; k.position.set(kx - 0.27, 0.035, 0.186); crt.add(k); }
    A.crtScreen = { mesh: screen, w: 0.30, h: 0.225 };
  }
  const radio = new THREE.Group(); radio.position.set(0.30, 0.76, 0.06); cd.add(radio);
  { const b = new THREE.Mesh(new THREE.BoxGeometry(0.30, 0.20, 0.20), mat(0x4a3020, 0.6)); b.position.y = 0.10; b.castShadow = b.receiveShadow = true; radio.add(b);
    const face = new THREE.Mesh(new THREE.PlaneGeometry(0.29, 0.193), new THREE.MeshStandardMaterial({ map: X.radioFace(lang), roughness: 0.6, emissive: 0xffffff, emissiveMap: X.radioFace(lang), emissiveIntensity: 0.06 })); face.position.set(0, 0.10, 0.1005); radio.add(face);
    for (const [kx, ky] of [[0.088, 0.03], [0.123, 0.03]]) { const k = new THREE.Mesh(new THREE.CylinderGeometry(0.014, 0.016, 0.02, 18), M.bakelite); k.rotation.x = Math.PI / 2; k.position.set(kx, ky + 0.012, 0.11); k.castShadow = true; radio.add(k); }
    const ant = new THREE.Mesh(new THREE.CylinderGeometry(0.002, 0.002, 0.35, 6), M.chrome); ant.position.set(0.12, 0.36, -0.06); ant.rotation.z = -0.3; radio.add(ant);
    const dl = new THREE.Mesh(new THREE.PlaneGeometry(0.135, 0.04), new THREE.MeshBasicMaterial({ color: 0xffb050, transparent: true, opacity: 0.25, depthWrite: false })); dl.position.set(0.069, 0.157, 0.1012); radio.add(dl); }
  A.radio = radio;
  // ★ desk microphone between CRT and radio
  const mic = new THREE.Group(); mic.position.set(0.13, 0.76, 0.17); cd.add(mic);
  { lcyl(mic, 0.045, 0.014, M.steelDark, 0, 0.007, 0); lcyl(mic, 0.006, 0.17, M.chrome, 0, 0.095, 0);
    const head = new THREE.Mesh(new THREE.CapsuleGeometry(0.022, 0.04, 6, 14), mat(0x1c1c1a, 0.4, 0.4)); head.position.set(0, 0.2, 0); head.rotation.x = 0.35; head.castShadow = true; mic.add(head);
    const grille = new THREE.Mesh(new THREE.TorusGeometry(0.022, 0.003, 6, 18), M.chrome); grille.position.set(0, 0.215, 0.006); grille.rotation.x = 0.35; mic.add(grille);
    lbox(mic, 0.02, 0.008, 0.012, mat(0xb8241a, 0.4), 0.03, 0.016, -0.03); }
  // ★ radio log book with entries + pencil
  const lb = new THREE.Group(); lb.position.set(-0.12, 0.761, 0.245); lb.rotation.y = 0.08; cd.add(lb);
  { lbox(lb, 0.22, 0.008, 0.16, mat(0x2e4a3a, 0.8), 0, 0.004, 0);
    lplane(lb, 0.21, 0.15, new THREE.MeshStandardMaterial({ map: X.logBook(lang, opts.logEntries || []), roughness: 0.9 }), 0, 0.0085, 0, { rx: -Math.PI / 2 });
    lcyl(lb, 0.004, 0.15, mat(0xd9a520, 0.5), 0.02, 0.012, -0.02, { rz: Math.PI / 2, ry: 0.5, seg: 6 }); }
  A.logBook = { group: lb, w: 0.21, h: 0.15 };
  // ★ door on the right wall (x = 2.2)
  A.door = buildDoor(root, opts);

  // ---------------- lights
  const lights = {};
  const hemi = new THREE.HemisphereLight(0x7f90a6, 0x16140f, opts.ambient ?? 0.6); scene.add(hemi); lights.hemi = hemi;
  const bulb = new THREE.PointLight(0xffcf86, (opts.bulbI ?? 4.2) * bulbOn, 0, 2);
  bulb.position.copy(U(bulbX, bulbY - 0.03, bulbZ)); bulb.castShadow = true; bulb.shadow.mapSize.set(1024, 1024); bulb.shadow.bias = -0.002; bulb.shadow.radius = 1.5; scene.add(bulb); lights.bulb = bulb;
  const spot = new THREE.SpotLight(0xffd9a0, opts.lampI ?? 9.0, 0, 0.82, 0.55, 2);
  const lp = new THREE.Vector3(); lampBulb.updateWorldMatrix(true, false); lampBulb.getWorldPosition(lp);
  spot.position.copy(lp); spot.target.position.copy(U(0.02, 0.76, 1.32)); scene.add(spot.target);
  spot.castShadow = true; spot.shadow.mapSize.set(2048, 2048); spot.shadow.bias = -0.0008; spot.shadow.radius = 1.2; scene.add(spot); lights.spot = spot;
  // crt spill
  const crtL = new THREE.PointLight(0x7fd9a0, opts.crtOff ? 0 : (opts.crtI ?? 0.8), 2.4, 2);
  const cp = new THREE.Vector3(); A.crtScreen.mesh.updateWorldMatrix(true, false); A.crtScreen.mesh.localToWorld(cp.set(0, -0.05, 0.42)); crtL.position.copy(cp); scene.add(crtL); lights.crt = crtL;
  // radio dial glow
  const rl = new THREE.PointLight(0xffa850, 0.12, 0.6, 2); const rp = new THREE.Vector3(); radio.localToWorld(rp.set(0.07, 0.14, 0.2)); rl.position.copy(rp); scene.add(rl);

  return { root, A, lights, desk, shelf, cd };
}

function buildTeamFigure(parent, tf) {
  const g = new THREE.Group(); parent.add(g);
  const base = new THREE.Mesh(new THREE.CylinderGeometry(0.034, 0.035, 0.006, 48), mat(0xa8844e, 0.5, 0.7));
  base.scale.z = 0.68; base.position.y = 0.003; base.castShadow = true; base.receiveShadow = true; g.add(base);
  const rim = new THREE.Mesh(new THREE.CylinderGeometry(0.0355, 0.0355, 0.0018, 48), mat(0x5c4826, 0.5, 0.6)); rim.scale.z = 0.68; rim.position.y = 0.0009; g.add(rim);
  // letter plate on the front half of the base (three +z = toward the chair)
  const pm = new THREE.MeshStandardMaterial({ map: T.teamPlateTex(tf.letter || 'A'), roughness: 0.8 });
  const plate = new THREE.Mesh(new THREE.CylinderGeometry(0.0105, 0.0105, 0.0022, 36), [M.bakelite, pm, M.bakelite]);
  plate.position.set(0, 0.0071, 0.0122); plate.rotation.y = Math.PI / 2 * (tf.plateRot ?? 1); plate.castShadow = true; g.add(plate);
  // three pawns: technician (blue), EMP specialist (amber), analyst (green)
  const bands = [0x3f5f8a, 0xb07a2a, 0x3e6b4a];
  [-0.019, 0, 0.019].forEach((x, i) => {
    const p = new THREE.Group(); g.add(p);
    const down = tf.state === 'injured' && i === 2;
    const body = new THREE.Mesh(new THREE.CylinderGeometry(0.0058, 0.0066, 0.016, 16), M.woodPale); body.position.y = 0.008; body.castShadow = true; p.add(body);
    const band = new THREE.Mesh(new THREE.CylinderGeometry(0.0062, 0.0066, 0.0042, 16), mat(bands[i], 0.6)); band.position.y = 0.0105; p.add(band);
    const head = new THREE.Mesh(new THREE.SphereGeometry(0.0062, 16, 12), mat(bands[i], 0.5)); head.position.y = 0.0205; head.castShadow = true; p.add(head);
    if (down) { p.rotation.z = Math.PI / 2; p.position.set(x + 0.004, 0.0062 + 0.006, -0.007); p.rotation.y = 0.5; }
    else p.position.set(x, 0.006, -0.007);
  });
  return g;
}
function peg(parent, x, y, z, band) {
  const b = new THREE.Mesh(new THREE.CylinderGeometry(0.0105, 0.012, 0.036, 14), M.woodPale); b.position.set(x, y + 0.018, -z); b.castShadow = true; parent.add(b);
  const h = new THREE.Mesh(new THREE.SphereGeometry(0.0105, 14, 10), M.woodPale); h.position.set(x, y + 0.046, -z); h.castShadow = true; parent.add(h);
  const c = new THREE.Mesh(new THREE.CylinderGeometry(0.0112, 0.0118, 0.009, 14), mat(band, 0.6)); c.position.set(x, y + 0.026, -z); parent.add(c);
}
function slot(parent, x, y, z) {
  const d = new THREE.Mesh(new THREE.CylinderGeometry(0.012, 0.012, 0.002, 18), M.brassDark); d.position.set(x, y + 0.001, -z); d.receiveShadow = true; parent.add(d);
}

function buildDiorama(g, opts, A) {
  const st = opts.state;
  const W = 0.80, H = 0.62, D = 0.34;
  const L = (w, h, d, m, x, y, z, o) => lbox(g, w, h, d, m, x, y, z, o);
  // outer frame
  L(0.02, H, D, M.wood, -W / 2 + 0.01, H / 2, 0);
  L(0.02, H, D, M.wood, W / 2 - 0.01, H / 2, 0);
  L(W, 0.02, D, M.wood, 0, 0.01, 0);
  L(W, H, 0.01, M.soil3, 0, H / 2, D / 2 - 0.005);
  // surface
  L(W - 0.04, 0.012, D - 0.02, M.rubble, 0, 0.566, 0);
  const ruins = [[-0.31, 0.05, 0.06], [-0.22, 0.03, 0.05], [-0.12, 0.065, 0.04], [0.05, 0.035, 0.07], [0.14, 0.02, 0.05], [0.31, 0.045, 0.05]];
  for (const [x, h, w] of ruins) L(w, h, 0.05, mat(0x3c3c38, 0.9), x, 0.572 + h / 2, 0.03 + (x * 7 % 0.05));
  L(0.035, 0.012, 0.012, mat(0x3c3c38, 0.9), -0.12, 0.64, 0.03, { rz: 0.5 });
  // AI mast with red beacon
  lcyl(g, 0.0025, 0.08, M.steelDark, 0.24, 0.612, -0.02);
  lsphere(g, 0.006, new THREE.MeshStandardMaterial({ color: 0xb8241a, emissive: 0xb8241a, emissiveIntensity: 2.0 }), 0.24, 0.655, -0.02, { cast: false });
  // soil strata
  L(W - 0.04, 0.025, D - 0.02, M.soil2, 0, 0.5475, 0, { cast: false });
  L(W - 0.04, 0.025, D - 0.02, M.soil1, 0, 0.5225, 0, { cast: false });
  L(W - 0.04, 0.02, D - 0.02, M.soil3, 0, 0.50, 0, { cast: false });
  // top slab
  L(W - 0.04, 0.02, D - 0.02, M.slab, 0, 0.48, 0);
  // fascias (card bands)
  L(W - 0.04, 0.065, D - 0.02, M.slab, 0, 0.0525, 0);       // bottom band y .02-.085
  L(W - 0.04, 0.065, D - 0.02, M.slab, 0, 0.2875, 0);       // middle band y .255-.32
  // walls
  for (const x of [-0.375, 0.375]) L(0.01, 0.39, D - 0.02, M.slab, x, 0.28, 0);
  for (const x of [-0.04, 0.04]) L(0.01, 0.39, D - 0.02, M.slab, x, 0.28, 0);
  // ladder in shaft
  for (const x of [-0.022, 0.022]) L(0.004, 0.39, 0.004, M.steelDark, x, 0.28, 0.10);
  for (let y = 0.1; y < 0.47; y += 0.03) L(0.044, 0.003, 0.003, M.steelDark, 0, y, 0.10);
  // rooms: [key, cx, floorY]
  const rooms = { yasam: [-0.2075, 0.32], jen: [0.2075, 0.32], atolye: [-0.2075, 0.085], revir: [0.2075, 0.085] };
  const roomH = 0.17, roomW = 0.325;
  A.rooms = {};
  for (const [k, [cx, fy]] of Object.entries(rooms)) {
    const r = st.rooms[k];
    // back wall + floor
    const wallCol = r.powered ? 0xcfc6ae : 0x5a554b;
    lplane(g, roomW, roomH, mat(wallCol, 0.9), cx, fy + roomH / 2, D / 2 - 0.012);
    L(roomW, 0.004, D - 0.03, mat(r.powered ? 0x77756b : 0x45433d, 0.9), cx, fy + 0.002, 0, { cast: false });
    // ceiling light
    const on = r.powered && r.level > 0;
    lsphere(g, 0.007, new THREE.MeshStandardMaterial({ color: on ? 0xfff0c8 : 0x3a3a34, emissive: on ? 0xffd27a : 0x000000, emissiveIntensity: on ? 2.5 : 0 }), cx, fy + roomH - 0.012, 0.02, { cast: false });
    if (on) { const pl = new THREE.PointLight(0xffd8a0, 0.06 * (opts.dioLight ?? 1), 0.45, 2); pl.position.set(cx, fy + roomH - 0.03, -0.0); g.add(pl); }
    // brass edge strip
    L(roomW, 0.004, 0.004, M.brassDark, cx, fy + 0.001, -D / 2 + 0.012, { cast: false });
    A.rooms[k] = {
      opening: [[cx - roomW / 2, fy, -D / 2 + 0.01], [cx + roomW / 2, fy, -D / 2 + 0.01], [cx + roomW / 2, fy + roomH, -D / 2 + 0.01], [cx - roomW / 2, fy + roomH, -D / 2 + 0.01]],
      card: [[cx - 0.095, fy - 0.0625, -D / 2 + 0.005], [cx + 0.095, fy - 0.0625, -D / 2 + 0.005], [cx + 0.095, fy - 0.0075, -D / 2 + 0.005], [cx - 0.095, fy - 0.0075, -D / 2 + 0.005]],
    };
    // card holder frame (brass)
    L(0.20, 0.004, 0.006, M.brass, cx, fy - 0.006, -D / 2 + 0.006, { cast: false });
    L(0.20, 0.004, 0.006, M.brass, cx, fy - 0.064, -D / 2 + 0.006, { cast: false });
    for (const sx of [-0.1, 0.1]) L(0.004, 0.062, 0.006, M.brass, cx + sx, fy - 0.035, -D / 2 + 0.006, { cast: false });
    if (opts.bakeCards) {
      const t = T.diaCardTex(r.name, r.line2, r.stamp);
      lplane(g, 0.19, 0.055, mat(0xffffff, 0.85, 0, { map: t }), cx, fy - 0.035, -D / 2 + 0.003);
    }
    // slots and figures
    const cap = r.cap || 0;
    for (let i = 0; i < cap; i++) {
      const sx = cx - 0.13 + (cap > 1 ? i * (0.26 / Math.max(cap - 1, 1)) : 0.13);
      slot(g, sx, fy + 0.004, -0.09);
      if (i < r.staff) peg(g, sx, fy + 0.006, -0.09, ['#3f5f8a', '#b07a2a', '#7a3a30', '#3e6b4a', '#6b5a8a', '#8a6a3f'][i % 6]);
    }
  }
  // props
  // yasam: bunks + table
  for (const bx of [-0.33, -0.21]) {
    for (const by of [0.335, 0.385]) L(0.10, 0.012, 0.055, M.blanket, bx, by, 0.09);
    for (const [px, pz] of [[-0.05, 0.065], [0.05, 0.065], [-0.05, 0.115], [0.05, 0.115]]) L(0.004, 0.08, 0.004, M.steelDark, bx + px, 0.36, pz);
  }
  L(0.05, 0.004, 0.04, M.wood, -0.10, 0.355, 0.07); L(0.004, 0.032, 0.004, M.wood, -0.10, 0.338, 0.07);
  // generator
  for (const gx of [0.12, 0.20, 0.28]) lcyl(g, 0.026, 0.09, M.genGreen, gx, 0.365, 0.08);
  lcyl(g, 0.006, 0.20, M.pipe, 0.20, 0.42, 0.08, { rz: Math.PI / 2 });
  L(0.04, 0.06, 0.02, M.steelDark, 0.34, 0.355, 0.12);
  // workshop: bench + pegboard + vise
  L(0.17, 0.006, 0.055, M.wood, -0.22, 0.13, 0.10);
  for (const px of [-0.295, -0.145]) L(0.006, 0.045, 0.05, M.wood, px, 0.107, 0.10);
  lplane(g, 0.16, 0.07, mat(0x9a7448, 0.9), -0.22, 0.19, D / 2 - 0.014);
  L(0.02, 0.015, 0.02, M.steelDark, -0.16, 0.14, 0.09);
  // infirmary (damaged): cot, cabinet with first-aid sign, rubble, fallen beam, crack
  L(0.12, 0.018, 0.05, M.paperWhite, 0.16, 0.105, 0.09, { rz: 0.06 });
  L(0.05, 0.09, 0.04, M.paperWhite, 0.32, 0.13, 0.11);
  lplane(g, 0.03, 0.03, mat(0xffffff, 0.6, 0, { map: T.firstAidTex() }), 0.32, 0.14, 0.089);
  const rb = [[0.22, 0.095, -0.02, 0.03, 0.02], [0.26, 0.092, 0.02, 0.025, 0.018], [0.12, 0.093, 0.03, 0.02, 0.015], [0.29, 0.094, -0.06, 0.028, 0.02], [0.18, 0.091, -0.07, 0.022, 0.014], [0.24, 0.1, 0.0, 0.018, 0.016]];
  rb.forEach(([x, y, z, s, h], i) => L(s, h, s * 0.9, M.rubble, x, y, z, { ry: i * 0.7, rz: i * 0.3 }));
  L(0.24, 0.016, 0.016, M.wood, 0.20, 0.16, -0.01, { rz: -0.42 });
  lplane(g, 0.012, 0.12, mat(0x2a2620, 0.9), 0.25, 0.17, D / 2 - 0.0135, { rz: 0.4 });
  lplane(g, 0.01, 0.07, mat(0x2a2620, 0.9), 0.21, 0.20, D / 2 - 0.0135, { rz: -0.5 });
}
