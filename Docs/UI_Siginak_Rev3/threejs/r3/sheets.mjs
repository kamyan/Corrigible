// Render model sheets: node sheets.mjs <outDir> [family|Model ...] [--pose Name={"Part":deg}] [--w 1600 --h 1000] [--jpg]
import { createRequire } from 'module';
import fs from 'fs';
import { MODELS } from './registry.js';
const require = createRequire(import.meta.url);
const { chromium } = require('playwright');
const args = process.argv.slice(2); const out = args[0] || 'out/sheets'; const sel = []; let W = 1600, H = 1000, jpg = false; const poses = {}; let extra = '';
for (let i = 1; i < args.length; i++) { const a = args[i];
  if (a === '--w') W = +args[++i]; else if (a === '--h') H = +args[++i]; else if (a === '--jpg') jpg = true;
  else if (a === '--q') extra += '&' + args[++i]; else if (a === '--pose') { const [n, p] = args[++i].split('='); poses[n] = p; } else sel.push(a); }
fs.mkdirSync(out, { recursive: true });
const names = Object.entries(MODELS).filter(([n, d]) => (!sel.length || sel.includes(n) || sel.includes(d.family)) && d.family !== 'test' || sel.includes(n)).map(([n]) => n);
const browser = await chromium.launch({ args: ['--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist'] });
const page = await browser.newPage({ viewport: { width: W, height: H }, deviceScaleFactor: 1 });
page.on('pageerror', e => console.log('[pageerror]', e.message));
page.on('console', m => { if (m.type() === 'error') console.log('[console]', m.text()); });
for (const n of names) {
  const url = `http://127.0.0.1:8765/r3/sheet.html?m=${encodeURIComponent(n)}&w=${W}&h=${H}` + (poses[n] ? `&pose=${encodeURIComponent(poses[n])}` : '') + extra;
  await page.goto(url, { waitUntil: 'load' });
  try { await page.waitForFunction(() => window.__done, null, { timeout: 120000 }); } catch (e) { console.log('timeout', n); continue; }
  const info = await page.evaluate(() => window.__done);
  const f = `${out}/${n}${extra.includes('view=hero') ? '_hero' : ''}.${jpg ? 'jpg' : 'png'}`;
  await page.screenshot({ path: f, type: jpg ? 'jpeg' : 'png', quality: jpg ? 90 : undefined });
  console.log('sheet', n, info.tris ?? info.err);
}
await browser.close();
