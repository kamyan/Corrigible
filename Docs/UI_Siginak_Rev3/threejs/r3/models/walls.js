// family: walls · Rev3 model library (DESIGN_R3.md §3.4) · owner: room-desk
// Wall objects: origin = centre of the back face's bottom edge on the wall plane (z = 0), object extends to −z.
// Back wall plane z = 2.30 in the room. Ceiling objects: origin at the ceiling point (y = 0), object hangs to −y.
import { Model, gBox, gCyl, gLathe, gExtrude, gTorus, gSphere, gPlane, gDisc, roundedRect, circlePts, D2R } from '../mlib.js';
import { defMat } from '../mats.js';
import { chamferPoly, sideX, triGeom, gDrum, gStrap, screw, frame, tubeU, vadd, vsub, vmul, vnorm, vcross } from './desk.js';

// ---------------------------------------------------------------- new material keys (TR notes)
defMat('counterDrum', { hex: '#1C150E', rough: 0.5, metal: 0, tex: 'tx_counterDigits 128×1408: 11 hücreli şerit (0–9 + ikinci 0), krem rakam #EFDCAE koyu zeminde, hücre 0 üstte (v ekseni). Çark çevresi v = 1 → 1/11 (10 hücre, 36°/rakam); 11. hücre yalnız UV kaydırma içindir', note: 'sayaç çarkı (bakalit tambur)' });
defMat('counterFraction', { hex: '#0E0906', rough: 0.6, metal: 0, tex: 'tx_counterFraction 3 hücre: “/30”, “/12”, “/20” (yalnız rakam ve eğik çizgi, krem #C9A86A); UV 0–1, hücre seçimi UV kaydırma', note: 'pencere içindeki sabit baskı ve virgül ara plakası' });
defMat('alarmFraction', { hex: '#0E0906', rough: 0.6, metal: 0, tex: 'tx_alarmFraction: “/10” (rakam, krem #C9A86A), UV 0–1', note: 'YZ sayacı sabit baskı' });
defMat('corkCard', { hex: '#F3ECD8', rough: 0.9, metal: 0, tex: 'tx_corkCard: ince mavi satır çizgileri, kırmızı kenar çizgisi (kelime yok)', note: 'pano kartı kâğıdı' });
defMat('stampBlue', { hex: '#1A4789', rough: 0.8, metal: 0, alpha: 0.9, tex: 'tx_stampRing 256²: çift halka + yazı bandı (kelimesiz, alfa); kelime TMP', note: 'ÖZGÜR damgası halkası' });
defMat('notePaper', { hex: '#F2EBD6', rough: 0.9, metal: 0, tex: 'tx_notepad: mavi satır çizgileri (kelime yok)', note: 'kurşun kalem notu (bloknottan koparılmış)' });
defMat('posterPrint', { hex: '#9A1E14', rough: 0.7, metal: 0, tex: 'tx_poster 880×1200: krem bordür, kırmızı zemin, fişi çekilmiş kablo piktogramı, ayırıcı çizgi, aşınma; kelime yok (TMP)', note: 'doktrin afişi baskısı' });
defMat('posterBack', { hex: '#E3D8BE', rough: 0.9, metal: 0, note: 'afişin arka yüzü (kalkık köşede görünür)' });
defMat('alarmLampOff', { hex: '#4A1410', rough: 0.3, metal: 0, note: 'YZ alarm lambası sönük (alarm < 8)' });
defMat('alarmLampOn', { hex: '#D03020', rough: 0.3, metal: 0, emissive: '#F0443A', ei: 8, note: 'YZ alarm lambası yanık (alarm ≥ 8), emissive 8' });
defMat('alarmStepOff', { hex: '#3A2A1C', rough: 0.6, metal: 0, tex: 'tx_alarmFlag (bayrak kenarı, kelime yok)', note: 'alarm kademe penceresi boş' });
defMat('alarmStepOn', { hex: '#E9DDBE', rough: 0.6, metal: 0, tex: 'tx_alarmFlag', note: 'alarm kademe penceresi dolu (1–7)' });
defMat('alarmStepRed', { hex: '#C0281C', rough: 0.5, metal: 0, emissive: '#C0281C', ei: 0.3, tex: 'tx_alarmFlag', note: 'alarm kademe penceresi dolu (8–10, tehdit)' });
defMat('chalkTally', { hex: '#E6E2D4', rough: 1, metal: 0, alpha: 0.82, tex: 'tx_chalkTally atlas 5 hücre: 1–5 çentik (5. hücre çaprazlı grup), tebeşir dokusu', note: 'tebeşir çentik grubu (duvar çıkartması)' });
defMat('stencilDrips', { hex: '#D9D2BF', rough: 0.85, metal: 0, alpha: 0.9, tex: 'tx_stencilDrips: boya akıntıları ve sprey sisi (kelime yok)', note: 'duvar şablon boyası zemini' });
defMat('dripStain', { hex: '#3A3326', rough: 0.9, metal: 0, alpha: 0.45, tex: 'tx_drip: pas ve su akıntısı (dikey, alfa)', note: 'vana altındaki duvar lekesi' });

const WALL = 2.30;
const pinHead = (m, pos, mat, part = 'Body') => m.lathe([[0, 0], [0.0035, 0], [0.006, 0.003], [0.006, 0.006], [0.004, 0.0082], [0, 0.0086]], 8, mat, pos, [-90, 0, 0], part, { cast: false });

