// family: menu (Giris landing) · Rev3 model library (see DESIGN_R3.md §3.1)
// Unity local coordinates (x right, y up, z forward/away); fronts face −z. Wall-mounted models: origin = centre of the
// back face's bottom edge... see each model's `origin`.
import { Model, gBox, gCyl, gLathe, gExtrude, gTorus, gRing, roundedRect, circlePts, D2R } from '../mlib.js';
import { defMat } from '../mats.js';

defMat('fuseCeramic', { hex: '#E4DDCB', rough: 0.4, metal: 0, note: 'sigorta gövdesi (porselen)' });

// ---------------------------------------------------------------- Son şalter (ANA ŞALTER) — knife switch under a lifting guard
export function LastSwitch() {
  const m = new Model('LastSwitch', { family: 'menu', budget: 'big', listName: 'Son şalter (bıçaklı, kapaklı)', origin: 'arka yüz, alt kenar ortası (duvar düzlemi z = 0)',
    note: 'Sahanlık duvarında ANA ŞALTER. Kapak (koruyucu başlık) üst menteşeden 115° kalkar; altında arduvaz pano üstünde iki kutuplu bıçaklı şalter. Kol +55° = AÇIK (bıçaklar yukarıda, kontakta), −55° = KESİK (öne ve aşağı). Amber lamba ŞEBEKE: ışık varken yanar.' });
  const W = 0.30, H = 0.50, D = 0.13;           // mounting plate and guard envelope
  // mounting plate (pressed steel, rolled edge), wall at z = 0
  m.box(W, H, 0.012, 0.004, 'paintOliveDark', [0, H / 2, -0.006]);
  m.box(W - 0.03, H - 0.03, 0.006, 0.002, 'paintOlive', [0, H / 2, -0.015]);
  for (const sx of [-1, 1]) for (const sy of [0.03, H - 0.03]) m.cyl(0.006, 0.006, 8, 0.002, 'steelBare', [sx * (W / 2 - 0.022), sy, -0.015], [90, 0, 0]); // wall bolts
  // lamp housing (fixed top block) with amber jewel and a short conduit to the ceiling
  m.box(W, 0.085, D, 0.008, 'paintOliveDark', [0, H - 0.0425, -D / 2]);
  m.cyl(0.024, 0.016, 16, 0.003, 'chrome', [0.09, H - 0.0425, -D - 0.006], [90, 0, 0]);                         // bezel
  m.part('Lamp', { pivot: [0.09, H - 0.0425, -D - 0.012], kind: 'swap', note: 'malzeme: jewelAmberOn (ışık var) / jewelAmberOff' });
  m.lathe([[0, 0], [0.016, 0], [0.017, 0.008], [0.012, 0.018], [0, 0.021]], 12, 'jewelAmberOn', [0.09, H - 0.0425, -D - 0.012], [-90, 0, 0], 'Lamp');
  m.cyl(0.024, 0.03, 12, 0.004, 'steelCast', [-0.07, H + 0.012, -0.05]);                                       // gland
  // slate panel + knife switch (two poles)
  const py = 0.235; // panel centre height
  m.box(0.22, 0.30, 0.02, 0.005, 'slate', [0, py, -0.028]);
  for (const sx of [-0.05, 0.05]) {
    m.box(0.024, 0.03, 0.03, 0.004, 'copper', [sx, py - 0.10, -0.051]);                                        // hinge clip
    m.box(0.03, 0.035, 0.028, 0.004, 'copper', [sx, py + 0.10, -0.05]);                                        // contact jaws
    m.box(0.008, 0.035, 0.03, 0.002, 'copper', [sx - 0.011, py + 0.10, -0.066]); m.box(0.008, 0.035, 0.03, 0.002, 'copper', [sx + 0.011, py + 0.10, -0.066]);
    m.lathe([[0, 0], [0.013, 0], [0.014, 0.006], [0.014, 0.05], [0.013, 0.056], [0, 0.056]], 12, 'fuseCeramic', [sx + 0.028, py - 0.135, -0.055], [0, 0, 90]); // fuse cartridge (horizontal)
    m.cyl(0.006, 0.012, 8, 0.002, 'steelBare', [sx - 0.034, py - 0.135, -0.055], [0, 0, 90]); m.cyl(0.006, 0.012, 8, 0.002, 'steelBare', [sx + 0.034, py - 0.135, -0.055], [0, 0, 90]);
  }
  // Lever: blades hinge at py − 0.10, z −0.051; geometry modelled at 0° = blades 55° out of the panel (35° above horizontal)
  const hz = -0.051, hy = py - 0.10; const L = 0.2;
  m.part('Lever', { pivot: [0, hy, hz], axis: 'x', range: [-55, 55], rest: 55, note: '+55 AÇIK (bıçak dik, kontakta) · −55 KESİK (öne, aşağı)' });
  const a = 35 * D2R; const dir = [0, Math.sin(a), -Math.cos(a)];                 // blade direction at 0°
  const at = (t, sx = 0) => [sx, hy + dir[1] * t, hz + dir[2] * t];
  for (const sx of [-0.05, 0.05]) m.box(0.008, 0.026, L, 0.002, 'copper', at(L / 2, sx), [35, 0, 0], 'Lever');
  m.cyl(0.01, 0.13, 10, 0.002, 'steelBare', at(0), [0, 0, 90], 'Lever');                                     // hinge pin
  m.box(0.15, 0.03, 0.026, 0.006, 'bakeliteBlack', at(L * 0.86), [35, 0, 0], 'Lever');                       // crossbar
  // handle perpendicular to the blades: points forward-down at 0°, straight out of the panel at ON (+55°)
  const hb = at(L * 0.86), hd = [0, -Math.cos(a), -Math.sin(a)]; const hp = (t) => [0, hb[1] + hd[1] * t, hb[2] + hd[2] * t];
  m.cyl(0.009, 0.03, 10, 0.002, 'steelBare', hp(0.028), [-145, 0, 0], 'Lever');
  m.lathe([[0, 0], [0.015, 0], [0.018, 0.01], [0.016, 0.04], [0.011, 0.047], [0, 0.048]], 14, 'bakeliteBlack', hp(0.04), [-145, 0, 0], 'Lever');
  // Cover (guard hood): hinge along the top of the switch bay, y = H − 0.085; modelled closed (hangs down)
  const cy0 = H - 0.085; const ch = 0.39, cw = 0.29, cd = 0.115;
  m.part('Cover', { pivot: [0, cy0, -D], axis: 'x', range: [0, 115], rest: 0, note: 'koruyucu başlık; 1. tık 115° kalkar, 4 s sonra kendiliğinden iner' });
  m.box(cw, ch, 0.012, 0.004, 'paintOliveDark', [0, cy0 - ch / 2, -D - 0.006], [0, 0, 0], 'Cover');                     // front face
  m.box(cw - 0.03, 0.09, 0.004, 0.001, 'paintHazard', [0, cy0 - ch + 0.07, -D - 0.0135], [0, 0, 0], 'Cover');           // hazard band (texture)
  for (const sx of [-1, 1]) m.box(0.01, ch, cd - 0.012, 0.003, 'paintOliveDark', [sx * (cw / 2 - 0.005), cy0 - ch / 2, -D + (cd - 0.012) / 2], [0, 0, 0], 'Cover'); // side cheeks
  m.box(cw, 0.01, cd - 0.012, 0.003, 'paintOliveDark', [0, cy0 - ch + 0.005, -D + (cd - 0.012) / 2], [0, 0, 0], 'Cover'); // bottom lip
  m.box(0.07, 0.014, 0.03, 0.005, 'steelBare', [0, cy0 - ch - 0.004, -D - 0.018], [0, 0, 0], 'Cover');                   // lifting tab
  m.cyl(0.008, cw - 0.02, 10, 0, 'steelCast', [0, cy0, -D - 0.006], [0, 0, 90], 'Cover');                               // piano hinge knuckle
  // brass plate below (fixed bilingual): ANA ŞALTER / MAIN SWITCH
  m.box(0.30, 0.085, 0.006, 0.002, 'brass', [0, -0.07, -0.004]);
  for (const sx of [-0.135, 0.135]) m.cyl(0.005, 0.004, 8, 0.0015, 'brassWorn', [sx, -0.07, -0.008], [90, 0, 0]);
  m.text('TXT_PlateTR', { center: [0, -0.055, -0.0072], size: [0.25, 0.034], role: 'plateLabel (sabit, iki dilli)', capMm: 22, sample: 'ANA ŞALTER' });
  m.text('TXT_PlateEN', { center: [0, -0.095, -0.0072], size: [0.22, 0.018], role: 'plateLabel küçük', capMm: 12, sample: 'MAIN SWITCH' });
  m.text('TXT_Lamp', { center: [0.035, H - 0.0425, -D - 0.0005], size: [0.07, 0.02], role: 'plateLabel', capMm: 9, sample: 'ŞEBEKE', sampleEn: 'MAINS' });
  m.light('LampGlow', { part: 'Lamp', pos: [0.09, H - 0.0425, -D - 0.03], type: 'emissive', note: 'jewelAmberOn ↔ jewelAmberOff; ışık bileşeni yok' });
  m.anchor('Hover', { pos: [0, 0.25, -D - 0.12], note: 'üzerine gelince kalkış ve kâğıt etiket bu noktaya göre' });
  return m;
}


