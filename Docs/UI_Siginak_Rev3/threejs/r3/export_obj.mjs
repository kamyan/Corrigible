// Export the model library: one OBJ (+MTL) per model, a manifest per model and an index.
// Usage: node export_obj.mjs [outDir] [family|ModelName ...]
//
// OBJ CONVENTION (see README): right-handed, Y up, metres. Unity's OBJ importer mirrors X, so vertices are written as
// (−x_unity, y_unity, z_unity); after import the model stands in Unity coordinates, front facing −z.
// Each part is "o <Part>" + "g <Part>". Body is in model space. A moving part is written RELATIVE TO ITS PIVOT
// (pivot = group origin); its place comes from the manifest (parent, pivotLocal, axis, range).
import fs from 'fs';
import path from 'path';
import * as THREE from 'three';
import { MODELS } from './registry.js';
import { MATS, NEW_KEYS } from './mats.js';
import { BUDGET } from './mlib.js';

const outDir = process.argv[2] || 'out/lib';
const filt = process.argv.slice(3);
fs.mkdirSync(path.join(outDir, 'obj'), { recursive: true });
fs.mkdirSync(path.join(outDir, 'manifest'), { recursive: true });

const f5 = (v) => (Math.abs(v) < 5e-7 ? 0 : +v.toFixed(5));
const f4 = (v) => (Math.abs(v) < 5e-6 ? 0 : +v.toFixed(4));
const srgb = (hex) => { const c = new THREE.Color(hex); c.convertLinearToSRGB(); return c; };

function exportModel(m) {
  m.root.updateMatrixWorld(true);
  const lines = [`# ${m.name} · Son Şalter Rev3 model library · metres, Y up, OBJ x = −Unity x`, `mtllib ${m.name}.mtl`];
  let vi = 1, vti = 1, vni = 1; const used = new Set(); const partTris = {};
  const partNames = Object.keys(m.parts);
  for (const pn of partNames) {
    const P = m.parts[pn]; const meshes = P.group.children.filter(o => o.isMesh);
    if (!meshes.length) continue;
    lines.push(`o ${pn}`, `g ${pn}`);
    const byMat = new Map();
    for (const me of meshes) { const k = me.userData.mat; if (!byMat.has(k)) byMat.set(k, []); byMat.get(k).push(me); }
    let pt = 0;
    for (const [k, list] of byMat) {
      used.add(k); const faces = [];
      for (const me of list) {
        me.updateMatrix(); const M = me.matrix; const NM = new THREE.Matrix3().getNormalMatrix(M);
        const g = me.geometry; const pos = g.attributes.position, nor = g.attributes.normal, uv = g.attributes.uv;
        const v = new THREE.Vector3(), n = new THREE.Vector3();
        const base = vi, baseT = vti, baseN = vni;
        for (let i = 0; i < pos.count; i++) {
          v.fromBufferAttribute(pos, i).applyMatrix4(M); lines.push(`v ${f5(-v.x)} ${f5(v.y)} ${f5(-v.z)}`);
          n.fromBufferAttribute(nor, i).applyMatrix3(NM).normalize(); lines.push(`vn ${f4(-n.x)} ${f4(n.y)} ${f4(-n.z)}`);
          lines.push(`vt ${f4(uv ? uv.getX(i) : 0)} ${f4(uv ? uv.getY(i) : 0)}`);
        }
        const det = M.determinant();
        for (let i = 0; i < pos.count; i += 3) {
          const a = i, b = det < 0 ? i + 2 : i + 1, c = det < 0 ? i + 1 : i + 2;
          faces.push(`f ${base + a}/${baseT + a}/${baseN + a} ${base + b}/${baseT + b}/${baseN + b} ${base + c}/${baseT + c}/${baseN + c}`);
        }
        vi += pos.count; vti += pos.count; vni += pos.count; pt += pos.count / 3;
      }
      lines.push(`usemtl ${k}`, ...faces);
    }
    partTris[pn] = pt;
  }
  const mtl = [`# ${m.name}`];
  for (const k of used) { const v = MATS[k]; const c = srgb(v.hex);
    mtl.push(`newmtl ${k}`, `Kd ${c.r.toFixed(4)} ${c.g.toFixed(4)} ${c.b.toFixed(4)}`, 'Ka 0 0 0', `Ks ${(v.metal ?? 0) > 0.5 ? '0.5 0.5 0.5' : '0.04 0.04 0.04'}`,
      `Ns ${Math.round((1 - (v.rough ?? 0.8)) * 900 + 4)}`, `d ${v.alpha ?? 1}`, 'illum 2');
    if (v.emissive) { const e = srgb(v.emissive); mtl.push(`Ke ${e.r.toFixed(4)} ${e.g.toFixed(4)} ${e.b.toFixed(4)}`); }
    mtl.push(''); }
  fs.writeFileSync(path.join(outDir, 'obj', `${m.name}.obj`), lines.join('\n') + '\n');
  fs.writeFileSync(path.join(outDir, 'obj', `${m.name}.mtl`), mtl.join('\n'));
  const st = m.stats(); const bb = m.bboxUnity();
  const man = { ...m.meta, tris: st.tris, trisPerPart: partTris, budgetTris: BUDGET[m.meta.budget], overBudget: st.tris > BUDGET[m.meta.budget],
    bbox: { min: bb.min.map(f4), max: bb.max.map(f4), size: bb.max.map((x, i) => f4(x - bb.min[i])) },
    materials: [...used].map(k => ({ key: k, ...MATS[k], tris: st.mats[k] || 0, isNew: NEW_KEYS.has(k) })) };
  fs.writeFileSync(path.join(outDir, 'manifest', `${m.name}.json`), JSON.stringify(man, null, 1));
  return man;
}

const index = []; let bad = 0;
for (const [name, def] of Object.entries(MODELS)) {
  if (filt.length && !filt.includes(name) && !filt.includes(def.family)) continue;
  // ek 11-05: shelf/diorama family, visitor silhouettes and the miner/fog pawns are out of the game (kept in source as legacy)
  if (!filt.includes(name) && (def.family === 'shelf' || /^Visitor_/.test(name) || name === 'Pawn_M' || name === 'Pawn_S')) continue;
  let m; try { m = def.build(); } catch (e) { console.log('BUILD FAIL', name, e.message); bad++; continue; }
  const man = exportModel(m);
  const warn = [];
  if (man.overBudget) warn.push(`OVER BUDGET ${man.tris}/${man.budgetTris}`);
  for (const p of man.parts) if (!p.axis && p.kind !== 'swap') warn.push(`part ${p.name} has no axis`);
  index.push({ name, family: def.family, listName: man.listName, tris: man.tris, budget: man.budget, size: man.bbox.size, parts: man.parts.map(p => p.name), texts: man.texts.length });
  console.log(`${warn.length ? '!!' : 'ok'} ${name.padEnd(26)} ${String(man.tris).padStart(5)}/${man.budgetTris} ${man.bbox.size.map(x => (x * 1000).toFixed(0)).join('×')} mm  parts:${man.parts.map(p => p.name).join(',') || '-'} ${warn.join(' ')}`);
  if (warn.length) bad++;
}
if (!filt.length) fs.writeFileSync(path.join(outDir, 'models.json'), JSON.stringify(index, null, 1));
console.log(`${index.length} models, ${bad} with warnings`);
