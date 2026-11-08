// Rev3 admission station dressing (supplement 2026-11-05, R17) — owner: door agent.
// dress(I, ctx): I = placed instances (room3.js puts AdmissionCounter, AdmissionPanel, ColdGlass, CounterLamp at meta.place);
// loose tools (stamp rack + 4 stamps, magnifier, request pad, ration tins, MASA key) are built here in the counter's frames.
// State from ctx.q: &adm=idle|present|inspect|pending|stamped|aid|v3|trace|cold  &cand=aylin|rauf  &admLang=tr|en
// (default: idle, or present when the knock lamp is on). Papers = R10 agent PNGs (/r10/papers_new/<name>_<lang>@2x.png),
// placeholders drawn here only where no paper exists. Poses: K8A (counter), K8G (breath glass), K8R (reading, top-down),
// K8P (= K8 door pose). Every pose writes projected callout points to info.proj (→ __done.extra in the render JSON).
import * as THREE from 'three';
import { inst, printPlane as printPlaneRaw, slotText, worldOf, anchorWorld, U, D2R, canvas } from './scene3.js';
import { material } from './mats.js';
import { admWorld } from './models/admission.js';
const printPlane = (m, c, sz, t, o = {}) => { const pl = printPlaneRaw(m, c, sz, t, o); if (pl && o.paper !== false) pl.material.color.setRGB(0.80, 0.78, 0.74); return pl; };

const PAP = '/r10/papers_new/';
const INK = '#2A1E10';
const first = (I, n) => (I[n] || []).find(Boolean);
function swapPart(m, part, from, to) { if (!m || !m.parts[part]) return; m.parts[part].group.traverse(o => { if (o.isMesh && o.userData.mat === from) { o.material = material(to); o.userData.mat = to; } }); }
function showPart(m, part, on) { if (m && m.parts[part]) m.parts[part].group.visible = !!on; }
async function tex(url) { try { const img = new Image(); img.src = url; await img.decode(); const t = new THREE.Texture(img); t.colorSpace = THREE.SRGBColorSpace; t.anisotropy = 8; t.needsUpdate = true; return t; } catch (e) { return null; } }
const plateLines = (t, cap = 9, ppm = 4000, f = 'cp', c = INK, ls = 0.06) => (Array.isArray(t) ? t : [t]).map(s => ({ t: s, size: cap * ppm / 1000 / 0.7, w: f === 'st' ? 800 : 700, f, c, ls }));

