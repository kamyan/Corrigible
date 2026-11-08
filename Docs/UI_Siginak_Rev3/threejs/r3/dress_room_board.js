// Planning board dressing (supplement 2026-11-05: R15 board K4, R16 cards, R18 book and production) — owner: main.
// dress(I, ctx): builds the card layout for a state on the PlanningBoard placed by room3.js, fills the board's TMP slots,
// prints the wordless board face, and returns the K4 pose and the board-lamp light.
// URL state: &b=first|late  &bs=<state>  &sel=<person id>  (lang from ctx). States (bs):
//   idle · hover:<id> · drag:<id>:<slot> · swap:<id>:<slot>:<t> · bosta_open · bosta_take:<id> · item:<itemId>:<personId>
//   locked:raid|slip · book · book:<spread> · bookdrag:<recipe> · order · product · file:<id>
import * as THREE from 'three';
import { inst, slotText, worldOf } from './scene3.js';
import { BOARD, P, BL, SLOTS, CARD } from './models/board.js';
import * as BT from './boardtex.js';

const C = await (await fetch(new URL('./community.json', import.meta.url))).json();
async function paperTex(name) { const im = new Image(); im.src = '/r10/papers_new/' + name; try { await im.decode(); } catch (e) { return null; } const t = new THREE.Texture(im); t.colorSpace = THREE.SRGBColorSpace; t.anisotropy = 8; t.needsUpdate = true; return t; }
const INK = '#1E1B16', PLATE_INK = '#1C150E';
const first = (I, n) => (I[n] || []).find(Boolean) || null;

// ---------------------------------------------------------------- data
function person(id) { const p = [...C.people, ...C.candidates].find(q => q.id === id); const pr = C.profiles[p.profile]; return { ...p, can: pr.can, har: pr.har, guc: pr.guc }; }
export function boardState(q, sk) {
  const which = q.get('b') || (sk === 'v031' ? 'late' : 'first');
  const S = JSON.parse(JSON.stringify(which === 'late' ? C.late_op : C.first_shift)); S.which = which;
  S.states = S.states || {}; S.dead = S.dead || []; S.items_on = S.items_on || {}; S.depot_items = S.depot_items || [];
  S.bs = q.get('bs') || 'idle'; S.sel = q.get('sel') || null; return S;
}

// ---------------------------------------------------------------- helpers
function tex(c) { const t = new THREE.CanvasTexture(c); t.colorSpace = THREE.SRGBColorSpace; t.anisotropy = 8; return t; }
function faceMat(c, o = {}) { const m = new THREE.MeshStandardMaterial({ map: tex(c), roughness: o.rough ?? 0.86, metalness: 0, transparent: !!o.transparent, alphaTest: o.transparent ? 0.02 : 0 }); return m; }
function setFace(card, canvas, key = 'cardFace') { card.root.traverse(o => { if (o.isMesh && o.userData.mat === key) o.material = faceMat(canvas); }); }
function put(m, name, lines, o = {}) { const t = m.meta.texts.find(s => s.name === name); if (!t) return null; const ppm = o.ppm || 5000;
  const L = (Array.isArray(lines) ? lines : [lines]).map(l => typeof l === 'string' ? { t: l } : l).map(l => ({ size: (l.cap || o.cap || t.capMm || t.size[1] * 700) * ppm / 1000 / 0.7, c: o.c || INK, f: o.f || 'cp', w: o.w || 700, ls: o.ls ?? 0.03, lh: o.lh, ...l, t: l.t }));
  return slotText(m, name, L, { ppm, align: o.align || t.align, pad: o.pad ?? 0, valign: o.valign }); }
// a decal plane in the board's model space (board mm), facing −z
function decal(board, canvas, xmm, ymm, wmm, hmm, z = BOARD.zf - 0.0004, o = {}) {
  const mat = new THREE.MeshStandardMaterial({ map: tex(canvas), transparent: true, depthWrite: false, roughness: o.rough ?? 0.8, polygonOffset: true, polygonOffsetFactor: -2 });
  const pl = new THREE.Mesh(new THREE.PlaneGeometry(wmm / 1000, hmm / 1000), mat); const p = P(xmm + wmm / 2, ymm + hmm / 2, z);
  pl.position.set(p[0], p[1], -p[2]); pl.renderOrder = 4; pl.receiveShadow = true; board.root.add(pl); return pl; }

