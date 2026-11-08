// print tris, bbox (mm), parts, texts, anchors for given models
import { MODELS } from '/tmp/claude-0/-home-claude/f9f1d8f0-179f-52d6-bfd9-32e90720d06b/scratchpad/work/r3/registry.js';
import * as THREE from 'three';
for (const n of process.argv.slice(2)) {
  const def = MODELS[n]; if (!def) { console.log(n, 'MISSING'); continue; }
  const m = def.build(); let tris = 0; m.root.traverse(o => { if (o.isMesh) tris += (o.geometry.index ? o.geometry.index.count : o.geometry.attributes.position.count) / 3; });
  const b = new THREE.Box3().setFromObject(m.root); const s = b.getSize(new THREE.Vector3());
  const meta = m.meta || {};
  console.log(JSON.stringify({ n, tris, budget: meta.budget || def.budget, size: [s.x, s.y, s.z].map(v => Math.round(v * 1000)), listName: meta.listName || def.listName, family: meta.family || def.family,
    parts: Object.keys(m.parts || {}).map(k => `${k}${m.parts[k].kind ? ':' + m.parts[k].kind : ''}`), texts: (meta.texts || []).map(t => t.name), anchors: (meta.anchors || []).map(a => a.name), lights: (meta.lights || []).map(l => l.name) }));
}
