import { MODELS } from './registry.js'; import * as THREE from 'three'; import { BOARD } from './models/board.js';
const m = MODELS.PlanningBoard.build(); m.root.updateMatrixWorld(true); let hid = 0, all = 0; const bb = new THREE.Box3(), c = new THREE.Vector3(), s = new THREE.Vector3();
m.root.traverse(o => { if (!o.isMesh) return; const t = (o.geometry.index ? o.geometry.index.count : o.geometry.attributes.position.count) / 3; all += t; bb.setFromObject(o); bb.getCenter(c); bb.getSize(s);
  if (bb.max.y < BOARD.y0 - 0.005 || s.y > 1.0 || (Math.abs(c.x) > 0.607 && c.y < BOARD.y0 + BOARD.PH)) hid += t; });
console.log('all', all, 'stand', hid, 'wall', all - hid);