// ---------------------------------------------------------------- Puantaj saati (time recorder) — hero of the M2 pose
defMat('clockHand', { hex: '#1A1714', rough: 0.4, metal: 0.3, note: 'saat ibresi (siyah boyalı çelik)' });
defMat('counterDrum', { hex: '#1C150E', rough: 0.45, metal: 0.2, tex: 'tx_digits (11 hücreli rakam şeridi 0–9 + 0, krem #EFDCAE)', note: 'sayaç tekeri (masadakiyle aynı)' });
export function PunchClock() {
  const m = new Model('PunchClock', { family: 'menu', budget: 'big', listName: 'Puantaj saati', origin: 'arka yüz, alt kenar ortası (duvar düzlemi z = 0)',
    note: 'Duvara asılı vardiya saati. Kart üstteki pirinç ağızdan 70 mm girer; sağdaki mantar başlı itici 12 mm iner (“ka-çunk”), kart delinir. Kadranın altındaki üç teker vardiya numarasını gösterir (masadaki sayaçların eşi). Amber lamba kart kabul edilince 0,3 s yanar.' });
  const W = 0.30, H = 0.46, D = 0.165;
  // case: pressed steel with a separate front frame and a raked top
  m.box(W, H - 0.04, D, 0.006, 'paintGreyGreen', [0, (H - 0.04) / 2, -D / 2]);
  m.add(gExtrude([[-D, 0], [0, 0], [0, 0.04], [-D + 0.035, 0.04]], W - 0.004, 0.004), 'paintGreyGreen', [0, H - 0.04, 0], [0, -90, 0]); // raked lid (profile in z-y)
  m.box(W + 0.012, 0.018, D + 0.008, 0.005, 'paintOliveDark', [0, 0.009, -D / 2 - 0.002]);                      // plinth band
  // dial: chrome bezel + glass + cream dial + hands (parts)
  const dc = [0, 0.315, -D]; const R = 0.098;
  m.add(gRingY(R + 0.012, 0.016, 0.022, 32, 0.004), 'chrome', [dc[0], dc[1], dc[2] - 0.006], [-90, 0, 0]);
  m.cyl(R, 0.004, 32, 0, 'dialCream', [dc[0], dc[1], dc[2] - 0.002], [-90, 0, 0]);
  m.cyl(R + 0.002, 0.003, 32, 0, 'glassClear', [dc[0], dc[1], dc[2] - 0.016], [-90, 0, 0], 'Body', { cast: false });
  m.part('HourHand', { pivot: [dc[0], dc[1], dc[2] - 0.006], axis: 'z', range: [0, -360], note: 'akrep; 12 saatte bir tur (ya da sabit 05:50)' });
  m.add(gExtrude([[-0.005, -0.012], [0.005, -0.012], [0.0035, 0.052], [0, 0.058], [-0.0035, 0.052]], 0.002, 0), 'clockHand', [dc[0], dc[1], dc[2] - 0.006], [0, 0, 0], 'HourHand', { cast: false });
  m.part('MinuteHand', { pivot: [dc[0], dc[1], dc[2] - 0.009], axis: 'z', range: [0, -360], note: 'yelkovan' });
  m.add(gExtrude([[-0.004, -0.016], [0.004, -0.016], [0.0025, 0.078], [0, 0.084], [-0.0025, 0.078]], 0.002, 0), 'clockHand', [dc[0], dc[1], dc[2] - 0.009], [0, 0, 0], 'MinuteHand', { cast: false });
  m.cyl(0.007, 0.006, 12, 0.001, 'chrome', [dc[0], dc[1], dc[2] - 0.012], [-90, 0, 0]);
  // shift counter window (3 wheels) under the dial: brass bezel, wheels as parts
  const cw = [0, 0.168, -D];
  m.box(0.112, 0.046, 0.008, 0.002, 'brass', [cw[0], cw[1], cw[2] - 0.004]);
  m.box(0.094, 0.032, 0.004, 0.001, 'counterFace', [cw[0], cw[1], cw[2] - 0.008]);
  for (let i = 0; i < 3; i++) { const x = (i - 1) * 0.029; m.part(`Wheel${i}`, { pivot: [x, cw[1], cw[2] + 0.008], axis: 'x', range: [0, 360], note: '36° / rakam; V.nnn' });
    m.cyl(0.022, 0.024, 16, 0.002, 'counterDrum', [x, cw[1], cw[2] + 0.008], [0, 0, 90], `Wheel${i}`); }
  m.text('TXT_ShiftLabel', { center: [0, 0.134, -D - 0.0002], size: [0.11, 0.014], role: 'plateLabel (gravür)', capMm: 7, sample: 'VARDİYA', sampleEn: 'SHIFT' });
  // card throat on the lid: brass funnel with a slot 0.10 × 0.006
  const ty = H - 0.006, tz = -D + 0.06;
  for (const [w, d, x, z] of [[0.13, 0.014, 0, tz - 0.012], [0.13, 0.014, 0, tz + 0.012], [0.014, 0.038, -0.058, tz], [0.014, 0.038, 0.058, tz]]) m.box(w, 0.012, d, 0.003, 'brass', [x, ty + 0.004, z]);
  m.box(0.104, 0.004, 0.008, 0, 'punchHole', [0, ty + 0.001, tz], [0, 0, 0], 'Body', { cast: false });
  m.anchor('CardSlot', { pos: [0, ty + 0.002, tz], note: 'kart buradan dik iner; 70 mm içeri (kart orijini alt kenarda)' });
  // plunger: chrome shaft + bakelite mushroom knob, top right
  m.cyl(0.02, 0.012, 16, 0.003, 'steelCast', [0.105, H + 0.006 - 0.004, -D + 0.07]);
  m.part('Plunger', { pivot: [0.105, H + 0.004, -D + 0.07], kind: 'slide', axis: 'y', range: [0, -0.012], note: 'basınca 12 mm iner, yayla döner' });
  m.cyl(0.006, 0.05, 10, 0.001, 'chrome', [0.105, H + 0.03, -D + 0.07], [0, 0, 0], 'Plunger');
  m.lathe([[0, 0], [0.012, 0], [0.019, 0.004], [0.021, 0.012], [0.017, 0.02], [0, 0.023]], 18, 'bakeliteBlack', [0.105, H + 0.052, -D + 0.07], [0, 0, 0], 'Plunger');
  // ready lamp (amber jewel) left of the throat on the lid front edge
  m.cyl(0.013, 0.008, 14, 0.002, 'chrome', [-0.105, H + 0.002, -D + 0.07]);
  m.part('Lamp', { pivot: [-0.105, H + 0.006, -D + 0.07], kind: 'swap', note: 'jewelAmberOn kart kabul edilince 0,3 s; aksi hâlde jewelAmberOff' });
  m.lathe([[0, 0], [0.009, 0], [0.0095, 0.004], [0.006, 0.009], [0, 0.011]], 12, 'jewelAmberOff', [-0.105, H + 0.006, -D + 0.07], [0, 0, 0], 'Lamp');
  // brass plates on the lower front
  m.box(0.22, 0.05, 0.005, 0.0015, 'brass', [0, 0.085, -D - 0.0025]);
  for (const sx of [-0.1, 0.1]) m.cyl(0.004, 0.003, 8, 0.001, 'brassWorn', [sx, 0.085, -D - 0.006], [90, 0, 0], 'Body', { cast: false });
  m.text('TXT_Plate', { center: [0, 0.085, -D - 0.0052], size: [0.18, 0.03], role: 'plateLabel', capMm: 16, sample: 'PUANTAJ', sampleEn: 'TIME CLOCK' });
  m.text('TXT_Maker', { center: [0, 0.042, -D - 0.0002], size: [0.2, 0.012], role: 'plateLabel küçük (boya)', capMm: 6, sample: 'KYGA · SİVİL SAVUNMA', sampleEn: 'KYGA · CIVIL DEFENCE' });
  // side lock + wall bolts
  m.cyl(0.011, 0.006, 12, 0.002, 'chrome', [W / 2 + 0.002, 0.3, -0.06], [0, 0, 90]);
  m.light('LampGlow', { part: 'Lamp', pos: [-0.105, H + 0.02, -D + 0.07], type: 'emissive', note: 'ışık bileşeni yok' });
  m.meta.place = [{ pos: [2.2, 1.27, -0.12], yaw: -90, note: 'sahanlık kapı duvarı, kart rafının sağı (menü sahnesi)' }];
  return m;
}
function gRingY(r, t, h, seg, c) { return gRing(r, t, h, seg, c); }

