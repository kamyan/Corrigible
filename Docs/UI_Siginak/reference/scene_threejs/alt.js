// Alternative props for the three comparison boards (Karar 1–3).
import { canvas, tex, noise, rnd } from './lib.js';

const INK = '#1A2F5A', PENCIL = '#4A4A48', PRINT = '#463F34', RED = '#9A1E14';

function paper(x, w, h, base = '#efe6d0') {
  x.fillStyle = base; x.fillRect(0, 0, w, h);
  noise(x, w, h, 10);
}

// Karar 1 · B — open ledger on a lectern: left page history, right page today's values.
export function ledgerTex(lang) {
  const W = 1240, H = 400; const [c, x] = canvas(W, H);
  paper(x, W, H);
  // gutter shading
  const g = x.createLinearGradient(W / 2 - 60, 0, W / 2 + 60, 0);
  g.addColorStop(0, 'rgba(60,45,25,0)'); g.addColorStop(0.5, 'rgba(60,45,25,0.35)'); g.addColorStop(1, 'rgba(60,45,25,0)');
  x.fillStyle = g; x.fillRect(W / 2 - 60, 0, 120, H);
  // ruled lines
  x.strokeStyle = 'rgba(70,90,130,0.28)'; x.lineWidth = 1.5;
  for (let y = 86; y < H - 10; y += 52) { x.beginPath(); x.moveTo(24, y); x.lineTo(W / 2 - 24, y); x.moveTo(W / 2 + 24, y); x.lineTo(W - 24, y); x.stroke(); }
  x.strokeStyle = 'rgba(154,30,20,0.35)'; x.beginPath(); x.moveTo(110, 20); x.lineTo(110, H - 10); x.stroke();
  // printed header (left)
  const cols = lang === 'en' ? ['SHIFT', 'FUNDS', 'RATIONS', 'MAT.', 'FREE'] : ['VARD.', 'FON', 'ERZAK', 'MALZ.', 'ÖZGÜR'];
  const cx = [40, 150, 270, 400, 500];
  x.fillStyle = PRINT; x.font = '700 22px "Courier Prime"';
  cols.forEach((t, i) => x.fillText(t, cx[i], 60));
  const rows = [['011', '44,5', '29/40', '17', '1/12'], ['012', '47,0', '30/40', '18', '1/12'], ['013', '59,5', '30/40', '19', '1/12'], ['014', '62,0', '31/40', '19', '1/12']];
  rows.forEach((r, j) => {
    x.fillStyle = j === 3 ? INK : PENCIL;
    x.font = `700 ${j === 3 ? 30 : 28}px "Kalam"`;
    r.forEach((t, i) => x.fillText(t, cx[i], 128 + j * 52));
  });
  // a struck-out correction
  x.strokeStyle = PENCIL; x.lineWidth = 2.5; x.beginPath(); x.moveTo(146, 172); x.lineTo(226, 162); x.stroke();
  // right page: today's values, written large
  x.fillStyle = PRINT; x.font = '700 22px "Courier Prime"';
  x.fillText(lang === 'en' ? 'TODAY · SHIFT 014' : 'BUGÜN · VARDİYA 014', W / 2 + 40, 60);
  const big = lang === 'en' ? [['FUNDS', '62,0'], ['RATIONS', '31 / 40'], ['MATERIALS', '19'], ['FREE', '1 / 12']] : [['FON', '62,0'], ['ERZAK', '31 / 40'], ['MALZEME', '19'], ['ÖZGÜR', '1 / 12']];
  big.forEach(([k, v], j) => {
    x.fillStyle = PRINT; x.font = '700 22px "Courier Prime"'; x.fillText(k, W / 2 + 40, 128 + j * 52);
    x.fillStyle = INK; x.font = '700 40px "Kalam"'; x.fillText(v, W / 2 + 250, 132 + j * 52);
  });
  return tex(c);
}

