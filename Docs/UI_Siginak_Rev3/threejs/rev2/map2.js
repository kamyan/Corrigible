// Revision 2 paper map (1920 x 1080 tex px, Sade frame, 12 game regions).
// Labels are sized for the start pose: names 40 px (K1 centre ≈ 17 px wide, map pose ≈ 25 px).
import { canvas, tex } from './lib.js';

let P = null;
async function paths() { if (!P) P = await (await fetch('/rev2/map/paths.json')).json(); return P; }

const C = {
  oceanC: '#DCE0D6', oceanE: '#C6CCC1', land: '#F3ECDA', grat: '#56604F', haloW: '#E4E7DE', haloN: '#B9C1B4',
  coast: '#3E3529', rbUnder: '#F4EEDD', rb: '#2C251E', cb: '#3A2A20', red: '#9A1E14', graphite: '#463F34', ink: '#1E1B16',
  hi: '#F5D646', smudge: '#8E8673', blue: '#1A4789', sub: '#675F50', paper: '#FBF7EC',
};
export const NAMES = {
  tr: { na: ['KUZEY AMERİKA'], sa: ['GÜNEY', 'AMERİKA'], weu: ['BATI AVRUPA'], eeu: ['DOĞU', 'AVRUPA'], ru: ['RUSYA'], me: ['ORTA DOĞU'], af: ['AFRİKA'], in: ['HİNDİSTAN'], cn: ['ÇİN'], jk: ['JAPONYA-', 'KORE'], sea: ['GÜNEYDOĞU', 'ASYA'], oc: ['OKYANUSYA'] },
  en: { na: ['NORTH AMERICA'], sa: ['SOUTH', 'AMERICA'], weu: ['WESTERN', 'EUROPE'], eeu: ['EASTERN', 'EUROPE'], ru: ['RUSSIA'], me: ['MIDDLE EAST'], af: ['AFRICA'], in: ['INDIA'], cn: ['CHINA'], jk: ['JAPAN-', 'KOREA'], sea: ['SOUTHEAST', 'ASIA'], oc: ['OCEANIA'] },
};
// label centre, font size; 'lead' = leader line target for labels placed off the region
export const LAB = {
  na: { at: [300, 318], s: 42, team: [330, 420] }, sa: { at: [455, 690], s: 40, team: [470, 800] }, weu: { at: [742, 352], s: 40, team: [668, 432] }, eeu: { at: [950, 305], s: 40, strip: [18, -38], team: [948, 226] },
  ru: { at: [1420, 160], s: 48, team: [1300, 260] }, me: { at: [1052, 470], s: 40, team: [1110, 405] }, af: { at: [925, 652], s: 42, team: [905, 760] }, in: { at: [1312, 530], s: 38, stripL: [-238, -30], team: [1268, 452] },
  cn: { at: [1452, 392], s: 42, team: [1400, 330] }, jk: { at: [1810, 360], s: 34, lead: [1668, 404], team: [1610, 395] }, sea: { at: [1530, 612], s: 36, team: [1470, 545] }, oc: { at: [1700, 812], s: 40, team: [1620, 745] },
};
const W = {
  tr: { occ: '%100', free: 'ÖZGÜR', key: 'İŞARETLER', st: ['işgal', 'cephe', 'aday', 'özgür'], se: ['geri alındı', 'hazır', 'kilitli'], cand: 'ADAY', front: 'CEPHE', reocc: 'YENİDEN İŞGAL', tier: 'TEHDİT' },
  en: { occ: '100%', free: 'FREE', key: 'KEY', st: ['occupied', 'front', 'candidate', 'free'], se: ['retaken', 'ready', 'locked'], cand: 'CANDIDATE', front: 'FRONT', reocc: 'RE-OCCUPIED', tier: 'THREAT' },
};
// free-region stamp positions + shift
const STAMP = { weu: [812, 186], eeu: [1090, 318], me: [1216, 390], af: [860, 762], ru: [1250, 250], in: [1222, 440], na: [522, 220], sa: [562, 842], cn: [1562, 320], jk: [1660, 482], sea: [1702, 642], oc: [1852, 720] };

