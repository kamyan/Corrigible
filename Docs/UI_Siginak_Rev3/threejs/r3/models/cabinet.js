// family: cabinet (settings cabinet, room + menu) · Rev3 model library (see DESIGN_R3.md §3.2)
// Unity local coordinates (x right, y up, z forward); fronts face −z. Panel controls are separate small models placed
// on the panel at the anchors listed in SettingsCabinet (PanelSlots).
import { Model, gBox, gCyl, gLathe, gExtrude, gTorus, gRing, roundedRect, circlePts, D2R } from '../mlib.js';
import { defMat } from '../mats.js';

defMat('cabPanel', { hex: '#2A1D14', rough: 0.4, metal: 0, note: 'dolap iç panosu (kahverengi bakalit levha)' });
defMat('scaleCream', { hex: '#E6DCC0', rough: 0.55, metal: 0, tex: 'tx_cab_scale (0–10 ölçek çizgileri ve rakamlar, kelime yok)', note: 'sürgü ölçek şeridi' });
defMat('gaugeDial', { hex: '#EFE6CC', rough: 0.6, metal: 0, tex: 'tx_cab_gauge (yay ölçek, 0–10, amber bölge; kırmızı yok)', note: 'ibreli gösterge kadranı' });
defMat('needleBlack', { hex: '#141210', rough: 0.4, metal: 0.2, note: 'ibre (2 mm)' });
defMat('enamelBadge', { hex: '#E8E2CF', rough: 0.3, metal: 0, tex: 'tx_icon_gear (dişli simgesi, mürekkep #1E1B16)', note: 'emaye rozet' });

