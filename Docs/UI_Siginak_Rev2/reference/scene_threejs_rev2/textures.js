import { canvas, tex, noise, rnd, img, textFit } from './lib.js';

export const STR = {
  tr: {
    counters: [['VARDİYA', ''], ['FON', ''], ['ERZAK', ''], ['MALZEME', ''], ['ÖZGÜR BÖLGE', '']],
    keys: ['KOMUTA', 'SIĞINAK', 'GÖREVLER', 'KILAVUZ', 'MASA'], keyCaps: ['1', '2', '3', 'H', 'Home'],
    keyHome: 'Home',
    sw1: 'VARDİYAYI BİTİR', sw2: 'Enter · kapağı kaldır',
    telexHead: 'TELEKS · KYGA-07',
    telex: [['014/06', 'EMİR UYGULANDI', 'Cephe açıldı: Doğu Avrupa'], ['014/07', 'EMİR UYGULANDI', 'Atölye → seviye 3'], ['014/08', 'YAPILAMADI', 'Revir onarımı: malzeme yetersiz (24 gerekli / 19 var)']],
    crate1: 'ERZAK', crate2: 'MALZEME',
    tray: 'EVRAK', guide: 'KILAVUZ', guideKey: 'H', telexPlate: 'TELEKS',
    crtPlate: 'SAHA YAYINI · K-07', shelfPlate: 'SIĞINAK 07 · KESİT',
  },
  en: {
    counters: [['SHIFT', ''], ['FUNDS', ''], ['RATIONS', ''], ['MATERIALS', ''], ['FREE REGIONS', '']],
    keys: ['COMMAND', 'SHELTER', 'MISSIONS', 'GUIDE', 'DESK'], keyCaps: ['1', '2', '3', 'H', 'Home'],
    keyHome: 'Home',
    sw1: 'END SHIFT', sw2: 'Enter · lift the cover',
    telexHead: 'TELEX · KYGA-07',
    telex: [['014/06', 'ORDER EXECUTED', 'Front opened: Eastern Europe'], ['014/07', 'ORDER EXECUTED', 'Workshop → level 3'], ['014/08', 'FAILED', 'Infirmary repair: not enough materials (24 needed / 19 held)']],
    crate1: 'RATIONS', crate2: 'MATERIALS',
    tray: 'PAPERS', guide: 'GUIDE', guideKey: 'H', telexPlate: 'TELEX',
    crtPlate: 'FIELD FEED · K-07', shelfPlate: 'SHELTER 07 · SECTION',
  },
};

const INK = '#1E1B16';

// ---------------- concrete (board-formed)
export function concreteTex(wM, hM, opt = {}) {
  const ppm = opt.ppm ?? 420;
  const W = Math.round(wM * ppm), H = Math.round(hM * ppm);
  const [c, x] = canvas(W, H);
  x.fillStyle = opt.base ?? '#8b8f86'; x.fillRect(0, 0, W, H);
  // board strips
  const board = 0.15 * ppm;
  for (let y = 0; y < H; y += board) {
    const v = (rnd() - 0.5) * 16;
    x.fillStyle = `rgba(${v > 0 ? 255 : 0},${v > 0 ? 255 : 0},${v > 0 ? 255 : 0},${Math.abs(v) / 255 * 1.4})`;
    x.fillRect(0, y, W, board);
    x.fillStyle = 'rgba(20,22,18,0.22)'; x.fillRect(0, y, W, 2);
    x.fillStyle = 'rgba(255,255,255,0.05)'; x.fillRect(0, y + 2, W, 1);
  }
  // tie holes grid
  if (opt.ties !== false) {
    for (let y = 0.45 * ppm; y < H; y += 0.6 * ppm) for (let xx = 0.3 * ppm; xx < W; xx += 0.6 * ppm) {
      x.fillStyle = 'rgba(25,25,22,0.55)'; x.beginPath(); x.arc(xx, y, 0.011 * ppm, 0, 7); x.fill();
      x.fillStyle = 'rgba(255,255,255,0.06)'; x.beginPath(); x.arc(xx + 1, y + 1.5, 0.011 * ppm, 0, 7); x.fill();
    }
  }
  noise(x, W, H, 26);
  // damp stains
  const stains = opt.stains ?? [];
  for (const [sx, sy, r, a] of stains) {
    const g = x.createRadialGradient(sx * W, sy * H, 0, sx * W, sy * H, r * W);
    g.addColorStop(0, `rgba(30,36,28,${a})`); g.addColorStop(0.6, `rgba(30,36,28,${a * 0.45})`); g.addColorStop(1, 'rgba(30,36,28,0)');
    x.fillStyle = g; x.fillRect(0, 0, W, H);
  }
  // floor rising damp band
  if (opt.damp) {
    const g = x.createLinearGradient(0, H, 0, H - 0.5 * ppm);
    g.addColorStop(0, 'rgba(26,30,24,0.55)'); g.addColorStop(1, 'rgba(26,30,24,0)');
    x.fillStyle = g; x.fillRect(0, 0, W, H);
  }
  // vertical streaks
  for (let i = 0; i < (opt.streaks ?? 40); i++) {
    const sx = rnd() * W, sy = rnd() * H * 0.5, len = (0.2 + rnd() * 0.8) * H * 0.5;
    const g = x.createLinearGradient(0, sy, 0, sy + len);
    g.addColorStop(0, 'rgba(28,32,26,0.18)'); g.addColorStop(1, 'rgba(28,32,26,0)');
    x.fillStyle = g; x.fillRect(sx, sy, 2 + rnd() * 5, len);
  }
  return tex(c);
}