// Karar 2 · B — shift log page awaiting the commander's signature.
export function logPageTex(lang) {
  const W = 800, H = 550; const [c, x] = canvas(W, H);
  paper(x, W, H, '#f1e9d6');
  const g = x.createLinearGradient(W / 2 - 40, 0, W / 2 + 40, 0);
  g.addColorStop(0, 'rgba(60,45,25,0)'); g.addColorStop(0.5, 'rgba(60,45,25,0.32)'); g.addColorStop(1, 'rgba(60,45,25,0)');
  x.fillStyle = g; x.fillRect(W / 2 - 40, 0, 80, H);
  x.strokeStyle = 'rgba(70,90,130,0.28)'; x.lineWidth = 1.5;
  for (let y = 96; y < H - 10; y += 64) { x.beginPath(); x.moveTo(20, y); x.lineTo(W / 2 - 20, y); x.moveTo(W / 2 + 20, y); x.lineTo(W - 20, y); x.stroke(); }
  x.fillStyle = PRINT; x.font = '700 24px "Courier Prime"';
  x.fillText(lang === 'en' ? 'SHIFT LOG · KYGA/07' : 'VARDİYA DEFTERİ · KYGA/07', 28, 52);
  const sig = (sx, sy) => { x.strokeStyle = INK; x.lineWidth = 3; x.beginPath(); x.moveTo(sx, sy); for (let i = 0; i < 7; i++) x.quadraticCurveTo(sx + 12 + i * 16, sy - 18 + (i % 2) * 30, sx + 20 + i * 16, sy); x.stroke(); };
  const stamp = (sx, sy, rot) => { x.save(); x.translate(sx, sy); x.rotate(rot); x.strokeStyle = 'rgba(154,30,20,0.85)'; x.lineWidth = 3.5; x.strokeRect(-62, -20, 124, 40); x.fillStyle = 'rgba(154,30,20,0.85)'; x.font = '700 20px "Courier Prime"'; x.textAlign = 'center'; x.fillText(lang === 'en' ? 'CLOSED' : 'KAPANDI', 0, 7); x.restore(); x.textAlign = 'left'; };
  [['012', 136], ['013', 200]].forEach(([n, y]) => {
    x.fillStyle = PENCIL; x.font = '700 28px "Kalam"'; x.fillText(n, 28, y - 8);
    sig(100, y - 14); stamp(300, y - 22, -0.08 + rnd() * 0.08);
  });
  x.fillStyle = INK; x.font = '700 30px "Kalam"'; x.fillText('014', 28, 256);
  x.fillStyle = PRINT; x.font = '700 22px "Courier Prime"'; x.fillText(lang === 'en' ? 'SIGN:' : 'İMZA:', 96, 256);
  x.setLineDash([8, 6]); x.strokeStyle = PRINT; x.lineWidth = 2; x.beginPath(); x.moveTo(170, 262); x.lineTo(370, 262); x.stroke(); x.setLineDash([]);
  // right page: notes
  x.fillStyle = PENCIL; x.font = '700 26px "Kalam"';
  x.fillText(lang === 'en' ? 'Raid EE-02 failed,' : 'Baskın DA-02 başarısız,', W / 2 + 30, 128);
  x.fillText(lang === 'en' ? 'Alfa down 2 days.' : 'Alfa 2 gün yaralı.', W / 2 + 30, 192);
  return tex(c);
}

