// Rev3 modeling helpers (model library + three.js scene). Runs in the browser and in Node (no DOM).
//
// COORDINATES. Model code is written in Unity-style local coordinates: x right, y up, z forward (away from the
// viewer, toward the back wall). An object's FRONT faces −z (toward the chair / camera). Metres.
// three.js space is (x, y, −z); the helpers convert. Euler angles are Unity degrees (applied Z, X, Y like Unity).
//
// PARTS. Every model has a static part "Body" (the model origin) and optional moving parts. A moving part is a
// group whose origin IS its pivot; its geometry is stored relative to the pivot (OBJ "o <Part>"), and its place is
// written to the manifest (parent, pivot, axis, range). Geometry is modelled at the zero of the range.
import * as THREE from 'three';
import { material, MATS } from './mats.js';

export const D2R = Math.PI / 180;
export const CREASE_DEG = 40;

// ---------------------------------------------------------------- geometry utilities (three space)
function triNormal(ax, ay, az, bx, by, bz, cx, cy, cz) {
  const ux = bx - ax, uy = by - ay, uz = bz - az, vx = cx - ax, vy = cy - ay, vz = cz - az;
  return [uy * vz - uz * vy, uz * vx - ux * vz, ux * vy - uy * vx]; // length = 2 × area
}
// area-weighted normals, merged across faces closer than creaseDeg (hash 0.01 mm)
export function creaseNormals(g, creaseDeg = CREASE_DEG) {
  if (g.index) g = g.toNonIndexed();
  const P = g.attributes.position.array, n = P.length / 9, cosT = Math.cos(creaseDeg * D2R);
  const fn = new Float32Array(n * 3), map = new Map();
  const key = (i) => `${Math.round(P[i] * 1e5)},${Math.round(P[i + 1] * 1e5)},${Math.round(P[i + 2] * 1e5)}`;
  for (let f = 0; f < n; f++) {
    const o = f * 9; const v = triNormal(P[o], P[o + 1], P[o + 2], P[o + 3], P[o + 4], P[o + 5], P[o + 6], P[o + 7], P[o + 8]);
    fn[f * 3] = v[0]; fn[f * 3 + 1] = v[1]; fn[f * 3 + 2] = v[2];
    for (let k = 0; k < 3; k++) { const kk = key(o + k * 3); if (!map.has(kk)) map.set(kk, []); map.get(kk).push(f); }
  }
  const N = new Float32Array(P.length);
  for (let f = 0; f < n; f++) {
    const ax = fn[f * 3], ay = fn[f * 3 + 1], az = fn[f * 3 + 2]; const al = Math.hypot(ax, ay, az) || 1;
    for (let k = 0; k < 3; k++) {
      const list = map.get(key(f * 9 + k * 3)); let sx = 0, sy = 0, sz = 0;
      for (const o of list) { const bx = fn[o * 3], by = fn[o * 3 + 1], bz = fn[o * 3 + 2]; const bl = Math.hypot(bx, by, bz) || 1;
        if ((ax * bx + ay * by + az * bz) / (al * bl) >= cosT) { sx += bx; sy += by; sz += bz; } }
      const l = Math.hypot(sx, sy, sz) || 1; N.set([sx / l, sy / l, sz / l], f * 9 + k * 3);
    }
  }
  g.setAttribute('normal', new THREE.BufferAttribute(N, 3));
  return g;
}
// planar UVs in metres by dominant face axis (1 UV unit = 1 m; materials set tiling)
export function metricUV(g) {
  const P = g.attributes.position.array, n = P.length / 9, uv = new Float32Array(n * 6);
  for (let f = 0; f < n; f++) {
    const o = f * 9; const [nx, ny, nz] = triNormal(P[o], P[o + 1], P[o + 2], P[o + 3], P[o + 4], P[o + 5], P[o + 6], P[o + 7], P[o + 8]);
    const ax = Math.abs(nx), ay = Math.abs(ny), az = Math.abs(nz);
    for (let k = 0; k < 3; k++) { const x = P[o + k * 3], y = P[o + k * 3 + 1], z = P[o + k * 3 + 2];
      let u, v; if (ax >= ay && ax >= az) { u = nx > 0 ? -z : z; v = y; } else if (ay >= az) { u = x; v = ny > 0 ? -z : z; } else { u = nz > 0 ? x : -x; v = y; }
      uv[f * 6 + k * 2] = u; uv[f * 6 + k * 2 + 1] = v; }
  }
  g.setAttribute('uv', new THREE.BufferAttribute(uv, 2));
  return g;
}
function fromTris(tris, opt = {}) { // tris: flat array of xyz (three space), CCW outward
  const g = new THREE.BufferGeometry(); g.setAttribute('position', new THREE.BufferAttribute(new Float32Array(tris), 3));
  metricUV(g); return opt.crease === false ? flatNormals(g) : creaseNormals(g, opt.crease ?? CREASE_DEG);
}
function flatNormals(g) { const P = g.attributes.position.array; const N = new Float32Array(P.length);
  for (let o = 0; o < P.length; o += 9) { const v = triNormal(P[o], P[o + 1], P[o + 2], P[o + 3], P[o + 4], P[o + 5], P[o + 6], P[o + 7], P[o + 8]); const l = Math.hypot(...v) || 1; for (let k = 0; k < 3; k++) N.set([v[0] / l, v[1] / l, v[2] / l], o + k * 3); }
  g.setAttribute('normal', new THREE.BufferAttribute(N, 3)); return g; }