export function linoTex() {
  const [c, x] = canvas(1024, 1024);
  x.fillStyle = '#5b7a66'; x.fillRect(0, 0, 1024, 1024);
  for (let i = 0; i < 900; i++) {
    x.fillStyle = `rgba(${rnd() > 0.5 ? '255,255,255' : '0,0,0'},${0.02 + rnd() * 0.04})`;
    x.beginPath(); x.ellipse(rnd() * 1024, rnd() * 1024, 4 + rnd() * 30, 2 + rnd() * 10, rnd() * 3, 0, 7); x.fill();
  }
  noise(x, 1024, 1024, 14);
  return tex(c, { repeat: [2, 1] });
}

// stenciled paint text on transparent background
export function stencilTex(lines, wM, hM, opt = {}) {
  const ppm = 600; const W = Math.round(wM * ppm), H = Math.round(hM * ppm);
  const [c, x] = canvas(W, H);
  x.fillStyle = opt.color ?? '#d9d2bf'; x.textAlign = 'center'; x.textBaseline = 'middle';
  const n = lines.length;
  lines.forEach((s, i) => {
    let size = opt.size ?? H / n * 0.78;
    const fit = () => { x.font = `800 ${size}px "Big Shoulders Stencil Display"`; x.letterSpacing = (opt.ls ?? 0.06) * size + 'px'; };
    fit(); while (x.measureText(s).width > W * 0.94 && size > 8) { size -= 2; fit(); }
    x.fillText(s, W / 2, H * (i + 0.5) / n);
  });
  // chipped paint: punch random holes
  x.globalCompositeOperation = 'destination-out';
  for (let i = 0; i < (opt.chips ?? 260); i++) {
    x.fillStyle = `rgba(0,0,0,${0.3 + rnd() * 0.7})`;
    x.beginPath(); x.ellipse(rnd() * W, rnd() * H, 1 + rnd() * 6, 1 + rnd() * 3, rnd() * 3, 0, 7); x.fill();
  }
  x.globalCompositeOperation = 'source-over';
  const t = tex(c); return t;
}

