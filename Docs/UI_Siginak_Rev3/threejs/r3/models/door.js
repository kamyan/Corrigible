// family: door · Rev3 model library (see DESIGN_R3.md §3.7) — owner: room-shelf
// Door group frame (DoorFrame, DoorLeaf, Corridor, Handrail): origin = bottom centre of the opening on the ROOM wall plane,
// world (2.20, 0, 0.75), yaw 90 → model −z = room side (world −x), model +z = corridor (world +x), model +x = world −z
// (hinge side, toward z0 = 0.32). Wall thickness 0.25 (corridor face at world x 2.45). Fronts face −z. Every word is TMP.
import * as THREE from 'three';
import { Model, gExtrude, gSphere, gTorus, gRing, roundedRect, circlePts, D2R } from '../mlib.js';
import { defMat } from '../mats.js';
import { gLoft, ringH, ringP, gSweep, gPipe, segCyl, segBox, tubeU, addU, wp, screwHead, dirEuler, V } from './shelf.js';

defMat('treadPlate', { hex: '#6B6E68', rough: 0.5, metal: 0.7, tex: 'tx_tread (baklava desenli sac, 256 px)', note: 'eşik plakası (baklava sac)' });
defMat('peepLens', { hex: '#141A18', rough: 0.08, metal: 0, note: 'gözetleme deliği merceği' });
defMat('knockLampRed', { hex: '#F0443A', rough: 0.25, metal: 0, emissive: '#F0443A', ei: 3.0, note: 'vuruş lambası ölümcül (kırmızı = tehdit)' });
defMat('intercomGrille', { hex: '#26271F', rough: 0.7, metal: 0.1, tex: 'tx_grille (Ø 4 mm delik ızgarası, 256 px)', note: 'interkom hoparlör ızgarası' });

export const DOOR = { base: [2.2, 0, 0.75], yaw: 90, x: 2.2, z0: 0.32, z1: 1.18, h: 2.0, wall: 0.25,
  leaf: { w: 0.86, h: 1.98, t: 0.07, zIn: 0.003 }, hinge: [0.44, 0, 0.038], wheel: [0, 1.05, -0.09], peepY: 1.56 };
const dplace = (note) => ({ pos: DOOR.base, yaw: DOOR.yaw, note: 'dünya; kapı grubu orijini (açıklığın alt orta noktası, oda duvar düzlemi x 2,20): ' + note });

// ---------------------------------------------------------------- DoorFrame
export function DoorFrame() {
  const m = new Model('DoorFrame', { family: 'door', budget: 'big', listName: 'Kapı kasası (çelik, beton söveli)', origin: 'açıklığın alt orta noktası, oda duvar düzlemi (z = 0)',
    note: 'Duvara gömülü çelik kasa: oda tarafında cıvatalı yüz lamaları (kilit tarafı ve üstte kanadın 25 mm’sini örter = dayama), menteşe tarafında menteşe dilleri görünsün diye dar yüz; açıklık boyunca çelik astar, ardında beton söve (duvar kalınlığı 0,25 m), baklava sac eşik. Menteşelerin sabit yarıları ve kilit karşılıkları bu modelde. Oda duvarındaki boşluk: 1,12 × 2,09 m.' });
  const zr = 0.14, W2 = 0.56, hy = 1.967;
  m.box(2 * W2, 2.09 - hy, 0.014, 0.006, 'paintOliveDark', [0, (hy + 2.09) / 2, -0.007]);                          // head face
  m.box(W2 - 0.405, hy, 0.014, 0.006, 'paintOliveDark', [-(0.405 + W2) / 2, hy / 2, -0.007]);                       // latch jamb face (stop)
  m.box(W2 - 0.48, hy, 0.014, 0.006, 'paintOliveDark', [(0.48 + W2) / 2, hy / 2, -0.007]);                          // hinge jamb face
  m.box(0.008, 1.995, zr, 0.002, 'paintOliveDark', [-0.442, 0.9975, zr / 2]);                                       // liners
  m.box(0.008, 1.995, zr, 0.002, 'paintOliveDark', [0.484, 0.9975, zr / 2]);
  m.box(0.934, 0.008, zr, 0.002, 'paintOliveDark', [0.021, 1.999, zr / 2]);
  // concrete reveal through the rest of the wall
  m.box(0.16, 2.1, DOOR.wall - zr, 0.006, 'concreteWall', [-0.446 - 0.08, 1.05, (zr + DOOR.wall) / 2]);
  m.box(0.16, 2.1, DOOR.wall - zr, 0.006, 'concreteWall', [0.50 + 0.08, 1.05, (zr + DOOR.wall) / 2]);
  m.box(1.26, 0.10, DOOR.wall - zr, 0.006, 'concreteWall', [0.027, 2.05, (zr + DOOR.wall) / 2]);
  m.box(0.98, 0.012, DOOR.wall + 0.03, 0.003, 'treadPlate', [0.02, 0.006, (DOOR.wall - 0.03) / 2], [0, 0, 0], 'Body', { skip: ['-y'] });
  // fixed hinge halves (lower knuckles) with weld plates to the liner
  for (const y of [0.30, 1.00, 1.70]) {
    m.lathe([[0.025, 0], [0.025, 0.088], [0.021, 0.092]], 12, 'steelCast', [DOOR.hinge[0], y - 0.092, DOOR.hinge[2]], [0, 0, 0], 'Body', { caps: true });
    m.box(0.046, 0.08, 0.012, 0.003, 'steelCast', [0.461, y - 0.046, DOOR.hinge[2]]);
  }
  for (const y of [0.50, 1.05, 1.60]) m.box(0.014, 0.06, 0.04, 0.003, 'steelCast', [-0.432, y, 0.04]);                // dog keepers
  m.box(0.08, 0.016, 0.04, 0.003, 'steelCast', [0, 1.987, 0.04]);
  for (const y of [0.25, 0.75, 1.25, 1.75]) { screwHead(m, [-0.49, y, -0.014], [0, 0, -1], 0.008, 0.005, 'steelBare', 'Body', 6); screwHead(m, [0.52, y, -0.014], [0, 0, -1], 0.008, 0.005, 'steelBare', 'Body', 6); }
  for (const x of [-0.35, 0, 0.35]) screwHead(m, [x, 2.03, -0.014], [0, 0, -1], 0.008, 0.005, 'steelBare', 'Body', 6);
  m.meta.place = [dplace('kasa; DoorLeaf ile aynı konum')];
  return m;
}