function halo(x, s, px, py, size, color = '#2E2820', weight = 700, align = 'center') {
  x.font = `${weight} ${size}px "Courier Prime"`; x.textAlign = align; x.textBaseline = 'middle';
  x.letterSpacing = (size * 0.04) + 'px';
  x.lineJoin = 'round'; x.strokeStyle = 'rgba(243,236,218,0.95)'; x.lineWidth = size * 0.36; x.strokeText(s, px, py);
  x.fillStyle = color; x.fillText(s, px, py);
  const w = x.measureText(s).width; x.letterSpacing = '0px'; return w;
}
export function mark(x, kind, px, py, s = 36) {
  if (kind === 'done') { x.fillStyle = C.blue; x.fillRect(px, py, s, s); x.strokeStyle = '#F4EEDD'; x.lineWidth = s * 0.13; x.lineCap = 'round'; x.lineJoin = 'round'; x.beginPath(); x.moveTo(px + s * 0.22, py + s * 0.52); x.lineTo(px + s * 0.42, py + s * 0.72); x.lineTo(px + s * 0.8, py + s * 0.28); x.stroke(); }
  else if (kind === 'ready') { x.fillStyle = C.paper; x.fillRect(px, py, s, s); x.strokeStyle = C.ink; x.lineWidth = s * 0.12; x.strokeRect(px + s * 0.06, py + s * 0.06, s * 0.88, s * 0.88); x.fillStyle = '#B8241A'; x.beginPath(); x.moveTo(px + s * 0.36, py + s * 0.25); x.lineTo(px + s * 0.76, py + s * 0.5); x.lineTo(px + s * 0.36, py + s * 0.75); x.fill(); }
  else if (kind === 'reocc') { x.fillStyle = C.paper; x.fillRect(px, py, s, s); x.strokeStyle = C.red; x.lineWidth = s * 0.12; x.strokeRect(px + s * 0.06, py + s * 0.06, s * 0.88, s * 0.88); x.lineWidth = s * 0.12; x.lineCap = 'round'; x.beginPath(); x.moveTo(px + s * 0.28, py + s * 0.28); x.lineTo(px + s * 0.72, py + s * 0.72); x.moveTo(px + s * 0.72, py + s * 0.28); x.lineTo(px + s * 0.28, py + s * 0.72); x.stroke(); }
  else { x.fillStyle = 'rgba(251,247,236,0.92)'; x.fillRect(px, py, s, s); x.strokeStyle = C.sub; x.lineWidth = 2.4; x.setLineDash([5, 4]); x.strokeRect(px + 1.5, py + 1.5, s - 3, s - 3); x.setLineDash([]);
    x.strokeStyle = C.sub; x.lineWidth = s * 0.08; x.strokeRect(px + s * 0.3, py + s * 0.47, s * 0.4, s * 0.3); x.beginPath(); x.arc(px + s * 0.5, py + s * 0.47, s * 0.13, Math.PI, 0); x.stroke(); }
}
export function stampRound(x, cx, cy, r, word, sub, color, rot, alpha = 0.88) {
  x.save(); x.translate(cx, cy); x.rotate(rot); x.globalAlpha = alpha;
  x.strokeStyle = color; x.fillStyle = color;
  x.lineWidth = r * 0.09; x.beginPath(); x.arc(0, 0, r, 0, 7); x.stroke();
  x.lineWidth = r * 0.035; x.beginPath(); x.arc(0, 0, r - r * 0.17, 0, 7); x.stroke();
  x.fillRect(-r - 6, -r * 0.3, 2 * r + 12, r * 0.6);
  x.fillStyle = '#F3ECDA'; x.font = `700 ${Math.round(r * 0.4)}px "Courier Prime"`; x.textAlign = 'center'; x.textBaseline = 'middle'; x.fillText(word, 0, 1);
  x.fillStyle = color; x.font = `700 ${Math.round(r * 0.24)}px "Courier Prime"`; x.fillText(sub, 0, r * 0.56); x.fillText('KYGA-07', 0, -r * 0.56);
  x.globalCompositeOperation = 'destination-out';
  for (let i = 0; i < 26; i++) { x.globalAlpha = 0.5; x.beginPath(); x.arc((Math.sin(i * 12.9898) * 0.5) * r * 2, (Math.cos(i * 78.233) * 0.5) * r * 2, 1.5 + (i % 3), 0, 7); x.fill(); }
  x.restore();
}
export function stampRect(x, cx, cy, word, color, rot, size = 30, dashed = false) {
  x.save(); x.translate(cx, cy); x.rotate(rot);
  x.font = `700 ${size}px "Courier Prime"`; x.letterSpacing = (size * 0.08) + 'px';
  const w = x.measureText(word).width + size * 0.9, h = size * 1.5;
  x.globalAlpha = 0.9; x.strokeStyle = color; x.lineWidth = size * 0.12; if (dashed) x.setLineDash([size * 0.4, size * 0.25]);
  x.strokeRect(-w / 2, -h / 2, w, h); x.setLineDash([]);
  x.fillStyle = color; x.textAlign = 'center'; x.textBaseline = 'middle'; x.fillText(word, 0, 2); x.letterSpacing = '0px';
  x.restore(); return w;
}
function hatchFill(x, path, bb, gap, w, a, ang) {
  x.save(); x.clip(path); x.strokeStyle = C.red; x.globalAlpha = a; x.lineWidth = w;
  const [[x0, y0], [x1, y1]] = bb; const d = (x1 - x0) + (y1 - y0);
  x.beginPath();
  if (ang > 0) for (let t = -d; t < d; t += gap) { x.moveTo(x0 + t, y1); x.lineTo(x0 + t + (y1 - y0), y0); }
  else for (let t = -d; t < d; t += gap) { x.moveTo(x0 + t, y0); x.lineTo(x0 + t + (y1 - y0), y1); }
  x.stroke(); x.restore();
}
function shelter(x, px, py) {
  x.fillStyle = 'rgba(0,0,0,0.3)'; x.fillRect(px - 38 + 3, py - 18 + 4, 76, 36);
  x.fillStyle = C.ink; x.fillRect(px - 38, py - 18, 76, 36);
  x.fillStyle = '#E6CF9C'; x.font = '700 24px "Courier Prime"'; x.textAlign = 'center'; x.textBaseline = 'middle'; x.fillText('S-07', px, py + 1);
}

