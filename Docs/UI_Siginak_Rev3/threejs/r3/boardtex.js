// Card faces and board prints for the planning board (supplement 2026-11-05: R15 board, R16 card family, R18 production).
// Every function draws on a canvas at `ppm` pixels per millimetre (default 8) and returns the canvas. Browser only.
// Sizes (mm): person 80×120 · item 80×50 · raw material 80×56 · material chip 56×40 · recipe 112×80. One thickness (0,6 mm).
// K4 readability at 1,52 px/mm: name 12,5 mm → 19 px; values, labels, item names ≥ 9,5 mm → ≥ 14 px.
import { PORTRAITS, portraitDataURL, CARD_CROP } from './portraits.js';

export const INK = '#1E1B16', PAPER = '#EFE6D0', PAPER2 = '#E6DCC0', RED = '#9A1E14', RULE = '#B9A98A', BLUE = '#1A2F5A', MUTE = '#5F574A';
export const SIZE = { person: [80, 120], item: [80, 50], material: [80, 56], chip: [56, 40], recipe: [112, 80] };
const FONT = { sans: '"Libre Franklin"', mono: '"Courier Prime"', hand: '"Kalam"', sten: '"Big Shoulders Stencil Display"' };

export const L = {
  tr: { can: 'CAN', har: 'HAR', guc: 'GÜÇ', duty: 'GÖREV', train: 'EĞİTİM', rest: 'DİNLENME', restGain: '+1 CAN', bed: 'YATIYOR', restricted: 'KISITLI', lost: 'KAYIP', hurt: 'YARALI', review: 'İNCELE', sh: 'V.',
    skills: { T: 'T', E: 'E', A: 'A', U: 'Ü', M: 'M' }, skillNames: { T: 'Teknisyen', E: 'EMP', A: 'Analiz', U: 'Üretim', M: 'Mutfak' },
    slot: { kit: 'KİT', armor: 'ZIRH', aux: 'YARDIMCI' }, free: 'serbest', stock: 'stok', res: 'ayrı', unit: 'ADET', shifts: (n) => `${n} vardiya`, out: 'ÇIKTI', inp: 'GİRDİ', one: '1 ürün',
    jobs: { tek: 'TEKNİSYEN', emp: 'EMP UZMANI', ana: 'ANALİST', uret: 'ÜRETİM', mutf: 'MUTFAK', egit: 'EĞİTİM', bosta: 'BOŞTA' } },
  en: { can: 'HP', har: 'MOVE', guc: 'POWER', duty: 'DUTY', train: 'TRAINING', rest: 'REST', restGain: '+1 HP', bed: 'BED REST', restricted: 'RESTRICTED', lost: 'LOST', hurt: 'INJURED', review: 'REVIEW', sh: 'SH.',
    skills: { T: 'T', E: 'E', A: 'A', U: 'P', M: 'K' }, skillNames: { T: 'Technician', E: 'EMP', A: 'Analysis', U: 'Production', M: 'Kitchen' },
    slot: { kit: 'KIT', armor: 'ARMOUR', aux: 'AID' }, free: 'free', stock: 'stock', res: 'held', unit: 'UNIT', shifts: (n) => `${n} shift${n > 1 ? 's' : ''}`, out: 'MAKES', inp: 'NEEDS', one: '1 item',
    jobs: { tek: 'TECHNICIAN', emp: 'EMP SPECIALIST', ana: 'ANALYST', uret: 'PRODUCTION', mutf: 'KITCHEN', egit: 'TRAINING', bosta: 'IDLE' } },
};

// ---------------------------------------------------------------- canvas helpers
export function mk(wmm, hmm, ppm = 8) { const c = document.createElement('canvas'); c.width = Math.round(wmm * ppm); c.height = Math.round(hmm * ppm); const x = c.getContext('2d'); x.scale(ppm, ppm); x.textBaseline = 'alphabetic'; return [c, x]; }
const font = (x, w, mm, fam = 'sans') => { x.font = `${w} ${mm}px ${FONT[fam]}`; };
function fit(x, text, maxW, w, mm, fam = 'sans', min = 0.7) { let s = mm; font(x, w, s, fam); while (x.measureText(text).width > maxW && s > mm * min) { s -= mm * 0.03; font(x, w, s, fam); } return s; }
function txt(x, t, X, Y, o = {}) { font(x, o.w || 700, o.mm || 5, o.f || 'sans'); x.fillStyle = o.c || INK; x.textAlign = o.align || 'left'; if (o.max) fit(x, t, o.max, o.w || 700, o.mm || 5, o.f || 'sans', o.min);
  if (o.ls) { x.letterSpacing = o.ls + 'px'; } x.fillText(t, X, Y); if (o.ls) x.letterSpacing = '0px'; }
