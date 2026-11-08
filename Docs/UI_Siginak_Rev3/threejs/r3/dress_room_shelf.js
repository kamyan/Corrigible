// Rev3 room dressing for the room-shelf families (shelf, crt, door) — owner: room-shelf.
// dress(I, ctx): I = { ModelName: [instances] } placed by room3.js via meta.place (WORLD coords);
// ctx = { lang, sk, D (DESK state, D.state = rooms/idle/reserve/erzak/malzeme/plaka/casting/patient), stage, door, LOG, THREE, selRoom? }.
// Fills TMP slots with canvas text (TMP stand-in), swaps state materials, poses parts, and builds the state-dependent
// instances (room cards, room props, depot band, pegs, visitor) as children of their local parents (diorama, tray, corridor).
import * as THREE from 'three';
import { inst, slotText, printPlane, matLike, canvas } from './scene3.js';
import * as T from '../rev2/textures.js';
import * as X from '../rev2/tex2.js';
import { DIO } from './models/shelf.js';

// models the room composition instantiates from meta.place; everything else below is built here
export const PLACED = ['Shelf', 'Crate', 'Jerrycan', 'HopePlant1', 'HopePlant2', 'HopePlant3', 'Diorama', 'ClipboardStand', 'IdleTray',
  'CrtTable', 'Crt', 'Radio', 'Microphone', 'LogBook', 'DoorFrame', 'DoorLeaf', 'KnockLamp', 'IntercomBox', 'HazardHeader', 'Corridor', 'Handrail', 'CorridorLamp'];
const KINDS = ['anne', 'yasli', 'silahli', 'asker', 'yarali', 'kopya', 'genel1', 'genel2', 'genel3'];
const DYN = ['DioramaRoomProps_yasam', 'DioramaRoomProps_telsiz', 'DioramaRoomProps_atolye', 'DioramaRoomProps_revir', 'DioramaDamage', 'DepotCrate', 'DepotBin', 'DepotPlate',
  'DepotSlate', 'RoomCardHolder', 'RoomCard', 'PegSlot', 'Peg', 'Peg_a', 'Peg_b', 'Peg_c', 'Peg_d', 'Peg_e', 'Peg_f', ...KINDS.map(k => 'Visitor_' + k)];
const PEGS = ['Peg_a', 'Peg_b', 'Peg_c', 'Peg_d', 'Peg_e', 'Peg_f'];
const EN_ROOM = { yasam: 'LIVING QUARTERS', telsiz: 'RADIO ROOM', atolye: 'WORKSHOP', revir: 'INFIRMARY' };
const EN_STAMP = { HASARLI: 'DAMAGED', DURDU: 'STOPPED' };
const INK = '#1E1B16', PLATE_INK = '#2A1E10';

// ---------------------------------------------------------------- small utilities
const first = (I, n) => (I[n] || []).find(Boolean) || null;
const vis = (m, part, on) => { const P = m && m.parts[part]; if (P) P.group.visible = !!on; };
function swap(m, part, from, to) { if (!m) return; m.root.traverse(o => { if (o.isMesh && o.userData.mat === from && (!part || o.name.startsWith(part + ':'))) { o.material = matLike(to); o.userData.mat = to; } }); }
function put(m, name, text, o = {}) { // fill a TMP slot (font size from capMm)
  const t = m && m.meta.texts.find(q => q.name === name); if (!t || text == null || text === '') return null;
  const ppm = o.ppm || 4000, size = (o.capMm || t.capMm || t.size[1] * 700) * ppm / 1000 / 0.7;
  return slotText(m, name, [{ t: String(text), size, c: o.c || INK, f: o.f || 'cp', w: o.w || 700, ls: o.ls ?? 0.04 }],
    { ppm, align: o.align || t.align, pad: o.pad ?? 0, emissive: o.emissive, ei: o.ei, rough: o.rough, metal: o.metal, lift: o.lift });
}
const slotOf = (m, name) => m.meta.texts.find(q => q.name === name);
function tex(c) { const t = new THREE.CanvasTexture(c); t.colorSpace = THREE.SRGBColorSpace; t.anisotropy = 8; return t; }
function addLight(m, light, pos, target) { light.position.set(pos[0], pos[1], -pos[2]); light.userData.dress = 'shelf'; m.root.add(light);
  if (target && light.target) { light.target.position.set(target[0], target[1], -target[2]); m.root.add(light.target); } return light; }

