import { MODELS } from './registry.js';
const fam = {}; for (const [n, d] of Object.entries(MODELS)) { if (n.startsWith('_')) continue; let m; try { m = d.build(); } catch (e) { continue; } const f = m.family || m.meta.family; (fam[f] = fam[f] || []).push(n); }
for (const [f, l] of Object.entries(fam)) console.log(f, l.length, l.filter(n => /^(Visitor_|Pawn_M$|Pawn_S$)/.test(n) || f === 'shelf').join(' ').slice(0, 400));
console.log('total', Object.values(fam).flat().length);