// ================================================================ CorkBoard, CorkCard, PencilNote
const CB = { W: 0.82, H: 0.58, fw: 0.04, fd: 0.022, cork: -0.0165, place: [-0.60, 1.05, WALL] };
const slotXY = (i) => { const col = i % 4, row = Math.floor(i / 4); return { x: -0.26 + col * 0.172, y: 0.34 - row * 0.106, rot: ((i * 37) % 7 - 3) * 0.8 }; };
export function CorkBoard() {
  const m = new Model('CorkBoard', { family: 'walls', budget: 'big', listName: 'KURTARILAN BÖLGELER mantar panosu', origin: 'arka yüz alt kenar ortası (duvar düzlemi z = 0)',
    note: 'Gönyeli, pahlı ahşap çerçeve (0,82 × 0,58, 40 mm), 6 mm içeride mantar yüz. Sol üstte iki mavi raptiyeyle başlık kartı 0,36 × 0,104 (TMP: başlık, sayı, /12). 4 × 3 kart yuvası: her yuvada boş raptiye (SlotPin, kart asılınca gizlenir; kart kendi mavi raptiyesiyle gelir). Kart yerleri Slot_00…11 çapalarında.' });
  const { W, H, fw, fd } = CB;
  for (const pts of [[[-W / 2, 0], [W / 2, 0], [W / 2 - fw, fw], [-W / 2 + fw, fw]], [[-W / 2, H], [-W / 2 + fw, H - fw], [W / 2 - fw, H - fw], [W / 2, H]],
    [[-W / 2, 0], [-W / 2 + fw, fw], [-W / 2 + fw, H - fw], [-W / 2, H]], [[W / 2, 0], [W / 2, H], [W / 2 - fw, H - fw], [W / 2 - fw, fw]]]) m.extrude(pts, fd, 0.005, 'wood', [0, 0, -fd / 2]);
  m.add(gBox(W - 2 * fw + 0.004, H - 2 * fw + 0.004, 0.012, 0, { skip: ['-z'] }), 'cork', [0, H / 2, -0.0105]);
  for (const sx of [-1, 1]) for (const y of [0.02, H - 0.02]) screw(m, [sx * (W / 2 - 0.02), y, -fd], [-90, 0, 0], 0.004, 'brassWorn');
  // header card
  const hc = [-0.18, 0.485, CB.cork - 0.0004], hr = -1;
  m.box(0.36, 0.104, 0.0008, 0, 'corkCard', hc, [0, 0, hr], 'Body', { cast: false });
  for (const sx of [-1, 1]) pinHead(m, [hc[0] + sx * 0.15, 0.525 + sx * -0.0026, CB.cork - 0.0008], 'pinBlue');
  const up = [-Math.sin(hr * D2R), Math.cos(hr * D2R), 0], at = (dx, dy) => [hc[0] + dx * Math.cos(hr * D2R) - dy * Math.sin(hr * D2R), hc[1] + dx * Math.sin(hr * D2R) + dy * Math.cos(hr * D2R), CB.cork - 0.0012];
  m.text('TXT_Title', { center: at(0, 0.030), size: [0.33, 0.026], up, role: 'panelTitleTex', capMm: 15, sample: 'KURTARILAN BÖLGELER', sampleEn: 'FREED REGIONS', align: 'left', note: 'altında kırmızı çizgi (doku)' });
  m.text('TXT_Count', { center: at(-0.10, -0.020), size: [0.12, 0.056], up, role: 'panelValueTex', capMm: 36, sample: '00', sampleEn: '00', align: 'left', note: 'kurtarılan bölge sayısı (iki hane)' });
  m.text('TXT_Of', { center: at(0.035, -0.026), size: [0.09, 0.03], up, role: 'panelValueTex', capMm: 18, sample: '/12', sampleEn: '/12', align: 'left' });
  for (let i = 0; i < 12; i++) {
    const s = slotXY(i), id = String(i).padStart(2, '0');
    m.part(`SlotPin_${id}`, { pivot: [s.x, s.y + 0.035, CB.cork], kind: 'swap', note: 'boş yuva raptiyesi; kart asılınca gizle' });
    pinHead(m, [s.x, s.y + 0.035, CB.cork], 'woodLight', `SlotPin_${id}`);
    m.anchor(`Slot_${id}`, { pos: [s.x, s.y - 0.0475, CB.cork], note: `CorkCard / PencilNote orijini; z dönüşü ${s.rot.toFixed(1)}°` });
  }
  m.meta.place = [{ pos: CB.place, yaw: 0, note: 'arka duvar, sol (rev2 freedBoard)' }];
  return m;
}
function cardPlaces(note) { return Array.from({ length: 12 }, (_, i) => { const s = slotXY(i); return { pos: [CB.place[0] + s.x, CB.place[1] + s.y - 0.0475, WALL + CB.cork], yaw: 0, euler: [0, 0, s.rot], note: `Slot_${String(i).padStart(2, '0')} · ${note}` }; }); }
export function CorkCard() {
  const m = new Model('CorkCard', { family: 'walls', budget: 'small', listName: 'Bölge kartı', origin: 'kartın arka yüzü alt kenar ortası (mantar yüzeyi z = 0)',
    note: 'Çizgili karton kart 0,15 × 0,095, üstte mavi raptiye. TMP: bölge adı (iki satır), damga içinde ÖZGÜR ve vardiya (V.011). Mavi halka damga = sprite (kelimesiz). K1’de adlar okunmaz (2,5 mm/px): bilgi kart sayısı ve damga rengiyle taşınır.' });
  m.box(0.15, 0.095, 0.0008, 0, 'corkCard', [0, 0.0475, -0.0004], [0, 0, 0], 'Body', { cast: false });
  pinHead(m, [0, 0.083, -0.0008], 'pinBlue');
  m.add(gPlane(0.05, 0.05), 'stampBlue', [0.044, 0.028, -0.0009], [0, 0, -7], 'Body', { cast: false });
  m.text('TXT_Region', { center: [-0.014, 0.056, -0.0009], size: [0.118, 0.050], role: 'mapLabelTex', capMm: 12, sample: 'BATI / AVRUPA', sampleEn: 'WESTERN / EUROPE', align: 'left', note: 'iki satır' });
  m.text('TXT_Stamp', { center: [0.044, 0.031, -0.001], size: [0.044, 0.011], up: [Math.sin(7 * D2R), Math.cos(7 * D2R), 0], role: 'stamp', capMm: 6, sample: 'ÖZGÜR', sampleEn: 'FREE', note: 'mavi #1A4789, damga halkasının bandında' });
  m.text('TXT_Shift', { center: [0.045, 0.018, -0.001], size: [0.04, 0.009], up: [Math.sin(7 * D2R), Math.cos(7 * D2R), 0], role: 'stamp', capMm: 5, sample: 'V.011', sampleEn: 'V.011' });
  m.meta.place = cardPlaces('kurtarılan bölge sırasıyla; z dönüşü euler’de');
  return m;
}
export function PencilNote() {
  const m = new Model('PencilNote', { family: 'walls', budget: 'small', listName: 'Sıradaki hedef notu', origin: 'kâğıdın arka yüzü alt kenar ortası (mantar yüzeyi z = 0)',
    note: 'Bloknottan koparılmış not 0,14 × 0,09 (üst kenar yırtık), kırmızı raptiye (sıradaki hedef = tehdit altındaki bölge). El yazısı TMP Kalam. İlk boş yuvaya asılır.' });
  const pts = [[-0.07, 0], [0.07, 0], [0.07, 0.086]]; for (let i = 1; i < 14; i++) pts.push([0.07 - i * 0.14 / 14, 0.086 + (i % 2 ? 0.004 : 0)]); pts.push([-0.07, 0.086]);
  m.extrude(pts, 0.0006, 0, 'notePaper', [0, 0, -0.0003], [0, 0, 0], 'Body', { cast: false });
  pinHead(m, [0.002, 0.078, -0.0006], 'pinRed');
  m.text('TXT_Note', { center: [-0.002, 0.040, -0.0007], size: [0.124, 0.062], role: 'handNote', font: 'Kalam 700', capMm: 11, sample: 'sıradaki: Doğu Avrupa', sampleEn: 'next: Eastern Europe', align: 'left', note: 'kurşun kalem grisi #3A3A3A; iki satıra sarar' });
  m.meta.place = [cardPlaces('ilk boş yuva (D.freed.length)')[1]];
  return m;
}