// glyph for signature machines / field trait (small ink pictograms printed on the map, 34 px)
export function glyph(x, kind, px, py, s = 34, col = C.ink) {
  x.save(); x.translate(px, py); x.strokeStyle = col; x.fillStyle = col; x.lineWidth = s * 0.08; x.lineCap = 'round'; x.lineJoin = 'round';
  x.strokeRect(-s / 2, -s / 2, s, s);
  const k = s * 0.32;
  if (kind === 'corridor') { // two walls with a narrow gap and a passage arrow
    x.fillRect(-k * 1.05, -k, k * 0.62, k * 2); x.fillRect(k * 0.43, -k, k * 0.62, k * 2);
    x.lineWidth = s * 0.07; x.beginPath(); x.moveTo(0, k * 0.85); x.lineTo(0, -k * 0.6); x.moveTo(-k * 0.2, -k * 0.35); x.lineTo(0, -k * 0.75); x.lineTo(k * 0.2, -k * 0.35); x.stroke(); }
  if (kind === 'locker') { // clamp jaws closing on a unit
    x.lineWidth = s * 0.1; x.beginPath(); x.moveTo(-k * 0.45, -k); x.lineTo(-k, -k); x.lineTo(-k, k); x.lineTo(-k * 0.45, k); x.moveTo(k * 0.45, -k); x.lineTo(k, -k); x.lineTo(k, k); x.lineTo(k * 0.45, k); x.stroke();
    x.beginPath(); x.arc(0, 0, k * 0.36, 0, 7); x.fill(); }
  if (kind === 'mortar') { x.lineWidth = s * 0.12; x.beginPath(); x.moveTo(-k * 0.7, k * 0.9); x.lineTo(k * 0.1, -k * 0.2); x.stroke(); x.lineWidth = s * 0.06; x.setLineDash([s * 0.08, s * 0.08]); x.beginPath(); x.moveTo(k * 0.15, -k * 0.35); x.quadraticCurveTo(k * 0.55, -k * 1.0, k * 0.9, -k * 0.2); x.stroke(); x.setLineDash([]); x.beginPath(); x.arc(k * 0.9, -k * 0.05, k * 0.2, 0, 7); x.fill(); }
  if (kind === 'sand') { x.lineWidth = s * 0.07; for (let i = -1; i <= 1; i++) { x.beginPath(); x.moveTo(-k, i * k * 0.62); x.quadraticCurveTo(-k * 0.5, i * k * 0.62 - k * 0.35, 0, i * k * 0.62); x.quadraticCurveTo(k * 0.5, i * k * 0.62 + k * 0.35, k, i * k * 0.62); x.stroke(); } }
  if (kind === 'drone') { x.lineWidth = s * 0.08; x.beginPath(); x.moveTo(-k * 0.6, -k * 0.6); x.lineTo(k * 0.6, k * 0.6); x.moveTo(k * 0.6, -k * 0.6); x.lineTo(-k * 0.6, k * 0.6); x.stroke(); for (const [a, b] of [[-1, -1], [1, -1], [1, 1], [-1, 1]]) { x.beginPath(); x.arc(a * k * 0.7, b * k * 0.7, k * 0.3, 0, 7); x.stroke(); } }
  if (kind === 'maint') { x.lineWidth = s * 0.12; x.beginPath(); x.moveTo(-k * 0.75, k * 0.75); x.lineTo(k * 0.25, -k * 0.25); x.stroke(); x.lineWidth = s * 0.1; x.beginPath(); x.arc(k * 0.45, -k * 0.45, k * 0.42, Math.PI * 0.95, Math.PI * 2.55); x.stroke(); }
  if (kind === 'shield') { x.beginPath(); x.moveTo(0, -k); x.lineTo(k * 0.85, -k * 0.55); x.lineTo(k * 0.7, k * 0.35); x.lineTo(0, k); x.lineTo(-k * 0.7, k * 0.35); x.lineTo(-k * 0.85, -k * 0.55); x.closePath(); x.lineWidth = s * 0.09; x.stroke(); }
  x.restore();
}

