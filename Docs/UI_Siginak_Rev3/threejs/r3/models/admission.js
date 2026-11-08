// Admission counter and tools at the door (supplement 2026-11-05, R17) — owner: door agent. Family 'admission'.
// Station = wall right of the door (seen from the room), world x 2.20, z −0.69…0.15: fold-down counter (AdmissionCounter)
// under a wall panel (AdmissionPanel: pass-through document drawer, outgoing slot, instruction strip holder with line
// jewels, old-strip pocket, findings slip mouth), counter lamp above. Breath glass on the door leaf beside the peephole
// (ColdGlass; the dress module parents it to the leaf). Loose tools on the counter: DecisionStampRack + 4 DecisionStamp,
// Magnifier (lens on a comparison ruler), RequestPad, RationTin, FileTag. Wall objects: origin on the wall plane (z = 0),
// object extends to −z (room); placed with yaw 90 → model +x = world −z, model −z = world −x. Every word is a TMP slot.
import * as THREE from 'three';
import { Model, gExtrude, gSphere, gTorus, gRing, roundedRect, circlePts, D2R } from '../mlib.js';
import { defMat } from '../mats.js';
import { segCyl, segBox, addU, wp, screwHead, V } from './shelf.js';

defMat('coldGlass', { hex: '#8FA6A8', rough: 0.06, metal: 0, alpha: 0.42, note: 'buğu camı: kalın soğuk cam (Transparent)' });
defMat('fogFrost', { hex: '#E9EFEC', rough: 0.95, metal: 0, alpha: 0.78, note: 'nefes buğusu katmanı (Fog swap; Transparent, kenarı yumuşak doku tx_fog)' });
defMat('tinOlive', { hex: '#56603F', rough: 0.5, metal: 0.35, note: 'erzak tenekesi boyası' });
defMat('tinRim', { hex: '#A3A79E', rough: 0.35, metal: 0.85, note: 'teneke kenarı (çıplak metal)' });
defMat('stampWood', { hex: '#6E4A2C', rough: 0.55, metal: 0, note: 'damga sapı (cilalı kayın)' });
defMat('enamelShade', { hex: '#2F3A2E', rough: 0.4, metal: 0.1, note: 'tezgâh lambası emaye başlık (dış)' });
defMat('enamelInner', { hex: '#E9E2CC', rough: 0.45, metal: 0, note: 'tezgâh lambası başlık içi (krem emaye)' });

export const ADM = { base: [2.2, 0.98, -0.27], yaw: 90, W: 0.84, D: 0.38, top: 0.98 };
const place = (note, dy = 0) => ({ pos: [ADM.base[0], ADM.base[1] + dy, ADM.base[2]], yaw: ADM.yaw, note: 'dünya; kapı duvarı (x 2,20), kapının sağı (odadan), z −0,69…0,15: ' + note });
// world position of a counter-local point (model space of AdmissionCounter / AdmissionPanel)
export const admWorld = (p) => wp(ADM.base, ADM.yaw, p);

