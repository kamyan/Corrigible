// Rev3 scene utilities: instantiate library models in Unity coordinates, pose parts, fill TMP text slots with canvas text,
// print textures (dials, scales, digit drums). Browser only.
import * as THREE from 'three';
import { MODELS } from './registry.js';
import { U, setUnityEuler, D2R } from './mlib.js';
import { material, MATS } from './mats.js';

export { U, D2R };
const FONT = { cp: '"Courier Prime"', lf: '"Libre Franklin"', ka: '"Kalam"', st: '"Big Shoulders Stencil Display"' };

export function has(name) { return !!MODELS[name]; }
// build a model and place it (Unity world pos, yaw deg about +y, optional full euler)
export function inst(parent, name, pos = [0, 0, 0], yaw = 0, o = {}) {
  const def = MODELS[name]; if (!def) { console.warn('missing model', name); return null; }
  const m = def.build();
  const g = new THREE.Group(); g.name = 'inst:' + name; g.position.copy(U(...pos)); setUnityEuler(g, o.euler || [0, yaw, 0]);
  if (o.scale) g.scale.setScalar(o.scale);
  g.add(m.root); parent.add(g); m.holder = g;
  m.root.traverse(ob => { if (ob.isMesh) { ob.castShadow = o.cast === false ? false : ob.castShadow; ob.receiveShadow = true; } });
  if (o.pose) for (const [k, v] of Object.entries(o.pose)) m.pose(k, v);
  return m;
}
export function placeAll(parent, name, o = {}) { const def = MODELS[name]; if (!def) return []; const probe = def.build(); const P = probe.meta.place || [];
  return P.map(p => inst(parent, name, p.pos, p.yaw || 0, o)); }
// world position of a model-space point (Unity) for a placed model
export function worldOf(m, p, part = 'Body') { const grp = m.parts[part].group; m.root.updateMatrixWorld(true); const pv = m.parts[part].pivot;
  return grp.localToWorld(U(p[0] - pv[0], p[1] - pv[1], p[2] - pv[2])); }
export function lightWorld(m, name) { const l = m.meta.lights.find(x => x.name === name); if (!l) return null; return worldOf(m, l.pos, l.part); }
export function anchorWorld(m, name) { const a = m.meta.anchors.find(x => x.name === name); if (!a) return null; return worldOf(m, a.pos, a.part); }
// replace a material key on one model instance (e.g. emissive on/off, textured print)
export function setMat(m, key, mat, part) { m.root.traverse(o => { if (o.isMesh && o.userData.mat === key && (!part || o.name.startsWith(part + ':'))) { o.material = mat; if (key === 'counterDrum') drumUV(o.geometry); } }); }
// cylindrical UVs for digit drums (geometry axis y): u = angle around (0.5 faces three +z = Unity −z), v = along the axis
export function drumUV(g) { if (g.userData.drumUV) return; const P = g.attributes.position; let y0 = 1e9, y1 = -1e9; for (let i = 0; i < P.count; i++) { y0 = Math.min(y0, P.getY(i)); y1 = Math.max(y1, P.getY(i)); }
  const uv = g.attributes.uv; for (let i = 0; i < P.count; i += 3) { const us = []; for (let k = 0; k < 3; k++) { const x = P.getX(i + k), z = P.getZ(i + k); us.push(Math.atan2(x, z) / (2 * Math.PI) + 0.5); }
    const mx = Math.max(...us); for (let k = 0; k < 3; k++) { let u = us[k]; if (mx - u > 0.5) u += 1; uv.setXY(i + k, u, (P.getY(i + k) - y0) / (y1 - y0)); } } uv.needsUpdate = true; g.userData.drumUV = true; }
export function matLike(key, extra = {}) { const b = material(key).clone(); Object.assign(b, extra); b.needsUpdate = true; return b; }

