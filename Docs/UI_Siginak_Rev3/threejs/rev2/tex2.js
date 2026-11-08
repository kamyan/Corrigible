// Revision 2 textures (world-space print / paint / screens).
import { canvas, tex, noise, textFit } from './lib.js';

const INK = '#1E1B16';
export const S2 = {
  tr: {
    counters: ['VARDİYA', 'FON', 'ERZAK', 'MALZEME', 'ÖZGÜR BÖLGE', 'GÜVEN'],
    keys: ['KOMUTA', 'SIĞINAK', 'GÖREVLER', 'KAPI', 'KILAVUZ', 'MASA'], keyCaps: ['1', '2', '3', '4', 'H', 'Home'],
    alarm: 'YZ ALARMI', cork: 'KURTARILAN BÖLGELER', free: 'ÖZGÜR', sw1: 'VARDİYAYI BİTİR', sw2: 'Enter · kapağı kaldır',
    telexPlate: 'TELEKS', crtPlate: 'TELSİZ · K-07', tray: 'EVRAK', depot: 'DEPO', radio: 'TELSİZ · K-07',
  },
  en: {
    counters: ['SHIFT', 'FUNDS', 'RATIONS', 'MATERIALS', 'FREE REGIONS', 'TRUST'],
    keys: ['COMMAND', 'SHELTER', 'MISSIONS', 'DOOR', 'GUIDE', 'DESK'], keyCaps: ['1', '2', '3', '4', 'H', 'Home'],
    alarm: 'AI ALARM', cork: 'FREED REGIONS', free: 'FREE', sw1: 'END SHIFT', sw2: 'Enter · lift the cover',
    telexPlate: 'TELEX', crtPlate: 'RADIO · K-07', tray: 'PAPERS', depot: 'STORE', radio: 'RADIO · K-07',
  },
};