// ---------------------------------------------------------------- SettingsCabinet: case, Leaf, Latch, panel, bulb hood
export function SettingsCabinet(o = {}) {
  const menu = !!o.menu;
  const m = new Model(menu ? 'SettingsCabinetMenu' : 'SettingsCabinet', { family: 'cabinet', budget: 'big',
    listName: menu ? 'Ayarlar dolabı (menü hâli)' : 'Ayarlar dolabı', origin: 'arka yüz, alt kenar ortası (duvar düzlemi z = 0)',
    note: (menu ? 'Menüdeki örnek: MENÜYE DÖN kolunun yerinde vidalı kör kapak; kanat −120° açılır. ' : 'Odadaki örnek: kanat −95° açılır (sağ duvara değmesin). ') +
      'Preslenmiş çelik hat dolabı 0,68 × 0,70 × 0,22. Kanat sağdan menteşeli, solda çeyrek tur mandal. Kanatta büyük “AYARLAR” şablonu ve Ø 130 emaye dişli rozeti (K1’den okunur). İçeride bakalit pano; ampul üst ön kenardaki siperin arkasında, panoyu sıyırarak aydınlatır (eski sıcak leke yok).' });
  const W = 0.68, H = 0.70, D = 0.22, t = 0.014;
  // case shell (hollow: back + 4 sides) with rolled front lip
  m.box(W, H, t, 0.004, 'paintOliveDark', [0, H / 2, -t / 2]);                                         // back
  for (const sx of [-1, 1]) m.box(t, H, D, 0.005, 'paintOliveDark', [sx * (W / 2 - t / 2), H / 2, -D / 2]);
  for (const y of [t / 2, H - t / 2]) m.box(W, t, D, 0.005, 'paintOliveDark', [0, y, -D / 2]);
  for (const [w, h, x, y] of [[W + 0.012, 0.03, 0, H - 0.009], [W + 0.012, 0.03, 0, 0.009], [0.03, H, -W / 2 + 0.009, H / 2], [0.03, H, W / 2 - 0.009, H / 2]])
    m.box(w, h, 0.014, 0.005, 'paintOliveDark', [x, y, -D + 0.002]);                                   // front frame lip
  for (let i = 0; i < 4; i++) m.box(0.006, 0.012, 0.11, 0.002, 'paintOliveDark', [W / 2 + 0.003, 0.2 + i * 0.03, -D / 2], [0, 0, 18]); // right louvres
  for (const y of [0.06, H - 0.06]) for (const sx of [-1, 1]) m.box(0.05, 0.03, 0.006, 0.002, 'steelCast', [sx * (W / 2 + 0.02), y, -0.003]); // wall tabs
  m.cyl(0.024, 0.03, 12, 0.004, 'steelCast', [-0.22, H + 0.012, -0.08]);                               // conduit gland (conduit = scene)
  // inner panel on standoffs (set back 0.07 from the front)
  const pz = -D + 0.075; const PW = 0.6, PH = 0.56;
  m.box(PW, PH, 0.01, 0.003, 'cabPanel', [0, H / 2 - 0.01, pz + 0.005]);
  for (const sx of [-1, 1]) for (const sy of [-1, 1]) m.cyl(0.006, 0.004, 8, 0.001, 'brassWorn', [sx * (PW / 2 - 0.015), H / 2 - 0.01 + sy * (PH / 2 - 0.015), pz - 0.001], [90, 0, 0], 'Body', { cast: false });
  // bulb behind a hood at the top front (light grazes the panel)
  m.box(W - 0.06, 0.05, 0.004, 0.0015, 'paintOliveDark', [0, H - 0.05, -D + 0.03], [-25, 0, 0]);       // hood deflector
  m.lathe([[0, 0], [0.012, 0], [0.012, 0.02], [0, 0.02]], 12, 'bakeliteBlack', [0, H - t, -D + 0.05], [180, 0, 0]);
  m.part('Bulb', { pivot: [0, H - t - 0.035, -D + 0.05], kind: 'swap', note: 'lampGlassWarm (kanat açıkken) / sönük; ışık: spot, gölgesiz' });
  m.lathe([[0, 0], [0.014, 0.004], [0.019, 0.02], [0.013, 0.034], [0, 0.036]], 14, 'lampGlassWarm', [0, H - t - 0.055, -D + 0.05], [0, 0, 0], 'Bulb', { cast: false });
  m.light('InnerSpot', { pos: [0, H - 0.06, -D + 0.05], type: 'spot', shadows: false, color: '#FFD8A0', note: 'aşağı ve geriye 35°, açı 70°, menzil 0,8 m; şiddet cabinet.bulbIntensity 0,3' });
  // panel slots (anchors) for the control sub-models (centres on the panel face, z = pz)
  const sx0 = -0.235, step = 0.095; const names = ['GENEL', 'MÜZİK', 'ORTAM', 'EFEKT'];
  names.forEach((n, i) => { const x = sx0 + i * step + (i > 0 ? 0.02 : 0);
    m.anchor(`Gauge${i}`, { pos: [x, 0.585, pz], note: 'CabGauge' }); m.anchor(`Slider${i}`, { pos: [x, 0.37, pz], note: 'CabSlider (ölçek solda)' });
    m.anchor(`Counter${i}`, { pos: [x, 0.165, pz], note: 'CabCounter' }); m.anchor(`Plate${i}`, { pos: [x, 0.115, pz], note: `CabPlate “${n}”` }); });
  m.anchor('ToggleSES', { pos: [0.205, 0.555, pz], note: 'CabToggle + CabLamp üstte' }); m.anchor('PlateSES', { pos: [0.205, 0.47, pz], note: 'CabPlate “SES”' });
  m.anchor('RotaryDIL', { pos: [0.205, 0.33, pz], note: 'CabRotary' }); m.anchor('PlateDIL', { pos: [0.205, 0.255, pz], note: 'CabPlate “DİL · LANGUAGE” (sabit, iki dilli)' });
  m.anchor('MenuLever', { pos: [0.205, 0.14, pz], note: menu ? 'menüde CabBlank (kör kapak)' : 'CabMenuLever' }); m.anchor('PlateMENU', { pos: [0.205, 0.075, pz], note: 'CabPlate “MENÜYE DÖN”' });
  // Leaf: hinge on the right edge (local y), latch on the left
  const lz = -D - 0.004; const LW = W - 0.02, LH = H - 0.02;
  m.part('Leaf', { pivot: [W / 2 - 0.01, H / 2, lz + 0.006], axis: 'y', range: [0, menu ? -120 : -95], note: menu ? 'menüde −120°' : 'odada −95° (sağ duvar)' });
  m.box(LW, LH, 0.012, 0.004, 'paintOliveDark', [0, H / 2, lz], [0, 0, 0], 'Leaf');
  for (const [w, h, x, y] of [[LW - 0.07, 0.016, 0, H / 2 + LH / 2 - 0.045], [LW - 0.07, 0.016, 0, H / 2 - LH / 2 + 0.045], [0.016, LH - 0.09, -LW / 2 + 0.035, H / 2], [0.016, LH - 0.09, LW / 2 - 0.035, H / 2]])
    m.box(w, h, 0.006, 0.003, 'paintOliveDark', [x, y, lz - 0.008], [0, 0, 0], 'Leaf');                 // pressed stiffening bead
  m.box(LW - 0.02, LH - 0.02, 0.006, 0.002, 'rubberGasket', [0, H / 2, lz + 0.009], [0, 0, 0], 'Leaf', { cast: false });
  for (const y of [0.12, H / 2, H - 0.12]) m.cyl(0.009, 0.07, 10, 0.002, 'steelCast', [W / 2 - 0.004, y, lz + 0.004], [0, 0, 0], 'Leaf'); // hinge knuckles
  m.cyl(0.07, 0.006, 32, 0.0015, 'brass', [-0.06, 0.39, lz - 0.009], [90, 0, 0], 'Leaf');               // badge rim
  m.cyl(0.064, 0.004, 32, 0, 'enamelBadge', [-0.06, 0.39, lz - 0.012], [90, 0, 0], 'Leaf');            // enamel gear badge
  m.text('TXT_Stencil', { part: 'Leaf', center: [-0.04, 0.57, lz - 0.0065], size: [0.5, 0.075], role: 'wallStencil (Big Shoulders Stencil, krem #D8D2BE)', capMm: 62, sample: 'AYARLAR', sampleEn: 'SETTINGS' });
  m.box(0.22, 0.034, 0.004, 0.0012, 'brass', [-0.06, 0.25, lz - 0.008], [0, 0, 0], 'Leaf');
  m.text('TXT_LeafPlate', { part: 'Leaf', center: [-0.06, 0.25, lz - 0.0102], size: [0.2, 0.022], role: 'plateLabel', capMm: 12, sample: 'SES · DİL', sampleEn: 'SOUND · LANGUAGE' });
  // Latch: quarter-turn T handle on the left edge (local z 90°)
  m.cyl(0.026, 0.008, 16, 0.002, 'steelCast', [-LW / 2 + 0.06, H / 2, lz - 0.009], [90, 0, 0], 'Leaf');
  m.part('Latch', { parent: 'Leaf', pivot: [-LW / 2 + 0.06, H / 2, lz - 0.013], axis: 'z', range: [0, 90], note: 'çeyrek tur; açılırken 0,15 s' });
  m.box(0.016, 0.07, 0.02, 0.005, 'bakeliteBlack', [-LW / 2 + 0.06, H / 2, lz - 0.026], [0, 0, 0], 'Latch');
  m.cyl(0.008, 0.016, 10, 0.002, 'steelBare', [-LW / 2 + 0.06, H / 2, lz - 0.016], [90, 0, 0], 'Latch');
  m.anchor('K9', { pos: [0, 0.35, -0.86], note: 'K9 kamera çapası (dikey FOV 45°), panoya bakar' });
  m.anchor('Hover', { pos: [0, 0.35, -0.3], note: 'K1’den üzerine gelince etiket' });
  m.meta.place = menu ? [{ pos: [2.2, 1.02, -0.92], yaw: -90, note: 'menü: kapı duvarında, en solda' }] : [{ pos: [1.68, 1.17, 2.30], yaw: 0, note: 'oda: arka duvarın sağı, CRT masasının üstü (merkez 1,68 · 1,52)' }];
  return m;
}