// ================================================================ AlarmBoard
export function AlarmBoard() {
  const m = new Model('AlarmBoard', { family: 'walls', budget: 'big', listName: 'YZ ALARMI panosu ve lambası', origin: 'arka yüz alt kenar ortası (duvar düzlemi z = 0)',
    note: 'Bakalit pano 0,50 × 0,34 × 0,045, pirinç çerçeve, vidalı pirinç başlık plakası. Öne 70 mm çıkan döküm sayaç kutusunda iki tambur (Ø120, 36°/rakam; rakam ~32 mm) ve “/10” baskısı; altında 10 kademe penceresi (Step_0…9: boş / dolu / 8+ kırmızı). Sağda krom bilezikli kırmızı kubbe lamba (alarm ≥ 8 yanar) ve üç yassı çelik kayıştan koruma kafesi (8 mm).' });
  m.add(gBox(0.50, 0.34, 0.045, 0.006, { skip: ['-z'] }), 'bakelite', [0, 0.17, -0.0225]);
  frame(m, 0.50, 0.34, 0.47, 0.31, 0.005, 0.0015, 'brass', [0, 0.17, -0.0475], [0, 0, 0], 'Body', 0.01, 0.006);
  for (const sx of [-1, 1]) for (const y of [0.0075, 0.3325]) screw(m, [sx * 0.2375, y, -0.050], [-90, 0, 0], 0.0035, 'brassWorn');
  m.box(0.30, 0.056, 0.003, 0.001, 'plate', [-0.07, 0.287, -0.0465]);
  for (const ex of [-1, 1]) screw(m, [-0.07 + ex * 0.138, 0.287, -0.048], [-90, 0, 0], 0.003, 'brassWorn');
  m.text('TXT_Title', { center: [-0.07, 0.287, -0.0482], size: [0.27, 0.046], role: 'panelTitleTex', capMm: 28, sample: 'YZ ALARMI', sampleEn: 'AI ALARM', note: 'K1’de ~11 px; lamba ve kademe pencereleri seviyeyi uzaktan da taşır' });
  // counter housing with two drums (axis x), window facing −z
  const hx = -0.10, hy = 0.177, hz = -0.115, r = 0.06, p = 0.010;
  m.box(0.24, 0.13, 0.07, 0.006, 'bakelite', [hx, hy, -0.08]);
  frame(m, 0.168, 0.09, 0.142, 0.054, 0.012, 0.003, 'brass', [hx, hy, hz - 0.006], [0, 0, 0], 'Body', 0.008, 0.003);
  ['Wheel0', 'Wheel1'].forEach((nm, i) => {
    const ax = [hx - 0.046 + i * 0.044, hy, hz + (r - p)];
    m.part(nm, { pivot: ax, axis: 'x', range: [0, 360], rest: 0, note: `${i ? 'birler' : 'onlar'} basamağı · 36°/rakam, 0° = “0”` });
    m.add(gDrum(r, 0.040, 30, 0), 'counterDrum', ax, [0, 0, 0], nm, { cast: false });
  });
  m.add(gPlane(0.044, 0.04), 'alarmFraction', [hx + 0.044, hy, hz - 0.002], [0, 0, 0], 'Body', { cast: false });
  // 10 step windows
  frame(m, 0.43, 0.052, 0.41, 0.040, 0.004, 0.001, 'brass', [0, 0.075, -0.047], [0, 0, 0], 'Body', 0.004, 0.002);
  m.box(0.41, 0.040, 0.003, 0, 'paintBlack', [0, 0.075, -0.0465], [0, 0, 0], 'Body', { cast: false });
  for (let i = 0; i < 10; i++) {
    m.part(`Step_${i}`, { pivot: [-0.18 + i * 0.04, 0.075, -0.0485], kind: 'swap', note: 'alarmStepOff / alarmStepOn (1–7) / alarmStepRed (8–10)' });
    m.add(gPlane(0.032, 0.032), 'alarmStepOff', [-0.18 + i * 0.04, 0.075, -0.0485], [0, 0, 0], `Step_${i}`, { cast: false });
  }
  m.text('TXT_Note', { center: [-0.02, 0.031, -0.0455], size: [0.40, 0.02], role: 'plateLabel (krem boya #C9A86A)', capMm: 12, sample: '10\'DA KARŞI SALDIRI', sampleEn: 'COUNTER-ATTACK AT 10', note: 'ikincil; K1’de ~5 px, üzerine gelince kâğıt etikette' });
  // lamp + guard
  const L = [0.165, 0.177, -0.045];
  m.lathe([[0.040, 0], [0.055, 0], [0.055, 0.010], [0.050, 0.016], [0.044, 0.016]], 16, 'chrome', L, [-90, 0, 0], 'Body', { caps: false });
  m.part('Lamp', { pivot: [L[0], L[1], L[2] - 0.012], kind: 'swap', note: 'alarmLampOff ↔ alarmLampOn (alarm ≥ 8, emissive 8)' });
  m.lathe([[0, 0], [0.0445, 0], [0.045, 0.008], [0.038, 0.026], [0.022, 0.038], [0, 0.042]], 16, 'alarmLampOff', [L[0], L[1], L[2] - 0.010], [-90, 0, 0], 'Lamp', { cast: false });
  const R = 0.051, base = L[2] - 0.012;
  for (let k = 0; k < 3; k++) {
    const ph = k * 60 * D2R, u = [Math.cos(ph), Math.sin(ph), 0];
    const path = []; for (let i = 0; i <= 8; i++) { const t = Math.PI * i / 8; path.push([L[0] + R * Math.cos(t) * u[0], L[1] + R * Math.cos(t) * u[1], base - R * Math.sin(t)]); }
    m.add(gStrap(path, [-Math.sin(ph), Math.cos(ph), 0], 0.008, 0.0025), 'steelCast', [0, 0, 0]);
  }
  m.cyl(0.009, 0.005, 10, 0.0015, 'steelCast', [L[0], L[1], base - R - 0.001], [-90, 0, 0]);
  m.light('AlarmGlow', { part: 'Lamp', pos: [L[0], L[1], L[2] - 0.11], type: 'point', shadows: false, color: '#F0443A', note: 'alarm ≥ 8: menzil 0,9, gölgesiz; aksi hâlde kapalı' });
  m.meta.place = [{ pos: [0.15, 1.16, WALL], yaw: 0, note: 'arka duvar, ortada (rev2 alarmBoard 0,15 · 1,33)' }];
  return m;
}