// ---------- six-module counter face, 1.56 x 0.212 m at 2000 px/m
// values: [{d:'014'}, {d:'062', frac:'0'}, {d:'22', fixed:'/30'}, ...]
export function counterFace6(values, lang) {
  const W = 3120, H = 424; const [c, x] = canvas(W, H);
  x.fillStyle = '#1b130c'; x.fillRect(0, 0, W, H); noise(x, W, H, 8);
  const L = S2[lang].counters; const gap = 20, x0 = 30, modW = (W - 2 * x0 - 5 * gap) / 6;
  let size = 64; for (const lab of L) { let s = 64; x.font = `700 ${s}px "Courier Prime"`; while (x.measureText(lab).width > modW - 70 && s > 20) { s--; x.font = `700 ${s}px "Courier Prime"`; } size = Math.min(size, s); }
  values.forEach((v, i) => {
    const mx = x0 + i * (modW + gap);
    const pg = x.createLinearGradient(0, 44, 0, 138); pg.addColorStop(0, '#dcbd7d'); pg.addColorStop(0.5, '#b8955a'); pg.addColorStop(1, '#8c6f3e');
    x.fillStyle = pg; x.fillRect(mx + 6, 44, modW - 12, 94);
    x.strokeStyle = 'rgba(40,28,10,0.6)'; x.lineWidth = 3; x.strokeRect(mx + 7.5, 45.5, modW - 15, 91);
    x.fillStyle = '#2a1e10'; x.textAlign = 'center'; x.textBaseline = 'middle'; x.font = `700 ${size}px "Courier Prime"`; x.fillText(L[i], mx + modW / 2, 93);
    x.fillStyle = '#8c6f3e'; x.fillRect(mx + 14, 146, modW - 28, 264);
    x.fillStyle = '#0e0906'; x.fillRect(mx + 22, 156, modW - 44, 244);
    const ww = 84, wg = 7, top = 166, hh = 224, mid = top + hh / 2;
    x.font = '700 76px "Courier Prime"';
    const fixedW = v.fixed ? x.measureText(v.fixed).width + 12 : 0;
    const comma = v.frac != null ? 30 : 0; const fracW = v.frac != null ? 74 : 0;
    const totalW = v.d.length * ww + (v.d.length - 1) * wg + comma + fracW + fixedW;
    let wx = mx + modW / 2 - totalW / 2;
    const wheel = (ch, w, inv) => {
      const g = x.createLinearGradient(0, top, 0, top + hh);
      const base = inv ? [233, 221, 190] : [28, 21, 14];
      g.addColorStop(0, `rgb(${base.map(b => b * 0.35).join(',')})`); g.addColorStop(0.5, `rgb(${base.join(',')})`); g.addColorStop(1, `rgb(${base.map(b => b * 0.35).join(',')})`);
      x.fillStyle = g; x.fillRect(wx, top, w, hh);
      x.save(); x.beginPath(); x.rect(wx, top, w, hh); x.clip();
      x.fillStyle = inv ? '#1C150E' : '#efdcae'; x.font = `700 ${inv ? 140 : 160}px "Courier Prime"`; x.textAlign = 'center'; x.textBaseline = 'middle';
      x.fillText(ch, wx + w / 2, mid + 8);
      const sh = x.createLinearGradient(0, top, 0, top + hh); sh.addColorStop(0, 'rgba(0,0,0,0.7)'); sh.addColorStop(0.26, 'rgba(0,0,0,0)'); sh.addColorStop(0.74, 'rgba(0,0,0,0)'); sh.addColorStop(1, 'rgba(0,0,0,0.72)');
      x.fillStyle = sh; x.fillRect(wx, top, w, hh); x.restore();
      wx += w + wg;
    };
    for (const ch of v.d) wheel(ch, ww, false);
    if (v.frac != null) {
      wx -= wg; // brass comma between integer and tenths
      x.fillStyle = '#d4b06e'; x.beginPath(); x.arc(wx + 15, top + hh - 46, 11, 0, 7); x.fill();
      x.beginPath(); x.moveTo(wx + 22, top + hh - 44); x.quadraticCurveTo(wx + 20, top + hh - 20, wx + 8, top + hh - 10); x.lineWidth = 7; x.strokeStyle = '#d4b06e'; x.stroke();
      wx += comma; wheel(v.frac, 74, true);
    }
    if (v.fixed) { x.fillStyle = '#c9a86a'; x.font = '700 76px "Courier Prime"'; x.textAlign = 'left'; x.textBaseline = 'middle'; x.fillText(v.fixed, wx + 4, mid + 12); }
  });
  return tex(c);
}

// ---------- AI alarm board (wall), 0.53 x 0.32 m
export function alarmBoard(val, lang) {
  const W = 1060, H = 640; const [c, x] = canvas(W, H);
  x.fillStyle = '#2a1d14'; x.fillRect(0, 0, W, H); noise(x, W, H, 10);
  x.strokeStyle = '#8c6f3e'; x.lineWidth = 10; x.strokeRect(10, 10, W - 20, H - 20);
  const pg = x.createLinearGradient(0, 40, 0, 130); pg.addColorStop(0, '#dcbd7d'); pg.addColorStop(1, '#8c6f3e');
  x.fillStyle = pg; x.fillRect(60, 40, 560, 96); x.fillStyle = '#2a1e10'; x.font = '700 64px "Courier Prime"'; x.textAlign = 'center'; x.textBaseline = 'middle'; x.fillText(S2[lang].alarm, 340, 90);
  // counter window
  x.fillStyle = '#8c6f3e'; x.fillRect(60, 160, 560, 300); x.fillStyle = '#0e0906'; x.fillRect(72, 172, 536, 276);
  const d = String(val).padStart(2, '0');
  [0, 1].forEach(i => { const wx = 96 + i * 150; const g = x.createLinearGradient(0, 184, 0, 436); g.addColorStop(0, '#0a0705'); g.addColorStop(0.5, '#1c150e'); g.addColorStop(1, '#0a0705'); x.fillStyle = g; x.fillRect(wx, 184, 136, 252); x.fillStyle = '#efdcae'; x.font = '700 210px "Courier Prime"'; x.fillText(d[i], wx + 68, 318); });
  x.fillStyle = '#c9a86a'; x.font = '700 110px "Courier Prime"'; x.textAlign = 'left'; x.fillText('/10', 400, 330);
  // 10-step scale (red from 8)
  for (let i = 0; i < 10; i++) { const sx = 72 + i * 54; const on = i < val; x.fillStyle = on ? (i >= 7 ? '#c0281c' : '#e9ddbe') : '#3a2a1c'; x.fillRect(sx, 492, 44, 44); x.strokeStyle = '#8c6f3e'; x.lineWidth = 3; x.strokeRect(sx, 492, 44, 44); }
  x.fillStyle = '#c9a86a'; x.font = '700 34px "Courier Prime"'; x.textAlign = 'left'; x.fillText(lang === 'en' ? 'COUNTER-ATTACK AT 10' : '10\'DA KARŞI SALDIRI', 72, 586);
  // lamp bezel (the lamp itself is a mesh)
  x.fillStyle = '#8c6f3e'; x.beginPath(); x.arc(840, 300, 120, 0, 7); x.fill(); x.fillStyle = '#1a120b'; x.beginPath(); x.arc(840, 300, 100, 0, 7); x.fill();
  return tex(c);
}