export function posterTex(lang) {
  const W = 880, H = 1200; const [c, x] = canvas(W, H);
  x.fillStyle = '#ead9c0'; x.fillRect(0, 0, W, H);
  x.fillStyle = '#9a1e14'; x.fillRect(36, 36, W - 72, H - 72);
  // pictogram: unplugged cable
  x.strokeStyle = '#efe2cf'; x.lineWidth = 26; x.lineCap = 'round';
  x.beginPath(); x.moveTo(170, 300); x.bezierCurveTo(260, 300, 300, 380, 360, 380); x.stroke();
  x.fillStyle = '#efe2cf'; x.fillRect(350, 340, 90, 80); x.fillRect(440, 352, 40, 14); x.fillRect(440, 394, 40, 14);
  x.beginPath(); x.moveTo(710, 300); x.bezierCurveTo(620, 300, 580, 380, 540, 380); x.stroke();
  x.fillRect(520, 340, 30, 80);
  x.lineWidth = 14; x.beginPath(); x.moveTo(470, 250); x.lineTo(500, 300); x.moveTo(500, 470); x.lineTo(470, 520); x.stroke();
  x.fillStyle = '#efe2cf'; x.textAlign = 'center';
  const L = lang === 'en' ? ["DON'T", 'CONNECT', 'STAY ANALOG'] : ['AĞA', 'BAĞLANMA', 'ANALOG KAL'];
  x.font = '700 120px "Courier Prime"'; x.fillText(L[0], W / 2, 700);
  textFit(x, L[1], W / 2, 820, W - 140, '700 {s} "Courier Prime"', 120);
  x.fillRect(120, 880, W - 240, 8);
  textFit(x, L[2], W / 2, 990, W - 160, '700 {s} "Courier Prime"', 92);
  x.font = '700 34px "Courier Prime"'; x.fillText(lang === 'en' ? 'KYGA · DOCTRINE 1' : 'KYGA · DOKTRİN 1', W / 2, 1090);
  noise(x, W, H, 18);
  // wear
  x.globalCompositeOperation = 'destination-out';
  x.beginPath(); x.moveTo(W, H); x.lineTo(W - 120, H); x.lineTo(W, H - 90); x.fill();
  x.globalCompositeOperation = 'source-over';
  return tex(c);
}

// small handwritten/typed notes for the cork board
export function noteTex(kind, lang) {
  const W = 420, H = 300; const [c, x] = canvas(W, H);
  if (kind === 'list') {
    x.fillStyle = '#f6f0e0'; x.fillRect(0, 0, W, H);
    x.strokeStyle = 'rgba(80,120,170,0.35)'; x.lineWidth = 2;
    for (let y = 70; y < H; y += 34) { x.beginPath(); x.moveTo(0, y); x.lineTo(W, y); x.stroke(); }
    x.strokeStyle = 'rgba(210,72,63,0.6)'; x.beginPath(); x.moveTo(44, 0); x.lineTo(44, H); x.stroke();
    x.fillStyle = INK; x.font = '700 30px "Courier Prime"'; x.fillText(lang === 'en' ? 'FREED REGIONS' : 'KURTARILAN BÖLGELER', 54, 48);
    x.fillStyle = '#1A4789'; x.font = '700 34px "Kalam"'; x.fillText(lang === 'en' ? '1. Western Europe' : '1. Batı Avrupa', 56, 100);
    x.font = '700 26px "Kalam"'; x.fillText('V.011 ✓', 300, 100);
    x.fillStyle = 'rgba(30,27,22,0.35)'; x.font = '700 30px "Kalam"'; x.fillText('2.', 56, 134); x.fillText('3.', 56, 168);
  } else {
    x.fillStyle = kind === 'lostB' ? '#e8e1cf' : '#efe8d6'; x.fillRect(0, 0, W, H);
    x.fillStyle = INK; x.font = '700 46px "Courier Prime"'; x.fillText(lang === 'en' ? 'MISSING' : 'KAYIP', 22, 56);
    x.strokeStyle = INK; x.lineWidth = 3; x.strokeRect(22, 76, 120, 150);
    x.beginPath(); x.arc(82, 132, 30, 0, 7); x.stroke(); x.beginPath(); x.arc(82, 230, 56, Math.PI * 1.15, Math.PI * 1.85); x.stroke();
    const names = { lostA: ['Elif Y.', 'Lyon · 2 yaş'], lostB: ['Marek D.', 'Gdańsk'], lostC: ['Ana R.', 'Porto'] }[kind] || ['—', ''];
    x.font = '700 34px "Kalam"'; x.fillText(names[0], 160, 120);
    x.font = '700 26px "Kalam"'; x.fillText(names[1], 160, 160);
    x.font = '400 22px "Courier Prime"'; x.fillText('— KYGA-07', 160, 210);
  }
  noise(x, W, H, 14);
  return tex(c);
}