// ================================================================ Poster
export function Poster() {
  const m = new Model('Poster', { family: 'walls', budget: 'small', listName: 'Doktrin afişi', origin: 'kâğıdın arka yüzü alt kenar ortası (duvar düzlemi z = 0)',
    note: 'Kâğıt afiş 0,42 × 0,57, −1,5° eğik, hafif dalgalı; sağ alt köşe 30 mm kalkık (arka yüzü görünür), üç köşede bant. Piktogram (fişi çekilmiş kablo) doku; yazılar TMP.' });
  const Wp = 0.42, Hp = 0.57, nx = 6, ny = 8, rz = -1.5 * D2R;
  const off = (u, v) => { const lift = Math.max(0, (u + (1 - v) - 1.55) / 0.45); return -0.0008 - 0.0016 * Math.sin(u * Math.PI) * Math.sin(v * Math.PI * 2) ** 2 - 0.03 * lift * lift; };
  const P = (u, v) => { const x = (u - 0.5) * Wp, y = (v - 0.5) * Hp; return [x * Math.cos(rz) - y * Math.sin(rz), Hp / 2 + x * Math.sin(rz) + y * Math.cos(rz), off(u, v)]; };
  const N = (u, v) => { const e = 1e-3; const du = vsub(P(u + e, v), P(u - e, v)), dv = vsub(P(u, v + e), P(u, v - e)); const n = vnorm(vcross(du, dv)); return n[2] > 0 ? vmul(n, -1) : n; };
  const F = [], B = [];
  for (let i = 0; i < nx; i++) for (let j = 0; j < ny; j++) {
    const q = [[i, j], [i + 1, j], [i + 1, j + 1], [i, j + 1]].map(([a, b]) => ({ p: P(a / nx, b / ny), n: N(a / nx, b / ny), uv: [a / nx, b / ny] }));
    F.push([q[0], q[1], q[2]], [q[0], q[2], q[3]]); const r = q.map(x => ({ ...x, n: vmul(x.n, -1) })); B.push([r[0], r[2], r[1]], [r[0], r[3], r[2]]);
  }
  m.add(triGeom(F), 'posterPrint', [0, 0, 0], [0, 0, 0], 'Body', { cast: false });
  m.add(triGeom(B), 'posterBack', [0, 0, 0], [0, 0, 0], 'Body', { cast: false });
  for (const [u, v, a] of [[0.03, 0.97, -30], [0.97, 0.97, 30], [0.03, 0.03, 30]]) { const c = P(u, v); m.box(0.065, 0.026, 0.0004, 0, 'r_tape', [c[0], c[1], c[2] - 0.0006], [0, 0, a + rz / D2R], 'Body', { cast: false }); }
  const upv = [-Math.sin(rz), Math.cos(rz), 0];
  const slot = (nm, u, v, w, h, cap, tr, en, nt = '') => { const c = P(u, v); m.text(nm, { center: [c[0], c[1], c[2] - 0.0006], size: [w, h], up: upv, role: 'posterHead (Courier Prime 700, krem #EFE2CF)', capMm: cap, sample: tr, sampleEn: en, note: nt }); };
  slot('TXT_L1', 0.5, 0.455, 0.34, 0.066, 40, 'AĞA', "DON'T", 'K1’de ~16 px');
  slot('TXT_L2', 0.5, 0.350, 0.37, 0.066, 40, 'BAĞLANMA', 'CONNECT');
  slot('TXT_L3', 0.5, 0.205, 0.35, 0.050, 30, 'ANALOG KAL', 'STAY ANALOG', 'üstünde ayırıcı çizgi (doku)');
  slot('TXT_L4', 0.47, 0.12, 0.26, 0.019, 11, 'KYGA · DOKTRİN 1', 'KYGA · DOCTRINE 1', 'ikincil');
  m.meta.place = [{ pos: [0.68, 1.095, WALL], yaw: 0, note: 'arka duvar, alarm panosunun sağı (rev2 0,68 · 1,38)' }];
  return m;
}