// ---------------------------------------------------------------- AdmissionCounter
export function AdmissionCounter() {
  const m = new Model('AdmissionCounter', { family: 'admission', budget: 'big', listName: 'Kabul tezgâhı (duvara katlanır çelik)', origin: 'sabit rafın arka üst kenarının ortası, duvar düzlemi (z = 0), tezgâh üstü yüksekliği (dünya y 0,98)',
    note: 'Kapının sağında duvarda 85 mm sabit raf ve ona menteşeli çelik kanat (toplam 0,84 × 0,38): kenarı kıvrık sac, üstte yeşil keçe altlık, önde pirinç şerit; altında iki döner konsol. Başvuru yokken kanat kalkıp paneli örter (Top 88°, konsollar içe 90°), alt yüzündeki KABUL şablonu odaya bakar; aday gelince iner (0,4 s, metal sesi). Sabit rafta BULGULAR kıskacı.' });
  const W = ADM.W, D = ADM.D, T = 0.03, L = 0.085;
  // wall angle, fixed ledge, piano hinge
  m.box(W, 0.05, 0.012, 0.003, 'steelCast', [0, -0.045, -0.006], [0, 0, 0], 'Body', { skip: ['+z'] });
  m.box(W, T, L, 0.004, 'paintOlive', [0, -T / 2, -L / 2]);
  m.box(W - 0.06, 0.003, L - 0.03, 0.001, 'feltBlotter', [0, 0.0015, -L / 2 + 0.005], [0, 0, 0], 'Body', { cast: false });
  m.cyl(0.0075, W - 0.02, 10, 0.0015, 'steelBare', [0, -0.004, -L], [0, 0, 90]);
  for (const x of [-0.36, -0.12, 0.12, 0.36]) screwHead(m, [x, -0.045, -0.0125], [0, 0, -1], 0.006, 0.004, 'steelBare', 'Body', 6);
  // findings clip (BULGULAR) on the ledge, under the panel's slip mouth
  m.box(0.11, 0.005, 0.034, 0.002, 'steelCast', [0.02, 0.0025, -0.04], [0, 0, 0], 'Body', { cast: false });
  m.box(0.10, 0.007, 0.024, 0.003, 'brassWorn', [0.02, 0.011, -0.048], [10, 0, 0], 'Body');
  m.cyl(0.005, 0.104, 10, 0.001, 'brassWorn', [0.02, 0.01, -0.034], [0, 0, 90], 'Body', { cast: false });
  m.text('TXT_Findings', { center: [0.02, 0.0055, -0.0595], size: [0.10, 0.012], normal: [0, 1, 0], up: [0, 0, 1], role: 'plateLabel', capMm: 7, sample: 'BULGULAR', sampleEn: 'FINDINGS', note: 'kıskaç tabanının önünde, tezgâhtan okunur' });
  // hinged leaf
  m.part('Top', { pivot: [0, 0, -L], axis: 'x', range: [0, 88], note: 'kanat: 0 = inik (çalışma), 88° = kalkık, paneli örter (başvuru yok)' });
  const LD = D - L, zc = -L - LD / 2;
  m.box(W, T, LD, 0.005, 'paintOlive', [0, -T / 2, zc], [0, 0, 0], 'Top');
  m.box(W - 0.06, 0.003, LD - 0.045, 0.001, 'feltBlotter', [0, 0.0015, zc + 0.005], [0, 0, 0], 'Top', { cast: false });
  m.box(W - 0.02, 0.012, 0.004, 0.0015, 'brassDark', [0, -0.015, -D - 0.0015], [0, 0, 0], 'Top', { cast: false });
  m.box(W - 0.04, 0.024, 0.018, 0.004, 'paintOlive', [0, -T - 0.009, -D + 0.035], [0, 0, 0], 'Top');                   // stiffening channel near the front
  m.text('TXT_Under', { part: 'Top', center: [0, -T - 0.0006, -L - 0.115], size: [0.70, 0.12], normal: [0, -1, 0], up: [0, 0, -1], role: 'wallStencil', capMm: 80, sample: 'KABUL', sampleEn: 'ADMISSION', note: 'kanadın alt yüzü: kanat kalkıkken odaya bakar (şablon boya)' });
  // swing brackets (gussets) under the leaf
  for (const [name, x] of [['BracketL', -0.32], ['BracketR', 0.32]]) {
    m.part(name, { pivot: [x, -0.04, -0.012], axis: 'y', range: [0, name === 'BracketL' ? -90 : 90], note: 'döner konsol: 0 = açık (kanadı taşır), içe 90° = duvara yatık' });
    m.extrude([[0, 0], [0.30, 0], [0.30, -0.025], [0.035, -0.25], [0, -0.25]], 0.008, 0.002, 'steelCast', [x, -0.032, -0.012], [0, 90, 0], name);
    m.box(0.016, 0.24, 0.016, 0.004, 'steelBare', [x, -0.155, -0.012], [0, 0, 0], name, { cast: false });                // hinge knuckle
  }
  for (const x of [-0.32, 0.32]) m.box(0.05, 0.27, 0.01, 0.003, 'steelCast', [x, -0.15, -0.005], [0, 0, 0], 'Body', { skip: ['+z'] });  // wall straps
  m.anchor('Doc', { part: 'Top', pos: [0.02, 0.003, -0.215], note: 'başvuru belgesinin yeri (kimlik / sevk belgesi)' });
  m.anchor('Pad', { part: 'Top', pos: [-0.29, 0.003, -0.305], note: 'dış kayıt talep defteri' });
  m.anchor('Rack', { part: 'Top', pos: [0.29, 0.003, -0.30], note: 'karar damgası rafı (kanadın sağ önü, kullanıcıya dönük)' });
  m.anchor('Ruler', { part: 'Top', pos: [0.03, 0.006, -0.335], note: 'büyüteç ve karşılaştırma cetveli' });
  m.anchor('Hover', { part: 'Top', pos: [0, 0.05, -0.2], note: 'kalkıkken üzerine gelince etiket: KABUL TEZGÂHI · başvuru yok' });
  m.meta.place = [place('tezgâh üstü y 0,98; kapı kasasının yüz laması z 0,19’da biter')];
  return m;
}

