import { PORTRAITS, portraitSVG } from './portraits.js';
import fs from 'fs';
for (const [id, spec] of Object.entries(PORTRAITS)) fs.writeFileSync(`portraits/${id}.svg`, portraitSVG(spec, { id }));
// contact sheet
const ids = Object.keys(PORTRAITS);
let html = `<!doctype html><html><head><meta charset="utf-8"><link rel="stylesheet" href="/node_modules/@fontsource/courier-prime/700.css"><style>body{margin:0;background:#D8CDB2;font:700 22px 'Courier Prime'}.g{display:grid;grid-template-columns:repeat(5,300px);gap:24px;padding:24px}.c{display:flex;flex-direction:column;gap:6px}</style></head><body><div class="g">`;
for (const id of ids) html += `<div class="c">${portraitSVG(PORTRAITS[id], { id })}<span>${id.slice(2)} · ${PORTRAITS[id].profile}</span></div>`;
html += `</div><script>window.__done={ok:1}</script></body></html>`;
fs.writeFileSync('portraits/sheet.html', html);
console.log('ok', ids.length);