// ---------------------------------------------------------------- canvases
function personIcon() { const [c, x] = canvas(96, 112); x.fillStyle = '#2E2820'; x.beginPath(); x.arc(48, 28, 20, 0, Math.PI * 2); x.fill();
  x.beginPath(); x.moveTo(10, 112); x.lineTo(10, 86); x.quadraticCurveTo(10, 56, 48, 56); x.quadraticCurveTo(86, 56, 86, 86); x.lineTo(86, 112); x.closePath(); x.fill(); return c; }
function stampCanvas(word) { const [c, x] = canvas(640, 192); x.strokeStyle = '#9A1E14'; x.fillStyle = '#9A1E14'; x.globalAlpha = 0.86;
  x.lineWidth = 10; x.strokeRect(14, 14, 612, 164); x.lineWidth = 5; x.strokeRect(32, 32, 576, 128);
  x.font = '700 96px "Courier Prime"'; x.textAlign = 'center'; x.textBaseline = 'middle'; let s = 96; while (x.measureText(word).width > 540 && s > 30) { s -= 4; x.font = `700 ${s}px "Courier Prime"`; }
  x.fillText(word, 320, 100); return c; }
function dialScale() { const [c, x] = canvas(1000, 300); const g = x.createLinearGradient(0, 0, 0, 300); g.addColorStop(0, '#F0BD62'); g.addColorStop(1, '#C4802F'); x.fillStyle = g; x.fillRect(0, 0, 1000, 300);
  x.fillStyle = '#3A240C'; for (let i = 0; i <= 50; i++) { const xx = 50 + i * 18; x.fillRect(xx - 2, 190, 4, i % 5 === 0 ? 80 : 40); }
  x.font = '700 64px "Courier Prime"'; x.textAlign = 'center'; x.textBaseline = 'alphabetic'; for (let ch = 1; ch <= 11; ch += 2) x.fillText(String(ch).padStart(2, '0'), 50 + (ch - 1) * 90, 160); return c; }
function meterFace() { const [c, x] = canvas(400, 350); x.fillStyle = '#EFE4BC'; x.fillRect(0, 0, 400, 350); x.strokeStyle = '#2A1E10'; x.lineWidth = 6;
  const cx = 200, cy = 300, R = 230; x.beginPath(); x.arc(cx, cy, R, Math.PI * 1.23, Math.PI * 1.77); x.stroke();
  for (let i = 0; i <= 10; i++) { const a = Math.PI * (1.23 + 0.054 * i), r2 = i % 5 ? R - 26 : R - 42; x.lineWidth = i % 5 ? 4 : 7; x.beginPath(); x.moveTo(cx + Math.cos(a) * R, cy + Math.sin(a) * R); x.lineTo(cx + Math.cos(a) * r2, cy + Math.sin(a) * r2); x.stroke(); }
  return c; }
function logPage(head, entries, lines = 15) { const W = 590, H = 1000; const [c, x] = canvas(W, H); x.fillStyle = '#F2EAD2'; x.fillRect(0, 0, W, H);
  x.fillStyle = '#7A8FB5'; for (let i = 0; i < lines; i++) x.fillRect(0, 150 + i * 56, W, 2); x.fillStyle = '#D2483F'; x.fillRect(118, 110, 3, H - 110);
  if (head) { x.fillStyle = INK; x.font = '700 40px "Courier Prime"'; x.textAlign = 'left'; x.textBaseline = 'alphabetic'; x.fillText(head, 14, 80); }
  entries.forEach(([t, msg], i) => { const y = 142 + i * 56; x.fillStyle = '#463F34'; x.font = '700 28px "Courier Prime"'; x.fillText(t, 10, y);
    x.fillStyle = '#1A2F5A'; x.font = '700 34px "Kalam"'; let s = 34; while (x.measureText(msg).width > W - 140 && s > 18) { s -= 2; x.font = `700 ${s}px "Kalam"`; } x.fillText(msg, 130, y + 2); });
  return c; }
