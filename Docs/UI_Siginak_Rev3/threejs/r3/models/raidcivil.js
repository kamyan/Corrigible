// family: raidcivil · Rev3 model library (see DESIGN_R3.md §3.9)
// Raid board: 1 tile = 1 unit, origin = tile centre on the board (y = 0), Unity coordinates (x east, y up, z north).
// Camera: orthographic, pitch 35°, yaw 45°, looking toward +x +z → visible faces are TOP, WEST (−x) and SOUTH (−z).
// Civilians: cream body with the three civil face keys, blue band, blue ring + cross on top (Ø 0.32); region equipment
// sits in a ≤ 0.066 slab in front of the west/south faces (body 0.588 pushed 0.03 toward +x +z: band back face at
// +0.33, equipment front ≤ −0.33, so the whole piece stays inside a centred 0.66 square).
// Entry doors: details on the outside face (−x, the face the camera sees); the door leads +x into the board. Channel parts: flat (≤ 0.048), no shadows, 45° mitred rails/strips so Hub + Arm/Edge
// combinations (straight, corner, T, cross, end) tile without overlaps.
import * as THREE from 'three';
import { Model, gBox, gLathe, gExtrude, gDisc, gTube, prep, roundedRect } from '../mlib.js';
import { defMat } from '../mats.js';

// ---------------------------------------------------------------- materials (new keys of this family)
defMat('rc_device',      { hex: '#5F5C54', rough: 0.5, metal: 0.12, note: 'sivil cihaz gövdesi: çekiç boya sıcak gri (ATM yüzü, konsol, pano)' });
defMat('rc_deviceDark',  { hex: '#35342F', rough: 0.5, metal: 0.1, note: 'cihazın koyu ön paneli (telsiz, telefon, tezgâh)' });
defMat('rc_screen',      { hex: '#1C2623', rough: 0.12, metal: 0, note: 'ekran ve pencere camı: koyu, ışımasız (baskında emisyon yok)' });
defMat('rc_keys',        { hex: '#B9B29B', rough: 0.6, metal: 0, tex: 'tx_keys (tuş ızgarası: 3×4 tuş takımı ve klavye sıraları, 256×128, yazısız)', note: 'tuş takımı, klavye, düğmeler' });
defMat('rc_plinth',      { hex: '#4A463F', rough: 0.85, metal: 0, note: 'sivil gövdenin koyu kaidesi (kâğıttan ayırır)' });
defMat('rc_vent',        { hex: '#3B3731', rough: 0.85, metal: 0, note: 'havalandırma yarıklarının koyu zemini' });
defMat('rc_stainless',   { hex: '#9EA19C', rough: 0.32, metal: 0.6, note: 'paslanmaz sac (temiz oda terminali, hava kilidi kapısı)' });
defMat('rc_glass',       { hex: '#C9D1CE', rough: 0.08, metal: 0, alpha: 0.45, note: 'açık gri cam (Transparent): cam kapı, vasistas, büyüteç' });
defMat('rc_channelHazard', { hex: '#A9520A', rough: 0.7, metal: 0, tex: 'tx_channel_hazard (turuncu #A9520A zemin, siyah #1E1B16 45° şerit, adım 0,10 birim; 256×32)', note: 'kanal kenar şeridi: turuncu-siyah, hiçbir kitte değişmez' });
defMat('rc_junction',    { hex: '#69665C', rough: 0.55, metal: 0.3, note: 'kanal bağlantı kutusu (döküm, boyalı)' });
defMat('rc_doorSteel',   { hex: '#6B6F6B', rough: 0.55, metal: 0.1, note: 'boyalı çelik kasa (varsayılan giriş)' });
defMat('rc_graphite',    { hex: '#464A49', rough: 0.5, metal: 0.15, note: 'grafit boyalı güvenlik kapısı kasası (banka)' });
defMat('rc_coldWhite',   { hex: '#CDD2CF', rough: 0.45, metal: 0.05, note: 'soğuk oda kapısı: soğuk beyaz emaye (krem sivilden ayrı)' });
defMat('rc_cleanGrey',   { hex: '#B3B8B3', rough: 0.4, metal: 0.05, note: 'hava kilidi kapı kanadı: açık gri emaye' });
defMat('rc_alu',         { hex: '#A4A7A2', rough: 0.35, metal: 0.6, note: 'eloksal alüminyum doğrama, itme barı' });
defMat('rc_navalGrey',   { hex: '#7C817C', rough: 0.55, metal: 0.1, note: 'gemi grisi boya (su geçirmez kapı)' });
defMat('rc_factoryGrey', { hex: '#7A776D', rough: 0.55, metal: 0.1, note: 'seksiyonel fabrika kapısı paneli (sıcak gri)' });
defMat('rc_dockGrey',    { hex: '#9C9D97', rough: 0.55, metal: 0.1, note: 'rampa kapısı paneli (açık gri)' });
defMat('rc_mesh',        { hex: '#4C4F4C', rough: 0.6, metal: 0.3, alpha: 0.55, tex: 'tx_mesh (kare tel örgü 5 cm, alfa kesme; 256²)', note: 'liman kapısı tel örgüsü' });

// ---------------------------------------------------------------- local geometry helpers
const R2D = 180 / Math.PI;
const NOSH = { cast: false };
// polygon (convex, ordered, three space) → fan, oriented away from `inside`
function pushPoly(out, pts, inside) {
  const [a, b, c] = pts; const ux = b[0] - a[0], uy = b[1] - a[1], uz = b[2] - a[2], vx = c[0] - a[0], vy = c[1] - a[1], vz = c[2] - a[2];
  const n = [uy * vz - uz * vy, uz * vx - ux * vz, ux * vy - uy * vx];
  const m = [0, 1, 2].map(k => pts.reduce((s, p) => s + p[k], 0) / pts.length - inside[k]);
  const q = n[0] * m[0] + n[1] * m[1] + n[2] * m[2] < 0 ? [...pts].reverse() : pts;
  for (let i = 1; i < q.length - 1; i++) out.push(...q[0], ...q[i], ...q[i + 1]);
}
function geoFromTris(arr, crease = 30) { const g = new THREE.BufferGeometry(); g.setAttribute('position', new THREE.BufferAttribute(new Float32Array(arr), 3)); return prep(g, { crease }); }
// unit face normal of triangle f, returned in UNITY convention (x, y, z)
function faceN(P, o) { const ux = P[o + 3] - P[o], uy = P[o + 4] - P[o + 1], uz = P[o + 5] - P[o + 2], vx = P[o + 6] - P[o], vy = P[o + 7] - P[o + 1], vz = P[o + 8] - P[o + 2];
  let x = uy * vz - uz * vy, y = uz * vx - ux * vz, z = ux * vy - uy * vx; const l = Math.hypot(x, y, z) || 1; return [x / l, y / l, -z / l]; }
