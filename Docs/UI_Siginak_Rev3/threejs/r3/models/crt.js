// family: crt · Rev3 model library (see DESIGN_R3.md §3.6) — owner: room-shelf
// CRT table group: table origin = world (1.50, 0, 1.72), yaw 35 (Rev2 room.js `cd`). Items on the table get WORLD placements
// computed from table-local positions (both are written in the note). Fronts face −z. Every word is a TMP slot.
import * as THREE from 'three';
import { Model, gExtrude, gSphere, gRing, gTorus, roundedRect, prep, D2R } from '../mlib.js';
import { defMat } from '../mats.js';
import { gLoft, gSweep, segCyl, segBox, tubeU, addU, wp, screwHead, dirEuler, V } from './shelf.js';

defMat('crtVent', { hex: '#3A3F35', rough: 0.7, metal: 0.05, tex: 'tx_vent (yatay yarık dizisi, alfa yok, 256 px)', note: 'CRT havalandırma paneli (yarıklar dokuda)' });
defMat('micGrille', { hex: '#7A7E7B', rough: 0.45, metal: 0.7, tex: 'tx_micgrille (ince delikli ızgara, 128 px)', note: 'mikrofon ızgarası' });

export const CRTT = { base: [1.50, 0, 1.72], yaw: 35, top: 0.76 };
const place = (local, relYaw, note) => ({ pos: wp(CRTT.base, CRTT.yaw, local), yaw: CRTT.yaw + relYaw, note: `dünya; masa yereli (${local.map(v => v.toFixed(3)).join(', ')}), yaw +${relYaw}: ${note}` });
// rounded rectangle with per-corner radii [tr, tl, bl, br], centred at (cx, cy); points CCW
function rr4(w, h, r, cx = 0, cy = 0, n = 3) { const P = []; const C = [[w / 2 - r[0], h / 2 - r[0], 0, r[0]], [-w / 2 + r[1], h / 2 - r[1], 90, r[1]], [-w / 2 + r[2], -h / 2 + r[2], 180, r[2]], [w / 2 - r[3], -h / 2 + r[3], 270, r[3]]];
  for (const [x, y, a0, rr] of C) for (let i = 0; i <= n; i++) { const a = (a0 + 90 * i / n) * D2R; P.push([cx + x + rr * Math.cos(a), cy + y + rr * Math.sin(a)]); } return P; }