// ---------------------------------------------------------------- AdmissionPanel
export function AdmissionPanel() {
  const m = new Model('AdmissionPanel', { family: 'admission', budget: 'big', listName: 'Kabul paneli (duvar: evrak gözü, giden ağzı, talimat, bulgu ağzı)', origin: 'alt kenarın ortası, duvar düzlemi (z = 0), tezgâh üstü yüksekliği (dünya y 0,98)',
    note: 'Tezgâhın arkasında duvara vidalı koyu zeytin sac panel 0,84 × 0,30. Solda duvarı geçen EVRAK GÖZÜ (çekmece 0,22 m odaya çıkar; dış kapağı sahanlıkta, dış interkomun altında), üstünde GİDEN · TELSİZ ağzı (dış kayıt talebi). Ortada BULGU ağzı (inceleme fişleri kayar), sağda pirinç çerçeveli dik TALİMAT yuvası (80 × 210, cam kapaklı) ve solunda kayan satır işaretçisi (Marker + yakut), en sağda ESKİ talimat cebi. İç interkom panelin hemen üstünde.' });
  const W = ADM.W, H = 0.30;
  m.box(W, H, 0.012, 0.004, 'paintOliveDark', [0, H / 2, -0.006], [0, 0, 0], 'Body', { skip: ['+z'] });
  for (const [x, y] of [[-0.40, 0.02], [0.40, 0.02], [-0.40, 0.28], [0.40, 0.28]]) screwHead(m, [x, y, -0.0125], [0, 0, -1], 0.006, 0.004, 'steelBare', 'Body', 6);
  // ---- evrak gözü (pass-through drawer)
  const dx = -0.26;
  m.box(0.31, 0.105, 0.026, 0.006, 'steelCast', [dx, 0.062, -0.019]);
  m.box(0.27, 0.083, 0.004, 0.001, 'paintBlack', [dx, 0.057, -0.033], [0, 0, 0], 'Body', { cast: false });
  m.part('Drawer', { pivot: [dx, 0.057, -0.034], axis: 'z', kind: 'slide', range: [0, -0.22], note: 'evrak gözü çekmecesi: 0 = kapalı, −0,22 m = odaya çıkmış (belge / teneke içinde)' });
  m.box(0.26, 0.078, 0.012, 0.004, 'steelCast', [dx, 0.057, -0.041], [0, 0, 0], 'Drawer');
  m.box(0.24, 0.004, 0.22, 0.001, 'steelBare', [dx, 0.022, 0.075], [0, 0, 0], 'Drawer', { cast: false });              // tray floor (inside the wall)
  for (const sx of [-1, 1]) m.box(0.004, 0.05, 0.22, 0.001, 'steelBare', [dx + sx * 0.118, 0.045, 0.075], [0, 0, 0], 'Drawer', { cast: false });
  m.box(0.24, 0.05, 0.004, 0.001, 'steelBare', [dx, 0.045, 0.183], [0, 0, 0], 'Drawer', { cast: false });
  segCyl(m, [dx - 0.06, 0.057, -0.047], [dx - 0.06, 0.057, -0.064], 0.004, 0.004, 8, 'brassWorn', 'Drawer', { cast: false });
  segCyl(m, [dx + 0.06, 0.057, -0.047], [dx + 0.06, 0.057, -0.064], 0.004, 0.004, 8, 'brassWorn', 'Drawer', { cast: false });
  m.cyl(0.0055, 0.13, 10, 0.0015, 'brassWorn', [dx, 0.057, -0.066], [0, 0, 90], 'Drawer');
  m.box(0.105, 0.022, 0.002, 0.0007, 'plate', [dx, 0.078, -0.0475], [0, 0, 0], 'Drawer', { cast: false });
  m.text('TXT_Drawer', { part: 'Drawer', center: [dx, 0.078, -0.0487], size: [0.095, 0.016], role: 'plateLabel', capMm: 10, sample: 'EVRAK', sampleEn: 'PAPERS' });
  m.anchor('DrawerTray', { part: 'Drawer', pos: [dx, 0.025, 0.075], note: 'çekmece tablası (dışarı çıkınca belge / iki teneke burada)' });
  // ---- outgoing slot (request to telex)
  m.box(0.25, 0.034, 0.026, 0.006, 'steelCast', [dx, 0.168, -0.019]);
  m.box(0.21, 0.007, 0.004, 0.001, 'paintBlack', [dx, 0.17, -0.033], [0, 0, 0], 'Body', { cast: false });
  m.box(0.215, 0.004, 0.006, 0.001, 'brassWorn', [dx, 0.162, -0.034], [0, 0, 0], 'Body', { cast: false });
  m.box(0.17, 0.024, 0.002, 0.0007, 'plate', [dx, 0.212, -0.013], [0, 0, 0], 'Body', { cast: false });
  m.text('TXT_Out', { center: [dx, 0.212, -0.0142], size: [0.16, 0.017], role: 'plateLabel', capMm: 9, sample: 'GİDEN · TELSİZ', sampleEn: 'OUT · RADIO' });
  m.anchor('OutSlot', { pos: [dx, 0.17, -0.036], note: 'dış kayıt talebi buraya atılır; gözetmen telsiz odasında sorar (R10 bulgu fişi: “telsizle soruldu”)' });
  // ---- instruction strip holder: portrait 80 × 210 strip (R10 kabul talimatı) in a brass frame, glass; sliding line marker
  const hx = 0.185, hy = 0.155, hw = 0.084, hh = 0.214;
  m.box(hw + 0.026, hh + 0.026, 0.006, 0.002, 'paintCream', [hx, hy, -0.015], [0, 0, 0], 'Body', { cast: false });       // backing board
  for (const sy of [-1, 1]) m.box(hw + 0.03, 0.012, 0.01, 0.002, 'brassWorn', [hx, hy + sy * (hh / 2 + 0.006), -0.021]);
  for (const sx of [-1, 1]) m.box(0.012, hh + 0.024, 0.01, 0.002, 'brassWorn', [hx + sx * (hw / 2 + 0.006), hy, -0.021]);
  m.box(hw, hh, 0.002, 0, 'glassClear', [hx, hy, -0.0235], [0, 0, 0], 'Body', { cast: false });
  m.text('TXT_Strip', { center: [hx, hy, -0.0185], size: [0.080, 0.210], role: 'paper (R10: kabul_talimati_vN, 80 × 210 mm)', note: 'talimat şeridi; okuma için tık: kâğıt öne gelir' });
  const mx = hx - hw / 2 - 0.026;
  m.box(0.008, hh, 0.006, 0.002, 'steelBare', [mx, hy, -0.016], [0, 0, 0], 'Body', { cast: false });                       // marker rail
  m.part('Marker', { pivot: [mx, hy + hh / 2 - 0.02, -0.021], axis: 'y', kind: 'slide', range: [0, -0.17], note: 'satır işaretçisi: büyüteç bir alanın üstündeyken talimatta eşleşen maddenin hizasına kayar (0,2 s); üstündeki yakut yanar' });
  m.box(0.022, 0.016, 0.01, 0.003, 'brassWorn', [mx, hy + hh / 2 - 0.02, -0.022], [0, 0, 0], 'Marker');
  m.extrude([[0, 0.006], [0.012, 0], [0, -0.006]], 0.004, 0.0008, 'brassWorn', [mx + 0.011, hy + hh / 2 - 0.02, -0.025], [0, 0, 0], 'Marker', { cast: false });
  m.part('Jewel', { parent: 'Marker', pivot: [mx - 0.002, hy + hh / 2 - 0.02, -0.028], kind: 'swap', note: 'işaretçi yakutu: jewelAmberOff ↔ jewelAmberOn (eşleşme var)' });
  m.add(gSphere(0.005, 10, 6, { thetaLength: Math.PI / 2 }), 'jewelAmberOff', [mx - 0.002, hy + hh / 2 - 0.02, -0.028], [-90, 0, 0], 'Jewel', { cast: false });
  m.box(0.07, 0.016, 0.002, 0.0007, 'plate', [hx, hy + hh / 2 + 0.024, -0.013], [0, 0, 0], 'Body', { cast: false });
  m.text('TXT_Holder', { center: [hx, hy + hh / 2 + 0.024, -0.0142], size: [0.066, 0.012], role: 'plateLabel', capMm: 7, sample: 'TALİMAT', sampleEn: 'ORDERS' });
  m.anchor('StripCenter', { pos: [hx, hy, -0.019], note: 'talimat şeridinin merkezi' });
  // ---- old-strip pocket (ESKİ): old strips stand in it, version heads visible above the lip
  const ax = 0.325;
  m.box(0.10, 0.15, 0.03, 0.004, 'steelCast', [ax, 0.10, -0.021]);
  m.box(0.088, 0.02, 0.024, 0.001, 'paintBlack', [ax, 0.17, -0.021], [0, 0, 0], 'Body', { cast: false });
  m.box(0.06, 0.02, 0.002, 0.0007, 'plate', [ax, 0.06, -0.037], [0, 0, 0], 'Body', { cast: false });
  m.text('TXT_Archive', { center: [ax, 0.06, -0.0382], size: [0.056, 0.014], role: 'plateLabel', capMm: 8, sample: 'ESKİ', sampleEn: 'OLD' });
  m.anchor('Archive', { pos: [ax, 0.175, -0.021], note: 'eski şeritler cebe dik girer; başlıkları (SÜRÜM 1, 2) üstten görünür' });
  // ---- findings slip mouth (BULGU), slips 90 × 60
  const bx = 0.02;
  m.box(0.13, 0.03, 0.026, 0.006, 'steelCast', [bx, 0.04, -0.019]);
  m.box(0.104, 0.006, 0.004, 0.001, 'paintBlack', [bx, 0.042, -0.033], [0, 0, 0], 'Body', { cast: false });
  m.box(0.108, 0.004, 0.008, 0.001, 'brassWorn', [bx, 0.035, -0.035], [0, 0, 0], 'Body', { cast: false });
  m.box(0.078, 0.02, 0.002, 0.0007, 'plate', [bx, 0.073, -0.013], [0, 0, 0], 'Body', { cast: false });
  m.text('TXT_Slip', { center: [bx, 0.073, -0.0142], size: [0.072, 0.014], role: 'plateLabel', capMm: 8, sample: 'BULGU', sampleEn: 'FINDING' });
  m.anchor('SlipMouth', { pos: [bx, 0.042, -0.036], note: 'inceleme fişi çıkış noktası (fiş 0,35 s’de 12 cm kayar, BULGULAR kıskacına düşer)' });
  m.meta.place = [place('panel tezgâhın arkasında; iç interkom (z 0,0, y 1,295) panelin hemen üstünde')];
  return m;
}

