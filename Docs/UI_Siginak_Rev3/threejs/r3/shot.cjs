// node r3/shot.cjs <url> <out> [w h] [timeoutS] — prints errors immediately
const { chromium } = require('playwright');
(async () => {
  const [url, out, w = 1920, h = 1080, to = 240] = process.argv.slice(2);
  const browser = await chromium.launch({ args: ['--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist'] });
  const page = await browser.newPage({ viewport: { width: +w, height: +h }, deviceScaleFactor: 1 });
  page.on('console', m => { if (m.type() === 'error' || m.type() === 'warning') console.log('[console]', m.text().slice(0, 300)); });
  page.on('pageerror', e => console.log('[pageerror]', e.message.slice(0, 400)));
  const t0 = Date.now();
  await page.goto(url, { waitUntil: 'load', timeout: 120000 });
  try { await page.waitForFunction(() => window.__done, null, { timeout: +to * 1000, polling: 500 }); }
  catch (e) { console.log('TIMEOUT'); await browser.close(); process.exit(1); }
  const info = await page.evaluate(() => window.__done);
  require('fs').writeFileSync(out.replace(/\.(png|jpg)$/, '.json'), JSON.stringify(info, null, 1));
  await page.screenshot({ path: out, type: out.endsWith('.jpg') ? 'jpeg' : 'png', quality: out.endsWith('.jpg') ? 92 : undefined });
  console.log('ok', out, ((Date.now() - t0) / 1000).toFixed(1) + 's');
  await browser.close();
})();