// ---------------------------------------------------------------- CrtTable
export function CrtTable() {
  const m = new Model('CrtTable', { family: 'crt', budget: 'big', listName: 'CRT masası (çelik konsol)', origin: 'taban ortası (zemin, y = 0)',
    note: 'Çelik konsol masa: linolyum kaplı üst tabla ve önde pirinç kenar şeridi, sağda iki çekmeceli ayak (kulp, etiket çerçevesi, girintili süpürgelik), solda yan panel, takviye kirişli arka panel. Sağ arkada telsiz için yükseltilmiş çelik raf; CRT ile raf arasında lastik kablo deliği.' });
  const W = 0.95, D = 0.58, T = CRTT.top;
  m.box(W, 0.028, D, 0.006, 'paintOlive', [0, T - 0.016, 0]);
  m.box(W - 0.03, 0.002, D - 0.03, 0.0008, 'linoDesk', [0, T - 0.001, 0.0], [0, 0, 0], 'Body', { cast: false });
  m.box(W - 0.02, 0.012, 0.004, 0.0015, 'brassDark', [0, T - 0.016, -D / 2 - 0.002], [0, 0, 0], 'Body', { cast: false });
  // right pedestal with two drawers
  const px = 0.2825, pw = 0.365, pd = 0.54;
  m.box(pw, 0.67, pd, 0.006, 'paintOlive', [px, 0.395, 0]);
  m.box(pw - 0.03, 0.06, pd - 0.05, 0.003, 'paintBlack', [px, 0.03, 0.01], [0, 0, 0], 'Body', { skip: ['-y'] });
  for (const [yc, h] of [[0.645, 0.15], [0.315, 0.49]]) {
    m.box(pw - 0.02, h, 0.008, 0.004, 'paintOlive', [px, yc, -pd / 2 - 0.004]);
    const hy = yc + h / 2 - 0.045;
    m.box(0.11, 0.012, 0.012, 0.004, 'steelBare', [px, hy, -pd / 2 - 0.027]);
    for (const sx of [-1, 1]) m.box(0.012, 0.012, 0.02, 0.003, 'steelBare', [px + sx * 0.045, hy, -pd / 2 - 0.016], [0, 0, 0], 'Body', { cast: false });
    m.box(0.056, 0.026, 0.003, 0.001, 'brassWorn', [px, hy + 0.03, -pd / 2 - 0.0095], [0, 0, 0], 'Body', { cast: false });
    m.box(0.048, 0.018, 0.001, 0, 'cardBlank', [px, hy + 0.03, -pd / 2 - 0.0112], [0, 0, 0], 'Body', { cast: false });
  }
  // left end panel + glides, modesty panel with bead, front apron
  m.box(0.03, 0.70, pd, 0.006, 'paintOlive', [-0.46, 0.38, 0]);
  for (const sz of [-1, 1]) m.box(0.03, 0.03, 0.04, 0.004, 'rubber', [-0.46, 0.015, sz * 0.23], [0, 0, 0], 'Body', { skip: ['-y'], cast: false });
  m.box(0.545, 0.40, 0.012, 0.004, 'paintOlive', [-0.1725, 0.50, 0.25]);
  m.box(0.52, 0.014, 0.008, 0.003, 'paintOlive', [-0.1725, 0.50, 0.24], [0, 0, 0], 'Body', { cast: false });
  m.box(0.545, 0.036, 0.018, 0.004, 'paintOlive', [-0.1725, T - 0.048, -0.255]);
  // raised radio shelf at the rear right
  const rx = 0.235, rw = 0.47, rz = 0.155, rd = 0.27, rh = 0.11;
  m.box(rw, 0.012, rd, 0.004, 'paintOlive', [rx, T + rh - 0.006, rz]);
  for (const sx of [-1, 1]) m.box(0.012, rh - 0.012, rd, 0.003, 'paintOlive', [rx + sx * (rw / 2 - 0.006), T + (rh - 0.012) / 2, rz]);
  m.box(rw - 0.024, rh - 0.012, 0.008, 0.002, 'paintOlive', [rx, T + (rh - 0.012) / 2, rz + rd / 2 - 0.004]);
  m.box(rw - 0.024, 0.022, 0.008, 0.003, 'paintOlive', [rx, T + rh - 0.023, rz - rd / 2 + 0.004]);
  // cable grommet between the CRT and the radio shelf
  m.add(gRing(0.03, 0.008, 0.007, 16, 0.0015), 'rubber', [-0.02, T + 0.0015, 0.215], [0, 0, 0], 'Body', { cast: false });
  m.cyl(0.0225, 0.002, 16, 0, 'paintBlack', [-0.02, T + 0.0002, 0.215], [0, 0, 0], 'Body', { cast: false });
  m.anchor('CrtSpot', { pos: [-0.24, T, 0.07], note: 'Crt orijini' });
  m.anchor('RadioSpot', { pos: [rx, T + rh, rz], note: 'Radio orijini (raf üstü y 0,87)' });
  m.anchor('MicSpot', { pos: [0.03, T, -0.17], note: 'Microphone orijini (yaw −20)' });
  m.anchor('LogSpot', { pos: [0.27, T, -0.165], note: 'LogBook orijini (yaw +8)' });
  m.meta.place = [{ pos: CRTT.base, yaw: CRTT.yaw, note: 'oda sağ arka köşesi (Rev2 room.js cd); dünya AABB x 0,94–2,06, z 1,21–2,23' }];
  return m;
}