// ---------------------------------------------------------------- ColdGlass (buğu camı, on the door leaf)
export function ColdGlass() {
  const m = new Model('ColdGlass', { family: 'admission', budget: 'small', listName: 'Buğu camı (kapı kanadında, gözetleme deliğinin yanında)', origin: 'arka yüz ortası, kanat panel yüzeyi (z = 0)',
    note: 'Kanadın iç yüzünde, gözetleme deliğinin menteşe tarafında döküm halkalı kalın soğuk cam Ø 110. Dış yüzü sahanlıktaki soğuğa bakar: aday cama nefes verince buğu olur (Fog). Üstten menteşeli çelik kapak (Cover) incelemede kalkar. Kanatla birlikte döner (Hinge’e bağlı).' });
  m.lathe([[0.055, 0], [0.08, 0], [0.08, 0.008], [0.072, 0.016], [0.058, 0.018], [0.055, 0.012]], 16, 'castIron', [0, 0, 0], [-90, 0, 0], 'Body', { caps: false });
  m.cyl(0.056, 0.012, 16, 0.002, 'coldGlass', [0, 0, -0.006], [-90, 0, 0], 'Body', { cast: false });
  for (let i = 0; i < 4; i++) { const a = (i * 90 + 45) * D2R; screwHead(m, [Math.cos(a) * 0.068, Math.sin(a) * 0.068, -0.0165], [0, 0, -1], 0.005, 0.003, 'steelBare', 'Body', 6); }
  m.part('Fog', { pivot: [0, 0, -0.013], kind: 'swap', note: 'nefes buğusu: görünmez ↔ fogFrost (açılıp 0,6 s’de belirir, 2,5 s’de kenardan söner)' });
  m.cyl(0.054, 0.001, 16, 0, 'fogFrost', [0, 0, -0.0128], [-90, 0, 0], 'Fog', { cast: false });
  m.part('Cover', { pivot: [0, 0.084, -0.022], axis: 'x', range: [0, 105], note: 'çelik kapak: 0 = kapalı, 105° = kalkık (üst menteşe, odaya doğru)' });
  m.cyl(0.074, 0.005, 16, 0.0015, 'paintOlive', [0, 0, -0.0215], [-90, 0, 0], 'Cover');
  m.box(0.03, 0.012, 0.012, 0.003, 'steelCast', [0, 0.078, -0.022], [0, 0, 0], 'Cover', { cast: false });
  m.cyl(0.008, 0.014, 10, 0.002, 'bakeliteBlack', [0, -0.05, -0.031], [-90, 0, 0], 'Cover', { cast: false });
  m.box(0.10, 0.022, 0.002, 0.0007, 'plate', [0, -0.098, -0.002], [0, 0, 0], 'Body', { cast: false });
  m.text('TXT_Plate', { center: [0, -0.098, -0.0032], size: [0.095, 0.016], role: 'plateLabel', capMm: 9, sample: 'BUĞU CAMI', sampleEn: 'BREATH GLASS' });
  m.anchor('Hover', { pos: [0, 0, -0.08], note: 'üzerine gelince etiket: SOĞUK GÖZLEM · kapağı kaldır' });
  m.meta.place = [{ pos: wp([2.2, 0, 0.75], 90, [0.17, 1.46, 0.013]), yaw: 90, note: 'dünya (kapı kapalıyken); kanat yereli (0,17 · 1,46 · 0,013): gözetleme deliğinin menteşe tarafı. Dress modülü kanadın Hinge grubuna bağlar' }];
  return m;
}