// ---------------------------------------------------------------- the wordless board face (tx_board_face)
function boardFace(S, lang) {
  const W = BOARD.IW, H = BOARD.IH, ppm = 3.2; const [c, x] = BT.mk(W, H, ppm);
  x.fillStyle = '#56604F'; x.fillRect(0, 0, W, H);
  // paint mottling
  let s = 11; const r = () => (s = (s * 16807) % 2147483647) / 2147483647; x.globalAlpha = 0.05; for (let i = 0; i < 2600; i++) { x.fillStyle = r() > 0.5 ? '#000' : '#9aa38f'; x.fillRect(r() * W, r() * H, 1 + r() * 5, 0.6 + r() * 2); } x.globalAlpha = 1;
  // depot inset (darker), order zone, folder zone tone
  x.fillStyle = 'rgba(20,24,18,0.22)'; x.fillRect(0, 446, W, 154);
  x.strokeStyle = 'rgba(232,224,200,0.20)'; x.lineWidth = 0.6; x.strokeRect(2, 446, W - 4, 152);
  // pocket prints (wordless): dashed card outline, person glyph, three dashed band strips
  for (const id of SLOTS) { const [cx, cy] = BL.card[id]; const x0 = cx - 40, y0 = cy - 60;
    x.setLineDash([2.2, 1.6]); x.strokeStyle = 'rgba(232,224,200,0.55)'; x.lineWidth = 0.55; x.strokeRect(x0, y0, 80, 120);
    BT.icon(x, 'person', cx - 12, cy - 22, 24, 'rgba(232,224,200,0.28)');
    for (let i = 0; i < 3; i++) { x.strokeStyle = (i === 0 && ['tek', 'emp', 'ana'].includes(id)) ? 'rgba(240,150,120,0.7)' : 'rgba(232,224,200,0.40)'; x.strokeRect(x0, y0 + 121 + i * 14, 80, 13); }
    x.setLineDash([]); }
  // EĞİTİM topic rows (thin rules between rows)
  { const { x: tx, y0, dy } = BL.topics; x.strokeStyle = 'rgba(232,224,200,0.22)'; x.lineWidth = 0.4; for (let i = 0; i <= 5; i++) { x.beginPath(); x.moveTo(tx - 2, y0 - 15 + i * dy); x.lineTo(tx + 70, y0 - 15 + i * dy); x.stroke(); } }
  // BOŞTA fan hint left of the tray
  { const [dx, dy] = BL.deck; x.setLineDash([2.2, 1.6]); for (let i = 1; i <= 4; i++) { x.strokeStyle = `rgba(232,224,200,${0.30 - i * 0.05})`; x.lineWidth = 0.55; x.strokeRect(dx - 40 - i * 78, dy - 60, 80, 120); } x.setLineDash([]);
    x.strokeStyle = 'rgba(232,224,200,0.35)'; x.lineWidth = 1; x.beginPath(); x.moveTo(dx - 52, dy + 74); x.lineTo(dx - 300, dy + 74); x.stroke();
    x.fillStyle = 'rgba(232,224,200,0.35)'; x.beginPath(); x.moveTo(dx - 306, dy + 74); x.lineTo(dx - 296, dy + 69); x.lineTo(dx - 296, dy + 79); x.closePath(); x.fill(); }
  // depot outlines: four material holders, six kit stacks, hook, scrap bin footprint
  x.setLineDash([2, 1.5]); x.strokeStyle = 'rgba(232,224,200,0.32)'; x.lineWidth = 0.5;
  BL.mats.forEach((mx, i) => { x.strokeRect(mx - 40, BL.matY - 28, 80, 56); BT.icon(x, ['metal', 'devre', 'kumas', 'kimyasal'][i], mx - 9, BL.matY - 9, 18, 'rgba(232,224,200,0.18)'); });
  BL.kits.forEach(kx => x.strokeRect(kx - 40, BL.kitY - 25, 80, 50)); x.setLineDash([]);
  // book shadow outline under the hook
  x.strokeStyle = 'rgba(232,224,200,0.18)'; x.strokeRect(BL.book[0] - 52, BL.book[1] + 6, 105, 140);
  return c;
}
// red grease-pencil ring around a training topic
function pencilRing(wmm = 74, hmm = 20) { const [c, x] = BT.mk(wmm, hmm, 8); x.clearRect(0, 0, wmm, hmm); x.strokeStyle = 'rgba(176,36,24,0.92)'; x.lineWidth = 1.1; x.lineCap = 'round';
  x.beginPath(); for (let a = 0.15; a <= Math.PI * 2 + 0.55; a += 0.05) { const rr = 1 + 0.04 * Math.sin(a * 3); const X = wmm / 2 + Math.cos(a) * (wmm / 2 - 2) * rr, Y = hmm / 2 + Math.sin(a) * (hmm / 2 - 2.2) * rr - (a > Math.PI * 2 ? (a - Math.PI * 2) * 2.2 : 0); a === 0.15 ? x.moveTo(X, Y) : x.lineTo(X, Y); } x.stroke(); return c; }