// ---------------------------------------------------------------- DoorLeaf
export function DoorLeaf() {
  const m = new Model('DoorLeaf', { family: 'door', budget: 'big', listName: 'Kapı kanadı (perçinli çelik, iki yüz)', origin: 'açıklığın alt orta noktası, oda duvar düzlemi (z = 0)',
    note: 'Perçinli çelik kanat 0,86 × 1,98 × 0,07: kenar çerçevesi ve üç takviye bandı her iki yüzde 10 mm kabarık, aralarda çökük paneller. İç yüz: kırmızı el çarkı (Wheel), göbek, kilit tarafına ve yukarı giden dil çubukları (sabit), pirinç gözetleme halkası ve sürgülü kapağı (PeepCover), şablon 07. Dış yüz: SIĞINAK 07 · KYGA şablonu, dış çark göbeği (menüdeki OuterWheel buraya takılır), merceğin dış ucu. Kanat koridora açılır (Hinge 0 → 95°).' });
  const L = DOOR.leaf, zi = L.zIn, zo = zi + L.t, zm = zi + L.t / 2, hw = L.w / 2, y0 = 0.012, H = L.h;
  m.part('Hinge', { pivot: DOOR.hinge, axis: 'y', range: [0, 95], note: 'kanat; + yön koridora (dünya +x). Menteşe ekseni kanat kalınlığının ortasında, kenardan 10 mm dışarıda' });
  const P = 'Hinge';
  const fw = 0.07;
  for (const sx of [-1, 1]) m.box(fw, H, L.t, 0.006, 'paintOlive', [sx * (hw - fw / 2), y0 + H / 2, zm], [0, 0, 0], P);
  for (const y of [y0 + fw / 2, y0 + H - fw / 2]) m.box(L.w - 2 * fw, fw, L.t, 0.005, 'paintOlive', [0, y, zm], [0, 0, 0], P);
  m.box(L.w - 2 * fw, H - 2 * fw, L.t - 0.02, 0, 'paintOlive', [0, y0 + H / 2, zm], [0, 0, 0], P);
  const bands = [0.30, 1.05, 1.70];
  for (const y of bands) m.box(L.w - 2 * fw, 0.08, L.t, 0.006, 'paintOlive', [0, y, zm], [0, 0, 0], P);
  // rivets (both faces): bands + stiles
  for (const [zf, d] of [[zi, [0, 0, -1]], [zo, [0, 0, 1]]]) {
    for (const y of bands) for (let i = 0; i < 6; i++) { const x = -0.29 + i * 0.116; if (y === 1.05 && Math.abs(x) < 0.1) continue; screwHead(m, [x, y, zf], d, 0.008, 0.005, 'paintOlive', P, 5); }
    for (const sx of [-1, 1]) for (const y of [0.12, 0.66, 1.38, 1.88]) screwHead(m, [sx * (hw - fw / 2), y, zf], d, 0.008, 0.005, 'paintOlive', P, 5);
  }
  // hinge halves on the leaf (upper knuckles) + weld straps on both faces
  for (const y of [0.30, 1.00, 1.70]) {
    m.lathe([[0.025, 0], [0.025, 0.09], [0.018, 0.096], [0, 0.098]], 12, 'steelCast', [DOOR.hinge[0], y, DOOR.hinge[2]], [0, 0, 0], P, { caps: false });
    for (const zf of [zi - 0.006, zo + 0.006]) m.box(0.16, 0.06, 0.012, 0.003, 'steelCast', [0.365, y + 0.045, zf], [0, 0, 0], P);
  }
  // inner face hardware: hub boss, dog bars + guides
  m.lathe([[0.05, 0], [0.048, 0.008], [0.036, 0.02], [0.03, 0.033]], 14, 'steelCast', [0, 1.05, zi], [-90, 0, 0], P, { caps: false });
  m.box(0.33, 0.04, 0.012, 0.004, 'steelBare', [-0.215, 1.05, zi - 0.008], [0, 0, 0], P);
  m.box(0.04, 0.62, 0.012, 0.004, 'steelBare', [0, 1.43, zi - 0.008], [0, 0, 0], P);
  m.box(0.05, 0.07, 0.026, 0.005, 'steelCast', [-0.37, 1.05, zi - 0.011], [0, 0, 0], P);
  m.box(0.07, 0.05, 0.026, 0.005, 'steelCast', [0, 1.73, zi - 0.011], [0, 0, 0], P);
  m.box(0.07, 0.05, 0.026, 0.005, 'steelCast', [0, 1.4, zi - 0.006], [0, 0, 0], P);
  // peephole: brass ring + lens, sliding cover in two guides
  const py = DOOR.peepY, pz = zi + 0.01;
  m.lathe([[0.012, 0], [0.025, 0], [0.025, 0.006], [0.021, 0.011], [0.012, 0.011]], 16, 'brass', [0, py, pz], [-90, 0, 0], P, { caps: false });
  m.cyl(0.012, 0.004, 12, 0, 'peepLens', [0, py, pz - 0.004], [-90, 0, 0], P, { cast: false });
  for (const sx of [-1, 1]) m.box(0.009, 0.17, 0.009, 0.002, 'brass', [sx * 0.037, py + 0.03, pz - 0.0145], [0, 0, 0], P);
  m.part('PeepCover', { parent: P, pivot: [0, py, pz - 0.016], axis: 'y', kind: 'slide', range: [0, 0.05], note: 'gözetleme kapağı: 0 = kapalı, +50 mm yukarı = açık. Dünyada tıklanır (üzerine gelince 4 mm kalkış)' });
  m.box(0.064, 0.08, 0.006, 0.002, 'brassWorn', [0, py, pz - 0.016], [0, 0, 0], 'PeepCover');
  m.cyl(0.008, 0.008, 10, 0.002, 'brassWorn', [0, py - 0.03, pz - 0.023], [-90, 0, 0], 'PeepCover', { cast: false });
  m.anchor('PeepHover', { part: P, pos: [0, py + 0.02, pz - 0.08], note: 'gözetleme deliği tıklama/etiket noktası' });
  // hand wheel (red = threat interface, kept)
  const wc = DOOR.wheel;
  m.part('Wheel', { parent: P, pivot: wc, axis: 'z', range: [0, 450], note: 'iç el çarkı: 1,25 tur (450°) = dilleri çeker; kapı açılırken döner' });
  m.add(gTorus(0.155, 0.017, 8, 28), 'red', wc, [0, 0, 0], 'Wheel');
  for (let i = 0; i < 4; i++) { const a = (45 + i * 90) * D2R; const c = [wc[0] + Math.cos(a) * 0.09, wc[1] + Math.sin(a) * 0.09, wc[2]];
    m.box(0.028, 0.13, 0.016, 0.005, 'red', c, [0, 0, (45 + i * 90) - 90], 'Wheel'); }
  m.lathe([[0.036, 0], [0.036, 0.04], [0.03, 0.05], [0.02, 0.058], [0, 0.06]], 12, 'red', [wc[0], wc[1], zi - 0.033], [-90, 0, 0], 'Wheel');
  // stencil 07 on the inner face
  m.text('TXT_Stencil07', { part: P, center: [0, 0.66, zi + 0.0095], size: [0.36, 0.2], role: 'wallStencil', font: 'Big Shoulders Stencil Display 800', capMm: 150, sample: '07', sampleEn: '07', note: 'boya #C9C2AD, hafif silik; çökük panel üstünde' });
  // outer face: stencil, outer wheel boss, lens end
  m.text('TXT_StencilOuter', { part: P, center: [0, 1.83, zo - 0.0095], size: [0.68, 0.11], normal: [0, 0, 1], role: 'wallStencil', font: 'Big Shoulders Stencil Display 800', capMm: 55, sample: 'SIĞINAK 07 · KYGA', sampleEn: 'SHELTER 07 · KYGA', note: 'dış yüz (sahanlıktan okunur)' });
  m.lathe([[0.055, 0], [0.052, 0.01], [0.04, 0.025], [0.032, 0.04]], 14, 'steelCast', [0, 1.05, zo], [90, 0, 0], P, { caps: true });
  m.box(0.03, 0.03, 0.03, 0.004, 'steelBare', [0, 1.05, zo + 0.055], [0, 0, 0], P);
  m.cyl(0.012, 0.004, 12, 0, 'peepLens', [0, py, zo - 0.008], [90, 0, 0], P, { cast: false });
  m.anchor('OuterWheel', { part: P, pos: [0, 1.05, zo + 0.07], note: 'menü: OuterWheel modeli bu kare mile takılır (çark düzlemi +z’ye bakar)' });
  m.anchor('PeepRingOuter', { part: P, pos: [0, py, zo - 0.01], note: 'menü: PeepRing modeli (dış pirinç halka)' });
  m.meta.place = [dplace('kapalı Hinge 0; açık 95° (koridora)')];
  return m;
}