function paperGrain(x, w, h, base = PAPER, seed = 7) { x.fillStyle = base; x.fillRect(0, 0, w, h); let s = seed; const r = () => (s = (s * 16807) % 2147483647) / 2147483647;
  x.globalAlpha = 0.05; for (let i = 0; i < w * h * 0.35; i++) { x.fillStyle = r() > 0.5 ? '#000' : '#fff'; x.fillRect(r() * w, r() * h, 0.35, 0.35); } x.globalAlpha = 1; }
function rule(x, x0, y, x1, c = RULE, lw = 0.3, dash) { x.strokeStyle = c; x.lineWidth = lw; if (dash) x.setLineDash(dash); x.beginPath(); x.moveTo(x0, y); x.lineTo(x1, y); x.stroke(); x.setLineDash([]); }
function box(x, X, Y, W, H, c = INK, lw = 0.35, dash) { x.strokeStyle = c; x.lineWidth = lw; if (dash) x.setLineDash(dash); x.strokeRect(X, Y, W, H); x.setLineDash([]); }
const imgCache = new Map();
async function loadImg(src) { if (imgCache.has(src)) return imgCache.get(src); const im = new Image(); im.src = src; await im.decode(); imgCache.set(src, im); return im; }
export async function portraitImg(pid) { const spec = PORTRAITS[pid]; if (!spec) return null; return loadImg(portraitDataURL(spec, { id: pid, crop: CARD_CROP, frame: false, w: 456, h: 500 })); }

// ---------------------------------------------------------------- pictograms (drawn in a 10 × 10 mm cell at X, Y)
export function icon(x, kind, X, Y, s = 10, c = INK) {
  x.save(); x.translate(X, Y); x.scale(s / 10, s / 10); x.strokeStyle = c; x.fillStyle = c; x.lineWidth = 0.9; x.lineJoin = 'round'; x.lineCap = 'round';
  const P = (pts, close = true, fill = false) => { x.beginPath(); pts.forEach(([a, b], i) => i ? x.lineTo(a, b) : x.moveTo(a, b)); if (close) x.closePath(); fill ? x.fill() : x.stroke(); };
  switch (kind) {
    case 'metal': { const h = []; for (let i = 0; i < 6; i++) { const a = Math.PI / 6 + i * Math.PI / 3; h.push([5 + 4.3 * Math.cos(a), 5 + 4.3 * Math.sin(a)]); } P(h); x.beginPath(); x.arc(5, 5, 1.8, 0, 7); x.stroke(); break; }
    case 'devre': { x.strokeRect(2.6, 2.6, 4.8, 4.8); for (let i = 0; i < 3; i++) { const t = 3.6 + i * 1.4; P([[t, 2.6], [t, 0.8]], false); P([[t, 7.4], [t, 9.2]], false); P([[2.6, t], [0.8, t]], false); P([[7.4, t], [9.2, t]], false); } x.fillRect(4.2, 4.2, 1.6, 1.6); break; }
    case 'kumas': { P([[1, 3], [7.5, 1.5], [9, 6.5], [2.5, 8.5]]); x.setLineDash([0.8, 0.7]); P([[2.2, 3.8], [7.2, 2.6], [8.3, 6.2], [3, 7.5]]); x.setLineDash([]); break; }
    case 'kimyasal': { P([[3.6, 1], [6.4, 1], [6.4, 3.6], [8.8, 8.2], [8.2, 9.2], [1.8, 9.2], [1.2, 8.2], [3.6, 3.6]]); P([[2.3, 6.6], [7.7, 6.6], [8.6, 8.4], [1.4, 8.4]], true, true); break; }
    case 'kit_tek': { P([[2, 8.4], [6.2, 4.2]], false); x.beginPath(); x.arc(7.2, 3, 2, 0.6, 5.2); x.stroke(); P([[8.2, 8.6], [3.4, 3.8]], false); P([[3.4, 3.8], [2.4, 1.6], [1.6, 2.4]], true, true); break; }
    case 'kit_emp': { x.beginPath(); x.arc(5, 5, 4.2, 0, 7); x.stroke(); P([[5.6, 1.6], [3.2, 5.4], [5, 5.4], [4.2, 8.4], [6.9, 4.4], [5.1, 4.4]], true, true); break; }
    case 'kit_ana': { for (let i = 1; i < 4; i++) { P([[1, i * 2.2 + 0.5], [6, i * 2.2 + 0.5]], false); } x.beginPath(); x.arc(6.2, 5.6, 2.3, 0, 7); x.stroke(); P([[7.8, 7.3], [9.4, 9.2]], false); break; }
    case 'zirh': { P([[5, 0.9], [8.8, 2.3], [8.4, 6], [5, 9.2], [1.6, 6], [1.2, 2.3]]); P([[5, 2.6], [5, 7.6]], false); P([[2.8, 4.4], [7.2, 4.4]], false); break; }
    case 'bakim': { x.strokeRect(1.2, 2.6, 7.6, 6); P([[3.6, 2.6], [3.6, 1.4], [6.4, 1.4], [6.4, 2.6]], false); x.fillRect(4.4, 3.8, 1.2, 3.6); x.fillRect(3.2, 5, 3.6, 1.2); break; }
    case 'emp_hucre': { x.strokeRect(2.4, 2, 5.2, 7.4); x.fillRect(4, 0.9, 2, 1.1); P([[5.6, 3.2], [3.9, 5.9], [5.2, 5.9], [4.4, 8.3], [6.3, 5.2], [5, 5.2]], true, true); break; }
    case 'person': { x.beginPath(); x.arc(5, 3.2, 2.2, 0, 7); x.fill(); P([[1.4, 9.6], [1.6, 7.2], [3, 5.8], [7, 5.8], [8.4, 7.2], [8.6, 9.6]], true, true); break; }
    case 'book': { P([[1, 2], [5, 3], [9, 2], [9, 8.6], [5, 9.6], [1, 8.6]]); P([[5, 3], [5, 9.6]], false); break; }
    case 'lock': { x.strokeRect(2.2, 4.6, 5.6, 4.6); x.beginPath(); x.arc(5, 4.6, 2, Math.PI, 0); x.stroke(); x.fillRect(4.5, 6.2, 1, 1.6); break; }
    case 'scrap': { P([[1.4, 3], [8.6, 3], [7.8, 9.4], [2.2, 9.4]]); P([[1, 3], [9, 3]], false); P([[3.6, 1.6], [6.4, 1.6]], false); P([[3.8, 4.6], [4.1, 8.2]], false); P([[6.2, 4.6], [5.9, 8.2]], false); break; }
  }
  x.restore();
}
export const ITEM_ICON = { kit_tek: 'kit_tek', kit_emp: 'kit_emp', kit_ana: 'kit_ana', zirh: 'zirh', bakim: 'bakim', emp_hucre: 'emp_hucre' };