// counter bank sloped face (1.40 x 0.21 m) — modules: label plate + wheel window
export function counterFaceTex(values, lang, opt = {}) {
  // face 1.56 x 0.212 m at 2000 px/m
  const W = 3120, H = 424; const [c, x] = canvas(W, H);
  x.fillStyle = '#1b130c'; x.fillRect(0, 0, W, H);
  noise(x, W, H, 8);
  const S = STR[lang];
  const modW = 592, gap = 24, x0 = 32;
  // common label size
  // the top ~39 px of this face sits inside the bakelite back box, so the label plate starts below it
  x.font = '700 72px "Courier Prime"';
  let size = 72;
  for (const [lab] of S.counters) { let s2 = 72; x.font = `700 ${s2}px "Courier Prime"`; while (x.measureText(lab).width > modW - 120 && s2 > 20) { s2--; x.font = `700 ${s2}px "Courier Prime"`; } size = Math.min(size, s2); }
  values.forEach((v, i) => {
    const mx = x0 + i * (modW + gap);
    const pg = x.createLinearGradient(0, 44, 0, 138);
    pg.addColorStop(0, '#dcbd7d'); pg.addColorStop(0.5, '#b8955a'); pg.addColorStop(1, '#8c6f3e');
    x.fillStyle = pg; x.fillRect(mx + 8, 44, modW - 16, 94);
    x.strokeStyle = 'rgba(40,28,10,0.6)'; x.lineWidth = 3; x.strokeRect(mx + 9.5, 45.5, modW - 19, 91);
    for (const sx of [mx + 30, mx + modW - 30]) { x.fillStyle = '#6e5a33'; x.beginPath(); x.arc(sx, 91, 9, 0, 7); x.fill(); x.strokeStyle = '#3a2c14'; x.lineWidth = 2; x.beginPath(); x.moveTo(sx - 6, 91); x.lineTo(sx + 6, 91); x.stroke(); }
    x.fillStyle = '#2a1e10'; x.textAlign = 'center'; x.textBaseline = 'middle';
    x.font = `700 ${size}px "Courier Prime"`; x.fillText(S.counters[i][0], mx + modW / 2, 93);
    // window
    x.fillStyle = '#8c6f3e'; x.fillRect(mx + 22, 146, modW - 44, 264);
    x.fillStyle = '#0e0906'; x.fillRect(mx + 32, 156, modW - 64, 244);
    const digits = v.digits; const frac = v.frac; const nW = digits.length + (frac != null ? 1 : 0);
    const wheelW = 96, wheelGap = 8;
    const fixed = v.fixed || '';
    x.font = '700 82px "Courier Prime"';
    const fixedW = fixed ? x.measureText(fixed).width + 16 : 0;
    const totalW = nW * wheelW + (nW - 1) * wheelGap + fixedW;
    let wx = mx + modW / 2 - totalW / 2;
    const top = 166, hh = 224, mid = top + hh / 2;
    const drawWheel = (ch, red, roll) => {
      const g = x.createLinearGradient(0, top, 0, top + hh);
      const base = red ? [128, 24, 16] : [28, 21, 14];
      g.addColorStop(0, `rgb(${base.map(b => b * 0.3).join(',')})`);
      g.addColorStop(0.5, `rgb(${base.join(',')})`);
      g.addColorStop(1, `rgb(${base.map(b => b * 0.3).join(',')})`);
      x.fillStyle = g; x.fillRect(wx, top, wheelW, hh);
      x.save(); x.beginPath(); x.rect(wx, top, wheelW, hh); x.clip();
      x.fillStyle = '#efdcae'; x.font = '700 176px "Courier Prime"'; x.textAlign = 'center'; x.textBaseline = 'middle';
      if (roll != null) { x.fillText(ch[0], wx + wheelW / 2, mid + 8 - roll * 200); x.fillText(ch[1], wx + wheelW / 2, mid + 8 - roll * 200 + 200); }
      else x.fillText(ch, wx + wheelW / 2, mid + 8);
      const s = x.createLinearGradient(0, top, 0, top + hh);
      s.addColorStop(0, 'rgba(0,0,0,0.7)'); s.addColorStop(0.26, 'rgba(0,0,0,0)'); s.addColorStop(0.74, 'rgba(0,0,0,0)'); s.addColorStop(1, 'rgba(0,0,0,0.72)');
      x.fillStyle = s; x.fillRect(wx, top, wheelW, hh);
      x.restore();
      wx += wheelW + wheelGap;
    };
    for (let k = 0; k < digits.length; k++) {
      const roll = v.roll && v.roll[k] != null ? v.roll[k] : null;
      drawWheel(roll != null ? v.rollPair[k] : digits[k], false, roll);
    }
    if (frac != null) { x.fillStyle = '#c9a86a'; x.beginPath(); x.arc(wx - wheelGap / 2, top + hh - 22, 10, 0, 7); x.fill(); drawWheel(String(frac), true, null); }
    if (fixed) { x.fillStyle = '#c9a86a'; x.font = '700 82px "Courier Prime"'; x.textAlign = 'left'; x.textBaseline = 'middle'; x.fillText(fixed, wx + 6, mid + 10); }
  });
  return tex(c);
}