// split a geometry into buckets by face normal: cls(nx, ny, nz) → key | null (dropped)
function splitBy(g, cls) {
  if (g.index) g = g.toNonIndexed();
  const P = g.attributes.position.array, N = g.attributes.normal.array, T = g.attributes.uv.array; const B = {};
  for (let f = 0; f < P.length / 9; f++) { const k = cls(...faceN(P, f * 9)); if (!k) continue; const b = (B[k] ||= { p: [], n: [], t: [] });
    for (let i = 0; i < 9; i++) { b.p.push(P[f * 9 + i]); b.n.push(N[f * 9 + i]); } for (let i = 0; i < 6; i++) b.t.push(T[f * 6 + i]); }
  const out = {}; for (const [k, b] of Object.entries(B)) { const q = new THREE.BufferGeometry();
    q.setAttribute('position', new THREE.Float32BufferAttribute(b.p, 3)); q.setAttribute('normal', new THREE.Float32BufferAttribute(b.n, 3)); q.setAttribute('uv', new THREE.Float32BufferAttribute(b.t, 2)); out[k] = q; }
  return out;
}
const filt = (g, keep) => splitBy(g, (x, y, z) => (keep(x, y, z) ? 'k' : null)).k;
// chamfered box with hidden LOCAL faces removed ('+x','-x','+y','-y','+z','-z', Unity names, before rotation)
function boxG(w, h, d, c = 0, drop = []) {
  const g = gBox(w, h, d, c); if (!drop.length) return g;
  return filt(g, (x, y, z) => !drop.some(f => (f[1] === 'x' ? x : f[1] === 'y' ? y : z) * (f[0] === '+' ? 1 : -1) > 0.99));
}
// convex prism: poly = 2D points (u, v); map(u, v, w) → Unity [x, y, z]; extruded from w0 to w1
function prism(poly, map, w0, w1, o = {}) {
  const out = [], n = poly.length; const V = (p, w) => { const [x, y, z] = map(p[0], p[1], w); return [x, y, -z]; };
  const A = poly.map(p => V(p, w0)), B = poly.map(p => V(p, w1)); const all = [...A, ...B];
  const ctr = [0, 1, 2].map(k => all.reduce((s, p) => s + p[k], 0) / all.length);
  if (o.cap0 !== false) pushPoly(out, A, ctr); if (o.cap1 !== false) pushPoly(out, B, ctr);
  for (let i = 0; i < n; i++) { if (o.skip && o.skip.includes(i)) continue; const j = (i + 1) % n; pushPoly(out, [A[i], A[j], B[j], B[i]], ctr); }
  return geoFromTris(out, o.crease ?? 30);
}
// flat horizontal polygon (Unity x, z) at height y, facing +y
function flatXZ(pts, y) { const out = []; const P = pts.map(([x, z]) => [x, y, -z]); const c = [0, 1, 2].map(k => P.reduce((s, p) => s + p[k], 0) / P.length); c[1] -= 1; pushPoly(out, P, c); return geoFromTris(out); }
// Unity euler that turns local +y into direction d (no roll)
function eulerY(d) { const L = Math.hypot(...d); const [x, y, z] = d.map(v => v / L); return [Math.atan2(z, y) * R2D, 0, Math.asin(Math.max(-1, Math.min(1, -x))) * R2D]; }
function rod(m, p0, p1, t, mat, c = 0, o = {}) { const d = [p1[0] - p0[0], p1[1] - p0[1], p1[2] - p0[2]];
  return m.add(gBox(t, Math.hypot(...d), t, c), mat, p0.map((v, i) => (v + p1[i]) / 2), eulerY(d), 'Body', o); }
// short ring/bezel lathe (outer wall + front rim) along local +y; profile starts on the mounting surface
const bezel = (ro, ri, h, seg) => gLathe([[ro, 0], [ro, h], [ri, h]], seg, { caps: false });
// rotations: local +y → −x (west face normal) / → −z (south face normal); a disc (faces −z) → −x / → +y
const TO_W = [0, 0, 90], TO_S = [-90, 0, 0], DISC_W = [0, 90, 0], DISC_UP = [90, 0, 0];

// ---------------------------------------------------------------- civilian base (keeps the civilian contract)
const CIV = { H: 0.64, y0: 0.03, band: [0.49, 0.55], ro: 0.16, ri: 0.128, crossL: 0.205, crossW: 0.034, B: 0.588, off: 0.03 };
function bandRing(B, c, t, y0, y1) { // raised band following the body's chamfered corners: sides + top bevel
  const oct = (a, k) => [[a, -(a - k)], [a, a - k], [a - k, a], [-(a - k), a], [-a, a - k], [-a, -(a - k)], [-(a - k), -a], [a - k, -a]];
  const O = oct(B / 2 + t, c + t * (Math.SQRT2 - 1)), I = oct(B / 2, c); const V = (p, y) => [p[0], y, -p[1]]; const out = [], ctr = [0, (y0 + y1) / 2, 0];
  for (let i = 0; i < 8; i++) { const j = (i + 1) % 8;
    pushPoly(out, [V(O[i], y0), V(O[j], y0), V(O[j], y1 - t), V(O[i], y1 - t)], ctr);
    pushPoly(out, [V(O[i], y1 - t), V(O[j], y1 - t), V(I[j], y1), V(I[i], y1)], ctr); }
  return geoFromTris(out, 30);
}
function plusShape(L, w) { const a = L / 2, b = w / 2;
  const pts = [[b, a], [-b, a], [-b, b], [-a, b], [-a, -b], [-b, -b], [-b, -a], [b, -a], [b, -b], [a, -b], [a, b], [b, b]];
  return prep(new THREE.ShapeGeometry(new THREE.Shape(pts.map(([x, y]) => new THREE.Vector2(x, y)))), { crease: 30 }); }
function civilBase(m, B, H, off, band) {
  const y0 = CIV.y0, h = H - y0;
  m.add(filt(gBox(B - 0.04, 0.056, B - 0.04, 0), (x, y) => Math.abs(y) < 0.5), 'rc_plinth', [off, 0.028, off], [0, 0, 0], 'Body', NOSH); // recessed dark plinth
  const faces = splitBy(gBox(B, h, B, 0.025), (x, y, z) => (y < -0.3 ? null : y > 0.3 ? 'r_civil_top' : Math.abs(x) >= Math.abs(z) - 1e-4 ? 'r_civil_west' : 'r_civil_south'));
  for (const k of ['r_civil_top', 'r_civil_west', 'r_civil_south']) m.add(faces[k], k, [off, y0 + h / 2, off]);
  m.add(bandRing(B, 0.025, 0.006, band[0], band[1]), 'r_blue', [off, 0, off], [0, 0, 0], 'Body', NOSH);
  m.lathe([[CIV.ro, 0], [CIV.ri, 0]], 20, 'r_blue', [off, H + 0.003, off], [0, 0, 0], 'Body', { caps: false, cast: false });
  m.add(plusShape(CIV.crossL, CIV.crossW), 'r_blue', [off, H + 0.003, off], DISC_UP, 'Body', NOSH);
}
const CIV_ORIGIN = 'karenin merkezi, pafta yüzeyi (y = 0)';
const CIV_RULE = 'Önce sivil okunur: krem gövde, mavi bant, tepede mavi halka + çarpı (Ø 0,32); ekipman batı/güney yüzde ikinci siluet. Gövde ekipmana yer açmak için +x +z yönüne 0,03 kaydırıldı (tamamı 0,66 kare içinde).';
function civil(id, listName, eq) {
  const m = new Model('Civil_' + id, { family: 'raidcivil', budget: 'raid', listName, origin: CIV_ORIGIN, note: `${eq.note} ${CIV_RULE}` });
  civilBase(m, CIV.B, CIV.H, CIV.off, CIV.band);
  const xw = CIV.off - CIV.B / 2, zs = xw, c = CIV.off;           // west face x, south face z (= −0.264), face centre (0.03)
  eq.build(m, { W: (z, y, d = 0) => [xw - d, y, z], S: (x, y, d = 0) => [x, y, zs - d], c, xw, zs, H: CIV.H });
  return m;
}