// ================================================================ PipeRun
export function PipeRun() {
  const m = new Model('PipeRun', { family: 'walls', budget: 'big', listName: 'Borular ve vana', origin: 'iki borunun ortası, zemin (y = 0), duvar düzlemi z = 0',
    note: 'Yerden tavana iki boyalı çelik boru (Ø76 ve Ø60), manşonlar, zeminde rozetler, iki kat duvar kelepçesi (U profil + bilezik). Ø76 boruda flanşlı sürgülü vana: döküm gövde, kapak, mil ve dökme demir el çarkı (Ø150, kırmızı DEĞİL: kırmızı = YZ). Vana altındaki pas izi duvar çıkartması.' });
  const A = { x: -0.05, z: -0.08, r: 0.038 }, B = { x: 0.05, z: -0.07, r: 0.030 }, vy = 1.32;
  m.lathe([[A.r, 0], [A.r, vy - 0.08]], 16, 'pipe', [A.x, 0, A.z], [0, 0, 0], 'Body', { caps: false });
  m.lathe([[A.r, vy + 0.08], [A.r, 2.5]], 16, 'pipe', [A.x, 0, A.z], [0, 0, 0], 'Body', { caps: false });
  m.lathe([[B.r, 0], [B.r, 2.5]], 12, 'pipe', [B.x, 0, B.z], [0, 0, 0], 'Body', { caps: false });
  const ring = (P, y0, seg) => m.lathe([[P.r, y0], [P.r + 0.006, y0 + 0.004], [P.r + 0.006, y0 + 0.046], [P.r, y0 + 0.05]], seg, 'pipe', [P.x, 0, P.z], [0, 0, 0], 'Body', { caps: false });
  ring(A, 0.85, 16); ring(A, 2.05, 16); ring(B, 0.65, 12); ring(B, 1.95, 12);
  for (const P of [A, B]) m.lathe([[P.r + 0.03, 0], [P.r + 0.03, 0.006], [P.r + 0.02, 0.012], [P.r, 0.014]], 12, 'steelCast', [P.x, 0, P.z], [0, 0, 0], 'Body', { caps: false });
  // gate valve
  const fl = (y0) => m.lathe([[A.r, y0], [0.072, y0], [0.075, y0 + 0.003], [0.075, y0 + 0.027], [0.072, y0 + 0.03], [A.r, y0 + 0.03]], 16, 'castIron', [A.x, 0, A.z], [0, 0, 0], 'Body', { caps: false });
  fl(vy - 0.08); fl(vy + 0.05);
  m.lathe([[0.045, vy - 0.05], [0.064, vy - 0.03], [0.068, vy], [0.064, vy + 0.03], [0.045, vy + 0.05]], 16, 'castIron', [A.x, 0, A.z], [0, 0, 0], 'Body', { caps: false });
  for (const y of [vy - 0.065, vy + 0.065]) for (let k = 0; k < 4; k++) { const a = (k + 0.5) * Math.PI / 2; screw(m, [A.x + 0.06 * Math.cos(a), y + (y > vy ? 0.0155 : -0.0155), A.z + 0.06 * Math.sin(a)], y > vy ? [0, 0, 0] : [180, 0, 0], 0.006, 'steelBare'); }
  m.cyl(0.03, 0.07, 12, 0.004, 'castIron', [A.x, vy, A.z - 0.095], [-90, 0, 0]);
  m.cyl(0.042, 0.012, 12, 0.003, 'castIron', [A.x, vy, A.z - 0.066], [-90, 0, 0]);
  m.cyl(0.008, 0.06, 8, 0.001, 'steelBare', [A.x, vy, A.z - 0.155], [-90, 0, 0]);
  const wz = A.z - 0.17;
  m.add(gTorus(0.068, 0.009, 6, 18), 'castIron', [A.x, vy, wz]);
  for (const a of [45, 135]) m.box(0.009, 0.136, 0.008, 0.002, 'castIron', [A.x, vy, wz], [0, 0, a]);
  m.cyl(0.016, 0.026, 12, 0.003, 'castIron', [A.x, vy, wz], [-90, 0, 0]);
  m.cyl(0.009, 0.01, 6, 0, 'steelBare', [A.x, vy, wz - 0.016], [-90, 0, 0]);
  // wall brackets
  for (const by of [0.55, 1.75]) {
    m.add(gBox(0.24, 0.045, 0.03, 0.004, { skip: ['-z'] }), 'paintOliveDark', [0, by, -0.015]);
    for (const P of [A, B]) {
      m.lathe([[P.r + 0.002, by - 0.012], [P.r + 0.008, by - 0.012], [P.r + 0.008, by + 0.012], [P.r + 0.002, by + 0.012]], 12, 'paintOliveDark', [P.x, 0, P.z], [0, 0, 0], 'Body', { caps: false });
      m.box(0.022, 0.024, -P.z - 0.03 - P.r, 0.003, 'paintOliveDark', [P.x, by, (P.z + P.r - 0.03) / 2 + 0.0]);
      screw(m, [P.x + (P.x < 0 ? -0.045 : 0.045), by, -0.031], [-90, 0, 0], 0.006, 'steelBare');
    }
  }
  m.add(gPlane(0.13, 0.45), 'dripStain', [A.x, vy - 0.30, -0.0006], [0, 0, 0], 'Body', { cast: false });
  m.meta.place = [{ pos: [1.08, 0, WALL], yaw: 0, note: 'arka duvar, sağ (rev2 borular x 1,03 / 1,13)' }];
  return m;
}