// ---------------------------------------------------------------- Crt
export function Crt() {
  const m = new Model('Crt', { family: 'crt', budget: 'big', listName: 'CRT monitör (TELSİZ · K-07)', origin: 'taban ortası (masa yüzeyi, y = 0)',
    note: '1950’ler monitörü: yuvarlatılmış kasa, geriye daralan tüp kaputu (üstte ve yanlarda havalandırma panelleri, yarıklar dokuda), derin bezel, hafif bombeli ekran (UV 0–1, CRT dokusu buraya; crtScreen emissive). Altta iki bakalit düğme, yeşil hazır LED’i, pirinç TELSİZ · K-07 plakası. Yanlarda taşıma sapları, arkadan masanın arkasına inen kablo.' });
  const cy = 0.195; // case centre (0.01 … 0.38)
  for (const [x, z] of [[-0.17, -0.165], [0.17, -0.165], [-0.12, 0.15], [0.12, 0.15]]) m.cyl(0.013, 0.01, 10, 0.002, 'rubber', [x, 0.005, z], [0, 0, 0], 'Body', { cast: false });
  m.extrude(roundedRect(0.42, 0.37, 0.045, 4), 0.23, 0.008, 'crtCase', [0, cy, -0.085]);
  const ring = (w, h, r, yc, z) => roundedRect(w, h, r, 2).map(([x, y]) => [x, y + yc, z]);
  addU(m, gLoft([ring(0.39, 0.34, 0.04, cy, 0.026), ring(0.35, 0.30, 0.04, cy - 0.008, 0.12), ring(0.25, 0.215, 0.03, cy - 0.02, 0.215)], { caps: [false, true], crease: 50 }), 'crtCase');
  m.box(0.2, 0.003, 0.085, 0.001, 'crtVent', [0, 0.352, 0.074], [16.6, 0, 0], 'Body', { cast: false });
  for (const sx of [-1, 1]) m.box(0.003, 0.10, 0.12, 0.001, 'crtVent', [sx * 0.2115, cy + 0.055, -0.085], [0, 0, 0], 'Body', { cast: false });
  // deep bezel + curved screen (UV 0–1)
  const by = 0.218, sw = 0.296, sh = 0.232;
  m.extrude(roundedRect(0.36, 0.30, 0.03, 3), 0.032, 0.005, 'crtBezel', [0, by, -0.216], [0, 0, 0], 'Body', { holes: [roundedRect(sw, sh, 0.022, 3)] });
  const pg = new THREE.PlaneGeometry(sw, sh, 12, 9); const P = pg.attributes.position;
  for (let i = 0; i < P.count; i++) { const u = P.getX(i) / (sw / 2), v = P.getY(i) / (sh / 2); P.setZ(i, 0.010 * (1 - 0.5 * (u * u + v * v))); }
  pg.computeVertexNormals();
  m.part('Screen', { pivot: [0, by, -0.205], kind: 'swap', note: 'ekran: crtScreen (açık, emissive CRT dokusu) ↔ kapalı (emissive 0); UV 0–1 bombeli yama' });
  m.add(prep(pg, { keepUV: true, smooth: true }), 'crtScreen', [0, by, -0.205], [0, 0, 0], 'Screen', { cast: false });
  for (const sx of [-1, 1]) for (const sy of [-1, 1]) screwHead(m, [sx * 0.162, by + sy * 0.133, -0.232], [0, 0, -1], 0.003, 0.0018, 'steelCast', 'Body', 6);
  // controls strip: two knobs, LED, brass plate
  for (const kx of [-0.16, -0.118]) {
    m.lathe([[0, 0], [0.0135, 0], [0.0135, 0.004], [0.0115, 0.0055], [0.0105, 0.017], [0.0085, 0.019], [0, 0.0195]], 12, 'bakeliteBlack', [kx, 0.038, -0.2], [-90, 0, 0]);
    m.box(0.0028, 0.008, 0.0016, 0, 'paintCream', [kx, 0.0425, -0.2198], [0, 0, 0], 'Body', { cast: false });
  }
  m.cyl(0.0085, 0.006, 12, 0.0015, 'chrome', [0.16, 0.038, -0.202], [-90, 0, 0], 'Body', { cast: false });
  m.part('Led', { pivot: [0.16, 0.038, -0.205], kind: 'swap', note: 'hazır LED’i: crtLed (açık) ↔ jewelOff' });
  m.add(gSphere(0.0055, 10, 6, { thetaLength: Math.PI / 2 }), 'crtLed', [0.16, 0.038, -0.205], [-90, 0, 0], 'Led', { cast: false });
  m.box(0.15, 0.026, 0.002, 0.0007, 'plate', [0.012, 0.038, -0.201], [0, 0, 0], 'Body', { cast: false });
  for (const sx of [-1, 1]) screwHead(m, [0.012 + sx * 0.067, 0.038, -0.202], [0, 0, -1], 0.0025, 0.0014, 'brassWorn', 'Body', 6);
  m.text('TXT_Plate', { center: [0.012, 0.038, -0.2022], size: [0.12, 0.017], role: 'plateLabel', capMm: 10, sample: 'TELSİZ · K-07', sampleEn: 'RADIO · K-07', note: 'K5a’da büyük harf ≈ 20 px' });
  // carrying handles
  for (const sx of [-1, 1]) {
    addU(m, gSweep([[sx * 0.212, 0.30, -0.165], [sx * 0.236, 0.30, -0.15], [sx * 0.238, 0.30, -0.10], [sx * 0.236, 0.30, -0.05], [sx * 0.212, 0.30, -0.035]], 0.014, 0.011, { side: [0, 1, 0] }), 'steelBare');
    for (const z of [-0.165, -0.035]) m.box(0.008, 0.028, 0.022, 0.002, 'steelCast', [sx * 0.213, 0.30, z], [0, 0, 0], 'Body', { cast: false });
  }
  tubeU(m, [[0.07, 0.10, 0.205], [0.075, 0.07, 0.235], [0.08, 0.02, 0.262], [0.082, -0.06, 0.274]], 0.0045, 6, 9, 'rubber', 'Body', { cast: false });
  m.anchor('ScreenCenter', { pos: [0, by, -0.215], note: 'K5a hedefi (screenLocalPos (0, −0,02, 0,62) bu noktaya göre)' });
  m.light('ScreenLight', { part: 'Screen', pos: [0, by - 0.05, -0.62], type: 'point', color: '#7FD9A0', shadows: false, note: 'Rev2 crtI 0,8, menzil 2,4; ekran kapalıyken 0' });
  m.meta.place = [place([-0.24, CRTT.top, 0.07], 0, 'masanın sol-orta kısmı, kaput masanın arka kenarına kadar')];
  return m;
}

