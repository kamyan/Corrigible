// family: board · Rev3 model library, supplement 2026-11-05 (R15 planning board, R16 cards, R18 recipe book) · owner: main
// Unity local coordinates (x right, y up, z away); fronts face −z. Board origin = floor, centre between the uprights.
// Layout is in board millimetres from the panel's inner top-left corner (BL); P(x, y) converts to model space.
import * as THREE from 'three';
import { Model, gBox, gCyl, gLathe, gTube, gTorus, gPlane, gSphere, D2R } from '../mlib.js';
import { defMat } from '../mats.js';

defMat('boardPanel',  { hex: '#56604F', rough: 0.72, metal: 0.05, note: 'mıknatıs tutan çelik pano yüzeyi, mat zeytin-gri boya (kartlardan iki ton koyu)' });
defMat('boardFace',   { hex: '#56604F', rough: 0.72, metal: 0, tex: 'tx_board_face 4096×2048 (1160×600 mm): kelimesiz baskı — cep sınırları (kesikli kart dış hattı + kişi simgesi + üç teçhizat şeridi), Boşta yelpaze izi, iş emri ve depo alanı tonu; yazılar TMP', note: 'pano baskı yüzü (UV 0–1)' });
defMat('cardEdge',    { hex: '#E3D8BA', rough: 0.9, metal: 0, note: 'kart kenarı (0,6 mm karton)' });
defMat('cardFace',    { hex: '#EFE6D0', rough: 0.88, metal: 0, tex: 'kart yüzü: çalışma anında çizilen kart dokusu (RenderTexture ya da UI Canvas → doku), 8 px/mm', note: 'kart ön yüzü (UV 0–1)' });
defMat('cardBack',    { hex: '#E2D7B8', rough: 0.9, metal: 0, tex: 'tx_card_back (KYGA · 07 soluk şablon)', note: 'kart arkası (UV 0–1)' });
defMat('magnetBlack', { hex: '#1A1917', rough: 0.35, metal: 0.1, note: 'kart mıknatısı (bakalit kaplı düğme Ø 10 × 3,5 mm)' });
defMat('manila',      { hex: '#C9A86A', rough: 0.85, metal: 0, note: 'kişi dosyası klasörü (manila karton)' });
defMat('bookCloth',   { hex: '#5A2420', rough: 0.82, metal: 0, tex: 'tx_book_cover (bez doku + krem etiket TARİF DEFTERİ)', note: 'tarif defteri cildi (bordo bez)' });
defMat('pageEdge',    { hex: '#E8DFC6', rough: 0.95, metal: 0, note: 'sayfa yığını kenarı' });
defMat('pageFace',    { hex: '#F0E8D2', rough: 0.9, metal: 0, tex: 'tx_recipe_spread_1..3 (iki tarif bir açılım; TR/EN ayrı doku ya da TMP)', note: 'tarif sayfası (UV 0–1)' });
defMat('wireBlack',   { hex: '#26262A', rough: 0.45, metal: 0.6, note: 'tel sepet, kanca' });
defMat('chalkWhite',  { hex: '#ECEBE4', rough: 0.95, metal: 0, note: 'tebeşir, yağlı kalem ucu' });
defMat('greasePencil',{ hex: '#9A1E14', rough: 0.6, metal: 0, note: 'kırmızı yağlı kalem (eğitim halkası)' });

// ---------------------------------------------------------------- layout (board mm, origin = panel inner top-left, y down)
export const BOARD = { PW: 1.196, PH: 0.636, y0: 0.975, zf: -0.014, IX0: -0.58, IY0: 1.593, IW: 1160, IH: 600 };
export const P = (x, y, z = BOARD.zf) => [BOARD.IX0 + x / 1000, BOARD.IY0 - y / 1000, z];
export const BL = {
  pockets: { tek: [14, 44, 108, 194], emp: [132, 44, 108, 194], ana: [250, 44, 108, 194], uret: [14, 244, 108, 194], mutf: [132, 244, 108, 194], egit: [250, 244, 180, 194] },
  card: { tek: [68, 128], emp: [186, 128], ana: [304, 128], uret: [68, 328], mutf: [186, 328], egit: [304, 328] },
  topics: { x: 356, y0: 282, dy: 22 },                      // EĞİTİM topic list (5 rows), baseline of first row
  bosta: [380, 44, 770, 194], deck: [1098, 128], handLeft: 462,
  order: [446, 244, 450, 194], sheet: [450, 266, 440, 170], recipe: [514, 328], inputs: [582, 296, 294, 56],
  folder: [906, 244, 244, 194], folderFace: [963, 268, 130, 180],
  depot: [0, 446, 1160, 154], mats: [180, 268, 356, 444], matY: 502, kits: [541, 623, 705, 787, 869, 951], kitY: 502,
  book: [70, 452], hurda: [1012, 474, 130, 126],
  header: { back: [6, 6, 124, 28], title: [140, 7, 404, 26], slate: [556, 5, 440, 30], flap: [1006, 5, 144, 30] },
  lockY: 241,
};
export const SLOTS = ['tek', 'emp', 'ana', 'uret', 'mutf', 'egit'];
export const CARD = { person: [0.080, 0.120], item: [0.080, 0.050], material: [0.080, 0.056], chip: [0.056, 0.040], recipe: [0.112, 0.080], t: 0.0006 };