// ---------------------------------------------------------------- placeholder papers (R10 style) where no PNG exists
function paperBase(W, H, bg = '#F2E9CC') { const [c, x] = canvas(W, H); x.fillStyle = bg; x.fillRect(0, 0, W, H); x.strokeStyle = 'rgba(103,95,80,0.55)'; x.lineWidth = 3; x.strokeRect(8, 8, W - 16, H - 16); return [c, x]; }
function slipCanvas(head, ver, title, body, note, stamp) {
  const W = 720, H = 480; const [c, x] = paperBase(W, H);
  x.fillStyle = '#675F50'; x.font = '700 22px "Courier Prime"'; x.letterSpacing = '2px'; x.fillText(head, 36, 54); x.textAlign = 'right'; x.fillText(ver, W - 36, 54); x.textAlign = 'left';
  x.fillStyle = '#1E1B16'; x.font = '700 30px "Courier Prime"'; x.fillText(title, 36, 104); x.fillStyle = '#D2483F'; x.fillRect(36, 122, W - 72, 4);
  x.letterSpacing = '0px'; x.fillStyle = '#1E1B16'; x.font = '400 30px "Libre Franklin"'; body.forEach((l, i) => x.fillText(l, 36, 178 + i * 40));
  x.fillStyle = '#463F34'; x.font = '700 22px "Courier Prime"'; (note || []).forEach((l, i) => x.fillText(l, 36, 394 + i * 30));
  if (stamp) { x.save(); x.translate(W - 150, 420); x.rotate(-3 * D2R); x.strokeStyle = '#675F50'; x.lineWidth = 3; x.font = '700 24px "Courier Prime"'; const w = x.measureText(stamp).width + 22; x.strokeRect(-w / 2, -22, w, 40); x.fillStyle = '#675F50'; x.textAlign = 'center'; x.fillText(stamp, 0, 7); x.restore(); }
  return c;
}
function stripHead(ver, en) { // old strip head for the ESKİ pocket (only the top shows above the lip)
  const W = 640, H = 1680; const [c, x] = paperBase(W, H, '#EFE6CF');
  x.fillStyle = '#675F50'; x.font = '700 30px "Courier Prime"'; x.fillText(en ? 'KYGA · ADMISSION ORDERS' : 'KYGA · KABUL TALİMATI', 40, 70); x.textAlign = 'right'; x.fillText('No. ' + ver, W - 40, 70); x.textAlign = 'left';
  x.fillStyle = '#1E1B16'; x.font = '700 64px "Courier Prime"'; x.fillText((en ? 'VERSION ' : 'SÜRÜM ') + ver, 40, 160);
  x.fillStyle = '#463F34'; x.font = '700 30px "Courier Prime"'; x.fillText(ver === 1 ? (en ? 'from S.001' : 'V.001’den geçerli') : (en ? 'from S.004' : 'V.004’ten geçerli'), 40, 214);
  x.fillStyle = '#D2483F'; x.fillRect(40, 236, W - 80, 6);
  x.fillStyle = '#8E8573'; x.font = '400 34px "Libre Franklin"'; for (let i = 0; i < 9; i++) x.fillRect(40, 300 + i * 56, W - 80 - (i % 3) * 90, 14);
  return c;
}
function requestForm(cand, en, carbon) {
  const W = 840, H = 1120; const [c, x] = paperBase(W, H, carbon ? '#E9E4D6' : '#F4EFE2'); const ink = carbon ? '#2E3C6E' : '#1E1B16';
  x.fillStyle = carbon ? '#4A5680' : '#675F50'; x.font = '700 30px "Courier Prime"'; x.fillText(en ? 'KYGA · OUTSIDE RECORD REQUEST' : 'KYGA · DIŞ KAYIT TALEBİ', 44, 74);
  x.textAlign = 'right'; x.fillText('V.005', W - 44, 74); x.textAlign = 'left';
  x.fillStyle = '#D2483F'; x.fillRect(44, 96, W - 88, 5);
  const rows = en ? [['NAME', 'Aylin'], ['DOC No.', 'T-04/009'], ['TRANSFER No.', 'T-04/009'], ['SEAL', 'KYGA-K'], ['ASK', 'Northern evac. line'], ['SUPERVISOR', 'İpek'], ['', '(top card of IDLE)']]
    : [['AD', 'Aylin'], ['BELGE No.', 'T-04/009'], ['SEVK No.', 'T-04/009'], ['MÜHÜR', 'KYGA-K'], ['SORULACAK', 'Kuzey tahliye hattı'], ['GÖZETMEN', 'İpek'], ['', '(BOŞTA üst kartı)']];
  rows.forEach(([k, v], i) => { const y = 180 + i * 92; x.fillStyle = carbon ? '#4A5680' : '#675F50'; x.font = '700 30px "Courier Prime"'; x.fillText(k, 44, y);
    x.fillStyle = ink; x.font = carbon ? '700 46px "Kalam"' : '700 46px "Kalam"'; x.fillText(v, 330, y + 6); x.strokeStyle = 'rgba(103,95,80,0.4)'; x.lineWidth = 2; x.beginPath(); x.moveTo(320, y + 20); x.lineTo(W - 44, y + 20); x.stroke(); });
  x.fillStyle = carbon ? '#4A5680' : '#463F34'; x.font = '700 28px "Courier Prime"'; x.fillText(en ? 'result at the next close' : 'sonuç sonraki kapanışta', 44, H - 120);
  if (carbon) { x.font = '700 28px "Courier Prime"'; x.fillText(en ? 'CARBON COPY' : 'KOPYA', W - 230, H - 60); }
  return c;
}
function platePrint(text, w = 480, h = 120, cap = 54) { const [c, x] = canvas(w, h); x.fillStyle = '#B8955A'; x.fillRect(0, 0, w, h); x.strokeStyle = '#5C4826'; x.lineWidth = 6; x.strokeRect(3, 3, w - 6, h - 6);
  x.fillStyle = INK; x.font = `700 ${cap}px "Courier Prime"`; x.textAlign = 'center'; x.textBaseline = 'middle'; x.letterSpacing = '4px'; x.fillText(text, w / 2, h / 2 + 2); return c; }
