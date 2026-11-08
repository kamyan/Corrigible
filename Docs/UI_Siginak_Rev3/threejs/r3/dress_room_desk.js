// Rev3 room dressing for the room-desk families (desk, walls) — owner: room-desk.
// dress(I, ctx): I = { ModelName: [instances] } placed by room3.js from meta.place (KeyKey / CorkCard / PencilNote carry `euler`);
// ctx = { lang, sk, D (DESK state merged over v014), stage 'mid'|'late', activeKey (Rev2 index 0–5, default 5 = MASA), teamPx?, THREE }.
// Words go into TMP slots via slotText (canvas stand-in); textures only for digit strips, marks, pictograms and paper.
import * as THREE from 'three';
import { slotText, matLike, canvas, lightWorld } from './scene3.js';
import { setUnityEuler } from './mlib.js';
import { COUNTER_MODULES, KEYS } from './models/desk.js';

export const PLACED = ['Desk', 'CounterRack', 'KeyRail', 'KeyKey', 'ShiftSwitch', 'Telex', 'TelexPaper', 'PaperTray', 'DeskLamp', 'Mug', 'Pencil', 'Eraser', 'Shavings',
  'StampRack', 'TeamFigure', 'CoffeeRing', 'TelexRoll', 'PaperPile',
  'CorkBoard', 'CorkCard', 'PencilNote', 'AlarmBoard', 'Poster', 'PipeRun', 'Duct', 'DuctEnd', 'JunctionBox', 'CeilingBulb', 'ChalkTally', 'StencilSign'];
const LATE = ['CoffeeRing', 'TelexRoll', 'PaperPile'];
const INK = '#1E1B16', PLATE_INK = '#2A1E10', CREAM = '#EFDCAE';
const REV2_KEY = { 1: 0, 2: 1, 3: 2, 5: 3 };            // Rev2 key index (6 keys) → Rev3 rail station (4 keys); door = 2
const REGION = { tr: { weu: 'BATI\nAVRUPA', eeu: 'DOĞU\nAVRUPA', me: 'ORTA\nDOĞU', af: 'AFRİKA', ru: 'RUSYA', in: 'HİNDİSTAN', cn: 'ÇİN', jk: 'JAPONYA\nKORE', sea: 'GÜNEYDOĞU\nASYA', oc: 'OKYANUSYA', na: 'KUZEY\nAMERİKA', sa: 'GÜNEY\nAMERİKA' },
  en: { weu: 'WESTERN\nEUROPE', eeu: 'EASTERN\nEUROPE', me: 'MIDDLE\nEAST', af: 'AFRICA', ru: 'RUSSIA', in: 'INDIA', cn: 'CHINA', jk: 'JAPAN\nKOREA', sea: 'SOUTHEAST\nASIA', oc: 'OCEANIA', na: 'NORTH\nAMERICA', sa: 'SOUTH\nAMERICA' } };

// ---------------------------------------------------------------- utilities
const all = (I, n) => (I[n] || []).filter(Boolean);
const first = (I, n) => all(I, n)[0] || null;
const vis = (m, part, on) => { const P = m && m.parts[part]; if (P) P.group.visible = !!on; };
const show = (m, on) => { if (m && m.holder) m.holder.visible = !!on; };
function eachMesh(m, key, fn, part) { if (!m) return; m.root.traverse(o => { if (o.isMesh && o.userData.mat === key && (!part || o.name.startsWith(part + ':'))) fn(o); }); }
function swap(m, part, from, to) { eachMesh(m, from, o => { o.material = matLike(to); o.userData.mat = to; }, part); }
function texOf(cv, o = {}) { const t = new THREE.CanvasTexture(cv); t.colorSpace = THREE.SRGBColorSpace; t.anisotropy = 8; if (o.repeat) { t.wrapS = t.wrapT = THREE.RepeatWrapping; t.repeat.set(...o.repeat); } return t; }
function printMat(cv, o = {}) { const t = texOf(cv, o); const m = new THREE.MeshStandardMaterial({ map: t, roughness: o.rough ?? 0.6, metalness: o.metal ?? 0, transparent: !!o.transparent, depthWrite: !o.transparent });
  if (o.ei) { m.emissive = new THREE.Color('#ffffff'); m.emissiveMap = t; m.emissiveIntensity = o.ei; } return m; }