// ---------------------------------------------------------------- Radio
export function Radio() {
  const m = new Model('Radio', { family: 'crt', budget: 'big', listName: 'Ahşap telsiz (tüplü alıcı)', origin: 'taban ortası (oturduğu yüzey, y = 0)',
    note: 'Ceviz kasalı tüplü alıcı: üst kenarları yuvarlak kasa, koyu bakalit ön panel. Solda pirinç çerçeveli hoparlör bezi ve üç çıta, altında TELSİZ · K-07 plakası; sağ üstte arkadan aydınlatılan kanal skalası (rakamlar dokuda) ve kayan DialPointer; altta SİNYAL ölçeği (Needle) ile SES ve AYAR düğmeleri. Arkada teleskop anten.' });
  const W = 0.46, H = 0.30, D = 0.26, fz = -D / 2, pz = fz - 0.005;
  m.extrude(rr4(D, H, [0.032, 0.032, 0.006, 0.006], 0, H / 2, 3), W, 0.006, 'radioBody', [0, 0, 0], [0, 90, 0]);
  for (const sx of [-1, 1]) for (const sz of [-1, 1]) m.cyl(0.011, 0.008, 10, 0.002, 'rubber', [sx * 0.2, -0.002, sz * 0.1], [0, 0, 0], 'Body', { cast: false });
  m.box(0.42, 0.24, 0.005, 0.0025, 'bakelite', [0, 0.145, fz - 0.0025]);
  // speaker
  const sx0 = -0.105, sy0 = 0.162;
  m.box(0.165, 0.175, 0.002, 0, 'radioGrille', [sx0, sy0, pz - 0.001], [0, 0, 0], 'Body', { cast: false });
  for (const sy of [-1, 1]) m.box(0.177, 0.006, 0.003, 0.001, 'brassWorn', [sx0, sy0 + sy * 0.0905, pz - 0.0015], [0, 0, 0], 'Body', { cast: false });
  for (const sx of [-1, 1]) m.box(0.006, 0.187, 0.003, 0.001, 'brassWorn', [sx0 + sx * 0.0855, sy0, pz - 0.0015], [0, 0, 0], 'Body', { cast: false });
  m.add(gTorus(0.064, 0.0042, 6, 24), 'brassWorn', [sx0, sy0, pz - 0.0035], [0, 0, 0], 'Body', { cast: false });                                 // speaker ring
  m.box(0.006, 0.17, 0.003, 0.001, 'brassWorn', [sx0, sy0, pz - 0.0025], [0, 0, 0], 'Body', { cast: false });                                 // centre slat
  m.box(0.165, 0.03, 0.002, 0.0007, 'plate', [sx0, 0.048, pz - 0.001], [0, 0, 0], 'Body', { cast: false });
  for (const sx of [-1, 1]) screwHead(m, [sx0 + sx * 0.074, 0.048, pz - 0.002], [0, 0, -1], 0.0025, 0.0014, 'brassWorn', 'Body', 6);
  m.text('TXT_Plate', { center: [sx0, 0.048, pz - 0.0022], size: [0.14, 0.018], role: 'plateLabel', capMm: 10, sample: 'TELSİZ · K-07', sampleEn: 'RADIO · K-07' });
  // channel dial window: scale (lit, digits texture), pointer, glass, brass bezel
  const dx = 0.105, dy = 0.208, dw = 0.20, dh = 0.06;
  m.box(dw, dh, 0.002, 0, 'radioDial', [dx, dy, pz - 0.001], [0, 0, 0], 'Body', { cast: false });
  m.part('DialPointer', { pivot: [dx, dy, pz - 0.003], axis: 'x', kind: 'slide', range: [-0.09, 0.09], note: 'kanal göstergesi; 0 = skala ortası (K-07)' });
  m.box(0.004, 0.052, 0.0015, 0, 'paintBlack', [dx, dy, pz - 0.003], [0, 0, 0], 'DialPointer', { cast: false });
  m.box(dw, dh, 0.0015, 0, 'glassClear', [dx, dy, pz - 0.0046], [0, 0, 0], 'Body', { cast: false });
  for (const sy of [-1, 1]) m.box(dw + 0.016, 0.008, 0.005, 0.0015, 'brassWorn', [dx, dy + sy * (dh / 2 + 0.004), pz - 0.0025]);
  for (const sx of [-1, 1]) m.box(0.008, dh + 0.016, 0.005, 0.0015, 'brassWorn', [dx + sx * (dw / 2 + 0.004), dy, pz - 0.0025]);
  m.text('TXT_Channel', { center: [dx, 0.2535, pz - 0.0002], size: [0.10, 0.012], role: 'plateLabel', capMm: 7, sample: 'KANAL', sampleEn: 'CHANNEL', note: 'krem baskı #E9DDBE' });
  m.light('DialLight', { pos: [dx, dy, pz - 0.03], type: 'point', color: '#FFA850', shadows: false, note: 'skala ışığı; Rev2 0,12, menzil 0,6 m' });
  // SİNYAL meter
  const mx = 0.045, my = 0.118, mw = 0.08, mh = 0.07;
  m.box(mw, mh, 0.002, 0, 'dialCream', [mx, my, pz - 0.001], [0, 0, 0], 'Body', { cast: false });
  m.part('Needle', { pivot: [mx, my - 0.03, pz - 0.0028], axis: 'z', range: [-50, 50], note: 'SİNYAL ibresi; 0 = dik' });
  m.box(0.0026, 0.05, 0.0012, 0, 'paintBlack', [mx, my - 0.005, pz - 0.0028], [0, 0, 0], 'Needle', { cast: false });
  m.cyl(0.0042, 0.002, 10, 0, 'paintBlack', [mx, my - 0.03, pz - 0.0028], [-90, 0, 0], 'Needle', { cast: false });
  m.box(mw, mh, 0.0015, 0, 'glassClear', [mx, my, pz - 0.0046], [0, 0, 0], 'Body', { cast: false });
  for (const sy of [-1, 1]) m.box(mw + 0.012, 0.006, 0.005, 0.0015, 'brassWorn', [mx, my + sy * (mh / 2 + 0.003), pz - 0.0025]);
  for (const sx of [-1, 1]) m.box(0.006, mh + 0.012, 0.005, 0.0015, 'brassWorn', [mx + sx * (mw / 2 + 0.003), my, pz - 0.0025]);
  m.text('TXT_Signal', { center: [mx, my - 0.016, pz - 0.0022], size: [0.06, 0.01], role: 'plateLabel', capMm: 6, sample: 'SİNYAL', sampleEn: 'SIGNAL', note: 'ölçek yüzünde, mürekkep #2A1E10' });
  // knobs
  for (const [name, kx, label, en] of [['KnobVolume', 0.135, 'SES', 'VOLUME'], ['KnobTune', 0.185, 'AYAR', 'TUNE']]) {
    m.part(name, { pivot: [kx, 0.11, pz], axis: 'z', range: [-150, 150], note: name === 'KnobVolume' ? 'ses' : 'ince ayar (DialPointer ile birlikte)' });
    m.lathe([[0, 0], [0.018, 0], [0.018, 0.005], [0.015, 0.0065], [0.0135, 0.02], [0.0115, 0.024], [0, 0.0245]], 14, 'bakeliteBlack', [kx, 0.11, pz], [-90, 0, 0], name);
    m.box(0.003, 0.011, 0.0018, 0, 'paintCream', [kx, 0.1165, pz - 0.0252], [0, 0, 0], name, { cast: false });
    m.text(`TXT_${name.slice(4)}`, { center: [kx, 0.062, pz - 0.0002], size: [0.048, 0.012], role: 'plateLabel', capMm: 7, sample: label, sampleEn: en });
  }
  // telescopic antenna (rear right)
  const ab = [0.19, H - 0.004, 0.08]; m.cyl(0.011, 0.014, 10, 0.002, 'chrome', [ab[0], ab[1] + 0.007, ab[2]]);
  const dirA = V.nrm([-0.25, 1, 0.12]); let p = [ab[0], ab[1] + 0.014, ab[2]];
  for (const [L, r] of [[0.15, 0.0056], [0.14, 0.0046], [0.12, 0.0038]]) { const q = V.add(p, V.scl(dirA, L)); segCyl(m, p, q, r, r, 8, 'chrome', 'Body', { cast: r > 0.004 }); p = q; }
  m.add(gSphere(0.0048, 8, 6), 'chrome', p, [0, 0, 0], 'Body', { cast: false });
  m.meta.place = [place([0.235, CRTT.top + 0.11, 0.155], 0, 'yükseltilmiş telsiz rafının üstü (CrtTable.RadioSpot)')];
  return m;
}