// Karar 3 · B — ozalid blueprint of the shelter laid over the desk (same plane as the map).
export function blueprintTex(lang) {
  const W = 1920, H = 1080; const [c, x] = canvas(W, H);
  x.fillStyle = '#244f86'; x.fillRect(0, 0, W, H); noise(x, W, H, 12);
  // faint grid
  x.strokeStyle = 'rgba(220,235,255,0.10)'; x.lineWidth = 1;
  for (let i = 0; i <= W; i += 40) { x.beginPath(); x.moveTo(i, 0); x.lineTo(i, H); x.stroke(); }
  for (let j = 0; j <= H; j += 40) { x.beginPath(); x.moveTo(0, j); x.lineTo(W, j); x.stroke(); }
  const L = 'rgba(236,244,255,0.92)', L2 = 'rgba(236,244,255,0.55)';
  x.strokeStyle = L; x.fillStyle = L; x.lineWidth = 3;
  // border + title block
  x.strokeRect(30, 30, W - 60, H - 60);
  x.strokeRect(W - 560, H - 190, 530, 160);
  x.font = '700 34px "Courier Prime"'; x.fillText(lang === 'en' ? 'SHELTER 07 · SECTION A–A' : 'SIĞINAK 07 · KESİT A–A', W - 540, H - 135);
  x.font = '700 24px "Courier Prime"'; x.fillText(lang === 'en' ? 'SHEET 2/4 · 1:50 · KYGA' : 'PAFTA 2/4 · 1:50 · KYGA', W - 540, H - 92);
  x.fillText(lang === 'en' ? 'SHIFT 014' : 'VARDİYA 014', W - 540, H - 56);
  // ground line + surface ruins
  const gx0 = 260, gx1 = 1500, gy = 250;
  x.beginPath(); x.moveTo(gx0 - 120, gy); x.lineTo(gx1 + 120, gy); x.stroke();
  x.lineWidth = 2;
  [[300, 90, 120], [460, 60, 80], [1240, 110, 140], [1390, 70, 70]].forEach(([bx, bw, bh]) => { x.beginPath(); x.moveTo(bx, gy); x.lineTo(bx, gy - bh); x.lineTo(bx + bw * 0.6, gy - bh + 18); x.lineTo(bx + bw, gy - bh * 0.6); x.lineTo(bx + bw, gy); x.stroke(); });
  // AI mast with red lamp
  x.beginPath(); x.moveTo(880, gy); x.lineTo(880, gy - 170); x.moveTo(860, gy); x.lineTo(880, gy - 120); x.lineTo(900, gy); x.stroke();
  x.fillStyle = '#d2483f'; x.beginPath(); x.arc(880, gy - 176, 9, 0, 7); x.fill(); x.fillStyle = L;
  x.font = '700 22px "Courier Prime"'; x.fillText(lang === 'en' ? 'SURFACE · AI MAST' : 'YÜZEY · YZ DİREĞİ', 910, gy - 150);
  // soil hatch
  x.save(); x.beginPath(); x.rect(gx0 - 120, gy, gx1 - gx0 + 240, 120); x.clip(); x.strokeStyle = L2; x.lineWidth = 1.5;
  for (let i = -200; i < 1800; i += 26) { x.beginPath(); x.moveTo(gx0 - 120 + i, gy + 120); x.lineTo(gx0 - 120 + i + 120, gy); x.stroke(); }
  x.restore();
  // rooms 2x2 + shaft
  const rx0 = 360, ry0 = 390, rw = 470, rh = 250, shaft = 90;
  const rooms = lang === 'en'
    ? [['LIVING QUARTERS', 'LV 1 · STAFF 2/2', 2, ''], ['GENERATOR', 'LV 1 · STAFF 0/2', 0, 'SEL'], ['WORKSHOP', 'LV 3 · STAFF 2/6', 2, 'NO POWER'], ['INFIRMARY', 'DAMAGED · 0/0', 0, 'DAMAGED']]
    : [['YAŞAM ALANI', 'SV 1 · PERS. 2/2', 2, ''], ['JENERATÖR', 'SV 1 · PERS. 0/2', 0, 'SEL'], ['ATÖLYE', 'SV 3 · PERS. 2/6', 2, 'ENERJİSİZ'], ['REVİR', 'HASARLI · 0/0', 0, 'HASARLI']];
  x.lineWidth = 4; x.strokeStyle = L;
  rooms.forEach(([nm, l2, ppl, st], i) => {
    const col = i % 2, row = Math.floor(i / 2);
    const X = rx0 + col * (rw + shaft), Y = ry0 + row * (rh + 40);
    x.strokeRect(X, Y, rw, rh);
    x.font = '700 30px "Courier Prime"'; x.fillStyle = L; x.fillText(nm, X + 18, Y + 42);
    x.font = '700 22px "Courier Prime"'; x.fillText(l2, X + 18, Y + 74);
    // capacity slots + people tokens
    const cap = i === 2 ? 6 : 2;
    for (let k = 0; k < cap; k++) {
      const sx = X + 40 + k * 64, sy = Y + rh - 50;
      x.strokeStyle = L2; x.lineWidth = 2; x.beginPath(); x.arc(sx, sy, 22, 0, 7); x.stroke();
      if (k < ppl) { x.fillStyle = '#efe6d0'; x.beginPath(); x.arc(sx, sy, 22, 0, 7); x.fill(); x.fillStyle = '#463F34'; x.font = '700 20px "Courier Prime"'; x.textAlign = 'center'; x.fillText(['AK', 'MT', 'ZY', 'HB'][(i + k) % 4], sx, sy + 7); x.textAlign = 'left'; }
    }
    x.lineWidth = 4; x.strokeStyle = L;
    if (st === 'SEL') { x.strokeStyle = '#f5d646'; x.lineWidth = 7; x.setLineDash([22, 10]); x.strokeRect(X - 10, Y - 10, rw + 20, rh + 20); x.setLineDash([]); x.strokeStyle = L; x.lineWidth = 4; }
    else if (st) {
      x.save(); x.translate(X + rw - 120, Y + 120); x.rotate(-0.08);
      x.fillStyle = 'rgba(239,230,208,0.9)'; x.fillRect(-100, -26, 200, 52);
      x.strokeStyle = '#9a1e14'; x.lineWidth = 4; x.strokeRect(-100, -26, 200, 52);
      x.fillStyle = '#9a1e14'; x.font = '700 26px "Courier Prime"'; x.textAlign = 'center'; x.fillText(st, 0, 9); x.restore(); x.textAlign = 'left';
      x.strokeStyle = L; x.lineWidth = 4;
    }
    if (i === 3) { x.strokeStyle = L2; x.lineWidth = 3; x.beginPath(); x.moveTo(X + 40, Y + 110); x.lineTo(X + 300, Y + 200); x.moveTo(X + 120, Y + 220); x.lineTo(X + 200, Y + 180); x.stroke(); x.strokeStyle = L; x.lineWidth = 4; }
  });
  // shaft + ladder
  const sx0 = rx0 + rw, sy0 = ry0;
  x.lineWidth = 2; x.strokeStyle = L2;
  for (let y = sy0 + 20; y < ry0 + 2 * rh + 40; y += 30) { x.beginPath(); x.moveTo(sx0 + 20, y); x.lineTo(sx0 + shaft - 20, y); x.stroke(); }
  x.beginPath(); x.moveTo(sx0 + 20, sy0); x.lineTo(sx0 + 20, ry0 + 2 * rh + 40); x.moveTo(sx0 + shaft - 20, sy0); x.lineTo(sx0 + shaft - 20, ry0 + 2 * rh + 40); x.stroke();
  // idle tokens (left margin)
  x.fillStyle = L; x.font = '700 24px "Courier Prime"'; x.fillText(lang === 'en' ? 'IDLE 4' : 'BOŞTA 4', 90, 520);
  for (let k = 0; k < 4; k++) { const ty = 560 + k * 64; x.fillStyle = '#efe6d0'; x.beginPath(); x.arc(150, ty, 22, 0, 7); x.fill(); x.fillStyle = '#463F34'; x.font = '700 20px "Courier Prime"'; x.textAlign = 'center'; x.fillText(['DE', 'NS', 'OK', 'TR'][k], 150, ty + 7); x.textAlign = 'left'; }
  // energy note
  x.fillStyle = L; x.font = '700 26px "Courier Prime"'; x.fillText(lang === 'en' ? 'POWER 4 / 6  −2' : 'ENERJİ 4 / 6  −2', 1460, 440);
  x.font = '700 24px "Courier Prime"'; x.fillText(lang === 'en' ? 'RATIONS 31/40' : 'ERZAK 31/40', 1460, 482);
  x.fillText(lang === 'en' ? 'PEOPLE 8/8' : 'TOPLULUK 8/8', 1460, 524);
  return tex(c);
}