function put(m, name, text, o = {}) {
  const t = m && m.meta.texts.find(q => q.name === name); if (!t || text == null || text === '') return null;
  const ppm = o.ppm || 4000, cap = o.capMm || t.capMm || t.size[1] * 700;
  const lines = String(text).split('\n').map(s => ({ t: s, size: cap * ppm / 1000 / 0.7, c: o.c || INK, f: o.f || 'cp', w: o.w || 700, ls: o.ls ?? 0.04, lh: o.lh }));
  return slotText(m, name, lines, { ppm, align: o.align || t.align, pad: o.pad ?? 0, emissive: o.emissive, ei: o.ei });
}
const L = (ctx, tr, en) => (ctx.lang === 'en' ? en : tr);

// ---------------------------------------------------------------- printed textures (no words)
function digitStrip(inv) { // 11 cells (0–9 + 0), cell 0 on top; matches gDrum uv (v = 1 → 1/11 around the drum)
  const [c, x] = canvas(128, 1408); x.fillStyle = inv ? '#E9DDBE' : '#1C150E'; x.fillRect(0, 0, 128, 1408);
  x.fillStyle = inv ? '#1C150E' : CREAM; x.font = '700 150px "Courier Prime"'; x.textAlign = 'center'; x.textBaseline = 'middle';
  for (let k = 0; k < 11; k++) x.fillText(String(k % 10), 64, k * 128 + 70);
  x.fillStyle = 'rgba(0,0,0,0.35)'; x.fillRect(0, 0, 5, 1408); x.fillRect(123, 0, 5, 1408); // rim shading
  return printMat(c, { rough: 0.5, ei: inv ? 0.05 : 0.14 });
}
function fractionMat(s) { const [c, x] = canvas(256, 232); x.fillStyle = '#0E0906'; x.fillRect(0, 0, 256, 232); x.fillStyle = '#C9A86A'; x.font = '700 120px "Courier Prime"'; x.textAlign = 'center'; x.textBaseline = 'middle'; x.fillText(s, 128, 124); return printMat(c, { ei: 0.1 }); }
function iconMat(k) {
  const [c, x] = canvas(256, 256); x.fillStyle = '#E9DDBE'; x.fillRect(0, 0, 256, 256); x.strokeStyle = INK; x.fillStyle = INK; x.lineWidth = 14; x.lineJoin = 'round';
  if (k === 0) { x.strokeRect(48, 60, 160, 136); x.beginPath(); x.moveTo(128, 60); x.lineTo(128, 196); x.moveTo(48, 128); x.lineTo(208, 128); x.stroke(); x.fillRect(116, 36, 24, 24); }
  else if (k === 1) { x.beginPath(); x.moveTo(70, 40); x.lineTo(160, 40); x.lineTo(196, 76); x.lineTo(196, 216); x.lineTo(70, 216); x.closePath(); x.stroke(); for (const y of [100, 136, 172]) { x.beginPath(); x.moveTo(96, y); x.lineTo(170, y); x.stroke(); } }
  else if (k === 2) { x.strokeRect(58, 36, 140, 188); x.beginPath(); x.arc(128, 124, 44, 0, 7); x.stroke(); for (let a = 0; a < 4; a++) { x.beginPath(); x.moveTo(128, 124); x.lineTo(128 + 44 * Math.cos(a * Math.PI / 2 + 0.785), 124 + 44 * Math.sin(a * Math.PI / 2 + 0.785)); x.stroke(); } }
  else { x.lineWidth = 16; x.beginPath(); x.moveTo(30, 206); x.lineTo(226, 206); x.moveTo(176, 206); x.lineTo(176, 120); x.lineTo(140, 104); x.stroke(); x.beginPath(); x.arc(110, 104, 54, Math.PI, 0); x.closePath(); x.fill(); x.fillRect(150, 190, 52, 16); }
  return printMat(c, { rough: 0.55 });
}
function ringStamp() { const [c, x] = canvas(256, 256); x.strokeStyle = '#1A4789'; x.globalAlpha = 0.9; x.lineWidth = 14; x.beginPath(); x.arc(128, 128, 112, 0, 7); x.stroke(); x.lineWidth = 6; x.beginPath(); x.arc(128, 128, 90, 0, 7); x.stroke(); x.fillStyle = '#1A4789'; x.fillRect(14, 104, 228, 40); return printMat(c, { transparent: true, rough: 0.8 }); }
function chalkMat(k) { const [c, x] = canvas(256, 240); x.strokeStyle = 'rgba(232,228,214,0.9)'; x.lineCap = 'round'; x.lineWidth = 13;
  for (let i = 0; i < Math.min(k, 4); i++) { const xx = 52 + i * 44; x.beginPath(); x.moveTo(xx + (i % 3) - 1, 30); x.lineTo(xx + 4 + (i % 2) * 3, 212); x.stroke(); }
  if (k >= 5) { x.beginPath(); x.moveTo(24, 186); x.lineTo(232, 58); x.stroke(); }
  return printMat(c, { transparent: true, rough: 1 }); }