export function plateTex(text, wM, hM, opt = {}) {
  const ppm = opt.ppm ?? 3200; const W = Math.round(wM * ppm), H = Math.round(hM * ppm);
  const [c, x] = canvas(W, H);
  const g = x.createLinearGradient(0, 0, 0, H);
  g.addColorStop(0, '#dcbd7d'); g.addColorStop(0.5, '#b8955a'); g.addColorStop(1, '#8c6f3e');
  x.fillStyle = g; x.fillRect(0, 0, W, H);
  x.strokeStyle = 'rgba(40,28,10,0.55)'; x.lineWidth = Math.max(2, H * 0.04); x.strokeRect(x.lineWidth / 2, x.lineWidth / 2, W - x.lineWidth, H - x.lineWidth);
  x.fillStyle = '#2a1e10'; x.textBaseline = 'middle';
  const lines = Array.isArray(text) ? text : [text];
  const pad = H * 0.16;
  if (opt.key) {
    // key badge at left
    const kw = H * 0.9;
    x.fillStyle = '#2a1e10'; x.fillRect(pad * 0.6, H * 0.12, kw * (opt.keyW ?? 1), H * 0.76);
    x.fillStyle = '#dcbd7d'; x.textAlign = 'center';
    textFit(x, opt.key, pad * 0.6 + kw * (opt.keyW ?? 1) / 2, H * 0.52, kw * (opt.keyW ?? 1) * 0.86, '700 {s} "Courier Prime"', H * 0.5);
    x.fillStyle = '#2a1e10'; x.textAlign = 'left';
    textFit(x, lines[0], pad * 0.6 + kw * (opt.keyW ?? 1) + pad * 0.6, H * 0.53, W - (pad * 1.8 + kw * (opt.keyW ?? 1)), '700 {s} "Courier Prime"', H * 0.5);
  } else {
    x.textAlign = 'center';
    if (lines.length === 1) textFit(x, lines[0], W / 2, H * 0.53, W - pad * 2, '700 {s} "Courier Prime"', H * 0.58);
    else {
      textFit(x, lines[0], W / 2, H * 0.36, W - pad * 2, '700 {s} "Courier Prime"', H * 0.42);
      textFit(x, lines[1], W / 2, H * 0.74, W - pad * 2, '700 {s} "Courier Prime"', H * 0.24);
    }
  }
  return tex(c);
}

export function keyCapTex(ch) {
  const [c, x] = canvas(256, 256);
  x.fillStyle = '#2a1d14'; x.fillRect(0, 0, 256, 256);
  const g = x.createRadialGradient(110, 100, 10, 128, 128, 180); g.addColorStop(0, 'rgba(255,255,255,0.10)'); g.addColorStop(1, 'rgba(0,0,0,0.25)');
  x.fillStyle = g; x.fillRect(0, 0, 256, 256);
  x.fillStyle = '#ecd7a6'; x.textAlign = 'center'; x.textBaseline = 'middle';
  textFit(x, ch, 128, 136, 220, '700 {s} "Courier Prime"', ch.length > 1 ? 92 : 170);
  return tex(c);
}