// ---------------------------------------------------------------- panel controls (small models, origin = centre on the panel face)
export function CabSlider(o = {}) {
  const brass = !!o.master;
  const m = new Model(brass ? 'CabSliderMaster' : 'CabSlider', { family: 'cabinet', budget: 'small', listName: brass ? 'Kollu dikey sürgü (GENEL, pirinç başlı)' : 'Kollu dikey sürgü',
    origin: 'yarığın ortası, pano yüzünde', note: '180 mm yarık, solda 0–10 ölçek (doku), T başlı bakalit kol. Kol yerel y’de ±80 mm kayar (−80 = %0, +80 = %100); her %10’da çentik. Yarığın çarpıştırıcısı kolun iki yanına ±15 mm taşar.' });
  m.box(0.034, 0.21, 0.004, 0.0015, 'brass', [0, 0, -0.002]);                         // escutcheon
  m.box(0.008, 0.184, 0.003, 0, 'punchHole', [0, 0, -0.0041], [0, 0, 0], 'Body', { cast: false }); // slot (dark)
  m.box(0.016, 0.19, 0.0015, 0, 'scaleCream', [-0.03, 0, -0.0008], [0, 0, 0], 'Body', { cast: false });  // scale strip (ticks texture)
  for (const y of [-0.098, 0.098]) m.cyl(0.003, 0.003, 8, 0.001, 'brassWorn', [0, y, -0.005], [90, 0, 0], 'Body', { cast: false });
  m.part('Handle', { pivot: [0, 0, -0.004], kind: 'slide', axis: 'y', range: [-0.08, 0.08], note: '−0,08 = %0 · +0,08 = %100 · %5 adım' });
  m.box(0.006, 0.012, 0.03, 0.001, 'steelBare', [0, 0, -0.019], [0, 0, 0], 'Handle');
  m.box(0.044, 0.018, 0.016, 0.005, brass ? 'brass' : 'bakeliteBlack', [0, 0, -0.04], [0, 0, 0], 'Handle');
  m.box(0.038, 0.004, 0.0172, 0.001, brass ? 'brassWorn' : 'paintCream', [0, 0, -0.04], [0, 0, 0], 'Handle', { cast: false }); // index line
  return m;
}
export function CabGauge() {
  const m = new Model('CabGauge', { family: 'cabinet', budget: 'small', listName: 'İbreli gösterge', origin: 'pano yüzünde merkez',
    note: 'VU tipi gösterge: krem kadran, yay ölçek ve amber bölge (doku; kırmızı yok), 2 mm siyah ibre. İbre değeri yaylı gecikmeyle izler (Needle yerel z ±50°).' });
  m.box(0.078, 0.06, 0.012, 0.003, 'paintBlack', [0, 0, -0.006]);
  m.box(0.068, 0.048, 0.002, 0, 'gaugeDial', [0, 0.002, -0.0122], [0, 0, 0], 'Body', { cast: false });
  m.box(0.07, 0.05, 0.002, 0, 'glassClear', [0, 0.002, -0.0145], [0, 0, 0], 'Body', { cast: false });
  m.part('Needle', { pivot: [0, -0.018, -0.0135], axis: 'z', range: [50, -50], note: '+50 = 0 · −50 = 10 (sağa)' });
  m.box(0.002, 0.04, 0.001, 0, 'needleBlack', [0, 0.002, -0.0135], [0, 0, 0], 'Needle', { cast: false });
  m.cyl(0.004, 0.002, 10, 0, 'needleBlack', [0, -0.018, -0.0135], [90, 0, 0], 'Needle', { cast: false });
  return m;
}
export function CabCounter() {
  const m = new Model('CabCounter', { family: 'cabinet', budget: 'small', listName: 'Sayaç tekerleri', origin: 'pano yüzünde merkez',
    note: 'Üç haneli teker (masanın CounterWheel’i ve rakam dokusu). Değer değişince teker döner (080 → 085).' });
  // brass bezel = a frame around a 54 × 18 mm window (the drums show through it)
  m.box(0.064, 0.006, 0.008, 0.002, 'brass', [0, 0.012, -0.004]); m.box(0.064, 0.006, 0.008, 0.002, 'brass', [0, -0.012, -0.004]);
  for (const sx of [-1, 1]) m.box(0.005, 0.03, 0.008, 0.0015, 'brass', [sx * 0.0295, 0, -0.004]);
  m.box(0.056, 0.022, 0.002, 0, 'counterFace', [0, 0, 0.012], [0, 0, 0], 'Body', { cast: false }); // dark well behind the drums
  for (let i = 0; i < 3; i++) { const x = (i - 1) * 0.0175; m.part(`Wheel${i}`, { pivot: [x, 0, 0.005], axis: 'x', range: [0, 360], note: '36° / rakam' });
    m.cyl(0.012, 0.015, 14, 0.0015, 'counterDrum', [x, 0, 0.005], [0, 0, 90], `Wheel${i}`); }
  return m;
}
export function CabLamp() {
  const m = new Model('CabLamp', { family: 'cabinet', budget: 'small', listName: 'Lamba ve çerçevesi', origin: 'pano yüzünde merkez',
    note: 'Ø 18 amber mücevher lamba, krom çerçeve. Kanal 0’dan büyük ve ses açıkken yanar.' });
  m.add(gRing(0.012, 0.004, 0.008, 16, 0.0015), 'chrome', [0, 0, -0.004], [90, 0, 0]);
  m.part('Lens', { pivot: [0, 0, -0.004], kind: 'swap', note: 'jewelAmberOn / jewelAmberOff' });
  m.lathe([[0, 0], [0.009, 0], [0.0095, 0.004], [0.006, 0.009], [0, 0.0105]], 12, 'jewelAmberOn', [0, 0, -0.004], [-90, 0, 0], 'Lens');
  return m;
}
export function CabToggle() {
  const m = new Model('CabToggle', { family: 'cabinet', budget: 'small', listName: 'Yarasa kollu anahtar', origin: 'pano yüzünde merkez',
    note: 'Krom altıgen somun ve AÇIK/KAPALI plakası; yarasa kol panodan 40 mm dışarı uzanır. Lever yerel x ±28° (yukarı = AÇIK). Tıklama ya da sürükleme çevirir (0,08 s, “klak”).' });
  m.box(0.05, 0.07, 0.003, 0.001, 'brass', [0, 0, -0.0015]);
  m.cyl(0.011, 0.008, 6, 0.0015, 'chrome', [0, 0, -0.007], [90, 0, 0]);
  m.part('Lever', { pivot: [0, 0, -0.01], axis: 'x', range: [-28, 28], rest: -28, note: '−28 = yukarı (AÇIK) · +28 = aşağı (KAPALI)' });
  m.lathe([[0, 0], [0.004, 0], [0.003, 0.03], [0.0055, 0.034], [0.006, 0.04], [0, 0.042]], 10, 'chrome', [0, 0, -0.01], [-90, 0, 0], 'Lever');
  return m;
}
export function CabRotary() {
  const m = new Model('CabRotary', { family: 'cabinet', budget: 'small', listName: 'Dil kademe anahtarı', origin: 'pano yüzünde merkez',
    note: 'Etekli bakalit düğme ve işaretçi; iki konum ±40° (TÜRKÇE / ENGLISH, her dil kendi dilinde). Tıklama sonraki konuma çevirir (45°, “klonk”).' });
  m.cyl(0.03, 0.003, 24, 0.001, 'brass', [0, 0, -0.0015], [90, 0, 0]);
  m.part('Knob', { pivot: [0, 0, -0.003], axis: 'z', range: [40, -40], rest: 40, note: '+40 = TÜRKÇE · −40 = ENGLISH' });
  m.lathe([[0, 0], [0.024, 0], [0.025, 0.004], [0.021, 0.008], [0.016, 0.01], [0.015, 0.024], [0.012, 0.028], [0, 0.029]], 20, 'bakeliteBlack', [0, 0, -0.003], [-90, 0, 0], 'Knob');
  m.box(0.004, 0.02, 0.004, 0.001, 'paintCream', [0, 0.012, -0.031], [0, 0, 0], 'Knob', { cast: false }); // pointer line
  m.text('TXT_PosTR', { center: [-0.032, 0.036, -0.0032], size: [0.05, 0.01], role: 'plateLabel (gravür)', capMm: 6, sample: 'TÜRKÇE' });
  m.text('TXT_PosEN', { center: [0.032, 0.036, -0.0032], size: [0.05, 0.01], role: 'plateLabel (gravür)', capMm: 6, sample: 'ENGLISH' });
  return m;
}
export function CabMenuLever() {
  const m = new Model('CabMenuLever', { family: 'cabinet', budget: 'small', listName: 'Kapaklı MENÜYE DÖN kolu', origin: 'pano yüzünde merkez',
    note: 'Vardiya şalterinin küçük kardeşi: tehlike çizgili koruyucu kapak (Cover, yerel x 0→112°) ve krom kol (Lever, yerel x +35 → −35). 1. tık kapak, 4 s içinde 2. tık kol: kaydedilir, ekran kararır, menü açılır. Masa meşgulken kapak kilitli, KİLİTLİ bandı görünür.' });
  m.box(0.08, 0.07, 0.016, 0.004, 'paintOliveDark', [0, 0, -0.008]);
  m.cyl(0.01, 0.06, 10, 0.002, 'steelCast', [0, -0.012, -0.02], [0, 0, 90]);
  m.part('Lever', { pivot: [0, -0.012, -0.02], axis: 'x', range: [35, -35], rest: 35, note: '+35 yukarı · −35 çekili' });
  m.cyl(0.004, 0.045, 8, 0.001, 'chrome', [0, 0.01, -0.02], [0, 0, 0], 'Lever');
  m.lathe([[0, 0], [0.008, 0.001], [0.009, 0.008], [0.006, 0.014], [0, 0.015]], 12, 'bakeliteBlack', [0, 0.031, -0.02], [0, 0, 0], 'Lever');
  m.part('Cover', { pivot: [0, 0.035, -0.018], axis: 'x', range: [0, 112], note: 'koruyucu; 4 s sonra kendiliğinden iner' });
  m.box(0.07, 0.074, 0.004, 0.0015, 'paintHazard', [0, -0.002, -0.058], [0, 0, 0], 'Cover');
  for (const sx of [-1, 1]) m.box(0.003, 0.074, 0.04, 0.001, 'paintOliveDark', [sx * 0.0335, -0.002, -0.038], [0, 0, 0], 'Cover');
  m.box(0.07, 0.003, 0.04, 0.001, 'paintOliveDark', [0, 0.0335, -0.038], [0, 0, 0], 'Cover');
  m.part('LockBand', { pivot: [0, 0, -0.062], kind: 'swap', note: 'masa meşgulken görünür (KİLİTLİ)' });
  m.box(0.074, 0.016, 0.002, 0, 'paintCream', [0, -0.01, -0.062], [0, 0, 0], 'LockBand', { cast: false });
  m.text('TXT_Lock', { part: 'LockBand', center: [0, -0.01, -0.0632], size: [0.07, 0.012], role: 'stamp (#675F50 kesik)', capMm: 6, sample: 'KİLİTLİ', sampleEn: 'LOCKED' });
  return m;
}
export function CabPlate(o = {}) {
  const w = o.w || 0.075; const nm = o.name || 'CabPlate';
  const m = new Model(nm, { family: 'cabinet', budget: 'small', listName: o.listName || 'Pirinç plaka', origin: 'pano yüzünde merkez',
    note: 'Kazınmış pirinç plaka, iki vida. Yazı TMP (Courier Prime 700, kazıma rengi #2A1E10); EN için plaka ~80 mm.' });
  m.box(w, 0.02, 0.003, 0.001, 'brass', [0, 0, -0.0015]);
  for (const sx of [-1, 1]) m.cyl(0.0022, 0.002, 8, 0.0006, 'brassWorn', [sx * (w / 2 - 0.005), 0, -0.0035], [90, 0, 0], 'Body', { cast: false });
  m.text('TXT', { center: [0, 0, -0.0032], size: [w - 0.016, 0.013], role: 'plateLabel', capMm: 8, sample: o.sample || 'MÜZİK', sampleEn: o.sampleEn || 'MUSIC' });
  return m;
}
export function CabBlank() {
  const m = new Model('CabBlank', { family: 'cabinet', budget: 'small', listName: 'Kör kapak (menü)', origin: 'pano yüzünde merkez',
    note: 'Menüdeki dolapta MENÜYE DÖN kolunun yeri: dört vidalı çelik kör kapak.' });
  m.box(0.08, 0.07, 0.004, 0.0015, 'paintOliveDark', [0, 0, -0.002]);
  for (const sx of [-1, 1]) for (const sy of [-1, 1]) m.cyl(0.003, 0.002, 8, 0.0006, 'steelBare', [sx * 0.032, sy * 0.027, -0.0045], [90, 0, 0], 'Body', { cast: false });
  return m;
}

export const MODELS = {
  SettingsCabinet: { build: () => SettingsCabinet() },
  SettingsCabinetMenu: { build: () => SettingsCabinet({ menu: true }) },
  CabSlider: { build: () => CabSlider() }, CabSliderMaster: { build: () => CabSlider({ master: true }) },
  CabGauge: { build: CabGauge }, CabCounter: { build: CabCounter }, CabLamp: { build: CabLamp }, CabToggle: { build: CabToggle },
  CabRotary: { build: CabRotary }, CabMenuLever: { build: CabMenuLever }, CabPlate: { build: () => CabPlate() }, CabBlank: { build: CabBlank },
};