function posterMat() { const W = 440, H = 600; const [c, x] = canvas(W, H); x.fillStyle = '#ead9c0'; x.fillRect(0, 0, W, H); x.fillStyle = '#9a1e14'; x.fillRect(18, 18, W - 36, H - 36);
  x.strokeStyle = '#efe2cf'; x.lineWidth = 13; x.lineCap = 'round'; x.beginPath(); x.moveTo(85, 150); x.bezierCurveTo(130, 150, 150, 190, 180, 190); x.stroke(); x.fillStyle = '#efe2cf'; x.fillRect(175, 170, 45, 40); x.fillRect(220, 176, 20, 7); x.fillRect(220, 197, 20, 7);
  x.beginPath(); x.moveTo(355, 150); x.bezierCurveTo(310, 150, 290, 190, 270, 190); x.stroke(); x.fillRect(260, 170, 15, 40); x.lineWidth = 7; x.beginPath(); x.moveTo(235, 125); x.lineTo(250, 150); x.moveTo(250, 235); x.lineTo(235, 260); x.stroke();
  x.fillRect(60, 440, W - 120, 4); for (let i = 0; i < 2500; i++) { x.fillStyle = `rgba(${Math.random() < 0.5 ? '0,0,0' : '255,240,220'},${Math.random() * 0.06})`; x.fillRect(Math.random() * W, Math.random() * H, 2, 2); }
  return printMat(c, { rough: 0.7 }); }
function sheetMat() { const [c, x] = canvas(512, 384); x.fillStyle = '#efe4bc'; x.fillRect(0, 0, 512, 384); x.fillStyle = 'rgba(70,60,40,0.3)'; for (let y = 14; y < 384; y += 28) { x.beginPath(); x.arc(14, y, 5, 0, 7); x.fill(); x.beginPath(); x.arc(498, y, 5, 0, 7); x.fill(); }
  x.strokeStyle = 'rgba(110,90,51,0.4)'; x.setLineDash([8, 6]); x.lineWidth = 2; x.beginPath(); x.moveTo(30, 0); x.lineTo(30, 384); x.moveTo(482, 0); x.lineTo(482, 384); x.stroke(); return printMat(c, { rough: 0.92 }); }