// ---------------------------------------------------------------- decision stamps
export function DecisionStampRack() {
  const m = new Model('DecisionStampRack', { family: 'admission', budget: 'small', listName: 'Karar damgası rafı (dört yuva)', origin: 'taban ortası (tezgâh yüzeyi, y = 0)',
    note: 'Cilalı ahşap raf 0,25 × 0,07: dört yuvalı üst tabla, önde her yuvanın altında pirinç plaka (karar adı, iki satır). Damga yuvadan kalkınca boş yuvada plaka okunmaya devam eder.' });
  m.box(0.25, 0.012, 0.07, 0.003, 'stampWood', [0, 0.006, 0]);
  m.box(0.25, 0.012, 0.06, 0.003, 'stampWood', [0, 0.046, 0.004]);
  for (const sx of [-1, 1]) m.box(0.012, 0.034, 0.06, 0.002, 'stampWood', [sx * 0.119, 0.029, 0.004]);
  m.box(0.226, 0.03, 0.006, 0.002, 'stampWood', [0, 0.027, 0.03]);
  for (let i = 0; i < 4; i++) { const x = -0.0885 + i * 0.059;
    m.cyl(0.017, 0.003, 12, 0, 'paintBlack', [x, 0.0525, 0.004], [0, 0, 0], 'Body', { cast: false });
    m.box(0.052, 0.026, 0.002, 0.0007, 'plate', [x, 0.024, -0.036], [-12, 0, 0], 'Body', { cast: false });
    m.text('TXT_S' + (i + 1), { center: [x, 0.024, -0.0373], size: [0.05, 0.023], normal: [0, Math.sin(12 * D2R), -Math.cos(12 * D2R)], up: [0, Math.cos(12 * D2R), Math.sin(12 * D2R)], role: 'plateLabel', capMm: 6,
      sample: ['KABUL ·\nTAM ERİŞİM', 'KISITLI\nKABUL', 'REDDET', 'DIŞARIDAN\nYARDIM'][i], sampleEn: ['ADMIT ·\nFULL ACCESS', 'RESTRICTED\nADMISSION', 'REJECT', 'OUTSIDE\nAID'][i] });
    m.anchor('Slot' + (i + 1), { pos: [x, 0.052, 0.004], note: 'damga yuvası ' + (i + 1) }); }
  m.box(0.05, 0.012, 0.03, 0.004, 'steelCast', [0.153, 0.006, -0.005]);
  return m;
}
export function DecisionStamp() {
  const m = new Model('DecisionStamp', { family: 'admission', budget: 'small', listName: 'Karar damgası (lastik, ahşap saplı)', origin: 'lastik yüzün ortası (y = 0)',
    note: 'Torna ahşap sap, pirinç bilezik, renk bandı (karar mürekkebinin rengi + kelime), dikdörtgen taban ve lastik yüz 50 × 28. Sap tepesinde kısa ad (TMP). Sürüklenince kalkar, belgenin KARAR kutusunun üstünde bırakılınca 0,12 s’de basar: geri alınmaz.' });
  m.box(0.05, 0.004, 0.028, 0.001, 'rubber', [0, 0.002, 0], [0, 0, 0], 'Body', { cast: false });
  m.box(0.054, 0.012, 0.032, 0.003, 'stampWood', [0, 0.010, 0]);
  m.part('Band', { pivot: [0, 0.019, 0], kind: 'swap', note: 'renk bandı: kararın mürekkep rengi (yeşil / gri / mürekkep / mavi); kelime sap tepesinde' });
  m.cyl(0.013, 0.008, 12, 0.002, 'paintCream', [0, 0.020, 0], [0, 0, 0], 'Band', { cast: false });
  m.lathe([[0.012, 0], [0.012, 0.004], [0.0105, 0.006]], 12, 'brassWorn', [0, 0.024, 0], [0, 0, 0], 'Body', { caps: false });
  m.lathe([[0.0105, 0], [0.0085, 0.012], [0.0095, 0.03], [0.0135, 0.044], [0.0125, 0.052], [0, 0.0545]], 12, 'stampWood', [0, 0.03, 0]);
  m.text('TXT_Cap', { center: [0, 0.0848, 0], size: [0.022, 0.008], normal: [0, 1, 0], up: [0, 0, 1], role: 'plateLabel', capMm: 4, sample: 'KABUL', sampleEn: 'ADMIT', note: 'sap tepesi; asıl okunan ad raftaki plakada' });
  m.anchor('Grip', { pos: [0, 0.07, 0], note: 'tutma noktası (sürüklerken 25 mm kalkar)' });
  return m;
}