// ---------------------------------------------------------------- Kart rafı (card rack) and cards
export function CardRack() {
  const m = new Model('CardRack', { family: 'menu', budget: 'small', listName: 'Kart rafı', origin: 'arka yüz, alt kenar ortası',
    note: 'Cilalı ahşap pano üstünde tek çelik cep: komutanın kartı (kayıt varsa) cebe 46 mm girer, başlığı ve ızgaranın üst sıraları görünür. Cebin üstünde pirinç etiket çerçevesi (KOMUTAN). Kayıt yokken cep boş. Altında boş kart destesi (ayrı model BlankDeck).' });
  const W = 0.13, H = 0.34;
  m.box(W, H, 0.018, 0.004, 'woodVarnish', [0, H / 2, -0.009]);
  const y = 0.10, pz = -0.026;                         // pocket centre height and depth
  m.box(0.11, 0.06, 0.004, 0.0015, 'paintGreyGreen', [0, y, pz - 0.006]);
  m.box(0.11, 0.004, 0.012, 0.0015, 'paintGreyGreen', [0, y - 0.028, pz]);
  for (const sx of [-1, 1]) m.box(0.004, 0.06, 0.012, 0.0012, 'paintGreyGreen', [sx * 0.053, y, pz]);
  m.cyl(0.0035, 0.11, 8, 0, 'paintGreyGreen', [0, y + 0.03, pz - 0.006], [0, 0, 90], 'Body', { cast: false });
  // label frame above the card
  m.box(0.07, 0.022, 0.004, 0.0012, 'brass', [0, 0.30, -0.02]);
  m.box(0.062, 0.015, 0.002, 0, 'cardBlank', [0, 0.30, -0.0225], [0, 0, 0], 'Body', { cast: false });
  m.text('TXT_Pocket', { center: [0, 0.30, -0.0237], size: [0.058, 0.012], role: 'slipCode (etiket)', capMm: 5, sample: 'KOMUTAN', sampleEn: 'COMMANDER' });
  for (const yy of [0.03, H - 0.02]) m.cyl(0.005, 0.004, 8, 0.0015, 'brassWorn', [0, yy, -0.0185], [90, 0, 0], 'Body', { cast: false });
  m.anchor('CardTop', { pos: [0, y - 0.026, -0.03], note: 'komutanın kartının alt kenarı (kart cebe 46 mm girer, 138 mm görünür)' });
  m.anchor('DeckSeat', { pos: [0, -0.19, -0.02], note: 'BlankDeck rafın 0,19 m altında' });
  m.meta.place = [{ pos: [2.2, 1.14, -0.55], yaw: -90, note: 'menü: saatin solunda' }];
  return m;
}
export function ShiftCard() {
  const m = new Model('ShiftCard', { family: 'menu', budget: 'small', listName: 'Vardiya kartı', origin: 'alt kenar ortası (devrilme ekseni)',
    note: 'Krem karton 90 × 184 mm. Üstte KOMUTAN · SIĞINAK 07, altında V.nnn · özgür x/12 (TMP). 8 × 10 zımba ızgarası (1–80 rakamları dokuda); her biten vardiya için bir delik açılır (PunchHole örnekleri, konumlar çapa listesinde). Kartın kendisi yerel x’te −14° öne devrilir (yeni harekât onayı).' });
  const W = 0.09, H = 0.184, T = 0.0008;
  m.part('Tilt', { pivot: [0, 0, 0], axis: 'x', range: [0, -14], note: 'yeni harekât onayında eski kart öne devrilir' });
  m.box(W, H, T, 0, 'cardStock', [0, H / 2, 0], [0, 0, 0], 'Tilt', { cast: false });
  m.text('TXT_Role', { part: 'Tilt', center: [-0.003, H - 0.010, -T / 2 - 0.0002], size: [0.08, 0.010], role: 'kart başlığı (Courier Prime 700, ikincil ≥14 px)', capMm: 6.5, align: 'left', sample: 'KOMUTAN', sampleEn: 'COMMANDER' });
  m.text('TXT_Shelter', { part: 'Tilt', center: [-0.003, H - 0.021, -T / 2 - 0.0002], size: [0.08, 0.010], role: 'kart başlığı 2 (ikincil)', capMm: 6.5, align: 'left', sample: 'SIĞINAK 07', sampleEn: 'SHELTER 07' });
  m.text('TXT_Shift', { part: 'Tilt', center: [-0.003, H - 0.036, -T / 2 - 0.0002], size: [0.08, 0.015], role: 'kart vardiyası (CP 700, gövde ≥18 px)', capMm: 10, align: 'left', sample: 'V.014', sampleEn: 'S.014' });
  m.text('TXT_Free', { part: 'Tilt', center: [-0.003, H - 0.052, -T / 2 - 0.0002], size: [0.08, 0.013], role: 'kart özeti (gövde ≥18 px)', capMm: 8.5, align: 'left', sample: 'özgür 3/12', sampleEn: 'free 3/12' });
  m.text('TXT_Sign', { part: 'Tilt', center: [0.012, 0.012, -T / 2 - 0.0002], size: [0.056, 0.01], role: 'handNote (Kalam) imza', capMm: 4, sample: 'K. — 07', note: 'el yazısı paraf' });
  // punch grid: 10 columns × 8 rows, cells 7.6 × 10 mm, origin top-left cell centre
  const grid = { cols: 10, rows: 8, pitch: [0.0076, 0.0105], first: [-0.0342, H - 0.070, -T / 2], holeDia: 0.0042, order: 'satır satır, soldan sağa (1–80)' };
  m.meta.punchGrid = grid;
  for (let k = 0; k < 80; k++) { const c = k % 10, r = Math.floor(k / 10); m.anchor(`H${String(k + 1).padStart(2, '0')}`, { part: 'Tilt', pos: [grid.first[0] + c * grid.pitch[0], grid.first[1] - r * grid.pitch[1], grid.first[2]] }); }
  return m;
}
export function PunchHole() {
  const m = new Model('PunchHole', { family: 'menu', budget: 'small', listName: 'Zımba deliği', origin: 'delik merkezi',
    note: 'Kartın ön yüzüne 0,1 mm önde siyah sekizgen; vardiya bitince ilgili çapada (H01–H80) görünür olur. 0,3 s’de ölçek 0 → 1.' });
  m.add(gDiscZ(0.0021, 8), 'punchHole', [0, 0, -0.0001], [0, 0, 0], 'Body', { cast: false });
  return m;
}
export function CancelSlip() {
  const m = new Model('CancelSlip', { family: 'menu', budget: 'small', listName: 'İPTAL EDİLİR fişi', origin: 'ataşın tepesi',
    note: 'Eski kartın üstüne ataşla takılan dar fiş: kırmızı çift çizgili damga “İPTAL EDİLİR · V.014” (TMP, colors.stamp.failed). Kıskaç kalkınca 0,15 s’de düşer, kapanınca kalkar.' });
  m.box(0.07, 0.03, 0.0006, 0, 'cardBlank', [0, -0.017, -0.0012], [0, 0, -4], 'Body', { cast: false });
  m.add(gTorus(0.006, 0.0009, 4, 12, Math.PI), 'steelBare', [0.018, -0.002, -0.0018], [0, 0, 90], 'Body', { cast: false });
  m.box(0.0018, 0.018, 0.0018, 0, 'steelBare', [0.012, -0.008, -0.0018], [0, 0, 0], 'Body', { cast: false });
  m.text('TXT_Stamp', { center: [0, -0.017, -0.0018], size: [0.064, 0.022], role: 'stamp (çift kenar #9A1E14)', capMm: 5, sample: 'İPTAL EDİLİR · V.014', sampleEn: 'TO BE CANCELLED · S.014' });
  return m;
}
export function BlankDeck() {
  const m = new Model('BlankDeck', { family: 'menu', budget: 'small', listName: 'Boş kart destesi ve kıskacı', origin: 'tepsinin arka alt kenarı',
    note: 'Açık çelik tepside 40 boş kart (kenar çizgileri dokuda) ve yaylı kıskaç. Kayıt varken 1. tık kıskacı −70° kaldırır, 4 s içinde 2. tık yeni harekâtı onaylar; süre dolarsa kapanır. Kayıt yoksa tek tık.' });
  const W = 0.112, D = 0.03;
  m.box(W, 0.004, D, 0.0012, 'paintGreyGreen', [0, 0.002, -D / 2]);
  m.box(W, 0.06, 0.003, 0.001, 'paintGreyGreen', [0, 0.03, -D + 0.0015]);
  for (const sx of [-1, 1]) m.box(0.003, 0.05, D, 0.001, 'paintGreyGreen', [sx * (W / 2 - 0.0015), 0.025, -D / 2]);
  m.box(0.092, 0.172, 0.012, 0.0006, 'cardBlank', [0, 0.004 + 0.086, -0.016]);   // the stack, standing
  m.text('TXT_Deck', { center: [0, 0.15, -0.0222], size: [0.08, 0.012], role: 'slipCode → kart başlığı', capMm: 4.2, sample: 'YENİ HAREKÂT', sampleEn: 'NEW CAMPAIGN' });
  // clip: hinge on top of the back plate, spring bar presses the stack top
  m.part('Clip', { pivot: [0, 0.178, -0.006], axis: 'x', range: [0, -70], note: '1. tık: kalkar (−70°); 4 s sonra kapanır' });
  m.cyl(0.004, 0.1, 10, 0, 'steelBare', [0, 0.178, -0.006], [0, 0, 90], 'Clip');
  m.box(0.1, 0.006, 0.026, 0.002, 'steelBare', [0, 0.176, -0.019], [-8, 0, 0], 'Clip');
  m.box(0.03, 0.008, 0.012, 0.003, 'bakeliteBlack', [0, 0.174, -0.034], [0, 0, 0], 'Clip');
  m.meta.place = [{ pos: [2.2, 0.95, -0.55], yaw: -90, note: 'kart rafının altında (rafın DeckSeat çapası)' }];
  return m;
}
function gDiscZ(r, seg) { const g = gCyl(r, 0.0002, seg, 0); g.rotateX(Math.PI / 2); return g; }