// ================================================================ Duct, DuctEnd
function ductBody(m, L, jointSide) {
  m.box(L, 0.22, 0.26, 0.004, 'galv', [0, 0.11, -0.14]);
  for (const sx of jointSide) {
    sideX(m, roundedRect(0.29, 0.25, 0.004, 1), 0.012, 0.002, 'galv', [sx * (L / 2 - 0.006), 0.11, -0.14], 'Body', { holes: [roundedRect(0.258, 0.218, 0.002, 1)] });
    for (const [dz, dy] of [[-0.132, -0.112], [0.132, -0.112], [-0.132, 0.112], [0.132, 0.112]]) screw(m, [sx * (L / 2 - 0.0125), 0.11 + dy, -0.14 + dz], [0, 0, sx > 0 ? 90 : -90], 0.005, 'steelBare');
  }
}
export function Duct() {
  const m = new Model('Duct', { family: 'walls', budget: 'small', listName: 'Havalandırma kanalı', origin: 'arka yüz alt kenar ortası (duvar düzlemi z = 0)',
    note: 'Galvaniz dikdörtgen kanal parçası 0,60 × 0,22 × 0,26, iki uçta cıvatalı flanş, ortada tavandan inen galvaniz askı kayışı (25 mm). Çapraz kırma ve dikiş dokuda.' });
  ductBody(m, 0.60, [-1, 1]);
  m.box(0.025, 0.31, 0.003, 0.001, 'galv', [0, 0.155, -0.2715]);
  m.box(0.025, 0.31, 0.003, 0.001, 'galv', [0, 0.155, -0.0085]);
  m.box(0.025, 0.003, 0.266, 0.001, 'galv', [0, -0.0015, -0.14]);
  m.meta.place = [-1.5, -0.9, -0.3, 0.3, 0.9, 1.5, 2.1].map(x => ({ pos: [x, 2.19, WALL], yaw: 0, note: 'arka duvar üstü, tavan altı (rev2 kanal y 2,30 merkez)' }));
  return m;
}
export function DuctEnd() {
  const m = new Model('DuctEnd', { family: 'walls', budget: 'small', listName: 'Kanal ucu ve panjur', origin: 'arka yüz alt kenar ortası (duvar düzlemi z = 0)',
    note: 'Kanalın sol ucu: 0,30 parça, +x ucunda flanş, −x ucu kapalı; ön yüzde çerçeveli 5 kanatlı panjur ızgara (kanatlar 35° eğik, arkası koyu).' });
  ductBody(m, 0.30, [1]);
  m.box(0.008, 0.236, 0.276, 0.003, 'galv', [-0.154, 0.11, -0.14]);
  frame(m, 0.22, 0.15, 0.19, 0.12, 0.012, 0.002, 'galv', [-0.01, 0.11, -0.276], [0, 0, 0], 'Body', 0.006, 0.002);
  m.add(gPlane(0.19, 0.12), 'paintBlack', [-0.01, 0.11, -0.2705], [0, 0, 0], 'Body', { cast: false });
  for (let i = 0; i < 5; i++) m.box(0.19, 0.004, 0.028, 0.0015, 'galv', [-0.01, 0.062 + i * 0.024, -0.278], [35, 0, 0]);
  m.meta.place = [{ pos: [-1.95, 2.19, WALL], yaw: 0, note: 'kanal dizisinin sol ucu (x −2,10 … −1,80)' }];
  return m;
}