// slot band pattern (reads in greyscale): kit = solid, armour = cross-hatch, aid = dots
function slotBand(x, slot, X, Y, W, H) {
  x.save(); x.beginPath(); x.rect(X, Y, W, H); x.clip();
  if (slot === 'kit') { x.fillStyle = '#4A5140'; x.fillRect(X, Y, W, H); }
  else if (slot === 'armor') { x.fillStyle = '#7D8079'; x.fillRect(X, Y, W, H); x.strokeStyle = '#3E413C'; x.lineWidth = 0.35; for (let i = -H; i < W; i += 1.6) { x.beginPath(); x.moveTo(X + i, Y + H); x.lineTo(X + i + H, Y); x.stroke(); x.beginPath(); x.moveTo(X + i, Y); x.lineTo(X + i + H, Y + H); x.stroke(); } }
  else { x.fillStyle = '#B08F4E'; x.fillRect(X, Y, W, H); x.fillStyle = '#5E4826'; for (let yy = Y + 0.9; yy < Y + H; yy += 1.6) for (let xx = X + 0.9 + ((yy - Y) % 3.2 > 1.5 ? 0.8 : 0); xx < X + W; xx += 1.6) { x.beginPath(); x.arc(xx, yy, 0.32, 0, 7); x.fill(); } }
  x.restore(); }