export function prep(g, opt = {}) { // any three geometry -> non-indexed, metric uv (unless keepUV), creased normals
  if (g.index) g = g.toNonIndexed();
  if (!opt.keepUV || !g.attributes.uv) metricUV(g);
  if (opt.smooth) return g; // keep the geometry's own normals
  return creaseNormals(g, opt.crease ?? CREASE_DEG);
}
// push polygon (convex, ordered) as a fan, oriented away from `inside` point
function pushPoly(out, pts, inside) {
  const [a, b, c] = pts; const nrm = triNormal(...a, ...b, ...c);
  const cx = pts.reduce((s, p) => s + p[0], 0) / pts.length - inside[0], cy = pts.reduce((s, p) => s + p[1], 0) / pts.length - inside[1], cz = pts.reduce((s, p) => s + p[2], 0) / pts.length - inside[2];
  const flip = nrm[0] * cx + nrm[1] * cy + nrm[2] * cz < 0; const q = flip ? [...pts].reverse() : pts;
  for (let i = 1; i < q.length - 1; i++) out.push(...q[0], ...q[i], ...q[i + 1]);
}

// ---------------------------------------------------------------- primitives (three space, centred at origin)
// chamfered box: 6 faces + 12 edge bevels + 8 corner triangles (44 tris; 12 without chamfer)
export function gBox(w, h, d, c = 0, opt = {}) {
  const a = w / 2, b = h / 2, e = d / 2; c = Math.min(c, a * 0.95, b * 0.95, e * 0.95);
  const out = []; const O = [0, 0, 0]; const S = [-1, 1];
  if (c <= 1e-6) {
    const C = (sx, sy, sz) => [sx * a, sy * b, sz * e];
    for (const s of S) { pushPoly(out, [C(s, -1, -1), C(s, 1, -1), C(s, 1, 1), C(s, -1, 1)], O); pushPoly(out, [C(-1, s, -1), C(1, s, -1), C(1, s, 1), C(-1, s, 1)], O); pushPoly(out, [C(-1, -1, s), C(1, -1, s), C(1, 1, s), C(-1, 1, s)], O); }
    return fromTris(out, { crease: false });
  }
  const X = (sx, sy, sz) => [sx * a, sy * (b - c), sz * (e - c)], Y = (sx, sy, sz) => [sx * (a - c), sy * b, sz * (e - c)], Z = (sx, sy, sz) => [sx * (a - c), sy * (b - c), sz * e];
  for (const s of S) {
    if (!opt.skip || !opt.skip.includes((s > 0 ? '+' : '-') + 'x')) pushPoly(out, [X(s, -1, -1), X(s, 1, -1), X(s, 1, 1), X(s, -1, 1)], O);
    if (!opt.skip || !opt.skip.includes((s > 0 ? '+' : '-') + 'y')) pushPoly(out, [Y(-1, s, -1), Y(1, s, -1), Y(1, s, 1), Y(-1, s, 1)], O);
    if (!opt.skip || !opt.skip.includes((s > 0 ? '+' : '-') + 'z')) pushPoly(out, [Z(-1, -1, s), Z(1, -1, s), Z(1, 1, s), Z(-1, 1, s)], O);
  }
  for (const s1 of S) for (const s2 of S) {
    pushPoly(out, [X(s1, s2, -1), X(s1, s2, 1), Y(s1, s2, 1), Y(s1, s2, -1)], O); // edges along z
    pushPoly(out, [Y(-1, s1, s2), Y(1, s1, s2), Z(1, s1, s2), Z(-1, s1, s2)], O); // edges along x
    pushPoly(out, [X(s1, -1, s2), X(s1, 1, s2), Z(s1, 1, s2), Z(s1, -1, s2)], O); // edges along y
  }
  for (const sx of S) for (const sy of S) for (const sz of S) pushPoly(out, [X(sx, sy, sz), Y(sx, sy, sz), Z(sx, sy, sz)], O);
  return fromTris(out, { crease: opt.crease ?? 30 });
}
// lathe around +y. profile: [[r, y], ...] bottom → top. Smooth around, hard along the profile.
export function gLathe(profile, seg = 16, opt = {}) {
  const p0 = opt.phiStart ?? 0, pl = opt.phiLength ?? Math.PI * 2, full = pl >= Math.PI * 2 - 1e-6;
  const pos = [], nor = [], uvs = []; let acc = 0;
  const ring = (i) => p0 + pl * i / seg;
  for (let j = 0; j < profile.length - 1; j++) {
    const [r0, y0] = profile[j], [r1, y1] = profile[j + 1]; const dr = r1 - r0, dy = y1 - y0, L = Math.hypot(dr, dy); if (L < 1e-9) continue;
    const nr = dy / L, ny = -dr / L; const v0 = acc, v1 = acc + L; acc = v1;
    for (let i = 0; i < seg; i++) {
      const a0 = ring(i), a1 = ring(i + 1);
      const P = (r, y, a) => [r * Math.sin(a), y, r * Math.cos(a)], Nn = (a) => [nr * Math.sin(a), ny, nr * Math.cos(a)];
      const q = [[P(r0, y0, a0), Nn(a0), [i / seg, v0]], [P(r0, y0, a1), Nn(a1), [(i + 1) / seg, v0]], [P(r1, y1, a1), Nn(a1), [(i + 1) / seg, v1]], [P(r1, y1, a0), Nn(a0), [i / seg, v1]]];
      const tri = (A, B, C) => { for (const t of [A, B, C]) { pos.push(...t[0]); nor.push(...t[1]); uvs.push(...t[2]); } };
      if (r0 > 1e-9 && r1 > 1e-9) { tri(q[0], q[1], q[2]); tri(q[0], q[2], q[3]); } else if (r0 <= 1e-9) tri(q[0], q[2], q[3]); else tri(q[0], q[1], q[2]);
    }
  }
  const cap = (r, y, up) => { if (r <= 1e-9 || opt.caps === false) return; for (let i = 0; i < seg; i++) { const a0 = ring(i), a1 = ring(i + 1);
    const A = [0, y, 0], B = [r * Math.sin(a0), y, r * Math.cos(a0)], C = [r * Math.sin(a1), y, r * Math.cos(a1)];
    const tri = up ? [A, B, C] : [A, C, B]; for (const t of tri) { pos.push(...t); nor.push(0, up ? 1 : -1, 0); uvs.push(t[0], t[2]); } } };
  if (full || opt.caps) { cap(profile[0][0], profile[0][1], false); cap(profile[profile.length - 1][0], profile[profile.length - 1][1], true); }
  const g = new THREE.BufferGeometry(); g.setAttribute('position', new THREE.Float32BufferAttribute(pos, 3)); g.setAttribute('normal', new THREE.Float32BufferAttribute(nor, 3)); g.setAttribute('uv', new THREE.Float32BufferAttribute(uvs, 2));
  // fix winding so faces point outward (lathe above is CCW from outside when phi increases toward +x from +z)
  return fixWinding(g);
}
function fixWinding(g) { // flip triangles whose geometric normal disagrees with the stored vertex normal
  const P = g.attributes.position.array, N = g.attributes.normal.array, U = g.attributes.uv.array;
  for (let o = 0, t = 0; o < P.length; o += 9, t += 6) {
    const v = triNormal(P[o], P[o + 1], P[o + 2], P[o + 3], P[o + 4], P[o + 5], P[o + 6], P[o + 7], P[o + 8]);
    const s = v[0] * (N[o] + N[o + 3] + N[o + 6]) + v[1] * (N[o + 1] + N[o + 4] + N[o + 7]) + v[2] * (N[o + 2] + N[o + 5] + N[o + 8]);
    if (s < 0) { for (let k = 0; k < 3; k++) { const a = P[o + 3 + k]; P[o + 3 + k] = P[o + 6 + k]; P[o + 6 + k] = a; const b = N[o + 3 + k]; N[o + 3 + k] = N[o + 6 + k]; N[o + 6 + k] = b; }
      for (let k = 0; k < 2; k++) { const c = U[t + 2 + k]; U[t + 2 + k] = U[t + 4 + k]; U[t + 4 + k] = c; } }
  }
  return g;
}
// chamfered cylinder along +y, centred
export function gCyl(r, h, seg = 16, c = 0, opt = {}) {
  const r2 = opt.r2 ?? r; const y0 = -h / 2, y1 = h / 2; const cb = opt.cb ?? c, ct = opt.ct ?? c;
  const prof = [[0, y0]]; if (cb > 0) prof.push([r - cb, y0], [r, y0 + cb]); else prof.push([r, y0]);
  if (ct > 0) prof.push([r2, y1 - ct], [r2 - ct, y1]); else prof.push([r2, y1]); prof.push([0, y1]);
  if (opt.open) { prof.shift(); prof.pop(); }
  return gLathe(prof, seg, { caps: !opt.open, ...opt });
}
// tube (hollow cylinder) with wall t, along +y, centred, chamfer c on the rims
export function gRing(r, t, h, seg = 24, c = 0) {
  const ri = r - t, y0 = -h / 2, y1 = h / 2;
  return gLathe([[ri, y1 - c], [ri, y0 + c], [ri + c, y0], [r - c, y0], [r, y0 + c], [r, y1 - c], [r - c, y1], [ri + c, y1], [ri, y1 - c]].filter((p, i, a) => i === 0 || Math.hypot(p[0] - a[i - 1][0], p[1] - a[i - 1][1]) > 1e-9), seg, { caps: false });
}
// extrusion of a 2D outline (x, y in three space) along z, centred; chamfer c on both faces (inset, nominal outline kept)
export function gExtrude(pts, depth, c = 0, opt = {}) {
  const shape = pts instanceof THREE.Shape ? pts : new THREE.Shape(pts.map(([x, y]) => new THREE.Vector2(x, y)));
  if (opt.holes) for (const h of opt.holes) shape.holes.push(new THREE.Path(h.map(([x, y]) => new THREE.Vector2(x, y))));
  const cc = Math.min(c, depth * 0.45);
  const g = new THREE.ExtrudeGeometry(shape, { depth: Math.max(depth - 2 * cc, 1e-5), bevelEnabled: cc > 0, bevelThickness: cc, bevelSize: cc, bevelOffset: -cc, bevelSegments: 1, curveSegments: opt.curveSegments ?? 6, steps: 1 });
  g.translate(0, 0, -(depth - 2 * cc) / 2);
  return prep(g, { crease: opt.crease ?? CREASE_DEG });
}
export function gTube(points, r, radialSeg = 8, tubularSeg = 24, opt = {}) {
  const curve = opt.curve || new THREE.CatmullRomCurve3(points.map(p => new THREE.Vector3(...p)), false, 'catmullrom', opt.tension ?? 0.1);
  return prep(new THREE.TubeGeometry(curve, tubularSeg, r, radialSeg, false), { smooth: true });
}
export function gTorus(R, r, radialSeg = 6, tubularSeg = 24, arc = Math.PI * 2) { return prep(new THREE.TorusGeometry(R, r, radialSeg, tubularSeg, arc), { smooth: true }); }
export function gSphere(r, wSeg = 12, hSeg = 8, opt = {}) { return prep(new THREE.SphereGeometry(r, wSeg, hSeg, opt.phiStart, opt.phiLength, opt.thetaStart, opt.thetaLength), { smooth: true }); }
export function gPlane(w, h) { return prep(new THREE.PlaneGeometry(w, h), { keepUV: true, smooth: true }); } // faces three +z = Unity −z (front)
export function gDisc(r, seg = 24) { return prep(new THREE.CircleGeometry(r, seg), { keepUV: true, smooth: true }); }
// convex polygon prism from 2D points (x, y), extruded along z, no chamfer
export function gPrism(pts, depth) { return gExtrude(pts, depth, 0); }
// helpers for 2D outlines
export function roundedRect(w, h, r, n = 3) { const pts = []; const cs = [[w / 2 - r, h / 2 - r, 0], [-w / 2 + r, h / 2 - r, 90], [-w / 2 + r, -h / 2 + r, 180], [w / 2 - r, -h / 2 + r, 270]];
  for (const [cx, cy, a0] of cs) for (let i = 0; i <= n; i++) { const a = (a0 + 90 * i / n) * D2R; pts.push([cx + r * Math.cos(a), cy + r * Math.sin(a)]); } return pts; }