function ringDecal() { const [c, x] = canvas(256, 256); x.strokeStyle = 'rgba(74,46,26,0.55)'; x.lineWidth = 9; x.beginPath(); x.arc(128, 128, 92, 0.3, 6.0); x.stroke(); x.lineWidth = 3; x.beginPath(); x.arc(131, 126, 84, 0, 7); x.stroke(); return printMat(c, { transparent: true, rough: 0.6 }); }
function dripsMat() { const [c, x] = canvas(1024, 184); x.clearRect(0, 0, 1024, 184); for (let i = 0; i < 9; i++) { const xx = 120 + i * 95 + Math.random() * 40, h = 12 + Math.random() * 26; const g = x.createLinearGradient(0, 130, 0, 130 + h); g.addColorStop(0, 'rgba(217,210,191,0.32)'); g.addColorStop(1, 'rgba(217,210,191,0)'); x.fillStyle = g; x.fillRect(xx, 130, 2.5, h); }
  return printMat(c, { transparent: true, rough: 0.85 }); }

// ---------------------------------------------------------------- dress
export function dress(I, ctx = {}) {
  const D = ctx.D || {}; const late = ctx.stage === 'late';
  for (const n of LATE) all(I, n).forEach(m => show(m, late));
  all(I, 'Mug').forEach((m, i) => show(m, i === 0 || late));
  all(I, 'PaperTray').forEach(m => { vis(m, 'StackLate', late); put(m, 'TXT_Label', L(ctx, 'EVRAK', 'PAPERS'), { c: INK }); });

  // counter rack: wheels posed to D.counters, labels, fixed fractions
  const cr = first(I, 'CounterRack');
  if (cr) {
    const dm = digitStrip(false), tm = digitStrip(true);
    eachMesh(cr, 'counterDrum', o => { o.material = dm; }); eachMesh(cr, 'counterTenths', o => { o.material = tm; });
    const vals = D.counters || [];
    COUNTER_MODULES.forEach((md, mi) => {
      const v = vals[mi] || { d: '0' }; const ds = String(v.d).padStart(md.n, '0').slice(-md.n);
      for (let i = 0; i < md.n; i++) cr.pose(`Wheel_${mi}_${i}`, +ds[i] * 36);
      if (md.tenth) cr.pose(`Wheel_${mi}_${md.n}`, +(v.frac ?? 0) * 36);
      put(cr, `TXT_Label_${mi}`, L(ctx, md.tr, md.en), { c: PLATE_INK });
    });
    const fr = COUNTER_MODULES.map((md, mi) => ({ x: -0.788 + 0.26267 * (mi + 0.5), f: md.frac })).filter(q => q.f);
    eachMesh(cr, 'counterFraction', o => { if (o.geometry.attributes.position.count !== 6) return; const ux = o.position.x; const q = fr.reduce((a, b) => (Math.abs(b.x - ux) < Math.abs(a.x - ux) ? b : a)); o.material = fractionMat(q.f); });
  }
  // key rail + keys
  const rail = first(I, 'KeyRail'); if (rail) KEYS.forEach((k, i) => put(rail, `TXT_Key_${i}`, L(ctx, k.tr, k.en), { c: PLATE_INK }));
  const act = REV2_KEY[ctx.activeKey ?? 5];
  all(I, 'KeyKey').forEach((m, i) => {
    eachMesh(m, 'keyIcon', o => { o.material = iconMat(i); });
    const on = i === act || (i === 2 && D.doorWaiting);
    if (on) swap(m, 'Jewel', 'jewelAmberOff', 'jewelAmberOn');
    m.pose('Cap', i === act ? -0.006 : 0);
    const tag = i === 2 && !!D.doorWaiting; vis(m, 'Tag', tag);
    if (tag) { m.pose('Tag', 6); put(m, 'TXT_Tag', L(ctx, 'kapıda biri\nbekliyor', 'someone at\nthe door'), { f: 'ka', c: '#1A2F5A', w: 700, ls: 0, lh: 1.05 }); }
  });
  // shift switch: plate, lock band hidden, lever up (shift running), ready lamp on
  const sw = first(I, 'ShiftSwitch');
  if (sw) { if (D.plate2) { const t = sw.meta.texts.find(q => q.name === 'TXT_Plate'); const ppm = 4000; // two lines: title + state (supplement: "kapağı kaldır" / "fişi EVRAK'a koy")
      slotText(sw, 'TXT_Plate', [{ t: L(ctx, 'VARDİYAYI BİTİR', 'END SHIFT'), size: 11.5 * ppm / 1000 / 0.7, c: PLATE_INK, f: 'cp', w: 700, ls: 0.04 }, { t: D.plate2, size: 6.4 * ppm / 1000 / 0.7, c: D.plate2Red ? '#7A1810' : '#4A3A22', f: 'cp', w: 700, ls: 0.02 }], { ppm, align: 'center', pad: 0 }); }
    else put(sw, 'TXT_Plate', L(ctx, 'VARDİYAYI BİTİR', 'END SHIFT'), { c: PLATE_INK }); vis(sw, 'LockBand', !!D.locked); sw.pose('Lever', 35);
    put(sw, 'TXT_Cover', L(ctx, 'VARDİYA', 'SHIFT'), { f: 'st', c: '#EFE2CF', w: 800, ls: 0.08 }); if (D.switchLamp === false) swap(sw, 'ReadyLamp', 'readyLamp', 'readyLampOff'); }
  // telex + paper
  const tx = first(I, 'Telex'); if (tx) put(tx, 'TXT_Plate', L(ctx, 'TELEKS', 'TELEX'), { c: PLATE_INK });
  const tp = first(I, 'TelexPaper');
  if (tp) {
    const sm = sheetMat(); eachMesh(tp, 'telexPaper', o => { if (o.geometry.attributes.position.count <= 72) o.material = sm; });
    const h = (D.telexHang || [])[0];
    if (h) { put(tp, 'TXT_Code', h[0], { c: '#463F34' }); put(tp, 'TXT_Status', h[1].t, { c: h[1].red ? '#9A1E14' : INK }); put(tp, 'TXT_Message', h[2], { w: 400, c: INK, ls: 0, align: 'left' }); }
    const p = (D.telexHang || [])[1]; if (p) put(tp, 'TXT_Prev', `${p[0]}  ${p[1].t}`, { c: '#5a5246' });
  }
  const tf = first(I, 'TeamFigure');
  if (tf) { put(tf, 'TXT_Letter', 'A', { c: CREAM });
    if (ctx.teamPx) { const [px, py] = ctx.teamPx; tf.holder.position.set(-0.60 + px / 1920 * 1.20, 0.7612, -(1.655 - py / 1080 * 0.675)); } }
  all(I, 'CoffeeRing').forEach(m => eachMesh(m, 'coffeeRingDecal', o => { o.material = ringDecal(); }));

  // cork board: header count, one card per freed region, pencil note on the next slot
  const freed = D.freed || [];
  const cb = first(I, 'CorkBoard');
  if (cb) { put(cb, 'TXT_Title', L(ctx, 'KURTARILAN BÖLGELER', 'FREED REGIONS')); put(cb, 'TXT_Count', String(freed.length).padStart(2, '0')); put(cb, 'TXT_Of', '/12', { c: '#675F50' });
    for (let i = 0; i < 12; i++) vis(cb, `SlotPin_${String(i).padStart(2, '0')}`, !(i < freed.length || (i === freed.length && D.nextTarget))); }
  const cards = all(I, 'CorkCard'); const rs = ringStamp();
  cards.forEach((m, i) => { const f = freed[i]; show(m, !!f); if (!f) return;
    eachMesh(m, 'stampBlue', o => { o.material = rs; });
    put(m, 'TXT_Region', (REGION[ctx.lang === 'en' ? 'en' : 'tr'][f[0]] || f[0]), { lh: 1.0 }); put(m, 'TXT_Stamp', L(ctx, 'ÖZGÜR', 'FREE'), { c: '#F6F0DE' }); put(m, 'TXT_Shift', f[1], { c: '#1A4789' }); });
  const pn = first(I, 'PencilNote');
  if (pn) { const k = freed.length; show(pn, !!D.nextTarget && k < 12);
    if (cards[k] && cards[k].holder) { pn.holder.position.copy(cards[k].holder.position); pn.holder.quaternion.copy(cards[k].holder.quaternion); }
    put(pn, 'TXT_Note', D.nextTarget, { f: 'ka', c: '#3A3A3A', w: 700, ls: 0, align: 'left' }); }

  // AI alarm board
  const ab = first(I, 'AlarmBoard');
  if (ab) {
    const a = Math.max(0, Math.min(10, D.alarm ?? 0)), dm = digitStrip(false), ds = String(a).padStart(2, '0');
    eachMesh(ab, 'counterDrum', o => { o.material = dm; }); ab.pose('Wheel0', +ds[0] * 36); ab.pose('Wheel1', +ds[1] * 36);
    eachMesh(ab, 'alarmFraction', o => { o.material = fractionMat('/10'); });
    for (let i = 0; i < 10; i++) swap(ab, `Step_${i}`, 'alarmStepOff', i < a ? (i >= 7 ? 'alarmStepRed' : 'alarmStepOn') : 'alarmStepOff');
    if (a >= 8) { swap(ab, 'Lamp', 'alarmLampOff', 'alarmLampOn'); const p = lightWorld(ab, 'AlarmGlow'); if (p) { const l = new THREE.PointLight(0xf0443a, 0.5, 0.9, 2); l.position.copy(p); ab.holder.parent.add(l); } }
    put(ab, 'TXT_Title', L(ctx, 'YZ ALARMI', 'AI ALARM'), { c: PLATE_INK }); put(ab, 'TXT_Note', L(ctx, '10\'DA KARŞI SALDIRI', 'COUNTER-ATTACK AT 10'), { c: '#C9A86A' });
  }
  // chalk tally (only when the state has one)
  const ct = first(I, 'ChalkTally');
  if (ct) { const n = D.tally || 0; show(ct, n > 0);
    if (n > 0) { const full = Math.floor(n / 5), rem = n % 5; const mats = [1, 2, 3, 4, 5].map(chalkMat);
      for (let i = 0; i < 16; i++) { const id = `ChalkMark_${String(i).padStart(2, '0')}`; const k = i < full ? 5 : i === full ? rem : 0; vis(ct, id, k > 0); if (k) eachMesh(ct, 'chalkTally', o => { o.material = mats[k - 1]; }, id); }
      put(ct, 'TXT_Label', L(ctx, 'vardiya', 'shifts'), { f: 'ka', c: 'rgba(230,226,212,0.85)', w: 700, ls: 0, align: 'left' }); } }
  // poster + stencil sign
  const po = first(I, 'Poster');
  if (po) { eachMesh(po, 'posterPrint', o => { o.material = posterMat(); });
    const pc = '#EFE2CF'; put(po, 'TXT_L1', L(ctx, 'AĞA', "DON'T"), { c: pc }); put(po, 'TXT_L2', L(ctx, 'BAĞLANMA', 'CONNECT'), { c: pc }); put(po, 'TXT_L3', L(ctx, 'ANALOG KAL', 'STAY ANALOG'), { c: pc }); put(po, 'TXT_L4', L(ctx, 'KYGA · DOKTRİN 1', 'KYGA · DOCTRINE 1'), { c: pc }); }
  const ss = first(I, 'StencilSign');
  if (ss) { eachMesh(ss, 'stencilDrips', o => { o.material = dripsMat(); }); put(ss, 'TXT_Stencil', L(ctx, 'KYGA / SIĞINAK 07', 'KYGA / SHELTER 07'), { f: 'st', c: 'rgba(217,210,191,0.88)', w: 800, ls: 0.08 }); }
  return { placed: PLACED.filter(n => all(I, n).length), late };
}