function clipSummary(D, ctx) { const W = 740, H = 1080; const [c, x] = canvas(W, H); const en = ctx.lang === 'en'; const S = D.state || {};
  x.fillStyle = '#EFE6D0'; x.fillRect(0, 0, W, H); x.textBaseline = 'alphabetic'; x.textAlign = 'left';
  const t = (s, px, y, f = '700', col = INK, xx = 40) => { x.font = `${f} ${px}px "Courier Prime"`; x.fillStyle = col; x.fillText(s, xx, y); };
  t(en ? 'SHELTER SUMMARY' : 'SIĞINAK ÖZETİ', 40, 80); x.textAlign = 'right'; t('V.' + String(D.counters ? D.counters[0].d : '014').padStart(3, '0'), 28, 80, '700', '#463F34', W - 40); x.textAlign = 'left';
  x.fillStyle = '#D2483F'; x.fillRect(40, 96, W - 80, 4);
  t(en ? 'RATIONS LAST' : 'ERZAK YETER', 24, 150, '700', '#675F50'); t((D.shiftsLeft || '15 vardiya').replace('vardiya', en ? 'SHIFTS' : 'VARDİYA').toUpperCase(), 64, 222);
  t(`${S.erzak ?? 22}/30`, 34, 272, '700', '#463F34');
  x.setLineDash([8, 8]); x.strokeStyle = '#8E8166'; x.lineWidth = 2; x.beginPath(); x.moveTo(40, 300); x.lineTo(W - 40, 300); x.stroke(); x.setLineDash([]);
  const rows = [[en ? 'IDLE' : 'BOŞTA', S.idle ?? 4], [en ? 'RESERVE' : 'YEDEK', S.reserve ?? 0], [en ? 'PLATES' : 'PLAKA', `${S.plaka ?? 0}/3`], [en ? 'MATERIALS' : 'MALZEME', S.malzeme ?? 19]];
  rows.forEach(([k, v], i) => { t(k, 26, 360 + i * 56, '700', '#675F50'); x.textAlign = 'right'; t(String(v), 34, 360 + i * 56, '700', INK, W - 40); x.textAlign = 'left'; });
  x.setLineDash([8, 8]); x.beginPath(); x.moveTo(40, 600); x.lineTo(W - 40, 600); x.stroke(); x.setLineDash([]);
  let y = 660; for (const [k, r] of Object.entries(S.rooms || {})) { t(en ? EN_ROOM[k] : r.name, 30, y); x.textAlign = 'right'; t((en ? r.line2.replace('SV', 'LV') : r.line2), 26, y, '700', '#463F34', W - 40); x.textAlign = 'left'; y += 64; }
  return c; }