export function telexTex(lang, lines, opt = {}) {
  const W = 800, H = opt.h ?? 1100; const [c, x] = canvas(W, H);
  x.fillStyle = '#efe4bc'; x.fillRect(0, 0, W, H);
  // feed holes
  for (let y = 12; y < H; y += 36) { x.fillStyle = 'rgba(70,60,40,0.34)'; x.beginPath(); x.arc(22, y, 8, 0, 7); x.fill(); x.beginPath(); x.arc(W - 22, y, 8, 0, 7); x.fill(); }
  x.strokeStyle = 'rgba(110,90,51,0.45)'; x.setLineDash([10, 8]); x.lineWidth = 2;
  x.beginPath(); x.moveTo(46, 0); x.lineTo(46, H); x.moveTo(W - 46, 0); x.lineTo(W - 46, H); x.stroke(); x.setLineDash([]);
  x.textBaseline = 'alphabetic';
  let y = opt.top ?? 80;
  const S = STR[lang];
  x.fillStyle = '#675F50'; x.font = '700 30px "Courier Prime"'; x.fillText(S.telexHead, 70, y); y += 60;
  for (const [no, head, body] of lines) {
    const fail = /YAPILAMADI|FAILED/.test(head);
    x.fillStyle = '#675F50'; x.font = '700 28px "Courier Prime"'; x.fillText(no, 70, y);
    x.fillStyle = fail ? '#9A1E14' : '#1E1B16'; x.font = '700 34px "Courier Prime"'; x.fillText(head, 190, y);
    y += 44;
    x.fillStyle = '#1E1B16'; x.font = '400 30px "Courier Prime"';
    // wrap body
    const words = body.split(' '); let line = '';
    for (const w of words) { const t = line ? line + ' ' + w : w; if (x.measureText(t).width > W - 140) { x.fillText(line, 70, y); y += 38; line = w; } else line = t; }
    if (line) { x.fillText(line, 70, y); y += 38; }
    y += 26;
    x.strokeStyle = 'rgba(70,63,52,0.35)'; x.setLineDash([4, 6]); x.beginPath(); x.moveTo(70, y - 18); x.lineTo(W - 70, y - 18); x.stroke(); x.setLineDash([]);
  }
  noise(x, W, H, 10);
  return tex(c);
}

export function crateTex(label) {
  const [c, x] = canvas(512, 320);
  x.fillStyle = '#7a5a36'; x.fillRect(0, 0, 512, 320);
  for (let y = 0; y < 320; y += 80) { x.fillStyle = 'rgba(0,0,0,0.25)'; x.fillRect(0, y, 512, 3); }
  noise(x, 512, 320, 24);
  x.fillStyle = 'rgba(232,224,200,0.82)'; x.textAlign = 'center'; x.textBaseline = 'middle';
  x.font = '800 96px "Big Shoulders Stencil Display"'; x.letterSpacing = '6px'; x.fillText(label, 256, 170);
  return tex(c);
}

export function firstAidTex() {
  const [c, x] = canvas(128, 128);
  x.fillStyle = '#1f7a45'; x.fillRect(0, 0, 128, 128);
  x.fillStyle = '#f4f1e8'; x.fillRect(48, 20, 32, 88); x.fillRect(20, 48, 88, 32);
  return tex(c);
}

export function keyboardTex() {
  const [c, x] = canvas(840, 300);
  x.fillStyle = '#3c3a30'; x.fillRect(0, 0, 840, 300);
  for (let r = 0; r < 5; r++) for (let k = 0; k < 14; k++) {
    const kx = 24 + k * 57 + (r % 2) * 14, ky = 22 + r * 54;
    if (kx > 800) continue;
    x.fillStyle = '#b8b29a'; x.fillRect(kx, ky, 48, 44);
    x.fillStyle = 'rgba(0,0,0,0.25)'; x.fillRect(kx, ky + 38, 48, 6);
  }
  return tex(c);
}