// ---------------------------------------------------------------- region equipment (≤ 0.066 in front of the faces, below the band)
// F.S(x, y, d) / F.W(z, y, d): point d in front of the south / west face. Main device on one face, sized to read at
// 88 px/tile (1080p): weu ru cn na sa on the south face, eeu me af sea oc on the west face, jk and in on both.
const EQ = {
  weu: { note: 'Banka veri merkezi: duvara gömülü ATM (güney yüz): uzun koyu ön yüz, gri siperlik ve yanaklarla ekran oyuğu, eğik tuş takımı rafı, çelik kart ve para ağzı.',
    build(m, F) { const { S, c, zs } = F;
      const x = c - 0.02;
      m.add(boxG(0.28, 0.42, 0.03, 0.008, ['+z']), 'rc_deviceDark', S(x, 0.25, 0.012));                   // tall fascia y 0.04–0.46
      m.add(boxG(0.30, 0.024, 0.062, 0.006, ['+z']), 'rc_device', S(x, 0.462, 0.031), [-8, 0, 0]);        // visor
      for (const s of [-1, 1]) m.add(boxG(0.025, 0.13, 0.05, 0, ['+z']), 'rc_device', S(x + s * 0.1375, 0.385, 0.037)); // cheeks → recess
      m.add(boxG(0.20, 0.12, 0.01, 0, ['+z']), 'rc_screen', S(x - 0.01, 0.385, 0.029), [8, 0, 0], 'Body', NOSH);
      m.add(boxG(0.025, 0.05, 0.012, 0, ['+z']), 'steelBare', S(x + 0.105, 0.30, 0.029), [0, 0, 0], 'Body', NOSH); // card slot bezel
      m.add(boxG(0.22, 0.022, 0.06, 0.005, ['+z']), 'rc_device', S(x - 0.01, 0.245, 0.03), [-18, 0, 0]);   // keypad ledge
      m.add(boxG(0.14, 0.01, 0.042, 0), 'rc_keys', [x - 0.01, 0.26, zs - 0.034], [-18, 0, 0], 'Body', NOSH);
      m.add(boxG(0.18, 0.03, 0.012, 0, ['+z']), 'steelBare', S(x, 0.14, 0.029), [0, 0, 0], 'Body', NOSH); // cash slot bezel
      m.add(boxG(0.15, 0.012, 0.006, 0), 'bakeliteBlack', S(x, 0.14, 0.036), [0, 0, 0], 'Body', NOSH);
    } },
  eeu: { note: 'Devlet arşivi: mikrofilm okuyucu (batı yüz): derin siperlikli, geriye yatık büyük ekran, film tablasında iki makara, yan kol (krank).',
    build(m, F) { const { W, c } = F;
      m.add(boxG(0.06, 0.08, 0.30, 0.008, ['+x']), 'rc_device', W(c, 0.16, 0.03));                        // base + film carrier
      for (const s of [-1, 1]) m.lathe([[0.033, 0], [0.033, 0.018], [0, 0.018]], 6, 'bakeliteBlack', W(c + s * 0.075, 0.20, 0.03), [0, 0, 0], 'Body', NOSH); // reels
      m.add(boxG(0.036, 0.23, 0.29, 0.008, ['+x']), 'rc_device', W(c, 0.33, 0.012), [0, 0, -7]);          // screen body, leaning back
      m.add(boxG(0.008, 0.175, 0.22, 0), 'rc_screen', W(c, 0.325, 0.03), [0, 0, -7], 'Body', NOSH);
      m.add(boxG(0.062, 0.024, 0.31, 0.006, ['+x']), 'rc_device', W(c, 0.457, 0.031), [0, 0, 6]);         // hood canopy
      for (const s of [-1, 1]) m.add(boxG(0.05, 0.22, 0.022, 0, ['+x']), 'rc_device', W(c + s * 0.145, 0.345, 0.025)); // deep cheeks
      const hub = W(c - 0.15, 0.16, 0.03);                                                                  // crank on the south end
      m.lathe([[0.02, 0], [0.02, 0.03], [0, 0.03]], 6, 'steelBare', hub, TO_S, 'Body', NOSH);
      rod(m, [hub[0], hub[1], hub[2] - 0.03], [hub[0] - 0.005, hub[1] - 0.07, hub[2] - 0.03], 0.025, 'steelBare', 0, NOSH);
      m.lathe([[0.015, 0], [0.015, 0.04], [0, 0.04]], 6, 'bakeliteBlack', [hub[0] - 0.005, hub[1] - 0.075, hub[2] - 0.03], TO_S, 'Body', NOSH);
    } },
  me: { note: 'Körfez rafinerisi: gaz analiz dolabı (batı yüz): dolap kapağında iki büyük yuvarlak gösterge ve örnek panjuru; tepenin kuzeybatı köşesinde şapkalı havalandırma bacası.',
    build(m, F) { const { W, c, H } = F;
      m.add(boxG(0.02, 0.37, 0.36, 0.006, ['+x']), 'rc_device', W(c, 0.26, 0.01));                      // cabinet door
      for (const s of [-1, 1]) { const p = W(c + s * 0.09, 0.33, 0.02);
        m.add(bezel(0.076, 0.061, 0.028, 12), 'steelBare', p, TO_W);
        m.add(gDisc(0.061, 12), 'dialCream', [p[0] - 0.024, p[1], p[2]], DISC_W, 'Body', NOSH); }
      m.add(boxG(0.012, 0.07, 0.24, 0, ['+x']), 'rc_vent', W(c, 0.145, 0.02), [0, 0, 0], 'Body', NOSH);   // sample louvre plate
      for (let i = 0; i < 3; i++) m.add(boxG(0.012, 0.025, 0.22, 0, ['+x', '-y']), 'rc_device', W(c, 0.125 + i * 0.022, 0.03), [0, 0, 25], 'Body', NOSH);
      const sx = -0.17, sz = 0.23;                                                                          // vent stack on the top
      m.cyl(0.024, 0.036, 8, 0, 'galv', [sx, H + 0.018, sz], [0, 0, 0], 'Body', { open: true });
      m.lathe([[0.046, 0], [0.046, 0.005], [0, 0.02]], 8, 'galv', [sx, H + 0.036, sz]);           // rain cap, top 0.696
    } },
  af: { note: 'Cevher hangarı: sevk telsizi konsolu (batı yüz): koyu ön panelde büyük beyaz ayar kadranı; yanında krem gövdeye asılı kara ahize.',
    build(m, F) { const { W, c } = F;
      m.add(boxG(0.04, 0.32, 0.30, 0.008, ['+x']), 'rc_deviceDark', W(c + 0.06, 0.27, 0.02));             // radio front
      const p = W(c + 0.06, 0.28, 0.04);
      m.add(bezel(0.11, 0.094, 0.016, 14), 'steelBare', p, TO_W);
      m.add(gDisc(0.094, 14), 'dialCream', [p[0] - 0.012, p[1], p[2]], DISC_W, 'Body', NOSH);
      m.cyl(0.03, 0.016, 8, 0, 'bakeliteBlack', [p[0] - 0.012, p[1], p[2]], TO_W, 'Body', NOSH);            // tuning knob
      const hz = c - 0.18;                                                                                  // handset on a hook
      m.add(boxG(0.03, 0.025, 0.026, 0, ['+x']), 'steelBare', W(hz, 0.44, 0.015), [0, 0, 0], 'Body', NOSH);
      m.add(boxG(0.036, 0.055, 0.052, 0), 'bakeliteBlack', W(hz, 0.395, 0.026));
      m.add(boxG(0.026, 0.125, 0.032, 0), 'bakeliteBlack', W(hz, 0.305, 0.022));
      m.add(boxG(0.036, 0.055, 0.052, 0), 'bakeliteBlack', W(hz, 0.215, 0.026));
      m.cyl(0.0125, 0.07, 6, 0, 'bakeliteBlack', W(hz, 0.155, 0.02), [0, 0, 0], 'Body', { open: true, cast: false }); // cord stub
    } },
  na: { note: 'Dağıtım merkezi: depo kiosku (güney yüz): gövdenin önünde ayaklı direk, üstünde eğik barkod terminali, fiş ağzından sarkan fiş.',
    build(m, F) { const { c, zs } = F;
      const px = c - 0.10;
      m.add(boxG(0.056, 0.38, 0.05, 0.008, ['-y']), 'rc_device', [px, 0.19, -0.305]);                     // pedestal post
      m.add(boxG(0.13, 0.012, 0.058, 0, ['-y']), 'rc_device', [px, 0.006, -0.299], [0, 0, 0], 'Body', NOSH); // foot plate
      const hx = c - 0.06, Z0 = zs, Z1 = -0.321, y0 = 0.37, y1 = 0.40, y2 = 0.465;                         // sloped head (side profile)
      m.add(prism([[Z0, y0], [Z1, y0], [Z1, y1], [Z0 - 0.012, y2], [Z0, y2]], (u, v, w) => [w, v, u], hx - 0.10, hx + 0.10), 'rc_device', [0, 0, 0]);
      const sa = Math.atan2(y2 - y1, Z0 - 0.012 - Z1);                                                     // slope angle from horizontal (rad)
      const sn = [Math.cos(sa), -Math.sin(sa)], sm = [(y1 + y2) / 2, (Z1 + Z0 - 0.012) / 2];
      m.add(boxG(0.15, 0.006, 0.05, 0), 'rc_screen', [hx - 0.01, sm[0] + sn[0] * 0.0035, sm[1] + sn[1] * 0.0035], [-sa * R2D, 0, 0], 'Body', NOSH); // scanner window
      m.add(boxG(0.055, 0.055, 0.004, 0), 'r_card', [hx + 0.03, 0.36, Z1 - 0.0045], [2, 0, 0], 'Body', NOSH);     // receipt from the slot
    } },
  ru: { note: 'Sibirya veri merkezi: operatör konsolu (güney yüz): büyük koyu ekranlı CRT ve altında açık tuşlu eğik klavye rafı.',
    build(m, F) { const { S, c, zs } = F;
      m.add(boxG(0.25, 0.20, 0.058, 0.02, ['+z']), 'rc_device', S(c - 0.02, 0.36, 0.027));               // CRT case y 0.26–0.46
      m.add(boxG(0.18, 0.135, 0.008, 0, ['+z']), 'rc_screen', S(c - 0.035, 0.365, 0.057), [0, 0, 0], 'Body', NOSH);
      for (const y of [0.40, 0.335]) m.add(boxG(0.026, 0.026, 0.01, 0, ['+z']), 'bakeliteBlack', S(c + 0.07, y, 0.058), [0, 0, 0], 'Body', NOSH);
      m.add(boxG(0.44, 0.025, 0.062, 0.006, ['+z']), 'rc_device', S(c, 0.215, 0.03), [-14, 0, 0]);       // keyboard shelf
      m.add(boxG(0.34, 0.016, 0.044, 0), 'rc_keys', [c, 0.232, zs - 0.034], [-14, 0, 0], 'Body', NOSH);
      for (const s of [-1, 1]) m.add(boxG(0.02, 0.07, 0.045, 0, ['+z']), 'rc_device', S(c + s * 0.18, 0.17, 0.0225));
    } },
  in: { note: 'Çağrı merkezi: masa ünitesi: güneybatı köşesini saran L tezgâh; güney kanatta kara masa telefonu, batı yüzde kancaya asılı kulaklık.',
    build(m, F) { const { W, S, c, xw, zs } = F;
      m.add(boxG(0.45, 0.025, 0.066, 0.006, ['+z']), 'rc_device', [-0.105, 0.26, zs - 0.033]);            // south wing (covers the corner)
      m.add(boxG(0.066, 0.025, 0.40, 0.006, ['+x']), 'rc_device', [xw - 0.033, 0.26, -0.064]);            // west wing
      m.add(boxG(0.02, 0.06, 0.045, 0, ['+z']), 'rc_device', S(0.08, 0.2175, 0.0225));
      m.add(boxG(0.045, 0.06, 0.02, 0, ['+x']), 'rc_device', W(0.10, 0.2175, 0.0225));
      const px = -0.02, Z0 = zs - 0.004, Z1 = -0.326;                                                     // desk phone (wedge) on the south wing
      m.add(prism([[Z0, 0.2725], [Z1, 0.2725], [Z1 + 0.006, 0.298], [Z0 - 0.008, 0.318]], (u, v, w) => [w, v, u], px - 0.06, px + 0.06), 'bakeliteBlack', [0, 0, 0]);
      m.add(boxG(0.13, 0.022, 0.03, 0), 'bakeliteBlack', [px, 0.33, -0.296]);                            // handset
      for (const s of [-1, 1]) m.add(boxG(0.036, 0.026, 0.038, 0, ['-y']), 'bakeliteBlack', [px + s * 0.055, 0.322, -0.296], [0, 0, 0], 'Body', NOSH);
      const hz = 0.0;                                                                                       // headset on a hook (west face)
      m.add(boxG(0.035, 0.025, 0.025, 0, ['+x']), 'steelBare', W(hz, 0.46, 0.0175), [0, 0, 0], 'Body', NOSH);
      m.add(boxG(0.02, 0.025, 0.13, 0), 'bakeliteBlack', W(hz, 0.44, 0.03), [0, 0, 0], 'Body', NOSH);
      for (const s of [-1, 1]) {
        m.add(boxG(0.02, 0.08, 0.025, 0), 'bakeliteBlack', W(hz + s * 0.06, 0.395, 0.03), [0, 0, 0], 'Body', NOSH);
        m.cyl(0.034, 0.028, 6, 0, 'rubberGasket', W(hz + s * 0.062, 0.345, 0.029), [90, 0, 0], 'Body', NOSH); }
    } },
  cn: { note: 'Montaj fabrikası: kalite kontrol panosu (güney yüz): ekranlı pano, iş rafı ve rafın üstüne eğilen kollu büyük büyüteç lambası.',
    build(m, F) { const { S, c } = F;
      m.add(boxG(0.42, 0.022, 0.06, 0.006, ['+z']), 'rc_device', S(c, 0.145, 0.03));                      // work ledge
      m.add(boxG(0.20, 0.20, 0.026, 0.006, ['+z']), 'rc_device', S(c + 0.10, 0.335, 0.013));              // display panel
      m.add(boxG(0.15, 0.10, 0.006, 0, ['+z']), 'rc_screen', S(c + 0.10, 0.36, 0.028), [0, 0, 0], 'Body', NOSH);
      for (let i = 0; i < 3; i++) m.add(boxG(0.026, 0.026, 0.012, 0, ['+z']), 'rc_keys', S(c + 0.06 + i * 0.04, 0.27, 0.031), [0, 0, 0], 'Body', NOSH);
      const el = S(c - 0.07, 0.43, 0.044);                                                                   // anglepoise arm
      m.add(boxG(0.05, 0.04, 0.024, 0, ['+z']), 'rc_deviceDark', S(c - 0.20, 0.445, 0.012), [0, 0, 0], 'Body', NOSH);
      rod(m, S(c - 0.20, 0.445, 0.03), el, 0.03, 'rc_deviceDark', 0, NOSH);
      rod(m, el, S(c - 0.10, 0.355, 0.035), 0.03, 'rc_deviceDark', 0, NOSH);
      const hc = S(c - 0.10, 0.285, 0.023);                                                                 // magnifier head, lens faces down-front
      m.add(bezel(0.07, 0.052, 0.02, 12), 'rc_deviceDark', hc, [-110, 0, 0]);
      m.add(gDisc(0.053, 12), 'rc_glass', hc, [-20, 0, 0], 'Body', NOSH);
    } },
  jk: { note: 'Çip fabrikası: temiz oda terminali: güney yüzde contalı eğik ekranlı paslanmaz kutu; batı yüzde iki büyük eldiven ağzı.',
    build(m, F) { const { W, c, zs } = F;
      const tx = c + 0.02, Zf = -0.327;
      m.add(prism([[zs, 0.20], [Zf, 0.20], [Zf, 0.245], [zs - 0.012, 0.44], [zs, 0.44]], (u, v, w) => [w, v, u], tx - 0.15, tx + 0.15), 'rc_stainless', [0, 0, 0]);
      const ta = Math.atan2(Zf - zs + 0.012, 0.44 - 0.245) * R2D;                                          // slope (deg from vertical)
      const mz = (Zf + zs - 0.012) / 2, my = (0.245 + 0.44) / 2;
      m.add(boxG(0.27, 0.185, 0.004, 0), 'rubberGasket', [tx, my, mz - 0.002], [-ta, 0, 0], 'Body', NOSH);  // gasket frame (0.025 border)
      m.add(boxG(0.22, 0.135, 0.004, 0), 'rc_screen', [tx, my, mz - 0.005], [-ta, 0, 0], 'Body', NOSH);
      for (const s of [-1, 1]) { const p = W(c + 0.04 + s * 0.12, 0.255, 0);                              // glove ports
        m.add(bezel(0.07, 0.05, 0.024, 12), 'rc_stainless', p, TO_W);
        m.add(gDisc(0.05, 12), 'rubberGasket', [p[0] - 0.02, p[1], p[2]], DISC_W, 'Body', NOSH); }
    } },
  sea: { note: 'Elektronik fabrikası: test tezgâhı (batı yüz): koyu rafta yuvarlak ekranlı büyük osiloskop, raf ucundan sarkan iki prob kablosu.',
    build(m, F) { const { W, c } = F;
      m.add(boxG(0.06, 0.028, 0.44, 0.006, ['+x']), 'rc_deviceDark', W(c, 0.20, 0.03));                   // bench shelf
      for (const s of [-1, 1]) m.add(boxG(0.04, 0.06, 0.02, 0, ['+x']), 'rc_deviceDark', W(c + s * 0.17, 0.156, 0.02));
      const oz = c + 0.06;
      m.add(boxG(0.056, 0.17, 0.22, 0.012, ['+x']), 'rc_device', W(oz, 0.30, 0.028));                     // scope case
      const p = W(oz - 0.03, 0.305, 0.056);
      m.add(bezel(0.056, 0.044, 0.008, 12), 'steelBare', p, TO_W, 'Body', NOSH);
      m.add(gDisc(0.044, 12), 'rc_screen', [p[0] - 0.006, p[1], p[2]], DISC_W, 'Body', NOSH);
      for (const y of [0.34, 0.27]) m.add(boxG(0.012, 0.028, 0.028, 0, ['+x']), 'bakeliteBlack', W(oz + 0.065, y, 0.06), [0, 0, 0], 'Body', NOSH);
      for (const [z, dd] of [[-0.23, 0.04], [-0.205, 0.02]]) {                                             // probe leads + tips
        m.cyl(0.0125, 0.13, 6, 0, 'bakeliteBlack', W(z, 0.13, dd), [0, 0, 0], 'Body', { open: true, cast: false });
        m.cyl(0.016, 0.035, 6, 0, 'steelBare', W(z, 0.05, dd), [0, 0, 0], 'Body', NOSH); }
    } },
  sa: { note: 'Hidroelektrik santrali: röle panosu (güney yüz): menteşeli dolap kapağında 2×2 yuvarlak sayaç ve röle, dikey kol.',
    build(m, F) { const { S, c } = F;
      m.add(boxG(0.38, 0.37, 0.022, 0.006, ['+z']), 'rc_device', S(c, 0.265, 0.011));                     // cabinet door
      for (const y of [0.14, 0.39]) m.add(boxG(0.02, 0.05, 0.03, 0, ['+z']), 'steelBare', S(c - 0.19, y, 0.015), [0, 0, 0], 'Body', NOSH);
      m.add(boxG(0.026, 0.12, 0.026, 0), 'steelBare', S(c + 0.16, 0.265, 0.04), [0, 0, 0], 'Body', NOSH); // handle bar
      for (const y of [0.225, 0.305]) m.add(boxG(0.02, 0.02, 0.02, 0, ['+z']), 'steelBare', S(c + 0.16, y, 0.03), [0, 0, 0], 'Body', NOSH);
      for (const s of [-1, 1]) { const p = S(c - 0.025 + s * 0.075, 0.355, 0.022);                       // two meters
        m.add(bezel(0.052, 0.042, 0.02, 8), 'bakeliteBlack', p, TO_S);
        m.add(gDisc(0.042, 8), 'dialCream', [p[0], p[1], p[2] - 0.016], [0, 0, 0], 'Body', NOSH);
        const q = S(c - 0.025 + s * 0.075, 0.185, 0.022);                                                  // two relay cans
        m.lathe([[0.046, 0], [0.046, 0.03], [0.038, 0.038], [0, 0.038]], 8, 'bakeliteBlack', q, TO_S); }
    } },
  oc: { note: 'Maden limanı: vinç kumanda kioskı (batı yüz): kayıtlı koyu pencere, altında eğik kumanda masası ve iki kumanda kolu.',
    build(m, F) { const { W, c, xw } = F;
      m.add(boxG(0.022, 0.15, 0.36, 0.006, ['+x']), 'rc_device', W(c, 0.39, 0.011));                      // window frame
      m.add(boxG(0.006, 0.11, 0.32, 0, ['+x']), 'rc_screen', W(c, 0.39, 0.025), [0, 0, 0], 'Body', NOSH);
      m.add(boxG(0.008, 0.11, 0.026, 0, ['+x']), 'rc_device', W(c, 0.39, 0.029), [0, 0, 0], 'Body', NOSH); // mullion
      m.add(boxG(0.06, 0.03, 0.40, 0.006, ['+x']), 'rc_device', W(c, 0.17, 0.03), [0, 0, 12]);            // control desk
      for (const s of [-1, 1]) m.add(boxG(0.04, 0.11, 0.025, 0, ['+x', '-y']), 'rc_device', W(c + s * 0.16, 0.10, 0.02));
      for (const s of [-1, 1]) { const z = c + s * 0.10, x = xw - 0.032, y = 0.19;                        // joysticks
        m.lathe([[0.024, 0], [0.016, 0.02]], 6, 'rubberGasket', [x, y - 0.004, z], [0, 0, 0], 'Body', NOSH);
        m.cyl(0.0125, 0.10, 6, 0, 'steelBare', [x, y + 0.065, z], [0, 0, 0], 'Body', { open: true, cast: false });
        m.lathe([[0, 0], [0.022, 0.008], [0.022, 0.022], [0, 0.034]], 8, 'bakeliteBlack', [x, y + 0.11, z], [0, 0, 0], 'Body', NOSH); }
    } },
};