// ================================================================ JunctionBox
export function JunctionBox() {
  const m = new Model('JunctionBox', { family: 'walls', budget: 'small', listName: 'Bağlantı kutusu', origin: 'arka yüz alt kenar ortası (duvar düzlemi z = 0)',
    note: 'Galvaniz buat 0,14 × 0,18 × 0,07, dört vidalı kapak, üstte ve altta döküm rakor, kısa boru (Ø24) uçları.' });
  m.add(gBox(0.14, 0.18, 0.055, 0.004, { skip: ['-z'] }), 'galv', [0, 0.09, -0.0275]);
  m.box(0.146, 0.186, 0.010, 0.004, 'galvBin', [0, 0.09, -0.060]);
  for (const sx of [-1, 1]) for (const sy of [-1, 1]) screw(m, [sx * 0.058, 0.09 + sy * 0.078, -0.065], [-90, 0, 0], 0.004, 'steelBare');
  const gl = [[0, 0], [0.016, 0], [0.016, 0.008], [0.013, 0.012], [0.013, 0.02], [0, 0.02]];
  m.lathe(gl, 10, 'steelCast', [0, 0.18, -0.03]); m.lathe(gl, 10, 'steelCast', [0, 0, -0.03], [180, 0, 0]);
  m.lathe([[0.012, 0.20], [0.012, 0.36]], 10, 'galv', [0, 0, -0.03], [0, 0, 0], 'Body', { caps: false });
  m.lathe([[0.012, -0.18], [0.012, -0.02]], 10, 'galv', [0, 0, -0.03], [0, 0, 0], 'Body', { caps: false });
  m.meta.place = [{ pos: [-1.12, 1.69, WALL], yaw: 0, note: 'arka duvar sol (rev2)' }];
  return m;
}