// ---------------------------------------------------------------- helpers
const zIn = BOARD.zf;
function plate(m, name, x, y, w, h, o = {}) {                  // brass plate (screwed), TMP slot on its face
  const c = P(x + w / 2, y + h / 2); const t = 0.0018; const zc = zIn - t / 2;
  m.box(w / 1000, h / 1000, t, o.chamfer ?? 0.0005, o.mat || 'brass', [c[0], c[1], zc], [0, 0, 0], o.part || 'Body', { cast: false });
  if (o.screws !== false) for (const s of [-1, 1]) m.cyl(0.0022, 0.0012, 4, 0, 'steelBare', [c[0] + s * (w / 2 - 4) / 1000, c[1], zc - t / 2 - 0.0003], [90, 0, 0], o.part || 'Body', { cast: false });
  m.text(name, { part: o.part || 'Body', center: [c[0], c[1], zc - t / 2 - 0.0002], size: [(w - (o.screws === false ? 4 : 14)) / 1000, h / 1000 * 0.8], role: o.role || 'plateLabel', capMm: o.cap || h * 0.5, sample: o.sample || '', sampleEn: o.sampleEn || '', note: o.note || '' });
}
function frameRect(m, x, y, w, h, mat = 'brassWorn', bar = 3) {  // raised drop-target frame (2 mm proud)
  const b = bar / 1000, d = 0.002, zc = zIn - d / 2; const c = P(x + w / 2, y + h / 2);
  m.box(w / 1000, b, d, 0, mat, [c[0], P(0, y)[1] - b / 2, zc], [0, 0, 0], 'Body', { cast: false });
  m.box(w / 1000, b, d, 0, mat, [c[0], P(0, y + h)[1] + b / 2, zc], [0, 0, 0], 'Body', { cast: false });
  m.box(b, h / 1000 - 2 * b, d, 0, mat, [P(x)[0] + b / 2, c[1], zc], [0, 0, 0], 'Body', { cast: false });
  m.box(b, h / 1000 - 2 * b, d, 0, mat, [P(x + w)[0] - b / 2, c[1], zc], [0, 0, 0], 'Body', { cast: false });
}

