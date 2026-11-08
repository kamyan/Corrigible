// Tokens 1.3 builder: reads the 1.1/1.2 token files, adds Revizyon 3 values (materials from mats.js incl. every family's
// defMat keys, menu + cabinet layout, cameras, light recipes, motion, raid kit models) and marks _new_1_3/_changed_1_3/_was_1_3.
// usage: node tokens13.mjs <outDir>
import fs from 'fs';
import { MODELS } from './registry.js';
import { MATS, NEW_KEYS } from './mats.js';
import BASE from './mats_base.js';
import { BOARD, BL, CARD, SLOTS } from './models/board.js';
const out = process.argv[2] || 'out/tokens'; fs.mkdirSync(out, { recursive: true });
const ATT = '/mnt/user-data/attachments/f9f1d8f0-179f-52d6-bfd9-32e90720d06b/';
const T = JSON.parse(fs.readFileSync(ATT + 'ac4ea53d-tokens_siginak.json', 'utf8'));
const RT = JSON.parse(fs.readFileSync(ATT + '7348e6f4-raid_tokens.json', 'utf8'));
const extra = fs.existsSync('tokens13_extra.json') ? JSON.parse(fs.readFileSync('tokens13_extra.json', 'utf8')) : {};
const sup = fs.existsSync('tokens13_sup.json') ? JSON.parse(fs.readFileSync('tokens13_sup.json', 'utf8')) : {};
// ek 11-05: models removed from the game (shelf/diorama family, visitor silhouettes, miner and fog pawns)
export const isRemoved = (n, fam) => fam === 'shelf' || /^Visitor_/.test(n) || n === 'Pawn_M' || n === 'Pawn_S';
const N = [], C = [], WAS = {}; const RN = [...(RT._new_1_3 || [])], RC = [], RWAS = {};
const get = (o, p) => p.split('.').reduce((a, k) => a == null ? a : a[k], o);
const set = (o, p, v) => { const ks = p.split('.'); let a = o; for (const k of ks.slice(0, -1)) { if (a[k] == null || typeof a[k] !== 'object') a[k] = {}; a = a[k]; } a[ks.at(-1)] = v; };
function put(doc, p, v, nl, cl, was) { const old = get(doc, p); if (old === undefined) { set(doc, p, v); nl.push(p); } else if (JSON.stringify(old) !== JSON.stringify(v)) { was[p] = old; set(doc, p, v); cl.push(p); } }
const S = (p, v) => put(T, p, v, N, C, WAS), Rr = (p, v) => put(RT, p, v, RN, RC, RWAS);
// build every model once (registers family materials) and collect usage
const models = {}, removedModels = []; for (const [n, d] of Object.entries(MODELS)) { if (n.startsWith('_')) continue; if (isRemoved(n, d.family)) { removedModels.push(n); continue; } try { models[n] = d.build(); } catch (e) { console.warn('build', n, e.message); } }
const usedBy = {}; for (const [n, m] of Object.entries(models)) m.root.traverse(o => { if (o.isMesh && o.userData.mat) (usedBy[o.userData.mat] = usedBy[o.userData.mat] || new Set()).add(n); });
const matTok = (v) => { const t = { hex: v.hex, roughness: v.rough ?? 0.8, metal: v.metal ?? 0 }; if (v.emissive) { t.emissive = v.emissive; t.emissiveIntensity = v.ei ?? 1; } if (v.alpha != null && v.alpha < 1) { t.alpha = v.alpha; t.surface = 'Transparent'; } if (v.tex) t.texture = v.tex; if (v.note) t.note = v.note; return t; };
const removedMats = []; for (const k of NEW_KEYS) { const v = MATS[k]; if (!usedBy[k] && ['shelfPost','rope','roomCard','selYellow','moltenGlow','firstAidSign','scrapMetal'].includes(k)) { removedMats.push(k); continue; } const fams = [...(usedBy[k] || [])].map(n => models[n].family || models[n].meta.family);
  const raid = fams.length && fams.every(f => /^raid/.test(f)); const tok = { ...matTok(v), usedBy: [...(usedBy[k] || [])].slice(0, 8) };
  if (raid || k.startsWith('r_') || k.startsWith('k_')) Rr('materials3d.' + k, tok); else S('materials.' + k, tok); }
// families' changes to existing keys (defMat never overrides; overrides happen only via mats.js R3 table)
for (const k of Object.keys(BASE)) if (JSON.stringify(BASE[k]) !== JSON.stringify(MATS[k])) S('materials.' + k, matTok(MATS[k]));
// model library summary
const fam = {}; for (const [n, m] of Object.entries(models)) { const f = m.family || m.meta.family; (fam[f] = fam[f] || []).push(n); }
S('models3d', { _: '★1.3 model kütüphanesi (Revizyon 3). Her model: OBJ (Y yukarı, metre, X aynalı yazıldı; Unity içe aktarınca düzelir) + MTL + manifest (boyut, üçgen, parçalar/pivotlar, malzeme anahtarları, TMP yuvaları, çapalar, ışık noktaları, dünya yerleşimi). Kaynak: threejs/r3/models/*.js.',
  path: 'modeller/obj, modeller/manifest, modeller/models.json', budgets: { big: 4000, small: 800, raid: 400, unit: 1500 },
  chamferRule: 'pah ≥ 3 ekran px: K1 6 mm · K9 2 mm · M0 5–6 mm · M2 1,5–2 mm · baskın 0,02–0,03 birim', thinRule: 'ince parça ≥ 3 px; daha incesi doku',
  families: Object.fromEntries(Object.entries(fam).filter(([f]) => !/^raid/.test(f)).map(([f, l]) => [f, l.sort()])) });