function CivilDefault() {
  const B = 0.63, H = 0.66;
  const m = new Model('Civil_default', { family: 'raidcivil', budget: 'raid', listName: 'Sivil sunucu (varsayılan kit)', origin: CIV_ORIGIN,
    note: 'Bugünkü krem sunucu, pahlı: krem gövde (üç yüz anahtarı), mavi bant, tepede mavi halka + çarpı (Ø 0,32), güney yüzde havalandırma panjuru. Eğitim, örnek baskın ve varsayılan kit.' });
  civilBase(m, B, H, 0, [0.49, 0.55]);
  m.add(boxG(0.40, 0.23, 0.006, 0, ['+z']), 'rc_vent', [0, 0.25, -B / 2 - 0.003], [0, 0, 0], 'Body', NOSH);
  for (let i = 0; i < 4; i++) m.add(boxG(0.38, 0.028, 0.008, 0, ['+z', '-y']), 'r_civil_south', [0, 0.17 + i * 0.053, -B / 2 - 0.0054], [-25, 0, 0], 'Body', NOSH);
  return m;
}

// ---------------------------------------------------------------- entry doors (west margin, u = −0.15; outside face −x)
// width along z 0.90 (±0.45), frame depth x −0.04…0.06, hardware up to x −0.08 → depth 0.14; height ≤ 0.60
const DOOR_ORIGIN = 'kapı izdüşümünün ortası, kontrplak kenar payının yüzeyi (y = 0)';
const DOOR_RULE = 'Paftanın dışında, batı kenar payında (u = −0,15) giriş satırının ortasında durur; dış yüz −x (kameranın gördüğü yüz), kapı +x yönünde tahtanın içine açılır. El çarkı yok, kırmızı/mavi/turuncu bant yok.';
function door(id, listName, note) {
  const m = new Model('EntryDoor_' + id, { family: 'raidcivil', budget: 'raid', listName, origin: DOOR_ORIGIN, note: `${note} ${DOOR_RULE}` });
  m.meta.place = [{ pos: [-0.15, 0, -3.5], yaw: 0, note: 'örnek: giriş satırı 4 (r = 3) · x = u = −0,15, z = −(r + 0,5)' }];
  return m;
}
function frame(m, mat, o = {}) { // two jambs + head (+ optional sill); jamb tops hidden under the head
  const jw = o.jw ?? 0.06, hh = o.hh ?? 0.06, H = o.H ?? 0.58, d = o.d ?? 0.10, xc = o.xc ?? 0.01, c = o.c ?? 0.012;
  for (const s of [-1, 1]) m.add(boxG(d, H - hh, jw, c, ['-y', '+y']), mat, [xc, (H - hh) / 2, s * (0.45 - jw / 2)]);
  m.add(boxG(d, hh, 0.90, c), mat, [xc, H - hh / 2, 0]);
  if (o.sill) m.add(boxG(d, o.sill, 0.90 - 2 * jw, c, ['-y']), mat, [xc, o.sill / 2, 0]);
}
const DOORS = {
  default: () => { const m = door('default', 'Giriş kapısı (varsayılan): sade kasa', 'Sade boyalı çelik kasa, eşik sacı; kanat yok (açık geçit).');
    frame(m, 'rc_doorSteel');
    m.add(boxG(0.09, 0.012, 0.78, 0.003, ['-y']), 'steelBare', [0.01, 0.006, 0], [0, 0, 0], 'Body', NOSH);
    return m; },
  weu: () => { const m = door('weu', 'Giriş kapısı · banka: çelik güvenlik kapısı', 'Grafit kasa, düz çelik kanat, dar dikey gözetleme camı, kollu kol ve tekmelik; güney kasada kart okuyucu kutusu.');
    frame(m, 'rc_graphite');
    m.add(boxG(0.04, 0.515, 0.78, 0.008, ['-y']), 'rc_doorSteel', [0, 0.2575, 0]);
    m.add(boxG(0.004, 0.07, 0.70, 0), 'steelBare', [-0.022, 0.045, 0], [0, 0, 0], 'Body', NOSH);          // kick plate
    m.add(boxG(0.006, 0.17, 0.055, 0), 'rc_screen', [-0.021, 0.36, -0.25], [0, 0, 0], 'Body', NOSH);     // vision slot
    m.add(boxG(0.008, 0.085, 0.04, 0, ['+x']), 'steelBare', [-0.024, 0.255, -0.33], [0, 0, 0], 'Body', NOSH); // rose
    m.add(boxG(0.025, 0.025, 0.10, 0), 'steelBare', [-0.04, 0.27, -0.29], [0, 0, 0], 'Body', NOSH);      // lever
    for (const y of [0.12, 0.40]) m.cyl(0.018, 0.08, 6, 0, 'steelCast', [-0.03, y, 0.39], [0, 0, 0], 'Body', NOSH); // hinge barrels
    m.add(boxG(0.032, 0.115, 0.052, 0.006, ['+x']), 'rc_deviceDark', [-0.056, 0.30, -0.42]);           // card reader
    m.add(boxG(0.004, 0.05, 0.034, 0), 'rc_alu', [-0.073, 0.315, -0.42], [0, 0, 0], 'Body', NOSH);
    return m; },
  eeu: () => { const m = door('eeu', 'Giriş kapısı · arşiv: vasistaslı çift kanatlı ahşap kapı', 'Koyu cilalı ahşap kasa ve kayıt, açık ahşap iki kanat, tablalar, üstte camlı vasistas, pirinç topuzlar.');
    frame(m, 'woodVarnish', { H: 0.59 });
    m.add(boxG(0.10, 0.03, 0.78, 0), 'woodVarnish', [0.01, 0.435, 0]);                                    // transom bar
    m.add(boxG(0.008, 0.10, 0.78, 0), 'rc_glass', [0.0, 0.49, 0], [0, 0, 0], 'Body', NOSH);
    for (const z of [-0.13, 0.13]) m.add(boxG(0.02, 0.10, 0.025, 0), 'woodVarnish', [0.0, 0.49, z], [0, 0, 0], 'Body', NOSH);
    for (const s of [-1, 1]) { const z = s * 0.195;
      m.add(boxG(0.035, 0.42, 0.385, 0.006, ['-y']), 'woodRaw', [0, 0.21, z]);
      m.add(boxG(0.01, 0.15, 0.27, 0, ['+x']), 'woodVarnish', [-0.0215, 0.12, z], [0, 0, 0], 'Body', NOSH);
      m.add(boxG(0.01, 0.13, 0.27, 0, ['+x']), 'woodVarnish', [-0.0215, 0.315, z], [0, 0, 0], 'Body', NOSH);
      m.lathe([[0.016, 0], [0.016, 0.012], [0.008, 0.024], [0, 0.024]], 6, 'brassWorn', [-0.0175, 0.22, s * 0.035], TO_W, 'Body', NOSH); }
    return m; },
  me: () => { const m = door('me', 'Giriş kapısı · rafineri: kollu mandallı patlama kapısı', 'Kalın koyu döküm kasa ve eşik, boyalı ağır çelik kanat, kilit tarafında üç kollu mandal, menteşe tarafında iki ağır menteşe (çark yok).');
    frame(m, 'castIron', { jw: 0.08, hh: 0.08, c: 0.015, sill: 0.03 });
    m.add(boxG(0.05, 0.46, 0.72, 0.015, ['-y']), 'rc_doorSteel', [-0.005, 0.26, 0]);
    for (const y of [0.12, 0.26, 0.40]) {
      m.cyl(0.02, 0.02, 6, 0, 'steelBare', [-0.04, y, -0.32], TO_W, 'Body', NOSH);
      m.add(boxG(0.025, 0.026, 0.095, 0), 'steelBare', [-0.052, y + 0.012, -0.36], [-14, 0, 0], 'Body', NOSH); }
    for (const y of [0.14, 0.38]) m.cyl(0.026, 0.09, 8, 0, 'steelCast', [-0.035, y, 0.36], [0, 0, 0]);
    return m; },
  af: () => { const m = door('af', 'Giriş kapısı · hangar: sarmal kepenk', 'Galvaniz oluklu kepenk perdesi (yatay nervürler), iki yan kılavuz ray, üstte yuvarlatılmış sarma kutusu, alt çıta ve tutamak.');
    for (const s of [-1, 1]) m.add(boxG(0.09, 0.50, 0.05, 0.008, ['-y']), 'steelCast', [0.01, 0.25, s * 0.425]);
    m.add(boxG(0.11, 0.10, 0.90, 0.035), 'galv', [0.005, 0.55, 0]);                                       // roll housing y 0.50–0.60
    m.add(boxG(0.02, 0.49, 0.80, 0, ['-y']), 'galv', [0.0, 0.255, 0]);
    for (let i = 0; i < 8; i++) m.add(boxG(0.012, 0.026, 0.80, 0, ['+x']), 'galv', [-0.016, 0.07 + i * 0.054, 0], [0, 0, 0], 'Body', NOSH);
    m.add(boxG(0.045, 0.04, 0.80, 0.008, ['-y']), 'steelCast', [-0.005, 0.02, 0]);                         // bottom bar
    m.add(boxG(0.025, 0.026, 0.09, 0), 'steelBare', [-0.04, 0.03, -0.10], [0, 0, 0], 'Body', NOSH);
    return m; },
  na: () => { const m = door('na', 'Giriş kapısı · dağıtım: tamponlu rampa kapısı', 'Açık gri seksiyonel rampa kapısı, çevresinde kara kauçuk körük yastıkları, altta iki kara tampon ve rampa sacı.');
    for (const s of [-1, 1]) m.add(boxG(0.07, 0.50, 0.08, 0.015, ['-y']), 'rubber', [-0.025, 0.25, s * 0.41]);
    m.add(boxG(0.07, 0.07, 0.90, 0.015), 'rubber', [-0.025, 0.535, 0]);
    m.add(boxG(0.025, 0.50, 0.74, 0.004, ['-y']), 'rc_dockGrey', [0.02, 0.25, 0]);
    for (const y of [0.125, 0.25, 0.375]) m.add(boxG(0.01, 0.025, 0.74, 0, ['+x']), 'rc_dockGrey', [0.003, y, 0], [0, 0, 0], 'Body', NOSH);
    for (const s of [-1, 1]) m.add(boxG(0.05, 0.10, 0.08, 0.01, ['-y']), 'rubber', [-0.055, 0.06, s * 0.30]);
    m.add(boxG(0.045, 0.012, 0.50, 0.003, ['-y']), 'steelBare', [-0.0375, 0.006, 0], [0, 0, 0], 'Body', NOSH);
    return m; },
  ru: () => { const m = door('ru', 'Giriş kapısı · Sibirya: yalıtımlı soğuk oda kapısı', 'Paslanmaz kasa, kalın yuvarlatılmış beyaz yalıtımlı kanat, kilit tarafında büyük kollu mandal, iki ağır blok menteşe.');
    frame(m, 'rc_stainless');
    m.add(boxG(0.06, 0.515, 0.78, 0.02, ['-y']), 'rc_coldWhite', [0, 0.2575, 0]);
    m.add(boxG(0.004, 0.07, 0.70, 0), 'rc_stainless', [-0.032, 0.045, 0], [0, 0, 0], 'Body', NOSH);
    m.add(boxG(0.03, 0.11, 0.045, 0.006, ['+x']), 'chrome', [-0.045, 0.27, -0.33]);                       // latch body
    m.add(boxG(0.025, 0.025, 0.17, 0.004), 'chrome', [-0.065, 0.30, -0.25], [-16, 0, 0]);               // big lever
    for (const y of [0.12, 0.40]) m.add(boxG(0.04, 0.075, 0.06, 0.008, ['+x']), 'chrome', [-0.05, y, 0.37]);
    return m; },
  in: () => { const m = door('in', 'Giriş kapısı · çağrı merkezi: itme barlı cam kapı', 'Eloksal alüminyum kasa ve kanat çerçevesi, açık gri cam (saydam), önde yatay itme barı.');
    frame(m, 'rc_alu', { jw: 0.05, hh: 0.05, d: 0.08 });
    for (const s of [-1, 1]) m.add(boxG(0.03, 0.52, 0.045, 0, ['-y']), 'rc_alu', [0, 0.26, s * 0.3725]);
    m.add(boxG(0.03, 0.05, 0.70, 0), 'rc_alu', [0, 0.495, 0]);
    m.add(boxG(0.03, 0.09, 0.70, 0, ['-y']), 'rc_alu', [0, 0.045, 0]);
    m.add(boxG(0.01, 0.38, 0.70, 0), 'rc_glass', [0, 0.28, 0], [0, 0, 0], 'Body', NOSH);
    m.cyl(0.014, 0.62, 8, 0, 'rc_alu', [-0.05, 0.25, 0], [90, 0, 0], 'Body', NOSH);                       // push bar along z
    for (const z of [-0.27, 0.27]) m.add(boxG(0.03, 0.025, 0.025, 0, ['+x']), 'rc_alu', [-0.03, 0.25, z], [0, 0, 0], 'Body', NOSH);
    return m; },
  cn: () => { const m = door('cn', 'Giriş kapısı · fabrika: seksiyonel kapı', 'Döküm kasa, dört geniş yatay panelli seksiyonel kapı, üçüncü panelde pencere sırası, alt kauçuk conta ve tutamak.');
    frame(m, 'steelCast');
    for (let i = 0; i < 4; i++) m.add(boxG(0.03, 0.122, 0.78, 0.006, ['+x']), 'rc_factoryGrey', [-0.005, 0.07 + i * 0.125, 0]);
    for (const z of [-0.24, 0, 0.24]) m.add(boxG(0.006, 0.05, 0.13, 0), 'rc_screen', [-0.021, 0.32, z], [0, 0, 0], 'Body', NOSH);
    m.add(boxG(0.035, 0.012, 0.78, 0, ['-y']), 'rubber', [-0.005, 0.006, 0], [0, 0, 0], 'Body', NOSH);
    m.add(boxG(0.025, 0.026, 0.09, 0), 'steelBare', [-0.03, 0.10, 0], [0, 0, 0], 'Body', NOSH);
    return m; },
  jk: () => { const m = door('jk', 'Giriş kapısı · çip fabrikası: yuvarlak pencereli hava kilidi kapısı', 'Kalın paslanmaz kasa ve eşik rampası, açık gri emaye kanat, ortada paslanmaz çerçeveli yuvarlak koyu pencere, dikey çekme kolu.');
    frame(m, 'rc_stainless', { jw: 0.08, hh: 0.08, c: 0.02 });
    m.add(boxG(0.04, 0.50, 0.74, 0.015, ['-y']), 'rc_cleanGrey', [0, 0.25, 0]);
    const p = [-0.02, 0.34, 0];
    m.add(bezel(0.09, 0.066, 0.014, 16), 'rc_stainless', p, TO_W);
    m.add(gDisc(0.067, 16), 'rc_screen', [-0.025, 0.34, 0], DISC_W, 'Body', NOSH);
    m.add(boxG(0.025, 0.15, 0.025, 0), 'rc_stainless', [-0.05, 0.22, -0.27], [0, 0, 0], 'Body', NOSH);    // pull handle
    for (const y of [0.17, 0.27]) m.add(boxG(0.03, 0.025, 0.025, 0, ['+x']), 'rc_stainless', [-0.03, y, -0.27], [0, 0, 0], 'Body', NOSH);
    m.add(prism([[-0.075, 0], [-0.04, 0.02], [0.06, 0.02], [0.06, 0]], (u, v, w) => [u, v, w], -0.37, 0.37, { cap0: true }), 'rc_stainless', [0, 0, 0], [0, 0, 0], 'Body', NOSH); // threshold ramp
    return m; },
  sea: () => { const m = door('sea', 'Giriş kapısı · elektronik fabrikası: mandallı oval su geçirmez kapı', 'Gemi grisi perde sacı, koyu oval kasa ağzı içinde gri oval kanat, çevrede altı küçük mandal kolu, iki menteşe (çark yok).');
    m.add(boxG(0.04, 0.58, 0.90, 0.012, ['-y']), 'rc_navalGrey', [0.035, 0.29, 0]);                       // bulkhead plate x 0.015–0.055
    const coaming = filt(gExtrude(roundedRect(0.66, 0.52, 0.19, 3), 0.025, 0), (x, y, z) => z < 0.99);    // dark oval coaming behind the leaf (back cap dropped)
    m.add(coaming, 'steelCast', [0.0025, 0.29, 0], [0, 90, 0]);
    const leaf = filt(gExtrude(roundedRect(0.60, 0.46, 0.16, 3), 0.03, 0), (x, y, z) => z < 0.99);
    m.add(leaf, 'rc_navalGrey', [-0.025, 0.29, 0], [0, 90, 0]);
    const dogs = [[0.47, -0.27, 20], [0.29, -0.33, 0], [0.11, -0.27, -20], [0.47, 0.27, -20], [0.29, 0.33, 0], [0.11, 0.27, 20]];
    for (const [y, z, a] of dogs) m.add(boxG(0.025, 0.07, 0.025, 0), 'steelBare', [-0.05, y, z], [a, 0, 0], 'Body', NOSH);
    for (const y of [0.16, 0.42]) m.add(boxG(0.03, 0.06, 0.05, 0, ['+x']), 'steelCast', [-0.045, y, 0.30], [0, 0, 0], 'Body', NOSH);
    return m; },
  sa: () => { const m = door('sa', 'Giriş kapısı · santral: ağır cıvatalı kapak kasası', 'Kalın galvaniz dört kenarlı kasa (eşikli), koyu döküm kapak ve çapraz takviyeleri, kasada somun sırası.');
    frame(m, 'galv', { jw: 0.10, hh: 0.10, c: 0.015, sill: 0.08, H: 0.60 });
    m.add(boxG(0.04, 0.40, 0.68, 0.012), 'castIron', [-0.005, 0.29, 0]);
    for (const s of [-1, 1]) rod(m, [-0.03, 0.12, s * 0.30], [-0.03, 0.46, -s * 0.30], 0.025, 'castIron', 0, NOSH);
    const bolts = [[0.04, -0.40], [0.04, 0.40], [0.30, -0.40], [0.30, 0.40], [0.55, -0.40], [0.55, 0.40], [0.55, -0.14], [0.55, 0.14]];
    for (const [y, z] of bolts) m.add(boxG(0.014, 0.03, 0.03, 0, ['+x']), 'steelBare', [-0.047, y, z], [45, 0, 0], 'Body', NOSH); // square nuts
    return m; },
  oc: () => { const m = door('oc', 'Giriş kapısı · maden limanı: tel örgülü liman kapısı', 'İki kare dikme, iki kanatlı boru çerçeveli kapı, tel örgü dolgu (alfa), kanat başına çapraz gergi ve ortada sürgü.');
    for (const s of [-1, 1]) { m.add(boxG(0.06, 0.57, 0.06, 0.008, ['-y']), 'steelCast', [0.01, 0.285, s * 0.415]);
      m.add(boxG(0.07, 0.015, 0.07, 0.004), 'steelCast', [0.01, 0.5775, s * 0.415], [0, 0, 0], 'Body', NOSH); }
    for (const s of [-1, 1]) { const zc = s * 0.1925, z0 = s * 0.355, z1 = s * 0.03;
      for (const z of [z0, z1]) m.add(boxG(0.03, 0.48, 0.03, 0), 'galv', [0.0, 0.27, z]);
      for (const y of [0.045, 0.495]) m.add(boxG(0.03, 0.03, 0.325, 0), 'galv', [0.0, y, zc]);
      m.add(boxG(0.006, 0.42, 0.325, 0), 'rc_mesh', [0.0, 0.27, zc], [0, 0, 0], 'Body', NOSH);
      rod(m, [-0.006, 0.06, z0 - s * 0.02], [-0.006, 0.48, z1 + s * 0.02], 0.025, 'galv', 0, NOSH); }
    m.add(boxG(0.03, 0.05, 0.06, 0), 'steelBare', [-0.025, 0.27, 0], [0, 0, 0], 'Body', NOSH);
    return m; },
};