// ================================================================ PlanningBoard
export function PlanningBoard() {
  const m = new Model('PlanningBoard', { family: 'board', budget: 'big', listName: 'Ayaklı planlama panosu (mıknatıslı çelik)',
    origin: 'zemin, iki dikmenin ortası (y = 0); ön yüz −z',
    note: 'Eski raf ve kesit maketinin yerinde, odanın sol arka köşesinde. İki kare boru dikme (30 mm) T ayak ve dört tekerlek üstünde; arada 1196 × 636 mm mıknatıs tutan çelik pano, zeytin çerçeve. Kartlar mıknatıs düğmesiyle tutunur: görev cepleri pirinç çerçeveli (2 mm kabarık), Boşta tepsisi, iş emri kâğıdı, kişi dosyası sepeti, altta depo ve hurda kutusu. Üstte pano lambası (spot, gölgeli). Bütün yazılar TMP.' });
  const { PW, PH, y0, zf } = BOARD; const y1 = y0 + PH, yc = y0 + PH / 2;
  // ---- stand: two square uprights, T feet, casters, cross bar, height knobs
  for (const s of [-1, 1]) { const x = s * 0.615;
    m.box(0.030, 1.58, 0.030, 0.003, 'paintOliveDark', [x, 0.07 + 0.79, 0.006]);
    m.box(0.040, 0.034, 0.40, 0.004, 'paintOliveDark', [x, 0.075, -0.06]);
    for (const z of [-0.23, 0.10]) { m.box(0.03, 0.025, 0.03, 0, 'steelCast', [x, 0.045, z]); m.cyl(0.026, 0.02, 8, 0, 'rubberGasket', [x, 0.026, z], [0, 0, 90]); }
    m.lathe([[0, 0], [0.018, 0], [0.020, 0.006], [0.017, 0.014], [0.008, 0.016], [0.008, 0.03], [0, 0.03]], 6, 'bakeliteBlack', [x + s * 0.015, yc + 0.18, 0.006], [0, 0, -s * 90]);
  }
  m.box(1.20, 0.03, 0.02, 0.003, 'paintOliveDark', [0, 0.32, 0.008]);
  // ---- panel, printed face, frame
  m.box(PW - 0.036, PH - 0.036, 0.010, 0, 'boardPanel', [0, yc, zf + 0.005]);
  m.add(gPlane(BOARD.IW / 1000, BOARD.IH / 1000), 'boardFace', [0, BOARD.IY0 - BOARD.IH / 2000, zf - 0.0002], [0, 0, 0], 'Body', { cast: false });
  m.box(PW, 0.018, 0.022, 0.002, 'paintOliveDark', [0, y1 - 0.009, zf + 0.004]); m.box(PW, 0.018, 0.022, 0.002, 'paintOliveDark', [0, y0 + 0.009, zf + 0.004]);
  for (const s of [-1, 1]) m.box(0.018, PH - 0.036, 0.022, 0.002, 'paintOliveDark', [s * (PW / 2 - 0.009), yc, zf + 0.004]);
  // depot ledge (60 mm deep lip) with grease pencil and chalk
  m.box(PW - 0.036, 0.010, 0.064, 0.002, 'paintOliveDark', [0, y0 + 0.020, zf - 0.030]);
  m.box(PW - 0.036, 0.016, 0.004, 0.001, 'paintOliveDark', [0, y0 + 0.031, zf - 0.060]);
  m.cyl(0.004, 0.11, 6, 0, 'greasePencil', [-0.36, y0 + 0.029, zf - 0.040], [0, 0, 90]); m.cyl(0.0045, 0.05, 6, 0, 'chalkWhite', [-0.25, y0 + 0.0295, zf - 0.035], [0, 20, 90]);
  // ---- header: MASAYA DÖN enamel sign (hover lift part), stencil title (TMP on paint), chalk slate, plan flap
  const H = BL.header;
  m.part('BackSign', { pivot: P(H.back[0] + H.back[2] / 2, H.back[1] + H.back[3] / 2, zf - 0.002), axis: 'z', kind: 'slide', range: [0, -0.004], note: 'MASAYA DÖN levhası: üzerine gelince 4 mm öne; tık = masaya dönüş (Esc ve sağ tık da)' });
  { const c = P(H.back[0] + H.back[2] / 2, H.back[1] + H.back[3] / 2); m.box(H.back[2] / 1000, H.back[3] / 1000, 0.002, 0.0008, 'enamelBadge', [c[0], c[1], zf - 0.002], [0, 0, 0], 'BackSign', { cast: false });
    m.text('TXT_Back', { part: 'BackSign', center: [c[0], c[1], zf - 0.0032], size: [(H.back[2] - 8) / 1000, H.back[3] / 1000 * 0.7], role: 'plateLabel', capMm: 10, sample: '◄ MASAYA DÖN', sampleEn: '◄ BACK TO DESK' }); }
  { const c = P(H.title[0] + H.title[2] / 2, H.title[1] + H.title[3] / 2); m.text('TXT_Title', { center: [c[0], c[1], zf - 0.0004], size: [H.title[2] / 1000, H.title[3] / 1000], role: 'wallStencil', font: 'Big Shoulders Stencil Display 800', capMm: 15, align: 'left', sample: 'VARDİYA PLANI · SIĞINAK 07', sampleEn: 'SHIFT PLAN · SHELTER 07' }); }
  { const c = P(H.slate[0] + H.slate[2] / 2, H.slate[1] + H.slate[3] / 2); m.box(H.slate[2] / 1000, H.slate[3] / 1000, 0.003, 0.001, 'slate', [c[0], c[1], zf - 0.0015], [0, 0, 0], 'Body', { cast: false });
    m.box(H.slate[2] / 1000 + 0.008, 0.004, 0.006, 0.001, 'woodRaw', [c[0], c[1] - H.slate[3] / 2000 - 0.002, zf - 0.003]);
    m.text('TXT_Header', { center: [c[0], c[1], zf - 0.0032], size: [(H.slate[2] - 8) / 1000, H.slate[3] / 1000 * 0.86], role: 'chalkNote', font: 'Kalam 700', capMm: 11, align: 'left', sample: 'V.001 · erzak 24 (−2,1) · 8/10 kişi', sampleEn: 'S.001 · rations 24 (−2.1) · 8/10 people', note: 'tebeşir; plan değiştikçe beklenen değişim yeniden yazılır (silinme izi 0,2 s)' }); }
  { const c = P(H.flap[0] + H.flap[2] / 2, H.flap[1] + H.flap[3] / 2);
    const ft = c[1] + H.flap[3] / 2000;   // hinge on the top edge (flip sign)
    m.part('Flap', { pivot: [c[0], ft, zf - 0.006], axis: 'x', range: [0, 180], rest: 0, note: 'iki yüzlü plaka, üst kenarından menteşeli: 0 = PLAN AÇIK (krem), 180 = PLAN KİLİTLİ (kırmızı yüz öne gelir); 0,3 s döner' });
    m.box(H.flap[2] / 1000, H.flap[3] / 1000, 0.003, 0.001, 'enamelBadge', [c[0], c[1], zf - 0.006], [0, 0, 0], 'Flap', { cast: false });
    m.box(H.flap[2] / 1000 - 0.004, H.flap[3] / 1000 - 0.004, 0.0004, 0, 'coverRed', [c[0], c[1], zf - 0.0043], [0, 0, 0], 'Flap', { cast: false });
    m.cyl(0.0025, H.flap[2] / 1000 + 0.012, 8, 0, 'steelBare', [c[0], ft + 0.002, zf - 0.006], [0, 0, 90]);
    m.text('TXT_FlapA', { part: 'Flap', center: [c[0], c[1], zf - 0.0077], size: [(H.flap[2] - 8) / 1000, H.flap[3] / 1000 * 0.7], role: 'plateLabel', capMm: 9.5, sample: 'PLAN AÇIK', sampleEn: 'PLAN OPEN' });
    m.text('TXT_FlapB', { part: 'Flap', center: [c[0], c[1], zf - 0.0041], size: [(H.flap[2] - 8) / 1000, H.flap[3] / 1000 * 0.7], normal: [0, 0, 1], up: [0, -1, 0], role: 'plateLabel', capMm: 9.5, sample: 'PLAN KİLİTLİ', sampleEn: 'PLAN LOCKED', note: 'arka yüz (kırmızı); plaka 180° dönünce öne gelir' }); }
  // ---- task pockets: brass frame + plate; field pockets share a red band
  const names = { tek: ['TEKNİSYEN', 'TECHNICIAN'], emp: ['EMP UZMANI', 'EMP SPECIALIST'], ana: ['ANALİST', 'ANALYST'], uret: ['ÜRETİM EKİBİ', 'PRODUCTION'], mutf: ['MUTFAK', 'KITCHEN'], egit: ['EĞİTİM', 'TRAINING'] };
  for (const id of SLOTS) { const [x, y, w, h] = BL.pockets[id]; frameRect(m, x, y, w, h);
    plate(m, 'TXT_P_' + id, x + 2, y + 2, Math.min(w, 108) - 4, 18, { sample: names[id][0], sampleEn: names[id][1], cap: 9.5 });
    const c = BL.card[id]; m.anchor('Slot_' + id, { pos: P(c[0], c[1], zf - 0.0025), note: `${names[id][0]} cebi: kişi kartının merkezi (kart 80 × 120 mm, mıknatıs üst sağda). Teçhizat şeritleri kartın altında 14 mm arayla (kit, zırh, yardımcı)` });
    m.text('TXT_S_' + id, { center: P(c[0], c[1] + 81), size: [0.076, 0.040], role: 'printLabel', capMm: 6.6, sample: id < 'b' || ['tek', 'emp', 'ana'].includes(id) ? 'KİT · GEREKLİ / ZIRH / YARDIMCI' : 'KİT / ZIRH / YARDIMCI', sampleEn: 'KIT / ARMOUR / AID', note: 'boş cebin baskısı: üç satır, 14 mm aralık; dolu cepte kartın altında kalır' }); }
  // red band over the three field pockets (SAHA · BASKIN KADROSU)
  { const c = P(186, 41.5); m.box(0.344, 0.004, 0.0006, 0, 'coverRed', [c[0], c[1], zf - 0.0003], [0, 0, 0], 'Body', { cast: false });
    m.text('TXT_FieldBand', { center: P(186, 37.6), size: [0.30, 0.0062], role: 'printLabel', capMm: 4.4, sample: 'SAHA · BASKIN KADROSU', sampleEn: 'FIELD · RAID TEAM' }); }
  // EĞİTİM topic list (TMP, 5 rows) and the pencil ring (drawn by the dress module as a decal)
  m.text('TXT_Topics', { center: P(BL.topics.x + 34, BL.topics.y0 + 40), size: [0.068, 0.110], role: 'printLabel', capMm: 6.8, align: 'left', sample: 'TEKNİSYEN / EMP / ANALİZ / ÜRETİM / MUTFAK', sampleEn: 'TECHNICIAN / EMP / ANALYSIS / PRODUCTION / KITCHEN', note: 'beş satır, 22 mm aralık; seçilen konu kırmızı yağlı kalem halkasıyla (decal) — tıklanan satıra yeniden çizilir' });
  m.anchor('Topics', { pos: P(BL.topics.x, BL.topics.y0), note: 'ilk konu satırının sol taban noktası; satır aralığı 22 mm' });
  // ---- BOŞTA: zone frame, plate, card tray (deck)
  { const [x, y, w, h] = BL.bosta; frameRect(m, x, y, w, h, 'brassWorn', 2.5); plate(m, 'TXT_P_bosta', x + 8, y + 2, 210, 18, { sample: 'BOŞTA / DİNLENME', sampleEn: 'IDLE / REST', cap: 9.5 });
    const [dx, dy] = BL.deck; const c = P(dx, dy); const tz = zf - 0.012;
    m.box(0.094, 0.006, 0.026, 0.001, 'paintOliveDark', [c[0], P(0, dy + 108)[1], zf - 0.013]);                // tray floor (card + item bands fit)
    m.box(0.094, 0.012, 0.003, 0.001, 'paintOliveDark', [c[0], P(0, dy + 102)[1], zf - 0.0255]);              // front lip
    for (const s of [-1, 1]) m.box(0.003, 0.176, 0.026, 0.001, 'paintOliveDark', [c[0] + s * 0.0455, P(0, dy + 21)[1], zf - 0.013]);
    m.anchor('Bosta_Deck', { pos: [c[0], c[1], tz], note: 'destenin üst kartı (kartlar 1 mm sağ-alt kaymalı yığın)' });
    m.anchor('Bosta_HandLeft', { pos: P(BL.handLeft, dy, zf - 0.003), note: 'açık elin en sol kartı; aralık min(82, 636/(n−1)) mm; her kart 320 ms, 70 ms ara, üstten başlar' }); }
  // ---- İŞ EMRİ: zone frame, plate, clip bar over the order sheet
  { const [x, y, w, h] = BL.order; frameRect(m, x, y, w, h, 'brassWorn', 2.5); plate(m, 'TXT_P_order', x + 8, y + 2, 230, 18, { sample: 'ÜRETİM · İŞ EMRİ', sampleEn: 'PRODUCTION · WORK ORDER', cap: 9.5 });
    const [sx, sy, sw, sh] = BL.sheet; const c = P(sx + sw / 2, sy + sh / 2);
    m.add(gPlane(sw / 1000, sh / 1000), 'paper', [c[0], c[1], zf - 0.0006], [0, 0, 0], 'Body', { cast: false });
    m.box(0.20, 0.012, 0.006, 0.002, 'brassWorn', [c[0], P(0, sy + 5)[1], zf - 0.004]); for (const s of [-1, 1]) m.cyl(0.004, 0.004, 8, 0, 'steelBare', [c[0] + s * 0.09, P(0, sy + 5)[1], zf - 0.0075], [90, 0, 0]);
    m.anchor('Order_Recipe', { pos: P(BL.recipe[0], BL.recipe[1], zf - 0.0012), note: 'tarif kartının yeri (112 × 80); serbest bırakılabilir, çoğu oyuncu basılı kutuya koyar' });
    const [ix, iy, iw, ih] = BL.inputs; m.anchor('Order_Inputs', { pos: P(ix + iw / 2, iy + ih / 2, zf - 0.0012), note: `girdi alanı ${iw} × ${ih} mm: hammadde fişleri (56 × 40) bırakıldıkları serbest konumda kalır; kayıtla korunur` }); }
  // ---- KİŞİ DOSYASI: zone frame, plate, wire basket holding the closed folder
  { const [x, y, w, h] = BL.folder; frameRect(m, x, y, w, h, 'brassWorn', 2.5); plate(m, 'TXT_P_dosya', x + 8, y + 2, 228, 18, { sample: 'KİŞİ DOSYASI', sampleEn: 'PERSONAL FILE', cap: 9.5 });
    const [fx, fy, fw, fh] = BL.folderFace; const c = P(fx + fw / 2, fy + fh / 2);
    const by = P(0, fy + fh - 30)[1];
    m.box(0.15, 0.003, 0.003, 0, 'wireBlack', [c[0], by, zf - 0.012]); m.box(0.15, 0.003, 0.003, 0, 'wireBlack', [c[0], by - 0.028, zf - 0.012]);
    for (const s of [-1, 0, 1]) m.box(0.003, 0.031, 0.003, 0, 'wireBlack', [c[0] + s * 0.0735, by - 0.014, zf - 0.012]);
    for (const s of [-1, 1]) m.box(0.003, 0.003, 0.012, 0, 'wireBlack', [c[0] + s * 0.0735, by - 0.028, zf - 0.006]);
    m.anchor('Folder', { pos: [c[0], c[1], zf - 0.006], note: 'kapalı klasörün merkezi (130 × 180); seçilen kişinin adı kapakta (TMP). Tık: klasör öne gelir ve açılır' }); }
  // ---- DEPO: plates, material clips, kit rack count tags; book hook; scrap bin
  { const D = BL.depot; plate(m, 'TXT_P_ham', 140, D[1] + 2, 344, 16, { sample: 'DEPO · HAMMADDE', sampleEn: 'STORE · RAW MATERIALS', cap: 8.6, screws: false, chamfer: 0 });
    plate(m, 'TXT_P_kit', 500, D[1] + 2, 492, 16, { sample: 'DEPO · KULLANILABİLİR KİTLER', sampleEn: 'STORE · AVAILABLE KITS', cap: 8.6, screws: false, chamfer: 0 });
    const mk = ['metal', 'devre', 'kumas', 'kimyasal'];
    BL.mats.forEach((x, i) => { const c = P(x, BL.matY); m.anchor('Mat_' + mk[i], { pos: [c[0], c[1], zf - 0.0025], note: 'hammadde stok kartı (80 × 56): büyük serbest sayı + birim noktaları; buradan sürüklenen her kart 1 adet ayırır' }); });
    BL.kits.forEach((x, i) => { const c = P(x, BL.kitY); m.anchor('Kit_' + i, { pos: [c[0], c[1], zf - 0.0025], note: 'aynı türden eşya kartı yığını (80 × 50); üst kart sürüklenir' });
      const t = P(x + 26, BL.kitY + 34); m.box(0.024, 0.012, 0.0015, 0, 'brass', [t[0], t[1], zf - 0.0008], [0, 0, 0], 'Body', { cast: false });
      m.text('TXT_Count_' + i, { center: [t[0], t[1], zf - 0.0017], size: [0.022, 0.010], role: 'plateLabel', capMm: 7, sample: '×2', sampleEn: '×2', note: 'yığındaki adet' }); });
    const bk = P(BL.book[0], BL.book[1]); m.add(gTorus(0.007, 0.0015, 6, 12, Math.PI * 1.3), 'brassWorn', [bk[0], bk[1] - 0.004, zf - 0.008], [0, 0, 200]);
    m.cyl(0.003, 0.012, 6, 0, 'brassWorn', [bk[0], bk[1], zf - 0.006], [90, 0, 0]);
    m.anchor('Book_Hook', { pos: [bk[0], bk[1] - 0.006, zf - 0.012], note: 'tarif defterinin ip halkası buraya asılır (defter 105 × 148 × 18)' });
    // scrap bin (galvanised, slotted lid on a hinge part)
    const [hx, hy, hw, hh] = BL.hurda; const hc = P(hx + hw / 2, hy + hh / 2); const bz = zf - 0.040;
    m.box(hw / 1000, hh / 1000, 0.07, 0.003, 'galv', [hc[0], hc[1], bz]);
    m.part('HurdaLid', { pivot: [hc[0], P(0, hy)[1], bz + 0.035], axis: 'x', range: [0, 28], rest: 0, note: 'eşya kartı bırakılınca kapak 28° kalkar (0,15 s) ve kapanır; tok “tunk”' });
    m.box(hw / 1000 + 0.006, 0.006, 0.076, 0.002, 'galv', [hc[0], P(0, hy)[1] + 0.003, bz], [0, 0, 0], 'HurdaLid');
    m.box(0.09, 0.0062, 0.010, 0, 'punchHole', [hc[0], P(0, hy)[1] + 0.0035, bz - 0.016], [0, 0, 0], 'HurdaLid', { cast: false });
    m.text('TXT_P_hurda', { center: [hc[0], hc[1] + 0.012, bz - 0.0353], size: [0.13, 0.05], role: 'wallStencil', font: 'Big Shoulders Stencil Display 800', capMm: 11, sample: 'HURDA', sampleEn: 'SCRAP', note: 'ikinci satır küçük: “1 eşya → 1 metal” / “1 item → 1 metal”' });
    m.anchor('Hurda_Slot', { pos: [hc[0], P(0, hy)[1] + 0.01, bz - 0.02], note: 'eşya kartının bırakıldığı yarık (hedef: kutunun bütün ön yüzü)' }); }
  // ---- lock rod (plan locked): steel rod through eyelets between the rows, padlock and tag
  { const y = P(0, BL.lockY)[1], z = zf - 0.012;
    for (const x of [6, 372, 740, 1154]) { const c = P(x, BL.lockY); m.add(gTorus(0.0075, 0.0018, 4, 6), 'steelBare', [c[0], y, z], [0, 90, 0]); m.box(0.004, 0.004, 0.012, 0, 'steelBare', [c[0], y, z + 0.006]); }
    m.part('LockRod', { pivot: P(1170, BL.lockY, z), kind: 'swap', note: 'görünür = PLAN KİLİTLİ (baskın emri verildi ya da devir fişi basıldı). Animasyon: çubuk sağ dikmeden 0,4 s’de kayar, asma kilit “klak”' });
    m.cyl(0.0055, 1.19, 10, 0, 'steelBare', [0, y, z], [0, 0, 90], 'LockRod');
    m.add(gTorus(0.012, 0.0028, 4, 8, Math.PI), 'steelBare', [0.585, y - 0.012, z], [0, 0, 180], 'LockRod');
    m.box(0.034, 0.030, 0.014, 0.003, 'brassWorn', [0.585, y - 0.034, z], [0, 0, 0], 'LockRod');
    m.box(0.052, 0.034, 0.0008, 0, 'tagPaper', [0.548, y - 0.046, z - 0.006], [0, 0, 8], 'LockRod');
    m.text('TXT_LockTag', { part: 'LockRod', center: [0.548, y - 0.046, z - 0.0066], size: [0.048, 0.030], role: 'handNote', font: 'Kalam 700', capMm: 5, sample: 'PLAN KİLİTLİ\nbaskın emri', sampleEn: 'PLAN LOCKED\nraid order', note: 'ikinci satır nedeni söyler: baskın emri / devir fişi basıldı' }); }
  // ---- board lamp on the top frame (spot, shadows) — its own light for K4
  { const yb = y1 + 0.002; m.box(0.04, 0.012, 0.03, 0.002, 'steelCast', [0, yb + 0.006, zf + 0.004]);
    m.add(gTube([[0, yb + 0.012, -(zf + 0.004)], [0, yb + 0.12, -(zf - 0.04)], [0, yb + 0.16, -(zf - 0.30)]].map(([x, y, z]) => [x, y, z]), 0.006, 6, 10), 'paintOliveDark', [0, 0, 0]);
    m.lathe([[0.012, 0], [0.05, -0.05], [0.056, -0.056], [0.05, -0.053], [0.01, -0.004]], 12, 'paintOlive', [0, yb + 0.16, zf - 0.30], [-62, 0, 0]);
    m.part('Lamp', { pivot: [0, yb + 0.14, zf - 0.32], kind: 'swap', note: 'lampGlassWarm ↔ roomBulbOff' });
    m.add(gSphere(0.018, 10, 6), 'lampGlassWarm', [0, yb + 0.142, zf - 0.318], [0, 0, 0], 'Lamp', { cast: false });
    m.light('BoardLamp', { part: 'Lamp', pos: [0, yb + 0.13, zf - 0.33], type: 'spot', color: '#FFE2B8', shadows: true, note: 'pano lambası: spot 48°, hedef pano merkezi (0, 1,29, −0,02); K4’te tek gölgeli ışık + tavan ampulü (gölgesiz dolgu)' }); }
  // ---- camera anchors (K4)
  m.anchor('Cam_K4', { pos: [0, BOARD.IY0 - 0.300, -0.98], note: 'K4 kamerası: pano merkezine 0,96 m, düşey FOV 41°, eğimsiz; 1920 × 1080’de 1,52 px/mm' });
  m.anchor('Look_K4', { pos: [0, BOARD.IY0 - 0.300, zf], note: 'K4 bakış noktası: iç alanın merkezi' });
  m.meta.place = [{ pos: [-1.62, 0, 1.70], yaw: -48, note: 'dünya; eski raf/maketin yeri (sol arka köşe), ön yüz masanın sol önüne dönük; K1’de yandan, küçük görünür. Arka ayaklar 0,12 m (duvara 4 cm)' }];
  return m;
}

