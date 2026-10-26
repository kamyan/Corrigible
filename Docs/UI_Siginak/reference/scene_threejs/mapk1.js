import { canvas, tex, img } from './lib.js';

// Label anchors in the 1920x1080 Sade map frame
export const LBL = {
  na: [180, 300], ca: [226, 520], sa: [440, 700], weu: [735, 330], eeu: [1020, 262], ru: [1353, 205],
  me: [1057, 470], af: [938, 626], in: [1305, 505], cn: [1473, 409], sea: [1470, 574], oc: [1643, 800],
};
export const NAMES = {
  tr: { na: 'KUZEY AMERİKA', ca: 'ORTA AMERİKA', sa: 'GÜNEY AMERİKA', weu: 'BATI AVRUPA', eeu: 'DOĞU AVRUPA', ru: 'AVRASYA', me: 'O. DOĞU · K. AFRİKA', af: 'SAHRA ALTI AFRİKA', in: 'GÜNEY ASYA', cn: 'DOĞU ASYA', sea: 'GÜNEYDOĞU ASYA', oc: 'OKYANUSYA' },
  en: { na: 'NORTH AMERICA', ca: 'CENTRAL AMERICA', sa: 'SOUTH AMERICA', weu: 'WESTERN EUROPE', eeu: 'EASTERN EUROPE', ru: 'EURASIA', me: 'MIDDLE EAST · N. AFRICA', af: 'SUB-SAHARAN AFRICA', in: 'SOUTH ASIA', cn: 'EAST ASIA', sea: 'SOUTHEAST ASIA', oc: 'OCEANIA' },
};
const W = {
  tr: { occ: '%100', cand: '%100 · ADAY', free: 'ÖZGÜR', key: 'İŞARETLER', st: ['işgal', 'cephe', 'aday', 'özgür'], se: ['alındı', 'hazır', 'kilitli'], slip: 'GÖREV FİŞİ', pct67: '%66,7' },
  en: { occ: '100%', cand: '100% · CANDIDATE', free: 'FREE', key: 'KEY', st: ['occupied', 'front', 'candidate', 'free'], se: ['taken', 'ready', 'locked'], slip: 'MISSION SLIP', pct67: '66.7%' },
};