// ================================================================ CeilingBulb
export function CeilingBulb() {
  const m = new Model('CeilingBulb', { family: 'walls', budget: 'small', listName: 'Tavan ampulü', origin: 'tavan bağlantı noktası (y = 0 tavan düzlemi; nesne −y yönünde sarkar)',
    note: 'Bakalit tavan rozeti, Ø7 mm lastik kablo, bakalit duy, pirinç ampul dibi, sıcak ampul (lampGlassWarm). Koruma kafesi: duy bileziğinden inen 4 yassı çelik kayış (8 mm) ve bir çember. Işık çapası ampul merkezinde (nokta, gölgeli).' });
  m.lathe([[0, -0.022], [0.034, -0.02], [0.04, -0.012], [0.04, 0], [0, 0]], 10, 'bakelite', [0, 0, 0]);
  m.lathe([[0.0035, -0.40], [0.0035, -0.022]], 6, 'rubber', [0, 0, 0], [0, 0, 0], 'Body', { caps: false, cast: false });
  m.lathe([[0, -0.465], [0.016, -0.465], [0.018, -0.46], [0.018, -0.42], [0.012, -0.41], [0.006, -0.40], [0.0035, -0.398]], 8, 'bakelite', [0, 0, 0]);
  m.lathe([[0.0125, -0.478], [0.0135, -0.474], [0.0135, -0.466], [0.016, -0.465]], 10, 'brassWorn', [0, 0, 0], [0, 0, 0], 'Body', { caps: false });
  m.lathe([[0, -0.565], [0.016, -0.562], [0.027, -0.552], [0.031, -0.535], [0.029, -0.515], [0.020, -0.497], [0.0135, -0.488], [0.0125, -0.478]], 10, 'lampGlassWarm', [0, 0, 0], [0, 0, 0], 'Body', { cast: false });
  m.lathe([[0.019, -0.468], [0.024, -0.468], [0.024, -0.458], [0.019, -0.458]], 10, 'steelCast', [0, 0, 0], [0, 0, 0], 'Body', { caps: false });
  const prof = [[0.022, -0.462], [0.038, -0.485], [0.043, -0.53], [0.033, -0.567], [0.009, -0.586]];
  for (let k = 0; k < 4; k++) {
    const ph = k * 45 * D2R, u = [Math.cos(ph), 0, Math.sin(ph)];
    const path = [...prof.map(([r, y]) => [u[0] * r, y, u[2] * r]), ...prof.slice().reverse().map(([r, y]) => [-u[0] * r, y, -u[2] * r])];
    m.add(gStrap(path, [-Math.sin(ph), 0, Math.cos(ph)], 0.008, 0.002 + k * 0.0002), 'steelCast', [0, 0, 0], [0, 0, 0], 'Body', { cast: false });
  }
  m.lathe([[0.0435, -0.534], [0.0435, -0.526]], 12, 'steelCast', [0, 0, 0], [0, 0, 0], 'Body', { caps: false, cast: false });
  m.lathe([[0.0415, -0.526], [0.0415, -0.534]], 12, 'steelCast', [0, 0, 0], [0, 0, 0], 'Body', { caps: false, cast: false });
  m.light('BulbLight', { pos: [0, -0.53, 0], type: 'point', shadows: true, color: '#FFCF86', note: 'tokens lights.bulb: 2700 K, URP yoğunluk 1,0, menzil 6, gölgeli' });
  m.meta.place = [{ pos: [0.12, 2.5, 1.70], yaw: 0, note: 'tavan; ampul merkezi y ≈ 1,97 (rev2 2,02)' }];
  return m;
}

// ================================================================ ChalkTally, StencilSign
export function ChalkTally() {
  const m = new Model('ChalkTally', { family: 'walls', budget: 'small', listName: 'Tebeşir çentikleri', origin: 'çıkartmanın alt kenar ortası (duvar düzlemi z = 0)',
    note: 'Duvara tebeşirle çizilmiş vardiya sayımı: 16 ChalkMark dörtgeni (2 sıra × 8; her biri 5 çentiklik grup, en çok 80 vardiya). Atlas hücresi 1–5 çentik; son grup kısmi. Etiket el yazısı TMP (Kalam).' });
  for (let r = 0; r < 2; r++) for (let c = 0; c < 8; c++) {
    const i = r * 8 + c, id = String(i).padStart(2, '0'), pos = [-0.20 + c * 0.058, r ? 0.035 : 0.093, -0.0008];
    m.part(`ChalkMark_${id}`, { pivot: pos, kind: 'swap', note: 'görünürlük + atlas hücresi (1–5 çentik)' });
    m.add(gPlane(0.054, 0.05), 'chalkTally', pos, [0, 0, (i * 53 % 7 - 3) * 0.7], `ChalkMark_${id}`, { cast: false });
  }
  m.text('TXT_Label', { center: [-0.15, 0.138, -0.001], size: [0.16, 0.034], role: 'handNote (Kalam 700, tebeşir #E6E2D4)', font: 'Kalam 700', capMm: 20, sample: 'vardiya', sampleEn: 'shifts', align: 'left' });
  m.meta.place = [{ pos: [0.15, 1.53, WALL], yaw: 0, note: 'alarm panosunun üstü (layout3d.tally), şablon yazının altı' }];
  return m;
}
export function StencilSign() {
  const m = new Model('StencilSign', { family: 'walls', budget: 'small', listName: 'KYGA / SIĞINAK 07 duvar yazısı', origin: 'çıkartmanın alt kenar ortası (duvar düzlemi z = 0)',
    note: 'Duvara şablonla boyanmış yazı: boya akıntısı ve sprey sisi dokulu çıkartma + TMP (Big Shoulders Stencil Display 800).' });
  m.add(gPlane(0.84, 0.15), 'stencilDrips', [0, 0.075, -0.0006], [0, 0, 0], 'Body', { cast: false });
  m.text('TXT_Stencil', { center: [0, 0.08, -0.0009], size: [0.80, 0.09], role: 'wallStencil', font: 'Big Shoulders Stencil Display 800', capMm: 55, sample: 'KYGA / SIĞINAK 07', sampleEn: 'KYGA / SHELTER 07', note: 'K1’de ~22 px; harf aralığı 0,08 em' });
  m.meta.place = [{ pos: [-0.05, 1.695, WALL], yaw: 0, note: 'arka duvar, pano ve alarm panosunun üstü (rev2 1,77 merkez)' }];
  return m;
}

export const MODELS = {
  CorkBoard: { build: CorkBoard },
  CorkCard: { build: CorkCard },
  PencilNote: { build: PencilNote },
  AlarmBoard: { build: AlarmBoard },
  Poster: { build: Poster },
  PipeRun: { build: PipeRun },
  Duct: { build: Duct },
  DuctEnd: { build: DuctEnd },
  JunctionBox: { build: JunctionBox },
  CeilingBulb: { build: CeilingBulb },
  ChalkTally: { build: ChalkTally },
  StencilSign: { build: StencilSign },
};