// amber glow frame (drop target highlight) as a decal
function glowFrame(wmm, hmm, col = '255,178,72') { const pad = 6; const [c, x] = BT.mk(wmm + pad * 2, hmm + pad * 2, 4); x.clearRect(0, 0, wmm + pad * 2, hmm + pad * 2);
  for (let i = 0; i < 6; i++) { x.strokeStyle = `rgba(${col},${0.10 + i * 0.05})`; x.lineWidth = 6 - i; x.strokeRect(pad - 3 + i * 0.6, pad - 3 + i * 0.6, wmm + 6 - i * 1.2, hmm + 6 - i * 1.2); }
  x.strokeStyle = `rgba(${col},0.95)`; x.lineWidth = 1.1; x.strokeRect(pad, pad, wmm, hmm); return c; }

// ---------------------------------------------------------------- main
export async function dress(I, ctx = {}) {
  const board = first(I, 'PlanningBoard'); if (!board) return { info: { board: false } };
  const lang = ctx.lang || 'tr', en = lang === 'en', q = ctx.q || new URLSearchParams(), S = boardState(q, ctx.sk); const T = BT.L[lang];
  const zf = BOARD.zf; const out = { cards: {}, anchors: {} };
  // ---- comparison 4 (option B): the same board hung flat on the left wall, no stand (&wall=1)
  if (q.get('wall') === '1') { const S3 = await import('./scene3.js');
    board.holder.position.copy(S3.U(-2.168, 0.0, 1.50)); board.holder.rotation.set(0, Math.PI / 2, 0);   // front faces +x (into the room)
    board.root.updateMatrixWorld(true); const inv = new THREE.Matrix4().copy(board.root.matrixWorld).invert(); const bb = new THREE.Box3(), c = new THREE.Vector3(), s = new THREE.Vector3();
    board.root.traverse(o => { if (!o.isMesh) return; bb.setFromObject(o).applyMatrix4(inv); bb.getCenter(c); bb.getSize(s);
      if (bb.max.y < BOARD.y0 - 0.005 || s.y > 1.0 || (Math.abs(c.x) > 0.607 && c.y < BOARD.y0 + BOARD.PH)) o.visible = false; });
    out.wall = true; }
  // ---- face, slots
  board.root.traverse(o => { if (o.isMesh && o.userData.mat === 'boardFace') o.material = faceMat(boardFace(S, lang), { rough: 0.74 }); });
  const NAMES = { tek: ['TEKNİSYEN', 'TECHNICIAN'], emp: ['EMP UZMANI', 'EMP SPECIALIST'], ana: ['ANALİST', 'ANALYST'], uret: ['ÜRETİM EKİBİ', 'PRODUCTION'], mutf: ['MUTFAK', 'KITCHEN'], egit: ['EĞİTİM', 'TRAINING'] };
  for (const id of SLOTS) { put(board, 'TXT_P_' + id, NAMES[id][en ? 1 : 0], { c: PLATE_INK, cap: 8.4 });
    const field = ['tek', 'emp', 'ana'].includes(id); const lines = [field ? `${T.slot.kit} · ${en ? 'NEEDED' : 'GEREKLİ'}` : T.slot.kit, T.slot.armor, T.slot.aux];
    put(board, 'TXT_S_' + id, lines.map((t, i) => ({ t, c: i === 0 && field ? '#F0A08A' : 'rgba(232,224,200,0.62)', lh: 2.0 })), { cap: 5.6 }); }
  put(board, 'TXT_FieldBand', en ? 'FIELD · RAID TEAM' : 'SAHA · BASKIN KADROSU', { c: '#E8D9B0', cap: 4.6, ls: 0.12 });
  put(board, 'TXT_P_bosta', en ? 'IDLE / REST' : 'BOŞTA / DİNLENME', { c: PLATE_INK, cap: 8.4 });
  put(board, 'TXT_P_order', en ? 'PRODUCTION · WORK ORDER' : 'ÜRETİM · İŞ EMRİ', { c: PLATE_INK, cap: 8.4 });
  put(board, 'TXT_P_dosya', en ? 'PERSONAL FILE' : 'KİŞİ DOSYASI', { c: PLATE_INK, cap: 8.4 });
  put(board, 'TXT_P_ham', en ? 'STORE · RAW MATERIALS' : 'DEPO · HAMMADDE', { c: PLATE_INK, cap: 7.6 });
  put(board, 'TXT_P_kit', en ? 'STORE · AVAILABLE KITS' : 'DEPO · KULLANILABİLİR KİTLER', { c: PLATE_INK, cap: 7.6 });
  put(board, 'TXT_P_hurda', [{ t: en ? 'SCRAP' : 'HURDA', cap: 13, f: 'st', w: 800, c: '#2A2B27' }, { t: en ? '1 item → 1 metal' : '1 eşya → 1 metal', cap: 5.2, c: '#2A2B27' }]);
  put(board, 'TXT_Back', en ? '◄ BACK TO DESK' : '◄ MASAYA DÖN', { c: INK, cap: 8.2 });
  put(board, 'TXT_Title', en ? 'SHIFT PLAN · SHELTER 07' : 'VARDİYA PLANI · SIĞINAK 07', { c: '#D9D2BF', f: 'st', w: 800, cap: 14, ls: 0.06, align: 'left' });
  const sh = String(S.shift).padStart(3, '0'), dl = (S.ration_delta > 0 ? '+' : '−') + Math.abs(S.ration_delta).toFixed(1).replace('.', en ? '.' : ',');
  put(board, 'TXT_Header', en ? `S.${sh} · rations ${S.rations} (${dl}) · ${S.alive}/${S.cap} people` : `V.${sh} · erzak ${S.rations} (${dl}) · ${S.alive}/${S.cap} kişi`, { c: '#EDEBE2', f: 'ka', cap: 10.5, align: 'left', pad: 2 });
  const locked = S.bs.startsWith('locked');
  put(board, 'TXT_FlapA', en ? 'PLAN OPEN' : 'PLAN AÇIK', { c: INK, cap: 8.6 }); put(board, 'TXT_FlapB', en ? 'PLAN LOCKED' : 'PLAN KİLİTLİ', { c: '#F2E9CC', cap: 8.6 });
  board.pose('Flap', locked ? 180 : 0); const lr = board.parts.LockRod; if (lr) lr.group.visible = locked;
  if (locked) put(board, 'TXT_LockTag', [{ t: en ? 'PLAN LOCKED' : 'PLAN KİLİTLİ' }, { t: S.bs.endsWith('slip') ? (en ? 'handover printed' : 'devir fişi basıldı') : (en ? 'raid ordered' : 'baskın emri verildi'), cap: 4.2 }], { f: 'ka', c: '#1A2F5A', cap: 5 });
  put(board, 'TXT_Topics', C.areas.skills.map(s => ({ t: (en ? s.en : s.tr).toUpperCase(), lh: 1.9 })), { c: '#E8E0C8', cap: 6.6, align: 'left' });
  // pencil ring around the chosen training topic
  { const ti = C.areas.skills.findIndex(s => s.id === (S.training_topic || 'T')); const { x: tx, y0, dy } = BL.topics; decal(board, pencilRing(74, 19), tx - 4, y0 - 13.5 + ti * dy, 74, 19); }
  // ---- order sheet print
  board.root.traverse(o => { if (o.isMesh && o.userData.mat === 'paper') {
    const wo = S.work_order; const status = wo ? (en ? `armour plate · inputs held · maker: ${person(S.assign.uret).name}` : `zırh plakası · girdiler ayrıldı · üretici: ${person(S.assign.uret).name}`) : (S.bs === 'order' || S.bs === 'product' ? '' : '');
    o.material = faceMat(BT.orderSheet({ lang, no: S.shift, status }), { rough: 0.9 }); } });

  // ---- cards
  const cardZ = (k) => zf - 0.0003 - k * 0.0007;          // k = stacking level (0 = on the panel)
  const place = (name, xmm, ymm, k = 0, o = {}) => { const p = P(xmm, ymm, cardZ(k) + (o.lift || 0) * -1); const m = inst(board.root, name, p, 0, o.rot ? { euler: [o.tilt || 0, 0, o.rot] } : {}); return m; };
  const items = C.items, mats = C.materials;
  async function personAt(id, xmm, ymm, k, st, o = {}) {
    const p = person(id); const card = place('CardPerson', xmm, ymm, k + 3, o);
    setFace(card, await BT.personCard(p, st, { lang, ppm: 8 }));
    card.parts.Flag.group.visible = !!st.review; card.parts.Tape.group.visible = false;
    if (st.review) card.root.traverse(ob => { if (ob.isMesh && ob.name.startsWith('Flag:coverRed')) ob.material = faceMat(BT.reviewFlag({ lang })); });
    // items tucked behind: kit, armour, aid (fixed 14 mm offsets)
    const its = (S.items_on[id] || []).slice().sort((a, b) => ['kit', 'armor', 'aux'].indexOf(items[a].slot) - ['kit', 'armor', 'aux'].indexOf(items[b].slot));
    its.forEach((iid, n) => { const ic = place('CardItem', xmm, ymm + 60 + 14 * (n + 1) - 25, k + 2 - n * 0.9, o); setFace(ic, BT.itemCard(iid, items, { lang })); if (ic.parts.Magnet) ic.parts.Magnet.group.visible = false; });
    out.cards[id] = [xmm, ymm]; return card; }
  const stOf = (id, job) => { const s = S.states[id] || {}; return { job, topic: S.training_topic, hurt: s.hurt, bed: s.bed, restricted: s.restricted, review: s.review }; };

  // which assignment is shown (drag / swap states move one card)
  const A = { ...S.assign }; const bs = S.bs.split(':');
  if (q.get('idle')) { const ids = q.get('idle').split(','); for (const k of SLOTS) if (ids.includes(A[k])) A[k] = null; A.bosta = [...new Set([...(A.bosta || []), ...ids])]; }
  const holding = (bs[0] === 'drag' || bs[0] === 'hover' || bs[0] === 'swap') ? bs[1] : null;
  for (const slot of SLOTS) { const id = A[slot]; if (!id || id === holding) continue; const [x, y] = BL.card[slot]; await personAt(id, x, y, 0, stOf(id, slot)); }
  // BOŞTA deck / hand
  const idle = (A.bosta || []).filter(id => id !== holding);
  const [dx, dy] = BL.deck;
  if (bs[0] === 'bosta_open' || bs[0] === 'bosta_take') { const n = idle.length; const step = Math.min(82, (dx - BL.handLeft) / Math.max(1, n - 1));
    for (let i = 0; i < n; i++) { const id = idle[i]; let x = dx - (n - 1 - i) * step; let y = dy; let k = i;
      if (bs[0] === 'bosta_take') { if (id === bs[1]) { await personAt(id, x - 10, y - 26, 30, stOf(id, 'bosta'), { lift: 0.010, rot: -3 }); out.held = [x - 10, y - 26]; continue; } x = dx - (n - 1 - i) * step * 0.45; }   // the others slide halfway back to the deck
      await personAt(id, x, y, k * 2, stOf(id, 'bosta')); } }
  if (!(bs[0] === 'bosta_open' || bs[0] === 'bosta_take')) { for (let i = idle.length - 1; i >= 0; i--) { const id = idle[i]; const d = idle.length - 1 - i; await personAt(id, dx + d * 1.0, dy + d * 1.0, (idle.length - 1 - d) * 1.6, stOf(id, 'bosta')); } }
  out.deck = idle.length;
  // KAYIPLAR: dead people are not on the board (archive) — shown only in R16
  if (S.bs === 'order' && S.which === 'first') { S.materials.metal = [3, 2]; S.materials.kumas = [2, 1]; }
  if (S.bs === 'product' && S.which === 'first') { S.materials.metal = [1, 0]; S.materials.kumas = [1, 0]; S.depot_items = [...S.depot_items, 'zirh']; S.shift = 2; out.product = 'zirh'; }
  // ---- depot: raw material stock cards
  ['metal', 'devre', 'kumas', 'kimyasal'].forEach((k, i) => { const [st, rs] = S.materials[k]; const c = place('CardMaterial', BL.mats[i], BL.matY, 0); setFace(c, BT.materialCard(k, st, rs, mats, { lang })); });
  // kit stacks by type, count tags
  const order = ['kit_tek', 'kit_emp', 'kit_ana', 'zirh', 'bakim', 'emp_hucre']; const counts = {}; for (const it of S.depot_items) counts[it] = (counts[it] || 0) + 1;
  order.forEach((it, i) => { const n = counts[it] || 0; const tagName = 'TXT_Count_' + i;
    if (n) { for (let j = Math.min(n, 3) - 1; j >= 0; j--) { const c = place('CardItem', BL.kits[i] + j * 0.9, BL.kitY + j * 0.9, 2 - j); setFace(c, BT.itemCard(it, items, { lang })); }
      put(board, tagName, '×' + n, { c: PLATE_INK, cap: 6.6 });
      if (out.product === it) { const [c2, x2] = BT.mk(30, 12, 8); x2.fillStyle = '#B4291C'; x2.fillRect(0, 0, 30, 12); x2.fillStyle = '#F7EFD8'; x2.font = '700 7px "Courier Prime"'; x2.textAlign = 'center'; x2.fillText(en ? 'NEW' : 'YENİ', 15, 8.4); decal(board, c2, BL.kits[i] + 8, BL.kitY - 34, 30, 12, zf - 0.0042); } } else put(board, tagName, '×0', { c: 'rgba(28,21,14,0.35)', cap: 6.6 }); });
  // ---- work order contents (late op or order/product states)
  const wo = S.work_order || (S.bs === 'order' ? { recipe: 'zirh', inputs: { metal: 2, kumas: 1 } } : null);
  if (wo && S.bs !== 'product') { const r = C.recipes.find(x => x.id === wo.recipe); const rc = place('CardRecipe', BL.recipe[0], BL.recipe[1], 0); setFace(rc, BT.recipeCard(r, mats, { lang, progress: 0 }));
    const spots = [[612, 314, -4], [668, 330, 3], [742, 318, -2], [800, 334, 5]]; let si = 0;
    for (const [k, n] of Object.entries(wo.inputs)) for (let j = 0; j < n; j++) { const [cx, cy, rot] = spots[si++ % spots.length]; const ch = place('CardChip', cx, cy, si % 2, { rot }); setFace(ch, BT.materialChip(k, mats, { lang })); } }
  // ---- folder face (selected person), book cover
  const selId = S.sel || (S.bs.startsWith('file:') ? S.bs.split(':')[1] : null);
  { const [fx, fy, fw, fh] = BL.folderFace; const fo = inst(board.root, 'PersonFolder', P(fx, fy + fh, zf - 0.0045), 0); const sp = selId ? person(selId) : null;
    put(fo, 'TXT_Face', [{ t: en ? 'PERSONAL FILE' : 'KİŞİ DOSYASI', cap: 8 }, { t: sp ? sp.name.toLocaleUpperCase(en ? 'en' : 'tr') : (en ? '— select a card —' : '— kart seç —'), cap: sp ? 11 : 5.6, f: sp ? 'lf' : 'cp', c: sp ? '#2A1E10' : 'rgba(42,30,16,0.6)' }], { c: '#2A1E10' });
    if (sp) put(fo, 'TXT_Tab', sp.name.toLocaleUpperCase(en ? 'en' : 'tr'), { c: '#2A1E10', cap: 5.6 }); out.folder = !!sp; }
  const bsk = S.bs.split(':')[0]; const bookOpen = bsk === 'book' || bsk === 'bookdrag';
  { const spread = bsk === 'bookdrag' ? (C.recipes.find(r => r.id === S.bs.split(':')[1]) || { spread: 1 }).spread : +(S.bs.split(':')[1] || 1);
    const bk = bookOpen ? inst(board.root, 'RecipeBook', bsk === 'bookdrag' ? [-0.02, BOARD.IY0 - 0.300 - 0.066 - 0.165, -0.62] : [0.0, BOARD.IY0 - 0.300 - 0.066, -0.665], 0, { euler: [bsk === 'bookdrag' ? -14 : -9, 0, 0] }) : inst(board.root, 'RecipeBook', P(18, 600, zf - 0.019), 0, { euler: [-4, 0, 0] });
    bk.root.traverse(o => { if (o.isMesh && o.userData.mat === 'bookCloth' && o.geometry.attributes.position.count <= 6) o.material = faceMat(BT.bookCover({ lang })); });
    if (bookOpen) { bk.pose('Cover', -178); const tx = await paperTex(`tarif_acilim_${spread}_${lang}@2x.png`);
      if (tx) { const half = (off) => { const t = tx.clone(); t.needsUpdate = true; t.repeat.set(0.5, 1); t.offset.set(off, 0); return new THREE.MeshStandardMaterial({ map: t, roughness: 0.9 }); };
        let n = 0; bk.root.traverse(o => { if (o.isMesh && o.userData.mat === 'pageFace') { const onCover = o.parent && o.parent.name === 'Cover'; o.material = half(onCover ? 0 : 0.5); n++; } }); }
      if (bk.parts.Leaf) bk.parts.Leaf.group.visible = false; out.book = spread; }
    ctx.R && (ctx.R.book = bk); }
  // dragged recipe card (from the open page toward the work order)
  if (bsk === 'bookdrag') { const r = C.recipes.find(x => x.id === S.bs.split(':')[1]) || C.recipes[0]; const rc = place('CardRecipe', 700, 322, 14, { lift: 0.12, rot: -6 }); setFace(rc, BT.recipeCard(r, mats, { lang }));
    const o = BL.order; decal(board, glowFrame(o[2] - 4, o[3] - 4), o[0] - 4, o[1] - 4, o[2] + 4, o[3] + 4, zf - 0.0026); out.held = [700, 322]; }
  // person file forward + open (file:<id>)
  if (bsk === 'file') { const id = S.bs.split(':')[1]; const fo2 = inst(board.root, 'PersonFolder', [0.015, BOARD.IY0 - 0.300 - 0.09, -0.68], 0, { euler: [-8, 0, 0] });
    fo2.pose('Cover', -176); const tx = await paperTex(`kisi_dosyasi_${id}_${lang}@2x.png`);
    if (tx) { const half = (off) => { const t = tx.clone(); t.needsUpdate = true; t.repeat.set(0.5, 1); t.offset.set(off, 0); return new THREE.MeshStandardMaterial({ map: t, roughness: 0.9, transparent: true }); };
      fo2.root.traverse(o => { if (o.isMesh && o.userData.mat === 'paper') o.material = half(0.5); });
      const pl = new THREE.Mesh(new THREE.PlaneGeometry(0.122, 0.17), half(0)); pl.position.set(0.065, 0.09, -0.0001); pl.rotation.y = Math.PI; fo2.parts.Cover.group.add(pl); }
    fo2.root.traverse(o => { if (o.isMesh && o.userData.mat === 'manila') o.castShadow = true; }); out.file = id; }
  // ---- hover / drag / swap: the held card off its slot, target highlight
  if (holding) { const from = Object.keys(A).find(k => A[k] === holding) || 'bosta'; const target = bs[2] || from;
    const [fx, fy] = from === 'bosta' ? BL.deck : BL.card[from]; const [tx, ty] = target === 'bosta' ? [dx - 200, dy] : BL.card[target] || [fx, fy];
    if (bs[0] === 'hover') { const c = await personAt(holding, fx, fy, 0, stOf(holding, from), { lift: 0.004 }); out.held = [fx, fy]; }
    else if (bs[0] === 'drag') { const hx = tx + 16, hy = ty - 10; const c = await personAt(holding, hx, hy, 8, stOf(holding, from), { lift: 0.010, rot: 3 }); out.held = [hx, hy];
      const tgt = target === 'bosta' ? BL.bosta : BL.pockets[target]; decal(board, glowFrame(tgt[2] - 4, tgt[3] - 4), tgt[0] - 4, tgt[1] - 4, tgt[2] + 4, tgt[3] + 4, zf - 0.0026); out.target = tgt;
      // swap preview: the occupant moves halfway toward the held card's origin
      const occ = target !== 'bosta' && A[target]; if (occ && occ !== holding) out.swapWith = occ; }
    else if (bs[0] === 'swap') { const t = +(bs[3] || 0.5); const occ = A[target];
      const hx = fx + (tx - fx) * t, hy = fy + (ty - fy) * t; await personAt(holding, hx, hy, 8, stOf(holding, target), { lift: 0.006 });
      if (occ && occ !== holding) { const ox = tx + (fx - tx) * t, oy = ty + (fy - ty) * t; await personAt(occ, ox, oy, 6, stOf(occ, from), { lift: 0.006 }); } } }
  // item dragged onto a person
  if (bs[0] === 'item') { const [it, pid] = [bs[1], bs[2]]; const slot = Object.keys(A).find(k => A[k] === pid); if (slot && BL.card[slot]) { const [cx, cy] = BL.card[slot]; const c = place('CardItem', cx + 18, cy + 14, 12, { lift: 0.010, rot: -4 }); setFace(c, BT.itemCard(it, items, { lang }));
    const p = BL.pockets[slot]; decal(board, glowFrame(p[2] - 4, p[3] - 4), p[0] - 4, p[1] - 4, p[2] + 4, p[3] + 4, zf - 0.0026); out.target = p; } }
  // project helpers for overlay labels (board mm → world)
  out.mm2world = true; out.boardMatrix = board.root.matrixWorld ? null : null;
  if (ctx.R) { ctx.R.board = { board, P, S }; const W = (x, y, z) => worldOf(board, P(x, y, z ?? BOARD.zf));
    const pts = ctx.R.projPts || (ctx.R.projPts = []);
    for (const id of SLOTS) { const [x, y] = BL.card[id]; pts.push(['slot_' + id, W(x, y)]); const r = BL.pockets[id]; pts.push(['pocket_' + id + '_tl', W(r[0], r[1])], ['pocket_' + id + '_br', W(r[0] + r[2], r[1] + r[3])]); }
    pts.push(['deck', W(...BL.deck)], ['bosta_tl', W(BL.bosta[0], BL.bosta[1])], ['bosta_br', W(BL.bosta[0] + BL.bosta[2], BL.bosta[1] + BL.bosta[3])], ['order_tl', W(BL.order[0], BL.order[1])], ['order_br', W(BL.order[0] + BL.order[2], BL.order[1] + BL.order[3])],
      ['folder', W(BL.folderFace[0] + 65, BL.folderFace[1] + 90)], ['book', W(70, 526)], ['hurda', W(1077, 520)], ['header', W(776, 20)], ['flap', W(1078, 20)], ['back', W(68, 20)], ['topics', W(BL.topics.x, BL.topics.y0)],
      ['mat_metal', W(BL.mats[0], BL.matY)], ['kits', W(BL.kits[0], BL.kitY)], ['recipe', W(...BL.recipe)]);
    for (const [k, v] of Object.entries(out.cards)) pts.push(['card_' + k, W(v[0], v[1])]); if (out.held) pts.push(['held', W(out.held[0], out.held[1], BOARD.zf - 0.01)]); }

  // ---- pose and light
  const poses = {
    K4: (h) => { const c = h.anchorWorld(board, 'Cam_K4'), t = h.anchorWorld(board, 'Look_K4'); return h.lookCam(c, t, h.num('fov', 42.5)); },
    K4C: (h) => { const a = (h.q.get('at') || '600,300').split(',').map(Number); const z = +(h.q.get('dist') || 0.45);
      const t = h.worldOf(board, P(a[0], a[1])); const c = h.worldOf(board, P(a[0], a[1], BOARD.zf - z)); return h.lookCam(c, t, h.num('fov', 30)); },
  };
  const lights = [(h) => { if (!['K1', 'K4', 'K4C'].includes(h.pose)) return; const p = h.lightWorld(board, 'BoardLamp'); if (!p) return;
    const s = h.sh(new h.THREE.SpotLight(0xffe2b8, h.num('blamp', h.pose === 'K1' ? 2.4 : 2.6), 3.2, 58 * h.D2R, 0.9, 2), 2048, -0.0006); s.position.copy(p); s.target.position.copy(h.worldOf(board, P(580, 330))); h.addL(s, 'pano lambası (spot, gölgeli)'); }];
  return { poses, lights, info: out };
}
// pre-hook: nothing yet (board does not change desk state)
export function pre(ctx) {}