// ---------- cork board cards
export function corkHeader(n, lang) {
  const W = 900, H = 260; const [c, x] = canvas(W, H);
  x.fillStyle = '#f3ecd8'; x.fillRect(0, 0, W, H); noise(x, W, H, 8);
  x.fillStyle = '#d2483f'; x.fillRect(0, 86, W, 4);
  x.fillStyle = INK; x.textBaseline = 'alphabetic'; x.textAlign = 'left';
  textFit(x, S2[lang].cork, 34, 70, W - 68, '700 {s} "Courier Prime"', 60);
  x.font = '700 140px "Courier Prime"'; x.fillText(n, 34, 232);
  x.fillStyle = '#675F50'; x.font = '700 64px "Courier Prime"'; x.fillText('/12', 34 + x.measureText(n).width + 210, 226);
  return tex(c);
}
export function corkCard(name, shift, lang, opt = {}) {
  const W = 600, H = 380; const [c, x] = canvas(W, H);
  x.fillStyle = opt.empty ? '#e6dfcb' : '#f6f0de'; x.fillRect(0, 0, W, H); noise(x, W, H, 8);
  if (opt.empty) { x.strokeStyle = 'rgba(70,63,52,0.45)'; x.lineWidth = 6; x.setLineDash([22, 16]); x.strokeRect(20, 20, W - 40, H - 40); x.setLineDash([]); x.fillStyle = 'rgba(70,63,52,0.6)'; x.font = '700 88px "Kalam"'; x.textAlign = 'center'; x.textBaseline = 'middle'; x.fillText(name, W / 2, H / 2 + 8); return tex(c); }
  x.fillStyle = '#7a8fb5'; for (let y = 120; y < H; y += 46) x.fillRect(0, y, W, 2);
  x.fillStyle = '#d2483f'; x.fillRect(64, 0, 3, H);
  x.fillStyle = INK; x.textAlign = 'left'; x.textBaseline = 'alphabetic';
  const lines = name.split('\n');
  lines.forEach((t, i) => textFit(x, t, 84, 98 + i * 92, W - 110, '700 {s} "Courier Prime"', 84));
  // blue stamp
  x.save(); x.translate(W - 170, H - 92); x.rotate(-0.12); x.globalAlpha = 0.9;
  x.strokeStyle = '#1A4789'; x.lineWidth = 10; x.beginPath(); x.arc(0, 0, 76, 0, 7); x.stroke(); x.lineWidth = 4; x.beginPath(); x.arc(0, 0, 62, 0, 7); x.stroke();
  x.fillStyle = '#1A4789'; x.fillRect(-90, -22, 180, 44); x.fillStyle = '#f6f0de'; x.font = '700 34px "Courier Prime"'; x.textAlign = 'center'; x.textBaseline = 'middle'; x.fillText(S2[lang].free, 0, 2);
  x.fillStyle = '#1A4789'; x.font = '700 26px "Courier Prime"'; x.fillText(shift, 0, 46); x.restore();
  return tex(c);
}