// ---------------------------------------------------------------- Microphone
export function Microphone() {
  const m = new Model('Microphone', { family: 'crt', budget: 'small', listName: 'Masa mikrofonu (bas-konuş)', origin: 'taban ortası (masa yüzeyi, y = 0)',
    note: 'Döküm kubbeli kaide, krom gövde ve çatal, yuvarlak krom kafa (ızgara dokuda), çatal vidaları bakalit. Kaidenin önünde bakalit bas-konuş kolu PTT (arka kenarından menteşeli, −8° basılır). Arkadan kısa kablo.' });
  m.lathe([[0, 0], [0.052, 0], [0.053, 0.004], [0.047, 0.014], [0.03, 0.022], [0.012, 0.026], [0, 0.0265]], 12, 'castIron');
  segCyl(m, [0, 0.024, 0.02], [0, 0.12, 0.02], 0.0065, 0.0055, 8, 'chrome');
  addU(m, gSweep([[-0.043, 0.153, 0.02], [-0.043, 0.123, 0.02], [0, 0.119, 0.02], [0.043, 0.123, 0.02], [0.043, 0.153, 0.02]], 0.01, 0.006, { side: [0, 0, 1] }), 'chrome');
  const hd = [0, Math.sin(15 * D2R), -Math.cos(15 * D2R)], hc = [0, 0.153, 0.02];
  m.lathe([[0, -0.016], [0.026, -0.016], [0.034, -0.012], [0.035, 0.0], [0.034, 0.012], [0.026, 0.016], [0, 0.016]], 14, 'chrome', hc, dirEuler(hd));
  m.cyl(0.0255, 0.002, 14, 0, 'micGrille', V.add(hc, V.scl(hd, 0.0162)), dirEuler(hd), 'Body', { cast: false });
  for (const sx of [-1, 1]) m.cyl(0.0065, 0.009, 10, 0.0015, 'bakeliteBlack', [sx * 0.049, 0.153, 0.02], [0, 0, 90], 'Body', { cast: false });
  m.part('PTT', { pivot: [0, 0.03, 0.012], axis: 'x', range: [0, -8], note: 'bas-konuş kolu: 0 = bırakılmış, −8° = basılı (ön uç aşağı)' });
  m.box(0.05, 0.007, 0.058, 0.0025, 'bakeliteBlack', [0, 0.0315, -0.016], [0, 0, 0], 'PTT');
  m.box(0.05, 0.011, 0.014, 0.004, 'bakeliteBlack', [0, 0.029, -0.041], [0, 0, 0], 'PTT');
  m.box(0.03, 0.012, 0.012, 0.003, 'castIron', [0, 0.028, 0.012], [0, 0, 0], 'Body', { cast: false });
  tubeU(m, [[0, 0.008, 0.048], [0.004, 0.005, 0.068], [0.02, 0.0035, 0.10], [0.045, 0.0035, 0.14]], 0.0035, 5, 9, 'rubber', 'Body', { cast: false });
  m.anchor('Hover', { pos: [0, 0.2, -0.05], note: 'üzerine gelince kalkış / kâğıt etiket' });
  m.meta.place = [place([0.03, CRTT.top, -0.17], -20, 'CRT ile telsiz rafı arasında, operatöre dönük')];
  return m;
}