// ---------------------------------------------------------------- person card (80 × 120)
// p: community person (+ computed can/har/guc), st: { job, topic, hurt, bed, restricted, lost:{shift,where}, review }, lang
export async function personCard(p, st = {}, o = {}) {
  const lang = o.lang || 'tr', T = L[lang], ppm = o.ppm || 8; const [W, H] = SIZE.person; const [c, x] = mk(W, H, ppm);
  paperGrain(x, W, H, PAPER, (p.name.charCodeAt(0) * 31) % 997 + 3);
  box(x, 1.6, 1.6, W - 3.2, H - 3.2, INK, 0.45); box(x, 2.5, 2.5, W - 5, H - 5, INK, 0.18);
  // name and job
  txt(x, p.name, 4.6, 14.6, { mm: 14, w: 700, max: 58 });
  rule(x, 5, 17.2, W - 5, RED, 0.6);
  const job = lang === 'en' ? p.job_en : p.job_tr; txt(x, job, 4.8, 26.2, { mm: 9.5, w: 600, max: W - 9.6, min: 0.74 });
  // portrait
  const px = 5, py = 29, pw = 44, ph = 48.2;
  x.fillStyle = PAPER2; x.fillRect(px, py, pw, ph);
  const im = await portraitImg(p.portrait); if (im) x.drawImage(im, px, py, pw, ph);
  box(x, px, py, pw, ph, INK, 0.5);
  // masteries (right column): letter + 3 pips per tier
  const xp = p.xp || {}; const tier = (v) => v >= 12 ? 3 : v >= 6 ? 2 : v >= 2 ? 1 : 0;
  ['T', 'E', 'A', 'U', 'M'].forEach((k, i) => { const y = py + 7.6 + i * 9.6; const t = tier((xp[k] || 0) + (st.xpAdd && st.xpAdd[k] || 0));
    txt(x, T.skills[k], 52.5, y, { mm: 9.5, w: 700, f: 'mono', c: t ? INK : '#8C826E' });
    for (let j = 0; j < 3; j++) { x.beginPath(); x.arc(61.4 + j * 4.6, y - 3.0, 1.9, 0, 7); if (j < t) { x.fillStyle = INK; x.fill(); } else { x.strokeStyle = '#8C826E'; x.lineWidth = 0.35; x.stroke(); } } });
  // stats: CAN (cur/max) · HAR · GÜÇ
  const sy = 80; rule(x, 5, sy, W - 5, RULE, 0.3);
  const cells = [[T.can, st.hurt != null ? `${p.can - st.hurt}/${p.can}` : `${p.can}/${p.can}`, st.hurt != null], [T.har, String(p.har)], [T.guc, String(p.guc)]];
  cells.forEach(([lab, v, red], i) => { const cx0 = 5 + i * 23.4; txt(x, lab, cx0 + 11.2, sy + 9, { mm: lang === 'en' && lab.length > 3 ? 7.6 : 9.5, w: 700, f: 'mono', align: 'center', c: MUTE });
    txt(x, v, cx0 + 11.2, sy + 20.6, { mm: 11.5, w: 700, f: 'mono', align: 'center', c: red ? RED : INK }); if (i) { x.strokeStyle = RULE; x.lineWidth = 0.3; x.beginPath(); x.moveTo(cx0, sy + 2); x.lineTo(cx0, sy + 22); x.stroke(); } });
  // duty / state strip
  const by = 104.5; x.fillStyle = 'rgba(30,27,22,0.06)'; x.fillRect(3.2, by, W - 6.4, 12.4); rule(x, 3.2, by, W - 3.2, INK, 0.35);
  let strip = '', sc = INK;
  if (st.lost) { strip = `${T.lost} · ${T.sh}${String(st.lost.shift).padStart(3, '0')}`; sc = INK; }
  else if (st.bed) { strip = `${T.bed} · ${st.bed} ${T.sh}`; sc = BLUE; }
  else if (st.restricted) { strip = `${T.restricted} · ${st.restricted} ${T.sh}`; sc = RED; }
  else if (st.job === 'egit') { strip = `${T.train} · ${T.skillNames[st.topic || 'T']} +2`; }
  else if (st.job === 'bosta' || !st.job) { strip = `${T.rest} · ${T.restGain}`; sc = MUTE; }
  else strip = T.jobs[st.job];
  txt(x, strip, W / 2, by + 9.2, { mm: 9.2, w: 700, f: 'sans', align: 'center', c: sc, max: W - 8, min: 0.7 });
  // ---- state overlays
  if (st.hurt != null && !st.lost) { // translucent tape across the portrait's top-left corner + YARALI
    x.save(); x.translate(px + 7, py + 7); x.rotate(-0.62); x.fillStyle = 'rgba(214,201,160,0.72)'; x.fillRect(-15, -4.2, 30, 8.4); x.strokeStyle = 'rgba(120,104,70,0.6)'; x.lineWidth = 0.25; x.setLineDash([0.9, 0.6]); x.strokeRect(-15, -4.2, 30, 8.4); x.setLineDash([]); x.restore();
    txt(x, T.hurt, px + pw - 1.5, py + ph - 2.2, { mm: 6.4, w: 700, f: 'mono', align: 'right', c: RED }); }
  if (st.bed) stamp(x, T.bed, px + pw / 2, py + ph / 2, -0.26, BLUE, 8.6);
  if (st.restricted) { x.save(); x.beginPath(); x.moveTo(W - 26, 1.6); x.lineTo(W - 1.6, 1.6); x.lineTo(W - 1.6, 26); x.closePath(); x.clip();
    x.fillStyle = RED; x.fillRect(W - 27, 0, 27, 27); x.restore(); x.save(); x.translate(W - 8.6, 8.6); x.rotate(Math.PI / 4); txt(x, T.restricted, 0, 1.8, { mm: 4.6, w: 700, f: 'mono', align: 'center', c: '#F2E9CC' }); x.restore(); }
  if (st.lost) { x.save(); x.globalCompositeOperation = 'multiply'; x.fillStyle = 'rgba(70,64,54,0.55)'; x.fillRect(px, py, pw, ph); x.restore();
    x.save(); x.beginPath(); x.moveTo(W - 30, 1.6); x.lineTo(W - 1.6, 1.6); x.lineTo(W - 1.6, 30); x.closePath(); x.fillStyle = '#121110'; x.fill(); x.restore();   // mourning corner (top right)
    x.save(); x.translate(W - 9.6, 9.6); x.rotate(Math.PI / 4); txt(x, T.lost, 0, 1.8, { mm: 4.6, w: 700, f: 'mono', align: 'center', c: '#D8D0BC' }); x.restore(); }
  return c;
}
function stamp(x, word, X, Y, rot, col, mm) { x.save(); x.translate(X, Y); x.rotate(rot); x.globalAlpha = 0.86; font(x, 700, mm, 'mono'); const w = x.measureText(word).width + 5;
  x.strokeStyle = col; x.lineWidth = 0.9; x.strokeRect(-w / 2, -mm * 0.78, w, mm * 1.32); x.lineWidth = 0.35; x.strokeRect(-w / 2 + 1, -mm * 0.78 + 1, w - 2, mm * 1.32 - 2);
  x.fillStyle = col; x.textAlign = 'center'; x.fillText(word, 0, mm * 0.32); x.restore(); }