// ---------- CRT idle screen (big lines readable from the start pose)
export function crtScreen(lines, opt = {}) {
  const W = 1200, H = 900; const [c, x] = canvas(W, H);
  x.fillStyle = '#07100b'; x.fillRect(0, 0, W, H);
  let y = opt.top ?? 150;
  for (const [txt, size, col, inv] of lines) {
    if (inv) { x.font = `700 ${size}px "Courier Prime"`; const w = x.measureText(txt).width; x.fillStyle = col || '#a6e8b8'; x.fillRect(80, y - size * 0.86, w + 60, size * 1.12); x.fillStyle = '#07100b'; x.fillText(txt, 110, y); }
    else { x.fillStyle = col || '#a6e8b8'; x.font = `700 ${size}px "Courier Prime"`; x.fillText(txt, 110, y); }
    y += size * 1.32;
  }
  if (opt.reticle) { x.strokeStyle = 'rgba(166,232,184,0.3)'; x.lineWidth = 4; x.beginPath(); x.arc(980, 700, 120, 0, 7); x.stroke(); x.beginPath(); x.arc(980, 700, 70, 0, 7); x.stroke(); x.beginPath(); x.moveTo(840, 700); x.lineTo(1120, 700); x.moveTo(980, 560); x.lineTo(980, 840); x.stroke(); }
  for (let yy = 0; yy < H; yy += 4) { x.fillStyle = 'rgba(0,0,0,0.28)'; x.fillRect(0, yy, W, 2); }
  const g = x.createRadialGradient(W / 2, H / 2, H * 0.2, W / 2, H / 2, H * 0.85); g.addColorStop(0, 'rgba(0,0,0,0)'); g.addColorStop(1, 'rgba(0,0,0,0.6)');
  x.fillStyle = g; x.fillRect(0, 0, W, H);
  return tex(c);
}

// ---------- teleprinter: hanging front sheet (faces the operator) — newest message on top, large type
export function telexHang(lines, opt = {}) {
  const W = 680, H = 500; const [c, x] = canvas(W, H);
  x.fillStyle = '#efe4bc'; x.fillRect(0, 0, W, H); noise(x, W, H, 7);
  x.fillStyle = 'rgba(120,100,60,0.35)'; for (let y = 20; y < H; y += 36) { x.beginPath(); x.arc(20, y, 7, 0, 7); x.fill(); x.beginPath(); x.arc(W - 20, y, 7, 0, 7); x.fill(); }
  const [code, status, msg] = lines[0];
  x.textAlign = 'left'; x.textBaseline = 'alphabetic';
  x.fillStyle = '#463F34'; x.font = '700 50px "Courier Prime"'; x.fillText(code, 48, 76);
  x.fillStyle = status.red ? '#9A1E14' : INK; textFit(x, status.t, 48, 150, W - 96, '700 {s} "Courier Prime"', 66);
  x.fillStyle = INK; x.font = '400 50px "Courier Prime"';
  const words = (msg || '').split(' '); let line = '', y = 222;
  for (const w of words) { const t = line ? line + ' ' + w : w; if (x.measureText(t).width > W - 96) { x.fillText(line, 48, y); y += 60; line = w; } else line = t; }
  if (line) { x.fillText(line, 48, y); y += 60; }
  if (lines[1]) { x.strokeStyle = 'rgba(30,27,22,0.35)'; x.setLineDash([10, 8]); x.lineWidth = 3; x.beginPath(); x.moveTo(40, y - 20); x.lineTo(W - 40, y - 20); x.stroke(); x.setLineDash([]);
    x.fillStyle = '#5a5246'; x.font = '700 34px "Courier Prime"'; x.fillText(lines[1][0] + '  ' + lines[1][1].t, 48, y + 26); }
  return tex(c);
}