// ---------------------------------------------------------------- cable channel (shared; kit content = textures)
// groove floor 0.62 (rails at ±0.31, 0.025 wide), hazard strips at ±0.36 (0.025 wide), copper Ø 0.043; everything ≤ 0.048,
// no shadows, no colliders. Composition per channel tile: ChannelHub at the centre (Junction hidden on straight runs);
// for each side: neighbour channel → ChannelArm rotated to that side, otherwise ChannelEdge; isolated tile → ChannelSingle.
const CH = { rail: 0.31, rw: 0.025, rh: 0.02, strip: 0.36, sw: 0.025, sy: 0.004, fy: 0.003, cr: 0.0215, cz: [-0.12, 0, 0.12] };
const CH_NOTE = 'Kanal işareti değişmez: zemine gömülü koyu oluk, kenarda turuncu-siyah şerit, görünür bakır. Hepsi ≤ 0,048 yüksek, gölgesiz, çarpıştırıcısız; kit yalnız oluk ve kuyu malzemesini değiştirir.';
const chModel = (name, listName, note) => new Model(name, { family: 'raidcivil', budget: 'raid', listName, origin: 'karenin merkezi, pafta yüzeyi (y = 0)', note: `${note} ${CH_NOTE}` });
// rail / strip along +x side closing (Edge) or along an arm (Arm): mitred 45° ends meet the neighbours exactly
function edgeRail(m) { const a = CH.rail - CH.rw / 2, b = CH.rail + CH.rw / 2;
  m.add(prism([[a, -a], [b, -b], [b, b], [a, a]], (u, v, w) => [u, w, v], 0, CH.rh, { cap0: false }), 'r_trenchWall', [0, 0, 0], [0, 0, 0], 'Body', NOSH); }