// ---------------------------------------------------------------- Magnifier on a comparison ruler
export function Magnifier() {
  const m = new Model('Magnifier', { family: 'admission', budget: 'small', listName: 'Büyüteç ve karşılaştırma cetveli', origin: 'cetvelin alt yüz ortası (y = 0)',
    note: 'Çelik cetvel 300 × 34 (ölçek ve kıl çizgisi dokuda) üstünde kayan pirinç araba ve Ø 70 mercek. Cetvel belgeye yatırılır, araba bir alanın üstüne gelince talimatta eşleşen satırın yakutu yanar; iki alan aynı hizaya alınınca fark (204 / 240) mercekte büyür.' });
  m.box(0.30, 0.003, 0.034, 0.0008, 'steelBare', [0, 0.0015, 0]);
  m.box(0.30, 0.0005, 0.004, 0, 'inkBlack', [0, 0.0032, 0.0], [0, 0, 0], 'Body', { cast: false });
  m.part('Carriage', { pivot: [-0.11, 0.003, 0], axis: 'x', kind: 'slide', range: [0, 0.22], note: 'araba cetvel boyunca kayar (0 = sol uç)' });
  m.box(0.03, 0.009, 0.04, 0.002, 'brassWorn', [-0.11, 0.006, 0], [0, 0, 0], 'Carriage');
  m.box(0.012, 0.006, 0.05, 0.002, 'brassWorn', [-0.11, 0.009, -0.035], [0, 0, 0], 'Carriage');
  m.add(gTorus(0.036, 0.004, 6, 28), 'brassWorn', [-0.11, 0.012, -0.085], [90, 0, 0], 'Carriage');
  m.cyl(0.034, 0.003, 24, 0.001, 'glassClear', [-0.11, 0.012, -0.085], [0, 0, 0], 'Carriage', { cast: false });
  m.anchor('Lens', { part: 'Carriage', pos: [-0.11, 0.012, -0.085], note: 'mercek merkezi (altındaki alan büyür)' });
  return m;
}