// ================================================================ cards (one thickness), each with a UV 0–1 face and back
function card(name, w, h, list, o = {}) {
  const m = new Model(name, { family: 'board', budget: 'small', listName: list, origin: 'kart merkezi, arka yüz z = 0 (panoya değen yüz); ön yüz −z',
    note: (o.note || '') + ' Kalınlık 0,6 mm (bütün kartlarda aynı). Yüz dokusu çalışma anında çizilir (cardFace, UV 0–1), arka yüz cardBack.' });
  const t = CARD.t; m.box(w, h, t, 0, 'cardEdge', [0, 0, -t / 2]);
  m.add(gPlane(w, h), 'cardFace', [0, 0, -t - 0.0001], [0, 0, 0], 'Body', { cast: false });
  m.add(gPlane(w, h), 'cardBack', [0, 0, 0.0001], [0, 180, 0], 'Body', { cast: false });
  if (o.magnet) { m.part('Magnet', { pivot: o.magnet, kind: 'swap', note: 'panodayken görünür; elde ve defterde gizli' }); m.cyl(0.005, 0.0035, 12, 0.0008, 'magnetBlack', [o.magnet[0], o.magnet[1], -t - 0.002], [90, 0, 0], 'Magnet'); }
  m.text('FACE', { center: [0, 0, -t - 0.0002], size: [w, h], role: 'cardFace', note: 'bütün yüz: kart dokusu (boardtex.js düzeni)' });
  return m;
}
export function CardPerson() { const [w, h] = CARD.person; const m = card('CardPerson', w, h, 'Kişi kartı (80 × 120)', { magnet: [w / 2 - 0.012, h / 2 - 0.008, 0], note: 'Ad 12,5 mm, meslek 9 mm, portre 44 × 48 mm, ustalık sütunu (T/E/A/Ü/M + üç nokta), CAN/HAR/GÜÇ hücreleri, görev/durum şeridi.' });
  m.part('Flag', { pivot: [-0.008, h / 2, -0.0012], kind: 'swap', note: 'İNCELE bayrağı: kartın üst kenarından 9 mm taşan kırmızı kâğıt, ataşla tutturulmuş (dosyaya yeni talimatla eşleşen kayıt düştü)' });
  m.box(0.018, 0.012, 0.0004, 0, 'coverRed', [-0.008, h / 2 + 0.002, -0.0012], [0, 0, 0], 'Flag', { cast: false }); m.box(0.004, 0.016, 0.0012, 0, 'steelBare', [-0.003, h / 2 - 0.002, -0.0016], [0, 0, 0], 'Flag', { cast: false });
  m.part('Tape', { pivot: [-0.028, 0.02, -0.0008], kind: 'swap', note: 'yaralı: yarı saydam bant portrenin sol üst köşesinde (r_tape)' });
  m.box(0.032, 0.009, 0.0002, 0, 'r_tape', [-0.022, 0.026, -0.0009], [0, 0, 36], 'Tape', { cast: false });
  m.anchor('ItemStrip', { pos: [0, -h / 2 - 0.007, 0.0008], note: 'ilk teçhizat şeridinin merkezi (kartın arkasında); şeritler 14 mm aşağı kayar: kit, zırh, yardımcı' });
  return m; }