function haloText(x, s, px, py, size, color = '#2E2820', weight = 700) {
  x.font = `${weight} ${size}px "Courier Prime"`; x.textAlign = 'center'; x.textBaseline = 'middle';
  x.letterSpacing = (size * 0.04) + 'px';
  x.lineJoin = 'round'; x.strokeStyle = 'rgba(243,236,218,0.94)'; x.lineWidth = size * 0.34; x.strokeText(s, px, py);
  x.fillStyle = color; x.fillText(s, px, py);
  const w = x.measureText(s).width;
  x.letterSpacing = '0px';
  return w;
}
export function mark(x, kind, px, py, s = 26) {
  if (kind === 'done') { x.fillStyle = '#1A4789'; x.fillRect(px, py, s, s); x.strokeStyle = '#F4EEDD'; x.lineWidth = s * 0.13; x.lineCap = 'round'; x.lineJoin = 'round'; x.beginPath(); x.moveTo(px + s * 0.22, py + s * 0.52); x.lineTo(px + s * 0.42, py + s * 0.72); x.lineTo(px + s * 0.8, py + s * 0.28); x.stroke(); }
  else if (kind === 'ready') { x.fillStyle = '#FBF7EC'; x.fillRect(px, py, s, s); x.strokeStyle = '#1E1B16'; x.lineWidth = s * 0.12; x.strokeRect(px + s * 0.06, py + s * 0.06, s * 0.88, s * 0.88); x.fillStyle = '#B8241A'; x.beginPath(); x.moveTo(px + s * 0.36, py + s * 0.25); x.lineTo(px + s * 0.76, py + s * 0.5); x.lineTo(px + s * 0.36, py + s * 0.75); x.fill(); }
  else { x.fillStyle = 'rgba(251,247,236,0.9)'; x.fillRect(px, py, s, s); x.strokeStyle = '#675F50'; x.lineWidth = 2; x.setLineDash([4, 3]); x.strokeRect(px + 1, py + 1, s - 2, s - 2); x.setLineDash([]);
    x.strokeStyle = '#675F50'; x.lineWidth = 2.2; x.strokeRect(px + s * 0.3, py + s * 0.47, s * 0.4, s * 0.3); x.beginPath(); x.arc(px + s * 0.5, py + s * 0.47, s * 0.13, Math.PI, 0); x.stroke(); }
}
export function stampRound(x, cx, cy, r, word, sub, color, rot, alpha = 0.86) {
  x.save(); x.translate(cx, cy); x.rotate(rot); x.globalAlpha = alpha;
  x.strokeStyle = color; x.fillStyle = color;
  x.lineWidth = r * 0.09; x.beginPath(); x.arc(0, 0, r, 0, 7); x.stroke();
  x.lineWidth = r * 0.035; x.beginPath(); x.arc(0, 0, r - r * 0.17, 0, 7); x.stroke();
  x.fillRect(-r - 6, -r * 0.3, 2 * r + 12, r * 0.6);
  x.fillStyle = '#F3ECDA'; x.font = `700 ${Math.round(r * 0.4)}px "Courier Prime"`; x.textAlign = 'center'; x.textBaseline = 'middle'; x.fillText(word, 0, 1);
  x.fillStyle = color; x.font = `700 ${Math.round(r * 0.22)}px "Courier Prime"`; x.fillText(sub, 0, r * 0.56); x.fillText('KYGA-07', 0, -r * 0.56);
  x.globalCompositeOperation = 'destination-out';
  for (let i = 0; i < 26; i++) { x.globalAlpha = 0.5; x.beginPath(); x.arc((Math.sin(i * 12.9898) * 0.5) * r * 2, (Math.cos(i * 78.233) * 0.5) * r * 2, 1.5 + (i % 3), 0, 7); x.fill(); }
  x.restore();
}
export function pin(x, px, py, col = '#B8241A') {
  x.fillStyle = 'rgba(0,0,0,0.32)'; x.beginPath(); x.ellipse(px + 5, py + 7, 11, 6, 0.4, 0, 7); x.fill();
  x.fillStyle = col; x.beginPath(); x.arc(px, py, 11, 0, 7); x.fill();
  x.fillStyle = 'rgba(255,255,255,0.45)'; x.beginPath(); x.arc(px - 3.5, py - 3.5, 3.8, 0, 7); x.fill();
}
function grease(x, cx, cy, rx, ry) {
  x.strokeStyle = 'rgba(30,27,22,0.86)'; x.lineCap = 'round';
  x.lineWidth = 6; x.beginPath(); x.ellipse(cx, cy, rx, ry, -0.05, 0.15, Math.PI * 2.08); x.stroke();
  x.lineWidth = 3; x.beginPath(); x.ellipse(cx + 4, cy - 2, rx + 7, ry + 5, -0.09, -0.3, Math.PI * 1.55); x.stroke();
}
function dashedRing(x, cx, cy, rx, ry, col = 'rgba(70,63,52,0.92)', w = 3) {
  x.strokeStyle = col; x.lineWidth = w; x.setLineDash([12, 8]); x.beginPath(); x.ellipse(cx, cy, rx, ry, 0, 0, 7); x.stroke(); x.setLineDash([]);
}

