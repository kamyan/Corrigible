// Shift close (supplement 2026-11-05: R19, K6) — owner: main. The brief's covered shift switch returns unchanged; a slim
// slip printer (SlipMouth) on its left flank ejects the HANDOVER SLIP, which the player drags into the desk's EVRAK tray.
// URL state: &sw=ready|cover|down|locked  &slip=none|emerge|out|hand|over|drop  &sl=v001|v031 (slip print)  &tx=devir (telex)
// &new=1 (counters for the card game: ERZAK /60, MALZEME = sum of raw materials)
import * as THREE from 'three';
import { inst, worldOf } from './scene3.js';

const first = (I, n) => (I[n] || []).find(Boolean) || null;   // papers: /r10/papers_new/ (work/r10 → scratchpad/r10)

export const K6 = { cam: [0.40, 1.28, 0.42], look: [0.665, 0.82, 1.08], fov: 42 };

export function pre(ctx) {
  const q = ctx.q; if (!q) return; const D = ctx.D; const sw = q.get('sw'); const en = ctx.lang === 'en';
  if (q.get('new') === '1' || sw || q.get('slip')) {     // card-game desk: ration cap 60, materials = raw material total
    const late = (ctx.sk === 'v031');
    D.counters = late ? [{ d: '031' }, { d: '071', frac: '5' }, { d: '17' }, { d: '016' }, { d: '03' }, { d: '12' }] : [{ d: '001' }, { d: '060', frac: '0' }, { d: '24' }, { d: '008' }, { d: '00' }, { d: '10' }]; }
  if (sw === 'ready') { D.plate2 = en ? 'lift the cover' : 'kapağı kaldır'; D.locked = false; }
  if (sw === 'cover') { D.plate2 = en ? 'pull the lever' : 'kolu indir'; }
  if (sw === 'down') { D.plate2 = en ? 'file the slip in EVRAK' : "fişi EVRAK'a koy"; D.switchLamp = false; D.plate2Red = true; }
  if (sw === 'locked') { D.plate2 = en ? 'raid in progress' : 'baskın sürüyor'; D.switchLamp = false; D.locked = true; }
  if (q.get('tx') === 'devir') D.telexHang = [[en ? '002/01' : '002/01', { t: en ? 'SHIFT HANDED OVER' : 'VARDİYA DEVREDİLDİ' }, en ? 'Handover slip filed in EVRAK. New shift started.' : 'Devir fişi EVRAK kutusuna kondu. Yeni vardiya başladı.'], ['001/01', { t: en ? 'PRE-CHECK' : 'ÖN KONTROL' }, '']];
  if (q.get('tx') === 'onkontrol') D.telexHang = [['001/01', { t: en ? 'PRE-CHECK' : 'ÖN KONTROL' }, en ? 'Plan ready. Print the handover slip.' : 'Plan hazır. Devir fişini çıkar.'], ['001/00', { t: 'KYGA-07 HAT AÇIK' }, '']];
  if (q.get('tx') === 'devir' && D.counters) D.counters[0] = { d: '002' };
}

async function slipTexture(name) {
  const url = `/r10/papers_new/${name}`; const im = new Image(); im.src = url; try { await im.decode(); } catch (e) { return null; }
  const t = new THREE.Texture(im); t.colorSpace = THREE.SRGBColorSpace; t.anisotropy = 8; t.needsUpdate = true; return t; }