export function circlePts(r, n = 24, cx = 0, cy = 0) { const p = []; for (let i = 0; i < n; i++) { const a = i / n * Math.PI * 2; p.push([cx + r * Math.cos(a), cy + r * Math.sin(a)]); } return p; }
export function tris(g) { return (g.index ? g.index.count : g.attributes.position.count) / 3; }

// ---------------------------------------------------------------- Unity ↔ three
export const U = (x, y, z) => new THREE.Vector3(x, y, -z);
export function setUnityEuler(obj, e) { obj.rotation.order = 'YXZ'; obj.rotation.set(-(e[0] || 0) * D2R, -(e[1] || 0) * D2R, (e[2] || 0) * D2R); }
const AX = { x: [1, 0, 0], y: [0, 1, 0], z: [0, 0, 1] };

// ---------------------------------------------------------------- Model
export const BUDGET = { big: 4000, small: 800, raid: 400, unit: 1500 };
export class Model {
  constructor(name, o = {}) {
    this.name = name; this.root = new THREE.Group(); this.root.name = name;
    this.meta = { name, family: o.family || '', budget: o.budget || 'small', origin: o.origin || 'taban ortası', note: o.note || '', listName: o.listName || name,
      parts: [], texts: [], anchors: [], lights: [], variants: o.variants || null, states: o.states || null };
    this.parts = { Body: { group: this.root, pivot: [0, 0, 0], parent: null } };
  }
  // pivotModel: pivot in MODEL space (Unity). axis 'x'|'y'|'z' (local), range [a, b] in deg (rotate) or m (slide), kind 'rotate'|'slide'|'swap'
  part(name, o = {}) {
    const parent = this.parts[o.parent || 'Body']; if (!parent) throw new Error('no parent part ' + o.parent);
    const pv = o.pivot || [0, 0, 0]; const g = new THREE.Group(); g.name = name;
    g.position.copy(U(pv[0] - parent.pivot[0], pv[1] - parent.pivot[1], pv[2] - parent.pivot[2])); parent.group.add(g);
    this.parts[name] = { group: g, pivot: pv, parent: o.parent || 'Body' };
    this.meta.parts.push({ name, parent: o.parent || 'Body', pivotModel: pv, pivotLocal: [pv[0] - parent.pivot[0], pv[1] - parent.pivot[1], pv[2] - parent.pivot[2]],
      kind: o.kind || 'rotate', axis: o.axis || null, range: o.range || null, rest: o.rest ?? 0, note: o.note || '' });
    return g;
  }
  // add a geometry (three space, centred) at Unity position (MODEL space) with Unity euler, into part
  add(geom, matKey, pos = [0, 0, 0], rot = [0, 0, 0], part = 'Body', o = {}) {
    const P = this.parts[part]; if (!P) throw new Error(`${this.name}: no part ${part}`);
    material(matKey); // validates key
    const mesh = new THREE.Mesh(geom, material(matKey)); mesh.name = `${part}:${matKey}`;
    mesh.position.copy(U(pos[0] - P.pivot[0], pos[1] - P.pivot[1], pos[2] - P.pivot[2])); setUnityEuler(mesh, rot);
    if (o.scale) mesh.scale.set(...o.scale);
    mesh.castShadow = o.cast !== false; mesh.receiveShadow = true; mesh.userData.mat = matKey;
    P.group.add(mesh); return mesh;
  }
  box(w, h, d, c, mat, pos, rot, part, o) { return this.add(gBox(w, h, d, c, o), mat, pos, rot, part, o); }
  cyl(r, h, seg, c, mat, pos, rot, part, o = {}) { return this.add(gCyl(r, h, seg, c, o), mat, pos, rot, part, o); }
  lathe(profile, seg, mat, pos, rot, part, o = {}) { return this.add(gLathe(profile, seg, o), mat, pos, rot, part, o); }
  extrude(pts, depth, c, mat, pos, rot, part, o = {}) { return this.add(gExtrude(pts, depth, c, o), mat, pos, rot, part, o); }
  // text slot: TMP rectangle. center MODEL space; size [w, h] m; normal (Unity) the reading side; role = type token; capMm = cap height
  text(name, o) { this.meta.texts.push({ name, part: o.part || 'Body', center: o.center, size: o.size, normal: o.normal || [0, 0, -1], up: o.up || [0, 1, 0],
    role: o.role || '', font: o.font || 'Courier Prime 700', capMm: o.capMm || null, align: o.align || 'center', sample: o.sample || '', sampleEn: o.sampleEn || '', note: o.note || '' }); }
  anchor(name, o) { this.meta.anchors.push({ name, part: o.part || 'Body', pos: o.pos, note: o.note || '' }); }
  light(name, o) { this.meta.lights.push({ name, part: o.part || 'Body', pos: o.pos, type: o.type || 'point', color: o.color || '', shadows: !!o.shadows, note: o.note || '' }); }
  // pose a moving part for previews (deg or m along axis)
  pose(name, v) { const p = this.parts[name], m = this.meta.parts.find(q => q.name === name); if (!p || !m) return;
    const base = U(...m.pivotLocal);
    if (m.kind === 'slide') { const a = AX[m.axis]; p.group.position.copy(base).add(U(a[0] * v, a[1] * v, a[2] * v)); }
    else if (m.kind === 'rotate') { const e = [0, 0, 0]; e['xyz'.indexOf(m.axis)] = v; setUnityEuler(p.group, e); } }
  stats() { let t = 0; const per = {}; const mats = {}; this.root.updateMatrixWorld(true);
    this.root.traverse(o => { if (!o.isMesh) return; const n = tris(o.geometry); t += n; const part = o.name.split(':')[0]; per[part] = (per[part] || 0) + n; mats[o.userData.mat] = (mats[o.userData.mat] || 0) + n; });
    return { tris: t, perPart: per, mats }; }
  bboxUnity() { this.root.updateMatrixWorld(true); const b = new THREE.Box3().setFromObject(this.root); return { min: [b.min.x, b.min.y, -b.max.z], max: [b.max.x, b.max.y, -b.min.z] }; }
}

// mirror/duplicate helpers
export function each(arr, fn) { arr.forEach(fn); }
export const MAT = MATS;