export function crtTex(lines, opt = {}) {
  const W = 1200, H = 900; const [c, x] = canvas(W, H);
  x.fillStyle = '#07100b'; x.fillRect(0, 0, W, H);
  x.fillStyle = '#a6e8b8'; x.textBaseline = 'alphabetic';
  let y = 150;
  for (const [txt, size, col] of lines) {
    x.fillStyle = col || '#a6e8b8'; x.font = `700 ${size}px "Courier Prime"`; x.fillText(txt, 110, y); y += size * 1.45;
  }
  if (opt.testCard) {
    x.strokeStyle = 'rgba(166,232,184,0.28)'; x.lineWidth = 4;
    x.beginPath(); x.arc(930, 610, 170, 0, 7); x.stroke(); x.beginPath(); x.arc(930, 610, 110, 0, 7); x.stroke();
    x.beginPath(); x.moveTo(730, 610); x.lineTo(1130, 610); x.moveTo(930, 410); x.lineTo(930, 810); x.stroke();
    x.fillStyle = 'rgba(166,232,184,0.35)'; x.font = '700 40px "Courier Prime"'; x.fillText('K-07', 1010, 820);
  }
  for (let yy = 0; yy < H; yy += 4) { x.fillStyle = 'rgba(0,0,0,0.28)'; x.fillRect(0, yy, W, 2); }
  const g = x.createRadialGradient(W / 2, H / 2, H * 0.2, W / 2, H / 2, H * 0.85);
  g.addColorStop(0, 'rgba(0,0,0,0)'); g.addColorStop(1, 'rgba(0,0,0,0.65)');
  x.fillStyle = g; x.fillRect(0, 0, W, H);
  return tex(c);
}

export function diaCardTex(name, line2, stamp) {
  const W = 680, H = 200; const [c, x] = canvas(W, H);
  x.fillStyle = '#fbf7ec'; x.fillRect(0, 0, W, H);
  x.fillStyle = '#d2483f'; x.fillRect(0, 62, W, 3);
  x.fillStyle = INK; x.textBaseline = 'alphabetic';
  textFit(x, name, 24, 50, W - 48, '700 {s} "Courier Prime"', 46);
  x.fillStyle = '#463F34'; textFit(x, line2, 24, 128, W - 48, '700 {s} "Courier Prime"', 36);
  if (stamp) {
    x.save(); x.translate(W - 150, 150); x.rotate(-0.08);
    x.strokeStyle = '#9A1E14'; x.lineWidth = 6; x.strokeRect(-130, -32, 260, 60);
    x.fillStyle = '#9A1E14'; x.textAlign = 'center'; x.font = '700 38px "Courier Prime"'; x.fillText(stamp, 0, 12); x.restore();
  }
  return tex(c);
}

export function radioDialTex() {
  const [c, x] = canvas(512, 96);
  const g = x.createLinearGradient(0, 0, 0, 96); g.addColorStop(0, '#e9a948'); g.addColorStop(1, '#b8742a');
  x.fillStyle = g; x.fillRect(0, 0, 512, 96);
  x.fillStyle = '#3a240c';
  for (let i = 0; i < 40; i++) { const xx = 20 + i * 12; x.fillRect(xx, 60, 2, i % 5 === 0 ? 28 : 14); }
  x.font = '700 22px "Courier Prime"'; x.fillText('MW  540  700  900  1200  1600', 18, 36);
  x.fillStyle = '#9a1e14'; x.fillRect(300, 8, 4, 84);
  return tex(c);
}

// team figure plate: cream disc with an ink ring and the team letter (seen from above)
export function teamPlateTex(letter) {
  const [c, x] = canvas(256, 256);
  x.fillStyle = '#24180f'; x.fillRect(0, 0, 256, 256);
  noise(x, 256, 256, 6);
  x.strokeStyle = '#c9a86a'; x.lineWidth = 12; x.beginPath(); x.arc(128, 128, 110, 0, 7); x.stroke();
  x.fillStyle = '#efdcae'; x.textAlign = 'center'; x.textBaseline = 'middle'; x.font = '700 172px "Courier Prime"';
  x.fillText(letter, 128, 140);
  return tex(c);
}