export function CardItem() { const [w, h] = CARD.item; return card('CardItem', w, h, 'Teçhizat kartı (80 × 50)', { magnet: [w / 2 - 0.009, h / 2 - 0.007, 0], note: 'Üst 14 mm sekme: yuva bandı (kit düz, zırh tarama, yardımcı nokta — gri tonda ayrılır) + simge + ad 9,4 mm. Kişi kartının arkasında yalnız sekme görünür.' }); }
export function CardMaterial() { const [w, h] = CARD.material; return card('CardMaterial', w, h, 'Hammadde stok kartı (80 × 56)', { magnet: [w / 2 - 0.009, h / 2 - 0.007, 0], note: 'Büyük serbest sayı (22 mm), her birim bir nokta (ayrılmış = kırmızı halka), alt satırda stok ve ayrı.' }); }
export function CardChip() { const [w, h] = CARD.chip; return card('CardChip', w, h, 'Hammadde fişi (1 adet, 56 × 40)', { note: 'İş emrine bırakılan tek birim; kırmızı üst şerit = ayrılmış.' }); }
export function CardRecipe() { const [w, h] = CARD.recipe; return card('CardRecipe', w, h, 'Tarif kartı (112 × 80)', { magnet: [w / 2 - 0.010, h / 2 - 0.008, 0], note: 'Defterden sürüklenen tarifin kopyası: ad, simge, girdiler, süre; iki vardiyalık tarifte ilerleme kutuları.' }); }