// ---------------------------------------------------------------- caged bulkhead lamp (KnockLamp, CorridorLamp)
function bulkhead(m, glass, swapNote) {
  const c = [0, 0.07, 0];
  m.lathe([[0.066, 0], [0.066, 0.008], [0.06, 0.016], [0.052, 0.02]], 12, 'castIron', c, [-90, 0, 0], 'Body', { caps: false });
  m.lathe([[0.052, 0.02], [0.048, 0.042]], 12, 'castIron', c, [-90, 0, 0], 'Body', { caps: false });
  m.part('Lamp', { pivot: [0, 0.07, -0.042], kind: 'swap', note: swapNote });
  m.lathe([[0.046, 0], [0.045, 0.012], [0.039, 0.032], [0.026, 0.048], [0, 0.056]], 14, glass, [0, 0.07, -0.042], [-90, 0, 0], 'Lamp', { cast: false });
  m.add(gTorus(0.05, 0.005, 5, 16), 'castIron', [0, 0.07, -0.044], [0, 0, 0], 'Body', { cast: false });
  const R = 0.054, C = [0, 0.07, -0.042];
  for (let k = 0; k < 3; k++) { const ph = (90 + k * 120) * D2R; const e = [Math.cos(ph), Math.sin(ph), 0];
    const path = []; for (let i = 0; i <= 5; i++) { const t = (i / 5) * (Math.PI / 2) * 0.93; path.push(V.add(C, V.add(V.scl(e, R * Math.cos(t)), [0, 0, -R * Math.sin(t)]))); }
    addU(m, gSweep(path, 0.008, 0.003, { side: [-Math.sin(ph), Math.cos(ph), 0] }), 'castIron', 'Body', { cast: false }); }
  m.cyl(0.012, 0.006, 10, 0.002, 'castIron', [0, 0.07, -0.097], [-90, 0, 0], 'Body', { cast: false });
}
export function KnockLamp() {
  const m = new Model('KnockLamp', { family: 'door', budget: 'small', listName: 'Vuruş lambası ve kafesi', origin: 'arka yüz alt kenar ortası (duvar düzlemi z = 0)',
    note: 'Kapının üstünde döküm gövdeli kafesli duvar lambası: kubbe cam (Lamp) sönük / amber (vuruş, her vuruşta parlar) / kırmızı (ölümcül karar). Kafes 3 yassı lama (8 mm). Tavana çıkan boru.' });
  bulkhead(m, 'jewelAmberOff', 'cam: jewelAmberOff (sönük) ↔ jewelAmberOn (biri çalıyor) ↔ knockLampRed (VURULDU / SIZMA)');
  segCyl(m, [0, 0.136, -0.02], [0, 0.25, -0.02], 0.01, 0.01, 8, 'galv', 'Body', { cast: false });
  m.cyl(0.015, 0.016, 8, 0.003, 'castIron', [0, 0.14, -0.02]);
  m.light('KnockLight', { part: 'Lamp', pos: [0, 0.04, -0.25], type: 'point', color: '#FFA53A', shadows: false, note: 'amber 1,2 · ölümcülde #F0443A 2,2; menzil 3,5 m (Rev2); gölgesiz' });
  m.meta.place = [{ pos: [2.2, 2.24, 0.75], yaw: 90, note: 'dünya; kapının üstü, tehlike bandının üstünde (lamba merkezi y 2,31)' }];
  return m;
}
export function CorridorLamp() {
  const m = new Model('CorridorLamp', { family: 'door', budget: 'small', listName: 'Koridor lambası (kafesli)', origin: 'arka yüz alt kenar ortası (duvar düzlemi z = 0)',
    note: 'Vuruş lambasıyla aynı kafesli duvar lambası, soğuk ışıklı. Kapı açılınca merdiven ışıkları yanar (Lamp: lampGlassCold ↔ roomBulbOff).' });
  bulkhead(m, 'lampGlassCold', 'cam: lampGlassCold (yanık) ↔ roomBulbOff (sönük)');
  m.light('CorridorLight', { part: 'Lamp', pos: [0, 0.0, -0.3], type: 'point', color: '#D8E6E2', shadows: false, note: 'Rev2: 5,5 menzil 5 m (1.) ve 2,2 menzil 3,5 m (2.); ilk lamba gölgeli olabilir (pozda ≤ 4 gölgeli)' });
  m.meta.place = [{ pos: [3.3, 2.18, -0.05], yaw: 180, note: 'dünya; koridor yakın duvarı (z −0,05), koridora bakar' },
    { pos: [4.9, 2.9, 1.55], yaw: 0, note: 'dünya; merdiven boşluğu, uzak duvar (z 1,55), basamaklara bakar' }];
  return m;
}