// ---------------------------------------------------------------- main
export function dress(I, ctx = {}) {
  const lang = ctx.lang || 'tr', en = lang === 'en', D = ctx.D || {}, S = D.state || {}, door = ctx.door || {};
  const STR = (T.STR && T.STR[lang]) || {}, S2 = X.S2[lang] || X.S2.tr;
  for (const n of DYN) for (const m of I[n] || []) if (m && m.holder) m.holder.visible = false;     // built below in local frames
  const made = (n, parent, pos, yaw = 0) => { const m = inst(parent.root, n, pos, yaw); if (m) (I['_' + n] ||= []).push(m); return m; };

  // ---- shelf: plate, crates, hope plant stage, lamp
  const shelf = first(I, 'Shelf'); put(shelf, 'TXT_Plate', STR.shelfPlate || (en ? 'SHELTER 07 · SECTION' : 'SIĞINAK 07 · KESİT'), { c: PLATE_INK });
  if (shelf) { const p = shelf.meta.lights.find(l => l.name === 'UnderShelfLight').pos; const s = new THREE.SpotLight(0xffd9a0, 1.6, 1.1, 55 * Math.PI / 180, 0.7, 2); addLight(shelf, s, p, [p[0], p[1] - 0.7, p[2] - 0.3]); }
  const crateWords = en ? ['RATIONS', 'MATERIALS', 'MATERIALS'] : ['ERZAK', 'MALZEME', 'MALZEME'];
  (I.Crate || []).forEach((m, i) => m && put(m, 'TXT_Stencil', crateWords[i] || crateWords[1], { f: 'st', w: 800, c: '#D9D2BF', ls: 0.06 }));
  const stage = ctx.stage === 'late' ? 3 : (ctx.sk === 'v001' ? 1 : 2);
  for (const k of [1, 2, 3]) for (const m of I['HopePlant' + k] || []) if (m && m.holder) m.holder.visible = k === stage;

  // ---- diorama: rooms (power, lamps, selection), cards, props, damage, slots + pegs, depot band
  const dio = first(I, 'Diorama');
  if (dio) {
    put(dio, 'TXT_Depo', en ? 'STORE' : 'DEPO', { c: PLATE_INK });
    for (const [k, [cx, fy]] of Object.entries(DIO.rooms)) {
      const r = (S.rooms || {})[k] || { name: k, line2: '', level: 0, staff: 0, cap: 0, powered: false };
      if (!r.powered) { swap(dio, 'Room_' + k, 'roomWall', 'roomWallOff'); swap(dio, 'Room_' + k, 'roomFloor', 'roomFloorOff'); }
      const lit = r.powered && r.level > 0; if (!lit) swap(dio, 'Lamp_' + k, 'bulbGlass', 'roomBulbOff');
      else addLight(dio, new THREE.PointLight(0xffd8a0, 0.06 * (ctx.dioLight ?? 2.4), 0.45, 2), [cx, fy + 0.14, 0.0]);
      vis(dio, 'Sel_' + k, ctx.selRoom === k);
      // card + holder
      const band = fy - 0.0325, hp = [cx, band - 0.0315, -0.16];
      const holder = made('RoomCardHolder', dio, hp); vis(holder, 'Sel', ctx.selRoom === k);
      const card = made('RoomCard', dio, [hp[0], hp[1] + 0.003, hp[2] - 0.0002]);
      if (card) {
        const [lv, st] = (r.line2 || '').split(' · ');
        put(card, 'TXT_Name', en ? EN_ROOM[k] : r.name);
        put(card, 'TXT_Level', ((en ? (lv || '').replace('SV', 'LV') : lv) || '') + ' ·', { c: '#2E2820' });
        const ic = slotOf(card, 'ICON_Person'); printPlane(card, [ic.center[0], ic.center[1], ic.center[2] - 0.0003], [0.0105, 0.0122], personIcon(), { transparent: true });
        put(card, 'TXT_Staff', st || '', { c: '#2E2820' });
        if (r.stamp) { const sl = slotOf(card, 'TXT_Stamp'); printPlane(card, [sl.center[0], sl.center[1], sl.center[2] - 0.0004], [0.08, 0.024], stampCanvas(en ? (EN_STAMP[r.stamp] || r.stamp) : r.stamp), { transparent: true, rot: -4 }); }
      }
      // room props + outputs
      const P = made('DioramaRoomProps_' + k, dio, [cx, fy + 0.004, 0]);
      if (P) {
        if (k === 'yasam') { vis(P, 'Output', r.staff > 0); vis(P, 'Reserve', (S.reserve ?? 0) > 0); }
        if (k === 'telsiz') { const on = r.staff > 0; if (!on) { swap(P, 'Dial', 'jewelAmberOn', 'jewelAmberOff'); swap(P, 'Listen', 'jewelAmberOn', 'jewelAmberOff'); }
          else addLight(P, new THREE.PointLight(0xffa850, 0.05 * (ctx.dioLight ?? 2.4), 0.25, 2), [-0.025, 0.08, 0.04]); vis(P, 'Headset', on); }
        if (k === 'atolye') { if (!((S.casting ?? 0) > 0)) swap(P, 'Mould', 'moltenGlow', 'castIron'); vis(P, 'Plates', (S.plaka ?? 0) > 0); }
        if (k === 'revir') vis(P, 'Patient', r.level > 0 && !!S.patient);
      }
      if (r.stamp === 'HASARLI' || (r.level === 0 && !r.powered)) made('DioramaDamage', dio, [cx, fy + 0.004, 0]);
      // staff slots + pegs
      const cap = r.cap || 0;
      for (let i = 0; i < cap; i++) { const x = cx - 0.13 + (cap > 1 ? i * (0.26 / (cap - 1)) : 0.13);
        made('PegSlot', dio, [x, fy + 0.004, -0.09]); if (i < (r.staff || 0)) made(PEGS[i % 6], dio, [x, fy + 0.0064, -0.09], (i * 47) % 30 - 15); }
    }
    // depot: 1 crate = 3 rations (10 slots), 1 bin = 5 materials (5 slots), plates 0–3, chalk slate
    const er = Math.min(30, S.erzak ?? 22), full = Math.floor(er / 3), part = er % 3;
    for (let i = 0; i < 10; i++) { const pos = [-0.335 + (i % 5) * 0.066, 0.510 + Math.floor(i / 5) * 0.044, 0.03];
      if (i < full) { const c = made('DepotCrate', dio, pos, (i * 37) % 7 - 3); ['Tin1', 'Tin2', 'Tin3'].forEach(t => vis(c, t, false)); }
      else if (i === full && part) { const c = made('DepotCrate', dio, pos, 2); vis(c, 'Lid', false); ['Tin1', 'Tin2', 'Tin3'].forEach((t, j) => vis(c, t, j < part)); } }
    const ma = S.malzeme ?? 19, bins = Math.min(5, Math.ceil(ma / 5));
    for (let i = 0; i < bins; i++) { const b = made('DepotBin', dio, [0.01 + i * 0.044, 0.510, 0.03]); const fill = (i === bins - 1 && ma < 25 && ma % 5) ? ma % 5 : 5; for (let j = 1; j <= 5; j++) vis(b, 'Fill' + j, j <= fill); }
    const pr = made('DepotPlate', dio, [0.31, 0.510, 0.04]); for (let j = 1; j <= 3; j++) vis(pr, 'Plate' + j, j <= (S.plaka ?? 0));
    const sl = made('DepotSlate', dio, [0.11, 0.522, 0.152]);
    put(sl, 'TXT_Line1', en ? 'RATIONS' : 'ERZAK', { c: '#E6E2D4' });
    put(sl, 'TXT_Line2', en ? (D.shiftsLeft || '15 vardiya').replace('vardiya', 'shifts') : (D.shiftsLeft || '15 vardiya'), { c: '#F2ECD8' });
    addLight(dio, new THREE.PointLight(0xffd8a0, 0.07 * (ctx.dioLight ?? 2.4), 0.6, 2), [0.0, 0.585, 0.0]);
  }
  // ---- BOŞTA tray + idle pegs, clipboard summary
  const tray = first(I, 'IdleTray');
  if (tray) { put(tray, 'TXT_Label', en ? 'IDLE' : 'BOŞTA', { c: PLATE_INK }); const n = Math.min(5, S.idle ?? 4); put(tray, 'TXT_Count', String(S.idle ?? 4), { c: PLATE_INK });
    for (let i = 0; i < n; i++) made(PEGS[i % 6], tray, [(i - (n - 1) / 2) * 0.05, 0.026, 0.008], (i * 53) % 24 - 12); }
  const clip = first(I, 'ClipboardStand');
  if (clip) { const t = slotOf(clip, 'TXT_Paper'); const c = t.center, nn = t.normal; printPlane(clip, [c[0] + nn[0] * 0.0004, c[1] + nn[1] * 0.0004, c[2] + nn[2] * 0.0004], t.size, clipSummary(D, ctx), { normal: nn, up: t.up, rough: 0.9 }); }

  // ---- CRT table group
  const crt = first(I, 'Crt');
  if (crt) { const st = X.crtScreen(D.crt || [['DİNLEMEDE', 150, '#a6e8b8', true], ['ALFA · HAZIR', 110], ['DOĞU AVR. / 02', 104], ['YZ ALARMI 4/10', 64, '#5e9c73']], { top: 190, reticle: false });
    const sm = new THREE.MeshStandardMaterial({ color: 0x050a07, emissive: 0xffffff, emissiveMap: st, emissiveIntensity: ctx.crtOff ? 0 : 1.0, roughness: 0.9, metalness: 0 });
    crt.root.traverse(o => { if (o.isMesh && o.userData.mat === 'crtScreen') { o.material = sm; } });
    put(crt, 'TXT_Plate', S2.crtPlate, { c: PLATE_INK }); }
  const radio = first(I, 'Radio');
  if (radio) {
    put(radio, 'TXT_Plate', S2.radio, { c: PLATE_INK }); put(radio, 'TXT_Channel', en ? 'CHANNEL' : 'KANAL', { c: '#E9DDBE' });
    put(radio, 'TXT_Volume', en ? 'VOLUME' : 'SES', { c: '#E9DDBE' }); put(radio, 'TXT_Tune', en ? 'TUNE' : 'AYAR', { c: '#E9DDBE' });
    const sig = slotOf(radio, 'TXT_Signal'); printPlane(radio, [0.045, 0.118, sig.center[2] + 0.0001], [0.08, 0.07], meterFace(), { rough: 0.6 });
    put(radio, 'TXT_Signal', en ? 'SIGNAL' : 'SİNYAL', { c: PLATE_INK, lift: 0.0002 });
    const dz = radio.meta.parts.find(p => p.name === 'DialPointer').pivotModel[2];
    printPlane(radio, [0.105, 0.208, dz + 0.0008], [0.2, 0.06], dialScale(), { emissive: '#ffffff', ei: 0.85, rough: 0.6 });
    radio.pose('DialPointer', -0.09 + (7 - 1) / 10 * 0.18); radio.pose('Needle', 18); radio.pose('KnobVolume', -40); radio.pose('KnobTune', 25);
  }
  const log = first(I, 'LogBook');
  if (log) { const head = en ? 'RADIO LOG · K-07' : 'TELSİZ KAYDI · K-07'; const E = ctx.LOG || X.LOG || [];
    printPlane(log, [-0.083, 0.0112, 0.0], [0.118, 0.2], logPage(head, E.slice(0, 14)), { normal: [0, 1, 0], up: [0, 0, 1], rough: 0.92 });
    printPlane(log, [0.083, 0.0112, 0.0], [0.118, 0.2], logPage(null, E.slice(14)), { normal: [0, 1, 0], up: [0, 0, 1], rough: 0.92 }); }

  // ---- door: leaf (open / wheel / peep), knock lamp, intercom LED, stencils, corridor lights, visitor
  const leaf = first(I, 'DoorLeaf');
  if (leaf) { leaf.pose('Hinge', door.open ? 95 : 0); leaf.pose('Wheel', door.open ? 450 : 0); leaf.pose('PeepCover', door.peepOpen ? 0.05 : 0);
    put(leaf, 'TXT_Stencil07', '07', { f: 'st', w: 800, c: '#C9C2AD', ls: 0.12 }); put(leaf, 'TXT_StencilOuter', en ? 'SHELTER 07 · KYGA' : 'SIĞINAK 07 · KYGA', { f: 'st', w: 800, c: '#C9C2AD', ls: 0.06 }); }
  const kl = first(I, 'KnockLamp');
  if (kl) { if (door.fatal) swap(kl, 'Lamp', 'jewelAmberOff', 'knockLampRed'); else if (door.knock) { swap(kl, 'Lamp', 'jewelAmberOff', 'jewelAmberOn');
    addLight(kl, new THREE.PointLight(0xffa53a, 1.2, 3.5, 2), [0, 0.04, -0.25]); } }
  const ic = first(I, 'IntercomBox'); if (ic) { if (!door.intercomOn) swap(ic, 'Led', 'jewelAmberOn', 'jewelAmberOff'); put(ic, 'TXT_Plate', en ? 'INTERCOM' : 'İNTERKOM', { c: PLATE_INK }); }
  const hz = first(I, 'HazardHeader'); if (hz) printPlane(hz, [0, 0.06, -0.0084], [1.094, 0.114], X.hazard(1024, 128), { rough: 0.6 });
  for (const m of I.CorridorLamp || []) if (m && !door.open) swap(m, 'Lamp', 'lampGlassCold', 'roomBulbOff');
  const cor = first(I, 'Corridor');
  if (cor && door.open && door.visitor) { const k = KINDS.includes(door.visitor) ? door.visitor : 'genel1'; const a = cor.meta.anchors.find(q => q.name === 'Visitor').pos; made('Visitor_' + k, cor, a, 0); }
  return { dressed: Object.keys(I).filter(k => k.startsWith('_')).map(k => `${k.slice(1)}×${I[k].length}`).join(' ') };
}
