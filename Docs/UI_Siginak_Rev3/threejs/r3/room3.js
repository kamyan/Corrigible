// Rev3 command room: Rev2 shell (concrete textures, map paper) + every r3 library model at its meta.place, dressed by the
// family dress modules (dress_room_desk.js, dress_room_shelf.js) and the cabinet (menu3.js helpers). Unity coordinates.
import * as THREE from 'three';
import { MODELS } from './registry.js';
import { inst, U, D2R } from './scene3.js';
import * as T from '../rev2/textures.js';
import { assembleCabinet, dressCabinet, dressCabinetPanel } from './menu3.js';

const ROOM = { x0: -2.2, x1: 2.2, z0: -1.6, z1: 2.3, h: 2.5, door: { x: 2.2, z0: 0.32, z1: 1.18, h: 2.0 } };
const FAMS = ['desk', 'walls', 'shelf', 'crt', 'door', 'board', 'admission'];
function plane(root, w, h, mat, pos, rot = [0, 0, 0]) { const m = new THREE.Mesh(new THREE.PlaneGeometry(w, h), mat); m.position.copy(U(...pos)); m.rotation.order = 'YXZ'; m.rotation.set(rot[0] * D2R, rot[1] * D2R, rot[2] * D2R); m.receiveShadow = true; root.add(m); return m; }
const cmat = (tex, r = 0.96) => new THREE.MeshStandardMaterial({ map: tex, roughness: r });

export async function buildRoom3(scene, o = {}) {
  const root = new THREE.Group(); scene.add(root); const R = { root, I: {}, models: [], fam: {} };
  const { x0, x1, z0, z1, h } = ROOM; const D = ROOM.door;
  // ---- shell (Rev2 textures): back wall z1, left wall x0, right wall x1 with the door opening, front wall z0, floor, ceiling
  plane(root, x1 - x0, h, cmat(T.concreteTex(4.4, 2.5, { base: '#7c837f', stains: [[0.18, 0.08, 0.16, 0.5], [0.75, 0.05, 0.22, 0.45], [0.55, 0.95, 0.25, 0.35], [0.05, 0.6, 0.12, 0.35]], damp: true })), [0, h / 2, z1], [0, 0, 0]);
  plane(root, z1 - z0, h, cmat(T.concreteTex(3.9, 2.5, { base: '#737a76', stains: [[0.5, 0.1, 0.25, 0.4]], damp: true, ties: true })), [x0, h / 2, (z0 + z1) / 2], [0, 90, 0]);
  const rw = (za, zb, ya, yb) => plane(root, zb - za, yb - ya, cmat(T.concreteTex(zb - za, yb - ya, { base: '#737a76', stains: [[0.5, 0.1, 0.25, 0.4]], damp: true, ties: true })), [x1, (ya + yb) / 2, (za + zb) / 2], [0, -90, 0]);
  rw(z0, D.z0, 0, h); rw(D.z1, z1, 0, h); rw(D.z0, D.z1, D.h, h);
  plane(root, x1 - x0, h, cmat(T.concreteTex(4.4, 2.5, { base: '#6f7672' })), [0, h / 2, z0], [0, 180, 0]);
  plane(root, x1 - x0, z1 - z0, cmat(T.concreteTex(4.4, 3.9, { ppm: 200, base: '#5a5b54', ties: false, streaks: 0, stains: [[0.5, 0.5, 0.4, 0.3]] }), 0.95), [0, 0, (z0 + z1) / 2], [-90, 0, 0]);
  plane(root, x1 - x0, z1 - z0, new THREE.MeshStandardMaterial({ color: '#3a3d3a', roughness: 0.95 }), [0, h, (z0 + z1) / 2], [90, 0, 0]);
  // ---- map paper on the desk (Rev2 baked texture)
  if (o.mapTexture) { const mm = new THREE.MeshBasicMaterial({ map: o.mapTexture }); const mp = plane(root, 1.20, 0.675, mm, [0, 0.7612, 1.3175], [-90, 0, 0]); mp.receiveShadow = false; R.map = mp; }
  // ---- every room-family model at its placements
  for (const [name, def] of Object.entries(MODELS)) {
    const probe = def.build(); const fam = probe.family || probe.meta.family; if (!FAMS.includes(fam)) continue;
    if (/^Visitor_/.test(name)) continue;                    // silhouettes left the game (supplement: document admission)
    const pl = probe.meta.place || []; if (!pl.length) continue; R.fam[name] = fam;
    R.I[name] = pl.map(p => { const m = inst(root, name, p.pos, p.yaw || 0, p.euler ? { euler: p.euler } : {}); if (m) { m.placeNote = p.note; R.models.push(m); } return m; });
  }
  // ---- settings cabinet on the back wall (closed unless o.cab)
  const cab = inst(root, 'SettingsCabinet', [1.68, 1.17, 2.30], 0); R.cab = cab; R.I.SettingsCabinet = [cab];
  dressCabinet(cab, { lang: o.lang });
  if (o.cab) { cab.pose('Leaf', o.cabLeaf ?? -95); assembleCabinet(cab); dressCabinetPanel(cab, { lang: o.lang, mute: o.mute, values: o.values, locked: o.locked }); }
  // ---- dressing (family modules, when present). Since the 2026-11-05 supplement the shelf + diorama are gone: the planning
  // board (dress_room_board.js) stands in their place, the switch gets its slip printer (dress_room_switch.js) and the door
  // gets the admission counter (dress_room_admission.js). A module's dress() may return { poses: {NAME: (h) => camera},
  // lights: [(h) => void] }; room3.html merges them (h = camera / light helpers). Each module owns its own files.
  R.poses = {}; R.lightFns = []; R.extra = {};
  const mods = ['dress_room_desk.js', 'dress_room_shelf.js', ...(o.extraDress || ['dress_room_board.js', 'dress_room_switch.js', 'dress_room_admission.js'])];
  const loaded = {};
  for (const f of mods) { try { loaded[f] = await import('./' + f); } catch (e) { R['dressErr_' + f] = e.message; } }
  for (const f of mods) { const mod = loaded[f]; if (mod && mod.pre) { try { mod.pre({ ...o.ctx, THREE, R }); } catch (e) { console.warn('pre', f, e.message); } } }
  for (const f of mods) {
    try { const mod = loaded[f]; if (!mod) continue; if (mod.dress) { const ret = await mod.dress(R.I, { ...o.ctx, THREE, R });
        if (ret && ret.poses) Object.assign(R.poses, ret.poses); if (ret && ret.lights) R.lightFns.push(...ret.lights); if (ret && ret.info) R.extra[f] = ret.info; }
      R['dressed_' + f] = true; }
    catch (e) { if (!/Failed to fetch|Importing a module|404/.test(e.message)) console.warn('dress module', f, e.message); R['dressErr_' + f] = e.message; }
  }
  // the old shelf family (shelf, crates, jerrycan, hope plant, diorama, room cards, clipboard, idle tray, pegs) is removed
  if (o.legacyShelf !== true) for (const [name, ms] of Object.entries(R.I)) if (R.fam[name] === 'shelf') for (const m of ms || []) if (m && m.holder) m.holder.visible = false;
  return R;
}
export { ROOM };