// ---------------------------------------------------------------- LogBook
export function LogBook() {
  const m = new Model('LogBook', { family: 'crt', budget: 'small', listName: 'Telsiz kayıt defteri (açık, A5)', origin: 'taban ortası (masa yüzeyi, y = 0)',
    note: 'Açık bez sırtlı A5 ciltli defter: sayfa blokları cilde doğru kıvrılır, sağ sayfanın alt köşesi kalkık, üstünde kurşun kalem. Yazılar TMP: sol sayfa başlığı ve kayıt satırları (Kalam el yazısı, saat sütunu Courier Prime).' });
  for (const sx of [-1, 1]) m.box(0.153, 0.003, 0.212, 0.0012, 'book1', [sx * 0.0785, 0.0015, 0]);
  m.box(0.016, 0.003, 0.212, 0.001, 'book2', [0, 0.0012, 0], [0, 0, 0], 'Body', { cast: false });
  const prof = [[0.002, 0.0032], [0.007, 0.0078], [0.018, 0.0098], [0.045, 0.0106], [0.10, 0.0105], [0.139, 0.0101], [0.1455, 0.0092], [0.148, 0.006], [0.148, 0.0032]];
  for (const sx of [-1, 1]) m.extrude(prof.map(([x, y]) => [sx * x, y]), 0.205, 0.0006, 'paper', [0, 0, 0], [0, 0, 0], 'Body', { cast: false });
  // lifted page corner (right page, front outer corner), both sides
  const A = [0.118, 0.0101, -0.1025], B = [0.148, 0.0094, -0.0725], C = [0.148, 0.0098, -0.1025];
  const M = V.lerp(A, B, 0.5), e = V.nrm(V.sub(B, A)), out = V.sub(C, M), dl = Math.hypot(...out), od = V.nrm(out), hl = Math.hypot(...V.sub(A, M));
  const rings = [0, 0.3, 0.6, 0.85, 1].map(t => { const th = 75 * D2R * Math.pow(t, 1.3), d = dl * t; const P = V.add(V.add(M, V.scl(od, d * Math.cos(th))), [0, d * Math.sin(th) + 0.0003, 0]); const h = Math.max(0.0008, hl * (1 - t)); return [V.sub(P, V.scl(e, h)), V.add(P, V.scl(e, h))]; });
  addU(m, gLoft(rings, { closed: false, orient: [0, 1, 0], crease: 70 }), 'paper', 'Body', { cast: false });
  addU(m, gLoft(rings.map(r => r.map(p => V.add(p, [0, -0.0003, 0]))), { closed: false, orient: [0, -1, 0], crease: 70 }), 'paper', 'Body', { cast: false });
  // pencil
  const dir = V.nrm([0.8, 0, -0.6]), y = 0.0106 + 0.0038, p0 = [0.012, y, 0.058];
  const at = (s) => V.add(p0, V.scl(dir, s));
  segCyl(m, at(0), at(0.006), 0.0036, 0.0036, 8, 'eraser', 'Body', { cast: false });
  segCyl(m, at(0.006), at(0.015), 0.0039, 0.0039, 8, 'brassWorn', 'Body', { cast: false });
  segCyl(m, at(0.015), at(0.135), 0.00375, 0.00375, 6, 'pencil', 'Body', { cast: false });
  segCyl(m, at(0.135), at(0.151), 0.00375, 0.0012, 6, 'pawn', 'Body', { cast: false });
  segCyl(m, at(0.151), at(0.155), 0.0012, 0.0003, 6, 'inkBlack', 'Body', { cast: false });
  const tz = 0.0109;
  m.text('TXT_Head', { center: [-0.083, tz, 0.083], size: [0.118, 0.014], normal: [0, 1, 0], up: [0, 0, 1], role: 'reportHead', capMm: 5.5, align: 'left', sample: 'TELSİZ KAYDI · K-07', sampleEn: 'RADIO LOG · K-07' });
  m.text('TXT_EntriesL', { center: [-0.083, tz, -0.012], size: [0.118, 0.16], normal: [0, 1, 0], up: [0, 0, 1], role: 'handNote (Kalam 700) + saat sütunu Courier Prime 700', font: 'Kalam 700', capMm: 5, align: 'left', sample: '014/02  Alfa hazır · Doğu Avr. 02', sampleEn: '014/02  Alpha ready · E. Europe 02', note: 'satır aralığı ≈ 9 mm (sayfa çizgileri dokuda), en çok 15 satır' });
  m.text('TXT_EntriesR', { center: [0.083, tz, 0.004], size: [0.118, 0.19], normal: [0, 1, 0], up: [0, 0, 1], role: 'handNote (Kalam 700)', font: 'Kalam 700', capMm: 5, align: 'left', sample: '014/03  gözcü: yayın yok', sampleEn: '014/03  spotter: no broadcast', note: 'sol sayfa dolunca' });
  m.meta.place = [place([0.27, CRTT.top, -0.165], 8, 'telsiz rafının önünde, masanın ön kenarı')];
  return m;
}

export const MODELS = {
  CrtTable: { build: CrtTable },
  Crt: { build: Crt },
  Radio: { build: Radio },
  Microphone: { build: Microphone },
  LogBook: { build: LogBook },
};