function edgeStrip(m) { const a = CH.strip - CH.sw / 2, b = CH.strip + CH.sw / 2;
  m.add(flatXZ([[a, -a], [b, -b], [b, b], [a, a]], CH.sy), 'rc_channelHazard', [0, 0, 0], [0, 0, 0], 'Body', NOSH); }
function ChannelHub() {
  const m = chModel('ChannelHub', 'Kanal göbeği ve bağlantı kutusu', 'Karenin ortasındaki oluk zemini (0,62 kare) ve üstünde döküm bağlantı kutusu (Junction: uçta, köşede, T ve dörtlü kavşakta görünür; düz geçişte gizlenir).');
  m.add(flatXZ([[-CH.rail, -CH.rail], [CH.rail, -CH.rail], [CH.rail, CH.rail], [-CH.rail, CH.rail]], CH.fy), 'r_trench', [0, 0, 0], [0, 0, 0], 'Body', NOSH);
  m.part('Junction', { pivot: [0, 0, 0], kind: 'swap', note: 'görünürlük: düz geçişte ve tek karede gizli; uç, köşe, T, dörtlü kavşakta açık' });
  m.add(boxG(0.32, 0.0435, 0.32, 0.012, ['-y']), 'rc_junction', [0, 0.004 + 0.02175, 0], [0, 0, 0], 'Junction', NOSH); // box above the copper (top 0.0475)
  for (const sx of [-1, 1]) for (const sz of [-1, 1]) m.add(flatXZ([[-0.014, -0.014], [0.014, -0.014], [0.014, 0.014], [-0.014, 0.014]].map(([x, z]) => [x + sx * 0.118, z + sz * 0.118]), 0.0478), 'steelCast', [0, 0, 0], [0, 0, 0], 'Junction', NOSH); // lid bolts (flat)
  return m;
}
function ChannelArm() {
  const m = chModel('ChannelArm', 'Kanal kolu (merkezden kenara, +x)', 'Göbekten kare kenarına oluk zemini, iki yan ray ve iki turuncu-siyah şerit (iç uçları 45° gönye), merkezden kenara üç bakır kablo. Oyun K/G/B için 90° döndürür.');
  const r = CH.rail, a = r - CH.rw / 2, b = r + CH.rw / 2, sa = CH.strip - CH.sw / 2, sb = CH.strip + CH.sw / 2;
  m.add(flatXZ([[r, -r], [0.5, -r], [0.5, r], [r, r]], CH.fy), 'r_trench', [0, 0, 0], [0, 0, 0], 'Body', NOSH);
  for (const s of [-1, 1]) {
    const rail = s > 0 ? [[a, a], [0.5, a], [0.5, b], [b, b]] : [[a, -a], [b, -b], [0.5, -b], [0.5, -a]];
    m.add(prism(rail, (u, v, w) => [u, w, v], 0, CH.rh, { cap0: false }), 'r_trenchWall', [0, 0, 0], [0, 0, 0], 'Body', NOSH);
    const st = s > 0 ? [[sa, sa], [0.5, sa], [0.5, sb], [sb, sb]] : [[sa, -sa], [sb, -sb], [0.5, -sb], [0.5, -sa]];
    m.add(flatXZ(st, CH.sy), 'rc_channelHazard', [0, 0, 0], [0, 0, 0], 'Body', NOSH); }
  for (const z of CH.cz) m.cyl(CH.cr, 0.5, 8, 0, 'r_copper', [0.25, CH.fy + CH.cr, z], [0, 0, -90], 'Body', { open: true, cast: false });
  return m;
}
function ChannelEdge() {
  const m = chModel('ChannelEdge', 'Kanal kenarı (kapalı yan, +x)', 'Komşusu kanal olmayan yanı kapatan şerit çifti: iç ray (oluk duvarı) ve dışta turuncu-siyah tehlike şeridi; iki ucu 45° gönye. Oyun K/G/B için 90° döndürür.');
  edgeRail(m); edgeStrip(m);
  return m;
}
function ChannelSingle() {
  const m = chModel('ChannelSingle', 'Tek kanal karesi (yalıtılmış kuyu)', 'Komşusuz kanal karesi: kare kuyu zemini (kitin kuyu malzemesi), dört yanda ray ve turuncu-siyah şerit çerçevesi, kuyuda sarılı bakır kablo. Bağlantı kutusu yok.');
  const r = CH.rail, a = r - CH.rw / 2, b = r + CH.rw / 2, sa = CH.strip - CH.sw / 2, sb = CH.strip + CH.sw / 2;
  m.add(flatXZ([[-r, -r], [r, -r], [r, r], [-r, r]], CH.fy), 'r_trench', [0, 0, 0], [0, 0, 0], 'Body', NOSH);
  const rot = (p, k) => { let [x, z] = p; for (let i = 0; i < k; i++) [x, z] = [-z, x]; return [x, z]; };
  for (let k = 0; k < 4; k++) {
    m.add(prism([[a, -a], [b, -b], [b, b], [a, a]].map(p => rot(p, k)), (u, v, w) => [u, w, v], 0, CH.rh, { cap0: false }), 'r_trenchWall', [0, 0, 0], [0, 0, 0], 'Body', NOSH);
    m.add(flatXZ([[sa, -sa], [sb, -sb], [sb, sb], [sa, sa]].map(p => rot(p, k)), CH.sy), 'rc_channelHazard', [0, 0, 0], [0, 0, 0], 'Body', NOSH); }
  const pts = [], turns = 1.65, N = 24, y = CH.fy + CH.cr;                                                // copper coil (spiral) + lead
  for (let i = 0; i <= N; i++) { const t = i / N, a2 = t * turns * Math.PI * 2, rr = 0.075 + t * 0.13; pts.push([Math.cos(a2) * rr, y, -Math.sin(a2) * rr]); }
  const last = pts[pts.length - 1]; pts.push([last[0] + 0.02, y, last[2] + 0.05]);
  m.add(gTube(pts.map(p => [p[0], p[1], p[2]]), CH.cr, 5, 30, { tension: 0.4 }), 'r_copper', [0, 0, 0], [0, 0, 0], 'Body', NOSH);
  return m;
}