// ---------------------------------------------------------------- RequestPad (outside record)
export function RequestPad() {
  const m = new Model('RequestPad', { family: 'admission', budget: 'small', listName: 'Dış kayıt talep defteri (kopyalı)', origin: 'taban ortası (tezgâh yüzeyi, y = 0)',
    note: 'Sert karton altlıklı kopyalı talep defteri 110 × 150, üstte çelik kıskaç. Üst yaprak (Sheet) koparılıp GİDEN · TELEKS ağzına atılır; karbon kopya dosyada BEKLENİYOR fişi olur. Gözetmen adı yaprağa kendiliğinden yazılır (BOŞTA destesinin üst kartı; öneri).' });
  m.box(0.11, 0.004, 0.15, 0.0015, 'cardboard', [0, 0.002, 0]);
  m.box(0.104, 0.007, 0.142, 0.001, 'paper', [0, 0.0075, 0.002], [0, 0, 0], 'Body', { cast: false });
  m.part('Sheet', { pivot: [0, 0.0112, 0.068], axis: 'x', range: [0, -35], note: 'üst yaprak: kıskaçtan kalkar ve koparılır' });
  m.box(0.104, 0.0004, 0.14, 0, 'paperWhite', [0, 0.0112, 0.0], [0, 0, 0], 'Sheet', { cast: false });
  m.text('TXT_Sheet', { part: 'Sheet', center: [0, 0.0116, -0.004], size: [0.1, 0.13], normal: [0, 1, 0], up: [0, 0, 1], role: 'paper (R10: dış kayıt talebi)' });
  m.box(0.07, 0.006, 0.016, 0.002, 'steelBare', [0, 0.012, 0.066]);
  m.cyl(0.003, 0.072, 8, 0.001, 'steelBare', [0, 0.016, 0.061], [0, 0, 90], 'Body', { cast: false });
  return m;
}