// ---------------------------------------------------------------- door exterior details
export function OuterWheel() {
  const m = new Model('OuterWheel', { family: 'menu', budget: 'small', listName: 'Dış çark', origin: 'göbek arkası (kapı dış yüzü)',
    note: 'Kapının dış yüzündeki dökme demir el çarkı (Ø 360). İçerideki kırmızı çarkla aynı mil; dışarıda boyasız dökme demir, el değen jant parlak. Kapı açılırken 1,25 tur döner.' });
  m.part('Wheel', { pivot: [0, 0, 0], axis: 'z', range: [0, -450], note: '1,25 tur; içerideki çarkın tersi yönde görünür' });
  m.add(gTorus(0.165, 0.016, 6, 24), 'castIron', [0, 0, -0.07], [0, 0, 0], 'Wheel');
  for (let i = 0; i < 4; i++) m.box(0.3, 0.022, 0.018, 0.005, 'castIron', [0, 0, -0.07], [0, 0, 45 + i * 45 * 2], 'Wheel');
  m.cyl(0.04, 0.05, 12, 0.008, 'castIron', [0, 0, -0.05], [-90, 0, 0], 'Wheel');
  m.cyl(0.018, 0.012, 12, 0.003, 'steelBare', [0, 0, -0.078], [-90, 0, 0], 'Wheel');
  m.cyl(0.055, 0.02, 12, 0.004, 'steelCast', [0, 0, -0.01], [-90, 0, 0]);   // boss on the leaf
  return m;
}
export function PeepRing() {
  const m = new Model('PeepRing', { family: 'menu', budget: 'small', listName: 'Gözetleme halkası (dış)', origin: 'halka arkası (kapı dış yüzü)',
    note: 'Pirinç halka ve koyu mercek; dışarıdan bakan için yalnız süs, tıklanmaz.' });
  m.add(gRing(0.026, 0.012, 0.012, 14, 0.003), 'brass', [0, 0, -0.006], [-90, 0, 0]);
  m.cyl(0.015, 0.004, 16, 0, 'glassClear', [0, 0, -0.004], [-90, 0, 0]);
  m.cyl(0.015, 0.002, 16, 0, 'punchHole', [0, 0, -0.001], [-90, 0, 0]);
  return m;
}
export function OuterIntercom() {
  const m = new Model('OuterIntercom', { family: 'menu', budget: 'small', listName: 'Dış interkom', origin: 'arka yüz, alt kenar ortası',
    note: 'Dökme alüminyum gövde, yağmur siperli hoparlör ızgarası (yarıklar dokuda), büyük siyah çağrı düğmesi. Menüde tıklanmaz; sesi kapı açılırken “cızırt” verir.' });
  defMat('aluCast', { hex: '#8A8D86', rough: 0.55, metal: 0.6, note: 'dökme alüminyum' });
  m.box(0.12, 0.2, 0.05, 0.008, 'aluCast', [0, 0.1, -0.025]);
  m.box(0.1, 0.012, 0.03, 0.004, 'aluCast', [0, 0.172, -0.062], [12, 0, 0]);        // rain hood
  m.box(0.09, 0.08, 0.004, 0.001, 'paintBlack', [0, 0.125, -0.051]);               // grille plate (slots texture)
  m.cyl(0.018, 0.012, 16, 0.003, 'steelCast', [0, 0.05, -0.054], [-90, 0, 0]);
  m.part('Button', { pivot: [0, 0.05, -0.06], kind: 'slide', axis: 'z', range: [0, 0.004], note: 'çağrı düğmesi' });
  m.lathe([[0, 0], [0.013, 0], [0.014, 0.006], [0.01, 0.01], [0, 0.011]], 14, 'bakeliteBlack', [0, 0.05, -0.06], [-90, 0, 0], 'Button');
  return m;
}
export function LandingLamp() {
  const m = new Model('LandingLamp', { family: 'menu', budget: 'small', listName: 'Sahanlık lambası', origin: 'duvar plakasının arka yüz ortası',
    note: 'Duvardan 0,22 m çıkan kollu, dökme gövdeli, cam fanuslu ve üç şeritli korumalı armatür (gemi tipi). Şeritler 8 mm (TAA). Fanus aşağı bakar; ışık yazıyı ve kapıyı önden-yukarıdan aydınlatır. Lamba gövdesi gölge atmaz (kendi şeritlerinin gölgesi duvara düşmesin).' });
  m.box(0.09, 0.12, 0.012, 0.004, 'paintBlack', [0, 0, -0.006], [0, 0, 0], 'Body', { cast: false });                  // wall plate
  m.cyl(0.014, 0.2, 10, 0.003, 'paintBlack', [0, 0.01, -0.11], [90, 0, 0], 'Body', { cast: false });                   // arm
  m.lathe([[0, 0], [0.058, 0], [0.06, 0.008], [0.05, 0.03], [0, 0.034]], 18, 'paintBlack', [0, 0.03, -0.22], [180, 0, 0], 'Body', { cast: false }); // hood
  m.lathe([[0, 0], [0.046, 0], [0.048, 0.01], [0.043, 0.04], [0.026, 0.066], [0, 0.07]], 18, 'lampGlassCold', [0, -0.004, -0.22], [180, 0, 0], 'Body', { cast: false }); // glass dome (down)
  for (let i = 0; i < 3; i++) m.add(gTorus(0.055, 0.004, 4, 14, Math.PI * 0.55), 'paintBlack', [0, -0.02, -0.22], [90, i * 120, -100], 'Body', { cast: false });
  m.light('Bulb', { pos: [0, -0.035, -0.22], type: 'point', shadows: true, color: '#D8E6E2', note: 'menü M0 ana ışığı; gövde gölge atmaz' });
  m.meta.place = [{ pos: [2.2, 2.42, 0.75], yaw: -90, note: 'kapı lentosunun üstü' }, { pos: [2.2, 2.3, -0.5], yaw: -90, note: 'saat ve kart rafının üstü' }];
  return m;
}
export function DoorBell() {
  const m = new Model('DoorBell', { family: 'menu', budget: 'small', listName: 'Kapı zili', origin: 'arka yüz ortası',
    note: 'Siyah kaide üstünde pirinç çan ve tokmak; kart basılınca bir kez çalar (0,4 s, tokmak titrer).' });
  m.box(0.07, 0.09, 0.02, 0.004, 'bakeliteBlack', [0, 0, -0.01]);
  m.lathe([[0, 0], [0.04, 0], [0.043, 0.006], [0.036, 0.02], [0.012, 0.03], [0, 0.031]], 20, 'brass', [0, 0.005, -0.02], [-90, 0, 0]);
  m.part('Striker', { pivot: [0, -0.04, -0.03], axis: 'z', range: [-6, 6], note: 'çalarken titrer' });
  m.box(0.005, 0.04, 0.005, 0.001, 'steelBare', [0.0, -0.02, -0.03], [0, 0, 0], 'Striker');
  m.lathe([[0, 0], [0.006, 0], [0.006, 0.01], [0, 0.012]], 10, 'steelBare', [0, 0, -0.03], [0, 0, 0], 'Striker');
  return m;
}
export function Stairs() {
  const m = new Model('Stairs', { family: 'menu', budget: 'big', listName: 'Merdiven ve tırabzan', origin: 'ilk basamağın ön alt kenarı, merdiven ekseninde',
    note: 'Yukarı kata çıkan beton merdiven (10 basamak, rıht 0,18, basamak 0,27, genişlik 0,90), çelik basamak burunları, duvar tarafında (−x) eğimli beton limon, açık tarafta (+x) Ø 42 galvaniz boru tırabzan ve 4 dikme. Merdiven +z yönünde yükselir; sahnede yaw ile çevrilir.' });
  const n = 10, rise = 0.18, run = 0.27, w = 0.9;
  for (let i = 0; i < n; i++) {
    m.box(w, rise * (i + 1), run, 0.006, 'concreteDark', [0, rise * (i + 1) / 2, run * i + run / 2]);
    m.box(w - 0.02, 0.014, 0.034, 0.003, 'steelCast', [0, rise * (i + 1) - 0.004, run * i + 0.014]);
  }
  // wall-side stringer (−x) following the pitch
  const L = Math.hypot(run * n, rise * n), ang = Math.atan2(rise * n, run * n) / D2R;
  m.box(0.08, 0.3, L + 0.1, 0.01, 'concreteDark', [-w / 2 - 0.04, rise * n / 2 + 0.1, run * n / 2], [-ang, 0, 0]);
  // handrail on the open side (+x): rail along the pitch + 4 posts
  m.cyl(0.021, L, 12, 0.004, 'galv', [w / 2 - 0.05, 0.92 + rise * n / 2, run * n / 2], [90 - ang, 0, 0]);
  for (const i of [0, 3, 6, 9]) m.cyl(0.014, 0.9, 8, 0.002, 'galv', [w / 2 - 0.05, rise * (i + 1) + 0.45, run * i + run / 2]);
  m.cyl(0.021, 0.12, 12, 0.004, 'galv', [w / 2 - 0.05, 0.92 + 0.06 - 0.12 + 0.18, -0.02], [0, 0, 0]);
  return m;
}
export function TitleStencil() {
  const m = new Model('TitleStencil', { family: 'menu', budget: 'small', listName: 'Başlık şablonu', origin: 'yazı alanının alt kenar ortası (duvarda)',
    note: 'Kapı lentosunun üstünde betona doğrudan boyanmış şablon yazı: TR “SON ŞALTER”, EN “CORRIGIBLE” (TMP, Big Shoulders Stencil 800). Tabela değil: altında boya taşması ve damlalar (doku), iki uçta yarım kalmış tehlike köşeli bandı.' });
  for (const sx of [-1, 1]) m.box(0.12, 0.06, 0.002, 0, 'paintHazard', [sx * 0.58, -0.05, -0.0015], [0, 0, 0], 'Body', { cast: false });
  m.text('TXT_Title', { center: [0, 0.12, -0.0025], size: [1.16, 0.2], role: 'wallStencil (Big Shoulders Stencil 800)', capMm: 165, sample: 'SON ŞALTER', sampleEn: 'CORRIGIBLE' });
  return m;
}

export const MODELS = {
  LastSwitch: { build: LastSwitch },
  PunchClock: { build: PunchClock }, CardRack: { build: CardRack }, ShiftCard: { build: ShiftCard }, PunchHole: { build: PunchHole },
  CancelSlip: { build: CancelSlip }, BlankDeck: { build: BlankDeck }, OuterWheel: { build: OuterWheel }, PeepRing: { build: PeepRing },
  OuterIntercom: { build: OuterIntercom }, LandingLamp: { build: LandingLamp }, DoorBell: { build: DoorBell }, Stairs: { build: Stairs }, TitleStencil: { build: TitleStencil },
};