// state: { regions:{k:'occ'|'front'|'free'|'freeNew'|'cand'|'candSel'}, frontPct, sectors:{k:[...]}, freeShift:{k:'V.011'},
//          trait:{k:'corridor'}, sig:{k:'locker'}, tier:'1/4', reocc:'eeu', team:{at:[x,y]}, legend:true }
export async function mapTex2(state, lang = 'tr', opt = {}) {
  const Pp = await paths();
  const [c, x] = canvas(1920, 1080);
  const N = NAMES[lang], T = W[lang];
  const keys = Object.keys(LAB);
  const R = {}; for (const k of keys) R[k] = new Path2D(Pp.regions[k]);
  const O = {}; for (const k of keys) O[k] = new Path2D(Pp.outlines[k]);
  // ocean
  const g = x.createRadialGradient(960, 500, 100, 960, 540, 1150); g.addColorStop(0, C.oceanC); g.addColorStop(1, C.oceanE);
  x.fillStyle = g; x.fillRect(0, 0, 1920, 1080);
  x.lineWidth = 1; x.strokeStyle = C.grat; x.globalAlpha = 0.2; x.stroke(new Path2D(Pp.graticule)); x.globalAlpha = 0.24; x.setLineDash([6, 5]); x.stroke(new Path2D(Pp.tropics)); x.setLineDash([]); x.globalAlpha = 0.34; x.lineWidth = 1.2; x.stroke(new Path2D(Pp.equator)); x.globalAlpha = 1;
  x.lineJoin = 'round';
  for (const k of keys) { x.strokeStyle = C.haloW; x.globalAlpha = 0.75; x.lineWidth = 11; x.stroke(R[k]); }
  for (const k of keys) { x.strokeStyle = C.haloN; x.globalAlpha = 0.9; x.lineWidth = 4; x.stroke(R[k]); }
  x.globalAlpha = 1; x.fillStyle = C.land; for (const k of keys) x.fill(R[k]);
  const st = state.regions || {};
  for (const k of keys) {
    const s = st[k] || 'occ'; const bb = Pp.bbox[k];
    if (s === 'occ' || s === 'cand' || s === 'candSel') { hatchFill(x, R[k], bb, 11, 1.7, 0.5, 1); hatchFill(x, R[k], bb, 11, 1.4, 0.32, -1); }
    else if (s === 'front') { const gap = state.frontGap || 10; hatchFill(x, R[k], bb, gap, 1.8, 0.55, 1); }
    else if (s === 'free' || s === 'freeNew') { hatchFill(x, R[k], bb, 10, 1.4, 0.07, 1); }
    if (s === 'candSel') { x.save(); x.globalAlpha = 0.34; x.fillStyle = C.hi; x.fill(R[k]); x.restore(); }
  }
  x.strokeStyle = C.cb; x.globalAlpha = 0.22; x.lineWidth = 0.8; x.stroke(new Path2D(Pp.countryBorders)); x.globalAlpha = 1;
  for (const k of keys) { x.strokeStyle = C.coast; x.globalAlpha = 0.8; x.lineWidth = 1.1; x.stroke(R[k]); }
  x.globalAlpha = 0.9; x.strokeStyle = C.rbUnder; x.lineWidth = 4.2; x.lineCap = 'round'; x.stroke(new Path2D(Pp.regionBorders));
  x.globalAlpha = 0.75; x.strokeStyle = C.rb; x.lineWidth = 1.6; x.stroke(new Path2D(Pp.regionBorders)); x.globalAlpha = 1;
  for (const k of keys) {
    const s = st[k];
    if (s === 'cand' || s === 'candSel' || s === 'front') {
      x.save(); x.clip(R[k]);
      if (s === 'front') { x.strokeStyle = C.ink; x.globalAlpha = 0.85; x.lineWidth = 10; }
      else { x.strokeStyle = s === 'candSel' ? C.ink : C.graphite; x.globalAlpha = 0.9; x.lineWidth = s === 'candSel' ? 8 : 7; x.setLineDash([12, 8]); }
      x.stroke(O[k]); x.restore();
    }
  }
  // labels
  const anchors = {};
  for (const k of keys) {
    const L = LAB[k]; const s = st[k] || 'occ'; const lines = N[k];
    const [px, py] = L.at; const size = L.s; const lh = size * 1.02;
    const y0 = py - (lines.length - 1) * lh / 2;
    if (L.lead) { x.strokeStyle = C.graphite; x.lineWidth = 2.4; x.beginPath(); x.moveTo(px - 70, py); x.lineTo(L.lead[0], L.lead[1]); x.stroke(); x.fillStyle = C.graphite; x.beginPath(); x.arc(L.lead[0], L.lead[1], 5, 0, 7); x.fill(); }
    let lw = 0;
    lines.forEach((t, i) => { lw = Math.max(lw, halo(x, t, px, y0 + i * lh, size, s === 'candSel' ? C.ink : '#2E2820')); });
    const below = y0 + (lines.length - 1) * lh + size * 0.5;
    anchors[k] = { x: px, y: py, w: lw, below };
    if (s === 'occ' || s === 'cand' || s === 'candSel') {
      x.font = `700 28px "Courier Prime"`; x.fillStyle = C.sub; x.textAlign = 'center'; x.textBaseline = 'top';
      x.lineWidth = 9; x.strokeStyle = 'rgba(243,236,218,0.9)'; x.strokeText(T.occ, px, below + 6); x.fillText(T.occ, px, below + 6);
      if (s !== 'occ') stampRect(x, px, below + 62, T.cand, s === 'candSel' ? C.ink : C.graphite, -0.04, 24, true);
    }
  }
  // free regions: round stamp + three retaken marks
  for (const k of keys) {
    const s = st[k];
    if (s === 'free' || s === 'freeNew') {
      const [sx, sy] = STAMP[k]; stampRound(x, sx, sy, s === 'freeNew' ? 60 : 54, T.free, (state.freeShift || {})[k] || 'V.---', C.blue, (k.charCodeAt(0) % 7 - 3) * 0.04, s === 'freeNew' ? 0.97 : 0.86);
      const a = anchors[k]; [0, 1, 2].forEach(i => mark(x, 'done', a.x - 60 + i * 42, a.below + 8, 36));
    }
  }
  // front region: sector strip, percent, trait and signature glyphs
  const fk = keys.find(k => st[k] === 'front');
  if (fk) {
    const a = anchors[fk]; const secs = (state.sectors || {})[fk] || ['done', 'ready', 'lock'];
    const so = LAB[fk].strip, sl = LAB[fk].stripL;
    const sx0 = so ? a.x + a.w / 2 + so[0] : sl ? a.x - a.w / 2 + sl[0] : a.x - a.w / 2, sy0 = so ? a.y + so[1] : sl ? a.y + sl[1] : a.below + 10;
    secs.forEach((m, i) => mark(x, m, sx0 + i * 44, sy0, 38));
    x.font = '700 32px "Courier Prime"'; x.textAlign = 'left'; x.textBaseline = 'middle';
    x.lineWidth = 9; x.strokeStyle = 'rgba(243,236,218,0.92)'; x.strokeText(state.frontPct || '%66,7', sx0 + 3 * 44 + 10, sy0 + 20); x.fillStyle = C.ink; x.fillText(state.frontPct || '%66,7', sx0 + 3 * 44 + 10, sy0 + 20);
    const tr = (state.trait || {})[fk], sg = (state.sig || {})[fk];
    let gx = sx0 + 19;
    if (tr || sg) {
      const gy = sy0 + 70;
      x.fillStyle = 'rgba(251,247,236,0.9)'; x.fillRect(gx - 25, gy - 23, (tr && sg) ? 96 : 50, 46);
      if (tr) { glyph(x, tr, gx, gy, 34); gx += 46; }
      if (sg) { glyph(x, sg, gx, gy, 34, C.red); }
    }
    if (state.reocc === fk) stampRect(x, sx0 + 150, sy0 + 128, T.reocc, C.red, -0.05, 26);
    anchors.front = { x: a.x, y: sy0 };
  }
  // shelter marker
  shelter(x, 600, 268);
  // legend (bottom right, over the Southern Indian Ocean)
  if (state.legend !== false) {
    const lx = 1030, ly = 912;
    x.font = '700 23px "Courier Prime"';
    const tw = T.st.map(w => x.measureText(w).width); const tw2 = T.se.map(w => x.measureText(w).width);
    let cx1 = lx + 16; const it1 = []; for (let i = 0; i < 4; i++) { it1.push(cx1); cx1 += 44 + 8 + tw[i] + 20; }
    let cx2 = lx + 16; const it2 = []; for (let i = 0; i < 3; i++) { it2.push(cx2); cx2 += 26 + 10 + tw2[i] + 28; }
    const legW = Math.max(cx1, cx2) - lx;
    x.fillStyle = 'rgba(251,247,236,0.93)'; x.fillRect(lx, ly - 8, legW, 150); x.strokeStyle = C.graphite; x.lineWidth = 2; x.strokeRect(lx, ly - 8, legW, 150);
    x.fillStyle = C.ink; x.font = '700 24px "Courier Prime"'; x.textAlign = 'left'; x.textBaseline = 'middle'; x.fillText(T.key, lx + 16, ly + 16);
    if (state.tier) { x.textAlign = 'right'; x.fillText(`${T.tier} ${state.tier}`, lx + legW - 16, ly + 16); x.textAlign = 'left'; }
    const r1 = ly + 60;
    { const sx = it1[0]; x.save(); x.beginPath(); x.rect(sx, r1 - 14, 40, 28); x.clip(); x.fillStyle = C.land; x.fillRect(sx, r1 - 14, 40, 28); x.strokeStyle = 'rgba(154,30,20,0.6)'; x.lineWidth = 2; for (let i = -40; i < 90; i += 9) { x.beginPath(); x.moveTo(sx + i, r1 - 14); x.lineTo(sx + i + 28, r1 + 14); x.stroke(); x.beginPath(); x.moveTo(sx + i + 28, r1 - 14); x.lineTo(sx + i, r1 + 14); x.stroke(); } x.restore(); }
    x.strokeStyle = C.ink; x.lineWidth = 4; x.strokeRect(it1[1] + 2, r1 - 12, 36, 24);
    x.strokeStyle = C.graphite; x.lineWidth = 3; x.setLineDash([8, 5]); x.strokeRect(it1[2] + 2, r1 - 12, 36, 24); x.setLineDash([]);
    x.strokeStyle = C.blue; x.lineWidth = 3.5; x.beginPath(); x.arc(it1[3] + 18, r1, 14, 0, 7); x.stroke(); x.beginPath(); x.arc(it1[3] + 18, r1, 9, 0, 7); x.stroke();
    x.fillStyle = C.ink; x.font = '700 23px "Courier Prime"'; T.st.forEach((w, i) => x.fillText(w, it1[i] + 52, r1 + 1));
    const r2 = ly + 106;
    ['done', 'ready', 'lock'].forEach((k, i) => { mark(x, k, it2[i], r2 - 13, 26); x.fillStyle = C.ink; x.font = '700 23px "Courier Prime"'; x.textAlign = 'left'; x.textBaseline = 'middle'; x.fillText(T.se[i], it2[i] + 36, r2 + 1); });
  }
  if (opt.extra) opt.extra(x, anchors);
  // lamp falloff (lighter than round 1 so edge labels stay legible)
  const lg = x.createRadialGradient(1040, 560, 120, 1000, 600, 1250);
  lg.addColorStop(0, 'rgba(255,233,190,0.10)'); lg.addColorStop(0.5, 'rgba(22,13,7,0.03)'); lg.addColorStop(0.8, 'rgba(22,13,7,0.18)'); lg.addColorStop(1, 'rgba(22,13,7,0.42)');
  x.fillStyle = lg; x.fillRect(0, 0, 1920, 1080);
  const t = tex(c);
  t.userData.anchors = anchors;
  t.userData.teamFront = state.team || (fk ? LAB[fk].team : [560, 380]);
  t.userData.teamBunker = [560, 380];
  t.userData.canvas = c;
  return t;
}