// ================================================================ RecipeBook (A6 hardcover, hangs on the board; opens forward for reading)
export function RecipeBook() {
  const m = new Model('RecipeBook', { family: 'board', budget: 'small', listName: 'Tarif defteri (ciltli, A6)', origin: 'sırt alt kenarının ortası; kapalıyken kapak −z',
    note: 'Bordo bez cilt, krem etiket; ip halkayla panonun sol alt kancasında asılı. Tık: defter 0,35 s’de panonun önüne (okuma konumu) gelir ve açılır; üç açılım, her açılımda iki tarif. Sayfa köşesine tık: önceki/sonraki (0,25 s çevirme). Kapak ya da pano boşluğuna tık: kapanır, yerine döner.' });
  const W = 0.105, H = 0.148, T = 0.018, ct = 0.0024;
  // spine on the left edge (x = 0), back cover, right page block (static), left page block + front cover (part Cover)
  m.box(0.004, H, T, 0.001, 'bookCloth', [0.002, H / 2, 0]);
  m.box(W, H, ct, 0.0008, 'bookCloth', [W / 2, H / 2, T / 2 - ct / 2]);
  m.box(W - 0.005, H - 0.006, (T - 2 * ct) / 2, 0, 'pageEdge', [W / 2 + 0.001, H / 2, (T - 2 * ct) / 4]);
  m.add(gPlane(W - 0.006, H - 0.008), 'pageFace', [W / 2 + 0.001, H / 2, -0.0002], [0, 0, 0], 'Body', { cast: false });
  m.part('Cover', { pivot: [0, 0, -T / 2], axis: 'y', range: [0, -178], rest: 0, note: 'ön kapak sırttan açılır; okuma konumunda −178°' });
  m.box(W, H, ct, 0.0008, 'bookCloth', [W / 2, H / 2, -T / 2 + ct / 2], [0, 0, 0], 'Cover');
  m.box(W - 0.005, H - 0.006, (T - 2 * ct) / 2, 0, 'pageEdge', [W / 2 + 0.001, H / 2, -(T - 2 * ct) / 4 - 0.0004], [0, 0, 0], 'Cover');
  m.add(gPlane(W - 0.006, H - 0.008), 'pageFace', [W / 2 + 0.001, H / 2, -0.00005], [0, 180, 0], 'Cover', { cast: false });   // on the inner face of the cover's page block
  m.add(gPlane(W - 0.012, H - 0.02), 'bookCloth', [W / 2, H / 2, -T / 2 - 0.0001], [0, 0, 0], 'Cover', { cast: false });
  m.part('Leaf', { pivot: [0.0005, 0, -0.0004], axis: 'y', range: [0, -176], rest: 0, note: 'çevrilen tek yaprak (önceki/sonraki), iki yüzü sayfa dokusu' });
  m.box(W - 0.006, H - 0.008, 0.0003, 0, 'pageEdge', [W / 2 + 0.0005, H / 2, -0.0006], [0, 0, 0], 'Leaf', { cast: false });
  m.text('TXT_Cover', { part: 'Cover', center: [W / 2 + 0.003, 0.098, -T / 2 - 0.0003], size: [0.066, 0.034], role: 'plateLabel', capMm: 8, sample: 'TARİF DEFTERİ', sampleEn: 'RECIPE BOOK' });
  m.text('TXT_PageL', { part: 'Cover', center: [W / 2 + 0.001, H / 2, -0.001], size: [W - 0.01, H - 0.012], normal: [0, 0, 1], role: 'recipePage', note: 'açılımın sol sayfası (tarif sayfası, R10)' });
  m.text('TXT_PageR', { center: [W / 2 + 0.001, H / 2, -0.0004], size: [W - 0.01, H - 0.012], role: 'recipePage', note: 'açılımın sağ sayfası' });
  // cord loop at the top of the spine
  m.add(gTorus(0.012, 0.0012, 4, 12), 'rp_rope', [0.006, H + 0.010, 0], [0, 90, 0]);
  m.anchor('Hang', { pos: [0.006, H + 0.021, 0], note: 'ip halkasının tepe noktası (Book_Hook’a oturur)' });
  m.anchor('Read', { pos: [0, H / 2, 0], note: 'okuma konumunda sırt ortası: kameradan 0,45 m, pano merkezinin 0,30 m önünde' });
  return m;
}