// inspection flag (red paper tab sticking up from the card top; separate quad in 3D) — 18 × 12 mm
export function reviewFlag(o = {}) { const T = L[o.lang || 'tr']; const [c, x] = mk(18, 12, o.ppm || 8); x.fillStyle = '#B4291C'; x.fillRect(0, 0, 18, 12); x.fillStyle = 'rgba(255,255,255,0.08)'; x.fillRect(0, 0, 18, 3);
  txt(x, T.review, 9, 7.8, { mm: 5.6, w: 700, f: 'mono', align: 'center', c: '#F7EFD8', max: 16.4 }); return c; }

// ---------------------------------------------------------------- item card (80 × 50); the BOTTOM 14 mm is the band that shows below a person card
// (items tuck behind the person card: kit first, then armour, then aid; each band 14 mm lower — fixed stack offsets)
export function itemCard(id, items, o = {}) {
  const lang = o.lang || 'tr', T = L[lang]; const it = items[id]; const [W, H] = SIZE.item; const [c, x] = mk(W, H, o.ppm || 8);
  paperGrain(x, W, H, '#ECE2C6', id.length * 37); box(x, 1.4, 1.4, W - 2.8, H - 2.8, INK, 0.4);
  // body: slot label, use line, big pictogram
  txt(x, (T.slot[it.slot] || '').toUpperCase(), 5, 9, { mm: 5.4, w: 700, f: 'mono', c: MUTE, ls: 0.4 });
  const use = { tr: { kit_tek: 'saha rolü: Teknisyen', kit_emp: 'saha rolü: EMP uzmanı', kit_ana: 'saha rolü: Analist', zirh: 'ilk hasarı karşılar, kırılır', bakim: 'bakımda +1 can, tükenir', emp_hucre: 'EMP kitine ek kullanım' },
    en: { kit_tek: 'field role: Technician', kit_emp: 'field role: EMP specialist', kit_ana: 'field role: Analyst', zirh: 'takes the first hit, breaks', bakim: '+1 HP in care, used up', emp_hucre: 'extra use for the EMP kit' } }[lang][id];
  x.fillStyle = INK; font(x, 400, 5.4, 'sans'); x.textAlign = 'left'; wrap(x, use, 5, 17.6, W - 34, 6.4);
  icon(x, ITEM_ICON[id], W - 27, 3.4, 23, INK);
  // identity band (bottom 14 mm): slot pattern block + light icon + name
  const by = H - 15.4; rule(x, 1.4, by, W - 1.4, INK, 0.4);
  slotBand(x, it.slot, 1.4, by, 11.8, 14); icon(x, ITEM_ICON[id], 2.5, by + 2.1, 9.6, '#F2E9CC');
  txt(x, lang === 'en' ? it.en : it.tr, 15.4, by + 10.2, { mm: 9.4, w: 700, max: W - 18, min: 0.8 });
  return c;
}
function wrap(x, t, X, Y, maxW, lh) { const words = String(t).split(' '); let line = '', y = Y; for (const w of words) { const n = line ? line + ' ' + w : w; if (x.measureText(n).width > maxW && line) { x.fillText(line, X, y); line = w; y += lh; } else line = n; } if (line) x.fillText(line, X, y); return y; }