// ---------------------------------------------------------------- IntercomBox
export function IntercomBox() {
  const m = new Model('IntercomBox', { family: 'door', budget: 'small', listName: 'İnterkom kutusu', origin: 'arka yüz alt kenar ortası (duvar düzlemi z = 0)',
    note: 'Zeytin yeşili sac kutu, vidalı kapak, yuvarlak hoparlör ızgarası (delikler dokuda), siyah bakalit çağrı düğmesi (Button) ve amber LED (Led), üstte pirinç İNTERKOM plakası, yukarı çıkan boru. Dünyada tıklanır (üzerine gelince 4 mm kalkış).' });
  const W = 0.17, H = 0.25, D = 0.06;
  m.box(W, H, D, 0.006, 'paintOlive', [0, H / 2, -D / 2], [0, 0, 0], 'Body', { skip: ['-z'] });
  m.box(W - 0.012, H - 0.012, 0.004, 0.002, 'paintOlive', [0, H / 2, -D - 0.002]);
  for (const sx of [-1, 1]) for (const sy of [-1, 1]) screwHead(m, [sx * 0.07, H / 2 + sy * 0.108, -D - 0.004], [0, 0, -1], 0.004, 0.0025, 'steelBare', 'Body', 6);
  m.cyl(0.05, 0.003, 14, 0, 'intercomGrille', [0, 0.14, -D - 0.0055], [-90, 0, 0], 'Body', { cast: false });
  m.lathe([[0.048, 0], [0.056, 0], [0.056, 0.004], [0.05, 0.007]], 16, 'steelBare', [0, 0.14, -D - 0.002], [-90, 0, 0], 'Body', { caps: false, cast: false });
  m.box(0.13, 0.026, 0.002, 0.0007, 'plate', [0, 0.215, -D - 0.005], [0, 0, 0], 'Body', { cast: false });
  m.text('TXT_Plate', { center: [0, 0.215, -D - 0.0062], size: [0.115, 0.018], role: 'plateLabel', capMm: 11, sample: 'İNTERKOM', sampleEn: 'INTERCOM' });
  m.lathe([[0.0135, 0], [0.019, 0], [0.019, 0.004], [0.015, 0.0065]], 12, 'chrome', [-0.03, 0.05, -D - 0.002], [-90, 0, 0], 'Body', { caps: false, cast: false });
  m.part('Button', { pivot: [-0.03, 0.05, -D - 0.009], axis: 'z', kind: 'slide', range: [0, 0.004], note: 'çağrı düğmesi; +4 mm içeri = basılı' });
  m.lathe([[0, 0], [0.0125, 0], [0.0125, 0.007], [0.0105, 0.0095], [0, 0.0105]], 12, 'bakeliteBlack', [-0.03, 0.05, -D - 0.004], [-90, 0, 0], 'Button');
  m.cyl(0.009, 0.006, 12, 0.0015, 'chrome', [0.035, 0.05, -D - 0.006], [-90, 0, 0], 'Body', { cast: false });
  m.part('Led', { pivot: [0.035, 0.05, -D - 0.009], kind: 'swap', note: 'jewelAmberOn (hat açık / konuşuluyor) ↔ jewelAmberOff' });
  m.add(gSphere(0.0065, 8, 5, { thetaLength: Math.PI / 2 }), 'jewelAmberOn', [0.035, 0.05, -D - 0.009], [-90, 0, 0], 'Led', { cast: false });
  m.cyl(0.015, 0.016, 8, 0.003, 'castIron', [0.045, H + 0.008, -0.03]);
  segCyl(m, [0.045, H + 0.016, -0.03], [0.045, H + 0.6, -0.03], 0.01, 0.01, 8, 'galv', 'Body', { cast: false });
  m.anchor('Hover', { pos: [0, 0.13, -0.16], note: 'dünyada tıklama; üzerine gelince kutu 4 mm öne kalkar, kâğıt etiket bu noktaya göre' });
  m.meta.place = [{ pos: [2.2, 1.295, 0.0], yaw: 90, note: 'dünya; kapının sağı (odadan bakınca), merkez y 1,42 (layout3d.door.intercom)' }];
  return m;
}

// ---------------------------------------------------------------- HazardHeader
export function HazardHeader() {
  const m = new Model('HazardHeader', { family: 'door', budget: 'small', listName: 'Tehlike bandı (kapı üstü)', origin: 'arka yüz alt kenar ortası (duvar düzlemi z = 0)',
    note: 'Kapı kasasının üstünde boyalı sac levha, 45° sarı-siyah şerit dokuda (paintHazard), dört vida.' });
  m.box(1.10, 0.12, 0.008, 0.003, 'paintHazard', [0, 0.06, -0.004], [0, 0, 0], 'Body', { skip: ['-z'] });
  for (const sx of [-1, 1]) for (const y of [0.02, 0.10]) screwHead(m, [sx * 0.53, y, -0.008], [0, 0, -1], 0.005, 0.003, 'steelBare', 'Body', 6);
  m.meta.place = [{ pos: [2.2, 2.10, 0.75], yaw: 90, note: 'dünya; kasa yüz lamasının hemen üstü' }];
  return m;
}

// ---------------------------------------------------------------- Corridor (landing + stair well)
export function Corridor() {
  const m = new Model('Corridor', { family: 'door', budget: 'big', listName: 'Koridor ve merdiven', origin: 'kapı grubu orijini (açıklığın alt ortası, oda duvar düzlemi)',
    note: 'Kapının ardındaki sahanlık (dünya x 2,45–4,60, z −0,05…1,55, h 2,6): ıslak beton zemin ve su birikintisi, alt kısmı sırlı fayans (en alt sıra kirli) üstü beton duvarlar, tavan. Uzak (+z dünya) yanda 9 basamak yukarı (rıht 0,17, basamak 0,26, çelik burunlu) uç duvardaki boşluktan merdiven kuyusuna girer; kuyu üst sahanlığa çıkar. Zeminde moloz. Fayans ve beton dokuda.' });
  const zw = DOOR.wall, zEnd = 2.40, xs = -0.10; // stairs between x −0.80 … −0.10 (world z 0.85 … 1.55)
  m.box(1.60, 0.05, zEnd - zw, 0.004, 'concreteFloorWet', [0, -0.025, (zw + zEnd) / 2], [0, 0, 0], 'Body', { skip: ['-y'] });
  addU(m, gExtrude([[0.0, 0.0], [0.22, -0.04], [0.38, 0.03], [0.34, 0.16], [0.18, 0.22], [0.02, 0.14]], 0.002, 0).rotateX(Math.PI / 2).translate(0.05, 0.0015, -0.75), 'puddle', 'Body', { cast: false });
  const wall = (x0, x1, z0, z1, yTop, face) => { // wall slab from x0..x1 / z0..z1 (thin along one axis), tiles to 1.5 m
    const cx = (x0 + x1) / 2, cz = (z0 + z1) / 2, sx = Math.abs(x1 - x0), sz = Math.abs(z1 - z0);
    m.box(sx, 0.15, sz, 0.003, 'tileGlazedDirty', [cx, 0.075, cz]); m.box(sx, 1.35, sz, 0.003, 'tileGlazed', [cx, 0.825, cz]);
    m.box(sx, yTop - 1.5, sz, 0.003, 'concreteDark', [cx, 1.5 + (yTop - 1.5) / 2, cz]); };
  wall(0.80, 0.82, zw, zEnd, 2.6);                     // near wall (world z −0.05)
  wall(-0.82, -0.80, zw, 3.62, 3.9);                   // far wall along the stairs (world z 1.55)
  wall(xs, 0.80, zEnd, zEnd + 0.02, 2.6);              // end wall of the landing (world x 4.60)
  wall(xs - 0.02, xs, zEnd, 3.62, 3.9);                // stair-well inner wall beyond the landing
  wall(-0.80, xs, 3.60, 3.62, 3.9);                    // stair-well end wall
  m.box(0.92, 0.05, zEnd - zw, 0.004, 'concreteDark', [0.35, 2.625, (zw + zEnd) / 2]);
  m.box(0.72, 0.05, 0.75, 0.004, 'concreteDark', [-0.45, 2.625, zw + 0.375]);
  m.box(0.72, 0.05, 3.62 - 1.0, 0.004, 'concreteDark', [-0.45, 3.925, 1.0 + (3.62 - 1.0) / 2]);
  m.box(0.02, 1.3, 3.62 - 1.0, 0.003, 'concreteDark', [xs - 0.01, 3.25, 1.0 + (3.62 - 1.0) / 2]);  // well side above the landing ceiling
  // 9 steps up toward +z with steel nosings, then the upper landing
  const s0 = 0.95, run = 0.26, rise = 0.17;
  for (let i = 0; i < 9; i++) { const zf = s0 + i * run, yt = rise * (i + 1);
    m.box(0.70, yt, run, 0, 'concreteWall', [-0.45, yt / 2, zf + run / 2], [0, 0, 0], 'Body', { skip: ['-y'] });
    m.box(0.70, 0.012, 0.03, 0.003, 'steelBare', [-0.45, yt - 0.006, zf + 0.015], [0, 0, 0], 'Body', { cast: false }); }
  const zt = s0 + 9 * run; m.box(0.70, 9 * rise, 3.60 - zt, 0, 'concreteWall', [-0.45, 9 * rise / 2, (zt + 3.60) / 2], [0, 0, 0], 'Body', { skip: ['-y'] });
  // debris
  for (const [x, z, s, ry] of [[0.42, 0.62, 0.07, 20], [0.55, 0.95, 0.045, 70], [0.30, 0.45, 0.05, 130], [0.62, 1.6, 0.06, 40], [0.12, 1.9, 0.04, 100]])
    m.box(s, s * 0.55, s * 0.8, 0.008, 'rubble', [x, s * 0.25, z], [6, ry, 9]);
  m.box(0.15, 0.008, 0.15, 0.002, 'tileGlazed', [0.5, 0.004, 1.25], [0, 25, 4], 'Body', { cast: false });
  m.anchor('Visitor', { pos: [0, 0, 0.55], note: 'ziyaretçi siluetinin ayak noktası (dünya ≈ 2,75 · 0 · 0,75), yüzü odaya (−z yerel)' });
  m.meta.place = [dplace('koridor; kanat açıkken görünür (ışıklar CorridorLamp)')];
  return m;
}