// ================================================================ PersonFolder (manila, opens forward)
export function PersonFolder() {
  const m = new Model('PersonFolder', { family: 'board', budget: 'small', listName: 'Kişi dosyası klasörü (manila)', origin: 'sol kenarın alt ucu (menteşe çizgisi); kapalıyken ön kapak −z',
    note: 'Manila klasör 130 × 180 mm, panonun sağındaki tel sepette. Kapakta KİŞİ DOSYASI ve seçili kişinin adı. Tık ya da kişi kartına çift tık: klasör öne gelir (0,35 s) ve açılır; sol sayfa kimlik/beden/ustalık/eşya, sağ sayfa belgeler/geçmiş/izler ve İNCELE · ERİŞİM · TAHLİYE eylem şeridi.' });
  const W = 0.130, H = 0.180, t = 0.0012;
  m.box(W, H, t, 0, 'manila', [W / 2, H / 2, 0.002]);
  m.add(gPlane(W - 0.008, H - 0.01), 'paper', [W / 2, H / 2, 0.0012], [0, 0, 0], 'Body', { cast: false });
  m.part('Cover', { pivot: [0, 0, 0], axis: 'y', range: [0, -176], rest: 0, note: 'ön kapak sol kenardan açılır' });
  m.box(W, H, t, 0, 'manila', [W / 2, H / 2, -0.0006], [0, 0, 0], 'Cover');
  m.text('TXT_Face', { part: 'Cover', center: [W / 2, H * 0.62, -0.0013], size: [W - 0.016, 0.06], role: 'plateLabel', capMm: 9, sample: 'KİŞİ DOSYASI\nSELİN', sampleEn: 'PERSONAL FILE\nSELİN' });
  m.text('TXT_PageL', { part: 'Cover', center: [W / 2, H / 2, 0.0002], size: [W - 0.01, H - 0.012], normal: [0, 0, 1], role: 'filePage', note: 'açık klasörün sol sayfası (kişi dosyası, R10)' });
  m.text('TXT_PageR', { center: [W / 2, H / 2, 0.0011], size: [W - 0.01, H - 0.012], role: 'filePage', note: 'sağ sayfa' });
  return m;
}

export const MODELS = {
  PlanningBoard: { build: PlanningBoard },
  CardPerson: { build: CardPerson },
  CardItem: { build: CardItem },
  CardMaterial: { build: CardMaterial },
  CardChip: { build: CardChip },
  CardRecipe: { build: CardRecipe },
  RecipeBook: { build: RecipeBook },
  PersonFolder: { build: PersonFolder },
};