// ---------------------------------------------------------------- raw material stock card (80 × 56): big free number, one pip per unit (reserved hatched)
export function materialCard(id, stock, reserved, mats, o = {}) {
  const lang = o.lang || 'tr', T = L[lang]; const [W, H] = SIZE.material; const [c, x] = mk(W, H, o.ppm || 8); const free = stock - reserved;
  paperGrain(x, W, H, '#E9E0C4', id.length * 53); box(x, 1.4, 1.4, W - 2.8, H - 2.8, INK, 0.4);
  icon(x, id, 4, 3.6, 10, INK); txt(x, (lang === 'en' ? mats[id].en : mats[id].tr).toUpperCase(), 16.4, 11.2, { mm: 9.4, w: 700, f: 'mono', max: 40 });
  // big free count
  txt(x, String(free), W - 5, 27.6, { mm: 22, w: 700, f: 'mono', align: 'right', c: free ? INK : '#8C826E' });
  txt(x, T.free, W - 5, 33.2, { mm: 5, w: 700, f: 'mono', align: 'right', c: MUTE });
  // pips: up to 20 visible (2 rows of 10); reserved = hatched ring
  const n = Math.min(stock, 20);
  for (let i = 0; i < n; i++) { const col = i % 10, row = Math.floor(i / 10); const cx = 6.6 + col * 4.5, cy = 20 + row * 5.6; const isRes = i >= free;
    x.beginPath(); x.arc(cx, cy, 1.7, 0, 7); if (!isRes) { x.fillStyle = INK; x.fill(); } else { x.strokeStyle = RED; x.lineWidth = 0.45; x.stroke(); x.beginPath(); x.moveTo(cx - 1.2, cy + 1.2); x.lineTo(cx + 1.2, cy - 1.2); x.stroke(); } }
  if (stock > 20) txt(x, `+${stock - 20}`, 6.6 + 10 * 4.5, 31.6, { mm: 5, w: 700, f: 'mono', c: MUTE });
  rule(x, 1.4, 38, W - 1.4, RULE, 0.3);
  txt(x, `${T.stock} ${stock}`, 5, 47.4, { mm: 7.4, w: 700, f: 'mono' }); txt(x, `${T.res} ${reserved}`, W - 5, 47.4, { mm: 7.4, w: 700, f: 'mono', align: 'right', c: reserved ? RED : MUTE });
  return c;
}
// one-unit chip dropped into the work order (56 × 40)
export function materialChip(id, mats, o = {}) { const lang = o.lang || 'tr', T = L[lang]; const [W, H] = SIZE.chip; const [c, x] = mk(W, H, o.ppm || 8);
  paperGrain(x, W, H, '#E9E0C4', id.length * 11); box(x, 1.2, 1.2, W - 2.4, H - 2.4, INK, 0.4); x.fillStyle = 'rgba(154,30,20,0.85)'; x.fillRect(1.2, 1.2, W - 2.4, 3);
  icon(x, id, 4, 9, 14, INK); txt(x, '1', W - 5, 24, { mm: 15, w: 700, f: 'mono', align: 'right' });
  txt(x, (lang === 'en' ? mats[id].en : mats[id].tr).toUpperCase(), W / 2, 35.4, { mm: 7.6, w: 700, f: 'mono', align: 'center', max: W - 6 }); return c; }

// ---------------------------------------------------------------- recipe card (112 × 80)
export function recipeCard(r, mats, o = {}) { const lang = o.lang || 'tr', T = L[lang]; const [W, H] = SIZE.recipe; const [c, x] = mk(W, H, o.ppm || 8);
  paperGrain(x, W, H, '#F0E8D2', r.id.length * 17); box(x, 1.4, 1.4, W - 2.8, H - 2.8, INK, 0.45); box(x, 2.4, 2.4, W - 4.8, H - 4.8, INK, 0.16);
  txt(x, lang === 'en' ? r.en : r.tr, 6, 14, { mm: 10, w: 700, max: W - 34 }); icon(x, ITEM_ICON[r.id], W - 24, 4, 17, INK);
  rule(x, 6, 18.6, W - 6, RED, 0.5);
  txt(x, T.inp, 6, 28, { mm: 5.4, w: 700, f: 'mono', c: MUTE, ls: 0.5 });
  let xx = 6; for (const [k, n] of Object.entries(r.in)) { icon(x, k, xx, 32, 13, INK); txt(x, `×${n}`, xx + 15, 43, { mm: 9.5, w: 700, f: 'mono' }); txt(x, lang === 'en' ? mats[k].en : mats[k].tr, xx, 52.4, { mm: 6.2, w: 400 }); xx += 38; }
  rule(x, 6, 58, W - 6, RULE, 0.3);
  font(x, 700, 9.5, 'mono'); const sw = x.measureText(T.shifts(r.shifts)).width; txt(x, T.shifts(r.shifts), 6, 70, { mm: 9.5, w: 700, f: 'mono' }); txt(x, `→ ${T.one}`, W - 6, 70, { mm: 9.5, w: 700, f: 'mono', align: 'right' });
  if (o.progress != null) { for (let i = 0; i < r.shifts; i++) { const bx = 8 + sw + i * 8.2; box(x, bx, 62.6, 6.4, 6.4, INK, 0.45); if (i < o.progress) { x.fillStyle = INK; x.fillRect(bx + 1.2, 63.8, 4, 4); } } }
  return c; }

// card backs (one per kind), plain stock with a printed band
export function cardBack(kind, o = {}) { const [W, H] = SIZE[kind]; const [c, x] = mk(W, H, o.ppm || 4); paperGrain(x, W, H, '#E2D7B8', 91); box(x, 2, 2, W - 4, H - 4, '#8C826E', 0.4);
  x.save(); x.translate(W / 2, H / 2); x.rotate(-Math.PI / 2 * (kind === 'person' ? 1 : 0)); font(x, 800, Math.min(W, H) * 0.16, 'sten'); x.fillStyle = 'rgba(30,27,22,0.22)'; x.textAlign = 'center'; x.fillText('KYGA · 07', 0, Math.min(W, H) * 0.06); x.restore(); return c; }