function deskIcon() { const [c, x] = canvas(256, 256); x.fillStyle = '#E9DDBE'; x.fillRect(0, 0, 256, 256); x.strokeStyle = '#1C150E'; x.lineWidth = 14; x.lineCap = 'round';
  x.beginPath(); x.moveTo(40, 196); x.lineTo(216, 196); x.moveTo(70, 196); x.lineTo(70, 226); x.moveTo(186, 196); x.lineTo(186, 226); x.moveTo(150, 196); x.lineTo(150, 110); x.lineTo(112, 80); x.stroke();
  x.beginPath(); x.moveTo(80, 96); x.lineTo(128, 58); x.lineTo(150, 86); x.closePath(); x.fillStyle = '#1C150E'; x.fill(); return c; }

// ---------------------------------------------------------------- main
export async function dress(I, ctx = {}) {
  const q = ctx.q || new URLSearchParams(''); const R = ctx.R; const door = ctx.door || {};
  const lang = q.get('admLang') || ctx.lang || 'tr', en = lang === 'en';
  const st = q.get('adm') || (door.knock ? 'present' : 'idle');
  const cand = q.get('cand') || ({ inspect: 'rauf', aid: 'rauf', trace: 'rauf' }[st] || 'aylin');
  const info = { state: st, cand, lang, proj: {} }; const PTS = {};
  const counter = first(I, 'AdmissionCounter'), panel = first(I, 'AdmissionPanel'), cg = first(I, 'ColdGlass'), lamp = first(I, 'CounterLamp'), leaf = first(I, 'DoorLeaf'), ic = first(I, 'IntercomBox');
  if (!counter || !panel) return { info };
  const P = (name, ...a) => tex(`${PAP}${name}_${lang}@2x.png`);
  const active = st !== 'idle';

  // ---- breath glass rides on the door leaf (attach at Hinge 0, then restore the leaf's pose)
  if (leaf && cg && cg.holder) { const ang = door.open ? 95 : 0; leaf.pose('Hinge', 0); R.root.updateMatrixWorld(true); leaf.parts.Hinge.group.attach(cg.holder); leaf.pose('Hinge', ang); R.root.updateMatrixWorld(true); }
  if (cg) { const open = st === 'cold' || q.get('glass') === 'open'; cg.pose('Cover', open ? 105 : 0); const fogOn = st === 'cold' && q.get('fog') !== '0'; showPart(cg, 'Fog', fogOn);
    if (fogOn) { cg.parts.Fog.group.traverse(o => { if (o.isMesh && o.userData.mat === 'fogFrost') o.visible = false; });
      const [fc, fx] = canvas(512, 512); const g = fx.createRadialGradient(256, 290, 20, 256, 256, 256); g.addColorStop(0, 'rgba(238,242,240,0.94)'); g.addColorStop(0.5, 'rgba(232,238,236,0.80)'); g.addColorStop(0.82, 'rgba(222,230,228,0.30)'); g.addColorStop(1, 'rgba(222,230,228,0)');
      fx.fillStyle = g; fx.fillRect(0, 0, 512, 512); let seed = 7; const rnd = () => (seed = (seed * 16807) % 2147483647) / 2147483647;
      for (let i = 0; i < 420; i++) { const a = rnd() * Math.PI * 2, r = Math.sqrt(rnd()) * 230, x = 256 + Math.cos(a) * r, y = 256 + Math.sin(a) * r; fx.fillStyle = `rgba(255,255,255,${0.15 + rnd() * 0.45})`; fx.beginPath(); fx.arc(x, y, 1.5 + rnd() * 4.5, 0, Math.PI * 2); fx.fill(); }
      const ft = new THREE.CanvasTexture(fc); ft.colorSpace = THREE.SRGBColorSpace; printPlaneRaw(cg, [0, 0, -0.0131], [0.108, 0.108], ft, { part: 'Fog', circle: true, transparent: true, rough: 0.95 }); }
    slotText(cg, 'TXT_Plate', plateLines(en ? 'BREATH GLASS' : 'BUĞU CAMI', 9)); }

  // ---- plates (TMP stand-ins)
  slotText(panel, 'TXT_Drawer', plateLines(en ? 'PAPERS' : 'EVRAK', 10));
  slotText(panel, 'TXT_Out', plateLines(en ? 'OUT · RADIO' : 'GİDEN · TELSİZ', 9));
  slotText(panel, 'TXT_Holder', plateLines(en ? 'ORDERS' : 'TALİMAT', 7));
  slotText(panel, 'TXT_Archive', plateLines(en ? 'OLD' : 'ESKİ', 8));
  slotText(panel, 'TXT_Slip', plateLines(en ? 'FINDING' : 'BULGU', 8));
  slotText(counter, 'TXT_Findings', plateLines(en ? 'FINDINGS' : 'BULGULAR', 7, 4000, 'cp', '#D9D2BF'));
  slotText(counter, 'TXT_Under', plateLines(en ? 'ADMISSION' : 'KABUL', 80, 1200, 'st', '#D9D2BF', 0.12));

  // ---- fold state, lamp
  if (!active) { counter.pose('Top', 88); counter.pose('BracketL', -90); counter.pose('BracketR', 90); }
  if (lamp && active) swapPart(lamp, 'Body', 'lampGlassWarm', 'lampGlassWarm');
  if (lamp && !active) swapPart(lamp, 'Body', 'lampGlassWarm', 'roomBulbOff');
  info.lampOn = active;

  // ---- instruction strip + old strips in the pocket
  const ver = (st === 'v3' || st === 'inspect' || st === 'aid' || st === 'trace') ? 3 : 2;
  const strip = await P('kabul_talimati_v' + ver);
  if (strip) printPlane(panel, [0.185, 0.155, -0.0187], [0.080, 0.210], strip, { rough: 0.85 });
  const oldVers = ver === 3 ? (st === 'v3' ? [1] : [2, 1]) : [1];
  for (let i = 0; i < oldVers.length; i++) { const v = oldVers[i]; const t = v === 2 ? (await P('kabul_talimati_v2')) : null;
    printPlane(panel, [0.325 - 0.004 + i * 0.007, 0.175 + 0.045 + i * 0.016 - 0.105, -0.030 + i * 0.007], [0.080, 0.210], t || stripHead(v, en), { rough: 0.9, rot: i ? 2 : -1.5 }); }
  if (st === 'v3') { // the S2 strip on its way into the pocket (half in, leaning)
    const s2 = await P('kabul_talimati_v2'); const pl = printPlane(panel, [0.33, 0.215, -0.034], [0.080, 0.210], s2 || stripHead(2, en), { rough: 0.85, rot: 6, normal: [0, 0.17, -0.985], up: [0, 0.985, 0.17] }); }

  if (!active) { info.proj = {}; return finish(); }

  // ---- loose tools on the counter leaf (frames: counter Top part)
  const top = counter.parts.Top, tp = top.pivot; const L = (p) => [p[0] - tp[0], p[1] - tp[1], p[2] - tp[2]];
  const rack = inst(top.group, 'DecisionStampRack', L([0.30, 0.003, -0.305]), -8);
  const DEC = [['kabul', en ? ['ADMIT ·', 'FULL ACCESS'] : ['KABUL ·', 'TAM ERİŞİM'], en ? 'ADMIT' : 'KABUL', '#24592E'], ['kisitli', en ? ['RESTRICTED', 'ADMISSION'] : ['KISITLI', 'KABUL'], en ? 'LIMIT' : 'KISITLI', '#A9520A'],
    ['ret', en ? ['REJECT'] : ['REDDET'], en ? 'REJECT' : 'RET', '#9A1E14'], ['yardim', en ? ['OUTSIDE', 'AID'] : ['DIŞARIDAN', 'YARDIM'], en ? 'AID' : 'YARDIM', '#675F50']];
  const usedDec = st === 'stamped' ? 1 : st === 'aid' ? 3 : -1;
  if (rack) DEC.forEach(([k, lab, cap, col], i) => {
    slotText(rack, 'TXT_S' + (i + 1), plateLines(lab, lab.length > 1 ? 5.2 : 6.5, 5000));
    if (i === usedDec) return;
    const a = rack.meta.anchors.find(z => z.name === 'Slot' + (i + 1)).pos; const s = inst(rack.root, 'DecisionStamp', [a[0], a[1] + 0.0005, a[2]], 0);
    if (s) { s.parts.Band.group.traverse(o => { if (o.isMesh) { o.material = material('paintCream').clone(); o.material.color.set(col); } }); slotText(s, 'TXT_Cap', plateLines(cap, 3.6, 8000, 'cp', '#E9DDBE')); (I._DecisionStamp ||= []).push(s); }
  });
  // magnifier (inspect: ruler across the document, lens over the transfer number)
  const magM = st === 'trace' ? null : inst(top.group, 'Magnifier', st === 'inspect' ? L([0.03, 0.0048, -0.112]) : L([0.02, 0.0035, -0.305]), st === 'inspect' ? -3 : 182);
  if (magM && st === 'inspect') magM.pose('Carriage', 0.158);
  // request pad (+ pencil)
  const pad = inst(top.group, 'RequestPad', L([-0.295, 0.003, -0.30]), 6);
  if (pad) {
    if (st === 'pending' || st === 'stamped') { showPart(pad, 'Sheet', false); printPlane(pad, [0, 0.0112, 0.0], [0.1, 0.133], requestForm(cand, en, true), { normal: [0, 1, 0], up: [0, 0, 1], rough: 0.9 }); }
    else printPlane(pad, [0, 0.0117, -0.004], [0.1, 0.133], requestForm(cand, en, false), { part: 'Sheet', normal: [0, 1, 0], up: [0, 0, 1], rough: 0.9 });
  }
  const pen = inst(top.group, 'Pencil', L([-0.215, 0.0075, -0.33]), 70);
  // MASA key (desk-rail twin) at the leaf's front left + brass plate
  const key = inst(top.group, 'KeyKey', L([-0.385, 0.0, -0.31]), 0);
  if (key) { showPart(key, 'Tag', false); const im = new THREE.CanvasTexture(deskIcon()); im.colorSpace = THREE.SRGBColorSpace; key.root.traverse(o => { if (o.isMesh && o.userData.mat === 'keyIcon') o.material = new THREE.MeshStandardMaterial({ map: im, roughness: 0.6 }); }); }
  printPlane(counter, [-0.385, 0.0034, -0.363], [0.062, 0.016], platePrint(en ? 'DESK' : 'MASA'), { part: 'Top', normal: [0, 1, 0], up: [0, 0, 1], metal: 0.4, rough: 0.45, paper: false });

  // ---- the application documents
  const docT = await P('kimlik_sevk_' + cand);
  const docOnCounter = ['inspect', 'pending', 'stamped', 'aid'].includes(st);
  if (st === 'present' && docT) { panel.pose('Drawer', -0.22); printPlane(panel, [-0.26, 0.0248, 0.08], [0.148, 0.105], docT, { part: 'Drawer', normal: [0, 1, 0], up: [0, 0, 1], rot: 3, rough: 0.85 }); }
  const DOC = { c: [0.02, 0.0043, -0.215], rot: -3 };
  const docXY = (rx, rz) => { const a = DOC.rot * D2R; return [DOC.c[0] + rx * Math.cos(a) - rz * Math.sin(a), DOC.c[1], DOC.c[2] + rx * Math.sin(a) + rz * Math.cos(a)]; };
  if (docOnCounter && docT && st !== 'aid') printPlane(counter, DOC.c, [0.148, 0.105], docT, { part: 'Top', normal: [0, 1, 0], up: [0, 0, 1], rot: DOC.rot, rough: 0.85 });
  // decision impression
  const stampOf = { stamped: ['damga_kisitli_3', [0.1149, 0.0252], [-0.012, -0.031]], aid: ['damga_yardim_2', [0.1158, 0.0319], [-0.012, -0.030]] };
  if (stampOf[st]) { const [nm, sz, rel] = stampOf[st]; const sT = await P(nm);
    if (st === 'aid') { panel.pose('Drawer', -0.22); printPlane(panel, [-0.26, 0.0248, 0.13], [0.148, 0.105], docT, { part: 'Drawer', normal: [0, 1, 0], up: [0, 0, 1], rot: 2, rough: 0.85 });
      if (sT) printPlane(panel, [-0.26 - 0.008, 0.0252, 0.13 + 0.022], sz, sT, { part: 'Drawer', normal: [0, 1, 0], up: [0, 0, 1], rot: -2, transparent: true, rough: 0.9 });
      for (const sx of [-1, 1]) { const t = inst(panel.parts.Drawer.group, 'RationTin', [-0.26 + sx * 0.058 - panel.parts.Drawer.pivot[0], 0.024 + 0.0375 - panel.parts.Drawer.pivot[1], -0.028 - panel.parts.Drawer.pivot[2]], 0, { euler: [90, 0, 0] });
        if (t) slotText(t, 'TXT_Stencil', plateLines(en ? 'RATIONS' : 'ERZAK', 12, 4000, 'st', '#D9D2BF', 0.06)); } }
    else if (sT) { const sp = docXY(rel[0], rel[1]); sp[1] += 0.0004; printPlane(counter, sp, sz, sT, { part: 'Top', normal: [0, 1, 0], up: [0, 0, 1], rot: DOC.rot - 4, transparent: true, rough: 0.9 }); }
    // the used stamp, lifted 25 mm over the document after pressing
    const [k, , cap, col] = DEC[usedDec]; const s = inst(top.group, 'DecisionStamp', st === 'aid' ? L([-0.10, 0.03, -0.16]) : L([0.072, 0.038, -0.172]), -14);
    if (s) { s.parts.Band.group.traverse(o => { if (o.isMesh) { o.material = material('paintCream').clone(); o.material.color.set(col); } }); slotText(s, 'TXT_Cap', plateLines(cap, 3.6, 8000, 'cp', '#E9DDBE')); }
  }

  // ---- findings slips: clip (fanned) and the one coming out of the mouth
  const slipList = { inspect: [], pending: ['bulgu_karsilastir_aylin', 'bulgu_gorusme_aylin', 'bulgu_soguk_aylin'], stamped: ['bulgu_karsilastir_aylin', 'bulgu_gorusme_aylin', 'bulgu_soguk_aylin', 'bulgu_diskayit_sonuc_aylin'], aid: ['bulgu_karsilastir_rauf'] }[st] || [];
  const outSlip = { inspect: 'bulgu_karsilastir_rauf', pending: 'bulgu_diskayit_bekliyor_aylin' }[st];
  for (let i = 0; i < slipList.length; i++) { const t = await P(slipList[i]); if (!t) continue;
    printPlane(counter, [0.02 + (i - (slipList.length - 1) / 2) * 0.014, 0.0040 + i * 0.0004, -0.078 - i * 0.004], [0.09, 0.06], t, { normal: [0, 1, 0], up: [0, 0, 1], rot: (i % 2 ? 4 : -3) + i, rough: 0.85 }); }
  if (st === 'v3') { const c = slipCanvas(en ? 'KYGA · REVIEW SLIP' : 'KYGA · İNCELEME FİŞİ', 'V.008', en ? 'ORDERS 3 · ITEM 1' : 'TALİMAT 3 · MADDE 1',
      en ? ['Matching old file: Aylin', '(restricted, S.006: fog seen).', 'REVIEW tab on her file.'] : ['Eşleşen eski dosya: Aylin', '(kısıtlı, V.006: buğu var).', 'Dosyasına İNCELE sekmesi.'], en ? ['fog alone is no proof'] : ['buğu tek başına kanıt değil'], en ? 'REVIEW' : 'İNCELE');
    const t = new THREE.CanvasTexture(c); t.colorSpace = THREE.SRGBColorSpace; printPlane(panel, [0.02, 0.032, -0.064], [0.09, 0.06], t, { normal: [0, 0.94, -0.34], up: [0, 0.34, 0.94], rough: 0.85 }); }
  if (outSlip) { const t = await P(outSlip); if (t) printPlane(panel, [0.02, 0.032, -0.064], [0.09, 0.06], t, { normal: [0, 0.94, -0.34], up: [0, 0.34, 0.94], rough: 0.85 }); }
  // the request sheet going into the GİDEN slot
  if (st === 'pending') { const c = requestForm(cand, en, false); const [cc, x] = canvas(840, 560); x.drawImage(c, 0, -560 + 0); const t = new THREE.CanvasTexture(cc); t.colorSpace = THREE.SRGBColorSpace;
    printPlane(panel, [-0.26, 0.135, -0.046], [0.1, 0.067], t, { normal: [0, 0.26, -0.966], up: [0, 0.966, 0.26], rot: -2, rough: 0.9 }); }
  // compare: marker on item 2 (N-17), jewel lit; interview: intercom button + LED
  if (st === 'inspect') { panel.pose('Marker', -0.083); swapPart(panel, 'Jewel', 'jewelAmberOff', 'jewelAmberOn'); }
  if (st === 'pending' && ic) { /* interview done earlier; LED off */ }
  if (q.get('ic') === '1' && ic && ic.parts.Button) ic.pose('Button', 0.004);
  // person file (trace): the R10 spread open on the leaf
  if (st === 'trace') { const t = await P('kisi_dosyasi_' + cand); if (t) printPlane(counter, [-0.05, 0.0046, -0.232], [0.296, 0.21], t, { part: 'Top', normal: [0, 1, 0], up: [0, 0, 1], rot: -1.5, rough: 0.85 }); }

  // ---- callout points (world)
  const A = (m, p, part) => m ? worldOf(m, p, part) : null;
  Object.assign(PTS, {
    drawer: A(panel, [-0.26, 0.057, -0.066], 'Drawer'), drawerTray: A(panel, [-0.26, 0.03, 0.08], 'Drawer'), outSlot: A(panel, [-0.26, 0.17, -0.036]), strip: A(panel, [0.185, 0.155, -0.02]),
    stripTop: A(panel, [0.185, 0.24, -0.02]), marker: A(panel, [0.117, 0.242, -0.028], 'Marker'), archive: A(panel, [0.325, 0.2, -0.03]), mouth: A(panel, [0.02, 0.042, -0.036]),
    clip: A(counter, [0.02, 0.012, -0.05]), leafFront: A(counter, [0.0, -0.015, -0.38], 'Top'), bracket: A(counter, [0.32, -0.12, -0.15], 'BracketR'), under: A(counter, [0, -0.03, -0.205], 'Top'),
    doc: A(counter, DOC.c, 'Top'), docBelge: A(counter, docXY(0.021, 0.006), 'Top'), docSevk: A(counter, docXY(0.058, 0.025), 'Top'), docStamp: A(counter, docXY(-0.012, -0.031), 'Top'),
    rack: A(counter, [0.30, 0.06, -0.305], 'Top'), pad: A(counter, [-0.295, 0.012, -0.30], 'Top'), ruler: A(counter, st === 'inspect' ? [0.03, 0.006, -0.112] : [0.02, 0.006, -0.305], 'Top'),
    lens: magM ? anchorWorld(magM, 'Lens') : null, key: A(counter, [-0.385, 0.03, -0.31], 'Top'), keyPlate: A(counter, [-0.385, 0.004, -0.363], 'Top'),
    lamp: lamp ? worldOf(lamp, [0, 0.0, -0.29]) : null, intercom: ic ? worldOf(ic, [0, 0.14, -0.065]) : null, icButton: ic ? worldOf(ic, [-0.03, 0.05, -0.07]) : null, icLed: ic ? worldOf(ic, [0.035, 0.05, -0.07]) : null,
    glass: cg ? worldOf(cg, [0, 0, -0.03]) : null, glassCover: cg ? worldOf(cg, [0, -0.05, -0.04], 'Cover') : null, peep: leaf ? worldOf(leaf, [0, 1.56, -0.02], 'Hinge') : null,
  });
  return finish();

  function finish() {
    const A2 = (m, p, part) => m ? worldOf(m, p, part) : null;
    const kl = first(I, 'KnockLamp'), hz = first(I, 'HazardHeader'), fr = first(I, 'DoorFrame'), cor = first(I, 'Corridor');
    Object.assign(PTS, {
      knock: kl ? worldOf(kl, [0, 0.07, -0.07]) : null, hazard: hz ? worldOf(hz, [0.3, 0.06, -0.01]) : null, wheel: leaf ? worldOf(leaf, [0, 1.05, -0.12], 'Wheel') : null,
      leafMid: leaf ? worldOf(leaf, [-0.2, 1.3, -0.01], 'Hinge') : null, stencil: leaf ? worldOf(leaf, [0, 0.66, -0.01], 'Hinge') : null, peepCover: leaf ? worldOf(leaf, [0, 1.56, -0.03], 'Hinge') : null,
      hinge1: fr ? worldOf(fr, [0.44, 1.70, -0.03]) : null, hinge2: fr ? worldOf(fr, [0.44, 1.00, -0.03]) : null, hinge3: fr ? worldOf(fr, [0.44, 0.30, -0.03]) : null,
      frame: fr ? worldOf(fr, [-0.49, 1.25, -0.02]) : null, reveal: fr ? worldOf(fr, [-0.52, 1.5, 0.2]) : null, threshold: fr ? worldOf(fr, [0.0, 0.012, 0.12]) : null,
      stairs: cor ? worldOf(cor, [-0.45, 0.85, 1.9]) : null, landing: cor ? worldOf(cor, [0.3, 0.05, 1.0]) : null, corrLamp: (I.CorridorLamp || [])[0] ? worldOf(I.CorridorLamp[0], [0, 0.07, -0.07]) : null,
      leafOpen: leaf ? worldOf(leaf, [-0.3, 1.2, 0.08], 'Hinge') : null, counterFolded: A2(counter, [0, 0.2, -0.13], 'Top'), intercomK: ic ? worldOf(ic, [0, 0.14, -0.065]) : null,
      glassK: cg ? worldOf(cg, [0, 0, -0.03]) : null, lampK: lamp ? worldOf(lamp, [0, 0, -0.29]) : null,
    });
    for (const k of Object.keys(PTS)) if (!PTS[k]) delete PTS[k];
    window.__admPts = PTS;
    const record = (h, cam) => { info.proj = {}; for (const [k, v] of Object.entries(PTS)) { const p = v.clone().project(cam); if (p.z > 1 || p.z < -1) continue; info.proj[k] = [+((p.x + 1) / 2 * h.W).toFixed(0), +((1 - p.y) / 2 * h.H).toFixed(0)]; } };
    const num = (h, k, d) => h.num(k, d);
    const poses = {
      K8A: (h) => { const c = h.lookCam([num(h, 'cx', 1.44), num(h, 'cy', 1.69), num(h, 'cz', -0.27)], [num(h, 'tx', 2.08), num(h, 'ty', 1.00), num(h, 'tz', -0.27)], num(h, 'fov', 47)); record(h, c); return c; },
      K8G: (h) => { const c = h.lookCam([num(h, 'cx', 1.62), num(h, 'cy', 1.52), num(h, 'cz', 0.40)], [num(h, 'tx', 2.21), num(h, 'ty', 1.45), num(h, 'tz', 0.62)], num(h, 'fov', 30)); record(h, c); return c; },
      K8R: (h) => { const t = admWorld([num(h, 'rx', 0.0), 0, num(h, 'rz', -0.23)]), p = admWorld([num(h, 'rx', 0.0), num(h, 'rh', 0.40), num(h, 'rz', -0.23) - 0.10]);
        const c = h.lookCam(p, t, num(h, 'fov', 40)); record(h, c); return c; },
      K8P: (h) => { const c = h.lookCam([num(h, 'cx', 0.05), num(h, 'cy', 1.35), num(h, 'cz', 0.95)], [num(h, 'tx', 2.2), num(h, 'ty', 1.30), num(h, 'tz', 0.85)], num(h, 'fov', 60)); record(h, c); return c; },
    };
    const lights = [(h) => { if (!active || !lamp) return; const p = h.lightWorld(lamp, 'CounterSpot'); if (!p) return;
      const s = h.sh(new THREE.SpotLight(0xffd9a0, h.num('cl', 4), 0, 34 * D2R, 0.65, 2), 1024, -0.0008); s.position.copy(p); s.target.position.copy(U(...admWorld([0.04, 0, -0.21]))); h.addL(s, 'tezgâh lambası (gölgeli)'); }];
    return { poses, lights, info };
  }
}