Rr('models3d', { _: '★1.3 baskın modelleri (Revizyon 3). Taş merkezi orijin, y yukarı; dünya = (u + 0,5, 0, −(v + 0,5)). Kit parçaları: Relief_/ReliefIso_/Bridge_/Rubble_<kit>; sivil Civil_<kit>; giriş kapısı EntryDoor_<kit>; kanal ChannelHub/Arm/Edge/Single.',
  families: Object.fromEntries(Object.entries(fam).filter(([f]) => /^raid/.test(f)).map(([f, l]) => [f, l.sort()])) });
for (const k of ['weu', 'eeu', 'me', 'af', 'na', 'ru', 'in', 'cn', 'jk', 'sea', 'sa', 'oc']) {
  const has = (n) => !!models[n]; const pick = (n) => has(n) ? { model: n, tris: models[n].stats().tris, note: models[n].meta.note || '' } : null;
  Rr(`kits.${k}.models`, Object.fromEntries([['relief', pick(`Relief_${k}`)], ['isolated', pick(`ReliefIso_${k}`)], ['bridge', pick(`Bridge_${k}`)], ['rubble', pick(`Rubble_${k}`)], ['civil', pick(`Civil_${k}`)], ['entryDoor', pick(`EntryDoor_${k}`)]].filter(([, v]) => v)));
}
// lead's hand-written sections (menu, cabinet, cameras, light recipes, motion, layout3d changes): tokens13_extra.json
for (const [p, v] of Object.entries(extra.siginak || {})) S(p, v);
for (const [p, v] of Object.entries(extra.raid || {})) Rr(p, v);
// ek 11-05: board layout straight from the model file, then the hand-written supplement sections and removal markers
for (const [p, v] of Object.entries(sup.siginak || {})) S(p, v);
S('board.layoutMm', { _: 'pano iç alanı, mm, sol üst köşeden; [x, y, g, y] dikdörtgenler; kart merkezleri [x, y]', pockets: BL.pockets, cardCentres: BL.card, bosta: BL.bosta, deck: BL.deck, handLeft: BL.handLeft,
  order: BL.order, sheet: BL.sheet, recipe: BL.recipe, inputs: BL.inputs, folder: BL.folder, folderFace: BL.folderFace, materialsX: BL.mats, materialsY: BL.matY, kitsX: BL.kits, kitsY: BL.kitY, book: BL.book, hurda: BL.hurda, header: BL.header, lockY: BL.lockY, topics: BL.topics, slots: SLOTS });
S('cards.sizesMm', { person: [80, 120], item: [80, 50], material: [80, 56], chip: [56, 40], recipe: [112, 80], thicknessMm: 0.6, magnet: { diaMm: 10, hMm: 3.5 } });
for (const [p, why] of Object.entries((sup.removed || {}).siginak || {})) { if (get(T, p) !== undefined) S(p, { _: 'KALDIRILDI (ek 11-05)', removed: true, why }); }
S('models3d.removed_1_3', { _: 'ek 11-05 ile oyundan çıkan modeller (pakette yok; kaynak dosyada legacy olarak duruyor)', models: removedModels.sort(), materials: removedMats });
// meta + markers
T.meta = { ...T.meta, version: '1.3', date: '2026-11-05', revision3: 'Revizyon 3 (modeller): yeni malzemeler, model kütüphanesi, menü ve ayarlar dolabı yerleşimi, poz başına ışık tarifi, yeni hareket süreleri. Ek 11-05: planlama panosu ve kart ailesi (board.*, cards.*), tarif defteri, kişi dosyası, vardiya kapanışı ve devir fişi (shiftClose.*), kapıda kabul (admission.*), K4/K6/K8A/K8G/K8R kameraları; maket, raf, oda kartları ve ziyaretçiler kaldırıldı (eski değerler _was_1_3’te). YENİ: “_new_1_3”; DEĞİŞEN: “_changed_1_3” (eski değer “_was_1_3”).' };
T._new_1_3 = N; T._changed_1_3 = C; T._was_1_3 = WAS;
RT.meta = { ...RT.meta, version: '1.3', date: '2026-11-05', revision3: 'Revizyon 3: kit rölyef modelleri, sivil ekipman, giriş kapıları, kanal parçaları, yeni 3B malzemeler. YENİ: “_new_1_3” (Claude Code’un kits.* girdileriyle birleşik); DEĞİŞEN: “_changed_1_3”.' };
RT._new_1_3 = [...new Set(RN)]; RT._changed_1_3 = RC; RT._was_1_3 = RWAS;
const order = (o, first) => Object.fromEntries([...first.filter(k => k in o).map(k => [k, o[k]]), ...Object.entries(o).filter(([k]) => !first.includes(k))]);
fs.writeFileSync(out + '/tokens_siginak.json', JSON.stringify(order(T, ['meta', '_new', '_new_1_1', '_changed_1_1', '_was_1_1', '_new_1_3', '_changed_1_3', '_was_1_3']), null, 2));
fs.writeFileSync(out + '/raid_tokens.json', JSON.stringify(order(RT, ['meta', '_new_1_3', '_changed_1_3', '_was_1_3']), null, 2));
console.log('siginak new', N.length, 'changed', C.length, '| raid new', RT._new_1_3.length, 'changed', RC.length);