// ---------- room card for the section model (baked for K1), 0.19 x 0.06 m
export function roomCard(name, line2, stamp) {
  const W = 760, H = 240; const [c, x] = canvas(W, H);
  x.fillStyle = '#fbf7ec'; x.fillRect(0, 0, W, H);
  x.fillStyle = '#d2483f'; x.fillRect(0, 104, W, 5);
  x.fillStyle = INK; x.textAlign = 'left'; x.textBaseline = 'alphabetic';
  textFit(x, name, 28, 86, W - 56, '700 {s} "Courier Prime"', 84);
  x.fillStyle = '#2E2820'; textFit(x, line2, 28, 196, W - (stamp ? 330 : 56), '700 {s} "Courier Prime"', 68);
  if (stamp) { x.save(); x.translate(W - 150, 168); x.rotate(-0.07); x.strokeStyle = '#9A1E14'; x.lineWidth = 8; x.font = '700 50px "Courier Prime"'; const w = x.measureText(stamp).width + 36; x.strokeRect(-w / 2, -36, w, 72); x.fillStyle = '#9A1E14'; x.textAlign = 'center'; x.textBaseline = 'middle'; x.fillText(stamp, 0, 2); x.restore(); }
  return tex(c);
}

// ---------- radio face: speaker cloth, dial scale K-07, signal meter, two knobs labels
export function radioFace(lang) {
  const W = 1200, H = 800; const [c, x] = canvas(W, H);
  x.fillStyle = '#4a3020'; x.fillRect(0, 0, W, H); noise(x, W, H, 10);
  // speaker cloth
  x.fillStyle = '#2b1d14'; x.fillRect(60, 80, 460, 520);
  x.strokeStyle = 'rgba(120,90,60,0.35)'; x.lineWidth = 2; for (let i = 0; i < 70; i++) { x.beginPath(); x.moveTo(60 + i * 7, 80); x.lineTo(60 + i * 7, 600); x.stroke(); }
  x.strokeStyle = '#b8955a'; x.lineWidth = 8; x.strokeRect(60, 80, 460, 520);
  // dial window
  const g = x.createLinearGradient(0, 90, 0, 250); g.addColorStop(0, '#f0bd62'); g.addColorStop(1, '#c4802f');
  x.fillStyle = g; x.fillRect(580, 90, 560, 170); x.strokeStyle = '#b8955a'; x.lineWidth = 8; x.strokeRect(580, 90, 560, 170);
  x.fillStyle = '#3a240c'; for (let i = 0; i < 46; i++) { const xx = 604 + i * 11.6; x.fillRect(xx, 196, 3, i % 5 === 0 ? 46 : 22); }
  x.font = '700 38px "Courier Prime"'; x.textAlign = 'left'; x.textBaseline = 'alphabetic'; x.fillText('01  03  05  07  09  11', 600, 160);
  x.fillStyle = '#9A1E14'; x.fillRect(834, 100, 6, 150);
  x.fillStyle = '#2a1e10'; x.font = '700 30px "Courier Prime"'; x.fillText(lang === 'en' ? 'CHANNEL' : 'KANAL', 1010, 160);
  // signal meter
  x.fillStyle = '#efe4bc'; x.fillRect(580, 300, 300, 200); x.strokeStyle = '#b8955a'; x.lineWidth = 8; x.strokeRect(580, 300, 300, 200);
  x.strokeStyle = '#2a1e10'; x.lineWidth = 4; x.beginPath(); x.arc(730, 490, 150, Math.PI * 1.15, Math.PI * 1.85); x.stroke();
  for (let i = 0; i <= 10; i++) { const a = Math.PI * (1.15 + 0.07 * i); x.beginPath(); x.moveTo(730 + Math.cos(a) * 150, 490 + Math.sin(a) * 150); x.lineTo(730 + Math.cos(a) * (i % 5 ? 132 : 122), 490 + Math.sin(a) * (i % 5 ? 132 : 122)); x.stroke(); }
  x.strokeStyle = '#9A1E14'; x.lineWidth = 5; const na = Math.PI * 1.36; x.beginPath(); x.moveTo(730, 490); x.lineTo(730 + Math.cos(na) * 140, 490 + Math.sin(na) * 140); x.stroke();
  x.fillStyle = '#2a1e10'; x.font = '700 30px "Courier Prime"'; x.textAlign = 'center'; x.fillText(lang === 'en' ? 'SIGNAL' : 'SİNYAL', 730, 470);
  // knob labels
  x.fillStyle = '#efe4bc'; x.font = '700 30px "Courier Prime"'; x.fillText(lang === 'en' ? 'VOLUME' : 'SES', 960, 740); x.fillText(lang === 'en' ? 'TUNE' : 'AYAR', 1100, 740);
  // brass plate
  const pg = x.createLinearGradient(0, 640, 0, 720); pg.addColorStop(0, '#dcbd7d'); pg.addColorStop(1, '#8c6f3e'); x.fillStyle = pg; x.fillRect(60, 650, 460, 70);
  x.fillStyle = '#2a1e10'; x.font = '700 40px "Courier Prime"'; x.fillText(S2[lang].radio, 290, 698);
  return tex(c);
}