// ---------------------------------------------------------------- board prints
// a pocket's printed field (drop target): dashed card outline, person glyph, three tab strips. W × H mm
export function pocketPrint(slot, o = {}) { const lang = o.lang || 'tr', T = L[lang]; const W = o.w || 104, H = o.h || 182; const [c, x] = mk(W, H, o.ppm || 6);
  x.clearRect(0, 0, W, H); const cx = (W - 80) / 2, cy = 4;
  box(x, cx, cy, 80, 120, 'rgba(232,224,200,0.55)', 0.5, [2.2, 1.6]);
  icon(x, 'person', cx + 28, cy + 36, 24, 'rgba(232,224,200,0.32)');
  const strips = [T.slot.kit, T.slot.armor, T.slot.aux];
  strips.forEach((s, i) => { const y = cy + 120 + 1 + i * 14; box(x, cx, y, 80, 13, 'rgba(232,224,200,0.42)', 0.4, [1.6, 1.4]);
    const need = o.field && i === 0; txt(x, need ? `${s} · ${lang === 'en' ? 'NEEDED' : 'GEREKLİ'}` : s, cx + 40, y + 9.2, { mm: 6.6, w: 700, f: 'mono', align: 'center', c: need ? 'rgba(240,150,120,0.85)' : 'rgba(232,224,200,0.6)' }); });
  return c; }
// engraved brass title plate text (transparent; drawn onto the plate quad)
export function plateText(text, wmm, hmm, o = {}) { const [c, x] = mk(wmm, hmm, o.ppm || 10); x.clearRect(0, 0, wmm, hmm); txt(x, text, wmm / 2, hmm * 0.7, { mm: o.mm || hmm * 0.58, w: 700, f: o.f || 'mono', align: 'center', c: o.c || '#1C150E', max: wmm - 3, ls: 0.3 }); return c; }
// chalk slate (header): left-aligned chalk line(s)
export function chalkSlate(lines, wmm, hmm, o = {}) { const [c, x] = mk(wmm, hmm, o.ppm || 8); x.fillStyle = '#2B302D'; x.fillRect(0, 0, wmm, hmm);
  x.globalAlpha = 0.07; for (let i = 0; i < 60; i++) { x.fillStyle = '#EDEBE2'; x.fillRect(Math.random() * wmm, Math.random() * hmm, 20 * Math.random(), 1.2); } x.globalAlpha = 1;
  lines.forEach((l, i) => { txt(x, l, 4, hmm * (lines.length === 1 ? 0.7 : 0.42 + i * 0.4), { mm: o.mm || hmm * 0.5, w: 700, f: 'hand', c: '#EDEBE2', max: wmm - 8 }); }); return c; }
// İŞ EMRİ order sheet: printed form with recipe box, input fields, producer line, progress
export function orderSheet(o = {}) { const lang = o.lang || 'tr'; const W = o.w || 440, H = o.h || 170; const [c, x] = mk(W, H, o.ppm || 5);
  paperGrain(x, W, H, '#EFE7D1', 41); box(x, 2, 2, W - 4, H - 4, INK, 0.5);
  txt(x, lang === 'en' ? 'WORK ORDER · PRODUCTION' : 'İŞ EMRİ · ÜRETİM', 8, 14, { mm: 9.5, w: 700, f: 'mono' }); txt(x, `No ${String(o.no || 1).padStart(3, '0')}`, W - 8, 14, { mm: 7.6, w: 700, f: 'mono', align: 'right', c: MUTE });
  rule(x, 6, 18, W - 6, RED, 0.6);
  // recipe box (112 × 80 outline)
  box(x, 8, 22, 112, 80, 'rgba(30,27,22,0.45)', 0.5, [2.2, 1.6]); icon(x, 'book', 54, 46, 20, 'rgba(30,27,22,0.22)');
  txt(x, lang === 'en' ? 'RECIPE' : 'TARİF', 64, 98, { mm: 6, w: 700, f: 'mono', align: 'center', c: 'rgba(30,27,22,0.5)' });
  // inputs: four faint material fields
  txt(x, lang === 'en' ? 'INPUTS (drop materials anywhere here)' : 'GİRDİLER (hammaddeyi buraya bırak)', 132, 27, { mm: 6, w: 700, f: 'mono', c: 'rgba(30,27,22,0.55)', max: W - 140 });
  ['metal', 'devre', 'kumas', 'kimyasal'].forEach((k, i) => { const X = 132 + i * 74; box(x, X, 31, 66, 56, 'rgba(30,27,22,0.28)', 0.4, [1.6, 1.4]); icon(x, k, X + 23, 49, 20, 'rgba(30,27,22,0.2)'); });
  rule(x, 6, 110, W - 6, RULE, 0.3);
  txt(x, lang === 'en' ? 'MAKER: the person in the PRODUCTION pocket' : 'ÜRETİCİ: ÜRETİM EKİBİ cebindeki kişi', 8, 122, { mm: 7, w: 400, c: INK });
  txt(x, lang === 'en' ? 'AT CLOSE: 1 item → STORE' : 'KAPANIŞTA: 1 ürün → DEPO', W - 8, 122, { mm: 7, w: 700, f: 'mono', align: 'right' });
  if (o.status) txt(x, o.status, 8, 144, { mm: 8.6, w: 700, f: 'hand', c: BLUE, max: W - 16 });
  return c; }