// ---------------------------------------------------------------- registry
const LIST = {
  weu: 'Sivil sunucu · Batı Avrupa: banka ATM\'si', eeu: 'Sivil sunucu · Doğu Avrupa: mikrofilm okuyucu', me: 'Sivil sunucu · Orta Doğu: gaz analiz dolabı',
  af: 'Sivil sunucu · Afrika: sevk telsizi', na: 'Sivil sunucu · Kuzey Amerika: depo kiosku', ru: 'Sivil sunucu · Rusya: operatör konsolu',
  in: 'Sivil sunucu · Hindistan: çağrı merkezi masası', cn: 'Sivil sunucu · Çin: kalite kontrol panosu', jk: 'Sivil sunucu · Japonya-Kore: temiz oda terminali',
  sea: 'Sivil sunucu · Güneydoğu Asya: test tezgâhı', sa: 'Sivil sunucu · Güney Amerika: röle panosu', oc: 'Sivil sunucu · Okyanusya: vinç kumanda kioskı',
};
export const REGIONS = ['weu', 'eeu', 'me', 'af', 'na', 'ru', 'in', 'cn', 'jk', 'sea', 'sa', 'oc'];
export const MODELS = {};
for (const id of REGIONS) MODELS['Civil_' + id] = { build: () => civil(id, LIST[id], EQ[id]) };
MODELS.Civil_default = { build: CivilDefault };
for (const id of REGIONS) MODELS['EntryDoor_' + id] = { build: DOORS[id] };
MODELS.EntryDoor_default = { build: DOORS.default };
Object.assign(MODELS, { ChannelHub: { build: ChannelHub }, ChannelArm: { build: ChannelArm }, ChannelEdge: { build: ChannelEdge }, ChannelSingle: { build: ChannelSingle } });