export async function dress(I, ctx = {}) {
  const q = ctx.q || new URLSearchParams(); const sw = q.get('sw'), slip = q.get('slip') || 'none'; const en = ctx.lang === 'en';
  const S = first(I, 'ShiftSwitch'), M = first(I, 'SlipMouth'), tray = first(I, 'PaperTray'); const out = {};
  if (!S) return { info: { switch: false } };
  // ---- switch states (Cover 0 → 112°, Lever +35 → −35°; waiting: cover open, lever down, ready lamp off)
  const st = sw || 'ready';
  S.pose('Cover', st === 'cover' || st === 'down' ? 112 : 0); S.pose('Lever', st === 'down' ? -35 : 35);
  if (S.parts.LockBand) S.parts.LockBand.group.visible = st === 'locked';
  const M3 = await import('./mats.js');
  const setMat = (m, part, key) => m.root.traverse(o => { if (o.isMesh && o.name.startsWith(part + ':')) { o.material = M3.material(key); o.userData.mat = key; } });
  if (st === 'down' || st === 'locked') setMat(S, 'ReadyLamp', 'readyLampOff'); else setMat(S, 'ReadyLamp', 'readyLamp');
  if (M) { setMat(M, 'Lamp', (st === 'down' && slip !== 'drop') ? 'jewelAmberOn' : 'jewelAmberOff');
    const t = M.meta.texts.find(x => x.name === 'TXT_Plate'); if (t) { const { slotText } = await import('./scene3.js'); slotText(M, 'TXT_Plate', [{ t: en ? 'HANDOVER' : 'DEVİR FİŞİ', size: 6.2 * 4 / 0.7, c: '#2A1E10', f: 'cp', w: 700, ls: 0.04 }], { ppm: 4000 }); } }
  // ---- the slip
  const late = q.get('sl') === 'v031' || ctx.sk === 'v031';
  const name = `devir_fisi_${late ? 'v031' : 'v001'}_${en ? 'en' : 'tr'}@2x.png`;
  if (slip !== 'none' && (st === 'down' || slip === 'drop' || slip === 'hand' || slip === 'over')) {
    const tex = await slipTexture(name);
    const mk = () => { const s = inst(ctx.R.root, 'HandoverSlip', [0, 0, 0], 0); if (tex) s.root.traverse(o => { if (o.isMesh) { o.material = new THREE.MeshStandardMaterial({ map: tex, roughness: 0.92, transparent: true, alphaTest: 0.3, side: THREE.DoubleSide }); } }); return s; };
    const s = mk(); const g = s.holder; out.slip = slip;
    const lw = (p) => worldOf(S, p);        // switch-local → world (three)
    if (slip === 'emerge' || slip === 'out') { // lying on the tongue, long side along the switch's −x, text up (+y), top edge toward the mouth
      const t = slip === 'emerge' ? 0.45 : 1; const cx = -0.18 + (-0.12) * t; const p = lw([cx, 0.1356, 0.015]);
      g.position.copy(p); const yaw = 10 * Math.PI / 180;
      g.rotation.order = 'YXZ'; g.rotation.set(-Math.PI / 2, -yaw, Math.PI / 2);   // face up, slip top toward +x (the mouth)
    } else if (slip === 'hand') { // held toward the K6 camera, ~720 px tall, slight tilt
      const cam = new THREE.Vector3(K6.cam[0], K6.cam[1], -K6.cam[2]), look = new THREE.Vector3(K6.look[0], K6.look[1], -K6.look[2]);
      const dir = look.clone().sub(cam).normalize(); const p = cam.clone().addScaledVector(dir, 0.30); p.y -= 0.004; p.x += 0.02;
      g.position.copy(p); g.lookAt(cam); g.rotateZ(-0.05); g.rotateX(-0.08);
    } else if (slip === 'over' && tray) { const p = worldOf(tray, [0, 0.10, 0.0]); g.position.copy(p); g.rotation.order = 'YXZ'; g.rotation.set(-Math.PI / 2 + 0.22, 4 * Math.PI / 180, 0.08);
    } else if (slip === 'drop' && tray) { const p = worldOf(tray, [0.006, 0.0615, -0.004]); g.position.copy(p); g.rotation.order = 'YXZ'; g.rotation.set(-Math.PI / 2, 2 * Math.PI / 180, 0.04); }
  }
  // tray highlight while the slip hovers over it: warm rim light (shadowless) — drop target cue
  const lights = [];
  if (slip === 'over' && tray) lights.push((h) => { const l = new h.THREE.PointLight(0xffb44a, h.num('rim', 0.55), 0.35, 2); l.position.copy(h.worldOf(tray, [0, 0.11, -0.02])); h.addL(l, 'EVRAK hedef ışığı (gölgesiz)'); });
  const poses = {
    K6: (h) => h.lookCam(h.U(...K6.cam), h.U(...K6.look), h.num('fov', K6.fov)),
    K6W: (h) => h.lookCam(h.U(0.22, 1.36, 0.22), h.U(0.60, 0.84, 1.10), h.num('fov', 50)),
  };
  return { poses, lights, info: out };
}