// ---------------------------------------------------------------- Handrail
export function Handrail() {
  const m = new Model('Handrail', { family: 'door', budget: 'small', listName: 'Merdiven korkuluğu (galvaniz boru)', origin: 'kapı grubu orijini (Corridor ile aynı)',
    note: 'Merdivenin açık yanında, basamak burnu çizgisinin 0,90 m üstünde Ø 42 galvaniz boru; alt uçta yatay uzatma ve aşağı dönüş; üç dikme (Ø 34) taban plakalı, üstte boru eyerli bağlantı.' });
  const s0 = 0.95, run = 0.26, rise = 0.17, x = -0.14, hh = 0.90;
  const nose = (z) => (z - s0) / run * rise + rise; // nosing line height at z (z ≥ s0)
  const pts = [[x, 0.62, s0 - 0.36], [x, hh + 0.02, s0 - 0.36], [x, hh + rise * 0.6, s0 - 0.12]];
  for (const z of [s0 + 0.2, s0 + 1.0, s0 + 1.8, s0 + 2.3]) pts.push([x, nose(z) + hh, z]);
  m.add(gPipe(pts, 0.021, 0.021, 8, { caps: [true, true] }), 'galv');
  for (const k of [1, 4, 7]) { const z = s0 + k * run + run * 0.5, yt = rise * (k + 1);
    m.box(0.07, 0.008, 0.07, 0.002, 'galv', [x, yt + 0.004, z], [0, 0, 0], 'Body', { cast: false });
    segCyl(m, [x, yt + 0.008, z], [x, nose(z) + hh - 0.02, z], 0.017, 0.017, 8, 'galv');
    m.box(0.05, 0.03, 0.06, 0.006, 'galv', [x, nose(z) + hh - 0.025, z], [Math.atan2(rise, run) / D2R * -1, 0, 0], 'Body', { cast: false }); }
  m.meta.place = [dplace('merdivenin açık yanı')];
  return m;
}