// ---------------------------------------------------------------- CounterLamp
export function CounterLamp() {
  const m = new Model('CounterLamp', { family: 'admission', budget: 'small', listName: 'Tezgâh lambası (duvar kollu, emaye)', origin: 'duvar plakasının arka yüz ortası',
    note: 'Duvar plakası, 0,30 m boru kol ve emaye çan başlık (dışı koyu zeytin, içi krem), opal ampul. Tezgâhın sağ yarısının üstünde; ışığı belgelere aşağı düşer (spot 70°, gölgeli). Kameranın görüş açısının üstünde kalır.' });
  m.box(0.07, 0.10, 0.012, 0.003, 'castIron', [0, 0, -0.006], [0, 0, 0], 'Body', { skip: ['+z'] });
  segCyl(m, [0, 0.0, -0.012], [0, 0.02, -0.26], 0.009, 0.009, 8, 'steelBare');
  m.lathe([[0.02, 0], [0.022, 0.02], [0.06, 0.07], [0.075, 0.085]], 16, 'enamelShade', [0, 0.02, -0.29], [180, 0, 0], 'Body', { caps: false });
  m.lathe([[0.019, 0.001], [0.021, 0.02], [0.058, 0.069], [0.073, 0.084]], 16, 'enamelInner', [0, 0.0195, -0.29], [180, 0, 0], 'Body', { caps: false, cast: false });
  m.add(gSphere(0.022, 10, 7), 'lampGlassWarm', [0, -0.03, -0.29], [0, 0, 0], 'Body', { cast: false });
  m.light('CounterSpot', { pos: [0, -0.04, -0.29], type: 'spot', color: '#FFD9A0', shadows: true, note: 'tezgâh spotu 70°, gölgeli (K8A’da 3. gölgeli ışık); başvuru yokken kapalı' });
  m.meta.place = [{ pos: admWorld([0.16, 0.80, 0]), yaw: ADM.yaw, note: 'dünya; panelin sağ yarısının 0,80 m üstü (y 1,78)' }];
  return m;
}

// ---------------------------------------------------------------- RationTin, FileTag
export function RationTin() {
  const m = new Model('RationTin', { family: 'admission', budget: 'small', listName: 'Erzak tenekesi (DIŞARIDAN YARDIM)', origin: 'taban ortası (y = 0)',
    note: 'Boyalı konserve Ø 75 × 110, kıvrık kenarlar, ERZAK şablonu (TMP). DIŞARIDAN YARDIM’da iki teneke evrak gözünün çekmecesiyle dışarı çıkar.' });
  m.lathe([[0, 0], [0.036, 0], [0.0375, 0.004], [0.0365, 0.008], [0.0365, 0.102], [0.0375, 0.106], [0.036, 0.11], [0, 0.11]], 16, 'tinOlive');
  for (const y of [0.004, 0.106]) m.add(gTorus(0.0368, 0.0018, 4, 16), 'tinRim', [0, y, 0], [90, 0, 0], 'Body', { cast: false });
  m.text('TXT_Stencil', { center: [0, 0.055, -0.0368], size: [0.05, 0.022], role: 'wallStencil', capMm: 14, sample: 'ERZAK', sampleEn: 'RATIONS', note: 'silindir yüzü (TMP eğri değil: dar yuva)' });
  return m;
}
export function FileTag() {
  const m = new Model('FileTag', { family: 'admission', budget: 'small', listName: 'Dosya sekmesi ve kıskacı (İNCELE / KISITLI)', origin: 'kıskacın alt kenar ortası',
    note: 'Klasörün kenarına takılan çelik kıskaç ve krem karton sekme 50 × 24 (kesikli kenar, TMP: İNCELE · S3 ya da KISITLI · 3 V). Talimat değişince eşleşen eski dosyalara kendiliğinden takılır.' });
  m.box(0.02, 0.012, 0.004, 0.001, 'steelBare', [0, 0.006, 0]);
  m.box(0.05, 0.024, 0.0008, 0, 'cardStock', [0, 0.024, -0.001], [0, 0, 0], 'Body', { cast: false });
  m.text('TXT_Tag', { center: [0, 0.025, -0.0016], size: [0.046, 0.02], role: 'stamp', capMm: 6, sample: 'İNCELE', sampleEn: 'REVIEW' });
  return m;
}

export const MODELS = {
  AdmissionCounter: { build: AdmissionCounter },
  AdmissionPanel: { build: AdmissionPanel },
  ColdGlass: { build: ColdGlass },
  DecisionStampRack: { build: DecisionStampRack },
  DecisionStamp: { build: DecisionStamp },
  Magnifier: { build: Magnifier },
  RequestPad: { build: RequestPad },
  CounterLamp: { build: CounterLamp },
  RationTin: { build: RationTin },
  FileTag: { build: FileTag },
};