// ---------------------------------------------------------------- canvas text (TMP stand-in)
export function textCanvas(lines, o = {}) {
  // lines: [{t, size(px), w(weight), f(font key), c(colour), ls(em), align}] ; canvas size = slot size × ppm
  const W = Math.round(o.w), H = Math.round(o.h); const c = document.createElement('canvas'); c.width = W; c.height = H; const x = c.getContext('2d');
  if (o.bg) { x.fillStyle = o.bg; x.fillRect(0, 0, W, H); }
  const total = lines.reduce((s, l) => s + (l.size * (l.lh ?? 1.18)), 0); let y = o.valign === 'top' ? (o.pad ?? 0) : (H - total) / 2;
  for (const l of lines) {
    const size = l.size; x.font = `${l.w || 700} ${size}px ${FONT[l.f || 'cp']}`; x.fillStyle = l.c || '#1E1B16';
    x.letterSpacing = ((l.ls || 0) * size) + 'px'; x.textBaseline = 'middle';
    const al = l.align || o.align || 'center'; x.textAlign = al; const tx = al === 'center' ? W / 2 : al === 'left' ? (o.pad ?? 0) : W - (o.pad ?? 0);
    let s = l.t; let fs = size; while (x.measureText(s).width > W - 2 * (o.pad ?? 0) && fs > 6) { fs -= 1; x.font = `${l.w || 700} ${fs}px ${FONT[l.f || 'cp']}`; }
    const lh = size * (l.lh ?? 1.18); x.fillText(s, tx, y + lh / 2 + (l.dy || 0)); y += lh;
  }
  return c;
}
// fill a model's text slot with lines; ppm = canvas pixels per metre
export function slotText(m, slot, lines, o = {}) {
  const t = m.meta.texts.find(q => q.name === slot); if (!t) { console.warn('no slot', m.name, slot); return null; }
  const ppm = o.ppm || 4000; const [w, h] = t.size;
  const cv = textCanvas(lines.map(l => typeof l === 'string' ? { t: l, size: (t.capMm || h * 700) * ppm / 1000 / 0.7 } : l), { w: w * ppm, h: h * ppm, align: o.align || t.align, pad: o.pad, bg: o.bg });
  const tex = new THREE.CanvasTexture(cv); tex.colorSpace = THREE.SRGBColorSpace; tex.anisotropy = 8;
  const mat = new THREE.MeshStandardMaterial({ map: tex, transparent: !o.bg, roughness: o.rough ?? 0.7, metalness: o.metal ?? 0, depthWrite: !!o.bg, polygonOffset: true, polygonOffsetFactor: -2 });
  if (o.emissive) { mat.emissive = new THREE.Color(o.emissive); mat.emissiveMap = tex; mat.emissiveIntensity = o.ei ?? 1; }
  const pl = new THREE.Mesh(new THREE.PlaneGeometry(w, h), mat); pl.renderOrder = 3;
  // orient: plane faces +z three; slot normal is Unity
  const n = new THREE.Vector3(t.normal[0], t.normal[1], -t.normal[2]).normalize(), up = new THREE.Vector3(t.up[0], t.up[1], -t.up[2]).normalize();
  const xAxis = new THREE.Vector3().crossVectors(up, n).normalize(); const yAxis = new THREE.Vector3().crossVectors(n, xAxis);
  pl.quaternion.setFromRotationMatrix(new THREE.Matrix4().makeBasis(xAxis, yAxis, n));
  const P = m.parts[t.part]; const pv = P.pivot; pl.position.copy(U(t.center[0] - pv[0], t.center[1] - pv[1], t.center[2] - pv[2])).addScaledVector(n, o.lift ?? 0.0004);
  pl.castShadow = false; pl.receiveShadow = true; P.group.add(pl); return pl;
}
// a free print plane in a model's space (Unity coords), facing normal (default −z)
export function printPlane(m, center, size, canvasOrTex, o = {}) {
  const tex = canvasOrTex.isTexture ? canvasOrTex : new THREE.CanvasTexture(canvasOrTex); tex.colorSpace = THREE.SRGBColorSpace; tex.anisotropy = 8;
  const mat = new THREE.MeshStandardMaterial({ map: tex, transparent: !!o.transparent, roughness: o.rough ?? 0.6, metalness: o.metal ?? 0, polygonOffset: true, polygonOffsetFactor: -1 });
  if (o.emissive) { mat.emissive = new THREE.Color(o.emissive); mat.emissiveMap = tex; mat.emissiveIntensity = o.ei ?? 1; }
  const geo = o.circle ? new THREE.CircleGeometry(size[0] / 2, 48) : new THREE.PlaneGeometry(size[0], size[1]);
  const pl = new THREE.Mesh(geo, mat); const part = o.part || 'Body'; const P = m.parts[part]; const pv = P.pivot;
  const n = new THREE.Vector3(...(o.normal || [0, 0, -1])); n.z = -n.z; n.normalize(); const up = new THREE.Vector3(...(o.up || [0, 1, 0])); up.z = -up.z; up.normalize();
  const xAxis = new THREE.Vector3().crossVectors(up, n).normalize(); const yAxis = new THREE.Vector3().crossVectors(n, xAxis);
  pl.quaternion.setFromRotationMatrix(new THREE.Matrix4().makeBasis(xAxis, yAxis, n)); if (o.rot) pl.rotateZ(o.rot * D2R);
  pl.position.copy(U(center[0] - pv[0], center[1] - pv[1], center[2] - pv[2])); pl.receiveShadow = true; pl.castShadow = false; pl.renderOrder = 2; P.group.add(pl); return pl;
}
export function canvas(w, h) { const c = document.createElement('canvas'); c.width = w; c.height = h; return [c, c.getContext('2d')]; }
// digit drum texture: 10 cells around u (0 at u=0), cream digits on dark drum
export function drumTex(o = {}) { const [c, x] = canvas(1280, 256); x.fillStyle = o.bg || '#1C150E'; x.fillRect(0, 0, 1280, 256);
  x.fillStyle = o.fg || '#EFDCAE'; x.font = `700 ${o.size || 118}px "Courier Prime"`; x.textAlign = 'center'; x.textBaseline = 'middle';
  for (let i = 0; i < 10; i++) { x.save(); x.translate(64 + i * 128, 128); x.rotate(o.rot ?? Math.PI / 2); x.fillText(String(i), 0, 6); x.restore(); }
  const t = new THREE.CanvasTexture(c); t.colorSpace = THREE.SRGBColorSpace; t.wrapS = t.wrapT = THREE.RepeatWrapping; t.anisotropy = 8; return t; }
// rotate a drum part so `digit` faces the viewer (−z) given drum axis x; returns angle
export function drumAngle(digit) { return 162 - digit * 36; }