// ---------------------------------------------------------------- visitor silhouettes (read by outline only)
function limb(m, pts, radii, mat, n = 8, o = {}) {
  const L = pts.length; const rings = pts.map((p, i) => { const tg = V.sub(pts[Math.min(L - 1, i + 1)], pts[Math.max(0, i - 1)]); return ringP(p, tg, radii[i] * (o.sx || 1), radii[i] * (o.sz || 1), n, o.side || [1, 0, 0]); });
  return addU(m, gLoft(rings, { caps: o.caps ?? [true, true], crease: 70 }), mat, 'Body', o);
}
// o: H, mat, n, lean, side, hipX, headLean, torso [[y,a,b,z]], coat [[y,a,b,z,tiltY]], legL/legR {knee, ankle}, legR0 (radius scale), armL/armR {e, w, hand}, hat, head scale
function figure(m, o) {
  const s = o.H / 1.75, mat = o.mat || 'visitor', n = o.n || 10, X = (v) => v * s;
  const piv = [0, X(0.95), 0], lean = (o.lean || 0) * D2R, side = (o.side || 0) * D2R, hx = X(o.hipX || 0);
  const rot = (p, c, ln, sd) => { let x = p[0] - c[0], y = p[1] - c[1], z = p[2] - c[2]; const x1 = x * Math.cos(sd) + y * Math.sin(sd), y1 = -x * Math.sin(sd) + y * Math.cos(sd);
    const y2 = y1 * Math.cos(ln) + z * Math.sin(ln), z2 = -y1 * Math.sin(ln) + z * Math.cos(ln); return [x1 + c[0], y2 + c[1], z2 + c[2]]; };
  const up = (p) => { const q = rot([X(p[0]), X(p[1]), X(p[2])], piv, lean, side); return [q[0] + hx, q[1], q[2]]; };          // canonical (units of s) → posed
  const neck = [0, 1.49, 0]; const hl = (o.headLean || 0) * D2R;
  const hu = (p) => { const q = rot([X(p[0]), X(p[1]), X(p[2])], [0, X(neck[1]), 0], hl, 0); return up([q[0] / s, q[1] / s, q[2] / s]); };
  const T = o.torso || [[0.93, 0.165, 0.11, 0], [1.03, 0.15, 0.10, 0], [1.16, 0.158, 0.106, -0.005], [1.29, 0.178, 0.114, -0.01], [1.39, 0.196, 0.106, 0], [1.445, 0.17, 0.088, 0], [1.485, 0.085, 0.066, 0]];
  const ws = o.ws || 1, ds = o.ds || 1;
  addU(m, gLoft(T.map(([y, a, b, z]) => ringH([0, y, z], a * ws, b * ds, n).map(up)), { crease: 70 }), mat);
  if (o.coat) addU(m, gLoft(o.coat.map(([y, a, b, z, tw]) => ringH([0, y, z || 0], a, b, n).map(p => { const yy = p[1] + (tw || 0) * (p[0] / a); return [X(p[0]) + hx, X(yy), X(p[2])]; })), { caps: [false, true], crease: 70 }), mat);
  const lr = o.legScale || 1;
  for (const sx of [-1, 1]) { const L = (sx < 0 ? o.legR : o.legL) || {};
    const hip = [sx * X(0.088) + hx, X(0.93), 0], knee = L.knee ? L.knee.map(X) : [sx * X(0.095), X(0.50), X(-0.01)], ank = L.ankle ? L.ankle.map(X) : [sx * X(0.10), X(0.085), 0];
    limb(m, [hip, V.lerp(hip, knee, 0.5), knee, V.lerp(knee, ank, 0.35), ank, V.add(ank, [0, -X(0.04), -X(0.012)])], [0.085, 0.074, 0.054, 0.056, 0.04, 0.036].map(r => X(r) * lr), mat, 8, { sz: 1.08 });
    const fs = o.foot || [0.045, 0.04, 0.115]; m.add(gSphere(1, 7, 4), mat, [ank[0], X(0.035) + (ank[1] - X(0.085)), ank[2] - X(0.04)], [0, sx * 8, 0], 'Body', { scale: fs.map(X) });
  }
  for (const sx of [-1, 1]) { const A = (sx < 0 ? o.armR : o.armL) || {}; const sh = up([sx * 0.188, 1.415, 0]);
    const e = up(A.e || [sx * 0.21, 1.13, 0.0]), w = up(A.w || [sx * 0.215, 0.88, -0.02]);
    limb(m, [sh, V.lerp(sh, e, 0.5), e, w], [0.058, 0.052, 0.045, 0.036].map(r => X(r) * (A.r || 1)), mat, 8);
    m.add(gSphere(X(0.062) * (A.r || 1), 7, 5), mat, V.add(sh, [-sx * X(0.01), X(0.002), 0]), [0, 0, 0], 'Body', { scale: [1, 0.92, 1.05] });   // deltoid: rounds the shoulder line
    if (A.hand !== false) { const d = V.nrm(V.sub(w, e)); m.add(gSphere(X(0.042), 8, 5), mat, V.add(w, V.scl(d, X(0.035))), [0, 0, 0], 'Body', { scale: [0.85, 1.15, 0.7] }); }
  }
  limb(m, [up([0, 1.455, 0]), hu([0, 1.565, 0.004])], [X(0.05), X(0.046)], mat, 8);
  const hc = hu([0, 1.635, o.headZ || 0]); const hs = o.headScale || [0.9, 1.1, 1.0];
  m.add(gSphere(X(0.105), o.n > 10 ? 14 : 10, o.n > 10 ? 10 : 7), mat, hc, [0, 0, 0], 'Body', { scale: hs });
  return { s, X, up, hu, hc, rotH: [-(o.lean || 0) - (o.headLean || 0), 0, -(o.side || 0)] };
}
const VIS = {
  anne: ['ziyaretçi · anne (çocuklu)', 1.66, 'Başörtülü kadın, uzun etek; kucağında battaniyeye sarılı çocuk (sağ omzunun üstünde başı), iki koluyla tutuyor; ağırlık bir bacakta.'],
  yasli: ['ziyaretçi · yaşlı', 1.58, 'Kambur yaşlı adam: fötr şapka, dizine kadar palto, dizler bükük, sağ elinde öne dayanmış baston.'],
  silahli: ['ziyaretçi · silahlı', 1.82, 'Kapüşonlu iri adam, bacaklar açık; tüfek göğsünde çapraz, namlu sol omzunun üstünde (gözetleme deliğinden görünür).'],
  asker: ['ziyaretçi · asker', 1.80, 'Miğferli asker: sırt çantası ve üstünde omuzlardan taşan battaniye rulosu, sağ omuzda askılı tüfek (namlu arkada yukarı), yanında matara, postal.'],
  yarali: ['ziyaretçi · yaralı', 1.72, 'Topallayan yaralı: sol kolu askıda (dirsek dışarı), sağ bacağı bükük ve yerden kesik, gövde sağlam bacağa yatık, başında sargı, paltosunun eteği yırtık.'],
  kopya: ['ziyaretçi · kopya (YZ)', 1.84, 'Fazla simetrik ve pürüzsüz: dimdik, kollar yanlarda, ayaklar bitişik, uzun boyun, yüzsüz yumurta baş; parlak malzeme (visitorCopy), önden geçen ince dikiş (steelBare).'],
  genel1: ['ziyaretçi · genel 1 (paltolu adam)', 1.78, 'Dizine kadar geniş palto, yakası kalkık, eller ceplerde, başı açık.'],
  genel2: ['ziyaretçi · genel 2 (çantalı kadın)', 1.65, 'Dizine kadar palto, topuz saç; sağ omzundan askılı çanta sol kalçasında, elleri askıda ve çantada.'],
  genel3: ['ziyaretçi · genel 3 (kasketli genç)', 1.70, 'İnce genç: kısa ceket, siperli kasket, eller ceplerde (dirsekler dışarı), hafif kambur.'],
};
export function Visitor(kind) {
  const [list, H, note] = VIS[kind];
  const m = new Model(`Visitor_${kind}`, { family: 'door', budget: 'unit', listName: list, origin: 'ayak tabanı ortası (y = 0), yüz −z (odaya)', note: note + ' Siluet yalnız dış hatla okunur; neredeyse siyah kumaş (visitor).' });
  const vm = kind === 'kopya' ? 'visitorCopy' : 'visitor';
  let F;
  if (kind === 'anne') {
    F = figure(m, { H, mat: vm, side: 3, hipX: 0.01, torso: [[0.93, 0.17, 0.112, 0], [1.03, 0.152, 0.10, 0], [1.16, 0.162, 0.11, -0.005], [1.29, 0.176, 0.118, -0.01], [1.39, 0.19, 0.108, 0], [1.445, 0.165, 0.088, 0], [1.485, 0.085, 0.066, 0]],
      coat: [[0.95, 0.175, 0.116], [0.70, 0.205, 0.132], [0.47, 0.232, 0.148], [0.29, 0.25, 0.158]], legL: { knee: [0.07, 0.5, 0], ankle: [0.075, 0.085, 0] }, legR: { knee: [-0.075, 0.5, -0.01], ankle: [-0.085, 0.085, 0.01] },
      armR: { e: [-0.20, 1.13, -0.07], w: [0.07, 1.08, -0.22] }, armL: { e: [0.255, 1.20, -0.06], w: [0.17, 1.38, -0.22] } });
    const { X, up } = F;
    m.add(gSphere(1, 10, 8), vm, up([0.09, 1.25, -0.16]), [0, 0, -42], 'Body', { scale: [X(0.12), X(0.215), X(0.11)] });
    m.add(gSphere(X(0.072), 10, 7), vm, up([0.235, 1.455, -0.12]), [0, 0, 0], 'Body');
    m.add(gSphere(1, 8, 6), vm, up([0.245, 1.49, -0.11]), [0, 0, -30], 'Body', { scale: [X(0.088), X(0.062), X(0.082)] }); // blanket fold over the child's head
    const sc = [[1.775, 0.012, 0.012, 0.01], [1.755, 0.075, 0.082, 0.01], [1.705, 0.116, 0.122, 0.012], [1.625, 0.127, 0.128, 0.02], [1.545, 0.122, 0.112, 0.028], [1.47, 0.172, 0.122, 0.012]];
    addU(m, gLoft(sc.map(([y, a, b, z], i) => ringH([0, y, z], a, b, 10).map(p => (i < 4 ? F.hu : up)(p))), { caps: [true, false], crease: 70 }), vm);
  } else if (kind === 'yasli') {
    F = figure(m, { H, mat: vm, lean: 17, headLean: 14, coat: [[0.95, 0.175, 0.12], [0.72, 0.19, 0.132], [0.52, 0.198, 0.136]],
      legL: { knee: [0.095, 0.5, -0.05], ankle: [0.10, 0.085, 0.02] }, legR: { knee: [-0.095, 0.5, -0.05], ankle: [-0.10, 0.085, 0.0] },
      armR: { e: [-0.23, 1.17, -0.10], w: [-0.25, 0.98, -0.25] }, armL: { e: [0.215, 1.14, -0.04], w: [0.205, 0.92, -0.10] } });
    const { X, up, hu } = F;
    const hand = up([-0.25, 0.94, -0.29]); const foot = [hand[0] - X(0.04), 0, hand[2] - X(0.09)];
    segCyl(m, foot, hand, 0.011, 0.011, 6, vm);
    m.add(gTorus(X(0.035), 0.011, 5, 8, Math.PI), vm, V.add(hand, [0, 0.008, X(0.035)]), [0, 90, 0], 'Body');
    const hp = hu([0, 1.715, 0.0]);
    m.lathe([[0.0, 0], [0.088, 0], [0.086, 0.055], [0.072, 0.09], [0, 0.097]].map(([r, y]) => [X(r), X(y)]), 12, vm, hp, F.rotH);
    m.lathe([[X(0.085), 0], [X(0.16), X(0.006)], [X(0.162), X(0.016)], [X(0.085), X(0.012)]], 14, vm, hp, F.rotH, 'Body', { caps: false });
  } else if (kind === 'silahli') {
    F = figure(m, { H, mat: vm, ws: 1.08, ds: 1.12, coat: [[0.95, 0.19, 0.125], [0.80, 0.205, 0.132]], legScale: 1.08,
      legL: { knee: [0.16, 0.5, -0.01], ankle: [0.21, 0.085, 0] }, legR: { knee: [-0.16, 0.5, -0.01], ankle: [-0.21, 0.085, 0] },
      armR: { e: [-0.25, 1.12, -0.07], w: [-0.12, 1.05, -0.21], r: 1.1 }, armL: { e: [0.25, 1.2, -0.08], w: [0.085, 1.40, -0.21], r: 1.1 } });
    const { X, up } = F;
    const a = up([-0.20, 0.93, -0.18]), b = up([0.23, 1.86, -0.13]); const d = V.nrm(V.sub(b, a)); const at = (t) => V.add(a, V.scl(d, t));
    segBox(m, a, at(X(0.26)), X(0.055), X(0.04), 0.008, vm); segBox(m, at(X(0.24)), at(X(0.62)), X(0.05), X(0.05), 0.008, vm);
    segCyl(m, at(X(0.6)), b, 0.012, 0.011, 6, vm);
    const pp = V.nrm(V.cross(d, [0, 0, -1])); segBox(m, at(X(0.45)), V.add(at(X(0.45)), V.scl(pp, X(0.13))), X(0.035), X(0.05), 0.006, vm);
    const hd = [[1.815, 0.012, 0.012, -0.03], [1.77, 0.10, 0.112, -0.005], [1.69, 0.135, 0.142, 0.012], [1.59, 0.132, 0.132, 0.02], [1.50, 0.185, 0.132, 0.012]];
    addU(m, gLoft(hd.map(([y, a2, b2, z], i) => ringH([0, y, z], a2, b2, 10, { fz: i === 2 ? 1.12 : 1 }).map(p => (i < 4 ? F.hu : up)(p))), { caps: [true, false], crease: 70 }), vm);
  } else if (kind === 'asker') {
    F = figure(m, { H, mat: vm, ws: 1.05, legL: { knee: [0.10, 0.5, 0], ankle: [0.11, 0.09, 0] }, legR: { knee: [-0.10, 0.5, 0], ankle: [-0.11, 0.09, 0] }, foot: [0.05, 0.048, 0.125],
      armR: { e: [-0.225, 1.14, 0.0], w: [-0.22, 0.90, -0.04] }, armL: { e: [0.225, 1.14, 0.0], w: [0.225, 0.90, -0.03] } });
    const { X, up, hu } = F;
    m.box(X(0.36), X(0.44), X(0.2), 0.02, vm, up([0, 1.24, 0.2]), [-(0), 0, 0]);
    segCyl(m, up([-0.29, 1.50, 0.17]), up([0.29, 1.50, 0.17]), X(0.065), X(0.065), 10, vm);
    segBox(m, up([-0.15, 0.96, 0.13]), up([-0.23, 1.62, 0.11]), X(0.05), X(0.04), 0.006, vm); segCyl(m, up([-0.23, 1.6, 0.11]), up([-0.255, 1.93, 0.10]), 0.011, 0.01, 6, vm);
    m.cyl(X(0.05), X(0.15), 10, 0.01, vm, up([0.205, 0.98, 0.03]), [0, 0, 0]);
    m.lathe([[0.146, 0], [0.142, 0.012], [0.128, 0.03], [0.124, 0.06], [0.105, 0.105], [0.06, 0.135], [0, 0.143]].map(([r, y]) => [X(r), X(y)]), 14, vm, hu([0, 1.655, 0.005]), F.rotH);
    for (const sx of [-1, 1]) limb(m, [up([sx * 0.10, 0.09, 0]).map((v, i) => i === 1 ? X(0.09) : v), up([sx * 0.10, 0.26, 0]).map((v, i) => i === 1 ? X(0.26) : v)], [X(0.058), X(0.058)], vm, 8);
  } else if (kind === 'yarali') {
    F = figure(m, { H, mat: vm, side: 7, hipX: 0.03, headLean: 8, coat: [[0.95, 0.172, 0.116], [0.72, 0.19, 0.13], [0.56, 0.198, 0.134, 0, -0.06]],
      legL: { knee: [0.12, 0.5, 0], ankle: [0.125, 0.085, 0] }, legR: { knee: [-0.07, 0.52, -0.08], ankle: [-0.08, 0.15, 0.07] },
      armL: { e: [0.25, 1.15, -0.02], w: [-0.04, 1.20, -0.17] }, armR: { e: [-0.26, 1.16, 0.0], w: [-0.31, 0.94, -0.04] } });
    const { X, up, hu } = F;
    segBox(m, up([-0.10, 1.45, -0.075]), up([0.10, 1.17, -0.17]), X(0.07), X(0.018), 0.004, vm);
    segBox(m, up([0.22, 1.16, -0.06]), up([-0.04, 1.17, -0.18]), X(0.10), X(0.03), 0.006, vm);
    m.add(gTorus(X(0.101), X(0.016), 5, 14), vm, hu([0, 1.665, 0.004]), [90 + F.rotH[0] + 18, 0, F.rotH[2] + 28], 'Body');
  } else if (kind === 'kopya') {
    F = figure(m, { H, mat: vm, n: 14, torso: [[0.93, 0.158, 0.105, 0], [1.03, 0.142, 0.096, 0], [1.16, 0.15, 0.10, 0], [1.29, 0.17, 0.108, 0], [1.39, 0.186, 0.10, 0], [1.445, 0.16, 0.084, 0], [1.50, 0.07, 0.06, 0]],
      legL: { knee: [0.08, 0.5, 0], ankle: [0.075, 0.085, 0] }, legR: { knee: [-0.08, 0.5, 0], ankle: [-0.075, 0.085, 0] }, headScale: [0.84, 1.2, 0.92], headZ: 0, foot: [0.042, 0.038, 0.11],
      armL: { e: [0.205, 1.12, 0.0], w: [0.203, 0.86, 0.0] }, armR: { e: [-0.205, 1.12, 0.0], w: [-0.203, 0.86, 0.0] } });
    const { X, up, hc } = F;
    const fr = [[0.95, 0.106], [1.16, 0.101], [1.29, 0.109], [1.39, 0.101], [1.47, 0.07]].map(([y, b]) => up([0, y, -b - 0.002]));
    for (let i = 0; i < fr.length - 1; i++) segBox(m, fr[i], fr[i + 1], 0.006, 0.004, 0, 'steelBare', 'Body', { cast: false });
    for (const [a1, a2] of [[-70, -20], [-20, 30], [30, 75]]) { const p1 = [hc[0], hc[1] + X(0.105 * 1.2) * Math.sin(a1 * D2R), hc[2] - X(0.105 * 0.92) * Math.cos(a1 * D2R) - 0.002];
      const p2 = [hc[0], hc[1] + X(0.105 * 1.2) * Math.sin(a2 * D2R), hc[2] - X(0.105 * 0.92) * Math.cos(a2 * D2R) - 0.002]; segBox(m, p1, p2, 0.006, 0.004, 0, 'steelBare', 'Body', { cast: false }); }
  } else if (kind === 'genel1') {
    F = figure(m, { H, mat: vm, ws: 1.04, coat: [[0.95, 0.19, 0.125], [0.72, 0.205, 0.136], [0.50, 0.214, 0.142]],
      armL: { e: [0.24, 1.14, 0.01], w: [0.17, 0.97, -0.085], hand: false, r: 1.12 }, armR: { e: [-0.24, 1.14, 0.01], w: [-0.17, 0.97, -0.085], hand: false, r: 1.12 } });
    addU(m, gLoft([ringH([0, 1.46, 0.005], 0.115, 0.09, 10), ringH([0, 1.585, 0.018], 0.098, 0.088, 10, { fz: 0.75 })].map(r => r.map(F.up)), { caps: [false, false], crease: 70 }), vm);
  } else if (kind === 'genel2') {
    F = figure(m, { H, mat: vm, legScale: 0.88, foot: [0.038, 0.034, 0.1], coat: [[0.95, 0.172, 0.115], [0.72, 0.186, 0.126], [0.55, 0.192, 0.13]],
      legL: { knee: [0.085, 0.5, 0], ankle: [0.085, 0.085, 0] }, legR: { knee: [-0.085, 0.5, -0.02], ankle: [-0.09, 0.085, 0.01] },
      armL: { e: [0.235, 1.14, 0.02], w: [0.262, 0.99, -0.045] }, armR: { e: [-0.22, 1.15, -0.03], w: [-0.07, 1.30, -0.13] } });
    const { X, up, hu } = F;
    m.box(X(0.25), X(0.17), X(0.08), 0.012, vm, [X(0.245), X(0.92), X(0.0)], [0, 8, -6]);
    segBox(m, up([-0.13, 1.44, -0.05]), up([-0.02, 1.25, -0.125]), X(0.026), 0.008, 0.002, vm); segBox(m, up([-0.02, 1.25, -0.125]), [X(0.22), X(1.0), X(-0.02)], X(0.026), 0.008, 0.002, vm);
    m.add(gSphere(X(0.05), 8, 6), vm, hu([0, 1.665, 0.098]), [0, 0, 0], 'Body');
  } else if (kind === 'genel3') {
    F = figure(m, { H, mat: vm, lean: 5, legScale: 0.9, ws: 0.96, coat: [[0.95, 0.172, 0.114], [0.86, 0.178, 0.117]],
      armL: { e: [0.245, 1.12, -0.02], w: [0.12, 1.0, -0.115], hand: false }, armR: { e: [-0.245, 1.12, -0.02], w: [-0.12, 1.0, -0.115], hand: false } });
    const { X, hu } = F;
    const cp = hu([0, 1.70, 0.004]);
    m.lathe([[0.1, 0], [0.1, 0.028], [0.086, 0.058], [0, 0.07]].map(([r, y]) => [X(r), X(y)]), 12, vm, cp, F.rotH);
    m.box(X(0.17), 0.01, X(0.085), 0.003, vm, hu([0, 1.705, -0.12]), [F.rotH[0] - 8, 0, 0]);
  }
  m.anchor('Head', { pos: F.hc, note: 'baş merkezi (gözetleme deliği görüşü / ışık hedefi)' });
  m.meta.place = [{ pos: [2.75, 0, 0.75], yaw: 90, note: 'dünya; kapının hemen ardında, kanat açıkken (Corridor.Visitor çapası). Kalabalık: genel1–3 arkada ±0,35 m yana, +0,5 m geriye' }];
  return m;
}

export const MODELS = {
  DoorFrame: { build: DoorFrame },
  DoorLeaf: { build: DoorLeaf },
  KnockLamp: { build: KnockLamp },
  IntercomBox: { build: IntercomBox },
  HazardHeader: { build: HazardHeader },
  Corridor: { build: Corridor },
  Handrail: { build: Handrail },
  CorridorLamp: { build: CorridorLamp },
  ...Object.fromEntries(Object.keys(VIS).map(k => [`Visitor_${k}`, { build: () => Visitor(k) }])),
};