// variants: 'start' (K7), 'mid_tag' (K1/K6), 'mid_open' (K2, slip as overlay), 'freed' (K3)
export async function mapCanvasK1(lang, svgUrl, variant = 'mid_tag', opt = {}) {
  const [c, x] = canvas(1920, 1080);
  const m = await img(svgUrl);
  x.drawImage(m, 0, 0, 1920, 1080);
  const N = NAMES[lang], T = W[lang];
  const state = {};
  for (const k of Object.keys(LBL)) state[k] = 'occ';
  if (variant === 'mid_tag' || variant === 'mid_open') { state.weu = 'free'; state.eeu = 'front'; state.na = 'cand'; state.me = 'cand'; }
  if (variant === 'freed') { state.weu = 'free'; state.eeu = 'freeNew'; state.na = 'cand'; state.me = 'cand'; state.ru = 'candSel'; }
  const S = 28, SUB = 23;
  const LW = {};
  for (const k of Object.keys(LBL)) {
    const [px, py] = LBL[k]; const s = state[k];
    const ly = (s === 'freeNew') ? py + 36 : py;
    x.font = `700 ${S}px "Courier Prime"`; x.letterSpacing = (S * 0.04) + 'px';
    let lines = [N[k]];
    if (x.measureText(N[k]).width > 330 && N[k].includes(' · ')) lines = N[k].split(' · ').map((t, i, arr) => i < arr.length - 1 ? t + ' ·' : t);
    const lw = Math.max(...lines.map(t => x.measureText(t).width)); x.letterSpacing = '0px';
    const yOff = lines.length > 1 ? -15 : 0;
    if (s === 'candSel') {
      x.fillStyle = 'rgba(0,0,0,0.28)'; x.fillRect(px - lw / 2 - 10 + 2, py - 18 + 3 + yOff, lw + 20, 36 * lines.length);
      x.fillStyle = '#F5D646'; x.fillRect(px - lw / 2 - 10, py - 18 + yOff, lw + 20, 36 * lines.length);
    }
    lines.forEach((t, i) => haloText(x, t, px, ly + yOff + i * 32, S, s === 'candSel' ? '#1E1B16' : '#2E2820'));
    LW[k] = lw;
    if (s === 'occ') { x.font = `700 ${SUB}px "Courier Prime"`; x.fillStyle = '#675F50'; x.textAlign = 'center'; x.fillText(T.occ, px, py + 30 + (lines.length - 1) * 16); }
    if (s === 'cand' || s === 'candSel') {
      x.font = `700 ${SUB}px "Courier Prime"`; x.fillStyle = s === 'candSel' ? '#1E1B16' : '#463F34'; x.textAlign = 'center'; x.fillText(T.cand, px, py + 32 + (lines.length - 1) * 16);
      const w = Math.max(lw, 190);
      dashedRing(x, px, py + 14 + (lines.length - 1) * 8, w / 2 + 34, 44 + (lines.length - 1) * 12, s === 'candSel' ? 'rgba(30,27,22,0.95)' : undefined, s === 'candSel' ? 4 : 3);
    }
  }
  // shelter marker
  x.fillStyle = '#1E1B16'; x.fillRect(640, 280, 56, 26); x.fillStyle = '#E6CF9C'; x.font = '700 18px "Courier Prime"'; x.textAlign = 'center'; x.textBaseline = 'middle'; x.fillText('S-07', 668, 294);
  if (state.weu === 'free') {
    stampRound(x, 808, 242, 48, T.free, 'V.011', '#1A4789', -0.14);
    [0, 1, 2].forEach(i => mark(x, 'done', LBL.weu[0] - 44 + i * 30, LBL.weu[1] + 18, 26));
  }
  if (state.eeu === 'front') {
    [['done'], ['ready'], ['lock']].forEach(([k], i) => mark(x, k, LBL.eeu[0] - 74 + i * 30, LBL.eeu[1] + 18, 26));
    x.font = `700 ${SUB}px "Courier Prime"`; x.fillStyle = '#1E1B16'; x.textAlign = 'left'; x.fillText(T.pct67, LBL.eeu[0] + 24, LBL.eeu[1] + 32);
    grease(x, LBL.eeu[0] + 4, LBL.eeu[1] + 18, Math.max(126, LW.eeu / 2 + 28), 46);
    if (opt.token) { x.save(); x.translate(LBL.eeu[0] + Math.max(150, LW.eeu / 2 + 52), LBL.eeu[1] + 52); x.rotate(Math.PI / 4); x.fillStyle = 'rgba(0,0,0,0.3)'; x.fillRect(-12, -10, 30, 30); x.fillStyle = '#1E1B16'; x.fillRect(-15, -15, 30, 30); x.restore();
    x.fillStyle = '#E6CF9C'; x.font = '700 22px "Courier Prime"'; x.textAlign = 'center'; x.textBaseline = 'middle'; x.fillText('A', LBL.eeu[0] + Math.max(150, LW.eeu / 2 + 52), LBL.eeu[1] + 53); }
  }
  if (state.eeu === 'freeNew') {
    stampRound(x, 1012, 232, 50, T.free, 'V.019', '#1A4789', 0.1, 0.97);
    [0, 1, 2].forEach(i => mark(x, 'done', LBL.eeu[0] - 44 + i * 30, LBL.eeu[1] + 54, 26));
  }
  if (variant === 'mid_tag') {
    const p0 = [LBL.eeu[0] + Math.max(124, LW.eeu / 2 + 26), LBL.eeu[1] - 14], p1 = [1236, 150];
    x.strokeStyle = 'rgba(0,0,0,0.25)'; x.lineWidth = 3; x.beginPath(); x.moveTo(p0[0] + 3, p0[1] + 4); x.quadraticCurveTo(1190, 200, p1[0] + 3, p1[1] + 4); x.stroke();
    x.strokeStyle = '#A3241A'; x.lineWidth = 3; x.beginPath(); x.moveTo(p0[0], p0[1]); x.quadraticCurveTo(1190, 196, p1[0], p1[1]); x.stroke();
    x.save(); x.translate(1236, 150); x.rotate(-0.05);
    x.fillStyle = 'rgba(0,0,0,0.35)'; x.fillRect(8, 10, 320, 132);
    x.fillStyle = '#D9C08B'; x.fillRect(0, 0, 320, 132);
    x.fillStyle = '#B99D63'; x.fillRect(0, 0, 320, 8);
    x.fillStyle = '#1E1B16'; x.font = '700 30px "Courier Prime"'; x.textAlign = 'left'; x.textBaseline = 'alphabetic';
    x.fillText(T.slip, 20, 52);
    x.font = '700 40px "Courier Prime"'; x.fillText('OP-02 · 1/3', 20, 104);
    x.fillStyle = '#1E1B16'; x.fillRect(262, 66, 40, 44); x.fillStyle = '#E6CF9C'; x.font = '700 34px "Courier Prime"'; x.textAlign = 'center'; x.fillText('3', 282, 100);
    x.restore();
    pin(x, p1[0] + 12, p1[1] + 14);
    pin(x, p0[0], p0[1]);
  }
  // printed legend
  const lx = 28, ly = 902;
  x.font = '700 23px "Courier Prime"';
  const tw = T.st.map(w => x.measureText(w).width);
  const items1 = []; let cx = lx + 16;
  for (let i = 0; i < 4; i++) { items1.push(cx); cx += 44 + 8 + tw[i] + 22; }
  x.font = '700 23px "Courier Prime"'; const tw2 = T.se.map(w => x.measureText(w).width);
  const items2 = []; let cx2 = lx + 16;
  for (let i = 0; i < 3; i++) { items2.push(cx2); cx2 += 26 + 10 + tw2[i] + 34; }
  const legW = Math.max(cx, cx2) - lx;
  x.fillStyle = 'rgba(251,247,236,0.92)'; x.fillRect(lx, ly - 8, legW, 160); x.strokeStyle = '#463F34'; x.lineWidth = 2; x.strokeRect(lx, ly - 8, legW, 160);
  x.fillStyle = '#1E1B16'; x.font = '700 24px "Courier Prime"'; x.textAlign = 'left'; x.textBaseline = 'middle'; x.fillText(T.key, lx + 16, ly + 18);
  const r1 = ly + 66;
  { const sx = items1[0]; x.save(); x.beginPath(); x.rect(sx, r1 - 14, 40, 28); x.clip(); x.fillStyle = '#F3ECDA'; x.fillRect(sx, r1 - 14, 40, 28); x.strokeStyle = 'rgba(154,30,20,0.6)'; x.lineWidth = 2; for (let i = -40; i < 90; i += 9) { x.beginPath(); x.moveTo(sx + i, r1 - 14); x.lineTo(sx + i + 28, r1 + 14); x.stroke(); x.beginPath(); x.moveTo(sx + i + 28, r1 - 14); x.lineTo(sx + i, r1 + 14); x.stroke(); } x.restore(); }
  x.strokeStyle = '#1E1B16'; x.lineWidth = 4; x.beginPath(); x.ellipse(items1[1] + 20, r1, 20, 12, 0, 0, 7); x.stroke();
  dashedRing(x, items1[2] + 20, r1, 21, 12, undefined, 3);
  x.strokeStyle = '#1A4789'; x.lineWidth = 3.5; x.beginPath(); x.arc(items1[3] + 18, r1, 14, 0, 7); x.stroke(); x.beginPath(); x.arc(items1[3] + 18, r1, 9, 0, 7); x.stroke();
  x.fillStyle = '#1E1B16'; x.font = '700 23px "Courier Prime"';
  T.st.forEach((w, i) => x.fillText(w, items1[i] + 52, r1 + 1));
  const r2 = ly + 116;
  ['done', 'ready', 'lock'].forEach((k, i) => { mark(x, k, items2[i], r2 - 13, 26); x.fillStyle = '#1E1B16'; x.font = '700 23px "Courier Prime"'; x.textAlign = 'left'; x.textBaseline = 'middle'; x.fillText(T.se[i], items2[i] + 36, r2 + 1); });
  // lamp falloff (stands in for the tx_lamp_desk overlay of the world-space canvas)
  const g = x.createRadialGradient(1060, 600, 80, 1040, 640, 1200);
  g.addColorStop(0, 'rgba(255,233,190,0.14)'); g.addColorStop(0.42, 'rgba(22,13,7,0.05)'); g.addColorStop(0.75, 'rgba(22,13,7,0.32)'); g.addColorStop(1, 'rgba(22,13,7,0.62)');
  x.fillStyle = g; x.fillRect(0, 0, 1920, 1080);
  const t = tex(c);
  // inside the front region, below its label and grease ring (language independent)
  t.userData.teamFront = variant === 'freed' ? [1000, 388] : [1000, 372];
  t.userData.teamBunker = [560, 380];
  return t;
}