// folder tab label
export function folderTab(name, o = {}) { const lang = o.lang || 'tr'; const [c, x] = mk(70, 16, o.ppm || 8); x.fillStyle = '#F3ECD6'; x.fillRect(0, 0, 70, 16); box(x, 0.6, 0.6, 68.8, 14.8, INK, 0.3);
  txt(x, (lang === 'en' ? 'FILE · ' : 'DOSYA · ') + name.toLocaleUpperCase(lang === 'en' ? 'en' : 'tr'), 35, 11, { mm: 8.2, w: 700, f: 'mono', align: 'center', max: 66 }); return c; }
// manila folder face
export function folderFace(name, o = {}) { const lang = o.lang || 'tr'; const W = 130, H = 175; const [c, x] = mk(W, H, o.ppm || 5); paperGrain(x, W, H, '#C9A86A', 13);
  x.fillStyle = 'rgba(80,56,20,0.10)'; for (let i = 0; i < 9; i++) x.fillRect(0, 20 + i * 18, W, 0.4);
  txt(x, lang === 'en' ? 'PERSONAL FILE' : 'KİŞİ DOSYASI', W / 2, 40, { mm: 10, w: 700, f: 'mono', align: 'center', c: '#2A1E10' });
  rule(x, 20, 46, W - 20, '#2A1E10', 0.5); if (name) txt(x, name, W / 2, 66, { mm: 13, w: 700, f: 'sans', align: 'center', c: '#2A1E10' });
  txt(x, lang === 'en' ? 'click: open' : 'tıkla: aç', W / 2, H - 14, { mm: 6, w: 700, f: 'mono', align: 'center', c: 'rgba(42,30,16,0.55)' }); return c; }
// recipe book cover
export function bookCover(o = {}) { const lang = o.lang || 'tr'; const W = 105, H = 148; const [c, x] = mk(W, H, o.ppm || 6); x.fillStyle = '#5A2420'; x.fillRect(0, 0, W, H);
  x.globalAlpha = 0.12; for (let i = 0; i < 900; i++) { x.fillStyle = i % 2 ? '#000' : '#fff'; x.fillRect(Math.random() * W, Math.random() * H, 0.5, 0.5); } x.globalAlpha = 1;
  x.fillStyle = '#3E1714'; x.fillRect(0, 0, 9, H); // spine band
  x.fillStyle = '#E9DFC2'; x.fillRect(22, 30, 66, 34); box(x, 23.5, 31.5, 63, 31, '#3E1714', 0.5);
  txt(x, lang === 'en' ? 'RECIPE' : 'TARİF', 55, 45, { mm: 10.5, w: 700, f: 'mono', align: 'center', c: '#2A1E10' }); txt(x, lang === 'en' ? 'BOOK' : 'DEFTERİ', 55, 58, { mm: 10.5, w: 700, f: 'mono', align: 'center', c: '#2A1E10' });
  txt(x, 'KYGA · 07', 55, H - 14, { mm: 6.2, w: 700, f: 'mono', align: 'center', c: 'rgba(233,223,194,0.65)' }); return c; }
// enamel sign (MASAYA DÖN, labels)
export function enamelSign(text, wmm, hmm, o = {}) { const [c, x] = mk(wmm, hmm, o.ppm || 8); x.fillStyle = o.bg || '#E8E2CF'; x.fillRect(0, 0, wmm, hmm); box(x, 1.2, 1.2, wmm - 2.4, hmm - 2.4, o.fg || '#1E1B16', 0.6);
  txt(x, text, wmm / 2, hmm * 0.68, { mm: o.mm || hmm * 0.5, w: 700, f: 'mono', align: 'center', c: o.fg || '#1E1B16', max: wmm - 5 }); return c; }
// printed hint under the BOŞTA tray: three fanned card outlines toward the left
export function fanHint(o = {}) { const W = o.w || 300, H = o.h || 130; const [c, x] = mk(W, H, o.ppm || 4); x.clearRect(0, 0, W, H);
  for (let i = 0; i < 4; i++) box(x, W - 84 - i * 58, 6, 80, 116, `rgba(232,224,200,${0.30 - i * 0.06})`, 0.6, [2.2, 1.6]);
  x.strokeStyle = 'rgba(232,224,200,0.35)'; x.lineWidth = 0.8; x.beginPath(); x.moveTo(W - 92, 64); x.lineTo(W - 250, 64); x.stroke(); x.beginPath(); x.moveTo(W - 250, 64); x.lineTo(W - 243, 59); x.lineTo(W - 243, 69); x.closePath(); x.fillStyle = 'rgba(232,224,200,0.35)'; x.fill();
  return c; }