// ---------- radio log book (A5, lined) with entries
export function logBook(lang, entries) {
  const W = 900, H = 640; const [c, x] = canvas(W, H);
  x.fillStyle = '#f2ead2'; x.fillRect(0, 0, W, H); noise(x, W, H, 8);
  x.fillStyle = '#7a8fb5'; for (let y = 150; y < H; y += 62) x.fillRect(0, y, W, 2);
  x.fillStyle = '#d2483f'; x.fillRect(150, 0, 3, H);
  x.fillStyle = INK; x.font = '700 46px "Courier Prime"'; x.textAlign = 'left'; x.textBaseline = 'alphabetic'; x.fillText(lang === 'en' ? 'RADIO LOG · K-07' : 'TELSİZ KAYDI · K-07', 40, 76);
  entries.forEach(([t, m], i) => { const y = 140 + i * 62; x.fillStyle = '#463F34'; x.font = '700 36px "Courier Prime"'; x.fillText(t, 22, y); x.fillStyle = '#1A2F5A'; x.font = '700 40px "Kalam"'; x.fillText(m, 170, y + 2); });
  return tex(c);
}

// ---------- chalk tally (shift count) on concrete (transparent)
export function tally(n, label) {
  const W = 1200, H = 300; const [c, x] = canvas(W, H);
  x.clearRect(0, 0, W, H);
  x.strokeStyle = 'rgba(230,226,212,0.82)'; x.lineCap = 'round'; x.lineWidth = 9;
  let gx = 30;
  for (let i = 0; i < n; i++) {
    const g = Math.floor(i / 5), k = i % 5;
    if (k < 4) { const xx = 40 + g * 150 + k * 26; x.beginPath(); x.moveTo(xx + (i % 3) - 1, 110); x.lineTo(xx + 3 + (i % 2), 230); x.stroke(); }
    else { const xx = 40 + g * 150; x.beginPath(); x.moveTo(xx - 12, 210); x.lineTo(xx + 96, 120); x.stroke(); }
  }
  if (label) { x.fillStyle = 'rgba(230,226,212,0.82)'; x.font = '700 64px "Kalam"'; x.textAlign = 'left'; x.fillText(label, 40, 84); }
  return tex(c);
}

// ---------- generic paper with typed lines (used for small world papers)
export function paperLines(lines, w = 800, h = 600, opt = {}) {
  const [c, x] = canvas(w, h); x.fillStyle = opt.bg || '#f3ecd8'; x.fillRect(0, 0, w, h); noise(x, w, h, 7);
  let y = opt.top || 80; for (const [t, size, col, font] of lines) { x.fillStyle = col || INK; x.font = `${font || '700'} ${size}px "Courier Prime"`; x.textAlign = 'left'; x.textBaseline = 'alphabetic'; x.fillText(t, 40, y); y += size * 1.35; }
  return tex(c);
}

// ---------- hazard stripes (yellow/black)
export function hazard(w = 1024, h = 128) {
  const [c, x] = canvas(w, h); x.fillStyle = '#F5D646'; x.fillRect(0, 0, w, h); x.fillStyle = '#1E1B16';
  for (let i = -h; i < w + h; i += 96) { x.beginPath(); x.moveTo(i, h); x.lineTo(i + 48, h); x.lineTo(i + 48 + h, 0); x.lineTo(i + h, 0); x.fill(); }
  noise(x, w, h, 14); return tex(c);
}
